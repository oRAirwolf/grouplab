#!/usr/bin/env python3
"""American spelling in everything a user reads. NOTES-FROM-PLANNING.md entry 169 section 9.

GroupLab's users are overwhelmingly American and its defaults are already inches, yards and MOA, so every
string a person reads, in the application and on the site, spells center, caliber, analyze and color. Code
identifiers are not renamed: that is churn with no reader.

    python scripts/american-spelling.py            list what is British, file and line, and change nothing
    python scripts/american-spelling.py --fix      change it

What counts as something a user reads:

- In C#, in the application and the engine behind it, the text of a string literal that holds a space,
  outside any {hole} of an interpolated string. A literal with no space is usually a key, a name in a file
  format or a command, and is never changed: renaming a JSON key would break every file already saved. But a
  literal whose whole text is one British word is checked too, entry 189 section 2: "Calibre" was the Setup
  panel's label for a week because it had no space in it. The few that really are keys say "British on
  purpose" on their line. Comments
  are not read by users and are left alone. A line that has to keep a British form, because it reads what
  a person types, says so with the words "British on purpose" in a comment.
- The command line tool is not swept: its usage names options such as --calibre that its parser reads,
  and the rest of its output is research tables for this repository.
- On the site, the prose of the research articles, the guides and glossary it renders, the tour, and the
  text in the site builder's string literals; and README.md, the repository's front page.

Quoted material is left as it was written: anything between double quotes or curly quotes in prose, and any
line of a Markdown block quote or code block. docs/RELEASE-NOTES.md is not swept: every entry in it is a
published release's notes, and a published release is never edited. A new note is checked before it reaches the
file instead: scripts/release-notes.py refuses one with a British form, from this file's own word list (entries 187 and 189). The test in
tests/GroupLab.Core.Tests/AmericanSpellingTests.cs runs this in its checking mode.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]

# British form -> American form, whole words only, case kept.
WORDS = {
    "centre": "center", "centres": "centers", "centrefire": "centerfire", "centred": "centered", "centring": "centering", "centreline": "centerline",
    "calibre": "caliber", "calibres": "calibers",
    "analyse": "analyze", "analysed": "analyzed", "analysing": "analyzing", "analyser": "analyzer",
    "colour": "color", "colours": "colors", "coloured": "colored", "colouring": "coloring", "colourful": "colorful",
    "grey": "gray", "greys": "grays", "greyed": "grayed", "greyish": "grayish", "greyscale": "grayscale",
    "metre": "meter", "metres": "meters", "millimetre": "millimeter", "millimetres": "millimeters",
    "centimetre": "centimeter", "centimetres": "centimeters", "kilometre": "kilometer", "kilometres": "kilometers",
    "behaviour": "behavior", "behaviours": "behaviors", "favour": "favor", "favours": "favors", "favourite": "favorite",
    "favourable": "favorable", "neighbour": "neighbor", "neighbours": "neighbors", "neighbouring": "neighboring",
    "honour": "honor", "honours": "honors", "honoured": "honored", "honouring": "honoring",
    "labelled": "labeled", "labelling": "labeling", "modelled": "modeled", "modelling": "modeling",
    "cancelled": "canceled", "cancelling": "canceling", "travelled": "traveled", "travelling": "traveling",
    "dialled": "dialed", "dialling": "dialing", "levelled": "leveled", "totalled": "totaled", "signalled": "signaled",
    "licence": "license", "licences": "licenses", "catalogue": "catalog", "catalogues": "catalogs",
    "programme": "program", "programmes": "programs", "judgement": "judgment", "judgements": "judgments",
    "defence": "defense", "practise": "practice", "practised": "practiced", "aluminium": "aluminum",
    "artefact": "artifact", "artefacts": "artifacts", "sceptical": "skeptical", "fulfil": "fulfill", "enrol": "enroll",
    "manoeuvre": "maneuver", "cheque": "check", "tyre": "tire", "tyres": "tires",
}
# The -ise verbs, every form.
for stem in ["organ", "recogn", "normal", "minim", "maxim", "real", "summar", "priorit", "optim", "custom", "final", "visual",
             "emphas", "standard", "apolog", "critic", "author", "initial", "serial", "synchron", "categor", "character",
             "util", "general", "special", "memor", "stabil", "random", "digit", "sanit", "neutral"]:
    for british, american in (("ise", "ize"), ("ised", "ized"), ("ises", "izes"), ("ising", "izing"), ("isation", "ization"), ("isations", "izations")):
        WORDS[stem + british] = stem + american

PATTERN = re.compile(r"\b(" + "|".join(sorted(WORDS, key=len, reverse=True)) + r")\b", re.IGNORECASE)


def american(word: str) -> str:
    out = WORDS[word.lower()]
    if word.isupper() and len(word) > 1:
        return out.upper()
    if word[0].isupper():
        return out[0].upper() + out[1:]
    return out


QUOTED = re.compile(r'"[^"\n]*"|“[^”\n]*”')


def prose(text: str, fix: bool) -> tuple[str, list[str]]:
    """British words in prose outside quoted material."""
    found: list[str] = []
    out: list[str] = []
    last = 0
    for q in QUOTED.finditer(text):
        piece = text[last:q.start()]
        found += [m.group(0) for m in PATTERN.finditer(piece)]
        out.append(PATTERN.sub(lambda m: american(m.group(0)), piece) if fix else piece)
        out.append(q.group(0))
        last = q.end()
    piece = text[last:]
    found += [m.group(0) for m in PATTERN.finditer(piece)]
    out.append(PATTERN.sub(lambda m: american(m.group(0)), piece) if fix else piece)
    return "".join(out), found


def literal_text(body: str, interpolated: bool, fix: bool) -> tuple[str, list[str]]:
    """A C# literal's text: only a literal holding a space is prose, and in an interpolated one only outside the holes."""
    if " " not in body:
        # Entry 189 section 2: one British word on its own is a label as often as a key, so it is reported, and a key says so on its line.
        if not interpolated and body.lower() in WORDS:
            return (american(body) if fix else body), [body]
        return body, []
    if not interpolated:
        return prose_plain(body, fix)
    out: list[str] = []
    found: list[str] = []
    i = 0
    while i < len(body):
        if body.startswith("{{", i):
            out.append("{{")
            i += 2
            continue
        if body[i] == "{":
            depth, j = 1, i + 1
            while j < len(body) and depth:
                depth += 1 if body[j] == "{" else -1 if body[j] == "}" else 0
                j += 1
            out.append(body[i:j])
            i = j
            continue
        j = i
        while j < len(body) and body[j] != "{":
            j += 1
        text, f = prose_plain(body[i:j], fix)
        out.append(text)
        found += f
        i = j
    return "".join(out), found


def prose_plain(text: str, fix: bool) -> tuple[str, list[str]]:
    # Inside a C# literal, quoted material is written with \" and is left alone.
    parts = re.split(r'(\\"[^"\\]*\\")', text)
    out, found = [], []
    for k, part in enumerate(parts):
        if k % 2:
            out.append(part)
            continue
        found += [m.group(0) for m in PATTERN.finditer(part)]
        out.append(PATTERN.sub(lambda m: american(m.group(0)), part) if fix else part)
    return "".join(out), found


def end_of_line(source: str, start: int) -> int:
    j = source.find("\n", start)
    return len(source) if j < 0 else j


def csharp(source: str, fix: bool) -> tuple[str, list[tuple[int, str]]]:
    """Every string literal's text in a C# file, comments and character literals skipped."""
    out: list[str] = []
    found: list[tuple[int, str]] = []
    i, n = 0, len(source)
    while i < n:
        c = source[i]
        if source.startswith("//", i):
            j = source.find("\n", i)
            j = n if j < 0 else j
            out.append(source[i:j])
            i = j
        elif source.startswith("/*", i):
            j = source.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(source[i:j])
            i = j
        elif c == "\n" and "British on purpose" in source[i + 1:end_of_line(source, i + 1)]:
            j = end_of_line(source, i + 1)
            out.append(source[i:j])
            i = j
        elif c == "'":
            j = i + 1
            while j < n and source[j] != "'":
                j += 2 if source[j] == "\\" else 1
            out.append(source[i:j + 1])
            i = j + 1
        elif c in "$@\"" and (c == '"' or source[i + 1:i + 2] in ('"', "@", "$")):
            k = i
            while k < n and source[k] in "$@":
                k += 1
            if k >= n or source[k] != '"':
                out.append(c)
                i += 1
                continue
            prefix = source[i:k]
            if source.startswith('"""', k):
                # Raw string literal: left as it is.
                j = source.find('"""', k + 3)
                j = n if j < 0 else j + 3
                out.append(source[i:j])
                i = j
                continue
            verbatim, interpolated = "@" in prefix, "$" in prefix
            j = k + 1
            while j < n:
                if verbatim and source.startswith('""', j):
                    j += 2
                    continue
                if not verbatim and source[j] == "\\":
                    j += 2
                    continue
                if interpolated and source[j] == "{" and not source.startswith("{{", j):
                    depth = 1
                    j += 1
                    while j < n and depth:
                        if source[j] == '"':
                            # A string inside a hole: skip it whole.
                            j += 1
                            while j < n and source[j] != '"':
                                j += 2 if source[j] == "\\" else 1
                        depth += 1 if source[j] == "{" else -1 if source[j] == "}" else 0
                        j += 1
                    continue
                if interpolated and source.startswith("{{", j):
                    j += 2
                    continue
                if source[j] == '"':
                    break
                j += 1
            body = source[k + 1:j]
            text, words = literal_text(body, interpolated, fix)
            line = source.count("\n", 0, i) + 1
            found += [(line, w) for w in words]
            out.append(prefix + '"' + text + '"')
            i = j + 1
        else:
            out.append(c)
            i += 1
    return "".join(out), found


def markdown(source: str, fix: bool) -> tuple[str, list[tuple[int, str]]]:
    """Prose in a Markdown file, block quotes, code blocks and code spans left as they are."""
    out, found = [], []
    fenced = False
    for number, line in enumerate(source.split("\n"), 1):
        if line.lstrip().startswith("```"):
            fenced = not fenced
            out.append(line)
            continue
        if fenced or line.lstrip().startswith(">"):
            out.append(line)
            continue
        pieces = re.split(r"(`[^`]*`|\]\([^)]*\))", line)
        new = []
        for k, piece in enumerate(pieces):
            if k % 2:
                new.append(piece)
                continue
            text, words = prose(piece, fix)
            found += [(number, w) for w in words]
            new.append(text)
        out.append("".join(new))
    return "\n".join(out), found


def targets() -> list[tuple[Path, str]]:
    files: list[tuple[Path, str]] = []
    for project in ("src/GroupLab.App", "src/GroupLab.Core"):
        for p in sorted((REPO / project).rglob("*.cs")):
            if not any(part in ("bin", "obj") for part in p.parts):
                files.append((p, "cs"))
    for p in sorted((REPO / "website/research").rglob("*.md")):
        files.append((p, "md"))
    files.append((REPO / "README.md", "md"))
    for name in ("USER-GUIDE.md", "TESTING-GUIDE.md", "GLOSSARY.md", "PLATFORM-SUPPORT.md"):
        if (REPO / "docs" / name).is_file():
            files.append((REPO / "docs" / name, "md"))
    files.append((REPO / "website/tour.json", "json"))
    # Entry 189 section 2: the cartridge families' names are the caliber box's suggestions, so they are text a user reads.
    files.append((REPO / "src/GroupLab.Core/Marking/cartridges.json", "names"))
    files.append((REPO / "website/build.py", "py"))
    return files


def python_strings(source: str, fix: bool) -> tuple[str, list[tuple[int, str]]]:
    """The site builder's string literals that hold a space, comments and f-string holes left alone."""
    out, found = [], []
    pattern = re.compile(r'(?P<prefix>[rbfuRBFU]{0,2})(?P<q>"""|\'\'\'|"|\')(?P<body>(?:\\.|(?!(?P=q)).)*?)(?P=q)', re.S)
    last = 0
    for m in pattern.finditer(source):
        # Skip matches that start inside a comment.
        line_start = source.rfind("\n", 0, m.start()) + 1
        if "#" in source[line_start:m.start()] and source[line_start:m.start()].count('"') % 2 == 0 and source[line_start:m.start()].count("'") % 2 == 0:
            continue
        out.append(source[last:m.start()])
        body = m.group("body")
        before = source[line_start:m.start()].strip()
        if len(m.group("q")) == 3 and ("r" in m.group("prefix").lower() or before == ""):
            # A docstring, or the stylesheet: not text a visitor reads.
            out.append(m.group(0))
            last = m.end()
            continue
        if " " in body:
            interpolated = "f" in m.group("prefix").lower()
            text, words = literal_text(body, interpolated, fix) if interpolated else prose(body, fix)
            found += [(source.count("\n", 0, m.start()) + 1, w) for w in words]
        else:
            text = body
        out.append(m.group("prefix") + m.group("q") + text + m.group("q"))
        last = m.end()
    out.append(source[last:])
    return "".join(out), found


def json_strings(source: str, fix: bool) -> tuple[str, list[tuple[int, str]]]:
    out, found, last = [], [], 0
    for m in re.finditer(r'"((?:\\.|[^"\\])*)"', source):
        out.append(source[last:m.start()])
        body = m.group(1)
        text, words = prose_plain(body, fix) if " " in body else (body, [])
        found += [(source.count("\n", 0, m.start()) + 1, w) for w in words]
        out.append('"' + text + '"')
        last = m.end()
    out.append(source[last:])
    return "".join(out), found


def json_names(source: str, fix: bool) -> tuple[str, list[tuple[int, str]]]:
    """Only the "name" values: the shorthand beside them is what a person types, "22 centrefire" included, and is read, never shown."""
    out, found, last = [], [], 0
    for m in re.finditer(r'"name":\s*"((?:\\.|[^"\\])*)"', source):
        out.append(source[last:m.start(1)])
        text, words = prose_plain(m.group(1), fix)
        found += [(source.count("\n", 0, m.start()) + 1, w) for w in words]
        out.append(text)
        last = m.end(1)
    out.append(source[last:])
    return "".join(out), found


HANDLERS = {"cs": csharp, "md": markdown, "json": json_strings, "py": python_strings, "names": json_names}


def self_test() -> int:
    """The cases entry 189 section 2 names, each with what the check must say. Prints and exits 0 or 1."""
    cases = [
        ('setup.Children.Add(Needed("Calibre", calibreNeeded, calibreFrame));', ["Calibre"]),
        ('ToolTip.SetTip(railHere, "Analyse");', ["Analyse"]),
        ('Opt(w, "licence", d.Licence);  // British on purpose: a key in a file GroupLab reads and writes', []),
        ('string key = "calibreInches";', []),
        ('Readout("Caliber", name);', []),
        ('status.Text = "Measured from the centre of each hole.";', ["centre"]),
    ]
    failed = 0
    for source, want in cases:
        # Each case is a line of its own, as in a file, so a marker on it is read the way the sweep reads it.
        _, found = csharp("\n" + source + "\n", False)
        got = [w for _, w in found]
        ok = got == want
        failed += not ok
        print(("ok   " if ok else "FAIL ") + source[:70] + ("" if ok else f": got {got}, want {want}"))
    print("american-spelling self-test: " + ("passed" if not failed else f"{failed} failed"))
    return 1 if failed else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="American spelling in user-facing text.")
    parser.add_argument("--fix", action="store_true", help="change the British forms")
    parser.add_argument("--self-test", action="store_true", help="check the checker against the cases it must catch and pass")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    total = 0
    for path, kind in targets():
        raw = path.read_bytes()
        crlf = b"\r\n" in raw
        source = raw.decode("utf-8").replace("\r\n", "\n")
        text, found = HANDLERS[kind](source, args.fix)
        for line, word in found:
            print(f"{path.relative_to(REPO).as_posix()}:{line}: {word} -> {american(word)}")
        total += len(found)
        if args.fix and text != source:
            path.write_bytes((text.replace("\n", "\r\n") if crlf else text).encode("utf-8"))
    print(f"{total} British forms" + (" changed" if args.fix else ""), file=sys.stderr)
    return 0 if total == 0 or args.fix else 1


if __name__ == "__main__":
    sys.exit(main())
