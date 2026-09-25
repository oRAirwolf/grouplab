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
from datetime import datetime
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
STATUS = Path(os.environ.get("GROUPLAB_LOCAL", REPO.parent / "grouplab-local")) / "automation.json"
FOR_ALAN = REPO / "docs" / "notes" / "for-alan.md"
START = "<!-- automation-week: written by scripts/automation-report.py each week; not a request -->"
END = "<!-- /automation-week -->"


def day(stamp: str | None) -> str:
    return datetime.fromisoformat(stamp.replace("Z", "+00:00")).strftime("%d %B").lstrip("0") if stamp else "never"


def line(s: dict) -> str:
    parts = []
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


def main() -> int:
    parser = argparse.ArgumentParser(description="The weekly automation line (entry 222).")
    parser.add_argument("--print", action="store_true", help="print it and write nothing")
    args = parser.parse_args()
    s = json.loads(STATUS.read_text(encoding="utf-8")) if STATUS.is_file() else {}
    words = line(s)
    if args.print:
        print(words)
        return 0
    raw = FOR_ALAN.read_bytes().decode("utf-8")
    crlf = "\r\n" in raw
    text = raw.replace("\r\n", "\n")
    block = f"{START}\n{words}\n{END}"
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
