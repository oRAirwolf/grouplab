# Phase 0 results: the registration spike

**Measured** 13 September 2026, from `scans/phase0/`, with the Phase 0 pipeline on branch `phase-0`, against the geometry that was printed
**Brief** `docs/PHASE0-SPIKE-BRIEF.md`
**Reproduce** every table with `grouplab spike sheets|photos|markers|refinement|threshold|scale|field|detectors`, and any single image with `grouplab measure <image> <definition> -v 3`
**Raw rows** each of those commands also writes the per-bull, per-corner and per-transform values behind its table to `scans/phase0/measurements/<command>.json` (section 6)
**Amended** 15 September 2026, `docs/NOTES-FROM-PLANNING.md` entry 52: the markers are now sorted by identifier before use, and every table and figure here that a `grouplab spike` command prints is regenerated with them. No verdict changed. What moved, and why the movement measures how unstable registration is on these frames rather than an improvement, is in `docs/PHASE1-RESULTS.md` "Entry 52"
**Amended** 17 September 2026, `docs/NOTES-FROM-PLANNING.md` entry 101: the synthetic scan's warp, both corner refinements and the homography's final fit are now managed code, so that every platform prints the same tables, and every table here is regenerated with them. No verdict changed. The paper gate's worst bull is 0.00319 in where it was 0.00340, the print scale 0.96200 where it was 0.96195, and the one finding that changed is measurement 2's 1.5-module window, no longer over the gate on paper. What moved and why is in `docs/PHASE1-RESULTS.md` "Entry 101"

---

## 1. The gates

| Gate | Measured on | Threshold | Result |
|---|---|---|---|
| Conformance test 43, unchanged | Synthetic raster of the PDF | 0.001 in worst bull | **Pass** on every page of every built-in sheet |
| Paper gate | 600 DPI scan of each of the ten printed sheets | 0.005 in worst bull | **Pass, ten of ten.** Worst 0.00319 in, tile 3 |
| Photograph gate, flat, Phase 0 | `main_flat1-3`: sheet 3 lying flat, main camera | 0.005 in worst bull | **Fail, three of three.** Worst 0.00661, 0.01016 and 0.01183 in. With every marker decoded, every scoring bull passes and only the sighter S1 fails, on the geometry section 4.4 fixes; the other two frames lose far-edge markers to defocus. Section 3b |
| Photograph gate, mounted, Phase 1 | The seven usable frames of sheet 3 hanging from a pin | 0.005 in worst bull | **Fail, seven of seven, as DESIGN.md section 21 [r5] expects** until a surface model exists. Worst 0.052 to 0.114 in. Section 3a; `telephoto1` and `telephoto3` overflow the frame and are excluded |
| Print-scale detection | `gl-cf25-ltr-96.2-*` against `gl-cf25-ltr-1-*` | Ratio 0.962 within 0.001 | **Pass** at both resolutions: 0.96200 at 600 DPI and 0.96201 at 300, by area |

The registration residual over marker corners is reported, not gated, per DESIGN.md section 21.

## 2. Every scan

The shipped pipeline: homography, and the edge-fit bull locator. The centroid column is the preliminary method through the same registration, because the choice between them is a result of its own (section 4.2). Inches throughout.

| Scan | DPI | Markers | Residual RMS / max | Bull mean / worst, edge fit | Paper gate | Bull mean / worst, centroid |
|---|---|---|---|---|---|---|
| `gl-cf25-ltr-1` | 600 | 34/34 | 0.00217 / 0.00507 | 0.00128 / 0.00251 | pass | 0.00223 / 0.00393 |
| `gl-cf25-ltr-2` | 600 | 34/34 | 0.00219 / 0.00561 | 0.00145 / 0.00316 | pass | 0.00217 / 0.00431 |
| `gl-cf25-ltr-3` | 600 | 34/34 | 0.00211 / 0.00447 | 0.00125 / 0.00290 | pass | 0.00212 / 0.00425 |
| `gl-cf25-ltr-96.2` | 600 | 34/34 | 0.00222 / 0.00519 | 0.00142 / 0.00260 | pass | 0.00165 / 0.00383 |
| `gl-cf25-ltr-d-blank` | 600 | 34/34 | 0.00259 / 0.00719 | 0.00119 / 0.00219 | pass | 0.00237 / 0.00519 |
| `gl-cf25-ltr-d-filled` | 600 | 34/34 | 0.00265 / 0.00722 | 0.00141 / 0.00247 | pass | 0.00217 / 0.00459 |
| `gl-lr300-t-1` | 600 | 9/9 | 0.00181 / 0.00505 | 0.00203 / 0.00254 | pass | 0.00162 / 0.00323 |
| `gl-lr300-t-2` | 600 | 9/9 | 0.00165 / 0.00445 | 0.00193 / 0.00267 | pass | 0.00201 / 0.00343 |
| `gl-lr300-t-3` | 600 | 9/9 | 0.00144 / 0.00309 | 0.00221 / 0.00319 | pass | 0.00236 / 0.00279 |
| `gl-lr300-t-4` | 600 | 9/9 | 0.00195 / 0.00424 | 0.00192 / 0.00314 | pass | 0.00265 / 0.00589 |
| `gl-cf25-ltr-2-rot180` | 600 | 34/34 | 0.00213 / 0.00533 | 0.00140 / 0.00268 | not gated | 0.00308 / 0.00658 |
| `gl-cf25-ltr-1` | 300 | 34/34 | 0.00189 / 0.00419 | 0.00126 / 0.00250 | not gated | 0.00280 / 0.00495 |
| `gl-cf25-ltr-2` | 300 | 34/34 | 0.00193 / 0.00429 | 0.00136 / 0.00289 | not gated | 0.00219 / 0.00511 |
| `gl-cf25-ltr-3` | 300 | 34/34 | 0.00193 / 0.00432 | 0.00131 / 0.00312 | not gated | 0.00234 / 0.00616 |
| `gl-cf25-ltr-96.2` | 300 | 34/34 | 0.00175 / 0.00392 | 0.00141 / 0.00243 | not gated | 0.00209 / 0.00432 |
| `gl-cf25-ltr-d-blank` | 300 | 34/34 | 0.00206 / 0.00447 | 0.00130 / 0.00216 | not gated | 0.00301 / 0.00492 |
| `gl-cf25-ltr-d-filled` | 300 | 34/34 | 0.00246 / 0.00577 | 0.00145 / 0.00228 | not gated | 0.00268 / 0.00514 |
| `gl-lr300-t-1` | 300 | 9/9 | 0.00178 / 0.00369 | 0.00211 / 0.00311 | not gated | 0.00320 / 0.00498 |
| `gl-lr300-t-2` | 300 | 9/9 | 0.00189 / 0.00433 | 0.00227 / 0.00279 | not gated | 0.00286 / 0.00432 |
| `gl-lr300-t-3` | 300 | 9/9 | 0.00180 / 0.00352 | 0.00244 / 0.00358 | not gated | 0.00352 / 0.00555 |
| `gl-lr300-t-4` | 300 | 9/9 | 0.00189 / 0.00380 | 0.00224 / 0.00316 | not gated | 0.00231 / 0.00483 |

Every tile is identified from its markers alone, and each matches its file name. The ink spread the edge fit solves for is 0.022 to 0.025 mm per edge at 600 DPI on every scan, and 0.005 to 0.012 mm at 300.

## 3. Every photograph, and why the gate fails

All four frames are the ultrawide lens, 2.20 mm at f/2.2 (notes entry 6). The sheet lies loose rather than held flat, so under DESIGN.md section 21 [r5] these frames are reported and not gated. Scoring bulls and sighters are given separately.

| Photograph | Markers | Residual RMS / max | Homography alone, RMS | Lens k1 / k2 | Distortion at the frame edge | Bull mean, edge fit | Worst scoring bull | Scoring bulls over the gate | Worst sighter | Sighters over the gate | Photograph gate |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `20260913_130543` | 34/34 | 0.00329 / 0.00920 | 0.00357 | +0.0097 / -0.0236 | 106 px = 1.029 in | 0.00231 | 0.00569 at 15 | 2 of 25 | 0.00383 at S3 | 0 of 3 | fail |
| `20260913_130550` | 34/34 | 0.00331 / 0.00883 | 0.00391 | -0.0020 / -0.0043 | 34 px = 0.263 in | 0.00252 | 0.00678 at 15 | 1 of 25 | 0.00876 at S3 | 1 of 3 | fail |
| `20260913_130554` | 34/34 | 0.00370 / 0.00942 | 0.00387 | +0.0049 / -0.0100 | 42 px = 0.370 in | 0.00309 | 0.00597 at 11 | 3 of 25 | 0.01083 at S3 | 3 of 3 | fail |
| `20260913_130559` | 34/34 | 0.00248 / 0.00560 | 0.00297 | +0.0034 / -0.0091 | 42 px = 0.171 in | 0.00198 | 0.00338 at 15 | 0 of 25 | 0.00922 at S3 | 2 of 3 | fail |

The diagnosis below was measured before the detection changes of section 4.1, when these frames registered from 33, 32, 34 and 34 markers; every frame now registers from all 34, and the figures that moved are quoted at their current values.

**The code is not the limit.** On a synthetic photograph built from the render, quarter-turned, with keystone perspective and radial distortion of the model's own form at three strengths, the same pipeline recovers the distortion coefficients to within 0.0007 and every bull to 0.00038 in worst (`LensModelSyntheticTests`), inside test 43's gate. Under the strongest distortion a plain homography would leave 0.020 in RMS.

**The printer is not the limit.** The photographs' bull fields correlate with sheet 1's own 600 DPI scan field at -0.07 to +0.33, where scans of different sheets correlate at +0.84 to +0.94.

**No global model rescues them.** Three radial coefficients with a free distortion centre, and a quadratic or cubic warp over the homography, were each evaluated leaving one marker out. None improves the corner residual consistently, and the best worst bull any of them reaches is 0.0050, 0.0062, 0.0063 and 0.0076 in.

**Two causes explain most of it.**

1. **The sighters sit outside the marker lattice.** The lowest marker row of `GL-CF25-LTR` is at y = 8.854 in and the sighter row at 9.902 in, so the three sighters are the only extrapolated bulls on the sheet. The worst sighter is worse than the worst scoring bull on three of the four photographs, and S3 is the worst bull on the scans of sheets 2 and 3. Section 4.4 has the geometry.
2. **The sheet is not one plane.** Registering each bull from only its nearest 6 or 8 markers takes the worst scoring bull from 0.0053 to 0.0035 in, 0.0066 to 0.0029 and 0.0034 to 0.0021 on three photographs; the fourth stays at 0.0057. The sighters stay at 0.008 to 0.013 in, because any choice of markers extrapolates to them. In every frame the sheet lies loose on a table, where `docs/PHASE0-PRINT-PROTOCOL.md` section 7 asks for it to be pinned to a wall.

**The scoring bulls do not pass on their own either.** Under the shipped whole-sheet registration the worst scoring bull is 0.00569, 0.00678, 0.00597 and 0.00338 in, so three photographs fail on scoring bulls as well as on sighters. Both causes are real, and neither alone explains the failure. The wall photographs of notes entry 6 were taken to separate them; section 3a reports why they cannot.

The lens is real but modelled: the fitted distortion at the outermost marker is 0.004 to 0.011 in, and the lens term lowers the corner RMS by 7 to 16 percent. The frames are the ultrawide: `FocalLength` 2.20 mm and `FNumber` 2.2 match `ultrawide1-3`, and only the app-computed `FocalLengthIn35mmFilm` of 23 disagrees (notes entry 6). The pipeline now reads `FNumber` and names a lens by the two physical tags.

## 3a. The wall photographs of sheet 3, by lens

Notes entry 6: nine frames of sheet 3, three per lens, to separate the lens, flatness and the sighter geometry. Under DESIGN.md section 21 [r5] they are measured against the mounted gate, which belongs to Phase 1 and is expected to fail until a surface model exists. Shipped pipeline, edge fit, inches. "Corners kept" is how many corners the fit keeps within 0.01 in; the residual is given over those and over every matched corner.

| Lens | Photograph | Markers | Corners kept | Residual RMS, kept / all corners | Homography alone, RMS | Bull mean | Worst scoring bull | Scoring bulls over the gate | Worst sighter | Sighters over the gate | Photograph gate |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Ultrawide, 2.20 mm f/2.2 | `ultrawide1` | 34/34 | 68 of 136 | 0.00420 / 0.02449 | 0.01829 | 0.01224 | 0.03902 at 21 | 14 of 25 | 0.07450 at S1 | 3 of 3 | fail |
| | `ultrawide2` | 34/34 | 46 of 136 | 0.00436 / 0.03756 | 0.01943 | 0.01981 | 0.08305 at 5 | 19 of 25 | 0.07438 at S1 | 3 of 3 | fail |
| | `ultrawide3` | 32/34 | 51 of 128 | 0.00547 / 0.07041 | 0.01689 | 0.02270 | 0.11331 at 21 | 18 of 25 | 0.10643 at S2 | 3 of 3 | fail |
| Main, 6.25 mm f/1.7 | `main1` | 34/34 | 94 of 136 | 0.00501 / 0.01472 | 0.01370 | 0.00860 | 0.01824 at 21 | 8 of 25 | 0.05183 at S1 | 3 of 3 | fail |
| | `main2` | 26/34 | 32 of 104 | 0.00494 / 0.05665 | 0.02028 | 0.01980 | 0.08900 at 5 | 22 of 25 | 0.06950 at S1 | 3 of 3 | fail |
| | `main3` | 27/34 | 58 of 108 | 0.00535 / 0.02374 | 0.01567 | 0.01881 | 0.06540 at 21 | 20 of 25 | 0.11379 at S1 | 3 of 3 | fail |
| Telephoto, 7.00 mm f/2.4 | `telephoto1` | 16/34 | | | | | | | | | excluded: the sheet overflows the frame |
| | `telephoto2` | 33/34 | 38 of 132 | 0.00622 / 0.03080 | 0.01975 | 0.01955 | 0.04584 at 21 | 21 of 25 | 0.09626 at S1 | 3 of 3 | fail |
| | `telephoto3` | 4/34 | | | | | | | | | excluded: the sheet overflows the frame |

The table frames of section 3 keep 135 or 136 corners of 136, with at most 0.0038 in RMS over all of them.

**The gate does not track focal length.** The worst bull is 0.075 to 0.113 in on the ultrawide, 0.052 to 0.114 on the main camera and 0.096 on the telephoto. The telephoto, the longest lens and the one whose distortion should be smallest, is no better than the ultrawide.

**These frames are not of a flat sheet.** In every frame the sheet hangs from a single pin at the top centre and its edges are visibly curved; nothing holds its lower half to the wall. The data say the same: no single plane-plus-lens model fits the corners. The misfit grows towards the free bottom edge: the two lowest marker rows, y = 2200 and 2300 dmm, sit 0.026 to 0.138 in RMS from the fit on every frame, where the table frames are 0.0025 to 0.0033 in at the same rows. `telephoto2`, on the longest lens, needs 0.020 in RMS from a homography alone.

**So the set cannot answer the three questions entry 6 set it.**

1. **Lens.** Every lens fails by 0.05 to 0.11 in, the size of the surface misfit, which hides a lens effect the size of section 3's, 0.004 to 0.011 in. What the set shows is that focal length does not rescue a curved sheet.
2. **Flatness.** The paired comparison runs the other way from the one intended: the table frames are the flatter set, and `ultrawide1-3` are 7 to 20 times worse by worst bull than `20260913_1305*` with the same lens.
3. **Sighter geometry.** Scoring bulls fail on every frame, 8 to 22 of 25, so the sighters cannot be isolated. A sighter is still the worst bull on four of the seven frames.

**Detection on these frames.** The pipeline matches 34, 26 and 27 markers on `main1-3`, 34, 34 and 32 on `ultrawide1-3`, and 16, 33 and 4 on `telephoto1-3`. Entry 6's scratch counts differ on seven of the nine. `main2` loses markers at the far edge of the sheet, which is visibly out of focus at f/1.7. The canonical-cell change of section 4.1 was measured on these frames and on all 21 scans before it shipped.

Notes entry 8 answers question 5: the protocol said "pin or tape", which is not flat, and `docs/PHASE0-PRINT-PROTOCOL.md` section 7 now asks for all four edges restrained. Two frames of a flat sheet are the one open item of the spike. **The wall set stays committed as the baseline a Phase 1 registration of a curved sheet has to beat** (section 4.5): a realistic curvature, under the shipped global registration, worst scoring bull 0.018 to 0.113 in, 8 to 22 of 25 scoring bulls over the gate, and 0.015 to 0.070 in corner RMS over all corners, every row in `scans/phase0/measurements/photos.json`.

## 3b. The flat photographs of sheet 3: Phase 0's photograph gate

Notes entry 10: `main_flat1-3`, sheet 3 lying flat, the main camera at 6.25 mm f/1.7, camera originals. No glass was available, so the sheet is flat by lying on a table rather than restrained. Shipped pipeline, edge fit, inches. Every row is in `scans/phase0/measurements/photos.json`.

| Photograph | Keystone (notes entry 10) | Markers | Corners kept | Residual RMS, kept / all corners | Homography alone, RMS | Lens k1 / k2 | Bull mean | Worst scoring bull | Scoring bulls over the gate | Worst sighter | Sighters over the gate | Flat gate |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `main_flat1` | 0.997, square on | 34/34 | 136 of 136 | 0.00270 / 0.00270 | 0.00718 | -0.0512 / +0.0659 | 0.00187 | 0.00343 at 21 | **0 of 25** | 0.00661 at S1 | 1 of 3 | fail |
| `main_flat2` | 0.926 | 25/34 | 100 of 100 | 0.00315 / 0.00315 | 0.00587 | -0.0436 / +0.0531 | 0.00270 | 0.00566 at 13 | 2 of 25 | 0.01016 at S1 | 2 of 3 | fail |
| `main_flat3` | 0.802 | 23/34 | 91 of 92 | 0.00395 / 0.00424 | 0.00945 | -0.0721 / +0.1058 | 0.00392 | 0.01183 at 1 | 6 of 25 | 0.00496 at S1 | 0 of 3 | fail |

**These frames are flat.** The fit keeps every corner, or all but one, and the residual over all corners equals the residual over those kept, where the pinned frames keep 32 to 94 of 104 to 136.

**With every marker decoded, the scoring bulls pass and only a sighter fails.** On `main_flat1` all 25 scoring bulls are inside the gate, worst 0.00343 in, and S1, at 0.00661 in, is the only bull over it. S1 lies outside the marker lattice, on the geometry that section 4.4 identifies and the deferred commit fixes. This is the case entry 6's third question asked for: a flat sheet, a well-behaved lens, and only a sighter failing.

**Every other failure is a bull outside the markers that were decoded, or within a thousandth of the gate.**

| Photograph | Bull | Error | Where it lies |
|---|---|---|---|
| `main_flat1` | S1 | 0.00661 | outside the lattice: the sighter row |
| `main_flat2` | S1, S2 | 0.01016, 0.00759 | outside the lattice: the sighter row |
| `main_flat2` | 13, 18 | 0.00566, 0.00509 | inside |
| `main_flat3` | 1, 2, 4, 5 | 0.01183, 0.00573, 0.00939, 0.01047 | outside the decoded markers: the top marker row was lost |
| `main_flat3` | 6, 13 | 0.00529, 0.00526 | inside |

**The lost markers are at the far edge, and out of focus.** `main_flat2` loses 9 markers along the far right side and `main_flat3` 11 across the far top rows; every one but one had a candidate quad at its position that did not decode. At f/1.7 the depth of field does not cover a sheet this far off-axis, and the far rows of `main_flat3` are visibly soft. Entry 10's own counts are 34, 29 and 23. The loss matters because it moves the lattice: the top row of `main_flat3` goes, and the five top scoring bulls become extrapolated, four of them past the gate. Decoding a defocused, foreshortened marker is a detection requirement for Phase 1, beside the surface model; it was not tuned against these three frames, which would fit the detector to the gate.

**The lens is modelled and is not the limit.** The main camera's fitted distortion is consistent across frames, k1 -0.044 to -0.073 and k2 +0.053 to +0.106 on the flat frames and `main1`, and it is real: a homography alone leaves 0.0059 to 0.0095 in RMS, and the lens term brings it to 0.0027 to 0.0042 over all corners. `main_flat3`'s 4.9 in of distortion at the frame edge is the polynomial extrapolated past the markers into an empty half of the frame, not a measurement.

**Entry 6's three questions, answered.** Lens: not the limit on a flat sheet, since the frame with every marker passes every scoring bull. Flatness: the dominant cause, an order of magnitude, 0.052 to 0.114 in pinned against 0.0066 to 0.0118 in flat with the same sheet and lens. Sighter geometry: confirmed on a flat sheet rather than inferred.

## 4. Findings

### 4.1 OpenCV misread inkjet markers it had found, and missed some it could have

On the first run OpenCV decoded 30 to 33 of 34 markers on the reference sheets and 5 to 8 of 9 on the tiles at 600 DPI, where the preliminary measurement had 34 of 34, and tiles 2, 3 and 4 failed the paper gate on that alone.

**Every missing marker had a candidate quad at its position that did not decode.** OpenCV reads bits from a canonical image of 4 pixels per cell, so a 94 px marker is resampled to 32 px and the printed black's inkjet texture aliases into wrong bits. At 8 or 12 pixels per cell the same scans read 9, 9 and 34. Error-correction rate, border tolerance, Otsu floor and threshold window changed nothing. The backend first sized the canonical cell from the module in pixels, clamped to 4 to 16. That still lost a marker on the 300 DPI scans of the blank load block and tile 1, and aliased the markers of the wall photographs, whose module is about 5 px. It now uses at least 8 pixels per cell, up to 16, and ignores 0.3 of each cell at its edge where OpenCV ignores 0.13. Over all 21 scans that reads 514 of 514 markers, against 512; margins of 0.13 and 0.3 both read 514, and on `telephoto2` 0.3 reads 33 against 30. On the seven usable wall frames, detection went from 21 to 33 markers per frame to 26 to 34. Stage S3 of the trace says of every missing marker whether it was found and unread or never found.

Measurement 3 then showed the adaptive threshold window capped at half the marker side still lost one marker in 240, and capped at the full side lost none. The backend now caps it at the marker side.

### 4.2 The bull locator was part of the limit

The preliminary centroid is reproduced to within 0.00025 in mean. The edge fit, which locates every declared ring edge along 180 rays and fits a centre and an ink spread to them, was validated on the synthetic raster first, where it recovers every bull to 0.00022 in worst at 300 DPI and 0.00013 at 600, against the centroid's 0.00069 and 0.00025. On paper it lowers the mean by 40 to 45 percent on the reference sheets and halves the random component (measurement 6). It ships. The centroid would have failed the paper gate on the blank load block and on tile 4. `docs/PHASE0-PRELIM.md` and DESIGN.md section 21 carry dated amendments.

### 4.3 The corner residual chooses wrongly, again and again

Contour refinement fits a better paper residual than subpixel and a worse worst bull, and no refinement at all gives the best worst bull at 600 DPI (measurement 2). libapriltag's residual ranks against its bull error on the external comparison (measurement 4). The paper residual barely moves above sixteen markers while the worst bull keeps improving (measurement 1). Each is further evidence for gating the bull and reporting the residual.

### 4.4 The sighter row misses the marker lattice, and the fix is deferred

On `GL-CF25-LTR` the fiducial lattice would place a marker row at y = 2705, below the sighters, but its box reaches 2735 against the half-safe-margin limit of 2734, so the row is dropped by 1 dmm. `docs/NOTES-FROM-PLANNING.md` entry 5 sets the fix: shorten the sighter gap where the lattice does not bracket the sighters, and declare `cells.sighterGap`. Sweeping the gap in `tools/layout/layout.py` reproduces entry 5's figures exactly.

| Sheet | Default gap (dmm) | Gap with a marker row below the sighters | Markers |
|---|---|---|---|
| GL-CF25-LTR | 456 | **454** | 34 to 38 |
| GL-LR300-R24 | 1219 | 1142 | 35 to 40 |
| GL-LR300-R36 | 1219 | 1142 | 48 to 56 |

The other sheets with sighters bracket them already. **The change is identified, costed and deferred**, per entry 5: the sample set was printed from the current definitions, so every table in this document is measured against the geometry that was printed, and the change lands in its own commit after Phase 0 reports. It gives the three sheets new identifiers and moves the `GL-CF25-LTR` scoring rows by 1 dmm.

**Measuring the general rule turned up more than the sighters.** On `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` the outermost bull columns also sit outside the lattice, by 200, 508 and 508 dmm, through the same edge-margin drop applied to a column; a sighter gap cannot fix that. On the two tiles the outermost markers share coordinates with the outermost bulls. The proposed TARGET-SCHEMA.md section 7 wording, its conformance test, and the decisions these findings leave are `docs/QUESTIONS-FOR-PLANNING.md` question 4, together with a gap in GLTD-B, which does not carry `sighterGap`.

**Those findings are the same defect at the same scale, not a technicality** (notes entry 9). The outermost bull columns lie 200 dmm outside the lattice on `GL-CF25-100M-A4` and 508 dmm outside on the two rolls, against the 266 dmm by which the `GL-CF25-LTR` sighters lie outside it. The defect was found once, by accident, through photographs of one sheet; the sweep found it on three more that nobody has photographed. That is the strongest argument for the rule.

**What lands now and what waits.** TARGET-SCHEMA.md section 7 carries the rule and section 10 test 26f, inclusive, as a warning. The validator flags 3 bulls on `GL-CF25-LTR`, 10 on `GL-CF25-100M-A4`, 13 on `GL-LR300-R24` and 11 on `GL-LR300-R36`, and nothing on any other sheet; the tiles conform at a margin of zero. Test 23 no longer warns where the shortened gap is what brings the sighter row inside the lattice, so a definition decoded from a sheet's codes needs no `sighterGap` and GLTD-B is unchanged. **The four sheets change in one geometry commit after Phase 0 closes, and test 26f becomes an error in that commit.**

### 4.5 Phase 1 requirement: registering a sheet that is not flat

Notes entries 8 and 10, and DESIGN.md section 21 [r5]. A target stapled to a board is the application's actual input: it bows between its fixings, curls at a free edge and moves in wind. The mounted photograph gate is the product requirement, and the flat gate is the control.

**The model is a developable surface fit**, as DESIGN.md section 6 already states: paper bends without stretching, so distance along the sheet is preserved where the projection is not planar. That is a stronger constraint than a generic warp, and the spike's evidence points at a constraint rather than more freedom: three radial coefficients with a free centre, and quadratic and cubic warps evaluated leaving one marker out, brought no frame inside the gate.

**Local or piecewise registration from nearby markers is the fallback** if the constrained fit proves impractical. What it bought on the table photographs, registering each bull from its nearest 6 or 8 markers instead of the whole sheet, inches:

| Photograph | Worst scoring bull, whole sheet | Worst scoring bull, nearest markers |
|---|---|---|
| `20260913_130543` | 0.0053 | 0.0035 |
| `20260913_130550` | 0.0066 | 0.0029 |
| `20260913_130554` | 0.0058 | 0.0057 |
| `20260913_130559` | 0.0034 | 0.0021 |

These were measured with a scratch diagnostic, before the detection changes of section 4.1, and Phase 1 re-measures them in the pipeline. **Its limit is the sighters**: they stayed at 0.008 to 0.013 in whichever markers were chosen, because no nearby marker brackets them. A local registration cannot help a bull outside the lattice, which is section 4.4 reached from a third direction; a developable fit constrains the surface beyond the markers, but a sighter outside the lattice is still extrapolated.

**The benchmark is the nine pinned frames**, seven measured and two excluded as overflowing the frame, and the figures to beat are section 3a's: worst scoring bull 0.018 to 0.113 in, 8 to 22 of 25 scoring bulls over the gate, 0.015 to 0.070 in corner RMS over all corners. Before entry 52 sorted the markers they read 0.015 to 0.091 in, 8 to 21 and 0.014 to 0.060 in. The frames, the markers and the corners are the same; only their order changed, so the difference measures how unstable registration is on these frames, not a harder benchmark. The flat frames of section 3b are the control a surface fit must not make worse.

## 5. The measurements of the brief, section 6

### Measurement 1: residual and worst bull against marker count

Random subsets of the matched markers, 40 per count, refitted by homography; bull centres located once with every marker and mapped through each fit. Median and 90th percentile across subsets, 600 DPI, inches. The full table, with 300 DPI and every count, is `grouplab spike markers`.

| Sheet | Markers used | Residual RMS, median / p90 | Worst bull, median / p90 | Mean bull, median |
|---|---|---|---|---|
| sheet 1 | 4 | 0.00551 / 0.02642 | 0.01097 / 0.04192 | 0.00357 |
| sheet 1 | 6 | 0.00281 / 0.00586 | 0.00489 / 0.01145 | 0.00211 |
| sheet 1 | **9** | 0.00237 / 0.00263 | **0.00364 / 0.00451** | 0.00153 |
| sheet 1 | 16 | 0.00225 / 0.00232 | 0.00294 / 0.00351 | 0.00134 |
| sheet 1 | 34 | 0.00217 | 0.00251 | 0.00128 |
| sheet 2 | 4 | 0.00679 / 0.01719 | 0.01538 / 0.03507 | 0.00425 |
| sheet 2 | 6 | 0.00280 / 0.00397 | 0.00475 / 0.00875 | 0.00195 |
| sheet 2 | **9** | 0.00246 / 0.00281 | **0.00391 / 0.00514** | 0.00151 |
| sheet 2 | 16 | 0.00231 / 0.00245 | 0.00341 / 0.00478 | 0.00150 |
| sheet 2 | 34 | 0.00219 | 0.00316 | 0.00145 |
| sheet 3 | 4 | 0.00396 / 0.01388 | 0.00782 / 0.02822 | 0.00297 |
| sheet 3 | 6 | 0.00253 / 0.00375 | 0.00395 / 0.00863 | 0.00163 |
| sheet 3 | **9** | 0.00235 / 0.00266 | **0.00308 / 0.00467** | 0.00140 |
| sheet 3 | 16 | 0.00220 / 0.00228 | 0.00303 / 0.00410 | 0.00128 |
| sheet 3 | 34 | 0.00211 | 0.00290 | 0.00125 |

**Nine markers, the real tiles.** The four GL-LR300-T tiles register from their nine printed markers at 0.00254 to 0.00319 in worst bull at 600 DPI and 0.00279 to 0.00358 at 300, every one inside the paper gate. Nine well-spread markers do better than nine random ones, whose 90th percentile reaches 0.0045 to 0.0051. Four markers are not enough; beyond sixteen the gain is small.

### Measurement 2: corner refinement

Paper, sheets 1 to 3; residual and bull mean are the mean over the sheets, the worst bull is the worst on any. The shipped window is 5 px capped at one module, which is 0.42 modules at 600 DPI and 0.85 at 300.

| DPI | Refinement | Residual RMS | Residual max | Bull mean | Worst bull |
|---|---|---|---|---|---|
| 600 | none | 0.00255 | 0.00643 | 0.00131 | 0.00279 |
| 600 | contour | 0.00194 | 0.00405 | 0.00137 | 0.00338 |
| 600 | subpix, shipped | 0.00216 | 0.00505 | 0.00133 | 0.00316 |
| 600 | subpix, 0.25 module | 0.00252 | 0.00588 | 0.00129 | 0.00316 |
| 600 | subpix, 0.5 module | 0.00202 | 0.00442 | 0.00132 | 0.00316 |
| 600 | subpix, 1 module | 0.00169 | 0.00407 | 0.00126 | 0.00335 |
| 600 | subpix, 1.5 modules | 0.00408 | 0.00882 | 0.00136 | 0.00290 |
| 600 | subpix, 2 modules | 0.00628 | 0.01057 | 0.00395 | 0.01169 |
| 300 | none | 0.00284 | 0.00714 | 0.00134 | 0.00358 |
| 300 | contour | 0.00176 | 0.00410 | 0.00140 | 0.00357 |
| 300 | subpix, shipped | 0.00192 | 0.00427 | 0.00131 | 0.00312 |
| 300 | subpix, 0.25 module | 0.00328 | 0.00808 | 0.00135 | 0.00403 |
| 300 | subpix, 0.5 module | 0.00214 | 0.00485 | 0.00135 | 0.00328 |
| 300 | subpix, 1 module | 0.00186 | 0.00417 | 0.00130 | 0.00305 |
| 300 | subpix, 1.5 modules | 0.00392 | 0.00799 | 0.00137 | 0.00266 |
| 300 | subpix, 2 modules | 0.00643 | 0.01090 | 0.00406 | 0.00925 |

Synthetic, where truth is known: corner error against truth in pixels, with its radial component about the marker centre.

| DPI | Refinement | Corner error RMS (px) | Radial bias (px, + outward) | Worst bull (in) |
|---|---|---|---|---|
| 600 | none | 0.641 | -0.339 | 0.00032 |
| 600 | contour | 0.584 | -0.445 | 0.00013 |
| 600 | subpix, shipped | 0.158 | -0.136 | 0.00012 |
| 600 | subpix, 0.5 module | 0.140 | -0.117 | 0.00012 |
| 600 | subpix, 1 module | 0.246 | -0.174 | 0.00015 |
| 600 | subpix, 1.5 modules | 2.326 | -1.668 | 0.00101 |
| 300 | none | 0.670 | -0.340 | 0.00027 |
| 300 | contour | 0.603 | -0.403 | 0.00020 |
| 300 | subpix, shipped | 0.163 | -0.119 | 0.00029 |
| 300 | subpix, 0.25 module | 0.800 | -0.465 | 0.00159 |
| 300 | subpix, 0.5 module | 0.214 | -0.197 | 0.00027 |
| 300 | subpix, 1 module | 0.226 | -0.179 | 0.00035 |
| 300 | subpix, 1.5 modules | 1.231 | -0.907 | 0.00118 |

**Which regime the difference lives in.** Corner accuracy depends on the window in both regimes the same way: best between about 0.4 and 1 module, and broken from 1.5 modules, where the window reaches the next module's edge. **The bull does not.** On paper the worst bull stays at 0.00279 to 0.00338 in at 600 DPI for every setting short of 2 modules, refinement off included, and at 2 modules reaches 0.01169 in, so the preliminary measurement's "the window makes no difference" is true of what the gate measures and false of the residual. Its flat residual across 3 to 24 px is what recent OpenCV produces when only the pixel window is set, because `relativeCornerRefinmentWinSize` then caps the effective window at 0.3 of a module, about 3.5 px at 600 DPI; that is the likely explanation rather than a confirmed one, since the preliminary run's OpenCV version is not recorded. The Phase 0a synthetic result, one module better than 0.3, is the corner regime; the paper result is the bull regime. **The inward corner bias is the detector's**: it is present on the synthetic raster, whose edges are exact, at -0.12 to -0.20 px under subpixel refinement. The shipped window is kept. **The 1.5-module row changed under entry 101.** It read 0.00517 in worst on paper, over the gate on one sheet, and now reads 0.00290. Its corner error against truth is unchanged to the third decimal, 2.326 px against 2.327, so the change is in the homography's final fit, which OpenCV stops after ten iterations and the managed fit iterates to convergence (`docs/PHASE1-RESULTS.md` "Entry 101"). A window reaching the next module's edge breaks the corners at 1.5 modules and the bull only at 2.

### Measurement 3: adaptive threshold window and downsampling

The 600 DPI scans of all ten sheets, 240 markers, with the edge-fit locator; the native 300 DPI scans of the same sheets for comparison. The window is in the pixels detection runs on, so a downsampled row's window is in reduced pixels.

| Threshold window max | Downsample | Markers matched | Sheets with every marker | Detection time, mean (ms) | Residual RMS, mean (in) | Worst bull, any sheet (in) | Sheets inside the paper gate |
|---|---|---|---|---|---|---|---|
| 7 px | none | 0 of 240 | 0 of 10 | 61 | | | 0 of 10 |
| 15 px | none | 225 of 240 | 1 of 10 | 73 | 0.00201 | 0.00357 | 10 of 10 |
| 23 px, OpenCV's default | none | 238 of 240 | 8 of 10 | 74 | 0.00206 | 0.00319 | 10 of 10 |
| 35 px | none | 238 of 240 | 8 of 10 | 71 | 0.00206 | 0.00320 | 10 of 10 |
| 49 px, half the marker side | none | 239 of 240 | 9 of 10 | 73 | 0.00207 | 0.00319 | 10 of 10 |
| 71 px | none | 240 of 240 | 10 of 10 | 77 | 0.00208 | 0.00319 | 10 of 10 |
| **95 px, the marker side, shipped** | none | **240 of 240** | **10 of 10** | 72 | 0.00208 | 0.00319 | 10 of 10 |
| the marker side | 2x | 240 of 240 | 10 of 10 | 25 | 0.00177 | 0.00323 | 10 of 10 |
| the marker side | 3x | 239 of 240 | 9 of 10 | 16 | 0.00167 | 0.00324 | 10 of 10 |
| the marker side, native 300 DPI scans | none | 240 of 240 | 10 of 10 | 21 | 0.00194 | 0.00358 | 10 of 10 |

**The window should reach the marker's side**, which ships: 71 and 95 px find every marker on every sheet, and OpenCV's default loses two. **Downsampling is not adopted, and it is no longer a detection problem.** Before the canonical-cell change of section 4.1 it lost 8 and 32 of 240 markers; with it, 2x finds all 240 and 3x finds 239, three to five times faster. The worst bull on any sheet differs by 0.00005 in, so the gate cannot choose between them; the corner residual is lower on the downsampled rows, which section 4.3 says is not a reason. Full resolution ships because every other table here was measured at it; 2x is the measured option if detection time ever matters. A native 300 DPI scan detects every marker too. Since entry 101 the sub-pixel refinement is managed code, which adds 10 to 16 ms per 600 DPI sheet wherever markers are found; the 7 px row, which finds none, moved 3 ms. Compare times within one run only.

### Measurement 4: OpenCV against the AprilTag reference detector

Recorded in `docs/FIDUCIAL-DECISION.md` section 10, where it is measurement 8. Run externally on the same scans with a scratch centroid, libapriltag fitted the better corner residual and the worse bull on five images of five. Re-run through this pipeline with libapriltag's committed corners, edge fit, inches:

| Image | OpenCV, shipped | libapriltag, as returned | libapriltag, minus half a pixel |
|---|---|---|---|
| `gl-cf25-ltr-1-600` | 0.00128 / 0.00251 | 0.00099 / 0.00196 | 0.00135 / 0.00259 |
| `gl-cf25-ltr-2-600` | 0.00145 / 0.00316 | 0.00106 / 0.00245 | 0.00145 / 0.00331 |
| `gl-cf25-ltr-3-600` | 0.00125 / 0.00290 | 0.00115 / 0.00280 | 0.00140 / 0.00334 |
| `gl-cf25-ltr-2-600-rot180` | 0.00140 / 0.00268 | 0.00248 / 0.00389 | 0.00137 / 0.00280 |
| `gl-cf25-ltr-1-300` | 0.00126 / 0.00250 | 0.00161 / 0.00266 | 0.00139 / 0.00267 |

Bull mean / worst per cell. The corner residuals of the two detectors are within 0.00007 in RMS of each other on every image.

**libapriltag's corners sit half a pixel from OpenCV's.** For the same markers the mean offset is +0.34 to +0.59 px in x and +0.37 to +0.54 in y, the same in pixels at 300 DPI as at 600, which makes it a pixel-convention difference rather than a difference in where the detectors put the ink edge. A constant image-space offset moves every bull by the same image vector, which in page coordinates points one way on an upright scan and the opposite way on the rotated one: that is why libapriltag as returned looks better than OpenCV on the three upright sheets and worse on the rotated rescan. **With the half pixel removed, the two detectors are tied**: OpenCV is ahead on three images, level on one and behind on one, and no mean differs by more than 0.00015 in. **Neither ranking, the external one or this one, is about the detectors until that convention is corrected**, and it is a third corner-convention trap, beside the two entry 2 records.

### Measurement 5: print-scale detection

| DPI | Sheet 1 scale x / y / area | 96.2 percent sheet x / y / area | Ratio x / y / area | Within 0.001 of 0.962 |
|---|---|---|---|---|
| 600 | 1.00006 / 1.00062 / 1.00034 | 0.96218 / 0.96246 / 0.96232 | 0.96212 / 0.96187 / 0.96200 | yes |
| 300 | 1.00012 / 1.00056 / 1.00034 | 0.96216 / 0.96250 / 0.96233 | 0.96205 / 0.96196 / 0.96201 | yes |

The printer's own scale, x 1.0001 and y 1.0006, cancels in the ratio, as `docs/PHASE0-PRINT-PROTOCOL.md` section 5.1 intended.

### Measurement 6: the systematic and random split of the displacement field

Sheets 1 to 3 at 600 DPI. Systematic is the per-bull mean over the three sheets; random is the deviation from it, as an unbiased per-bull RMS. The smooth share is what a quadratic over the page absorbs from the systematic field.

| Locator | Single sheet mean / worst | Systematic mean / worst | Random RMS | Smooth share of systematic, RMS | Systematic after the smooth share, mean / worst |
|---|---|---|---|---|---|
| Edge fit, shipped | 0.00132 / 0.00316 | 0.00128 / 0.00268 | 0.00048 | 0.00137 | 0.00039 / 0.00071 |
| Centroid | 0.00217 / 0.00431 | 0.00201 / 0.00373 | 0.00103 | 0.00149 | 0.00147 / 0.00314 |

| Locator | Sheet 1 against 2 | 1 against 3 | 2 against 3 | Sheet 1, 600 against 300 DPI | Rotated rescan against sheet 2 | Rotated rescan against the scanner-fixed prediction |
|---|---|---|---|---|---|---|
| Edge fit | +0.921 | +0.835 | +0.879 | +0.973 | +0.659 | -0.051 |
| Centroid | +0.824 | +0.812 | +0.875 | +0.890 | +0.805 | -0.274 |

**The field is fixed to the paper under both locators**, as `docs/PHASE0-PRELIM.md` section 5a found. **With the shipped locator the systematic part is 0.0013 in and the random part 0.0005 in**, and **most of the systematic part is smooth**: a quadratic over the page leaves 0.0004 in mean and 0.0007 in worst. A printer whose systematic component were calibrated out would register this sheet to well under a thousandth. Nobody should build that now; this is the size of the prize. The extra error the rotated rescan shows under a centroid, 0.00658 in worst against 0.00431 unrotated, is absent under the edge fit, 0.00268 against 0.00316, so the scanner-fixed component noted in `docs/NOTES-FROM-PLANNING.md` entry 2 belongs to the centroid rather than to the ink.

**Not possible yet: marker module size.** No sheets at other module sizes exist. It stays on the batched list for the next time paper is involved, with the reprint that carries the section 4.4 geometry.

## 6. Raw rows

Notes entry 7. Every `grouplab spike` command writes the values behind its table to `scans/phase0/measurements/<command>.json`, with a `units` field. Page lengths are dmm from the page top-left; image points are pixels in OpenCV's convention.

| File | Per element | Transforms |
|---|---|---|
| `sheets.json` | Every scan: matched and missing marker ids; every corner with marker id, image and page position, error and inlier flag; every bull under both locators with label, scoring flag, declared and recovered position, error and ink spread | The fitted homography, image to page |
| `photos.json` | The same for every photograph, with focal length, f-number and the lens report | Homography with radial distortion: centre, scale, k1, k2 and the normalised-to-page matrix |
| `markers.json` | Measurement 1: every subset's marker ids and seed, its residual over all corners, and every bull's error through it | The tiles' homographies |
| `refinement.json` | Measurement 2: every sheet and variant's corners and bulls on paper; on the synthetic raster, every corner's truth, error and radial component | The synthetic perturbation |
| `threshold.json` | Measurement 3: every sheet and variant's matched ids, detection time, residual and bulls | |
| `scale.json` | Measurement 5: pixels per dmm in x, y and by area, and each ratio | Each sheet's homography |
| `field.json` | Measurement 6: every image's bulls under each locator, the systematic field and its quadratic share per bull, the reflection pairs, and every correlation | Each image's homography |
| `detectors.json` | Measurement 4: per-corner offsets between libapriltag and OpenCV, and each detector's corners and bulls under both locators | Each detector's homography |

## 7. Decision log

One line per method choice where there was a real alternative: what was rejected, and why.

- **Edge fit over the thresholded centroid for bull centres.** Chosen on the synthetic raster, where truth is known, at 0.00022 in worst against 0.00069 at 300 DPI, before it was run on paper; the centroid would fail the paper gate on two sheets.
- **Ink spread fitted per bull, over assuming the declared radii.** Printed edges sit 0.022 to 0.025 mm outside the declared radii at 600 DPI, and a fixed radius biases a centre wherever part of a ring is lost.
- **Subpixel corner refinement at 5 px capped at one module, over none, contour and wider windows.** It gives the best corner accuracy against synthetic truth. On paper the bull is indifferent, so the residual, which prefers contour, was not allowed to decide.
- **Threshold window up to the marker side, over OpenCV's 23 px and half the side.** 240 of 240 markers against 238 and 239, in the same detection time.
- **Canonical cell of at least 8 px with a 0.3 margin, over OpenCV's 4 px and 0.13, and over one pixel per module.** 4 px aliases inkjet texture into wrong bits on scans, and one per module still aliases 5 px photograph modules. Either margin reads 514 of 514 scan markers; 0.3 reads more on `telephoto2`.
- **Full resolution, over detecting on a 2x downsample.** The gate result is identical; full resolution is what every table was measured at, and speed is not a Phase 0 criterion.
- **Area gate inside OpenCV before decoding and side-ratio gate after, over both after.** OpenCV has no side-ratio setting; the area gate, expressed as a minimum perimeter, saves decoding small quads, and a side-ratio rejection is still recorded with its id.
- **Tile inferred from the decoded marker ids, over trusting the file name.** Section 3.7 makes ids unique across the assembly and an application has no file name to trust; the sheets table checks every inference against the name, and every tile matched.
- **Homography for scans and homography with a two-term radial lens for photographs, chosen by the presence of a focal-length tag.** A scan has no lens to fit. On photographs, three terms, a free centre, and quadratic or cubic warps were each no better with one marker left out, and the synthetic lens test recovers the two-term model to 0.00038 in.
- **Photographs registered by RANSAC at 0.05 in, then reclassified at 0.01 in after the lens fit, over one pass at 0.01 in.** The first homography cannot model distortion, so a tight first pass would discard corners the lens term explains.
- **Bull error gated and corner residual reported, over gating the residual.** Refinement, detector choice and marker count each ranked differently by residual than by bull error in this spike, as DESIGN.md section 21 anticipated.
- **The photograph gate counts sighters, over scoring bulls only.** Option C of question 1, rejected by notes entry 5: a sighter is a bull a shooter fires at.
- **Marker-count subsets map bulls located once, over locating bulls again per subset.** It measures the registration without the locator's own variation.
- **Detectors compared with libapriltag's half-pixel convention both removed and raw, over the raw ranking alone.** A constant image offset changes sign on the rotated rescan, which is how it was shown to be a convention and not accuracy.
- **Field correlations over concatenated x and y components, over per-axis or magnitude correlations.** It is the form `docs/PHASE0-PRELIM.md` used, so the figures compare.
- **Local registration from the nearest markers measured as a diagnostic, not shipped.** The brief stops detection at markers, homography and bull location, and local registration cannot help the sighters.
- **Geometry change deferred, over applying it during the spike.** Notes entry 5: the sample set was printed from the current definitions.
- **Bracketing inclusive and a warning first, over strict or an error on landing.** Strict fails both tiles at a margin of zero with no fix short of a denser scheme, and an error would fail four sheets before their geometry is designed (notes entry 9).
- **Test 23 exempts a shortening that brackets, over carrying `sighterGap` in GLTD-B or scoping the test to documents not decoded.** The gap is recoverable from the body, and a shortening that brackets is self-evidently deliberate (notes entry 9).
- **The exemption re-derives the lattice at the conventional gap, over checking only that the sighters are bracketed now.** A gap shortened where the lattice already brackets is not the case the rule describes, so it still warns.
- **The flat gate measured on `main_flat1-3` only, over counting the table frames as flat.** Entry 10 names these as the control; the table frames are a different sheet and lens lying loose, and are reported, not gated.
- **Far-edge marker loss reported, not tuned against.** Adjusting detection until three frames pass would fit the detector to the gate it is measured on.
- **A developable surface fit as the Phase 1 model, over piecewise registration.** Notes entry 10: the spike's failed models had more freedom, not the right constraint; piecewise registration is the fallback.

## 8. Phase 0 verdict

Notes entry 10 closes Phase 0 on the flat photographs. Against DESIGN.md section 21 [r5]:

| Gate | Result |
|---|---|
| Conformance test 43 | **Pass** |
| Paper gate | **Pass**, ten of ten, worst 0.00319 in |
| Print-scale detection | **Pass**, 0.96200 and 0.96201 |
| Photograph gate, flat | **Fail**, three of three, worst 0.00661, 0.01016 and 0.01183 in |
| Photograph gate, mounted | Phase 1's; fails seven of seven, 0.052 to 0.114 in, as expected |

**The flat gate fails, and every failure has a named cause that the spike did not change.** On the one frame that decoded every marker, the scoring bulls pass and the only failure is a sighter outside the marker lattice. The others add bulls made extrapolated by far-edge markers lost to defocus, and four bulls inside the lattice over the gate by at most 0.0007 in. Whether the flat gate passes on a sheet whose lattice brackets its sighters is **not measured**: that needs the geometry commit of section 4.4 and a print of it, and no paper is being used.

**Reproducing these tables.** Every table in this document is measured against the definitions the sample set was printed from, frozen at `targets/frozen/phase0/`: `GL-YCSK-DZZ1-R0VJ-4T5Y` (`GL-CF25-LTR` as printed), `GL-R0T0-384Z-HRBE-M0EW` (`GL-CF25-LTR-D`) and `GL-G8JP-FF4D-AE0T-GPMN` (`GL-LR300-T`, 2 by 2). `grouplab spike` resolves against that directory, not the live library. The geometry change of section 4.4 has since superseded `GL-YCSK-DZZ1-R0VJ-4T5Y` with `GL-20J3-Y141-0BN3-EYME` as the live `GL-CF25-LTR` (`docs/NOTES-FROM-PLANNING.md` entry 13); the other two are not superseded. Rerunning every spike command against the frozen definitions reproduces every measured value in `scans/phase0/measurements/`; only the detection times in `threshold.json` differ, as they do between any two runs.

**What Phase 1 inherits, in the order it bears on the gates.**

1. The geometry commit: `GL-CF25-LTR`, `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36`, with test 26f promoted to an error (section 4.4).
2. Decoding defocused, foreshortened markers at the far edge of an off-axis frame (section 3b).
3. A developable surface fit for the mounted gate, piecewise registration as its fallback, benchmarked on the nine pinned frames (section 4.5).
4. The rest of this document's findings: the edge-fit locator, the canonical cell and threshold window, the corner residual reported rather than gated, and the half-pixel detector convention.

