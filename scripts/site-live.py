"""Whether grouplab.org is serving a commit yet (NOTES-FROM-PLANNING.md entry 462 section 5).

CLAUDE.md asks that a push which publishes is confirmed live within 20 minutes from the grouplab-site-build meta tag, and reported with the
evidence after 30. That was a curl typed by hand and read by eye; this asks the page once a minute until the commit is there or the time is
up, and says which, with the commit the site does serve.

    python scripts/site-live.py [<commit>] [--minutes 30]

The commit defaults to origin/main. Exit 0 when live, 1 when the time ran out, 2 when the page could not be read at all.
"""

from __future__ import annotations

import re
import subprocess
import sys
import time
import urllib.request

PAGE = "https://grouplab.org/"


def serving() -> str | None:
    request = urllib.request.Request(PAGE, headers={"User-Agent": "GroupLab site-live check", "Cache-Control": "no-cache"})
    with urllib.request.urlopen(request, timeout=30) as response:
        found = re.search(r'name="grouplab-site-build" content="([0-9a-f]+)"', response.read().decode("utf-8", "replace"))
    return found.group(1) if found else None


def main(argv: list[str]) -> int:
    minutes = float(argv[argv.index("--minutes") + 1]) if "--minutes" in argv else 30.0
    given = [a for i, a in enumerate(argv) if not a.startswith("--") and (i == 0 or argv[i - 1] != "--minutes")]
    commit = given[0] if given else subprocess.run(["git", "rev-parse", "origin/main"], capture_output=True, text=True, check=True).stdout.strip()
    deadline = time.monotonic() + (minutes * 60)
    last = None
    while True:
        try:
            last = serving()
        except OSError as error:
            if time.monotonic() >= deadline:
                print(f"site: the page could not be read ({error})")
                return 2
        if last and (last.startswith(commit) or commit.startswith(last)):
            print(f"site: live at {last[:8]}")
            return 0
        if time.monotonic() >= deadline:
            print(f"site: not live after {minutes:g} minutes; it serves {last[:8] if last else 'no build tag'}, waiting for {commit[:8]}")
            return 1
        time.sleep(60)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
