# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-10-23, entries 470 (Android's back on Sync and your data) and 469 (the audit's wording) done.

---

## In flight

- **Entries 469 and 470 (2026-10-23)**, done: Settings' old page names fixed everywhere; Android's back on Sync and your data made a
  records copy, fixed for nightly 219; the phone's Shooting defaults and Sync and your data pictures wait for the walk.
- **Entry 468 (2026-10-22)**, done: Settings is eight categories (side menu on the computer, a list on the phones) with Find a setting
  and "Put this page back as it came"; daily copies of the records (`RecordsCopies`, 14 kept, restore); Shooting defaults, the camera's
  start and sound, "The analysis opens with", the computer's text size and Reduce motion. Saving sits on Sync and your data; rounds
  fired and rifle and load are computer only (default Ask, today's behaviour). The phone pictures of the new pages wait for the
  emulator's walk.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: done in entry 324 section 1 (da9cf190): the far column from 0.08 to 0.01 or 0.02 in on the 9 and 15
  degree photos, nothing else changed. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Chronograph files** (Phase 5): every one of Alan's Xero exports reads, and a timed string proposes its pairing (entry 342);
  checking a proposal needs a sheet shot with its string.
- **Velocity regression** (Phase 5): on both screens since entry 323, in the conditions entered on Ballistics since 329; a
  session's own conditions are entered under the block (entry 416); the phone's picture waits for request 50's sitting.
- **Synchronization** (Phase 7): built on every platform with tidying (entries 461, 463, 464); proving it needs two real devices.
- **iOS** (Phase 8): in a public beta through TestFlight; the device checks need a sitting (50), GroupLab Dev on the iPad request
  61, an App Store release Alan's word.
- **Performance** (Phase 9): hole finding on a scan (entries 392, 433, 447, and 2026-10-20's open); a photograph's codes left as they are (question 106, (b)); nothing queued.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Request 83, the M834 printing from the computer, now that the phone's prints are true both ways.
2. The range kit in `range-next` waits for Alan's next trip; its results come back as an entry.
3. Questions 107 to 110 once planning answers them (phone hole counts, the scan's resolution line, forgetting a sync device, precompiled builds).

## Blocked, and on what

- **Entry 380 section 3**: measuring the M220 labels waits on request 77's photos in `m220-labels`.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **25**; waiting on him: 105 (a screen reader sitting, later), 83, 70, 72, 74 to 77, 104 (104 the point blank card on the M220; 86 aim points through the other optics, for later; 83 the M834 from the computer; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Eight, all in `docs/QUESTIONS-FOR-PLANNING.md` (106 answered 2026-10-21: leave it):
- **111** whether a published build's notes are edited when a line was wrong (nightly 219 corrects 218; nothing waits)
- **110** whether the builds are published precompiled, for a faster first picture (nothing waits)
- **109** a sync device that never comes back holds tidying back for ever (nothing waits)
- **108** the scan's own resolution as a scale has no words on the Set tab yet (nothing waits)
- **107** whether the phone stops numbering found holes on a marking with no bulls (nothing waits)
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9
- **36** a light installer, measured

## Builds and the site

- **Last nightly:** 0.2.0-nightly.218 (2026-10-22 13:07 UTC): entry 468. ios-app.yml: the iPhone app passes; Apple's audit times out
  on its result screen (PHASE1-RESULTS.md, entry 468), recorded, not changed.
- **The site** follows main by itself (website.yml) and the nightly publishes it; live at 4882f861 (entry 468) on 2026-10-22.
- Crash reports: none open; 9, 36 and 37 closed with nightly 206; assessed in `docs/notes/error-reports.md`.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **A phone page's way back is `Screens.BackButton`** (entry 470), never guessed from words; `Entry470Tests` holds it.
- **Settings is eight pages** (entry 468): a test opens the one it means with `ShowSettings(SettingsCategory.X)` on the computer or
  `new SettingsView(store, category)` / `Shell.ShowSettings(category)` on the phone; scripted walks press `settings-<category>` first.
  Search and put back go through `SettingsPages` in `src/GroupLab.App/SettingsMenu.cs`.
- **The records are copied daily** beside the database in `copies/` (trash in `trash/copies/`); `RecordsCopies` makes and restores them.
- **Paper is `AppSettingsStore.LetterPaper()`**, the chosen paper else the region's; **text size** is `TextSize.Choose`; **Reduce motion**
  is `Landing.Still()`, which tests that fake the system still drive through `Landing.ReducedMotion`.
- **The server's logs are in Denver time** (-0600, -0700 from 1 November), not UTC; a log paste converts its window (request 99).
- **The zero's words are `ZeroWords`** (entry 424), read by the desktop panel, the report and the phone; `ZeroAxis.Rounded` has the
  clicks on every axis, `Clicks` only on one worth dialling.
- **A tab can hold a set** (entry 423): `OpenTarget.Set` parks each picture of the set as a tab parks a target, and the pool is worked out
  afresh whenever it is shown; a drop onto a tab holding a target always opens beside it. "Read these as one set" is gone.
- **A Features sentence changed since the newest nightly fails the site build** (entry 421) unless one of its note phrases is still to
  come, or it carries `"reworded"` naming that nightly; a widened feature reads "Since nightly.N, changed in nightly.M".
- **A hole can be a ring** (entry 417): on a flatbed with a white lid a hole is as light as the paper; detection fits a circle through its
  rim (`FitCircle`) and the snap does the same. **Restyling adds the new styles before removing the old** (a macOS crash in between).
- **ios-app.yml's "Real taps" lost a first tap** (about 1 run in 4) because a field's Cut, Copy and Paste menu opened for a tap handled
  late while the keyboard first came up; `KeyboardRoom.HoldOffForAFocusingPress` stops it (entry 447 section 3).
- **The database is schema version 6** (entry 461): each upgrade's first open copies `grouplab.db` to `grouplab.pre-vN.db` first.
- **OpenCV's QR detectors are never run on several threads** (entry 414): reading a sheet's codes at once crashed Linux in native code
  in 3 of 12 gate runs and hung macOS before. A job dying with exit 134 or 139 is tabulated against what was in effect, not rerun.
- **`docs/notes/external-status.md`** holds TestFlight's and the Store's state, written by their workflows when it changes; read it
  at the start of a run (entries 335 and 336).
- **Helpers only when Alan asks, ending at the week's reset; chores to chores-haiku, wording to docs-sonnet** (CLAUDE.md, 2026-10-08).
  A finished worker's subjects are compared with main before its worktree goes to the trash.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **A failing site check holds back only its pages** (entry 387): whole-site checks still stop it; held pages keep grouplab.org's copy and
  are listed in website.yml's summary and the weekly consistency issue; each gets one line in for-alan.md when it appears.
- **Tag36h11 470 to 549 are scale labels' and 555 to 586 scale markers' (entries 372 and 365)**: the sheets' markers must stay below; the sheet look and the validator know.
- **A picture with a bank card in it is never sent**, and marking by hand works on a blanked copy (entry 365 section D).
