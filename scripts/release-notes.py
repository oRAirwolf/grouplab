"""The notes for one build, in plain words, from the commits since the last build on the same train.

NOTES-FROM-PLANNING.md entry 132 section 1 stopped the notes being commit subjects, because Alan read
"Entry 130 item 3.3: doubt travels with the number" and it told him nothing. Entry 145 fixes what that left
behind. Six published builds said:

    Nothing in this build changes what you see or do. It carries internal work only.

Alan, on reading them: "No matter what is done, it should be stated plainly what changed." He is right, and
the sentence was not even true. Something changed in every build or there would have been no build. One of
those six carried the mounted photograph gate measured on 59 real frames for the first time; another carried
eight research articles. Saying "internal work only" about those teaches a reader that the page is filler.

**So no build ever says nothing changed.** Every build lists what is in it under at most two headings:

    **What you will notice**   something on screen, something that behaves differently, a new or removed
                               feature, a fix, a change to what is installed or downloaded
    **Under the hood**         everything else, still in plain words: tests, documentation, the website,
                               the build, refactoring, performance nobody can perceive yet

A commit says which it belongs under:

    Release-note: When GroupLab finds fewer holes than the shots you fired, it now says so and lists the
    bulls with nothing on them, instead of reporting a clean result. (Entry 130, 2b.2)
    Release-note-kind: fixed

``new``, ``fixed`` and ``changed`` all mean the first heading and are kept, because every commit in the
history uses them and they still say something useful. ``user`` is a synonym of ``changed``. ``internal``
means the second.

**A commit with no trailer is not a count any more.** It gets a line of its own, written from its subject
with the entry reference taken off, under the second heading. That is a floor, not a target: ``--missing``
lists every commit since the previous build that made the generator do it, so the build's own report names
them and a missing trailer is noticed on the day rather than months later on the website.

Every line, written or generated, is then checked, and the build fails rather than publishing notes a
shooter cannot read: a line that is only a reference, that begins with "Entry", that is too short to be a
sentence, that uses a word meaning nothing outside this repository, or that carries a file path, a commit
hash or a class name. And the rules the public repository has always had: no em dashes, nothing from the
private range folder or a submission, no coordinates, no server address.
"""
import json
import re
import subprocess
import sys

NOTE = re.compile(r"^Release-note:\s*(?P<note>.+)$", re.IGNORECASE)
KIND = re.compile(r"^Release-note-kind:\s*(?P<kind>new|fixed|changed|user|internal)\s*$", re.IGNORECASE)

# Entry 145 section 2. Two headings, and a build shows only the ones it has. The three older kinds are kept because every
# commit in the history uses them and "New" and "Fixed" still tell a reader something "changed" does not; they are simply
# all under the first heading now. "user" is a synonym of "changed" for anybody writing a trailer from entry 145 alone.
KINDS = ["new", "fixed", "changed", "internal"]
SAME = {"user": "changed"}
NOTICED = ["new", "fixed", "changed"]
NOTICE_HEADING = "**What you will notice**"
HOOD_HEADING = "**Under the hood**"

# The fewest words that can be a sentence about what changed. A trailer shorter than this is a label, not a note.
LEAST_WORDS = 8

# How many builds of notes the second manifest format carries, entry 139 section 3. Ten is far more than anybody skips
# between updates, and the whole list is a few kilobytes.
VERSIONS_CARRIED = 10

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

# The few repository files whose names turn up in a commit subject, and what they are to somebody who has never read this
# repository. Entry 145 section 1 asks that a build carrying one documentation commit says which document and what it now says, so
# these are translated rather than refused. Anything not here still fails, which is what keeps this list from becoming a way of
# publishing a line nobody outside can read.
SPELL = {
    "docs/RELEASE-NOTES.md": "the release notes",
    "RELEASE-NOTES.md": "the release notes",
    "docs/USER-GUIDE.md": "the user guide",
    "docs/TESTING-GUIDE.md": "the guide for testers",
    "docs/GLOSSARY.md": "the glossary",
    "docs/WEBSITE.md": "the notes on how the website is built and served",
    "docs/RESEARCH.md": "the notes on how the research articles are made",
    "CLAUDE.md": "the rules this project works to",
    "README.md": "the front page of the project",
}


# Entry 145 section 4: write for a shooter who has never read this repository. Name the thing on screen, not the class.
# "GroupLab" is the one word shaped like a class name that belongs in a note.
CODE_SHAPED = [
    ("a file path", re.compile(r"\b[\w.-]+/[\w./-]+|\b[\w-]+\.(?:md|py|cs|json|ya?ml|html|css|js|txt|pdf|png)\b", re.IGNORECASE)),
    ("a commit hash", re.compile(r"\b(?=[0-9a-f]{7,40}\b)(?=[^\s]*\d)[0-9a-f]{7,40}\b")),
    ("a class or method name", re.compile(r"\b(?!GroupLab\b)[A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]*)+\b")),
    ("a name in code style", re.compile(r"`[^`]+`")),
]

FORBIDDEN = [
    ("an em dash", re.compile("[—–]")),
    ("the private range folder", re.compile(r"grouplab-range-2026|\d{8}_\d{6}\.jpg", re.IGNORECASE)),
    ("a submission", re.compile(r"grouplab-submissions|\b\d{4}-\d{2}-\d{2}_[0-9a-f]{8}\b")),
    ("coordinates", re.compile(r"\b-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}\b")),
    ("a server address", re.compile(r"\bpissinhot\b|\b\d{1,3}(\.\d{1,3}){3}\b")),
]



def nightly_number(tag):
    """The run number in a per-build nightly tag, or None where the tag is not one."""
    m = re.fullmatch(r"v\d+\.\d+\.\d+-nightly\.(\d+)", tag.strip())
    return int(m.group(1)) if m else None


def previous_published(version):
    """The tag of the newest published build on this train below this one, or "" where this is the first.

    NOTES-FROM-PLANNING.md entry 138 section 2. The rolling ``nightly`` tag moves, so a run that reads it can
    see itself or an older build depending on when it looks; the per-build ``v<version>-nightly.N`` tags do not
    move, and the newest one below this build is exactly the version a person could have been on before it.

    A nightly that was cancelled or skipped before publishing never got a tag, so its changes roll into the next
    published build on their own, which is what entry 138 section 1 asks for.
    """
    mine = nightly_number(version if version.startswith("v") else "v" + version)
    if mine is None:
        return ""

    out = subprocess.run(
        ["git", "tag", "--list", "v*-nightly.*"], capture_output=True, text=True, check=True
    ).stdout.split()
    below = [(n, t) for t in out if (n := nightly_number(t)) is not None and n < mine]
    return max(below)[1] if below else ""


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
            kind = SAME.get(k.group("kind").lower(), k.group("kind").lower())
    return (note, kind) if note else None


def plain(subject):
    """A commit subject turned into a sentence, for a commit that carries no trailer.

    Entry 145 section 3.2: where a commit has no trailer this must not fall back to a count. The subject is what
    the repository already has, and most of a subject here is already a sentence with a reference bolted to the
    front of it. So the reference comes off, the first letter goes up, and a full stop goes on.

    It is a floor and not a target. A subject written for the log will often fail the checks below, which is the
    point: the line names the commit that needs a trailer instead of hiding it in a number.
    """
    text = subject.strip()

    # "Entry 144 section 3: ", "Entry 130 item 2b.6: ", "Entry 142, research batch 3: " and the bare "CLAUDE.md: ".
    text = re.sub(r"^Entry\s+\d+[^:]*:\s*", "", text, flags=re.IGNORECASE)
    text = re.sub(r"^[\w.-]+\.(?:md|py|cs|json|ya?ml):\s*", "", text, flags=re.IGNORECASE)
    for name, words in SPELL.items():
        text = re.sub(re.escape(name), words, text, flags=re.IGNORECASE)

    text = text.strip().rstrip(".")

    if not text:
        return ""
    return text[0].upper() + text[1:] + "."


def problems(sha, note, generated=False):
    """Everything wrong with one note, said so it can be fixed.

    ``generated`` is on for a line written from a commit subject rather than from a trailer. Such a line is held to
    everything that could mislead a reader or leak something, and not to the length rule. The length rule is there to
    stop somebody writing a label where a note belongs; a commit subject is not a label chosen instead of a note, it
    is the repository's own summary of the change, and rejecting it would put the build back to saying nothing, which
    is the one thing entry 145 forbids. ``--missing`` names those commits instead, so the note gets better next time.
    """
    found = []
    words = [w for w in re.split(r"\s+", re.sub(r"\(.*?\)", "", note)) if w]

    if note.lower().startswith("entry"):
        found.append("it begins with an entry reference rather than saying what changed")

    if len(words) < LEAST_WORDS and not generated:
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

    # Entry 145 section 4. The reference in brackets at the end is the one place a note is allowed to name an entry,
    # so it is taken off before this runs; everything else is prose a shooter has to be able to read.
    for what, pattern in CODE_SHAPED:
        if (m := pattern.search(re.sub(r"\((?:Entry|entry)[^)]*\)\s*$", "", note))) is not None:
            found.append(f"it contains {what}, {m.group(0)!r}, which names something only this repository knows about")

    return [f"{sha}: {p}" for p in found]


def published_below(version):
    """Every published per-build tag below this version, newest first."""
    mine = nightly_number(version if version.startswith("v") else "v" + version)
    if mine is None:
        return []
    out = subprocess.run(
        ["git", "tag", "--list", "v*-nightly.*"], capture_output=True, text=True, check=True
    ).stdout.split()
    below = [(n, t) for t in out if (n := nightly_number(t)) is not None and n < mine]
    return [t for _, t in sorted(below, reverse=True)]


def versions_file(out, version, head, count):
    """The notes of this build and the ones before it, as the second manifest format carries them.

    NOTES-FROM-PLANNING.md entry 139 section 3. Somebody on nightly 31 offered nightly 40 has not seen 32 to 39
    either, so the update bar shows all of them; the notes were written once when each build was published, and
    this writes them out again rather than asking the application to work anything out.

    Only the second manifest format carries this. The first cannot gain a field without stopping every installed
    build from updating itself, which is what entry 138 section 5 found the hard way.
    """
    builds = []
    tags = published_below(version)
    for tag, older in zip([None] + tags, tags + [""]):
        this = version if tag is None else tag.lstrip("v")
        text, wrong = build_notes(this, head if tag is None else tag, older, heading=False)
        if wrong:
            print("The notes for " + this + " cannot be published:", file=sys.stderr)
            for line in wrong:
                print("  " + line, file=sys.stderr)
            return 1
        builds.append({"version": this, "notes": text})
        if len(builds) >= count:
            break

    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(builds, f, indent=2)
        f.write("\n")
    print("Wrote " + out + ": " + str(len(builds)) + " versions of notes, newest first.")
    return 0


def build_notes(version, head, previous, heading=True):
    """One build's notes as text, and everything wrong with the trailers behind them.

    ``heading`` is off for the per-version notes the second manifest carries, where the update bar writes the
    version heading itself and a second one inside the text would read as a stutter.
    """
    notes = {k: [] for k in KINDS}
    generated = []
    wrong = []

    for sha, message in commits(previous, head):
        read_note = read(message)
        if read_note is None:
            # Entry 145 section 1: no build ever says nothing changed. A commit with no trailer still did something,
            # so its subject becomes a line rather than a number.
            first = message.splitlines()[0] if message.splitlines() else ""
            line = plain(first)
            if not line:
                continue
            wrong += problems(sha, line, generated=True)
            notes["internal"].append(line)
            generated.append(sha)
            continue
        note, kind = read_note
        wrong += problems(sha, note)
        notes[kind].append(note)

    if wrong:
        return "", wrong

    # A build with no commits behind it is the only thing left that could say nothing, and it cannot happen: a build is
    # made from a commit. Refusing it here rather than printing an empty block is what stops the old sentence returning.
    if not any(notes[k] for k in KINDS):
        return "", ["this build has no commits behind it, which cannot be right, so there is nothing honest to publish"]

    out = [f"GroupLab {version}.", ""] if heading else []

    if any(notes[k] for k in NOTICED):
        out.append(NOTICE_HEADING)
        out.append("")
        for kind in NOTICED:
            for note in notes[kind]:
                out.append("- " + note)
        out.append("")

    if notes["internal"]:
        out.append(HOOD_HEADING)
        out.append("")
        for note in notes["internal"]:
            out.append("- " + note)
        out.append("")

    text = "\n".join(out)
    for what, pattern in FORBIDDEN:
        if pattern.search(text):
            return "", ["the notes contain " + what]

    return text.rstrip() + "\n", []


def missing(version, head, previous):
    """Every commit since the previous build that carries no Release-note trailer, named.

    Entry 145 section 3.3. The generator no longer hides these in a count, so they reach the notes as a line from
    the commit subject; this is the other half, which puts them in the build's own report by name. A trailer that
    was forgotten is then noticed on the day the build goes out, by the person who wrote the commit, rather than
    months later by somebody reading the website.

    It reports; it does not fail. A build is not worth stopping over a note that can be improved afterwards, and
    entry 144 section 2.4 says the generated entry is the floor and not the ceiling.
    """
    found = [(sha, message.splitlines()[0] if message.splitlines() else "")
             for sha, message in commits(previous, head) if read(message) is None]

    if not found:
        print("Every commit in " + version + " carries a Release-note trailer.")
        return 0

    print(str(len(found)) + " of the commits in " + version + " carry no Release-note trailer, so their notes were "
          + "written from their subjects. Named here so they can be improved:")
    for sha, subject in found:
        print("  " + sha + "  " + subject)
    return 0


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "--missing":
        version = sys.argv[2] if len(sys.argv) > 2 else "this build"
        head = sys.argv[3] if len(sys.argv) > 3 else "HEAD"
        previous = sys.argv[4] if len(sys.argv) > 4 and sys.argv[4] else previous_published(version)
        return missing(version, head, previous)

    if len(sys.argv) > 2 and sys.argv[1] == "--versions":
        out = sys.argv[2]
        version = sys.argv[3] if len(sys.argv) > 3 else "this build"
        head = sys.argv[4] if len(sys.argv) > 4 else "HEAD"
        count = int(sys.argv[5]) if len(sys.argv) > 5 else VERSIONS_CARRIED
        return versions_file(out, version, head, count)

    version = sys.argv[1] if len(sys.argv) > 1 else "this build"
    head = sys.argv[2] if len(sys.argv) > 2 else "HEAD"
    previous = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] else previous_published(version)

    text, wrong = build_notes(version, head, previous)
    if wrong:
        print("These release notes cannot be published:", file=sys.stderr)
        for line in wrong:
            print("  " + line, file=sys.stderr)
        print("\nFix the Release-note trailer on those commits. See NOTES-FROM-PLANNING.md entry 132 section 1.", file=sys.stderr)
        return 1

    print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
