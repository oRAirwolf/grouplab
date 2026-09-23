# Entry 145: every build says what changed, in plain words

Written by the planning session at 23:45 Mountain on 2026-09-22.

Alan, on the release notes as they read today: "I dont like how many of the release notes just say 'Nothing in this build changes what you see or do. It carries internal work only.' No matter what is done, it should be stated plainly what changed."

He is right, and the sentence is not even true. Something changed in every build, or there would be no build. Saying otherwise teaches a reader that the page is filler and trains them to stop reading it.

Do this after entry 144, and before the rest of entry 143.

## 1. The rule

**No build ever says nothing changed.** Every published build lists what is in it, in plain English, whoever it affects. A build that carries one documentation commit says which document and what it now says.

## 2. The shape of an entry

Two headings, and a build shows only the ones it has.

**What you will notice.** Changes a user meets: something on screen, something that behaves differently, a new or removed feature, a fix to something that was wrong, a change to what is installed or downloaded. Written from the user's side, never from the code's.

**Under the hood.** Everything else, still in plain words: tests, documentation, the website, the build, refactoring, performance work that nobody can perceive yet. One line per real change, not a count. "Two internal changes" is the thing this entry exists to remove.

Keep each line to one sentence. Group several commits that did one job into one line, and say so: "three commits finishing the shot editor's undo support". Aim for at most six lines a build, by grouping rather than by leaving things out. Where a build is genuinely one commit, it is one line.

## 3. Where the words come from

1. `Release-note:` stays the first source, and it should now be written for every commit, not only the ones a user notices. `Release-note-kind:` says which of the two headings it belongs under: `user` or `internal`.
2. Where a commit has no trailer, `scripts/release-notes.py` must not fall back to a count. It writes a line from the commit's subject, rewritten as a plain sentence, and marks it so the build reads as complete rather than as boilerplate.
3. Add a check on main: every commit since the previous tag either carries a `Release-note:` trailer or is reported by name in the build's report, so a missing one is noticed at the time rather than months later on the site.

## 4. Plain words, specifically

Write for a shooter who has never read this repository. Name the thing on screen, not the class.

- Not "refactored MarkingSession.Load". Instead: "opening a sheet again keeps the marks you moved by hand".
- Not "added ResearchArticleTests". Instead: "the website now refuses to publish an article whose data file is missing".
- Not "bumped the freshness gate". Instead: "a nightly build is no longer published when a newer commit has already landed".

No class names, no file paths, no commit hashes in the body. The commit is already named in the entry's header for anyone who wants it. Keep the project's other rules: no em dashes, no pseudoscience, no jargon left unexplained.

## 5. Go back over the ones already published

Every entry in `docs/RELEASE-NOTES.md` that says nothing changed, or gives only a count, is rewritten from its own commits under section 2's shape. Do not invent detail: where a build really was one documentation commit, say which document and what changed in it. Keep each build's date and commit as recorded; this is a rewording, not a rewrite of history.

Nightlies 77 and 78 are the two nearest examples, and they are honest ones: 77 carried the release notes for nightlies 71 to 76, and 78 carried a note about where the notes stopped. Both are worth one plain line each, and both are more interesting than "internal work only".

## 6. Tests

- A test fails if any entry contains "nothing in this build changes", "internal work only", or a bare count of changes.
- A test fails if an entry has no lines under either heading.
- A test fails if a line contains a file path, a commit hash, or a bare identifier in code style, in the body of an entry.
- The update bar in the application reads the same source, so check that a multi-build offer still reads well with the new shape, and say in your report what it looks like for a three build jump.
