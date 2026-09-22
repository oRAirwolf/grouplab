"""The lead figure for "Why a photo cannot tell you your bullet's size".

NOTES-FROM-PLANNING.md entry 142 section 2.2: every chart is built by a script beside it, from data a
reader can download. The data here is `hole-ratios.csv`, one row per image, and the only thing this
script does is draw it.

It writes SVG by hand rather than through a plotting library. Nothing on this machine may install a
package, so a chart that needs one is a chart that stops building the day somebody checks the site out
somewhere else. SVG also stays sharp at any size, which a thumbnail on the index needs.

    python lead.py
"""
from __future__ import annotations

import csv
from pathlib import Path

HERE = Path(__file__).resolve().parent
DATA = HERE.parent / "hole-ratios.csv"
OUT = HERE / "lead.svg"

WIDTH, HEIGHT = 720, 420
LEFT, RIGHT, TOP, BOTTOM = 190, 30, 54, 56

# The figure is drawn on its own pale card rather than on the page, so it reads the same whether somebody
# has the site in its dark theme or its light one. An SVG loaded through <img> cannot see the page's theme,
# and a chart whose labels vanish on half the site is worse than one that looks like a printed figure.
PAPER = "#faf8f5"
INK = "#2b2a28"
FAINT = "#8b8781"
SCAN = "#3d7f6f"
PHOTO = "#c8791a"
RULE = "#d9d4cc"


def main() -> int:
    rows = list(csv.DictReader(DATA.open(encoding="utf-8")))
    sheets = []
    for row in rows:
        if row["sheet"] not in sheets:
            sheets.append(row["sheet"])

    lo, hi = 0.7, 1.5
    plot_w = WIDTH - LEFT - RIGHT
    plot_h = HEIGHT - TOP - BOTTOM
    step = plot_h / len(sheets)

    def x_of(ratio: float) -> float:
        return LEFT + (ratio - lo) / (hi - lo) * plot_w

    def y_of(sheet: str) -> float:
        return TOP + sheets.index(sheet) * step + step / 2

    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {WIDTH} {HEIGHT}" width="{WIDTH}" height="{HEIGHT}" '
        f'role="img" aria-label="Measured hole size as a fraction of the bullet, scans against photographs">',
        f'<rect width="{WIDTH}" height="{HEIGHT}" rx="6" fill="{PAPER}"/>',
    ]

    # The line at 1.0 is the bullet itself: a hole exactly the width of what made it.
    one = x_of(1.0)
    parts.append(f'<line x1="{one:.1f}" y1="{TOP - 8}" x2="{one:.1f}" y2="{HEIGHT - BOTTOM}" stroke="{FAINT}" stroke-width="1" stroke-dasharray="4 4"/>')
    parts.append(f'<text x="{one:.1f}" y="{TOP - 16}" fill="{FAINT}" font-size="13" font-family="system-ui, sans-serif" text-anchor="middle">the bullet itself</text>')

    for tick in (0.8, 1.0, 1.2, 1.4):
        x = x_of(tick)
        parts.append(f'<line x1="{x:.1f}" y1="{HEIGHT - BOTTOM}" x2="{x:.1f}" y2="{HEIGHT - BOTTOM + 6}" stroke="{RULE}" stroke-width="1"/>')
        parts.append(f'<text x="{x:.1f}" y="{HEIGHT - BOTTOM + 24}" fill="{FAINT}" font-size="13" font-family="system-ui, sans-serif" text-anchor="middle">{tick:g}</text>')

    for sheet in sheets:
        y = y_of(sheet)
        parts.append(f'<text x="{LEFT - 14}" y="{y + 5:.1f}" fill="{INK}" font-size="14" font-family="system-ui, sans-serif" text-anchor="end">{sheet}</text>')
        parts.append(f'<line x1="{LEFT}" y1="{y:.1f}" x2="{WIDTH - RIGHT}" y2="{y:.1f}" stroke="{RULE}" stroke-width="1"/>')

    for row in rows:
        x, y = x_of(float(row["ratio"])), y_of(row["sheet"])
        scan = row["medium"] == "scan"
        parts.append(
            f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{7 if scan else 6}" '
            f'fill="{SCAN if scan else PHOTO}" fill-opacity="{1 if scan else 0.85}"/>'
        )

    parts.append(f'<circle cx="{LEFT}" cy="{HEIGHT - 16}" r="7" fill="{SCAN}"/>')
    parts.append(f'<text x="{LEFT + 14}" y="{HEIGHT - 11}" fill="{INK}" font-size="13" font-family="system-ui, sans-serif">scanned</text>')
    parts.append(f'<circle cx="{LEFT + 100}" cy="{HEIGHT - 16}" r="6" fill="{PHOTO}"/>')
    parts.append(f'<text x="{LEFT + 114}" y="{HEIGHT - 11}" fill="{INK}" font-size="13" font-family="system-ui, sans-serif">photographed</text>')
    parts.append("</svg>")

    OUT.write_text("\n".join(parts) + "\n", encoding="utf-8")
    print(f"wrote {OUT.name}: {len(rows)} images over {len(sheets)} sheets")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
