"""Whether the website's screenshots still show the application, NOTES-FROM-PLANNING.md entry 253 section 5.

Alan: "at some point code should update all of the screenshots on the website." So that it does not have to be asked for again, every
published desktop picture carries a stamp, written here when the pictures are rendered: the newest nightly at the time, and for each
screen a hash of the source files that draw it (docs/figures/screens/screens.json). The site build calls problems() and fails when

- a screen's files have changed since its picture and a whole nightly has shipped the change without a new picture (one nightly of grace,
  because the screenshot job runs after the change reaches main and can lag the nightly by a few minutes); or
- the desktop pictures are more than maxNightlies behind the newest nightly, whatever changed; or
- the phone's pictures are more than phoneMaxNightlies behind, since only a sitting with Alan's devices can retake them.

    python scripts/screens-stamp.py --write        after rendering the desktop pictures (the screenshot job does this)
    python scripts/screens-stamp.py --phone N      after a device sitting on nightly N
    python scripts/screens-stamp.py --check        what the site build checks
"""

from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SCREENS = REPO / "docs" / "figures" / "screens"
MAP = SCREENS / "screens.json"
DESKTOP_STAMP = SCREENS / "current" / "made-from.json"
PHONE_STAMP = SCREENS / "phone" / "made-from.json"
NOTES = REPO / "docs" / "RELEASE-NOTES.md"
PICTURE = re.compile(r"^(?P<screen>[a-z0-9-]+?)-(?:dark|light)-\d+x\d+\.png$")


def newest_nightly() -> int:
    """The newest nightly's number, from the top of docs/RELEASE-NOTES.md, which the nightly writes itself."""
    found = re.search(r"^## \d+\.\d+\.\d+-nightly\.(\d+)\s*$", NOTES.read_text(encoding="utf-8"), re.M)
    if found is None:
        raise SystemExit("docs/RELEASE-NOTES.md names no nightly")
    return int(found.group(1))


def screen_map() -> dict:
    return json.loads(MAP.read_text(encoding="utf-8"))


def fingerprint(files: list[str]) -> str:
    """The files' contents, line endings made LF so a Windows checkout and a Linux one agree."""
    digest = hashlib.sha256()
    for name in sorted(set(files)):
        digest.update(name.encode() + b"\n")
        digest.update((REPO / name).read_bytes().replace(b"\r\n", b"\n"))
        digest.update(b"\n")
    return digest.hexdigest()


def fingerprints(mapping: dict) -> dict[str, str]:
    return {screen: fingerprint(mapping["shared"] + files) for screen, files in sorted(mapping["screens"].items())}


def write_json(path: Path, value: dict) -> None:
    path.write_bytes((json.dumps(value, indent=2) + "\n").encode("utf-8"))


def write_desktop() -> None:
    write_json(DESKTOP_STAMP, {
        "about": "Written by scripts/screens-stamp.py when these pictures were rendered: the newest nightly then, and each screen's source files' hash.",
        "afterNightly": newest_nightly(),
        "screens": fingerprints(screen_map()),
    })


def write_phone(nightly: int) -> None:
    write_json(PHONE_STAMP, {
        "about": "The nightly these phone and tablet pictures were taken on, written by scripts/screens-stamp.py after a device sitting.",
        "nightly": nightly,
    })


def problems() -> list[str]:
    found: list[str] = []
    mapping = screen_map()
    for name in mapping["shared"] + [f for files in mapping["screens"].values() for f in files]:
        if not (REPO / name).is_file():
            found.append(f"docs/figures/screens/screens.json names {name}, which is not in the repository")
    if found:
        return found

    pictured = {m.group("screen") for p in (SCREENS / "current").glob("*.png") if (m := PICTURE.match(p.name))}
    # Entry 256: a bull, a grid or a sheet drawn as it prints, one picture for both themes.
    pictured |= {p.stem for p in (SCREENS / "current").glob("sheet-*.png")}
    for screen in sorted(pictured - set(mapping["screens"])):
        found.append(f"docs/figures/screens/current has pictures of {screen!r} and screens.json does not say which files draw it")
    for screen in sorted(set(mapping["screens"]) - pictured):
        found.append(f"docs/figures/screens/screens.json names {screen!r} and no picture of it was rendered")

    newest = newest_nightly()
    if not DESKTOP_STAMP.is_file():
        return found + ["docs/figures/screens/current/made-from.json is missing: run scripts/screens-stamp.py --write after rendering"]
    stamp = json.loads(DESKTOP_STAMP.read_text(encoding="utf-8"))
    behind = newest - int(stamp["afterNightly"])
    if behind > mapping["maxNightlies"]:
        found.append(f"the desktop screenshots were rendered after nightly {stamp['afterNightly']}, {behind} nightlies behind "
                     f"{newest}; the limit is {mapping['maxNightlies']}. Run the screenshots workflow.")
    now = fingerprints(mapping)
    changed = [s for s in sorted(now) if stamp["screens"].get(s) != now[s]]
    if changed and behind >= 2:
        found.append(f"the code of {', '.join(changed)} changed after its picture, and nightly {newest} has shipped it without a new one. "
                     "Run the screenshots workflow.")

    if PHONE_STAMP.is_file():
        phone = int(json.loads(PHONE_STAMP.read_text(encoding="utf-8"))["nightly"])
        if newest - phone > mapping["phoneMaxNightlies"]:
            found.append(f"the phone's screenshots are from nightly {phone}, {newest - phone} behind {newest}; the limit is "
                         f"{mapping['phoneMaxNightlies']}. They are retaken in a device sitting (docs/notes/for-alan.md).")
    else:
        found.append("docs/figures/screens/phone/made-from.json is missing: it names the nightly the phone's pictures were taken on")
    return found


def main(argv: list[str]) -> int:
    if argv[:1] == ["--write"]:
        write_desktop()
        return 0
    if argv[:1] == ["--phone"] and len(argv) == 2:
        write_phone(int(argv[1]))
        return 0
    if argv[:1] == ["--check"]:
        found = problems()
        for line in found:
            print(line)
        print("screenshots: current" if not found else f"screenshots: {len(found)} problems")
        return 1 if found else 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
