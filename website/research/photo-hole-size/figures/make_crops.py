#!/usr/bin/env python3
"""Three crops of real holes, with the measurement drawn on them.

NOTES-FROM-PLANNING.md entry 153 section 5, on the research section: "Show examples, graphics, and
measurements where applicable. Being able to visualize something is much easier than just reading about
it." The article said a hole measures 0.9 to 1.5 times the bullet and showed nobody a hole.

**The only real material this project may publish is the sample scan**, `samples/gl-cf25-ltr-d-25-shots-600-dpi.png`,
under the consent record in `samples/PROVENANCE.md`. Every other scan and every photograph from that
range day stays private, so these three crops are all scans. That limit is stated in the captions rather
than worked around: the shadow case section 5 asks for exists only in a photograph, and there is no
photograph anybody has consented to publish a crop of.

The numbers are not typed in. They come from `grouplab analyze` on that scan, so a crop cannot drift
from the figure the article quotes.

It reads its three measurements from `hole-crops.json` beside the article, which the analyser produced once.
Entry 160 section 4.4: do not re-derive a measurement that is already written down, and the site build runs
every figure script on every build.

    python3 make_crops.py                    draw the crops from the recorded measurements
    python3 make_crops.py <marking.json>     rewrite the record from a fresh `grouplab analyze --json`
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent.parent.parent
SCAN = REPO / "samples" / "gl-cf25-ltr-d-25-shots-600-dpi.png"

DPI = 600
NOMINAL = 0.264          # 6.5 Creedmoor, the cartridge written on this sheet's load block.
CROP_INCHES = 0.75       # About three hole widths, so the hole is the subject and the paper gives it scale.

INK = (20, 22, 26)
RULE = (196, 58, 44)
PAPER = (255, 255, 255)


def font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for name in ("seguisb.ttf", "segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def crop(image: Image.Image, shot: dict, name: str) -> Path:
    """One hole, with a caliper line across its measured diameter and a scale bar under it."""
    measured = shot["measuredDiameterInches"]
    cx, cy = shot["image"]["x"], shot["image"]["y"]
    half = int(CROP_INCHES * DPI / 2)

    box = (int(cx) - half, int(cy) - half, int(cx) + half, int(cy) + half)
    out = image.crop(box).convert("RGB")

    # Drawn at the crop's own resolution and then reduced, so the lines and the text come out smooth
    # without carrying a 450 by 450 pixel scan of paper grain into the page.
    scale = 2
    out = out.resize((out.width * scale, out.height * scale), Image.LANCZOS)
    d = ImageDraw.Draw(out)
    mid = out.width // 2
    px = measured * DPI * scale

    # The caliper, across the hole at its measured width, with the ticks a caliper's jaws would make.
    y = mid
    left, right = mid - px / 2, mid + px / 2
    d.line([(left, y), (right, y)], fill=RULE, width=3)
    for x in (left, right):
        d.line([(x, y - 22), (x, y + 22)], fill=RULE, width=3)

    label = f"{measured:.3f} in"
    f = font(30)
    w = d.textlength(label, font=f)
    d.rectangle([(mid - w / 2 - 8, y - 68), (mid + w / 2 + 8, y - 28)], fill=PAPER)
    d.text((mid - w / 2, y - 64), label, fill=RULE, font=f)

    # A tenth of an inch, so the reader can check every other length on the picture against something.
    bar = 0.1 * DPI * scale
    bx, by = out.width - bar - 30, out.height - 40
    d.line([(bx, by), (bx + bar, by)], fill=INK, width=4)
    for x in (bx, bx + bar):
        d.line([(x, by - 10), (x, by + 10)], fill=INK, width=4)
    small = font(24)
    d.text((bx, by - 36), "0.1 in", fill=INK, font=small)

    out = out.resize((out.width // scale * 2, out.height // scale * 2), Image.LANCZOS)
    path = HERE / f"{name}.png"
    out.save(path, optimize=True)
    return path


RECORD = HERE.parent / "hole-crops.json"


def rewrite_record(marking: Path) -> None:
    """The three holes and their measurements, from a fresh analysis, so the record can be remade."""
    shots = sorted(json.loads(marking.read_text(encoding="utf-8"))["shots"], key=lambda s: s["measuredDiameterInches"])
    picked = {"hole-smallest": shots[0], "hole-typical": shots[len(shots) // 2], "hole-largest": shots[-1]}
    record = json.loads(RECORD.read_text(encoding="utf-8"))
    record["holes"] = [
        {"name": name, "shot": s["id"], "x": round(s["image"]["x"], 2), "y": round(s["image"]["y"], 2),
         "measuredInches": round(s["measuredDiameterInches"], 4),
         "fractionOfBullet": round(s["measuredDiameterInches"] / NOMINAL, 3)}
        for name, s in picked.items()
    ]
    RECORD.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(f"{RECORD.name} rewritten from {marking.name}")


def main() -> int:
    if len(sys.argv) > 1:
        rewrite_record(Path(sys.argv[1]))

    record = json.loads(RECORD.read_text(encoding="utf-8"))
    image = Image.open(SCAN)
    for hole in record["holes"]:
        shot = {"id": hole["shot"], "image": {"x": hole["x"], "y": hole["y"]},
                "measuredDiameterInches": hole["measuredInches"]}
        path = crop(image, shot, hole["name"])
        print(f"{path.name}: shot {hole['shot']}, {hole['measuredInches']:.4f} in, "
              f"{hole['fractionOfBullet']:.3f} of the {NOMINAL} in bullet")
    return 0


if __name__ == "__main__":
    sys.exit(main())
