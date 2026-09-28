#!/usr/bin/env python3
"""The Android application's icons, from the desktop's mark.

NOTES-FROM-PLANNING.md entry 248. Alan: "Can you make the app icon match the desktop icon? Right now it is a generic android icon." The
mark is `src/GroupLab.App/Assets/grouplab-mark.svg`, circles only, which is all this reads; the desktop's `.ico`, `.icns` and Linux PNGs
come from the same file through `grouplab icons`. Run once when the mark changes, and commit what it writes:

    python3 scripts/android-icons.py

It writes, under android/GroupLab.Android/Resources:

- an adaptive icon (`mipmap-anydpi-v26/ic_launcher.xml` and `ic_launcher_round.xml`): the desktop's dark `bg` behind, the mark in front
  inside the 66 dp safe zone so no launcher's mask crops it, and a monochrome layer for Android 13's themed icons;
- PNG fallbacks at every density, square and round, for Android 10 to 12 launchers that do not take an adaptive icon;
- the same set for GroupLab Dev, `ic_launcher_dev`, on the light `bg` with the light mark, so the two are never confused on a home screen;

and, in docs/store, the Play listing's 512 by 512 icon and its 1024 by 500 feature graphic.

Each size is drawn at eight times its size and averaged down, as the desktop's icons are, rather than shrunk from a large one.
"""

from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent.parent
MARK = HERE / "src" / "GroupLab.App" / "Assets" / "grouplab-mark.svg"
MARK_LIGHT = HERE / "src" / "GroupLab.App" / "Assets" / "grouplab-mark-light.svg"
RES = HERE / "android" / "GroupLab.Android" / "Resources"
STORE = HERE / "docs" / "store"
FONTS = HERE / "src" / "GroupLab.App" / "Assets" / "Fonts"

# The desktop's window colors, Tokens.Dark.Bg and Tokens.Light.Bg (src/GroupLab.App/Theme/Tokens.cs).
DARK_BG = "#131417"
LIGHT_BG = "#F4F3F0"
AMBER = "#E0912F"

DENSITIES = {"mdpi": 1.0, "hdpi": 1.5, "xhdpi": 2.0, "xxhdpi": 3.0, "xxxhdpi": 4.0}
SUPERSAMPLE = 8


def circles(svg: Path) -> list[dict]:
    """The mark's circles in its own units, each with its fill or its stroke."""
    root = ET.parse(svg).getroot()
    out = []
    for e in root.iter():
        if e.tag.split("}")[-1] != "circle":
            continue
        out.append({
            "x": float(e.get("cx")), "y": float(e.get("cy")), "r": float(e.get("r")),
            "fill": None if e.get("fill") in (None, "none") else e.get("fill"),
            "stroke": e.get("stroke"), "width": float(e.get("stroke-width") or 0),
        })
    return out


def reach(marks: list[dict]) -> float:
    """How far the mark reaches from its centre, (50, 50), in its units."""
    return max(((c["x"] - 50) ** 2 + (c["y"] - 50) ** 2) ** 0.5 + c["r"] + c["width"] / 2 for c in marks)


def draw_mark(image: Image.Image, marks: list[dict], cx: float, cy: float, scale: float, one_color: str | None = None) -> None:
    """The mark centred at (cx, cy), `scale` pixels to its unit, in its own colors or all in one."""
    for c in marks:
        x, y = cx + (c["x"] - 50) * scale, cy + (c["y"] - 50) * scale
        if c["fill"]:
            r = c["r"] * scale
            ImageDraw.Draw(image).ellipse([x - r, y - r, x + r, y + r], fill=one_color or c["fill"])
        elif c["stroke"]:
            outer, inner = (c["r"] + c["width"] / 2) * scale, (c["r"] - c["width"] / 2) * scale
            ring = Image.new("L", image.size, 0)
            d = ImageDraw.Draw(ring)
            d.ellipse([x - outer, y - outer, x + outer, y + outer], fill=255)
            d.ellipse([x - inner, y - inner, x + inner, y + inner], fill=0)
            image.paste(Image.new("RGBA", image.size, one_color or c["stroke"]), (0, 0), ring)


def render(size: int, background: str | None, shape: str, marks: list[dict], mark_radius: float, one_color: str | None = None) -> Image.Image:
    """One icon image, `size` pixels square: a background of the shape given, or none, and the mark reaching `mark_radius` of it."""
    big = size * SUPERSAMPLE
    image = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    if background:
        d = ImageDraw.Draw(image)
        if shape == "circle":
            d.ellipse([0, 0, big - 1, big - 1], fill=background)
        elif shape == "rounded":
            d.rounded_rectangle([0, 0, big - 1, big - 1], radius=big * 0.18, fill=background)
        else:
            d.rectangle([0, 0, big, big], fill=background)
    draw_mark(image, marks, big / 2, big / 2, mark_radius * SUPERSAMPLE / reach(marks), one_color)
    return image.resize((size, size), Image.LANCZOS)


def save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def adaptive(name: str) -> str:
    return ("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            "<!-- Written by scripts/android-icons.py from the desktop's mark (entry 248); do not edit by hand. -->\n"
            "<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n"
            f"  <background android:drawable=\"@color/{name}_background\" />\n"
            f"  <foreground android:drawable=\"@mipmap/{name}_foreground\" />\n"
            f"  <monochrome android:drawable=\"@mipmap/{name}_monochrome\" />\n"
            "</adaptive-icon>\n")


def write_set(name: str, background: str, marks: list[dict]) -> list[str]:
    written = []
    for density, scale in DENSITIES.items():
        folder = RES / f"mipmap-{density}"
        # The adaptive layers are 108 dp; the safe zone is the middle 66, and the mark stays a little inside it.
        layer = round(108 * scale)
        save(render(layer, None, "none", marks, layer * 30 / 108), folder / f"{name}_foreground.png")
        save(render(layer, None, "none", marks, layer * 30 / 108, one_color="#FFFFFF"), folder / f"{name}_monochrome.png")
        # The fallbacks are 48 dp, the mark reaching most of the way to the edge.
        legacy = round(48 * scale)
        save(render(legacy, background, "rounded", marks, legacy * 0.40), folder / f"{name}.png")
        save(render(legacy, background, "circle", marks, legacy * 0.36), folder / f"{name}_round.png")
        written.append(density)
    for variant in (name, f"{name}_round"):
        path = RES / "mipmap-anydpi-v26" / f"{variant}.xml"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(adaptive(name).encode())
    colors = RES / "values" / f"{name}_background.xml"
    colors.write_bytes(("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                        "<!-- Written by scripts/android-icons.py (entry 248): the desktop's window color behind the mark. -->\n"
                        f"<resources>\n  <color name=\"{name}_background\">{background}</color>\n</resources>\n").encode())
    return written


def store(marks: list[dict]) -> None:
    STORE.mkdir(parents=True, exist_ok=True)
    save(render(512, DARK_BG, "square", marks, 512 * 0.36).convert("RGB"), STORE / "play-icon-512.png")

    # The feature graphic: the mark and the wordmark as the desktop's header sets them, GROUP in grey and LAB in amber.
    w, h = 1024 * 2, 500 * 2
    image = Image.new("RGBA", (w, h), DARK_BG)
    draw_mark(image, marks, 230 * 2, h / 2, 140 * 2 / reach(marks))
    d = ImageDraw.Draw(image)

    # The wordmark as large as fits between the mark and a margin the same as the mark's, measured rather than guessed.
    x, y, room = 430 * 2, h / 2, (1024 - 430 - 90) * 2
    size = 200 * 2
    while True:
        font = ImageFont.truetype(str(FONTS / "IBMPlexSansCondensed-Bold.ttf"), size)
        if d.textlength("GROUPLAB", font=font) <= room:
            break
        size -= 4
    d.text((x, y), "GROUP", font=font, fill="#8A9199", anchor="lm")
    d.text((x + d.textlength("GROUP", font=font), y), "LAB", font=font, fill=AMBER, anchor="lm")
    save(image.resize((1024, 500), Image.LANCZOS).convert("RGB"), STORE / "play-feature-1024x500.png")


def main() -> int:
    marks = circles(MARK)
    dens = write_set("ic_launcher", DARK_BG, marks)
    write_set("ic_launcher_dev", LIGHT_BG, circles(MARK_LIGHT))
    store(marks)
    print(f"android icons from {MARK.name}: ic_launcher and ic_launcher_dev at {', '.join(dens)}, adaptive with monochrome; "
          f"docs/store/play-icon-512.png and play-feature-1024x500.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
