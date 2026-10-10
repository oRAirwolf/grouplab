# What proves each feature, and what is still needed

NOTES-FROM-PLANNING.md entry 331 section 3. The README lists 31 features as built, not proven. For each: what would prove it (its
written gate, or a proposed one where none is written), what was measured on 2026-10-01 with material already on Alan's computer or on
the `test-data` release, and the result. Everything that still needs Alan is gathered at the end as one checklist for one sitting.

This file decides no state. Where a written gate was found met, it is listed under "For planning" for planning to decide; README.md is
unchanged.

**Entry 363 (Alan's answer to question 79, 2026-10-04):** rows 7, 22 and 26 are now Done in the README, and Phase 0's row says its scan half is met and its photograph half (row 1) is not. Row 6 stays Built, not proven until its MANOVA and ratio-interval fixtures exist.

**2026-10-08, the rows whose proof needs no hardware:** each was given a test against real behaviour, on Alan's own scans and exports where
they exist and on rendered sheets where they do not. Rows 6, 8, 13, 15, 21 and 23 are now proven against their gates by the tests their rows
name; row 16 too, once question 91 decided the report's zero sentence (below). Every other row still needs the material
in the checklist at the end, a change to detection, or a gate planning has not written.

## The 31

Results are from 2026-10-01 unless a date says otherwise. "Proposed" marks a gate nobody has written yet.

| # | feature | what proves it | today's result |
|---|---|---|---|
| 1 | Registration from an off-axis photograph of a sheet held flat | Phase 0 gate: worst bull center within 0.005 in on an off-axis photograph of a sheet held flat | **Not met, closer** (entry 399). `grouplab compare-photos`, the four Fold 7 captures of 2026-09-29 against the 600 dpi scan of the same sheet (dominus-k): worst bull 0.0124, 0.0117, 0.0077 and 0.0143 in, medians 0.0019 to 0.0034 in; the 2026-09-26 pictures of dominus-k and magnus-m 0.0057 and 0.0102 in. The earlier 0.0155 to 0.0216 in were read with no lens correction, because those copies carry no camera tags; GroupLab now corrects the lens on such a photograph too. What is left is the paper's relief between the codes seen through the 13 mm equivalent ultrawide (PHASE1-RESULTS.md, entry 399 section 1). Those pictures were 1 to 15 degrees off square; none was taken at 20 or 35, nor with the main lens: request 94. |
| 2 | Developable-surface model | Phase 1's mounted photograph gate, 0.005 in | **Not met** (0 of 7 frames; entry 399 section 3). With the lens and the bend the median bull is 0.0018 to 0.0046 in on all seven, but the worst is 0.051 to 0.117 in: on five it is the sighter S1, an inch below the last row of codes, where no current GroupLab sheet puts a bull; inside the codes the worst is 0.0051 to 0.0122 in on four frames and 0.063 to 0.116 in on the three where the curl left 2 to 8 codes unread. The cylinder reads those three at 0.034 to 0.055 in, but choosing it from seven frames of one sheet would be fitting these pictures, so the model was not changed. What would settle it: a current sheet, mounted as at the range, photographed and scanned (request 94). |
| 3 | Render-and-difference hole detection | Phase 1 gate: at least 99 percent of holes found, no false positives, matched at 0.15 in | **Not met.** `grouplab scoreboard --corpus` over the nine corpus photographs with a hole truth (entry 399): 221 of 223 holes found (99.1 percent), 6 false marks, 5 of them on the crinkled .22 LR diamond sheet and 1 below the codes on the taped 2026-09-26 dominus-k picture. The four 2026-09-29 pictures alone: 100 of 100, none false. |
| 4 | A mark much bigger than your bullet goes to you | Proposed: every mark holding two holes is ringed, and no clean hole is | **Not met as proposed; half met** (entry 399 section 4). No clean hole is ringed on the corpus photographs: the 7 rings on clean single holes in three of the 2026-09-29 pictures (1.4 to 3.1 holes' area) were all on the right-hand column, where reading those untagged pictures without a lens correction misplaced the printed artwork, and with the correction none is ringed. No two true holes are closer than one bullet anywhere in the corpus, so the first half still cannot be tested on a real photograph; on rendered sheets every double is still found (`RoundsFiredOnARenderedSheetTests`). |
| 5 | The statistics engine against `shotGroups` | Phase 2 gate, STATISTICS.md 15.5 points 1 to 6 | **Points 1 and 3 to 6 met; point 2 not** (as recorded: only on `shotGroups`' own series split of shot 242). The fixture, distribution, coverage and specification tests passed today. |
| 6 | Significance testing | STATISTICS.md 15.5 for the `compareGroups` keys; for the MANOVA row and the ratio interval, proposed: known-truth fixtures, as 15.2 describes for what `shotGroups` cannot provide | **Met for the `compareGroups` keys** (their tests passed 2026-10-01). **Proven 2026-10-08 for the MANOVA row and the ratio interval** by `SignificanceKnownTruthTests`: `TwoGroupManovaIsHotellingsTwoSampleTest` (the two-group MANOVA equals Hotelling's T² written out independently), `TheManovaRejectsAlikeCentresAsOftenAsItsLevelSays` (2, 3 and 4 groups with one centre: p below 0.05 in 4.9 to 5.3 percent of 20,000 sheets) and `TheDispersionRatioIntervalCoversTheTrueRatioAsLabelled` (95.0 percent at equal sizes; 94.6 percent exactly at 5 shots against 25, where the c4 correction only nearly cancels, as STATISTICS.md 8.1 says). |
| 7 | Hit probability inside a radius | STATISTICS.md 15.3 tolerance and 15.5 point 5 | **Met** (its tests passed today). |
| 8 | Subgroups within one sheet | Proposed: each subgroup's figures equal its shots analyzed as a session of their own | **Proven 2026-10-08** by `LadderSubgroupTests.EachRowOfALadderSheetReadsAsASheetOfItsOwn`: the dominus-k scan read as a five-load ladder, a row each, one shot excluded; every figure of every row equals that row alone analyzed as a session told which bulls were aimed at. Not told, four of the first row's five shots go to the row below. No real ladder sheet yet. |
| 9 | The review queue | Phase 3 gate: a full 25-shot target with several misassignments corrected in under two minutes | Not measurable without a person: the wrong-bull scan exists; the gate is Alan's two minutes, timed by `grouplab timing`. |
| 10 | Keyboard operation | Phase 3 gate, done with the keyboard alone | Not measurable without a person: the same sitting as 9. |
| 11 | Shots per bull for doubles | Proposed: a real doubles sheet is matched without the queue raising every second shot | Not measurable: no doubles sheet exists. |
| 12 | `grouplab compare-photos` | Proposed: it reads gates 1 and 2 on real pictures | **It works on real pictures:** the five rows of item 1, with holes 24 or 25 of 25 found, none false, worst hole 0.090 in. It reads the gates; it does not meet them. |
| 13 | The rounds fired as a check | Proposed: a sheet with a known count and one hole holding two shots is flagged | **Proven 2026-10-08 on a rendered sheet** by `RoundsFiredOnARenderedSheetTests.TwoShotsThroughOneHoleAreFlaggedWhenTheRoundsFiredAreEntered`: GL-CF25-LTR at 300 dpi, two .308 holes two thirds of a bullet apart on one bull, read by the real detector; with 26 rounds entered the count item is raised and names the ragged hole, which also has its own item. It is named second, behind a single hole with a torn rim read at 1.48 holes, and the detector found no halves in it, so neither item offers the second shot as a key press. Not yet on paper. |
| 14 | Printing from inside GroupLab on Windows | A sheet from the fixed print path checked on paper | Not measurable: nothing has been printed from the fixed path. |
| 15 | Session records | Proposed: a session saved, reopened, exported and imported gives the same figures | **Proven 2026-10-08 on one of Alan's own** by `RealSessionProofTests.AlansOwnSessionKeepsItsFiguresThroughSaveReopenExportAndImport`: the dominus-k session saved by Accept and analyse, reopened after another sheet, exported with Export all my data and imported in a second window shows the same figures, cards and zero, line for line. |
| 16 | The session report PDF | Proposed: every line equals what the analysis screen shows | **Met as proposed on generated sessions** (report tests passed 2026-10-01). **Met on Alan's own too, by question 91's rule (2026-10-08):** the one line the dominus-k test found was the zero verdict; planning chose the screen's wording, its lines joined into one sentence in the report, and `Entry170Tests.TheReportsZeroSentenceIsTheScreensLinesJoined` holds it. |
| 17 | The target library | No gate | Its tests passed today (built-in library, and the library screen's layout). |
| 18 | Support link | One browser launch once a page exists | Not measurable: there is no support page address. |
| 19 | Volunteer print pack | Proposed: printed at size within 0.5 percent and used once | Not measurable: no pack has been printed. |
| 20 | Chronograph strings by hand | Phase 5: readings reconciled against marked shots | Not measurable: no sheet has been shot with its string recorded in order. |
| 21 | Garmin Xero import | Every one of Alan's exports reads | **Proven 2026-10-08** by `ChronographWorkbookTests.EveryOneOfAlansExportsReadsWhereTheyAreHere`, run on this computer over all 387 of Alan's files (294 .xlsx, 89 .xls, 2 .xlsm, 2 .csv): every one reads and every Garmin Xero string has shots. It runs only where his folder is, never on CI. |
| 22 | Ballistic solver validated | Phase 5's first half, BALLISTICS-VALIDATION.md's tolerances, written before the comparison | **Met** (as recorded; the independent comparison tests passed today). |
| 23 | Load against load on screen | Proposed: the screen's verdicts equal the comparison on two real sessions | **Measured.** The two 6 ARC suppressor scans (dominus-k, magnus-m), 25 shots each, through the comparison the Compare screen calls: sigma 0.347 and 0.341 in, "These two loads group alike as far as these shots can tell, and put their groups in different places" (dispersion p = 0.907, centers p = 0.049), and about 434 shots each to tell a 10 percent difference. **Proven 2026-10-08 on the screen** by `RealSessionProofTests.AlansTwoSuppressorSessionsCompareOnScreenAsTheComparisonSays`: the two sessions chosen in Session records and compared; the screen's groups, tests, verdicts, powers and headline equal the comparison of the two markings worked out without the screen, and say it (sigma 0.347 and 0.341 in, dispersion p = 0.906, centers p = 0.049). |
| 24 | Velocity regression | Proposed: one real string paired with its shots | Not measurable: no paired string. |
| 25 | Hit probability at another distance | Proposed: the prediction against a group measured at the second distance | Not measurable: no load shot at two distances. |
| 26 | The camera capture path | Phase 6 gate: camera capture and lens distortion fitted on the device | **Measured.** The four Fold 7 captures of 2026-09-29, as the phone itself marked them: lens distortion fitted on the phone in every one; 97 of 100 holes found, 4 false marks, median hole error 0.012 to 0.014 in, worst 0.086 in, against the scan of the same sheet. No iPhone or iPad capture yet. |
| 27 | The result on the phone, shared sessions | Request 33 steps 6 and 7 | Partly: the phone's markings above opened and were read here; no session has been shared from a phone and opened in the desktop application. |
| 28 | The Targets screen on the phone | Proposed: a sheet printed from the phone measures true size | Not measurable: nothing printed from a phone. |
| 29 | Signed APK, development build, Play internal test | No gate | Not measurable here: needs both installed on one phone (request 50). |
| 30 | Marking by hand on the phone | Proposed: within the click noise of request 9 | Not measurable: no hand marking on a phone of a shot store-bought target, and request 9's noise is not measured. |
| 31 | `grouplab bench` | Phase 9's baseline gate (entry 388 section 4): no figure more than a quarter and 20 ms slower than `docs/performance-baseline.json` | **Met** on 2026-10-08: the gate passes on the code that set the baseline (docs/PERFORMANCE.md). |

## For planning: written gates found met

1. **Hit probability inside a radius (7):** STATISTICS.md 15.3 and 15.5 point 5.
2. **The ballistic solver (22):** BALLISTICS-VALIDATION.md's tolerances, which are Phase 5's first half.
3. **Significance testing (6), for the `compareGroups` keys only.**
4. **The camera capture path (26), in the Phase 6 gate's own words** (capture and lens distortion fitted on the device), on the Fold 7.
   The gate has no number; the numbers measured are above.
5. **Phase 0, its first half:** a 600 dpi scan of a printed sheet, worst bull 0.00275 in on the dominus-k scan today. The second half,
   the off-axis photograph, is not met (item 1).

6. **Significance testing (6), the MANOVA row and the ratio interval** (2026-10-08): their known-truth tests pass; whether they are the
   fixtures entry 363 asked for is planning's to say.
7. **Garmin Xero import (21)** (2026-10-08): every one of Alan's 387 exports reads, the gate in its own words.

Not met against a written gate: 1, 2, 3, and point 2 of 5. Met against a proposed gate: 8, 13 (on a rendered sheet), 15 and 23, by the
tests their rows name; 16 only on generated sessions.

## The checklist: material still needed, one list for one sitting

Entry 342 item 4: one list, ordered by how many of the 31 each piece of material would prove, then by how little it takes. Where it is
done is in brackets. Nothing in it can be settled without Alan: every line needs paper shot, printed or photographed, or a person timed.

1. **A sheet shot in a known order with its Garmin Xero string recorded**, the order written down: proves 20 and 24, and checks the
   pairing the Xero's own timing now proposes (entry 342) against the truth. (Range, then the scanner.)
2. **A sheet with two shots through one hole** somewhere on it, the count written down: proves 4, and 13 on paper. (Range.)
3. **The dominus-k sheet** (or any shot GroupLab sheet), taped flat and photographed square, at about 20 degrees and at about 35 degrees;
   its scan already exists: proves 1 and 12. (Home.)
4. **Two minutes with the wrong-bull scan**: correct its misassignments with the keyboard alone, detailed logging on: proves 9 and 10.
   (Home, at the computer.)
5. **A shot store-bought target**, scanned and photographed, then marked by hand on both phones: proves 30 with request 9, and gives the
   recognition of entries 340 and 341 its first shot sheet (request 58). (Range, home, the phone sitting.)
6. **A doubles sheet**: two shots on chosen bulls on purpose: proves 11. (Range.)
7. **A ladder sheet**: five or more loads, a few bulls each: proves 8. (Range.)
8. **One load shot at 100 yd and at 300 yd**, a group at each: proves 25. (Range.)
9. **One sheet printed with GroupLab's own Print** on Windows, then scanned at 600 dpi: proves 14. (Home.)
10. **A volunteer pack printed**, its bull 1 to bull 5 distance measured with a ruler: proves 19. (Home.)
11. **A capture on the iPhone and on the iPad** of a shot sheet that has a scan: proves 26 on Apple's phones. (Phone sitting.)
12. **One session shared from a phone and opened on the desktop**: proves 27. (Phone sitting.)
13. **A sheet printed from the phone's Targets screen**, then scanned: proves 28. (Phone sitting.)
14. **The signed APK and the development build installed together** on one phone: proves 29 (request 50). (Phone sitting.)
15. **One stapled, curled sheet photographed three times**, once the surface model has been changed: proves 2. (Home, later.)
16. **The support page's address**, when there is one: proves 18.

Every sheet from the range is also scanned at 600 dpi, which is what lets each one be measured against its own photographs. Item 3
needs a change to detection rather than more material (197 of 198 holes, 2 false marks), item 31's gate is written and met (entry 388),
and items 5, 6, 7, 8, 15, 16, 17, 21, 22 and 23 need nothing more from Alan (question 79 asks planning about the written gates
found met): 16 needs its report's zero verdict to be the screen's, 17 has no gate, and point 2 of 5 cannot be met on `shotGroups`' own
series.
