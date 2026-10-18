#!/usr/bin/env python3
"""Entry 441 section 3, entry 440's check turned around: concept B is the home page, indexed and in the sitemap, and /testpage/ only
points to it.

    python tests/python/site-home-tests.py

Runs before the site builds in website.yml. It imports website/build.py, renders the pages, and drives home_problems() over made-up
output in a temporary folder: no network, nothing published.
"""
import importlib.util
import shutil
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("site_build", REPO / "website" / "build.py")
build = importlib.util.module_from_spec(spec)
spec.loader.exec_module(build)

passed, failed = 0, []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what)
        print("FAIL " + what + (": " + detail if detail else ""))


def main() -> int:
    home = build.page_home()
    moved = build.page_testpage_moved()
    check("the home page is concept B", "tp-hero" in home)
    check("the home page does not ask to stay out of search", 'name="robots"' not in home)
    check("it names its own address as canonical", f'<link rel="canonical" href="{build.SITE_URL}/">' in home)
    check("it carries the site build's commit, so its publishing can be confirmed", 'name="grouplab-site-build"' in home)
    check("the navigation does not name the old preview address", all(href != build.TESTPAGE_PATH for _, href in build.NAV + build.nav()))
    check("the old preview address is a link to the home page, not a copy", "tp-hero" not in moved and 'href="/"' in moved)
    # Entry 441 section 4: what the page before carried and a visitor still needs.
    for what, href in [("downloads", "/download/"), ("the guides", "/guides/"), ("support", "/support/"), ("Discord", build.DISCORD),
                       ("the release notes", "/releases/"), ("shooting a target", "/shoot-a-target/"), ("the hardware survey", "/survey/")]:
        check(f"the home page reaches {what}", f'href="{href}' in home)
    check("the home page still says what is not built yet", "Not built yet" in home and "may be broken" in home)

    tmp = Path(tempfile.mkdtemp(prefix="grouplab-home-"))
    try:
        good_map = f"<urlset><url><loc>{build.SITE_URL}/</loc></url></urlset>"

        def problems(page: str, sitemap: str = good_map, old: str | None = moved) -> list[str]:
            out = tmp / "out"
            shutil.rmtree(out, ignore_errors=True)
            out.mkdir(parents=True)
            (out / "index.html").write_text(page, encoding="utf-8")
            (out / "sitemap.xml").write_text(sitemap, encoding="utf-8")
            if old is not None:
                (out / "testpage").mkdir()
                (out / "testpage" / "index.html").write_text(old, encoding="utf-8")
            return build.home_problems(out)

        check("the pages as built pass", problems(home) == [], str(problems(home)))
        check("without the old address at all passes too", problems(home, old=None) == [])
        check("a home page asking not to be indexed fails",
              any("indexed" in p for p in problems(home.replace("<title>", '<meta name="robots" content="noindex, nofollow"><title>'))))
        check("a sitemap without the home page fails", any(p.startswith("sitemap.xml") for p in problems(home, "<urlset></urlset>")))
        check("a sitemap listing the old address fails",
              any(p.startswith("sitemap.xml") for p in problems(home, good_map.replace("</urlset>", f"<loc>{build.SITE_URL}/testpage/</loc></urlset>"))))
        check("a second copy of the home page at the old address fails", any(p.startswith("testpage/") for p in problems(home, old=home)))
        check("the page before concept B at / fails", any("concept B" in p for p in problems("<html><body>old</body></html>")))
        check("a home page not built at all fails", build.home_problems(tmp / "nowhere") != [])

        # Entry 443 section 2: one header and one footer on every page, with every link Alan asked back.
        out = tmp / "headers"
        shutil.rmtree(out, ignore_errors=True)
        (out / "support").mkdir(parents=True)
        (out / "index.html").write_text(home, encoding="utf-8")
        plain = build.shell("/support/", "Support", "A page.", "<p>words</p>", "Support")
        (out / "support" / "index.html").write_text(plain, encoding="utf-8")
        check("the home page and a plain page carry the header and footer with every link", build.header_problems(out) == [],
              str(build.header_problems(out)))
        top, rest = plain.split("</header>", 1)
        (out / "support" / "index.html").write_text(top.replace(f'href="{build.GITHUB}"', 'href="#"') + "</header>" + rest, encoding="utf-8")
        check("a header without its GitHub link fails", any("support/index.html" in p and build.GITHUB in p for p in build.header_problems(out)))
        (out / "support" / "index.html").write_text("<html><body><header class=\"site-header\"></header></body></html>", encoding="utf-8")
        check("a page with the old header fails", any("shared header" in p for p in build.header_problems(out)))

        # Entry 444 section 3: a page whose h1 is not the home page's display face fails the build.
        out = tmp / "headings"
        shutil.rmtree(out, ignore_errors=True)
        (out / "support").mkdir(parents=True)
        (out / "assets" / "css").mkdir(parents=True)
        site_css = build.served_css(build.CSS)
        (out / "assets" / "css" / "site.css").write_text(site_css, encoding="utf-8")
        page = build.shell("/support/", "Support", "A page.", "<section class=\"wrap page-head\"><h1>Title</h1></section>", "Support")
        (out / "support" / "index.html").write_text(page, encoding="utf-8")
        check("a page as built has its h1 in the display face", build.heading_problems(out) == [], str(build.heading_problems(out)))
        (out / "assets" / "css" / "site.css").write_text(site_css + ".page-head h1{font-family:\"IBM Plex Sans\",sans-serif}\n", encoding="utf-8")
        check("a rule giving the h1 the plain face fails", any(".page-head h1" in p for p in build.heading_problems(out)))
        (out / "assets" / "css" / "site.css").write_text(site_css + "@media (max-width:860px){.wrap h1{font-weight:600}}\n", encoding="utf-8")
        check("a rule at one width giving the h1 another weight fails", any(".wrap h1" in p for p in build.heading_problems(out)))
        (out / "assets" / "css" / "site.css").write_text(site_css + ".odd{font-family:serif}\n", encoding="utf-8")
        (out / "support" / "index.html").write_text(page.replace("<h1>", '<h1 class="odd">'), encoding="utf-8")
        check("a class on the h1 that changes its face fails", any(".odd" in p for p in build.heading_problems(out)))
        (out / "assets" / "css" / "site.css").write_text(site_css.replace(".tp h1,.tp h2{", ".tp h4{"), encoding="utf-8")
        (out / "support" / "index.html").write_text(page, encoding="utf-8")
        check("a stylesheet that no longer gives .tp h1 the display face fails", any("no stylesheet" in p for p in build.heading_problems(out)))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    # Entry 443 section 1: the home page's header has every link the header had before entry 441.
    head = home[home.index('<header class="tp-header'):home.index("</header>")]
    for label in ("Community", "Release notes", "Support", "GitHub"):
        check(f"the home page's header names {label}", f">{label}<" in head)
    plain = build.shell("/support/", "Support", "A page.", "<p>words</p>", "Support")
    check("the home page and a plain page take their header and footer from the one place",
          build.site_header(home=True) in home and build.site_header("Support") in plain
          and build.site_footer() in home and build.site_footer() in plain)
    check("a plain page has no orbit and no motion script", "tp-orbits" not in plain and "home.js" not in plain and "tp-motion" not in plain)
    check("a plain page marks where the reader is", '<a href="/support/" aria-current="page" class="nav-more">Support</a>' in plain)

    # Entry 444 sections 1 and 2.
    check("every page but the home page has the still rings behind its top band", build.PAGE_RINGS in plain and build.PAGE_RINGS not in home)
    check("the rings do not move", "tp-rings" not in build.HOME_CSS and "animation" not in build.CSS.split(".tp-rings{", 1)[1].split("\n.page-head", 1)[0])
    check("the bar shows every link from 1280 pixels, with More only from 981 to 1279",
          "@media (max-width:1279px) and (min-width:981px){" in build.CSS and "max-width:1499px) and (min-width:981px)" not in build.CSS)
    check("the home page's header is not see-through over the orbit", "backdrop-filter:blur" in build.HOME_CSS and "background:transparent" not in build.HOME_CSS)
    long = build.shell("/guides/user-guide/", "Guide", "A page.", "<h2>One</h2><p>a</p><h2 id=\"two\">Two</h2>", "Guides")
    check("a long page has the ruler and a section index with one link a section",
          "tp-ruler" in long and 'href="#one"' in long and 'href="#two"' in long and 'id="one"' in long and "page.js" in long)
    short = build.shell("/survey/", "Survey", "A page.", "<h2>One</h2><h2>Two</h2>")
    check("a short page has neither", "tp-ruler" not in short and "tp-index" not in short and "page.js" not in short)
    for path in ("/research/how-many-shots/", "/releases/", "/features/", "/support/", "/guides/testing-guide/"):
        check(f"{path} is a long page", bool(build.LONG_PAGES.match(path)))
    check("the glossary and the download page are not long pages", not build.LONG_PAGES.match("/guides/glossary/") and not build.LONG_PAGES.match("/download/"))

    print(f"{passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
