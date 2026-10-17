#!/usr/bin/env python3
"""Entry 440 section 1.2: the concept B page at /testpage/ stays out of sight until Alan approves it.

    python tests/python/site-testpage-tests.py

Runs before the site builds in website.yml. It imports website/build.py, renders the page, and drives testpage_problems() over made-up
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
    page = build.page_testpage()
    check("the page asks not to be indexed or followed", '<meta name="robots" content="noindex, nofollow">' in page)
    check("the navigation does not name it", all(href != build.TESTPAGE_PATH for _, href in build.NAV + build.nav()))
    check("it carries the site build's commit, so its publishing can be confirmed", 'name="grouplab-site-build"' in page)
    check("the home page is not the test page", build.TESTPAGE_PATH not in build.page_home() and "tp-hero" not in build.page_home())

    tmp = Path(tempfile.mkdtemp(prefix="grouplab-testpage-"))
    try:
        def problems(test: str, sitemap: str = "<urlset></urlset>", other: str = "<a href=\"/download/\">x</a>") -> list[str]:
            out = tmp / "out"
            shutil.rmtree(out, ignore_errors=True)
            (out / "testpage").mkdir(parents=True)
            (out / "testpage" / "index.html").write_text(test, encoding="utf-8")
            (out / "index.html").write_text(other, encoding="utf-8")
            (out / "sitemap.xml").write_text(sitemap, encoding="utf-8")
            return build.testpage_problems(out)

        check("the page as built passes", problems(page) == [], str(problems(page)))
        check("a page without its robots tag fails", any("robots" in p for p in problems(page.replace('content="noindex, nofollow"', 'content="all"'))))
        check("a sitemap listing it fails", any(p.startswith("sitemap.xml") for p in problems(page, "<loc>https://grouplab.org/testpage/</loc>")))
        check("another page linking to it fails", any(p.startswith("index.html") for p in problems(page, other='<a href="/testpage/">new</a>')))
        check("a full address to it fails too", any(p.startswith("index.html") for p in problems(page, other='<a href="https://grouplab.org/testpage/">new</a>')))
        check("a page not built at all fails", build.testpage_problems(tmp / "nowhere") != [])
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    print(f"{passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
