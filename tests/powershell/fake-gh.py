#!/usr/bin/env python3
"""A stand-in for the gh command line, NOTES-FROM-PLANNING.md entry 220, for tests/powershell/archive-tests.ps1.

It keeps releases as folders under FAKE_GH_STATE and answers the handful of gh calls the archive scripts make. Where the real gh talks
on stderr, so does this, with the real words: "release not found" for a month with no release yet is the line that stopped request 31's
first pull under Windows PowerShell 5.1. Nothing here touches the network.
"""

import json
import os
import shutil
import sys
from pathlib import Path

STATE = Path(os.environ["FAKE_GH_STATE"])


def option(args, name):
    return args[args.index(name) + 1] if name in args else None


def main(args):
    if args[:2] == ["repo", "view"]:
        print(json.dumps({"isPrivate": True}))
        return 0
    if args[:1] != ["release"]:
        print(f"fake gh does not know: {' '.join(args)}", file=sys.stderr)
        return 2
    verb, rest = args[1], args[2:]
    if verb == "list":
        print(json.dumps([{"tagName": p.name} for p in sorted(STATE.iterdir()) if p.is_dir()]))
        return 0
    tag = STATE / rest[0]
    if verb == "view":
        if not tag.is_dir():
            print("release not found", file=sys.stderr)
            return 1
        print(f"title:\t{rest[0]}")
        return 0
    if verb == "create":
        tag.mkdir(parents=True, exist_ok=True)
        print(f"https://github.com/example/releases/tag/{rest[0]}")
        return 0
    if verb == "upload":
        if not tag.is_dir():
            print("release not found", file=sys.stderr)
            return 1
        shutil.copy(rest[1], tag / Path(rest[1]).name)
        print("Successfully uploaded 1 asset", file=sys.stderr)
        return 0
    if verb == "download":
        name, into = option(rest, "-p"), Path(option(rest, "-D"))
        if not tag.is_dir():
            print("release not found", file=sys.stderr)
            return 1
        if not (tag / name).is_file():
            print("no assets match the file pattern", file=sys.stderr)
            return 1
        into.mkdir(parents=True, exist_ok=True)
        shutil.copy(tag / name, into / name)
        return 0
    print(f"fake gh does not know: release {verb}", file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
