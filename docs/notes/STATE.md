# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-11-07, entries 481 and 482 done; the inbox is empty.

---

## In flight

- **Entry 481 (2026-11-07)**, done: ten more launch clips (50 in all; the two camera clips wait for request 50's sitting), OpenCV's
  video reader left out (Windows zip 114.4 MB, question 36 closed), research table caught up, the bulls found three times sooner, and a
  scan's own dpi offered as the scale with its own pill and line (question 108 (a)).
- **Entry 482 (2026-11-07)**, done: "Rifles and loads" in the phones' Settings opens Ballistics, with "‹ Settings" back (question 112
  answered, 2A); the Duo's bar kept as it is (1A).
- **Entry 480 (2026-11-07)**, in part: the iPhone Duo. **Dated check, from 2026-11-11 (.NET's November release):** is there an iOS
  workload set after 10.0.402.1 with dotnet/macios issue 26969 fixed? If so, `python scripts/ios-xcode27.py` on ios-app.yml with its
  version, prove it, then nightly.yml and IosSigningTests (docs/IOS-PLAN.md, The iPhone Duo). Fold tests and two phone fixes are in.
- **Entries 476 to 479 and error reports 41 to 44 (2026-11-07)**, done: the site's dispatches, the notes counting, four consistency
  audits; 42 and 43 fixed for nightly 223, 41 and 44 no fault.

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
  61, an App Store release Alan's word.
- **Performance** (Phase 9): hole finding on a scan (entries 392, 433, 447, and 2026-10-20's open); a photograph's codes left as they are (question 106, (b)); nothing queued.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Google Play past internal test is Alan's call; Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A beta or stable release**: only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Request 83, the M834 printing from the computer, now that the phone's prints are true both ways.
2. The range kit in `range-next` waits for Alan's next trip; its results come back as an entry.
3. Request 50's sitting, when Alan has it: the device checks and the two camera clips for the launch videos.

## Blocked, and on what

- **The iPhone Duo's inner screen**: Microsoft's iOS 27.1 workload (dotnet/macios issue 26969); nothing needed from Alan.

- **Entry 380 section 3**: measuring the M220 labels waits on request 77's photos in `m220-labels`.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **25**; waiting on him: 105 (a screen reader sitting, later), 83, 70, 72, 74 to 77, 104 (104 the point blank card on the M220; 86 aim points through the other optics, for later; 83 the M834 from the computer; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Two, both in `docs/QUESTIONS-FOR-PLANNING.md` (36 answered in entry 481 and 112 in entry 482, 2026-11-07):
- **67** the printer check page as grid style 4 (with Alan)
- **51** which hole center GroupLab should report; waits on request 9

## Builds and the site

- **Last nightly:** 0.2.0-nightly.223 (2026-11-07 09:17 UTC), sent to TestFlight and the Store; nightly 224 carries entries 481 and 482.
- **The site** follows main by itself (website.yml), and now also after the Store's version and either screenshot job (entry 476).
- Crash reports: none open (41 to 44 closed 2026-11-07; `docs/notes/error-reports.md`).

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **A one-inch LengthReference on the diagonal is a scan's stated dpi** (`StatedResolutionScale.DotsPerInchOf`, entry 481 E), as
  imported shots are recognized by theirs; `Directory.Build.targets` drops `opencv_videoio_ffmpeg*` from every build (481 B).
- **Ballistics can be opened from Settings** (`Shell.ShowRiflesAndLoads`, entry 482) with "‹ Settings" back; the back walk expects a
  place opened with its own way back to return there, not to Capture.
- **Every phone sheet closes through `ProblemSheet.PutBack`** (error report 43), and a choice handed to `ProblemSheet.Over` that the
  caller did not place is shown under the body.
- **A rifle, barrel or load kept from its form renames** (`RecordBook.With(record, was)`, error report 42); a name in use is refused.
- **Four workflows may start website.yml** (nightly, screenshots, android-emulator, store-status), each after a push with its own token.
- **Nightly 222's notes are left as published**; its report-born fixes are counted from `attributions` in release-note-corrections.json.

- **A phone page's way back is `Screens.BackButton`** (entry 470), never guessed from words, or `Screens.LeavesPage` for a Done,
  Cancel, Skip or "Another target" that is the page's way out (entry 472); `Entry472BackTests` fails on a new page Back was never pressed on.
- **Sync change files carry `sha256`** of their records (entry 472); a file is taken only when it matches and every record applies.
- **The records are copied daily** beside the database in `copies/` (trash in `trash/copies/`); `RecordsCopies` makes and restores them.
- **The server's logs are in Denver time** (-0600, -0700 from 1 November), not UTC; a log paste converts its window (request 99).
- **A Features sentence changed since the newest nightly fails the site build** (entry 421) unless one of its note phrases is still to
  come, or it carries `"reworded"` naming that nightly; a widened feature reads "Since nightly.N, changed in nightly.M".
- **A hole can be a ring** (entry 417): on a flatbed with a white lid a hole is as light as the paper; detection fits a circle through its
  rim (`FitCircle`) and the snap does the same. **Restyling adds the new styles before removing the old** (a macOS crash in between).
- **The database is schema version 6** (entry 461): each upgrade's first open copies `grouplab.db` to `grouplab.pre-vN.db` first.
- **OpenCV's QR detectors are never run on several threads** (entry 414): reading a sheet's codes at once crashed Linux in native code
  in 3 of 12 gate runs and hung macOS before. A job dying with exit 134 or 139 is tabulated against what was in effect, not rerun.
- **`docs/notes/external-status.md`** holds TestFlight's and the Store's state, written by their workflows when it changes; read it
  at the start of a run (entries 335 and 336).
- **Alan allowed ssh to the server, standing, 2026-10-26**; it is still announced in panel.md, and sudo's rules are unchanged.
- **Helpers only when Alan asks, ending at the week's reset; chores to chores-haiku, wording to docs-sonnet** (CLAUDE.md, 2026-10-08).
  A finished worker's subjects are compared with main before its worktree goes to the trash.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **A failing site check holds back only its pages** (entry 387): whole-site checks still stop it; held pages keep grouplab.org's copy and
  are listed in website.yml's summary and the weekly consistency issue; each gets one line in for-alan.md when it appears.
