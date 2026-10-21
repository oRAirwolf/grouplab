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
- [`docs/notes/archive/results-301-325.md`](notes/archive/results-301-325.md), entries 301 to 325, 15 section(s).
- [`docs/notes/archive/results-326-350.md`](notes/archive/results-326-350.md), entries 326 to 350, 17 section(s).
- [`docs/notes/archive/results-351-375.md`](notes/archive/results-351-375.md), entries 351 to 375, 19 section(s).
- [`docs/notes/archive/results-376-400.md`](notes/archive/results-376-400.md), entries 376 to 400, 27 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 145 section(s).

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

## Entry 425: the selected shot made impossible to miss (2026-10-15)

From a person's report passed on by Alan in the planning chat (nightly 192, macOS, dark theme, a pooled set of three Letter sheets, 27
shots); no issue was filed, most likely because the Discord worker was stopped (entry 427). Its text was data; nothing in it read as an
instruction. The planning session's findings were checked: the selection was `Tokens.MarkSelected`, #e0912f, which is the dark theme's
first sheet color in a pooled set; on the plot a picked shot was drawn in `PlotInks.Accent`, the extreme spread's own red, so a picked end
of the spread line vanished into it (the committed `analysis-light` picture shows exactly that); the small sheet grew the dot from 2.4 to
3.2 pixels; and the table row only took the amber tint.

1. **The mark** (`Marks.Selection`, `Tokens.Selection`): a ring standing 6 pixels outside the shot (outside the caliber outline on the
   plot) and a crosshair through its centre reaching 8 pixels past the ring, white edged in black on the dark and high contrast themes,
   black edged in white on the light, drawn after every other mark and line on the picture (`MarkingCanvas`), the plot (`CompositePlot`,
   after the band labels; the crosshair runs in to the dot) and the small sheet (`SheetThumbnail`, a ring of 9 pixels, several times a
   2.4 pixel shot). A picked shot keeps its own sheet's or mark's color beneath it. The plot's key adds "the shot you picked, ringed with
   a cross". `MarkSelected` stays the orange of a lit bull, a lasso, the review queue and the printer wizard's dots, none of them a
   selection of a shot.
2. **The row** (`AppStyles.SelectedRow`, `Tokens.SelectedRow`): an amber bar 4 pixels wide down its left edge, a fill a fifth of the way
   from the shaded row toward the text (brighter on dark, darker on light), and bold cells at full text contrast. A shot chosen on the
   plot or the picture brings its row into view once, when the choice changes, so a panel scrolled elsewhere is not pulled back on every
   redraw.
3. **Both themes, both platforms**: the desktop in dark, light and high contrast. The phone has no selected shot: it draws no selection
   on its picture (its amber rings are marks to check, which stay orange per section 1.4) and its plot is the shared `CompositePlot`,
   so a selection there would take the same mark; nothing else on the phone changed.
4. **Tests** (`Entry425Tests`, 9): the selection's ink is 100 or more in RGB from every pooled sheet color, the bull, the extreme spread,
   CEP and the center's green, the aim's blue and the old orange, in all three themes, and its edge is the opposite of it; the selected row
   is brighter (dark) or darker (light) than the background, panel and shaded row by 1.4 to 1 or more with text at 7 to 1 (high contrast
   at its own ratio); selecting a shot on the small sheet changes 250 or more pixels in each theme; a picked shot marks its row alone,
   bold, and the key says what the ring is. App 571, Mobile 179, Core cross-cutting 37 passed.
5. **Finishing**: the screenshot walk now pictures the analysis with one end of the extreme spread picked, so the next screenshot run
   after the nightly replaces `analysis` and `analysis-open` in both themes (checked locally in `out/screens`). The tour's analysis page
   said "The widest pair is picked out in bold", which was not true (nothing picks it until its line is clicked); it now describes the
   picked row and the plot's ring, and the user guide's analysis section says the same. Release note kind `fixed`, claimed on the
   Features page under the analysis screen. No issue to close: the report will be filed when request 100 runs, and is then closed with
   this commit.

**Question 99** records that the dark theme's white is also a single sheet's plot ink and nearly the placed marks' #e6e8ea, so there the
ring and crosshair, not the color, tell the selection apart; built as the entry says meanwhile.

**After it, its CI (f65147e8 and e52cd430):** red on macOS and Windows, neither from the selection. Twice on macOS and once on Windows
(e8aa367a), a phone test changing the text size stopped inside Avalonia 12.1.2 with "UnsetValueType to IBrush" while it re-evaluated
every control for the new styles, and the tests after it ran at the largest size. It never happened alone or in four local runs of the
suite. e52cd430 took the new styles out and added them again on that failure; on its own CI run the removal stopped in the same way, so
the fault sits on one control and is not passing, and that change is taken back (its release note corrected in
`docs/release-note-corrections.json`). What the phone tests did leave: nine tests opened windows they never closed, four of them the
whole phone (`SharedScreensTests`, one after pressing every button on its result), and every later change of text size restyled them
too. They are closed now (`SharedScreensTests.CloseStarted`, `IosWideScreenTests.CloseOpened`, `Entry295Tests`). Whether that was the
cause is not proven: it cannot be reproduced here, so the next runs will say. And `SolverWarmUpTests` failed on Windows at 1033 ms
against its 600 ms bound, the warm-up thread starting late on a busy runner; it now holds what it meant, that the flights run on a
thread that is not the caller's (`SolverUse.WarmedOnThread`), not how long a thread took to start.

## Entry 426: request 98's paste named a commit that is not on GitHub (2026-10-15)

1. **Request 98** is answered (Alan ran it on 14 October with the planning session's pastes at 77b3db39, entry 427) and moved whole to
   `docs/notes/for-alan-archive.md`; its paste there now names 77b3db39, the commit Alan ran, instead of e959956a, which is on no branch.
   Open requests: 23.
2. **The test**: `ForAlanTests.EveryPasteFetchesFromACommitOnMain` reads every `raw.githubusercontent.com/oRAirwolf/grouplab/<commit>/`
   in `for-alan.md` and `for-alan-archive.md` and fails on a commit that is not on the history of what is checked out (`git merge-base
   --is-ancestor`), which on CI is main. CI's test job was a one-commit checkout, so it now fetches main's history first (commits and
   trees only, no tags, so `ReleaseNotesTests` and `VersionTests` see the checkout as before); a shallow checkout on CI fails the test
   rather than checking nothing. Before the change e959956a was the only stranded commit; request 97's c95280d0 is on main.
3. No release note: nothing here ships.

**Just before this entry, not from it (e8aa367a):** build and test was red on 77b3db39 (all three systems) and 800e1d93 (Windows). Entry 424's
report sentence joins the distance and rounding to the first sentence of the screen's line and keeps "what to leave and why" after it, as
the entry meant; `RealSessionProofTests.AlansOwnSessionsReportGivesTheScreensZeroVerdict` still held question 91's older form and now
holds this one (passes on the 2026-09-26 6 ARC scans). The Windows failures were `PrinterConnectTests`: each blocking printer connect
ran on the shared pool, so on a busy runner a connect that answers at once never started inside its 200 ms; each now has its own thread
(`PrinterConnect.FirstThatConnects`), and one that finished as the limit fired counts as connected.

## Entry 427: the Discord worker installed, and its crash on an attachment fixed (2026-10-15)

0. **Recorded**: request 98 is answered and archived (entry 426); the install of 2026-10-15 04:16 UTC and its backup
   `2026-10-15T041559Z-discord-worker` (40,264 bytes, the four installed paths absent before) are in `docs/RESTORE.md` with the undo.
1. **The cause, measured rather than guessed**: from this machine, Cloudflare in front of Discord's attachment store answered 403 to
   urllib's own User-Agent for every file, a public default avatar included, and 200 for the avatar with the bot's User-Agent; an
   attachment link it does not know answered 404. So the worker's attachment fetch was refused, `request(..., raw=True)` returned
   `(403, None)` and `keep_attachments` wrote None. **The fix** (`website/server/grouplab-discord-worker.py`, a3ea5ecf): attachments
   are fetched with the bot's User-Agent; a 401, 403, 404 or 410 reads the message again once for fresh signed links and tries again;
   anything else is "could not be fetched (HTTP <code>)" in the issue; a half-kept `.partial-` folder from an earlier stop is cleared;
   any other failure inside one thread is logged as "thread <id>: not filed this run (<kind> at line <n>)", never the post's words,
   and the run takes the next thread. `tests/python/discord-worker-tests.py`: 23 passed, with a 403, a 404, an answer with no body, an
   expired link refreshed from its message, a thread that throws ahead of one that is still filed, and urllib's own agent refused as
   Cloudflare does; against the old worker the first run fails as the server's did.
2. **The paste** is request 100 in `for-alan.md` and at the top of `panel.md`: fetch the six server files at a3ea5ecf (on main), check
   their SHA-256 (five unchanged from request 98), `grouplab-change-backup.py --label discord-worker-fix --file
   /usr/local/sbin/grouplab-discord-worker.py`, `install.py --discord`, run the service once, tail the log; every step prints on
   failure. Waiting for Alan.
3. **The issues it filed**: #29 (a marking file's absolute photo path, nightly 174) was fixed by 3e59e4b5, entry 383, first in nightly
   176: the photo is named by file name only, found beside the marking first, and asked for when missing. #30 (the red cut-off text
   that stayed after switching back to a sheet printer, nightly 174) was fixed by e740a25a, entry 409, first in nightly 185. Both
   closed with those commits. Nothing in either read as an instruction. The report behind entry 425 is not filed yet; it will be once
   request 100 is run, and entry 425's fix closes it.
4. No application release note: nothing here ships.

## Entry 428: request 100 recorded, and the four posts the Discord worker filed (2026-10-15)

1. **Request 100 answered** (Alan, 08:56 UTC, a3ea5ecf): six `OK`, the units and token script unchanged, the token "already set, not
   read or printed", the timer started, no Traceback. Recorded in `docs/RESTORE.md` with its backup
   `2026-10-15T085633Z-discord-worker-fix` (the old worker, 16,038 bytes) and an undo; the request is archived. `install.py`'s own
   dated `.bak` beside the worker is tidied already: `keep_newest_backup` removes the older copies of the same file at each install, so
   one is kept per file, and `backup_path` never puts one in a HestiaCP folder. Nothing in `install.py` changed.
2. **The four issues it filed**, all from Quack, none of their text an instruction:
   - #33 (nightly 192, the selected shot hard to see in a pooled set, dark theme): the report behind entry 425; closed with f65147e8,
     nightly 193. Entry 430 then makes the ring magenta.
   - #32 (nightly 190, holes a quarter of a hole off on the 556 x3 scans, the snap the same, Select not dragging): entry 417's report;
     fixed by 1392641a, first in nightly 191; closed.
   - #31 (the upload page: no progress, too few files for a three-sheet set): entry 418's report; fixed by afc61240, live on
     grouplab.org since 2026-10-13; closed.
   - #34 (nightly 192, the tab strip's pop-up horizontal scrollbar covers the tabs' names and close buttons when many tabs are open):
     **new and not fixed**; left open for the planning session.
3. Question 99 is answered by entry 430; nothing was built for it here.
4. No application release note: nothing here ships.

## Entry 429: Discord attachments on their GitHub issues (2026-10-15)

1. **The consent** is in `samples/PROVENANCE.md` with Alan's words and the date: attachments in the Discord's bug-reports forum may go
   on their GitHub issues in `oRAirwolf/grouplab-crash-reports` without asking the poster; grouplab.org, public test data and research
   articles are not covered and stay as they were. Nothing already in PROVENANCE.md reads against it, so no question.
2. **Not done.** The worker change (each kept file committed under `discord/<thread>/<message>/` through the contents API and shown on
   its issue, a refusal as a line in the issue, a once-only backfill of kept folders, the issue wording, tests) was refused by Claude
   Code's own permission check as handling posters' personal data, so it was not written; nothing on the server or GitHub changed.
   Request 101 asks Alan for his word in the session. Found while preparing it: the token the worker uses (`grouplab-error-reports`,
   entry 194) was specified as Issues: Read and write only, so committing files will very likely also need Contents: Read and write
   added to that token; the worker would say so in its log rather than assume. The pull to this computer has never run, so the six
   issues' kept attachments are still on the server, and a backfill has to run there.
3. No application release note: nothing ships.

**Section 2, done later the same day** on Alan's word in the session ("Go ahead with entry 429 section 2", request 101 answered), with
his note that the `grouplab-error-reports` token now has Contents: Read and write on the crash-reports repository.

1. **The worker** (`put_on_issue`, `attachment_line` in `website/server/grouplab-discord-worker.py`): each kept attachment is also
   committed through the contents API to `discord/<thread>/<message>/<id>-<name>` on `main` and shown on its issue or comment, a picture
   inline and any other file as a link. A file already there from a run that stopped (422) is read back and counts as on the issue. A
   refusal, a server error or no answer is a line saying so ("kept privately for diagnosis; it could not be put on this issue (GitHub
   answered 403)"), never an exception, so it stops no other file and no other thread. The private server copy, its manifest and the pull
   to `C:\Dev\grouplab-discord` are unchanged.
2. **The wording**: the issue now says the attachments are on it with a private copy kept, and that grouplab.org, public test data or a
   research article still needs the poster's consent (`ATTACHMENTS_LINE`); the worker's docstring and `Get-DiscordAttachments.ps1` say
   the same.
3. **The backfill** ran once from this computer, not the server: the pull of 15 October had already copied all six kept folders here and
   removed them from the server. A one-off script imported the worker's own `put_on_issue` and `attachment_line`, checked each file
   against its manifest's SHA-256, committed it, and replaced its "kept privately" line in place: #30 (one comment), #31 (the post and one
   comment), #32, #33 and #34, six pictures. All six issues #29 to #34 had their "never published" line replaced. Their text before is in
   `C:\Dev\grouplab-local\discord-issues-29-34-before-entry-429-2026-10-15.json` (the weekly restore test last failed, so a dated copy
   came first); today's 09:39 nightly backup also holds it. #34's rendered body shows the picture as an image tag on the issue. The
   backfill used Alan's own GitHub login on this computer, so the server's token has not yet been shown to write contents.
4. **Tests** (`tests/python/discord-worker-tests.py`, 30 passed, run in CI): the picture is committed whole in its thread's and
   message's folder and shown; a 403 and a 502 from GitHub are lines in the issue with the file still kept privately; an already committed
   file is shown; a text file is a link; and a thread after the failures is still filed.
5. **The server** is request 102, one paste in request 100's form from this commit on `main`: fetch and check the six files, back up the
   worker, install, run once, then prove the token can write contents by committing `discord/check/entry-429.txt` (OK or STOP with
   GitHub's answer). `docs/RESTORE.md`'s rows for the crash-reports files and the Discord copies now name `discord/`.
   **Run by Alan on 2026-10-16 at 07:45 UTC**: six files `OK`, backed up, installed, `OK: the token can put attachments on issues`
   (the check file is commit 26e83e9f in `grouplab-crash-reports`), and the worker's log clean; recorded in `docs/RESTORE.md`.

## Entry 430: the selected shot in magenta (question 99 answered) (2026-10-15)

1. **`Tokens.Selection`** is #ff3df5 edged in black in the dark themes (high contrast included) and #b0009f edged in white in the
   light; ring, crosshair, widths, gap, arm and the small sheet's ring are unchanged.
2. **The phone** draws its plot with the same `CompositePlot` and tokens, on Android and the iPhone alike, so it takes the magenta with no
   change of its own; it has no selected shot to show yet (entry 425), so nothing there looks different today.
3. **The test** (`Entry425Tests`, 12 passed) now also holds the selection apart from the plot's own ink (`PlotInks.Ink`) and
   `MarkPlaced` in every theme, still from every sheet, bull and overlay color, and its edge is the opposite of it (at least 6 to 1;
   7.2 dark, 6.3 light). Too close, as section 3 foresaw: dark #ff3df5 is 84 from bull mark #b24bd6; light #b0009f is 68 from the
   fourth pooled sheet's plum #9c3f8f, 88 from #d62a5b and 93 from #b24bd6. Kept as Alan chose; question 100 has the numbers and
   three options, and the test holds the plum pair at 65 and the bull marks at 80 until it is answered.
4. The selected row's amber bar is unchanged.
5. **Question 99** is answered (b) and archived. The tour's key line and the user guide now say magenta; the Features entry for the
   analysis claims the new note. `analysis-dark.png` and `analysis-light.png` already show a picked shot (entry 425's walk) and are
   reshot by `screenshots.yml`, which this push starts.
6. Release note, kind `changed`, in the commit.

## Entry 431: the tab strip's scrollbar in a row of its own (issue #34) (2026-10-15)

1. **`TabScrollBar`** (`src/GroupLab.App/TabScrollBar.cs`): an 8 px bar 4 px under the tabs, drawn from the theme's own colours (the
   track in `Sunk`, the thumb in `Faint`, `Dim` while the pointer is on the strip or the bar or the thumb is held). It is there only
   while the tabs overflow (`IsVisible` follows the strip's extent and viewport), so a window whose tabs fit looks as it did. Dragging the
   thumb scrolls the strip; pressing the track beside it moves a page. The strip's own `ScrollViewer` keeps scrolling by wheel and
   touchpad with its Fluent bar hidden, which is the bar that was drawn over the lower half of the tabs. The tabs are unchanged.
2. **A tab chosen or opened is scrolled into view whole**, close button and all, once the row has its new width; only when the chosen
   tab changes, so scrolling by hand to look along the row is not undone by the next refresh.
3. **The test** `TargetTabsTests.TheTabStripsScrollbarHasARowOfItsOwnAndCoversNoTab` (15 tabs in a 900 px window): no tab and no close
   button intersects the bar's bounds, before and after the pointer moves over the strip; the strip's height does not change; with one
   tab the row takes no space; the last tab's close button lies inside the strip's viewport; pressing the track scrolls back. The tab
   tests (11), the theme, sweep and bench tests (78 in all) and the Features, shipping and guide checks pass.
4. **The phone** has no tab strip, and none of its screens has a horizontal pop-up bar over something it can be tapped on.
5. **Screenshots**: the tour's and Features' marking pictures show one tab, so the strip looks as before and nothing is reshot; the
   tour's words on the tabs are still true. The user guide's paragraph on several targets now says a thin bar under the tabs scrolls
   them when they do not fit; its PDF follows with the nightly's guide run. `target-tabs` in `website/features.json` claims the note.
6. Issue #34 is closed with the commit and nightly 195.

## Entry 432: the iOS workflow's red steps (2026-10-16)

1. **"The iPad and landscape" was a check wrong about the app.** It first failed on run 37416825125 (e0c3277f, 2026-10-06), not
   2026-10-11; it last passed on 7dfc3691. Every tour stopped after the Settings screen and sat "running" until its 400 s limit: the
   app logged `capture.ask calibre=False` as the sample was handed over, and nothing answered. Entry 376 item B3 (5df65754, between those
   two commits) made GroupLab ask for the caliber of every new target and empty it after each result, on Alan's word; the self-test still
   relied on the caliber it saves before the start, which the app now never reads. The full self-test had the same fault (paste, chosen
   picture, cancel while reading, open in, shared, the parity tour and the data round trip all FAILED in `ios-results.json`), unseen
   because the Compare step that judges them never ran after the iPad step failed. Nothing in the app was wrong on an iPad or in landscape.
2. **The fix is to the check**: while the self-test or the tour runs, the question is answered as a person answers it, the sample's
   caliber typed into the box and Continue pressed, each time it shows (`AnswerCalibreWhile` in `ios/GroupLab.iOS/SelfTest.cs`). A new
   check, "caliber asked", passes only where the question was asked at least once, so B3 is now held on iOS too. Nothing was skipped,
   deleted or loosened. The iOS project builds only on the Mac runners, so this is proven by the run in section 4.
   **Run 38873299212 (25c20598) then found a real fault**: "The iPad and landscape" and Real taps passed, and the Compare step, which had
   not run since 2026-10-05, failed on the second picture onwards (chosen, cancel while reading, open in, shared, and the parity tour and
   data round trip after them). The question sits on the Capture start; after the first picture's result the Capture screen shows the
   result, so a second picture shared or opened into GroupLab raised the question where nobody could see it and was never read, on iPhone,
   iPad and Android alike, since entry 376 B3. Fixed in the app: the start comes back with the question, the result staying open in its
   tab (`AskFirst` in `CapturePage`); `Entry432Tests` fails on the old page and passes on the new. Release-note kind `fixed`.
   **Run 38878915397 (2776a941)** then passed every picture check, the iPad and landscape and Real taps; Compare failed only on "data
   round trip: no data file was written", although the log shows the export written (426,038 bytes) and the share sheet open. Since
   entry 377 (1fb8f104, 2026-10-05) the phone writes the export in the cache's `shared` folder, and the check looked only in the cache
   itself: another check wrong about the app, hidden behind the iPad step. It now looks where the app writes (`DataSelfTest`).
4. **Green:** ios-app.yml run 38883488382 on a2a0345a passed every step, the self-test, the iPad and landscape, Compare and Real taps.
   Real taps' Next on Targets passed there too; it fails on some runs only, and the new log lines will say where when it next does.
3. **"Real taps" fails at two different taps**, each on some runs only (18 red runs read):
   - **Step 23, closing the photo picker** (12 runs). The simulator has no camera, so Take a picture opens the picker in its place. On a
     slow runner the app asked for it 2.8 s after the camera's place showed (run 38513312105: `camera.none` 21.385, `ios.pick` 24.161),
     past the close step's 2 s quiet time, and the close step read the log's "picker opened" in the moment before the picker was on
     screen and failed at once; every later step failed after it. Fixed in the step (`Close` in `mobile/GroupLab.Mobile/Dev/Scenario.cs`):
     a picker asked for and not yet seen is waited for and closed within the step's own 15 s; one still unseen then fails as before.
     `PhoneTouchScenarioTests.CloseWaitsForAPickerStillOnItsWay` brings the picker after the quiet time: it fails on the old step and
     passes on the new one. Not a retry: the step now waits for the thing it closes.
   - **Steps 5 to 12, Next on the keyboard on Targets** (5 runs). The first press on Next after a tap into a field is lost and each later
     press moves the focus one field, so every check is one field behind (run 38705763402). The runner delivered the press at the right
     place (08:48:48.68), the hold had found Next settled and reachable there, exactly as in a passing run (38740221248), and the app
     logged nothing: no focus change and no keyboard closing. Avalonia 12.1.2's iOS touch handling and its text responder were read and
     neither takes the press. The cause is not found yet, so the press, the release and Next's move of the focus are now each written to
     the log (`keyboard.bar`, `keyboard.next` in `KeyboardRoom`), and the next failing run says which one stops. No retry was put round it.
5. Nothing needed Alan.

## Entry 433: finding the holes faster (2026-10-16)

1. **Measured first**, from one Release build, with timers put into `RenderDifferenceHoleDetector.Detect` and taken out after, on the
   16 corpus photographs the four local scoreboards read and the 600 dpi sample with 25 shots (docs/PERFORMANCE.md, "Entry 433"). All of
   finding the holes: 498 ms median on a photograph (125 to 1726), 1238 on the scan. The largest step is now the marks one by one (132
   and 344, most of it each mark's moments), then the close (44 and 202), then the alignment (52 and 159); the close is 3 to 437 ms on a
   photograph, more than the 3 to 14 entry 410 saw on its pictures; why it grew was not looked into.
2. **The close** now works only the columns that can change (`BinaryMorphology.Pass`): an erosion from each row's first set pixel to its
   last, a dilation within a half width of another row's set pixels. On the scan 202 ms to 102, and hole finding about 1238 to 1133; on a
   photograph the median close 44 to 42, the slowest 437 to 322. **The phase correlations were not changed:** OpenCV already pads each
   cell to its best transform size and the cells already run at once, so what is left is the transforms, about 33 ms of work a cell at
   600 dpi, and a different transform gives different shifts. No QR detector runs on several threads.
3. **Nothing moved:** the four corpus scoreboards, the synthetic scoreboard, `corpus counts` (committed and local) and the identify sweep
   are identical before and after but for their time columns. `BinaryMorphologyTests` holds the close to OpenCV's own on six new wide
   pictures with marks inside them as well as the ten before; a stretch cut too short fails 16 of 18. The baseline gate passed.
   **Found while checking, not changed:** `grouplab corpus counts` reports that its committed record differs from main's own output on
   29 of 55 images (and the local manifest's likewise), before this change as after it; the record has not been rewritten since
   detection changed in entries 415 and 417. Rewriting it with `--write` is a review of what changed, left to the planning session.
4. **Written down** under its own heading in docs/PERFORMANCE.md, before and after. Release-note kind `internal`: a 600 dpi scan's holes
   are found about a tenth sooner, and no person would notice.
5. **Worth an article?** Not written (docs/RESEARCH.md): ordinary engineering a shooter never sees.

## Entry 434: the four false hole marks left after entry 415 (2026-10-16)

1. **What makes each of the four**, from the mark, the expected print and the residual around it (a scratch test, deleted after, running
   each corpus picture exactly as `grouplab scoreboard --corpus` does):
   - **diamond-22lr, two marks centred on a diamond's white window** (bulls at about (1.36, 8.40) and (2.81, 8.42) in): the window's
     edges sit unevenly off the render where the crinkle stretched it, and the aiming dot inside it shows as a crescent; the close joins
     the bands and the crescent into one ring of residue (hull solidity 0.50 and 0.46, about 0.26 in). One shift of the print cannot
     explain a stretch, which is why entry 415's moved-print check lets them through.
   - **diamond-22lr, one mark at (1.47, 3.03) in**: a real hole beside a diamond's edge, whose crease shadow lies along that edge. The two
     closed into one long mark (elongation 2.08, about two calibres of area); the split put one centre on the hole, which is found, and
     the other on the shadow, which is the false mark.
   - **dominus-k, one mark at (7.83, 10.56) in**, below the frame: faint residue on the tape's edge, 0.126 in across, a quarter of a
     .243 calibre's area, just over the size floor.
2. **The candidate tried, measured before any change:** a hole's residual is a step all round its rim, a shadow's or a band's is not.
   For every mark on the nine pictures (222 real holes and the four), the weakest of eight sectors' step from inside the mark to a ring
   just outside it. **It separates nothing:** real holes step as weakly as the false marks (the weakest real holes -1.54, -1.01, -0.80);
   two false marks lie among 5 and 16 real holes, one beyond all but one; only the split's far half (-3.14) is below every real hole,
   and a line placed for one half of one split on one sheet is the setting the entry rules out. Nine photographs are too few to tune on.
   Entry 410's rule about faint marks hugging a printed edge was not tried again.
3. **Nothing was changed in detection, so no hole could be lost**; the corpus, rendered sheets, scans, range pictures, scoreboards and
   the learning loop are as main left them (entry 433's checks of 2026-10-16: every scoreboard identical).
4. **Closed by section 4:** nothing honest removes them; this is what was tried and why it failed.
5. **Proof checklist row 3** still says 4 false marks and now names each one's cause. No release note a person would see. **Worth an
   article?** Not written (docs/RESEARCH.md).

## Entry 435: consistency audit of 2026-10-16 (2026-10-16)

1. **The user guide's PDF** is made again from the guide (`grouplab user-guide`), so it has the magenta ring on the selected shot and the
   tab strip's scrollbar in a row of its own; the testing guide's PDF was remade with it (its words are unchanged, its pictures newer).
   **Why the check of 2026-10-07 did not stop it:** `guide_pdfs` in `scripts/consistency.py` did find it, but CI runs that script with
   `--warn`, so it was only ever a warning. It now asks git whether the guide has a commit after its PDF's last one (a rebase cannot
   reorder that, as it can commit times), and a new CI step, "The guide PDFs are made from their guides", runs it with `--guides`, which
   fails. Before the PDFs were remade it failed naming the three commits.
2. **The download page** says each of the newest thirty builds keeps a release, older ones are removed with their tags, and their notes
   stay on the Release notes page (`website/build.py`).
3. **The README's support link** line says Support GroupLab in Settings opens grouplab.org/support, with the address in one place held by
   `SupportLinkTests`. Its state stays Built, not proven: nothing new proves it.
4. **The README and the support page** say a post in the Discord's bug-reports forum becomes a bug report for the developer, its pictures
   on that report and nowhere else, and that anything private goes to the support address.
5. **The Mac sentence** about Command shortcuts and pinch zoom now follows the first tester's (nightly 93) sentences in
   `docs/PLATFORM-SUPPORT.md`, and the README's copy was regenerated from it; the download page follows on the next site build.
6. **Thanks and credits**: Jylee's line names the click that brings a shot's place into view and, with Quack, the dropped set; Quack's
   line names the dropped set; `shots-table` ends "Suggested by Jylee." and `pool-set` "Asked for by Quack and Jylee.", both marked
   reworded since nightly 196.
7. **The phone's half of entry 422** is in the `shots-table` sentence and the parity row.
8. **The GPL section 7 permission**: the wrong words were in six backing records' "where" fields in `docs/claims-backing.json`; they now
   cite entry 279 section 1 (the permission is in LICENSE) and entry 267 (no lawyer review), and `docs/CLAIMS.md` was regenerated.
   `DESIGN.md` line 560 still says the permission is not in force; it is the design as decided in its day, a record, and is left.

The wording of sections 2 to 8 was done by docs-sonnet and read before commit. readme `--check`, claims `--check` (all backed) and the
site build pass.

## Entry 436: the user guide's contents open by section, follow the reader, and every section has a link (2026-10-17)

1. **Subsections.** 62 bold leads in `docs/USER-GUIDE.md` became `###` headings with their own words, the trailing period or comma
   dropped (docs-sonnet). Three were shortened where the lead ran on into its sentence: "Several bulls on any target", "Words underlined
   with dots", "Android updates itself from an APK"; in each, the whole original lead stays in the paragraph. Left alone on purpose:
   wrapped text and button names inside a sentence (`**Marking**`, `**Result** button`, `**Add hole here**`, `**It is a store-bought
   target**`, `**Printer calibrated or serviced**`), leads in the middle of a paragraph, three that read as button names (Advanced,
   Back, Export) and one with a button name nested in it. No `##` line changed. Per section:
   - **Mil or MOA**: Each rifle keeps its own; Everything aiming follows it; Getting GroupLab.
   - **1. Print a sheet**: Three sheets come with the E bull as well; And three with the C bull; The large format sheets print on Letter or A4; Label targets are for a thermal label printer; The zeroing grids are for sighting in by eye; Made for your optic; Several bulls on any target; Store-bought targets; Scale markers; Add a store-bought target; Find holes (Experimental); Large targets; Check the size before you shoot; Your own sheet; A volunteer pack.
   - **2. Shoot it**: none, it has no natural parts.
   - **3. Photograph or scan it**: A flat scan at 600 dpi is the best record of a sheet; Flat on a table, or straight on at the backer; Real inches from a photograph.
   - **4. Mark it and settle the review queue**: Several targets open at once; When a picture names no sheet; Change an answer; Name the caliber if you know it; Holes between bulls, and marks off the grid.
   - **5. Read the analysis**: Tap a number to switch units; Click a hole to edit it; Words underlined with dots; Six figures stay in view; Velocity and the vertical; The zero correction; The two cards; What each "why" says, in plain words; The full CEP table and the fitted ellipse; Fudd buster mode; Shots Needed to Zero.
   - **6. Sessions and the report**: One-page report.
   - **7. Comparing loads**: The loads are never ranked by their figures alone; Each session is named by what tells it apart.
   - **8. The zero correction at another distance, and the dope table**: Chronograph readings; How the screen is laid out; The dope table; At another distance; Hit probability.
   - **9. Keeping GroupLab up to date**: Android updates itself from an APK.
   - **10. Comparing several sheets at once**: The sheets of a set pool into one group; Quicker: drop them together.
   - **11. If something goes wrong**: Report a problem; Send to GroupLab and Send diagnostics, on the phone; Sending in targets.
   - **12. Settings**: none, it has no natural parts.
   - **13. On the phone**: The bar along the bottom; Capture; Show diagnostics on the camera; A photograph from any photo app; On iPhone and iPad; Updates on phones that stop apps in the background; The picture check; The result; Several targets open at once; Sessions, Ballistics and Targets; A target GroupLab did not print.
2. **The contents list** (`toc_list`, `page_guide` in `website/build.py`) is each section with its subsections under it, all in the HTML.
   `/assets/js/guide-toc.js` (no library, `defer`) folds every section but the one being read, marks the current section and
   subsection with the amber left border the hover already used (`aria-current="location"` on the deeper one), opens a section when its
   line is clicked, and scrolls the list itself when it is taller than the window. An `IntersectionObserver` on the headings says when
   to look again; the heading being read is the last one above 30 percent of the window. The fold is a 0.2 s height change, none under
   `prefers-reduced-motion`. Below 1100 px the same list is a closed "On this page" disclosure at the top of the guide, in the download
   page's existing fold style; choosing a line closes it.
3. **Links.** Every h2 and h3 has an id; the h2 ids are unchanged (`site-guide-tests.py` holds them to the slugs of the `##` lines). The
   one repeated subsection, "Several targets open at once", is `13-several-targets-open-at-once` in section 13. Each heading carries a
   `#` link mark shown on hover and keyboard focus; clicking it puts the full address in the address bar with `replaceState`, scrolls the
   heading up, copies the address, and says "Link copied" in a `role="status"` note (or "The link is in the address bar" when the
   clipboard refuses). While scrolling, the address bar follows the heading being read with `replaceState`, only after the reader has
   scrolled, so the history gains nothing. An address with a section lands it just under the sticky header (the page already had
   `scroll-padding-top: 88px`; the headings add 16 px) with its section open.
4. **Checked in headless Chrome** at 1400 by 900 and 390 by 844, both themes, from a local copy of the built site: on load nothing is
   open; scrolled into section 5 it opens with "Six figures stay in view" marked and the address following, history length unchanged; in
   section 13 the list had scrolled itself 273 px to keep the entry in sight; `#5-read-the-analysis` and `#fudd-buster-mode` landed
   184 px down before the margin fix, which is what found the doubled padding; clicking section 8 in the list opened it; the phone
   disclosure works and closes on a choice; with scripts off the side list shows every section and subsection; the testing guide has
   its 9 sections and 2 subsections in the same list.
5. **The build's checks** (`guide_problems`): an id used twice, a link to `#x` with nothing called x, a side list that does not name
   every heading in order, and a user guide with no h3. `feature_problems` now accepts any id on the built guide, so a Features link
   to a subsection is checked the same way. **One choice to note:** these are page checks, so under entry 387 a failure holds back the
   guide page (keeping the copy grouplab.org serves) rather than stopping the whole site; the message names the page, and a test holds
   that. If planning wants it to stop the publish instead, it is one line (`paged` to `problems`).
6. **Features.** 31 of the 62 "In the user guide" links now point at their subsection (docs-sonnet's choice, checked here), for
   example scale-markers, fudd-buster, shots-to-zero, picture-check; the other 31 span several subsections and keep their section.
7. **The PDFs** were made again. Subsections are set at 11.5 pt with no rule under them, as `DocumentPdf` already set level 3 headings.
   GroupLab's PDF writer now writes bookmarks when it is given them (`PdfWriter.Write(pages, bookmarks)`, `DocumentPdf.Bookmarks`): one per
   section, its subsections under it, folded, and the panel opens with them. With no bookmarks a PDF is byte for byte as before
   (`APdfWithNoBookmarksIsUnchanged`), so no sheet changes. The user guide is 1421 KB (1348 before), the testing guide 287 KB.
8. **What else reads the guide.** `scripts/claims.py` keyed 35 backed sentences by their old bold leads; the backing of each was carried
   to its new wording (28 by a matching script, 7 contents-list or picture lines by hand), and the extractor now ignores the `#` marks.
   `claims.py --check` 5303 backed, `consistency.py --offline` 0 findings, `readme.py --check` and `counts.py --check` current.
   `UserGuideTests` read no bold leads and needed no change; it gained the bookmark tests.
9. **Tour and Features** do not describe the guide page's layout, so nothing there is now wrong.
10. **Live, section 5** (checked 2026-10-17 by chores-haiku, read only, with curl): grouplab.org served 436b5f1d. `#5-read-the-analysis`,
   `#fudd-buster-mode` and `#13-several-targets-open-at-once` each name exactly one heading (an h2, then two h3s), and the side list has
   each, the two subsections nested under sections 5 and 13; `guide-toc.js` and the 1421 KB PDF answer 200. The list is in the page
   twice by design: the side list and the phone's "On this page" disclosure.

## Entry 440: the new home page, concept B, built at /testpage/ for Alan to approve (2026-10-17)

1. **The page** is `page_testpage` in `website/build.py`, with its own stylesheet (`/assets/css/testpage.css`) and script
   (`/assets/js/testpage.js`), the site's tokens, self-hosted Plex fonts and theme switch. It has the concept's own header and footer
   and its sections in the canvas's order: the orbit hero, the feature strip, 01 / MEASURE with six cards, 02 / HONEST, 03 / PHONE,
   the closing call and the footer. The canvas's words are kept. **The home page at `/` is unchanged.**
2. **Out of sight:** a `noindex, nofollow` robots tag, not in `sitemap.xml`, in no navigation, linked from no page. `testpage_problems`
   checks all four on every build and stops the publish if one fails (a hidden page found is a whole-site matter, not a page to hold
   back); `tests/python/site-testpage-tests.py` (10 checks, run by website.yml) proves each check catches its case.
3. **Where the buttons go:** Download free and Download for desktop go to `/download/`, which picks the visitor's platform (the
   current home page's button links the Windows installer directly, which a generic "Download free" should not do to a Mac or phone
   visitor); View the source to GitHub; the phone buttons to `/download/?device=android#android` and `?device=iphone#iphone`; each
   card to its Features entry (every-hole, six-figures, compare, ballistics, pool-set, one-page-report).
4. **Motion**, all `transform` and `opacity`: the two rings turn opposite ways (120 s and 200 s a turn) with each picture turned back
   so it stays upright; the wordmark docks into the header as the hero scrolls away and hands over to the small one (the header fills
   in only then, so it never covers the wordmark on its way); the ruler's amber line follows how far down the page the reader is
   (hidden below 860 px); the section index stays bottom right and marks the section in view (hidden on phones); the strip scrolls
   sideways and stops under the pointer; headings and cards ease in once. **Pause motion** stops all of it and is remembered in the
   browser (`grouplab-motion`, read and written in try/catch). Under `prefers-reduced-motion`, or with scripts off, nothing moves,
   nothing is hidden, and the strip wraps instead of scrolling.
5. **Weight**, first load with the cache off, measured in headless Chrome from a local copy: the test page 430 KB in 24 files at 1440
   by 900 and 380 KB at 390 by 844; the current home page 523 KB at both. Two things made it lighter than a first build's 636 KB: the
   orbit and the cards use their own copies (360 and 560 pixels, WebP), the orbit's phone screens cut to their slot's shape; and
   headings and links are set as the canvas sets them, in plain Plex Sans at 400 and 600, so two font files (Plex Sans 500 and Mono
   500) are not needed.
6. **Glossary words:** the page's main content goes through the same glossary step as every page. Decorative labels (the strip, the
   section labels, the cards' label strips) now opt out with `data-terms="off"`, which the glossary check honours too, so a word is
   explained where it is read as prose and never inside a moving strip.
7. **Checked** in headless Chrome: dark and light at 1440 wide and 390 wide; docked state (wordmark gone, header word shown, header
   filled); Pause motion stops the rings and is remembered across a reload; reduced motion shows everything still; the section index
   marks the section in view; no sideways scroll at 390. **Claims:** the page's 13 checkable sentences are backed in
   `docs/claims-backing.json` (the cards by their Features entries, the platforms by docs/PLATFORM-SUPPORT.md, the licence by LICENSE).
   No sentence had to change.
8. **Live** (sections 4.2 and 4.3): grouplab.org served 74cf38fd within about seven minutes of the publish; `/testpage/` answers 200
   with its noindex tag and is not in the live sitemap, and the home page is unchanged. Eight screenshots of the live page, desktop at
   1440 and phone at 390, light and dark, the hero and the whole page each (after scrolling through once, so every section has eased
   in and every picture loaded), are in `C:\Dev\grouplab-local\homepage-concept-b\screens\` for the planning session to show Alan.

## Entry 437: more screenshots in the user guide, and markings on them that stay right (2026-10-17)

1. **Pictures added** (docs-sonnet chose and captioned them; checked here against the pictures): 19 new, none of the seven removed, so
   the guide shows 26. Per section: Mil or MOA, scope-unit and equipment; 1, scale-markers, fingerprint and store-target; 2,
   bull-lines and groups; 3, shots-open; 4, which-target; 5, velocity; 8, pairing and ballistics-hit (the ballistics picture moved down
   to "How the screen is laid out"); 10, set-tab; 12, settings-data (new, below); 13, four phone pictures (fold-capture,
   fold-result-landscape, fold-open-targets, fold-sessions-landscape), shown at a phone's size. **One new render**: the lower half of
   Settings, `settings-data`, which section 12 describes (Your data, Diagnostics, Report a problem), added to the render walk, to
   `screens.json` and `SOURCES.md`, and to the tour's Settings stop as its second picture. **Screens the guide describes that have no
   render yet**, each needing a new state in the render walk, listed for the next pass: the update bar (9); the Report a problem
   window (11), a window of its own that the walk does not open; the live camera with its level (3 and 13), which needs a camera; the
   Design your own sheet form scrolled to Make the sheet (1); the hole editor (5); on the phone, the picture check, Fix holes,
   marking by hand, Ballistics and Targets (13), which come from the emulator's sweep, not this walk.
2. **Where things are on each picture.** When `Entry109Tests` photographs a screen at 1400 by 900 in the light theme, it now writes
   `<screen>-bounds.json` beside it (`ScreenBounds`): every visible control with a name, as `[x, y, width, height]` in the picture's
   own pixels, clipped to what is on the picture. A control's name is its `Name`, else its accessible name, else the words on it
   (button, tab, check box, menu entry); parts of a control's template are left out, and the first visible control with a name keeps it.
   The weekly screenshot job writes them with the pictures and commits the folder, so a picture and its positions are always retaken
   together. **The phone pictures have none:** they are taken on the Android emulator by `scripts/android-screens.sh` from the screen
   alone, and nothing there reads the application's layout yet; section 13's phone pictures are therefore shown unmarked.
3. **The markdown says which controls a picture marks**, in one line under it, `<!-- marks: Accept and analyze | Next unsure hole -->`,
   names in the order the text mentions them; GitHub shows nothing for it. `render_guide` takes it out and draws the markings;
   the text's "(1)" and the caption's "1 ..." become numbers that point at their outline. **The site build fails** (the guide page is held
   back, as every page check since entry 387) when a marking names a control not in its picture's positions file, a marked picture
   has no positions file, a picture has more than three markings, a caption does not list its markings, or a "(n)" in the text has no
   marked picture with that number under its heading. `site-guide-tests.py` holds each of these (26 checks).
4. **The look, as section 3 set it:** an amber (`--amber`) outline 2 px wide with a 1 px halo in the page's background, outside and
   inside, and a 22 px amber badge with its number in the page's dark text color (`--on-amber`) in Plex Sans, on the outline's top left
   corner, moved inside the picture at its edge and along the outline when it would sit on another badge (`mark_overlay`). It is laid
   over the picture as positioned elements sized in percent of the picture, so it scales with it and the PNG is untouched; HTML rather
   than an SVG layer, because a badge has to stay 22 px across however small the picture is shown, which an SVG scaled with the
   picture cannot do. Pointing at or focusing a number in the text or caption thickens that outline to 4 px (no animation under
   reduced motion). Badges and outlines are `aria-hidden`; the caption, which is the alt text, says the same in words.
5. **In the PDF** `GuideVerb` draws the same outlines and badges into each picture from the same positions file and the same
   placement (white halo for the paper), and `DocumentPdf` skips the marks line and sets a phone's tall picture at most half a page
   high at its own proportions instead of across the page.
6. **Which controls are marked** (2.4, docs-sonnet's choice from each positions file): scope-unit, Mil, MOA and Both; equipment, the
   scope unit and click value fields; targets, Open as PDF and Bulls in; scale-markers, the three print buttons; fingerprint, the
   steps, Next and Its printed size; bull-lines, Lines to bulls, Next unsure hole and Done; shots-open, Check your printer and Measure
   this sheet with a ruler; which-target, its three answers; velocity, Conditions on the day and Velocity band; sessions, the rifle
   and load filters and Delete; pairing, the three choices under a reading; ballistics, Work out the table and the Trajectory and Hit
   probability tabs; ballistics-hit, the target shape; set-tab, the set's tab; settings-data, Export all my data, Import data and What GroupLab sends. A second marking on set-tab (a single table row
   standing for the whole Shots list) was dropped here as misleading. **Unmarked, and why:** analysis-open and settings, because no
   control the text under them sends the reader to has a name in their positions files (the six figures are not controls); marking,
   because it shares its heading with which-target; compare, which has no positions file yet (below); store-target, whose picture is
   put together from two windows and has none; and the phone pictures.
7. **The pictures were retaken** to go with their positions: the positions must come from the same render as the picture, so this
   commit carries a fresh render of every desktop screen, exactly as the weekly job makes them, except **Compare loads**, whose
   published picture is Alan's own two suppressor sheets from the test data (186 MB, not downloaded here): it keeps its picture, has
   no positions file and no markings until the weekly job retakes both. **Fixed on the way:** the render walk analyzes the same
   sample many times, and its "This looks like another copy of a sheet" notices had piled up in the corner of every later screen (two
   in the published pictures, covering Open as PDF on Targets); a notice fades by itself in a few seconds, so the walk now clears
   them before each picture.
8. **Checked** in headless Chrome, all 14 marked pictures at 1400 by 900 in both themes: every outline sits on its control, badges
   clear the edges, and numbers in text and caption thicken their outline. Before and after, the Session records picture:
   [before](figures/entry-437/sessions-before.png), [after](figures/entry-437/sessions-after.png).
9. **The PDF** was made again: 35 pages, the markings drawn into its pictures, the phone pictures half a page high and centered; the
   user guide PDF is 4104 KB (1421 before), a reasonable download for 26 pictures. Pictures are now held to 1400 by 1100 pixels, which
   is what half a page shows at about 200 dots an inch.
10. **Claims:** the extractor now ignores a picture's badges and the bracketed numbers, as it ignores the heading marks; the backing of
   each sentence that only gained a number was carried over, and each new caption is backed by its picture and positions file
   (5322 claims, all backed).

## Entry 438: a statistics primer for shooters, from nothing to judging a group (2026-10-17)

1. **Where it lives.** `docs/STATISTICS-PRIMER.md`, "Statistics for shooters, from the beginning", is the fourth tab beside User guide,
   Trying GroupLab and Glossary, with entry 436's contents list and a link on every section and the guides index, sitemap and README
   naming it. It has its own PDF (111 KB, bookmarked) and its own link preview picture. Linked from the home page, the analysis section
   of the user guide, the glossary's head and the five articles (cep-explained, how-many-shots, mean-radius-or-extreme-spread,
   moa-mils-inches, pooling-groups). Published, since it meets section 5 (below).
2. **How it teaches.** It opens on a real 25-shot group read on the phone (a consented scan, already published) and builds 18
   sections in order: the center, extreme spread, mean radius, angles, sigma, CEP, luck, intervals, comparing loads and p-values, how
   many shots, flyers, velocity SD, hit probability, what is good enough, reading a claim, what no number can tell you. Each has the
   idea in one sentence, a picture or a figure where one helps, "At the range", and "In GroupLab" linking to the user guide's
   subsection (a link into another guide goes to its page on the site, and the site build checks the heading exists). The formula comes
   last, folded under "The math, if you want it", and labeled the same in the PDF. Angles come before sigma so no unit is used before
   it is explained.
3. **The numbers are GroupLab's.** `PrimerNumbers` in Core works out every figure on a fixed seed (40 groups each of 5, 10 and 20
   shots with their figures; how far extreme spread and mean radius wander over 4000 groups at 3 to 30 shots; two identical loads
   compared 2000 times; the hit table; shots needed), `grouplab primer` writes `docs/figures/primer/primer-data.json`, and
   `PrimerTests` fails if the committed file is not what Core computes, and holds four figures to the published articles (0.68 to
   1.92 from five shots, 81 shots for 20 percent tighter, CEP 50 and mean radius as 1.18 and 1.25 sigma, the test calling identical
   loads different about one time in twenty). The page does no statistics: the build draws each figure as an SVG from the file, and
   `primer.js` only steps through more of the same groups.
4. **Figures that teach luck**: six five-shot groups with **Shoot five more** and a choice of 5, 10 or 20 shots; the spread of extreme
   spread by shot count; two identical loads with **Compare again**, keeping a tally of how often one "won". Also the center, mean
   radius and CEP 50 and 90 drawn on groups GroupLab simulated. With scripts off, or reduced motion, each is a still picture whose caption
   says what it shows (checked: all six drawn, every button hidden).
5. **Judging what is good** is by job: a hit table from precision alone for 4 and 8 inch circles at 100 to 800 yards, with what it
   leaves out said plainly. **No outside typical figures are given** (factory, match or benchrest groups, velocity SDs): no measured
   source was found that could be backed, and the page says so rather than quote one. Reading a "half-MOA rifle" claim and the honest
   limits have their own sections.
6. **Reading level** (`scripts/readability.py`, Flesch-Kincaid grade, leaving out the folded math, tables, pictures and links): the
   whole page grade 5.1, no section above 6.1 after docs-sonnet's plain-language pass (5.2 before it); a general newspaper aims at
   about 8 to 9. The pass changed no number, claim, link, heading or math part.
7. **Easy to pass on**: a one-page summary, "five things to know", made from the guide itself (the five points marked in it, each with
   its section's "At the range" advice), in Letter and A4 (`DocumentPdf` now sets A4 and tables), linked at the top and bottom of the
   page. Each section's opening sentence stands alone.
8. **Section 5's rules**: every checkable sentence backed (one rule for the primer's surfaces naming the data file, `PrimerTests` and
   STATISTICS.md, 5403 claims backed); no pseudoscience; the developer not named; American spelling (0 British forms); no em dashes;
   the glossary's words used its way. On a phone the six-column hit table scrolls inside itself (all guides' tables do now below 860
   px), and the page is 390 px wide at 390.

## Entry 439: "Walk me through a demo", a guided tour of GroupLab on a sample target (2026-10-17)

1. **Where it is offered.** Once, after the first run's questions are answered, as its own card: "Walk me through a demo (about
   five minutes)" or "Not now", and never again on its own after either (Esc is Not now). Any time from Settings, a row near the top
   ("Walk me through a demo", and "Carry on from step N of 13" when a tour was left part way, which does the steps before it for the
   person so the screen is where that step expects), and from the header's menu. The offer sits beside the first run, not on it, so
   the first run's own questions and tests are unchanged.
2. **The sample is made by GroupLab** when the demo starts: the 25-bull Letter sheet as it prints, one simulated hole a bull, the way
   the tests make theirs (`SyntheticSheet`), written into the application's own folder. **This differs from the entry**, which names
   a shipped `samples/sample-25-shots.png`: no such file exists, and the only sample (`gl-cf25-ltr-d-25-shots-600-dpi.png`) is a real
   person's consented scan carried by the Windows zip alone. Question 101 asks whether this is what was meant; the testing guide's
   stale sentence is corrected. It opens in a tab of its own named "Demo: sample sheet", is read as any sheet is (24 of 25 holes found,
   so the review queue has something in it), and is never offered to the project.
3. **The steps** are one list of data (`MainWindow.DemoSteps`: screen, named control, words, and what Next does), 13 of them: the
   sample, GroupLab reading it, the review queue, the caliber, Accept and analyze, the plot and figures, each figure's range and why,
   clicking a hole, the zero correction, Session records and the report, Targets and its print button, Ballistics, and where to
   get help.
   Comparing loads and several sheets are left out: honestly shown they need a second sample. **The tour never prints**: its Targets
   step points at Open to print, the print button on every platform (Print... is Windows only), and has no action (a test holds
   that).
4. **How it behaves.** Each step draws entry 437's marking around the real control, an amber outline with a halo in the window's own
   background and the step's number in a badge, with a card beside it (the step, "6 of 13", Back, Next, End the tour) placed below,
   above or beside the control so it never covers it, and following it as the window lays out. The rest of the window stays usable
   and is not dimmed. The person can do the step or press Next and GroupLab does it. Tab and Enter reach every button, Next takes
   the focus with the step's words as its description, and Esc ends the tour. Checked in both themes at 1280 by 720 and 1400 by 900.
5. **Nothing the demo makes is kept** unless the last step's "Keep this demo in Sessions" is ticked: its tab closes with nothing
   asked, every session it saved (found by its picture) is deleted, and its picture is removed. `Entry439Tests` holds that the
   database is the same after a finished tour and after one ended part way with Esc, that keeping keeps exactly one session, and that
   the offer comes once.
6. **It stays in step with the application.** Controls are named one way for the guide's positions files and the demo
   (`ControlNames`, which the test recorder now uses too), and the headless walk fails, naming the step, when a step points at a
   control that is not on its screen. To give the steps something to point at, the rail's six buttons now carry accessible names
   (they had only tooltips, so a screen reader could not name them), and the picture, status line, review queue, plot and zero
   correction carry names.
7. **The weekly render walk** photographs the demo at its plot step (`demo-tour`), for the Features page (a new feature, "since next")
   and the tour's Settings stop; the user guide gains a "Walk me through a demo" subsection under Settings and a line at its start;
   the testing guide's first minute now starts with the demo; the README's feature list names it.
8. **The phone tour (section 5) is the next entry's work**: the desktop tour filled this one. Android and iPhone need the same offer
   and steps for their own screens (Capture with the sample chosen from the app's files, the result, Shots, Share a picture, Sessions,
   Ballistics, Targets), and their own headless walk.

## Entry 441: concept B ("Orbit") is the grouplab.org home page (2026-10-18)

1. **The home page is concept B.** `page_home` in `website/build.py` is entry 440's page, renamed from `page_testpage`, with its
   stylesheet and script now `assets/css/home.css` and `assets/js/home.js`. Nothing about its motion changed, so pause motion, reduced
   motion and scripts off still leave it complete and still.
2. **The page before it is restorable in one step**: it is `page_home` in `website/build.py` at b216ce56, named in the commit message
   and in the comment above the new page. The helpers only it used (the README's product picture, its alt text and its three lines,
   and the two product WebP files) went with it; they are in the same commit.
3. **Indexed and in the sitemap.** The noindex is gone, and the page now carries a canonical address and the same sharing tags as every
   other page. `/` was already in the sitemap. Entry 440's check is turned around as `home_problems`: the home page is concept B, does
   not ask to stay out of search, is in the sitemap; the old preview address is not in the sitemap and is a page with one link to `/`
   (no meta refresh, which entry 151 bans), never a second copy; the navigation does not name it. `tests/python/site-home-tests.py`
   (renamed from `site-testpage-tests.py`) holds it with 22 checks, run by website.yml before the build.
4. **What the page before carried and a visitor still needs**: downloads, the guides, Shoot a target and Research are in the header;
   support, release notes, GitHub and Discord were in the footer, which now adds the guides, Shoot a target and the hardware survey
   (the survey was reachable only from the old footer). The honest limits are one paragraph under the three points: test builds,
   unsigned and rebuilt after every change that passes, so one may be broken; not built yet, hole detection on plain paper and
   synchronization; links to the download page's expectations and the full status. Not carried over: the "New in GroupLab"
   spotlight (the Features page has its own, "Newest"), and the tour and statistics primer paragraphs (the header reaches both).
5. **Everything that describes the home page**: the README describes no part of it and pictures none; no screenshot of the site
   exists in `docs/figures`; `docs/WEBSITE.md` names only the live check, which reads the build tag the page still carries.
   `scripts/consistency.py`: 0 findings. Claims: three new sentences backed, 5408 claims all backed.

## Entry 442: the desktop from the keyboard, Quack's request (2026-10-18)

1. **Why the focus went back to the top.** Choosing a shot calls `PickShots`, which refreshes the analysis, and the refresh clears the
   shot table and builds it again (`offsetTable.Children.Clear()`), so the row that had the focus left the window and the focus with it.
   The same happens wherever a panel is rebuilt when something in it is chosen: the tabs (`ShowTabs`), session rows, the library list
   and the rest of the 85 panels that are cleared and rebuilt. One fix covers them all: `KeyboardFocus` (new,
   `src/GroupLab.App/KeyboardFocus.cs`) remembers the focused control and its ancestors when it takes the focus, and when it leaves the
   window with nothing else focused, gives the focus to its replacement: the control with the same `Tag` (each shot row now carries its
   shot), otherwise the same kind of control in the same place of the rebuilt panel. It comes back the way it went, so a ring drawn for
   the keyboard is drawn again and a click draws none. On the main window and the figures' own window.
2. **Arrow keys.** The shot table answers its own (`OnShotKey`): Up and Down choose the shot before and after through `PickShots`, as a
   click does; Home and End the first and last; Left and Right the same bull on the nearest sheet before and after in a pooled set
   (`SetPool.SheetOf` and `BullOnSheet`), nothing on one sheet; Esc clears the choice. The focus follows the chosen row. Elsewhere,
   `KeyboardFocus` moves between buttons by where they sit: Left and Right to the nearest button on the same line, Up and Down to the
   nearest in the same column for the rail's buttons and a list's rows, within the bar or scrolled panel the button is in (the tab
   strip, the header, the toolbar, a side column). It acts only when a plain button has the focus: text boxes, number boxes, check
   boxes, radio buttons, switches and drop downs keep their own arrows, and moving never presses anything. The marking picture's arrows
   are left alone; the status line suggests what to do with them.
3. **The caliber line.** `UnitSwitch`'s number pattern did not allow a number written without its leading zero, so a tap remembered in
   centimeters read ".224 in (5.69 mm)" as 224 inches after the point and wrote ".568.96 cm (0.57 cm)". The pattern now reads ".224" whole
   and never starts inside a number, and the caliber line, already in both units, is no longer a number a tap switches. Nothing to do
   with the culture: every figure is written with the invariant culture.
4. **Tests.** `Entry442Tests`: on one sheet, Tab from the window's start into the shot table, Space, and the focus is on the chosen row
   with its keyboard ring, the next Tab on the row after, Enter the same; Quack's pooled set of three sheets end to end with Space, Up,
   Down, Home, End, Right and Left across sheets and Esc; the rail with Up and Down, the top bar and the tab strip with Left and Right,
   a text box keeping its arrows; the caliber line. With `KeyboardFocus` switched off three of the four fail. `UnitSwitchTests` gains
   the number without its leading zero.
5. **Focus ring.** Fluent's own ring, rendered and looked at on a tab and a chosen row; it is not clipped by the tab strip.
6. **Everything public.** The user guide's new "From the keyboard" under Read the analysis; a Features entry, "Work it from the
   keyboard", with its own picture from the render walk (`keyboard`, a row chosen with Space and the ring on it); the README's Phase 3
   line; three release notes crediting Quack; a note for Quack in for-alan.md.

## Entry 443: one header, one footer and the home page's look on every page (2026-10-18)

1. **The header.** `site_header` in `website/build.py` writes the one header every page carries, the home page's, with every link it
   had before entry 441: Download, Tour, Features, Shoot a target (Send a target while the upload page is open), Guides, Research,
   Community, Release notes, Support and GitHub. As entry 304 had it, from 981 to 1499 pixels the last four move into More, and below
   981 every link is in the menu. The page being read is underlined in amber. The link words are written as before and set in capitals
   by the stylesheet, so no word changed. Only the home page has Pause motion and the docking wordmark, since only it moves.
2. **The footer.** `site_footer` writes the one footer, in the home page's look with the site footer's words and all ten links (the
   home page's footer had four).
3. **One look.** Every other page takes the home page's header and footer, its type (headings in Plex Sans 600, labels in mono
   capitals), its buttons (mono capitals, an 8 pixel corner, a hairline border, amber for the main one), square-cornered panels, notes
   and callouts, and 40 pixel gutters; colors and both themes were already the same tokens. No orbit, no docking and no motion off the
   home page, and no script added, so with scripts off or reduced motion every page is complete and still. No ruler or section index
   on the long pages: they would add weight and motion for little help. No page's words or addresses changed.
4. **Weight.** Measured over all 76 pages (HTML, stylesheets, scripts, preloaded fonts and the images a page loads first): every page
   is lighter, by 2.2 to 9.5 KB; the home page 116,407 to 114,171 bytes, the download page 221,526 to 212,011. The stylesheets are served
   without their comments (`served_css`), and the old header and footer rules and seven rules only the page before concept B used are
   gone; `site.css` is 35,144 bytes, `home.css` 9,461.
5. **Checks.** `header_problems` fails the build when a page lacks the shared header or footer, or its header lacks one of the links;
   `tests/python/site-home-tests.py` holds it (32 checks), and that the home page and a plain page take their header and footer from
   the same two functions. `scripts/claims.py` now reads the shared header and footer once, as a surface of its own, instead of running
   them into each page's first and last sentences; the 20 page sentences this split out are backed as the same words as before.
6. **Looked at** in headless Chrome at 1600, 1400 and 1200 pixels and at the narrowest Chrome allows (512): the header, the More menu,
   the footer, the download, support, release notes and user guide pages. At 360 pixels 22 pages scroll sideways, the same 22 before
   this entry (wide tables and figures in research articles and two guides): recorded, not fixed here.
7. **Pictures of the site.** None exists in the README or `docs/figures`, so none to retake. The user guide's PDF, behind since entry
   442, is regenerated.

## Entry 444: the home page's look really on every page, and every link in the bar from 1280 pixels (2026-10-18)

1. **Headings.** Every page's h1 and h2 are the home page's display face, IBM Plex Sans Condensed bold (entry 443 had set them in Plex
   Sans 600). The h1 runs from 44 to 80 pixels with the window (it was 52 at most), a guide's or article's from 40 to 60, an h2 from 30
   to 46. The home page's own section headings keep the face they were designed in. Titles are as written, not in capitals: the look
   question is a DESIGN NEEDED line in `docs/notes/for-alan.md`.
2. **The top band.** Every page's top is in the hero's order: the mono label with a short amber rule before it, the condensed title, the
   lead, and the page's buttons, which were already the home page's (now 48 pixels tall, as "Download free"). Research, Release notes
   and Tour gained the label their neighbours had; an article's and a tour screen's breadcrumb is set as the label. Behind every top
   band but the home page's, the home page's rings, still: an inline drawing (`PAGE_RINGS`) in the theme's own line color on the sunk
   background, fading into the page, so it is right in both themes with no picture to load.
3. **Ruler and section index** on the guides, research articles, release notes, features and support (`LONG_PAGES`): the ruler down the
   right edge as on the home page, its amber line following the scroll, and the section index, one link to each h2 (on the release
   notes, the twelve newest builds), the headings given an address where they had none. At 1400 pixels the home page's index would
   cover the content's right edge, so on a long page it shows its numbers and opens the names on hover or focus. `page.js` adds the
   ruler's numbers and marks the section being read; nothing moves by itself, and with scripts off both are there, unmarked.
4. **Cards, tables, code.** Research cards, feature cards and the Newest cards sit in the home page's hairline grid, a picture edge to
   edge on a research card; corners square on folds, picks, tabs and badges; list markers amber; table headings in mono capitals; code
   blocks on the sunk background with an amber edge.
5. **The header.** All ten links show from 1280 pixels (measured: nothing overflows at 1280, 1300 or 1500 on the home page, support or
   the user guide), tighter type and spacing below 1500; More from 981 to 1279; the menu below. On the home page from 1280 to 1499 Pause
   motion shows its icon, as on a phone. The home page's header is blurred and tinted from the start, so the orbiting screens no
   longer show through its links, and solid once the wordmark has docked; the wordmark flies above it while it docks.
6. **Weight.** Same measure as entry 443, over all 76 pages: every page is lighter, by 452 to 9,900 bytes; the home page 114,179 to
   104,279, features 245,790 to 240,043, download 238,531 to 232,035, the user guide 320,341 to 314,111, support 151,389 to 146,107,
   release notes 320,303 to 315,118. Paid for by serving the how-it-works pages' and the download page's own styles only to them
   (`how-it-works.css`, `download.css`), 15 unused rules removed, and scripts served without their comment lines (`served_js`).
   `site.css` is 28,250 bytes.
7. **Checks.** `heading_problems` fails the build when a page's h1 is not the display face: it reads the stylesheets each page links,
   at every width, and fails on a rule reaching an h1 (by element or by one of its classes) with another face or weight, on an h1 with
   its own style, and when nothing gives `.tp h1` the display face. `tests/python/site-home-tests.py` holds it and the rest (49 checks).
   `scripts/claims.py` cuts the section index out of each page as it does the header, since it repeats the page's headings; the tour's
   opening, now with its label, is backed as the same words.
8. **Proof.** Twenty-four pictures in `docs/figures/site/entry-444/`: home, features, download, the user guide, support and release
   notes, at 1400 by 900 and at 390 on a phone, dark and light. Also looked at with scripts off and with reduced motion (complete and
   still), and no sideways scroll at 390.

## Entry 445: the guides' "On this page" on the other long pages (2026-10-18)

1. **One function.** `with_contents` in `website/build.py` puts the guides' contents on every page `CONTENTS_PAGES` names, and the
   guides now take theirs from it too: `contents` reads a page's h2 and h3 headings (a small HTML reader, `_Headings`), gives an
   address to any heading without one and a link mark to each, and `contents_navs` writes the side list and the same list folded at the
   top below 1100 pixels. A page laid out as a guide gets the list beside its article; any other gets it beside everything after its top
   band. `guide-toc.js` now follows the list's own entries rather than a guide's headings, so it works on every page, and passes over an
   entry the Desktop | Mobile switch hides.
2. **Pages.** The guides, features, every tour screen (the list follows the view where a screen's sections differ by side; on the
   features page both sides have the same sections), every research article and the research index, release notes, support, what can be
   measured, shoot a target and the community page. **Not given one:** the download page (one section a device, behind its own picker),
   the tour's index (its headings are links to the screens, not sections) and how it works (its map links each part already). The
   release notes are now grouped: "The newest builds", the ten newest each named in the list, then "Older builds" as one entry; a
   build's own headings sit a level below its name.
3. **One list, not two.** Entry 444's numbered section index is gone from every page that had it (all of them now have the list); the
   home page keeps its own. The ruler down the edge stays.
4. **Checks.** `guide_problems` runs on every page with the list: every id once, every link lands, the list names every heading in
   order in the HTML as built, so it is whole with scripts off. `tests/python/site-guide-tests.py` 56 checks (26 new),
   `site-home-tests.py` 49. `scripts/claims.py` cuts the lists out like the header; six sentences that used to run on from a list are
   backed as the same words as before.
5. **Weight** (HTML, stylesheets, scripts, preloaded fonts and first images, as entry 443 measured): features 239,843 to 260,369 bytes
   (73 entries listed twice), a tour screen 284,419 to 291,851, a research article 152,319 to 158,991, release notes 314,918 to 320,794,
   support 145,907 to 152,246; the user guide 313,911 to 312,438 and the primer 201,537 to 199,610 are lighter (the numbered index gone).
6. **Pictures.** Features, release notes, support, a research article and a tour screen at 1400 pixels and at a phone's 390, both
   themes, in `docs/figures/site/entry-445/` (headless Edge). Nothing on the site or in the README pictures the website's own pages, so
   nothing there to retake.
7. **Also found.** `scripts/american-spelling.py` let a backslash in a quoted string match two ways, so where it paired a quote wrongly it
   tried every split of every backslash after it: with this entry's builder it ran for hours (the spelling test ten minutes before it was
   stopped). A backslash now only matches as an escape; the whole check takes a second. Paired rightly, it found two style names
   spelled "centre" in the builder, now "center"; nothing a reader sees changed.

## Entry 446: the phone's "Walk me through a demo" (2026-10-18)

1. **The tour on the phone.** `PhoneDemo` (`mobile/GroupLab.Mobile/PhoneDemo.cs`), shared by Android and the iPhone: thirteen steps as
   data (`PhoneDemo.Steps`), each naming its place, the control it outlines by automation id, its words and what Next does. The sample
   opens as a chosen picture from GroupLab's own files (never the camera), then the plot, the figures, Fix holes, Shots and its table, Zero
   from this group, Share a picture and its page, Sessions, Ballistics, Targets' Made for your optic, and Settings. Each step draws the
   desktop's amber outline and numbered badge around the real control and a card under it, over it, or in the larger room left, where it
   scrolls. Offered once after the first run's questions (`Shell.Begin`); Settings has a **Guided demo** card with **Walk me through a
   demo** and **Carry on from step N of 13**. Next does a step; **End the tour** or the phone's Back ends it (`Shell.Back`). The card's
   words are the desktop's, now in `src/GroupLab.App/DemoWords.cs`, linked into the phones.
2. **One sample.** `DemoSample` in Core makes the 25-bull Letter sheet at 300 dpi with one hole a bull, from a fixed seed; the desktop's
   `WriteDemoSheet` and the phones both call it, so all three make the same pixels (`DemoSampleTests`). Question 101 archived as answered.
3. **Nothing kept unless chosen.** Ending removes the demo's target with nothing asked, its session, the session's picture folder and the
   sample; ticking **Keep this demo in Sessions** on the last step keeps them. The caliber the demo names (.308 Winchester, as on the
   desktop) goes back to what was typed. Text goes through the phone's styles and so `TextSize.Of`, the iPhone's Dynamic Type included;
   the badge grows with the text; a screen reader hears each card as "N of 13: title. words" and again on Next, which takes the focus.
4. **Walked in CI.** A scenario step, `demo`, walks every step and fails on a control not on its screen or on anything left behind; it is
   the last step of `phone-sweep.json`, which the iOS simulator run (ios-app.yml) and the Android emulator run both play, and headless in
   `PhoneSweepScenarioTests`. `Entry446Tests` (6): the walk, keeping, ending part way by Back, Settings' carry-on, the card inside the
   screen and named at Android's largest text, the offer once. The phone screens walk pictures step 2 as `fold-demo`, waiting for the
   emulator's first run (SCREENSHOTS.md); the Features entry's Mobile side says so until then.
5. **Also found.** The card's first placement measured its own words on every layout pass, against its scroll's own measuring, and
   the screen under it was never laid out again; placement now reads the size the last layout found and sets a value only when it
   changes. The screen quality check now treats the card's words over the page as an overlay, as a sheet is.
6. **Same change:** the user guide's demo section, the Features entry (Android, iPhone and iPad), the parity table, the release note.
   No look question: the outline, badge and card are the desktop's styles.

## Entry 447: an overnight queue, its four parts (2026-10-18)

1. **The marking picture's arrow keys.** With a hole selected and the picture holding the focus, an arrow nudges the hole one pixel of
   the picture and Shift and an arrow ten, in the direction the arrow points on the screen however the view is turned
   (`MarkingCanvas.OnKeyDown`); a run of presses, ended by any other key, a press on the picture or a pause of two seconds
   (`MarkingCanvas.NudgeRun`), is one step Undo takes back (`MarkingSession.NudgeShot`). With nothing selected the arrows move the view,
   40 pixels a press and 200 with Shift. A text or number box with the focus keeps its arrows. `Entry447Tests` (4); the user guide's
   From the keyboard section has the bullet.
2. **Keyboard and screen reader pass.** Desktop: every screen (marking, analysis, Sessions, Targets, Ballistics, Compare, Equipment,
   Settings) and the report and printer check windows tabbed all the way round headless: the focus never lost, the round reaching the
   bar down the left (so nothing holds it inside a panel), every stop named as a screen reader reads it (its automation peer's name).
   About 380 stops; 14 had no name and now do: the column grips ("Column width: the left and right arrows widen or narrow it"), the
   caliber box (named on its inner field too, `CaliberBoxes.Named`, the phone's as well), the marking picture, and the printer check's
   name, across, down and unit boxes (by the words beside them). The user guide's tool keys gained B, O, Ctrl+N, Ctrl+V, Delete on a
   placed bull and the arrows. Phones, at Android's largest text: every place along the bottom, the result and the five pages one press
   from it, every control named (none was missing); the one fault was that TalkBack and VoiceOver read the bar along the bottom before
   every screen, so the frame now holds the page first (`Shell`, a grid in place of the dock). Tests: `Entry447Tests` on both, the
   desktop's two round trips and the phones' names and order. The focus ring is Fluent's own, unchanged. **Left** (in STATE): the
   dialogs not opened by the tests (CSV mapping, store-bought target choice, Fudd buster, group tools, print, the figures window), the
   phones' pages two presses away (Fix holes, velocity, fingerprint), and a run with a real screen reader on each system.
3. **The iOS Real taps flake, found and fixed at its cause.** The red runs share one thing: a tap lost just after a field took the
   focus and the keyboard came up for the first time. Two records were added to find it: what the drawing held when GroupLab's own
   check could not reach a control, and every press and release the window receives while a scenario runs (`scenario.pointer`). The
   loop of runs then caught it (run 39142559271): the tap on the distance box reached the app 0.7 s late and its release 1.8 s after the
   press, while the keyboard first came up and held the screen's thread; by then the wait that decides a finger is being held had run
   out, so the box opened its Cut, Copy and Paste menu, and the first tap on Next landed on that menu's dismissing layer
   (`LightDismissOverlayLayer`), closing it and nothing else; every later Next worked, one field behind. A person meets the same when
   the keyboard is slow to come up. **The first fix** (nightly 201) closed such a menu when the late release showed the finger had been
   down less than the hold's wait; 12 runs in a row were green, and the 13th (39172455366) showed it closing the menu while a
   dismissing layer stayed over the window, because the field's hold does more than open the menu (it selects a word and shows
   handles). **The fix at that cause** (`KeyboardRoom.HoldOffForAFocusingPress`): the press that first puts the focus in a field is a
   tap to place the caret and bring up the keyboard, so the field's hold is off while that press is down and comes back once its
   release has been handled; a long press on a field that already has the focus opens its menu as before (`Entry447HoldTests`).
   **Two more causes, found on 2026-10-19 with more records** (a stalled screen thread, `scenario.stall`; each holder of an unreached
   control as a hit test sees it; a count of frames drawn). Run 39243232233: XCTest spent 34 s in "Wait for com.apple.springboard to
   idle" before one press, with GroupLab answering throughout, and the press lost its release; a finger does not wait for the home
   screen, so the tapper now replaces XCTest's wait for idle with one that returns at once (`NoWaitForIdle`, printed in
   touch-runner.log). Run 39284329697, then 39292731869 with frames counted: after the first keyboard, and only the first, the
   simulator drew nothing for 2 to 6 seconds (131 frames from the keyboard opening until 2.3 s later) while the screen's thread
   answered, and every holder of the bar's Next was hit-testable, enabled, shown and in place; Avalonia judges a press against the last
   drawing, so Next could not be reached until drawing resumed, and a hold gave up after 5 seconds. A person cannot tap a bar that has
   not been drawn, so this is the simulator's drawing, not GroupLab's fault. The check now counts only time in which frames are drawn
   toward its 5 seconds, waits out a pause in drawing up to 30 seconds and says it ("nothing was drawn", never "covered"), and a control
   that is really covered still fails after 5 seconds of drawing (`PhoneTouchScenarioTests.AHoldWaitsOutAPauseInDrawingAndSaysIt`).
   No sleep or retry was added; question 104 asks planning whether this counts as fixed at the cause. The scripted taps' check also
   refuses a tap a menu's layer would take, and says so. **The proof**: 20 runs of ios-app.yml in a row, all green, from 34d259e6 on
   2026-10-19 and 20 (one more, 39304940907, was cancelled by another run starting and is not counted): 39299320603, 39306970375,
   39310435399, 39314918748, 39319450122, 39323658807, 39327999986, 39332104867, 39335983262, 39339795636, 39342772254, 39345433622,
   39348682023, 39351458267, 39354305928, 39356898269, 39359305053, 39361859347, 39364572136, 39366879795. The second showed the pause
   waited out: 1.0 s with nothing drawn, then Next reached.
4. **Tests and speed.** 4.1, **the corpus counts record rewritten** (`scans/phase1/measurements/detection-counts.json`, with
   `grouplab corpus counts --write`), approved by planning: 29 of 55 images changed, every one listed old and new in the commit. No
   image lost a real hole: on all 27 punched scans the punched holes found rose or held (by 0 to 5), never above the number punched. On
   the unpunched photographs, where every hole found is spurious, over the six whose count changed the spurious holes fell from 36 to
   16; two rose by 2 (ultrawide3, telephoto2) and four fell, telephoto3 from 17 to 3. The changes are entries 415 and 417's.
   4.2, **the proof checklist.** Two rows the repository's own material could prove: row 12, `grouplab compare-photos`, against its
   proposed gate (it reads gates 1 and 2 on real pictures), by `PhotoComparisonTests.OnAlansOwnScanAndPhotographItReadsBothGates` on
   Alan's 6 ARC dominus-k scan and its Fold 7 photograph from the test-data release (worst bull 0.0057 in, 25 of 25 holes, none false;
   it reads, it does not meet); and row 18, the support link, whose written gate (one browser launch once a page exists) the live
   support page and `OutsideWorldTests.TheSupportButtonAsksForTheSupportPageAndOpensNothing` meet: its README row is now Done, and
   row 12's README line says what it reads, its state left to planning since its gate is only proposed. Every other row still names
   material from Alan, a detection change or an unwritten gate.
   4.3, **speed** (docs/PERFORMANCE.md, "Entry 447 section 4.3"): measured first; the slowest step left in reading a photograph is
   finding its holes, and inside it a large mark's rows were found by testing every pixel from each end against the mark's hull. They
   are now found by halving, with the same ends on every row (`RowInsideTests`): finding the holes on 15 local corpus photographs
   13,260 to 11,044 ms in all, load-sheet-close 1,055 to 702, the bench's phone photograph 172 to 140. Every scoreboard, the counts and
   the identify sweep are the same but for time. Two other tries (stopping the code reading early, whole-number moments) measured
   nothing and were taken back.

## Entry 448: the home page's menu above the wordmark, and everything on it tappable (2026-10-19)

1. **The fix.** While the header's menu is open the header is raised above the docking wordmark (`.tp-home:has(.tp-menu[open])`, and a
   `tp-menu-open` class the menu's own toggle sets for a browser without `:has`), and the wordmark waits where it is until the menu
   closes. The menu now closes on Escape (the focus back on its button), on a tap outside it and on choosing a link (the site script).
   **Also found by the check:** once docked, the big wordmark is invisible but was still on top of the header's small GroupLab link, so
   after scrolling that link could not be tapped at any width; the wordmark now takes no taps (`pointer-events:none`).
2. **The check** (`tests/site/taps.mjs`, run in website.yml before anything publishes, in the runner's own Chrome; Edge here): the built
   site served locally and the home page driven headless at 390, 1440 and 1280 px. With the menu open and the wordmark undocked, every link
   is the element at its left edge, middle and right edge; the menu closes again on its button, Escape, a tap outside and a link; with it
   closed, every link, button and summary on screen is the element at its own middle at scroll 0, 150, 300, 450, 700 and 1000. Before the
   fix it named Guides and Research under `H1.cond` at 390 px, as planning measured, and the header's GroupLab link under it at scroll 300,
   450 and 700 at all three widths: 11 faults. After: none.
3. **Live:** see entry 449's results (checked once this published).

## Entry 449: page titles stay as written (2026-10-19)

1. Alan chose A: every page title as written in the condensed bold face, as built in entry 444; the home page keeps GROUPLAB and START
   MEASURING. in capitals. Nothing on the site changed. The DESIGN NEEDED line is marked answered in for-alan.md and gone from STATE.
2. `heading_problems` now also fails a page, other than the home page, whose title a stylesheet turns to capitals (three lines);
   `site-home-tests.py` holds it (50 checks).
3. **Entry 448 section 3, on the live site** (c00232ec, 2026-10-19): grouplab.org at 390 px in light and dark, the menu opened with the
   wordmark undocked: all ten links are the element at their left edge, middle and right edge, and the panel is drawn over the
   wordmark (`docs/figures/site/entry-448/menu-open-390-light.webp` and `-dark`). The tap check passed in CI on its first run there.

## Entry 450: a flaky test or a slow phone build no longer holds back the other platforms' nightly (2026-10-19)

Sections 1, 2 and 4 were built by a helper in its own worktree (Alan asked for the inbox to be worked in parallel), reviewed and pushed
here as d1c07ea7; section 5's proofs were run here.

1. **One retry of failed tests** (`ci.yml`, all three legs): the test step writes TRX results; when it fails, `scripts/test-retry.py`
   reruns only the tests that failed, once, in the same job, and the leg passes only if every one passes. A crash, a hang, a project
   with no results or more than 30 failures is not retried, and a build failure never reaches it. A test that passed only on its retry
   is a warning on the run, a line in the leg's summary and a row in a `flaky-tests-<leg>` artifact; `scripts/flaky-tests.py` (main's
   runs only) gathers them into `docs/notes/flaky-tests.md`, with a "To fix" list for any test retried three times in a fortnight.
2. **The desktop publish no longer waits** for Android or the arm64 command line: `publish` needs only the naming and the packages; a
   late `android-files` job re-signs the update manifest with the Android downloads and adds the APKs, the AAB and the symbols to the
   build's release and the rolling `nightly` release, and `server-command-line` does the same for the arm64 tarball. Play upload, TestFlight
   and the Store still start only when the whole nightly run has ended, so after the Android files exist. The notes and the release body
   never list files, so a later file does not make them wrong; README, docs/ANDROID.md, docs/UPDATES.md, the download page and the
   `nightly-builds` article say the Android files join when their build finishes. `NightlyNeverWaitsTests` (5) hold it.
3. **Measured** (GitHub keeps runs from 2026-09-15; counted from 2026-09-28, when the signed Android build first ran in the nightly):
   **32 times** a failed "build and test" meant no nightly while the phone builds would have been fine: 20 failed on one or two of the
   three legs (36667907420, 36749517180, 36827714330, 37008809245, 37722546938, 37830239736, 37907966181, 38208601401, 38367496105,
   38391812817, 38398018198, 38526570152, 38686202750, 38694447563, 38998534625, 36369536300, 37002640014, 37199873957, 38136708283,
   38386382784) and 12 only in a job no phone uses (the website job 11 times, the linux tarball's ships check once). No Windows or
   linux-x64 package failed in a nightly. More than two, so said in STATE for planning; the split itself is not built. Also seen: 7
   nightlies failed at "Write the notes" with the Android build fine (36372334414, 36434428741, 36515160656, 36648108371, 37426892451,
   38409950467, 38449280753), and ci.yml's website job gates every platform's nightly and is not helped by the retry.
4. **Proofs** on a throwaway branch (`proof/entry-450`, deleted): with a test that always fails and one that fails only on its first
   attempt (run 39202156149), every leg retried both, the second passed and was recorded (warning and `flaky-tests-<leg>` artifact on all
   three), the first failed twice and the leg failed; with only the second (run 39206563079) every leg passed on its retry and recorded
   it. The real nightly: 0.2.0-nightly.203 (run 39206788728, from d1c07ea7) published the desktop files on its own, and the new Android
   job then failed, because it checked the repository out after downloading the signed files and the checkout emptied them away; no
   Android file reached either release. Fixed in 37326a5c (checkout first, held by
   `NightlyNeverWaitsTests.ALateJobChecksOutBeforeItDownloadsAnything`); the next nightly is the proof that the Android files join.

## Entry 451: the home page's ruler on every page (2026-10-19)

1. **One ruler.** `RULER` in `website/build.py` is the home page's ruler, its twelve numbered ticks written by the build, and both the
   home page and `shell()` use it, so they cannot drift; `page.js` no longer makes ticks of its own and only moves the amber mark, over
   the whole scroll (at the top on a page too short to scroll). It is on every page (`tp-ruled`, which carries the `--rule` margin on
   the header, main and footer), and hidden below 861 px as on the home page. `tp-long` keeps only what long pages do; the numbered section
   index stays the home page's. Nothing is fixed to the right edge in the tour's, how it works' or the download page's own styles.
2. **Held:** `header_problems` fails the build for a page without the ruler; `site-home-tests.py` (50) holds that a short page has it
   and no section index; the tap check (`tests/site/taps.mjs`) still finds nothing covered.
3. **Pictures** in `docs/figures/site/entry-451/`: download, tour, tour/how-it-works, targets and the guides index at 1400 px, light and
   dark, and targets at 390 px with the ruler hidden. Live check: below, once published.

## Entry 454: a phone updates to the newest nightly that has an Android file (2026-10-19)

1. **Nightly 203 repaired.** Its signed Android files were still on run 39206788728 (until 2026-10-20 06:00 UTC). The Android job's
   gathering, signing and uploading is now one script, `scripts/nightly-android-files.sh`, run by nightly.yml and by a new dispatchable
   `android-files.yml` that takes a nightly run and its version (26be6361). Run 39223723577 added the APKs, the AAB and the symbols to
   `v0.2.0-nightly.203` and the moving release and signed the manifest again: both formats now list `apk-dev` and `apk` for 203. A phone
   on 202 or earlier is offered 0.2.0-nightly.203. Rerun once this change is on main, so 203's manifest also carries the new per-platform
   list that 204 carries forward.
2. **Each platform follows its own newest build.** The second-format manifest gains `platforms`: per platform, the newest build with a
   file for it, its commit and date, and its files on that build's own numbered release. `scripts/nightly-manifest.sh` passes this
   build's numbered files (`--own`) and the manifest before it (`--carry`, this build's own when the Android job signs again, the moving
   release's when the publish job signs first); a platform this build has no file for keeps the earlier entry when it is an older build,
   and a manifest before that does not verify with the key carries nothing. The first format is unchanged (`Legacy()` strips the list
   and the command line refuses to sign it there).
3. **Both updaters compare with their platform's build**: `UpdateManifest.OnPlatform`, used by `UpdatePolicy.Decide`, `UpdateRun` (the
   desktop passes its platform) and `AndroidUpdates.CheckAsync`. Equal or older is never offered.
4. **Older builds:** they ignore the new field (the second format is verified over the bytes as written), and the top of the manifest
   lists an Android file only when it belongs to the top build, so an older phone is never handed an older APK under a newer number and
   cannot loop; it waits, as before, for a nightly with its Android file. `NewestForEachPlatformTests` (8): no Android file, a late one,
   a phone on the build before, one on the newest Android build, a manifest with no Android build at all, an older build's record
   reading the new manifest, the desktop for its own platform, and the carrying rules. Core suite 3040 passed.
5. **Words:** "You have the newest GroupLab Dev for Android, 0.2.0-nightly.202." (`AndroidUpdates.Newest`), shown by the phone for
   nothing newer and for a manifest that names no Android build; no other platform's number.
6. docs/UPDATES.md, docs/ANDROID.md, the nightly-builds article and the download page say so (written by the docs helper, reviewed);
   the Android updates feature claims the note.
7. **Proof, a dry run** with a throwaway key and stand-in files through the real command line: 203 signed with Android, then 204 with
   the Android build failed: 204's Android entry stayed on 203 (`v0.2.0-nightly.203`'s own file), `update-check --as android
   0.2.0-nightly.202` was offered 203, a copy on 203 nothing, Windows on 203 was offered 204, and the first format carried no new field.
   Alan's tablet should now reach **0.2.0-nightly.203**; the first real nightly after this change is the live proof.
   Live: nightly 204 (run 39231460564) was signed first with Android's entry carried from 203, then moved to 204 when its Android
   files arrived three minutes later; a phone on 203 is offered 204.

## Entry 452: the website check and a refused note stop costing nightlies (2026-10-19)

1. **The website job no longer gates the nightly.** It moved out of ci.yml ("build and test", whose success starts the nightly) into
   `.github/workflows/checks.yml` ("checks no nightly waits for"), under the same name, so it still runs and fails on every push and pull
   request; website.yml's own gate is unchanged. "build and test" now holds the three test legs (each retrying a failed test once, entry
   450) and the jobs that build what ships: the Windows package, the linux tarball, the intake worker and the test data.
2. **The linux tarball's ships check** (`shipping-gate.py --check-generated`) compares the list of what ships with what the build reads.
   That list decides whether a nightly is built; it is not what the Linux download contains. So it moved to checks.yml as "ships list".
   The tarball job itself, which packs the Linux publish and checks it carries its native OpenCV library, stays a gate.
3. **"Write the notes".** All seven runs (36372334414, 36434428741, 36515160656, 36648108371, 37426892451, 38409950467, 38449280753)
   failed for one cause: the checker refused a trailer ('folded', "did not change", 'TestFlight' five times, 'millimetres', 'centres'),
   and it ran only after every build had finished. Fixed at the cause: checks.yml's "release notes" job runs `release-notes.py --check` on
   every push, so a refused note is red on its own commit and is put right in `docs/release-note-corrections.json`. And if one still
   reaches the nightly, it publishes with `release-notes.py --fallback` (the version, the commit, a link to the list of changes since the
   build before) and a warning annotation and summary; `docs/RELEASE-NOTES.md` takes the notes the build went out with
   (`notes-written.md`) instead of asking the checker again. `NightlyNeverWaitsTests` (8) holds both.
4. docs/UPDATES.md and the nightly-builds article say what decides a nightly and what happens to a refused note (written by the docs
   helper, corrected: 12 nightlies were lost to the website job and the ships list alone, not 32).
5. **Proofs.** Throwaway branch `proof/entry-452` (deleted): with the site build made to fail, "checks no nightly waits for" failed on
   "website builds" (run 39226777937) while "build and test", the nightly's trigger, passed on all three legs and every build job (run
   39226778099). The notes fallback, as a dry run of the nightly's own step on a commit carrying a refused note ('centres'): it exited 0,
   wrote the short notes, an empty notes history, the warning and the summary, and `--check` named the same commit. No release was made.
6. **The first nightly after 37326a5c**, 0.2.0-nightly.204 (run 39231460564): the desktop files were published at 09:44:37 UTC with
   Android's entry carried from 203, and the Android files joined both releases at 09:47:36, Android's entry moving to 204.

## Entry 453: a thumbnail for every research article (2026-10-19)

Alan chose concept C: keep the charts, give every other article its own picture. Built by a helper in its own worktree (Alan asked for
the inbox to be worked in parallel), reviewed here picture by picture and taken as 7e4b1719. Website only.

1. The 15 lead figures are unchanged. The build agreed with the entry's list of 21 plates.
2. Every plate is gone. Each of the 21 has `website/research/<slug>/thumb.webp` (a real picture) or `thumb.svg` (a drawn mark),
   inlined on `var(--sunk)` in the 320 by 200 box, aria-hidden, colored only through the entry's eight classes. `research_lead` stops
   the build on a published article with neither a lead figure nor a thumbnail; `thumb_problems` checks every thumbnail (the box, the
   classes, no words or fixed colors, one solid amber element at most, a picture WebP 640 by 400 with no EXIF, XMP or ICC).
3. Of the four canvas marks, smaller-installer is used as given; real pictures replaced one-hole-or-two, hole-is-not-the-bullet and
   wrong-bull, as section 8 allows. The canvas marks carry dashed amber guides, so the rule counts solid amber elements only.
4. Drawn marks (7): safe-updates, what-grouplab-sends, nightly-builds, smaller-installer, uploads-rebuilt-from-pixels, shots-to-zero (a
   calculation with no sheet behind it) and bent-sheet-registration (its own pictures are kept off the site).
5. `research-plate` and its CSS are removed.
6. Proof screenshots of the whole Research page at 1440 px light and dark and at 390 px are in `C:\Dev\grouplab-local\research-thumbs\`,
   with `which-is-which.txt`. Live check: below.
7. SCREENSHOTS.md and the site notes never described the plates. Nine card keys in docs/claims-backing.json were renamed with the
   card text.
8. Real pictures (14), crops of Alan's own scans and photographs from the 2026-09-20 range trip, the 2026-09-26 aim point card, the
   2026-10-04 corner bracket photograph and the 2026-10-08 M834 check print, each turned upright and rebuilt from pixels with no
   metadata; sources, sizes and SHA-256 in samples/PROVENANCE.md, "Entry 453". Checked here: nothing in any crop shows a name, an
   address, a serial or a QR code; nothing is a tester's, from Discord or the 2026-09-16 friend scan. Two no_figure lines the pictures
   made untrue were corrected (primer-comparison, how-far-off-square).
9. Real pictures: one-hole-or-two, hole-is-not-the-bullet, blank-sheet-zero, scale-markers-beside-a-target, how-far-off-square,
   how-grouplab-reads-a-target, wrong-bull, scans-against-photos, pooling-groups, wind-or-rifle, primer-comparison, choosing-the-markers,
   designing-a-readable-target, phomemo-m834-print-commands. Drawn: the seven in section 4. Lead charts: the other fifteen.

Checks: the site build, the four site suites, the tap check, claims (all backed), readme and consistency pass. Worth an article? No: a
presentation change with no measurement in it.

## Entry 455: maximum point blank range, a third view in Ballistics (2026-10-19)

1. **The model** (`src/GroupLab.Core/Ballistics/PointBlank.cs`): the trajectory flown once from the rifle's zero, every other zero the
   same flight tilted; the chance of a hit at each distance from the mean radius (per-axis sigma, MR = sigma times the root of pi/2,
   growing with distance) and the velocity SD carried through the solver as a vertical spread, aimed at the zone's center; a circle by
   48-point Gauss-Legendre over the part of it the normal has weight in, a rectangle as two intervals. Classic MPBR for reference, the
   certainty band, the three ways to set the zero (longest dead-on, most likely distance, a window, with the refusal when no zero holds the
   window) and the rifle's present zero beside them; what ends it (certainty, an energy floor, the bullet's least expansion velocity,
   each named); 100 % refused in words.
2. **Against planning's numbers** (Test load, 8 in circle, 0.63 MOA): classic start 0, near zero 25 yd (26), peak +4.00 in at 148 yd,
   far zero 266 (266), end 313 (312), +3.38 in at 100; 90 % 0 to 263 yd (262) at +2.60 in, 95 % 251 (252), 99 % 228 (228), 99.9 % 201
   (200); the 100 yd zero 0 to 207 yd by the classic rule (208); most likely 200 yd +1.76 in (1.77), 99.93 % at 200, band 0 to 237 (236);
   window 150 to 300 best worst case 58.8 % (56). Prairie dog starts at 15.5 yd (about 16). All within 2 yd. `PointBlankTests` (15).
3. **The words, the same everywhere** (`PointBlankReport`): "Dead on 0 to 263 yd", "9 in 10 shots in an 8 in circle", "Zero 2.6 in high at
   100 yd: 0.72 mil, 7 clicks up from your 100 yd zero.", the ending, the wind's drift at the far end with a warning past half the zone's
   width, the present zero, the classic figure, the law line for hunting targets, what is not counted.
4. **The targets** (`PointBlankTargets`): the eleven sourced game zones (the smaller figure where sources disagree), five estimates
   labelled as such and editable, the IPSC A zone, the USPSA head A zone, the IDPA body, two steel silhouettes said to be a vendor's
   shape, your own circle or rectangle; drawn to scale in the list. Energy floors 1000 and 1500 ft lb as rules of thumb.
5. **The computer** (`MainWindow.PointBlank.cs`): the third view with its settings on the left, the answer card, a picture of the path
   with the zone and the dead-on stretch, Print a card (a 203 dpi picture and a PDF), Keep this zero on the rifle and Put back.
   **The phone**: Ballistics' Point blank tab, the same inputs and words, the card shared or printed on the M220 over Bluetooth (when
   50 by 30 mm labels are loaded), keep and put back. `Entry455Tests` on both (3 and 1).
6. **The card** (`PointBlankCard`): 50 by 30 mm, six lines, nothing round; `TheCardIsFiftyByThirtyAndEveryLineFits`.
7. **Records, schema version 4**: a load's least expansion velocity (Equipment, Ballistics' load section, the phone's load form, the
   export) and a rifle's previous zeros (kept as text so equal histories compare equal), with the upgrade, docs/SESSION-SCHEMA.md and
   the export accepting versions 2 to 4.
8. **IPSC in Hit probability** is now the official A zone, 15 by 32.5 cm; GroupLab's own outline is gone. The rest of the outline is
   question 102.
9. **Docs** (docs helper, reviewed): the user guide's Point blank range part with From the keyboard and the sources, its PDF, the
   Features entry with its picture (`ballistics-point`), the tour, README, and the research article "Point blank range, honestly",
   published under entry 393's rule (every figure held by `PointBlankTests.TheArticlesFiguresHold`). Worth an article? Yes, written:
   the classic zero keeps 46 % of shots in the circle at its peak and 42 % at its far end.
10. **Alan's proof**: request 104, the card on his M220 once a nightly carries it. Request 103 (entry 456's Top Gun question) was
   written at the same time.

## Entry 456: rail cant, sight scale factor and Top Gun, from Alan's pissinhot.com calculators (2026-10-19)

The formulas are the entry's own account of the three pages. Reading the pages here: the safety check refused the fetch of the scope cant
page, and the other two came back as page text without their scripts; the entry's formulas were complete, so nothing waited on it.

1. **Rail cant** (`ScopeMount`, the rifle page's Scope and mount section): the elevation the zero uses is GroupLab's zero launch angle
   (the one deliberate difference: his page adds a vacuum drop angle; for the Test load at 100 yd the page's formula gives 4.053 MOA and
   the solver 4.012), half the travel (or the most up from the zero plus the zero's, his second mode), the most rail cant that still
   zeroes, the elevation left above the zero with the rail's cant and the farthest range it reaches with a chosen load (1550 yd for the
   screenshot rifle, 30 mil and a 6 mil rail), and the plain refusal when the rail is canted past what zeroes. Ballistics uses it: the
   Trajectory table's clicks say "out of elevation, hold X mil in the reticle" past the last dialable range, At one range says the same,
   and Point blank says when its zero needs more than is left; the phone's dope table too.
2. **Sight scale factor**: a rifle field and a tracking-test helper (expected 20.0 mil or 66.0 MOA, the turret's reading, the factor,
   the error, the band: under 0.5 % essentially perfect, under 2 % normal, 2 % or more significant, and the steps). Every click is the true
   angle over the factor: `Clicks.For` (Trajectory, At one range, zero carried, Point blank and its card), the analysis's `FourUnits.Clicks`,
   Shots Needed to Zero (`SigmaClicks`), the phone's dope table; "SSF 0.980 applied" said once beside them. **Later idea, not built**:
   tracking measured from a scanned tracking target, which at 20 mil and 100 yd is 72 in tall (tiled pages or a plotter); in STATE.
3. **Top Gun**: a rifle field, weight as fired; the figure as Litz states it on the rifle page for each load, in the analysis's advanced
   figures (computer and phone) and in Compare loads (both), never coloured or worded as a pass or a fail. Not converted to mean radius and
   not compared with GroupLab's figures: `TopGun.MetricConfirmed` is false until request 103 is answered, and the "clearly worse than the
   trend" pointer (`TopGun.WorseThanTrend`, no pseudoscience) waits on it too. The combined dispersion table is left out.
4. **Records, schema version 5**: `scope_travel`, `scope_travel_kind`, `rail_cant`, `sight_scale_factor`, `weight_as_fired_lb` on rifles,
   through `EquipmentForm` (with a Section heading for Scope and mount), the record book file, the export and import (versions 2 to 5), and
   the phone's rifle form. A desktop rifle save now keeps the rifle's previous zeros (entry 455), which its form does not show.
5. **Phone parity**: the same fields, figures, tracking helper and Top Gun in Ballistics' rifle form; the dope's holds; Top Gun in the
   result and Compare. **Docs** (docs helper, reviewed): the guide's Scope and mount part with the credits (pissinhot.com, Bryan Litz), its
   PDF, the Features entry with its picture (`equipment-scope`), the tour, README, PHONE-PARITY.
6. **Tests**: `ScopeMountTests` (7: the page's worked example against the solver's angle, both travel kinds, the refusal, the hold, the
   tracking bands, the factor in every click count, Top Gun's arithmetic), `Entry456Tests` on the computer (2) and the phone (1), and
   the equipment form's round trip with every new field.

## Entry 457: the Next unsure hole error, a Send button on every report, and error reports triaged as they arrive (2026-10-19)

1. **The error** (issue 37, nightly 204 on Windows, display scale 1.5, four times in one session; Alan's logs read on this computer):
   planning's reading was right. The focus keeping (`KeyboardFocus.Keeper`) recorded place -1 for a control whose visual parent is a panel
   but which is not one of its Children, and `Restore` read `Children[-1]` after Next unsure hole and after the autosave. A place is now
   recorded only when it is one and read clamped, and `Restore` never throws (one warning in the log). The unobserved-task copies came
   from Avalonia faulting the posted operation's own task as well as raising the error, so a `Restore` that cannot throw ends both; that
   is also issue 9's return on nightly 204. Issue 36 (GroupLab Dev on Android, a tap on the result's sheet near nothing) was MinBy over
   none; it now finds none. Fixed in 0af39fc2, shipping in the nightly after 205; `Entry457Tests` on both reproduce each fault on the
   code before and pass now (the first version of the desktop test passed on the old code too, and was rewritten until it did not).
2. **Send on every report**: the report window's first button is Send whenever the error receiver is open; it goes through the same
   receivers as the automatic error reports (`ErrorReports.SendDiagnosticsAsync`, now with the report's own error and its run's logs),
   with what the person wrote; a contact stays in a saved report only. The email and GitHub sentence shows only when sending is not open
   or a send fails, and says why. `crashReportUrl` and its uploader are retired. The crash banner, asked each time, has Send the error
   report first and primary; sending automatically, it says the report has gone or goes within a minute. The phone, asked each time, puts
   the same offer in the note along the top. `Entry457Tests.TheReportWindowSendsFirstAndTheWayRoundIsOnlyTheFallback`.
3. **Triaged as they arrive**: CLAUDE.md now lists the open issues before each inbox entry, before ending a turn and at least hourly
   while waiting on CI, and assesses each at once. `docs/notes/error-reports.md` holds issues 37, 36 and 9 (37 also Alan's own report);
   STATE's crash line points to it. The three issues close when the nightly with 0af39fc2 is out.
4. **Docs**: the guide's Report a problem part, the phone's part, the error reports feature's note and the support page say a report can
   be sent from GroupLab, with GitHub and email as the fallback.

## Entry 458: release notes say what people's reports and targets changed (2026-10-19)

1. **Where a fix's report came from**: `scripts/release-notes.py` reads `Reported-via:` (automatic error report, error report sent from
   GroupLab, GitHub issue, email, Discord, with counts) and puts it on the fix's line, "(found from 5 automatic error reports)"; any other
   word, a name among them, is refused. A commit that closes a crash report issue without it fails the release notes check on every push.
   Entry 457's fix (0af39fc2) already carries it, so the next nightly's notes say "found from 5 automatic error reports". People who
   agreed to be named (Quack, Jylee, Unholy, Fenix, Justin) stay in the commit's prose only; the trailer never carries a name.
2. **Detection changes from targets**: `From-submissions:` says how many targets people sent went into a change ("made with 48 targets
   people sent" on its line). The learning loop writes its commit's note from its own result (`learn.py release_note`): the targets and
   where they came from, how many held back, how many more holes and fewer false marks on those (public totals only, from the tuning
   result's new numeric fields), and what changed in plain words from `KNOB_WORDS`, never a constant's name; no change, no commit. The app
   now records one word with each target, automatic or by hand (`TargetPackages.HowSent`), and the receiver keeps it in `meta.json` beside
   `source` (app or web); targets from before say "people sent" without the split.
3. **The running total**: each build's notes end with "From your reports and targets in this build: ..." when there is any; the releases
   page has "What your reports and targets changed" (`/releases/#from-you`), the README's What is new carries the total once a build has
   counted, the Support and Send a target pages link to it, and the Discord announcement carries the build's line with its notes.
4. **Checks and docs**: the notes self-test covers both trailers, the counts, the total, a name refused and the crash report rule;
   `SendEverythingTests.APackageSaysHowItWasSentInOneWord`. CONTRIBUTING.md says how to write the trailers and the nightly-builds article
   mentions the counts (docs helper, reviewed).

## Entry 459: consistency audit of 2026-10-19 (2026-10-19)

Wording by the docs helper (docs-sonnet), reviewed; sections 6, 7 and 11's last bullet on the main model.

1. **Trying GroupLab** (TESTING-GUIDE and its PDF) says to write what happened and press Send; Save only writes a zip; GitHub or email
   only when sending is not open or fails.
2. **The platform statement** (PLATFORM-SUPPORT.md, so README and /download/): reports are sent from GroupLab with Send, the Discord
   bug-reports forum works too, support@grouplab.org for anything private.
3. **Phones and the log**: the user guide now says what the code does (`SettingsView` offers "Send them with the log" behind the same
   switch as the computer).
4. **Store-bought targets**: the user guide names the one route that exists, email to support@grouplab.org (the Send a target page
   takes images only and the app only saves the file); "no way to send one in yet" retired.
5. **Home page**: only the Windows and Linux downloads are called unsigned.
6. **Website notes in the application's notes**: nightly 205's entry now says it changed nothing in the application (only the consent
   record beside the Windows sample is longer), and 206's bullet says what the application does (a sent target records how it was sent),
   both also in `docs/release-note-corrections.json`. The cause: `samples/PROVENANCE.md` ships in the Windows zip, so entry 453's
   consent records for the research pictures made 205 a real build whose only note was the website one. Question 105 proposes shipping
   only the sample's own record.
7. **The site a nightly behind**: published by hand for 207 (live at f094544b with 209). The cause: the nightly's notes and README pushes
   use its own token, which starts no workflow, so the paths filter never saw them. The publish job now dispatches website.yml after the
   README (`actions: write`); `WebsiteWorkflowTests.NoOtherWorkflowStartsTheWebsiteWorkflow` allows exactly that one step, and
   `NightlyNeverWaitsTests.TheNightlyPublishesTheSiteAfterItsNotes` holds it.
8. **Not built yet**: both README lists name hole detection on plain paper, in plain words.
9. **Phases 6 and 8**: one sentence under README's table says why a phase whose gate is met stays In progress.
10. **Samples**: the Windows zip and installer carry the two sheets (the installer is built from the same staging folder); macOS, Linux
    and the phones do not; README, /download/ and TESTING-GUIDE say so.
11. Tour Ballistics blurb names Point blank; the Error reports card names Send (reworded since 207); the phone lists name Point blank;
    for-alan.md's first line rewritten from the open requests, and request 104 corrected to nightly 206 (Point blank is not in 205).

## Questions 102 to 105, answered by Alan in the session (2026-10-19)

- **102**: keep the A zone only. Hit probability's IPSC choice stays the official 15 by 32.5 cm A zone; no other zones are added.
- **103**: keep the credit as it is. The guide and README say the three calculators began as ones GroupLab's developer wrote before
  GroupLab, and the old site is not named.
- **104**: keep it. The Real taps check goes on waiting out a measured pause in the simulator's drawing, and saying so.
- **105**: yes. The Windows package now carries only the sample's own consent, `samples/sample-consent.md`, under the name
  `samples/PROVENANCE.md` it always had in the zip. That section is a committed file rather than one cut out at packaging time,
  because the shipping gate decides by path: `samples/PROVENANCE.md` left the ships list and `samples/sample-consent.md` joined it,
  so a change to the website's consents no longer makes a build. `ReleaseAssetTests.ThePackageCarriesItsLicenceItsNoticesAndItsSamples`
  holds the file to the full record's first section word for word, and `ShippingPathsTests` holds which of the two ships.

## Request 103, answered by Alan: Top Gun is five-shot extreme spread, and groups are now set beside it (2026-10-20)

- **The answer**: Litz's Top Gun group size (Modern Advancements in Long Range Shooting, Volume III, chapter 3) is the extreme spread
  of five-shot groups, averaged. Request 103 is closed and archived in `docs/notes/for-alan-archive.md`.
- **The comparison**: `TopGun.OnItsBasis` gives a group's own figure on that basis, the average five-shot extreme spread its sigma
  implies: the range-statistic table's five-shot mean (3.066 at sigma 1) times the group's Rayleigh sigma, in MOA at the distance, with
  the sigma's interval carried through. So a 20 or 50 shot session is compared fairly, not by cutting it into fives. `TopGun.Compared`
  says "clearly wider" (with entry 456's list of what is usually checked) or "clearly tighter" only where the whole interval is on one
  side of the trend, and otherwise "not clearly different at this many shots". No distance, or under five shots, and no line is shown.
- **Where**: the analysis's Advanced figures on the computer and the phone (`ResultFigures`, key `topGunYours`; `MainWindow`), and
  Compare loads on both, each group under its load's trend (`TopGun.Lines`, now given the groups and the distance).
- **Proof**: `ScopeMountTests.YourGroupOnTopGunsBasisIsTheAverageFiveShotSpread` draws 40,000 five-shot groups at sigma 0.5 in at 100 yd
  and their average spread (1.4668 MOA) lands within 0.01 MOA of the figure on its basis (1.4639); `ResultFiguresTests.AdvancedSetsTheGroupBesideTopGunOnItsBasis`.
- The guide, README and the Features sentence say it; `TopGun.MetricConfirmed` is gone, and every "average group" now reads "average
  five-shot group".

## Entry 447 part 2, what was left: six windows and the phones' deeper pages (2026-10-20)

- **Desktop, headless** (`Entry447WindowsTests`, one test a window): importing shot coordinates, Which target is this?, Fudd buster,
  the three group tools (Shots and clicks, Zero from this group, Share a picture), the print confirmation, and the figures in their own
  window, each tabbed all the way round: the focus never lost, something taking it, every stop named as a screen reader reads it.
- **Found and fixed**: the coordinates window's four drop downs and its distance box had no names (now named by the words beside them,
  the box as "Shot at, in yards, needed for mil or MOA"); Fudd buster had nothing that could take the focus, so a keyboard could not
  scroll it and Tab lost the focus (its page now takes the focus, named, and the arrows and Page Down read down it). The other four
  had no fault.
- **Phones, headless at Android's largest text** (`Entry447Tests` on the phone, extended): Fix holes and the readings page from the
  result, the store-bought target's fingerprint from Targets, and the printer check from Settings: every control named, and the
  screen read before the bar. No fault.
- **Still left**: a run with a real screen reader (Narrator, VoiceOver, TalkBack) on each system; that needs Alan's devices, and the
  headless tests stand in until then.

## Phase 9, 2026-10-20: finding the holes on a 600 dpi scan about a fifth sooner, every result the same

- **Measured first**: the slowest step left in reading was finding the holes on the 600 dpi scan (about 490 ms in the bench); inside it,
  besides the alignment (which entry 433 showed cannot be cheaper with the same shifts), the open of the grey residual and the threshold
  after it (85 ms) and setting everything beyond the sheet to paper on one thread (37 ms).
- **The change**: the residual is thresholded and then opened as a 0 and 1 image (`BinaryMorphology.Open`, taken by the imaging backend
  for any 0 and 1 open), which for a flat element gives the same pixels as opening and then thresholding; the pass beyond the sheet runs
  on every core. `BinaryMorphologyTests.ThresholdingThenOpeningIsOpeningThenThresholdingToTheByte` holds it to the byte against OpenCV.
- **Before and after**: the bench's 600 dpi scan, finding the holes, 492 to 378 ms; a phone photograph 143 to 127; `grouplab analyze` on
  the 25 hole 600 dpi sample 1,134 to 1,024. Detail in docs/PERFORMANCE.md.
- **Nothing moved**: the four local corpus scoreboards, the synthetic and any-target scoreboards and both corpus counts are line for line
  the same but for their time columns.

## Entry 460: questions 100 and 98 answered by Alan from the planning canvas (2026-10-20)

1. **Question 100, option B.** The dark themes' selection is now #f800f8 (`Tokens.Selection`), 108 or more from every plot ink and bull
   mark where #ff3df5 was 84 from the violet bull mark; edged in black, 6.3 to 1. The light theme's #b0009f and its fourth pooled
   sheet's #9c3f8f are kept. `Entry425Tests` now holds the dark selection 100 from the bull marks and the light one at 80, and the light
   plum pair at 65, as answered. No guide, page or release note named the old value. Question 100 archived.
2. **Question 98, option B.** Two or more pictures of the same single-page design dropped together (codes read, no set sheet number) are
   sorted as copies (`DropSorting.Copies`) and asked about once with Alan's words (`CopiesQuestion`): "Pool these 3 pictures as one
   group?", "They are three copies of the same page, GroupLab 3x3 2 MOA, Letter. ...", **Pool as one group** (the default) or **Keep a
   tab each**. Pooled, they open in one tab named "GroupLab 3x3 2 MOA, Letter, 3 copies pooled" and pool as pictures with no codes do
   (`SetPool.CompositeOfPictures`), but each is read and its holes found as it opens, since its codes read (`SetGroup.CopiesOf`); a later
   copy dropped onto that tab joins it. Kept, a tab each as before. On the phone, copies chosen together are asked the same question
   (`CapturePage.Copies`, `CapturePage.PoolCopies`) and pooled open as one group on the result screen. Tests: `Entry460Tests` (sorting,
   the words, a drop of three 2 MOA copies with both answers) and `Entry460PhoneTests`; the entry 423 drop tests still pass. The guide's
   drop paragraph says it. Question 98 archived.

## Entry 461 part 1: every research row that said "not yet" decided (2026-10-21)

Ten rows of `docs/RESEARCH.md`'s "Worth an article?", each checked for whether its awaited data now exists:

- **Written and published (two new articles and one update):** `/research/recognizing-store-bought-targets/` (entry 332 and question
  87, decided on what exists as entry 341 says: the scale is measured only against the learned sheet, and the article shows the
  warning a shooter checks; no maker's artwork); `/research/opencv-on-android-from-dotnet/` (entry 198: the build, its two traps and
  the Fold 7's first run); and two new sections of `/research/wrong-bull/` (entries 374 and 376 Part A: a row higher fits exactly as
  well, the shooting order breaks the tie, and a real tie is asked). Both new articles have a drawn thumbnail, are listed in
  `website/research/PUBLISHED.md`, and are backed by rules in `docs/claims-backing.json`; the research card's sentence is backed alone.
- **Not written, with the reason in the row:** entry 410 section 1 (already decided twice since, entries 415 and 434), entry 362
  (five photographs on one counter; request 74 never came), entry 261 (still synthetic), entries 196 and 197 (synthetic; the only real
  sheet is the orange commercial target), entry 344 (no real photo of a real poster), and entry 158 program A step 2 (the orange
  commercial target, and a scan detector on photographs).
- Checks: the site builds; `claims.py --check` all backed; consistency 0 findings; README checked.

## Entry 461 part 2.1: screen reader audits on all three platforms (2026-10-21)

- **The computer** (`tests/Shared/ScreenReaderAudit.cs`): Avalonia hands Windows UI Automation its automation peers, so the peer tree
  is what Narrator reads. Every window the entry 447 tests tab round (the eight places, the report, the printer check and the six
  windows of `Entry447WindowsTests`) is walked: every control a person presses, types into or chooses, shown and enabled, must be in
  the control view (not Raw), carry a name in words, a control type that says what it is, and be reached by Tab (a stop, or inside a
  stop that takes it with the arrows). `Entry461AccessibilityTests` holds the audit to a window made to fail it.
- **What it found and was fixed:** a button or choice holding a panel was named by Avalonia after the panel's type, so Narrator read
  the printer check's four ways to print as "Avalonia.Controls.StackPanel" and three marking buttons as "Avalonia.Controls.DockPanel";
  the old tab round missed it because the name was not empty. `SpokenNames` (desktop and phone) names each such control from the words
  inside it unless the code named it, and keeps it current when the content changes; a type-shaped name now counts as no name in both
  checks. On the phone, Settings' "Run the benchmark now" was 44 tall, now 48 (`BenchmarkPanel`'s `runHeight`).
- **The phones, headless** (`Entry461PhoneAccessibilityTests`): the first run and every place, the same walk with a 48 by 48 touch
  target: no other finding.
- **The phones, on the platforms' own trees:** a scenario step `audit` (`Scenario.Audit`) stops on each of the sweep's 13 screens
  (`scripts/scenarios/phone-audit.json`, held to the sweep by `ScreenReaderAuditTests`); `scripts/screen-reader-audit.py` reads
  Android's accessibility node tree with uiautomator (every node that can be tapped named, with a role, 48 by 48 dp, and every control
  GroupLab Dev says can be tapped present in the tree) and runs XCTest's `performAccessibilityAudit` through the XCUITest runner
  (element detection, hit regions, descriptions and traits judged; contrast, Dynamic Type and clipped text kept in the artifact). New
  steps "Screen reader audit" in android-emulator.yml (`scripts/android-audit.sh`) and ios-app.yml.
- **The first runs (39539794514, 39539797209).** Android read all 13 screens: every node named, every role given, and two findings
  that are GroupLab's, "More ▸" 47 dp wide (now at least as wide as tall, `MoreFold`) and the bull buttons of "How you shot this sheet"
  44 dp tall (now 48, their gap 1 where it was 3, so the grid keeps its height). The rest was one fault: **every control's box in
  Android's accessibility tree sat 128 pixels, the status bar, above where it is drawn**, because Avalonia 12.1.2's helper sets each
  box on the screen from the control's place in GroupLab's view and never adds where the view is (it calls `SetBoundsInScreen` and never
  `GetLocationOnScreen`); TalkBack's outline was drawn above what it read, and Android cut the top and bottom controls to slivers.
  `AccessibilityOnScreen` wraps Avalonia's helper on the Android head, answering exactly as it does and moving each box by the view's
  place before Android clips it. A scroll bar's own arrows are left out of the judging, as on the computer. iOS: the runner did not
  build, because XCTest has no audit types for actions or parent and child; they were taken out.
- **The second runs (39544356145, 39544358247).** Android: the wrapper installed (`accessibility.bounds installed=True` in GroupLab Dev's
  log) and the 100 misplaced boxes were gone; the three left were controls cut off by the bottom of the scrolled view, whose box is the
  sliver showing, now not judged for size, as the Accessibility Test Framework does not. iOS read all 13 screens: 20 issues. Fixed:
  empty lines of text (a status line with nothing to say yet) were read as elements with no description, now kept out of the tree while
  empty (`SpokenNames.Quiet`); "grouplab.org" alone was not a name a person can say, now "GroupLab's website, grouplab.org". Not
  judged, kept in the artifact: the scroll bar, a 16 point strip down the right edge, and text XCTest found inside the photograph of
  the sheet on Fix holes (its handwriting and printed labels), which no label can represent and the words above it say.
- Request 105 in for-alan.md: a later twenty minutes with a real screen reader, for what a machine cannot judge. The guide's
  keyboard part says what is checked and that no person using a screen reader has tried it yet.

## Entry 461 part 2.2: a phone photograph's codes, measured (2026-10-21)

- The bench's photograph reads its codes square on in 662 to 861 ms (`grouplab analyze`, three fresh runs): the marker search 68 to
  92 ms, placing and cutting out the 8 places about 75, and about 450 ms in OpenCV's two readers failing in full on the 6 places with no
  code (WeChat 44 to 68 ms each, the second reader 5 to 35), where the other sheets sharing the frozen sheet's markers print theirs.
- **Nothing changed:** a failing read is the reader's fixed sequence, and reading fewer places changes how many codes a picture is said
  to have read (entry 415's finding). Question 106 puts the one real option to planning, with what it would cost; the bench baseline is
  unchanged. Detail in docs/PERFORMANCE.md. QR codes are still read on one thread.
- Worth an article? Not written: ordinary engineering, and the table is in PERFORMANCE.md.

## Entry 461 part 3: synchronization through the person's own cloud folder, option A, the engine (2026-10-21)

- **Decided:** option A of docs/notes/sync-options.md (Alan, 2026-10-20); recorded there, in DESIGN.md section 18 and the decision log.
  Option B is not decided and not built.
- **Revisions (3.1 item 1):** schema 6 adds `sync_records`, `sync_seen`, `sync_conflicts` and `sync_kept` (docs/SESSION-SCHEMA.md); the
  first open of the new build copies `grouplab.db` to `grouplab.pre-v5.db` before upgrading, as every schema change does. Each record
  the export carries has a version vector (`VersionVector`); a device finds its own changes by each record's SHA-256 at every run.
- **Change files (item 2):** whole records with their identity (`DataExport.Identity` for a session) and revision, in
  `GroupLab sync/changes/<device>/`, written only by that device, beside their name and renamed into place, at most 25 sessions a file,
  ending `"end": true`.
- **Reading and merging (item 3):** `SyncEngine.Run`, at start and every five minutes once a screen calls it (`SyncEngine.Every`);
  newer wins, the same record changed on two devices is kept both ways and listed, a deletion against a change keeps the change and
  lists it on both, deletion markers kept 90 days in the database and always in the change files. `SyncEngine.Resolve`: keep this
  one, keep the other, keep both (sessions), understood. Every record a merge replaces or takes away is kept whole first (`Kept`).
- **Tiers (item 4):** geometry and the proof picture always; the originals only with `SyncOptions.Originals`, to
  `GroupLab sync/originals/<sha256>.<ext>`; the photograph's path on a device never travels.
- **Reaching the folder (item 5):** `DirectorySyncFolder` (the computer, and iOS once a bookmark has opened it), `SafSyncFolder`
  (Android's folder picker, the Storage Access Framework, with its permission kept), `BookmarkSyncFolder` (a security-scoped bookmark
  to a folder in Files). The two phone ones build in CI and are reached by nothing.
- **Tests (item 6):** `SyncEngineTests`, twelve: two devices (a session with its strings, a rifle, a sheet, a setting), three devices,
  the same session changed on two (listed both sides, chosen, settled everywhere), keep both, a deletion against an edit, a half-written
  change file read once whole, a device back after 200 days (the deletion still reaches it), a folder that disappears (nothing changes),
  nothing lost (a load and a sheet kept as they were), originals only when asked, each device only its own files.
- **`grouplab sync <database> <folder>`**, with `--list` and `--choose`, for testing. Nothing on any screen, setting or menu; the
  README and the site say nothing of sync.
- **Measured for the screens (3.2):** on this computer's 22 sessions, a proof picture's median is 226 KB, a marking's 38 KB and a
  design's 7 KB: about 350 KB a target in the folder, 70 MB for a year of 200. The DESIGN NEEDED in for-alan.md carries the figures.
- **Not built:** tidying old change files; the three screens (DESIGN NEEDED).

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
- **Entry 461: version vectors noticed by hash at each run, over a revision counter bumped in every save.** A counter alone cannot tell
  "changed on two devices since they last met" from "changed twice on one", and bumping in every save would have touched every screen
  that saves; the hash finds a device's own changes wherever they were made, and the vector says how two versions stand.
- **Entry 391: the M834's feed taken as 99.30 percent, the mean of all five readings against the lengths drawn, over planning's 99.35 or the scan's 99.49 alone.** The scan is the most precise single reading and agreed with the rendering across, but its scale along the carriage is unproven, and the plain mean keeps every reading within the instruments' own 0.2 percent.
