#!/usr/bin/env python3
"""Put each checked submission in the private archive and take it off the server, NOTES-FROM-PLANNING.md entry 222 section 2.

The intake worker leaves each submission it has rebuilt and checked in private/ready. This zips one exactly as the PC pull does (the
folder's files at the root of the zip), uploads it to the month's release in the private repository oRAirwolf/grouplab-submissions-archive,
downloads it back and compares the SHA-256, rewrites the release's manifest.json with it, and only then deletes the folder. A submission the
archive already lists is proven the same way, by downloading it back, before its folder goes. A failure leaves the folder where it is to be
tried again on the next run; after five failures it is reported once as an error report, which the error worker turns into an issue that
Code reads. So nothing leaves the server that the archive has not been shown to hold, and this computer is never needed for it.

**The token.** A fine-grained token limited to that one repository with Contents read and write, never in this file, the repository, a log
or an argument. systemd hands it to this service alone with LoadCredential, from a root-owned file Alan fills with
grouplab-set-archive-token. It is sent to api.github.com and uploads.github.com only, never along the redirect to the storage a download
goes to. Until it is set, every submission waits in ready, as it did before, and the state says so.
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import shutil
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zipfile
from datetime import datetime, timezone
from pathlib import Path

PRIVATE = Path(os.environ.get("GROUPLAB_PRIVATE", "/home/airwolf/web/grouplab.org/private"))
READY = PRIVATE / "ready"
STATE = PRIVATE / "archive-worker"
ERRORS_INCOMING = PRIVATE / "error-reports" / "incoming"
LOG = Path(os.environ.get("GROUPLAB_ARCHIVE_LOG", "/home/airwolf/logs/grouplab-archive-worker.log"))
API = os.environ.get("GROUPLAB_GITHUB_API", "https://api.github.com").rstrip("/")
UPLOADS = os.environ.get("GROUPLAB_GITHUB_UPLOADS", "https://uploads.github.com").rstrip("/")
REPOSITORY = os.environ.get("GROUPLAB_ARCHIVE_REPO", "oRAirwolf/grouplab-submissions-archive")
TOKEN_FILE = Path(os.environ["CREDENTIALS_DIRECTORY"]) / "archive-token" if "CREDENTIALS_DIRECTORY" in os.environ \
    else Path(os.environ.get("GROUPLAB_ARCHIVE_TOKEN_FILE", "/nonexistent"))

NAME = re.compile(r"^(\d{4})-(\d{2})-\d{2}_[0-9a-f]{8}$")
MOST_A_RUN = 20
MOST_ATTEMPTS = 5
SETTLE_SECONDS = 120


class Unreachable(Exception):
    """GitHub did not answer, or answered with a server error: everything waits for the next run."""


class NotAccepted(Exception):
    """GitHub refused the token or the request: everything waits, and the state says why."""


def log(message: str) -> None:
    line = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ") + " " + message
    try:
        LOG.parent.mkdir(parents=True, exist_ok=True)
        with LOG.open("a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass
    print(line, flush=True)


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):  # noqa: D401 - urllib's own signature
        return None


_opener = urllib.request.build_opener(_NoRedirect)


def call(method: str, url: str, token: str, body: bytes | None = None, content_type: str | None = None,
         accept: str = "application/vnd.github+json") -> tuple[int, bytes, dict]:
    headers = {"Authorization": f"Bearer {token}", "Accept": accept, "X-GitHub-Api-Version": "2022-11-28", "User-Agent": "grouplab-archive-worker"}
    if content_type:
        headers["Content-Type"] = content_type
    request = urllib.request.Request(url, data=body, method=method, headers=headers)
    try:
        with _opener.open(request, timeout=120) as r:
            return r.status, r.read(), dict(r.headers)
    except urllib.error.HTTPError as e:
        if e.code in (301, 302, 307, 308):
            return e.code, b"", dict(e.headers)
        if e.code in (401, 403):
            raise NotAccepted(f"GitHub refused the request ({e.code})") from None
        if e.code >= 500:
            raise Unreachable(f"GitHub answered {e.code}") from None
        return e.code, e.read(), dict(e.headers)
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        raise Unreachable(type(e).__name__) from None


def api(method: str, path: str, token: str, payload: dict | None = None) -> tuple[int, dict | list | None]:
    status, body, _ = call(method, f"{API}{path}", token, json.dumps(payload).encode() if payload is not None else None,
                           "application/json" if payload is not None else None)
    return status, (json.loads(body) if body else None)


def download(asset_id: int, token: str) -> bytes:
    """An asset's bytes. GitHub answers with a redirect to its storage, which is fetched without the token."""
    status, body, headers = call("GET", f"{API}/repos/{REPOSITORY}/releases/assets/{asset_id}", token, accept="application/octet-stream")
    if status in (301, 302, 307, 308):
        location = headers.get("Location") or headers.get("location")
        try:
            with urllib.request.urlopen(urllib.request.Request(location, headers={"User-Agent": "grouplab-archive-worker"}), timeout=300) as r:
                return r.read()
        except (urllib.error.URLError, OSError) as e:
            raise Unreachable(f"the download could not be fetched: {type(e).__name__}") from None
    if status == 200:
        return body
    raise Unreachable(f"the download answered {status}")


def upload(release: dict, name: str, data: bytes, content_type: str, token: str) -> dict:
    for asset in release.get("assets", []):
        if asset["name"] == name:
            api("DELETE", f"/repos/{REPOSITORY}/releases/assets/{asset['id']}", token)
    url = f"{UPLOADS}/repos/{REPOSITORY}/releases/{release['id']}/assets?name={urllib.parse.quote(name)}"
    status, body, _ = call("POST", url, token, data, content_type)
    if status != 201:
        raise Unreachable(f"the upload of {name} answered {status}")
    return json.loads(body)


def release_for(tag: str, token: str) -> dict:
    status, release = api("GET", f"/repos/{REPOSITORY}/releases/tags/{tag}", token)
    if status == 200 and isinstance(release, dict):
        return release
    status, release = api("POST", f"/repos/{REPOSITORY}/releases", token, {
        "tag_name": tag, "name": f"Submissions, {tag[8:]}",
        "body": "Private. One zip a submission, exactly as pulled, and manifest.json. Never published from here."})
    if status != 201 or not isinstance(release, dict):
        raise Unreachable(f"the release {tag} could not be made ({status})")
    return release


def consent(folder: Path) -> str:
    """The level the PC pull writes into CONSENT.txt, read the same way: testing where the contributor opted out."""
    if (folder / "DO-NOT-PUBLISH").exists():
        return "testing"
    meta = json.loads((folder / "meta.json").read_text(encoding="utf-8"))
    level = (meta.get("consent") or {}).get("level") if isinstance(meta.get("consent"), dict) else None
    if level:
        return str(level)
    return "testing" if meta.get("exclude_from_public_dataset") else "publishable"


def zipped(folder: Path, into: Path) -> Path:
    out = into / f"{folder.name}.zip"
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        for f in sorted(p for p in folder.rglob("*") if p.is_file()):
            z.write(f, f.relative_to(folder).as_posix())
    return out


def archive(folder: Path, token: str) -> None:
    """Archives one submission and proves it; raises when the archive cannot be shown to hold it."""
    m = NAME.match(folder.name)
    tag = f"archive-{m.group(1)}-{m.group(2)}"
    release = release_for(tag, token)
    assets = {a["name"]: a for a in release.get("assets", [])}
    manifest = {"format": "grouplab-submissions-archive-1", "release": tag, "submissions": []}
    if "manifest.json" in assets:
        manifest = json.loads(download(assets["manifest.json"]["id"], token).decode("utf-8-sig"))
    submissions = [s for s in manifest.get("submissions", []) if s.get("name") != folder.name]
    listed = next((s for s in manifest.get("submissions", []) if s.get("name") == folder.name), None)
    asset_name = f"{folder.name}.zip"

    if listed is None or asset_name not in assets:
        with tempfile.TemporaryDirectory(prefix="gl-archive-") as tmp:
            z = zipped(folder, Path(tmp))
            data = z.read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        asset = upload(release, asset_name, data, "application/zip", token)
        listed = {"name": folder.name, "bytes": len(data), "sha256": digest, "consent": consent(folder)}
    else:
        asset = assets[asset_name]

    back = download(asset["id"], token)
    if hashlib.sha256(back).hexdigest() != str(listed["sha256"]).lower():
        raise ValueError(f"{folder.name} in the archive does not match its SHA-256")

    manifest = {
        "format": "grouplab-submissions-archive-1",
        "release": tag,
        "written": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "submissions": sorted(submissions + [listed], key=lambda s: s["name"]),
    }
    release = release_for(tag, token)
    upload(release, "manifest.json", (json.dumps(manifest, indent=1) + "\n").encode(), "application/json", token)


def report(name: str, why: str) -> None:
    """Five failures on one submission: one error report, which the error worker turns into an issue Code reads."""
    ERRORS_INCOMING.mkdir(parents=True, exist_ok=True)
    report_id = uuid.uuid4().hex
    body = {
        "schema": "grouplab-error-report-1", "report_id": report_id, "kind": "survived", "made": "automatic", "count": MOST_ATTEMPTS,
        "app": {"version": "archive-worker", "commit": "", "channel": "server"},
        "environment": {"os": "server", "framework": "Python", "renderer": "none", "display_scale": None},
        "exceptions": [{"type": "GroupLab.Server.ArchiveFailed", "message": f"{name} could not be archived after {MOST_ATTEMPTS} tries: {why}"[:2000], "stack": ""}],
        "last_actions": ["archive-worker"],
    }
    name_out = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H%M%SZ") + f"_{report_id}.json"
    tmp = ERRORS_INCOMING / f".{name_out}.tmp"
    tmp.write_text(json.dumps(body, indent=1) + "\n", encoding="utf-8")
    tmp.replace(ERRORS_INCOMING / name_out)


def status(**fields) -> None:
    STATE.mkdir(parents=True, exist_ok=True)
    fields["checked_utc"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    tmp = STATE / "status.tmp"
    tmp.write_text(json.dumps(fields, indent=1) + "\n", encoding="utf-8")
    tmp.replace(STATE / "status.json")


def reachable(token: str) -> None:
    """Entry 224: with nothing waiting, the run still proves the token reads the archive, so a broken token shows before it matters."""
    try:
        code, _ = api("GET", f"/repos/{REPOSITORY}", token)
    except NotAccepted:
        status(token="refused", waiting=0)
        log("the archive token was refused by GitHub")
        return
    except Unreachable as e:
        status(token="unchecked", waiting=0, last_error=str(e))
        return
    status(token="ok" if code == 200 else f"cannot see the archive ({code})", waiting=0, archive="reachable" if code == 200 else "not found")


def main() -> int:
    now = time.time()
    waiting = sorted(p for p in READY.iterdir() if p.is_dir() and NAME.match(p.name) and now - p.stat().st_mtime > SETTLE_SECONDS) \
        if READY.is_dir() else []
    try:
        token = TOKEN_FILE.read_text(encoding="utf-8").strip()
    except OSError:
        token = ""
    if not token:
        if waiting:
            log(f"{len(waiting)} waiting in ready, and no archive token has been set, so they stay there")
        status(token="not set", waiting=len(waiting))
        return 0
    if not waiting:
        reachable(token)
        return 0

    attempts_dir = STATE / "attempts"
    attempts_dir.mkdir(parents=True, exist_ok=True)
    done = 0
    for folder in waiting[:MOST_A_RUN]:
        tries = attempts_dir / folder.name
        try:
            archive(folder, token)
        except (Unreachable, NotAccepted) as e:
            log(f"{folder.name}: kept in ready, {e}")
            status(token="refused" if isinstance(e, NotAccepted) else "ok", waiting=len(waiting) - done, last_error=str(e))
            return 0 if isinstance(e, Unreachable) else 1
        except (ValueError, OSError, KeyError, json.JSONDecodeError) as e:
            n = int(tries.read_text()) + 1 if tries.exists() else 1
            tries.write_text(str(n))
            log(f"{folder.name}: kept in ready, try {n}: {type(e).__name__}: {e}")
            if n == MOST_ATTEMPTS:
                report(folder.name, f"{type(e).__name__}: {e}")
            continue
        shutil.rmtree(folder)
        tries.unlink(missing_ok=True)
        done += 1
        log(f"{folder.name}: archived, proven, and removed from the server")
    status(token="ok", waiting=len(waiting) - done, archived=done)
    return 0


if __name__ == "__main__":
    sys.exit(main())
