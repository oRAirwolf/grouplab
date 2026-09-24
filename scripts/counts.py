#!/usr/bin/env python3
"""Every number that counts a thing in this repository, computed from the thing it counts.

NOTES-FROM-PLANNING.md entry 159 section 5.2. The home page said twenty-two built-in sheets; the tour and
the testing guide said twenty. Both had been typed by hand, one of them counted the two tiled layouts as
sheets, and neither was checked against the folder that holds them. A count that is typed is a count
that is right until the day the thing changes and then wrong for as long as nobody reads that page.

    python3 scripts/counts.py            print every count
    python3 scripts/counts.py --write    rewrite every <!--count:NAME-->...<!--/count--> span in the documents
    python3 scripts/counts.py --check    fail if any span says something other than the count

The site builder imports `counts()` and `words()` and puts the same numbers into its own pages, so a
page and a document cannot disagree about how many of something there are.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

# The documents that carry generated counts. A document not listed here is not rewritten, which is
# deliberate: the logs quote old numbers on purpose.
DOCUMENTS = ["README.md", "docs/USER-GUIDE.md", "docs/TESTING-GUIDE.md"]

SPAN = re.compile(r"<!--count:(?P<name>[a-z-]+)(?::(?P<form>words|Words|digits))?-->(?P<said>.*?)<!--/count-->")

SMALL = ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
         "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"]
TENS = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"]


def words(n: int) -> str:
    """A count as a person would write it in a sentence, for the numbers this project has."""
    if n < 20:
        return SMALL[n]
    if n < 100:
        return TENS[n // 10] + ("" if n % 10 == 0 else "-" + SMALL[n % 10])
    return str(n)


def counts() -> dict[str, int]:
    targets = sorted((REPO / "targets").glob("*.gltd.json"))
    # A tiled sheet's 3x2 preset is the same sheet laid out on six pages. The library lists it as its own
    # row, so it is an entry, and it is not a sheet.
    sheets = [t for t in targets if ".3x2." not in t.name]

    research = REPO / "website" / "research"
    articles = [p for p in research.glob("*.md") if p.stem.isascii() and p.stem != "PUBLISHED"]
    published = [p for p in articles if re.search(r"^state:\s*published\s*$", p.read_text(encoding="utf-8"), re.M)]

    import json
    tour = json.loads((REPO / "website" / "tour.json").read_text(encoding="utf-8"))

    return {
        "sheets": len(sheets),
        "library-entries": len(targets),
        "tiled-layouts": len(targets) - len(sheets),
        "articles": len(articles),
        "articles-published": len(published),
        "tour-screens": len(tour["order"]),
    }


def render(name: str, form: str | None, found: dict[str, int]) -> str:
    n = found[name]
    if form == "digits":
        return str(n)
    said = words(n)
    return said[:1].upper() + said[1:] if form == "Words" else said


def rewrite(check: bool) -> int:
    found = counts()
    stale = 0
    for rel in DOCUMENTS:
        path = REPO / rel
        text = path.read_text(encoding="utf-8")

        def put(m: re.Match[str]) -> str:
            nonlocal stale
            name = m.group("name")
            if name not in found:
                raise SystemExit(f"{rel}: <!--count:{name}--> counts nothing this script knows. Known: {sorted(found)}")
            wanted = render(name, m.group("form"), found)
            if m.group("said") != wanted:
                stale += 1
                print(f"{rel}: says {m.group('said')!r} for {name}, and the count is {wanted!r}",
                      file=sys.stderr if check else sys.stdout)
            form = f":{m.group('form')}" if m.group("form") else ""
            return f"<!--count:{name}{form}-->{wanted}<!--/count-->"

        new = SPAN.sub(put, text)
        if not check and new != text:
            path.write_text(new, encoding="utf-8", newline="\n")

    if check and stale:
        print(f"{stale} count(s) have drifted. Run: python3 scripts/counts.py --write", file=sys.stderr)
        return 1
    print("every generated count is current." if check else f"counts: {found}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Counts computed from the things they count.")
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    if args.write or args.check:
        return rewrite(check=args.check)
    for name, n in counts().items():
        print(f"{name}: {n}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
