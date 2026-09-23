#!/usr/bin/env python3
"""The platform statement, from its one source into everywhere it has to appear.

NOTES-FROM-PLANNING.md entry 147 section 3.2: the same statement is on the download page, in README.md and
on every release carrying a macOS asset, and all three come from `docs/PLATFORM-SUPPORT.md`.

    python3 scripts/platform-support.py --readme        rewrite README.md between its markers
    python3 scripts/platform-support.py --check         say whether README.md is current, change nothing
    python3 scripts/platform-support.py --release       print it for a release body

**Why one source rather than three careful copies.** A statement this specific, in somebody's settled words,
is exactly the thing that gets reworded in one place and not the others. Then there are two versions of what
the project promises about macOS and no way to tell which is the real one. The page, the README and the
release are all readers of one file, and `--check` fails the build if any of them has drifted.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent.parent
SOURCE = HERE / "docs" / "PLATFORM-SUPPORT.md"
README = HERE / "README.md"

# The statement itself begins after the horizontal rule that closes the file's own explanation, so the notes
# about where it is used never reach a reader.
RULE = "\n---\n"

BEGIN = "<!-- platform-support: generated from docs/PLATFORM-SUPPORT.md, do not edit between these markers -->"
END = "<!-- end platform-support -->"


def statement() -> str:
    """The words, without the file's own notes about itself."""
    text = SOURCE.read_text(encoding="utf-8")
    if RULE not in text:
        raise SystemExit(f"{SOURCE} has no rule separating its notes from the statement")
    return text.split(RULE, 1)[1].strip()


def for_readme() -> str:
    """The statement as a README section: its headings pushed down a level, under one of the README's own."""
    lines = []
    for line in statement().splitlines():
        if line.startswith("## "):
            lines.append("#" + line)
        elif line.startswith("**Windows is the supported platform.**"):
            lines.append(line)
        else:
            lines.append(line)
    return "\n".join(lines)


def for_release() -> str:
    """The statement as it goes on a release whose assets include a macOS build."""
    return ("## What is supported, and what is not\n\n"
            + "\n".join("#" + l if l.startswith("## ") else l for l in statement().splitlines()))


def write_readme(check_only: bool) -> int:
    text = README.read_text(encoding="utf-8")
    if BEGIN not in text or END not in text:
        raise SystemExit(f"{README} has no platform-support markers; add {BEGIN} and {END} where it belongs")

    before, rest = text.split(BEGIN, 1)
    _, after = rest.split(END, 1)
    wanted = before + BEGIN + "\n\n" + for_readme() + "\n\n" + END + after

    if wanted == text:
        print("README.md carries the current statement.")
        return 0

    if check_only:
        print("README.md has drifted from docs/PLATFORM-SUPPORT.md. Run: python3 scripts/platform-support.py --readme",
              file=sys.stderr)
        return 1

    README.write_text(wanted, encoding="utf-8", newline="\n")
    print("README.md rewritten from docs/PLATFORM-SUPPORT.md.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="The platform statement, from its one source.")
    parser.add_argument("--readme", action="store_true", help="rewrite README.md between its markers")
    parser.add_argument("--check", action="store_true", help="say whether README.md is current and change nothing")
    parser.add_argument("--release", action="store_true", help="print it for a release body")
    args = parser.parse_args()

    if args.release:
        print(for_release())
        return 0
    if args.check:
        return write_readme(check_only=True)
    if args.readme:
        return write_readme(check_only=False)

    print(statement())
    return 0


if __name__ == "__main__":
    sys.exit(main())
