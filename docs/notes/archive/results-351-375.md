# Phase 1 results, entries 351 to 375

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

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

## Entry 350: a friend's TestFlight reports, the keyboard on Targets

Done 2026-10-02. Issues 15 and 16 (build 150, an iPhone, Targets): the keyboard's bar was left over the screen after the keyboard went,
with Done doing nothing, and the number pad stayed up with no bar. `KeyboardRoom` trusted the system's keyboard events to arrive and in
order; it no longer does: a tap outside a field closes the keyboard whenever a field has the focus, Done always takes the focus, asks
UIKit to end editing (the iOS head's `HideSystemKeyboard`) and puts everything back, the bar goes as soon as no field has the focus (on
focus loss and every 400 ms while shown), and a field focused with the keyboard already up raises the bar from the pane's own state.
`Entry350Tests`: three tests, each failing on the old code. The scheduled feedback run now looks back 24 hours, not one.

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

## Entry 374, what was left of it (2026-10-07)

- **The timed pairing** (section 2), run on the range strings with `ChronographReconciliation.Propose`, the shots in firing order: the
  6 ARC string, 25 readings all timed, is one run with no pause of five minutes, and pairs reading for shot with nothing proposed apart;
  the .300 Norma string, 15 readings all timed, is three runs of 5 (its pauses between strings of five), and with 15 shots marked it too
  pairs one for one with nothing proposed apart. Right for both: neither string has a reading of another group or a missing shot, so
  the proposal has nothing to say and says nothing. The XLS forms go through the workbook reader, which entry 374 already showed agrees
  with the CSV.
- **The 2 MOA 3 by 4 sheet** (section 4) does not fit on Letter with the 2.00 in bulls, at any spacing the format allows, either way
  round, nor on A4; two layouts that pass are put to planning as question 86 ((a) Letter with 1.50 in bulls 2.00 in apart, (b) tabloid
  with the 2.00 in bulls). Not built until the answer.
- **An answered review question can be asked again** (section 4): `MarkingSession.Reopen` forgets a "leave it" and lets go of a bull
  the person chose, so the matching places the shot again, as one step Undo takes back (`ReviewQueue.Reopen`). On the computer an
  answered item clicked in the queue shows its card, "Answered." and its sentence, with **Ask again** and its choices, which change the
  answer at once. On the phone, answered marks to check fold up under "Show the N answered marks", each with **Ask again**. An answer
  that removed its item (not a shot, two shots) is taken back with Undo. Tests: `ReviewQueueTests.AnAnsweredItemCanBeAskedAgainAndChanged`,
  the desktop's `TheReviewQueueSettlesAContestedShotFromTheKeyboard` and the phone's `AMarkMuchBiggerThanTheBulletIsRinged...`.
- **Two holes in one** (section 4): "shot N is 2 shots" already existed as Possibly two holes (One shot, Two shots, Not a shot). The
  other way round is new: two counted marks closer than half a hole's width (the caliber's, or the sheet's own hole size) raise
  **Possibly one hole**, with **One shot**, which keeps the first at the middle of the two and removes the second as one undoable step
  (`MarkingSession.MergeShots`), and **Two shots**, which keeps both. Two shots through one hole sit further apart than half its width.
  On the computer in the review queue, on the phone with the marks to check. Test:
  `ReviewQueueTests.TwoMarksCloserThanHalfAHoleAreAskedAboutAndCanBeTakenAsOneShot`.
- **Shot data out and in** (section 4): the CSV export (entry 169) already gave each shot's bull and its offset from that bull in inches,
  MOA and mil, with the distance in the header; each row now also names its sheet, its session (the picture's name) and the distance
  in yards, before "excluded". Import takes several files at once, on the computer and the phone, and reads files with the same
  columns as one group (`ShotCsv.Pool`), so shots from many sheets of any kind are pooled in one analysis; files whose columns differ
  are refused by name. Test: `ShotCsvTests.ShotsFromSeveralSheetsArePutTogetherAndEachRowSaysWhereItCameFrom`.
- **The range photographs in the local corpus and the scoreboard** (section 1, 2026-10-07): ten folders under
  `C:\Dev\grouplab-local\corpus\range-2026-10-04\`, never committed, truth Alan's count of shots fired. GroupLab's own sheets, with
  `grouplab scoreboard --corpus`: **137 of 145 holes found, no false mark**: C bull close 15 of 15, far 14, the phone's photo 14 (bull
  10's torn hole missed on all three); E bull close 22 of 25 and, new since 2026-10-05, **far 22 of 25** (its codes now read from 3 ft);
  load sheet close and far 25 of 25.
- **The store-bought targets** (section 1): recognition, then Find holes with the calibre shot. **Recognized: the Eze-Scorer only**
  (layout 0.870, 693 features), Find holes 40 marks for 5 shots. **The right product but refused:** NTC 100 yard (370 features,
  layout 0.576, the ejecta from the steel above spatter its printing) and the Rigid crosshair (730 features, layout 0.760); both under
  the 0.85 the fingerprint trial set, which no wrong product reached. With their fits anyway, Find holes gave 74 marks for 5 and 24 for
  4. **Not recognized at all:** the Shoot-N-C (no product had any features agreeing) and both Allen targets, which are not in the
  library. On the three with a fit, 138 marks for 14 shots: Find holes (Experimental) invents far more than it finds on a shot-up
  commercial target, NTC's ejecta above all, as Alan warned. Question 87 asks whether to measure a lower line for fits carried by many
  features. Hole positions are not known, so which marks are the real ones is not scored.
- **Still not done:** nothing in section 1; section 4's 2 MOA sheet waits on question 86.

