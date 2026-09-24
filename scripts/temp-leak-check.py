#!/usr/bin/env python3
"""Fails when the test suite leaves anything in the temporary directory. NOTES-FROM-PLANNING.md entry 179.

    python3 scripts/temp-leak-check.py snapshot     before the suite: what is there now
    python3 scripts/temp-leak-check.py check        after it: fails on anything new that the suite made

A suite that leaks files is broken in the same way a test that leaks memory is. Alan's temporary folder held
14,987 settings files and thousands of empty folders from test runs. The tests now write into one folder per
run, grouplab-tests/<run>, removed when the run ends (tests/Shared/TestTempRoot.cs); this is the check from
outside that it worked.

What counts as the suite's: anything named grouplab-*, the grouplab-tests folder holding a run that did not
remove itself, and randomly named folders of the eight dot three shape that a temporary directory API makes.
Other programs on the machine write there too, so nothing else is judged.
"""

from __future__ import annotations

import json
import re
import sys
import tempfile
from pathlib import Path

TEMP = Path(tempfile.gettempdir())
SNAPSHOT = TEMP / "grouplab-leak-check-snapshot.json"
RANDOM = re.compile(r"^[a-z0-9]{8}\.[a-z0-9]{3}$")


def ours(name: str) -> bool:
    return name.startswith("grouplab-") or RANDOM.match(name) is not None or name == "grouplab-tests"


def listing() -> dict[str, list[str]]:
    top = sorted(p.name for p in TEMP.iterdir() if ours(p.name) and p.name != SNAPSHOT.name)
    runs = TEMP / "grouplab-tests"
    inside = sorted(p.name for p in runs.iterdir()) if runs.is_dir() else []
    return {"top": top, "runs": inside}


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in ("snapshot", "check"):
        print(__doc__)
        return 2

    if sys.argv[1] == "snapshot":
        SNAPSHOT.write_text(json.dumps(listing()), encoding="utf-8")
        print(f"snapshot of {TEMP}: {len(listing()['top'])} entries of the suite's shape already there")
        return 0

    before = json.loads(SNAPSHOT.read_text(encoding="utf-8")) if SNAPSHOT.exists() else {"top": [], "runs": []}
    now = listing()
    new_top = [n for n in now["top"] if n not in before["top"] and n != "grouplab-tests"]
    new_runs = [n for n in now["runs"] if n not in before["runs"]]
    SNAPSHOT.unlink(missing_ok=True)
    if not new_top and not new_runs:
        print("the test suite left nothing in the temporary directory")
        return 0

    for n in new_top:
        print(f"LEFT BEHIND: {TEMP / n}")
    for n in new_runs:
        print(f"LEFT BEHIND: a run folder that did not remove itself, {TEMP / 'grouplab-tests' / n}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
