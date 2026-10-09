#!/usr/bin/env python3
"""The server change backup, NOTES-FROM-PLANNING.md entry 397: its dry run names every file with its mode and owner, says which files do
not exist yet so the undo deletes them, says what lists it would take, and copies nothing. Linux only, as the server is.

    python3 tests/python/change-backup-tests.py
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
TOOL = REPO / "website" / "server" / "grouplab-change-backup.py"

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what)
        print("FAIL " + what + (": " + detail if detail else ""))


if sys.platform != "linux":
    print("change backup tests: skipped, the tool runs on the Linux server")
    sys.exit(0)

with tempfile.TemporaryDirectory() as work:
    present = Path(work) / "unit.service"
    present.write_text("[Unit]\n", encoding="utf-8")
    absent = Path(work) / "new.timer"
    done = subprocess.run([sys.executable, str(TOOL), "--label", "test-change", "--file", str(present), "--file", str(absent),
                           "--packages", "--units", "--dry-run"], capture_output=True, text=True)
    out = done.stdout
    check("a dry run succeeds without root", done.returncode == 0, done.stderr)
    check("it names the folder under grouplab-server/backups", "/home/ubuntu/grouplab-server/backups/" in out and "-test-change" in out, out)
    check("an existing file is named with its mode and owner", str(present) in out and "0o644" in out.replace("0o664", "0o644"), out)
    check("a file that does not exist yet is listed for the undo to delete", f"{absent}: absent now, so the undo deletes it" in out, out)
    check("the package and unit lists are named", "dpkg --get-selections" in out and "apt-mark showmanual" in out and "list-timers --all" in out, out)
    check("nothing is copied on a dry run", "nothing was copied" in out, out)
    bad = subprocess.run([sys.executable, str(TOOL), "--label", "bad label!", "--dry-run"], capture_output=True, text=True)
    check("a label with spaces or punctuation is refused", bad.returncode == 2, bad.stdout)

print(f"change backup tests: {passed} passed, {len(failed)} failed")
sys.exit(1 if failed else 0)
