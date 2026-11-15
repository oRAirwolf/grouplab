"""Google Play's phone screenshots from the phone's own screenshots, NOTES-FROM-PLANNING.md entry 487 section 3.

Each of the seven places is docs/figures/screens/phone/fold-<place>-dark.png, scaled to 1920 tall and centred on a 1080 by 1920 canvas
filled with the picture's own colour at x = 3 on the middle row, so nothing is cut off and Play's 9:16 rule is met. This is how the
first closed test's set was made on 2026-11-15; the next listing update runs it again on the newest pictures.

    python scripts/play-screens.py            writes docs/store/play-phone/01-result.png and the rest, and the tablet sets
    python scripts/play-screens.py --check    fails if any is missing or not its size

Entry 489 section 4: the tablet sets, from the Galaxy Tab S8 Ultra pictures (docs/figures/screens/phone/tab-*-landscape-dark.png), each
fitted whole on a 16:9 canvas filled the same way: 1920 by 1080 in docs/store/play-tablet/7in/ and 2560 by 1440 in
docs/store/play-tablet/10in/, both within Play's 320 to 3,840 px and at least 1,080 px a side, as promotion asks.
"""

import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[1]
SOURCE = REPO / "docs" / "figures" / "screens" / "phone"
OUT = REPO / "docs" / "store" / "play-phone"
PLACES = ["result", "capture", "fingerprint", "pairing", "targets", "scale-markers", "open-targets"]
WIDTH, HEIGHT = 1080, 1920
TABLET_PLACES = ["result", "capture", "targets", "sessions", "settings"]
TABLETS = {"7in": (1920, 1080), "10in": (2560, 1440)}
TABLET_OUT = REPO / "docs" / "store" / "play-tablet"


def fit(source: Path, size: tuple[int, int]) -> Image.Image:
    """The whole picture scaled to fit, centred, on a canvas of its own colour at x = 3 on the middle row."""
    picture = Image.open(source).convert("RGB")
    scale = min(size[0] / picture.width, size[1] / picture.height)
    scaled = picture.resize((round(picture.width * scale), round(picture.height * scale)), Image.LANCZOS)
    canvas = Image.new("RGB", size, picture.getpixel((3, picture.height // 2)))
    canvas.paste(scaled, ((size[0] - scaled.width) // 2, (size[1] - scaled.height) // 2))
    return canvas


def name(i: int, place: str) -> str:
    return f"{i:02d}-{place}.png"


def make(source: Path) -> Image.Image:
    picture = Image.open(source).convert("RGB")
    scaled = picture.resize((round(picture.width * HEIGHT / picture.height), HEIGHT), Image.LANCZOS)
    if scaled.width > WIDTH:
        raise SystemExit(f"{source.name} is wider than 9:16 once {HEIGHT} tall; it would be cut")
    fill = picture.getpixel((3, picture.height // 2))
    canvas = Image.new("RGB", (WIDTH, HEIGHT), fill)
    canvas.paste(scaled, ((WIDTH - scaled.width) // 2, 0))
    return canvas


def main() -> int:
    if "--check" in sys.argv:
        bad = []
        for i, place in enumerate(PLACES, 1):
            path = OUT / name(i, place)
            if not path.is_file() or Image.open(path).size != (WIDTH, HEIGHT):
                bad.append(path.name)
        for kind, size in TABLETS.items():
            for i, place in enumerate(TABLET_PLACES, 1):
                path = TABLET_OUT / kind / name(i, place)
                if not path.is_file() or Image.open(path).size != size:
                    bad.append(f"{kind}/{path.name}")
        print("play screenshots: " + ("missing or wrong size: " + ", ".join(bad) if bad else
              f"{len(PLACES)} at {WIDTH} by {HEIGHT}, and {len(TABLET_PLACES)} for each tablet size"))
        return 1 if bad else 0
    OUT.mkdir(parents=True, exist_ok=True)
    for i, place in enumerate(PLACES, 1):
        make(SOURCE / f"fold-{place}-dark.png").save(OUT / name(i, place), optimize=True)
    for kind, size in TABLETS.items():
        (TABLET_OUT / kind).mkdir(parents=True, exist_ok=True)
        for i, place in enumerate(TABLET_PLACES, 1):
            fit(SOURCE / f"tab-{place}-landscape-dark.png", size).save(TABLET_OUT / kind / name(i, place), optimize=True)
    print(f"wrote {len(PLACES)} screenshots to {OUT.relative_to(REPO)} and {len(TABLET_PLACES)} for each of {', '.join(TABLETS)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
