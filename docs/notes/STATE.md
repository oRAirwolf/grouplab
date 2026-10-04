# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-04 12:30 UTC, after entry 369 (the Microsoft Store follows the nightlies); the inbox is empty.

---

## In flight

- **Running from a terminal (entry 361), 2026-10-04.** The status line writes docs/notes/usage-now.json every minute: 70% of the
  week at 10:25 UTC. Plan each block to end under 85% (entry 360 section 2). The hook `scripts/usage-guard.js` blocks from 85
  unless docs/notes/finishing.flag is under 45 minutes old, and from 88 always.
- **Entries 363, 364 and 365 done** (2026-10-04), for nightly 167: the M834 files (166), aiming marks, issue 19 (closed), the
  sheet look 8.4 s to 0.15 s, the phone's failures as sheets, the phone sender (off until "forms updated"), four reference files,
  scale markers A to D.
- **Entries 358 and 359 done but for the radio**: no platform's Bluetooth or USB link until requests 72 and 73.
- **Entries 366 to 370 done**: the range kit, cut down by 370 to an hour (three loads one sheet each, store-bought targets, C and E bulls), in `C:\Dev\grouplab-local\range-2026-10-04\`, its results to come back in `results\`.
- **Waiting on Alan:** "forms updated" (switches on the phones' log, Send everything I open and sending targets); requests 70,
  72, 73 (the M834's recordings), 74, 75, 76.

- **Entry 369 done**: store-follow.yml sends every published nightly to the Microsoft Store (one in certification at a time; a
  failure stops it); the Store's search did not find GroupLab on 2026-10-04, and a ticket is drafted for Alan after eight days.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: the far column's 0.08 in errors come from a lifted margin; the bent-sheet registration is entry 324
  section 1, with the other session. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
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
2. Question 83: identification's 34 s on a photo with no codes; measure option (b) on the corpus when planning answers.
3. The M834: the recording reader waits for request 73's recordings; then the printer's own protocol.

- **When the range results come (entry 370):** RANGE-PLAN-HOLE-SIZE.md's results say this run is one sheet each of three loads: the
  speed effect at .22 width and a centrefire point, not speed against width without the .300 Blackout subsonic sheet.

## Blocked, and on what

- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30 morning.
- **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **20** (76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 71 switching on entry 357, the store forms; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Nine, all in `docs/QUESTIONS-FOR-PLANNING.md`:

- **83** identification takes 34 s on a photo with no codes (my choice: full size only when no marker is found)
- **80** a newer fingerprint library without a new build
- **78** the plot's toggles cover the last lines of its key at 1400x900 (found in entry 323; a change to the look)
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have; **36** a light installer, measured
- **34** pooling two sheets of one load needs a rule for a pooled group's center

## Builds and the site

- **Last nightly:** 0.2.0-nightly.166 (2026-10-04): entry 363 section 2, the M834 files.
- **The site** is live at 6308cf5c, after nightly 153 (a bot's `[screens]` or notes push starts no workflow; publish by hand).
- Crash reports open: none. Issue 19 (the keyboard bar's Next) closed: fixed in ee435491, proven by the simulator's real taps.

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
- **The Mac packaging can be run by hand**: `gh workflow run package.yml --ref <branch> -f ref=<sha>` proves the signing and
  notarization on a commit without a nightly, and publishes nothing.
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
- **Tag36h11 555 to 586 are scale markers' (entry 365)**: the sheets' markers must stay below; the sheet look and the validator know.
- **A picture with a bank card in it is never sent**, and marking by hand works on a blanked copy (entry 365 section D).
- **Entry 317's budget is in force:** one worker by default, the ccusage line once a day in for-alan.md.
