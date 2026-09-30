# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-30 12:15 UTC, when Alan stopped the overnight run at a clean point: three workers stopped, their
work pushed as branches (below), main unchanged by it.

---

## In flight

- **iOS until 2026-10-01 02:00 UTC** (entries 290 and 296): on main and proven on the simulator: every tab, the imaging, the 25-shot
  sample identical to the desktop, Apple Photos, Files, sharing into GroupLab (a share extension), share, print and paste, the parity tour
  of every feature, Home A, the torch on Auto a step at a time (entry 302 item 3), and a `.grouplab` data file opened from Files (entry
  307). Nightly 134 was signed and sent to TestFlight; 135 and 136 were signed but not sent (next three, 1). Entry 310's testflight
  workflow keeps GroupLab Team and Public Beta on one build once Alan makes the groups (request 59). iPad and landscape are
  photographed and fixed (the short build code, the bar to both edges, a large iPhone's result side by side). The camera, torch and level wait for the iPad sitting.
- **GroupLab Dev updates itself** (entry 288): the Dev APK only, never the AAB, `UPDATE_PACKAGES_WITHOUT_USER_ACTION` for a silent second
  update. The Fold proved the first prompt; the silent second update on the tablet (nightly 128) and the Fold (129) waits for the devices
  to be reachable over adb again; `docs/ANDROID.md` section 17 has the results so far.
- **Export and import everything** (entry 307): one `.grouplab` file that any GroupLab reads, merged without overwriting, conflicts listed
  first; on Android, the desktop and iOS.
- **Colored bulls** (entry 297): red or blue bulls on every sheet, found from the photo; question 75 on the ink they use is with planning.
- **The detection scoreboard** (entry 308): `grouplab scoreboard` against made-up targets in CI and the local corpus by hand.
- **The Mac build is signed and notarized** (entry 306): shipped from nightly 135 on both architectures; the xattr Terminal command is
  gone from the README and the download page, which say Windows and Linux are unsigned and the Mac build is approved by Apple.
- **The phone does what the desktop does** (entries 258, 259, 290): `docs/PHONE-PARITY.md` has an Android and an iOS column; the site
  build fails on a feature with no row.

## The next three

1. **Review and merge the three overnight branches** (pushed, not on main, not proven in CI; each commit has its trailers):
   `overnight/reading` (c1516855): entry 313 section 1 (Cancel always works, a time limit, the screen lock, `read.stage` timings, crash
   report 11) and section 2 (the panel above the preview, no stray dash), 313 section 1.4 (no doubled picture for the codes), entry 311
   sections 1 (Guided fires about a second after ready, both platforms) and 2 (the level). `overnight/screens` (15643ee6): crash report
   12 (Shots Needed to Zero calculated twice), all of entry 312, then a **WIP** commit of entry 314 (table, lookup, setting, unfinished).
   `overnight/tooling` (3f4e571c): entry 311 section 3 (Send diagnostics, the Files folder, `scripts/` for the iPad over USB, TestFlight
   feedback), entry 315 section 2 (scenario files) and the iOS GroupLab Dev app on the simulator, then a **WIP** commit of the bridge.
   Run each branch's tests and the `ios app` workflow, merge the finished commits, then fold entries 311 to 313 (and 314, 315 when done).
2. **Prove the TestFlight upload** on the first nightly that changes the app (63eadc57 made the encryption check print and warn); its log
   says what the signed package carries (the unsigned device build carries false in both bundles, read from nightly 136's log).
3. Entry 290's end-of-window iOS summary in `docs/notes/for-alan.md` (the window ends 2026-10-01 02:00 UTC), then entry 314's rest and
   entry 315's sections 1, 3 and 4.

## Blocked, and on what

- **TestFlight distribution and the public beta link**: request 59 (the two groups, then the public link).
- **The phones**: not reachable over adb since 2026-09-30 morning; their wireless debugging adverts refuse connections.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 section 3.2.** Request 16: the Mac tester's trackpad check.

Open requests in `docs/notes/for-alan.md`: **12** (59 the TestFlight groups and the iPad; 56 the printer scale; 50 the device sitting;
57 red bulls; 58 store-bought targets for the detector; 54 a plain target; 46 the backups on 4 October; 38 the Store; then 33, 9, 16 and 20).
for-alan.md's own count says the same.

## Open questions

Seven, all in `docs/QUESTIONS-FOR-PLANNING.md`; 69 to 74 were answered by the run's own messages.

- **75** colored ring bulls use more ink than black on a color inkjet (A proposed: accept it)
- **67** the printer check page as grid style 4, and its card outline 3 mm outside the card (with Alan)
- **51** which hole center GroupLab should report; agreed to wait on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's center means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.136 (2026-09-30, about 11:05 UTC): the iPad and landscape fixes and the short build code; Windows,
  Linux, Android and the notarized Mac all published; the iOS build signed but not sent (next three, 1). Nightly 135 was the first with
  the Mac build notarized.
- **The site follows each push by itself**, but not the nightly's own notes commits; it serves f2328b6b (the Mac download without the Terminal command).
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ. The planning session's files are not committed, so while a run is working through them the
line reads what the repository holds, and the test fails locally until the last is done.

**Holds:** none

Waiting on this computer: entries 311 to 315 (and entry 314's CSV), read and partly built on the overnight branches (next three,
1); none folded yet. Crash reports 11 and 12 are fixed on those branches and still open; 9 reopened with a different fault.

## Things that would surprise somebody who was not here yesterday

- **The logs were split again** on 2026-09-28 (entry 160's rule): the live files keep the newest fifteen entries and twelve results
  sections; the rest is whole in `docs/notes/archive/`. A question is taken as open only when its line reads `**Status: open`.
- **A push to main while a nightly runs cancels it** once that push's build passes (the nightly's concurrency group), iOS job included.
  A notes-only commit headed `[notes] ` skips CI and cancels nothing.
- **The Mac packaging can be run by hand**: `gh workflow run package.yml --ref <branch> -f ref=<sha>` proves the signing and
  notarization on a commit without a nightly, and publishes nothing.
- **A UI dump cannot see the phone's camera screen**: its views are native, inside Avalonia's host. The device check reads the screen's
  own `camera.layout` log line instead (`scripts/device-capture-check.py`).
- **Starting GroupLab Dev's main screen over the idle screen makes a second window, which crashes**; press Back first.
- **A photograph's scale comes from the printer chosen**, where one is, across and down; a scan's own measured scale always wins.
- **`android/GroupLab.Android/` ships** (entry 275): a change to the Android application alone now starts a nightly and is in its notes.
- **The check page is in the library but is not a target**: it counts as no sheet in the README, and a picture of it opened as a target
  goes to the printer check. A value on screen is tappable exactly when it shows a unit (`UnitTap.KindOf`).
- **A disc can be a square** (entry 243 section 4): its diameter is the diagonal, and a square is measured as itself.
- **A zeroing grid is drawn by its style**: style 3's strokes, ticks, numbers, legend and bar are fixed by `GridStyle3`. A real hole in
  black is as large as on white; a synthetic one is not.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **The tour and Features have two sides** (entry 249): every stop and phone feature needs a phone screenshot or words.
- **Every published sentence has its backing**: `scripts/claims.py --check` fails CI otherwise (entry 159).
- **Nothing under `website/server/` may hold a carriage return**: it is copied to Linux as it is.
- **Nothing is written into a HestiaCP `conf/web/<domain>/` folder** but the include itself.
- **Alan's own photographs and scans may be published**, and so may what he passes on from Unholy (also TNA) and his other friends;
  the 2026-09-16 friend scan never is. Justin is credited as "Justin" only.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel.
