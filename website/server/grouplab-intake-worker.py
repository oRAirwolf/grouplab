#!/usr/bin/env python3
"""Rebuild every uploaded photograph from its pixels, NOTES-FROM-PLANNING.md entry 129 section 3.5.

The receiver writes an upload into quarantine and does nothing else with it. This takes it from
there, decodes it, and writes a **new** PNG from the decoded pixels only. The original bytes are
then deleted. Only the rebuilt image ever leaves quarantine.

**This is the strongest single protection here, and it is worth saying why.** Checking a file's
type tells you what it claims to be. A file can carry the right magic bytes and still hide a
payload: data appended after the image, a polyglot that is a valid image and a valid script at
once, a crafted metadata block aimed at whatever opens it next. None of that survives being
decoded to a pixel array and written out again, because nothing but the pixels is carried over.

It keeps exactly the camera facts GroupLab measures with, validated by type and range, written
freshly into the new file rather than copied as bytes. GPS, timestamps, maker notes, thumbnails,
XMP, comments, serial numbers and owner fields are not kept, and cannot be: they are never read
into the new file, because the new file is built from nothing but pixels and a short list of
numbers.

It runs as its own systemd service, not as the web user and not in PHP, with no network, a
read-only view of everything but its own folders, and limits on memory, time and pixels.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

PRIVATE = Path("/home/airwolf/web/grouplab.org/private")
QUARANTINE = PRIVATE / "quarantine"
READY = PRIVATE / "ready"
REFUSED = PRIVATE / "refused"
LOG = Path("/home/airwolf/logs/grouplab-intake-worker.log")

# A decompression bomb is a small file that decodes to something enormous, so the pixel count is capped
# before anything is decoded. NOTES-FROM-PLANNING.md entry 176 section 3: the cap and the unit's memory
# limit are one decision, derived here and held together by a test. The rebuild holds at most three whole
# copies of the image at once, the decoded one, the one turned upright and the one converted to RGB, at up
# to four bytes a pixel: twelve bytes a pixel. 120 megapixels takes 1.44 GB that way, and the interpreter
# and Pillow about 100 MB more, so the unit's MemoryMax is 1600M. 120 megapixels takes a 108 megapixel
# phone and a 1200 dpi scan of a letter sheet; a 200 megapixel phone photograph is refused with the reason.
# It used to be 600 megapixels, which needs 7 GB this way and would have been killed by the 1 GB limit on a
# file it had already accepted.
MAX_PIXELS = 120_000_000
BYTES_PER_PIXEL_AT_PEAK = 12
MEMORY_MAX_MB = 1600

# NOTES-FROM-PLANNING.md entry 182: the scanner is handed each file's bytes as a stream, so clamd's limits must exceed the largest file
# the worker scans. That is not the 30 MB upload but the PNG rebuilt from it: at worst three bytes a pixel, colour with no alpha, and
# noise that does not compress, at the 120 megapixel cap, 360 MB, plus a filter byte a row and zlib's own few bytes. So 400 MB for
# StreamMaxLength, and for MaxFileSize and MaxScanSize too, because a file over either is skipped and reported clean unless
# AlertExceedsMax is on. install.py --intake refuses to finish while clamd.conf says less.
LARGEST_SCAN_BYTES = MAX_PIXELS * 3 + 16 * 1024 * 1024
CLAMD_LIMIT_MB = 400

# A submission the worker has started three times and never finished, because it was killed or crashed, is
# refused with that reason rather than tried every two minutes for ever. Entry 176 section 5.
#
# A folder a person moves back from refused keeps its count, deliberately (entry 186 section 1.1): the count is what stops a submission
# that kills the worker from being tried for ever, and a folder moved back by a script or by mistake must not reset it. The cost is that
# a folder refused for a bug can be moved back twice and no more; a person moving one back a third time deletes its .attempts first.
MAX_ATTEMPTS = 3
ATTEMPTS = ".attempts"

# NOTES-FROM-PLANNING.md entry 183. The receiver writes this marker beside meta.json when the contributor ticks "Do not include my
# photos in the public data set", so somebody listing the folder sees it without opening a file. It is not an image, and the worker
# once tried to decode it and refused every opted out submission. It travels with the folder to ready, and it must agree with
# meta.json's exclude_from_public_dataset, or the submission is refused rather than guessed at.
DO_NOT_PUBLISH = "DO-NOT-PUBLISH"

# Everything in a submission folder that is not an upload: the receiver's record and marker, and the worker's own bookkeeping. A folder
# moved back from refused carries refused.txt, which is dropped when it is tried again.
BOOKKEEPING = ("meta.json", ATTEMPTS, DO_NOT_PUBLISH, "refused.txt")

# Nothing bigger than the receiver would have accepted in the first place.
MAX_BYTES = 30 * 1024 * 1024

# Quarantine is a waiting room, not a store. Entry 129 section 3.7. But entry 176 section 9.3: nothing is
# deleted from it for age. A folder still in quarantine is one the worker has not finished, and deleting it
# for sitting there an hour would have destroyed the first two real submissions on the night the worker was
# being killed. What cannot be finished goes to refused, with its reason, after MAX_ATTEMPTS.
REFUSED_DAYS = 7

# NOTES-FROM-PLANNING.md entries 215 and 216: nothing stays on the server longer than it is needed, and no folder grows without bound.
# A folder in ready is removed by Alan's pull once it is verified and archived; one that nobody pulls is deleted after sixty days
# regardless, with the reason in the log. Sixty rather than fewer, because deleting a submission before it has been pulled loses it; each
# pull clears ready of everything it has verified and archived, so this is for a pull that has not happened in two months.
READY_DAYS = 60

# The camera facts GroupLab measures with, and nothing else.
#
# **This list is not the authority.** `ImageScrubber.KeptFieldNames` in the application is, and
# `WhitelistTests` fails if this drifts from it. Entry 129 section 3.5.2a is explicit that there
# must not be a third copy of this list deciding anything; this is a transcription that a test
# holds to the original.
KEPT = [
    "Make",
    "Model",
    "Orientation",
    "ExposureTime",
    "FNumber",
    "ISOSpeedRatings",
    "FocalLength",
    "PixelXDimension",
    "PixelYDimension",
    "DigitalZoomRatio",
    "FocalLengthIn35mmFilm",
    "LensModel",
]

# A camera or lens name is somebody else's text in a file this machine will publish, so it is cut
# to printable ASCII and to a length. 64 is longer than any real lens name and short enough that
# nothing can be smuggled in it.
MAX_TEXT = 64


def log(message: str) -> None:
    line = time.strftime("%Y-%m-%dT%H:%M:%S%z") + " " + message
    print(line)
    try:
        LOG.parent.mkdir(parents=True, exist_ok=True)
        if LOG.exists() and LOG.stat().st_size > 2 * 1024 * 1024:
            LOG.replace(LOG.with_suffix(".log.1"))
        with LOG.open("a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass


def clean_text(value: object) -> str | None:
    """Somebody else's string, made safe to write and safe to read."""
    if not isinstance(value, str):
        return None
    printable = "".join(c for c in value if 32 <= ord(c) < 127).strip()
    return printable[:MAX_TEXT] or None


def clean_number(value: object, low: float, high: float) -> float | None:
    """A number, or nothing. A rational out of range is a broken file, not a fact."""
    try:
        if isinstance(value, tuple) and len(value) == 2:
            value = value[0] / value[1] if value[1] else None
        number = float(value)  # type: ignore[arg-type]
    except (TypeError, ValueError, ZeroDivisionError):
        return None
    if number != number or number in (float("inf"), float("-inf")):
        return None
    return number if low <= number <= high else None


def sha256_of(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def clamav() -> str | None:
    """
    Which ClamAV is usable here, if either. Reported rather than assumed.

    Entry 176: the daemon's client first. Standalone clamscan loads the whole signature database into its
    own memory every run, about a gigabyte, which the worker's memory limit killed every time. With the
    daemon the database lives once, in clamd, and the worker stays small.
    """
    if shutil.which("clamdscan"):
        return "clamdscan"
    if shutil.which("clamscan"):
        return "clamscan"
    return None


def scan(path: Path, tool: str | None) -> tuple[bool, str]:
    """
    Whether the file may go on, and what the scan did, for the file's record.

    0 is clean and 1 is found. Anything else is the scanner not completing a scan, killed, broken or not
    reachable. Entry 129 accepted the rebuild from pixels as the real defence, so the file still goes on, but
    entry 176 section 9.2: a scanner that did not scan is a broken installation, so it is said in the file's
    record, where the pull script counts it, and loudly in the log.
    """
    if tool is None:
        return True, "not scanned: no ClamAV on the server"

    # Entry 182: --stream sends the file's bytes over clamd's socket, so clamd opens nothing. --fdpass handed it a descriptor from
    # inside the worker's sandbox, a mount clamd cannot see, and clamd's AppArmor profile refused it as a disconnected path: no upload
    # was scanned. Streaming keeps the sandbox exactly as tight and quarantine 0750 airwolf.
    command = [tool, "--stream", "--no-summary", str(path)] if tool == "clamdscan" else [tool, "--no-summary", str(path)]
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=300)
    except (OSError, subprocess.TimeoutExpired) as e:
        log(f"  SCANNER DID NOT RUN on {path.name}: {type(e).__name__}")
        return True, f"not scanned: {tool} did not run, {type(e).__name__}"

    said = (result.stdout or "") + (result.stderr or "")
    if "Limits.Exceeded" in said or "size limit" in said.lower():
        # A stream or a file over clamd's limits is not scanned in full, whatever the exit code, and is never called clean.
        log(f"  SCANNER DID NOT COMPLETE on {path.name}: it is over clamd's limits, {path.stat().st_size} bytes; raise StreamMaxLength, MaxFileSize and MaxScanSize to {CLAMD_LIMIT_MB}M")
        return True, f"not scanned: over clamd's limits at {path.stat().st_size} bytes"
    if result.returncode == 1:
        log(f"  {tool} found something in {path.name}")
        return False, f"{tool} found something"
    if result.returncode != 0:
        why = (result.stderr or result.stdout or "").strip().splitlines()
        detail = why[0][:120] if why else ""
        log(f"  SCANNER DID NOT COMPLETE on {path.name}: {tool} exit {result.returncode} {detail}; the rebuild is the only check on it")
        return True, f"not scanned: {tool} exit {result.returncode}"
    return True, f"clean, {tool}"


def is_heic(path: Path) -> bool:
    """HEIC and HEIF, by their own bytes: an ISO media box whose brand is one of HEIF's."""
    with path.open("rb") as f:
        head = f.read(12)
    return len(head) == 12 and head[4:8] == b"ftyp" and head[8:12] in (b"heic", b"heix", b"hevc", b"hevx", b"mif1", b"msf1")


def decodable(original: Path, work: Path) -> Path:
    """
    The file Pillow is to decode: the original, or for HEIC, a PNG made from it by heif-convert.

    Entry 176 section 9.1: phones send HEIC, and Ubuntu's Pillow cannot read it. libheif's own converter,
    from the distribution's libheif-examples, decodes it to pixels with the image already turned upright, and
    the PNG it writes is decoded and rebuilt like any other file, so nothing of the HEIC travels. Its camera
    facts do not come across that way, and the record says the file was HEIC.
    """
    if not is_heic(original):
        return original
    converter = shutil.which("heif-convert")
    if converter is None:
        raise RuntimeError("a HEIC photograph, and heif-convert is not installed; the installer should have refused")
    out = work / (original.stem + ".heic.png")
    result = subprocess.run([converter, str(original), str(out)], capture_output=True, text=True, timeout=300)
    if result.returncode != 0 or not out.is_file():
        raise ValueError(f"heif-convert could not decode it, exit {result.returncode}")
    return out


def facts_from(image) -> dict[str, object]:
    """The whitelist, read from the original and validated. Nothing outside KEPT is looked at."""
    from PIL import ExifTags

    raw = {}
    try:
        exif = image.getexif()
        by_name = {ExifTags.TAGS.get(t, str(t)): v for t, v in exif.items()}
        for ifd in (getattr(ExifTags, "IFD", None) and [ExifTags.IFD.Exif]) or []:
            try:
                for t, v in exif.get_ifd(ifd).items():
                    by_name[ExifTags.TAGS.get(t, str(t))] = v
            except (KeyError, AttributeError, OSError):
                pass
        raw = by_name
    except (AttributeError, OSError, ValueError):
        raw = {}

    facts: dict[str, object] = {}
    for name in KEPT:
        if name not in raw:
            continue
        value = raw[name]
        if name in ("Make", "Model", "LensModel"):
            cleaned = clean_text(value)
        elif name == "Orientation":
            cleaned = clean_number(value, 1, 8)
            cleaned = int(cleaned) if cleaned is not None else None
        elif name == "ExposureTime":
            cleaned = clean_number(value, 0, 3600)
        elif name == "FNumber":
            cleaned = clean_number(value, 0.5, 100)
        elif name == "ISOSpeedRatings":
            cleaned = clean_number(value, 1, 1_000_000)
        elif name in ("FocalLength", "FocalLengthIn35mmFilm"):
            cleaned = clean_number(value, 0.1, 100_000)
        elif name == "DigitalZoomRatio":
            cleaned = clean_number(value, 0, 1000)
        elif name in ("PixelXDimension", "PixelYDimension"):
            cleaned = clean_number(value, 1, 1_000_000)
        else:
            cleaned = None

        if cleaned is not None:
            facts[name] = cleaned

    return facts


def rebuild(original: Path, into: Path) -> dict[str, object]:
    """
    Decodes the original and writes a new PNG from its pixels. Returns what was kept.

    Nothing of the original file travels: not a byte, not a chunk, not a segment. The new file is
    a PNG written from a pixel array, plus the resolution and a freshly built EXIF block holding
    only validated numbers and cut strings.
    """
    from PIL import Image

    Image.MAX_IMAGE_PIXELS = MAX_PIXELS

    with Image.open(decodable(original, into.parent)) as image:
        width, height = image.size
        if width * height > MAX_PIXELS:
            raise ValueError(f"{width} by {height} is more pixels than this will decode")

        facts = facts_from(image)

        # The orientation is applied to the pixels, and the original value recorded, so the rebuilt
        # file is the right way up without anything having to read a tag to know it.
        from PIL import ImageOps

        upright = ImageOps.exif_transpose(image)
        if upright is None:
            upright = image

        # NOTES-FROM-PLANNING.md entry 177 section 2: the pixels are upright now, so the new file says so. Copying the original value onto
        # turned pixels told everything that honours the tag, GroupLab included, to turn them a second time, and a portrait phone photograph
        # showed on its side. The value the camera wrote is kept in the record, never in the file.
        if "Orientation" in facts:
            facts["OriginalOrientation"] = facts["Orientation"]
            facts["Orientation"] = 1

        # sRGB, and the profile dropped: a colour profile is another block of somebody else's data.
        if upright.mode not in ("RGB", "L"):
            upright = upright.convert("RGB")
        else:
            upright = upright.copy()

        dpi = image.info.get("dpi")

    resolution = None
    if isinstance(dpi, tuple) and len(dpi) == 2:
        x = clean_number(dpi[0], 1, 20000)
        y = clean_number(dpi[1], 1, 20000)
        if x and y:
            resolution = (x, y)

    save: dict[str, object] = {"optimize": True}
    if resolution:
        save["dpi"] = resolution

    exif = build_exif(facts)
    if exif is not None:
        save["exif"] = exif

    upright.save(into, "PNG", **save)

    facts["RebuiltWidth"] = upright.size[0]
    facts["RebuiltHeight"] = upright.size[1]
    if resolution:
        facts["Dpi"] = [resolution[0], resolution[1]]
    return facts


def build_exif(facts: dict[str, object]):
    """A new EXIF block, built from validated values. No byte of the original block is reused."""
    try:
        from PIL import Image, ExifTags
    except ImportError:
        return None

    by_name = {v: k for k, v in ExifTags.TAGS.items()}
    exif = Image.Exif()
    written = 0
    for name, value in facts.items():
        tag = by_name.get(name)
        if tag is None:
            continue
        try:
            exif[tag] = value
            written += 1
        except (ValueError, TypeError, OverflowError):
            continue

    return exif if written else None


def attempts(folder: Path) -> int:
    """How many times the worker has started this submission, counted before it starts, so a kill still counts."""
    marker = folder / ATTEMPTS
    try:
        n = int(marker.read_text(encoding="utf-8").strip() or "0")
    except (OSError, ValueError):
        n = 0
    return n


def refuse(folder: Path, reason: str) -> None:
    """To refused, with the reason beside it, where the seven day rule applies."""
    REFUSED.mkdir(parents=True, exist_ok=True)
    (folder / "refused.txt").write_text(reason + "\n", encoding="utf-8")
    shutil.move(str(folder), str(REFUSED / folder.name))
    log(f"{folder.name}: refused, {reason}")


def rebuilt_name(uploaded: str) -> str:
    """The name the rebuilt PNG of an upload takes: its own stem, and "-rebuilt" where the upload was a PNG already."""
    stem = Path(uploaded)
    target = stem.with_suffix(".png")
    return (stem.stem + "-rebuilt.png") if target.name == uploaded else target.name


def read_record(folder: Path) -> tuple[dict | None, str | None]:
    """The receiver's meta.json, or why it cannot be used. Without it neither the uploads nor the consent are known."""
    meta = folder / "meta.json"
    if not meta.is_file():
        return None, "there is no meta.json, so neither its files nor its consent are known"
    try:
        record = json.loads(meta.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as e:
        return None, f"meta.json would not read: {type(e).__name__}"
    if not isinstance(record, dict):
        return None, "meta.json is not a record"
    return record, None


def opted_out(folder: Path, record: dict) -> tuple[bool | None, str | None]:
    """
    Entry 183 section 3.1: the opt out, from meta.json and the marker together. They are written by the same request, so they agree on
    every submission the receiver made; where they do not, something has been changed by hand or lost, and the worker says so and stops
    rather than choosing one.
    """
    flag = record.get("exclude_from_public_dataset")
    marker = (folder / DO_NOT_PUBLISH).is_file()
    if not isinstance(flag, bool):
        return None, "meta.json does not say whether the contributor opted out of the public data set"
    if flag != marker:
        return None, (f"the {DO_NOT_PUBLISH} marker and meta.json disagree about the opt out: meta.json says "
                      f"exclude_from_public_dataset is {str(flag).lower()} and the marker is {'present' if marker else 'absent'}")
    return flag, None


def one(folder: Path, tool: str | None) -> tuple[bool, str]:
    """One submission. True where every file in it passed and it moved to ready; otherwise the reason."""
    record, problem = read_record(folder)
    if record is None:
        return False, problem or "meta.json would not read"

    opt_out, problem = opted_out(folder, record)
    if opt_out is None:
        return False, problem or "the opt out is not known"

    # Entry 183 section 3.2: the uploads are what the receiver recorded, never whatever is in the folder.
    uploads = record.get("files")
    if not isinstance(uploads, list) or not uploads:
        return False, "meta.json records no uploaded files"
    expected: list[tuple[str, dict]] = []
    for entry in uploads:
        name = entry.get("stored_name") if isinstance(entry, dict) else None
        if not isinstance(name, str) or not name or name != Path(name).name or name.startswith(".") or name in BOOKKEEPING:
            return False, "meta.json records an upload by a name the worker will not use: " + repr(name)[:80]
        expected.append((name, entry))

    allowed = set(BOOKKEEPING) | {name for name, _ in expected} | {rebuilt_name(name) for name, _ in expected}
    for present in sorted(folder.iterdir()):
        if present.name not in allowed or not present.is_file():
            return False, f"{present.name} is in the folder and the receiver did not record it"

    if (folder / "refused.txt").is_file():
        log(f"{folder.name}: back from refused, tried again")
        (folder / "refused.txt").unlink()

    rebuilt = []
    for name, entry in expected:
        original = folder / name
        target = folder / rebuilt_name(name)
        again = False
        if not original.is_file():
            # Entry 183 section 4: a folder moved back from refused after its original was already rebuilt and deleted. The worker's
            # own PNG is all that is left, and it goes through exactly the same scan and rebuild as an upload would.
            if not target.is_file():
                return False, f"{name} is recorded by the receiver and is not in the folder"
            again = True
            original = target
            target = folder / (Path(rebuilt_name(name)).stem + ".again.png")
            log(f"{folder.name}: {name} was rebuilt by an earlier run and its original deleted; rebuilding again from {original.name}")

        if original.stat().st_size > MAX_BYTES and not again:
            return False, f"{original.name} is larger than the receiver accepts"

        before = sha256_of(original)
        received = entry.get("sha256")
        if not again and isinstance(received, str) and received and received.lower() != before:
            return False, f"{original.name} is not the file the receiver stored: its SHA-256 has changed"

        passed, scanned = scan(original, tool)
        if not passed:
            return False, f"{original.name}: {scanned}"

        was_heic = is_heic(original)
        try:
            facts = rebuild(original, target)
        except Exception as e:  # noqa: BLE001 - any failure to decode cleanly is a refusal
            target.unlink(missing_ok=True)
            return False, f"{original.name} would not decode cleanly: {type(e).__name__}: {e}"
        finally:
            for made in folder.glob("*.heic.png"):
                made.unlink(missing_ok=True)

        passed, rescanned = scan(target, tool)
        if not passed:
            target.unlink(missing_ok=True)
            return False, f"{target.name}: {rescanned}"

        if again:
            # The worker's earlier PNG is replaced by the one just made from it, under the same name.
            target.replace(original)
            target = original
            log(f"{folder.name}: rebuilt {name} again as {target.name}, {scanned}")
        else:
            # The original bytes go. From here on nothing of the uploaded file exists but its hash.
            original.unlink()
            log(f"{folder.name}: rebuilt {original.name} as {target.name}, original deleted, {scanned}")

        rebuilt.append({
            "stored": target.name,
            "uploaded": name,
            "originalSha256": received if again else before,
            "sha256": sha256_of(target),
            "scan": scanned,
            "heic": True if was_heic else None,
            "rebuiltAgain": True if again else None,
            "facts": facts,
        })

    meta = folder / "meta.json"
    record["files"] = [{k: v for k, v in f.items() if v is not None} for f in rebuilt]
    record["notScanned"] = sum(1 for f in rebuilt if str(f["scan"]).startswith("not scanned"))
    record["rebuiltUtc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    meta.write_text(json.dumps(record, indent=2), encoding="utf-8")
    (folder / ATTEMPTS).unlink(missing_ok=True)

    READY.mkdir(parents=True, exist_ok=True)
    shutil.move(str(folder), str(READY / folder.name))
    log(f"{folder.name}: ready, {len(rebuilt)} files" + (f", {record['notScanned']} NOT SCANNED" if record["notScanned"] else "")
        + (", opted out of the public data set" if opt_out else ""))
    return True, "ready"


def sweep() -> None:
    """
    Refused folders go after seven days, and ready ones nobody has pulled after sixty (entry 216). Nothing in quarantine is ever deleted
    for age: entry 176 section 9.3; a folder leaves it within MAX_ATTEMPTS runs, to ready or to refused.
    """
    now = time.time()
    for place, days, why in ((REFUSED, REFUSED_DAYS, "from refused"), (READY, READY_DAYS, "from ready, never pulled,")):
        for folder in place.iterdir() if place.is_dir() else []:
            if folder.is_dir() and now - folder.stat().st_mtime > days * 86400:
                shutil.rmtree(folder, ignore_errors=True)
                log(f"{folder.name}: deleted {why} after {days} days")


def main() -> int:
    parser = argparse.ArgumentParser(description="Rebuild quarantined uploads from their pixels.")
    parser.add_argument("--dry-run", action="store_true", help="say what is waiting and change nothing")
    args = parser.parse_args()

    tool = clamav()
    if tool is None:
        log("no ClamAV on this machine, so the rebuild is the only check. See docs/WEBSITE.md.")

    if not QUARANTINE.is_dir():
        log("nothing to do: there is no quarantine folder yet")
        sweep()
        return 0

    waiting = sorted(p for p in QUARANTINE.iterdir() if p.is_dir())
    if args.dry_run:
        log(f"dry run: {len(waiting)} waiting, scanner {tool or 'none'}. Nothing changed.")
        return 0

    for folder in waiting:
        # Counted before it starts, so a run the kernel kills part way still counts. Entry 176 section 5.1.
        n = attempts(folder) + 1
        if n > MAX_ATTEMPTS:
            refuse(folder, f"the worker started it {MAX_ATTEMPTS} times and never finished, killed or crashed each time")
            continue
        (folder / ATTEMPTS).write_text(str(n), encoding="utf-8")
        log(f"{folder.name}: attempt {n} of {MAX_ATTEMPTS}")
        try:
            done, why = one(folder, tool)
            if not done:
                refuse(folder, why)
        except Exception as e:  # noqa: BLE001 - one bad submission never stops the rest
            log(f"{folder.name}: unexpected failure on attempt {n}, {type(e).__name__}: {e}; left in quarantine for the next run")

    sweep()
    return 0


if __name__ == "__main__":
    sys.exit(main())
