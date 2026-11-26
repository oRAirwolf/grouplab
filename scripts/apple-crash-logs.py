"""What App Store Connect's API holds about a build crashing, read only (request 121).

Apple turned down GroupLab for Mac 1.0 (build 240) as crashing on launch and said its crash logs were attached, but none showed on the
message in App Store Connect. This asks the API every place a crash or diagnostic log of a build can be read from: each Mac build's
diagnostic signatures and their logs, TestFlight's crash feedback for the app, and the Mac review submissions with their items. It prints
what each answered, writes any log text it finds into the folder given, and never changes anything or fails the run.

    python3 scripts/apple-crash-logs.py <out folder> [build ...]   with APPLE_API_ISSUER_ID, APPLE_API_KEY_ID, APPLE_API_KEY_P8
"""
from __future__ import annotations

import json
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import importlib

testflight = importlib.import_module("testflight")

BUNDLE = "org.grouplab.app"


def ask(store, path: str) -> dict | None:
    try:
        answer = store.call("GET", path)
        print(f"  {path.split('?')[0]}: {len(answer.get('data', [])) if isinstance(answer.get('data'), list) else 1} found")
        return answer
    except testflight.Refused as refused:
        print(f"  {path.split('?')[0]}: refused, {refused}")
        return None


def main(argv: list[str]) -> int:
    out = Path(argv[1] if len(argv) > 1 else "apple-logs")
    out.mkdir(parents=True, exist_ok=True)
    wanted = {int(b) for b in argv[2:]} or {240, 247}
    store = testflight.Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"],
                             testflight.key_text(os.environ["APPLE_API_KEY_P8"]))
    app = store.app(BUNDLE)
    if app is None:
        print("no app record for the bundle")
        return 0
    saved = 0
    for build in store.builds(app, "MAC_OS"):
        if build.number not in wanted:
            continue
        print(f"Mac build {build.number} ({build.processing}):")
        signatures = ask(store, f"/v1/builds/{build.id}/diagnosticSignatures?limit=50") or {}
        for s in signatures.get("data", []):
            a = s.get("attributes", {})
            print(f"    {a.get('diagnosticType')}: {a.get('signature')} (weight {a.get('weight')})")
            logs = ask(store, f"/v1/diagnosticSignatures/{s['id']}/logs?limit=5")
            if logs:
                (out / f"diagnostic-{build.number}-{s['id']}.json").write_text(json.dumps(logs, indent=1), encoding="utf-8")
                saved += 1
    print("TestFlight crash feedback:")
    crashes = ask(store, f"/v1/apps/{app}/betaFeedbackCrashSubmissions?limit=50") or {}
    for c in crashes.get("data", []):
        a = c.get("attributes", {})
        print(f"    {a.get('createdDate')} {a.get('deviceModel')} {a.get('osVersion')} {a.get('appPlatform') or ''} build {a.get('buildBundleId') or ''}")
        log = ask(store, f"/v1/betaFeedbackCrashSubmissions/{c['id']}/crashLog")
        text = ((log or {}).get("data") or {}).get("attributes", {}).get("logText")
        if text:
            (out / f"testflight-crash-{c['id']}.crash").write_text(text, encoding="utf-8")
            saved += 1
    print("Mac review submissions:")
    submissions = ask(store, f"/v1/apps/{app}/reviewSubmissions?filter[platform]=MAC_OS&limit=10") or {}
    for r in submissions.get("data", []):
        a = r.get("attributes", {})
        print(f"    {r['id']}: {a.get('state')} submitted {a.get('submittedDate')}; relationships: {', '.join(sorted(r.get('relationships', {})))}")
        items = ask(store, f"/v1/reviewSubmissions/{r['id']}/items?limit=20") or {}
        for i in items.get("data", []):
            print(f"      item {i.get('attributes', {}).get('state')}; relationships: {', '.join(sorted(i.get('relationships', {})))}")
    print(f"{saved} log file(s) saved in {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
