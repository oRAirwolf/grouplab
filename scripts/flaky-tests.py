#!/usr/bin/env python3
"""Gathers the tests that passed only on their retry into docs/notes/flaky-tests.md. NOTES-FROM-PLANNING.md entry 450 section 1.

    python scripts/flaky-tests.py            rewrite docs/notes/flaky-tests.md from CI and say what is to be fixed
    python scripts/flaky-tests.py --check    say what would change, and write nothing

ci.yml retries a failed test once (scripts/test-retry.py). A test that passed only on its retry is written by that leg
into a small artifact, flaky-tests-<leg>, kept thirty days. CI cannot commit, so this is run by Claude Code at the
start of a run, and the planning session reads the file it writes: newest first, with the test, the leg, the run and
the date. Rows already in the file are kept for sixty days, so nothing is lost when an artifact expires.

A test that needed its retry three times in a fortnight is listed at the top under "To fix", and STATE.md carries it
as an item of its own until it is fixed. Nothing here reads anything but the repository's own artifacts.
"""

from __future__ import annotations

import datetime
import io
import json
import subprocess
import sys
import zipfile
from pathlib import Path

REPO = "oRAirwolf/grouplab"
LEGS = ("windows-latest", "ubuntu-latest", "macos-latest")
FILE = Path(__file__).resolve().parent.parent / "docs" / "notes" / "flaky-tests.md"
KEEP_DAYS = 60
FORTNIGHT = 14
TIMES = 3
HEADER = "| Date | Test | Leg | Run |"


def gh(*args: str) -> bytes:
    return subprocess.run(["gh", *args], capture_output=True, check=True).stdout


def from_ci() -> set[tuple[str, str, str, str]]:
    """Every row in every unexpired flaky-tests artifact, as (date, test, leg, run)."""
    rows = set()
    for leg in LEGS:
        listing = json.loads(gh("api", f"repos/{REPO}/actions/artifacts?name=flaky-tests-{leg}&per_page=100"))
        for artifact in listing.get("artifacts", []):
            if artifact.get("expired"):
                continue
            with zipfile.ZipFile(io.BytesIO(gh("api", f"repos/{REPO}/actions/artifacts/{artifact['id']}/zip"))) as z:
                for name in z.namelist():
                    for line in z.read(name).decode("utf-8").splitlines():
                        parts = line.split("\t")
                        if len(parts) == 4:
                            date, row_leg, run, test = parts
                            rows.add((date, test, row_leg, run))
    return rows


def from_file() -> set[tuple[str, str, str, str]]:
    rows = set()
    if FILE.exists():
        for line in FILE.read_text(encoding="utf-8").splitlines():
            cells = [c.strip().strip("`") for c in line.strip().strip("|").split("|")]
            if len(cells) == 4 and cells[0][:2] == "20":
                run = cells[3].split("]")[0].lstrip("[")
                rows.add((cells[0], cells[1], cells[2], run))
    return rows


def render(rows: set[tuple[str, str, str, str]], today: datetime.date) -> str:
    since = (today - datetime.timedelta(days=FORTNIGHT)).isoformat()
    recent: dict[str, int] = {}
    for date, test, _, _ in rows:
        if date >= since:
            recent[test] = recent.get(test, 0) + 1
    to_fix = sorted(t for t, n in recent.items() if n >= TIMES)
    out = [
        "# Tests that passed only on their retry",
        "",
        "Written by `scripts/flaky-tests.py` from CI's `flaky-tests-<leg>` artifacts (NOTES-FROM-PLANNING.md entry 450",
        "section 1). ci.yml runs a failed test once more in the same job, and a test that passes the second time lets the",
        "leg pass; each such pass is listed here, newest first, for sixty days. A test that needed its retry three times in",
        "a fortnight is under To fix, and STATE.md carries it as an item of its own.",
        "",
        "## To fix",
        "",
    ]
    out += [f"- `{t}`: {recent[t]} retries in the fortnight to {today.isoformat()}" for t in to_fix] or ["None."]
    out += ["", "## Every retry that passed", "", HEADER, "|---|---|---|---|"]
    for date, test, leg, run in sorted(rows, key=lambda r: (r[0], r[3], r[1]), reverse=True):
        out.append(f"| {date} | `{test}` | {leg} | [{run}](https://github.com/{REPO}/actions/runs/{run}) |")
    if not rows:
        out.append("| none yet | | | |")
    return "\n".join(out) + "\n"


def main(argv: list[str]) -> int:
    today = datetime.datetime.now(datetime.timezone.utc).date()
    cutoff = (today - datetime.timedelta(days=KEEP_DAYS)).isoformat()
    rows = {r for r in from_file() | from_ci() if r[0] >= cutoff}
    text = render(rows, today)
    old = FILE.read_text(encoding="utf-8") if FILE.exists() else ""
    to_fix = text.split("## To fix", 1)[1].split("##", 1)[0].strip()
    print(f"{len(rows)} retries that passed in the last {KEEP_DAYS} days; to fix: {to_fix}")
    if "--check" in argv:
        print("unchanged" if text == old else "docs/notes/flaky-tests.md would change")
        return 0
    if text != old:
        with FILE.open("w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print("docs/notes/flaky-tests.md rewritten")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
