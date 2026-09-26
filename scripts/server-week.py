#!/usr/bin/env python3
"""The server's week, read-only, NOTES-FROM-PLANNING.md entry 222 sections 4.3 and 6.5. Run by the weekly scheduled task.

One ssh command, with the key passed by path and never read here, runs a short read-only script on the server: from the GroupLab workers'
own logs, how many things each deleted or archived in the last seven days, and the newest HestiaCP backup file with its date. The result
goes into C:\\Dev\\grouplab-local\\automation.json, where the weekly line in for-alan.md and the ledger read it. The server's address is
never printed or written: only the host name the pull script already uses.

    python scripts/server-week.py
    python scripts/server-week.py --print   print it and write nothing

The Oracle boot volume backups of request 35 step 3 cannot be seen from the server; until something here can read them, the line says to
look in the console.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
STATUS = Path(os.environ.get("GROUPLAB_LOCAL", REPO.parent / "grouplab-local")) / "automation.json"
KEY = os.environ.get("GROUPLAB_SSH_KEY", r"C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key")
TARGET = "ubuntu@ssh.pissinhot.com"

# Runs on the server as root, reads only, prints one line of JSON.
REMOTE = r"""
import glob, json, os, re, time
week = time.time() - 7 * 86400
out = {"workers": {}, "backups": []}
for path in sorted(glob.glob('/home/airwolf/logs/grouplab-*worker.log')):
    name = os.path.basename(path)[len('grouplab-'):-len('-worker.log')]
    n = 0
    for line in open(path, encoding='utf-8', errors='replace'):
        m = re.match(r'(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)', line)
        if m and time.mktime(time.strptime(m.group(1), '%Y-%m-%dT%H:%M:%SZ')) >= week and re.search(r'deleted|removed from the server|archived', line):
            n += 1
    out["workers"][name] = n
for path in glob.glob('/backup/*.tar'):
    out["backups"].append({"user": os.path.basename(path).split('.')[0], "day": time.strftime('%Y-%m-%d', time.gmtime(os.path.getmtime(path))), "megabytes": round(os.path.getsize(path) / 1e6)})
print(json.dumps(out))
"""


def words(week: dict) -> str:
    done = ", ".join(f"{name} {n}" for name, n in week["workers"].items() if n) or "nothing"
    newest = max((b["day"] for b in week["backups"]), default=None)
    age = (datetime.now(timezone.utc).date() - datetime.fromisoformat(newest).date()).days if newest else None
    backup = (f"the server's own backup is from {newest}" + (" (**over two days old**)" if age is not None and age > 2 else "")) if newest \
        else "**the server has no backup file of its own**"
    return (f"on the server, workers deleted or archived: {done}; {backup}; the Oracle boot volume backups are not seen by this report: "
            "Alan can check them in the Oracle console, under Boot Volume Backups, whenever he wants")


def main() -> int:
    parser = argparse.ArgumentParser(description="The server's week, read-only (entry 222).")
    parser.add_argument("--print", action="store_true", help="print it and write nothing")
    args = parser.parse_args()
    run = subprocess.run(["ssh", "-i", KEY, "-o", "BatchMode=yes", "-o", "LogLevel=ERROR", TARGET, "sudo python3 -"],
                         input=REMOTE, capture_output=True, text=True, timeout=120)
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    if run.returncode != 0:
        result = {"at": now, "ok": False, "words": "**the server could not be read this week**"}
    else:
        week = json.loads(run.stdout.strip().splitlines()[-1])
        result = {"at": now, "ok": True, "workers": week["workers"], "backups": week["backups"], "words": words(week)}
    print(result["words"])
    if not args.print:
        STATUS.parent.mkdir(parents=True, exist_ok=True)
        status = json.loads(STATUS.read_text(encoding="utf-8")) if STATUS.is_file() else {}
        status["server"] = result
        STATUS.write_text(json.dumps(status, indent=1) + "\n", encoding="utf-8", newline="\n")
    return 0 if result["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())
