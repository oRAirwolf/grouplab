# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-30, after entries 288 to 309 (the Android self-updater, the iOS app and its parity with Android, colored
bulls, export and import of everything in one file, the detection scoreboard, the Mac signing path, Home A) and nightly 133.

---

## In flight

- **iOS until 2026-10-01 02:00 UTC** (entries 290 and 296): on main and proven on the simulator: every tab, the imaging, the 25-shot
  sample identical to the desktop, Apple Photos, Files, sharing into GroupLab (a share extension), share, print and paste, the parity tour
  of every feature, Home A, the torch on Auto a step at a time (entry 302 item 3), and a `.grouplab` data file opened from Files (entry
  307). The nightly's iOS job builds unsigned until request 55's secrets; the camera, torch and level wait for the iPad sitting.
- **GroupLab Dev updates itself** (entry 288): the Dev APK only, never the AAB, `UPDATE_PACKAGES_WITHOUT_USER_ACTION` for a silent second
  update. The Fold proved the first prompt; the silent second update on the tablet (nightly 128) and the Fold (129) waits for the devices
  to be reachable over adb again; `docs/ANDROID.md` section 17 has the results so far.
- **Export and import everything** (entry 307): one `.grouplab` file that any GroupLab reads, merged without overwriting, conflicts listed
  first; on Android, the desktop and iOS.
- **Colored bulls** (entry 297): red or blue bulls on every sheet, found from the photo; question 75 on the ink they use is with planning.
- **The detection scoreboard** (entry 308): `grouplab scoreboard` against made-up targets in CI and the local corpus by hand.
- **The Mac build can be signed and notarized** (entry 306) once request 55's Mac steps give it a certificate.
- **The phone does what the desktop does** (entries 258, 259, 290): `docs/PHONE-PARITY.md` has an Android and an iOS column; the site
  build fails on a feature with no row.

## The next three

1. Entry 290's end-of-window summary in `docs/notes/for-alan.md` at 2026-10-01 02:00 UTC (its section 5), then Android and desktop work
   at their normal share.
2. Entry 288's no-tap second update on the tablet and the Fold as soon as adb reaches them; record it in `docs/ANDROID.md` section 17.
3. The device sitting (request 50): the camera fixes, the torch on Auto, red bulls (request 57), the card photo.

## Blocked, and on what

- **Signing for iOS and the Mac**: request 55 (Apple enrollment, eight secrets, and the Developer ID certificate).
- **The phones**: not reachable over adb since 2026-09-30 morning; their wireless debugging adverts refuse connections.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 section 3.2.** Request 16: the Mac tester's trackpad check.

Open requests in `docs/notes/for-alan.md`: **12** (56 the printer scale; 50 the device sitting; 57 red bulls; 58 store-bought targets for
the detector; 54 a plain target; 55 the Apple and Mac steps; 46 the backups on 4 October; 38 the Store; then 33, 9, 16 and 20).
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

- **Last nightly:** 0.2.0-nightly.133 (2026-09-30), with the export and import, colored bulls and Home A. The next carries the iOS torch,
  the iOS data file, and the test fix that kept 133 waiting.
- **The site follows each push by itself**, but not the nightly's own notes commits; it serves 40c85b6 (nightly 133's refresh).
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ. The planning session's files are not committed, so while a run is working through them the
line reads what the repository holds, and the test fails locally until the last is done.

**Holds:** none

## Things that would surprise somebody who was not here yesterday

- **The logs were split again** on 2026-09-28 (entry 160's rule): the live files keep the newest fifteen entries and twelve results
  sections; the rest is whole in `docs/notes/archive/`. A question is taken as open only when its line reads `**Status: open`.
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
