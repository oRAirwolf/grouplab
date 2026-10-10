#!/usr/bin/env python3
"""The learning loop on the web server, NOTES-FROM-PLANNING.md entries 394 and 395. No person and no Claude takes part in any step.

    grouplab-learn-worker.py score     every few minutes, with no network: each submission the intake worker left in ready is read by
                                       the current command line and scored against the person's own corrections, before the archive
                                       worker files it and deletes it, so no picture stays on the server longer than it did before
    grouplab-learn-worker.py nightly   after the nightly: the newest command line fetched and its signature checked, every corrected
                                       submission read again from the archive, the synthetic board beside it, the check against the
                                       baseline, the summary and the scoreboard written to the archive repository, and one issue in the
                                       error-report repository when a line is worse
    grouplab-learn-worker.py tune      once a month: the constant search, once there are enough corrected submissions, and a pull request
                                       when its result passes, which Code reads and merges (entry 396)

**What it keeps.** Numbers and labels only, in private/learning/ (rows.jsonl, baseline.jsonl, summary.md, tuning.md, and a marker a
submission for every one scored), and the same files in the archive repository's learning/ folder. Never a picture, never a location,
never a name or a file path. Pictures the nightly and the search need are fetched from the private archive into the service's own
temporary folder and are gone when the run ends (Alan's retention rule of 2026-09-25: nothing on the server longer than needed).

**The command line** is the nightly's linux-arm64 build, `grouplab-cli-linux-arm64.tar.gz` on the rolling release, installed into
/home/airwolf/grouplab-learning/cli/<first 12 of its SHA-256>/ only when its signature verifies against the update key's public half
the site sync already trusts (/etc/grouplab-site-sync/update-signing.pub) and it reads a sample sheet; `current` then points at it and
`previous` at the one before, which is kept to fall back to (entry 395 section 4).

**The tokens.** The archive token (the archive worker's, Contents read and write on the archive repository) reads the archive and writes
learning/ there. The learning token (request 88: issues on the error-report repository, a pull request on GroupLab's) is needed only to
file a report or open a pull request; until Alan sets it with grouplab-set-learning-token, those are skipped and the log says so. Every
nightly checks that it still reaches both, and says so in the log and in summary.md (question 95). The two tokens come
from systemd's credentials, never from a file in a repository, an argument or a log.
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from datetime import datetime, timezone
from pathlib import Path

PRIVATE = Path(os.environ.get("GROUPLAB_PRIVATE", "/home/airwolf/web/grouplab.org/private"))
READY = PRIVATE / "ready"
STATE = PRIVATE / "learning"
CLI = Path(os.environ.get("GROUPLAB_LEARNING_CLI", "/home/airwolf/grouplab-learning/cli"))
LOG = Path(os.environ.get("GROUPLAB_LEARN_LOG", "/home/airwolf/logs/grouplab-learn-worker.log"))
PUBLIC_KEY = Path(os.environ.get("GROUPLAB_UPDATE_PUBLIC_KEY", "/etc/grouplab-site-sync/update-signing.pub"))
API = os.environ.get("GROUPLAB_GITHUB_API", "https://api.github.com").rstrip("/")
NIGHTLY = os.environ.get("GROUPLAB_NIGHTLY_URL", "https://github.com/oRAirwolf/grouplab/releases/download/nightly")
ARCHIVE_REPO = "oRAirwolf/grouplab-submissions-archive"
REPORTS_REPO = "oRAirwolf/grouplab-crash-reports"
GROUPLAB_REPO = "oRAirwolf/grouplab"
TARBALL = "grouplab-cli-linux-arm64.tar.gz"
ISSUE_TITLE = "Learning loop: real targets read worse than their baseline"
DETECTOR = "src/GroupLab.Core/Detection/RenderDifferenceHoleDetector.cs"
NAME = re.compile(r"^\d{4}-\d{2}-\d{2}_[0-9a-f]{8}$")
SETTLE_SECONDS = 60
MOST_ATTEMPTS = 3
# RealScoreboard.CorrectedForTuning, held equal to it by tests/python/learn-worker-tests.py.
CORRECTED_FOR_TUNING = 50
SCORE_TIMEOUT = 900
SYNTHETIC_TIMEOUT = 3600


class Unreachable(Exception):
    """GitHub did not answer, or answered with a server error: the run stops and the next one tries again."""


def log(message: str) -> None:
    line = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ") + " " + message
    try:
        LOG.parent.mkdir(parents=True, exist_ok=True)
        with LOG.open("a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass
    print(line, flush=True)


def credential(name: str) -> str:
    folder = os.environ.get("CREDENTIALS_DIRECTORY")
    if not folder:
        return ""
    try:
        return (Path(folder) / name).read_text(encoding="utf-8").strip()
    except OSError:
        return ""


# ---- GitHub, the archive worker's way: the token goes to the API only, never along a redirect to storage.

class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):  # noqa: D401 - urllib's own signature
        return None


_opener = urllib.request.build_opener(_NoRedirect)


def call(method: str, url: str, token: str, body: bytes | None = None, accept: str = "application/vnd.github+json") -> tuple[int, bytes, dict]:
    headers = {"Authorization": f"Bearer {token}", "Accept": accept, "X-GitHub-Api-Version": "2022-11-28", "User-Agent": "grouplab-learn-worker"}
    if body is not None:
        headers["Content-Type"] = "application/json"
    try:
        with _opener.open(urllib.request.Request(url, data=body, method=method, headers=headers), timeout=120) as r:
            return r.status, r.read(), dict(r.headers)
    except urllib.error.HTTPError as e:
        if e.code >= 500:
            raise Unreachable(f"GitHub answered {e.code}") from None
        return e.code, e.read() if e.code not in (301, 302, 307, 308) else b"", dict(e.headers)
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        raise Unreachable(type(e).__name__) from None


def api(method: str, path: str, token: str, payload: dict | None = None) -> tuple[int, object]:
    status, body, _ = call(method, f"{API}{path}", token, json.dumps(payload).encode() if payload is not None else None)
    return status, (json.loads(body) if body else None)


def fetch(url: str, target: Path) -> None:
    """A public file, without any token."""
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "grouplab-learn-worker"}), timeout=300) as r, target.open("wb") as f:
            shutil.copyfileobj(r, f)
    except (urllib.error.URLError, OSError) as e:
        raise Unreachable(f"{url.rsplit('/', 1)[-1]} could not be fetched: {type(e).__name__}") from None


def download_asset(asset_id: int, token: str, target: Path) -> None:
    status, body, headers = call("GET", f"{API}/repos/{ARCHIVE_REPO}/releases/assets/{asset_id}", token, accept="application/octet-stream")
    if status in (301, 302, 307, 308):
        fetch(headers.get("Location") or headers.get("location"), target)
    elif status == 200:
        target.write_bytes(body)
    else:
        raise Unreachable(f"an archive download answered {status}")


# ---- The scoreboard's files.

def read_rows(path: Path) -> dict[str, dict]:
    if not path.exists():
        return {}
    rows = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    return {row["Submission"]: row for row in rows}


def write_rows(path: Path, rows: dict[str, dict]) -> None:
    path.write_text("".join(json.dumps(rows[k], separators=(",", ":")) + "\n" for k in sorted(rows)), encoding="utf-8")


def corrected(rows: dict[str, dict]) -> list[str]:
    return sorted(name for name, row in rows.items() if row.get("Holes") is not None)


# ---- The command line.

def current() -> Path | None:
    link = CLI / "current"
    return link.resolve() if link.exists() else None


def version(cli: Path) -> str:
    try:
        return (cli / "VERSION").read_text(encoding="utf-8").split()[0]
    except (OSError, IndexError):
        return "unknown"


def grouplab(cli: Path, args: list[str], cwd: Path | None = None, timeout: int = SCORE_TIMEOUT) -> subprocess.CompletedProcess:
    environment = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", HOME=tempfile.gettempdir())
    return subprocess.run([str(cli / "grouplab" / "grouplab"), *args], cwd=str(cwd or cli), capture_output=True, text=True,
                          timeout=timeout, env=environment)


def works(cli: Path) -> bool:
    """The build draws a sample sheet and reads it back, the arm64 trial's check, before anything uses it."""
    with tempfile.TemporaryDirectory(prefix="cli-check-") as work:
        sample = Path(work) / "sample.png"
        drawn = grouplab(cli, ["sample", str(sample), "--target", "targets/GL-CF25-LTR.gltd.json", "--seed", "395"], timeout=300)
        if drawn.returncode != 0:
            return False
        read = grouplab(cli, ["analyze", str(sample), "--library", "targets"], timeout=300)
        return read.returncode == 0 and "25 shots" in read.stdout


def update_cli() -> Path | None:
    """The nightly's command line, installed only when its signature verifies and it works; otherwise the one in place stays."""
    CLI.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="cli-") as work:
        tarball, signature = Path(work) / TARBALL, Path(work) / (TARBALL + ".sig")
        try:
            fetch(f"{NIGHTLY}/{TARBALL}", tarball)
            fetch(f"{NIGHTLY}/{TARBALL}.sig", signature)
        except Unreachable as e:
            log(f"the command line was not fetched ({e}); keeping {version(current()) if current() else 'none'}")
            return current()
        checked = subprocess.run(["openssl", "dgst", "-sha256", "-verify", str(PUBLIC_KEY), "-signature", str(signature), str(tarball)],
                                 capture_output=True, text=True)
        if checked.returncode != 0:
            log("the command line's signature does not verify against the update key; it was not installed")
            return current()
        digest = hashlib.sha256(tarball.read_bytes()).hexdigest()[:12]
        target = CLI / digest
        if current() == target.resolve():
            return current()
        if not target.exists():
            unpacking = CLI / (digest + ".unpacking")
            shutil.rmtree(unpacking, ignore_errors=True)
            with tarfile.open(tarball) as archive:
                archive.extractall(unpacking, filter="data")
            unpacking.rename(target)
    if not works(target):
        log(f"the command line {version(target)} did not read its sample sheet; keeping {version(current()) if current() else 'none'}")
        shutil.rmtree(target, ignore_errors=True)
        return current()
    before = current()
    for name, points in (("previous", before), ("current", target)):
        if points is None:
            continue
        link = CLI / (name + ".new")
        link.unlink(missing_ok=True)
        link.symlink_to(points)
        link.replace(CLI / name)
    keep = {target.resolve(), before.resolve() if before else None}
    for old in CLI.iterdir():
        if old.is_dir() and not old.is_symlink() and old.resolve() not in keep and not old.name.startswith(("current", "previous")):
            shutil.rmtree(old, ignore_errors=True)
    log(f"the command line is now {version(target)}, the one before kept to fall back to")
    return target


def score(cli: Path, folders: list[Path]) -> list[dict]:
    """Rows for these submission folders, from the command line; numbers and labels only."""
    if not folders:
        return []
    with tempfile.TemporaryDirectory(prefix="rows-") as work:
        out = Path(work) / "rows.jsonl"
        done = grouplab(cli, ["learn", "score", *map(str, folders), "--build", version(cli), "--library", "targets", "--out", str(out)],
                        timeout=SCORE_TIMEOUT * max(1, len(folders)))
        if done.returncode != 0 or not out.exists():
            raise ValueError(f"grouplab learn score answered {done.returncode}")
        return [json.loads(line) for line in out.read_text(encoding="utf-8").splitlines() if line.strip()]


# ---- score: each new submission, before the archive worker takes it.

def command_score() -> int:
    STATE.mkdir(parents=True, exist_ok=True)
    (STATE / "scored").mkdir(exist_ok=True)
    (STATE / "attempts").mkdir(exist_ok=True)
    cli = current()
    if cli is None:
        log("no command line installed yet; the nightly run installs one, and submissions are archived meanwhile unscored")
        return 0
    now = time.time()
    waiting = sorted(p for p in READY.iterdir() if p.is_dir() and NAME.match(p.name) and now - p.stat().st_mtime > SETTLE_SECONDS
                     and not (STATE / "scored" / p.name).exists()) if READY.is_dir() else []
    rows_path = STATE / "rows.jsonl"
    for folder in waiting:
        marker, tries = STATE / "scored" / folder.name, STATE / "attempts" / folder.name
        try:
            row = score(cli, [folder])[0]
        except (ValueError, OSError, IndexError, subprocess.TimeoutExpired) as e:
            n = int(tries.read_text()) + 1 if tries.exists() else 1
            tries.write_text(str(n))
            log(f"{folder.name}: not scored, try {n}: {type(e).__name__}")
            if n >= MOST_ATTEMPTS:
                marker.write_text("unscored\n")
                log(f"{folder.name}: left unscored after {n} tries, so the archive worker takes it as before")
            continue
        rows = read_rows(rows_path)
        rows[row["Submission"]] = row
        write_rows(rows_path, rows)
        marker.write_text("scored\n")
        tries.unlink(missing_ok=True)
        log(f"{folder.name}: scored, {row.get('Found')} of {row.get('Holes')} found, {row.get('FalseMarks')} false marks")
    return 0


# ---- nightly and tune: from the archive.

def archive_submissions(token: str) -> dict[str, int]:
    """Every submission the archive holds, name to its zip's asset id, from every month's release."""
    status, releases = api("GET", f"/repos/{ARCHIVE_REPO}/releases?per_page=100", token)
    if status != 200 or not isinstance(releases, list):
        raise Unreachable(f"the archive's releases answered {status}")
    found = {}
    for release in releases:
        if not str(release.get("tag_name", "")).startswith("archive-"):
            continue
        for asset in release.get("assets", []):
            name = asset["name"].removesuffix(".zip")
            if asset["name"].endswith(".zip") and NAME.match(name):
                found[name] = asset["id"]
    return found


def unpack(names: list[str], where: dict[str, int], token: str, work: Path) -> list[Path]:
    folders = []
    for name in names:
        zipped = work / f"{name}.zip"
        download_asset(where[name], token, zipped)
        with zipfile.ZipFile(zipped) as archive:
            archive.extractall(work / name)
        zipped.unlink()
        folders.append(work / name)
    return folders


def put_learning(name: str, text: str, token: str) -> None:
    """learning/<name> in the archive repository, numbers only, through the contents API with the archive token."""
    path = f"/repos/{ARCHIVE_REPO}/contents/learning/{name}"
    status, existing = api("GET", path, token)
    payload = {"message": f"learning: {name}", "content": base64.b64encode(text.encode("utf-8")).decode("ascii"),
               "committer": {"name": "grouplab-learning", "email": "grouplab-learning@users.noreply.github.com"}}
    if status == 200 and isinstance(existing, dict):
        if base64.b64decode(existing.get("content", "")).decode("utf-8", "replace") == text:
            return
        payload["sha"] = existing["sha"]
    status, _ = api("PUT", path, token, payload)
    if status not in (200, 201):
        raise Unreachable(f"learning/{name} could not be written ({status})")


def report(drops: str, build: str) -> None:
    token = credential("learning-token")
    if not token:
        log("lines are worse than their baseline, and the learning token is not set (request 88), so no issue was filed")
        return
    status, issues = api("GET", f"/repos/{REPORTS_REPO}/issues?state=open&per_page=100", token)
    number = next((i["number"] for i in issues if i.get("title") == ISSUE_TITLE), None) if status == 200 and isinstance(issues, list) else None
    body = (f"Build {build}. These lines of the real scoreboard read worse than their baseline by more than the scoreboard's margins "
            f"(docs/DETECTION-LEARNING-STUDY.md section 9). Numbers only; no picture is attached.\n\n{drops}")
    if number is None:
        status, _ = api("POST", f"/repos/{REPORTS_REPO}/issues", token, {"title": ISSUE_TITLE, "body": body})
    else:
        status, _ = api("POST", f"/repos/{REPORTS_REPO}/issues/{number}/comments", token, {"body": body})
    log(f"the regression report was {'filed' if status == 201 else f'not filed ({status})'}")


def token_check() -> str:
    """Question 95, answer (b), Alan, 2026-10-10: the learning token is used only on a night a line reads worse and once a month for a
    tuning pull request, so a wrong or expired one would be found on the night it matters. Every nightly asks GitHub with it for one issue
    and one pull request and says what came back, in the log and in summary.md. Never the token itself."""
    token = credential("learning-token")
    if not token:
        return "- The learning token is not set (request 88)."
    answers = []
    for what, path in (("the error reports", f"/repos/{REPORTS_REPO}/issues?per_page=1"), ("GroupLab's pull requests", f"/repos/{GROUPLAB_REPO}/pulls?per_page=1")):
        try:
            status = call("GET", f"{API}{path}", token)[0]
            answers.append(f"{what}: {'yes' if status == 200 else f'no ({status})'}")
        except Unreachable as e:
            answers.append(f"{what}: GitHub did not answer ({e})")
    return "- The learning token reaches " + "; ".join(answers) + "."


def rescore(cli: Path, names: list[str], fetch_one) -> tuple[dict[str, dict], list[str]]:
    """Each submission read in a process of its own and deleted before the next is fetched (entry 395 section 6, measured on the server:
    one 600 dpi scan peaks at 1.1 GB, so ten in one process passed the 1.5 GB cap and the whole run was stopped). One that cannot be read,
    for memory or anything else, is skipped and named, and the rest go on."""
    rows, failed = {}, []
    for name in names:
        folder = None
        try:
            folder = fetch_one(name)
            for row in score(cli, [folder]):
                rows[row["Submission"]] = row
        except (ValueError, OSError, IndexError, zipfile.BadZipFile, subprocess.TimeoutExpired):
            failed.append(name)
        finally:
            if folder is not None:
                shutil.rmtree(folder, ignore_errors=True)
    return rows, failed


def command_nightly() -> int:
    STATE.mkdir(parents=True, exist_ok=True)
    reach = token_check()
    log(reach[2:])
    cli = update_cli()
    if cli is None:
        log("no command line could be installed, so nothing was read tonight")
        return 1
    token = credential("archive-token")
    if not token:
        log("the archive token is not set, so the archive cannot be read tonight")
        return 0
    rows_path = STATE / "rows.jsonl"
    rows = read_rows(rows_path)
    where = archive_submissions(token)
    # Every corrected submission read again by tonight's build, and any the score run never saw (archived before this worker existed).
    wanted = sorted((set(where) - set(rows)) | {n for n in corrected(rows) if n in where})
    with tempfile.TemporaryDirectory(prefix="nightly-") as work:
        found, failed = rescore(cli, wanted, lambda name: unpack([name], where, token, Path(work))[0])
    rows.update(found)
    if failed:
        log(f"{len(failed)} not read tonight, tried again tomorrow: " + ", ".join(failed))
    write_rows(rows_path, rows)
    try:
        synthetic = grouplab(cli, ["scoreboard", "--synthetic", "--baseline", "docs/scoreboard/synthetic-baseline.json"], timeout=SYNTHETIC_TIMEOUT)
        drops_synthetic = [line[5:] for line in synthetic.stderr.splitlines() if line.startswith("DROP ")]
        verdict = "every condition within its margin." if synthetic.returncode == 0 else f"{len(drops_synthetic)} condition(s) fell: " + "; ".join(drops_synthetic)
    except subprocess.TimeoutExpired:
        # The real submissions' check still runs and is written; only the synthetic line says it is missing tonight.
        verdict = f"not read tonight: the synthetic board did not finish in {SYNTHETIC_TIMEOUT // 60} minutes."
        log(verdict)
    with tempfile.TemporaryDirectory(prefix="check-") as work:
        (Path(work) / "synthetic.txt").write_text(verdict, encoding="utf-8")
        checked = grouplab(cli, ["learn", "check", "--rows", str(rows_path), "--baseline", str(STATE / "baseline.jsonl"), "--write-baseline",
                                 "--build", version(cli), "--summary", str(STATE / "summary.md"), "--drops", str(Path(work) / "drops.md"),
                                 "--synthetic", str(Path(work) / "synthetic.txt")])
        drops = (Path(work) / "drops.md").read_text(encoding="utf-8") if (Path(work) / "drops.md").exists() else ""
    if checked.returncode not in (0, 1):
        log(f"grouplab learn check answered {checked.returncode}")
        return 1
    if (STATE / "summary.md").exists():
        with (STATE / "summary.md").open("a", encoding="utf-8", newline="\n") as f:
            f.write(reach + "\n")
    for name in ("rows.jsonl", "baseline.jsonl", "summary.md"):
        if (STATE / name).exists():
            put_learning(name, (STATE / name).read_text(encoding="utf-8"), token)
    log(f"nightly with {version(cli)}: {len(wanted)} read, {len(corrected(rows))} corrected, {'lines worse' if drops.strip() else 'nothing worse'}")
    if drops.strip():
        report(drops, version(cli))
    return 0


def command_tune() -> int:
    cli = current()
    token = credential("archive-token")
    rows = read_rows(STATE / "rows.jsonl")
    names = corrected(rows)
    if len(names) < CORRECTED_FOR_TUNING or cli is None or not token:
        text = (f"# Tuning\n\n- {len(names)} corrected submissions of the {CORRECTED_FOR_TUNING} a held-out check needs "
                f"(RealScoreboard.CorrectedForTuning); nothing was searched.\n")
        (STATE / "tuning.md").write_text(text, encoding="utf-8")
        if token:
            put_learning("tuning.md", text, token)
        log(f"tuning waits: {len(names)} of {CORRECTED_FOR_TUNING} corrected submissions")
        return 0
    where = archive_submissions(token)
    with tempfile.TemporaryDirectory(prefix="tune-") as work:
        unpack([n for n in names if n in where], where, token, Path(work))
        out = Path(work) / "result.json"
        done = grouplab(cli, ["learn", "tune", work, "--out", str(out), "--library", "targets"], timeout=6 * 3600)
        if done.returncode != 0 or not out.exists():
            log(f"grouplab learn tune answered {done.returncode}")
            return 1
        result = json.loads(out.read_text(encoding="utf-8"))
    text = f"# Tuning, {version(cli)}\n\n- {result['reason']}.\n"
    (STATE / "tuning.md").write_text(text, encoding="utf-8")
    put_learning("tuning.md", text, token)
    if result.get("accepted"):
        pull_request(result, version(cli))
    return 0


def pull_request(result: dict, build: str) -> None:
    """A branch with the new constants and nothing else, and a pull request with the table, which Code reads and merges (entry 396)."""
    token = credential("learning-token")
    if not token:
        log("tuning passed, and the learning token is not set (request 88), so no pull request was opened; tuning.md has the result")
        return
    status, main = api("GET", f"/repos/{GROUPLAB_REPO}/git/ref/heads/main", token)
    if status != 200:
        raise Unreachable(f"GroupLab's main could not be read ({status})")
    branch = f"tuning/{build}"
    api("POST", f"/repos/{GROUPLAB_REPO}/git/refs", token, {"ref": f"refs/heads/{branch}", "sha": main["object"]["sha"]})
    status, source = api("GET", f"/repos/{GROUPLAB_REPO}/contents/{DETECTOR}?ref={urllib.parse.quote(branch)}", token)
    text = base64.b64decode(source["content"]).decode("utf-8")
    for name, value in result["constants"].items():
        text, n = re.subn(rf"(double {name} = )[0-9.]+", rf"\g<1>{value}", text, count=1)
        if n != 1:
            log(f"{name} was not found once in the detector, so no pull request was opened")
            return
    message = (f"Detection constants from the learning loop, {build}\n\nRelease-note: Hole finding uses settings tuned against the targets "
               f"people have sent and corrected, and finds more holes on the ones held back to check it. (Entry 394)\nRelease-note-kind: changed")
    api("PUT", f"/repos/{GROUPLAB_REPO}/contents/{DETECTOR}", token,
        {"message": message, "content": base64.b64encode(text.encode("utf-8")).decode("ascii"), "sha": source["sha"], "branch": branch})
    held = result["heldOut"]
    body = "\n".join([
        "The learning loop's constant search (NOTES-FROM-PLANNING.md entry 394 section 4) found these, and they passed: better on the held-out",
        "submissions, worse on none, and G3 holds around them (recall above 90 percent with any threshold 30 percent off).", "",
        "| Constant | Was | Now |", "|---|---|---|", *[f"| {k} | {result['was'][k]} | {v} |" for k, v in result["constants"].items()], "",
        f"Held out: {held['submissions']} submissions. Publishable ones: found {held['publicFound']}; false marks {held['publicFalseMarks']}. "
        f"Testing-only ones checked as well: {held['testingOnlyCount']}, whose numbers stay private.", "", result["table"],
        "Code reads this table and the checks and merges it when every gate passes and nothing here is odd (entry 396); otherwise it says why here and it waits."])
    status, _ = api("POST", f"/repos/{GROUPLAB_REPO}/pulls", token,
                    {"title": f"Detection constants from the learning loop, {build}", "head": branch, "base": "main", "body": body})
    log(f"the tuning pull request was {'opened' if status == 201 else f'not opened ({status})'}")


def main() -> int:
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    try:
        if command == "score":
            return command_score()
        if command == "nightly":
            return command_nightly()
        if command == "tune":
            return command_tune()
    except Unreachable as e:
        log(f"{command}: stopped, GitHub could not be reached ({e}); the next run tries again")
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main())
