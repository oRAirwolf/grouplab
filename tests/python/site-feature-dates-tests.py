#!/usr/bin/env python3
"""Entry 421: a feature widened on main says its newest part is coming, and names the build that part arrived in once it has.

    python3 tests/python/site-feature-dates-tests.py

Runs before the site builds in website.yml. It imports website/build.py and gives it made-up release notes: no network, nothing published.
"""
import importlib.util
import sys
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
    sections = {"0.2.0-nightly.113": "0.2.0-nightly.113\n- You can now pool a set.", "0.2.0-nightly.190": "0.2.0-nightly.190\n- Other things."}
    build.release_sections = lambda: sections
    pooled = {"since": "next", "note": ["pool", "opens your pooled set in its own tab"]}
    check("a widened feature still arrived in its first build", build.arrived(pooled) == "0.2.0-nightly.113", str(build.arrived(pooled)))
    check("its newest part is coming while no build says it", build.changed(pooled) == "next", str(build.changed(pooled)))
    check("the Features page says so", build.since_words(pooled) == "Since nightly.113; its newest part is coming in the next build",
          build.since_words(pooled))
    check("the home page's card says it is coming", build.spotlight_words(pooled) == "Coming in the next build", build.spotlight_words(pooled))

    sections["0.2.0-nightly.191"] = "0.2.0-nightly.191\n- Pool the chosen now opens your pooled set in its own tab."
    check("once a build says it, that is where it changed", build.changed(pooled) == "0.2.0-nightly.191", str(build.changed(pooled)))
    check("the Features page names both builds", build.since_words(pooled) == "Since nightly.113, changed in nightly.191",
          build.since_words(pooled))
    check("the home page's card names the newest", build.spotlight_words(pooled) == "Changed in nightly.191", build.spotlight_words(pooled))

    whole = {"since": "0.2.0-nightly.113", "note": "pool"}
    check("a feature with one part is new in its build", build.spotlight_words(whole) == "New in nightly.113", build.spotlight_words(whole))
    check("and says nothing of a change", build.since_words(whole) == "Since nightly.113", build.since_words(whole))
    fresh = {"since": "next", "note": "something nobody has published"}
    check("a feature in no build yet is coming", build.since_words(fresh) == build.spotlight_words(fresh) == "Coming in the next build")

    print(f"{passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
