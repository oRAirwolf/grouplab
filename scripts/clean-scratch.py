#!/usr/bin/env python3
"""Removes earlier Claude Code sessions' scratch folders on this repository. NOTES-FROM-PLANNING.md entry 179 section 2.5.

    python scripts/clean-scratch.py <current session folder>            say what it would remove
    python scripts/clean-scratch.py <current session folder> --delete   remove it

Run at the start of every run. A session's folder under %LOCALAPPDATA%\\Temp\\claude\\c--Dev-grouplab is removed
when the newest file anywhere in it is more than seven days old. The current session's folder is never
touched, and nothing outside that one parent folder is ever looked at: the parent is worked out from the
current session's folder and must be named c--Dev-grouplab.
"""

from __future__ import annotations

import shutil
import sys
import time
from pathlib import Path

DAYS = 7


def newest(folder: Path) -> float:
    latest = folder.stat().st_mtime
    for p in folder.rglob("*"):
        try:
            latest = max(latest, p.stat().st_mtime)
        except OSError:
            continue
    return latest


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    current = Path(sys.argv[1]).resolve()
    delete = "--delete" in sys.argv[2:]
    parent = current.parent
    if parent.name != "c--Dev-grouplab" or not current.is_dir():
        print(f"{current} is not a session folder under c--Dev-grouplab, so nothing was looked at")
        return 2

    cutoff = time.time() - DAYS * 86400
    removed = kept = 0
    for session in sorted(p for p in parent.iterdir() if p.is_dir() and p.resolve() != current):
        if newest(session) < cutoff:
            size = sum(f.stat().st_size for f in session.rglob("*") if f.is_file()) / 1_048_576
            print(f"{'removing' if delete else 'would remove'} {session.name}, {size:.0f} MB, untouched for more than {DAYS} days")
            if delete:
                shutil.rmtree(session, ignore_errors=True)
            removed += 1
        else:
            kept += 1
    print(f"{removed} {'removed' if delete else 'to remove'}, {kept} newer than {DAYS} days kept, and this session's own folder")
    return 0


if __name__ == "__main__":
    sys.exit(main())
