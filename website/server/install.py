#!/usr/bin/env python3
"""Install the grouplab.org site sync on the server, NOTES-FROM-PLANNING.md entry 128 section 4.

Run once, as root, from a copy of website/server/ in the ubuntu user's home:

    sudo python3 install.py --dry-run
    sudo python3 install.py

It is idempotent: running it again installs the same files and changes nothing else. Anything it
would overwrite is copied to a timestamped .bak first.

It never touches nginx, never reloads anything, and never goes near pissinhot.com or any other
domain. It writes no address into any file.
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent

SCRIPT = ("grouplab-site-sync.py", Path("/usr/local/sbin/grouplab-site-sync.py"), 0o755)
UNITS = [
    ("grouplab-site-sync.service", Path("/etc/systemd/system/grouplab-site-sync.service"), 0o644),
    ("grouplab-site-sync.timer", Path("/etc/systemd/system/grouplab-site-sync.timer"), 0o644),
]
PUBLIC_KEY = ("update-signing.pub", Path("/etc/grouplab-site-sync/update-signing.pub"), 0o644)

FOLDERS = [
    (Path("/etc/grouplab-site-sync"), 0o755, None),
    (Path("/var/lib/grouplab-site-sync"), 0o750, None),
    (Path("/home/airwolf/backups/grouplab.org"), 0o750, "airwolf"),
    (Path("/home/airwolf/logs"), 0o750, "airwolf"),
]

NEEDED = ["python3", "curl", "rsync", "openssl", "systemctl"]


def say(message: str) -> None:
    print(message, flush=True)


def missing_tools() -> list[str]:
    return [t for t in NEEDED if shutil.which(t) is None]


def put(name: str, target: Path, mode: int, dry_run: bool) -> bool:
    """Copies one file into place, backing up anything different that is already there."""
    source = HERE / name
    if not source.is_file():
        say(f"  missing {source}, which this installer needs")
        return False

    wanted = source.read_bytes()
    if target.is_file() and target.read_bytes() == wanted:
        say(f"  {target} is already what it should be")
        return True

    if dry_run:
        what = "replace" if target.exists() else "create"
        say(f"  would {what} {target} (mode {oct(mode)[2:]})")
        if target.exists():
            say(f"  would back it up beside itself first")
        return True

    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        backup = target.with_name(target.name + "." + time.strftime("%Y%m%d-%H%M%S") + ".bak")
        shutil.copy2(target, backup)
        say(f"  kept the old one as {backup}")

    target.write_bytes(wanted)
    target.chmod(mode)
    say(f"  wrote {target}")
    return True


def folders(dry_run: bool) -> None:
    for path, mode, owner in FOLDERS:
        if path.is_dir():
            say(f"  {path} is there")
            continue
        if dry_run:
            say(f"  would create {path} (mode {oct(mode)[2:]}" + (f", owned by {owner}" if owner else "") + ")")
            continue
        path.mkdir(parents=True, exist_ok=True)
        path.chmod(mode)
        if owner:
            shutil.chown(path, owner, owner)
        say(f"  created {path}")


def run(args: list[str], dry_run: bool) -> int:
    if dry_run:
        say("  would run: " + " ".join(args))
        return 0
    say("  running: " + " ".join(args))
    result = subprocess.run(args, capture_output=True, text=True, timeout=600)
    if result.stdout.strip():
        for line in result.stdout.strip().splitlines():
            say("    " + line)
    if result.returncode != 0:
        for line in result.stderr.strip().splitlines()[:10]:
            say("    " + line)
    return result.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description="Install the grouplab.org site sync.")
    parser.add_argument("--dry-run", action="store_true", help="say what would happen and change nothing")
    args = parser.parse_args()

    gone = missing_tools()
    if gone:
        say("This needs " + ", ".join(gone) + ", which " + ("is" if len(gone) == 1 else "are") + " not installed. Nothing was changed.")
        return 2

    if not args.dry_run and os.geteuid() != 0:
        say("This has to run as root: sudo python3 install.py")
        return 2

    say("folders")
    folders(args.dry_run)

    say("the public key the sync checks signatures with")
    if not put(*PUBLIC_KEY, args.dry_run):
        say("Without the public key nothing can be trusted, so nothing else was installed.")
        return 2

    say("the sync script")
    if not put(*SCRIPT, args.dry_run):
        return 2

    say("the systemd units")
    for unit in UNITS:
        if not put(*unit, args.dry_run):
            return 2

    say("systemd")
    if run(["systemctl", "daemon-reload"], args.dry_run) != 0:
        return 1
    if run(["systemctl", "enable", "--now", "grouplab-site-sync.timer"], args.dry_run) != 0:
        return 1

    # This used to pass False here, so the installer's own dry run really executed the sync. The sync then created its state folder and its
    # log folder, and a dry run that says "nothing was changed" had changed two things. A dry run runs nothing.
    say("a dry run of the sync itself")
    run([str(SCRIPT[1]) if not args.dry_run else str(HERE / SCRIPT[0]), "--dry-run"], args.dry_run)

    if not args.dry_run:
        say("and one real run")
        run([str(SCRIPT[1])], False)
        run(["systemctl", "list-timers", "grouplab-site-sync.timer", "--no-pager"], False)

    say("done" if not args.dry_run else "dry run finished, nothing was changed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
