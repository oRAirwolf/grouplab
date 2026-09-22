# 2026-09-22, entry 138: each version's notes cover only what changed since the version before it

Alan: "The release notes should show changes since the last release. For instance, GroupLab 0.2.0-nightly.35 should only show changes since GroupLab 0.2.0-nightly.31."

The published notes for `v0.2.0-nightly.35` still open with the hand-written **"Since nightly 18"** block from entry 132 section 1.6. That catch-up list belonged in the first readable nightly only, once; it is now repeated on every build, so each version appears to contain months of changes.

1. **The rule**: a version's notes list exactly the changes between the previous **published** version on the same train and this one. Nightlies that were cancelled or skipped before publishing (entry 123's freshness check) do not count as versions: their changes roll into the next published one. So nightly 35's notes are everything from nightly 31 (exclusive) to nightly 35 (inclusive), and nothing older.
2. **Find the previous published version from the releases, not the rolling tag.** Read the newest published `v<version>-nightly.N` tag lower than this build (the per-build tags the workflow keeps), and take the `Release-note:` trailers in that range. Do not use the rolling `nightly` tag as the start, since it moves.
3. **Remove the "Since nightly 18" block from the generator for good.** It stays exactly once, in `docs/RELEASE-NOTES.md`, as the entry for nightly 31 (the first build with readable notes), where the release notes page shows it in its place in history.
4. **The release notes page and `docs/RELEASE-NOTES.md`** follow the same rule: every version's block holds only its own changes. Check the entries written for entry 136 and correct any that overlap.
5. **The in-application update bar** is the one place that should combine versions: someone on nightly 31 offered nightly 40 should see the changes of every version from 32 to 40, newest first and grouped by version, because all of them are new to that person. Build that from the per-version notes, not by re-reading history.
6. **Tests**: a generated history with published and skipped nightlies, checking that each version's notes start at the previous published version and that the update bar combines exactly the versions between the installed build and the offered one.
7. **Do not edit published GitHub releases**; the next nightly is the first with correct notes. Republish the website once `docs/RELEASE-NOTES.md` is corrected.
8. A plain `Release-note:` trailer.
