#!/usr/bin/env python3
"""The outlines the sheet preview draws its words with. NOTES-FROM-PLANNING.md entry 250 section 1.

A printed sheet's words are Helvetica, one of the PDF's standard fonts, which the reader supplies; the preview on the Targets screen is
drawn by GroupLab itself and had no font, so it showed the artwork and none of the writing. Liberation Sans is metric-compatible with
Helvetica and free to redistribute (SIL Open Font License 1.1), so its outlines, placed at Helvetica's own advances, put every word
where the PDF puts it. This writes the outlines of every character a sheet can carry, printable ASCII and Latin-1 as the PDF's
WinAnsi encoding has them, to a small text file the Core embeds: no font file ships, and nothing is read from the machine.

    python scripts/sheet-glyphs.py LiberationSans-Regular.ttf LICENSE

The outlines are a modified form of the font, so under the SIL Open Font License they do not carry its reserved name: the file is
SheetSans.glyphs, and its header holds the font's copyright notice and the whole license, as the license asks of a modified version.

Each line is one character: its code point, then its contours, each "M x y" followed by "L x y" and "Q cx cy x y" segments and "Z",
in font units (2048 to the em), y up. Run it again only to change the font.
"""
from __future__ import annotations

import sys
from pathlib import Path

from fontTools.pens.basePen import BasePen
from fontTools.ttLib import TTFont

OUT = Path(__file__).resolve().parent.parent / "src" / "GroupLab.Core" / "Rendering" / "Glyphs" / "SheetSans.glyphs"
CODES = list(range(0x20, 0x7F)) + list(range(0xA0, 0x100))


class Path_(BasePen):
    def __init__(self, glyphs):
        super().__init__(glyphs)
        self.out: list[str] = []

    def _moveTo(self, p):
        self.out.append(f"M {round(p[0])} {round(p[1])}")

    def _lineTo(self, p):
        self.out.append(f"L {round(p[0])} {round(p[1])}")

    def _qCurveToOne(self, c, p):
        self.out.append(f"Q {round(c[0])} {round(c[1])} {round(p[0])} {round(p[1])}")

    def _curveToOne(self, c1, c2, p):
        raise SystemExit("a cubic curve in a TrueType font; this expects quadratic outlines")

    def _closePath(self):
        self.out.append("Z")

    _endPath = _closePath


def main() -> int:
    font = TTFont(sys.argv[1])
    if font["head"].unitsPerEm != 2048:
        raise SystemExit("expected 2048 units to the em")
    cmap = font.getBestCmap()
    glyphs = font.getGlyphSet()
    notice = Path(sys.argv[2]).read_text(encoding="utf-8").splitlines()
    lines = ["# Glyph outlines derived from Liberation Sans Regular 2.1.5 by scripts/sheet-glyphs.py (entry 250 section 1), converted",
             "# to straight and quadratic segments; a modified version under the license below, so it does not use the reserved name.", "#"]
    lines += ["# " + line if line else "#" for line in notice]
    for code in CODES:
        name = cmap.get(code)
        if name is None:
            continue
        pen = Path_(glyphs)
        glyphs[name].draw(pen)
        lines.append(f"{code:X} " + " ".join(pen.out))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_bytes(("\n".join(lines) + "\n").encode("ascii"))
    print(f"{sum(1 for line in lines if not line.startswith('#'))} characters, {OUT.stat().st_size:,} bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
