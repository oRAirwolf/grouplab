# Phase 1 results, entries 401 to 425

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 400: the phone's reading of a camera photograph, about a tenth sooner, part taken back (2026-10-10)

The record is `docs/PERFORMANCE.md`, "Entry 400". In short:

1. **Measured** with `PhoneAnalysis.Run` headless on this desktop (Release, quiet), on the 600 dpi sample and the nine Fold 7 pictures,
   after entry 399's change sent the untagged pictures through the lens fit. The slowest stage was reading the codes (190 to 420 ms; 7.6
   s on the picture whose codes read only enlarged); entry 399 had added registration's bend fits, 95 to 230 ms on the bent pictures.
2. **Made faster** without changing any decision's order: the code cut-outs read at once and taken in order; the enlarged cut-outs the
   same; the markers found once where finding the code places looked twice; the bend's leave-one-out fits at once, summed in order. An
   ordinary Fold 7 picture 893 to 613 ms on average (31 percent), the hard one 8016 to 4562 ms, the nine together 38 percent less. Not
   done: reading the whole picture at its several sizes at once, which would multiply the memory a phone holds (entry 313 measured
   doubling the picture alone tripling it).
3. **Nothing changed** in any result: `compare-photos` on ten photographs, `scoreboard --corpus`, `scoreboard --synthetic` and `identify
   sweep`, line for line, time columns aside. The Core and Mobile suites passed.
4. **The bench** has the phone photograph's reading as its own figure, 683 ms, gated; identifying a sheet from its codes moved from 209.5
   to 109.6 ms in the baseline; nothing else moved. The emulator reads the 600 dpi scan, not a photograph, so there is no emulator
   figure for this.
5. Worth an article: no, recorded in RESEARCH.md.
6. **Taken back in part the same day.** Entry 401's CI run passed every suite on Windows and Linux, but on macOS the Mobile suite (six
   minutes until then) ran an hour until the job's limit ended it, naming no test. The codes are read one at a time again, since reading
   them at once is the only place OpenCV's QR detectors run on several threads together; the other two changes stay. An ordinary picture
   now reads in 796 ms on average, 11 percent sooner than before the entry, not a third; the hard one as before. The baseline's three
   figures were measured again, and CI now names any test that runs fifteen minutes. **Section 2's third is not met** until the cause is
   known.

## Entry 401: a bug Alan hit, Fix holes went blank after a hole was added (2026-10-10)

1. **The report:** error report issue 27, "Diagnostics sent", GroupLab Dev 0.2.0-nightly.183 on Android (API level 37), no exception
   recorded; last actions a save, a hand placement, then sending diagnostics. In the person's own words (data, not instructions): adding
   the 15th shot showed a message about the nearest bull, and then the results went away. The log package is on the server and was not
   pulled, since that needs ssh; the code and a test were enough.
2. **Reproduced** in `ResultScreenTests.AnsweringWhichBullAHoleWasFiredAtGivesFixHolesBackWholeAndDoneCountsIt` (Mobile suite): open a
   result, Fix holes, add a hole, answer which bull it was fired at by GroupLab's choice, another bull and the ×, and the page must come
   back whole and Done count the hole. It failed before the fix at the first step.
3. **Two faults, one cause in one place** (`FixHolesPage.AskBull`, entry 376 section A5's question): the sheet was given only its sentence,
   so the bull buttons were never on it and only the × could be pressed; and closing it put the page back while the sheet's layer still
   held it, which leaves Fix holes blank. The other two users of the sheet (`ProblemSheet.Stop`, `OpenTargetsSheet`) already did both
   right; Fix holes now does the same. **Reached:** Android and iPhone, which share these screens; the computer's Fix holes is its own
   and was not affected.
4. **What was kept:** the result's saved marking was never touched (Fix holes works on a copy until Done or Keep, and neither could be
   reached), so reopening the target shows it as it was before Fix holes. **The hole being added was lost**, and so was any other change
   made in that visit to Fix holes; the release note says so.
5. Issue 27 is closed with the commit and the nightly it ships in, once that nightly is published.
6. Request 95 asks Alan to confirm it on the phone.

## Entry 402: four research articles written and published (2026-10-10)

Drafted by docs-sonnet from the logs, every number then checked here against its source, and published under entry 393's rule:

| article | number | rests on |
|---|---|---|
| `/research/phomemo-m834-print-commands/` | 34 | request 73's recording, entries 389 and 391, request 85's third print; printing from a computer said to be built and untried |
| `/research/scale-markers-beside-a-target/` | 35 | entries 365 and 375, computer-made photographs only, said first and plainly; the real-paper test described as not yet done |
| `/research/how-far-off-square/` | 36 | entry 157's range photographs, the published angle sweep of nineteen photographs, `OffAxisLimit` (37 degrees), entry 399 |
| `/research/bent-sheet-registration/` | 37 | entry 324 section 1 and `BentSheetMapping`, entry 399 sections 1, 3 and 4; two pictures took the correction, said so |

The drafting found four places where the brief and the sources differed, and the sources were followed: the refusal is at 37 degrees
(`OffAxisLimit.Degrees`), not 40; a sheet has been measured against a scan up to 66 degrees (the published angle sweep), not only to 35;
request 94 asks for main-lens photographs, not for a narrower lens as such; entry 391's five feed readings average 99.28, the code uses
99.30 and the article says about 99.3. Edits after drafting: internal request and entry numbers taken out of the bodies, a table
introduced as four pictures that has six, and four sentences of article 37 reworded so it does not say "nothing here is about holes"
beside a table of hole results. No figures: each says why in `no_figure`. Claims: four article rules and eight index sentences in
`docs/claims-backing.json`; 5109 claims, all backed.

## Entry 403: the download page on an Android tablet asking for the desktop site (2026-10-10)

1. **Two signals read before the touch test**, in `DOWNLOAD_JS` and the Desktop or Mobile switch in `website/build.py`: the browser's own
   platform (`navigator.userAgentData.platform` is "Android"), and an ARM processor named by `navigator.platform` or `navigator.oscpu`
   while the user agent says x86_64. Entry 349's touch-only test stays as the last fallback, and any guess resting on these says "Looks
   like this device". **Documented:** Firefox for Android has reported its processor as "Linux armv81" in the user agent,
   `navigator.platform` and `navigator.oscpu` since version 123, and Firefox on Linux as "Linux x86_64" (Mozilla's release notes for
   Firefox 123 for Android, and bug 1861847), so a Linux computer on ARM in Firefox says x86_64 throughout and stays Linux; the
   user-agent hint's platform values are Android, Chrome OS, Linux and the rest (MDN, Sec-CH-UA-Platform). **Documented against us:**
   Chrome's desktop mode on large tablets reports the platform as "Linux" (Chrome's own announcement), so the first signal will not
   catch Chrome in that mode. **Not documented, to be observed:** what Firefox for Android reports in `navigator.platform` and `oscpu`
   when it asks for the desktop site; whether its user agent's x86_64 is matched by them is what request 96's readout will show.
2. **`?why` on `/download/`** shows under the device buttons the user agent, platform, oscpu, the user-agent hint's platform,
   maxTouchPoints, the pointer and hover results and which rule chose the guess, as plain text on the screen only; nothing is sent or
   kept. Checked in Node against the built script with a stubbed page.
3. **Tests:** `DEVICE_CASES` gains a field for what else the browser says of itself and eight cases: Firefox on a tablet with a pen
   (ARM platform, fine pointer), Chrome on a tablet with a pen, Chrome where only its own platform says Android, a tablet with a
   keyboard's trackpad, a Raspberry Pi, a Linux laptop with a touch screen and a trackpad, a Chromebook and an ARM Chromebook. All
   sixteen cases pass in the site build, entry 349's eight among them.
4. Request 96 asks Alan to open `https://grouplab.org/download/?why` in Firefox on the tablet.

## Entry 404: the server's intake worker brought up to the repository's, prepared for Alan (2026-10-10)

1. **The difference.** The server's worker is taken to be the one of e7ef5040 (entries 215 to 218, 2026-09-25), since entry 357's change
   was never installed; the repository's differs from it by one log line (`grouplab-intake-worker.py`, the "sent <state>" a picture
   under "Send everything I open" carries). The installed file was not read, so the dry run is what says which files differ. What
   changes and the undo are in RESTORE.md's waiting entry, written before anything runs.
2. **Not run by Code.** This session's connection to the server is refused by its permission check (it refused a copy for request 93
   the same day), and entry 404 section 3 says not to look for a way around it. Request 97 gives Alan two pastes: the first fetches the
   eight files at commit c95280d0 into a new folder, refuses them unless every SHA-256 matches (checked here against what GitHub serves),
   backs up the six installed paths with `grouplab-change-backup.py` and runs `install.py --intake --dry-run`; the second installs, with
   `nginx -t` before any reload and both sites checked after, only if `install.py` says the include changed. panel.md carries both.
3. Entries 399 to 403 were committed before this; the push and the site check are below in the reports.

## Entry 405: the consistency audit of 2026-10-10, ten findings fixed together (2026-10-10)

1. **Phone pictures.** The README, the tour index and /features/ now say the phone and tablet pictures are taken on the Android
   emulator after each nightly; the site reads the nightly from `docs/figures/screens/phone/made-from.json` (`phone_nightly()` in
   `website/build.py`), so the number cannot go stale again.
2. **The tour's pending phone pictures.** Fingerprint now shows `fold-fingerprint` with its caption, parts and steps. Analysis-open,
   compare and ballistics stay pending; their words and the eyebrow no longer promise a device sitting.
3. **M834.** README and the Features page: from nightly 181 pages to the M834 are drawn 0.7 percent longer, and the third check print
   was true both ways to 0.05 percent. Windows printing to it stays built, not yet tried (request 83).
4. **M220.** README, the scale markers feature and its claims backing: two labels printed on a real M220, codes 59.96 to 59.98 mm apart
   (60.0 designed), about 0.4 to 0.7 mm left of center, left as it is (question 94).
5. **Label targets.** Narrowed to "no 4x6 label printer tested yet".
6. **Narrow windows.** PLATFORM-SUPPORT.md, README and /download/ describe what entry 243 built instead of "being worked on".
7. **Testing guide.** The 4 October range day added (137 of 145 holes, no false mark); both guide PDFs rebuilt, the user guide's
   because its phone pictures had changed since.
8. **README mosaic.** Remade from the current phone pictures; `android-emulator.yml` now remakes it when the phone pictures it commits
   have made it stale (`readme-images.py --check`), so it cannot fall behind again.
9. **PERFORMANCE.md and the entry 400 heading** say "about a tenth sooner, part taken back".
10. Nothing else. `claims.py --check` passes (every claim backed), `readme.py --check` and the site build pass; the one consistency
    warning left is the weekly sheet screenshots, which their own workflow refreshes. The wording was done by the docs-sonnet worker
    and checked here.

## Entry 406: requests 93 and 96 answered, and the server paste's silent stop (2026-10-11)

1. **Request 93.** The learning worker on the server is the repository's (SHA-256 `cdf083f7...`), and the token reaches both
   repositories. RESTORE.md's waiting entry is now a dated change with its backup folder
   (`2026-10-11T004851Z-learning-token-check`) and its undo; the request is in the archive, answered. Reading the next learning
   nightly's `summary.md` for the token line waits for 13:45 UTC on 2026-10-11.
2. **Request 96.** Firefox and Chrome on Alan's tablet both send an x86_64 Linux user agent with the platform `Linux armv81`, and both
   picked Android with entry 403's reason: the fix holds on real hardware. No test holds the guess to sample user agents (entry 403
   added none), so there was nothing to add the two to.
3. **The silent stop.** Request 93's paste read the root-only backup folder without sudo; the only such paste was that one, corrected
   in the archive to the form that worked. Request 97's pastes already print on every failure. `ForAlanTests.NoPasteReadsABackupFolderWithoutSudo`
   now fails on any command in `for-alan.md`, its archive or RESTORE.md that reads or copies from a backup folder without sudo, and
   CLAUDE.md carries the rule that no step in a chained paste fails without printing something.

## Entry 407: Alan's picks from the open screen decisions canvas, built (2026-10-11)

Alan picked 1A, 2B, 3A, 4A, 5B, 6A and 7A; the seven DESIGN NEEDED items in `for-alan.md` are marked decided and question 88 is
answered (7A) and archived. One commit a section, then this one.

1. **Which bull each shot belongs to (4A).** `BullLines` (Core) draws every line on both platforms. **What unsure means in the code:**
   "not sure" is the matcher's own `ShotAssignmentDetail.Ambiguous`, set when the hole was given a bull other than its nearest or its
   two nearest bulls are within `ReviewQueue.ContestedMarginInches` (0.15 in) of each other, the same flag the review queue raises as
   contested; no new score. "Long" is more than twice the sheet's median line and more than a quarter of the spacing from its bull to that
   bull's nearest neighbour, so a whole group an inch low lights nothing. "Crosses into a neighbour" is the line passing within 0.45 of a
   neighbour's spacing of its center: a bull's rings fill at most half the distance to the next bull, and the marking does not carry the
   ring geometry. A hole whose bull a person chose, or whose contested item was dismissed, is never drawn unsure. Distances are on the
   target where a scale is set. **Taking a hole away** (`MarkingSession.TakeFromBull`): the person's choice is dropped, the hole goes back
   to its nearest bull and the sheet is matched again under its rule, as any unchosen shot is; where that puts it straight back on the bull
   it was taken from, it goes to its next nearest bull instead and is held there, since taking it away has to take it away. One undo step.
   **Colors:** the canvas's #2f7fe0 and #c46a00 have the same lightness (relative luminance 0.21 and 0.22), so the chosen blue is #1f4fa8
   (0.09): 6.8 to 1 on the paper, orange 3.4 to 1, and 2.0 to 1 between them; the paper is the same in both themes (`ThemeTests`). The
   phone's lines were orange-red and the lit bull amber, which an orange unsure line could not stand out from, so plain lines are grey and
   the lit or chosen bull blue there too. **The lasso is kept**: putting many shots on a bull at once is one gesture with it and one click
   a hole without it. The "Which bull was this hole fired at?" question on Add a hole is unchanged.
2. **The Groups section (1A).** `SheetReading` says it; the rules underneath (`AssignmentRule`, `SubgroupMap`) are unchanged, so saved
   sessions read the same. **Finding unused bulls by themselves:** with nothing said, the bulls fired at are the whole-sheet reading's
   (entry 229), and where the holes fit more than one set equally the "Which bulls did you fire at?" question is still asked (entry 376 A2);
   that reading already existed and is what the figures used, so the explicit step is kept as the correction, "Tap the bulls you fired at",
   which makes the set the person's own, exactly the rule "Bulls you fired at" made. The offset fix never loses its input. "Only the bulls I
   aimed at" folded into answer one with that tap, as the code allows. "Some bulls took two or more" takes two on each bull tapped; three or
   more is not offered by a tap (the old typed box did not offer it either). Phone: the result row is "How you shot this sheet".
3. **The Shots list (2B).** `ShotList` (Core) gives the rows and the open row's parts. The Fired list is new data: `MarkedShot.Fired`, kept
   in the marking file, two shots never holding one place, and used by "In the order fired" when no chronograph string says the order.
   "Delete, not a hole" is `SetNotAShot`, so the mark, its provenance and Undo are kept and the mark stays at the end of the list. The
   tick boxes stay (they put several shots on a bull at once). A shot on the phone's Shots page opens from its row; the phone's result
   picture is for looking only, so a shot there does not open a row.
4. **Selected shot and Show in folder (3A).** The reason list is gone; Counted off stores "left out by the shooter", which no screen
   prints as a reason, and a reason already stored is shown read only and still printed by reports and exports. The Selected shot area's
   "Not a shot" became "Delete, not a hole" and its full delete "Remove the mark", so two buttons are never both called Delete. "Open the
   session's folder" is in the ··· menu.
5. **Barrels (5B).** Schema version 3 (`SessionStore.Upgrades`, `docs/SESSION-SCHEMA.md`). **Found on the way:** the Equipment screen's
   fields from entry 131 section 7 were never in the database (a barrel's length, twist, direction, install date and notes, a rifle's
   cartridge, scope, stock and notes, a load's bullet, brass, powder, charge, primer, lengths and notes), so they were dropped at every
   save since entry 112 moved the records into it; version 3 keeps them. Before any upgrade GroupLab copies the database beside itself
   (`grouplab.pre-v2.db`, SQLite's `VACUUM INTO`), recorded in RESTORE.md with how to put it back. `BarrelMigration` then runs once:
   **the migration on Alan's desktop database, run on a copy** (deleted after): rifles 1 to 2, barrels 1 to 1, barrels naming no rifle
   1 to 0; the one barrel names no rifle and no session names it, so it goes under "Barrels not yet on a rifle", which Alan moves by
   choosing its rifle; 21 sessions, none naming a barrel, all name the same rifle and barrel as before. On a test database of version 2
   (`AVersionTwoDatabaseIsCopiedThenEveryBarrelGoesUnderARifle`): rifles 2 to 3, barrels 2 to 3, unplaced 2 to 0, one placed by its
   sessions, one under the new rifle, one made from a rifle's own twist, every session unchanged. The rifle keeps the length and twist
   fields, copied from the barrel on now, because the ballistic solver reads them there. The entry 307 export carries the barrel on each
   rifle (an added field, so version 1 still, and older builds still read it); an older file's barrels are placed on import. The phone
   has no barrel screens. Kept though the entry's list for a rifle's page did not name them: rifle or pistol, manufacturer and scope
   unit, because the form needs the scope unit for the click and the other two are a person's data.
6. **Paper and backing (6A).** The fold is closed at first and not remembered open; "Remember these for my next sheet" is remembered.
   The phone could not record paper and backing before; it can now, in the same fold under the caliber and distance.
7. **Chronograph readings on the analysis (7A).** Import a file, Type them in, and a drop area naming Garmin Xero, LabRadar and CSV (the
   formats `ChronographFiles` reads; BulletSeeker is Experimental and not named). Match to shots proposes the pairing with
   `ChronographMarks`, Ballistics' own, and keeps it; a reading's pairing is changed in Ballistics as before. A workbook of several
   strings still goes to Ballistics to choose one. Phone: Import a file and Type them in on the velocity card, in the same place as the
   desktop's block, both opening its readings page.
8. **Pictures.** The walk draws four new views for the Features page (`bull-lines`, `groups`, `shots-open`, `readings-here`) and the
   `equipment` picture now shows a rifle's page with its barrels.

## Entry 408: request 95 confirmed on the phone (2026-10-11)

Alan checked entry 401's Fix holes fix on nightly 184 or later: adding a hole asks which bull, and Fix holes comes back with the
picture. Request 95 is closed and archived; the fix is confirmed on real hardware.

## Entry 409: the cut-off warning that stayed after a thermal printer (Quack, the Discord) (2026-10-11)

1. **The fix.** `PrintPanel.ShowDots` now takes away its own "would be cut off" warning when the page is drawn again with nothing cut
   off, a sheet printer chosen or another sheet; a status written by anything else since is left alone (`cutWarning`). The phone's
   Targets screen never shows this warning, so nothing changed there.
2. **Why it showed after calibrating.** It did not come from the calibration: the "Print on" list is a fixed list built once with the
   panel, and nothing but a person's choice (and the tests' `ChoosePrintOn`) moves it, so no index can shift when a printer is
   calibrated. The Targets screen's panel lives for the whole session, so a thermal printer chosen earlier left the warning in place
   until something else wrote the status line, which is why "Open to print" cleared it.
3. **Test:** `Entry409CutOffWarningTests`: the warning shows on a 4 inch head, goes when the sheet printer is chosen again, and another
   sheet keeps Print on where it was.
4. Release note `fixed`, naming Quack; Quack is in the README's Thanks, with Alan's words in `samples/PROVENANCE.md`.

## Entry 410 section 1: the six false marks, investigated, nothing kept (2026-10-11)

1. **Before, per picture** (`grouplab scoreboard --corpus`, the nine GroupLab-sheet pictures with a hole truth): diamond-22lr 25 of 25,
   **5 false**; load-sheet-22lr 24 of 25, 0; dominus-k 25 of 25, **1 false**; magnus-c-6.5 22 of 23, 0; magnus-m 25 of 25, 0; the four
   2026-09-29 pictures 25 of 25 each, 0. In all 221 of 223 and 6 false, as PROOF-CHECKLIST row 3 says.
2. **What the five on diamond-22lr are** (the show-your-work patches, the photograph's pixels only): four are along the edges of the
   diamonds' own printing, the white square's upper left rim and a crescent beside the printed center dot, and one is a black diamond's
   tip against the paper. **Not misregistration:** re-aligning each mark's 0.6 in neighbourhood by phase correlation found shifts under
   one pixel. **Shading:** where the crinkled sheet is creased, the square and the dot throw a shadow along one side, and the shadow is the
   residual. The dominus-k mark (1.3 in below the codes where that sheet was taped) was not looked at further.
3. **Tried: a faint mark hugging a printed edge is shading.** On diamond-22lr the four shading marks have a mean residual of 60 to 68
   (of 255) with 0.90 to 1.00 of their pixels within 0.04 in of a printed edge; its 25 real holes are 92 to 159, and none hugs an edge
   past 0.62. Refusing marks under 0.32 of the range and over 0.85 on an edge took diamond-22lr from 5 false marks to 1 and changed
   nothing on the other eight pictures, the 2026-10-04 range pictures or the 28 rendered conditions (`scoreboard --synthetic`, before and
   after from one build). **It lost a real hole:** scan 4 of 2026-09-20 (Alan's, 600 dpi, `RangeScanCountsTests`), bull 11, a .22 hole
   through the ring line, which on a scan shows the light lid, so it reads faint (78) and hugs the ring's edge (0.88). Developed on
   diamond-22lr; it held on the 2026-09-29 pictures and the rendered set and failed on the scans. Tightening the two numbers until that
   hole survives would be fitting the material, so the change is not kept and the detector is as it was.
4. **What would separate them,** for a later attempt with more material: a shadow's residual has one sign and fades away from the edge
   that casts it, toward the light; a hole's is a step all round its rim and, on a scan, the lid's own grey. A test on the direction of
   the fade would need creased sheets photographed under known light, which the corpus has one of.
5. PROOF-CHECKLIST rows 1, 3 and 12 are unchanged: the gate is not met. No Release-note: nothing a shooter sees changed.

## Entry 410 sections 2 to 6: the overnight queue (2026-10-11)

1. **Section 1** is its own section above: the six false marks, investigated, nothing kept.
2. **Section 2, proof row 16.** The report's zero verdict was already the screen's on generated sessions (entry 170's test); a test now
   holds it on Alan's dominus-k session too (`RealSessionProofTests.AlansOwnSessionsReportGivesTheScreensZeroVerdict`), run here with
   the test-data scans and on CI. PROOF-CHECKLIST's paragraph that still said row 16 needed it says it is met.
3. **Section 3, finding the holes.** Measured warm, the closing and the phase correlations were small; the marks' own moments and split
   were the cost. Each mark is now summed over the runs of its hull row by row, the same pixels in the same order, and the split's squares
   are products: 346 holes on fifteen pictures unchanged to six decimals, the generated sheet's holes 261 to 168 ms, the bench gate
   passed (PERFORMANCE.md). **Reading the codes at once is back** (entry 400), after nightly 185 had published, behind
   `SheetIdentificationTests.ReadingTheCodesAtOnceFinishesOnEveryPlatform`; CI passed on all three platforms with it, macOS's Mobile suite
   162 of 162.
4. **Section 4.** Nightly 185 (from 3ecf22a3) carries entries 407 to 409; its notes read to a shooter and thank Quack; grouplab.org
   showed it from 6109d181. Every view entry 407 changed has its own Features picture; the last, the paper and backing fold
   (`paper-fold`), was added, with its tour words, and the guide PDFs were rebuilt. The emulator's sweep now photographs Fix holes, Next
   unsure hole and How you shot this sheet; run 38136714382 passed, and chores-haiku found the frames drawn right in light and dark, with
   nothing cut off or blank. On the sample, Fix holes counts 3 unsure holes and names bull 11's line as long for the sheet. **Noticed, not
   from this entry:** the sweep's largest-text pass sets Android's font scale to 2.0 and GroupLab's text does not grow, so the phone
   application may not follow the system text size; put to planning as question 96.
5. **Section 5:** the learning nightly of 2026-10-11 (summary committed 13:55 UTC) says "The learning token reaches the error reports: yes;
   GroupLab's pull requests: yes." Entry 406 section 1.3 is done; nothing for for-alan.md.

## Entry 411: the 4 October sheets gone, and the phone follows Android's text size (2026-10-12)

1. **Section 1, request 94.** The three 4 October sheets were thrown away, so their scans will never exist; recorded in request 94 and
   under entry 399's results (`docs/notes/archive/results-376-400.md`) and proof checklist row 2. Request 94 stays open for the three
   main-lens photographs of the 26 September sheet only. The mounted-sheet question now needs a current sheet shot mounted on a current
   build and scanned flat afterwards at 600 dpi as PNG; it is not a request of its own and goes in the next range kit (section 4).
   **So it does not happen again:** CLAUDE.md now says a range kit's checklist and any request that has a sheet shot and then scanned
   names the sheets to keep, flat, and until when; in `for-alan.md` that is a `**Keep:**` line, which
   `ForAlanTests.ARequestThatScansAShotSheetSaysToKeepIt` requires. Requests 20, 54, 58 and 94 have one, and the hole size plan's text
   says to bring the sheets home and keep them (its printed PDF was not remade; the next range kit does that).
2. **Section 2, the text size.** Question 96 answered and archived. Every text size the styles set now passes through one place,
   `TextSize` in the shared theme: the size itself on the desktop and the iPhone, and on Android the phone's own sp conversion
   (`App.SystemTextSize`, `TypedValue.ApplyDimension`), so from Android 14 the larger sizes grow less than the smaller ones as in Android's
   own apps. The Fluent theme's field and tooltip sizes and the few sizes set in code on phone screens (the ballistics percentage, the
   printer check's figure, the pairing pause line, the load comparison chart, and the shared velocity, load group and fingerprint views)
   follow it too; picture labels drawn on the target do not. A change of the setting while GroupLab is open restyles it
   (`ConfigChanges.FontScale`, `OnConfigurationChanged`). **At Android's largest size (2.0)** the headless sweeps found the bottom bar's
   five names too wide for a fifth of a small phone, two figure tiles' units past the tile, a checked-print pill cut off, and a Settings
   button cut off. Fixed: a figure's unit goes under its number when it does not fit, the pill's words wrap, and any button's words wrap
   when they do not fit (one style rule; words that fit stay on one line). The bottom bar needs a real decision, so for-alan.md has a
   **DESIGN NEEDED** line and meanwhile a name that does not fit is drawn smaller until it does. **Checks:** `QualitySweepTests` runs the
   sweep's screens at a small phone's width at Android's largest size with no faults and every frame's text larger; `MobileSweepTests`'
   large text runs now use Android's largest size rather than a raised base size the phone's styles overrode; `Entry411Tests` holds the
   body, a heading, a field and a button to growing and coming back. On the emulator each frame's quality record carries its middle
   text size and `phone-sweep-check.py --larger` fails the sweep when a screen's text is no larger in the largest-text pass than in the
   plain one; it first runs with the next nightly. **The iPhone** does not follow Dynamic Type (nothing reads it, and Avalonia 12 has no
   text scale); question 97, unchanged. Release note `changed`; ANDROID.md says how it works; the README, guides and site said nothing
   about text size, and no Features picture changes at the usual size.
3. **Section 4** is recorded for the next range kit (one current sheet shot mounted and scanned at home, kept), in STATE.md.
4. **Tests:** Mobile 165 passed, App 534 passed, Core 2994 passed (the one failure the local inbox line, entry 412 waiting).
5. **A fault found by CI and fixed.** The first version told the styles of a size change through a static event, which held every
   application it was given. The headless tests start an application for every test, so the desktop suite on Linux held 500 of them
   and the runner was shut down three times about 12 to 15 minutes in (runs 38206202325 and 38208601401, Windows and macOS passed).
   `TextSize` now keeps them in a weak table, and `Entry411Tests.AnApplicationIsNotKeptAliveByTheTextSize` holds it. The growth test
   also failed when it ran first, because it opened a Shell without answering the first run's questions; it answers them now.

## Entry 412: no shot sheets left from past trips (2026-10-12)

1. **Request 94 closed** with Alan's words and moved to the archive; the 26 September sheet is gone as well as the three from 4 October.
   Entry 399's results now say both comparisons it wanted (a range photograph against a flat scan of the same sheet, and the main lens
   against the ultrawide) wait for new sheets shot at the next range trip and kept; proof checklist row 1 and material item 3 say the
   same.
2. **The open requests swept.** Changed: **74** now asks for one or two new photos of a blank, unshot target on the wood table, all four
   corners in view, no range trip; **86** says the 26 September aim point card was shot and is gone, and a new card comes with the next
   range kit (no printable copy of it is in the repository or `grouplab-local`, only scans of the shot one); **9** says where its scan
   is if it has left Downloads (the `test-data` release keeps the same picture). Not changed: 54, 58 and 75 use unshot store-bought
   targets, and 20, 50, 33, 56, 57, 76 and 77 print or shoot something new. Nothing else in a plan, a draft article or a research note
   counted on more old material arriving: RESEARCH.md's corner finder line waits on request 74, which now asks for new photos.
3. Open requests: 22.

## Entry 413: the phone's bottom bar shows its icons alone at large text sizes (2026-10-12)

Alan chose B from the canvas of five frames (entry 411's DESIGN NEEDED line, answered in for-alan.md).

1. **Built.** `NavBar` (mobile/GroupLab.Mobile/NavBar.cs) replaces the bar's plain grid. In its own measure pass it lays each of the five
   names, at the size followed and semibold (as the current place's name is drawn, so moving between places never flips the bar), against
   its fifth of the bar's real width less the tab's padding; when any one is wider, all five names are left off, the icons go from 20 to
   26 px and the pill from 56 by 28 to 60 by 40 with a 20 px radius (a style on `nav-icons-only`). A name not shown still changes size
   with the setting, and the bar asks again when it does, so the change is live both ways. Each tab has its place's name as its
   accessible name and as a tip, opened by a long press; it stays a full fifth wide and 56 high. Entry 411's shrink-to-fit Viewbox is
   gone. The iPhone follows no text size, so it never reaches the point and is unchanged.
2. **Held.** `Entry413Tests`: at a 360 wide window all five names show; at Android's largest size (the sweep's `font_scale 2.0`) none show,
   each tab is still named for TalkBack and as a tip, at least 48 high and a fifth wide, the icons 26 and the pill 60 by 40, Targets still
   marked, and back at the plain size the names return; a 900 wide window keeps its names at the largest size. The emulator sweep's
   quality records carry the bar's state (`bar`: names or icons), and `phone-sweep-check.py --larger` fails when the largest-text pass
   on the sweep's Pixel 6 shows names or the plain pass shows icons alone.
3. **Public.** The user guide's phone section describes the bar and the text size; ANDROID.md the rule. The Features picture is taken by
   the emulator's screens run on the Fold's cover screen at text size 2.0, on Targets (`fold-targets-largest-text`), from the first
   emulator run after this reaches main; SCREENSHOTS.md names both files as waiting and the layouts test lets exactly those wait.

## Entry 414: the overnight queue (2026-10-12)

1. **Phase 9, three named optimizations** (docs/PERFORMANCE.md, one heading each). **1.1 A scan decoded once:** a PNG's grey decode is
   libpng's conversion, a fixed sum truncated where the file names no gamma; made from the colour decode that way it is identical at
   every pixel of every PNG tried, so an 8 bit PNG without a gamma, colour space or profile chunk is decoded once (`LoadGreyAndMax`, the
   window's open). Loading the 600 dpi scan 645 to 337 ms, the scan stage by stage 1348 to 1034, opening it in the window 1020 to 823.
   Every scoreboard (four corpus folders and the synthetic board), the corpus counts and the identify sweep identical but for time
   columns; the bench, window and start-up gates pass. **1.2 The first trajectory:** nothing in the solver is built on first use; the
   100 ms is tiered compilation (113, 110, 70, then 4.3 ms; 14 every time with tiering off). Eight dope tables flown on a lowest-priority
   thread once the window is up make the first table 6.4 ms. **1.3 The CEP table:** 0.4 ms since entry 170; nothing to change.
   **Found while measuring, and taken back:** reading a sheet's codes at once (entry 410 section 3.3) crashed the Phase 0 gate record's
   identify sweep on Linux in native code in 3 of 12 runs, never in the 28 reading them one at a time; reverted (aae48796), the sweep
   the same line for line. PERFORMANCE.md says why.
2. **A force-stop in the middle of a reading.** It does not reproduce. `scripts/android-force-stop.sh` now runs after every nightly on CI's x86_64 emulator: a control round (stopped while idle, started again: done in 9 s), then GroupLab Dev stopped 2, 5, 9, 14 and 20 seconds into a
   reading of the 300 dpi sample, its reading running each time (11 to 28 reading lines in the log), and every start after it ran its
   scenario to the end in 7 or 8 seconds (run 38260940771, nightly 189). Tried first on this computer's emulator under arm64
   translation, where no scenario starts at all, so that setup cannot answer it. What run 37735635656 most likely saw is the walk of
   that time, not GroupLab: the first emulator walks pressed Next before the photo arrived and replaced the target showing, and
   everything after one stuck step failed; its logs were not kept. No fix, so no release note; STATE.md's line is corrected.
   **Also found:** the emulator's commit of the phone pictures collided with the screenshots job's over the README mosaic; both jobs
   now commit their pictures, rebase, and only then remake the mosaic. Tonight's eight changed phone pictures are committed by hand
   from the run's artifact, and entry 413's Features entry has its own picture, `fold-targets-largest-text`.
3. **Consistency after entries 411 to 413** (docs-sonnet): the README, the site's sources, the three guides, the glossary and the phone
   documents describe no fixed text and no always-named tabs; nothing to fix. No open `consistency` issue. Its findings otherwise:
   the guide PDF was older than the guide (rebuilt), /releases/ did not name nightly 188 (published; the first dispatch met an HTTP 500
   from GitHub), and the screenshots job's last push met "fatal error in commit_refs" from GitHub, so its pictures wait for its next run.
4. **The proof checklist sorted.** Row 11 proven on a rendered sheet (`DoublesSheetTests`); row 4 half met on a rendered sheet too (the
   two-shot hole ringed at 1.39 holes' area, a clean torn hole at 1.48); row 29 two of three on an emulator (nightly 188's signed APK
   and GroupLab Dev installed together, each started). Every unproven row now names exactly what it needs, the range part marked.
5. **The next range kit** in `C:\Dev\grouplab-local\range-next\`: CHECKLIST.pdf, the load sheet (3), the 5x5 sheet for doubles, the
   red bulls, and the aim point cards by distance (`cards.py` beside the article: 25, 50 and 100 yards, rings for a 2 MOA dot, a
   score sheet each). About an hour; the doubles sheet and the aim points if there is time; a Keep paragraph. One line in for-alan.md.

## Entry 415: four things that need nobody (2026-10-13)

1. **Question 97 answered: the iPhone follows its text size (Dynamic Type).** The iOS head points `TextSize` at iOS's own factor for
   body text (`UIFontMetrics.DefaultMetrics.GetScaledValue`, read each time) and restyles on `UIContentSizeCategory`'s change notice,
   so a change in Settings is followed with GroupLab open. **The cap, and why:** iOS grows every size by one factor (about 0.82 at the
   smallest, 1.35 at xxxLarge, 3.1 at AX5); Android's largest (`font_scale 2.0`, Android 14's table) grows small text 2 times and large
   text far less (30 to 38, 44 to 50.4). Each size is held at Android's largest for that size (`TextSize.ByFactor`, `AndroidLargest`),
   so the iPhone's largest is exactly the size the headless sweeps already hold at 360 wide, narrower than any iPhone on iOS 26 (375).
   Found by the test: at xxxLarge iOS already passes Android's largest for the lead figure (29) and the two large percentages (30 and
   44), so those three are held from xxxLarge; everything up to a value's size (22) follows iOS exactly through xxxLarge. Above the cap
   the text stays at it. The bottom bar is the shared `NavBar`, so entry 413's rule holds on the iPhone (`Entry415Tests`, at an iPhone
   SE's 375 to the largest iPhone's 440). No screen needed a DESIGN NEEDED line. **Found:** `ios-app.yml`'s sweep already had a
   largest-text pass (xxxLarge), and it showed GroupLab's text unchanged because nothing followed it; it now runs at AX5 and is held to
   larger text and the icons alone by `phone-sweep-check.py --larger`, as the emulator's is, so the parity column says **on iOS**.

2. **Proof checklist row 3: the six false marks, each found.** The scoreboard over the nine corpus photographs read 221 of 223 holes and
   6 false marks, as the row said. Looked at mark by mark, against the render at the same place: **diamond-22lr, four marks at the
   middle of a diamond** (bulls in its first two columns, where the sheet was most crinkled): the white window's edge sat up to 0.035 in
   off the render on one side, softly, and the bands of residue along one or two edges closed into one mark; on two of them the window's
   one edge moved and the other did not, a stretch of about a tenth across the window. **diamond-22lr, one mark on a diamond's edge**,
   0.3 in from a real torn hole: a crease's soft shadow along the edge, wider than print can move. **dominus-k, one mark** below the
   frame, on the edge of the tape that held the corner. **The change, a mechanism and not a setting:** a mark that is not a clean round
   hole of a bullet's size is refused as "the sheet's own printing, moved or blurred by a crinkle" when one shift of the whole print, of
   no more than `MovedPrintInches` (0.04 in, the registration's own bent-sheet threshold, not chosen here), with no more blur than the
   opening already ignores, explains every pixel of its residue. **Taken back first:** letting each pixel move on its own removed all
   five diamond marks and lost two rendered holes on the zero grids (a crossing and the white centre at .224), so it was not kept; one
   shift for the whole mark is what a crinkle does. **Result:** 221 of 223 and 4 false marks (2 of the windows' marks gone; the
   stretched windows, the shadow and the tape stay). No hole lost anywhere: the corpus, the synthetic board (identical to main's but for
   "far 3 ft", 44 to 45 on main already, so the baseline file is behind), the rendered sheets and zero grids, and the Core suite. **The two
   missed:** magnus-c-6.5's torn hole refused as not compact (hull solidity 0.42 against 0.55; the torn-hole rescue, which has its
   calibre, stops at 0.45, `TornHole.LeastSolidity`, and lowering it for one hole is the tuning the entry rules out), and load-sheet-22lr's hole inside printed marker 31, where no hole is looked for; neither
   cause is clear enough to change for one hole. Nine photographs are too few to tune on, and this change is at that line: it removes
   two marks on one sheet. The learning loop has 9 of the 50 corrected submissions its held-out share needs, so there is no held-out
   share yet; its nightly re-score of those 9 runs from the next nightly's tag, so it starts from this code. Row 3 is not met.
3. **Phase 9: reading a photograph's codes, still on one thread** (PERFORMANCE.md has the tables). A cut-out now stops at its first code,
   which is the only one the square-on reading uses: 10 to 40 percent off reading the codes of a Fold 7 picture (184 to 383 ms became 167
   to 283) and of the camera stills (79 to 113 became 64 to 86), and identifying a 600 dpi scan in the bench 171 to 120 ms. Every
   scoreboard, the corpus counts and the identify sweep are identical but for their time columns, and the baseline gate passed. Keeping
   the detectors made once a thread gained nothing and was taken back. **Section 3.2, finding the holes, not done:** entry 416, a feature
   Alan chose, was taken first; its binary closing was already made managed code in entry 392.
4. **Worth an article?** Section 2 recorded in RESEARCH.md (not written); section 3 not written either: ordinary engineering a shooter
   sees as a slightly quicker reading.

## Entry 417: a tester's bug on macOS, marking a scanned 3-sheet set (2026-10-13)

Quack's three scans (`GL-CF9-T-C`, nine .223 shots a page, 600 dpi, a flatbed with a white lid), read on Windows through the command line
and the window's own path. **The causes are in the pictures and the pointer logic, not in macOS:** they happen on every platform, and the
detection and the snap are shared with the phones. Neither entry 414's single decode (the window shows a PNG made from the same decoded
pixels it measures) nor the printer's scale (it multiplies sizes and never moves a position in the image) moved anything.

1. **Detection found 4 of 9 holes on each page.** A hole seen through to the lid is as light as the paper (both read 254), so only its
   rim is residue; where the rim is faint the ring has gaps, no outline closes, and the hole was refused as "not compact" (solidity 0.43 to
   0.52). Now a refused mark whose pixels make a circle of a hole's size (Kasa's least-squares fit through them), whose arc goes round more
   than half of it and whose middle is clear is that hole, placed at the circle's centre, not at its darker side. Pages 1, 2 and 3: 4, 4
   and 4 found before; 7, 9 and 8 now, every mark on a hole. The ones still missed lie on or against a printed marker, where no hole is
   looked for, or were called residue. The limits (half the rays, a clear third of the radius) are a ring's shape in round numbers and were
   checked on these eleven rims, so they sit close to what they were checked on. **No hole lost and no mark added elsewhere:** the corpus
   (and magnus-c-6.5's torn hole, one of the two entry 415 called missed, is now found: 73 of 73 in that folder), the synthetic board
   identical to the last centre, the zero grids, the rendered sheets.
2. **The snap went to the rim's darker side.** The Impact tool's snap weighs dark pixels; on a light-middled hole that is the rim's
   shadowed side. On main's code three presses on one hole landed 15 to 36 px (up to 0.06 in, a quarter of a .224 hole) below its centre,
   wherever the press was; now all three land within 2 px of the same centre. A hole dark all through keeps its weighed centre. Left as
   they were: a hole whose rim lies partly on a printed marker (0.05 in off) and light holes on the black diamonds, which the dark-weight
   snap was never for; detection places both.
3. **Select stopped dragging.** A click on a bull with no hole marked on it, as each missed hole's bull was, chooses that bull (entry 407),
   and from then on every press on a hole gave it to that bull or took it away instead of starting a drag, until Done or Escape. Now a
   press on a hole starts a drag as it always has: let go where it was pressed and it is entry 407's tap; move it and the hole moves.
4. **Online:** no Avalonia report of a macOS drag or capture fault of this kind was found; the general capture rules are as GroupLab uses
   them.
5. **Tests:** `LightHoleTests` reads page 2, reduced to 300 dpi and committed (samples/PROVENANCE.md; 1.4 MB), with a printer chosen, with
   a calibre and without: every hole within 0.02 in and no mark on anything else (main's code found 4); a press a quarter of a hole off
   snaps within 0.02 in of a hole on the paper. `Entry417Tests`: with a bull chosen, a tap on a hole still gives it and a press and drag
   moves it.

## Entry 418: the upload page, from a tester's bug report (2026-10-13)

1. **Which reply it was** (request 99, answered 2026-10-16). The server's nginx logs are in Denver time (-0600 in October), so the
   first paste's UTC window found nothing; Alan's corrected read-only paste over 11 October 19:00 to 12 October 04:59 local found every
   post to the upload page. The failed one is at 20:53:26 -0600 (02:53:26 UTC): **status 499 with 0 bytes**, nginx's word for the other
   end closing the connection before any reply was sent. So it was not a 413 from nginx or Cloudflare, and not PHP's `post_max_size`:
   the receiver read the whole post and stored it as `312dab10`, stamped 02:53:26 UTC, the same second. **The first "1 of 2" was stored
   although the page said it failed**, and Quack's resend three minutes later (`06910ef5`) went to the trash (section 4). Since nginx
   sent nothing, the reply the page could not read was Cloudflare's own error page, sent when its side closed as the receiver finished;
   most likely its 524 timeout, which nginx's log cannot tell apart from Cloudflare's other pages. After it: 200 with 57 bytes
   (GroupLab's reply) at 20:56, 21:02, 21:04 and 21:08, which with the first make the five an address may send an hour
   (`RATE_PER_HOUR`), so the sixth at 21:09:50 got 429 with 175 bytes, the "come back in an hour" reply; then 200 at 22:00 and 22:01.
   Seven stored, as the submissions folder shows. The page built in section 2 already says this case plainly: a 524 is "took too long",
   a dropped connection is said as one, and both say it may have arrived and that sending again is safe because a photo already in is
   kept once. The logs reach back to 21 September; **a paste that reads them uses Denver time** (-0600, and -0700 from 1 November).
2. **Progress and plain errors.** The page sends with progress events: a plain bar under the button, the megabytes sent of the total,
   then "Received, checking the files", then the reference. Before a byte is sent it says what is over and what to do: a file over 30 MB,
   a set over 30 files or 400 MB, a PDF or another type. A reply that is not GroupLab's own says what happened with its status (413 is
   "too large for one send", 502, 504, 522 and 524 are "took too long", a dropped connection is said as one) and whether anything may
   have arrived: a set is never stored until its last part, and a single send can be repeated safely.
3. **A whole set in one go.** Up to 30 files and 400 MB, sent by the page in parts of up to 10 files and 90 MB, each well under
   Cloudflare's 100 MB. The first part passes the person check and is given a token; the later parts bring it, from the same address,
   within the hour, in order. The parts wait in `private/sets`, which the intake worker never reads, and the set moves into quarantine in
   one step when its last part arrives, as one submission with each file labelled by sheet and view where the sender gave them ("sheet 1
   of 3, front"). A set given up half way is deleted with nothing kept after two hours. **Counted as one submission**, and its bytes
   against a new hourly budget per address of 450 MB, five submissions of 90 MB, which is what one address could already send in an hour:
   so no address can send more in an hour than before. Every existing protection stays (Turnstile on every set's first part and every
   single send, the 5 an hour and 20 a day, the global 60 an hour, the disk cap, sniffing, clamd and quarantine, the consent text).
   **Duplicates:** each stored file's SHA-256 is kept for a day; a file sent again in the set or within a day is kept once, and a send
   whose every file is already in stores nothing and answers with the earlier reference. limits.json holds the two new limits and the
   site build holds the receiver to them. Receiver tests: 122, among them a set split across requests, a wrong token, another address, a
   file sent twice in a set, a set sent again, a set over the limit, a part naming no set and the hour's bytes.
4. **The .50 AE duplicates:** identical file for file (eight each); `312dab10` kept, `06910ef5` to the trash.

## Entry 419: Discord bug reports filed by themselves (2026-10-13)

1. **The worker** (`website/server/grouplab-discord-worker.py`, e959956a): a systemd timer every five minutes, 240 s at most a run and
   new threads started only in the first 180 s, reads the bug-reports forum through Discord's REST API and keeps one issue per post in
   `oRAirwolf/grouplab-crash-reports`, labelled `discord`: the post's title, the first message fenced as untrusted text, the poster's
   public handle, the time, the thread's link and its attachments listed. Later messages become comments; the state file, keyed by thread
   and last message, files nothing twice; the first run files every post already there, open and archived. The bot replies once in a
   thread it files ("Thank you, this is filed for the developer.", no mentions allowed) and posts nothing else; an archived thread cannot
   be answered and is not tried again. Its own messages are never filed. At signs are broken so nobody is notified, and words that read
   like an instruction are noted in the issue as data. The server and the forum are found from the token. **Attachments** are kept
   privately, one folder a message written whole with a SHA-256 manifest, and the nightly job pulls them to `C:\Dev\grouplab-discord`
   (`scripts/Get-DiscordAttachments.ps1`) and deletes the server's copy once every hash matches; no consent covers them, and each issue
   says a poster who wants them used can send them through the Send a target page. **Tokens:** both through LoadCredential, the GitHub one
   the error worker's own for the same repository (it already makes issues and labels there), the bot's typed in with
   `grouplab-set-discord-token`, which never echoes it. Tested in CI against a stand-in Discord and GitHub, 16 checks.
2. **For Alan:** request 98, written before the build and completed with the two pastes after it.
3. **Records:** RESTORE.md (the attachments, the state, `--discord`), CLAUDE.md (a `discord` issue is a bug report a person hit).

## Entry 420: a pooled set opens like a sheet, from a tester's question (2026-10-13)

1. **The pooled set opens in the analysis screen.** Pool the chosen still says which sheets are here and missing, and now also opens the
   pooled set on the analysis screen in a tab of its own. It is one marking (`SetPool.Composite`): each sheet read, once, laid beside the
   last on one page, its bulls where they print and each shot at its own offset from its own bull in inches, as its sheet measured it with
   its own scale; so the plot, every figure and the Shots list are the analysis screen's own, and its figures equal the pool's to the last
   digit (`SetPoolTests`). A bull is named sheet.bull, so the Shots list says which sheet each shot came from ("2.5" is bull 5 on sheet 2).
   **Shades:** each sheet's shots in their own colour, the colours Compare gives its loads, with a row a sheet in the plot's key; tapping a
   row shows that sheet alone, and again shows all; the figures stay the whole set's. Nothing else on the screen changed, so no DESIGN
   NEEDED line. It is opened as a saved sheet with no picture is, and is not saved as a session of its own. **The phone:** the set's
   checklist's See the pooled group opens the same plot on the result screen, Android and iOS, with the checklist as its way back.
2. **Dropping several sheets.** On both systems a drop of several pictures opened only the first and said the others were not opened,
   which is what Quack saw on his Mac. Each now opens in a tab of its own, the next once the last one's detection is done (at most two
   minutes each). Once two or more of them are accepted and are sheets of one set, the analysis screen offers **Read these as one set**,
   which pools them as Pool the chosen does.
3. **The repeated records.** A second Accept on the same tab already updated its record; the same picture opened again, by Open, a drop or
   a reopened tab, started with no session, so Accept made another record of the same sheet each time, which is what the identical
   records were. Now a save with no session first looks for a record of the same picture (its SHA-256) and the same design, updates that
   one, keeps its date, and says so on the status line (`SessionStore.WithImage`, `Entry420Tests`). A record saved without its picture
   cannot be matched this way. The 4-shot record from entry 417's bug was left alone.
4. **Pictures:** the pooled view's own Features picture is listed as waiting in docs/figures/SCREENSHOTS.md, for the screenshot walk; the
   guide's pooling paragraph, the tour and the Features sentence say how.

## Entry 416: a session's own conditions for Velocity and the vertical, concept A (2026-10-13)

1. **The session carries its own conditions.** Temperature, altitude or station pressure, humidity and shot angle, each optional
   (`SessionConditions`), in the marking (`conditions`), so the record, a shared session, an export and the other device all carry them;
   the database's schema is unchanged, since `marking_json` already holds the marking (SESSION-SCHEMA.md says so). Field by field, as
   entry 329 set it (`VelocityBlocks.Resolve`): the session's own value, then the Ballistics conditions for its rifle and load, then the
   standard day; the "why" line and the summary say where each came from ("this sheet, and the rest from Ballistics, for ..."). Sight
   height and zero stay on the rifle. Nothing is filled from a location or a weather service. **CSV out does not carry them:** the shot
   CSV is a table whose only header is its row of column names, with no place for them, and reading CSVs back for pooling depends on it.
2. **The desktop, board "A on the desktop":** Conditions on the day under the block once it has readings, closed by default with one line
   in the person's own units; open, the four fields, Use station pressure instead of altitude, Fill from Ballistics (greyed, with its
   reason, where Ballistics has nothing for this rifle and load), Standard day, the rifle's line with Change them on its page, Remember for
   my next sheet, and "With these, velocity alone is X. On a standard day it would be Y." (shown whenever the conditions are not the
   standard day, even where the two read alike at a short distance). A value changes the block at once.
3. **The phone, board "A on the phone", Android and iOS:** the card's Conditions row with the summary opens a sheet from the bottom with
   the title and Cancel, the note, the four fields, the swap link, the rifle's line, Remember, and Fill from Ballistics and Use these, each
   a thumb high. Fill from Ballistics is greyed on the phone with its reason: the phone's Ballistics page keeps no conditions of its own.
   At Android's largest text size and 320 wide every field and button of the sheet is inside the window (`Entry416Tests`).
4. **Remember for my next sheet:** ticked, the values go to the next sheet saved on the same day, marked "from your last sheet" until
   changed; not ticked, or the next day, it starts blank. The day is the device's own calendar day when the next sheet is saved, so a session
   that runs past midnight starts blank after it; that is the simplest honest rule.
5. **Found while building it, and fixed:** saving a session a second time replaced its row, and the delete under the replace cascaded to
   its chronograph readings and their pairing with shots, so a sheet accepted again after its readings were added lost them. The row is now
   updated in place (`SessionStoreTests.SavingASessionAgainKeepsItsReadings`, which fails on the old code).
6. **Pictures and words:** the Features entries for the desktop's open part and the phone's sheet are listed as waiting in
   docs/figures/SCREENSHOTS.md (the walk, and the phone's next sitting); the guide's Velocity paragraph, PHONE-PARITY.md, the Features
   sentence and STATE say where conditions are entered; claims backed.

## Entry 421: consistency audit of 2026-10-13 (2026-10-14)

1. **The Mac, two testers.** The platform statement (and the README's copy, written from it), the README's table row, the download page's
   Mac card and the testing guide say the Apple silicon build has been run on two Macs by two testers, nightly 93 and nightly 190; the
   Intel build is still untried. Nothing in Quack's reports (entries 417 and 420) says Command shortcuts or pinch zoom work on a Mac, so
   that sentence stays. `MacBuildsTests` holds the new wording, as it held the old.
2. **Phase 9** in the README says In progress, names the optimizations made and measured (entries 392, 400, 414, 415) and says the gate
   is not written; the bullets now agree with the table.
3. **Features dated by their newest part.** `website/build.py changed()` dates each feature by the newest build whose notes say one of
   its phrases, or "next" while one is in no published build. The Features page says "Since nightly.113; its newest part is coming in
   the next build" and, once that build is out, "Since nightly.113, changed in nightly.191"; the home page's card says "Coming in the
   next build", "Changed in" or "New in", and orders by the newest part. The rule asked for: a feature whose sentence differs from the
   newest nightly's tag fails the site build unless one of its phrases is still to come, or it carries `"reworded"` naming that nightly
   for a change of words alone. `tests/python/site-feature-dates-tests.py` holds it, run by website.yml. The pooled set's phrase did not
   match its own note and is corrected; a phrase of the problem dialog's that no build's notes ever said is removed.
4. **The PDFs** of the user guide and the testing guide are made again from the guides as they stand after this entry.
5. **Report a problem** is described one way in the user guide, the testing guide and the support page: a window from Diagnostics in
   the settings that lists what the report holds, takes a description and a way to reach you, saves a zip where you choose, and sends
   it only where a report address is set (`ReportWindow`).
6. **Phone parity:** `target-tabs` says the phone keeps several targets open through Open targets and leaves out only the tab strip and
   the keyboard; `pool-set` says the checklist also opens the pooled plot.
7. **Thanks:** Quack's flatbed scans, the upload page's report and the question about a set of three are named beside the printer warning.
8. **Two README lines:** the phone's list is introduced as since entries 258 and 259; the snap goes to the hole's middle, fitting a
   circle through the rim where the hole is as light as the paper.
9. **The donor page is unchanged.** Quack's 2026-10-12 submissions name no sheet design in their file names or records; the one design
   named, in entry 420, is a 3x3 2 MOA grid, not the page's C bull sheet, so "seen from one range day only" is not shown wrong.

Wording for sections 1, 2 and 5 to 9 by docs-sonnet; section 3's code, its test and the checks by the main model.

## Entry 422: a click on the picture says what it did, and the side panel lands on it cleanly (Jylee) (2026-10-14)

1. **What each click did before** (the desktop's analysis screen; the default tool and Select alike): a plain click on a bull chose it to
   give holes to, rewrote "Which bull each shot belongs to" where it stood and moved nothing; Shift or Command on a bull added it to the
   load's bulls and showed Loads on this sheet where it stood; a click on a hole, or on the line from its bull, selected it and opened its
   row under Shots in place, the panel not moving; the Impact tool placed a hole, selected and opened the same way; Next unsure hole
   selected the hole, centered the picture on it and said why on the status line, the panel not moving. **No click scrolled the side
   panel at all**, so a row opened below the fold stayed out of sight, and the panel's own rebuild could shift what was showing by a few
   lines. **The phone:** a tap on the result's picture lights the bull and says so under it, moving nothing; a row of Bull by bull lights
   its bull and scrolled the picture into view with the platform's own rule.
2. **The section lands in view** (`Landing`, shared by both): its heading a small margin below the top and the row fully visible; where
   the section is taller than the panel the row wins; the last section goes as far as the panel can. Worked out once the layout has
   settled and checked once more after the work the click queued. The sums
   are taken from the scroller's own content, so the panel's margin inside it does not leave the row a few lines short. Held at three window heights for a hole (the last section),
   a bull and Shift on a bull (`Entry422Tests`), and on the phone at two heights.
3. **It flashes twice** in the amber tint, about a third of a second each and a second in all; a click that does not move the panel
   flashes nothing. With the system's animations off (Windows' Animation effects, macOS's Reduce motion, GNOME's animations, Android's
   Remove animations, the iPhone's Reduce Motion) it holds the tint for a second and a half.
4. **The status line says what the click did** (`ClickWords`): "Bull C3 chosen: a hole you click now goes to C3.", "Bull C3 added to
   the bulls for a load; 2 chosen, under Loads on this sheet.", "Shot C3.2 selected; its row is open under Shots.", "Hole added as ...".
   A notice the click raised itself (a hole not snapped, a hole given to a bull) is left in place; Next unsure hole keeps its own words.
5. The user guide's Shots paragraph, the tour's Shots list and the Features page's Shots list say it; the guide's PDFs are made again.
   For Jylee's line is in for-alan.md. A click in the Shots list itself does not flash, because it does not move the panel.

## Entry 423: several pictures dropped together open in ONE tab, pooled without being asked (Alan, Quack, Jylee) (2026-10-14)

0. **The bug Alan hit.** Found by reading the code on main and nightly 190 (the planning session says he was on nightly 192).
   **Nightly 190 and earlier:** a drop always opened only the first picture, in the tab dropped on (asking first only where there was
   unsaved work), and said the rest were not opened; so "only opens 1 image and overwrites the session" was always true there.
   **Nightlies 191 and 192:** entry 420's drop opened the first in the tab dropped on, then made a tab for each of the rest; but
   `AddTarget` ignored `ShowTarget` refusing, which it does while a question is open in the middle of the window (a scan whose codes do
   not read asks "Which target is this?"), so the tab was kept, never shown, and each later picture opened over the same tab. Fixed:
   a tab that cannot be shown goes again and the drop says so; and a drop or paste onto a tab holding a target (a picture, a marking, a
   saved session, a pooled set) never replaces it, it opens in a new tab beside it (`TabHoldsATarget`); only an empty tab is filled. A
   picture that then fails to open closes the empty tab again. **No session record was overwritten by any of these builds:** opening a
   picture has started a record of its own since entry 243 (nightly 113); before nightly 113 a dropped picture accepted over a saved one
   saved over its record, and nothing on this machine can bring such a record back (the pre-v2 copy of `grouplab.db` was made later, in
   entry 407). Said in for-alan.md. Tests: `ADropOntoASavedSheetLeavesItAndItsRecord` (one and three dropped; the tab, its record's
   marking and the record count unchanged) and `ANewTabThatCannotBeShownIsNotKept`.
1. **A set opens in one tab** (`MainWindow.SetTab.cs`, `SetTab.cs`). Two or more pictures dropped or pasted together have their codes read
   first, one after another off the window's thread (never two at once, entry 414), with "Reading the codes of picture 2 of 3".
   `DropSorting.Of` folds the same picture under two names into one and puts the sheets of each set together. A set's tab is named for
   it ("GroupLab 3x3 2 MOA, Set of 3 Letter Sheets, 3 sheets"); its sheets are opened and detected in turn inside the tab, the status
   saying "Reading sheet 2 of 3." before each stage's words; each is saved as its own session record; then the tab shows the pool exactly
   as Pool the chosen does (every shot about its own bull, a shade and key row per sheet, the figures, the Shots list). No question. The
   pool is worked out afresh from the sheets each time it is shown. `ThreeSheetsOfOneSetOpenOneTabPooledAsPoolTheChosen` holds the two
   routes to the same shots, CEP 50 (to 1e-9) and mean radius, on three rendered sheets of the 2 MOA set.
2. **Each sheet checked alone.** A **Check one sheet** menu on a set's analysis screen lists every sheet (its file, its shots, "not
   counted" where it is out) and opens one alone, its own picture, marking and record, so Fix holes, the bull lines and Accept act on it;
   **Back to the set** shows the pool with the change in it (saved by itself as it is left, or asked about under saving with a button).
   Tapping a key row still shows a sheet's shots alone. The layout choice, the menu against a row of sheet switches above the picture,
   is a DESIGN NEEDED line in for-alan.md. Test: `OneSheetAloneCanBeCheckedAndTheWayBackShowsThePool`. Found on the way: the notice
   "This looks like another copy of a sheet you have analyzed before" fired for the second sheet of any set, because the sheets of a set
   share a design; it now counts only earlier records of the same sheet of the set (`SessionStore.MarkingsUsing`).
3. **Everything else.** Two sets in one drop: a tab per set, a tab of its own for a picture alone, and one status line saying what went
   where (`TwoSetsMixedOpenATabEachAndAPictureAloneItsOwn`). A missing sheet is named ("2 is still missing") and a later drop onto the
   set's tab joins it (`AMissingSheetIsNamedAndADropOntoTheTabAddsIt`). The same sheet in two different pictures: neither counts and a
   centered question asks which does, "Count first-0.png" or "Count second-0.png"; an identical picture twice is one
   (`TheSameSheetInTwoPicturesIsAskedAboutAndAnIdenticalPictureIsOne`). Pictures with no codes: "Pool these 3 pictures as one group?",
   **Pool them in one tab** the default and **One tab each**; pooled, they are read without detection (it cannot read them) and the pool
   names each picture still without a scale (`PicturesWithNoCodesAskOnceAndFollowTheAnswer`, both answers; `SetPool.CompositeOfPictures`).
   One picture dropped on an empty tab behaves as before. Not covered by the entry and asked as question 98: several pictures of one
   single-page design (each opens its own tab meanwhile).
4. **Read these as one set is gone**, with what kept it (`DroppedSetSessions`, the dropped-together bookkeeping); its test went with it,
   and `DropAndPasteTests` now hold the new rule. The user guide's section 10 and phone paragraph, the Features page's Pool the sheets of a
   set (its own picture now, `set-tab`, the 2 MOA set's three sheets dropped together) and Open by dropping or pasting, the tour's set
   text and the phone parity row say it. The tour's screens show no drop of several, so no tour picture changed.
5. **The phone.** Its pickers chose one picture at a time on Android (the photo picker and From another app) and on iPhone and iPad
   (Photos and Files); only sharing could hand over several. All four now take several (20 at most). Pictures chosen or shared together are
   read and saved in turn as before; where every one is a sheet of one set, the set's checklist opens with the pooled group showing
   (`SetPage.OneSet`, `Entry423PhoneTests`); otherwise the last one's result is shown, as before. **Not done on the phone:** section 3's
   other cases (a drop of two sets, a sheet twice, pictures with no codes) keep the phone's old behaviour. The iPhone picker change is
   proved only by ios-app.yml's build.
6. Tests above; the Release-note thanks Quack and Jylee; their lines are in for-alan.md.

## Entry 424: the zero correction says why an axis gets no click, and a setting gives both clicks always (2026-10-14)

1. **"Leave it", and why** (`ZeroWords`, one class the desktop's zero panel, the session report and the phone's figures all read). An axis
   inside the scatter is named instead of dropped: "Dial 1 click left, at 0.1 mil a click. Elevation: leave it. 0.08 mil low is within
   what these 20 shots can tell apart (about 0.11 mil either way)." The angle is in the rifle's scope unit, the unit its click is in, and
   the size in the length unit chosen. When neither axis is distinguishable the old verdict stays and each axis is named the same way; an
   axis right on the aim says "it is on the aim, as near as these 20 shots can tell". Where an axis was left and more shots would settle
   it, the line points to Shots Needed to Zero. The phone's zero rows say the same words under each axis, and its note carries the line.
   **The why, rewritten** in this order: why the axis got no click (or got one anyway); "The group's center is known to within 0.225 in
   (0.06 mil) across and 0.411 in (0.11 mil) up and down, at 95 percent."; what rounding to whole clicks leaves; and the method last,
   "Worked out on each axis separately from 20 shots, because the group is taller than it is wide" (or "both axes together ... about as
   wide as it is tall"). The report's own "give or take" line separates the figures the same way. Question 91's rule holds: the report's
   sentence is the screen's, joined with the distance and the rounding, then what to leave and why.
2. **The setting**: Settings, **Zero correction**, "When an axis is inside the scatter": **Say leave it, and why** (default) or **Give
   the clicks anyway**, remembered in the settings file on both platforms (`AppSettingsStore.LoadInsideScatter`). With it: "Dial 1 click
   left and 1 click up, at 0.1 mil a click. The up click is within the scatter; more shots would tell whether it is needed."; an axis that
   rounds to no click says "no click" either way. Every axis now carries its rounded clicks (`ZeroAxis.Rounded`) whatever the verdict;
   `Clicks` is still only given for an axis worth dialling, so nothing that read it changes.
3. **The pooled windage figure.** Every shot's offset in the Shots list and in the zero comes from one calculation
   (`GroupAnalysis.CompositeOffsets`) on the same pooled marking, so over the shots the figures count the list's mean is the centre to the
   last digit (`ZeroWordsTests.APoolsCentreIsTheMeanOfItsShotsListOverTheCountedShots`, with a left-out shot in the pool). The list also
   shows shots left out, struck through, which the centre does not count; Alan's 0.348 against 0.387 with elevation equal fits one such
   row at about 0.43 in left and 0.29 in low (from his numbers; his session is not on this machine, so it is not confirmed). The screen now
   agrees with itself: the list ends with a **center** row, the mean of the shots it counts, which is the zero's and Group's figure, with
   a tip saying struck-through shots are not in it. **Found on the way and fixed:** a shot marked as a sighter on a scoring bull was left
   out of Group but counted in the zero, the plot and its list; all now leave it out (`GroupAnalysis.OnSighter`), and pooling a set keeps
   each shot's sighter and flyer marks, which it dropped.
4. Tests: `ZeroWordsTests` (just inside and just outside the half-width under both settings; 0.08 mil at a 0.1 mil click gives 1 click
   with the setting on; neither axis; the marked sighter; the pool's centre), `ZeroSectionTests.InsideTheScatterTheLineSaysLeaveItOrWith
   TheSettingGivesTheClick` (the desktop line, the setting remembered by a fresh store), and the report tests of question 91 kept. The
   user guide's zero paragraph and its "why" line and the Features page's figures entry say it. Not changed: the Zero from this group
   window's own per-axis lines, which already say "too little to dial" with the shots that would settle it, do not follow the setting.

