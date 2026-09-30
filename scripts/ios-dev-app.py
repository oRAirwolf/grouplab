#!/usr/bin/env python3
"""GroupLab Dev for iPhone and iPad: its Info.plist and entitlements, made from GroupLab's own so the two never drift apart.

NOTES-FROM-PLANNING.md entry 315, amendment section 2. GroupLab Dev (org.grouplab.app.dev, "GroupLab Dev" on the home screen, its own icon)
is the same sources built with -p:GroupLabDev=true, which compiles in the developer tools the public application leaves out. It installs
beside GroupLab, so everything two applications cannot share is its own:

- the name under the icon, GroupLab Dev, and the share extension's name in the share sheet;
- the address the share extension opens it with, grouplab-dev://shared, so a share into GroupLab Dev never opens GroupLab;
- the app group, group.org.grouplab.app.dev, and the share extension's id, org.grouplab.app.dev.share;
- the GroupLab data file type is imported rather than declared, and opened as an alternate, since only GroupLab owns it.

Each project reads its .dev file only in GroupLab Dev (GroupLab.iOS.csproj and GroupLab.Share.csproj). Run after changing any of the four
public files, and commit what it writes; the Core tests fail while a .dev file is out of step.

    python scripts/ios-dev-app.py            writes the four .dev files
    python scripts/ios-dev-app.py --check    exit 1, naming the file, where one differs from what would be written
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
APP = ROOT / "ios" / "GroupLab.iOS"
SHARE = ROOT / "ios" / "GroupLab.Share"
NOTE = "<!-- GroupLab Dev's, written by scripts/ios-dev-app.py from {name}; change that file and run the script, never this one. -->\n"


def stamped(text: str, name: str) -> str:
    """The generated file with its note after the XML declaration and doctype."""
    head, _, rest = text.partition("<plist")
    return head + NOTE.format(name=name) + "<plist" + rest


def app_info(text: str) -> str:
    text = re.sub(r"(<key>CFBundleDisplayName</key>\s*<string>)GroupLab(</string>)", r"\1GroupLab Dev\2", text, count=1)
    text = text.replace("<string>org.grouplab.app.shared</string>", "<string>org.grouplab.app.dev.shared</string>")
    text, schemes = re.subn(r"(<key>CFBundleURLSchemes</key>\s*<array>\s*<string>)grouplab(</string>)", r"\1grouplab-dev\2", text)
    if schemes != 1:
        raise SystemExit("ios-dev-app: GroupLab's Info.plist has no grouplab address scheme to change")
    text = text.replace("<key>UTExportedTypeDeclarations</key>", "<key>UTImportedTypeDeclarations</key>")
    text = text.replace("<key>LSHandlerRank</key>\n\t\t\t<string>Owner</string>", "<key>LSHandlerRank</key>\n\t\t\t<string>Alternate</string>")
    return stamped(text, "ios/GroupLab.iOS/Info.plist")


def share_info(text: str) -> str:
    text = re.sub(r"(<key>CFBundleDisplayName</key>\s*<string>)GroupLab(</string>)", r"\1GroupLab Dev\2", text, count=1)
    return stamped(text, "ios/GroupLab.Share/Info.plist")


def entitlements(text: str, name: str) -> str:
    if "<string>group.org.grouplab.app</string>" not in text:
        raise SystemExit(f"ios-dev-app: {name} has no app group to change")
    return stamped(text.replace("<string>group.org.grouplab.app</string>", "<string>group.org.grouplab.app.dev</string>"), name)


def made() -> dict[Path, str]:
    def read(path: Path) -> str:
        return path.read_bytes().decode("utf-8").replace("\r\n", "\n")

    return {
        APP / "Info.dev.plist": app_info(read(APP / "Info.plist")),
        APP / "Entitlements.dev.plist": entitlements(read(APP / "Entitlements.plist"), "ios/GroupLab.iOS/Entitlements.plist"),
        SHARE / "Info.dev.plist": share_info(read(SHARE / "Info.plist")),
        SHARE / "Entitlements.dev.plist": entitlements(read(SHARE / "Entitlements.plist"), "ios/GroupLab.Share/Entitlements.plist"),
    }


def main(argv: list[str]) -> int:
    files = made()
    if argv[1:2] == ["--check"]:
        stale = [p for p, text in files.items() if not p.is_file() or p.read_bytes().decode("utf-8").replace("\r\n", "\n") != text]
        for path in stale:
            print(f"{path.relative_to(ROOT).as_posix()} is out of step with GroupLab's own; run python scripts/ios-dev-app.py")
        if not stale:
            print("ios-dev-app: GroupLab Dev's four files match GroupLab's")
        return 1 if stale else 0
    for path, text in files.items():
        path.write_bytes(text.encode("utf-8"))
    print("ios-dev-app: wrote " + ", ".join(p.relative_to(ROOT).as_posix() for p in files))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
