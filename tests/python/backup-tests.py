#!/usr/bin/env python3
"""The nightly backup's rules that decide what is deleted, NOTES-FROM-PLANNING.md entry 222 sections 3.1 and 3.2.

It checks the retention rule (7 daily, 4 weekly on Sundays, 6 monthly) over a year of nightly backups, and that nothing the backup must
never carry can be chosen: the keys folder, build output and its own earlier copies.

    python3 tests/python/backup-tests.py
"""

from __future__ import annotations

import importlib.util
import sys
from datetime import date, datetime, timedelta, timezone
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

# Entry 489 section 1: a night whose backup never wrote its line is named in the weekly line, and a failed run says which part failed.
spec = importlib.util.spec_from_file_location("automation_report", REPO / "scripts" / "automation-report.py")
report = importlib.util.module_from_spec(spec)
spec.loader.exec_module(report)
nights = [f"2026-11-{d:02d} 09:30 Nightly, Discord attachments, exit 0" for d in range(4, 16)]
nights += [f"2026-11-{d:02d} 09:42 Nightly, backup, exit 0" for d in range(4, 16) if d != 10]
log_text = chr(10).join(nights) + chr(10)
check("a night with no backup line is found", report.silent_nights(log_text, date(2026, 11, 15)) == [date(2026, 11, 10)])
check("a full week has none", report.silent_nights(log_text.replace("2026-11-10 09:30 Nightly, Discord attachments, exit 0",
                                                                     "2026-11-10 09:43 Nightly, backup, exit 1"), date(2026, 11, 15)) == [])
check("the weekly line says it plainly", "no backup line on 10 November" in report.line({}, log_text, date(2026, 11, 15)))
check("a part that fails names itself", str(backup.PartFailed("upload", "gh release upload did not answer")).startswith("upload:"))

# 16 and 17 November 2026: the bundle passed 1.9 GB and two nights failed. A file over the part size is cut into parts that join back
# byte for byte; a part that is wrong is refused; and a backup from before the split needs no joining.
import json
import os
import tempfile

with tempfile.TemporaryDirectory(dir=os.environ.get("TMP")) as tmp:
    folder = Path(tmp)
    data = bytes(range(256)) * 41 + b"tail"
    whole = folder / "grouplab.bundle"
    whole.write_bytes(data)
    digest = backup.sha256(whole)
    pieces = backup.split(whole, 4096)
    check("a file over the part size is cut into numbered parts", [p.name for p in pieces] == [f"grouplab.bundle.{n:03d}" for n in (1, 2, 3)],
          str([p.name for p in pieces]))
    check("no part is over the part size, and the whole is gone", all(p.stat().st_size <= 4096 for p in pieces) and not whole.exists())
    manifest = {"joined": [{"name": "grouplab.bundle", "bytes": len(data), "sha256": digest, "parts": [p.name for p in pieces]}]}
    check("the parts join back to the same file", backup.join(folder, manifest) == ["grouplab.bundle"] and whole.read_bytes() == data)
    check("the parts are deleted once joined", not any(p.exists() for p in pieces))
    exact = folder / "exact.bin"
    exact.write_bytes(b"x" * 8192)
    check("a file of exactly two parts makes two parts, not an empty third", len(backup.split(exact, 4096)) == 2)
    pieces = backup.split(whole, 4096)
    pieces[1].write_bytes(b"0" * 4096)
    try:
        backup.join(folder, manifest)
        check("a damaged part is refused", False)
    except RuntimeError as e:
        check("a damaged part is refused", "does not match" in str(e))
    check("a backup from before the split joins nothing", backup.join(folder, {"assets": []}) == [])
    check("the part size is well under what a release asset may be", backup.PART_BYTES <= backup.MOST_ASSET // 1.5)

check("a failed backup puts its own line in for-alan.md", (report.failure({"backup": {"at": "2026-11-17T09:30:00Z", "ok": False, "error": "x"}})
                                                         or "").startswith("**The nightly backup failed on 17 November**"))
check("a failed restore test does too", "restore test failed" in (report.failure({"backup": {"ok": True}, "restoreTest": {"ok": False}}) or ""))
check("a good night has no such line", report.failure({"backup": {"ok": True}, "restoreTest": {"ok": True}}) is None)
# Entry 499 section 0: a night that never started writes nothing, so a backup more than 26 hours old says so the same day.
now = datetime(2026, 11, 18, 16, 0, tzinfo=timezone.utc)
check("a backup 30 hours old is a night that never started", "No nightly backup since 17 November" in (report.failure(
    {"backup": {"at": "2026-11-17T09:30:00Z", "ok": True}, "restoreTest": {"ok": True}}, now) or ""))
check("a backup 7 hours old is not", report.failure({"backup": {"at": "2026-11-18T09:30:00Z", "ok": True}, "restoreTest": {"ok": True}}, now) is None)

print(f"backup tests: {passed} passed, {len(failed)} failed")
for f in failed:
    print("  FAILED  " + f)
sys.exit(0 if not failed else 1)
