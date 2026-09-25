#!/usr/bin/env python3
"""The nightly backup's rules that decide what is deleted, NOTES-FROM-PLANNING.md entry 222 sections 3.1 and 3.2.

It checks the retention rule (7 daily, 4 weekly on Sundays, 6 monthly) over a year of nightly backups, and that nothing the backup must
never carry can be chosen: the keys folder, build output and its own earlier copies.

    python3 tests/python/backup-tests.py
"""

from __future__ import annotations

import importlib.util
import sys
from datetime import date, timedelta
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("backup", REPO / "scripts" / "backup.py")
backup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(backup)

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what + (": " + detail if detail else ""))
        print("FAIL " + what + (": " + detail if detail else ""))


today = date(2026, 9, 25)
nights = [today - timedelta(days=n) for n in range(365)]
tags = [f"backup-{d.isoformat()}" for d in nights]
kept = backup.keep(tags, today)
check("the newest seven nights are kept", all(f"backup-{(today - timedelta(days=n)).isoformat()}" in kept for n in range(7)))
sundays = [d for d in nights if d.weekday() == 6][:4]
check("the newest four Sundays are kept", all(f"backup-{d.isoformat()}" in kept for d in sundays))
firsts = [d for d in nights if d.day == 1][:6]
check("the first of each of the newest six months is kept", all(f"backup-{d.isoformat()}" in kept for d in firsts), str(sorted(kept)))
check("nothing else is kept", len(kept) <= 7 + 4 + 6, str(len(kept)))
check("a year-old backup goes", f"backup-{nights[-1].isoformat()}" not in kept)
check("a release that is not a backup is never counted", backup.keep(tags + ["archive-2026-09", "backup-notadate"], today) == kept)

check("the keys folder is never read", not backup.included(backup.DEV / "keys" / "anything.key"))
check("build output is never read", not backup.included(backup.DEV / "grouplab-local" / "x" / "bin" / "a.dll"))
check("the backup's own earlier copy is never read", not backup.included(backup.LOCAL / "backups" / "local.zip"))
check("an ordinary file is read", backup.included(backup.DEV / "grouplab-local" / "corpus.json"))

print(f"backup tests: {passed} passed, {len(failed)} failed")
for f in failed:
    print("  FAILED  " + f)
sys.exit(0 if not failed else 1)
