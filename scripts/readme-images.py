"""The README's two pictures, NOTES-FROM-PLANNING.md entry 266: the product shot at the top and the six-screen mosaic under it.

Alan chose design B's product shot over design C's mosaic. Both are composed here from pictures the screenshot job and the phone sitting
already make, never drawn by hand, so they change when the screens do:

- the product shot: the published sample scan of a shot GroupLab sheet, tilted, behind; the desktop analysis screen in a window; the
  phone's result overlapping at the right; numbered amber callouts 1, 2 and 3;
- the mosaic: six tiles, 3 by 2, each captioned inside the picture.

Each is made in the dark and the light theme. docs/figures/readme/made-from.json records a hash of every picture they were made from, and
the site build fails when those have changed and these were not made again (the same rule as entry 253's stale check).

    python scripts/readme-images.py            make them
    python scripts/readme-images.py --check    what the site build checks
"""

from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

REPO = Path(__file__).resolve().parent.parent
OUT = REPO / "docs" / "figures" / "readme"
SCREENS = REPO / "docs" / "figures" / "screens"
FONTS = REPO / "src" / "GroupLab.App" / "Assets" / "Fonts"
STAMP = OUT / "made-from.json"
SAMPLE = REPO / "samples" / "gl-cf25-ltr-d-25-shots-600-dpi.png"

AMBER = (232, 150, 46)
THEMES = {
    "dark": {"ground": (16, 20, 24), "panel": (30, 36, 42), "text": (236, 238, 240), "dim": (150, 158, 166), "frame": (58, 66, 74)},
    "light": {"ground": (244, 241, 234), "panel": (255, 255, 255), "text": (24, 28, 32), "dim": (100, 106, 112), "frame": (206, 200, 190)},
}

# The mosaic's six tiles: caption, then the picture it shows, per theme.
TILES = [
    ("Print a sheet", "current/targets-{t}-1400x900.png"),
    ("Photograph it", "phone/fold-capture-{t}.png"),
    ("Every hole found", "current/marking-{t}-1400x900.png"),
    ("Honest numbers", "current/analysis-open-{t}-1400x900.png"),
    ("Compare loads", "current/compare-{t}-1400x900.png"),
    ("Ballistics and hit chance", "current/ballistics-hit-{t}-1400x900.png"),
]


def inputs() -> list[Path]:
    files = [SAMPLE]
    for t in THEMES:
        files += [SCREENS / f"current/analysis-{t}-1400x900.png", SCREENS / f"phone/fold-result-{t}.png"]
        files += [SCREENS / path.format(t=t) for _, path in TILES]
    return files + [Path(__file__)]


def fingerprints() -> dict[str, str]:
    return {f.relative_to(REPO).as_posix(): hashlib.sha256(f.read_bytes().replace(b"\r\n", b"\n") if f.suffix == ".py" else f.read_bytes()).hexdigest()
            for f in inputs()}


def font(size: int, weight: str = "SemiBold") -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONTS / f"IBMPlexSans-{weight}.ttf"), size)


def rounded(img: Image.Image, radius: int) -> Image.Image:
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, img.width - 1, img.height - 1], radius, fill=255)
    out = Image.new("RGBA", img.size)
    out.paste(img.convert("RGBA"), (0, 0), mask)
    return out


def shadowed(canvas: Image.Image, piece: Image.Image, at: tuple[int, int], blur: int = 18) -> None:
    shadow = Image.new("RGBA", (piece.width + 4 * blur, piece.height + 4 * blur), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle([2 * blur, 2 * blur, 2 * blur + piece.width, 2 * blur + piece.height], 16, fill=(0, 0, 0, 110))
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    canvas.alpha_composite(shadow, (at[0] - 2 * blur + 6, at[1] - 2 * blur + 10))
    canvas.alpha_composite(piece, at)


def callout(draw: ImageDraw.ImageDraw, n: int, at: tuple[int, int]) -> None:
    r = 26
    draw.ellipse([at[0] - r, at[1] - r, at[0] + r, at[1] + r], fill=AMBER, outline=(16, 20, 24), width=4)
    f = font(30, "Bold")
    draw.text(at, str(n), fill=(16, 20, 24), font=f, anchor="mm")


def product(theme: str) -> Image.Image:
    c = THEMES[theme]
    canvas = Image.new("RGBA", (1600, 900), c["ground"] + (255,))
    # 1: the shot sheet, tilted, behind.
    sheet = Image.open(SAMPLE).convert("RGB")
    sheet = sheet.resize((430, int(430 * sheet.height / sheet.width)), Image.LANCZOS)
    sheet = rounded(sheet, 6).rotate(-8, resample=Image.BICUBIC, expand=True)
    shadowed(canvas, sheet, (40, 150))
    # 2: the desktop analysis in a window.
    shot = Image.open(SCREENS / f"current/analysis-{theme}-1400x900.png").convert("RGB")
    shot = shot.resize((1000, int(1000 * shot.height / shot.width)), Image.LANCZOS)
    window = Image.new("RGB", (shot.width, shot.height + 34), c["frame"])
    window.paste(shot, (0, 34))
    bar = ImageDraw.Draw(window)
    for i, colour in enumerate([(236, 95, 88), (245, 190, 80), (98, 196, 102)]):
        bar.ellipse([14 + 22 * i, 11, 26 + 22 * i, 23], fill=colour)
    shadowed(canvas, rounded(window, 12), (330, 90))
    # 3: the phone's result, overlapping at the right.
    phone = Image.open(SCREENS / f"phone/fold-result-{theme}.png").convert("RGB")
    phone = phone.resize((300, int(300 * phone.height / phone.width)), Image.LANCZOS).crop((0, 0, 300, 640))
    body = Image.new("RGB", (phone.width + 24, phone.height + 24), (12, 12, 14))
    body.paste(phone, (12, 12))
    shadowed(canvas, rounded(body, 34), (1240, 200))
    draw = ImageDraw.Draw(canvas)
    callout(draw, 1, (80, 170))
    callout(draw, 2, (318, 76))
    callout(draw, 3, (1232, 186))
    return canvas.convert("RGB")


def mosaic(theme: str) -> Image.Image:
    c = THEMES[theme]
    tile_w, tile_h, gap, caption = 520, 330, 20, 54
    canvas = Image.new("RGB", (3 * tile_w + 4 * gap, 2 * (tile_h + caption) + 3 * gap), c["ground"])
    draw = ImageDraw.Draw(canvas)
    for i, (words, path) in enumerate(TILES):
        x, y = gap + (i % 3) * (tile_w + gap), gap + (i // 3) * (tile_h + caption + gap)
        img = Image.open(SCREENS / path.format(t=theme)).convert("RGB")
        card = Image.new("RGB", (tile_w, tile_h + caption), c["panel"])
        if img.height > img.width:
            # A phone's screen stands upright: the whole of it, fitted to the tile's height and centered.
            scale = tile_h / img.height
            img = img.resize((int(img.width * scale), tile_h), Image.LANCZOS)
            card.paste(img, ((tile_w - img.width) // 2, 0))
        else:
            scale = max(tile_w / img.width, tile_h / img.height)
            card.paste(img.resize((int(img.width * scale), int(img.height * scale)), Image.LANCZOS).crop((0, 0, tile_w, tile_h)), (0, 0))
        cd = ImageDraw.Draw(card)
        cd.text((16, tile_h + caption // 2), words, fill=c["text"], font=font(24), anchor="lm")
        canvas.paste(rounded(card, 12), (x, y), rounded(card, 12))
        draw.rectangle([x, y + tile_h, x + 5, y + tile_h + caption - 1], fill=AMBER)
    return canvas


def write() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for theme in THEMES:
        product(theme).save(OUT / f"product-{theme}.png", optimize=True)
        mosaic(theme).save(OUT / f"mosaic-{theme}.png", optimize=True)
    STAMP.write_bytes((json.dumps({"about": "The pictures the README's two images were made from, by scripts/readme-images.py.", "inputs": fingerprints()}, indent=2) + "\n").encode())


def problems() -> list[str]:
    if not STAMP.is_file():
        return ["docs/figures/readme/made-from.json is missing: run scripts/readme-images.py"]
    made = json.loads(STAMP.read_text(encoding="utf-8"))["inputs"]
    changed = [k for k, v in fingerprints().items() if made.get(k) != v]
    return [f"the README's pictures were made from older versions of {', '.join(changed)}: run scripts/readme-images.py"] if changed else []


if __name__ == "__main__":
    if sys.argv[1:] == ["--check"]:
        found = problems()
        for line in found:
            print(line)
        print("readme images: current" if not found else "readme images: stale")
        sys.exit(1 if found else 0)
    write()
    print("readme images: made")
