"""Sends one nightly's Android App Bundle, and its native debug symbols, to Google Play's internal testing track.

NOTES-FROM-PLANNING.md entry 384 section 3: until now a build reached Play only when Alan uploaded it by hand, so testers who
installed GroupLab from Play stayed on whatever was last uploaded. This uses the Play Developer API as a service account with
release rights on this app only (request 80). The key comes from the environment variable PLAY_SERVICE_ACCOUNT_JSON and is never
written to disk or printed. With --dry-run it checks the files and says what it would do without contacting Google.

    python scripts/play-upload.py --aab <file.aab> [--symbols <file.zip>] --version 0.2.0-nightly.176 [--track internal] [--dry-run]
"""

import argparse
import json
import os
import sys
import zipfile

PACKAGE = "org.grouplab.app"
API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + PACKAGE
UPLOAD = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications/" + PACKAGE


def release_name(version: str) -> str:
    """The name Play Console shows for the release: the nightly's own version, so a tester can match it with the download page."""
    return version


def notes(version: str) -> str:
    """Play's release note, at most 500 characters: where the full notes are, rather than a copy that can drift."""
    return f"GroupLab {version}. What changed: https://grouplab.org/releases/"[:500]


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


def send(aab: str, symbols: str | None, version: str, track: str) -> int:
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

    edit = ok(requests.post(f"{API}/edits", headers=headers, timeout=60), "a new edit")["id"]
    with open(aab, "rb") as bundle:
        code = ok(requests.post(f"{UPLOAD}/edits/{edit}/bundles?uploadType=media", headers={**headers, "Content-Type": "application/octet-stream"},
                                data=bundle, timeout=900), "the bundle")["versionCode"]
    print(f"bundle uploaded as version code {code}")
    if symbols:
        with open(symbols, "rb") as zipped:
            ok(requests.post(f"{UPLOAD}/edits/{edit}/apks/{code}/deobfuscationFiles/nativeCode?uploadType=media",
                             headers={**headers, "Content-Type": "application/octet-stream"}, data=zipped, timeout=900), "the native symbols")
        print("native debug symbols uploaded")
    body = {"track": track, "releases": [{"name": release_name(version), "versionCodes": [str(code)], "status": "completed",
                                          "releaseNotes": [{"language": "en-US", "text": notes(version)}]}]}
    ok(requests.put(f"{API}/edits/{edit}/tracks/{track}", headers=headers, json=body, timeout=60), f"the {track} track")
    ok(requests.post(f"{API}/edits/{edit}:commit", headers=headers, timeout=120), "the commit")
    print(f"{version} (version code {code}) is on the {track} track")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--aab", required=True)
    parser.add_argument("--symbols")
    parser.add_argument("--version", required=True)
    parser.add_argument("--track", default="internal")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    if problems := check(args.aab, args.symbols):
        print("; ".join(problems), file=sys.stderr)
        return 1
    if args.dry_run or not os.environ.get("PLAY_SERVICE_ACCOUNT_JSON"):
        why = "a dry run" if args.dry_run else "no PLAY_SERVICE_ACCOUNT_JSON (request 80)"
        print(f"{why}: would send {os.path.basename(args.aab)}{' and its symbols' if args.symbols else ''} to the {args.track} track as {args.version}")
        return 0
    return send(args.aab, args.symbols, args.version, args.track)


if __name__ == "__main__":
    sys.exit(main())
