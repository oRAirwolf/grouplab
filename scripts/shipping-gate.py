#!/usr/bin/env python3
"""Did anything that ships inside the executable change?

NOTES-FROM-PLANNING.md entry 150. Alan, reading the releases page: "it seems like a lot of builds and
releases are extremely minor, like just updating release note pages being brought up to date or research
articles were written. Why does this need a new executable?" He is right. Nightly 84 was website changes
and nothing else, and it compiled and tested a new executable on three operating systems to produce it.

So a build is for a change to the application. Everything else is content and publishes through the site
path entry 144 built. `.github/shipping-paths.json` is the one list of which is which, read here and by
`ShippingPathsTests`, so the gate and the rule cannot drift apart.

    python3 scripts/shipping-gate.py --decide <base> <head>   yes or no, with the paths that decided it
    python3 scripts/shipping-gate.py --lists                  every top level entry is in exactly one list
    python3 scripts/shipping-gate.py --history 30             what the gate would have done to the last N nightlies

**A path in neither list fails.** A new top level directory should make somebody decide which side it is
on, rather than silently picking one and either wasting a build every time or shipping something
untested.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent.parent
PATHS = HERE / ".github" / "shipping-paths.json"


def lists() -> tuple[dict[str, str], dict[str, str]]:
    data = json.loads(PATHS.read_text(encoding="utf-8"))
    return data["ships"], data["content"]


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=HERE, capture_output=True, text=True, check=True).stdout.strip()


def side(path: str) -> str | None:
    """Which list a changed path falls on, by its first segment, or None if the lists do not say."""
    ships, content = lists()
    first = path.split("/", 1)[0]
    if first in ships:
        return "ships"
    if first in content:
        return "content"
    return None


def decide(base: str, head: str) -> int:
    """yes when something shipping changed between the two commits, no when only content did."""
    changed = [p for p in git("diff", "--name-only", f"{base}..{head}").splitlines() if p]
    if not changed:
        print("application-changed=no")
        print("nothing changed at all between those two commits")
        return 0

    unknown = sorted({p.split("/", 1)[0] for p in changed if side(p) is None})
    if unknown:
        print("application-changed=fail", file=sys.stderr)
        for name in unknown:
            print(f"{name} is in neither list in .github/shipping-paths.json", file=sys.stderr)
        print("Decide which side it is on and add it. A path in neither list is a failure, not a default.",
              file=sys.stderr)
        return 2

    shipping = [p for p in changed if side(p) == "ships"]
    print(f"application-changed={'yes' if shipping else 'no'}")
    if shipping:
        for p in shipping[:20]:
            print(f"  {p}")
        if len(shipping) > 20:
            print(f"  and {len(shipping) - 20} more")
    else:
        print(f"  {len(changed)} changed path{'' if len(changed) == 1 else 's'}, all of them content")
    return 0


def check_lists() -> int:
    """Every top level entry in the repository is in exactly one list, and no list names something absent."""
    ships, content = lists()
    here = {p for p in git("ls-tree", "--name-only", "HEAD").splitlines() if p}
    faults = []

    both = sorted(set(ships) & set(content))
    for name in both:
        faults.append(f"{name} is in both lists")

    for name in sorted(here - set(ships) - set(content)):
        faults.append(f"{name} is in neither list")

    for name in sorted((set(ships) | set(content)) - here):
        faults.append(f"{name} is listed but is not in the repository")

    for fault in faults:
        print(fault, file=sys.stderr)

    if faults:
        print("Fix .github/shipping-paths.json. A path in neither list is a failure, not a default.", file=sys.stderr)
        return 1

    print(f"{len(here)} top level entries, {len(ships)} shipping and {len(content)} content, each in exactly one list.")
    return 0


def history(count: int) -> int:
    """What the gate would have done to the last N nightlies, against the real history rather than a fixture."""
    tags = git("tag", "--list", "v*-nightly.*", "--sort=-creatordate").splitlines()[:count + 1]
    if len(tags) < 2:
        print("fewer than two nightly tags here, so there is nothing to compare", file=sys.stderr)
        return 0

    skipped = 0
    for newer, older in zip(tags, tags[1:]):
        try:
            a, b = git("rev-list", "-n1", older), git("rev-list", "-n1", newer)
        except subprocess.CalledProcessError:
            continue
        changed = [p for p in git("diff", "--name-only", f"{a}..{b}").splitlines() if p]
        unknown = sorted({p.split("/", 1)[0] for p in changed if side(p) is None})
        shipping = [p for p in changed if side(p) == "ships"]
        verdict = "FAIL, unlisted: " + ", ".join(unknown) if unknown else ("built" if shipping else "SKIPPED")
        if verdict == "SKIPPED":
            skipped += 1
        print(f"{newer:<28} {len(changed):>4} changed  {verdict}")

    print(f"\n{skipped} of the last {len(tags) - 1} nightlies would not have been built.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Did anything that ships inside the executable change?")
    parser.add_argument("--decide", nargs=2, metavar=("BASE", "HEAD"))
    parser.add_argument("--lists", action="store_true")
    parser.add_argument("--history", type=int, metavar="N")
    args = parser.parse_args()

    if args.decide:
        return decide(*args.decide)
    if args.lists:
        return check_lists()
    if args.history:
        return history(args.history)

    parser.print_help()
    return 1


if __name__ == "__main__":
    sys.exit(main())
