#!/usr/bin/env python3
"""
Request 117 (Alan, 2026-11-23: yes): the bot's own Discord replies posted before entry 515 end "(Posted by the GroupLab bot for Alan.)".
This edits each named message in place, changing only that last line to the worker's SIGNED line, "This is an AI generated response.",
and nothing else in it. A one-off, run by hand under systemd-run with the worker's Discord credential, never by a timer.

    grouplab-discord-old-line.py --dry-run LINK...             says what each message would become, by id; changes nothing
    grouplab-discord-old-line.py --backup DIR LINK...           writes every message as it is to DIR/messages.json, then edits
    grouplab-discord-old-line.py --restore DIR/messages.json    puts each message's words back as they were saved

A LINK is https://discord.com/channels/<guild>/<thread>/<message>. A message is edited only when the bot itself wrote it and its last
line is exactly the old line; one already ending with the new line is left alone, so a second run changes nothing. Every message is
read and the backup written before the first edit, and nothing is edited when any message cannot be read. A thread Discord has archived
is opened for the edit and archived again after it. Nobody is notified: the edit sends no mentions, and Discord sends no notice of an
edit. The words of a message are never printed, only its id and what was checked.

It imports the worker that sits beside it, for its Discord client, token file and SIGNED line, so both are fetched at the same commit.
"""

from __future__ import annotations

import importlib.util
import json
import os
import re
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("grouplab_discord_worker", HERE / "grouplab-discord-worker.py")
worker = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(worker)

OLD = "(Posted by the GroupLab bot for Alan.)"
NEW = worker.SIGNED
LINK = re.compile(r"^https://discord\.com/channels/(\d+)/(\d+)/(\d+)$")
PAUSE_SECONDS = float(os.environ.get("GROUPLAB_DISCORD_EDIT_PAUSE", "1"))


def relined(content: str) -> str | None:
    """The words with only the last line changed, the same words when they already end with the new line, or None."""
    lines = content.split("\n")
    if lines[-1] == NEW:
        return content
    if lines[-1] != OLD:
        return None
    return "\n".join(lines[:-1] + [NEW])


def thread_state(token: str, thread: str) -> tuple[bool, bool]:
    """Whether the thread is archived and whether it is locked."""
    code, channel = worker.discord(f"/channels/{thread}", token)
    if code != 200 or not isinstance(channel, dict):
        raise worker.Unreachable(f"thread {thread} could not be read, Discord answered {code}")
    meta = channel.get("thread_metadata") or {}
    return bool(meta.get("archived")), bool(meta.get("locked"))


def read_all(token: str, links: list[str]) -> list[dict]:
    """Every message read and checked before anything is changed; a problem with any one stops the run."""
    code, me = worker.discord("/users/@me", token)
    if code != 200 or not isinstance(me, dict) or not me.get("id"):
        raise worker.Unreachable(f"the bot's own id could not be read, Discord answered {code}")
    found = []
    for link in links:
        m = LINK.match(link.strip())
        if not m:
            raise ValueError(f"not a message link: {link!r}")
        _, thread, mid = m.groups()
        code, msg = worker.discord(f"/channels/{thread}/messages/{mid}", token)
        if code != 200 or not isinstance(msg, dict):
            raise worker.Unreachable(f"message {mid} could not be read, Discord answered {code}")
        if (msg.get("author") or {}).get("id") != me["id"]:
            raise ValueError(f"message {mid} was not written by the bot, so it is not the bot's to edit")
        content = msg.get("content") or ""
        new = relined(content)
        if new is None:
            raise ValueError(f"message {mid} does not end with the old line, so it is left for a person to look at")
        archived, locked = thread_state(token, thread)
        if locked and new != content:
            raise ValueError(f"thread {thread} is locked, so message {mid} cannot be edited")
        found.append({"link": link.strip(), "thread": thread, "id": mid, "content": content, "new": new,
                      "edited_timestamp": msg.get("edited_timestamp"), "archived": archived,
                      "problems": worker.reply_problems(new) if new != content else []})
    return found


def edit(token: str, thread: str, mid: str, words: str, archived: bool) -> list[str]:
    """One message's words replaced; what went wrong beside the edit itself, which is an exception."""
    notes = []
    if archived:
        code, _ = worker.discord(f"/channels/{thread}", token, "PATCH", {"archived": False})
        if code != 200:
            raise worker.Unreachable(f"thread {thread} is archived and could not be opened, Discord answered {code}")
    try:
        code, msg = worker.discord(f"/channels/{thread}/messages/{mid}", token, "PATCH",
                                   {"content": words, "allowed_mentions": {"parse": []}})
        if code != 200 or not isinstance(msg, dict) or msg.get("content") != words:
            raise worker.Unreachable(f"message {mid} was not edited, Discord answered {code}")
    finally:
        if archived:
            code, _ = worker.discord(f"/channels/{thread}", token, "PATCH", {"archived": True})
            if code != 200:
                notes.append(f"thread {thread} could not be archived again (Discord answered {code}); it archives itself when quiet")
    return notes


def main(argv: list[str]) -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(errors="backslashreplace")
    dry = "--dry-run" in argv
    args = [a for a in argv if a != "--dry-run"]
    token = worker.token(worker.DISCORD_TOKEN_FILE)
    if not token:
        print("STOP: the Discord bot token is not set, so nothing was read")
        return 1
    try:
        if args[:1] == ["--restore"]:
            if len(args) != 2:
                print("STOP: --restore takes the backup's messages.json")
                return 2
            saved = json.loads(Path(args[1]).read_text(encoding="utf-8"))
            for m in saved:
                archived, _ = thread_state(token, m["thread"])
                if dry:
                    print(f"would restore message {m['id']} in thread {m['thread']}")
                    continue
                for note in edit(token, m["thread"], m["id"], m["content"], archived):
                    print("note: " + note)
                print(f"restored message {m['id']} in thread {m['thread']}")
                time.sleep(PAUSE_SECONDS)
            return 0
        backup = None
        if args[:1] == ["--backup"]:
            if len(args) < 3:
                print("STOP: --backup takes a folder and at least one link")
                return 2
            backup, args = Path(args[1]), args[2:]
        if not args:
            print("STOP: no message links were given")
            return 2
        if not dry and backup is None:
            print("STOP: an edit needs --backup DIR, so every message is saved first")
            return 2
        found = read_all(token, args)
        for m in found:
            what = "already ends with the new line, left alone" if m["new"] == m["content"] else "last line would change, nothing else"
            print(f"message {m['id']} in thread {m['thread']}: {what}" + (" (thread archived)" if m["archived"] else "")
                  + "".join(f"; note: {p}" for p in m["problems"]))
        if dry:
            print(f"dry run: {sum(m['new'] != m['content'] for m in found)} of {len(found)} would be edited; nothing was changed")
            return 0
        backup.mkdir(parents=True, exist_ok=True)
        os.chmod(backup, 0o700)
        target = backup / "messages.json"
        with open(target, "w", encoding="utf-8", newline="\n") as f:
            json.dump([{k: m[k] for k in ("link", "thread", "id", "content", "edited_timestamp")} for m in found], f, indent=1)
            f.flush()
            os.fsync(f.fileno())
        print(f"backup: {target}, {len(found)} messages")
        edited = 0
        for m in found:
            if m["new"] == m["content"]:
                continue
            for note in edit(token, m["thread"], m["id"], m["new"], m["archived"]):
                print("note: " + note)
            edited += 1
            print(f"edited message {m['id']} in thread {m['thread']}")
            time.sleep(PAUSE_SECONDS)
        print(f"done: {edited} edited, {len(found) - edited} already had the new line")
        return 0
    except (ValueError, worker.Unreachable, worker.TokenRefused) as e:
        print(f"STOP: {e}")
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
