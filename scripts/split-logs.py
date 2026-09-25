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

It is written to be run whenever the live files have grown back. **Entry 210 found the second run
destructive**: every archive file was written from the batch being moved alone, so the September notes
archive lost 8,400 lines before anything was committed, and the results' "newest" were taken by position
in a file whose sections are no longer in order. So now:

- an archive file is always what it held, in its order, and after it whatever is moving in that it does
  not already hold word for word; a heading is never a key, since older results repeat part headings;
- a "##" heading with no entry number in the results is a part of the result above it and moves with it;
- what stays live is chosen by entry number, not by where a block sits;
- every index is rebuilt from everything archived, and the old index is taken out before it is;
- before anything is written, every block that existed in the live file and the archive is checked to be
  present, byte for byte, in what is about to be written, and the run stops if one is not.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
ARCHIVE = REPO / "docs" / "notes" / "archive"

# Entry 160 section 1.1: the last fifteen entries stay live. Its fourteen day clause is dropped by entry 171
# section 2, answering question 48: the count is what the rule is for, and the day count moved nothing.
LIVE_ENTRIES = 15

# Entry 160 section 1.2: the results file keeps its newest sections. Ten is about one run's worth.
LIVE_RESULTS = 10

NOTES_HEADING = re.compile(r"^#{1,2} (?P<date>\d{4}-\d{2}-\d{2}), entry (?P<n>\d+)[:,]", re.MULTILINE)
# A result is headed "## Entry N" or, in a few older ones, "# Entry N"; a "##" heading with no entry number is a part of the result
# above it (entry 147's "What is published now", the gates' "What is not done, and why") and travels with it.
RESULTS_HEADING = re.compile(r"^(?:## |# (?=Entr))(?P<title>.+)$", re.MULTILINE)
RESULTS_ENTRY = re.compile(r"^#{1,2} Entr(?:y|ies) (?P<n>\d+)")
QUESTION_HEADING = re.compile(r"^## (?P<date>\d{4}-\d{2}-\d{2}), question (?P<n>\d+)[:,]", re.MULTILINE)


class Lost(Exception):
    """A block that was in the logs would not be in them after the run."""


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8").replace("\r\n", "\n")


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


def ended(body: str) -> str:
    """A block as it sits in a file: ending in exactly one blank line, so blocks join the same wherever they go."""
    return body.rstrip("\n") + "\n\n"


def without_section(text: str, title: str) -> str:
    """The text with one "## title" section taken out, up to and including the rule that closes it, if it is there."""
    start = text.find(f"## {title}")
    if start < 0:
        return text
    rule = text.find("\n---\n", start)
    end = rule + len("\n---\n") if rule >= 0 else len(text)
    return text[:start] + text[end:].lstrip("\n")


def merged(path: Path, heading: re.Pattern[str], arriving: list[str]) -> list[str]:
    """
    What an archive file holds after the run: everything it held, in its order, and after it whatever arrives that is not already
    there word for word. Headings are not keys, since older results repeat part headings such as "What is not done, and why".
    """
    held = [ended(b) for _, b in blocks(read(path), heading)[1]] if path.exists() else []
    have = {b.strip() for b in held}
    return held + [b for b in arriving if b.strip() not in have]


def first_line(body: str) -> str:
    return body.split("\n", 1)[0].strip()


def notes_number(body: str) -> int:
    return int(NOTES_HEADING.match(body).group("n"))


def results_number(body: str) -> int:
    m = RESULTS_ENTRY.match(body)
    return int(m.group("n")) if m else -1


def everything(texts: list[str], heading: re.Pattern[str]) -> list[str]:
    """Every block in a set of files, whitespace at its end aside, for the check that nothing is lost."""
    return sorted(b.rstrip("\n") for t in texts for _, b in blocks(t, heading)[1])


def unchanged(before: list[str], after: list[str], what: str) -> None:
    missing = [b for b in before if b not in after]
    if missing:
        raise Lost(f"{what}: {len(missing)} block(s) would be lost, the first headed {first_line(missing[0])!r}; nothing was written")


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

    archived = sorted(ARCHIVE.glob("notes-*.md"))
    before = everything([text] + [read(p) for p in archived], NOTES_HEADING)

    ordered = sorted(entries, key=lambda e: int(e[0].group("n")), reverse=True)
    live, older = ordered[:LIVE_ENTRIES], ordered[LIVE_ENTRIES:]

    months: dict[str, list[str]] = {}
    for m, body in older:
        months.setdefault(m.group("date")[:7], []).append(ended(body))
    for p in archived:
        months.setdefault(p.stem.removeprefix("notes-"), [])

    files: dict[Path, str] = {}
    counts: dict[str, tuple[int, int, int]] = {}
    for month, arriving in months.items():
        target = ARCHIVE / f"notes-{month}.md"
        all_here = sorted(merged(target, NOTES_HEADING, arriving), key=notes_number, reverse=True)
        numbers = [notes_number(b) for b in all_here]
        counts[month] = (min(numbers), max(numbers), len(numbers))
        head = (f"# Notes from the planning session, {month}\n\n"
                f"Archived from `docs/NOTES-FROM-PLANNING.md` under entry 160. Entries {min(numbers)} to {max(numbers)}, "
                "newest first, exactly as they were written.\n\n---\n\n")
        files[target] = head + "".join(all_here)

    index = ["## The archive", "",
             "Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the",
             "only written record of why much of this project is the way it is.", ""]
    for month in sorted(counts, reverse=True):
        low, high, n = counts[month]
        index.append(f"- [`docs/notes/archive/notes-{month}.md`](notes/archive/notes-{month}.md), entries {low} to {high}, {n} of them.")
    index.append("")
    files[path] = without_section(preamble, "The archive").rstrip("\n") + "\n\n" + "\n".join(index) + "\n---\n\n" + "".join(ended(b) for _, b in live)

    unchanged(before, everything(list(files.values()), NOTES_HEADING), "the notes")
    for p, t in files.items():
        write(p, t, check)
    return [f"notes: {len(live)} entries live, {len(older)} moved to the archive, {sum(c[2] for c in counts.values())} archived in all"]


def split_results(check: bool) -> list[str]:
    """docs/PHASE1-RESULTS.md: the gates, the newest sections by entry number and the decision log stay; the rest is banded."""
    path = REPO / "docs" / "PHASE1-RESULTS.md"
    text = read(path)
    preamble, sections = blocks(text, RESULTS_HEADING)
    if not sections:
        return ["docs/PHASE1-RESULTS.md: no sections found, so nothing was touched"]

    archived = sorted(ARCHIVE.glob("results-*.md"))
    real = [(m, b) for m, b in sections if m.group("title").strip() != "The archive"]
    before = everything(["".join(b for _, b in real)] + [read(p) for p in archived], RESULTS_HEADING)

    keep_named = {"Where the Phase 1 gates stand", "Decision log"}

    # Each result with its parts: a section with no entry number, and not one of the named ones, joins the one above it.
    units: list[tuple[re.Match[str], str]] = []
    for m, b in real:
        if units and results_number(b) < 0 and m.group("title").strip() not in keep_named:
            units[-1] = (units[-1][0], units[-1][1] + b)
        else:
            units.append((m, b))
    real = units
    movable = [(m, b) for m, b in real if m.group("title").strip() not in keep_named]
    newest = {id(b) for _, b in sorted(movable, key=lambda s: results_number(s[1]), reverse=True)[:LIVE_RESULTS]}
    older = [(m, b) for m, b in movable if id(b) not in newest]

    # Banded by the entry a result belongs to, so a reader looking for entry 87 knows which file to open
    # without opening any of them. Everything before entry numbering existed goes in one file of its own.
    def band(body: str) -> str:
        n = results_number(body)
        return f"{(n // 25) * 25 + 1:03d}-{(n // 25) * 25 + 25:03d}" if n >= 0 else "milestones"

    bands: dict[str, list[str]] = {}
    for _, body in older:
        bands.setdefault(band(body), []).append(ended(body))
    for p in archived:
        bands.setdefault(p.stem.removeprefix("results-"), [])

    files: dict[Path, str] = {}
    index = ["## The archive", "",
             "Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.", ""]
    for key in sorted(bands):
        target = ARCHIVE / f"results-{key}.md"
        all_here = merged(target, RESULTS_HEADING, bands[key])
        what = "the milestone work, before results were written per entry" if key == "milestones" else f"entries {key.replace('-', ' to ')}"
        index.append(f"- [`docs/notes/archive/results-{key}.md`](notes/archive/results-{key}.md), {what}, {len(all_here)} section(s).")
        files[target] = f"# Phase 1 results, {what}\n\nArchived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.\n\n" + "".join(all_here)

    index.append("")
    live = preamble
    kept = {id(b) for _, b in older}
    for m, body in real:
        if id(body) in kept:
            continue
        if m.group("title").strip() == "Decision log":
            live += "\n".join(index) + "\n"
        live += ended(body) if m.group("title").strip() != "Decision log" else body
    files[path] = live

    unchanged(before, everything(list(files.values()), RESULTS_HEADING), "the results")
    for p, t in files.items():
        write(p, t, check)
    return [f"results: {len(newest) + len(keep_named)} sections live, {len(older)} moved to the archive"]


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
    target = ARCHIVE / name
    held = read(target) if target.exists() else (
        "# Questions for the planning session, answered\n\n"
        "Archived from `docs/QUESTIONS-FOR-PLANNING.md` under entry 160, exactly as written. Newest first. A question\n"
        "number is never reused and never lost: the live file lists every number that has moved here.\n\n---\n\n")
    head, archived = blocks(held, QUESTION_HEADING)
    have = {first_line(b) for _, b in archived}
    arriving = [ended(b) for _, b in gone if first_line(b) not in have]
    all_here = sorted([ended(b) for _, b in archived] + arriving, key=lambda b: int(QUESTION_HEADING.match(b).group("n")), reverse=True)

    listed = re.search(r"(?m)^> ((?:\d+, )*\d+)\.$", preamble)
    numbers = sorted({int(m.group("n")) for m, _ in gone} | ({int(x) for x in listed.group(1).split(", ")} if listed else set()), reverse=True)
    index = ["## Answered, and moved", "",
             f"These {len(numbers)} are in [`docs/notes/archive/{name}`](notes/archive/{name}), whole. They are listed here so a",
             "number is never reused and a question is never lost:", "",
             "> " + ", ".join(str(n) for n in numbers) + ".", ""]
    files = {target: head + "".join(all_here),
             path: without_section(preamble, "Answered, and moved").rstrip("\n") + "\n\n" + "\n".join(index) + "\n---\n\n" + "".join(ended(b) for _, b in live)}
    unchanged(everything([read(path), held], QUESTION_HEADING), everything(list(files.values()), QUESTION_HEADING), "the questions")
    for p, t in files.items():
        write(p, t, check)
    return [f"questions: {len(live)} open live, {len(gone)} answered archived"]


def main() -> int:
    parser = argparse.ArgumentParser(description="Split the big logs into a live file and an archive.")
    parser.add_argument("--check", action="store_true", help="say what would move and change nothing")
    args = parser.parse_args()

    before = {p: p.stat().st_size for p in [REPO / "docs" / "NOTES-FROM-PLANNING.md",
                                            REPO / "docs" / "PHASE1-RESULTS.md",
                                            REPO / "docs" / "QUESTIONS-FOR-PLANNING.md"]}
    try:
        said = split_notes(args.check) + split_results(args.check) + split_questions(args.check)
    except Lost as lost:
        print(f"stopped: {lost}", file=sys.stderr)
        return 1
    for line in said:
        print(line)

    if not args.check:
        for p, was in before.items():
            now = p.stat().st_size
            print(f"{p.name}: {was:,} -> {now:,} bytes, {100 - (now * 100 // was)} percent smaller")
    return 0


if __name__ == "__main__":
    sys.exit(main())
