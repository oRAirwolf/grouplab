#!/usr/bin/env python3
"""Every new build to the App Store, iPhone and iPad and the Mac, NOTES-FROM-PLANNING.md entry 513.

Alan, 2026-11-23: "All new builds should be submitted to all of the app stores including the apple app store, play store, and microsoft
store." An Apple-approved build is released by itself, with no word from him each time, and the Mac App Store copy joins now too. Each
run of the testflight workflow (after every nightly and every half hour) moves each platform on by what Apple allows at that moment, and
can be run again at any time without doing anything twice:

1. A version Apple approved and holds for release (PENDING_DEVELOPER_RELEASE) is released.
2. While a version is waiting for review, in review or being released, nothing is submitted on that platform: the newer build waits, and
   the newest waiting build goes in once the review ends. A review in progress is never cancelled.
3. Otherwise the newest build Apple has processed, from a nightly whose release notes are published (a nightly whose checks fail never
   publishes them), newer than the one live and than any turned down, is put on an App Store version, released automatically once
   approved: the version being prepared or turned down is reused, or a new one is made, named by the build's own version (1.0.N).
4. Its English (U.S.) listing is made complete first: for iPhone and iPad the description carries the sentence about iOS 18 to 25 (entry
   505); a first Mac version takes the iPhone listing's keywords and links, the description in docs/store/apple/mac-description.txt, the
   screenshots in docs/store/apple/mac/ and the review contact of the iPhone version, which is never printed. What's New is the nightly's
   "What you will notice" lines, cut to Apple's limit, on every version after a platform's first.
5. Then it is submitted for App Review. Anything still missing is named and nothing is submitted.

Age rating, category, privacy answers and export compliance are the app's and its builds' own, set for 1.0 and reused. A version turned
down is said in a line for docs/notes/for-alan.md; Apple's words are in App Store Connect, which the API does not give.

STORE_SUBMISSIONS_PAUSED (a repository variable set to true, or a file of that name at the top of the repository) stops everything here.

    python3 scripts/app-store.py --run [--dry-run]   one step for each platform; --dry-run reads only and says what it would do
    python3 scripts/app-store.py --inspect           what App Store Connect holds for each platform, read only, no words of the listing
    python3 scripts/app-store.py --self-test         every case against a made-up App Store Connect, no network

The key is the one the nightly uploads with: APPLE_API_ISSUER_ID, APPLE_API_KEY_ID, APPLE_API_KEY_P8. No key, email, phone number or
token is ever printed. The lines are printed and appended to $GITHUB_STEP_SUMMARY when that is set.
"""
from __future__ import annotations

import hashlib
import importlib.util
import os
import re
import sys
import urllib.request
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
STORE_FILES = REPO / "docs" / "store" / "apple"
NOTES = REPO / "docs" / "RELEASE-NOTES.md"
PAUSE = "STORE_SUBMISSIONS_PAUSED"

_spec = importlib.util.spec_from_file_location("testflight", REPO / "scripts" / "testflight.py")
testflight = importlib.util.module_from_spec(_spec)
sys.modules["testflight"] = testflight  # its dataclasses look their module up by name
_spec.loader.exec_module(testflight)
BUNDLE_ID, Refused, VERSION_WORDS = testflight.BUNDLE_ID, testflight.Refused, testflight.VERSION_WORDS

PLATFORMS = {"IOS": "iPhone and iPad", "MAC_OS": "Mac"}
# A platform's review in Apple's hands: nothing more is submitted there until it ends.
BUSY = {"WAITING_FOR_REVIEW", "IN_REVIEW", "WAITING_FOR_EXPORT_COMPLIANCE", "ACCEPTED", "PENDING_APPLE_RELEASE",
        "PROCESSING_FOR_APP_STORE", "PROCESSING_FOR_DISTRIBUTION"}
TURNED_DOWN = {"REJECTED", "METADATA_REJECTED", "INVALID_BINARY"}
# The one version a platform may have that can still be changed and submitted.
EDITABLE = {"PREPARE_FOR_SUBMISSION", "READY_FOR_REVIEW", "DEVELOPER_REJECTED"} | TURNED_DOWN
RELEASED = {"READY_FOR_SALE", "READY_FOR_DISTRIBUTION"}
# The screenshot sets App Review needs on each platform, and the committed pictures that fill an empty one.
SETS = {"IOS": {"APP_IPHONE_67": "iphone", "APP_IPAD_PRO_3GEN_129": "ipad"}, "MAC_OS": {"APP_DESKTOP": "mac"}}
NEEDED = ("description", "keywords", "supportUrl")
COPIED = ("description", "keywords", "supportUrl", "marketingUrl", "promotionalText")
IOS_FLOOR = "Requires iOS 26 or later. It also installs on iOS 18 to 25, without support."
WHATS_NEW_LIMIT = 4000
COPYRIGHT = "2026 Alan Hayes"


@dataclass
class Build:
    id: str
    number: int
    version: str             # the build's own short version, such as 1.0.242, which the App Store version must carry
    processing: str
    expired: bool = False
    encryption: bool | None = None


@dataclass
class Version:
    id: str
    number: str
    state: str
    created: str = ""
    build: int | None = None
    release_type: str = ""
    copyright: str = ""


class Connect:
    """What a step needs from App Store Connect. The real one talks to Apple; the self-test's keeps everything in memory."""

    def app(self, bundle: str) -> str | None: raise NotImplementedError
    def builds(self, app: str, platform: str) -> list[Build]: raise NotImplementedError
    def versions(self, app: str, platform: str) -> list[Version]: raise NotImplementedError
    def make_version(self, app: str, platform: str, number: str, build: str, copyright: str) -> str: raise NotImplementedError
    def change_version(self, version: str, number: str, build: str) -> None: raise NotImplementedError
    def localization(self, version: str, make: bool = True) -> tuple[str, dict]: raise NotImplementedError
    def change_localization(self, localization: str, fields: dict) -> None: raise NotImplementedError
    def screenshot_counts(self, localization: str) -> dict[str, int]: raise NotImplementedError
    def upload_screenshots(self, localization: str, display: str, files: list[Path]) -> int: raise NotImplementedError
    def review_detail(self, version: str) -> dict | None: raise NotImplementedError
    def make_review_detail(self, version: str, fields: dict) -> None: raise NotImplementedError
    def no_encryption(self, build: str) -> None: raise NotImplementedError
    def submit(self, app: str, platform: str, version: str) -> None: raise NotImplementedError
    def release(self, version: str) -> None: raise NotImplementedError


def published(notes: str) -> set[int]:
    """The nightlies whose release notes are published: only a nightly that passed its checks writes them."""
    return {int(n) for n in re.findall(r"^## [^\n]*nightly\.(\d+)\s*$", notes, re.M)}


def whats_new(number: int, notes: str) -> str:
    return testflight.whats_new(number, notes)[:WHATS_NEW_LIMIT]


def paused() -> bool:
    return os.environ.get(PAUSE, "").strip().lower() in ("true", "1", "yes") or (REPO / PAUSE).exists()


def newest(versions: list[Version], states: set[str]) -> Version | None:
    return next((v for v in sorted(versions, key=lambda v: v.created, reverse=True) if v.state in states), None)


def step(store: Connect, platform: str, notes: str, dry_run: bool = False) -> tuple[list[str], str | None]:
    """One step for one platform. Returns the lines for the summary and, where Alan should know, the line for docs/notes/for-alan.md."""
    name = f"App Store, {PLATFORMS[platform]}"
    would = "would be " if dry_run else ""
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"{name}: App Store Connect has no app {BUNDLE_ID}; nothing was submitted."], None
    versions = store.versions(app, platform)
    held = newest(versions, {"PENDING_DEVELOPER_RELEASE"})
    if held:
        if not dry_run:
            store.release(held.id)
        return [f"{name}: version {held.number} (build {held.build}) was approved and {would}released now (entry 513: an approved build "
                "is released by itself)."], None
    busy = newest(versions, BUSY)
    ready = sorted((b for b in store.builds(app, platform) if b.processing == "VALID" and not b.expired and b.number in published(notes)),
                   key=lambda b: b.number, reverse=True)
    candidate = ready[0] if ready else None
    if busy:
        waiting = f"; build {candidate.number} waits until the review ends" if candidate and candidate.number != busy.build else ""
        return [f"{name}: version {busy.number} (build {busy.build}) is {VERSION_WORDS.get(busy.state, busy.state.lower())}{waiting}."], None
    live = newest(versions, RELEASED)
    editable = newest(versions, EDITABLE)
    down = editable if editable and editable.state in TURNED_DOWN else None
    floor = max((live.build or 0) if live else 0, (down.build or 0) if down else 0)
    if candidate is None or candidate.number <= floor:
        lines = [f"{name}: nothing newer to submit; on the App Store: {'version ' + live.number if live else 'nothing yet'}."]
        alan = None
        if down:
            alan = (f"Apple's App Review turned down GroupLab for {PLATFORMS[platform]} {down.number} (build {down.build}). Apple's words "
                    "are under App Review in App Store Connect; the next nightly's build is submitted in its place.")
            lines.append(alan)
        return lines, alan
    build = candidate
    if dry_run:
        how = f"version {editable.number} ({editable.state.lower()}) reused" if editable else "a new version made"
        return [f"{name}: build {build.number} would be submitted as version {build.version}, released once approved; {how}."], None
    if editable:
        store.change_version(editable.id, build.version, build.id)
        version = editable.id
    else:
        source = newest(store.versions(app, "IOS"), RELEASED)
        version = store.make_version(app, platform, build.version, build.id, (source.copyright if source else "") or COPYRIGHT)
    missing = complete(store, app, platform, version, build, notes, first=live is None)
    if missing:
        alan = (f"GroupLab for {PLATFORMS[platform]} build {build.number} is on App Store version {build.version} but was not submitted: "
                f"{'; '.join(missing)}.")
        return [f"{name}: {alan}"], alan
    if build.encryption is None:
        store.no_encryption(build.id)
    store.submit(app, platform, version)
    return [f"{name}: build {build.number} submitted for App Review as version {build.version}, released by itself once approved."], None


def complete(store: Connect, app: str, platform: str, version: str, build: Build, notes: str, first: bool) -> list[str]:
    """The version's English listing, screenshots and review contact made whole where they can be; returns what is still missing."""
    localization, fields = store.localization(version)
    reference = newest(store.versions(app, "IOS"), RELEASED)
    ref_fields = store.localization(reference.id)[1] if reference and reference.id != version else {}
    change = {k: ref_fields[k] for k in COPIED if not fields.get(k) and ref_fields.get(k)}
    if platform == "MAC_OS":
        mac = STORE_FILES / "mac-description.txt"
        if not fields.get("description") and mac.is_file():
            change["description"] = mac.read_text(encoding="utf-8").strip()
    description = change.get("description", fields.get("description") or "")
    if platform == "IOS" and description and "installs on iOS 18 to 25" not in description:
        change["description"] = description.rstrip() + "\n\n" + IOS_FLOOR
    if not first:
        change["whatsNew"] = whats_new(build.number, notes)
    if change:
        store.change_localization(localization, change)
        fields = {**fields, **change}
    missing = [f"no {k} in its English listing" for k in NEEDED if not fields.get(k)]
    counts = store.screenshot_counts(localization)
    for display, folder in SETS[platform].items():
        if counts.get(display):
            continue
        files = sorted((STORE_FILES / folder).glob("[0-9][0-9]-*.png"))
        if not files or not store.upload_screenshots(localization, display, files):
            missing.append(f"no {display} screenshots")
    if store.review_detail(version) is None:
        source = store.review_detail(reference.id) if reference and reference.id != version else None
        if source:
            store.make_review_detail(version, source)
        else:
            missing.append("no App Review contact")
    return missing


def inspect(store: Connect, notes: str) -> list[str]:
    """What App Store Connect holds, read only: states, builds, which listing fields are filled (never their words), screenshot counts."""
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"App Store Connect has no app {BUNDLE_ID}."]
    lines = []
    ready = published(notes)
    for platform, called in PLATFORMS.items():
        builds = store.builds(app, platform)
        good = [b for b in builds if b.processing == "VALID" and not b.expired]
        lines.append(f"{called}: {len(builds)} builds, newest processed {good[0].number if good else 'none'} (version "
                     f"{good[0].version if good else '-'}, export answer {good[0].encryption if good else '-'}), newest with published notes "
                     f"{next((b.number for b in good if b.number in ready), 'none')}")
        for v in sorted(store.versions(app, platform), key=lambda v: v.created, reverse=True)[:4]:
            localization, fields = store.localization(v.id, make=False)
            filled = ", ".join(k for k in COPIED + ("whatsNew",) if fields.get(k)) or "nothing"
            counts = store.screenshot_counts(localization) if localization else {}
            lines.append(f"  version {v.number} {v.state} build {v.build} release {v.release_type or '-'}; English listing: {filled}; "
                         f"screenshots: {', '.join(f'{k} {n}' for k, n in sorted(counts.items())) or 'none'}; review contact: "
                         f"{'yes' if store.review_detail(v.id) else 'no'}")
    return lines


class Apple(testflight.Apple, Connect):
    """App Store Connect's own API, signed with the key the nightly already uses to upload."""

    def builds(self, app, platform):
        answer = self.call("GET", f"/v1/builds?filter[app]={app}&filter[preReleaseVersion.platform]={platform}"
                                  "&sort=-uploadedDate&limit=50&include=preReleaseVersion")
        short = {p["id"]: p["attributes"].get("version", "") for p in answer.get("included", []) if p.get("type") == "preReleaseVersions"}
        found = []
        for b in answer.get("data", []):
            a = b["attributes"]
            pre = ((b.get("relationships") or {}).get("preReleaseVersion") or {}).get("data") or {}
            found.append(Build(b["id"], int(re.sub(r"\D", "", a.get("version", "0")) or 0), short.get(pre.get("id", ""), ""),
                               a.get("processingState", ""), a.get("expired", False), a.get("usesNonExemptEncryption")))
        return sorted(found, key=lambda b: b.number, reverse=True)

    def versions(self, app, platform):
        answer = self.call("GET", f"/v1/apps/{app}/appStoreVersions?filter[platform]={platform}&include=build&limit=20")
        builds = {b["id"]: b["attributes"].get("version", "") for b in answer.get("included", []) if b.get("type") == "builds"}
        found = []
        for v in answer.get("data", []):
            a = v["attributes"]
            link = ((v.get("relationships") or {}).get("build") or {}).get("data") or {}
            number = re.sub(r"\D", "", builds.get(link.get("id", ""), ""))
            found.append(Version(v["id"], a.get("versionString", ""), a.get("appVersionState") or a.get("appStoreState") or "",
                                 a.get("createdDate", ""), int(number) if number else None, a.get("releaseType") or "", a.get("copyright") or ""))
        return found

    def make_version(self, app, platform, number, build, copyright):
        made = self.call("POST", "/v1/appStoreVersions", {"data": {
            "type": "appStoreVersions",
            "attributes": {"platform": platform, "versionString": number, "releaseType": "AFTER_APPROVAL", "copyright": copyright},
            "relationships": {"app": {"data": {"type": "apps", "id": app}}, "build": {"data": {"type": "builds", "id": build}}}}})
        return made["data"]["id"]

    def change_version(self, version, number, build):
        self.call("PATCH", f"/v1/appStoreVersions/{version}", {"data": {"type": "appStoreVersions", "id": version,
                  "attributes": {"versionString": number, "releaseType": "AFTER_APPROVAL"}}})
        self.call("PATCH", f"/v1/appStoreVersions/{version}/relationships/build", {"data": {"type": "builds", "id": build}})

    def localization(self, version, make=True):
        found = self.call("GET", f"/v1/appStoreVersions/{version}/appStoreVersionLocalizations").get("data", [])
        english = next((l for l in found if l["attributes"].get("locale") == "en-US"), None)
        if english is None and not make:
            return "", {}
        if english is None:
            made = self.call("POST", "/v1/appStoreVersionLocalizations", {"data": {"type": "appStoreVersionLocalizations",
                             "attributes": {"locale": "en-US"},
                             "relationships": {"appStoreVersion": {"data": {"type": "appStoreVersions", "id": version}}}}})
            english = made["data"]
        return english["id"], dict(english.get("attributes") or {})

    def change_localization(self, localization, fields):
        self.call("PATCH", f"/v1/appStoreVersionLocalizations/{localization}",
                  {"data": {"type": "appStoreVersionLocalizations", "id": localization, "attributes": fields}})

    def screenshot_counts(self, localization):
        sets = self.call("GET", f"/v1/appStoreVersionLocalizations/{localization}/appScreenshotSets?include=appScreenshots&limit=50")
        return {s["attributes"]["screenshotDisplayType"]: len(((s.get("relationships") or {}).get("appScreenshots") or {}).get("data") or [])
                for s in sets.get("data", [])}

    def upload_screenshots(self, localization, display, files):
        sets = self.call("GET", f"/v1/appStoreVersionLocalizations/{localization}/appScreenshotSets?limit=50").get("data", [])
        chosen = next((s["id"] for s in sets if s["attributes"]["screenshotDisplayType"] == display), None)
        if chosen is None:
            chosen = self.call("POST", "/v1/appScreenshotSets", {"data": {"type": "appScreenshotSets",
                               "attributes": {"screenshotDisplayType": display},
                               "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations",
                                                                                          "id": localization}}}}})["data"]["id"]
        ids = []
        for path in files:
            data = path.read_bytes()
            made = self.call("POST", "/v1/appScreenshots", {"data": {"type": "appScreenshots",
                             "attributes": {"fileName": path.name, "fileSize": len(data)},
                             "relationships": {"appScreenshotSet": {"data": {"type": "appScreenshotSets", "id": chosen}}}}})["data"]
            for part in made["attributes"]["uploadOperations"]:
                headers = {h["name"]: h["value"] for h in part.get("requestHeaders", [])}
                put = urllib.request.Request(part["url"], method="PUT", data=data[part["offset"]:part["offset"] + part["length"]], headers=headers)
                with urllib.request.urlopen(put, timeout=300) as answer:
                    answer.read()
            self.call("PATCH", f"/v1/appScreenshots/{made['id']}", {"data": {"type": "appScreenshots", "id": made["id"],
                      "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(data).hexdigest()}}})
            ids.append(made["id"])
        self.call("PATCH", f"/v1/appScreenshotSets/{chosen}/relationships/appScreenshots",
                  {"data": [{"type": "appScreenshots", "id": i} for i in ids]})
        return len(ids)

    def review_detail(self, version):
        try:
            data = self.call("GET", f"/v1/appStoreVersions/{version}/appStoreReviewDetail").get("data")
        except Refused as said:
            if str(said).startswith("404"):
                return None
            raise
        if not data:
            return None
        keep = ("contactFirstName", "contactLastName", "contactPhone", "contactEmail", "demoAccountRequired", "notes")
        return {k: v for k, v in (data.get("attributes") or {}).items() if k in keep and v is not None}

    def make_review_detail(self, version, fields):
        self.call("POST", "/v1/appStoreReviewDetails", {"data": {"type": "appStoreReviewDetails", "attributes": fields,
                  "relationships": {"appStoreVersion": {"data": {"type": "appStoreVersions", "id": version}}}}})

    def submit(self, app, platform, version):
        open_ = self.call("GET", f"/v1/reviewSubmissions?filter[app]={app}&filter[platform]={platform}"
                                 "&filter[state]=READY_FOR_REVIEW,UNRESOLVED_ISSUES&limit=10").get("data", [])
        if open_:
            submission = open_[0]["id"]
            items = self.call("GET", f"/v1/reviewSubmissions/{submission}/items?include=appStoreVersion&limit=50").get("data", [])
            held = {((i.get("relationships") or {}).get("appStoreVersion") or {}).get("data", {}).get("id") for i in items}
        else:
            submission = self.call("POST", "/v1/reviewSubmissions", {"data": {"type": "reviewSubmissions", "attributes": {"platform": platform},
                                   "relationships": {"app": {"data": {"type": "apps", "id": app}}}}})["data"]["id"]
            held = set()
        if version not in held:
            self.call("POST", "/v1/reviewSubmissionItems", {"data": {"type": "reviewSubmissionItems", "relationships": {
                "reviewSubmission": {"data": {"type": "reviewSubmissions", "id": submission}},
                "appStoreVersion": {"data": {"type": "appStoreVersions", "id": version}}}}})
        self.call("PATCH", f"/v1/reviewSubmissions/{submission}",
                  {"data": {"type": "reviewSubmissions", "id": submission, "attributes": {"submitted": True}}})


@dataclass
class Pretend(Connect):
    """A made-up App Store Connect for the self-test."""
    builds_: dict[str, list[Build]] = field(default_factory=dict)
    versions_: dict[str, list[Version]] = field(default_factory=dict)
    texts: dict[str, dict] = field(default_factory=dict)
    shots: dict[str, dict[str, int]] = field(default_factory=dict)
    details: dict[str, dict] = field(default_factory=dict)
    submitted: list[str] = field(default_factory=list)
    released: list[str] = field(default_factory=list)
    uploaded: list[tuple[str, int]] = field(default_factory=list)
    writes: int = 0

    def app(self, bundle): return "app" if bundle == BUNDLE_ID else None
    def builds(self, app, platform): return list(self.builds_.get(platform, []))
    def versions(self, app, platform): return list(self.versions_.get(platform, []))

    def make_version(self, app, platform, number, build, copyright):
        self.writes += 1
        made = Version(f"v{platform}{number}", number, "PREPARE_FOR_SUBMISSION", "9999", self.number_of(build), "AFTER_APPROVAL", copyright)
        self.versions_.setdefault(platform, []).append(made)
        return made.id

    def number_of(self, build):
        return next(b.number for bs in self.builds_.values() for b in bs if b.id == build)

    def change_version(self, version, number, build):
        self.writes += 1
        v = self.find(version)
        v.number, v.build, v.release_type = number, self.number_of(build), "AFTER_APPROVAL"

    def find(self, version):
        return next(v for vs in self.versions_.values() for v in vs if v.id == version)

    def localization(self, version, make=True): return "l" + version, dict(self.texts.get("l" + version, {}))

    def change_localization(self, localization, fields):
        self.writes += 1
        self.texts.setdefault(localization, {}).update(fields)

    def screenshot_counts(self, localization): return dict(self.shots.get(localization, {}))

    def upload_screenshots(self, localization, display, files):
        self.writes += 1
        self.shots.setdefault(localization, {})[display] = len(files)
        self.uploaded.append((display, len(files)))
        return len(files)

    def review_detail(self, version): return self.details.get(version)

    def make_review_detail(self, version, fields):
        self.writes += 1
        self.details[version] = dict(fields)

    def no_encryption(self, build): self.writes += 1

    def submit(self, app, platform, version):
        self.writes += 1
        self.find(version).state = "WAITING_FOR_REVIEW"
        self.submitted.append(version)

    def release(self, version):
        self.writes += 1
        self.find(version).state = "PROCESSING_FOR_DISTRIBUTION"
        self.released.append(version)


def self_test() -> int:
    failures: list[str] = []

    def expect(what: str, ok: bool, detail: object = "") -> None:
        if not ok:
            failures.append(f"{what}: {detail}")

    notes = ("## 1.0.0-nightly.243\n\n**What you will notice**\n\n- The square prints a set's place.\n\n"
             "## 1.0.0-nightly.242\n\n**What you will notice**\n\n- Something you can see.\n\n## 1.0.0-nightly.226\n")
    listing = {"description": "GroupLab measures a group.", "keywords": "group size", "supportUrl": "https://grouplab.org/support/",
               "marketingUrl": "https://grouplab.org/", "promotionalText": "Free."}
    contact = {"contactFirstName": "A", "contactLastName": "H", "contactPhone": "+1 000", "contactEmail": "x@example.org", "notes": "demo"}

    def world() -> Pretend:
        p = Pretend()
        p.builds_ = {"IOS": [Build("b244", 244, "1.0.244", "VALID"), Build("b242", 242, "1.0.242", "VALID", encryption=False),
                             Build("b226", 226, "1.0", "VALID")],
                     "MAC_OS": [Build("m242", 242, "1.0.242", "VALID")]}
        p.versions_ = {"IOS": [Version("v1", "1.0", "READY_FOR_DISTRIBUTION", "2026-11-15", 226, "MANUAL", "2026 Alan Hayes")], "MAC_OS": []}
        p.texts = {"lv1": dict(listing)}
        p.shots = {"lv1": {"APP_IPHONE_67": 7, "APP_IPAD_PRO_3GEN_129": 7}}
        p.details = {"v1": dict(contact)}
        return p

    # iPhone and iPad: a new version for the newest build with published notes (244 has none: its nightly did not publish).
    p = world()
    before = p.writes
    lines, _ = step(p, "IOS", notes, dry_run=True)
    expect("a dry run changes nothing", p.writes == before and "would be submitted as version 1.0.242" in lines[0], lines)
    lines, alan = step(p, "IOS", notes)
    made = p.versions_["IOS"][-1]
    expect("the newest build from a published nightly is submitted", p.submitted == [made.id] and made.build == 242
           and made.number == "1.0.242" and made.release_type == "AFTER_APPROVAL", (lines, made))
    expect("a build whose nightly published no notes is never submitted", all(v.build != 244 for v in p.versions_["IOS"]))
    text = p.texts["l" + made.id]
    expect("the listing is carried over, with the iOS 18 sentence", text["description"].endswith(IOS_FLOOR) and text["keywords"] == "group size")
    expect("What's New is the nightly's noticed lines", "Something you can see." in text.get("whatsNew", ""), text)
    expect("the review contact is carried over", p.details.get(made.id) == contact)
    expect("the copyright is the released version's", made.copyright == "2026 Alan Hayes")
    # A version in review holds the newer build back, and nothing is cancelled.
    notes2 = "## 1.0.0-nightly.244\n\n**What you will notice**\n\n- Newer.\n\n" + notes
    lines, _ = step(p, "IOS", notes2)
    expect("a review in progress makes the newer build wait", "build 244 waits until the review ends" in lines[0]
           and len(p.submitted) == 1, lines)
    # Approved and held for release: released by itself.
    made.state = "PENDING_DEVELOPER_RELEASE"
    lines, _ = step(p, "IOS", notes2)
    expect("an approved version is released by itself", p.released == [made.id], lines)
    made.state = "READY_FOR_DISTRIBUTION"
    lines, _ = step(p, "IOS", notes2)
    newer = p.versions_["IOS"][-1]
    expect("after the release, the waiting build goes in on a new version", newer.build == 244 and newer.state == "WAITING_FOR_REVIEW"
           and p.texts["l" + newer.id]["description"].count(IOS_FLOOR) == 1, lines)
    # Turned down: said for Alan; the same build is not sent again, and the next build reuses the version.
    newer.state = "REJECTED"
    lines, alan = step(p, "IOS", notes2)
    expect("a rejection is said for Alan and nothing is resubmitted with the same build", alan and "turned down" in alan
           and p.submitted.count(newer.id) == 1, lines)
    p.builds_["IOS"].insert(0, Build("b245", 245, "1.0.245", "VALID"))
    notes3 = "## 1.0.0-nightly.245\n\n**What you will notice**\n\n- Fixed.\n\n" + notes2
    lines, _ = step(p, "IOS", notes3)
    expect("the next build reuses the turned-down version", newer.build == 245 and newer.number == "1.0.245"
           and newer.state == "WAITING_FOR_REVIEW" and p.submitted.count(newer.id) == 2, (lines, newer))
    # The Mac's first version: the listing from the iPhone's, its own description and screenshots, no What's New.
    p = world()
    lines, alan = step(p, "MAC_OS", notes)
    mac = p.versions_["MAC_OS"][-1] if p.versions_["MAC_OS"] else None
    has_mac_files = (STORE_FILES / "mac-description.txt").is_file() and any((STORE_FILES / "mac").glob("[0-9][0-9]-*.png"))
    expect("the committed Mac description and screenshots exist", has_mac_files)
    if mac:
        t = p.texts["l" + mac.id]
        expect("the first Mac version is submitted", p.submitted == [mac.id] and mac.number == "1.0.242", (lines, alan))
        expect("its description is the Mac's own, without the iOS sentence", IOS_FLOOR not in t["description"]
               and t["description"] != listing["description"] and t["keywords"] == "group size", t)
        expect("a platform's first version has no What's New", "whatsNew" not in t, t)
        expect("its screenshots are uploaded to the desktop set", p.uploaded == [("APP_DESKTOP", len(list((STORE_FILES / "mac").glob("[0-9][0-9]-*.png"))))],
               p.uploaded)
        expect("it takes the iPhone version's review contact", p.details.get(mac.id) == contact)
    # Missing pieces are named and nothing is submitted.
    p = world()
    p.details = {}
    lines, alan = step(p, "IOS", notes)
    expect("a version with something missing is not submitted, and says what", not p.submitted and alan and "no App Review contact" in alan,
           lines)
    # Nothing new: nothing happens.
    p = world()
    p.builds_["IOS"] = [Build("b226", 226, "1.0", "VALID")]
    before = p.writes
    lines, alan = step(p, "IOS", notes)
    expect("nothing newer than the live build: nothing written", p.writes == before and alan is None and "nothing newer" in lines[0], lines)
    expect("inspect prints no listing words or contact", not any("GroupLab measures" in l or "+1 000" in l for l in inspect(world(), notes)))

    for f in failures:
        print("FAIL " + f)
    print(f"app-store self-test: {'all passed' if not failures else f'{len(failures)} failed'}")
    return 1 if failures else 0


def store_from_env() -> Apple | None:
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        return None
    return Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"], testflight.key_text(os.environ["APPLE_API_KEY_P8"]))


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] not in (["--run"], ["--inspect"]):
        print(__doc__)
        return 2
    notes = NOTES.read_text(encoding="utf-8") if NOTES.is_file() else ""
    alans: list[str] = []
    store = store_from_env()
    if store is None:
        lines = ["App Store: the App Store Connect key is not set, so nothing was read or submitted."]
    elif argv[1] == "--inspect":
        try:
            lines = inspect(store, notes)
        except Refused as said:
            lines = [f"App Store: App Store Connect refused a read ({said})."]
    elif paused():
        lines = [f"App Store: {PAUSE} is set, so nothing was submitted or released."]
    else:
        lines = []
        for platform in PLATFORMS:
            # Each platform on its own, so a refusal on one moves nothing on the other.
            try:
                said, alan = step(store, platform, notes, dry_run="--dry-run" in argv)
            except Refused as refused:
                said, alan = [f"App Store, {PLATFORMS[platform]}: App Store Connect refused a request ({refused}); the next run tries again."], None
            lines += said
            if alan:
                alans.append(alan)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(summary, "a", encoding="utf-8") if summary else open(os.devnull, "w") as out:
        out.write("### App Store submissions\n\n" + "".join(f"- {line}\n" for line in lines))
    for line in lines:
        print(line)
    for alan in alans:
        print(f"::warning::{alan}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
