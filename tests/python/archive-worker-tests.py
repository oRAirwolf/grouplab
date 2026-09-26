#!/usr/bin/env python3
"""The archive worker against a GitHub that answers from this process, NOTES-FROM-PLANNING.md entry 222 section 2.

No request leaves the machine. The stand-in plays releases, asset uploads and downloads, including the redirect to storage a real download
takes, and records whether the token was ever sent there. It checks that a submission leaves the server only once the archive has been
shown to hold it, that one the PC already archived is proven rather than uploaded again, that a corrupt copy keeps the folder and is
reported after five tries, that GitHub down keeps everything, and that nothing moves without a token.

    python3 tests/python/archive-worker-tests.py
"""

from __future__ import annotations

import hashlib
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading
import time
import urllib.parse
import zipfile
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORKER = REPO / "website" / "server" / "grouplab-archive-worker.py"
ARCHIVE = "test/archive"
TOKEN = "github_pat_" + "x" * 60

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
    def __init__(self) -> None:
        self.releases: dict[str, dict] = {}
        self.next_id = 100
        self.token_at_storage = False
        self.uploads: list[str] = []
        self.down = False
        self.corrupt: set[str] = set()

    def new_id(self) -> int:
        self.next_id += 1
        return self.next_id

    def asset(self, asset_id: int):
        for r in self.releases.values():
            for name, a in r["assets"].items():
                if a["id"] == asset_id:
                    return name, a
        return None, None


def handler(gh: GitHub, port_box: list[int]):
    class H(BaseHTTPRequestHandler):
        def log_message(self, *args):  # quiet
            pass

        def send(self, code: int, body: bytes = b"", headers: dict | None = None):
            self.send_response(code)
            for k, v in (headers or {}).items():
                self.send_header(k, v)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def json(self, code: int, obj) -> None:
            self.send(code, json.dumps(obj).encode(), {"Content-Type": "application/json"})

        def release_json(self, tag: str) -> dict:
            r = gh.releases[tag]
            return {"id": r["id"], "tag_name": tag, "assets": [{"id": a["id"], "name": n} for n, a in r["assets"].items()]}

        def do_GET(self):
            path = urllib.parse.urlparse(self.path).path
            if path.startswith("/storage/"):
                gh.token_at_storage |= "Authorization" in self.headers
                name, a = gh.asset(int(path.rsplit("/", 1)[1]))
                data = a["data"] if a else b""
                if name in gh.corrupt:
                    data = data + b"corrupted"
                return self.send(200, data)
            if gh.down:
                return self.send(503)
            if self.headers.get("Authorization") != f"Bearer {TOKEN}":
                return self.send(401)
            if path == f"/repos/{ARCHIVE}":
                return self.json(200, {"full_name": ARCHIVE, "private": True})
            if path.startswith(f"/repos/{ARCHIVE}/releases/tags/"):
                tag = path.rsplit("/", 1)[1]
                return self.json(200, self.release_json(tag)) if tag in gh.releases else self.json(404, {"message": "Not Found"})
            if path.startswith(f"/repos/{ARCHIVE}/releases/assets/"):
                return self.send(302, b"", {"Location": f"http://127.0.0.1:{port_box[0]}/storage/{path.rsplit('/', 1)[1]}"})
            self.send(404)

        def do_POST(self):
            parsed = urllib.parse.urlparse(self.path)
            length = int(self.headers.get("Content-Length", 0))
            body = self.rfile.read(length)
            if gh.down:
                return self.send(503)
            if self.headers.get("Authorization") != f"Bearer {TOKEN}":
                return self.send(401)
            if parsed.path == f"/repos/{ARCHIVE}/releases":
                tag = json.loads(body)["tag_name"]
                gh.releases[tag] = {"id": gh.new_id(), "assets": {}}
                return self.json(201, self.release_json(tag))
            if parsed.path.endswith("/assets"):
                rid = int(parsed.path.split("/")[-2])
                name = urllib.parse.parse_qs(parsed.query)["name"][0]
                tag = next(t for t, r in gh.releases.items() if r["id"] == rid)
                if name in gh.releases[tag]["assets"]:
                    return self.json(422, {"message": "already_exists"})
                gh.releases[tag]["assets"][name] = {"id": gh.new_id(), "data": body}
                gh.uploads.append(name)
                return self.json(201, {"id": gh.releases[tag]["assets"][name]["id"], "name": name})
            self.send(404)

        def do_DELETE(self):
            path = urllib.parse.urlparse(self.path).path
            name, _ = gh.asset(int(path.rsplit("/", 1)[1]))
            for r in gh.releases.values():
                r["assets"].pop(name, None) if name in r["assets"] and r["assets"][name]["id"] == int(path.rsplit("/", 1)[1]) else None
            self.send(204)

    return H


def submission(ready: Path, name: str, level: str = "testing", age: int = 600) -> Path:
    folder = ready / name
    folder.mkdir(parents=True)
    (folder / "meta.json").write_text(json.dumps({"consent": {"version": "consent_v2", "level": level}}), encoding="utf-8")
    (folder / "photo.png").write_bytes(os.urandom(2048))
    old = time.time() - age
    os.utime(folder, (old, old))
    return folder


def run(env: dict) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(WORKER)], env=env, capture_output=True, text=True, timeout=120)


def manifest_of(gh: GitHub, tag: str) -> dict:
    return json.loads(gh.releases[tag]["assets"]["manifest.json"]["data"])


def main() -> int:
    gh = GitHub()
    port_box = [0]
    server = HTTPServer(("127.0.0.1", 0), handler(gh, port_box))
    port_box[0] = server.server_address[1]
    threading.Thread(target=server.serve_forever, daemon=True).start()
    root = Path(tempfile.mkdtemp(prefix="grouplab-archive-worker-"))
    try:
        private = root / "private"
        ready = private / "ready"
        token_file = root / "token"
        env = dict(os.environ, GROUPLAB_PRIVATE=str(private), GROUPLAB_ARCHIVE_LOG=str(root / "worker.log"),
                   GROUPLAB_GITHUB_API=f"http://127.0.0.1:{port_box[0]}", GROUPLAB_GITHUB_UPLOADS=f"http://127.0.0.1:{port_box[0]}",
                   GROUPLAB_ARCHIVE_REPO=ARCHIVE, GROUPLAB_ARCHIVE_TOKEN_FILE=str(token_file))
        env.pop("CREDENTIALS_DIRECTORY", None)

        first = submission(ready, "2026-10-02_aaaaaaaa")
        fresh = submission(ready, "2026-10-02_bbbbbbbb", age=10)
        result = run(env)
        check("without a token nothing moves", result.returncode == 0 and first.is_dir() and not gh.releases, result.stdout + result.stderr)

        token_file.write_text(TOKEN)
        shutil.move(str(first), str(root / "aside"))
        shutil.move(str(fresh), str(root / "aside-fresh"))
        run(env)
        state = json.loads((private / "archive-worker" / "status.json").read_text())
        check("with nothing waiting, a run still proves the token reaches the archive", state.get("token") == "ok"
              and state.get("archive") == "reachable", json.dumps(state))
        shutil.move(str(root / "aside"), str(first))
        shutil.move(str(root / "aside-fresh"), str(fresh))
        result = run(env)
        check("with the token the submission is archived and leaves the server", not first.exists(), result.stdout + result.stderr)
        check("one still being written is left alone", fresh.is_dir())
        m = manifest_of(gh, "archive-2026-10")
        entry = next((s for s in m["submissions"] if s["name"] == "2026-10-02_aaaaaaaa"), None)
        data = gh.releases["archive-2026-10"]["assets"]["2026-10-02_aaaaaaaa.zip"]["data"]
        check("the manifest lists it with its hash and its consent", entry is not None and entry["sha256"] == hashlib.sha256(data).hexdigest()
              and entry["consent"] == "testing", json.dumps(m))
        names = zipfile.ZipFile(io.BytesIO(data)).namelist()
        check("the zip holds the folder's files at its root, as the PC pull makes it", sorted(names) == ["meta.json", "photo.png"], str(names))
        check("the token is never sent to the storage a download is redirected to", not gh.token_at_storage)

        # A submission the PC already put in the archive: proven by downloading it back, not uploaded again.
        pc = submission(ready, "2026-10-03_cccccccc", level="publishable")
        buf = io.BytesIO()
        with zipfile.ZipFile(buf, "w") as z:
            for f in sorted(pc.iterdir()):
                z.write(f, f.name)
        zdata = buf.getvalue()
        rel = gh.releases["archive-2026-10"]
        rel["assets"]["2026-10-03_cccccccc.zip"] = {"id": gh.new_id(), "data": zdata}
        m["submissions"].append({"name": "2026-10-03_cccccccc", "bytes": len(zdata), "sha256": hashlib.sha256(zdata).hexdigest(), "consent": "publishable"})
        rel["assets"]["manifest.json"]["data"] = json.dumps(m).encode()
        uploads_before = list(gh.uploads)
        run(env)
        check("one the PC archived is proven and removed, not uploaded again", not pc.exists()
              and "2026-10-03_cccccccc.zip" not in gh.uploads[len(uploads_before):])
        check("the manifest keeps both", {s["name"] for s in manifest_of(gh, "archive-2026-10")["submissions"]} >= {"2026-10-02_aaaaaaaa", "2026-10-03_cccccccc"})

        # A copy that comes back different: the folder stays, and the fifth failure files one error report.
        bad = submission(ready, "2026-10-04_dddddddd")
        gh.corrupt.add("2026-10-04_dddddddd.zip")
        for _ in range(5):
            run(env)
        reports = list((private / "error-reports" / "incoming").glob("*.json"))
        check("a copy that does not match keeps the folder on the server", bad.is_dir())
        check("the fifth failure is reported once as an error report", len(reports) == 1
              and "2026-10-04_dddddddd" in reports[0].read_text(encoding="utf-8"), str(len(reports)))
        run(env)
        check("and only once", len(list((private / "error-reports" / "incoming").glob("*.json"))) == 1)
        gh.corrupt.clear()
        shutil.rmtree(bad)

        # GitHub down: everything stays, and the run does not fail.
        down = submission(ready, "2026-11-01_eeeeeeee")
        gh.down = True
        result = run(env)
        check("GitHub down keeps the submission and is not a failure", down.is_dir() and result.returncode == 0)
        gh.down = False
        run(env)
        check("the next run archives it in a release of its own month", not down.exists() and "archive-2026-11" in gh.releases)

        token_file.write_text("github_pat_" + "y" * 60)
        refused = submission(ready, "2026-11-02_ffffffff")
        result = run(env)
        check("a refused token keeps everything and says so", refused.is_dir() and result.returncode == 1
              and json.loads((private / "archive-worker" / "status.json").read_text())["token"] == "refused")
    finally:
        server.shutdown()
        shutil.rmtree(root, ignore_errors=True)

    script = (REPO / "website" / "server" / "grouplab-set-archive-token").read_text(encoding="utf-8")
    check("the token script names the archive worker, never the error worker (entry 224)",
          "grouplab-archive-worker" in script and "error-worker" not in script and "error worker" not in script)

    print(f"archive worker tests: {passed} passed, {len(failed)} failed")
    for f in failed:
        print("  FAILED  " + f)
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
