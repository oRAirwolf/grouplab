#!/usr/bin/env python3
"""Announce a published build in Discord. NOTES-FROM-PLANNING.md entry 184.

    python3 scripts/discord-announce.py VERSION NOTES_FILE [--stable] [--dry-run]
    python3 scripts/discord-announce.py --self-test

Every published build posts one message to #builds; a stable release also posts to #announcements, and a
nightly never does, because a message a night would bury everything else there. The workflows run this
only after the release is published, so a night the gate skips posts nothing.

The notes are the release's own, from scripts/release-notes.py, so the message and the release body cannot
differ; the workflow passes the file it wrote before the platform line was added, which does not belong in
a chat message. The message is one embed: the version as its title, linked to the release page, the notes
with their two headings, and the download page.

The webhook addresses come from the environment, DISCORD_BUILDS_WEBHOOK and DISCORD_ANNOUNCE_WEBHOOK, which
the workflows fill from repository secrets that Alan added himself. Anyone holding one can post to the
channel, so this never prints, logs or writes one; --dry-run prints the exact JSON with the address
replaced. A missing secret is not a failure: the step says so and does nothing, because an announcement is
not part of the product. Nor is a Discord that refuses: the build is published either way.

.github/announced-builds.txt records each version once it has been posted, so a rerun of the workflow does
not post the same build twice. The workflow commits that file.
"""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
RECORD = REPO / ".github" / "announced-builds.txt"
RELEASES = "https://github.com/oRAirwolf/grouplab/releases/tag/v"
DOWNLOAD = "https://grouplab.org/download/"

# A Discord embed's description holds 4096 characters. Entry 168 fixed one set of notes cut off in the middle
# of a sentence, so a long set ends at a whole line and says where the rest is.
LIMIT = 4096
MORE = "Full notes on the release page."
AMBER = 0xE8A33D


def description(notes: str, version: str) -> str:
    """The notes as a chat message: the generator's naming line dropped, whole lines only, and where to download."""
    lines = [l.rstrip() for l in notes.replace("\r\n", "\n").split("\n")]
    lines = [l for l in lines if not (l.startswith("GroupLab ") and l.rstrip(".").endswith(version))]
    body = "\n".join(lines).strip()
    if not body:
        body = "This build's notes are on its release page."
    tail = f"\n\n[Download GroupLab]({DOWNLOAD})"
    if len(body) + len(tail) <= LIMIT:
        return body + tail

    kept: list[str] = []
    room = LIMIT - len(tail) - len("\n\n" + MORE)
    for line in body.split("\n"):
        if len("\n".join(kept + [line])) > room:
            break
        kept.append(line)
    return "\n".join(kept).rstrip() + "\n\n" + MORE + tail


def message(version: str, notes: str) -> dict:
    return {
        "username": "GroupLab builds",
        "allowed_mentions": {"parse": []},
        "embeds": [{
            "title": f"GroupLab {version}",
            "url": RELEASES + version,
            "description": description(notes, version),
            "color": AMBER,
        }],
    }


def announced(version: str) -> bool:
    return RECORD.is_file() and version in RECORD.read_text(encoding="utf-8").split()


def record(version: str) -> None:
    text = RECORD.read_text(encoding="utf-8") if RECORD.is_file() else "# Builds already announced in Discord, one a line, written by the workflows. Entry 184.\n"
    RECORD.write_bytes((text + version + "\n").encode("utf-8"))


def post(address: str, payload: dict) -> str:
    """Posts, and says how it went without ever repeating the address."""
    request = urllib.request.Request(
        address + ("&" if "?" in address else "?") + "wait=true",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json", "User-Agent": "GroupLab-announce (https://grouplab.org, 1)"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as answer:
            return f"posted, HTTP {answer.status}"
    except urllib.error.HTTPError as e:
        return f"not posted: Discord answered HTTP {e.code}"
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        return f"not posted: {type(e).__name__}"


def run(version: str, notes_file: str, stable: bool, dry_run: bool) -> int:
    notes = Path(notes_file).read_text(encoding="utf-8") if Path(notes_file).is_file() else ""
    payload = message(version, notes)
    if dry_run:
        print(json.dumps({"to": ["<DISCORD_BUILDS_WEBHOOK>"] + (["<DISCORD_ANNOUNCE_WEBHOOK>"] if stable else []), "payload": payload}, indent=2))
        return 0

    if announced(version):
        print(f"{version} was already announced; nothing posted.")
        return 0

    targets = [("#builds", os.environ.get("DISCORD_BUILDS_WEBHOOK", ""))]
    if stable:
        targets.append(("#announcements", os.environ.get("DISCORD_ANNOUNCE_WEBHOOK", "")))
    any_posted = False
    for channel, address in targets:
        if not address.strip():
            print(f"{channel}: no webhook secret is set, so nothing was posted there.")
            continue
        said = post(address.strip(), payload)
        print(f"{channel}: {said}")
        any_posted = any_posted or said.startswith("posted")
    if any_posted:
        record(version)
    return 0


def self_test() -> int:
    failed = 0

    def check(name: str, ok: bool) -> None:
        nonlocal failed
        failed += not ok
        print(("ok   " if ok else "FAIL ") + name)

    normal = ("GroupLab 0.2.0-nightly.95.\n\n**What you will notice**\n\n- Pinch to zoom now works on a trackpad. (Entry 166)\n\n"
              "**Under the hood**\n\n- The notes can name pages on grouplab.org. (Entry 185, 3)\n")
    m = message("0.2.0-nightly.95", normal)
    d = m["embeds"][0]["description"]
    check("normal notes keep both headings", "**What you will notice**" in d and "**Under the hood**" in d)
    check("the naming line is not repeated under the title", "GroupLab 0.2.0-nightly.95." not in d)
    check("the title is the version, linked to its release", m["embeds"][0]["title"] == "GroupLab 0.2.0-nightly.95"
          and m["embeds"][0]["url"].endswith("/v0.2.0-nightly.95"))
    check("the download page is linked", DOWNLOAD in d)
    check("nobody is pinged", m["allowed_mentions"] == {"parse": []})

    long_notes = "**What you will notice**\n\n" + "\n".join(f"- A change that is described in a whole sentence, number {i}. (Entry {i})" for i in range(200))
    d = description(long_notes, "0.2.0-nightly.96")
    check("long notes fit the embed", len(d) <= LIMIT)
    check("long notes end at a whole line and say where the rest is", MORE in d and all(
        l.endswith(")") or l in ("", MORE, "**What you will notice**") or l.startswith("[Download") for l in d.split("\n")))

    hood = "GroupLab 0.2.0-nightly.97.\n\n**Under the hood**\n\n- Tests run faster on every system. (Entry 1)\n"
    d = description(hood, "0.2.0-nightly.97")
    check("notes with only Under the hood keep it", d.startswith("**Under the hood**"))

    d = description("", "0.2.0-nightly.98")
    check("a release with no notes still says where they are", d.startswith("This build's notes are on its release page."))
    check("no message carries a webhook address", "discord.com/api/webhooks" not in json.dumps(message("x", normal)))
    print("discord-announce self-test: " + ("passed" if not failed else f"{failed} failed"))
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    args = [a for a in argv[1:] if not a.startswith("--")]
    if len(args) != 2:
        print(__doc__.split("\n\n")[1], file=sys.stderr)
        return 2
    return run(args[0], args[1], "--stable" in argv, "--dry-run" in argv)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
