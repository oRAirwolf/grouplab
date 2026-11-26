# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-11-26, after requests 120 and 121 (the platform sentence; the Mac's crash on launch); the inbox is empty.

---

## In flight

- **Requests 120 and 121, done 2026-11-26**: Apple turned down Mac 1.0 for crashing on launch. Cause found (`mac-launch.yml`): inside the
  sandbox, .NET started from separate runtime libraries is refused LaunchServices and AppKit aborts; one file is let through. The Mac App
  Store copy is now one file (`PublishSingleFile`, store copy on macOS only) and passes its self-check in the sandbox; the nightly's
  sandbox check now stops the job. **Mac 1.0.247 in review has the fault**: request 122 asks Alan to take it out; a withdrawn build is
  never resubmitted, so 248 (uploaded, waiting behind 247's review) goes in. The platform sentence (120) is in PLATFORM-SUPPORT.md, README and download page.
- **Question 120 (b): LLVM on for the iPhone and iPad** (`GroupLabAppleLlvm`; RESTORE.md has the undo): nightly 247 says `llvm: ran`.
  Apple error reports are read for an LLVM cause (switch off at once if one could be); the device comparison at request 50's sitting.
- **Report 60's poster** says nightly 247 finds .338 holes poorly and sent four three-page sets (testing only): read them on 243 and 247
  when the nightly's submissions copy brings them here, to see whether the dot-pattern rule drops large holes (error-reports.md).
- **Entry507SplashTests fail here** (6, thread ownership; pass in CI). closed-replies.py stays (entry 510).

## The next three

**What is left of the plan** (entry 318 section 5, one line each, with what blocks it):
- **Detection, angled photos**: done (entry 324). **Holes on any target**: Experimental, measured; harder cases wait on request 58.
- **Detection on blank paper with no definition** (Phase 4): needs a photo of plain paper with real holes at a known scale (54, 58).
- **Chronograph files** (Phase 5): every one of Alan's Xero exports reads, and a timed string proposes its pairing (entry 342);
  checking a proposal needs a sheet shot with its string.
- **Velocity regression** (Phase 5): on both screens, with a session's own conditions (entry 416); the phone's picture waits on 50.
- **Synchronization** (Phase 7): built on every platform (entries 461 to 472), tested on a simulated cloud with three devices; a real
  cloud client over weeks needs Alan's two devices (PROOF-CHECKLIST.md, line 4 of entry 472's list).
- **iOS** (Phase 8): on the App Store as 1.0.247 (entry 513), each nightly after it to App Review; TestFlight for
  the testers already in it; the device checks need a sitting (50), GroupLab Dev on the iPad request 61.
- **Performance** (Phase 9): hole finding on a scan (entries 392, 433, 447, and 2026-10-20's open); a photograph's codes left as they are (question 106, (b)); nothing queued.
- **Proof of the 31 "built, not proven" features**: docs/PROOF-CHECKLIST.md, one checklist of material; question 79 on five met.
- **Stores**: Microsoft follows the nightlies (entry 369); Play's closed test is open (entry 490); Test Lab waits on request 62.
- **Deferred on purpose**: the designer's canvas and automatic detection on a bought target; nine are recognized by fingerprint.
- **A numbered release** (made by release.yml from a tag): only when Alan asks, after the eight checks in `docs/RELEASE-PLAN.md`.

1. Nightly 248's Mac store copy through mac-store.yml's sandbox check and into review in 247's place (request 122); Apple's answer.
2. Report 60's four sets read on nightlies 243 and 247 (the .338 holes); a real phone's and iPhone's log at request 50's sitting.
3. Request 83, the M834 printing from the computer; request 50's sitting, when Alan has it.

## Blocked, and on what

- **The Mac App Store**: 1.0.247 in review has the crash; request 122 (take it out), then build 248 or later. Request 111 needs build 248 or later.
- **Google Play production**: Google's 14 days (about 2 December) and Alan's application.
- **The iPhone Duo's inner screen**: the Duo leg on Xcode 27.2 (simulator starts folded; IOS-PLAN.md); nothing needed from Alan.
- **Entry 380 section 3**: measuring the M220 labels waits on request 77's photos in `m220-labels`.
- **The iOS GroupLab Dev upload**: request 61 (its App ID, profiles and record).
- **The phones**: not reachable over adb since 2026-09-30. **Entry 170 section 4.4.** Request 9. **Entry 166 section 3.2.** Request 16.

Open requests in `docs/notes/for-alan.md`: **29**; waiting on him: 122 (take Mac 1.0.247 out of review, most urgent), 119 (Google's production answers, on or after 2 December), 118 (a Discord post for Android testers), 111 (the Mac TestFlight build on a real Mac), 105 (a screen reader sitting, later), 83, 70, 72, 74 to 77, 104 (104 the point blank card on the M220; 86 aim points through the other optics, for later; 83 the M834 from the computer; 77 M220 labels; 76 scale markers on real paper; 75 two reference files and a tape measure; 74 a kitchen table photo; 70 Fenix's report package; 67 TestFlight team distribution off; 59 TestFlight groups; 62 Firebase Test Lab; 56 printer scale; 50 the device sitting,
now with a look at the velocity card; 54, 57, 58 at the range; 46 backups on 4 October; 61 GroupLab Dev's Apple
steps; then 33, 9, 16 and 20).

## Open questions

Two, in `docs/QUESTIONS-FOR-PLANNING.md` (120 answered (b), 2026-11-25; 118 answered (a); 119 withdrawn):
- **67** the printer check page as grid style 4 (with
  Alan); **51** which hole center to report, waits on request 9

## Builds and the site

- **Last nightly:** 1.0.0-nightly.248 (2026-11-26, 1fab547), the Mac App Store copy as one file; its Mac build passed the sandbox check and is on TestFlight. GitHub's
  Mac runners are sometimes short: rerun a job "not acquired" with --failed.
- **The site** follows main by itself (website.yml), and now also after the Store's version and either screenshot job (entry 476).

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

Inbox files are never committed, so CI sees an empty inbox and this line says none. Waiting locally: none.

## Things that would surprise somebody who was not here yesterday

- **An App Store refusal now says why** (entry 516): `said_by` in testflight.py keeps Apple's associated errors; `app-store.py --inspect`
  (testflight.yml run by hand with inspect) lists each platform's review submissions. Both status workflows publish the site on a version change.
- **Android is compiled with LLVM** (`EnableLLVM` in GroupLab.Android.csproj, Release only); `android-emulator.yml`'s `no_llvm` builds
  without, for comparing. The phone's log has `read.holes` (the hole finder's nine steps, cores, `jit`) and a `result=` fingerprint on
  `phone.detect` (`ReadFingerprint`); the same fingerprint on the same picture means the same result to the bit.
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
