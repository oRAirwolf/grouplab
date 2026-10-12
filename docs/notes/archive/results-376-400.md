# Phase 1 results, entries 376 to 400

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

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

## Entry 377: the M834 print that stayed on "Connecting", and the darkness test's share (2026-10-06)

- **Why it hung, from the code and the log together.** The Fold 7's log had no line at all for the print. `AndroidSerialPrinter.OpenAsync`
  called `adapter.CancelDiscovery()` before connecting, which from Android 12 needs the nearby-devices scan permission; GroupLab asks
  only for connect (the manifest has `BLUETOOTH_CONNECT` and not `BLUETOOTH_SCAN`). The call threw a security exception that no catch
  named, inside a print started with `_ =` and never awaited, so it was lost and the screen kept "Connecting to the M834..." for ever.
  The serial terminal app connecting in 1.3 s with the same service fits: the connect itself was never reached. Not proven on the
  printer until Alan tries nightly 173 (request 78 says so).
- **Built:** no call needs the scan permission; `PrinterConnect.FirstThatConnects` (Core, tested with fakes that block as Android's
  connect does: `PrinterConnectTests`, three) gives each way of connecting 12 s, closes the socket to end one that hangs, and stops at
  Cancel; four ways in order: the serial port's service secure, RFCOMM channel 1 secure and insecure (where the Phomemo app reached the
  printer), the service insecure. Each paired device whose name holds "M834" is logged by name, type and bond state, never its address;
  an LE-only bond is said in words with how to pair it again. Each block written has 15 s before the link is called stuck.
- **The print** draws and encodes every page before connecting (the printer dropped an idle link after about 35 s), logs each step
  (`print.m834` encoded, connect, sent, cancelled or the error, with milliseconds; `print.serial` for every attempt and the close), has a
  Cancel button while it runs, and says whatever stops it in the middle of the screen.
- **Not done:** the BLE route as a last fallback (item 4): nothing decoded says the M834 takes a page that way; it stays for the iPhone
  work. Reading the seven bytes the printer sends on connect: nothing reads the input stream, so they cannot stall anything; left.
- **The darkness test's crash**: the picture was written at the top of the cache, which Android's file provider does not share
  (`share_paths.xml` names only `shared/`). Now written in `shared/`, as were the data export and the fingerprint file, which would
  have crashed the same way. `Entry363PrinterAppTests` holds the path.

## Entry 379: the phones' senders switched on (2026-10-06)

- `website/api/limits.json`: `sendEverythingOpenPhones`, `fullLogErrorReportsPhones` and `sendTargetsPhones` true, on Alan's "forms
  updated". The application reads them as built, so Android and the iPhone offer Send everything I open, carry the log with automatic
  error reports, and send targets (over Wi-Fi unless mobile data is allowed in Settings) from nightly 173. `Entry357Tests`' switches-off
  case now holds all three off itself.
- What Apple's App Privacy and Google Play's App content declare is in docs/store/LISTING.md and docs/store/PLAY.md; the delete-data
  address it names, grouplab.org/support/, needs entry 378 section 4's section.
- **Waiting:** section 3, the page "What GroupLab sends" and the guides for both phones, once nightly 173 is out.

## Entry 376 Part B: Alan's tablet list (2026-10-06)

- **B1 Back:** Android's back presses a sheet's × first, then the page's own Back ("Back", "Back to the result" and the rest, tagged
  `PhoneStyles.PageBack`), then the tab rule as before (`Shell.WayBack`). `BackEverywhereTests`.
- **B2 Full width:** every phone page, the result and Compare loads use the whole width (they were held to 640).
- **B3 Caliber every target:** the box starts empty for each new target and is cleared when a result shows; Take a picture, Choose a
  photo, Paste and a picture shared in all ask first; the four calibers used most recently are one-tap choices, none chosen
  (`AppSettingsStore.LoadRecentCalibres`). A scenario names its caliber once and it is typed for each picture.
- **B4** marks to check in bull order (`ReviewQueue.MarksToCheck`). **B5** a tap anywhere in a figure's box switches its units, on the
  phone's tiles and rows (`UnitTap.Widen`) and in the desktop's figure rows, where the value now fills its row on a clear background. **B8** the phone's Targets lists the targets first, then
  store-bought targets, scale markers and thermal label printers. **B9** the top buttons are Capture and Result; Capture returns to the
  start of Capture. **B10** the logo is as wide as the screen, or as large as fits without scrolling, whichever is smaller.
- **B6 Report:** the one-page report opens as its own page drawn by Android's PDF renderer, with Share, Save on the phone (Downloads,
  GroupLab, through MediaStore) and Print. iPhone: the page says it cannot show it there yet and Share works as before.
- **B11 Zoom and pan:** the combined group picture keeps a drag once zoomed in, as Fix holes does; fitted, a drag still scrolls.
- **B12 The logs:** of the tablet's five "closed" records, two (5 October 05:14 and 23:05) are GroupLab's own silent update ending the
  run to replace it, and three Android ending it while it was on screen with no exception; none is a crash. The installer path now ends
  the run's marker first, so an update is no longer recorded. `camera.frame error=ObjectDisposedException` at 23:15:56 was a frame
  still being judged when the camera closed, 3.7 s after it opened; caught and harmless, and no longer logged once the camera has stopped.
- **Also, the red CI on entry 379's commit:** with the phones' senders on, a test that left an earlier "Always" made entry 357's two
  follow-up questions due, so the first run's screen stood where Capture should be and six phone tests failed in order but not alone;
  `Scenario.AnswerFirstRun` answers them too. One British spelling in the printer's log words was corrected.
- **Not done: B7**, Send diagnostics straight to GroupLab: the receiver takes only the kinds survived, closed and read-failure and no
  folders inside a package, so it needs a "diagnostics" kind server-side and a flat package; next run, now that the phones' senders are on.

## Entry 381: the M834 print that stopped after a few millimetres (2026-10-06)

**The cause, from request 73's recording** (`m834-bugreport.zip`, its full HCI log): the printer sets the pace itself. When the link
opens it asks for credit-based flow control with 666-byte frames and then grants one credit per frame, so the page crosses only as fast
as the printer takes it: the recorded page, 208 frames (139 KB), took 22.6 s, about 12 ms a frame with four pauses of 3 to 4 s where it
held back. It answers small status lines between (`1A 04 62` and the like), and its only answer after the page, `1A 0F 0C`, came 22.5 s
after the last frame; the Phomemo app held the link open until then, 45 s from first block to finished. GroupLab's three prints on
nightly 173 handed the whole 146,525 bytes to Android in 4 to 8 ms and closed the socket 2.0 s later, so the printer received a strip's
worth and the rest was thrown away. Pacing by GroupLab is not needed: Android's own stack honours the printer's credits.

**The fix:** the Android link reads the printer from the moment it opens, logs every answer as hex with its time, and logs each block
written with its time; the print then waits for `1A 0F 0C` (`PrinterFinish.WaitAsync`), or failing it for 30 s plus a second per
2,500 bytes (89 s for Alan's page), with Cancel ending the wait at once; the screen says "The M834 is printing the page" meanwhile and
then whether the printer said it had finished. Five tests on a scripted link (`PrinterFinishTests`): waits for the answer, an answer
split over two reads, the limit, Cancel, and the limit's size. Core printing tests 289 passed; mobile and Android build clean.

**Also:** crash issue 23 (the darkness test share, nightly 172) closed: entry 377's fix shipped in 173 and 173 has no report of it.

## Entry 382: the M834 sheet that stayed behind the tear bar (2026-10-07)

**Section 1:** GroupLab's Letter page on the M834 is 3300 rows at 300 dpi, true size; the Phomemo app sent 3294, with its page shrunk to
94.7 percent and top aligned, so its last 15 mm or so were white and cleared the tear bar. GroupLab's content runs to the page's bottom
edge, which stayed behind the bar. Alan measured 0.61 in (15.5 mm) from the end of the print head to the tear bar on 2026-10-07.

**Section 2:** the recording has no feed command after the page, and no M834 feed command is known, so rather than try `1B 64 n` blind
the feed is white rows at the end of the raster, which the printer is known to print and feed: on a roll 15.5 mm, 184 rows, after every
page (`TearBar`, `LabelJob.FeedAfterMm`); on fanfold nothing, since a true-size 11 in page ends on the perforation. Whether the printer
finds the fold by itself is asked of Alan in request 78. The phone's print screen has "Paper in the M834", a roll or fanfold sheets,
chosen once, remembered (`LoadM834Paper`), the chosen one shown; a roll until changed. The desktop has no direct M834 print, so there
is nothing to match there.

**Section 3:** the paper and the rows fed are logged at the start of every print; the printer's answers were already logged (entry 381).
Request 78's steps rewritten. Test: `M834Tests.OnARollTheSheetIsFedPastTheTearBarAndOnFanfoldItIsNot` (3300 rows, 3484 on a roll, the
added rows white, the page's rows unchanged).

## Entry 378: Android developer verification for the APKs sent outside Google Play (2026-10-07)

**Section 1:** GroupLab publishes two APKs outside Play with every nightly, on GitHub and grouplab.org: `grouplab-android.apk`
(`org.grouplab.app`) and `grouplab-android-dev.apk` (`org.grouplab.app.dev`). Both are signed with the upload key (alias
`grouplab-upload`, kept in GitHub's secrets); `apksigner verify --print-certs` on nightly 174's two gives the same certificate, SHA-256
`98b36d56ef6f3d62ce63c8066c0b0c64b472141e090af9f493a41bd5c51fe3fc`. Play re-signs what it installs with its own app signing key
(docs/ANDROID.md section 12), so Play holds `org.grouplab.app` with that key only: the upload certificate has to be added for it, and
`org.grouplab.app.dev` registered as a new package with it. The spike's `org.grouplab.app.spike` is never published. No private key was
read or printed; the public certificate is in `C:\Dev\grouplab-local\android-verification\` for Alan to pick in Play Console.

**Section 2:** request 79 has the Play Console steps. Google's help page names only "the asset folder" for the snippet, so the new
`android-verify.yml` (run by hand) takes the package, the snippet and the file name (`adi-registration.properties` unless Google's sample
says otherwise), builds an APK with no code, signs it with the upload key as the nightly does, and keeps it as the run's artifact.

**Section 3:** a note for testers in Brazil, Indonesia, Singapore and Thailand on the download page, in the README, the testing guide
and docs/ANDROID.md: Android there refuses unregistered APKs since 2026-09-30, so until registration is done, the Play internal test.

## Entry 380: the phone chooses the scale label size (2026-10-07)

**Section 1:** the phone's Targets, Scale markers now has **Label size loaded** above "Share four scale labels for the printer's app",
the same sizes as the computer (`ScaleLabels.Sizes`), saved in the same setting (`SaveLabelSize`); before, the phone always made 70 by 80
mm labels. docs/PHONE-PARITY.md's scale markers row says so. Phone build clean, phone suite run before pushing.

**Section 2:** request 77 rewritten for the 50 x 30 mm roll: set the size to 50 x 30 (remembered), print the PNGs from the Phomemo app at
100 percent. A 50 mm label's two codes are 40 mm apart against the 70 mm label's 60, so its scale is about 1.5 times as uncertain:
about 0.15 percent where the 70 x 80 label typically gave 0.1, scaled from that figure and not measured on its own.

**Section 3:** `C:\Dev\grouplab-local\m220-labels\` does not exist yet, so nothing to measure.

**Entry 378, also:** `android-verify.yml` proven with a dry run (package `org.grouplab.app.dev`, a placeholder snippet): the APK held
`assets/adi-registration.properties` and the manifest only, signed with the same certificate as the nightly APKs; its artifact deleted.

## Entry 383: marking files kept the photo's full path (a tester's report, 2026-10-07)

**Sections 1 and 2:** `MarkingFile.Write` writes the photo's file name only, on every platform (`MarkingFile.ImageName` cuts at either
separator, so a Windows path read on a Mac loses its folders too). Opening a marking looks for the photo beside the marking file by
name, then at the whole path an older file holds, as written (`MarkingFile.FindImage`); failing both, the desktop says "The photo N was
not found next to the marking file. Choose the photo to reopen the marking." and opens the picker, and if none is chosen says so in the
middle of the window; never silent. A file opened that way saves with the name only. Sessions keep their photo's path in the device's
own database, so reopening a session on the computer and the phone takes it from there.

**Section 3, what leaves the device:** the sent target package names its image `target` plus the extension and has never carried a
marking file; error reports pass messages and stacks through the log's scrubber and the report package refuses markings and paths;
the session package already renamed its photo `image.<ext>`; Export all my data dropped each session's path and now also takes the
folders out of each stored marking, which markings saved before this change still hold (`MarkingFile.WithoutFolders`); the shot CSV
names the session by the photo's name only. **Archived submissions:** the upload page's receiver keeps the browser's file name, which
browsers send without folders and PHP strips of any, and the application's packages never held a path, so no archived submission can
carry one; the server's archive itself was not opened (that needs SSH).

**Section 4, tests:** `Entry383Tests` (desktop: the saved file holds the name only; moved together, it opens with nothing asked; an
old file with a whole path that is gone opens by the name beside it; alone, the message and the picker), `MarkingFilePathTests` (both
separators, the search order, the stripping), and `ViewRotationTests` now expects the name only. **Section 5:** the user guide says the
marking names its photo by file name and that the two travel together; the release note follows.

## Entry 384: the analysis key's blank strip, Add readings, and Play testers on old builds (2026-10-07)

**Section 1:** with the key beside the analysis drawing (or below it), the drawing was clipped to the area clear of the key for the
whole height, so everything right of the key's left edge was blank paper, black in dark mode. The group is still fitted to that area,
so the key never covers it, but the drawing now runs over the whole canvas and the key covers only its own box (`CompositePlot.Render`);
the aim and center lines and the velocity band reach the canvas's edge. The phone draws the same control. `Entry384Tests` renders the
plot light and dark at 1000 by 420, 1500 by 700 and 700 by 420 (key below) and fails on the old clip in all four.

**Section 2:** Add readings opens Ballistics with the Chronograph section scrolled into view, outlined in amber for four seconds, and a
first line saying what to do: "Import a file from your chronograph (CSV, TXT or Excel), or paste the velocities, then Accept the
mapping." The phone's own Chronograph readings page says the same. Chronograph entry on the analysis itself is question 88 (DESIGN
NEEDED). The user guide says where Add readings goes. `Entry323Tests` checks the outline and the line.

**Section 3:** (a) request 80 has Alan's steps (Play Console, Google Cloud service account with release rights on this app only, its
JSON key as the secret `PLAY_SERVICE_ACCOUNT_JSON` that he adds); `.github/workflows/play-upload.yml` and `scripts/play-upload.py` send
each published nightly's AAB and native symbols to internal testing, skipped while the secret is absent; a dry run on nightly 175's
bundle passed. (b) the download page, README and testing guide now say the plain APK and the Play copy do not update themselves for now,
and GroupLab Dev does and installs beside either. (c) the internal track: the last upload on record is nightly 110; request 80's first
step asks Alan which version Play Console shows.

**Request 79 (Alan, 2026-10-07):** org.grouplab.app verified with the upload key, no APK needed; GroupLab Dev's verification APK built
with `android-verify.yml` from his snippet (package org.grouplab.app.dev, only `assets/adi-registration.properties`, signed with the
upload key, checked with apksigner) and put in `C:\Dev\grouplab-local\android-verification\`; request 79 rewritten (Add key takes the
SHA-256 fingerprint, not the certificate).

## Entry 379 section 3 and the consistency audit of 2026-10-07 (2026-10-07)

**Entry 379 section 3:** the page What GroupLab sends now covers the computer and both phones: its title, the log in automatic error
reports and Send everything I open on Android and the iPhone since nightly 173, and that a phone sends over Wi-Fi unless mobile data is
allowed; it already listed the survey's random installation number, the log and sent photographs. The README and the guides agree.

**The consistency audit of 2026-10-07** (ten findings, read before its inbox file was replaced by entry 384, question 89), all fixed:
1. Features: `send-targets` lists both phones and says so, its 173 note claimed, and a reason for its missing phone picture;
   `error-reports` says the log goes from the phones too; PHONE-PARITY's row is "on the phone" and "on iOS"; PhoneSending.cs's comment.
2. The support page has a "Delete your data" section (`/support/#delete-your-data`): what the project can hold, no accounts, how to ask
   by email with the date and build of what was sent, and Delete my survey reports in Settings; no time limit promised. Settings does
   not show the installation number, so the page does not ask for it. PLAY.md's "entry 378 section 4" corrected.
3. The M834 print is in `phone-targets` on the Features page, its four notes claimed there, in PHONE-PARITY and in the user guide's phone
   section, with Paper in the M834.
4. The phone's button now reads "Print on the Phomemo M834 (Bluetooth; true size still being checked)"; the README gives the M834 a
   Status bullet of its own and leaves "(no label printer has printed from GroupLab yet)" on the label bullet.
5. The tour: Capture says Mark it by hand sets the scale, aim points and holes under the crosshair on a target GroupLab did not print;
   Marking says only the lasso is on the desktop alone.
6. Shoot a target: the C3 grids have no real photograph yet, the E and C bulls one range day.
7. README: "Of the computer downloads, only the Windows installer updates itself."
8. Both guide PDFs regenerated (`grouplab user-guide`), and `scripts/consistency.py` now reports a guide PDF older than its Markdown.
9. The `csv` feature says several files import as one group and each row names its sheet, session and distance; its note claimed.
10. README: the GroupLab Dev row says "see On Android, below".

## Entry 385: request 78 answered, the M834's first true-size prints measured (2026-10-07)

Alan printed the C bull sheet and the Letter printer check from the Fold 7 on nightly 175, on a roll: both whole, both past the tear
bar, each 293 mm end to end (a page 0.8 percent short, 277.2 mm, plus entry 382's 15.5 mm feed, 292.7 mm: the feed works). On the check
page, caliper across 150.47 mm of 150.00 (**+0.31 percent**), caliper down 148.81 mm (**-0.80 percent**), ruler across 190.0 of 190.0, ruler
down 248 of 250 (-0.80 percent). So the M834 prints true across the paper and 0.8 percent short along the feed.

**The scans as a cross-check** (`grouplab measure`, `C:\Dev\grouplab-local\printers\`, local only): "thermal test10072026.png" is the C bull
sheet and "thermal test 210072026.png" the check page; both read **99.98 percent across and 99.5 percent down** at the page center. They
agree on the direction (true across, short along the feed) and differ from the caliper by 0.3 points across and 0.3 down; the paper's
curl bowed the scans, so the caliper is the reference, as planning said.

**Section 1:** a printer check lives in each device's own settings, so it cannot be written from here; request 81 gives Alan the caliper
path with his numbers (Phomemo M834, across 150.47, down 148.81, which GroupLab turns into 100.3 by 99.2 percent, each direction on its
own). **Section 2:** no stretch yet; request 81 asks for the same four numbers from a second print, and +0.81 percent along the feed for
the direct print follows if the 0.8 repeats. **Section 3:** request 78 closed and archived, its note saying to measure on the check page,
which has the lines. **Section 4:** the user guide's phone section says to flatten a thermal sheet before scanning.

## Alan's message of the evening, 2026-10-07: the guard at 98, requests 79 and 80, questions 84, 86, 87 and 89

- **The usage guard** (`scripts/usage-guard.js`) stops at 98 until the week resets on 2026-10-08 02:00 UTC and goes back to 85, with 88
  the last line for a finishing block, by itself; its stop message now names the line in force. Checked with a reading of 97 (passes) and
  98 (blocks).
- **Request 80 answered:** `PLAY_SERVICE_ACCOUNT_JSON` set (2026-10-07 19:21 UTC); Play's internal test showed nightly 110. The nightly
  176 upload of 18:35 UTC had skipped for want of the key, so `play-upload.yml` was run once by hand for 0.2.0-nightly.176 (run
  37674523785): bundle uploaded as version code 176, native symbols uploaded, the internal track committed. The README, the download
  page, the testing guide and docs/ANDROID.md section 12 now say the Play copy gets every nightly.
- **Request 79 answered:** Alan uploaded GroupLab Dev's verification APK; both package names are registered with the upload key. The
  four public places that said the APKs were not registered now say they were on 7 October.
- **Question 86:** the 2 MOA 3 by 4 sheet dropped; entry 374 is done. **Question 89:** no number of its own. **Question 84:** A, the
  sentence stays.
- **Question 87, the fingerprint trial again** (`grouplab fingerprint-trial shipped`, new: the application's own recognizer and the
  nine shipped fingerprints over the trial's pictures from seed 332 and the range photographs): wrong products reach 590 features (the 8 in
  Shoot-N-C crosshair on the 6 in and 8 in bulls, layout 0.55 to 0.65), and every second line that would reach the refused NTC and Rigid
  photographs names the wrong product for 1 to 64 of the 200 pictures once their own product is absent (16 to 23 at the 0.55 NTC needs).
  **The rule stays**; details in `docs/notes/fingerprint-trial.md`. The 190 MB of made pictures were deleted after.

## Entry 386: the plain APK updates itself, identification without markers, the M220's check, Send to GroupLab, pooled sheets (2026-10-07)

- **Section 1, the plain APK updates itself.** `GroupLabUpdater` is on for every APK and off for every AAB and the emulator's sweep build
  (`GroupLab.Android.csproj`); the nightly builds the plain APK on the nightly train, checks both APKs carry the updater, and lists it in
  the signed manifest as `android apk`; `SelfUpdate` is automatic by default on both. An APK from before nightly 177 has no updater, so its
  first update is one newer APK by hand, which the download page, README, both guides and docs/ANDROID.md say. Test:
  `UpdaterFlavorTests`. Not yet seen on a phone: the first nightly with it is 177.
- **Section 2, question 83 (b).** `SheetIdentification.Identify` reads a picture with no GroupLab marker once, whole, at full size, without
  the other sizes, the corners or the cut-outs. Measured first with `grouplab identify-trial` over the local corpus (samples, the corpus,
  the range photographs, camera-0929, the scale test, the tablet, printer and M834 scans, the store-bought targets and the surface trial):
  175 pictures, 78 with GroupLab markers (the same path either way) and 97 without; **no sheet lost**, and the 97 took 334 s against 1960 s the old way, 3 to 11 s each against 15 to 41. Tests: `SheetIdentificationTests.APictureWithNoMarkerIsReadOnceWholeAndItsCodeStillNamesTheSheet`, and the old path kept
  for the test that tries every resolution.
- **Section 3, entry 372's remainder.** The M220 is in `printers.json` (Phomemo ESC, Bluetooth LE service ff00, write ff02, notify ff01 and
  ff03, 203 dpi, label widths 20 to 75 mm, untested by this profile); the ESC encoder sends a label taller than 1200 rows in blocks of
  1200, as phomemo-tools does. **The printer check label** (`ScaleLabelCheck`): two rows of codes at the label's top and bottom, millimetre
  marks each way, saved on Targets, Scale markers; **Measure a scanned check label** reads a 600 dpi scan and gives the scale across the
  head and along the feed, measured square to the rows; on rendered labels 100.00 percent across, and a feed made 1.2 percent short read
  as such within 0.2 (`Entry365Tests.ThePrinterCheckLabelMeasuresAcrossAndAlong`). The result is kept as the printer's check with its
  label size (`PrinterProfile.LabelSize`), and the size is read from there once one exists. **The phone's Bluetooth LE link**, built the
  same evening after question 90 (Alan): `AndroidLePrinter` finds the M220 paired, or from Android 12 by a ten second scan declared never
  for location, connects, asks for room for 128 byte writes, turns on its answers and writes each chunk with a response; Targets, Scale
  markers, **Print two scale labels on the Phomemo M220** sends two labels of the size loaded at exactly the label's width. Built for
  Android and compiled here; not yet printed on a real M220 by this path.
- **Section 4, Send to GroupLab** (entry 376 B7). On the phone, Settings, About, first: the two newest logs (typed names and notes replaced
  by their length, paths already out) and the newest crash record go to the crash receiver as a package, then a report of the kind
  `diagnostics`, made by hand, with the package's reference and the person's note, to the error receiver; the error worker opens an
  issue of its own for each, titled by the note (`ErrorReports.SendDiagnosticsAsync`, `error-report.php`, `grouplab-error-worker.py`).
  Share diagnostics stays as the second choice. Tests: the receiver's three new checks (CI), the worker's three (26 of 26 here). **Live
  once deployed:** the receiver with the next site publish (question 90), the worker with `install.py` under sudo.
- **Section 5, question 34.** `PooledSpread`: each sheet measured from its own centre, the radii pooled, is the headline; every shot from
  one centre beside it; and the root mean square distance of the sheets' centres from their mean, named as the movement between sheets.
  Shown when imported rows name their sheet and session (`ShotCsv.SheetsOf`), on the computer's import line and the phone's import page.
  Built as proposed, for planning to confirm. Tests: `PooledSpreadTests`.
- **Question 90, answered by Alan the same evening**: (a) `phoneMaxNightlies` 90, and the site published again (6c68f164); (b)
  `scripts/android-screens.sh` in the emulator's run takes the phone's published screenshots at each pictured device's size, light and
  dark, and commits them with the nightly (`AndroidEmulatorSweepTests.TheEmulatorTakesEveryPublishedPhoneScreenshotUnderItsOwnName`).
  The error worker's reinstall is a command for Alan (panel.md), because this session has no way onto the server.
- **The standing rule** (Alan, 2026-10-07): before ending a turn, the inbox is looked in and waiting entries taken, in CLAUDE.md.

## Entry 387: a site check holds back only what it is about (2026-10-07)

- **Sections 1 and 3.** `website/build.py` sorts its checks three ways. The whole site's safety and truth (the figures, limits.json, links,
  tables, PHP, the send page, devices, downloads, How it works, privacy) still stop the publish. The page checks (research articles, figure
  themes, the tour, features, glossary words on a page, phone parity) name their pages (`pages_of`), and only those are held back: the
  copy grouplab.org serves now is fetched and kept (`GROUPLAB_LIVE_SITE` in website.yml), or, where it cannot be, the page publishes and
  is said to be stale. Screenshots that are only old (`screens-stamp.py`, the README's images) publish and are reported. The home page,
  the release notes and the download page are never held back. A page check that names no page stops the publish, the safe way round.
- **Section 2.** Every held or stale page is one line, printed by the build, put in website.yml's summary, and picked up by
  `scripts/consistency.py`, so the weekly `consistency` issue lists it; STATE.md says each gets a line in for-alan.md when it appears.
- **Section 4.** `tests/python/site-holdback-tests.py`, 10 checks, run before every site build: a stale phone screenshot holds back
  nothing and everything else goes out new; a research article's fault holds back only that article; a tour fault only the tour; the
  release notes are never held; a whole-site fault still stops it.

## Entry 388: work that needs nobody, at the start of the new week (2026-10-08)

- **Error report 25, before the entry** (a bug Alan hit, nightly 176): a click with the hand tool near nothing threw "Sequence contains no
  elements" from the search for the nearest line, a `MinBy` over value tuples. Now nullable; `CrashReport25Tests` reproduces it on the old
  code. The issue closes with the commit and the nightly it ships in.
- **Section 1, the phone pictures on the emulator.** `scripts/scenarios/phone-screens.json` now also takes Targets' Scale markers card,
  the second step of Add a store-bought target (with `scripts/scenarios/stand-in-poster.jpg`, drawn by `StandInPoster`), the pairing rows
  for typed readings, and the Open targets sheet with two targets open; `docs/figures/screens/phone/fold-*-{light,dark}.png` and the
  Features page carry them. A `pick` step hands a photo to the screen asking for one. The first run (37735635656) found three faults of
  the walk, fixed with the walk now run headless first (`PhoneScreensScenarioTests`): Next pressed before the photo reached the screen; a
  new picture replaces the target showing, so the second is opened with Open another target; and a wait that found the first result left
  the second reading running at the force-stop, **after which no later start of GroupLab Dev on that emulator ran anything**, in that
  step or the next ones. With the walk ending cleanly every pass worked (37755482923, 34 pictures), and the fourth run (37762745721) pictured the store-bought target's second step once the poster was put on the device too. Whether a person whose phone kills
  GroupLab in the middle of a reading meets the same thing is not established: the logs of that run were not kept, and the scripts now keep
  them. The workflow's commit step now stages first, since `git diff` never saw a picture taken for the first time.
- **Section 2, the quality sweep.** Every scenario screenshot writes `<screen>.quality.json` (`Scenario.Quality`): controls under 44 units,
  words cut short, anything past the side, text over text. `scripts/android-quality.sh` runs the sweep at the Fold 7's cover screen (411
  units), its inner screen (750) and a small phone (360), light and dark, and the cover screen at the largest text; `scripts/phone-quality.py`
  gathers the report. One real fault: **the Counted and Left out switch on the Shots page was 32 units tall at every width**, now 48.
  The other 39 lines were the check's own (a scroll bar's arrows, the switch's hidden word, text clipped under the tab bar) and it no
  longer counts them. Nothing needed a layout decision, so no DESIGN NEEDED. `QualitySweepTests` holds the sweep's screens at 360 units.
- **Section 3, question 43 (answered, archived).** The 400 megapixel cap is judged from the file's header before anything is decoded, on
  `ImageLoader`'s loads and on the store-bought target's and scale markers' own decodes (`ImageLoader.Checked`); the desktop shows the size
  and the limit. Open, drop and paste decode on a background thread and stop waiting after 60 seconds, saying so; a slow open finishing
  after a newer one is put down. The phone already read on a background thread and shares the cap. `Question43Tests`.
- **Section 4, Phase 9's baseline.** Nothing changed for speed. `docs/performance-baseline.json`: on TACIT-BLUE, start-up to a usable
  window 1081 ms, opening a 600 dpi scan as Open does 965 ms, switching between three open targets 7 ms, identification 210 ms, hole
  detection on the 600 dpi scan 1769 ms, one sheet from file to figures 530 ms, ten sheets 4150 ms. The gate (`BenchGate`: more than a
  quarter and 20 ms slower fails) is `grouplab bench --gate`, `GROUPLAB_PERF_GATE=1` on `Phase9BaselineTests`, and
  `scripts/startup-time.ps1 -Gate`; all three passed on the code that set it. On GitHub's emulator (`scripts/android-perf.sh`, a record,
  never a gate): start-up 2031 ms, a 600 dpi reading 14.4 s (7.1 s of it hole detection, 2.6 s identification), a switch 162 ms. Proof
  checklist item 31 is met.
- **Section 5, question 34 confirmed** (option C with B as the headline) and archived with question 43.
- **Section 6.** STATE's line on the desktop tabs corrected: merged at 21:02 on 2026-10-07 (86a8d207).

## Entry 389: the M834 from the computer, the sweep on nightly 179, and no wait left running (2026-10-08)

- **Section 1, the M834 from the computer.** On Windows a paired M834 is a Bluetooth serial port, so the computer now writes the phone's
  bytes into it. The page encoding, the sending in the profile's chunks and the wait for the printer's `1A 0F 0C` moved into
  `M834Print` (Core), which the phone's print and the computer's both call, so the two cannot drift. `BluetoothPorts` finds the outgoing
  COM port by the device's Bluetooth name from what Windows keeps about paired devices (read only, the address never logged);
  `WindowsSerialPrinter` opens it within `PrinterConnect.Timeout`, writes with a 15 s limit a block and reads answers without waiting.
  Not paired, no answer (off, or a phone holding it) and busy (another program has the port) each have their own words
  (`SerialPrinterWords`). It goes through `IOutsideWorld.OpenSerialPrinterAsync`, whose default, and macOS and Linux, says it is not
  available on this computer yet; the Targets screen shows the choice there with that line rather than hiding it. With **Phomemo M834**
  chosen under Print on, the panel shows Paper in the M834 (roll or fanfold, the same saved setting as the phone), a line on pairing, the
  button and Cancel; the M834's head there is now its profile's 2528 dots (was 2560), the page the phone draws. A saved printer check is
  not applied to the dots on either: it corrects photographs, chosen in Settings, until request 81's second print says whether the 0.8
  percent along the feed repeats. `Entry389M834Tests` sends the printer check sheet through a fake port and finds exactly the phone route's
  bytes; `M834ComputerTests` holds the port finding, the words, the encoding against the phone's old code and the send. No layout decision
  was needed. Request 83 (for later) has the pairing and the measuring steps. Not yet tried on a real M834 from a computer.
- **Section 2, the sweep and the baseline on nightly 179** (android-emulator.yml run 37817278203, main at a57093f1): **no faults at any
  size or theme**, 11 screens at each of seven sizes and themes; the Shots switch is 48 units tall at 411, 750 and 360 units, light and
  dark, and at the largest text. The emulator baseline again, recorded beside the first in `docs/performance-baseline.json`: start-up
  1318 ms (2031 the first time), a 600 dpi reading 8.9 s (14.4 s), a switch 105.5 ms (162). Nothing changed for speed between the two, so
  the difference is GitHub's runner; it stays a record, never a gate.
- **Section 3, no wait left running.** Nothing from entry 388 was still running here. CLAUDE.md's Waiting section now says every wait has
  a time limit and every background task an entry started is stopped before its report.
- **Section 4, STATE.** Question 80 (answered in entry 347) moved to the answered archive and off STATE's open list; STATE says entry 380
  section 3 waits on request 77's photos.

## Entry 390: the second M220 label of a job printed shifted (2026-10-08)

- **Section 1, label S1.** The planning session's reading of Alan's scan closes request 82 step 2 for S1: its codes 59.99 and 59.96 mm
  apart across (60.0 designed), rows 10.39 mm apart (10.5 designed, about 1 percent short along the feed, which the layout does not
  trust), codes about 7.8 mm. Request 82 answered and archived.
- **Section 2, label S2.** GroupLab's bytes for the labels of one job are the same but for their codes and serial: the same 11 bytes of
  commands and raster header, the same 8 at the end, the ink beginning at the same byte of every row (`M220LabelsTests`). So the shift
  arose between the phone and the paper. The phone's writes over Bluetooth LE are acknowledged one by one, and the only thing that set the
  second label apart was that its first block went out straight after the first label's end commands, with no pause, while the printer was
  still finishing and feeding the first. A whole-label shift of about 60 dots, wrapped round, with no shear, is what losing about 8 bytes at
  the start of the rows gives. **Fix:** `PrinterJob.SendLabelsAsync` leaves the printer `LabelSettle`, eight seconds (what the last label
  already had), after every label before the next; the phone says which label is printing. A fake printer that loses the first rows
  arriving while it feeds reproduces the shift back to back (8 bytes, 64 dots) and lines up three labels with the pause. **The cause is
  inferred from the scan and the bytes, not observed on the M220**; request 84 prints two labels again to confirm it.
- **Section 3.** Request 77 is unchanged.

## Entry 391: the M834 feeds short, so its pages are drawn longer along the feed (2026-10-08)

- **Section 2 first, the across in dots.** On the M834's 300 dpi head the Letter check page is 2550 dots wide; the 2528-dot head loses 11
  each side, a whole number of bytes, so nothing is shifted (`PhomemoLzoEncoder.OnHead` copies the rows as they are). The top crosshairs'
  vertical arms are 1772 dots apart, 150.02 mm (100.01 percent of 150.00, the nearest whole dots), and the bottom ruler's end ticks 2244
  dots, 189.99 mm. Along the feed, unstretched, the crosshairs were 1772 rows apart (150.02 mm), the side ruler 2953 rows (250.02 mm), the
  page 3300 rows. **Both prints were rendered the same:** nothing that draws or encodes an M834 page changed between nightly 175 (print 1)
  and 180 (`ThermalRaster`, `SceneRasterizer`, `GridStyle4`, the LZO encoder and the M834's profile are unchanged; nightly 176's encoder
  change is the M110 family's), both were on the roll, and the roll only adds 15.5 mm of white after the page. So print 1's 100.31 percent
  across was the caliper, and print 2's scan, 100.01 percent, agrees with the rendering to the hundredth.
- **Section 1, the stretch.** Five readings of the feed over the two prints, against what GroupLab drew (150.02 and 250.02 mm): print 1,
  caliper 148.81 (99.19 percent) and ruler 248 (99.19, from entry 385); print 2, caliper 149.03 (99.34), ruler 248 (99.19) and scan 149.25
  (99.49). The mean is 99.30 percent; the instruments weighted equally (calipers 99.27, rulers 99.19, scan 99.49) give 99.32, and the
  planning session's four against the drawn 150.00 gave about 99.35. **Chosen: 99.30 percent, a stretch of 1 / 0.9930 = 1.0070,** the plain
  mean of everything measured; every reading is within 0.2 percent of it, which is the rulers' resolution and the caliper's own spread
  between prints. `M834Print.MeasuredFeed` holds it and `M834Print.Head` is the profile's head with `PrintHead.FeedStretch`;
  `M834Print.Encode`, which the phone's and the computer's direct prints both call, draws on it, so nothing else changes: other printers,
  the preview and the PDF for the Phomemo app are drawn as before. `ThermalRaster` places rows along the feed at `RowsPerUnit`, a code's
  module its own whole number of rows (still 5 by 5 at 0.4 mm), a line's width rounded once; `SceneRasterizer`'s `stretchY` decides a
  bull's band in the unstretched frame, so a circle prints as a circle. The check page now: crosshairs 1784 rows apart (151.04 mm drawn,
  149.99 at 99.30 percent), side ruler 2973 rows (251.71 drawn, 249.95), page 3323 rows (`M834FeedTests`).
- **Section 3.** On print 2 the scan reads larger than the caliper both ways, by 0.21 percent across (150.02 against 149.71) and 0.15 down
  (149.25 against 149.03). Across, the scan agrees with the rendering and the caliper does not, which points at by-eye centring; but print
  1's caliper read 0.30 percent high across, so the caliper's error is a spread, not a bias, and two prints cannot separate it from the
  scanner's own scale along its carriage. Recorded; nothing to fix.
- **Section 4, the saved check.** One check per printer, kept: the one saved on the Fold 7 (100.3 by 99.2 percent) fits sheets printed
  before the stretch along the feed (they print 99.2 to 99.3 percent) and is 0.3 percent high across, where the true figure is 100.0. After
  the stretch the M834 should print about 100 by 100 percent, and the saved check would then shrink photos of new sheets by 0.7 percent along
  the feed. So no separate check for new sheets: request 85 prints the check page once more after the nightly with the stretch, measures
  the same four numbers, and replaces the saved check with them; Alan does not shoot on M834 sheets printed before it. Request 81 closed
  with these numbers; request 83 (the M834 from the computer) now follows 85.
- **Section 5, the shear.** The top and right caliper lines on print 2's scan meet at 90.22 degrees: the paper tracked about 1 mm sideways
  over 250 mm of feed. A scanner cannot add shear. Noted; nothing corrected unless a later print repeats it.

## Entry 392: twelve research drafts, the first speed-up, question 44 (2026-10-09)

- **Section 1, the drafts.** Each article's numbers were checked against its data files and the code it describes, its figure scripts
  rerun in a scratch Python with numpy 2.5.3, scipy 1.18.1 and matplotlib 3.11.2 (every one byte-identical to the committed data and
  figures; the machine's own Python has none of the three, so the site build uses the committed figures), and every checkable sentence
  backed in `docs/claims-backing.json` one by one: 245 sentences over eleven articles, `claims.py --check` 4893 claims, 0 unbacked.
  docs-sonnet did a wording pass after the checks; a count of every number before and after showed it changed none. **Eleven `ready`:**
  mean-radius-or-extreme-spread, how-many-shots, cep-explained, velocity-sd-small-samples, when-to-adjust-zero, moa-mils-inches,
  printer-true-size, scanner-traps, photographing-targets, printed-numbers-are-not-holes, range-test-log. **One `draft`:**
  aim-points-by-optic-class, whose red dot, prism and medium power classes are not shot; request 86 asks for them, for later.
- **What the checks corrected:** the shotGroups author is Daniel, not David (five articles); a GroupLab sheet has up to 42 bulls, not 25;
  a 9 percent difference in dispersion needs 434 shots of each, and "more than many barrels fire in their lives" had nothing behind it;
  CEP 90 is 1.7 times mean radius, not "a little under twice"; the CEP circles are dotted, solid and dashed for 50, 90 and 95, each on its
  own switch; two velocity SDs of 8 and 12 are not told apart by twenty shots each (p = 0.085) but are by thirty (p = 0.033); the zero
  article described a picture with an arrow that entry 169 removed, and now describes the per-axis block; the first run asks inches or
  millimeters; a scan's stated resolution is believed only within 15 percent of the sheet's size; request 18's angled photographs are
  answered, so the photo article no longer waits on them; a friend was named in range-test-log and is not now. printed-numbers-are-not-holes
  had two numbers with no record behind them (straight runs, 0.69; enclosed share, 2 percent): they are gone, the rest backed by commit
  9421ee13, and it has a figure of drawn shapes. photographing-targets' 0.86 in was measured again: `grouplab analyze` gives 0.859 in.
- **printer-true-size with real data:** a section on the M834 (true across, 99.28 percent along the feed, the instruments 0.2 percent
  apart, the 90.22 degree shear) and the M220 label (59.99 and 59.96 mm for 60.0 across; 10.39 for 10.5 along the feed), with
  `data/thermal-check-prints.csv` and a figure. **Entry 391's mean was not 99.30:** the five readings against what GroupLab drew average
  99.28 percent (instruments weighted equally, 99.32). `M834Print.MeasuredFeed` stays 0.9930: the 0.02 is a tenth of the readings' spread,
  and request 85 measures the stretched print.
- **Section 2, the speed-up.** Profiled with temporary timings in `RenderDifferenceHoleDetector.Detect` (removed): of the 2253 ms the
  hole stage took on the bench's 600 dpi scan that night, the close of the binary mask by a disc 67 px across took about 1700 ms, the open
  about 100, alignment 220, everything else under 120 each. OpenCV takes the maximum over every point of the disc at every pixel.
  `BinaryMorphology.Close` does the same close from each row's distance to the nearest set (or unset) pixel and the element's own row
  half widths, read from `Cv2.GetStructuringElement`, so the result is the same by construction: 17 ms. `OpenCvSharpBackend.Morphology`
  uses it for a close of a 0 and 1 image and OpenCV for everything else. **Identical results:** `BinaryMorphologyTests` compares it with
  OpenCV pixel by pixel at ten radii from 1 to 50, edges included; a temporary check ran both on every close the local corpus, the
  corpus counts and 30 real scans and photographs make, 133 closes, 0 pixels different; and a temporary switch back to OpenCV gave
  identical output for `scoreboard --corpus` on all four local corpus folders, `scoreboard --synthetic`, `corpus counts` and `analyze` on
  the 30 images (timings aside). **Measured** with `grouplab bench --runs 5` on TACIT-BLUE while another program held about 80 percent of
  the CPU, so the before and after were also run back to back: the scan's hole stage 1769 ms in the baseline to 693, the whole scan 2604
  to 1671, the generated sheet's hole stage 333 to 294; end to end, one sheet 879 to about 720 ms and ten sheets 6196 to about 5180, back
  to back. The gate passed, and the four stage-by-stage measurement figures were moved in `docs/performance-baseline.json`; the others keep
  their tighter 2026-10-08 values. **Tried and not needed:** nothing else; the open (100 ms), the alignment (220) and the rasterising (110)
  are what is left, and halving was passed without them.
- **Section 3, question 44.** A Debug build put the throw in `SurfaceMapping.ToImage`'s table, not `FoldedSheet`'s folds: the photograph
  holds several sheets, `ToPage`'s Newton steps for pixels on the others run far off the page, `(int)Math.Floor(u)` saturates at
  `int.MaxValue` and `i + 1` wrapped negative past the bounds test. The range is now tested in double first, the smaller of the two
  changes the entry named; the iteration is unchanged, since the sheet's mask removes those pixels. `compare-photos --model surface` on
  `20260920_153336.jpg` completes: bull median 0.0050 in against the homography's 0.0110, 18 of the scan's 19 holes. `SurfaceCrashTests`:
  a far page point and far image points everywhere (it fails without the fix), and the whole reading of the photograph where it is.
  Question 44 archived.

## Entry 393: ready research articles published (2026-10-09)

- **Sections 1 and 3, published:** mean-radius-or-extreme-spread, how-many-shots, cep-explained, velocity-sd-small-samples,
  when-to-adjust-zero, moa-mils-inches, printer-true-size, scanner-traps, photographing-targets, printed-numbers-are-not-holes,
  range-test-log and can-you-see-the-bull, listed in `website/research/PUBLISHED.md` with 2026-10-09 and entry 393.
- **can-you-see-the-bull, the same checks as entry 392:** its three figure scripts rerun byte-identical, PDFs included; Cohen's kappa
  (0.44, 0.52 weighted, 81 cells, 53 equal, 75 within one), the four groups and the C against E test (sigma ratio 2.18, p = 0.159)
  recomputed from its data; the bull geometry checked against `GL-CF25-LTR-D` and `GL-CF25-LTR-C`. Its 45 sentences backed one by one, and
  the eight index sentences the newly published articles brought. Changed: a friend was named in the text, in `scores-2026-09-26.csv`'s
  scorer column (81 rows) and in `figures/scopes.py`, and is now "the friend"; pronouns for the developer rephrased; the card "taken to
  the range on 2026-09-23" was shot on 2026-09-26; "question 61" and "we propose" reworded; its "see the article" summary written.
- **Section 2:** aim-points-by-optic-class stays a draft (request 86). Nothing published has an unbacked sentence: `claims.py --check`
  4911 claims, 0 unbacked.
- **Section 4, not done:** the CLAUDE.md paragraph. The auto-mode permission check refused the edit as an inbox file changing Code's own
  standing instructions; Code did not route round it. Request 87 holds the paragraph for Alan to add or approve.
- **Section 4, done later the same day:** Alan typed "Add the entry 393 publishing rule to CLAUDE.md." into the session; the paragraph
  is in CLAUDE.md under "Is it worth an article?", and request 87 is answered and archived.

## Entry 394: the learning loop, built to run with no Claude (2026-10-09)

- **Section 1, every submission scored.** `grouplab learn score` (`src/GroupLab.Cli/LearnVerb.cs`) reads a submission folder as the
  archive holds it, takes the person's final marks from the package's `corrected` part as the truth (kept, moved and added marks, a shot
  left out for a reason included, none called not a shot), reads the rebuilt picture with the current build, and matches within 0.1 in:
  one `RealRow` (`src/GroupLab.Core/Evaluation/RealScoreboard.cs`) of numbers and labels, keyed by the archive's folder name. An upload
  from the page and a store-bought target each get a row saying they are not scored. On this computer's copy, 38 submissions: 29 from
  the page, 9 corrected, 225 of 225 holes found, no false marks, centre error 0 (every person kept GroupLab's marks).
- **Section 2, where:** the archive repository's Actions, `scripts/learning/learn.yml` there as `.github/workflows/learn.yml`, reading
  with a checkout of the newest nightly's tag; nothing on the server changes and no arm64 build is needed. Adding a submission to a
  month's release starts no workflow, so four short runs a day compare the manifests with the scoreboard and stop before building when
  nothing is new. **Minutes:** about 550 a month at today's rate, about 1,600 in the first month at ten times it and growing, against
  2,000 (Free) or 3,000 (Pro); the nightly re-read needs a cap before about 150 corrected submissions (study section 10).
- **Section 3, every night:** `learn.py nightly` reads every corrected submission again, runs the synthetic board beside it, and
  `grouplab learn check` holds each row to its baseline with the synthetic margins, writes `learning/summary.md` (totals, no submission)
  and, for a worse line, opens or adds to one issue in `grouplab-crash-reports`, which needs request 88's token.
- **Section 4, tuning, built and waiting:** `grouplab learn tune` searches six thresholds against the synthetic board and the corrected
  submissions not held out; it passes only better on the held-out three in ten, worse on none, with G3 holding around it, and then
  `learn.py` opens a pull request with the constants and the table (testing-only targets as a count only). **It waits for 50 corrected
  submissions** (`RealScoreboard.CorrectedForTuning`; the sign-test derivation is in its comment and the study). `AutomaticMarking.Run`
  and `Scoreboard.RunSynthetic` take optional detection settings for it; without them nothing changes.
- **Sections 5 and 6:** request 89 asks Alan how a tuning pull request is merged; no learned model, and nothing a device does changes by
  itself. **Section 7:** the study's section 10, "What GroupLab sends" (a target is read again by every build, numbers only),
  `docs/RESTORE.md` and `scripts/backup.py` (the archive repository's files in the nightly backup). The README and the site do not
  describe how detection improves, so they did not change.
- **Tests:** `LearningLoopTests` (7): the person's truth, the margins, the baseline moving only for the better, the held-out share, a
  summary of totals only, tuning waiting below 50, and the driver's copy of 50 held to the code's.

## Entry 395: the learning loop to the web server, in part (2026-10-09)

- **Blocked:** the auto-mode permission check refused `ssh ... "uname -m; nproc; free -m"`, a read-only command announced in the panel,
  as remote execution. Without the server, sections 2 (the worker), 3 (the nightly check and tuning there), 4 (its self-update), 6 (the
  measurements) and the nightly half of section 1 cannot be done or even sized. Alan's answer to that is the next step.
- **Section 1, what could be done:** OpenCvSharp's own `OpenCvSharp4.runtime.linux-arm64` 4.13.0.20260627, the version GroupLab uses,
  carries an aarch64 `libOpenCvSharpExtern.so` (38 MB), so no OpenCV build is needed. Its dynamic section names Ubuntu 24.04's libraries:
  `libtesseract.so.5`, GTK 3, FFmpeg 6 (`libavcodec.so.60`), `libjpeg.so.8`, `libwebp.so.7`, `libtiff.so.6`. So it runs as is only on an
  Ubuntu 24.04 arm64 server with those packages, which is the first thing to read there; otherwise a slim library is built in CI.
  `GroupLab.Cli.csproj` takes it for `-r linux-arm64` only (nothing else changes), and `.github/workflows/cli-arm64.yml`, started by
  hand, publishes the command line on GitHub's arm64 runner, free for a public repository (generally available since August 2025),
  lists what the library lacks, installs it, and reads a sample sheet with its time and peak memory.
- **The arm64 trial ran** (cli-arm64.yml run 37913509509, 2026-10-09): Ubuntu 24.04.5 aarch64; before installing, the library lacked
  only Tesseract 5 and FFmpeg 6 (`libtesseract5`, `libavcodec60`, `libavformat60`, `libswscale7`); after, nothing. The arm64 command line
  drew a 300 dpi sample sheet and read it end to end, 38 of 38 markers and 25 shots, in 3.1 s with a 428 MB peak. So the server needs those
  four packages if it is Ubuntu 24.04 arm64, and nothing has to be built.
- **Section 5:** request 88 now has the token typed on the server with `sudo grouplab-set-learning-token`, once the worker is installed.
  The archive repository's workflow was never switched on, so it is already the disabled fallback.
- **Finished 2026-10-09, on the server** (Alan allowed ssh and sudo for this entry): Ubuntu 24.04.5 on aarch64, two processors, 11.9 GB,
  no swap. Installed over entry 397's backup `2026-10-09T112228Z-learning-worker` (78,544 bytes): the six packages (81 with what they
  pull in), the worker, its token script, the archive worker that waits for a score, six units and three timers. The undo's dry run listed
  exactly that. The first nightly fetched nightly 183's command line, checked its signature and its sample sheet, then passed its 1.5 GB
  cap reading ten submissions in one process (one 600 dpi scan alone peaks at 1.1 GB; a phone photo 351 MB; 39 s and 21 s at half a
  processor). The fix, `rescore` in the worker and `OOMPolicy=continue` on the three units, held by a new case in
  `tests/python/learn-worker-tests.py` (13 of 13 on the server's Linux), was installed over a second backup
  `2026-10-09T113248Z-learning-worker-oom` (116,322 bytes). The rerun read all 42 archived submissions, the 9 corrected ones 225 of 225
  holes and no false marks as on this computer, and took 13 minutes 20 seconds with the synthetic board; both sites answered 200 throughout.
  A synthetic board past its hour no longer stops the run. Request 88 is ready for Alan.

## Entry 396: who merges a tuning pull request (2026-10-09)

- Request 89 answered (b), archived. The rule (Code reads only the table and the checks, merges when every gate passes and the table is
  better on the held-out share and worse on nothing, and leaves anything odd for planning, saying why) is in
  docs/DETECTION-LEARNING-STUDY.md section 10 and in the body of the pull request `scripts/learning/learn.py` opens. It is a standing rule
  for Code, so request 90 asked Alan to add it to CLAUDE.md; he asked for it in the session the same day, and it is there. No pull request exists yet: tuning waits for 50 corrected
  submissions.

## Entry 397: a backup of everything changed on the server, before it is changed (2026-10-09)

- **The rule** is `docs/RESTORE.md`'s standing rule 5, with a list of the changes made under it (none yet).
- **The tool,** `website/server/grouplab-change-backup.py`: run with sudo on the server before a change, it copies each file the change
  will edit, replace or create with its mode, owner and SHA-256 (one that does not exist yet is recorded as absent, so the undo deletes
  it), `dpkg --get-selections` and `apt-mark showmanual` with `--packages`, `systemctl list-unit-files` and `list-timers --all` with
  `--units`, and crontabs, into `/home/ubuntu/grouplab-server/backups/<UTC time>-<label>/` with a manifest and its size; it refuses a
  path under `conf/web/` for the backup and has a `--dry-run`. `tests/python/change-backup-tests.py` holds its dry run, in CI on Linux.
- **Section 1 stops the server work:** the Oracle boot volume backups are seen only in the console, and their restore was never tested
  (entry 230 closed "apart from the proof"). Request 91 asks Alan to note today's backup and restore it to a throwaway volume. Until
  then nothing on the server changes; entry 395 builds what it can without it.

## Entry 398: the server's backup restores; five days of backups (2026-10-09)

- **Request 91 answered:** Alan restored the 2026-10-09 09:02 UTC boot volume backup to a throwaway volume, which came up Available at
  47 GB and was terminated. `docs/RESTORE.md` records it as the first restore test of the whole server, with how it is done; the console's
  action is "Restore boot volume", now the wording there and in the archived request. Request 91 archived.
- **Retention:** one backup a day kept 4 days, within Always Free's five, set by Alan in the console; the older backups expire on their
  own by 2026-10-22. RESTORE.md says a server mistake must be noticed within about four days to be undone from the whole-server backup,
  while Code's own dated copies of changed files stay until removed.
- **Question 93 answered, the packages,** and archived. Entry 395's server half goes ahead under entry 397's rule 5.

## Requests 84, 85 and 88 answered (2026-10-10)

Alan's message of 2026-10-10, not an inbox entry: the two scans measured, the requests closed, request 83 written now that it is free.

- **Request 84, the M220 labels again.** `two-labels-again-600dpi.png` holds labels S3 and S4, the same, all four codes whole on each.
  Measured at 600 dpi from the codes' corners, each label turned square by its own 3.4 degree tilt on the glass: codes 59.98 and 59.96 mm
  apart across on S3, 59.96 and 59.97 on S4 (60.0 designed); rows 10.35 and 10.37 mm apart on S3, 10.45 and 10.38 on S4 (10.5 designed,
  about 1 percent short along the feed, as S1 was); codes 7.69 to 7.79 mm (8.0 designed, the ink's edge). Entry 390's eight seconds
  after each label is confirmed on one job of two: S2's shift did not recur.
- **Left of centre: yes, on every good label.** Against the label's own die-cut edges (the label face reads brighter than the liner, and
  its right edge leaves a line inside the liner's), the codes' ink is 0.47 mm from the left edge on S3, 0.30 on S4 and 0.72 on S1 (the 8
  October scan), and 1.61, 1.74 and 1.48 mm from the right. The layout puts each code's outer edge 1.0 mm in (`ScaleLabels.Inset` 5,
  `Code` 8), so the print sits 0.57, 0.72 and 0.38 mm left of the label's centre, a mean of 0.56 mm, about 4.5 of the M220's 8 dots a
  millimetre; the labels read 69.93 to 70.06 mm wide. GroupLab sends the M220 its 70 mm image at its own width (`PhomemoEscEncoder`, 560
  dots for a 600-dot head) and does not choose where on the head it lands, so the offset is the printer's placement or the roll's guide,
  and the 0.34 mm spread between labels is the roll wandering. Nothing is cut: the worst, S4, keeps 0.30 mm beside its codes, and the
  scale comes from the codes' spacing, not from where they sit. Question 94 asks whether to move it.
- **Request 85, the M834's third check print, after the stretch.** Alan's caliper 150.0 across and 150.0 down, rulers 190 and 250. Its 600
  dpi scan (each crosshair's centre where straight lines fitted to its arms outside the circle cross; each ruler end the same way): 149.98
  mm across (drawn 150.02), 150.07 mm down (drawn 151.04, designed 150.00), bottom ruler 189.93 (drawn 189.99), side ruler 249.93 (drawn
  251.71, designed 250.0). The bottom ruler's right end runs off the scan's bottom edge; its upper half was fitted. **The stretch works:**
  the M834 now prints true both ways to 0.05 percent by every instrument. Along the feed against what was drawn: caliper 99.31, ruler
  99.32, scan 99.36 and 99.29 percent; with entry 391's five readings the nine average 99.30, so **`M834Print.MeasuredFeed` stays 0.9930**,
  the question entry 392 left to this print. The M834's check is now 150.0 by 150.0 (100.0 by 100.0 percent), which request 85 step 3
  asked Alan to save; request 83's first step repeats it in case. The top and right caliper lines meet 0.15 degrees off square (print 2:
  0.22), the paper tracking about 0.65 mm sideways over 250 mm; recorded, as before.
- **Request 88, the learning token.** Alan set it on the server. The nightly check uses it only when a line reads worse (`report` in
  `grouplab-learn-worker.py`) and the monthly tuning only for a pull request; the summary goes to the archive with the archive token. So
  the next nightly check, 13:45 UTC, proves nothing about the token on a quiet night, and with no nightly build since 183 its summary is
  unchanged and it writes nothing (the archive's last `learning:` commit is 2026-10-09 11:46 UTC). Code's read-only check over SSH,
  printing status codes only, was refused by the session's permission check; request 92 gives Alan the same check as two lines, and
  question 95 asks whether the nightly should check the token itself.
- **The research article** "Does your printer print at true size?" now says the third print came out true and that the M220's short feed
  repeated on two more labels, each sentence backed in `docs/claims-backing.json`. Its figure is unchanged: it shows the two prints
  before the stretch.
- Requests 84, 85 and 88 closed and moved to `for-alan-archive.md`; request 83 rewritten; request 92 added. Open: 23.

## Request 92 closed, questions 94 and 95 answered (2026-10-10)

Alan's answers, in the message that started the run, with no inbox entry.

- **Request 92:** both token checks on the server printed `200`: the learning token reaches the error reports and GroupLab's pull
  requests. Closed and moved to `for-alan-archive.md`.
- **Question 94, answer (a):** the M220's placement stays; it moves only if a label ever comes out with a code cut. Archived. Question 88
  went to the archive with it by mistake in 035f0d8e and is back in the live file, still open.
- **Question 95, answer (b):** `token_check` in `website/server/grouplab-learn-worker.py`. Every nightly, before anything else, asks
  GitHub with the learning token for one issue in the error-report repository and one pull request on GroupLab's, logs the sentence, and
  appends it to `summary.md` after `grouplab learn check` writes it, so the archive's `learning/summary.md` carries it. A refusal is
  written with GitHub's status code; no answer is written and does not stop the run; the token never appears. Five new checks in
  `tests/python/learn-worker-tests.py` against a stand-in GitHub, run on every platform: 7 passed locally.
- **The server install is not done.** Code's copy of the worker to the server was refused by the session's permission check, so request
  93 gives Alan one paste: a backup of the installed worker under RESTORE.md rule 5, the new worker fetched from main and refused unless
  its SHA-256 is `cdf083f7...`, installed, and the check run once in a transient unit. RESTORE.md lists it as waiting, with its undo.

## Entry 399: accuracy on photographs (2026-10-10)

### Section 1: where a photograph's bull-centre error comes from, measured before anything changed

Tool: `grouplab photo-parts` (`src/GroupLab.Cli/Spike/PhotoParts.cs`, new, reads only). Each photograph is registered from its own codes
four ways (plain homography; homography with the two-term radial lens; the lens with entry 324's bend laid over it where accepted; the
Phase 1 cylinder) and its bulls located through each. The final model's error is then split: the codes' share by refitting 30 times
with every kept corner moved at random by the fit's own corner residual (fixed seed; an upper bound, since the residual holds the model's
misfit too); the registration's share as the part of the 25 error vectors a quadratic in page position explains; and the bull-to-bull
scatter that leaves. Truth: the 600 dpi scan of the same sheet for the photographs of a scanned sheet, the printed positions for the
mounted Phase 0 frames. Material: the four Fold 7 pictures of the 2026-09-29 sitting (`corpus/sitting-2026-09-29`), the 2026-09-26
pictures of dominus-k and magnus-m against their scans, and the seven mounted frames (`scans/phase0`, sheet 3 hanging from a pin).

**1. Row 1's numbers were measured with no lens fit at all.** GroupLab decides a picture is a photograph by its focal length tag
(`ImageMetadata.IsCamera`). The corpus copies were stripped of every tag before they were kept, so each was read as a flat scan, by a
plain homography. `compare-photos` reproduces the checklist's 0.0155, 0.0176, 0.0216 and 0.0186 in exactly that way; with the lens fit
(`--model radial`) the same four read 0.0124, 0.0117, 0.0077 and 0.0143 in, and the dominus-k picture 0.0070 to 0.0057 in. The phone keeps
its capture's tags (`PhoneAnalysis.Prepare`), and so does the upload page's intake (it keeps the camera facts GroupLab measures with), so
the phone and the learning loop already get the lens fit; a photograph that lost its tags on the way (sent through a messaging app, a
screenshot, the corpus) does not. The phone's own PNG copies of the sitting (`camera-0929/pictures`) say the same: homography 0.0084 to
0.0516 in, lens fit 0.0070 to 0.0087 in.

**2. With the lens fit, the worst bulls carry a smooth error the codes cannot see.**

| photograph | worst, lens fit (+ bend) | codes' share, rms / at worst | smooth field, rms / at worst | scatter, rms / at worst | corner residual |
|---|---|---|---|---|---|
| picture-1 | 0.0124 | 0.0025 / 0.0055 | 0.0035 / 0.0104 | 0.0026 / 0.0026 | 0.0029 |
| picture-2 | 0.0117 | 0.0027 / 0.0048 | 0.0030 / 0.0054 | 0.0037 / 0.0066 | 0.0029 |
| picture-3 | 0.0077 (bend) | 0.0029 / 0.0029 | 0.0022 / 0.0038 | 0.0029 / 0.0039 | 0.0004 |
| picture-4 | 0.0143 (bend) | 0.0010 / 0.0011 | 0.0035 / 0.0090 | 0.0034 / 0.0055 | 0.0005 |
| dominus-k 09-26 | 0.0057 | 0.0006 / 0.0008 | 0.0018 / 0.0021 | 0.0019 / 0.0048 | 0.0022 |
| magnus-m 09-26 | 0.0102 | 0.0011 / 0.0013 | 0.0025 / 0.0051 | 0.0026 / 0.0052 | 0.0023 |

Inches. The codes fit to 0.0022 to 0.0029 in, yet the smooth field at the worst bull is up to 0.0104 in, so the field is not in the
registration at the codes: it lies between them. 75 to 90 percent of the squared error lies along the image's radius at each bull, but
its sign changes from bull to bull and from picture to picture (mean radial error by distance from the image centre: +0.5, +1.0, -3.1,
-4.7 thousandths at the outer band of the four Fold 7 pictures). A lens profile the two terms miss would be the same function of that
distance in every picture from one camera; it is not. **What is left is the paper's own relief between the codes, seen through a wide
lens**: the sitting used the Fold 7's 13 mm equivalent ultrawide, where a ray to the outer bulls is about 40 degrees off the lens axis, so
0.1 mm of paper height moves a bull about 0.003 in.

**3. Where the worst bulls sit.** Always on the outside of the 5 by 5 grid (bulls 25, 5, 1, 5, 6, and 5 on magnus-m), 1.06 in from the
nearest code, the outer bulls' distance. Not consistently on the far side: by local scale they rank 24, 15, 17, 1 and 4 of 25 (1 is the
farthest), and the rank correlation of error with distance from the camera is +0.01 to +0.54.

**4. The mounted frames (row 2).** With the lens and the bend the median bull is 0.0018 to 0.0046 in on all seven, but the worst is 0.051
to 0.117 in, every time an outer bull on the lower edge (the sighter S1 on five, bulls 21 and 5 on two), with a radial error of +12 to
+32 thousandths outward in the outer band: the sheet hangs from one pin and curls toward the camera at the bottom, beyond where the
bend, fitted across the codes and faded past them, reaches. The cylinder reads them at 0.018 to 0.058 in. Code noise is 0.001 to 0.023
in rms there, small beside the curl except on main2 and ultrawide3, which read only 26 and 32 of 34 codes.

**Which part carries the worst bulls:** the shape of the paper (the bent-sheet part), between the codes on a sheet held flat and beyond
them on a hanging one. The lens fit decides the result when it does not run (untagged photographs); when it runs it is not the limit.
The codes' corners are second, at most about 0.003 in; the bull locator's scatter about 0.003 in.

### Section 2: photographs at an angle

**Kept: a photograph with no camera tags is read through the lens.** `SheetMeasurer.UntaggedPhotograph`: an image with no focal length,
stating no scanning resolution (none, or below 150 dpi, since 72 and 96 are what photo tools write), whose codes fit a flat page worse than
0.003 in rms (`UntaggedPhotographRmsDmm`), is registered as a photograph, with the lens fit and the bend after it. **Chosen on** the
Phase 0 set: every scan in `scans/phase0` and the three 2026-09-26 load sheets fit a flat page to 0.0014 to 0.0027 in and gain nothing
from the lens (ratio 1.0); the tagged Phase 0 photographs fit it to 0.0030 to 0.087 in. **Reported on** the 2026-09 photographs, which
did not choose it (`grouplab compare-photos`, worst bull before and after, inches):

| photograph | before | after |
|---|---|---|
| sitting picture-1 to picture-4 | 0.0155, 0.0176, 0.0216, 0.0186 | 0.0124, 0.0117, 0.0077, 0.0143 |
| dominus-k and magnus-m, 2026-09-26 | 0.0070, 0.0133 | 0.0057, 0.0102 |
| the phone's PNG copies, 065419, 181253, 181549, 182144 | 0.0084, 0.0238, 0.0351, 0.0516 | 0.0070, 0.0087, 0.0078, 0.0080 |

No photograph got worse at its worst bull. The fifth PNG (223707) fits a flat page to 0.0020 in and is left as it was. Two holes the PNGs
missed are found, and their false marks go from two to one (181253 gains one). Holes are 0.0107 to 0.0180 in at the median after, against 0.0125 to 0.0225 before.
`scoreboard --corpus`: only the three untagged 2026-09-26 pictures change; found holes the same (72 of 73), hole median better on
magnus-c (0.0258 to 0.0228) and magnus-m (0.0207 to 0.0175), and one false mark moved from magnus-c to dominus-k, 1.3 in below the codes
where that sheet was taped down. Phone captures and the upload page's submissions keep their camera tags, so they read as before.

**Tried, not kept: anchoring on the printed bulls.** Section 1 names the paper's relief between the codes. The bulls are printed at
known positions exactly where the codes cannot see it, so a correction pulling each located bull to its printed position (Gaussian
weighted, zero at the codes) was tried and judged on holes against the scan, never on bulls, which would be circular
(`photo-parts --holes`). It changed the hole median by -0.0006 to +0.0009 in and the worst by -0.0040 to +0.0018 in: nothing. A
photograph's holes sit 0.0107 to 0.0180 in from the scan's at the median, against 0.0018 to 0.0034 in for its bulls, so the holes' own
centres (question 51, the shadow) dominate, not the paper's shape. Measured from their own bull, the offset a shooter reads, the holes
are no better (median 0.0122 to 0.0194 in).

### Section 3: the bent sheet (row 2)

Measured on the same seven frames (table in section 1, item 4; `photo-parts --mounted`). Most of the worst error is the sighter S1, an
inch below the last row of codes, where the bend fades to nothing on purpose (entry 324: carried on past the last row it raised a false
mark) and where no current GroupLab sheet puts a bull: every current definition keeps every bull inside its codes. Inside the codes the
lens and bend leave 0.0051 to 0.0122 in on four frames and 0.063 to 0.116 in on the three where the curl left 2 to 8 codes unread. The
cylinder, question 44's bounded step kept, reads those three at 0.034 to 0.055 in, but three of the four others worse (0.0060, 0.0224 and
0.0578 in against 0.0051, 0.0122 and 0.0093), and five of the six flat photographs far worse (0.044 to 0.097 in). A rule choosing it, say by unread codes, would be set on seven frames of one sheet, and
flat photographs miss a code or two as well (two of the PNGs read 31 and 32 of 34), so the model was not changed. Too few frames to
split; nothing here is offered as a result beyond the measurement.

### Section 4: holes ringed as two shots, and false marks

The 7 rings on clean single holes (row 4) are gone, by section 2's change and nothing else: all seven were on the right-hand column of the
9 and 15 degree pictures (bulls 15, 20 and 25, 1.4 to 3.1 holes' area), where the plain homography misplaced the printed artwork and left
its edges in the difference. With the lens fit the four pictures ring nothing. Every rendered double is still found: the Core suite,
`RoundsFiredOnARenderedSheetTests` among it, passed. The false marks over the nine corpus photographs with a hole truth are 6 before and 6
after (5 on the crinkled .22 LR diamond sheet, and the one that moved); no change to the detector was made for them.

### Section 5: the rules

Settings chosen on the Phase 0 frames and scans, reported on the 2026-09 photographs (5.1). Scans cannot change: an image stating 150 dpi
or more never enters the new test, and every scan in the corpus states one; the Core suite passed (2960) and its one failure is the
local inbox line (5.2). The one photograph a change made worse in any way (dominus-k's moved false mark) is recorded above; the worst
case improved everywhere (5.3). **The learning loop**: submissions keep their camera tags, so the real scoreboard reads them as before;
the synthetic board, whose pictures carry no tags, stayed within every margin of its baseline (`scoreboard --synthetic --baseline`,
exit 0), so the next nightly check should read the same (5.4). The bench gate passed, no stage slower (5.5).

### Section 6 and 7

The gates are not met with the material here: request 94 asks for scans of the three 4 October sheets (a current sheet mounted at the
range, photographed and scanned, for row 2) and three main-lens photographs of the dominus-k sheet at 0, 20 and 35 degrees (row 1). Rows
1 to 4 of `docs/PROOF-CHECKLIST.md` carry the new numbers; RESEARCH.md records the article decision (written, as part of entry 401's
article on how far off square a photograph can be).

**Update 2026-10-12 (entry 411 section 1):** the three 4 October sheets were thrown away before request 94 reached them, so the scans to
compare their range photographs against will never exist; the photographs stay. Row 2's question, how a current sheet mounted at the
range compares with its own scan, now needs a current GroupLab sheet shot mounted on a current build and scanned flat afterwards at
600 dpi as PNG. It is not a request of its own: it goes in the next range kit (with requests 57, 58 and 86), whose checklist says to
keep the sheet. Request 94 stays open for the three main-lens photographs of the 26 September sheet (row 1) only.

