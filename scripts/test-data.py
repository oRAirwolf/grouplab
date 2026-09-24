#!/usr/bin/env python3
"""Large test files live on a GitHub release, never in the repository.

NOTES-FROM-PLANNING.md entry 171 section 6, closing request 8: a sample over about 10 MB is attached to the
dedicated `test-data` release as a download, so nothing is added to any clone, ever. CI fetches each file by
URL and verifies its SHA-256 before a test uses it. The release is created once by the CI workflow that uses
it, not by the nightly, so nothing that rewrites or prunes nightly releases can sweep it up.

`tests/test-data.json` lists the files: name, SHA-256, size, and the consent record that lets each be
published. A test finds a file through the folder named by GROUPLAB_TEST_DATA, then the folder its own
fixture names on the machine it was measured on, and skips with the reason where neither has it.

    python3 scripts/test-data.py fetch --to DIR     download every listed file into DIR and verify it
    python3 scripts/test-data.py rebuild SRC DEST   rebuild an image from its pixels alone, before upload

`rebuild` keeps the pixels and the resolution and nothing else from the original: no text chunk, no
profile, no timestamp, no location. It never prints the original's metadata, because it never asks for it.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
MANIFEST = REPO / "tests" / "test-data.json"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def release_assets(repository: str, release: str, token: str) -> dict[str, str] | None:
    """
    Entry 185 section 2: the release's assets through the API, which also finds it as a draft. A draft is off the public releases page,
    and its files are not at the public download address, so a draft is read with a token that can see it: the workflow's, in the one job
    that holds contents: write, or the person's own. Returns each asset's API address by name, or None where the release cannot be seen.
    """
    request = urllib.request.Request(f"https://api.github.com/repos/{repository}/releases?per_page=100",
                                     headers={"Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json", "User-Agent": "grouplab-test-data"})
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            releases = json.load(response)
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
        return None
    for r in releases:
        if r.get("tag_name") == release or r.get("name") == release:
            return {a["name"]: a["url"] for a in r.get("assets", [])}
    return None


def download(address: str, target: Path, token: str | None) -> None:
    headers = {"User-Agent": "grouplab-test-data"}
    if token:
        headers |= {"Authorization": f"Bearer {token}", "Accept": "application/octet-stream"}
    with urllib.request.urlopen(urllib.request.Request(address, headers=headers), timeout=300) as response, target.open("wb") as out:
        while block := response.read(1 << 20):
            out.write(block)


def fetch(to: Path) -> int:
    doc = json.loads(MANIFEST.read_text(encoding="utf-8"))
    to.mkdir(parents=True, exist_ok=True)
    base = f"https://github.com/{doc['repository']}/releases/download/{doc['release']}/"
    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    assets = release_assets(doc["repository"], doc["release"], token) if token else None
    print("reading the release through the API, which sees it as a draft too" if assets is not None else "reading the release at its public address")
    for item in doc["files"]:
        target = to / item["name"]
        if target.exists() and sha256(target) == item["sha256"]:
            print(f"{item['name']}: already here, hash verified")
            continue
        try:
            if assets is not None:
                if item["name"] not in assets:
                    raise urllib.error.HTTPError(base + item["name"], 404, "not attached", None, None)
                download(assets[item["name"]], target, token)
            else:
                download(base + item["name"], target, None)
        except urllib.error.HTTPError as e:
            # Not uploaded yet, or the release not yet created: the tests that need it skip and say so.
            target.unlink(missing_ok=True)
            print(f"{item['name']}: not on the {doc['release']} release ({e.code}), so the tests that need it skip")
            continue
        got = sha256(target)
        if got != item["sha256"]:
            target.unlink()
            print(f"{item['name']}: downloaded, but its SHA-256 is {got}, not {item['sha256']}; deleted", file=sys.stderr)
            return 1
        print(f"{item['name']}: downloaded, {target.stat().st_size} bytes, hash verified")
    return 0


def rebuild(src: Path, dest: Path) -> int:
    from PIL import Image

    with Image.open(src) as original:
        dpi = original.info.get("dpi")
        pixels = Image.frombytes(original.mode, original.size, original.tobytes())
    options = {"dpi": tuple(round(d) for d in dpi)} if dpi else {}
    pixels.save(dest, format="PNG", compress_level=9, **options)
    print(f"{dest.name}: {dest.stat().st_size} bytes, SHA-256 {sha256(dest)}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="command", required=True)
    f = sub.add_parser("fetch")
    f.add_argument("--to", type=Path, required=True)
    r = sub.add_parser("rebuild")
    r.add_argument("src", type=Path)
    r.add_argument("dest", type=Path)
    args = parser.parse_args()
    return fetch(args.to) if args.command == "fetch" else rebuild(args.src, args.dest)


if __name__ == "__main__":
    sys.exit(main())
