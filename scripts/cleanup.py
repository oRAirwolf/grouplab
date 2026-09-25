#!/usr/bin/env python3
"""The weekly cleanup, with its safety net, NOTES-FROM-PLANNING.md entry 222 section 4. Run by the weekly scheduled task.

    python scripts/cleanup.py             clean
    python scripts/cleanup.py --dry-run   say what would go, and change nothing

**Only what it knows is generated**, and nothing outside these places:

- **Deleted directly, because it rebuilds itself:** the numbered build folders `bin\\alt2` onwards in this repository's projects (entry 132
  keeps `bin\\alt` and no others), test run folders under `bin\\alt\\t*`, and the test suites' leftovers in `%TEMP%` named `grouplab-*`,
  each over a day old.
- **Moved to the trash first**, because it does not rebuild itself: the desktop's debug logs in `out\\logs` over thirty days old. The trash
  is `C:\\Dev\\grouplab-trash\\<date>\\`; a day's folder is emptied after fourteen days, and **never before a nightly backup has succeeded
  since that day**, which `C:\\Dev\\grouplab-local\\automation.json` records.

It never touches Alan's own files, `C:\\Dev\\grouplab-site`, `C:\\Dev\\keys`, Downloads, or the submissions and originals folders.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
import tempfile
import time
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
DEV = REPO.parent
TRASH = Path(os.environ.get("GROUPLAB_TRASH", DEV / "grouplab-trash"))
STATUS = Path(os.environ.get("GROUPLAB_LOCAL", DEV / "grouplab-local")) / "automation.json"
TEMP = Path(os.environ.get("GROUPLAB_CLEANUP_TEMP", tempfile.gettempdir()))
DAY = 86400
LOG_DAYS = 30
TRASH_DAYS = 14
NEVER = [DEV / "keys", DEV / "grouplab-site", DEV / "grouplab-submissions", DEV / "grouplab-originals", Path.home() / "Downloads"]


def size(path: Path) -> int:
    return path.stat().st_size if path.is_file() else sum(f.stat().st_size for f in path.rglob("*") if f.is_file())


def age(path: Path) -> float:
    return time.time() - path.stat().st_mtime


def allowed(path: Path) -> bool:
    resolved = path.resolve()
    return not any(resolved == n or n in resolved.parents for n in [p.resolve() for p in NEVER])


def regenerable() -> list[Path]:
    """What rebuilds itself and is over a day old."""
    found = []
    for bin_folder in list((REPO / "src").glob("*/bin")) + list((REPO / "tests").glob("*/bin")):
        found += [p for p in bin_folder.iterdir() if p.is_dir() and re.fullmatch(r"alt\d+", p.name) and age(p) > DAY]
        alt = bin_folder / "alt"
        if alt.is_dir():
            found += [p for p in alt.iterdir() if p.is_dir() and re.fullmatch(r"t\d*", p.name) and age(p) > DAY]
    if TEMP.is_dir():
        found += [p for p in TEMP.iterdir() if p.name.startswith("grouplab-") and age(p) > DAY]
    return [p for p in found if allowed(p)]


def to_trash() -> list[Path]:
    """What does not rebuild itself and is old enough to go: the desktop's debug logs over thirty days old."""
    logs = REPO / "out" / "logs"
    return [p for p in logs.glob("*") if p.is_file() and age(p) > LOG_DAYS * DAY] if logs.is_dir() else []


def last_backup() -> datetime | None:
    try:
        b = json.loads(STATUS.read_text(encoding="utf-8")).get("backup", {})
        return datetime.fromisoformat(b["at"].replace("Z", "+00:00")) if b.get("ok") else None
    except (OSError, ValueError, KeyError):
        return None


def emptiable(backed: datetime | None, today: date) -> list[Path]:
    """Trash days over fourteen days old, and only those a successful backup has run after."""
    if not TRASH.is_dir() or backed is None:
        return []
    out = []
    for day in TRASH.iterdir():
        try:
            d = date.fromisoformat(day.name)
        except ValueError:
            continue
        if (today - d).days > TRASH_DAYS and backed.date() > d:
            out.append(day)
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description="The weekly cleanup (entry 222 section 4).")
    parser.add_argument("--dry-run", action="store_true", help="say what would go and change nothing")
    args = parser.parse_args()
    today = date.today()
    deleted = regenerable()
    trashed = to_trash()
    emptied = emptiable(last_backup(), today)
    freed = sum(size(p) for p in deleted)
    verb = "would" if args.dry_run else "did"
    for p in deleted:
        print(f"{verb} delete {p} ({size(p) / 1e6:.1f} MB)")
        if not args.dry_run:
            shutil.rmtree(p, ignore_errors=True) if p.is_dir() else p.unlink(missing_ok=True)
    for p in trashed:
        print(f"{verb} move to the trash {p}")
        if not args.dry_run:
            into = TRASH / today.isoformat() / p.relative_to(DEV)
            into.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(str(p), str(into))
    for p in emptied:
        print(f"{verb} empty the trash of {p.name}")
        if not args.dry_run:
            shutil.rmtree(p, ignore_errors=True)
    summary = {"at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"), "ok": True, "deleted": len(deleted),
               "freedMegabytes": round(freed / 1e6, 1), "trashed": len(trashed), "trashEmptied": [p.name for p in emptied]}
    print(f"cleanup: {len(deleted)} deleted, {freed / 1e6:.1f} MB; {len(trashed)} to the trash; {len(emptied)} trash days emptied"
          + (" (dry run, nothing changed)" if args.dry_run else ""))
    if not args.dry_run:
        STATUS.parent.mkdir(parents=True, exist_ok=True)
        now = json.loads(STATUS.read_text(encoding="utf-8")) if STATUS.is_file() else {}
        now["cleanup"] = summary
        STATUS.write_text(json.dumps(now, indent=1) + "\n", encoding="utf-8", newline="\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
