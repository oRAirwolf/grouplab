#!/usr/bin/env python3
"""The server's learning worker, NOTES-FROM-PLANNING.md entries 394 and 395, with a stand-in command line and a stand-in nightly release, so
nothing leaves the runner. Linux only, as the server is.

    python3 tests/python/learn-worker-tests.py
"""

from __future__ import annotations

import io
import json
import os
import re
import subprocess
import sys
import tarfile
import tempfile
import threading
import time
from functools import partial
from http.server import HTTPServer, SimpleHTTPRequestHandler
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORKER = REPO / "website" / "server" / "grouplab-learn-worker.py"

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what)
        print("FAIL " + what + (": " + detail[-600:] if detail else ""))


# The waiting number is the code's, wherever it is written.
code = (REPO / "src" / "GroupLab.Core" / "Evaluation" / "RealScoreboard.cs").read_text(encoding="utf-8")
worker_text = WORKER.read_text(encoding="utf-8")
check("the worker waits for the same number of corrected submissions as the code",
      re.search(r"CorrectedForTuning = (\d+);", code).group(1) == re.search(r"^CORRECTED_FOR_TUNING = (\d+)", worker_text, re.M).group(1))
check("the worker has no Windows line endings, which would stop it starting", b"\r" not in WORKER.read_bytes())

if sys.platform != "linux":
    print(f"learn worker tests: {passed} passed, {len(failed)} failed; the rest run on Linux, as the server does")
    sys.exit(1 if failed else 0)

# A stand-in command line: learn score writes one row a folder; sample and analyze pass the worker's check.
FAKE = """#!/bin/sh
case "$1" in
  sample) echo png > "$2"; exit 0 ;;
  analyze) echo "group: 25 shots, pooled"; exit 0 ;;
  learn)
    shift 2
    out=""; folders=""
    while [ $# -gt 0 ]; do
      case "$1" in --out) out="$2"; shift 2 ;; --build|--library) shift 2 ;; *) folders="$folders $1"; shift ;; esac
    done
    for f in $folders; do
      [ -f "$f/broken" ] && exit 3
      n=$(basename "$f")
      echo "{\\"Submission\\":\\"$n\\",\\"Holes\\":25,\\"Found\\":24,\\"FalseMarks\\":0}" >> "$out"
    done
    exit 0 ;;
esac
exit 2
"""


def build(root: Path, name: str) -> Path:
    cli = root / name
    (cli / "grouplab").mkdir(parents=True)
    exe = cli / "grouplab" / "grouplab"
    exe.write_text(FAKE)
    exe.chmod(0o755)
    (cli / "VERSION").write_text(f"0.2.0-nightly.{name} abc\n")
    return cli


def run(env: dict, mode: str) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(WORKER), mode], env=env, capture_output=True, text=True, timeout=120)


with tempfile.TemporaryDirectory(prefix="learn-worker-") as work:
    root = Path(work)
    private, clis = root / "private", root / "cli"
    ready = private / "ready"
    clis.mkdir()
    env = dict(os.environ, GROUPLAB_PRIVATE=str(private), GROUPLAB_LEARNING_CLI=str(clis), GROUPLAB_LEARN_LOG=str(root / "log"))
    env.pop("CREDENTIALS_DIRECTORY", None)

    old = time.time() - 600
    for name in ("2026-10-09_aaaaaaaa", "2026-10-09_bbbbbbbb"):
        (ready / name).mkdir(parents=True)
        (ready / name / "meta.json").write_text("{}")
        os.utime(ready / name, (old, old))
    (ready / "2026-10-09_bbbbbbbb" / "broken").write_text("")
    os.utime(ready / "2026-10-09_bbbbbbbb", (old, old))

    result = run(env, "score")
    check("with no command line installed, nothing is scored and nothing fails", result.returncode == 0 and not (private / "learning" / "rows.jsonl").exists(),
          result.stdout + result.stderr)

    (clis / "current").symlink_to(build(clis, "100"))
    for _ in range(3):
        result = run(env, "score")
    rows = [json.loads(l) for l in (private / "learning" / "rows.jsonl").read_text().splitlines()]
    check("a submission in ready is scored once, its row kept", [r["Submission"] for r in rows] == ["2026-10-09_aaaaaaaa"], json.dumps(rows))
    check("and marked scored, so the archive worker takes it", (private / "learning" / "scored" / "2026-10-09_aaaaaaaa").read_text() == "scored\n")
    check("one that cannot be read is left unscored after three tries, so the archive is never held up",
          (private / "learning" / "scored" / "2026-10-09_bbbbbbbb").read_text() == "unscored\n", result.stdout)
    check("the submission itself is never touched", (ready / "2026-10-09_aaaaaaaa" / "meta.json").exists())

    # The self-update: a signed build is installed and becomes current; a build whose signature does not verify is not.
    site = root / "site"
    site.mkdir()
    subprocess.run(["openssl", "genpkey", "-algorithm", "EC", "-pkeyopt", "ec_paramgen_curve:P-256", "-out", str(root / "key.pem")], check=True,
                   capture_output=True)
    subprocess.run(["openssl", "pkey", "-in", str(root / "key.pem"), "-pubout", "-out", str(root / "pub.pem")], check=True, capture_output=True)

    def publish(name: str, sign: bool) -> None:
        cli = build(root / "pack", name)
        with tarfile.open(site / "grouplab-cli-linux-arm64.tar.gz", "w:gz") as tar:
            for item in cli.iterdir():
                tar.add(item, arcname=item.name)
        if sign:
            subprocess.run(["openssl", "dgst", "-sha256", "-sign", str(root / "key.pem"), "-out", str(site / "grouplab-cli-linux-arm64.tar.gz.sig"),
                            str(site / "grouplab-cli-linux-arm64.tar.gz")], check=True)
        else:
            (site / "grouplab-cli-linux-arm64.tar.gz.sig").write_bytes(b"not a signature")

    server = HTTPServer(("127.0.0.1", 0), partial(SimpleHTTPRequestHandler, directory=str(site)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    env_n = dict(env, GROUPLAB_NIGHTLY_URL=f"http://127.0.0.1:{server.server_address[1]}", GROUPLAB_UPDATE_PUBLIC_KEY=str(root / "pub.pem"))
    import importlib.util
    spec = importlib.util.spec_from_file_location("worker", WORKER)
    for key, value in env_n.items():
        os.environ[key] = value
    worker = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(worker)

    publish("200", sign=False)
    before = (clis / "current").resolve()
    worker.update_cli()
    check("a build whose signature does not verify is not installed", (clis / "current").resolve() == before)

    publish("201", sign=True)
    worker.update_cli()
    now = (clis / "current").resolve()
    check("a signed build that reads its sample is installed and becomes current", (now / "VERSION").read_text().startswith("0.2.0-nightly.201"),
          str(now))
    check("the one before is kept to fall back to", (clis / "previous").resolve() == before)
    server.shutdown()

    # The nightly reads each submission in a process of its own, and one that fails (as one too big for the memory cap does) is skipped.
    fetched = root / "fetched"

    def fetch_one(name: str) -> Path:
        folder = fetched / name
        folder.mkdir(parents=True)
        if name.endswith("broken"):
            (folder / "broken").write_text("")
        return folder

    found, failed_names = worker.rescore(now, ["2026-10-01_aaaaaaaa", "2026-10-02_broken", "2026-10-03_cccccccc"], fetch_one)
    check("the nightly reads the others when one submission cannot be read", sorted(found) == ["2026-10-01_aaaaaaaa", "2026-10-03_cccccccc"],
          json.dumps(found))
    check("and names the one it could not read, for tomorrow", failed_names == ["2026-10-02_broken"], str(failed_names))
    check("each fetched submission is deleted once read", not any(fetched.iterdir()))

print(f"learn worker tests: {passed} passed, {len(failed)} failed")
sys.exit(1 if failed else 0)
