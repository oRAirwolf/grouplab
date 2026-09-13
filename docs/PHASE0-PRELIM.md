# Phase 0 preliminary measurement, from the first sample set

**Measured** 13 September 2026, from `scans/phase0/`, before the Phase 0 spike was written
**Status** Finding, not a conclusion. The cause is not yet separated and one cheap experiment separates it.
**Method** Scratch analysis, not the GroupLab pipeline. Reproduce it properly in Phase 0.

---

## 1. What was measured

Three separately printed copies of `GL-CF25-LTR`, scanned at 600 and 300 DPI on a flatbed. For each scan: detect the 34 `tag36h11` markers with OpenCV, correct the corner order, fit a homography from the declared marker corners to the detected ones, then locate each bull centre by an ink-weighted centroid over a 100 dmm circular window and map it back to page coordinates through the inverse homography. The window holds the inner annulus and the centre dot entirely, and touches neither the outer annulus nor the printed label, so the ink inside it is symmetric about the bull centre. Results are stable to within 0.00002 inches across window radii of 90, 100 and 110 dmm, so the method is not the limiting factor.

## 2. Results

| Quantity | Value |
|---|---|
| Markers detected, 600 and 300 DPI, every sheet | **34 of 34** |
| Scanner capture | 8.263 by 10.763 inches of an 8.5 by 11 page, 0.237 inches lost in each dimension |
| Print scale, from the marker fit | **x 1.00005, y 1.00058** |
| Ink spread, from the best-fit marker size | 0.01 mm per edge, negligible |
| Marker corner residual, 600 DPI | 0.00212 in rms, 0.00518 in max |
| **Bull centre error, 600 DPI, three sheets** | **0.0021 to 0.0022 in mean, 0.0042 in worst** |
| Bull centre error, 300 DPI | 0.00305 in mean, 0.00555 in worst |

**The Phase 0 gate is one thousandth of an inch. The measured bull error is two to four times that.**

## 3. What the error is not

**Not corner refinement.** Sweeping OpenCV's subpixel window from 3 to 24 pixels, which is 0.25 to 2.0 marker modules at 600 DPI, changes the residual by less than one part in a thousand. It is flat at 0.00213 inches throughout.

**Not the geometric model.** Adding a quadratic or a cubic correction on top of the homography, evaluated leave-one-marker-out so the flexible model cannot flatter itself, gives 0.00201 and 0.00202 inches against the homography's 0.00224. A smooth global warp is not what is missing.

**Not ink spread.** Solving for the marker size that best fits the printed ink gives 40.2 dmm against a declared 40.0, so the printed markers are 0.01 mm per edge larger than nominal. That is far too small to explain the residual, and it also means this Brother's dot gain is much lower than the 0.05 to 0.15 mm that was assumed.

**Not the measurement method.** The result is unchanged across three window radii, and a symmetric estimator applied to a symmetric object cannot manufacture a spatially structured field.

**Not print scale.** The fitted scale is within six parts in ten thousand of nominal on both axes.

## 4. What the error is: systematic, and reproducible across sheets

The per-bull displacement is a **vector field**, and the same field appears on every sheet.

| Comparison | Mean vector difference | Correlation |
|---|---|---|
| Same sheet, 600 against 300 DPI | 0.00163 in | **+0.844** |
| Sheet 1 against sheet 2 | 0.00148 in | **+0.752** |
| Sheet 1 against sheet 3 | 0.00154 in | **+0.725** |
| Sheet 2 against sheet 3 | 0.00127 in | **+0.841** |

Three sheets, printed separately and scanned separately, agree on where the error points. Averaging the three fields leaves a magnitude of 0.00194 inches mean and 0.00363 worst, barely below the single-sheet figures, which is what a systematic error looks like: averaging does not remove it.

Its shape, averaged over the three sheets, in dmm:

```
by row (y)        dx        dy          by column (x)    dx        dy
  y  539       +0.007    +0.537          x  320       -0.081    -0.120
  y  919       -0.006    +0.215          x  700       +0.354    +0.172
  y 1299       -0.106    -0.761          x 1080       +0.064    +0.144
  y 1679       -0.023    +0.386          x 1460       -0.152    +0.275
  y 2059       -0.003    +0.097          x 1840       -0.315    +0.003
```

The `dy` term varies by row in a way that is not monotone, which is the signature of banding rather than of scale. The `dx` term falls roughly monotonically across the columns, which is the signature of a residual optical term the homography cannot absorb.

## 5. The three candidate causes, and the experiment that separates them

1. **Printer dot placement.** Consumer inkjets place ink to roughly 0.05 to 0.1 mm, and the measured field is 0.05 mm. Head pass banding along the feed axis would produce exactly the non-monotone `dy` pattern above. If this is the cause, no amount of software removes it, because it is where the ink physically is.
2. **Scanner geometry.** Carriage speed variation along the scan axis and residual optical distortion across it. Same scanner and same registration corner every time, so it would reproduce across sheets exactly as observed.
3. **Something in the renderer or the definition.** Least likely, because Phase 0a's conformance test 43 recovers every bull to 0.0002 inches on a synthetic render of the same PDF, and test 39 shows the PDF and the renderer's own raster agree within 0.27 pixels.

**One experiment separates 1 from 2, and it costs one scan.** Take a sheet that has already been scanned, rotate it 180 degrees on the platen, and scan it again at 600 DPI. Then compare the displacement field to the original, after rotating the field back into page coordinates.

- If the field **rotates with the sheet**, it is fixed to the paper, and the cause is the printer.
- If the field **stays fixed in scanner coordinates**, the cause is the scanner.
- If it is partly both, the split is directly measurable from the same pair.

## 6. What this does not yet justify

**It does not justify relaxing the Phase 0 gate.** A gate that is missed is a finding, and the response to a finding is diagnosis rather than a redefinition that makes it pass. That is doubly true here because the same instinct has already been caught twice in this project, on the grid rounding rule and on the tile cells.

**It does say the gate needs re-examining once the cause is known.** If the cause is printer dot placement, then one thousandth of an inch is below the physical placement accuracy of the machine producing the artwork, and no registration scheme can recover a bull that was printed 0.05 mm from where it was asked for. The question then becomes what accuracy the application actually needs, which is a different and much coarser number: a shot measured against a bull that is 0.05 mm from its declared position carries 0.05 mm of error into a group whose dimensions are measured in millimetres.

DESIGN.md section 6 already recorded that a four-point homography leaves 0.0026 to 0.0032 inches rms on pristine sheets from the existing corpus. The 34-marker homography measured here gives 0.0021, which is better but the same order. That figure has been in the design documents since the beginning and nobody connected it to the Phase 0 gate.
