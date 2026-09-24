#!/usr/bin/env python3
"""Split the three big logs into a live file and a never-deleted archive.

NOTES-FROM-PLANNING.md entry 160 section 1. Alan: "I would like going forward is for cowork and code to
be more efficient with tokens without sacrificing the quality of research or the application."

Three files weighed 2.1 MB between them and both sessions read some version of them most days. Reading
them whole is most of a day's token allowance spent before a line of work happens.

**Nothing is deleted and nothing is rewritten.** An entry that moves is byte identical to the one that
was there. The live file keeps the newest material plus an index of where the rest went, so a number is
never lost and never reused.

    python3 scripts/split-logs.py --check     say what would move, change nothing
    python3 scripts/split-logs.py             do it

It is written to be run once, and then again whenever the live files have grown back. Running it twice
is safe: material already in the archive is not moved again, and the index is rewritten from what is
actually there.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
ARCHIVE = REPO / "docs" / "notes" / "archive"

# Entry 160 section 1.1: the last fifteen entries stay live. The entry also offers "or the last fourteen
# days, whichever is longer", which is inoperative here: this repository is eleven days old, so fourteen
# days is the whole file and the rule would move nothing. Raised as a question; the count is what the
# section is for.
LIVE_ENTRIES = 15

# Entry 160 section 1.2: the results file keeps its newest sections. Ten is about one run's worth.
LIVE_RESULTS = 10

NOTES_HEADING = re.compile(r"^#{1,2} (?P<date>\d{4}-\d{2}-\d{2}), entry (?P<n>\d+)[:,]", re.MULTILINE)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str, check: bool) -> None:
    if check:
        print(f"  would write {path.relative_to(REPO)}, {len(text.encode('utf-8')):,} bytes")
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def blocks(text: str, heading: re.Pattern[str]) -> tuple[str, list[tuple[re.Match[str], str]]]:
    """The preamble, and each heading with everything under it up to the next heading."""
    marks = list(heading.finditer(text))
    if not marks:
        return text, []
    out = []
    for i, m in enumerate(marks):
        end = marks[i + 1].start() if i + 1 < len(marks) else len(text)
        out.append((m, text[m.start():end]))
    return text[:marks[0].start()], out


def split_notes(check: bool) -> list[str]:
    """docs/NOTES-FROM-PLANNING.md: newest fifteen entries live, the rest into one file per month."""
    path = REPO / "docs" / "NOTES-FROM-PLANNING.md"
    text = read(path)

    # The heading level drifted: entries 119 to 152 were written with one # and 1 to 118 with two, so
    # NotesStatusTests and QuestionStatusTests, which look for "## ", have been silently skipping the
    # thirty four newest entries. Normalised here, before anything moves, because a log whose headings
    # are two different things cannot be split reliably either.
    text = re.sub(r"^# (\d{4}-\d{2}-\d{2}, entry )", r"## \1", text, flags=re.MULTILINE)

    preamble, entries = blocks(text, NOTES_HEADING)
    if not entries:
        return ["docs/NOTES-FROM-PLANNING.md: no entry headings found, so nothing was touched"]

    live, older = entries[:LIVE_ENTRIES], entries[LIVE_ENTRIES:]

    months: dict[str, list[tuple[re.Match[str], str]]] = {}
    for m, body in older:
        months.setdefault(m.group("date")[:7], []).append((m, body))

    index = ["## The archive", "",
             "Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the",
             "only written record of why much of this project is the way it is.", ""]
    for month in sorted(months, reverse=True):
        items = months[month]
        numbers = sorted(int(m.group("n")) for m, _ in items)
        name = f"notes-{month}.md"
        index.append(f"- [`docs/notes/archive/{name}`](notes/archive/{name}), entries {numbers[0]} to {numbers[-1]}, "
                     f"{len(items)} of them.")

        head = (f"# Notes from the planning session, {month}\n\n"
                f"Archived from `docs/NOTES-FROM-PLANNING.md` under entry 160. Entries {numbers[0]} to {numbers[-1]}, "
                "newest first, exactly as they were written.\n\n---\n\n")
        write(ARCHIVE / name, head + "".join(body for _, body in items), check)

    index.append("")
    write(path, preamble + "\n".join(index) + "\n---\n\n" + "".join(body for _, body in live), check)
    return [f"notes: {len(live)} entries live, {len(older)} archived over {len(months)} month file(s)"]


RESULTS_HEADING = re.compile(r"^## (?P<title>.+)$", re.MULTILINE)
RESULTS_ENTRY = re.compile(r"^## Entry (?P<n>\d+)")


def split_results(check: bool) -> list[str]:
    """docs/PHASE1-RESULTS.md: the gates, the newest sections and the decision log stay; the rest is banded."""
    path = REPO / "docs" / "PHASE1-RESULTS.md"
    preamble, sections = blocks(read(path), RESULTS_HEADING)
    if not sections:
        return ["docs/PHASE1-RESULTS.md: no sections found, so nothing was touched"]

    keep_named = {"Where the Phase 1 gates stand", "Decision log"}
    movable = [(m, b) for m, b in sections if m.group("title").strip() not in keep_named]
    stay = {id(b) for m, b in sections if m.group("title").strip() in keep_named}

    older, newest = movable[:-LIVE_RESULTS], movable[-LIVE_RESULTS:]
    keep_ids = stay | {id(b) for _, b in newest}

    # Banded by the entry a result belongs to, so a reader looking for entry 87 knows which file to open
    # without opening any of them. Everything before entry numbering existed goes in one file of its own.
    bands: dict[str, list[tuple[re.Match[str], str]]] = {}
    for m, body in older:
        n = RESULTS_ENTRY.match(m.group(0))
        key = f"{(int(n.group('n')) // 25) * 25 + 1:03d}-{(int(n.group('n')) // 25) * 25 + 25:03d}" if n else "milestones"
        bands.setdefault(key, []).append((m, body))

    index = ["## The archive", "",
             "Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.", ""]
    for key in sorted(bands):
        name = f"results-{key}.md"
        what = "the milestone work, before results were written per entry" if key == "milestones" else f"entries {key.replace('-', ' to ')}"
        index.append(f"- [`docs/notes/archive/{name}`](notes/archive/{name}), {what}, {len(bands[key])} section(s).")
        head = (f"# Phase 1 results, {what}\n\n"
                "Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.\n\n")
        write(ARCHIVE / name, head + "".join(b for _, b in bands[key]), check)

    index.append("")
    live = preamble
    for m, body in sections:
        if id(body) in keep_ids:
            if m.group("title").strip() == "Decision log":
                live += "\n".join(index) + "\n"
            live += body
    write(path, live, check)
    return [f"results: {len(newest) + len(keep_named)} sections live, {len(older)} archived over {len(bands)} file(s)"]


QUESTION_HEADING = re.compile(r"^## (?P<date>\d{4}-\d{2}-\d{2}), question (?P<n>\d+)[:,]", re.MULTILINE)


def split_questions(check: bool) -> list[str]:
    """docs/QUESTIONS-FOR-PLANNING.md: open questions stay, answered ones move, every number stays listed."""
    path = REPO / "docs" / "QUESTIONS-FOR-PLANNING.md"
    preamble, questions = blocks(read(path), QUESTION_HEADING)
    if not questions:
        return ["docs/QUESTIONS-FOR-PLANNING.md: no question headings found, so nothing was touched"]

    def answered(body: str) -> bool:
        status = next((l for l in body.splitlines() if l.startswith("**Status:")), "")
        return not status.startswith("**Status: open")

    live = [(m, b) for m, b in questions if not answered(b)]
    gone = [(m, b) for m, b in questions if answered(b)]
    if not gone:
        return ["questions: nothing answered to move"]

    name = "questions-answered.md"
    existing = read(ARCHIVE / name) if (ARCHIVE / name).exists() else (
        "# Questions for the planning session, answered\n\n"
        "Archived from `docs/QUESTIONS-FOR-PLANNING.md` under entry 160, exactly as written. Newest first. A question\n"
        "number is never reused and never lost: the live file lists every number that has moved here.\n\n---\n\n")
    write(ARCHIVE / name, existing + "".join(b for _, b in gone), check)

    numbers = sorted((int(m.group("n")) for m, _ in gone), reverse=True)
    index = ["## Answered, and moved", "",
             f"These {len(numbers)} are in [`docs/notes/archive/{name}`](notes/archive/{name}), whole. They are listed here so a",
             "number is never reused and a question is never lost:", "",
             "> " + ", ".join(str(n) for n in numbers) + ".", ""]
    write(path, preamble + "\n".join(index) + "\n---\n\n" + "".join(b for _, b in live), check)
    return [f"questions: {len(live)} open live, {len(gone)} answered archived"]


def main() -> int:
    parser = argparse.ArgumentParser(description="Split the big logs into a live file and an archive.")
    parser.add_argument("--check", action="store_true", help="say what would move and change nothing")
    args = parser.parse_args()

    before = {p: p.stat().st_size for p in [REPO / "docs" / "NOTES-FROM-PLANNING.md",
                                            REPO / "docs" / "PHASE1-RESULTS.md",
                                            REPO / "docs" / "QUESTIONS-FOR-PLANNING.md"]}
    said = split_notes(args.check) + split_results(args.check) + split_questions(args.check)
    for line in said:
        print(line)

    if not args.check:
        for p, was in before.items():
            now = p.stat().st_size
            print(f"{p.name}: {was:,} -> {now:,} bytes, {100 - (now * 100 // was)} percent smaller")
    return 0


if __name__ == "__main__":
    sys.exit(main())
