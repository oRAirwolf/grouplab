#!/usr/bin/env python3
"""NOTES-FROM-PLANNING.md entry 438 section 2.4: how hard a guide is to read, as the Flesch-Kincaid grade, section by section.

    python scripts/readability.py docs/STATISTICS-PRIMER.md [--max 9]

The grade is the number of years of school a reader needs, from sentence length and word length (syllables, counted by a plain rule
that is right for most English words). A general newspaper aims at about grade 8 to 9. The folded "math, if you want it" parts, tables,
pictures and link addresses are left out: the reader the page is for can skip all of them. With --max, it fails when the whole page is
above that grade.
"""
import argparse
import re
import sys
from pathlib import Path


def syllables(word: str) -> int:
    word = word.lower().strip("'")
    if len(word) <= 3:
        return 1
    word = re.sub(r"(?:[^laeiouy]es|ed|[^laeiouy]e)$", "", word)
    word = re.sub(r"^y", "", word)
    return max(1, len(re.findall(r"[aeiouy]{1,2}", word)))


def prose(markdown: str) -> list[tuple[str, str]]:
    """Each ## section's readable words: no math parts, tables, pictures, comments, headings or link addresses."""
    text = re.sub(r"<!-- math -->.*?<!-- /math -->", "", markdown, flags=re.S)
    sections, name, lines = [], "Opening", []
    for line in text.splitlines():
        if line.startswith("## "):
            sections.append((name, " ".join(lines)))
            name, lines = line[3:].strip(), []
            continue
        if line.startswith(("#", "|", "!", "<!--")) or not line.strip():
            lines.append("\n")
            continue
        line = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", line)
        lines.append(re.sub(r"[*`]", "", line.lstrip("- ")))
    sections.append((name, " ".join(lines)))
    return [(n, t) for n, t in sections if t.strip()]


def grade(text: str) -> tuple[float, int]:
    # A list item or a line break between paragraphs ends a sentence as a full stop does.
    sentences = [s for s in re.split(r"[.!?]+[\s\"]|\n", text) if re.search(r"[A-Za-z]", s)]
    words = re.findall(r"[A-Za-z][A-Za-z'-]*|\d+(?:\.\d+)?", text)
    if not sentences or not words:
        return 0.0, 0
    syl = sum(syllables(w) if w[0].isalpha() else 2 for w in words)
    return 0.39 * len(words) / len(sentences) + 11.8 * syl / len(words) - 15.59, len(words)


def main() -> int:
    parser = argparse.ArgumentParser(description="Flesch-Kincaid grade of a guide, by section.")
    parser.add_argument("file", type=Path)
    parser.add_argument("--max", type=float)
    args = parser.parse_args()
    parts = prose(args.file.read_text(encoding="utf-8"))
    for name, text in parts:
        g, n = grade(text)
        print(f"{g:5.1f}  {n:4d} words  {name}")
    whole, n = grade("\n".join(t for _, t in parts))
    print(f"{whole:5.1f}  {n:4d} words  the whole page")
    if args.max is not None and whole > args.max:
        print(f"readability: grade {whole:.1f} is above {args.max:g}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
