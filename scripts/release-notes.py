"""The notes for one build, written from the commits since the last build on the same train.

NOTES-FROM-PLANNING.md entry 119 section 5. A person reading a nightly's notes wants to know what changed, not to read a commit log, so the
first line of each commit is grouped under a plain heading by the rule below and everything that is not about the software is dropped.

The rule, stated so it can be argued with:
  - The "Entry NNN section N:" prefix is taken off first, so the words that decide are the ones about the software.
  - "fixed", where the line says a fault was put right: fix, defect, correct, wrong, fault, broke, regression, red, silently.
  - "new", where something is added: add, new, introduce, publishes, first, now has.
  - "changed" for everything else, because a commit that is neither a fix nor an addition changed something already there.
  - "other" is kept for lines a person put there by hand and is empty under this rule.
A merge commit is skipped. A commit that only folds planning notes is skipped: its first line begins with "Entries" or matches
"folded into the notes log" or is only a notes delivery.

It then checks the notes against the same rules as the public repository, and fails rather than publishing: no em dashes, nothing from the
private range folder or a submission, no coordinates, no server address.
"""
import re
import subprocess
import sys

SKIP = re.compile(r"^(Merge |Entries? \d|Entry \d+ and \d+ folded|.*folded into the notes log)", re.IGNORECASE)
TRAILER = re.compile(r"^(Co-Authored-By|Signed-off-by|Generated with|🤖)", re.IGNORECASE)

PREFIX = re.compile(r"^Entry \d+[^:]*:\s*", re.IGNORECASE)

RULES = [
    ("fixed", re.compile(r"\b(fix\w*|defect|corrects?|corrected|wrong\w*|fault\w*|broke\w*|regression|red|silently)\b", re.IGNORECASE)),
    ("new", re.compile(r"\b(add\w*|new|introduce\w*|publishes?|first|now has)\b", re.IGNORECASE)),
]

FORBIDDEN = [
    ("an em dash", re.compile("[—–]")),
    ("the private range folder", re.compile(r"grouplab-range-2026|\d{8}_\d{6}\.jpg", re.IGNORECASE)),
    ("a submission", re.compile(r"grouplab-submissions|\b\d{4}-\d{2}-\d{2}_[0-9a-f]{8}\b")),
    ("coordinates", re.compile(r"\b-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}\b")),
    ("a server address", re.compile(r"\bpissinhot\b|\b\d{1,3}(\.\d{1,3}){3}\b")),
]


def subjects(previous, head):
    span = (previous + ".." + head) if previous else ("-n 20 " + head)
    out = subprocess.run(
        ["git", "log", "--no-merges", "--format=%s"] + span.split(),
        capture_output=True, text=True, check=True).stdout
    lines = []
    for line in out.splitlines():
        line = line.strip()
        if not line or SKIP.match(line) or TRAILER.match(line):
            continue
        lines.append(line)
    return lines


def group(lines):
    buckets = {"new": [], "fixed": [], "changed": [], "other": []}
    for line in lines:
        body = PREFIX.sub("", line)
        for name, rule in RULES:
            if rule.search(body):
                buckets[name].append(line)
                break
        else:
            # Anything that is neither a fault put right nor something added has changed something already there.
            buckets["changed"].append(line)
    return buckets


def render(version, buckets):
    out = []
    for name, heading in (("new", "New"), ("fixed", "Fixed"), ("changed", "Changed"), ("other", "Other")):
        if buckets[name]:
            out.append("**" + heading + "**")
            out.append("")
            out.extend("- " + line for line in buckets[name])
            out.append("")
    if not out:
        out = ["No commits since the previous build on this train.", ""]
    return "\n".join(out).strip() + "\n"


def check(text):
    bad = []
    for what, rule in FORBIDDEN:
        found = rule.search(text)
        if found:
            bad.append(what + ": " + found.group(0)[:60])
    return bad


if __name__ == "__main__":
    version = sys.argv[1]
    head = sys.argv[2]
    previous = sys.argv[3] if len(sys.argv) > 3 else ""
    notes = render(version, group(subjects(previous, head)))
    problems = check(notes)
    if problems:
        sys.stderr.write("the release notes fail the repository's own rules, so nothing is published: " + "; ".join(problems) + "\n")
        raise SystemExit(1)
    sys.stdout.write(notes)
