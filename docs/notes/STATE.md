# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-17, entry 437 (before it: entries 440 and 436, entry 435, entries 432 to 434).

---

## In flight

- **Entry 437 (2026-10-17)**: the user guide shows 26 pictures; 15 carry numbered markings that name controls from each screen's
  positions file (`<screen>-bounds.json`, written by the render walk), and the site build holds every name. Compare loads has no
  positions file until the weekly screenshot job retakes it with the test data.
- **Entry 440 (2026-10-17)**: concept B, the new home page, is built at grouplab.org/testpage/ for Alan to approve: noindex, not in the
  sitemap, linked from nowhere (a build check). **The home page at / is unchanged** until Alan approves and an entry swaps it in.
- **Entries 418 and 421 to 436 are done**: the guide's subsections and links (436); CI fails on a guide PDF behind its guide (435);
  the iOS self-test is green (432); the selected shot is ringed in magenta (430, question 100); #31 to #34 are closed.
- **The usage guard** stops at 85% of the week, 88% to finish a block; this block's own limit is 70% (Alan, 2026-10-13).
- **The proof checklist**: rows 6, 8, 11, 13, 15, 16, 21 and 23 proven by tests (11 and 13 on rendered sheets), 31 met; every other
  row names its material (entry 414 section 4).
- **Waiting on Alan:** requests 83, 70, 72, 74 to 77; 86 later.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: done in entry 324 section 1 (da9cf190): the far column from 0.08 to 0.01 or 0.02 in on the 9 and 15
  degree photos, nothing else changed. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Chronograph files** (Phase 5): every one of Alan's Xero exports reads, and a timed string proposes its pairing (entry 342);
  checking a proposal needs a sheet shot with its string.
- **Velocity regression** (Phase 5): on both screens since entry 323, in the conditions entered on Ballistics since 329; a
  session's own conditions are entered under the block (entry 416); the phone's picture waits for request 50's sitting.
- **Synchronization** (Phase 7): the options paper is `docs/notes/sync-options.md` (entry 324 section 2), for Alan to choose from.
- **iOS** (Phase 8): in a public beta through TestFlight; the device checks need a sitting (50), GroupLab Dev on the iPad request
  61, an App Store release Alan's word.
- **Performance** (Phase 9): hole finding on a scan (entry 392) and the phone's reading of a photograph (entry 400); the next waits for planning.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Entries 438 (a statistics primer) and 439 (a guided demo in the application).
2. Request 83, the M834 printing from the computer, now that the phone's prints are true both ways.
3. The range kit in `range-next` waits for Alan's next trip; its results come back as an entry.

## Blocked, and on what

- **Entry 380 section 3**: measuring the M220 labels waits on request 77's photos in `m220-labels`.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **22** (86 aim points through the other optics, for later; 83 the M834 from the computer; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Five, all in `docs/QUESTIONS-FOR-PLANNING.md` (99 answered by entry 430 and archived 2026-10-15):

- **100** the selection's magenta is near the light theme's fourth sheet and two bull marks (entry 430)
- **98** several pictures of one single-page design dropped together: a tab each meanwhile (entry 423)
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **36** a light installer, measured

## Builds and the site

- **Last nightly:** 0.2.0-nightly.196 (2026-10-16 11:20 UTC, from 0206b663): entries 432 and 433, the caliber question after a result. 195 carried 431.
- **The site** follows main by itself (website.yml), but not the nightly's own [notes] pushes; those are dispatched by hand.
- Crash reports open: none. **#35** (Quack: a pooled set's small sheet showed sheet 1 only, a bull click picked by nearness) fixed and closed 2026-10-16, in nightly 197.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: entries 438 and 439.

## Things that would surprise somebody who was not here yesterday

- **The server's logs are in Denver time** (-0600, -0700 from 1 November), not UTC; a log paste converts its window (request 99).
- **The zero's words are `ZeroWords`** (entry 424), read by the desktop panel, the report and the phone; `ZeroAxis.Rounded` has the
  clicks on every axis, `Clicks` only on one worth dialling.
- **A tab can hold a set** (entry 423): `OpenTarget.Set` parks each picture of the set as a tab parks a target, and the pool is worked out
  afresh whenever it is shown; a drop onto a tab holding a target always opens beside it. "Read these as one set" is gone.
- **A Features sentence changed since the newest nightly fails the site build** (entry 421) unless one of its note phrases is still to
  come, or it carries `"reworded"` naming that nightly; a widened feature reads "Since nightly.N, changed in nightly.M".
- **A hole can be a ring** (entry 417): on a flatbed with a white lid a hole is as light as the paper; detection fits a circle through its
  rim (`FitCircle`) and the snap does the same. **Restyling adds the new styles before removing the old** (a macOS crash in between).
- **ios-app.yml is green again** (run 38883488382, a2a0345a) after red from 2026-10-06: the self-test waited on the caliber question.
  "Real taps" can still lose the first Next on Targets (5 of 18 red runs); `keyboard.bar` and `keyboard.next` say where.
- **The committed corpus counts record is behind main** on 29 of 55 images (entry 433 found it; rewriting it is planning's review).
- **The database is schema version 3** (entry 407 section 5): a new build's first open copies `grouplab.db` to `grouplab.pre-v2.db`.
- **OpenCV's QR detectors are never run on several threads** (entry 414): reading a sheet's codes at once crashed Linux in native code
  in 3 of 12 gate runs and hung macOS before. A job dying with exit 134 or 139 is tabulated against what was in effect, not rerun.
- **`docs/notes/external-status.md`** holds TestFlight's and the Store's state, written by their workflows when it changes; read it
  at the start of a run (entries 335 and 336).
- **Helpers only when Alan asks, ending at the week's reset; chores to chores-haiku, wording to docs-sonnet** (CLAUDE.md, 2026-10-08).
  A finished worker's subjects are compared with main before its worktree goes to the trash.
- **`android/GroupLab.Android/` ships** (entry 275): a change to the Android application alone now starts a nightly and is in its notes.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel. It holds only open requests and the latest summary
  (entry 317); answered ones are whole in `docs/notes/for-alan-archive.md`.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **A failing site check holds back only its pages** (entry 387): whole-site checks still stop it; held pages keep grouplab.org's copy and
  are listed in website.yml's summary and the weekly consistency issue; each gets one line in for-alan.md when it appears.
- **The Phomemo app prints a Letter sheet at 94.7 percent** (request 73): only GroupLab's own direct print is true size.
- **Tag36h11 470 to 549 are scale labels' and 555 to 586 scale markers' (entries 372 and 365)**: the sheets' markers must stay below; the sheet look and the validator know.
- **A picture with a bank card in it is never sent**, and marking by hand works on a blanked copy (entry 365 section D).
