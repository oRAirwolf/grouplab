# Phase 1 results, entries 326 to 350

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entries 326 and 330: TestFlight feedback reaches Code, privately

Done 2026-10-01, once request 63 brought `CRASH_REPORTS_TOKEN` (entry 330).

- **Filing** (`scripts/testflight-feedback.py`, `file_items`): each screenshot and crash submission is filed once in the private
  `oRAirwolf/grouplab-crash-reports`, labelled `testflight-feedback`: the comment whole with any email, telephone number or web address
  replaced, the build, device and iOS version, the screenshots put in that repository and shown in the issue, and for a crash its
  exception lines and GroupLab's own frames. Never an email or a name: the API is asked for `FILE_FIELDS`, the summary's fields and the
  screenshots alone. A hidden line with the submission's id keeps anything from being filed twice, so the half-hourly testflight run files
  as items arrive; started by hand it looks back 14 days. The public log and summary say only how many were filed.
- **The first run** (36831492456, 14 days): Unholy's two screenshots, issues 13 and 14, each linked to entry 328's commits; no crash in
  the window. It then stopped on a refusal: a crash submission had been asked for screenshots, which it does not have. Fixed (crash
  submissions are asked only for their own fields), with the self-test's made-up App Store Connect refusing the same way.
- **Backups:** the nightly backup takes the private repository's tarball as well as its issues, since the screenshots are files there;
  RESTORE.md has the line. The earlier attempt at reading through a sealed file (a worker's commit, never pushed) is in the trash folder.

## Entry 328: Unholy's two TestFlight reports (nightly 143, iPhone)

Done 2026-10-01, one worker on the main model; ships in nightly 147.

- **1, the keyboard** (`mobile/GroupLab.Mobile/KeyboardRoom.cs`, one place for every screen): while the keyboard is up the Shell gives
  up the height it covers (read from the system's input pane as its top edge in the window, so a window the system already shrank gives
  up nothing more) and puts the bottom bar away; the focused field is brought into view, and on the last field of a sheet or page its
  confirming button with it; a bar on the keyboard says Next, to the next field in reading order (two side by side are one row), or
  Done, which keeps the entry and closes the keyboard; a tap outside any field closes it. Capture's caliber sheet scrolls in the room left.
  `Entry328Tests`: the sheet at 402 by 874 (his iPhone) and 320 by 568, the distance field and Continue clear of the keyboard and its bar,
  Done and Next; and every field on Settings, Targets, Ballistics and its three forms, the chronograph and distance pages and the
  printer check, at 320 by 568. Not checked on a device yet: the input pane's rectangle on a real iPhone and Android phone (request 50).
- **2, the note** (`PictureCheck.RegistrationNote`): noted only from 0.00725 in, where the markers' disagreement alone would hold the
  score to a noted picture's 95; nearer a flat sheet's 0.005 in it still counts in the score unannounced. In words, no figures: "The
  sheet looks slightly curled. GroupLab allowed for it; flattening the sheet would measure a little better." The two other notes with two
  figures that could print alike now cannot: an angle just over 37 degrees reads "a little over 37", and a large sheet's pixels an inch
  are rounded down beside the 150 it falls short of. Begun in the ended session's worktree, carried over and checked here.
- **3:** the plain words are at the top of for-alan.md; Unholy retests on nightly 147. His feedback files stay in
  `C:\Dev\grouplab-local\testflight-feedback\`, never committed.

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

