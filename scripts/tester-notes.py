"""Tester notes passed on in the Discord, prepared and off until Alan says yes (NOTES-FROM-PLANNING.md entry 462 section 5, request 107).

docs/notes/for-alan.md holds paragraphs headed **For <name>** that Alan passes on to a tester by hand once the fix is out: a thank you and
what changed. This finds each one whose fix is in a published nightly, and posts it once through a Discord webhook, opening with
"Hey <name>," as Alan would. Ready means the paragraph names a nightly no newer than the newest in docs/RELEASE-NOTES.md, or names no
nightly and its entry or issue number appears in a published build's notes.

**Off until Alan says yes:** without DISCORD_TESTERS_WEBHOOK in the environment nothing is posted; every message it would post is printed
instead, so the nightly's summary shows the drafts. .github/tester-notes-posted.txt records each paragraph posted (a hash of its words),
so a rerun never posts one twice. Nobody is pinged. The webhook address is never printed.

    python3 scripts/tester-notes.py [--dry-run] [--self-test]
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import sys
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
FOR_ALAN = REPO / "docs" / "notes" / "for-alan.md"
NOTES = REPO / "docs" / "RELEASE-NOTES.md"
POSTED = REPO / ".github" / "tester-notes-posted.txt"
LIMIT = 1900  # Discord's 2000, with room


def paragraphs(text: str) -> list[tuple[str, str, str]]:
    """Each **For <name>** paragraph: the name, its reference in brackets, and its words, joined onto one line."""
    found = []
    for block in re.split(r"\n\s*\n", text):
        head = re.match(r"\*\*For (?P<name>[^*]+)\*\* \((?P<ref>[^)]*)\):\s*(?P<words>.*)", block.strip(), re.S)
        if head:
            found.append((head["name"].strip(), head["ref"], " ".join(head["words"].split())))
    return found


def newest_nightly(notes: str) -> int:
    numbers = [int(n) for n in re.findall(r"nightly\.(\d+)", notes)]
    return max(numbers) if numbers else 0


def ready(ref: str, words: str, notes: str, newest: int) -> bool:
    named = [int(n) for n in re.findall(r"nightly[ .](\d+)", words)]
    if named:
        return max(named) <= newest
    for kind, number in re.findall(r"\b(entry|issue) (\d+)", ref, re.I):
        if re.search(rf"\({kind} {number}\b", notes, re.I):
            return True
    return False


def message(name: str, words: str) -> str:
    said = f"Hey {name}, {words[0].lower() + words[1:] if words[:1].isupper() and not words[:2].isupper() else words}"
    return said if len(said) <= LIMIT else said[: LIMIT - 1].rsplit(" ", 1)[0] + "…"


def key(name: str, words: str) -> str:
    return hashlib.sha256(f"{name}\n{words}".encode("utf-8")).hexdigest()[:16]


def posted() -> set[str]:
    return set(POSTED.read_text(encoding="utf-8").split()) if POSTED.exists() else set()


def post(address: str, content: str) -> None:
    body = json.dumps({"content": content, "allowed_mentions": {"parse": []}}).encode("utf-8")
    request = urllib.request.Request(address, data=body, method="POST", headers={"Content-Type": "application/json", "User-Agent": "GroupLab tester notes"})
    with urllib.request.urlopen(request, timeout=30):
        pass


def run(dry_run: bool) -> int:
    notes = NOTES.read_text(encoding="utf-8")
    newest = newest_nightly(notes)
    done = posted()
    address = os.environ.get("DISCORD_TESTERS_WEBHOOK", "")
    waiting = [(n, w) for n, r, w in paragraphs(FOR_ALAN.read_text(encoding="utf-8")) if key(n, w) not in done and ready(r, w, notes, newest)]
    if not waiting:
        print("Tester notes: nothing new to pass on.")
        return 0
    if not address or dry_run:
        print(f"Tester notes: {len(waiting)} ready, posted nowhere (off until Alan says yes, request 107). Drafts:")
        for name, words in waiting:
            print("\n> " + message(name, words))
        return 0
    for name, words in waiting:
        try:
            post(address, message(name, words))
        except OSError as error:
            print(f"Tester notes: the post for {name} failed ({type(error).__name__}); it waits for the next build.")
            continue
        with POSTED.open("a", encoding="utf-8") as record:
            record.write(key(name, words) + "\n")
        print(f"Tester notes: passed on to {name}.")
    return 0


def self_test() -> int:
    text = "**For Quack** (entry 442, not a request): Thank you. In 0.2.0-nightly.199, out now, the rows keep the focus.\n\nOther.\n\n**For Fenix** (issue 35): The small sheet fits."
    found = paragraphs(text)
    checks = [
        ("two paragraphs found", len(found) == 2),
        ("named nightly decides", ready(found[0][1], found[0][2], "", 199) and not ready(found[0][1], found[0][2], "", 198)),
        ("an issue in the notes decides", ready(found[1][1], found[1][2], "- Fixed. (Issue 35)", 0)),
        ("not yet when nowhere", not ready(found[1][1], found[1][2], "- Fixed. (Entry 40)", 0)),
        ("opens as Alan would", message("Quack", "Thank you.") == "Hey Quack, thank you."),
        ("a long one is cut", len(message("Q", "word " * 900)) <= LIMIT),
    ]
    for name, ok in checks:
        print(("ok   " if ok else "FAIL ") + name)
    return 0 if all(ok for _, ok in checks) else 1


if __name__ == "__main__":
    sys.exit(self_test() if "--self-test" in sys.argv else run("--dry-run" in sys.argv))
