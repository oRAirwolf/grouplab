# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-29, after entries 281 to 283 (the camera test's fixes, Alan's screenshots, the shutter) and 278 (CSV B, the
spread audit, a shot left out left out everywhere, the iOS plan) and 279 (the App Store permission, Marking A, Fudd buster mode,
saving A), 280 in part, 284 (Behind the curtain), 285 (the README's list) 286 (nightly 123 on both devices), 287 (the product picture on
the home page) and 288 in part (GroupLab Dev updates itself; its device check waits for two nightlies). Entry 280's screens are next.

---

## In flight

- **iOS until 2026-10-01 02:00 UTC** (entry 290): the phone's screens are in `mobile/GroupLab.Mobile` (plain .NET, both heads link it,
  `IPhonePlatform` is the only way to the OS). OpenCV for iOS is being built on the branch `ios/opencv` by a worker; the iOS head and the
  simulator job come next, then section 6's parity, screen by screen. The checklist is at the top of `for-alan.md`.
- **GroupLab Dev updates itself** (entry 288): `AndroidUpdates` in Core, the `Updates` folder in the Android project compiled only with
  `-p:GroupLabUpdater=true` (GroupLab Dev's APK only, never the AAB), and the Dev APK listed in the signed manifest as `android apk-dev`.
  Nightly 125 is the first with it: install it over adb once, then the next nightly should arrive by itself; record the first prompt,
  whether the second is silent, data kept and minutes from publishing (`update.installed`) in `docs/ANDROID.md` section 17.
- **The phone's camera screen is rebuilt** (entry 260): a native Capture B over the live preview, Guided and Manual, the quality bar,
  the torch on Auto, and Feedback B checking every picture with a score from 0 to 100 (`PictureCheck`). Alan's camera test on nightly 121 went badly;
  entry 281's fixes (level, torch, lifecycle, one field of view, steady words, Camera and Result buttons, the sheet offered when codes
  fail) are built and wait for a device check on the next nightly.
- **A curled sheet is registered through every marker** (`MarkerMesh`, a thin-plate spline) when the radial fit keeps fewer than half
  the corners; a saved session keeps the mesh as its corners (entry 260).
- **The printer check** (entries 271 to 273): the check page (`GL-SCALE-LTR-1`, `GL-SCALE-A4-1`, grid style 4, question 67), the
  three-screen wizard on both platforms, card photos (`CardCheck`), caliper and ruler readings, scans, Printers in Settings, offers at
  first run and first print, and the paper-edge check on every photo (`PaperEdgeCheck`). A profile holds across and down.
- **Tap a number to switch units** (entry 273): `UnitTap` on the desktop's figures, Compare and Ballistics and the phone's tiles and rows;
  numbers inside sentences and the full table's bare cells are not tappable yet.
- **The phone does what the desktop does** (entries 258, 259): full figures, the bulls you fired at, Shots Needed to Zero, compare loads,
  Ballistics as a fifth tab, the set as a checklist and the scan pill are on the phone; `docs/PHONE-PARITY.md` holds the rest (marking
  by touch, CSV, large sheet advice, a picture shared in), and the site build fails on a feature with no row.
- **Any target, and the sheet as the fast lane** (entry 270): the README, the home page, the tour index, Features and the user guide
  lead with it, with a sample target marked by hand as the picture (entry 274).
- **The README is Alan's chosen design** (entry 266), its pictures made by `scripts/readme-images.py`; `scripts/consistency.py` audits
  the README, the site and the assets (entry 267), including retired wording in `docs/RETIRED-WORDING.json`.
- **The donor pack** is one sheet of each style, built from the library (entry 264); /download/ lists Android (entry 265).
- **GroupLab Dev has a black idle screen** for the OLED devices (entry 268); every device session ends on it.

## The next three

0. Entry 290 (iOS) has most of the effort until Wednesday 8 pm Mountain; entry 289's 2 MOA sheets run in their own worker; entry 288's
   device check as soon as nightly 125 and the one after it are out.
1. The device sitting on the nightly that carries entries 281 to 283: the camera fixes checked, `scripts/shutter-timing.py` on both
   devices, the phone and tablet pictures (253 section 3), the inner Fold screen (257), torch strength (262), the card photo (273).
2. Entry 280 section 2's remaining screens, Share A and the dated report, and the desktop's equivalents of all five, after the sitting
   confirms the phone screens built tonight (Shots A, zero from this group with the Ballistics offset, several aim points); then the
   desktop's tabs (entry 281 section 2). Question 70 is answered B: each CEP table cell shows and switches its own unit.
3. The 2 MOA sheets once planning draws their layout; question 67 from request 53's photographs.

## Blocked, and on what

- **Nothing waits on a device**: both are paired again and their wireless authorizations no longer expire (entry 280 section 4).
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 section 3.2.** Request 16: the Mac tester's trackpad check (his thanks, to Fenix, is in the README since entry 189).

Open requests in `docs/notes/for-alan.md`: **10** (50 the device sitting; 53 the card outline test pages; 54 a store-bought target; 55 the Apple steps; 46 the backups on 4 October; 38 the Store; then 9, 16 and 20). for-alan.md's own count says the same.

## Open questions

Six, all in `docs/QUESTIONS-FOR-PLANNING.md`; 65 and 66 were answered by entry 274, 68 by entry 276.

- **67** the printer check page as grid style 4, and its card outline 3 mm outside the card (with Alan)
- **51** which hole centre GroupLab should report; agreed to wait on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's center means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.124 (b856d1f), on both devices; nightly 125 will carry the Android updater.
- **The site follows each push by itself**; it serves b856d1f and later.
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
