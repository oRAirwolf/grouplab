# 2026-09-21, entry 132: release notes that say what changed, and smaller builds

## 1. Release notes a tester can read

Alan read the notes for `v0.2.0-nightly.26` and they told him nothing. They read, for example, "Entry 130 item 3.3: doubt travels with the number", "Entry 130 folded, with question 34 and the night's write-up" and "Entry 129 section 3.5.1: the worker decodes with no way out". Commit subject lines are written for the log, not for a person deciding whether to install a build. Keep the entry reference, but every note must say **what changed, in plain words, from the user's side**.

1. **Every commit that changes something a person can see or rely on carries a `Release-note:` trailer**: one or two plain sentences saying what is different for someone using GroupLab, ending with the reference in brackets. For example:
   - `Release-note: When GroupLab finds fewer holes than the shots you fired, it now says so and lists the bulls with nothing on them, instead of reporting a clean result. (Entry 130, 2b.2)`
   - `Release-note: For small calibres such as .22 LR, holes are no longer rejected as too small when you have entered the calibre. (Entry 130, 2b.3)`
   - `Release-note: A blank sheet scanned on a flatbed can now use the scan's own resolution as its scale; GroupLab shows the number and you can refuse it. (Entry 130, 4.1)`
2. `scripts/release-notes.py` builds the notes **only from these trailers**, grouped under New, Fixed and Changed by a `Release-note-kind:` trailer (new, fixed or changed) rather than by guessing from words. Commits without a trailer (notes folding, write-ups, tests, internal refactors, CI) do not appear, except as one closing line: "Plus N internal changes (tests, documentation, build)."
3. The script **fails the nightly** if a trailer is only a reference, starts with "Entry", is shorter than eight words, uses internal jargon that means nothing to a shooter (list the words you check: for example "folded", "gate record", "recorder", "harness", "manifest" unless explained), or breaks the existing rules (em dash, private paths, coordinates, server address). A failed check names the commit so it can be fixed.
4. Add the trailer rule to `CLAUDE.md` in your own words, with the three examples.
5. The same notes appear in the in-application update bar (entry 119 section 5.2), so they must read well there too.
6. Write proper notes for **everything since `v0.2.0-nightly.18`** that a tester would notice, as a hand-written list the next nightly's body starts with ("Since nightly 18"), since those builds went out with unreadable notes. Do not edit the published releases.

## 2. Smaller builds, and no more copies

Alan's `C:\Dev\grouplab` folder is about 11 GB, and about 10 GB of it is build output: nine copies of the Core test build (`Debug`, `Release`, `alt` to `alt7`) at about 685 MB each, and in every build about 675 MB of native libraries for some 30 platforms nobody runs (Linux on ARM, RISC-V, s390x, LoongArch, MIPS, WebAssembly, Mac Catalyst and more).

1. **Stop making new `altN` output folders.** If a build needs to avoid a locked file, reuse one alternate folder (for example `bin/alt`) and clear it before reuse. Say in `CLAUDE.md` that no other output folders are created.
2. **Copy native libraries only for the platforms GroupLab builds and tests on**: Windows x64, Linux x64, macOS (x64 and arm64). Do this in the build (for example restricting runtime identifiers for the test and application projects, or trimming the packages' native assets) without changing the published packages' behaviour, and prove it: CI green on all three platforms, the installer and zip still run with nothing installed, and the Phase 0 gate record unchanged. Report the size of one Core test build before and after.
3. **Do not delete anything in Alan's folders yourself**, including the old `alt` folders; Alan will clear them himself in the morning with the commands in `C:\Dev\DEV-CLEANUP-REPORT-2026-09-21.md`. Do not read or change that report.

## 3. Order

Section 1 goes before any further nightly is published, so the next nightly already has readable notes. Section 2 fits in after entry 131, within tonight's queue under entry 130 section 0 and entry 131 section 0.
