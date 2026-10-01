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

## Entries 324, 325 and 327: the bent sheet, the sync options, and the store-bought blanks

Done 2026-10-01, one worker at a time, while another session did entry 323.

- **324 section 1, a bent-sheet registration (da9cf190):** a smoothed thin-plate correction over the lens fit from every marker corner,
  taken only when the lens fit left a marker 0.04 in or more out and the bend predicts it without seeing it; the hole stage reads each
  cell through it. The far column's worst error fell from 0.080 to 0.009 in at 9 degrees and from 0.083 to 0.016 in at 15; worst on
  those pictures 0.080 to 0.028 and 0.083 to 0.022 in; found and false marks unchanged; every other real picture and all 27 synthetic
  lines identical. The registration figure shown rises from about 0.0036 to 0.0075 in on those two pictures, because it now counts the
  far column. Article: not yet, until a second sitting with a lifted sheet.
- **324 section 2:** `docs/notes/sync-options.md` (a folder the person's cloud client syncs, recommended first; the providers' APIs; a
  GroupLab service, not recommended).
- **325 and 327, the five blanks (2a5757bd):** false marks on the clean sheets before and after: the 6 in and 8 in bulls and the sight-in
  grid 0 and 0, the crosshair 3 and 0 (its solid printed diamonds, now refused as print), the Eze-Scorer 8 and 8 (printed numbers and
  logo letters, which look like holes on every measure the finder has). The cut-off edge of a partial scan no longer counts as print or
  as a bright ring (entry 327). The committed commercial scans and the hand-checked scan are unchanged. Request 58 asks for the shot
  scans in the same corner of the glass, the shot counts and phone photos.

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

## Entry 323: Velocity and the vertical, Desktop B and Phone B

Done 2026-10-01, one worker on the main model.

- **The one computation.** `VelocityBlocks.Build` (src/GroupLab.Core/Marking/VelocityBlock.cs) turns `VelocityVertical.Analyze` into
  what both screens draw, so every figure is `grouplab velocity`'s: the session's newest chronograph string (never several pooled), the
  pairs a person accepted for it, the session's load's BC and drag model, the rifle's sight height and zero where it records them (1.5 in
  and 100 yd otherwise, said under why), and the counted shots' heights. Its five states carry the approved words; the confidence shown
  is the result's own (`TheConfidenceShownIsTheResults` asks for 80%).
- **1, the desktop block** (`VelocityBlockView`, shared with the phone): directly after the Group block, the heading with why at its
  right, closed and remembered; the share at the lead size in amber with its interval; the sentence; the meter; the two bars with
  whiskers, each value by the Group block's rule with the size on the paper beneath; the chart and its sentence where shots are paired;
  why. The slope and why sentences give heights on the paper, as the entry's sample sentence does, because an angle to two places reads
  0.00 for velocity's few hundredths of an inch at 100 yd.
- **2, the band:** amber tint one predicted SD each way of the group center with dashed edges, dotted lines one measured SD each way,
  both labelled on a backing drawn last; a Velocity band switch beside Extreme spread, shown only with a result, on by default, kept in
  the remembered plot marks (`PlotMarks.VelocityBand`).
- **3, the phone:** a card above All figures, full width; a Velocity band chip under the plot, and the phone's chips are now remembered
  as the desktop's are. The phone had no chronograph entry and no way to set the distance after a picture was read, so a button there
  would have opened nothing: Add readings opens a page to paste them, read, then "Keep, paired in this order" or "Keep without pairing";
  Set the distance a page taking yards or metres; Set the load's BC opens Ballistics on the load's form. Where a phone session names no
  load, the load the phone's Ballistics page uses (the first kept) is taken, and why names it.
- **A real fault fixed on the way:** accepting a chronograph pairing on the desktop stored each reading's position counted from 0 where the
  store counts from 1, so whenever the first reading belonged to a shot the store refused the pairing (a foreign key failure), and
  otherwise every shot was paired with the reading after its own. Entry 115's test only passed because its first reading belonged to no
  shot. Now stored from 1, on both platforms; that test now checks the positions. A pairing saved before this build with the first reading
  marked as no shot reads one place off; none is known to exist.
- **4, the states:** every one tested in Core (`VelocityBlockTests`, 9), on the desktop (`Entry323Tests`, 2: the order in the column,
  readings to result, the switch shown only with a band and remembered, the BC button to Equipment) and on the phone (`Entry323Tests`, 2:
  the card above All figures at 320 wide with nothing broken inside a word, readings pasted and paired, the chip remembered; the distance
  page in yards and metres).
- **5, everything public:** the README (velocity regression is no longer "not built"), the user guide's analysis section, the tour's
  analysis stop on both sides, the Features page with its own picture (`velocity`, rendered by the screenshot walk at 600 yd with
  readings made by the walk), PHONE-PARITY, the claims backing, and two release notes. The phone's own picture waits for request 50's
  sitting, as the Features entry says.
- **Found, not mine to change:** at 1400x900 the plot's toggles already cover the last lines of its key; question 78.

## Entries 319 and 320: the TestFlight groups, and Public Beta's first review left alone

Done 2026-09-30 as far as Apple allows. Every testflight run since the groups were made (about 20:49 UTC) had stopped at App Store
Connect's refusal of the What to Test field, so nothing had been added to Public Beta or submitted, and build 134's review was not
disturbed. The step now holds while Public Beta's first build waits for review: nothing newer is added or submitted, GroupLab Team keeps
Alan's automatic distribution, and each run's summary names the review's state (4cd83444, with a self-test case). The field is Apple's
`whatsNew`. Request 59 is marked being applied. The public link is kept on this computer only, not in the repository, until Apple
approves (entry 320 section 1); then the download page, the README and the guide get it, the lockstep is proven on the next nightly,
and only then does Alan turn automatic distribution off. The feedback summary already writes no tester's words, screenshots, name or
email (entry 311's review); that covers public testers too. Unholy's address is written nowhere.

## Entry 322: question 77, and an hour of three workers before the weekly reset

Done 2026-10-01 between 01:02 and 01:45 UTC, three workers as Alan asked.

- **Question 77, A and B (b5ff2465):** the guide says about 2 ft (60 cm); the next phone sitting and the first TestFlight sitting
  photograph one sheet at 1.5, 2, 2.5 and 3 ft.
- **1, markers at a distance (babcddcf, 176221d3):** measured first: markers are 15.5 px at 2 ft and 10.3 px at 3 ft in the working
  picture, and a photograph's first pass guessed a sheet across half the frame (23.4 px markers), so its size gates threw every marker out
  before decoding. A second pass sized for a sheet across a quarter of the frame reads 33 of 38 at 2 ft; at 3 ft, where every marker is
  found and none decodes, each undecoded square no larger than a marker is cut out and read enlarged four times. Scoreboard: far 2 ft
  0 to 48 of 50 holes, far 3 ft 0 to 44 of 50, no false marks; every other line identical; the seven real pictures identical. Desktop
  Release, one far picture: 0.36 s and 0.6 s for the whole reading, peak 500 MB as at 1.25 ft. Not yet: the phone's full-resolution frame
  for the cut-outs, and a phone measurement.
- **2, the far column (20211cc4):** not the lens fit's fault: the same sheet photographed taped flat keeps 136 of 136 corners, and richer
  lens models barely move the far column's misses (rms 3.82 to 3.77 dmm at 9 degrees); the far margin was lifted off the sheet's plane in
  that sitting, so leaving those markers out is right. A real fault fixed on the way: the lens fit now refits until its kept corners
  settle (a hard-bending lens at 30 degrees had kept 96 of 136 consistent corners). Real pictures unchanged in holes and false marks. One
  local check moved: the 2026-09-20 range photograph at 32.4 degrees now measures 31.2; nothing says which is nearer the truth, and the
  test records the move. The 0.08 in far-column errors need a bent-sheet registration or a photograph held flat.
- **3, velocity regression (1c4a5b74):** velocity's share of the vertical with an F interval, the vertical SD velocity alone adds from
  the solver's drop per ft/s, and where readings are paired with shots the measured slope against the solver's; `grouplab velocity`
  prints it. Over 200 synthetic groups the true share fell inside its 90% interval at least 85% of the time. No screen: DESIGN NEEDED in
  for-alan.md.

## Entry 321: the camera's level upright at a backer as well as flat over a table

Done 2026-09-30 (e1c95abb), one worker. Gravity chooses the mode by itself with hysteresis: "Looking down" once the camera points more
than 55 degrees below the horizon, "Upright" again below 35, so a hand near 45 never flickers; upright is level when the camera's axis is
horizontal and the phone is not rolled, in portrait, landscape or upside down. The word sits under the crosshair; green is within 3
degrees in both. Once a frame reads the markers, the sheet's own angle decides (gravity again 2.5 s after the last such frame), and
`camera.level` logs the mode and the source. Found on the way: Android's gravity was read in the phone's natural axes, so the dot would
have moved sideways in landscape; it now uses the screen's axes as iOS did (not yet checked on a phone). Synthetic angled cases with the
pipeline's 26 mm lens read all 50 holes from 5 to 45 degrees, so they cannot find the limit; Guided's square-on limit stays 37 degrees
in both positions until real pictures say otherwise. Range cases added to the scoreboard: 15 and 30 degrees 50 of 50, sun and the
shooter's shadow 38 of 50 (9 lost to clipping alone), 6 px of motion 50 of 50, and 2 and 3 ft no marker read (question 77). Replayed
upright clips in sun, in wind, curled and taped are taken without "Hold steadier"; a shaken clip is not. The guide, a capture tip, the
README and the Features page mention both positions. Tests: Core 125 on the area, Mobile 88. Not done: a real phone check, and the tour's
camera picture for the new word.

## Entry 318: the next batch without Alan

Done 2026-09-30, with one worker at a time (entry 317).

- **1, angled photos (e75d137a, 70a098ed):** on the seven real pictures, 172 of 173 holes found before and after, false marks 4 to 2
  (the 9 degree picture 2 to 0); every synthetic line unchanged. A mark too small to be two holes and 4.5 or more times longer than wide
  is refused as printed-ring residue (the slivers measured 5.05 and 5.14; the longest real hole 3.38). The cause underneath: the lens fit
  leaves the far column's markers out as outliers, so each far cell stays 3 to 6 px misaligned; aligning quarter-cells fixed the 9 degree
  picture but worsened the 15, so it was not kept, and both are in the study. A mark much bigger than the bullet now carries its size to
  the result: an amber ring and a plain sentence on the phone and the desktop, through the review queue, until the person confirms or
  moves it.
- **2, holes on any target (e82e7925, 884026e2):** a finder built on the neutral darkness detector (darker than the paper, lighter than
  a black bull's ink, a dark centre in a bright fluorescent ring), measured as the scoreboard's "any target" class with its own baseline:
  371 of 512 synthetic holes over black bulls, fluorescent, diamonds and grids under four conditions, 24 false marks; 25 of 27 with none
  on the hand-checked commercial scan; 331 of 345 reference marks on the 15 committed commercial scans. "Find holes (Experimental)" on
  the desktop's marking once the scale is set, and at the phone's Marking A holes step in GroupLab Dev only; proposals are ordinary marks,
  unsure ones review items. Known misses are in DETECTION-LEARNING-STUDY.md section 7. Request 58 adds harder cases.
- **3, Firebase Test Lab (90ee7ff0):** GroupLab Dev answers Test Lab's game loop; `testlab.yml` runs daily on three real phones and one
  virtual, inside the free plan, and says "not set up" until request 62 (written, with Alan's exact steps).
- **4:** questions 75 and 76 answered as built; the guide says blue uses a little less ink than red (b7f21712).
- **5:** STATE.md opens its next items with what is left of the plan; writing it found the README calling hand marking on the phone
  "not started", corrected (1b5b4925).

## Entry 317: the token budget

Done 2026-09-30. The rules are in `CLAUDE.md` under "The standing budget". `for-alan.md` went from 1709 lines to about 330: 47 answered
requests and the old summaries moved whole to `docs/notes/for-alan-archive.md`. The first usage line is in it: 29 September 1.31 billion
tokens with 43.5 million of cache creation (five times a normal day), 30 September 0.31 billion at 13:20 UTC. This run already ran one
worker at a time after the entry arrived.

## Entry 314: the caliber box offers calibers, cartridges or both, with a lookup of every cartridge

Built 2026-09-30 on `overnight/screens` from a stopped half-commit, finished, reviewed and merged as 96c848a6.

- **The table:** `src/GroupLab.Core/Data/cartridges.csv`, one table for every platform. The draft's 661 rows name the same cartridge twice
  56 times (both the rifle and handgun lists, or two names); merged, 605 rows hold every name and alias of the 661 (checked by a script),
  and the list shown before typing (tiers 1 and 2 and every precision row) is 105, not 111. Tests: every row has a diameter and a tier,
  names are unique after aliases, and 26 standard diameters agree with SAAMI or CIP.
- **The lookup:** name and aliases, forgiving punctuation and spacing ("65 creed", "6.5cm", "308", "9mm" all tested); ranked by exact alias,
  then tier, then the precision flag.
- **The setting:** "Caliber box shows" Calibers, Cartridges or Both (Both for a new install), one setting on the desktop, Android and iOS;
  the Calibers list keeps the existing grouping by diameter; any other diameter can still be typed in inches or mm.
- **Question 76:** a held-back cartridge (entry 163) typed in full without choosing it still asks for a diameter; built as B.
- Tests: Core 165 on the area, App 12 on the caliber box and 35 over Settings and Compare, Mobile 72. The user guide has the line.

## Entry 312: Alan's iPad screenshots of build 134

Built overnight on `overnight/screens`, reviewed, tested and merged 2026-09-30.

- **1 (88d4a2b1):** Compare's plots and chart use the card's whole width on a tablet, two loads side by side and more in a row or a grid,
  square and at one scale; a way back to Sessions at the top on iOS as on Android.
- **2 (54ed59a5):** equal figures sit at the same place on the chart; the dot had been placed on each row's own scale. A test holds equal
  values at equal x.
- **3 (c7e2617d):** the verdict card in plain words first, the exact figures behind Details.
- **4 (67cea745):** every numeric field on both phones opens the number pad with a decimal point.
- **5 (756ce71b):** the caliber box shows a short name after a choice, selects all on a tap, and has a clear button, on the phones and the
  desktop.
- **6 (2d150031):** the line asking for the camera goes once the camera is allowed.
- Crash report 12 (Shots Needed to Zero calculated twice) is fixed with it (38862de3).

## Entry 315: GroupLab Dev driven without hands (sections 1 and 2, and the iOS Dev app)

Built overnight on `overnight/tooling`; the bridge was a stopped half-commit, finished, reviewed and merged 2026-09-30.

- **2, scenario files (b8484f72):** a development build runs a JSON list of steps from its scenario folder, a launch argument on iOS or
  the extra on Android, and writes each step's result, time, memory, screenshots, control trees and the log back for a script to copy.
- **Amendment 1 (a8ce0e90):** with `-p:GroupLabDev=true` the iOS head is GroupLab Dev, `org.grouplab.app.dev`, beside GroupLab; the ios
  app workflow builds it for the simulator and runs a scenario over the sample scan, and checks the public build carries none of the
  developer tools. The nightly builds it every night and signs and uploads it only once request 61's profiles exist.
- **1, the automation bridge (c6a31b63):** compiled only into GroupLab Dev, on 127.0.0.1 only, on by default there, with a random key
  shown on screen and written to Documents. Reviewed for safety: a line over 64 KiB is refused, at most four connections and one command
  at a time, idle connections closed after ten minutes, the key compared in constant time over hashes, a malformed or unkeyed request
  refused and the connection closed. Commands reuse the scenario runner's steps (go, press, type, choose, scroll, read a picture, a
  setting, reset) and add ping, tree, log, timings and memory. 42 key controls carry stable automation ids, held by a test.
  `scripts/app-bridge.py --platform android|ios` drives it over `adb forward` or pymobiledevice3. Not yet run on a device: the copies on
  the phones and the iPad predate it. Tests: Mobile 64, the Core tests for the area 138.
- **4, more visibility (d3cf5717):** the log stays one event per line as key=value (already structured, so not rewritten as JSON);
  `read.stage`, `camera.say`, `camera.level` and `camera.torch` now carry memory, the managed heap and the device's heat (Android's
  thermal status, iOS's thermal state), and `camera.say` names the check holding a frame back. "Show diagnostics on the camera" in
  Settings, About, off by default on every build as the amendment asks: frame rate, the instruction and its failing check, tilt and
  torch over the camera, and the stage and times over the reading, at most four redraws a second. Mobile 78 tests. iOS is first
  compiled by CI.
- **3, the replay camera (b2377a8f):** both heads' per-frame work (the sheet search, the guidance, the torch on Auto, the steadied words,
  the card check and the auto shutter) was the same code twice; it is now one shared frame judge, which the live camera, the replay and
  the tests all run. GroupLab Dev records the camera's last seconds (greyscale JPEG frames with each frame's time, gravity, torch level and
  light, in `clips/<time>/`, never uploaded) and replays a clip or a picture through the capture screen, as a scenario or bridge step.
  Frames are kept at the stream's full 1920 px by default: at 1280 px the committed sample read 16 of 34 markers against 29. Tests: a steady
  clip fires at the third ready frame (600 ms), an unsteady one never does. Not yet run on a device.
- **Amendment 2 item 6, Firebase Test Lab, reported and not used:** on the free Spark plan, up to 15 test runs a day (10 on virtual
  devices, 5 on physical ones), no charge. On the paid Blaze plan the first 30 minutes a day on physical devices and 60 on virtual ones are
  free, then $5 an hour per physical device and $1 per virtual one (firebase.google.com/docs/test-lab/usage-quotas-pricing, read
  2026-09-30). GroupLab Dev's scenario files fit Test Lab's "game loop" test, where the app is started with a test intent and its
  results files are collected, so a scenario could run on a few Samsung, Xiaomi, Oppo and Pixel models a day at no cost. It needs a
  Firebase project under Alan's Google account, so nothing is set up until planning and Alan want it; nothing paid without Alan.

## Entry 311: the first iPad sitting, Guided sooner, the level, and the iPad's logs

Built overnight on `overnight/reading` and `overnight/tooling`, reviewed, tested and merged 2026-09-30.

- **1, Guided sooner (4930c1c2):** measured from the Fold 7's 79 `camera.say` lines: analysis frames at a median of 892 ms, and Guided
  waited the half-second hold of the words plus three ready frames, about 2.7 s at the median. The count and the hold held it back, not a
  threshold. One shared rule now: the picture is taken once every frame for 0.6 s, and at least two, has been judged ready, about 0.9 s at
  the Fold 7's pace. What counts as ready is unchanged, and no detection code changed. Whether pictures taken this soon read as well
  comes from the next sitting's `camera.auto` and `phone.detect` lines.
- **2, the level (f8da0c6d):** the tilt did not depend on the axes; on the iPad only an 18 point dot turned green. The whole crosshair now
  turns green at Android's 3 degrees, and `camera.level` logs the raw gravity, the screen's turn and the tilt.
- **3.1 and 3.2 (5f8287af):** Settings, About, Send diagnostics on both phones (the newest five logs, crash records and kept pictures in
  one zip through the share sheet). On iOS, GroupLab's Documents shows in Files under On My iPad, GroupLab, with the keep-pictures switch.
- **3.3 (451c21b7):** `scripts/` reads GroupLab's own log and copies its Documents over USB with pymobiledevice3, nothing else on the
  device. Request 60 (the pairing) was done with Alan on 2026-09-30; build 134 lacks file sharing, so Documents over USB starts with the
  next build on the iPad.
- **3.4 (acfb1823):** the testflight workflow summarizes new beta feedback and crashes after each step. Reviewed before merging: the
  repository is public, so the summary says only whether a tester wrote a comment, never the words (a follow-up commit).
- Item 2 of Alan's sitting (the gap at the top of the preview) is entry 313 section 2's layout.

## Entry 313: the iPad's reading hang, Cancel, and the camera panel

Built overnight on `overnight/reading`, reviewed, tested (Mobile 50, Core 550 on the changed areas) and merged 2026-09-30 as 3638462d,
5a18a86a and 4d27fcbe.

- **1.1 to 1.3:** a reading runs off the interface thread under one helper (`Reading` in the phone's shared project). Cancel never throws,
  does not wait, and returns to Capture with the picture kept; a reading's late answer is cleaned up. One minute of running time is the
  limit, with the time the application was suspended (the screen locked) not counted; iOS is asked for background time. Every error ends
  the reading with a message, and one nobody expected is recorded as an error report, which is what crash reports 9 and 11 lacked.
- **1.4:** measured on the desktop over the 21 real phone pictures (12 megapixel, as the iPad mini's): 19 are named in 0.7 to 1.4 s; two
  fell through every resolution and took 30 and 36 s, 11 to 12.5 s of it reading the whole picture doubled, which named none of the 21.
  The phone no longer doubles a whole picture: the two hard ones take 19.4 and 17.7 s, and peak memory fell from 1782 to 713 MB. The
  iPad's own times come from the next sitting's `read.stage` lines.
- **1.5:** the iOS self-test reads the 32 megapixel scan, presses Cancel during the codes, and requires Capture back within a second.
- **2:** the panel sits under the status bar and the preview below it, centred; the dash beside the quality bar before a score is gone.
  The self-test checks the three parts do not overlap and photographs the screen.

## Entry 316: the Mac download names no chip generations

Done 2026-09-30. The Apple silicon card on the download page says "For any Mac with Apple silicon" and "Any Mac with Apple silicon
(M-series). Not an Intel Mac"; the README's row says the same. No other list of chip names was found in the site, the README, the guides,
the platform notes or the application. "M1, M2, M3" and "M1 or later" are retired wording, so `scripts/consistency.py` reports them if
they come back. The Intel card is unchanged. Three claims were moved to the new sentences with their backing (entry 306).

## Entry 310: TestFlight's two groups on the same build

- **Built:** `scripts/testflight.py`, run by `.github/workflows/testflight.yml` after every nightly (waiting up to 30 minutes for Apple
  to process the upload) and at a quarter past and a quarter to each hour, on a Linux runner. Each run is one step and can be repeated:
  the newest processed build into Public Beta with that nightly's "What you will notice" lines as What to Test, a Beta App Review
  submission where Apple asks for one, then GroupLab Team given the newest build Public Beta can install. Its self-test holds eleven
  cases against a made-up App Store Connect; `TestFlightTests` runs it and holds the workflow to a Linux runner.
- **Section 5:** the application already carried `ITSAppUsesNonExemptEncryption = false`; the share extension did not, and does now
  (c5a359ad), with the nightly reading the key from both bundles in the signed package. Where Apple still asks, the step answers it.
- **Not proven against Apple yet:** the groups do not exist until Alan makes them (request 59, rewritten to entry 310's names). Until
  then each run says so in one line and does nothing. The site's "Join the iPhone and iPad beta" waits for his public link.
- **What Apple does not allow:** a waiting beta review cannot be withdrawn through the API, so a newer build waits for the older
  review and follows it; both groups stay together throughout. Nothing is ever taken out of a group.

## Request 55: the first signed iPhone build, and the Mac build notarized

- **iOS:** nightly 134 (2026-09-30 07:00 UTC) was the first to sign with the distribution certificate, carry the share extension with
  its own profile, and upload to TestFlight; the step passed. Nightly 133's iOS job had failed only because it read the secrets at
  05:23 UTC, while Alan was still setting them (one of eight set).
- **Mac, first attempt (nightly 134):** both packages failed at signing: codesign counts every file in `Contents/MacOS` as code and
  only the native files had been signed, so the main program failed on the first unsigned .NET library. Every file there is now
  signed first and the program with the bundle (6dc18259).
- **Mac, proven by hand before the next nightly** (package.yml can now be run by hand; it publishes nothing): arm64 was signed,
  accepted by Apple's notary service, stapled, and Gatekeeper said "accepted, source=Notarized Developer ID". x64 was signed the same,
  but notarytool's `--wait` stopped on one status request that timed out while Apple was still working. The build now asks again
  every 30 seconds for up to 45 minutes (50fd9253); the second run used exactly that, 64 answers of "In Progress" over about 32
  minutes, then Accepted, and Gatekeeper accepted it too. The Mac package job may take 90 minutes instead of 60.
- **Also:** a refused notarization prints Apple's own reasons in the build log (8670092a).

## Entry 291 sections 2, 3 and 7.5: the result screen and the camera's words

- **3.1:** naming an off-square picture from its codes took 5.1, 13.7, 4.2 and 30.0 s on the Fold 7: the whole picture was read at four
  scales with corner searches after each failure, and the cut-outs took a code's centre for its corner. Codes are now read square on at 6
  px a module, each placed by its six nearest markers: 1.2 / 8.3 / 6.9 / 22.5 s became 1.5 / 1.1 / 1.3 / 1.3 s on the desktop. The waiting
  line names the step in progress.
- **2.1 to 2.3:** the result picture is upright from the registration, full width, no empty area (tested at the four holds); holes are
  fixed on their own Marking A page with zoom, undo and a keep-changes question; the selected Camera or Result button shows as selected.
- **3.2:** "Move back" only when the printing leaves the frame (the paper's corner touching the edge said it 15 times in the sitting);
  "Move closer" below 2.4 px a code module in the picture, leaving at 2.1, set from the four pictures shrunk in seven steps (all read and
  named down to 1.6 to 2.1 px, failures from 1.9 or less); a rendered sheet agrees.
- **3.3:** the last live frames read 5 to 25 markers and the pictures 34, with the same 4:3 view at 2.27 times the pixels; the live frame now
  foretells the picture's count (markers readable from 14 picture px), logged as `camera.live` against `phone.markers`.
- **3.5:** every "Hold steadier" of the sitting came from the live markers-read rule. Now only measured shake says it: from 0.6 picture px,
  steady under 0.45, between a 1 px blur (all read) and 1.5 px (two of three failed). The stream's own blur is a 0.3 px allowance until
  the next sitting measures it. **3.4:** the whole crosshair turns green. **7.5:** GroupLab Dev keeps each picture of a sitting, its live
  record and trace, with a switch that turns it off and deletes it. `grouplab capture-tune` reruns the tables (docs/MOBILE-CAPTURE.md
  section 8).

## Entry 291: the scale test, and a printer check tied to its prints

- **The photos:** six printer-check photos from the Fold 7 (pages A, B, C; card 1 a new silver card, card 2 an older dark one), kept
  locally with the three 600 dpi scans. The photo Guided mode took by itself was page A with card 1 on it.
- **The scans:** the printer printed 100.01 to 100.02% across and 100.03 to 100.07% down on all three pages: true size within 0.1%.
- **The cards:** the app's saved check (C, card 2) said 100.48 / 100.38%, so the saved correction is about 0.4% too large. Most of it is
  the card-thickness correction (0.48% applied, about 0.35% fits the distance); with none, A and C agree with the scan to about 0.2%.
  Question 74.
- **The outlines**, mean error with no thickness correction: A 0.20%, B 0.44%, C 0.13%; spread between the two cards, across / down:
  A 0.37 / 0.42, B 0.60 / 0.02, C 0.03 / 0.47. B's hairline reads the card about 0.5% large and is ruled out; A and C are not separable on
  two cards each.
- **5.2:** a printer check now carries its date on every result it corrects; Settings can mark a printer calibrated or serviced, and a
  marked or six-month-old check says so and offers a new one (`PrinterProfile.Stale`, `PrinterChangedTests`).

## Entry 294: mil as a first-class scope unit

- The first run asks "Is your scope in mil or MOA?" (Mil, MOA, "Both, I have rifles of each"), inches or millimeters beside it, on both
  platforms, and asks existing installs once; no region guess for the angle.
- `UnitSettings.Aiming(rifle)`: a session's rifle's scope unit beats Settings for every aiming figure. The desktop zero block shows one
  angle column in the scope's unit, the other a tap away. `Entry294Tests` (Core, App, Mobile) hold that a mil session shows no MOA in any
  aiming figure, a rifle's unit overrides Settings, and the first run's answer is kept.
- A "Mil or MOA" guide section, paired wording across the site, a Features entry "Works in your scope's unit", and the screenshots of the
  analysis, zero, dope and hit views in mil.

## Entry 295: Compare loads, All figures and session names

- **The chart** (desktop and phone): each name on its own line, the range and value beneath, as tall as its rows; `IntervalChartTests`
  holds that nothing overlaps from 180 to 700 wide at text sizes 11.5 and 28. Its sentence comes from `LoadComparison.ChartSays`, the same
  test as the verdict, so they never disagree (288 generated comparisons). Extreme spread says it has no range and points to mean radius.
- **All figures:** a value and its note go under the label when they do not fit beside it; no label breaks mid-word at 320 wide.
- **Names:** `SessionNames`, the load, then the date and time where needed, the sheet beneath, on both platforms' Compare, the phone's
  Sessions and the desktop's Session records.
- **Waiting:** each load's group drawn in Compare (DESIGN NEEDED); the result picture (entry 291 section 2).

## Entry 293: the Microsoft Store draft, and a login that submits nothing

- `release.yml` run by hand with `store_draft` builds `grouplab-win-x64.msix` with the identity from the four variables, checks the
  manifest's Identity Name, Publisher and PublisherDisplayName against them (a mismatch fails naming the field), runs the certification kit
  and keeps the package on the draft `store-draft-0.2.0`: 92,035,133 bytes, version 0.2.0.0, no tag made.
- The kit: WARNING, 24 tests, one optional test failed ("blocked executables", on .NET, Avalonia and OpenCV files), and a warning that the
  executable does not declare PerMonitorV2 DPI awareness.
- The Store login with the four secrets answered 200 and read the product back as "GroupLab"; nothing was submitted. A refused login says
  the Store secret may have expired, never the secret. The secret expires about 2028-09-28.
- The Store takes only higher versions after the first submission (0.2.0.0), so the first stable release it gets by itself is 0.2.1 or later.

## Entry 292, Android: pictures from any photo app

- **Choose a photograph** opens the system photo picker (images only, no storage permission, cloud photos where Google Photos provides
  them), or the apps chooser on a phone without one. **From another app** lists every app answering image/* by name.
- **Received:** SEND, SEND_MULTIPLE, VIEW and EDIT of image/*; several shared at once are read in turn as a set.
- **Cloud photos** are fetched through their content stream with a line naming the app ("Getting the photo from Google Photos, 2.1 of
  6.4 MB") and a Cancel that returns at once; a failed fetch says the phone is offline where Android says so. A reduced copy is detected
  against the app's stated size or the size the camera recorded (EXIF PixelX/YDimension; the GPS block is never followed) and said.
- **Play services:** nothing GroupLab needs depends on them but the picker's Android 10 backport; the update check runs at launch as well
  as in the background.
- Tests: `PhotoIntakeTests` (10). Both the Dev APK and the Play AAB build with no storage permission. The iOS half waits for the head.

## Entry 291 section 7: the detection scoreboard

- **Built** (entry 261 option a): `Scoreboard` in Core, `grouplab scoreboard` on the command line (`--synthetic`, `--corpus`, and `truth`
  from a scan), the baseline in `docs/scoreboard/synthetic-baseline.json`, and `ScoreboardTests` in every build (36 s in Release). A drop
  beyond the margin (1 hole, 1 false mark, 0.005 in median centre error) fails the build and names the condition.
- **Synthetic,** two seeds, 13 conditions: 48 to 50 of 50 holes on every line but a hand's shadow (44) and glare (45); one false mark in
  650 readings; median centre error 0.006 to 0.008 in. Curl now registers (49 of 50, against 0 of 50 on 2026-09-28, since entry 260),
  with bulls up to 0.033 in off. Glare, clipped as a camera clips it, reproduces the study's 22 and 23.
- **Real, local only:** 7 photographs of three sheets against their 600 dpi scans, hole by hole: 169 of 173 found, 6 false marks,
  median centre error 0.012 to 0.026 in, worst 0.086 in, about 1 s a picture. Square-on pictures are clean; at 9 and 15 degrees off square
  the losses and false marks are all in the right-hand column, a hole and the paper beside it read as one 0.3 to 0.5 in blob.
- **Proposed next, not done:** send a mark of twice the calibre or more to review instead of placing it; a local paper level per bull for
  shadow edges; a glare hot-spot mask; the mesh's interpolation between markers on a curled sheet. No detection parameter was changed.

## Entry 280 section 2: row 10 on both platforms

- **Phone:** Share a picture (the results box dragged, pinched or corner-resized, lines chosen by a tap; chips for the mean radius circle,
  a label, the style and the crop; Save to gallery through MediaStore into Pictures/GroupLab, with no new permission, and Share) and the
  one-page Report (one dated Letter or A4 page, shared or printed). `IPhonePlatform.SaveToGallery` has a default, so the iOS head builds.
- **Desktop:** "Shots and clicks..." and "Share a picture..." under the shot table, an Aim points section with "+ Aim point", "Zero from
  this group..." under the zero correction (its offset carried into Ballistics' dope until stopped), and Report as a menu of the full and
  the one-page reports.
- **Shared, so the two cannot differ:** the words (`ResultWords`), the picture's box and circle (`ShareCard`), the page
  (`OnePageReport`: the picture, a plot centered on the counted shots with whole grid steps stated, the figures, the load and equipment
  line, and the mean radius interval as the confidence sentence), the picture re-encoded without its metadata, and the `SharePicture`
  control. Tests: `OnePageReportTests`, `ResultWordsTests`, `Entry280Tests`.
- **Not done:** the device check and the phone photographs (next sitting); the five desktop windows' own Features pictures (listed in
  `docs/figures/SCREENSHOTS.md`).

## Entry 289: the 2 MOA sheets

Twelve sheets for Unholy's request: `GL-CF9-LTR` and `GL-CF9-A4` (one page), `GL-CF9-T` and `GL-CF9-TA4` (a set of three pooled by tile
index), each plain, C and E. Nine 2.00 in bulls, 3 by 3, on a 63.4 mm pitch (the even-pitch rule takes 0.1 mm off 2.5 in and keeps the
1 MOA sheets' 12.6 mm gap), with the nine-field load block.

- **The bull:** 508/492/254/238/50 dmm. The E is twice the 1 MOA E (0.72 in center); the C is a 2.00 in diamond, point to point.
- **Codes:** the top pair on every page (question 71).
- **Markers:** a new derived rule, `grid-boundary-edge-1` (scheme byte 4): the lattice intersections plus the outer cell edges' midpoints,
  26 on Letter and 28 on A4. Registration over 60 synthetic photographs per page: with the 14 intersections alone, 3 of 30 bowed Letter
  frames missed the 0.005 in gate (worst 0.0060 in); with the edge markers the worst is 0.0020 in bowed (the 5x5: 0.0023).
- **A4:** the grid sits 9.9 mm from the side edges, the nearest ink 7.9 mm (the 5x5 on A4: 8.0 mm). Nothing shrank.
- **Not carried by the format**, raised as questions 71 to 73: four codes with the load block between them, numbering 1 to 25 with S1 and
  S2, and the six named load fields.
- `TwoMoaSheetTests` renders and reads back every page of all twelve, both page sizes and all three bulls. Features has `two-moa` with its
  own two pictures; the Targets tour, TARGET-LIBRARY.md section 4.6 and PHONE-PARITY follow.

## Entry 290: iOS, the shared mobile project first

**The shared mobile project is built** (section 2 item 1). Every Avalonia screen of the phone, 26 files, moved from the Android head to
`mobile/GroupLab.Mobile`, a plain `net10.0` library that also compiles the desktop files the phone shares (settings, the plot, the curves,
unit taps, the log and error reports, the imaging backend and the target generator). It reaches the phone only through `IPhonePlatform`
(16 members: the app's folders, the bundled sheets, a reduced decode, the memory budget, the camera, sharing, printing, pasting, the update
card, the Dev flag); `AndroidPhone` answers it with exactly the code the screens ran before. `Phone.Start` is the start both heads call.
Because the library is plain .NET, it builds on any machine, and `MobileProjectTests` holds it free of Android and iOS calls.

- **Android stays as it was:** GroupLab Dev's APK and the Play AAB both build; the Core and App suites pass. The nightly's APK goes on
  the Fold 7 at the next install to confirm it behaves as nightly 124 did.
- **One desktop file changed:** `AppSettings`' phone branch now keys on `GROUPLAB_MOBILE`, which the shared project defines.
- **OpenCV for iOS is built in CI** (item 2): `ios/opencv/build-extern.sh` builds OpenCV 4.13.0 with the Android module list and
  OpenCvSharpExtern as static libraries for iphoneos arm64 and the arm64 simulator, merges each slice into one archive and wraps them in
  `OpenCvSharpExtern.xcframework` (21,892,888 bytes zipped). The `ios` workflow's `opencv` job on macos-26 builds it once per change of the
  script (cache key `ios-opencv-<hash of the script>`; a rerun restored it in 42 s against 8 min 50 s), checks the SHA-256 and that 19
  OpenCvSharpExtern entry points the .NET code calls are in both archives, and keeps it as the artifact `ios-opencv`. OpenCV reads no HEIC,
  so the head hands it JPEG or PNG.
- **The signing check is built** (item 7): `scripts/ios-signing.py` signs only with all seven secrets set and well formed, builds unsigned
  with none, and fails naming a malformed one, never printing a value; its self-test runs in every build.
- **The shared screens are tested on every machine** (toward item 4): `tests/GroupLab.Mobile.Tests` starts them headlessly on a stand-in
  phone that is neither Android nor iOS; every tab opens, and the committed 25-shot 600 dpi sample goes through the phone's own pipeline
  (the path a picked picture takes) to all 25 shots, a named sheet and a saved session. CI runs it on Windows, Linux and macOS.
- **Question 70, answered B:** each cell of the desktop's full CEP table shows its own unit and switches alone.

## Entry 288: GroupLab Dev updates itself

GroupLab Dev now carries an updater; nightly 125 is the first build with it. It reads the signed second manifest, where the nightly now
lists GroupLab Dev's APK as `android` `apk-dev`, at launch and every six hours through WorkManager on an unmetered network; downloads
resumably (`IOutsideWorld.ResumeDownloadAsync`, a range request that carries a part on); checks size, SHA-256 and that the APK's signing
certificates equal the installed copy's; and installs through a PackageInstaller session with `USER_ACTION_NOT_REQUIRED` from Android 12,
never while the camera is open, a sheet is being read or a marking page is open, and, when automatic, only once GroupLab has left the
screen. Settings, About has the card: installed, newest and when checked, Update now, Install updates automatically. A notice along the top
says when one is downloaded, and after an update "Updated to nightly N" with What changed.

- **Rules in Core:** `AndroidUpdates`, 15 tests in `AndroidUpdatesTests`, including the six the entry names. GroupLab Dev's `-dev` mark is
  taken off its place in the order, because `0.2.0-nightly.124-dev` read as it stands sorts after `nightly.125`.
- **The flavor:** `-p:GroupLabUpdater=true`, on only for GroupLab Dev's APK. Built here: the Dev APK's merged manifest has
  `REQUEST_INSTALL_PACKAGES`, the receiver and WorkManager's job service; the Play AAB's has none of them; an AAB built with the updater
  stops with an error. The nightly fails if its AAB's manifest asks to install packages. `UpdaterFlavorTests`, 4 tests.
- **The key:** `apksigner verify --print-certs` on nightly 124's APK and on the copies installed on the Fold 7 and the tablet: one
  certificate, SHA-256 beginning `98b36d56ef6f3d62`. Both devices are Android 16 (API 36).
- **The device check, first half** (Tab S8 Ultra, Android 16): nightly 125 over adb, then nightly 126 by itself: found at the next
  start, 44.9 MB in 2 s on Wi-Fi, hash and certificate checked, the "Install unknown apps" sentence and page, then Android's one "Do you
  want to update this app?"; one tap installed it in place, sessions kept, 56 minutes after publishing; the installer of record is now
  GroupLab Dev. A pending tap was taken for a failure, which lost the "Updated to nightly N" notice; fixed for nightly 127, where the
  silent second update is to be seen.

## Entry 287: the product picture at the top of the home page

The README's picture and its three lines, from one source, at the top of grouplab.org.

## Entry 286: nightly 123 on the Fold 7 for request 53

Installed over the existing app on both devices; the card photo screen opens on the Fold 7.

## Entry 285: the README's empty table row

The three lines under the main picture are a numbered list, so GitHub draws no empty header row above them.

## Entry 284: Behind the curtain

The map and three deep dives are built (`website/how_it_works.py`, `website/how-it-works.json`, 20 drawings in `website/how-it-works/`). Of the 321 facts in the approved designs, 248 were confirmed, 63 corrected and 10 reworded because the repository cannot confirm them, each against the repository on 2026-09-29. The corrections that change what a reader learns:

1. **Markers.** Today's GL-CF25-LTR carries 38 markers (152 corners); 34 was the Phase 0 printing, and GL-CF25-LTR-D still has 34.
   So the curl test's 38 and the sheet's 38 now agree.
2. **Bulls.** GL-CF25-LTR has 28 bulls, 25 scoring and 3 sighters: "25 shots to 25 bulls" stands, "30 bulls" was the specification's slip.
3. **Sheets named from their codes.** 35 of the 37 Phase 0 images on Windows, Linux and macOS alike, none wrongly, since tonight's code
   crops (the gate record of 78ee351); it was 33, 33 and 32. DETECTION-PIPELINE.md is amended to say so.
4. **Only the Core is kept off OpenCV.** The apps call OpenCV themselves to decode and encode image files; the map says "the Core's only
   door to OpenCV".
5. **The QR path** is as planning read it, on the desktop and the phone alike (one source file): the WeChat detector is created with no
   model paths, only finds the codes, and the plain decoder reads the bytes; no model file ships. dnn is compiled in only because the
   WeChat finder needs it.
6. **"0 learned models"** stands for deciding what is a hole: no trained model is loaded anywhere.
7. **The phone's live analysis** is 1920 by 1440, 4:3, the newest frame kept, after entry 281.
8. **Built and tested on Windows, Linux and macOS** stands; the phase 0 gate record runs on all three. "A few seconds for a 300 dpi
   Letter scan" stands (3.1 s and 3.9 s measured); "a few hundred MB" is not claimed, having no measurement.
9. **Holes on ink**, from SCAN-MEASUREMENTS.md section 3.6, worded once: the rim is shallower and less even (its darkest point V 48
   against 34 on paper), its dark band about 29 percent thicker, and it measures about 7 percent smaller; milestone M2.2's "darker" was wrong.
10. **The paper level** is taken in eighth-inch blocks since entry 233, not quarter-inch; the opening radius the code uses is 0.012 in
    (DETECTION-PIPELINE.md's 0.032 in reasoning is kept, now with an amendment beside it).
11. **The trace** on the pipeline page is a real run of the committed sample `gl-cf25-ltr-d-25-shots-600-dpi.png`, timings dropped.
12. **Stage codes** are written "S1 to S4", "S5 to S8" and "after S10" throughout; the code records S5 to S8 as one stage.
13. **"5 assumptions overturned"** reworded: four assumptions were overturned and one, scale, was confirmed and became the main test.
14. **Research links** to six draft articles are left out (scanner-traps, photographing-targets, printer-true-size,
    mean-radius-or-extreme-spread, cep-explained, how-many-shots); published ones stand in where a part would have none.
15. **Timeline dates** 13, 14, 16, 21, 22, 24 and 28 September are confirmed from the commits and entries.
16. **Synthetic results** are labelled an upper bound everywhere, and the held-out figures are given as first read at a split of 1.45
    with today's 1.80 figures beside them.

## Entry 280: one number at a time

A tap now switches the number tapped and nothing else, and GroupLab remembers the unit for that figure (`UnitTap`, `UnitSwitch.Convert`,
held by `UnitTapTests` and `UnitSwitchTests`). Of row 10's screens, the phone now has Shots A (`ShotsPage`, with the "Left out by the
shooter" reason), Zero from this group (`ZeroFromPage`, and the offset carried into the Ballistics dope) and several aim points (chips,
colored rings, "+ Aim point"). Share A, the report and the desktop's equivalents come after the device sitting; the full CEP table's cells
are question 70.

## Entry 279: the App Store permission, Marking A, and Unholy's requests

LICENSE now opens with a GPLv3 section 7 permission for Apple's App Store and TestFlight. Marking A is on the phone: the scale, the aim
points and the holes under a fixed crosshair, a ring the bullet's size, Undo, and a template. "Fudd buster mode" (Unholy's idea and name)
works out, from twenty shots and a seed made from them, the tightest and widest three-shot groups, what averaging three- and five-shot
groups would have said (biased low and still moving with the split), and the zero chased five at a time (`FuddBusterTests`, 6), shown as
one page on the phone and a window on the desktop. The desktop now saves by itself once a target is changed or accepted and says when,
where and that it is safe to close, with a Save button as the other choice in Settings (`Entry278Tests`). The 2 MOA sheets wait for their
layout, and the tabs are next.

## Entry 278: Alan's decisions of the evening

CSV B is built (`CsvGuess`, the phone's `CsvImportPage`, the desktop's dialog), with tests for names that say the unit, names that do not,
millimeters, centimeters, MOA, mil and a flipped vertical (`CsvGuessTests`, `Entry278Tests`). The three card-outline test pages are made
locally (`grouplab scale-test-pages`, held by `ScaleTestPagesTests`), and requests 53, 54 and 55 ask Alan for the prints, the store-bought
target and the Apple steps. The spread audit: every figure is what its name says once the glossary says the mean radius is sigma times
root(pi / 2), an estimate and not the plain average (64/35 in against 1.6 in on the hand-worked plus of five shots), and the full figures
no longer say their 95 percent ellipse holds 95 percent of later shots. A shot left out is out of every figure (`LeftOutShotTests`, which
failed before); each aim point has its own figures (`ByAimPointTests`); each shot has its offset and clicks (`ShotOffsetsTests`).
`docs/IOS-PLAN.md` sizes the iOS build at about three weeks, with the floor at iOS 26 as entry 206 found it.

## Entries 282 and 283: Alan's screenshots, and the shutter

Alan's eight screenshots of the camera test (local only) added nine items to entry 281's. The codes: a module got about 3.1 pixels and read
only enlarged three times, so each code is now cut out where the markers put it and read at two to four times (`LiveSheet.CodeCrops`):
the refused picture of 00:15:49 is named by its codes and a good one reads 2 of 2 (`SheetsByMarkersTests`, the CLI on the saved
pictures). Every phone text taking its words from a value wraps (`PhoneTextWrapsTests`); a picture with notes scores at most 95
(`PictureCheckTests`); the windage gives its amount (`AnalysisPanelTests`); the screen is drawn again on a return. The shutter: every step
from the press to the first result is logged, the press answers with a sound and a flash, the live analysis stands aside, the capture is
minimum latency, and `scripts/shutter-timing.py` is the repeatable device test; the numbers wait for the next sitting.

## Entry 281: the camera test's fixes

The logs of Alan's camera test (Fold 7, nightly 121, 2026-09-29 00:01 to 00:21 UTC) are read in `docs/MOBILE-CAPTURE.md` section 7: 0 codes
read live on every frame, six pictures all taken by pressing, three refused for unreadable codes, "Move closer" and "Move back" alternating
more than twenty times, the torch left on, and the camera not starting after the application came back. Built: the bubble level
(`BubbleLevel`, green within 3 degrees), the torch off at the picture and whenever the camera is let go, the camera let go on pause and
taken again on resume, one 4:3 field of view shown whole, the picture kept in shape and upright on the result and the check, Camera and
Result buttons always in view, the words held by `GuidanceSteadier` (resolution judged at the working copy's size, three two-edged bands,
500 ms), and a picture with unreadable codes offered the sheet it looks most like (`LiveSheet.MostAlike`, question 69), which named the
right sheet on all three saved pictures. Tests: `GuidanceSteadierTests` (5), `BubbleLevelTests` (3), `SheetsByMarkersTests` (2). The
device checks wait for the nightly that carries them.

## Entry 277: GroupLab Dev nightly 121 on the Fold 7 for the camera test

Installed at 18:01 MDT from the nightly's signed `android-dev` APK, replacing nightly 119; the running activity is GroupLab Dev's and it opens to Capture. The phone had dropped off wireless debugging and came back once Alan turned it off and on. A local build cannot replace a nightly on the phone (version code 1 against 119), so a sitting installs the nightly's asset. Nothing else on the phone was touched.

## Entry 261, continued: the server's capacity read

Section 6's one read-only command ran at 17:45 UTC with Alan's approval (request 52): two processors, 11.9 GB of memory with 10.2 GB available and no swap, 36 GB free of 45 GB, load 0.64. The re-reading loop fits at tens of submissions a day as one more worker capped at one processor and about 1.5 GB; with no swap the memory cap is required. `docs/DETECTION-LEARNING-STUDY.md` section 6 has it. Nothing on the server changed.

## Entry 260, continued: the score checked, the off-white counter, and shadows

**The score against how pictures measured.** On the sixteen Phase 0 phone photographs of unshot sheets the first score disagreed with how each measured; with the angle free to 25 degrees and the registration's error a part, the tightest registrations score 91 to 100 and the four worst 40 (MOBILE-CAPTURE.md section 6). **The off-white counter.** `CaptureScreenTests.ASheetOnAnOffWhiteCounterIsFoundAndReady` holds the guidance to a 1920 by 1440 frame of a sheet on a counter barely darker than its paper; at that size the codes are not read and the sheet is found by its markers' layout, which the E and C bull variants share. **Shadows.** The normalisation entry 260 asks for is already render-and-difference's stages S5 and S6: a local paper field from the pixels the render calls paper, and both images divided by it before differencing; the detection study measured hard, soft and hand shadows against it. **Still to do:** the inner Fold screen and landscape, the camera test with Alan, and the torch pair, which need the devices.

## Entry 276: question 68 answered

The "Apple mobile" paragraph keeps Alan's sentences with "iOS is not planned." in front; `docs/RETIRED-WORDING.json` now has a "settled" list, and this paragraph is its first entry, so a consistency audit does not report it again.

## Entry 275: the consistency audit of 2026-09-28, fixed

The tour's three phone stops say what the phone does since nightly 119 (a "pending" Mobile side the site build now accepts); the phone pictures' captions and the README name nightly 115; the download page's Android card and minimums table are current, the table rendered by the tables extension and a site build check (`raw_table_problems`) failing any page that prints one as text; "sends nothing anywhere" is gone from the footer, the README and the testing guide, and retired; the updates feature says only the Windows installer updates itself; iOS is not planned everywhere and the Android imaging row names OpenCV; the testing guide has a phone part and no stale lines; the user guide has section 13, On the phone, linked from the six Android-only features; phases 5 and 9 are in progress; the README's developer sections name Android and the site; any target is said once in the README and marked as on the computer for now; the home eyebrow names four platforms; the Store clause waits for the Store; STATE and for-alan agree on eight requests. **The cause behind section 9:** `.github/shipping-paths.json` still classed `android/` as the spike, so a commit touching only the Android application was left out of the notes and could not start a nightly; `android/GroupLab.Android/` now ships.

## Entry 274: a target GroupLab did not print, shown as a sample

The screenshot walk draws a plain Letter target at 150 dpi (four ring bulls with a solid center, five holes each, "Sample target" along the bottom, no markers or codes), opens it on the marking screen, sets the scale from bull 1's outer ring, 2.0 in across, places the four bulls and twenty shots by hand, and saves `marking-other` in both themes at every size. The home page's "Your own targets", the tour index and the marking stop show it, each saying that marking by hand is on the computer and coming to the phone. Entry 261's wording in the notes log no longer names `tools/study/`.

## Entries 272 and 273: the printer check, and tap a number to switch units

**The printer check.** `GL-SCALE-LTR-1` and `GL-SCALE-A4-1` are grid style 4 (`GridStyle4`, TARGET-SCHEMA.md section 3.13): three
crosshairs in an L 150.00 mm apart, rulers of 250.0 and 190.0 mm, a card outline standing 3 mm outside an ID-1 card (question 67), the
title and four instructions, all derived from the page size; the crosshairs' circles are unscored bulls. `PrinterProfile` now holds across
and down. `CardCheck` finds the card's four sides where the photograph stops matching the printed page, fits each straight, and corrects for
the card's 0.76 mm thickness from the camera's distance; on synthetic photographs at 99.2 percent a dark, a very dark and a light grey card,
shifted up to 0.6 mm and turned up to 1.2 degrees, read 0.992 within 0.0015 across and 0.002 down. `PaperEdgeCheck` measures the paper
through the markers and tells Letter from A4 by its shape; a sheet printed at 96.2 percent on Letter reads 0.955 to 0.969 and is said to
look like Fit to page. The wizard is on both platforms; Printers in Settings lists each printer with its figures and date.

**Tap a number to switch units.** `UnitSwitch` decides what a tap chooses, `UnitTap` makes any value that shows a unit tappable, reading its
kind from the unit after its number; the desktop goes through `UseUnits`, so what is typed on Ballistics is rewritten rather than changing
meaning (the Settings combos now do the same), and the phone's result, Ballistics, Compare and Shots Needed to Zero show a change at once.
`UnitTapTests` taps the mean radius and checks every angle becomes mil, lengths stay, the note says so and it is remembered.

**The curled-sheet mesh, rechecked.** Recording the new pages' artwork showed four Phase 0 phone photographs registering through entry 260's
mesh, where the radial fit had held a third of their corners; the spurious marks on them went from 19 to 12 (ultrawide2 from 1 to 6). The
mesh is now taken only where its error between markers beats the plain homography's over every corner, which these four still pass.

## Entry 271: real inches on photographs

`PrinterProfile` keeps a printer's measured scale, from a scan (its x and y spread, at least 0.1 percent) or one typed ruler distance (a thirty-second of an inch over the span); `RulerSpan` names the two bulls to measure (bull 1 to bull 5 on the 5 by 5 sheets). A photograph is multiplied by the chosen printer's scale and says so in one line; a scan that measured its own ignores it. On a sheet printed at 96.2 percent and photographed, bull 1 to bull 5 reads within 0.002 in of its true size with the profile; a ruler read to a sixteenth agrees with the scan's profile within their stated uncertainty. The session file carries the factor and its line. DESIGN.md no longer says a photograph measures the print scale.

## Entry 270: any target, and the sheet as the fast lane

The README's pitch, first caption and mosaic tile, the home page's lead and a "Your own targets" section, the tour's index, the Features page's first feature ("Any target you already shoot"), and the user guide's opening all lead with any target, with automatic holes on any target said as the goal. The mission line is in "Why it exists" and on the home page. `docs/RETIRED-WORDING.json` now names wording that makes a GroupLab sheet sound required. The picture waits on question 66.

## Entry 269: Shots Needed to Zero's colors

Within one click amber, the closest click teal, on the desktop and the phone.

## Entry 268: dark OLED screens

GroupLab Dev's idle screen is pure black with a dim line of text; Close, Back and Home all leave it; every device check ends on it.

## Entry 267: no lawyer, and a standing consistency audit

`scripts/consistency.py` checks the README, the site and the assets agree, and that retired wording (`docs/RETIRED-WORDING.json`) is gone; CI warns, and a weekly run opens an issue.

## Entry 266: the README as Alan chose it

Product shot, captions as a table, the six-screen mosaic, generated by `scripts/readme-images.py` from the screenshot job's pictures; "What GroupLab is not" is gone.

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
- [`docs/notes/archive/results-251-275.md`](notes/archive/results-251-275.md), entries 251 to 275, 17 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 145 section(s).

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
