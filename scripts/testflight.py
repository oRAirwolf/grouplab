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

    python3 scripts/testflight.py --run [--wait-minutes N]   one step, with the key from APPLE_API_ISSUER_ID, APPLE_API_KEY_ID, APPLE_API_KEY_P8
    python3 scripts/testflight.py --self-test                  every case against a made-up App Store Connect, no network

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


class Store:
    """What the steps need from App Store Connect. The real one talks to Apple; the self-test's keeps everything in memory."""

    def app(self, bundle: str) -> str | None: raise NotImplementedError
    def groups(self, app: str) -> dict[str, str]: raise NotImplementedError
    def builds(self, app: str) -> list[Build]: raise NotImplementedError
    def in_group(self, group: str) -> set[str]: raise NotImplementedError
    def add(self, group: str, build: str) -> None: raise NotImplementedError
    def external_state(self, build: str) -> str: raise NotImplementedError
    def review(self, build: str) -> str | None: raise NotImplementedError
    def submit(self, build: str) -> None: raise NotImplementedError
    def no_encryption(self, build: str) -> None: raise NotImplementedError
    def what_to_test(self, build: str, text: str) -> None: raise NotImplementedError


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

    def builds(self, app):
        found = []
        for b in self.call("GET", f"/v1/builds?filter[app]={app}&sort=-uploadedDate&limit=50").get("data", []):
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

    def app(self, bundle): return "app" if bundle == BUNDLE_ID else None
    def groups(self, app): return dict(self.names)
    def builds(self, app): return sorted(self.builds_, key=lambda b: b.number, reverse=True)
    def in_group(self, group): return set(self.members[group])
    def external_state(self, build): return self.states.get(build, "READY_FOR_BETA_SUBMISSION")
    def review(self, build): return self.reviews.get(build)
    def no_encryption(self, build): self.states[build] = "READY_FOR_BETA_SUBMISSION"
    def what_to_test(self, build, text): self.notes[build] = text

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

    # What to Test from the release notes.
    notes = ("## 0.2.0-nightly.142\n\n**2026-10-01**\n\n**What you will notice**\n\n- First thing.\n- Second thing.\n\n"
             "**Under the hood**\n\n- Hidden.\n\n## 0.2.0-nightly.141\n")
    text = whats_new(142, notes)
    expect("notes read", text.startswith("- First thing.\n- Second thing.") and "Hidden" not in text)
    expect("notes fallback", whats_new(999, notes).startswith("Nightly 999."))

    for failure in failures:
        print("FAILED:", failure)
    print(f"testflight.py self-test: {'failed' if failures else 'passed'}")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] != ["--run"]:
        print(__doc__)
        return 2
    wait = int(argv[argv.index("--wait-minutes") + 1]) if "--wait-minutes" in argv else 0
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        lines, alan = ["TestFlight: the App Store Connect key is not set, so this step distributed nothing."], None
    else:
        notes = NOTES.read_text(encoding="utf-8") if NOTES.is_file() else ""
        try:
            store = Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"], key_text(os.environ["APPLE_API_KEY_P8"]))
            lines, alan = step(store, lambda n: whats_new(n, notes), wait)
        except Refused as said:
            lines, alan = [f"TestFlight: App Store Connect refused a request ({said}); nothing further was done this run."], None
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(summary, "a", encoding="utf-8") if summary else open(os.devnull, "w") as out:
        out.write("### TestFlight\n\n" + "".join(f"- {line}\n" for line in lines))
    for line in lines:
        print(line)
    if alan:
        print(f"::warning::{alan}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
