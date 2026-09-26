#!/usr/bin/env python3
"""The nightly backup and the weekly restore test, NOTES-FROM-PLANNING.md entry 222 sections 2.3, 3 and 6. Run by the scheduled tasks
that scripts/Register-GroupLabTasks.ps1 sets up, as Alan, while he is logged on; nothing here needs a password.

    python scripts/backup.py                 the nightly run: copy the archive's submissions here, then back up and prune
    python scripts/backup.py --dry-run       build everything, upload and delete nothing, say what would happen
    python scripts/backup.py --restore-test  the weekly proof: download the newest backup, check it, restore the bundle

**What is backed up** (entry 222 section 3.1): every branch and tag of this repository and of `grouplab-testdata` as git bundles; the
files git does not hold that matter (`docs/notes/panel.md`, the `.claude` settings); `C:\\Dev\\grouplab-local` and
`C:\\Dev\\grouplab-originals`; and the text of every issue in `grouplab-crash-reports`. **Never** `C:\\Dev\\keys`, the SSH key, any token or
secret, build output, or `%TEMP%`. `C:\\Dev\\grouplab-submissions` is covered by the private archive and the copy this run makes of it.

**Where**: one release a night in the private `oRAirwolf/grouplab-backups`, tag `backup-YYYY-MM-DD`, its files and a `manifest.json` of
every file with its SHA-256. Kept: 7 daily, 4 weekly (Sundays) and 6 monthly (the first backup of a month); the rest are deleted by the
same run. The newest is also kept in `C:\\Dev\\grouplab-local\\backups`, which the next one leaves out.

**A failure** is sent as an error report to grouplab.org, the path error reports already take, so it becomes an issue Code reads at the
start of every run; and it is written to `C:\\Dev\\grouplab-local\\automation.json`, which the weekly line in for-alan.md is made from.
Everything goes through the `gh` command line as whoever is signed in; nothing here reads or prints a token.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
import urllib.parse
import urllib.request
import uuid
import zipfile
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
DEV = REPO.parent
OWNER = "oRAirwolf"
BACKUPS = f"{OWNER}/grouplab-backups"
ARCHIVE = f"{OWNER}/grouplab-submissions-archive"
LOCAL = Path(os.environ.get("GROUPLAB_LOCAL", DEV / "grouplab-local"))
SUBMISSIONS = Path(os.environ.get("GROUPLAB_SUBMISSIONS", DEV / "grouplab-submissions"))
STATUS = LOCAL / "automation.json"
KEEP_COPY = LOCAL / "backups"
ERROR_RECEIVER = "https://grouplab.org/api/error-report.php"

# What goes into local.zip, as (the folder, the name it has in the zip). Only these; nothing else on the machine is read.
SOURCES = [
    (REPO / "docs" / "notes" / "panel.md", "repository/docs/notes/panel.md"),
    (REPO / ".claude", "repository/.claude"),
    (LOCAL, "grouplab-local"),
    (DEV / "grouplab-originals", "grouplab-originals"),
]
NEVER = {"bin", "obj", "out", "backups", "node_modules", "__pycache__", ".git"}
NEVER_PATHS = [DEV / "keys"]
MOST_ASSET = 1900 * 1024 * 1024


def log(message: str) -> None:
    print(datetime.now(timezone.utc).strftime("%H:%M:%S ") + message, flush=True)


def gh(*args: str, check: bool = True) -> subprocess.CompletedProcess:
    run = subprocess.run(["gh", *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and run.returncode != 0:
        raise RuntimeError(f"gh {' '.join(args[:3])} failed: {run.stderr.strip()[:300]}")
    return run


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def status(**fields) -> None:
    """Writes this run's outcome beside the others, for the weekly line."""
    LOCAL.mkdir(parents=True, exist_ok=True)
    now = json.loads(STATUS.read_text(encoding="utf-8")) if STATUS.is_file() else {}
    for key, value in fields.items():
        now[key] = value
    STATUS.write_text(json.dumps(now, indent=1) + "\n", encoding="utf-8", newline="\n")


def report_failure(what: str, detail: str, steps: list[str]) -> None:
    """A failure, as an error report: it becomes an issue in grouplab-crash-reports, which Code reads at the start of every run."""
    commit = subprocess.run(["git", "-C", str(REPO), "rev-parse", "--short", "HEAD"], capture_output=True, text=True).stdout.strip()
    report = {
        "schema": "grouplab-error-report-1",
        "report_id": uuid.uuid4().hex,
        "kind": "survived",
        "made": "automatic",
        "count": 1,
        "app": {"version": "automation", "commit": commit, "channel": "automation"},
        "environment": {"os": platform.platform()[:120], "framework": "Python " + platform.python_version(), "renderer": "none", "display_scale": 1},
        "exceptions": [{"type": f"GroupLab.Automation.{what}", "message": detail[:1900], "stack": ""}],
        "last_actions": steps[-20:],
    }
    body = urllib.parse.urlencode({"report": json.dumps(report)}).encode()
    try:
        with urllib.request.urlopen(urllib.request.Request(ERROR_RECEIVER, data=body, headers={"User-Agent": "GroupLab automation"}), timeout=30) as r:
            log(f"reported the failure ({r.status})")
    except OSError as e:
        log(f"the failure could not be reported: {type(e).__name__}")


def repository_exists(repo: str) -> bool:
    return gh("repo", "view", repo, "--json", "isPrivate", check=False).returncode == 0


README = """# GroupLab backups

Private. One release a night, `backup-YYYY-MM-DD`, written by `scripts/backup.py` in the grouplab repository: git bundles of every branch and
tag, the local-only files, and a manifest of every file's SHA-256. How to restore is in `docs/RESTORE.md` there. Never made public.
"""


def ensure_first_commit(repo: str) -> None:
    """GitHub makes no release in a repository with no commit (entry 224: the new backups repository was empty), so an empty one gets
    a README as its first commit. Nothing is written where anything already is."""
    if gh("api", f"repos/{repo}/commits?per_page=1", check=False).returncode == 0:
        return
    import base64
    gh("api", "-X", "PUT", f"repos/{repo}/contents/README.md", "-f", "message=What this repository is",
       "-f", "content=" + base64.b64encode(README.encode()).decode())
    log(f"{repo} was empty, so it now has a README as its first commit")


# ------------------------------------------------------------------------------------------------- the archive, copied here --

def sync_archive(dry: bool, steps: list[str]) -> dict:
    """Entry 222 section 2.3: every archived submission not yet in grouplab-submissions is downloaded and checked against the manifest."""
    got, checked, bad = [], 0, []
    listed = json.loads(gh("release", "list", "-R", ARCHIVE, "--limit", "1000", "--json", "tagName").stdout or "[]")
    for tag in [r["tagName"] for r in listed if r["tagName"].startswith("archive-")]:
        with tempfile.TemporaryDirectory(prefix="gl-sync-") as tmp:
            if gh("release", "download", tag, "-R", ARCHIVE, "-p", "manifest.json", "-D", tmp, check=False).returncode != 0:
                bad.append(f"{tag}: no manifest")
                continue
            manifest = json.loads(Path(tmp, "manifest.json").read_text(encoding="utf-8-sig"))
            for entry in manifest.get("submissions", []):
                name = entry["name"]
                checked += 1
                if (SUBMISSIONS / name).is_dir():
                    continue
                if dry:
                    got.append(name)
                    continue
                if gh("release", "download", tag, "-R", ARCHIVE, "-p", f"{name}.zip", "-D", tmp, check=False).returncode != 0:
                    bad.append(f"{name}: could not be downloaded")
                    continue
                zipped = Path(tmp, f"{name}.zip")
                if sha256(zipped) != entry["sha256"].lower():
                    bad.append(f"{name}: does not match the manifest")
                    continue
                with zipfile.ZipFile(zipped) as z:
                    z.extractall(SUBMISSIONS / name)
                zipped.unlink()
                got.append(name)
    steps.append(f"sync: {len(got)} new of {checked}")
    log(f"archive copy: {checked} in the archive, {len(got)} {'would be ' if dry else ''}copied here, {len(bad)} problems")
    return {"checked": checked, "copied": got, "problems": bad}


# ------------------------------------------------------------------------------------------------------------ the backup --

def included(path: Path) -> bool:
    if any(part in NEVER for part in path.parts):
        return False
    return not any(path == n or n in path.parents for n in NEVER_PATHS)


def build(work: Path, steps: list[str]) -> list[Path]:
    """The night's files: the bundles, local.zip, the crash reports, and the manifest of every file in them."""
    files: list[Path] = []
    entries: list[dict] = []
    for name, repo in [("grouplab.bundle", REPO), ("grouplab-testdata.bundle", DEV / "grouplab-testdata")]:
        if (repo / ".git").exists():
            out = work / name
            run = subprocess.run(["git", "-C", str(repo), "bundle", "create", str(out), "--all"], capture_output=True, text=True)
            if run.returncode != 0:
                raise RuntimeError(f"git bundle of {repo.name} failed: {run.stderr.strip()[:300]}")
            files.append(out)
    steps.append("bundles")

    local = work / "local.zip"
    with zipfile.ZipFile(local, "w", zipfile.ZIP_DEFLATED) as z:
        for source, inside in SOURCES:
            if source.is_file():
                z.write(source, inside)
                entries.append({"path": inside, "bytes": source.stat().st_size, "sha256": sha256(source)})
            elif source.is_dir():
                for f in sorted(p for p in source.rglob("*") if p.is_file()):
                    if not included(f):
                        continue
                    name = inside + "/" + f.relative_to(source).as_posix()
                    z.write(f, name)
                    entries.append({"path": name, "bytes": f.stat().st_size, "sha256": sha256(f)})
    files.append(local)
    steps.append(f"local.zip: {len(entries)} files")

    crash = work / "crash-reports.json"
    issues = gh("issue", "list", "-R", f"{OWNER}/grouplab-crash-reports", "--state", "all", "--limit", "1000",
                "--json", "number,title,state,labels,createdAt,updatedAt,body,comments", check=False)
    crash.write_text(issues.stdout if issues.returncode == 0 else "[]", encoding="utf-8", newline="\n")
    files.append(crash)
    steps.append("crash reports")

    for f in files:
        if f.stat().st_size > MOST_ASSET:
            raise RuntimeError(f"{f.name} is {f.stat().st_size >> 20} MB, over the 1.9 GB a release asset may be; split it before the next run")
    manifest = work / "manifest.json"
    manifest.write_text(json.dumps({
        "format": "grouplab-backup-1",
        "written": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "assets": [{"name": f.name, "bytes": f.stat().st_size, "sha256": sha256(f)} for f in files],
        "local": entries,
    }, indent=1) + "\n", encoding="utf-8", newline="\n")
    return files + [manifest]


def keep(tags: list[str], today: date) -> set[str]:
    """Entry 222 section 3.2: 7 daily, 4 weekly on Sundays, 6 monthly on the first backup of each month."""
    dated = sorted(((date.fromisoformat(t[7:]), t) for t in tags if t.startswith("backup-") and len(t) == 17), reverse=True)
    kept = {t for _, t in dated[:7]}
    kept |= {t for d, t in [x for x in dated if x[0].weekday() == 6][:4]}
    firsts: dict[tuple[int, int], tuple[date, str]] = {}
    for d, t in dated:
        firsts[(d.year, d.month)] = (d, t)
    kept |= {t for _, t in sorted(firsts.values(), reverse=True)[:6]}
    return kept


def backup(dry: bool, steps: list[str]) -> dict:
    tag = "backup-" + date.today().isoformat()
    with tempfile.TemporaryDirectory(prefix="gl-backup-") as tmp:
        files = build(Path(tmp), steps)
        total = sum(f.stat().st_size for f in files)
        log(f"built {len(files)} files, {total / 1e6:.1f} MB")
        if dry:
            log(f"dry run: would upload {tag} to {BACKUPS}")
            return {"tag": tag, "bytes": total, "uploaded": False}
        if not repository_exists(BACKUPS):
            log(f"{BACKUPS} does not exist yet (request 35 step 1), so the backup is kept here only")
            KEEP_COPY.mkdir(parents=True, exist_ok=True)
            for old in KEEP_COPY.iterdir():
                old.unlink()
            for f in files:
                shutil.copy2(f, KEEP_COPY / f.name)
            return {"tag": tag, "bytes": total, "uploaded": False, "waiting": "the backups repository"}
        ensure_first_commit(BACKUPS)
        if gh("release", "view", tag, "-R", BACKUPS, check=False).returncode != 0:
            gh("release", "create", tag, "-R", BACKUPS, "--title", f"Backup {tag[7:]}",
               "--notes", "Private. A nightly backup: see docs/RESTORE.md in the grouplab repository.")
        gh("release", "upload", tag, *[str(f) for f in files], "-R", BACKUPS, "--clobber")
        steps.append("uploaded")
        KEEP_COPY.mkdir(parents=True, exist_ok=True)
        for old in KEEP_COPY.iterdir():
            old.unlink()
        for f in files:
            shutil.copy2(f, KEEP_COPY / f.name)

    listed = json.loads(gh("release", "list", "-R", BACKUPS, "--limit", "1000", "--json", "tagName").stdout or "[]")
    tags = [r["tagName"] for r in listed]
    kept = keep(tags, date.today())
    gone = [t for t in tags if t.startswith("backup-") and t not in kept]
    for t in gone:
        gh("release", "delete", t, "-R", BACKUPS, "--yes", "--cleanup-tag")
    steps.append(f"pruned {len(gone)}")
    log(f"uploaded {tag}; {len(kept)} kept, {len(gone)} older deleted")
    return {"tag": tag, "bytes": total, "uploaded": True, "deleted": gone}


# --------------------------------------------------------------------------------------------------------- the restore test --

def restore_test(steps: list[str]) -> dict:
    """Entry 222 section 3.4: the newest backup downloaded, every file checked against its manifest, and the bundle cloned and checked."""
    with tempfile.TemporaryDirectory(prefix="gl-restore-") as tmp:
        work = Path(tmp)
        if repository_exists(BACKUPS):
            listed = json.loads(gh("release", "list", "-R", BACKUPS, "--limit", "1", "--json", "tagName").stdout or "[]")
            if not listed:
                raise RuntimeError("the backups repository has no backup in it")
            tag = listed[0]["tagName"]
            gh("release", "download", tag, "-R", BACKUPS, "-D", str(work))
        elif KEEP_COPY.is_dir() and any(KEEP_COPY.iterdir()):
            tag = "the copy on this computer"
            for f in KEEP_COPY.iterdir():
                shutil.copy2(f, work / f.name)
        else:
            raise RuntimeError("there is no backup to test")
        manifest = json.loads((work / "manifest.json").read_text(encoding="utf-8"))
        for asset in manifest["assets"]:
            if sha256(work / asset["name"]) != asset["sha256"]:
                raise RuntimeError(f"{asset['name']} in {tag} does not match its manifest")
        steps.append("assets match")
        with zipfile.ZipFile(work / "local.zip") as z:
            listed_files = {e["path"]: e["sha256"] for e in manifest["local"]}
            for name, digest in listed_files.items():
                if hashlib.sha256(z.read(name)).hexdigest() != digest:
                    raise RuntimeError(f"{name} in local.zip does not match its manifest")
        steps.append(f"local files match: {len(listed_files)}")
        clone = work / "clone"
        run = subprocess.run(["git", "clone", "-q", str(work / "grouplab.bundle"), str(clone)], capture_output=True, text=True)
        if run.returncode != 0:
            raise RuntimeError("the repository bundle does not clone: " + run.stderr.strip()[:300])
        head = subprocess.run(["git", "-C", str(clone), "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
        if gh("api", f"repos/{OWNER}/grouplab/commits/{head}", "--jq", ".sha", check=False).returncode != 0:
            raise RuntimeError(f"the bundle's head {head[:7]} is not on GitHub")
        steps.append("bundle clones")
        log(f"restore test passed on {tag}: {len(manifest['assets'])} files and {len(listed_files)} local files match, the bundle clones")
        return {"tag": tag, "files": len(listed_files), "head": head[:7]}


def main() -> int:
    parser = argparse.ArgumentParser(description="The nightly backup and the weekly restore test (entry 222).")
    parser.add_argument("--dry-run", action="store_true", help="build everything, upload and delete nothing")
    parser.add_argument("--restore-test", action="store_true", help="download the newest backup, check it and restore the bundle")
    args = parser.parse_args()
    steps: list[str] = []
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        if args.restore_test:
            result = restore_test(steps)
            status(restoreTest={"at": now, "ok": True, **result})
            return 0
        synced = sync_archive(args.dry_run, steps)
        done = backup(args.dry_run, steps)
        if not args.dry_run:
            status(archiveCopy={"at": now, "ok": not synced["problems"], "copied": len(synced["copied"]), "problems": synced["problems"]},
                   backup={"at": now, "ok": True, **done})
        if synced["problems"]:
            report_failure("ArchiveCopyFailed", "; ".join(synced["problems"]), steps)
        return 0
    except Exception as e:  # noqa: BLE001 - every failure is reported, whatever it is
        what = "RestoreTestFailed" if args.restore_test else "BackupFailed"
        log(f"{what}: {e}")
        if not args.dry_run:
            status(**{("restoreTest" if args.restore_test else "backup"): {"at": now, "ok": False, "error": str(e)[:500]}})
            report_failure(what, str(e), steps)
        return 1


if __name__ == "__main__":
    sys.exit(main())
