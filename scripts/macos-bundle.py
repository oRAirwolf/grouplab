#!/usr/bin/env python3
"""Wrap a published GroupLab build as a macOS .app bundle, NOTES-FROM-PLANNING.md entry 147 section 1.2.

A bare executable runs from a terminal, shows no icon, and behaves like a stranger in the dock. A bundle
is three things on top of the same files: a folder shaped the way macOS expects, an Info.plist saying what
the application is called and which file to run, and an icon. None of it is signing, and none of it makes
the build trusted; it makes the build usable by somebody who has decided to trust it.

    python3 scripts/macos-bundle.py <published directory> <output directory> --version 0.2.0 --arch macos-arm64

**This is the one place the bundle's shape is decided.** It is written out rather than left to a packaging
tool so that what ends up in the download can be read here, and so that the workflow's step is one line.

Nothing here is Mac-specific in the running, so it can be checked on any machine: the bundle is a folder
tree and a plist, and `MacosBundleTests` builds one and reads it back.
"""

from __future__ import annotations

import argparse
import os
import plistlib
import shutil
import stat
import sys
from pathlib import Path

# The executable inside the published output. It is the application's own name, not a launcher script.
EXECUTABLE = "GroupLab.App"

# What Finder shows, and what the dock says. Not "GroupLab.App", which is the file's name and reads as a
# mistake to anybody who has not seen a .NET publish folder.
DISPLAY_NAME = "GroupLab"

BUNDLE_ID = "org.grouplab.GroupLab"

ICON = Path("src/GroupLab.App/Assets/icons/grouplab.icns")

# The oldest macOS this is built against. .NET 10 supports macOS 12 and later, and saying so here is what
# stops the system offering the build to a Mac that cannot run it and then failing without explanation.
MINIMUM_SYSTEM = "12.0"


def plist(version: str, arch: str) -> dict:
    """The Info.plist, as the fields macOS actually reads.

    ``CFBundleShortVersionString`` is what a person sees in Finder's Get Info, and macOS expects it to look
    like a version number: a nightly's ``0.2.0-nightly.84`` does not, so the suffix goes in
    ``CFBundleVersion`` where a longer string is allowed, and the plain part is kept here.
    """
    short = version.split("-")[0] or "0.0.0"
    return {
        "CFBundleName": DISPLAY_NAME,
        "CFBundleDisplayName": DISPLAY_NAME,
        "CFBundleIdentifier": BUNDLE_ID,
        "CFBundleExecutable": EXECUTABLE,
        "CFBundleIconFile": "grouplab.icns",
        "CFBundlePackageType": "APPL",
        "CFBundleShortVersionString": short,
        "CFBundleVersion": version,
        "CFBundleInfoDictionaryVersion": "6.0",
        "LSMinimumSystemVersion": MINIMUM_SYSTEM,

        # It is a window, not a background process, and it is not a document-based application.
        "LSApplicationCategoryType": "public.app-category.utilities",
        "NSHighResolutionCapable": True,
        "NSSupportsAutomaticGraphicsSwitching": True,

        # Said out loud in the bundle itself, because entry 147 section 1.6 asks for the label everywhere the
        # build appears and this is the one place that travels with the file after it is unpacked.
        "GroupLabBuildArchitecture": arch,
        "GroupLabTested": "no: nobody has run this on a real Mac",
    }


def build(published: Path, out: Path, version: str, arch: str, icon: Path) -> Path:
    if not published.is_dir():
        raise SystemExit(f"{published} is not a directory, so there is nothing to wrap")
    if not (published / EXECUTABLE).is_file():
        raise SystemExit(f"{published / EXECUTABLE} is not there, so this is not a published GroupLab build")

    app = out / (DISPLAY_NAME + ".app")
    if app.exists():
        shutil.rmtree(app)

    contents = app / "Contents"
    macos = contents / "MacOS"
    resources = contents / "Resources"
    macos.mkdir(parents=True)
    resources.mkdir(parents=True)

    for item in sorted(published.iterdir()):
        target = macos / item.name
        if item.is_dir():
            shutil.copytree(item, target)
        else:
            shutil.copy2(item, target)

    if icon.is_file():
        shutil.copy2(icon, resources / "grouplab.icns")
    else:
        print(f"note: {icon} is not here, so the bundle has no icon", file=sys.stderr)

    with (contents / "Info.plist").open("wb") as f:
        plistlib.dump(plist(version, arch), f)

    # macOS looks for this and treats a bundle without it as a folder that happens to be named .app.
    (contents / "PkgInfo").write_text("APPL????", encoding="ascii")

    # The executable bit does not survive every copy, and a bundle whose executable is not executable fails
    # with "the application cannot be opened" and nothing else.
    runner = macos / EXECUTABLE
    runner.chmod(runner.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)

    return app


def main() -> int:
    parser = argparse.ArgumentParser(description="Wrap a published GroupLab build as a macOS .app bundle.")
    parser.add_argument("published", type=Path, help="the directory dotnet publish wrote")
    parser.add_argument("out", type=Path, help="where to put GroupLab.app")
    parser.add_argument("--version", required=True)
    parser.add_argument("--arch", required=True, help="macos-arm64 or macos-x64, as the download names it")
    parser.add_argument("--icon", type=Path, default=ICON)
    args = parser.parse_args()

    args.out.mkdir(parents=True, exist_ok=True)
    app = build(args.published, args.out, args.version, args.arch, args.icon)
    files = sum(1 for _ in app.rglob("*") if _.is_file())
    size = sum(f.stat().st_size for f in app.rglob("*") if f.is_file())
    print(f"{app}: {files} files, {size / (1024 * 1024):.0f} MB, {args.arch}, version {args.version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
