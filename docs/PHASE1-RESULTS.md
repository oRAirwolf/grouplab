# Phase 1 results

**Brief** `docs/PHASE1-BRIEF.md`
**Branch** `phase-1`
**Reproduce** each table with the command named above it

---

## Where the Phase 1 gates stand

Stated plainly, `docs/NOTES-FROM-PLANNING.md` entry 33 section 5, so that "not yet measured" is never read as "passed". The gates are `docs/PHASE1-BRIEF.md` section 2's.

| Gate | Threshold | Status |
|---|---|---|
| Conformance test 43 | 0.001 in worst bull on a synthetic raster | **Met**, and checked on every test run. Must not regress |
| Paper gate | 0.005 in worst bull on the ten printed sheets | **Met.** Ten of ten through the selected model, worst bull 0.00319 in (M1.5) |
| Photograph gate, flat | 0.005 in worst bull on `main_flat1-3` | **Not met.** The flat frames nearly pass: the frame that decoded every marker is inside on every scoring bull, and the failures are named (M1.11) |
| Photograph gate, mounted | 0.005 in worst bull on the seven usable pinned frames | **Not met: 0 of 7**, and an open requirement (M1.11). It cannot be settled until mounted GroupLab sheets are photographed, which is this weekend's paper session |
| Hole detection, point 1 | At least 25 of 27 on `300_nm_hand_load` with zero false positives | **Met,** as a reproduction of the verified survey result rather than a fresh position check (M2.1) |
| Hole detection, point 2 | 99 percent of holes with no false positives on synthetic sheets, matched at 0.15 in | **Not met,** under the amended tolerance too (M2.2) |
| Hole detection, point 3 | Centre accuracy against truth | Reported, not gated |
| Statistics | `docs/STATISTICS.md` section 15.5 | **Met on points 1 and 3 to 6**: 45,476 keys compared with nothing pending ("Entry 28"), coverage 94.73 percent, the Monte Carlo table within tolerance on all 490 cells. **Point 2 is met on the shots and not on the series**, because the two fixtures group shot 242 differently |

**Not a brief gate, and new:** the whole path from image to group now runs as one command and matches synthetic truth ("Entry 33" below).

**Not a brief gate, measured for the first time: the Phase 0 gate record on Linux and macOS**, entry 32 section 3.
- **Windows** reproduces it byte for byte.
- **Linux** reproduces every table, and its records differ only below the precision any table reports.
- **macOS** reproduces the paper and photograph gate tables, and differs in measurements 1 and 2.
- **Linux: explained,** on entry 48 section 2's three terms. **macOS: not yet explained,** and localised to S2 ("Entry 48" below).

---

## What is not done, and why

| section | what | why |
|---|---|---|
| 4.3, 4.4, 4.5 | the server purge, deletion as part of the intake run, and the backup question | all need SSH; the backup question is a read on Alan's list |
| 6.1, second half | deleting the six from the old server | SSH, after the install |
| 6.2 | the redirect | SSH, and only after the new page is live and tested |
| 8.2 | one real test submission through the live page, and one real crash report | the page is not live until the install has run |

## Entry 357: a second sending level, and error reports with the log, built switched off

Built 2026-10-03 behind `sendEverythingOpen` and `fullLogErrorReports` in website/api/limits.json, both false. On, the four choices are
Send everything I open (a picture goes when it is left, as unread or stopped at review, with its stage records and log; the accepted
version later is linked to it), Send finished targets only (renamed; existing users asked once, never moved), Ask me each time and
Never, with "Do not send this picture" and Wi-Fi only on phones; and an automatic error report also sends the Report a problem package
(this run's and the last run's log, typed values replaced by their length) and, for a failed read, its stage records matched to the
picture's submission. The receivers record a submission's state; the intake refuses to publish anything not accepted. Off, nothing
changes. The phone has no target sender (question 81).

## Entry 356: a failure in the middle of the window, option B final

Done 2026-10-03 by one worker. Opening a picture now decides: a GroupLab sheet that reads, a store-bought target recognized, a
picture that looks like a GroupLab sheet (its corner markers, code blocks or a printed "GL-" name) whose codes would not read, which is
the error dialog of board B final, or anything else, which is the calm "Which target is this?" with Mark it by hand first. The error
dialog shows the picture with its codes outlined and the stages, Choose the sheet first, Try again reading harder (once a picture), Mark
it by hand, and "Not a GroupLab sheet?", remembered for that picture and counted; dismissing it leaves an amber bar. Reading harder squares
and evenly lights each code where the markers predict it, reads it at several thresholds and enlargements, uses one code to find the
others, and looks for the printed name: 6 of 18 unread pictures rescued. On the computer every failure that stops the work is now a
dialog (Enter takes the first choice, Escape closes), information stays in the status line. The "looks like" test fired on none of 70
pictures without GroupLab marks, but costs about 5.4 s on such a picture. Not done: the phone's page-by-page failures as sheets.

## Entry 354: Fenix's .22 LR targets

Done 2026-10-03 by one worker.

- **The load sheet** (002_IMG_3819, .22 at 50 yd), marks before and after, found and false: camera reading 32/24/8 to 24/24/0; the
  server's PNG 29/21/8 to 24/21/3; with no metadata 27/21/6 to 22/20/2. A photograph's candidate past everything the sheet prints is
  refused unless it is a clean round bullet-sized hole (scans keep their margin: scan 5 of 2026-09-20 has a real shot 0.2 in from a
  corner); a mark wholly on the sheet's printed words is those words; a split half inside a marker or code zone is refused. The review
  counts shots ("2 of 29 and 1 more"), and with more marks than rounds starts with marks off the bulls and beside the printing. Bull 23's
  hole, touching a marker, is still missed. Corpus: the kitchen counter line 2 false marks to 1; every other line identical.
- **The diamond sheet** (001_IMG_3817, generated, 50 yd, 6x): its codes read, but a sheet never saved is on no list, so it was refused
  and the desktop wrongly said the codes could not be read. Identification now takes the design from the codes; 35 of 36 markers, all 25
  holes, 5 false on the wavy left side. Both photos are 22 LR scoreboard cases and test-data files.

## Entries 353 and 355: the iPhone ignoring taps, and what the sending settings send

Done 2026-10-03.

- **353:** on TestFlight builds 157 to 159 a tap did nothing while a field had been typed in. Entry 350's KeyboardRoom gave the
  keyboard's room back during the press: a press outside a field closed the keyboard at once, the focus leaving a field for a button
  closed it, and the system's own close did too; the page moved under the finger and the release missed. Now nothing moves the page
  while a finger is down (followed at the window, so a pop-up counts), the keyboard closes after the release, and closing does nothing
  when nothing was raised. Entry353Tests press as a finger does and fail on build 157's code. Build 160 reached both TestFlight groups
  the same night. Real taps now run with each nightly that changes the application: XCUITest on the iPhone simulator and held adb
  swipes on the Android emulator, ten checks each, all passing; getting the iPhone run right took three fixes to the test itself (it
  read a control's place before it was drawn, expected a camera the simulator does not have, and could not see the photo picker it
  opens instead).
- **355:** the account of the sending settings at the top of for-alan.md. Nothing from Fenix's desktop reached the server or the archive;
  a target goes only after Accept and analyze, so the likeliest reason is that he never pressed it (request 70). The settings' question
  and the "What GroupLab sends" page now say Accept and analyze, and that the archive worker files submissions.

## Entry 352: tonight's list, one worker

Done 2026-10-02.

- **1, Android in the phone sweep:** `.github/workflows/android-emulator.yml` (an API 34 x86_64 emulator with KVM, GroupLab Dev built for
  x86_64 with its own OpenCV) runs `scripts/android-sweep.sh`, three passes like the iPhone's, judged by `scripts/phone-sweep-check.py`;
  the nightly starts it when the application changed. First run: built, booted and installed, then the files piped into the app arrived
  cut short; they now go through the shared temporary folder. The iPhone sweep passes plain, largest text and dark on a 300 dpi copy of
  the sample (the 600 dpi read took 52 to 62 s on the runner and hit the one-minute limit).
- **2, the Eze-Scorer:** bold print is strokes of even width closing round a small counter; a torn hole is a core with spikes narrowing
  to nothing. Clean-sheet false marks 6 to 2 (the logo's letters, which have no counter), synthetic false marks 32 to 6, every other
  scoreboard line, the corpus and the 15 commercial scans unchanged. Margins are thin (0.60 against 0.62).
- **3, untrusted files:** chronograph files at most 10 MB, an xlsx counted before the workbook reader sees it (64 MB, 10,000 parts, 1,000
  sheets, 4 million cells), text decoded by its byte order mark, formulas never worked out, fingerprints and references bounded, the
  library refused unread past 64 MB. Before: a damaged xlsx closed GroupLab, and a fingerprint claiming 2^31 bulls ran out of memory.
  `UntrustedFilesTests`, about one second.
- **4, speed:** on an idle machine the 2026-09-20 and today's code by turns: today's is about 3 percent faster, so the 7 percent was the
  machine. The two-centre split stops when its centres stop moving and finds its pixels once; cells and bulls run in parallel, results in
  order. One sheet 924 to 486 ms, ten sheets 8279 to 3897 ms; the phone's sample 1485 to 1185 ms. Every Phase 0 table, scoreboard and
  the corpus identical.
- **5, write-ups:** the week's investigations each have a decision in docs/RESEARCH.md; one draft, "printed numbers are not holes",
  unpublished.

## Entry 342, worker A item 2: the phone sweep

Done 2026-10-02 (worker A, finished after the pause). Two layers. **Headless, every push** (`MobileSweepTests`): every place along the
bottom, a result, and every page or sheet one press away, at 320 x 568, 1024 x 1366 and both turned on their sides, both themes and large
text: every control named for a screen reader, inside the window or scrolled into view, no text cut off, every field above the keyboard,
and every button that changes nothing for good pressed without a throw or a dead end. **On the iOS simulator** (`ios-app.yml`, started
by every nightly that changes the application): GroupLab Dev runs `scripts/scenarios/phone-sweep.json` plain, at the largest text size
and dark, keeping a screenshot and a control tree of each screen, failing on a crash or a missing step; the same scenario runs headlessly
on every push (`PhoneSweepScenarioTests`). **Found and fixed:** about forty fields and switches announced only as "text field" or "switch",
and buttons announced by their panel's type, now named by the words a person reads (`FieldNames`); the bull picker's numbers 10 to 25
cut off at 320 wide; on an upright iPad the keyboard made the result page wider than tall and flipped it to its side-by-side layout,
losing the field. **Not done:** no Android emulator job, so Android is covered by the headless layer and the sittings only; the headless
layer cannot see a real keyboard, camera, picker, share sheet or scaling, which the simulator pass and the sittings cover in part.

## Entries 348 and 351: adding a store-bought target, and changing a pairing one mark at a time

Done 2026-10-02 by worker A, from Alan's concept A on both.

- **348:** "Add a store-bought target" on Targets, computer and phone: Photo, Size and scale (the three sources, each with what entry
  344's poster trial measured), Straighten (amber corner handles), The bulls (numbered rings, remove, add, "These are right"), Name and
  send ("Save reference file", or "Save and share the file" through the share sheet; the fingerprint, name, size and bulls, never the
  photo). One session drives both screens. A bug found on the way: on the phone, going back to the targets list after opening a sheet
  failed, the list being rebuilt around fields that still had a parent.
- **351:** a row per reading in the order fired, a mark on each (teal paired, amber to look at, plain "Not this group"), pauses as
  dividers, a bottom sheet on the phone and the same four choices under the row on the computer; a shot already taken swaps, and says
  so. Imports in m/s show m/s. Tested at 320 wide, large text, both themes and with a 93-shot string.
- **Pictures:** `fingerprint` (a poster GroupLab draws) and `pairing` (a timed string) on the Features page and the tour; the phone's
  pictures wait for a sitting.

## Entry 347: the fingerprint library reaches people without a new build

Done 2026-10-02. The nightly signs `grouplab-target-library.json` with the update key (`grouplab target-reference library`, the key
from the nightly's secret), lists it in the signed manifest as platform `library`, kind `targets`, with its SHA-256, and attaches it to
the numbered and the moving release; every older build's lookup passes the extra asset over. The site publish copies it to
grouplab.org/library/. `StoreLibraryUpdate` reads the manifest, refuses it unless signed, skips the download when the kept file already
has the listed SHA-256, checks the file against it and against its own signature, and installs it only when newer than the built-in
list (`library.json` now carries `version`, which `target-reference add` raises). grouplab.org's copy is read only when GitHub is not
reached, trusted only by its signature. The computer looks at launch on every copy; every phone copy looks at start on an unmetered
connection with the battery and storage not low, at most every six hours (Android reads all three; the iPhone reads Wi-Fi, Low Data
Mode and Low Power Mode). Tests: three in Core, one in Mobile.

## Entry 349: the download page and an Android tablet asking for the desktop site

Done 2026-10-02. Firefox and Chrome on an Android tablet ask for the desktop site with "X11; Linux x86_64", so the page offered Linux
and said "This computer". A touch screen with no fine pointer now counts as Android there (and as handheld for the Desktop or Mobile
switch), a Linux laptop with a touch screen and a trackpad stays Linux, and a guess resting on the touch screen says "Looks like this
device". `website/build.py` runs eight cases in Node on every build; on the old script the two tablets got Linux, as Alan saw.

## Entry 346: a Garmin Xero workbook is a selection, not a month

Done 2026-10-02. ShotView's workbook holds the strings the person selected, named after their month or months. The three code comments
are corrected, "monthly export" is retired wording (`docs/RETIRED-WORDING.json`), and the next nightly's notes carry the correction;
nightly 145's published note stays as it was. The reader never assumed a whole month.

## Entry 350: a friend's TestFlight reports, the keyboard on Targets

Done 2026-10-02. Issues 15 and 16 (build 150, an iPhone, Targets): the keyboard's bar was left over the screen after the keyboard went,
with Done doing nothing, and the number pad stayed up with no bar. `KeyboardRoom` trusted the system's keyboard events to arrive and in
order; it no longer does: a tap outside a field closes the keyboard whenever a field has the focus, Done always takes the focus, asks
UIKit to end editing (the iOS head's `HideSystemKeyboard`) and puts everything back, the bar goes as soon as no field has the focus (on
focus loss and every 400 ms while shown), and a field focused with the keyboard already up raises the bar from the pane's own state.
`Entry350Tests`: three tests, each failing on the old code. The scheduled feedback run now looks back 24 hours, not one.

## Entry 345: the consistency audit of 2026-10-01

Done 2026-10-01, in one change.

- **The site:** the home page's "Not built yet" is now blank-paper detection and synchronization, and its platforms name the iPhone and
  iPad beta and the Store copy; the download page says only the Windows installer of the computer downloads updates itself, that
  TestFlight installs each beta build, and that the plain APK has a different key from Google Play's; the support page has a phone
  paragraph (Send diagnostics, TestFlight's Send Beta Feedback) and says the recovery note is about Windows.
- **The Features page:** iPhone and iPad on the 39 features PHONE-PARITY.md marks on iOS, and `parity_problems` now fails when the two
  disagree; Velocity and the vertical says the readings can come from a file.
- **The guides:** the testing guide's "What is not done yet" no longer says Xero import or the phone's hand marking and CSV are missing;
  the user guide's iPhone and iPad line says public beta.
- **The platform statement:** the Store copy is published and signed; the two references to things no longer below it are fixed.
- **README, CONTRIBUTING, STATE, for-alan:** the status list names the iPhone and iPad beta, store-bought recognition and chronograph
  files; the deferred line no longer contradicts recognition; CONTRIBUTING matches LICENSE's section 7 permission; STATE's nightly, site,
  Store and next three are current; request 59 is cut to what may be left; two superseded summaries are archived.

## Entry 342, worker A: the phone's pipeline, the desktop sweep, the checklist

2026-10-01; items 1 and 3 by worker A, item 4 and item 1's first half by this session. Item 2 is not started (budget).

- **1, the slowdown:** the 2026-09-20 and today's code benched by turns on one machine are within a few percent on image work; only the
  end to end case looks about 7 percent slower, too noisy to bisect under load (docs/PERFORMANCE.md).
- **1, the phone's pipeline:** reading the sheet's codes was the slowest stage on every picture (0.6 to 0.95 s of 1.3 to 2.0 s): a
  cut-out that failed to read went through the whole-sheet corner search. Cut-outs are now read as themselves: 462 to 208 ms on the
  600 dpi sample, 467 to 949 down to 191 to 415 ms on eight Fold 7 photos; the whole analysis 1.27 s and 1.10 s. Every Phase 0 table,
  the identify sweep, both scoreboards and the corpus counts are identical but for their time columns. Left: hole detection and the bull
  locator, which decide figures, and one photo's 3.7 s whole-picture read, which would change the sweep's "read at" column.
- **3, the desktop sweep** (`DesktopSweepTests`): eight screens and four windows at 1060 x 720, 1920 x 1080 and 2560 x 1440: every control
  named for a screen reader, a tab stop, reached by Tab, inside the window, and Space and Enter press a button reached by Tab. Fixed:
  about 50 unnamed fields; Targets' zoom buttons 89 units past the edge at 1060; Space and Enter on the marking screen always going to
  the review shortcuts. The headless platform cannot see scaling above one, hear a screen reader, see focus rings or reach system dialogs;
  the printer wizard, the CSV import, the chronograph import picker and the Fudd buster window are not covered yet.
- **4:** `docs/PROOF-CHECKLIST.md` is one list ordered by how many features each piece of material proves; question 79 stays open.

## Entry 344: a store-bought target's fingerprint from a camera photo, the engine

Done 2026-10-01 by worker A; no screens (request 69).

- **The command:** `grouplab target-reference make` straightens a photo by one of three scale sources, each recorded with an uncertainty
  of about two standard deviations: (a) the printed size typed in, fitted to the four corners; (b) a GroupLab sheet in the same photo,
  read by its markers, which also corrects the lens; (c) two tapped points and a distance, the corners taking out the angle. Then entry
  332's fingerprint from the straightened picture with the bulls the person confirms, and the family check against the whole library
  (the same size is a duplicate, another size joins the family). `check` does Code's checks on a submitted file, `add` puts it in the
  built-in library (`src/GroupLab.Core/StoreTargets/Fingerprints/library.json`), `library` writes the signed library file, read only
  when its signature checks. Never a photograph or a maker's artwork.
- **Synthetic posters**, 12 photos each, tilted 1 to 29 degrees (`grouplab poster-trial`), scale error median / largest: 12 x 18 in, (a)
  0.01 / 0.32 percent, (b) 0.28 / 0.55 (the sheet read in 8 of 12), (c) 0.16 / 0.46; 23 x 35 in, (a) 0.28 / 0.63 (corners found by
  themselves in 1 of 12, the rest tapped), (b) never read, a Letter sheet too small at that distance, (c) 0.40 / 0.80. A fingerprint from
  one photo named its poster in 11 of 11 others at both sizes. **So (b) is not the most accurate at poster size**, as entry 344 expected;
  carried to a poster's far corner it does no better than (a), and without a printer check it carries another 3 percent.
- **Not done:** the screens (request 69); the fetch of a newer library on the update check (question 80); a real phone photo of a real
  poster (the trial's photos are synthetic, with no lens distortion and an assumed focal length).

## Entries 340 and 341: store-bought targets recognized, with a warning on their scale

Done 2026-10-01 by worker A.

- **Recognition:** the five Birchwood Casey products, from fingerprints of 25 to 60 KB made by `grouplab store-fingerprints build` (no
  scan or picture committed), with the trial's ORB features, colour layout and thresholds. On the trial's 200 pictures, 120 phone views
  and 42 scans are named, none wrong, 2 now asked about; of 130 real pictures of other targets none is claimed. 0.5 to 1.2 s a picture
  on this desktop; not measured on a phone.
- **Where it shows:** on the computer, a picture naming no GroupLab sheet tries the five before asking which sheet; a recognized one is
  named, its bulls placed and its printed size set as the scale, so Find holes is offered at once. On the phone it goes straight to the
  hand-marking page with the bull and scale set.
- **The family:** the Shoot-N-C bullseye at 6 and 8 in asks which, with GroupLab's own outline of each and Not sure, last answer first,
  only when the picture cannot tell. Beyond the trial's rule, a size is named without asking only when clearly ahead (agreement 0.95 or
  150 features), since the rule alone named the wrong size on 4 crops; on such tight crops the chosen scale can be about 10 percent off,
  which the warning covers.
- **The warning (entry 341):** under the scale, on the result and in the report's scale sentence, with Check the scale one click away;
  never on a GroupLab sheet. Request 64 closed as not needed.
- **Not done:** the Features page's own picture of it (the screenshot walk); recognition time on a phone.

## Entry 342, worker B: the audit, the pairing proposal, the Store route, triage

2026-10-01, this session.

- **1, the audit by hand:** the README, the user guide, the release notes (current to nightly 149), the Features page and the tour
  already said what the application does tonight; the testing guide knew only Android's routes and now names the Microsoft Store copy
  and the iPhone and iPad beta. `scripts/consistency.py` now checks that the README and both guides link the Store and the beta the
  download page offers, and that the README and user guide name every chronograph file the application reads.
- **2, chronograph reconciliation:** an imported string that numbers its shots proposes its own marks, with a sentence for each: the
  Xero's shot times split a string into runs at pauses (over five minutes and three times its median interval), and where exactly one
  stretch of runs is as many as the group's shots the rest are no shot of it; then a shot the Xero left out of its own figures, then one
  marked clean bore; and a skipped number gives the shot fired there no reading. Nothing is guessed where the file says nothing. On the
  computer the marks are the rows' own; the phone pairs as proposed, and changing one mark there is request 68 (DESIGN NEEDED). Across
  Alan's exports 534 strings carry times and 132 hold more than one run; tested on his 2026-04-25 string with synthetic shots.
- **3, the Store route:** a dry run of `store-submit.yml` for 0.2.0 signed in, read the published submission and downloaded the package,
  changing nothing; a real submission waits for request 66.
- **4, triage:** no issue is open in the crash reports repository.
- **On the way:** the Phase 0 gate record had failed on every push since entry 322's refit, on the photos table alone; re-recorded.
  Entries 325 and 327's detection work and the fold of 324, 325 and 327 had never reached main and were recovered from a finished
  worker's worktree; seven such worktrees, about 6 GB, went to the trash folder.

## Entries 339 and 343: the BulletSeeker's name, and GroupLab Dev while idle

Done 2026-10-01.

- **339:** format A's reader is named BulletSeeker everywhere a person sees it; still Experimental, its two shotless files said. Across
  every Xero file no column names the device, so C1 and C2 read the same; the metric export's "Speed (MPS)" was read as ft/s and now
  reads as m/s (23 strings), with a test.
- **343, 1:** GroupLab Dev's update check now waits for battery not low and storage not low as well as an unmetered network, and existing
  schedules are updated to it. **343, 2:** without an emulator in CI, a Core test reads the Android source: the pause releases the
  camera, the torch and the level's sensor, and nothing else registers a sensor, location, wake lock, alarm or foreground service. The
  idle-hour batterystats reading is for request 50's sitting.

## Entries 335 to 338: the Public Beta, the Microsoft Store, and the download page

Done 2026-10-01.

- **Published, with Alan's approval in the session:** the TestFlight invitation and Microsoft's "Get it from Microsoft" badge (served from
  the site, unaltered) on the download page, the README's generated table and the start of the guide.
- **The lockstep:** build 148 went into GroupLab Team and the Public Beta and was submitted with nobody touching App Store Connect;
  request 67 asks Alan to turn the team group's automatic distribution off.
- **Why nobody heard:** the TestFlight step and the Store check wrote only into their run summaries. `scripts/status-note.py` now keeps
  `docs/notes/external-status.md`, committed as "[notes] " only when a state changes; the Store check also names the package version.
- **The Store route:** `store-submit.yml`, by hand, with request 38's existing keys, a dry run unless the version is typed twice; request
  66 proposes the cadence.
- **The download page** (entry 338, worker A): device buttons, the newest build on the left and the store copy on the right, everything
  else folded, `?device=` links, all five sections shown with scripts off; every figure from docs/RELEASE-NOTES.md.
- **An incident on the way:** nightly 148 left the moving "Latest nightly" release a draft, so every download link answered 404 from about
  09:06 to 09:20 UTC; published by hand, and the nightly now publishes it if it is a draft and fails unless a download answers.

## Entry 331: a batch that needs nobody, and entry 332's fingerprint trial

Done 2026-10-01; sections 1, 2 and 4 by worker B (this session), section 3 and entry 332 by worker A.

- **1, store-bought blanks:** Find holes gave 0, 0, 0, 3 and 8 marks on the five clean blanks; the crosshair's three were its solid black
  diamonds, the EZE scorer's eight printed ring numbers and logo letters. `PrintedShape` refuses a solid straight-sided shape filling its
  smallest rectangle, and a light mark in print made of strokes; a dark mark is never judged by its strokes, since a torn hole's dark rim
  round a light centre is a thin ring (four real any-target holes were refused until it was so). Now 0, 0, 0, 0 and 6; every any-target
  scoreboard line identical. `grouplab scoreboard --blanks` renders holes into each blank and scores them, nothing written: paper about
  half found, ink most on the Shoot-N-C targets but none on the 6 in bull, ring lines almost none, red centres some, 0 to 7 false marks a
  picture. Detail in the commit; the real shot scans will check it.
- **2, chronograph files:** a generic CSV, LabRadar's report and Garmin Xero's export behind one interface, ending in the hand-entry box's
  list, with "Import a file" on both platforms; entry 334 then proved the Xero reader on Alan's own files.
- **3, the 31 features** (`docs/PROOF-CHECKLIST.md`): measured where material allowed. Not met: off-axis photographs (worst bull 0.0155 to
  0.0216 in against 0.005), detection (197 of 198 holes, 2 false marks), the large-mark rule (7 rings on clean single holes). Met where a
  written gate exists: hit probability inside a radius (7), the solver (22), the compareGroups keys of significance testing (6), the
  Phase 6 capture gate in its own words (26: 97 of 100 holes, lens fitted on the device), and Phase 0's scan half (worst bull 0.00275 in).
  Question 79 asks planning about their states. The rest are listed with the exact material each needs, as one checklist.
- **4, performance:** `docs/PERFORMANCE.md`: start-up 1.9 s to a window, about 180 MB idle, the phone's pipeline 2.5 to 2.7 s on this
  desktop's processor; the image cases 20 to 35 percent slower than 2026-09-20, not yet explained.
- **Entry 332, the fingerprint trial** (`docs/notes/fingerprint-trial.md`, worker A): ORB features at two resolutions with a colour layout
  checked through the fitted transform (AKAZE was slower and claimed a wrong product); 33 to 84 KB a target; 163 of 200 test pictures
  identified, no wrong product, no false match on 123 non-targets; scale error median 0.06 percent, bulls 0.007 in; about 0.6 s on this
  desktop. Recommendation: ship recognition, keep a scale check until request 64 measures sheet to sheet differences.

## Entry 334: Alan's chronograph exports, and entry 333's two workers

Done 2026-10-01 by worker B (this session); worker A took entry 332 and entry 331 section 3 in its own worktree (entry 333).

- **The files:** 387 copied from his Google Drive folder, read only, into `C:\Dev\grouplab-local\chronograph-samples\garmin-xero\`
  (by year, and a `metric` folder of two Xero exports in m/s that appeared during the work); the 23 zips hold an app's database backups
  (`dbBackup.db3`), not exports, and were left out; three photographs were left out.
- **The reader** (`ChronographFiles.ReadFile`, through ExcelDataReader): Garmin Xero, format B, 92 files, 536 strings, 9,863 shots, every
  one read; the 2023 radar export, format A, another maker's, 295 files and 2,860 shots, two files holding a string with no shots.
  `grouplab chronograph-files <folder>` makes the count. Deleted shots stay gaps in the numbering. A shot the Xero shows as "--" in its
  difference column is one it left out of its own figures; it is kept, marked and said, and the footer is checked against the others,
  which took the disagreements from ten to three: two May 2024 footers stating an SD of 0.0, and one average 0.3 ft/s off.
- **Location:** no reader reads a row it does not name, so the 2023 exports' location block never comes in;
  `ChronographWorkbookTests.TheRadarExportReadsItsShotsAndNeverItsLocation` writes one with a place and coordinates and finds neither in the
  result. Nothing of Alan's is committed; the tests write workbooks in both layouts, and a local test reads his whole folder where it is.
- **On screen:** Import a file takes .xls, .xlsx and .xlsm on the desktop and the phone, a string chosen by its name where a workbook
  holds several. Not yet: offering a file's projectile weight and weather to fill the load and the conditions (section 3.3).

## Entry 329: Velocity and the vertical flies the load in the conditions entered

Done 2026-10-01, one worker on the main model.

- **1, the solver's input:** `VelocityBlocks.Build` takes a `VelocityConditions` (the air and the shot angle, and where they came from)
  and fills the solver with them, the rifle's sight height, zero, twist and direction, and the bullet's diameter and length. Sessions do
  not carry conditions yet, so on the desktop they are the Ballistics screen's air and shot angle, and only when that screen has the
  session's rifle and load chosen (`MainWindow.ConditionsForVelocity`). The phone's Ballistics air lives only while its page is open, so
  the phone's block takes the standard day. No new form; a session's own conditions would need a DESIGN NEEDED.
- **2, why:** one line names the conditions and their source ("Conditions: 90 F, 6000 ft of altitude, 50 percent humidity, shot level,
  from Ballistics, for ..."), or says "Standard day assumed: no temperature or altitude entered."; the assumed sight height and zero are
  named one by one.
- **3, the command:** `grouplab velocity` takes `--temperature --altitude --pressure --humidity --angle --twist --diameter --length` and
  the same names in the session file, prints the air, the humidity, the angle and the twist, and the standard-day line when none is given.
- **4, the test:** the same group and readings at 59 F at sea level and at 90 F at 6,000 ft: the vertical from velocity alone changes by
  the ratio of the solver's own heights per ft/s, the share by its square, the measured vertical not at all; with nothing entered, the
  standard-day line (`TheConditionsEnteredChangeTheHeightPerFpsTheWayTheSolverSays`). The command's options are in the CLI test.
- **5:** the guide's paragraph says the conditions are used and that why names them; claims all backed.

## The archive

Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.

- [`docs/notes/archive/results-026-050.md`](notes/archive/results-026-050.md), entries 026 to 050, 16 section(s).
- [`docs/notes/archive/results-051-075.md`](notes/archive/results-051-075.md), entries 051 to 075, 10 section(s).
- [`docs/notes/archive/results-076-100.md`](notes/archive/results-076-100.md), entries 076 to 100, 6 section(s).
- [`docs/notes/archive/results-101-125.md`](notes/archive/results-101-125.md), entries 101 to 125, 26 section(s).
- [`docs/notes/archive/results-126-150.md`](notes/archive/results-126-150.md), entries 126 to 150, 15 section(s).
- [`docs/notes/archive/results-151-175.md`](notes/archive/results-151-175.md), entries 151 to 175, 25 section(s).
- [`docs/notes/archive/results-176-200.md`](notes/archive/results-176-200.md), entries 176 to 200, 23 section(s).
- [`docs/notes/archive/results-201-225.md`](notes/archive/results-201-225.md), entries 201 to 225, 21 section(s).
- [`docs/notes/archive/results-226-250.md`](notes/archive/results-226-250.md), entries 226 to 250, 25 section(s).
- [`docs/notes/archive/results-251-275.md`](notes/archive/results-251-275.md), entries 251 to 275, 27 section(s).
- [`docs/notes/archive/results-276-300.md`](notes/archive/results-276-300.md), entries 276 to 300, 23 section(s).
- [`docs/notes/archive/results-301-325.md`](notes/archive/results-301-325.md), entries 301 to 325, 14 section(s).
- [`docs/notes/archive/results-326-350.md`](notes/archive/results-326-350.md), entries 326 to 350, 2 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 145 section(s).

## Entry 375: corner brackets that do not depend on the cut (2026-10-05)

- **The scale never came from the cut**, and now nothing else does either. The plane (pixels to inches) is fitted to the brackets'
  printed codes; only the target's corners, and so a reference file's width and height, came from the L's inside corners
  (`MarkerFinding.NearCorners`, renamed from `TargetCorners` to say so).
- **The corners now come from the paper's own edges** (`ScaleMarkerFinder.CornersNearBrackets`): a strip 16 mm either side of the
  line between two brackets, straightened in the codes' millimetres along the middle of each side clear of the arms, its steps found
  every 2 mm, and the line most of them hold; the four lines meet at the corners. They are called sure only where the four make a
  rectangle in the codes' plane (square to 1 degree, opposite sides within 1.5 mm), step the same way by much the same, darker going
  out, with the surface just outside. Painting the brackets out (the first try) left flat patches the outline finder took for the
  target, and is gone.
- **Measured** with `grouplab surface-trial --brackets` (4 surfaces, gaps 0, 2, 5 and 10 mm, laid square or roughly: each piece up to
  2 mm astray and about 2 degrees turned, which is what a crooked cut does once laid against the paper; 177 scenes). The target's size
  from the cut corners was 2.0 to 3.1 percent out at a 2 mm gap, 4.8 to 5.6 at 5 mm and 9.4 to 12.3 at 10 mm. The codes' scale was
  0.03 to 0.13 percent (median by row), 0.23 worst, at every gap and laid either way. Corners were sure in 44 of 177 scenes, every one
  within 0.6 mm of the true corner; the rest start at the brackets for the person to drag, as before entry 371.
- **Where it is never sure:** a white target on a white counter (its edge does not show, and a printed frame 10 mm inside one was
  taken for it until the darker-going-out rule), a target inked to its edge on a light surface, and a target too small to leave 20 mm
  of edge between the arms (about 7.8 in a side).
- **The fit's rectangle loosened** (`MarkerFit.SoftRectangle.Laid` and `Square`) from 0.5 mm and 0.5 degree to 3 mm and 3 degrees,
  since the pieces are now laid anywhere close. `grouplab marker-trial`: brackets alone 0.12 percent median, 0.75 worst (0.78
  before); every marker together on the 23 by 35 poster 0.14 worst against 0.04, one photo; the surface trial's scale did not move.
- **The words** (section 3): the Scale markers screen, the printed bracket page and the user guide now say to cut roughly and place
  one near each corner, flat, anywhere close. `ScaleMarkerTests.BracketsSetOffOrLaidRoughlyGiveTheSameScale` holds the scale to 0.1
  percent with gaps of 0, 2, 5 and 10 mm and pieces 2 mm and 2 degrees astray.
- **Section 2:** the L stays. It holds both codes in one rigid piece, which the fit uses, and lets a piece sit round a corner without
  covering the target; it does not help the edge search, which skips the 81 mm each arm lies along. Tiles were not made (see the
  status line in NOTES-FROM-PLANNING.md).

## Entry 373: the consistency audit of 2026-10-04 (2026-10-05)

All twelve findings fixed in one change; no `consistency` issue was open. README: iPhone and iPad in the platforms sentence and badge;
the Store row says it gets every nightly once certified (and /download/'s "Steady" badge reads "Updates itself"); GroupLab Dev first in
the Android row; the store-bought and scale markers bullets put back whole, the M220 labels said to be in nightly 168 and not yet
printed on a real one; nine store-bought targets (count markers in the README) in the README, the tour, PHONE-PARITY and CLAIMS; iPhone
and iPad in a public beta through TestFlight in PLATFORM-SUPPORT, the README, the how-it-works page and the plan's diagram, with a row in
the minimums table (iOS and iPadOS 26, iPhone 11; memory and disk not measured yet); Send
everything I open and the log in error reports in the user guide, the tour's Settings stop and the Features page; label targets on the
phone too, `phone-targets` matched by a longer phrase; the release notes' website lines removed and request, question and entry numbers
taken off the public text, the generator doing the same from now on (an issue number stays); the data folder named for every platform
in the README and the testing guide; request 16 trimmed to its open half; STATE.md rewritten. **Section 7 not done:** the "Not built
yet" sentence is what entry 103's `ReadmeTests.WhatTheConceptScreensCallUnbuiltIsPlannedAndNotDone` reads to hold the Screens
paragraph to the plan; removing it fails that test, so it stays and question 84 asks planning which way.

## Entry 374: the range trip of 4 October, and Unholy's feedback (2026-10-05, partly done)

- **The C bull sheet on the phone** (15 shots of .300 Norma Magnum, one at each of bulls 1 to 15, every one about an inch low and right;
  Alan's drawing is the truth). The phone found the holes (16 marks for 15 shots, calibre still .243 from the 6 ARC session) and gave
  them **one per bull by nearest bull, so most went to the bull below their own**: the "only 1 point of impact" Alan saw after clearing
  them. The whole-sheet offset (entry 229) existed for exactly this and stood down, rightly unsure: read one row higher, as fired at bulls
  6 to 20, the holes fit exactly as well (scatter 0.312 in either way). **Fixed:** among readings that fit as well as the best, the one
  whose shots sit on the first bulls, the order a sheet is shot in, is taken, one hole in ten allowed outside it
  (`ImpactOffsets.InShootingOrder`). All three of Alan's photos and the phone's own now give bulls 1 to 15, as his drawing does.
- **Per photo, current pipeline** (`grouplab analyze`): C bull 18:35 close: 15 marks, 14 real (bull 10's hole, torn at the paper's edge,
  missed on every photo) and 1 invented, assigned in order; 17:38 in sun and 18:35 wide: 14 of 15, all right. 6 ARC load sheet: wide 25 of 25,
  **every one on the bull of Alan's drawing**, bull 2's high flyer included; close 24 (bull 6's hole on its ring missed). E bull: close 22
  of 25 (no truth for assignment, as Alan said); **wide: no code read**, so the sheet was not recognised from 3 ft. The five store-bought
  targets and the Rigid crosshair were not run (they go through Add a store-bought target, by hand): next session.
- **The chronograph files**: the existing reader already reads the Xero's results export in both forms. CSV and XLS (old binary Excel)
  agree in every speed, time, note, the string's name and the bullet weight (245 gr on the .300 Norma string); nothing differs that
  matters. A test now holds the CSV's shape (name line, two byte order marks, quoted thousands, "--" energies). The timed pairing of the
  6 ARC and .300 Norma strings with their sheets was not run: next session.
- **The phone's record (section 3) and why it did not update for two days.** Every automatic check logged `update.check result=Offline`
  (the error sender failed at the same moment), and a check from Settings eight seconds later found nightly 169 and installed it. A
  process Android has only just started often has no network yet; the only automatic checks were at a process start (Android then kept
  the process for days, only resuming it) and a six-hourly worker that runs only on unmetered Wi-Fi. **Fixed:** a check on each return to
  the screen when the last answer is over an hour old or found no network; a failed launch check tried again after 30 seconds; the
  worker asks to be retried when it found no network; and on a metered connection "GroupLab N is out" is now said on the screen with
  Update now, not only in Settings. The paper protocol's Part A now starts with updating GroupLab before a range trip.
- **The five crash records**: none is a crash. Each is a run that ended without reaching its own exit, which on Android is the normal end
  (swiped away, reclaimed in the background, or replaced by an update): 07:02:15 is the run nightly 169's install replaced, 06:51:40 a
  start that wrote nothing before Android ended it, 03:51:26 the update worker's run, and 4 October 09:42 and 12:01 runs ended in the
  background. No exception and no stage in any; none became an issue. The range afternoon's own log is gone: only five logs are kept and
  each start makes one.
- **Fixed in section 4:** the zoomed picture keeps every drag (phone and desktop canvases stop the page's scroll gesture while zoomed);
  the "Updated to nightly N" note closes itself after 20 seconds; a selected shot shows its distance from its own bull, right or left
  and high or low, in the person's unit and MOA (desktop; the phone shows no selected position). **Paper and backing do not help
  detection**: they are only recorded, for separating the hole-to-bullet ratio by paper and backing later (entry 162).
- **Not done, next session:** the store-bought photos and the Rigid crosshair; adding the photos to the scoreboard and the local corpus;
  the timed pairing; two holes in one; undoing a review answer; shot data out and in; the 2 MOA 3 by 4 sheet.

## Request 73: the Phomemo M834's print commands, decoded and proved (2026-10-05)

- **The link.** Alan's bug report's snoop log held two devices: about 20 KB of Bluetooth LE writes were a watch's (its own bytes name it),
  and the Phomemo app sent the page, 136,992 bytes, to the M834 over **classic Bluetooth's serial port** (RFCOMM channel 1), not LE. The
  M834 also offers LE (nRF Connect: service ff00, write ff02, notify ff03), which the app did not use; that is the way an iPhone would
  need, and it is not learnt yet. `scripts/printer-recording.py` now takes the link that carried the most and says the other was
  another device's.
- **The commands.** Status questions and settings (`1F 11 n`, `1A ...`, `1B 4E 1C 02`, reset `1B 40`, then `1F 11 02 04`,
  `1F 11 37 64`, `1F 11 0B`, `1F 11 35 01`, `1F 11 3C 02`), then ESC/POS's raster header `1D 76 30 00`, 316 bytes (2528 dots) across
  by 3294 lines, and then **not raw rows but 255 LZO1X blocks**, each the next 4096 bytes of the page (the last 520), its packed length in
  three bytes, low first, each ending in LZO's own end mark `11 00 00`. 1,040,904 bytes unpacked, exactly 316 by 3294, nothing left
  over. Which setting is darkness, speed or paper is not known; GroupLab sends them as the app did.
- **The proof.** The rebuilt page is the C bull sheet, its identifier readable. Rendered at 300 dpi, the PDF Alan printed lies over
  it at **94.675 percent** scale (measured from the ink's extent across, best offset found by search): 96.8 percent of the black is
  shared and 0.84 percent of all dots differ, the rest being edge smoothing and the foot of the page. At 100 percent only 34 percent
  is shared. **So the Phomemo app printed the Letter sheet 5.3 percent small, and cut its last line in half**: a target printed through
  the app is the wrong size whatever its scale setting said. That is the measured case for printing directly.
- **Direct printing, started.** `Lzo1x` (a compressor of literal runs and long matches, and a full decompressor), `PhomemoLzoEncoder`
  (the app's settings, then the raster in 4 KB LZO blocks, a page of another width centred on the head), and the M834's profile
  (classic Bluetooth, 300 dpi, 2528 dots, not yet tested). Checked both ways: GroupLab's decompressor reads the app's whole recording
  to the same page as the independent Python one, Python reads GroupLab's own stream (158 KB for the app's 137 KB) back to the same
  page, and 300 random round trips agree. On Android: a classic serial link to the paired printer, Bluetooth permission asked the first
  time Print is pressed, and **Print on the Phomemo M834** on a sheet's page in Targets, marked new and not yet tried.
- **Not done, next session:** the computer (a paired M834 is a serial port there); the iPhone's LE way; what each setting does
  (the darkness test through GroupLab); the printer's answers (battery, paper) read back; the article.

## Entry 372: scale labels for the Phomemo M220

- `ScaleLabels`: rows of two 8 mm tag36h11 codes (1 mm modules, 8 dots at 203 dpi) across the label's width only, 5 mm in from each side;
  each width from 20 to 75 mm has its own block of ten identifiers in 470 to 549, so the spacing comes from the identifier, and a running
  serial gives each label its own pair. Two rows on a label 26 mm or more high, each its own piece (the feed is not trusted). Only the
  printer's across scale applies. Targets: the size loaded, remembered, and four labels saved (computer) or shared (phone) at 203 dpi.
- `grouplab surface-trial --labels --scenes 4` (one label an inch in from the top left, or two at opposite corners, square to the target,
  the target's corners taking out the angle; percent):

```
surface          label    count  read   median error  worst error  claimed (median), percent
dark wood        70x80      1   4/4         0.051        0.133     1.630
dark wood        70x80      2   4/4         0.051        0.186     1.612
dark wood        50x30      1   4/4         0.069        0.246     1.963
dark wood        50x30      2   4/4         0.041        0.073     1.780
white counter    70x80      1   4/4         0.048        0.468     0.897
white counter    70x80      2   4/4         0.266        0.426     1.653
white counter    50x30      1   4/4         0.475        0.651     2.385
white counter    50x30      2   4/4         0.236        0.398     1.117
cardboard        70x80      1   4/4         0.145        0.243     1.428
cardboard        70x80      2   4/4         0.196        0.458     1.462
cardboard        50x30      1   4/4         0.077        0.086     1.399
cardboard        50x30      2   4/4         0.050        0.440     2.147
black table      70x80      1   4/4         0.883        0.933     1.981
black table      70x80      2   4/4         0.202        0.358     0.985
black table      50x30      1   4/4         0.110        0.278     1.708
black table      50x30      2   4/4         0.188        0.740     1.450
```

- Not done: GroupLab's own M220 encoder over Bluetooth (ESC/POS GS v 0, after phomemo-tools' description) and whether the M220 is
  Bluetooth LE or classic; the M220 printer check label; the size kept in the printer's own profile (it is kept in the settings); a size
  choice on the phone (it uses the remembered one).

## Entry 371: corner and marker finding on simulated surfaces

- **The simulator.** `grouplab surface-trial` lays Alan's four 600 dpi Birchwood Casey scans and six of his phone photos, flattened by
  their checked corners, on 14 procedural surfaces (no texture photograph, so no licence question) under the poster trial's camera:
  tilt to 30 degrees, turn, a shadow across an edge (30 percent), glare on the glossy targets, tape over a corner (15 percent), a
  corner out of the frame (8 percent), warm or cool light, blur, noise and JPEG. Not simulated: curl and a lifted corner.
- **Section 1, honesty.** 112 scenes, seed 371 (corners right within 1.2 percent of the width on a scan, 3 percent on a flattened photo,
  whose own corners were checked to about 1 percent):

```
surface             scenes  right  found  found and wrong  not sure but right
black table              8      7      6                0                   1
dark grey table          8      8      5                0                   3
dark wood                8      4      4                2                   2
light wood               8      5      5                1                   1
white counter            8      6      6                0                   0
cream counter            8      5      3                0                   2
cardboard                8      6      6                1                   1
OSB                      8      1      2                1                   0
foam board               8      3      4                2                   1
backer with holes        8      5      6                1                   0
over old targets         8      5      1                0                   4
grass                    8      6      3                0                   3
gravel                   8      4      3                0                   1
carpet                   8      5      4                0                   1
all                    112     70     58                8                  20
```

  Before (weakest side 0.7 or more): 86 called found, 21 wrong (24 percent). The weakest side alone stays about one in seven wrong at
  any threshold (0.98: 48 found, 5 wrong). Now, the weakest side 0.8 or more and a second way of looking (colour, edges, the light
  region, the printed extent) on the same corners: 38 found, 1 wrong (2.6 percent); 33 right outlines start as not sure. **Alan's 15
  real photos, held out:** before 14 found, 3 wrong (the grained floor); now 6 found, none wrong; 5 right ones start as not sure.
- **Section 4, touching or a gap** (`--markers`, four scenes each, brackets and two bars together):

```
surface          gap mm  brackets: corner off mm, size error percent | bars: corners right, scale error percent
dark wood             0    0.09 mm,  0.02 percent (3/4) | 0/4 right, 0.022 percent
dark wood             2    2.87 mm,  1.97 percent (4/4) | 0/4 right, 0.171 percent
dark wood             5    7.11 mm,  4.85 percent (4/4) | 0/4 right, 0.100 percent
dark wood            10   14.19 mm, 12.72 percent (4/4) | 0/4 right, 0.028 percent
dark wood            20   28.35 mm, 19.35 percent (4/4) | 0/4 right, 0.221 percent
white counter         0    0.18 mm,  0.06 percent (2/4) | 0/4 right, 0.083 percent
white counter         2    2.88 mm,  2.58 percent (4/4) | 0/4 right, 0.041 percent
white counter         5    7.14 mm,  4.84 percent (4/4) | 0/4 right, 0.043 percent
white counter        10   14.20 mm,  9.69 percent (4/4) | 0/4 right, 0.059 percent
white counter        20   28.33 mm, 19.33 percent (4/4) | 0/4 right, 0.031 percent
cardboard             0    0.09 mm,  0.04 percent (2/4) | 0/4 right, 0.045 percent
cardboard             2    2.94 mm,  1.96 percent (4/4) | 0/4 right, 0.055 percent
cardboard             5    7.11 mm,  4.86 percent (4/4) | 0/4 right, 0.121 percent
cardboard            10   14.20 mm, 12.72 percent (4/4) | 0/4 right, 0.216 percent
cardboard            20   28.34 mm, 19.35 percent (4/4) | 1/4 right, 0.010 percent
black table           0    0.09 mm,  0.01 percent (4/4) | 0/4 right, 0.022 percent
black table           2    2.86 mm,  1.93 percent (4/4) | 0/4 right, 0.045 percent
black table           5    7.11 mm,  4.79 percent (4/4) | 0/4 right, 0.019 percent
black table          10   14.24 mm,  9.70 percent (4/4) | 0/4 right, 0.276 percent
black table          20   28.33 mm, 19.38 percent (4/4) | 0/4 right, 0.079 percent
```

  Brackets must touch: touching, the corners are within 0.1 mm and the size within 0.06 percent; each millimetre of gap moves each
  corner about 1.4 mm and makes a 12 in target read about 1 percent larger, 2 mm about 2 percent, the scale itself unchanged. Bars may
  touch or lie apart: the scale is 0.01 to 0.28 percent at every gap. (The bars' corner column is confounded: the brackets were in the
  same scenes; on Alan's real photos the bars once joined the outline, and are now painted out.)
- **Section 3, not done in this run's budget:** the printed border as its own cue, right angles after perspective as a score, the
  outline live on the camera preview with a tap as a hint, and a surface suggestion where contrast is the problem. Pictures:
  C:\Dev\grouplab-local\surface-trial-2026-10-04\ (one scene a surface, the truth in magenta, the outline green where found and orange where not sure).

## Alan's photos on wood (requests 74 and 76, 2026-10-04)

- **Corners.** The light kitchen table: 4 of 4 right. The dark grained floor: only the tilted Shoot-N-C; the others ran onto the boards'
  seams (weakest side on an edge 0.67, 0.75, 0.88) and were called found. Now below 0.9 the finder says it is not sure and offers the
  outline as a guess; every right outline here, on the counter and on the scans had 0.92 or more. A largest-bright-region candidate was
  tried for the floor and scored 0.69 and 0.00: the floor itself is not solved.
- **Bars** were taken into the target's outline, an inch too tall; painted out of the corner search they give 11.88 to 11.99 in on both
  targets. **Brackets** give the rectangle they enclose: 12.05 in (Rigid, touching) and 12.21 in (Eze-Scorer, a gap on purpose).
- **A light card on white paper** was not found: its edges are faint and the target's grid lines run into them. It is now found by its
  magnetic stripe, which runs the card's width: the near long edge searched beside the stripe, the far one placed a card's height across.
  Eze-Scorer 11.98 in (one bar 11.99), Rigid 11.87 in (bars 11.91 to 11.95); half a percent of doubt added for a stripe set in. The
  card photos stay local and in no test, as Alan's notes ask; the computer-made trial is unchanged (30 of 40 cards, none wrong).

## Entry 363 sections 3.4 and 3.5: the phone's failures, and its sender

- **3.4.** `ProblemSheet.Stop` puts a failure that stops the work on the centred sheet over the page it happened on, its choices first,
  and puts the page back on the x or any choice. Converted: a photo Add a store-bought target cannot use, its file not written, the
  one-page report, CSV and chronograph files not read (five places), a share picture, a sheet that cannot be printed, a print or share
  the phone refused, a board not measured. Information (saved, shared, sent, progress) stays on its line. `Entry363StopTests`.
- **3.5.** `PhoneSending`: the computer's package (`TargetPackages.Build`) and queue (`TargetSender`, now its own file, compiled by the
  phone), sent when the choice is to send every target, asked about with Ask me each time, kept for Wi-Fi on mobile data unless allowed,
  retried at start; never a picture with a bank card in it. Off in every phone build behind `sendTargetsPhones` until Alan writes
  "forms updated" (Photos joins the store forms' checklist). `Entry363SenderTests`.

## Entry 363 section 3.3: the "looks like a GroupLab sheet" check

- The check's cost was locating QR codes a second time: `OpenCvSharpBackend.LocateCodes` ran WeChat and DetectMulti at full size, 8.3 of
  its 8.5 s on Alan's Rigid crosshair photo. Identification had just read the same picture at full size with the same two detectors.
- Now the full-size reading keeps where it located codes, for that picture object, and the check reuses them: 8.4 s to 0.15 s there, and
  0.05 s against 0.9 to 1.1 s on three corpus photos of sheets, with the same markers and codes found every time (`Entry363LookTests`).
- Scale markers (entry 365) are left out of the marker count, so brackets beside a target do not make it look like a GroupLab sheet.
- Identification itself took 33.9 s on that photo; question 83.

## Entry 365: scale markers beside a target, A to D

- **One engine.** `ScaleMarkerLayout` keeps tag36h11 555 to 586 for markers (the library's sheets reach 150; the validator warns of any
  that would reach 555). `MarkerFit` fits one plane to every bracket, bar, board and card found, each a rigid body of known shape, its
  doubt from the fit's own covariance; three or more brackets are held softly to a rectangle (0.5 mm, 0.5 degree). `ScaleMarkerFinder`
  reads the codes at four sizes and finds a card by its rounded corners, opposite sides and fill, refitting its sides at full size.
- **Accuracy, `grouplab marker-trial --photos 20`, seed 365,** computer-made photos tilted up to 30 degrees, markers printed exactly
  (error = larger of width and height, percent; columns: found, median, 95th, worst, claimed median, within claim):

```
brackets   12x12    20/20    0.068   0.131   0.185      0.171           19/20
brackets   23x35    20/20    0.203   0.696   0.753      0.353           17/20
one bar    12x12    20/20    0.054   0.334   0.575      1.278           20/20
one bar    23x35    16/20    0.061   0.509   0.525      1.004           16/16
two bars   12x12    20/20    0.074   0.444   0.452      1.045           20/20
two bars   23x35    19/20    0.064   0.336   0.838      0.997           19/19
board      12x12    20/20    0.015   0.025   0.026      0.112           20/20
board      23x35    19/20    0.014   0.023   0.028      0.113           19/19
card       12x12    19/20    0.161   0.265   0.313      1.232           19/19
card       23x35    11/20    0.135   0.251   0.251      1.115           11/11
all        12x12    20/20    0.015   0.023   0.024      0.247           20/20
all        23x35    20/20    0.014   0.027   0.072      0.247           20/20
```

- **What the trial changed.** A hard bracket rectangle took 0.3 mm of placement into 1.2 percent, so it is soft. Fitting codes by their
  centres was worse than by their corners (brackets 0.13 against 0.04 percent). 22 mm bracket codes on 28 mm arms were worse than 16 mm on
  22 mm (thin white border). Bars' codes went from 10 to 12 mm (inch) and 16 mm (metric), which took the poster bars from 4 to 16 of 20
  found. The card finder took a bracket's L and a silhouette for cards until it refused quads near codes and ones not filled or not
  nearly parallelogram.
- **Where it shows.** Add a store-bought target: Markers in the photo (chosen by itself when found) and A bank card in the photo; four
  brackets replace the corner finder's corners. Marking by hand, computer and phone: the scale is set from markers by itself. A card is
  blanked in every copy at once, the marking is made on a blanked copy, and no picture with a card in it is ever sent.
- **Not done:** the Features page's own picture (listed in docs/figures/SCREENSHOTS.md). Request 76: the figures on real paper.

## Entry 364: Alan's seven reference files

- Every file read and recognized its own photograph (836 to 1511 features, layout 0.979 to 0.999) and none of the other six.
- Added to the built-in library, now nine: Shoot-N-C 12 in 5-bull sight-in (0.61 percent), Eze-Scorer 12 in sight-in grid (0.62), Rigid crosshair (0.74), ST-4 (0.56).
- Held: Allen splash bull 55124A (7.39 percent) and the Eze-Scorer bullseye (7.38), both from two points an inch apart; Allen EZ Aim 55134A (1.86, 12.8 by 13.6 in) until a tape measure. Its photo's printed inch squares put the paper at about 13.6 by 12.5 in, so the paper, not the scale, is what exceeds the package's 12 by 12 in.
- The Size step now says to measure far apart, and the last step warns when the scale is worse than 2 percent.

## Entries 358 and 359: thermal label targets, a printer framework, darkness

- **Sections 1 to 3** (a worker's three commits, merged after the three suites): 4x6, A6 and 100 by 150 mm pages beside Letter and A4; the thermal print mode (one bit at the head's own dot pitch, every code and marker on whole dots, black only, never fitted to the page, the cut-off named before printing); the X6 labels, six of the Letter sheets' bulls to a label in sets of five that pool, and a printer check label.
- **The framework** (`src/GroupLab.Core/Printing/Labels`): a printer is an entry in `printers.json` (services, name hints, transports, characteristics, dpi, head dots, page widths, chunk and pacing, density and speed ranges, media codes, encoder, tested, source) plus at most one encoder. Encoders written here for TSPL (rows turned over, since its BITMAP takes a set bit as paper), the Phomemo ESC family, ZPL and ESC/POS, each tested on exact bytes. Three profiles ship, none tested; no M220 profile (section 5), none for the M834 or the 4x6 printer until their recordings (requests 73 and 72).
- **Not built:** the radio on any platform. `IOutsideWorld.FindPrintersAsync` and `ConnectPrinterAsync` answer "nothing" everywhere until a printer has been recorded, so a test can never open a real connection and nothing claims a printer works.
- **Darkness:** the test page (a strip per setting: its number, a solid square, lines 1 to 4 dots, squares 3 to 6 dots) and the rule (the darkest setting whose lines grow by no more than half a dot) are built and tested; reading the photograph and each printer's real ranges wait on the test printers.
- **Entry 359:** requests 72 and 73 rewritten; nothing built used USB to the M834.

## Entry 362: store-bought targets, upright, their corners found, zoomed and turned

- **Why every photo failed.** `SheetOutline` takes the largest light region by brightness; on Alan's white counter that region was the counter, which touches the frame, so all five were refused as "runs out of the frame" and the screen offered a fixed rectangle. Four of the five carry orientation 3 or 6, which the screen ignored (`FingerprintSession.Load` decoded with `IgnoreOrientation`).
- **The finder** (`StoreTargetOutline`): colour edges (lightness and both colour axes, the colour axes weighted 2.5), printed lines left out by the colour beyond both sides, a second Hough for long faint edges; the region whose colour differs from the picture's border (target paper is bluer than a cream counter); the sheet finder's light region; each outline snapped side by side to the outermost consistent edge, scored by its weakest side and by how far its edges run on past its corners, then refined at full size, averaged along the side, twice.
- **Alan's five photos** (4000 by 3000): sight-in, splash bull, Eze-Scorer bull and Eze-Scorer sight-in grid found, corners within about ten pixels by eye on crops, about twenty five on one corner of the grid; Rigid crosshair not found (its edge shows on about a fifth of each side), guess within about twenty pixels. 0.3 to 1.0 s each.
- **The 600 dpi scans**: four found, two or three sides on the scan's own edges where the target reaches them; the Eze-Scorer bull is larger than the scanner and is the whole scan, not found. The GroupLab sheet cases and the stand-in poster are unchanged; `SheetOutline` itself was not changed.
- **Alan's second set, 2026-10-04**: the NTC ST-4 and the Shoot-N-C 12 in sight-in, both found, corners within about ten pixels by eye. The Shoot-N-C lies beside the counter's own edge, which first made a larger outline; an outline is now passed over where a smaller one inside it passes with a strip of counter between them (at least a twentieth of it, so a paper's pale margin does not count). Six of the seven photos are found. With the packages' names and printed sizes (all 12 by 12 in) the five packaged targets go through all five steps; the bull finder finds none on the two sight-in grids or the splash bull, where the person adds them by hand.
- **Placing a corner**: wheel, pinch and trackpad zoom to twelve times, a drag pans once zoomed, a magnifier four times larger above the finger while dragging, and on release a snap to the strongest corner within a hundredth of the picture where it is at least four times the typical strength and twice any rival, with Undo. Rotate left and right on the Photo and Straighten steps turn everything placed with the picture.

## Entry 361: the finishing flag, and a live reading

- The hook now follows entry 360 as amended: under 85 it lets every call through; from 85 it blocks unless `docs/notes/finishing.flag` is under 45 minutes old; from 88 it blocks whatever the flag says. The six cases above were run by hand with sample files.
- From a terminal the status line runs and `docs/notes/usage-now.json` stays fresh: 62% of the week at 03:06 UTC, 2026-10-04. Each block's cost goes in the local `docs/notes/usage-log.md`.
- The Android emulator's red real taps on 2cce12a1 (run 37118457310) were not GroupLab: the emulator's own launcher stopped answering and Android's "Pixel Launcher isn't responding" box covered the screen, so every tap after the first went to the box. Both emulator scripts now switch those boxes off (`hide_error_dialogs`); GroupLab Dev's own crashes are still caught from its log.

## Entry 360: a hard stop at 85% of the week

- The hook refuses every tool call once either reading of the week (the status line's file, or Claude Code's cached figure) is 85 or more; about 32 ms a call; with no reading it lets the call through, as section 3 asked.
- The status line does not run in the VS Code extension, so this session had no fresh reading after 09:55 UTC (60%). Under section 4 it finished the step in hand of entry 358 (sections 1 to 3, unmerged on the worker's branch) and stopped at 11:14 UTC.
- Not done: the finishing flag and the 88% backstop (refused by the session's safety check; Alan's to decide), and per-block measuring, which needs a live reading.

## Entry 376 Part A: which bull each shot belongs to (2026-10-06)

- **Why the tablet's two runs disagreed.** Both runs detected the same 14 holes (the log shows identical stages); the difference is the
  hole Alan added by hand at bull 10, at the paper's right edge. The rule entry 374 added let one hole in ten fall outside the shooting
  order, so with 15 holes the reading a row higher (bulls 6 to 20) passed as well as bulls 1 to 15, the two tied, and the sheet fell back
  to plain matching: bulls 1, 3 and 6 to 20, "Shot 20" first. Scanned over 399 places a thumb could put that hole (x 7.5 to 8.45 in,
  y 4.2 to 5.2 in), the old rule found no offset at any of them; session 7 came out right only because its hole sat on the knife edge
  the other way, at the phone's working resolution.
- **The matching is now one problem** (`ImpactOffsets.ReadWholeSheet`): every reading is a one-to-one (Hungarian) matching around one
  common offset, re-centred on the median and repeated, seeded from two holes against every bull. Holes that fit the bulls as aimed, with
  no offset, as well as any reading are read that way, so an ordinary sheet is untouched (scan 4 of 20 September showed why this has to
  come first: with its holes partly merged, a shooting order 0.8 in off fitted almost as well). Otherwise the shooting order, bulls 1 to
  k for the fewest k that fit, is taken when it fits as well as any reading and its offset is under the spacing between bulls; then a
  single reading that clearly fits best; otherwise GroupLab asks.
- **The question**, "Which bulls did you fire at?", in the middle of the screen on the desktop and the phone (`MarkingSession.WhichBulls`),
  GroupLab's guess first (the smallest offset), up to two more readings, and "Choose the bulls myself". An answer is the same rule
  "Bulls you fired at" sets, which always wins. Asked once per result; dismissing it keeps the guess, which the figures already use.
- **On the tablet photo**: bulls 1 to 15 at all 399 places, without asking; bulls 1 to 9 and 11 to 15 as detected. Tests:
  `ImpactOffsetTests.TheTabletPhotoWithTheHandAddedHoleIsBulls1To15` (five places), `TheTabletPhotoAsDetectedLeavesBull10Empty`,
  `AShotSheetThatFitsSeveralWaysAsks`, `ASheetShotFromBull1WhereAimedIsLeftAlone`; `WhichBullsTests` through the session on a rendered
  C bull sheet (the hand-added hole, and the question answered); and on the photo itself where it is on this machine (kept in
  `C:\Dev\grouplab-local\tablet-2026-10-05\`, never committed).
- **Lines (A3)**: a thin line from each bull's centre to every hole given to it, on the desktop's picture (it was a faint dashed line,
  now solid), on the phone's result picture and in its Fix holes.
- **Lighting (A4)**: a click or tap on a bull, a hole or the line between them lights all three in the selection colour; the phone says
  under the picture where that bull's shots landed. Bull by bull is now on every sheet of bulls on the desktop, each row a button that
  lights its bull, and on the phone each row says its offset and lights its bull when tapped.
- **The bull for a hole (A5)**: on the phone, after Add a hole here or Put the hole here, "Which bull was this hole fired at?" with
  GroupLab's choice first and the five nearest bulls; another choice fixes the hole there. On the desktop a new hole is selected and its
  bull picker, GroupLab's choice already in it, is in the selection panel, as it was.
- **Names (A6)**: every shot is "Bull 7", or "Bull 7, shot 2" (`ShotLabel.Name`), and "the shot on bull 7" inside a sentence, on the
  Shots pages, the CSV (in bull order), the legend ("extreme spread, bulls 1 and 15"), the review queue, the undo list, the desktop's
  status line and report. The holes in Fix holes carry their bull's number, not a count. The picture's small labels stay the bull's number.
- Suites: Core 2890 passed and 2 skipped (the inbox test cleared with this fold); App 496 with the eleven tests that held the old names and the plain dashed line updated; Mobile 140 passed.
- **Worth an article?** Recorded in `docs/RESEARCH.md`: yes, as an update to the wrong-bull article, once the phone pictures exist.

## Decision log

One line per method choice where there was a real alternative: what was rejected, and why.

- **M0: `markerSize` is 8 modules, over the brief's 10.** TARGET-SCHEMA.md section 3.7, the renderer and FIDUCIAL-DECISION.md all put 8 modules in `markerSize`; 10 would print the wrong modules and two of the five sheets would not render.
- **M0: quiet zone of two modules, over a fixed 10 dmm.** It keeps each marker in the proportion FIDUCIAL-DECISION.md settled at 0.5 mm, so only the module scale changes.
- **M0: `GL-CF25-LTR` as printed, over the 454 dmm sighter gap of the pending geometry change.** The 0.5 mm sheet is then the Phase 0 sheet by identifier, which makes it the sweep's control; the sighter geometry does not bear on the module floor.
- **M0: each sheet's lattice derived at its own footprint, over forcing the 0.5 mm lattice onto all five.** The derivation rule is the format's, and an explicit lattice would be a different fiducial scheme; the count difference is handled at measurement by registering from the shared positions.
- **M0: a footprint parameter in `tools/layout/layout.py`, over a C#-only generator.** CONTRIBUTING.md makes the layout tool the authority; the C# derivation is checked against it marker for marker, and `layouts.json` is byte-identical with the parameter at its default.
- **M0: sheets named by module, frozen by identifier once printed.** Names a person can match to a printout while the set is live, and the entry 11 rule when it becomes a measurement input.
- **M1: the cross-section as a tangent angle integrated along arc length, over a height function of page position.** Integration makes the page-to-sheet map an isometry by construction; a height function stretches the sheet wherever it slopes.
- **M1: a cubic tangent angle, three coefficients, over five.** The brief's lower bound; noise-free recovery is exact at every bend swept, and each extra coefficient is more bend for noise to fit.
- **M1: corner residuals in image pixels, over page dmm.** A detector's corner error is a pixel quantity; reclassification and selection stay in page dmm so the Phase 0 distances mean what they meant.
- **M1: six starting ruling angles, over a single start.** The ruling angle has no gradient until the sheet bends, so one start can settle on the wrong family.
- **M1: focal length refined from the brief's EXIF estimate, over fixing it at the estimate.** Starts from 0.55 to 1.80 times truth all refine to within 1.4 percent; a fixed wrong focal length leaves the pose unable to reproduce even a flat sheet's homography.
- **M1: reclassify every corner at 12.7 then 2.54 dmm, over 2.54 only.** The rendered 1.00 in bow: 96 of 136 corners and a fail, against 136 and 0.00040 in.
- **M1: an F test at p = 0.001 for keeping the bend, over always fitting it or a fixed residual threshold.** Always fitting doubled a flat sheet's worst bull; a residual threshold would depend on scale and marker count, which an F test does not.
- **M1: synthetic corner noise from `main_flat1`'s residual, 0.52 px per axis, over the 0.16 px a clean render gives.** The render's figure would flatter the model; the flat frame's includes whatever its flat model left, so it is a ceiling.
- **M1: a forward-difference Jacobian, over analytic derivatives.** Fourteen parameters, and noise-free recovery to under 0.00001 in shows it is accurate enough.
- **M1: a bull whose edge profile has under 3 or over 4096 samples fails with a reason, over clamping the sample count.** A clamped count would still sample a mapping already known to be degenerate and report a centre from it.
- **M1: one starting focal length per joint fit, the EXIF candidate with the lower alone-fit residual, over the group median or a vote.** Four of the seven 2.2 mm frames carry one 35 mm equivalent and three the other, so a median or vote decides by frame count. Measured on those frames the choice does not matter: the costs are 0.1 percent apart and the fitted results are identical from either start.
- **M1: the joint-fit lens key left as focal length and f-number, with the table frames reported rather than regrouped.** Entry 6 settled the key, and regrouping after seeing these frames degenerate would be fitting the method to them.
- **M1: the lens held on the mounted frames was fitted flat and shared on the camera's flat frames, over the median of Phase 0's per-frame lenses or a fit on the full-marker bent frames.** A flat frame has no bend for the lens to absorb, a median breaks the pairing of k1 and k2, and a bent frame is the absorption being tested.
- **M1: the leftover-shape diagnostic on two terms, curvature along the rulings and a saddle, over a saddle alone.** A twist is a saddle only in the truth's rulings: the synthetic fit turned its rulings to 72 degrees, and in turned rulings a saddle is a difference of squares, half of which the bend absorbs.
- **M1: the real-frame lens run written and started before the sweep's numbers were read, over adjusting it to them.** Brief section 3.3's rule applied a second time.
- **M1: M1.2's "at random" marker rows relabelled as the first markers in raster order, over re-running them at random.** The label was wrong and the numbers right, and they are cited in entry 13; M1.7's sweep carries a random coverage of its own.
- **M1: corner quality placed on the sweep by a robust sigma over all corners, over the RMS of the kept corners.** The kept corners are truncated at 2.54 dmm, and their RMS stays between 0.9 and 1.2 px from 1 px of noise to 5.
- **M1: joint fits keyed on physical focal length, f-number, 35 mm equivalent and image size, over the equivalent and image size alone.** Entry 16 section 2 asks for the pixel geometry; the equivalent alone would join the cropped table frames to the main camera, which shares their 23 mm and their image size but not their distortion.
- **M1: the general developable surface as folds along turning rulings 10 dmm apart, over a smooth parametrisation of a tangent developable.** Rigid strips keep the isometry exact for any turn and reduce to the cylinder, within 0.018 dmm, when the rulings do not turn.
- **M1: a general surface whose rulings cross inside the page is undefined, over letting it fold through itself.** No sheet of paper takes that shape, and a fit that could reach it would report a shape that cannot exist.
- **M1: the minimiser takes a backward difference, and holds a parameter, at an undefined boundary, over a smooth barrier penalty.** The change leaves every fit that never meets the boundary unchanged, which the cylinder's raw rows confirm; a barrier would change every general fit's cost.
- **M1: stopped at the general surface with its joint fit unconverged and its alone fits as the measurement, over constraining its turn on weakly bent frames.** Entry 16 section 5 stops at the general developable surface, and a constraint would be a further model decision.
- **M1: one frame at a time by default, over the joint fit.** A user photographs one target at a time, the lens barely matters (M1.7), and sharing a camera cost `main1` a factor of two; the joint fit stays behind `--joint`.
- **M1: fewer than eight kept corners select the plane, over the bend.** Less evidence has to mean fewer parameters; the old default preferred the model with more.
- **M1: the correlation diagnostic on per-marker mean residuals between neighbouring markers, with a permutation null, over corner pairs or a variogram.** Corners of one marker share their detection and would read as structure that is not the sheet, and a permutation test needs no model of the noise.
- **M1: the mounted gate left at 0.005 in and recorded as open, over a separate looser gate.** Entry 17 section 2: the error budget argues for 0.003 to 0.005 in, and a number chosen after seeing the results is not a gate.
- **M2: morphology and blob extraction behind the imaging backend, the arithmetic, filters and measures in Core, over the whole primitive in Core or the whole primitive in the backend.** Core keeps no OpenCV dependency, and the port can be checked against the survey step by step because the backend calls are the survey's own.
- **M2: the baseline compared through the survey's own roll-up band, 0.15 to 0.55 in, over the detector's 0.60 in.** The survey's tables were computed through that band, and two detected holes fall between the two.
- **M2: render-and-difference opens with a 0.012 in disk, over the survey's 0.032 in.** After differencing only registration slivers remain to remove, and the survey's disk erased thin rims around pale cores, which were both detectors' misses.
- **M2: each bull's cell aligned to the observed image by phase correlation, over trusting the registration or opening wider.** The rings are fiducials already printed, and a wider opening would bring back the faint-hole misses.
- **M2: rim closure recorded and not gated, over a threshold between arrowheads and holes.** The two overlapped on synthetic sheets, and the synthesis draws no C-shaped rim, the case the signature exists to tolerate.
- **M2: merged neighbours split at an elongation of 1.45 by two-means, over refusing them as too large.** Refusal lost both holes of a pair; a split reports two and flags them.
- **M2: the oversized flag against the sheet's median, over a calibre size window.** The synthetic sheet carries no calibre; the median's failure when every bull holds a pair is reported.
- **M2: the synthesis calibration stopped at four iterations with its gaps reported, over iterating until it matched.** Each iteration moved one quantity at the cost of another, and the gaps' direction says which way the recall errs.
- **M2: thresholds set on seeds 1 to 3 and the gate read on seeds 1001 to 1003, over one seed set.** A threshold read off the seeds it is gated on is fitted to them.
- **M2: truth matched nearest pairs first within 0.15 in, over an optimal one-to-one matching.** Scoring should not share the rule of the assignment it is used to evaluate.

- **M3: special functions written for the engine, over a numerics package.** Nothing may be installed, and section 15.3's 1e-12 needs control of every step: the incomplete gamma prefactor through R's deviance form, and every upper tail computed as a tail.
- **M3: CorrNormal CEP through a trapezoid rule over angle on the Hoyt CDF, over Imhof's integral or a series.** The integrand is smooth and periodic, so the rule converges geometrically and matches shotGroups' hit probabilities to 1e-15.
- **M3: the minimum-volume ellipse by Khachiyan's algorithm at shotGroups' 0.001 tolerance, over an exact solver.** Section 15.3 says to match the tolerance or expect disagreement; at the package's tolerance it agrees to 3e-13.
- **M3: range statistics from ten groups per replication with running means, over simulating each group count separately.** Every cell keeps its full 10 million independent replications at a fifth of the cost of fifty-five groups.
- **M3: quantiles from 8,192-bin histograms, over storing ten million values per cell.** One bin is 7.3e-4 of the mean against a 5e-3 tolerance, and 65,536 bins ran three times slower for no measurable gain.
- **M3: the table read between rows by R's fmm spline on shotGroups' own grid, over simulating every n or interpolating linearly.** At n = 92 the spline reproduces shotGroups, and linear interpolation is 4.8e-5 off.
- **M3: a key held back only on evidence checked key by key, over excluding by dataset or loosening a tolerance.** The point of aim is detected from the fixture's own two centres, a Monte Carlo p-value by being an integer over 9999, and a disputed CEP by not being a root of its own distribution.
- **M3: the Fligner-Killeen statistic at 1e-10 relative, over 1e-12.** It subtracts n mean^2 from a sum of squares of up to 530 normal-quantile scores, which costs about three digits; the worst difference is 2.4e-12.
- **M3: the flyer expectation as an integral, over section 10's alternating binomial sum.** At 25 shots the sum's terms reach 5.2 million with alternating signs; the integral has no cancellation.
- **M3: the pre-pooling guard as pairwise F tests with Holm's adjustment, over Bartlett's test of homogeneity.** Section 11 names section 8.1's F test, and section 8.4 names Holm for its family.
- **M3: the engine's own xoshiro256** generator for resampling, over the runtime's Random.** Section 6 requires a recorded seed to reproduce an interval exactly, and the runtime does not promise its stream across versions.

- **Entry 20: bull centres from ring fits confirmed by centre dots, over the survey's annulus matched filter.** Holes break the rings and the scan's rings carry light stripes, so neither survives as a clean blob, while the dots do; the dot centroids are kept beside the ring centres and agree to a median 0.0019 in.
- **Entry 20: grid orientation found from the bulls, over trusting pixel order or reading EXIF orientation.** The photograph's pixels are stored a quarter turn from upright; lattice phase and the sighter line's 0.2-pitch offset place the grid whatever the storage, and an unmirrored layout picks the column direction.
- **Entry 19: the hole detector run on the whole photograph and on the registered sheet, over the whole photograph only.** Untuned on the whole frame it finds nothing, which is the finding; the sheet-only run shows what registration would let the same primitive do, and is labelled as that.

- **M4: the marking screen and the correction screen as one screen over one model, over a separate manual mode.** Entry 21 section 3 and DESIGN.md section 13 describe the same interactions; one immutable model gives both undo and a test surface that needs no window.
- **M4: the image shown from the pipeline's own OpenCV decode, over Avalonia's decoder.** The two can disagree about EXIF orientation, and a mark must land on the pixels the statistics and the detector use.
- **M4: the image sized by its decoded pixel grid, over the bitmap's size.** The bitmap's size follows the file's DPI tag; the headless test showed marks scaling with it.
- **M4: the UI built in C#, over XAML.** Avalonia 12 compiles bindings by default and the screen is one window; code keeps every control's wiring in one place a reader can follow.
- **M4: a headless test through the platform's pointer input, over calling the model directly.** It caught two faults that would have reached a person, which the model tests could not.
- **Entry 24: a dispersion figure withheld below five shots, over printing it with a warning.** A warning beside a three-decimal headline is what Alan read past; the count and the centre offset stay, because they are exact at any count.
- **Entry 24: each interval labelled with its exact coverage, over removing the c4 correction from the endpoints to make them 95 percent.** Brief section 5 has GroupLab match shotGroups' intervals; stating their coverage keeps that and stops the label from lying.
- **Entry 24: extreme spread's interval in the form that covers the expected spread, over shotGroups' `getRangeStat` form.** The latter covers 84.7 percent at two shots; entry 23 section 1 says to stay exact where shotGroups is not, and the harness still checks its form.
- **Entry 26: rotation as a view property of the marking state, over rotating the decoded pixels on load.** Rotating pixels changes the frame every saved position is in; a view property moves no mark, gives undo for free and reopens as it was left.
- **Entry 26: the stored pixel frame as the canonical frame, over the upright displayed frame.** It is the frame M4.1 files were already written in, so version 1 migrates without moving a mark, and it can be checked against the file itself.
- **Entry 24 section 5: the hole-size flag at nominal plus 0.132 in, over a ratio of the calibre.** Section 3.5 found the hole deficit roughly constant in absolute terms rather than proportional.
- **Entry 25: one application-wide unit setting, over a unit dropdown beside each input.** Entry 25 section 1 asks for it, and a setting that every figure obeys cannot leave one figure behind in inches.
- **Entry 25: "mil" as the milliradian, over the 6400 NATO mil of section 12.5's table.** Turrets are marked in milliradians; offering the NATO mil under the same name is the dialling error entry 25 warns about.
- **Entry 25: lengths and distance stored in inches whatever the setting, over storing in the unit the user worked in.** A file then means the same on every machine, which the test asserts by writing one marking under both settings.
- **Entry 25: printing through the system's PDF print command, over drawing pages to a printer from the application.** Avalonia has no printing API and nothing may be installed; the PDF is the path that already works, and what it cannot control is said on the screen and on the sheet.
- **Entry 25: the actual-size note as a render option, on in the print screen and off by default, over adding it to every render.** Phase 0's pages and the gates measured on them stay item for item as they were.
- **Entry 25: `/PrintScaling /None` in every PDF GroupLab writes, over only the print screen's.** A sheet printed from the command line is measured the same way and deserves the same request.
- **Entries 22 and 27: the scrubber in C#, over running `scrub_exif.py`.** Its library is not installed and nothing may be installed; the C# version follows the script's policy, keeps digital zoom for entry 27, and also removes XMP and trailing data, which the script leaves.
- **Entry 27: triage by decoded markers, holding rather than refusing what fails it.** Markers are the check the application already has; a held file costs a person one look and an `--accept`, where a refused one would need resubmitting.
- **Entry 22: the committed-image guard names the 16 photographs with GPS, over failing the suite until history is rewritten.** The rewrite is a decision for question 13; a named list keeps the suite green without letting a seventeenth image in or letting the list go stale.
- **Entry 28: rebuilding the aimed coordinates from the shot and a recovered aim, over excluding the four Fligner-Killeen keys as a known difference.** The rebuild reproduces R's doubles and all four statistics to 5.5e-13, so the gate checks something true rather than recording a gap; regenerating the fixtures at full precision would make it unnecessary.
- **Entry 28: a stated digital zoom of 0 read as 1 in the lens key, over keeping the tag's value.** The EXIF standard defines 0 as digital zoom not used, which is the geometry of 1, and every Pixel photograph in `scans/mounted/` states it.
- **Entry 28: refusing a `meta.json` whose opt-out field is missing, over treating it as false.** A missing opt-out is unknown, and publishing on unknown consent cannot be undone.
- **Entry 33: `grouplab analyze` composing `AutomaticMarking` and `GroupAnalysis`, over a separate pipeline for the command line.** The screen and the command then run the same code, so a fault found by one is fixed in both, which is how the sighter fault reached the marking screen's fix.
- **Entry 33: a printed sheet analysed even when its definition fails today's validator, over refusing it.** Validation decides whether to print a sheet; a sheet already on paper is what it is, and refusing it would make every Phase 0 sheet unanalysable.
- **Entry 33: `--target` required, over guessing the definition from the image.** Nothing reads the printed identifier or codes yet, and the built-in definitions share marker ids.
- **Entry 34: two of the owner's photographs held, over publishing all 28.** A screenshot and a messenger download cannot show who took them. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one until Alan confirms costs nothing.
- **Entry 34: a donated submission whose files were all held published as a provenance record alone, over leaving it out.** The record shows that a consented submission arrived and why none of it is published, which is the question a contributor would ask.
- **Entry 34: a separate `publish-owner` path, over running the owner's photographs through `intake`.** Intake requires a consent record, and entry 34 section 2 rules out inventing one.
- **Entry 35: a missing camera make alone is enough to hold a file, over requiring a copy's file name as well.** Entry 35 section 1 calls the make the stronger signal. A person can still accept a held donated file by name, so a false hold costs a look, and a false publication cannot be undone.
- **Entry 35: an edited phone copy that keeps its camera metadata, the four `~2` photographs, not held.** The rule is about lost camera geometry. Those copies keep their make, model and focal length, and nothing in entry 35 asks for more.
- **Entry 37: a submission whose consent cannot be read withholds its files' hashes, over ignoring it.** The rule exists because publishing under ambiguous consent cannot be undone, and an unreadable `meta.json` is the most ambiguous consent there is.
- **Entry 37: a file held for a consent conflict cannot be accepted by name, unlike a file triage holds.** Triage is a judgement about usefulness that a person can overrule; an opt-out is the contributor's decision, and only the contributor can change it.
- **Entry 37: the two conflicted submissions not published even as provenance records.** Publishing a record of a submission whose consent is in question, with its answers and credit name, waits for the contributor's answer as the photographs do.
- **Entry 36: `DFdistr` left at its committed version, over committing a JSON whose numbers are strings or editing `sg_distr.R` here.** Nothing reads it, `tools/` is the authority and planning's to change, and a fixture committed in a broken shape would be read as correct later.
- **Entries 39 and 40: any unassigned shot on a sheet of several scoring bulls withholds every figure, over quoting the assigned shots alone.** Leaving shots out without saying so would be a different wrong number, and the sentence in their place tells the user exactly what to do.
- **Entry 40: a tap on printed ink placed where it was tapped, with a note, over snapping onto the ink and naming it.** The centre of a printed stroke is never the answer, and a tap placed where the user put it is at worst as wrong as the user.
- **Entry 39: a moved shot follows its nearest bull unless the user assigned it elsewhere, over keeping whatever bull it had.** A shot dragged across to the next bull is almost always a correction of position, and a deliberate reassignment is recognisable because it differs from the nearest.
- **Entry 39: an impact placed by press, drag and release, over click, drag, click.** It is one gesture with a finger or a pointer, and a plain tap still places a shot.
- **Entry 42: a text colour below 4.5:1 moved along its own hue, over changing which role the text takes.** Section 2 says so. The contrast is measured on the three surfaces text sits on, not on the sunk image area, which carries marks. Counting the image area too would have pushed light `faint` almost onto `dim`, erasing the difference between the two.
- **Entry 42: styles rebuilt when the theme changes, over binding each control to a theme resource.** The shell is built in code, and rebuilding one style set keeps every colour decision in `AppStyles` and `Tokens` rather than spread across every control.
- **Entry 42: a unit left at its figure's size for now, over splitting the figure text.** The existing tests read that text, and section 1 requires them to pass unchanged.
- **Entry 42: mark labels in light text on a dark plate, over text in the mark's colour.** The mark's colour as text could not be read on the first screenshots, and the colour survives as a bar beside the number.
- **Entry 41: image facts from the metadata GroupLab already reads, over reading the further facts section 2 permits.** Bit depth, lens model, ISO, exposure and a count of EXIF tags would each mean reading more of a photograph's metadata for the sake of a log, which is the step the rule exists to stop.
- **Entry 41: the directory named in symbols on the first log line, over the resolved path.** Section 3 asks that the first line report where the log is, and section 3 also forbids a path. `%LOCALAPPDATA%\GroupLab\logs` satisfies both.
- **Entry 45: `last_action` as the name of the last event logged, over a separate list of action names.** Every user action already writes a stable event name, such as `print.select`, and a second list would drift from it.
- **Entry 41: Send saves the package before sending, into the log directory when the user has not saved it.** Section 7 says the zip is kept if sending fails, and the only way to guarantee that is for it to exist before the attempt.
- **Entry 41: a crash record not rewritten when a second crash lands in the same second of the same process.** The receiver's name pattern leaves no room for a counter, and the first crash is usually the cause.
- **Entry 46: an alert ring at the measured size on a flagged hole, over every impact ring at its measured size.** The size check reads an extent only for a dark region on paper, so a measured ring for every shot would silently fall back to the calibre on ink and on a dark backer. Every ring stays comparable, and the one that disagrees shows by how much.
- **Entry 35 section 6 item 3: the definition from the sheet's QR codes, over choosing among definitions by registering against each.** A frame that passes its CRC names one definition. A registration that fits well against the wrong definition is possible, because the built-in definitions share marker ids.
- **Entry 35 section 6 item 3: refuse and ask when the codes do not settle it, over falling back to the nearest match.** Eight of the 37 Phase 0 images are not identified and fall back to naming the definition. A wrong definition would produce a plausible wrong group.
- **Entry 35 section 6 item 3: the frozen Phase 0 definitions shipped beside the application, over the live library only.** The sheets already printed carry the frozen identifiers, and the print screen's list does not show them.
- **Entry 37 section 5: no size recorded for a note with no unit, over assuming inches.** Every size in the first notes had its unit, and a wrong assumption is a scale error of 25.4 or 2.54 times, which the marking screen could not detect.
- **Entry 37 section 5: the stated size offered in the rectangle prompt, over setting the scale from it.** The size is the sheet's, and only a person can say which corners are the sheet's and which side was tapped first.
- **Entry 37 section 4: the scrubber's keep list left as entry 29 set it, with `LensModel` reported rather than added.** Adding a field to what is published is a publication decision, and the lens grouping does not need it.
- **Entry 46 section 3: the count line replaces the "Placed:" line, over keeping both.** They carried the same three numbers, and the one planning asked for sits above the figures where it is read first.
- **Entry 35 section 6 item 2: the gate record workflow left failing on Linux and macOS, over passing within a tolerance.** Entry 32 section 3 asks for byte identity or an explained difference, and choosing a tolerance that makes the difference pass would be choosing the gate.
- **Entry 47: double resolution tried second, over a capability fallback.** The WeChat module is present in the runtime packages for all three platforms. The failures were detection on code modules under five pixels, which only a larger working image can help.
- **Entry 47: resolutions past 8000 px skipped, over trying every one.** Doubling a 600 DPI scan cost 52.7 s and gave nothing the scan did not, and the bound lost no image on the sweep.
- **Entry 47: identification counted per platform in the gate record workflow, over a per-platform expectation in the unit tests.** No count is known yet for Linux or macOS, and an expectation written before measuring would be a guess.
- **Entry 48: Linux's gate record difference explained and left red in the workflow, over a Linux reference record or a rule that passes it.** Making the job green needs either Linux's own records committed as its reference, or a rule about which differences pass. The first adds about 12 MB and the second is a gate written after the results, so the choice is planning's.
- **Entry 48: the owner corpus republished from the originals through `publish-owner`, over editing the published files.** Every published file still comes from the one scrubber, and the 13 files without a lens model reproduce byte for byte, which shows nothing else changed.
- **Entry 49 section 1: the printed tables committed as Windows text files, over comparing the platforms with one another inside one run.** A committed reference fails a single platform's job on its own, and it states in the repository what the record is.
- **Entry 49 section 4: the README's platform guard requires equality with the CI matrix, over naming at least as many.** A README naming a platform CI does not build would be false in the other direction.
- **Entry 49 section 2: the marker sort held for question 15, over committing it with regenerated records.** It changes committed evidence and a benchmark the Phase 1 surface work is measured against, which entry 49 did not foresee, and the question file exists for a measurement that contradicts what was written down.
- **Entry 52: every record regenerated twice under identical code, without the sort and with it, over comparing the sorted run with the committed records.** Five records were already stale, and comparing against them would have credited their changes to the sort.
- **Entry 52: tables from fits no command reproduces left as measured and marked, over updating the columns that can be regenerated.** Half a row regenerated disagrees with its other half and with the conclusions written beneath the table.
- **Entry 52 section 3: reordering applied at the homography fit, over shuffling the detector's output.** The sort puts any shuffled detection back in order, so only a shuffle after it measures the registration's sensitivity to order.
- **Entry 52 section 3: the edge fit's leave-one-out run from the converged pass's start, over rerunning the whole locator per point.** It isolates one point's weight in the fit; rerunning the locator would also move the rays and confound the two.
- **Entry 49 section 2: the journal replayed by call order, with the image hash reported beside it, over looking each detection up by its image.** A platform whose raster differs would match nothing and replay nothing, and the rerun exists to hand it Windows' corners anyway.
- **Entry 49 section 2: only the two measurements whose tables differ replayed, over all eight.** The other six already print identically on macOS, so replaying them could only repeat what the gate record shows.
- **Entry 58 section 4: the opt-out's second key is what a file scrubs to, over the decoded pixels.** It collapses the four re-exports to one value as a pixel hash would, and it keeps a consent mechanism inside `GroupLab.Core`, which has no image decoder and whose tests run on every platform.
- **Entry 58 section 3: either key alone withholds, over requiring both.** The same reasoning as entry 37 section 1's two opt-out signals: redundancy is the point, and publishing under ambiguous consent cannot be undone.
- **Entry 55 section 3 item 1: the counts recorded in the stage record and in `grouplab measure --json`, over the committed spike records.** A field in the spike records would regenerate thirteen of them and move the gate record's raw comparison, for a diagnostic that changes no figure anybody reads.
- **Entry 55 section 3 item 1: a ray counted as marginal within a tenth of the crossing threshold on either side, over counting only the rays that failed it.** A ray that just cleared the threshold is as easily flipped by the image as one that just missed it, and the tenth is the convention the leave-one-out already uses for the rejection limit.
- **Entry 61 section 3: no catch added for `PlatformNotSupportedException`, over adding one defensively.** The runtime throws `Win32Exception` for a verb off Windows, so a catch for the other would assert a behaviour that does not exist and would outlive anybody who remembers why it is there.
- **Entry 64: the working tree left alone, over running the fix as written.** Nothing differed, so the command would have been a no-op dressed as a repair, and running it would have left a false record that something was cleaned.
- **Entry 61 section 5 item 2: the tarball built on `ubuntu-latest`, over pinning `ubuntu-24.04`.** Pinning would freeze the glibc floor deliberately and freeze it silently apart from the test matrix, which tracks `ubuntu-latest`; printing the release it built on keeps the day they diverge visible, which is what entry 63 section 2 asks for.
- **Entry 61 section 5 item 2: the step fails when the native imaging library is missing, over shipping whatever publish produced.** A tarball without `libOpenCvSharpExtern.so` installs, launches, and then cannot detect a marker, which is a failure that arrives late and in front of a user rather than in CI.
- **Entries 65 to 69: the concept image left uncommitted, over committing a second copy.** The committed `docs/figures/screens/assignment-editor.png` is the same picture pixel for pixel, and the copy would add 481 KB and a content-credential block to the history for nothing.
- **Entry 65 section 4 step 2: wording that names no platform, over "on Linux".** The branch it describes runs on macOS as well.
- **Entry 70 section 3: a moved shot recorded as the difference from the bull detection gave it, over a list of notices appended at each edit.** The difference is derived from state, so undo, redo and a shot moved back all keep it true without bookkeeping, and it stays visible for as long as it stands rather than until the next edit.
- **Entry 70 section 3: the counts rule falls back to the nearest free bull, over the nearest of all bulls.** A bull a person has decided is not available to the matching in either mode, so the two methods differ only in whether the matching is forced.
- **Entry 70 section 6: three status states, over styling only the failures.** Success needs its own colour for the same reason alert does: a line that reads the same whether it worked or not teaches people to stop reading it.
- **Entry 74: pinning read from `BullChosen` alone, over `Corrected` or a chosen bull.** A shot becomes corrected when it is moved or marked not a shot, and neither of those chooses a bull.
- **Entry 73 section 1: a shot's pool is its nearest bull's, with the margin still measured to every bull, over a pool chosen by page region.** The sheet already says which bulls are sighters, and nearest-bull is the rule's own fallback; a region would be a second geometry to keep in step with every definition.
- **Entry 73 section 1: the overall method reported as nearest-bull when either pool fell back, over reporting the scoring pool's.** Reporting one-to-one would hide that a pool stopped being matched, which is the thing section 13 says must be said; the reason names each pool.
- **Entry 73 section 5: no mark size invented when no calibre is set, over drawing a nominal diameter.** A ring at a made-up diameter reads as a claim about the bullet, which is the kind of implied fact section 2 rules out.
- **Entry 73 section 7: the interval labels left at their exact coverage, over matching them.** Entry 24 decided the label states the coverage the interval actually has.
- **Entry 71: intake run on a copy without the unlisted scan, over adding the scan to the manifest.** Whether the consent covers the scan is the contributor's question, and the submission as received is left as it is.
- **Entry 71: the worst bull reported beside the worst clean bull, over the worst bull alone.** On a shot sheet the holes cut the rings of the bulls they hit, and separating the two shows the error is registration.
- **Entry 76 section 4: the printed name reverted, over landing it with a box that covers both captions.** On a sheet already printed the wider box hid a real hole, and the analyser cannot tell which caption a sheet carries; where the name goes is question 16.
- **Entry 76 section 4: detection cancelled at checkpoints between stages, over interrupting a stage.** A stage stopped halfway leaves nothing a person can use, and the checkpoints are where the work can be dropped cleanly.
- **Entry 76 section 4: a moved shot drops its measured diameter, over keeping it.** The measurement described the point the detector chose, and a ring at that size around a point a person chose would claim a measurement nobody made.
- **Entry 76 section 1: the aspect's null integrated exactly, over a simulated table.** The density has a closed form for every n, so there is no table to extend or to seed.
- **Entry 76 section 2: the two split detections merged at their midpoints for the re-run, over the pipeline's figures or hand-picked positions.** The pipeline's figures count one hole twice, and the midpoint uses only what the pipeline found, which is the question entry 76 asked.
- **Entry 75: a shot with no bull named "unassigned, at x, y" in the list, over "unassigned" alone.** Two such shots would otherwise read the same, and the position is a fact the screen has, not an order.
- **Entry 77 section 3 item 1: a blob counted as swallowed only when it passed every shape filter, over every blob centred in a zone.** Printed matter leaves residue that the size and shape filters refuse anyway, and counting it would bury the one number that means a hole may have been lost.
- **Entry 77 section 3 item 2: the check made standing by an artwork fingerprint test, over running the corpus inside the test suite.** The corpus takes minutes and its counts differ by platform. The fingerprint is fast and exact on every platform, and it forces the comparison to be run where it can be.
- **Entry 77 section 3 item 2: the committed corpus punched with synthetic holes on three offset grids, over committing a real shot sheet.** No shot sheet has consent to be committed, and the Phase 0 scans are real print made before every change since.
- **Entry 77 section 4: oversize measured three ways, over the detector's diameter alone.** Entry 73's warnings came from the screen's size check, which is a different measurement, and the test had to see the one that was reported.
- **Entry 77 section 5: a sheet with no clear place prints no name, over shrinking the name further or trying another margin.** Section 5 states the rule. One place with one clearance is also what the placement test can check on every sheet.
- **Entry 77 section 5: the identifier caption left at the bottom beside the new name line, over moving it.** It is C6's recovery path, and its zone is what every sheet already printed is read with.
- **Entry 78 section 4: the calibre only vetoes a split, over also splitting a round blob of two holes' area.** Splitting on size alone would cut a genuinely odd hole to fit the expectation, which section 4's second guard rules out; the blob is flagged instead.
- **Entry 79 section 1: a measured ratio per image kind, over the calibre or one pooled ratio.** Scans and photographs measure holes differently, 0.944 against 0.986, and section 1 asked for them apart.
- **Entry 79 section 1: a photograph without camera data taken as a scan, over refusing the ratio.** There is nothing in such a file to tell the two apart, and the one such photograph measures 0.924, within the scan ratio's spread.
- **Entry 78 section 4: a calibre named after an uncorrected detection detects again, over asking.** Nothing a person did is lost, and a result found without the size is the one section 3 says is wrong.
- **Entry 80 section 2: the old sweep stopped unfinished, over letting it run.** Its synthetic holes were the wrong size, and its real rows used the bullet diameter, so its result could only have been discarded.
- **Entry 80 section 2: the synthetic holes scaled to read like .308 on a scan, over reweighting the sweep.** A scale fixes what the holes are; a weight would only change how much a wrong population counts.
- **Entry 80 section 2: no split threshold adopted, over adopting the survivor.** The only setting the real holes allow also changes the default without a calibre and moves committed synthetic records, and no real merged pair has been measured to say what it costs.
- **Entry 78 section 2: the residue fix proposed and not built, over a plain elongation cap.** A cap alone would refuse two real holes joined by the closing, a silent loss, and the safe form depends on the threshold still open.
- **Entry 81 section 2: the oversize flag rebuilt around a single-hole size, over keeping the median rule and lowering the calibre flag alone.** The median rule flagged ordinary holes on a tight sheet and let pairs through on a sheet full of them, so a calibre-only fix would have left the loud failure silent whenever no calibre is named.
- **Entry 81 section 2: the single hole without a calibre is the sheet's 25th percentile mark, over its median.** On the composite sheets half the marks were merged pairs, and the median moved to a pair's size and flagged none of them.
- **Entry 81 section 3: the residue fix on size and elongation together, over solidity.** Solidity overlaps across all three populations, real single holes reaching 0.59, while size splits the elongated ones cleanly.
- **Entry 78 section 2: a small elongated blob kept as one hole below 2.2, over refusing every blob the size vetoes.** Refusing a real hole is a silent loss, and the margin between the most elongated real hole, 1.72, and the split threshold, 1.80, is too thin to refuse on.
- **Entry 82 section 2: the floor used to veto and never to flag, over clamping the quarter-point alone.** On the clean photographs there were no round marks to clamp, and a floor that flagged would flag every real hole, since each is larger than the smallest a bullet makes.
- **Entry 82 section 2: the floor at 0.16 in, over 0.17.** 0.17 is the bullet; a .17 hole measures about 0.944 of it on a scan, and the floor must sit below any real hole.
- **Entry 82 section 3: two sizes need a gap of five pooled deviations, over three.** An even spread of sizes cut in half is 3.3 apart, so three would ask for a calibre on any wide one-calibre sheet.
- **Entry 82 section 3: no flags at all on two sizes, over tentative flags on the larger group.** Entry 82 asks for one sentence rather than many flags, and a sheet of two calibres would have every larger hole flagged as a merge.
- **Entry 82 section 6: the detector's flag drawn beside the size check's, over merging the two.** They measure different things, the detector's residual against the size check's dark region, and each says what it measured.
- **Entry 83 section 2: sizes converted at each blob's own scale, over leaving the detector alone as entry 83 section 4 asked.** Section 2's test found a wrong unit conversion rather than a tuning question. A size read 30 percent wrong on any oblique photograph is a defect, and the fix is a few lines.
- **Entry 83 section 2: the clean photographs' rise from 24 to 35 accepted, over restoring the single scale for the veto.** The single scale was wrong for real holes and for residue alike, and the residue it hid is the kind section 3 says cannot be resolved without a calibre.
- **Entry 83 section 4: the review queue computed from the marking, over storing it.** Every edit changes what needs review, and a stored queue would go stale; only a person's "keep it" is stored, because nothing else can know it.
- **Entry 83 section 4: the editor built into the marking screen, over a separate mode with Accept and analyse.** The statistics are already live on every edit, so an accept step would commit nothing. Discard edits is kept, as one undoable step.
- **Entry 83 section 4: the review keys taken on the tunnel route, over the window's key handler.** A focused button would otherwise take Space and Enter, and pressing Space would press the last choice again.
- **Entry 87 section 1: the screen's calibre size check removed, over keeping it and feeding it into the queue.** It measures the dark region connected to a mark, so a printed ring is part of every mark that touches one, and it read five confirmed single holes at about twice their size. Adding those five to the queue would have made the queue wrong rather than the panel.
- **Entry 87 section 1: `HoleSize` kept as a measurement.** The apparent extent is what the harnesses read and what the snap radius needs, and keeping it without a threshold is what makes the removal a removal of a judgement rather than of a number.
- **Entry 86 section 3: the enclosed-ink fraction recorded on every detection although nothing reads it.** It is the quantity that separates a hole on a ring from one beside it, it cost nothing to carry, and it is what proved the hypothesis wrong rather than plausible.
- **Entry 87 section 2: the README's states tied to DESIGN.md by a test, over a review habit.** Entry 60 found the README stale for days, and the two documents can now only disagree by failing a test.
- **Entry 90: the parametric editor and the visual designer deferred, over giving them a phase.** Alan is getting a specification from Jeff, and a phase for a screen nobody has specified would be a date attached to a guess. The deferral is explicit, carries its reason, and is checked by a test, which is the difference between parking something and losing it.
- **Entry 90: assisted hole placement raised as a question, over scheduling it or dropping it.** The detector differences against a definition and a store-bought target has none, so the bullet is not schedulable as written; but two of the three meanings of "assisted" are already built, so it is not droppable either. That is a design answer and not a wording fix.
- **Entry 90: the scope test requires a citation on a deferral.** A bullet could otherwise satisfy the test with the word alone, which is exactly the quiet drop the test exists to catch.
- **Entry 88: the oversize flag left alone although the measurement points at a defect in it.** Four of five flags on real material hold one hole's worth of ink inside a ragged hull, so flagging on ink area would remove them; the threshold is one the Phase 1 gate is measured against, and entry 83 section 3 says stop tuning the detector. Recorded with its numbers instead.
- **Entry 84: `PhotographHoleToCalibre` changed to the re-measured 0.948 and `ScanHoleToCalibre` left at 0.944.** The photograph figure was measured with the single-scale defect in place; the scan figure was not, and its 0.006 move comes from a longer verified hole list rather than from the fix.
- **Entry 84: the 25-shot rehearsal recorded as a rehearsal.** It measures the software's share of the two minutes and nothing about a person, so recording it as the Phase 3 gate would put a state of "done" on a page where the thing being gated has not happened.
- **Entry 94 section 1: the oversize threshold left at 1.35 while the quantity under it changed.** Area reads about six percent lower than the hull-derived reference it is compared against, so the threshold is already slightly stricter; and the value that would catch every 0.10 in pair would also flag S1b again, which is the false flag the change exists to remove.
- **Entry 94 section 1: the hull area kept beside the mark's area rather than dropped.** Solidity is the ratio of the two, and entry 88's shape measurements rest on it.
- **Entry 94 section 4: the split's two centres computed at detection time and carried on the flag, over computing them when the choice is taken.** The review queue has no image, and a choice that needs the pixels is a choice that needs a mouse.
- **Entry 94 section 2: subgroups keyed by bull rather than by shot.** A shot moves between bulls during review and its load does not; the sheet's layout is what holds the loads.
- **Entries 91 and 92: the zero correction refuses rather than rounds.** Where the offset is inside the sampling error the panel gives a shot count instead of a number, because a bare figure will be dialled.
- **Entry 93 section 3: high contrast derived from the dark tokens in code, over a fourth hand-drawn palette.** A palette that cannot be derived is evidence the roles carry values rather than meanings, and deriving it is what proves the concept is a design language rather than one screenshot.
- **Entry 95 section 2: the count item acts on its first candidate only.** Naming three and acting on one keeps it a key press; a person who disagrees with the ranking reaches the right mark through its own item or by selecting it.
- **Entry 95 section 2: every mark gets a size in holes, measured against the veto's size where no flag size exists.** The ranking needs a size on unflagged marks, which are most of the candidates, and only the flag's own size may raise the flag.
- **Entry 95 section 3: nothing changed for the mounted gate.** The investigation found where the error lives and what would separate the two explanations left; a change made before that photograph would be tuning against the frames being gated.
- **Entry 96 section 2: the warp that passes `IMG_5819` not adopted.** It was found on the frames being gated, it makes `IMG_5820` worse, and a model chosen because it passes the frames that prompted it is not a gate result.
- **Entry 97 section 1: a shot a person placed is neutral, not teal.** Teal means the software found it on its own, and a mark a person put down or moved is not that.
- **Entry 97 section 2: the marking keeps a copy of the rifle rather than its name.** A correction read from a saved marking must be the one that was right when it was shot, whatever the record book says since.
- **Entry 97 section 2: a barrel's count grows only on a person's step.** Counting automatically on detection would count a reopened sheet twice.
- **Entry 97 section 3: the per-stage rasters left for the next batch.** The records land live and cost nothing; the rasters would need keeping images the detector currently throws away, which section 19 allows for one interactive analysis and which deserves its own measurement of cost.
- **Entry 97 section 5: the window rehearsal pinned at 10 presses.** It is its own baseline at 300 DPI, and a ceiling the next batch must not raise.
- **Entry 98 section 3: a control added that the entry did not ask for.** The holed half and the clean half are different places on the page, and without the same split on unshot sheets a positional difference would have read as hole damage.
- **Entry 98 section 2: the nominal hole is .30 when nothing better is known.** It is the middle of what the corpus carries, and the sheet's own measured holes replace it as soon as the detector has run.
- **Entry 99: the editor ports the library's solver rather than calling it.** `tools/` is planning's and Python is not shipped; the port is held to the original by rebuilding ten sheets exactly.
- **Entry 99: the fewest markers allowed is 9, the fewest any built-in sheet carries.** It is the one figure the project has shown registration holding at, on GL-LR300-T's tile, and choosing a lower one would be a guess.
- **Entry 98 section 5: only the residual is kept.** The markers, corners and rejections were already in the result, so the interactive run's extra cost is one image.
- **Entry 101: the homography's final fit carried to convergence, over stopping at OpenCV's ten iterations.** A fixed iteration count reproduces the native figures only as far as every other detail of its solver does, and convergence is a definition every platform reaches the same way.
- **Entry 101: native code kept for the integer steps.** Candidate detection, decoding and RANSAC's inlier choice were identical on every platform in every gate record run; porting them would add risk to steps that already agree.
- **Entry 101: the contour lines fitted in double precision, over emulating OpenCV's single precision.** The emulation reproduces native and proves the contours are the same, but its answer is up to 0.09 px from the least-squares line it sets out to compute.
- **Entry 101 section 5: the picture carried on the stage record, over a separate artefact channel.** The record already reached the timeline live, so attaching the picture to it makes the two arrive together, and a trace put on the timeline after its run draws its pictures by the same path.
- **Entry 103 section 1: extreme spread drawn as the line between its two shots, over the concept's circle.** A circle that size reads as a region containing the shots; extreme spread is a distance between two of them.
- **Entry 103 section 1: Show work opens the timeline in the editor, over a second timeline in the analysis state.** The timeline already shows the work, and one copy cannot disagree with itself.
- **Entry 103 section 2: the flyer card says "further out than a group this size usually puts its worst" below one time in twenty, over always saying "not a flyer".** The old line said not a flyer whatever the distance; the card still leaves the call to the shooter.
- **Entry 103 section 3: held as a question, over running the sweep.** The sort was already committed by entry 52, and regenerating would have reproduced entry 101's records.
- **Entry 104 section 2: the worst shot calibrated by simulation at every count, over withholding the verdict below some count.** The calibration is valid from the dispersion minimum up and costs milliseconds, so there was no count where withholding was the more honest answer.
- **Entry 104 section 4: only the inked discs faded, over fading the whole bull.** Fading the paper as well turned it grey on dark chrome, and the paper-on-dark contrast is most of the concept's character.
- **Entry 105 section 6: the work bar defaulted closed, over open.** Alan asked for the strip off the screen; a failure stays a prominent error without it, and Show work turns red and says so when a stage fails.
- **Entry 105 section 7: a name read by its leading number only after the table, over the table alone.** "6.5 Creedmoor" and "30-06" are what shooters type, and their leading number is the table's name; a wildcat still falls through to the old rule and says what it read.
- **Entry 105 section 4: the mark drawn in the files' own colours until question 20 is answered, over recolouring it to the nearest tokens.** Recolouring would change a mark Alan chose to a set of colours nobody chose.
- **Entry 105 section 5: the icons drawn by a command from the committed mark, over a script outside the build.** The CLI already carries the imaging library, and nothing new is installed.
- **Entry 106 section 1: the viewer path on every platform with a confirmation dialog, over keeping the print verb anywhere.** The verb printed silently at the viewer's own scaling on Alan's machine, and GroupLab cannot see what any registered print command does.
- **Entry 106 section 4: the PDF drawn by the renderer's own writer, over printing the Markdown through a browser.** The other PDFs came from Chromium by hand; a command in the repository keeps the list and its PDF in step with the code, and installs nothing.
- **Entry 106 section 5: raised as question 21, over building it now.** It is about a run of its own, three decisions are open, and its test needs a PDF printer the CI runner may not have.
- **Entry 107 section 1: "9mm" read as a diameter, over refusing it.** The section's rule reads any number marked mm, and its test list refuses "9mm"; the rule is built and the conflict is question 22.
- **Entry 107 section 2: the quiet zone not counted in the margin refusal, over refusing on it.** The margin leaves bare paper and the quiet zone is bare paper, so it prints as intended, and a smudge near the edge lands on ink, which is refused on its own account.
- **Entry 107 section 2: `WindowsPrinter` in the CLI project, over the application.** The Core tests reach it there to print and measure a real job, and they already reference that project for the imaging backend.
- **Entry 107 section 2: markers located on the printed page with no fitting, over registering through a homography.** A homography absorbs scale and offset, which are the errors the test exists to catch.
- **Entry 108 section 2: designations compared as decimals at the value typed, over matching the text.** ".270" and ".27" are one value, and matching text would let one form through that the other refuses.
- **Entry 108 section 2: the refused value shown as typed with its unit, over normalising it.** The person sees their own entry named, which is what the refusal is about.
- **Entry 109 section 1: "why" as a compact disclosure on each item, remembered per item, over one panel of explanations.** An explanation read beside the figure it explains needs no hunting, and closed it is one short line.
- **Entry 109 section 1: the older size names mapped onto the five, over replacing every use.** Every existing use lands on the scale at once, and the test catches any new size.
- **Entry 109 section 2: settings as a screen in the main window, over a dialog.** The rail is navigation, and the gear is a destination like Print.
- **Entry 109 section 3: the flyer card's hedge kept in view, over moving it behind "why".** Without "by that measure alone" the verdict would say more than the test can.
- **Entry 109 section 3: excluded rows struck through, over a word in the row.** One number per shot leaves no room for a word, and the tooltip says it.
- **Entry 110 section 2f: the tolerances committed and pushed before the reference tables were generated, over writing them in the same commit.** The history then shows the order, which is the point of stating them first.
- **Entry 110 section 2f: both references generated on a GitHub runner, over installing Node and py-ballisticcalc here.** Nothing is installed on the development machine, and the same runner re-checks the JavaScript on every push.
- **Entry 110 section 2f: the G1 cases held as named known failures, over a red check or a wider tolerance.** A red check would block every other change, and a wider tolerance would make the gate mean nothing; the named list fails the day it is no longer true.
- **Entry 110 section 2a: the Coriolis vertical term held out rather than ported with its sign corrected.** The entry said to port it as it stands, so the conflict is a question, not a quiet fix.
- **Entry 110 section 2c: aerodynamic jump left out, over Litz's fit from memory.** The entry required the coefficients from the publication, which was not to hand.
- **Entry 110 section 2b: the rifle zeroed on the flat and then tilted, over zeroing at the shooting angle.** A rifle is zeroed at a range and then carried to the hill.
- **Entry 111 section 1: poncelet as the second transcription, over the gehtsoft ports.** Those are py-ballisticcalc's own ancestry, so agreeing with them would prove nothing; poncelet reached JBM's McCoy tables through JBM's files.
- **Entry 111 section 1: the JavaScript's tables kept only for the compatible mode, over deleting them.** The transcription check compares the port with the file as it is, and that needs the file's tables.
- **Entry 111 section 3: "why" as a plain button beside the item's last line, over a toggle.** The theme paints a checked toggle amber, and an open explanation needs no one's attention.
- **Entry 111 section 3: the count kept in the placement sentence, over the "Shots" row.** The sentence carries the count and how the shots were placed; the row carried the count alone.
- **Entry 111 section 4: the timing read from the log, over a stopwatch in the window.** The log already records every step with its time and no path, so the measurement needed no change to the application.
- **Entry 112 section 1: the sheet's registration kept in the marking file, over re-detecting on reopen.** Section 18 says no image is ever needed to reopen, and without the mapping a session reopened from its marking had no scale.
- **Entry 112 section 1: the proof image as a JPEG in the database, over a file beside it.** One file is the whole record, and the export carries it.
- **Entry 112 section 2: the report's words from the screen's own functions, over a second wording.** Two wordings drift, and the rule is that paper says nothing the screen does not.
- **Entry 112 section 3: deleting a sheet a session used allowed, over refusing.** Every session keeps its own copy of the definition, and refusing would make a sheet undeletable while any session of it is kept.
- **Entry 112 section 4: elevation carried by a central difference of the zero range, over a new solver input.** It needed no change to the validated solver, and a test holds it to a directly flown change within 1 percent.
- **Entry 112 section 4: the dope table on a Ballistics screen with its own rail slot, over a panel in the analysis.** The analysis column is 372 pixels, and the table has seven columns; question 26 asks.
- **Entry 113 section 2: the chart slot as Compare loads, over Reports.** The concept's chart icon is Compare loads, and a report is written from its analysis.
- **Entry 113 section 2: sessions at different distances compared as angles, over refusing.** Dispersion scales with distance, and the screen says it compared them as angles.
- **Entry 113 section 3: an optional fixed launch angle on the solver's input, over a zero-range difference for velocity.** A change of velocity with the zero kept would move the bore; the angle must be held, and without the field nothing changes.
- **Entry 113 section 4: the rule re-baselines the detected reading, over counting its moves as edits.** A rule is how the sheet is read, and flagging every shot it placed as moved would raise the doubles again.
- **Entry 113 section 6: JPEG pictures in GroupLab's own PDF writer, over another PDF tool.** The project's reading documents already come from that writer, and a JPEG goes in as it is.
- **Entry 113 section 7: the new screens held to their themes' tested text roles, over pixel contrast measurement.** The roles are what ThemeTests holds to their ratios, and a render's pixels vary between machines.
- **Entry 114 section 1: every filled shape drawn as a closed path, over keeping `FillRect` with a capability check.** Both drivers report the same RASTERCAPS, so a capability check cannot tell them apart; a path is honoured by every driver.
- **Entry 114 section 1: the page aborted when a drawing call fails, over printing what did draw.** A sheet with a marker missing looks normal and cannot be measured, and the fault is found only when it comes back from the range.
- **Entry 114 section 1: the in-app Print button demoted, over disabling it.** Hiding it would leave a person who wants it with no path and no reason; the screen now says what was wrong and what is safer today.
- **Entry 114 section 1: the drivers the tests print through are a fixed list, over enumerating the machine's printers.** A driver can open an application when it is printed to, as the OneNote one does.
- **Entry 115 section 2: bulls chosen with shift and click, over a plain click.** Every bull on a shot sheet has a hole on it, and a plain click there is the hole, which is what the editor has always done with it.
- **Entry 115 section 3: the velocity SD written to the load record with its provenance, over showing it on screen alone.** A figure that looks typed and a figure that was measured are different things, and the record is where the solver reads it.
- **Entry 115 section 3: the schema upgraded in place on open, over refusing an older database.** A person's sessions are not something to make them re-create, and the upgrade is one statement in one transaction.
- **Entry 115 section 4: a sheet read as the wrong definition warned about, over refused.** A damaged or marked-up sheet looks the same to the evidence, and the person can see the sheet.
- **Entry 115 section 4: the low-resolution threshold measured, over assumed.** The detector reads a sheet at 96 dpi and fails at 60, so the message is keyed at 120 rather than at the 150 that seemed obvious.
- **Entry 115 section 6: the corrected JavaScript generated from the original by a script, over edited by hand.** Every change is then exactly the five, and the file can be regenerated when the original changes.
- **Entry 116 section 1: a folder publish, over a single file.** The native OpenCV library then sits beside the executable where the loader expects it, nothing unpacks itself into a temporary folder on first run, and an antivirus sees an ordinary folder rather than the self-extracting shape it distrusts.
- **Entry 116 section 2: the shot sample generated by GroupLab, over shipping no shot sheet.** The repository holds no shot sheet that may be published, and a tester who cannot see the figures in the first minute has not seen GroupLab. It is question 29 all the same.
- **Entry 116 section 2a: the packaging script warns about a missing Inno Setup locally and refuses in a release.** A working copy should still build a zip on a machine with no installer tooling; a release that quietly shipped one asset of two would be worse than a failed release.
- **Entry 116 section 3: every asset attached twice, versioned and stable.** A bug report then names a build, and the README's front-page links keep working with nobody editing the README after a release.
- **Entry 117: the benchmark generates its own material, over reading anything a person has.** It then runs on a bare checkout, on CI and on a machine that has never analysed a target, and no case can quietly depend on one machine's folder.
- **Entry 117: the stage timings come from the stage record, over a second timing path.** Two clocks disagree eventually, and the one the application already files is the one a person sees in Show work.
- **Entry 117 section 3a: coverage checked by reflection over what does work, over a list of cases.** A hand list rots the moment a feature is added; this one named 25 gaps the first time it ran.
- **Entry 117 section 3b: the interface benchmark lives in the test project, over the command line.** Timing a control needs a windowing platform, and shipping a headless one inside the application to measure it would be a cost carried by every person to serve a benchmark.
- **Entry 117 section 3b: a dropdown measured by choosing from it, over opening its popup.** Choosing is the work; opening a popup headlessly crashes in the toolkit, and it would have been measuring the platform rather than GroupLab.
- **Entry 118: the anchors derived in the test from the heading text, over read from the list.** A renamed heading then fails the test rather than leaving a link that scrolls nowhere, which is the failure a reader cannot see.
- **Entry 118: the contents list after the Download section, over at the top.** Somebody who came to get the program should not have to read past a contents list to find it.
- **Entry 119 section 1: the updater ranks the trains, over SemVer's own text ordering.** SemVer puts beta below nightly because "b" sorts before "n", and section 4.5 needs the opposite. Question 30.
- **Entry 119 section 3: ECDSA P-256, over the Ed25519 the entry asks for.** .NET 10 has no Ed25519 and this machine has no NuGet source, so no package can be restored to provide one. The algorithm is named in every manifest, so a later change is one older builds refuse by name. Question 31.
- **Entry 119 section 3: the workflow fails loudly with no key, over publishing unsigned.** An unsigned manifest is one the application would have to trust without being able to check it.
- **Entry 120 section 4: the failing result carries its definition, over the screen falling back to the pipeline's words.** The advice written for a sheet that will not register was unreachable in exactly the case it was written for.
- **Entry 120 section 10: the preview sits in the grid row, over inside the scroll viewer.** Inside one it measured its own natural size and left the window two thirds empty; the scroll viewer is now used only when zoomed, where panning is the point of it.
- **Entry 121: the version raised to 0.2.0, over renaming what is already published.** v0.1.0 is history and stays where it is; the train moves above it instead.
- **Entry 122: one interface for everything outside the process, over telling the benchmark not to click that button.** An exclusion list would have fixed this button and left the next one to be found by somebody's browser opening.
