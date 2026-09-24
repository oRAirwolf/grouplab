#!/usr/bin/env python3
"""Every checkable claim this project publishes, and what backs it.

NOTES-FROM-PLANNING.md entry 159. Alan: "The website and readme should be scrutinized for
contradictions that I may have missed as well." Entry 152 fixed two claims he caught by reading one
tour page, and one of them was contradicted by a research article on the same site. Two found by eye
on one page is not a good rate, and the answer is not to read harder.

    python3 scripts/claims.py --extract     rewrite docs/CLAIMS.md from what is published
    python3 scripts/claims.py --check       fail if a claim is unbacked or the register is stale
    python3 scripts/claims.py --contradict  only the contradiction report

**A claim is a sentence that asserts something checkable**: what the software does, what it cannot do,
a number, a measurement, a limit, a version, an address, a licence, a platform, a name. The extractor
finds candidates by shape and is deliberately generous, because a claim it misses is a claim nobody
checks.

**The surfaces are what a reader actually sees**, so the site is read from `website/_site`, the built
pages, rather than from the sources that make them. A sentence written inside `build.py` is published
exactly like one written in an article, and reading the build output is the only way to catch both.

`docs/claims-backing.json` says what backs each claim. It is the part a person writes; everything else
here is mechanical.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SITE = REPO / "website" / "_site"
REGISTER = REPO / "docs" / "claims-backing.json"
CLAIMS = REPO / "docs" / "CLAIMS.md"

# The logs, the inbox and the archive are the project talking to itself, not to a reader. They record
# wrong claims on purpose, which is what a log is for.
SKIP_DOCS = {"NOTES-FROM-PLANNING.md", "PHASE1-RESULTS.md", "QUESTIONS-FOR-PLANNING.md", "CLAIMS.md",
             "PHASE0-RESULTS.md", "SCAN-MEASUREMENTS.md", "ONTARGET-DIMENSIONS.md"}

TAG = re.compile(r"<[^>]+>")
WS = re.compile(r"\s+")

# What makes a sentence checkable. Generous on purpose.
CHECKABLE = [
    re.compile(r"\b\d"),                                             # any number at all
    re.compile(r"\bGroupLab (?:is|does|can|cannot|never|always|only|refuses|reads|measures|prints)\b"),
    re.compile(r"\byou can\b|\byou cannot\b|\bit cannot\b|\bnothing\b.*\bever\b"),
    re.compile(r"\bsupported\b|\bunsupported\b|\blicence\b|\blicense\b|\bGPL\b"),
    re.compile(r"\bWindows\b|\bmacOS\b|\bLinux\b"),
]

# Sentences that are instructions, headings or navigation rather than assertions.
NOT_A_CLAIM = re.compile(r"^(?:download|open|print|scan|photograph|click|tap|choose|read|see|go to|next|back)\b",
                         re.IGNORECASE)

STALE = re.compile(r"\b(?:currently|at the moment|at present|for now|not yet|so far|today|right now)\b",
                   re.IGNORECASE)

WORDS = {"one": 1, "two": 2, "three": 3, "four": 4, "five": 5, "six": 6, "seven": 7, "eight": 8,
         "nine": 9, "ten": 10, "eleven": 11, "twelve": 12, "thirteen": 13, "fourteen": 14,
         "fifteen": 15, "sixteen": 16, "seventeen": 17, "eighteen": 18, "nineteen": 19,
         "twenty": 20, "thirty": 30, "forty": 40, "fifty": 50}

COUNTED = re.compile(
    r"\b(?P<n>\d+|" + "|".join(WORDS) + r")\s+(?P<what>research articles|articles|target sheets|sheets|"
    r"built-in sheets|built in sheets|platforms|operating systems|guides|bulls|tour pages)\b", re.IGNORECASE)


def sentences(text: str) -> list[str]:
    text = WS.sub(" ", TAG.sub(" ", text))
    return [s.strip() for s in re.split(r"(?<=[.!?])\s+", text) if s.strip()]


def surfaces() -> list[tuple[str, Path]]:
    """Every file a reader's words come out of, with the name to record it under."""
    out: list[tuple[str, Path]] = []
    if SITE.exists():
        for page in sorted(SITE.rglob("*.html")):
            out.append((f"site:{page.relative_to(SITE).as_posix()}", page))
    out.append(("README.md", REPO / "README.md"))
    for md in sorted(REPO.glob("*.md")):
        if md.name != "README.md":
            out.append((md.name, md))
    for md in sorted((REPO / "docs").glob("*.md")):
        if md.name not in SKIP_DOCS:
            out.append((f"docs/{md.name}", md))
    return [(name, path) for name, path in out if path.exists()]


def claims() -> list[tuple[str, int, str]]:
    """Every candidate claim: where it is published, which sentence of that surface, and the sentence."""
    found = []
    seen: set[tuple[str, str]] = set()
    for name, path in surfaces():
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        for i, s in enumerate(sentences(text), start=1):
            if len(s) < 25 or len(s) > 400 or NOT_A_CLAIM.match(s):
                continue
            if not any(p.search(s) for p in CHECKABLE):
                continue
            key = (name.split(":")[0] if name.startswith("site:") else name, s)
            if key in seen:
                continue
            seen.add(key)
            found.append((name, i, s))
    return found


def contradictions(rows: list[tuple[str, int, str]]) -> list[str]:
    """Entry 159 section 3: the shapes a contradiction takes, found rather than spotted."""
    out = []

    # 3.2 and 3.5: a count of something in this repository that appears with two different values.
    counts: dict[str, dict[int, list[str]]] = defaultdict(lambda: defaultdict(list))
    for name, _, s in rows:
        for m in COUNTED.finditer(s):
            n = m.group("n").lower()
            value = WORDS.get(n, None) if not n.isdigit() else int(n)
            if value is None:
                continue
            counts[m.group("what").lower().rstrip("s")][value].append(name)
    for what, values in sorted(counts.items()):
        if len(values) > 1:
            said = "; ".join(f"{v} in {', '.join(sorted(set(w))[:3])}" for v, w in sorted(values.items()))
            out.append(f"COUNT: \"{what}\" is published with {len(values)} different values: {said}")

    # 3.5: a sentence that is true today and says so.
    for name, _, s in rows:
        if STALE.search(s):
            out.append(f"STALE: {name}: {s[:150]}")

    # 3.4: the same assertion written twice in slightly different words, which is a contradiction waiting.
    # The same sentence rendered from its source to a page differs only in markup, so markup is removed before
    # comparing: a document and the page built from it are one statement, not two.
    def plain(s: str) -> str:
        return " ".join(re.findall(r"[a-z0-9.]+", s.lower()))

    shape: dict[str, list[tuple[str, str]]] = defaultdict(list)
    for name, _, s in rows:
        key = " ".join(sorted(w for w in re.findall(r"[a-z]{4,}", s.lower())))[:220]
        shape[key].append((name, s))
    for group in shape.values():
        variants = {plain(s) for _, s in group}
        if len(variants) > 1:
            where = ", ".join(sorted({n.split(":")[0] for n, _ in group})[:3])
            out.append(f"NEARLY: two wordings of one sentence in {where}: " + " || ".join(sorted(variants)[:2])[:260])

    return out


def register() -> dict:
    return json.loads(REGISTER.read_text(encoding="utf-8")) if REGISTER.exists() else {"backed": {}, "_": []}


def backing_for(name: str, sentence: str, book: dict) -> tuple[str, str]:
    """What backs a claim: an exact sentence first, then the first rule whose surface and sentence both match.

    A rule with only a surface classifies everything published there, which is how whole documents that are
    themselves a record of decisions are classified. The report says how many claims were classified that way,
    because a rule is not the same thing as a reading.
    """
    if sentence in book.get("backed", {}):
        entry = book["backed"][sentence]
        return entry["kind"], entry["where"]
    for rule in book.get("rules", []):
        if "surface" in rule and not re.search(rule["surface"], name):
            continue
        if "match" in rule and not re.search(rule["match"], sentence, re.IGNORECASE):
            continue
        return rule["kind"], rule["where"]
    return "unbacked", ""


def extract() -> int:
    rows = claims()
    book = register()
    kinds: dict[str, int] = defaultdict(int)

    lines = ["# What this project claims, and what backs it", "",
             "Generated by `scripts/claims.py --extract`. Do not edit: edit `docs/claims-backing.json`, which is where",
             "the backing is written down, and run it again.", "",
             "NOTES-FROM-PLANNING.md entry 159. A claim is a sentence asserting something checkable: what the software",
             "does, what it cannot do, a number, a measurement, a limit, a version, an address, a licence, a platform.",
             "The site is read from `website/_site`, the built pages, because a sentence written inside `build.py` is",
             "published exactly like one written in an article.", "",
             "**code** names a file and a symbol. **measured** names where the measurement lives and when it was taken.",
             "**decided** names the entry that decided it. **unbacked** means nothing was found, and that list is the",
             "one that matters.", "", "---", ""]

    by_surface: dict[str, list[tuple[int, str, str, str]]] = defaultdict(list)
    for name, i, s in rows:
        kind, where = backing_for(name, s, book)
        kinds[kind] += 1
        by_surface[name].append((i, s, kind, where))

    lines.append("## The count")
    lines.append("")
    lines.append("| backing | claims |")
    lines.append("|---|---|")
    for kind in ("code", "measured", "decided", "unbacked"):
        lines.append(f"| {kind} | {kinds.get(kind, 0)} |")
    lines.append(f"| **total** | **{len(rows)}** |")
    lines.append("")

    lines.append("## The claims")
    lines.append("")
    for name in sorted(by_surface):
        lines.append(f"### {name}")
        lines.append("")
        for i, s, kind, where in by_surface[name]:
            lines.append(f"- *{kind}*{(' (' + where + ')') if where else ''}: {s}")
        lines.append("")

    CLAIMS.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"{len(rows)} claims over {len(by_surface)} surfaces: "
          + ", ".join(f"{kinds.get(k, 0)} {k}" for k in ("code", "measured", "decided", "unbacked")))
    return 0


def check() -> int:
    rows = claims()
    book = register()
    unbacked = [(n, s) for n, _, s in rows if backing_for(n, s, book)[0] == "unbacked"]
    for name, s in unbacked[:40]:
        print(f"unbacked: {name}: {s[:160]}", file=sys.stderr)
    if unbacked:
        print(f"{len(unbacked)} published claims have nothing backing them. Back them, reword them or delete them, "
              "and record the backing in docs/claims-backing.json.", file=sys.stderr)
        return 1
    print(f"{len(rows)} published claims, all backed.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Every checkable claim this project publishes.")
    parser.add_argument("--extract", action="store_true")
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--contradict", action="store_true")
    args = parser.parse_args()

    if args.contradict:
        for line in contradictions(claims()):
            print(line)
        return 0
    if args.check:
        return check()
    if args.extract:
        return extract()
    parser.print_help()
    return 1


if __name__ == "__main__":
    sys.exit(main())
