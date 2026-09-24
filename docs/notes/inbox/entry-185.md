# 2026-09-24, entry 185: the releases page shows two copies of nightly 94 and a test data release

Alan, looking at the releases page after entry 168's rewrite: he could not tell why nightly 94 exists or why
it has no notes, and the page lists three releases that look alike. Nightly 94 itself is explained (entry 168:
the old gate counted a non shipping path as the application, and its rewritten body now says honestly that
nothing changed). What is still confusing is the page. Do this after entry 184; it is small.

## 1. The rolling `nightly` release looks like a second build

The page shows "GroupLab 0.2.0-nightly.94" (tag `v0.2.0-nightly.94`) **and** "GroupLab nightly,
0.2.0-nightly.94" (tag `nightly`), with the same commit and the same body. To a reader that is two builds.

1. Say what uses the `nightly` tag release: the updater, the download page, or nothing. If nothing needs it,
   stop creating it and delete it; that deletion is Alan's approval, so put it in `panel.md`.
2. If something does need it, retitle it so it cannot be mistaken for a build: **"Latest nightly (always the
   newest build, moves with every build)"**, with a one line body linking to the numbered release it points
   at, and none of the notes repeated.
3. Its body still carries the platform line saying macOS has never been run on a Mac. Entry 166 corrects the
   statement; make sure this release is regenerated from the one source when it does, like every other copy.

## 2. The test data release sits among the builds

"Test data, not a build", tag `test-data`, from entry 171 section 6, is listed as a pre release between two
builds. It is correctly named, but it should not be on a page people read to find builds.

Move it out of sight without losing what it does: a **draft** release is invisible on the public page, and CI
can still fetch its assets with the workflow token. Check that works, then make it a draft. If it does not,
say so and propose where else the test files can live. Do not move the files anywhere that puts them back
into the repository's history.

## 3. The next nightly

Tonight's work includes real changes to the application: pan by default, the calibre names, the freezes,
the zero correction's distance, the scan in real inches. So the next scheduled nightly will be built, and its
notes must list them under "What you will notice" in plain words. Check that before it publishes; it is the
first nightly since entry 168 fixed the gate and the notes, so it is the proof both fixes work.
