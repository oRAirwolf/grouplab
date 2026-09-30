#!/usr/bin/env python3
"""The iPhone and iPad application's icon, from the desktop's mark.

NOTES-FROM-PLANNING.md entry 290 section 2 item 3. The same mark and the same dark background as the Android icon (scripts/android-icons.py,
entry 248), drawn the same way, as the single 1024 by 1024 image Xcode 26 makes every size from. iOS rounds the corners itself, so the image
is square and has no transparency. Run once when the mark changes, and commit what it writes:

    python3 scripts/ios-icons.py

It writes ios/GroupLab.iOS/Assets.xcassets/AppIcon.appiconset/icon-1024.png and the catalog's Contents.json files.
"""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent.parent
SPEC = importlib.util.spec_from_file_location("android_icons", HERE / "scripts" / "android-icons.py")
android = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(android)

CATALOG = HERE / "ios" / "GroupLab.iOS" / "Assets.xcassets"
ICON = CATALOG / "AppIcon.appiconset"
# Entry 315's amendment: GroupLab Dev's icon, the light background with the light mark as Android's ic_launcher_dev, so the two are never
# confused on a home screen.
ICON_DEV = CATALOG / "AppIconDev.appiconset"
SIZE = 1024


def write_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes((json.dumps(value, indent=2) + "\n").encode())


def icon(folder: Path, background: str, mark: Path) -> None:
    """One 1024 square icon and its catalog entry."""
    # The mark reaches 36 percent of the side from the middle, as the Play listing's icon does.
    image = android.render(SIZE, background, "square", android.circles(mark), SIZE * 0.36).convert("RGB")
    android.save(image, folder / "icon-1024.png")
    write_json(folder / "Contents.json", {
        "images": [{"filename": "icon-1024.png", "idiom": "universal", "platform": "ios", "size": f"{SIZE}x{SIZE}"}],
        "info": {"author": "xcode", "version": 1},
    })


def main() -> int:
    write_json(CATALOG / "Contents.json", {"info": {"author": "xcode", "version": 1}})
    icon(ICON, android.DARK_BG, android.MARK)
    icon(ICON_DEV, android.LIGHT_BG, android.MARK_LIGHT)
    print(f"ios icons from {android.MARK.name}: {ICON.relative_to(HERE).as_posix()} and {ICON_DEV.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
