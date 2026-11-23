#!/usr/bin/env python3
"""Google Play's upload rules, NOTES-FROM-PLANNING.md entry 487 section 1.

A build's Play notes are its own "What you will notice" lines, whole lines only and at most 500 characters; the closed track is sent a
build only when its newest release is completed; and a commit refused because Google is reviewing earlier changes is recognised, so a
run skips rather than cancels that review.

    python3 tests/python/play-upload-tests.py
"""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("play_upload", REPO / "scripts" / "play-upload.py")
play = importlib.util.module_from_spec(spec)
spec.loader.exec_module(play)

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
    else:
        failed.append(f"{what}{': ' + detail if detail else ''}")


NOTES = """# Release notes

## 0.2.0-nightly.9

**2026-11-09**, commit `abc1234`. Nightly.

**What you will notice**

- Correction: nightly 8's notes named a report twice.
- Find holes is faster.
- """ + "A long line " * 60 + """

**Under the hood**

- Something nobody sees.

[Downloads for this build](https://example.invalid)

---

## 0.2.0-nightly.8

**What you will notice**

- Older.
"""

lines = play.noticed("0.2.0-nightly.9", NOTES)
check("the noticed lines are the build's own", len(lines) == 3 and lines[1] == "Find holes is faster.", repr(lines))
check("an unknown build has none", play.noticed("0.2.0-nightly.77", NOTES) == [])
text = play.notes("0.2.0-nightly.9", NOTES)
check("the notes fit Play's 500 characters", len(text) <= 500, str(len(text)))
check("a line that does not fit is left out whole", "A long line" not in text, text)
check("the notes end with where the rest are", text.endswith(play.MORE), text)
check("a correction comes last", text.index("Find holes") < text.index("Correction"), text)
check("nothing under the hood is in them", "nobody sees" not in text)
check("a build with no notes points to the page", "grouplab.org/releases" in play.notes("0.2.0-nightly.77", NOTES))
only_long = "## 0.2.0-nightly.5\n\n**What you will notice**\n\n- " + "word " * 200 + "\n"
check("a single overlong line is cut, not dropped", len(play.notes("0.2.0-nightly.5", only_long)) <= 500
      and play.notes("0.2.0-nightly.5", only_long).startswith("word"))

ready = play.closed_track_ready
check("a completed newest release is ready", ready({"releases": [{"status": "completed", "versionCodes": ["225"]}]})[0])
check("an empty track is not", not ready({"track": "alpha"})[0])
check("a draft is not", not ready({"releases": [{"status": "completed", "versionCodes": ["224"]},
                                                {"status": "draft", "versionCodes": ["225"]}]})[0])
check("a release in progress is not", not ready({"releases": [{"status": "inProgress", "versionCodes": ["225"]}]})[0])


class Response:
    def __init__(self, code: int, text: str):
        self.status_code, self.text = code, text


check("a review in progress is recognised", play.in_review(Response(400, '{"error": {"status": "FAILED_PRECONDITION", '
                                                                      '"details": [{"reason": "CHANGES_ALREADY_IN_REVIEW"}]}}')))
check("another refusal is not taken for one", not play.in_review(Response(403, "forbidden")))

# Entry 513: production takes the closed track's newest reviewed release, never past an unfinished one, never twice.
closed = {"releases": [{"name": "1.0.0-nightly.241", "versionCodes": ["241"], "status": "completed"},
                       {"name": "1.0.0-nightly.242", "versionCodes": ["242"], "status": "inProgress"}]}
code, why, source = play.production_plan({}, closed)
check("an empty production track takes the closed track's newest completed release", code == "241" and source["name"].endswith("241"), why)
check("production never takes the same build twice", play.production_plan({"releases": [{"versionCodes": ["241"], "status": "completed"}]},
                                                                         closed)[0] is None)
check("an unfinished production release holds it back", play.production_plan({"releases": [{"versionCodes": ["240"], "status": "draft"}]},
                                                                             closed)[0] is None)
check("no completed closed release sends nothing", play.production_plan({}, {"releases": [{"versionCodes": ["9"], "status": "draft"}]})[0] is None)

for f in failed:
    print("FAILED", f)
print(f"play upload: {passed} passed, {len(failed)} failed")
sys.exit(1 if failed else 0)
