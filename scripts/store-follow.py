"""The Microsoft Store follows the nightlies, NOTES-FROM-PLANNING.md entry 369 (Alan: "GroupLab nightlies should be sent to the Microsoft Store
every time they are generated/published to github").

store-follow.yml runs this between its steps; docs/notes/store-follow.json holds what it has done, committed as a "[notes] " commit.

    python3 scripts/store-follow.py decide            which nightly to send now, if any (to GITHUB_OUTPUT: go, version, tag, why)
    python3 scripts/store-follow.py whats-new TAG     that nightly's notes as plain text, at most 1500 characters, for What's new
    python3 scripts/store-follow.py submitted VERSION a submission was committed
    python3 scripts/store-follow.py failed REASON     a certification failed: automatic submissions stop, and Alan is told
    python3 scripts/store-follow.py search            whether apps.microsoft.com finds GroupLab by name (a line for status-note.py)

The rules are entry 369 section 2's: only a nightly whose release is published whole; never more than one submission in Microsoft's hands,
and the one in certification never cancelled, so the newest nightly simply waits for the next run; never more than the newest; and a failed
certification stops everything until Alan has dealt with it.
"""

from __future__ import annotations

import datetime as dt
import json
import os
import re
import subprocess
import sys
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
STATE = REPO / "docs" / "notes" / "store-follow.json"
FOR_ALAN = REPO / "docs" / "notes" / "for-alan.md"
PRODUCT = "9NWJCXBKZNPZ"
NIGHTLY = re.compile(r"^v(\d+\.\d+\.\d+-nightly\.(\d+))$")

# The assets a nightly's release has when it was published whole (nightly.yml uploads them together), named with its version and commit:
# grouplab-0.2.0-nightly.166-win-x64-080a6d8.zip, grouplab-setup-0.2.0-nightly.166-win-x64-080a6d8.exe and so on.
WHOLE = {"the Windows zip": re.compile(r"^grouplab-.+-win-x64-[0-9a-f]+\.zip$"),
         "the Windows installer": re.compile(r"^grouplab-setup-.+-win-x64-[0-9a-f]+\.exe$"),
         "the Linux archive": re.compile(r"^grouplab-.+-linux-x64-[0-9a-f]+\.tar\.gz$"),
         "the update manifest": re.compile(r"^update-manifest\.json$")}


def load() -> dict:
    if STATE.is_file():
        return json.loads(STATE.read_text(encoding="utf-8"))
    return {"about": "Written by scripts/store-follow.py, entry 369: the nightly last sent to the Microsoft Store, and whether sending has stopped.",
            "submitted": None, "submittedOn": None, "stopped": False, "reason": None, "ticketDrafted": False}


def save(state: dict) -> None:
    STATE.write_bytes((json.dumps(state, indent=2) + "\n").encode("utf-8"))


def output(**values: str) -> None:
    lines = "".join(f"{k}={v}\n" for k, v in values.items())
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as f:
            f.write(lines)
    sys.stdout.write(lines)


def gh(*args: str) -> str:
    return subprocess.run(["gh", *args], check=True, capture_output=True, text=True).stdout


def newest_nightly(releases: list[dict]) -> dict | None:
    nightlies = [r for r in releases if NIGHTLY.match(r["tagName"]) and not r.get("isDraft")]
    return max(nightlies, key=lambda r: int(NIGHTLY.match(r["tagName"]).group(2)), default=None)


def decide() -> int:
    state = load()
    if state.get("stopped"):
        output(go="false", why="stopped after a failed certification: " + (state.get("reason") or ""))
        return 0

    releases = json.loads(gh("release", "list", "--limit", "30", "--json", "tagName,isDraft,isPrerelease,publishedAt"))
    newest = newest_nightly(releases)
    if newest is None:
        output(go="false", why="no published nightly")
        return 0

    tag = newest["tagName"]
    version = NIGHTLY.match(tag).group(1)
    if state.get("submitted") == version:
        output(go="false", why=f"{version} was sent already")
        return 0

    assets = [a["name"] for a in json.loads(gh("release", "view", tag, "--json", "assets"))["assets"]]
    missing = [what for what, pattern in WHOLE.items() if not any(pattern.match(name) for name in assets)]
    if missing:
        output(go="false", why=f"{tag} is not published whole: missing {', '.join(missing)}")
        return 0

    output(go="true", version=version, tag=tag, why=f"{version} is the newest nightly and has not been sent")
    return 0


def plain(markdown: str) -> str:
    """A release's notes as the Store shows them: no headings' marks, no links' addresses, no code marks, one line a note."""
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", markdown)
    text = re.sub(r"[`*_]", "", text)
    lines = []
    for line in text.splitlines():
        line = line.strip()
        if not line or line.startswith("<!--"):
            continue
        line = re.sub(r"^#+\s*", "", line)
        line = re.sub(r"^[-+]\s+", "- ", line)
        lines.append(line)
    return "\n".join(lines)


def whats_new(tag: str) -> int:
    body = json.loads(gh("release", "view", tag, "--json", "body"))["body"] or ""
    text = plain(body)
    if len(text) > 1500:
        cut = text[:1497]
        text = cut[: max(cut.rfind("\n"), cut.rfind(". ") + 1, 1000)].rstrip() + "..."
    sys.stdout.write(text + "\n")
    return 0


def submitted(version: str) -> int:
    state = load()
    state["submitted"] = version
    state["submittedOn"] = dt.date.today().isoformat()
    save(state)
    return 0


def tell_alan(paragraph: str, marker: str) -> None:
    lines = FOR_ALAN.read_text(encoding="utf-8").split("\n")
    if any(line.startswith(marker) for line in lines):
        return
    lines.insert(1, paragraph)
    FOR_ALAN.write_bytes("\n".join(lines).encode("utf-8"))


def failed(reason: str) -> int:
    state = load()
    state["stopped"] = True
    state["reason"] = reason
    save(state)
    tell_alan("**THE MICROSOFT STORE REFUSED A BUILD** (entry 369): automatic submissions have stopped until this is dealt with. Microsoft "
              f"said: {reason} Partner Center shows it in full; when it is fixed or the submission deleted, tell the planning session and "
              "sending starts again.", "**THE MICROSOFT STORE REFUSED A BUILD**")
    return 0


def search() -> int:
    """Found when the Store website's own search, the API its search page calls, lists the product ID for "GroupLab"; the result is a line for status-note.py."""
    state = load()
    request = urllib.request.Request("https://apps.microsoft.com/api/products/search?query=GroupLab&mediaType=all&age=all&price=all&category=all&subscription=all&hl=en-us&gl=US", headers={"User-Agent": "Mozilla/5.0"})
    try:
        with urllib.request.urlopen(request, timeout=30) as answer:
            page = answer.read().decode("utf-8", "replace")
    except OSError as e:
        print(f"Store search: could not be read ({type(e).__name__})")
        return 0

    found = PRODUCT.lower() in page.lower()
    print(f"Store search for GroupLab: {'found' if found else 'not found'}")
    if not found and state.get("submittedOn") and not state.get("ticketDrafted"):
        since = dt.date.fromisoformat(state["submittedOn"])
        if dt.date.today() >= since + dt.timedelta(days=8):
            tell_alan("**A SUPPORT TICKET FOR THE MICROSOFT STORE, TO PASTE** (entry 369): \"GroupLab (product ID 9NWJCXBKZNPZ) is published, "
                      "Public, and set to be available and discoverable in the Microsoft Store, and its direct link works, but searching "
                      "for GroupLab in the Store app and on apps.microsoft.com (also in a private window) does not find it. It was first "
                      f"reported on 2026-10-04; a new submission was published around {since.isoformat()}, and a daily check of the search "
                      "has not found it since. Please check whether the product is indexed for search.\" Partner Center, Help and support, "
                      "Developer support.", "**A SUPPORT TICKET FOR THE MICROSOFT STORE")
            state["ticketDrafted"] = True
            save(state)
    return 0


def main() -> int:
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    if command == "decide":
        return decide()
    if command == "whats-new":
        return whats_new(sys.argv[2])
    if command == "submitted":
        return submitted(sys.argv[2])
    if command == "failed":
        return failed(" ".join(sys.argv[2:]))
    if command == "search":
        return search()
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main())
