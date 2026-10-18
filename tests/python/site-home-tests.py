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
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    print(f"{passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
