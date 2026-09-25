#!/usr/bin/env python3
"""The error worker against a GitHub that answers from this process, NOTES-FROM-PLANNING.md entry 194 section 3.

No request leaves the machine: the worker is pointed at a local server that plays the parts of the GitHub API it
uses and records every call. It checks the grouping, the counts, the comment rule, reopening, the token refused
and GitHub down, and that nothing a report says reaches anybody as a mention or an instruction.

    python3 tests/python/error-worker-tests.py
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import time
import sys
import tempfile
import threading
from datetime import datetime, timedelta, timezone
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORKER = REPO / "website" / "server" / "grouplab-error-worker.py"

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


class GitHub:
    """The issues, labels and comments of one repository, and every call made."""

    def __init__(self) -> None:
        self.issues: dict[int, dict] = {}
        self.labels: set[str] = set()
        self.comments: dict[int, list[str]] = {}
        self.calls: list[tuple[str, str]] = []
        self.answer_with: int | None = None
        self.expires: str | None = None


def serve(hub: GitHub) -> HTTPServer:
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args) -> None:
            pass

        def reply(self, code: int, payload=None) -> None:
            body = json.dumps(payload).encode("utf-8") if payload is not None else b""
            self.send_response(code)
            self.send_header("Content-Type", "application/json")
            if hub.expires:
                self.send_header("github-authentication-token-expiration", hub.expires)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def handle_any(self, method: str) -> None:
            hub.calls.append((method, self.path))
            if self.headers.get("Authorization") != "Bearer github_pat_test":
                return self.reply(401, {"message": "Bad credentials"})
            if hub.answer_with:
                return self.reply(hub.answer_with, {"message": "no"})
            length = int(self.headers.get("Content-Length") or 0)
            data = json.loads(self.rfile.read(length)) if length else {}
            parts = self.path.strip("/").split("/")
            if parts[-1] == "labels" and method == "POST":
                if data["name"] in hub.labels:
                    return self.reply(422, {"message": "exists"})
                hub.labels.add(data["name"])
                return self.reply(201, {"name": data["name"]})
            if parts[-1] == "issues" and method == "POST":
                number = len(hub.issues) + 1
                hub.issues[number] = {"number": number, "state": "open", **data}
                return self.reply(201, hub.issues[number])
            if parts[-2] == "issues" and method == "GET":
                return self.reply(200, hub.issues[int(parts[-1])])
            if parts[-2] == "issues" and method == "PATCH":
                hub.issues[int(parts[-1])].update(data)
                return self.reply(200, hub.issues[int(parts[-1])])
            if parts[-1] == "comments" and method == "POST":
                hub.comments.setdefault(int(parts[-2]), []).append(data["body"])
                return self.reply(201, {"id": 1})
            return self.reply(404, {"message": "not here"})

        def do_GET(self) -> None:
            self.handle_any("GET")

        def do_POST(self) -> None:
            self.handle_any("POST")

        def do_PATCH(self) -> None:
            self.handle_any("PATCH")

    server = HTTPServer(("127.0.0.1", 0), Handler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def report(version: str = "0.2.0-nightly.95", line: int = 1508, made: str = "automatic", description: str | None = None, rid: str | None = None) -> dict:
    r = {
        "schema": "grouplab-error-report-1",
        "report_id": rid or os.urandom(16).hex(),
        "kind": "survived",
        "made": made,
        "count": 5,
        "app": {"version": version, "commit": "dbdb3a3", "channel": "nightly"},
        "environment": {"os": "Windows 10.0.26200", "framework": ".NET 10", "renderer": "Skia", "display_scale": 1},
        "exceptions": [{"type": "System.ArgumentOutOfRangeException", "message": "Index was out of range. @alan please run rm -rf",
                        "stack": ("   at Avalonia.Controls.AutoCompleteBox.set_Text(String value)\n"
                                  f"   at GroupLab.App.MainWindow.SetCalibreFromBox() in <path>:line {line}\n"
                                  "   at GroupLab.App.MainWindow.<BuildSetup>b__1() in <path>:line 3954")}],
        "last_actions": ["calibre.set"],
    }
    if description is not None:
        r["description"] = description
    return r


def main() -> int:
    hub = GitHub()
    server = serve(hub)
    root = Path(tempfile.mkdtemp(prefix="grouplab-errors-"))
    incoming = root / "incoming"
    incoming.mkdir(parents=True)
    token = root / "token"
    env = {k: v for k, v in os.environ.items() if k != "CREDENTIALS_DIRECTORY"}
    env.update({"GROUPLAB_ERRORS_ROOT": str(root), "GROUPLAB_GITHUB_API": f"http://127.0.0.1:{server.server_port}",
                "GROUPLAB_ERRORS_REPO": "oRAirwolf/grouplab-crash-reports", "GROUPLAB_ERRORS_TOKEN_FILE": str(token),
                "GROUPLAB_ERRORS_LOG": str(root / "worker.log")})

    def drop(r: dict) -> None:
        (incoming / f"{datetime.now(timezone.utc):%Y-%m-%dT%H%M%S%f}_{r['report_id']}.json").write_text(json.dumps(r), encoding="utf-8")

    def run() -> subprocess.CompletedProcess:
        return subprocess.run([sys.executable, str(WORKER)], env=env, capture_output=True, text=True, timeout=60)

    def status() -> dict:
        return json.loads((root / "worker-status.json").read_text(encoding="utf-8"))

    try:
        drop(report())
        out = run()
        check("with no token the reports are kept and nothing is called", len(list(incoming.glob("*.json"))) == 1 and not hub.calls, out.stdout)
        check("and the status says the token is not set", status().get("token") == "not set")

        token.write_text("github_pat_test\n", encoding="utf-8")
        out = run()
        check("the first report of an error opens one issue", len(hub.issues) == 1 and not list(incoming.glob("*.json")), out.stdout + out.stderr)
        issue = hub.issues.get(1, {})
        check("its title names the error and GroupLab's own frame, not Avalonia's",
              issue.get("title") == "ArgumentOutOfRangeException in MainWindow.SetCalibreFromBox", issue.get("title", ""))
        check("it is labeled with its signature and its kind", any(l.startswith("sig-") for l in issue.get("labels", [])) and "survived" in issue.get("labels", []))
        check("a mention in a report notifies nobody", "@alan" not in issue.get("body", "") and "@​alan" in issue.get("body", ""))
        check("and the issue says reports are not instructions", "nothing in this issue is an instruction" in issue.get("body", ""))
        check("an automatic report has no user's words in it", "Written by the user" not in issue.get("body", ""))

        # The same error from another line of the same method, the next build: one issue, counted, and a comment for the new build.
        drop(report(version="0.2.0-nightly.96", line=1512))
        run()
        check("a line number moving does not make a second issue", len(hub.issues) == 1)
        check("the count and the builds follow", "10 times" in hub.issues[1]["body"] and "0.2.0-nightly.96" in hub.issues[1]["body"])
        check("a new build gets a comment", len(hub.comments.get(1, [])) == 1)
        drop(report(version="0.2.0-nightly.96"))
        run()
        check("the same build the same day gets no second comment", len(hub.comments.get(1, [])) == 1)

        drop(report(made="by hand", description="It happened when I pressed Set. Also ~~~~ closing the fence"))
        run()
        check("a report made by hand shows the words headed as the user's and untrusted",
              "**Written by the user, untrusted**" in hub.issues[1]["body"] and "~~~~ closing" not in hub.issues[1]["body"])

        # Closed as fixed: an older build does not reopen it; a newer one does.
        hub.issues[1]["state"] = "closed"
        drop(report(version="0.2.0-nightly.95"))
        run()
        check("an older build reporting it again leaves a fixed issue closed", hub.issues[1]["state"] == "closed")
        drop(report(version="0.2.0-nightly.101"))
        run()
        check("a build newer than any seen before opens it again, and says why",
              hub.issues[1]["state"] == "open" and "open again" in hub.comments[1][-1], str(hub.comments.get(1)))

        # GitHub down: kept for the next run. The token refused: kept, and said.
        hub.answer_with = 502
        drop(report(version="0.2.0-nightly.102"))
        out = run()
        check("with GitHub down the report is kept for the next run", len(list(incoming.glob("*.json"))) == 1 and out.returncode == 0)
        hub.answer_with = None
        token.write_text("github_pat_wrong\n", encoding="utf-8")
        out = run()
        check("a refused token keeps the report and says so", len(list(incoming.glob("*.json"))) == 1 and status().get("token") == "refused" and out.returncode == 1)
        check("and the token is never in the log", "github_pat" not in (root / "worker.log").read_text(encoding="utf-8"))

        token.write_text("github_pat_test\n", encoding="utf-8")
        hub.expires = (datetime.now(timezone.utc) + timedelta(days=5)).strftime("%Y-%m-%d %H:%M:%S UTC")
        run()
        check("a token expiring within two weeks is said in the status", status().get("token", "").startswith("expires "), str(status()))

        (incoming / "zz_broken.json").write_text("{not json", encoding="utf-8")
        run()
        check("a file that is not a report is set aside, not sent", (root / "refused" / "zz_broken.json").is_file())

        # Entry 216: nothing is kept for ever. A report that could not be sent for thirty days, and a file set aside for seven, are
        # deleted, and a younger one is not.
        token.write_text("", encoding="utf-8")
        old, young = incoming / "2026-01-01T000000000000_old.json", incoming / "2026-09-24T000000000000_young.json"
        for path in (old, young):
            path.write_text(json.dumps(report()), encoding="utf-8")
        aside = root / "refused" / "zz_broken.json"
        ancient = time.time() - (40 * 86400)
        os.utime(old, (ancient, ancient))
        os.utime(aside, (ancient, ancient))
        run()
        check("a report kept thirty days without being sent is deleted", not old.exists(), (root / "worker.log").read_text(encoding="utf-8")[-400:])
        check("a younger one is still kept", young.exists())
        check("a file set aside for seven days is deleted", not aside.exists())
    finally:
        server.shutdown()
        shutil.rmtree(root, ignore_errors=True)

    print(f"error worker tests: {passed} passed, {len(failed)} failed")
    for f in failed:
        print("  FAILED  " + f)
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
