#!/usr/bin/env python3
"""Entry 387: a site check holds back only what it is about, never the whole site.

    python3 tests/python/site-holdback-tests.py

Runs before the site builds in website.yml. It imports website/build.py and drives its settle() with made-up check results in a
temporary folder: no network, nothing published.
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
    out = Path(tempfile.mkdtemp(prefix="grouplab-holdback-"))
    try:
        for page in ("index.html", "features/index.html", "tour/index.html", "tour/capture/index.html", "releases/index.html",
                     "download/index.html", "research/how-many-shots/index.html"):
            (out / page).parent.mkdir(parents=True, exist_ok=True)
            (out / page).write_text("new " + page, encoding="utf-8")
        old = lambda page, live: ("old " + page).encode("utf-8")

        stale = ["the phone's screenshots are from nightly 115, 61 behind 176; the limit is 60. They are retaken in a device sitting."]
        stop, report = build.settle([], [], stale, out, "https://example.invalid", old)
        check("a stale phone screenshot does not stop the publish", not stop, str(report))
        check("and it is reported, one line", len(report) == 1 and report[0].startswith("published with a stale part: the screenshots"), str(report))
        check("and every page goes out new", all((out / p).read_text(encoding="utf-8").startswith("new ") for p in ("index.html", "releases/index.html", "download/index.html", "features/index.html")))

        stop, report = build.settle([], ["research/how-many-shots: front matter has no title"], [], out, "https://example.invalid", old)
        check("a page check holds back only its page, with the last published copy", not stop
              and (out / "research/how-many-shots/index.html").read_text(encoding="utf-8") == "old research/how-many-shots/index.html"
              and (out / "features/index.html").read_text(encoding="utf-8") == "new features/index.html", str(report))
        check("and says so in one line", any(l.startswith("held back: research/how-many-shots/index.html") for l in report), str(report))

        stop, report = build.settle([], ["website/tour.json: capture's picture is missing"], [], out, "https://example.invalid", old)
        check("a tour problem holds back the tour's pages and nothing else", not stop
              and (out / "tour/capture/index.html").read_text(encoding="utf-8").startswith("old ")
              and (out / "download/index.html").read_text(encoding="utf-8").startswith("new "), str(report))

        stop, report = build.settle([], ["releases/index.html: a heading is missing"], [], out, "https://example.invalid", old)
        check("the release notes are never held back", not stop and (out / "releases/index.html").read_text(encoding="utf-8").startswith("new ")
              and any(l.startswith("published with a stale part: releases/index.html") for l in report), str(report))

        stop, report = build.settle([], ["features/index.html: something"], [], out, None, lambda page, live: None)
        check("with no live copy to keep, the page publishes and the line says it is stale", not stop
              and any(l.startswith("published with a stale part: features/index.html") for l in report), str(report))

        stop, _ = build.settle(["download/index.html does not offer grouplab-android.apk, so its buttons will rot"], [], stale, out, None, old)
        check("a check on the whole site's truth still stops everything", stop)
        stop, _ = build.settle([], ["something with no page named"], [], out, None, old)
        check("and a page check that names no page counts as the whole site's", stop)
    finally:
        shutil.rmtree(out, ignore_errors=True)

    print(f"site hold-back tests: {passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
