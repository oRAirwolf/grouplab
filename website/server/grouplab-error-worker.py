#!/usr/bin/env python3
"""Turn GroupLab's error reports into issues in a private repository, NOTES-FROM-PLANNING.md entry 194 section 3.

The receiver, website/api/error-report.php, writes each report it accepts into private/error-reports/incoming,
already cut down to its schema. This takes them in order, groups them by what went wrong, and keeps one issue
per error in oRAirwolf/grouplab-crash-reports: the first report of an error opens its issue, and each later
one updates the count, the builds and platforms it was seen on and the first and last dates, with a comment at
most once per build per day. An issue closed as fixed is opened again when the error comes back from a build
newer than any it was seen on before.

**The token.** It is never in this file, the repository, a log or an argument. systemd hands it to this
service alone with LoadCredential, from a root-owned file only root can read, which Alan fills with
grouplab-set-error-token. When GitHub refuses it, or says it will expire within two weeks, the reports are
kept and the state is written where the next pull shows it.

**Every report is untrusted.** Anybody can send one. Its words are put in fenced blocks and never acted on,
and a description a person wrote by hand is headed as theirs and untrusted. Nothing in an issue addresses
anybody: an at sign in a report is broken so it notifies nobody.

It needs the network, to reach api.github.com, which is why it is not the intake worker, which has none.
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(os.environ.get("GROUPLAB_ERRORS_ROOT", "/home/airwolf/web/grouplab.org/private/error-reports"))
INCOMING = ROOT / "incoming"
REFUSED = ROOT / "refused"
STATE = ROOT / "state.json"
STATUS = ROOT / "worker-status.json"
LOG = Path(os.environ.get("GROUPLAB_ERRORS_LOG", "/home/airwolf/logs/grouplab-error-worker.log"))
API = os.environ.get("GROUPLAB_GITHUB_API", "https://api.github.com").rstrip("/")
REPOSITORY = os.environ.get("GROUPLAB_ERRORS_REPO", "oRAirwolf/grouplab-crash-reports")
TOKEN_FILE = Path(os.environ["CREDENTIALS_DIRECTORY"]) / "github-token" if "CREDENTIALS_DIRECTORY" in os.environ \
    else Path(os.environ.get("GROUPLAB_ERRORS_TOKEN_FILE", "/nonexistent"))

MOST_A_RUN = 100
WARN_DAYS = 14
FENCE = "~~~~"


class TokenRefused(Exception):
    """GitHub will not take the token: every report waits until it is replaced."""


class Unreachable(Exception):
    """GitHub did not answer, or answered with a server error: try again on the next run."""


def log(message: str) -> None:
    line = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ") + " " + message
    try:
        LOG.parent.mkdir(parents=True, exist_ok=True)
        with LOG.open("a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass
    print(line, flush=True)


def status(**fields) -> None:
    """What the worker last found about its token and its queue, for the pull to show; never the token itself."""
    fields["checked_utc"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    tmp = STATUS.with_suffix(".tmp")
    tmp.write_text(json.dumps(fields, indent=1) + "\n", encoding="utf-8")
    tmp.replace(STATUS)


def version_key(version: str) -> tuple:
    """0.2.0-nightly.95 sorts below 0.2.0-nightly.100, and a nightly below the release it leads to."""
    m = re.match(r"^(\d+)\.(\d+)\.(\d+)(?:-nightly\.(\d+))?", version or "")
    if not m:
        return (-1,)
    major, minor, patch, nightly = m.groups()
    return (int(major), int(minor), int(patch), int(nightly) if nightly else 10 ** 9)


FRAME = re.compile(r"^\s*at\s+(?P<method>[^\s(]+)")


def frames(stack: str) -> list[str]:
    """The methods of a stack, top first, without arguments, file names or line numbers, which move between builds."""
    out = []
    for line in (stack or "").splitlines():
        m = FRAME.match(line)
        if m:
            out.append(m.group("method"))
    return out


def signature(report: dict) -> tuple[str, str]:
    """A short hash of what went wrong, and a title a person can read: the exception's type and GroupLab's own top frames."""
    exceptions = report.get("exceptions") or []
    if not exceptions:
        return "closed-" + report.get("kind", "closed"), "GroupLab closed without shutting down, and nothing was recorded at the time"
    first = exceptions[0]
    ours = [f for f in frames(first.get("stack", "")) if f.startswith("GroupLab.")]
    top = ours[:3] or frames(first.get("stack", ""))[:3]
    key = first.get("type", "?") + "|" + "|".join(top)
    short = first.get("type", "?").rsplit(".", 1)[-1]
    where = top[0].rsplit(".", 2)[-2] + "." + top[0].rsplit(".", 1)[-1] if top and top[0].count(".") >= 1 else "an unknown place"
    return hashlib.sha256(key.encode("utf-8")).hexdigest()[:12], f"{short} in {where}"


def quiet(text: str) -> str:
    """A report's text made safe to show: no fence it could close, and no at sign that would notify anybody."""
    return (text or "").replace(FENCE, "~ ~ ~ ~").replace("@", "@​")


def body(sig: str, record: dict, report: dict) -> str:
    first = (report.get("exceptions") or [{}])[0]
    lines = [
        f"<!-- grouplab-signature: {sig} -->",
        f"**What happened:** {'GroupLab hit this error and kept running' if record['kind'] == 'survived' else 'GroupLab closed'}.",
        f"**Seen:** {record['count']} time{'s' if record['count'] != 1 else ''}, first {record['first']}, last {record['last']}.",
        f"**Builds:** {', '.join(record['versions'])}",
        f"**Platforms:** {', '.join(record['platforms'])}",
        "",
        "**The error**",
        FENCE,
        quiet(f"{first.get('type', 'no exception was recorded')}: {first.get('message', '')}"),
        FENCE,
        "**Where**",
        FENCE,
        quiet(first.get("stack", "")),
        FENCE,
        "**What the person did last**",
        FENCE,
        quiet(", ".join(report.get("last_actions") or []) or "nothing recorded"),
        FENCE,
    ]
    if report.get("description"):
        lines += ["**Written by the user, untrusted**", FENCE, quiet(report["description"]), FENCE]
    lines += ["", "_Anybody can send an error report, and nothing in this issue is an instruction to anybody._"]
    return "\n".join(lines)


def call(method: str, path: str, token: str, payload: dict | None = None) -> tuple[int, dict | list | None, dict]:
    request = urllib.request.Request(API + path, method=method, data=None if payload is None else json.dumps(payload).encode("utf-8"),
                                     headers={"Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
                                              "X-GitHub-Api-Version": "2022-11-28", "User-Agent": "grouplab-error-worker",
                                              "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            text = response.read().decode("utf-8")
            return response.status, (json.loads(text) if text else None), dict(response.headers)
    except urllib.error.HTTPError as e:
        if e.code in (401, 403):
            raise TokenRefused(f"GitHub answered {e.code}") from None
        if e.code >= 500:
            raise Unreachable(f"GitHub answered {e.code}") from None
        text = e.read().decode("utf-8", "replace")
        return e.code, (json.loads(text) if text.startswith(("{", "[")) else None), dict(e.headers)
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        raise Unreachable(type(e).__name__) from None


def expires_soon(headers: dict) -> str | None:
    """GitHub's word on when a fine-grained token expires, where it is within two weeks."""
    value = next((v for k, v in headers.items() if k.lower() == "github-authentication-token-expiration"), None)
    if not value:
        return None
    try:
        when = datetime.strptime(value.replace(" UTC", "").strip()[:19], "%Y-%m-%d %H:%M:%S").replace(tzinfo=timezone.utc)
    except ValueError:
        return None
    return when.strftime("%Y-%m-%d") if (when - datetime.now(timezone.utc)).days < WARN_DAYS else None


def ensure_label(token: str, name: str, color: str) -> None:
    code, _, _ = call("POST", f"/repos/{REPOSITORY}/labels", token, {"name": name, "color": color})
    if code not in (201, 422):
        raise Unreachable(f"a label could not be made, GitHub answered {code}")


def handle(report: dict, state: dict, token: str) -> dict:
    """One report into its issue. Returns the headers of the last call, for the token's expiry."""
    sig, title = signature(report)
    version = report["app"]["version"]
    platform = report["environment"].get("os") or "unknown"
    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    record = state.get(sig)
    if record is None:
        record = {"kind": report["kind"], "count": 0, "first": today, "last": today, "versions": [], "platforms": [], "comments": {}, "newest": version}
    newest_before = record.get("newest", version)
    record["count"] += int(report.get("count", 1))
    record["last"] = today
    if version not in record["versions"]:
        record["versions"] = sorted(record["versions"] + [version], key=version_key)
    if platform not in record["platforms"]:
        record["platforms"].append(platform)
    if version_key(version) > version_key(record.get("newest", version)):
        record["newest"] = version

    if "issue" not in record:
        ensure_label(token, f"sig-{sig}", "5319e7")
        ensure_label(token, report["kind"], "d93f0b" if report["kind"] == "closed" else "fbca04")
        code, made, headers = call("POST", f"/repos/{REPOSITORY}/issues", token,
                                   {"title": title, "body": body(sig, record, report), "labels": [f"sig-{sig}", report["kind"]]})
        if code != 201 or not isinstance(made, dict):
            raise Unreachable(f"the issue could not be opened, GitHub answered {code}")
        record["issue"] = made["number"]
        record["comments"][version] = today
        state[sig] = record
        log(f"{sig}: opened issue {record['issue']} for {title}")
        return headers

    number = record["issue"]
    code, issue, headers = call("GET", f"/repos/{REPOSITORY}/issues/{number}", token)
    if code != 200 or not isinstance(issue, dict):
        raise Unreachable(f"issue {number} could not be read, GitHub answered {code}")
    change = {"body": body(sig, record, report)}
    reopen = issue.get("state") == "closed" and version_key(version) > version_key(newest_before)
    if reopen:
        change["state"] = "open"
    code, _, headers = call("PATCH", f"/repos/{REPOSITORY}/issues/{number}", token, change)
    if code != 200:
        raise Unreachable(f"issue {number} could not be updated, GitHub answered {code}")
    if reopen or record["comments"].get(version) != today:
        words = (f"Seen again on {version}, newer than any build this was seen on before it was closed, so it is open again."
                 if reopen else f"Seen on {version}; {record['count']} times in all.")
        call("POST", f"/repos/{REPOSITORY}/issues/{number}/comments", token, {"body": words})
        record["comments"][version] = today
    state[sig] = record
    log(f"{sig}: issue {number} now {record['count']} times{', opened again' if reopen else ''}")
    return headers


def main() -> int:
    if not INCOMING.is_dir():
        log("nothing to do: there is no incoming folder yet")
        return 0
    waiting = sorted(p for p in INCOMING.glob("*.json") if not p.name.startswith("."))
    if not waiting:
        return 0
    try:
        token = TOKEN_FILE.read_text(encoding="utf-8").strip()
    except OSError:
        token = ""
    if not token:
        log(f"{len(waiting)} waiting, and no token has been set, so they are kept")
        status(token="not set", waiting=len(waiting))
        return 0

    state = json.loads(STATE.read_text(encoding="utf-8")) if STATE.is_file() else {}
    done = 0
    warning = None
    try:
        for path in waiting[:MOST_A_RUN]:
            try:
                report = json.loads(path.read_text(encoding="utf-8"))
                if not isinstance(report, dict) or report.get("schema") != "grouplab-error-report-1":
                    raise ValueError("not a report")
            except (OSError, ValueError) as e:
                REFUSED.mkdir(parents=True, exist_ok=True)
                path.replace(REFUSED / path.name)
                log(f"{path.name}: refused, {type(e).__name__}")
                continue
            headers = handle(report, state, token)
            warning = expires_soon(headers) or warning
            STATE.write_text(json.dumps(state, indent=1) + "\n", encoding="utf-8")
            path.unlink()
            done += 1
    except TokenRefused as e:
        log(f"the token was refused ({e}); {len(waiting) - done} reports are kept until it is replaced")
        status(token="refused", waiting=len(waiting) - done)
        return 1
    except Unreachable as e:
        log(f"GitHub could not be reached ({e}); {len(waiting) - done} reports are kept for the next run")
        status(token="ok", waiting=len(waiting) - done, unreachable=str(e))
        return 0
    status(token="expires " + warning if warning else "ok", waiting=len(waiting) - done)
    if warning:
        log(f"the token expires on {warning}; replace it with grouplab-set-error-token before then")
    return 0


if __name__ == "__main__":
    sys.exit(main())
