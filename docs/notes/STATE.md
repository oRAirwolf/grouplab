# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-07 evening, after entry 387 (before it: entry 386, question 90's answer, and Alan's guard at 98).

---

## In flight

- **The usage guard stops at 98% until the week resets (2026-10-08 02:00 UTC)**, Alan, 2026-10-07 evening; at 85% again after it,
  88% the last line for a finishing block, with no edit (`scripts/usage-guard.js`). 89% at 19:25 UTC on 2026-10-07.
- **Google Play's internal test now follows the nightlies** (request 80 answered): nightly 176 sent by hand, version code 176 committed
  (run 37674523785); from nightly 177 every one goes by itself. **Request 79 answered**: both APKs registered with the upload key.
- **Question 87 measured**: no second line for fits with many features is safe (wrong fits reach 590 features); the rule stays.
- **Entry 386 done** (2026-10-07): the plain APK updates itself from nightly 177; a photo with no marker is read once at full size
  (question 83 (b)); the M220's profile, check label, label size with the printer, and **Print two scale labels on the Phomemo M220**
  over Bluetooth LE (not yet tried on a real M220); **Send to GroupLab** live once Alan runs the worker reinstall in panel.md (the receiver
  is on the site); question 34 built as proposed, **for planning to confirm**.
- **Entry 387 done** (2026-10-07): a failing site check holds back only its pages.
- **Question 90 answered (Alan)**: the phone pictures' limit is 90 and the site publishes again; the emulator's run now takes the phone
  pictures after each nightly and commits them (`scripts/android-screens.sh`); its first run is 37689905340.
- **Entry 385 done** (2026-10-07): the M834 prints true across, 0.8 percent short along the feed (caliper); request 78 closed,
  request 81 saves the check and asks for a second print before any stretch.
- **Entry 379 section 3 done, and the consistency audit of 2026-10-07 fixed, all ten findings** (2026-10-07; question 89).
- **Entries 384, 383 (the tester's report, fixed in nightly 176), 378, 376 (B7 built in 386), 374 and 373 done.**
- **Waiting on Alan:** requests 70, 72, 74, 75, 76, 77, 81, 82 (the M834's check, a second print).

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
- **Performance** (Phase 9): the baseline gate and any optimization; waits for planning to say the application has settled.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. The phone pictures of Scale markers, the fingerprint and pairing screens, at the next sitting with a phone.
2. Alan's first print on the M220 from the phone, and his worker reinstall (panel.md).
3. Check the emulator's first phone pictures against the tour pages' words.

## Blocked, and on what

- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **22** (82 the worker reinstall and a first M220 print; 81 the M834's check and a second print; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Eight, all in `docs/QUESTIONS-FOR-PLANNING.md` (84, 86, 87, 89, 83 and 90 answered 2026-10-07 evening):

- **88** DESIGN NEEDED, chronograph entry on the analysis
- **80** a newer fingerprint library without a new build
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have; **36** a light installer, measured
- **34** a pooled group's center: built as proposed in entry 386 (each sheet from its own center first), for planning to confirm

## Builds and the site

- **Last nightly:** 0.2.0-nightly.176 (2026-10-07 18:00 UTC: entry 383's marking file fix and the work before it); on Google Play's
  internal test as version code 176.
- **The site** follows main by itself (website.yml), but not the nightly's own [notes] pushes; dispatched by hand for 174's notes on 2026-10-06.
- Crash reports open: none (issue 23, nightly 172's darkness share, closed 2026-10-06). Issue 19 (the keyboard bar's Next) closed: fixed in ee435491, proven by the simulator's real taps.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **The chronograph store counts readings from 1**, and accepting a pairing used to store them from 0: the first reading paired with a
  shot was refused, and every other pairing was one reading off. Fixed in entry 323 on both platforms.
- **`docs/notes/external-status.md`** holds TestFlight's and the Store's state, written by their workflows when it changes; read it
  at the start of a run (entries 335 and 336).
- **A worker's commits can be stranded in its worktree.** Entries 325 and 327's detection work and the fold of 324, 325 and 327 sat
  unpushed in `.claude/worktrees/push-324` until entry 342 found them; compare a finished worker's subjects with main before removing it.
- **A UI dump cannot see the phone's camera screen**: its views are native, inside Avalonia's host. The device check reads the screen's
  own `camera.layout` log line instead (`scripts/device-capture-check.py`).
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
