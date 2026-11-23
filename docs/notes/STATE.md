# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-11-23, after entry 513; next entry 515 (the bot's new last line), then entry 514's overnight queue.

---

## In flight

- **Entry 513 (2026-11-23)**: every new build to every store; Apple releases by itself once approved (CLAUDE.md). App Review has
  iPhone and iPad 1.0.242 (build 242, the iOS 18 to 25 one) and the Mac's 1.0 (build 240), submitted 11:59 UTC by `scripts/app-store.py`;
  when the iPhone one is released, reply in question 59's thread (Quack, issue 59). Play production waits on Google (14 days of 12
  testers, about 2 December, then Alan's application). `STORE_SUBMISSIONS_PAUSED` stops all three stores.
- **Request 115 is in** (2026-11-23 11:02 UTC, run by Code on Alan's word: "I give you permission to run commands on the web host"):
  the worker at d893e027 reads bug-reports, feature-requests and help; the server has no support forum, which is now a warning and is
  read by itself once made. A dry run prints its own lines only and writes no log, state or status. Help threads became questions 58
  and 59; replies 58-1 and 59-1 posted at 11:09. 55-2, 56-3 and 57-2 posted at 10:59 after nightly 242.
- **Entry 512 (2026-11-23)**: every new message labels its issue `new-reply` (Alan's `alan-replied`); a Session name and a set's
  "2 of 3" print in the load block's square when it carries words; question 118 answered (a), leave it as built.
- **Entry 510 (2026-11-22)**: `scripts/closed-replies.py` stays beside the issue check until the worker reopens an issue itself.
- **Entry 501 (2026-11-18)**, in part: the Mac App Store copy (`org.grouplab.app`, sandboxed, docs/MAC-APP-STORE.md) is built by
  mac-store.yml after each nightly; whether it runs on a real Mac is request 111; never submitted without Alan's word.

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
- **iOS** (Phase 8): on the App Store (1.0, build 226); 1.0.242 in App Review (entry 513), and every nightly after it; TestFlight for
  the testers already in it; the device checks need a sitting (50), GroupLab Dev on the iPad request 61.
- **Performance** (Phase 9): hole finding on a scan (entries 392, 433, 447, and 2026-10-20's open); a photograph's codes left as they are (question 106, (b)); nothing queued.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Play's closed test is open (entry 490); Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A numbered release** (made by release.yml from a tag): only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Entry 515, then entry 514's queue in order; every new Discord message at every check, replies in `discord-replies/`.
2. The phone's Session name box on Targets (desktop only in entry 512).
3. Request 83, the M834 printing from the computer; request 50's sitting, when Alan has it.

## Blocked, and on what

- **The App Store versions**: Apple's review of 1.0.242 and the Mac's 1.0 (entry 513); request 111 stays a check on a real Mac only.
- **Google Play production**: Google's 14 days (about 2 December) and Alan's application.
- **The iPhone Duo's inner screen**: the Duo leg on Xcode 27.2 (simulator starts folded; IOS-PLAN.md); nothing needed from Alan.
- **Entry 380 section 3**: measuring the M220 labels waits on request 77's photos in `m220-labels`.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **26**; waiting on him: 111 (the Mac TestFlight build on a real Mac), 105 (a screen reader sitting, later), 83, 70, 72, 74 to 77, 104 (104 the point blank card on the M220; 86 aim points through the other optics, for later; 83 the M834 from the computer; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Two, in `docs/QUESTIONS-FOR-PLANNING.md` (118 answered (a) by Alan, 2026-11-23):
- **67** the printer check page as grid style 4 (with Alan); **51** which hole center to report, waits on request 9

## Builds and the site

- **Last nightly:** 1.0.0-nightly.242 (2026-11-23), with entry 512. GitHub's
  Mac runners are sometimes short: rerun a job "not acquired" with --failed.
- **The site** follows main by itself (website.yml), and now also after the Store's version and either screenshot job (entry 476).

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **A session's name is its sheet label** (entry 511): the stored sheet name keeps "target, name" for older builds; lists show
  `SessionNames.Titled` ("name, target") and `SessionSummary.Label` comes from the marking by `json_extract`. No schema change.
- **Code's Discord replies are files** (entry 511): `discord-replies/<issue>-<n>.md` in the crash-reports repo, header `issue:`,
  `thread:`, `written:`, optional `post-after: nightly N`, then the words, plain, ending "This is an AI generated response." (entry 515; nothing else names Alan);
  `reply_problems` in the worker refuses anything else. `discord-replies/PAUSED` stops posting. Drafts no longer go in for-alan.md.
  Entry 512: every new message labels its issue `new-reply` (Alan's `alan-replied`); Code removes both once it has answered.
- **The iPhone's launch screen is generated** (entry 508): edit `scripts/ios-launch.py`, never the storyboard; ibtool refuses runtime
  attributes in a launch screen, so rounded corners there are stretchable images. It follows the system's light or dark appearance.
- **Needs nobody, after entry 508:** the iOS real-tap step on Targets' keyboard Next fails now and then (ios app runs 44891904639 and
  44906341195: the button covered by a light-dismiss overlay, or not showing; passed in 44903839710), from before entry 508; find what
  opens the overlay. Everything else waits on Alan's requests, a tester's photograph (report 52), or a sitting.
- **GroupLab installs on iOS 18 to 25 without support** (entry 505): a new screen comes before the first run's questions there, and a
  scripted run accepts it (`Scenario.AnswerFirstRun`). Reports from below 26 are headed "unsupported iOS" in error-reports.md.
- **A pooled set changed on two devices waits like a session** (entries 498, 504): the banner counts it, its row is tagged, and it opens
  concept A, each sheet tagged; a sheet not here yet is never counted. A pool reaching a device before its sheets asks nothing.
- **A phone page's way back is `Screens.BackButton`** (entry 470), never guessed from words, or `Screens.LeavesPage` for a Done,
  Cancel, Skip or "Another target" that is the page's way out (entry 472); `Entry472BackTests` fails on a new page Back was never pressed on.
- **A Features sentence changed since the newest nightly fails the site build** (entry 421) unless one of its note phrases is still to
  come, or it carries `"reworded"` naming that nightly; a widened feature reads "Since nightly.N, changed in nightly.M".
- **The database is schema version 7** (entry 495 added `pools`, listed by negated id): each upgrade's first open copies the database first.
- **`docs/notes/external-status.md`** holds TestFlight's, the App Store version's and the Store's state, written by their workflows when it
  changes; read it at the start of a run (entries 335, 336 and 491).
- **Alan allowed ssh to the server, standing, 2026-10-26**; it is still announced in panel.md, and sudo's rules are unchanged.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted (entry 222); the Holds line never lists them.
- **Any push to main cancels the running build and nightly**, and a push headed `[notes] ` then builds nothing, so the last commit of a
  push that should make a nightly must not be one.
- **A failing site check holds back only its pages** (entry 387): whole-site checks still stop it; held pages keep grouplab.org's copy and
  are listed in website.yml's summary and the weekly consistency issue; each gets one line in for-alan.md when it appears.
