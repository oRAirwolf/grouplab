# Phase 1 results, entries 276 to 300

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

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

## Entry 276: question 68 answered

The "Apple mobile" paragraph keeps Alan's sentences with "iOS is not planned." in front; `docs/RETIRED-WORDING.json` now has a "settled" list, and this paragraph is its first entry, so a consistency audit does not report it again.

## Entry 275: the consistency audit of 2026-09-28, fixed

The tour's three phone stops say what the phone does since nightly 119 (a "pending" Mobile side the site build now accepts); the phone pictures' captions and the README name nightly 115; the download page's Android card and minimums table are current, the table rendered by the tables extension and a site build check (`raw_table_problems`) failing any page that prints one as text; "sends nothing anywhere" is gone from the footer, the README and the testing guide, and retired; the updates feature says only the Windows installer updates itself; iOS is not planned everywhere and the Android imaging row names OpenCV; the testing guide has a phone part and no stale lines; the user guide has section 13, On the phone, linked from the six Android-only features; phases 5 and 9 are in progress; the README's developer sections name Android and the site; any target is said once in the README and marked as on the computer for now; the home eyebrow names four platforms; the Store clause waits for the Store; STATE and for-alan agree on eight requests. **The cause behind section 9:** `.github/shipping-paths.json` still classed `android/` as the spike, so a commit touching only the Android application was left out of the notes and could not start a nightly; `android/GroupLab.Android/` now ships.

