---
title: "Does your printer print at true size?"
description: "One wrong setting in the print dialog shrinks a target by about 6 percent, and every group on it with it. How print scale affects your numbers, how GroupLab measures it from a scan or a one-time printer check, what one thermal printer did across and along its feed, and a thirty-second check you can do with a ruler."
group: Guides
number: 22
written: 2026-09-22
data_date: "GroupLab Phase 0 print tests, September 2026; two thermal printer check prints, 2026-10-07 and 2026-10-08, and a third after the correction, scanned 2026-10-10"
samples: "Test sheets printed at 100 and 96.2 percent, scanned at 300 and 600 dpi; one thermal printer's check page printed three times and read with a caliper, a ruler and a scan; three thermal labels read from two scans"
state: published
found: "a printer set to 'Actual size' can be very accurate: the developer's inkjet measured 1.0001 across and 1.0006 down, within 0.06 percent. The danger is the print dialog: 'Fit to page' prints a letter sheet at about 94 percent. A printer can also be true one way and not the other: one thermal printer printed true across its head and 99.28 percent along its paper feed, the mean of five readings over two prints. A scan measures the print scale and GroupLab corrects for it; a photograph cannot, so GroupLab uses the scale measured once for that printer."
sure: "the scan detection is from GroupLab's own acceptance tests. The thermal figures are one printer, one roll and two prints, read with a caliper, a ruler and a scan that disagree with each other by up to 0.3 percent. One printer is not every printer; that is why the check exists."
data:
  - data/error-by-scale.csv
  - data/phase0-print-scale.csv
  - data/thermal-check-prints.csv
sources:
  - "GroupLab Phase 0 results, measurement 5, print-scale detection (docs/PHASE0-RESULTS.md) and the print protocol (docs/PHASE0-PRINT-PROTOCOL.md)."
  - "GroupLab detection advice, the print-scale message and its 0.25 percent threshold (src/GroupLab.Core/Marking/DetectionAdvice.cs), and the printer check (src/GroupLab.Core/Marking/PrinterProfile.cs)."
  - "The thermal printer's three check prints and the label scans, docs/PHASE1-RESULTS.md, entries 382, 390 and 391, and requests 84 and 85 (2026-10-10)."
---

## Why print size matters

A GroupLab sheet is a measuring instrument. The distances between its printed markers are known exactly, and GroupLab uses them to turn pixels into inches. If the sheet comes out of the printer 4 percent small, the markers are 4 percent closer together than GroupLab expects, and anything measured against them is off by the same proportion unless the print scale is known.

![What fit to page does](/research/printer-true-size/figures/fit-to-page.png)

## The setting that causes it

Almost every print dialog has an option that shrinks the page to fit inside the printer's margins. It goes by different names: "Fit", "Fit to page", "Shrink oversized pages", "Scale to fit" or "Fit to printable area". Some programs turn it on by default. Because the whole page shrinks evenly, the printout looks perfectly normal. You cannot see a 5 percent change by eye.

How much it shrinks depends on the printer's margins. With a quarter inch on each side, a letter page shrinks to about 94 percent. A printer's own phone app can do the same: given a letter page, the app for the thermal printer described below shrank it to 94.7 percent.

![Measurement error by print scale](/research/printer-true-size/figures/error-by-scale.png)

| Printed at | Every distance on the sheet is | A true 0.300 in mean radius would read |
|---|---|---|
| 100 percent | correct | 0.300 in |
| 97 percent | 3 percent small | 0.309 in |
| 96.2 percent | 3.8 percent small | 0.312 in |
| 94 percent | 6 percent small | 0.319 in |
| 90 percent | 10 percent small | 0.333 in |

The error is always in the same direction: a shrunken sheet makes groups look bigger, because the bullets are full size and the ruler they are measured against has shrunk. Calipers are not fooled, since they measure the holes directly. Anything that measures against the printed sheet is, unless it knows the print scale.

## How a scan catches it

A flatbed scanner has an absolute ruler built in: its resolution. At 600 dpi, 600 pixels is one inch, whatever is on the glass. So when GroupLab reads a scanned sheet it can compare the markers' real spacing with the spacing they were designed to have, and work out the print scale.

In GroupLab's acceptance tests, a sheet deliberately printed at 96.2 percent was measured at 0.96200 from a 600 dpi scan and 0.96201 from a 300 dpi scan. The test required agreement within 0.001; it agreed within 0.00001. The printer's own error at 100 percent (1.0001 across, 1.0006 down) cancels out in that comparison.

GroupLab then corrects for it: on a scan every distance is multiplied by the measured scale, so a group on a sheet printed at 96 percent reads its true size. When the scale differs from 100 percent by more than a quarter of a percent, the results panel names it and says the sizes are corrected.

## Photographs need the printer measured once

A phone photo has no built-in ruler, because the camera's distance from the paper is unknown, so a sheet printed small looks exactly like a full-size sheet a little farther away. Print scale belongs to the printer and its settings, though, and it stays put. So GroupLab measures it once per printer with its check page, and multiplies every later photograph of that printer's sheets by the result, across the sheet and down it separately.

The check page is offered the first time GroupLab opens and the first time you print, and is always in Settings under Printers. Print it at actual size and measure it one way: lay a bank, gift or ID card inside its outline and take one photo (good to about 0.3 percent), measure between its crosshairs with a digital caliper (about 0.1 percent), measure its two long lines with a ruler or tape, or scan it. Each result corrected this way says whose figures it used. With no check saved, a photograph's figures stay in the sheet's own inches and the result says so; and when the paper's own edge says the sheet looks printed with Fit to page, GroupLab says that too.

[What GroupLab can measure](/what-can-be-measured/) sets out where the scale comes from in every case, and what a mis-scaled print does to every figure.

## A printer can be true one way and not the other

Across and down are measured separately because a printer's two directions are made by different parts. On an inkjet or a laser, across is set by the print head or the scanning beam, and down by the paper feed. On a thermal printer, across is fixed by the dots of the head and down is entirely the feed rollers.

One thermal printer, a Phomemo M834 printing GroupLab's letter check page from a roll, shows it. Its page was printed twice, a day apart, and the same two lengths were read on each print: the distance between the crosshairs, which GroupLab drew 150.02 mm apart, and the side ruler, drawn 250.02 mm long.

![One thermal printer, measured across and along the feed](/research/printer-true-size/figures/thermal-two-ways.png)

| | Readings | Printed at |
|---|---|---|
| Across the head | caliper on print 1, caliper and scan on print 2 | 99.79 to 100.30 percent, mean 100.03 |
| Along the feed | caliper and ruler on both prints, scan on print 2 | 99.19 to 99.49 percent, mean 99.28 |

Across, the scan read exactly what GroupLab drew, and the two caliper readings fell either side of it. Along the feed, every one of five readings was short. So the head is true and the feed pulls the paper about 0.7 percent too fast, which is enough to make every vertical figure on a photographed sheet about 0.7 percent large if nothing corrected it. GroupLab now draws this printer's pages 0.70 percent longer along the feed, so that they print true. A third check print, made after that change, came out true both ways: 150.0 mm across and down by caliper, 190 and 250 mm on the rulers, and 149.98 mm across and 150.07 mm down on its 600 dpi scan.

Three smaller things the same prints showed, all worth knowing before you trust a single reading:

- **The instruments disagree with each other.** On the same sheet the scan read 0.21 percent larger than the caliper across and 0.15 percent larger down. Readings by eye on crosshairs are good to a few tenths of a percent, not better, which is why GroupLab gives each way of checking a printer its own uncertainty.
- **One caliper reading is not a measurement.** Print 1's caliper said 100.30 percent across; print 2's said 99.79, from a page drawn identically. Two prints are what showed that the spread was the caliper's.
- **The paper can track sideways.** On print 2 the top and side crosshair lines meet at 90.22 degrees, not 90: the paper drifted about 1 mm sideways over 250 mm of feed. A scanner cannot add that. It is small, and it is recorded rather than corrected until a later print repeats it.

A smaller thermal label printer, a Phomemo M220, printed two codes GroupLab placed 60.0 mm apart at 59.99 and 59.96 mm, read from a scan of one label: true across its head to 0.07 percent. Along its feed, two rows 10.5 mm apart came out 10.39 mm apart, about 1 percent short, the same direction as the larger printer. Two more labels repeated it, 10.35 to 10.45 mm on both sides of each, so it is the printer's feed and not one label. Nothing needs correcting, because GroupLab reads these labels across only.

## A thirty-second check

1. Print the sheet with the scale set to **100 percent** or **Actual size**. Turn off any fit or shrink option.
2. Lay a ruler along the sheet and measure a known distance. GroupLab's check page has crosshairs and two long ruled lines for this; the aim point test card has a 2 inch bar.
3. Over 2 inches, 94 percent shows as about 1.88 inches, an eighth of an inch short, which is easy to see on a ruler. 98 percent is about 1.96, harder but visible on a good steel rule.
4. Measure across the page and down it. A printer can be right in one and wrong in the other.
5. If it is short, check the print dialog again. On some systems the setting is buried under "More settings" or in the printer's own preferences.
6. Once it is right, most programs remember it. Check again if you change programs or printers, or if the printer is serviced.

## A note on paper

Paper grows and shrinks slightly with humidity, and a sheet that has been rained on or left in the sun for hours can change shape. For normal range use this is small compared with the print-scale mistake above, but do not measure a sheet that has been soaked and dried.

## What this means

**Check once, then stop worrying.** An inkjet set to "Actual size" was accurate to 0.06 percent here. The usual danger is not the printer; it is the print dialog, or a printer's app, quietly fitting the page and shrinking the sheet by three to six percent.

**Check both directions.** A thermal printer that was true across its head printed 0.7 percent short along its feed, every time. A check that measures only one direction would have missed it.

**On a photograph, a shrunk sheet makes every group read large by the same fraction**, because the markers shrank with the sheet and a photograph has nothing else to measure against. A scan measures the shrink and corrects for it; for photographs, GroupLab's one-time printer check does the same job. [What GroupLab can measure](/what-can-be-measured/) sets out which errors are recovered and which are not.
