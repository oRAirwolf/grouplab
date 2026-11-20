# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-11-20, during entry 505 (sections 1 and 2 done, section 3's queue in flight); the inbox is empty.

---

## In flight

- **Entry 505 (2026-11-20)**: iOS 18.0 is the install floor, 26 the supported one; below 26 the phone shows Alan's warning first
  (`UnsupportedIos`, remembered with the version; Settings, About says so). The App Store description gains the sentence with the next
  version Alan releases. Question 116 closed (a). **Section 3, the week's queue, in flight:** 3.1 needed no build (the phone deletes
  no saved sheet). **3.2, approved and not built, smallest first:** (a) done: look 1A's "?" kept clear of the tags (tags on the
  phone's result picture are a DESIGN NEEDED line); (b) is question 117 (how printers travel in sync, for App Store 1.0's sake): both checks of a same-named printer kept, with a question
  which to use (entry 493 section 2: a per-printer sync record with a deleted mark; the question's look goes to for-alan as DESIGN
  NEEDED); (c) done: a light hole inside dark print found by its light middle. 3.3: no article needs nobody (the draft waits on
  request 86). **In flight: 3.4 speed**, then 3.5 audits and automation.
- **Entry 504 (2026-11-20)**: the pooled set's choice is concept A on both platforms (`PoolChoices`); Mac testers' link stays in
  App Store Connect (question 116 (a)); request 111.
- **Entry 501 (2026-11-18)**, in part: the Mac App Store copy (`org.grouplab.app`, sandboxed, docs/MAC-APP-STORE.md) is built by mac-store.yml after each nightly; signed ad hoc the sandbox stops it at LaunchServices, so its proof needs TestFlight on a Mac; request 110 (first numbered 107) answered 2026-11-18: nightly 233's copy is signed, on TestFlight, valid, in GroupLab Team only; whether it runs on a real Mac is request 111; never submitted without Alan's word.
- **Entry 499 (2026-11-18)**: Snap to holes (toolbar and Shooting defaults) and Alt for one click; the snap keeps a click on its own
  touching ring and stays put when it finds no hole's edge. The missed backup night is caught at the start of every Code run now.

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: done in entry 324 section 1 (da9cf190): the far column from 0.08 to 0.01 or 0.02 in on the 9 and 15
  degree photos, nothing else changed. **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Chronograph files** (Phase 5): every one of Alan's Xero exports reads, and a timed string proposes its pairing (entry 342);
  checking a proposal needs a sheet shot with its string.
- **Velocity regression** (Phase 5): on both screens since entry 323, in the conditions entered on Ballistics since 329; a
  session's own conditions are entered under the block (entry 416); the phone's picture waits for request 50's sitting.
- **Synchronization** (Phase 7): built on every platform with tidying (entries 461, 463, 464); in tests a simulated cloud with three
  devices, late, out of order and half-written files, conflicted copies and wrong clocks (entry 472); a real cloud client over weeks
  needs Alan's two devices (PROOF-CHECKLIST.md, line 4 of entry 472's list).
- **iOS** (Phase 8): in a public beta through TestFlight; the device checks need a sitting (50), GroupLab Dev on the iPad request
  61; App Store 1.0 released 2026-11-17 on Alan's word.
- **Performance** (Phase 9): hole finding on a scan (entries 392, 433, 447, and 2026-10-20's open); a photograph's codes left as they are (question 106, (b)); nothing queued.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Play's closed test is open (entry 490); Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A numbered release** (made by release.yml from a tag): only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Request 83, the M834 printing from the computer, now that the phone's prints are true both ways.
2. The range kit in `range-next` waits for Alan's next trip; its results come back as an entry.
3. Request 50's sitting, when Alan has it: the device checks and the two camera clips for the launch videos.

## Blocked, and on what

- **The Mac App Store copy**: request 111 (the TestFlight build opened on a real Mac, now also through Mac testers for Alan's friend once
  Apple's beta review approves it), then Alan's word to submit (entry 501 section 4).
- **The iPhone Duo's inner screen**: the Duo leg on Xcode 27.2 (simulator starts folded; IOS-PLAN.md); nothing needed from Alan.
- **Entry 380 section 3**: measuring the M220 labels waits on request 77's photos in `m220-labels`.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **26**; waiting on him: 111 (the Mac TestFlight build on a real Mac), 105 (a screen reader sitting, later), 83, 70, 72, 74 to 77, 104 (104 the point blank card on the M220; 86 aim points through the other optics, for later; 83 the M834 from the computer; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Three, in `docs/QUESTIONS-FOR-PLANNING.md` (116 closed (a) by entry 505); **117** how printers travel in sync, (a) recommended:
- **67** the printer check page as grid style 4 (with Alan); **51** which hole center to report, waits on request 9

## Builds and the site

- **Last nightly:** 1.0.0-nightly.235 (2026-11-20), with entry 504. GitHub's Mac runners are sometimes short: rerun a job
  "not acquired" with --failed.
- **The site** follows main by itself (website.yml), and now also after the Store's version and either screenshot job (entry 476).

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **GroupLab installs on iOS 18 to 25 without support** (entry 505): a new screen comes before the first run's questions there, and a
  scripted run accepts it (`Scenario.AnswerFirstRun`). Reports from below 26 are headed "unsupported iOS" in error-reports.md.
- **Snap to holes can be off** (entry 499): a hole placed by hand then goes exactly where it is clicked; Alt does it for one click.
  **At the start of every run, `python scripts/automation-report.py`** (CLAUDE.md, backups rule 6) catches a night that never started.
- **A pooled set changed on two devices waits like a session** (entries 498, 504): the banner counts it, its row is tagged, and it opens
  concept A, each sheet tagged; a sheet not here yet is never counted. A pool reaching a device before its sheets asks nothing.
- **A phone page's way back is `Screens.BackButton`** (entry 470), never guessed from words, or `Screens.LeavesPage` for a Done,
  Cancel, Skip or "Another target" that is the page's way out (entry 472); `Entry472BackTests` fails on a new page Back was never pressed on.
- **A Features sentence changed since the newest nightly fails the site build** (entry 421) unless one of its note phrases is still to
  come, or it carries `"reworded"` naming that nightly; a widened feature reads "Since nightly.N, changed in nightly.M".
- **Find holes looks for rings on paper first** (report 50): nothing inside a ring's outline is proposed again; not on dark targets.
- **With no design known, the snap looks for a hole's outline first** (error report 50, `Snapping.Outline`), then weighs the dark as
  before if it finds none; a sheet with its design known snaps as it did. Backups over 1 GB are in parts (`backup.py --join`).
- **A hole can be a ring** (entry 417): on a flatbed with a white lid a hole is as light as the paper; detection fits a circle through its
  rim (`FitCircle`) and the snap does the same. **Restyling adds the new styles before removing the old** (a macOS crash in between).
- **The database is schema version 7** (entry 495 added `pools`, listed by negated id): each upgrade's first open copies the database first.
- **OpenCV's QR detectors are never run on several threads** (entry 414): reading a sheet's codes at once crashed Linux in native code
  in 3 of 12 gate runs and hung macOS before. A job dying with exit 134 or 139 is tabulated against what was in effect, not rerun.
- **`docs/notes/external-status.md`** holds TestFlight's, the App Store version's and the Store's state, written by their workflows when it
  changes; read it at the start of a run (entries 335, 336 and 491).
- **Alan allowed ssh to the server, standing, 2026-10-26**; it is still announced in panel.md, and sudo's rules are unchanged.
- **Helpers only when Alan asks, ending at the week's reset; chores to chores-haiku, wording to docs-sonnet** (CLAUDE.md, 2026-10-08).
  A finished worker's subjects are compared with main before its worktree goes to the trash.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **A failing site check holds back only its pages** (entry 387): whole-site checks still stop it; held pages keep grouplab.org's copy and
  are listed in website.yml's summary and the weekly consistency issue; each gets one line in for-alan.md when it appears.
