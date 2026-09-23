#!/usr/bin/env python3
"""Pull the published grouplab.org site and install it, NOTES-FROM-PLANNING.md entry 128 section 4.

The server pulls. GitHub holds no key, no address and no password, and this machine needs no
new inbound access. It runs as root from a systemd timer, every 5 minutes and 2 minutes after
boot.

What it will not do, ever: change nginx, reload nginx, touch anything belonging to another
domain, or write an address into any file.

Nothing is installed until the archive's SHA-256 matches and its signature verifies against the
public key at /etc/grouplab-site-sync/update-signing.pub, which is the same public half the
application compiles in. The site is backed up before it is replaced, checked after, and
restored if the check fails.
"""

from __future__ import annotations

import argparse
import hashlib
import os
import shutil
import subprocess
import sys
import tarfile
import tempfile
import time
from pathlib import Path

RELEASE = "https://github.com/oRAirwolf/grouplab/releases/download/site"
ARCHIVE = "grouplab-site.tar.gz"

SITE_ROOT = Path("/home/airwolf/web/grouplab.org/public_html")
ERROR_PAGES = Path("/home/airwolf/web/grouplab.org/document_errors")
BACKUPS = Path("/home/airwolf/backups/grouplab.org")
STATE = Path("/var/lib/grouplab-site-sync")
LOG = Path("/home/airwolf/logs/grouplab-site-sync.log")
PUBLIC_KEY = Path("/etc/grouplab-site-sync/update-signing.pub")

OWNER = "airwolf:airwolf"

# A built site is about 50 files and a few megabytes. These bounds are wide enough that an
# ordinary change never trips them and narrow enough that something absurd does.
MAX_BYTES = 200 * 1024 * 1024
MIN_FILES = 20
MAX_FILES = 2000
KEEP_BACKUPS = 10

# A site missing any of these is not a site, and installing it would take grouplab.org down.
REQUIRED = [
    "index.html",
    "404.html",
    "download/index.html",
    "support/index.html",
    "assets/css/site.css",
]

# Checked through the server itself after installing, before the backup is let go.
CHECK_PATHS = ["/", "/download/", "/support/"]


def log(message: str) -> None:
    line = time.strftime("%Y-%m-%dT%H:%M:%S%z") + " " + message
    print(line)
    try:
        # The log folder is the installer's to create. Making it here meant a dry run on a machine that had never had the installer
        # run left a folder behind, which is the one thing a dry run must not do. The line is still printed either way.
        if not LOG.parent.is_dir():
            return
        # Kept from growing without bound: rolled at 2 MB, one old copy kept.
        if LOG.exists() and LOG.stat().st_size > 2 * 1024 * 1024:
            LOG.replace(LOG.with_suffix(".log.1"))
        with LOG.open("a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass


def run(args: list[str], **kw) -> subprocess.CompletedProcess:
    return subprocess.run(args, capture_output=True, text=True, timeout=300, **kw)


def fetch(url: str, into: Path) -> int:
    """Downloads one file. Returns the HTTP status; 0 means curl itself failed."""
    result = run([
        "curl", "--silent", "--show-error", "--location", "--fail-with-body",
        "--max-time", "120", "--max-filesize", str(MAX_BYTES),
        "--write-out", "%{http_code}", "--output", str(into), url,
    ])
    code = result.stdout.strip()[-3:]
    if not code.isdigit():
        log(f"could not reach {url}: {result.stderr.strip()[:200]}")
        return 0
    return int(code)


def sha256_of(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def safe_members(tar: tarfile.TarFile, into: Path) -> list[tarfile.TarInfo]:
    """
    Every member checked before anything is written. Python's data filter does most of this;
    the explicit checks are here because an archive that unpacks outside its folder is the one
    mistake that cannot be undone, and this runs as root.
    """
    members = []
    root = into.resolve()
    for m in tar.getmembers():
        name = m.name
        if name.startswith("/") or ".." in Path(name).parts or Path(name).is_absolute():
            raise ValueError(f"refusing {name!r}: it would unpack outside its folder")
        if m.issym() or m.islnk():
            raise ValueError(f"refusing {name!r}: it is a link")
        if m.ischr() or m.isblk() or m.isfifo() or m.isdev():
            raise ValueError(f"refusing {name!r}: it is a device or a pipe")
        if not (m.isfile() or m.isdir()):
            raise ValueError(f"refusing {name!r}: it is neither a file nor a directory")
        if not (root / name).resolve().is_relative_to(root):
            raise ValueError(f"refusing {name!r}: it resolves outside its folder")
        members.append(m)
    return members


def check_build(folder: Path, commit: str | None) -> None:
    """A site is only installed when it is whole and says what it is."""
    files = [p for p in folder.rglob("*") if p.is_file()]
    if not (MIN_FILES <= len(files) <= MAX_FILES):
        raise ValueError(f"the build has {len(files)} files, outside {MIN_FILES} to {MAX_FILES}")

    for rel in REQUIRED:
        if not (folder / rel).is_file():
            raise ValueError(f"the build has no {rel}")

    pages = [p for p in files if p.suffix == ".html"]
    for page in pages:
        text = page.read_text(encoding="utf-8", errors="replace")
        if 'name="grouplab-site-build"' not in text:
            raise ValueError(f"{page.relative_to(folder)} does not say what commit it was built from")
        if commit and f'content="{commit}"' not in text:
            raise ValueError(f"{page.relative_to(folder)} was built from a different commit than the rest")


def commit_of(folder: Path) -> str | None:
    index = folder / "index.html"
    if not index.is_file():
        return None
    text = index.read_text(encoding="utf-8", errors="replace")
    marker = 'name="grouplab-site-build" content="'
    at = text.find(marker)
    if at < 0:
        return None
    return text[at + len(marker):].split('"', 1)[0] or None


def back_up() -> Path | None:
    if not SITE_ROOT.is_dir():
        return None
    BACKUPS.mkdir(parents=True, exist_ok=True)
    stamp = time.strftime("%Y%m%d-%H%M%S")
    path = BACKUPS / f"web-{stamp}.tar.gz"
    with tarfile.open(path, "w:gz") as tar:
        tar.add(SITE_ROOT, arcname=".")
    old = sorted(BACKUPS.glob("web-*.tar.gz"))
    for gone in old[:-KEEP_BACKUPS]:
        gone.unlink(missing_ok=True)
    return path


# NOTES-FROM-PLANNING.md entry 129 section 7.1: PHP's per-directory settings for grouplab.org live in
# public_html/.user.ini, because HestiaCP regenerates the FPM pool file on a template rebuild and a
# direct edit of it does not survive. That file is not part of the built site, so rsync --delete would
# remove it on the first sync after the installer put it there, and the only symptom would be every
# real photograph failing to upload with nothing saying why. It is excluded here rather than shipped
# in the site, because it is server configuration and the site archive is public.
KEEP_IN_PLACE = [".user.ini"]


def install(folder: Path) -> None:
    SITE_ROOT.mkdir(parents=True, exist_ok=True)
    result = run([
        "rsync", "-a", "--delete",
        *[f"--exclude={name}" for name in KEEP_IN_PLACE],
        f"--chown={OWNER}", "--chmod=D755,F644",
        str(folder) + "/", str(SITE_ROOT) + "/",
    ])
    if result.returncode != 0:
        raise RuntimeError(f"rsync failed: {result.stderr.strip()[:300]}")

    page = folder / "404.html"
    if page.is_file():
        ERROR_PAGES.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(page, ERROR_PAGES / "404.html")
        shutil.chown(ERROR_PAGES / "404.html", "airwolf", "airwolf")
        (ERROR_PAGES / "404.html").chmod(0o644)


def own_address() -> str | None:
    """The first address the machine answers on, used only to talk to itself. Never written anywhere."""
    result = run(["hostname", "-I"])
    parts = result.stdout.split()
    return parts[0] if parts else None


# How many times the live check asks before it believes the answer, and how long it waits between.
#
# The first publish rolled back on this. The install had worked and the files were in the web root, and the check,
# run the instant the directory was replaced, read the page the web server was still holding open. A single
# immediate read is not evidence that an install failed: a web server can take a moment to notice that the
# directory under it has been swapped, and rolling a good site back because of that is the worse mistake.
#
# It still rolls back on a real failure. Nothing here weakens that: it only stops the check calling a slow answer
# a wrong one.
CHECK_TRIES = 5
CHECK_WAIT_SECONDS = 3


def asked_once(address: str, commit: str | None) -> str | None:
    """One pass of the live check. None where it is serving the new site, or what is wrong where it is not."""
    for path in CHECK_PATHS:
        result = run([
            "curl", "--silent", "--show-error", "--max-time", "30",
            "--resolve", f"grouplab.org:443:{address}",
            "--write-out", "%{http_code}", "--output", "/tmp/grouplab-site-check",
            f"https://grouplab.org{path}",
        ])
        code = result.stdout.strip()[-3:]
        if code != "200":
            return f"{path} returned {code or 'nothing'}"

        if path == "/" and commit:
            body = Path("/tmp/grouplab-site-check").read_text(encoding="utf-8", errors="replace")
            if f'content="{commit}"' not in body:
                return "the home page is not serving the new commit"

    return None


def serving(commit: str | None) -> bool:
    """Asks the server itself, through TLS, whether it is serving the site that was just installed."""
    address = own_address()
    if not address:
        log("could not work out the machine's own address, so the live check was skipped")
        return True

    wrong = None
    for attempt in range(1, CHECK_TRIES + 1):
        wrong = asked_once(address, commit)
        if wrong is None:
            if attempt > 1:
                log(f"the live check passed on attempt {attempt}")
            return True

        if attempt < CHECK_TRIES:
            time.sleep(CHECK_WAIT_SECONDS)

    log(f"the live check failed after {CHECK_TRIES} attempts: {wrong}")
    return False


def restore(backup: Path) -> None:
    with tempfile.TemporaryDirectory(prefix="grouplab-site-restore-") as tmp:
        folder = Path(tmp)
        with tarfile.open(backup) as tar:
            tar.extractall(folder, members=safe_members(tar, folder), filter="data")
        install(folder)


def verify_signature(archive: Path, signature: Path) -> None:
    if not PUBLIC_KEY.is_file():
        raise ValueError(f"there is no public key at {PUBLIC_KEY}, so nothing can be trusted")
    result = run([
        "openssl", "dgst", "-sha256", "-verify", str(PUBLIC_KEY),
        "-signature", str(signature), str(archive),
    ])
    if result.returncode != 0:
        raise ValueError("the signature does not verify against the installed public key")


def sync(dry_run: bool) -> int:
    # The state folder is made when there is state to write, not on the way in, so a dry run leaves the machine as it found it.
    last = STATE / "deployed.sha256"

    with tempfile.TemporaryDirectory(prefix="grouplab-site-sync-") as tmp:
        work = Path(tmp)
        hash_file = work / (ARCHIVE + ".sha256")

        code = fetch(f"{RELEASE}/{ARCHIVE}.sha256", hash_file)
        if code == 404:
            log("nothing to do: the site release does not exist yet")
            return 0
        if code != 200 or not hash_file.is_file():
            log(f"could not read the published hash, HTTP {code}")
            return 1

        wanted = hash_file.read_text(encoding="utf-8").split()[0].strip().lower()
        if len(wanted) != 64 or not all(c in "0123456789abcdef" for c in wanted):
            log("the published hash is not a SHA-256")
            return 1

        if last.is_file() and last.read_text(encoding="utf-8").strip() == wanted:
            log(f"nothing to do: {wanted[:12]} is already deployed")
            return 0

        archive = work / ARCHIVE
        signature = work / (ARCHIVE + ".sig")
        if fetch(f"{RELEASE}/{ARCHIVE}", archive) != 200 or not archive.is_file():
            log("could not download the site archive")
            return 1
        if fetch(f"{RELEASE}/{ARCHIVE}.sig", signature) != 200 or not signature.is_file():
            log("could not download the site signature")
            return 1

        got = sha256_of(archive)
        if got != wanted:
            log(f"the archive does not match its hash: wanted {wanted[:12]}, got {got[:12]}. The live site is untouched.")
            return 1

        try:
            verify_signature(archive, signature)
        except ValueError as e:
            log(f"{e}. The live site is untouched.")
            return 1

        unpacked = work / "site"
        unpacked.mkdir()
        try:
            with tarfile.open(archive) as tar:
                tar.extractall(unpacked, members=safe_members(tar, unpacked), filter="data")
        except (ValueError, tarfile.TarError) as e:
            log(f"the archive was refused: {e}. The live site is untouched.")
            return 1

        commit = commit_of(unpacked)
        try:
            check_build(unpacked, commit)
        except ValueError as e:
            log(f"the build was refused: {e}. The live site is untouched.")
            return 1

        if dry_run:
            files = sum(1 for p in unpacked.rglob("*") if p.is_file())
            log(f"dry run: would install {files} files, commit {(commit or 'unknown')[:12]}, hash {wanted[:12]}. Nothing changed.")
            return 0

        backup = back_up()
        try:
            install(unpacked)
        except (OSError, RuntimeError) as e:
            log(f"install failed: {e}")
            if backup:
                restore(backup)
                log("rolled back to the backup taken before this run")
            return 1

        if not serving(commit):
            if backup:
                restore(backup)
                log("rolled back: the site did not answer correctly after installing")
            return 1

        STATE.mkdir(parents=True, exist_ok=True)
        last.write_text(wanted + "\n", encoding="utf-8")
        files = sum(1 for p in unpacked.rglob("*") if p.is_file())
        log(f"installed {files} files, commit {(commit or 'unknown')[:12]}, hash {wanted[:12]}")
        return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Pull and install the published grouplab.org site.")
    parser.add_argument("--dry-run", action="store_true", help="do everything up to backing up, and change nothing")
    args = parser.parse_args()

    if not args.dry_run and os.geteuid() != 0:
        log("this has to run as root to write the site root")
        return 2

    try:
        return sync(args.dry_run)
    except Exception as e:  # noqa: BLE001 - a timer job reports rather than disappearing
        log(f"unexpected failure: {type(e).__name__}: {e}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
