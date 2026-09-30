#!/usr/bin/env python3
"""TestFlight's beta feedback and crashes for GroupLab, read from App Store Connect and summarized in the run's summary.

NOTES-FROM-PLANNING.md entry 311 section 3 item 4. A tester who takes a screenshot in GroupLab and shares it as beta feedback, and every
crash a tester sends, lands in App Store Connect. This reads the ones from the last day through App Store Connect's API, with the key the
nightly already uploads with (the token is made by scripts/testflight.py), and writes one short line each to $GITHUB_STEP_SUMMARY:

- a screenshot: when, which build, the device and iOS version, and whether the tester wrote a comment. The comment itself is never
  written here: the repository is public, so a run's summary is too, and a Public Beta tester's words are theirs. It is read in App Store
  Connect, TestFlight, Feedback;
- a crash: the same, and the crash log's exception lines and GroupLab's own frames, never the whole log.

Never published: a screenshot (its address is not even asked for), a tester's email or name. The API is asked only for the fields listed in
FIELDS, so those never reach the runner. It never fails a build: no key, no app, or a refusal is one line in the summary.

    python3 scripts/testflight-feedback.py --run [--since-hours N]   with APPLE_API_ISSUER_ID, APPLE_API_KEY_ID and APPLE_API_KEY_P8
    python3 scripts/testflight-feedback.py --self-test               every rule against a made-up App Store Connect, no network
"""
from __future__ import annotations

import datetime as _dt
import importlib.util
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

HERE = Path(__file__).resolve().parent
# GroupLab, and GroupLab Dev once it has its App Store Connect record (entry 315's amendment, request 61).
BUNDLES = ("org.grouplab.app", "org.grouplab.app.dev")
KINDS = {"betaFeedbackScreenshotSubmissions": "screenshot", "betaFeedbackCrashSubmissions": "crash"}
# The only attributes asked for: no email, no tester, no screenshot address.
FIELDS = "createdDate,comment,deviceModel,osVersion,build"
MOST = 20
COMMENT = 240


def _testflight():
    """scripts/testflight.py, for its App Store Connect client, token and key reading."""
    spec = importlib.util.spec_from_file_location("testflight", HERE / "testflight.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


@dataclass
class Item:
    kind: str
    id: str
    when: _dt.datetime
    build: str
    device: str
    os: str
    comment: str


EMAIL = re.compile(r"[\w.+-]+@[\w-]+(\.[\w-]+)+")
ADDRESS = re.compile(r"\b(?:https?://|www\.)\S+", re.I)
PHONE = re.compile(r"\+?\d[\d ()-]{7,}\d")


def scrubbed(comment: str | None) -> str:
    """A tester's comment fit for a summary: on one line, without an email, a telephone number or a web address, and not too long."""
    text = " ".join((comment or "").split())
    text = EMAIL.sub("[email]", text)
    text = ADDRESS.sub("[address]", text)
    text = PHONE.sub("[number]", text)
    return text if len(text) <= COMMENT else text[:COMMENT - 3].rstrip() + "..."


def crash_lines(log: str) -> list[str]:
    """What a crash log says went wrong, in a few lines: its exception and termination lines and the first of GroupLab's own frames."""
    said = [line.strip() for line in log.splitlines()
            if re.match(r"\s*(Exception Type|Exception Reason|Exception Codes|Termination Reason)\s*:", line)]
    frames = [re.sub(r"\s+", " ", line.strip()) for line in log.splitlines() if re.match(r"\s*\d+\s+GroupLab", line)]
    return [scrubbed(line) for line in said[:4] + frames[:3]]


class Store:
    """What this needs from App Store Connect. The real one is testflight.py's client; the self-test's keeps everything in memory."""

    def app(self, bundle: str) -> str | None: raise NotImplementedError
    def submissions(self, app: str, kind: str) -> list[dict]: raise NotImplementedError
    def builds(self, included: list[dict]) -> dict[str, str]: raise NotImplementedError
    def crash_log(self, submission: str) -> str: raise NotImplementedError


def read(store: Store, since: _dt.datetime) -> tuple[list[str], int]:
    """The summary's lines and how many submissions were found since the time given."""
    lines: list[str] = []
    found = 0
    for bundle in BUNDLES:
        app = store.app(bundle)
        if app is None:
            if bundle == BUNDLES[0]:
                lines.append(f"App Store Connect has no app {bundle}, so there is no feedback to read.")
            continue
        for resource, kind in KINDS.items():
            items = []
            for data in store.submissions(app, resource):
                attributes = data.get("attributes", {})
                when = _dt.datetime.fromisoformat(attributes.get("createdDate", "1970-01-01T00:00:00+00:00").replace("Z", "+00:00"))
                if when < since:
                    continue
                build = data.get("relationships", {}).get("build", {}).get("data") or {}
                items.append(Item(kind, data["id"], when, build.get("id", ""), attributes.get("deviceModel") or "a device",
                                  attributes.get("osVersion") or "", scrubbed(attributes.get("comment"))))
            found += len(items)
            for item in sorted(items, key=lambda i: i.when, reverse=True)[:MOST]:
                number = store.builds([]).get(item.build, "?")
                line = (f"{'Screenshot' if kind == 'screenshot' else 'Crash'} {item.when:%Y-%m-%d %H:%M} UTC, "
                        f"{'GroupLab Dev' if bundle.endswith('.dev') else 'GroupLab'} build {number}, {item.device} iOS {item.os}".rstrip())
                line += ", with a comment" if item.comment else ", no comment"
                lines.append(line)
                if kind == "crash":
                    try:
                        lines.extend("  " + said for said in crash_lines(store.crash_log(item.id)))
                    except Exception as error:  # a crash log Apple has not made yet is no reason to stop
                        lines.append(f"  the crash log could not be read ({type(error).__name__})")
            if len(items) > MOST:
                lines.append(f"... and {len(items) - MOST} more {kind} submissions; App Store Connect, TestFlight, Feedback has them all.")
    if found == 0 and not any("has no app" in line for line in lines):
        lines.append("No new TestFlight feedback or crashes.")
    return lines, found


class Apple(Store):
    """App Store Connect's API through scripts/testflight.py's client."""

    def __init__(self, client):
        self.client = client
        self.numbers: dict[str, str] = {}

    def app(self, bundle):
        return self.client.app(bundle)

    def submissions(self, app, kind):
        query = f"/v1/apps/{app}/{kind}?limit=50&fields[{kind}]={FIELDS}&include=build&fields[builds]=version"
        answer = self.client.call("GET", query)
        for build in answer.get("included", []):
            if build.get("type") == "builds":
                self.numbers[build["id"]] = build.get("attributes", {}).get("version", "?")
        return answer.get("data", [])

    def builds(self, included):
        return self.numbers

    def crash_log(self, submission):
        answer = self.client.call("GET", f"/v1/betaFeedbackCrashSubmissions/{submission}/crashLog")
        return (answer.get("data") or {}).get("attributes", {}).get("logText", "")


@dataclass
class Pretend(Store):
    """A made-up App Store Connect for the self-test, with an email and a screenshot address it must never pass on."""
    apps: dict[str, str] = field(default_factory=lambda: {"org.grouplab.app": "app"})
    data: dict[str, list[dict]] = field(default_factory=dict)
    logs: dict[str, str] = field(default_factory=dict)

    def app(self, bundle): return self.apps.get(bundle)
    def submissions(self, app, kind): return self.data.get(kind, [])
    def builds(self, included): return {"b140": "140"}

    def crash_log(self, submission):
        if submission not in self.logs:
            raise KeyError(submission)
        return self.logs[submission]


def summary(lines: list[str]) -> str:
    return "### TestFlight feedback and crashes\n\n" + "".join(f"- {line}\n" if not line.startswith("  ") else f"  - {line.strip()}\n"
                                                                for line in lines)


def self_test() -> int:
    failures: list[str] = []

    def expect(what: str, condition: bool) -> None:
        if not condition:
            failures.append(what)

    now = _dt.datetime(2026, 10, 1, 12, 0, tzinfo=_dt.timezone.utc)
    since = now - _dt.timedelta(hours=24)

    def submission(id_: str, hours_ago: float, comment: str | None) -> dict:
        return {"id": id_, "attributes": {"createdDate": (now - _dt.timedelta(hours=hours_ago)).isoformat().replace("+00:00", "Z"),
                                          "comment": comment, "deviceModel": "iPad14,1", "osVersion": "26.0",
                                          "email": "tester@example.com",
                                          "screenshots": [{"url": "https://example.com/shot.png"}]},
                "relationships": {"build": {"data": {"type": "builds", "id": "b140"}}}}

    store = Pretend(data={
        "betaFeedbackScreenshotSubmissions": [
            submission("s1", 2, "The level never turns green. Mail me at tester@example.com or +1 555 123 4567, see https://x.example/y"),
            submission("s2", 48, "An old one"),
        ],
        "betaFeedbackCrashSubmissions": [submission("c1", 1, None), submission("c2", 3, "crashed again")],
    }, logs={"c1": "Incident Identifier: 1234\nHardware Model: iPad14,1\nException Type:  EXC_CRASH (SIGABRT)\n"
                   "Termination Reason: SIGNAL 6 Abort trap: 6\nThread 0 Crashed:\n0   libsystem_kernel.dylib 0x1\n"
                   "1   GroupLab.iOS                  0x0000000100abc GroupLab_Mobile_CapturePage_Read + 12\n"})
    lines, found = read(store, since)
    text = "\n".join(lines)
    expect("three in the last day", found == 3)
    expect("the old one left out", "An old one" not in text)
    expect("no comment's words", "level never turns green" not in text and "crashed again" not in text and "with a comment" in text)
    expect("no email, telephone number or web address", "tester@example.com" not in text and "555" not in text and "x.example" not in text)
    expect("no screenshot address", "shot.png" not in text)
    expect("the build number", "build 140" in text)
    expect("the crash's exception", "Exception Type: EXC_CRASH (SIGABRT)" in text and "GroupLab_Mobile_CapturePage_Read" in text)
    expect("not the whole crash log", "Incident Identifier" not in text and "libsystem_kernel" not in text)
    expect("a crash log not made yet", "could not be read (KeyError)" in text)
    expect("no comment said", "no comment" in text)
    crashes = [line for line in lines if line.startswith("Crash ")]
    expect("newest first", len(crashes) == 2 and "no comment" in crashes[0] and "with a comment" in crashes[1])
    expect("a long comment shortened", len(scrubbed("word " * 200)) <= COMMENT)

    quiet, found = read(Pretend(), since)
    expect("nothing new", found == 0 and quiet == ["No new TestFlight feedback or crashes."])
    missing, _ = read(Pretend(apps={}), since)
    expect("no app", missing and "has no app org.grouplab.app" in missing[0])
    expect("the summary", summary(["a", "  b"]) == "### TestFlight feedback and crashes\n\n- a\n  - b\n")

    for failure in failures:
        print("FAILED:", failure)
    print(f"testflight-feedback.py self-test: {'failed' if failures else 'passed'}")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] != ["--run"]:
        print(__doc__)
        return 2
    hours = float(argv[argv.index("--since-hours") + 1]) if "--since-hours" in argv else 24.0
    since = _dt.datetime.now(_dt.timezone.utc) - _dt.timedelta(hours=hours)
    names = ("APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8")
    if not all(os.environ.get(n) for n in names):
        lines = ["The App Store Connect key is not set, so TestFlight's feedback was not read."]
    else:
        testflight = _testflight()
        try:
            client = testflight.Apple(os.environ["APPLE_API_ISSUER_ID"], os.environ["APPLE_API_KEY_ID"],
                                      testflight.key_text(os.environ["APPLE_API_KEY_P8"]))
            lines, _ = read(Apple(client), since)
        except testflight.Refused as said:
            lines = [f"App Store Connect refused a request for TestFlight's feedback ({said}); nothing was read this run."]
    out = os.environ.get("GITHUB_STEP_SUMMARY")
    with open(out, "a", encoding="utf-8") if out else open(os.devnull, "w") as f:
        f.write(summary(lines))
    for line in lines:
        print(line)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
