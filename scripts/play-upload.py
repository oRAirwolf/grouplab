"""Sends one nightly's Android App Bundle, and its native debug symbols, to Google Play's internal and closed testing tracks.

NOTES-FROM-PLANNING.md entry 384 section 3: until then a build reached Play only when Alan uploaded it by hand, so testers who
installed GroupLab from Play stayed on whatever was last uploaded. This uses the Play Developer API as a service account with
release rights on this app only (request 80). The key comes from the environment variable PLAY_SERVICE_ACCOUNT_JSON and is never
written to disk or printed.

Entry 487 section 1: each nightly goes to `internal` and to `alpha` (Play Console's "Closed testing - Alpha"), with the build's own
"What you will notice" lines as its notes. Two guards keep it from undoing a review:

- The edit is committed with `changesInReviewBehavior=ERROR_IF_IN_REVIEW`. Play's default cancels whatever is in review and sends the
  new changes instead, which would have pulled the first closed release out of Google's review. While anything is in review, nothing
  is sent that run, internal included, and the summary says so; the next nightly tries again.
- `alpha` is sent only when the track's newest release is `completed` and none is a draft or in progress; otherwise that run skips
  `alpha` and says so.

    python scripts/play-upload.py --aab <file.aab> [--symbols <file.zip>] --version 0.2.0-nightly.176 [--tracks internal,alpha] [--dry-run]
    python scripts/play-upload.py --read-only
    python scripts/play-upload.py --production [--dry-run]

Entry 513: --production puts the closed track's newest completed release (one Google has already reviewed for testers) on the
production track at full rollout, when it is newer than production's and nothing there is unfinished, with the same review guard.
Until Google gives the account production access (12 testers for 14 days, then Alan's application in Play Console) it refuses; the
run then prints one dated "Production: blocked" line with Google's words, for docs/notes/external-status.md, and fails nothing. The
first night Google allows it, it goes through by itself.

--dry-run checks the files and prints the notes and tracks without contacting Google. --read-only signs in, opens an edit, prints
each track's releases and the en-US store listing, and deletes the edit without changing anything.
"""

import argparse
import json
import os
import re
import sys
import zipfile
from pathlib import Path

PACKAGE = "org.grouplab.app"
API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + PACKAGE
UPLOAD = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications/" + PACKAGE
RELEASE_NOTES = Path(__file__).resolve().parents[1] / "docs" / "RELEASE-NOTES.md"
LIMIT = 500
MORE = "All notes: https://grouplab.org/releases/"
TRACKS = ("internal", "alpha")


def release_name(version: str) -> str:
    """The name Play Console shows for the release: the nightly's own version, so a tester can match it with the download page."""
    return version


def noticed(version: str, text: str) -> list[str]:
    """The "What you will notice" lines of one build's section in RELEASE-NOTES.md, without their bullets."""
    section = re.search(rf"^## {re.escape(version)}\n(.*?)(?=^## |\Z)", text, re.M | re.S)
    if not section:
        return []
    part = re.search(r"\*\*What you will notice\*\*\n(.*?)(?=\n\*\*|\n\[|\Z)", section.group(1), re.S)
    if not part:
        return []
    return [line[2:].strip() for line in part.group(1).splitlines() if line.startswith("- ")]


def notes(version: str, text: str | None = None) -> str:
    """Play's release note, at most 500 characters: the build's noticed lines, whole lines only, then where the rest are."""
    if text is None:
        text = RELEASE_NOTES.read_text(encoding="utf-8") if RELEASE_NOTES.is_file() else ""
    # A correction to an earlier build's notes matters least to somebody installing this one, so it goes last.
    lines = sorted(noticed(version, text), key=lambda line: line.startswith("Correction"))
    if not lines:
        return f"GroupLab {version}. What changed: https://grouplab.org/releases/"[:LIMIT]
    kept: list[str] = []
    for line in lines:
        candidate = "\n".join(f"- {x}" for x in kept + [line]) + "\n" + MORE
        if len(candidate) > LIMIT:
            continue
        kept.append(line)
    if not kept:
        return (lines[0][: LIMIT - len(MORE) - 4].rstrip() + "...\n" + MORE)[:LIMIT]
    return "\n".join(f"- {x}" for x in kept) + "\n" + MORE


def closed_track_ready(track: dict) -> tuple[bool, str]:
    """Whether a new release may go to this track: its newest release completed, and nothing in draft or in progress."""
    releases = track.get("releases") or []
    if not releases:
        return False, "the track has no release yet"
    unfinished = [r for r in releases if r.get("status") in ("draft", "inProgress", "halted")]
    if unfinished:
        return False, "a release on it is " + ", ".join(sorted({r["status"] for r in unfinished}))
    newest = max(releases, key=lambda r: max((int(c) for c in r.get("versionCodes") or ["0"]), default=0))
    if newest.get("status") != "completed":
        return False, f"its newest release is {newest.get('status')}"
    return True, "its newest release is completed"


def production_plan(production: dict, closed: dict) -> tuple[str | None, str, dict | None]:
    """Entry 513: which version code goes to production, if any, and why; with the closed release it comes from."""
    unfinished = [r for r in production.get("releases") or [] if r.get("status") in ("draft", "inProgress", "halted")]
    if unfinished:
        return None, "a production release is " + ", ".join(sorted({r["status"] for r in unfinished})), None
    done = [r for r in closed.get("releases") or [] if r.get("status") == "completed" and r.get("versionCodes")]
    if not done:
        return None, "the closed track has no completed release", None
    newest = max(done, key=lambda r: max(int(c) for c in r["versionCodes"]))
    code = str(max(int(c) for c in newest["versionCodes"]))
    live = max((int(c) for r in production.get("releases") or [] for c in r.get("versionCodes") or []), default=0)
    if int(code) <= live:
        return None, f"production already has version code {live}", None
    return code, f"version code {code} ({newest.get('name', '')}) from the closed track", newest


def production(dry_run: bool) -> int:
    """Entry 513: the closed track's newest reviewed release to production. Never fails the run: a refusal is a dated line."""
    from datetime import date
    day = date.today().isoformat()
    requests, headers, ok = session()
    edit = ok(requests.post(f"{API}/edits", headers=headers, timeout=60), "a new edit")["id"]
    try:
        live = ok(requests.get(f"{API}/edits/{edit}/tracks/production", headers=headers, timeout=60), "the production track")
        closed = ok(requests.get(f"{API}/edits/{edit}/tracks/alpha", headers=headers, timeout=60), "the alpha track")
    except SystemExit as said:
        requests.delete(f"{API}/edits/{edit}", headers=headers, timeout=60)
        print(f"Production, {day}: blocked: {str(said)[:300]}")
        return 0
    code, why, source = production_plan(live, closed)
    if code is None or dry_run:
        requests.delete(f"{API}/edits/{edit}", headers=headers, timeout=60)
        print(f"Production: {'would send ' + why if code and dry_run else 'nothing sent, ' + why}.")
        return 0
    release = {"name": source.get("name") or code, "versionCodes": [code], "status": "completed"}
    if source.get("releaseNotes"):
        release["releaseNotes"] = source["releaseNotes"]
    response = requests.put(f"{API}/edits/{edit}/tracks/production", headers=headers, json={"track": "production", "releases": [release]}, timeout=60)
    if response.status_code < 300:
        response = requests.post(f"{API}/edits/{edit}:commit?changesInReviewBehavior=ERROR_IF_IN_REVIEW", headers=headers, timeout=120)
    if response.status_code < 300:
        print(f"Production: {why} sent at full rollout; Google reviews it before it is public.")
        return 0
    requests.delete(f"{API}/edits/{edit}", headers=headers, timeout=60)
    if in_review(response):
        print("Production: Google is still reviewing earlier changes, so nothing was sent; the next nightly tries again.")
    else:
        print(f"Production, {day}: blocked: Google refused it (HTTP {response.status_code}: {response.text[:300]}). It goes by itself the "
              "first night Google allows it.")
    return 0


def in_review(response) -> bool:
    return response.status_code == 400 and "CHANGES_ALREADY_IN_REVIEW" in response.text


def check(aab: str, symbols: str | None) -> list[str]:
    """What is wrong with the files before anything is sent, or nothing."""
    problems = []
    if not os.path.isfile(aab):
        problems.append(f"no bundle at {aab}")
    elif not zipfile.is_zipfile(aab) or "base/manifest/AndroidManifest.xml" not in zipfile.ZipFile(aab).namelist():
        problems.append(f"{aab} is not an Android App Bundle")
    if symbols is not None and not (os.path.isfile(symbols) and zipfile.is_zipfile(symbols)):
        problems.append(f"no symbols zip at {symbols}")
    return problems


def session():
    # Imported here so --dry-run and the tests need neither package.
    import requests
    from google.auth.transport.requests import Request
    from google.oauth2 import service_account

    info = json.loads(os.environ["PLAY_SERVICE_ACCOUNT_JSON"])
    credentials = service_account.Credentials.from_service_account_info(info, scopes=["https://www.googleapis.com/auth/androidpublisher"])
    credentials.refresh(Request())
    headers = {"Authorization": f"Bearer {credentials.token}"}

    def ok(response, what):
        if response.status_code >= 300:
            raise SystemExit(f"Play refused {what}: HTTP {response.status_code} {response.text[:400]}")
        return response.json() if response.content else {}

    return requests, headers, ok


def read_only() -> int:
    requests, headers, ok = session()
    edit = ok(requests.post(f"{API}/edits", headers=headers, timeout=60), "a new edit")["id"]
    try:
        for track in TRACKS:
            data = ok(requests.get(f"{API}/edits/{edit}/tracks/{track}", headers=headers, timeout=60), f"the {track} track")
            for r in data.get("releases") or []:
                print(f"{track}: {r.get('name')} codes {','.join(r.get('versionCodes') or [])} {r.get('status')}")
            ready, why = closed_track_ready(data)
            print(f"{track}: {'would send' if ready else 'would skip'}, {why}")
        listing = ok(requests.get(f"{API}/edits/{edit}/listings/en-US", headers=headers, timeout=60), "the en-US listing")
        print(json.dumps({k: listing.get(k) for k in ("title", "shortDescription", "fullDescription")}, ensure_ascii=False, indent=1))
    finally:
        requests.delete(f"{API}/edits/{edit}", headers=headers, timeout=60)
        print("the edit was deleted; nothing changed")
    return 0


def send(aab: str, symbols: str | None, version: str, tracks: list[str]) -> int:
    requests, headers, ok = session()
    edit = ok(requests.post(f"{API}/edits", headers=headers, timeout=60), "a new edit")["id"]
    sending = []
    for track in tracks:
        if track == "internal":
            sending.append(track)
            continue
        ready, why = closed_track_ready(ok(requests.get(f"{API}/edits/{edit}/tracks/{track}", headers=headers, timeout=60), f"the {track} track"))
        if ready:
            sending.append(track)
        else:
            print(f"The {track} track is skipped this run: {why}.")
    if not sending:
        requests.delete(f"{API}/edits/{edit}", headers=headers, timeout=60)
        print("Nothing was sent.")
        return 0
    with open(aab, "rb") as bundle:
        code = ok(requests.post(f"{UPLOAD}/edits/{edit}/bundles?uploadType=media", headers={**headers, "Content-Type": "application/octet-stream"},
                                data=bundle, timeout=900), "the bundle")["versionCode"]
    print(f"bundle uploaded as version code {code}")
    if symbols:
        with open(symbols, "rb") as zipped:
            ok(requests.post(f"{UPLOAD}/edits/{edit}/apks/{code}/deobfuscationFiles/nativeCode?uploadType=media",
                             headers={**headers, "Content-Type": "application/octet-stream"}, data=zipped, timeout=900), "the native symbols")
        print("native debug symbols uploaded")
    text = notes(version)
    for track in sending:
        body = {"track": track, "releases": [{"name": release_name(version), "versionCodes": [str(code)], "status": "completed",
                                              "releaseNotes": [{"language": "en-US", "text": text}]}]}
        ok(requests.put(f"{API}/edits/{edit}/tracks/{track}", headers=headers, json=body, timeout=60), f"the {track} track")
    response = requests.post(f"{API}/edits/{edit}:commit?changesInReviewBehavior=ERROR_IF_IN_REVIEW", headers=headers, timeout=120)
    if in_review(response):
        requests.delete(f"{API}/edits/{edit}", headers=headers, timeout=60)
        print(f"Google is still reviewing earlier changes, so {version} was not sent this run: sending now would cancel that review. "
              "The next nightly tries again.")
        return 0
    ok(response, "the commit")
    print(f"{version} (version code {code}) is on the {' and '.join(sending)} track{'s' if len(sending) > 1 else ''}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--aab")
    parser.add_argument("--symbols")
    parser.add_argument("--version")
    parser.add_argument("--tracks", default="internal,alpha")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--read-only", action="store_true")
    parser.add_argument("--production", action="store_true")
    args = parser.parse_args()
    if args.production:
        if not os.environ.get("PLAY_SERVICE_ACCOUNT_JSON"):
            print("Production: no PLAY_SERVICE_ACCOUNT_JSON (request 80), so nothing was sent.")
            return 0
        return production(args.dry_run)
    if args.read_only:
        if not os.environ.get("PLAY_SERVICE_ACCOUNT_JSON"):
            print("no PLAY_SERVICE_ACCOUNT_JSON (request 80), so nothing can be read", file=sys.stderr)
            return 1
        return read_only()
    if not args.aab or not args.version:
        parser.error("--aab and --version are needed unless --read-only")
    tracks = [t for t in args.tracks.split(",") if t]
    if unknown := [t for t in tracks if t not in TRACKS]:
        parser.error(f"unknown track {', '.join(unknown)}")
    if problems := check(args.aab, args.symbols):
        print("; ".join(problems), file=sys.stderr)
        return 1
    if args.dry_run or not os.environ.get("PLAY_SERVICE_ACCOUNT_JSON"):
        why = "a dry run" if args.dry_run else "no PLAY_SERVICE_ACCOUNT_JSON (request 80)"
        print(f"{why}: would send {os.path.basename(args.aab)}{' and its symbols' if args.symbols else ''} to the "
              f"{' and '.join(tracks)} track{'s' if len(tracks) > 1 else ''} as {args.version}, alpha only if its newest release is "
              "completed, and nothing if Google is reviewing earlier changes. Notes:")
        print(notes(args.version))
        return 0
    return send(args.aab, args.symbols, args.version, tracks)


if __name__ == "__main__":
    sys.exit(main())
