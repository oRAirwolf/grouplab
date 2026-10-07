"""Whether the README, the website and the assets agree with the project, NOTES-FROM-PLANNING.md entry 267 section 2(a).

Alan: "the github readme, grouplab.org website and all of the assets need to be checked every so often to make sure they are up to date and
agree with what is currently happening in the project. I should not have to find these oversights because they should be analyzed."

It runs every check the repository already has for this, and a few of its own, and lists every finding in one place:

- the site builds, with its own checks: banned words, no IP address, every platform on /download/, every feature's phone status, every
  screenshot and generated picture current (entries 253, 256, 264, 265, 266);
- the README's generated sections, its platform statement and its claims are current (scripts/readme.py, platform-support.py, claims.py);
- every platform in the platform statement is in the README's Download table;
- the newest nightly named in the README is the newest in the release notes, and (online) the live site names it too;
- every feature on the Features page is named in the README's summary;
- no retired wording remains (docs/RETIRED-WORDING.json), such as a claim that something waits on a lawyer;
- (online) every external link in the README resolves, allowing for a temporary failure;
- every place that tells a person how to get GroupLab names the Microsoft Store and the iPhone beta, and every chronograph file the
  application reads is named in the README and the user guide (entry 342: the testing guide still knew only Android's routes).

    python scripts/consistency.py                findings, exit 1 when there are any
    python scripts/consistency.py --warn         the same as warnings, always exit 0 (the ordinary build)
    python scripts/consistency.py --offline      skip the checks that need the network
    python scripts/consistency.py --issue        also open, update or close the one GitHub issue labelled consistency (the weekly job)
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
README = REPO / "README.md"
NOTES = REPO / "docs" / "RELEASE-NOTES.md"
PLATFORMS = REPO / "docs" / "PLATFORM-SUPPORT.md"
FEATURES = REPO / "website" / "features.json"
RETIRED = REPO / "docs" / "RETIRED-WORDING.json"
TEXT = {".md", ".json", ".py", ".html", ".yml", ".txt", ".cs"}


def run(*args: str) -> tuple[int, str]:
    done = subprocess.run([sys.executable, *args], cwd=REPO, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return done.returncode, (done.stdout + done.stderr).strip()


def checks_that_exist() -> list[str]:
    found = []
    for label, args in [
        ("the site build", ["website/build.py"]),
        ("the README's generated sections", ["scripts/readme.py", "--check"]),
        ("the README's platform statement", ["scripts/platform-support.py", "--check"]),
        ("the published claims' backing", ["scripts/claims.py", "--check"]),
        ("the counted facts", ["scripts/counts.py", "--check"]),
    ]:
        code, out = run(*args)
        if code != 0:
            tail = [l for l in out.splitlines() if l.strip()][-6:]
            found.append(f"{label} fails: " + " | ".join(tail))
        # Entry 387 section 2: a page the site held back, or published with a stale part, is one line each, so it is not left unnoticed.
        found += [l.strip() for l in out.splitlines() if l.startswith(("held back: ", "published with a stale part: "))]
    return found


def guide_pdfs() -> list[str]:
    """The consistency audit of 2026-10-07, finding 8: a guide's PDF committed before its Markdown last changed is a PDF behind its guide."""
    found = []
    for md in ("USER-GUIDE.md", "TESTING-GUIDE.md"):
        pdf = md.replace(".md", ".pdf")
        times = []
        for name in (md, pdf):
            done = subprocess.run(["git", "log", "-1", "--format=%ct", "--", f"docs/{name}"], cwd=REPO, capture_output=True, text=True)
            times.append(int(done.stdout.strip() or 0))
        if times[0] and times[1] and times[0] > times[1]:
            found.append(f"docs/{pdf} is older than docs/{md}: run grouplab user-guide and commit the PDFs with the guide")
    return found


def newest() -> str:
    m = re.search(r"^## (\d+\.\d+\.\d+-nightly\.\d+)\s*$", NOTES.read_text(encoding="utf-8"), re.M)
    return m.group(1) if m else ""


def readme_checks() -> list[str]:
    found = []
    readme = README.read_text(encoding="utf-8")
    for name in re.findall(r"^\| \*\*([A-Za-z]+)\*\* \|", PLATFORMS.read_text(encoding="utf-8"), re.M):
        if f"| **{name}** |" not in readme:
            found.append(f"the platform statement lists {name}, and the README's Download table does not")
    stated = re.search(r"\*\*The newest build is ([^*]+)\*\*", readme)
    if stated is None or stated.group(1) != newest():
        found.append(f"the README names {stated.group(1) if stated else 'no build'} as the newest, and the release notes' newest is {newest()}")
    book = json.loads(FEATURES.read_text(encoding="utf-8"))
    for f in book["features"]:
        if f"#{f['key']})" not in readme:
            found.append(f"the feature {f['name']!r} is on the Features page and not named in the README's summary")
    return found


GETTING = [README, REPO / "docs" / "USER-GUIDE.md", REPO / "docs" / "TESTING-GUIDE.md"]
CHRONOGRAPH = REPO / "src" / "GroupLab.Core" / "Records" / "ChronographFiles.cs"


def routes() -> list[str]:
    """Where to get GroupLab, and which chronograph files it reads, said the same everywhere a person reads it."""
    site = (REPO / "website" / "build.py").read_text(encoding="utf-8")
    found = []
    for name in ("STORE", "TESTFLIGHT"):
        url = re.search(rf'^{name} = "([^"]+)"', site, re.M).group(1)
        for doc in GETTING:
            if url not in doc.read_text(encoding="utf-8"):
                found.append(f"{doc.relative_to(REPO).as_posix()} does not link {url}, which the download page offers")
    body = re.search(r"enum ChronographFormat\s*\{(.*?)\}", CHRONOGRAPH.read_text(encoding="utf-8"), re.S).group(1)
    readers = [n for n in re.findall(r"^\s*([A-Z][A-Za-z]+),", body, re.M) if n != "Generic"]
    for reader in readers:
        words = re.sub(r"(?<=[a-z])(?=[A-Z][a-z])", " ", reader) if reader.startswith("Garmin") else reader
        for doc in GETTING[:2]:
            if words.lower() not in doc.read_text(encoding="utf-8").lower():
                found.append(f"{doc.relative_to(REPO).as_posix()} does not name {words}, a chronograph file GroupLab reads")
    return found


def retired() -> list[str]:
    rules = json.loads(RETIRED.read_text(encoding="utf-8"))
    never = [REPO / p for p in rules["neverSearched"]]
    files = []
    for root in rules["searched"]:
        path = REPO / root
        files += [path] if path.is_file() else [p for p in path.rglob("*") if p.is_file() and p.suffix in TEXT]
    found = []
    for f in files:
        if any(f == n or n in f.parents for n in never):
            continue
        rel = f.relative_to(REPO).as_posix()
        text = f.read_text(encoding="utf-8", errors="replace").lower()
        for rule in rules["phrases"]:
            if rel in rule["allowedIn"]:
                continue
            count = text.count(rule["phrase"].lower())
            if count:
                found.append(f"{rel} says {rule['phrase']!r} {count} time{'s' if count != 1 else ''} ({rule['why']})")
    return found


def fetch(url: str) -> int | None:
    for attempt in range(2):
        try:
            request = urllib.request.Request(url, method="GET", headers={"User-Agent": "GroupLab consistency check"})
            with urllib.request.urlopen(request, timeout=15) as response:
                return response.status
        except urllib.error.HTTPError as e:
            if e.code < 500 and e.code != 429:
                return e.code
        except (urllib.error.URLError, TimeoutError, OSError):
            pass
        time.sleep(5 * (attempt + 1))
    return None


def online() -> list[str]:
    found = []
    try:
        request = urllib.request.Request("https://grouplab.org/releases/", headers={"User-Agent": "Mozilla/5.0 (GroupLab consistency check)"})
        releases = urllib.request.urlopen(request, timeout=20).read().decode("utf-8", "replace")
        if newest() and newest() not in releases:
            found.append(f"grouplab.org/releases/ does not yet name {newest()}, the newest nightly in the release notes")
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        found.append(f"grouplab.org/releases/ could not be read: {type(e).__name__}")
    links = sorted(set(re.findall(r"\((https?://[^)\s]+)\)", README.read_text(encoding="utf-8"))))
    for url in links:
        status = fetch(url)
        if status is None or status >= 400:
            found.append(f"the README links {url}, which answered {status if status else 'nothing'}")
    return found


ISSUE_TITLE = "Consistency: the README, the site and the assets"


def issue(found: list[str]) -> None:
    def gh(*args: str) -> str:
        return subprocess.run(["gh", *args], cwd=REPO, capture_output=True, text=True, check=False).stdout.strip()

    gh("label", "create", "consistency", "--color", "E8962E", "--description", "Findings of scripts/consistency.py (entry 267)", "--force")
    existing = gh("issue", "list", "--label", "consistency", "--state", "open", "--json", "number", "--jq", ".[0].number")
    if not found:
        if existing:
            gh("issue", "close", existing, "--comment", "The weekly consistency check found nothing; closing.")
        return
    body = "Found by `scripts/consistency.py` (NOTES-FROM-PLANNING.md entry 267):\n\n" + "\n".join(f"- {f}" for f in found)
    if existing:
        gh("issue", "edit", existing, "--body", body)
    else:
        gh("issue", "create", "--title", ISSUE_TITLE, "--label", "consistency", "--body", body)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--warn", action="store_true")
    parser.add_argument("--offline", action="store_true")
    parser.add_argument("--issue", action="store_true")
    args = parser.parse_args()
    found = checks_that_exist() + readme_checks() + routes() + retired() + guide_pdfs() + ([] if args.offline else online())
    for f in found:
        print(f"::warning::{f}" if args.warn else f"- {f}")
    print(f"consistency: {len(found)} finding{'s' if len(found) != 1 else ''}")
    if args.issue:
        issue(found)
    return 0 if args.warn or not found else 1


if __name__ == "__main__":
    sys.exit(main())
