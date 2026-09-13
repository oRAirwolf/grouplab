# Phase 0 print, measure and scan protocol

**For** producing the physical sample set that Phase 0 measures against
**Status** Procedure. Every design decision it refers to is settled elsewhere.
**Read with** `docs/PHASE0-BRIEF.md` section 4, `DESIGN.md` section 21, `SAMPLE-NOTES.md`

---

## 0. What this is for, and what it is not

Phase 0a proved that the renderer and the analyser agree with each other. They agree in a world with no paper, no ink and no scanner, which is exactly the world where agreement is easy. Phase 0 asks the only question that matters after that: does the agreement survive contact with a printer.

**This protocol produces the evidence, it does not judge the printer.** A print that comes out at 99.6 percent of nominal is not a failure. The whole point of the fiducial scheme is that the software measures what actually landed on the paper and reports it, so scale error is data rather than damage. What this protocol is looking for is the difference between a print that is slightly off, which is normal and absorbed, and a print that is grossly off because something in the print path silently rescaled it, which is not.

There is one thing here that genuinely is a gate, and it is not a measurement: **every sheet must be scanned before it is shot.** A clean scan and a shot scan of the same physical sheet is the input pair that render-and-difference needs at Phase 1, and it cannot be reconstructed later. Once a sheet has holes in it, the clean version of that exact sheet is gone forever.

---

## 1. Equipment

**Required.**

- A printer that takes Letter paper. Any consumer inkjet or laser is fine.
- Digital calipers reading to 0.01 mm, with jaws of at least 150 mm. Vernier calipers work but are slower and harder to read on a printed edge.
- A flatbed scanner. The existing corpus came from one and its behaviour is already characterised in section 8.
- A phone camera that can save an unresized original.
- Plain white printer paper, ordinary weight. Do not use photo paper, card, or anything coated.

**Optional.**

- A steel rule 200 mm or longer, for the spans that exceed the calipers.
- Plotter access for the roll sheet. If you do not have it, skip that item and say so in the notes rather than substituting something else.
- Buff or tinted stock, one sheet, if you have any. The paper knockout is tested against stock colour in the software already, but seeing it on real tinted paper is worth one sheet.

**Not required and not wanted.** Do not use a target backer, a staple gun, or anything else at this stage. Nothing gets shot until section 10.

---

## 2. Before you print, verify the files

Claude Code renders the print set into `out/` along with a one-page index. Open the index first and check that the file list matches section 4 of this document. Then open `out/GL-CF25-LTR.pdf` in a PDF viewer and look at it at 100 percent zoom.

You are looking for four things, and all four are visual rather than measured.

1. **Twenty-five bulls in a five by five grid, plus a row of three below it.** The bottom row is the sighter row and it sits further from the grid than the grid rows sit from each other.
2. **Four square QR codes, one near each page corner.**
3. **Small black squares scattered between the bulls.** These are the fiducial markers. There should be thirty-four of them on this sheet. You do not need to count them now, but they should be obviously present and obviously square.
4. **Nothing overlapping anything.** No marker touching a ring, no code touching a bull.

If any of those four is wrong, stop. That is a renderer fault and printing it wastes paper and time.

---

## 3. Printer settings, and the traps

This is the part where the run gets ruined, and it gets ruined silently. Work through it once, carefully, and then leave the settings alone for the whole run.

**Open the PDF in a real PDF viewer, not a browser.** Adobe Acrobat Reader, or whatever desktop PDF application you use. Browser print dialogs hide the scaling control or rename it, and Chrome in particular defaults to a "Fit to printable area" behaviour that is exactly the thing this protocol exists to prevent.

**In the print dialog, find the page scaling control and set it to 100 percent.** Depending on the application it is called one of:

- "Actual size" in Adobe Acrobat Reader. This is the correct choice there. Do not choose "Fit", "Shrink oversized pages" or "Custom scale".
- "Scale: 100" or "Custom Scale: 100%" elsewhere.
- In Chrome or Edge, "Scale" set to "Default" is **not** the same as 100 percent. Change it to "Custom" and type 100.

**Turn off every automatic fitting option.** The names vary by driver. Look for and disable anything called fit to page, fit to printable area, shrink to fit, scale to paper size, borderless, or expand to fill. Any one of these applies a scale factor of roughly 94 to 97 percent without telling you.

**Set the paper size to Letter**, 8.5 by 11 inches. Not A4. If the driver offers both Letter and "Letter (borderless)", choose plain Letter.

**Set orientation to portrait.**

**Set print quality to normal or better.** Draft mode on an inkjet thins the ink and can break up the fiducial markers, which are only 4.0 mm square. High quality is fine and slower. Do not use any "economy" or "toner save" mode.

**Set colour handling to whatever your printer does natively.** The built-in sheets are black only, so mono is fine. If your printer composites black from colour inks, that is worth knowing and worth noting, because it changes how the artwork subtracts at Phase 1.

**Turn off duplex.** Every sheet is single sided.

**Turn off any "auto rotate and centre" option** if the driver offers one. It usually does no harm, but it moves the artwork relative to the paper edge and there is no reason to introduce a variable.

**Print one copy of `GL-CF25-LTR.pdf` and stop there.** Do not print the whole set yet. Section 5 tells you what to do with that first sheet, and if the settings are wrong you will have wasted one sheet rather than fifteen.

---

## 4. The print list

Print these only after the first sheet has passed section 5.

| # | File | Copies | Scale | Notes |
|---|---|---|---|---|
| 1 | `GL-CF25-LTR.pdf` | 3 | 100% | The reference sheet. One stays clean forever as a control |
| 2 | `GL-CF25-LTR-D-blank.pdf` | 1 | 100% | Load block with captions, nothing filled in |
| 3 | `GL-CF25-LTR-D-filled.pdf` | 1 | 100% | Same definition, load data printed, instance code present |
| 4 | `GL-CF25-LTR-96.pdf` | 1 | 100% | **Already scaled in the file.** Print at 100 percent, not at 96.2 |
| 5 | `GL-LR300-T-tile1.pdf` to `tile4.pdf` | 1 each | 100% | The four sheets of the 2 by 2 assembly |
| 6 | `GL-LR300-R36.pdf` | 1 | 100% | Plotter only. Skip if you do not have one |
| 7 | `GL-CF25-LTR.pdf` on buff stock | 1 | 100% | Optional. Only if you have tinted paper |

**Item 4 is the one people get wrong.** The 96.2 percent sheet is deliberately mis-scaled, and the mis-scaling is baked into the PDF. You print it at 100 percent like everything else. If you also set the printer to 96.2 percent you get 92.5 percent and a fixture that tests nothing anybody planned.

**Items 2 and 3 must produce identical geometry.** That is conformance test 27 and it is worth confirming with your own eyes: hold the two sheets up to a window, one on top of the other, and every bull, marker and code should sit exactly on top of its twin. Only the contents of the block at the bottom differ.

**Write on each sheet in pencil, in the top margin, as it comes out of the printer:** the file name, the date, and a sequence number. Do this immediately. Fifteen sheets of nearly identical black rings become indistinguishable within about ninety seconds. Pencil rather than pen, in the margin rather than anywhere near a bull or a marker.

---

## 5. Measurement

### 5.1 Why you are measuring at all

Two reasons, and neither of them is quality control on your printer.

**To catch a gross scale error.** Fit-to-page produces roughly 94 to 96 percent, which is a 4 to 6 percent error. On the measurement below that is 4.5 to 6.8 mm, which you cannot miss. This check exists to catch that, not to catch 0.3 percent.

**To confirm that the deliberately mis-scaled sheet really is mis-scaled.** Sheet 04 is 96.2 percent of sheet 01, which is a 4.3 mm difference on the span below. That is a large, obvious, hand-measurable difference and confirming it is what makes the fixture worth having.

**What calipers are not for here.** They are not the ground truth for the software's reported print scale, and an earlier draft of this document wrongly asked for one. A printed ink edge is soft at roughly the 0.05 mm level, locating it by hand with caliper jaws is not a hundredth-of-a-millimetre operation whatever the instrument resolves to, and the paper moves with humidity by more than the quantity being chased. Asking for 113.98 against 114.00 is asking for a number the method cannot produce, and a fabricated one would be worse than none because it would be trusted.

**The precise work belongs to the scans, and to a ratio rather than an absolute.** A single absolute measurement cannot separate print scale from measurement error in any case. But the same definition is printed twice, at 100 percent and at 96.2 percent, on the same paper on the same day, and the ratio between those two sheets is exactly 0.962 by construction. Every systematic error common to both, whether edge-finding bias, operator technique, the scanner's own scale error, or overnight humidity, cancels in the ratio. So the real check is whether the software reports a scale for sheet 04 that is 3.8 percent below the one it reports for sheet 01, computed from 600 DPI scans where a pixel is 0.042 mm and the edge finding is subpixel. Hand measurement only has to be good to about half a millimetre for that to hold together, and it comfortably is.

### 5.2 The like-edge rule, which matters more than the caliper does

Ink spreads. A printed black disc is very slightly larger than the disc in the file, by an amount called dot gain that depends on the printer, the ink and the paper. On a consumer inkjet it is commonly 0.05 to 0.15 mm on each edge.

This means **measuring the outside diameter of a ring gives you the true diameter plus two dot gains**, and it will read over 25.4 mm every time. That is not an error and it is not something to correct for.

The way round it is to measure **like edge to like edge**: the left edge of one ring to the left edge of another ring. Both edges are displaced outward by the same dot gain, in the same direction, so the gain cancels exactly and what you measure is the true centre-to-centre distance. This is the single most useful measuring habit in this document.

```
   left edge to left edge          outside to outside
   gain cancels, exact             gain adds twice, reads high

   |<--------- 114.0 --------->|   |<------- 139.4 + 2g ------->|
   (##)  (##)  (##)  (##)          (##)  (##)  (##)  (##)
   ^                 ^             ^                        ^
```

### 5.3 What to measure on `GL-CF25-LTR`

Take the first printed sheet. Lay it flat on a hard surface. Measure each of these and write the result down.

**Record each to the nearest half millimetre. Do not chase hundredths.** The thresholds below are what the check is for; anything inside them passes and the exact digits are not used for anything.

| # | What | Nominal | Gross-error threshold |
|---|---|---|---|
| A | Top row, leftmost bull left edge to fourth bull left edge | **114.0 mm** | Anything outside 113.0 to 115.0 mm |
| B | Left column, top bull top edge to fourth bull top edge | **114.0 mm** | Anything outside 113.0 to 115.0 mm |
| C | Any single ring, outside diameter | **25.4 mm plus dot gain** | Anything outside 25.3 to 25.8 mm |
| D | Two adjacent bulls, like edge to like edge | **38.0 mm** | Anything outside 37.7 to 38.3 mm |
| E | Last scoring row to sighter row, like edge to like edge | **45.6 mm** | Anything outside 45.2 to 46.0 mm |

Measurements A and B are the important pair. Measurement C reads high, because ink spreads, and that is expected rather than wrong. Do not try to extract a dot-gain figure from it by hand: dot gain is measured properly from the 600 DPI scans, where a pixel is 0.042 mm, by comparing the imaged ring diameter against the declared one. That is measurement 2 of `docs/FIDUCIAL-DECISION.md` section 10 and it needs the scan, not the caliper.

**If A and B are both inside the thresholds, the print path is honest and you can print the rest of the set.**

**If A or B is low by 4 to 6 percent**, roughly 108 to 110 mm, then a fit-to-page setting is still on somewhere. Go back to section 3 and work through it again. The likeliest culprits, in order, are: printing from a browser, an "Actual size" versus "Fit" radio button that reset itself, and a driver-level fit option under a properties or advanced button that the application's own dialog does not show.

**If A and B disagree with each other by more than about 0.5 mm**, the printer is scaling the two axes differently. That is unusual but real, especially on inkjets where the paper feed direction and the head travel direction are different mechanisms. Note it, because the software should detect it as separate x and y scale factors, and that is now a specific thing to check rather than a surprise.

**If A and B are off by less than 1 percent, that is normal and you do nothing about it.** Write the numbers down and carry on. That is exactly the error the fiducials exist to measure.

### 5.4 What to measure on the other sheets

Do not repeat the full set. One measurement each is enough, because you are confirming that nothing changed between sheets rather than re-characterising the printer.

| Sheet | Measure | Nominal |
|---|---|---|
| `GL-CF25-LTR-D` blank | Top row, first to fourth bull, like edge | 114.0 mm |
| `GL-CF25-LTR-D` filled | Same | 114.0 mm, and identical to the blank |
| `GL-CF25-LTR-96` | Top row, first to fourth bull, like edge | **109.7 mm** |
| Any `GL-LR300-T` tile | The two columns, like edge to like edge | 101.6 mm |
| `GL-LR300-R36` | Two adjacent bulls in a row, like edge | 101.6 mm |

**The 96.2 percent sheet is the one that proves the fixture is real.** 114.0 times 0.962 is 109.7 mm. If it measures 114.0 you printed the wrong file. If it measures about 105.5 you scaled it twice.

### 5.5 Recording

Create `scans/phase0/MEASUREMENTS.md` and write the conditions and the outcome into it, one block per sheet, in this shape:

```
## GL-CF25-LTR, sheet 1 of 3
Printed 2026-09-14, Brother MFC-J430W, plain paper, Normal quality, colour
Foxit print dialog: Scale None, preview reported 8.5 x 11.0 document on
8.5 x 11.0 paper at 100 percent zoom. Driver reported Scaling Off.
Calipers, Mitutoyo 500-197-30, to the nearest 0.5 mm:
A  top row, 1st to 4th, left edge to left edge   114 mm, within tolerance
B  left column, 1st to 4th, top edge to top edge 114 mm, within tolerance
C  ring outside diameter                          reads slightly over 25.4, as expected
D  adjacent bulls, like edge                      38 mm, within tolerance
Conclusion: no gross scale error. Precise scale to come from the 600 DPI scans.
```

Record the printer make and model, the paper, the quality setting, and what the print dialog reported. Those matter and they are exact. The caliper numbers matter only as a pass or fail against the thresholds, so write them as such rather than inventing a precision the method does not have.

---

## 6. Scanning

### 6.1 Settings

- **Resolution: scan every sheet twice, once at 600 DPI and once at 300 DPI.** Both are needed. The 600 DPI scan is the reference and the 300 DPI scan is what most users will actually produce.
- **Colour mode: colour, 24 bit**, even though the sheets are black only. A colour scan can be converted to grayscale later and the reverse is not true, and the chroma residual work in DESIGN.md section 11 needs colour.
- **File format: PNG or TIFF.** Not JPEG. JPEG artefacts sit exactly on the ring edges that everything downstream measures. The existing corpus is JPEG and that is a limitation of the existing corpus, not a precedent.
- **Turn off every automatic correction.** Specifically: auto crop, auto deskew, auto colour correction, auto contrast, auto exposure, descreen, dust removal, sharpening, and any "text enhancement" or "document mode". Every one of these alters edge positions or invents pixels. Scan raw and flat.
- **Turn off any multi-page or PDF output mode.** One image file per scan.

### 6.2 Placement

Place the sheet squarely against the scanner's registration corner, artwork face down, and close the lid fully. Do not weight it, and do not press on the glass.

Deliberate skew is not needed here. The off-axis case is covered by the photograph in section 7, and a scanner that has been given a skewed sheet plus an auto-deskew you forgot to turn off produces a result nobody can interpret.

### 6.3 The thing that will catch you out

**Your scanner does not capture the whole page.** This is already measured and recorded in `SAMPLE-NOTES.md`: the existing 600 DPI scans are 4958 by 6458 pixels, which is 8.263 by 10.763 inches, against a page that is 8.5 by 11. Roughly 0.24 inches of width and 0.24 inches of height are missing.

The corner QR codes start 12.0 mm, or 0.47 inches, in from the page edge, so they survive comfortably. The outermost fiducial markers can sit as close as 6.0 mm, or 0.24 inches, from the edge, and those are right on the boundary of what your scanner keeps.

So, **after the first 600 DPI scan, before you scan anything else, check the marker count.** Open the image and look at all four edges. `GL-CF25-LTR` carries thirty-four markers. If the edge markers are cut off or missing, that is your scanner cropping, not a printing fault and not a detector fault, and it needs to be known before fourteen more scans are made the same way.

If markers are being lost, note it precisely, in pixels from each edge, and tell me. There are several ways to handle it and the right one depends on how much is going.

### 6.4 Naming

```
scans/phase0/<sheet>_<state>_<dpi>.png

GL-CF25-LTR_clean_600.png
GL-CF25-LTR_clean_300.png
GL-CF25-LTR-96_clean_600.png
GL-LR300-T-tile1_clean_600.png
```

Use `clean` now. The same sheets scanned after shooting become `shot`, which is what makes the pair obvious to anything that reads the directory.

---

## 7. The photograph

`SAMPLE-NOTES.md` records that the existing corpus has **no genuine off-axis photograph at all**, and that the distortion fitting in DESIGN.md section 11 cannot be validated without one. This is the single most valuable new sample in the whole run, because it is the only one that tests a code path nothing has ever tested.

Take at least one, of `GL-CF25-LTR`, clean.

- **Unresized camera original.** Not a screenshot, not shared through a messaging app, not exported from a photo editor. Get the file off the phone by cable or by a method that preserves the original bytes. The corpus already contains a messaging-app-downscaled image and it is there as a degraded-input test, not as a substitute for the real thing.
- **Handheld, from normal standing distance**, roughly the distance you would naturally hold a phone from a target pinned to a wall. Do not brace it, do not use a tripod. The point is to capture what a user will actually produce.
- **Deliberately off-axis.** Stand to one side and above, so the sheet appears as a clear trapezoid rather than a rectangle. Something in the region of twenty to thirty degrees off perpendicular. If it looks square in the viewfinder, move.
- **The whole sheet in frame**, with a little margin around it. All four corner codes must be visible.
- **No flash.** Ordinary indoor or window light. Flash produces a specular hotspot on the paper that is its own separate problem and not the one being tested here.
- **Restrain all four edges.** This is the instruction that went wrong the first time it was given, and it cost a whole photograph session. Masking tape along each of the four edges onto a wall or a door, or the sheet laid on a table under a pane of glass or clear acrylic. **A single pin at the top is not flat**: a sheet hanging from one point curls away under its own weight, and measured against a global homography that curl put the two lowest marker rows 0.026 to 0.138 inches out, six to sixteen times worse than the same sheet lying loose on a table. A clipboard is not flat either. If taping four edges is awkward, glass on a table is easier and better, and an off-axis frame of a sheet on a table tests perspective just as well as one on a wall.

Take three or four and keep them all. They cost nothing and the failure modes of handheld photographs are not visible on a phone screen.

```
scans/phase0/GL-CF25-LTR_clean_photo1.jpg
```

Also worth one photograph of a tile, since the tile carries only nine markers and the photograph path is where nine markers is most likely to be too few.

---

## 8. Order of operations

This order exists because two of the steps are irreversible.

1. Print `GL-CF25-LTR`, one copy.
2. Measure it, section 5.3.
3. If the measurements pass, print the rest of the set. If not, fix the print path and start again at step 1.
4. Measure one span on each remaining sheet, section 5.4.
5. Write `scans/phase0/MEASUREMENTS.md`.
6. **Scan every sheet clean**, at 600 and at 300, section 6.
7. Check the marker count on the first 600 DPI scan before continuing, section 6.3.
8. Photograph, section 7.
9. Write the `SAMPLE-NOTES.md` entries, section 9.
10. Stop. Tell me the numbers before anything gets shot.

**Step 6 comes before any shooting and there is no way to go back and do it later.** Once a sheet has holes, its clean scan cannot be made.

**Keep one copy of `GL-CF25-LTR` clean and unshot, permanently.** File it somewhere flat. It is the control sheet, and in six months when something is behaving oddly it is the only physical object that can tell you whether the target changed or the software did.

---

## 9. Writing the notes

`SAMPLE-NOTES.md` documents the existing corpus well, and the new material should not be worse. Add a `scans/phase0/` section with one row per file and, above it, a short block recording the conditions everything shares: printer make and model, paper, quality setting, scanner make and model, scanner software and version, and the date.

The reason to record the scanner software version is that scanner drivers change their default processing between versions, and a scan that suddenly looks different a year from now is otherwise unexplainable.

---

## 10. What happens after this, and what not to do yet

Phase 0 runs against these clean scans and this photograph. Its gate is registration residual under one thousandth of an inch across the whole page, on both the scan and the photograph, plus correct detection of the deliberately mis-scaled print. The measurements you wrote down in section 5 are what the reported scale gets checked against.

**Do not shoot anything until Phase 0 has run.** Not because shooting breaks anything, but because if the registration work turns up a reason to change the sheet, you want to find that out before there are fifteen shot targets of the old design and a temptation to keep them.

**Do not write on the sheets** beyond the pencil note in the top margin. No X marks, no arrows, no load data in the block by hand. That comes later and it is a Phase 1 concern.

**Do not fold them.** Flat storage, in a folder or a box. Paper is a developable surface and a homography absorbs a fold better than you would expect, per DESIGN.md section 6, but there is no reason to spend that margin before the baseline exists.
