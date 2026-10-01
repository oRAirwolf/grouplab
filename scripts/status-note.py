#!/usr/bin/env python3
"""The state of Apple's and Microsoft's stores for GroupLab, in one file both sessions read: docs/notes/external-status.md.

NOTES-FROM-PLANNING.md entries 335 and 336. Apple approved the Public Beta and Microsoft listed GroupLab in its Store, and neither reached
for-alan.md, because the TestFlight step and the Store check wrote only into their own run summaries, which nobody reads. Each now pipes
its state lines here: the section named is rewritten between its markers, and "changed" or "same" is printed, so the workflow commits only
when a state moved, as a "[notes] " commit that starts no build.

    python3 scripts/status-note.py --section testflight < lines.txt
    python3 scripts/status-note.py --self-test
"""
from __future__ import annotations

import datetime as _dt
import sys
from pathlib import Path

FILE = Path(__file__).resolve().parent.parent / "docs" / "notes" / "external-status.md"
HEAD = ("# Apple's and Microsoft's stores, as the scheduled checks last saw them\n\n"
        "Written by scripts/status-note.py from the testflight and store status workflows (entries 335 and 336), only when a state changes. "
        "Both sessions read it at the start of a run; anything here that needs Alan goes into for-alan.md the same day.\n")
TITLES = {"testflight": "TestFlight", "store": "The Microsoft Store"}


def update(text: str, section: str, lines: list[str], now: str) -> tuple[str, bool]:
    """The file's text with one section replaced, and whether its state lines changed (the date line alone is not a change)."""
    start, end = f"<!-- status:{section} -->", f"<!-- /status:{section} -->"
    body = "\n".join(f"- {l}" for l in lines if l.strip())
    if not text.strip():
        text = HEAD
    if start in text and end in text:
        before, rest = text.split(start, 1)
        old, after = rest.split(end, 1)
        old_lines = [l for l in old.strip().splitlines() if l.startswith("- ")]
        if old_lines == body.splitlines():
            return text, False
        return before + start + f"\n## {TITLES.get(section, section)}, {now}\n\n{body}\n" + end + after, True
    return text.rstrip("\n") + f"\n\n{start}\n## {TITLES.get(section, section)}, {now}\n\n{body}\n{end}\n", True


def self_test() -> int:
    failures = []
    t, changed = update("", "testflight", ["Both groups have build 148."], "2026-10-01")
    failures += [] if changed and "Both groups have build 148." in t and t.startswith("# Apple") else ["a first section"]
    t2, changed = update(t, "testflight", ["Both groups have build 148."], "2026-10-02")
    failures += [] if not changed and t2 == t else ["the same state is no change"]
    t3, changed = update(t, "store", ["Published in the Store: yes."], "2026-10-01")
    failures += [] if changed and "Both groups have build 148." in t3 and "Published in the Store: yes." in t3 else ["a second section"]
    t4, changed = update(t3, "testflight", ["Both groups have build 149."], "2026-10-02")
    failures += [] if changed and "149" in t4 and "148" not in t4 and "Published in the Store" in t4 else ["a section replaced"]
    for f in failures:
        print("FAILED:", f)
    print("status-note.py self-test: " + ("failed" if failures else "passed"))
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] != ["--section"] or len(argv) < 3:
        print(__doc__)
        return 2
    lines = [l.rstrip() for l in sys.stdin.read().splitlines() if l.strip() and "still being processed" not in l]
    text = FILE.read_text(encoding="utf-8") if FILE.exists() else ""
    new, changed = update(text, argv[2], lines, _dt.datetime.now(_dt.timezone.utc).strftime("%Y-%m-%d %H:%M UTC"))
    if changed:
        FILE.write_bytes(new.encode("utf-8"))
    print("changed" if changed else "same")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
