# Phase 1 results, the milestone work, before results were written per entry

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## M0. The marker module sweep

`docs/FIDUCIAL-DECISION.md` section 10, measurement 2: the same target printed at 0.3, 0.4, 0.5, 0.6 and 0.8 mm modules, to find the dot-gain floor on the actual printer. The sheets are generated and checked here; the measurement needs paper, and is next weekend's.

**Reproduce:** `python -B tools/layout/module_sweep.py`, then `grouplab sweep module targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json scans/phase1/module-sweep/layouts.json scans/phase1/module-sweep`. Everything is in `scans/phase1/module-sweep/`, with a README. Since the geometry change of `docs/NOTES-FROM-PLANNING.md` entry 13 the sweep's base is that frozen definition rather than the live `GL-CF25-LTR`, whose sighter row moved, and test 26f is an error, so a rerun reports the 26f findings below as errors and renders the sheets regardless; every committed definition and PDF is unchanged.

| Module | Marker / quiet zone / footprint (dmm) | Markers | Lattice matches `layout.py` | Identifier | Validator | Test 43 worst bull, 300 / 600 DPI (in) | PDF pages |
|---|---|---|---|---|---|---|---|
| 0.3 mm | 24 / 6 / 36 | 44 | yes | `GL-DFA3-H72S-8KKS-A00S` | clean | 0.00013 / 0.00003 | 1 |
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

**Amended 15 September 2026, `docs/NOTES-FROM-PLANNING.md` entry 52: the markers are now sorted before use.**
- **Regenerated:** every table below that a command prints today. That is M0's test 43 row, M1.10's real-frame tables from `surface general`, and M1.11's frames-alone and correlation tables from `surface frames`, `surface noise` and `surface correlation`, with the prose that reads them.
- **Left as measured:** the tables from fits no command reproduces today, in the detector's marker order. They are M1.5's joint fits and its comparison table, M1.7's held lens, M1.8's joint noise run and M1.9's grouped joint fits. Their whole-sheet figures are superseded by `docs/PHASE0-RESULTS.md` section 3a.
- **What moved, and why it is not an improvement:** "Entry 52" below.

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

`grouplab surface frames` was first run once in the foreground on 14 September 2026 per `docs/NOTES-FROM-PLANNING.md` entry 14, with the surface fit of `88dc0f9` (was `d78c17c` before the 2026-09-14 rewrite) plus two changes that do not alter what is computed: a tabulated profile and cached rotation in `SurfaceMapping`, held to 1e-6 px of the direct geometry by `DevelopableSurfaceTests`, and the frames prepared and evaluated in parallel. **It crashed after 60 seconds and wrote no raw rows.** Per entry 14 it was not retried and the frame set was not reduced.

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
| `gl-cf25-ltr-1-600-dpi.png` | 34/34 | 0.00251 | 0.00394 | 0.064 | -10.6 (6.91) | no | 0.00251 | pass / pass / pass |
| `gl-cf25-ltr-2-600-dpi.png` | 34/34 | 0.00316 | 0.00372 | 0.173 | -14.0 (6.91) | no | 0.00316 | pass / pass / pass |
| `gl-cf25-ltr-3-600-dpi.png` | 34/34 | 0.00290 | 0.00406 | 0.015 | -11.9 (6.91) | no | 0.00290 | pass / pass / pass |
| `gl-cf25-ltr-96.2-600-dpi.png` | 34/34 | 0.00340 | 0.00329 | 0.059 | 1.1 (6.91) | no | 0.00260 | pass / pass / pass |
| `gl-cf25-ltr-d-blank-600-dpi.png` | 34/34 | 0.00219 | 0.00240 | 0.103 | -4.5 (6.91) | no | 0.00219 | pass / pass / pass |
| `gl-cf25-ltr-d-filled-600-dpi.png` | 34/34 | 0.00251 | 0.00290 | 0.221 | 11.2 (6.91) | yes | 0.00290 | pass / pass / pass |
| `gl-lr300-t-1-600-dpi.png` | 9/9 | 0.00254 | 0.01867 | 2.515 | 4.1 (6.91) | no | 0.00254 | pass / fail / pass |
| `gl-lr300-t-2-600-dpi.png` | 9/9 | 0.00271 | 0.02087 | 2.573 | 2.0 (6.91) | no | 0.00271 | pass / fail / pass |
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

**Re-run.** All three commands were re-run, and their raw rows now hold these figures. The earlier rows are at `adbc0cc` (was `fa36186` before the 2026-09-14 rewrite).

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
| flat | `main_flat1.jpg` | 0.00343 / 0.00661 | 0.00443 / 0.00654 | 0.09427 / 0.00496 | 0.09427 / 0.00496 | 0 / 0 / 16 / 16 | fail / fail / fail / fail |
| flat | `main_flat2.jpg` | 0.00566 / 0.01016 | 0.01780 / 0.01134 | 0.04277 / 0.02178 | 0.07760 / 0.05044 | 2 / 5 / 25 / 8 | fail / fail / fail / fail |
| flat | `main_flat3.jpg` | 0.01183 / 0.00496 | 0.02581 / 0.00648 | 0.06210 / 0.05572 | 0.06210 / 0.05572 | 6 / 9 / 21 / 21 | fail / fail / fail / fail |
| mounted | `main1.jpg` | 0.01824 / 0.05183 | 0.01177 / 0.00620 | 0.02770 / 0.05150 | 0.02770 / 0.05150 | 8 / 8 / 11 / 11 | fail / fail / fail / fail |
| mounted | `main2.jpg` | 0.08900 / 0.06950 | 0.05971 / 0.01856 | 0.05505 / 0.07232 | 0.05505 / 0.07232 | 22 / 19 / 24 / 24 | fail / fail / fail / fail |
| mounted | `main3.jpg` | 0.06540 / 0.11379 | 0.04522 / 0.00508 | 0.05452 / 0.01617 | 0.05116 / NaN | 20 / 7 / 23 / 13 | fail / fail / fail / fail |
| mounted | `telephoto2.jpg` | 0.04584 / 0.09626 | 0.02241 / 0.01371 | 0.02330 / 0.01102 | 0.02330 / 0.01102 | 21 / 8 / 8 / 8 | fail / fail / fail / fail |
| mounted | `ultrawide1.jpg` | 0.03902 / 0.07450 | 0.01682 / 0.04843 | 0.05385 / NaN | 0.05385 / NaN | 14 / 17 / 9 / 9 | fail / fail / fail / fail |
| mounted | `ultrawide2.jpg` | 0.08305 / 0.07438 | 0.03367 / 0.01675 | 0.03467 / 0.04004 | 0.05184 / 0.04464 | 19 / 11 / 23 / 21 | fail / fail / fail / fail |
| mounted | `ultrawide3.jpg` | 0.11331 / 0.10643 | 0.03695 / 0.04062 | 0.04669 / 0.03528 | 0.04669 / 0.03528 | 18 / 12 / 21 / 21 | fail / fail / fail / fail |

**The frames fitted alone do not depend on the joint fit, and they are the measurement.** Each frame has its own focal length and lens, as cylinder and as general surface:

| Gate | Photograph | Forward residual median (px) | Corners kept | Robust sigma (px per axis) | Rulings turn (deg) | Cylinder alone | General alone | Selected | Scoring bulls over the gate | Gate |
|---|---|---|---|---|---|---|---|---|---|---|
| flat | `main_flat1.jpg` | 0.65 / 0.64 | 136 / 136 of 136 | 0.55 / 0.55 | 52.4 | 0.00305 / 0.00830 | 0.00292 / 0.00728 | 0.00343 / 0.00661 | 0 / 0 / 0 | fail / fail / fail |
| flat | `main_flat2.jpg` | 0.86 / 0.80 | 100 / 100 of 100 | 0.74 / 0.70 | 29.4 | 0.02314 / 0.01054 | 0.01804 / 0.01002 | 0.00566 / 0.01016 | 6 / 8 / 2 | fail / fail / fail |
| flat | `main_flat3.jpg` | 0.66 / 0.63 | 91 / 91 of 92 | 0.58 / 0.55 | 25.1 | 0.00814 / 0.00465 | 0.00777 / 0.00416 | 0.00777 / 0.00416 | 3 / 4 / 4 | fail / fail / fail |
| mounted | `main1.jpg` | 0.87 / 1.08 | 125 / 128 of 136 | 0.74 / 0.92 | 26.2 | 0.00604 / 0.01820 | 0.00860 / 0.02981 | 0.00860 / 0.02981 | 4 / 9 / 9 | fail / fail / fail |
| mounted | `main2.jpg` | 1.56 / 1.62 | 70 / 72 of 104 | 1.46 / 1.54 | 36.6 | 0.05149 / 0.01694 | 0.05434 / 0.01460 | 0.05434 / 0.01460 | 19 / 19 / 19 | fail / fail / fail |
| mounted | `main3.jpg` | 0.94 / 0.94 | 96 / 98 of 108 | 0.88 / 0.83 | 37.0 | 0.03353 / 0.00968 | 0.03440 / 0.02806 | 0.03440 / 0.02806 | 5 / 7 / 7 | fail / fail / fail |
| mounted | `telephoto2.jpg` | 1.12 / 1.09 | 105 / 104 of 132 | 0.97 / 0.96 | 27.3 | 0.02241 / 0.01371 | 0.02330 / 0.01102 | 0.02330 / 0.01102 | 8 / 8 / 8 | fail / fail / fail |
| mounted | `ultrawide1.jpg` | 1.33 / 1.27 | 113 / 117 of 136 | 1.20 / 1.18 | 22.5 | 0.01044 / 0.02522 | 0.00976 / 0.02257 | 0.00976 / 0.02257 | 9 / 9 / 9 | fail / fail / fail |
| mounted | `ultrawide2.jpg` | 1.08 / 1.05 | 108 / 108 of 136 | 0.98 / 0.99 | 33.7 | 0.05784 / 0.00747 | 0.05767 / 0.01464 | 0.05767 / 0.01464 | 8 / 9 / 9 | fail / fail / fail |
| mounted | `ultrawide3.jpg` | 1.30 / 1.78 | 81 / 85 of 128 | 1.13 / 1.62 | 40.8 | 0.05509 / 0.03144 | 0.04669 / 0.03528 | 0.04669 / 0.03528 | 13 / 21 / 21 | fail / fail / fail |

**What the real frames say.**

1. **No mounted frame comes inside 0.005 in, as either surface.**
   - General surface alone: worst scoring bull 0.00860 to 0.05767 in.
   - Cylinder alone: 0.00604 to 0.05784 in.
2. **The general surface changes almost nothing on these frames.**
   - **Worst scoring bull:** it moves by -15 to +42 percent, better on three mounted frames and worse on four.
   - **Corners:** the median forward residual moves by at most 0.48 px, and the corners kept by at most four.
   - **Contrast with synthetic truth:** on a synthetic cone it removes the whole of the cylinder's error. The shape M1.7 found on these corners is not one this family takes up either.
3. **Its fitted turns carry no information.** They are 25 to 52 degrees on the three flat frames, where there is no bend for a turn to act on, and 23 to 41 degrees on the mounted ones.
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
| mounted | `ultrawide1` | 113 of 136 | 0.227 | 375.4 (4.62) | yes | 0.01044 / 0.02522 | 0.01044 / 0.02522 | 1.20 | 1.31 |
| mounted | `ultrawide2` | 108 of 136 | 0.307 | 489.4 (4.62) | yes | 0.05784 / 0.00747 | 0.05784 / 0.00747 | 0.98 | 1.03 |
| mounted | `ultrawide3` | 81 of 128 | 0.492 | 411.9 (4.62) | yes | 0.05509 / 0.03144 | 0.05509 / 0.03144 | 1.13 | 1.23 |
| mounted | `main1` | 125 of 136 | 0.417 | 267.5 (4.62) | yes | 0.00604 / 0.01820 | 0.00604 / 0.01820 | 0.74 | 0.77 |
| mounted | `main2` | 70 of 104 | 0.417 | 210.1 (4.62) | yes | 0.05149 / 0.01694 | 0.05149 / 0.01694 | 1.46 | 1.60 |
| mounted | `main3` | 96 of 108 | 0.440 | 587.9 (4.62) | yes | 0.03353 / 0.00968 | 0.03353 / 0.00968 | 0.88 | 0.93 |
| mounted | `telephoto2` | 105 of 132 | 0.397 | 920.8 (4.62) | yes | 0.02241 / 0.01371 | 0.02241 / 0.01371 | 0.97 | 1.02 |
| flat | `main_flat1` | 136 of 136 | 0.041 | -5.1 (4.62) | no | 0.00305 / 0.00830 | 0.00343 / 0.00661 | 0.55 | 0.57 |
| flat | `main_flat2` | 100 of 100 | 0.172 | -4.9 (4.62) | no | 0.02314 / 0.01054 | 0.00566 / 0.01016 | 0.74 | 0.78 |
| flat | `main_flat3` | 91 of 92 | 0.020 | 22.7 (4.62) | yes | 0.00814 / 0.00465 | 0.00814 / 0.00465 | 0.58 | 0.60 |

- **Mounted: still 0 of 7.** Worst scoring bull 0.00604 to 0.05784 in, corner sigma 0.74 to 1.46 px.
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
| mounted | `ultrawide1.jpg` | 34 | 2.07 | 570 | 104 | +0.15 | 0.002 | +0.01 | not distinguishable from random |
| mounted | `ultrawide2.jpg` | 32 | 2.43 | 570 | 97 | +0.16 | 0.002 | -0.10 | not distinguishable from random |
| mounted | `ultrawide3.jpg` | 30 | 3.02 | 570 | 89 | +0.18 | < 0.001 | -0.14 | structured |
| mounted | `main1.jpg` | 34 | 1.32 | 570 | 104 | +0.06 | 0.081 | +0.01 | not distinguishable from random |
| mounted | `main2.jpg` | 24 | 2.18 | 570 | 68 | +0.29 | < 0.001 | -0.18 | structured |
| mounted | `main3.jpg` | 27 | 1.80 | 570 | 76 | +0.08 | 0.068 | -0.04 | not distinguishable from random |
| mounted | `telephoto2.jpg` | 33 | 2.65 | 570 | 96 | +0.15 | 0.012 | +0.05 | not distinguishable from random |
| flat | `main_flat1.jpg` | 34 | 0.60 | 570 | 104 | +0.09 | 0.038 | -0.10 | not distinguishable from random |
| flat | `main_flat2.jpg` | 25 | 0.70 | 570 | 70 | +0.22 | < 0.001 | -0.17 | structured |
| flat | `main_flat3.jpg` | 23 | 0.75 | 570 | 63 | -0.01 | 0.311 | +0.02 | not distinguishable from random |

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
   - **Significant:** two of seven mounted frames, `ultrawide3` +0.18 and `main2` +0.29.
   - **Borderline:** `ultrawide1` at +0.15 and p 0.002, `ultrawide2` at +0.16 and p 0.002, and `telephoto2` at +0.15 and p 0.012.
   - **Not correlated:** `main1` and `main3`, +0.06 and +0.08.
   - **The ceiling:** on no frame do neighbouring markers share more than about 29 percent of the residual variance.
2. **The calibration places it.** White corner noise after the fit reads -0.06 to -0.12, never significant. A synthetic twist of 0.25 in reads +0.21, significant on 3 of 10 seeds, and 0.50 in reads +0.28, on 10 of 10. The two structured mounted frames sit where a quarter to half inch of twist puts them.
3. **The flat controls are not clean.** `main_flat2` reads +0.22 at p 0.001, structured by this test, and `main_flat1` +0.09, with no bend at all. That bounds how much of any frame's structure can be put down to how the sheet is held.
4. **For the paper protocol, both sentences apply, in this order.**
   - **Corner quality first:** light, aperture and distance. On the mounted frames the per-marker residual is 1.3 to 3.0 dmm, against 0.6 to 0.75 dmm on the flat ones, and most of that excess is not shared by neighbouring markers.
   - **How the sheet is held second.** Two mounted frames carry a structured part that a quarter to half inch of twist would produce, and so does one flat control.
5. **Entry 17's clue is not settled by this.** Double the residual producing twenty times the bull error was expected to mean correlated error. One reading consistent with both is error coherent within a marker, all four corners moving together. That does not average out over 136 corners, since only about 34 markers are independent, and it reads as random between neighbours. It is not measured here.

#### Amended 2026-09-15: a gently deformed sheet, measured (entry 23 section 3)

M1.11 was right about what it measured: nine photographs of a sheet hanging from one pin, free to twist. The N568 photograph against its own scan, in "Entries 19 and 20" below, is the first measurement of a sheet lying on a mat with a gentle sag, and there the surface models do most of the work. Residual RMS over its 25 bulls:

| Model | Residual RMS |
|---|---|
| Homography | 0.0206 in |
| Lens | 0.0181 in |
| Cylinder | 0.0078 in |
| General developable surface | 0.0063 in |

- **The statement is narrower than entry 17 left it.** No developable surface fits a sheet twisting on a pin, which is what the nine frames showed and what the synthetic sweep broke at a quarter inch of twist. On a gently deformed sheet the model takes most of the error away.
- **It does not pass the gate even there:** 0.0063 in against 0.005 in, on 25 bull constraints, far coarser than 136 marker corners.
- **The mounted requirement stays open.** That sheet lay on a mat, and no photograph in the collection is both a whole sheet and mounted. The mounted case deserves the measurement rather than being written off, and the next session's frames are the ones it needs.

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

**Re-recorded 2026-09-17:** the tables below are the split threshold of 1.45 as first read. `holes-synthetic.json` and `holes-synthetic-held-out.json` now hold the figures at 1.80, with the new oversize flag and residue rule, because real holes overruled 1.45 (entry 81, "Entry 81" below).

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

**Re-read under the amended gate.** Entry 18 section 3 accepted question 9, and brief section 4.4 point 2 now matches at 0.15 in, with centre accuracy reported under point 3. Read against the tables above, unchanged, gate 2 is still not met:

- **One hole per bull:** 100 percent recalled at both resolutions, with 1 stray on three sheets at 600 DPI against the gate's none.
- **Two holes per bull:** 94.0 and 96.4 percent, against 99, with 2 and 0 strays.
- **Registration:** it breaks at a cliff, and at 0.24 in into 48 false holes, which is catastrophic, not graceful.
- **Centre accuracy, reported:** median 0.0063 to 0.0082 in, at the 0.008 in paper floor.

**For planning, and not blocking M3.** The first is raised as question 9 in `docs/QUESTIONS-FOR-PLANNING.md`, because brief section 7 sends evidence that a gate is wrong there.

1. **Gate 2's 0.01 in tolerance sits at the real noise floor.** The options:
   - keep it, and the gate fails by construction on any honest synthesis;
   - gate recall at a match tolerance and report centre error as gate 3 already does;
   - set the tolerance from the noise floor, for example 0.02 in, which held-out one per bull meets at 95th percentiles of 0.015 to 0.019 in.

   I would choose the second. It separates finding a hole from locating it, and the location is judged against paper, not against a drawn centre.
2. **Arrowheads and overlapping pairs need signatures the synthesis cannot test.** The two candidates are chroma for blue ink and a calibre size window for pairs. Otherwise they are M4's review queue, which DESIGN.md section 13 requires anyway.

---

## M3. The statistics engine

### M3.1 The engine, against shotGroups and against its own specification

`docs/PHASE1-BRIEF.md` section 5: implement `docs/STATISTICS.md` as written, with section 15 as the build plan and section 15.5 as the gate, against the shotGroups 0.8.4 fixtures that `docs/NOTES-FROM-PLANNING.md` entry 18 committed.

**Reproduce:**
- `dotnet test --filter ShotGroupsFixtureTests` compares every key of the nine fixtures and prints each dataset's accounting.
- `dotnet test --filter "SpecificationTableTests|UnitSystemTests|DistributionTests"` checks the document's own tables and section 15.5 points 2, 4 and 6.
- `grouplab stats range-table [from to [replications]]` regenerates GroupLab's Monte Carlo table.
- `grouplab stats coverage` measures bootstrap coverage.

**What is built,** in `src/GroupLab.Core/Statistics/`, each file citing its section:

| File | Sections | What |
|---|---|---|
| `SpecialFunctions.cs`, `Distributions.cs` | 3.2, 15.3 | Log-gamma, incomplete gamma and beta in R's precision-preserving forms, `c4`; normal, chi-square, t and F in both tails with quantiles |
| `GroupStatistics.cs` | 3, 4, 6, 7 | Rayleigh sigma with estimated or known centre and its multiples; axis spreads; centre intervals; Hotelling; error and confidence ellipses; CorrNormal, Grubbs-Patnaik and Rayleigh CEP and hit probability |
| `GroupGeometry.cs` | 5, 6 | Extreme spread, bounding and minimum-area boxes, minimum enclosing circle, minimum-volume ellipse |
| `RangeStatistics.cs`, `RangeStatisticsSimulation.cs`, `RangeStatistics.csv` | 5, 15.3 | GroupLab's own range-statistic table and what a range statistic implies |
| `GroupComparison.cs` | 8 | Dispersion F test and ratio interval, MANOVA, exact Ansari-Bradley and Wilcoxon, Kruskal-Wallis, Fligner-Killeen, Holm |
| `ShapeTests.cs` | 7 | Circularity by the Bartlett-corrected likelihood ratio, simulated below 20 shots; vertical stringing by Pitman-Morgan |
| `Planning.cs` | 9, 10, 11 | Sample size and power, flyer expectations, pooled and all-in groups with the pre-pooling guard |
| `Bootstrap.cs`, `StatisticsRandom.cs` | 6 | BCa with percentile fallback, 9999 resamples, recorded seed, unreliable below 10 shots |
| `Angular.cs` | 12.5, 13 | Half-angle conversions, suppressed without exactly one distance |

**Not built, and why.** Section 12's solver-coupled analyses need the ballistic solver of `DESIGN.md` section 16, which no phase has built yet. The Rice offset hit probability and the elliptical CEP about the point of aim have no estimator specified in section 4. The Bayesian version is section 16 question 5, which is still open. Presentation, such as mean radius as the headline, is M4's.

**The gate, section 15.5.**

| Point | Gate | Reading |
|---|---|---|
| 1 | Every closed-form quantity within tolerance on all eight fixtures | **Met on what the fixtures can show.** All nine fixtures: 38,899 keys compared, 0 outside tolerance. Three classes are held back, each with evidence: 899 CorrNormal CEPs and 99 SMOA conversions disputed, and 2,163 frame-based keys of `DFcm` and `DFinch` awaiting a point of aim. All three are question 11 |
| 2 | `DFcm` and `DFinch` identical after conversion | **Met on the shots, not on the frames.** Linear results agree to 4.9e-14. Angular results differ by 1.21600e-5, exactly the rounding of 25 m to 27.34 yd (1.21601e-5). Shot 242 is in series 5 of one and series 4 of the other |
| 3 | Multiple distances suppress angular output | **Met.** `DFsavage`'s pooled scope has no angular keys and GroupLab produces none; the harness fails any angular value shotGroups suppressed |
| 4 | Sigma unbiased, 95 percent interval covering 94.0 to 96.0 percent over 10,000 simulated 25-shot groups | **Met.** Coverage 94.73 percent, bias +0.0008 |
| 5 | Monte Carlo table within 0.2 percent on means and 0.5 percent on quantiles, n 2 to 50, 1 to 10 groups | **Met.** All 490 cells; worst mean 4.7e-4, worst quantile 2.03e-3 |
| 6 | Estimated-centre and known-centre configurations both checked | **Met.** Known centre: coverage 94.51 percent, bias +0.0009, on 50 degrees of freedom against 48 |

**The fixture harness.** Every key is compared, excluded with a reason, awaiting, or disputed with checked evidence. A key that is none of these fails the test, so no section can pass by being skipped.

| Tolerance class (section 15.3) | Keys compared | Worst relative error |
|---|---|---|
| Closed form, 1e-12 relative | 22,742 | 9.8e-13 |
| Geometry, 1e-9 absolute | 8,909 | 6.3e-14 |
| Range lookup, 2e-3 relative | 3,290 | 1.4e-3 |
| Minimum-volume ellipse, 1e-4 relative | 1,770 | 3.0e-13 |
| Angular, 1e-12 relative | 1,188 | 6.2e-10, the 99 disputed SMOA keys; the rest pass |
| CorrNormal, 1e-8 relative | 977 | 2.3e-5, the 899 disputed CEPs; the hit probabilities pass |
| MANOVA intercept row, 1e-8 relative | 15 | 2.3e-10 |
| Rank test statistics, 1e-10 relative | 8 | 2.4e-12 |

| Excluded | Keys | Reason |
|---|---|---|
| Fixture inputs and provenance | 9,999 | Not outputs |
| Robust (MCD) estimates | 5,685 | No section specifies them |
| Eight further CEP estimators | 5,400 | Section 4 deliberately does not implement them |
| CEP about the point of aim | 1,300 | Section 4 names the case, specifies no estimator |
| Rice parameters | 600 | Section 4 uses the Rice distribution only for offset hit probability |
| Normality tests | 500 | Section 7 replaces them with the circularity and stringing tests |
| `groupShape.multNorm.p.value` | 100 | Stochastic, listed by the fixture |
| Fligner-Killeen and Kruskal-Wallis p-values | 18 | Monte Carlo: each is an integer over 9999, checked per key |

**GroupLab's Monte Carlo table.**
- **The grid:** 59 values of n (2 to 50, then every fifth to 100) by 1 to 10 groups, 10 million replications per cell, seed 20260914.
- **The run:** about 36 minutes on this machine, in six foreground runs.
- **The method:** each replication draws ten groups, and the running means over the first k groups give every group count its own full set of independent replications.
- **Between rows:** the table is read by R's `splinefun` (method fmm), as shotGroups reads `DFdistr`. At n = 92, the only fixture scope off the grid, a spline on shotGroups' own table reproduces its interval endpoint exactly, where linear interpolation is 4.8e-5 off.
- **Efficiency:** `getRangeStatEff` is M^2 / (n groups SD^2), which needs the standard deviation `DFdistr`'s export omitted, so GroupLab's table carries it. The worst range key, 1.4e-3 on a diagonal efficiency, is inside 2e-3.

**The specification's own tables,** reproduced to the digits printed:

- **Section 9.1:** all nine rows.
- **Section 9.2:** all six rows, the exact search matching the exact column in every row, 1651, 434, 203, 81, 26 and 10.
- **Section 10:** all seven rows, and the simulated 2.0675 and 2.7274 to four decimals.
- **Section 11:** eight 25-shot targets pool to 384 degrees of freedom.
- **Section 7's simulated sizes:** Pitman-Morgan 0.0502 at n = 10 with correlation 0.5, and the Bartlett-corrected likelihood ratio 0.0590 at n = 20, against the document's 0.059.

**What it shows.**

1. **The engine is exact where shotGroups is, and in two places more exact than shotGroups.**
   - **The quantile:** shotGroups' CorrNormal hit probabilities match GroupLab's Hoyt CDF to 1e-15, but its CEPs miss their own probability under that CDF by up to 7e-6, where GroupLab's meet it to 1e-12.
   - **The SMOA constant:** shotGroups' `fromMOA` for SMOA is 1 + 6.21288e-10 times the inverse of its own `getMOA`.
   - **What the harness does:** both are held as disputed only after it checks exactly that evidence, key by key. Question 11 asks how the gate should treat them.
2. **The fixtures cannot show everything the gate asks of them.**
   - **The point of aim:** `DFcm` and `DFinch` were read by shotGroups relative to a point of aim the fixture does not carry, so their frame-based results cannot be recomputed. The fixture's own frame-based and matrix-based centres disagree in all 20 of their scopes.
   - **The two frames:** they differ by one shot's series.
   - **The MANOVA:** the fixture's MANOVA row is `anova.mlm`'s intercept test, not section 8.2's test of group centres. GroupLab reproduces it and computes the group test separately.
3. **Conventions matched rather than argued.**
   - **`getMinBBox`'s angle:** the direction of the longer side.
   - **Extreme spread's pair:** any tied pair is the pair.
   - **`compareGroups`' box figures:** the minimum-area box's.
   - **Past the table:** the figure of merit's interval is named `FOM`.
4. **Two slips in `docs/STATISTICS.md`,** found by computing rather than copying:
   - **Section 3.4:** it prints sqrt(2 ln 2) as 1.1774100226, where the value 1.17741002251547 rounds to 1.1774100225.
   - **Section 10's table:** it prints 2.534 at 15 shots, where the alternating sum at 60 significant digits gives 2.533450.
   - **What the tests do:** they check the exact values and name both slips.
5. **The bootstrap under-covers, and more than section 6 warns.** `grouplab stats coverage`, BCa intervals for the Grubbs-Patnaik CEP(0.5) of an elliptical normal (standard deviations 1 and 2, correlation 0.3), 1000 datasets of 9999 resamples each:

   | Shots | Coverage | Binomial 95 percent band at nominal | Intervals below truth / above |
   |---|---|---|---|
   | 10 | 79.5 percent | 93.6 to 96.4 | 158 / 47 |
   | 25 | 89.1 percent | 93.6 to 96.4 | 69 / 40 |
   | 50 | 92.7 percent | 93.6 to 96.4 | 46 / 27 |

   - **The pattern:** the shortfall closes with n, and most misses lie below the truth. That is the bootstrap's known narrowness on a biased, skewed statistic at small n, not a failure of the BCa arithmetic, which fell back to percentile on none of the 3000 datasets.
   - **Why it matters:** section 6 flags only groups under 10 shots as unreliable, and at 25 shots the interval is still about a third too often wrong. Section 15.5 gates no bootstrap figure, so this is reported, not gated.
   - **For planning:** it bears on what the interface should say beside a bootstrap interval.
   - **Amended 2026-09-15, entry 23 section 2: this table is the bootstrap's entry in the gate record.** It is measured the way section 15.5 point 4 measures the closed-form interval, and recorded rather than gated because no section sets a bound for it. The closed-form intervals the marking panel shows cover 92.5 to 94.9 percent from 2 to 20 shots and are labelled with it, M4.2. No screen shows a bootstrap interval, and `Bootstrap` says it must never carry a bare 95.

**Amended 2026-09-15: question 11 answered, and the harness on the regenerated fixtures** (`docs/NOTES-FROM-PLANNING.md` entry 23 section 1).

- **The point of aim is read.** The harness computes `groupLocation`, `groupSpread`, `groupShape` and `compareGroups` from `shots.xPOA` and `shots.yPOA`, the frame shotGroups gave them, and everything else from `shots.x` and `shots.y`. The awaiting class is gone.
- **The CorrNormal CEP, answer A.**
  - **shotGroups' CEP** is compared at 1e-4 relative, and every key is inside it, as question 11's measured misses of up to 2.3e-5 said it would be.
  - **The distribution** stays gated through the hit probabilities at 1e-8.
  - **GroupLab's own CEP** is checked as a root of that distribution in every scope and both coordinate forms: 600 checks, worst 8.8e-14.
- **The SMOA inverse, answer A.** It is compared with shotGroups' constant, 1 + 6.21288e-10, encoded, at 1e-12.
- **Recorded in `docs/STATISTICS.md`:** section 15.4 items 10 to 12 (the MANOVA intercept row, the SMOA inverse, the CorrNormal root finder), and section 15.2's `DFcm` and `DFinch` row.
- **The count.**
  - **Compared:** 41,058 keys, 0 outside tolerance, where M3.1 compared 38,899 with 2,163 awaiting and 998 disputed.
  - **Disputed:** none.
  - **Pending:** 4. These are the Fligner-Killeen statistics on `DFinch` and `DFcm`, which no centring or tie rule tried here reproduces on a frame with a point of aim. They are question 14.

| Dataset | Compared | Excluded | Pending |
|---|---|---|---|
| `DF300BLK` | 318 | 282 | 0 |
| `DFscar17` | 308 | 212 | 0 |
| `DFcciHV` | 1,163 | 694 | 0 |
| `DF300BLKhl` | 1,580 | 973 | 0 |
| `DFcm` | 4,678 | 4,778 | 2 |
| `DFinch` | 4,678 | 4,778 | 2 |
| `DFsavage` | 3,943 | 2,614 | 0 |
| `DFlandy04` | 2,916 | 2,186 | 0 |
| `DFlandy01` | 21,474 | 11,063 | 0 |

- **Point 2 of section 15.5 is met on the shots:** `UnitSystemTests`. The series cannot meet it, because shot 242 is grouped differently in the two frames.

---

## Entries 19 and 20. A photograph against its own scan

`docs/NOTES-FROM-PLANNING.md` entry 20 section 5, asked for once M3 reported: the `N568 GM210M` sheet exists as a 600 DPI flatbed scan and as a phone photograph, so the scan, flat by construction, is ground truth for the photograph. It also carries entry 19's two measurements as far as one frame can.

**Reproduce:** `grouplab mounted pair`. Raw rows go to `scans/phase1/measurements/mounted-pair.json`.
- **The inputs:** `scans/n568-gm210m.jpg`, and `scans/mounted/20260329_183028.jpg`, a Galaxy Z Fold7 at 2.2 mm f/2.2 tagged as a 23 mm equivalent, 4000 by 3000 pixels.
- **What it is:** the sheet is lying on a mat, not mounted, with a fold across its bottom edge, so this is the flat control entry 20 section 5 names, not the mounted case.

**Bulls.** The OnTarget #18 sheet prints thirty coloured rings, each with a centre dot.
- **Why dots:** printed ink has chroma and a hole has none, but holes break the rings and the scan's rings carry light stripes, so rings do not survive as clean blobs. Dots do.
- **The method:**
  - each compact coloured blob of dot size is a candidate;
  - it is confirmed by 72 rays meeting a ring at a consistent radius;
  - its centre is a circle fitted to the ray crossings, trimmed at 2.5 robust standard deviations over three passes, as the survey trimmed its ring fits.
- **Orientation:** the photograph's pixels are stored a quarter turn from upright, with the turn only in EXIF. So the grid is placed by lattice phase on both image axes, the sighter line found by its 0.2-pitch offset, and the column direction chosen so the layout is not mirrored.
- **Bulls the dots missed:** they are predicted from the layout and found from their rings alone.
- **The count:**
  - scan: 27 bulls from their dots and 3 from the layout;
  - photograph: 23 from their dots and 7 from the layout, including two of the sighter row.
- **Ring fits:** 0.54 px RMS on the scan and 1.45 px on the photograph, over 65 and 68 of 72 rays. On the scan the dot centroids agree with the ring centres to a median 0.0019 in, worst 0.0054 in.

**The sheet's shape.** Each bull's photograph centre is mapped to the scan through the model, less its scan centre, in scan inches, with M1.11's neighbour correlation and its permutation p:

| Model | RMS residual (in) | Worst (in) | RMS (dmm) | Neighbour correlation | p | Detail |
|---|---|---|---|---|---|---|
| Homography | 0.02060 | 0.06000 | 5.23 | +0.18 | 0.009 | |
| Homography and radial lens | 0.01805 | 0.04492 | 4.58 | +0.17 | 0.004 | k1 +0.041, k2 -0.028 |
| Generalised cylinder | 0.00776 | 0.02774 | 1.97 | +0.15 | 0.001 | deflection 0.332 in, 26 of 30 kept |
| General developable surface | 0.00629 | 0.01721 | 1.60 | +0.09 | 0.050 | deflection 0.694 in, 27 of 30 kept |

**What it shows.**

1. **A sheet laid flat and photographed is not flat to this project's tolerance, and the departure is a bend.**
   - **The planar models:** they leave 0.018 to 0.021 in RMS and 0.045 to 0.060 in at worst, and a lens term takes up little of it.
   - **The surfaces:** a generalised cylinder takes the RMS to 0.0078 in, and the general developable surface to 0.0063 in with its worst bull at 0.017 in. What is left is not distinguishable from random.
   - **The fold:** the sheet has one across its bottom edge, visible in the photograph, and the general surface's deflection, 0.694 in, is twice the cylinder's 0.332.
2. **It sits between M1.11's flat and mounted frames.** Per point after the fit, M1.11's pinned mounted frames left 1.3 to 3.3 dmm and its flat frames 0.6 to 0.75 dmm, against 1.60 dmm here.
   - **The caveat:** those were marker corners on a GroupLab sheet, and these are ring centres on a commercial one, so the figures are the same kind of number, not the same measurement.
   - **Where it leaves the question:** a real sheet that Alan laid down and photographed needs the surface model as much as a pinned one. That strengthens M1.11's reading that the mounted requirement is about shape and corner quality, not about a pin.
3. **No frame in the collection is both a full sheet and mounted.** Entry 20 said so and this confirms it: the one frame measurable at all is a sheet on a mat. Entry 19's measurement A, bull-grid residuals on stapled targets, has no stapled full sheet to run on.

**Holes on a photograph.** This is entry 19's measurement B, on the one frame where it has truth.
- **The truth:** the neutral-darkness detector's 28 holes on the scan, where the survey verified it, mapped through the lens model.
- **The detector:** run untuned at the model's 320 px per inch.

| Run | Detections | Recall within 0.15 in | Strays | Centre difference median / worst (in) |
|---|---|---|---|---|
| Whole photograph | 0 | 0 of 28 | 0 | |
| The sheet alone, cropped to where the lens model maps the scan's page | 32 | 26 of 28 (93%) | 6 | 0.0231 / 0.0558 |

4. **On the whole photograph the detector finds nothing, and the reason is the frame, not the holes.**
   - **What happened:** the dark mat around the sheet passes the neutral-darkness threshold everywhere. Filling its outline makes one blob 12.2 in across that swallows every hole, refused as too large.
   - **The assumption it breaks:** the primitive assumes paper fills the frame, which a scan guarantees and a photograph does not.
   - **What fixes it:** registration, which is what the sheet-only run stands in for.
5. **On the sheet alone it recovers 26 of 28.**
   - **The two misses:** bull 4's round hole and the keyhole beside it, which in the photograph join into one blob and are refused as elongated or too large. That is the keyhole entry 20 section 4 predicted nothing would size correctly.
   - **The six strays:** three lie on the barcode at the foot of the sheet and three on the handwritten label, the survey's own false-positive population. The position prior of M2.2 would refuse all six.
6. **The centres differ from the scan's by 0.023 in at the median, four times the model's 0.006 in residual.** A scanned hole's core is the scanner lid and a photographed one's is the mat, and a ragged rim reads differently against each. That is the survey's point that the bright core is the lid, measured for the first time on the same holes.

**Amended 2026-09-15: detection now runs inside the registered sheet** (`docs/NOTES-FROM-PLANNING.md` entry 23 section 4).
- **What changed.** `RenderDifferenceHoleDetector`, the detector the automatic marking path calls, masks itself to the sheet. Before any stage reads the image, every pixel beyond the page's edge, mapped through the registration, is replaced by the sheet's own paper level. The residual is zeroed there too, so nothing outside can become a candidate, whatever frame a caller passes.
- **The first attempt failed its own test.** Masking only the residual was not enough: a synthetic sheet framed by 120 px of V 25 on every side gave 0 of 28 holes and one "too large" blob, because the dark border had already lowered the local paper estimate inside the sheet. With the border replaced by paper before S5, the same test finds 28 of 28 with no strays.
- **What `grouplab mounted pair` still shows, and why.** Its "whole photograph" row is unchanged at 0 of 28. That row runs the M2.1 neutral-darkness baseline, which takes no registration and so cannot know where the sheet is; it is kept as the control that showed the problem. The sheet-cropped row, 26 of 28, is what the pipeline now does for itself.
- **Not measured here:** render-and-difference on the N568 photograph itself, which needs that sheet's definition rather than the scan's grid.

---

## M4. The application shell

### M4.1 The marking screen

`docs/PHASE1-BRIEF.md` section 6: an Avalonia desktop shell that Alan can open and click on. `docs/NOTES-FROM-PLANNING.md` entry 21 decides its first screen: the manual marking path and the correction interface of `DESIGN.md` section 13 are the same screen, built once, and automatic detection pre-fills marks the user accepts, moves or deletes.

**Reproduce:**
- `dotnet run --project src/GroupLab.App` starts the application.
- `dotnet test tests/GroupLab.App.Tests` drives the screen headlessly.
- `dotnet test tests/GroupLab.Core.Tests --filter Marking` tests the model underneath it.

**What is built.**

| Where | What |
|---|---|
| `src/GroupLab.Core/Marking/MarkingSession.cs` | The marking as immutable states with undo and redo throughout; shots with provenance (automatic, corrected, manual), exclusion with a reason from section 10's short list, not-a-shot, and assignment to a bull |
| `src/GroupLab.Core/Marking/ScaleReference.cs` | Entry 21 section 4's two manual scales, a reference length and a reference rectangle, and a registered GroupLab sheet. Each says what it assumes |
| `src/GroupLab.Core/Marking/GroupAnalysis.cs` | The report from M3's engine: mean radius with its interval as the headline, sigma beneath, extreme spread subordinate, every figure with and without exclusions, the composite group about each shot's bull, section 10's expectation of the worst shot; and the JSON export |
| `src/GroupLab.Core/Marking/AutomaticMarking.cs` | The automatic path for a GroupLab sheet: registration, render-and-difference, one-to-one assignment; and section 13's snap of a rough tap to the hole under it |
| `src/GroupLab.App/` | The window and the canvas, in code rather than XAML |

**The screen,** in the brief's order:

1. **Open an image.** It is decoded once through OpenCV without applying EXIF orientation, the decode the pipeline measures, and shown from those pixels. A mark on the screen is therefore a mark on the pixels the statistics use, which the N568 photograph, stored a quarter turn from upright, showed is not a given.
2. **Register a GroupLab sheet** from its definition. The markers it did not find are crossed in red, and a failure is a prominent message with the advice to mark by hand, not an entry in a trace.
3. **Bulls and holes over the image.**
   - **Shots:** coloured by provenance and dashed when excluded.
   - **Assignment:** each assigned shot has a line to its bull.
   - **Scale and aim:** the scale reference and the point of aim are drawn too.
4. **Correct by hand:**
   - tap a shot and drag it;
   - tap a shot then a bull to reassign it;
   - mark a detection as not a shot, exclude a shot with a reason, unassign, delete;
   - undo and redo any of it.

   A detected shot the user touches becomes corrected.
5. **The statistics panel.**
   - **Headline:** mean radius with its interval, in a monospace, with sigma beneath and extreme spread smaller and dimmer.
   - **Exclusions:** with any shot excluded, every figure is shown without it as well.
   - **Beside the figures:** the centre from the aim, the error ellipse, the worst shot against what a group of that size is expected to do, and how the shots were placed.
   - **The scale:** the panel says how it was set, in orange when it assumes the photograph square on.
6. **Export** the marking and its report as JSON, `grouplab-marking-1`: GroupLab's own record, not another application's format.

**Not a mouse application.**
- **Every action is a tap or a drag:** a finger does both as well as a pointer does.
- **Every action has a button:** zoom has buttons as well as the wheel. Keyboard shortcuts duplicate the tools and undo for speed and are never the only way to do something.
- **Why:** entry 21 section 6 asks for that, and leaves the phone decision for later.

**Tests.**
- **Core, the marking model:** nine tests.
  - both manual scales, the rectangle recovering a point through a perspective to 1e-9;
  - undo and redo, and provenance;
  - exclusion reported both ways, with not-a-shot in neither;
  - the composite group, the report without a scale, the export, and snapping.
- **Core, the automatic path:** one test. On GL-CF25-LTR rendered at 300 DPI with a hole beside every bull, it registers with no marker missing and pre-fills one shot per bull, each assigned to the bull it was punched beside.
- **App, headless:** one test drives the real window through the headless platform's pointer input.
  - **The actions:** open an image, set a reference length by two taps, mark the point of aim, tap three impacts.
  - **The checks:** each tap snaps onto its hole, the headline is the engine's mean radius, and undo removes the last shot.

**Two faults the headless test found before a person did.**
1. **The canvas sized the image from the bitmap.** A bitmap's size follows the file's DPI tag, so a PNG tagged at another DPI would have scaled every mark, and in the headless platform the zoom came out 783-fold. The canvas now sizes the image from the decoded pixel grid.
2. **The selected-shot panel reused its exclusion-reason picker without detaching it.** The first tap on an impact would have thrown and closed the application.

**Not built, and why.**
- **Adjust to zero and calibre** (entry 21 section 5) are scope to be specified before they are built.
- **The phone** (section 6) is a decision not yet taken.
- **The pipeline trace timeline** of `DESIGN.md` section 19 is not in the brief's minimum scope.
- **Themes:** the brief rules out a theme engine, so the screen follows the system's light or dark setting through Avalonia's Fluent theme.
- **Where the imaging backend lives:** the shell references `GroupLab.Cli` for the OpenCV backend instead of moving it in this milestone.

### M4.2 First human use: entries 24 and 26

Alan's first session with the marking screen, `docs/NOTES-FROM-PLANNING.md` entry 24, and entry 26's amendment to its rotation finding.

**Reproduce:**
- `dotnet test tests/GroupLab.Core.Tests --filter "Marking|SmallGroupCoverage"`
- `dotnet test tests/GroupLab.App.Tests`

**1. No group size below five shots.** Two shots had printed `meanRadius 0.914 in (95% 0.476 to 5.744)`.
- **Withheld below the minimum.** Below 5 shots the panel prints no mean radius, sigma, extreme spread, ellipse or flyer line. It says "2 shots. At least 5 are needed before a group size is worth quoting, so none is shown", with section 9.1's range at that count. The count and the centre from the aim are always shown.
- **Where five comes from.** It is section 9.1's "the five-shot row is the one to put in front of a user". Section 9 gives no sharper threshold, so it is interim and question 12.
- **Below twenty shots,** the panel also prints section 9.1's range in words: "From 5 shots the true group size could be anywhere from 0.68 to 1.92 times what they measure".

**What the panel's intervals actually cover**, from `tests/GroupLab.Core.Tests/Statistics/SmallGroupCoverageTests.cs`, 40,000 circular normal groups at each n:

| n | Mean radius and sigma, exact | Simulated | Extreme spread, shotGroups' form | Extreme spread, the panel's form |
|---|---|---|---|---|
| 2 | 92.51 % | 92.60 % | 84.66 % | 95.10 % |
| 3 | 93.70 % | 93.50 % | 89.39 % | 94.96 % |
| 5 | 94.34 % | 94.42 % | 92.31 % | 95.17 % |
| 10 | 94.71 % | 94.78 % | 93.91 % | 94.93 % |
| 20 | 94.86 % | 94.92 % | 94.68 % | 94.94 % |

- **Mean radius and sigma** fall short of 95 percent only because the c4 correction multiplies both endpoints, as shotGroups does. The exact coverage is `IntervalCoverage.RayleighSigma`, and the panel prints it, "94.3% interval" at five shots, never a bare 95.
- **Extreme spread in shotGroups' `getRangeStat` form** scales the observation by the quantiles over the mean, which does not cover the expected spread at its stated level. The panel uses `RangeStatistics.MeanInterval`, the observation times the mean over the quantiles, which does. The M3.1 harness still compares shotGroups' form against shotGroups.
- **Entry 24 cites the bootstrap's 79.5 percent at ten shots.** That is the BCa interval for the Grubbs-Patnaik CEP, which the panel does not show. The two-shot interval covered 92.5 percent; what misled was a three-decimal headline over a factor of twelve. Question 12 records this.

**2. The headline wraps.** Each figure is its value on one line, its labelled interval beneath in smaller type, and every line wraps, so the largest type cannot clip at the panel's edge.

**3. No NaN in the export.**
- **Null with a reason.** Every figure that can be undefined is null beside a sibling, for instance `"aspectRatio": null, "aspectRatioUnavailable": "needs at least 3 shots"`.
- **The same treatment elsewhere:** the centre from the aim without an aim, the dispersion figures below five shots, the ellipse of five shots in a line, the worst shot of coincident shots, and extreme spread beyond the table.
- **The serializer now refuses NaN,** so an undefined figure that escaped would fail the export rather than reach a file.

**4, with entry 26. Rotation is a view transform.**
- **The frame does not change.** Every position stays in the stored pixel frame, the frame M4.1 already decoded in.
- **The tag and the controls.** The view starts turned as the EXIF Orientation tag asks: 3, 6 and 8, with the mirrored values not applied. Rotate left and Rotate right, as buttons and as `[` and `]`, turn it further on any image, tag or no tag.
- **Where the rotation lives.** It is part of the marking state, so undo covers it and a saved marking reopens as it was left. The canvas draws the image and every mark through one map, and nothing is re-encoded or rewritten.
- **One consequence found while building it.** With a single reference length, the target axes are the stored image's, so "right" and "low" in the centre offset and the ellipse angle turn with the view. With a rectangle or a sheet the axes are the sheet's own and do not turn.
- **Entry 26's test**, `RotatingTheViewMovesNoMarkAndTurnsWhereEveryMarkIsDrawn`, runs through the real window. It marks four shots and turns the view, then asserts:
  - every stored and target position is unchanged;
  - the image's corners are drawn where a clockwise quarter turn puts them;
  - every drawn displacement (dx, dy) between marks is now (-dy, dx) at the refitted zoom;
  - a tap on a fifth hole after the turn snaps onto that hole in stored pixels;
  - undo returns every mark to exactly where it was drawn before.
- **A second test** in `ViewRotationTests` marks twelve shots and turns three times: the shot list is the same object and every figure is unchanged.
- **A fault the test found:** the canvas refitted the view from inside its render pass, which Avalonia refuses. It now refits without asking for a redraw.

**The marking file is now `grouplab-marking-2`**, `MarkingFile`. It records:
- `imageFrame` as `stored-pixels` with its definition, `exifOrientation`, and `displayRotationDegrees`;
- the scale's values, section 7: the two tapped points and the length, or the four corners and the rectangle's size, beside the sentence;
- the calibre, and any hole-size flags.

It reads its own files back, which is what "Open marking" does.
- **A file whose frame is not `stored-pixels`** is refused with the reason, and so is a rotation that is not a quarter turn or an unknown format.
- **A version 1 file is migrated.** Version 1 was written from the same stored-pixel decode, so every mark stands. It kept its scale only as a sentence, so the scale must be set again, and the migration says so.
- **A sheet registration is not stored**, and reopening one says to detect again.

**5. Calibre,** an optional group property: the pick list, or typed.
- **What typing reads.** Text is read as a diameter in inches or millimetres, or in hundredths or thousandths of an inch when it is a bare name number such as "22" or "308". The panel says what it read, since .300 Win Mag fires a .308 bullet.
- **Edge to edge beside centre to centre.** With a calibre, extreme spread is printed both ways, edge to edge as centre to centre plus one bullet diameter. Without one it says it needs the calibre.
- **The snap reaches one bullet diameter** once a calibre and a scale are set; otherwise it keeps its on-screen reach.
- **A hole too large for the calibre is ringed in red and listed.**
  - **The test:** its dark region's largest extent is compared against nominal plus 0.132 in. That is three standard deviations above the mean of `docs/SCAN-MEASUREMENTS.md` section 3.5's 260 holes, and nothing it is gated on was used to set it.
  - **What is not measured:** a region that reaches the edge of the search circle is ink or a dark backer, and is not measured.
  - **On synthetic holes:** a single hole at 0.92 of calibre is not flagged, a touching pair marked as one is, at 0.54 in, and a mark on a large printed disc is not.
- **Nothing is gated on it.**

**6 needs nothing. 7 is answered** by entry 25, and the file now records the values.

**Tests:** Core 672, App 2, all passing.

**Named gaps, from entry 25.**
- **Units.** Every figure is in inches, and angular figures and shot distance have no input yet. Closed in M4.3.
- **Printing a target from the window.** There is no print screen; the command line renders the PDFs.
- **Adjust to zero** waits for units, as entry 25 section 3 requires.

### M4.3 Units: entry 25 section 1

**One application-wide setting on three axes,** at the top of the panel:
- **Linear:** in, cm or mm.
- **Angular:** MOA, mil or SMOA.
- **Distance:** yd or m.

Every value the screen shows obeys it:
- the scale entry, a length or a rectangle, typed in the chosen unit;
- the headline and every figure with its interval;
- the centre offset, edge to edge, the calibre, and the hole-size flags;
- the shot distance, which is new, beside the calibre.

**Display only.**
- **Storage is canonical.** Every length is stored in inches at the target and the shot distance in inches too, `docs/STATISTICS.md` section 13, and converted at the edge in `UnitSettings`.
- **The file.** `grouplab-marking-2` carries `shotDistanceInches` and, beside the canonical values, `displayUnits`, so a reader can reproduce the screen. Reading a file never changes the setting.
- **The tests.** `UnitsTests` writes the same marking under both settings and asserts the files are identical once `displayUnits` is removed. The headless test switches from centimetres to inches and asserts the written marking does not change.

**Mil is the milliradian.**
- **Why:** a turret marked in mil is marked in milliradians. The 6400 NATO mil is 1.8 percent different, and entry 25 section 1 is explicit that the wrong angular unit sends a dialled correction elsewhere.
- **In the code:** the choice maps to `AngularUnit.Mrad`, and the NATO mil is not offered.
- **Where it matters most:** adjust to zero, when it is built, will read this setting.

**Angular figures need the distance.** They follow section 12.5's half-angle form: section 12.5's anchor, 1 in at 100 yd as 1.000000 SMOA and 0.954930 MOA, holds through the setting. Without a distance they are absent, and the panel says "Angular figures need the shot distance", per section 13.

**Default and memory.**
- **First run:** the default comes from the system's region: inches, yards and MOA in the United States, Liberia and Myanmar, and centimetres, metres and mil elsewhere.
- **After that:** the choice is remembered in `%APPDATA%\GroupLab\settings.json`.
- **Globalization:** the shell turns invariant globalization off so it can read the region. Every number is still formatted and parsed with the invariant culture.
- **The headless tests** keep their settings in files of their own and never touch the user's.

**Tests:** Core 676, App 3, all passing. **Adjust to zero** remains unbuilt until entry 21 section 5 is specified; the units it needs are now in place.

### M4.4 Printing a target: entry 25 section 2

**The pillar that was missing.** GroupLab's premise is that you print its target, shoot it and photograph it, and until now the window could not print one. "Print a target" opens `src/GroupLab.App/PrintWindow.cs`, a screen over the renderer Phase 0 built. The built-in library ships beside the application.

**Reproduce:**
- `dotnet test tests/GroupLab.Core.Tests --filter PrintNote`
- `dotnet test tests/GroupLab.App.Tests --filter PrintScreen`

**The screen, in entry 25's order.**
1. **Pick a sheet.** All 22 built-ins are listed by what a shooter recognises: the family, the name, the description with its bulls, the distance it was designed around, the paper in millimetres and inches, and how many sheets assemble into it.
   - **Where it comes from:** `TargetLibrary` takes the families from `docs/TARGET-LIBRARY.md` section 1 and the distances from section 2.
   - **The guard:** a test fails if a built-in is not catalogued.
2. **A preview** of the artwork, sheet by sheet for a tiled set, rasterised by `SceneRasterizer`. It draws no text, and the screen says so.
3. **The load block, blank or filled.**
   - **Filled:** the fields come from the definition's field set under their printed captions, with the date prefilled and an optional serial.
   - **Refusals:** what the renderer refuses, a value too wide for its field or an instance code over budget, is shown in its words.
4. **Save a PDF, or print.** Print writes the PDF to a temporary file and hands it to the print command of whatever opens PDFs; where there is none, it opens the PDF instead.
5. **Scale, handled as far as the platform allows.**
   - **The PDF asks for no scaling.** Every PDF GroupLab writes, the command line's included, now carries `/ViewerPreferences << /PrintScaling /None >>` in its catalog.
   - **What cannot be driven is said in plain words.** GroupLab cannot reach the printer driver's own scaling through the system's print command. The screen says so beside the buttons: "choose Actual size or 100%, never Fit...; a sheet printed at 97 percent measures 3 percent small".
   - **The sheet carries the instruction.** "Print at actual size, 100 percent. Never fit to page: a sheet printed at any other scale measures wrong." It runs along the bottom edge, so a sheet that came out wrong carries the evidence. It is on by default and can be turned off.
6. **A multi-page set** is one PDF of every tile in order, each numbered beside its identifier as Phase 0 already printed.

**Where the note goes, measured before it was placed.** Every page of all 22 definitions was scanned for free margin:
- **The identifier** sits 74 to 99 dmm above the bottom edge.
- **Nothing else** comes below 70 dmm across the middle of any page. The lowest are the sighter-row markers of `GL-CF25-LTR`, at 70 dmm.
- **The note** therefore sits on a 45 dmm baseline at 18 dmm, 41 to 58 dmm above the edge.
- **`PrintNoteTests` checks it on every page of every built-in:**
  - at least 8 dmm from every other item;
  - at least 36 dmm above the edge;
  - clear of the side margins;
  - without the note, the page is unchanged item for item.
- **Phase 0's renders** change only in the catalog line.

**Limits, stated.**
- **Clipping:** a printer whose unprintable bottom margin is wider than about 4 mm may clip the note.
- **Viewers:** a viewer may ignore the viewer preference.
- **Nothing measured:** the note is an instruction, not a measurement.

**Tests:** Core 699, App 4, all passing.

**Not built.** The volunteer kit's instruction sheet, which entry 25 names as a natural later use of this screen, and adjust to zero, per entry 25 section 3.

---

## Entries 22 and 27. Intake of donated photographs

`docs/NOTES-FROM-PLANNING.md` entry 22 asks for a single intake gate and a test that fails on a location, an opt-out or missing provenance. Entry 27, the first real submission, adds that most submissions will be unusable and must be triaged with a reason per file, and that digital zoom belongs in the lens grouping key.

**Reproduce:**
- `dotnet test tests/GroupLab.Core.Tests --filter "Publication|ImageMetadataTests"`
- `grouplab intake <submission> <public directory> [--accept <file>]...`

**Where the images go is question 13 and blocks every image commit.** The tool writes to whatever directory it is given, and the test guards `testdata/donated/` in this repository, which is empty.

**What is built.**

| Where | What |
|---|---|
| `src/GroupLab.Core/Publication/ImageScrubber.cs` | Rebuilds a JPEG's EXIF keeping only Make, Model, Orientation, exposure, f-number, ISO, focal length, the 35 mm equivalent, pixel dimensions and digital zoom. It drops XMP, IPTC, every application segment but JFIF and ICC, comments and everything after the end marker, and copies the compressed image data byte for byte. On a PNG it drops every ancillary chunk but colour and resolution |
| `src/GroupLab.Core/Publication/PublicationCheck.cs` | Every place a location can hide: an EXIF GPS block, GPS in XMP or PNG text, and data after the end marker |
| `src/GroupLab.Core/Publication/Intake.cs` | Entry 22 section 2 in order: refuse `DO-NOT-PUBLISH` (file first, then flag), refuse incomplete provenance or a file that does not match its upload hash, triage, scrub, check, then write the files and `provenance.json` with both hashes into a new directory, never over an existing one |
| `src/GroupLab.Cli/IntakeVerb.cs` | The verb, with entry 27's triage: a file on which fewer than four GroupLab markers decode is held with that reason until a person accepts it by name. The stored size, the aspect and the lens group key are recorded beside the verdict |

**Why the scrubber is C# and not `scrub_exif.py`.** The script needs `piexif`, which is not installed here, and nothing may be installed. The C# scrubber follows the script's policy with two differences:
- **It keeps the digital zoom ratio**, for entry 27.
- **It also removes XMP and trailing data.** The script leaves both, and either can carry a location or a date.

**Tests.**
- **Scrubbing:** a synthetic phone JPEG with a GPS block, GPS in XMP, a capture date, a comment and a motion-photo trailer scrubs to nothing a location can hide in. It keeps Make, Model, Orientation, focal length and zoom, and its scan data is identical. A PNG with a GPS eXIf chunk and a location in text scrubs clean.
- **A real committed phone photograph,** `scans/phase0/main1.jpg`, loses its coordinates, keeps every metadata field GroupLab reads, and decodes to identical pixels.
- **Intake:**
  - **Published:** a good submission is published scrubbed with its received and published hashes side by side, and the file triage could not use is held with the reason.
  - **Accepted:** a person can accept a held file by name.
  - **Refused, with nothing written:** an opt-out by file or by flag, missing provenance, an altered file, an unhashed file and an unsafe identifier.
- **The repository guard:**
  - **Public test data:** it fails if any image under `testdata/donated/` carries a location, sits in an opted-out submission, or is not in its provenance record with its published hash.
  - **Everything committed:** it reads every committed image, and fails if one carries GPS other than the 16 question 13 is about. It also fails if one of those is scrubbed and left on the list.

**On real files,** in the scratchpad and not committed:
- **The demo submission:** `grouplab intake` on the GroupLab photograph `main1.jpg` beside the commercial target `300_nm_hand_load.jpg`.
- **The sheet:** 34 markers decoded, so it was published. The tool removed its GPS block, 31 other EXIF fields, the thumbnail, XMP, Samsung's application segments, the multi-picture index and 39,273 bytes after the end marker.
- **The commercial target:** 0 markers decoded, so it was held with that reason.
- **An independent check:** a separate Python EXIF parser read the published file as Make, Model, Orientation and the EXIF pointer, with no GPS block, no XMP and nothing after the end marker.

**A finding, question 13.** 16 of the 78 committed images carry an EXIF GPS block, all Phase 0 phone photographs, 13 of them with a non-zero position. Removing them from history is a rewrite and a force push, which is not mine to run.

**Entry 27 section 2: digital zoom in the lens key.**
- **Where it is read:** `ImageMetadata` now reads `DigitalZoomRatio`. `LensGroupKey` names it, with a missing tag as unknown, never 1.
- **Where it is used:** the joint fit in `SurfaceFrames` groups by it, and the raw measurement files record the key used.
- **The test:** three otherwise identical frames, zoomed 1.64, stating 1 and stating nothing, get three keys.
- **What the first run showed:** the N568 photograph of entries 19 and 20 states a digital zoom of 1.66 beside a 23 mm equivalent on its 2.2 mm lens. So Alan's phone updates the equivalent when it zooms, where entry 27's contributor's S24+ did not. The key now carries both, so neither behaviour can merge two geometries.

**Entry 27 section 1, what triage does not do yet.**
- **The sheet boundary:** it does not look for a rectangular sheet boundary, so a commercial target that is usable for the manual path is held until a person accepts it.
- **Section 3's aspect ratios:** they are recorded per file and change nothing.

**Tests:** Core 708, App 4, all passing.

---

## Entries 29 and 30. The coordinates scrubbed out of history, locally

`docs/NOTES-FROM-PLANNING.md` entry 29 approves replacing the sixteen Phase 0 photographs that carried GPS with scrubbed copies in every commit, before the repository is published. It allows five steps here and reserves the push and the repository deletion for Alan. Entry 30 adds that the commit id map must be tracked, and sets the order in which step 5 finishes.

**Step 1: no recorded digest.**
- **The search:** every tracked text file, for the SHA-256, SHA-1, MD5, CRC32 and git blob id of each of the sixteen files, and for 12-character prefixes of each.
- **The result:** none found.
- **Entry 30's point:** commit ids are the same kind of record, and they are handled in step 5 below.

**Step 2: the bundle.** `../grouplab-prerewrite-2026-09-14.bundle`, outside the working tree, 167 MB. `git bundle verify` reports it okay, with all 11 refs and a complete history.

**Step 3: the scrubbed copies.**
- **Which scrubber:** `grouplab scrub`, through `ImageScrubber`, which entry 29 makes the definition. `scrub_exif.py` now keeps `DigitalZoomRatio` and says the C# version is authoritative.
- **Pixels:** `PublicationTests.EveryCommittedPhotographScrubsToIdenticalPixels` decodes all 31 committed JPEGs, including the sixteen, before and after scrubbing. Every one decodes to identical colour pixels.
- **Planning's check** (entry 30 section 3), with a different EXIF library: no GPS, maker note, date, unique id or software string on any of the sixteen, and `DigitalZoomRatio` present on all of them.
- **Nothing lost before this:** `scans/mounted/` was never put through the Python script, so no zoom tag was lost.

**Step 4: the rewrite.**
- **The tool:** `git filter-branch --index-filter` over the local branches only, since `git filter-repo` is not installed. The index filter replaced each photograph's blob with its scrubbed blob in every commit that held it.
- **Scope:** 59 commits, in 49 seconds.
- **The result:** no branch or tag reaches any of the sixteen original blobs, and all sixteen scrubbed blobs are reachable.
- **Pack size,** each measured on a freshly compacted bare clone: 158.45 MiB before and 156.97 MiB after. The difference is thumbnails, maker notes and phone trailers.
- **Left in place:** `refs/original/*` still points at the unscrubbed history, as `filter-branch` leaves it.

**Step 5: verification, in entry 30's order.**
1. **The commit id map.** 42 commits changed id and 17 did not. They were matched by author time and subject, and every one of the 42 was checked to map onto its mapped parents.
   - **The first map in this session was wrong:** it paired commits by position and counted 40. The corrected map is `docs/REWRITE-HASH-MAP.md`.
   - **Citations:** nine citations of old ids in `NOTES-FROM-PLANNING.md`, this file and `QUESTIONS-FOR-PLANNING.md` now read as the new id with the old one beside it.
   - **The rule:** `CONTRIBUTING.md` now says not to cite a bare commit id.
2. **The Phase 0 photograph gate record.** **Before and after agree exactly.** `grouplab spike photos` was run on the photographs before the rewrite and again on the scrubbed files after it, and `scans/phase0/measurements/photos.json` came out byte-identical, with the console table byte-identical too. The post-rewrite run also matches the record committed before any scrub, apart from the two camera fields this phase added. For example `main_flat1` keeps 136 of 136 corners, residual RMS 0.00270 in and worst scoring bull 0.00343 in; `main1` keeps 90 of 136, 0.00474 / 0.01421 in and 0.01532 in; `ultrawide3` keeps 42 of 128, 0.00610 / 0.06022 in and 0.09123 in; every verdict is unchanged. Scrubbing the metadata moved no measurement, which planning confirmed independently (entry 31 section 1).
3. **The suite:** Core 709 of 709 and App 4 of 4, with the allowlist removed from `PublicationTests`.
4. **`PublicationTests`:** All five pass. `NoCommittedImageCarriesGps` has no allowlist and finds no committed image with a GPS block of any kind; `EveryCommittedPhotographScrubsToIdenticalPixels` scrubs all 31 committed JPEGs to identical decoded pixels; and `PublicTestDataCarriesNoLocationNoOptOutAndFullProvenance` does nothing and says so, because there is no `grouplab-testdata` checkout. One test failed after the rewrite and was rebuilt rather than weakened (entry 31 section 2): it had asserted that the committed `main1.jpg` still carried GPS. `ARealPhonePhotographWithALocationWrittenIntoItScrubsToIdenticalPixels` now copies that real photograph, writes a GPS block with its own coordinates, GPS in XMP, a comment and a trailer into the copy, and requires the scrubber to remove all four, leave every camera field including the digital zoom unchanged, and leave the decoded pixels identical. It records what it can no longer cover: the maker notes and thumbnails the rewrite removed from the repository.

**A pull nearly undid it.** After the rewrite, a `git pull --rebase` was started in this clone against the stale remote, and paused on conflicts in four of the photographs. Continuing it would have rebuilt the scrubbed history on top of the unscrubbed one. It was stopped and aborted, and no original blob became reachable. Until the fresh repository exists, nobody pulls, pushes or fetches in this clone (entry 30 section 1).

**Not done here, and why.**
- **Alan's steps:** the push of the rewritten history and the deletion of the old GitHub repository are Alan's (entry 29 section 4 and entry 31 section 4). The leftover `worktree-agent-a5825dfa6aad44e1d` branch has been deleted here, per entry 31; its history is in the bundle.
- **The backups:** `refs/original/*` must not be pushed. An ordinary `git push origin main phase-1` will not carry it.

**Commands the permission classifier refused,** for Alan to allow if he wants them (entry 30 section 7):
- `git ls-remote origin`
- `git rev-list --count <branch>` with `git diff --name-only <old> <new>`, in a loop over the branches, refused twice
- `rm -rf <scratchpad>/before.git && git clone --bare <bundle> <scratchpad>/before.git`, refused once. The same clone without the `rm -rf` was allowed.

---

## Entries 39 and 40. The print crash, unassigned shots, the snap onto artwork, and the editor

`docs/NOTES-FROM-PLANNING.md` entries 39 and 40, from Alan's second session with the window, in entry 39 section 7's order. Each part is its own commit.

**Entry 39 section 2: the print screen crash.**
- **The cause:** choosing a second sheet that has a load block, or switching a load block to Filled a second time, threw "The control TextBox already has a visual parent". The serial box and the field boxes outlive the rows that hold them, and Avalonia refuses a control with two parents.
- **The fix:** each is taken out of its previous row before it is shown in a new one.
- **The test:** `EverySheetCanBeSelectedInTurnToggledPagedAndSavedWithoutACrash` reproduced the crash before the fix, as entry 39 asked. It is also the deliberate attempt to break the screen:
  - every built-in sheet is selected in turn, then in reverse;
  - its load block is switched between blank and filled more than once;
  - its pages are turned;
  - each sheet is saved, with a PDF or a stated reason.

**Entry 39 section 1: hand-placed shots and the composite group.**
- **Alan's exact sequence is not reconstructable from the entry:**
  - detection does load the bulls, and the canvas already gave a shot tapped after it its nearest bull;
  - but a shot marked before detection was kept with no bull;
  - opening the image again clears the bulls.
- **So every route is closed rather than the one guessed at:**
  - `MarkingSession` assigns a hand-placed shot to its nearest bull, measured on the target plane when a scale is set;
  - detection assigns any hand-placed shot marked before it;
  - a moved shot follows its nearest bull, unless the user had assigned it elsewhere or unassigned it.
- **The second change, which entry 39 says matters more:** on a sheet of more than one scoring bull, `GroupAnalysis` withholds every figure while any counted shot is unassigned, and puts the reason where the figures would be. For example: "1 of 6 shots is not assigned to a bull, so it would be measured from the point of aim and the rest from their bulls, and no figure means anything. Assign every shot to its bull."
- **A sheet of one scoring bull is not affected,** because its single aim is the right origin.
- **The general rule** is now written in `CONTRIBUTING.md`: a report never prints a number it cannot mean.

**Entry 40 section 1: the snap onto printed artwork.**
- **What changed in the detector:** it now returns its expected artwork after alignment, in image pixels, and the window keeps it after "Detect on a GroupLab sheet".
- **The snap:** a tap whose dark surroundings are mostly printed ink is placed where it was tapped, and the status line says "placed where tapped: the dark area under the tap is the printed target, not a hole". Otherwise, only dark pixels the artwork calls paper pull the snap.
- **The size message** now names both explanations: "Two holes marked as one, or a tap that snapped to the printed target rather than a hole." When the region is mostly artwork it names only the printed target, because that is then known.
- **The limit:** the artwork exists only after detection in the current window. A reopened marking, or an image scaled by hand, has none, so there the message names both explanations and the snap is the old one.

**Entry 39 sections 3 to 5: the editor.**
- **The scale line:**
  - it is drawn as it is made, out to the pointer, with a circle at every end;
  - after the last tap it stays drawn while it waits for its size;
  - either end can be dragged onto its mark before the length is used, and the dragged end is the one used;
  - after it is used, an end of the reference in use can be dragged with the Select tool, as one step undo reverses;
  - a rectangle's corners behave the same way.
- **Placing an impact:** press, drag to where it belongs, and let go. A dashed ring shows where it will snap while it is dragged, and the shot is set where it is let go.
  - **Entry 39 quoted Alan as click, drag, then click to set.** One press-drag-release gesture was chosen, because it is the same gesture with a finger and a single tap still places a shot.
  - **If Alan wants the two-click form,** it is a small change.
- **The shot list:**
  - every shot is a row with its number as drawn, its bull, and whether it is excluded or not a shot;
  - a row selects its shot on the image;
  - each row carries an Exclude or Restore button, using the reason chosen under Selected shot, which starts at called flyer.
- **The marks:**
  - nothing is drawn in white;
  - the selection ring is pink;
  - every stroke, ring and label is drawn over a dark outline.

  **These have not been looked at by eye,** because the headless tests render nothing this session can see. Alan's next session is the check.

**Entry 39 section 6.** The README's wording about its concept screens is left as it is until someone has used the new editor, so that the page is changed to describe an application a person has seen.

**Worth knowing:**
- **A running window:** `GroupLab.App` was running throughout this work, and held its build folder. The app tests were built into a separate folder.
- **Its build is out of date:** the window that is open is the build from before these changes. It needs restarting to show them.

**Tests:** Core 729 passing, App 10 passing, none skipped.

---

## Entries 41 and 45. The diagnostic log, crash records, and report packages

`docs/NOTES-FROM-PLANNING.md` entries 41 and 45, built in entry 41 section 8's order, one commit a step. Everything is in `src/GroupLab.App/Diagnostics`. `docs/CRASH-REPORTING.md` is the contract and the privacy statement.

**Why.** The application logged nothing: `Main` ended in Avalonia's own trace logging, which went nowhere. It had no global exception handler, and every `catch` either set a status line or carried on. When the print screen crashed, nothing could say why.

**Step 1, the log (sections 3 and 4).**
- **Files:** one plain text file per run, `grouplab-YYYYMMDD-HHmmss-pid.log`, with UTC timestamps to the millisecond and `key=value` fields.
- **Rotation:** at startup, to the newest twenty files or 20 MB.
- **The writer:** a background thread behind a bounded queue. When the queue is full it drops DEBUG first and counts what it dropped. ERROR is flushed at once, and everything else within two seconds.
- **Failure:** silent. If the directory cannot be used, logging is off, and the Diagnostics panel says why.
- **Where:** `out/logs` for a Debug build run from the repository, the platform's directory otherwise, and `GROUPLAB_LOG_DIR` over both. The first line names the directory in symbols such as `%LOCALAPPDATA%`, because the real path begins with the user's name.
- **What is recorded:**
  - `app.start` with the environment, and `app.exit`;
  - every dialog and its result, and every file open and save;
  - each detection run, with every stage record at DEBUG in its console form;
  - a line for each catch that used to swallow its error.
- **A fault found on the way:** opening a marking file that no longer exists threw instead of saying so, and now says so.

**Section 2's rule,** written as code rather than care:
- **A file** is its name and a hash of its full path salted per run, never its directory.
- **Image facts** come only through `ImageFacts`, a whitelist over the metadata GroupLab already reads. That metadata holds no location, timestamp, maker note or free text.
- **Every value, exception message and stack** has anything shaped like a path replaced.
- **The tests:**
  - they build a JPEG carrying a GPS block, a maker note, an `Artist` and an `ImageDescription`;
  - they open it through the window from a directory named for a person;
  - they check that no coordinate, name, maker note, directory or drive letter reaches the log, while the file's name and its whitelisted make do.

**Step 2, the crash record (section 5 and entry 45 section 2).**
- **Handlers:** three, installed before the window exists: the process's unhandled exceptions, unobserved tasks, and the dispatcher's. At the dispatcher, a click handler's exception is marked handled and the window carries on.
- **On a crash:** an ERROR line with the whole chain, flushed. Then `crash-YYYYMMDD-HHmmss-pid.json` in schema 1, carrying:
  - the environment;
  - the last action, the name of the last event logged;
  - the exceptions, outermost first, with messages and stacks scrubbed of paths;
  - the stage records of a detection in flight.
- **The next launch** offers every crash not yet dealt with, in a banner above the settings.
- **The test:** it throws from a real click handler through the dispatcher, and reads the record back.

**Step 3, the package (section 6 and entry 45 section 1).**
- **Contents:** one flat zip holding the crash record, the log of the run that crashed and the run before, `environment.txt`, and the user's description and contact only when typed.
- **The whitelist:** every entry must match the receiver's five patterns, which a test holds equal to the receiver's list. The builder takes no arbitrary file, and a name not on the list is refused.
- **The cap:** over 2 MB the previous log goes first. A package still over it is saved, not sent.
- **The dialog:**
  - it states in one sentence what the report contains and does not;
  - it lists every entry before anything is written;
  - it offers Save only, defaulting to the desktop, then Show me the file.
- **Where it is reached:** the crash banner has Make a report, and the toolbar Report a problem.

**Step 4, the upload (section 7 and entry 45 sections 3 and 4).**
- **The request:** one HTTPS `POST` of `multipart/form-data`, with the zip in `report` and the version in `version`.
- **Attempts:** one, with a 30 second timeout, and no retry.
- **The reply:** a success shows the server's reference, and a failure shows the server's `error` verbatim. Anything else keeps the zip and says where it is.
- **The address:** `crashReportUrl` in the settings file is empty by default, and while it is empty there is no Send button, so no copy of GroupLab posts anywhere unless configured to.
- **The pull script:** planning's version of `scripts/Get-TargetSubmissions.ps1`, with `-CrashReports`, was reviewed and committed. It adds a mode, a verify branch for a single zip and a summary of what crashed, and it holds no secret.

**Not done as entry 41 section 8 asked.** Its last point was to reproduce the entry 39 print crash with logging in place, before fixing it, and paste the lines. The print crash was fixed in an earlier commit, before logging existed. So `CrashTests` throws from a click handler through the dispatcher instead, the same path the print crash took, and checks the crash record and the log line it leaves.

**Tests:** App 31 passing, none skipped. Core is unchanged at 729.

---

## Entries 49 and 50. What the gate record compares, a README that told visitors something false, and when entry 43 starts

`docs/NOTES-FROM-PLANNING.md` entries 49 and 50. Entry 49 sections 2 and 5, the marker sort and the edge fit's sensitivity, are measured next and reported in their own section.

**Entry 49 section 1: the gate record reproduces when every verdict and every printed table is identical.**
- **What is gated:** each platform's eight console tables, compared line for line with the Windows tables now committed in `scans/phase0/measurements/tables`. The threshold table's detection time column is left out, as it differs between any two runs. Those Windows tables are the ones this machine and the Windows runner both print.
- **What is reported, not gated:** the raw records, still compared byte for byte, with the count of differing values and the largest difference under each field name in the run summary.
- **Expected under the rule:** Windows and Linux pass, and macOS fails, since two of its study tables differ in print.
- **Why this is not a gate written after the results:** it sets no tolerance and names no magnitude. It says which artefact carries the claim, which is the one a person reads.
- **What was not done:** no per-platform reference records, so the repository asserts one truth.

**Entry 49 section 3:** `tools/scan_analysis/scrub_exif.py` keeps `LensModel`, with its docstring line, as planning wrote them.

**Entry 49 section 4 and entry 50 section 1: the README.**
- **Building** now says GroupLab builds and its tests pass on Windows, Linux and macOS, that every push runs the suite on all three, and that the application is offered for Windows until the gate record reproduces elsewhere. The paragraph it replaces said only Windows builds.
- **Concept screens** says the application carries the screens' palette, type and marks, and not their layout.
- **Status** adds the self-identifying sheet, the `analyze` command and the diagnostics, and no longer lists the assignment editor as missing.
- **Planned** no longer says "once CI exists".
- **Guarded:** the platforms sit between `<!--platforms-->` markers, and `ReadmeTests` requires them to equal the `os` matrix of `.github/workflows/ci.yml`.
- **The rule:** `CONTRIBUTING.md` now says a commit that makes a statement in the README false updates it in the same commit, with the sections most likely to go stale.

**Entry 50 section 2: when entry 43, the analysis screen, starts.** All three of these, and not before:
1. **The gate record workflow is green on Windows, Linux and macOS,** under the rule above.
2. **The edge fit's sensitivity is measured and reported:** the one edge point of thirty that moved a bull 0.30 dmm on `telephoto3.jpg` (entry 49 section 5).
3. **Alan's range session has been through the pipeline.**

**Tests:** Core 758 passing, none skipped; `ReadmeTests` has a fifth test, the platform guard.

---

## Entries 65 to 69. The renormalisation measured, the design target found where it already was, and the editor's real size

`docs/NOTES-FROM-PLANNING.md` entries 65 to 69, in the order Alan set: entry 67 section 3, the concept image, entry 69 section 3, and entry 65 section 4 step 2. Entry 68 shelves entry 65 section 5, entry 67 section 4 and entry 65 section 4 step 3, and none of them was touched. The CI matrix stays on all three platforms and the macOS gate record stays open.

### Entry 67 section 3: `git add --renormalize .`

**On this machine it moves nothing:** no file staged. Entry 66 is right that this answer depends on which git asks, so the question `* text=auto` exists to answer was measured too, with `core.autocrlf` switched off for the one command, and the index reset afterwards:

| | Files the renormalisation stages |
|---|---|
| This machine, `core.autocrlf` true | 0 |
| `core.autocrlf` false, `.gitattributes` as committed | **109 files, 21,634 lines each way** |
| `core.autocrlf` false, with `* text=auto` added | 1, the `.gitattributes` line itself |

- **Entry 64 described a real state,** the one any git without `core.autocrlf` sees. Its figures reproduce exactly.
- **Entry 67's prediction holds:** with `* text=auto` the renormalisation moves no content, so the commit that adopts it would be that one line.
- **Nothing was committed.** Adopting it remains Alan's decision, as entry 66 says.

### The concept image: already in the repository

**The design target has been committed since the concept screens were added,** at `docs/figures/screens/assignment-editor.png`, and the README's concept table links it. Entry 69 section 1 searched for `*concept*`, and this file is named for its screen.

**The new file is the same picture.** Both decode to the same 1440 by 900 pixels (SHA-256 of the pixels `249c0ffb...`). The new copy is 480,726 bytes against 188,998, RGBA against a palette, and carries a C2PA content-credential chunk naming Claude and Anthropic as the software that provided it. **It was not committed:** a second copy adds nothing but size and a provenance block, and it is left untracked at `docs/figures/concept-assignment-editor.png` for Alan to delete or keep.

### Entry 69 section 3, read against the source

| Concept element | Entry 69 said | What the source has |
|---|---|---|
| One-to-one matching | `ShotAssignment`, `AssignmentMethod.OneToOne` | **Confirmed.** Hungarian matching, with nearest-bull when shots outnumber bulls |
| The contested card's numbers | `AssignedShot` | **Computed, then dropped.** `AutomaticMarking` computes them for every shot and writes the ambiguous ones into the `S9.assign` trace, then hands the session only a position and a bull. `MarkedShot` has no field for them, so the application cannot show the card or the review counter today |
| The card's prose | `ShotAssignmentResult.Reason` | **Corrected.** `Reason` is one line about the method, "as many shots as bulls". The card's sentence, including the distance of the shot already holding bull 4, has to be composed from the per-shot figures and the other shots' assignments |
| Provenance pills | `ShotProvenance` | **Confirmed,** automatic, corrected and manual, with a moved or reassigned detection becoming corrected |
| Not a shot | `MarkedShot.NotAShot` | **Confirmed,** and setting it on a detection marks that detection corrected |
| Click a hole, then a bull | `MarkingCanvas` | **Confirmed,** in the canvas's press handler, citing section 13 |
| Alert rings | `MarkingCanvas.FlaggedShots` | **Corrected.** Those are oversized holes from the calibre check, entry 24 section 5 and entry 46 section 1, not contested assignments. Nothing rings a contested one |
| Counting by provenance | `GroupAnalysis` | **Confirmed** |

**What exists and the table did not list:** undo and redo in the session, section 13's snap to the local centroid (`Snapping`, which quotes the sentence the concept's hint quotes), and the calibre size check, which reaches the window live and is the source the concept's "diameter against the median" queue row needs.

**What the "did not find" list should add, stated as how it was looked for:**
- **Assignment is not rerun after an edit.** A placed shot goes to its nearest bull and a moved one keeps its bull, so one-to-one matching happens once, at detection. A contested card would go stale after the first correction unless the matching is rerun, or the card says it was computed before the edit.
- **Assignment uses the declared bull positions,** while the canvas draws the located ones. On a well-registered sheet the difference is small; it is still a difference to state wherever a distance is printed.
- **No score.** A detected hole carries its diameter and solidity; nothing named score or confidence was found in `Core/Detection` or `Core/Marking`.
- **Rejected candidates reach only the trace.** The queue's "added" and "kept out" rows need them, and `AutomaticResult` does not carry them.

**So the job is presentation plus one plumbing change and one decision.** The plumbing is carrying the per-shot assignment detail and the rejected candidates from `AutomaticMarking` into the session. The decision is whether matching reruns as the person edits. Both come before any layout, and neither is started, since Alan's report on using the application comes first.

### Entry 65 section 4 step 2: the print wording

Off Windows, the print window said "This system has no print command GroupLab can call". Entry 67 established that a stock Ubuntu 24.04 desktop has `lp`, so the sentence read as a false statement about the person's computer. It now reads **"GroupLab cannot send this to a printer itself here, so the PDF is open in your viewer."** It names no platform, because the same branch runs on macOS.

**Noted and not changed:** the print window's status line carries the alert style for every message, so the confirmation that a PDF was sent or saved is drawn in red too. That is a styling question, held with the rest of the chrome until Alan has used the application.

**Tests:** App 36 passing, none skipped. No Core file changed.

---

## Entries 71 and 72. The first consented mounted photographs, registered, and the scan against its prediction

`docs/NOTES-FROM-PLANNING.md` entries 71 and 72, submission `2026-09-16_3a493942`, sheet `GL-20J3-Y141-0BN3-EYME`.

### Intake

**As submitted, intake refuses the whole submission:** `Scan_20260916.png` is in the folder and not in `meta.json`, so nothing shows it was consented to. That is entry 71 section 6 item 2's open decision, and it was not taken here: the submission folder is untouched.

**Run on a scratch copy without the scan**, with opt-outs read from the real submissions and output to a scratch directory rather than `grouplab-testdata`:
- **`IMG_5820` and `IMG_5822` published,** each with GPS, 35 other EXIF fields, the thumbnail, the multi-picture index, APP2, APP10 and a trailer removed.
- **The other four held by triage:** 0 markers decoded on `IMG_5819`, `IMG_5821` and `IMG_5823`, 3 on `IMG_5824`. They stay unpublished and unscrubbed until a person who has looked accepts them by name.
- **Triage is wrong about all four,** as the registration below shows: every one registers. Triage tries three fixed guesses of the marker size, and the measurement's own first pass finds it. That is a defect in the intake gate, recorded here and not fixed in this pass.

### Registration, all six frames

`grouplab measure <frame> targets/GL-CF25-LTR.gltd.json --model homography|surface`, read from the originals on this machine. The surface model is the generalised cylinder through a camera with radial distortion.

| Frame | Model | Markers | Corners kept | Scoring bulls located | Worst scoring bull (in) | Scoring bulls over 0.005 in | Worst sighter (in) |
|---|---|---|---|---|---|---|---|
| `IMG_5819` | homography (homography) | 38 of 38 | 114 of 152 | 25 of 25 | 0.01101 at 8 | 16 of 25 | 0.00624 at S1 |
| `IMG_5819` | surface (generalised cylinder through a camera wi) | 38 of 38 | 134 of 152 | 25 of 25 | 0.00587 at 20 | 3 of 25 | 0.01332 at S2 |
| `IMG_5820` | homography (homography) | 38 of 38 | 97 of 152 | 25 of 25 | 0.02587 at 5 | 16 of 25 | 0.01250 at S2 |
| `IMG_5820` | surface (generalised cylinder through a camera wi) | 38 of 38 | 136 of 152 | 25 of 25 | 0.00531 at 1 | 2 of 25 | 0.01354 at S2 |
| `IMG_5821` | homography (homography) | 36 of 38 | 79 of 144 | 25 of 25 | 0.03717 at 21 | 20 of 25 | 0.04737 at S1 |
| `IMG_5821` | surface (generalised cylinder through a camera wi) | 36 of 38 | 115 of 144 | 25 of 25 | 0.01283 at 21 | 10 of 25 | 0.01451 at S2 |
| `IMG_5822` | homography (homography) | 34 of 38 | 68 of 136 | 25 of 25 | 0.03451 at 21 | 19 of 25 | 0.04730 at S1 |
| `IMG_5822` | surface (generalised cylinder through a camera wi) | 34 of 38 | 115 of 136 | 25 of 25 | 0.02338 at 25 | 8 of 25 | 0.02738 at S3 |
| `IMG_5823` | homography (homography) | 36 of 38 | 64 of 144 | 25 of 25 | 0.05864 at 21 | 17 of 25 | 0.08783 at S3 |
| `IMG_5823` | surface (generalised cylinder through a camera wi) | 36 of 38 | 113 of 144 | 25 of 25 | 0.02886 at 21 | 11 of 25 | 0.01503 at S2 |
| `IMG_5824` | homography (homography) | 33 of 38 | 66 of 132 | 25 of 25 | 0.04800 at 25 | 16 of 25 | 0.07076 at S3 |
| `IMG_5824` | surface (generalised cylinder through a camera wi) | 33 of 38 | 109 of 132 | 25 of 25 | 0.04607 at 21 | 11 of 25 | 0.01900 at S1 |

**Holes do not explain the worst bulls.** Bulls 1 to 10 carry the shots, and under the surface model the worst of them is 0.00825 in on any frame. On `IMG_5821` to `IMG_5824` the worst bull under both models is a clean one, 21 or 25 at the bottom corners, by a wide margin. On `IMG_5819` and `IMG_5820` the worst holed and worst clean bulls are within 0.005 in of each other, the holed one slightly worse under the homography. So the large errors are registration at the lower corners, where the sheet is taped to the bowed cardboard, not hole damage.

**Against the Phase 1 record.**
- **The flat homography fails every frame,** worst scoring bull 0.011 to 0.059 in, inside the range the pinned sheet gave, 0.018 to 0.113 in.
- **The surface model halves the worst bull or better** on four of six frames, and on `IMG_5820` and `IMG_5819` reaches **0.00531 and 0.00587 in**, with 2 and 3 scoring bulls over the gate. That is closer to 0.005 in than any mounted frame in this document, and still not inside it.
- **So the mounted gate still has no passing frame,** now on a second mounting by a different person with a different fixing. Entry 52 section 3 applies to every figure here: each is one registration, and on the pinned sheet a frame's worst bull varied by up to threefold with nothing but the order of its corners.
- **The best two are `IMG_5820`, which fills the frame, and `IMG_5819`, one of the wide frames;** the oblique `IMG_5822` to `IMG_5824` do worst. Entry 71's concern that the wide frames would fail on marker size did not happen: all three decoded 36 to 38 of 38 markers.

### The scan against entry 72's prediction

After entry 73 section 1, `grouplab analyze Scan_20260916.png` finds 15 holes: 10 in the scoring pool, on bulls 1 to 10, and 5 in the sighter row for 3 sighter bulls.

- **Observed misassignment: 0 of 10 scoring shots.** One-to-one matching gives every scoring shot its nearest bull, none is flagged, and the ten occupy bulls 1 to 10 once each, so no shot is nearest a bull other than the one it was matched to. **Entry 72 predicted 2 to 3.**
- **The prediction assumed the old ratio, and the ratio moved.** Entry 56's model was applied at spacing over sigma 3.89. This sheet's sigma after the fix is 0.274 in, which puts the 1.5 in spacing at **5.5 sigma**, where the same model expects about 1.5 percent per shot, about 0.15 of a shot in ten. Against that, 0 is what the model predicts.
- **Two caveats.** The sigma comes from the same shots, and a shot that had landed nearest the wrong bull would make it smaller, so the agreement is weaker than it looks. And "own bull" can only mean the bull matching assigns, since the firing order is not known.
- **Before the fix** the sigma was 0.492 in, inflated by the two sighter holes, which puts the ratio at 3.05 and would have predicted about one shot in four misassigned.
- **The sighter row holds five holes for three bulls**, which the counts rule handles by nearest bull, all flagged. Whether two extra sighter shots were fired, or some of these are not shots, is for the shooter.

**Tests:** Core 771 passing, App 39 passing, none skipped.

---

## Entries 75 and 76. One numbering system, five decisions, the aspect's null, and two predictions tested

`docs/NOTES-FROM-PLANNING.md` entries 75 and 76, in the order Alan set:
- entry 75;
- entry 76 section 4;
- then sections 1, 3 and 2.

### Entry 75: a shot is named by its bull, everywhere

**The rule as implemented.** `ShotLabels.For` gives every shot the label of the bull it is assigned to, in the sheet's order: scoring bulls, then sighters, then shots with no bull, then shots marked not a shot.
- **A bull holding two or more shots** labels them `7a`, `7b` by position on the image, top to bottom then left to right.
- **A shot with no bull** is labelled `unassigned`, in the alert colour, so it can never be read as a bull number.
- **A shot marked not a shot** is labelled `not a shot`, in the alert colour as well.
- **A plain group with no bulls** has no labels at all, and its list is ordered by position.

**Where it applies.**
- **The canvas.** It draws no per-detection index anywhere.
- **The SHOTS list.** It is ordered and named by label. An unassigned shot or one that is not a shot reads "unassigned, at x, y" so two of them can be told apart.
- **`grouplab analyze`'s shot column** and the marking file, which gains a `label` field.

The shot's internal identity stays internal. The detection order is no longer shown anywhere.

### Entry 76 section 4: the five decisions

**Scan consent: not published.** `Scan_20260916.png` stays local, and the submission folder is untouched.

**The four held photographs: the check is fixed, and all six now pass on merit.**
- **The fault.** Intake's triage tried three guesses of a marker's side: a 20th, a 40th and an 80th of the long side. A sheet photographed from where a person stands has markers at a 110th to a 175th.
- **The fix.** `MarkerTriage` now tries down to a 320th. Wherever a guess decodes anything, it detects again at the median side it found, which is the bootstrap the measurement itself uses.
- **The test.** `MarkerTriageTests` places a rendered `GL-CF25-LTR` at 228 DPI in a 6000 by 4500 frame, and requires all but two printed markers counted.
- **The re-run.** Intake ran on a scratch copy without the scan, with output to a scratch directory and nothing written to `grouplab-testdata`.
  - All six frames published, with 38, 38, 36, 35, 37 and 33 markers counted.
  - `PublicationTests` passes over the published copies, 8 of 8, with stand-in checkout files.

**Rings: the measured diameter, always, with the calibre beside it.**
- **What is recorded.** A detected shot carries the diameter the detector measured, through `DetectedShot` into `MarkedShot.MeasuredDiameterInches`, and the marking file saves it.
- **How it is drawn.** The canvas draws the ring at that diameter in sheet units. Once a calibre is set, the calibre's hole is drawn beside it, faint and dashed.
- **When there is no measurement.** A shot placed by hand has none, and moving a detected shot drops it, because the measurement described the detector's point and not the person's. Such a shot keeps the calibre ring.

**Detect on open: yes, on a recognised sheet only.**
- **On a GroupLab sheet,** opening an image starts detection. The status bar shows an indeterminate progress bar and a Cancel button.
- **On anything else,** it does nothing and says so: "Nothing detected: this image is not a GroupLab sheet GroupLab recognises". The Detect button still runs detection by hand, with the definition picker as before.
- **What cancelling does.** It checks at each resolution of identification and between detection's stages, then says "Detection cancelled". **Cancellation takes effect only at those checkpoints**, so a long stage runs to its end before it stops.

**The printed name: checked first, as section 4 asked, and not changed, because the check failed.**
- **Where the caption sits.** The identifier caption is inside the analysed region.
- **How it is excluded today.** The expected artwork is drawn by `SceneRasterizer`, which draws no text. The caption is kept out of the difference only by an exclusion box sized to the caption's own text.
- **What the change needed.** Putting the name in front of the identifier widens that text, so the box had to widen too. It was widened to cover both the new caption and the identifier alone, which is what every sheet printed so far carries.
- **What the change did.** On Alan's scan, a sheet printed before the change, the wider box covers blank paper beside the old caption. **It hid a real sighter hole**, S1's shot at page (3.070, 10.573) in.
- **The counts.**
  - 15 holes before the change.
  - 13 holes with it.
  - 15 holes again once it was reverted.
- **So the change breaks sheets already printed.** It was reverted in full and is not in the repository. Question 16 asks planning where the name can go.

**A false positive the check turned up, which answers entry 73 section 4.** The scan's shot at page (2.909, 10.784) in is not a hole.
- **Where it sits.** On the printed sentence "Print at actual size, 100 percent", whose baseline is 45 dmm above the bottom edge.
- **Why it was detected.** That sentence is not in any exclusion zone, so its ink reads as a difference.
- **What would fix it.** A box around that sentence alone, its glyph band plus 10 dmm. It would remove this detection without reaching the hole at 10.573 in. **It is not implemented**, because it changes what the detector reads on every printed sheet. It is part of question 16.

### Entry 76 section 1: the aspect is printed beside what a circular group gives

**The null is exact rather than simulated.** `CircularAspect` integrates the density of the inverse aspect for n circular shots, `u^(n-3) (1 - u²) / (1 + u²)^(n-1)`, derived in `docs/STATISTICS.md` section 7.
- **Against entry 76's simulation.** It reproduces the simulated quantiles at 10 and 12 shots within their noise.
- **Against a seeded simulation.** One at 3, 5 and 25 shots agrees as well.
- **Entry 76's worked case.** An aspect of 2.818 from ten shots is exceeded by circular groups with probability 0.0249.

**The report** carries `circularMedianAspect` and `circularAspectExceedance`, each with its reason when it is null.

**The panel** reads, for example, "Error ellipse aspect 2.82, major axis at 27 degrees; 10 circular shots give about 1.5 and exceed 2.82 one time in forty (STATISTICS.md section 7)". Section 7 now carries the table at 5, 10, 12 and 25 shots.

### Entry 76 section 3: the per-bull residual maps

Each frame's 25 scoring bulls are shown under both models. Error is in thousandths of an inch, with the paper's top row first.

| Frame | Homography, rows top to bottom | Surface, rows top to bottom |
|---|---|---|
| `IMG_5819` | 5.1 5.5 9.2 5.6 10.9 / 5.9 10.9 11.0 9.1 3.8 / 6.0 8.7 7.7 7.4 1.5 / 2.5 3.5 1.9 4.2 1.7 / 8.5 5.6 4.1 2.8 8.8 | 2.7 1.3 2.8 2.9 2.6 / 1.8 2.6 1.9 0.7 4.7 / 4.6 3.8 2.5 1.6 5.6 / 1.9 2.6 2.3 2.4 5.9 / 5.9 4.0 3.9 0.4 5.0 |
| `IMG_5820` | 8.9 8.0 7.5 14.1 25.9 / 2.8 7.3 5.4 5.0 22.6 / 6.0 9.4 6.1 5.4 13.3 / 4.0 5.0 2.4 4.6 11.9 / 4.9 4.1 3.4 2.4 21.0 | 5.3 3.3 1.4 2.3 1.8 / 2.6 2.2 1.5 2.0 4.5 / 2.7 3.0 2.6 1.6 3.8 / 1.6 3.3 3.8 1.1 3.4 / 5.2 2.9 4.1 1.4 3.3 |
| `IMG_5821` | 5.7 5.9 14.8 24.7 16.5 / 1.4 5.6 8.9 16.3 15.8 / 4.8 7.0 5.5 7.3 7.9 / 8.9 2.0 0.4 5.6 4.3 / 37.2 16.7 8.4 6.7 5.3 | 2.4 2.3 4.4 6.6 5.6 / 5.6 4.0 2.6 5.2 5.9 / 1.1 1.8 4.3 1.1 0.8 / 2.4 2.4 5.3 4.0 5.6 / 12.8 1.8 5.7 3.3 11.1 |
| `IMG_5822` | 1.5 5.7 6.4 8.9 12.0 / 6.3 13.9 11.0 5.5 7.0 / 12.4 15.8 11.9 3.6 8.0 / 3.1 6.5 5.0 1.0 8.8 / 34.5 13.6 5.0 0.6 8.4 | 3.0 2.7 3.5 5.2 8.0 / 5.0 3.4 1.5 3.3 3.5 / 1.4 1.5 4.6 1.3 2.8 / 2.2 2.7 4.5 5.8 12.5 / 8.0 2.3 2.5 8.4 23.4 |
| `IMG_5823` | 14.1 3.1 7.5 11.6 6.2 / 9.5 0.9 5.2 3.9 0.2 / 1.8 3.4 7.7 3.3 3.4 / 13.5 9.5 5.3 10.1 15.5 / 58.6 34.9 26.7 30.3 41.5 | 6.2 5.9 1.7 7.7 5.0 / 7.6 4.8 3.9 3.5 4.3 / 3.4 6.7 7.7 1.2 3.9 / 5.5 2.7 6.5 0.6 2.6 / 28.9 2.7 9.0 6.9 0.8 |
| `IMG_5824` | 12.4 1.4 3.5 2.5 10.9 / 11.7 1.9 8.3 7.5 12.0 / 4.3 7.0 13.3 9.3 9.2 / 3.5 1.0 4.9 3.4 18.2 / 34.4 14.0 10.9 22.1 48.0 | 6.8 8.3 2.3 5.5 1.4 / 5.7 6.1 6.0 1.4 3.6 / 2.3 4.3 7.1 0.6 3.3 / 17.4 6.2 2.7 2.0 2.8 / 46.1 14.6 2.5 4.5 1.6 |

**Under the surface model, the large errors are all in the bottom row.** Every scoring bull over 0.010 in is bull 21 or 25, the paper's bottom corners, or a neighbour of one.

**The camera is where the prediction has to be read, and it was not placed to separate top from bottom.**
- **How distance was measured.** Pixels per dmm, taken from the fitted marker mapping at each corner bull, show how far each corner was from the camera.
- **The finding.** In every frame the paper's top was nearer than its bottom, by a scale ratio of 1.07 to 1.19. So the bottom corners were always the far ones.
- **What that means.** "The worst region stays at the bottom" and "the worst region is the part farthest from the camera" predict the same thing in all six frames, and top against bottom cannot separate them.

**Left against right can separate them, because the camera swung.** The frames differ in which bottom corner was farther from the camera.

| Frame | Pixels per dmm at 21 and 25 | Farther bottom corner | Markers not decoded | Surface error at 21 and 25 (in) |
|---|---|---|---|---|
| `IMG_5819` | 0.923, 0.933 | neither | none | 0.0059, 0.0050 |
| `IMG_5820` | 1.275, 1.281 | neither | none | 0.0052, 0.0033 |
| `IMG_5821` | 0.909, 0.744 | 25 | two, bottom right | 0.0128, 0.0111 |
| `IMG_5822` | 1.254, 0.950 | 25 | four, bottom right | 0.0080, 0.0234 |
| `IMG_5823` | 0.632, 0.809 | 21 | two, bottom left | 0.0289, 0.0008 |
| `IMG_5824` | 0.812, 1.093 | 21 | five, bottom left | 0.0461, 0.0016 |

**The results, frame by frame.**
- **The two square-on frames:** both corners are small.
- **Three of the four oblique frames:** the bad corner is the far one, by a factor of 3 to 36.
- **`IMG_5821`:** the corner farther from the camera is the better of the two, by 0.0017 in, and both are over 0.010 in. It is the one frame against the pattern, and it is close to a tie.

**The paper did not move between frames**, as entry 76 says. So the bad corner switching from right to left follows the camera, not a fixed pucker at one corner.

**The mechanism the frames show is lost markers.**
- **Which markers.** In every oblique frame, the markers that failed to decode are at the far bottom corner.
- **Why they failed.** A crop of `IMG_5824` shows that corner visibly out of focus, and `IMG_5822`'s far corner the same.
- **What follows.** The bull there loses the markers either side of it and is extrapolated from further away.
- **The same rows elsewhere.** Even in the two square-on frames, some corners of the bottom two marker rows are rejected as outliers.

**What this settles and what it does not.**
- **Settled:** the dominant cause is viewing geometry. The far corner goes soft, its markers drop out, and its bull loses support. That is the optical branch, and **the gate becomes a photography instruction.** Stand square to the sheet: the two frames taken that way have their worst scoring bull at 0.0059 and 0.0053 in under the surface model, and no bull over 0.010 in.
- **Not settled:** whether a small lift at both bottom corners also contributes. Obliquity would amplify it, and the far corner would show it more. This data cannot tell that apart from the optical cause, because the far corner was always a bottom one.
- **What would settle it.** One frame taken from below the sheet, so the top corners are the far ones.

### Entry 76 section 2: the friend's earlier sheet, re-run, keeps its corroboration

`grouplab analyze` was run on the 300 DPI scan of entry 56, which is local only and has no consent record. The marking was written to a scratch directory.

**The pipeline's own figures on that sheet are wrong, for a reason that is not the sighter pooling.**
- **What it found.** 15 holes: 12 scoring and 3 sighters.
- **What the sheet has.** 10 scoring holes, one per bull, entry 56 section 1. A crop of the scan shows exactly those ten, including one near bull 6 that entry 56's table leaves out: the table lists nine rows under ten numbers.
- **The two extra detections are duplicates.** The hole through bull 3's centre dot and the hole on bull 10's inner ring were each found twice, 0.151 in and 0.139 in apart, on either side of the printed ink.
- **What the duplicates did.** One-to-one matching then gave twelve shots to bulls. **The pipeline reports sigma 0.607 in, which is wrong.**
- **Not fixed here.** This is a detector defect for its own entry: a hole that crosses printed ink can be split into two.

**With the duplicates merged at their midpoints**, the ten real holes give:
- **σ 0.390 in**, 95% interval 0.295 to 0.577 in, against entry 56's 0.386 in;
- mean radius 0.489 in and extreme spread 1.493 in;
- aspect 1.24, which circular groups exceed 83 times in a hundred;
- **spacing over σ 3.84.**
- **Nearest-bull misassignment: 2 of 10.** Those are the shot left of bull 2, which is bull 1's, and the shot above bull 8, which lies nearer bull 3.

**Entry 76's hypothesis does not hold.** That sheet's sighters were never in its figures: entry 56 left them out by hand, and the pipeline leaves them out too. **Sigma did not fall, and the count did not move.**
- **The table still has its one real-paper corroboration.** At ratio 3.84 it expects 1.1 misassigned shots in ten centred on the aim, or 2.1 at the group's measured offset, and 2 were observed.
- **Why the two sheets differ.** The difference from Alan's scan, σ 0.274 in, is the two groups' own. Their intervals overlap.
- **Where it is recorded.** `docs/STATISTICS.md` section 9.3 now holds the table, its closed form, and what it rests on. The corroboration is described as one sheet of ten shots, which agrees with the table but cannot discriminate much.

**Tests:** Core 782 passing, App 42 passing, none skipped.

---

## Entries 78 and 79. The split, the size a hole of a calibre is detected at, and the thresholds still being measured

`docs/NOTES-FROM-PLANNING.md` entries 78 and 79.
- **The order:** entry 78 section 5's order, with entry 79 section 1 applied before anything was built on entry 78 section 4.
- **Entry 78 section 1:** not pursued as a difference-stage fix for split or oversized, as it asks.
- **The print note:** no box is added over it.

### CI for the entry 77 push, which entry 78 section 6 asked about first

Both **build and test** runs for the entry 77 commits passed on Windows, Ubuntu and macOS, the Linux tarball included. So the PDF fingerprint test holds on all three platforms: the renderer's PDF is byte-identical on each. The only red is the phase 0 gate record on macOS, which stays open.

### Entry 78 section 3, checked against the record before it was built on

**Entry 78 read the 54 "oversized" punched detections in `ink-proximity.json` as two holes merged into one mark. They are not.**
- **All 54 are split halves.** Most are halves of neighbouring punched holes that the split separated correctly.
- **Why they read as oversized.** Every half reports its whole blob's diameter, and that is what the harness compared. The harness's oversize column for split halves is therefore an artefact, and that is corrected here.
- **The 36 "split" detections are split halves too**, as entry 77 reported.

**So entry 78's reading, that split and oversized are one threshold failing in two directions, rests partly on that artefact.**

**The design point itself stands.** Stage S8's split decision is one threshold with two failures:
- a blob holding one hole that is split, a hole counted twice;
- a blob holding two holes that is not split, one oversized mark.

**`grouplab holes split-calibration [--local <manifest>]` measures that decision per blob**, over the elongation threshold crossed with the calibre veto below.
- **The corpus:**
  - the six 300 DPI letter scans punched once with single holes and once with overlapping pairs;
  - the two real sheets, the scan and seven photographs.
- **The synthetic sheets' single-hole size** is the median whole single-hole blob, 0.338 in over 848 blobs.
- **It is still running,** as the report says; its thresholds are not chosen yet.

### Entry 79 section 1: the bullet is not the hole, measured

**Detected diameter over stated calibre**, on the two real .308 sheets. These are whole detections matched to hand-verified holes, from the local ink-proximity record.

| | Holes | Mean | sd | Range | 10th to 90th percentile |
|---|---|---|---|---|---|
| Scans | 24 | 0.944 | 0.039 | 0.868 to 1.021 | 0.898 to 0.992 |
| Photographs | 75 | 0.986 | 0.110 | 0.740 to 1.304 | 0.863 to 1.131 |

**By image:**

| Image | Holes | Mean | sd |
|---|---|---|---|
| Alan's scan | 13 | 0.952 | 0.050 |
| The friend's scan | 11 | 0.934 | 0.017 |
| The six mounted frames | 13, 13, 13, 13, 10, and none on `IMG_5823` | 0.951, 1.003, 0.943, 1.043, 1.068 | up to 0.149 |
| The friend's photograph | 12 | 0.924 | 0.050 |

**What the ratio shows.**
- **The detector measures the torn crown, not the bright aperture.** Its extent is close to the calibre, not the aperture's 0.68 of it.
- **The probe's 0.235 in holes were the synthesis's own size**, which is smaller than any real .308 hole measures.
- **The spread differs by image kind.** Scans are tight, with a coefficient of variation of 4 percent. Photographs are moderately wide, at 11 percent, and their frame means move by 15 percent.

**What is used, therefore.**
- **Not the calibre,** but the calibre times these measured ratios: `AutomaticMarking.ScanHoleToCalibre` 0.944 and `PhotographHoleToCalibre` 0.986.
- **Which ratio applies** is decided by whether the image carries camera data. A photograph stripped of it is taken as a scan. The one such photograph, the friend's, measures 0.924, close to the scan ratio.
- **The calibre's role is narrowed as section 1 asks when the spread is wide.** It separates one hole from two, and nothing else. No filter reads it as an absolute size.

### Entry 78 section 4: the size fed into segmentation, with its two guards

**`RenderDifferenceOptions.CalibreInches`** is the size a single hole is detected at. When it is set:
- **the veto:** a blob whose elongation asks for a split, but whose area holds fewer than `SplitMinimumHoles` such holes, is kept as one hole. Such a hole is flagged `SplitVetoed`, and is exempt from the aspect and size filters just as a split candidate is;
- **the report:** a whole blob of `CalibreOversizeHoles` holes or more is flagged oversized and left as one mark.

**The size can stop a split but never make one**, and a hole that disagrees with it is reported, not cut to agree. Those are entry 78's two guards.

**Without a calibre, nothing changes.**
- **The rule is the old one:** a blob with no calibre is judged exactly as before.
- **Both real scans read as before:** 15 holes each, sigma 0.607 and 0.274 in.
- **The corpus counts are unchanged,** because no committed or local record names a calibre.

**The defaults are provisional until the sweep reports.** `SplitMinimumHoles` is 1.5 and `CalibreOversizeHoles` is 1.8.

**Where the calibre comes from.**
- **`grouplab analyze --calibre <calibre>`** passes it on the command line.
- **The marking screen** passes the calibre named at the time of detection.
  - A calibre named for one image stays named for the next, in view in the box, so detection on opening can use it.
  - Naming a calibre after a detection nobody has corrected detects again with it.
  - Naming one after a correction says the next Detect would use it and would replace the corrections, and leaves the marks alone.
- **The S5-S8 stage records** the calibre and the size used, and how many splits the calibre stopped.

**The result that matters:** on the friend's earlier sheet at .308, the two holes that each read twice now read once.

| The friend's earlier sheet | Holes | Sigma (in) |
|---|---|---|
| Without a calibre | 15 | 0.607 |
| At .308 | 13 | 0.391 |
| Hand-merged truth, entry 76 section 2 | 13 | 0.390 |

**Alan's scan does not move.** Its one split blob, the sighter hole joined to the print note's residue, holds 2.2 holes' area, so the calibre does not veto it.

**Tests.**
- **`CalibreSplitTests`** puts a hole of each of four kinds on a synthetic sheet:
  - a plain hole is untouched;
  - a hole with hand ink joined to it is cut in two without the size and kept whole with it;
  - an overlapping pair is split either way;
  - a large round hole of 2.9 holes' area stays one mark, flagged.
- **`AutomaticMarkingTests`** checks that the detector is handed 0.308 times 0.944 on a scan, and that the trace says so.
- **The marking screen's test** checks detecting again, leaving corrections alone, and carrying the calibre to the next image.

### Entry 78 section 2

**Not started.** The photograph residue is a different stage and follows the threshold work.

### Entry 79 sections 2 and 3: long jobs, and the CLI's lock on its own build

**`CONTRIBUTING.md` "Long-running steps" now carries entry 79 section 2's rule** in place of the old one.
- **The rule:** a CI run or a sweep that outlasts the turn ends the turn, with its output path and the command that reads it.
- **The convention it adds:** long command-line jobs run from a published copy outside the build tree, not from `bin`.
- **Why:** the running sweep held `grouplab.exe` in `bin`, and this work was built and tested to a separate output path under `tests/GroupLab.Core.Tests/bin/alt` until it ends.

**Tests:** Core 810 passing, App 43 passing, none skipped.

---

## Entries 87 and 86. Two size checks reduced to one, ink inside the footprint measured, and the README's states

`docs/NOTES-FROM-PLANNING.md` entry 87 first, because it corrects entry 86, then entry 86.

### Entry 87 section 1: which check the per-frame table scored, and one check from here

**The per-frame false-flag table scored the detector's flag.** Its harness reads `RenderDifferenceHole.Oversized`, the flag the review queue counts. **So the table stands and the ground truth behind it needed no rewrite.**

**Both checks reproduced on Alan's 600 DPI scan, at .308:**

| | Fires |
|---|---|
| The detector's flag, which the queue counts | 1, on S1b, at 2.18 holes' area |
| The screen's calibre size check | 5, on the shots at bulls 1, 4, 6, 7 and 10 |

**Those five are the ones Alan has confirmed with the shooter as single holes.** Their two measurements, side by side:

| Shot | The detector's diameter | Ink inside its footprint | The screen's dark-region extent |
|---|---|---|---|
| 1 | 0.274 in | 0.12 | 0.547 in |
| 4 | 0.296 in | 0.11 | 0.607 in |
| 6 | 0.267 in | 0.10 | 0.600 in |
| 7 | 0.310 in | 0.11 | 0.607 in |
| 10 | 0.282 in | 0.10 | 0.508 in |

**The detector reads all five as ordinary holes, about 0.29 in. The screen's check reads them at about twice that.** It measures the dark region connected to the mark, and a printed ring is dark and connected, so the ring is included.

**So the screen's check is the defect, and it is gone.** What is left of `HoleSize` measures and judges nothing: the snap radius, and the apparent extent as a number anything may read.
- **The one size opinion** is the detector's flag, which is a review queue item, so nothing on screen asserts a size the queue has not counted.
- **What went with it:** the alert ring driven by that check, its five red sentences in the panel, its wording, and the `holeSizeFlags` field in the marking file. The detector's flag has its own ring and its own sentence, and rides on each shot in the file.
- **Its evidence is kept as a test:** the dark region under a mark on a printed line reads more than three times wider than the same measurement on a hole beside one.

**One correction to my own figures, from entry 86 section 1.** The per-frame table counted the flag on S1b as correct, on the ground that it was a hole joined to the print note's ink. **That was my assumption and it is wrong.**
- **S1b encloses no printed ink at all,** 0.00 of its footprint, and it reads 0.429 in across.
- **So Alan's scan has one false detector flag of 14 marks**, not none, and the print note is not part of it.
- **What the flag is saying** is that the mark covers 2.18 single holes' area. On a sheet where the shooter counted four sighter shots and the detector found four, that reading is unexplained rather than explained.

### Entry 86 section 1: S1b is a real shot, and the rule that follows

**Recorded.** The fourth sighter detection is a bullet hole. I identified it as the print note's ink by its position, from entry 77, without looking at the pixels, and told Alan to mark it not a shot. He has restored it.

**Entry 77's defect was already fixed.** The print note's ink is refused by the size and cell filters, and the log shows that happening on the same run.

**The rule, recorded here because it is a working rule and not an apology: a recorded defect is evidence about the past.** Before using one to explain something on screen, check it is still there.

**Nothing in the application needed changing for it.** The selected-shot panel's "It is a shot" and Undo both restore such a mark, and the review queue does not ask about it again.

### Entry 86 section 3: ink inside the detection's own footprint, measured

**This is a new measurement, not a re-run.** Each detection's footprint is its own hull, and the quantity is the mean expected printed-ink coverage inside it. It is now on every detection as `RenderDifferenceHole.InkFraction` and in `ink-proximity.json` as `inkInside`.

**On the two real scans, where there is no scale variation, enclosed ink does not explain the detector's diameters.**

| Sheet | Marks | Correlation of enclosed ink with diameter |
|---|---|---|
| Alan's scan | 14 | **-0.53** |
| The friend's scan | 13 | **-0.28** |

**Both are negative:** the marks enclosing the most ink are, if anything, the smaller ones.

**On the punched scans, the flagged marks are larger and enclose slightly less ink:**

| Punched scans | Marks | Median enclosed ink | Median diameter |
|---|---|---|---|
| Flagged | 1154 | 0.06 | 0.401 in |
| Unflagged | 1567 | 0.07 | 0.304 in |

**So the mechanism entry 86 section 3 proposes is not what inflates the detector's diameters.** Their size comes from merged holes, which is what the flag is for.

**It is exactly what inflated the screen's check**, and that is where the hypothesis was right: the extent it measured ran along the ring. The measurement above is what separates the two, and the check that had the defect is gone.

**What the enclosed-ink figures do show** is that a hole centred on a ring encloses 0.10 to 0.13 of its own footprint in ink on these sheets, and one beside a ring encloses none. It is a usable quantity, now recorded for every detection in the corpus, and nothing currently reads it.

### Entry 87 section 2: the README's planned features and their states

**The Planned section now lists every phase of `DESIGN.md` section 21 with one of four states and its gate**, and, under "What each phase holds", the features inside each phase with a state on each.

| State | Means |
|---|---|
| Not started | no code |
| In progress | being built, not usable |
| Built, not proven | the code exists and works, and its gate has not been met or cannot yet be run |
| Done | its gate has been met and recorded |

**The states as they stand:** Phase 0a done; phases 0, 1, 2 and 3 built, not proven; phase 4 in progress; phases 5 to 8 not started.

**The editor is why the fourth state exists.** It works, and its gate needs a 25-shot target nobody has shot, so neither "done" nor "in progress" would be true.

**The two guards entry 87 asks for.**
- **A test ties the two documents together.** `ReadmeTests` reads `DESIGN.md` section 21's phases and the README's table: the same phases must appear in both, each with exactly one of the four states and a gate beside it, each with a feature list, and every feature must carry a state. Checked by breaking it both ways: an invented state fails, and a renamed phase fails.
- **A state changes in the commit that changes the thing,** which the section says in its last line rather than leaving to habit.

**Three things kept out, following entry 60:** no test counts, no percentages and no dates. The test enforces that too, refusing a per cent sign, a year, or a phrase like "40 tests". A gate's own threshold, such as 99 percent of holes found, is the gate and is spelled in words.

**Tests:** Core 821 passing, App 45 passing, none skipped.

---

## Entries 90, 89, 88 and 84. The scope given phases, the load block checked, the shape of the last unexplained flag, and the 25-shot rehearsal

`docs/NOTES-FROM-PLANNING.md` entries 90, 89 section 4, 88 section 1 and 84, in that order.

### Entry 90 section 1 and 2: ten promises, eight scheduled and two deferred

**Every bullet of `DESIGN.md` section 3 now names a phase or is marked deferred with the reason, and section 19's two commitments name a phase.** Which each one got:

| Promise | Given | Where it now stands |
|---|---|---|
| A parametric target editor | **Deferred** | No specification of the authoring screen exists, and one is being written with the shooter who asked for it. The format is not the obstacle: a fully custom sheet of forty arbitrarily placed bulls fits in a QR payload |
| A full visual target designer | **Deferred** | The same reason and the same specification |
| Hit probability | **Phase 2**, built not proven | Three estimators are in the engine, with the CEP table behind them. At a distance other than the one shot it goes through the solver, which is Phase 5 |
| Distance normalisation | **Phase 5**, not started | Named in the phase rather than left implied by the solver being ported |
| Records for rifles, barrels and loads | **Phase 4**, not started | Beside the session records, which were the only one of the four the plan carried |
| The secondary mode for any target | **Phase 3**, done | It is marking by hand on any photograph against a reference length, which exists. The phase's feature line now says so in the document's own words |
| Assisted hole placement in that mode | **Deferred** | Question 18. A store-bought target has no definition to render and difference against |
| An unobtrusive support link | **Phase 4**, not started | One menu item, on the terms in section 20 |
| The four themes | **Phase 4**, not started | Dark, light, high contrast, follow system |
| The stage timeline that shows the analysis working | **Phase 4**, not started | The record every stage emits exists and the console form of it gave both spikes their output. The screen does not exist |

**Two things the sweep found that the list did not predict.**
- **Significance testing and hit probability are built and the README had lost both.** The engine carries rank and dispersion tests between two groups and among several, MANOVA on shot coordinates, the dispersion ratio with its interval and Holm correction, and three hit-probability estimators. Entry 90 section 3 spotted significance testing missing from the page; hit probability was missing from both documents' plans while being implemented. The page understated the engine rather than overstating it, which is the rarer direction.
- **The three-axis unit setting is done**, and it was sitting in the trailing orphan line rather than in a phase.

**The four orphans are now features with states and the line is gone:** the unit setting done, adjust-to-zero turret corrections not started, calibre-aware edge-to-edge spread done, the volunteer print pack not started.

### Entry 90 section 4 item 2: the test now reads the scope

**`ReadmeTests.EveryPromiseInScopeNamesAPhaseThatExistsOrSaysItIsDeferred`** reads `DESIGN.md` section 3's In scope list and requires of every bullet:
- **a phase of section 21 by number**, and that phase must exist, so renaming a phase cannot orphan a promise; **or**
- **the word deferred, with a citation** of the notes or of the questions file, so a deferral cannot be a quiet drop.

It also requires the README to list at least as many deferrals as section 3 declares. **Checked by breaking it both ways:** removing "(Phase 5)" from the chronograph bullet fails with "promises something that no phase builds and no deferral covers", and sending it to a Phase 12 fails with "which section 21 does not have".

**The feature scan of entry 87's test is now scoped to the "What each phase holds" subsection**, so the deferral bullets below it are not read as features with impossible states.

### Entry 90 section 5: assisted hole placement is question 18, and two thirds of it is already built

**Raised as a question rather than scheduled, as the entry asks.** What the question carries, which was not known before it was written:

| What "assisted" could mean | Built | Needs a definition |
|---|---|---|
| A tap snapped to the hole under it | **yes**, `Snapping.ToHole` takes the artwork as an optional argument and falls back to the dark centroid | no |
| A snap radius set by the calibre, so the snap is the size of a hole | **yes** | no |
| Finding holes unprompted | yes, render-and-difference | **yes** |

**So the bullet is not unachievable; it is undefined.** Blank paper is the easy case, because paper has a predictable appearance and the difference stage degenerates to dark blobs on a light field. A printed store-bought target is the hard case and is exactly what the artwork mask exists for. A user-traced definition is the third route and it loops back into the deferred designer. The question puts those three to planning with their costs and recommends naming the snap as the promise now, scheduling blank paper, and refusing ring-fitting heuristics.

### Entry 89 section 4: nothing in the stack ties a load field to a bull

**Checked rather than assumed, and the answer is no, at every level.**

| | What it carries | Any bull association |
|---|---|---|
| `dataBlock` (section 3.10) | one band, one `layout`, one `fieldSet`, one geometry | **none.** Nine captions for the sheet |
| `instance` (section 3.11) | a flat map of field name to value, explicitly not part of the definition or its identifier | **none** |
| `bulls[i]` (section 3.5) | x, y, ringSet, label, scoring, labelOffset | **`scoring` only**, a two-way split into the composite pool and the sighters |
| `MarkedShot` | a bull index, provenance, exclusion, diameter, oversize | **none.** No group tag |

**Labels cannot be pressed into service**, because section 3.5 states they are not identifiers, are not required to be unique, and the sample data has a real sheet with two bulls both labelled 9.

**The analysis half of subgroups already exists.** `GroupComparison.KruskalWallis`, `FlignerKilleen`, `ManovaGroups` and `DispersionRatio` all take a group label per shot, so six loads on one sheet could be compared today. Nothing can tell them which shot belongs to which load.

**Three routes, with their costs, for planning rather than as a plan.**

| | Where the mapping would live | Cost |
|---|---|---|
| **A** | A subgroup tag on each bull in the definition | A new field in GLTD-J, a new block and flag bit in GLTD-B, and new identifiers for every sheet that uses it. The printed sheet can then label its own subgroups |
| **B** | In `instance`, where per-print data belongs | The GLTD-I budget is 152 bytes and one filled standard-9 set already measures 128, so six loads cannot ride in the printed code |
| **C** | In the session, as an editor step: these bulls hold this load | **No format change, and it works on the existing 30-bull and 36-bull sheets today.** The sheet itself says nothing, so the shooter writes the loads in the blank block by hand |

**C first is the recommendation**, with A only if the printed sheet must carry the subgroups. This is recorded as a finding; entry 89 section 5's questions for Jeff are what decide it.

### Entry 88 section 1: S1b's shape measured, and the flag is counting the hull

**Every detection now carries its shape**, in `RenderDifferenceHole` and in `ink-proximity.json`: the bounding box aspect, the orientation-free elongation from the residual-weighted second moments, the hull solidity, and the hull area in square inches at the blob's own scale. Entry 88 section 2 asked for the measurement to live where the detector knows which mark is which, and it does.

**S1b, against the sheet it is on and against real pairs of known separation:**

| | Diameter | Elongation | Solidity |
|---|---|---|---|
| The other 13 marks on Alan's scan | 0.267 to 0.314 in | 1.05 to 1.39 | 0.89 to 0.97 |
| **S1b** | **0.429 in** | **1.60** | **0.59** |
| Composited pairs of real holes, 0.15 in apart | 0.380 in | 1.65 | 0.926 |
| the same, 0.25 in apart | 0.425 in | 2.12 | 0.875 |
| the same, 0.35 in apart | 0.466 in | 2.73 | 0.785 |

**The yaw explanation is not supported by the shape.** A bullet arriving yawed makes an oval, and an oval is convex: it would read a high solidity, like every clean hole on the sheet. S1b has the lowest solidity of any mark on it by a wide margin.

**Two holes is not supported either.** A composited pair the size of S1b reads an elongation over 2, and S1b reads 1.60. Pairs never fall below 0.70 solidity at any separation that keeps them one mark.

**What the numbers do say, and it is a finding about the flag rather than about the shot.** The flag measures hull area, and a mark with a ragged outline has a hull much larger than its ink.

| Flagged mark | Hull | Ink inside the hull | In single holes, hull against ink |
|---|---|---|---|
| S1b, Alan's scan | 0.1444 sq in | 0.0847 sq in | **2.10 against 1.23** |
| `IMG_5822` at (2.850, 9.299) | 0.1212 | 0.0772 | 1.76 against 1.12 |
| `IMG_5824` at (1.371, 1.988) | 0.1030 | 0.0708 | 1.50 against 1.03 |
| `IMG_5822` at (7.225, 3.479) | 0.0933 | 0.0823 | 1.36 against 1.20 |
| The friend's photograph at (7.621, 3.554) | 0.1689 | 0.1253 | 2.46 against 1.82 |

A single hole on Alan's scan is 0.0687 sq in of hull at the median. **Four of the five flags on real material hold one hole's worth of ink**, so the third cause of an oversized reading is neither two holes nor a yawed bullet: it is one hole with a torn or ragged rim, counted by its convex hull.

**Nothing was changed for it**, per entry 83 section 3. The candidate fix is to flag on ink area, or to require a solidity, and it moves a threshold the Phase 1 gate is measured against, so it is recorded with its numbers rather than applied. S1b's ink area of 0.0847 sq in is an equivalent diameter of 0.329 in, which is an ordinary .308 hole.

**One figure confirmed rather than re-derived:** S1b encloses 0.00 of its footprint in printed ink, as entry 87's correction said.

### Entry 84 section 3: the photograph ratio, re-measured at each blob's own scale

**Entry 79's 0.986 was measured with one scale for a whole oblique image, which the entry 83 fix showed is wrong by up to 30 percent across a sheet.** Re-measured from the rebuilt local record, whole matched detections with no oversize flag:

| | Holes | Mean | sd | Spread of frame means |
|---|---|---|---|---|
| Photographs, entry 79, one scale for the image | 75 | 0.986 | 0.110 | 0.92 to 1.07 |
| **Photographs, now, each blob's own scale** | 76 | **0.948** | **0.039** | **0.918 to 0.976** |
| Scans, now | 26 | 0.938 | 0.044 | 0.923 and 0.952 by sheet |

**The defect was inflating both the figure and its spread.** The coefficient of variation falls from 11 percent to 4 percent, and the frame-to-frame spread from 15 percent to 6.

**`AutomaticMarking.PhotographHoleToCalibre` is 0.948**, the re-measurement, where it was 0.986. **`ScanHoleToCalibre` stays 0.944**: scans have no scale variation to fix and re-measure at 0.938, the move coming from the friend's scan's verified hole list growing by two, which is inside the sheet-to-sheet spread that entry 80 section 3 established as the unit of uncertainty.

**The two image kinds now agree within 0.010**, so the distinction between them was substantially an artefact of the defect. The constants are kept separate because two sheets are not evidence for merging them, and the figure is read only to tell one hole from two.

**The spurious rise entry 84 section 3 asks to record deliberately:** the clean photographs carry **35** spurious detections where they carried 24, because far-side residue now reads at its true larger size and fewer slivers fall under the floor. It is the correct direction and it is left alone.

### Entry 84 section 2: the 25-shot rehearsal, which is not the gate

**A synthetic GL-CF25-LTR sheet, 26 marks for 25 scoring bulls, with entry 84's five errors injected:** two shots placed past halfway to the next bull, one merged pair 0.17 in apart, one shot fired at one bull and landed on its neighbour, one half-size faint mark, leaving one bull with two and one with none. The holes are drawn at the 0.871 scale entry 81 established, so a single one reads what a real .308 hole reads.

**What the software did.**

| | |
|---|---|
| Registration and detection at 600 DPI | 9 to 22 seconds, all 26 marks found, 25 detections, the pair read as one mark |
| The queue as detection left it | **15 items:** 4 contested, 11 oversized |
| Settled with the answer truth says is right | **19 items handled, 22 key presses, 1 tap on the image, 9 ms of software time**, 0 left open |
| After settling | 26 shots, every bull holding what was fired at it except the stray, which no image can attribute |

**Four findings, which is what a rehearsal is for.**

1. **One extra shot in a cell displaced a chain of three assignments.** One-to-one matching moved bull 20's own shot to bull 22, 4.7 inches away, the stray onto bull 20, and bull 22's shot onto bull 21. **All three surfaced as contested items with the right bull among the choices**, which is the mechanism working; nearest-bull would have put the stray on bull 20 and left bull 21 silently empty.
2. **The merged pair cannot be finished from the keyboard.** Its item reads "add the second shot if there is one" and offers only "One shot" and "Not a shot", so the second shot is a tap on the image. That is the one injected error the keyboard path does not cover, and it is worth a choice on the item.
3. **The synthetic hole model overstates the flag load.** Eleven of 25 marks were flagged oversized at the corrected scale, and 17 of 23 unscaled, against **one of 14 on Alan's real scan**. The model's area spread is far wider than real holes', so a synthetic sheet rehearses the assignment path honestly and the flag load not at all.
4. **A half-size mark is still a shot.** At 0.5 scale it reads over the 0.15 in floor, so no refused-candidate item arose. Rehearsing that item needs a mark near the floor rather than merely small.

**It is not the Phase 3 gate and is not recorded as passing it.** The gate is a person correcting several misassignments on a real 25-shot sheet in under two minutes, and nothing here measures a person reading fifteen cards. What it establishes is that the software's own share of those two minutes is 9 milliseconds of editing and 9 to 22 seconds of analysis, that the injected errors all reach the queue, and that one of them needs a mouse.

**Tests:** Core and App suites both pass, none skipped.

---

## Entries 94, 91, 92 and 93. The flag counts the mark, subgroups in the session, the zero correction, and the concept as the design language

`docs/NOTES-FROM-PLANNING.md` entry 94 first, then 91, 92 and 93.

### Entry 94 section 1: the oversize flag counts the mark's own area, and the sweep that confirms it

**The flag measured the area of a convex hull thrown around a mark. It now measures the mark.** A torn single hole and a genuinely merged pair are 2.1 and 2.2 holes of hull, which nothing can separate, and about 1 and about 2 of their own area, which separates cleanly.

**Entry 81's confirming sweep re-run, because the thresholds were fitted against the wrong quantity.** Composited pairs of real holes at known separations, with the calibre named, `grouplab holes composite-pairs`:

| | Single holes | Pairs 0.10 in apart | 0.15 in apart | 0.20 in apart |
|---|---|---|---|---|
| **Holes' area read, min to max** | **0.86 to 1.01** | **1.30 to 1.41** | 1.49 to 1.59 | 1.73 to 1.75 |
| Flagged, before, by hull | 0 percent | 100 percent | 100 percent | 38 percent of those still one mark |
| **Flagged, now, by area** | **0 percent** | **69 percent** | **100 percent** | 38 percent of those still one mark |

**Nothing changed for a single hole and the 1.35 threshold is unchanged.** What changed is at the tightest separation: a pair whose centres are 0.10 in apart overlaps by two thirds, so its ink really is only 1.37 holes, and about a third of those now fall under the threshold.

**That is the trade and it is worth taking.** On real material:

| | Flags on the corpus's real sheets |
|---|---|
| Before, by hull | **5**, of which four were ordinary holes with ragged rims |
| **Now, by area** | **2** |

The two that remain are S1b, at 1.37 of the sheet's own single-hole size, and the friend's photograph's 0.464 in mark at 1.90, which holds nearly two holes of ink and is the one mark on real material that looks like two shots.

**The threshold was left at 1.35 deliberately.** Lowering it to about 1.28 would catch every 0.10 in pair, and it would also bring S1b back, because a torn hole holding 1.28 holes of ink and a pair holding 1.30 are not separable by area either. Fitting a threshold into that gap would be fitting it to one mark.

**What else reads the new quantity:** the review queue's sentence, the canvas ring, and `RenderDifferenceHole.CalibreHoles`, so nothing reports a mark as bigger than it is. The hull area is kept beside it as `HullAreaInches`, since solidity is the ratio of the two and the shape measurements of entry 88 rest on it.

### Entry 94 section 4: the merged pair has a keyboard path

**The item the flag exists to raise offered "One shot" and "Not a shot", so a mark that really was two shots sent a person to the mouse.** It now offers **"Two shots"**, and the detector supplies where they go: when a mark is large enough that the flag may fire on it, the same weighted split the detector uses when it does cut a blob runs anyway, and the two centres ride on the flag through the marking file. Taking the choice moves the shot to the first centre and puts a second on the other, on the same bull, clearing the size flag from both because it was a statement about one mark that is now two. The pair then appears as a bull holding two shots, which is the item that asks whether that is what happened.

**On the keyboard it is `T`**, beside Enter for the first choice, Space for the next item, a bull's number to reassign and N for not a shot.

**The rehearsal, re-run:**

| | Before | After |
|---|---|---|
| Queue items as detection leaves it | 15 | **8** |
| Items handled to settle everything | 19 | 12 |
| Key presses | 22 | **15** |
| **Taps on the image** | **1** | **0** |

**The loop is keyboard-complete on the injected set**, which is what entry 84 built the rehearsal to find out and entry 94 section 4 asked for before the gate is timed for real.

### Entry 94 section 5: the synthetic flag ratio, re-checked

**It was 11 of 25 synthetic marks against 1 of 14 on real paper, about ten to one. It is now 4 of 25 against 1 of 14, about two to one.** Most of the excess was the hull rather than the hole model: a synthetic hole's rim is rougher than a real one's, and the hull counted that roughness as area. What is left is a real but much smaller difference, and a synthetic sheet is now within a factor of two of real paper on the flag as well as honest on the assignment path.

### Entry 94 section 2: subgroups take the session route

**One sheet can now carry several loads**, and nothing about the format changed.
- **`MarkingState.Subgroups`** maps a bull index to a name, in the session and in the marking file, so a saved marking still knows which bulls held which load.
- **`GroupAnalysis.Subgroups`** reports each subgroup with its own figures, computed exactly as a whole sheet's are, and compares them: Fligner-Killeen on each shot's distance from its own subgroup's centre for dispersion, and the one-way MANOVA of `docs/STATISTICS.md` section 8.2 on the offsets for the centres, which for two subgroups is Hotelling's test.
- **No verdict is drawn.** The p-values sit beside the subgroups, for the same reason entry 91 gives about a zero correction: which load is better is a claim about the next group.
- **The whole-sheet figures are unchanged**, and a bull in no subgroup is in none.

**It works on the thirty-bull and thirty-six-bull sheets in the library today**, so six charge weights at five shots each can be shot and compared before anything is designed. When Jeff's specification arrives the format question reopens with this as the fallback that already works.

### Entry 94 section 3: one hole-size constant

**`ScanHoleToCalibre` 0.944 and `PhotographHoleToCalibre` 0.986 are one constant, `HoleToCalibre` 0.945**, over 102 holes on eight frames of two sheets, sheet means 0.949 and 0.932, frame means 0.918 to 0.976. **The branch went with them:** a photograph carrying no camera data was read as a scan, which mattered only while the two figures differed.

### Entries 91 and 92: the zero correction, with its uncertainty

**It is its own section, above the group statistics**, because it answers a different question at a different moment: what to dial now, read standing at a bench, against how well the rifle shoots, read afterwards sitting down. It is also the one figure on the screen that is a claim about the next group rather than a description of this one.

**What it shows**, per entry 92 section 2:

| Row | |
|---|---|
| Group centre, windage | the offset, in a linear and an angular unit at once, and which way it sits |
| Group centre, elevation | the same, as a separate row, because a turret has two knobs and nobody dials a diagonal |
| give or take | the uncertainty on each axis, in the same units, at 95 percent |
| **The verdict** | either "Dial 0.44 MOA left and 0.23 MOA down", or "Not distinguishable from zero at 10 shots: the smallest offset these shots can call is 0.70 MOA. About 25 shots would settle it." |

**The rule is entry 53 section 3's** and the code computes its table rather than copying it: the smallest offset distinguishable from zero is `t(0.975, df) / sqrt(n)` times sigma, which is 1.603 sigma at three shots, 1.031 at five, 0.664 at ten and 0.402 at twenty-five, with `df = 2n - 2` for a circular group and `n - 1` per axis where circularity is rejected, since a group that strings vertically knows less about its vertical centre. A test reproduces all six rows of the table from the formula.

**One correction to entry 92 section 1's example, and it matters at a bench.** The entry writes the correction for a centre sitting left and high as "dial 0.44 MOA left and 0.23 MOA down". **The turret moves the point of impact, so a group sitting left is corrected by dialling right.** The code takes the direction opposite the offset and the two words are kept apart in the readout: where the group sits, and what to dial.

**Two things it does not do**, both named on screen: it does not cross a distance, which is the solver's job in Phase 5, and it does not count clicks, which needs the scope's click value, which is a rifle record, which entry 90 scheduled in Phase 4. Until then it stops at a linear and an angular figure, and every turret is marked in one of them.

**Also entry 91 section 1's wording correction:** the label says group centre. The mean radius is a distance, not a place.

### Entry 92 section 3: the statistics as label and value rows

**Three headline figures, each followed by two lines of interval, was nine lines before a reader reached anything else.** Now each figure is one row, label left and value right, with the angular conversion on the same row as its linear value. Sigma and mean radius keep an interval line; extreme spread's interval moved behind "More figures", because it is the least informative of the three and its interval changes no decision. The row is the concept's own selected-detection pattern, arrived at from two directions: entry 92 section 3 from use, entry 93 section 2 from the design.

### Entry 93: the concept as a design language, and the four themes from one token set

**Entry 84's hold on appearance is lifted**, on Alan's approval of the concept as the guideline, and the parts that touch the working loop came first, per entry 93 section 5.

- **The tool strip** is icons with their keys drawn as keycaps rather than a wrapped row of text buttons, and the names stay beside the icons, because an icon alone is a guess for anyone who has not used the application.
- **The breadcrumb header** carries what is open and what is on it, with one primary action in amber on the right and a secondary beside it.
- **The left rail is built and its destinations are not**, per entry 93 section 4. Four of its five icons say which phase builds them rather than opening an empty screen. A styling pass that starts inventing screens is how this becomes a rewrite.
- **Readouts are label and value rows**, which is entry 92 section 3.
- **The two accents keep fixed meanings**: teal for what the software found on its own, amber for what needs a person and for the primary action. A test holds them to their hues in every theme.

**The four-theme token guard, which entry 93 section 3 calls not optional.** The high contrast theme is **derived from the dark tokens in code**, not drawn: every hue is kept and every text colour is lifted along it until it reaches WCAG's AAA ratio of 7:1 against a black window and near-black panels, with the separators taken to a visible grey. Light was already derived by hand under entry 42.

**Two tests hold the guard.** Every text colour reaches its ratio on every surface in all three palettes, 4.5:1 for dark and light and 7:1 for high contrast; and the other themes are the same roles as the dark one, with teal still green, amber still warm and alert still red, so "teal is what the software found" survives a theme change. If a token set cannot produce the other themes it is not a token set, and this is where that would show.

**What is not done:** the paper-coloured sheet on dark chrome, and the rest of the framing. Entry 93 section 5 puts those second, and the gate has not moved: appearance does not pass Phase 3, a person with a stopwatch and a real 25-shot sheet does.

---

## Entries 97 and 96. The concept's framing, records and clicks, the analysis showing its work, and whether a warp absorbs the print

`docs/NOTES-FROM-PLANNING.md` entry 97 in its own order, with entry 96 section 2 first because it needed no building.

### Entry 96 section 2: a low-order warp absorbs part of the print error, not most of it

**The test as entry 96 asks it:** fit the markers of the flatbed scan with warps of rising freedom and see whether the bull error falls from 0.0027 in toward 0.0015. The warps are image-to-page polynomials fitted by least squares to the marker corners alone, so the bulls are an out-of-sample check.

| Scoring bulls, rms (in) | Surface fit | Degree 2 | Degree 3 | Degree 4 |
|---|---|---|---|---|
| **Alan's sheet, flatbed** | **0.0027** | 0.0024 | 0.0022 | **0.0020** |
| Phase 0 sheet 1, flatbed | 0.0012 | 0.0010 | 0.0010 | 0.0011 |
| Phase 0 sheet 2 | 0.0014 | 0.0011 | 0.0011 | 0.0013 |
| Phase 0 sheet 3 | 0.0012 | 0.0010 | 0.0008 | 0.0009 |

**About 0.0018 in of this sheet's error is smooth and a fit can absorb it; about 0.0020 in is not.** The unabsorbed part is still twice what a Phase 0 sheet leaves under the same warp, so entry 96 section 1's concern stands for the scan gate: a warp does not make a poorly printed sheet into a good one.

**The numbers still do not reconcile, and this is why.** If the scan's unabsorbed 0.0020 in were print error, the photographs would carry it too, since a photograph's surface fit is smoother than a degree-4 warp. They correlate with the scan at +0.27 and -0.04. **So at least part of what the scan reports belongs to the scan**: this sheet was scanned after being mounted, shot, taped and handled, and a sheet that no longer lies flat on the platen reports its own shape. The flatbed scan of the clean sheet before it is shot, which entry 96 section 1 asks for, is the measurement that separates the two.

**One more thing the same warp showed, recorded and not acted on.** On `IMG_5819` a degree-3 or degree-4 warp over the markers brings the scoring bulls to **0.0020 and 0.0016 in rms, worst 0.0036 and 0.0034 in**, inside the gate, where the surface model gives 0.0034 rms and 0.0059 worst. On `IMG_5820`, which fills the frame, the same warps are worse than the surface model, 0.0039 in rms. **So on the wide square-on frame the surface model's residual is smooth enough for a generic warp to take up**, which fits section 7's camera-or-paper question without answering it. The warp is not adopted: it was tried on the frames being gated, and a model chosen because it passes them is the thing entry 17 forbids.

### Entry 97 section 1: the concept's framing

- **The document is a paper sheet on dark chrome.** The image sits on a paper mount edged by one line, and an empty canvas shows a blank letter sheet with its hint on it, not text on the chrome.
- **The accents follow entry 93's rule on the image as well as in the panel.** A shot the software found and nobody has had to touch is teal; one a person placed or corrected is neutral; one the review queue still wants a decision on is amber, and the current one's line to its bull is amber and dashed, as the concept draws it. Red is kept for what is wrong: a missing marker and a mark flagged as two holes.
- **The review card is amber-tinted**, with its first choice, the one Enter takes, as the amber primary. It had a red border, which put the alert colour on the thing that only needs a person.
- **The list carries NOW, NEXT and DONE as coloured words**, the header carries the concept's "2 of 3 need review" beside the actions, the rail marks where you are in amber, and the rotate buttons have keycaps like every other tool.
- **The selected shot reads as label and value rows**, shot, position, diameter, size in holes and margin, with its provenance as a chip in the shot list's own words.

### Entry 97 section 5: the guard, and what it found on its first run

**The Core rehearsal cannot see the window.** It drives the review queue directly, so a styling pass that broke a key or left an item unselected would pass it. **`RehearsalTests` now does the same job through the window**: it builds entry 84's injected 25-shot sheet in memory, detects it, puts the result in the window, and settles every item with the answer truth says is right by pressing the window's own keys.

**Its first run failed, on a real defect.** Straight after detection the first item was current and its shot was not selected, so a bull typed for it went nowhere until a person clicked the shot. The window now selects the current item's shot whenever nothing else is selected.

**Its second finding was layout, not keys.** When the timeline strip first went under the image it took enough height that the fitted zoom fell, and the tap snap, whose radius is set in screen pixels, grew until a tap on one hole was pulled toward the next. The strip is one row while closed. **The snap radius depending on the window's layout is worth knowing**: any chrome that takes height from the image changes how taps snap.

| After | Core rehearsal, 600 DPI | Window rehearsal, 300 DPI |
|---|---|---|
| Entry 94, the last figure | 15 presses, 0 taps | did not exist |
| Batch 1, framing | 15 presses, 0 taps | 10 presses, 0 taps, 7 items |
| Batch 2, records | 15 presses, 0 taps | 10 presses, 0 taps |
| Batch 3, timeline | 15 presses, 0 taps | 10 presses, 0 taps |

**Nothing in the three batches cost the loop a key press.** The window rehearsal's 10 is its ceiling in the test, so the next batch that costs one fails.

### Entry 97 section 2: rifle, barrel and load records, and clicks

**Kept small, as asked.** A rifle is a name and its scope's click, a barrel is a name, its rifle and a round count, a load is a name and its components as free text. They live in one `records.json` beside the settings. **Not a reloading database.**

- **The marking says what it was shot with**, and keeps the rifle whole, click value included, so a correction read from a saved marking is the one that was right when it was shot.
- **The zero correction says clicks** where the marking names a rifle and the shot distance is set, for each axis worth dialling: "Dial 5 clicks left (1.200 in  1.15 MOA, leaving 0.10 MOA)". Whole clicks and what rounding leaves, because a turret has no half positions.
- **The angle of an offset is the full angle to a point**, atan(offset over distance), not the half-angle form `docs/STATISTICS.md` section 12.5 uses for a size across a group. At zeroing distances the two differ in the fourth figure, and the right one costs nothing.
- **A barrel's count grows only when a person says so**, with "Add this sheet's shots", so reopening a marking can never count a sheet twice.
- **Without a rifle or a distance** the panel says which is missing rather than printing a bare angle as if it were the answer.

### Entry 97 section 3: the analysis showing its work

**Every stage's record is on a timeline under the image**, the ones every stage already files. A slider scrubs it; the stage buttons, coloured by outcome, jump to one; its parameters, metrics, decisions and details open beneath; and **every rejection with a page position is a button that finds it on the image**, centred and ringed in amber, with the scrubbed stage's other rejections drawn faint around it.

**A live run shows each stage as it lands.** The trace raises an event as each stage files its record, and the window puts it on the timeline and moves to it while the analysis is still running.

**The design's two constraints hold, and each has a test.**
- **The trace is never the only place an error appears.** A blank page fails registration; the panel says "Detection failed" in the normal way, and the failed stage is on the timeline as the detail behind it.
- **The theatre does not slow the pipeline.** The records are the ones every stage files anyway, nothing is computed for the timeline, and a batch run has nobody listening to the event, which then costs nothing.

**What is not done:** the per-stage image artefacts of `DESIGN.md` section 19, the markers lighting up, the residual map settling and the artwork vanishing. What lands live is each stage's record; the image shows a stage's rejections, not its intermediate rasters. Those rasters exist inside the detector and are not kept, and keeping them for an interactive run is the next step, behind section 19's rule that they default on for one analysis and off for a batch.

### Entry 97 section 4: the macOS gate record

**Not picked up.** No batch was blocked, so there was no gap to fill. It remains the only red in CI, with its named cause.

**Tests:** Core and App suites both pass, none skipped.

---

## Entries 98, 99 and 100. The leftover scan error is not the holes, the snap in sheet units, the parametric editor, and each stage's picture

`docs/NOTES-FROM-PLANNING.md` entry 98 in the order its covering message set, with entries 99 and 100, which arrived during it, after its section 2. Entry 98 section 5's second item, the macOS gate record divergence, gets its own turn next, as the entry asks.

### Entry 98 section 3: the leftover is not where the holes are

**The test as entry 98 sets it:** after the warp, compare the flatbed scan's holed bulls, 1 to 10 and the three sighters, against the clean bulls 11 to 25. **With a control the entry did not ask for and which turned out to decide it:** the same split on the three unshot Phase 0 sheets, where "holed" is only a position on the page.

| Scoring and sighter bulls, rms (in) | Surface fit, 1-10 and sighters | Surface fit, 11-25 | Degree 4, 1-10 and sighters | Degree 4, 11-25 |
|---|---|---|---|---|
| **Alan's sheet, shot** | **0.0019** | **0.0030** | **0.0019** | **0.0020** |
| Phase 0 sheet 1, unshot | 0.0019 | 0.0011 | 0.0023 | 0.0009 |
| Phase 0 sheet 2, unshot | 0.0021 | 0.0013 | 0.0021 | 0.0012 |
| Phase 0 sheet 3, unshot | 0.0021 | 0.0010 | 0.0016 | 0.0008 |

**The holed half is not worse. On an unshot sheet the same half, the top row and the sighter row at the foot, is already the worse half, and Alan's holed half reads what an unshot sheet's does.** His excess is entirely in the clean bulls 11 to 25: 0.0030 against 0.0010 to 0.0013 under the surface fit, 0.0020 against 0.0008 to 0.0012 after the warp.

**So shooting and handling around the holes is ruled out as the cause of the leftover.** What is left sits in the lower middle of the sheet, where there are no holes, which is where the scan's smooth vertical drift ran, dy rising to +0.0046 in on the fourth row. Printer feed and a sheet not lying flat on the platen across its lower half both fit; the holes do not. **The weekend's scan of the clean sheet stays the experiment, not a confirmation**: if it reads like a Phase 0 sheet the cause was the shot sheet's shape on the platen, and if it reads like this one it is the printer.

### Entry 98 section 2: the snap is sized in sheet units

**The snap's reach is now two terms, as the entry specifies.** One hole's extent in sheet units: the calibre's diameter where one is named, otherwise the median diameter the detector measured the sheet's own holes at, otherwise a nominal .30 hole. And a pointing tolerance of four screen pixels on top, because a finger misses by pixels whatever the zoom. Only with no scale at all, where there are no sheet units, does it fall back to screen pixels. **A test zooms in by four and the reach moves by less than the pointing tolerance**, where a screen-pixel reach would have shrunk to a quarter.

**What else on the canvas was in the wrong units, as entry 98 asks:**

| Quantity | Was | Now |
|---|---|---|
| The snap's reach | screen pixels without a calibre | one hole in sheet units plus 4 screen pixels |
| Clicking a shot to select it | within 18 screen pixels | within the drawn ring, which is true size, or 18 pixels when the ring is small on screen. Zoomed in, a click on a hole's rim missed it |
| Clicking a bull to reassign to it | within 54 screen pixels | within half the distance to its nearest bull, the bull's own share of the sheet |
| Drawing minimums, the alert ring's gap, a mark's stroke | screen pixels | unchanged: legibility on screen is a screen quantity |

### Entry 99: the parametric editor, and the visual designer deferred on its merits

**The editor is in the print screen, behind "Design your own sheet"**, because designing a sheet is for printing it. It takes the page, columns and rows, the spacing, the ring, the sighters and a load block, and a design that passes its checks becomes the selected sheet, so the preview, Save PDF and Print are the library's own.

**It places a sheet by the rule the library was laid out with**, `tools/layout/layout.py`'s solver ported to C#, and finishes it with the code that finishes a built-in sheet. **The proof is a test that rebuilds ten built-in sheets from their parameters alone and gets back every bull and every marker exactly**: the letter, A4, tabloid and A3 multi-bull sheets, with and without sighters and load blocks. Two of them needed the library's own fallbacks, now written into the editor rather than rediscovered: GL-CF25-LTR declares a sighter gap of 454 dmm because the conventional 456 drops the marker line below the sighters off the page, and GL-CF25-LTR-D carries its two codes at the top because four leave no room above a load block. The editor tries the conventional gap first and closes it a step at a time until the sheet validates, declaring the gap it used, and with a load block tries two codes when four do not fit.

**The three checks of entry 99 section 4, in entry 100 section 3's voice:**
- **Page fit and the format's own validation refuse**, in red, with the reason: "9 rows 1.50 in apart with a sighter row and a load block need 7.19 in more height than a letter page has. Fewer rows, a closer spacing or a larger page would fit."
- **Markers:** fewer than 9, the fewest any built-in sheet carries, is refused; fewer than 16 is a warning with the count and what an oblique photograph leaves of it.
- **Spacing against the stated group is a warning, never a refusal**, from entry 56's closed form, which a test reproduces from the formula: "A rifle shooting 1 MOA five-shot groups at 100 yd has a sigma of about 0.34 in, so 1.00 in between bulls is 2.9 sigma: about one shot in 4 (26.58 percent) would land nearer a neighbouring bull than its own. Six sigma is where that falls to one in 185." The number, what it means, and nothing about whether to proceed.

**The visual designer stays deferred**, now for its own reason: every built-in sheet is a grid, the format already carries arbitrarily placed bulls for the day something needs them, and a canvas is a large screen for a case nobody has asked for. `DESIGN.md` section 3 and the README carry it with entry 99's citation, and the scope-to-phase test passes.

### Entry 100: the form's colours are token roles

**Six new roles in every palette, added before the form was built:** field background, field border, focus ring, disabled, warning text and error text. Dark and light have their values; high contrast derives them from the dark ones like everything else in it. **Warning text is amber and error text red in all three**, the meanings entry 93 fixed, and a test holds them to those hues.

**The contrast tests cover them.** Every text colour, warning and error included, reaches 4.5:1 on fields as well as panels, 7:1 in high contrast. Field borders, the focus ring and disabled text reach WCAG's 3:1 for interface edges against the field and the panel, 4.5:1 in high contrast. The text boxes and dropdowns across the application now take their colours from these roles rather than borrowing the panel's.

**The window rehearsal ran after the editor landed and after every other change here, and it holds at 10 presses and no taps.** The Core rehearsal holds at 15 and none.

### Entry 98 section 5, first item: each stage's picture

**The timeline now shows each stage's own picture, as `DESIGN.md` section 19 describes.** At the fiducial stage the markers it found light up, outlined in teal. At registration each corner is ringed by how far the fit left it, twenty times true size so a tenth of a millimetre can be seen, red where the fit threw the corner out. **At the difference stage the photograph gives way to the residual**, drawn as paper and ink, so the printed artwork has vanished and the holes are what is left.

**Only the residual needed keeping; the rest was already in the analysis's result.** The detector keeps it only when asked, and only the marking screen asks. **A test runs the same sheet both ways: the batch run carries no residual, the interactive one does.** The pictures appear when the analysis finishes rather than stage by stage while it runs; each stage's record still lands live.

**Tests:** Core and App suites both pass, none skipped.

---

## Entries 131, 132 and 133

### Entry 132 section 1, release notes a person can read

Alan read the notes for `v0.2.0-nightly.26` and they told him nothing, because they were commit subjects: "Entry 130 item 3.3: doubt travels with the number". That is written for the log.

Notes are now built from `Release-note:` trailers alone, with a kind of new, fixed or changed, and nothing is guessed from a subject line. A commit with nothing a person would notice carries no trailer and appears only in a count, because a notes fold or a test is noise that hides the real notes.

**The script refuses to publish bad notes**, naming the commit each time: a note that is only a reference, begins with "Entry", is under eight words, or uses words that mean nothing to a shooter (folded, gate record, recorder, harness, manifest, and the rest). Checked against seven notes, six deliberately bad and one good, and it refused all six for the right reason.

Nightlies 18 to 26 went out with unreadable notes, so the next nightly opens with ten hand-written lines saying what was actually in them. The published releases are not edited.

### Entry 132 section 2, and the largest single win of the night

Measuring for entry 133 turned up something worth more than what was being measured.

**100.7 MB of the shipped build was debug symbols**, and 100 MB of that was two files: `libSkiaSharp.pdb` at 80.1 MB and `libHarfBuzzSharp.pdb` at 19.9 MB. Native symbols for Skia and HarfBuzz. Nothing at runtime reads them, no crash report this project writes can use them, and no user of GroupLab will ever open them in a debugger.

| | |
|---|---|
| installed before | 332.6 MB |
| installed after | **204.3 MB** |
| saved | 128.3 MB, a 39 percent cut |

Proved rather than assumed: the trimmed build validates a definition and analyses the 25 shot sample to 25 holes, 25 shots, mean radius 0.232 in, which is what it read before. GroupLab's own symbols stay, 0.4 MB, because a crash record naming a line in GroupLab is worth having.

### Entry 133, the light installer, measured

| | unpacked | zip | installer |
|---|---|---|---|
| self-contained, before tonight | 332.6 MB | about 102 MB | 97.3 MB |
| framework-dependent | 227.9 MB | 78.2 MB | not built |
| **self-contained, after the symbol fix** | **204.3 MB** | | |

**The self-contained build is now smaller than the framework-dependent one was**, and it carries its own runtime. Of the framework-dependent build, Avalonia and Skia are 121.4 MB and OpenCV 93.6 MB: the runtime was never what made GroupLab large, and leaving it out fixes none of that.

Question 36 recommends **not yet**, with the per-user runtime route judged workable but a new failure surface on somebody's first run, unpatched by anybody afterwards, and doubling the updater's cases in a week when the real update test found three separate defects in the single-package one.

### Entry 131, what was built

The interface overhaul is the largest thing any entry has asked for. Five pieces were built, all at the model level with tests, and the screens that use them are the work still outstanding.

| section | built |
|---|---|
| 9, confirmations | `Toaster`: one component, never a dialog, Undo running the session's own undo so the button and Ctrl+Z cannot disagree. Exclude, restore, not a shot, unassign and delete all confirm. |
| 4, explaining every figure | Ten figures in two or three plain sentences, with `docs/GLOSSARY.md` generated from the same list so the screen and the page cannot drift. |
| 3.1, four units | MOA, mil, inches and centimetres at once, the scope's unit leading where the rifle records one, and the clicks spelled out. |
| 3.2, metric toggle | A view of the figures that never touches what is stored. |
| 6.1, mean radius on a scale | With the marks attributed to the podcast they came from, and a caveat that scales with the shot count. |

Three of those carry a judgement worth stating plainly.

**Undo belongs after the change, not before it.** A confirmation dialog asks whether you are sure about something you cannot yet see. A toast lets you do it, look at what it did, and undo it having seen the result, which is the order a person actually decides in.

**Every explanation says what the sample size does to the figure**, and a test holds that rather than trusting whoever writes the next one. The commonest mistake in group shooting is treating one five shot group as a measurement of a rifle, and a figure shown without that caveat invites it. The extreme spread's explanation says it uses two shots and throws the rest away; the zero correction's says that dialling from a handful of shots can move you further from where you want to be.

**The scale's marks are somebody else's rules of thumb and every path through the code says so.** This project's argument is that a handful of shots does not support a verdict. Handing one down using numbers quoted on a podcast would contradict everything else it says, so the attribution is in the caveat at every shot count, and a five shot group is told the same rifle could land anywhere across several marks.

### Entry 131 sections 2, 5 and 7, built after the first pass

Three more sections landed, all as models in Core with tests, so the screens that draw them have nothing left to decide.

**Section 2, the shot editor.** `ShotEditor` is the popover's contents, its actions, its keyboard map and what each one does to the marking. Three judgements in it are held by tests rather than left to a comment.

- **A flyer and an exclusion are two marks, not one.** Section 2 asks for a flyer "kept but called out" beside a shot left out of the figures, and the whole difference is that pointing at a shot must not shrink the group. A test measures a group before and after a flyer is called and requires every figure to be identical.
- **A shot marked a sighter by hand leaves the group**, or the mark would be decoration and somebody would think they had set a shot aside when they had not. It leaves the sighters' own view alone: the flag is cleared on the way in, because otherwise the one screen built to measure sighters would set them all aside and show nothing.
- **An arrow key moves a hundredth of an inch on the paper, not a pixel.** A `ScaleReference` maps pixels to paper and not back, and the three kinds invert quite differently, so the step is converted by sampling the map one pixel each way: exact for a length and a rectangle, right to first order for a registered sheet, and falling back to pixels rather than to a guess where the fit is degenerate.

A hole size set by hand is kept beside what the detector measured rather than over it, because the measurement is evidence about the image and a person disagreeing with it is a second opinion.

**Section 5, the right-hand panel.** `AnalysisPanel` is four blocks, each with a heading, a table of rows and one headline figure. The rule worth having is a test: **no number reaches the panel without an explanation behind its ?**. Section 4.1 asks for that, and a promise like it decays the first time somebody adds a row in a hurry; now a figure with nothing to say about itself fails `EveryFigureCanExplainItself` instead of quietly appearing with no **?**. A figure too small a group to quote keeps its row with the reason in it, so the panel does not change shape as shots are added.

**Section 7, the equipment records, and a defect they turned up.**

The three record types were filled out as section 7 describes them, with one field list that the form, the autocomplete and the tests all read, so a field cannot be added to a record and forgotten on the screen.

Writing the round-trip test found this:

> **The record book saved a rifle's name, click value and click unit, and nothing else.**

A sight height, zero distance and twist typed into the ballistics page, and every one of a load's muzzle velocity, velocity SD, ballistic coefficient, drag model and bullet figures, were dropped the moment the book was saved. Nothing failed and nothing said so. The page simply asked for them again at the next start, which reads as forgetfulness rather than as a bug, and is exactly the kind of thing a person stops reporting. Every field is written and read now, and the test requires a book read back to equal the book written.

A name already in use is now refused with a sentence naming the clash, because `RecordBook.With` replaces by name and saving under a forgotten name would have silently overwritten the record behind it.

### Entry 131 sections 3.3 and 6.3, and entry 132 section 2.2

**Section 3.3, the shot distance's unit.** It was a label showing whichever unit the Settings held, so somebody who thinks in metres but shoots at a hundred yard range had to change a global setting to type one number, or convert it in their head at the bench. It is a real choice beside the number now, and changing it rewrites the number rather than reading the old one as the new unit. Nothing stored moves: the distance is kept in inches whichever is chosen.

**Section 6.3, the calibre confirmed before Accept.** Tonight's scan re-run is the argument for this, and it is a strong one: naming the calibre is worth five holes on scan 4 and the edge shot on scan 6. GroupLab now reads a calibre from the holes it measured, puts back the 0.0202 in that paper closes behind a bullet, and offers it with a sentence saying what it cannot know. Under three holes it says it has too few rather than answering from one or two marks. **The guess is offered and never applied**, because using it silently would be GroupLab deciding a fact the shooter knows for certain on evidence that is only suggestive, and then measuring everything else against it. Accept is held on a sheet of bulls until the calibre is answered; a plain group marked by hand is not held, because there is nothing for the answer to change.

**And it reached the screen.** The marking panel shows the reading where no calibre is named, and Accept on a sheet of bulls is held until the question is answered. Clearing the box counts as an answer, because somebody marking a photograph of something that is not a GroupLab sheet means "no calibre"; the gate exists to stop Accept on a sheet nobody was asked about, not to force a number out of anyone. **Eight existing tests accepted without answering**, which is the behaviour change working rather than a problem with it.

**Entry 132 section 2.2, native libraries only for the platforms anything runs on.**

| build | before | after |
|---|---|---|
| Core tests | 685 MB | **262 MB** |
| App tests | 705 MB | **278 MB** |

A build with no runtime identifier was copying native assets for about thirty platforms: WebAssembly, Mac Catalyst, s390x, LoongArch, MIPS, RISC-V, the musl variants. That was 674 MB of a 685 MB test build. A little over 420 MB a folder, and entry 132 counts nine such folders on Alan's machine.

**Nothing that ships changes.** A published package names one runtime identifier and is self contained, so the SDK already gives it that one platform's assets: a self-contained win-x64 publish measures 205 MB with and without this, the same figure the symbol fix left it at. CI's Windows package, Linux tarball and macOS jobs are all green on it.

### What entry 131 did not get

Sections 1, 3.3, 6.2, 6.3, section 7's screen, 8 and 10: the mockups and the before and after renders, the shot distance unit dropdown, the zero offset picture, the calibre confirmation before Accept, the Equipment screen itself with the removal of the shared "rounds or components" box, the ballistics page and the comparison screen. None was started, so none is half built.

**Every model those screens need now exists and is tested**, so what is left is drawing rather than deciding. Section 1's checklist is still the part to do first, and the before renders it asks for do not exist yet for any screen except settings and library.

## A flake worth naming: temp files under load on this machine

Three different tests failed tonight, once each, and every one passed on its own immediately afterwards:

| test | what it said |
|---|---|
| `FolderVerbsTests.AFolderOfScansIsAnalysedOneLineEach` | failed once in a full run, passed alone |
| `BenchCoverageTests.EverythingThatCanBeMeasuredHasABenchCase` | `bench-25-shots.png` "used by another process" |
| `EndToEndTests.AnalyzeRecoversEveryShotPlacedOnARenderedSheetAndPoolsThem` | the same, on deleting `grouplab-end-to-end-<guid>.png` |

**All three happened while both suites were running at once on this machine, and all three are a file in `%TEMP%` that could not be opened or deleted at that moment.** The names carry a GUID, so it is not two tests choosing the same path: it is something outside the test holding a newly written file for a moment, which on Windows is usually the antivirus or the search indexer, and it only shows up when the machine is busy enough for that moment to matter.

It has never happened in CI, where the suites run in separate jobs on quieter machines.

**It is not a defect in GroupLab and it is not worth chasing as one, but it should stop wasting a session's time.** What I would do: give the tests one shared helper that retries a create or delete a few times over a second before giving up, and use it wherever a test writes into `%TEMP%`. That turns a confusing red into nothing at all, and it does not hide a real failure, because a file genuinely held open stays held for much longer than a second.

The counts either side of it: **Core 1193 passed with that one flake, 1184 passed clean on the run before it; App 145 passed clean.**

## The Turnstile secret is not a morning step, and it should not be

Entry 129 section 2.2 asks for a script Alan runs himself, once, to put the Cloudflare Turnstile secret on the server, and for the exact command to be in the report. The script is written and sits in `website/server/grouplab-set-turnstile-secret`.

**It is not installed by anything, and it should not be run yet.** `install.py` installs the sync script, its two systemd units and the public key; it does not install the Turnstile script, because entry 129 section 7.1 scopes that to entry 129's own server work, and that work is not built. Nor is the receiver that would read the secret, nor is PHP enabled for grouplab.org.

So putting the secret on the server in the morning would be putting a credential somewhere nothing can use it, on a host where the folder it belongs in does not exist and the permissions it needs cannot yet be set correctly. **The right order is the reverse**: build and install the receivers, then set the secret, then send a real submission through the page. That keeps the secret's life on the server as short as it can be before it is doing something.

## The end of the night, proved rather than asserted

A self-contained win-x64 package published from the last commit of the night, carrying every change in it, read the published sample scan:

```
25 holes inside the registered sheet, 9 candidates rejected
25 shots pooled about their own bulls, mean radius 0.232 in
mean radius 0.232 in, 94.9% interval 0.193 to 0.289 in
```

**Identical to the figure this sheet has read all along**, after a night that took 128 MB of debug symbols out of the package, 423 MB of native libraries out of every build, rewired how holes are assigned to bulls, and added four marks to a shot. That is the point of measuring it from the package rather than from a test: the tests say the code is right, and this says the thing a person would download is the same thing.

## Leaving a sheet, and starting one on purpose

Section 1's reset is right and is also how ten minutes of correcting marks disappears without anybody being asked. **Every way out of a sheet now goes through one question**: opening an image, opening a marking, opening a saved session, and New target. Where the sheet holds edits that are not in a saved session, a row appears at the top of the panel, beside the crash banner: *This target has edits that are not saved. Save it, discard it, or stay here?* Cancel puts everything back exactly as it was; it is a row rather than a dialog, which is entry 131 section 9's rule.

"Unsaved work" is exact rather than a guess. The window keeps the marking's own file as it stood at the last save and compares: a save followed by an undo and a redo is not unsaved work, and moving one mark is.

**New target (Ctrl+N)** is in the header menu and clears the sheet exactly as opening a new image does, with nothing opened afterwards, and the toast carries Undo, so the sheet that was there comes back whole. The image is not reopened with it, and the toast says so.

## The "possibly two holes" check no longer cries wolf

Section 3.2, and the thing on Alan's screenshot that mattered most. If most of the marks on a sheet read as more than one hole, the assumption is wrong, not the holes. The queue now raises **one** item:

> 15 of the 15 marks on this sheet read as more than one hole, which usually means the calibre is wrong rather than that you fired twice at every bull. Check what you were shooting.

"Most" is three marks at minimum and three fifths of the sheet: three is a pattern and one is a mark, and a person who fired three doubles among fifteen still deserves to be told which three, so a few flagged marks are still raised one by one. Answering the one item settles the lot.

Section 3.1's other half was already true and is now held by a test: with no calibre the size comes from the sheet's own round marks, which is what "about twice its neighbours" means in practice. Section 1 is what stops a calibre nobody confirmed for this sheet reaching it.

Four generated sheets hold it, at 150 dpi on GL-CF25-LTR, with the detector doing the judging:

| sheet | flagged by the detector | review items |
|---|---|---|
| 15 single 6.5 mm holes, no calibre | 0 | 0 |
| the same, calibre stated 0.7 times too small | 15 | **1**, about the calibre |
| 15 singles with 3 marks flagged by hand | 3 | 3, one each |
| 14 singles and one merged pair, no calibre | 1 | 1, on the pair |

## "You fired 25" was never said by anybody

Running Alan's own photograph turned up something the entry did not have. The sentence he read as the last sheet's rounds fired following him across, *"You fired 25 and 15 are marked"*, was **the sheet's own arithmetic**: GL-CF25-LTR has twenty five bulls, nothing had been typed anywhere, and `ReviewQueue.Expected` falls back to the bull count. The count was not carried over at all. What was wrong was the wording, which told him he had said something he had not, and his reading of it was the reasonable one.

It now says what it means:

> This sheet takes 25 shots and 15 are marked. Nobody has said how many rounds were fired. Nothing is marked on bulls 16, 17, 18, 19, 20, 21, 22, 23 and 2 more.

A number somebody typed still reads "You fired 25". The shortfall itself is untouched: entry 130 section 2b.2 put it there and it is right.

## Alan's evening, recreated

Section 4's test analyses a generated twenty five shot sheet with a calibre, rounds fired and distance set, then opens and analyses a fifteen shot sheet on top of it, and holds that no count, calibre, distance, review item or mark from the first reaches the second. `NewTargetResetsTests` holds the same rule field by field by reading the state's own properties; this one holds it end to end, because the fault was never in one field.

## The photograph itself, before and after

`20260920_165624.jpg` from the range folder, read only, nothing committed, no metadata read. Registration 34 of 34 markers at RMS 0.0047 in; 15 holes found; mean radius 0.232 in, extreme spread 0.787 in.

| | marks flagged | review items | what the queue says |
|---|---|---|---|
| **Before**: the previous sheet's calibre carried over | 15 of 15 | **16** | fifteen "Possibly two holes", plus the count |
| **After**: no calibre, which is now what happens | 1 of 15 | **2** | the count, and one mark at 1.4 holes worth looking at |

The one remaining flag is shot 15 at 0.457 in, the widest mark on the sheet: a fair thing to raise.

**And a finding that outlives this entry.** Stating the *correct* calibre on this photograph flags all fifteen as well, at 1.42 to 2.61 holes' area. The measured diameters run 0.302 to 0.457 in for a 6.5 mm bullet, one and a half times what `docs/SCAN-MEASUREMENTS.md` measured on scans, and the registration and the group figures say the scale is right, so the holes really do photograph that wide. Entry 82's reference sizes are calibrated on scans with a white lid behind the sheet; a photographed hole leaves a wider residual. That is **question 38**, with the numbers and what I would do about it. Nobody is flooded in the meantime, because one item is raised rather than fifteen.


# Sign the bytes that were published: entry 139

## What went wrong, in one sentence

A build verifies its update information by serialising the record it read the file into and checking those bytes against the signature, so a build that meets a field it does not know drops that field, checks different bytes, and refuses the update.

Entry 138 section 5 added one field. Nightly 42 went out with it. Nightly 37 answered:

```
update.check result=Refused refusal=BadSignature
```

Every build already installed, unable to update itself at all. The revert is `6545cf2`. This is the fix.

## The second file

Beside `update-manifest.json`, every build now publishes `update-manifest-2.json`:

```json
{
  "algorithm": "ecdsa-p256-sha256",
  "payload": "eyJtYW5pZmVzdCI6MSwidmVyc2lvbiI6IjAuMi4wLW5pZ2h0bHkuNDQiLC ...",
  "signature": "MEYCIQCVTB3oor8tKbnZzG1C ..."
}
```

The payload is the exact bytes that were signed, carried base64. A build decodes them, verifies the signature **over what arrived**, and only then reads them as JSON. A field it has never heard of is a field it ignores, because it was never asked to reproduce anything.

The order is the point: verify, then parse. Nothing between the signing machine and the machine installing the update ever serialises the manifest again.

## The first file is frozen, and cannot be unfrozen by accident

The first format still cannot gain a field while any build that reads only it may be installed. That is now a property of the code rather than a comment asking people to be careful:

- `UpdateManifest.Legacy()` is the shape the first format can carry, and it is the only shape `UpdateSignature.Sign` will sign. Handing it a manifest with the new field produces a first-format file without it.
- `grouplab update-manifest` refuses to write anything if what it just signed for the first format carries a field it should not.
- `PublishedManifestTests.TheFirstFormatsSignedBytesAreHeldToARecordedString` holds those bytes to a literal string in the test file. Changing them means editing that string, which means somebody looked.

`docs/UPDATES.md` records when the first file may go: a beta or release published from the second format, every nightly up to 44 aged out of the thirty the workflow keeps, and nothing having asked for `update-manifest.json` in sixty days, which GitHub's per-asset download count on the rolling release answers.

## What the tests hold

| test | what it would catch |
|---|---|
| A manifest with four unknown fields verifies and parses, and the same payload refused under the first format | the whole fault, from both sides |
| A byte changed at six places in the payload, and a signature from another key | a payload that can be edited after signing |
| The first format's signed bytes against a recorded string | anything that would change what installed builds have to reproduce |
| Nightly 37's verification, pinned as its own copy, against a manifest generated today | the day a build in the field stops being able to update itself |
| The second address is asked for first, the first is the fallback | a build published before the second format existed being stranded |

The pinned nightly 37 copy is deliberately a separate record with only the seven fields that build knew, and its own re-serialise-then-verify, because the fault was precisely a build meeting a field it did not know.

## And entry 138 section 5 lands with it

The per-version notes finally have somewhere safe to travel. `scripts/release-notes.py --versions versions.json <version>` writes the last ten published builds' own notes, the nightly passes it to `update-manifest --versions`, and it rides in the second file only. Somebody on nightly 31 offered nightly 40 now reads "9 builds are new to you, newest first" with each build's own changes under its own heading, instead of one build's worth of notes for nine builds of work.

## Proved, from a build that had never heard of the second format

`scripts/Test-RealUpdate.ps1` ran from the installed nightly 44 to nightly 49, pressing the real buttons on the real window.

That pairing is the proof rather than a formality. **Nightly 44 predates this entry**, so it reads only the first format and verifies it the old way, by re-serialising what it read. **Nightly 49 is the first build to publish both files.** If the second file had changed anything about the first, or if the first had gained so much as a null field, nightly 44 would have answered `BadSignature`, which is exactly what nightly 37 did to nightly 42.

```
08:58:34 update.check   result=Offered refusal=None
08:58:39 update.install version=0.2.0-nightly.49 silent=yes
08:58:47 app.start      version=0.2.0-nightly.49+b5ea04c
08:58:48 update.arrived from=0.2.0-nightly.44 to=0.2.0-nightly.49
```

Offered, downloaded, installed, and back on its own fifteen seconds later, with no crash record. The build now installed reads the second file first.


# The ballistics page in either system, and the loads compared with their velocities

Entry 131 sections 8 and 10, the last two pieces of the entry 135 queue's items 4 and 5.

## Imperial and metric, on the page where it matters

Alan's own calculator has the toggle at the top, and the ballistics page is the one screen where a person types physical quantities rather than reading them. It now has one, and it moves the **whole application's** units: a page in one system beside a panel in another is how somebody reads a number as the wrong thing.

**The labels are the easy half.** A toggle that renames `ft/s` to `m/s` and leaves 2850 in the box has quietly turned a rifle into something travelling at Mach 8, and the solver will answer in perfect detail about it. So the values are rewritten as the toggle moves, from the imperial figures everything is stored in:

| field | imperial | metric |
|---|---|---|
| sight height, twist, bullet length and diameter | in | mm |
| muzzle velocity and its SD | ft/s | m/s |
| temperature | °F | °C |
| station pressure | inHg | hPa |
| altitude | ft | m |
| crosswind uncertainty | mph | km/h |
| zero distance, dope table, projection | yd | m |
| bullet weight | gr | gr |

Grains stay grains, because a reloader weighs in them whatever else they measure in.

**Nothing stored changes.** The records hold inches and feet a second on both sides, and the solver works in them, so 869 m/s typed by one person and 2850 ft/s typed by another are the same load in the same file. `BallisticUnitsTests` types on one side and reads the record on the other.

**And a defect it turned up.** Every unit-bearing label on that page was built once, at startup, from the units in force then. Changing the units in Settings left `Zero distance, yd` saying yd over a box holding metres. The labels are now written rather than built, and `SetUnits` writes them wherever the change was made.

## Velocity and SD on the compare cards

Each card in Compare loads now carries what the load was chronographed at, from the record book, under the shot count: `2850 ft/s, SD 11 ft/s from 24 readings, 20 September 2026`. Where GroupLab worked the SD out from readings it says so, because a measured spread and a typed one are not the same claim; where the book has no velocity the card says nothing rather than a dash.

## One test was taking two minutes because of the group it used

`NewTargetTests` put its five shots on a straight line, which is degenerate for the shape tests, and the resampling behind them ran for about a hundred seconds a test: five tests took 520 seconds where the same five on an ordinary group take 58. That is worth writing down because the fault is invisible: the test passes either way, and it is only the clock that says anything is wrong. A group in a test is a scatter unless the test is about a line.


# What a hole measures in a photograph: question 38 answered by measuring

Alan asked me to measure the photographed hole size factor myself, from `C:\Dev\grouplab-range-2026-09-20`, with his known calibres as ground truth, and to report rather than force one number if it varies. It varies, and the reason it varies means **there is no factor to measure**.

Pixels only. No location, no timestamp and no other metadata was read, printed or logged. Nothing from that folder is committed: what follows is the derived numbers and nothing else.

## The measurement

Nine photographs of four sheets, and the 600 dpi scans of those same four sheets as the control. The ratio is the median measured hole diameter over the bullet diameter Alan says was fired.

| sheet | bullet | scan | photographs | holes |
|---|---|---|---|---|
| .22 LR block | 0.224 in | **0.758** | 1.069, 1.077 | 19 scanned, 48 photographed |
| 6 ARC block | 0.243 in | **0.923** | 1.256, 1.333, 1.360 | 20 scanned, 58 photographed |
| 6.5 Creedmoor, 25 shots | 0.264 in | **0.949** | 0.898 | 25 scanned, 25 photographed |
| 6.5 Creedmoor, 15 shots | 0.264 in | **0.937** | 1.452, 1.405, 1.449 | 14 scanned, 45 photographed |

176 holes photographed, 78 scanned. Every sheet registered from its own markers, at 0.0042 to 0.0060 in RMS on the photographs and 0.0023 to 0.0026 in on the scans, so none of this is a registration failure.

**The scans agree with the constant.** `HoleToCalibre` is 0.945; the three centrefire scans read 0.923 to 0.949, and their spread within a sheet is 0.007 to 0.010 in.

**The photographs do not agree with anything.** They run 0.90 to 1.45, a factor of 1.6 between sheets, and their spread within a single photograph is 0.026 to 0.085 in, three to ten times the scans'.

## It is not resolution, and it is not angle

The obvious explanation is that a photograph is lower resolution, so blur inflates the blob. The numbers refuse it:

| photograph | pixels per inch at the sheet | ratio |
|---|---|---|
| 6.5 Creedmoor 15-shot, square on and close | 278 | 1.452 |
| the same sheet, further away | 177 | 1.449 |
| 6.5 Creedmoor 25-shot | 177 | 0.898 |
| 6 ARC | 170 | 1.256 |
| 6 ARC, closer | 291 | 1.360 |

Two photographs of one sheet at 278 and 177 pixels per inch give 1.452 and 1.449. Two photographs of **different** sheets at the same 177 give 0.898 and 1.449. Resolution moves the ratio by about 0.10 at most; the sheet moves it by 0.55.

Nor is it obliqueness: the 15-shot sheet's worst reading, 1.452, is the one photograph in the set with all 34 markers found and the lowest residual, which is the squarest and cleanest of them.

**What it is.** Two 6.5 Creedmoor sheets, same rifle, same load, same day, scanned at 0.949 and 0.937, photograph at 0.898 and 1.45. The difference between them is not the hole; it is the photograph. The 25-shot sheet was photographed at 15:33 and the 15-shot sheet at 16:56, an hour and twenty minutes later with the sun that much lower. (I first wrote "three hours lower" here, which is simply wrong arithmetic on those two times; the planning session caught it when checking a draft against this file.) A hole photographed in low, raking light carries its own shadow, and the dark blob that render-and-difference measures is the hole plus that shadow. A scan has a lamp at a fixed angle and a white lid behind the paper, which is why its numbers are steady.

So the quantity is not "how much larger a hole is in a photograph". It is "how much shadow was in that photograph", and no constant can carry it.

## What the evidence supports instead

**The sheet's own marks, which GroupLab already has.** On every one of the thirteen sheets measured here, photographs and scans alike, the size reference came out as `HoleSizeSource.Sheet`: the quarter-point of the sheet's own round marks. Judged against that, the number of marks flagged "possibly two holes" was:

- **zero on ten of the thirteen**, and **one on the other three**.

Including the photograph Alan met, where a stated calibre flagged all fifteen. The sheet's own marks are self-calibrating: whatever the light did to the holes, it did to all of them.

**So the recommendation is a change of order, not a new constant.** Where a sheet has enough round marks to speak for itself, its own marks should be the flag's reference, and the stated calibre the fallback rather than the first choice. Today `SizeReference` takes the calibre first and the sheet second. On a scan this costs nothing, because the two agree within five percent. On a photograph it is the difference between sixteen review items and one.

The calibre keeps its other two jobs, where it is still the better answer: the smallest-hole gate, and the split veto.

**I have not built it.** Entry 82 is the planning session's design and this reverses its first rule, so it belongs in an entry rather than in a quiet change of my own. Entry 140 section 3.2's one-question guard is the backstop meanwhile, and nobody is flooded.

**And a second finding, smaller but real.** The .22 LR scan reads 0.758 where the three centrefire scans read 0.92 to 0.95. A .22 hole measures proportionally less of its bullet than a centrefire hole does, so the single constant is not calibre-independent either. It was measured on .264, .308 and .338 only, which is why nobody had seen this. It matters for the size gate on small calibres, which entry 130 section 2b.3 has already been round once. I have changed nothing: that needs its own measurement across more small-calibre sheets than the one here.


# The calibre guess lands on something somebody shoots

Alan's requirement of 2026-09-22, and entry 141 section 3.3.

## The lists

The guess now snaps to one of Alan's own diameters and nothing else:

- **Rifle**: .172, .204, .222 (the rimfire, named `22LR`), .224, .243, .257, .264, .277, .284, .308, .338, .375, .416, .458, .510
- **Pistol**: .312, .355, .400, .410, .430, .451, .500

Each is shown in both units, as everything else here is. Reading 0.2371 in off a sheet and offering "0.237 in" is arithmetic dressed as knowledge, because nobody loads a .237; offering .243 with .224 beside it is a question a shooter answers in a second.

**The list limits the guess, not the person.** Any diameter at all can still be typed, and the refusals for designations typed as diameters (6.5 mm, 7.62 mm, .38) are untouched.

## Which list, and when there is nothing to guess

The Equipment screen's rifle form gains **Rifle or pistol**, defaulting to rifle, so every record made before it existed is one. The guess reads it from the rifle the sheet names.

**A diameter on the chosen load wins outright and nothing is guessed.** The shooter wrote it down; nothing read off paper beats that.

## Never a calibre the evidence cannot support

Where the holes cannot tell two listed diameters apart, both are offered, the likelier first, and the sentence says why. "Cannot tell apart" is two standard errors of the estimate, and the estimate has two sources of error:

1. **The sheet's own spread**, the scatter of its single marks, which is what Alan asked for: a sheet that measures consistently earns a narrow window and a ragged one is told it is ragged. Marks flagged as possibly two holes are left out, since a merged pair measures like nothing on the sheet.
2. **The estimator's own error, about 0.007 in**, which no sheet can see. Putting back a fixed allowance for the paper assumes this paper, backing and velocity close a hole like the ones the allowance was measured on. On the four range scans of known calibre the estimate landed 0.0016, 0.0035 and 0.0068 in high on the three centrefire sheets.

The second term is the larger one on a tidy sheet, and it is why .451 and .458 are offered together however well a sheet measures. **The neighbours are drawn from both lists even though the preselection respects the firearm type**: most of the pairs that cannot be separated straddle the lists (.308 against .312, .451 against .458, .500 against .510), and hiding half of one because a record says "rifle" would assert a calibre the evidence does not separate.

## What it does on the four range scans

Read only, nothing committed.

| scan | known | preselected | offered beside it |
|---|---|---|---|
| 1 | 6.5 Creedmoor, .264 | **.264** | .277, .257 |
| 3 | 6.5 Creedmoor, .264 | .277 | **.264**, .284, .257 |
| 4 | .22 LR, .222 | .204 | .172 |
| 5 | 6 ARC, .243 | **.243** | .257 |

Two of four preselect the right calibre, three of four offer it, and every one of them is marked rough with its neighbours beside it, so nothing is asserted.

**Scan 4 is a miss and it is the rimfire one.** Its holes measure 0.170 in for a 0.222 in bullet, a deficit of 0.052 where the three centrefire sheets give 0.013 to 0.019. Question 38's second finding is exactly this: the hole-to-bullet ratio is not calibre-independent, and it was measured on .264, .308 and .338 only. Nothing has been fudged to cover it. The guess is offered and never applied, so a person on that sheet types .222 and moves on, and the fix is a measurement across more rimfire sheets than the one there is.

## A photograph guesses nothing

Question 38 measured photographs of sheets of known calibre at 0.90 to 1.45 times the bullet, sheet by sheet. So from a photograph the confirmation step preselects nothing, offers the list, and says plainly that a hole photographed in low light reads far wider than the same hole scanned.


# Printing held by tests that need no printer

Entry 141 section 1.1, and the first thing done tonight because Alan prints his targets before he goes shooting and that cannot slip.

**The two tests that covered printing best both need a real printer driver.** `PrintedItemsTests` prints to "Microsoft Print to PDF" and `PrintedSizeTests` needs it installed, so both skip on CI and both skipped here the moment the feature was off. Printing was therefore held, on the machine that matters, by nothing that runs automatically.

`PrintGateTests` runs anywhere. It renders every sheet in the library through `TargetRenderer.Render`, which is the call `PrintWindow.SavePdf` makes, and then reads the PDF that came out: 88 checks over 22 sheets, in two seconds.

| what it holds | how it holds it |
|---|---|
| Every sheet renders with no error, blank **and** filled | the diagnostics, plus one page for every tile |
| The page is the size the sheet asks for | the `/MediaBox` parsed out of the PDF bytes, letter being 612 by 792 points |
| The markers, the codes and the bulls are on the page | the scene's own layers, with the codes checked against what the definition declares |
| **The printed scale is exact** | the two furthest markers the definition itself places, measured back out of the PDF |

## The scale check is the one that matters

A sheet that prints at 96 percent still looks perfect. Every measurement taken from it is then wrong by four percent, and nothing on the paper says so.

So the PDF is rasterised by PDFium at 300 dpi, the markers are found by the same detector that reads a scan, and the distance between the two furthest apart is compared with the distance the definition declares. The two furthest, because a long distance is where a scale error shows and a short one hides it.

Across the library it measures 8 to 10 inches and agrees to **0.0000 to 0.0006 in**, against the 0.005 in entry 141 section 1.1 asks for:

```
GL-LR300-T    markers 0 and 16   8.0000 in on the sheet, 8.0000 in in the PDF
GL-RF36-LTR   markers 0 and 53   8.7633 in on the sheet, 8.7633 in in the PDF
GL-RF25-A4    markers 0 and 41   7.9625 in on the sheet, 7.9631 in in the PDF
GL-LR300-TA4  markers 0 and 22  10.1980 in on the sheet, 10.1983 in in the PDF
```

**It cannot pass by doing nothing.** A definition that stores no markers fails it by name rather than returning quietly, which is the way a measurement test usually rots.


# Fewer CI runs, and a nightly that cannot be missed

Entry 141 section 2, workflow files only. No repository setting was touched.

## One run per branch, and the newest wins

`build and test` now has a concurrency group of workflow and branch, with cancel in progress. Two pushes a few minutes apart used to build the same code twice over to the end.

It is not only waste. On 2026-09-22 a `windows-latest` runner was lost after 52 minutes on a run that a later push had already made pointless, and the red it left on main had nothing whatever to do with the code: the identical tree passed on the other branch in the same minutes. A cancelled run says "cancelled", which is a word nobody has to investigate.

**And pushing goes to main only from now on.** Every push used to go to `phase-1` and `main` at the same commit, so everything ran twice. Nothing depends on `phase-1` any more: the nightly already listens to main alone, the website workflow runs from main, and `main` is force-set to the tested commit before every push. The branch stays where it is as a record; nothing new goes to it.

## A nightly that a quiet day cannot lose

The `workflow_run` path only fires when a push happens. A day with no push produces no build, and a day whose runs were cancelled produces none either, which is how a nightly quietly stops existing without anybody noticing.

`nightly.yml` now also runs on a schedule at 12:00 UTC. It builds **the newest commit on main that `build and test` passed on**, and it does nothing at all where that commit already carries a per-build tag: a schedule exists to catch a day with no push, not to publish the same code twice. Both cases say so in the run summary rather than failing.

The commit is now worked out once, in a step of its own, and every later job takes it from there. On the `workflow_run` path it is the commit that was tested, exactly as before; on the schedule it is the one the schedule found. The freshness check of entry 123, which stops a nightly publishing a commit main has moved past, stays on the `workflow_run` path where it belongs.

`ReleaseAssetTests` holds all of it: the schedule's cron, that the resolved commit is what packaging and publishing use, that `head_branch` appears nowhere, and the concurrency group on both workflows.


# The sheet's own marks decide what is one hole and what is two

Entry 141 section 4, approving question 38's recommendation. This reverses the first rule of entry 82.

## The rule

**Where a sheet has twelve or more round marks and they do not fall into two clear sizes, its own marks are the reference, and a stated calibre is the fallback.** Below that, the stated calibre is used, and with no calibre the ladder of entry 82 stands as it was.

Twelve is the number the sheet was already trusted at. The thirteen images of question 38 ran from 14 to 25 round marks and every one produced a usable reference from its own marks, so the measurement supports 14 and above directly; 12 is inherited from entry 82's own line, and below 12 there is no evidence either way, which is exactly why a stated calibre is what is used there.

**The reference survives the doubles it is judging** because it is the quarter-point, not a mean. A merged pair measures larger than anything else on the sheet, so it sits at the top of the order and moves the lower quartile not at all. With a sixth of a sheet's marks turned into real doubles the reference moves by less than a thousandth of an inch.

**The calibre keeps its other jobs**, which is the whole reason a person should still enter it: the smallest-hole gate, the torn-hole rescue and the size reported in holes are all still the calibre's.

## What it does to the thirteen images of question 38

Read only, nothing committed. Every one with the calibre Alan actually shot, stated:

| sheet | marks | flagged before | flagged now | reference |
|---|---|---|---|---|
| photo .22 LR, two of them | 24, 24 | 15 to 24 of them | **0**, **0** | the sheet |
| photo 6 ARC, three of them | 20, 18, 21 | most of them | **1**, **1**, **0** | the sheet |
| photo 6.5 CM 25 shot | 25 | most of them | **0** | the sheet |
| photo 6.5 CM 15 shot, three of them | 15, 17, 14 | **15 of 15** on the one Alan met | **1**, **1**, **0** | the sheet |
| the four scans | 15, 25, 24, 20 | 0 | **0** | the sheet |

The photograph Alan opened, which flagged all fifteen of its holes with the right calibre entered, now flags one.

**And the scans are untouched**, which is what entry 141 section 4.5 requires: they read the same reference they always did, because on a scan the sheet's quarter-point and the calibre agree within five percent. `HoleToCalibre` is unchanged.

**One thing worth seeing in that table.** Scan 4, the .22 LR sheet, finds **24 marks with the calibre stated against 19 without it**. The calibre is still worth entering, and still for the reason entry 130 section 2b.3 gave: it is what stops small holes being refused as too small. What it no longer does is decide whether those holes are one or two.

## Where it stops

At a third doubles the marks fall into two clear sizes and entry 82 section 3 takes over, refusing to read a size and asking for the calibre, so nothing is flagged at all. That is the opposite of what entry 141 section 4.2 wants, and the two sizes cannot be told from two calibres by their sizes alone: a merged pair is 1.41 times a single across, and .224 against .308 is 1.38. **Question 40** carries it with what I would do. Both rows are pinned by tests, so whichever way it is settled the change is one line.


# One type scale, one spacing scale, and a measurement instead of a squint

Entry 141 section 5.1, the first part of the interface work.

## The scales were already there; nothing held them

`Tokens` has had a six-size type scale and a six-step spacing scale for some time, and `Entry109Tests` already walks a rendered window and fails on a text size that is not on the scale. That is the better of the two checks **where it reaches**, and it only reaches what a test happens to render: a size set on a control no headless test shows is invisible to it, and stays invisible until somebody looks at that screen.

`TypeScaleTests` reads the source instead. Every file under `src/GroupLab.App` outside the theme folder, every commit:

- **No font size is a number.** There were none to begin with, so this holds a line that was already good.
- **Every gap is a step on the scale.** There were **eighteen** that were not: gaps of 1, 2, 6 and 10 pixels scattered through the marking window, the compare screen, the equipment form, the loads column and the print screen. They are now `Space4`, `Space8` and `Space12`.
- **The scale is five or six sizes**, because a scale of ten is not a scale.

## And a defect I did not find, which is the point of measuring

Reading the analysis screen's render at 1280 by 720, the figures in the right column looked cut off at the window's edge: "0.132 in" appeared to lose its last letter under the scrollbar. I changed the side columns to refuse a sideways scroll, re-rendered, and it looked exactly the same.

**So I measured it instead of looking harder.** `NothingIsCutOffTests` renders the analysis window at both of entry 141's sizes, asks Avalonia where it actually put every word, and fails on any whose right edge is past the window's. Nothing is past it, at either size. The figures sit close to the edge and are inside it, and the change I had made fixed nothing, so it is reverted rather than kept as a fix for a defect that was not there.

What is kept is the test. A screenshot is looked at once, on the night it is taken; this runs on every commit at both sizes and fails with the word and how far out it landed.


# Telling GroupLab which bulls you aimed at

Entry 141 section 5.3.4, question 37, and queue item 6 of entry 135.

## The fact the sheet cannot hold

Twenty holes on a twenty five bull sheet does not say whether five bulls were missed, five were never fired at, or every shot landed a bull away from where it was aimed. Only the shooter knows, and until now nothing asked them.

Entry 120's 6 ARC sheet is the case that matters: a load the rifle was not zeroed for, so every one of the twenty shots landed high and left by more than the gap between bulls, and **every one is nearer a bull it was not aimed at**. Read by nearest bull it gives a group that is tight, confident and about nothing at all.

## It needed no new machinery

`AssignmentRule.PerBull` already says how many shots a bull is expected to hold, and the matching already treats a bull with no room as closed. **A bull nobody aimed at is a bull expecting zero shots.** So saying which bulls were aimed at is saying which ones expect none, and it flows through the existing matching and the existing sheet-offset solver without either of them changing.

`AimedBulls` builds that rule three ways, because three ways is how people shoot:

| what a person says | what they type |
|---|---|
| every bull | leave it empty |
| whole rows | `rows 1-3` |
| the same columns of every row | `columns 2-5` |
| a list of bulls | `1-10, 12` |

The rows come from where the bulls sit on the page, not from their order in the definition, because a person reads rows off the paper and a definition may list its bulls however it likes. The test builds its sheet listed back to front for exactly that reason.

## It says back what it was told

Under the control: **"15 shots at 15 bulls, one shot each, and 10 bulls nobody aimed at."**

This is the input that decides what every figure afterwards is about, so a person has to be able to read it back. An input nobody can check is an input nobody can correct, and a mistyped row would otherwise be invisible.

## What is not done

**The end to end proof against entry 120's ground truth.** Section 5.3.4 asks for scans 4, 5 and 6 to be run with their aimed bulls set and the assignments compared with Alan's table. That needs a registered sheet rather than a hand built one, and it is the test that would show the 6 ARC sheet coming out right. `AimedBullsTests` covers the rule and says plainly that it is not that proof.


# Across, and up and down: the spread drawn

Entry 141 section 5.2.2, answering "is my group wider than it is tall".

Two strips under the group figures, sharing one scale, with every shot as a dot and a band for one standard deviation each side of the centre. One scale, because the whole question is which of the two is larger, and two scales would answer it by drawing rather than by measuring.

The dots are the shots themselves rather than a bar, because a spread where one shot is a long way out is a different thing from a spread where they are evenly placed, and a bar cannot tell those apart.

**The caption is the part that matters.** Every group is wider than it is tall or taller than it is wide; none is ever exactly square. A picture of two spreads, on its own, invites a person to read wind, a bipod or a technique into the ordinary lopsidedness of a handful of shots. So the words always carry the answer to the second question:

> Across 0.112 in, up and down 0.097 in. It measures wider than it is tall, but 24 shots cannot tell that from an ordinary round group.

Where the shots **can** separate them it says that instead, and where there were too few shots to run the circularity test it says there is no way to tell. The p value is the analysis's own, so the picture makes no claim the figures do not already make.


# Did the group open up as it was shot?

Entry 141 section 5.2.3. A barrel warming, a shooter tiring, a rest settling: all things people believe they can see in a group, and a group of ten shots fired in a random order will look like one of them often enough to convince somebody.

## It draws nothing unless the order is really known

A sheet does not record what order it was shot in. The order exists only where a chronograph string has been mapped to the shots, and nothing else in GroupLab knows it.

Numbering the holes left to right and calling that the shot order would draw a chart that looks **exactly** like a real one, and a reader would have no way to tell the two apart. So with no order known the block does not appear and the words say why.

## The test, not the picture

Spearman's rank correlation between the order fired and the distance from the group's centre, with a two-sided permutation p-value. `docs/STATISTICS.md` section 12a has the reasoning:

- **Ranks**, because the question is whether later shots sit further out, not whether they sit further out in proportion to anything, and because one wild shot should not decide it.
- **A permutation test**, because under "order carries nothing" every ordering of these shots is equally likely, so the null distribution comes from the shots themselves and assumes no distribution at all.
- **Two sided**, because a shooter looking for a barrel warming would otherwise find one at half price, and a shooter looking for settling in would find the opposite.
- **Nothing below five shots**, where every ordering is a large share of the ones there are.

## The test that decides whether it was worth building

`ShotOrderTrendTests` fires **400 simulated groups of ten Rayleigh radii in a random order** and requires the share called a trend at the 5 percent level to stay under 11 percent.

That is the measure that matters. What counts is not that a real trend is found; it is how often one is announced when there is none, because that is the number that decides whether this picture teaches people something true or teaches them to see barrel warmings in noise.

The caption follows it:

> It tightened a little, but 8 shots cannot tell that from chance: a group whose order carried nothing looks at least this ordered about 62 percent of the time.


# Is this load getting better or worse?

Entry 141 section 5.2.4: mean radius per session for one load, with its uncertainty, on the Session records screen.

## Why no line is drawn

Four sessions of the same load plotted against the date climb or fall. They always do. A shooter looking at that line sees a barrel wearing, or a batch of powder going off, or their own technique improving, and every one of those readings is a story told about four numbers a coin could have produced.

So there is no trend line on the chart at all. The sessions are dots at their dates, each with its own interval, and the caption carries the answer:

> 6 sessions of H4350 41.5, from 0.390 in to 0.550 in mean radius. The sessions go up and down, but 6 of them cannot tell that from chance.

## The test is the shot order test, asked of sessions

`ShotOrderTrend` already answers "do later values in a known order sit higher than earlier ones, more than a shuffle of the same values would". Sessions rather than shots changes nothing about the arithmetic, so it is not written twice: the chart calls the same Spearman rank correlation with the same two-sided permutation test, and the same floor of five values below which nothing is claimed.

Three or four sessions therefore get a picture and no verdict, and the caption says which:

> 3 sessions of H4350 41.5, from 0.400 in to 0.520 in mean radius. 3 sessions cannot show a trend at all; 5 is the fewest that could.

## The strongest thing it can say

Where every session's interval covers every other, that is said outright, because it is the case a table of mean radii hides completely: six different numbers, and nothing at all to choose between them.

> Every session's interval overlaps every other, so these 6 sessions cannot separate them at all.

## Two smaller decisions, both visible in the picture

- **Spaced by date, not evenly.** Sessions a year apart and sessions an hour apart are different evidence, and even spacing would hide that. Sessions all at one moment fall back to even spacing rather than stacking on one pixel.
- **Zero stays on the axis** wherever including it does not flatten the sessions into one line. An axis starting at 0.40 in makes an ordinary difference between two sessions look enormous.

## It refuses to draw for a mixture of loads

"Is this load getting better or worse" cannot be asked of a list mixing two loads, and a chart drawn over such a list would answer a question nobody asked while looking exactly like one that did. With the load filter on "Every load" the chart is absent and the screen says what to do to get one. `SessionsScreenTrendTests` holds that, and holds that the points are in date order whatever order the records come back in.


# The two numbers every chronograph prints, and why both mislead

Entry 141 section 5.2.5: the velocities drawn, with the mean and SD marked on them, and the extreme spread.

## The SD

An SD of 10 ft/s over ten shots is not a rifle that holds 10 ft/s. The chi-squared interval on nine degrees of freedom puts the truth anywhere from **6.9 to 18.3 ft/s**, and nothing about the number 10 says so. At thirty shots, which almost nobody fires for this, it is still 8.0 to 13.4.

So the caption never gives an SD bare:

> 10 readings: mean 2710 ft/s, SD 10.0 ft/s (6.9 ft/s to 18.3 ft/s).

`VelocitySdIntervalTests` draws 2000 strings of ten from a rifle whose SD is truly 12 ft/s and requires the interval to cover 12 between 93 and 97 percent of the time. An interval that does not cover the truth as often as it claims is a decoration, and that is measurable rather than arguable.

## The extreme spread

The extreme spread of ten shots is expected to be larger than that of five from the same rifle, because more shots means more chances at both tails. A shooter who fires more and reports a bigger ES has not found a worse load. So it is given with what it depends on, in the same sentence:

> Extreme spread 34.0 ft/s, which grows with the number of shots on its own, so it can only be compared with another string of 10.

## The picture

The readings as dots on one axis, the mean marked, and a band of one SD each side drawn over them. Dots rather than a bar, because a string where one shot is 34 ft/s off is a different thing from a string spread evenly and the two have the same SD. Only the two ends are labelled, because the extreme spread is the distance between them and a label on every dot would hide it.

It appears in the Ballistics screen's chronograph section, drawn from the string in hand: the list just read where there is one, otherwise the newest string the session has saved. Nothing is pooled across strings, because two strings shot on different days are two measurements and combining them would invent a spread neither has.

`units.SpeedDifference` was added for this: `Speed` rounds a muzzle velocity to whole units, which is right for 2710 ft/s and throws away a tenth of an SD of 10.4.


# The charts read as one set

Entry 141 section 5.2.6: bring the compare charts to the same type scale and colours as the rest.

The compare charts were already on the scale, so the work was the other way round: the two charts built tonight had to join them rather than the other way about. `SessionsOverTime` started with faint grey whiskers, which read as a background rule rather than as the measurement's uncertainty, and has been brought onto the compare charts' convention:

- **The interval is teal and heavier than the dot.** It is the thing that decides whether a comparison means anything.
- **The measurement is the impact colour, and is a dot.** It is only where the measurement happened to land.

`ChartConsistencyTests` reads the source of all six chart controls and holds three things: every dot-and-whisker chart uses those two colours that way round, no chart mixes a colour of its own instead of taking one from the palette, and every chart's text comes from the scale and the application's own faces.

**Why a source test rather than a rendered one.** Two charts in different colours are not wrong on any one screen a test can assert about. They are wrong together, across screens a person visits minutes apart, and by then nothing fails. Somebody who has learnt that the teal bar is the interval on Compare loads should not have to learn it again on Session records, and the only place that decision is visible is the line that draws it.


# Entry 141 section 5.3, item by item, against what was already built

Before building anything for section 5.3 I read what the editor already does, because the section reads as a list of new work and most of it is not.

| item | state |
|---|---|
| 1. Select a shot; highlighted on the image, in the shots list and in the review queue at once | image and list already; **the review queue was the gap**, and is done |
| 2. Move by dragging, add by a click in add mode, delete by key or button, all through undo | **already built.** A drag is one undo step however far the mark travelled; Delete works on the selected shot and the selection panel carries a Delete button |
| 3. Assign by bull picker, by keyboard, by dragging onto the bull; several at once | picker **done**; keyboard already built (type the bull's label, then Enter); several at once **done** by ticking rows; dragging onto a bull is **question 41** |
| 4. Which bulls were aimed at, with presets | done before this entry, and reported under its own heading |
| 5. A hand-edited shot is marked, shown differently, and never changed by a later re-detection or re-assignment | marked and shown already; re-assignment already safe through `BullChosen`; **re-detection is not**, and that is question 42 |
| 6. Every statistic and graphic updates as soon as an edit is made | **already built**: `Refresh` recomputes `GroupAnalysis.Analyse` and rebuilds every figure, judgement and flag on each change, and the session raises one after undo and redo too |
| 7. A review item opens the shot it is about, selected and ready to edit | **already built**: `FocusReview` selects the item's shot and brings it to the middle of the view |

The two questions are the only places this section cannot be finished by building, and both are behaviour questions rather than bugs. Everything else in section 5.3 is now done.


# One selection, and a third way to say which bull

Entry 141 section 5.3, items 1 and 3, as far as they go without an answer to question 41.

## The review queue was the place that did not agree

Selecting a shot already highlighted it on the image and in the shots list. The review queue marked only the item it was working through, so somebody who clicked a hole **because they wanted to know why it had been queried** had no way to see which of the items was about it. That is exactly the moment the question is being asked.

Now every review row whose item is about the selected shot is marked the same way the shots list marks its row. `OneSelectionEverywhereTests` holds both directions: every marked row names the selected shot, the count equals the number of items about it, and selecting another shot moves the mark rather than adding to it.

## A bull picker on each row

There were two ways to say "this shot belongs to that bull": click the hole then click the bull, and type the bull's label then Enter. **Both need the person to have found the hole on the sheet first.** Somebody working down the shots list has the shot's name in front of them and not its position, and on a 25 bull sheet at 1280 by 720 that hunt is the slow part of the job.

Each shots-list row now carries a picker of its own: the bulls by their printed labels, plus "none", starting on the bull the shot is already on. It is one word rather than "no bull" for a measured reason: entry 73 section 6 holds every row inside the right column, and adding a picker beside the two buttons that were already there broke that test by 16 pixels at 1280 by 720. The test is the only reason anybody would have known before somebody saw a cut-off button on their own screen. Choosing one selects the shot, assigns it, and **marks the bull as chosen**, exactly as clicking the bull does, so a later re-assignment leaves it alone. It is one undo step. `BullPickerTests` holds that the mark does not move, that the choice is marked as a person's, and that Undo puts it back.

## What is not done, and why

Section 5.3 item 3 also asks for dragging a shot onto a bull, and for assigning several selected shots at once.

**Dragging onto a bull is question 41**, raised rather than guessed: section 5.3 item 2 makes a drag a move, and a mark's position is a measurement that every figure is computed from. The same gesture cannot safely mean both. What is built is the click-hole-then-click-bull route, which is the same two-target gesture with no risk to the measurement.

## Assigning several shots at once, without moving a gesture people have learnt

The obvious way to select a second shot is control-click or shift-click on the mark, and on the marking canvas both are already taken: entry 115 section 2 gave them to choosing **bulls** for the load field, and says in as many words "never a hole".

So the several-shots answer lives where the one-shot answer already is. Each shots-list row has a tick box, the same control Session records uses to choose sessions for comparing, and once two or more are ticked a bar appears above the list with one bull picker and one button. `MarkingSession.AssignBulls` applies them all in **one** state change.

**One undo step, because it was one action.** Eight shots put on bull 3 as eight separate edits means pressing Ctrl+Z once leaves seven moved and one back: a state nobody asked for, and one nobody can see is wrong by looking at the sheet. `AssignSeveralShotsTests` ticks four shots, assigns them, checks every mark stayed exactly where it was, and then undoes once and requires all four to be back on their old bulls.

The bar is absent with nothing ticked and with one thing ticked, so the list never offers an action that would do nothing.


# Drop an image on the window, or paste one

Entry 137, the first item of entry 141 section 6.

## Both are Open, with a different way in

A dropped file and a pasted file go through the same call a chosen file does, so the three cannot behave differently. That includes entry 140 section 1.4's question: a sheet with edits nobody has saved asks before a drop replaces it, and Cancel leaves the sheet exactly as it was. `DropAndPasteTests` holds that, because a careless drop onto an afternoon's marking is precisely the accident this feature could cause.

Several files dropped at once opens the first and counts the rest in the status line. It is not a question: the answer is always the first one, and a dialog in the way of a gesture whose whole point is speed would make dropping worse than Open.

## The case with no file behind it

Image data copied from a browser, or a screenshot, has no name, no path and no metadata. It is written into `%LOCALAPPDATA%\GroupLab\pasted`, never beside the person's own files, and is an ordinary opened file from then on.

The one place that costs something is said out loud, because nothing else would say it:

> It was pasted, so it has no file name and no resolution of its own: set the scale by measuring a known distance.

A blank sheet scanned on a flatbed can take its scale from the scan's own resolution (entry 130 section 4.1). Pasted pixels have no resolution to take, and a person who does not know that would wonder why the offer never appeared.

## The clipboard is behind the one way out

Entry 122 put the browser, the file manager and the installer behind `IOutsideWorld` because a test run opened browser tabs on Alan's machine. A clipboard is the same kind of thing and worse: reading it in a test takes whatever happened to be on the machine at that moment, and writing it takes something away from whoever was working.

So `IOutsideWorld` gains `ReadClipboardAsync`, and `OneWayOutTests` gains a second guard beside the one for shelling out: exactly one file in `src/` may touch a real clipboard, and it fails the day a second one learns how. GroupLab reads the clipboard only on an explicit Ctrl+V or the Paste menu item, never on its own.

The reading itself lives in the application, because a clipboard belongs to a window and `GroupLab.Core` has none. `TheOutsideWorld` takes it as an installed function; with nothing installed, as on the command line, the clipboard is empty.

## One refusal message, which there was not one of before

Open's file picker filters to JPEG and PNG, so a file that is not an image almost never reached `OpenImage`, and when it did the decode failure went out through the crash reporter. A drop and a paste have no picker in front of them, so that had to be settled: all three routes now go through one guarded call, and a file that will not decode says

> That file could not be opened as an image. GroupLab opens JPEG and PNG images.

and the sheet that was open stays open.

## Where entry 137 and the code disagree

Entry 137 section 4 says: *"The same image safety applies (pixel cap, decode with a time limit) as for any file."* There is no pixel cap and no decode time limit on the desktop's Open path. They exist on the submission intake, which is server side and reached by a different route entirely.

So drop and paste have exactly the safety Open has, which is what the section's first sentence asks for, and the parenthetical describes something that does not exist yet. Raised as question 43 rather than invented tonight, because a cap is a number somebody has to choose and a wrong one refuses a legitimate 60 megapixel scan.

**Answered by entry 143, question 43, on 2026-09-23: build both, as their own item, after the current queue. Not urgent, and not built yet.** The numbers are decided:

1. A cap at **400 megapixels**, phrased as a limit against a hostile or broken file rather than a judgement about scanning, with the number and the measured size in the message. Alan's 600 dpi letter scans are about 32 megapixels, so the cap is twelve times his largest real file.
2. The decode moves to a background thread with a timeout and shows progress, because the window freezing on a large scan is a real complaint waiting to happen.

**Item 1 is built, 2026-09-23.** `ImageLoader.MostPixels` is 400 megapixels, checked after every decode in all four of the loader's paths, and the message says the measured size, the limit, and that a file that large is broken or built to exhaust memory rather than a scan anybody made. The check is after the decode because that is where the size is known: OpenCV reads the header and allocates in one call and there is no way through it to ask first, so this does not prevent the allocation, it stops everything downstream working on a file nothing here should be working on.

`PixelCapTests` holds the number against a real 600 dpi letter scan, which is 33.7 megapixels, and holds the other direction too: an ordinary image is still read. A cap written the wrong way round refuses every file, and a test that only checked the refusal would pass.

**Item 2, the background decode with a timeout and progress, is still not built.**

**Why it is still not built, said here so the specification and the code agree rather than only appearing to.** Entry 143 section 3 puts question 43 last of everything in that entry, behind questions 45, 46, 42, 41 and 44 and behind the batch 1 fixes. It is the one item in the queue that guards against a file nobody has sent, on a path nobody has complained about, and the items ahead of it were all things that mislead somebody using GroupLab today. Until it is built, **the desktop's Open, drop and paste paths have no pixel cap and no decode timeout**, and entry 137 section 4's parenthetical describes the submission intake's protections rather than the desktop's.


# The same newly written file, on the other side of the test

Twice in one night, and the second time it was not cleanup.

`EndToEndTests` writes a PNG and hands the path straight to the analysis. On this machine the analysis could not open it: something outside the process had taken the newly written file, which on Windows is the virus scanner or the search indexer. It passed on its own straight afterwards, which is what CLAUDE.md says these look like.

Cleanup can give up quietly, because the machine will clear a temporary folder. A test that needs to read its own file cannot. So `Temp.Readable` waits for the file to open, five tries at 120 ms, and then lets the failure happen anyway, where at least the message says what was going on.

## And then CI hit it, which changed the answer

I wrote the paragraph above saying this was one machine's virus scanner and not GroupLab's problem. Within the hour, `build and test` went red on `windows-latest`:

```
System.IO.IOException : The process cannot access the file
'...\grouplab-bench-262b8699\bench-25-shots.png' because it is being used by another process.
   at System.IO.File.ReadAllBytes(String path)
   at GroupLab.Cli.Imaging.ImageLoader.Load(String path)
   at GroupLab.Cli.Bench.BenchMaterial.Prepare(String root)
```

A clean hosted runner, with no virus scanner of Alan's on it. **So it is GroupLab's problem after all**, and the same thing happens to a person who opens a scan the moment their scanner finished writing it, or whose targets live in a synchronised folder.

`ImageLoader` now waits for a moment's lock: five tries, 120 ms apart, under a second in total, and almost always one attempt. A file still held after that is a real refusal and is reported as one. A moment's wait is the right answer to a moment's lock, and it is worth saying that I only believed that once a machine I do not own proved it.


# Entry 131 section 1's checklist, done by looking, and what looking is worth

Entry 131 section 1 asks for renders of every screen at both sizes in both themes, looked at against a checklist. Every screen now has them, so this was a pass over the set rather than new drawing.

## What looking found

**The Equipment form was the one real failure.** Fourteen fields in a single tall column: at 1280 by 720 seven of them were below the fold, and at 2560 by 1440 the click value, a number like 0.25, had a box 1200 pixels wide while the left half of the screen was empty. It failed three lines of the checklist at once: figures in aligned grids, consistent spacing, and every control's purpose obvious.

It is now a two column grid, and a field's width follows what it holds: a number gets 140 pixels, a name gets its column, and Notes takes the full width because it is the one field somebody really does write a sentence in. Twelve of the fourteen fields are visible at 1280 by 720, and all of them with Save and Cancel at 2560 by 1440.

## What looking got wrong, for the second time

The library's sheet list looked cut off to me at 1280 by 720: the sighter counts sit within a pixel or two of the divider, and "25 + 3" losing its last character reads as "25 + " while "6" losing its only one reads as nothing at all.

**It is not cut off.** I measured rather than trusting the render, and nothing on any screen is.

That is the same mistake as the analysis figures earlier this month, and the same lesson: a figure two pixels inside a panel's edge and a figure two pixels outside it look identical at any scale a person views a render at. So the answer is the same as it was then, a measurement rather than a promise.

## The measurement that was missing

`NothingIsCutOffTests` held every word against the **window's** edge. A word inside a panel that clips its own contents sits well within the window and is still cut in half, and the render just shows a smaller number.

The test now also holds every word against every ancestor that clips, walks five screens rather than the marking one alone, and names the screen and the panel in the failure. Two things had to be got right for it to mean anything:

- **`IsEffectivelyVisible`, not `IsVisible`.** The editor's header stays in the tree while another screen is showing, with its buttons far off to the side. Something nobody can see has not been cut off, and treating it as cut gave four confident false reports on three screens.
- **It counts what it measured.** If nothing in the window sits inside a panel that clips, the walk proves nothing and would go on passing while saying nothing at all, so it fails in that case too. A test that cannot fail is worse than no test, because it looks like cover.


# Two sheets in one photograph, and which one you are reading

Entry 130 section 2c's last item: "the several-sheets-in-frame case, where GroupLab should say that more than one sheet is in view and which one it measured rather than choosing silently."

## What it did before

Marker ids are unique on a sheet, so two copies of the same printed sheet in one frame repeat every id.

`DetectFiducials` matched each decoded marker against the printed list and removed it as it went. The first marker carrying a given id won, purely by the order the detector listed them; every later marker with that id fell through to `unexpected` and was rejected as "decoded, but not printed on this tile or already matched".

So the measurement was of one of the two targets, chosen by list order, and **nothing anywhere said there was a second one**. Not the summary, not the trace, not the screen. Somebody who photographed two targets on the bench at once got figures about one of them and no way to know which.

That is not an accuracy problem. Both sheets were measurable and one of them was measured correctly. It is a problem of silence, which is the failure this project treats as the serious one.

## What it does now

**It counts.** The number of copies in view is the largest number of times any one id appears. One copy is the ordinary case and says nothing, because there is nothing to say.

**It groups.** With two or more copies, the markers are split into that many groups by where they are in the frame, largest first, each group taking at most one marker of any id. Two sheets side by side separate on the gap between them.

**It chooses, by a rule written down.** The group with the most markers decoded wins, because a sheet whose markers all read is the one the measurement can trust. The largest markers break a tie, being the sheet nearest the camera and most square to it.

**It says so**, in the summary a person reads:

> 38 of 38 markers found. 2 sheets are in view; the one measured has 38 of its markers decoded, the largest in the frame.

and in the trace beside the tile choice, with what it passed over, in the existing `Decide` form:

> **sheet**: 1 of 2 in view, because 4 of its markers decoded, the largest in the frame. Instead of: a sheet with 2 markers decoded.

`TwoSheetsInFrameTests` holds six things, including the one that would let the whole thing be wrong: **no sheet ever takes the same id twice**, which is what would let two sheets be measured as one.

## What is still not done in section 2c

The four measurement items need the range folder read in place: pairing each burst with its scan by hole pattern, agreement in inches hole by hole, the 14:14 burst's per-photograph identification at each angle, and what the blank-sheet path makes of the two tape-measure frames. None was reached. The behaviour change was taken first because it is the one that changes what a person is told, and because it needed no photographs to build or to prove.


# The mounted photograph gate, measured for the first time

Entry 130 section 6b item 1. `DESIGN.md` says the mounted half of the photograph gate is the product requirement and has never had real material. Alan's 59 photographs of 2026-09-20, taken at his range at many angles, are that material. Read in place; nothing committed, no metadata read.

**A correction, 2026-09-23.** This record first said those sheets were stapled to corrugated plastic outdoors. That description came from entry 130 section 6b's own wording and I repeated it; Alan says his sheets are on his backer board, and the stapled corrugated plastic is a submission from a second phone that none of this work used. I read those photographs for measurement only and never established what they show, so the mounting should never have been asserted here. What the figures below rest on is stated by provenance instead.

## The first number is the one nobody expected

**28 of the 59 photographs could not be read at all.**

| why it failed | how many |
|---|---|
| no code on the sheet could be read | 27 |
| the code read, but 0 of 34 markers found | 1 |
| **read and registered** | **31** |

Twenty-seven photographs of a day's shooting, in which GroupLab could not find the printed code that says which sheet it is looking at. Before any question about accuracy, that is the gate: **on nearly half of a real day's photographs the software does not get as far as having an opinion.**

Whether those 27 are photographs of sheets at all, or of the bench, the rifle and the view, is not established: the folder was read in place and its contents were not catalogued beyond what the command reported. That is the first thing section 2c's pairing will settle, and it could move this number a long way in either direction.

## What the 31 that registered actually measured

Every one of them fitted the same model: a homography with radial distortion. Not one needed anything else, and not one failed to fit.

| bull-centre error, inches on the page | median across the 31 | range |
|---|---|---|
| each photograph's median | 0.0048 | 0.0019 to 0.0132 |
| each photograph's worst bull | 0.0202 | 0.0044 to 0.0898 |

Against the gate's 0.005 in for bull centres:

- **18 of 31** have a median inside it.
- **1 of 31** has its *worst* bull inside it.
- 9 of 31 keep the worst bull under 0.010 in; 15 under 0.017 in.

24 of the 31 located all 25 bulls. The rest located 4, 5, 5, 11, 15, 15 and 24, which is a sheet partly out of frame or too oblique at one end.

## What that means, plainly

**A photograph of a mounted sheet registers well on average and badly somewhere.** The median bull is inside the gate on more than half the photographs; the worst bull is outside it on thirty of thirty-one. A group is measured from particular bulls, not from the median bull, so the worst-bull figure is the one that decides what a photograph can be trusted for.

Set 0.02 in, the median worst-bull error, against a mean radius of about 0.17 in on scan 6: a shot measured from a bull that is 0.02 in out carries roughly a **twelve percent** error in its own offset. That will not turn a good group into a bad one, and it is far too large to compare two loads with.

## What is not measured here, and must not be read as if it were

**The hole columns in this run are meaningless and are not reported.** Every photograph was run against one scan, scan 6, because pairing each photograph with its own scan is section 2c's first item and is not done. The bull-centre figures survive that, because both the scan and the photograph locate the *printed* bulls of the same definition in page coordinates, and every copy of a definition has the same printed geometry. Hole positions do not survive it: they are the holes of whichever sheet was in front of the camera.

So this is half a gate record. The half it has is the half that was missing.

## Still to do in section 6b

Items 2 and 3 are untouched: the bent-page model fitted with markers held out in turn, and the proposed wording for when GroupLab should tell somebody to flatten the sheet, shoot more squarely, or scan instead. Both want the pairing first, because a candidate model has to be judged on hole positions and those need the right scan.


# Each photograph paired with its own scan, and the gate record completed

Entry 130 section 2c items 1 and 2, and entry 130 section 6b item 1's other half. Every photograph was run against all five scans that can be read, and paired with the one whose holes it matches.

## Scan 2 cannot be read at all

`compare-photos` on scan 2: "the scan could not be measured: no code on the sheet could be read".

That is a flatbed scan at 600 dpi, the easiest case there is, and GroupLab cannot identify the sheet. It is the same failure as the 27 photographs, on material where distance, angle and light are not excuses. **Anything shot on scan 2's sheet has no truth to be compared against**, and that is not a photography problem.

## The pairing

| scan | its holes | photographs paired to it | holes matched |
|---|---|---|---|
| 1 | 14 | 165624, 165627, 165634, 165637 | 14, 14, 13, 12 |
| 1 | 14 | 165611, 165617, 161502, partly in frame | 5, 5, 2 |
| 4 | 19 | 153309, 153325, 153333, 153336, 153356 | 18, 18, 17, 18, 18 |
| 5 | 20 | 153340, 153344, 153347 | 20, 18, 20 |
| 3, 6 | | none | |
| none | | the 14:14 burst, 15 photographs | 0 against every scan |

**The 14:14 burst matches nothing.** Fifteen photographs, every one registering well, with bull-centre medians from 0.0019 to 0.0119 in, and not one hole in common with any readable scan. Several of them find no holes at all and one finds ten. The likeliest reading is that the burst is of scan 2's sheet, whose scan cannot be read, so the pairing that would prove it is the one pairing that cannot be made. That is entry 130 section 2c's "14:14 burst identification" answered, and the answer is that it cannot be identified from this material.

## Hole agreement, the half the earlier record was missing

Over the 15 paired photographs:

| hole-position error, inches on the page | median across them | range |
|---|---|---|
| each photograph's median | 0.0316 | 0.0229 to 0.0478 |
| each photograph's 95th percentile | 0.0695 | 0.0457 to 0.1346 |
| each photograph's worst | 0.0852 | 0.0457 to 0.1476 |

Read that against the 0.15 in the gate uses for deciding whether a hole in a photograph is the *same hole* as one in the scan. Every photograph clears that comfortably, which is why the matching worked at all.

Now read it against what the number is for. **A hole's position in a photograph is out by about 0.03 in at the median and 0.07 in at the 95th percentile**, against a mean radius of about 0.17 in on scan 6. That is roughly **18 percent of the group's own size at the median**, and 40 percent at the 95th percentile.

## What a photograph can be trusted for

- **Counting shots, and seeing where they are**: yes. Matching found 12 to 20 of 14 to 20 holes on every fully framed photograph.
- **Measuring a group**: no, not to compare loads. An 18 percent error on each shot's own offset is larger than the differences people are trying to detect.
- **Zeroing**: probably, since a zero correction is a group centre and averaging 20 shots reduces the error considerably. Not measured here.
- **A sheet partly out of frame**: the three photographs that located 4 or 5 bulls matched 2 to 5 holes of 14. They register, they look like they worked, and most of the sheet is simply not there.

## What this does not settle

The bent-page model of section 6b item 2 is not tried, and the warning wording of item 3 is not proposed. Both now have what they need: a pairing, and hole errors to judge a candidate model by.


# The application was slower than the command line because it decoded the same file three times

Entry 130 section 6 item 1.

## What it was doing

`OpenImage` did this, on the thread that draws:

1. `ImageLoader.Load(path)`: read the file, decode it grey.
2. `ImageLoader.LoadMaxChannel(path)`: read the file again, decode it in colour again, split, take max(R, G, B).
3. `Cv2.ImRead(path, Color)`: read the file a third time and decode it in colour a third time, for the picture on screen.

Three reads and three decodes of the same file. On a 600 dpi letter scan that is three passes over about 34 megapixels where the command line makes one. It is enough on its own to explain being two to three times slower without any of the analysis being slower at all.

`LoadForEditor` now reads once and hands back the grey, the max channel and the colour image together.

## The saving I did not take, and why

The obvious further step is to convert the colour image to grey rather than decoding a second time. OpenCV's grayscale decode and its BGR to grey conversion are built on the same coefficients, so they ought to agree.

**They do not.** Measured over the committed screen renders, they differ at about eight percent of pixels, by one level, on every image tested.

One level matters here. Every threshold in the detector is a comparison against a grey level, so a pixel that moves by one can move a hole's edge, and in a marginal case a hole in or out of the result. Entry 130 section 6 item 2 says an optimisation changes no result: same holes, same assignments, same gate record. A grey that differs anywhere is a different image.

So the grey is still decoded as grey, and the saving is one read and one decode out of three rather than two.

**`ImageLoaderSameResultTests` is the reason this is known rather than assumed.** It was written expecting to pass, and it failed on the first run, on all eight images. Without it the conversion would have shipped, every figure would have moved by an amount nobody could predict, and the commit message would have said "no change in results".


# The bent-sheet model, run beside the ordinary one

Entry 130 section 6b item 2: a model that allows the page to bend, as a candidate beside the current one and never replacing it.

GroupLab already has one. `RegistrationModel.Surface` is a generalised cylinder through a camera, and `Auto` never chooses it, which is why every row of the gate record says "homography with radial distortion". `compare-photos` gains `--model auto|homography|radial|surface`, defaulting to `auto`, so the candidate runs only where it is asked for and the application's behaviour is untouched.

## What it does to the seven paired photographs it can run on

| photograph | bull median, radial | bull median, surface | bull worst, radial | bull worst, surface | hole median, radial | hole median, surface |
|---|---|---|---|---|---|---|
| 165624 | 0.0076 | **0.0065** | 0.0353 | **0.0184** | **0.0316** | 0.0328 |
| 165627 | 0.0044 | **0.0030** | 0.0197 | **0.0130** | **0.0346** | 0.0361 |
| 165634 | 0.0109 | **0.0084** | **0.0552** | 0.0601 | **0.0450** | 0.0466 |
| 165637 | 0.0088 | **0.0066** | 0.0636 | **0.0617** | **0.0375** | 0.0392 |
| 153340 | 0.0138 | **0.0059** | **0.0491** | 0.0542 | **0.0282** | 0.0354 |
| 153344 | 0.0125 | **0.0082** | **0.0217** | 0.0486 | **0.0229** | 0.0287 |
| 153347 | 0.0044 | **0.0037** | 0.0222 | **0.0213** | **0.0246** | 0.0248 |

**The bent-sheet model fits the markers better and predicts the holes worse.** Seven of seven on the median bull error, four of seven on the worst bull, and **nought of seven** on hole positions.

That is worth stating plainly because it is the opposite of what a better registration is supposed to buy. The markers are what the model is fitted to; the holes are the points it was not fitted to, and they are the only ones that matter. A model that improves where it was fitted and not where it was not is describing the markers rather than the sheet.

It also does not rescue the gate: not one photograph comes inside 0.005 in at the worst bull under either model, which is what `DESIGN.md` [r6] already found on the Phase 0 frames.

## What section 6b item 2 asked for that this is not

The entry asks for a **smooth correction over the marker grid**, a thin-plate spline or piecewise fit on top of a deterministic robust homography, with markers held out in turn. That is not built. What is reported here is the model that already exists, run on the same photographs, which is the comparison the entry wants without the new model it also wants.

Given what the existing surface model does to the hole errors, a leave-one-out measurement is exactly the right next step, and the result above is the reason: it is the measurement that would have caught this without needing the scans at all.

## A crash, found by running it

`20260920_153336.jpg` under `--model surface` throws `System.IndexOutOfRangeException` inside the surface mapping:

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at GroupLab.Core.Detection.ExpectedImage.Render(...) ExpectedImage.cs:line 31
```

Line 31 is the `mapping.ToPage` call, so the index is thrown inside `DevelopableSurface`'s inverse, through `FoldedSheet.Sheet`. The other fourteen paired photographs run. It is **not reachable from the application**, since `Auto` never selects this model and the window passes no options, so it is a defect in a candidate rather than a live fault. Recorded as question 44 with the exact reproduction rather than guessed at: the candidates are `Beyond` and `Last` indexing a `Side` whose arrays are shorter than the bisection assumes, and a proper answer needs a debugger on that one photograph.


# When GroupLab should tell somebody to scan instead

Entry 130 section 6b item 3: say what a photograph can be trusted for, and propose the warning's wording. Proposed, not built.

## What the measurements support

- **A hole's position in a photograph is out by about 0.03 in at the median**, 0.07 in at the 95th percentile, against a group whose mean radius is about 0.17 in.
- **The worst bull is outside the 0.005 in gate on 30 of 31 photographs**, under either model.
- **28 of 59 photographs could not be read at all.**

## The proposed wording

Three messages, none of them a refusal, because a photograph that reads is worth having.

**When a sheet is read from a photograph at all**, on the analysis, beside the scale:

> Measured from a photograph. Shot positions are typically out by about 0.03 in, which is a fifth of a group this size. Good for counting shots and for a zero; scan the sheet before comparing two loads.

**When the worst bull is far out**, which is the case a person can do something about:

> This photograph was taken at enough of an angle, or the sheet was bent enough, that one corner of it registers about {worst} in off. Photograph the sheet square on and as flat as you can get it, or scan it.

**When no code could be read**, which is the commonest failure by a long way:

> GroupLab could not find the printed code on this sheet, so it does not know which target it is. Fill the frame with the sheet, get the whole of it in, and try not to photograph it at an angle. A scan almost always works.

The numbers in the first two come from the figures above and would move with them, so they belong in one place with the measurement beside them, the way `HoleToCalibre` does.


# The end to end proof against entry 120's ground truth

Entry 141 section 5.3.4: "Scans 4, 5 and 6's ground truth in entry 120 is the test: show that with the aimed bulls set, the assignments match Alan's table." Scans read in place, nothing committed.

`grouplab analyze` gains `--aimed`, taking the same words the window's own box takes, because both now call `AimedBulls.Parse`. A proof run from the command line is worth nothing if "bulls 2 to 5 of every row" means something different there than in the product.

## Scan 5, which is the sheet the whole feature exists for

Alan's table: 20 shots, bulls 2, 3, 4 and 5 of every row, a load the rifle is not zeroed for, so every shot lands high and left of its aim point and many holes sit nearer a bull they were not aimed at.

| | nearest bull | aimed bulls set |
|---|---|---|
| assignment | bulls it landed nearest | **2, 3, 4, 5, 7, 8, 9, 10, 12, 13, 14, 15, 17, 18, 19, 20, 22, 23, 24, 25** |
| mean radius | 1.120 in | 0.617 in |
| extreme spread | 5.304 in | 2.375 in |
| centre from aim, across | **-0.041 in** | **-1.163 in** |
| centre from aim, down | -0.657 in | -1.106 in |

**The assignment is Alan's table exactly**, all twenty, one shot per aimed bull.

**The bottom row is the defect.** With nearest bull, GroupLab told this shooter their group was 0.041 in off centre across: nothing to dial. It is 1.163 in left. A person reading the old figure would have left the sights alone, and the number that told them to was produced with no hint that anything was wrong with it.

## Scan 4, which it does not fix, and was never going to

Alan's table: 23 shots on bulls 1 to 4, 6 to 9, and 11 to 25. Windage was changed after row 2, so rows 1 and 2 share one point of impact and rows 3 to 5 another.

With those bulls named, GroupLab assigns its 24 detected holes to bulls 2, 3, 4, 4, 7, 8, 9, 9, 11, 12, 13, 13, 14, 16, 17, 17, 18, 19, 20, 20, 21, 23, 24, 24: six bulls take two shots and six take none.

**That is the expected answer and not a failure of this feature.** A single sheet-wide offset cannot describe a sheet with two points of impact, which entry 120 said on paper before any of it was built. The item that would fix scan 4 is entry 130 section 3.2, row breaks, and it is still not built. Until it is, scan 4 is a sheet GroupLab should be saying it cannot assign confidently rather than assigning.

## Scan 6, where the assignment is right and a hole is missing

Alan's table: 10 shots, bulls 1 to 5 with one primer and 6 to 10 with another, with shot 6 landing left of bull 21, far from the rest.

GroupLab detects **9** holes and assigns them to bulls 1, 2, 3, 4, 5, 7, 8, 9, 10: one per aimed bull except bull 6, whose shot is the one that is not detected. Every shot it has is on the bull Alan aimed at.

## Two disagreements this turned up

**1. Scan 6 detects 9 holes where entry 130 item 2b.6 recorded 10.** That item says "scan 6 goes 9 to 10, both exactly Alan's own counts", with the calibre named. Measured tonight, scan 6 reads 9 holes with `--calibre .243`, 9 without a calibre, and 9 with `--sighters`. Recorded as **question 45** rather than assumed to be a mistake in either place.

**2. The sheet offset is solved over every bull, and narrowing it makes things worse.** `AimedBulls.For` lists every scoring bull and gives nought shots to the ones nobody aimed at, so `SheetOffset`'s test of `PerBull.ContainsKey` is true for the whole sheet, which is exactly what its own documentation says must not happen. Narrowing it to a count above zero made `SheetOffsetAssignmentTests` put five of twenty shots on bulls nobody aimed at, with the real scan 5 unaffected either way. **It is back as it was, with a note at the line**, because a comment is not a reason to turn a passing proof red. Question 46 carries what I think is going on: the restraint is real but delivered by the matching rather than by the solve, which would make the code right and the paragraph above it wrong.

**And a third, smaller.** "columns 7" on a sheet five bulls wide parsed perfectly and produced a rule naming no bulls, which makes the offset give up and the shots go to whichever bull they landed nearest: the exact behaviour the shooter was turning off, with nothing on screen saying so. It is refused now, like any other text that names nothing.


# Entry 144: the site publishes itself, and the release notes keep up

`docs/NOTES-FROM-PLANNING.md` entry 144, actioned 2026-09-23. It supersedes entry 128 section 6.

## What changed, in one table

| what | before | after |
|---|---|---|
| a site content commit | published nothing until a person ran `gh workflow run website.yml` | publishes itself, within about 7 to 8 minutes of the push |
| what the site waits on | `build and test` on Windows, macOS and Linux, about half an hour | its own build and its own page tests, about a minute |
| the release notes | written by hand, and nine builds behind on 2026-09-22 | the nightly writes and pushes its own entry, which publishes the page |
| the server's check | every 15 minutes | every 5 |
| the screenshots | whenever somebody remembered, last on 2026-09-19 | weekly, and on demand |
| a publish's reason | required on the dispatch | optional; a push records the commit subject |

## The paths that publish

`website/**`, `docs/RELEASE-NOTES.md`, `docs/GLOSSARY.md`, `docs/USER-GUIDE.md`, `docs/TESTING-GUIDE.md`, `docs/USER-GUIDE.pdf`, `docs/TESTING-GUIDE.pdf`, `docs/figures/screens/**`, `targets/**`, `.github/workflows/website.yml`.

A commit touching only `src/` publishes nothing, which is the point: the application changing is not the site changing, and the nightly's own notes commit is what carries an application change onto the site.

## Two gaps I found in my own work, before either could bite

Both were found by re-reading what I had written against what section 4 needed, not by a test.

1. **`docs/figures/screens/**` was not in the paths filter.** The weekly screenshot job would have rendered the interface, committed twenty changed images, and published nothing. The symptom would have been a site that looked maintained and was not.
2. **The screenshot commit would have started a full CI run and a nightly build.** An image-only commit has no C# in it, and a build of the application from one is a release of nothing. It now carries a `[screens] ` marker, guarded exactly like `[notes] `.

## The screenshot finding, which is worse than the drift the job was written for

The site's screenshots were last regenerated on 2026-09-19. They could not have been refreshed by running the render walk, because **the walk did not produce the size the website uses.** It rendered 1280 by 720 and 2560 by 1440; the site shows 1400 by 900.

So for four days the live site showed an interface from before the type scale work, the three new charts, the bull picker, the tick boxes on every shot row and the drop target, and nothing could have said so: the walk passed, every size it produced was current, and the size being served was not among them.

1400 by 900 is back in `Entry109Tests`. This commit carries **31 refreshed images**, two of them screens the site had never shown at any size.

## The loop guard, proved both ways round

Section 2.2 asks for proof in both directions, and the second direction is the one that fails silently.

| what is proved | how |
|---|---|
| a notes commit starts no test run | every job in `ci.yml` refuses a subject beginning `[notes] ` |
| and starts no nightly either | the nightly's first job refuses it too, because a run whose jobs all skip still reports success |
| an ordinary commit still runs everything | every clause of every condition is read and required to be a denial, with no `\|\|` anywhere |
| a notes commit does publish the site | `docs/RELEASE-NOTES.md` is in the site workflow's paths filter |

`NotesCommitLoopTests`, four tests. The loop it guards against is not hypothetical: notes land, `build and test` runs, the nightly fires on that success, builds, publishes, writes notes, pushes. That is a release every few minutes for ever, each one deleting the oldest to keep thirty, until somebody notices.

The third row is the one worth the effort. A guard written the wrong way round would skip every ordinary commit instead of the marked ones, turning the whole test suite off, and CI would go green faster than ever.

## What is still open in section 6

Named rather than implied, because a report that quietly omits its unproved parts is the thing entry 130 section 4 was written about.

- **A site content commit that published itself, with its timings:** this commit. Reported below.
- **The failure path:** measured locally. Reported below.
- **A nightly whose notes reach the live releases page with no human step:** cannot be shown until the next nightly runs.
- **The sync log at a 5 minute cadence:** cannot be shown until Alan runs `install.py`. It is the one manual step section 3 names.


# Entry 145: every build says what changed, in plain words

`docs/NOTES-FROM-PLANNING.md` entry 145, actioned 2026-09-23.

Alan, on the release notes as they read that morning: "I dont like how many of the release notes just say 'Nothing in this build changes what you see or do. It carries internal work only.' No matter what is done, it should be stated plainly what changed."

## The six, and what they actually carried

Every one of these said nothing changed. This is what was in them.

| build | what it really carried |
|---|---|
| nightly.78 | a note saying where the release notes history starts, and why that is not a gap |
| nightly.77 | six missing builds added to the release notes page |
| nightly.76 | the progress file saying in one place what is waiting on a decision |
| nightly.75 | the last eight of the eighteen research articles |
| nightly.72 | **the first measurement GroupLab has against 59 real photographs of a target on a board** |
| nightly.30 | the key the server uses to check a site update, and a check of the build against a real scanned sheet |

Nightly 72 is the one that makes the case. 28 of 59 photographs could not be read at all, and of the 31 that could, one was accurate enough to measure a group from. That is the most useful thing GroupLab had learned about itself that week, and the page said "internal work only".

## Four more that were silent in a way the entry did not name

Nightlies 26, 18, 14 and 12 listed their known issues and said nothing at all about what changed. They were not caught by the entry's wording, and they are the same fault, so they are written too.

**Nightly 12 is the first build GroupLab ever published for itself**, and the file did not say so.

## The one claim I would not write

Nightly 26 carries the commits behind "where the group actually landed" and "the scan's stated resolution". `CLAUDE.md` records that nightly 27's note about the first of those was untrue on the day it was published, because the code existed and was wired to nothing.

So its new entry says those two are groundwork that **could not be reached from any screen in that build**. That is what was true, and it is the second time this project has had to write that sentence about the same feature.

## What the generator does now

| before | after |
|---|---|
| New, Fixed, Changed | **What you will notice** and **Under the hood**, and a build shows only the ones it has |
| a commit with no trailer became part of "Plus 7 internal changes" | it gets a line of its own, written from its subject |
| four checks on a note | eight: the four that were there, plus no file path, no commit hash, no class or method name, nothing in code style |
| no way to know a trailer was forgotten | `--missing` names those commits in the build's own report |

A build with no commits behind it is refused outright. It is the only thing left that could honestly say nothing, and it cannot happen: a build is made from a commit.

## The awkward part, said plainly

A commit subject here is often written for the log, so a line generated from one can carry a file path or a class name, which section 4 forbids. Two things stop that, and neither is free:

1. **A short table** translating the handful of repository files whose names appear in subjects into plain words. "docs slash release notes carries nightlies 71 to 76" becomes "The release notes carries nightlies 71 to 76."
2. **For anything the table does not cover, the build fails** and names the commit.

**The grammar suffers in the translated case**, as that example shows, and I have left it rather than guess at a rewrite. The fix is a trailer on the commit, which is what section 3.1 asks for anyway, and `--missing` is what makes the omission visible.

## Section 6: what a three build jump looks like in the update bar

Generated from the real builds, as `SkippedVersions.Combined` assembles them. Somebody on nightly 76 being offered nightly 81 sees:

```
3 builds are new to you, newest first.

GroupLab 0.2.0-nightly.81

**Under the hood**

- No link to the old site anywhere on grouplab.org, and scan 6 was never ten.
- Two corrections before anything is published.
- The research section goes live, and the notes carry nightlies 77 and 78.

GroupLab 0.2.0-nightly.78

**Under the hood**

- A note on where the release notes stop, and why that is not a gap.

GroupLab 0.2.0-nightly.77

**Under the hood**

- The release notes carries nightlies 71 to 76.
```

**It reads adequately and not well**, and it is worth saying which part is which. The shape works: the heading tells the reader at a glance that none of this is something they will meet, which is exactly what they want to know before deciding whether to update. The lines are the generated kind, because none of those three commits carried a trailer, so they are the floor rather than the ceiling. The same jump written from trailers would be three sentences a person could act on. That is the argument for section 3.1's rule that every commit carries one.

## The tests

| what | why it exists |
|---|---|
| the sentence cannot come back | it is named, and so is the count that replaced the rest of a build |
| every build lists something | a build with no lines is the old fault in a new shape |
| every build's lines sit under a heading | a loose list says nothing about whether you will meet it |
| no line names a file, a hash or a class | this page is read by people who have never seen this repository |
| the generator cannot write the old sentence | the way it returns is not somebody typing it, it is the generator falling back to it |


# Entry 146: a tour of the application, one page per screen

`docs/NOTES-FROM-PLANNING.md` entry 146, actioned 2026-09-23. Every screen, not the three the entry allows as a fallback.

Alan: "I think there should be a separate page for screenshots of each section of the application that explains what is happening instead of just a few screenshots on the main page."

## What is there

`https://grouplab.org/tour/`, in the top navigation between Download and Guides. Eleven pages: an index of ten cards, and one page per screen.

| page | what it covers |
|---|---|
| Target library | the twenty built-in sheets, grouped by job, and designing your own |
| Printing | printing at true size, and what GroupLab has and has not proved on paper |
| Marking and review | every hole numbered to its bull, and the questions raised before anything is measured |
| The analysis | the composite, the intervals, the zero correction and the per-axis views |
| Showing the work | the same screen with every explanation open |
| Session records | the saved sheets, filtered, and choosing two to compare |
| Compare loads | two loads side by side, and the sentence saying whether the shots tell them apart |
| Equipment | rifles, barrels and loads, and which fields the other screens need |
| Ballistics | a trajectory and a dope table, which changes no marking |
| Settings | units, theme, which builds it offers, and the log |

Each page: the screenshot large in both themes, what the screen is for, a numbered list of its parts, two to five steps, where it sits in the flow with links either side, and links to the guide or research article where one exists.

## Section 2.3: a list, not an overlay

The entry offers a numbered overlay on the image or a labelled list beneath, and says a reader must be able to match every item to something they can see.

**I used the list**, and it is worth saying why rather than leaving it as a preference. Every one of these screens has eight to eleven parts worth naming. Eleven numbered badges over a 1400 by 900 screenshot would sit on top of the thing they point at, and the screens with the most to say, marking and the analysis, are the ones where the badges would cover the most. The list names each part in the words the screen itself uses, so "Accept and analyse" and "Detect on a GroupLab sheet" are found by reading the button rather than by hunting for a small number.

## Section 4: how it cannot go stale

The tour and the weekly screenshot job are two lists of the same screens in two languages that cannot see each other. Two lists drift.

1. **`website/tour.json` is the list**, and `website/build.py` builds every page from it.
2. **The site build refuses** when a screen there has no render, or a render has no page. That is a build failure rather than a test, because the page is the thing that goes wrong.
3. **`TourTests` says the same from the other side**, so a screen added to the render walk fails with the name of the page somebody still has to write.

**Proved rather than assumed.** A screen named `reloading` was added with no picture behind it, and the build stopped:

```
website/tour.json: the tour has a page for 'reloading' and no screenshot of it was rendered
```

## Section 5: what came off the home page

It had four screenshots. **It has two.**

| was | now |
|---|---|
| analysis, in the hero | kept |
| marking, in a row of three | kept, as a single figure beside the status panel, with a link to its tour page |
| the target library, in that row | removed; it is the tour's first page |
| printing, in that row | removed; it is the tour's second page |

In their place, a note pointing at the tour: the quickest way to see whether GroupLab suits you before downloading it. A test fails if the home page ever shows more than two screens again.

## Section 3: the words

No class names and no file paths, held by a test on the same four patterns the release notes use.

That rule is not free. Writing these meant reading every screenshot rather than the code, which is exactly the point: a page written from the code names the class, and a reader with the application open beside the page cannot find it.

## What section 4.4 asks of every entry after this one

The screenshot job replaces the picture on its own and nothing replaces the words. A tour page naming a button that is no longer there is worse than no tour page, because a reader takes it for the truth. So an entry that changes a screen now says in its report whether that screen's tour page still describes it. It is in `CLAUDE.md` rather than in a test, because no test can tell whether a sentence is still true.


# Entry 143, question 44: leave one marker out, and the answer is not to write the spline

`docs/NOTES-FROM-PLANNING.md` entry 143, question 44, measured 2026-09-23. "Measure first, build nothing."

## The question, and why the fit's own residual could not answer it

A bent-sheet model is fitted to the printed markers and then used to say where a bullet hole is. A hole is not a marker: it sits between them, where nothing was measured. So the residual at the markers the model was fitted to says almost nothing about how well it places a hole, and the model that fits its own markers best is the one most likely to be bending to them.

What does say something is holding a marker out of the fit and asking the model to predict it.

`grouplab surface held-out`, on the 15 paired photographs of entry 130 section 2c. For each photograph: register as the application does with the surface model, then, for every marker the fit kept, refit with that marker's four corners excluded and measure where the refitted model puts them.

## The measurement

| | inches |
|---|---|
| held-out error, median over the 15 photographs | 0.0043 |
| held-out error at the worst marker, median over the 15 | 0.0139 |
| held-out error at the worst marker on any photograph | 0.0214 |
| the photograph gate | 0.005 |

15 of 15 fitted. 8 to 33 markers each.

## The finding, which is not what either side of the question expected

**The held-out error is the same as the fit's own residual.** The ratio of one to the other is between 0.8 and 1.0 on every one of the fifteen photographs, and 1.0 on five of them.

| photograph | markers | fit rms, in | held-out median, in | ratio |
|---|---|---|---|---|
| 165624 | 26 | 0.0055 | 0.0055 | 1.0 |
| 165627 | 25 | 0.0051 | 0.0051 | 1.0 |
| 165634 | 18 | 0.0049 | 0.0043 | 0.9 |
| 165637 | 22 | 0.0058 | 0.0058 | 1.0 |
| 165611 | 11 | 0.0031 | 0.0026 | 0.8 |
| 165617 | 11 | 0.0038 | 0.0031 | 0.8 |
| 161502 | 8 | 0.0021 | 0.0020 | 0.9 |
| 153309 | 23 | 0.0045 | 0.0038 | 0.8 |
| 153325 | 32 | 0.0046 | 0.0043 | 0.9 |
| 153333 | 16 | 0.0052 | 0.0052 | 1.0 |
| 153336 | 19 | 0.0047 | 0.0040 | 0.9 |
| 153356 | 33 | 0.0047 | 0.0045 | 1.0 |
| 153340 | 21 | 0.0057 | 0.0053 | 0.9 |
| 153344 | 19 | 0.0062 | 0.0064 | 1.0 |
| 153347 | 28 | 0.0049 | 0.0040 | 0.8 |

**The model is not overfitting.** It predicts a marker it has never seen as accurately as it reproduces one it was fitted to. Whatever is costing it 0.005 in is not flexibility spent bending to its own markers, because taking a marker away costs it nothing.

## So: do not write the thin-plate spline

Entry 130 section 6b item 2 proposed a thin-plate spline as the next model, on the reasoning that a more flexible surface would follow a real bow more closely.

**A more flexible surface fitted to the same markers cannot help.** A spline earns its keep exactly where a stiffer model is leaving structure in the residual, and there is none: the residual is already at the level of what the corners themselves can be located to. Adding flexibility to a model that already generalises perfectly would make the fit's own residual smaller and the held-out error larger, which is the one thing this measurement can see and the fit's own residual cannot.

That is question 44 answered, and the answer is to leave it. The 0.005 in is in the corner measurements, the lens, or the sheet's real shape between markers, and none of those is fixed by a spline.

## The crash, narrowed without a debugger

`20260920_153336.jpg` throws `System.IndexOutOfRangeException` under `compare-photos --model surface`, at `ExpectedImage.Render`'s `mapping.ToPage` call.

**It fitted here, cleanly, with 19 markers and a held-out median of 0.0040 in.** That narrows it usefully: the surface fit works on this photograph, and so does `ToPage` at every marker corner, twenty times over with a different marker held out each time. What this measurement does not do is call `ToPage` for every pixel of a bull's box, which is what `ExpectedImage.Render` does.

So the crash is not in the fit and not in `ToPage` over the page. It is `ToPage` at a point outside the page, where the Newton iteration in `SurfaceMapping.ToPage` is free to wander before it converges and `FoldedSheet.Sheet` is asked about a page point nothing bounded.

Left there rather than fixed, as entry 143 allows: the model is not reachable from the application, `Auto` never selects it, and the measurement above says the model should not be extended anyway. `SurfaceCrashTests` records the photograph, the command and this narrowing, so the next person starts from here rather than from the stack trace.


# Entry 129: the target upload page, the receivers, and what is waiting on the server

`docs/NOTES-FROM-PLANNING.md` entry 129, built 2026-09-23. **Sections 1, 2, 3, 5, 7 and 8.1 are built and tested. Sections 4, 6 and 8.2 need the server, and Alan's list is in the report.**

## The stack, and why

**The receiver is PHP. The worker stays Python.**

The server already runs PHP-FPM under HestiaCP for the other domain, the receiver that ran at the old address is PHP and has been taking real submissions for months, and entry 129 section 7.1 says to enable it here the same way. Rewriting a proven receiver in another language to avoid a language would have thrown away the part that was already right.

## What the receiver keeps from the old one, and what is new

Kept, every one of them: storage outside the web root, content sniffing by magic bytes rather than by extension or by what the browser said, safe stored names, a honeypot, per-address rate limits on a salted hash of `CF-Connecting-IP` taken only from Cloudflare's own ranges, the size and count caps, the disk cap with a free-space floor, a consent record written by the receiver, and a SHA-256 per file on arrival.

| new | why |
|---|---|
| quarantine, and nothing else | the old receiver stored the bytes as received; this one writes them into quarantine and the worker rebuilds each from its pixels. PHP never decodes an image |
| Cloudflare Turnstile, server side | verified before anything is written, and **refusing when siteverify cannot be reached or the secret is missing**, because refusing is the safe direction when the check itself is unavailable |
| no PDF | Alan's decision 6. It is not a photograph and the rebuild cannot handle it, which is the whole safety of the pipeline |
| 60 submissions an hour across every address | a botnet spread thin enough to stay under the per-address limit could still fill the disk |
| an `open` flag | the page is not built and the receiver is not shipped until the server can answer. A form posting to a path the server does not serve takes somebody's photographs, spends their upload and tells them nothing |

## The one that would have broken quietly

PHP's per-directory settings for grouplab.org live in `public_html/.user.ini`, because HestiaCP regenerates the FPM pool file on a template rebuild and a direct edit of one does not survive.

**That file is not part of the built site, so the sync's `rsync --delete` would have removed it on the first run after the installer put it there.** The only symptom would have been every real photograph failing to upload, with nothing anywhere saying why: nginx would have returned 413 before PHP was ever reached. `grouplab-site-sync.py` excludes it by name now, and the installer order in Alan's list puts the updated sync in before the intake, so the window never opens.

## The tests, and the two things they found

31 receiver tests, no network, on the Linux runner beside the syntax check. Cloudflare's siteverify is faked with a `file://` URL, and storage is a temporary tree that goes when the run ends.

Covered: a good submission, a PDF, a renamed executable, a mismatched extension, more than ten files, a file over the limit, **a part-way upload that still sniffs as a valid JPEG**, the honeypot, consent, a missing token, a refused token, an unreachable siteverify, a missing secret, the rate limit, and for the crash receiver a good report, one carrying a photograph, one carrying settings, an entry with a path in its name, nothing attached, one over the wall, the kill switch and its rate limit.

**Two of them found real faults rather than confirming what I had written.**

1. **The crash receiver told somebody whose upload did not attach that their report was too large.** It would have sent them away to shrink a file that was never the problem. It now distinguishes a post PHP dropped for size, where the form fields are missing too, from a request that simply carried no report.
2. **My own test was quietly testing the wrong thing.** PHP's `??` treats an explicit `null` as absent, so the case called "no secret on the server" was running against a server that had one. It passed, and it was worthless.

## The crash receiver's whitelist is now read from the receiver

The application has held the receiver's `ALLOWED_ENTRIES` as a list it could only take on trust, because the receiver did not exist. It exists, so `ReportPackageTests` reads the patterns out of it.

A pattern changed on one side and not the other would refuse every report from every installed build, and the only sign would be people saying the button does not work.

## The six waiting submissions, ingested

All six, through the existing intake tool, into `C:/Dev/grouplab-submissions/public`, **which is outside the repository and is not the website**. Nothing is published to anybody by running it.

| submission | files | hashes verified | accepted | held for a person |
|---|---|---|---|---|
| 2026-09-20_26eeb40d | 8 | yes | 7 | 1 |
| 2026-09-20_a75ba5a0 | 8 | yes | 7 | 1 |
| 2026-09-20_aa9361c8 | 7 | yes | 6 | 1 |
| 2026-09-20_c157245c | 1 | yes | 0 | 1 |
| 2026-09-20_45235a2d | 1 | yes | 0 | 1 |
| 2026-09-21_86926341 | 10 | yes | 10 | 0 |

**23 photographs were withheld by the opt-out list**, by bytes and by photograph, which is the rule working on real material.

**Why the two iPhone submissions were held: 0 GroupLab markers decoded on either.** Both are 4032 by 3024 at a 48 mm equivalent, and the automatic path can neither register nor scale a photograph it cannot find four markers on. They are not bad photographs; they are photographs of something this software cannot measure yet.

**And a finding worth the entry on its own: every single photograph carried data after the image's end marker**, between 28 KB and 280 KB of it. On the Pixel photographs that is the motion-photo payload and on the iPhone ones the depth data, and both are benign. But it is exactly the shape of the thing entry 129 section 3.5.3 exists to defeat, arriving on ordinary submissions from ordinary phones, and the rebuild removes all of it along with the GPS block every one of them also carried.

## The ledger

`C:/Dev/grouplab-submissions/ledger.json`, outside the repository because it names submissions. Each entry records pulled, hashes verified, ingested with its outcome, and whether it has been deleted from the server. All six are marked ingested and none deleted, because deletion is an SSH step on Alan's list.

`scripts/Remove-ReadSubmissions.ps1` deletes only the IDs the ledger marks ingested, and only after re-verifying the local copy against its own `meta.json`: where the local copy no longer matches, the server copy is the only good one left and it refuses to touch it.

## The donor PDFs name no address at all

Entry 129 section 6.3 asks whether the donor instructions name the old address, and whether they need regenerating.

**Neither PDF names any address, old or new.** Checked by decompressing every stream in both and searching the raw bytes as well. So no regeneration is needed for the reason the entry gives.

**But that is not entirely good news, and it is worth saying rather than passing.** A person holding the printed pack has no way to find where to send their photographs. The pack tells them to keep the files and says nothing about where they go. That is a gap the entry did not anticipate, because it assumed the PDFs named the old address; they name none. Raised here rather than fixed, because regenerating a donor pack is a design change and this entry did not ask for one.

