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

- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 261, 260 of them.

---

## 2026-09-30, entry 317: the token budget, a standing rule (replaces entry 296's spending rules)

**Status: done 2026-09-30; in force from this run on.**

From planning, 2026-09-30, approved by Alan ("Go ahead"). In force from the next session start and every session after.

## Why

Alan's usage log: about 99% of tokens are cache reads, the conversation re-sent on every step. Cost is context size times steps times
sessions running at once. These rules cut all three without touching the app's quality.

## Budget

- About 12% of the weekly limit per day, about 15% per hour within a 5-hour window; the rest is reserve.
- Once a day, run `npx.cmd ccusage@latest daily` (Node is installed) and put one line in for-alan.md: yesterday's tokens and the trend.
  If a day ran hot, the next day runs one worker.

## Habits

1. **A fresh session per block of work.** When a batch is done: commit, update STATE.md, and end the session (or /clear) rather than
   carrying the day's history. Compact early when a session grows.
2. **One worker by default**; two only for truly separate areas; three only when Alan asks.
3. **Models, with quality first (Alan: "As long as the cheaper model mode does not affect the quality of the application").** All
   application code, tests, detection, statistics, camera and UI work stay on the main model. Cheaper models only for chores that
   cannot change the app: screenshot and PDF regeneration, searches, log and CI summaries, doc consistency passes, file moves. Do not
   switch the main session to a cheaper model.
4. **Smaller files to reread.** Keep STATE.md under 120 lines. Trim for-alan.md to open requests and the latest summary; move answered
   requests and old "UNDER WAY" and "READY" paragraphs to `docs/notes/for-alan-archive.md`. Read NOTES-FROM-PLANNING.md by entry
   number, never whole.
5. **Ceremony once a day, not per change:** screenshots, guide PDFs, claims backing, the site consistency audit, STATE.md rewrites and
   folding entries into the log happen once per nightly.
6. **No watching.** Start a CI run or a long build, work on something else, check back once when it should be done (a monitor or a
   long sleep, never minute-by-minute polling).
7. **Targeted tests locally,** the full suite in CI.
8. **Priorities when the budget is tight:** bugs Alan hit and iOS parity first, approved features second, polish last.
9. **Overnight runs continue, at one worker** (Alan: "Yes").
10. If a limit is hit: one line in for-alan.md saying where the run stopped, commit, stop.

## 2026-09-30, entry 316: the Mac download names no chip generations

**Status: done 2026-09-30.**

From planning, 2026-09-30, for Alan: "under the macos download, it says 'M1, M2, M3, M4. Not an Intel Mac'...isn't the M5 SoC out now?
Should it just say 'M-series SoCs' or something to that effect so it doesnt have to list out the cpu's?"

1. The Apple silicon card on grouplab.org/download/ reads "Any Mac with Apple silicon (M-series). Not an Intel Mac." (or as close as
   the card's layout allows), and its description "For any Mac with Apple silicon." No list of generations anywhere.
2. Search the site sources, README, user guide, PLATFORM-SUPPORT.md, release notes templates and the app's own update text for "M1, M2"
   and any other list of chip names, and word them the same way (rule c). The Intel card stays as it is.
3. Small; do it with the next site change, no rush.

## 2026-09-30, entry 315: letting Code drive and watch the app without Alan's hands (dev builds, iOS first, Android too)

**Status: sections 1, 2 and 4 and amendment 1 done 2026-09-30 (b8484f72, a8ce0e90, c6a31b63, d3cf5717); not done: section 3 (the replay camera), amendment 2 items 3 and 6 (the replay camera on Android, Firebase Test Lab); the iOS Dev app's upload waits for request 61.**

From planning, 2026-09-30, for Alan. Alan: "it seems like it [pymobiledevice3] has most of the features code will need to develop and
troubleshoot except the tap swipe and type. Is there anything that can be added to the development version of the application that would
give extra visibility or development support needed to automate code's ability to develop and test the application without me doing
things manually?"

Yes: make the app drivable from inside, so taps are not needed. One design for iOS and Android, so the same scripts test both.

## 1. An automation bridge, off by default

- A hidden developer switch (Settings, About, tap the version seven times, or on by default in GroupLab Dev on Android) turns on a small
  command server inside the app, listening **only on the device's localhost**, with a random token shown on screen and written to the
  app's Documents folder. Code reaches it over USB: `pymobiledevice3 usbmux forward` on iOS, `adb forward` on Android. Nothing listens
  unless the switch is on; nothing is reachable from the network; public App Store builds may keep the switch but it starts off.
- Commands (JSON): go to a screen; press a control by its automation name; type into a field; choose a list item; scroll; open a picture
  already in Documents as if chosen or shared; run the full reading on it; set a setting; return the visible screen's element tree
  (names, text, bounds, enabled); take an in-app screenshot; return the last N log lines, timings and memory; reset to a clean state.
- Every control that matters gets a stable automation name (Avalonia's AutomationProperties), checked by a test so none goes missing.

## 2. Scripted runs without the bridge

- On launch, a dev build reads a scenario file (JSON) from Documents, or launch arguments passed by `pymobiledevice3 developer dvt
  launch` / `adb shell am start`: steps like "open Capture, load sample.jpg, read, open Fix holes, move hole 3 by 0.02 in, save, open
  Compare". It writes a results file, screenshots and the log back to Documents for Code to pull. Android already has the
  `org.grouplab.test.picture` extra; extend that into the same scenario format.

## 3. The camera without pointing a camera

- A "replay camera" in dev builds: the capture screen takes its frames from a picture or a short recorded frame sequence in Documents
  instead of the live camera, so Guided, the words, the level, the torch logic and auto-capture timing can be tested by script.
- During Alan's sittings, record short frame sequences (a few seconds, low resolution, local only, never committed) so real framing can be
  replayed against every later build: the camera's own regression test.

## 4. More visibility

- Structured JSON-lines log with per-stage timings (`read.stage`, `camera.say`, `camera.level`, `camera.torch`), state changes, errors
  with stack traces, memory and thermal state; unhandled exceptions written to a file before the app dies.
- A debug overlay (dev switch): frame rate, current guidance verdict and its failing check, tilt values, torch level, reading stage and
  elapsed time, drawn over the camera and the reading screen, so a screenshot alone explains a hang.

## 5. What still needs Alan

Pointing the real camera at a real sheet, the first setup of pymobiledevice3 (the Apple Devices app, a cable, Trust This Computer, and
Developer Mode on the iPad), and judging how things look. Write the setup as a request with exact steps.

## Order

After entries 313 (the hang) and 311 items 1 and 2 (logs out of the iPad). Build section 4 first (it helps every fix), then 2, 1 and 3.

## Amendment, 2026-09-30: where the dev features live (Alan asked about Apple's review; "Can you have code build in all of the development features you just mentioned?")

Alan wants all of sections 1 to 4 built. To keep them away from Apple's review:

1. **The public app (`org.grouplab.app`, internal and Public Beta in lockstep, later the App Store) carries none of the automation
   bridge, scenario files or replay camera.** They are compiled out (a build property, as `GroupLabUpdater` is on Android), not hidden
   behind a switch: App Review guideline 2.3.1 forbids hidden or undocumented features, and Beta App Review applies it to external builds.
   The public app keeps what is visible and documented: the structured log, crash files, "Send diagnostics", and the debug overlay as a
   plainly labelled setting ("Show diagnostics on the camera"), described in the guide.
2. **A separate iOS app, GroupLab Dev (`org.grouplab.app.dev`, "GroupLab Dev" on the home screen, its own icon as on Android),**
   built by the nightly with everything in sections 1 to 4, sent to TestFlight and added **only to the internal group** (GroupLab Team),
   never to an external group or the App Store. Internal TestFlight builds are not reviewed by Apple; they pass only the automated upload
   checks, which look for private API use, and a localhost listener uses public APIs only. Bind it to 127.0.0.1 so iOS does not ask for
   local network permission.
3. **Android:** GroupLab Dev already exists and carries all of it; the Play build does not.
4. **Alan's steps for the iOS Dev app** (write them as a request with exact paste lines, like request 55): App ID `org.grouplab.app.dev`
   with App Groups (a group `group.org.grouplab.app.dev`), the share extension's ID `org.grouplab.app.dev.share` if the Dev app keeps
   sharing, two App Store profiles from the existing distribution certificate, the secrets for them, and an App Store Connect app record
   named "GroupLab Dev" (or "GroupLab Dev Build" if taken). Until those exist, build and test the Dev app on the simulator in CI only.
5. A test that the public build contains none of the bridge's types, so it cannot slip in.

## Amendment 2, 2026-09-30: Android GroupLab Dev gets the same, plus wider device coverage

Alan asked whether Android GroupLab Dev already has all of this. It has part of it: launch extras (`org.grouplab.test.picture`,
`.camera`, `.capturemode`, `.press`, `.idle`, `.shotstozero`), a sitting's pictures kept on the phone, self-update, and adb (logcat,
screenshots, screenrecord, install, input taps). Missing, and wanted:

1. **The same automation bridge** (section 1) over `adb forward`. adb can tap, but only by screen coordinates: Avalonia draws its own
   controls, so Android's UI tools cannot find buttons by name, and a coordinate breaks whenever a layout moves. The bridge addresses
   controls by automation name on both platforms.
2. **The scenario format** (section 2) replacing the one-off extras, which stay as shortcuts that run one-step scenarios.
3. **The replay camera** (section 3) and the sitting clips, shared with iOS so one clip tests both.
4. **The debug overlay and structured timings** (section 4).
5. **A clean-state command and test data:** reset GroupLab Dev to a first run, or load a fixed set of sample sessions, rifles and printers,
   so every scripted run starts from the same place.
6. **More phones without Alan buying them:** consider Firebase Test Lab (Google's real and virtual devices; a free daily quota) to run
   the scenario suite on Samsung, Xiaomi, Oppo, Pixel and other models, which is where entry 292's photo-app differences would show.
   Report its cost and what the free quota covers before relying on it; nothing paid without Alan.

## 2026-09-30, entry 314: calibers, cartridges and a lookup of every cartridge

**Status: done 2026-09-30 (96c848a6); question 76 asks whether a held-back cartridge typed in full should count, built as B meanwhile.**

From planning, 2026-09-30, for Alan.

## What Alan asked

"I think we should probably add some options to the caliber dropdown in settings. I like the caliber list in ballistic x and the hornady
app ... We should include the common calibers. We should also have an option for common cartridges for people who do not know the
caliber. There should be a setting that lets you choose if you want one or both. For somebody like me, I only want caliber to show up.
For somebody that is new, they may want to type in the cartridge name." And a lookup of every cartridge that autocompletes as one types.
(Ballistic-X picks the caliber on a wheel, .243 to .280 and more; Hornady offers eight bullet diameters. See the planning study
`claude/competitor-study.md`.)

## 1. A setting: what the caliber box offers

Settings, "Caliber box shows": **Calibers** (bullet diameters), **Cartridges** (names), or **Both** (the default for a new install;
Alan will choose Calibers). Desktop, Android and iOS alike, one setting.

- **Calibers:** the bullet diameters in common use, each written as its diameter with its usual names, for example ".224 (5.56 mm)",
  ".243 (6 mm)", ".264 (6.5 mm)", ".277 (6.8 mm, .270)", ".284 (7 mm)", ".308 (7.62 mm, .30)", ".311", ".338", ".355 (9 mm)", ".357",
  ".400 (10 mm, .40)", ".429 (.44)", ".452 (.45)". Derive the list from the cartridge table's tier 1 to 2 diameters, and let any other
  diameter be typed in inches or millimetres.
- **Cartridges:** the names, each carrying its diameter ("6.5 Creedmoor, 0.264 in").
- **Both:** one list, calibers first, then cartridges as you type.
- The box itself behaves as entry 312 section 5 says (short name after a choice, select all on tap, a clear button).

## 2. The cartridge lookup

`entry-314-cartridges-draft.csv` beside this entry: 661 cartridges from Wikipedia's rifle and handgun lists plus 12 precision cartridges
Wikipedia omits (6mm Dasher, 6mm GT, 6 BRA, 6 BRX, 6x47 Lapua, .30 BR, 6.5 SAUM, .284 Shehane, .22 GT, .25 Creedmoor, and 5.56x45 NATO
and 5.45x39), each with aliases, bullet diameter in inches and mm, rifle or handgun, a tier (1 everyday, 2 common, 3 niche but current,
4 rare or obsolete) and a precision flag. Planning's draft, compiled by research agents from the two pages; some obsolete diameters are
marked approximate or unsure in the note column. Alan decides the cutoff (below); until he does, build with it as it stands:

1. Move it into the repository as data (for example `src/GroupLab.Core/Data/cartridges.csv`), one table used by every platform, with a
   test that every row has a diameter and a tier, names are unique after aliases, and every tier 1 to 2 row's diameter is right against
   SAAMI or CIP where one exists.
2. Autocomplete searches name and aliases, forgiving punctuation and spacing ("65 creed", "6.5cm", "308", "9mm"). Ranking: exact alias
   first, then tier, then precision flag.
3. What is shown without typing (the dropdown's common list) is tier 1 and 2 plus every precision row; everything else appears only as
   typed. So the full table can hold all tiers without cluttering the list.
4. The existing `CartridgeTable` groups several cartridges under one diameter ("6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and
   others"); keep that grouping for the Calibers list and use the new table for Cartridges.
5. Site, guide and README lines to match (rule c).

## Open for Alan

The cutoff for what the lookup holds at all: all 661, or tiers 1 to 3 plus precision (227), or tiers 1 to 2 plus precision (111).
Planning recommends all 661 in the lookup with only tiers 1 to 2 plus precision in the unprompted list, since typed search keeps the
rare ones out of the way.

## Answered (added 2026-09-30)

Alan: "It seems like the cartridges are fine as recommended." So: all 661 rows in the lookup; tiers 1 and 2 plus every precision row
(111) in the list shown before typing. Build it as section 2 says.

## 2026-09-30, entry 313: the iPad hangs on "reading the sheet's codes", Cancel does nothing (PRIORITY), and the preview's layout

**Status: done 2026-09-30 (overnight/reading, merged as 3638462d, 5a18a86a, 4d27fcbe); section 1.4's times on the iPad itself come from the next sitting's log.**

From planning, 2026-09-30, for Alan, from the second look at TestFlight build 134 on the iPad mini.

## 1. PRIORITY, before the deadline: reading hangs and cannot be cancelled

Alan: "After taking a picture of the target, it hangs for a long time on reading the sheet's codes for me. One time it seemed to time
out entirely. The screen locked itself and when I opened it again, it was still saying that. I pressed cancel and nothing happened. I
ended up having to force close the application."

1. **Cancel must always work**, within about a second, from any stage of reading, and return to Capture with the picture kept.
   A Cancel that does nothing suggests the reading holds the UI thread, or the cancellation token is not passed to the slow step.
   Reading runs off the UI thread with the token checked between stages and inside long loops (the code enlarging, each marker pass).
2. **A time limit**: if reading takes longer than a sensible bound (measure the normal time on the iPad first), stop and say what was
   tried ("The codes could not be read in time; try again closer, or choose the sheet") rather than spin forever.
3. **Survive the lock**: iOS suspends the app when the screen locks. On coming back, reading either carries on or stops cleanly with a
   message; it never shows a frozen "reading" line. Hold a background task or restart the step on resume.
4. **Why is it slow on the iPad at all?** Measure each stage on the iPad's own pictures (the iPad mini camera is 12 MP; is the full frame
   going through the code search, several times, at full size?). Compare with the same picture on the simulator and the desktop.
   Log each stage's time (`read.stage` lines) so the next sitting's log shows where it goes.
5. A test in the iOS self-test: a large picture, Cancel pressed during code reading, back on Capture within a second.

## 2. The preview (Alan's screenshot, 04:39, the camera open on Guided)

What it shows: black bars on both sides of the preview (about 40 points each) and the preview's top edge peeking out between the
status bar and the panel, with the panel laid over the top part of the picture. So "space at the top" is the preview sitting under a
floating panel rather than below it. Choose one clean layout: either the panel sits above the preview (the preview starting under it,
centered in what is left), or the preview fills from the status bar down and the panel is fully opaque, with the framing guides and the
"whole sheet in view" check knowing which part of the picture is hidden. The first is simpler and honest about what will be captured.
Also: a stray dash at the right end of the quality bar (a clipped label or a character); remove it.

## 3. Noted, nothing to change

"The shutter lag time is acceptable."

## 2026-09-30, entry 312: Alan's iPad screenshots of build 134: Compare, the caliber and distance fields

**Status: done 2026-09-30 (overnight/screens, merged as 54ed59a5 to 2d150031).**

From planning, 2026-09-30, for Alan. Four screenshots from the iPad mini (planning has seen them; not in the repository). Alan's words:
"The compare loads doesnt take up the entire screen. The distance box brings up the keyboard when it should bring up the num pad. The
caliber fills in the text with what you select and if you want to change it, you have to delete everything and then start typing again."

## 1. Compare on a tablet uses the whole width (iPad, and Android tablets and the open Fold)

- "Each load's group" draws two plots of about 265 points each in the left half of a 1,300 point wide card, with the right half empty.
  On a wide screen the plots grow to share the width (two loads side by side filling the card, three or four in a row or a 2 by 2 grid),
  still square and at one shared scale.
- The page's column may stay at a comfortable reading width for text, but the plots card and the charts should use the full width.
- There is no way back to Sessions visible at the top of Compare on the iPad; check the back control is there on iOS as on Android.

## 2. A bug in Extreme spread, as measured

Both loads read 0.56 mil, yet the first load's dot sits far right and the second's far left of the same row width. Two equal values must
sit at the same position on one shared axis. Check the axis (it may be drawing each row on its own scale, or placing the dot by an index).
Add a test: equal values, equal x.

## 3. The verdict card still reads as statistics

"2026-09-30, 02:35's sigma is 1.01 times 2026-09-30, 03:55's, 95 percent interval 0.76 to 1.34" and "To resolve a difference of 10
percent, with 80 percent power at the 5 percent level, takes 434 ..." are what entry 295 section 3 asked to put in plain words. Plain first
(for example "Their spreads are within about a third of each other either way; to tell a 10 percent difference apart would take about 434
shots each"), with the exact figures behind the term help or a "Details" expander.

## 4. The distance field opens the number pad

On iOS and Android, every numeric field (distance, seed, click value, velocity and the rest) opens the number pad with a decimal point
(iOS decimal pad; Android number with decimal), not the full keyboard. Check each numeric entry in the app, desktop excepted.

## 5. The caliber field can be changed without deleting everything

Choosing a caliber now fills the box with its whole entry ("6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and others: 0.264 in (6.71
mm)"), and changing it means deleting all of that. Instead:
- after a choice, the box shows a short name ("6.5 Creedmoor, 0.264 in") with the full entry as the line under it;
- tapping the box selects all of its text, so typing replaces it at once;
- a clear (x) button inside the box empties it.
Same on Android and iOS, and the desktop's box gets the select-all on focus and the clear button too.

## 6. A stale message on Capture

The 03:01 screenshot shows "GroupLab needs the camera to take the picture. Allow it, then press Take a picture again." under the
buttons, after Alan had already allowed the camera (entry 311, item 1). The line should disappear once permission is granted, and on a
later visit to Capture.

## Still owed by Alan

The screenshot of the gap at the top of the camera preview (entry 311 item 2) was not among these four.

## 2026-09-30, entry 311: the first iPad sitting (TestFlight build 134), and getting the iPad's logs to Code

**Status: done 2026-09-30 (4930c1c2, f8da0c6d, 5f8287af, 451c21b7, acfb1823); whether Guided's sooner pictures read as well waits for the next sitting's log.**

From planning, 2026-09-30, for Alan. GroupLab 134 is on Alan's iPad mini through TestFlight. The first TestFlight sitting
(`docs/IOS-PLAN.md`), Alan's words:

1. Camera permission: "I was able to give it permission to use the camera." **Yes.**
2. Preview: "The preview looks good but there is some space at the top. I took a screenshot." **Partly.** The screenshot follows in
   `C:\Dev\grouplab-local\ipad-sitting\` (local only, metadata stripped before any use, never committed). Find what the gap is (safe area
   counted twice, the status bar strip, or the 4:3 letterbox placed at the top instead of centered) and fix it.
3. Guided: "Guided mode worked but like on android, the threshold for taking a photo automatically seems too high and it takes a long
   time for it to do it." **Works, too slow.** Same complaint as Android.
4. Level: "The level on the camera screen does not turn green when it is level. I am not sure if this is because it is just not working
   or if it is way too sensitive." **No.**
5. Manual: "Manual mode works." **Yes.**
Items 6 onward were not tried yet.

## 1. Guided fires sooner, on both platforms

Measure first, from the logs (Android's sittings already have `camera.say` lines with frame times): how long from the first Ready-worthy
frame to the shot, and which check holds it back (sharpness, steadiness, the ready-frame count, the code count). Then loosen what holds it
back without letting in pictures the detector reads worse (check against the scoreboard: fired pictures must still read as well). Target:
under about a second of steady framing on a good sheet. Same change on Android and iOS.

## 2. The level turns green

Log the raw gravity or attitude values and the tilt the level computes (`camera.level`) and check on the iPad whether the axes are
right for the iPad's orientation and whether the green tolerance is sensible (Android's is the reference). Likely suspects: the iPad's
orientation not applied to the axes, or a tolerance tighter than the sensor's noise. Green should hold steadily when the iPad lies flat on
a table.

## 3. Logs from the iPad, three ways (Alan: "How can code get detailed logs and information from the ios/ipados app?")

1. **Share the log from the app:** Settings, About, "Send diagnostics" (or the Android equivalent's name): the share sheet with the
   log file and the sitting's record, so Alan can AirDrop, save to Files or OneDrive, or email it. The same button as Android if it has one.
2. **The app's folder in Files:** set `UIFileSharingEnabled` and `LSSupportsOpeningDocumentsInPlace` (check what Info.plist already
   has), with the log and a sitting's pictures kept in Documents (the iOS twin of GroupLab Dev's kept pictures, with the same switch to
   turn it off). They then appear in the Files app under On My iPad, GroupLab.
3. **Over USB from Alan's Windows computer, like adb:** with Apple's devices driver (the Apple Devices app or iTunes) installed,
   `pymobiledevice3` (Python) can stream the iPad's system log filtered to GroupLab and copy the app's Documents folder, which file
   sharing (item 2) allows even for a TestFlight build. Write the setup as a request for Alan (install the Apple Devices app, plug in, trust
   the computer), then a script in `scripts/` like the Android ones. Touch only GroupLab on the iPad; nothing else is read, never
   notifications or other apps' data.
4. **TestFlight's own feedback and crashes:** a screenshot shared as beta feedback in TestFlight, and every crash, land in App Store
   Connect. Read them with the App Store Connect API key already in the secrets (beta feedback and crash submissions) in a small script
   or a scheduled workflow step, and summarize new ones in for-alan.md. Never publish a tester's screenshot or email.

## Order

1 and 2 before the iOS deadline if they fit (they are what the next sitting needs), then 3.

## 2026-09-30, entry 310: TestFlight, the team and the public beta always on the same build

**Status: built 2026-09-30; section 5 done (c5a359ad); proven against Apple once Alan's two groups exist (request 59); the beta link on the site waits for his public link.** The testflight workflow (after each nightly and every half hour, on Linux) runs scripts/testflight.py: the newest processed build goes to Public Beta with that nightly's notes as What to Test and is submitted for Beta App Review; GroupLab Team gets the newest build Public Beta can install, so neither is ahead; a rejection moves nothing and is said; a missing group or key does nothing and says so. Nothing is ever taken out of a group. Apple offers no way to withdraw a waiting submission, so a newer build waits for the older review and follows it.

From planning, 2026-09-30, for Alan.

## What Alan decided

1. "I want to give unholy and fenix app store connect team rights." Alan is collecting their Apple ID emails and adds them himself in
   Users and Access; planning suggested the Marketing role, limited to the GroupLab app, without access to reports. Once they accept, they
   go in the internal group. Code never handles their emails; do not write them into any file.
2. "I would also like to keep the external testing version up to date with the internal testing version. They should always be on the
   same version."

## The groups (Alan creates them in App Store Connect)

- Internal: **GroupLab Team** (Alan, then Unholy and Fenix). Automatic distribution **off**, because the nightly distributes (below).
- External: **Public Beta**, with the public link on, once Alan has filled in Test Information (feedback email, marketing URL
  https://grouplab.org, privacy policy https://grouplab.org/research/what-grouplab-sends/, review contact, no sign-in).

## What Code builds: one step at the end of the nightly's iOS job

Using the App Store Connect API key already in the secrets:

1. Wait for the uploaded build to finish processing (poll in minutes, with a time limit; the job may hand off to a small follow-up workflow
   rather than hold a Mac runner).
2. Add the same build to **Public Beta** and submit it for Beta App Review when Apple requires it.
3. **Lockstep, as Alan asked:** the build is added to **GroupLab Team** only once it is available to Public Beta: at once when Apple
   needs no review, or when the review is approved. Both groups therefore always carry the same build. A later build that arrives while
   one is waiting supersedes it (submit the newest, withdraw the older submission where the API allows).
4. If a review is rejected, nothing moves: both groups stay on the last approved build, the run summary says why, and a line goes in
   for-alan.md. Never move one group ahead of the other.
5. Add `ITSAppUsesNonExemptEncryption = false` to the iOS Info.plist so builds do not wait on the export compliance question.
6. Look the groups up by name (GroupLab Team, Public Beta); if either does not exist yet, skip this step with a clear line in the summary,
   so the nightly never fails for it.
7. Record each nightly's TestFlight state (build number, review status, both groups' current build) in the run summary.

## After Alan sends the public link

Add "Join the iPhone and iPad beta" with that link to the download page, the README and the guide (rule c), next to GroupLab Dev for
Android, with one honest line: builds reach testers after Apple's check, usually within minutes, sometimes a day.

## Order

After the current iOS work, before the deadline if it fits: it completes entry 290's TestFlight path.

## 2026-09-30, entry 303: mil bulls only as a designer option, and the self-update worked

**Status: done 2026-09-30.** 1: no new built-in sheets; the desktop designer takes a bull's size in mil or MOA at the sheet's distance as well as inches (the phone has no grid designer). 2: GroupLab Dev updated itself on the Fold 7 without adb, after a quick Play Protect scan, recorded in ANDROID.md and the guide. 3: request 56 now says to print and add the Scanner check on the computer. 4: the drafted Discord reply is gone from for-alan.md.

From planning, 2026-09-29, for Alan.

1. **Mil bulls (entry 294 section 3), answered:** "I dont really see much value in adding mil bulls and I don't want to have too many targets
   initially. I would say make it an option in the target designer." So: no new built-in sheets. In the designer, the bull size can be given
   in mil (at the sheet's distance) as well as inches and MOA. Guide line to match.
2. **Entry 288 device check, from Alan:** GroupLab Dev updated itself on the Fold 7 without adb. The phone made him scan the app first
   (Google Play Protect's scan of apps from outside the Play Store); he does not mind, it is quick. Record it in `docs/ANDROID.md` section 17
   and mention the scan in the guide's update section so it does not surprise anyone.
3. **Request 56:** planning is telling Alan to print from the desktop at Actual size and add the Scanner check on the desktop.
4. **Glawk:** Alan already replied on Discord; drop the drafted Discord reply from for-alan.md.

## Order, with entry 296's rules

iOS stays first until the deadline. These go to one desktop and Android worker, in this order: 302 (torch), 299 (Settings), 298 (panes),
297 (colors), 300 (live preview), 303 item 1. Entry 301 waits for Alan's choice.

## 2026-09-30, entry 308: commercial targets for the detector, and request 54 clarified

**Status: done 2026-09-30; request 54 clarified, request 58 opened.** The scoreboard reads another maker's target as an "any target" case: a truth file with the shot scan, an optional blank scan and the shot count, found holes against the count and every mark on the blank a false one (a made-up orange target: 5 of 5, none false). No commercial scans exist yet; request 58 asks for them. Request 54 now asks for a plain target with no maker's logo for the home page, since GroupLab never shows another maker's target.

From planning, 2026-09-30, for Alan.

1. **Alan's photos of 20 September** (the orange commercial target that says "100-yard precision rifle target", with close-ups of the
   groups) stay what they are: test material for the detector. Under the standing rule they are never shown or named on the site, so they
   cannot be request 54's home page picture.
2. **Request 54 is still wanted**, for the home page: a plain target with no brand design or logo on it (or one whose logo can be cropped
   off). Rewrite request 54 to say so plainly.
3. **Alan offers to buy several commercial targets that fit on the scanner glass.** Planning says yes, for testing automatic holes on any
   target (a stated goal). Add a request for Alan: for each target, scan it at 600 dpi **before** shooting, shoot it, then scan it again
   and take two or three phone photos (square on, and one at an angle). A blank scan of the same target makes each one a hard test case.
   Suggest a spread: black bulls, fluorescent or splatter targets, colored diamonds, small grid targets. Files go in
   `C:\Dev\grouplab-local\commercial-targets\`, local only, never committed or shown.
4. **Use them:** add the pairs to the scoreboard as "any target" cases, and report how the automatic finder does on each. The wiring of the
   automatic finder to targets GroupLab did not print (DETECTION-LEARNING-STUDY.md) is the work this feeds; plan it after the iOS deadline.

## Order after entry 303's list

305 (small), 307 (export), 306 (Mac, with request 55), 308.

## 2026-09-30, entry 306: notarize the Mac build (Alan: "Mac notarization: yes")

**Status: done 2026-09-30 but the xattr instructions, which stay until a notarized nightly is seen accepted; the Mac steps are in request 55.** The Mac packages are signed inside out with the hardened runtime, notarized (the build fails unless Apple accepts it), stapled and checked by Gatekeeper, once the Developer ID certificate and its password are set; until then they build unsigned as before. scripts/macos-signing.py decides, as ios-signing.py does, reusing the team ID and App Store Connect key.

From planning, 2026-09-30, for Alan.

1. **Why:** today a Mac user must run `xattr -dr com.apple.quarantine` in Terminal before GroupLab opens. A Developer ID signature plus
   Apple's notarization lets macOS open it normally. Not the Mac App Store (that would need a sandbox and no self-updater).
2. **Add Alan's steps to request 55**, after the iOS secrets, in the same style: create a "Developer ID Application" certificate from a
   certificate signing request made in Git Bash with OpenSSL (as for the iOS distribution certificate), download it, make the .p12, and set
   the new secrets with `gh secret set` (for example `MACOS_DEVID_CERT_P12` and `MACOS_DEVID_CERT_PASSWORD`). Reuse the App Store Connect
   API key and team ID he is already setting for iOS for `notarytool`. Files stay in `C:\Dev\keys\apple`, which Code never opens.
3. **CI:** sign the .app with the hardened runtime and the entitlements .NET and Avalonia need, submit with `notarytool --wait`, staple the
   ticket, and package a signed, notarized DMG (or zip). Sign only when the secrets exist and are well formed, as the iOS check does;
   otherwise build unsigned as today.
4. **Once a notarized nightly passes a check on a Mac** (the Mac tester, request 16, or CI's Gatekeeper assessment `spctl`), remove the
   `xattr` instructions from the README, site and guide (rule c). Not before.

## 2026-09-30, entry 307: export everything to one file, and import it on any GroupLab

**Status: done 2026-09-30, the iOS "Open in" for a .grouplab file included (4362991b, 4123240c).** "Export all my data" writes one .grouplab file (sessions with their pictures and marks, rifles, barrels and loads, designed sheets, printers and units), and "Import data" on any GroupLab shows what it will add, skips what is already there, and lists what differs, keeping the local copy. Settings, "Your data", on the desktop and the phone; Android opens a shared .grouplab file straight into it. Round trip desktop to phone to desktop identical; 300 sessions in about a second; a newer or damaged file refused in plain words.

From planning, 2026-09-30, for Alan: "make the ability to export all of your data from any of the apps into a file that any of the
applications can read and import ... as a stop gap" until cloud backup.

1. **Settings, "Export all my data"**, on the desktop, Android and iOS: one file with every session (with its pictures and marks),
   rifles, barrels, loads and equipment, printers and their checks, designed sheets, and settings. One format for every platform, with a
   version number, readable by every build from this one on.
2. **"Import data"** reads that file on any platform. It merges: nothing already there is overwritten or duplicated (match by each
   item's id); conflicts are listed before anything is written; the import can be cancelled.
3. **Saving and opening** use each platform's own way: a save dialog on the desktop, Android's file picker and share sheet, iOS Files and
   the share sheet. On the phone, opening such a file from another app offers to import it.
4. **Privacy:** the export follows the app's existing rules for photo metadata and says so on the export screen. The file stays with
   the person; nothing is sent anywhere.
5. **Tests:** round trip desktop to phone to desktop with identical results, an old-version file, a damaged file refused with a clear
   message, and a large library (hundreds of sessions with pictures) within reasonable time and memory.
6. Guide section, site feature line, README line (rule c). Mention in entry 305's download text that Dev's data can be moved this way.

## 2026-09-30, entry 305: the download page recommends GroupLab Dev for Android testing, and mentions Play Protect

**Status: done 2026-09-30.** The download page puts GroupLab Dev first in the Android row as the recommended download: it updates itself from every nightly with no computer after the first install, installs beside the Play test copy, and sends its logs easily; a nightly can break something, said plainly. The plain APK says it does not update itself. A panel explains Google Play Protect's scan. The README and the user guide say the same.

From planning, 2026-09-30, for Alan.

1. **On the download page (and the Android part of the README and guide), GroupLab Dev gets a "Recommended download" badge** with a short
   explanation, until GroupLab is published on the Play Store: it updates itself from every nightly (after the first install, no computer
   or adb needed); it installs beside the Play test copy without replacing it; its logs are easy to send with a problem report; and fixes
   reach testers the same day. One honest line too: a nightly can occasionally break something, and Dev's data stays in Dev unless exported
   (entry 307).
2. **Google Play Protect:** say that Android asks to scan an app installed from outside the Play Store, on the first install and on updates,
   that this is Google's own check and takes a few seconds, and that the scan is expected. Same line in the guide's update section (entry 303).
3. Rule c: the site, README and guide say the same thing.

## 2026-09-30, entry 304: the tour page's top, as Alan sees it on his desktop

**Status: done 2026-09-30.** The tour page's "Your own targets" box lays its picture full width under the words, each theme's picture linking to its full size; the top bar stays on one line from about 1500 px, and between 861 and 1499 px Community, Release notes, Support and GitHub move into a "More" menu; the tour index title has the same top space as other pages. Checked in headless Chrome at 1280, 1920 and 3840 px in both themes; phone width not checkable there, and unchanged.

From planning, 2026-09-29, for Alan. Alan sent a screenshot of grouplab.org/tour/ in dark mode, about 1800 px wide (his monitor is a 48 inch
3840 by 2160 OLED). Planning sees these problems; Alan may add more.

1. **The "Your own targets" box is mostly empty.** The label and paragraph sit at the bottom left, with a large blank area above them, and
   the box is far taller than its content. Align the text to the top (or center it against the picture), and size the box to its content.
2. **The picture in that box is tiny and unreadable** (about 450 px wide showing a whole desktop window). Make it large enough to read,
   sharp at 2x, and let it open full size on click. Tie in entry 300 section 5 (sharp images on the site).
3. **The top bar wraps**: "Send a target" and "Release notes" each break onto two lines at this width, so the bar is uneven. Keep every
   item on one line (no wrapping in nav labels; tighten spacing, or move items into a "More" menu before they would wrap), checked from
   1280 to 3840 px wide and on a phone.
4. **The page title sits hard under the top bar** with almost no space above it. Give it the same top spacing as the other pages.
5. Check the other tour pages and the home page for the same four problems, and fix them the same way. Screenshot each at 1280, 1920 and
   3840 wide, dark and light, before and after, and list them in the fold.

Order: after entry 302, alongside the site work in entry 300.

## 2026-09-30, entry 300: the sheet preview drawn live and sharp, not a compressed picture

**Status: done 2026-09-30.** The sheet preview is drawn live from the same vectors the PDF is written from (the Scene), sharp at any zoom, on the desktop's Targets and the phone's sheet page, with "Open as PDF" beside the zoom; drawn against the PDF's own rasterizer it differs by under 4 gray levels on rings, C bulls and a zero grid. The site's desktop screenshots and sheet pictures are lossless WebP, and the sheet close-ups SVG.

From planning, 2026-09-29, for Alan. Alan asked whether the Targets screen and the designer can show the sheet as a live PDF in the right
pane: "it may also be easier to just display a realtime PDF instead of maintaining screenshots that are also heavily compressed and don't
look good." His monitor is a 48 inch LG C1 OLED at 3840 by 2160, where a soft or compressed image looks especially bad.

1. **Goal:** the preview is exactly what will print, redrawn the moment any setting changes (sheet, paper, bull color from entry 297,
   every designer field), and sharp at any zoom on a 4K screen.
2. **Recommended way, to be confirmed by measurement:** draw the preview from the same scene the PDF is written from, as vectors (or
   rasterized at the screen's true pixel density and re-rasterized on zoom), rather than showing a fixed-resolution bitmap. That keeps it
   identical to the PDF without embedding a PDF viewer on four platforms. If an embedded PDF renderer (for example PDFium) turns out simpler,
   say what it costs in download size and platforms, and ask planning before adding it.
3. **"Open as PDF"** beside the preview opens the real PDF in the system viewer, for anyone who wants it.
4. **The designer benefits most:** every change shows at once. Keep it responsive; debounce typing if needed.
5. **The site and README images:** where a target or sheet is shown, serve it sharp at 2x or as SVG generated from the same scene, not a
   heavily compressed PNG. App screenshots for the site: lossless PNG at 2x where size allows. Say what this does to page weight.
6. Phone: the same live drawing on the Targets screen.

## 2026-09-30, entry 297: bulls in black, blue or red

**Status: done 2026-09-30, with entry 309 section 4; question 75 open, request 57 for Alan.** Bulls print in black, blue (#1F5FBF) or red (#D22630), wide bands at a 60% tint; codes, markers and the load block stay black. The color is found from the photo, not carried in the codes (a coded color would make every colored sheet a new definition): where solid ink reads light, the expected sheet is redrawn with the bulls' own levels, and black sheets read exactly as before. The scoreboard gains red and blue under four conditions, each held to black's line: found of 50, black, blue, red: clean 49, 50, 49; hard shadow 49, 50, 49; glare 45, 48, 48; dim 49, 49, 48; no false marks. "Bulls in" on the desktop's Targets and designer and the phone's sheet page. Ring bulls in color cost more ink on a color inkjet than black (question 75).

From planning, 2026-09-29, for Alan. Feedback from users: targets should not be only black and white.

1. **Every sheet (built-in and designed) offers the bull color: black (default), blue or red.** A dropdown or radio buttons beside the
   print settings on the Targets screen and in the designer, on the desktop and the phone. The choice is remembered per sheet.
2. **The preview changes the moment the color changes** (see entry 300 for the preview itself). So does the printed PDF.
3. **Only the bulls and their rings and numbers take the color.** The corner codes, markers, the title and the load block stay black, so
   registration and code reading are unchanged.
4. **Detection must not get worse.** The detector renders the expected sheet and differences it against the photo, so it must render the
   chosen color. Decide whether the color is carried in the sheet's codes or found from the photo, and say which in the fold. Add red and blue
   synthetic sheets to the scoreboard (shadow, glare, poor light) and to the gates; a color that fails the gates is not offered. Check a
   red sheet photographed under the phone's torch and under warm indoor light.
5. **The hues are provisional until Alan approves them.** Choose a blue and a red that stay distinct from a bullet hole in grayscale and
   print well on an ordinary inkjet and on a black-and-white printer (where they print as gray). Planning will show them to Alan as swatches.
6. Guide, site and README updated to match (rule c). No new built-in sheet names: the color is an option, not a new target.

## 7. The hues and ink, decided by planning (added 2026-09-30)

Alan: colors should be "high contrast and easy for the human eye to see", and should "not waste ink in a printer if at all possible",
leaving the call to planning. Decided:

1. **Lines, rings and numbers in the full color.** Start from a strong red near #D22630 and a strong blue near #1F5FBF; tune for the gates.
2. **Large solid areas (the C bull's diamond, filled centers) print as a lighter tint of the color, about 60%, not solid.** That saves ink
   on the areas that use most of it, and a bullet hole shows dark against a tint where it hides in solid black, so the shooter sees hits
   through a spotting scope and the detector has more contrast. Check the tint still reads clearly as an aiming point at 100 yards.
3. **No thicker lines or bigger fills than the black version.** Color never costs more ink than black does in the same style.
4. Report in the fold the ink coverage of each style in each color against black, and the hole contrast each gives.

## 2026-09-30, entry 309: Alan's answers on the four designs (home, Compare, moving a hole, bull colors)

**Status: done 2026-09-30 but section 4 (bull colors), which goes with entry 297.** 1: Home A on the Capture page: the brand lockup, one line, the caliber and distance with Change, Take a picture, Choose a photo and Print a target, Getting started (the guide's phone section), grouplab.org and the version; the first caliber asked once over the page. 2: Compare draws each load's group side by side at one scale in its own color and marker, with the mean radius dashed, a tap stacking them with per-load toggles, on both platforms. 3: Fix holes circles each hole at the caliber, Move drags the circle under the crosshair with a ghost, a line and the distance; the desktop's rings are the caliber once set.

From planning, 2026-09-30, for Alan. The canvas is "GroupLab waiting designs" (https://claude.ai/artifact/SAoLbcEdBhN3wS6zifZDZ3);
a local copy of every board is in `C:\Dev\grouplab-local\design-concepts\waiting-2026-09-30\` (never committed). The numbers on the
boards are sample data.

## 1. The phone's first screen is the Capture page, redrawn as Home A (replaces entry 301's "design first")

Alan: "Lets use A as the capture page and make it the default landing page." Board `Main.dc.html`.

1. The app opens on Capture, and Capture is Home A: the GroupLab mark and word, one line saying what GroupLab does, then **one row
   showing the caliber and distance** (remembered from the last target, "Change" opens them), then **Take a picture** (primary, full
   width), **Choose a photo** and **Print a target** side by side, then a **"Getting started on your phone"** card, then grouplab.org and
   the version at the foot. The tab bar stays, with Capture selected. No sixth tab.
2. **Caliber and distance** (Alan asked whether it asks after the buttons): it does not ask every time. They show on the page as they are
   today, remembered from the last target. Only when no caliber is set yet (first use) does Take a picture or Choose a photo ask for it
   first, in a sheet over the page, then go straight on to the camera or picker. Distance may stay "Not known". Both remain changeable on the
   result, as now.
3. "Getting started on your phone" opens the phone section of the user guide (today grouplab.org/guides/user-guide/#13-on-the-phone) unless a
   dedicated mobile getting-started page exists; report which in the fold.
4. iOS the same. OLED black idle still applies.

## 2. Compare: side by side, and a tap stacks them

Alan: "A but can you make it where if you tap on one of the groups, it stacks them?" Boards `CompareA.dc.html` and `CompareB.dc.html`.

1. The card "Each load's group": one small plot per load at the same scale, each load in its own color (amber, then a new blue near
   #6ea8ec in the dark theme, matched for the light theme; more loads take further colors that differ in lightness as well as hue), shots as
   dots, the mean radius as a dashed ring, each group centered on its own middle, a shared scale bar, and shots and mean radius beneath.
2. **Tapping any group stacks them**: the card switches to one larger plot with every group on the same center (first load filled dots,
   the second rings, and so on), with a button per load to hide or show it. Tapping the stacked plot puts them side by side again. The
   choice holds while the page is open.
3. The range chart and the verdict card below use the same load colors. The desktop's Compare gets the same card and the same tap.

## 3. Moving a hole: circles the size of the bullet, and the crosshair moves with the circle

Alan: "Move: A. Moving the impact should move the circle and the circle should match the caliber diameter. It is fine to move the
crosshair with it." Boards `MoveA1.dc.html` and `MoveA2.dc.html`.

1. **Every hole's circle in Fix holes is drawn at the caliber's diameter** at the current zoom (a 6 ARC hole's circle is 0.243 in across),
   so a correct circle sits on the edge of the hole. Where the caliber is not known, the present size, and a line saying the caliber makes
   the circles true size.
2. Step 1 is as built: pan the picture under the fixed crosshair; a hole under it is chosen; Add, Move this hole, Remove, Undo, Done.
3. **Step 2, moving:** the chosen circle is dragged directly with a finger (not by panning the picture), and the crosshair travels with the
   circle's center. A dashed ghost stays where the circle was, with a dashed line to it; a readout gives how far it has moved in the person's
   units; the picture scrolls by itself when the circle nears an edge. "Put the hole here" drops it; "Cancel" puts it back. Undo covers it.
4. The desktop's marking screen: circles at the caliber's diameter too, and dragging a hole moves its circle the same way.

## 4. Bull colors approved (entry 297 section 7 stands)

Alan: "Colors: Approved." Red near #D22630 and blue near #1F5FBF for rings and lines, large solid areas at a 60% tint, codes black.
Tune only if the detection gates require it, and say so.

## Order

Within entry 303's list for the desktop and Android worker: section 3 (moving a hole) and section 2 (Compare) after 302 and 299; section 1
(the Capture page) alongside, on Android and iOS; section 4 with entry 297.

## 2026-09-30, entry 301: a home screen on the phone, design first

**Status: superseded 2026-09-30 by entry 309 section 1, done.** Home A is the Capture page and first screen (built with entry 309). There is no separate mobile getting-started page; the link is https://grouplab.org/guides/user-guide/#13-on-the-phone.

**Superseded 2026-09-30 by entry 309 section 1:** Alan chose Home A as the Capture page and the first screen. Build it from entry 309.

From planning, 2026-09-29, for Alan. When the phone app opens, show a home screen instead of going straight to Targets: a graphic with
the GroupLab logo, basic information such as grouplab.org, and a link to the mobile getting-started guide.

1. **Planning is drawing options for Alan now. Do not build the screen itself until he approves one.**
2. Meanwhile, if it is cheap: prepare the navigation so a Home screen can be the first screen on Android and iOS, reachable again from the
   tab bar or the logo, without changing what users see yet.
3. Check where the mobile getting-started guide lives on grouplab.org and report its URL in the fold.

## 2026-09-30, entry 298: every pane can be resized and is remembered

**Status: done 2026-09-30.** Every multi-pane screen has a grip to drag (amber on hover), each split remembered: the editor, analysis, library, Targets, Equipment and Ballistics on the desktop, and the result's sheet and numbers side by side on a wide phone or tablet; Settings has Reset layout on both. Tested headlessly at 3840 by 2160 and at 150% scaling; a real 4K monitor is not checked. The pictures follow at the next walk.

From planning, 2026-09-29, for Alan.

1. **On the desktop, every split between panes inside GroupLab can be dragged** (Targets list, print panel and preview; the designer;
   Analyze; Sessions; Compare; Ballistics; Settings; any other two- or three-pane screen), with a visible grip and a sensible least width.
2. **Each size is remembered** between runs, per screen, and survives a window resize sensibly (proportions, not raw pixels, where that
   reads better). The Targets list width is already remembered; follow that pattern.
3. **Settings gets "Reset layout"**, which puts every pane back to its default size at once.
4. On the phone, where a screen has two panes side by side (tablet, the Fold's inner screen), do the same if it is cheap; otherwise say so.
5. Check at 3840 by 2160 with 100% and 150% Windows scaling: Alan's monitor is a 48 inch 3840 by 2160 OLED at 120 Hz.

## 2026-09-30, entry 299: Settings, the privacy and feedback text folded away

**Status: done 2026-09-30.** Settings' Sending targets, Error reports and Hardware survey each show their choice and one short line; the rest folds under a remembered "More", on both platforms, with "What GroupLab sends" always in view. The Settings pictures and guide PDF follow at the next walk.

From planning, 2026-09-29, for Alan: "It is very wordy and takes up too much space."

1. Under the privacy options and the feedback options in Settings, show the switch and one short line each. The full explanation goes
   behind a "More" (or chevron) expander, closed by default, remembered once opened.
2. Nothing is removed: every word that is there today is still one tap away. Links to "What GroupLab sends" stay visible.
3. Desktop and phone alike. Screenshots and the guide follow at the next nightly's refresh (entry 296 section 2.4).

## 2026-09-30, entry 302: the torch should dim, or go off, when the page is too bright

**Status: done 2026-09-30, the iOS part (item 3) included (c58bb4cd); the torch itself waits for the iPad sitting.** A shared `TorchGovernor` starts at the lowest level,
steps up only while the page is dim, and steps down or off on glare (over 2% clipped), a hot spot, or a paper median of 235 or more; four
agreeing frames and 1.5 s between changes; the Android camera sets CameraX's torch strength where the phone offers levels, and on or off
elsewhere. Whether the strength changes mid-session on the Fold 7 is for the next sitting's log.

From planning, 2026-09-29, for Alan: "It seems like once it gets bright, it doesn't get dimmer. Will it dim if it determines it is too bright?"

Today (`CameraView.cs`, torch on Auto): the torch turns on when the paper is dim or the light uneven, at full strength, and then stays on
for the rest of the session so it does not flicker. It never dims and never turns off.

1. **Turn it down or off when it hurts:** a hotspot or clipped highlights on the paper (glare from a point light next to the lens), or the
   page already bright without it. Use hysteresis and a minimum time between changes so it never flickers.
2. **Use strength levels where the phone supports them** (the Fold 7 should): start low, step up only if the frame is still too dim, step
   down on glare. Where levels are not supported, on and off only. Entry 262's torch-strength work is the starting point.
3. **iOS:** the same logic with the iPhone's torch levels.
4. Log each change (`camera.torch` with the reason) so the next sitting shows what it did, and add a line to the sitting's checklist.

## 2026-09-29, entry 296: the iOS push goes on, with a leaner way of spending

**Status: in force until 2026-10-01 02:00 UTC; section 1 done 2026-09-29.** The uncommitted work was committed (the updater's
remembered tap, entry 294's fold, the screenshots, PDFs and claims regenerated once); entries 291, 293, 294 and 295 and the iOS app,
camera and nightly job reached main as d05e112. Ten merged worktrees removed, none with uncommitted work; `ios/photos` kept (unfinished).
Alan answered questions 71 to 74: A to each.

From planning, 2026-09-29, for Alan. This changes how entry 290 is carried out, not what it asks for. The deadline, the priority on iOS and
"never idle" all stand (Wednesday 2026-09-30 20:00 Mountain, 2026-10-01 02:00 UTC).

## Why

The session stopped at about 13:35 UTC with 55 files changed and not committed, most likely at a usage limit. Seven hours were lost. Alan
wants the work to stay parallel and fast, but without spending on things that do not move the app forward.

## 1. Before anything else

1. Commit the uncommitted work as it stands: the self-updater fix (`SelfUpdate.cs`, `InstallResultReceiver.cs`), the notes and for-alan
   line. Check the regenerated screenshots, mosaic and guide PDF first; commit them only if they reflect a real change, otherwise restore them.
2. Note for Code: planning found a stale empty `.git/index.lock` at 20:58 UTC (planning's own status check caused it) and renamed it to
   `.git/index.lock.stale-planning-20260929`. Nothing else in `.git` was touched.
3. Prune worktrees under `.claude/worktrees/` (13 now) whose branches are merged. Keep a backup of anything unmerged.

## 2. The spending rules until the deadline

1. **At most three workers at once**, each on a separate area (for example: iOS, entry 295, entry 293). Not a dozen.
2. **Mechanical work goes to a smaller model.** Screenshot and PDF regeneration, doc passes, searches, renaming, the site's consistency
   audit: Sonnet or Haiku. Design, detection, the camera and the iOS platform code: the main model.
3. **Narrow worker prompts.** Give each worker the files and the goal. Workers do not re-read the whole repository, STATE, or the long
   notes files; they read the section they need.
4. **Screenshots, the README mosaic, the guide PDF and the claims backing are regenerated once per nightly**, at the end, not after each entry.
5. **Tests where the change is.** Run the tests for the area changed locally; let CI run the full suite and the iOS simulator. When waiting on
   CI, sleep in long steps (five minutes or more), do not poll every minute, and do other work meanwhile.
6. **Commit small and often**, at least after every finished item, so a stop never strands work again.
7. **Short reports.** One line per item in for-alan.md and panel.md. No rewrites of long documents unless the entry asks for it.
8. **No scoreboard re-reads of every sitting** on each change; run the scoreboard once per nightly, or when detection code changes.

## 3. The order

1. Entry 295 (Compare loads, the verdict card, All figures, sessions named by sheet name). Alan sees these on his phone now.
2. Entry 290 iOS, items 3 (joining the nightly), 6 (files, sharing, printing, the idle screen), then the self-test. The TestFlight job waits
   only on Alan's Apple secrets; build everything around it.
3. Entry 293, the draft MSIX and the read-only authentication check. No submission.
4. Entry 294, the guide and site pass.
5. The nightly, then one screenshot, guide and site refresh.

## 4. If a limit is hit again

Write one line to for-alan.md with the time and what was in progress, commit, and stop cleanly. On restart, read that line first.

## 5. What Alan's usage log shows (added 23:10 UTC)

Alan ran ccusage. Almost all of the tokens since 12 September are cache reads (9.74 billion of 9.83 billion): the conversation being sent
again on every step. Output is under 0.2%. On 29 September (Mountain time), in about seven and a half hours before the stop, cache creation
was 27.4 million tokens, five times any earlier full day, which is the mark of many fresh workers each loading their context. So:

1. The cost is context size times the number of steps. Keep contexts small (compact the main session after each finished item; workers
   read only what they need) and steps few (no minute-by-minute polling, no rebuild-and-look loops when a test would answer).
2. Start a new worker only for a separate area of work, and reuse it for follow-ups in that area rather than starting another.

## 2026-09-29, entry 295: Alan on nightly 126 on the Fold 7: Compare loads draws badly, All figures squeezes its labels, and the result picture is still sideways

**Status: done 2026-09-29 but for section 4 and part of 1.3.** 1: the interval chart (desktop and phone) puts each name on its own
line with the range and value beneath, is as tall as its rows, and its sentence comes from Core (`LoadComparison.ChartSays`), so it cannot
disagree with the verdict (held over 288 comparisons); extreme spread says it has no range and points to mean radius; the sigma sentence
is in plain words. 1.3, each load's group drawn: DESIGN NEEDED, since entry 259's Compare design draws no groups. 2: All figures moves a
value and its note under the label when they do not fit beside it (tested at 320 wide, text size 28). 3: `SessionNames` (load, then date,
then time as needed, sheet beneath) on both platforms. 4: the result picture is entry 291 section 2's, which is being built. 5: guide,
PDFs, PHONE-PARITY, tour and the eight Compare screenshots.

On the Android line, first in it after the section of entry 291 already under way (alongside entry 290's iOS work). Five screenshots, local only, metadata stripped: `C:\Dev\grouplab-local\camera-0929\screenshots-alan-3\compare-1.jpg` to `compare-5.jpg`.

Alan: "the compare loads is not rendering correctly. I thought there was supposed to be two groups shown and the graphs are also covering the text. When looking at a single session, [some of] the text is strange, the image is still horizontal, and there is still a lot of blank space."

### 1. Compare loads (compare-1 to compare-3)

1. **The chart's labels and bars overlap.** Each load's name is drawn in the same row as its interval bar and value, so the bar, the dot and "0.72 MOA" sit on top of "GroupLab 5x5 Load Development with Load Block, Letter". Put each load's name on its own line above its bar (or as a short label, section 3), with the bar and value on the line below, across the full width. Nothing may overlap at any font size or phone width; a test that no two text boxes intersect.
2. **The chart box has a large empty area** under the two rows. Size it to its rows.
3. **"Two groups shown":** the approved phone design for Compare (phone parity canvas, row of entry 259) shows each load's group itself, not only the interval chart. Check the board: if the design has the two groups drawn (side by side or overlaid in two colors), build that above the chart. If it does not, post a DESIGN NEEDED line and planning draws it.
4. **Extreme spread contradicts itself.** On compare-3, extreme spread draws dots with no range, says "Some of these do not overlap, so there is a difference these shots can see", and the card below says "These two loads are not distinguishable on this evidence." Extreme spread has no interval here, so it must not claim a difference: say that extreme spread has no range to compare, and point to mean radius. The chart's sentence and the verdict card must never disagree on any figure (a test).
5. **"The sigma intervals overlap"** in the verdict card is jargon on a phone: plain words, with sigma kept behind the dotted underline as elsewhere.

### 2. A session's All figures (compare-4)

The label column is squeezed to one or two letters wide when the value is long: "Zero, elevation" is drawn one letter per line down the screen. When a value will not fit beside its label, put the value (and its note, "too small to dial yet; about 44 shots would settle it") on the line under the label, full width. Labels never wrap mid-word. A test at the narrowest phone width.

Also: those zero lines read "0.1 MOA high". Entry 294 makes aiming figures follow the scope's unit; this screen is one of them.

### 3. Names in Compare and Sessions

Both loads are named "GroupLab 5x5 Load Development with Load Block, Letter", the sheet's name, so they cannot be told apart (one has ", 2026-09-29" added). Name a session by what distinguishes it: the load or the label the person gave it, then the date and time, with the sheet's name secondary. Where two sessions would still read the same, add the time.

### 4. The result picture (compare-5)

Still sideways, still an empty area below, and the Move, Add and Remove buttons still on the result page: expected, since entry 291 section 2 (upright picture, no empty area, holes edited on their own Marking A page) is not built yet. This screenshot is its evidence too; it stays next in entry 291 once its current section is done.

### 5. Done when

Built, tested, in the next nightly, and on the Fold 7 (by the new updater of entry 288 where it works). Say in `for-alan.md` what to look at.

## 2026-09-29, entry 294: a mil shooter should never feel like an afterthought

**Status: done 2026-09-29 but section 3, which is Alan's question through planning (no sheet changed).** 1: both platforms' first run asks
"Is your scope in mil or MOA?" (Mil, MOA, "Both, I have rifles of each") with inches or millimeters beside it, and asks existing installs
once; the angle is no longer guessed from the region; `UnitSettings.Aiming(rifle)` makes a session's rifle's scope unit win for every
aiming figure (the zero block, Zero from this group, Shots Needed to Zero, the dope, the hit chance, the reports), MOA a tap away; Settings
leads Units with "Scope unit"; clicks offered as 0.1 mil, 0.05 mil, 1/4 MOA, 1/8 MOA or typed. Not done: the typed inputs of the desktop's
hit chance view stay in Settings' unit so a typed value keeps its meaning. 2: a "Mil or MOA" guide section; neutral or paired wording on the
tour, Features, home page, README and research; a Features entry "Works in your scope's unit" with its own picture; the screenshot walk's
rifle is a 0.1 mil scope. 4: a Discord reply line in for-alan.md.

On the reloading Discord, a shooter wrote to Alan (quoted as posted): "it looks like from the descriptions you are doing everything in moa and just supplying mil as an after thought both in the pics and the text. I didnt see anything in the user guide talking about setting a user preference to pick if you are using a moa or mil scope and then making the interface function in the selected mode. If i was using it to zero my scopes I need mil as the only scopes i have in moa are my hunting rifle, the competition ones are in mil as are most peoples i would bet."

**What exists already, and why he missed it:** Settings has an angle unit (`docs/USER-GUIDE.md` section 12, "Units: length, angle and distance, each chosen on its own"); each figure can be tapped to switch and is remembered (entry 280); a rifle carries a click value. But the angle unit is defaulted from the system's region, so almost every American starts in MOA; the first run never asks; the guide mentions it in one line; the zero correction shows "MOA and mil side by side" rather than in the scope's own unit; and the site's pictures and wording lead with MOA. He is right about how it looks. On the Android and desktop line, alongside entry 290's iOS work:

### 1. The scope's unit is a first-class choice

1. **The first run asks**, on both platforms, as one of its first questions: "Is your scope in mil or MOA?" with a third answer, "Both, I have rifles of each", and the length unit (inches or millimeters) beside it. No region guess for the angle unit; the region may still preselect the length unit.
2. **Each rifle has its scope's unit and click value** (0.1 mil, 0.05 mil, 1/4 MOA, 1/8 MOA, and any other). Where a session names a rifle, **that rifle's unit wins** for everything aiming: the zero correction, the clicks, Shots Needed to Zero, the dope table, hit chance, the per-shot table, "Zero from this group", Ballistics. Where no rifle is named, the Settings choice decides. The "Both" answer means GroupLab asks which rifle, or uses the rifle's own unit.
3. **In mil mode, the aiming figures show mil only**: the zero correction reads "0.3 mil left, 0.2 mil up, 3 and 2 clicks" with no MOA beside it. MOA stays one tap or press-and-hold away, as now. Group size figures follow the same angle unit.
4. **Settings says it plainly** at the top of Units: "Scope unit: mil / MOA", with one line saying what it changes.
5. Tests: a session in mil mode shows no MOA text in any aiming figure unless the person asked for it; a rifle's unit overrides Settings; the first run's answer is kept.

### 2. The words and the pictures

1. **User guide:** a short section near the start, "Mil or MOA", saying how to choose, that each rifle keeps its own, and that everything aiming follows it.
2. **The site, the README, the Features page and the tour:** audit every figure and sentence for MOA-first wording, and make them neutral or paired ("mil or MOA, your choice"). Where a screenshot shows angles, show mil in at least half of them; the zero screenshots should show a mil scope. The Features page gets an entry "Works in your scope's unit", with its own picture (entry 256).
3. The printed sheets already come in both (the zeroing grids in 0.2 mil and 0.5 MOA squares); say so where the sheets are listed.

### 3. A question for Alan, through planning, not blocking the rest

The load development bulls are sized in inches (1.00 in, the "1 MOA" family; 2.00 in, "2 MOA", entry 289). Whether mil shooters want bulls named or sized in mil (for example 0.3 mil at 100 m, or labels giving each bull's size in both) is Alan's choice; planning will ask him. Do not change the sheets for this entry.

### 4. Reporting

Say in `for-alan.md` when it is in a nightly, with a one-line reply Alan can give the shooter on the Discord.

## 2026-09-29, entry 293: request 38, the Microsoft Store: Part A is done

**Status: done 2026-09-29.** 1: the draft `store-draft-0.2.0` (`grouplab-win-x64.msix`, 92,035,133 bytes, version 0.2.0.0, SHA-256
`34c6db9f...ef999b`, no tag made), built by `release.yml` with the new `store_draft` input; the manifest's identity matches the four
variables (`scripts/check-msix.ps1`, on every build); the certification kit ran: WARNING, 24 tests, only the optional "blocked
executables" failing, and a DPI-awareness warning for later. 2: the Store login (`scripts/store-login-check.ps1`) answered 200 with the
product "GroupLab"; nothing submitted. 3 and 4: request 38's Part A closed in for-alan.md; the secret's expiry, about 2028-09-28, is in
`docs/RELEASE-PLAN.md`, and a failed login says the Store secret may have expired.

Alan finished Part A of request 38 with planning, step by step, on 2026-09-29.

**Set, and checked with `gh variable list` and `gh secret list`:**
- Variables: `STORE_IDENTITY_NAME`, `STORE_PUBLISHER` (it matches the Windows publisher ID on Partner Center's Identifiers page), `STORE_PUBLISHER_DISPLAY_NAME`, `STORE_PRODUCT_ID`. The name reserved is `GroupLab`.
- Secrets: `AZURE_AD_TENANT_ID`, `AZURE_AD_APPLICATION_CLIENT_ID`, `AZURE_AD_APPLICATION_SECRET`, `SELLER_ID`.

**How it is set up:** a new Microsoft Entra tenant made for GroupLab alone (`GroupLab.onmicrosoft.com`), separate from any work tenant, associated with Alan's Partner Center account; an app registration `grouplab-store-publisher` in it, single tenant, with a client secret that expires in 24 months; the app added in Partner Center under Microsoft Entra applications with the Manager (Windows) role. The client secret was made again after the first one was exposed in a chat, and Alan has deleted the first in Entra (confirmed 2026-09-29); neither session has seen the current one. **Note in `docs/store/` or `docs/RELEASE-PLAN.md` that the client secret expires about 2028-09-28**, so a reminder to renew it lands well before, and a release that fails to authenticate says "the Store secret may have expired" in its error.

**What Code does now** (Part A's "I then build the first package with your identity as a draft release"), on the Android and desktop line alongside entry 290's iOS work:
1. Build the Store package with this identity (`grouplab-win-x64.msix`) as a **draft** GitHub release. Validate it with the Windows App Certification Kit if the runner has it, and check the manifest's identity against the four variables.
2. Check that `release.yml` can authenticate to the Store with the four secrets **without submitting anything** (a read-only call, such as listing the product's submissions), and report the result. Nothing is submitted until Alan does Part B by hand.
3. Tell Alan in `for-alan.md` that the draft is ready, with the link, and planning walks him through Part B (the first submission: pricing, properties, age rating, the package, the listing from `docs/store/LISTING.md` and its screenshots).
4. Close Part A of request 38 in `for-alan.md`.

## 2026-09-29, entry 291: PRIORITY on the Android line: Alan's second camera sitting (nightly 123 on the Fold 7), and the scale test's card photos

**Status: done 2026-09-29 but for the device checks at the next sitting and one DESIGN NEEDED.** Sections 1, 5 and 6, the scale test:
the printer printed true size within 0.1%; the saved card correction is about 0.4% too large, most of it the card-thickness correction
(question 74); outline B ruled out, A and C not separable on two cards; the Guided picture was page A with card 1; request 56 asks for one
scan. 5.2: a printer check carries its date, can be marked calibrated or serviced, and a stale one says so. 2: the result picture upright
from the registration, full width, no empty area; holes fixed on their own page ("Fix holes", Marking A, undo, a keep-changes question);
the selected Camera or Result button shown selected. 3.1: naming an off-square picture took 4.2 to 30.0 s on the Fold 7 and now about
1.3 s (codes read square on, placed by the nearest markers). 3.2: "Move back" only when the printing leaves the frame, "Move closer" below
2.4 px a code module. 3.3: the live frame foretells the picture's marker count (same 4:3 view, 2.27 times the pixels). 3.4: the whole
crosshair turns green. 3.5: "Hold steadier" only on measured shake (from 0.6 px; the stream's own blur, 0.3 px, is an allowance until the
next sitting measures it). 7: the scoreboard in every build and on a local corpus (169 of 173 real holes, then 172 of 173 with 4 false
marks after the joined-hole fix); GroupLab Dev keeps every picture of a sitting. DESIGN NEEDED: Move under a fixed crosshair in Fix holes.

Runs alongside entry 290's iOS work, first in the Android and desktop line (entry 290 section 1: work Alan is waiting on goes first there). Alan, after a sitting with nightly 123: "I took some target photos from odd angles and tested the camera. It is working better now." Then the faults below, in his words where they are his.

**Screenshots:** six, local only, in `C:\Dev\grouplab-local\camera-0929\screenshots-alan-2\` (metadata stripped). Never committed, never in a public log: they are the app's own screens, but the rule for this folder stands. Pull the logs of this sitting from the Fold 7 into `C:\Dev\grouplab-local\camera-0929\sitting-2\` before anything else, never committed.

### 1. The scale test (request 53): pull the card photos from the phone

Alan: "I have taken pictures of all 3 pages with 2 cards in the grouplab app." The photos are in GroupLab Dev on the Fold 7, taken through Settings, Printers, Add a printer, "A card and one photo". Pull them (and whatever the app measured from each) into `C:\Dev\grouplab-local\scale-test\` with names saying the page and the card (`A-card1`, `A-card2`, ...). **The 600 dpi scans have not arrived yet**: planning has asked Alan where the scanner saved them. Measure what can be measured now (each card photo's own reading, and how the three outlines compare with each other), and the comparison against the scans when they come. Report in `for-alan.md`.

### 2. The result screen

1. **The photo is always shown sideways, with a large empty space below it.** "The photo of the target seems to always be oriented horizontally in the app and there is a ton of blank space below the photo." Screenshot `..._011515` shows a portrait Letter sheet drawn turned 90 degrees in a landscape box, and `..._011526` a screen-high empty area below it. Show the picture upright as the sheet is (a portrait sheet portrait, from the registration, not the file's tags), filling the width, and no empty area after it. A test at each of the four ways the phone can be held.
2. **Editing holes moves to its own page.** "It gives you the option on the analysis page to move the holes. This should probably be moved to a separate page with a better interface. There is no option to zoom into the photo and accidentally touching one of the holes moves it and screws up everything."
   - The result's picture becomes view only: no touch moves a hole there.
   - A button on the result, "Fix holes" (or the wording already used for it), opens a page built on **Marking A**, the fixed-crosshair pattern Alan approved for hand marking (entries 278 and 279): pinch to zoom, drag to pan the picture under a fixed crosshair; Move, Add and Remove act on the hole under the crosshair; Undo; Done returns to the result, which recomputes.
   - A mistaken change is always undoable, and leaving the page without Done asks whether to keep the changes.
   - If the page needs a layout choice Marking A does not settle, post a DESIGN NEEDED line and planning draws it; build the rest meanwhile.
3. The **Camera and Result** buttons: in the screenshots Result looks greyed out while the result is showing. The one showing should look selected, not disabled.

### 3. The camera screen

1. **Off-axis pictures wait a long time before analysis starts.** "When taking an off-axis photo, it says it is loading the target or photo for a long time before starting analysis." Time each step of an off-axis picture on the Fold 7 (the same timing script as entry 283), find the slow step, and make it fast. Meanwhile, the waiting text names the step it is on.
2. **The distance guidance is wrong.** "The helper also kept telling me to move back until it could no longer read the target at all." And: "when I appeared to have the phone held at the correct distance according to the helper, it had worse detection than if I held it closer." "Move back" may only be said when the sheet actually runs out of the frame. "Move closer" and the right distance come from what the live frame can read (markers decoded and the QR code's module size in pixels), not from the sheet's size in the frame. The distance the guide calls good must be the distance where detection is best; measure that on the Fold 7 at several distances and show the numbers.
3. **The viewfinder still does not see what the picture sees.** "The image the viewfinder is seeing also appears to still not be what the application takes in the end because it was not detecting the same amount of tags on the viewfinder page versus the final image." Log, for each picture, the markers read in the last live frame and in the final picture, with each one's field of view and resolution. They should read the same markers, or the live count should honestly predict the final one. Fix the field-of-view or resolution difference that is left.
4. **The level:** "When the crosshair level is perfectly level, the whole crosshair should go green instead of just the dot in the center so you know when it is perfectly flat."
5. **"Hold steadier" asks for the impossible.** "The helper would say to hold the phone steadier when I was already holding my breath and doing everything humanly possible to hold still." Recalibrate the steadiness and focus check against real handheld frames from the Fold 7: judge the sharpness the analysis actually needs (the markers' edges), not a threshold a steady hand cannot meet. Say "hold steadier" only when a frame is too blurred to measure. Show the before and after thresholds with the measured blur of Alan's frames.

### 5. Alan, two more facts from the same sitting

1. **Guided mode fired by itself on one scale-test page.** "At one point, grouplab did take a picture of one of the printer scaling pages automatically, which I assume means everything was aligned as best as possible." Find that picture among the card photos pulled in section 1, say which page it was and whether a card was on it, and do not count it as a card photo if it has none. Note it as a good sign for Guided mode (every condition held), and keep its readings.
2. **Alan has since calibrated his printer.** "I just calibrated my printer and hopefully made it slightly more accurate."
   - **The scale test is unaffected** as long as the scans are of the same printed pages the card photos were taken on. Planning has told Alan not to reprint them.
   - **The saved printer correction is now probably stale.** The result in screenshot `..._011445` says "Printed 0.4% large, every size corrected". Sheets printed before the calibration are still 0.4% large; sheets printed after it may not be. Check how GroupLab applies a printer's saved correction: if it applies to every sheet from that printer regardless of when it was printed, that is now wrong for new prints. Tie a correction to the prints it was measured on (a printer check dated, and sheets printed after it using it), offer a new printer check when the printer's correction may have changed, and say in `for-alan.md` what Alan should do (most likely: run the printer check once more on a sheet printed now).

### 6. The scans have arrived

The three 600 dpi scans of the same printed pages the card photos were taken on are now in `C:\Dev\grouplab-local\scale-test\` as `scan-A.png`, `scan-B.png` and `scan-C.png` (4958 by 6458 px, 600 dpi, the scanner's text metadata stripped; each page's label checked). Scanned after the photos and before any reprint; the printer was calibrated after these pages were printed, which does not matter for this test. Run the full comparison of section 1 now: each card photo's print-scale reading against its page's scan, new card and old, A against B against C, and report which outline reads most accurately and most consistently, with the numbers.

### 7. Every picture from a sitting becomes a measured test case (Alan: "Will code look at every photo that was taken and learn from them and refine the scaling, camera, and hole detection?")

Today the answer is "only the ones an entry names", and the scoreboard of `docs/DETECTION-LEARNING-STUDY.md` (entry 261, option a) is not built. Make the answer yes, in the study's own terms: measured tuning of the classical rules, each change with its reason, no learned model.

1. **Pull everything from this sitting**, not only the card photos: every target photo (the odd angles included), each with its live-frame record and analysis trace, into a local corpus, `C:\Dev\grouplab-local\corpus\sitting-2026-09-29\`, metadata stripped (no GPS, no timestamps kept in the files). Alan's standing consent of 2026-09-24 covers his own sheets for testing; they still never go into the public repository, a public CI log or a public artifact. Real-photo runs happen locally or in the private archive repository's Actions, as the study says.
2. **Truth for each photo.** Where the same sheet was scanned (the three 25-shot load sheets of 2026-09-26, and any other), the scan's hand-checked holes are the truth for the photo: hole by hole, recall, false marks and centre error, and the registration error at every bull. Where no scan exists, the shot count Alan wrote on the sheet is the truth for the count, and the photo is marked as count-only.
3. **Build the scoreboard now** (entry 261 option a), fed by this corpus and the synthetic degradations: every build re-reads them and records recall, false marks, centre error, registration error and time per picture, by condition (angle, distance, light, blur). A drop beyond a set margin fails loudly. Every future sitting's pictures are added the same way.
4. **Use it for this entry's fixes:** the distance guidance (section 3.2), the steadiness threshold (3.5) and the live-versus-final tag counts (3.3) are tuned against these real frames, and the scale test (section 1) against the scans. Each tuned number is written down with the pictures that set it.
5. **So nothing is lost next time:** GroupLab Dev (only Dev, never the release or Play builds) keeps every picture it takes, with its live-frame record and analysis trace, on the phone in its own folder, so Code can pull a whole sitting at once. Nothing is sent anywhere; a setting in Dev turns it off and clears it; a line in `for-alan.md` says it is on.
6. **Report** the scoreboard's first full table in `for-alan.md` in plain words: how many holes were found on the real photos, how many false marks, how far off, and what changed because of them.

### 4. Done when

Each item above tested where a test can hold it, shipped in the next nightly, and installed on the Fold 7 (by adb until entry 288's updater exists). Say in `for-alan.md` what to try at the next sitting. Then back to the iOS work of entry 290, which runs alongside all of this.

## 2026-09-29, entry 292: pictures from anywhere on the phone: Google Photos, Samsung Gallery, the maker's own photo app, and the cloud, on Android and on iOS

**Status: in progress 2026-09-29. Sections 1, 3 and 4 (Android) done; section 2 (iOS: PHPicker with iCloud Photos, the document picker, a
Share Extension and "Open in GroupLab") not done yet**, part of entry 290's iOS work once the iOS head is merged. Android: the system photo
picker first (no storage permission), "From another app" (every app answering image/* by name), SEND, SEND_MULTIPLE, VIEW and EDIT
received (several become a set), cloud photos fetched with progress naming the app and a Cancel, offline said as offline, a reduced copy
detected and said. No Google Play services dependency but the picker's Android 10 backport; the update check runs at launch too. Only
Samsung has been tried by a person; the other brands by the standard intents.

Alan, from the same sitting: "GroupLab on Android does not have access to Google photos. It seems to only have access to drive and images. Make sure iOS version has access to it's version of photos as well as Google photos if it is installed. It is important that it can access cloud stored photos and not just ones local to the phone." And: "It should also have access to any photo applications that you would normally see in a photo editor like samsung photos or applications that other phone manufacturers include in place of a product like google photos on a pixel phone."

The rule: **a person can reach any photograph they can see in their phone's photo apps, whichever app keeps it and whether it is on the phone or only in the cloud**, the way a photo editor can. Android goes first in the Android line (after entry 291); the iOS half joins entry 290's iOS work.

### 1. Android

Today's "choose a photograph" reaches only the documents picker (Drive and Images). Offer all of these:

1. **The system photo picker** (`PickVisualMedia`, images only) as the first choice. On devices where Google Photos is the cloud media provider it shows cloud-only photos too; it needs no storage permission.
2. **"From another app"**: an `ACTION_GET_CONTENT` chooser for `image/*`, so every app that offers pictures appears by name: Google Photos, Samsung Gallery, the maker's own gallery on other phones (Xiaomi, OnePlus, Motorola and so on), Drive, OneDrive, Dropbox, Files. This is how a photo editor reaches them, and it is the path that lists Samsung Gallery and Google Photos by name on the Fold 7.
3. **Receiving a picture from any app:** GroupLab (and GroupLab Dev) appear in the share sheet and in "Open with" for images: intent filters for `ACTION_SEND` and `ACTION_SEND_MULTIPLE` with `image/*`, and `ACTION_VIEW` (and `ACTION_EDIT` if it fits) with `image/*`. A shared or opened picture goes straight to analysis, as if taken in GroupLab. Several pictures shared at once become a set, one per sheet.
4. **Cloud-only photos:** a picture that is not on the phone is downloaded through its `content://` stream with a progress line ("Getting the photo from Google Photos"), cancelable, and a clear message if the phone is offline. **Always the full original**, never a thumbnail or a reduced copy: check the resolution received against what the provider reports, and if an app hands back a reduced copy, say so and suggest another way (the result's picture check already scores resolution).
5. **Orientation and metadata:** as for camera pictures, the picture is stood upright from its own data, and location metadata is never read, logged or kept.
6. **Tests and the sitting:** unit tests for each entry path; on the Fold 7, one picture each from Google Photos (a cloud-only one), Samsung Gallery, Drive, and one shared from Google Photos and from Samsung Gallery. Say in `for-alan.md` what to try.

### 2. iOS (part of entry 290's parity work)

1. **Apple Photos, including iCloud Photos:** `PHPickerViewController`, images only, needing no photo library permission. Photos kept only in iCloud download on demand through the item provider, with progress, and always at full resolution (request the original file, not a display copy); HEIC is decoded as the pipeline's S0 already specifies.
2. **Files:** the document picker (`UIDocumentPickerViewController`) for `public.image`, which reaches iCloud Drive, Google Drive, OneDrive, Dropbox and any other Files provider installed.
3. **Google Photos and other photo apps:** iOS has no system way for one app to browse another's library, so Google Photos reaches GroupLab by **sharing**: a **Share Extension** so GroupLab appears in the share sheet of Google Photos, Photos and every other app, plus document types so "Open in GroupLab" works. The shared picture opens straight into analysis. Say plainly on the Features page that on iPhone and iPad a Google Photos picture is shared into GroupLab.
4. The same full-original, progress, offline and metadata rules as Android.

### 4. Phones sold outside America (Alan: "This may be especially important for europeans that have access to a lot of phones that are not available in america like chinese brands like oppo, redmi, Xiaomi, vivo, honor, redmagic, etc")

Europe and South America buy far more of these than the US (the device study, `claude/mobile-device-study.md`, has Xiaomi, Redmi and Motorola among Latin America's best sellers). Treat them as first-class:

1. **Their own galleries must appear by name** in "From another app" and must be able to share into GroupLab: Xiaomi and Redmi Gallery (HyperOS, MIUI), OPPO and OnePlus Photos (ColorOS, OxygenOS), realme, vivo Albums (OriginOS, Funtouch), Honor Gallery (MagicOS), Nubia and RedMagic, Motorola, and Huawei Gallery. The standard intents of section 1 are what these apps answer; nothing brand-specific is written unless a test shows a brand needs it.
2. **Their clouds** (Xiaomi Cloud, HeyTap and OPPO Cloud, vivo Cloud, Honor Cloud, Huawei Cloud): a photo the gallery shows but keeps only in the cloud arrives through the gallery's `content://` stream; the download, progress, offline and full-original rules of section 1 item 4 apply unchanged.
3. **Phones without Google Play services** (Huawei, and some phones sold with Chinese-market software): the system photo picker may be missing or limited there, so when it is not available GroupLab goes straight to the `ACTION_GET_CONTENT` chooser, never to an error. Check that nothing else GroupLab needs (the camera, OpenCV, SQLite, the updater of entry 288) depends on Play services, and list anything that does.
4. **Aggressive battery managers** on these phones stop background work: entry 288's six-hourly update check must be checked on launch as well, and must not rely on background work alone. Say so in the user guide for these brands (how to allow GroupLab to run in the background, per brand, in one line each).
5. **Testing without the phones:** the Android emulator for the no-Play-services path, the device survey (docs/SURVEY.md) to learn which of these brands people actually use, and a short "tried it on your phone?" note for testers on the Discord. Record in `PHONE-PARITY.md` which brands have been tried by a real person and which only by the standard intents.

### 3. In the same change (rule c)

The user guide, the Features page, `PHONE-PARITY.md` (both columns), and `what-grouplab-sends` if anything about permissions changes (the photo picker needs no storage permission; say so).

## 2026-09-29, entry 289: the 2 MOA sheets: Alan chose C with 2.00 in bulls

**Status: done 2026-09-29, with three parts the format cannot carry, raised as questions 71 to 73.** Twelve sheets: `GL-CF9-LTR` and
`GL-CF9-A4` (one page), `GL-CF9-T` and `GL-CF9-TA4` (the set of three, a 3x1 tiled set pooled by tile index), each plain, C and E, nine
2.00 in bulls 3 by 3, described as 1.91 MOA at 100 yd and 1.75 at 100 m, credited to Unholy. Not done as written: four corner codes
(two, question 71), numbering 1 to 25 with S1 and S2 (1 to 9 on each page, question 72), the six named load fields (the standard nine,
question 73). Done differently and allowed by section 1: 26 markers on Letter and 28 on A4, the edge midpoints added by a new rule.

Answers entry 279 section 4's DESIGN NEEDED (Unholy's request). Alan: "2 MOA: C, 2.00 in bulls".

The concepts are on the Design canvas "GroupLab 2 MOA sheets"; their sources are copied, local only, to `C:\Dev\grouplab-local\design-concepts\2moa-2026-09-29\` (`Main.dc.html` the single page drawn to scale in millimeters, `SetB.dc.html` the set of three, `Detail.dc.html` the reasons). Take the geometry from them; build the sheets as library definitions like every other sheet.

### 1. The page (both sheets use it)

- **Letter, portrait. Nine bulls, 3 by 3, on a 2.5 in (63.5 mm) grid**: the cell lattice 190.5 mm square, lines at x 12.7, 76.2, 139.7, 203.2 mm and y 40.0, 103.5, 167.0, 230.5 mm from the top left. Bull centers at x 44.45, 107.95, 171.45 and y 71.75, 135.25, 198.75 mm.
- **Why not 3 by 4** (Code's option A as first written): twelve 2 in bulls on a 2.5 in grid fill the page, and the first and last bulls overlap the corner QR codes by more than half an inch. Keep the codes where every sheet keeps them.
- **The bull:** 2.00 in outer ring (the family's "1 MOA" is the 1.00 in bull, so this is "2 MOA" in the same sense: 1.91 MOA at 100 yd, 1.75 at 100 m, stated so in the description and on the Features page), an inner ring at 1.00 in, a center dot, ring strokes as the 1 MOA sheets. Half an inch between neighboring bulls, as the family keeps.
- **Markers:** AprilTag 36h11 at every lattice intersection except the two top corners, which sit against the top codes: 14 markers. Check that 14 register photographs within the gates (the 5x5 has 34); if the lens fit needs more, add markers at the midpoints of the outer cell edges and say so.
- **Codes:** the four corner codes as on every sheet. **Title band** between the top codes; **load block** between the bottom codes (date, distance, rifle, caliber, load, notes) as a declared exclusion zone.
- **A4:** the same grid; it sits about 8 mm from the side edges. Check it against the printer check's margins and the renderer's safe area; if it does not clear them, say so rather than shrinking the bulls.
- **Bull styles:** plain first, then the C and E bulls, as the 1 MOA family has them, sized to the 2.00 in bull.

### 2. The two sheets (option C: both)

1. **The single page**: nine scoring bulls, 1 to 9. For a quick group or a zero check.
2. **The set of three**: the same page three times, tile indices 1 to 3, bulls numbered 1 to 25 across the set, and bulls 8 and 9 of the third page as sighters S1 and S2. For load development. Each page reads itself; S10 pools them by tile index, and a missing page is reported, as for the large format sets.

Names and identifiers follow the library's scheme; Code chooses them and says which in the results.

### 3. The rest, in the same change (rule c)

The library's list, the designer, Made for your optic if it lists families, the Features page with each sheet's own picture (entry 256), the tour's Targets screen if it lists sheets, the README's sheet list, `docs/TARGET-LIBRARY.md`, and the conformance tests (render and read back each sheet, both page sizes, all three styles). Credit Unholy for the request, as the credits allow.

### 4. Order

After entry 288 (the updater) and the entry 280 sitting. Report it in `for-alan.md` with a line for Unholy saying where to print them.

## 2026-09-29, entry 290: PRIORITY, the whole window: iOS until Wednesday 30 September, 20:00 Mountain (2026-10-01 02:00 UTC)

**Status: in progress, the window runs to 2026-10-01 02:00 UTC.** Section 2 item 1, the shared mobile project, is built: the screens are
in `mobile/GroupLab.Mobile`, a plain .NET library, and the Android head answers `IPhonePlatform`. Item 2, OpenCV for iOS in CI, is being
built by a worker on the branch `ios/opencv`; items 3 to 7 and section 6's parity follow. Entry 289 (the 2 MOA sheets) runs alongside in
its own worker. The iOS checklist is at the top of `docs/notes/for-alan.md`.

Alan: "I just reset my claude usage for the week and back to zero percent. Please have code use this time between now and Weds at 8pm mountain to dedicate as much power and tokens as possible to getting the iOS build in a good state."

### 1. What this changes

- **From now until 2026-10-01 02:00 UTC, iOS gets the most effort, but not all of it.** Alan, later: "I want code to work on the iOS build as much as possible, but it doesn't necessarily have to do this in front of Android and Desktop things." So Android and desktop work continues alongside: entry 288 (the Android updater), entry 280's remaining screens after the sitting, entry 289 (the 2 MOA sheets), and anything Alan brings back from the printer check and his group photos. Give iOS the parallel capacity (its own subagents and worktrees, running continuously); keep one line of work on the rest of the inbox. An Android or desktop item Alan is waiting on goes first in that line.
- **Use the capacity.** Work continuously through the window. Split independent pieces across parallel subagents in their own worktrees (for example: the OpenCV iOS build, the shared mobile project, the AVFoundation camera, the CI job), and merge each only when its tests pass. Do not stop after each piece to report and wait: record progress in `for-alan.md` as you go and carry on. Stop to ask only where every path needs a decision from Alan, and even then keep working on the pieces that do not.
- **The plan is `docs/IOS-PLAN.md`,** sections 2, 3 and 5. Follow its order where it can be followed; where a piece is blocked, move to the next.

### 2. "A good state" by Wednesday evening, in order of value

1. **The shared mobile project.** The Avalonia screens move out of `android/GroupLab.Android/` into one project both heads link, with the Android-only calls behind small interfaces. **Android must stay exactly as good as it is** (rule b, phone parity): its tests pass, the nightly's android job stays green, and nightly 124 or later on the Fold 7 behaves as 123 did. A regression on Android is a stop-and-fix before anything else.
2. **OpenCV for iOS in CI.** OpenCV and opencv_contrib 4.13.0 with OpenCvSharp's native half, the same modules as `android/opencv/build-extern.sh`, built as a static xcframework for iOS arm64 and the arm64 simulator on `macos-26` with Xcode 26, linked through `__Internal`. Cache it or publish it as a build asset with its checksum, so every nightly does not rebuild it.
3. **The iOS head builds in the nightly**, `ios/GroupLab.iOS`, bundle ID `org.grouplab.app`, iOS 26 and later, iPhone and iPad, unsigned until the secrets exist, as section 3 says (a nightly never fails for want of them).
4. **It runs, proven on the iOS Simulator in CI:**
   - it launches to the first screen and every tab opens;
   - the imaging backend works on iOS: markers, homography, morphology, blobs, phase correlation and the QR codes on a rendered GroupLab sheet;
   - the whole pipeline on a committed sample scan gives the same shots and figures as the desktop, within the gate record's tolerances;
   - a picture chosen with the files picker is analyzed end to end.
   Save simulator screenshots of the main screens as a CI artifact for planning to look at. Only the app's own screens, no photographs from `camera-0929` or anything "testing only".
5. **The camera screen**, AVFoundation, Capture B as on Android: preview, 4:3 throughout, photo output, live analysis at 1920 by 1440, Guided and Manual, the level from Core Motion, the torch off after the picture, the camera released and taken again on background and foreground. It can only be proven on a device; build it so the simulator falls back to the files picker, and list what the first TestFlight sitting must check.
6. **Files, sharing, printing, and the OLED rules** (IOS-PLAN section 2).
7. **The TestFlight path ready:** when Alan's seven secrets appear (request 55), the nightly signs and uploads with no code change. Prove the unsigned path now, and the signing step's logic with a check that fails loudly and clearly if a secret is malformed, never printing it.

### 3. What waits on Alan

- **Enrolment.** The build to TestFlight and any install on the iPad mini need the Apple Developer Program membership and request 55's secrets. Apple can take up to two days to activate a new membership, so the simulator is the proof for this window if they are not there. Do not ask Alan for anything else about Apple; request 55 already says it all.

### 4. What does not change

Every standing rule holds: never see or print the Apple secrets or open `C:\Dev\keys`; no repository settings changes; no v* tags but the nightly's; nothing submitted to the App Store; the LICENSE permission of entry 279 already covers TestFlight; README, site and docs updated in the same change (rule c), with the platform line saying iOS is being built, and the Features page and `PHONE-PARITY.md` saying what the iOS build does so far; THIRD-PARTY-NOTICES for anything new.

### 6. Alan, a minute later: parity, and no waiting on him

"It should go through as many iterations as possible without needing anything from me. Lets get the app up and running and on par with the android build if possible."

- **The target is parity with the Android build**, not only section 2's minimum. Once items 1 to 4 are proven, go screen by screen through `docs/PHONE-PARITY.md` and bring each Android feature to iOS: capture, the result and figures, Sessions, Targets and printing, the printer check, Ballistics, Compare, Shots Needed to Zero, CSV import, hand marking, Fudd buster mode, units by tap, saving, error reports, the survey. Add an iOS column to `PHONE-PARITY.md` and keep it true as each lands.
- **Iterate without him.** Build, run on the simulator in CI, read the screenshots and logs, fix, and go again, as many rounds as the window allows. Never wait for Alan inside the window: anything that truly needs him (the Apple secrets, a device sitting, a visual choice) goes on the list in `for-alan.md`, and the work moves on around it. Where a visual choice comes up, follow the Android screen as it is; planning will raise anything that needs a concept afterwards.
- **Only on a real device** (the camera, the torch, the level, printing to a real printer): build it fully, prove what the simulator can, and put the rest on the first TestFlight sitting's checklist.

### 7. Alan, again: never idle

"Basically, code should never be pausing for anything when it could be using that time to work on iOS stuff. If it can multitask, that would be great."

- **No idle waits.** Whenever the main line would wait (CI, a nightly build, a device, an install, a reply from Alan, a report that has been written), it picks up the next iOS piece instead. A wait is never a reason to stop working; a written report is never a reason to end the turn.
- **Multitask.** Run the iOS pieces in parallel background subagents in their own worktrees, and keep them fed: when one finishes, start the next from section 2 and the parity list of section 6. Watch CI runs in the background rather than blocking on them.
- **The only stops:** a genuine blocker on every piece at once, or the end of the window. Even Android or desktop work Alan is waiting on runs alongside iOS, not instead of it.
- **Outside the window**, after 2026-10-01 02:00 UTC, this standing rule still holds in spirit: spare time goes to iOS until Alan says otherwise.

### 8. Alan, again: spend freely until the deadline

"I highly doubt it would be possible to run out of tokens before weds at 8 so burn through them and ignore the token savings we have talked about until then if it will help speed along the process."

- **Until 2026-10-01 02:00 UTC, the token-saving rules are suspended** wherever they slow the work: read whole files when it helps, run more parallel subagents, re-run CI and the simulator as often as it is useful, take and read as many simulator screenshots as the iterations need, and review your own changes with a second agent before merging. Speed and quality of the iOS build come first; cost does not count.
- **What stays:** every safety rule in section 4 (secrets, keys, repository settings, tags, backups before deleting, no App Store submission), and Android staying as good as it is.
- **After the deadline** the usual token care returns.

### 5. Reporting

- Keep a short iOS checklist at the top of `for-alan.md` (the seven items of section 2, each built, proven, or not yet) and update it as items land.
- At the end of the window, 2026-10-01 02:00 UTC, write a summary: what works, what is proven and how, what is left with sizes, and what the first TestFlight sitting on the iPad mini will check. Then return to the order in section 1.

## 2026-09-29, entry 288: PRIORITY: GroupLab Dev updates itself on the phone, so installing never depends on adb

**Status: in progress 2026-09-29. Sections 1, 3 and 4 done; section 2's unit tests done; section 2's device check not done yet**, because it
needs two nightlies with the updater: nightly 125 (this commit) installed over adb once, as the first copy that has an updater, and the next
nightly after it arriving by itself. The results, the first prompt, whether the second is silent, data kept and the minutes from publishing
to installing, go into `docs/ANDROID.md` section 17 and `docs/PHASE1-RESULTS.md` when it has happened. Nightly 124 is on both devices.
The signing key is confirmed the same: nightly 124's APK and the copies installed over adb on both devices carry one certificate.
WorkManager is used as the entry says (`Xamarin.AndroidX.Work.Runtime` 2.11.2.1, in the updater flavor only).

Alan: "It seems like code has a lot of problems connecting to my fold 7 despite me never turning off wireless debugging or stopping any services on my desktop. It is getting frustrating. Please tell code to make a new auto updater for the android app that if it is installed via adb, it can update itself by downloading a new apk and install it itself or I can press the update button."

Your own finding tonight (adb restarts moving discovery to a backend that finds nothing) is part of the answer and the note you saved stands. This entry removes the dependence altogether: adb stays for testing and logs, never again for getting a build onto the phone.

### 1. What to build

1. **An updater in the sideloaded Android builds**, GroupLab Dev (`org.grouplab.app.dev`) on the nightly train, and later the sideloaded `org.grouplab.app` on its own train. Same trains, same ordering and the same signed `update-manifest.json` as the desktop (`docs/UPDATES.md`): the manifest names the APK, its size and its SHA-256, and its signature is checked against the key built into the app before anything is downloaded.
2. **Checking:** on launch and about every six hours in the background (WorkManager), one plain HTTPS GET as the desktop does, nothing about the person sent. Offline is silent.
3. **Downloading:** by default only on Wi-Fi, in the background, resumable. Before installing: the file's SHA-256 matches the signed manifest, and the APK's signing certificate matches the installed app's. Anything that does not verify is deleted and logged, never installed.
4. **Installing:** through `PackageInstaller` sessions, with `REQUEST_INSTALL_PACKAGES`.
   - The first time, Android asks Alan to allow "Install unknown apps" for GroupLab Dev. The app explains this in one sentence and opens the right settings page.
   - **Installed by adb, the first self-update will ask for a tap**, because the installer of record is the shell, not the app. After that the app is its own installer of record, and on Android 12 and later it asks for `USER_ACTION_NOT_REQUIRED`, so later updates can install with no prompt. Say in the results whether the Fold 7 and the tablet then update silently, and on which Android versions they do not.
   - **Never in the middle of work:** not while the camera is open, an analysis is running, or a change is unsaved. It waits until the app is in the background or idle, or until the person presses the button.
   - Data is kept: an in-place update, same package, same signing key. Say in the results that the key the nightly signs with is the same key the adb installs used, so the first self-update does not fail on a signature mismatch.
5. **The button:** Settings, About (or wherever the version is shown): the installed version, the newest on its train, "Update now", and a switch "Install updates automatically" (on by default for GroupLab Dev). A small notice when an update is downloaded and waiting. After an update, a one-line "Updated to nightly N" with a link to its notes.
6. **Never in a Play build.** Google Play does not allow an app to update itself outside Play, and restricts `REQUEST_INSTALL_PACKAGES`. The updater and the permission exist only in the sideloaded APKs (a build flavor), and the app also turns the updater off if its installer of record is Play. The AAB for Play must not contain it: a test that fails if it does.

### 2. Tests and the device check

- Unit tests: a manifest with a bad signature, a wrong hash, a different signing certificate, an older version, another train, and offline; each refuses or stays silent as the desktop's tests require.
- On the devices, once: install the nightly with adb, then publish the next nightly and let the app update itself. Record: the prompt the first time, silent or not the second time, data kept, and how long from nightly published to installed.

### 3. Documents, in the same change (rule c)

`docs/UPDATES.md` gains an Android section; `docs/ANDROID.md` says which builds carry the updater; the research article `what-grouplab-sends` and the "safe updates" article cover the phone; the Features page and the user guide say how to update on the phone. `PHONE-PARITY.md`: updates on the phone, yes, except Play builds, which Play updates.

### 4. Order: now, first

Alan, later the same evening: "Can you tell it to start on the new android app now because I can't do the scale test this instant." Start this entry now, before the scale test and before anything else in entry 280. Request 53 stays open for whenever Alan can do it; the phone does not need to wait on its card screen. Install nightly 124 on both devices when it is out, as the last adb install before the updater takes over. Once the updater works, the "install nightly N on the phone" step in future entries means the app does it itself, and adb is only needed for logs and screenshots.

## 2026-09-29, entry 287: the home page's top picture becomes the README's product picture

**Status: done 2026-09-29.** The home page's top picture is the README's product picture, made into WebP by the site build from `docs/figures/readme/product-*.png` at 1600 by 900, dark and light, with the README's own alt text and its three numbered lines read from README.md, so both change together. The analysis screenshot stays everywhere else. At 500 pixels wide the whole picture shows, the phone uncut; the number badges are small there, and a 390 pixel phone was not checked here because headless Chrome will not go that narrow. The site's screenshot set has no picture of the home page, so none needed catching up.

Alan: "I think the picture at the top of the grouplab.org page should be the one used on the github page that shows the target, the application, and the mobile app."

1. **Replace the hero on grouplab.org's home page** (today `analysis-dark-1400x900.webp` and `analysis-light-1400x900.webp`) with the README's product picture: `docs/figures/readme/product-dark.png` and `product-light.png`, the shot sheet numbered 1, the desktop analysis numbered 2 and the phone result numbered 3. Dark and light follow the site's theme as the current hero does.
2. **Keep the numbers meaningful.** Put the README's three lines under it on the home page, word for word, as a numbered list, so 1, 2 and 3 on the picture are explained there too:
   1. Shoot any target, or print a GroupLab sheet that reads itself.
   2. On the computer, every hole found, then the group with honest ranges.
   3. On the phone, photograph it at the range.
3. **One source.** The site and the README use the same picture from the same source, so a regenerated product picture updates both (rule c). Serve a web-sized copy (webp, with width and height set, `fetchpriority="high"` on the first, the dark and light pair as now), and keep the README's alt text.
4. **Phones.** Check the home page at phone width: the picture scales down without cropping away the phone at the right, and the numbers stay readable. If they do not, say so in `for-alan.md` rather than cropping.
5. The analysis screenshot that was the hero stays wherever else it is used; do not delete it.
6. Screenshots of the home page (the site's own `docs/figures/screens` set) catch up in the same change, and say in `for-alan.md` that it is live.

## 2026-09-29, entry 286: PRIORITY: put the newest GroupLab Dev on the Fold 7 now; Alan is printing request 53's pages

**Status: done 2026-09-29.** Nightly 123 is on the Fold 7 and the tablet, installed over the existing app. The Fold had dropped off adb because I had restarted the adb server on a discovery backend that finds nothing on this PC; back on the default one it was seen again, and Alan paired it once more. The card photo screen opens (Settings, Printers, Add a printer, A card and one photo, Next); request 53's wording is corrected to that path.

Alan: "Can you have code move the new version to my phone and I will print the 3 pages."

1. **Before anything else in the inbox**, install the newest published GroupLab Dev (`org.grouplab.app.dev`) on the Fold 7 over wireless adb: nightly 123 if it has published by the time this is read, otherwise nightly 122. Install over the existing app so its data stays. Touch only GroupLab Dev; put back any setting changed; do not read lock-screen notifications.
2. **If the Fold 7 is not reachable** (it may still be off Wi-Fi, or the pairing may have lapsed), put one line at the very top of `for-alan.md` saying so, with the exact steps: Wi-Fi on, then `adb pair` and `adb connect` from `C:\Dev\tools\android-sdk\platform-tools\adb.exe`, as in entry 277. Then try again when he says it is back.
3. **When it is installed**, say so at the top of `for-alan.md` with the nightly number, and confirm that Settings, Printers, Check your printer, Card opens on the phone, because request 53 step 3 uses it for the card photos.
4. The tablet already has nightly 122; bring it to 123 as well if 123 is out, in the same sitting.
5. Then carry on with entry 280 section 2.

## 2026-09-29, entry 285: the README's three-line table under the main picture shows an empty row on GitHub

**Status: done 2026-09-29.** The table is a numbered list, the three sentences word for word; nothing else read the table. Checked on GitHub in light and dark after the push.

Alan, from a screenshot of the repository's front page: "the table below the main image has an empty row in it".

**Why.** README.md lines 18 to 22 are a Markdown table whose header row is blank (`| | |`). GitHub always draws a table's header row, so it shows as an empty bordered row above rows 1 to 3.

**The fix.** Replace the table with a plain numbered list, which matches the 1, 2 and 3 on the picture and has no header to draw:

```
1. Shoot any target, or print a GroupLab sheet that reads itself.
2. On the computer, every hole found, then the group with honest ranges.
3. On the phone, photograph it at the range.
```

Keep the three sentences word for word. If a test, the consistency audit, or the site's build reads that table, change it in the same commit so they agree (rule c). Check the result on GitHub in both light and dark themes, and say in `for-alan.md` that it is fixed.

## 2026-09-29, entry 284: the architecture pages ("Behind the curtain"): Alan approved the refined design; build it

**Status: done 2026-09-29.** The four pages are built from `website/how-it-works.json` and the drawings in `website/how-it-works/` by `website/how_it_works.py`, at the paths suggested: `/tour/how-it-works/` and its `opencv/`, `hole-detection/` and `pipeline/`. The map works without JavaScript (every part and step a plain section); the drawings are SVG in the site's own colors, light and dark; the tour, the README's Architecture section and the Features page link to it. All 255 figures are in the data file, each with its source, and the build fails on a figure without one or on a source that is not in the repository. Every one of 321 claims was checked against the code; 63 were corrected, listed in PHASE1-RESULTS.md. The pages at phone width wait for a look on the phone: headless Chrome will not lay out narrower than about 500 pixels.

Alan asked for a page linked from the tour that shows how GroupLab works behind the curtain: how the parts fit together, what each is built from, how OpenCV is used, and how the hole detector was developed, linking to the research articles and the Features page. He chose concept B (a clickable map) with concept C's drawings, asked for it to be much more thorough and technical, and approved the refined design: "Architecture refined: approved".

### 1. The design, and where to read it

- The approved boards are on the Design canvas "GroupLab architecture tour concepts", page "Refined: B with C's drawings". Their sources are copied, local only, to `C:\Dev\grouplab-local\design-concepts\architecture-2026-09-29\`:
  - `RefMap.dc.html`: the map, the page reached from the tour (interactive).
  - `DeepOpenCV.dc.html`: deep dive, how GroupLab uses OpenCV.
  - `DeepHoles.dc.html`: deep dive, how the hole detector was built.
  - `DeepFlow.dc.html`: deep dive, follow a target stage by stage (S0 to S10).
  - `PhoneDeep.dc.html`: the OpenCV deep dive at phone width, as the pattern for all three.
  - `Illus.dc.html`: the eight step drawings (print, shoot, capture, register, detect as photo minus artwork, assign, combine, report).
  - `TourLink.dc.html`: how the tour links to it (a card at the end of the tour and a small link at the top).
- The files are the design's HTML. Take the layout, the order of sections, the words and the drawings from them; build the pages in the site's own way. They are not to be copied into the repository as they are.

### 2. What to build

1. **Four pages on the site**, suggested paths `/tour/how-it-works/` (the map) and `/tour/how-it-works/opencv/`, `/tour/how-it-works/hole-detection/`, `/tour/how-it-works/pipeline/`. Code may choose better paths; say which in the results.
2. **The map.** Layers as drawn: Apps (desktop, Android, iPhone and iPad being built, command line), GroupLab.Core (target format, renderer, registration, hole detection, statistics, ballistics), Imaging (OpenCV behind IImagingBackend), Checked at build time (R shotGroups, Python), Online (site, server workers, builds and updates). A click shows the part's summary, key numbers, built from, where it lives (linked to GitHub) and research links, and a button to its deep dive where one exists. "Follow a target" steps through the eight steps with the drawings and the stage codes each covers.
   - **It must work without JavaScript**: with scripts off, every part's summary is readable as a plain list, and each step as a plain section. The interaction is an enhancement.
   - **Phone:** the map stacks by layer and a tap opens the summary as a sheet or an expanding section; deep dives stack their tables into cards as `PhoneDeep` shows.
3. **The three deep dives** as drawn, including the drawings (marker detection in six panels, the morphology in five panels, the widths ruler of printed lines, hand strokes and holes).
4. **Light and dark**, like the rest of the site. The drawings are SVG and follow the theme.
5. **Links in:** the card at the end of the tour and the small link at its top (`TourLink`), and a link from the README's Architecture section and from the Features page where it fits. Rule (c): README, site and assets stay consistent, in the same change.
6. **The numbers come from one place.** Keep the figures the pages show (for example 343 holes, 45,476 values compared with R, 33 of 37 sheets named, 0.00319 in) in one data file the build reads, each with the document or command it comes from, so the consistency audit can check them and a changed figure changes everywhere.

### 3. Check every fact before it is published

Planning took every figure from the repository's documents and code (DETECTION-PIPELINE.md, DETECTION-LEARNING-STUDY.md, FIDUCIAL-DECISION.md, MOBILE-CAPTURE.md, PHASE1-RESULTS.md and results-milestones.md M1.5 and M2.2, SCAN-MEASUREMENTS.md, IImagingBackend.cs, PortableImaging.cs, OpenCvSharpBackend.cs, the Detection and Registration summaries, android/opencv/build-extern.sh). Check each one against the current code and results, correct what has moved, and list the corrections in the results. In particular:

- **The timeline dates** on the hole-detection page (13, 14, 16, 21, 22, 24 and 28 September) were read from the commit history and entry numbers. Confirm each.
- **The QR path:** the page says the WeChat detector is used without its neural network models, finds the codes, and the plain decoder reads the bytes. Confirm this is the same on desktop and Android, and that no WeChat model files ship.
- **"No learned model decides what is a hole"**, shown as "0 learned models". Confirm, or reword.
- **The phone's live analysis resolution:** the page says 1920 by 1440 (MOBILE-CAPTURE.md C3a). Entry 281 changed the camera; use whatever is true after it.
- **"Every change is built and tested on Windows, Linux and macOS"** and **"a few seconds to analyze a 300 dpi Letter scan"**: confirm or reword.
- **Marker counts:** 34 on GL-CF25-LTR, but the curl test sheet read 38; the page must not make those look contradictory.
- **Holes on ink:** DETECTION-PIPELINE.md says their rim is paler; M2.2 says the synthetic ones are drawn darker. Planning left the direction out. Settle it from SCAN-MEASUREMENTS.md section 3.6 and word it once.
- **The trace block** on the pipeline page is labeled illustrative, in the specification's format. If a real `--trace` run of a GroupLab sheet can supply the text, use a real one and say so.
- **Synthetic results** are always labeled as an upper bound, as the page does.
- **Where the site says "the developer"**, keep that wording; the credit rules stand (Unholy and Jylee may be credited, Justin by first name only, Fenix thanked).

### 4. What must not appear

- No photographs from `camera-0929`, `planning\camera-0928`, the friend's 2026-09-16 scan, or anything sent as "testing only". The pages use drawings only; any real image needs its own consent check first.
- No server address, no key names, no internal paths beyond the repository's own.
- No mention of a lawyer reviewing anything.

### 5. When it is done

Say in `for-alan.md` where the pages are on a local build and on the nightly site, with the list of facts corrected. Planning will look at them and send Alan the links.

## 2026-09-29, entry 283: with entries 281 and 282: the shutter is slow to respond; measure it and make it fast

**Status: done 2026-09-29, in part.** Built: every step from the press to the first result in the log, the sound and flash at the press, the live analysis standing aside, the minimum latency capture mode (the quality mode kept for GroupLab Dev's comparison), and `scripts/shutter-timing.py`, the repeatable device test. Not done: sections 1 and 3's numbers, twenty presses on each device, which need the build that carries the timings; they go in `docs/ANDROID.md`.

Alan: there is a lot of lag between pressing the shutter in GroupLab Dev and the phone responding with the shutter sound and starting to read the picture. Treat it as part of the camera work of entry 281.

1. **Measure first.** Instrument the path from the press to each step, with timestamps in the capture log: press received; capture request sent; sensor exposure (the shutter sound); image available; image decoded and rotated; saved; analysis started; first result on screen. Run it on the Fold 7 (and the Tab S8 Ultra at the next sitting), 20 presses each, torch off and on, Guided and Manual, and report the median and slowest for each step in a table in `docs/ANDROID.md` or a benchmark note.
2. **Then make it fast.** Likely causes to check against the numbers: the capture mode (CameraX `CAPTURE_MODE_MINIMIZE_LATENCY` rather than maximum quality, or zero-shutter-lag where the phone supports it); a torch or autofocus and exposure pre-capture sequence run at every press when the live view has already settled them; decoding a full-resolution JPEG on the UI thread; saving before analysing instead of alongside; the live analysis still running and competing for the camera and the CPU at the moment of capture. The shutter sound and a visible "taken" flash must come at once, and the reading can follow with its progress shown.
3. **A target to aim for, reported against:** the shutter responds (sound and flash) within about 0.3 s of the press, and the first result appears as soon as the detector allows, with the time for each step shown in the log. Keep the measurement as a repeatable device test so it can be rerun after changes.

## 2026-09-29, entry 282: with entry 281: Alan's eight screenshots of the camera test, and what they show

**Status: done 2026-09-29, in part.** 1: every phone text taking its words from a value wraps, held by `PhoneTextWrapsTests`. 2: with entry 281, the picture kept in shape and turned upright; the saved sessions carry orientation 6, which the display now honours. 3: the whole screen laid out and drawn again on a return. 4 and 5: the codes cut out where the markers put them and read enlarged, which names the refused picture and reads 2 of 2 on a good one; where they still fail the sheet it looks like is offered (question 69). 6: a picture with notes scores at most 95, and the notes are in a shooter's words. 7: the windage gives its amount. Not done: 8, the empty plot, is in the long screenshot only and not in the ordinary one of the same moment, so it looks like the long capture itself; it is checked on the device at the next sitting. 9 goes with entry 280. The device checks of 1 to 3 wait for the next nightly.

Alan's screenshots are in `C:\Dev\grouplab-local\camera-0929\screenshots-alan\` (local only, never committed; the pictures in them show personal papers in the background, so they are never published or put in a public log). Planning read them. In addition to entry 281's list:

1. **Text runs outside its box** (Alan pointed this out). The numbered notes under the score do not wrap; each line is cut off at the card's right edge ("GroupLab could not read the square codes that r…", "32 of 34 markers read; GroupLab measured fron…", "A shadow falls on bull 23, evened out: check its…", "Move closer next time: the bulls got 137 pixels a…"). The section heading "Full CEP table and the fitted" is cut off too. Every text in a card wraps to the card's width, on the phone at every width and font scale; add a layout test that fails on a clipped text.
2. **The picture on the result is the wrong part of the photograph, and stretched.** Under "Drag a ring to the center of its hole" (181332-long, 181356) the picture shows the wall and the board above the sheet, stretched tall, not the sheet the preview showed. This is entry 281 items 6 and 7 together, and possibly the orientation: the preview in the score card shows the sheet turned a quarter, so check the photograph's rotation from capture to result.
3. **Blank buttons** (181924): on one Retake screen both buttons are empty and the bottom bar's labels are gone, with the icons half drawn. Alan confirms this is what the screen looks like after minimising the app and reopening it: the same resume fault as entry 281 item 5 (the camera does not restart). On resume, the whole screen must redraw with its text, not only the camera rebind; the lifecycle test must check that the buttons and the bottom bar have their labels after a return.
4. **A score of 0 when the sheet is plainly readable** (181205, 181924): "GroupLab could not read the square codes", yet the whole sheet is in view and sharp. By Alan's tolerance-first rule a picture whose markers are read must still be measured: when neither code reads, identify the sheet from its markers' layout or ask which sheet it is (the text already starts "or choose wh…"), and score it on what it can measure, not 0.
5. **Only one of the two codes is ever read** ("1 of 2 codes read" on every good picture). Find why the second code fails (its size, its position near the edge, the torch's glare, the resolution it is read at) and fix it.
6. **The score says 100 while it lists notes** (181322: "Good with notes", 100, "32 of 34 markers read"). A picture with notes is not a perfect score; make the score and the notes agree, and word it for a shooter: "137 pixels" means nothing to him ("move a little closer next time" does).
7. **Zero, windage shows no number** (181332-long, All figures): "right" and "left" appear without the value, while elevation shows "0.2 MOA down". Fix it and test both.
8. **The plot is empty in Alan's scrolling screenshot** (181332-long): only an arc and one ring in the top left, though the ordinary screenshot of the same moment shows the plot correctly. Check whether the plot redraws wrongly when the page scrolls or is captured long; fix it if so.
9. **The hint still says "everywhere at once"**; entry 280 changes the tap to one number, so its words change with it.

## 2026-09-29, entry 281: PRIORITY: the camera test went badly; pull the logs now, then these fixes before the rest of the inbox

**Status: done 2026-09-29, in part.** Section 0: the logs are in `C:\Dev\grouplab-local\camera-0929`, the pictures stripped of every tag first; what they show, item by item, is `docs/MOBILE-CAPTURE.md` section 7. Section 1: all eight built. Item 3 is two buttons, Camera and Result, above the Capture page and a Result button on the camera, so no DESIGN NEEDED. Item 8: the stream was already 1920 by 1440; the live words and the picture disagreed because resolution was judged at the stream's scale, and the codes fail on the picture too, so a picture whose codes cannot be read now says which sheet it looks like, offered and not taken (question 69). Not done: the device checks, and the results in plain words at the top of `for-alan.md`, which wait for the nightly that carries the fixes; the lifecycle, torch and viewport have no test here because they are Android's own calls. Section 2 is recorded and is actioned with entry 279.

Alan finished the camera test on the Fold 7 with GroupLab Dev nightly 121 at about 00:25 UTC. "It did not go very well." Finish the step you are in the middle of, then do this entry before 278 to 280.

### 0. First, now: pull the logs

Over adb (the Fold 7 is paired and its authorization no longer expires), from GroupLab Dev only (`run-as org.grouplab.app.dev`): the capture log, the per-frame guidance log, the picture scores, the saved pictures of the test and their results. Keep everything under `C:\Dev\grouplab-local\camera-0929\`, never committed. Strip location, time and device metadata from any picture before anything else reads it; the pictures are testing-only and never published or put in a public log. Write in the panel, in one line, that the logs are in. Then read them against the list below and say, for each item, what the logs show.

### 1. What Alan found, and what to do

1. **The level: a four-way crosshair with a dot that moves like a bubble level.** Replace the current level indicator with a crosshair (four arms) and a dot driven by the gravity sensor: the dot sits at the centre when the phone is parallel to the table and drifts toward the high side the way a bubble does. It turns the ready colour inside a small tolerance. This is Alan's own specification; build it as described.
2. **The torch stays on after the picture.** It must go off as soon as the picture is taken, when the capture screen is left, and when the app goes to the background. Torch Auto, On and Off keep their meaning during capture only.
3. **Capture and analysis must be two separate buttons.** Going back to the camera now means scrolling to the bottom of the result and pressing Take a new photo. Make Capture and Analysis (the result) two separate, always-visible buttons, so either is one press away without scrolling; the result never hides the way back to the camera. If this needs a layout choice beyond two buttons, post it as DESIGN NEEDED for planning to draw.
4. **"Move closer" and "move back" are far too sensitive.** They seem to want millimetres. Give the distance guidance a dead zone and hysteresis: an acceptable band of sheet size in the frame rather than a target value; once inside, stay "good" until clearly outside the band; smooth over recent frames and change the words only after a new state has held for about half a second. Do the same for tilt and the other guidance lines. Choose the band from the logs and the synthetic tests, and write down the numbers.
5. **After minimising and reopening the app, the camera does not start.** Rebind the camera on resume and release it on pause (the lifecycle), with a test that leaves and returns to the capture screen and the app.
6. **The viewfinder is not the same scale as the saved picture.** The preview and the captured picture must show the same field of view: the same aspect ratio for preview, analysis and capture, and a shared viewport so the capture crops exactly what the preview showed. What the shooter framed is what is saved.
7. **The picture is stretched on the analysis page.** Keep its aspect ratio everywhere (uniform scaling, never fill), and honour its orientation.
8. **The live finding of tags is poor, but the taken picture scores much higher.** Alan asks whether the viewfinder image is lower resolution. It very likely is: CameraX's image analysis defaults to about 640 by 480, while the picture is full resolution. Give the live analysis a resolution high enough to find the tags from where people stand (choose from the logs; 1280 by 960 or more), analyse a frame as often as the phone can keep up rather than every frame, and use the same detector settings as the final picture, so the live checks and the final score agree. Report the live and final tag counts from the logs, before and after.

Each fix gets a test where one is possible; the device checks wait for the next sitting, which Alan can do soon now that both devices are paired. Put the results at the top of for-alan.md in plain words when the fixes are in a build.

### 2. Alan's answers to row 11 (they go with entry 279)

- **Fudd buster mode: A**, one page with three sections, as on board FuddA of the phone parity canvas. The desktop shows the same page as a window.
- **Knowing it is saved: A, with a setting for A or B.** Default A: saved by itself, with "Saved N seconds ago", where the file is, "Show in folder", and "safe to close". Settings offers B: a Save button, a "Not saved yet" marker, and a question on closing an unsaved target. Both on the desktop; the phone follows A, and B where it applies.
- **Tabs: OK** as on board Tabs: New target opens a second tab below the controls and the first stays open; tabs appear only once a second target is open; each tab shows its saved mark and closes with its own ×.

## 2026-09-29, entry 280: tap to switch changes one value only; row 10 chosen; "Fudd buster mode" kept; both devices paired

**Status: done 2026-09-29, in part.** 1: a tap switches the number tapped, converted from what it shows (`UnitSwitch.Convert`, a range as a whole), and the unit is remembered for that figure by its name and kind, on both platforms; Settings keeps the units of figures never tapped, press and hold lists every unit for that number, and the hint says "this number"; `UnitTapTests` holds that one tap leaves the others unchanged. 3: "Fudd buster mode" is the name in the application, the Features page and the guide. 4: noted in STATE. **Updated 2026-09-29: done.** Section 2's row 10 is built on both platforms (Shots A, Share A, several aim points, the one-page report and Zero from this group, the desktop's in its own layout, from shared Core code), and section 1's full CEP table cells carry their own unit (question 70, answered B). Not done: the phone screens' device check and phone photographs, at the next sitting.

### 1. Tap to switch units: one value, not every value (changes entry 272's behaviour)

Alan: "when you tap a value, it should only change that individual value and not all of the values displayed on the screen." Today a tap switches every angle (or every size) on the screen at once. Change it so a tap switches only the number tapped. Planning's reading, which Alan may correct: the choice is remembered for that figure, so the same figure shows in the same unit on the next result and wherever that figure appears again (mean radius in mil stays in mil; extreme spread can stay in MOA). Settings keeps the default units for figures never tapped; press and hold still shows every unit for that number; the one-time hint changes its words to say "this number". Both platforms, the desktop's table cells included where entry 273 left them untappable. Update the user guide, the Features page entry, its picture and the tests (a test that tapping one figure leaves the others unchanged).

### 2. Row 10 of the phone parity canvas: Shots A, Share A, rest OK

The entry 278 item 5 screens may now be built as drawn:
- **Shots A** (board ShotsA; features c and f): a table of every shot with its offset across and up and down from the aim point, the clicks to bring it to the aim point (from the scope's click value), and a switch "Counted" per shot; a left-out shot is struck through in the table and dashed on the picture; the summary line gives the counted figures and says a shot was left out by the shooter; "Share the table (CSV)".
- **Share A** (board ShareA; features d and g): a results box on the picture, dragged anywhere and resized by pinch or its corner handle, lines chosen by tapping it; chips for the mean radius circle, a label, the box's style and crop; "Save to gallery" and "Share". The mean radius circle is drawn about the group's centre.
- **Several aim points** (board MultiAim; feature b): a chip per aim point in its own colour, "+ Aim point", holes belonging to an aim point by colour, figures per aim point and pooled when the same load.
- **The report** (board Report; feature e): one dated Letter page (A4 where set): the picture, a plot centred on the group and scaled to fill with a stated grid, the figures table, the load and equipment line, and the confidence sentence.
- **Zero from this group** (board ZeroFrom; feature h): the group's centre from the aim point, the clicks with the scope named, how well the centre is known, "Open in Shots Needed to Zero" and "Use as the zero offset in Ballistics".
Desktop equivalents follow the same content in the desktop's layout. Numbers on the boards are samples.

### 3. The small-samples window keeps Unholy's name: "Fudd buster mode"

Alan's decision; it is the feature's name in the application, the Features page and the guide. Planning draws the window next.

### 4. Both devices are paired, and Alan turned off the automatic revocation of wireless adb authorizations

The Fold 7 and the Tab S8 Ultra are both paired with this computer again, and their authorizations should no longer expire. Note it in STATE's devices section. The camera test is under way on the Fold 7 (entry 277).

## 2026-09-29, entry 279: Alan's follow-ups (App Store permission, Marking A confirmed), and Unholy's request, bug reports and feature idea

**Status: done 2026-09-29, in part.** 1: the section 7 permission heads LICENSE, the README's License section names it and CONTRIBUTING says every contribution is accepted under it. 2: Marking A is built on the phone (`MarkingAPage`), reached from a picture whose codes cannot be read. 3: the 2 MOA sheets are DESIGN NEEDED in `for-alan.md`, with the options; the bug is fixed with entry 278 (a shot left out is out of every figure, `LeftOutShotTests`); saving today and the tabs are answered below; Fudd buster mode's engine (`FuddBuster`, with fixed seeds per result) and, with entry 281 section 2's choices, page A on the phone and a window on the desktop, and saving A with a setting for B on the desktop and A on the phone. How saving worked: the desktop saved a session only on Accept and analyze, to grouplab.db beside settings.json, and asked before leaving a target edited since; nothing on the screen said so; the phone saved on every result and every edit. The tabs: the desktop holds one target (one marking session and its saved state), so two open at once needs that moved into a document the window holds several of, several days of work. Not done: the tabs, which are next; the 2 MOA sheets, which wait for the layout; and the device checks of Marking A and the Fudd page.

### 1. The App Store permission: yes

Add a GPLv3 section 7 additional permission to LICENSE (and name it in the README's License section) allowing GroupLab to be distributed through Apple's App Store and TestFlight under Apple's terms, in the form other GPL projects on the App Store use (Signal, Nextcloud iOS). Alan is the only copyright holder today, so he can grant it; say in CONTRIBUTING that every contribution is accepted under the licence including this permission. The iOS plan of entry 278 item 6 may now include App Store submission once the app is ready; the submission itself still waits for Alan.

### 2. Marking A: confirmed

Build the phone's marking of a target GroupLab did not print as drawn on the phone parity canvas, row 8, boards MarkA1 and MarkA2 (redrawn): three steps, scale, bull, holes; the picture pans and pinches under a crosshair fixed at the screen centre; a button sets the point and counts ("Set end 2 of 2", "Add hole here (6)") with Undo; the crosshair ring drawn at the bullet's real size; holes GroupLab found are marked and numbered, holes the shooter adds are marked differently, a wrong mark is removed by putting the crosshair on it; "Done: measure N shots" and "Keep as a template". The same crosshair marking is feature 5a of entry 278, and correcting a hole on any result uses it too.

The concepts for entry 278 items 5b to 5h are now on the same canvas as row 10; build their screens only after Alan's answer arrives as its own entry.

### 3. From Unholy (he may be credited by that name)

**Request: a target with 2 MOA circles instead of 1 MOA,** "for guns that don't shoot 1 MOA". Find each GroupLab sheet whose circles are 1 MOA and make a 2 MOA variant at each distance it comes in (larger circles; fewer bulls to a page where they would not fit). Credit Unholy on the Features page. If a layout choice arises (how many bulls, which sheets first), post it as DESIGN NEEDED rather than choosing.

**Bug: excluding a shot does not exclude it from the analysis.** Reproduce it on the desktop (and the phone if the phone has the control), find where the exclusion is lost (figures, plot, pooled figures, Shots Needed to Zero, exports, a reopened session), fix it, and add tests that fail today. This overlaps feature 5c (a switch per shot); the rule there holds: the shot stays on the record, shown as left out, and every figure and export respects it.

**Feedback: nothing says when a target is saved, or where.** He sees earlier targets under Sessions but wants an easy way to know the current one is saved and safe to close. Write down exactly how saving works today (automatic or not, when, where the file lives) in the answer, then planning draws the indicator (a concept goes to Alan). Until then, change nothing visible.

**Feedback: "New target" should not close the current target.** He suggests that New target opens the new one alongside, as tabs below the control buttons, and that the tabs appear only once a second target is open. Say in the answer whether the desktop can hold two results open at once today and what it would take; planning draws it for Alan.

**Feature idea (Alan's working title is Unholy's: "Fudd buster mode"; the final name is Alan's).** After an analysis of 20 or more shots, an optional window that uses the shooter's own shots to show why small samples mislead:
1. Random three-shot samples from his own shots, the tightest and the widest, shown side by side with the line "These shots came from the same gun, with the same ammunition, on the same range trip."
2. What his "group size" would have been had he averaged three-shot or five-shot groups instead, against the figure from all the shots, and how much that average moves with how the shots are split.
3. The zero chase: take the shots in order, five at a time; "set the zero" to each five-shot centre in turn, and show how far the correction wandered from the rifle's true centre (the centre of all the shots), and how many clicks each "correction" would have been.
Build the engine now as pure functions over a result's shots, with a fixed random seed per result so the same result always shows the same examples, and tests. The words must be exactly true (for example, averaging small groups' extreme spreads is biased low and still noisy; say it that way, not that it is "invalid"). The window itself waits for planning's concept and Alan's choice. It serves Alan's standing goal of teaching mean radius and confidence over small-sample extreme spread; the Features page credits the idea to Unholy.

### 4. DESIGN NEEDED

Update the line: the concepts for 5b to 5h are with Alan; planning is drawing the save indicator, the tabs for a second target, and the small-samples window; the 2 MOA sheets may add one of their own.

## 2026-09-28, entry 278: Alan's decisions of the evening: marking, CSV import, the card outline test, the store-bought target, eight features from the competitor study, and iOS

**Status: done 2026-09-29, in part.** 1: not built, as it says; entry 279 section 2 then confirmed Marking A. 2: CSV B built on the phone (Sessions, Import shots from a CSV file) and the desktop's dialog starting from the same guesses (`CsvGuess`), with numbers measured from the group's center imported with no aim point. 3: the three pages are in `C:\Dev\grouplab-local\scale-test` (`grouplab scale-test-pages`) and request 53 asks for the prints, scans and card photos; the measurement waits for them. 4: request 54. 5: the engine and data work: each aim point's own figures (`GroupAnalysis.ByAimPoint`), each shot's offset and clicks (`ShotOffsets`), and a shot left out left out of every figure, the saved session and what is sent (`GroupReport.Counted`, which is entry 279's bug); 5h's engine was there already. The spread audit found the mean radius explained as the plain average when it is the Rayleigh estimate, 1.2533 sigma, and the 95 percent ellipse claiming 95 percent of later shots; both reworded, and `SpreadFiguresTests` holds a five-shot group worked by hand. 6: `docs/IOS-PLAN.md`, request 55, and iOS as being built everywhere. 7: the DESIGN NEEDED line. Not done: 6.2, the nightly's iOS job, which needs the iOS head of the plan's step 3 to build; the screens of 5a to 5h, which wait for their concepts (entry 280 has since chosen some); and the phone's CSV B checked on a device.

Alan answered at about 23:45 UTC. Background for items 5 and 6 is in the planning project doc `competitor-study.md` (Ballistic-X and Hornady Group Analysis, from his own screenshots) and in the planning chat; the essentials are repeated here.

### 1. Marking a target GroupLab did not print (phone): A, step by step, preliminary

Alan chose A (scale, then bull, then holes) as a preliminary answer and asked planning to redraw it first with the fixed crosshair both competitors use: the picture moves under a crosshair fixed at the screen centre, a button places the point and counts ("Mark hole (5)"), with Undo; markers drawn at the bullet's real size and numbered. **Do not build the marking screen yet.** The redrawn concept goes to Alan, and his final answer comes as its own entry. Work that does not decide the look can start (see 5b).

### 2. Importing shots from a CSV file (phone): B, GroupLab guesses and you check

Build CSV B as drawn on the phone parity canvas (board CsvB): after a file is chosen, GroupLab reads the column names and numbers and guesses which column is across, which is up and down, the unit and whether the numbers are measured from the aim point or the group's centre; it shows the group as it will be read and a card of its guesses, each line tappable to change; "Import N shots" and "Show me the whole file". Where it cannot guess a line, that line says so and asks. The desktop's import dialog gains the same guesses as its starting values (parity). Tests with files whose names say the unit (`x_in`), files that do not, millimetres, centimetres, MOA and mil, and a file with the vertical sign flipped.

### 3. Question 67, the card outline: print all three and let a real photograph decide

Alan wants to test rather than choose. Make three printable check pages, Letter, identical except for the card outline, each marked in large type **TEST A**, **TEST B** or **TEST C** and "not for use":
- **A**: the outline 3 mm outside the card, as built;
- **B**: a hairline exactly on the card's edge, "edges on the line";
- **C**: corner marks only, 3 mm outside the card.

Put the three PDFs in `C:\Dev\grouplab-local\scale-test\` (local, never committed). Then write a request for Alan (the next number) with exact steps: print all three in one batch at 100 percent on the same printer; scan each printed page at 600 dpi on the Brother flatbed first (that scan is the truth for each page's real print scale); then on each page lay a new card and, separately, an old worn card, dark cards if he has them, and take the printer check's card photo in GroupLab Dev (or, if B and C cannot be read by the app, ordinary phone photos taken straight down in room light, which you then measure here); where to put the files and what to name them. When the files arrive, measure each card photo against its page's scan and report which outline reads most accurately and most consistently, new card and old, with the numbers, as a question answered in QUESTIONS-FOR-PLANNING. Keep A built until the result is in.

### 4. Question 66: yes, Alan will photograph a plain store-bought target

Keep the generated stand-in (entry 274) until then. Write a request for Alan with exactly what to shoot and photograph: a plain store-bought target that carries no one's design (a simple bullseye or square), five shots at any distance, photographed flat in good light, and, if convenient, scanned at 600 dpi too; where to put the files. When they arrive, mark it on the desktop's marking screen with the scale set by hand, as a new user would, and use that for "Your own targets" on the home page and the tour.

### 5. Eight features from the competitor study: all eight, on the desktop and the phone

Alan said yes to all of them. **Engine, data and file work can start now. Every new screen, overlay, card or report layout waits for planning's concepts and Alan's choice (the visual decisions rule).** Planning will draw them; do not invent layouts.

- **a. Marking with a fixed crosshair and a counting button, with Undo, markers at the bullet's real size and numbered.** Part of item 1; also for correcting a hole the detector missed or added.
- **b. Several aim points on one photograph**, for targets GroupLab did not print (a GroupLab sheet already knows its bulls): the data model, sessions, exports and figures per aim point, and pooling across them where the rules already allow it.
- **c. A switch on each shot to leave it out** (a flyer). The shot stays in the record, is shown as left out on the picture and in lists, every figure updates, and sessions, CSV and reports record which shots were left out and that they were. Nothing is ever deleted by the switch.
- **d. A results box on the picture** that can be moved and resized, with a label, saved as a full-size image (the phone's gallery in a GroupLab album; a file on the desktop) and shared. Mean radius first; units follow the tap-to-switch setting; one unit per kind throughout (never the competitors' mix of MOA, mil and bare numbers).
- **e. A dated PDF report** of a result: the picture, the figures, and a plot scaled so the group fills it (Ballistic-X's plot is a dot in a black square; do not repeat that).
- **f. A table of each shot's offset from the aim point**, across and up and down, with scope clicks beside them from the equipment's click value (and in the chosen unit when no scope is set).
- **g. The mean radius drawn as a circle** about the group's centre on the picture, switchable.
- **h. The group's offset handed to the ballistics and to Shots Needed to Zero**, so the zero correction comes straight from the result.

While here: Ballistic-X reports a "vertical SD" of 0.051 in for a five-shot group 0.524 in high, which cannot be a sample standard deviation. Check that every spread figure GroupLab shows is exactly what its name and explanation say (sample or population, per axis or radial), and add a test with a hand-worked five-shot example. PHONE-PARITY, the Features page, the user guide and the README change with each feature (entry 267's rule).

### 6. iOS: yes

Alan will enrol in the Apple Developer Program as an **individual** ($99 a year) from the Apple Developer app on his iPad mini (6th generation, iPadOS 26 and 27 supported). He has no Mac and no iPhone; the plan needs neither to start.

1. **Write `docs/IOS-PLAN.md` first**, with sizes: an Avalonia iOS head (bundle ID `org.grouplab.app`, iPhone and iPad, a minimum iOS version you justify); OpenCV built for iOS as a static framework with the same ArUco and AprilTag 36h11 detector the phone uses; a camera screen on AVFoundation with the same Capture B design, Guided and Manual, the picture score, and the torch (iOS allows a torch level while the camera runs); printing, sharing and files; the OLED idle and exit rules. List what carries over unchanged.
2. **Builds without a Mac:** the nightly workflow gains an iOS job on GitHub's `macos-26` runner (free for this public repository) with Xcode 26, which signs and uploads to TestFlight. Alan installs from TestFlight on the iPad mini; iPhone testing later through TestFlight invitations. Signing material and the App Store Connect API key are secrets Alan sets himself with `gh secret set`; neither you nor planning ever sees them.
3. **A request for Alan** (the next number) with the Apple steps after enrolment, in order, exact: the App Store Connect API key (Issuer ID, Key ID, the .p8 file) into secrets; the distribution certificate (made from a certificate request generated on Windows with openssl, so no Mac is needed) and its password into secrets; the app record in App Store Connect with the bundle ID. Each step says where to click and what "done" looks like.
4. **Documents:** iOS moves from "not planned" to "being built" everywhere (README, PLATFORM-SUPPORT, ANDROID.md, the home page's "Not built yet", the diagram, PHONE-PARITY). Alan's decision replaces the settled "Apple mobile" paragraph of entry 147 and question 68: rewrite it to say an iOS version is being built, tested on the iPad mini, built on GitHub's Mac machines, with his tone; change the pinned sentence in `MacBuildsTests` with it.
5. **Not yet:** no App Store submission and no change to LICENSE. Planning recommended a GPLv3 section 7 additional permission for App Store distribution before any outside contribution; Alan has not answered that. Check that no GPL code from other projects would enter the iOS build and note it in the plan.

### 7. The DESIGN NEEDED line

Change it to say the concepts are with planning and Alan: marking A redrawn with the fixed crosshair, and the screens for items 5a to 5h.

## 2026-09-28, entry 277: PRIORITY, before anything else: GroupLab Dev on the Fold 7 for the camera test, now

**Status: done 2026-09-28.** GroupLab Dev nightly 121 (the build with the camera screen and the recalibrated picture score) is on the Fold 7, installed from the nightly's own signed APK over the old 119, and it opens to Capture. The phone first showed offline and then vanished from wireless debugging; the line at the top of `for-alan.md` asked Alan to turn it off and on, and it came back without pairing again. A build made locally could not replace the nightly's, being version 1 against 119, so the nightly's asset is the one to install.

Alan is doing the camera test now (request 50, line 2 of for-alan.md). Before any other entry: install the newest GroupLab Dev (nightly 121 or later, whichever build carries the camera screen and the recalibrated picture score) on the Fold 7 over wireless debugging, the way you did for the earlier sittings. Only GroupLab Dev is touched; nothing else on the phone. If the phone does not answer adb, write one line at the top of for-alan.md saying exactly what Alan must do (for example "turn Wireless debugging off and on and read me the address and port"), and tell him in the panel. When it is installed and opens to Capture, say so in the panel in one line: "GroupLab Dev nightly N is on the Fold 7: go ahead with the camera test." Then continue with the rest of the inbox in number order.

## 2026-09-28, entry 276: answer to question 68 ("Apple mobile")

**Status: done 2026-09-28.** Question 68 closed as answered; the paragraph is listed under "settled" in `docs/RETIRED-WORDING.json`, which the audit's reader and `scripts/consistency.py` share, so it is not reported again.

Keep what you did. Alan's settled sentences stay word for word, and "iOS is not planned." in front of them is enough: the rest reads as what would happen if an iOS version were ever made, which agrees with `docs/ANDROID.md` and the README. Do not rewrite the paragraph or change the pinned sentence in `MacBuildsTests`. If Alan ever wants the paragraph reworded, he will say so and it will come as its own entry. Close question 68, and tell the next consistency audit's reader (the entry 267 rule) that this paragraph is settled wording, so it is not reported again: a line in the audit's notes, or wherever the weekly CI check keeps its exceptions.

## 2026-09-28, entry 275: consistency audit, 2026-09-28

**Status: done 2026-09-28.** All sixteen fixed in one change, the README, the site, the guides and the assets together, every new sentence backed. Two done differently, and why: section 1's three stops say in words that the phone has them since nightly 119, a new "pending" Mobile side the site build accepts until their pictures are taken at the next sitting; section 9's two lines are added to nightly 119 in RELEASE-NOTES.md with a line saying when and why, and its GitHub release is left as published. Section 9 also found the cause: `android/` was still classed as the spike, so a change to the Android application alone did not count as shipping; `android/GroupLab.Android/` now ships. Section 7's "Apple mobile" paragraph is Alan's settled wording, held literally by a test, so it keeps his sentences with "iOS is not planned." in front; question 68 asks whether to reword it. Not done: the phone pictures themselves, at the next device sitting.

The scheduled consistency audit of entry 267 section 2b. It read the README, the live site (home, /download/, /features/, /tour/,
/shoot-a-target/, /guides/, /releases/, /support/, fetched about 15:50 UTC), `website/features.json`, `website/tour.json`, the newest
release notes, STATE, for-alan, PLATFORM-SUPPORT, PHONE-PARITY, ANDROID, the testing guide, the inbox and the last five days of commits.
No GitHub issue labelled `consistency` exists yet. Nothing here needs Alan. Fix each in the same way as any change: the README, the site,
the guides and the assets together, with claims backing where a sentence is published.

### 1. The tour's Mobile side still calls three phone screens desktop only

- **Where:** `website/tour.json`, the `mobile` field of `analysis-open` (line 300), `compare` (line 418) and `ballistics` (line 524),
  shown at https://grouplab.org/tour/ with the Mobile switch, and on each stop's own page.
- **What it says:** "On the desktop only, for now." For `analysis-open` it adds that the phone shows the figures and the plot but not the
  explanations.
- **What it should say:** since nightly 119 the phone has every figure with its explanation, compare loads, and Ballistics as a fifth tab
  (docs/RELEASE-NOTES.md nightly 119; docs/PHONE-PARITY.md rows `why`, `compare`, `ballistics` read "on the phone"). Give each a phone
  screenshot at the next device sitting, and until then words saying it is on the phone since nightly 119. Check `equipment` too: if the
  phone's Ballistics tab lets you enter a rifle and load, that stop is not desktop only either.

### 2. The phone's pictures show the old capture screen and four tabs, under a caption that says "the current build"

- **Where:** `docs/figures/screens/phone/fold-capture-light.png` and `-dark.png` (nightly 115), used by `scripts/readme-images.py`
  (line 42) for the "Photograph it" tile of `docs/figures/readme/mosaic-*.png`, and by the tour's Capture stop. README.md line 29 says
  under the mosaic: "The pictures come from the current build."
- **What it shows:** the nightly 115 Capture form (caliber, distance, Take a picture, Choose a photograph) and a tab bar of four tabs.
- **What it should show:** nightly 119's Capture B over the live picture, Guided and Manual, the quality bar, and five tabs with Ballistics.
  The retake is already planned for the device sitting (entry 253 section 3). Until then README line 29 should not claim the current build
  for the phone tile, for example: "The desktop pictures come from the current build; the phone's from nightly 115, retaken at the next
  device sitting." Every phone screenshot on the site shows four tabs, so the same caveat applies to the Features and tour captions that
  do not already name nightly 115.

### 3. The download page's Android card is behind the build

- **Where:** `website/build.py` line 719, shown on https://grouplab.org/download/ under Android.
- **What it says:** "An early test build: it photographs or opens a sheet and reads it with the same engine as the desktop." and
  "Builds up to nightly 118 showed only the camera on the capture screen; the next build shows its words, shutter and Back".
- **What it should say:** nightly 119 is out, so "the next build" is now wrong: "Since nightly 119 the capture screen shows its words,
  shutter and Back over the picture", or drop the line. The description should match the README's Android row (README.md, the
  "Before you install" table): every figure with its explanation, the bulls you fired at, Shots Needed to Zero, compare loads,
  Ballistics, printing and sessions; marking by hand is not on the phone yet.

### 4. The minimums table on /download/ is printed as raw text

- **Where:** https://grouplab.org/download/, "Minimums". The HTML is one `<p>` holding `| | Operating system | Built and published |
  ...` with the pipes and the `|---|` row visible. Source: `docs/PLATFORM-SUPPORT.md` line 31 and the table around it, converted by
  `website/build.py`.
- **What it should be:** a real table, as it renders in the README. The converter used for the platform statement does not handle
  Markdown tables. Add a site build check that no published page contains a line starting with `|---` or `| |`.

### 5. "Sends nothing anywhere" contradicts three features

- **Where:** the footer of every page (`website/build.py` line 489): "The application keeps everything on your own computer and sends
  nothing anywhere." README.md, "Before you install": "It sends nothing anywhere, and an update check sends nothing about you."
  `docs/TESTING-GUIDE.md` line 5: "it keeps everything on your own machine".
- **Why it is wrong:** /features/ lists "Send a target to the project", "Error reports" ("automatically, after asking, or never") and
  "The hardware survey"; the tour's first run asks those three questions.
- **What it should say:** something true and short, for example "It keeps everything on your own computer and sends nothing you have not
  agreed to: targets, error reports and the survey each ask first." Keep it backed in `docs/claims-backing.json`.

### 6. The Features page says GroupLab updates itself on macOS and Linux

- **Where:** `website/features.json` line 612, key `updates`, platforms Windows, macOS, Linux; https://grouplab.org/features/#updates.
- **What it says:** "GroupLab updates itself, and the update bar lists every build you skipped".
- **What it should say:** only the Windows installer updates itself; the zip, the tarball and the Mac builds tell you and leave the
  download to you (the download page, "Updating", and README "Before you install" both say so). For example: "The Windows installer
  updates itself, and on every desktop build the update bar lists each build you skipped, newest first, with what each changed."

### 7. iOS and the phone's marker detector are described three different ways

- **Where and what:**
  - `docs/PLATFORM-SUPPORT.md` lines 75 to 77, "Apple mobile", shown in the README and on /download/: an iOS version "would be tested
    on" the iPad Mini and "cannot be produced at present" for want of a Mac.
  - `docs/ANDROID.md` section 1: "iOS is not planned. The iPad Mini is for testing the website only." README Phase 8 and License agree
    with ANDROID.md.
  - README.md line 555, the architecture diagram: `iOS planned`. Line 561: `libapriltag mobile, planned`.
  - README "Built with", Imaging row: "On mobile the marker detector is the AprilTag reference implementation under BSD-2-Clause,
    reached through P/Invoke." `docs/ANDROID.md` section 3 says the phone runs GroupLab's own OpenCV build (ArUco with the AprilTag
    36h11 dictionary, `libOpenCvSharpExtern.so`), and no Android or core source names libapriltag.
  - The home page, "Not built yet": "... hand marking on the phone · iOS".
- **What it should say:** "Apple mobile": iOS is not planned; the iPad Mini is used to test the website. The diagram: iOS "not planned",
  and the imaging backend as OpenCV on both desktop and phone (drop the libapriltag box, or mark it "not used"). The Imaging row: OpenCV
  through OpenCvSharp on the desktop and a GroupLab build of OpenCV on Android. The home page: drop iOS from "Not built yet", or say
  "not planned".

### 8. The testing guide's "What is not done yet" is out of date

- **Where:** `docs/TESTING-GUIDE.md` (and its PDF), linked from https://grouplab.org/guides/ and from /support/ step 01.
- **What it says:** line 80, "The Equipment screen is not built."; line 81, "Sending in sheets is coming. `grouplab.org/upload` is written
  ... the server is not installed yet, so the link does not work." "It keeps itself up to date" names only the zip and the Linux tarball.
  Android is not mentioned anywhere in it.
- **What it should say:** the Equipment screen exists (tour /tour/equipment/, README Phase 4 "Done. Records for rifles, barrels and
  loads"); sending works (https://grouplab.org/shoot-a-target/ has the upload, Features "Send a target" since nightly 102); the Mac builds
  also only tell you of a newer build; and a short Android part: the APK or the Play internal test, and what the phone does not do yet
  (marking by hand, CSV, large sheet advice, a picture shared in, from PHONE-PARITY). Regenerate the PDF.

### 9. Nightly 119's notes leave out two phone features it carries

- **Where:** `docs/RELEASE-NOTES.md`, 0.2.0-nightly.119, and so /releases/ and README "What is new".
- **What is missing:** the set of sheets as a checklist and the scan pill (entry 259 screens 6 and 7, commit `ffc5c21`, an ancestor of
  nightly 119's `9046087`). README "Status" lists both as new on the phone.
- **What it should say:** two more lines under nightly 119, for example "On the phone, a set of sheets is a checklist: the sheets read so
  far pooled into one group, and those still to read." and "On the phone, a scan says how large it was printed, and every size is
  corrected to real inches."

### 10. Two phase states in the README contradict their own items

- **Where:** README.md line 378, Phase 5 "**Not started**", while its items include four "Built, not proven" (chronograph strings by hand,
  the ballistic solver, load against load, hit probability at another distance). Phase 9 is "Not started" while `grouplab bench` is
  "Built, not proven".
- **What it should say:** "In progress" for both, by the README's own four states ("being built, not usable" does not fit either, so
  choose the nearest honest state and say why in one line). `DESIGN.md` section 21 changes with it, since `ReadmeTests` holds them equal.

### 11. Android is left off in the README's developer sections

- **Where:** README "Building": "Every nightly build is published for Windows, Linux and macOS". "Repository layout" has no row for
  `android/` or `website/`.
- **What it should say:** every nightly also publishes the Android APK and GroupLab Dev; add rows for `android/` (the Android shell and
  its OpenCV build) and `website/` (the site's source and build).

### 12. Hand marking on any target: said twice, and never said to be desktop only

- **Where:** README "How it works" opens with "Any target works ... mark the holes by hand" and repeats it at line 294 ("It also works on
  targets GroupLab did not print"). The home page's "Your own targets" and the tour index's "Your own targets" say the same with no
  platform.
- **What it should say:** keep one of the two README paragraphs, and add in each place that marking by hand is on the computer and coming
  to the phone (PHONE-PARITY `other-targets`: coming). The hero may keep "Photograph any target", since the phone photographs and the
  computer marks.

### 13. The home page's eyebrow names one platform

- **Where:** `website/build.py` line 568, https://grouplab.org/: "Free · open source · GPL-3.0 · Windows test build".
- **What it should say:** the same page's "What it is today" and README line 5 name Windows, macOS, Linux and Android. For example
  "Free · open source · GPL-3.0 · test builds for Windows, macOS, Linux and Android". The Windows download button can stay as it is.

### 14. The minimums name a Microsoft Store copy that does not exist

- **Where:** `docs/PLATFORM-SUPPORT.md` line 31, Windows: "version 1809 or later for the Microsoft Store copy", in the README and on
  /download/. The same section says "nothing is listed for a platform that has no published build", and the Store listing waits on
  request 38.
- **What it should say:** drop the Store clause until the Store copy is published, or write "for the Microsoft Store copy, once it is
  published".

### 15. The phone has no guide

- **Where:** https://grouplab.org/guides/: "Both guides describe the Windows application". `docs/USER-GUIDE.md` mentions the phone four
  times, only as a camera. The Android-only features on /features/ (Guided or Manual, Every picture checked, Print a sheet from the phone,
  and others) have no "In the user guide" link.
- **What it should be:** a phone part in the user guide, or a short phone guide beside the two, covering install (APK or Play test),
  Capture in Guided and Manual, reading the picture check, the result's figures, Sessions and compare, Ballistics and Targets. Link each
  Android feature to it.

### 16. STATE and for-alan disagree on the open requests

- **Where:** `docs/notes/STATE.md`: "Open requests ...: **7** (50 ...; 46 ...; 38 ...; then 9, 16 and 20)". `docs/notes/for-alan.md`:
  "**Open: 8**", with 52 and 33 besides. STATE's blocked list also says request 16 waits on "his name for a thanks", which entry 189
  answered (Fenix, thanked in the README).
- **What it should say:** the same count and list in both, and request 16 as the trackpad check only.

## 2026-09-28, entry 274: answers to questions 65 and 66 (question 67 is with Alan)

**Status: done 2026-09-28.** Question 65: entry 261's wording corrected in this log. Question 66: the home page's "Your own targets", the tour index and the marking stop show a sample target GroupLab draws itself, marked by hand, labelled as a sample (`marking-other`, made by the screenshot walk); the Features entry uses it. Question 67 stays with Alan; nothing waits on it.

**Question 65, where the detection scoreboard lives.** `tools/` stays read only, and CLAUDE.md is not amended. When the scoreboard is built (the study's recommendation), it lives where everything else Code writes lives: the scoring code and its fixtures in the test projects, so it runs with every build like any other test, and any command-line runner or report script under `scripts/`. What you did for the study (a scratch test, deleted once its numbers were in `docs/DETECTION-LEARNING-STUDY.md`, with its conditions listed) was right. Correct entry 261's wording in the log so it no longer names `tools/study/`.

**Question 66, the "Your own targets" picture.** Do not use the OnTarget scans on the home page or in the tour, not even cropped. Use (c) now: a generated "other target" drawn by GroupLab itself, plainly not a GroupLab sheet (no tags, no QR code, a simple aim point and plain rings or a square), shot and marked on the desktop's marking screen, with the scale set by hand, as a real user would. Label it as a sample. Option (b), Alan's own photograph of a plain store-bought target, replaces it later if he takes one; I am asking him in chat and will pass on his answer. Keep the screenshot job and the Features page in step (entry 267's rule).

**Question 67, the card outline's 3 mm gap.** A visual change from an approved drawing, so it goes to Alan (standing rule). I have drawn three options on the printer check canvas (A the gap as built, B a hairline on the card's edge, C corner marks only) and recommended A. Keep what you built; I will send his answer as its own entry. Nothing waits on it.

**DESIGN NEEDED in for-alan.md (entry 258's two phone screens).** Concepts are with Alan on the phone parity canvas, rows 8 and 9: marking a target GroupLab did not print (A, one step at a time: scale, bull, holes; B, one screen with Scale, Bull, Holes and Template tools), and importing shots from a CSV file (A, the file as a table with a role over each column; B, GroupLab guesses the columns, unit and origin and shows the group to check). In both marking ideas the holes are found automatically and the scale is set by hand. Do not build either screen until his choice arrives as its own entry. Change the DESIGN NEEDED line to say the concepts are with Alan.

## 2026-09-28, entry 273: Alan approved the printer check and unit-tap concepts. Build them as drawn

**Status: done 2026-09-28, in part.** Built as drawn: the check page (grid style 4, question 67), the three wizard screens on both platforms with the phone's camera looking for the card, Printers in Settings, the offers at first run and first print, the paper-edge check and the line on every photo, and tap to switch units with its note, press and hold, and the one-time hint. Not done: numbers inside sentences (Shots Needed to Zero's size, the hit chance's prose) and the desktop's full table, whose cells are bare numbers under unit headings, are not tappable; the phone's Ballistics, Compare and Shots Needed to Zero follow a change but their numbers are not yet tappable; the card photo and the phone screens wait for a device sitting; question 67 asks about the outline's 3 mm gap.

Alan: "I like the concepts. Go ahead and implement them." The canvas is claude.ai/artifact/ECbWJdj8VcYwpv8gd9Appc. This releases the hold in entries 271 and 272. Build:

1. **Tap to switch units,** as in the demo board:
   - Tapping any angular number switches MOA and mil everywhere. Tapping any size on paper switches inches and cm. Tapping a distance switches yards and meters.
   - A short "Angles now in mil everywhere · remembered" note appears at the bottom.
   - Labels keep their dotted underline and tap-to-explain.
   - Press and hold (right-click on the desktop) lists every unit.
   - A one-time hint card reads "Tap a number to switch units".
   - It uses the same setting as Settings and the unit switches. Desktop, phone, reports, Compare, Ballistics and Shots Needed to Zero all follow it.
2. **The Scale check page,** as drawn: Letter, and A4 for A4 locales.
   - The title and "Print at Actual size (100%). Never Fit to page."
   - A code naming the page (GL-SCALE-LTR-1, and an A4 equivalent) and tags.
   - A card outline, 85.60 by 53.98 mm, with "Lay any bank, gift or ID card here".
   - Three crosshairs in an L, 150.00 mm center to center across and down, labeled in mm and inches.
   - Two ruler lines, 250.0 mm down the side and 190.0 mm across the bottom (adjust to fit A4 and label to match).
   - The four numbered instructions.
   The page is built from a definition in the library like any sheet, so it reads itself from a photo or scan.
3. **The wizard,** three screens as drawn:
   - **Pick a method:** name the printer, Print the check page, then choose Card photo, Digital caliper, Ruler or tape, or Scanner. Next, or "Skip for now, run it later from Settings".
   - **The card photo:** Capture B's style, with live checks for card edges, printed outline, tags and focus, and the automatic shutter.
   - **The result:** across and down percentages with their uncertainty, what it means for a group, the paper-edge check agreeing or not, and "Save and finish" or "Check again another way".
   For the caliper and ruler methods, the middle screen is two number fields with a unit choice and a picture of where to measure.
   When it appears: offered at first run (skippable), offered again the first time a sheet is printed, and always in Settings, under Printers.
4. **Settings, under Printers:** the list of named printers with their factors and dates, a default, Check again, Add a printer, Delete, and a switch to turn correction off.
5. **The paper-edge check on every photo,** and the line on each photo result: "Corrected for My printer, 99.2 by 99.4%", or "Measured in the sheet's own inches" with a link to run the check.

The screenshot job, the Features page (one entry for the printer check and one for tap to switch units, each with its own picture), the user guide, the tour and the README all get updated in the same change (entry 267's rule).

## 2026-09-28, entry 272: Alan's choices for checking the print scale, and switching units by tapping a number

**Status: done 2026-09-28, with entry 273,** which released its build. The card, caliper, ruler and scan methods, the thickness correction, the result's words and the paper-edge check are in; the coin is left out as Alan chose. Not done: a card photographed for real, which waits for Alan's first check.

### 1. The printer check (follows entry 271; the study is in the planning project, `print-scale-study.md`)

Alan took every recommendation:

1. **Methods at launch:** card photo, digital caliper, ruler or tape, and scan. The coin is left out.
2. **When the wizard appears:** offered at first run (skippable), offered again the first time a sheet is printed, and always in Settings.
3. **Profiles:** named printer profiles with one default ("My printer").
4. **The paper-edge check on every photo: yes.** Warn when it disagrees with the profile by more than about 1.5 percent, or when there is no profile and the sheet looks fit-to-page.

The build:
- **The Scale check page,** one Letter (A4 where the locale uses it) sheet with:
  - a card outline, 85.60 by 53.98 mm;
  - two caliper crosshair pairs, across and down, 150.00 mm apart between centres;
  - ruler lines both ways, as long as the page allows, with their designed lengths printed in mm and inches;
  - tags, a code naming the page, and "Print at Actual size (100 percent), never Fit to page".
- **The card photo:** detect the card's edges and the printed outline in one photo, correct for the card's thickness (0.76 mm, using the tag model's camera distance), and measure both directions. Report the uncertainty. Card size tolerance: unused cards 85.47 to 85.72 by 53.92 to 54.03 mm, worn cards within about 0.3 percent.
- **Caliper and ruler:** type the measured lengths, with the unit chosen.
- **Scan:** the existing scan path saves the measured scale to a profile.
- **The result:** for example "My printer prints at 99.2% across and 99.4% down (plus or minus 0.3%)". Save it, use it for every photo, and show it in one line on each result.
- **Design:** planning is making concepts of the check page and the wizard screens now. Build the measuring logic first and the screens once Alan has chosen.

### 2. Switch units by tapping a number (Alan)

"We need to find a way to make it easier to switch any value presented between moa and mil or inches to centimeters. Maybe if you click on the value, it switches and remembers that."

- **Tap (or click) any angular value to switch MOA and mil. Tap any length to switch inches and cm.** Every value of that kind switches together, everywhere, and the choice is remembered. It is the same setting as the unit switches and Settings, so they always agree.
- A value shown as both (an angle with its size on paper beneath) switches the part tapped.
- **Long-press on the phone, or right-click on the desktop,** shows every unit the value can take (MOA, mil, IPHY where offered, in, cm, mm) so nothing is out of reach.
- It must stay discoverable and not collide with tap-to-explain (entry 259, which opens a figure's explanation from its label). The value switches units; the label explains. Show a short hint the first time a result appears ("Tap a number to switch units"), and keep the dotted underline on labels only.
- Desktop, phone, reports, Compare, Ballistics and Shots Needed to Zero all follow it. Reports and exports state their units in their headers.
- Planning's concept canvas includes a working demo of this. Build it now. Alan may adjust the details after seeing the demo.

## 2026-09-28, entry 271: real inches on photographs too (the print scale, measured once and remembered)

**Status: done 2026-09-28** (3a1e70b), sections 1, 2, 4 and 5; section 3 is written up as a study. The wizard, the check page and card detection were held by the entry and are released by entries 272 and 273.

Alan: "I was under the impression that the photos could also check the scale of the prints. I thought this was a design requirement."
DESIGN.md's "Print scale verification" still says that because the fiducials are at known coordinates, GroupLab detects the print scale
"automatically". That is true for a scan only. WHAT-CAN-BE-MEASURED.md (entry 171) explains why a photograph cannot: with no absolute
ruler in the frame, a sheet printed small is indistinguishable from a full-size sheet a little farther away. Fix the wording, and then
close the gap as far as physics allows, with as little work for the shooter as possible.

1. **Correct DESIGN.md** (and anything else that says photos measure the print scale) to match WHAT-CAN-BE-MEASURED.md: the tags measure
   the sheet's shape (perspective, curl, lens) on any picture, and its absolute size on a scan.
2. **A printer profile, measured once and applied from then on.** Print scale is a property of a printer and its settings, and it is
   stable. So:
   - When a **scan** of a GroupLab sheet measures its print scale, offer to save it: "Your printer printed this sheet at 99.2 percent.
     Use this for photos of sheets from the same printer?" The person names the printer (default: "My printer").
   - Or, with no scanner, **type one ruler measurement** (bull 1 to bull 5, the distance printed on the sheet) once, on the desktop or the
     phone, and save it the same way.
   - After that, **photographs are corrected automatically** using the chosen printer profile (a setting, with the last one used as the
     default), and the result says so in one line: "Corrected for My printer's 99.2 percent, measured from a scan on 28 September." With
     no profile, the existing line stays ("measured in the sheet's own inches").
   - A per-sheet override: on any photo, "Measure this sheet with a ruler" corrects just that one.
   - Profiles travel with sessions and between phone and desktop (the session file carries the factor used). Record the method in the
     saved marking, as today.
3. **A known object in the frame, as a study (add to entry 261's study, no build yet):** could a credit card sized card (ISO/IEC 7810 ID-1,
   85.60 by 53.98 mm, a size every wallet has) laid flat on the sheet give the absolute scale from a photo alone, to better than about
   0.5 percent at phone resolutions, given its rounded corners and edge contrast? Measure it on real photos before promising anything.
   Phone depth estimates and autofocus distance are not accurate enough (percent-level at best) and are not to be used for scale.
4. **Tell users plainly** in the tour, the user guide and the capture screen: for real inches from a photo, scan one sheet or measure one
   ruler distance once per printer; GroupLab remembers it.
5. Tests: a synthetic sheet printed at 96.2 percent, photographed, reads true size once the profile is applied; a missing profile reads
   in sheet inches with the line; a profile from a scan and from a ruler agree within their stated uncertainty.

**Update, same day:** Alan asked planning to study easy ways for an average person to verify the print scale: a first-run wizard,
also reachable from Settings, that prints a check page with outlines of known objects (a credit card, a coin) and marks for a ruler or a
digital caliper. The options are with Alan for decision. **Build now:** the printer profile plumbing (items 1, 2's storage and the
automatic correction of photos, 4's wording, 5's tests), and the scan and typed-ruler paths. **Hold** the wizard, the check page and
any card or coin detection until a later entry brings Alan's choices.

## 2026-09-28, entry 270: say plainly, everywhere, that GroupLab works on any target, not only its own sheets

**Status: in part 2026-09-28** (3693025). Not done: section 3's picture of a commercial target marked by hand, which waits on question 66.

Feedback from the reloading Discord, after Alan's announcement: "The whole 'print a target sheet' bit is gonna be a barrier to entry that most people won't bother with. When your competitors can do it from just a picture, why bother?" Alan: this person did not understand that GroupLab works with non-GroupLab targets, and that needs emphasis here and on the site.

The README, the site's home page, the Features page and the tour all lead with "print a GroupLab sheet". That reads as a requirement. Change the message to:

- **GroupLab works on any target.** Photograph or scan whatever you shot on. You set the scale once, then mark the holes by hand today. **Automatic hole detection on any target is the goal** (entry 261, section 7), so say it as a goal, not as a feature.
- **A GroupLab sheet is the fast lane, not a requirement.** On its own printed sheets everything is automatic (scale, every hole, which bull each shot belongs to), and one shot per bull gives large groups.

Changes:
1. **The pitch** in the README (entry 266) and on the home page, for example: "Photograph any target. GroupLab measures the group and tells you honestly what the size is worth. Print a GroupLab sheet and it does everything by itself." Keep it to two short sentences.
2. **The README's three captions under the product shot:** caption 1 names both paths, any target or a GroupLab sheet. The mosaic's first tile reads "Any target, or print a sheet".
3. **The home page and the tour:** a clear "Your own targets" section near the top, with a picture of a commercial target marked by hand (the desktop's marking screen on a non-GroupLab target). Once the phone can do it (entry 258), use the phone too.
4. **The Features page:** move "Targets GroupLab did not print" into the first group, and rename it to something plainer, such as "Any target you already shoot".
5. **The mission,** in one line wherever the pitch is expanded (README "Why it exists", home page): GroupLab also aims to help shooters think in mean radius and confidence rather than extreme spread from a few shots.
6. The consistency audit (entry 267) now also checks that no page implies GroupLab sheets are required.

## 2026-09-28, entry 269: answer to DESIGN NEEDED, Shots Needed to Zero colors

**Status: done 2026-09-28** (246f6c5).

Alan chose **option 1** (the parity canvas, board "ZeroColors"): **"Within 1 click" is amber everywhere, and "Closest click" is teal everywhere**, on the phone and on the desktop. Change the desktop chart to match: the within-1-click line becomes amber and the closest-click line teal, with the legend and table row names written as they are now. Update the website's pictures and the Features page entry through the screenshot job. Remove the question from `for-alan.md`.

## 2026-09-28, entry 268: both devices are on the charger for the night (read now, with entry 263)

**Status: followed 2026-09-28.** GroupLab Dev's black idle screen (f2b1b81) was shown whenever a device was not in use. The Tab S8 Ultra did not answer adb all night.

Alan, about 11:20 UTC (05:20 his time): he is putting the Tab S8 Ultra on the charger now, with Wireless debugging and Stay awake on. The Fold 7 goes on a charging cable with the same settings while he sleeps. If a device is not reachable at first, it should be shortly, so retry every 10 to 15 minutes for the first hour before marking it skipped.

- Use both overnight for the unattended work in entry 263: checking entry 260's camera fix with an adb screenshot or UI dump on the Fold 7 (front and inner screens only if it is unfolded; do not ask), entry 253's phone and tablet screenshots, entry 262's torch-strength readings, and entry 258 and 259's screens as they land.
- Nothing needs Alan tonight. The camera test with the printed sheet waits for the morning. Put its steps at the top of `for-alan.md`.
- The usual device rules: touch only GroupLab and GroupLab Dev, never read notifications, put every changed setting back, and do not unlock or change the lock screen. If a device locks, note it and move on.

### OLED screens: keep them dark whenever they are not in use (Alan, standing rule)

Alan: "both my phone and tablet have OLED screens and I don't like keeping them on at the risk of burn in. I have the brightness on my tablet set to the absolute minimum and will do the same to my phone before I go to bed, but as a general rule, it makes me uncomfortable leaving my screen on constantly."

This applies to every device sitting, not only tonight:
- **A black idle screen.** Add to GroupLab Dev a full-screen, pure black idle view with no text, no status bar and no navigation bar (immersive mode). On an OLED screen, black pixels are switched off, so nothing can burn in, and the device stays awake and unlocked for adb. Whenever Code is not actively driving a device for more than about a minute, bring up that view over adb, for example with an intent extra to GroupLab Dev, the way the existing test extras work.
- **Keep the screen on only for work.** Batch device work so the screen shows real content for as short a time as possible. Never leave a static screen (a result, Settings, the camera) on while waiting.
- **Brightness:** do not raise it. Take screenshots over adb, which do not depend on brightness. If a camera test needs light, that is a morning task with Alan anyway.
- **At the end of the night, or when device work is done,** leave the black idle view up and write in `for-alan.md` that the devices can be picked up. Do not turn off Stay awake or lock the device, since that would stop the next session from reaching it. Alan turns Stay awake off in the morning.
- Record the rule in `docs/ANDROID.md` under the device-testing section, and in the device scripts.
- **Alan must always be able to leave it easily.** Use ordinary immersive mode only, never screen pinning, kiosk or lock-task mode, and never block Home, Back or Recents. A swipe up from the bottom (Home) or the Back gesture closes it as with any app. A single tap anywhere shows, for a few seconds, one dim line of text, "GroupLab Dev idle screen, used for overnight testing", and a Close button at least 48 dp. Tapping Close ends it. Test on both devices that each of these works.

## 2026-09-28, entry 267: no lawyer review, and a standing consistency audit of the README, the site and every asset

**Status: done 2026-09-28** (dac0f4b): no lawyer's review; scripts/consistency.py runs in CI and weekly, and opens an issue on a finding.

### 1. No lawyer, for now

Alan: "I am not going to get a lawyer's review unless this application really takes off. As of right now, I am not willing to pay a lawyer hundreds or thousands of dollars for this."

- **Take out every claim that something is "with a lawyer" or "waits on the attorney"**: README (License, Planned), `docs/ANDROID.md` (line 25, the public Play listing), `DESIGN.md`, `docs/CLAIMS.md` and `docs/claims-backing.json`, STATE.md, the site, and anywhere else the search finds (`grep -rni "lawyer\|attorney"`). Leave the planning history and the archive as they are.
- **The public Google Play listing is no longer blocked by a lawyer.** Its remaining gate is Google's own rule for new personal accounts: a closed test with at least 12 testers for 14 days. Record that in ANDROID.md as the path to a public listing.
- **The GPL section 7 app-store permission** concerns Apple's App Store, and iOS is not planned. Record it as "not in force, and not pursued now; it would be revisited only before an iOS release". Keep one plain sentence on the reason in the License section, without any mention of a lawyer.
- **The effect on outside contributions:** adding such a permission later needs every copyright holder's agreement. In CONTRIBUTING.md, say plainly that contributions are accepted under GPL-3.0 and may later be offered under an added app-store permission, and ask contributors to agree to that when they open a pull request. Keep it one or two sentences.
- The patent and trademark searches (docs/PATENT-SEARCH.md, TRADEMARK-SEARCH.md) stay as they are; only change a line that says a lawyer is reviewing something now.

### 2. The consistency audit, as a standing rule

Alan: "the github readme, grouplab.org website and all of the assets need to be checked every so often to make sure they are up to date and agree with what is currently happening in the project. I should not have to find these oversights because they should be analyzed. It does not have to happen with every build, but it should at least happen once every few days or a week."

Two layers, both automatic:

**(a) The mechanical checks, in CI, weekly, plus on demand.** Add a scheduled workflow (for example weekly, on Monday at 13:07 UTC) that runs a consistency script, and opens or updates one GitHub issue labelled `consistency` listing every finding. It closes the issue when there are none. It checks:
- every platform with a published build is offered on /download/, in the README and in PLATFORM-SUPPORT.md (entry 265);
- the version, date and commit named in the README, on the site and in the release notes agree with the newest nightly;
- counted facts agree everywhere, taken from the source, not typed: sheets in the library (`<!--count-->` markers), platforms, features;
- every feature in `features.json` appears on the Features page, in the README summary and in PHONE-PARITY.md with a phone status;
- every screenshot and generated image is current (entry 253's stale check), including the README's two new images (entry 266) and the donor pack (entry 264);
- every link on the site and in the README resolves (internal links always; external links, allowing for temporary failures);
- the claims in CLAIMS.md still have their backing (claims-backing.json);
- no retired wording remains, from a list kept in the repository: for now "lawyer", "attorney", and any feature named as missing that has since shipped, such as "marking a target by hand is not on the phone yet" once it ships;
- the site's banned-term and IP-address checks.

The script also runs in the ordinary build as a warning, so it can be run locally at any time.

**(b) The judgement review, every three days.** A scheduled planning session (set up by planning, not Code) reads the README, the live site, the release notes, STATE.md, for-alan.md, the inbox and recent commits. It looks for what a script cannot see: a page describing something that has changed, a promise that no longer holds, wording that contradicts a recent decision, a missing mention of something new. It writes its findings as a numbered inbox entry for Code, and tells Alan only when something needs him. Code actions those entries like any other.

**The rule, from now on:** whenever Code changes behaviour, a platform, a decision or a user-facing name, it updates the README, the site, the guides and the assets in the same change, or lists why not in the commit. The audits catch whatever slips through; they do not replace doing it.

## 2026-09-28, entry 266: the new README, as Alan chose it

**Status: done 2026-09-28** (f1d775e). Entry 270 then changed its pitch and first caption.

Alan chose, on the canvas claude.ai/artifact/KeUwJotHu9SvkkGUPoPBSJ (boards "Chosen: B's picture over C's mosaic" and its light-theme twin): **design B's product shot at the top, with design C's six-screen mosaic under it**, and the new order. Build it so the README stays fully automatic.

**Approved by Alan (2026-09-28): "I like the new design. Lets go with it."** One change: **take out the "What GroupLab is not" section.** Alan: "It comes off kind of mean." It is not moved into a fold either. The rules it stated still hold for the project: no OnTarget compatibility, no pseudoscience, not commercial, not an electronic target system. Keep them where contributors read them (CONTRIBUTING.md or DESIGN.md), worded as what GroupLab does rather than as what it is not, and keep the site build's banned-term check. Check grouplab.org for a similar "is not" section, and soften or remove it the same way.

**The top, in order:**
1. `# GroupLab`, then the pitch in bold: "Print a target, shoot it, photograph it. GroupLab measures every hole and tells you honestly what your group size is worth." Then one line: free and open source under GPL-3.0, no account, no ads, no paid tier, on Windows, macOS, Linux and Android. (Keep "GroupLab is a working name" somewhere lower down, for example in License.)
2. Badges: license, the newest nightly, the tests, platforms, and Discord. Use shields.io or GitHub's own badges; the nightly badge must update itself.
3. **The product shot (B),** one image made by the screenshot job:
   - a shot GroupLab sheet (the published sample scan), tilted, behind;
   - the desktop analysis screen in a window;
   - the phone result overlapping at the right;
   - numbered amber callouts 1, 2 and 3.
   Make a dark and a light version (a `<picture>` with `prefers-color-scheme` sources), and alt text that describes it.
4. Under it, a **markdown table** of the three numbered captions, so the captions are real text:
   - 1, print and shoot a GroupLab sheet;
   - 2, on the computer, every hole found, then the group with honest ranges;
   - 3, on the phone, photograph it at the range.
5. **The mosaic (C),** one image, 3 by 2 tiles with their captions inside the image: Print a sheet, Photograph it, Every hole found, Honest numbers, Compare loads, Ballistics and hit chance. Dark and light versions again. Under it, one centred line saying the pictures come from the current build, with a link to the tour.
6. One centred line of links: Download · Website · Features · Tour · User guide · Discord.

**Then the sections, in order:**
- **What it does:** three columns (Reads targets, Honest statistics, Prints sheets), a few lines each, generated from `features.json` groups, with a link to the Features page. This replaces the long link list.
- **Download:** the nightly's number and date, and a single table by platform: Windows (installer, zip), macOS (Apple silicon, Intel), Linux (tarball), Android (APK, Play test). The long notes go into a `<details>` titled "Before you install": unsigned builds, the Mac quarantine command in a code block, the samples, where it keeps files, and updates.
- **What is new:** in `<details>`, generated as today.
- **What is supported, and what is not:** in `<details>`, generated as today.
- **Why it exists:** two short paragraphs.
- **How it works:** the flow, then one line on targets GroupLab did not print.
- **Status and plan:** Status, Planned and Deferred, each in `<details>`, generated as today.
- **For developers:** Built with, Architecture, Building, Repository layout and Test data, each in `<details>`.
- **Thanks:** Unholy, Jylee (Shots Needed to Zero, and helped choose the C3 grid, with Unholy) and Fenix, by the names they gave.
- **License.**

**Rules:**
- Nothing else is deleted: apart from "What GroupLab is not", every sentence of today's README lands in a fold, or on the page a fold links to.
- Keep every generated marker and `scripts/readme.py`. Blank lines inside `<details>` let GitHub render markdown there.
- Both images come from the screenshot job and join entry 253's stale check.
- Render the README with GitHub's markdown (for example `gh api markdown`) in both themes before committing, and look at it.
- The website's home page may borrow the two images later; that is a separate decision.

## 2026-09-28, entry 265: the download page on grouplab.org is missing Android

**Status: done 2026-09-28** (0a81418): Android is on /download/, and the site build fails when a published platform has no download. Section 4 is confirmed with the push that carries it.

Alan noticed that grouplab.org/download/ does not offer the Android build. The Android decisions (entries 198 and 199) said the nightly APK goes on GitHub **and grouplab.org**. So this is an omission, not a decision. The page mentions Android only in the minimums table, and its description still says "for Windows, Linux or macOS".

1. Add Android to /download/ beside the desktop builds:
   - the signed APK (`grouplab-android.apk`), with its minimums (Android 10 or later, arm64, 4 GB);
   - how to install it (allow installing from the browser, and remove any Play copy first, since the keys differ);
   - the Google Play internal test, by invitation, with how to ask for an invitation (the Discord);
   - GroupLab Dev for testers.
   Mark it as an early test build, and state the known camera problem plainly until entry 260's fix ships. Once it ships, the note comes out.
2. Update the page description and meta text, the home page's download button text if it names platforms, and anywhere else on the site that lists only three platforms.
3. Add a site build check that every platform in `docs/PLATFORM-SUPPORT.md` with a published build has a download on /download/, so a platform cannot go missing again.
4. Publish the site as usual and confirm it is live.

## 2026-09-28, entry 264: the donor pack and the Shoot a target page get the new sheets

**Status: done 2026-09-28** (bde8f6b, 1009cd2): the donor pack is one sheet of each style, built from the library, and /shoot-a-target/ offers each sheet or the whole pack.

Alan: "the donor page of the grouplab website may need updating with the new targets, as the default ones were hard to use. Maybe one of each?"

The donor pack (`website/donor/`, shown on /shoot-a-target/) still offers only GL-CF25-LTR and GL-CF25-LTR-D, which Alan and his testers found hard to use.

1. **One of each current style.** Offer one sheet of each style a donor might shoot:
   - the E bull sheet;
   - the C bull sheet;
   - the C bull with the centre dot, if it is a separate sheet;
   - the C3 zeroing grid, mil at 100 yd and MOA at 100 yd (and 100 m where it exists);
   - the standard 5x5 load sheet;
   - Made for your optic, as a link to the Targets screen and the website's generator, since it is made for each person.
   Letter first, A4 beside it. Each sheet gets a one-line description of when to use it, and a picture of the sheet itself (entry 256's rule), not a screen.
2. **Built, not copied.** Generate the donor PDFs from the target library in the site build, as the other sheets are, so a redrawn sheet updates the pack by itself. Add the pack to entry 253's stale check, so the site refuses to publish a pack that no longer matches the library.
3. **The instructions.** Update `docs/VOLUNTEER-PACK.md` and the instructions PDF:
   - how to check the printed size for each sheet style, since the bull 1 to bull 5 measure does not fit a zeroing grid;
   - the photo guidance as it will be after entry 260 (Guided or Manual, the torch, and whatever the check says);
   - what to write on each sheet.
   Keep "Submitting means following the terms on that page."
4. **The page.** On /shoot-a-target/, let the donor download the whole pack or any single sheet. List which sheet is most useful to the project right now. For example, if the corpus lacks photos of zeroing grids or of the C bull, say so, and take the list from entry 261's study once it exists.

Tell Alan when it is live, with the page link.

## 2026-09-28, entry 263: overnight rules, read now, before continuing entry 260

**Status: followed through the night of 2026-09-28.** The morning summary is at the top of for-alan.md.

Alan is going to bed (about 04:45 his time, 10:45 UTC) and wants Code busy all night without waiting on him.

1. **Never stop to wait for Alan overnight.** When a step needs him (a camera test with the sheet, a decision, a secret), write it at the top of `for-alan.md` for the morning. Then **move straight on** to the next piece of work that does not need him. Where a DESIGN NEEDED question comes up, write the line and keep building the parts it does not affect.
2. **The order:**
   - Entry 260 part 1 and the checking logic of part 2. Verify over adb, with no one present, that the words, shutter and Back show over the live preview on every screen and orientation available.
   - Then entry 259's screens, starting with the shared code of entry 258.
   - Then entry 261's study.
   - Then entry 262's device measurements (reading the torch strength characteristics needs no person).
   - Entries 253, 255 and 257's remaining device work wherever it can run unattended.
   Ship nightlies as usual.
3. **Devices overnight:** Alan will say whether he leaves them on the charger, unlocked, with Stay awake and Wireless debugging on. If a device is not reachable, skip its steps and note it. Touch only GroupLab and GroupLab Dev, never read notifications, and put every changed setting back.
4. **The morning summary:** at the top of `for-alan.md`, in five lines or fewer: what was finished, what is waiting on Alan (with the camera test steps if the fix is in), and what is next.

## 2026-09-28, entry 262: torch brightness, a bracketed burst, and combining frames (investigate, then build what the phones support)

**Status: in part 2026-09-28.** Step 1 on the Fold 7 only: Android 16, strength levels 1 to 5, default 1 (docs/ANDROID.md section 16). Not done: the Tab S8 Ultra, which did not answer; setting the level during a session and timing the settling; steps 2 to 4.

After entry 260. Alan: "Can the application adjust the flash's brightness? Can it progressively increase the brightness of the flash and take a burst of photos at different brightnesses and then try and composite them together or do some post processing to get the best image possible?"

**What the platform allows (planning's reading; confirm on the devices):**

- Android 13 added torch strength levels (`CameraManager.turnOnTorchWithStrengthLevel`). That call cannot be used while the camera is open, and CameraX always has it open.
- Android 15 added `CaptureRequest.FLASH_STRENGTH_LEVEL`, which sets the torch strength **during** a camera session. It is reached through Camera2Interop, or through CameraX 1.5's own torch strength API.
- It is optional for phone makers. A device supports it only if `FLASH_INFO_STRENGTH_MAXIMUM_LEVEL` is greater than 1.
- GroupLab supports Android 10 and later, so on older or unsupported phones the torch is simply on or off.

**Step 1, measure, at the next device sitting:**

- Read and record `FLASH_INFO_STRENGTH_MAXIMUM_LEVEL` and the default level on the Fold 7 and the Tab S8 Ultra, with each device's Android version.
- Check whether setting the level during a session works on each.
- Time how long exposure takes to settle after each torch change.

Record the results in `docs/ANDROID.md`.

**Step 2, a bracketed burst, where supported:** one press takes a short series. For example: torch off, then low, medium and high, or as many levels as the device offers, each after exposure settles. It must stay short enough that a hand-held phone does not drift badly. Because the sheet carries its own tags, every frame can be aligned exactly to the others, so small movement between frames does not matter.

**Step 3, choose or combine. Measure each against the corpus and real photos before shipping:**

- **(a) Choose the best single frame** by the quality score (entry 260). This is the simplest option and keeps the measurement on one real exposure.
- **(b) Combine per region:** for each area of the sheet, take the frame with the least shadow and no glare, after aligning them all on the tags.
- **(c) Flash and no-flash processing:** use the torch frame to find and remove the shadows cast by the phone and hand, and the no-torch frame to find and remove glare.

Hole edges and centres must not move because of combining. Measure the centre error of (b) and (c) against (a). Ship a combined image only where it is measured to be at least as accurate, and record which method made each picture in the capture record.

**Step 4, the setting:** under Torch, Auto uses the burst when the light is dim or there are shadows. On and Off stay available. The panel says what was done, for example "Torch at 3 levels, best frame kept".

If the devices do not support strength levels, report that. Fall back to a torch-on and torch-off pair, which works on every phone with a torch, and keep steps 3 and 4 for that pair.

