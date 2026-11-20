#!/usr/bin/env python3
"""TestFlight's two groups, GroupLab Team and Public Beta, kept on the same build, a step at a time.

NOTES-FROM-PLANNING.md entry 310. Alan: "I would also like to keep the external testing version up to date with the internal testing
version. They should always be on the same version." Each run of the testflight workflow, after every nightly and every half hour, moves the
state on by what Apple allows at that moment, and can be run again at any time without doing anything twice:

1. The newest build Apple has finished processing goes to Public Beta, with that nightly's "What you will notice" lines as its What to Test,
   and is submitted for Beta App Review where Apple asks for one.
2. GroupLab Team is given the newest build that Public Beta can already install: at once where Apple needed no review, or once the review
   is approved. So neither group is ever ahead of the other. A newer build waiting for review leaves both on the one before it.
3. A rejected review moves nothing: both groups stay where they are, and the run says so, for a line in docs/notes/for-alan.md.
4. Where either group does not exist yet, or the App Store Connect key is not set, the run says so in one line and does nothing else. It
   never fails a build.

Nothing is ever taken out of a group: a build Alan added to GroupLab Team by hand is left there, and the run names it.

Entry 501: the app record also carries the Mac App Store copy, whose builds share its numbers. The steps above read the iPhone and iPad
builds only, so a Mac build never reaches Public Beta or Beta App Review. --mac-internal gives the Mac build named to GroupLab Team and
does nothing else: no Public Beta, no review, nothing submitted.

Entry 504 section 3: each --run also keeps the external group Mac testers (made by the run where it is missing, with a public link for at
most five testers) on the newest Mac App Store build, submitted for Beta App Review the way Public Beta's builds are. It takes Mac builds
only, Public Beta never takes one, and nothing here submits anything for App Store review. Once its testers can install a build, the run
says so, and that line reaches docs/notes/external-status.md; the public link itself is never printed, because this repository is public.

    python3 scripts/testflight.py --run [--wait-minutes N]   one step, with the key from APPLE_API_ISSUER_ID, APPLE_API_KEY_ID, APPLE_API_KEY_P8
    python3 scripts/testflight.py --mac-internal 233 [--wait-minutes N]   Mac App Store build 233 to GroupLab Team only (entry 501)
    python3 scripts/testflight.py --app-store                  the App Store version's review state, read only (entry 491)
    python3 scripts/testflight.py --release 1.0 226            release that approved version, only by hand, on Alan's word
    python3 scripts/testflight.py --self-test                  every case against a made-up App Store Connect, no network

Entry 491: the App Store version's review is read beside TestFlight, so an approval or a rejection is seen without anyone opening App
Store Connect. That check only reads: nothing here submits, cancels or changes an App Store version, so a nightly going to TestFlight
never takes a version out of review.

Alan, 2026-11-17: "release GroupLab 1.0 (build 226) on the App Store now". --release is the one thing here that changes an App Store
version, and only that way: it releases the version named, with the build named, when Apple has approved it and it waits for release
(PENDING_DEVELOPER_RELEASE), and does nothing at all otherwise. It is run only by hand, from the testflight workflow's release input, so
no schedule and no nightly can ever release a version.

The lines are printed and appended to $GITHUB_STEP_SUMMARY when that is set. No key, email or token is ever printed.
"""
from __future__ import annotations

import base64
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

BUNDLE_ID = "org.grouplab.app"
TEAM = "GroupLab Team"
BETA = "Public Beta"
MAC_TESTERS = "Mac testers"    # entry 504 section 3: external, Mac builds only, a public link for at most five
MAC_TESTERS_LIMIT = 5
API = "https://api.appstoreconnect.apple.com"
NOTES = Path(__file__).resolve().parent.parent / "docs" / "RELEASE-NOTES.md"

# The states of a build for external testers in which Public Beta's testers can install it.
AVAILABLE = {"BETA_APPROVED", "IN_BETA_TESTING", "READY_FOR_BETA_TESTING"}

# A Beta App Review still to be decided.
PENDING = {"WAITING_FOR_REVIEW", "IN_REVIEW"}

# The same, as a build's external state.
WAITING_STATES = {"WAITING_FOR_BETA_REVIEW", "IN_BETA_REVIEW"}


class Refused(Exception):
    """App Store Connect said no to a request, with its own words."""


@dataclass
class Build:
    id: str
    number: int
    processing: str          # PROCESSING, VALID, INVALID or FAILED
    expired: bool = False


@dataclass
class Version:
    """An App Store version as App Store Connect lists it: its number, its state, and the build attached, if any."""
    number: str
    state: str
    created: str = ""
    build: int | None = None
    id: str = ""


# Entry 491: an App Store version's state in plain words. Old and new names both, since Apple renamed them (appStoreState, appVersionState).
VERSION_WORDS = {
    "PREPARE_FOR_SUBMISSION": "being prepared, not submitted",
    "READY_FOR_REVIEW": "ready, not submitted",
    "WAITING_FOR_REVIEW": "waiting for review",
    "IN_REVIEW": "in review",
    "WAITING_FOR_EXPORT_COMPLIANCE": "waiting for the export question",
    "PENDING_DEVELOPER_RELEASE": "approved, waiting for Alan to release it",
    "ACCEPTED": "approved",
    "PENDING_APPLE_RELEASE": "approved, Apple is releasing it",
    "PROCESSING_FOR_APP_STORE": "approved, being prepared for the App Store",
    "PROCESSING_FOR_DISTRIBUTION": "approved, being prepared for the App Store",
    "READY_FOR_SALE": "released on the App Store",
    "READY_FOR_DISTRIBUTION": "released on the App Store",
    "REJECTED": "rejected by App Review",
    "METADATA_REJECTED": "rejected by App Review for its listing",
    "INVALID_BINARY": "rejected, Apple found the build invalid",
    "DEVELOPER_REJECTED": "taken out of review",
    "REPLACED_WITH_NEW_VERSION": "replaced by a newer version",
}
RELEASED = {"READY_FOR_SALE", "READY_FOR_DISTRIBUTION"}
TURNED_DOWN = {"REJECTED", "METADATA_REJECTED", "INVALID_BINARY"}


class Store:
    """What the steps need from App Store Connect. The real one talks to Apple; the self-test's keeps everything in memory."""

    def app(self, bundle: str) -> str | None: raise NotImplementedError
    def groups(self, app: str) -> dict[str, str]: raise NotImplementedError
    def builds(self, app: str, platform: str = "IOS") -> list[Build]: raise NotImplementedError
    def in_group(self, group: str) -> set[str]: raise NotImplementedError
    def add(self, group: str, build: str) -> None: raise NotImplementedError
    def external_state(self, build: str) -> str: raise NotImplementedError
    def review(self, build: str) -> str | None: raise NotImplementedError
    def submit(self, build: str) -> None: raise NotImplementedError
    def no_encryption(self, build: str) -> None: raise NotImplementedError
    def what_to_test(self, build: str, text: str) -> None: raise NotImplementedError
    def versions(self, app: str) -> list["Version"]: raise NotImplementedError
    def release(self, version: str) -> None: raise NotImplementedError
    def make_group(self, app: str, name: str, limit: int) -> str: raise NotImplementedError
    def public_link(self, group: str) -> str | None: raise NotImplementedError


def whats_new(number: int, notes: str) -> str:
    """The build's "What you will notice" lines from docs/RELEASE-NOTES.md, or a pointer to the release notes where it has none."""
    section = re.search(rf"^## [^\n]*nightly\.{number}\n(.*?)(?=^## |\Z)", notes, re.S | re.M)
    lines: list[str] = []
    if section:
        noticed = re.search(r"\*\*What you will notice\*\*\n(.*?)(?=\n\*\*|\n\[|\Z)", section.group(1), re.S)
        if noticed:
            lines = [line[2:].strip() for line in noticed.group(1).splitlines() if line.startswith("- ")]
    text = "\n".join("- " + line for line in lines) if lines else f"Nightly {number}."
    text += "\n\nEverything that changed: https://grouplab.org/releases/"
    return text[:3900]


def step(store: Store, notes: Callable[[int], str], wait_minutes: int = 0, sleep: Callable[[float], None] = time.sleep
         ) -> tuple[list[str], str | None]:
    """One step forward. Returns the lines for the summary and, where Alan should know, the line for docs/notes/for-alan.md."""
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"TestFlight: App Store Connect has no app {BUNDLE_ID}, so nothing was distributed."], None
    groups = store.groups(app)
    missing = [name for name in (TEAM, BETA) if name not in groups]
    if missing:
        return [f"TestFlight: there is no group named {' or '.join(repr(m) for m in missing)} yet, so this step distributed nothing "
                "(entry 310: Alan creates both in App Store Connect)."], None
    team, beta = groups[TEAM], groups[BETA]

    lines: list[str] = []
    builds = store.builds(app)
    waited = 0
    while builds and builds[0].processing == "PROCESSING" and waited < wait_minutes:
        sleep(60)
        waited += 1
        builds = store.builds(app)
    if builds and builds[0].processing == "PROCESSING":
        lines.append(f"Build {builds[0].number} is still being processed by Apple; the next run carries it on.")
    usable = [b for b in builds if b.processing == "VALID" and not b.expired]
    if not usable:
        return lines + ["TestFlight: no build Apple has finished processing yet."], None

    newest = usable[0]
    in_beta, in_team = store.in_group(beta), store.in_group(team)

    # Entry 319 section 1: Public Beta's first review is never disturbed. Until a build of it has been approved, a build already waiting
    # for review keeps its place in Apple's queue: nothing newer is added or submitted, and GroupLab Team keeps its own automatic
    # distribution, which Alan left on for that time.
    if not any(b.id in in_beta and store.external_state(b.id) in AVAILABLE for b in usable):
        # The review's own state, or the build's where the review cannot be read: either says it is still with Apple.
        waiting = next((b for b in usable if b.id in in_beta
                        and (store.review(b.id) in PENDING or store.external_state(b.id) in WAITING_STATES)), None)
        if waiting is not None:
            said = (store.review(waiting.id) or store.external_state(waiting.id)).replace("_", " ").lower()
            lines.append(f"{BETA}'s first build, {waiting.number}, is {said}; nothing newer is added or submitted until Apple approves it, "
                         "so it keeps its place in the queue (entry 319).")
            return lines, None
    if store.external_state(newest.id) == "MISSING_EXPORT_COMPLIANCE":
        store.no_encryption(newest.id)
        lines.append(f"Build {newest.number}: answered the export question (GroupLab uses only the system's encryption).")
    if newest.id not in in_beta:
        store.what_to_test(newest.id, notes(newest.number))
        store.add(beta, newest.id)
        in_beta.add(newest.id)
        lines.append(f"Build {newest.number} added to {BETA}, with its release notes as What to Test.")
    if store.external_state(newest.id) == "READY_FOR_BETA_SUBMISSION" and store.review(newest.id) is None:
        try:
            store.submit(newest.id)
            lines.append(f"Build {newest.number} submitted for Beta App Review.")
        except Refused as said:
            lines.append(f"Build {newest.number} not submitted yet: Apple said {said}. The next run tries again.")

    for_alan = None
    state, review = store.external_state(newest.id), store.review(newest.id)
    if state == "BETA_REJECTED" or review == "REJECTED":
        for_alan = (f"Apple's beta review turned down build {newest.number}. Both TestFlight groups stay where they were; "
                    f"App Store Connect, TestFlight, build {newest.number} says why.")
        lines.append(for_alan)
    elif state not in AVAILABLE:
        lines.append(f"Build {newest.number} for {BETA}: {state.replace('_', ' ').lower()}"
                     + (f", review {review.replace('_', ' ').lower()}" if review else "") + ".")

    shared = next((b for b in usable if b.id in in_beta and store.external_state(b.id) in AVAILABLE), None)
    if shared is None:
        lines.append(f"{BETA} has no build its testers can install yet, so {TEAM} is not moved either.")
        return lines, for_alan
    if shared.id not in in_team:
        store.add(team, shared.id)
        in_team.add(shared.id)
        lines.append(f"{TEAM} moved to build {shared.number}, the build {BETA} has.")
    else:
        lines.append(f"Both groups have build {shared.number}.")
    ahead = [b.number for b in usable if b.id in in_team and b.number > shared.number]
    if ahead:
        lines.append(f"{TEAM} also has build {', '.join(map(str, ahead))}, newer than {BETA}'s; it was added by hand and is left there.")
    return lines, for_alan


def mac_internal(store: Store, number: int, wait_minutes: int = 0, sleep: Callable[[float], None] = time.sleep) -> tuple[list[str], bool]:
    """Entry 501: the Mac App Store build named, to GroupLab Team and nowhere else. Returns the lines and whether the team has it."""
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"TestFlight for Mac: App Store Connect has no app {BUNDLE_ID}."], False
    groups = store.groups(app)
    if TEAM not in groups:
        return [f"TestFlight for Mac: there is no group named {TEAM!r}, so the Mac build was given to nobody."], False
    team = groups[TEAM]

    def find() -> Build | None:
        return next((b for b in store.builds(app, "MAC_OS") if b.number == number), None)

    build, waited = find(), 0
    while (build is None or build.processing == "PROCESSING") and waited < wait_minutes:
        sleep(60)
        waited += 1
        build = find()
    if build is None:
        return [f"TestFlight for Mac: Apple does not list Mac build {number} yet; run this again later."], False
    if build.processing == "PROCESSING":
        return [f"TestFlight for Mac: Apple is still processing Mac build {number}; run this again later."], False
    if build.processing != "VALID":
        return [f"TestFlight for Mac: Apple found Mac build {number} {build.processing.lower()}; App Store Connect, TestFlight, macOS says why."], False
    lines: list[str] = []
    if store.external_state(build.id) == "MISSING_EXPORT_COMPLIANCE":
        store.no_encryption(build.id)
        lines.append(f"Mac build {number}: answered the export question (GroupLab uses only the system's encryption).")
    if build.id in store.in_group(team):
        lines.append(f"{TEAM} already has Mac build {number}.")
    else:
        store.add(team, build.id)
        lines.append(f"Mac build {number} given to {TEAM} (internal testers only; nothing submitted for review).")
    return lines, True


def mac_testers(store: Store, notes: Callable[[int], str]) -> tuple[list[str], str | None]:
    """Entry 504 section 3: the external group Mac testers, Mac App Store builds only, with a public link for at most five testers.

    Made once where it does not exist. The newest Mac build Apple has finished goes in with its What to Test and is submitted for Beta App
    Review; while a Mac build of the group is still with Apple's review nothing newer is added, so it keeps its place in the queue (as entry
    319 does for Public Beta). An iPhone or iPad build never goes in, and nothing here touches Public Beta or an App Store version. Returns
    the lines and, once testers can install a build, the public link. Anybody with the link may join, up to five, and this repository is
    public, so the link is returned, never printed.
    """
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"{MAC_TESTERS}: App Store Connect has no app {BUNDLE_ID}."], None
    lines: list[str] = []
    groups = store.groups(app)
    if MAC_TESTERS not in groups:
        groups[MAC_TESTERS] = store.make_group(app, MAC_TESTERS, MAC_TESTERS_LIMIT)
        lines.append(f"Made the external group {MAC_TESTERS}, with a public link for at most {MAC_TESTERS_LIMIT} testers.")
    group = groups[MAC_TESTERS]
    usable = [b for b in store.builds(app, "MAC_OS") if b.processing == "VALID" and not b.expired]
    if not usable:
        return lines + [f"{MAC_TESTERS}: no Mac build Apple has finished processing yet."], None
    newest = usable[0]
    members = store.in_group(group)
    waiting = next((b for b in usable if b.id in members
                    and (store.review(b.id) in PENDING or store.external_state(b.id) in WAITING_STATES)), None)
    if waiting is not None and waiting.id != newest.id:
        lines.append(f"{MAC_TESTERS}: Mac build {waiting.number} is with Apple's beta review; nothing newer is added until it is decided.")
    else:
        if store.external_state(newest.id) == "MISSING_EXPORT_COMPLIANCE":
            store.no_encryption(newest.id)
            lines.append(f"Mac build {newest.number}: answered the export question (GroupLab uses only the system's encryption).")
        if newest.id not in members:
            store.what_to_test(newest.id, notes(newest.number))
            store.add(group, newest.id)
            members.add(newest.id)
            lines.append(f"Mac build {newest.number} added to {MAC_TESTERS}, with its release notes as What to Test.")
        if store.external_state(newest.id) == "READY_FOR_BETA_SUBMISSION" and store.review(newest.id) is None:
            try:
                store.submit(newest.id)
                lines.append(f"Mac build {newest.number} submitted for Beta App Review, for {MAC_TESTERS} only.")
            except Refused as said:
                lines.append(f"Mac build {newest.number} not submitted yet: Apple said {said}. The next run tries again.")
        state, review = store.external_state(newest.id), store.review(newest.id)
        if state == "BETA_REJECTED" or review == "REJECTED":
            lines.append(f"Apple's beta review turned down Mac build {newest.number}; App Store Connect, TestFlight, macOS says why.")
        elif state not in AVAILABLE:
            lines.append(f"Mac build {newest.number} for {MAC_TESTERS}: {state.replace('_', ' ').lower()}"
                         + (f", review {review.replace('_', ' ').lower()}" if review else "") + ".")
    shared = next((b for b in usable if b.id in members and store.external_state(b.id) in AVAILABLE), None)
    if shared is None:
        return lines + [f"{MAC_TESTERS} has no Mac build its testers can install yet."], None
    link = store.public_link(group)
    if not link:
        return lines + [f"{MAC_TESTERS} can install Mac build {shared.number}, but the group has no public link turned on."], None
    # The repository and its Actions logs are public, and five places are meant for Alan's friend, so the link itself is never printed.
    lines.append(f"{MAC_TESTERS} can install Mac build {shared.number}; its public link is on (App Store Connect, TestFlight, "
                 f"{MAC_TESTERS}, Public Link), and is kept out of this public log.")
    return lines, link


def app_store(store: Store) -> tuple[list[str], str | None]:
    """Entry 491: the newest App Store version's review state, read only. Returns the lines and, where Alan should know, the line for
    docs/notes/for-alan.md: a rejection the same day, and an approval because release is manual and needs his word."""
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"App Store: App Store Connect has no app {BUNDLE_ID}."], None
    versions = sorted(store.versions(app), key=lambda v: v.created, reverse=True)
    if not versions:
        return ["App Store: no iOS version yet."], None
    newest = versions[0]
    built = f" (build {newest.build})" if newest.build else ""
    lines = [f"App Store version {newest.number}{built}: {VERSION_WORDS.get(newest.state, newest.state.replace('_', ' ').lower())}."]
    live = next((v for v in versions if v.state in RELEASED), None)
    if live:
        lines.append(f"The App Store page: https://apps.apple.com/app/id{app}")
    if newest.state not in RELEASED:
        lines.append(f"On the App Store now: {'version ' + live.number if live else 'nothing yet'}; TestFlight stays the way in.")
    for_alan = None
    if newest.state in TURNED_DOWN:
        for_alan = (f"Apple's App Review turned down GroupLab {newest.number}{built}. Apple's own words are under App Review in App Store "
                    "Connect, which this check cannot read; the planning session copies them into for-alan.md. TestFlight goes on as before.")
    elif newest.state == "PENDING_DEVELOPER_RELEASE":
        for_alan = (f"Apple approved GroupLab {newest.number}{built} for the App Store. Release is manual: nothing is public until Alan "
                    "says to release it, through the planning session.")
    if for_alan:
        lines.append(for_alan)
    return lines, for_alan


def release(store: Store, number: str, build: int) -> tuple[list[str], bool]:
    """Alan, 2026-11-17: the approved version named, with the build named, released on the App Store; nothing else is ever released.
    Returns the lines and whether the version is now released or releasing."""
    app = store.app(BUNDLE_ID)
    if app is None:
        return [f"App Store: App Store Connect has no app {BUNDLE_ID}; nothing was released."], False
    match = [v for v in store.versions(app) if v.number == number]
    if not match:
        return [f"App Store: there is no version {number}; nothing was released."], False
    version = match[0]
    if version.state in RELEASED or version.state == "PROCESSING_FOR_DISTRIBUTION":
        return [f"App Store version {number} (build {version.build}) is already released: {version.state}."], True
    if version.build != build:
        return [f"App Store version {number} carries build {version.build}, not {build}; nothing was released."], False
    if version.state != "PENDING_DEVELOPER_RELEASE":
        return [f"App Store version {number} (build {build}) is {version.state}, not waiting for release; nothing was released."], False
    store.release(version.id)
    after = next((v for v in store.versions(app) if v.number == number), version)
    words = VERSION_WORDS.get(after.state, after.state.replace("_", " ").lower())
    return [f"App Store version {number} (build {build}): released by hand on Alan's word; Apple now says {after.state} ({words})."], True


class Apple(Store):
    """App Store Connect's own API, signed with the key the nightly already uses to upload."""

    def __init__(self, issuer: str, key_id: str, key: str):
        import jwt  # PyJWT with its crypto extra, installed by the workflow
        now = int(time.time())
        self.token = jwt.encode({"iss": issuer, "iat": now, "exp": now + 1100, "aud": "appstoreconnect-v1"}, key,
                                algorithm="ES256", headers={"kid": key_id, "typ": "JWT"})

    def call(self, method: str, path: str, body: dict | None = None) -> dict:
        request = urllib.request.Request(API + path, method=method, data=json.dumps(body).encode() if body is not None else None,
                                         headers={"Authorization": "Bearer " + self.token, "Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(request, timeout=60) as answer:
                text = answer.read()
                return json.loads(text) if text else {}
        except urllib.error.HTTPError as error:
            try:
                said = "; ".join(e.get("detail") or e.get("title", "") for e in json.loads(error.read()).get("errors", []))
            except ValueError:
                said = ""
            raise Refused(f"{error.code} {said}".strip()) from None

    def app(self, bundle):
        found = self.call("GET", f"/v1/apps?filter[bundleId]={bundle}&limit=1").get("data", [])
        return found[0]["id"] if found else None

    def groups(self, app):
        return {g["attributes"]["name"]: g["id"] for g in self.call("GET", f"/v1/apps/{app}/betaGroups?limit=200").get("data", [])}

    def builds(self, app, platform="IOS"):
        found = []
        # Entry 501: one platform's builds only; the Mac App Store copy's share the record and its numbers.
        for b in self.call("GET", f"/v1/builds?filter[app]={app}&filter[preReleaseVersion.platform]={platform}"
                                  "&sort=-uploadedDate&limit=50").get("data", []):
            a = b["attributes"]
            found.append(Build(b["id"], int(re.sub(r"\D", "", a.get("version", "0")) or 0), a.get("processingState", ""), a.get("expired", False)))
        return sorted(found, key=lambda b: b.number, reverse=True)

    def in_group(self, group):
        return {b["id"] for b in self.call("GET", f"/v1/betaGroups/{group}/builds?limit=200").get("data", [])}

    def add(self, group, build):
        self.call("POST", f"/v1/betaGroups/{group}/relationships/builds", {"data": [{"type": "builds", "id": build}]})

    def external_state(self, build):
        return self.call("GET", f"/v1/builds/{build}/buildBetaDetail")["data"]["attributes"].get("externalBuildState", "")

    def review(self, build):
        found = self.call("GET", f"/v1/betaAppReviewSubmissions?filter[build]={build}").get("data", [])
        return found[0]["attributes"].get("betaReviewState") if found else None

    def submit(self, build):
        self.call("POST", "/v1/betaAppReviewSubmissions",
                  {"data": {"type": "betaAppReviewSubmissions", "relationships": {"build": {"data": {"type": "builds", "id": build}}}}})

    def no_encryption(self, build):
        self.call("PATCH", f"/v1/builds/{build}",
                  {"data": {"type": "builds", "id": build, "attributes": {"usesNonExemptEncryption": False}}})

    def what_to_test(self, build, text):
        existing = self.call("GET", f"/v1/builds/{build}/betaBuildLocalizations").get("data", [])
        english = next((l for l in existing if l["attributes"].get("locale", "").startswith("en")), None)
        if english:
            self.call("PATCH", f"/v1/betaBuildLocalizations/{english['id']}",
                      {"data": {"type": "betaBuildLocalizations", "id": english["id"], "attributes": {"whatsNew": text}}})
        else:
            self.call("POST", "/v1/betaBuildLocalizations",
                      {"data": {"type": "betaBuildLocalizations", "attributes": {"locale": "en-US", "whatsNew": text},
                                "relationships": {"build": {"data": {"type": "builds", "id": build}}}}})

    def versions(self, app):
        answer = self.call("GET", f"/v1/apps/{app}/appStoreVersions?filter[platform]=IOS&include=build&limit=10")
        builds = {b["id"]: b["attributes"].get("version", "") for b in answer.get("included", []) if b.get("type") == "builds"}
        found = []
        for v in answer.get("data", []):
            a = v["attributes"]
            link = ((v.get("relationships") or {}).get("build") or {}).get("data") or {}
            number = re.sub(r"\D", "", builds.get(link.get("id", ""), ""))
            found.append(Version(a.get("versionString", ""), a.get("appVersionState") or a.get("appStoreState") or "",
                                 a.get("createdDate", ""), int(number) if number else None, v.get("id", "")))
        return found

    def make_group(self, app, name, limit):
        made = self.call("POST", "/v1/betaGroups", {"data": {
            "type": "betaGroups",
            "attributes": {"name": name, "publicLinkEnabled": True, "publicLinkLimitEnabled": True, "publicLinkLimit": limit,
                           "feedbackEnabled": True},
            "relationships": {"app": {"data": {"type": "apps", "id": app}}}}})
        return made["data"]["id"]

    def public_link(self, group):
        a = self.call("GET", f"/v1/betaGroups/{group}")["data"]["attributes"]
        return a.get("publicLink") if a.get("publicLinkEnabled") else None

    def release(self, version):
        self.call("POST", "/v1/appStoreVersionReleaseRequests",
                  {"data": {"type": "appStoreVersionReleaseRequests",
                            "relationships": {"appStoreVersion": {"data": {"type": "appStoreVersions", "id": version}}}}})


def key_text(value: str) -> str:
    """The .p8 as the nightly takes it: the PEM itself, or the PEM in base64."""
    return value if "BEGIN PRIVATE KEY" in value else base64.b64decode(value).decode()


@dataclass
class Pretend(Store):
    """A made-up App Store Connect for the self-test."""
    builds_: list[Build] = field(default_factory=list)
    states: dict[str, str] = field(default_factory=dict)
    reviews: dict[str, str] = field(default_factory=dict)
    members: dict[str, set[str]] = field(default_factory=lambda: {"team": set(), "beta": set()})
    names: dict[str, str] = field(default_factory=lambda: {TEAM: "team", BETA: "beta"})
    refuse_submit: str | None = None
    needs_review: bool = True
    notes: dict[str, str] = field(default_factory=dict)
    versions_: list[Version] = field(default_factory=list)
    mac_builds: list[Build] = field(default_factory=list)

    def versions(self, app): return list(self.versions_)

    def release(self, version):
        for v in self.versions_:
            if v.id == version:
                v.state = "PROCESSING_FOR_DISTRIBUTION"
    def app(self, bundle): return "app" if bundle == BUNDLE_ID else None
    def groups(self, app): return dict(self.names)
    def builds(self, app, platform="IOS"):
        return sorted(self.builds_ if platform == "IOS" else self.mac_builds, key=lambda b: b.number, reverse=True)
    def in_group(self, group): return set(self.members[group])
    def external_state(self, build): return self.states.get(build, "READY_FOR_BETA_SUBMISSION")
    def review(self, build): return self.reviews.get(build)
    def no_encryption(self, build): self.states[build] = "READY_FOR_BETA_SUBMISSION"
    def what_to_test(self, build, text): self.notes[build] = text
    links: dict[str, str] = field(default_factory=dict)
    made: list[tuple[str, int]] = field(default_factory=list)

    def make_group(self, app, name, limit):
        self.names[name] = "mac"
        self.members["mac"] = set()
        self.links["mac"] = "https://testflight.apple.com/join/MADEUP"
        self.made.append((name, limit))
        return "mac"

    def public_link(self, group): return self.links.get(group)

    def add(self, group, build):
        self.members[group].add(build)
        if group == "beta" and not self.needs_review:
            self.states[build] = "IN_BETA_TESTING"

    def submit(self, build):
        if self.refuse_submit:
            raise Refused(self.refuse_submit)
        self.reviews[build] = "WAITING_FOR_REVIEW"
        self.states[build] = "WAITING_FOR_BETA_REVIEW"


def self_test() -> int:
    failures: list[str] = []

    def expect(what: str, condition: bool) -> None:
        if not condition:
            failures.append(what)

    no_notes = lambda n: f"Nightly {n}."

    # No groups yet: one line, nothing touched.
    s = Pretend(builds_=[Build("b134", 134, "VALID")], names={})
    lines, alan = step(s, no_notes)
    expect("missing groups say so", "no group named" in lines[0] and alan is None and not s.members["beta"])

    # The newest build goes to Public Beta and is submitted; the team is not moved while Public Beta has nothing installable.
    s = Pretend(builds_=[Build("b134", 134, "VALID")])
    lines, alan = step(s, no_notes)
    expect("newest into Public Beta", "b134" in s.members["beta"] and s.reviews.get("b134") == "WAITING_FOR_REVIEW")
    expect("team not ahead", not s.members["team"] and alan is None)
    expect("notes given", s.notes.get("b134") == "Nightly 134.")

    # Run again while waiting: nothing is done twice.
    before = (set(s.members["beta"]), dict(s.reviews))
    step(s, no_notes)
    expect("idempotent", (s.members["beta"], s.reviews) == before)

    # Approved: the team gets the same build.
    s.states["b134"] = "BETA_APPROVED"
    lines, _ = step(s, no_notes)
    expect("lockstep on approval", "b134" in s.members["team"])

    # A newer build waiting for review leaves both groups on the approved one.
    s.builds_.append(Build("b135", 135, "VALID"))
    lines, _ = step(s, no_notes)
    expect("newer waits", "b135" in s.members["beta"] and "b135" not in s.members["team"] and "b134" in s.members["team"])
    expect("says both", any("Both groups have build 134" in line for line in lines))

    # Rejected: nothing moves, and Alan hears of it.
    s.states["b135"] = "BETA_REJECTED"
    lines, alan = step(s, no_notes)
    expect("rejection moves nothing", "b135" not in s.members["team"] and alan and "turned down build 135" in alan)

    # Where Apple needs no review, both move at once.
    s = Pretend(builds_=[Build("b136", 136, "VALID")], needs_review=False)
    step(s, no_notes)
    expect("no review: both at once", "b136" in s.members["beta"] and "b136" in s.members["team"])

    # Still processing: said, waited on, and nothing else done with it.
    s = Pretend(builds_=[Build("b137", 137, "PROCESSING")])
    slept: list[float] = []
    lines, _ = step(s, no_notes, wait_minutes=2, sleep=slept.append)
    expect("processing waited", len(slept) == 2 and any("still being processed" in line for line in lines) and not s.members["beta"])

    # Apple refuses a submission (another in review): said, and tried again next time.
    s = Pretend(builds_=[Build("b138", 138, "VALID")], refuse_submit="409 another build is in review")
    lines, _ = step(s, no_notes)
    expect("refusal said", any("not submitted yet" in line and "409" in line for line in lines))

    # A build added to the team by hand is left there and named.
    s = Pretend(builds_=[Build("b139", 139, "VALID"), Build("b140", 140, "VALID")], states={"b139": "IN_BETA_TESTING"})
    s.members["beta"].add("b139")
    s.members["team"].update({"b139", "b140"})
    lines, _ = step(s, no_notes)
    expect("hand-added left", "b140" in s.members["team"] and any("added by hand" in line for line in lines))

    # The export question, where Apple asks it, is answered.
    s = Pretend(builds_=[Build("b141", 141, "VALID")], states={"b141": "MISSING_EXPORT_COMPLIANCE"})
    lines, _ = step(s, no_notes)
    expect("export answered", any("export question" in line for line in lines) and s.reviews.get("b141"))

    # Entry 319: Public Beta's first build waiting for review is never superseded; nothing newer is added or submitted.
    s = Pretend(builds_=[Build("b134", 134, "VALID"), Build("b140", 140, "VALID")],
                states={"b134": "WAITING_FOR_BETA_REVIEW"}, reviews={"b134": "WAITING_FOR_REVIEW"})
    s.members["beta"].add("b134")
    lines, alan = step(s, no_notes)
    expect("first review held", "b140" not in s.members["beta"] and "b140" not in s.reviews and not s.members["team"] and alan is None)
    expect("first review said", any("first build, 134" in line and "entry 319" in line for line in lines))
    # The same where only the build's state says it waits (its review record not readable).
    s2 = Pretend(builds_=[Build("b134", 134, "VALID"), Build("b140", 140, "VALID")], states={"b134": "WAITING_FOR_BETA_REVIEW"})
    s2.members["beta"].add("b134")
    step(s2, no_notes)
    expect("first review held by state", "b140" not in s2.members["beta"] and "b140" not in s2.reviews)
    # Once approved, the newer build goes on as before.
    s.states["b134"] = "BETA_APPROVED"
    s.reviews["b134"] = "APPROVED"
    step(s, no_notes)
    expect("after approval, on", "b140" in s.members["beta"] and "b134" in s.members["team"])

    # Entry 501: a Mac build is never taken for the newest iPhone and iPad build, and goes to GroupLab Team only.
    s = Pretend(builds_=[Build("b233", 233, "VALID")], mac_builds=[Build("m233", 233, "VALID"), Build("m234", 234, "VALID")],
                states={"b233": "IN_BETA_TESTING"})
    s.members["beta"].add("b233")
    step(s, no_notes)
    expect("mac builds ignored by the step", not {"m233", "m234"} & (s.members["beta"] | s.members["team"] | set(s.reviews)))
    lines, done = mac_internal(s, 233)
    expect("mac to team only", done and "m233" in s.members["team"] and "m233" not in s.members["beta"] and "m233" not in s.reviews)
    lines, done = mac_internal(s, 233)
    expect("mac once", done and "already has" in lines[0])
    s.mac_builds = [Build("m235", 235, "PROCESSING")]
    slept = []
    lines, done = mac_internal(s, 235, wait_minutes=3, sleep=slept.append)
    expect("mac processing waited", not done and len(slept) == 3 and "still processing" in lines[0] and "m235" not in s.members["team"])
    lines, done = mac_internal(s, 236)
    expect("mac missing said", not done and "does not list" in lines[0])
    s.mac_builds = [Build("m237", 237, "INVALID")]
    lines, done = mac_internal(s, 237)
    expect("mac invalid said", not done and "invalid" in lines[0] and "m237" not in s.members["team"])

    # Entry 504 section 3: Mac testers is made once, holds Mac builds only, and is submitted for review; nothing reaches Public Beta.
    s = Pretend(builds_=[Build("b240", 240, "VALID")], mac_builds=[Build("m240", 240, "VALID"), Build("m239", 239, "VALID")],
                states={"b240": "IN_BETA_TESTING"})
    s.members["beta"].add("b240")
    lines, link = mac_testers(s, no_notes)
    expect("mac testers made once with five", s.made == [(MAC_TESTERS, 5)] and "Made the external group" in lines[0])
    expect("newest mac build in and submitted", s.members["mac"] == {"m240"} and s.reviews.get("m240") == "WAITING_FOR_REVIEW"
           and "m240" in s.notes and link is None)
    expect("no iPhone build for mac testers", "b240" not in s.members["mac"])
    step(s, no_notes)
    expect("no mac build for public beta", not {"m239", "m240"} & (s.members["beta"] | s.members["team"]))
    mac_testers(s, no_notes)
    expect("mac testers made only once", len(s.made) == 1)
    s.mac_builds.insert(0, Build("m241", 241, "VALID"))
    lines, link = mac_testers(s, no_notes)
    expect("mac review held", "m241" not in s.members["mac"] and "m241" not in s.reviews and link is None
           and any("with Apple's beta review" in l for l in lines))
    s.states["m240"], s.reviews["m240"] = "BETA_APPROVED", "APPROVED"
    lines, link = mac_testers(s, no_notes)
    expect("after approval the link, and the next build on", link == "https://testflight.apple.com/join/MADEUP"
           and "m241" in s.members["mac"] and "240" in lines[-1] and not any(link in l for l in lines))

    # What to Test from the release notes.
    notes = ("## 0.2.0-nightly.142\n\n**2026-10-01**\n\n**What you will notice**\n\n- First thing.\n- Second thing.\n\n"
             "**Under the hood**\n\n- Hidden.\n\n## 0.2.0-nightly.141\n")
    text = whats_new(142, notes)
    expect("notes read", text.startswith("- First thing.\n- Second thing.") and "Hidden" not in text)
    expect("notes fallback", whats_new(999, notes).startswith("Nightly 999."))

    # Entry 491: the App Store version's review, read only.
    lines, alan = app_store(Pretend())
    expect("no version yet", lines == ["App Store: no iOS version yet."] and alan is None)
    s = Pretend(versions_=[Version("1.0", "WAITING_FOR_REVIEW", "2026-11-15", 226)])
    lines, alan = app_store(s)
    expect("waiting said", lines[0] == "App Store version 1.0 (build 226): waiting for review." and alan is None
           and "nothing yet" in lines[1])
    s.versions_[0].state = "REJECTED"
    lines, alan = app_store(s)
    expect("a rejection goes to Alan", alan is not None and "turned down" in alan and alan in lines)
    s.versions_[0].state = "PENDING_DEVELOPER_RELEASE"
    lines, alan = app_store(s)
    expect("an approval goes to Alan", alan is not None and "Release is manual" in alan)
    s = Pretend(versions_=[Version("1.0", "READY_FOR_SALE", "2026-11-15", 226), Version("1.1", "IN_REVIEW", "2026-12-01", 240)])
    lines, alan = app_store(s)
    expect("the live one named", lines[0].startswith("App Store version 1.1 (build 240): in review") and "version 1.0" in lines[2])
    expect("its page linked", lines[1] == "The App Store page: https://apps.apple.com/app/idapp")
    # Reading the versions moves nothing in TestFlight and nothing in the version.
    expect("read only", s.versions_[1].state == "IN_REVIEW" and not s.members["beta"] and not s.reviews)

    # Alan, 2026-11-17: only the approved version named, with the build named, is ever released.
    s = Pretend(versions_=[Version("1.0", "WAITING_FOR_REVIEW", "2026-11-15", 226, "v1")])
    lines, done = release(s, "1.0", 226)
    expect("nothing released in review", not done and s.versions_[0].state == "WAITING_FOR_REVIEW" and "nothing was released" in lines[0])
    s.versions_[0].state = "PENDING_DEVELOPER_RELEASE"
    lines, done = release(s, "1.0", 227)
    expect("nothing released for another build", not done and s.versions_[0].state == "PENDING_DEVELOPER_RELEASE")
    lines, done = release(s, "1.1", 226)
    expect("nothing released for another version", not done and s.versions_[0].state == "PENDING_DEVELOPER_RELEASE")
    lines, done = release(s, "1.0", 226)
    expect("the approved one released", done and s.versions_[0].state == "PROCESSING_FOR_DISTRIBUTION" and "released by hand" in lines[0])
    lines, done = release(s, "1.0", 226)
    expect("released once", done and "already released" in lines[0])

    for failure in failures:
        print("FAILED:", failure)
    print(f"testflight.py self-test: {'failed' if failures else 'passed'}")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] == ["--app-store"]:
        return main_app_store()
    if argv[1:2] == ["--mac-internal"] and len(argv) >= 3 and argv[2].isdigit():
        return main_mac_internal(int(argv[2]), int(argv[argv.index("--wait-minutes") + 1]) if "--wait-minutes" in argv else 0)
    if argv[1:2] == ["--release"] and len(argv) == 4 and argv[3].isdigit():
        return main_release(argv[2], int(argv[3]))
    if argv[1:2] != ["--run"]:
        print(__doc__)
        return 2
    wait = int(argv[argv.index("--wait-minutes") + 1]) if "--wait-minutes" in argv else 0
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        lines, alan = ["TestFlight: the App Store Connect key is not set, so this step distributed nothing."], None
    else:
        notes = NOTES.read_text(encoding="utf-8") if NOTES.is_file() else ""
        store = Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"], key_text(os.environ["APPLE_API_KEY_P8"]))
        try:
            lines, alan = step(store, lambda n: whats_new(n, notes), wait)
        except Refused as said:
            lines, alan = [f"TestFlight: App Store Connect refused a request ({said}); nothing further was done this run."], None
        # Entry 504 section 3: the Mac testers group after the iPhone and iPad groups, on its own, so a refusal here moves nothing there.
        try:
            lines += mac_testers(store, lambda n: whats_new(n, notes))[0]
        except Refused as said:
            lines.append(f"{MAC_TESTERS}: App Store Connect refused a request ({said}); the next run tries again.")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(summary, "a", encoding="utf-8") if summary else open(os.devnull, "w") as out:
        out.write("### TestFlight\n\n" + "".join(f"- {line}\n" for line in lines))
    for line in lines:
        print(line)
    if alan:
        print(f"::warning::{alan}")
    return 0


def main_app_store() -> int:
    """Entry 491: the App Store version's state, printed for status-note.py; never fails the run."""
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        lines, alan = ["App Store: the App Store Connect key is not set, so the review was not read."], None
    else:
        try:
            store = Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"], key_text(os.environ["APPLE_API_KEY_P8"]))
            lines, alan = app_store(store)
        except Refused as said:
            lines, alan = [f"App Store: App Store Connect refused the read ({said})."], None
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(summary, "a", encoding="utf-8") if summary else open(os.devnull, "w") as out:
        out.write("### The App Store version\n\n" + "".join(f"- {line}\n" for line in lines))
    for line in lines:
        print(line)
    if alan:
        print(f"::warning::{alan}")
    return 0


def main_mac_internal(number: int, wait: int) -> int:
    """Entry 501: fails the run when GroupLab Team was not given the Mac build, so nobody takes it for done."""
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        lines, done = ["TestFlight for Mac: the App Store Connect key is not set, so the Mac build was given to nobody."], False
    else:
        try:
            store = Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"], key_text(os.environ["APPLE_API_KEY_P8"]))
            lines, done = mac_internal(store, number, wait)
        except Refused as said:
            lines, done = [f"TestFlight for Mac: App Store Connect refused a request ({said})."], False
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(summary, "a", encoding="utf-8") if summary else open(os.devnull, "w") as out:
        out.write("### TestFlight for Mac\n\n" + "".join(f"- {line}\n" for line in lines))
    for line in lines:
        print(line)
    return 0 if done else 1


def main_release(number: str, build: int) -> int:
    """Alan, 2026-11-17: release the approved version named. Fails the run when it released nothing, so nobody takes it for done."""
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        lines, done = ["App Store: the App Store Connect key is not set, so nothing was released."], False
    else:
        try:
            store = Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"], key_text(os.environ["APPLE_API_KEY_P8"]))
            lines, done = release(store, number, build)
        except Refused as said:
            lines, done = [f"App Store: App Store Connect refused the release ({said})."], False
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(summary, "a", encoding="utf-8") if summary else open(os.devnull, "w") as out:
        out.write("### The App Store release\n\n" + "".join(f"- {line}\n" for line in lines))
    for line in lines:
        print(line)
    return 0 if done else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
