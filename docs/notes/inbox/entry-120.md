# 2026-09-21, entry 120: the second range day, run through GroupLab

Alan went back to the range on 2026-09-20 and shot five 5x5 load-block sheets (GL-CF25-LTR-D), one blank zero sheet, and one commercial target. The files are on his machine outside the repository:

- `C:\Dev\grouplab-range-2026-09-20\scans\` : six 600 dpi scans, `1-600-dpi09202026.png` to `6-600-dpi09202026.png` (SHA-256 prefixes 44c377d5120a75bf, 859715cbb9f2def6, 93140a6a37777667, a2e04ebf34e857dc, dabd4bedafe41c7e, d1aff0b4d6b55f39).
- `C:\Dev\grouplab-range-2026-09-20\photos\` : 59 phone photographs, `20260920_HHMMSS.jpg`, taken in six bursts (11:06, 14:14, 15:33, 16:14, 16:56, 18:59).

These are real photographs from a phone. **Do not read, print or log any GPS or location metadata in them. Nothing from this folder is committed, with one exception: scan 3 becomes the published sample under section 9, after its consent record is in place.** Any figure or crop you commit must be re-encoded from pixels with no metadata, and only with Alan's say-so in a later command. The folder `C:\Dev\grouplab-range-2026-09-20\sheet1\` named in entry 114 never existed: the 9-shot sheet from the first visit is not in this set, and scan 1 is a different sheet (Alan confirmed).

What I see in the scans, from reading them at reduced size. Treat it as a description to check, not as ground truth:

| Scan | Sheet | Load block as written | What is on it |
|---|---|---|---|
| 1 | LTR-D | 9/20/26, 100 y, 6.5 Creed, 153.5 LRHT, 42.4 H4350, Alpha SRP brass, primer 7.5BR, 2.874, notes 28" Seekins | Shots on bulls 1 to 15 only, rows 4 and 5 unshot |
| 2 | blank letter sheet | none | Hand-drawn circle and cross in blue, one tight group through it. Alan: zero group, 6.5 Creedmoor, 100 yards |
| 3 | LTR-D | same load as scan 1 but primer written as GM205MAR, no notes. **The primer is written wrongly: Alan says it was 7.5BR, the same as scan 1** | Shots on all 25 bulls, plus a few holes in the left margin beside bulls 11 and 16 |
| 4 | LTR-D | 100 y, 22LR, 40 gr SK, SK, Std+, 1.000, 20" CZ | Wide dispersion, many holes between bulls rather than on them |
| 5 | LTR-D | 100 y, 6 ARC, 108 ELDM, 27 N140, Starline, GM205MAR, 2.250, 18" RTR | Wide dispersion, holes between bulls, one hole near the top left QR code |
| 6 | LTR-D | 100 y, 6mm Creedmoor, 120 gr LRHT, 39.0 N550, Lapua SRP, GM205MAR-BR4, 2.810, AI AXSR (written lighter, in pencil or thin pen) | Shots on rows 1 to 3 |

Scans 1 and 3 are therefore the same load, same rifle, same day: 40 shots. Scan 6 is the real primer comparison (section 7).

### Ground truth from Alan

This is what the shooter says happened. Score GroupLab against it, and keep it in the report beside every count GroupLab produces.

| Scan | Shots | Aimed at | Notes |
|---|---|---|---|
| 1 | 15 | one shot at each of bulls 1 to 15 | |
| 2 | zero group, 6.5 Creedmoor, 100 y | the drawn cross | shot count is GroupLab's to find; report it |
| 3 | 25 | one shot at each of bulls 1 to 25 | primer 7.5BR, not the GM205MAR written on the sheet |
| 4 | 23 | row 1: bulls 1 to 4; row 2: bulls 6 to 9; row 3: bulls 11 to 15; row 4: bulls 16 to 20; row 5: bulls 21 to 25 | 22LR at 100 y in a strong, variable crosswind. Windage was changed after row 2, so rows 1 and 2 share one point of impact and rows 3 to 5 another. Scatter is real, not a detection fault |
| 5 | 20 | bulls 2, 3, 4 and 5 of every row | a load the rifle is not zeroed for, so every shot is high and left of its aim point; many holes will sit nearer another bull than the one aimed at |
| 6 | 10 | bulls 1 to 5 with GM205MAR primers; bulls 6 to 10 with CCI BR-4 primers | the BR-4 shots impacted low, landing on row 3. Shot 6, aimed at bull 6, landed left of bull 21, much lower than the rest, cause unknown. It is a real shot, not a flyer to delete, and it belongs to bull 6 and the BR-4 load |

Where a count below says "holes", it is GroupLab's reading; where it says "shots", it is this table.

## 1. Every scan through the application as a user would

Open each scan in the built application, by the ordinary Open path, and record for each: which definition was identified and how (codes, markers, or by name), the time from open to first result and to final result, holes found, holes assigned to each bull, holes left unassigned or flagged, whether the load block was offered for reading or entry, and every message shown. Save a render of each result under a scratch folder outside the repository and look at each one against the scan yourself, hole by hole. Report any hole you can see in the scan that GroupLab missed, and any detection that is not a hole (handwriting in the load block, the blue ink on scan 2, scanner dust, the paper edge). The handwriting on these sheets is thick marker and crosses the load block rules; nothing in it may become a hole.

## 2. Holes between bulls

Scans 4 and 5 are the case the matching has not met: shots that land between bulls, or nearer a neighbouring bull than the one aimed at. Show what GroupLab does with them today, and say whether nearest-bull assignment is producing groups that are wrong. Then propose, in the report and not yet in code, how GroupLab should handle a sheet where assignment is uncertain: at minimum it must say so on screen and let the shooter move a hole to another bull by hand, and it must never present a group statistic built on an assignment it is unsure of as if it were sure. If you think a sheet-wide assignment (one common point-of-impact offset for the whole sheet, with each bull taking its nearest shots after that offset is removed) is sound, describe it with its failure cases and what the shooter must tell GroupLab for it to work, such as shots per bull. Test any such idea on paper against the ground truth: scan 5 (one common offset, bulls 2 to 5 of each row), scan 4 (two offsets, rows 1 to 2 and rows 3 to 5, plus real wind scatter) and scan 6 (the BR-4 shots landing a whole row low, and shot 6 far from everything). Say plainly which of these a sheet-wide assignment would get right, which it would get wrong, and what the shooter would have to enter for it to work.

## 3. The margin holes on scan 3 and the sighters

Scan 3 has holes outside the bull grid, beside bulls 11 and 16. This sheet has no sighter bulls. Report how GroupLab treats a hole that is on the sheet but not near any bull, and make sure it is shown, counted as unassigned, and never silently dropped or pulled into a bull's group.

## 4. The blank zero sheet

Scan 2 is a blank letter sheet with a hand-drawn aiming mark. Run it through the blank-sheet path from entry 115. Note that the scanner crops to 8.26 x 10.76 in (4958 x 6458 at 600 dpi), so the paper edges may not all be in the image; say what scale GroupLab used (scan resolution or paper edges) and whether it asked. The group centre relative to the drawn cross is only as good as the drawing, and GroupLab must not claim otherwise.

## 5. Photographs against scans

I have looked at all 59 photographs. What each burst contains, with my pairing to the scans, which your compare-photos tool must confirm or contradict on its own evidence (report every disagreement with me):

| Burst | Photos | What they show | Pairs with |
|---|---|---|---|
| 11:06 | 1 | An unshot sheet on a table, printed from inside GroupLab before the entry 114 fix: no codes, no markers | nothing (no scan) |
| 14:14 to 14:15 | 17 | Four unshot LTR-D sheets on the backer board, before shooting, from many angles: square on, strongly oblique, close, wide, some cut off at the frame edge, the board's old holes all around them | nothing: no holes on the paper |
| 15:33 | 10 | The same sheets after shooting, load blocks still blank. The 22LR sheet (153325, 153356) and the 6 ARC sheet (153340, 153344, 153347) square on; the 6.5 Creedmoor 25-shot sheet at the left of 153309 and 153336; 153318 the whole board | scans 4, 5 and 3 |
| 16:14 to 16:15 | 9 | The blank zero sheet and the orange commercial target, both unshot. 161541 and 161547 show a tape measure held across the blank sheet, horizontally and vertically | nothing (unshot) |
| 16:56 | 13 | The zero sheet after shooting (165611 to 165620, 165627); a 5x5 sheet with rows 1 to 3 shot, one hole per bull (165624, 165634, 165637); the orange target with a group in its centre (165641 to 165649) | scans 2 and 1 |
| 18:59 to 19:00 | 9 | The orange target finished, whole and in close-ups of each corner diamond | nothing (no scan; private hard test) |

The scan 6 sheet (6mm Creedmoor) was photographed on a second phone and came in through the upload page: submission `C:\Dev\grouplab-submissions\2026-09-21_86926341` (10 photographs, 6mm Creedmoor, 100 yards, corrugated plastic, staples; consent agreed, not excluded from the public dataset). Alan confirmed these are the scan 6 photographs. What they show:

- 001 to 006 (14:13 to 14:14): a different board, several unshot GroupLab sheets, one of them stapled over another printed target so that target's grid and lines show around and behind it, and a sheet cut off at the frame edge in several.
- 007 (14:35): the shot sheet, wide, with a tape measure held against it.
- 008 (14:35): the shot sheet square on and close. Row 1 holes sit just below bulls 1 to 5; the BR-4 holes sit on row 3; row 2 is empty. This is the best photograph of the set.
- 009 and 010: the same sheet, oblique.

Pair 007 to 010 with scan 6 and score them against its ground truth in the table above. Read the photographs from the submission folder in place; do not copy, rename or alter anything in it, and do not run it through intake or publish anything from it in this entry.

The load blocks were filled in after the range, so the photographs cannot be paired by what is written on them. Pair by hole pattern, and use the capture time only to order a burst, never as evidence of which sheet it is.

For each pairing report: identified or not and how, holes found, agreement with the scan hole by hole in inches, and which photographs were refused and why. Then these cases specifically, reported and not worked around:

1. `20260920_110616.jpg`, codes and markers missing, must take the by-name path and must not crash or guess.
2. The 14:14 burst is the best material GroupLab has yet for identification under perspective: the same unshot sheets at many angles. Report, per photograph, whether each sheet in frame was identified, and where it fails (angle, distance, sheet cut off, several sheets in frame). The board's own holes around the sheets must never be counted as holes on a sheet.
3. Several sheets in one frame (153309, 153318, 153336, much of the 14:14 burst): say whether GroupLab picks one sheet, asks which, or fails.
4. The tape measure in 161541 and 161547 is a scale reference the shooter put in on purpose. Report what the blank-sheet path makes of those two photographs, and propose, without building it, whether a visible ruler or tape should be offered as a way to set scale on a blank sheet.
5. The orange commercial target (16:15, 16:56 and 18:59 bursts) has no GroupLab definition. It is a private hard test only: GroupLab must say it does not recognise the sheet and offer the blank-sheet path, never match it to a GroupLab definition. Never commit it, its name, or any render of it.

## 6. Timing

Add these six scans to the timing measurement from entry 115 section 7 (open to first result, open to final result, identification share), on this machine, and put the figures beside the earlier ones in your report. Do not optimise anything in this entry; the performance phase is where that happens.

## 7. Compare loads

Two runs:

1. Scan 6, bulls 1 to 5 (GM205MAR) against bulls 6 to 10 (CCI BR-4), with the load set per bull as entry 115 question 27 allows. The BR-4 holes sit on row 3, so first say whether GroupLab assigns them to bulls 6 to 10 or to row 3's bulls, and what the shooter must do to put them right. Then report what compare-loads shows, including the power statement: five shots against five, one of them far out, say plainly what the comparison can and cannot tell, and do not let the screen suggest a difference the numbers cannot support. The point-of-impact shift between the primers is visible to the eye and is a legitimate thing to report; a precision difference from five shots is not.
2. Scans 1 and 3 as one load across two sheets (40 shots). Report whether GroupLab can pool two sheets of the same load today, and if not, note it as a proposal.

The wrong primer written on scan 3 is a case to report too: say whether the shooter can correct a load block reading after the fact, and whether the correction is kept with the session.

## 8. Report

One report for this entry in your summary: a table per scan, the photograph pairing table, every missed or false hole with its location, the between-bull findings and your proposal from section 2, the timings, and a short list of what should change, each item marked as a defect or as a proposal. Fix outright only plain defects (a crash, a hole silently dropped, handwriting read as holes, a message that says nothing useful), each with a test built from generated material, never from these files. Anything that changes behaviour a shooter would notice waits for my answer.

## 9. On the report from your last run

You wrote that the Inno Setup installer has never been built. It has: Alan ran the release workflow by hand after your run, and the draft release "GroupLab 0.1.0, draft" from commit 5a4cd07 carries `grouplab-setup-win-x64.exe` and its versioned copy, identical by SHA-256. It was built on the GitHub Windows runner, where Inno Setup is installed. Alan is installing it now. Correct the status line and any document that says otherwise. Alan has answered questions 28 and 29:

1. **Question 28, support link:** no address yet; he may register a domain later. Leave a clearly marked placeholder: one constant in one place, and the menu item says there is no support address yet rather than opening anything. Do not invent a domain. A test holds that no other support address appears anywhere in the repository.
2. **Question 29, published sample:** use scan 3, `C:\Dev\grouplab-range-2026-09-20\scans\3-600-dpi09202026.png` (SHA-256 prefix 93140a6a37777667), as the sample. Alan wrote to me: "Use scan 3 as the sample scan. I dont care about my load data being shared." That sentence, the date 2026-09-21, the file name, its SHA-256 and the fact that it was given in the planning session go into a consent record in the repository, beside the existing sample records, before the file is committed. No consent record, no publication, as always. The PNG carries no metadata beyond its resolution (I checked: every text chunk is empty), but strip all ancillary chunks except the resolution anyway. You may recompress it losslessly to save space, provided the decoded pixels are identical to the original; record both file hashes and the pixel hash. It replaces the generated sample in the package and in the README where the generated one is described, and its ground truth (25 shots, one per bull, load as corrected above) becomes the expected result for the package's self-test in the `windows package` job. This consent covers scan 3 only; the other scans and every photograph stay private.

## 10. The target library uses the whole window

Alan's screenshot of the target library at 2000 by 1125 shows the page using about a third of the window, with the rest empty:

1. The sheet list is a fixed narrow column, so names are cut off ("GroupLab 5x5 Load Development with Lo...", "GroupLab 5x5 Load Development, Let..."). Worse, on the first row the name runs into the size column: "100 m, A4A4 · 25 + 3". That overlap is a plain defect.
2. The preview is a small fixed box (about 330 by 420) beside the list, with blank space to its right and below it.
3. The status bar says "Drag to move the image. Zoom with the wheel or the buttons." There are no zoom buttons on the screen.

Make the library fill the window:

1. The list column is wide enough for the longest built-in sheet name and its paper-and-bulls column at the current font size, with no truncation at the minimum window size the application supports. The column is resizable with the same splitter behaviour as entry 105, and its width is remembered. If a name still cannot fit at the minimum window size, wrap it to a second line rather than cut it, and never let the name and the size column overlap.
2. The detail area takes all the remaining width and height: title, description and buttons at the top, and the preview filling everything below, scaled to fit the page whole by default, keeping its aspect ratio, growing as the window grows.
3. Either add the zoom buttons the status bar promises (zoom in, zoom out, fit) or change the status bar text to say what is actually there. Adding them is preferred, matching whatever the analysis screen already uses.
4. Tests: a layout test that renders the library at 1280 by 720 and at 2560 by 1440 and asserts that no list item's text is truncated or overlapping, and that the preview's area is at least half the window's. Save renders at both sizes under `docs/figures/screens/current/` as entry 109 set out, and look at them yourself before reporting.

