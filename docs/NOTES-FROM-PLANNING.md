# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

**How entries arrive, from entry 44 on.**
- **Delivery:** the planning session delivers each new entry as its own file in `docs/notes/inbox/`, named `entry-NN.md`, and never writes this log or any other existing file.
- **The only writer:** the Claude Code session is the only writer of this log.
- **Actioning includes three steps:** folding the entry into the top of the log, setting its status, and deleting its inbox file.
- **Several files waiting:** fold them in ascending entry number, so the newest ends up first.
- **Why:** two writers rewriting one file with no locking overwrote this log once, and separate paths cannot collide.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## The archive

Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the
only written record of why much of this project is the way it is.

- [`docs/notes/archive/notes-2026-10.md`](notes/archive/notes-2026-10.md), entries 322 to 330, 9 of them.
- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 321, 320 of them.

---

## 2026-10-01, entry 346: correction, Garmin Xero has no "monthly export"

**Status: done 2026-10-02, in one commit.** 1: the correction is the next nightly's note, kind changed; the published entry is untouched. 2: the three comments say "a workbook of the strings selected in ShotView, one sheet per string". 3: nothing else said monthly (the built site carries it only from the published notes, the guides and their PDFs never did, and no test or fixture name does); the reader assumes no whole month, its only gap being a deleted shot inside a string. 5: "monthly export" is retired wording, allowed only in the published release notes.

Alan, 2026-10-01 21:31 UTC: "it seems that Claude is under the impression that Garmin [Xero] does monthly exports. I believe you are
getting confused by the naming scheme of the file. If you select more than one session or string when you are doing an export it shows
the month in the file name but it is not necessarily the entire month that has been exported. You should probably go back and fix the
release notes and any other places that this assumption was made."

The mistake was planning's (entry 334 called "Sessions_MAY_2024-MAY_2024.xls" a monthly export). The truth: ShotView exports either one
string ("<name>_<date>_<time>.xls") or **the strings the person selected**, in one workbook with a sheet per string, named after the
month or months of those strings ("Sessions_SEP_2026-SEP_2026.xls"). It may hold a few strings of that month, not the whole month.
Nothing in the reader depends on it holding a whole month; check that is true (no "expect every day of the month", no gap warnings).

Fix every place that says or assumes "monthly", in one commit, before the rest of the queue (small):

1. `docs/RELEASE-NOTES.md` line 78, already published: **do not edit the published entry** (CLAUDE.md); the next nightly's notes carry
   a correction in plain words, for example: "Correction: a Garmin Xero file with several strings holds the strings you selected when
   exporting, not a whole month; earlier notes called it a monthly export." Kind `changed`.
2. Code comments: `src/GroupLab.Core/Records/ChronographFiles.cs` line 82, `src/GroupLab.App/MainWindow.Chronograph.cs` line 104,
   `mobile/GroupLab.Mobile/VelocityPages.cs` line 133: "a workbook of the strings selected in ShotView, one sheet per string".
3. Any app text, guide, README, site page, test name or fixture name that says monthly (git grep found none beyond these, but check
   the built site, the guide PDFs and the Features page texts).
4. `docs/NOTES-FROM-PLANNING.md` keeps entry 334 as written (it is the log); this entry, folded on top, is its correction.
5. Add "monthly export" for Xero to `docs/RETIRED-WORDING.json` so the consistency check catches it if it comes back.

## 2026-10-02, entry 350: a friend's new TestFlight feedback

**Status: done 2026-10-02.** The feedback was filed (issues 15 and 16, 2026-10-01 13:00 UTC, after the main session's last look); a run by hand found nothing more. The half-hourly run looked back one hour while GitHub ran it every three or four, so a scheduled run now looks back a day. Both reports fixed in nightly 154; for-alan.md says so in plain words.

Alan, 2026-10-02 05:04 UTC: "A friend submitted feedback through testflight. Can code retrieve it or do I need to?" Code can: entry 326
files every new TestFlight screenshot, comment and crash as an issue in the private oRAirwolf/grouplab-crash-reports. First thing in this
run: read the open issues there (`gh issue list -R oRAirwolf/grouplab-crash-reports --state open`); if the new feedback is not filed,
run the feedback workflow by hand and say in for-alan.md why the half-hourly run missed it. Then fix what it reports, as bugs a tester
hit (they jump the queue), and say in plain words in for-alan.md what the friend reported, what changed and which nightly carries it.
The tester's name and email never appear in any file.

## 2026-10-01, entry 345: consistency audit, 2026-10-01

**Status: done 2026-10-01, all fourteen.** 5: the platform statement's Store lines changed as facts only, as entries 166 and 306 did, so no line for Alan. 9: iPhone and iPad are on 39 features, held by the site build to PHONE-PARITY.md's fifth column; chronograph files are a sentence in Velocity and the vertical rather than an entry of their own, which would need its own picture. 3: the mounted photograph line stands (31 of 59 registered is still the latest).

The scheduled consistency audit (entry 267 section 2b). Read: README.md, the live site (home, /download/, /features/, /tour/,
/shoot-a-target/, /guides/, /releases/, /support/) as served at 15:50 UTC on 2026-10-01, website/features.json, website/build.py where a
page's words come from it, docs/RELEASE-NOTES.md (nightlies 150 to 153), STATE.md, for-alan.md, external-status.md, PLATFORM-SUPPORT.md,
PHONE-PARITY.md, ANDROID.md, TESTING-GUIDE.md, USER-GUIDE.md, CONTRIBUTING.md and LICENSE, and the commits of the last five days. No open
issue is labelled `consistency`, so nothing below repeats one. The releases page matches RELEASE-NOTES.md through nightly 153, the README's
download table and the download page name nightly 153 and commit 828858f, the credits name only Unholy and Jylee, nothing says a lawyer is
reviewing anything, and "What GroupLab is not" is gone. Fourteen findings follow, the public ones first. Fix them in one change, the
README, the site, the guides and CLAIMS.md together, as entry 267 asks.

### 1. Home page, "Not built yet" names two things that have shipped

- **Where:** https://grouplab.org/ under "What it is today"; source `website/build.py` lines 726 and 727.
- **Says:** "Not built yet: Hole detection on plain paper · Garmin Xero import · hand marking on the phone · iPhone and iPad, being built".
- **Should say:** something like "Not built yet: Hole detection on plain paper · synchronization", with iPhone and iPad moved out of the
  list (they are in a public beta).
- **Evidence:** Garmin Xero import is in nightlies 145 to 150 (RELEASE-NOTES.md lines 64, 65, 78 and 79; README line 453 "Built, not
  proven"; USER-GUIDE.md line 300). Hand marking is on the phone: PHONE-PARITY.md row `other-targets` says "on the phone", and the same home
  page says "Marking by hand is on the computer and, under a crosshair, on the phone" a few paragraphs earlier, so the page contradicts
  itself. The iPhone and iPad public beta is on /download/ and in the README's download table (line 95).

### 2. Home page, the platforms leave out iPhone and iPad and the Microsoft Store

- **Where:** the eyebrow line, `website/build.py` line 649, "Free · open source · GPL-3.0 · test builds for Windows, macOS, Linux and
  Android"; and "What it is today", line 725, "Test builds for Windows, macOS, Linux and Android."
- **Should say:** both name iPhone and iPad (public beta through TestFlight). "What it is today" could add that Windows also has a steadier
  copy in the Microsoft Store.
- **Evidence:** /download/ opens with "Free, for Windows, Mac, iPhone and iPad, Android and Linux"; external-status.md says the Store
  carries 0.2.0.

### 3. The testing guide's "What is not done yet" is out of date, and the support page sends people to it

- **Where:** docs/TESTING-GUIDE.md lines 92 and 93, rendered at /guides/testing-guide/#what-is-not-done-yet, which /support/ links as step
  01, "Check what is not done yet".
- **Says:** "Garmin Xero import is not built. Chronograph readings are typed in" and "The phone does not yet mark a target by hand, nor
  import shots from a CSV file; both are coming".
- **Should say:** neither line. Chronograph readings can come from a file (CSV, Garmin Xero, Labradar and BulletSeeker, the last two
  Experimental), with a proposed pairing; checking a proposal against a sheet shot with its string is what is left. The phone marks by hand
  and imports CSV.
- **Evidence:** RELEASE-NOTES.md lines 64, 65, 78 and 79; PHONE-PARITY.md row `other-targets` (line 29 area) and row `csv` (line 42, "in
  under Sessions since entry 278"); USER-GUIDE.md lines 300 to 310.
- The same section's "mounted photograph gate has one day's material, 59 photographs from 2026-09-20, and GroupLab could not read about half
  of them" may also be stale after entries 321 and 322; check it against PHASE1-RESULTS.md while there.

### 4. The user guide calls the iPhone and iPad build not installable

- **Where:** docs/USER-GUIDE.md line 457, "**On iPhone and iPad** (being built, and not yet installable)".
- **Should say:** "(in a public beta through TestFlight)" or similar.
- **Evidence:** the same guide, lines 28 and 29, gives the TestFlight invitation; /download/ "The public beta".

### 5. The platform statement still treats the Microsoft Store as future

- **Where:** docs/PLATFORM-SUPPORT.md, rendered into README.md (line 194 and line 230) and into /download/ under "Minimums" and "Signing
  elsewhere".
- **Says:** Minimums, Windows: "version 1809 or later for the Microsoft Store copy, once it is published". Signing elsewhere: "A signed
  Windows version through the Microsoft Store is intended in due course". "Why Windows and Linux stay unsigned" says nothing of the Store.
- **Should say:** the Store copy is published (version 1809 or later); a signed Windows version is in the Microsoft Store, and a code
  signing certificate for the direct downloads may still be bought; "Why Windows and Linux stay unsigned" adds that the Store copy is signed
  through the Store and does not show the warning (the download page's own Windows section already says exactly that).
- **Evidence:** external-status.md "Published in the Store: yes", "package version 0.2.0.0"; request 38 closed (commit c6a5f2c3).
- **Note:** the file's header calls the statement Alan's settled wording. Entries 166 and 306 changed it for facts only; this is the same
  kind of change. If you read it as needing Alan's word, add one line to for-alan.md rather than leave it stale.

### 6. The platform statement points at two things that are no longer below it

- **Where:** PLATFORM-SUPPORT.md line 24 (README line 185, /download/) and line 55 (README line 216, /download/).
- **Says:** "macOS blocked the first launch, and the Terminal command below cleared it." There is no Terminal command below: it moved to
  the "Old builds still exist" note, which is not rendered. And "macOS depends on the hardware question below." There is no hardware
  question below.
- **Should say:** for example "macOS blocked the first launch of that unsigned build, and a Terminal command cleared it; builds from
  nightly 135 are signed and open normally", and the hardware clause removed or replaced with what actually decides more macOS testing now.

### 7. Download page, "Only the Windows installer updates itself"

- **Where:** /download/, "Updating"; `website/build.py` line 1019; claim in docs/CLAIMS.md line 3695.
- **Says:** "Only the Windows installer updates itself." The next paragraph then names the Store, GroupLab Dev and Google Play as
  updating, and TestFlight is not named at all.
- **Should say:** "Of the computer downloads, only the Windows installer updates itself", and the second paragraph adds "TestFlight
  installs each new iPhone and iPad beta build". Update the CLAIMS.md line with it.

### 8. Download page, the plain APK card contradicts the page about its key

- **Where:** /download/, Android, "The plain APK"; `website/build.py` line 992.
- **Says:** "The same app under GroupLab's own name, signed like the Google Play copy."
- **Should say:** "signed, with a different key from the Google Play copy". As written it reads as the same signature, against line 988
  ("Signed with a different key from the plain APK"), line 994 and README line 121.

### 9. Features page: no iPhone and iPad anywhere, and no entry for chronograph files

- **Where:** https://grouplab.org/features/ and `website/features.json`.
- **Says:** 41 features list "Windows · macOS · Linux · Android" and 8 list "Android"; none lists iPhone or iPad, though the page's
  Mobile side covers phones. The Garmin Xero and chronograph file notes (features.json lines 17, 18, 21 and 22) are filed under
  `notFeatures`, so the page never mentions that readings can come from a file.
- **Should say:** iPhone and iPad (or iOS) on each feature that PHONE-PARITY.md's last column marks "on iOS", with the site build checking
  that column the way it checks the Android one; and a feature for chronograph files (CSV, Garmin Xero, Labradar, BulletSeeker, the
  proposed pairing), or that sentence added to "Velocity and the vertical" or "Ballistics and hit chances", with its own line in
  PHONE-PARITY.md.
- **Evidence:** PHONE-PARITY.md, for example the `store-targets`, `velocity`, `compare` and `csv` rows, all "on iOS"; RELEASE-NOTES.md
  line 79, "on the computer and the phone".

### 10. Support page: nothing for a phone

- **Where:** https://grouplab.org/support/.
- **Says:** only the desktop route: "the gear at the bottom left ... Report a problem", and recovery from `%APPDATA%\GroupLab`.
- **Should say:** a short phone paragraph: Settings, About, Send diagnostics on Android and on iPhone and iPad; Send Beta Feedback in
  TestFlight (as TESTING-GUIDE.md line 26 already says); GroupLab Dev for Android logs. The recovery paragraph could say it is about
  Windows.
- **Evidence:** PHONE-PARITY.md lines 82 to 88; TESTING-GUIDE.md line 26.

### 11. CONTRIBUTING.md contradicts itself and LICENSE on the section 7 permission

- **Where:** CONTRIBUTING.md line 123.
- **Says:** contributions "may later be offered under an added GPL section 7 permission for app stores".
- **Should say:** what line 7 and LICENSE say: contributions are accepted under GPL-3.0 with the section 7 permission for Apple's App Store
  and TestFlight that LICENSE begins with.
- **Evidence:** LICENSE lines 1 to 14; entry 279 section 1 (NOTES-FROM-PLANNING.md line 2097); README line 652.

### 12. README "Where GroupLab stands" stops short of the phone and two recent features

- **Where:** README.md, Status, "What exists and is tested" (about lines 302 to 318).
- **Says:** "an Android app, in testing" and no iPhone and iPad app; nothing about store-bought recognition or chronograph files.
- **Should say:** an iPhone and iPad app in public beta through TestFlight beside the Android line, and one line each for the five
  recognized store-bought targets (nightly 152) and chronograph readings from a file, marked as the README's phase list marks them.

### 13. README "Deferred" reads as if store-bought targets can only come through the designer

- **Where:** README.md line 486, "Deferred: the full visual designer, and with it the full detector on a bought target", and STATE.md "The
  next three", "Deferred on purpose".
- **Says:** "The same canvas is how a person would trace a store-bought target into a definition ... when either is asked for, both arrive
  together."
- **Should say:** that five store-bought targets are already recognized by fingerprint (nightly 152), with their bulls and scale, that
  fingerprints from a camera photo are being built (request 69), and that only automatic hole detection on a bought target, and the
  designer, stay deferred. As it stands a reader of line 486 and line 270 gets two different stories.

### 14. STATE.md and for-alan.md disagree with what has happened (internal, but Alan reads for-alan through planning)

- **STATE.md line 76:** "Last nightly: 0.2.0-nightly.146"; the newest is 153.
- **STATE.md line 41:** "Stores: Microsoft in certification (38)"; it is published and 38 is closed. "The next three" items 1 and 2
  (nightly 147 for Unholy; close request 38 when listed) are done (for-alan.md "PUBLIC BETA AND THE STORE").
- **STATE.md line 51:** "The public beta link: Apple's first review of build 134 ... published only after approval"; Apple approved it and
  the link is published.
- **STATE.md, "Open requests":** lists 38 among the open requests (18 numbers for a count of 17); for-alan.md has no request 38.
- **STATE.md line 77:** "The site is live at 4ec87124"; check it against the commit the site now stamps (commit 6308cf5c).
- **for-alan.md request 59:** its status line still says it waits for Apple's first review of build 134 and "then I publish the link";
  both are done (entries 335 to 337), and request 67 now covers the automatic distribution. Close 59, or cut it down to what is left (the
  iPad install, item 4, and the two testers' invitations, item 3, if those are still open).
- **for-alan.md "GOOD MORNING" item 2** ("at 15:45 UTC it read Certification ... I say here when Microsoft answers") and **"IOS, THE
  WINDOW'S SUMMARY" item 7 and its closing lines** ("The two groups (request 59) are still needed for the public beta link") are
  superseded; move them to for-alan-archive.md.

Nothing here needs Alan himself.

## 2026-10-01, entry 344: fingerprints of any store-bought target, made from a camera photo (engine now, screens after concepts)

**Status: sections 1, 2, 3 and 5 done 2026-10-01 as an engine and a command (worker A, 42c48394); section 4's screens wait for planning's concepts (request 69); the library's fetch on the update check is built and signed but not wired, question 80.** Measured on synthetic posters only: a GroupLab sheet in the photo, (b), was not the most accurate source at poster size.

Alan, 2026-10-01 10:48 UTC: "I would like the ability to take camera photos of other commercial targets and manually measure the scale
and you make fingerprints of those as well. Many of the ones I saw at Cabelas and other places are poster sized and wont fit on a
flatbed scanner." And at 10:50: "These should be fingerprints for everybody to use but initially, only submitted by me. Concepts
tomorrow." Builds on entries 332, 340 and 341. Worker A, after entry 342's first item; main model.

1. **From a photograph to a reference.** One photo of a blank target (flat on a table or on a wall), lens-corrected as GroupLab's own
   captures are, then straightened to the sheet using one of three scale sources: (a) the sheet's printed outer size typed in (find the
   four corners, fit the rectangle); (b) a GroupLab sheet or card in the same photo, read by its markers (the most accurate); (c) two
   tapped points and a typed distance. Record which source was used and an honest uncertainty for it; report on synthetic posters
   (12 x 18, 23 x 35 in) photographed at the angles and distances a person would use what each source achieves.
2. **Then the same fingerprint as entry 332**, the bull positions the person confirms, and the family check of entry 340 against the
   whole library (a new artwork at another size joins that family).
3. **Who adds to the library: Alan only, for now.** The made reference is exported as one small file (fingerprint, name, printed size,
   bulls, scale source; never the photograph). Alan's files go into a folder Code reads (`C:\Dev\grouplab-local\commercial-targets\
   submitted\`); Code checks each one (it recognizes its own photograph, it is not a duplicate, its family is right) and adds it to the
   built-in library in the repository, signed with the rest. **Everybody gets the library**: it ships in every build, and the app also
   fetches a newer signed library on its regular update check, so a new target reaches people without a new app. No public submission
   path now; say in the guide that the library grows as targets are added.
4. **The screens wait for concepts.** Planning draws the capture flow for Alan on 2026-10-01 (the steps, choosing the scale source,
   confirming the bulls, exporting). Until then build the engine and a `grouplab target-reference` command that does steps 1 to 3 from a
   photo file, so Alan can try it on the desktop; DESIGN NEEDED in for-alan.md for the screens.
5. As entry 341: a store-bought scale always carries its plain note and the one-tap check; only fingerprints and GroupLab-drawn outlines
   ever ship, never a maker's artwork.

## 2026-10-01, entry 343: GroupLab Dev while idle, two small tightenings

**Status: section 1 done 2026-10-01; section 2 done only as a source test.** 1: the check now also needs the battery and the storage not low, and is updated rather than kept so existing phones take it. 2: no emulator runs in CI, so a Core test reads the source: leaving the screen lets go of the camera, the torch and the level's sensor, and nothing else registers. The batterystats reading waits for request 50's sitting; PERFORMANCE.md says "not measured on a device".

Alan, 2026-10-01 10:39 UTC: "Is grouplab dev respecting battery and resource usage? ... when idling." Planning read the updater:
idle cost is one WorkManager check about every six hours on an unmetered network only (one GET of the signed manifest), a download only
when a newer build exists, on Wi-Fi, the file deleted after install; no foreground service, wake lock, location or alarm; the camera's
sensors are registered only while the camera view is open. Good. Two tightenings, small, with Worker A's sweep in entry 342:

1. Add WorkManager's `RequiresBatteryNotLow` and `RequiresStorageNotLow` to the periodic check, so a phone on low battery or low storage
   is left alone until it recovers (Update now in Settings still works on demand). Test the constraints are set.
2. A test (on the emulator) that sending GroupLab to the background releases the camera, the torch and the level's sensor, and that
   nothing else stays registered. At request 50's sitting, read `adb shell dumpsys batterystats` for GroupLab Dev after an idle hour and
   record it in docs/PERFORMANCE.md; until then say "not measured on a device".

## 2026-10-01, entry 342: the overnight list, so the loop always has work

**Status: done 2026-10-01 except worker A's item 2, the phone sweep on the emulator and simulator in CI, not started: the day reached entry 317's share (about 0.6 billion tokens against last week's 5.4) and it is the largest item; it is first tomorrow.** Worker A 1: the slowdown was mostly the machine; the phone reads a sheet's codes in about half the time, every table identical. A3: the desktop sweep, about 50 fields named, Targets fits 1060 wide, Space and Enter work on a button reached by Tab. A4: one checklist. Worker B 1 to 4: the audit, the pairing proposal, the Store dry run, nothing open to triage.

Alan, 2026-10-01 10:34 UTC, going to bed: "What can be worked on while I sleep? I know there are still plenty of features that need to
be added so it should never be idle for a while." Order of work: entries 335 to 341 first (in number order), then this list, top to
bottom. Two workers (entry 333) on separate areas; each takes the next item in its area when it finishes one. Bugs a tester hits jump
the queue. Entry 317's budget still holds: at 12% of the weekly limit for the day, finish the item in hand, write the summary, stop.

### Worker A: the application

1. **Phase 9 starts now** (planning's word, entry 331 section 4 asked for it). First the regression: image work is 20 to 35% slower
   than on 2026-09-20; find which change did it, by bisecting the bench across nightlies, and fix it or say why the cost is worth it.
   Then the phone's pipeline (2.7 s headless for the 600 dpi sample; the Fold 7 photograph 2.5 s): profile, and speed up the slowest
   stages. Every change keeps the scoreboard, the corpus and every synthetic case identical, or it is not kept; record before and after
   in docs/PERFORMANCE.md.
2. **A quality sweep of every screen on both phones** with the automation of entry 315 (command bridge, scenarios, replay camera) on the
   Android emulator and the iOS simulator in CI: drive each screen and each control, every sheet and dialog, at the smallest and largest
   supported sizes, both themes, large text, and rotation. Fix every crash, clipped label, unreachable control, field under the
   keyboard (entry 328's rule) and dead end it finds; add each scenario to the nightly so it stays fixed.
3. **The same sweep on the desktop**: every window at 1060 wide and at 4K scaling, keyboard only (every control reachable with Tab and
   working with Enter or Space), and the screen reader names of every control. Fix what fails.
4. **docs/PROOF-CHECKLIST.md and question 79**: settle what can be settled without Alan, and leave one consolidated checklist for his
   next sitting, ordered by how many features each piece of material proves.

### Worker B: data, site and tooling

1. **The consistency audit, by hand once**: README, every page of grouplab.org, the three guides and the release notes against what the
   application does tonight (velocity block, chronograph files, recognition, the Store and the beta). Fix every stale sentence, picture
   and number; add what the weekly check should have caught to `scripts/consistency.py`.
2. **Chronograph reconciliation**: when a session has both marked shots and an imported Xero string, propose the pairing (shot order
   against time, the "--" exclusions, missing shots) for the person to accept, as DESIGN.md section 15 already describes, and build the
   tests from Alan's real strings with synthetic shots. If a screen layout question comes up, DESIGN NEEDED and build the engine anyway.
3. **Store submission automation** (entry 337 section 4): everything except Alan's credentials, ready to run once request lands.
4. **Crash and feedback triage**: read every open issue in grouplab-crash-reports, fix what is fixable, close with the build.

### When everything above is done

Do not idle and do not invent features: write a short list in for-alan.md of what is next and what each item waits on (Alan's sync
decision, a sitting, a design), then end the loop.

### Devices tonight

Alan's phones, tablets and iPad are asleep and not connected tonight (no wireless debugging, no cable). Nothing on this list uses them:
the phone sweep runs on the emulator and the simulator in CI. If a step would need a real device, skip it, note it for request 50's
sitting, and carry on; never wait for one.

## 2026-10-01, entry 341: no second sheets; commercial scale is assumed, with a warning (amends entries 332 and 340)

**Status: done 2026-10-01.** 1: request 64 closed as not needed. 2: the warning shows under the scale, on the result and in the report's scale sentence, with Check the scale one click away, never on a GroupLab sheet (e0695661). 3: unchanged. 4: the guide and the README in one sentence, with backing (76e9a876).

Alan, 2026-10-01 10:27 UTC: "I do not plan on scanning other sheets. That is a waste of time. Just assume they are all the same size or
within an acceptable error limit. Worst case scenario, just have a warning when using commercial targets that the scale was recorded but
could be wrong and encourage the user to verify."

1. **Close request 64** (a second sheet of each product) as not needed, Alan's decision, 2026-10-01. Nothing waits on print-to-print
   consistency any more.
2. **Entry 340 section 3 becomes:** a store-bought target's scale comes from its fingerprint and is used, with a visible warning on the
   result wherever that scale is in play, in plain words, for example: "Scale from this target's printed size. Printed targets can vary
   a little from sheet to sheet; check it against a ruler or a GroupLab sheet if the numbers matter." The scale check stays one tap away.
   The warning does not block anything, and it is never shown for GroupLab's own sheets.
3. The family question of entry 340 section 2 stays as written: it settles which size, not how exact the print is.
4. The guide and the README say the same in one sentence, with its claims backing (Alan's decision, and the trial's 0.06% median on the
   fingerprinted sheets themselves).

Also from Alan: the download page's phone layout stays as drawn (store card first, the file list in "Everything else"); his only changes
are the desktop ones already in entry 338. He has given Code his word in the panel to publish the TestFlight link, the Microsoft badge
and the new download page.

## 2026-10-01, entry 340: store-bought target recognition ships, and a look-alike asks which size

**Status: done 2026-10-01 (worker A, e0695661), except the Features page's own picture**, which waits for the screenshot walk (docs/figures/SCREENSHOTS.md). The five products are recognized on the computer and the phone with the trial's thresholds; the Shoot-N-C family asks 6 in or 8 in only when the picture cannot tell, and names a size without asking only when clearly ahead (agreement 0.95 or 150 features), which the trial's rule alone got wrong on 4 crops. Recognition time on a phone is not measured.

Alan, 2026-10-01 10:24 UTC, on the fingerprint trial (entry 332, `docs/notes/fingerprint-trial.md`): "How about for the two targets
that could be mistaken because they are the same design but different size, just have a pop up that asks the user to confirm which size
target it is?" Planning reads that as yes to shipping recognition, with this rule for look-alikes. After entries 335 to 339.

1. **Recognition ships** for the five fingerprinted products: GroupLab names the target and places its bulls, in "Find holes" and in
   the capture flow, on the desktop and the phone. Fingerprints only, never a scan or image of another maker's target (entry 332).
2. **Families.** Products that share one artwork at different sizes are a family in the library (today: the Shoot-N-C bullseye at 6 in
   and at 8 in; say if the trial found any other). When the best match is a family member and the picture cannot tell the sizes apart by
   the trial's own measure, GroupLab asks: "Which target is this?", with each family member's name, printed size and a small drawing of
   its outline (drawn by GroupLab, not the maker's art), plus "Not sure". The chosen size sets the scale and the bull positions;
   "Not sure" falls back to a scale check. When the picture does tell them apart, no question is asked. The person's last answer for
   that family is offered first next time. It is a small dialog in the app's existing style on both platforms; if it needs more than
   that, DESIGN NEEDED as usual.
3. **Trusting the scale.** Outside a family, and after a family answer, the fingerprint's scale is used with a plain note saying where it
   came from ("Scale from the target's printed size"), and the scale check stays one tap away. Whether the note can drop once request 64's
   second sheets show print-to-print consistency within about 0.2% is decided then.
4. A test for each family member at the crops that fooled the trial: the question appears, each answer gives that member's scale, and a
   picture that is clearly one size asks nothing.

## 2026-10-01, entry 339: the 2023 radar export has a name: BulletSeeker

**Status: done 2026-10-01 (commit a187d5e3).** Format A's reader is BulletSeeker in the code, the import list, the guide and the README, still Experimental, its two shotless files said. No Xero column or header names the device, so C1 and C2 cannot be told apart in the files. The metric exports wrote "Speed (MPS)", which the reader took for feet a second; fixed with a test.

Entry 334 was amended at 08:42 UTC, after its first version was read: Alan, "The ones that have raw radar samples were from the
bulletseeker chronograph that I had for a short time prior to the xero c1 and xero c2." Name format A's reader **BulletSeeker** in the
code, the import list, the guide and the notes (it stays Experimental until every BulletSeeker file reads; two have no shots, which is
fine if said). Report whether any Xero file differs between his Xero C1 and Xero C2 (a column, a header, a footer line), and the two
metric exports in `garmin-xero\metric\` ("Speed (MPS)", "KE (J)", "Power Factor (N⋅s)", the "All shots included in the calculations"
line) read correctly. Small; one commit.

## 2026-10-01, entry 338: the download page redesign, concept A ("pick your device")

**Status: done 2026-10-01 (worker A), except two small parts.** The Steady card cannot show the Store's version until docs/notes/external-status.md records it and the site reads it; the guides' Android links still go to the plain page, which the device guess covers on a phone. No Features or tour picture showed the old page, so none is stale.

Alan, 2026-10-01 09:09 UTC, on the canvas claude.ai/artifact/79m6nHkTJXekeMvhX9ZXF8: "Lets go with A for desktop but put the nightly
on the left. A on mobile." The boards "A: pick your device (desktop, Windows chosen)" (now redrawn with the nightly on the left) and
"A on a phone (Android chosen)" are the reference. Do it after entries 335 to 337 (it uses 335's TestFlight link and 336's badge).
Sample numbers on the canvas are samples; every real figure comes from the build and the release data.

1. **Layout, top to bottom.** "Download GroupLab" and one line under it; a row of five device buttons (Windows, Mac, iPhone and iPad,
   Android, Linux) with the visitor's own device chosen and marked "This computer" (on a phone, "This phone"), as large buttons on the
   desktop and a row of pill buttons on a phone; then only the chosen device's choices; a "Not sure?" line; that device's other
   downloads and its help, folded; then "Everything else", folded: what each device needs (the minimums table), what is supported and
   tested (today's long paragraphs, kept whole), updating, and every file of the current nightly on GitHub. Nothing on today's page is
   deleted; it moves behind a fold.
2. **Each device's two cards.** On the desktop the **nightly card is on the left** ("Newest", amber, one big download button, the file
   name, nightly number and date, three short points, and the honest line that it passed the tests but nobody has used it yet), the
   store card on the right ("Steady", the store's own badge or link, what updating through the store means, the store's version).
   On a phone the two stack, as drawn. Per device:
   - Windows: newest the installer; steady the Microsoft Store badge; folded: the zip, and "Windows protected your PC".
   - Mac: newest Apple silicon; no store yet, so one card and a plain line saying so; folded: the Intel build (marked untested) and
     "Which Mac have I got?".
   - iPhone and iPad: the TestFlight Public Beta only (the internal group's nightlies are not public), so one card; folded: "Install
     TestFlight first".
   - Android: newest GroupLab Dev; steady Google Play by invitation, with the Discord step; folded: the plain APK, and the Play Protect
     note; the "take one copy only" line stays visible.
   - Linux: newest the tarball, one card.
3. **Behaviour.** The device is guessed from the browser and can always be changed; a link can name one (`/download/?device=mac`), so
   guides and posts can point at a device. With JavaScript off, every device's section shows in order, so nothing is hidden for good.
   Device buttons are real buttons with `aria-pressed`; folds are real disclosure buttons; everything reaches 4.5:1 in both themes and
   works at 320 wide.
4. **The usual.** The site's look and tokens as they are; official badges under each store's rules; claims backing for every figure;
   the tour and README links still land on the right device; the site's tests, a Features or tour picture of the new page if one shows
   the old; for-alan.md says where to look.

### 5. Amendment (Alan, 09:11 UTC): the file list moves up on the desktop

"For A desktop, also move the 'every file in nightly 147' section right below 'Not sure?'" On the desktop, the folded "Every file in
nightly N" (on GitHub, with the commit it was built from) sits directly under the "Not sure?" line, above the device's other downloads
and help, and leaves "Everything else". The phone keeps it in "Everything else" unless Alan says otherwise. The canvas board is redrawn
to match.

## 2026-10-01, entry 337: Alan's answers on the Microsoft Store (entry 336), and a download page redesign coming

**Status: done 2026-10-01.** 2: the badge on the current page, then entry 338's. 3: the check, as entry 336. 4: no new credentials were needed: request 38's app registration already holds the Manager role, so store-submit.yml uses it; request 66 proposes the cadence, and nothing is submitted until Alan answers.

Alan, 2026-10-01 09:01 UTC, answering entry 336's four points:

1. "I installed it from the store and it is working fine. It is definitely an older build." So the install check of 336 section 1 is
   done by Alan on his own PC; still record which version the Store carries, and compare it with the newest nightly in for-alan.md.
2. "Please add the badge." Yes to the "Get it from Microsoft" badge. But: "Now that there are a lot of different versions and
   installers, the download page is starting to get pretty crowded." Planning is making concepts for a cleaner download page for Alan to
   choose from. Until he chooses, add the badge and the TestFlight link (entry 335) to the current page in the plainest way, and do not
   rework the page's layout; the chosen design comes as its own entry.
3. "Yes please": fix the six-hourly Store check (336 section 3).
4. "I want to have an automated route." Build the Store submission route through the Microsoft Store submission API: a workflow that
   submits a chosen build as a new Store submission and reports its certification status back to for-alan.md. Write the request for the
   credentials Alan alone can create, as one sitting with exact clicks: an app registration in his Entra ID tenant, associated with
   Partner Center under Users, Microsoft Entra applications, with the Manager role, and the tenant ID, client ID, client secret and
   seller ID stored as GitHub secrets by `gh secret set` (written out in full). Alan is an Entra ID administrator; the steps can be
   brief but exact. Which builds go to the Store, and how often, is Alan's decision: propose a cadence (for example, a build that has
   been on the nightly train without a new crash for a set time), and submit nothing until he agrees.

## 2026-10-01, entry 336: GroupLab is in the Microsoft Store (request 38 answered)

**Status: done 2026-10-01, except the Store's version, which the next Store check writes.** 1: request 38 closed; Alan installed it himself (entry 337). 2: the badge, with entry 337. 3: same cause and fix as entry 335 section 3, and the check now names the package version the Store carries. 4: entry 337 section 4.

Alan, 2026-10-01 08:54 UTC, from a screenshot of Partner Center: GroupLab, "MSIX or PWA app", badge "In Microsoft Store", Store presence
"Submission 1: Last modified on 10/01/2026", and "Your product is currently available in the Microsoft Store based on the
discoverability configured in the Availability module." Product ID 9NWJCXBKZNPZ (from the Partner Center address). Small; do it with
entry 335, before the rest of the queue.

1. Close request 38. Check the public listing at https://apps.microsoft.com/detail/9NWJCXBKZNPZ from outside Partner Center: it opens,
   installs on this PC, and the installed app starts and reads a sample sheet. Say in for-alan.md which version the Store carries.
2. Put "Get it from Microsoft" on the download page, the README and the guide, following Microsoft's own badge rules, in one change with
   its claims backing; say how the Store build differs from the nightly and the direct download (updates, signing, which version).
3. Why did the six-hourly check not report it? Fix it so it reports the day it changes.
4. Plan how later versions reach the Store (by hand in Partner Center each time, or the Store submission API with credentials only Alan
   creates). Write it as a request for Alan only if it needs him, with the exact steps; nothing is submitted to the Store without his word.

## 2026-10-01, entry 335: the Public Beta is approved; do entries 319 and 320's next steps now

**Status: done 2026-10-01.** 1: the invitation is on the download page, the README and the guide, with Alan's approval in the session (auto mode had first stopped it as a publication from a private note). 2: the lockstep is automatic: build 148 went into both groups by itself, so 147 was not needed; request 67 asks Alan to turn GroupLab Team's automatic distribution off. 3: the check reported only into its run summary; it now writes docs/notes/external-status.md when a state changes. 4: for-alan.md says Unholy can retest on 148.

Alan, 2026-10-01 08:54 UTC, from a screenshot of App Store Connect, TestFlight, iOS Builds, version 0.2.0:

- **Approved, in both GroupLab Team and Public Beta:** 134 (expires in 89 days), 143, 144, 145 and 146.
- **147: "Ready to Submit", GroupLab Team only**, not yet in Public Beta. 147 carries Unholy's two fixes (entry 328).
- 137 to 142: "Ready to Submit", GroupLab Team only. Feedback: one item on 143 (Unholy's, already handled).

for-alan.md still says build 134 is waiting for review, so the scheduled TestFlight check did not notice the approval. Do this before
the rest of the queue (it is small), by whichever worker is free:

1. Entry 319 and 320's plan, now that Apple has approved: put the "Join the iPhone and iPad beta" link
   (`C:\Dev\grouplab-local\testflight-public-link.txt`) on the download page, the README and the guide, in one change with its claims
   backing, and say so in for-alan.md.
2. Add 147 to Public Beta (submit it for beta review) so both groups are on the same build, and prove the lockstep on the next nightly:
   both groups receive it without anyone touching App Store Connect. Only then tell Alan, in for-alan.md, to turn GroupLab Team's
   automatic distribution off, with the exact clicks.
3. Fix the TestFlight check so an approval, a rejection or a build left in "Ready to Submit" for an external group shows in for-alan.md
   the same day; say why it missed this one.
4. Tell Alan in for-alan.md that Unholy can install 147 from TestFlight now (he is in GroupLab Team) to retest the keyboard and the note.

## 2026-10-01, entry 334: request 65 answered, Alan's Garmin Xero exports, and an older chronograph's files beside them

**Status: done 2026-10-01, except section 3.3's offer.** Every Garmin Xero export reads and the reader is no longer Experimental; the projectile weight and the 2023 export's weather are read and kept with each string, but no screen yet offers them to fill the load or the conditions, which needs a small design (where the offer sits beside the readings). A place name from the 2023 exports' location block was printed once into this session's own console during the first look at the files, before the reader existed; it was not written to any file, log or commit, and the reader never reads that block.

Alan, 2026-10-01: "All of the xlsx files in here are garmin chronograph files G:\My Drive\chronograph.files\2026 and
G:\My Drive\chronograph.files\2025. Check the older ones as the format may have changed." The folder also holds 2023 and 2024. About
385 files: xlsx 294 (2023 and 2024), xls 87 (2024 to 2026), csv 2 and xlsx 1 (2026), xlsm 2 (2023), and 23 zip files (look inside;
they may hold more exports). Google Drive for desktop; read it directly from G:\. Worker B, with entry 331 section 2; close request 65.

**Correction from Alan, 2026-10-01 08:39 UTC:** "I think you may be confusing chronograph results from other chronograph makers with
the garmin xero. I can tell you that I have had one since at least 6/1/2024 but no longer than that." So only Format B below is Garmin
Xero (it first appears in May 2024). Format A, 2023 to early 2024, is **another maker's chronograph**, not a Xero format and not an
older Xero version: do not label it Xero anywhere, in code, tests, the guide or release notes. Alan, 08:42 UTC: "The ones that have raw radar
samples were from the bulletseeker chronograph that I had for a short time prior to the xero c1 and xero c2." So Format A is the
**BulletSeeker** chronograph's export. The Xero reader is built and proven on Format B only. Format A gets its own reader, named
BulletSeeker, behind the same import interface, Experimental until every BulletSeeker file reads, and it does not hold up closing
request 65. Format B covers both of Alan's Garmins, the Xero C1 and the Xero C2: report whether anything in the files differs between
the two (a column, a header, a footer line), and handle both.

### 1. Copy, and the location rule

Copy every xls, xlsx, xlsm, csv and any export inside a zip into `C:\Dev\grouplab-local\chronograph-samples\garmin-xero\`, keeping the
year and date folders. Never write anything into G:\. **The older files carry where Alan shot: a "Location" name, "Latitude" and
"Longitude".** Treat them as the CLAUDE.md rule treats GPS in a photograph: never read into GroupLab, never printed, logged, stored on a
session or committed. The importer skips those rows by name; any file committed as test data has them removed first (and the place
name), with a test that fails if a committed sample holds a coordinate.

### 2. What planning saw in a sample of 15 (the reader must take all of these)

**Format A, 2023 to early 2024, .xlsx and .xlsm, the BulletSeeker, NOT Garmin (see the correction above)**, sheets "Data" and "Chart". Shots run across columns, not down: a row "Shot Number"
(Shot 1, Shot 2, ...), a row "Time" (US date and time), a row "Mean Speed [fps]" with whole-number speeds (the reading to use), then
"Measurements [fps]" followed by many rows of the radar's raw samples per shot (ignore them). Two header variants above it: early files
start "String", "Created", location rows, "Temperature" with "°F", "Pressure" with "inHg", "Humidity", "Notes"; later ones start "Name",
"Created", then "Rifle" (Rifle Name, Barrel Length), "Ammunition" (Brand, Name, Powder Weight, Primer, Bullet Mass gr), "Statistics"
(Min, Max, Avg, Deviation), a location block, "Weather" (Temperature, Pressure, Humidity, where one file stores humidity as a fraction,
0.0154), "Notes". openpyxl in read-only mode sees only one cell in some of these (no stored dimensions): read them fully.

**Format B, Garmin Xero, from May 2024 to now, .xls (old binary Excel), and in 2026 also .xlsx and .csv.** One sheet per string; a monthly export
("Sessions_MAY_2024-MAY_2024.xls") holds many sheets. Row 1 is the string's name (sheet names are cut to 31 characters, so use row 1).
Row 2 is the header: "#", "Speed (FPS)", "Δ AVG (FPS)" (later "Δ Avg (FPS)"), "KE (FT-LBS)" or "KE (FT-LB)", "Power Factor
(kgr⋅ft/s)", "Time", "Clean Bore", "Cold Bore", "Shot Notes". Then one row per shot, numbers stored as text, and in September 2026 with
thousands separators ("2,853.4"). Then a footer: "-", "AVERAGE SPEED", "AVERAGE POWER FACTOR", "STD DEV", "SPREAD", "Projectile Weight
(GRAINS)", "AVG KINETIC ENERGY", "Session Note", "Date" ("SEPTEMBER 19, 2026 13:16" or "April 11, 2026 at 1:31 PM"). Times may contain a
narrow no-break space before PM. The csv has the name on line 1 and a byte order mark at the start of line 2, not line 1.

### 3. Reading them right

1. Read speeds from the shot rows only, never the footer; check each string's mean, SD and spread against the footer's own figures where
   it has them (and against "Statistics" in format A), and report any file that disagrees.
2. Units from the header (FPS today; handle m/s if a header says so). Shot numbers with gaps mean deleted shots: keep the numbering.
3. Clean Bore, Cold Bore and Shot Notes are kept with the shot when present; projectile weight, temperature, pressure and humidity may
   fill the load and conditions only when the person accepts them (entry 329's conditions).
4. Every Format B file must read, or be listed with why not; then drop the Experimental label from the Xero reader. Report the counts
   by format and year, and list any file that fits neither format.

### 4. Metric exports, from the latest ShotView (Alan, 2026-10-01 08:48 UTC)

Alan switched ShotView (latest version) to metric and exported two files, now in
`C:\Dev\grouplab-local\chronograph-samples\garmin-xero\metric\`: a monthly multi-session export (22 sheets) and a single-session export
(93 shots). What they show, which the reader must handle:

1. The metric header is "Speed (MPS)", not "M/S": then "Δ Avg (MPS)", "KE (J)", "Power Factor (N⋅s)". Projectile weight stays
   "(GRAINS)" in metric. Units come from the bracket in the header, never from a setting or a guess; MPS converts to ft/s for GroupLab's
   own figures and the original unit is kept with the string.
2. Thousands separators appear in metric too ("1,460.4" in the energy column).
3. A new last footer line: "All shots included in the calculations". Shots left out in ShotView probably change that line (and the
   footer's figures); the reader keeps every shot row, marks any the footer says were excluded when it says so, and never uses the
   footer's averages in place of the shots. If no file yet shows an exclusion, say so; planning will ask Alan for one.
4. Sheet names now carry the date, time and an index ("1156arc1_2026-09-06_15-44_1"); the full string name stays in row 1.
5. These two are Alan's own files: they may be committed as test data after the same location check (they carry none that planning
   saw), with a line in samples/PROVENANCE.md.

### 5. Finding the excluded shots without Alan (Alan, 08:49 UTC: "I definitely have files where I have excluded a shot. It is not obvious from what I remember.")

Find them in the files themselves; do not ask him which. For every Format B string, across all his files:

1. Any last footer line other than "All shots included in the calculations" is a candidate; record its exact words.
2. Recompute the average, SD and spread from every shot row and compare with the footer. Where they differ, try leaving out each
   shot, then each pair, and keep the set that reproduces the footer to its printed rounding. One unique answer is an exclusion found;
   several answers, or none, is reported as such and never guessed.
3. Also look for gaps in the shot numbers, a mark in Shot Notes, or anything else that differs in those rows.
4. Report what ShotView does with an excluded shot (leaves the row out, keeps it with a mark, or keeps it unmarked and changes only
   the footer), with the files that show it. The reader then imports every shot that was fired and marks the excluded ones as
   excluded, so GroupLab's own figures can match ShotView's or include them, as the person chooses.

## 2026-10-01, entry 333: two workers for this batch, and real Garmin Xero files are coming

**Status: done 2026-10-01.** Two workers from 08:45 UTC: worker A (a worker in its own worktree) on entry 332 then entry 331 section 3; worker B (this session) on entry 331 sections 2 and 4, both done, then entry 334. Request 65 was written and answered the same hour (archived).

### 1. Two workers (Alan, 2026-10-01 08:20 UTC: "Can we have code use 2 workers instead of one? It seems to spend a lot of time idling.")

Entry 317 allows two workers for separate areas; Alan now asks for two by default while the queue holds work for both. Split the open
batch so the two never touch the same files:

- **Worker A, detection:** entry 332 (the fingerprint trial), then entry 331 section 3 (prove what existing material can).
- **Worker B, data and speed:** entry 331 section 2 (the chronograph imports), then section 4 (the performance baseline).

Each worker commits its own sections; one of them folds an entry once all its sections are done. When one worker's list is empty it
stops rather than waiting; one worker again when the queue has only one area. Keep entry 317's daily budget: if the day passes 12%,
drop to one worker at a clean point and say so in for-alan.md. Main model for both.

### 2. Real Garmin Xero exports (request 65)

Alan owns a Xero and will export some strings into `C:\Dev\grouplab-local\chronograph-samples\garmin-xero\` (his own data: usable for
tests; small enough files may be committed as test data with a line in samples/PROVENANCE.md, Alan 2026-10-01). Write request 65 in
for-alan.md to track it. Worker B builds the Xero reader from the published layout now, and as soon as a file appears there, tests
against it and drops the Experimental label when every file reads correctly (shot count, each velocity, units, deleted shots).

## 2026-10-01, entry 332: a fingerprint trial for store-bought targets (recognize the target, and its scale with it)

**Status: done 2026-10-01, sections 1, 2 and 4 (worker A); section 3 is request 64.** Not measured: the phone through the replay path (desktop figures only), and Unholy's range screenshot (none of these products appears in any range picture on the computer). The trial is the command `grouplab fingerprint-trial`, a spike in the command line program; nothing reaches the desktop or phone application.

Alan, 2026-10-01: "Is it possible to keep some type of hash or fingerprint for those store bought targets so they can be recognized if
somebody scans them and remembers the scale?" Planning explained the approach and Alan said: "Yes add the fingerprint trial." A trial
only: nothing ships in the application from this entry. Do it after entry 331 section 1 (it uses the same blanks); main model, one worker.

### 1. Build the fingerprint

From each blank in `C:\Dev\grouplab-local\commercial-targets\` (600 dpi, partial scans, entry 327), make a fingerprint with the OpenCV
already in GroupLab: local features (ORB or AKAZE, whichever measures better; say which and why), their positions in inches on the
target, the bull centers in the same frame, and a cheap global signature (color layout or a small thumbnail descriptor) for shortlisting.
Report each fingerprint's size; the aim is well under 200 KB a target.

### 2. Test recognition and registration

Make test pictures from each blank, kept local: perspective warps up to 37 degrees (Guided's limit), rotation, scale from about 1 to 3
ft as a phone would see it, crops to half the sheet, blur, uneven light, phone JPEG compression, and synthetic shot holes, including
the Shoot-N-C chartreuse halos and pasters over some holes. Also Unholy's range screenshot and any range photograph that shows one of
these products, if there is one. For each:

1. Does it identify the right product, and never claim a match on a GroupLab sheet, a different product, or a blank wall? A wrong match
   is worse than no match: report the false-match rate with the threshold chosen.
2. How well does the fitted transform recover the scale and the bull centers? Error in percent of scale and in inches at the bulls.
3. Time on the desktop, and on the phone through the replay path (entry 315) where no device is needed; memory.

### 3. Print consistency: what is still unknown

The scale from a fingerprint is only as good as the press. Write request 64 in for-alan.md: scan one more sheet of each of the five
products at 600 dpi in the same corner of the glass, a sheet from a second pack where he has one. When those arrive, measure sheet to
sheet scale differences; a remembered scale is trusted only if they agree within about 0.2%, otherwise recognition still finds the
bulls but GroupLab asks for a scale check.

### 4. Report and decision

`docs/notes/fingerprint-trial.md`: sizes, rates, errors, times, and a recommendation planning takes to Alan (ship it, ship recognition
without the scale, or not yet). Only fingerprints would ever ship, never a scan or image of another maker's target; that decision is
Alan's and is not made here. The worth-an-article decision as usual.

## 2026-10-01, entry 331: a batch that needs nobody, so the loop has real work

**Status: done 2026-10-01, every section.** Section 1: Find holes on the five blanks went from 0, 0, 0, 3 and 8 marks to 0, 0, 0, 0 and 6, every any-target scoreboard line unchanged; the remaining six (printed dark numbers and letters, two white digits) cannot be told from a hole without refusing real ones. Section 2: a generic CSV, LabRadar and Garmin Xero, then entry 334's real files. Section 3: docs/PROOF-CHECKLIST.md (worker A). Section 4: docs/PERFORMANCE.md's second baseline. Section 5: the summary is at the top of for-alan.md.

Alan, 2026-10-01 07:31 UTC: "It seems like code is idling a lot and is just waiting for stuff." Everything left in STATE.md's plan is
blocked on him, on Apple or Microsoft, or on planning, so here is work that is not. One worker, main model, entry 317's budget; in this
order, one commit per section where it allows. Bugs a tester or Alan hits still jump the queue.

### 1. The store-bought blanks as a detection test now (request 58's material, entry 325 and 327)

1. Run "Find holes (Experimental)" on the five blanks in `C:\Dev\grouplab-local\commercial-targets\`. A clean sheet must give zero
   holes; list every false mark by cause (printed numbers, pasters, ring lines, the red centers, the cut edge of a partial scan) and fix
   what can be fixed without making any existing case worse.
2. Render synthetic holes into each blank (several calibers, touching pairs, holes on ring lines, on the red centers, on the grid lines,
   and a Shoot-N-C style chartreuse halo where the real target shows one) with known positions, and score finding them, the way the
   scoreboard does for GroupLab sheets. Kept local, never committed (request 58). Report the scores per target; the real shot scans will
   check them later.

### 2. Chronograph files that need no sample from Alan (DESIGN.md section 17)

1. A generic CSV import: a column of velocities picked by header or by the person, units fps or m/s, everything else ignored. This is
   the "most users arrive from a spreadsheet" route.
2. LabRadar's CSV export, from its published layout, and Garmin Xero's CSV export from ShotView, from its published layout. A public
   sample file under a license that allows it may be used as a test, recorded in samples/PROVENANCE.md; otherwise build the test from
   the documented columns and label the reader Experimental until a real file passes. Never anything from a stranger's upload.
3. All three behind the one import interface, ending in the same list of numbers the hand-entry box makes, then the existing
   reconciliation. Place "Import a file" beside the existing entry on both platforms in the existing style; if it needs a new layout,
   DESIGN NEEDED and build the readers anyway.

### 3. The 27 "built, not proven" features: prove what existing material can

Go through the 27 and, for each, say whether material already on Alan's computer or in test-data (the 2026-09-20 range photographs, the
camera-0929 sitting, the corpus, the scale-test scans, Unholy's range screenshot, the commercial blanks) can prove it. Prove every one
that it can. For the rest, name the exact material each needs, so the next request to Alan is one sitting with a checklist.

### 4. Performance (Phase 9): a baseline only

Planning says the application has settled enough to measure. Record a baseline, nothing optimized yet: desktop start time and memory,
a GroupLab sheet read end to end on the desktop, and on the phone through the replay camera (entry 315) where no device is needed; the
numbers into the gate record with their dates. Say what is slowest; planning decides what to optimize.

### 5. When the loop stops

When 1 to 4 are done or blocked and the inbox is empty: write the summary at the top of for-alan.md and end the loop. Do not wake to
wait for Apple, Microsoft or a nightly; the scheduled checks already watch those.

