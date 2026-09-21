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

# A decompression bomb is a small file that decodes to something enormous. 600 megapixels is far
# beyond any camera or flatbed scan this project will ever see, and small enough that a refusal
# costs a moment rather than the machine.
MAX_PIXELS = 600_000_000

# Nothing bigger than the receiver would have accepted in the first place.
MAX_BYTES = 30 * 1024 * 1024

# Quarantine is a waiting room, not a store. Entry 129 section 3.7.
QUARANTINE_HOURS = 1
REFUSED_DAYS = 7

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
    """Which ClamAV is usable here, if either. Reported rather than assumed."""
    if shutil.which("clamdscan"):
        return "clamdscan"
    if shutil.which("clamscan"):
        return "clamscan"
    return None


def scan(path: Path, tool: str | None) -> bool:
    """True where the file is clean or there is nothing to scan with."""
    if tool is None:
        return True
    result = subprocess.run([tool, "--no-summary", str(path)], capture_output=True, text=True, timeout=300)
    # 0 clean, 1 found, anything else is the scanner itself failing, which is not the file's fault.
    if result.returncode == 1:
        log(f"  {tool} found something in {path.name}")
        return False
    if result.returncode not in (0, 1):
        log(f"  {tool} could not scan {path.name}, exit {result.returncode}; letting it through on the rebuild instead")
    return True


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

    with Image.open(original) as image:
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


def one(folder: Path, tool: str | None) -> bool:
    """One submission. True where every file in it passed and it moved to ready."""
    files = sorted(p for p in folder.iterdir() if p.is_file() and p.name != "meta.json")
    if not files:
        log(f"{folder.name}: nothing in it")
        return False

    rebuilt = []
    for original in files:
        if original.stat().st_size > MAX_BYTES:
            log(f"{folder.name}: {original.name} is larger than the receiver accepts")
            return False

        if not scan(original, tool):
            return False

        before = sha256_of(original)
        target = original.with_suffix(".png")
        if target == original:
            target = original.with_name(original.stem + "-rebuilt.png")

        try:
            facts = rebuild(original, target)
        except Exception as e:  # noqa: BLE001 - any failure to decode cleanly is a refusal
            log(f"{folder.name}: {original.name} would not decode cleanly: {type(e).__name__}: {e}")
            target.unlink(missing_ok=True)
            return False

        if not scan(target, tool):
            target.unlink(missing_ok=True)
            return False

        # The original bytes go. From here on nothing of the uploaded file exists but its hash.
        original.unlink()

        rebuilt.append({
            "stored": target.name,
            "originalSha256": before,
            "sha256": sha256_of(target),
            "facts": facts,
        })
        log(f"{folder.name}: rebuilt {original.name} as {target.name}, original deleted")

    meta = folder / "meta.json"
    record = {}
    if meta.is_file():
        try:
            record = json.loads(meta.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            record = {}

    record["files"] = rebuilt
    record["rebuiltUtc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    meta.write_text(json.dumps(record, indent=2), encoding="utf-8")

    READY.mkdir(parents=True, exist_ok=True)
    shutil.move(str(folder), str(READY / folder.name))
    log(f"{folder.name}: ready, {len(rebuilt)} files")
    return True


def sweep() -> None:
    """Entry 129 section 3.7: quarantine is a waiting room, and refused files do not pile up."""
    now = time.time()
    for folder in QUARANTINE.iterdir() if QUARANTINE.is_dir() else []:
        if folder.is_dir() and now - folder.stat().st_mtime > QUARANTINE_HOURS * 3600:
            shutil.rmtree(folder, ignore_errors=True)
            log(f"{folder.name}: deleted, it sat in quarantine for more than {QUARANTINE_HOURS} hour")

    for folder in REFUSED.iterdir() if REFUSED.is_dir() else []:
        if folder.is_dir() and now - folder.stat().st_mtime > REFUSED_DAYS * 86400:
            shutil.rmtree(folder, ignore_errors=True)
            log(f"{folder.name}: deleted from refused after {REFUSED_DAYS} days")


def main() -> int:
    parser = argparse.ArgumentParser(description="Rebuild quarantined uploads from their pixels.")
    parser.add_argument("--dry-run", action="store_true", help="say what is waiting and change nothing")
    args = parser.parse_args()

    tool = clamav()
    if tool is None:
        log("no ClamAV on this machine, so the rebuild is the only check. See docs/WEBSITE.md.")

    if not QUARANTINE.is_dir():
        log("nothing to do: there is no quarantine folder yet")
        return 0

    waiting = sorted(p for p in QUARANTINE.iterdir() if p.is_dir())
    if args.dry_run:
        log(f"dry run: {len(waiting)} waiting, scanner {tool or 'none'}. Nothing changed.")
        return 0

    for folder in waiting:
        try:
            if not one(folder, tool):
                REFUSED.mkdir(parents=True, exist_ok=True)
                shutil.move(str(folder), str(REFUSED / folder.name))
                log(f"{folder.name}: refused")
        except Exception as e:  # noqa: BLE001 - one bad submission never stops the rest
            log(f"{folder.name}: unexpected failure, {type(e).__name__}: {e}")

    sweep()
    return 0


if __name__ == "__main__":
    sys.exit(main())
