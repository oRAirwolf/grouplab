# 2026-09-24, entry 168: nightly 94 should not exist, its notes are cut off, and the platform statement leaves the releases

Alan, looking at nightly 94 on GitHub: "It looks like nightlies are still getting published when there
are no changes to the application. Also the notes are getting cut off. I dont think the 'what is
supported' section needs to be added to every release note. Can you go back and fix all of the release
notes?"

Do this entry **next**, ahead of the rest of the queue. It is public, and every night it waits, another
release goes out with the same three faults.

## 1. What nightly 94's release says

- "What you will notice": one item, "The website and the guides now agree on how many target sheets".
  That is a website change. Nobody will notice it in the application.
- "Under the hood": four items, and three are cut off mid sentence: "The research article about hole size
  now shows three real", "Nothing in this changes the application. The project's own", "Every research
  article on grouplab.org now ends with what its". The fourth is "[notes] 0.2.0-nightly.93.", which is
  the nightly's own release notes commit showing up as a change.
- Then the full platform statement, which still says macOS has never been run on a Mac.

**Its own notes say nothing in it changes the application.** So the entry 150 gate let through a build
that, by its own account, should not have been built.

## 2. Why the gate let it through

The planning session read `.github/shipping-paths.json`. Its rule is that the first segment of a path
decides, and `ships` includes `tests`, `scripts`, `.github`, `samples` and `targets` as whole
directories. So a change to any test, to `release-notes.py`, to `shipping-gate.py`, to any workflow, or
to a research figure script under `scripts/` produces an executable. None of those change the
executable.

That is partly the planning session's fault: entry 150 section 1 listed "tests where a test failure would
have stopped the build" as shipping, which is too broad and was never a rule a machine could apply.

1. **Say exactly which path made nightly 94 build.** Run `scripts/shipping-gate.py --decide` on the
   commits behind nightlies 93 and 94 and report the path, in the run report.
2. **Three classes, not two:**
   - **ships**: an input to the published executable or the installer. The build produces a release.
   - **checked**: tests, the build and release tooling, the workflows, the site scripts. CI runs the full
     suite on them, and a failure still blocks main, but **no release is produced**.
   - **content**: the website, the documentation, the research, the release notes. Published by the site
     path only.
3. **Take the ships list from the build, not from a judgement about directories.** Ask MSBuild what
   `dotnet publish` actually reads for each shipped project, the compile items, embedded resources and
   content files, plus what the installer and packaging scripts read, and generate the ships list from
   that. Where a directory is partly shipped, as `scripts/` is, classify by file. `package-windows.ps1`
   ships. `release-notes.py` and `shipping-gate.py` do not.
4. **Keep the rule that an unclassified path is a failure**, which is right.
5. A test holds the generated list to what the build reads, so a new embedded resource cannot be added
   without the gate knowing.

## 3. Why the notes are cut off

`scripts/release-notes.py`, `read()`: it keeps only the line that matches `Release-note:` and discards
every line after it. Any note longer than one line is cut at the first line break. **The script's own
documented example wraps onto a second line**, so the example in the docstring would itself be truncated.

1. A trailer's value continues onto every following line until a blank line or the next `Key:` trailer,
   whether or not the continuation is indented. Join them with single spaces.
2. A test using the docstring's own two line example, and one with a three line note.
3. **Commits with no trailer**: a commit whose subject starts with `[notes]`, or whose changed files are
   all release notes, never appears in any release note. The nightly writing its own notes is not a
   change to the application.

## 4. Only what ships goes in an application release

A build's release notes describe that build. A research article, a website page or a guide is not in the
executable, so it does not belong in the executable's notes, under either heading. It is published on
the site and the site says it changed.

1. The generator includes a commit only if it touched a path the gate classes as ships.
2. "What you will notice" is only for something a person will notice **in the application**.
3. **Ban the pattern, not only the phrase.** Entry 145 banned "nothing in this build changes". Nightly 94
   says "Nothing in this changes the application". Any note in an application release that says the
   application did not change is a contradiction, because such a build should not exist. The test
   refuses any note matching that meaning, and names the commit whose trailer said it.

## 5. The platform statement leaves the release bodies

Entry 147 section 3.2 put the whole statement on every release that carries a macOS asset. That was the
planning session's instruction and it was wrong for two reasons. It repeats the same long section on
every release, and **a published release body is frozen**, so every copy goes stale the day the statement
changes. Nightly 94's copy was already false when it was published.

1. Remove the platform section from release bodies, and remove the step in `nightly.yml` that adds it.
2. Replace it with one line, generated from `docs/PLATFORM-SUPPORT.md`'s own address: which platforms are
   supported, what is tested on each, and how to open the unsigned macOS build, with a link to the
   download page, where the statement is always current.
3. The README and the download page keep the full statement, generated from the one source as now.
4. The drift test from entry 147 changes to assert the link is present and the section is absent.

## 6. Fix every published release

Alan has asked for all of them to be fixed. With sections 2 to 5 in place, regenerate the body of **every
published release** and `docs/RELEASE-NOTES.md`, the same way entry 145's backfill did:

1. Full sentences, with no cut off notes.
2. No platform section.
3. No website, research or documentation items, and no `[notes]` commits.
4. **A release whose build contained no application change says so, plainly and once**: "This build has
   no change to the application; it behaves exactly as nightly NN does." It does not pretend otherwise,
   and it does not list content changes to fill the space.
5. **Do not delete any release, and do not touch any asset or tag.** Every build keeps a release so that
   a bug report names something that still exists. If Alan wants the empty ones removed, that is his
   decision and he will say so separately.
6. After the rewrite, read back three of them from GitHub, including nightly 94, and quote the first line
   of each in the report as proof.

## 7. Report

Five lines as usual, plus these, because Alan asked a direct question:

1. The path that made nightly 94 build.
2. How many past nightlies had no application change under the corrected classes.
3. How many release bodies were rewritten.
