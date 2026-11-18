#!/usr/bin/env python3
"""The weekly line in docs/notes/for-alan.md, NOTES-FROM-PLANNING.md entry 222 sections 4.4 and 6.5: what was backed up, cleaned and
archived that week, whether the restore test passed, and anything that failed, in plain words. Not a request: it sits between two markers
under the count of open requests and is rewritten each week from C:\\Dev\\grouplab-local\\automation.json.

    python scripts/automation-report.py           rewrite the line
    python scripts/automation-report.py --print   print it and write nothing
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
STATUS = Path(os.environ.get("GROUPLAB_LOCAL", REPO.parent / "grouplab-local")) / "automation.json"
LOG = STATUS.with_name("automation.log")
FOR_ALAN = REPO / "docs" / "notes" / "for-alan.md"
START = "<!-- automation-week: written by scripts/automation-report.py each week; not a request -->"
END = "<!-- /automation-week -->"


def day(stamp: str | None) -> str:
    return datetime.fromisoformat(stamp.replace("Z", "+00:00")).strftime("%d %B").lstrip("0") if stamp else "never"


def silent_nights(log_text: str, today: date, days: int = 7) -> list[date]:
    """Entry 489 section 1: the nights of the past week whose nightly backup wrote no result line, the way 3 and 10 November did."""
    ended = {d for d in (today - timedelta(days=n) for n in range(1, days + 1)) if f"{d.isoformat()} " in log_text
             and re.search(rf"^{d.isoformat()} \d\d:\d\d Nightly, backup, exit", log_text, re.M)}
    return [today - timedelta(days=n) for n in range(days, 0, -1) if today - timedelta(days=n) not in ended]


def line(s: dict, log_text: str | None = None, today: date | None = None) -> str:
    parts = []
    if log_text is not None:
        silent = silent_nights(log_text, today or date.today())
        if silent:
            names = ", ".join(d.strftime("%d %B").lstrip("0") for d in silent)
            parts.append(f"**no backup line on {names}**: the nightly backup did not finish {'that night' if len(silent) == 1 else 'those nights'}")
    b = s.get("backup")
    if not b:
        parts.append("no nightly backup has run yet")
    elif not b.get("ok"):
        parts.append(f"**the backup failed on {day(b.get('at'))}**: {b.get('error', 'no reason recorded')}")
    elif b.get("uploaded"):
        parts.append(f"backed up on {day(b.get('at'))} ({b.get('bytes', 0) / 1e6:.0f} MB, {b.get('tag')})")
    else:
        parts.append(f"backed up on {day(b.get('at'))}, kept on this computer only until the backups repository exists (request 35)")
    r = s.get("restoreTest")
    if not r:
        parts.append("no restore test yet")
    elif r.get("ok"):
        parts.append(f"the restore test passed on {day(r.get('at'))}")
    else:
        parts.append(f"**the restore test failed on {day(r.get('at'))}**: {r.get('error', '')}")
    a = s.get("archiveCopy")
    if a:
        parts.append(f"{a.get('copied', 0)} archived submissions copied here" + (f", **{len(a['problems'])} problems**" if a.get("problems") else ""))
    c = s.get("cleanup")
    if c:
        parts.append(f"cleanup freed {c.get('freedMegabytes', 0):.0f} MB" + (f" and put {c['trashed']} files in the trash" if c.get("trashed") else ""))
    w = s.get("server")
    if w:
        parts.append(w.get("words", ""))
    return "**This week, by itself** (not a request): " + "; ".join(p for p in parts if p) + "."


FAILED = "**The nightly backup failed on"


#: Entry 499 section 0: a nightly backup older than this, in hours, means a night that never started.
STALE_HOURS = 26


def failure(s: dict, now: datetime | None = None) -> str | None:
    """Entry of 17 November 2026: a failed backup or restore test puts its own line in for-alan.md the same day, above the week's line,
    instead of waiting for Sunday. Two nights failed unseen because their error reports landed on a closed issue. Entry 499 section 0: a
    night that never started writes nothing at all (18 November: Windows Update restarted the computer and nobody signed in, and the task
    runs only while somebody is), so a newest backup more than STALE_HOURS old says so too; Code runs this at the start of every run."""
    b, r = s.get("backup") or {}, s.get("restoreTest") or {}
    if b and not b.get("ok"):
        return f"{FAILED} {day(b.get('at'))}** (not a request; Code fixes it at the start of its next run): {b.get('error', 'no reason recorded')}"
    if b.get("at") and (now or datetime.now(timezone.utc)) - datetime.fromisoformat(b["at"].replace("Z", "+00:00")) > timedelta(hours=STALE_HOURS):
        return (f"**No nightly backup since {day(b.get('at'))}** (not a request; Code runs it by hand at the start of its next run): the "
                "nightly did not start, most likely because the computer was off, asleep or signed out at 02:30; signing in after a restart "
                "lets the next night run.")
    if r and not r.get("ok"):
        return f"**The weekly restore test failed on {day(r.get('at'))}** (not a request; Code fixes it at the start of its next run): {r.get('error', '')}"
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description="The weekly automation line (entry 222).")
    parser.add_argument("--print", action="store_true", help="print it and write nothing")
    args = parser.parse_args()
    s = json.loads(STATUS.read_text(encoding="utf-8")) if STATUS.is_file() else {}
    words = line(s, LOG.read_text(encoding="utf-8-sig", errors="replace") if LOG.is_file() else None)
    if args.print:
        print(failure(s) or "")
        print(words)
        return 0
    raw = FOR_ALAN.read_bytes().decode("utf-8")
    crlf = "\r\n" in raw
    text = raw.replace("\r\n", "\n")
    failed = failure(s)
    block = f"{START}\n" + (f"{failed}\n" if failed else "") + f"{words}\n{END}"
    if START in text:
        text = re.sub(re.escape(START) + r".*?" + re.escape(END), lambda _: block, text, flags=re.S)
    else:
        # After the paragraph that says how many requests are open.
        m = re.search(r"\*\*Open: \d+\.\*\*[^\n]*(?:\n[^\n]+)*\n", text)
        at = m.end() if m else 0
        text = text[:at] + "\n" + block + "\n" + text[at:]
    FOR_ALAN.write_bytes((text.replace("\n", "\r\n") if crlf else text).encode("utf-8"))
    print(words)
    return 0


if __name__ == "__main__":
    sys.exit(main())
