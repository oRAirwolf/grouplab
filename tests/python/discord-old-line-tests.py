#!/usr/bin/env python3
"""Request 117: the one-off that changes the last line of the bot's earlier Discord replies, against a Discord in this process.

Checks that a dry run changes nothing, an edit needs a backup and writes it before the first change, only the last line changes, a
message somebody else wrote or one not ending with the old line stops the run before anything is edited, an archived thread is archived
again, a second run changes nothing, and --restore puts the words back.

    python3 tests/python/discord-old-line-tests.py
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SCRIPT = REPO / "website" / "server" / "grouplab-discord-old-line.py"
OLD = "(Posted by the GroupLab bot for Alan.)"
NEW = "This is an AI generated response."
BOT = "900"

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what + (": " + detail if detail else ""))
        print("FAIL " + what + (": " + detail if detail else ""))


class World:
    def __init__(self) -> None:
        self.messages = {
            ("10", "1"): {"author": BOT, "content": f"Thanks for the report.\nIt is fixed in the next build.\n{OLD}"},
            ("20", "2"): {"author": BOT, "content": f"See https://grouplab.org/guide/ for the steps.\n{OLD}"},
            ("30", "3"): {"author": "7", "content": f"Somebody else.\n{OLD}"},
            ("10", "4"): {"author": BOT, "content": "No signature at all."},
        }
        self.archived = {"10": False, "20": True, "30": False}
        self.calls: list[str] = []
        self.patches: list[dict] = []


def serve(world: World) -> HTTPServer:
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args) -> None:
            pass

        def reply(self, code: int, payload=None) -> None:
            body = json.dumps(payload).encode("utf-8") if payload is not None else b""
            self.send_response(code)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def parts(self) -> list[str]:
            return self.path.split("/discord/", 1)[1].split("/")

        def do_GET(self) -> None:
            world.calls.append("GET " + self.path)
            p = self.parts()
            if p == ["users", "@me"]:
                return self.reply(200, {"id": BOT})
            if len(p) == 2 and p[0] == "channels":
                return self.reply(200, {"id": p[1], "thread_metadata": {"archived": world.archived[p[1]], "locked": False}})
            if len(p) == 4 and p[2] == "messages" and (p[1], p[3]) in world.messages:
                m = world.messages[(p[1], p[3])]
                return self.reply(200, {"id": p[3], "author": {"id": m["author"]}, "content": m["content"], "edited_timestamp": None})
            return self.reply(404, {})

        def do_PATCH(self) -> None:
            world.calls.append("PATCH " + self.path)
            body = json.loads(self.rfile.read(int(self.headers.get("Content-Length") or 0)) or b"{}")
            world.patches.append(body)
            p = self.parts()
            if len(p) == 2:
                world.archived[p[1]] = body["archived"]
                return self.reply(200, {"id": p[1]})
            key = (p[1], p[3])
            if world.archived[p[1]]:
                return self.reply(400, {"code": 50083, "message": "Thread is archived"})
            if world.messages[key]["author"] != BOT:
                return self.reply(403, {})
            world.messages[key]["content"] = body["content"]
            return self.reply(200, {"id": p[3], "content": body["content"]})

    server = HTTPServer(("127.0.0.1", 0), Handler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def run(root: Path, port: int, *args: str) -> subprocess.CompletedProcess:
    env = dict(os.environ)
    env.pop("CREDENTIALS_DIRECTORY", None)
    env.update(GROUPLAB_DISCORD_ROOT=str(root / "discord"), GROUPLAB_DISCORD_LOG=str(root / "worker.log"),
               GROUPLAB_DISCORD_API=f"http://127.0.0.1:{port}/discord", GROUPLAB_GITHUB_API=f"http://127.0.0.1:{port}/github",
               GROUPLAB_DISCORD_TOKEN_FILE=str(root / "discord-token"), GROUPLAB_ERRORS_TOKEN_FILE=str(root / "none"),
               GROUPLAB_DISCORD_EDIT_PAUSE="0", PYTHONIOENCODING="utf-8")
    return subprocess.run([sys.executable, str(SCRIPT), *args], env=env, capture_output=True, text=True, encoding="utf-8", timeout=60)


def link(thread: str, mid: str) -> str:
    return f"https://discord.com/channels/5/{thread}/{mid}"


def main() -> int:
    root = Path(tempfile.mkdtemp(prefix="grouplab-old-line-"))
    world = World()
    server = serve(world)
    port = server.server_address[1]
    try:
        (root / "discord-token").write_text("token\n", encoding="utf-8")
        before = {k: dict(v) for k, v in world.messages.items()}
        mine = [link("10", "1"), link("20", "2")]

        r = run(root, port, "--dry-run", *mine)
        check("a dry run finishes", r.returncode == 0, r.stdout + r.stderr)
        check("a dry run says two would be edited", "2 of 2 would be edited" in r.stdout, r.stdout)
        check("a dry run changes nothing", world.messages == before and not world.patches)
        check("a dry run prints no message's words", "Thanks for the report" not in r.stdout and OLD not in r.stdout)

        r = run(root, port, *mine)
        check("an edit without --backup is refused", r.returncode == 2 and not world.patches, r.stdout)

        r = run(root, port, "--backup", str(root / "b0"), link("10", "1"), link("30", "3"))
        check("somebody else's message stops the run", r.returncode == 1 and "not written by the bot" in r.stdout, r.stdout)
        check("nothing is edited when one message is not the bot's", not world.patches and not (root / "b0").exists())

        r = run(root, port, "--backup", str(root / "b0"), link("10", "1"), link("10", "4"))
        check("a message not ending with the old line stops the run", r.returncode == 1 and not world.patches, r.stdout)

        r = run(root, port, "--backup", str(root / "b1"), *mine)
        check("the edit finishes", r.returncode == 0 and "done: 2 edited" in r.stdout, r.stdout + r.stderr)
        saved = json.loads((root / "b1" / "messages.json").read_text(encoding="utf-8"))
        check("the backup holds both messages as they were", [m["content"] for m in saved] == [before[("10", "1")]["content"], before[("20", "2")]["content"]])
        check("only the last line changed", world.messages[("10", "1")]["content"] == f"Thanks for the report.\nIt is fixed in the next build.\n{NEW}"
              and world.messages[("20", "2")]["content"] == f"See https://grouplab.org/guide/ for the steps.\n{NEW}")
        check("nobody is notified by an edit", all(p.get("allowed_mentions") == {"parse": []} for p in world.patches if "content" in p))
        check("an archived thread is archived again", world.archived["20"] is True)
        check("the other messages are untouched", world.messages[("30", "3")] == before[("30", "3")] and world.messages[("10", "4")] == before[("10", "4")])

        count = len(world.patches)
        r = run(root, port, "--backup", str(root / "b2"), *mine)
        check("a second run edits nothing", r.returncode == 0 and "done: 0 edited, 2 already" in r.stdout and len(world.patches) == count, r.stdout)

        r = run(root, port, "--restore", str(root / "b1" / "messages.json"))
        check("--restore puts the words back", r.returncode == 0 and world.messages[("10", "1")] == before[("10", "1")]
              and world.messages[("20", "2")] == before[("20", "2")], r.stdout + r.stderr)
        check("no log or state was written", not (root / "worker.log").exists() and not (root / "discord").exists())
    finally:
        server.shutdown()
        shutil.rmtree(root, ignore_errors=True)
    print(f"{passed} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
