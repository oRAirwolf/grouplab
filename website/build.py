#!/usr/bin/env python3
"""Build the grouplab.org static site.

NOTES-FROM-PLANNING.md entry 128 section 1. The site lives in this repository now, so this
reads the repository it sits in and writes into website/_site/, which git ignores. Every path
is relative to the repository root: nothing here knows where the repository is on disk.

Run:  python website/build.py
Needs: Python 3.9+, and the packages markdown, Pillow, fonttools, brotli.

It only ever builds. Publishing is .github/workflows/website.yml, which a person starts by
hand and nothing else may start.

Nothing in this script or its output may contain an IP address, a password, a key or a token.
No em dash appears anywhere in the output. The build fails on any of those, on a banned term,
on a link to releases/latest or v0.1.0, and if the download buttons do not point at the
rolling nightly release.
"""

from __future__ import annotations

import datetime
import hashlib
import html
from html.parser import HTMLParser
import json
import math
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

import markdown
from fontTools.ttLib import TTFont
from PIL import Image

# NOTES-FROM-PLANNING.md entry 159 section 5.2: a number that counts a thing in this repository is computed from
# the thing it counts. The home page counted the two tiled layouts as sheets while the tour did not, both typed by hand.
import importlib.util as _importlib_util

_spec = _importlib_util.spec_from_file_location("counts", Path(__file__).resolve().parent.parent / "scripts" / "counts.py")
_counts = _importlib_util.module_from_spec(_spec)
_spec.loader.exec_module(_counts)
COUNTS = _counts.counts()

# Entry 266: the README's two pictures, held to the pictures they are made from the same way.
_spec = _importlib_util.spec_from_file_location("readme_images", Path(__file__).resolve().parent.parent / "scripts" / "readme-images.py")
_readme_images = _importlib_util.module_from_spec(_spec)
_spec.loader.exec_module(_readme_images)

# Entry 253 section 5: the build fails when a screenshot no longer shows the application (scripts/screens-stamp.py says how).
_spec = _importlib_util.spec_from_file_location("screens_stamp", Path(__file__).resolve().parent.parent / "scripts" / "screens-stamp.py")
_screens_stamp = _importlib_util.module_from_spec(_spec)
_spec.loader.exec_module(_screens_stamp)

# Entry 284: Behind the curtain, the map of how GroupLab works and its three deep dives, from website/how-it-works.json.
_spec = _importlib_util.spec_from_file_location("how_it_works", Path(__file__).resolve().parent / "how_it_works.py")
_how_it_works = _importlib_util.module_from_spec(_spec)
_spec.loader.exec_module(_how_it_works)


def count_words(name: str, capital: bool = False) -> str:
    said = _counts.words(COUNTS[name])
    return said[:1].upper() + said[1:] if capital else said

# ---------------------------------------------------------------- settings

SITE_URL = "https://grouplab.org"
GITHUB = "https://github.com/oRAirwolf/grouplab"
NIGHTLY = GITHUB + "/releases/download/nightly/"
# Entries 335 to 337: the Microsoft Store's listing and the TestFlight Public Beta's invitation, published with Alan's approval on 2026-10-01.
STORE = "https://apps.microsoft.com/detail/9NWJCXBKZNPZ"


def store_version() -> str | None:
    """The version the Microsoft Store carries, as the Store check last wrote it in docs/notes/external-status.md (entries 336 and 338):
    "0.2.0" for a package version "0.2.0.0"; None where the check has not recorded one."""
    status = Path(__file__).resolve().parent.parent / "docs" / "notes" / "external-status.md"
    if not status.exists():
        return None
    m = re.search(r"The Store carries package version (\d+)\.(\d+)\.(\d+)\.\d+", status.read_text(encoding="utf-8"))
    return ".".join(m.groups()) if m else None
TESTFLIGHT = "https://testflight.apple.com/join/A3xyT6C6"
# Entry 129 section 1: the upload page moves to grouplab.org. It stays pointed at the old one until
# the receiver is installed on the server and tested, because a button that leads nowhere is worse
# than one that leads somewhere old. Entry 129 section 6.2 flips it and redirects the old page.

# The Turnstile site key is the public half and belongs in the page. The secret half lives only on
# the server, in a file with mode 600, put there by grouplab-set-turnstile-secret. It is never in
# this repository.
TURNSTILE_SITE_KEY = "0x4AAAAAAE-rzvtx_a4nBHHr"
SUPPORT_EMAIL = "support@grouplab.org"
# Donations: the Support page carries a section for them, hidden until this
# is set to a real address, for example "https://github.com/sponsors/...".
DONATION_URL: str | None = None
DONATION_LABEL = "Support GroupLab"

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
OUT = HERE / "_site"
DONOR = HERE / "donor"

SCREENS = REPO / "docs" / "figures" / "screens" / "current"
# Entry 246: the phone's own screenshots in look B, one per theme, the status bar already cut off.
PHONE_SCREENS = REPO / "docs" / "figures" / "screens" / "phone"
ASSETS = REPO / "src" / "GroupLab.App" / "Assets"

# The stable asset names the rolling nightly release always serves. The Download buttons must
# point at these and nothing else: a versioned name would be a link that rots in a week.
NIGHTLY_ASSETS = ["grouplab-setup-win-x64.exe", "grouplab-win-x64.zip", "grouplab-linux-x64.tar.gz"]


def site_commit() -> str:
    """The commit this site was built from, which every page carries so the server can prove what it is serving.

    The checkout's own HEAD first, because that is what was built and what website.yml checks every page against. The workflow checks out
    main, and a status commit landing between a dispatch and its checkout once left every page carrying the dispatched commit while HEAD
    had moved on, so nothing published (2026-10-01). GITHUB_SHA is the fallback where there is no repository to ask."""
    try:
        out = subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=str(REPO), capture_output=True, text=True, check=True, timeout=30
        )
        return out.stdout.strip()[:40]
    except (OSError, subprocess.SubprocessError):
        return (os.environ.get("GITHUB_SHA") or "unknown")[:40]


COMMIT = site_commit()

BANNED = [
    "\u2014",  # em dash
    "ontarget",
    "harmonic",
    "barrel time",
    "velocity node",
    "accuracy node",
]

# ---------------------------------------------------------------- helpers


def esc(s: str) -> str:
    return html.escape(s, quote=True)


def write(rel: str, text: str) -> None:
    path = OUT / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def copy(src: Path, rel: str) -> None:
    dst = OUT / rel
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(src, dst)


def need(path: Path) -> Path:
    if not path.exists():
        sys.exit(f"build: missing input {path}")
    return path


# ---------------------------------------------------------------- assets


FONT_FILES = {
    "plex-sans-400": "IBMPlexSans-Regular.ttf",
    "plex-sans-500": "IBMPlexSans-Medium.ttf",
    "plex-sans-600": "IBMPlexSans-SemiBold.ttf",
    "plex-sans-condensed-700": "IBMPlexSansCondensed-Bold.ttf",
    "plex-mono-400": "IBMPlexMono-Regular.ttf",
    "plex-mono-500": "IBMPlexMono-Medium.ttf",
}


def build_fonts() -> None:
    for name, file in FONT_FILES.items():
        font = TTFont(need(ASSETS / "Fonts" / file))
        font.flavor = "woff2"
        dst = OUT / "assets" / "fonts" / f"{name}.woff2"
        dst.parent.mkdir(parents=True, exist_ok=True)
        font.save(dst)
    copy(need(ASSETS / "Fonts" / "LICENSE.txt"), "assets/fonts/LICENSE.txt")


def clean_svg(src: Path) -> str:
    s = need(src).read_text(encoding="utf-8")
    s = re.sub(r"<metadata>.*?</metadata>", "", s, flags=re.S)
    s = s.replace(' xmlns:c2pa="http://c2pa.org/manifest"', "")
    return s


def build_images() -> None:
    for name in ["grouplab-mark", "grouplab-mark-light", "grouplab-lockup", "grouplab-lockup-light"]:
        write(f"assets/img/{name}.svg", clean_svg(ASSETS / f"{name}.svg"))
    write("favicon.svg", clean_svg(ASSETS / "grouplab-mark.svg"))
    # Entries 336 and 337: Microsoft's own "Get it from Microsoft" badge, unaltered, served from this site so no visitor is sent to Microsoft
    # just by opening the page.
    for theme in ("dark", "light"):
        write(f"assets/img/get-it-from-microsoft-{theme}.svg", need(HERE / "badges" / f"get-it-from-microsoft-{theme}.svg").read_text(encoding="utf-8"))
    copy(need(ASSETS / "icons" / "grouplab.ico"), "favicon.ico")
    icon = Image.open(need(ASSETS / "icons" / "linux" / "grouplab-256.png")).convert("RGBA")
    icon.resize((180, 180), Image.LANCZOS).save(OUT / "apple-touch-icon.png", optimize=True)

    # Every screen in both themes at 1400 by 900, as WebP. Entry 300 section 5: lossless, so nothing is softened, which on these flat
    # screens is also smaller than quality 90 was: about 73 KB a screen against 132 KB, measured 2026-09-30 on twelve of them.
    for png in sorted(SCREENS.glob("*-1400x900.png")):
        img = Image.open(png).convert("RGB")
        dst = OUT / "assets" / "screens" / (png.stem + ".webp")
        dst.parent.mkdir(parents=True, exist_ok=True)
        img.save(dst, "WEBP", lossless=True, method=6)

    # Entry 256: the bulls, grids and sheets as they print, one picture for both themes, on their own white paper. Entry 300 section 5: a
    # bull or a grid also comes as vectors from the same scene as its PDF, served instead of the picture wherever it is shown.
    for png in sorted(SCREENS.glob("sheet-*.png")):
        img = Image.open(png).convert("RGB")
        dst = OUT / "assets" / "screens" / (png.stem + ".webp")
        dst.parent.mkdir(parents=True, exist_ok=True)
        img.save(dst, "WEBP", lossless=True, method=6)
    for svg in sorted(SCREENS.glob("sheet-*.svg")):
        shutil.copyfile(svg, OUT / "assets" / "screens" / svg.name)

    # The phone's screens at half their size, which is still sharper than any page shows them. They stay at quality 90: with the camera and
    # photographs in them, lossless would be 3.6 MB for the 28 of them against 1.5 MB (entry 300, measured 2026-09-30).
    for png in sorted(PHONE_SCREENS.glob("*.png")):
        img = Image.open(png).convert("RGB")
        img = img.resize((img.width // 2, img.height // 2), Image.LANCZOS)
        dst = OUT / "assets" / "screens" / "phone" / (png.stem + ".webp")
        dst.parent.mkdir(parents=True, exist_ok=True)
        img.save(dst, "WEBP", quality=90, method=6)

    # Entry 440 section 3.1: the test page's orbit and cards carry smaller copies than the 1400 pixel screens, sized to how large they are
    # shown, so the page weighs less than the home page it may replace.
    for name, width, folder in [(n, 360, "small") for n in sorted(set(HOME_INNER + HOME_OUTER))] + [(c[3], 560, "card") for c in HOME_CARDS]:
        for theme in ("dark", "light"):
            src = PHONE_SCREENS / f"{name}-{theme}.png" if name.startswith("fold-") else SCREENS / f"{name}-{theme}-1400x900.png"
            img = Image.open(need(src)).convert("RGB")
            img = img.resize((width, round(img.height * width / img.width)), Image.LANCZOS)
            if folder == "small":
                # An orbit slot shows the top of its picture at 230 by 148, so a phone's tall screen is cut to that shape here.
                img = img.crop((0, 0, width, min(img.height, round(width * 148 / 230))))
            dst = OUT / "assets" / "screens" / folder / f"{name}-{theme}.webp"
            dst.parent.mkdir(parents=True, exist_ok=True)
            img.save(dst, "WEBP", quality=74, method=6)

    # Link preview image, 1200 by 630, cut from the dark analysis screen.
    img = Image.open(need(SCREENS / "analysis-dark-1400x900.png")).convert("RGB")
    w, h = img.size
    crop = img.crop((0, 0, w, int(w * 630 / 1200)))
    crop.resize((1200, 630), Image.LANCZOS).save(OUT / "assets" / "img" / "og.png", optimize=True)

    # Entry 438 section 4.2: the statistics primer's own link preview, its title beside one of its groups, in the site's dark theme.
    from PIL import ImageDraw, ImageFont
    card = Image.new("RGB", (1200, 630), "#131417")
    draw = ImageDraw.Draw(card)
    fonts = REPO / "src" / "GroupLab.App" / "Assets" / "Fonts"
    big, small = ImageFont.truetype(str(need(fonts / "IBMPlexSans-Bold.ttf")), 64), ImageFont.truetype(str(need(fonts / "IBMPlexSans-Regular.ttf")), 30)
    for i, text in enumerate(("Statistics for", "shooters, from", "the beginning")):
        draw.text((70, 150 + i * 80), text, font=big, fill="#e6e8ea")
    draw.text((70, 430), "Group size, sigma, CEP and p-values, in plain words.", font=small, fill="#9aa1a9")
    draw.text((70, 520), "GROUPLAB", font=small, fill="#e0912f")
    shots = primer()["groups"]["20"][0]["shots"]
    for r in (60, 120, 180):
        draw.ellipse((930 - r, 315 - r, 930 + r, 315 + r), outline="#3a3f47", width=2)
    for x, y in shots:
        draw.ellipse((930 + x * 120 - 8, 315 - y * 120 - 8, 930 + x * 120 + 8, 315 - y * 120 + 8), fill="#e0912f")
    card.save(OUT / "assets" / "img" / "og-primer.png", optimize=True)


def build_downloads() -> None:
    # Entry 264: one PDF a sheet, each the sheet and its page of instructions, made by `grouplab donor-pack` from the library
    # (DonorPackTests holds them to it), and the whole pack as one zip with fixed dates, so the same PDFs make the same file.
    import zipfile
    files = donor_files()
    for pdf in files:
        copy(need(DONOR / pdf), f"donor/{pdf}")
    (OUT / "donor").mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(OUT / "donor" / "grouplab-donor-pack.zip", "w", zipfile.ZIP_DEFLATED) as pack:
        for pdf in files:
            info = zipfile.ZipInfo(pdf, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            pack.writestr(info, (DONOR / pdf).read_bytes())
    copy(need(REPO / "docs" / "USER-GUIDE.pdf"), "guides/user-guide.pdf")
    copy(need(REPO / "docs" / "TESTING-GUIDE.pdf"), "guides/testing-guide.pdf")
    copy(need(REPO / "docs" / "STATISTICS-PRIMER.pdf"), "guides/statistics-primer.pdf")
    for _, source, published in PRIMER_SUMMARIES:
        copy(need(REPO / "docs" / source), f"guides/{published}")
    # The primer's page fetches its numbers from beside itself, so "Shoot five more" can show groups GroupLab drew.
    copy(need(PRIMER_DATA), "guides/statistics-primer/primer-data.json")


# ---------------------------------------------------------------- page shell

# NOTES-FROM-PLANNING.md entry 148 section 3: the Discord invite is written down in exactly one place. Everything
# published points at the canonical address, which redirects to the invite, so replacing the invite later is a single
# edit and no published link ever breaks.
LINKS = REPO / "website" / "links.json"


def links() -> dict:
    return json.loads(need(LINKS).read_text(encoding="utf-8"))


DISCORD = "/discord/"


# NOTES-FROM-PLANNING.md entry 173 section 2: the upload page's one address. The old path keeps a plain page that links here.
SEND = "/targets/"
SEND_BEFORE = "/shoot-a-target/send/"

NAV = [
    ("Download", "/download/"),
    ("Tour", "/tour/"),
    ("Features", "/features/"),
    ("Shoot a target", "/shoot-a-target/"),
    ("Guides", "/guides/"),
    ("Research", "/research/"),
    ("Community", DISCORD),
    ("Release notes", "/releases/"),
    ("Support", "/support/"),
]

def nav() -> list[tuple[str, str]]:
    """The top bar. Entry 173 section 2.3: while the upload page is open, "Shoot a target" becomes "Send a target" and goes to it; the
    donor pack keeps its place in the footer. While it is closed the bar is exactly as it was, so the link can never point at a page
    that does not answer."""
    if not limits().get("open"):
        return NAV
    return [("Send a target", SEND) if href == "/shoot-a-target/" else (label, href) for label, href in NAV]


# NOTES-FROM-PLANNING.md entry 142 section 3: the index is grouped, and the groups read in this order rather than
# alphabetically, because somebody arriving wants to know what a target says before they want to know how it is built.
RESEARCH_GROUPS = ["Reading targets", "Measuring groups", "Range tests", "Guides", "How GroupLab is built"]

ICON_GITHUB = '<svg class="ico" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="currentColor"><path d="M12 2a10 10 0 0 0-3.16 19.49c.5.09.68-.22.68-.48v-1.7c-2.78.6-3.37-1.34-3.37-1.34-.45-1.16-1.11-1.47-1.11-1.47-.91-.62.07-.61.07-.61 1 .07 1.53 1.03 1.53 1.03.9 1.52 2.34 1.08 2.91.83.09-.65.35-1.08.63-1.33-2.22-.25-4.55-1.11-4.55-4.94 0-1.09.39-1.98 1.03-2.68-.1-.25-.45-1.27.1-2.64 0 0 .84-.27 2.75 1.02a9.5 9.5 0 0 1 5 0c1.91-1.29 2.75-1.02 2.75-1.02.55 1.37.2 2.39.1 2.64.64.7 1.03 1.59 1.03 2.68 0 3.84-2.34 4.68-4.57 4.93.36.31.68.92.68 1.85v2.74c0 .27.18.58.69.48A10 10 0 0 0 12 2z"/></svg>'
ICON_THEME = '<svg class="ico" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="12" cy="12" r="4.5"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>'
ICON_MENU = '<svg class="ico" width="22" height="22" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M4 7h16M4 12h16M4 17h16"/></svg>'
ICON_PDF = '<svg class="ico pdf" width="28" height="34" viewBox="0 0 28 34" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M2 2h16l8 8v22H2z"/><path d="M18 2v8h8"/><path d="M7 19h14M7 24h14M7 14h6"/></svg>'



# ---------------------------------------------------------------- glossary

# NOTES-FROM-PLANNING.md entry 154. Alan: "anything like the word Sigma that is not commonly understood by a layman in either the
# website or application should create a tooltip popup that explains what it means." The words and their plain sentences are the
# application's own list, read here from the same file, so the site and the screen cannot say different things.
GLOSSARY_FILE = REPO / "src" / "GroupLab.Core" / "Marking" / "glossary.json"
GLOSSARY_PATH = "/guides/glossary/"


def glossary() -> list[dict]:
    return json.loads(GLOSSARY_FILE.read_text(encoding="utf-8"))["terms"]


def _glossary_words() -> tuple[re.Pattern, dict]:
    by_word = {}
    for term in glossary():
        for word in term["words"]:
            by_word.setdefault(word.lower(), term)
    words = sorted(by_word, key=len, reverse=True)
    return re.compile(r"(?<![\w-])(" + "|".join(re.escape(w) for w in words) + r")(?![\w-])", re.IGNORECASE), by_word


GLOSSARY_WORDS, GLOSSARY_BY_WORD = _glossary_words()

# Text inside these is never marked: a link is already a link, a heading is a label rather than a sentence, code is literal, and a
# control or a navigation list is not prose.
TERM_SKIP = {"a", "code", "pre", "h1", "h2", "h3", "h4", "h5", "h6", "script", "style", "button", "summary", "label", "svg", "nav",
             "select", "option", "textarea", "figcaption", "title"}
VOID = {"br", "img", "hr", "input", "meta", "link", "source", "wbr", "col", "area", "base", "embed", "param", "track"}


# Entry 440: a decorative label, such as the test page's strip of feature names, opts out of glossary links with this attribute on an element
# holding only text and spans; the words are explained where they are read as prose.
NO_TERMS = 'data-terms="off"'


def explain(body: str) -> str:
    """Entry 154 section 2: the first appearance of each glossary term in a page's body becomes a link to its entry, carrying the plain
    sentence, which terms.js shows on hover and on tap. Only the first appearance, because a page with every "sigma" underlined is
    unreadable, and a link, because a reader with scripts off still reaches the explanation."""
    out, skip, done = [], [], set()
    for token in re.split(r"(<[^>]+>)", body):
        if token.startswith("<"):
            m = re.match(r"<(/?)([a-zA-Z0-9]+)", token)
            if m and not token.startswith("<!"):
                name = m.group(2).lower()
                if m.group(1):
                    if skip and skip[-1] == name:
                        skip.pop()
                elif (name in TERM_SKIP or NO_TERMS in token) and name not in VOID and not token.endswith("/>"):
                    skip.append(name)
            out.append(token)
            continue
        if skip or not token.strip():
            out.append(token)
            continue
        pieces, last = [], 0
        for m in GLOSSARY_WORDS.finditer(token):
            term = GLOSSARY_BY_WORD[m.group(1).lower()]
            if term["term"] in done:
                continue
            done.add(term["term"])
            pieces.append(token[last:m.start()])
            pieces.append(f'<a class="term" href="{GLOSSARY_PATH}#{term["term"]}" data-term="{term["term"]}" data-name="{esc(term["name"])}" '
                          f'data-plain="{esc(term["plain"])}">{m.group(1)}</a>')
            last = m.end()
        pieces.append(token[last:])
        out.append("".join(pieces))
    return "".join(out)


def page_glossary() -> str:
    entries = []
    for term in sorted(glossary(), key=lambda x: x["name"].lower()):
        more = [f'<p class="small"><span class="mono faint">Precisely:</span> {esc(term["precise"])}</p>'] if term.get("precise") else []
        if term.get("article"):
            more.append(f'<p class="small"><a href="{term["article"]}">The research article on it</a></p>')
        entries.append(f"""<section class="glossary-entry" id="{term["term"]}">
<h2 class="h3">{esc(term["name"])}</h2>
<p>{esc(term["plain"])}</p>
{"".join(more)}
</section>""")
    body = f"""
<section class="wrap guide-bar">
<nav class="tabs" aria-label="Guides">{guide_tabs("glossary")}</nav>
</section>
<section class="wrap page-head">
<p class="eyebrow">Glossary</p>
<h1>What the words mean</h1>
<p class="lead">Every figure GroupLab shows, and every word a shooter may not know, in plain words. The application says the same thing
beside the same word, because both are written from one list. To learn the ideas in order, from the beginning, read
<a href="/guides/statistics-primer/">Statistics for shooters</a>.</p>
</section>
<section class="wrap prose glossary last">
{"".join(entries)}
</section>
"""
    return shell(GLOSSARY_PATH, "Glossary", "What the words GroupLab uses mean, in plain words: sigma, mean radius, CEP, mil, MOA and the rest.", body, "Guides")


def term_problems() -> list[str]:
    """Entry 154 section 5: a glossary word in a page's text with no explanation on that page is a failure, and so is an explanation that
    does not say what the list says. A word that appears on no page is reported, not failed: it may be waiting for its feature."""
    problems, seen = [], set()
    plains = {term["term"]: esc(term["plain"]) for term in glossary()}
    for f in sorted(OUT.rglob("*.html")):
        rel = f.relative_to(OUT).as_posix()
        if rel == GLOSSARY_PATH.strip("/") + "/index.html":
            continue
        text = f.read_text(encoding="utf-8")
        main = text.split('<main id="main">', 1)[-1].split("</main>", 1)[0] if '<main id="main">' in text else ""
        marked = {}
        for m in re.finditer(r'<a class="term" href="[^"]*" data-term="([^"]+)" data-name="[^"]*" data-plain="([^"]*)">', main):
            marked[m.group(1)] = m.group(2)
            if m.group(2) != plains.get(m.group(1)):
                problems.append(f"{rel}: the explanation of {m.group(1)} is not what the glossary says")
        skip = []
        for token in re.split(r"(<[^>]+>)", main):
            if token.startswith("<"):
                m = re.match(r"<(/?)([a-zA-Z0-9]+)", token)
                if m and not token.startswith("<!"):
                    name = m.group(2).lower()
                    if m.group(1):
                        if skip and skip[-1] == name:
                            skip.pop()
                    elif (name in TERM_SKIP or NO_TERMS in token) and name not in VOID and not token.endswith("/>"):
                        skip.append(name)
                continue
            if skip:
                continue
            for m in GLOSSARY_WORDS.finditer(token):
                term = GLOSSARY_BY_WORD[m.group(1).lower()]["term"]
                seen.add(term)
                if term not in marked:
                    problems.append(f"{rel}: \"{m.group(1)}\" is a glossary word with no explanation on the page")
                    marked[term] = "reported"
        seen.update(marked)
    unused = sorted(term["term"] for term in glossary() if term["term"] not in seen)
    if unused:
        print("glossary words on no page yet: " + ", ".join(unused))
    return problems



# Entry 304 section 3: the items the top bar moves into its More menu on a window too narrow to hold every item on one line.
NAV_MORE = {"Community", "Release notes", "Support"}


def site_header(active: str = "", home: bool = False) -> str:
    """Entry 443: the one header every page carries, the home page's, with every link the header had before entry 441: the main pages,
    Community, Release notes, Support and GitHub. Between 981 and 1499 pixels the less used ones move into More, as entry 304 had it,
    and below that every link is in the menu. Only the home page has motion, so only it has Pause motion and the docking wordmark."""
    bar, menu = [], []
    for label, href in nav():
        cur = ' aria-current="page"' if label == active else ""
        more = label in NAV_MORE
        bar.append(f'<a href="{href}"{cur}{' class="nav-more"' if more else ""}>{esc(label)}</a>')
        menu.append(f'<a href="{href}"{cur}{"" if more else ' class="menu-main"'}>{esc(label)}</a>')
    bar.append(f'<a href="{GITHUB}" class="nav-more tp-gh">{ICON_GITHUB}<span>GitHub</span></a>')
    menu.append(f'<a href="{GITHUB}">GitHub</a>')
    motion = (f'<button type="button" class="tp-motion tp-button mono" aria-pressed="false" aria-label="Pause motion">{ICON_PAUSE}'
              '<span class="tp-motion-word">PAUSE MOTION</span></button>' if home else "")
    return f"""<header class="tp-header{' tp-home' if home else ''}">
<a class="tp-brand cond" href="/" aria-label="GroupLab home">{HOME_MARK}<span class="tp-word">GROUP<span class="tp-amber">LAB</span></span></a>
<nav class="tp-nav mono" aria-label="Main">{"".join(bar)}</nav>
{motion}<button type="button" class="theme-toggle tp-button tp-square" aria-label="Switch between dark and light">{ICON_THEME}</button>
<a class="tp-dl mono" href="/download/">DOWNLOAD</a>
<details class="tp-menu">
<summary class="tp-button tp-square" aria-label="Menu">{ICON_MENU}<span class="menu-word mono">More</span></summary>
<nav class="menu-panel" aria-label="Main">{"".join(menu)}</nav>
</details>
</header>"""


def site_footer() -> str:
    """Entry 443: the one footer every page carries, in the home page's look, with the words and links the site's footer had."""
    year = datetime.date.today().year
    return f"""<footer class="tp-footer">
<div class="tp-footer-about">
<a class="tp-brand cond" href="/" aria-label="GroupLab home">{HOME_MARK}<span>GROUP<span class="tp-amber">LAB</span></span></a>
<p>Free and open source under GPL-3.0. No account, no ads, no paid tier. The application keeps everything on your own computer and sends nothing you have not agreed to: targets, error reports and the survey each ask first.</p>
</div>
<nav class="tp-footer-links" aria-label="Footer">
<a href="/download/">Download</a><a href="{GITHUB}">Source on GitHub</a>
<a href="/shoot-a-target/">Shoot a target</a><a href="{GITHUB}/blob/main/LICENSE">License, GPL-3.0</a>
<a href="/guides/">Guides</a><a href="/releases/">Release notes</a><a href="{GITHUB}/releases">All builds</a>
<a href="/support/">Support</a><a href="{DISCORD}">Community</a>
<a href="/survey/">Hardware survey</a>
</nav>
<span class="tp-copy">GroupLab is a working name and may change. &#169; {year} the GroupLab contributors.</span>
</footer>"""


# Entry 444 section 1: a still, faint copy of the home page's target rings behind every other page's top band, drawn in the theme's own line
# colors so it follows dark and light with no second picture.
PAGE_RINGS = ('<div class="tp-rings" aria-hidden="true"><svg width="1600" height="1600" viewBox="-800 -800 1600 1600">'
              '<circle r="130"/><circle r="330"/><circle r="560"/><circle class="tp-dash" r="780"/></svg></div>')
# Entry 451: the home page's ruler down the right edge, numbered ticks and all, on every page, written once here so the home page and the
# rest cannot drift apart; page.js (home.js on the home page) moves its amber mark with the scroll.
RULER = ('<div class="tp-ruler" aria-hidden="true"><span class="tp-ruler-mark"></span>'
         + "".join(f'<span class="tp-tick-n mono" style="top:{i * 200 + 12}px">{i}</span>' for i in range(12)) + "</div>")
# The pages long enough for entry 444's ruler down the right edge and section index: the guides, the research articles, the release notes,
# the features and support.
LONG_PAGES = re.compile(r"^/(?:guides/(?!glossary/)[^/]+|research/[^/]+|releases|features|support)/$")


def shell(path: str, title: str, description: str, body: str, active: str = "", image: str = "og.png", css: tuple[str, ...] = ()) -> str:
    full_title = "GroupLab" if not title else f"{title} | GroupLab"
    long = bool(LONG_PAGES.match(path))
    if CONTENTS_PAGES.match(path):
        body = with_contents(body)
    styles = "".join(f'\n<link rel="stylesheet" href="/assets/css/{name}.css">' for name in css)
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{esc(full_title)}</title>
<meta name="description" content="{esc(description)}">
<link rel="canonical" href="{SITE_URL}{path}">
<meta name="grouplab-site-build" content="{COMMIT}">
<meta name="color-scheme" content="dark light">
<meta name="theme-color" content="#131417" media="(prefers-color-scheme: dark)">
<meta name="theme-color" content="#f4f3f0" media="(prefers-color-scheme: light)">
<meta property="og:type" content="website">
<meta property="og:site_name" content="GroupLab">
<meta property="og:title" content="{esc(full_title)}">
<meta property="og:description" content="{esc(description)}">
<meta property="og:url" content="{SITE_URL}{path}">
<meta property="og:image" content="{SITE_URL}/assets/img/{image}">
<meta name="twitter:card" content="summary_large_image">
<link rel="icon" href="/favicon.ico" sizes="any">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
<link rel="preload" href="/assets/fonts/plex-sans-400.woff2" as="font" type="font/woff2" crossorigin>
<link rel="preload" href="/assets/fonts/plex-sans-condensed-700.woff2" as="font" type="font/woff2" crossorigin>
<link rel="stylesheet" href="/assets/css/site.css">{styles}
<script src="/assets/js/theme.js"></script>
<script src="/assets/js/terms.js" defer></script>
<script src="/assets/js/page.js" defer></script>
</head>
<body class="tp tp-ruled{' tp-long' if long else ''}">
<a class="skip" href="#main">Skip to content</a>
{PAGE_RINGS}{RULER}
{site_header(active)}
<main id="main">
{body if path == GLOSSARY_PATH else explain(body)}
</main>
{site_footer()}
</body>
</html>
"""


def screen(name: str, alt: str, eager: bool = False, cls: str = "shot") -> str:
    """A screenshot in both themes; CSS shows the one that matches the page."""
    base = name.replace("-dark", "").replace("-light", "")
    load = 'fetchpriority="high"' if eager else 'loading="lazy"'
    return (
        f'<img class="{cls} only-dark" src="/assets/screens/{base}-dark-1400x900.webp" alt="{esc(alt)}" width="1400" height="900" {load} decoding="async">'
        f'<img class="{cls} only-light" src="/assets/screens/{base}-light-1400x900.webp" alt="{esc(alt)}" width="1400" height="900" loading="lazy" decoding="async">'
    )


def screen_link(name: str, alt: str) -> str:
    """Entry 304 section 2: a screenshot as large as its box allows, in both themes, each opening full size on a click."""
    base = name.replace("-dark", "").replace("-light", "")
    return "".join(
        f'<a class="plain only-{theme} shot-link" href="/assets/screens/{base}-{theme}-1400x900.webp" aria-label="{esc(alt)}, full size">'
        f'<img class="shot" src="/assets/screens/{base}-{theme}-1400x900.webp" alt="{esc(alt)}" width="1400" height="900" loading="lazy" decoding="async"></a>'
        for theme in ("dark", "light"))


def phone_themes(name: str) -> list[str]:
    """The files a phone screenshot has: one per theme, or one alone for something with no theme, such as the launcher's icons."""
    if (PHONE_SCREENS / f"{name}.png").exists():
        return [name]
    return [f"{name}-{theme}" for theme in ("dark", "light") if (PHONE_SCREENS / f"{name}-{theme}.png").exists()]


def phone_screen(name: str, alt: str) -> str:
    """A phone screenshot in both themes, at the phone's own proportions."""
    files = phone_themes(name)
    size = Image.open(need(PHONE_SCREENS / f"{files[-1]}.png")).size
    w, h = size[0] // 2, size[1] // 2
    return "".join(
        f'<img class="shot phone-shot{"" if len(files) == 1 else " only-" + stem.rsplit("-", 1)[1]}" src="/assets/screens/phone/{stem}.webp" alt="{esc(alt)}" width="{w}" height="{h}" loading="lazy" decoding="async">'
        for stem in files)


def platform_switch() -> str:
    """Entry 249: Desktop | Mobile at the top of the tour and the Features page. The script in the head picks the side before the page
    draws, from a link's ?platform=, then the visitor's own last choice, then whether they are on a phone or tablet; without it, Desktop."""
    return ('<div class="platform-switch" role="group" aria-label="Show GroupLab on">'
            '<button type="button" data-choose="desktop">Desktop</button><button type="button" data-choose="mobile">Mobile</button></div>')


def shown(platform: str, html: str) -> str:
    """Something only one side of the switch shows."""
    return f'<div data-show="{platform}">{html}</div>' if html else ""


def only_note(words: str) -> str:
    return f'<p class="note">{esc(words)}</p>'


def phone_thumb(name: str) -> str:
    return f'<img class="research-thumb phone-thumb" src="/assets/screens/phone/{phone_themes(name)[0]}.webp" alt="" width="320" height="206" loading="lazy">'


def btn(label: str, href: str, primary: bool = False, sub: str | None = None, big: bool = False) -> str:
    cls = "btn " + ("btn-primary" if primary else "btn-secondary") + (" btn-big" if big else "")
    inner = f"<span>{esc(label)}</span>"
    if sub:
        inner += f'<span class="btn-sub mono">{esc(sub)}</span>'
    return f'<a class="{cls}" href="{href}">{inner}</a>'


# ---------------------------------------------------------------- pages


# NOTES-FROM-PLANNING.md entry 147 section 3.2: the platform statement appears on this page, in README.md and on every
# release carrying a macOS asset, and all three are generated from docs/PLATFORM-SUPPORT.md. It is Alan's settled wording and
# is not to be reworded, which is exactly the kind of text that gets edited in one place and not the others. One source, three
# readers, and a test that fails when any of them drifts.
PLATFORM_SUPPORT = REPO / "docs" / "PLATFORM-SUPPORT.md"


def platform_support() -> str:
    """The statement as HTML, without the source file's own notes about where it is used."""
    text = need(PLATFORM_SUPPORT).read_text(encoding="utf-8")
    if "\n---\n" not in text:
        raise SystemExit("docs/PLATFORM-SUPPORT.md has no rule separating its notes from the statement")

    body = text.split("\n---\n", 1)[1].strip()
    # Entry 275 section 4: the minimums are a Markdown table, which printed as raw pipes without the tables extension.
    rendered = markdown.markdown(body, extensions=["sane_lists", "fenced_code", "tables"])

    # The page's own heading level: the statement's h2s sit under this section's h2, so they become h3s.
    rendered = rendered.replace("<h2>", '<h3 class="h3">').replace("</h2>", "</h3>")
    rendered = rendered.replace("<pre><code>", '<p><code class="block">').replace("</code></pre>", "</code></p>")
    return '<h2 class="h3">What is supported, and what is not</h2>' + rendered


# Entry 338: the download page, concept A, "pick your device". The five devices in the order the page shows them, with what a link names
# each by: /download/?device=mac and so on, so a guide or a post can point at one.
DEVICES = [("windows", "Windows"), ("mac", "Mac"), ("iphone", "iPhone and iPad"), ("android", "Android"), ("linux", "Linux")]

# Every file the moving nightly release carries, in the order .github/workflows/nightly.yml publishes them, and what each one is for.
# The Android ones are added by its android-files job once they exist (entry 450 section 2).
NIGHTLY_FILES = [
    ("grouplab-setup-win-x64.exe", "Windows, the installer"),
    ("grouplab-win-x64.zip", "Windows, the zip"),
    ("grouplab-linux-x64.tar.gz", "Linux, the tarball"),
    ("grouplab-macos-arm64.tar.gz", "Mac with Apple silicon"),
    ("grouplab-macos-x64.tar.gz", "Mac with an Intel processor, untested"),
    ("grouplab-android-dev.apk", "Android, GroupLab Dev"),
    ("grouplab-android.apk", "Android, the plain APK"),
    ("grouplab-android.aab", "Android, the bundle Google Play takes"),
    ("update-manifest.json", "what the updater in GroupLab reads, nothing to download by hand"),
    ("update-manifest-2.json", "the same, for nightly 45 and after"),
]


def newest_build() -> dict:
    """Entry 338: the newest published build's number, date and commit, from docs/RELEASE-NOTES.md, which each nightly writes its own
    entry into as it publishes. Nothing on the page is typed: a build later, every figure on it has moved with the file."""
    for version, section in release_sections().items():
        if "nightly." not in version:
            continue
        date = re.search(r"\*\*(\d{4}-\d{2}-\d{2})\*\*", section)
        commit = re.search(r"commit `([0-9a-f]{7,40})`", section)
        if date is None or commit is None:
            raise SystemExit(f"docs/RELEASE-NOTES.md: {version} names no date or no commit, and the download page shows both")
        return {"version": version, "number": build_number(version), "date": date.group(1), "commit": commit.group(1),
                "anchor": slug(version)}
    raise SystemExit("docs/RELEASE-NOTES.md has no nightly build, and the download page names the newest one")


# The device the visitor is on, guessed before the sections draw so nothing flickers: the page loads this small file without defer, just
# above the device buttons. A link's ?device= wins, and the buttons change it. Without scripts none of this runs, the attribute is never
# set, and every device's section shows in order. A file rather than an inline script, like send.js and survey.js, so it is fingerprinted
# and cached, and so scripts/claims.py does not read code as sentences.
DOWNLOAD_JS = """(function () {
  var root = document.documentElement, ua = navigator.userAgent || "";
  var names = ["windows", "mac", "iphone", "android", "linux"];
  var alias = { ios: "iphone", ipad: "iphone", macos: "mac", win: "windows" };
  var touchMac = navigator.maxTouchPoints > 1 && /Macintosh/.test(ua);
  // Entry 349: a tablet's browser asking for the desktop site says "X11; Linux x86_64" (Firefox and Chrome on Android both do); a touch
  // screen with no fine pointer at all is taken for Android, while a Linux laptop with a touch screen and a mouse or trackpad stays Linux.
  var touchOnly = navigator.maxTouchPoints > 0 && !!window.matchMedia && window.matchMedia("(pointer: coarse)").matches
    && !window.matchMedia("(any-pointer: fine)").matches;
  // Entry 403: a tablet with a pen or a keyboard's trackpad has a fine pointer, so the touch test missed Alan's Galaxy tablet in Firefox.
  // Two things a desktop-site request leaves alone are read first: the browser's own platform (Chromium's userAgentData), and an ARM
  // processor named by navigator.platform or Firefox's oscpu while the user agent says x86_64, which is a browser pretending. A Linux
  // computer on ARM says ARM in both and stays Linux.
  var cpu = (navigator.platform || "") + " " + (navigator.oscpu || "");
  var plainLinux = /Linux|X11/.test(ua) && !/Android|CrOS/.test(ua);
  var saysAndroid = !!(navigator.userAgentData && navigator.userAgentData.platform === "Android");
  var armAsX86 = /arm|aarch64/i.test(cpu) && /x86_64|amd64/i.test(ua);
  var touchLinux = plainLinux && (saysAndroid || armAsX86 || touchOnly);
  var rule = /[?&]device=/.test(location.search) ? "the address names the device"
    : plainLinux && saysAndroid ? "the browser's own platform says Android" : plainLinux && armAsX86 ? "an ARM processor under an x86_64 user agent"
    : touchLinux ? "a touch screen with no fine pointer" : touchMac ? "a touch screen on a Mac user agent" : "the user agent";
  var guess = /iPhone|iPod|iPad/.test(ua) || touchMac ? "iphone" : /Android/.test(ua) || touchLinux ? "android" : /Windows/.test(ua) ? "windows"
    : /Macintosh|Mac OS X/.test(ua) ? "mac" : /Linux|X11|CrOS/.test(ua) ? "linux" : "windows";
  var phone = /iPhone|iPod/.test(ua) || (/Android/.test(ua) && /Mobile/.test(ua));
  // A guess resting on the touch screen rather than on what the browser says is never said as if certain.
  var words = touchMac || touchLinux ? "Looks like this device" : phone ? "This phone"
    : (guess === "iphone" || guess === "android") ? "This tablet" : "This computer";
  function known(d) { d = alias[d] || d; return names.indexOf(d) >= 0 ? d : null; }
  var asked = /[?&]device=([a-z]+)/.exec(location.search);
  root.setAttribute("data-device", (asked && known(asked[1])) || guess);
  function mark() {
    var now = root.getAttribute("data-device");
    Array.prototype.forEach.call(document.querySelectorAll("[data-pick]"), function (b) {
      b.setAttribute("aria-pressed", String(b.getAttribute("data-pick") === now));
    });
  }
  function choose(d, keep) {
    root.setAttribute("data-device", d);
    mark();
    if (keep) { try { history.replaceState(null, "", "?device=" + d + location.hash); } catch (e) {} }
  }
  function follow() {
    var target = location.hash.length > 1 ? document.getElementById(decodeURIComponent(location.hash.slice(1))) : null;
    if (!target) { return; }
    var owner = target.closest("[data-for]");
    if (owner) { choose(owner.getAttribute("data-for"), false); }
    for (var el = target; el; el = el.parentElement) { if (el.tagName === "DETAILS") { el.open = true; } }
    target.scrollIntoView();
  }
  document.addEventListener("DOMContentLoaded", function () {
    Array.prototype.forEach.call(document.querySelectorAll("[data-pick]"), function (b) {
      if (b.getAttribute("data-pick") === guess) { b.querySelector(".dl-this").textContent = words; }
      b.addEventListener("click", function () { choose(b.getAttribute("data-pick"), true); });
    });
    mark();
    follow();
    // Entry 403: ?why shows, under the device buttons, what the guess rested on. It is on this screen only: nothing is sent or kept.
    if (/[?&]why(?:[=&]|$)/.test(location.search)) {
      var first = document.querySelector("[data-pick]");
      if (first && first.parentElement) {
        var media = function (q) { return window.matchMedia ? String(window.matchMedia(q).matches) : "unknown"; };
        var why = document.createElement("pre");
        why.className = "small dim";
        why.style.whiteSpace = "pre-wrap";
        why.style.overflowWrap = "anywhere";
        why.textContent = ["user agent: " + ua, "platform: " + (navigator.platform || "none"), "oscpu: " + (navigator.oscpu || "none"),
          "userAgentData platform: " + (navigator.userAgentData ? navigator.userAgentData.platform : "none"),
          "maxTouchPoints: " + navigator.maxTouchPoints, "pointer coarse: " + media("(pointer: coarse)"),
          "any pointer fine: " + media("(any-pointer: fine)"), "hover: " + media("(hover: hover)"),
          "guess: " + guess + ", from " + rule].join("\\n");
        first.parentElement.insertAdjacentElement("afterend", why);
      }
    }
  });
  window.addEventListener("hashchange", follow);
})();"""


def page_download() -> str:
    n = newest_build()
    commit_link = f'<a class="mono" href="{GITHUB}/commit/{n["commit"]}">{n["commit"]}</a>'
    built = f'<p class="small dim">Nightly {n["number"]}, built {n["date"]} from commit {commit_link}.</p>'

    def points(items: list[str]) -> str:
        return '<ul class="dim small-list">' + "".join(f"<li>{p}</li>" for p in items) + "</ul>"

    def newest(title: str, file: str, desc: str, items: list[str], label: str, honest: bool = True) -> str:
        """The left card: the newest build, in amber, one big button, the file, the build it is, three points and the honest line."""
        line = '<p class="small faint">Honestly: it passed the tests, which is not the same as somebody having used it.</p>' if honest else ""
        return f"""<article class="panel card card-rec dl-card">
<div class="card-head"><h3 class="h3">{title}</h3><span class="badge mono">Newest</span></div>
<div>{btn(label, NIGHTLY + file, True, big=True)}</div>
<p class="mono teal small dl-file">{file}</p>
{built}
<p>{desc}</p>
{points(items)}
{line}
</article>"""

    def steady(title: str, action: str, desc: str, items: list[str]) -> str:
        """The right card: the copy a store keeps up to date, with the store's own badge or link."""
        return f"""<article class="panel card dl-card">
<div class="card-head"><h3 class="h3">{title}</h3><span class="badge badge-teal mono">Updates itself</span></div>
<div>{action}</div>
<p>{desc}</p>
{points(items)}
</article>"""

    def other(title: str, file: str, desc: str, items: list[str], label: str) -> str:
        """Another download for the same device, inside its fold."""
        return f"""<div class="stack tight">
<p class="mono teal small dl-file">{file}</p>
<p>{desc}</p>
{points(items)}
<div>{btn(label, NIGHTLY + file)}</div>
</div>"""

    def fold(title: str, inner: str, ident: str = "", cls: str = "") -> str:
        id_attr = f' id="{ident}"' if ident else ""
        return f"""<details class="dl-fold{(' ' + cls) if cls else ''}"{id_attr}>
<summary>{title}</summary>
<div class="dl-fold-body">
{inner}
</div>
</details>"""

    def section(key: str, name: str, cards: str, unsure: str, help_folds: list[str], keep: str = "", single: bool = False) -> str:
        return f"""<section class="dl-sec" data-for="{key}" id="{key}" aria-labelledby="dl-{key}">
<h2 class="h3 dl-sec-title" id="dl-{key}">{name}</h2>
<div class="dl-cards{' dl-one' if single else ''}">
{cards}
</div>
{keep}
<p class="dl-unsure"><strong>Not sure?</strong> {unsure}</p>
{('<div class="dl-help stack tight">' + "".join(help_folds) + "</div>") if help_folds else ""}
</section>"""

    files_rows = "".join(f'<tr><td><a href="{NIGHTLY}{f}"><code>{f}</code></a></td><td>{what}</td></tr>' for f, what in NIGHTLY_FILES)
    files_inner = f"""<p class="small">The moving release on GitHub always carries the newest build under these names, so a link to one of them never goes stale. The Android files join it when their own build finishes, usually some minutes after the others. Nightly {n["number"]}, built {n["date"]} from commit {commit_link}.</p>
<div class="dl-table"><table class="small"><thead><tr><th>File</th><th>What it is</th></tr></thead><tbody>{files_rows}</tbody></table></div>
<p class="small">The same build with its number in every file name, and the notes it was published with: <a href="{GITHUB}/releases/tag/v{n["version"]}">GroupLab {n["version"]} on GitHub</a>. What changed in it: <a href="/releases/#{n["anchor"]}">its release notes</a>.</p>"""
    files_title = f"Every file in nightly {n['number']}, on GitHub"

    windows = section("windows", "Windows", newest(
        "The installer", "grouplab-setup-win-x64.exe",
        "Windows 10 and 11. Installs into your own user account, with an entry in Add or remove programs.",
        ["No administrator rights needed", "Keeps itself up to date: asks first, then updates in the background",
         "Rebuilt after every change that passes the tests on Windows, Linux and macOS"],
        "Download the installer") + steady(
        "Microsoft Store",
        f'<a href="{STORE}"><img class="only-dark" src="/assets/img/get-it-from-microsoft-dark.svg" alt="Get it from Microsoft" width="161" height="44"><img class="only-light" src="/assets/img/get-it-from-microsoft-light.svg" alt="Get it from Microsoft" width="161" height="44"></a>',
        "The Store keeps GroupLab up to date itself and installs it without the \"Windows protected your PC\" warning. It gets every nightly too, once Microsoft has certified it, usually a day or so after it is published.",
        ["Updates come from the Store, like any Store app, and GroupLab's own updater is switched off in that copy",
         "Windows 10 version 1809 or later",
         (f"Version {store_version()} today; the Store's page and GroupLab's Settings screen name it too" if store_version()
          else "Which build it is: the Store's page names its version, and so does GroupLab's Settings screen")]),
        "Take the Microsoft Store copy if you want GroupLab to look after itself. Take the newest if you want each fix the day it is made and do not mind the odd broken build.",
        [fold("The zip: nothing to install", other(
            "Zip", "grouplab-win-x64.zip", "Windows 10 and 11. Unzip it anywhere and run GroupLab.App.exe.",
            ["Nothing to install", "Tells you when a newer build exists; you download it yourself"], "Download the zip")),
         fold("When Windows says \"Windows protected your PC\"", """<p>The Windows and Linux builds are unsigned, because signing costs money the project has not spent. Windows says this about any program nobody has paid to sign. The source of every build is public, at the commit the download names.</p>
<ol class="text">
<li>Click <strong>More info</strong>.</li>
<li>Click <strong>Run anyway</strong>.</li>
<li>If your antivirus quarantines it, the file it took is <code>GroupLab.App.exe</code>.</li>
</ol>
<p class="small">The Microsoft Store copy does not show this warning.</p>""")])

    mac = section("mac", "Mac", newest(
        "macOS, Apple silicon", "grouplab-macos-arm64.tar.gz",
        "For any Mac with Apple silicon. Self-contained, built on macOS, and tested by the suite on every change.",
        ["<strong>Run on two real Macs.</strong> Two testers, nightlies 93 and 190",
         "Signed, notarized and stapled: opens like any other Mac application",
         "Any Mac with Apple silicon (M-series). Not an Intel Mac"],
        "Download for Apple silicon") + """<div class="panel pad dl-none">
<h3 class="h3">No store copy for the Mac</h3>
<p>GroupLab is not in the Mac App Store. Every Mac download is the newest build, and GroupLab tells you when a newer one exists; you download it yourself.</p>
</div>""",
        "Apple menu, then About This Mac. A chip whose name begins with M is Apple silicon, and this is its download; Which Mac have I got, below, says more.",
        [fold("The Intel build, untested", other(
            # ClaimsAboutMeasuringTests holds this card's first four arguments to one line, the way the README's table says them.
            "macOS, Intel", "grouplab-macos-x64.tar.gz", "For a Mac with an Intel processor. Self-contained, built on macOS, and tested by the suite on every change.", ["<strong>Untested on a real Mac.</strong> Nobody has run it",
             "Signed, notarized and stapled: opens like any other Mac application", "Intel only. Not an Apple silicon Mac"], "Download for an Intel Mac")),
         fold("Which Mac have I got?", """<p>Apple menu, then About This Mac. A line saying <strong>Chip</strong> and a name beginning with M is Apple silicon. A line saying <strong>Processor</strong> and Intel is the Intel one.</p>
<p class="small faint">Taking the wrong one gives you an application that will not open, with no useful message about why.</p>""", ident="which-mac"),
         fold("Opening it on a Mac", """<p>From nightly 135, the macOS build is signed with a Developer ID, notarized by Apple and stapled. Move <code>GroupLab.app</code> into your Applications folder and open it; the first time, macOS checks it with Apple and opens it.</p>""")])

    iphone = section("iphone", "iPhone and iPad", f"""<article class="panel card card-rec dl-card">
<div class="card-head"><h3 class="h3">The public beta</h3><span class="badge mono">Public beta</span></div>
<div>{btn("Join the iPhone and iPad beta", TESTFLIGHT, True, big=True)}</div>
<p>GroupLab for iPhone and iPad is in a public beta through Apple's TestFlight. Install TestFlight from the App Store first, then open the invitation on the iPhone or iPad. Each new beta build arrives through TestFlight.</p>
{points(["Free, like every GroupLab download", "TestFlight installs each new beta build for you", "Not in the App Store itself yet"])}
</article>""",
        "Install TestFlight from the App Store first, then open the invitation on the iPhone or iPad.",
        [fold("Install TestFlight first", """<ol class="text">
<li>On the iPhone or iPad, open the App Store and install <strong>TestFlight</strong>, Apple's own app for beta builds.</li>
<li>On the same iPhone or iPad, open the invitation: <strong>Join the iPhone and iPad beta</strong>, above.</li>
<li>TestFlight opens. Accept the invitation, then install GroupLab.</li>
</ol>""", ident="install-testflight")], single=True)

    android = section("android", "Android", newest(
        "GroupLab Dev", "grouplab-android-dev.apk",
        "For testing GroupLab on Android until it is published on the Play Store. An arm64 phone or tablet with Android 10 or later and 4 GB of memory. After the first install it keeps itself on the newest nightly, and fixes reach you the same day they are made.",
        ["Updates itself from every nightly: no computer and no adb after the first install",
         "Installs beside the Google Play test copy without replacing it",
         "Its logs are easy to send with a problem report",
         "Honestly: a nightly can occasionally break something, and Dev's data stays in Dev unless you move it with Settings, Export all my data"],
        "Download GroupLab Dev", honest=False) + steady(
        "Google Play, by invitation",
        btn("Ask on the Discord", DISCORD),
        f'GroupLab\'s internal test on Google Play receives every nightly through Google Play, which updates your copy as it does any app. It is open by invitation: ask on the <a href="{DISCORD}">Discord</a>, then opt in at <a href="https://play.google.com/apps/internaltest/4701684356677501640">the internal test\'s page</a>.',
        ["Each nightly reaches Play by itself", "One step first: ask on the Discord to be invited",
         "Signed with a different key from the plain APK"]),
        "Take GroupLab Dev: it keeps itself on the newest build and installs beside the Play copy. The Play copy gets every nightly through Google Play, the plain APK keeps itself current as GroupLab Dev does, and GroupLab Dev installs beside either with no uninstall.",
        [fold("The plain APK", other(
            "Android", "grouplab-android.apk",
            "The same app under GroupLab's own name, signed, with a different key from the Google Play copy. It keeps itself on the newest nightly, as GroupLab Dev does. An APK from before nightly 177 has no updater, so install one newer plain APK by hand once; it keeps itself current after that.",
            ["Open the file on the phone; allow your browser to install apps when Android asks",
             "Remove the Google Play copy first, if you have it: the two are signed with different keys",
             "Marking a target by hand is on the phone, as on the computer"], "Download for Android")),
         fold("In Brazil, Indonesia, Singapore or Thailand", "<p>In Brazil, Indonesia, Singapore and Thailand, certified Android phones have refused apps from outside Google Play since 30 September 2026 unless their developer has registered them with Google; GroupLab Dev and the plain APK were registered with Google on 7 October 2026; if a phone there still refuses one, ask on the Discord for the Google Play internal test instead. The rest of the world follows in 2027.</p>"),
         fold("When Android says Google Play Protect is scanning the app", """<p>Android asks to scan an app installed from outside the Play Store, the first time you install it and when it updates. It is Google's own check, it takes a few seconds, and the scan is expected: let it finish, then carry on.</p>""")],
        keep='<p class="small dl-keep">Take either the Play copy or the APK, not both: remove one before installing the other. GroupLab Dev is different: it installs beside either.</p>')

    linux = section("linux", "Linux", newest(
        "Linux tarball", "grouplab-linux-x64.tar.gz", "Self-contained, built on Ubuntu, and tested on every change.",
        ["Nobody uses it day to day yet", "Reports from Linux are especially welcome",
         "Tells you when a newer build exists; you download it yourself"],
        "Download the Linux tarball"),
        "What each device needs, under Everything else below, lists the oldest distributions it runs on.", [], single=True)

    picks = "".join(f'<button type="button" class="dl-pick" data-pick="{key}" aria-pressed="false" aria-controls="{key}">'
                    f'<span>{name}</span><span class="dl-this mono"></span></button>' for key, name in DEVICES)

    # The statement stays whole and in Alan's words; only its minimums table is shown on its own, under What each device needs.
    statement = platform_support()
    start = statement.find('<h3 class="h3">Minimums</h3>')
    end = statement.find("<h3", start + 1)
    if start < 0 or end < 0:
        raise SystemExit("docs/PLATFORM-SUPPORT.md: the Minimums section was not found, and the download page shows it on its own")
    minimums, supported = statement[start:end], statement[:start] + statement[end:]

    updating = f"""<p>The newest build is rebuilt automatically after every change that passes the tests on Windows, Linux and macOS, and published within minutes. It may be broken: passing the tests is not the same as somebody having used it. There is no full release yet.</p>
<h3 class="h3">Updating</h3>
<p><strong>Of the computer downloads, only the Windows installer updates itself.</strong> It asks first, then updates in the background.</p>
<p>The zip, the Linux tarball and both macOS builds tell you when a newer build exists and leave the downloading to you. There is no silent update on those platforms, and GroupLab will not pretend otherwise: it says so on the Settings screen rather than offering an update it cannot apply.</p>
<p>The Microsoft Store copy is updated by the Store, GroupLab Dev and the plain Android APK update themselves from every nightly, the Google Play copy is updated by Google Play, and TestFlight installs each new iPhone and iPad beta build.</p>
<h3 class="h3">What you get</h3>
<dl class="facts">
<div><dt class="mono">Runtime</dt><dd>Nothing else to install. The download carries its own .NET runtime.</dd></div>
<div><dt class="mono">Samples</dt><dd>Two sample sheets, so there is something to open in the first minute.</dd></div>
<div><dt class="mono">Your data</dt><dd>Kept in <code>%APPDATA%\\GroupLab</code> on Windows, and under your home folder elsewhere. An update check sends nothing about you.</dd></div>
<div><dt class="mono">Which build</dt><dd>The Settings screen names the version, the train and the commit. Put that line in any report.</dd></div>
</dl>"""

    body = f"""
<section class="wrap page-head dl-head">
<p class="eyebrow">Download</p>
<h1>Download GroupLab</h1>
<p class="lead">Free, for Windows, Mac, iPhone and iPad, Android and Linux. Choose your device: the newest build, and where a store carries GroupLab, the copy it keeps up to date.</p>
<script src="/assets/js/download.js"></script>
<div class="dl-picks" role="group" aria-label="Your device">{picks}</div>
</section>
<div class="wrap dl-stage">
{windows}
{mac}
{iphone}
{android}
{linux}
{fold(files_title, files_inner, ident="every-file", cls="dl-files dl-files-wide")}
</div>
<section class="wrap section-sm">
<details class="dl-fold dl-else" id="everything-else">
<summary>Everything else</summary>
<div class="dl-fold-body">
<p class="small">What each device needs, what is supported and tested, how updating works, and every file of the newest build.</p>
{fold("What each device needs", '<div class="prose dl-table">' + minimums + "</div>", ident="needs")}
{fold("What is supported and tested", '<div class="stack supported prose">' + supported + "</div>", ident="supported")}
{fold("Updating, and what you get", updating, ident="updating")}
{fold(files_title, files_inner, ident="every-file-phone", cls="dl-files dl-files-narrow")}
</div>
</details>
</section>
<section class="wrap section-sm last row-between">
<p>Each of the newest thirty builds keeps a release of its own, so a bug report names something that still exists; older ones are removed with their tags, and their notes stay on the Release notes page. If something does not work, the <a href="{DISCORD}">Discord</a> is somewhere to ask.</p>
<a href="/releases/">What changed in each build</a>
<a href="{GITHUB}/releases">Every build on GitHub</a>
</section>
"""
    return shell("/download/", "Download", "Download GroupLab for Windows, Mac, iPhone and iPad, Android or Linux: the newest build, and the copy a store keeps up to date.", body, "Download", css=("download",))


def donor() -> dict:
    return json.loads(need(DONOR / "sheets.json").read_text(encoding="utf-8"))


def donor_files() -> list[str]:
    return [f"{s[paper]}.pdf" for s in donor()["sheets"] for paper in ("letter", "a4") if s.get(paper)]


def donor_card(s: dict) -> str:
    """Entry 264: one donor sheet, its picture (the sheet itself, entry 256), when to use it, and its PDF in Letter and A4."""
    stem = s["letter"].replace("GL-", "").lower()
    size = Image.open(need(SCREENS / f"sheet-page-{stem}.png")).size
    buttons = btn("Letter PDF", f"/donor/{s['letter']}.pdf", True) + (btn("A4 PDF", f"/donor/{s['a4']}.pdf") if s.get("a4") else "")
    return (f'<article class="panel pad stack tight donor-sheet"><img class="donor-pic" src="/assets/screens/sheet-page-{stem}.webp" alt="{esc(s["name"])}, as it prints" '
            f'width="{size[0]}" height="{size[1]}" loading="lazy" decoding="async"><h3 class="h4">{esc(s["name"])}</h3><p>{esc(s["use"])}</p>'
            f'<div class="actions">{buttons}</div></article>')


def page_shoot() -> str:
    def pdf(title: str, file: str, desc: str, paper: str, primary: bool = False) -> str:
        # Entry 159 section 5.2: the page count is read from the file, like its size, never typed beside it.
        size_kb = (DONOR / file).stat().st_size // 1024
        count = len(re.findall(rb"/Type\s*/Page[^s]", (DONOR / file).read_bytes()))
        pages = f"{count} page{'s' if count != 1 else ''}, {paper}"
        return f"""<div class="panel pdf-row{' card-rec' if primary else ''}">
{ICON_PDF}
<div class="pdf-text"><strong>{title}</strong><span>{desc}</span><span class="mono faint small">{file} &#183; {pages} &#183; {size_kb} KB</span></div>
{btn("Download PDF", "/donor/" + file, primary)}
</div>"""

    def st(n: str, title: str, text: str, note: str | None = None) -> str:
        nt = f'<p class="mono teal small">{note}</p>' if note else ""
        return f"""<li class="step"><span class="mono step-n">{n}</span><div><h2 class="h3">{title}</h2><p>{text}</p>{nt}</div></li>"""

    body = f"""
<section class="wrap page-head two-col bottom">
<div class="stack">
<p class="eyebrow">Shoot a target</p>
<h1>Help prove that GroupLab measures correctly.</h1>
<p class="lead">It needs real targets, shot by real people with real rifles. Five steps, about ten minutes of your time on top of the shooting. Any distance, any rifle: nine shots is useful, twenty-five is better.</p>
{f'<div class="actions">{btn("Send your target", SEND, True, "Already shot one? Send the photos")}</div>' if limits().get("open") else ""}
</div>
<div class="stack tight">
<div class="panel pdf-row card-rec">
{ICON_PDF}
<div class="pdf-text"><strong>The whole donor pack</strong><span>Every sheet below, Letter and A4, each with its page of instructions.</span><span class="mono faint small">grouplab-donor-pack.zip &#183; {len(donor_files())} PDFs</span></div>
{btn("Download all", "/donor/grouplab-donor-pack.zip", True)}
</div>
<p class="small"><strong>Most useful right now:</strong> {esc(donor()["mostUseful"])}</p>
<p class="small faint">Made for your optic draws a sheet for your own scope and distance: it is on GroupLab's <a href="/tour/optic/">Targets screen</a>, on the desktop and the phone.</p>
</div>
</section>
<section class="wrap section">
<h2>One sheet of each</h2>
<div class="grid-2 donor-sheets">{"".join(donor_card(s) for s in donor()["sheets"])}</div>
</section>
<section class="wrap section two-col top last">
<ol class="step-list">
{st("1", "Print it at actual size", "Letter or A4 paper, Actual size or 100 percent. Never Fit to page. Then check the size: between the centers of bull 1 and bull 5 on a 5x5 sheet, or the bar under a zeroing grid.", "The page of instructions with each sheet gives its own number")}
{st("2", "Say what you shot", "On a sheet with a load block, the date, the distance and the cartridge, inside the block only. On any other sheet write nothing, and name the load when you send it: pen marks can be mistaken for bullet holes.")}
{st("3", "Mount it and shoot it", "Tape it onto cardboard at the four corners; flat is best, and GroupLab follows a gentle curl. On a 5x5 sheet one shot per bull, in number order; on a zeroing grid one group at the diamond.")}
</ol>
<div class="stack">
<ol class="step-list" start="4">
{st("4", "Photograph it before you take it down", "In GroupLab on your phone, Take a picture: Guided takes it when everything is right, Manual when you press, and the torch helps in dim light. It scores each picture and says what it corrected. Without GroupLab, two photographs on the main camera at 1x. A target still hanging where it was shot is the material the project most needs.")}
{st("5", "Send them as they came off the camera", "A 600 dpi flatbed scan too, if you have one. Do not crop them and do not send them through a messaging app, which shrinks them.", "Send your photos, below" if limits().get("open") else "Sending opens here shortly")}
</ol>
<div class="callout small-callout">
<div class="stack">
<h2 class="h3">Send them</h2>
<p class="text">No account, no email address and no follow up. Every photo is rebuilt from its pixels on the server, so the camera facts the measurements need come across and the location and the date and time cannot.</p>
{"" if limits().get("open") else '<p class="text">The page is built and waiting on one install on the server. Until then, keep your files as they came off the camera.</p>'}
</div>
{f'<div class="actions col">{btn("Send your target photos", SEND, True, "JPEG, PNG, HEIC or TIFF")}</div>' if limits().get("open") else ""}
</div>
</div>
</section>
"""
    return shell("/shoot-a-target/", "Shoot a target", "Print a GroupLab target, shoot it, photograph it and send it, to help prove the software measures correctly. Donor pack PDFs and instructions.", body, "Shoot a target")


# ---------------------------------------------------------------- the upload page, entry 129


def limits() -> dict:
    """The one place the page and the receiver both take their limits from, entry 129 section 1.2."""
    return json.loads(need(REPO / "website" / "api" / "limits.json").read_text(encoding="utf-8"))


def limit_problems() -> list:
    """The page's limits and the receiver's are the same numbers, entry 129 section 1.2.

    The page is built by Python and the receiver is PHP, and neither can read the other's constants. A page that
    says 30 MB while the receiver refuses at 20 wastes somebody's upload and tells them nothing useful about why,
    so the build reads the receiver's own constants back and holds them to this file.
    """
    found = []
    limit = limits()
    php = need(REPO / "website" / "api" / "upload.php").read_text(encoding="utf-8")

    def constant(name: str) -> str | None:
        m = re.search(r"const\s+" + name + r"\s*=\s*([^;]+);", php)
        return m.group(1).strip() if m else None

    pairs = [
        ("MAX_FILES", str(limit["maxFiles"])),
        ("MAX_FILE_BYTES", f'{limit["maxFileMegabytes"]} * 1024 * 1024'),
        ("MAX_SUBMISSION_BYTES", f'{limit["maxSubmissionMegabytes"]} * 1024 * 1024'),
        ("MAX_SET_FILES", str(limit["maxSetFiles"])),
        ("MAX_SET_BYTES", f'{limit["maxSetMegabytes"]} * 1024 * 1024'),
    ]
    for name, expected in pairs:
        got = constant(name)
        if got is None:
            found.append(f"website/api/upload.php: no {name} constant, so the page's limits cannot be checked against it")
        elif not got.startswith(expected):
            found.append(f"website/api/upload.php: {name} is {got!r} and limits.json says {expected!r}")

    # Entry 165 section 2: both receivers record the two consent levels in the page's own words, from this one file.
    app = need(REPO / "website" / "api" / "app-submission.php").read_text(encoding="utf-8")
    for name, source in [("upload.php", php), ("app-submission.php", app)]:
        if f"const CONSENT_VERSION = '{limit['consentVersion']}'" not in source:
            found.append(f"website/api/{name}: the consent version is not {limit['consentVersion']}")
        for level, text in limit["consentTexts"].items():
            if f"'{level}'" not in source or text not in source:
                found.append(f"website/api/{name}: the {level} consent text differs from limits.json, and the page shows one while the receiver records the other")

    def app_constant(name: str) -> str | None:
        m = re.search(r"const\s+" + name + r"\s*=\s*([^;]+);", app)
        return m.group(1).strip() if m else None

    for name, expected in [("MAX_FILE_BYTES", f'{limit["maxFileMegabytes"]} * 1024 * 1024'), ("MAX_PACKAGE_BYTES", f'{limit["maxAppPackageMegabytes"]} * 1024 * 1024')]:
        got = app_constant(name)
        if got is None or not got.startswith(expected):
            found.append(f"website/api/app-submission.php: {name} is {got!r} and limits.json says {expected!r}")

    # Entries 207 and 208: the survey's receiver takes reports only while limits.json says the survey is open, so it cannot be answering
    # before the worker that deletes what it stores is installed.
    survey = need(REPO / "website" / "api" / "survey.php").read_text(encoding="utf-8")
    want = "true" if limit["surveyOpen"] else "false"
    if f"const OPEN = {want};" not in survey:
        found.append(f"website/api/survey.php: OPEN is not {want}, and limits.json's surveyOpen is {limit['surveyOpen']}")

    # Entry 357: the application's receiver takes "Send everything I open" only while limits.json offers it, and the error receivers take
    # the log package's reference only while limits.json says error reports carry it.
    for file, const, key in [("app-submission.php", "SEND_EVERYTHING_OPEN", "sendEverythingOpen"), ("error-report.php", "FULL_LOG_REPORTS", "fullLogErrorReports")]:
        source = need(REPO / "website" / "api" / file).read_text(encoding="utf-8")
        want = "true" if limit[key] else "false"
        if f"const {const} = {want};" not in source:
            found.append(f"website/api/{file}: {const} is not {want}, and limits.json's {key} is {limit[key]}")

    # Entry 129, Alan's decision 6: no PDF. The refusal has to be in the receiver, not only on the page.
    if "'application/pdf'" in php.split("const ACCEPTED")[-1].split("];")[0]:
        found.append("website/api/upload.php: PDF is in ACCEPTED, and Alan's decision 6 refuses it")

    return found


def page_send() -> str:
    limit = limits()
    types = ", ".join(limit["types"][:-1]) + " and " + limit["types"][-1]

    def ask(name: str, label: str, options: list | None = None, placeholder: str = "", maxlength: int = 60, area: bool = False) -> str:
        if options is not None:
            choices = "".join(f"<option>{esc(o)}</option>" for o in options)
            field = f'<select id="{name}" name="{name}"><option value="">No answer</option>{choices}</select>'
        elif area:
            field = f'<textarea id="{name}" name="{name}" rows="3" maxlength="{maxlength}" placeholder="{esc(placeholder)}"></textarea>'
        else:
            field = f'<input type="text" id="{name}" name="{name}" maxlength="{maxlength}" placeholder="{esc(placeholder)}" autocomplete="off">'
        return f'<div class="ask"><label for="{name}">{esc(label)}</label>{field}</div>'

    body = f"""
<section class="wrap page-head">
<div class="stack">
<p class="eyebrow">Send a target &middot; <a class="plain" href="/shoot-a-target/">How to shoot one</a></p>
<h1>Send your target photos.</h1>
<p class="lead">No account, no email address, no follow up. Everything on this page is optional except the consent box.</p>
</div>
</section>

<section class="wrap section top">
<div class="panel pad stack send-terms">
<h2 class="h3">What happens to them</h2>
<ul>
<li>They go straight into a holding area on the server that is not reachable from the web.</li>
<li>Every photo is then <strong>rebuilt from its pixels</strong> and the file you sent is deleted. Your camera's make, model, lens, focal length, exposure and resolution are carried across, because the measurements need them. <strong>Location, GPS and the date and time are not, and cannot be</strong>, because the new file is built from pixels and a short list of numbers rather than copied.</li>
<li>They are <strong>deleted from the server</strong> as soon as they have been checked and copied: onto the developer's own machine, and into the project's private archive hosted by GitHub, which is never made public. Anything refused is deleted within seven days, and nothing waits on the server longer than sixty. <a href="/research/what-grouplab-sends/">Where each thing is kept, and for how long</a>.</li>
<li>If you tick the box below, they are used for testing only and never published.</li>
</ul>
</div>
</section>

<section class="wrap section last">
<form id="send" class="send-form stack" method="post" action="/api/upload.php" enctype="multipart/form-data" data-max-files="{limit["maxFiles"]}" data-max-file-mb="{limit["maxFileMegabytes"]}" data-max-send-mb="{limit["maxSubmissionMegabytes"]}" data-max-set-files="{limit["maxSetFiles"]}" data-max-set-mb="{limit["maxSetMegabytes"]}">
<noscript><p class="small warn">Sending needs JavaScript, because of the check that proves you are a person. Reading this page does not.</p></noscript>

<h2 class="h3">The photos</h2>
<p class="small faint">Up to {limit["maxSetFiles"]} files, {limit["maxFileMegabytes"]} MB each and {limit["maxSetMegabytes"]} MB in total, so a set shot across several sheets goes in one go: the page sends it in parts of up to {limit["maxFiles"]} files and {limit["maxSubmissionMegabytes"]} MB by itself, and it arrives as one submission. {types}. Not {esc(limit["typesRefused"][0])}: it is not a photograph and the rebuild cannot handle it. Send them as they came off the camera, not cropped and not through a messaging app.</p>
<input type="file" id="photos" name="photos[]" multiple accept="image/jpeg,image/png,image/tiff,image/heic,image/heif,.jpg,.jpeg,.png,.tif,.tiff,.heic,.heif" required>
<div id="setLabels" class="stack tight" hidden>
<p class="small faint">If these are several sheets of one set, say which sheet and which view each photo is. Leave any you are not sure of.</p>
<label class="small">How many sheets? <select id="setSheets"><option value="1">1</option><option value="2">2</option><option value="3">3</option><option value="4">4</option><option value="5">5</option></select></label>
<div id="setRows" class="stack tight"></div>
</div>

<h2 class="h3">About the target</h2>
<p class="small faint">Answer what you can be bothered to answer and leave the rest. Every question here is one that cannot be read off the image.</p>
<div class="asks">
{ask("backing", "What was the target attached to?", ["Cardboard", "OSB or plywood", "Corrugated plastic", "Steel frame", "Paper backer", "Nothing behind it", "Other or not sure"])}
{ask("attachment", "How was it attached?", ["Staples", "Tape", "Clips or clothespins", "Glued or stuck on", "Not attached, it was lying flat", "Other or not sure"])}
{ask("shot_distance", "What distance did you shoot it at?", placeholder="100 yards", maxlength=40)}
{ask("caliber", "Bullet diameter?", placeholder="0.264 in, or 6.5 mm", maxlength=40)}
</div>
<p class="small faint">The diameter, not the cartridge name, because that is what measures a hole: a 6.5 Creedmoor and a 6.5x55 both put a 0.264 in bullet through the paper. If you only know the cartridge, write that instead and it will be worked out.</p>
<div class="asks">
{ask("notes", "Anything else worth knowing?", placeholder="A pulled shot, a wrong bull, the wind", maxlength=300, area=True)}
{ask("credit_name", "Name or handle, if you want credit.", placeholder="Leave blank to stay anonymous", maxlength=60)}
</div>

<div class="hp" aria-hidden="true">
<label for="contact_reason">Do not fill this in</label>
<input type="text" id="contact_reason" name="contact_reason" tabindex="-1" autocomplete="off">
</div>

<fieldset class="panel pad stack tight consent">
<legend>How may GroupLab use them? Choose one.</legend>
<label class="tick"><input type="radio" id="level_testing" name="level" value="testing" required><span><strong>Testing only.</strong> {esc(limit["consentTexts"]["testing"])}</span></label>
<label class="tick"><input type="radio" id="level_publishable" name="level" value="publishable"><span><strong>May be published.</strong> {esc(limit["consentTexts"]["publishable"])}</span></label>
</fieldset>

<div class="cf-turnstile" data-sitekey="{esc(limit["turnstileSiteKey"])}"></div>

<div class="actions">
<button type="submit" class="btn btn-primary btn-big" id="sendButton"><span>Send the photos</span></button>
</div>
<progress id="sendProgress" max="1" value="0" hidden></progress>
<p id="sendStatus" class="small" role="status" aria-live="polite"></p>
</form>
</section>

<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<script src="/assets/js/send.js" defer></script>
"""
    return shell(SEND, "Send a target",
                 "Send photographs of a target you have shot, to help prove GroupLab measures correctly. No account and no email address.",
                 body, "Send a target")


def page_send_moved() -> str:
    """The old address, entry 173 section 2.2. A plain page with one link, not a meta refresh, which entry 151 bans: nobody holding
    the old link is sent anywhere they did not choose to go. A real 301 would be an nginx change, and this needs none."""
    body = f"""
<section class="wrap page-head stack last">
<p class="eyebrow">Send a target</p>
<h1>This page has moved.</h1>
<p class="lead">Sending a target is at <a href="{SEND}">grouplab.org/targets</a> now. Nothing else about it has changed.</p>
<div class="actions">{btn("Go to the send page", SEND, True)}</div>
</section>
"""
    return shell(SEND_BEFORE, "Send a target", "The page for sending a target has moved to grouplab.org/targets.", body, "Send a target")


def page_support() -> str:
    donate = ""
    if DONATION_URL:
        donate = f'{btn(DONATION_LABEL, DONATION_URL)}'
    donation_note = (
        "The most useful help is a shot target. If you would also like to give money towards the project's costs, the button beside this goes to the donation page."
        if DONATION_URL
        else "The most useful help is a shot target."
    )
    body = f"""
<section class="wrap page-head">
<p class="eyebrow">Support</p>
<h1>Something wrong? Tell us.</h1>
<p class="lead">GroupLab is a test build that changes whenever the code does. A report with the build it came from is the most useful thing you can send.</p>
</section>
<section class="wrap section-sm">
<div class="panel pad stack tight">
<h2 class="h3">Discord</h2>
<p>Questions, bug reports, target sheets, and what people are shooting. The project's developer reads it.</p>
<p class="small">A post in the bug-reports forum becomes a bug report for the developer, and the pictures you post there go on that report and nowhere else. For anything private, the support address below is better.</p>
<div class="actions">{btn("What is in the Discord", DISCORD, True)}</div>
</div>
</section>
<section class="wrap grid-3">
<div class="panel pad stack tight"><span class="mono amber">01</span><h2 class="h3">Check what is not done yet</h2><p>Some things are known to be missing or unproven. The testing guide lists them in one short section.</p><a href="/guides/testing-guide/#what-is-not-done-yet">What is not done yet</a></div>
<div class="panel pad stack tight"><span class="mono amber">02</span><h2 class="h3">Make a report package</h2><p>In GroupLab, open the settings from the gear at the bottom left and choose <strong>Report a problem</strong> under Diagnostics. A window lists what the report holds; press <strong>Save only</strong> to write it as a zip with the log and the computer's details: no location data and no photographs.</p></div>
<div class="panel pad stack tight"><span class="mono amber">03</span><h2 class="h3">Send it</h2><p>Open an issue on GitHub, attach the zip, and add a line about what you were doing. Copy the build line from the top of the settings screen into it.</p><a href="{GITHUB}/issues/new">Open an issue on GitHub</a></div>
</section>
<section class="wrap section-sm grid-2">
<div class="panel pad stack tight"><h2 class="h3">No GitHub account?</h2><p>Email the report package instead, with the build line and a line about what you were doing.</p><p><a class="mono" href="mailto:{SUPPORT_EMAIL}">{SUPPORT_EMAIL}</a></p></div>
<div class="panel pad stack tight"><h2 class="h3">On a phone</h2><p>On Android, iPhone and iPad, open Settings, then About: <strong>Send to GroupLab</strong> sends the newest logs and any crash record straight to the project, with a note if you write one, and gives you a reference; <strong>Send diagnostics</strong> puts the newest logs and crash records in one zip and hands it to the share sheet, so you can attach it to an issue or an email. On the iPhone and iPad beta, <strong>Send Beta Feedback</strong> in TestFlight, or a screenshot shared to it, reaches the project too.</p></div>
<div class="panel pad stack tight"><h2 class="h3">If a new build will not start</h2><p>On Windows, nothing of yours is at risk: settings, sessions, your own sheets and the log live in <code>%APPDATA%\\GroupLab</code>, which no installer or uninstaller touches. To go back, download the build you were on from the releases list and install it over the broken one.</p><a href="{GITHUB}/releases">All builds on GitHub</a></div>
</section>
<section class="wrap section-sm" id="delete-your-data">
<div class="panel pad stack tight">
<h2 class="h3">Delete your data</h2>
<p>GroupLab has no accounts, so nothing is kept against a name or a login. What the project can hold is only what you chose to send: targets you sent (the picture and what GroupLab worked out about it), error reports with their logs, and hardware survey reports, which are counted into totals and deleted, keyed by a random number GroupLab made for your installation.</p>
<p>To have what you sent deleted, email <a class="mono" href="mailto:{SUPPORT_EMAIL}">{SUPPORT_EMAIL}</a> with the date and the build of what was sent (the build line is at the top of Settings), so it can be found. Survey reports need no email: Settings, under Sharing, has <strong>Delete my survey reports</strong>. <a href="/research/what-grouplab-sends/">What GroupLab sends</a> lists everything that can leave your computer or phone.</p>
</div>
</section>
<section class="wrap section-sm last">
<div class="callout">
<div class="stack">
<p class="eyebrow amber">Support the project</p>
<h2>Free for everyone, and it stays that way.</h2>
<p class="text">{donation_note}</p>
</div>
<div class="actions">
{btn("Shoot a target", "/shoot-a-target/", True)}
{donate}
</div>
</div>
</section>
"""
    return shell("/support/", "Support", "How to report a problem with GroupLab: make a report package and send it through GitHub or by email.", body, "Support")


# ---------------------------------------------------------------- guides

GUIDES = [
    ("user-guide", "USER-GUIDE.md", "user-guide.pdf", "User guide",
     "The GroupLab user guide: one sheet from the printer to the analysis, with renders of the current build."),
    ("testing-guide", "TESTING-GUIDE.md", "testing-guide.pdf", "Trying GroupLab",
     "Trying GroupLab for the first time: getting the test build, the first minute with no rifle, and what is not done yet."),
    # Entry 438: the statistics primer, for a reader who has never taken a statistics class.
    ("statistics-primer", "STATISTICS-PRIMER.md", "statistics-primer.pdf", "Statistics for shooters",
     "Statistics for shooters, from the beginning: group size, sigma, CEP, confidence and p-values in plain words, with what each "
     "means at the range and how to judge what is good enough."),
]
PRIMER_DATA = REPO / "docs" / "figures" / "primer" / "primer-data.json"
# Entry 438 section 4.1: the primer's one-page summary, made from the guide by `grouplab user-guide`, in both paper sizes.
PRIMER_SUMMARIES = [("Letter", "STATISTICS-PRIMER-SUMMARY-LETTER.pdf", "statistics-primer-summary-letter.pdf"),
                    ("A4", "STATISTICS-PRIMER-SUMMARY-A4.pdf", "statistics-primer-summary-a4.pdf")]


def slug(text: str) -> str:
    s = re.sub(r"<[^>]+>", "", text).lower()
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return s


def render_guide(md_file: Path, links: bool = False) -> tuple[str, str, list[tuple[str, str, list[tuple[str, str]]]]]:
    src = md_file.read_text(encoding="utf-8")
    title_m = re.match(r"#\s+(.+)\n", src)
    title = title_m.group(1).strip() if title_m else md_file.stem
    src = src[title_m.end():] if title_m else src
    # Entry 437 section 2.2: a picture's markings are one line under it, "<!-- marks: Find holes | review queue -->", naming controls on
    # that screen in the order the text mentions them; the line is taken out here and the names drawn over the picture below.
    marks: dict[str, list[str]] = {}

    def take(m: re.Match) -> str:
        marks[m.group(2)] = [n.strip() for n in m.group(3).split("|") if n.strip()]
        return m.group(1)

    src = re.sub(r"^(!\[[^\]]*\]\(([^)]+)\))[ \t]*\r?\n<!-- marks: (.*?) -->[ \t]*$", take, src, flags=re.M)
    # GitHub starts a list straight after a line of text; Python-Markdown needs a blank line first.
    lines, fixed = src.split("\n"), []
    for i, line in enumerate(lines):
        is_item = re.match(r"\s*(?:[-*+]|\d+\.)\s", line) is not None
        prev = fixed[-1] if fixed else ""
        prev_item = re.match(r"\s*(?:[-*+]|\d+\.)\s", prev) is not None
        if is_item and prev.strip() and not prev_item and not prev.startswith((" ", "\t")):
            fixed.append("")
        fixed.append(line)
    src = "\n".join(fixed)
    body = markdown.markdown(src, extensions=["tables", "sane_lists", "fenced_code"])

    # Headings get ids, and the contents list is each h2 with its h3s under it. Entry 436 section 3.1: the h2 ids never change, because
    # features.json and every link already sent point at them; an h3 that would repeat an id carries its section's number in front.
    toc: list[tuple[str, str, list[tuple[str, str]]]] = []
    seen: set[str] = set()

    def head(m: re.Match) -> str:
        level, inner = m.group(1), m.group(2)
        text = re.sub(r"<[^>]+>", "", inner)
        hid = slug(inner)
        if level == "3" and hid in seen and toc:
            number = re.match(r"(\d+)-", toc[-1][0])
            hid = f"{number.group(1) if number else toc[-1][0]}-{hid}"
        seen.add(hid)
        if level == "2":
            toc.append((hid, text, []))
        elif toc:
            toc[-1][2].append((hid, text))
        # Entry 436 section 3.2: a link mark, shown on hover and on focus; guide-toc.js copies its address, and without it it is a link.
        mark = f'<a class="hlink" href="#{hid}" aria-label="Link to {esc(html.unescape(text))}">#</a>' if links else ""
        return f'<h{level} id="{hid}">{inner}{mark}</h{level}>'

    body = re.sub(r"<h([23])>(.*?)</h\1>", head, body)

    # Screenshots: repository renders become the site's WebP pair, one per theme; a phone's picture is shown at a phone's size.
    page = GUIDE_PAGES.get(md_file.name, md_file.name)

    def img(src_attr: str, alt: str) -> str:
        desk = re.search(r"figures/screens/current/([a-z0-9-]+?)-(light|dark)-1400x900\.png", src_attr)
        phone = re.search(r"figures/screens/phone/([a-z0-9-]+?)-(light|dark)\.png", src_attr)
        if not desk and not phone:
            sys.exit(f"build: guide image not in screens/current or screens/phone: {src_attr}")
        name = (desk or phone).group(1)
        names = marks.get(src_attr, [])
        fig = f"fig-{name}"
        if desk:
            pic = screen(name, html.unescape(alt))
            overlay = mark_overlay(name, names, fig, page) if names else ""
            pic = f'<div class="fig-marks">{pic}{overlay}</div>' if overlay else pic
        else:
            pic = phone_screen(name, html.unescape(alt))
            if names:
                GUIDE_FIGURE_PROBLEMS.add(f"{page}: the phone picture {name} has markings, and no phone picture has its controls' positions yet")
        caption = caption_refs(alt, fig, len(names), page) if names else alt
        count = f' data-marks="{len(names)}"' if names else ""
        return f'<figure class="guide-fig" id="{fig}"{count}>{pic}<figcaption class="mono faint small">{caption}</figcaption></figure>'

    body = re.sub(r'<p><img alt="([^"]*)" src="([^"]+)" ?/?></p>', lambda m: img(m.group(2), m.group(1)), body)
    body = re.sub(r'<p><img src="([^"]+)" alt="([^"]*)" ?/?></p>', lambda m: img(m.group(1), m.group(2)), body)
    body = text_refs(body, page)

    # Links: relative repository links go to GitHub.
    def link(m: re.Match) -> str:
        href = m.group(1)
        if href.startswith(("http://", "https://", "#", "mailto:")):
            return m.group(0)
        # Entry 438: a link into another guide goes to that guide's page on the site, to the same heading.
        md_name, _, fragment = href.partition("#")
        if md_name in GUIDE_KEYS:
            return f'href="/guides/{GUIDE_KEYS[md_name]}/{"#" + fragment if fragment else ""}"'
        return f'href="{GITHUB}/blob/main/docs/{href}"'

    body = re.sub(r'href="([^"]+)"', link, body)
    # Entry 438 section 2.3: the math comes last in a section, folded, and a reader can skip it without losing the thread.
    body = re.sub(r"<!-- math -->\s*(.*?)\s*<!-- /math -->",
                  lambda m: f'<details class="dl-fold math-fold"><summary>The math, if you want it</summary><div class="dl-fold-body">{m.group(1)}</div></details>',
                  body, flags=re.S)
    body = re.sub(r"<!-- figure: ([a-z-]+) -->", lambda m: primer_figure(m.group(1)), body)
    body = body.replace("<!-- summary -->", "")
    return title, body, toc


# Entry 437: which built page each guide becomes, so a marking's problem names the page it holds back; and the problems found while
# rendering, read by guide_problems.
GUIDE_PAGES = {g[1]: f"guides/{g[0]}/index.html" for g in GUIDES}
GUIDE_KEYS = {g[1]: g[0] for g in GUIDES}
GUIDE_FIGURE_PROBLEMS: set[str] = set()
BADGE = 22  # half a badge's width, in a 1400 pixel picture's own pixels, at the width the guide shows it


def primer() -> dict:
    return json.loads(need(PRIMER_DATA).read_text(encoding="utf-8"))


def svg_group(shots: list, x0: float, y0: float, half: float, scale: float, extra: str = "") -> str:
    """One group drawn in an SVG at (x0, y0), its aim point at the middle of a square half wide, in MOA times scale pixels."""
    rings = "".join(f'<circle class="pf-ring" cx="{x0}" cy="{y0}" r="{r * scale:.1f}"/>' for r in (0.5, 1.0, 1.5) if r * scale < half)
    dots = "".join(f'<circle class="pf-shot" cx="{x0 + x * scale:.1f}" cy="{y0 - y * scale:.1f}" r="3.2"/>' for x, y in shots)
    return f'<rect class="pf-frame" x="{x0 - half}" y="{y0 - half}" width="{2 * half}" height="{2 * half}" rx="6"/>{rings}{extra}{dots}'


def farthest(shots: list) -> tuple:
    best = (0.0, 0, 0)
    for i in range(len(shots)):
        for j in range(i + 1, len(shots)):
            d = ((shots[i][0] - shots[j][0]) ** 2 + (shots[i][1] - shots[j][1]) ** 2) ** 0.5
            best = max(best, (d, i, j))
    return best


def centre_of(shots: list) -> tuple:
    return sum(x for x, _ in shots) / len(shots), sum(y for _, y in shots) / len(shots)


def primer_figure(name: str) -> str:
    """Entry 438 section 2.5: each of the primer's figures, drawn here from GroupLab's own numbers (docs/figures/primer/primer-data.json),
    so the page is complete and still with scripts off; primer.js adds the buttons that step through more of the same numbers."""
    d = primer()
    caption, svg, controls = "", "", ""
    if name in ("centre", "mean-radius", "cep"):
        shots = d["groups"]["10" if name != "cep" else "20"][0]
        cx, cy = centre_of(shots["shots"])
        scale, half = 110.0, 180.0
        extra = ""
        if name == "centre":
            extra = (f'<path class="pf-center" d="M{180 + cx * scale - 10:.1f} {180 - cy * scale:.1f}h20M{180 + cx * scale:.1f} {180 - cy * scale - 10:.1f}v20"/>')
            caption = "Ten shots from one rifle, and their center, the amber cross: the average place they landed. The rings are half an MOA apart, around the aim."
        elif name == "mean-radius":
            spokes = "".join(f'<path class="pf-spoke" d="M{180 + cx * scale:.1f} {180 - cy * scale:.1f}L{180 + x * scale:.1f} {180 - y * scale:.1f}"/>' for x, y in shots["shots"])
            extra = spokes + f'<circle class="pf-center-ring" cx="{180 + cx * scale:.1f}" cy="{180 - cy * scale:.1f}" r="{shots["mr"] * scale:.1f}"/>'
            caption = (f"The same kind of group with a line from its center to every shot. Their average length is the mean radius, here "
                       f"{shots['mr']:.2f} MOA, drawn as the amber circle.")
        else:
            c50, c90 = shots["sigma"] * d["cep50Factor"], shots["sigma"] * 2.146
            extra = (f'<circle class="pf-center-ring" cx="{180 + cx * scale:.1f}" cy="{180 - cy * scale:.1f}" r="{c50 * scale:.1f}"/>'
                     f'<circle class="pf-center-ring pf-dashed" cx="{180 + cx * scale:.1f}" cy="{180 - cy * scale:.1f}" r="{c90 * scale:.1f}"/>')
            caption = (f"Twenty shots with GroupLab's CEP 50, the solid amber circle ({c50:.2f} MOA), which should hold about half of a "
                       f"rifle's shots, and CEP 90, the dashed one ({c90:.2f} MOA), about nine in ten.")
        svg = f'<svg class="pf" viewBox="0 0 360 360" role="img" aria-label="{esc(caption)}">{svg_group(shots["shots"], 180, 180, half, scale, extra)}</svg>'
    elif name == "five-more":
        svg = primer_groups_svg(d["groups"]["5"][:6])
        caption = ("Six groups from the same rifle, which never changes. The line joins the two shots farthest apart, and the "
                   "number is that extreme spread in MOA.")
        controls = ('<div class="pf-controls" hidden><button type="button" class="btn btn-secondary" data-act="more">Shoot five more</button>'
                    '<span class="pf-sizes" role="group" aria-label="Shots in each group">'
                    + "".join(f'<button type="button" class="pf-size" data-n="{n}" aria-pressed="{str(n == 5).lower()}">{n} shots</button>' for n in (5, 10, 20))
                    + '</span><p class="pf-said small" aria-live="polite"></p></div>')
    elif name == "luck":
        rows = d["spread"]
        width, left, top, gap = 640, 230, 20, 46
        x = lambda v: left + v / 2.5 * (width - left - 30)
        bars = []
        for i, r in enumerate(rows):
            y = top + i * gap
            bars.append(f'<text class="pf-label" x="16" y="{y + 14}">{r["n"]} shots</text>'
                        f'<text class="pf-label" x="{left - 14}" y="{y + 14}" text-anchor="end">{r["esLow"]:.1f} to {r["esHigh"]:.1f}</text>'
                        f'<rect class="pf-bar" x="{x(r["esLow"]):.1f}" y="{y}" width="{x(r["esHigh"]) - x(r["esLow"]):.1f}" height="20" rx="4"/>'
                        f'<path class="pf-mid" d="M{x(r["esMid"]):.1f} {y - 3}v26"/>')
        axis_y = top + len(rows) * gap
        ticks = "".join(f'<path class="pf-tick" d="M{x(v):.1f} {axis_y}v6"/><text class="pf-label" x="{x(v):.1f}" y="{axis_y + 22}" text-anchor="middle">{v:g}</text>' for v in (0, 0.5, 1, 1.5, 2, 2.5))
        svg = (f'<svg class="pf" viewBox="0 0 {width} {axis_y + 36}" role="img" aria-label="Extreme spread, the middle nine groups in ten, by shots in a group">'
               f'{"".join(bars)}<path class="pf-tick" d="M{left} {axis_y}H{width - 30}"/>{ticks}</svg>')
        caption = ("How far one rifle's extreme spread wanders, in MOA: each bar holds the middle nine groups in ten of 4000, and the mark is "
                   "the middle one. The rifle is the same in every row; only the number of shots changes.")
    elif name == "same-loads":
        five = next(s for s in d["sameLoads"] if s["n"] == 5)
        ex = five["examples"][0]
        svg = primer_pair_svg(ex)
        caption = (f"Two loads that are exactly the same, five shots each. Compared {five['trials']} times, one looked at least 20 percent "
                   f"tighter by extreme spread {round(five['looksClearlyBetter'] * 100)} times in 100, and the test called them different "
                   f"{round(five['testSaysDifferent'] * 100)} times in 100.")
        controls = ('<div class="pf-controls" hidden><button type="button" class="btn btn-secondary" data-act="again">Compare again</button>'
                    '<p class="pf-said small" aria-live="polite"></p></div>')
    else:
        sys.exit(f"build: the primer names a figure that is not drawn: {name}")
    return f'<figure class="guide-fig primer-fig" data-primer="{name}">{svg}{controls}<figcaption class="mono faint small">{esc(caption)}</figcaption></figure>'


def primer_groups_svg(groups: list) -> str:
    """Six groups in two rows of three, each with its farthest pair joined and its extreme spread written beneath."""
    cells = []
    for i, g in enumerate(groups):
        x0, y0 = 105 + (i % 3) * 215, 105 + (i // 3) * 240
        _, a, b = farthest(g["shots"])
        (ax, ay), (bx, by) = g["shots"][a], g["shots"][b]
        line = f'<path class="pf-es" d="M{x0 + ax * 70:.1f} {y0 - ay * 70:.1f}L{x0 + bx * 70:.1f} {y0 - by * 70:.1f}"/>'
        cells.append(svg_group(g["shots"], x0, y0, 95, 70, line) + f'<text class="pf-label" x="{x0}" y="{y0 + 122}" text-anchor="middle">{g["es"]:.2f} MOA</text>')
    return f'<svg class="pf" viewBox="0 0 640 475" role="img" aria-label="Six groups from the same rifle with their extreme spreads">{"".join(cells)}</svg>'


def primer_pair_svg(ex: dict) -> str:
    cells = []
    for i, (key, es) in enumerate((("a", ex["esA"]), ("b", ex["esB"]))):
        x0 = 160 + i * 320
        _, a, b = farthest(ex[key])
        (ax, ay), (bx, by) = ex[key][a], ex[key][b]
        line = f'<path class="pf-es" d="M{x0 + ax * 65:.1f} {130 - ay * 65:.1f}L{x0 + bx * 65:.1f} {130 - by * 65:.1f}"/>'
        cells.append(svg_group(ex[key], x0, 130, 120, 65, line)
                     + f'<text class="pf-label" x="{x0}" y="{280}" text-anchor="middle">Load {key.upper()}: {es:.2f} MOA</text>')
    return f'<svg class="pf" viewBox="0 0 640 300" role="img" aria-label="Two identical loads, five shots each, side by side">{"".join(cells)}</svg>'


def mark_overlay(name: str, names: list[str], fig: str, page: str) -> str:
    """Entry 437 section 3: an outline round each named control and a numbered badge on its top left corner, placed from the screen's
    positions file in the picture's own pixels and laid over the picture, so they scale with it and the PNG is untouched. A badge at the
    picture's edge moves inside it, and one that would sit on another moves along its outline."""
    path = SCREENS / f"{name}-bounds.json"
    if not path.exists():
        GUIDE_FIGURE_PROBLEMS.add(f"{page}: the picture {name} has markings and no positions file, docs/figures/screens/current/{name}-bounds.json")
        return ""
    data = json.loads(path.read_text(encoding="utf-8"))
    w, h = data["width"], data["height"]
    if len(names) > 3:
        GUIDE_FIGURE_PROBLEMS.add(f"{page}: the picture {name} has {len(names)} markings; three at most (entry 437 section 3.6)")
    spans, placed = [], []
    for n, control in enumerate(names, 1):
        box = data["controls"].get(control)
        if box is None:
            GUIDE_FIGURE_PROBLEMS.add(f"{page}: the picture {name} marks {control!r}, which is not among its controls ({name}-bounds.json)")
            continue
        x, y, bw, bh = box
        bx, by = x, y
        for _ in range(12):
            bx, by = min(max(bx, BADGE), w - BADGE), min(max(by, BADGE), h - BADGE)
            if all(abs(bx - px) >= 2 * BADGE or abs(by - py) >= 2 * BADGE for px, py in placed):
                break
            if bx + 2 * BADGE + 4 <= x + bw:
                bx += 2 * BADGE + 4
            else:
                by += 2 * BADGE + 4
        placed.append((bx, by))

        def pct(v: float, t: float) -> str:
            return f"{100 * v / t:.3f}%"

        spans.append(f'<span class="mark-box" id="{fig}-{n}" style="left:{pct(x, w)};top:{pct(y, h)};width:{pct(bw, w)};height:{pct(bh, h)}"></span>'
                     f'<span class="mark-badge" style="left:{pct(bx, w)};top:{pct(by, h)}">{n}</span>')
    return f'<span class="marks" aria-hidden="true">{"".join(spans)}</span>' if spans else ""


def mark_ref(fig: str, n: int, text: str) -> str:
    return f'<span class="mark-ref" data-mark="{fig}-{n}" tabindex="0">{text}</span>'


def caption_refs(alt: str, fig: str, count: int, page: str) -> str:
    """Entry 437 section 3.3: the caption lists the markings, "1 Find holes, 2 the review queue", and each number points at its outline."""
    m = re.search(r"(?:^|(?<=[:.;] ))1 (?=\D)", alt)
    if m is None:
        GUIDE_FIGURE_PROBLEMS.add(f"{page}: the caption of {fig} does not list its markings as 1 ..., 2 ...")
        return alt
    tail, found = alt[m.start():], set()

    def one(k: re.Match) -> str:
        n = int(k.group(1))
        if n > count or n in found:
            return k.group(0)
        found.add(n)
        return mark_ref(fig, n, k.group(1)) + " "

    tail = re.sub(r"(?:^|(?<=, )|(?<=and ))([1-9]) (?=\D)", one, tail)
    if found != set(range(1, count + 1)):
        GUIDE_FIGURE_PROBLEMS.add(f"{page}: the caption of {fig} lists {sorted(found)} for {count} markings")
    return alt[:m.start()] + tail


def text_refs(body: str, page: str) -> str:
    """Entry 437 section 3.3: "Find holes (1)" in the text under a heading points at the marked picture under the same heading. A
    number with no marked picture there, or past its count, fails the page, as does a heading with two marked pictures."""
    out = []
    for part in re.split(r"(?=<h[23] )", body):
        figs = re.findall(r'<figure class="guide-fig" id="([^"]+)" data-marks="(\d)">', part)

        def one(k: re.Match) -> str:
            n = int(k.group(1))
            if len(figs) != 1 or n > int(figs[0][1]):
                GUIDE_FIGURE_PROBLEMS.add(f"{page}: ({n}) in the text has no marked picture with that number under its heading")
                return k.group(0)
            return mark_ref(figs[0][0], n, k.group(0))

        pieces = re.split(r"(<figure .*?</figure>|<[^>]+>)", part, flags=re.S)
        out.append("".join(p if p.startswith("<") else re.sub(r"\(([1-9])\)", one, p) for p in pieces))
    return "".join(out)


def guide_ids(page: str) -> list[str]:
    """Every id on a page, repeats kept, so a duplicate can be found."""
    return re.findall(r'\sid="([^"]+)"', page)


def guide_problems(out: Path = OUT) -> list[str]:
    """Entry 436 sections 3.1 and 4.5, the guides' own page checks, and from entry 445 every page with the guides' contents: every id once,
    every link to a part of the page has its target, and the side list names every heading in order in the HTML as built, so it is whole
    with scripts off. The user guide has subsections, and a link into another guide lands on a heading there."""
    found = sorted(GUIDE_FIGURE_PROBLEMS)
    guides = {f"guides/{key}/index.html" for key, *_ in GUIDES}
    pages = sorted(guides | {rel for rel in (p.relative_to(out).as_posix() for p in out.rglob("index.html"))
                             if CONTENTS_PAGES.match("/" + rel[: -len("index.html")])})
    for rel in pages:
        if not (out / rel).exists():
            continue
        page = (out / rel).read_text(encoding="utf-8")
        ids = guide_ids(page)
        for dup in sorted({i for i in ids if ids.count(i) > 1}):
            found.append(f"{rel}: the id {dup!r} is on more than one element")
        for target in sorted(set(re.findall(r'href="#([^"]+)"', page)) - set(ids)):
            found.append(f"{rel}: a link to #{target} has no heading or element with that id")
        side = re.search(r'<nav class="toc"[^>]*>(.*?)</nav>', page, re.S)
        if not side and rel not in guides:
            continue
        listed = re.findall(r'href="#([^"]+)"', side.group(1)) if side else []
        main = re.search(r"<main\b.*</main>", page, re.S)
        if listed != [h["target"] for h in headings(main.group(0) if main else page)]:
            found.append(f"{rel}: the contents list does not name every heading, in order, in the page as built")
        for other, target in sorted(set(re.findall(r'href="/guides/([a-z-]+)/#([^"]+)"', page))):
            built = out / "guides" / other / "index.html"
            if built.exists() and target not in guide_ids(built.read_text(encoding="utf-8")):
                found.append(f"{rel}: a link to /guides/{other}/#{target} has no heading with that id")
        if rel == "guides/user-guide/index.html" and "<h3 id=" not in page:
            found.append(f"{rel}: the user guide has no subsections (entry 436 section 1)")
    return found


def guide_tabs(current: str) -> str:
    tabs = []
    for key, _, _, label, _ in GUIDES:
        cur = ' aria-current="page"' if key == current else ""
        tabs.append(f'<a class="tab" href="/guides/{key}/"{cur}>{label}</a>')
    cur = ' aria-current="page"' if current == "glossary" else ""
    tabs.append(f'<a class="tab" href="{GLOSSARY_PATH}"{cur}>Glossary</a>')
    return "".join(tabs)


def toc_list(toc: list[tuple[str, str, list[tuple[str, str]]]], views: dict[str, str] | None = None) -> str:
    """Entry 436 section 2.1: every section with its subsections under it, all in the HTML, so the list is whole with scripts off. An entry
    for a heading only one side of the Desktop | Mobile switch shows carries that side, so the list follows the view (entry 445)."""
    views = views or {}

    def li(hid: str, text: str) -> str:
        view = f' data-show="{views[hid]}"' if hid in views else ""
        return f'<li{view}><a href="#{hid}">{esc(html.unescape(text))}</a>'
    items = []
    for hid, text, subs in toc:
        inner = "".join(f"{li(s, t)}</li>" for s, t in subs)
        items.append(li(hid, text) + (f"<ol>{inner}</ol>" if subs else "") + "</li>")
    return "<ol>\n" + "\n".join(items) + "\n</ol>"


class _Headings(HTMLParser):
    """Every h2 and h3 of a part of a page, with where it starts, its words, the address its contents link goes to and the side of the
    switch it is on. A heading inside a link is a card pointing somewhere else, not a section; a card's title (in an article or a
    folded block with an address, a feature or a release) goes to the card, so its picture is in view."""
    VOID = {"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"}

    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.stack: list[tuple[str, dict]] = []
        self.found: list[dict] = []
        self.cur: dict | None = None
        self.mark = 0

    def handle_starttag(self, tag: str, attrs: list) -> None:
        a = {k: v or "" for k, v in attrs}
        if tag in ("h2", "h3") and self.cur is None:
            inside = [t for t, _ in self.stack]
            card = next((x["id"] for t, x in reversed(self.stack) if t in ("article", "details") and x.get("id")), None)
            self.cur = {"level": int(tag[1]), "id": a.get("id"), "pos": self.getpos(), "words": [],
                        "skip": "a" in inside or "nav" in inside, "summary": "summary" in inside, "mark": False,
                        "target": a.get("id") or card, "view": next((x["data-show"] for _, x in reversed(self.stack) if x.get("data-show")), None)}
        if self.cur is not None and tag == "a":
            if "hlink" in a.get("class", "").split():
                self.cur["mark"] = True
                self.mark += 1
        if tag not in self.VOID:
            self.stack.append((tag, a))

    def handle_endtag(self, tag: str) -> None:
        for i in range(len(self.stack) - 1, -1, -1):
            if self.stack[i][0] == tag:
                del self.stack[i:]
                break
        if tag == "a" and self.mark:
            self.mark -= 1
        if self.cur is not None and tag == f"h{self.cur['level']}":
            self.cur["text"] = " ".join("".join(self.cur.pop("words")).split())
            if not self.cur["skip"]:
                self.found.append(self.cur)
            self.cur = None

    def handle_data(self, data: str) -> None:
        if self.cur is not None and not self.mark:
            self.cur["words"].append(data)


def headings(part: str) -> list[dict]:
    reader = _Headings()
    reader.feed(part)
    reader.close()
    return reader.found


def contents(part: str, used: set[str]) -> tuple[str, list[tuple[str, str, list[tuple[str, str]]]], dict[str, str]]:
    """Entry 445: the guides' contents for any part of a page. A heading with no address gets one from its words, and each heading that is
    its own section gets its link mark. Returns the part, the list as toc_list takes it, and which side of the switch each entry is on."""
    starts = [0] + [i + 1 for i, c in enumerate(part) if c == chr(10)]
    edits, toc, views = [], [], {}
    for h in headings(part):
        at = starts[h["pos"][0] - 1] + h["pos"][1]
        target = h["target"]
        if not target:
            target = base = slug(h["text"]) or "section"
            n = 2
            while target in used:
                target, n = f"{base}-{n}", n + 1
            used.add(target)
            edits.append((at + 3, f' id="{target}"'))
        if not h["mark"] and not h["summary"]:
            close = part.index(f"</h{h['level']}>", at)
            edits.append((close, f'<a class="hlink" href="#{target}" aria-label="Link to {esc(h["text"])}">#</a>'))
        if h["view"]:
            views[target] = h["view"]
        if h["level"] == 2 or not toc:
            toc.append((target, esc(h["text"]), []))
        else:
            toc[-1][2].append((target, esc(h["text"])))
    for at, text in sorted(edits, reverse=True):
        part = part[:at] + text + part[at:]
    return part, toc, views


def contents_navs(toc: list, views: dict[str, str]) -> tuple[str, str]:
    """The side list and, for below 1100 px where the side list is hidden, the same list folded at the top (entry 436 section 2.4)."""
    items = toc_list(toc, views)
    side = f'<nav class="toc" aria-label="On this page">\n<p class="mono faint small caps">On this page</p>\n{items}\n</nav>\n'
    mini = (f'<details class="dl-fold toc-mini">\n<summary>On this page</summary>\n<div class="dl-fold-body">\n'
            f'<nav aria-label="On this page, short">\n{items}\n</nav>\n</div>\n</details>\n')
    return side, mini


# Entry 445: the pages with the guides' "On this page": the guides, the tour's screens, every research article and its index, the release
# notes, the features, support, what can be measured, shooting a target and the community. Not the download page (one section a device,
# behind its own picker), the tour's index (its headings are links to the screens) or how it works (its map is its contents).
CONTENTS_PAGES = re.compile(r"^/(?:guides/(?!glossary/)[^/]+|tour/(?!how-it-works/)[^/]+|research(?:/[^/]+)?|releases|features|support"
                            r"|what-can-be-measured|shoot-a-target|discord)/$")


def with_contents(body: str) -> str:
    """Entry 445: one function puts the guides' contents on every page that has them. On a page laid out as a guide the side list sits
    beside the article and the folded list under its title; on any other the list sits beside everything after the top band. A page
    with fewer than three entries is left as it was."""
    used = set(re.findall(r'\bid="([^"]+)"', body))
    guide = re.search(r'<section class="wrap guide">\s*(<article class="prose)', body)
    if guide:
        start, end = guide.start(1), body.rindex("</article>")
        part, toc, views = contents(body[start:end], used)
        if sum(1 + len(subs) for _, _, subs in toc) < 3:
            return body
        side, mini = contents_navs(toc, views)
        part = part.replace("<article ", "<article data-contents ", 1)
        if "</h1>" in part:
            part = part.replace("</h1>", "</h1>\n" + mini, 1)
        body = body[:start] + side + part + body[end:]
    else:
        top = re.search(r'<section class="wrap[^"]*\bpage-(?:head|top)\b.*?</section>', body, re.S)
        cut = top.end() if top else 0
        part, toc, views = contents(body[cut:], used)
        if sum(1 + len(subs) for _, _, subs in toc) < 3:
            return body
        side, mini = contents_navs(toc, views)
        body = (body[:cut] + f'\n<section class="wrap guide toc-page">\n{side}<div class="toc-scope" data-contents>\n{mini}{part}</div>\n'
                '</section>\n')
    return body + '<script src="/assets/js/guide-toc.js" defer></script>\n'


def page_guide(key: str, md: str, pdf: str, label: str, desc: str) -> str:
    title, content, _ = render_guide(need(REPO / "docs" / md), links=True)
    # Entry 436 section 2: the contents beside the guide and folded under its title are with_contents's, as on every long page (entry 445).
    body = f"""
<section class="wrap guide-bar">
<nav class="tabs" aria-label="Guides">{guide_tabs(key)}</nav>
<a class="pdf-link" href="/guides/{pdf}">Download as PDF</a>
</section>
<section class="wrap guide">
<article class="prose">
<h1>{esc(title)}</h1>
{content}
</article>
</section>
{'<script src="/assets/js/primer.js" defer></script>' if key == "statistics-primer" else ""}
"""
    if key == "statistics-primer":
        # Entry 438 section 4.1: the one-page summary, linked at the top and at the bottom.
        summary = ('<p class="primer-summary small">Five things to know, on one page to print or pass on: '
                   + " or ".join(f'<a href="/guides/{published}">{paper}</a>' for paper, _, published in PRIMER_SUMMARIES) + ".</p>")
        body = body.replace("<h1>", summary + "\n<h1>", 1).replace("</article>", summary + "\n</article>", 1)
        return shell(f"/guides/{key}/", label, desc, body, "Guides", image="og-primer.png")
    return shell(f"/guides/{key}/", label, desc, body, "Guides")


def page_what_can_be_measured() -> str:
    """docs/WHAT-CAN-BE-MEASURED.md as a page, NOTES-FROM-PLANNING.md entry 152 section 5.2.

    Two statements published on the tour were wrong about what GroupLab measures and about what a mis-scaled print
    costs, and a research article on this same site said the opposite of the second one. A reader who read both
    learned that the site cannot be trusted rather than which sentence was right. The wrong wordings are quoted in
    NOTES-FROM-PLANNING.md entry 152, which is where a log of them belongs; a test fails if they come back here.

    So there is one source and everything else points at it: the tour pages, the research articles and the README.
    A page that states its own version of this is the fault coming back.
    """
    title, content, _ = render_guide(need(REPO / "docs" / "WHAT-CAN-BE-MEASURED.md"))
    body = f"""
<section class="wrap guide">
<article class="prose">
<h1>{esc(title)}</h1>
{content}
</article>
</section>
"""
    return shell("/what-can-be-measured/", "What GroupLab can measure",
                 "Where the scale comes from, what a GroupLab sheet adds, and what a sheet printed at the wrong size does to every figure.",
                 body, "Tour")


# ---------------------------------------------------------------- the home page, concept B ("Orbit")

# Entry 440 built concept B at /testpage/ for Alan to approve, and entry 441 made it the home page (Alan, 2026-10-18: "Yes on the new
# home page design"). The page before it is in git history at b216ce56, page_home in website/build.py. /testpage/ is now a page with one
# link to /, so there are never two copies of the home page; home_problems holds the home page indexed and in the sitemap.
TESTPAGE_PATH = "/testpage/"
# The orbit's screens, as drawn: the inner ring, then the outer. A name with "fold-" is a phone screen.
HOME_INNER = ["analysis-open", "compare", "marking", "ballistics-hit"]
HOME_OUTER = ["groups", "fold-result", "sessions", "analysis-open", "fold-capture", "marking"]
HOME_TAGS = ["Finds the holes", "Mean radius", "CEP 50 and 90", "Every figure with its range", "Load comparison",
                 "Zero correction in clicks", "Dope table", "Hit probability", "Phone capture", "Store-bought targets", "Pooled sets",
                 "One-page report"]
# (label, number, feature key, picture, title, words), the feature key being the Features page entry the card opens.
HOME_CARDS = [
    ("Marking", "01", "every-hole", "marking", "Finds the holes",
     "It reads the sheet, finds each hole and gives it to its bull. You check only the few it is unsure of."),
    ("Analysis", "02", "six-figures", "analysis-open", "Every figure with its range",
     "Mean radius, CEP, sigma and the rest, each with how big it could really be, and why."),
    ("Loads", "03", "compare", "compare", "Compare loads honestly",
     "Two loads side by side, and a plain answer when the data cannot yet tell them apart."),
    ("Ballistics", "04", "ballistics", "ballistics-hit", "Hit probability",
     "The chance of a hit on a plate or a ring at your distance, worked out from your own group."),
    ("Sets", "05", "pool-set", "groups", "Several sheets, one group",
     "The sheets of a set pool into one group, each sheet in its own color."),
    ("Sessions", "06", "one-page-report", "sessions", "Sessions and a one-page report",
     "Every result kept by load, date and time, and one dated page to print or share."),
]
HOME_POINTS = [
    ("A", "Free and open source", "GPL-3.0. Every line of how a figure is worked out can be read, checked and built yourself."),
    ("B", "The range, not just the number", "A figure without its range invites a story. GroupLab shows how big each one could really be."),
    ("C", "On what you already own", "Windows, macOS, Linux and Android, with iPhone and iPad in a public beta."),
]
HOME_DESCRIPTION = "Photograph any target. GroupLab measures the group and gives every figure with the range it could really be."
HOME_SECTIONS = [("measure", "Measure"), ("honest", "Honest"), ("phone", "Phone"), ("start", "Get started")]

HOME_MARK = ('<svg class="tp-logo" width="26" height="26" viewBox="0 0 100 100" aria-hidden="true">'
                 '<circle cx="50" cy="50" r="40" class="tp-ring-line"/><circle cx="50" cy="50" r="21" class="tp-ring-line"/>'
                 '<circle cx="61.94" cy="32.75" r="11" class="tp-hit"/><circle cx="52.75" cy="41.94" r="11" class="tp-hit"/>'
                 '<circle cx="65.31" cy="45.31" r="11" class="tp-hit"/></svg>')
ICON_PAUSE = '<svg class="ico tp-motion-icon" width="16" height="16" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 5v14M16 5v14"/></svg>'


def home_small(name: str, theme: str) -> str:
    """The orbit's copy of a screen, 360 pixels across, made by build_images (entry 440 section 3.1)."""
    return f"/assets/screens/small/{name}-{theme}.webp"


def home_slots(names: list[str], radius: int, w: int, h: int, start: float) -> str:
    """Pictures spaced round a ring of the given radius, as the canvas placed them; each turns back as its ring turns, so it stays upright."""
    out = []
    for i, name in enumerate(names):
        a = start + i * 2 * math.pi / len(names)
        left, top = radius + radius * math.cos(a) - w / 2, radius + radius * math.sin(a) - h / 2
        imgs = "".join(f'<img class="only-{t}" src="{home_small(name, t)}" alt="" width="{w}" height="{h}" loading="lazy" decoding="async">'
                       for t in ("dark", "light"))
        out.append(f'<div class="tp-slot" style="left:{left:.1f}px;top:{top:.1f}px;width:{w}px;height:{h}px">'
                   f'<div class="tp-up"><div class="tp-shot">{imgs}</div></div></div>')
    return "".join(out)


def home_phone(name: str, alt: str, cls: str = "") -> str:
    """A phone screen in its dark frame; a name with a theme is that picture alone, as drawn, and one without is the theme's own."""
    files = [name] if name.endswith(("-dark", "-light")) else phone_themes(name)
    w, h = Image.open(need(PHONE_SCREENS / f"{files[-1]}.png")).size
    return "".join(
        f'<img class="tp-phone-shot {cls}{"" if len(files) == 1 else " only-" + stem.rsplit("-", 1)[1]}" src="/assets/screens/phone/{stem}.webp" '
        f'alt="{esc(alt)}" width="{w // 2}" height="{h // 2}" loading="lazy" decoding="async">' for stem in files)


def home_card_picture(name: str, alt: str) -> str:
    """A card's screen in both themes, from the 560 pixel copies build_images makes; only the theme shown is fetched."""
    return "".join(f'<img class="tp-card-img only-{t}" src="/assets/screens/card/{name}-{t}.webp" alt="{esc(alt)}" width="560" height="360" '
                   f'loading="lazy" decoding="async">' for t in ("dark", "light"))


def page_home() -> str:
    tags = "".join(f'<span><span class="tp-dot">&#183;</span> {esc(t.upper())}</span>' for t in HOME_TAGS)
    cards = "".join(
        f'<a class="tp-card tp-ease" href="{FEATURES_PATH}#{key}"><div class="tp-strip-label mono" data-terms="off"><span>{esc(tag.upper())}</span><span>{n}</span></div>'
        f'<div class="tp-card-pic">{home_card_picture(pic, title)}</div>'
        f'<div class="tp-card-text"><h3>{esc(title)}</h3><p>{esc(words)}</p></div></a>'
        for tag, n, key, pic, title, words in HOME_CARDS)
    points = "".join(f'<div class="tp-point tp-ease"><p class="tp-letter mono">{l}</p><h3>{esc(t)}</h3><p>{esc(words)}</p></div>'
                     for l, t, words in HOME_POINTS)
    index = "".join(f'<a href="#{key}">{esc(label.upper())}</a>' for key, label in HOME_SECTIONS)
    phones = (home_phone("fold-capture", "Capture on the phone", "tp-low") + home_phone("fold-result-dark", "A result on the phone")
              + home_phone("fold-result-light", "A result in the light theme", "tp-low tp-third"))
    main = f"""
<section class="tp-hero" id="top">
<div class="tp-orbits" aria-hidden="true">
<div class="tp-circle tp-core"></div>
<div class="tp-ring tp-inner">{home_slots(HOME_INNER, 330, 210, 135, -0.5)}</div>
<div class="tp-ring tp-outer">{home_slots(HOME_OUTER, 560, 230, 148, 0.2)}</div>
<div class="tp-circle tp-dashed"></div>
</div>
<div class="tp-hero-text">
<h1 class="cond">GROUP<span class="tp-amber">LAB</span></h1>
<p class="tp-tagline">Know what your rifle <em>really</em> shoots.</p>
<p class="tp-lead tp-wide">Photograph any target. GroupLab measures the group and gives every figure with the range it could really be.</p>
<div class="tp-actions tp-wide">
<a class="tp-btn tp-primary mono" href="/download/">DOWNLOAD FREE</a>
<a class="tp-btn mono" href="{GITHUB}">VIEW THE SOURCE</a>
</div>
<p class="tp-platforms mono tp-wide">GPL-3.0 &#183; WINDOWS &#183; MACOS &#183; LINUX &#183; ANDROID &#183; IPHONE AND IPAD BETA</p>
</div>
<div class="tp-hero-phone">
<p class="tp-lead">Photograph any target. Every figure comes with the range it could really be.</p>
<a class="tp-btn tp-primary mono" href="/download/?device=android#android">GET IT FOR ANDROID</a>
<a class="tp-btn mono" href="/download/?device=iphone#iphone">IPHONE AND IPAD BETA</a>
</div>
</section>
<nav class="tp-index mono" aria-label="Sections of this page">{index}</nav>
<div class="tp-band"><div class="tp-ticker mono"><div class="tp-tags" data-terms="off">{tags}</div><div class="tp-tags tp-again" aria-hidden="true" data-terms="off">{tags}</div></div></div>

<section class="tp-section" id="measure">
<div class="tp-strip-label mono tp-head" data-terms="off"><span>01 / MEASURE</span><span class="tp-wide">REAL SCREENS, RENDERED FROM THE CURRENT BUILD</span></div>
<div class="tp-intro tp-ease">
<h2>Five shots are a hint. <em>GroupLab</em> tells you how big a hint.</h2>
<p>A small group is mostly luck. Every figure here comes with the range it could really be, so you know when to shoot more and when to stop.</p>
</div>
<div class="tp-cards">{cards}</div>
</section>

<section class="tp-section" id="honest">
<div class="tp-strip-label mono tp-head" data-terms="off"><span>02 / HONEST</span><span class="tp-wide">FREE AND OPEN SOURCE</span></div>
<div class="tp-intro tp-ease"><h2>No bragging numbers.</h2></div>
<div class="tp-points">{points}</div>
<p class="tp-today">Today GroupLab is test builds, unsigned and rebuilt after every change that passes its tests, so one may be broken.
Not built yet: hole detection on plain paper, and synchronization between devices. <a href="/download/">What to expect</a>, and
<a href="{GITHUB}#planned">the full status on GitHub</a>.</p>
</section>

<section class="tp-section" id="phone">
<div class="tp-strip-label mono tp-head" data-terms="off"><span>03 / PHONE</span><span class="tp-wide">ANDROID, IPHONE AND IPAD</span></div>
<div class="tp-phone-row">
<div class="tp-phone-words tp-ease"><h2>Measure it at the bench.</h2><p>Point the phone at the sheet, flat on the table or straight on at the backer, and read the group before you walk back.</p></div>
<div class="tp-phones">{phones}</div>
</div>
</section>

<section class="tp-section tp-close" id="start">
<h2 class="cond tp-ease">START MEASURING.</h2>
<div class="tp-actions">
<a class="tp-btn tp-primary mono" href="/download/"><span class="tp-wide">DOWNLOAD FOR DESKTOP</span><span class="tp-narrow">DOWNLOAD FREE</span></a>
<a class="tp-btn mono tp-wide" href="/download/#android">GET IT ON YOUR PHONE</a>
</div>
<p class="mono tp-free">Free &#183; open source</p>
</section>
"""
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>GroupLab</title>
<meta name="description" content="{esc(HOME_DESCRIPTION)}">
<link rel="canonical" href="{SITE_URL}/">
<meta name="grouplab-site-build" content="{COMMIT}">
<meta property="og:type" content="website">
<meta property="og:site_name" content="GroupLab">
<meta property="og:title" content="GroupLab">
<meta property="og:description" content="{esc(HOME_DESCRIPTION)}">
<meta property="og:url" content="{SITE_URL}/">
<meta property="og:image" content="{SITE_URL}/assets/img/og.png">
<meta name="twitter:card" content="summary_large_image">
<meta name="color-scheme" content="dark light">
<meta name="theme-color" content="#131417" media="(prefers-color-scheme: dark)">
<meta name="theme-color" content="#f4f3f0" media="(prefers-color-scheme: light)">
<link rel="icon" href="/favicon.ico" sizes="any">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
<link rel="preload" href="/assets/fonts/plex-sans-condensed-700.woff2" as="font" type="font/woff2" crossorigin>
<link rel="stylesheet" href="/assets/css/site.css">
<link rel="stylesheet" href="/assets/css/home.css">
<script src="/assets/js/theme.js"></script>
<script src="/assets/js/terms.js" defer></script>
<script src="/assets/js/home.js" defer></script>
</head>
<body class="tp tp-landing">
<a class="skip" href="#main">Skip to content</a>
{RULER}
{site_header(home=True)}
<main id="main">{explain(main)}</main>
{site_footer()}
</body>
</html>
"""


def header_problems(out: Path = OUT) -> list[str]:
    """Entry 443 section 2: every page carries the one header and the one footer, the header with every link entry 441 had dropped and
    Alan asked back (the main pages, Community, Release notes, Support and GitHub), so the links can never drift apart again."""
    wanted = [href for _, href in nav()] + [GITHUB]
    found = []
    for page in sorted(out.rglob("*.html")):
        text = page.read_text(encoding="utf-8", errors="replace")
        where = page.relative_to(out).as_posix()
        if '<header class="tp-header' not in text or '<footer class="tp-footer">' not in text:
            found.append(f"{where}: it does not carry the shared header and footer")
            continue
        # Entry 451: every page carries the home page's ruler.
        if RULER not in text:
            found.append(f"{where}: it does not carry the home page's ruler down the right edge")
        header = text[text.index('<header class="tp-header'):text.index("</header>")]
        missing = [href for href in wanted if f'href="{href}"' not in header]
        if missing:
            found.append(f"{where}: its header has no link to {', '.join(missing)}")
    return found


DISPLAY_FACE = "IBM Plex Sans Condensed"


def css_rules(css: str) -> list[tuple[str, str]]:
    """Every rule in a stylesheet as (selector list, declarations), those inside @media and @supports included, in order."""
    css = re.sub(r"/\*.*?\*/", "", css, flags=re.S)
    return [(m.group(1).strip(), m.group(2)) for m in re.finditer(r"([^{}@]+)\{([^{}]*)\}", css)]


def heading_problems(out: Path = OUT) -> list[str]:
    """Entry 444 section 3: every page's h1 is the home page's display face, IBM Plex Sans Condensed bold, and entry 449: as written, never
    turned to capitals by a stylesheet, outside the home page. Read from the stylesheets each
    page links, at every width: the page's body is .tp, which gives its h1 the display face, and no rule that can reach an h1, through the
    element or a class it carries, gives it another face or weight. An h1 with its own style attribute fails too."""
    found, sheets = [], {}
    for page in sorted(out.rglob("*.html")):
        text = page.read_text(encoding="utf-8", errors="replace")
        where = page.relative_to(out).as_posix()
        h1s = re.findall(r"<h1([^>]*)>", text)
        if not h1s:
            continue
        rules = []
        for href in re.findall(r'<link rel="stylesheet" href="(/assets/css/[^"?]+)', text):
            if href not in sheets:
                f = out / href.lstrip("/")
                sheets[href] = css_rules(f.read_text(encoding="utf-8")) if f.is_file() else []
            rules += sheets[href]
        body = re.search(r'<body class="([^"]*)"', text)
        if not body or "tp" not in body.group(1).split():
            found.append(f"{where}: its body is not .tp, so its h1 does not take the display face")
        if not any(sel == ".tp h1,.tp h2" and DISPLAY_FACE in decl and "font-weight:700" in decl for sel, decl in rules):
            found.append(f"{where}: no stylesheet it links gives .tp h1 the display face, {DISPLAY_FACE} bold")
        for attrs in h1s:
            if "style=" in attrs:
                found.append(f"{where}: its h1 carries its own style")
            classes = (re.search(r'class="([^"]*)"', attrs) or re.search("()", "")).group(1).split()
            last = re.compile(r"(?:^|[\s>+~])(?:h1|[\w-]*(?:" + "|".join(r"\." + re.escape(c) for c in classes or ["\0"]) + r"))(?![\w-])[^\s>+~]*$")
            for sel, decl in rules:
                hits = [one.strip() for one in sel.split(",") if last.search(one.strip())]
                face = re.search(r"font-family:\s*([^;]+)", decl)
                weight = re.search(r"font-weight:\s*([^;]+)", decl)
                if hits and (re.search(r"(?:^|;)\s*font:", decl) or (face and not face.group(1).strip().strip('"').startswith(DISPLAY_FACE))
                             or (weight and weight.group(1).strip() != "700")):
                    found.append(f"{where}: {hits[0]} gives its h1 a face or weight other than {DISPLAY_FACE} bold")
                # Entry 449, Alan's answer A: every page's title as written; only the home page's own words are in capitals.
                if hits and where != "index.html" and re.search(r"text-transform:\s*uppercase", decl):
                    found.append(f"{where}: {hits[0]} sets its h1 in capitals, and page titles are as written (entry 449)")
    return sorted(set(found))


def page_testpage_moved() -> str:
    """Entry 441 section 3: the address entry 440 used for the preview, now a page with one link to the home page (no meta refresh, which
    entry 151 bans), so nobody holding the old link meets a second copy of the home page."""
    body = f"""
<section class="wrap page-head stack last">
<p class="eyebrow">GroupLab</p>
<h1>This page is the home page now.</h1>
<p class="lead">The design previewed here became grouplab.org's home page on 18 October 2026.</p>
<div class="actions">{btn("Go to the home page", "/", True)}</div>
</section>
"""
    return shell(TESTPAGE_PATH, "Moved", "The design previewed here is grouplab.org's home page now.", body)


def home_problems(out: Path = OUT) -> list[str]:
    """Entry 441 section 3, entry 440's check turned around: the home page is indexed and in the sitemap, the preview's address answers
    with a link to it rather than a copy, and nothing in the navigation still names the preview. A whole-site check."""
    found = []
    home = out / "index.html"
    if not home.exists():
        return ["index.html: the home page was not built"]
    text = home.read_text(encoding="utf-8")
    if re.search(r'<meta name="robots"[^>]*noindex', text):
        found.append("index.html: it asks not to be indexed")
    if "tp-hero" not in text:
        found.append("index.html: it is not the concept B home page")
    sitemap = out / "sitemap.xml"
    listed = sitemap.read_text(encoding="utf-8") if sitemap.exists() else ""
    if f"<loc>{SITE_URL}/</loc>" not in listed:
        found.append("sitemap.xml: it does not list the home page")
    if TESTPAGE_PATH in listed:
        found.append("sitemap.xml: it lists the preview's old address")
    old = out / "testpage" / "index.html"
    if old.exists():
        moved = old.read_text(encoding="utf-8")
        if "tp-hero" in moved or 'href="/"' not in moved:
            found.append("testpage/index.html: it is a second copy of the home page, not a link to it")
    if any(href == TESTPAGE_PATH for _, href in NAV + nav()):
        found.append("website/build.py: the navigation names the preview's old address")
    return found


def releases() -> list[tuple[str, str, str]]:
    """Every build in docs/RELEASE-NOTES.md, newest first: its anchor, its version and its notes as HTML.

    NOTES-FROM-PLANNING.md entry 136 section 2.1. The file is the source of truth and the page is built from it, so the
    two cannot disagree and the build never fetches anything from the network to make this page.
    """
    src = need(REPO / "docs" / "RELEASE-NOTES.md").read_text(encoding="utf-8")
    out: list[tuple[str, str, str]] = []
    version = None
    body: list[str] = []
    for line in src.splitlines():
        if line.startswith("## ") and not line.startswith("### "):
            if version is not None:
                out.append((slug(version), version, markdown.markdown(chr(10).join(body).strip(), extensions=["tables", "sane_lists"])))
            version = line[3:].strip()
            body = []
        elif version is not None and line.strip() != "---":
            body.append(line)
    if version is not None:
        out.append((slug(version), version, markdown.markdown(chr(10).join(body).strip(), extensions=["tables", "sane_lists"])))
    return out


def release_notes_are_current() -> list[str]:
    """Every published build has an entry, NOTES-FROM-PLANNING.md entry 136 section 2.4.

    The page is built from docs/RELEASE-NOTES.md, so a build published since that file was last
    written would put up a history already missing its newest entries, and nobody would notice:
    a page short of its newest build looks exactly like a page nobody has updated.

    The tags in the checkout are the evidence, never the network. A checkout with no tags at all
    is a shallow one and knows nothing either way, so it says so rather than guessing; the
    publishing workflow fetches them for this reason.
    """
    try:
        tags = subprocess.run(
            ["git", "tag", "--list", "v*"], cwd=REPO, capture_output=True, text=True, timeout=60, check=True
        ).stdout.split()
    except (OSError, subprocess.SubprocessError):
        return []

    published = [t[1:] for t in tags if t.startswith("v") and not t.endswith("-draft")]
    if not published:
        print("release notes: this checkout carries no tags, so whether the page is current cannot be told here.")
        return []

    written = {v.lower() for v, in [(v,) for _, v, _ in releases()]}
    missing = sorted(v for v in published if v.lower() not in written)
    if missing:
        return [
            "docs/RELEASE-NOTES.md has fallen behind: "
            + ", ".join(missing)
            + ". Add each from its Release-note trailers before publishing."
        ]
    return []


def front_matter(text: str, where: str) -> dict:
    """The `key: value` and `key:` then `- item` front matter of an article.

    Written out rather than handed to PyYAML because the build runs on a CI runner that has markdown,
    Pillow and fontTools and nothing else, and a site that cannot build on a fresh checkout is a site
    nobody else can build. The shape an article uses is small enough to read in twenty lines.
    """
    meta: dict = {}
    key = None
    for number, line in enumerate(text.splitlines(), start=2):
        if not line.strip() or line.lstrip().startswith("#"):
            continue

        if line.startswith((" ", "	")) and line.lstrip().startswith("- "):
            if key is None:
                raise SystemExit(f"{where} line {number}: a list item before any field")
            meta.setdefault(key, []).append(unquote(line.lstrip()[2:].strip()))
            continue

        if ":" not in line:
            raise SystemExit(f"{where} line {number}: {line.strip()!r} is neither a field nor a list item")

        key, _, value = line.partition(":")
        key = key.strip()
        value = value.strip()
        meta[key] = unquote(value) if value else []

    return meta


def unquote(value: str) -> str:
    """A front matter value, with one layer of quotes taken off and doubled quotes inside it undone."""
    for quote in ('"', "'"):
        if len(value) >= 2 and value.startswith(quote) and value.endswith(quote):
            return value[1:-1].replace(quote * 2, quote)
    return value


# Entry 143 section 1.3: three states, not two, because "published" was being used for two different things.
# An article that is finished and reviewed is `ready`; it becomes `published` on the day a batch goes live, and
# PUBLISHED.md is the record of that having happened.
STATES = {"draft", "ready", "published"}

PUBLISHED = REPO / "website" / "research" / "PUBLISHED.md"

DRAFT_NOTICE = ('<p class="small warn">This article is a draft. It is not finished, it is not linked from the '
                'research index, and anything in it may change.</p>')

READY_NOTICE = ('<p class="small warn">This article is finished and waiting to be published with its batch. It is '
                'not linked from the research index yet.</p>')


def published_articles() -> set:
    """The slugs that have actually gone live, from the record beside the articles.

    Entry 143 section 1.3. The record is a deliberate act with a date on it, which is the difference between an
    article saying it is published and an article having been published. The build holds the two together in both
    directions, so neither can drift.
    """
    if not PUBLISHED.exists():
        return set()
    out = set()
    for line in PUBLISHED.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line.startswith("- ") and " " in line[2:]:
            out.add(line[2:].split()[0])
    return out


def research_articles() -> list[dict]:
    """Every article, front matter parsed, newest number last. Entry 142 section 2.1.

    A draft is built like any other so it can be looked at locally, and left out of the index and the
    sitemap, so nothing half written is ever linked from the site.
    """
    out = []
    folder = REPO / "website" / "research"
    for path in sorted(folder.glob("*.md")):
        # The record of what has gone live lives beside the articles and is not one. Entry 143 section 1.3.
        if path.name == PUBLISHED.name:
            continue
        text = path.read_text(encoding="utf-8")
        if not text.startswith("---"):
            raise SystemExit(f"{path.name}: no front matter")
        _, front, body = text.split("---", 2)
        meta = front_matter(front, path.name)
        meta["slug"] = path.stem
        meta["body"] = body.strip()
        out.append(meta)
    return sorted(out, key=lambda m: m.get("number", 0))


def research_figure(meta: dict) -> str | None:
    """The article's lead figure, or None. It is a file beside the article, built by its own script."""
    folder = REPO / "website" / "research" / meta["slug"] / "figures"
    for ext in (".svg", ".png"):
        if (folder / f"lead{ext}").exists():
            return f"/research/{meta['slug']}/figures/lead{ext}"
    return None


def research_lead(meta: dict) -> str:
    """The picture at the top of an index card, and never a gap.

    Entry 143 section 1.1: "Every card on the index needs a lead image, or none of them do." One card in four had a
    thumbnail, so the row stood as tall as its tallest card with large blanks in the rest of it.

    **On the live site it was worse than that.** Every article with a chart was still a draft, so seventeen of the
    eighteen published articles had no lead at all. The entry's own fallback is what fits: where an article has no
    natural chart, a plain titled panel in the site's own style is better than a gap. The panel is built from the
    article's own group and number, in the site's colors, at the same shape as a thumbnail, so it follows the
    reader's theme for nothing and there is no image to go stale.
    """
    figure = research_figure(meta)
    if figure:
        return themed_image(figure, "", "research-thumb", width=320, height=200)

    return (
        f'<div class="research-thumb research-plate" aria-hidden="true">'
        f'<span class="mono small caps">{esc(str(meta.get("group", "")))}</span>'
        f'<span class="plate-rule"></span>'
        f'<span class="mono plate-number">{esc(str(meta.get("number", "")))}</span>'
        "</div>"
    )


def themed_image(src: str, alt: str, cls: str, width: int = 0, height: int = 0) -> str:
    """One image, or a light and a dark one where the figure script wrote both.

    Entry 143 section 1.2: every figure was drawn on a near-white surface, which glared against the dark theme and
    looked pasted on. ``_style.py`` now writes ``<name>-dark.png`` beside every figure it saves, and this is the
    half that puts it on the page. A figure with no dark file beside it is shown as it always was, so an older
    figure that has not been regenerated still appears rather than vanishing.
    """
    size = f' width="{width}" height="{height}"' if width else ""
    dot = src.rfind(".")
    dark_src = src[:dot] + "-dark" + src[dot:]
    if not (REPO / "website" / dark_src.lstrip("/")).exists():
        return f'<img class="{cls}" src="{src}" alt="{esc(alt)}"{size} loading="lazy" decoding="async">'

    return (
        f'<img class="{cls} only-dark" src="{dark_src}" alt="{esc(alt)}"{size} loading="lazy" decoding="async">'
        f'<img class="{cls} only-light" src="{src}" alt="{esc(alt)}"{size} loading="lazy" decoding="async">'
    )


def research_swap(html_text: str) -> str:
    """Every figure in an article body, given its dark half where one exists."""
    def one(m):
        return themed_image(m.group(2), html.unescape(m.group(1)), "research-fig")

    return re.sub(r'<img alt="([^"]*)" src="(/research/[^"]+)"\s*/?>', one, html_text)


def build_research_figures() -> list[str]:
    """Runs every article's figure scripts, before the pages that show their output are written.

    Entry 142 section 2.2: a chart is built by a script from data in the repository, so the chart and the
    number in the text cannot drift apart. Running them on every build is what makes that true rather than
    a thing somebody remembered to do once.
    """
    problems = []
    for script in sorted((REPO / "website" / "research").glob("*/figures/*.py")):
        # The research folder is on the path so a shared style module beside the articles is importable from any of them,
        # which is how the planning session's figure scripts are written.
        env = dict(os.environ, PYTHONPATH=str(REPO / "website" / "research"))
        done = subprocess.run([sys.executable, "-B", script.name], cwd=script.parent, capture_output=True, text=True, env=env)
        if done.returncode == 0:
            continue

        tail = done.stderr.strip().splitlines()[-1] if done.stderr.strip() else "no output"

        # A script that needs a package this machine does not have is not a broken script, and this machine has no
        # package source to install one from. It says so and moves on; anything else fails the build. The figure it
        # would have drawn is committed beside it, so the page is complete either way.
        if "ModuleNotFoundError" in tail:
            print(f"research/{script.parent.parent.name}: {script.name} needs a package that is not here ({tail}); its committed figure is used")
            continue

        problems.append(f"research/{script.parent.parent.name}: {script.name} failed: {tail}")
    return problems


# Entry 143 section 1.2: figures a script drew as artwork of white paper. These are pictures of a scanned sheet, so
# white is the subject and not the theme, and they are the same in both. Everything else must have a dark version.
PAPER = {"scanner-traps/settings.png", "can-you-see-the-bull/test-card.png",
         "photo-hole-size/hole-smallest.png", "photo-hole-size/hole-typical.png",
         "photo-hole-size/hole-largest.png", "photo-hole-size/hole-shadow.png"}

# The surface _style.py draws a dark figure on. Held here as well so a figure saved by something that did not use the
# shared style, or a light figure copied to a dark name, is caught rather than published.
DARK_SURFACE = (0x19, 0x1a, 0x1e)


def figure_theme_problems() -> list[str]:
    """Every figure has a dark version, and every dark version is actually dark.

    Entry 143 section 1.2. The first run of this produced dark figures with black titles on them, because the style was
    recolouring the centre title artist and every title in these articles is a left one. Nothing in the build noticed:
    the file was written, the page showed it, and it was only visible to somebody looking at the picture. So the build
    looks at the picture.
    """
    problems = []
    root = REPO / "website" / "research"
    for light in sorted(root.glob("*/figures/*.png")):
        name = f"{light.parent.parent.name}/{light.name}"
        if "-dark" in light.stem or light.stem == "lead" or name in PAPER:
            continue

        dark = light.with_name(light.stem + "-dark.png")
        if not dark.exists():
            problems.append(f"research/{name}: has no dark version. Draw it with _style.save, which writes both")
            continue

        corner = Image.open(dark).convert("RGB").getpixel((1, 1))
        if corner != DARK_SURFACE:
            problems.append(
                f"research/{light.parent.parent.name}/{dark.name}: its corner is {corner}, not the dark surface "
                f"{DARK_SURFACE}, so it is a light figure under a dark name")

    return problems


def research_problems() -> list[str]:
    """Everything wrong with the research articles, entry 142 sections 2.5 and 2.6.

    Front matter complete, every data file it offers actually there, every figure script still runs and
    still writes the figure the page shows, and no published image carrying metadata. A reader who is
    given a download link that 404s has been told the data is available when it is not.
    """
    required = ["title", "description", "group", "number", "written", "data_date", "samples", "state", "found", "sure"]
    problems: list[str] = []
    live = published_articles()
    for meta in research_articles():
        where = f"research/{meta['slug']}.md"
        for key in required:
            if not str(meta.get(key, "")).strip():
                problems.append(f"{where}: front matter has no {key}")
        if meta.get("group") not in RESEARCH_GROUPS:
            problems.append(f"{where}: group {meta.get('group')!r} is not one of {RESEARCH_GROUPS}")
        if meta.get("state") not in STATES:
            problems.append(f"{where}: state is {meta.get('state')!r}, and it must be one of {sorted(STATES)}")
        if meta.get("status"):
            problems.append(f"{where}: still has a status field. Entry 143 section 1.3 replaced it with state")

        # Entry 143 section 1.3, and the whole point of the three states. "published" is not a thing an article can
        # say about itself: it is a thing the record says happened, on a date, to a batch. Setting it here and
        # nowhere else would put a page on the index that nobody agreed to publish.
        if meta.get("state") == "published" and meta["slug"] not in live:
            problems.append(
                f"{where}: state says published and {PUBLISHED.name} does not list it. Publishing is a decision, so it "
                "is recorded there when a batch actually goes live, and only then does this field say published")
        if meta.get("state") != "published" and meta["slug"] in live:
            problems.append(
                f"{where}: {PUBLISHED.name} says this went live and the state says {meta.get('state')!r}. A page that "
                "is on the site and does not admit it will be taken down by the next build without anybody deciding to")

        # Entry 153 sections 2 and 3. It went on in the batches section 6 asks for, gated on a list that grew from
        # three articles to all thirty; the list is gone now that every article is on it, and a new article meets the
        # standard from its first commit rather than joining a backlog.
        note = problems.append

        means = re.search(r"^##\s+What this means\s*$(.*?)(?=^##\s|\Z)", meta["body"], re.MULTILINE | re.DOTALL)
        if means is None:
            note(f'{where}: no "What this means" section. Entry 153 section 2: say what a reader should do differently, '
                 "or stop believing, because of what this measured")
        else:
            said = means.group(1).strip()
            if len(said) < 200:
                note(f'{where}: "What this means" is {len(said)} characters. It is the section a reader came for, and a '
                     "sentence is not an answer")
            elif said[:120].strip() and said[:120].strip() in str(meta.get("found", "")):
                note(f'{where}: "What this means" repeats the front matter\'s found line. It is for what to do about the '
                     "result, not for the result again")

        # Entry 153 section 3.5: an article about something visible carries a picture of it, or says in one line why it
        # cannot. The exemption is a written sentence rather than a silent pass, so somebody had to decide.
        figures = re.findall(r"!\[([^\]]*)\]\(([^)\s]+)", meta["body"])
        if not figures and not str(meta.get("no_figure", "")).strip():
            note(f"{where}: no figure, and no no_figure line in the front matter saying why. Entry 153 section 3")
        for alt, src in figures:
            if not alt.strip():
                note(f"{where}: the figure {src} has no caption. Entry 153 section 3.3: say what to look at, not what "
                     "the figure is")

        folder = REPO / "website" / "research" / meta["slug"]
        for data in meta.get("data") or []:
            if not (folder / data).exists():
                problems.append(f"{where}: offers {data} for download and it is not there")

        for link in re.findall(r"\]\((/research/[^)\s]+)\)", meta["body"]):
            if not (OUT / link.lstrip("/")).exists() and not (REPO / "website" / link.lstrip("/")).exists():
                problems.append(f"{where}: links to {link}, which is not built")

    # Entry 142 section 2.5: nothing published carries metadata. A JPEG or PNG from a camera carries the
    # time and often the place it was taken, and an article image is the easiest way for that to escape.
    for image in list(OUT.rglob("*.jpg")) + list(OUT.rglob("*.jpeg")) + list(OUT.rglob("*.png")):
        raw = image.read_bytes()
        for marker, what in ((b"Exif", "EXIF"), (b"http://ns.adobe.com/xap/", "XMP"), (b"Photoshop 3.0", "IPTC"), (b"GPS", "GPS")):
            if marker in raw[:65536]:
                problems.append(f"{image.relative_to(OUT)}: carries {what} metadata")

    return problems


def page_research_index() -> str:
    published = [m for m in research_articles() if m.get("state") == "published"]
    groups = []
    for group in RESEARCH_GROUPS:
        cards = []
        for meta in [m for m in published if m.get("group") == group]:
            thumb = research_lead(meta)
            cards.append(
                f'<a class="panel pad stack tight research-card plain" href="/research/{meta["slug"]}/">'
                f"{thumb}"
                f'<h3 class="h3">{esc(meta["title"])}</h3>'
                f'<p class="small">{esc(meta["description"])}</p>'
                "</a>"
            )
        if cards:
            groups.append(f'<h2 class="h2">{esc(group)}</h2><div class="research-grid">{"".join(cards)}</div>')

    body = f"""
<section class="wrap page-head">
<p class="eyebrow">Research</p>
<h1>Research</h1>
<p class="lead">What we have measured, on real targets, with the numbers and the data behind every claim.</p>
<p class="small faint">Every figure says how many shots it rests on. Nothing here is called proven that the shots cannot prove, and where we do not know, it says so.</p>
</section>
<section class="wrap stack last">
{"".join(groups) if groups else '<p>The first articles are being written.</p>'}
</section>
"""
    return shell("/research/", "Research", "What GroupLab has measured on real targets, with the data behind it.", body, "Research")


# NOTES-FROM-PLANNING.md entry 153 section 1.2. Alan: "For all of the research documents, I would prefer if my
# name is not mentioned. Just say the author or developer." One form, used everywhere, rather than a variant invented
# per page: a byline that varies reads as carelessness about attribution, which is the opposite of what it is for.
BYLINE = "GroupLab project. Researched and written with Claude. Testing and data collection by the developer."


def page_research_article(meta: dict) -> str:
    content = research_swap(markdown.markdown(meta["body"], extensions=["tables", "sane_lists", "fenced_code"]))
    sources = "".join(f"<li>{markdown.markdown(str(x), extensions=[])[3:-4]}</li>" for x in (meta.get("sources") or []))
    data = "".join(
        f'<li><a href="/research/{meta["slug"]}/{d}">{esc(d.rsplit("/", 1)[-1])}</a></li>' for d in (meta.get("data") or [])
    )
    # Entry 142 section 2.4: every article opens with what we found, how sure we are, and where the data is.
    box = f"""<div class="panel pad stack tight research-box">
<p class="mono faint small caps">What we found</p>
<p>{esc(meta["found"])}</p>
<p class="mono faint small caps">How sure we are</p>
<p class="small">{esc(meta["sure"])}</p>
{f'<p class="mono faint small caps">The data</p><ul class="small">{data}</ul>' if data else ""}
</div>"""
    draft = {"draft": DRAFT_NOTICE, "ready": READY_NOTICE}.get(str(meta.get("state")), "")
    # Entry 153 section 3.4: where an article genuinely has nothing to show, it says so in one line rather than
    # leaving a gap where a reader expects a picture and assumes somebody forgot.
    no_figure = (f'<p class="small faint">No figure: {esc(str(meta["no_figure"]))}</p>'
                 if str(meta.get("no_figure", "")).strip() else "")
    body = f"""
<section class="wrap guide">
<article class="prose research-article">
<p class="eyebrow"><a class="plain" href="/research/">Research</a> &rsaquo; {esc(meta.get("group", ""))}</p>
<h1>{esc(meta["title"])}</h1>
<p class="small faint">{BYLINE} Written {esc(str(meta["written"]))}; the data is from {esc(str(meta["data_date"]))}.</p>
{draft}
{no_figure}
{box}
{content}
{f'<h2>Sources</h2><ol class="small">{sources}</ol>' if sources else ""}
</article>
</section>
"""
    return shell(f"/research/{meta['slug']}/", meta["title"], meta["description"], body, "Research")


NEWEST_RELEASES = 10
HEAD4 = '<h4 class="h3">'


def page_releases() -> str:
    blocks = []
    for i, (anchor, version, notes) in enumerate(releases()):
        # The newest is open, the rest closed, so the page opens on what somebody almost always came for. Entry 445: the newest ten are
        # headings the contents list names, the older ones are under one entry, and a build's own headings sit a level below its name.
        open_attr = " open" if i == 0 else ""
        tag = "h3" if i < NEWEST_RELEASES else "span"
        blocks.append(
            f'<details class="panel pad release" id="{anchor}"{open_attr}>'
            f'<summary><{tag} class="h3">{esc(version)}</{tag}></summary>'
            f'<div class="prose tight">{notes.replace("<h3>", HEAD4).replace("</h3>", "</h4>")}</div>'
            "</details>"
        )

    body = f"""
<section class="wrap page-head">
<p class="eyebrow">Release notes</p>
<h1>Release notes</h1>
<p class="lead">Every build of GroupLab anyone could download, newest first. GroupLab is unreleased, so every one of these is a pre-release.</p>
<p class="small faint">Nightly builds are produced on the nights the application changed, so a gap in the numbers means nothing shipped that night. Work on the website, the documentation or these notes does not make a new build. Numbers below 92 were the run that built them rather than the build itself, and a run that was canceled or skipped still took its number, which is why the older gaps are wider.</p>
</section>
<section class="wrap last stack">
<h2 id="newest">The newest builds</h2>
<div class="stack tight releases">
{"".join(blocks[:NEWEST_RELEASES])}
</div>
<h2 id="older">Older builds</h2>
<div class="stack tight releases">
{"".join(blocks[NEWEST_RELEASES:])}
</div>
</section>
"""
    return shell("/releases/", "Release notes", "What changed in each build of GroupLab, newest first.", body, "Release notes")


def page_guides_index() -> str:
    cards = []
    for key, md, pdf, label, desc in GUIDES:
        cards.append(f"""<div class="panel pad stack tight">
<h2 class="h3"><a class="plain" href="/guides/{key}/">{label}</a></h2>
<p>{desc}</p>
<div class="actions">{btn("Read it", f"/guides/{key}/", True)}{btn("PDF", f"/guides/{pdf}")}</div>
</div>""")
    body = f"""
<section class="wrap page-head">
<p class="eyebrow">Guides</p>
<h1>Guides</h1>
<p class="lead">The user guide and Trying GroupLab describe the Windows application as it is built today, and every picture in them is a render of the build; the user guide's last section is the phone. Statistics for shooters explains, from the beginning, what the figures GroupLab gives you mean. All three are published from the repository, so they change when it does.</p>
</section>
<section class="wrap grid-2 last">
{''.join(cards)}
</section>
"""
    return shell("/guides/", "Guides", "The GroupLab user guide, the guide to trying the test build for the first time, and statistics for shooters from the beginning.", body, "Guides")


# ---------------------------------------------------------------- the tour, entry 146


def tour() -> dict:
    """The screen list the tour is built from, and the order it reads in.

    NOTES-FROM-PLANNING.md entry 146 section 4.1: the same list drives the tour and the weekly screenshot job, so a
    screen that gains or loses a render fails this build rather than going stale quietly. The job renders every screen
    in both themes at 1400 by 900, and those files are the evidence; ``tour_problems`` holds the two lists to each
    other in both directions.
    """
    text = need(REPO / "website" / "tour.json").read_text(encoding="utf-8")
    # Entry 159 section 5.2: a count in the tour is a token, filled from the repository, never typed.
    text = re.sub(r"\{count:([a-z-]+)\}", lambda m: count_words(m.group(1)), text)
    # Entry 242 section 1: the optic stop's numbers are the generator's own, written by the render walk beside its screenshots.
    text = re.sub(r"\{optic:([a-zA-Z0-9.]+)\}", lambda m: optic_number(m.group(1)), text)
    return json.loads(text)


def optic_number(path: str) -> str:
    value = json.loads(need(SCREENS / "optic-numbers.json").read_text(encoding="utf-8"))
    for part in path.split("."):
        value = value[part]
    return f"{value:g}" if isinstance(value, float) else str(value)


def rendered_screens() -> set:
    """Every screen the render walk actually produced, by the name the tour uses, less the second pictures a stop shows (entry 242)."""
    extra = {shot[0] for item in json.loads(need(REPO / "website" / "tour.json").read_text(encoding="utf-8"))["screens"].values()
             for shot in item.get("moreShots", [])}
    return {p.name[: -len("-light-1400x900.png")] for p in SCREENS.glob("*-light-1400x900.png")} - extra


def raw_table_problems() -> list:
    """Entry 275 section 4: a Markdown table that reached a page as text, its pipes and its |---| row showing."""
    found = []
    for page in sorted(OUT.rglob("*.html")):
        text = re.sub(r"<(pre|code)[^>]*>.*?</\1>", "", page.read_text(encoding="utf-8"), flags=re.DOTALL)
        if re.search(r"(?:^|>)\s*\|\s*-{3}|(?:^|>)\s*\|\s\|", text, flags=re.MULTILINE):
            found.append(f"{page.relative_to(OUT).as_posix()}: a Markdown table is printed as text; convert it with the tables extension")
    return found


def tour_problems() -> list:
    """Entry 146 section 4.2, as a build failure rather than a test, because the page is the thing that goes wrong."""
    found = []
    data = tour()
    # A stop only the phone has (entry 249) has no desktop render to hold it to.
    listed = {k for k in data["order"] if data["screens"].get(k, {}).get("platform") != "mobile"}
    have = rendered_screens()

    for key in sorted(listed - have):
        found.append(f"website/tour.json: the tour has a page for {key!r} and no screenshot of it was rendered")
    for key in sorted(have - listed):
        found.append(f"website/tour.json: {key!r} is rendered and has no tour page, so the tour is missing a screen")

    for key in data["order"]:
        screen_data = data["screens"].get(key)
        if screen_data is None:
            found.append(f"website/tour.json: {key!r} is in the order and has no entry")
            continue
        # Entry 152 section 4: every tour page says what that screen does with a target GroupLab did not print.
        for field in ["name", "blurb", "purpose", "fits", "withoutASheet"]:
            if not str(screen_data.get(field, "")).strip():
                found.append(f"website/tour.json: {key} has no {field}")
        if len(screen_data.get("parts") or []) < 3:
            found.append(f"website/tour.json: {key} names fewer than three parts, so a reader cannot follow the picture")
        if not (2 <= len(screen_data.get("steps") or []) <= 5):
            found.append(f"website/tour.json: {key} needs two to five steps, entry 146 section 2.4")
        # Entry 249: every stop has a Mobile side, a real phone screenshot with its own parts and touch steps, or words saying it is not there.
        mobile = screen_data.get("mobile") or {}
        # Entry 275: a stop the phone has but has not been photographed on says so in words, until the next device sitting.
        if mobile.get("pending") and not mobile.get("words"):
            found.append(f"website/tour.json: {key}'s Mobile side is on the phone with its picture to come, and does not say what the phone does")
        if not mobile.get("shot") and mobile.get("only") != "desktop" and not mobile.get("pending"):
            found.append(f"website/tour.json: {key} has no Mobile side: a phone screenshot, or \"only\": \"desktop\" with its words")
        if mobile.get("shot") and not phone_themes(mobile["shot"]):
            found.append(f"website/tour.json: {key}'s phone screenshot {mobile['shot']!r} is not in docs/figures/screens/phone")
        if mobile.get("shot") and screen_data.get("platform") != "mobile":
            if len(mobile.get("parts") or []) < 3 or not (2 <= len(mobile.get("steps") or []) <= 5):
                found.append(f"website/tour.json: {key}'s Mobile side needs three parts and two to five steps of its own")
        if mobile.get("only") == "desktop" and not mobile.get("words"):
            found.append(f"website/tour.json: {key}'s Mobile side does not say it is on the desktop only")
        if screen_data.get("platform") == "mobile" and not screen_data.get("desktopWords"):
            found.append(f"website/tour.json: {key} is on the phone only and its Desktop side does not say so")

    return found


def tour_shot(key: str, alt: str, eager: bool = False) -> str:
    """The screenshot for one tour page, linked to the full size image for a reader who wants to look closely."""
    return (
        f'<a class="plain tour-shot" href="/assets/screens/{key}-dark-1400x900.webp">{screen(key, alt, eager, cls="shot tour-img")}</a>'
    )


def phone_nightly() -> int:
    """The nightly the phone pictures were taken on, from docs/figures/screens/phone/made-from.json, so the words cannot go stale."""
    return int(json.loads(need(REPO / "docs" / "figures" / "screens" / "phone" / "made-from.json").read_text(encoding="utf-8"))["nightly"])


def page_tour_index() -> str:
    data = tour()
    cards = []
    for key in data["order"]:
        item = data["screens"][key]
        mobile = item.get("mobile") or {}
        desktop_thumb = ('<p class="eyebrow">On the phone only</p>' if item.get("platform") == "mobile" else
                         f'<img class="research-thumb" src="/assets/screens/{key}-dark-1400x900.webp" alt="" width="320" height="206" loading="lazy">')
        mobile_thumb = (phone_thumb(mobile["shot"]) if mobile.get("shot")
                        else '<p class="eyebrow">On the phone; not yet pictured there</p>' if mobile.get("pending")
                        else '<p class="eyebrow">On the desktop only, for now</p>')
        cards.append(
            f'<a class="panel pad stack tight research-card plain" href="/tour/{key}/">'
            f'{shown("desktop", desktop_thumb)}{shown("mobile", mobile_thumb)}'
            f'<h3 class="h3">{esc(item["name"])}</h3>'
            f'<p class="small">{esc(item["blurb"])}</p>'
            "</a>"
        )

    body = f"""
<section class="wrap stack page-top">
<p class="eyebrow">Tour</p>
<h1>A tour of GroupLab</h1>
<p class="small"><a href="/tour/how-it-works/">Behind the curtain: how GroupLab works &#8594;</a></p>
<p class="lead">Every screen, what it is for, and what you would do on it. {count_words('tour-screens', capital=True)} pages, one per screen, so you can see what using GroupLab is like before you download it.</p>
<div class="note note-teal note-figure"><span class="mono">Your own targets</span><p>GroupLab works on any target you already shoot: photograph or scan it, set the scale once, and mark the holes by hand on the computer's <a href="/tour/marking/">marking screen</a>, as in this sample, or on the phone under a crosshair. A GroupLab sheet is the fast lane, where the scale and every hole are found by themselves; it is not a requirement.</p>{screen_link("marking-other", "A plain sample target marked by hand on the marking screen")}</div>
{platform_switch()}
{shown("desktop", '<p class="small faint">The pictures are regenerated every week from the newest build, so what you see here is the version you would install. Every sheet and every result in them is generated: no real target and nobody\'s photographs.</p>')}
{shown("mobile", f'<p class="small faint">The pictures are taken on the Android emulator, sized as a Galaxy Z Fold 7 and a Galaxy Tab S8 Ultra, after each nightly (these are from nightly {phone_nightly()}); the result in them is Alan\'s own 600 dpi scan of a 25 shot group, published with his consent.</p>')}
<div class="research-grid">{"".join(cards)}</div>
<div class="panel pad hiw-tour-card"><div><p class="eyebrow">Behind the curtain</p><h2 class="h3">How GroupLab works</h2>
<p class="small">A map of the parts and what each is built from, how OpenCV is used, how the hole detector was built, and one target followed stage by stage.</p></div>
<a class="btn btn-primary" href="/tour/how-it-works/">See how it works</a></div>
</section>
"""
    return shell("/tour/", "Tour", "Every screen in GroupLab, what it is for, and what you would do on it.", body, "Tour")


def page_tour_screen(key: str) -> str:
    data = tour()
    item = data["screens"][key]
    order = data["order"]
    at = order.index(key)
    before = order[at - 1] if at > 0 else None
    after = order[at + 1] if at + 1 < len(order) else None

    def parts_and_steps(parts: list, steps: list) -> str:
        listed = "".join(f'<li><strong>{esc(label)}.</strong> {text}</li>' for label, text in parts)
        done = "".join(f"<li>{esc(step)}</li>" for step in steps)
        return (f'<h2>What you are looking at</h2><ul class="tour-parts">{listed}</ul>'
                f'<h2>What you would do here</h2><ol>{done}</ol>')

    # Entry 249: the Desktop side and the Mobile side of the stop. A stop only one of them has says so on the other, rather than hiding.
    mobile = item.get("mobile") or {}
    phone_only = item.get("platform") == "mobile"
    if phone_only:
        desktop_picture, desktop_words = only_note(item["desktopWords"]), ""
    else:
        desktop_picture = (tour_shot(key, item["name"] + " in GroupLab: " + item["blurb"], eager=True)
                           + '<p class="small faint">From the newest build of GroupLab, regenerated every week. Tap the picture for it full size.</p>'
                           + "".join(f'{tour_shot(shot, alt)}<p class="small faint">{esc(caption)}</p>' for shot, alt, caption in item.get("moreShots", [])))
        desktop_words = parts_and_steps(item["parts"], item["steps"])
    if mobile.get("shot"):
        mobile_picture = (f'<a class="plain tour-shot" href="/assets/screens/phone/{phone_themes(mobile["shot"])[0]}.webp">'
                          f'{phone_screen(mobile["shot"], item["name"] + " in GroupLab on a phone")}</a>'
                          f'<p class="small faint">{esc(mobile.get("caption", ""))}</p>')
        mobile_words = parts_and_steps(item["parts"], item["steps"]) if phone_only else parts_and_steps(mobile["parts"], mobile["steps"])
        if mobile.get("note"):
            mobile_words += only_note(mobile["note"])
    else:
        mobile_picture, mobile_words = only_note(mobile.get("words", "On the desktop only, for now.")), ""
    links = "".join(f'<li><a href="{href}">{esc(label)}</a></li>' for label, href in (item.get("links") or []))

    around = []
    if before:
        around.append(f'<a href="/tour/{before}/">&lsaquo; {esc(data["screens"][before]["name"])}</a>')
    around.append('<a href="/tour/">All screens</a>')
    if after:
        around.append(f'<a href="/tour/{after}/">{esc(data["screens"][after]["name"])} &rsaquo;</a>')

    body = f"""
<section class="wrap guide">
<article class="prose">
<p class="eyebrow"><a class="plain" href="/tour/">Tour</a> &rsaquo; {esc(item["name"])}</p>
<h1>{esc(item["name"])}</h1>
<p class="lead">{esc(item["blurb"])}</p>
{f'<p class="note note-teal">{esc(item["merged"])}</p>' if item.get("merged") else ""}
{platform_switch()}
{shown("desktop", desktop_picture)}
{shown("mobile", mobile_picture)}
<h2>What this screen is for</h2>
<p>{esc(item["purpose"])}</p>
<h2>Without a GroupLab sheet</h2>
<p>{esc(item["withoutASheet"])} <a href="/what-can-be-measured/">What GroupLab can measure</a>.</p>
{shown("desktop", desktop_words)}
{shown("mobile", mobile_words)}
<h2>Where it fits</h2>
<p>{item["fits"]}</p>
{f'<h2>Read more</h2><ul>{links}</ul>' if links else ""}
<nav class="tour-around" aria-label="Other screens">{" ".join(around)}</nav>
</article>
</section>
"""
    return shell(f"/tour/{key}/", item["name"], item["blurb"], body, "Tour")


# Entry 155: the target library and the print screen became one screen, Targets. Their old tour addresses stay, each a plain page with
# one link to the new one, the way the send page's old address is kept: nobody holding the old link is sent anywhere without choosing.
TOUR_MOVED = {"library": "the target library", "print": "printing"}


def page_tour_moved(old: str) -> str:
    body = f"""
<section class="wrap page-head stack last">
<p class="eyebrow">Tour</p>
<h1>This page has moved.</h1>
<p class="lead">The target library and printing were two screens doing one job, and they are one screen now, Targets. What this page said about
{TOUR_MOVED[old]} is on its page.</p>
<div class="actions">{btn("Go to Targets", "/tour/targets/", True)}</div>
</section>
"""
    return shell(f"/tour/{old}/", "Targets", "The target library and printing are one screen now, Targets.", body, "Tour")


FEATURES_PATH = "/features/"
PLATFORMS = ["Windows", "macOS", "Linux", "Android", "iPhone and iPad"]
PHONES = {"Android", "iPhone and iPad"}


def features() -> dict:
    return json.loads(need(REPO / "website" / "features.json").read_text(encoding="utf-8"))


def build_number(version: str) -> int:
    m = re.search(r"nightly\.(\d+)", version)
    return int(m.group(1)) if m else 0


def release_sections() -> dict[str, str]:
    """Each build's section of docs/RELEASE-NOTES.md, by version."""
    text = need(REPO / "docs" / "RELEASE-NOTES.md").read_text(encoding="utf-8")
    return {s.split("\n", 1)[0].strip(): s for s in re.split(r"\n## ", text)[1:]}


def noticed(section: str) -> list[str]:
    m = re.search(r"\*\*What you will notice\*\*\n\n(.*?)(?:\n\n\*\*|\n\n\[|\Z)", section, re.S)
    return re.findall(r"^- (.*)$", m.group(1), re.M) if m else []


def notes_of(f: dict) -> list[str]:
    """A feature's release-note phrases: one, or several where the change it is came with more than one note (entry 340's two)."""
    note = f.get("note")
    return [note] if isinstance(note, str) else list(note or [])


def arrived(f: dict) -> str | None:
    """The build a feature arrived in. "next" means the build after the newest published one: the first whose notes say its phrase, or
    None until one does, which the page shows as coming in the next build (entry 243, so a feature can be listed with its change)."""
    if f.get("since") != "next":
        return f.get("since")
    found = [v for v, s in release_sections().items() if any(n in s for n in notes_of(f))]
    return min(found, key=build_number) if found else None


def changed(f: dict) -> str | None:
    """The build a feature's newest part arrived in, or "next" while one of its phrases is in no published build's notes yet (entry
    421: a sentence widened on main read as if the build it first arrived in already did it)."""
    sections = release_sections()
    builds = [min((v for v, s in sections.items() if n in s), key=build_number, default=None) for n in notes_of(f)]
    if arrived(f) is None:
        return None
    if None in builds:
        return "next"
    return max(builds + [arrived(f)], key=build_number)


def newest(f: dict) -> int:
    last = changed(f)
    return 10**6 if last in (None, "next") else build_number(last)


def spotlight(count: int = 3) -> list[dict]:
    """The newest features, entry 242 section 2.4: by the build their newest part arrived in (entry 421), and within a build as the file
    lists them."""
    items = features()["features"]
    return sorted(items, key=lambda f: -newest(f))[:count]


def published_sentences() -> dict[str, str]:
    """Each feature's sentence as the newest published nightly had it, from that nightly's tag; empty where the tag cannot be read."""
    versions = sorted(release_sections(), key=build_number)
    if not versions:
        return {}
    try:
        out = subprocess.run(["git", "show", f"v{versions[-1]}:website/features.json"], cwd=REPO, capture_output=True, text=True,
                             encoding="utf-8", timeout=30)
        return {f["key"]: f["sentence"] for f in json.loads(out.stdout)["features"]} if out.returncode == 0 else {}
    except (OSError, subprocess.SubprocessError, ValueError, KeyError):
        return {}


def feature_problems() -> list[str]:
    """Entry 242 section 2.2, as a build failure: nothing is listed that is not what it says."""
    found = []
    data = features()
    sections = release_sections()
    tours = set(tour()["screens"])
    shots = {p.name[: -len("-light-1400x900.png")] for p in SCREENS.glob("*-light-1400x900.png")}
    articles = {m["slug"] for m in research_articles()} if "research_articles" in globals() else set()
    # Entry 436 section 4.3: a feature may point at a subsection, so every id the page carries counts.
    anchors = guide_ids(render_guide(need(REPO / "docs" / "USER-GUIDE.md"))[1])
    published = published_sentences()
    keys = set()
    for f in data["features"]:
        where = f"website/features.json: {f.get('key')}"
        if f["key"] in keys:
            found.append(f"{where} is listed twice")
        keys.add(f["key"])
        for field in ("group", "name", "sentence", "platforms", "since", "note"):
            if not f.get(field):
                found.append(f"{where} has no {field}")
        if f.get("group") not in data["groups"]:
            found.append(f"{where}: {f.get('group')!r} is not one of the groups")
        if not set(f.get("platforms", [])) <= set(PLATFORMS):
            found.append(f"{where}: a platform is not one of {PLATFORMS}")
        section = sections.get(arrived(f) or "") if f.get("since") == "next" else sections.get(f.get("since", ""))
        if f.get("since") == "next" and arrived(f) is None:
            pass
        elif section is None:
            found.append(f"{where}: no build {f.get('since')} in docs/RELEASE-NOTES.md")
        elif notes_of(f) and not any(n in section for n in notes_of(f)):
            found.append(f"{where}: the {f['since']} notes do not say {notes_of(f)[0]!r}, so that is not where it arrived")
        if f.get("shot") is not None and f["shot"] not in shots:
            found.append(f"{where}: there is no screenshot {f['shot']!r}")
        for name, _ in f.get("sheets", []):
            if not (SCREENS / f"sheet-{name}.png").is_file():
                found.append(f"{where}: there is no picture sheet-{name}.png in docs/figures/screens/current (entry 256)")
        # Entry 249 item 4: a feature the phone has needs the phone's own picture, as one the desktop has needs the desktop's (entry 242).
        mobile = f.get("mobile") or {}
        desktop = set(f.get("platforms", [])) - PHONES
        if desktop and f.get("shot") is None and not f.get("noPicture"):
            found.append(f"{where} has no desktop picture and does not say why")
        if "Android" in f.get("platforms", []) and not mobile.get("shot") and not mobile.get("noPicture"):
            found.append(f"{where}: the phone has it and there is no phone picture, nor a reason; take one in the next device sitting")
        if mobile.get("shot") and not phone_themes(mobile["shot"]):
            found.append(f"{where}: there is no phone screenshot {mobile['shot']!r} in docs/figures/screens/phone")
        if mobile and "Android" not in f.get("platforms", []):
            found.append(f"{where} has a Mobile side and Android is not one of its platforms")
        if f.get("tour") and f["tour"] not in tours:
            found.append(f"{where}: no tour stop {f['tour']!r}")
        if f.get("guide") and f["guide"] not in anchors:
            found.append(f"{where}: the user guide has no section {f['guide']!r}")
        if f.get("article") and articles and f["article"] not in articles:
            found.append(f"{where}: no published article {f['article']!r}")
        # Entry 421: a sentence changed since the newest nightly describes something that build does not do, unless a note of the
        # change is still to come; "reworded" names the newest nightly when only the words changed.
        newest_build = max(sections, key=build_number, default="")
        before = published.get(f["key"])
        if before is not None and before != f["sentence"] and changed(f) != "next" and f.get("reworded") != newest_build:
            found.append(f"{where}: its sentence changed since {newest_build} and none of its notes is still to come; add the change's "
                         f"note phrase, or \"reworded\": \"{newest_build}\" if only the words changed")
    # From checkedFrom on, every note a reader will notice belongs to a feature or is said not to be one.
    claimed = [n for f in data["features"] for n in notes_of(f)] + data.get("notFeatures", [])
    for version, section in sections.items():
        if build_number(version) < build_number(data["checkedFrom"]):
            continue
        for note in noticed(section):
            if not any(c in note for c in claimed):
                found.append(f"website/features.json: {version}'s note {note[:80]!r} belongs to no feature and is not in notFeatures")
    return found


# Entry 265: which download on /download/ stands for each platform the platform statement says is built and published.
PLATFORM_DOWNLOADS = {"Windows": "grouplab-setup-win-x64.exe", "macOS": "grouplab-macos-", "Linux": "grouplab-linux-x64", "Android": "grouplab-android.apk"}


def download_problems() -> list:
    """Entry 265: every platform in docs/PLATFORM-SUPPORT.md's table has a download on /download/, so none can go missing again."""
    found = []
    page = (OUT / "download" / "index.html").read_text(encoding="utf-8")
    for name in re.findall(r"^\| \*\*([A-Za-z]+)\*\* \|", need(PLATFORM_SUPPORT).read_text(encoding="utf-8"), re.M):
        asset = PLATFORM_DOWNLOADS.get(name)
        if asset is None:
            found.append(f"docs/PLATFORM-SUPPORT.md names {name}, and the site build does not know which download stands for it (PLATFORM_DOWNLOADS)")
        elif asset not in page:
            found.append(f"/download/ has no download for {name}, which docs/PLATFORM-SUPPORT.md says is built and published")
    return found


# Entry 349: what the download page and the Desktop or Mobile switch guess for each kind of device, run in Node with the browser's parts
# stubbed. The user agents are the published forms of each browser, not ones captured from anybody.
DEVICE_CASES = [
    ("Firefox on an Android tablet asking for the desktop site", "Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", 5, True, False, {}, "android", "Looks like this device", "mobile"),
    ("Chrome on an Android tablet asking for the desktop site", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", 5, True, False, {}, "android", "Looks like this device", "mobile"),
    ("Safari on an iPad asking for the desktop site", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Safari/605.1.15", 5, True, False, {}, "iphone", "Looks like this device", "mobile"),
    ("a Linux laptop with a touch screen and a trackpad", "Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", 10, False, True, {}, "linux", "This computer", "desktop"),
    ("Linux on a desktop", "Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", 0, False, True, {}, "linux", "This computer", "desktop"),
    ("Chrome on a Galaxy Z Fold 7", "Mozilla/5.0 (Linux; Android 16; SM-F966U) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Mobile Safari/537.36", 5, True, False, {}, "android", "This phone", "mobile"),
    ("Firefox on an Android phone", "Mozilla/5.0 (Android 16; Mobile; rv:143.0) Gecko/143.0 Firefox/143.0", 5, True, False, {}, "android", "This phone", "mobile"),
    ("Windows on a laptop with a touch screen", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36", 10, False, True, {}, "windows", "This computer", "desktop"),
    # Entry 403: a pen or a trackpad gives the tablet a fine pointer, so the touch test alone said Linux; what the browser says of its
    # processor and platform is read first. A Linux computer on ARM, a Raspberry Pi, and ChromeOS stay what they are.
    ("Firefox on a Galaxy tablet with an S Pen asking for the desktop site", "Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", 10, True, True, {"platform": "Linux aarch64", "oscpu": "Linux aarch64"}, "android", "Looks like this device", "mobile"),
    ("Chrome on a tablet with a pen asking for the desktop site", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", 10, True, True, {"platform": "Linux armv8l", "userAgentData": {"platform": "Android", "mobile": False}}, "android", "Looks like this device", "mobile"),
    ("Chrome asking for the desktop site where only its own platform says Android", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", 10, False, True, {"platform": "Linux x86_64", "userAgentData": {"platform": "Android", "mobile": False}}, "android", "Looks like this device", "mobile"),
    ("a tablet with a keyboard cover and trackpad, Firefox, desktop site", "Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", 10, False, True, {"platform": "Linux armv81", "oscpu": "Linux armv81"}, "android", "Looks like this device", "mobile"),
    ("a Raspberry Pi", "Mozilla/5.0 (X11; Linux aarch64; rv:131.0) Gecko/20100101 Firefox/131.0", 0, False, True, {"platform": "Linux aarch64", "oscpu": "Linux aarch64"}, "linux", "This computer", "desktop"),
    ("a Linux laptop with a touch screen and a trackpad, saying its processor", "Mozilla/5.0 (X11; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", 10, False, True, {"platform": "Linux x86_64", "oscpu": "Linux x86_64"}, "linux", "This computer", "desktop"),
    ("a Chromebook", "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", 10, False, True, {"platform": "Linux x86_64", "userAgentData": {"platform": "Chrome OS", "mobile": False}}, "linux", "This computer", "desktop"),
    ("a Chromebook on ARM", "Mozilla/5.0 (X11; CrOS aarch64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", 10, False, True, {"platform": "Linux aarch64", "userAgentData": {"platform": "Chrome OS", "mobile": False}}, "linux", "This computer", "desktop"),
]

DEVICE_HARNESS = r"""
const cases = JSON.parse(process.argv[1]), scripts = JSON.parse(process.argv[2]), out = [];
for (const [name, ua, touch, coarse, fine, extra] of cases) {
  const attrs = {}, ready = [], label = { textContent: "" };
  Object.defineProperty(globalThis, "navigator", { value: Object.assign({ userAgent: ua, maxTouchPoints: touch }, extra), configurable: true, writable: true });
  global.window = { matchMedia: q => ({ matches: q === "(pointer: coarse)" ? coarse : q === "(any-pointer: fine)" ? fine : false }), addEventListener() {} };
  global.location = { search: "", hash: "" };
  global.history = { replaceState() {} };
  global.localStorage = { getItem() { return null; }, setItem() {} };
  global.document = {
    documentElement: { setAttribute: (k, v) => { attrs[k] = v; }, getAttribute: k => attrs[k] },
    addEventListener: (e, f) => ready.push(f),
    querySelectorAll: sel => sel === "[data-pick]" ? [{ getAttribute: () => attrs["data-device"], querySelector: () => label, setAttribute() {}, addEventListener() {} }] : [],
    getElementById: () => null,
  };
  for (const js of scripts) { new Function(js)(); }
  ready.forEach(f => f());
  out.push([name, attrs["data-device"], label.textContent, attrs["data-platform"]]);
}
console.log(JSON.stringify(out));
"""


def device_problems() -> list:
    """Entry 349: every case in DEVICE_CASES gets the download and the words it should, and the switch the platform it should. Needs Node,
    which GitHub's runners carry; where it is missing the check says so and passes."""
    node = shutil.which("node")
    if node is None:
        print("device guess: Node is not here, so the download page's guesses were not run")
        return []
    cases = [list(c[:6]) for c in DEVICE_CASES]
    run = subprocess.run([node, "-e", DEVICE_HARNESS, json.dumps(cases), json.dumps([DOWNLOAD_JS, JS])], capture_output=True, text=True, timeout=60)
    if run.returncode != 0:
        return [f"device guess: the scripts did not run in Node: {run.stderr.strip()[:300]}"]
    found = []
    for (name, device, words, platform), case in zip(json.loads(run.stdout), DEVICE_CASES):
        want = case[6:]
        if (device, words, platform) != want:
            found.append(f"device guess: {name} gets {device!r}, {words!r} and {platform!r}, not {want[0]!r}, {want[1]!r} and {want[2]!r}")
    return found


def parity_problems() -> list:
    """Entry 258: every feature says whether it is on the phone, coming, or left out, in docs/PHONE-PARITY.md, and agrees with features.json."""
    found = []
    table = need(REPO / "docs" / "PHONE-PARITY.md").read_text(encoding="utf-8")
    rows = {}
    # Entry 290 section 6: a fifth column says where each feature stands on iPhone and iPad, in one of four words.
    for m in re.finditer(r"^\| [^|]+ \| `([a-z0-9-]+)` \| (on the phone|coming|left out) \| ([^|]*)\| ([^|]*)\|$", table, re.M):
        if m.group(1) in rows:
            found.append(f"docs/PHONE-PARITY.md: {m.group(1)} has two rows")
        rows[m.group(1)] = (m.group(2), m.group(3).strip(), m.group(4).strip())
        if m.group(4).strip() not in ("on iOS", "not yet", "on a device", "left out"):
            found.append(f"docs/PHONE-PARITY.md: {m.group(1)} says {m.group(4).strip()!r} for iPhone and iPad, not one of on iOS, not yet, on a device, left out")
    for f in features()["features"]:
        row = rows.pop(f["key"], None)
        if row is None:
            found.append(f"docs/PHONE-PARITY.md: {f['key']} has no row saying whether it is on the phone, coming or left out (entry 258)")
            continue
        on = "Android" in f["platforms"]
        if on != (row[0] == "on the phone"):
            found.append(f"docs/PHONE-PARITY.md: {f['key']} says {row[0]!r} and features.json {'lists' if on else 'does not list'} Android")
        # Entry 345: the Features page names iPhone and iPad exactly where the fifth column says on iOS.
        if ("iPhone and iPad" in f["platforms"]) != (row[2] == "on iOS"):
            found.append(f"docs/PHONE-PARITY.md: {f['key']} says {row[2]!r} for iPhone and iPad and features.json {'lists' if 'iPhone and iPad' in f['platforms'] else 'does not list'} them")
        if row[0] != "on the phone" and not row[1]:
            found.append(f"docs/PHONE-PARITY.md: {f['key']} is {row[0]} and says no {'stage' if row[0] == 'coming' else 'reason'}")
    for key in rows:
        found.append(f"docs/PHONE-PARITY.md: {key} is not a feature in website/features.json")
    return found


def feature_card(f: dict, compact: bool = False) -> str:
    links = []
    if f.get("tour"):
        links.append(f'<a href="/tour/{f["tour"]}/">On the tour</a>')
    if f.get("guide"):
        links.append(f'<a href="/guides/user-guide/#{f["guide"]}">In the user guide</a>')
    if f.get("article"):
        links.append(f'<a href="/research/{f["article"]}/">The research behind it</a>')
    picture = ""
    if not compact:
        # Entry 249: each side of the switch shows its own picture, or says plainly that the feature is not on it.
        mobile = f.get("mobile") or {}
        on_desktop = bool(set(f["platforms"]) - PHONES)
        desktop = (f'<a class="plain" href="/assets/screens/{f["shot"]}-dark-1400x900.webp">{screen(f["shot"], f["name"] + " in GroupLab")}</a>'
                   if f.get("shot") else "" if on_desktop else only_note("On the phone only."))
        phone = (f'<a class="plain" href="/assets/screens/phone/{phone_themes(mobile["shot"])[0]}.webp">{phone_screen(mobile["shot"], f["name"] + " in GroupLab on a phone")}</a>'
                 if mobile.get("shot") else "" if "Android" in f["platforms"] else only_note("On the desktop only, for now."))
        picture = sheet_pictures(f) + shown("desktop", desktop) + shown("mobile", phone)
    platforms = " · ".join(f["platforms"])
    return (f'<article class="panel pad stack tight feature" id="{f["key"]}">{picture}<h3 class="h4">{esc(f["name"])}</h3>'
            f'<p>{esc(f["sentence"])}</p><p class="small faint">{esc(platforms)}. {esc(since_words(f))}.</p>'
            + (f'<p class="small">{" · ".join(links)}</p>' if links else "") + "</article>")


def sheet_pictures(f: dict) -> str:
    """Entry 256: a feature that names a bull, a grid or a sheet shows that thing as it prints, the same on both sides of the switch."""
    if not f.get("sheets"):
        return ""
    figures = []
    for name, caption in f["sheets"]:
        size = Image.open(need(SCREENS / f"sheet-{name}.png")).size
        # Entry 300 section 5: the vectors where the walk wrote them, sharp at any size; the picture otherwise.
        src = f"/assets/screens/sheet-{name}.svg" if (SCREENS / f"sheet-{name}.svg").is_file() else f"/assets/screens/sheet-{name}.webp"
        figures.append(f'<figure class="sheet-pic"><a class="plain" href="{src}"><img src="{src}" '
                       f'alt="{esc(caption)}" width="{size[0]}" height="{size[1]}" loading="lazy" decoding="async"></a>'
                       f'<figcaption class="small faint">{esc(caption)}</figcaption></figure>')
    return f'<div class="sheet-pics">{"".join(figures)}</div>'


def since_words(f: dict) -> str:
    version, last = arrived(f), changed(f)
    if not version:
        return "Coming in the next build"
    words = "Since " + version.replace("0.2.0-", "")
    if last == "next":
        return words + "; its newest part is coming in the next build"
    return words + (", changed in " + last.replace("0.2.0-", "") if build_number(last) > build_number(version) else "")


def spotlight_words(f: dict) -> str:
    """Entry 421: the home page's card names the build its newest part arrived in."""
    version, last = arrived(f), changed(f)
    if not version or last == "next":
        return "Coming in the next build"
    return ("Changed in " if build_number(last) > build_number(version) else "New in ") + last.replace("0.2.0-", "")


def spotlight_section(heading: str) -> str:
    cards = "".join(f'<a class="panel pad stack tight plain spot" href="{FEATURES_PATH}#{f["key"]}"><p class="eyebrow">{esc(spotlight_words(f))}</p>'
                    f'<h3 class="h4">{esc(f["name"])}</h3><p class="small">{esc(f["sentence"])}</p></a>' for f in spotlight())
    return f'<section class="wrap stack"><h2>{esc(heading)}</h2><div class="grid-3">{cards}</div></section>'


def page_features() -> str:
    """Every feature GroupLab has, entry 242 section 2, from website/features.json; the tour is the walk, this is the list."""
    data = features()
    groups = []
    for group in data["groups"]:
        items = [f for f in data["features"] if f["group"] == group]
        groups.append(f'<section class="wrap stack"><h2 id="{slug(group)}">{esc(group)}</h2><div class="grid-2 features">{"".join(feature_card(f) for f in items)}</div></section>')
    body = f"""
<section class="wrap page-head stack">
<p class="eyebrow">Features</p>
<h1>Everything GroupLab does</h1>
<p class="lead">Every feature, grouped, with where it is explained. The <a href="/tour/">tour</a> walks the main path, print, shoot, scan or photograph, read the numbers; this is the whole list. <a href="/tour/how-it-works/">Behind the curtain</a> shows how the parts behind them fit together.</p>
{platform_switch()}
<p class="small">{" · ".join(f'<a href="#{slug(g)}">{esc(g)}</a>' for g in data["groups"])}</p>
</section>
{spotlight_section("Newest")}
{"".join(groups)}
<section class="wrap stack last">{shown("desktop", '<p class="small faint">The pictures are the desktop application\'s, from the newest build, regenerated every week.</p>')}{shown("mobile", f'<p class="small faint">The pictures are taken on the Android emulator, sized as a Galaxy Z Fold 7 and a Galaxy Tab S8 Ultra, after each nightly (these are from nightly {phone_nightly()}); the result in them is Alan\'s own 600 dpi scan of a 25 shot group, published with his consent.</p>')}</section>
"""
    return shell(FEATURES_PATH, "Features", "Every feature GroupLab has, grouped, each with the build it arrived in and where it is explained.", body, "Features")


SURVEY_PATH = "/survey/"


def survey_devices() -> dict:
    return json.loads(need(REPO / "website" / "survey-devices.json").read_text(encoding="utf-8"))


def page_survey() -> str:
    """The hardware survey's published figures, NOTES-FROM-PLANNING.md entry 241 section 5.

    Two halves, and they come from different places on purpose. The project's own test devices are Alan's, shown by name with his
    permission, and their numbers are what each device showed on its own screen, from `website/survey-devices.json`: the server keeps no
    individual record to take them from. Everybody else's figures come from the aggregate the survey worker publishes beside this page,
    counts and medians only, read by a small script; until a group has ten machines it says so rather than showing it.
    """
    data = survey_devices()
    shown = [d for d in data["devices"] if d.get("milliseconds")]
    slowest = max(d["milliseconds"] for d in shown) if shown else 1
    cards = []
    for d in data["devices"]:
        if d.get("milliseconds"):
            share = max(4, round(100 * d["milliseconds"] / slowest))
            stages = d.get("stages") or {}
            stage_rows = "".join(f'<tr><td>{esc(k)}</td><td class="mono num">{v / 1000:.2f} s</td></tr>' for k, v in stages.items())
            stage_part = (f'<table class="survey-stages"><caption class="small faint">Stage by stage</caption>{stage_rows}</table>'
                          if stage_rows else '<p class="small faint">Its screen gives the whole time, not the stages.</p>')
            result = (f'<p class="survey-time"><span class="mono">{d["milliseconds"] / 1000:.1f} s</span> '
                      f'<span class="small faint">median of {d["runs"]} run{"s" if d["runs"] != 1 else ""}, GroupLab {esc(d["version"])}</span></p>'
                      f'<div class="survey-bar" role="img" aria-label="{d["milliseconds"] / 1000:.1f} seconds"><span style="width:{share}%"></span></div>'
                      f'<p class="small">At most {d["peakMegabytes"]} MB of memory; {d["holesFound"]} of its {d["holesPlaced"]} holes found.</p>'
                      + stage_part)
        else:
            result = '<p class="small">Not read yet.</p>'
        cards.append(f'<div class="panel pad stack tight"><h3 class="h4">{esc(d["name"])}</h3><p class="small">{esc(d["hardware"])}</p>'
                     f'{result}<p class="small faint">Source: {esc(d["source"])}.</p></div>')
    body = f"""
<section class="wrap page-head stack">
<p class="eyebrow">Hardware survey</p>
<h1>What GroupLab runs on, and how fast</h1>
<p class="lead">GroupLab asks once whether it may tell the project what it runs on and how long the built-in benchmark takes. These are the published figures: counts and medians, never one person's record.</p>
<p class="small" id="survey-range">The figures from everybody's reports load from the survey's own count, updated every hour.</p>
<p class="small"><a href="/research/what-grouplab-sends/">What a report holds, and what is never in it</a>. In GroupLab you can say yes or no, run the benchmark, see your own runs, reset your survey number and delete your reports in Settings, under Sharing.</p>
</section>

<section class="wrap stack">
<h2>The project's own test devices</h2>
<p>These three are the developer's own machines, not users, shown by name with his permission. Each ran the same benchmark every copy of GroupLab runs: one analysis of a built-in 25 bull sheet with a hole in every bull. The bars are to the same scale.</p>
<div class="grid-3">{"".join(cards)}</div>
</section>

<section class="wrap stack">
<h2>Everybody else</h2>
<p>Each machine counts once for each version of GroupLab, by the median of its own runs: not its best, which would make every machine look faster than it is, and not its average, which one run slowed by something else would move. A group of fewer than {10} machines is not shown.</p>
<div id="survey-benchmark"><p class="small">Not enough reports yet: a group of fewer than 10 machines is not shown.</p></div>
</section>

<section class="wrap stack last">
<h2>Operating systems and memory</h2>
<p>From every report, each machine once. Anything seen on fewer than 10 machines is counted under "other".</p>
<div class="grid-2"><div id="survey-platforms"><p class="small">Not enough reports yet.</p></div><div id="survey-memory"><p class="small">Not enough reports yet.</p></div></div>
</section>
<script src="/assets/js/survey.js" defer></script>
"""
    return shell(SURVEY_PATH, "Hardware survey", "What GroupLab runs on and how fast the benchmark runs, from the hardware survey: counts and medians, never one person's record.", body)


def page_discord() -> str:
    """The community page, NOTES-FROM-PLANNING.md entry 151.

    Alan: "make it so the community link at the top of the page does not automatically redirect to the discord
    server. People will not appreciate this. Make a page that has a link to the invite and let people choose if
    they want to click on it."

    He is right, and the meta refresh entry 148 built is gone. A navigation item that ejects you from the site
    before you have read anything is the kind of thing people remember badly. The canonical address stays,
    because everything published points at it; it is a page now rather than a redirect.

    **One thing on this page leaves the site**, and it says so in words before it is clicked. The invite is
    visible as text as well as being that link's target, because some people want to see where a link goes
    before they follow it.
    """
    data = links()
    invite = data["discordInvite"]
    groups = "".join(
        f'<div class="panel pad stack tight"><h3 class="h4">{esc(g["name"])}</h3><p class="small">{esc(g["what"])}</p>'
        + (f'<p class="small mono">{" · ".join(esc(c) for c in g["channels"])}</p>' if g.get("channels") else "")
        + "</div>"
        for g in data.get("discordGroups", [])
    )
    rules = "".join(f"<li>{esc(r)}</li>" for r in data.get("discordRules", []))

    body = f"""
<section class="wrap page-head stack">
<p class="eyebrow">Community</p>
<h1>The GroupLab Discord</h1>
<p class="lead">Somewhere to ask a question, say what a build did, show a target, and talk to whoever is working on GroupLab. Nothing here joins anything on its own.</p>
</section>

<section class="wrap stack">
<h2>What is in there</h2>
<p>The server is grouped like this, so you can see what you are joining before you join.</p>
<div class="grid-2">{groups}</div>
</section>

<section class="wrap stack">
<h2>Joining</h2>
<p>The invite opens Discord in a new tab. It is the only thing on this page that leaves grouplab.org.</p>
<div class="actions"><a class="btn btn-primary" href="{invite}" target="_blank" rel="noopener noreferrer"><span>Open the invite on Discord</span><span class="btn-sub mono">Opens Discord in a new tab</span></a></div>
<p class="small faint">It goes to <code>{esc(invite)}</code>.</p>
<p class="small">Discord is a third party service. It has its own account requirement and its own terms, and it is not run by this project. <strong>Nobody has to join it to use GroupLab or to get help.</strong> <a href="/support/">The support address</a> reaches the same people, and <a href="https://github.com/oRAirwolf/grouplab/issues">GitHub issues</a> are the place for anything that should stay on the record.</p>
</section>

<section class="wrap stack last">
<h2>What is expected there</h2>
<p>The server's rules are the first thing you will see there. In short, so they are readable before you join rather than only after:</p>
<ol class="prose tight">{rules}</ol>
<p class="small">A photograph of a target can carry the place it was taken. Take the location data off it before posting, or send it through {f'<a href="{SEND}">the submission page</a>' if limits().get("open") else "the submission page, once it opens"}, which strips that before anybody sees the file. Nothing said on the server is legal advice, and no part of GroupLab is.</p>
</section>
"""
    return shell(DISCORD, "Community", "The GroupLab Discord: what is in it, what is expected there, and the invite, which you click yourself.", body, "Community")


def page_404() -> str:
    body = f"""
<section class="wrap page-head last">
<p class="eyebrow">404</p>
<h1>That page is not here.</h1>
<p class="lead">It may have moved, or the address may be mistyped.</p>
<div class="actions">{btn("Go to the home page", "/", True)}{btn("Download GroupLab", "/download/")}</div>
</section>
"""
    return shell("/404.html", "Page not found", "Page not found.", body)


# ---------------------------------------------------------------- css and js

TERMS_JS = """// NOTES-FROM-PLANNING.md entry 154: a glossary word explains itself on hover, on tap and on keyboard focus. Without this script the
// word is a link to its glossary entry, which is the mechanism; this is the enhancement.
(() => {
  let pop = null, owner = null;
  const close = () => {
    if (!pop) return;
    pop.remove(); pop = null;
    if (owner) owner.setAttribute("aria-expanded", "false");
    owner = null;
  };
  const open = (a) => {
    if (owner === a) return;
    close();
    owner = a;
    pop = document.createElement("div");
    pop.className = "term-pop";
    pop.setAttribute("role", "dialog");
    pop.setAttribute("aria-label", a.dataset.name);
    const text = document.createElement("p");
    text.textContent = a.dataset.plain;
    const row = document.createElement("div");
    row.className = "term-pop-row";
    const more = document.createElement("a");
    more.href = a.href;
    more.textContent = "More in the glossary";
    const shut = document.createElement("button");
    shut.type = "button";
    shut.textContent = "Close";
    shut.addEventListener("click", close);
    row.append(more, shut);
    pop.append(text, row);
    document.body.append(pop);
    const r = a.getBoundingClientRect(), view = document.documentElement.clientWidth;
    const width = Math.min(352, view - 24);
    pop.style.width = width + "px";
    pop.style.left = Math.max(12, Math.min(r.left, view - width - 12)) + window.scrollX + "px";
    // Below the line the word is on, so it never covers the sentence it explains.
    pop.style.top = r.bottom + window.scrollY + 8 + "px";
    a.setAttribute("aria-expanded", "true");
  };
  const term = (e) => e.target instanceof Element ? e.target.closest("a.term") : null;
  document.addEventListener("click", (e) => {
    const a = term(e);
    if (a) {
      // The first tap explains; a second follows the link to the full entry.
      if (owner !== a) { e.preventDefault(); open(a); }
      return;
    }
    if (pop && !pop.contains(e.target)) close();
  });
  document.addEventListener("mouseover", (e) => {
    const a = term(e);
    if (a && window.matchMedia("(hover: hover)").matches) open(a);
  });
  document.addEventListener("focusin", (e) => { const a = term(e); if (a) open(a); });
  document.addEventListener("keydown", (e) => { if (e.key === "Escape") close(); });
})();
"""

PRIMER_JS = """// NOTES-FROM-PLANNING.md entry 438 section 2.5: the statistics primer's figures that teach luck. Every group and every comparison here
// was drawn and measured by GroupLab's own statistics (docs/figures/primer/primer-data.json); this script only steps through them. Without
// it, each figure is the same picture, still, with its caption saying what it shows.
(() => {
  const figures = document.querySelectorAll(".primer-fig[data-primer]");
  if (!figures.length || !window.fetch) return;
  const ns = "http://www.w3.org/2000/svg";
  const f = (v) => v.toFixed(1);
  const farthest = (shots) => {
    let best = [0, 0, 0];
    for (let i = 0; i < shots.length; i++) for (let j = i + 1; j < shots.length; j++) {
      const d = Math.hypot(shots[i][0] - shots[j][0], shots[i][1] - shots[j][1]);
      if (d > best[0]) best = [d, i, j];
    }
    return best;
  };
  // The same drawing as the build's svg_group, so a stepped group looks exactly like the still one.
  const group = (shots, x0, y0, half, scale, extra) => {
    let out = `<rect class="pf-frame" x="${x0 - half}" y="${y0 - half}" width="${2 * half}" height="${2 * half}" rx="6"/>`;
    for (const r of [0.5, 1.0, 1.5]) if (r * scale < half) out += `<circle class="pf-ring" cx="${x0}" cy="${y0}" r="${f(r * scale)}"/>`;
    out += extra;
    for (const [x, y] of shots) out += `<circle class="pf-shot" cx="${f(x0 + x * scale)}" cy="${f(y0 - y * scale)}" r="3.2"/>`;
    return out;
  };
  const line = (shots, x0, y0, scale) => {
    const [, a, b] = farthest(shots);
    return `<path class="pf-es" d="M${f(x0 + shots[a][0] * scale)} ${f(y0 - shots[a][1] * scale)}L${f(x0 + shots[b][0] * scale)} ${f(y0 - shots[b][1] * scale)}"/>`;
  };
  const paint = (svg, inner) => { svg.innerHTML = inner; };
  fetch("/guides/statistics-primer/primer-data.json").then((r) => r.ok ? r.json() : null).then((data) => {
    if (!data) return;
    for (const fig of figures) {
      const controls = fig.querySelector(".pf-controls");
      const svg = fig.querySelector("svg");
      const said = fig.querySelector(".pf-said");
      if (!controls || !svg) continue;
      controls.hidden = false;
      if (fig.dataset.primer === "five-more") {
        let n = "5", start = 0;
        const more = controls.querySelector('[data-act="more"]');
        const show = () => {
          const list = data.groups[n];
          let inner = "";
          const six = [];
          for (let i = 0; i < 6; i++) six.push(list[(start + i) % list.length]);
          six.forEach((g, i) => {
            const x0 = 105 + (i % 3) * 215, y0 = 105 + Math.floor(i / 3) * 240;
            inner += group(g.shots, x0, y0, 95, 70, line(g.shots, x0, y0, 70));
            inner += `<text class="pf-label" x="${x0}" y="${y0 + 122}" text-anchor="middle">${g.es.toFixed(2)} MOA</text>`;
          });
          paint(svg, inner);
          const sizes = six.map((g) => g.es);
          more.textContent = `Shoot ${({ 5: "five", 10: "ten", 20: "twenty" })[n]} more`;
          said.textContent = `The newest group, top left: ${six[0].es.toFixed(2)} MOA. These six ${n}-shot groups run from ` +
            `${Math.min(...sizes).toFixed(2)} to ${Math.max(...sizes).toFixed(2)} MOA, from a rifle that never changed.`;
        };
        more.addEventListener("click", () => { start = (start - 1 + data.groups[n].length) % data.groups[n].length; show(); });
        for (const b of controls.querySelectorAll(".pf-size")) b.addEventListener("click", () => {
          n = b.dataset.n; start = 0;
          for (const o of controls.querySelectorAll(".pf-size")) o.setAttribute("aria-pressed", String(o === b));
          show();
        });
      } else if (fig.dataset.primer === "same-loads") {
        const five = data.sameLoads.find((s) => s.n === 5);
        let i = 0, shown = 1, looked = 0;
        const clearly = (e) => Math.min(e.esA, e.esB) <= 0.8 * Math.max(e.esA, e.esB);
        looked += clearly(five.examples[0]) ? 1 : 0;
        controls.querySelector('[data-act="again"]').addEventListener("click", () => {
          i = (i + 1) % five.examples.length;
          shown += 1;
          const e = five.examples[i];
          looked += clearly(e) ? 1 : 0;
          let inner = "";
          [["a", e.esA], ["b", e.esB]].forEach(([k, es], j) => {
            const x0 = 160 + j * 320;
            inner += group(e[k], x0, 130, 120, 65, line(e[k], x0, 130, 65));
            inner += `<text class="pf-label" x="${x0}" y="280" text-anchor="middle">Load ${k.toUpperCase()}: ${es.toFixed(2)} MOA</text>`;
          });
          paint(svg, inner);
          const better = e.esA < e.esB ? "A" : "B";
          said.textContent = `Load ${better} looks tighter this time${clearly(e) ? ", by 20 percent or more" : ""}. In the ${shown} ` +
            `comparisons so far, one looked at least 20 percent tighter ${looked} times, though the loads are the same.`;
        });
      }
    }
  }).catch(() => {});
})();
"""

GUIDE_TOC_JS = """// NOTES-FROM-PLANNING.md entry 436: the guide's contents list opens the section being read and folds the rest, marks where the reader
// is, and the address bar follows without adding to the history; a heading's link mark copies its address. Without this script the list
// is whole and every mark is a plain link, which is the mechanism; this is the enhancement.
// Entry 445: the same script on every page with the list, so what it follows is the list's own entries, each a heading or the card a
// heading names, and an entry the Desktop | Mobile switch hides is passed over.
(() => {
  const article = document.querySelector("[data-contents]");
  if (!article) return;
  const toc = document.querySelector("nav.toc");
  const still = window.matchMedia("(prefers-reduced-motion: reduce)");
  const links = new Map(), owner = new Map();
  const idOf = (a) => decodeURIComponent(a.hash.slice(1));
  if (toc) {
    toc.classList.add("js");
    for (const li of toc.querySelectorAll(":scope > ol > li")) {
      const top = li.querySelector(":scope > a");
      links.set(idOf(top), top);
      for (const a of li.querySelectorAll(":scope > ol a")) { links.set(idOf(a), a); owner.set(idOf(a), idOf(top)); }
    }
  }
  const heads = [...links.keys()].map((id) => document.getElementById(id)).filter(Boolean);
  const seen = (h) => h.getClientRects().length > 0;
  const sectionOf = (h) => owner.has(h.id) ? document.getElementById(owner.get(h.id)) : h;
  const fold = (li, open) => {
    const sub = li.querySelector(":scope > ol");
    li.classList.toggle("open", open);
    if (sub) sub.style.maxHeight = open ? sub.scrollHeight + "px" : "0px";
  };
  // When the list is taller than the window, it scrolls itself so the current entry stays in sight, after the fold has settled.
  const keep = (a) => {
    const go = () => {
      if (toc.scrollHeight <= toc.clientHeight) return;
      const top = a.offsetTop, bottom = top + a.offsetHeight, room = 48;
      if (top < toc.scrollTop + room) toc.scrollTop = top - room;
      else if (bottom > toc.scrollTop + toc.clientHeight - room) toc.scrollTop = bottom - toc.clientHeight + room;
    };
    go();
    if (!still.matches) setTimeout(go, 260);
  };
  let current;
  const show = (h) => {
    if (h === current || !toc) return;
    current = h;
    const section = h ? sectionOf(h) : null;
    const sec = section ? links.get(section.id) : null;
    const sub = h && h !== section ? links.get(h.id) : null;
    for (const li of toc.querySelectorAll(":scope > ol > li")) fold(li, sec ? li === sec.parentElement : false);
    for (const a of toc.querySelectorAll("a.here")) { a.classList.remove("here"); a.removeAttribute("aria-current"); }
    if (sec) sec.classList.add("here");
    if (sub) sub.classList.add("here");
    const deepest = sub || sec;
    if (deepest) { deepest.setAttribute("aria-current", "location"); keep(deepest); }
  };
  // The heading being read is the last one above a line near the top of the window.
  const reading = () => {
    const line = Math.min(window.innerHeight * 0.3, 260);
    let found = null;
    for (const h of heads) { if (!seen(h)) continue; if (h.getBoundingClientRect().top <= line) found = h; else break; }
    return found;
  };
  let moved = false, addressed, pinned = null;
  const address = (h) => {
    if (!moved || h === addressed) return;
    addressed = h;
    history.replaceState(history.state, "", h ? "#" + h.id : location.pathname + location.search);
  };
  const update = () => {
    if (pinned) return;
    const h = reading();
    show(h);
    address(h);
  };
  // A jump to a heading near the end may not bring it up to the line, so it stays the current one until that scroll has finished.
  const pin = (h) => {
    pinned = h;
    show(h);
    addressed = h;
    const done = () => { pinned = null; };
    if ("onscrollend" in window) window.addEventListener("scrollend", done, { once: true });
    setTimeout(done, 1200);
  };
  window.addEventListener("scroll", () => { moved = true; }, { once: true, passive: true });
  if ("IntersectionObserver" in window) {
    const watch = new IntersectionObserver(update, { rootMargin: "0px 0px -70% 0px" });
    for (const h of heads) watch.observe(h);
  }
  const target = () => {
    const h = location.hash ? document.getElementById(decodeURIComponent(location.hash.slice(1))) : null;
    return h && heads.includes(h) ? h : null;
  };
  if (target()) pin(target()); else update();
  window.addEventListener("hashchange", () => { if (target()) pin(target()); });
  const mini = document.querySelector("details.toc-mini");
  document.addEventListener("click", (e) => {
    const a = e.target instanceof Element ? e.target.closest("a[href^='#']") : null;
    if (!a) return;
    if (mini && mini.contains(a)) mini.open = false;
    if (toc && toc.contains(a)) {
      const h = document.getElementById(decodeURIComponent(a.hash.slice(1)));
      if (h) setTimeout(() => pin(h), 0);
    }
  });
  // Entry 437 section 3.4: pointing at, or focusing, a marking's number in the text or the caption thickens that outline.
  const hot = (e, on) => {
    const r = e.target instanceof Element ? e.target.closest(".mark-ref") : null;
    const box = r ? document.getElementById(r.dataset.mark) : null;
    if (box) box.classList.toggle("hot", on);
  };
  article.addEventListener("mouseover", (e) => hot(e, true));
  article.addEventListener("mouseout", (e) => hot(e, false));
  article.addEventListener("focusin", (e) => hot(e, true));
  article.addEventListener("focusout", (e) => hot(e, false));
  // The link mark: the section's full address in the address bar and on the clipboard, and a short note a screen reader hears too.
  const note = document.createElement("p");
  note.className = "link-note";
  note.setAttribute("role", "status");
  document.body.append(note);
  let hide;
  article.addEventListener("click", async (e) => {
    const a = e.target instanceof Element ? e.target.closest("a.hlink") : null;
    if (!a) return;
    e.preventDefault();
    const h = document.getElementById(idOf(a));
    if (!h) return;
    const url = location.origin + location.pathname + location.search + "#" + h.id;
    history.replaceState(history.state, "", "#" + h.id);
    pin(h);
    h.scrollIntoView({ block: "start", behavior: still.matches ? "auto" : "smooth" });
    let said = "Link copied";
    try { await navigator.clipboard.writeText(url); } catch (_) { said = "The link is in the address bar"; }
    note.textContent = said;
    note.classList.add("on");
    clearTimeout(hide);
    hide = setTimeout(() => { note.classList.remove("on"); note.textContent = ""; }, 2400);
  });
})();
"""

DOWNLOAD_CSS = r"""
/* Entry 338: the download page, concept A. With scripts off :root has no data-device, the buttons are hidden and every device's
   section shows in order; with them on, only the chosen device's section shows, and its parts join the stage so the file list can
   sit under its Not sure? line on the desktop. */
.dl-head{padding-bottom:28px}
.dl-picks{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;margin-top:8px}
:root:not([data-device]) .dl-picks{display:none}
.dl-pick{font:inherit;font-size:17px;font-weight:600;color:var(--text);background:var(--panel);border:1px solid var(--line2);border-radius:0;padding:16px 18px;min-height:76px;display:flex;flex-direction:column;align-items:flex-start;justify-content:center;gap:4px;text-align:left;cursor:pointer}
.dl-pick:hover{border-color:var(--dim)}
.dl-pick[aria-pressed="true"]{background:var(--amber-tint);border-color:var(--amber);box-shadow:inset 0 0 0 1px var(--amber)}
.dl-this{font-size:12px;font-weight:500;letter-spacing:.08em;text-transform:uppercase;color:var(--teal)}
.dl-this:empty{display:none}
.dl-stage{display:flex;flex-direction:column;gap:20px}
.dl-sec{display:flex;flex-direction:column;gap:20px;padding-top:32px;border-top:1px solid var(--line)}
.dl-sec:first-child{border-top:0;padding-top:0}
:root[data-device] .dl-sec{display:none}
:root[data-device="windows"] .dl-sec[data-for="windows"],:root[data-device="mac"] .dl-sec[data-for="mac"],:root[data-device="iphone"] .dl-sec[data-for="iphone"],:root[data-device="android"] .dl-sec[data-for="android"],:root[data-device="linux"] .dl-sec[data-for="linux"]{display:contents}
:root[data-device] .dl-sec-title{position:absolute;width:1px;height:1px;margin:-1px;overflow:hidden;clip:rect(0 0 0 0);white-space:nowrap}
.dl-sec-title{order:0;font-size:22px}
.dl-cards{order:1;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}
.dl-keep{order:1;color:var(--text)}
.dl-unsure{order:2;color:var(--text);font-size:15px}
.dl-files-wide{order:3}
.dl-help{order:4}
.badge-teal{background:var(--teal-tint);border-color:var(--teal-tint-b);color:var(--teal)}
.dl-file{overflow-wrap:anywhere}
.dl-none{border-style:dashed;align-self:start}
.dl-else>summary{font-family:"IBM Plex Sans Condensed","IBM Plex Sans",sans-serif;font-size:26px;font-weight:700}
.dl-table{overflow-x:auto}
.dl-table table{border-collapse:collapse;width:100%;border-top:1px solid var(--line2)}
.dl-table th,.dl-table td{border-bottom:1px solid var(--line);padding:8px 10px;text-align:left;vertical-align:top;color:var(--text)}
.dl-table th{font-family:"IBM Plex Mono",monospace;font-size:12px;font-weight:400;letter-spacing:.08em;text-transform:uppercase;color:var(--faint);border-bottom-color:var(--line2)}
.dl-table code{overflow-wrap:anywhere}
.dl-files-narrow{display:none}
@media (max-width:860px){
.dl-picks{display:flex;flex-wrap:wrap;gap:8px}
.dl-pick{flex-direction:row;align-items:center;min-height:44px;padding:8px 16px;border-radius:999px;font-size:15px;gap:8px}
.dl-this{font-size:11px}
.dl-cards{grid-template-columns:minmax(0,1fr)}
.dl-files-wide{display:none}
.dl-files-narrow{display:block}
}

"""

HOME_CSS = r"""
/* Entry 440: the concept B home page, the home page since entry 441. The site's tokens and fonts; the layout is the canvas's. */
.tp-landing{overflow-x:hidden}
.tp-landing main,.tp-landing .tp-footer,.tp-landing .tp-band{margin-right:var(--rule)}
/* Entry 443: the header is every page's; on the home page it floats over the orbit. Entry 444 section 2: the orbiting screens passed under
   its links and made them hard to read, so it is blurred and tinted from the start, and solid once the wordmark has docked. The docking
   wordmark flies above it while it moves. */
.tp-home{position:fixed;left:0;right:var(--rule);background:var(--bg);border-bottom-color:transparent;transition:background-color .2s,border-color .2s}
@supports ((-webkit-backdrop-filter:blur(1px)) or (backdrop-filter:blur(1px))){
.tp-home{background:color-mix(in srgb,var(--bg) 70%,transparent);-webkit-backdrop-filter:blur(14px);backdrop-filter:blur(14px)}}
html.tp-js:not(.tp-still) .tp-hero h1{position:relative;z-index:7;pointer-events:none}
/* Entry 448: a menu open in the header is above the docking wordmark, and the wordmark never takes a tap: docked, it is invisible over
   the header's own small one, which then could not be tapped. */
.tp-home:has(.tp-menu[open]),.tp-home.tp-menu-open{z-index:8}
.tp-home.tp-solid{background:var(--bg);border-bottom-color:var(--line)}
.tp-motion-icon{display:none}
html:not(.tp-js) .tp-motion{display:none}
/* Entry 444 section 2: from 1280 to 1499 pixels all ten links share the bar with Pause motion, which shows its icon there, as on a phone. */
@media (min-width:1280px) and (max-width:1499px){
.tp-motion{width:36px;padding:0;color:var(--text)}
.tp-motion-word{display:none}
.tp-motion-icon{display:block}
}
.tp-hero{position:relative;height:940px;overflow:hidden;background:var(--sunk)}
.tp-orbits{position:absolute;left:50%;top:470px;width:0;height:0}
.tp-circle,.tp-ring{position:absolute;border-radius:50%}
.tp-core{left:-130px;top:-130px;width:260px;height:260px;border:1px solid var(--line)}
.tp-inner{left:-330px;top:-330px;width:660px;height:660px;border:1px solid var(--line2)}
.tp-outer{left:-560px;top:-560px;width:1120px;height:1120px;border:1px solid var(--line2)}
.tp-dashed{left:-780px;top:-780px;width:1560px;height:1560px;border:1px dashed var(--line)}
.tp-slot{position:absolute}
.tp-up{width:100%;height:100%}
.tp-shot{width:100%;height:100%;border-radius:10px;border:1px solid var(--line2);box-shadow:var(--shadow);overflow:hidden;background:var(--panel)}
.tp-shot img{width:100%;height:100%;object-fit:cover;object-position:top left}
@keyframes tp-spin{to{transform:rotate(360deg)}}
@keyframes tp-spin-back{to{transform:rotate(-360deg)}}
@keyframes tp-tick{to{transform:translateX(-50%)}}
html.tp-js:not(.tp-still) .tp-inner{animation:tp-spin 120s linear infinite}
html.tp-js:not(.tp-still) .tp-inner .tp-up{animation:tp-spin-back 120s linear infinite}
html.tp-js:not(.tp-still) .tp-outer{animation:tp-spin-back 200s linear infinite}
html.tp-js:not(.tp-still) .tp-outer .tp-up{animation:tp-spin 200s linear infinite}
.tp-ring,.tp-up,.tp-ticker{will-change:transform}
.tp-hero-text{position:absolute;left:0;right:0;top:300px;display:flex;flex-direction:column;align-items:center;text-align:center;padding:0 40px}
.tp-hero h1{display:inline-block;margin:0;font-size:136px;line-height:.9;letter-spacing:.01em;transform-origin:0 0}
.tp-tagline{margin:22px 0 6px;font-size:30px;font-weight:600;color:var(--text)}
.tp-tagline em{font-weight:400}
.tp-lead{margin:0 0 28px;max-width:560px;font-size:17px;line-height:1.6;color:var(--dim)}
.tp-actions{display:flex;gap:12px;flex-wrap:wrap;justify-content:center}
.tp-btn{display:flex;align-items:center;justify-content:center;gap:10px;padding:14px 22px;border-radius:8px;border:1px solid var(--line2);
background:var(--bg);color:var(--text);font-size:13px;letter-spacing:.08em}
.tp-btn:hover{text-decoration:none;border-color:var(--text)}
.tp-btn.tp-primary{background:var(--amber);border-color:var(--amber);color:var(--on-amber)}
.tp-btn.tp-primary:hover{opacity:.92}
.tp-platforms{margin:22px 0 0;font-size:11px;letter-spacing:.1em;color:var(--faint)}
.tp-hero-phone{display:none}
.tp-narrow{display:none}
.tp-band{border-top:1px solid var(--line);border-bottom:1px solid var(--line);overflow:hidden;padding:14px 0;background:var(--bg)}
.tp-ticker{display:flex;width:max-content;font-size:12px;letter-spacing:.1em;color:var(--dim)}
.tp-tags{display:flex;gap:40px;padding-right:40px}
.tp-tags>span{white-space:nowrap}
.tp-dot{color:var(--amber)}
html.tp-js:not(.tp-still) .tp-ticker{animation:tp-tick 40s linear infinite}
.tp-band:hover .tp-ticker,.tp-band:focus-within .tp-ticker{animation-play-state:paused}
html:not(.tp-js) .tp-again,.tp-still .tp-again{display:none}
html:not(.tp-js) .tp-ticker,.tp-still .tp-ticker{width:auto;padding:0 40px}
html:not(.tp-js) .tp-tags,.tp-still .tp-tags{flex-wrap:wrap;row-gap:8px;padding-right:0}
.tp-section{border-top:1px solid var(--line);scroll-margin-top:68px}
#measure{border-top:0}
.tp-strip-label{display:flex;justify-content:space-between;gap:16px;font-size:11px;letter-spacing:.1em;color:var(--faint);padding:12px 20px}
.tp-head{padding:18px 40px;font-size:12px;border-bottom:1px solid var(--line)}
.tp-intro{padding:90px 40px 70px}
.tp-intro h2,.tp-phone-words h2{margin:0 0 18px;font-size:64px;line-height:1.02;font-weight:600;max-width:900px}
.tp-intro h2 em{font-weight:400}
.tp-intro p,.tp-phone-words p{margin:0;max-width:620px;font-size:18px;line-height:1.6;color:var(--dim)}
#honest .tp-intro{padding-bottom:40px}
#honest .tp-intro h2{margin:0}
.tp-cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));border-top:1px solid var(--line)}
.tp-card{display:block;background:var(--panel);color:var(--text);border-right:1px solid var(--line);border-bottom:1px solid var(--line)}
.tp-card:hover{text-decoration:none}
.tp-card .tp-strip-label{border-bottom:1px solid var(--line)}
.tp-card-pic{height:280px;overflow:hidden;background:var(--sunk)}
.tp-card-img{width:100%;height:100%;object-fit:cover;object-position:top left;transition:transform .6s ease}
.tp-card:hover .tp-card-img{transform:scale(1.05)}
.tp-card-text{padding:22px 22px 28px}
.tp-card h3,.tp-point h3{margin:0 0 8px;font-size:24px;font-weight:600}
.tp-card p,.tp-point p{margin:0;font-size:16px;line-height:1.55;color:var(--dim)}
.tp-points{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));border-top:1px solid var(--line)}
.tp-point{padding:32px 40px 48px;border-right:1px solid var(--line)}
.tp-point h3{margin-bottom:10px}
.tp .tp-point .tp-letter{font-size:13px;color:var(--amber);margin-bottom:14px}
.tp-phone-row{padding:80px 40px;display:flex;flex-wrap:wrap;gap:56px;align-items:center}
.tp-phone-words{flex:1 1 360px;min-width:0}
.tp-phone-words h2{font-size:56px;line-height:1.04}
.tp-phones{flex:1 1 560px;display:flex;gap:22px;justify-content:center;padding-bottom:30px}
.tp-phone-shot{height:440px;width:auto;border-radius:24px;border:6px solid #0b0c0e;box-shadow:var(--shadow)}
.tp-phone-shot.tp-low{transform:translateY(30px)}
.tp-close{background:var(--sunk);padding:110px 40px;text-align:center}
.tp-close h2{margin:0 0 26px;font-size:96px;line-height:.95}
.tp .tp-free{margin:18px 0 0;font-size:12px;color:var(--faint)}
.tp .tp-today{margin:0;padding:28px 40px 0;max-width:860px;font-size:15px;line-height:1.6;color:var(--faint)}
html.tp-js:not(.tp-still) .tp-ease{transition:opacity .7s ease,transform .7s ease}
html.tp-js:not(.tp-still) .tp-ease:not(.tp-in){opacity:0;transform:translateY(18px)}
@media (max-width:860px){
.tp-wide{display:none}
.tp-narrow{display:inline}
.tp-home{position:sticky;background:var(--bg);border-bottom-color:var(--line)}
.tp-motion{width:44px;padding:0;color:var(--text)}
.tp-motion-word{display:none}
.tp-motion-icon{display:block}
.tp-hero{height:700px}
.tp-orbits{top:300px;transform:scale(.5)}
.tp-hero-text{top:240px;padding:0 16px}
.tp-hero h1{font-size:72px}
.tp-tagline{margin-top:14px;font-size:22px}
.tp-hero-phone{display:flex;flex-direction:column;gap:10px;position:absolute;left:16px;right:16px;bottom:24px;z-index:2}
.tp-hero-phone .tp-lead{margin:0 0 6px;max-width:none;font-size:16px;line-height:1.55;text-align:center;background:var(--sunk)}
.tp-hero-phone .tp-btn{padding:15px}
.tp-band{padding:12px 0}
.tp-ticker{font-size:11px}
.tp-head{padding:14px 16px;font-size:11px}
.tp-intro{padding:44px 16px 32px}
#honest .tp-intro{padding-bottom:12px}
.tp-intro h2,.tp-phone-words h2{font-size:36px;line-height:1.05}
.tp-intro p,.tp-phone-words p{font-size:16px;line-height:1.55}
.tp-cards{grid-template-columns:minmax(0,1fr)}
.tp-card{border-right:0}
.tp-card .tp-strip-label{padding:10px 16px;font-size:10px;border-bottom:0}
.tp-card-pic{height:200px}
.tp-card-text{padding:16px 16px 22px}
.tp-card h3,.tp-point h3{font-size:20px;margin-bottom:6px}
.tp-card p,.tp-point p{font-size:15px;line-height:1.5}
.tp-points{grid-template-columns:minmax(0,1fr);border-top:0}
.tp-point{padding:22px 16px;border-right:0;border-bottom:1px solid var(--line)}
.tp .tp-point .tp-letter{font-size:12px;margin-bottom:8px}
.tp-phone-row{padding:44px 16px;gap:24px}
.tp-phone-words{flex-basis:100%}
.tp-phone-words h2{font-size:32px}
.tp-phones{flex-basis:100%;gap:14px;padding-bottom:18px}
.tp-phone-shot{height:340px;border-radius:18px;border-width:4px;max-width:calc(50% - 7px);object-fit:contain}
.tp-phone-shot.tp-low{transform:translateY(18px)}
.tp-third{display:none !important}
.tp-close{padding:60px 16px}
.tp-close h2{font-size:52px;margin-bottom:20px}
.tp-close .tp-btn{width:100%;padding:15px}
.tp .tp-today{padding:20px 16px 0}
}
@media (prefers-reduced-motion: reduce){
.tp-card-img,.tp-home{transition:none}
html.tp-js .tp-ease{transition:none}
}
"""

HOME_JS = """// NOTES-FROM-PLANNING.md entry 440: the concept B home page's motion. Without this script the page is complete and still: nothing turns,
// the strip of features wraps instead of scrolling, and every section is shown. Pause motion, or prefers-reduced-motion, gives the same.
(() => {
  const root = document.documentElement;
  root.classList.add("tp-js");
  const KEY = "grouplab-motion";
  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)");
  let paused = false;
  try { paused = localStorage.getItem(KEY) === "paused"; } catch (_) {}
  const still = () => paused || reduced.matches;
  const header = document.querySelector(".tp-header");
  const h1 = document.querySelector(".tp-hero h1");
  const word = document.querySelector(".tp-word");
  const mark = document.querySelector(".tp-ruler-mark");
  const button = document.querySelector(".tp-motion");

  // The wordmark docks: as the hero scrolls away the big GROUPLAB shrinks and moves to where the header's small one is, then hands over.
  let from = null;
  const measure = () => {
    h1.style.transform = "";
    const a = h1.getBoundingClientRect(), b = word.getBoundingClientRect();
    from = { x: a.left, y: a.top + window.scrollY, w: a.width, bx: b.left, by: b.top, bw: b.width };
  };
  const dock = () => {
    if (!h1 || !word) return;
    // Entry 448: while the menu is open the wordmark waits where it is, so nothing flies over the open panel.
    if (header && header.classList.contains("tp-menu-open")) return;
    const y = window.scrollY;
    if (still()) {
      h1.style.transform = ""; h1.style.opacity = ""; word.style.opacity = "";
      if (header) header.classList.toggle("tp-solid", y > 8);
      return;
    }
    if (!from) measure();
    const p = Math.min(1, Math.max(0, y / Math.max(1, from.y - from.by)));
    // The header fills in only once the wordmark has arrived, so it never covers the wordmark on its way up.
    if (header) header.classList.toggle("tp-solid", p >= 1);
    const s = 1 + (from.bw / from.w - 1) * p;
    const tx = (from.bx - from.x) * p, ty = y + (from.by - from.y) * p;
    h1.style.transform = "translate(" + tx + "px," + ty + "px) scale(" + s + ")";
    h1.style.opacity = p >= 1 ? "0" : "1";
    word.style.opacity = p >= 1 ? "1" : "0";
  };
  // The ruler's amber line is how far down the page the reader is.
  const ruler = () => {
    if (!mark) return;
    const room = document.documentElement.scrollHeight - window.innerHeight;
    const p = room > 0 ? Math.min(1, window.scrollY / room) : 0;
    mark.style.transform = "translateY(" + Math.round(p * (window.innerHeight - 2)) + "px)";
  };
  let queued = false;
  const frame = () => { queued = false; dock(); ruler(); };
  const later = () => { if (!queued) { queued = true; requestAnimationFrame(frame); } };
  window.addEventListener("scroll", later, { passive: true });
  window.addEventListener("resize", () => { from = null; later(); });
  for (const menu of document.querySelectorAll("details.tp-menu")) menu.addEventListener("toggle", later);

  const apply = () => {
    root.classList.toggle("tp-still", still());
    if (button) {
      const words = paused ? "Play motion" : "Pause motion";
      button.setAttribute("aria-pressed", String(paused));
      button.setAttribute("aria-label", words);
      const w = button.querySelector(".tp-motion-word");
      if (w) w.textContent = words.toUpperCase();
    }
    from = null;
    later();
  };
  if (button) button.addEventListener("click", () => {
    paused = !paused;
    try { localStorage.setItem(KEY, paused ? "paused" : "moving"); } catch (_) {}
    apply();
  });
  if (reduced.addEventListener) reduced.addEventListener("change", apply);
  apply();

  // The section index marks the section in view; its links jump there as any link does.
  const index = [...document.querySelectorAll(".tp-index a")];
  const eases = [...document.querySelectorAll(".tp-ease")];
  if (!("IntersectionObserver" in window)) { for (const e of eases) e.classList.add("tp-in"); return; }
  const here = (id) => { for (const a of index) a.classList.toggle("tp-here", a.hash === "#" + id); };
  here(index.length ? index[0].hash.slice(1) : "");
  const watch = new IntersectionObserver((seen) => {
    for (const s of seen) if (s.isIntersecting) here(s.target.id);
  }, { rootMargin: "-45% 0px -50% 0px" });
  for (const a of index) { const s = document.getElementById(a.hash.slice(1)); if (s) watch.observe(s); }
  // Each section's heading and cards ease in as they arrive, once.
  const arrive = new IntersectionObserver((seen) => {
    for (const s of seen) if (s.isIntersecting) { s.target.classList.add("tp-in"); arrive.unobserve(s.target); }
  }, { rootMargin: "0px 0px -8% 0px" });
  for (const e of eases) arrive.observe(e);
})();
"""


PAGE_JS = """// NOTES-FROM-PLANNING.md entry 444: the long pages' ruler and section index, the home page's, following the scroll. Nothing moves on its
// own, so reduced motion changes nothing here; without this script the ruler has no numbers and the index marks nothing, and the page is complete.
(() => {
  const ruler = document.querySelector(".tp-ruler"), mark = document.querySelector(".tp-ruler-mark");
  const nav = document.querySelector(".tp-index"), index = nav ? [...nav.querySelectorAll("a")] : [];
  // The section in view is the last whose heading has passed a third of the way down; a long index keeps it in sight without moving the page.
  const heads = index.map((a) => document.getElementById(a.hash.slice(1)));
  let current = null;
  const here = () => {
    let at = 0;
    heads.forEach((h, i) => { if (h && h.getBoundingClientRect().top < window.innerHeight / 3) at = i; });
    const a = index[at];
    if (!a || a === current) return;
    if (current) current.classList.remove("tp-here");
    current = a; a.classList.add("tp-here");
    if (a.offsetTop < nav.scrollTop || a.offsetTop + a.offsetHeight > nav.scrollTop + nav.clientHeight) nav.scrollTop = a.offsetTop - nav.clientHeight / 2;
  };
  let queued = false;
  const frame = () => {
    queued = false;
    if (index.length) here();
    if (!mark) return;
    const room = document.documentElement.scrollHeight - window.innerHeight;
    const p = room > 0 ? Math.min(1, window.scrollY / room) : 0;
    mark.style.transform = "translateY(" + Math.round(p * (window.innerHeight - 2)) + "px)";
  };
  const later = () => { if (!queued) { queued = true; requestAnimationFrame(frame); } };
  window.addEventListener("scroll", later, { passive: true });
  window.addEventListener("resize", later);
  later();
})();
"""


def served_js(js: str) -> str:
    """Entry 444: a script as it is served, without its whole-line comments and indentation, as served_css does for a stylesheet. Only
    for scripts with no template literal, whose lines a stripped indent could change."""
    assert "`" not in js
    js = re.sub(r"^\s*/\*.*?\*/\s*$", "", js, flags=re.S | re.M)
    return "\n".join(line.strip() for line in js.splitlines() if line.strip() and not line.strip().startswith("//")) + "\n"


def served_css(css: str) -> str:
    """Entry 443: a stylesheet as it is served, without the comments that explain it here, and without blank lines. Every page loads it,
    so what only a reader of this file needs is not sent to every visitor."""
    css = re.sub(r"/\*.*?\*/", "", css, flags=re.S)
    return "\n".join(line for line in css.splitlines() if line.strip()) + "\n"


CSS = r"""
@font-face{font-family:"IBM Plex Sans";font-weight:400;font-style:normal;font-display:swap;src:url(/assets/fonts/plex-sans-400.woff2) format("woff2")}
@font-face{font-family:"IBM Plex Sans";font-weight:500;font-style:normal;font-display:swap;src:url(/assets/fonts/plex-sans-500.woff2) format("woff2")}
@font-face{font-family:"IBM Plex Sans";font-weight:600;font-style:normal;font-display:swap;src:url(/assets/fonts/plex-sans-600.woff2) format("woff2")}
@font-face{font-family:"IBM Plex Sans Condensed";font-weight:700;font-style:normal;font-display:swap;src:url(/assets/fonts/plex-sans-condensed-700.woff2) format("woff2")}
@font-face{font-family:"IBM Plex Mono";font-weight:400;font-style:normal;font-display:swap;src:url(/assets/fonts/plex-mono-400.woff2) format("woff2")}
@font-face{font-family:"IBM Plex Mono";font-weight:500;font-style:normal;font-display:swap;src:url(/assets/fonts/plex-mono-500.woff2) format("woff2")}

/* The application's tokens (Tokens.cs): dark is the base, light follows the system or the toggle. */
:root{
--bg:#131417;--panel:#1a1c20;--panel2:#212429;--sunk:#0f1013;--line:#2c3037;--line2:#3a3f47;
--text:#e6e8ea;--dim:#9aa1a9;--faint:#858b92;--amber:#e0912f;--teal:#6fbfa8;--on-amber:#17120a;
--amber-tint:#221c12;--amber-tint-b:#3a2d18;--teal-tint:#141f1c;--teal-tint-b:#2c463f;--focus:#e0912f;
--shadow:0 24px 60px rgba(0,0,0,.35);color-scheme:dark}
@media (prefers-color-scheme: light){:root:not([data-theme="dark"]){
--bg:#f4f3f0;--panel:#ffffff;--panel2:#eceae4;--sunk:#e4e2dd;--line:#d3d0c9;--line2:#bdb9b0;
--text:#1a1c20;--dim:#5a6068;--faint:#666a70;--amber:#965d12;--teal:#367462;--on-amber:#ffffff;
--amber-tint:#f4efe7;--amber-tint-b:#d0b694;--teal-tint:#ebf1ef;--teal-tint-b:#a5c0b8;--focus:#965d12;
--shadow:0 20px 50px rgba(40,30,10,.12);color-scheme:light}}
:root[data-theme="light"]{
--bg:#f4f3f0;--panel:#ffffff;--panel2:#eceae4;--sunk:#e4e2dd;--line:#d3d0c9;--line2:#bdb9b0;
--text:#1a1c20;--dim:#5a6068;--faint:#666a70;--amber:#965d12;--teal:#367462;--on-amber:#ffffff;
--amber-tint:#f4efe7;--amber-tint-b:#d0b694;--teal-tint:#ebf1ef;--teal-tint-b:#a5c0b8;--focus:#965d12;
--shadow:0 20px 50px rgba(40,30,10,.12);color-scheme:light}

.only-light{display:none !important}
@media (prefers-color-scheme: light){:root:not([data-theme="dark"]) .only-light{display:block !important}:root:not([data-theme="dark"]) .only-dark{display:none !important}}
:root[data-theme="light"] .only-light{display:block !important}
:root[data-theme="light"] .only-dark{display:none !important}

*,*::before,*::after{box-sizing:border-box}
html{-webkit-text-size-adjust:100%;scroll-padding-top:88px}
body{margin:0;background:var(--bg);color:var(--text);font-family:"IBM Plex Sans",system-ui,-apple-system,"Segoe UI",sans-serif;font-size:17px;line-height:1.6;-webkit-font-smoothing:antialiased}
img{max-width:100%;height:auto}
a{color:var(--amber);text-decoration:none;font-weight:500}
a:hover{text-decoration:underline}
:focus-visible{outline:2px solid var(--focus);outline-offset:3px;border-radius:2px}
code,.mono{font-family:"IBM Plex Mono",ui-monospace,Consolas,monospace}
code{font-size:.88em;background:var(--panel2);padding:1px 6px;border-radius:3px}
a.term{color:inherit;font-weight:inherit;text-decoration:underline dotted;text-underline-offset:3px;cursor:help}
a.term:hover{text-decoration:underline dotted}
.term-pop{position:absolute;z-index:20;background:var(--panel);color:var(--text);border:1px solid var(--line2);border-radius:6px;padding:12px 14px;box-shadow:0 6px 24px rgba(0,0,0,.25);font-size:.92rem;line-height:1.5}
.term-pop p{margin:0 0 8px}
.term-pop-row{display:flex;justify-content:space-between;align-items:center;gap:12px}
.term-pop button{background:none;border:1px solid var(--line2);color:var(--dim);border-radius:4px;padding:2px 10px;font:inherit;cursor:pointer}
.glossary-entry{scroll-margin-top:80px;border-bottom:1px solid var(--line);padding:6px 0 10px}
h1,h2{font-family:"IBM Plex Sans Condensed","IBM Plex Sans",sans-serif;font-weight:700;letter-spacing:0;line-height:1.08;margin:0;color:var(--text)}
h1{font-size:clamp(44px,5.6vw,80px);line-height:1}
h2{font-size:clamp(30px,3.4vw,46px);line-height:1.05}
h3,.h3{font-family:"IBM Plex Sans",sans-serif;font-size:19px;font-weight:600;letter-spacing:0;line-height:1.3;margin:0}
p{margin:0;color:var(--dim)}
p.text,.text p,.text{color:var(--text)}
.dim{color:var(--dim)}.faint{color:var(--faint)}.teal{color:var(--teal)}.amber{color:var(--amber)}
.small{font-size:13px}.caps{letter-spacing:.1em;text-transform:uppercase}
.wrap{max-width:1280px;margin:0 auto;padding-left:40px;padding-right:40px}
.skip{position:absolute;left:-9999px;top:8px;background:var(--amber);color:var(--on-amber);padding:10px 14px;border-radius:4px;z-index:10}
.skip:focus{left:16px}

.page-top{padding-top:96px}
.note.note-figure{display:grid;grid-template-columns:auto minmax(0,1fr);align-items:start;row-gap:16px}
.note-figure>*{min-width:0}
.note-figure .shot-link{grid-column:1 / -1}
.note-figure .shot-link img{max-width:100%}
.menu-panel{position:absolute;right:0;top:52px;min-width:220px;display:flex;flex-direction:column;background:var(--panel);border:1px solid var(--line2);border-radius:6px;padding:8px;box-shadow:var(--shadow)}
.menu-panel a{padding:12px 14px;color:var(--text);border-radius:4px}
.menu-panel a:hover{background:var(--panel2);text-decoration:none}
.menu-panel a[aria-current="page"]{color:var(--amber)}

/* Entry 443: one look for the whole site, the home page's. The header and footer every page shares, its type and its buttons. */
.tp{--ring:#6b727b}
@media (prefers-color-scheme: light){:root:not([data-theme="dark"]) .tp{--ring:#8f8b83}}
:root[data-theme="light"] .tp{--ring:#8f8b83}
/* Entry 444: every heading but an h3 is the home page's display face, IBM Plex Sans Condensed bold; the home page's own section headings
   keep the plain face they were designed in. heading_problems fails the build on a page whose h1 is not the display face. */
.tp h1,.tp h2{font-family:"IBM Plex Sans Condensed","IBM Plex Sans",sans-serif;font-weight:700}
.tp h3,.tp .h3,.tp-landing h2{font-family:"IBM Plex Sans",system-ui,sans-serif;letter-spacing:normal;text-transform:none;font-weight:600}
.tp a{font-weight:400}
.tp .cond{font-family:"IBM Plex Sans Condensed","IBM Plex Sans",sans-serif;font-weight:700}
.tp-amber{color:var(--amber)}
.tp-ring-line{fill:none;stroke:var(--ring);stroke-width:6}
.tp-hit{fill:var(--amber)}
.tp-header{position:sticky;top:0;z-index:6;display:flex;align-items:center;gap:28px;padding:16px 40px;background:var(--bg);border-bottom:1px solid var(--line)}
.tp-brand{display:flex;align-items:center;gap:10px;font-size:18px;letter-spacing:.04em;color:var(--text)}
.tp-brand:hover{text-decoration:none}
.tp-word{display:inline-block;transform-origin:0 0}
.tp-nav{display:flex;align-items:center;gap:22px;margin-left:auto;font-size:12px;letter-spacing:.08em;text-transform:uppercase}
.tp-nav a{color:var(--dim);white-space:nowrap;padding:6px 0;border-bottom:2px solid transparent}
.tp-nav a:hover{color:var(--text);text-decoration:none}
.tp-nav a[aria-current="page"]{color:var(--text);border-bottom-color:var(--amber)}
.tp-gh{display:flex;align-items:center;gap:8px}
.tp-button{height:36px;padding:0 12px;border-radius:8px;border:1px solid var(--line2);background:transparent;color:var(--dim);font-size:11px;
letter-spacing:.06em;cursor:pointer;display:flex;align-items:center;justify-content:center;gap:8px}
.tp-button:hover{color:var(--text);border-color:var(--dim)}
.tp-square{width:36px;padding:0;color:var(--text)}
.tp-dl{height:36px;display:flex;align-items:center;padding:0 16px;border-radius:8px;background:var(--text);color:var(--bg);font-size:12px;letter-spacing:.08em}
.tp-dl:hover{text-decoration:none;opacity:.9}
.tp-menu{display:none;position:relative}
.tp-menu summary{list-style:none}
.tp-menu summary::-webkit-details-marker{display:none}
.menu-word{display:none;font-size:11px;letter-spacing:.08em;text-transform:uppercase}
.tp-footer{border-top:1px solid var(--line);margin-top:96px;padding:36px 40px;display:flex;justify-content:space-between;flex-wrap:wrap;gap:24px 48px;font-size:14px;color:var(--faint)}
.tp-footer-about{display:flex;flex-direction:column;gap:12px;max-width:440px}
.tp-footer-about p{margin:0;font-size:14px;color:var(--faint)}
.tp-footer-links{display:grid;grid-template-columns:repeat(2,minmax(0,auto));column-gap:48px;row-gap:8px;align-content:start}
.tp-footer-links a{color:var(--dim)}
.tp-footer-links a:hover{color:var(--text)}
.tp-copy{flex-basis:100%;font-size:13px}
/* Entry 444 section 2: all ten links in the bar from 1280 pixels, tighter below 1500 so they fit; More only from 981 to 1279. */
@media (max-width:1499px){.tp-header{gap:18px;padding:16px 28px}.tp-nav{gap:15px;font-size:11px;letter-spacing:.05em}}
@media (max-width:1279px) and (min-width:981px){
.tp-nav .nav-more{display:none}
.tp-menu{display:block}
.tp-menu summary{width:auto;padding:0 12px}
.menu-word{display:inline}
.menu-panel .menu-main{display:none}
}
@media (max-width:1100px){.tp-header{gap:16px;padding:14px 24px}.tp-nav{gap:16px}}
@media (max-width:980px){.tp-nav{display:none}.tp-menu{display:block;margin-left:auto}}
@media (max-width:860px){
.tp-header{padding:10px 16px;gap:8px}
.tp-brand{font-size:17px}
.tp-dl{display:none}
.tp-square,.tp-menu summary{width:44px;height:44px}
.tp-menu .menu-panel{top:52px}
.tp-footer{padding:26px 16px;margin-top:64px}
.tp-footer-links{grid-template-columns:repeat(2,minmax(0,1fr));column-gap:24px}
}

/* Entry 444 section 1: the home page's identity on every other page. Its rings, still and faint, behind the top band; the band in the
   hero's order (mono label, condensed title, lead, buttons); the ruler and section index on the long pages; cards in its hairline grid. */
.tp-rings{position:absolute;top:0;left:0;right:var(--rule,0px);height:760px;z-index:-1;overflow:hidden;pointer-events:none;background:var(--sunk);
-webkit-mask-image:linear-gradient(#000 40%,transparent);mask-image:linear-gradient(#000 40%,transparent)}
.tp-rings svg{position:absolute;left:calc(88% - 800px);top:-500px}
.tp-rings circle{fill:none;stroke:var(--line);stroke-width:1}
.tp-rings .tp-dash{stroke-dasharray:6 6;opacity:.7}
.page-head .eyebrow{font-size:12px;color:var(--faint)}
:is(.page-head,.page-top,.prose)>.eyebrow:first-child::before,.page-head>div>.eyebrow:first-child::before{content:"";display:inline-block;width:18px;height:2px;margin-right:12px;vertical-align:middle;background:var(--amber)}
.page-head h1,.page-top h1{max-width:1040px}
main>.guide:first-child{padding-top:88px}
.page-head .lead{max-width:640px}
.tp-landing,.tp-ruled{--rule:46px}
.tp-ruled main,.tp-ruled .tp-footer,.tp-ruled .tp-header{margin-right:var(--rule)}
.tp-ruler{position:fixed;top:0;right:0;bottom:0;width:var(--rule);border-left:1px solid var(--line);z-index:3;overflow:hidden;
background-image:linear-gradient(to bottom,var(--line2) 0 1px,transparent 1px),linear-gradient(to bottom,var(--line2) 0 1px,transparent 1px);
background-size:9px 40px,18px 200px;background-repeat:repeat-y;background-position:right 0 top 20px,right 0 top 20px}
.tp-tick-n{position:absolute;right:22px;font-size:10px;color:var(--faint)}
.tp-ruler-mark{position:absolute;right:0;top:0;width:var(--rule);height:2px;background:var(--amber);transform:translateY(0)}
.tp-index{position:fixed;right:calc(var(--rule) + 24px);bottom:24px;z-index:4;display:flex;flex-direction:column;align-items:flex-end;
font-size:12px;letter-spacing:.1em;line-height:1.7;padding:8px 10px;border-radius:6px;background:var(--bg)}
.tp-index a{color:var(--faint)}
.tp-index a:hover{color:var(--text);text-decoration:none}
.tp-index a.tp-here{color:var(--text)}
.tp-index a span:first-child{color:var(--amber)}
.panel .h3,.panel h3,.panel .h4{font-size:21px;font-weight:600}
:where(.features,.research-grid,.grid-3,.donor-sheets):not(.wrap):has(>.panel){gap:0;border-top:1px solid var(--line);border-left:1px solid var(--line)}
:where(.features,.research-grid,.grid-3,.donor-sheets):not(.wrap)>.panel{border-width:0 1px 1px 0}
a.panel:hover,.research-card:hover{border-color:var(--line);background:var(--panel2);text-decoration:none}
.research-card{padding:0;gap:0}
.research-card>.research-thumb,.research-card>.research-plate,.research-card>picture img{margin:0;border:0;border-radius:0}
.research-card>h3,.research-card>p{padding:0 22px}
.research-card>h3{padding-top:20px;margin-bottom:8px}
.research-card>p{padding-bottom:24px}
.prose ul>li::marker{color:var(--amber)}
.prose ol>li::marker{color:var(--amber);font-family:"IBM Plex Mono",monospace;font-size:.9em}
pre{margin:0;padding:16px 18px;background:var(--sunk);border:1px solid var(--line);border-left:2px solid var(--amber);overflow-x:auto;font-size:14px;line-height:1.55}
pre code{background:none;padding:0;font-size:inherit}
.prose>.eyebrow{font-size:12px;line-height:1.6;color:var(--faint)}
.hiw-tour-card{display:flex;flex-wrap:wrap;gap:16px;align-items:center;justify-content:space-between}
@media (max-width:860px){
.tp-landing,.tp-ruled{--rule:0px}
.tp-ruler,.tp-index{display:none}
.tp-rings{height:560px}
.tp-rings svg{left:calc(70% - 800px);top:-620px}
}

/* type blocks */
.eyebrow{font-family:"IBM Plex Mono",monospace;font-size:12px;letter-spacing:.1em;text-transform:uppercase;color:var(--dim);font-weight:400}
.eyebrow.amber{color:var(--amber)}
.lead{font-size:clamp(17px,1.6vw,20px);line-height:1.6;max-width:780px}
.stack{display:flex;flex-direction:column;gap:20px}
.stack.tight{gap:12px}

/* buttons */
.actions{display:flex;flex-wrap:wrap;gap:14px;align-items:center}
.actions.col{flex-direction:column;align-items:flex-start}
.btn{display:inline-flex;flex-direction:column;justify-content:center;gap:4px;min-height:48px;padding:14px 22px;border-radius:8px;border:1px solid var(--line2);background:var(--bg);color:var(--text);font-family:"IBM Plex Mono",monospace;font-size:13px;font-weight:400;letter-spacing:.08em;text-transform:uppercase;line-height:1.3}
.btn:hover{text-decoration:none}
.btn-big{padding:14px 22px;font-size:13px}
.btn-primary{background:var(--amber);border-color:var(--amber);color:var(--on-amber)}
.btn-primary:hover{opacity:.92}
.btn-secondary{color:var(--text)}
.btn-secondary:hover{border-color:var(--text)}
.btn-sub{font-family:"IBM Plex Sans",sans-serif;font-size:12px;font-weight:400;letter-spacing:normal;text-transform:none;opacity:.85}

/* panels */
.panel{background:var(--panel);border:1px solid var(--line);border-radius:0}
.pad{padding:28px}
.card-rec{border-color:var(--amber)}
.note{display:flex;gap:16px;align-items:baseline;padding:18px 24px;border-radius:0}
.note-teal{background:var(--teal-tint);border:1px solid var(--teal-tint-b)}
.note-teal .mono{color:var(--teal);font-size:13px;flex-shrink:0}
.note p{color:var(--text);font-size:15px}
.callout{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:64px;row-gap:24px;align-items:center;padding:48px 56px;background:var(--amber-tint);border:1px solid var(--amber-tint-b);border-radius:0}
.callout h2{font-size:clamp(26px,3vw,36px)}
.small-callout{display:flex;flex-direction:column;align-items:flex-start;gap:16px;padding:28px}

/* layout */
.section{padding-top:120px;display:flex;flex-direction:column;gap:40px}
.section-sm{padding-top:40px}
.last{padding-bottom:120px}
.page-head{padding-top:96px;padding-bottom:48px;display:flex;flex-direction:column;gap:20px}
.two-col{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:80px;row-gap:40px;align-items:start}
.two-col.bottom{align-items:end}
.grid-2{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:32px}
.grid-3{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}
.row-between{display:flex;justify-content:space-between;align-items:center;gap:24px}
.row-between a{flex-shrink:0}

/* screenshots */
.shot{display:block;width:100%;border:1px solid var(--line2);border-radius:6px;box-shadow:var(--shadow)}
.status{padding:32px;display:flex;flex-direction:column;gap:16px;align-self:start}
.status p{font-size:15px}

/* download */
.card{padding:28px;display:flex;flex-direction:column;gap:14px}
.card-head{display:flex;justify-content:space-between;align-items:center;gap:12px}
.card-head .h3{font-size:20px}
.card p{font-size:15px}
.badge{font-size:11px;letter-spacing:.1em;text-transform:uppercase;padding:4px 8px;border-radius:0;background:var(--amber-tint);border:1px solid var(--amber-tint-b);color:var(--amber)}
.small-list{margin:0;padding-left:18px;font-size:14px;line-height:1.5}
.small-list li{margin-bottom:6px}
.panel ol{margin:0;padding-left:20px;font-size:15px}
.panel.pad{display:flex;flex-direction:column;gap:16px}
.panel.pad p{font-size:15px}
.facts{margin:0;display:flex;flex-direction:column;gap:14px}
.facts div{display:flex;gap:16px}
.facts dt{width:110px;flex-shrink:0;font-size:13px;color:var(--teal);padding-top:2px}
.facts dd{margin:0;font-size:15px;color:var(--text)}

/* shoot a target */
.pdf-row{display:flex;gap:20px;align-items:center;padding:22px 24px}
.pdf-row .pdf{color:var(--dim);flex-shrink:0}
.pdf-row.card-rec .pdf{color:var(--amber)}
.pdf-text{display:flex;flex-direction:column;gap:4px;flex-grow:1;min-width:0}
.pdf-text strong{font-size:17px;font-weight:600}
.pdf-text span{font-size:14px;color:var(--dim)}
.step-list{list-style:none;margin:0;padding:0;display:flex;flex-direction:column}
.step{display:flex;gap:24px;padding:24px 0;border-top:1px solid var(--line)}
.step-n{font-size:28px;color:var(--amber);width:48px;flex-shrink:0;line-height:1}
.step div{display:flex;flex-direction:column;gap:8px}
.two-col.top{align-items:start}

/* guides */
.guide-bar{padding-top:56px;display:flex;align-items:center;gap:12px}
.tabs{display:flex;gap:8px;flex-wrap:wrap}
.tab{font-size:14px;padding:8px 14px;border-radius:0;color:var(--dim)}
.tab[aria-current="page"]{background:var(--panel2);color:var(--text);font-weight:600}
.tab:hover{text-decoration:none;color:var(--text)}
.pdf-link{margin-left:auto;font-size:14px}
/* Research, entry 142. The cards are a plain grid that collapses to one column on a phone, and the
   thumbnail is the article's lead chart, so the index reads as a contents page rather than a list. */
.research-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:16px;margin:12px 0 32px}
.research-card{display:block;transition:border-color .15s ease}
/* Entry 143 section 1.1: every card's lead is the same shape, whether it is a chart or a plate, so the row is
   even. Without this the one card with a real figure set the height of the whole row. */
.research-thumb{width:100%;aspect-ratio:320/200;object-fit:cover;border-radius:4px;background:var(--sunk);margin-bottom:8px}
.research-fig{max-width:100%;height:auto;border-radius:4px}
/* Entry 143 section 1.1: the lead for an article with no chart. Same shape as a thumbnail so the row is even,
   and drawn in the site's own colors so it follows the theme with no second image to keep in step. */
.research-plate{aspect-ratio:320/200;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;background:var(--sunk);border:1px solid var(--line);color:var(--faint)}
.plate-rule{width:42px;height:2px;background:var(--amber);opacity:.8}
.plate-number{font-size:30px;color:var(--dim);line-height:1}
.research-box{border-left:3px solid var(--amber);margin:20px 0 28px}

/* Entry 129: the send form. Plain, wide fields, and a honeypot nobody sees. */
.send-form{max-width:720px}
.send-terms ul{margin:4px 0 0;padding-left:20px}
.send-terms li{margin:0 0 8px}
.asks{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px;margin:8px 0 4px}
.ask{display:flex;flex-direction:column;gap:6px}
.ask label{font-size:14px;color:var(--dim)}
.ask input,.ask select,.ask textarea{font:inherit;font-size:15px;padding:10px 12px;color:var(--text);background:var(--panel);border:1px solid var(--line2);border-radius:4px;width:100%}
.ask textarea{resize:vertical}
.send-form input[type=file]{font:inherit;font-size:15px;padding:12px;color:var(--text);background:var(--panel);border:1px dashed var(--line2);border-radius:4px;width:100%}
.consent{margin:8px 0}
.tick{display:flex;gap:10px;align-items:flex-start;font-size:15px;line-height:1.5}
.tick input{margin-top:3px;flex:0 0 auto}
.hp{position:absolute;left:-9999px;width:1px;height:1px;overflow:hidden}
.cf-turnstile{margin:8px 0}

/* Entry 146: the tour. The picture carries each page, so it goes full width of the column with a little air
   under it, and the numbered list beneath is what a reader matches against it. */
.tour-shot{display:block;margin:8px 0 6px}
.tour-img{margin:0}
.tour-parts{margin:8px 0 24px;padding-left:22px}
.tour-parts li{margin:0 0 10px}
.tour-around{display:flex;flex-wrap:wrap;gap:20px;margin-top:36px;padding-top:16px;border-top:1px solid var(--line2);font-size:15px}
.research-box p{margin:0}
/* A flex child will not shrink below its content by default, and a four column table is wider than a
   phone, so the article held the page open and everything ran off the right edge. */
.research-article{min-width:0;max-width:100%}
.research-article img{max-width:100%;height:auto}
.research-article table{font-size:.94em;display:block;overflow-x:auto;max-width:100%}
.research-article pre{overflow-x:auto}
.guide{padding-top:32px;padding-bottom:120px;display:flex;gap:64px;align-items:flex-start}
.toc{position:sticky;top:104px;width:260px;flex-shrink:0;display:flex;flex-direction:column;gap:2px;max-height:calc(100vh - 128px);overflow:auto}
.toc a{display:block;padding:7px 0 7px 14px;font-size:14px;font-weight:400;color:var(--dim);border-left:2px solid var(--line)}
.toc a:hover{color:var(--text);border-left-color:var(--amber);text-decoration:none}
.toc p{margin-bottom:10px}
.toc ol{list-style:none;margin:0;padding:0;display:flex;flex-direction:column;gap:2px}
.toc ol ol{padding:2px 0 6px}
.toc ol ol a{padding:5px 0 5px 28px;font-size:13px}
.toc a.here{color:var(--text);border-left-color:var(--amber)}
.toc.js ol ol{overflow:hidden;max-height:0;visibility:hidden;transition:max-height .2s ease,visibility 0s linear .2s}
.toc.js li.open>ol{visibility:visible;transition:max-height .2s ease}
.toc-mini{display:none}
.dl-fold.toc-mini ol{list-style:none;padding-left:0}
.dl-fold.toc-mini ol ol{padding-left:16px;margin-bottom:6px}
.toc-mini a{display:block;padding:5px 0;color:var(--text)}
.dl-fold.toc-mini ol ol a{color:var(--dim)}
.prose h2,.prose h3{scroll-margin-top:16px}
/* Entry 445: the guides' contents beside any long page: the top band stays full width, everything after it moves beside the list. */
.toc-page{padding-top:0}
.toc-scope{flex:1 1 auto;min-width:0;display:flex;flex-direction:column}
.toc-scope>.wrap{max-width:none;margin:0;padding-left:0;padding-right:0}
.toc-scope [id]{scroll-margin-top:16px}
.toc-scope>.toc-mini{margin-bottom:24px}
.release summary h3{display:inline}
.prose h4.h3{margin-top:12px}
.hlink{margin-left:.4em;font-weight:400;color:var(--faint);opacity:0}
h2:hover>.hlink,h3:hover>.hlink,.hlink:focus-visible{opacity:1}
.hlink:hover{color:var(--amber);text-decoration:none}
.link-note{position:fixed;left:50%;bottom:24px;transform:translateX(-50%);z-index:6;margin:0;padding:8px 14px;font-size:14px;color:var(--text);background:var(--panel2);border:1px solid var(--line2);border-radius:6px;opacity:0;pointer-events:none;transition:opacity .15s}
.link-note.on{opacity:1}
@media (prefers-reduced-motion: reduce){.toc.js ol ol,.toc.js li.open>ol,.link-note{transition:none}}
.prose{flex-grow:1;min-width:0;max-width:780px;display:flex;flex-direction:column;gap:18px}
.prose h1{font-size:clamp(40px,4.6vw,60px);margin-bottom:6px}
.prose h2{font-size:34px;margin-top:28px}
.prose h3{margin-top:12px}
.prose p,.prose li{color:var(--text);font-size:17px;line-height:1.7}
.prose ul,.prose ol{margin:0;padding-left:24px}
.prose li{margin-bottom:6px}
.prose table{border-collapse:collapse;width:100%;font-size:15px;border-top:1px solid var(--line2)}
.prose th,.prose td{border-bottom:1px solid var(--line);padding:10px 12px;text-align:left;vertical-align:top}
.prose th{font-family:"IBM Plex Mono",monospace;font-size:12px;font-weight:400;letter-spacing:.08em;text-transform:uppercase;color:var(--faint);border-bottom-color:var(--line2)}
.guide-fig{margin:8px 0;display:flex;flex-direction:column;gap:10px}
.guide-fig img.phone-shot{width:auto;max-width:100%;max-height:560px;margin:0 auto;border-radius:14px}
.fig-marks{position:relative}
.marks{position:absolute;inset:1px;pointer-events:none}
.mark-box{position:absolute;border:2px solid var(--amber);border-radius:6px;box-shadow:0 0 0 1px var(--bg),inset 0 0 0 1px var(--bg);transition:border-width .12s}
.mark-box.hot{border-width:4px}
.mark-badge{position:absolute;width:22px;height:22px;margin:-11px 0 0 -11px;border-radius:50%;background:var(--amber);color:var(--on-amber);
font:600 13px/22px "IBM Plex Sans",sans-serif;text-align:center;box-shadow:0 0 0 1px var(--bg)}
.mark-ref{color:var(--amber);font-weight:600;cursor:default;border-radius:3px}
.primer-fig .pf{width:100%;height:auto;display:block;background:var(--panel);border:1px solid var(--line);border-radius:6px}
.pf-frame{fill:none}
.pf-ring{fill:none;stroke:var(--line2);stroke-width:1}
.pf-shot{fill:var(--text)}
.pf-center{stroke:var(--amber);stroke-width:3;fill:none}
.pf-center-ring{fill:none;stroke:var(--amber);stroke-width:2}
.pf-dashed{stroke-dasharray:6 5}
.pf-spoke{stroke:var(--faint);stroke-width:1}
.pf-es{stroke:var(--amber);stroke-width:2}
.pf-label{fill:var(--dim);font:13px "IBM Plex Mono",monospace}
.pf-bar{fill:var(--amber-tint-b)}
.pf-mid{stroke:var(--amber);stroke-width:3}
.pf-tick{stroke:var(--line2);stroke-width:1}
.pf-controls{display:flex;flex-wrap:wrap;align-items:center;gap:12px}
.pf-sizes{display:flex;gap:6px}
.pf-size{min-height:40px;padding:0 12px;border-radius:6px;border:1px solid var(--line2);background:transparent;color:var(--dim);font:inherit;cursor:pointer}
.pf-size[aria-pressed="true"]{color:var(--text);border-color:var(--amber)}
.pf-said{margin:0;flex-basis:100%;color:var(--dim)}
.math-fold summary{font-size:16px}
.primer-summary{margin:0}
.mark-ref:focus-visible{outline:2px solid var(--focus);outline-offset:1px}
@media (prefers-reduced-motion: reduce){.mark-box{transition:none}}
a.plain{color:var(--text)}

/* the features page and its spotlight, entry 242 */
.feature img.shot{border-radius:6px;border:1px solid var(--line)}
.feature img.phone-shot{width:auto;max-width:100%;max-height:420px;margin:0 auto;border-radius:14px}
[data-show="mobile"]{display:none}
:root[data-platform="mobile"] [data-show="desktop"]{display:none}
:root[data-platform="mobile"] [data-show="mobile"]{display:block}
.platform-switch{display:inline-flex;border:1px solid var(--line2);border-radius:4px;overflow:hidden;align-self:flex-start}
.platform-switch button{font:inherit;padding:8px 18px;background:transparent;border:0;color:var(--dim);cursor:pointer;min-height:44px}
.platform-switch button+button{border-left:1px solid var(--line2)}
:root:not([data-platform="mobile"]) .platform-switch [data-choose="desktop"],:root[data-platform="mobile"] .platform-switch [data-choose="mobile"]{background:var(--amber-tint);color:var(--amber)}
.tour-shot img.phone-shot{width:auto;max-width:100%;max-height:640px;margin:0 auto;border-radius:14px}
img.phone-thumb{object-fit:contain;background:var(--line)}
/* Entry 256: a bull, grid or sheet as it prints, on its own paper, so one picture reads in both themes. */
.sheet-pics{display:flex;flex-wrap:wrap;gap:12px;align-items:flex-end}
/* Entry 264: each donor sheet whole, on its own paper. */
.donor-sheet .donor-pic{width:120px;height:auto;background:#fff;border:1px solid var(--line);border-radius:4px}
.sheet-pic{margin:0;flex:1 1 140px;max-width:320px}
.sheet-pic img{width:100%;height:auto;background:#fff;border:1px solid var(--line);border-radius:4px;display:block}
.sheet-pic figcaption{margin-top:4px}
a.spot{color:inherit}
a.spot:hover{border-color:var(--amber);text-decoration:none}

/* the survey page, entry 241 */
.grid-3{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}
.survey-time{margin:0;font-size:22px}
.survey-bar{height:10px;background:var(--sunk);border:1px solid var(--line);border-radius:5px;overflow:hidden}
.survey-bar span{display:block;height:100%;background:var(--amber)}
.survey-row{display:flex;justify-content:space-between;margin:8px 0 4px}
.survey-stages{width:100%;border-collapse:collapse;font-size:14px}
.survey-stages td,.survey-stages th{padding:4px 0;border-bottom:1px solid var(--line);text-align:left}
.survey-stages .num{text-align:right}
@media (max-width:900px){.grid-3{grid-template-columns:1fr}}

/* Entry 338: the download page's folds, which the guides' contents lists use too; the rest of its styles are DOWNLOAD_CSS. */
.dl-fold{background:var(--panel);border:1px solid var(--line);border-radius:0}
.dl-fold>summary{display:flex;align-items:center;gap:12px;min-height:48px;padding:12px 18px;cursor:pointer;list-style:none;font-weight:600;color:var(--text)}
.dl-fold>summary::-webkit-details-marker{display:none}
.dl-fold>summary::before{content:"";flex-shrink:0;width:8px;height:8px;border-right:2px solid var(--dim);border-bottom:2px solid var(--dim);transform:rotate(-45deg)}
.dl-fold[open]>summary::before{transform:rotate(45deg)}
.dl-fold-body{padding:4px 18px 20px;display:flex;flex-direction:column;gap:14px}
.dl-fold-body p{font-size:15px}
.dl-fold ol{margin:0;padding-left:20px;font-size:15px}
@media (max-width:860px){
.dl-fold-body{padding:4px 14px 16px}
}
.pdf-row .btn{white-space:nowrap;flex-shrink:0}
.small-callout h2{font-family:"IBM Plex Sans",sans-serif;font-size:19px;letter-spacing:0}
.two-col.bottom{align-items:center}

/* narrower screens */
@media (max-width:1100px){
.wrap{padding-left:40px;padding-right:40px}
.grid-3{grid-template-columns:repeat(1,minmax(0,1fr))}
.toc{display:none}
.toc-mini{display:block}
}
@media (max-width:860px){
.wrap{padding-left:16px;padding-right:16px}
.section{padding-top:72px}
.last{padding-bottom:72px}
.page-head{padding-top:48px}
.page-top{padding-top:48px}
.note.note-figure{grid-template-columns:minmax(0,1fr)}
.two-col,.grid-2,.callout{grid-template-columns:repeat(1,minmax(0,1fr))}
.callout{padding:28px 24px}
.row-between{flex-direction:column;align-items:flex-start}
.pdf-row{flex-wrap:wrap}
.guide-bar{flex-wrap:wrap}
.pdf-link{margin-left:0}
.prose table{display:block;overflow-x:auto;max-width:100%}
.btn-big{width:100%}
}
@media (prefers-reduced-motion: reduce){*{scroll-behavior:auto !important}}
"""

JS = r"""/* GroupLab theme: follows the system unless the visitor has chosen, and remembers the choice. */
(function () {
  var KEY = "grouplab-theme";
  var root = document.documentElement;
  function stored() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
  function store(v) { try { localStorage.setItem(KEY, v); } catch (e) {} }
  var saved = stored();
  if (saved === "dark" || saved === "light") root.setAttribute("data-theme", saved);
  function current() {
    var t = root.getAttribute("data-theme");
    if (t) return t;
    return window.matchMedia && window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark";
  }
  document.addEventListener("DOMContentLoaded", function () {
    /* Entry 448: the header's menu says it is open on the header, for browsers without :has, and closes on Escape, on a tap outside it
       and on choosing a link. */
    var menus = document.querySelectorAll("details.tp-menu");
    for (var m = 0; m < menus.length; m++) {
      (function (menu) {
        var header = menu.closest(".tp-header");
        menu.addEventListener("toggle", function () { if (header) header.classList.toggle("tp-menu-open", menu.open); });
        menu.addEventListener("click", function (e) { if (e.target.closest && e.target.closest(".menu-panel a")) menu.open = false; });
        document.addEventListener("click", function (e) { if (menu.open && !menu.contains(e.target)) menu.open = false; });
        document.addEventListener("keydown", function (e) {
          if (e.key === "Escape" && menu.open) { menu.open = false; menu.querySelector("summary").focus(); }
        });
      })(menus[m]);
    }
    var buttons = document.querySelectorAll(".theme-toggle");
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].addEventListener("click", function () {
        var next = current() === "dark" ? "light" : "dark";
        root.setAttribute("data-theme", next);
        store(next);
      });
    }
  });
})();
/* Entry 249: Desktop or Mobile on the tour and the Features page. A link's ?platform= first, then the visitor's own last choice, kept in
   this browser and never sent anywhere, then Mobile on a phone or tablet and Desktop otherwise. */
(function () {
  var KEY = "grouplab-platform";
  var root = document.documentElement;
  function stored() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
  function store(v) { try { localStorage.setItem(KEY, v); } catch (e) {} }
  var ua = navigator.userAgent || "";
  var forced = /[?&]platform=(mobile|desktop)(?:&|$)/.exec(location.search);
  var saved = stored();
  // Entry 349: a tablet asking for the desktop site says Macintosh (an iPad) or "X11; Linux" (Android); a touch screen with no fine
  // pointer is handheld either way.
  var touchOnly = navigator.maxTouchPoints > 0 && !!window.matchMedia && window.matchMedia("(pointer: coarse)").matches
    && !window.matchMedia("(any-pointer: fine)").matches;
  // Entry 403: as the download page reads it, the browser's own platform or an ARM processor under an x86_64 user agent says Android
  // too, where a pen or a trackpad gives the tablet a fine pointer.
  var cpu = (navigator.platform || "") + " " + (navigator.oscpu || "");
  var disguised = !!(navigator.userAgentData && navigator.userAgentData.platform === "Android") || (/arm|aarch64/i.test(cpu) && /x86_64|amd64/i.test(ua));
  var handheld = /Android|iPhone|iPad|iPod|Mobile/i.test(ua) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(ua))
    || ((touchOnly || disguised) && /Linux|X11/.test(ua) && !/CrOS/.test(ua));
  root.setAttribute("data-platform", forced ? forced[1] : (saved === "mobile" || saved === "desktop") ? saved : (handheld ? "mobile" : "desktop"));
  function mark() {
    var now = root.getAttribute("data-platform");
    var buttons = document.querySelectorAll(".platform-switch [data-choose]");
    for (var i = 0; i < buttons.length; i++) buttons[i].setAttribute("aria-pressed", String(buttons[i].getAttribute("data-choose") === now));
  }
  document.addEventListener("DOMContentLoaded", function () {
    mark();
    var buttons = document.querySelectorAll(".platform-switch [data-choose]");
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].addEventListener("click", function (e) {
        var choice = e.currentTarget.getAttribute("data-choose");
        root.setAttribute("data-platform", choice);
        store(choice);
        mark();
      });
    }
  });
})();
"""

# The stylesheet and the script are fingerprinted by fingerprint() with everything else now,
# so there is no hand-rolled version to keep in step with them.

# ---------------------------------------------------------------- fingerprints and checks

# What gets a content hash in its URL. Entry 128 section 1.4: not only the stylesheet and the
# script, but the screenshots, the PDFs and the fonts too, so Cloudflare can never serve a
# stale file after a publish. A hash in the query string is enough; the file keeps its name,
# which matters for a PDF somebody saves.
FINGERPRINTED = {".woff2", ".webp", ".png", ".svg", ".pdf", ".zip", ".ico", ".css", ".js"}

# Referenced from a page or a stylesheet, so these are the ones worth rewriting.
REFERENCE = re.compile(r'(?P<url>/(?:assets|donor|guides)/[A-Za-z0-9._/-]+|/favicon\.(?:ico|svg)|/apple-touch-icon\.png)(?P<query>\?v=[A-Za-z0-9]+)?')


def fingerprint() -> None:
    """Puts a content hash on every asset URL, so a changed file is never served from a cache."""
    hashes: dict[str, str] = {}
    for f in OUT.rglob("*"):
        if f.is_file() and f.suffix in FINGERPRINTED:
            rel = "/" + f.relative_to(OUT).as_posix()
            hashes[rel] = hashlib.sha256(f.read_bytes()).hexdigest()[:10]

    def swap(m: re.Match) -> str:
        url = m.group("url")
        # The hash is of the file's own bytes, so an unchanged file keeps its URL across builds.
        return f"{url}?v={hashes[url]}" if url in hashes else m.group(0)

    for f in OUT.rglob("*"):
        if f.suffix in {".html", ".css", ".xml"}:
            text = f.read_text(encoding="utf-8")
            f.write_text(REFERENCE.sub(swap, text), encoding="utf-8", newline="\n")


def send_problems() -> list[str]:
    """Entry 173 section 3. The top bar shows "Send a target" only while the page is open, and the page is in the build whenever the
    link is. The old path answers with a page that links to the new one. The consent on the page is the one in limits.json, and the
    page says in plain words what happens to a photograph."""
    problems = []
    is_open = bool(limits().get("open"))
    page = OUT / "targets" / "index.html"
    for f in OUT.rglob("*.html"):
        text = f.read_text(encoding="utf-8")
        linked = f'href="{SEND}"' in text and ">Send a target</a>" in text
        if linked and not is_open:
            problems.append(f"{f.relative_to(OUT)}: the top bar offers Send a target while the page is closed")
        if is_open and ">Shoot a target</a>" in text.split("</header>")[0] and "<header" in text:
            problems.append(f"{f.relative_to(OUT)}: the top bar still says Shoot a target while the send page is open")
    if is_open:
        if not page.exists():
            problems.append("targets/index.html: the top bar links to it and it is not in the build")
        else:
            text = page.read_text(encoding="utf-8")
            for level, words in limits()["consentTexts"].items():
                if esc(words) not in text:
                    problems.append(f"targets/index.html: the {level} consent text is not the one in limits.json")
            for words in ["rebuilt from its pixels", "Location, GPS and the date and time are not", "as soon as they have been checked and copied", "deleted from the server", "private archive"]:
                if words not in text:
                    problems.append(f"targets/index.html: does not say {words!r}")
        # Entry 174: PHP builds the per-file arrays the receiver reads only for a field whose name ends in [], so a file input named
        # "photos" had every submission refused as having no photos. The name is read out of the page as built, not out of this source.
        names = re.findall(r'<input type="file"[^>]* name="([^"]+)"', page.read_text(encoding="utf-8")) if page.exists() else []
        if not names or not all(n.endswith("[]") for n in names):
            problems.append(f"targets/index.html: the file input is named {names}, and PHP needs a name ending in [] to receive several files")
        moved = OUT / "shoot-a-target" / "send" / "index.html"
        if not moved.exists() or f'href="{SEND}"' not in moved.read_text(encoding="utf-8"):
            problems.append("shoot-a-target/send/index.html: the old address does not answer with a link to /targets/")
    elif page.exists():
        problems.append("targets/index.html: built while the page is closed")
    return problems


def link_problems() -> list[str]:
    """
    Entry 128 section 1.2. Two ways the Download page could quietly stop working, both of which
    have happened to this project already.

    A link to releases/latest points at whatever GitHub calls latest, which is a numbered
    release nobody has made yet, so it 404s. A link to v0.1.0 points at a draft. And the
    download buttons must use the rolling nightly's stable names, because a versioned name is
    right for about a day and then serves nothing.
    """
    problems = []
    pages = [f for f in OUT.rglob("*.html")]

    for f in pages:
        text = f.read_text(encoding="utf-8")
        for bad in ["releases/latest", "/v0.1.0"]:
            if bad in text:
                problems.append(f"{f.relative_to(OUT)}: links to {bad}, which does not serve a build")

    download = OUT / "download" / "index.html"
    if not download.exists():
        return problems + ["download/index.html is missing"]

    text = download.read_text(encoding="utf-8")
    for asset in NIGHTLY_ASSETS:
        wanted = f"releases/download/nightly/{asset}"
        if wanted not in text:
            problems.append(f"download/index.html does not offer {wanted}, so its buttons will rot")

    return problems


# Entry 129 section 3.3: PHP runs only for the receivers, and nginx is configured to refuse a .php
# request anywhere else. This is the other half of that: the built site may not contain a .php file
# that is not a receiver, so there is nothing else for a misconfiguration to execute.
# ---------------------------------------------------------------- the send form's script


# Entry 129 section 1.3: the page reads without JavaScript and says plainly that sending needs it, because the
# check that proves you are a person does. This posts the form rather than letting the browser navigate, so the
# result can be said on the page instead of as a page of JSON.
SURVEY_JS = """// NOTES-FROM-PLANNING.md entry 241 section 5: the survey page's figures, from the aggregate the survey worker publishes beside the page.
// Counts and medians only. A group the worker has not published is left saying so.
(function () {
  function bars(id, title, counts) {
    var box = document.getElementById(id), names = Object.keys(counts || {});
    if (!box || names.length === 0) { return; }
    var most = Math.max.apply(null, names.map(function (n) { return counts[n]; }));
    var html = '<h3 class="h4">' + title + '</h3>';
    names.sort(function (a, b) { return counts[b] - counts[a]; }).forEach(function (n) {
      var w = Math.max(4, Math.round(100 * counts[n] / most));
      html += '<p class="small survey-row"><span>' + n.replace(/[<>&]/g, '') + '</span><span class="mono">' + counts[n] + '</span></p>'
        + '<div class="survey-bar"><span style="width:' + w + '%"></span></div>';
    });
    box.innerHTML = html;
  }
  fetch('/survey/aggregate.json', { cache: 'no-store' }).then(function (r) { return r.ok ? r.json() : null; }).then(function (a) {
    if (!a) { return; }
    var range = document.getElementById('survey-range');
    if (range && a.from) {
      range.textContent = a.reports + ' reports from ' + a.machines + ' machines, ' + a.from + ' to ' + a.to + '. Updated every hour.';
    }
    bars('survey-platforms', 'Operating systems', a.platforms);
    bars('survey-memory', 'Memory', a.memory);
    var box = document.getElementById('survey-benchmark');
    if (box && a.benchmark && a.benchmark.length) {
      var html = '<table class="survey-stages"><tr><th>Platform</th><th>Cores</th><th>Machines</th><th>Median</th></tr>';
      a.benchmark.forEach(function (b) {
        html += '<tr><td>' + b.platform + '</td><td>' + b.cores + '</td><td class="mono num">' + b.machines + '</td><td class="mono num">'
          + (b.medianMilliseconds / 1000).toFixed(1) + ' s</td></tr>';
      });
      var lo = Math.min.apply(null, a.benchmark.map(function (b) { return b.runsPerMachine[0]; }));
      var hi = Math.max.apply(null, a.benchmark.map(function (b) { return b.runsPerMachine[1]; }));
      box.innerHTML = html + '</table><p class="small faint">Each machine\\'s median rests on ' + lo + ' to ' + hi + ' runs.</p>';
    }
  }).catch(function () {});
})();
"""

SEND_JS = """(function () {
  var form = document.getElementById('send');
  if (!form) { return; }
  var button = document.getElementById('sendButton');
  var status = document.getElementById('sendStatus');
  var bar = document.getElementById('sendProgress');
  var photos = document.getElementById('photos');
  var labels = document.getElementById('setLabels');
  var sheets = document.getElementById('setSheets');
  var rows = document.getElementById('setRows');
  var MB = 1024 * 1024;
  var limit = {
    files: +form.getAttribute('data-max-files'),
    fileMb: +form.getAttribute('data-max-file-mb'),
    sendMb: +form.getAttribute('data-max-send-mb'),
    setFiles: +form.getAttribute('data-max-set-files'),
    setMb: +form.getAttribute('data-max-set-mb')
  };
  var VIEWS = ['', 'front', 'from the left', 'from the right', 'scan'];
  var TYPES = /\.(jpe?g|png|tiff?|heic|heif)$/i;

  function say(text, bad) {
    status.textContent = text;
    status.className = 'small ' + (bad ? 'warn' : 'teal');
  }

  function mb(bytes) { return (bytes / MB).toFixed(1); }

  // Entry 418 section 3.1: with several photos, each can say which sheet of the set it shows and from where.
  function drawRows() {
    var files = photos.files ? Array.prototype.slice.call(photos.files) : [];
    rows.innerHTML = '';
    labels.hidden = files.length < 2;
    var n = +sheets.value;
    files.forEach(function (f, i) {
      var row = document.createElement('div');
      row.className = 'small';
      var name = document.createElement('span');
      name.textContent = f.name + ' ';
      var sheet = document.createElement('select');
      sheet.setAttribute('data-sheet', i);
      sheet.setAttribute('aria-label', 'Which sheet ' + f.name + ' shows');
      [''].concat(Array.apply(null, Array(n)).map(function (_, k) { return (k + 1) + ' of ' + n; })).concat(n > 1 ? ['all together'] : []).forEach(function (v) {
        var o = document.createElement('option'); o.value = v; o.textContent = v === '' ? 'sheet?' : 'sheet ' + v; sheet.appendChild(o);
      });
      var view = document.createElement('select');
      view.setAttribute('data-view', i);
      view.setAttribute('aria-label', 'Which view ' + f.name + ' is');
      VIEWS.forEach(function (v) { var o = document.createElement('option'); o.value = v; o.textContent = v === '' ? 'view?' : v; view.appendChild(o); });
      row.appendChild(name); row.appendChild(sheet); row.appendChild(document.createTextNode(' ')); row.appendChild(view);
      rows.appendChild(row);
    });
  }
  photos.addEventListener('change', drawRows);
  sheets.addEventListener('change', drawRows);

  function labelOf(i) {
    var sheet = rows.querySelector('[data-sheet="' + i + '"]');
    var view = rows.querySelector('[data-view="' + i + '"]');
    var parts = [];
    if (sheet && sheet.value) { parts.push(sheet.value === 'all together' ? 'all sheets together' : 'sheet ' + sheet.value); }
    if (view && view.value) { parts.push(view.value); }
    return parts.join(', ');
  }

  // Section 2.2: everything the receiver would refuse is said before a byte is sent.
  function problem(files) {
    if (files.length > limit.setFiles) { return 'That is ' + files.length + ' files, and one set can hold ' + limit.setFiles + '. Please send them as two sets.'; }
    var total = 0;
    for (var i = 0; i < files.length; i++) {
      var f = files[i];
      if (/\.pdf$/i.test(f.name) || f.type === 'application/pdf') { return f.name + ' is a PDF. This page takes photographs only: save a scan as a JPEG or a PNG and send that.'; }
      if (!TYPES.test(f.name) && !/^image\//.test(f.type)) { return f.name + ' is not a photo this page can take. JPEG, PNG, HEIC and TIFF are accepted.'; }
      if (f.size > limit.fileMb * MB) { return f.name + ' is ' + mb(f.size) + ' MB, over the ' + limit.fileMb + ' MB a photo may be. Send it as it came off the camera, or as a JPEG.'; }
      total += f.size;
    }
    if (total > limit.setMb * MB) { return 'These come to ' + mb(total) + ' MB, over the ' + limit.setMb + ' MB one set can hold. Please send them as two sets.'; }
    return null;
  }

  // In order, a new part when the next file would pass the files or the megabytes one send may carry.
  function split(files) {
    var parts = [], current = [], bytes = 0, room = limit.sendMb * MB - MB;
    files.forEach(function (f, i) {
      if (current.length && (current.length >= limit.files || bytes + f.size > room)) { parts.push(current); current = []; bytes = 0; }
      current.push(i); bytes += f.size;
    });
    if (current.length) { parts.push(current); }
    return parts;
  }

  // Section 2.3: a reply that is not GroupLab's own says what happened, with its status, and whether anything may have arrived.
  function notOurs(code, part, parts) {
    var what = code === 413 ? 'The upload was too large for one send (HTTP 413).'
      : code === 0 ? 'The connection dropped before the server answered.'
      : (code === 502 || code === 504 || code === 522 || code === 524) ? 'The server took too long to answer (HTTP ' + code + ').'
      : 'The server sent a reply this page could not read (HTTP ' + code + ').';
    var arrived = parts > 1
      ? ' Nothing is stored as a submission until the last part of a set arrives, so sending the whole set again is safe.'
      : ' It may have arrived anyway. Sending again is safe: GroupLab recognizes photos it already has and keeps each once.';
    return what + arrived;
  }

  function send(data, sentBefore, partBytes, total, part, parts) {
    return new Promise(function (resolve) {
      var xhr = new XMLHttpRequest();
      xhr.open('POST', form.action);
      xhr.upload.onprogress = function (e) {
        if (!e.lengthComputable) { return; }
        var done = sentBefore + Math.min(e.loaded, partBytes);
        bar.value = Math.min(1, done / total);
        say((parts > 1 ? 'Sending part ' + part + ' of ' + parts + ': ' : 'Sending: ') + mb(Math.min(done, total)) + ' of ' + mb(total) + ' MB.', false);
      };
      xhr.upload.onload = function () { say('Received, checking the files.', false); };
      xhr.onload = function () {
        var r = null;
        try { r = JSON.parse(xhr.responseText); } catch (e) { r = null; }
        resolve(r && typeof r === 'object' ? r : { ok: false, error: notOurs(xhr.status, part, parts) });
      };
      xhr.onerror = function () { resolve({ ok: false, error: notOurs(0, part, parts) }); };
      xhr.send(data);
    });
  }

  form.addEventListener('submit', function (e) {
    e.preventDefault();
    var files = photos.files ? Array.prototype.slice.call(photos.files) : [];
    if (files.length === 0) {
      say('Choose at least one photo first.', true);
      return;
    }
    if (!form.querySelector('input[name="level"]:checked')) {
      say('Please choose how GroupLab may use the photos. It is the one thing that is not optional.', true);
      document.getElementById('level_testing').focus();
      return;
    }
    var wrong = problem(files);
    if (wrong) { say(wrong, true); return; }

    button.disabled = true;
    bar.hidden = false;
    bar.value = 0;
    var parts = split(files);
    var total = files.reduce(function (t, f) { return t + f.size; }, 0);
    var base = new FormData(form);
    base.delete('photos[]');
    var setId = '', token = '', sentBefore = 0, duplicates = 0;

    function finish(r) {
      bar.value = 1;
      var again = r.already ? '<p>These photos were already in: nothing new was stored, and nothing more is needed.</p>'
        : duplicates > 0 ? '<p class=\"small faint\">' + duplicates + ' of them had arrived before and were kept once.</p>' : '';
      form.innerHTML = '<h2 class=\"h3\">Thank you.</h2>' +
        '<p>Your photos are in. Reference <span class=\"mono\">' + (r.id || '') + '</span>, which you do not need to keep.</p>' + again +
        '<p class=\"small faint\">Nothing else happens now: no email, no account, no follow up. ' +
        '<a href=\"/shoot-a-target/\">Back to shooting a target</a>.</p>';
      form.scrollIntoView({ block: 'start' });
    }

    function failed(r) {
      button.disabled = false;
      bar.hidden = true;
      say((r && r.error) || 'That did not go through. Please try again.', true);
      if (window.turnstile) { window.turnstile.reset(); }
    }

    function next(k) {
      var data = new FormData();
      base.forEach(function (v, key) { data.append(key, v); });
      data.append('part', String(k + 1));
      data.append('parts', String(parts.length));
      if (parts.length > 1) { data.append('set_bytes', String(total)); }
      if (k > 0) { data.append('set_id', setId); data.append('set_token', token); }
      var bytes = 0;
      parts[k].forEach(function (i) { data.append('photos[]', files[i], files[i].name); data.append('labels[]', labelOf(i)); bytes += files[i].size; });
      send(data, sentBefore, bytes, total, k + 1, parts.length).then(function (r) {
        if (!r || !r.ok) { failed(r); return; }
        duplicates += r.duplicates || 0;
        sentBefore += bytes;
        if (k === 0 && parts.length > 1) { setId = r.id || ''; token = r.set_token || ''; }
        if (k + 1 < parts.length) { next(k + 1); } else { finish(r); }
      });
    }
    next(0);
  });
})();
"""


RECEIVERS = ["api/upload.php", "api/crash-report.php", "api/app-submission.php", "api/error-report.php", "api/survey.php"]


def php_problems() -> list[str]:
    found = sorted(p.relative_to(OUT).as_posix() for p in OUT.rglob("*.php"))
    stray = [p for p in found if p not in RECEIVERS]
    problems = [f"{p}: a .php file that is not one of the receivers, and nothing else may be executable" for p in stray]
    # Entry 195 section 1: error-report.php was written, named in the nginx include and installed on the server, and never shipped,
    # because it was missing from this list and the copy below, and nothing said so. A receiver the list names and the build did not
    # write is now a failed build, and a receiver in website/api/ the list does not name is too.
    if limits().get("open"):
        problems += [f"{r}: a receiver this build should ship and did not" for r in RECEIVERS if not (OUT / r).is_file()]
    problems += [f"website/api/{p.name}: a receiver the site does not ship; add it to RECEIVERS and the copy in main"
                 for p in sorted((REPO / "website" / "api").glob("*.php")) if f"api/{p.name}" not in RECEIVERS]
    return problems


# ---------------------------------------------------------------- main


def main() -> None:
    for p in [REPO, DONOR]:
        need(p)
    # Start from nothing. Without this a page that was deleted from the builder stays in the output
    # folder for ever and is published with every archive after it, which nobody would notice by
    # reading a diff. Found while proving the stray .php check works.
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True, exist_ok=True)

    build_fonts()
    build_images()
    build_downloads()
    write("assets/css/site.css", served_css(CSS))
    # Entry 444: the how-it-works pages' own styles go only to those four pages, and the page weight saved pays for the new look.
    write("assets/css/how-it-works.css", served_css(_how_it_works.CSS))
    write("assets/js/page.js", served_js(PAGE_JS))
    write("assets/js/how-it-works.js", _how_it_works.JS.strip() + "\n")
    write("assets/js/theme.js", served_js(JS))
    write("assets/js/terms.js", served_js(TERMS_JS))
    write("assets/js/guide-toc.js", served_js(GUIDE_TOC_JS))
    write("assets/js/primer.js", PRIMER_JS)
    write("assets/css/home.css", served_css(HOME_CSS))
    write("assets/css/download.css", served_css(DOWNLOAD_CSS))
    write("assets/js/home.js", served_js(HOME_JS))

    write("index.html", page_home())
    # Entry 440: concept B, out of sight until Alan approves it; the home page above is unchanged.
    write("testpage/index.html", page_testpage_moved())
    write("download/index.html", page_download())
    write("shoot-a-target/index.html", page_shoot())
    # Entry 129: the page and the receiver go up together or not at all. A form posting to a path the server does
    # not serve yet takes somebody's photographs, spends their upload and tells them nothing useful.
    if limits().get("open"):
        write("targets/index.html", page_send())
        write("shoot-a-target/send/index.html", page_send_moved())
        # Section 3.4: the receivers are part of the site build, so they are versioned here, signed and delivered
        # by the same pipeline as the pages rather than copied to the server by hand.
        copy(need(REPO / "website" / "api" / "upload.php"), "api/upload.php")
        copy(need(REPO / "website" / "api" / "crash-report.php"), "api/crash-report.php")
        # Entry 165: the application's receiver ships with the site. Until nginx names it, the server answers it with the 404 every other
        # .php file gets, and the application never uses it while limits.json's appOpen is false.
        copy(need(REPO / "website" / "api" / "app-submission.php"), "api/app-submission.php")
        # Entry 194, shipped at last by entry 195: the error report receiver. The application uses it only while errorReportsOpen is true.
        copy(need(REPO / "website" / "api" / "error-report.php"), "api/error-report.php")
        # Entries 207 and 208: the survey's receiver. Until nginx names it the server answers 404, and the application sends nothing to it
        # while limits.json's surveyOpen is false.
        copy(need(REPO / "website" / "api" / "survey.php"), "api/survey.php")
        write("assets/js/send.js", SEND_JS)
    write("support/index.html", page_support())
    write("guides/index.html", page_guides_index())
    for g in GUIDES:
        write(f"guides/{g[0]}/index.html", page_guide(*g))
    write("guides/glossary/index.html", page_glossary())
    write("what-can-be-measured/index.html", page_what_can_be_measured())
    write("releases/index.html", page_releases())
    figure_problems = build_research_figures()
    write("research/index.html", page_research_index())
    for meta in research_articles():
        write(f"research/{meta['slug']}/index.html", page_research_article(meta))
        figures = REPO / "website" / "research" / meta["slug"]
        for extra in sorted(figures.rglob("*")) if figures.is_dir() else []:
            if extra.is_file() and extra.suffix.lower() in {".png", ".svg", ".csv"}:
                copy(extra, f"research/{meta['slug']}/{extra.relative_to(figures).as_posix()}")
    write("tour/index.html", page_tour_index())
    for path, title, description, body in _how_it_works.pages():
        write(path.strip("/") + "/index.html", shell(path, title, description, body, "Tour", css=("how-it-works",)))
    for key in tour()["order"]:
        write(f"tour/{key}/index.html", page_tour_screen(key))
    for old in TOUR_MOVED:
        write(f"tour/{old}/index.html", page_tour_moved(old))
    write("discord/index.html", page_discord())
    write("survey/index.html", page_survey())
    write("features/index.html", page_features())
    write("assets/js/survey.js", served_js(SURVEY_JS))
    write("assets/js/download.js", served_js(DOWNLOAD_JS))
    write("404.html", page_404())

    pages = ["/", "/download/", "/tour/", "/shoot-a-target/", "/guides/", "/guides/user-guide/", "/guides/testing-guide/", "/guides/statistics-primer/", GLOSSARY_PATH, "/releases/", "/support/", SURVEY_PATH, FEATURES_PATH]
    pages += [f"/tour/{key}/" for key in tour()["order"]]
    today = datetime.date.today().isoformat()
    urls = "".join(f"<url><loc>{SITE_URL}{p}</loc><lastmod>{today}</lastmod></url>" for p in pages)
    write("sitemap.xml", f'<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">{urls}</urlset>\n')
    write("robots.txt", f"User-agent: *\nAllow: /\nSitemap: {SITE_URL}/sitemap.xml\n")

    fingerprint()

    # Checks: banned words and characters, and no IP address anywhere in text output.
    problems = release_notes_are_current()
    ip = re.compile(r"\b(?:\d{1,3}\.){3}\d{1,3}\b")
    for f in OUT.rglob("*"):
        if f.suffix in {".html", ".css", ".js", ".xml", ".txt", ".svg"}:
            text = f.read_text(encoding="utf-8", errors="replace")
            low = text.lower()
            for b in BANNED:
                if b in low:
                    problems.append(f"{f.relative_to(OUT)}: contains {b!r}")
            if f.name != "LICENSE.txt":
                # SVG drawing data is all numbers; leave it out of the address check.
                for m in ip.findall(re.sub(r'\s(?:d|points|viewBox)="[^"]*"', "", text)):
                    problems.append(f"{f.relative_to(OUT)}: looks like an IP address: {m}")

    # Entry 387: a check about the whole site's safety or truth stops the publish; a check about some pages holds back only those pages
    # (their last published copy is kept where it can be fetched); a picture that is only old publishes and is reported.
    problems += figure_problems + limit_problems()
    problems += link_problems()
    problems += home_problems()
    problems += header_problems()
    problems += heading_problems()
    problems += raw_table_problems()
    problems += php_problems()
    problems += send_problems()
    problems += device_problems()
    problems += download_problems()
    problems += _how_it_works.problems(published_articles(), lambda rel: (REPO / rel).exists())
    paged = research_problems() + figure_theme_problems() + tour_problems() + feature_problems() + term_problems() + parity_problems()
    paged += guide_problems()
    stale = _screens_stamp.problems() + _readme_images.problems()
    stop, report = settle(problems, paged, stale, OUT, os.environ.get("GROUPLAB_LIVE_SITE"))
    for line in report:
        print(line)
    if stop:
        sys.exit("build: checks failed")

    total = sum(f.stat().st_size for f in OUT.rglob("*") if f.is_file())
    count = sum(1 for f in OUT.rglob("*") if f.is_file())
    print(f"build: ok, {count} files, {total / 1024 / 1024:.1f} MB in {OUT}")


# Entry 387 (Alan: "So one part of the site doesn't hold up everything"). The home page, the release notes and the download page are never
# held back: they publish with the stale part, and the report says so.
NEVER_HELD = {"releases/index.html", "download/index.html", "index.html"}


def pages_of(message: str, out: Path) -> list[str]:
    """The built pages a check's message is about, from where it says it is, or [] where it does not say: such a message stops the publish."""
    where = message.split(":", 1)[0].strip()
    if where in ("website/features.json", "docs/PHONE-PARITY.md") or where.startswith("features"):
        return ["features/index.html"]
    if where == "website/tour.json" or where.startswith("tour"):
        return sorted(p.relative_to(out).as_posix() for p in (out / "tour").rglob("index.html")) or ["tour/index.html"]
    m = re.match(r"^research/([a-z0-9-]+)", where)
    if m:
        return [f"research/{m.group(1)}/index.html"]
    if where.endswith(".html"):
        return [where]
    return []


def last_published(page: str, live: str | None) -> bytes | None:
    """The page as grouplab.org serves it now, or None where there is no live site to ask or it does not answer."""
    if not live:
        return None
    import urllib.request
    address = live.rstrip("/") + "/" + (page[: -len("index.html")] if page.endswith("index.html") else page)
    try:
        with urllib.request.urlopen(address, timeout=20) as answer:
            return answer.read() if answer.status == 200 else None
    except OSError:
        return None


def settle(whole: list[str], paged: list[str], stale: list[str], out: Path, live: str | None, fetch=last_published) -> tuple[bool, list[str]]:
    """Entry 387: whether the publish stops, and the lines to print: every whole-site failure, and one line for every page held back or
    published with a stale part. A message from a page check that names no page counts as about the whole site, the safe way round."""
    report = list(whole)
    stops = bool(whole)
    held: dict[str, list[str]] = {}
    for message in paged:
        pages = pages_of(message, out)
        if not pages:
            report.append(message)
            stops = True
            continue
        for page in pages:
            held.setdefault(page, []).append(message)
    for page, why in sorted(held.items()):
        copy = None if page in NEVER_HELD else fetch(page, live)
        if copy is not None:
            (out / page).write_bytes(copy)
            report.append(f"held back: {page}, the last published copy kept: {why[0]}")
        else:
            report.append(f"published with a stale part: {page}: {why[0]}")
    for message in stale:
        report.append(f"published with a stale part: the screenshots: {message}")
    return stops, report


if __name__ == "__main__":
    main()
