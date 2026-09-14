# Phase 1 results

**Brief** `docs/PHASE1-BRIEF.md`
**Branch** `phase-1`
**Reproduce** each table with the command named above it

---

## M0. The marker module sweep

`docs/FIDUCIAL-DECISION.md` section 10, measurement 2: the same target printed at 0.3, 0.4, 0.5, 0.6 and 0.8 mm modules, to find the dot-gain floor on the actual printer. The sheets are generated and checked here; the measurement needs paper, and is next weekend's.

**Reproduce:** `python -B tools/layout/module_sweep.py`, then `grouplab sweep module targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json scans/phase1/module-sweep/layouts.json scans/phase1/module-sweep`. Everything is in `scans/phase1/module-sweep/`, with a README. Since the geometry change of `docs/NOTES-FROM-PLANNING.md` entry 13 the sweep's base is that frozen definition rather than the live `GL-CF25-LTR`, whose sighter row moved, and test 26f is an error, so a rerun reports the 26f findings below as errors and renders the sheets regardless; every committed definition and PDF is unchanged.

| Module | Marker / quiet zone / footprint (dmm) | Markers | Lattice matches `layout.py` | Identifier | Validator | Test 43 worst bull, 300 / 600 DPI (in) | PDF pages |
|---|---|---|---|---|---|---|---|
| 0.3 mm | 24 / 6 / 36 | 44 | yes | `GL-DFA3-H72S-8KKS-A00S` | clean | 0.00016 / 0.00003 | 1 |
| 0.4 mm | 32 / 8 / 48 | 44 | yes | `GL-78H9-DKWN-AHDV-4WAK` | clean | 0.00017 / 0.00008 | 1 |
| 0.5 mm | 40 / 10 / 60 | 34 | yes | `GL-YCSK-DZZ1-R0VJ-4T5Y` | 3 warnings, test 26f | 0.00026 / 0.00012 | 1 |
| 0.6 mm | 48 / 12 / 72 | 34 | yes | `GL-683J-3ZR8-60D5-0FGG` | 3 warnings, test 26f | 0.00012 / 0.00004 | 1 |
| 0.8 mm | 64 / 16 / 96 | 34 | yes | `GL-SEBE-5F06-GVTF-3CZK` | 3 warnings, test 26f | 0.00007 / 0.00015 | 1 |

**For the paper protocol: five Letter pages**, one per sheet, printed at 100 percent on the Phase 0 printer and paper. No renderer change and no schema change was needed.

**The marker sizes are 24, 32, 40, 48 and 64 dmm, not the 30, 40, 50, 60 and 80 the brief and notes entry 12 give.** `tag36h11` prints as 8 modules across, a 6 by 6 data field inside a one-module black border, and TARGET-SCHEMA.md section 3.7 defines `markerSize` as the edge of that square, excluding the quiet zone. The renderer draws a module as `markerSize / 8` (`Tag36h11.Modules`), and FIDUCIAL-DECISION.md's own printed size is a 0.5 mm module in a 4.0 mm marker. "Ten modules across" counts the white ring, which the schema calls quiet zone. At 30, 40, 50, 60 and 80 dmm the modules would be 0.375, 0.5, 0.625, 0.75 and 1.0 mm, not the five measurement 2 names, and the renderer refuses 30 and 50 outright, because a 3.75 or 6.25 dmm module is not a whole half-dmm. The measurement's module sizes decide the sheets, and any protocol text quoting marker sizes should quote these.

**The 0.5 mm sheet is the Phase 0 sheet.** Its identifier is `GL-YCSK-DZZ1-R0VJ-4T5Y`, the built-in `GL-CF25-LTR` as printed for Phase 0, because an identifier hashes the geometry and not the name. So the sweep carries its own control, and the Phase 0 scans of sheets 1 to 3 are a second 0.5 mm point from an earlier print session.

**The marker count is not constant across the sweep, and the measurement has to allow for it.** The derivation merges lattice rows closer than the footprint plus 20 dmm, and drops a position whose footprint does not clear the bulls, codes and page edge, so both steps depend on the marker size: 44 markers at 0.3 and 0.4 mm, 34 at 0.5 mm and above. The two small-module sheets keep all 34 positions of the others and add ten: six in a row at y = 2325 dmm, which a footprint of 60 dmm or more merges into the row above, and four below the sighters at y = 2705 dmm. That second row is why those two sheets pass test 26f and the other three do not. Measurement 1 showed the worst bull still improving above 16 markers, so a raw comparison would credit the small modules with their extra markers. When the scans exist, register every sheet from the 34 lattice positions all five share as well as from all its markers; that needs no change to the sheets.

**The validator says nothing about the 0.3 mm module.** TARGET-SCHEMA.md section 7 says the format enforces the fiducial decision's module floor "through `markerSize` validation against `family`". No such rule exists in the validator or in section 10's conformance tests; the only limit is the JSON schema's `minimum` of 20 dmm. It is reported here rather than added, as the brief asks. The three test 26f warnings on the 0.5, 0.6 and 0.8 mm sheets are the sighter geometry as printed, the finding of `docs/PHASE0-RESULTS.md` section 4.4.

**Synthetic rendering is not the limit at any module.** Conformance test 43 passes on all five sheets at 300 and 600 DPI, worst bull 0.00026 in, with the shipped detector. The shipped canonical cell of at least 8 pixels reads a 3.5 px module at 300 DPI. Whatever the printed sweep shows is the printer's dot gain, which is what the measurement is for.

---

## M1. The developable surface fit

**Reproduce:** `grouplab surface synthetic` and `grouplab surface rendered` for the synthetic truth, then `grouplab surface frames --joint` for the real frames as M1.5 to M1.9 fitted them; since M1.11, `grouplab surface frames` fits one frame at a time. Raw rows: `scans/phase1/measurements/surface-synthetic.json` and `surface-rendered.json`, and `scans/phase0/measurements/surface.json`.

### M1.1 The model

A generalised cylinder, brief section 3.1, in `src/GroupLab.Core/Registration/`.

- **The sheet.** Rulings at an angle in page coordinates. The cross-section is integrated from a tangent angle that is a cubic in arc length across the rulings, so the map from page to sheet is an isometry by construction, and a flat sheet is all zeros. Distance along a straight page line is preserved to 0.0001 dmm on a strongly bent test sheet (`DevelopableSurfaceTests`).
- **The camera.** Rotation, translation, a focal length, and the Phase 0 lens, `u' = u (1 + k1 r^2 + k2 r^4)`. The focal length starts from the brief's EXIF estimate, the 35 mm equivalent over 36 mm times the long side, and is refined. Frames the EXIF says share a lens, focal length and f-number, are fitted jointly with one focal length and one lens. A scan, which has no focal length, is fitted orthographically.
- **The mapping.** `SurfaceMapping` implements the same page mapping the Phase 0 pipeline uses, so detection and the edge-fit bull locator run through it unchanged, and `grouplab measure --model surface` uses it.
- **The fit.** Levenberg-Marquardt on image-space corner residuals, from the Phase 0 lens fit's plane, at six starting ruling angles, because the ruling angle is undefined until the sheet bends. Every corner is then reclassified, at 12.7 dmm and then at the Phase 0 inlier distance of 2.54 dmm, with a refit after each.
- **Selection.** The bend is kept only when an F test says the corners support it; the next section says why.

### M1.2 Synthetic truth, and where it breaks

**Truth:** a 3000 by 4000 frame, focal length 2600 px against the EXIF start of 2556, k1 -0.05 and k2 +0.065 as the flat frames fitted, 260 mm from the page centre, so about a pixel per dmm as `main_flat1` is, tilted 8 degrees. **Corner noise** is 0.52 px per axis: `main_flat1`'s 0.00270 in RMS over 136 corners, at its scale. Ten seeds per cell, plus a noise-free trial on the bend axes, which recovers every bull to under 0.00001 in at every bend. The whole-sheet lens model is the Phase 0 photograph registration on the same corners.

| Axis, away from a 0.25 in bow | Passes the gate, median and 90th percentile, up to | First failure | Whole-sheet lens model |
|---|---|---|---|
| Bow, rulings vertical | **2.00 in**, worst bull 0.00218 / 0.00234 in | not reached | 0.01377 in at a 0.05 in bow, 0.448 in at 2.00 |
| Curl, forward at the free bottom edge | **1.50 in**, 0.00127 / 0.00195 | 2.00 in: 90th percentile 0.00605 | 0.01129 at 0.05 in |
| Twist the model cannot represent | **0.10 in**, 0.00179 / 0.00265 | 0.25 in: 90th percentile 0.00607; 0.50 in fails outright, 0.01365 median | 0.072 to 0.108 |
| Marker count | **the 23 `main_flat3` decoded**, 0.00227 / 0.00356 | the first 23 in raster order: 90th percentile 0.00692; the first 16 or fewer fail | 0.100 at the 23 of `main_flat3` |
| Keystone | **0.70**, the harshest tried, 0.00133 / 0.00159 | not reached | 0.040 to 0.056 |
| Starting focal length | **0.55 to 1.80 times truth**, refined to within 1.4 percent | not reached | |

**The model survives any bend a mounted sheet plausibly makes, and a keystone harsher than any real frame.** What breaks it is a twist of a quarter inch or more, which no generalised cylinder can take, and losing markers from where the bend is. `main_flat3`'s own 23 markers are enough because they are spread across the sheet; the first 23 in raster order are not, because they leave the bottom of the sheet unconstrained. **These rows were labelled "at random" until M1.7**, which found the shuffle never changed the order: they were always the first markers in raster order, the same set on every seed. The conclusion that losing markers from one region is what hurts is unaffected, and M1.7 measures it on the marker sets the Phase 0 frames actually decoded. `docs/NOTES-FROM-PLANNING.md` entry 13 cited the mislabelled row as its fifth argument for option A; entry 16 section 4 reads the corrected finding as the same proposition, and option A stands.

**Rendered, through detection and the edge-fit locator** (`grouplab surface rendered`), after the two changes below:

| Case | Whole-sheet lens model: worst scoring / sighter (in) | Surface: corners kept, fitted deflection, worst scoring / sighter (in) |
|---|---|---|
| Flat | 0.00055 / 0.00050, pass | 136 of 136, 0.013 in, 0.00045 / 0.00210, pass |
| Bow 0.25 in | 0.03338 / 0.02981, fail | 136 of 136, 0.251 in, 0.00034 / 0.00052, pass |
| Bow 1.00 in | 0.08262 / 0.11029, fail | 136 of 136, 1.000 in, 0.00040 / 0.00016, pass |
| Bow 0.25 in at keystone 0.802 | 0.04420 / 0.04280, fail | 136 of 136, 0.251 in, 0.00034 / 0.00051, pass |
| Bow 0.25 in with a 0.10 in twist | 0.03596 / 0.04605, fail | 136 of 136, 0.348 in, 0.00095 / 0.00150, pass |

### M1.3 Two changes, both made on synthetic evidence before any real frame

1. **Every corner is reclassified, at 12.7 dmm before 2.54.** The first version reclassified only at 2.54 dmm, after fitting the corners the planar RANSAC had kept. On the rendered 1.00 in bow that RANSAC rejected the outer marker columns, the bend fitted to the rest missed them by 4 to 5 dmm, and the fit kept 96 of 136 corners, fitted 1.035 in, and failed at 0.00555 in. Reclassifying at the misread distance first gives the columns back: 136 of 136, 1.000 in, 0.00040 in.
2. **The bend is kept only when it earns its parameters.** Always fitting the bend doubled a flat sheet's worst bull under noise, 0.00217 in median and 0.00470 at the 90th percentile against the lens model's 0.00108, with the extrapolated sighters taking most of it, which brief section 3.4 forbids. `SurfaceSelection` compares the page-space residual of the surface fit with the planar model refitted to the same corners, by an F test at p = 0.001 on the surface's four extra parameters. On the sweep it chose the plane for 10 of 10 noisy flat sheets, 0.00108 / 0.00122 in, and the bend for every bent sheet at every level, from 0.05 in. The rendered table above is the surface fit alone, which is why its flat row still shows the sighter at 0.00210 in.

### M1.4 The first real-frame run crashed, and was not retried

`grouplab surface frames` was first run once in the foreground on 14 September 2026 per `docs/NOTES-FROM-PLANNING.md` entry 14, with the surface fit of `d78c17c` plus two changes that do not alter what is computed: a tabulated profile and cached rotation in `SurfaceMapping`, held to 1e-6 px of the direct geometry by `DevelopableSurfaceTests`, and the frames prepared and evaluated in parallel. **It crashed after 60 seconds and wrote no raw rows.** Per entry 14 it was not retried and the frame set was not reduced.

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at GroupLab.Core.Measurement.EdgeFitBullLocator.Crossing(Double[] v, Int32 sign, Double contrast) in BullLocators.cs:line 311
   at GroupLab.Core.Measurement.EdgeFitBullLocator.Locate(...) in BullLocators.cs:line 246
   at GroupLab.Core.Measurement.SheetMeasurer.LocateBulls(...) in SheetMeasurer.cs:line 556
   at GroupLab.Cli.Spike.SurfaceFrames.Evaluate(Prepared p, SurfaceFrameResult fit) in SurfaceFrames.cs:line 255
```

It threw on `20260913_130543`, `130550` and `130554`, sheet 1 lying loose on a table, fitted jointly with `ultrawide1-3` as the 2.20 mm f/2.2 lens. `Crossing` averages three samples at each end of a radial edge profile, and the surface mappings of those frames put fewer than three across an edge. Entry 15 answered it: those frames are `photographGate: none`, every gated frame had finished, and the order is a guard, a seeding fix, a re-run and this report. The eleven frames that had finished are reproduced to every printed digit by the run below, so their partial lines are not repeated.

### M1.5 The real-frame run

`grouplab surface frames`, run in the foreground on 14 September 2026 after the two changes of entry 15 section 3: **all 14 photographs and all 10 gated scans complete in 1.3 minutes**, raw rows in `scans/phase0/measurements/surface.json`. The ultrawide and table-frame rows in this section were measured before M1.9 grouped joint fits by pixel geometry; M1.9 gives theirs now, and the raw rows hold M1.9's.

**The two changes.**

1. **The edge-fit locator has no input that throws.** A profile of fewer than 3 or more than 4096 samples, a mapping with no finite scale at the bull, or a centre fit that diverges now fails that bull with the reason in its `failure` field and the trace, and the sheet carries on. `EdgeFitGuardTests` breaks a real render's mapping eight ways and requires a named failure every time.
2. **One starting focal length per joint fit, never one per frame, with a warning naming each frame whose EXIF disagrees.** The seven 2.2 mm frames carry two 35 mm equivalents, 13 mm on `ultrawide1-3` and 23 mm on the four table frames, so a median or a vote would decide by frame count. `SurfaceFit.SeedFocal` tries each distinct EXIF start on every frame fitted alone and keeps the one with the lower total residual.

**It was run twice.** The first run's warnings said the ultrawide frames "fit best" from 2556 px, which the costs do not support, so the text was made neutral and the command run again. The two sets of raw rows are identical apart from those three warnings.

**The seeding change did not matter, and neither the start nor the seeding caused the crash.** The two candidate starts cost 1.955E+4 and 1.957E+4 px^2, 0.1 percent apart. `ultrawide1-3` start from 2556 px in this run and started from 1444 px in the crashed one, and every figure they print is the same to five digits. The three table frames started from 2556 px both times and are degenerate both times.

Surface fit per photograph. Focal lengths in pixels: the EXIF starting estimate, the frame fitted alone, and shared across the frames of its lens. Inches.

| Gate | Lens | Photograph | Markers | EXIF focal | Focal alone / shared | Ruling angle (deg) | Deflection | Corners kept | Residual kept / all | F (critical) | Bend kept |
|---|---|---|---|---|---|---|---|---|---|---|---|
| mounted | 2.20 mm f/2.2 | `ultrawide1.jpg` | 34/34 | 1444, start 2556 | 1940 / 1659 | 127.2 | 0.387 | 78 of 136 | 0.00622 / 0.02565 | 65.6 (4.62) | yes |
| mounted | 2.20 mm f/2.2 | `ultrawide2.jpg` | 34/34 | 1444, start 2556 | 1629 / 1659 | 28.1 | 0.177 | 97 of 136 | 0.00494 / 0.03259 | 323.6 (4.62) | yes |
| mounted | 2.20 mm f/2.2 | `ultrawide3.jpg` | 32/34 | 1444, start 2556 | 1621 / 1659 | 25.2 | 0.239 | 72 of 128 | 0.00515 / 0.02922 | 142.5 (4.62) | yes |
| mounted | 6.25 mm f/1.7 | `main1.jpg` | 34/34 | 2556 | 3478 / 2725 | 25.7 | 0.387 | 120 of 136 | 0.00445 / 0.00766 | 297.0 (4.62) | yes |
| mounted | 6.25 mm f/1.7 | `main2.jpg` | 26/34 | 2556 | 2718 / 2725 | 19.6 | 0.324 | 66 of 104 | 0.00522 / 0.02269 | 286.6 (4.62) | yes |
| mounted | 6.25 mm f/1.7 | `main3.jpg` | 27/34 | 2556 | 2775 / 2725 | 28.8 | 0.422 | 98 of 108 | 0.00456 / 0.01039 | 509.0 (4.62) | yes |
| mounted | 7.00 mm f/2.4 | `telephoto2.jpg` | 33/34 | 7667 | 7924 / 7924 | 20.6 | 0.397 | 105 of 132 | 0.00439 / 0.01124 | 920.8 (4.62) | yes |
| flat | 6.25 mm f/1.7 | `main_flat1.jpg` | 34/34 | 2556 | 2971 / 2725 | 70.0 | 0.029 | 136 of 136 | 0.00296 / 0.00296 | -10.7 (4.62) | no |
| flat | 6.25 mm f/1.7 | `main_flat2.jpg` | 25/34 | 2556 | 2750 / 2725 | 111.3 | 0.143 | 100 of 100 | 0.00355 / 0.00355 | -9.9 (4.62) | no |
| flat | 6.25 mm f/1.7 | `main_flat3.jpg` | 23/34 | 2556 | 2667 / 2725 | 80.1 | 0.025 | 89 of 92 | 0.00432 / 0.00474 | -9.1 (4.62) | no |
| not gated | 2.20 mm f/2.2 | `20260913_130543.jpg` | 34/34 | 2556 | 2366 / 1659 | 166.7 | 0.728 | 8 of 136 | 0.00308 / 188.68876 | -0.5 (4.62) | no |
| not gated | 2.20 mm f/2.2 | `20260913_130550.jpg` | 34/34 | 2556 | 2353 / 1659 | 99.7 | 2.162 | 4 of 136 | 0.00152 / 153.55351 | NaN (4.62) | yes |
| not gated | 2.20 mm f/2.2 | `20260913_130554.jpg` | 34/34 | 2556 | 2771 / 1659 | 111.4 | 1.373 | 0 of 136 | NaN / 5570.64523 | NaN (4.62) | yes |
| not gated | 2.20 mm f/2.2 | `20260913_130559.jpg` | 34/34 | 2556 | 906 / 1659 | 109.6 | 0.026 | 119 of 136 | 0.00588 / 0.00762 | -47.4 (4.62) | no |

Worst scoring bull / worst sighter per approach, inches; then scoring bulls over the gate, and the gate verdict, in the order the columns give. Whole sheet is the Phase 0 registration; nearest 6 and 8 are the diagnostic of `docs/PHASE0-RESULTS.md` section 4.5; selected is the surface where the F test keeps the bend and the planar model otherwise.

| Gate | Photograph | Whole sheet | Nearest 6 | Nearest 8 | Surface | Selected | Scoring bulls over the gate: whole / near 6 / near 8 / surface / selected | Gate: whole / surface / selected |
|---|---|---|---|---|---|---|---|---|
| mounted | `ultrawide1.jpg` | 0.03301 / 0.06983 | 0.01925 / 0.05085 | 0.01926 / 0.05402 | 0.02913 / 0.05645 | 0.02913 / 0.05645 | 13 / 11 / 10 / 18 / 18 | fail / fail / fail |
| mounted | `ultrawide2.jpg` | 0.06983 / 0.08732 | 0.05151 / 0.08429 | 0.04359 / 0.08420 | 0.09183 / 0.01187 | 0.09183 / 0.01187 | 20 / 19 / 20 / 9 / 9 | fail / fail / fail |
| mounted | `ultrawide3.jpg` | 0.09123 / 0.07995 | 0.03460 / 0.06408 | 0.09560 / 0.07276 | 0.05180 / 0.06474 | 0.05180 / 0.06474 | 21 / 14 / 15 / 15 / 15 | fail / fail / fail |
| mounted | `main1.jpg` | 0.01532 / 0.04841 | 0.00820 / 0.04157 | 0.00927 / 0.03833 | 0.01177 / 0.00620 | 0.01177 / 0.00620 | 8 / 5 / 7 / 8 / 8 | fail / fail / fail |
| mounted | `main2.jpg` | 0.04350 / 0.06231 | 0.04285 / 0.06844 | 0.03987 / 0.06205 | 0.05971 / 0.01856 | 0.05971 / 0.01856 | 21 / 22 / 21 / 19 / 19 | fail / fail / fail |
| mounted | `main3.jpg` | 0.06540 / 0.11379 | 0.04371 / 0.03066 | 0.05337 / 0.08145 | 0.04522 / 0.00508 | 0.04522 / 0.00508 | 20 / 11 / 12 / 7 / 7 | fail / fail / fail |
| mounted | `telephoto2.jpg` | 0.04584 / 0.09626 | 0.03626 / 0.05101 | 0.03478 / 0.08633 | 0.02241 / 0.01371 | 0.02241 / 0.01371 | 21 / 17 / 18 / 8 / 8 | fail / fail / fail |
| flat | `main_flat1.jpg` | 0.00343 / 0.00661 | 0.00387 / 0.00582 | 0.00356 / 0.00587 | 0.00443 / 0.00654 | 0.00343 / 0.00661 | 0 / 0 / 0 / 0 / 0 | fail / fail / fail |
| flat | `main_flat2.jpg` | 0.00566 / 0.01016 | 0.00705 / 0.00730 | 0.00789 / 0.00672 | 0.01780 / 0.01134 | 0.00566 / 0.01016 | 2 / 4 / 2 / 5 / 2 | fail / fail / fail |
| flat | `main_flat3.jpg` | 0.01183 / 0.00496 | 0.01975 / 0.00464 | 0.01513 / 0.00598 | 0.02581 / 0.00648 | 0.01108 / 0.00450 | 6 / 6 / 5 / 9 / 4 | fail / fail / fail |
| not gated | `20260913_130543.jpg` | 0.00569 / 0.00383 | 0.00471 / 0.00394 | 0.00406 / 0.00495 | 0.08520 / 0.01510 | 0.08256 / 0.07316 | 2 / 0 / 0 / 14 / 24 | fail / fail / fail |
| not gated | `20260913_130550.jpg` | 0.00678 / 0.00876 | 0.00311 / 0.00699 | 0.00353 / 0.00785 | 0.03536 / 0.01932 | 0.03536 / 0.01932 | 1 / 0 / 0 / 8 / 8 | fail / fail / fail |
| not gated | `20260913_130554.jpg` | 0.00597 / 0.01083 | 0.00652 / 0.01315 | 0.00581 / 0.00907 | 0.02891 / 0.00742 | 0.02891 / 0.00742 | 3 / 1 / 1 / 13 / 13 | fail / fail / fail |
| not gated | `20260913_130559.jpg` | 0.00338 / 0.00922 | 0.00283 / 0.00817 | 0.00256 / 0.00809 | 0.01063 / 0.01341 | 0.00444 / 0.00947 | 0 / 0 / 0 / 13 / 0 | fail / fail / fail |

The paper gate on the ten gated scans, through the orthographic surface. Worst bull, inches.

| Scan | Markers | Homography | Surface | Deflection (in) | F (critical) | Bend kept | Selected | Paper gate: homography / surface / selected |
|---|---|---|---|---|---|---|---|---|
| `gl-cf25-ltr-1-600-dpi.png` | 34/34 | 0.00254 | 0.00394 | 0.064 | -10.6 (6.91) | no | 0.00251 | pass / pass / pass |
| `gl-cf25-ltr-2-600-dpi.png` | 34/34 | 0.00316 | 0.00372 | 0.173 | -14.0 (6.91) | no | 0.00316 | pass / pass / pass |
| `gl-cf25-ltr-3-600-dpi.png` | 34/34 | 0.00290 | 0.00406 | 0.015 | -11.9 (6.91) | no | 0.00290 | pass / pass / pass |
| `gl-cf25-ltr-96.2-600-dpi.png` | 34/34 | 0.00234 | 0.00329 | 0.059 | 1.1 (6.91) | no | 0.00260 | pass / pass / pass |
| `gl-cf25-ltr-d-blank-600-dpi.png` | 34/34 | 0.00227 | 0.00240 | 0.103 | -4.5 (6.91) | no | 0.00219 | pass / pass / pass |
| `gl-cf25-ltr-d-filled-600-dpi.png` | 34/34 | 0.00247 | 0.00290 | 0.221 | 11.2 (6.91) | yes | 0.00290 | pass / pass / pass |
| `gl-lr300-t-1-600-dpi.png` | 9/9 | 0.00254 | 0.01867 | 2.515 | 4.1 (6.91) | no | 0.00254 | pass / fail / pass |
| `gl-lr300-t-2-600-dpi.png` | 9/9 | 0.00267 | 0.02087 | 2.573 | 2.0 (6.91) | no | 0.00267 | pass / fail / pass |
| `gl-lr300-t-3-600-dpi.png` | 9/9 | 0.00325 | 0.01817 | 2.598 | 1.2 (6.91) | no | 0.00319 | pass / fail / pass |
| `gl-lr300-t-4-600-dpi.png` | 9/9 | 0.00317 | 0.03366 | 1.845 | 1.3 (6.91) | no | 0.00314 | pass / fail / pass |

#### The mounted gate fails on all seven frames, under every approach

**No mounted frame comes inside 0.005 in, whichever model registers it.** The surface's worst scoring bull is 0.01177 in at best (`main1`) and 0.09183 at worst (`ultrawide2`). The generalised cylinder does not make a sheet hanging from one pin measurable.

**It is better than the Phase 0 registration, but not enough.**

- **Worst scoring bull:** better on five frames and worse on two, `ultrawide2` (0.09183 against 0.06983 in) and `main2` (0.05971 against 0.04350).
- **Worst sighter:** better on all seven. On `main3` it goes from 0.11379 to 0.00508 in, and on `main1` from 0.04841 to 0.00620.
- **Scoring bulls over the gate:** fewer on five frames, the same on `main1`, and more on `ultrawide1`, 18 against 13.

**A plane through the six markers nearest each bull beats the cylinder on six of the seven frames.** Only `telephoto2` goes the other way, 0.03626 against 0.02241 in. A local model absorbing error that a whole-sheet cylinder leaves says the cylinder's form is what limits it, not the corners it was given.

**The corners say the same.**

- **Residual:** over the corners the surface keeps it is 0.00439 to 0.00622 in on the mounted frames, against 0.00296 to 0.00432 on the flat ones.
- **Corners rejected:** 9 to 47 percent on the mounted frames (the ultrawide frames keep 56, 57 and 71 percent), where every rendered bow in M1.2 kept 136 of 136.

Which misfit is responsible, the lens absorbing the bend or a twist the cylinder cannot represent, is entry 15 section 4's question, and M1.7 measures it.

#### The flat control held

The F test declined the bend on all three flat frames. The selected model returns the Phase 0 figures on `main_flat1` and `main_flat2`, 0.00343 and 0.00566 in, and 0.01108 on `main_flat3` against Phase 0's 0.01183, because there the plane is refitted to the 89 corners the surface kept rather than Phase 0's 91. The surface alone would have made all three worse, 0.00443, 0.01780 and 0.02581 in, which is what M1.3's selection exists to stop. The flat gate verdicts are Phase 0's, three fails, for the reasons `docs/PHASE0-RESULTS.md` section 8 gives.

#### The paper gate passes ten of ten through the selected model

Worst bull 0.00319 in, on tile 3, and every selected figure is within 0.00026 in of the homography's. The surface alone fails all four 300 yard tiles: 36 corners for ten parameters, fitted as 1.8 to 2.6 in of deflection on a flat scan and extrapolated to 0.018 to 0.034 in, and the F test declines it every time. **It also kept the bend once on a flat scan**, `gl-cf25-ltr-d-filled`, F 11.2 against 6.91, a false positive at a nominal p = 0.001. That costs nothing here, 0.00290 against the homography's 0.00247 in. It shows a scan's corner errors are not the independent Gaussian errors the test assumes, so its nominal level is optimistic.

#### The table frames, and what the sheet says about its EXIF

`20260913_130559` fits. The bend is declined and the selected model is within the gate on every scoring bull, failing on a sighter at 0.00947 in. The other three complete without an exception, but degenerate: the joint fit keeps 8, 4 and 0 of 136 corners and fits 0.73 to 2.16 in of deflection to a sheet lying on a table. Bulls fail with named reasons, such as "the mapping gives the 127 dmm edge a profile of 2 samples at 31.322 dmm per pixel", and the rest are located through a mapping that is wrong.

**The sheet does check its own EXIF, and on these frames it contradicts the reading that the 35 mm tag is the wrong one.**

- **Main and telephoto:** a lens whose tags agree fits within a few percent of its EXIF estimate. The main camera's shared focal length is 2725 px against 2556 (+6.6 percent), and the telephoto's 7924 against 7667 (+3.4 percent).
- **Ultrawide:** fitted alone, `ultrawide1-3` recover 1621 to 1940 px against their 13 mm tag's 1444.
- **Table frames:** fitted alone, three of the four recover 2353, 2366 and 2771 px, near their 23 mm tag's 2556 and far from the ultrawide's figures. The fourth, `130559`, recovers 906 px, a frame whose flat, nearly frontal sheet constrains its focal length least.

If the 23 mm tag were wrong and these were ultrawide pixels, they would fit near the ultrawide. Forcing the ultrawide's shared 1659 px onto them is what degenerates them.

**One explanation fits all three tags being true.** The phone took the close-up table frames on the ultrawide sensor and cropped them to the main camera's field of view. The physical focal length and f-number are then the ultrawide's, the 35 mm equivalent describes the pixels, and a joint fit keyed on focal length and f-number mixes two pixel geometries. Their distortion differs too, since a crop rescales the normalised radius. That is not established from these frames. What is established is that the lens key entry 6 chose puts frames with different pixel focal lengths into one fit. The key is a planning decision, so it is reported and not changed. The shared 2.2 mm lens, k1 -0.031 and k2 +0.022, therefore includes the table frames and is not the ultrawide's lens.

#### Two defects the run exposed, not fixed

1. **`SurfaceSelection` keeps the bend by default when it cannot fit a plane.** With fewer than eight corners kept it returns "prefer surface" with no F, which is why `130550` and `130554` print "bend kept" at 4 and 0 corners. A surface fit that keeps fewer corners than it has parameters should fail the frame with a reason.
2. **Nothing fails a frame whose surface fit rejects nearly every corner.** The three degenerate table frames produce bull figures instead of a frame failure.

Both touch only the ungated table frames in this run, and neither changes a gated figure.

### M1.6 What M1 means for the paper protocol

**Do not plan on the surface fit to pass photographs of a sheet hanging from one pin.** None of seven passes, the best is 2.4 times the gate, and a local plane beats the whole-sheet cylinder on six of them.

**What measures today is a sheet held flat.** Through the selected model a flat photograph reproduces Phase 0 and a scan passes ten of ten. Any photograph the protocol means to gate should have the sheet held flat, not hanging.

**If mounted frames are wanted for entry 15 section 4**, the numbers suggest four things for planning to weigh:

- one camera per set, at one zoom setting;
- shot from far enough that the phone keeps that camera, with the camera recorded;
- the sheet fixed at all four corners in at least some frames, so bend without twist is in the set;
- every marker in frame.

The mounted frames lost the most accuracy where they lost the most corners.

### M1.7 The lens is not what limits the mounted frames; the shape is

`docs/NOTES-FROM-PLANNING.md` entry 15 section 4 asked whether the lens and the bend compete for the same error, and whether a twist is what is left. Built on synthetic truth first, then run once on the real frames with the evaluation fixed before the sweep's numbers were read.

**Reproduce:** `grouplab surface lens-sweep` (raw rows `scans/phase1/measurements/surface-lens-synthetic.json`), then `grouplab surface lens` (`scans/phase0/measurements/surface-lens.json`).

**What was added.**

- **A held lens.** `SurfaceFit` can hold k1 and k2, the focal length, or the bend at their starting values (`SurfaceHold`). With the lens held, `SurfaceSelection` compares against a plane through the same lens. Holding nothing gives `surface frames` byte-identical raw rows.
- **A leftover-shape diagnostic.** `SurfaceTwist` regresses each corner's page residual on the two shapes a developable sheet cannot take in the fitted ruling coordinates. Those are curvature along the rulings and a twist of them. It reports their offset at the page corner and an F test against no leftover shape, critical 6.91 at p = 0.001.
- **Tests.** `SurfaceLensTests` holds a lens, fits a flat hold, finds a twist with the lens held, and carries entry 15 section 3 step 5's regression test: two frames of one lens with disagreeing focal tags, one start, one joint fit, the shared focal length within 0.2 percent of truth.

#### On synthetic truth

The main camera of M1.2 at a 0.40 in bow, the most the mounted frames fitted, ten seeds per cell. Lens free, held at truth, and held at the two extremes of the main camera's own Phase 0 flat-frame fits: `main_flat2`'s k1 -0.0436, k2 +0.0531 and `main_flat3`'s k1 -0.0721, k2 +0.1058.

Every marker, surface worst bull median / 90th percentile, inches, against corner noise per axis:

| Noise (px) | Corners kept, lens free | Lens free | Held at truth | Held at `main_flat2`'s fit | Held at `main_flat3`'s fit |
|---|---|---|---|---|---|
| 0.5 | 136 of 136 | 0.00149 / 0.00212 | 0.00136 / 0.00195 | 0.00146 / 0.00253 | 0.00385 / 0.00433 |
| 1.0 | 131 of 136 | 0.00207 / 0.00347 | 0.00224 / 0.00268 | 0.00240 / 0.00304 | 0.00420 / 0.00625 |
| 2.0 | 81 of 136 | 0.00679 / 0.00962 | 0.00607 / 0.00767 | 0.00653 / 0.00783 | 0.00635 / 0.00940 |
| 3.0 | 42 of 136 | 0.01062 / 0.01135 | 0.00847 / 0.01284 | 0.00803 / 0.01310 | 0.00997 / 0.01254 |
| 5.0 | 23 of 136 | 0.02229 / 0.06453 | 0.01622 / 0.02233 | 0.01560 / 0.03076 | 0.01654 / 0.01847 |

The marker sets the Phase 0 frames decoded, and 25 at random, at 0.5 and 1.0 px:

| Coverage | 0.5 px, lens free | 0.5 px, held at truth | 0.5 px, held at `main_flat3`'s fit | 1.0 px, lens free | 1.0 px, held at truth |
|---|---|---|---|---|---|
| all 34 | 0.00149 / 0.00212 | 0.00136 / 0.00195 | 0.00385 / 0.00433 | 0.00207 / 0.00347 | 0.00224 / 0.00268 |
| `main_flat2`'s 25 | 0.00342 / 0.00488 | 0.00322 / 0.00509 | 0.00607 / 0.00709 | 0.00593 / 0.01105 | 0.00585 / 0.01147 |
| `main2`'s 26 | 0.00190 / 0.00261 | 0.00190 / 0.00317 | 0.00666 / 0.00857 | 0.00466 / 0.01081 | 0.00419 / 0.00877 |
| `main3`'s 27 | 0.00510 / 0.00662 | 0.00516 / 0.00680 | 0.00611 / 0.00903 | 0.00721 / 0.01293 | 0.00811 / 0.01236 |
| 25 at random | 0.00177 / 0.00228 | 0.00152 / 0.00216 | 0.00388 / 0.00580 | 0.00442 / 0.00582 | 0.00301 / 0.00411 |

The leftover-shape diagnostic against a twist put in, every marker, 0.52 px:

| Twist (in) | Lens | Surface worst bull, median / 90th pct | Leftover at the page corner, median (in) | F, median / 10th pct |
|---|---|---|---|---|
| 0.00 | free | 0.00155 / 0.00221 | +0.0001 | 0.1 / 0.0 |
| 0.00 | held at truth | 0.00142 / 0.00203 | -0.0003 | 0.0 / 0.0 |
| 0.10 | free | 0.00180 / 0.00243 | -0.0007 | 0.1 / 0.0 |
| 0.10 | held at truth | 0.00227 / 0.00249 | -0.0017 | 0.7 / 0.3 |
| 0.25 | free | 0.00505 / 0.00590 | -0.0025 | 0.9 / 0.6 |
| 0.25 | held at truth | 0.00881 / 0.01013 | -0.0093 | 9.6 / 7.1 |
| 0.50 | free | 0.01453 / 0.01509 | -0.0158 | 7.5 / 6.9 |
| 0.50 | held at truth | 0.03267 / 0.03505 | -0.0599 | 80.6 / 71.2 |

**What the synthetic run settles.**

1. **Holding the lens does not rescue anything that fails with it free.** Entry 15 asked whether a held lens holds where a free lens falls apart. It does not. With every marker the gate breaks between 1 and 2 px of corner noise per axis under every lens. Holding the true lens gains up to a quarter at 3 to 5 px, where everything has already failed.
2. **A held lens is only as good as the lens.** Held at the far end of the main camera's own Phase 0 range, `main_flat3`'s fit, the worst bull goes from 0.00149 to 0.00385 in at 0.5 px, and on `main2`'s markers from a pass to a fail.
3. **Where the markers are lost matters more than how many.** At 0.5 px the 27 `main3` decoded fail under every lens, 0.00510 in median, while 25 at random and `main2`'s 26 pass.
4. **A free lens hides a twist and a held lens exposes it.** With the lens free, a quarter inch of twist costs 0.00505 in and the diagnostic does not see it (F 0.9), because k1, the focal length, the pose and the ruling angle absorb most of it. Held at truth, the same twist costs 0.00881 in and is found, F 9.6 median. Without a twist, holding the lens changes nothing.

#### On the real frames

**The main camera's lens, fitted flat and shared on `main_flat1-3`:** focal length 2712 px, k1 -0.0480, k2 +0.0603. That is inside Phase 0's range for this camera (k1 -0.044 to -0.073, k2 +0.053 to +0.106), and within 0.0008 of the lens M1.5's free joint fit found, k1 -0.0474 and k2 +0.0611. The ultrawide and telephoto have no flat frame, so no lens could be fitted for them without a bend, and they are not held.

Worst scoring bull, surface / selected, inches, the main camera's six frames in M1.5's joint fit:

| Photograph | Lens free (M1.5) | k1 and k2 held | k1, k2 and focal length held |
|---|---|---|---|
| `main1` (mounted) | 0.01177 / 0.01177 | 0.01218 / 0.01218 | 0.01181 / 0.01181 |
| `main2` (mounted) | 0.05971 / 0.05971 | 0.06060 / 0.06060 | 0.06002 / 0.06002 |
| `main3` (mounted) | 0.04522 / 0.04522 | 0.04391 / 0.04391 | 0.04426 / 0.04426 |
| `main_flat1` | 0.00443 / 0.00343 | 0.00470 / 0.00338 | 0.00474 / 0.00338 |
| `main_flat2` | 0.01780 / 0.00566 | 0.01972 / 0.00536 | 0.01866 / 0.00536 |
| `main_flat3` | 0.02581 / 0.01108 | 0.02714 / 0.00887 | 0.02307 / 0.02307 |

The leftover-shape diagnostic on every mounted frame, and the flat controls:

| Photograph | Lens | Corners | Leftover at the page corner (in) | t along the rulings / saddle | F |
|---|---|---|---|---|---|
| `main1` | free | 136 | -0.0140 | -6.2 / +5.0 | 25.3 |
| `main1` | k1 and k2 held | 136 | -0.0126 | -5.8 / +4.8 | 22.6 |
| `main2` | free | 96 | -0.0158 | -2.0 / +2.6 | 4.0 |
| `main2` | k1 and k2 held | 96 | -0.0147 | -1.9 / +2.4 | 3.6 |
| `main3` | free | 108 | -0.0606 | -6.3 / -2.2 | 76.8 |
| `main3` | k1 and k2 held | 108 | -0.0575 | -6.2 / -2.0 | 72.1 |
| `telephoto2` | free | 131 | -0.0254 | -6.4 / +0.5 | 20.5 |
| `ultrawide1` | free | 131 | -0.0704 | -10.6 / -1.1 | 56.4 |
| `ultrawide2` | free | 128 | +0.0092 | -1.8 / +4.2 | 9.0 |
| `ultrawide3` | free | 116 | -0.0258 | -3.2 / -1.9 | 14.8 |
| `main_flat1`, `main_flat2`, `main_flat3` | free and held | | | | 0.8 to 2.9 |

**What the real frames say.**

1. **The lens is not what limits the main camera's mounted frames.**
   - Holding the flat frames' lens moves their worst scoring bull by 3 percent at most, up on `main1` and `main2` and down on `main3`, and no verdict changes.
   - It changes so little because M1.5's joint fit already shared one lens across all six frames, and the three flat frames pinned it.
   - The inflated lens entry 15 found on `main2`, k1 -0.1597, is its own per-frame Phase 0 planar fit. The surface fit never had that freedom, so the observation is right about Phase 0 and does not bear on M1.
   - The prediction made before the run, that a held lens would improve the bulls if the lens were absorbing the bend, is not met.
2. **The corners of six of the seven mounted frames carry a shape no generalised cylinder takes.**
   - F is 9.0 to 76.8 on the six, against 0.8 to 2.9 on the flat controls under every lens.
   - It is mostly curvature along the fitted rulings, not a saddle.
   - These are large effects, not marginal ones. With the lens free the diagnostic is weak: a synthetic 0.50 in twist leaves -0.016 in at F 7.5. `main3` and `ultrawide1` leave -0.061 and -0.070 in with the lens free, and `main3` leaves -0.058 in with it held, about what a 0.50 in twist leaves with the lens held.
3. **It does not say twist rather than a cone.** Curvature along the rulings is what a cylinder leaves on a twisted sheet once it has turned its rulings. It is also what a cylinder leaves on a cone, or on any developable sheet whose rulings are not parallel. Either way the generalised cylinder is the wrong family for a sheet hanging from one pin, which is the condition entry 15 set for the cone to come next.
4. **`main2` is the exception, and it is coverage.** It has no significant leftover shape (F 4.0) but the worst bull of the main camera, 0.060 in, on 26 markers with 38 of 104 corners rejected.
5. **Holding the focal length as well made one flat frame worse.** With k1, k2 and focal length held, `main_flat3` kept a bend its corners do not support, F 13.4 against 4.62, and its selected worst bull went from 0.00887 to 0.02307 in. Holding k1 and k2 alone did not.
6. **The mounted frames reject corners the way 1 to 2 px of noise does.** The main camera keeps 63 to 91 percent of its mounted corners and 97 to 100 percent of its flat ones. The sweep keeps 96 percent at 1 px and 60 percent at 2 px. With a leftover shape present, at least part of that rejection is misfit rather than detector noise, and the sweep says that much noise breaks the gate by itself.

**For the paper protocol, M1.6 stands, and one thing is added.** A lens can only be fitted without a bend on flat frames, so a set of frames from any camera the protocol means to use should include flat frames from that camera.

### M1.8 The mounted frames' corners, on the noise sweep

`docs/NOTES-FROM-PLANNING.md` entry 16 section 5 asked, before a general developable surface is built, whether the mounted frames' corners already put the gate out of reach. If they sat above 2 px of post-fit residual per axis, no surface model could pass them. If they sat near or below, the shape would be the limit.

**Reproduce:** `grouplab surface noise`, raw rows `scans/phase0/measurements/surface-noise.json`. It reads the M1.5 fit in `surface.json` as committed.

**Method.**

- **Pixels per axis.** Each corner's post-fit page residual is scaled to pixels by its own marker's size in the image, its mean edge in pixels over 40 dmm, so frames at different scales convert correctly.
- **The statistic.** Three are reported. The one that places a frame is a robust sigma: the median two-dimensional residual over the square root of 2 ln 2, the per-axis sigma of a two-dimensional Gaussian, over all corners.
- **Why not the kept corners.** Their RMS cannot place a frame. The fit keeps only corners within 2.54 dmm, so their RMS levels off near 1.1 px whatever the noise.
- **The calibration.** The same three statistics on synthetic trials of known noise: M1.7's truth camera at a 0.40 in bow, every marker, lens free, ten seeds per level.

The sweep:

| Corner noise put in (px per axis) | Corners kept | Post-fit RMS, kept corners (px per axis) | Post-fit RMS, all corners (px per axis) | Robust post-fit sigma (px per axis) | Surface worst bull, median / 90th pct | Gate |
|---|---|---|---|---|---|---|
| 0.5 | 136 of 136 | 0.49 | 0.49 | 0.49 | 0.00149 / 0.00212 | pass |
| 1.0 | 131 of 136 | 0.91 | 0.97 | 0.95 | 0.00207 / 0.00347 | pass |
| 1.5 | 108 of 136 | 1.08 | 1.44 | 1.34 | 0.00521 / 0.00640 | fail |
| 2.0 | 78 of 136 | 1.15 | 2.00 | 1.92 | 0.00621 / 0.00770 | fail |
| 3.0 | 44 of 136 | 1.18 | 3.10 | 3.08 | 0.00873 / 0.01067 | fail |
| 5.0 | 21 of 136 | 1.04 | 5.30 | 4.86 | 0.02472 / 0.04717 | fail |

The gated photographs:

| Gate | Photograph | Markers | Scale (px per dmm) | Corners kept | Post-fit RMS, kept (px per axis) | Post-fit RMS, all (px per axis) | Robust sigma (px per axis) | Robust sigma (dmm per axis) | Equivalent sweep noise, by px / by dmm | Worst scoring bull |
|---|---|---|---|---|---|---|---|---|---|---|
| mounted | `ultrawide1.jpg` | 34 | 1.10 | 78 of 136 | 1.23 | 5.15 | 2.13 | 1.94 | 2.18 / 1.98 | 0.02913 |
| mounted | `ultrawide2.jpg` | 34 | 1.00 | 97 of 136 | 0.88 | 5.03 | 1.16 | 1.17 | 1.27 / 1.25 | 0.09183 |
| mounted | `ultrawide3.jpg` | 32 | 0.90 | 72 of 128 | 0.94 | 3.77 | 1.71 | 1.91 | 1.82 / 1.96 | 0.05180 |
| mounted | `main1.jpg` | 34 | 1.03 | 120 of 136 | 0.83 | 1.47 | 0.91 | 0.88 | 0.96 / 0.91 | 0.01177 |
| mounted | `main2.jpg` | 26 | 1.04 | 66 of 104 | 1.00 | 3.65 | 1.53 | 1.47 | 1.67 / 1.59 | 0.05971 |
| mounted | `main3.jpg` | 27 | 1.08 | 98 of 108 | 0.92 | 1.86 | 0.93 | 0.87 | 0.98 / 0.89 | 0.04522 |
| mounted | `telephoto2.jpg` | 33 | 1.04 | 105 of 132 | 0.81 | 2.04 | 0.97 | 0.93 | 1.02 / 0.96 | 0.02241 |
| flat | `main_flat1.jpg` | 34 | 1.03 | 136 of 136 | 0.55 | 0.55 | 0.58 | 0.56 | 0.60 / 0.57 | 0.00443 |
| flat | `main_flat2.jpg` | 25 | 1.16 | 100 of 100 | 0.74 | 0.74 | 0.75 | 0.64 | 0.78 / 0.66 | 0.01780 |
| flat | `main_flat3.jpg` | 23 | 1.07 | 89 of 92 | 0.80 | 0.85 | 0.80 | 0.75 | 0.84 / 0.77 | 0.02581 |

**What it says.**

1. **The mounted frames are not above 2 px, so the shape is the limit and the general developable surface is next.**
   - `main1`, `main3` and `telephoto2` sit at 0.96 to 1.02 px, where the sweep with every marker passes, 0.00207 in median.
   - Their worst scoring bulls are 0.01177 to 0.04522 in, six to twenty times what that noise produces. `main3`'s 27 markers fail at 1 px on their own (M1.7, 0.00721 in), which still leaves a factor of six.
   - `ultrawide2`, `main2` and `ultrawide3` sit at 1.27 to 1.82 px, inside the band where the sweep breaks.
   - `ultrawide1` sits at 2.18 px, just above it.
2. **Every one of these figures is an upper bound on detector noise.**
   - A post-fit residual includes whatever the model could not fit, and six of the seven carry a shape the cylinder cannot take (M1.7).
   - The flat controls, where there is no such shape, sit at 0.60 to 0.84 px.
3. **Pixels and page dmm place the frames alike.** The frames' scales are 0.90 to 1.10 px per dmm against the sweep's 0.98, and the two placements agree within 0.15 px.
4. **A noise level that passes is necessary, not sufficient.** `main_flat2` and `main_flat3` sit at 0.78 and 0.84 px, but the surface alone fails them on coverage, 0.01780 and 0.02581 in, which is why their selected model is the plane.
5. **The ultrawide rows come from the joint fit entry 16 section 2 regroups**, and M1.9 measures them again: every mounted frame then sits at or below 1.88 px.

### M1.9 Joint fits grouped by pixel geometry

`docs/NOTES-FROM-PLANNING.md` entry 16 section 2 accepted question 8 and corrected entry 15 section 1: the table photographs are a cropped or digitally zoomed ultrawide, and both of their focal length tags are true. Their Phase 0 distortion shows it. `ultrawide1-3` fit k1 of -0.0296, -0.0765 and -0.0679, and the four table frames +0.0097, -0.0020, +0.0049 and +0.0034: the centre of an ultrawide's image circle is its low-distortion part.

**The change.**

- **The key.** `grouplab surface frames`, `surface lens` and `surface noise` group a joint fit by physical focal length, f-number, 35 mm equivalent and image size together.
- **No warning.** The EXIF disagreement warning of M1.5 is gone.
- **Why not the equivalent and image size alone.** The table frames share the main camera's 23 mm equivalent and 4000 by 3000 pixels, but not its distortion: k1 about 0, against the main camera's -0.047.

**A product finding.**

- **A cropping phone rewrites one tag.** It keeps the physical focal length and changes the 35 mm equivalent. Anything in this pipeline that reasons about a camera has to reason about the equivalent and the image size together, never about the physical focal length alone.
- **Neither tag gives the distortion.** A cropped frame has the distortion of the part of the image circle it kept, so the physical lens does not identify it either.

**Re-run.** All three commands were re-run, and their raw rows now hold these figures. The earlier rows are at `fa36186`.

- **Unchanged to the digit:** every figure for the main camera, the telephoto and the scans.
- **What moves:**

| Photograph | Shared focal (px) | Deflection (in) | Corners kept | Worst scoring / sighter, surface (in) | Robust corner sigma (px per axis) | Leftover-shape F | Before: worst scoring bull, sigma, F |
|---|---|---|---|---|---|---|---|
| `ultrawide1` | 1636 | 0.073 | 85 of 136 | 0.01435 / 0.04783 | 1.79 | 44.6 | 0.02913, 2.13, 56.4 |
| `ultrawide2` | 1636 | 0.516 | 96 of 136 | 0.03409 / 0.01686 | 1.07 | 21.4 | 0.09183, 1.16, 9.0 |
| `ultrawide3` | 1636 | 0.537 | 85 of 128 | 0.04056 / 0.05194 | 1.13 | 4.0 | 0.05180, 1.71, 14.8 |
| `20260913_130543` | 2401 | 0.282 | 62 of 136 | 0.02679 / 0.02735; plane selected, 0.00522 / 0.00657 | | | 8 of 136 corners kept |
| `20260913_130550` | 2401 | 2.418 | 38 of 136 | 0.05053 / 0.03987; plane selected, 0.01877 / 0.01300 | | | 4 of 136 |
| `20260913_130554` | 2401 | 0.915 | 16 of 136 | 0.09117 / 0.09227; plane selected, 0.02034 / 0.03917 | | | 0 of 136 |
| `20260913_130559` | 2401 | 0.038 | 101 of 136 | 0.01701 / 0.02339; plane selected, 0.00344 / 0.01047 | | | 119 of 136 |

**What it changes in M1.5, M1.7 and M1.8.**

1. **The mounted gate is still 0 of 7, and the surface is now better than Phase 0 on six of them.**
   - All three ultrawide frames improve. They are now better than the Phase 0 registration's 0.03301, 0.06983 and 0.09123 in; only `main2` is worse.
   - A plane through the six nearest markers beats the cylinder on four of seven now (`ultrawide3`, `main1`, `main2`, `main3`), not six.
2. **The table frames are no longer degenerate.** They keep 62, 38, 16 and 101 of 136 corners, and the plane is selected on all four.
   - They share 2401 px against their tag's 2556: the 23 mm equivalent describes their pixels, as entry 16 says.
   - M1.5's selection defect, below eight corners, no longer triggers here, but it is still in the code.
3. **Every mounted frame now sits at or below 1.88 px** by pixels, and 1.72 by page dmm. `ultrawide1` moves from 2.18. Section 5's answer is unchanged and clearer: the shape is the limit.
4. **The leftover shape stays significant on five of seven mounted frames.** `ultrawide3` drops to F 4.0 and `ultrawide2` rises to 21.4, against the flat controls' 0.8 to 2.9.

### M1.10 The general developable surface: it recovers a cone, and it does not pass the mounted frames

`docs/NOTES-FROM-PLANNING.md` entry 16 section 5: M1.8 put the shape, not the corners, as the limit, so a general developable surface comes next, on the cylinder's discipline, and nothing after it.

**Reproduce:** `grouplab surface general-sweep` (raw rows `scans/phase1/measurements/surface-general-synthetic.json`), then `grouplab surface general` (`scans/phase0/measurements/surface-general.json`).

**The model.** `FoldedSheet`, in `src/GroupLab.Core/Registration/`.

- **Rulings.** They cross the flat page in straight lines whose direction turns with arc length along a spine through the page centre: a linear turn makes a cone, a quadratic one a fan that returns.
- **Folds.** The sheet is folded along rulings 10 dmm apart, each by the change in the bend's tangent angle across it. Every strip is a rigid piece of the page, so the map from page to sheet is an isometry by construction, as the cylinder's is.
- **Two parameters.** It adds a linear and a quadratic turn to the cylinder. Selection counts them, six extra parameters against the plane, critical F 3.74.
- **Validity.** Rulings that cross inside the page would fold the sheet through itself, so such a model maps nothing.
- **Tests.** `GeneralDevelopableTests` checks five things:
  - with no turn the folded sheet is the cylinder, within 0.018 dmm on a strongly bent sheet;
  - a straight page line keeps its length on a turned sheet;
  - its mapping inverts to 1e-6 dmm;
  - rulings that cross are refused;
  - a cone's bulls are recovered, to 0.00193 in where the cylinder gives 0.01000.

#### On synthetic truth

Both surfaces on the same corners, M1.7's truth camera, ten seeds per cell.

| Axis | Level | Noise (px) | Markers | Truth deflection (in) | Cylinder: worst bull median / 90th pct | Cylinder gate | General: worst bull median / 90th pct | General gate | General: corners kept, robust sigma (px) | General: fitted turn across the page (deg), median | General bend kept | Selected: worst bull median / 90th pct | Selected gate |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cone, linear turn | 0.0: rulings turn 0.0 degrees across the page | 0.52 | 34 | 0.400 | 0.00155 / 0.00221 | pass | 0.00183 / 0.00227 | pass | 136 of 136, 0.50 | 0.6 of 0.0 | 10 of 10 | 0.00183 / 0.00227 | pass |
| cone, linear turn | 0.1: rulings turn 12.4 degrees across the page | 0.52 | 34 | 0.400 | 0.01010 / 0.01088 | fail | 0.00151 / 0.00181 | pass | 136 of 136, 0.51 | 12.5 of 12.4 | 10 of 10 | 0.00151 / 0.00181 | pass |
| cone, linear turn | 0.2: rulings turn 24.7 degrees across the page | 0.52 | 34 | 0.398 | 0.01761 / 0.01949 | fail | 0.00152 / 0.00254 | pass | 136 of 136, 0.50 | 24.6 of 24.7 | 10 of 10 | 0.00152 / 0.00254 | pass |
| cone, linear turn | 0.3: rulings turn 37.1 degrees across the page | 0.52 | 34 | 0.397 | 0.02919 / 0.03220 | fail | 0.00135 / 0.00187 | pass | 136 of 136, 0.52 | 37.3 of 37.1 | 10 of 10 | 0.00135 / 0.00187 | pass |
| quadratic turn | 0.05: rulings turn 3.3 degrees across the page | 0.52 | 34 | 0.400 | 0.00243 / 0.00260 | pass | 0.00155 / 0.00196 | pass | 136 of 136, 0.51 | 3.8 of 3.3 | 10 of 10 | 0.00155 / 0.00196 | pass |
| quadratic turn | 0.10: rulings turn 6.7 degrees across the page | 0.52 | 34 | 0.400 | 0.00445 / 0.00497 | pass | 0.00150 / 0.00211 | pass | 136 of 136, 0.51 | 6.8 of 6.7 | 10 of 10 | 0.00150 / 0.00211 | pass |
| twist, not developable | 0.10 in over the bow | 0.52 | 34 | 0.400 | 0.00220 / 0.00240 | pass | 0.00242 / 0.00259 | pass | 136 of 136, 0.54 | 4.2 of 0.0 | 10 of 10 | 0.00242 / 0.00259 | pass |
| twist, not developable | 0.25 in over the bow | 0.52 | 34 | 0.400 | 0.00536 / 0.00601 | fail | 0.00424 / 0.00476 | pass | 136 of 136, 0.66 | 10.2 of 0.0 | 10 of 10 | 0.00424 / 0.00476 | pass |
| twist, not developable | 0.50 in over the bow | 0.52 | 34 | 0.400 | 0.01405 / 0.01462 | fail | 0.00927 / 0.01116 | fail | 120 of 136, 0.89 | 22.1 of 0.0 | 10 of 10 | 0.00927 / 0.01116 | fail |
| noise, cone 0.2 | 1.0 px | 1.00 | 34 | 0.398 | 0.01916 / 0.02059 | fail | 0.00293 / 0.00433 | pass | 131 of 136, 1.01 | 25.2 of 24.7 | 10 of 10 | 0.00293 / 0.00433 | pass |
| noise, cone 0.2 | 1.5 px | 1.50 | 34 | 0.398 | 0.02256 / 0.02604 | fail | 0.00502 / 0.00621 | fail | 104 of 136, 1.38 | 24.5 of 24.7 | 10 of 10 | 0.00502 / 0.00621 | fail |
| noise, cone 0.2 | 2.0 px | 2.00 | 34 | 0.398 | 0.01996 / 0.02550 | fail | 0.00725 / 0.00917 | fail | 80 of 136, 1.90 | 24.6 of 24.7 | 10 of 10 | 0.00725 / 0.00917 | fail |
| markers, cone 0.2 | main2's 26 | 0.52 | 26 | 0.398 | 0.04385 / 0.04778 | fail | 0.00561 / 0.00687 | fail | 104 of 104, 0.52 | 26.2 of 24.7 | 10 of 10 | 0.00561 / 0.00687 | fail |
| markers, cone 0.2 | main3's 27 | 0.52 | 27 | 0.398 | 0.02604 / 0.02752 | fail | 0.00365 / 0.00459 | pass | 108 of 108, 0.48 | 25.3 of 24.7 | 10 of 10 | 0.00365 / 0.00459 | pass |
| markers, cone 0.2 | 25 at random | 0.52 | 25 | 0.398 | 0.01876 / 0.02992 | fail | 0.00197 / 0.00351 | pass | 100 of 100, 0.49 | 25.5 of 24.7 | 10 of 10 | 0.00197 / 0.00351 | pass |
| bow, cone 0.2 | 1.00 in, rulings turn 24.7 degrees across the page | 0.52 | 34 | 0.996 | 0.04041 / 0.04140 | fail | 0.00127 / 0.00218 | pass | 136 of 136, 0.50 | 24.7 of 24.7 | 10 of 10 | 0.00127 / 0.00218 | pass |

**What the sweep settles.**

1. **The general surface takes a cone the cylinder cannot.** A linear turn of 12 degrees across the page fails the cylinder, 0.01010 in, and a turn of 37 degrees passes the general surface at 0.00135 in, the fitted turn within 0.2 degrees of truth. With a 1.00 in bow on top it still passes, 0.00127 in.
2. **It breaks in four places.**
   - **The family's own limit.** A linear turn of 0.4 (49.5 degrees) or a quadratic turn of 0.2 crosses rulings inside the page, which no sheet of paper does. Those cells were not run.
   - **Corner noise.** Its break sits between 1.0 and 1.5 px.
   - **Coverage.** `main2`'s 26 markers fail it, 0.00561 in.
   - **Twist.** A non-developable twist of 0.50 in over the bow fails it, 0.00927 in. It passes 0.25 in, which the cylinder does not.
3. **With no turn to find it costs a little.** It gives 0.00183 in on a plain bow, against the cylinder's 0.00155.

#### On the real frames

**The first run's joint fit stalled.** `grouplab surface general`, run with the code before the minimiser changes below, kept 0 corners on `main_flat1`, `main_flat3`, `main1` and `ultrawide1`, with worst bulls up to 3.0 in. Every frame's turn and ruling angle came out exactly as fitted alone, and only the shared focal length and lens had moved, to the medians of the frames' own fits. Those rows are not kept, because the committed code does not reproduce them.

**The cause, found on the frames' corners with nothing adjusted to their bulls.**

1. **The fit ends on the boundary.** On a flat or barely bent frame the ruling turn is unconstrained, so a frame's own fit runs to where its rulings begin to cross. `main_flat1` fitted a turn of 52 degrees on an unbent sheet.
2. **One step leaves the valid region.** A ruling-angle step of 1e-6 there makes all 136 corners undefined.
3. **The penalty floods the solve.** The minimiser's forward-difference Jacobian took that step. The penalty's column, of order 1e12, set the damping floor of every other parameter in the joint solve, and no step was taken.

**Two changes to the minimiser, general and tested.** `LevenbergMarquardt` now does two things at an undefined boundary:

- **A backward difference.** A difference step whose cost is not finite, or jumps by 1e9 plus a thousand times the cost, takes the backward difference, and a parameter undefined both ways is left out of the iteration.
- **A held parameter.** A parameter whose solved step would carry it across the boundary is held for that step.

What the changes did:

- **Tests.** `LevenbergMarquardtTests` has two cases.
- **The cylinder.** `surface frames` gives the same numbers as M1.9.
- **The synthetic sweep.** 14 of its 160 general trials changed, all in the noisy and lost-marker cells, because those fits had met the boundary too. The 1.5 px row went from "median passes, 90th fails" to a fail, and the table above is after the change.

**The joint fit still does not converge.** The flat frames press against the boundary along a combination of parameters, not one. Fitting the turn only where the bend is supported would be a further modelling decision, and entry 16 section 5 says to stop here, so it was not made. The joint fit, as the final run gives it:

| Gate | Photograph | Whole sheet | Cylinder | General | Selected | Scoring bulls over the gate | Gate |
|---|---|---|---|---|---|---|---|
| flat | `main_flat1.jpg` | 0.00343 / 0.00661 | 0.00443 / 0.00654 | 0.09489 / 0.00507 | 0.09489 / 0.00507 | 0 / 0 / 16 / 16 | fail / fail / fail / fail |
| flat | `main_flat2.jpg` | 0.00566 / 0.01016 | 0.01780 / 0.01134 | 0.04167 / 0.02254 | 0.07760 / 0.05044 | 2 / 5 / 25 / 8 | fail / fail / fail / fail |
| flat | `main_flat3.jpg` | 0.01183 / 0.00496 | 0.02581 / 0.00648 | 0.06219 / 0.05635 | 0.06219 / 0.05635 | 6 / 9 / 21 / 21 | fail / fail / fail / fail |
| mounted | `main1.jpg` | 0.01532 / 0.04841 | 0.01177 / 0.00620 | 0.03131 / 0.04765 | 0.03131 / 0.04765 | 8 / 8 / 9 / 9 | fail / fail / fail / fail |
| mounted | `main2.jpg` | 0.04350 / 0.06231 | 0.05971 / 0.01856 | 0.04921 / 0.06129 | 0.04921 / 0.06129 | 21 / 19 / 24 / 24 | fail / fail / fail / fail |
| mounted | `main3.jpg` | 0.06540 / 0.11379 | 0.04522 / 0.00508 | 0.05439 / 0.01669 | 0.07518 / NaN | 20 / 7 / 23 / 15 | fail / fail / fail / fail |
| mounted | `telephoto2.jpg` | 0.04584 / 0.09626 | 0.02241 / 0.01371 | 0.02330 / 0.01102 | 0.02330 / 0.01102 | 21 / 8 / 8 / 8 | fail / fail / fail / fail |
| mounted | `ultrawide1.jpg` | 0.03301 / 0.06983 | 0.01435 / 0.04783 | 0.03226 / 0.00461 | 0.03226 / 0.00461 | 13 / 18 / 11 / 11 | fail / fail / fail / fail |
| mounted | `ultrawide2.jpg` | 0.06983 / 0.08732 | 0.03409 / 0.01686 | 0.02549 / 0.02181 | 0.02549 / 0.02181 | 20 / 10 / 17 / 17 | fail / fail / fail / fail |
| mounted | `ultrawide3.jpg` | 0.09123 / 0.07995 | 0.04056 / 0.05194 | 0.03211 / 0.07210 | 0.03211 / 0.07210 | 21 / 14 / 14 / 14 | fail / fail / fail / fail |

**The frames fitted alone do not depend on the joint fit, and they are the measurement.** Each frame has its own focal length and lens, as cylinder and as general surface:

| Gate | Photograph | Forward residual median (px) | Corners kept | Robust sigma (px per axis) | Rulings turn (deg) | Cylinder alone | General alone | Selected | Scoring bulls over the gate | Gate |
|---|---|---|---|---|---|---|---|---|---|---|
| flat | `main_flat1.jpg` | 0.65 / 0.64 | 136 / 136 of 136 | 0.55 / 0.55 | 52.4 | 0.00305 / 0.00830 | 0.00292 / 0.00728 | 0.00343 / 0.00661 | 0 / 0 / 0 | fail / fail / fail |
| flat | `main_flat2.jpg` | 0.86 / 0.80 | 100 / 100 of 100 | 0.74 / 0.70 | 29.4 | 0.02314 / 0.01054 | 0.01804 / 0.01002 | 0.00566 / 0.01016 | 6 / 8 / 2 | fail / fail / fail |
| flat | `main_flat3.jpg` | 0.66 / 0.63 | 91 / 91 of 92 | 0.58 / 0.55 | 25.1 | 0.00814 / 0.00465 | 0.00777 / 0.00416 | 0.00777 / 0.00416 | 3 / 4 / 4 | fail / fail / fail |
| mounted | `main1.jpg` | 0.87 / 1.03 | 125 / 130 of 136 | 0.74 / 0.87 | 26.3 | 0.00604 / 0.01820 | 0.00764 / 0.02498 | 0.00764 / 0.02498 | 4 / 5 / 5 | fail / fail / fail |
| mounted | `main2.jpg` | 1.74 / 1.85 | 70 / 67 of 104 | 1.46 / 1.57 | 32.5 | 0.05149 / 0.01694 | 0.05060 / 0.01110 | 0.05060 / 0.01110 | 19 / 19 / 19 | fail / fail / fail |
| mounted | `main3.jpg` | 0.94 / 0.94 | 96 / 98 of 108 | 0.88 / 0.83 | 37.0 | 0.03353 / 0.00968 | 0.03440 / 0.02806 | 0.03440 / 0.02806 | 5 / 7 / 7 | fail / fail / fail |
| mounted | `telephoto2.jpg` | 1.12 / 1.09 | 105 / 104 of 132 | 0.97 / 0.96 | 27.3 | 0.02241 / 0.01371 | 0.02330 / 0.01102 | 0.02330 / 0.01102 | 8 / 8 / 8 | fail / fail / fail |
| mounted | `ultrawide1.jpg` | 1.25 / 1.25 | 113 / 113 of 136 | 1.15 / 1.18 | 12.8 | 0.01249 / 0.02129 | 0.01165 / 0.02064 | 0.01165 / 0.02064 | 9 / 11 / 11 | fail / fail / fail |
| mounted | `ultrawide2.jpg` | 1.08 / 1.08 | 110 / 109 of 136 | 0.99 / 0.99 | 33.8 | 0.05809 / 0.00758 | 0.05645 / 0.01645 | 0.05645 / 0.01645 | 9 / 9 / 9 | fail / fail / fail |
| mounted | `ultrawide3.jpg` | 1.36 / 1.33 | 87 / 95 of 128 | 1.23 / 1.26 | 46.7 | 0.06660 / 0.03095 | 0.05749 / 0.05103 | 0.05749 / 0.05103 | 12 / 15 / 15 | fail / fail / fail |

**What the real frames say.**

1. **No mounted frame comes inside 0.005 in, as either surface.**
   - General surface alone: worst scoring bull 0.00764 to 0.05749 in.
   - Cylinder alone: 0.00604 to 0.06660 in.
2. **The general surface changes almost nothing on these frames.**
   - **Worst scoring bull:** it moves by -14 to +26 percent, better on four mounted frames and worse on three.
   - **Corners:** the median forward residual moves by at most 0.16 px, and the corners kept by at most eight.
   - **Contrast with synthetic truth:** on a synthetic cone it removes the whole of the cylinder's error. The shape M1.7 found on these corners is not one this family takes up either.
3. **Its fitted turns carry no information.** They are 25 to 52 degrees on the three flat frames, where there is no bend for a turn to act on, and 13 to 47 degrees on the mounted ones.
4. **Fitted alone, `main1` is the best mounted frame, and its cylinder beats its joint fit.** Its cylinder alone gives 0.00604 in, against 0.01177 in its joint fit (M1.5), with 125 corners kept against 120. The shared camera costs `main1` a factor of two, and it is still outside the gate.

**So, per entry 16 section 5, this is where the surface models stop, and M2 is next.**

- **Not tried:** a third surface model, or constraining the general surface's turn.
- **The recorded fallback:** the piecewise registration of `docs/PHASE0-RESULTS.md` section 4.5.
- **The finding:** the mounted gate needs more than a developable fit of the whole sheet, which is a finding about the product.
- **The protocol:** M1.6 stands unchanged.

### M1.11 The gate stays at 0.005 in, the mounted requirement stays open, and what the fit leaves is mostly not structured

`docs/NOTES-FROM-PLANNING.md` entry 17 closes the surface models and records four things here.

#### Why the gate did not move

A separate, looser gate for mounted photographs was proposed, on the grounds that 0.005 in was inherited from the paper gate and never argued on its own. Alan asked the planning session to decide, and it worked the error budget and withdrew the proposal (entry 17 section 2).

- **The budget.** The finest quantity measured is a hole centre, with a noise floor of 0.008 in on real paper. A subsystem that contributes a third of the dominant term adds under five percent in quadrature: 0.0027 in, and half is 0.004 in. So the budget argues for 0.003 to 0.005 in, and the gate already sits at its loose end. The statistics would tolerate more, but the gate exists so that the instrument is not the limit, and that is the anchor.
- **The argument not made.** A worst bull of 28 is a bound, not a typical error, and could plausibly run to twice the typical figure. That argument was not made, because it would be made after seeing the results, and the best mounted frame came in at 0.00604 in. A gate that moves to within a thousandth of the number that makes one frame pass is not a gate.
- **So the gate is 0.005 in.** If it is to move, the argument is written down and the number fixed before more mounted frames are measured, and it is tested on frames not used to set it.

#### The mounted photograph gate is an open requirement

- **Scans:** the paper gate passes, ten of ten.
- **Flat photographs:** they nearly pass. The frame that decoded every marker is inside on every scoring bull, and the failures are named.
- **Mounted photographs:** they do not pass, by any developable surface, and the residual is not a bendable shape.

`DESIGN.md` section 21 is amended `[r6]` to say so. The recorded fallback is piecewise registration (`docs/PHASE0-RESULTS.md` section 4.5), and it is not attempted in Phase 1: it costs days budgeted for hole detection, it cannot help a bull outside the lattice, and the time to try it is when real shot targets exist.

#### Two changes (entry 17 section 4)

1. **One frame at a time by default.**
   - **The change.** `grouplab surface frames` fits each frame alone, and `grouplab surface frames --joint` keeps the joint fit for a set known to share a camera. A user photographs one target at a time, and sharing a camera cost `main1` a factor of two (M1.10).
   - **What still reproduces.** `surface lens` and `surface general` keep their joint fits, so M1.7 and M1.10 reproduce. `surface noise` and `surface correlation` read whatever `surface.json` holds, now the alone fit, so M1.8 and M1.9 reproduce after `surface frames --joint`.
2. **Selection defaults to the plane below eight corners.** Fewer corners is less evidence, so the default is the model with fewer parameters. The plane is fitted to the kept corners from four, with the lens from six, and below four no model is supported (`SurfaceSelectionTests`). No gated figure moves. In M1.10's raw rows, "bend kept" now reads no on the five joint general fits that kept no corners.

Every gated photograph fitted alone (`grouplab surface frames`, then `surface noise` for the last two columns). Worst scoring bull / worst sighter, inches:

| Gate | Photograph | Corners kept | Deflection (in) | F (critical) | Bend kept | Surface | Selected | Robust corner sigma (px per axis) | Equivalent sweep noise (px) |
|---|---|---|---|---|---|---|---|---|---|
| mounted | `ultrawide1` | 113 of 136 | 0.219 | 420.6 (4.62) | yes | 0.01249 / 0.02129 | 0.01249 / 0.02129 | 1.15 | 1.26 |
| mounted | `ultrawide2` | 110 of 136 | 0.302 | 502.0 (4.62) | yes | 0.05809 / 0.00758 | 0.05809 / 0.00758 | 0.99 | 1.05 |
| mounted | `ultrawide3` | 87 of 128 | 0.574 | 620.1 (4.62) | yes | 0.06660 / 0.03095 | 0.06660 / 0.03095 | 1.23 | 1.36 |
| mounted | `main1` | 125 of 136 | 0.417 | 267.5 (4.62) | yes | 0.00604 / 0.01820 | 0.00604 / 0.01820 | 0.74 | 0.77 |
| mounted | `main2` | 70 of 104 | 0.417 | 210.1 (4.62) | yes | 0.05149 / 0.01694 | 0.05149 / 0.01694 | 1.46 | 1.60 |
| mounted | `main3` | 96 of 108 | 0.440 | 587.9 (4.62) | yes | 0.03353 / 0.00968 | 0.03353 / 0.00968 | 0.88 | 0.93 |
| mounted | `telephoto2` | 105 of 132 | 0.397 | 920.8 (4.62) | yes | 0.02241 / 0.01371 | 0.02241 / 0.01371 | 0.97 | 1.02 |
| flat | `main_flat1` | 136 of 136 | 0.041 | -5.1 (4.62) | no | 0.00305 / 0.00830 | 0.00343 / 0.00661 | 0.55 | 0.57 |
| flat | `main_flat2` | 100 of 100 | 0.172 | -4.9 (4.62) | no | 0.02314 / 0.01054 | 0.00566 / 0.01016 | 0.74 | 0.78 |
| flat | `main_flat3` | 91 of 92 | 0.020 | 22.7 (4.62) | yes | 0.00814 / 0.00465 | 0.00814 / 0.00465 | 0.58 | 0.60 |

- **Mounted: still 0 of 7.** Worst scoring bull 0.00604 to 0.06660 in, corner sigma 0.74 to 1.46 px.
- **Better alone on six of seven.** Against M1.9's joint fits, the worst scoring bull is better on `ultrawide1`, `main1`, `main2`, `main3` and `telephoto2`, the same on `telephoto2`'s lens group of one, and worse on `ultrawide2` and `ultrawide3`.
- **A false bend on a flat frame.** Fitted alone, `main_flat3` keeps a bend it does not have, F 22.7 against 4.62. Its selected worst bull, 0.00814 in, is nonetheless better than the joint fit's plane, 0.01108. It is the F test's second false positive on a flat sheet, after M1.5's filled scan.

#### Is what the fit leaves structured or random? (entry 17 section 5)

**Reproduce:** `grouplab surface correlation`, raw rows `scans/phase0/measurements/surface-correlation.json`. It reads the fit above.

**Method.** The same question Phase 0 measurement 6 asked of the printer's displacement field, asked of each frame's post-fit residual.

- **Per marker.** Each usable corner's signed page residual is averaged over its marker, and the frame's mean is removed. Corners of one marker are 40 dmm apart and share their detection, so pairing them would read as structure that is not the sheet.
- **The statistic.** The mean dot product of neighbouring markers' residuals, those within 1.5 times the median nearest-marker distance, over the mean squared residual. It is near 0 for independent error and near 1 for a field that varies slowly across the sheet.
- **Significance.** The markers' residuals are shuffled among their positions 2000 times. Structured means p below 0.001 with a positive correlation.

| Gate | Photograph | Markers | RMS marker residual (dmm) | Neighbour distance (dmm) | Neighbour pairs | Neighbour correlation | p | Far correlation | Reading |
|---|---|---|---|---|---|---|---|---|---|
| mounted | `ultrawide1.jpg` | 34 | 1.96 | 570 | 104 | +0.18 | < 0.001 | +0.01 | structured |
| mounted | `ultrawide2.jpg` | 32 | 2.41 | 570 | 97 | +0.15 | 0.003 | -0.09 | not distinguishable from random |
| mounted | `ultrawide3.jpg` | 30 | 3.25 | 570 | 88 | +0.20 | < 0.001 | -0.18 | structured |
| mounted | `main1.jpg` | 34 | 1.32 | 570 | 104 | +0.06 | 0.054 | +0.01 | not distinguishable from random |
| mounted | `main2.jpg` | 24 | 2.18 | 570 | 68 | +0.29 | < 0.001 | -0.18 | structured |
| mounted | `main3.jpg` | 27 | 1.80 | 570 | 76 | +0.08 | 0.077 | -0.04 | not distinguishable from random |
| mounted | `telephoto2.jpg` | 33 | 2.65 | 570 | 96 | +0.15 | 0.012 | +0.05 | not distinguishable from random |
| flat | `main_flat1.jpg` | 34 | 0.60 | 570 | 104 | +0.09 | 0.032 | -0.10 | not distinguishable from random |
| flat | `main_flat2.jpg` | 25 | 0.70 | 570 | 70 | +0.22 | 0.002 | -0.17 | not distinguishable from random |
| flat | `main_flat3.jpg` | 23 | 0.75 | 570 | 63 | -0.01 | 0.293 | +0.02 | not distinguishable from random |

Calibration, fitted the same way:

| Case | RMS marker residual (dmm), median | Neighbour correlation, median | Seeds structured at p below 0.001 | Far correlation, median |
|---|---|---|---|---|
| white corner noise 0.52 px | 0.36 | -0.12 | 0 of 10 | +0.00 |
| white corner noise 1.0 px | 0.63 | -0.08 | 0 of 10 | +0.00 |
| white corner noise 2.0 px | 1.32 | -0.06 | 0 of 10 | -0.01 |
| 0.25 in twist, 0.52 px | 0.74 | +0.21 | 3 of 10 | -0.01 |
| 0.50 in twist, 0.52 px | 1.52 | +0.28 | 10 of 10 | -0.01 |

**What it says.**

1. **It is mostly not structured, and partly structured on some frames.**
   - **Significant:** three of seven mounted frames, `ultrawide1` +0.18, `ultrawide3` +0.20 and `main2` +0.29.
   - **Borderline:** `ultrawide2` (p 0.003) and `telephoto2` (p 0.012), both at +0.15.
   - **Not correlated:** `main1` and `main3`, +0.06 and +0.08.
   - **The ceiling:** on no frame do neighbouring markers share more than about 29 percent of the residual variance.
2. **The calibration places it.** White corner noise after the fit reads -0.06 to -0.12, never significant. A synthetic twist of 0.25 in reads +0.21, significant on 3 of 10 seeds, and 0.50 in reads +0.28, on 10 of 10. The three structured mounted frames sit where a quarter to half inch of twist puts them.
3. **The flat controls are not clean.** `main_flat2` reads +0.22 at p 0.002 and `main_flat1` +0.09, with no bend at all. That bounds how much of any frame's structure can be put down to how the sheet is held.
4. **For the paper protocol, both sentences apply, in this order.**
   - **Corner quality first:** light, aperture and distance. On the mounted frames the per-marker residual is 1.3 to 3.3 dmm, against 0.6 to 0.75 dmm on the flat ones, and most of that excess is not shared by neighbouring markers.
   - **How the sheet is held second.** Three frames carry a structured part that a quarter to half inch of twist would produce.
5. **Entry 17's clue is not settled by this.** Double the residual producing twenty times the bull error was expected to mean correlated error. One reading consistent with both is error coherent within a marker, all four corners moving together. That does not average out over 136 corners, since only about 34 markers are independent, and it reads as random between neighbours. It is not measured here.

## M2. Hole detection

### M2.1 The baseline: the neutral-darkness detector, ported and revalidated

`docs/PHASE1-BRIEF.md` section 4.1: port the primitive of `docs/SCAN-MEASUREMENTS.md` section 3.1 into `GroupLab.Core`, revalidate it on all 343 holes and the messaging-app copy, and commit it before anything is built to beat it.

**Reproduce:** `grouplab holes baseline`, raw rows `scans/phase1/measurements/holes-baseline.json`, every detection and every rejected blob, in inches.

**The port.** `NeutralDarknessHoleDetector`, in `src/GroupLab.Core/Detection/`, follows `tools/scan_analysis/s05_holes.py` step for step:

- **The pipeline.** Neutral darkness `paper - max(R, G, B)`, with paper at the 90th percentile. Then open with a 0.032 in disk, threshold at 28, close with a 0.055 in disk, fill, and measure each blob by its convex hull.
- **The filters.** Hull diameter 0.15 to 0.60 in, hull solidity at least 0.55, bounding-box aspect at most 2.2.
- **The per-hole measures.** Paper level, core mean V and annulus minimum V, from 360 rays at half-pixel steps.
- **Where it runs.** The morphology and blob extraction are the two operations the survey called in OpenCV, and they sit behind `IImagingBackend`. The arithmetic, the filters and the measures are in Core, in the survey's order.
- **Why it works.** The code comments carry the survey's reasons: an opening disk wider than any printed stroke, neutral darkness that is blind to single-hue artwork, and a hull that recovers a C-shaped rim.
- **Its test.** `NeutralDarknessHoleDetectorTests` keeps a drawn hole and erases a ring stroke and a rule.

| File | DPI | Holes, survey / port | Hull diameter mean ± sd, survey | Port | Core mean V, survey / port | Annulus minimum V, survey / port |
|---|---|---|---|---|---|---|
| 28_6_5 cci_450 | 600 | 16 / 16 (16 detected) | 0.2201 ± 0.0319 | 0.2201 ± 0.0319 | 195.4 / 195.4 | 24.8 / 24.8 |
| 28_6_5 40_3 gm205mar | 600 | 25 / 25 (25 detected) | 0.2388 ± 0.0349 | 0.2388 ± 0.0349 | 194.1 / 194.0 | 31.5 / 31.5 |
| 28_6_5 rem_7_5_br | 600 | 24 / 24 (24 detected) | 0.2188 ± 0.0279 | 0.2188 ± 0.0279 | 190.0 / 190.0 | 36.3 / 36.3 |
| 28_6_5 42_4 gm205mar | 600 | 20 / 20 (20 detected) | 0.2410 ± 0.0715 | 0.2410 ± 0.0715 | 194.8 / 194.8 | 48.0 / 48.0 |
| 300_nm_factory | 600 | 17 / 17 (17 detected) | 0.2649 ± 0.0408 | 0.2649 ± 0.0408 | 205.3 / 205.3 | 16.4 / 16.4 |
| 300_nm_hand_load | 600 | 25 / 25 (25 detected) | 0.2958 ± 0.0376 | 0.2958 ± 0.0376 | 198.0 / 198.0 | 18.9 / 18.9 |
| 338lmao | 600 | 28 / 28 (29 detected) | 0.3236 ± 0.0768 | 0.3236 ± 0.0768 | 187.8 / 187.8 | 68.0 / 68.0 |
| 6_5retumbo | 600 | 19 / 19 (19 detected) | 0.2256 ± 0.0572 | 0.2256 ± 0.0572 | 193.6 / 193.6 | 54.6 / 54.6 |
| IMG_20250530_0001 (300 DPI) | 300 | 7 / 7 (7 detected) | 0.1988 ± 0.0761 | 0.1988 ± 0.0761 | 169.9 / 169.7 | 21.7 / 21.6 |
| n568-gm210m | 600 | 28 / 28 (28 detected) | 0.2366 ± 0.0259 | 0.2366 ± 0.0260 | 197.8 / 197.8 | 41.7 / 41.7 |
| n568-ruag | 600 | 28 / 28 (28 detected) | 0.2435 ± 0.0267 | 0.2435 ± 0.0267 | 199.3 / 199.3 | 48.1 / 48.0 |
| n568 | 600 | 29 / 29 (29 detected) | 0.2961 ± 0.0241 | 0.2961 ± 0.0241 | 194.4 / 194.4 | 41.2 / 41.3 |
| retumbo.jpg | 600 | 25 / 25 (25 detected) | 0.3033 ± 0.0213 | 0.3033 ± 0.0213 | 186.6 / 186.6 | 21.6 / 21.6 |
| retumbo.png | 600 | 25 / 25 (25 detected) | 0.3110 ± 0.0421 | 0.3110 ± 0.0421 | 178.6 / 178.6 | 18.9 / 18.9 |
| retumbo_0001 | 600 | 27 / 27 (28 detected) | 0.2773 ± 0.0559 | 0.2773 ± 0.0559 | 191.0 / 191.0 | 60.4 / 60.4 |
| messaging copy (93 DPI, outside the pool) | 93 | 12 / 12 | | 0.1988 ± 0.1016 | / 180.6 | / 72.9 |

| Quantity | Survey | Port |
|---|---|---|
| holes | 343 | 343 |
| hull diameter (in) | 0.2655 ± 0.0570 | 0.2655 ± 0.0570 |
| paper V | 245.65 ± 9.93 | 245.65 ± 9.94 |
| core mean V | 192.55 ± 26.70 | 192.54 ± 26.72 |
| annulus minimum V | 38.53 ± 35.59 | 38.53 ± 35.59 |

**What it shows.**

1. **The port reproduces the survey exactly.**
   - **Per file:** every one of the fifteen files agrees on count, hull diameter mean and sample sd, core mean V and annulus minimum V, to the last digit the survey printed, with differences of at most 0.2 grey levels.
   - **Pooled:** 343 holes, 0.2655 ± 0.0570 in.
   - **Messaging copy:** it gives the survey's 12 detections at 93 DPI.
2. **Two holes are detected and not in the survey's tables, and the survey's roll-up is why.**
   - **What happened:** `tools/scan_analysis/s12_summarise_holes.py` keeps holes from 0.15 to 0.55 in, while the detector accepts them to 0.60 in. `338lmao` and `retumbo_0001` each have one detected hole between the two.
   - **How the comparison handles it:** it applies the roll-up's band and reports both counts.
   - **An inconsistency inside the survey:** section 3.5 of the survey counts 338lmao at 29 holes, 0.3317 ± 0.0856 in. That is the detector's 29, not section 3.7's 28.
3. **On `300_nm_hand_load` it makes 25 detections, the survey's 25 verified true positives, and misses the same two holes a person located.** The nearest detections are 0.661 and 0.164 in away, against a hit tolerance of 0.15 in.
4. **What this does not show, stated plainly.**
   - **Not recorded anywhere:** the survey's per-hole coordinates, which would let each of the 25 be matched by position. The run that produced them wrote `work/holes_all.json`, which is not in the repository.
   - **What is shown instead:** identical counts, identical per-file and pooled statistics, and the same two misses. Brief section 4.4's first gate, at least 25 of 27 with zero false positives, is met as a reproduction of a verified result, not as a fresh position check.
   - **What would close it:** the survey's coordinates, committed as a fixture.
5. **The raggedness measure is ported as well.** It is the survey's section 3.4 figure, the spread of the rim radius over angle, and pools to 0.0470 ± 0.0250 in for both, which the synthesis below is calibrated against.

### M2.2 Render-and-difference on synthetic GroupLab sheets

`docs/PHASE1-BRIEF.md` sections 4.2 to 4.5: render the sheet as printed, map it through the registration, difference, and measure it against truth on synthetic sheets, sweeping registration error to find where it breaks.

**Reproduce:**
- `grouplab holes synthetic` runs the development seeds, 1 to 3, which every change below was made on. Raw rows go to `scans/phase1/measurements/holes-synthetic.json`.
- `grouplab holes synthetic --held-out` runs seeds 1001 to 1003, which nothing was developed against, once, after the last change. Raw rows go to `holes-synthetic-held-out.json`.
- Every row is one sheet and method. It holds counts, centre errors, every missed hole with the parameters it was drawn with, and every stray with its rim closure.

**The detector.** `RenderDifferenceHoleDetector`, in `src/GroupLab.Core/Detection/`, stages S5 to S8 of `docs/DETECTION-PIPELINE.md`:

- **S5, the expected image.** The definition is rasterised and resampled through the registration by `ExpectedImage`, which a test holds pixel for pixel against the render.
  - **Paper:** the 95th percentile of the observed pixels the render calls paper, in quarter-inch blocks, smoothed.
  - **Ink level:** the median ratio of observed to paper where the render calls ink.
- **S5, local alignment.** Each bull's cell of the expected image is aligned to the observed one by phase correlation, up to 0.1 in, and the rest of the page by the median of those shifts.
- **S6, the residual.** The absolute difference of observed and expected, both normalised by paper, as a fraction of the paper-to-ink range. It counts in both directions, because a hole on a black disc only makes the page lighter.
- **S7, candidates.**
  1. Open with a 0.012 in disk.
  2. Threshold at 0.15 of the range.
  3. Close with a 0.055 in disk.
  4. Fill, and take each blob's convex hull.
- **S8, filters and centres.**
  - **Carried over:** the survey's size, solidity and aspect filters.
  - **Exclusion:** the declared zones (markers with quiet zones, codes, data block, identifier), and the position prior (a candidate outside every bull's cell is refused).
  - **Centre:** the residual-weighted centroid.
  - **Merged neighbours:** a blob whose residual has an elongation of at least 1.45 is split by two-means into two holes, both flagged.
  - **Oversized:** a hole wider than the sheet's median by two robust standard deviations is flagged.
  - **Rim closure:** recorded for every hole, and not gated.
- **S9, assignment.** `ShotAssignment` matches one to one when detections are no more than bulls, and otherwise uses nearest-bull with every shot flagged. A margin under 0.15 in, or a shot not given its nearest bull, is flagged either way.

**The synthesis.** `SyntheticSheet` draws holes from `docs/SCAN-MEASUREMENTS.md` sections 3.3 to 3.6 into the render of `GL-CF25-LTR`, which has 28 bulls. The recipe:

- **Hole shape:** a lobed rim, a core, and a disturbed zone around the rim.
- **Ink versus paper:** holes on ink get section 3.6's darker, thicker rim.
- **Backing:** the core is the scanner lid, except in the dark-backing case.
- **The page:** hand ink laid over, a paper gradient, a 0.6 px blur and grey noise of 2.

The cases:

- **Main:** 600 and 300 DPI, one and two holes per bull.
- **Registration:** 600 DPI, the registration wrong by a known translation or by a rotation about the page centre.
- **Hard:**
  - overlapping pairs;
  - X marks over holes;
  - arrowheads in the cells and letter bowls below them;
  - holes on the outer ring's edge;
  - a dark backing;
  - cross-cell shots, placed 0.52 to 0.60 of the way to the nearest other bull.

**Scoring.** A detection matches a truth hole within 0.15 in, nearest pairs first. "Within 0.01 in" is gate 2's tolerance, and a stray is a detection with no truth hole within 0.15 in. The baseline sees the same images.

**Realism.** The synthetic holes and the survey's real ones, both measured by the baseline. The synthetic set is a blank page of 63 holes, three seeds, of which the baseline matched 161; the real set is section 3.6's 229 holes on bare paper.

| Quantity | Real (survey) | Synthetic |
|---|---|---|
| hull diameter (in) | 0.2717 ± 0.0537 | 0.3244 ± 0.0760 |
| core mean V | 195.6 ± 24.5 | 175.6 ± 27.0 |
| annulus minimum V | 33.8 ± 30.0 | 32.1 ± 26.7 |
| raggedness, sd of rim radius (in) | 0.0456 | 0.0370 ± 0.0120 |

**Calibration was timeboxed at four iterations and the gaps are reported, not closed.**

| Iteration | Hull diameter (in) | Core mean V | Raggedness (in) |
|---|---|---|---|
| first blank page | 0.301 | 161 | 0.026 |
| 2 | 0.296 | 159 | 0.038 |
| 3 | 0.362 | 186.6 | 0.025 |
| 4, kept | 0.324 | 175.6 | 0.037 |

The synthetic holes are 19 percent wider, 20 grey levels darker in the core and 19 percent less ragged than real ones. Wider, darker-cored and rounder holes are all easier to find. So the synthesis is optimistic, and the recall below is an upper bound for these conditions, not a prediction for paper.

**Development, seeds 1 to 3, render-and-difference.** Each step was made because of the one before.

| Step | 600 DPI, one per bull, within 0.15 / 0.01 in | 600 DPI, two per bull, within 0.15 in | Registration off 0.02 / 0.04 in, within 0.15 in | Strays on the arrowhead sheets |
|---|---|---|---|---|
| Survey opening, 0.032 in | about 85 / 36% | | | |
| Opening 0.012 in | 97.6 / 70.2% | 73.8% | 60.7 / 0.0% | 49 |
| Cells aligned by phase correlation | 97.6 / 70.2% | 73.8% | 92.9 / 98.2% | 49 |
| Merged neighbours split, oversized flagged | 97.6 / 70.2% | 95.2% | 92.9 / 98.2% | 56 |

1. **The survey's opening erased faint holes.** Both detectors found about 85 percent, and every miss was a thin rim, 0.010 to 0.037 in wide, around a core at V 215 to 244. The 0.064 in disk is wider than the rim, and the core is too close to paper to survive the threshold. In a difference the printed artwork has already cancelled, so the opening only has to remove registration slivers, and 0.012 in does that.
2. **The smaller opening broke registration.**
   - **What broke:** off by 0.02 in, 60.7 percent; off by 0.04 in, none, because every printed edge left a sliver the small disk could not remove.
   - **The fix:** the printed rings are fiducials in their own right, so each cell is aligned locally.
3. **Two holes per bull lost a quarter of them to merging.** 26 of 44 misses came in pairs within 0.45 in, joined by the closing and then refused as too large or reported as one.
   - **What separates them:** the residual's elongation. Single holes read at most 1.26 to 1.37 at the 99th percentile by case, and 1.61 at worst. Merged neighbours read from 1.50, with a median of 1.86 to 2.15.
   - **The threshold:** 1.45 lies between the two.
4. **Rim closure did not separate arrowheads, and is not gated.** This is the signature `docs/DETECTION-PIPELINE.md` S8 adds for the purpose.
   - **The overlap:** arrowheads read 0.65 to 0.81, and four read 1.00 where the centre fell on a stroke. Holes read as low as 0.73.
   - **Why it stays ungated:** a real rim is often a C, which the synthesis never draws, so a gate set here would refuse real holes the synthesis cannot show.

**The gate reading, held-out seeds 1001 to 1003, run once.** Main sheets:

| DPI | Holes per bull | Method | Holes | Within 0.01 in | Within 0.15 in | Strays | Centre error median / 95th pct (in) |
|---|---|---|---|---|---|---|---|
| 600 | 1 | render-and-difference | 84 | 64 (76.2%) | 84 (100.0%) | 1 | 0.0063 / 0.0192 |
| 600 | 1 | baseline | 84 | 30 (35.7%) | 72 (85.7%) | 0 | 0.0112 / 0.0522 |
| 600 | 2 | render-and-difference | 168 | 104 (61.9%) | 158 (94.0%) | 2 | 0.0074 / 0.0375 |
| 600 | 2 | baseline | 168 | 42 (25.0%) | 113 (67.3%) | 6 | 0.0147 / 0.1069 |
| 300 | 1 | render-and-difference | 84 | 69 (82.1%) | 84 (100.0%) | 0 | 0.0067 / 0.0148 |
| 300 | 1 | baseline | 84 | 31 (36.9%) | 70 (83.3%) | 1 | 0.0122 / 0.0453 |
| 300 | 2 | render-and-difference | 168 | 95 (56.5%) | 162 (96.4%) | 0 | 0.0082 / 0.0328 |
| 300 | 2 | baseline | 168 | 55 (32.7%) | 121 (72.0%) | 2 | 0.0125 / 0.0657 |

Registration error, render-and-difference, 600 DPI, one per bull, 56 holes each:

| Registration | Within 0.01 in | Within 0.15 in | Strays | Centre error median / 95th pct (in) |
|---|---|---|---|---|
| off by 0.005 in | 45 (80.4%) | 56 (100.0%) | 1 | 0.0064 / 0.0166 |
| off by 0.010 in | 45 (80.4%) | 54 (96.4%) | 1 | 0.0058 / 0.0135 |
| off by 0.020 in | 43 (76.8%) | 55 (98.2%) | 0 | 0.0060 / 0.0129 |
| off by 0.040 in | 43 (76.8%) | 56 (100.0%) | 1 | 0.0065 / 0.0148 |
| off by 0.080 in | 49 (87.5%) | 56 (100.0%) | 0 | 0.0060 / 0.0109 |
| rotated 0.25 deg, corners off 0.030 in | 38 (67.9%) | 54 (96.4%) | 0 | 0.0064 / 0.0166 |
| rotated 0.50 deg, corners off 0.061 in | 45 (80.4%) | 56 (100.0%) | 0 | 0.0062 / 0.0171 |
| off by 0.120 in | 0 (0.0%) | 0 (0.0%) | 0 | |
| off by 0.160 in | 0 (0.0%) | 0 (0.0%) | 0 | |
| off by 0.240 in | 0 (0.0%) | 0 (0.0%) | 48 | |

The last three rows were added after the first held-out run, when no break had appeared by 0.08 in. The detector was unchanged, the new cases were appended so every earlier case drew the same sheets, and both seed sets were run again. Every earlier row came back identical, and the development seeds give the same three rows.

Hard cases, 600 DPI, two sheets each:

| Case | Holes | Render-and-difference within 0.01 / 0.15 in | Strays | Baseline within 0.15 in | Baseline strays |
|---|---|---|---|---|---|
| overlapping pairs | 112 | 2 (1.8%) / 54 (48.2%) | 0 | 53 (47.3%) | 0 |
| X marks over holes | 56 | 37 (66.1%) / 53 (94.6%) | 0 | 50 (89.3%) | 0 |
| arrowheads and letter bowls | 56 | 41 (73.2%) / 55 (98.2%) | 53 | 49 (87.5%) | 0 |
| holes on the outer ring edge | 56 | 54 (96.4%) / 56 (100.0%) | 0 | 47 (83.9%) | 0 |
| dark backing | 56 | 38 (67.9%) / 55 (98.2%) | 0 | 56 (100.0%) | 0 |
| cross-cell shots | 56 | 48 (85.7%) / 55 (98.2%) | 0 | 51 (91.1%) | 1 |

Assignment of render-and-difference's detections, held-out:

| Case | Matched | S9 right | Nearest-bull right | S9 differs from nearest |
|---|---|---|---|---|
| cross-cell shots | 55 | 55 | 47 | 8 |
| registration off by 0.010 in | 54 | 51 | 54 | 3 |
| every other case | | as nearest-bull | as S9 | 0 |

**What it shows.**

1. **Gate 2 is not met, and the reason is mostly the tolerance.**
   - **Within 0.15 in:** one hole per bull is recovered completely at both resolutions. Two per bull reaches 94.0 and 96.4 percent.
   - **Within 0.01 in:** 56.5 to 82.1 percent.
   - **Strays:** 0 to 2 per case, over three sheets.
   - **Why the tolerance fails:** the median centre error is 0.0063 to 0.0082 in. That is the survey's 0.008 in noise floor on real paper, and it puts 0.01 in near the 75th percentile of the error.
   - **What the centre is:** truth is the centre a lobed rim was drawn around, and no detector's centroid of a lobed star recovers that to better than the lobes allow.
   - **Against the brief's warning:** a synthetic figure far below the noise floor would mean the synthesis is too clean, and this one is not below it.
2. **Render-and-difference is better than the baseline everywhere except the dark backing, by a wide margin.**
   - **Recall:** 94 to 100 percent against 67 to 86 percent on the main sheets.
   - **Centre error:** about half the baseline's at the median and a third at the 95th percentile.
   - **Strays:** fewer on two holes per bull.
3. **Registration holds to 0.08 in, then breaks at a cliff where the local alignment's reach ends.**
   - **Where it holds:** from 0.005 to 0.080 in of translation, and through 0.5 degrees of rotation. Without local alignment the detector broke at 0.02 in.
   - **Where it breaks:** off by 0.12 and 0.16 in, it finds nothing. Those errors are past the 0.1 in reach, so every cell's shift is refused and the difference is back to its unaligned state.
   - **Why the break is catastrophic, not graceful:** off by 0.24 in, it reports 48 strays and no holes, because misaligned ring edges wide enough to pass every filter are taken for holes.
   - **The guard it needs, not built yet:** refuse the sheet, with the reason, when most cells cannot be aligned. Reporting no holes and saying why is graceful; reporting 48 false ones is not. On a real sheet, 0.1 in is far outside anything the fiducials let through, so this bounds a failure of registration rather than its normal error.
4. **Two hard cases fail, and both are the survey's.**
   - **Overlapping pairs:** a pair whose rims overlap reads as round as one hole, elongation 1.01 to 1.15, so no split finds it. On a sheet where every bull holds a pair, the sheet median is itself a pair's width, so the oversized flag does not fire either. 50 of the 52 merged detections are reported silently as one. That is what `docs/DETECTION-PIPELINE.md` forbids for bull 20 of `300_nm_hand_load`, where one pair among singles would stand out against the median. What would catch it is S8's size window from the calibre, which the synthetic sheet does not carry.
   - **Arrowheads in the cells:** a black arrowhead is dark, compact and hole-sized after closing, and gives 53 strays over two sheets. It survives the 0.012 in opening, which the survey's 0.032 in opening did not allow. The survey calls black marker on a greyscale target the hardest case in its corpus. The letter bowls placed below the bulls and in the data block made no strays, because the cell prior and the declared zone refuse them.
5. **One-to-one assignment gets every cross-cell shot right, and nearest-bull misses 8 of 55.** Its cost is completeness.
   - **When a hole is missed:** one-to-one can move a neighbour into the empty bull. It did so three times at 0.010 in, where nearest-bull was right.
   - **When there is one stray:** it takes a 28-bull sheet to 29 detections, and the whole sheet falls back to nearest-bull with all 29 flagged. That happened on four held-out sheets.
6. **What this does not show, stated plainly.**
   - **Not measured on paper:** anything on a GroupLab sheet. The recall is an upper bound, for the realism reasons above.
   - **Not in the synthesis:**
     - C-shaped rims;
     - colour and chroma, so blue ink cannot be rejected on hue;
     - printed ink spread;
     - a registration error that varies across the page other than by rotation.
   - **The sample size:** 84 holes per main case, where one miss is 1.2 percent. So "at least 99 percent" can only be read here as none missed.

**For planning, and not blocking M3.**

1. **Gate 2's 0.01 in tolerance sits at the real noise floor.** The options:
   - keep it, and the gate fails by construction on any honest synthesis;
   - gate recall at a match tolerance and report centre error as gate 3 already does;
   - set the tolerance from the noise floor, for example 0.02 in, which held-out one per bull meets at 95th percentiles of 0.015 to 0.019 in.

   I would choose the second. It separates finding a hole from locating it, and the location is judged against paper, not against a drawn centre.
2. **Arrowheads and overlapping pairs need signatures the synthesis cannot test.** The two candidates are chroma for blue ink and a calibre size window for pairs. Otherwise they are M4's review queue, which DESIGN.md section 13 requires anyway.

---

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
