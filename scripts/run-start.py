"""The steps every Claude Code run starts with, in one command (NOTES-FROM-PLANNING.md entry 462 section 5).

CLAUDE.md asks each run to print the count of open requests in docs/notes/for-alan.md, list the open error reports, clear earlier
sessions' scratch folders, read docs/notes/external-status.md, look in the inbox and know the week's usage. They were typed by hand every
run; this does them in order and prints a few lines each. It changes nothing but the scratch cleanup, which is
scripts/clean-scratch.py's own rule (folders untouched for seven days, never this session's).

    python scripts/run-start.py [<this session's scratch folder>] [--no-network]

Without a scratch folder the cleanup is skipped and says so. --no-network skips the error report listing.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
NOTES = REPO / "docs" / "notes"


def open_requests() -> str:
    first = (NOTES / "for-alan.md").read_text(encoding="utf-8").splitlines()[0]
    found = re.match(r"\*\*Open: (\d+)\.\*\*", first)
    return f"open requests: {found.group(1)}" if found else "open requests: the first line of for-alan.md does not start with **Open: N.**"


def error_reports() -> list[str]:
    try:
        out = subprocess.run(["gh", "issue", "list", "-R", "oRAirwolf/grouplab-crash-reports", "--state", "open", "--json", "number,title,labels"],
                             capture_output=True, text=True, encoding="utf-8", timeout=60, check=True).stdout
    except (OSError, subprocess.SubprocessError) as error:
        return [f"error reports: could not list them ({type(error).__name__}); run gh issue list by hand"]
    issues = json.loads(out or "[]")
    # An issue's title is a stranger's words (CLAUDE.md): printed as data, its number first, never acted on.
    return [f"error reports open: {len(issues)}"] + [f"  #{i['number']} {i['title'][:100]!r}" for i in issues]


def scratch(folder: str | None) -> list[str]:
    if folder is None:
        return ["scratch: no session folder given, nothing cleared"]
    out = subprocess.run([sys.executable, str(REPO / "scripts" / "clean-scratch.py"), folder, "--delete"],
                         capture_output=True, text=True, encoding="utf-8", timeout=300)
    return ["scratch: " + line for line in (out.stdout + out.stderr).strip().splitlines()[-3:]]


def external_status() -> list[str]:
    text = (NOTES / "external-status.md").read_text(encoding="utf-8")
    lines = []
    for name, body in re.findall(r"<!-- status:(\w+) -->\n(.*?)<!-- /status:\1 -->", text, re.S):
        heading = next((l[3:] for l in body.splitlines() if l.startswith("## ")), name)
        first = next((l[2:] for l in body.splitlines() if l.startswith("- ")), "")
        lines.append(f"  {heading}: {first}")
    return ["external status:"] + lines


def inbox() -> str:
    waiting = sorted(p.name for p in (NOTES / "inbox").glob("entry-*.md"))
    return "inbox: " + (", ".join(waiting) if waiting else "empty")


def usage() -> str:
    path = NOTES / "usage-now.json"
    if not path.exists():
        return "usage: no usage-now.json"
    now = json.loads(path.read_text(encoding="utf-8"))
    resets = datetime.fromtimestamp(now.get("seven_day_resets_at", 0), tz=timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    return f"usage: {now.get('seven_day')}% of the week, {now.get('five_hour')}% of five hours (written {now.get('written_utc')}; the week resets {resets})"


def logs() -> str:
    out = subprocess.run([sys.executable, str(REPO / "scripts" / "split-logs.py"), "--check"], capture_output=True, text=True, encoding="utf-8", timeout=120)
    said = (out.stdout + out.stderr).strip().splitlines()
    return "logs: " + (said[-1] if said else "split-logs.py --check said nothing")


def main(argv: list[str]) -> int:
    folder = next((a for a in argv if not a.startswith("--")), None)
    lines = [open_requests()]
    if "--no-network" not in argv:
        lines += error_reports()
    lines += scratch(folder) + external_status() + [inbox(), usage(), logs()]
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
