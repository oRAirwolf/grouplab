"""The App Store screenshots, NOTES-FROM-PLANNING.md entry 487 section 4.

Two jobs, both run by .github/workflows/apple-screens.yml:

    python3 scripts/apple-screens.py fit <scenario results folder> <iphone|ipad> <out folder>
    python3 scripts/apple-screens.py made-from <artifact folder> <source ref>
    python3 scripts/apple-screens.py upload [--dry-run]
    python3 scripts/apple-screens.py mac

`mac` (entry 513) fits five of the desktop screens the screenshot job renders, docs/figures/screens/current/<screen>-dark-2560x1440.png,
to the Mac App Store's 2880 by 1800 the same way, into docs/store/apple/mac/; scripts/app-store.py uploads them to a first Mac version's
empty desktop set.

`fit` takes the seven places of the Play set (result, capture, fingerprint, pairing, targets, scale-markers, open-targets) from a
scenario's screenshots and fits each to App Store Connect's exact size, 1320 by 2868 for the iPhone's 6.9 inch display and 2064 by 2752
for the iPad's 13 inch one: scaled to fit whole, centred, the rest filled with the picture's own colour at x = 3 on the middle row, as
scripts/play-screens.py does for Play. Nothing is cut off and nothing is stretched.

`upload` sends docs/store/apple/iphone/ and docs/store/apple/ipad/ to version 1.0's English (U.S.) screenshot sets through the App Store
Connect API, in that order, with the key the nightly uploads with (APPLE_API_ISSUER_ID, APPLE_API_KEY_ID, APPLE_API_KEY_P8). A set that
already holds screenshots is left alone. It never attaches a build and never submits anything for review: both wait for Alan's yes.
"""

from __future__ import annotations

import hashlib
import importlib.util
import json
import os
import sys
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
STORE = REPO / "docs" / "store" / "apple"
PLACES = ["result", "capture", "fingerprint", "pairing", "targets", "scale-markers", "open-targets"]
SIZES = {"iphone": (1320, 2868), "ipad": (2064, 2752)}
# App Store Connect's display types: the 6.9 inch iPhone's pictures go in the 6.7 inch set, which takes 1320 by 2868, and the 13 inch
# iPad's in the 12.9 inch set, which takes 2064 by 2752.
DISPLAY = {"iphone": "APP_IPHONE_67", "ipad": "APP_IPAD_PRO_3GEN_129"}
# Entry 513: the Mac App Store's pictures, from the desktop screens, in this order.
MAC_PLACES = ["analysis", "marking", "compare", "ballistics-hit", "targets"]
MAC_SIZE = (2880, 1800)
SCREENS = REPO / "docs" / "figures" / "screens" / "current"
BUNDLE = "org.grouplab.app"
VERSION = "1.0"


def name(i: int, place: str) -> str:
    return f"{i:02d}-{place}.png"


def fit(source: Path, size: tuple[int, int]):
    from PIL import Image

    picture = Image.open(source).convert("RGB")
    width, height = size
    scale = min(width / picture.width, height / picture.height)
    scaled = picture.resize((round(picture.width * scale), round(picture.height * scale)), Image.LANCZOS)
    canvas = Image.new("RGB", size, picture.getpixel((3, picture.height // 2)))
    canvas.paste(scaled, ((width - scaled.width) // 2, (height - scaled.height) // 2))
    return canvas, picture.size


def fit_all(results: Path, device: str, out: Path) -> int:
    out.mkdir(parents=True, exist_ok=True)
    made, missing = {}, []
    for i, place in enumerate(PLACES, 1):
        source = results / f"{place}.png"
        if not source.is_file():
            missing.append(place)
            continue
        picture, raw = fit(source, SIZES[device])
        picture.save(out / name(i, place), optimize=True)
        made[name(i, place)] = {"from": f"{place}.png", "taken": list(raw)}
    (out / "fitted.json").write_text(json.dumps({"size": list(SIZES[device]), "pictures": made, "missing": missing}, indent=1) + "\n")
    print(f"{device}: {len(made)} of {len(PLACES)} fitted to {SIZES[device][0]} by {SIZES[device][1]}"
          + (f"; missing: {', '.join(missing)}" if missing else ""))
    return 0 if made else 1


def fit_mac() -> int:
    out = STORE / "mac"
    out.mkdir(parents=True, exist_ok=True)
    made = {}
    for i, place in enumerate(MAC_PLACES, 1):
        source = SCREENS / f"{place}-dark-2560x1440.png"
        picture, raw = fit(source, MAC_SIZE)
        picture.save(out / name(i, place), optimize=True)
        made[name(i, place)] = {"from": f"docs/figures/screens/current/{source.name}", "taken": list(raw)}
    (out / "fitted.json").write_text(json.dumps({"size": list(MAC_SIZE), "pictures": made}, indent=1) + "\n", encoding="utf-8")
    print(f"mac: {len(made)} fitted to {MAC_SIZE[0]} by {MAC_SIZE[1]}")
    return 0


def apple():
    spec = importlib.util.spec_from_file_location("testflight", REPO / "scripts" / "testflight.py")
    testflight = importlib.util.module_from_spec(spec)
    sys.modules["testflight"] = testflight  # its dataclasses look their module up by name
    spec.loader.exec_module(testflight)
    store = testflight.Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"],
                             testflight.key_text(os.environ["APPLE_API_KEY_P8"]))
    return store, testflight.Refused


def put(address: str, headers: dict, data: bytes) -> None:
    request = urllib.request.Request(address, method="PUT", data=data, headers=headers)
    with urllib.request.urlopen(request, timeout=300) as answer:
        answer.read()


def upload(dry_run: bool) -> int:
    pictures = {d: sorted((STORE / d).glob("[0-9][0-9]-*.png")) for d in SIZES}
    for device, files in pictures.items():
        print(f"{device}: {len(files)} pictures for {DISPLAY[device]}")
    if dry_run:
        print("a dry run: nothing was sent")
        return 0
    store, Refused = apple()
    try:
        app = store.app(BUNDLE)
        versions = store.call("GET", f"/v1/apps/{app}/appStoreVersions?filter[platform]=IOS&filter[versionString]={VERSION}").get("data", [])
        if not versions:
            print(f"No version {VERSION} in App Store Connect; nothing was sent")
            return 1
        version = versions[0]["id"]
        localizations = store.call("GET", f"/v1/appStoreVersions/{version}/appStoreVersionLocalizations").get("data", [])
        english = next((l for l in localizations if l["attributes"].get("locale") == "en-US"), None)
        if not english:
            print("No English (U.S.) localization on version 1.0; nothing was sent")
            return 1
        sets = {s["attributes"]["screenshotDisplayType"]: s["id"]
                for s in store.call("GET", f"/v1/appStoreVersionLocalizations/{english['id']}/appScreenshotSets").get("data", [])}
        for device, files in pictures.items():
            kind = DISPLAY[device]
            if not files:
                print(f"{device}: no pictures, skipped")
                continue
            if kind in sets:
                held = store.call("GET", f"/v1/appScreenshotSets/{sets[kind]}/appScreenshots").get("data", [])
                if held:
                    print(f"{device}: the {kind} set already holds {len(held)} screenshots; left as it is")
                    continue
                chosen = sets[kind]
            else:
                chosen = store.call("POST", "/v1/appScreenshotSets", {"data": {"type": "appScreenshotSets", "attributes": {"screenshotDisplayType": kind},
                                    "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": english["id"]}}}}})["data"]["id"]
            ids = []
            for path in files:
                data = path.read_bytes()
                made = store.call("POST", "/v1/appScreenshots", {"data": {"type": "appScreenshots",
                                  "attributes": {"fileName": path.name, "fileSize": len(data)},
                                  "relationships": {"appScreenshotSet": {"data": {"type": "appScreenshotSets", "id": chosen}}}}})["data"]
                for part in made["attributes"]["uploadOperations"]:
                    headers = {h["name"]: h["value"] for h in part.get("requestHeaders", [])}
                    put(part["url"], headers, data[part["offset"]:part["offset"] + part["length"]])
                store.call("PATCH", f"/v1/appScreenshots/{made['id']}", {"data": {"type": "appScreenshots", "id": made["id"],
                           "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(data).hexdigest()}}})
                ids.append(made["id"])
            store.call("PATCH", f"/v1/appScreenshotSets/{chosen}/relationships/appScreenshots",
                       {"data": [{"type": "appScreenshots", "id": i} for i in ids]})
            print(f"{device}: {len(ids)} screenshots uploaded to {kind}, in order")
    except Refused as refused:
        print(f"App Store Connect refused a request ({refused}); what is in docs/store/apple/ waits for the planning session to upload by hand")
        return 1
    print("No build was attached and nothing was submitted for review.")
    return 0


def made_from(source: Path, ref: str) -> int:
    """docs/store/apple/made-from.json: which build, which simulators, and what each picture was fitted from."""
    devices = source / "devices.txt"
    made = {"about": "App Store screenshots taken on the iOS simulators by .github/workflows/apple-screens.yml (entry 487 section 4), "
                     "fitted to App Store Connect's sizes by scripts/apple-screens.py.",
            "source": ref, "devices": devices.read_text().strip().splitlines() if devices.is_file() else [],
            "theme": "dark", "statusBar": "9:41, full battery and signal", "sample": "samples/gl-cf25-ltr-d-25-shots-600-dpi.png"}
    for device in SIZES:
        fitted = source / "apple" / device / "fitted.json"
        made[device] = json.loads(fitted.read_text()) if fitted.is_file() else None
    STORE.mkdir(parents=True, exist_ok=True)
    (STORE / "made-from.json").write_text(json.dumps(made, indent=1) + "\n")
    return 0


def main(argv: list[str]) -> int:
    if len(argv) >= 3 and argv[0] == "made-from":
        return made_from(Path(argv[1]), argv[2])
    if len(argv) >= 4 and argv[0] == "fit":
        return fit_all(Path(argv[1]), argv[2], Path(argv[3]))
    if argv and argv[0] == "mac":
        return fit_mac()
    if argv and argv[0] == "upload":
        return upload("--dry-run" in argv)
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
