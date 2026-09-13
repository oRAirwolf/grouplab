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

## 5a. The experiment was run, and the answer is the printer

Sheet 2 was rotated 180 degrees on the platen and rescanned at 600 DPI. The homography absorbs the rotation, so the displacement field comes back in page coordinates either way and the two hypotheses make opposite predictions.

| Test | Correlation |
|---|---|
| Rotated sheet 2 against **normal sheet 2** | **+0.769** |
| Rotated sheet 2 against normal sheet 1 | +0.571 |
| Rotated sheet 2 against normal sheet 3 | +0.675 |
| Sheet-to-sheet baseline, all unrotated | +0.725 to +0.841 |
| Rotated sheet 2 against the **negated, position-reflected** sheet 2, which is what a scanner-fixed field predicts | **+0.185** |

**The field travels with the paper.** Turning the sheet upside down on the glass turns the error field with it, and the correlation against the unrotated scan of the same sheet is +0.769, indistinguishable from the sheet-to-sheet baseline. The scanner-fixed prediction scores +0.185 and is rejected.

**The renderer is excluded independently.** Phase 0a's conformance test 43 recovers every bull to 0.0002 inches from a synthetic raster of the same PDF, using a comparable centroid method. The same measurement on a scan of that PDF printed gives 0.0021 inches. The twenty-fold difference is introduced between the PDF and the paper, and the rotation test puts it on the paper side.

**So the ink is not where the definition says it is, by about 0.05 mm, and the pattern is a property of the printer.** Splitting the variance by the correlation, roughly 0.0019 inches of it is systematic and reproducible across sheets, and roughly 0.0010 inches is random from sheet to sheet.

## 6. What this does not yet justify

The diagnosis is now done, so restating the gate is earned rather than evasive. **The two-gate structure below was proposed on 13 September 2026 and accepted the same day**, and DESIGN.md section 21 now carries it.

**One thousandth of an inch is below the placement accuracy of the machine that prints the target.** No registration scheme recovers a bull whose ink was laid 0.05 mm from where it was asked for, because there is nothing to recover it from: the fiducials are printed by the same head on the same pass and carry their own share of the same error. The gate as written asks the software to correct the paper.

**And the accuracy that matters is two orders of magnitude coarser.** A shot is measured against its own bull's declared centre, so a bull printed 0.05 mm off contributes 0.05 mm of error to that shot's offset. In a composite group the bull-to-bull variation adds in quadrature with the true dispersion. For a rifle holding a sigma of 2.5 mm at 100 yards, which is roughly half a minute, 0.05 mm of placement error inflates the estimated sigma by **0.02 percent**. For the smallest group anyone would try to measure it is still under a tenth of a percent.

**Proposed structure: two gates, measuring two different things.**

| Gate | Measured on | Threshold | What it protects |
|---|---|---|---|
| Conformance test 43 | A synthetic raster of the PDF | 0.001 in worst bull | That the renderer and the analyser agree. Catches rule R5 disagreements. Already passing at 0.0002 in |
| Phase 0 paper gate | A 600 DPI scan of a printed sheet | **0.005 in worst bull** | That registration, detection and print together stay far enough inside what the statistics need |

Five thousandths keeps the contribution to an estimated sigma below half a percent on the tightest group worth measuring, and the present measurement of 0.0042 inches worst sits just inside it with the printer, the scanner and a scratch centroid all working against it. The real pipeline should do better than a scratch script.

**The proportion is worth stating explicitly, because it is the whole justification.** Ranked by size, the error terms in a measured group are:

| Term | Magnitude at 100 yards |
|---|---|
| A 5 mph crosswind on a match bullet | about 12.7 mm |
| A good shooter's hold off a bipod, 0.1 to 0.3 MOA | 2.5 to 7.5 mm |
| The rifle's own dispersion, half a minute | sigma about 2.5 mm |
| **Hole centroid noise floor, measured** | **0.2 mm**, per TARGET-SCHEMA.md 3.4 |
| Proposed paper gate | 0.127 mm |
| **Printer dot placement, measured here** | **0.05 mm** |
| Conformance test 43 on a synthetic raster | 0.005 mm |

The bull is not the dominant instrument error. The hole is, by a factor of four, and it was measured and written down long before this. Everything the shooter and the weather contribute is larger again by one to two orders of magnitude. A gate set below the hole noise floor is doing its job; a gate set below the printer's physical placement accuracy was asking the software to correct the paper.

**The registration residual stays a reported diagnostic**, per DESIGN.md section 21, and Phase 0 should additionally record the systematic and random split, because a printer whose systematic component is known is a printer whose systematic component could one day be calibrated out. That is not a feature anyone should build now, but it is worth knowing it exists.

DESIGN.md section 6 already recorded that a four-point homography leaves 0.0026 to 0.0032 inches rms on pristine sheets from the existing corpus. The 34-marker homography measured here gives 0.0021, which is better but the same order. That figure has been in the design documents since the beginning and nobody connected it to the Phase 0 gate.
