#!/usr/bin/env python3
"""Entry 436: the guides' contents list, their ids and the checks that hold them.

    python tests/python/site-guide-tests.py

Runs before the site builds in website.yml. It imports website/build.py, renders made-up guides and the real user guide, and drives
guide_problems() over pages written to a temporary folder: no network, nothing published.
"""
import importlib.util
import re
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


def page(toc_ids: list[str], heads: list[tuple[int, str]], extra: str = "") -> str:
    side = "".join(f'<a href="#{i}">x</a>' for i in toc_ids)
    body = "".join(f'<h{level} id="{i}">x</h{level}>' for level, i in heads)
    return f'<main id="main"><nav class="toc" aria-label="On this page">{side}</nav><article class="prose">{body}{extra}</article></main>'


def main() -> int:
    tmp = Path(tempfile.mkdtemp(prefix="grouplab-guide-"))
    try:
        # Rendering: the h2 ids are the slugs they always were, a repeated h3 carries its section's number, and the list nests.
        md = tmp / "g.md"
        md.write_text("# T\n\n## 4. Mark it\n\n### Several targets open at once\n\nA.\n\n## 13. On the phone\n\n"
                      "### Several targets open at once\n\nB.\n\n## Mil or MOA\n\n### Capture\n\n### Capture\n", encoding="utf-8")
        _, body, toc = build.render_guide(md, marks=True)
        check("an h2 keeps its slug as its id", '<h2 id="4-mark-it">' in body)
        check("the first of two same h3s keeps its slug", 'id="several-targets-open-at-once"' in body)
        check("the later one carries its section's number", 'id="13-several-targets-open-at-once"' in body)
        check("a section with no number puts its own id in front", 'id="mil-or-moa-capture"' in body)
        check("the list nests each section's subsections under it",
              [(h, [s for s, _ in subs]) for h, _, subs in toc] == [("4-mark-it", ["several-targets-open-at-once"]),
                                                                  ("13-on-the-phone", ["13-several-targets-open-at-once"]),
                                                                  ("mil-or-moa", ["capture", "mil-or-moa-capture"])])
        check("every heading has its link mark", body.count('class="hlink"') == 7)
        _, plain, _ = build.render_guide(md)
        check("a page that did not ask for marks has none", "hlink" not in plain)
        nested = build.toc_list(toc)
        check("the list is all in the HTML", all(f'href="#{h}"' in nested for h in re.findall(r'id="([^"]+)"', body)))

        # The real guide: every h2 id is exactly the slug of its line, as features.json and every link sent already expect.
        guide = (REPO / "docs" / "USER-GUIDE.md").read_text(encoding="utf-8")
        _, real, real_toc = build.render_guide(REPO / "docs" / "USER-GUIDE.md", marks=True)
        check("the user guide's h2 ids did not change",
              [h for h, _, _ in real_toc] == [build.slug(h) for h in re.findall(r"^## (.+?)\r?$", guide, re.M)])
        check("the user guide has subsections", any(subs for _, _, subs in real_toc))
        ids = build.guide_ids(real)
        check("every id in the user guide is used once", len(ids) == len(set(ids)), str(sorted({i for i in ids if ids.count(i) > 1})))

        # guide_problems over built pages.
        def problems(user: str, testing: str | None = None) -> list[str]:
            out = tmp / "out"
            shutil.rmtree(out, ignore_errors=True)
            for key, text in (("user-guide", user), ("testing-guide", testing or page(["a"], [(2, "a")]))):
                (out / "guides" / key).mkdir(parents=True)
                (out / "guides" / key / "index.html").write_text(text, encoding="utf-8")
            return build.guide_problems(out)

        good = page(["a", "a-1", "b"], [(2, "a"), (3, "a-1"), (2, "b")], '<a href="#main">skip</a>')
        check("a whole guide passes", problems(good) == [], str(problems(good)))
        dup = page(["a", "x", "b"], [(2, "a"), (3, "x"), (2, "b"), (3, "x")])
        check("a repeated id fails", any("'x' is on more than one" in p for p in problems(dup)))
        missing = page(["a", "gone"], [(2, "a"), (3, "a-1")])
        check("a contents link with no heading fails", any("#gone has no heading" in p for p in problems(missing)))
        check("a contents list short of a heading fails",
              any("does not name every heading" in p for p in problems(page(["a"], [(2, "a"), (3, "a-1")]))))
        check("a user guide with no subsections fails",
              any("no subsections" in p for p in problems(page(["a"], [(2, "a")]))))
        check("a problem names its page, so only that page is held back",
              all(build.pages_of(p, tmp / "out") == ["guides/user-guide/index.html"] for p in problems(dup)))

        # features.json: a guide link to a subsection is accepted, one to nothing is not.
        anchors = set(ids)
        links = [f["guide"] for f in build.features()["features"] if f.get("guide")]
        check("every Features link into the guide has its id", all(l in anchors for l in links),
              str([l for l in links if l not in anchors]))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    print(f"{passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
