"""The installer wizard's small image, NOTES-FROM-PLANNING.md entry 134 section 3.

Inno Setup will only take a BMP for its wizard images, and the mark this project ships is an
icon and an SVG. So the BMPs are generated from the icon that is already approved, at the two
scalings Windows asks for, rather than anyone drawing something new.

    python packaging/windows/make-wizard-images.py

It writes the files beside the installer script and changes no design: the pixels come from
src/GroupLab.App/Assets/icons/grouplab.ico, which is mark A, flattened onto the wizard's white
because a BMP carries no transparency.
"""
from __future__ import annotations

import pathlib
import sys

try:
    from PIL import Image
except ImportError:  # pragma: no cover - only hit on a machine without Pillow
    print("this needs Pillow: python -m pip install Pillow", file=sys.stderr)
    raise SystemExit(1)

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent.parent
ICON = ROOT / "src" / "GroupLab.App" / "Assets" / "icons" / "grouplab.ico"

# The two Inno Setup asks for on a 100 percent and a 200 percent display. It picks whichever
# fits the current scaling.
SIZES = [55, 110]

# The wizard page's background in the modern style.
PAPER = (255, 255, 255)


def main() -> int:
    if not ICON.exists():
        print(f"no icon at {ICON}", file=sys.stderr)
        return 1

    with Image.open(ICON) as source:
        # The largest frame, so every smaller one is a clean reduction of it rather than a blur
        # of an already small image.
        source.size = max(source.ico.sizes())
        mark = source.convert("RGBA")

    for size in SIZES:
        scaled = mark.resize((size, size), Image.LANCZOS)
        page = Image.new("RGB", (size, size), PAPER)
        page.paste(scaled, (0, 0), scaled)
        out = HERE / f"wizard-small-{size}.bmp"
        page.save(out, "BMP")
        print(f"wrote {out.relative_to(ROOT)} ({size}x{size})")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
