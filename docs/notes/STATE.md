# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-12, entry 414 (before it: entries 413, 412 and 411).

---

## In flight

- **Entry 414 done** (the overnight queue): a 600 dpi PNG decoded once (opening a scan 1020 to 823 ms), the solver warmed in the
  background; reading the codes at once **taken back** (it crashed Linux in native code, 3 of 12 gate runs); a force-stop in the middle
  of a reading does not stop the next start (5 of 5 on CI's emulator, checked after every nightly now); proof rows 4, 11, 29 moved;
  **the next range kit is ready** in `C:\Dev\grouplab-local\range-next\` (for-alan.md says so, no request).
- **Entry 413 done**: at text sizes where a name would not fit, the phone's bottom bar shows its icons alone (`NavBar`), with its own
  Features picture, `fold-targets-largest-text`.
- **Entries 406 to 412 done**: no shot sheets are left from past trips (entry 412); the phone's text follows Android's (411).
- **The usage guard stops at 85% of the new week**, 88% for a finishing block (`scripts/usage-guard.js`); 9% on 2026-10-09 05:49 UTC.
- **The proof checklist**: rows 6, 8, 11, 13, 15, 16, 21 and 23 proven by tests (11 and 13 on rendered sheets), 31 met; every other
  row names its material (entry 414 section 4).
- **Waiting on Alan:** requests 83 (the M834 from the computer), 70, 72, 74 to 77; 86 later.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: done in entry 324 section 1 (da9cf190): the far column from 0.08 to 0.01 or 0.02 in on the 9 and 15
  degree photos, nothing else changed. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Chronograph files** (Phase 5): every one of Alan's Xero exports reads, and a timed string proposes its pairing (entry 342);
  checking a proposal needs a sheet shot with its string.
- **Velocity regression** (Phase 5): on both screens since entry 323, in the conditions entered on Ballistics since 329; a
  session's own conditions would need a form (DESIGN NEEDED if wanted); the phone's picture waits for request 50's sitting.
- **Synchronization** (Phase 7): the options paper is `docs/notes/sync-options.md` (entry 324 section 2), for Alan to choose from.
- **iOS** (Phase 8): in a public beta through TestFlight; the device checks need a sitting (50), GroupLab Dev on the iPad request
  61, an App Store release Alan's word.
- **Performance** (Phase 9): hole finding on a scan (entry 392) and the phone's reading of a photograph (entry 400); the next waits for planning.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Whatever the inbox brings.
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

Four, all in `docs/QUESTIONS-FOR-PLANNING.md` (96 answered by entry 411 and archived 2026-10-12):

- **97** whether the iPhone should follow Dynamic Type, as Android now follows its text size (entry 411)
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **36** a light installer, measured

## Builds and the site

- **Last nightly:** 0.2.0-nightly.190 (2026-10-12 15:39 UTC, from 0b11c037): entry 414 whole. 189 carried 413 and 414 sections 1
  and 2 and the take-back of reading codes at once; 188 carried entry 413.
- **The site** follows main by itself (website.yml), but not the nightly's own [notes] pushes; 188 and 190 were dispatched by hand
  (one HTTP 500 from GitHub first).
- Crash reports open: none. Issue 28 (nightly 183, Android: the exception behind 27) closed 2026-10-12 as fixed by entry 401 in nightly 184.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **Text sizes go through `TextSize.Of`** (entry 411): a size set in code on a phone screen that skips it does not follow Android's
  text size. The desktop and the iPhone keep the sizes as they are.
- **The database is schema version 3** (entry 407 section 5): a new build's first open copies `grouplab.db` to `grouplab.pre-v2.db`.
- **OpenCV's QR detectors are never run on several threads** (entry 414): reading a sheet's codes at once crashed Linux in native code
  in 3 of 12 gate runs and hung macOS before. A job dying with exit 134 or 139 is tabulated against what was in effect, not rerun.
- **A force-stop in the middle of a reading is checked after every nightly** (`scripts/android-force-stop.sh`): it did not stop the
  next start in any of five rounds. Run 37735635656's "nothing ran" was most likely its own walk's faults of that time.
- **The solver is warmed after start-up** (`SolverUse.WarmInBackground`): the first trajectory's 100 ms was tiered compilation.
- **A PNG's grey is made from its colour decode as libpng makes it** (`ImageLoader.GreyLikeLibpng`), only where the file names no
  gamma, colour space or profile; `ImageLoaderSameResultTests` holds it to the bit.
- **`docs/notes/external-status.md`** holds TestFlight's and the Store's state, written by their workflows when it changes; read it
  at the start of a run (entries 335 and 336).
- **Helpers only when Alan asks, ending at the week's reset; chores to chores-haiku, wording to docs-sonnet** (CLAUDE.md, 2026-10-08).
  A finished worker's subjects are compared with main before its worktree goes to the trash.
- **A UI dump cannot see the phone's camera screen**; the device check reads its `camera.layout` log line instead.
- **A photograph's scale comes from the printer chosen**, where one is, across and down; a scan's own measured scale always wins.
- **`android/GroupLab.Android/` ships** (entry 275): a change to the Android application alone now starts a nightly and is in its notes.
- **The check page is in the library but is not a target**: it counts as no sheet in the README, and a picture of it opened as a target
  goes to the printer check. A value on screen is tappable exactly when it shows a unit (`UnitTap.KindOf`).
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
