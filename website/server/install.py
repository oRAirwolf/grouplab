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

# --- the intake side, NOTES-FROM-PLANNING.md entry 129 section 7.1 -------------------------------
#
# Installed by --intake, separately from the site sync, because the site sync has been running since
# 2026-09-22 and a change to it is a change to something that works. Everything here is additive: no
# file the site sync owns is touched, and nothing belonging to any other domain is touched at all.

SITE = Path("/home/airwolf/web/grouplab.org")

INTAKE_SCRIPTS = [
    ("grouplab-intake-worker.py", Path("/usr/local/sbin/grouplab-intake-worker.py"), 0o755),
    ("grouplab-set-turnstile-secret", Path("/usr/local/sbin/grouplab-set-turnstile-secret"), 0o750),
]

INTAKE_UNITS = [
    ("grouplab-intake-worker.service", Path("/etc/systemd/system/grouplab-intake-worker.service"), 0o644),
    ("grouplab-intake-worker.timer", Path("/etc/systemd/system/grouplab-intake-worker.timer"), 0o644),
]

# PHP's per-directory settings, and the nginx include. The .user.ini sits inside public_html, which the
# site sync rsyncs with --delete, so the sync excludes it by name; without that exclusion the first sync
# after this installer would delete it and every real photograph would fail to upload with nothing
# saying why.
INTAKE_CONFIG = [
    ("user.ini", SITE / "public_html" / ".user.ini", 0o644, "airwolf"),
    ("nginx.ssl.conf_grouplab", Path("/home/airwolf/conf/web/grouplab.org/nginx.ssl.conf_grouplab"), 0o644, "airwolf"),
]

# Everything an upload passes through, outside public_html and never served. 0750 and owned by the
# site's own user, which is the user PHP-FPM runs as and the user the worker runs as.
INTAKE_FOLDERS = [
    (SITE / "private", 0o750, "airwolf"),
    (SITE / "private" / "quarantine", 0o750, "airwolf"),
    (SITE / "private" / "ready", 0o750, "airwolf"),
    (SITE / "private" / "refused", 0o750, "airwolf"),
]


def say(message: str) -> None:
    print(message, flush=True)


def missing_tools() -> list[str]:
    return [t for t in NEEDED if shutil.which(t) is None]


# NOTES-FROM-PLANNING.md entry 176 section 9.1: everything the intake worker needs, each with the Ubuntu package that provides it. The
# worker's whole job is rebuilding images with Pillow, and the first install finished without it: every submission then failed to
# decode. So the intake install checks each of these and refuses to finish, naming the package, where one is missing.
WORKER_NEEDS = [
    ("Pillow, which rebuilds every image from its pixels", [sys.executable, "-c", "import PIL.Image"], "python3-pil"),
    ("heif-convert, which decodes the HEIC photographs phones send", ["heif-convert", "--version"], "libheif-examples"),
    ("clamdscan, the virus scanner's client", ["clamdscan", "--version"], "clamdscan"),
    ("clamd, the virus scanner's daemon, answering on its socket", ["clamdscan", "--ping=3"], "clamav-daemon"),
]


def worker_missing() -> list[str]:
    """What the intake worker would fail without, as the package to install for each."""
    missing = []
    for what, command, package in WORKER_NEEDS:
        try:
            ok = subprocess.run(command, capture_output=True, timeout=60).returncode == 0
        except (OSError, subprocess.TimeoutExpired):
            ok = False
        if not ok:
            missing.append(f"{what}: sudo apt-get install -y {package}")
    return missing


# Every file this run wrote, or in a dry run would write, so the closing lines can say only what is still to do.
# NOTES-FROM-PLANNING.md entry 171 section 6: the reminder used to say "set the secret and reload nginx" every time.
CHANGED: set[Path] = set()

# Where grouplab-set-turnstile-secret keeps the secret. Only its presence is checked here; it is never opened.
TURNSTILE_SECRET = Path("/home/airwolf/web/grouplab.org/private/turnstile-secret.txt")


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

    CHANGED.add(target)
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


def put_owned(name: str, target: Path, mode: int, owner: str, dry_run: bool) -> bool:
    """As :func:`put`, and then owned by somebody other than root. Used for the files the site's own user reads."""
    if not put(name, target, mode, dry_run):
        return False
    if dry_run:
        say(f"  would give it to {owner}")
        return True
    shutil.chown(target, owner, owner)
    say(f"  gave it to {owner}")
    return True


def make_folders(items: list, dry_run: bool) -> None:
    for path, mode, owner in items:
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


def intake(dry_run: bool) -> int:
    """The target upload intake, NOTES-FROM-PLANNING.md entry 129 section 7.1.

    Separate from the site sync above, and additive: it installs the quarantine folders, the worker and its units,
    the script Alan types the Turnstile secret into, PHP's per-directory settings and the nginx include. It touches
    no file the site sync owns, and nothing belonging to any other domain.

    **It never runs nginx -t and never reloads nginx.** Entry 129 section 7.3 asks for the configuration to be
    tested before a graceful reload and for pissinhot.com to be checked afterwards, and those are Alan's commands to
    run and read, not this script's to run on his behalf. It prints them at the end.
    """
    if not SITE.is_dir():
        say(f"{SITE} is not there, so grouplab.org is not set up on this machine. Nothing was changed.")
        return 2

    gone = worker_missing()
    if gone:
        say("The worker would fail on every submission without these, so nothing was changed:")
        for line in gone:
            say("  " + line)
        return 2

    say("the folders an upload passes through, outside public_html and never served")
    make_folders(INTAKE_FOLDERS, dry_run)

    say("the worker, and the script the Turnstile secret is typed into")
    for item in INTAKE_SCRIPTS:
        if not put(*item, dry_run):
            return 2

    say("the worker's systemd units")
    for unit in INTAKE_UNITS:
        if not put(*unit, dry_run):
            return 2

    say("PHP's settings for this site, and the nginx include")
    for name, target, mode, owner in INTAKE_CONFIG:
        if not put_owned(name, target, mode, owner, dry_run):
            return 2

    say("systemd")
    if run(["systemctl", "daemon-reload"], dry_run) != 0:
        return 1
    if run(["systemctl", "enable", "--now", "grouplab-intake-worker.timer"], dry_run) != 0:
        return 1

    if not dry_run:
        run(["systemctl", "list-timers", "grouplab-intake-worker.timer", "--no-pager"], False)

    say("")
    left = []
    # The secret is looked at only to see that it is there: its size, never its contents.
    if TURNSTILE_SECRET.is_file() and TURNSTILE_SECRET.stat().st_size > 0:
        say("The Turnstile secret is already set. It was not read or printed.")
    else:
        left.append(["The Turnstile secret, which nobody but you ever sees:",
                     "     sudo /usr/local/sbin/grouplab-set-turnstile-secret"])

    include = next(target for name, target, _, _ in INTAKE_CONFIG if name == "nginx.ssl.conf_grouplab")
    if include in CHANGED:
        left.append(["nginx, tested before it is reloaded, and pissinhot.com checked afterwards:",
                     "     sudo nginx -t",
                     "     sudo systemctl reload nginx",
                     "     curl -sS -o /dev/null -w '%{http_code}\\n' https://pissinhot.com/",
                     "     curl -sS -o /dev/null -w '%{http_code}\\n' https://grouplab.org/",
                     "",
                     "If nginx -t complains about a duplicate client_max_body_size, another include for this site already sets",
                     "it. Raise that one instead and delete the line from nginx.ssl.conf_grouplab; do not reload until -t passes."])
    else:
        say("The nginx include was already current, so nginx has nothing new to read and needs no reload.")

    if left:
        say("")
        say(("One thing is" if len(left) == 1 else f"{len(left)} things are") + " left, and not this script's to do.")
        for i, lines in enumerate(left, 1):
            say("")
            say(f"{i}. {lines[0]}")
            for line in lines[1:]:
                say(line)
    say("")
    say("done" if not dry_run else "dry run finished, nothing was changed")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Install the grouplab.org site sync.")
    parser.add_argument("--dry-run", action="store_true", help="say what would happen and change nothing")
    parser.add_argument("--intake", action="store_true",
                        help="install the target upload intake instead of the site sync (entry 129)")
    args = parser.parse_args()

    gone = missing_tools()
    if gone:
        say("This needs " + ", ".join(gone) + ", which " + ("is" if len(gone) == 1 else "are") + " not installed. Nothing was changed.")
        return 2

    if not args.dry_run and os.geteuid() != 0:
        say("This has to run as root: sudo python3 install.py")
        return 2

    if args.intake:
        return intake(args.dry_run)

    say("folders")
    make_folders(FOLDERS, args.dry_run)

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
