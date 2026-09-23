# 2026-09-23, entry 159: audit the website, the README and the application for claims that are not true

Alan: "The website and readme should be scrutinized for contradictions that I may have missed as well."

Entry 152 fixes two claims he caught by reading one tour page. One of them is contradicted by a
research article the same site publishes. Two found by eye on one page is not a good rate, and the
right response is not to read harder, it is to build the thing that makes a contradiction fail a build.

Do this after entry 153, so the research standard is in place before the articles are audited, and
before entries 155 to 158 write a great deal of new text.

## 1. Build the claims register first

Read every published surface and write down every factual claim it makes. The surfaces are:

- `website/**`, every page, including all thirty research articles and every tour page
- `README.md` and every other markdown file at the repository root
- `docs/**`
- every string a user reads inside the application, which `OneWayOutTests` and the glossary work of
  entry 154 already give you a way to enumerate
- the release notes and the release bodies on GitHub
- `docs/PLATFORM-SUPPORT.md` and every place entry 147's statement is repeated

A claim is any sentence that asserts something checkable: what the software does, what it cannot do,
a number, a measurement, a limit, a version, an address, a licence, a platform, a name.

Write them into `docs/CLAIMS.md`, one per line, with the file and line it came from.

## 2. Classify every claim

For each one, decide which it is and record it:

1. **Backed by code.** Name the file and the symbol. A claim about behaviour whose code cannot be found
   is not backed, it is remembered.
2. **Backed by a measurement.** Name where in `docs/PHASE0-RESULTS.md`, `docs/PHASE1-RESULTS.md` or
   `docs/STATISTICS.md` the measurement lives, and the date it was taken.
3. **Backed by a decision.** Name the entry in `docs/NOTES-FROM-PLANNING.md`.
4. **Unbacked.** Nothing found. This is the list that matters.

Every unbacked claim gets a line in the report saying what it would take to back it, and then one of
three things happens: it is verified and moved to a backed category, it is reworded so that what it
says is what is true, or it is deleted.

## 3. Then look for the contradictions

With the register built, the contradictions fall out of it rather than having to be spotted by reading.
Look for these shapes in particular:

1. **Two claims that cannot both be true.** The print scale pair in entry 152 is the example: the tour
   says a scale error cannot be recovered and `printer-true-size` says GroupLab detects and corrects it.
2. **A number that appears more than once with different values.** Calibre diameters, tolerances,
   sample sizes, the print scale figures, version numbers, file size limits, the number of articles,
   the number of target sheets, the number of supported platforms.
3. **A claim of a feature that no screen reaches.** Question 37 is exactly this: the point of impact
   correction was built, correct, and reachable from nothing. Anything the site says a shooter can do,
   a shooter must be able to find. Check every "you can" sentence against a screen.
4. **A sentence that appears in several places in slightly different words.** That is a contradiction
   waiting to happen even if the versions agree today. The platform statement from entry 147 is
   supposed to come from one source; verify that it actually does, everywhere, including the GitHub
   release bodies.
5. **Stale counts and stale states.** Anything that says "eighteen articles", "twenty two sheets",
   "nightly NN", "currently", "at the moment", or "not yet". Each one is either generated from the
   thing it counts or it is wrong on some future day.
6. **A claim about another product or another person's work.** Check it is accurate and check it is
   necessary. Remove any comparison that is not both.

## 4. Fix, with the same rule as entry 145

Every wrong claim is corrected in plain words. Where a claim was wrong because two places each wrote
their own version of the same sentence, the fix is not to make the two versions match; it is to make
one of them the source and have the other read from it. Matching by hand is how they came to disagree.

## 5. What stops it happening again

1. **One source per repeated statement.** Extend the mechanism already used for the platform statement
   and for `website/links.json` to every statement that appears in more than one place. A drift test
   for each, the way entry 147 did it.
2. **Generated counts.** Any number that counts a thing in this repository is computed at build time
   from the thing it counts. Never typed.
3. **The banned sentence list grows.** It already holds entry 145's filler sentence and gains entry
   152's two claims. Add every false claim this audit finds, so the exact wording cannot come back.
4. **A claims register test.** `docs/CLAIMS.md` lists every claim with its backing. The build fails on
   a claim marked unbacked, and fails on a published page whose checkable sentences are not in the
   register. That is the check that makes this audit worth doing once rather than every quarter.
5. **The register is part of the definition of done.** A new page or a new article adds its claims to
   the register in the same commit, the way a new figure adds its caption.

## 6. Report

In the run report: how many claims, how many in each of the four categories, how many contradictions
found, and the full list of what was corrected. If the unbacked list is long, say so plainly rather
than working through it quietly; the size of that list is itself the most useful finding here.
