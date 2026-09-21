# 2026-09-21, entry 120: the second range day, run through GroupLab

Alan went back to the range on 2026-09-20 and shot five 5x5 load-block sheets (GL-CF25-LTR-D), one blank zero sheet, and one commercial target. The files are on his machine outside the repository:

- `C:\Dev\grouplab-range-2026-09-20\scans\` : six 600 dpi scans, `1-600-dpi09202026.png` to `6-600-dpi09202026.png` (SHA-256 prefixes 44c377d5120a75bf, 859715cbb9f2def6, 93140a6a37777667, a2e04ebf34e857dc, dabd4bedafe41c7e, d1aff0b4d6b55f39).
- `C:\Dev\grouplab-range-2026-09-20\photos\` : 59 phone photographs, `20260920_HHMMSS.jpg`, taken in six bursts (11:06, 14:14, 15:33, 16:14, 16:56, 18:59).

These are real photographs from a phone. **Do not read, print or log any GPS or location metadata in them. Nothing from this folder is committed.** Any figure or crop you commit must be re-encoded from pixels with no metadata, and only with Alan's say-so in a later command. The folder `C:\Dev\grouplab-range-2026-09-20\sheet1\` named in entry 114 never existed: the 9-shot sheet from the first visit is not in this set, and scan 1 is a different sheet (Alan confirmed).

What I see in the scans, from reading them at reduced size. Treat it as a description to check, not as ground truth:

| Scan | Sheet | Load block as written | What is on it |
|---|---|---|---|
| 1 | LTR-D | 9/20/26, 100 y, 6.5 Creed, 153.5 LRHT, 42.4 H4350, Alpha SRP brass, primer 7.5BR, 2.874, notes 28" Seekins | Shots on bulls 1 to 15 only, rows 4 and 5 unshot |
| 2 | blank letter sheet | none | Hand-drawn circle and cross in blue, one tight group through it. Alan: zero group, 6.5 Creedmoor, 100 yards |
| 3 | LTR-D | same load as scan 1 but primer GM205MAR, no notes | Shots on all 25 bulls, plus a few holes in the left margin beside bulls 11 and 16 |
| 4 | LTR-D | 100 y, 22LR, 40 gr SK, SK, Std+, 1.000, 20" CZ | Wide dispersion, many holes between bulls rather than on them |
| 5 | LTR-D | 100 y, 6 ARC, 108 ELDM, 27 N140, Starline, GM205MAR, 2.250, 18" RTR | Wide dispersion, holes between bulls, one hole near the top left QR code |
| 6 | LTR-D | 100 y, 6mm Creedmoor, 120 gr LRHT, 39.0 N550, Lapua SRP, GM205MAR-BR4, 2.810, AI AXSR (written lighter, in pencil or thin pen) | Shots on rows 1 to 3 |

Scans 1 and 3 are the same load with only the primer changed, which is exactly the compare-loads case from entry 113.

Alan says the number of shots per bull differs by sheet and will give the per-sheet detail separately. Until he does, do not assume one shot per bull anywhere, and mark every count in your report as GroupLab's reading, not the truth.

## 1. Every scan through the application as a user would

Open each scan in the built application, by the ordinary Open path, and record for each: which definition was identified and how (codes, markers, or by name), the time from open to first result and to final result, holes found, holes assigned to each bull, holes left unassigned or flagged, whether the load block was offered for reading or entry, and every message shown. Save a render of each result under a scratch folder outside the repository and look at each one against the scan yourself, hole by hole. Report any hole you can see in the scan that GroupLab missed, and any detection that is not a hole (handwriting in the load block, the blue ink on scan 2, scanner dust, the paper edge). The handwriting on these sheets is thick marker and crosses the load block rules; nothing in it may become a hole.

## 2. Holes between bulls

Scans 4 and 5 are the case the matching has not met: shots that land between bulls, or nearer a neighbouring bull than the one aimed at. Show what GroupLab does with them today, and say whether nearest-bull assignment is producing groups that are wrong. Then propose, in the report and not yet in code, how GroupLab should handle a sheet where assignment is uncertain: at minimum it must say so on screen and let the shooter move a hole to another bull by hand, and it must never present a group statistic built on an assignment it is unsure of as if it were sure. If you think a sheet-wide assignment (one common point-of-impact offset for the whole sheet, with each bull taking its nearest shots after that offset is removed) is sound, describe it with its failure cases and what the shooter must tell GroupLab for it to work, such as shots per bull. Alan's per-sheet answer will say which of these sheets it would apply to.

## 3. The margin holes on scan 3 and the sighters

Scan 3 has holes outside the bull grid, beside bulls 11 and 16. This sheet has no sighter bulls. Report how GroupLab treats a hole that is on the sheet but not near any bull, and make sure it is shown, counted as unassigned, and never silently dropped or pulled into a bull's group.

## 4. The blank zero sheet

Scan 2 is a blank letter sheet with a hand-drawn aiming mark. Run it through the blank-sheet path from entry 115. Note that the scanner crops to 8.26 x 10.76 in (4958 x 6458 at 600 dpi), so the paper edges may not all be in the image; say what scale GroupLab used (scan resolution or paper edges) and whether it asked. The group centre relative to the drawn cross is only as good as the drawing, and GroupLab must not claim otherwise.

## 5. Photographs against scans

Use the compare-photos tool from entry 113 to pair each burst with the scan of the same sheet and report the pairing, how each photograph fared (identified, holes found, agreement with the scan in inches), and which photographs were refused and why. Expect hard cases, and report them, do not work around them:

1. `20260920_110616.jpg` is an unshot sheet printed from inside GroupLab before the entry 114 fix: the codes and markers are missing. It must take the by-name path and must not crash or guess.
2. The 14:14 and 15:33 bursts show two or three sheets side by side on the backer board, some cut off at the frame edge, with holes in the board around them. Say whether GroupLab picks one sheet, asks which, or fails.
3. The 16:14 and 16:56 bursts show the blank sheet before and after, with a 5x5 sheet partly in frame.
4. The 18:59 burst is a commercial printed target (an orange 100-yard grid with diamonds) that GroupLab has no definition for. This is a private hard test only: GroupLab must say it does not recognise the sheet and offer the blank-sheet path, never match it to a GroupLab definition. Never commit it, its name, or any render of it.

## 6. Timing

Add these six scans to the timing measurement from entry 115 section 7 (open to first result, open to final result, identification share), on this machine, and put the figures beside the earlier ones in your report. Do not optimise anything in this entry; the performance phase is where that happens.

## 7. Compare loads

Run the compare-loads view on scans 1 and 3 (same load, primer 7.5BR against GM205MAR). Report what it shows, including the power statement: with the shot counts on these sheets, say plainly whether the comparison can tell the primers apart, and do not let the screen suggest a difference the numbers cannot support.

## 8. Report

One report for this entry in your summary: a table per scan, the photograph pairing table, every missed or false hole with its location, the between-bull findings and your proposal from section 2, the timings, and a short list of what should change, each item marked as a defect or as a proposal. Fix outright only plain defects (a crash, a hole silently dropped, handwriting read as holes, a message that says nothing useful), each with a test built from generated material, never from these files. Anything that changes behaviour a shooter would notice waits for my answer.

## 9. On the report from your last run

You wrote that the Inno Setup installer has never been built. It has: Alan ran the release workflow by hand after your run, and the draft release "GroupLab 0.1.0, draft" from commit 5a4cd07 carries `grouplab-setup-win-x64.exe` and its versioned copy, identical by SHA-256. It was built on the GitHub Windows runner, where Inno Setup is installed. Alan is installing it now. Correct the status line and any document that says otherwise. Questions 28 and 29 stay open; Alan will answer them.
