#!/usr/bin/env python3
"""Replies that arrived on a closed Discord issue after it was closed, NOTES-FROM-PLANNING.md entry 510 section 2.

The Discord worker adds a later message in a thread as a comment on the thread's issue, and before entry 510 it never reopened a closed
one, so the usual check of open issues missed a reply with a screen recording on issue 53 for two days. Until the worker that reopens
such an issue and labels it `new-reply` is installed and proven, Claude Code runs this with the open issue check (the start of a run,
before each inbox entry, before ending a turn, and hourly while waiting on CI). It prints one line per closed `discord` issue with a
comment from Discord made after the issue closed, and nothing else; no comment's words are printed, because they are data.

A reply drops off this list once a comment that is not from Discord follows it on the issue (Code's assessment of it), or once the
issue is closed again after it.

    python scripts/closed-replies.py [--days 30]
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from datetime import datetime, timedelta, timezone

REPOSITORY = "oRAirwolf/grouplab-crash-reports"
# The worker heads each comment it adds with the poster's handle in bold and "on Discord"; Code's own comments never are.
FROM_DISCORD = re.compile(r"^\*\*[^*\n]+\*\* on Discord, ")


def api(path: str) -> list | dict:
    done = subprocess.run(["gh", "api", "--paginate", path], capture_output=True, text=True, encoding="utf-8")
    if done.returncode != 0:
        raise SystemExit("closed-replies: gh api failed: " + done.stderr.strip()[:200])
    text = done.stdout.strip()
    # --paginate joins pages of a list as several JSON arrays one after another.
    return json.loads("[" + text.replace("]\n[", ",").replace("][", ",").strip("[]") + "]") if text.startswith("[") else json.loads(text)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--days", type=int, default=30, help="look at issues updated in the last this many days")
    args = parser.parse_args()
    since = (datetime.now(timezone.utc) - timedelta(days=args.days)).strftime("%Y-%m-%dT%H:%M:%SZ")
    issues = api(f"/repos/{REPOSITORY}/issues?state=closed&labels=discord&since={since}&per_page=100")
    found = 0
    for issue in issues:
        closed = issue.get("closed_at")
        if not closed or "pull_request" in issue:
            continue
        comments = api(f"/repos/{REPOSITORY}/issues/{issue['number']}/comments?per_page=100")
        late = [c for c in comments if c["created_at"] > closed and FROM_DISCORD.match(c.get("body") or "")]
        # A reply already answered on the issue (a comment of Code's after it) is assessed, and is not listed again.
        if late and any(c["created_at"] > late[-1]["created_at"] and not FROM_DISCORD.match(c.get("body") or "") for c in comments):
            late = []
        if late:
            found += 1
            attached = sum((c.get("body") or "").count("\nAttachments:") for c in late)
            print(f"issue {issue['number']}: {len(late)} Discord reply(s) after it closed on {closed[:10]}, the newest {late[-1]['created_at']}"
                  + (f", {attached} with attachments" if attached else ""))
    print(f"closed-replies: {found} closed Discord issue(s) with a reply after closing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
