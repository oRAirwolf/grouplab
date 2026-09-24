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
import json
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
# the thing it counts. The home page said twenty-two built-in sheets while the tour said twenty, both typed by hand.
import importlib.util as _importlib_util

_spec = _importlib_util.spec_from_file_location("counts", Path(__file__).resolve().parent.parent / "scripts" / "counts.py")
_counts = _importlib_util.module_from_spec(_spec)
_spec.loader.exec_module(_counts)
COUNTS = _counts.counts()


def count_words(name: str, capital: bool = False) -> str:
    said = _counts.words(COUNTS[name])
    return said[:1].upper() + said[1:] if capital else said

# ---------------------------------------------------------------- settings

SITE_URL = "https://grouplab.org"
GITHUB = "https://github.com/oRAirwolf/grouplab"
NIGHTLY = GITHUB + "/releases/download/nightly/"
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
ASSETS = REPO / "src" / "GroupLab.App" / "Assets"

# The stable asset names the rolling nightly release always serves. The Download buttons must
# point at these and nothing else: a versioned name would be a link that rots in a week.
NIGHTLY_ASSETS = ["grouplab-setup-win-x64.exe", "grouplab-win-x64.zip", "grouplab-linux-x64.tar.gz"]


def site_commit() -> str:
    """The commit this site was built from, which every page carries so the server can prove what it is serving."""
    head = os.environ.get("GITHUB_SHA")
    if head:
        return head[:40]
    try:
        out = subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=str(REPO), capture_output=True, text=True, check=True, timeout=30
        )
        return out.stdout.strip()[:40]
    except (OSError, subprocess.SubprocessError):
        return "unknown"


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
    copy(need(ASSETS / "icons" / "grouplab.ico"), "favicon.ico")
    icon = Image.open(need(ASSETS / "icons" / "linux" / "grouplab-256.png")).convert("RGBA")
    icon.resize((180, 180), Image.LANCZOS).save(OUT / "apple-touch-icon.png", optimize=True)

    # Every screen in both themes at 1400 by 900, as WebP.
    for png in sorted(SCREENS.glob("*-1400x900.png")):
        img = Image.open(png).convert("RGB")
        dst = OUT / "assets" / "screens" / (png.stem + ".webp")
        dst.parent.mkdir(parents=True, exist_ok=True)
        img.save(dst, "WEBP", quality=90, method=6)

    # Link preview image, 1200 by 630, cut from the dark analysis screen.
    img = Image.open(need(SCREENS / "analysis-dark-1400x900.png")).convert("RGB")
    w, h = img.size
    crop = img.crop((0, 0, w, int(w * 630 / 1200)))
    crop.resize((1200, 630), Image.LANCZOS).save(OUT / "assets" / "img" / "og.png", optimize=True)


def build_downloads() -> None:
    for pdf in ["grouplab-donor-pack.pdf", "grouplab-donor-instructions.pdf", "GL-CF25-LTR-D.pdf", "GL-CF25-LTR.pdf"]:
        copy(need(DONOR / pdf), f"donor/{pdf}")
    copy(need(REPO / "docs" / "USER-GUIDE.pdf"), "guides/user-guide.pdf")
    copy(need(REPO / "docs" / "TESTING-GUIDE.pdf"), "guides/testing-guide.pdf")


# ---------------------------------------------------------------- page shell

# NOTES-FROM-PLANNING.md entry 148 section 3: the Discord invite is written down in exactly one place. Everything
# published points at the canonical address, which redirects to the invite, so replacing the invite later is a single
# edit and no published link ever breaks.
LINKS = REPO / "website" / "links.json"


def links() -> dict:
    return json.loads(need(LINKS).read_text(encoding="utf-8"))


DISCORD = "/discord/"


NAV = [
    ("Download", "/download/"),
    ("Tour", "/tour/"),
    ("Shoot a target", "/shoot-a-target/"),
    ("Guides", "/guides/"),
    ("Research", "/research/"),
    ("Community", DISCORD),
    ("Release notes", "/releases/"),
    ("Support", "/support/"),
]

# NOTES-FROM-PLANNING.md entry 142 section 3: the index is grouped, and the groups read in this order rather than
# alphabetically, because somebody arriving wants to know what a target says before they want to know how it is built.
RESEARCH_GROUPS = ["Reading targets", "Measuring groups", "Range tests", "Guides", "How GroupLab is built"]

ICON_GITHUB = '<svg class="ico" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="currentColor"><path d="M12 2a10 10 0 0 0-3.16 19.49c.5.09.68-.22.68-.48v-1.7c-2.78.6-3.37-1.34-3.37-1.34-.45-1.16-1.11-1.47-1.11-1.47-.91-.62.07-.61.07-.61 1 .07 1.53 1.03 1.53 1.03.9 1.52 2.34 1.08 2.91.83.09-.65.35-1.08.63-1.33-2.22-.25-4.55-1.11-4.55-4.94 0-1.09.39-1.98 1.03-2.68-.1-.25-.45-1.27.1-2.64 0 0 .84-.27 2.75 1.02a9.5 9.5 0 0 1 5 0c1.91-1.29 2.75-1.02 2.75-1.02.55 1.37.2 2.39.1 2.64.64.7 1.03 1.59 1.03 2.68 0 3.84-2.34 4.68-4.57 4.93.36.31.68.92.68 1.85v2.74c0 .27.18.58.69.48A10 10 0 0 0 12 2z"/></svg>'
ICON_THEME = '<svg class="ico" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="12" cy="12" r="4.5"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>'
ICON_MENU = '<svg class="ico" width="22" height="22" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M4 7h16M4 12h16M4 17h16"/></svg>'
ICON_PDF = '<svg class="ico pdf" width="28" height="34" viewBox="0 0 28 34" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M2 2h16l8 8v22H2z"/><path d="M18 2v8h8"/><path d="M7 19h14M7 24h14M7 14h6"/></svg>'


def logo(cls: str = "logo") -> str:
    return (
        f'<img class="{cls} only-dark" src="/assets/img/grouplab-lockup.svg" alt="GroupLab" width="143" height="30">'
        f'<img class="{cls} only-light" src="/assets/img/grouplab-lockup-light.svg" alt="GroupLab" width="143" height="30">'
    )


def shell(path: str, title: str, description: str, body: str, active: str = "") -> str:
    full_title = "GroupLab" if not title else f"{title} | GroupLab"
    links = []
    for label, href in NAV:
        cur = ' aria-current="page"' if label == active else ""
        links.append(f'<a href="{href}"{cur}>{esc(label)}</a>')
    nav_links = "\n".join(links)
    year = datetime.date.today().year
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
<meta property="og:image" content="{SITE_URL}/assets/img/og.png">
<meta name="twitter:card" content="summary_large_image">
<link rel="icon" href="/favicon.ico" sizes="any">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
<link rel="preload" href="/assets/fonts/plex-sans-400.woff2" as="font" type="font/woff2" crossorigin>
<link rel="preload" href="/assets/fonts/plex-sans-condensed-700.woff2" as="font" type="font/woff2" crossorigin>
<link rel="stylesheet" href="/assets/css/site.css">
<script src="/assets/js/theme.js"></script>
</head>
<body>
<a class="skip" href="#main">Skip to content</a>
<header class="site-header">
<div class="wrap header-row">
<a class="brand" href="/" aria-label="GroupLab home">{logo()}</a>
<nav class="nav" aria-label="Main">
{nav_links}
<a href="{GITHUB}" class="gh">{ICON_GITHUB}<span>GitHub</span></a>
</nav>
<div class="header-tools">
<button type="button" class="theme-toggle" aria-label="Switch between dark and light">{ICON_THEME}</button>
<details class="menu">
<summary aria-label="Menu">{ICON_MENU}</summary>
<nav class="menu-panel" aria-label="Main">
{nav_links}
<a href="{GITHUB}">GitHub</a>
</nav>
</details>
</div>
</div>
</header>
<main id="main">
{body}
</main>
<footer class="site-footer">
<div class="wrap footer-row">
<div class="footer-about">
<a href="/" aria-label="GroupLab home">{logo("logo small")}</a>
<p>Free and open source under GPL-3.0. No account, no ads, no paid tier. The application keeps everything on your own computer and sends nothing anywhere.</p>
</div>
<nav class="footer-links" aria-label="Footer">
<a href="/download/">Download</a><a href="{GITHUB}">Source on GitHub</a>
<a href="/shoot-a-target/">Shoot a target</a><a href="{GITHUB}/blob/main/LICENSE">Licence, GPL-3.0</a>
<a href="/guides/">Guides</a><a href="/releases/">Release notes</a><a href="{GITHUB}/releases">All builds</a>
<a href="/support/">Support</a><a href="{DISCORD}">Community</a>
</nav>
</div>
<div class="wrap footer-base">GroupLab is a working name and may change. &#169; {year} the GroupLab contributors.</div>
</footer>
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


def btn(label: str, href: str, primary: bool = False, sub: str | None = None, big: bool = False) -> str:
    cls = "btn " + ("btn-primary" if primary else "btn-secondary") + (" btn-big" if big else "")
    inner = f"<span>{esc(label)}</span>"
    if sub:
        inner += f'<span class="btn-sub mono">{esc(sub)}</span>'
    return f'<a class="{cls}" href="{href}">{inner}</a>'


# ---------------------------------------------------------------- pages


def page_home() -> str:
    body = f"""
<section class="wrap hero">
<div class="hero-text">
<p class="eyebrow">Free &#183; open source &#183; GPL-3.0 &#183; Windows test build</p>
<h1 class="display">Measure how accurately your rifle shoots, and how little a small group can tell you.</h1>
<p class="lead">GroupLab reads a photograph or a scan of a target you have shot, finds every hole, and gives you the group's statistics. Every figure comes with the range it could really be, and the software says plainly when the evidence does not support a conclusion.</p>
<div class="actions">
{btn("Download for Windows", NIGHTLY + "grouplab-setup-win-x64.exe", True, "Latest test build · installer · no admin rights", True)}
{btn("View the source on GitHub", GITHUB, big=True)}
</div>
<p class="fine mono">Unsigned and rebuilt after every change that passes its tests. It may be broken. <a href="/download/">Other downloads and what to expect</a></p>
</div>
<figure class="hero-shot">
{screen("analysis", "GroupLab's analysis screen: twenty-four shots composited onto one bull, with mean radius, sigma, extreme spread and CEP, each with its interval", eager=True)}
<figcaption class="fine mono">The analysis screen, rendered from the current build. The sheet is a synthetic test sheet, not anybody's target.</figcaption>
</figure>
</section>

<section class="wrap section two-col">
<div class="stack">
<p class="eyebrow">The problem</p>
<h2>Five shots measured three quarters. What does the rifle actually shoot?</h2>
<p>A shooter fires five rounds, measures the two widest holes, changes one thing, fires five more and concludes the change worked. It almost certainly did not. Two loads that differ by 20 percent on five-shot groups cannot be told apart.</p>
<p>GroupLab measures far more carefully, and then tells you what the number is worth.</p>
</div>
<div class="panel figure-panel">
<div class="figure-top">
<p class="mono dim small">From five shots, the true dispersion lies between</p>
<p class="mono big-figure">0.68 <span class="faint">and</span> 1.92 &#215;</p>
<p class="mono dim small">what was measured, a factor of 2.8</p>
</div>
<dl class="figure-list">
<div><dt class="mono">&lt; 5</dt><dd>Refuses to quote a group size at all, and says why.</dd></div>
<div><dt class="mono">5 to 20</dt><dd>Prints each figure with its interval's real coverage, not a comfortable 95 percent.</dd></div>
<div><dt class="mono">compare</dt><dd>Tells you when two loads cannot be separated, and how many rounds it would take to know.</dd></div>
</dl>
</div>
</section>

<section class="wrap section">
<div class="stack narrow">
<p class="eyebrow">How it works</p>
<h2>One shot per bull, twenty-five bulls, one honest group.</h2>
<p>Each hole is measured against its own aiming point, so holes never overlap, and the offsets are pooled into one group far larger than you could shoot into a single bullseye.</p>
</div>
<ol class="steps">
<li class="panel"><span class="mono num">01</span><h3>Print a GroupLab sheet</h3><p>{count_words('sheets', True)} built-in sheets, printed at actual size. Registration markers and QR codes carry the sheet's full definition.</p></li>
<li class="panel"><span class="mono num">02</span><h3>Shoot it</h3><p>One shot per bull, in order. Write your load in the block at the bottom, or print it filled in.</p></li>
<li class="panel"><span class="mono num">03</span><h3>Scan or photograph it</h3><p>A flat 600 dpi scan is best. A photograph works too, even with the sheet still stapled to the board.</p></li>
<li class="panel"><span class="mono num">04</span><h3>Read the analysis</h3><p>Mean radius, sigma, CEP, extreme spread and the zero correction, each with its interval and the reasoning one click away.</p></li>
</ol>
<div class="note note-teal"><span class="mono">Any target</span><p>A store-bought target or blank paper can be marked by hand: set a known length, tap each impact, and the same statistics run.</p></div>
</section>

<section class="wrap section">
<div class="stack">
<p class="eyebrow">The application</p>
<h2>It shows its work.</h2>
<p>Every figure has its reasoning one click away, and anything GroupLab is unsure of is raised for you to settle rather than guessed at quietly.</p>
</div>
<div class="grid-2">
<figure class="fig">{screen("marking", "The marking screen with every detected hole numbered to its bull and one shot raised for review")}<figcaption><strong>Marking and review</strong><span>Every hole numbered to its bull. Anything the software is unsure of is raised for you to settle, with the keys to do it. <a href="/tour/marking/">See this screen explained</a></span></figcaption></figure>
<div class="panel status">
<h3>What it is today</h3>
<p>A Windows test build. Printing, marking, detection, the statistics, session records and reports all work. Much of it is built but not yet proven against a large body of real targets, which is why the project asks for them.</p>
<p class="mono dim small">Not built yet</p>
<p class="text">Hole detection on plain paper &#183; Garmin Xero import &#183; Android and iOS</p>
<a href="{GITHUB}#planned">The full status, phase by phase, on GitHub</a>
</div>
</div>
<div class="note note-teal"><span class="mono">Every screen</span><p>The <a href="/tour/">tour</a> has a page for each of the ten screens: what it is for, what you are looking at, and what you would do there. It is the quickest way to see whether GroupLab suits you before you download it.</p></div>
</section>

<section class="wrap section last">
<div class="callout">
<div class="stack">
<p class="eyebrow amber">Help prove it</p>
<h2>Shoot a target for GroupLab.</h2>
<p class="text">GroupLab needs real targets, shot by real people with real rifles, to prove it measures correctly. Print a sheet, shoot it, photograph it before you take it down: about ten minutes on top of the shooting. {"Sending them takes no account, no email address and no follow up." if limits().get("open") else "The page for sending them is built and waiting on one install on the server; keep the files meanwhile."}</p>
</div>
<div class="actions col">
{btn("Get the donor pack", "/shoot-a-target/", True, "Instructions and two targets · PDF")}
</div>
</div>
</section>
"""
    return shell("/", "", "GroupLab measures how accurately a rifle shoots from a photograph or scan of a target, and is honest about how little a small group tells you. Free and open source.", body)


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
    rendered = markdown.markdown(body, extensions=["sane_lists", "fenced_code"])

    # The page's own heading level: the statement's h2s sit under this section's h2, so they become h3s.
    rendered = rendered.replace("<h2>", '<h3 class="h3">').replace("</h2>", "</h3>")
    rendered = rendered.replace("<pre><code>", '<p><code class="block">').replace("</code></pre>", "</code></p>")
    return '<h2 class="h3">What is supported, and what is not</h2>' + rendered


def page_download() -> str:
    def card(title: str, file: str, desc: str, points: list[str], rec: bool = False, label: str | None = None) -> str:
        badge = '<span class="badge mono">Recommended</span>' if rec else ""
        lis = "".join(f"<li>{p}</li>" for p in points)
        return f"""<div class="panel card{' card-rec' if rec else ''}">
<div class="card-head"><h2 class="h3">{title}</h2>{badge}</div>
<p class="mono teal small">{file}</p>
<p>{desc}</p>
<ul class="dim small-list">{lis}</ul>
<div class="card-foot">{btn(label or ("Download " + title.lower()), NIGHTLY + file, rec)}</div>
</div>"""

    body = f"""
<section class="wrap page-head">
<p class="eyebrow">Download</p>
<h1>The latest test build</h1>
<p class="lead">Rebuilt automatically after every change that passes the tests on Windows, Linux and macOS, and published within minutes. It may be broken: passing the tests is not the same as somebody having used it. There is no full release yet.</p>
</section>
<section class="wrap grid-3">
{card("Installer", "grouplab-setup-win-x64.exe", "Windows 10 and 11. Installs into your own user account, with an entry in Add or remove programs.", ["No administrator rights needed", "Keeps itself up to date: asks first, then updates in the background"], True)}
{card("Zip", "grouplab-win-x64.zip", "Windows 10 and 11. Unzip it anywhere and run GroupLab.App.exe.", ["Nothing to install", "Tells you when a newer build exists; you download it yourself"])}
{card("Linux tarball", "grouplab-linux-x64.tar.gz", "Self-contained, built on Ubuntu, and tested on every change.", ["Nobody uses it day to day yet", "Reports from Linux are especially welcome"])}
</section>
<section class="wrap grid-3">
{card("macOS, Apple silicon", "grouplab-macos-arm64.tar.gz", "For any Mac with an M1 or later. Self-contained, built on macOS, and tested by the suite on every change.", ["<strong>Untested on a real Mac.</strong> Nobody has run it", "Unsigned: see the Terminal command below", "M1, M2, M3, M4. Not an Intel Mac"], label="Download for Apple silicon")}
{card("macOS, Intel", "grouplab-macos-x64.tar.gz", "For a Mac with an Intel processor. Self-contained, built on macOS, and tested by the suite on every change.", ["<strong>Untested on a real Mac.</strong> Nobody has run it", "Unsigned: see the Terminal command below", "Intel only. Not an Apple silicon Mac"], label="Download for an Intel Mac")}
<div class="panel pad stack tight">
<h2 class="h3">Which Mac have you got?</h2>
<p class="small">Apple menu, then About This Mac. A line saying <strong>Chip</strong> and a name beginning with M is Apple silicon. A line saying <strong>Processor</strong> and Intel is the Intel one.</p>
<p class="small faint">Taking the wrong one gives you an application that will not open, with no useful message about why.</p>
</div>
</section>
<section class="wrap section-sm grid-2">
<div class="panel pad">
<h2 class="h3">When Windows says "Windows protected your PC"</h2>
<p>Every build is unsigned, because signing costs money the project has not spent. Windows says this about any program nobody has paid to sign. The source of every build is public, at the commit the download names.</p>
<ol class="text">
<li>Click <strong>More info</strong>.</li>
<li>Click <strong>Run anyway</strong>.</li>
<li>If your antivirus quarantines it, the file it took is <code>GroupLab.App.exe</code>.</li>
</ol>
</div>
<div class="panel pad">
<h2 class="h3">When macOS refuses to open it</h2>
<p>macOS puts a quarantine flag on anything downloaded from the internet, and Gatekeeper refuses to open an unsigned application that carries it. Move <code>GroupLab.app</code> into your Applications folder, then run this in Terminal:</p>
<p><code class="block">xattr -dr com.apple.quarantine /Applications/GroupLab.app</code></p>
<p class="small">That removes the quarantine flag macOS puts on downloaded files, and nothing else. It is the standard way to run unsigned software. <strong>Anyone not comfortable running that command should not run this build.</strong> Why there is no signed build is below, under <a href="#supported">What is supported, and what is not</a>.</p>
</div>
</section>
<section class="wrap section-sm grid-2">
<div class="panel pad">
<h2 class="h3">What you get</h2>
<dl class="facts">
<div><dt class="mono">Runtime</dt><dd>Nothing else to install. The download carries its own .NET runtime.</dd></div>
<div><dt class="mono">Samples</dt><dd>Two sample sheets, so there is something to open in the first minute.</dd></div>
<div><dt class="mono">Your data</dt><dd>Kept in <code>%APPDATA%\\GroupLab</code> on Windows, and under your home folder elsewhere. An update check sends nothing about you.</dd></div>
<div><dt class="mono">Which build</dt><dd>The Settings screen names the version, the train and the commit. Put that line in any report.</dd></div>
</dl>
</div>
<div class="panel pad">
<h2 class="h3">Updating</h2>
<p><strong>Only the Windows installer updates itself.</strong> It asks first, then updates in the background.</p>
<p>The zip, the Linux tarball and both macOS builds tell you when a newer build exists and leave the downloading to you. There is no silent update on those platforms, and GroupLab will not pretend otherwise: it says so on the Settings screen rather than offering an update it cannot apply.</p>
</div>
</section>
<section class="wrap section-sm">
<div class="panel pad stack supported prose" id="supported">
{platform_support()}
</div>
</section>
<section class="wrap section-sm last row-between">
<p>Every build keeps a release of its own, so a bug report names something that still exists. If something does not work, the <a href="{DISCORD}">Discord</a> is somewhere to ask.</p>
<a href="/releases/">What changed in each build</a>
<a href="{GITHUB}/releases">Every build on GitHub</a>
</section>
"""
    return shell("/download/", "Download", "Download the latest GroupLab test build for Windows, Linux or macOS: the installer, the zip, the Linux tarball or an untested Mac build.", body, "Download")


def page_shoot() -> str:
    def pdf(title: str, file: str, desc: str, pages: str, primary: bool = False) -> str:
        size_kb = (DONOR / file).stat().st_size // 1024
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
</div>
<div class="stack tight">
{pdf("Donor pack", "grouplab-donor-pack.pdf", "The instructions and both targets, ready to print.", "4 pages, Letter", True)}
{pdf("Instructions only", "grouplab-donor-instructions.pdf", "The two pages of steps, without the targets.", "2 pages, Letter")}
{pdf("Target with load block", "GL-CF25-LTR-D.pdf", "25 bulls and a block for your load details.", "1 page, Letter")}
{pdf("Target with sighters", "GL-CF25-LTR.pdf", "25 bulls and a row of three sighter bulls.", "1 page, Letter")}
</div>
</section>
<section class="wrap section two-col top last">
<ol class="step-list">
{st("1", "Print it at actual size", "Letter or A4 paper, Actual size or 100 percent. Never Fit to page. Then measure between the centres of the first and last bull in the top row.", "It must be 152.0 mm, or 5.98 in")}
{st("2", "Fill in the block", "At least the date, the distance and the cartridge. Write only inside the block: pen marks anywhere else can be mistaken for bullet holes.")}
{st("3", "Mount it flat and shoot it", "Staple or tape it flat onto cardboard at the four corners. One shot per bull, in number order. A pulled shot or a wrong bull goes in the Notes box.")}
</ol>
<div class="stack">
<ol class="step-list" start="4">
{st("4", "Photograph it before you take it down", "Four photographs on your phone's main camera at 1x: not the wide lens, not zoomed, no flash, your shadow off the sheet. A target still hanging where it was shot is the material the project most needs.")}
{st("5", "Send them as they came off the camera", "A 600 dpi flatbed scan too, if you have one. Do not crop them and do not send them through a messaging app, which shrinks them.", "Send your photos, below" if limits().get("open") else "Sending opens here shortly")}
</ol>
<div class="callout small-callout">
<div class="stack">
<h2 class="h3">Send them</h2>
<p class="text">No account, no email address and no follow up. Every photo is rebuilt from its pixels on the server, so the camera facts the measurements need come across and the location and the date and time cannot.</p>
{"" if limits().get("open") else '<p class="text">The page is built and waiting on one install on the server. Until then, keep your files as they came off the camera.</p>'}
</div>
{f'<div class="actions col">{btn("Send your target photos", "/shoot-a-target/send/", True, "JPEG, PNG, HEIC or TIFF")}</div>' if limits().get("open") else ""}
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
    ]
    for name, expected in pairs:
        got = constant(name)
        if got is None:
            found.append(f"website/api/upload.php: no {name} constant, so the page's limits cannot be checked against it")
        elif not got.startswith(expected):
            found.append(f"website/api/upload.php: {name} is {got!r} and limits.json says {expected!r}")

    if f"const CONSENT_VERSION = '{limit['consentVersion']}'" not in php:
        found.append(f"website/api/upload.php: the consent version is not {limit['consentVersion']}")
    if limit["consentText"] not in php:
        found.append("website/api/upload.php: the consent text differs from limits.json, and the page shows one while the receiver records the other")

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
<p class="eyebrow"><a class="plain" href="/shoot-a-target/">Shoot a target</a> &rsaquo; Send your photos</p>
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
<li>They are pulled onto one machine, measured, and then deleted from the server.</li>
<li>If you tick the box below, they are used for testing only and never published.</li>
</ul>
</div>
</section>

<section class="wrap section last">
<form id="send" class="send-form stack" method="post" action="/api/upload.php" enctype="multipart/form-data">
<noscript><p class="small warn">Sending needs JavaScript, because of the check that proves you are a person. Reading this page does not.</p></noscript>

<h2 class="h3">The photos</h2>
<p class="small faint">Up to {limit["maxFiles"]} files, {limit["maxFileMegabytes"]} MB each and {limit["maxSubmissionMegabytes"]} MB in total. {types}. Not {esc(limit["typesRefused"][0])}: it is not a photograph and the rebuild cannot handle it. Send them as they came off the camera, not cropped and not through a messaging app.</p>
<input type="file" id="photos" name="photos" multiple accept="image/jpeg,image/png,image/tiff,image/heic,image/heif,.jpg,.jpeg,.png,.tif,.tiff,.heic,.heif" required>

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

<div class="panel pad stack tight consent">
<label class="tick"><input type="checkbox" id="consent" name="consent" value="1" required><span>{esc(limit["consentText"])}</span></label>
<label class="tick"><input type="checkbox" id="exclude_public" name="exclude_public" value="1"><span>Do not include my photos in the public data set. Use them for testing only.</span></label>
</div>

<div class="cf-turnstile" data-sitekey="{esc(limit["turnstileSiteKey"])}"></div>

<div class="actions">
<button type="submit" class="btn btn-primary btn-big" id="sendButton"><span>Send the photos</span></button>
</div>
<p id="sendStatus" class="small" role="status" aria-live="polite"></p>
</form>
</section>

<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<script src="/assets/js/send.js" defer></script>
"""
    return shell("/shoot-a-target/send/", "Send your target photos",
                 "Send photographs of a target you have shot, to help prove GroupLab measures correctly. No account and no email address.",
                 body, "Shoot a target")


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
<p class="small">For anything private, or anything with a photograph attached, the support address below is better.</p>
<div class="actions">{btn("What is in the Discord", DISCORD, True)}</div>
</div>
</section>
<section class="wrap grid-3">
<div class="panel pad stack tight"><span class="mono amber">01</span><h2 class="h3">Check what is not done yet</h2><p>Some things are known to be missing or unproven. The testing guide lists them in one short section.</p><a href="/guides/testing-guide/#what-is-not-done-yet">What is not done yet</a></div>
<div class="panel pad stack tight"><span class="mono amber">02</span><h2 class="h3">Make a report package</h2><p>In GroupLab, open the settings from the gear at the bottom left and choose <strong>Report a problem</strong>. It writes a zip with the log and what GroupLab was doing: no location data, and no images unless you add them.</p></div>
<div class="panel pad stack tight"><span class="mono amber">03</span><h2 class="h3">Send it</h2><p>Open an issue on GitHub, attach the zip, and add a line about what you were doing. Copy the build line from the top of the settings screen into it.</p><a href="{GITHUB}/issues/new">Open an issue on GitHub</a></div>
</section>
<section class="wrap section-sm grid-2">
<div class="panel pad stack tight"><h2 class="h3">No GitHub account?</h2><p>Email the report package instead, with the build line and a line about what you were doing.</p><p><a class="mono" href="mailto:{SUPPORT_EMAIL}">{SUPPORT_EMAIL}</a></p></div>
<div class="panel pad stack tight"><h2 class="h3">If a new build will not start</h2><p>Nothing of yours is at risk: settings, sessions, your own sheets and the log live in <code>%APPDATA%\\GroupLab</code>, which no installer or uninstaller touches. To go back, download the build you were on from the releases list and install it over the broken one.</p><a href="{GITHUB}/releases">All builds on GitHub</a></div>
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
]


def slug(text: str) -> str:
    s = re.sub(r"<[^>]+>", "", text).lower()
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return s


def render_guide(md_file: Path) -> tuple[str, str, list[tuple[str, str]]]:
    src = md_file.read_text(encoding="utf-8")
    title_m = re.match(r"#\s+(.+)\n", src)
    title = title_m.group(1).strip() if title_m else md_file.stem
    src = src[title_m.end():] if title_m else src
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

    # Headings get ids, and h2s build the contents list.
    toc: list[tuple[str, str]] = []

    def head(m: re.Match) -> str:
        level, inner = m.group(1), m.group(2)
        hid = slug(inner)
        if level == "2":
            toc.append((hid, re.sub(r"<[^>]+>", "", inner)))
        return f'<h{level} id="{hid}">{inner}</h{level}>'

    body = re.sub(r"<h([23])>(.*?)</h\1>", head, body)

    # Screenshots: repository renders become the site's WebP pair, one per theme.
    def img(m: re.Match) -> str:
        src_attr, alt = m.group(1), m.group(2)
        mm = re.search(r"figures/screens/current/([a-z-]+?)-(light|dark)-1400x900\.png", src_attr)
        if not mm:
            sys.exit(f"build: guide image not in screens/current: {src_attr}")
        return f'<figure class="guide-fig">{screen(mm.group(1), html.unescape(alt))}<figcaption class="mono faint small">{alt}</figcaption></figure>'

    body = re.sub(r'<p><img alt="([^"]*)" src="([^"]+)" ?/?></p>', _swap, body)
    body = re.sub(r'<p><img src="([^"]+)" alt="([^"]*)" ?/?></p>', img, body)

    # Links: relative repository links go to GitHub.
    def link(m: re.Match) -> str:
        href = m.group(1)
        if href.startswith(("http://", "https://", "#", "mailto:")):
            return m.group(0)
        return f'href="{GITHUB}/blob/main/docs/{href}"'

    body = re.sub(r'href="([^"]+)"', link, body)
    return title, body, toc


def _swap(m: re.Match) -> str:
    alt, src_attr = m.group(1), m.group(2)
    mm = re.search(r"figures/screens/current/([a-z-]+?)-(light|dark)-1400x900\.png", src_attr)
    if not mm:
        sys.exit(f"build: guide image not in screens/current: {src_attr}")
    return f'<figure class="guide-fig">{screen(mm.group(1), html.unescape(alt))}<figcaption class="mono faint small">{alt}</figcaption></figure>'


def guide_tabs(current: str) -> str:
    tabs = []
    for key, _, _, label, _ in GUIDES:
        cur = ' aria-current="page"' if key == current else ""
        tabs.append(f'<a class="tab" href="/guides/{key}/"{cur}>{label}</a>')
    return "".join(tabs)


def page_guide(key: str, md: str, pdf: str, label: str, desc: str) -> str:
    title, content, toc = render_guide(need(REPO / "docs" / md))
    toc_html = "\n".join(f'<a href="#{hid}">{esc(html.unescape(text))}</a>' for hid, text in toc)
    body = f"""
<section class="wrap guide-bar">
<nav class="tabs" aria-label="Guides">{guide_tabs(key)}</nav>
<a class="pdf-link" href="/guides/{pdf}">Download as PDF</a>
</section>
<section class="wrap guide">
<nav class="toc" aria-label="On this page">
<p class="mono faint small caps">On this page</p>
{toc_html}
</nav>
<article class="prose">
<h1>{esc(title)}</h1>
{content}
</article>
</section>
"""
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
    title, content, toc = render_guide(need(REPO / "docs" / "WHAT-CAN-BE-MEASURED.md"))
    toc_html = "\n".join(f'<a href="#{hid}">{esc(html.unescape(text))}</a>' for hid, text in toc)
    body = f"""
<section class="wrap guide">
<nav class="toc" aria-label="On this page">
<p class="mono faint small caps">On this page</p>
{toc_html}
</nav>
<article class="prose">
<h1>{esc(title)}</h1>
{content}
</article>
</section>
"""
    return shell("/what-can-be-measured/", "What GroupLab can measure",
                 "Where the scale comes from, what a GroupLab sheet adds, and what a sheet printed at the wrong size does to every figure.",
                 body, "Tour")


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
    article's own group and number, in the site's colours, at the same shape as a thumbnail, so it follows the
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
<section class="wrap stack">
<h1>Research</h1>
<p class="lead">What we have measured, on real targets, with the numbers and the data behind every claim.</p>
<p class="small faint">Every figure says how many shots it rests on. Nothing here is called proven that the shots cannot prove, and where we do not know, it says so.</p>
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
<p class="small faint"><a class="plain" href="/research/">Research</a> &rsaquo; {esc(meta.get("group", ""))}</p>
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


def page_releases() -> str:
    blocks = []
    for i, (anchor, version, notes) in enumerate(releases()):
        # The newest is open, the rest closed, so the page opens on what somebody almost always came for.
        open_attr = " open" if i == 0 else ""
        blocks.append(
            f'<details class="panel pad release" id="{anchor}"{open_attr}>'
            f'<summary><span class="h3">{esc(version)}</span></summary>'
            f'<div class="prose tight">{notes}</div>'
            "</details>"
        )

    body = f"""
<section class="wrap stack">
<h1>Release notes</h1>
<p class="lead">Every build of GroupLab anyone could download, newest first. GroupLab is unreleased, so every one of these is a pre-release.</p>
<p class="small faint">Nightly builds are produced on the nights the application changed, so a gap in the numbers means nothing shipped that night. Work on the website, the documentation or these notes does not make a new build. Numbers below 92 were the run that built them rather than the build itself, and a run that was cancelled or skipped still took its number, which is why the older gaps are wider.</p>
<div class="stack tight releases">
{"".join(blocks)}
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
<p class="lead">Both guides describe the Windows application as it is built today, and every picture in them is a render of the build. They are published from the repository, so they change when it does.</p>
</section>
<section class="wrap grid-2 last">
{''.join(cards)}
</section>
"""
    return shell("/guides/", "Guides", "The GroupLab user guide and the guide to trying the test build for the first time.", body, "Guides")


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
    return json.loads(text)


def rendered_screens() -> set:
    """Every screen the render walk actually produced, by the name the tour uses."""
    return {p.name[: -len("-light-1400x900.png")] for p in SCREENS.glob("*-light-1400x900.png")}


def tour_problems() -> list:
    """Entry 146 section 4.2, as a build failure rather than a test, because the page is the thing that goes wrong."""
    found = []
    data = tour()
    listed, have = set(data["order"]), rendered_screens()

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

    return found


def tour_shot(key: str, alt: str, eager: bool = False) -> str:
    """The screenshot for one tour page, linked to the full size image for a reader who wants to look closely."""
    return (
        f'<a class="plain tour-shot" href="/assets/screens/{key}-dark-1400x900.webp">{screen(key, alt, eager, cls="shot tour-img")}</a>'
    )


def page_tour_index() -> str:
    data = tour()
    cards = []
    for key in data["order"]:
        item = data["screens"][key]
        cards.append(
            f'<a class="panel pad stack tight research-card plain" href="/tour/{key}/">'
            f'<img class="research-thumb" src="/assets/screens/{key}-dark-1400x900.webp" alt="" width="320" height="206" loading="lazy">'
            f'<h3 class="h3">{esc(item["name"])}</h3>'
            f'<p class="small">{esc(item["blurb"])}</p>'
            "</a>"
        )

    body = f"""
<section class="wrap stack">
<h1>A tour of GroupLab</h1>
<p class="lead">Every screen, what it is for, and what you would do on it. Ten pages, one per screen, so you can see what using GroupLab is like before you download it.</p>
<p class="small faint">The pictures are regenerated every week from the newest build, so what you see here is the version you would install. Every sheet and every result in them is generated: no real target and nobody's photographs.</p>
<div class="research-grid">{"".join(cards)}</div>
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

    parts = "".join(
        f'<li><strong>{esc(label)}.</strong> {text}</li>' for label, text in item["parts"]
    )
    steps = "".join(f"<li>{esc(step)}</li>" for step in item["steps"])
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
<p class="small faint"><a class="plain" href="/tour/">Tour</a> &rsaquo; {esc(item["name"])}</p>
<h1>{esc(item["name"])}</h1>
<p class="lead">{esc(item["blurb"])}</p>
{tour_shot(key, item["name"] + " in GroupLab: " + item["blurb"], eager=True)}
<p class="small faint">From the newest build of GroupLab, regenerated every week. Tap the picture for it full size.</p>
<h2>What this screen is for</h2>
<p>{esc(item["purpose"])}</p>
<h2>Without a GroupLab sheet</h2>
<p>{esc(item["withoutASheet"])} <a href="/what-can-be-measured/">What GroupLab can measure</a>.</p>
<h2>What you are looking at</h2>
<ul class="tour-parts">{parts}</ul>
<h2>What you would do here</h2>
<ol>{steps}</ol>
<h2>Where it fits</h2>
<p>{item["fits"]}</p>
{f'<h2>Read more</h2><ul>{links}</ul>' if links else ""}
<nav class="tour-around" aria-label="Other screens">{" ".join(around)}</nav>
</article>
</section>
"""
    return shell(f"/tour/{key}/", item["name"], item["blurb"], body, "Tour")


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
<p class="small">A photograph of a target can carry the place it was taken. Take the location data off it before posting, or send it through <a href="/send/">the submission page</a>, which strips that before anybody sees the file. Nothing said on the server is legal advice, and no part of GroupLab is.</p>
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
img.logo.only-light,img.logo.only-dark{display:inline-block}

*,*::before,*::after{box-sizing:border-box}
html{-webkit-text-size-adjust:100%;scroll-padding-top:88px}
body{margin:0;background:var(--bg);color:var(--text);font-family:"IBM Plex Sans",system-ui,-apple-system,"Segoe UI",sans-serif;font-size:17px;line-height:1.6;-webkit-font-smoothing:antialiased}
img{max-width:100%;height:auto}
a{color:var(--amber);text-decoration:none;font-weight:500}
a:hover{text-decoration:underline}
:focus-visible{outline:2px solid var(--focus);outline-offset:3px;border-radius:2px}
code,.mono{font-family:"IBM Plex Mono",ui-monospace,Consolas,monospace}
code{font-size:.88em;background:var(--panel2);padding:1px 6px;border-radius:3px}
h1,h2,.display{font-family:"IBM Plex Sans Condensed","IBM Plex Sans",sans-serif;font-weight:700;letter-spacing:-.01em;line-height:1.08;margin:0;color:var(--text)}
h1{font-size:clamp(36px,5vw,52px)}
.display{font-size:clamp(38px,6vw,64px);line-height:1.04}
h2{font-size:clamp(28px,3.4vw,40px);line-height:1.1}
h3,.h3{font-family:"IBM Plex Sans",sans-serif;font-size:19px;font-weight:600;letter-spacing:0;line-height:1.3;margin:0}
p{margin:0;color:var(--dim)}
p.text,.text p,.text{color:var(--text)}
.dim{color:var(--dim)}.faint{color:var(--faint)}.teal{color:var(--teal)}.amber{color:var(--amber)}
.small{font-size:13px}.caps{letter-spacing:.1em;text-transform:uppercase}
.wrap{max-width:1280px;margin:0 auto;padding-left:80px;padding-right:80px}
.skip{position:absolute;left:-9999px;top:8px;background:var(--amber);color:var(--on-amber);padding:10px 14px;border-radius:4px;z-index:10}
.skip:focus{left:16px}

/* header */
.site-header{position:sticky;top:0;z-index:5;background:var(--bg);border-bottom:1px solid var(--line)}
.header-row{height:72px;display:flex;align-items:center;justify-content:space-between;gap:24px}
.brand{display:flex;align-items:center}
.logo{height:30px;width:auto;display:block}
.logo.small{height:22px}
.nav{display:flex;align-items:center;gap:32px;margin-left:auto}
.nav a{color:var(--dim);font-size:15px;padding:10px 0;border-bottom:2px solid transparent}
.nav a:hover{color:var(--text);text-decoration:none}
.nav a[aria-current="page"]{color:var(--text);border-bottom-color:var(--amber)}
.nav .gh{display:flex;align-items:center;gap:8px}
.header-tools{display:flex;align-items:center;gap:8px}
.theme-toggle{width:44px;height:44px;display:flex;align-items:center;justify-content:center;background:transparent;border:1px solid var(--line2);border-radius:4px;color:var(--dim);cursor:pointer}
.theme-toggle:hover{color:var(--text);border-color:var(--dim)}
.menu{display:none;position:relative}
.menu summary{list-style:none;width:44px;height:44px;display:flex;align-items:center;justify-content:center;cursor:pointer;color:var(--text);border:1px solid var(--line2);border-radius:4px}
.menu summary::-webkit-details-marker{display:none}
.menu-panel{position:absolute;right:0;top:52px;min-width:220px;display:flex;flex-direction:column;background:var(--panel);border:1px solid var(--line2);border-radius:6px;padding:8px;box-shadow:var(--shadow)}
.menu-panel a{padding:12px 14px;color:var(--text);border-radius:4px}
.menu-panel a:hover{background:var(--panel2);text-decoration:none}
.menu-panel a[aria-current="page"]{color:var(--amber)}

/* type blocks */
.eyebrow{font-family:"IBM Plex Mono",monospace;font-size:12px;letter-spacing:.12em;text-transform:uppercase;color:var(--teal);font-weight:400}
.eyebrow.amber{color:var(--amber)}
.lead{font-size:clamp(17px,1.6vw,20px);line-height:1.6;max-width:780px}
.fine{font-size:13px;color:var(--faint)}
.stack{display:flex;flex-direction:column;gap:20px}
.stack.tight{gap:12px}
.narrow{max-width:760px}

/* buttons */
.actions{display:flex;flex-wrap:wrap;gap:14px;align-items:center}
.actions.col{flex-direction:column;align-items:flex-start}
.btn{display:inline-flex;flex-direction:column;justify-content:center;gap:2px;min-height:44px;padding:11px 18px;border-radius:4px;font-size:15px;font-weight:600;line-height:1.3}
.btn:hover{text-decoration:none}
.btn-big{padding:16px 24px;font-size:17px}
.btn-primary{background:var(--amber);color:var(--on-amber)}
.btn-primary:hover{filter:brightness(1.08)}
.btn-secondary{border:1px solid var(--line2);color:var(--text);font-weight:500}
.btn-secondary:hover{border-color:var(--dim)}
.btn-sub{font-size:12px;font-weight:400;opacity:.85}

/* panels */
.panel{background:var(--panel);border:1px solid var(--line);border-radius:6px}
.pad{padding:28px}
.card-rec{border-color:var(--amber)}
.note{display:flex;gap:16px;align-items:baseline;padding:18px 24px;border-radius:6px}
.note-teal{background:var(--teal-tint);border:1px solid var(--teal-tint-b)}
.note-teal .mono{color:var(--teal);font-size:13px;flex-shrink:0}
.note p{color:var(--text);font-size:15px}
.callout{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:64px;row-gap:24px;align-items:center;padding:48px 56px;background:var(--amber-tint);border:1px solid var(--amber-tint-b);border-radius:8px}
.callout h2{font-size:clamp(26px,3vw,36px)}
.small-callout{display:flex;flex-direction:column;align-items:flex-start;gap:16px;padding:28px}

/* layout */
.section{padding-top:120px;display:flex;flex-direction:column;gap:40px}
.section-sm{padding-top:40px}
.last{padding-bottom:120px}
.page-head{padding-top:72px;padding-bottom:40px;display:flex;flex-direction:column;gap:20px}
.two-col{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:80px;row-gap:40px;align-items:start}
.two-col.bottom{align-items:end}
.grid-2{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:32px}
.grid-3{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}
.row-between{display:flex;justify-content:space-between;align-items:center;gap:24px}
.row-between a{flex-shrink:0}

/* home */
.hero{padding-top:88px;display:flex;flex-direction:column;gap:48px}
.hero-text{display:flex;flex-direction:column;gap:24px;max-width:880px}
.hero-shot{margin:0;display:flex;flex-direction:column;gap:14px}
.shot{display:block;width:100%;border:1px solid var(--line2);border-radius:6px;box-shadow:var(--shadow)}
.figure-panel{display:flex;flex-direction:column}
.figure-top{padding:32px 32px 28px;border-bottom:1px solid var(--line);display:flex;flex-direction:column;gap:10px}
.big-figure{font-size:clamp(34px,4vw,48px);font-weight:500;letter-spacing:-.02em;color:var(--text);margin:0}
.figure-list{margin:0;padding:24px 32px;display:flex;flex-direction:column;gap:18px}
.figure-list div{display:flex;gap:16px}
.figure-list dt{font-size:13px;color:var(--amber);width:72px;flex-shrink:0;padding-top:2px}
.figure-list dd{margin:0;font-size:15px;color:var(--text)}
.steps{list-style:none;margin:0;padding:0;display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}
.steps li{padding:24px;display:flex;flex-direction:column;gap:12px}
.steps .num{font-size:13px;color:var(--amber)}
.steps h3{font-size:18px}
.steps p{font-size:15px}
.fig{margin:0;display:flex;flex-direction:column;gap:16px}
.fig figcaption{display:flex;flex-direction:column;gap:6px}
.fig figcaption strong{font-size:16px;font-weight:600;color:var(--text)}
.fig figcaption span{font-size:15px;color:var(--dim)}
.status{padding:32px;display:flex;flex-direction:column;gap:16px;align-self:start}
.status p{font-size:15px}

/* download */
.card{padding:28px;display:flex;flex-direction:column;gap:14px}
.card-head{display:flex;justify-content:space-between;align-items:center;gap:12px}
.card-head .h3{font-size:20px}
.card p{font-size:15px}
.card-foot{margin-top:auto;padding-top:8px}
.badge{font-size:11px;letter-spacing:.1em;text-transform:uppercase;padding:4px 8px;border-radius:3px;background:var(--amber-tint);border:1px solid var(--amber-tint-b);color:var(--amber)}
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
.tab{font-size:14px;padding:8px 14px;border-radius:4px;color:var(--dim)}
.tab[aria-current="page"]{background:var(--panel2);color:var(--text);font-weight:600}
.tab:hover{text-decoration:none;color:var(--text)}
.pdf-link{margin-left:auto;font-size:14px}
/* Research, entry 142. The cards are a plain grid that collapses to one column on a phone, and the
   thumbnail is the article's lead chart, so the index reads as a contents page rather than a list. */
.research-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:16px;margin:12px 0 32px}
.research-card{display:block;transition:border-color .15s ease}
.research-card:hover{border-color:var(--accent)}
/* Entry 143 section 1.1: every card's lead is the same shape, whether it is a chart or a plate, so the row is
   even. Without this the one card with a real figure set the height of the whole row. */
.research-thumb{width:100%;aspect-ratio:320/200;object-fit:cover;border-radius:4px;background:var(--sunk);margin-bottom:8px}
.research-fig{max-width:100%;height:auto;border-radius:4px}
/* Entry 143 section 1.1: the lead for an article with no chart. Same shape as a thumbnail so the row is even,
   and drawn in the site's own colours so it follows the theme with no second image to keep in step. */
.research-plate{aspect-ratio:320/200;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;background:var(--sunk);border:1px solid var(--line);color:var(--faint)}
.plate-rule{width:42px;height:2px;background:var(--amber);opacity:.8}
.plate-number{font-size:30px;color:var(--dim);line-height:1}
.research-box{border-left:3px solid var(--accent);margin:20px 0 28px}

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
.prose{flex-grow:1;min-width:0;max-width:780px;display:flex;flex-direction:column;gap:18px}
.prose h1{font-size:clamp(32px,4vw,44px);margin-bottom:6px}
.prose h2{font-size:28px;margin-top:28px}
.prose h3{margin-top:12px}
.prose p,.prose li{color:var(--text);font-size:17px;line-height:1.7}
.prose ul,.prose ol{margin:0;padding-left:24px}
.prose li{margin-bottom:6px}
.prose table{border-collapse:collapse;width:100%;font-size:15px}
.prose th,.prose td{border-bottom:1px solid var(--line);padding:10px 12px;text-align:left;vertical-align:top}
.prose th{color:var(--dim);font-weight:600}
.guide-fig{margin:8px 0;display:flex;flex-direction:column;gap:10px}
a.plain{color:var(--text)}

/* footer */
.site-footer{border-top:1px solid var(--line);padding:40px 0 48px}
.footer-row{display:flex;justify-content:space-between;align-items:flex-start;gap:48px}
.footer-about{display:flex;flex-direction:column;gap:14px;max-width:440px}
.footer-about p{font-size:14px}
.footer-links{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:64px;row-gap:10px}
.footer-links a{color:var(--dim);font-size:14px;font-weight:400}
.footer-links a:hover{color:var(--text)}
.footer-base{margin-top:32px;font-size:13px;color:var(--faint)}

.pdf-row .btn{white-space:nowrap;flex-shrink:0}
.small-callout h2{font-family:"IBM Plex Sans",sans-serif;font-size:19px;letter-spacing:0}
.two-col.bottom{align-items:center}

/* narrower screens */
@media (max-width:1100px){
.wrap{padding-left:40px;padding-right:40px}
.nav{gap:22px}
.steps{grid-template-columns:repeat(2,minmax(0,1fr))}
.grid-3{grid-template-columns:repeat(1,minmax(0,1fr))}
.toc{display:none}
}
@media (max-width:860px){
.wrap{padding-left:16px;padding-right:16px}
.nav{display:none}
.menu{display:block}
.header-row{height:60px}
.logo{height:24px}
.hero{padding-top:40px;gap:32px}
.section{padding-top:72px}
.last{padding-bottom:72px}
.page-head{padding-top:40px}
.two-col,.grid-2,.callout{grid-template-columns:repeat(1,minmax(0,1fr))}
.callout{padding:28px 24px}
.steps{grid-template-columns:repeat(1,minmax(0,1fr))}
.footer-row{flex-direction:column}
.row-between{flex-direction:column;align-items:flex-start}
.pdf-row{flex-wrap:wrap}
.guide-bar{flex-wrap:wrap}
.pdf-link{margin-left:0}
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
"""

# The stylesheet and the script are fingerprinted by fingerprint() with everything else now,
# so there is no hand-rolled version to keep in step with them.

# ---------------------------------------------------------------- fingerprints and checks

# What gets a content hash in its URL. Entry 128 section 1.4: not only the stylesheet and the
# script, but the screenshots, the PDFs and the fonts too, so Cloudflare can never serve a
# stale file after a publish. A hash in the query string is enough; the file keeps its name,
# which matters for a PDF somebody saves.
FINGERPRINTED = {".woff2", ".webp", ".png", ".svg", ".pdf", ".ico", ".css", ".js"}

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
SEND_JS = """(function () {
  var form = document.getElementById('send');
  if (!form) { return; }
  var button = document.getElementById('sendButton');
  var status = document.getElementById('sendStatus');

  function say(text, bad) {
    status.textContent = text;
    status.className = 'small ' + (bad ? 'warn' : 'teal');
  }

  form.addEventListener('submit', function (e) {
    e.preventDefault();

    var photos = document.getElementById('photos');
    if (!photos.files || photos.files.length === 0) {
      say('Choose at least one photo first.', true);
      return;
    }
    if (!document.getElementById('consent').checked) {
      say('Please read and tick the consent box. It is the one thing that is not optional.', true);
      document.getElementById('consent').focus();
      return;
    }

    button.disabled = true;
    say('Sending. Large photos over a phone connection can take a few minutes, so please leave this page open.', false);

    var data = new FormData(form);
    fetch(form.action, { method: 'POST', body: data })
      .then(function (r) { return r.json().catch(function () { return { ok: false, error: 'The server answered with something this page could not read.' }; }); })
      .then(function (r) {
        if (r && r.ok) {
          form.innerHTML = '<h2 class=\"h3\">Thank you.</h2>' +
            '<p>Your photos are in. Reference <span class=\"mono\">' + (r.id || '') + '</span>, which you do not need to keep.</p>' +
            '<p class=\"small faint\">Nothing else happens now: no email, no account, no follow up. ' +
            '<a href=\"/shoot-a-target/\">Back to shooting a target</a>.</p>';
          form.scrollIntoView({ block: 'start' });
          return;
        }
        button.disabled = false;
        say((r && r.error) || 'That did not go through. Please try again.', true);
        if (window.turnstile) { window.turnstile.reset(); }
      })
      .catch(function () {
        button.disabled = false;
        say('That did not go through, and it looks like the connection dropped. Your photos are still on your phone; please try again.', true);
        if (window.turnstile) { window.turnstile.reset(); }
      });
  });
})();
"""


RECEIVERS = ["api/upload.php", "api/crash-report.php"]


def php_problems() -> list[str]:
    found = sorted(p.relative_to(OUT).as_posix() for p in OUT.rglob("*.php"))
    stray = [p for p in found if p not in RECEIVERS]
    return [f"{p}: a .php file that is not one of the receivers, and nothing else may be executable" for p in stray]


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
    write("assets/css/site.css", CSS.strip() + "\n")
    write("assets/js/theme.js", JS)

    write("index.html", page_home())
    write("download/index.html", page_download())
    write("shoot-a-target/index.html", page_shoot())
    # Entry 129: the page and the receiver go up together or not at all. A form posting to a path the server does
    # not serve yet takes somebody's photographs, spends their upload and tells them nothing useful.
    if limits().get("open"):
        write("shoot-a-target/send/index.html", page_send())
        # Section 3.4: the receivers are part of the site build, so they are versioned here, signed and delivered
        # by the same pipeline as the pages rather than copied to the server by hand.
        copy(need(REPO / "website" / "api" / "upload.php"), "api/upload.php")
        copy(need(REPO / "website" / "api" / "crash-report.php"), "api/crash-report.php")
        write("assets/js/send.js", SEND_JS)
    write("support/index.html", page_support())
    write("guides/index.html", page_guides_index())
    for g in GUIDES:
        write(f"guides/{g[0]}/index.html", page_guide(*g))
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
    for key in tour()["order"]:
        write(f"tour/{key}/index.html", page_tour_screen(key))
    write("discord/index.html", page_discord())
    write("404.html", page_404())

    pages = ["/", "/download/", "/tour/", "/shoot-a-target/", "/guides/", "/guides/user-guide/", "/guides/testing-guide/", "/releases/", "/support/"]
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

    problems += figure_problems + research_problems() + tour_problems() + figure_theme_problems() + limit_problems()
    problems += link_problems()
    problems += php_problems()
    if problems:
        print("\n".join(problems))
        sys.exit("build: checks failed")

    total = sum(f.stat().st_size for f in OUT.rglob("*") if f.is_file())
    count = sum(1 for f in OUT.rglob("*") if f.is_file())
    print(f"build: ok, {count} files, {total / 1024 / 1024:.1f} MB in {OUT}")


if __name__ == "__main__":
    main()
