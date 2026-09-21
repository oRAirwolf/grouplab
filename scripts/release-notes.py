"""The notes for one build, written from the Release-note trailers of the commits since the last build on the same train.

NOTES-FROM-PLANNING.md entry 132 section 1, which replaced the old rule for a plain reason: Alan read the notes for v0.2.0-nightly.26 and they
told him nothing. They said things like "Entry 130 item 3.3: doubt travels with the number". That is a commit subject, written for the log,
and a person deciding whether to install a build cannot use it.

So nothing is guessed from a subject line any more. A commit that changes something a person can see or rely on says so itself:

    Release-note: When GroupLab finds fewer holes than the shots you fired, it now says so and lists the bulls with
    nothing on them, instead of reporting a clean result. (Entry 130, 2b.2)
    Release-note-kind: fixed

Commits without a trailer do not appear at all, except in one closing line counting them. That is deliberate: a notes fold, a write-up, a test
or a build change is invisible to a tester, and listing it is noise that makes the real notes harder to find.

It then checks each note and fails rather than publishing a build with unreadable notes: a note that is only a reference, that begins with
"Entry", that is too short to be a sentence, or that uses words meaning nothing to a shooter. And the rules the public repository has always
had: no em dashes, nothing from the private range folder or a submission, no coordinates, no server address.
"""
import re
import subprocess
import sys

NOTE = re.compile(r"^Release-note:\s*(?P<note>.+)$", re.IGNORECASE)
KIND = re.compile(r"^Release-note-kind:\s*(?P<kind>new|fixed|changed)\s*$", re.IGNORECASE)

KINDS = ["new", "fixed", "changed"]
HEADINGS = {"new": "**New**", "fixed": "**Fixed**", "changed": "**Changed**"}

# The fewest words that can be a sentence about what changed. A trailer shorter than this is a label, not a note.
LEAST_WORDS = 8

# Words that mean something inside this project and nothing to somebody who shoots. A note may use one only if it explains it in the same
# sentence, which is why the check looks for the word without an explanation beside it.
JARGON = {
    "folded": "it describes what happened to a planning note, not to the application",
    "gate record": "it is this project's own measurement file",
    "recorder": "it is a test double",
    "harness": "it is test machinery",
    "manifest": "say what it is for, such as the small file GroupLab reads to see whether a newer build exists",
    "trailer": "it is a line in a commit message",
    "fixture": "it is test material",
    "regression": "say what broke and what now works",
    "refactor": "nothing about it is visible to a person using GroupLab",
    "stub": "it is unfinished code",
}

FORBIDDEN = [
    ("an em dash", re.compile("[—–]")),
    ("the private range folder", re.compile(r"grouplab-range-2026|\d{8}_\d{6}\.jpg", re.IGNORECASE)),
    ("a submission", re.compile(r"grouplab-submissions|\b\d{4}-\d{2}-\d{2}_[0-9a-f]{8}\b")),
    ("coordinates", re.compile(r"\b-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}\b")),
    ("a server address", re.compile(r"\bpissinhot\b|\b\d{1,3}(\.\d{1,3}){3}\b")),
]

# One hand written block, entry 132 section 1.6: the builds from nightly 18 onwards went out with unreadable notes, so the next one says what
# they were. It is written here rather than derived, because those commits do not carry trailers and their releases are not edited.
SINCE_EIGHTEEN = """**Since nightly 18**

These builds went out with notes that did not say what changed. In plain words, this is what happened in them.

- GroupLab can update itself at last. Every build before nightly 25 refused its own update as unsigned, whichever version it was, so it could never install anything. If you are on nightly 18 or earlier you have to install this one by hand, once; after that it updates itself.
- Builds before nightly 16 described themselves as a development build in Settings and never looked for an update at all.
- When GroupLab finds fewer holes than the shots you fired, it says so and names the bulls with nothing on them, instead of showing a clean result you have no reason to question.
- Holes from small calibres such as .22 LR are no longer refused as too small when you have entered the calibre.
- A hole cut off by the edge of the scan is detected instead of being ignored.
- A shot that landed off the bulls is kept and offered, instead of being dropped.
- GroupLab now works out where your group actually landed before deciding which bull each shot belongs to, so a sheet shot away from its aim is no longer measured against the wrong bulls.
- Where the shot to bull assignment is not certain, the group figures say so, and the zero correction is withheld rather than being given from shots that may belong elsewhere.
- A blank sheet scanned on a flatbed can use the scan's own resolution as its scale. GroupLab shows the number and you can refuse it.
- The Support button opens the support page at grouplab.org, and the report window tells you both ways to send a report.
"""


def commits(previous, head):
    """Every commit's full message since the previous build, newest last."""
    span = (previous + ".." + head) if previous else ("-n 40 " + head)
    out = subprocess.run(
        ["git", "log", "--no-merges", "--format=%H%x00%B%x01"] + span.split(),
        capture_output=True, text=True, check=True).stdout
    for block in out.split("\x01"):
        block = block.strip()
        if not block:
            continue
        sha, _, body = block.partition("\x00")
        yield sha.strip()[:7], body


def read(body):
    """The note and its kind, or None where the commit does not carry one."""
    note = None
    kind = "changed"
    for line in body.splitlines():
        line = line.strip()
        if (m := NOTE.match(line)) is not None:
            note = m.group("note").strip()
        elif (k := KIND.match(line)) is not None:
            kind = k.group("kind").lower()
    return (note, kind) if note else None


def problems(sha, note):
    """Everything wrong with one note, said so it can be fixed."""
    found = []
    words = [w for w in re.split(r"\s+", re.sub(r"\(.*?\)", "", note)) if w]

    if note.lower().startswith("entry"):
        found.append("it begins with an entry reference rather than saying what changed")

    if len(words) < LEAST_WORDS:
        found.append(f"it is {len(words)} words, and a note has to be a sentence a person can read")

    if re.fullmatch(r"\(?[Ee]ntry[^)]*\)?\.?", note.strip()):
        found.append("it is only a reference")

    low = note.lower()
    for word, why in JARGON.items():
        if word in low:
            found.append(f"it uses {word!r}, which means nothing to somebody who shoots: {why}")

    for what, pattern in FORBIDDEN:
        if pattern.search(note):
            found.append(f"it contains {what}")

    return [f"{sha}: {p}" for p in found]


def main():
    version = sys.argv[1] if len(sys.argv) > 1 else "this build"
    head = sys.argv[2] if len(sys.argv) > 2 else "HEAD"
    previous = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] else ""

    notes = {k: [] for k in KINDS}
    silent = 0
    wrong = []

    for sha, body in commits(previous, head):
        read_note = read(body)
        if read_note is None:
            silent += 1
            continue
        note, kind = read_note
        wrong += problems(sha, note)
        notes[kind].append(note)

    if wrong:
        print("These release notes cannot be published:", file=sys.stderr)
        for line in wrong:
            print("  " + line, file=sys.stderr)
        print("\nFix the Release-note trailer on those commits. See NOTES-FROM-PLANNING.md entry 132 section 1.", file=sys.stderr)
        return 1

    lines = [f"GroupLab {version}.", ""]

    # Entry 132 section 1.6: the builds from nightly 18 onwards went out with notes that said nothing, so the first nightly with readable
    # notes carries a hand written account of them. It is dropped once a build after it has been published with real notes.
    if "--since-eighteen" in sys.argv:
        lines.append(SINCE_EIGHTEEN)
        lines.append("")

    if previous and not any(notes[k] for k in KINDS) and silent:
        lines.append("Nothing in this build changes what you see or do. It carries internal work only.")
        lines.append("")

    for kind in KINDS:
        if not notes[kind]:
            continue
        lines.append(HEADINGS[kind])
        lines.append("")
        for note in notes[kind]:
            lines.append("- " + note)
        lines.append("")

    if silent:
        lines.append(f"Plus {silent} internal changes (tests, documentation, build).")
        lines.append("")

    text = "\n".join(lines)
    for what, pattern in FORBIDDEN:
        if pattern.search(text):
            print(f"The notes contain {what}, so this build is not published.", file=sys.stderr)
            return 1

    print(text.rstrip() + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
