# Phase 1 results, entries 301 to 325

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

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

