#!/usr/bin/env python3
"""A dated copy of exactly what a change is about to touch on the server, NOTES-FROM-PLANNING.md entry 397 section 2.

Run as root, before the change, with the files the change will edit or replace and what else it touches:

    sudo python3 grouplab-change-backup.py --label learning-worker --file /etc/systemd/system/x.service ... --packages --units [--dry-run]

It writes /home/ubuntu/grouplab-server/backups/<UTC date and time>-<label>/, never anywhere under HestiaCP's conf/web/ folders:

- files/<the path>: each named file copied with its mode, owner and times (a file that does not exist yet is listed as absent, so the
  undo knows to delete it rather than put something back);
- packages.txt and manual.txt: `dpkg --get-selections` and `apt-mark showmanual`, with --packages, so packages can be put back exactly;
- unit-files.txt and timers.txt: `systemctl list-unit-files` and `systemctl list-timers --all`, with --units;
- crontab-<user>.txt for each --crontab user;
- manifest.json: what was copied, each file's SHA-256, mode and owner, and the folder's total size.

It reads and copies; it changes nothing else. The folder is kept until the change has been in place a week and the next whole-server
backup exists, and is never published: it may hold configuration.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import pwd
import grp
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path("/home/ubuntu/grouplab-server/backups")
FORBIDDEN = "/conf/web/"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def capture(command: list[str], target: Path, dry_run: bool) -> None:
    print(f"  {'would write' if dry_run else 'writing'} {target.name}: {' '.join(command)}")
    if not dry_run:
        done = subprocess.run(command, capture_output=True, text=True)
        target.write_text(done.stdout, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Back up what a server change will touch, before it touches it (entry 397).")
    parser.add_argument("--label", required=True, help="a few words for the change, letters, digits and hyphens")
    parser.add_argument("--file", action="append", default=[], help="a file the change will edit, replace or create")
    parser.add_argument("--packages", action="store_true", help="the change installs or removes packages")
    parser.add_argument("--units", action="store_true", help="the change enables or changes a service or timer")
    parser.add_argument("--crontab", action="append", default=[], help="a user whose crontab the change touches")
    parser.add_argument("--dry-run", action="store_true", help="say what would be copied and copy nothing")
    args = parser.parse_args()

    if not args.label.replace("-", "").isalnum():
        print("The label is letters, digits and hyphens only.")
        return 2
    if not args.dry_run and os.geteuid() != 0:
        print("This has to run as root, to copy files with their owners: sudo python3 grouplab-change-backup.py ...")
        return 2

    stamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H%M%SZ")
    folder = ROOT / f"{stamp}-{args.label}"
    if FORBIDDEN in str(folder):
        print("Never under conf/web/: HestiaCP loads what it finds there as configuration.")
        return 2
    print(f"{'would back up to' if args.dry_run else 'backing up to'} {folder}")
    if not args.dry_run:
        folder.mkdir(parents=True, exist_ok=False)
        os.chmod(folder, 0o700)

    files = []
    for name in args.file:
        path = Path(name)
        if FORBIDDEN in name:
            print(f"  {name} is under conf/web/; it is copied, but the copy stays here, never beside it")
        if not path.exists():
            print(f"  {name}: absent now, so the undo deletes it")
            files.append({"path": name, "absent": True})
            continue
        stat = path.stat()
        entry = {"path": name, "absent": False, "mode": oct(stat.st_mode & 0o7777), "owner": pwd.getpwuid(stat.st_uid).pw_name,
                 "group": grp.getgrgid(stat.st_gid).gr_name, "bytes": stat.st_size, "sha256": sha256(path) if path.is_file() else None}
        files.append(entry)
        print(f"  {name}: {entry['mode']} {entry['owner']}:{entry['group']}, {entry['bytes']} bytes")
        if not args.dry_run:
            copy = folder / "files" / name.lstrip("/")
            copy.parent.mkdir(parents=True, exist_ok=True)
            if path.is_dir():
                shutil.copytree(path, copy, symlinks=True, copy_function=shutil.copy2)
            else:
                shutil.copy2(path, copy)
            os.chown(copy, stat.st_uid, stat.st_gid)

    if args.packages:
        capture(["dpkg", "--get-selections"], folder / "packages.txt", args.dry_run)
        capture(["apt-mark", "showmanual"], folder / "manual.txt", args.dry_run)
    if args.units:
        capture(["systemctl", "list-unit-files", "--no-pager"], folder / "unit-files.txt", args.dry_run)
        capture(["systemctl", "list-timers", "--all", "--no-pager"], folder / "timers.txt", args.dry_run)
    for user in args.crontab:
        capture(["crontab", "-l", "-u", user], folder / f"crontab-{user}.txt", args.dry_run)

    if args.dry_run:
        print("dry run finished, nothing was copied")
        return 0
    size = sum(p.stat().st_size for p in folder.rglob("*") if p.is_file())
    (folder / "manifest.json").write_text(json.dumps({"format": "grouplab-change-backup-1", "label": args.label, "written": stamp,
                                                      "files": files, "packages": args.packages, "units": args.units,
                                                      "crontabs": args.crontab, "bytes": size}, indent=1) + "\n", encoding="utf-8")
    print(f"done: {folder}, {size} bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
