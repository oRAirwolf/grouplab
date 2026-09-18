# Phase 1 results

**Brief** `docs/PHASE1-BRIEF.md`
**Branch** `phase-1`
**Reproduce** each table with the command named above it

---

## Where the Phase 1 gates stand

Stated plainly, `docs/NOTES-FROM-PLANNING.md` entry 33 section 5, so that "not yet measured" is never read as "passed". The gates are `docs/PHASE1-BRIEF.md` section 2's.

| Gate | Threshold | Status |
|---|---|---|
| Conformance test 43 | 0.001 in worst bull on a synthetic raster | **Met**, and checked on every test run. Must not regress |
| Paper gate | 0.005 in worst bull on the ten printed sheets | **Met.** Ten of ten through the selected model, worst bull 0.00319 in (M1.5) |
| Photograph gate, flat | 0.005 in worst bull on `main_flat1-3` | **Not met.** The flat frames nearly pass: the frame that decoded every marker is inside on every scoring bull, and the failures are named (M1.11) |
| Photograph gate, mounted | 0.005 in worst bull on the seven usable pinned frames | **Not met: 0 of 7**, and an open requirement (M1.11). It cannot be settled until mounted GroupLab sheets are photographed, which is this weekend's paper session |
| Hole detection, point 1 | At least 25 of 27 on `300_nm_hand_load` with zero false positives | **Met,** as a reproduction of the verified survey result rather than a fresh position check (M2.1) |
| Hole detection, point 2 | 99 percent of holes with no false positives on synthetic sheets, matched at 0.15 in | **Not met,** under the amended tolerance too (M2.2) |
| Hole detection, point 3 | Centre accuracy against truth | Reported, not gated |
| Statistics | `docs/STATISTICS.md` section 15.5 | **Met on points 1 and 3 to 6**: 45,476 keys compared with nothing pending ("Entry 28"), coverage 94.73 percent, the Monte Carlo table within tolerance on all 490 cells. **Point 2 is met on the shots and not on the series**, because the two fixtures group shot 242 differently |

**Not a brief gate, and new:** the whole path from image to group now runs as one command and matches synthetic truth ("Entry 33" below).

**Not a brief gate, measured for the first time: the Phase 0 gate record on Linux and macOS**, entry 32 section 3.
- **Windows** reproduces it byte for byte.
- **Linux** reproduces every table, and its records differ only below the precision any table reports.
- **macOS** reproduces the paper and photograph gate tables, and differs in measurements 1 and 2.
- **Linux: explained,** on entry 48 section 2's three terms. **macOS: not yet explained,** and localised to S2 ("Entry 48" below).

---

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
| flat | `main_flat1.jpg` | 0.00343 / 0.00661 | 0.00443 / 0.00654 | 0.09425 / 0.00507 | 0.09425 / 0.00507 | 0 / 0 / 16 / 16 | fail / fail / fail / fail |
| flat | `main_flat2.jpg` | 0.00566 / 0.01016 | 0.01780 / 0.01134 | 0.04278 / 0.02184 | 0.07760 / 0.05044 | 2 / 5 / 25 / 8 | fail / fail / fail / fail |
| flat | `main_flat3.jpg` | 0.01183 / 0.00496 | 0.02581 / 0.00648 | 0.06230 / 0.05640 | 0.06230 / 0.05640 | 6 / 9 / 21 / 21 | fail / fail / fail / fail |
| mounted | `main1.jpg` | 0.01824 / 0.05183 | 0.01177 / 0.00620 | 0.02783 / 0.07217 | 0.02783 / 0.07217 | 8 / 8 / 11 / 11 | fail / fail / fail / fail |
| mounted | `main2.jpg` | 0.08900 / 0.06950 | 0.05971 / 0.01856 | 0.05537 / 0.07244 | 0.05537 / 0.07244 | 22 / 19 / 24 / 24 | fail / fail / fail / fail |
| mounted | `main3.jpg` | 0.06540 / 0.11379 | 0.04522 / 0.00508 | 0.05446 / 0.01612 | 0.05116 / NaN | 20 / 7 / 23 / 13 | fail / fail / fail / fail |
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

## Entry 28. Questions 12 to 14 answered, and the real upload schema

`docs/NOTES-FROM-PLANNING.md` entry 28 answers questions 12, 13 section 1 and section 3, and 14.

**Reproduce:** `dotnet test tests/GroupLab.Core.Tests --filter "ShotGroupsFixture|Publication|ImageMetadataTests"`

**Section 1: intake reads the page's real `meta.json`.** Every name the tool had guessed was wrong. It now reads schema 1 exactly as the first real submission has it.
- **Snake case throughout:** `submission_id`, `submitted_utc`, `exclude_from_public_dataset`, `consent`, `answers`, and `files` as objects with `index`, `stored_name`, `original_name`, `bytes`, `sniffed_type` and `sha256`.
- **Refused, with nothing written:**
  - any other `schema_version`;
  - an opt-out, or an opt-out field that is missing, which is unknown and not false;
  - a consent not agreed, or missing its version, time or text;
  - a file whose byte count or hash differs from the upload's, or whose `stored_name` is not a safe file name.
- **No sentinel file.** The page writes none, so the tool no longer looks for `DO-NOT-PUBLISH`.
- **Recorded in `provenance.json`:** the consent text verbatim, since a later version will say something else. Also `original_name`, which is never used as a path.
- **Empty answers are accepted.** They are the normal case.
- **The tests:** `IntakeTests` builds its submissions from entry 28's file, field for field, and refuses eleven variations of it.

**Section 2: question 12, five shots.** The panel already does it. Section 15.4 item 13 now carries the coverage table for extreme spread's two interval forms, so the number no longer lives only in a test.

**Section 3: question 14, and the difference was the fixtures' precision.**
- **Planning's probe:** `compareGroups` hands the test exactly `shots.xPOA`, and neither the Fligner-Killeen formula nor `coin` accounts for the gap.
- **What GroupLab found:** from the same written vector, its statistic was 10.073187875409229 against R's 10.075167218103388. Its medians and counts matched the probe exactly, and its normal quantiles matched an independent incomplete-gamma computation to 1e-15.
- **The cause.** `sg_dump.R` writes the JSON at 15 digits, which does not round-trip R's doubles. The aimed coordinates carry the noise of `point.x - aim.x` in exactly the bits lost, and those bits make or break ties among the absolute deviations. On raw coordinates, which are short decimals, GroupLab already matched R to 2e-13.
- **The fix.** The harness recovers each aim to six decimals from `shots.x` and `shots.xPOA`, and redoes the subtraction. All four statistics then match: `DFinch` x to 1.2e-13 and y to 3e-14, `DFcm` x to 3.4e-14 and y to 5.5e-13. Recorded as section 15.4 item 15.
- **The probe keys:** every one of the 4,414 is now compared by `sg_dump.R`'s own definitions. On `DFlandy01` that meant reproducing the misaligned pasting of section 5, below.

**The harness now.**
- **Compared:** 45,476 keys plus 600 checks that GroupLab's CorrNormal CEP is a root of its distribution.
- **Otherwise:** nothing outside tolerance, disputed or pending.

| Dataset | Compared, with root checks | Excluded |
|---|---|---|
| `DF300BLK` | 324 | 282 |
| `DFscar17` | 314 | 212 |
| `DFcciHV` | 1,181 | 694 |
| `DF300BLKhl` | 1,749 | 973 |
| `DFcm` | 5,775 | 4,778 |
| `DFinch` | 5,775 | 4,778 |
| `DFsavage` | 4,424 | 2,614 |
| `DFlandy04` | 3,351 | 2,186 |
| `DFlandy01` | 23,183 | 11,063 |

- **A slip the new keys exposed.** The rule excluding robust estimates matched any key containing "rob", and so every key of `flignerProbe`. It now leaves the probe alone.

**Section 4: question 13 section 1, a separate GPL-3.0 data repository.** The README has a "Test data" section.
- **What it says:** donated photographs live in `grouplab-testdata`, under GPL-3.0 as the consent text says. The only way in is `grouplab intake`.
- **What reads it:** `PublicationTests` reads a checkout beside this repository, or the one `GROUPLAB_TESTDATA` names. Without either it does nothing and says so in its output.
- **Not done:** the repository does not exist yet, so no URL or commit is pinned. Creating it is Alan's.

**Section 5: `compareGroups` pastes coordinates beside the wrong labels.** Recorded as section 15.4 item 14, with the binding order the probe confirmed. GroupLab pairs every coordinate with its own label.

**Also: a stated digital zoom of 0 is 1.** Every Pixel photograph in `scans/mounted/` states a DigitalZoomRatio of 0, which the EXIF standard defines as digital zoom not used. `ImageMetadata.EffectiveDigitalZoom` reads it as 1 for the lens key and the joint fit, and a missing tag stays unknown.

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

## Entry 33. The first end-to-end run

`docs/NOTES-FROM-PLANNING.md` entry 33 section 1: every stage was green and nothing had joined them.

**`grouplab analyze <image> --target <definition>` is the whole path.** `src/GroupLab.Core/Analysis/SheetAnalysis.cs` composes the same code the marking screen uses, `AutomaticMarking` and `GroupAnalysis`, so the command line and the screen cannot disagree. The stages are:
1. decode;
2. register from the printed markers;
3. detect holes inside the registered sheet;
4. assign each hole to its bull;
5. pool the offsets into one group, and compute its statistics.

**The trace.** Every stage files a `StageRecord`, and the command prints DETECTION-PIPELINE section 6.3's console form, so the console exists before any analysis screen does. Detection records each rejected candidate at its page position, and assignment records every ambiguous shot. `--json` writes the result as a marking file the marking screen opens.

**`--target` is required for now.** The sheet's identifier is printed and encoded in its codes, but nothing reads either. Guessing among built-in definitions that share marker ids is not safe.

**The gate against synthetic truth,** `EndToEndTests`.
- **The image:** GL-CF25-LTR rendered at 300 DPI, turned 0.7 degrees, scaled by 1.001 and shifted inside a larger frame, with one hole per bull at an offset the test chose.
- **The run:** the image is written to a PNG, and the command's own analysis runs on the file.
- **The result:**
  - **Recovered:** all 28 placed shots, each within the brief's 0.15 in and assigned to the bull it was placed beside, with no strays.
  - **Centre error:** median 0.0019 in, worst 0.0047 in.
  - **Pooled group:** 25 scoring shots, mean radius 0.1951 in against the placed shots' 0.1935 in.
  - **Time:** the whole analysis took 1.7 s.

**It found two integration faults that no stage test could.**
1. **A printed sheet whose definition fails today's validator could not be analysed at all.**
   - **The cause:** the Phase 0 sheets fail test 26f, which entry 13 made an error. The renderer refused them, and the hole detector indexed an empty page list and crashed.
   - **The fix:** the detector now draws the expected artwork of a sheet that already exists whatever the validator says about printing new ones. It records that decision in the trace, and fails with a reason instead of an index error. The automatic path reports it as a failed stage.
2. **Sighter shots were pooled into the group.** GL-CF25-LTR's three sighters went into its 25-shot group, because `GroupAnalysis` never read a bull's `Scoring` flag.
   - **The fix:** a bull now carries it, the marking file records it, and shots on a sighter are reported and left out, counted in `SighterShots`.
   - **Where it shows:** the marking screen shares the fix.

**On the committed Phase 0 images,** which are unshot sheets:

| Image | Registration | Holes | Stages that dominate | Total |
|---|---|---|---|---|
| `gl-cf25-ltr-1-600-dpi.png` | 34 of 34 markers, 136 of 136 corners, printed at 100.04 percent | 0, none rejected | holes 4.3 s, bull location 2.5 s, decode 0.7 s | 7.7 s |
| `main_flat1.jpg` | 34 of 34 markers, homography with radial distortion | 0, 54 candidates rejected | holes 6.6 s, bull location 1.2 s | 8.0 s |

**Where it is slow.**
- **Hole detection,** 4 to 7 s at these resolutions, is most of every run.
- **Bull location** is Phase 0's measurement locator, 1 to 2.5 s. Analysis uses its recovered centres, and it could be skipped when only the group is wanted.
- **Decoding** reads the image twice, once as grey and once as value.

Nothing has been tuned for speed, and nothing here needs to be before the weekend.

**What it is for.** This weekend's mounted sheets go straight into `grouplab analyze` on Monday.

**Entry 33 section 3: the README guard and CI.**
- **`ReadmeTests`** checks README.md's facts. Every relative link and image must resolve. The number of built-in sheets between `<!--count:sheets-->` markers must match `targets/`. The framework between `<!--framework-->` markers must match `Directory.Build.props`. No em dash may appear. Each failure names the line and says what to change.
- **`.github/workflows/ci.yml`** builds and tests on Windows, Linux and macOS on every push and pull request, and writes each platform's test counts to the run summary. Windows is required. Linux and macOS may fail until they pass.

**Entry 33 section 4 and entry 32 section 1: OpenCV's native runtime per platform.**
- **The fix:** the CLI referenced `OpenCvSharp4.runtime.win` unconditionally, so nothing restored off Windows. Each runtime is now conditioned on its platform, at the wrapper's 4.13.0.20260627:
  - `OpenCvSharp4.runtime.win` on Windows;
  - `OpenCvSharp4.official.runtime.linux-x64` on Linux;
  - `OpenCvSharp4.runtime.osx.x64` and `OpenCvSharp4.runtime.osx.arm64` on macOS.
- **The ids were checked on nuget.org.** The older `osx.10.15-x64` and `osx_arm64` packages stop at 4.6 and 4.8.
- **The first CI run passed on all three,** for the commit these changes landed in:

  | Platform | Build | Core | App |
  |---|---|---|---|
  | `windows-latest` | passed | 714 of 714, 7 m 33 s | 4 of 4 |
  | `ubuntu-latest` | passed | 714 of 714, 7 m 27 s | 4 of 4 |
  | `macos-latest` | passed | 714 of 714, 6 m 50 s | 4 of 4 |

- **`GroupLab.App`'s `WinExe`** builds, and its headless tests pass, on Linux and macOS, so it is harmless there as documented.
- **Linux and macOS are now required** in the workflow, as entry 32 section 4 asks once they pass.
- **Not yet claimed: that the Phase 0 gate record reproduces off Windows.** The suite includes conformance test 43 and every stage test, at their tolerances, and all of them pass on all three. That is agreement within tolerance, not the byte-identical comparison, or explained difference, that entry 32 section 3 requires before macOS or Linux is offered to anybody.

**Tests:** Core 714 passing, App 4 passing, none skipped.

---

## Entry 34. grouplab-testdata populated

`docs/NOTES-FROM-PLANNING.md` entry 34. [grouplab-testdata](https://github.com/oRAirwolf/grouplab-testdata) at commit `1544f1d` holds its README, `CONTRIBUTORS.md`, one donated submission and the owner's photographs, about 60 MB.

**The README answers section 1's questions in order:**
- what the photographs are and what was done to them;
- the consent text `consent_v1`, verbatim;
- what is not there;
- the size;
- how to cite it;
- how a contributor asks for removal.

"What is not there" states that opted-out submissions exist, are used for testing only and are never published. It also covers files held at intake and the owner's two held photographs.

**Donated submissions are named as the upload page names them.**
- **The rule:** `grouplab intake` named the published directory by the identifier alone. It now uses the UTC date of submission and the identifier, `Intake.DirectoryName`.
- **The check:** the first submission's own directory, `2026-09-14_1a8f39ad` from `2026-09-14T20:41:55Z`, has the same form.
- **The first submission:** it is published as a provenance record with no image. Triage decoded 0 GroupLab markers on all three photographs, so all three are held until a person looks at them and accepts them, as entry 27 section 1 requires.

**The owner's photographs, `owner/`, through a new `grouplab publish-owner`.**
- **Scrubbed like a donation:** every file goes through the same scrubber, with the original name, both hashes and what was removed recorded.
- **An owner's record, not a submission's:** the record says who took the photographs and on what terms they are published. It has no submission identifier and no consent record, as section 2 asks.
- **Refusals:** it refuses to write without both statements, and never overwrites.
- **What was removed:** every file had GPS, and the Pixel motion photographs carried 2.5 to 4.1 MB of appended video each, removed with the rest.
- **The totals:** 26 photographs published, 58 MB.

**Two photographs are held rather than published.** Entry 34 describes `scans/mounted/` as 23 of Alan's own photographs. The directory holds 28 files, and two of them cannot show who took them:
- `Screenshot_20231029-170033.png` is a phone screenshot of a photograph of a target;
- `signal-2023-07-25-20-37-40-354-1.jpg` was saved from the Signal messenger.

I looked at both, and at the four `~2` files entry 20 could not open. All six show only targets and backers, with nothing personal in view. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one costs nothing, so each is in `owner/provenance.json` with the reason until Alan confirms taking it. `publish-owner` refuses an existing directory, so publishing them later is a new `owner/` run, or a manual addition that the data test would then check.

**The originals.** They are still in `scans/mounted/` in this working tree, untracked, which is where they were. Section 2 says Alan keeps the originals outside both repositories. Moving them is Alan's to do, and until then nothing stops them being committed here by accident.

**The data test, section 5.** `PublicTestDataCarriesNoLocationNoOptOutAndFullProvenance` still does nothing without a checkout. With one, it now requires:
- a complete provenance record for every donated submission, the directory named from its date and identifier, and publication cleared;
- a provenance record for `owner/` saying who took the photographs and on what terms;
- for every image, no location, and an entry in its record at the hash it was published at;
- every file recorded as published to be present;
- no image anywhere a record does not cover;
- every credit name given to be in `CONTRIBUTORS.md`.

It passes against the checkout. `OwnerPublicationTests` covers `publish-owner` without any data.

**Wiring.** The URL and the pinned commit are in the README's "Test data" and in `CONTRIBUTING.md`. `docs/QUESTIONS-FOR-PLANNING.md` question 13 section 1 is settled by this.

**Removal requests.** The data README says a removal takes the photographs out of the current contents, that removing them from history is decided for each request, and that GPL-3.0 copies already downloaded cannot be recalled. Whether to promise a history rewrite is Alan's to decide, so the README does not promise one.

**Tests:** Core 716 passing, App 4 passing, none skipped, with the data checkout present.

---

## Entry 35. Camera originals, the ignore rule, and two steps waiting on Alan

`docs/NOTES-FROM-PLANNING.md` entry 35.

**Section 1: a file that is not a camera original is held by default.** `CameraOriginal.Problem` is the rule.
- **Two signals, either enough:**
  - a name a screenshot tool or messaging app writes, under any prefix the upload page adds: `Screenshot` or `Screen Shot`, `signal-`, WhatsApp's `IMG-yyyymmdd-WAnnnn` and Facebook's `FB_IMG_`;
  - no camera make in the file.
- **Where it applies:** both `grouplab intake`, where a person can still accept a held file by name, and `grouplab publish-owner`.
- **No false holds on the real files:**
  - all 26 published owner photographs pass, including the four edited `~2` Pixel copies, which keep their camera make;
  - none of the donated submission's three files is flagged, and they stay held for triage's reason alone.
- **The two held photographs:** `scans/mounted/` is no longer in this working tree, so the rule could not be rerun on them. `CameraOriginalTests` checks both of their exact names.
- **A small fix found on the way:** `publish-owner` crashed on a missing source directory, and now refuses with the reason.

**The two held photographs are held permanently,** at `grouplab-testdata` commit `c80055c`. `owner/provenance.json` and the data README give the reason: neither is a camera original, so neither can serve the corpus's purpose, whoever took it. If an original turns up, the original is what would be published.

**Section 2: not done.** The README's removal wording stands, with no promise of a history rewrite. This session's permission check refused the edit that adds a warning that the data repository's history may be rewritten, so the warning waits for Alan.

**Section 3: not done, and waiting on Alan.**
- **Why:** this session's permission check refused deleting the five `refs/original` refs as irreversible local destruction. Nothing was deleted, and the risk entry 35 names is still present.
- **What was checked first:**
  - **The backup:** `C:\Dev\grouplab-backup-2026-09-15.bundle` exists, 166,677,290 bytes. `git bundle verify` reports a complete history. It holds sixteen refs, among them all five `refs/original` refs at the same commits as the local ones.
  - **The pack before any change:** 159.74 MiB in 3 packs, plus 30.50 MiB loose.
- **Once allowed,** the steps are:
  1. Delete each `refs/original/*` ref.
  2. Expire every reflog.
  3. Garbage collect, pruning unreachable objects immediately.
  4. Confirm the five old commits are no longer in the object store.
  5. Report the pack size again.

**Section 4:** `scans/mounted/` is in `.gitignore`.

**Section 5:** `CONTRIBUTING.md` now says that a stage passing its own tests is not evidence that the pipeline is right, and that `EndToEndTests` speaks for the product.
- **The example it gives:** the sighter fault it found, and what that fault had survived.
- **What it asks:** when a stage is added, or what its output means changes, extend that test as well as the stage's own.

**Section 6, items 2 and 3:** reported in "Entry 35 section 6" below.

**Tests:** Core 724 passing, App 4 passing, none skipped.

---

## Entry 37. The opt-out file restored, and consent that wins by content hash

`docs/NOTES-FROM-PLANNING.md` entry 37 sections 1 and 2. Sections 3 to 5 wait, as its section 6 orders.

**Section 1: the `DO-NOT-PUBLISH` check is back.**
- **Its history:** the intake gate of entries 22 and 27 checked for it. Entry 28 section 1 said the page writes no such file, and the check was removed. The first opted-out submission, `2026-09-15_eac0bae6`, carries one.
- **Either signal withholds:** a submission is refused when the file is present or `exclude_from_public_dataset` is true. No agreement between them is required.
- **A disagreement is named:** a file present beside a field reading false is refused with a statement that the two signals disagree.
- **Order:** both signals are checked before the schema version, so an opted-out submission is always reported as opted out.
- **Unchanged:** a missing field is still refused as unknown.

**Section 2: an opt-out wins by content hash, across every submission.**
- **The set:** `Intake.WithheldHashes` reads every submission directory before anything is published. It maps every file hash in each submission that is not plainly publishable to that submission's identifier, counting both the bytes on disk and the hashes `meta.json` recorded.
- **What counts as withheld:** a submission whose opt-out is set by either signal, is missing, or whose `meta.json` cannot be read. Withholding costs nothing, and publishing under ambiguous consent cannot be undone.
- **Required, not optional:** `Intake.Run` takes that set as a required argument, so no caller can publish without it.
- **What a match does:** a file whose bytes are in the set is held whatever triage says, and accepting it by name does not override that. Its entry in the provenance record carries `optedOutIn` with the withheld submission's identifier, beside the record's own `submissionId`.
- **Amended 2026-09-16, entry 58 sections 3 and 4:** the set holds a second key beside the bytes, what each withheld file scrubs to, so another export of the same photograph is held too. A byte hash alone missed that, and the four uploads of 16 September are four hashes of one picture. See "Entry 58 sections 3 and 4".
- **Where the opt-outs are read from:** `grouplab intake` reads them from the directory holding the submission, or from `--submissions`, and prints how many hashes it withheld.

**On the four real submissions**, run into a scratch directory:

| Submission | Result |
|---|---|
| `2026-09-15_eac0bae6` | Refused: `exclude_from_public_dataset` is true and a `DO-NOT-PUBLISH` file is present. Its 9 files are the withheld hashes |
| `2026-09-15_5068047f` | Its one photograph, `001_IMG_1580.jpg`, held for a consent conflict naming `eac0bae6` |
| `2026-09-15_bf6d885d` | Its one photograph, `001_IMG_1696.jpg`, held for a consent conflict naming `eac0bae6` |
| `2026-09-14_1a8f39ad` | Unchanged: its three photographs are held by triage |

**Nothing was published to `grouplab-testdata`.**
- **What publishing would add:** each new publishable submission would be a provenance record with no photograph, carrying the contributor's answers and credit name.
- **Why not yet:** a public record of a submission whose consent is in question waits until Alan has asked the contributor which they meant.

**Worth knowing for when consent is settled.** Triage decoded no GroupLab markers on either photograph, because both are commercial Action Target sheets. Entry 27 section 1's triage therefore holds them until a person accepts them by name, even without the conflict. So frames entry 37 calls the case the project has never had can reach the public data only by a person's acceptance.

**Tests:** Core 725 passing, App 4 passing, none skipped.

---

## Entry 36. The shotGroups fixtures at full double precision

`docs/NOTES-FROM-PLANNING.md` entry 36. Planning regenerated the fixtures at 17 significant digits, and this session verified them independently, removed the harness's workaround and committed them. `tools/` was not edited.

**The regenerated values, against the committed fixtures, on all ten files rather than entry 36's three:**

| | |
|---|---|
| Keys, nine datasets | 73,086 |
| Keys missing or added | 0 |
| Stored numbers whose bits changed | 18,491 |
| Largest relative change of any stored number | 5.53e-16 |
| Numbers moved by more than 1e-14 relative | 0 |

**Every difference is digits appearing, not a value moving,** as entry 36 section 3 measured.

**CSV and JSON agree bit for bit** on 71,056 numeric values across the nine datasets, with three exceptions. All three are negative zeros in `DFlandy01`, such as `shots.y.303`, which the CSV writes as `-0` and the JSON as `0`. They compare equal as numbers, and no statistic here can tell them apart.

**The reconstruction is removed, and the harness passes from the fixture alone.**
- **The change:** `ShotGroupsFixtureTests` now reads `shots.xPOA` and `shots.yPOA` directly. All 67 statistics tests pass, including the four Fligner-Killeen keys question 14 was about.
- **How the reconstruction compares with the true stored values, entry 36 section 4 point 2:**
  - identical on 3,775 of 3,978 coordinates, across every dataset but one;
  - off on 203 coordinates, all in `DFcm`, by at most 3.6e-15.
- **Why `DFcm`:** the reconstruction assumed each aim is a number with at most six decimals. `DFcm` is in centimetres, where that does not hold.
- **Which was right:** the stored values are R's own doubles, so they were right, and the reconstruction was an approximation that happened to be close enough for every key it served.

**A defect in the regeneration: `shotGroups_DFdistr.json` holds its table as strings.**
- **The cause:** `sg_distr.R` formats the table's double columns with `sprintf("%.17g")` for the CSV. It then builds the JSON from the same data frame, so all 8,850 numeric table values are strings, such as `"ES_M": "1.7727261017613225"`. Only `inSection15_3Gate`, an integer column, stayed a number. `sg_dump.R` avoided this by keeping the numeric values aside before formatting, and `sg_distr.R` needs the same.
- **The CSV** is correct at 17 digits.
- **What was committed:** nothing in the test suite reads `DFdistr`, and `tools/` is planning's to change. So `sg_distr.R` and both `DFdistr` files are left out of this commit, at their committed 15-digit versions, and `docs/STATISTICS.md` section 15.4 item 15 says so.

**`docs/STATISTICS.md` section 15.4 item 15** is amended, not deleted. It records:
- what the defect was;
- that it was fixed on 2026-09-15;
- that `digits = NA` would not have fixed it;
- the checks above;
- that question 14's four keys were its only known casualty.

**Tests:** Core 725 passing, App 4 passing, none skipped.

---

## Entry 38. `DFdistr` numeric, and no negative zeros

`docs/NOTES-FROM-PLANNING.md` entry 38, which closes entry 36's one exception. Planning corrected `sg_distr.R` and regenerated `DFdistr` and `DFlandy01`. This session verified the files independently and committed them.

**`shotGroups_DFdistr`:**
- **Types:** 590 rows and 9,440 table values, 2,360 integers and 7,080 doubles, with no strings.
- **Agreement:** the CSV and the JSON agree bit for bit on every one of the 9,440 values.
- **Against the committed 15-digit file:** 1,947 values gained digits, and the largest relative change is 4.44e-16. No other field changed.

**`shotGroups_DFlandy01`:**
- **CSV:** exactly three keys changed, each from `-0` to `0`: `shots.y.303`, `shots.yPOA.303` and `flignerProbe.FlignerY.input.243`.
- **JSON:** no value changed, because jsonlite had already written those three as `0`. Only the generation time moved.
- **Agreement:** the CSV and the JSON now agree bit for bit on every numeric value.

**No fixture CSV holds a `-0`.** All 67 statistics tests pass on the regenerated files, and nothing else in the suite reads them. `docs/STATISTICS.md` section 15.4 item 15 now describes both corrections.

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

## Entry 42. The styling pass

`docs/NOTES-FROM-PLANNING.md` entry 42: the concept screens' palette, type, density and marks, written into code. It changes no number and no behaviour. Every test that existed before it passes unchanged.

**Where it lives,** as section 6 asks:
- **`src/GroupLab.App/Theme/Tokens.cs`:** both palettes, the type scale, the spacing scale and the mark colours. No colour literal appears anywhere else in the application, and `ThemeTests.NoColourLiteralAppearsOutsideTheTokens` fails if one does.
- **`src/GroupLab.App/Theme/AppStyles.cs`:** the control styles, and the Fluent theme's own state colours, pointer over, pressed, checked and focused, set from the palette so hovering a button cannot bring back a Fluent grey. The styles are rebuilt whenever the resolved theme changes.
- **`src/GroupLab.App/Theme/Marks.cs`:** how every mark on the image is drawn. The composite plot of entry 43 will use the same code.

**Themes.** Dark, light and follow system, chosen under Theme and remembered with the units. High contrast is `DESIGN.md` section 19's fourth theme, and later work.

**Contrast.** Six text colours from the concept screens fall below 4.5:1 on the surfaces text sits on: `bg`, `panel` and `panel2`. Section 2 says to adjust the value rather than the role, so each is moved along its own hue, toward white in the dark theme and toward black in the light, by the smallest step that reaches 4.5:1:

| Theme | Role | Concept screen | Now | Worst ratio now |
|---|---|---|---|---|
| dark | `faint` | #697079 | #858b92 | 4.53 |
| dark | `alert` | #e0604a | #e1634d | 4.52 |
| light | `faint` | #868c94 | #666a70 | 4.52 |
| light | `amber` | #b46f16 | #965d12 | 4.51 |
| light | `teal` | #3f8873 | #367462 | 4.56 |
| light | `alert` | #bf4531 | #b8422f | 4.52 |

**Two light values were left unstated by entry 42, and are chosen here.**
- **Primary button text:** white on the adjusted amber (5.4:1), because #17120a on it is 3.4:1.
- **Selected and good tints:** amber and teal nine tenths of the way to white, on which amber and teal text reach 4.7:1 and 4.8:1.

`ThemeTests` checks every pair.

**Type.** IBM Plex Sans, Sans Condensed and Mono are embedded as resources, with the SIL Open Font License shipped beside the application and listed in `THIRD-PARTY-NOTICES.md`.
- **Section labels:** uppercase at 10 point semibold, spaced, in `faint`.
- **Figures:** mean radius leads at 29 point mono and sigma follows at 21. Every interval line is mono at 11.5 in `dim`.
- **Shot rows:** mono.

**Layout.**
- **Structure:** the toolbar sits on a bar with a one pixel separator; the right column is 372 wide; a status line runs along the bottom.
- **Spacing:** section padding is 12 by 14, and every spacing value is from the scale.
- **Buttons:** radius 4, padding 6 by 12, 12 point at weight 500. The current tool is amber on its tint.
- **The scale's pill:** teal when the sheet registered, amber for a reference drawn by hand. The navigation rail is not built, as section 4 says.

**The marks,** section 5:
- **The stroke:** every mark is a two tone stroke, 3 pixels of near black at 55 percent under its colour at 1.6 pixels, in screen pixels at any zoom.
- **An impact:** a ring at the true hole diameter once the calibre and the scale are known, with a one pixel pip, in `impact`. Selected is amber at 2 pixels; excluded is `dim` and dashed.
- **Other marks:**
  - the scale reference is a teal line with a filled circle at each end;
  - the point of aim is a teal cross, not a circle;
  - a bull centre is a small `faint` cross;
  - a missing marker keeps its cross, in `alert`.

**Checked by eye, on screenshots the test writes to `out/screens`,** section 7 points 2 and 3:
- **`marking-dark.png` and `marking-light.png`** show the whole window in each theme.
- **`marks-closeup-dark.png` and `marks-closeup-light.png`** show bull 13 of `gl-cf25-ltr-1-600-dpi.png`, detected, with a .308 calibre and three impacts: on bare paper, on the inner printed ring, and on the printed "13".
  - **Readability:** all three rings read, as does the point of aim over the bull.
  - **The warning:** the size check on the ring's impact now says it sits on the printed target, because the window holds the detected artwork.
- **`photo-marks-dark.png`** is marks on the donated `001_IMG_1696.jpg`, in hard sunlight, on paper, printed rings and the dark backer. That photograph has no shadow across it, so the shadow case section 7 names is not yet looked at.

**The screenshots are for planning to set beside `docs/figures/screens`.** The content differs, and entry 43 is that difference. They are in the working tree's `out/screens`, which git ignores.

**Where this departs from entry 42,** and why:
1. **Units are not yet set smaller than their figures.** "0.1046 in" is still one run of text at the figure size. Setting the unit at 13 point means splitting the text into runs, and the figure text is what the existing tests read, which section 1 says must pass unchanged. The figure stack of entry 43 is the place to do it.
2. **The image no longer colour codes a shot's provenance.** Section 5 gives one impact colour, where automatic, corrected and manual shots were gold, orange and green. Provenance is still in the selected shot's panel and the report, but not on the image or in the shot list's rows. Whether it should come back is a design question.
3. **Mark labels are light text on a dark plate, with a bar in the mark's colour.** The first screenshots drew a label in the mark's own colour on the halo, and red on near black at 11.5 point could not be read.

**How the frames are captured.** The headless tests now draw through Skia rather than the null renderer, so every app test renders for real. CI on Linux and macOS is the check that Skia's native libraries load there.

**Tests:** App 14 passing, none skipped. The 10 that existed are unchanged, and the new ones are contrast, colour literals, the theme choice, and the screenshots. Core is unchanged at 729.

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

## Entry 46. The alert ring at the measured size, and a real run's log

`docs/NOTES-FROM-PLANNING.md` entry 46 sections 0 to 2. The rest of the entry is reported below as it lands.

**Section 1: the impact ring is the calibre.**
- **What the code did:** `MarkingCanvas` drew every impact ring at the calibre's diameter at the image's local scale. So every ring was the same size, whatever the hole measured, and entry 42 section 5's "true hole diameter" was not what it drew.
- **The close-up, measured by the screenshot test:** the three impacts on bull 13 draw at 0.308 in. Shot 2 reads 0.508 in across. Its alert ring sat 6 screen pixels outside its impact ring, which at the close-up's zoom is a few hundredths of an inch and says nothing about size.
- **The change:** the impact ring stays at the calibre, which planning offered to write into the entries. A flagged hole's alert ring is drawn at the extent it reads, half of it as the radius, and never closer than 6 pixels outside the impact ring. On shot 2 that is 0.508 in against 0.308, so two holes marked as one look wrong on the image.
- **Why only flagged holes:** the size check reads a hole's extent only where it finds a dark region on paper within two diameters. On ink or a dark backer it reads nothing, so a ring at the measured size for every shot would fall back to the calibre on exactly the marks where the question matters least clearly.
- **Test:** `ScreenshotTests` asserts every impact ring at 0.308 in and the flagged ring at the size its flag reads, and writes the figures to `out/screens/marks-closeup-rings.txt`.

**Section 2: a real run writes its log.** The window screenshots come from the headless test, which starts no log, so the panel there says logging has not started. The explicit check was a real run:
- **The build:** a Debug build run from its place in the repository.
- **The run:** driven through UI Automation. It clicked Open image, typed the path of `scans/phase0/gl-cf25-ltr-1-600-dpi.png` into the real file dialog, waited, and closed the window.
- **The result:** `out/logs/grouplab-20260915-180437-32256.log`, which reads in full:
  - `app.start`, with `channel=debug` and `logdir=<repository>/out/logs`;
  - `app.window`;
  - `dialog.open` and `dialog.result chosen=True`;
  - `image.open file=gl-cf25-ltr-1-600-dpi.png pathid=ab0b3ee4`, with format, size and resolution;
  - `app.exit code=0 seconds=25.5`.
- **Why the directory was empty:** `out/logs` had not existed until this run. The only build that had been run from the repository was the copy Alan had open, built before logging existed.

**Tests:** App 31 passing, none skipped. Core unchanged.

---

## Entry 35 section 6. The sheet names its own definition, and the gate record on three platforms

`docs/NOTES-FROM-PLANNING.md` entry 35 section 6 items 2 and 3, the order entry 46 section 4 gives.

### Item 3: the definition read off the sheet

`grouplab analyze <image>` no longer needs `--target`, and the marking screen's Detect asks for a definition only when the sheet's codes cannot give one.

**How.**
- **The codes, not the markers:** every GroupLab sheet prints its definition as a GLTD-B frame in each QR code, and the identifier is computed from the frame's body. So a frame that passes its CRC names exactly one definition. The built-in definitions share marker ids, which is why nothing is chosen from the markers.
- **`SheetIdentification`, in Core:**
  - it reads the codes at full, half and quarter resolution and stops at the first that yields a valid frame;
  - it finds the definition with that identifier among the candidates;
  - it records `S0.identify` in the trace.
- **What it refuses, with the reason:** codes naming two definitions or two tiles, a damaged frame, and an identifier not among the candidates.
- **Where the candidates come from:**
  - **The command:** every `*.gltd.json` under `--library` directories, by default `targets`, frozen definitions included.
  - **The application:** the definitions shipped beside it, which now include the three frozen Phase 0 definitions, so the sheets Alan has already printed are recognised. The print screen's list does not look in the frozen directory.

**Reading binary QR codes through OpenCV took two detectors.** GLTD-B frames are binary, and both of OpenCvSharp's QR decoders return strings. Measured on the Phase 0 scans before choosing:
- **The WeChat detector,** without its neural network models, finds codes reliably. But it returns the payload through UTF-8, so every byte above 0x7F came back as U+FFFD and no frame passed its CRC.
- **The plain `QRCodeDetector`** returns one byte per character, which Latin-1 turns back into the frame exactly. But on its own it missed the codes on the 600 DPI scan, a tile and a photograph it was tried on.
- **So the backend locates with WeChat and decodes with the plain decoder at the corners WeChat found,** and adds whatever the plain detector finds alone.

**On the 37 Phase 0 images, 29 name the definition they were printed from, and none names a wrong one.**

| Images | Identified | At |
|---|---|---|
| Letter reference sheets, 300 and 600 DPI, including the 96.2 percent and blank data block renders | 10 of 13 | mostly full resolution; 600 DPI sheets 1 and 3 at quarter and half |
| `GL-LR300-T` tiles, 300 and 600 DPI | 8 of 8, each with its own tile index | full |
| The four frames of 13 September | 4 of 4 | full |
| `main1-3`, `main_flat1-3` | 6 of 6 | full |
| `telephoto1-3`, `ultrawide1-3` | 1 of 6 | full |

- **The eight that are not identified:**
  - the 600 DPI scan turned 180 degrees;
  - the 300 DPI 96.2 percent scan;
  - the 300 DPI filled data block scan;
  - five of the six wall photographs.
- **What happens to them:** they fall back to naming the definition, with the reason. Nothing was tuned to raise the count, which would fit the reader to the frames it is reported on.
- **Speed:** a readable code at full resolution costs 0.2 to 3.4 s. A 600 DPI scan that needs every resolution costs about 8.5 s.

**Checked on a real run.** The Debug application was driven through UI Automation: open `gl-cf25-ltr-1-600-dpi.png`, press Detect, close. The log reads:
- `detect.identify definition=GL-YCSK-DZZ1-R0VJ-4T5Y tile=0 codes=1`;
- then `detect.run`, with 34 of 34 markers and registration RMS 0.0022 in.

No picker opened.

**Tests.**
- **`SheetIdentificationTests`:**
  - three printed scans and a photograph, one of them a tile;
  - four fresh renders of built-ins, one of them tile 3 of `GL-LR300-T`, which CI decodes on all three platforms;
  - refusals through a fake backend: two definitions, two tiles, an identifier not among the candidates, a damaged frame and no codes.
- **`EndToEndTests`:** reruns the rendered sheet without a definition and requires the identical shots.

### Item 2: the Phase 0 gate record on three platforms

**The workflow.** `.github/workflows/gate-record.yml` runs on every push, on Windows, Linux and macOS:
- it reruns the eight Phase 0 spike commands;
- it compares every record byte for byte with the commit, leaving out only `threshold.json`'s `detectMs`, a detection time;
- it hashes each console table into the run summary.

**Before the first run, locally on Windows.**
- **Seven records** reproduced exactly.
- **`photos.json`** lacked the two camera fields the code has written since the lens work, and is regenerated. No measured value in it changed.
- **The newline:** the records were written with the platform's newline, CRLF on Windows and LF elsewhere. They are now written with LF everywhere, so the bytes on disk are comparable.

**The first run: Windows reproduces the record exactly; Linux and macOS do not.** The step that should have said how they differ stopped at the first difference, because the runner's shell ends a step on a failing pipeline. It now reports how many values differ, the largest difference under each field name, and anything that is not a number. The second run is the comparison below. Every run now also uploads each platform's records and console tables, so the comparison can be checked.

**Windows, `windows-latest`:** all eight records byte for byte, as locally.

**Linux, `ubuntu-latest`, x64: every console table is identical to Windows, line for line.** The records differ in every file, all below what any table reports:

| Record | Values that differ | Largest difference |
|---|---|---|
| `sheets.json` | 254 | 0.0001 dmm in a bull error; scales below 1e-10 |
| `photos.json` | 285 | 0.0006 dmm in a bull position; the lens model's frame edge 0.0018 px |
| `markers.json` | 289 | 0.0071 dmm, one bull in one random marker subset |
| `refinement.json` | 1 | 0.0001 dmm |
| `threshold.json` | 3 | 0.0001 dmm |
| `scale.json` | 62 | below 1e-10 |
| `field.json` | 85 | 0.0001 dmm; a correlation by 4e-13 |
| `detectors.json` | 100 | 0.0001 dmm |

**macOS, `macos-latest`, arm64: the paper gate and photograph gate tables, `sheets` and `photos`, are identical to Windows.** 20 console lines differ, all in two studies:
- **Measurement 1, marker count:** one figure, 0.00412 against 0.00411 in, for 12 markers on sheet 3.
- **Measurement 2, corner refinement, on paper:** three rows change in the fourth or fifth decimal place of an inch. The largest change is the 300 DPI quarter-module window, 0.00836 against 0.00887 in.
- **Measurement 2, on the synthetic raster:** all 16 rows change. The windows of 1.5 and 2 modules, which the study shows are broken, change most, up to 0.01195 against 0.00845 in. The shipped window changes least: corner RMS 0.159 against 0.158 px at 600 DPI. At 300 DPI its bias is -0.120 against -0.119 px and one bull figure 0.00028 against 0.00029 in.

**In the records, macOS differs in more values than Linux.**
- **Counts:** 330 in `sheets.json`, 541 in `photos.json`, 710 in `markers.json` and 15,813 in `refinement.json`.
- **Most of the last:** the synthetic raster's corners listed in a different order, which is the marker detector returning the markers in another order.
- **The largest move of a bull anywhere:** `telephoto3.jpg`'s scoring bull 24, 6.7277 against 6.4236 dmm (0.0265 against 0.0253 in), where one ray lost its edge point, 30 to 29. That frame is excluded from the gate because the sheet overflows it, which is why no table shows it.

**What this establishes, and what it does not.**
- **Every figure the Phase 0 gates report reproduces to its printed precision on all three platforms.**
- **The records are not byte-identical off Windows,** so entry 32 section 3's first condition is not met. Whether the differences count as explained is planning's decision. Until then the workflow fails on Linux and macOS, which is the truthful state.
- **Two sources, not separated here.** The differences can come from the native OpenCV in each platform's runtime package, which is a different build on each, or from each platform's maths library, which .NET's `Math` functions call. This comparison cannot tell them apart. The reordered markers on macOS can only have come from OpenCV.

**Tests:** Core 740 passing, App 31 passing, none skipped.

---

## Entry 37 sections 3 to 5. What the donated submissions showed, and the sheet size the contributor stated

`docs/NOTES-FROM-PLANNING.md` entry 37 sections 3 to 5. Nothing is published: the consent conflict of section 2 still holds both photographs.

### Section 3: the wording of the request, measured

The four submissions' `meta.json` answers, counted:

| Submission | When | Answers filled | Photographs | What happened to them |
|---|---|---|---|---|
| `2026-09-14_1a8f39ad` | Before the post was edited | 0 of 6 | 3 | Held by triage |
| `2026-09-15_5068047f` | After | **6 of 6** | 1 | Held for the consent conflict |
| `2026-09-15_bf6d885d` | After | **6 of 6** | 1 | Held for the consent conflict |
| `2026-09-15_eac0bae6` | After, opted out | 3 of 6 | 9 | Withheld |

The two complete submissions give backing, attachment, distance, calibre (5.56 NATO and 8.6 Blackout), a credit name, and in the notes the exact commercial target. Planning read both photographs as meeting the brief: whole targets, still stapled to the backer, with all four edges in frame. **The difference between a useless submission and a good one was the wording of the request, not the contributor.**

### Section 4: what an iPhone upload keeps

**The browser was Chrome for iOS, not Safari.** All three later submissions carry the same user agent: iOS 18.7.10, `CriOS/152`. Chrome on iOS uses WebKit, so the upload page's `accept` attribute is answered for WebKit's file picker on an iPhone. Safari itself has still not been used.

**Amended 2026-09-16, entry 58 section 1: read nothing from a user agent on iOS.** DuckDuckGo reports itself as Safari, so the field cannot tell those two apart, and no absence of a browser can be concluded from it. Four deliberate uploads later settled what actually happens: every iOS browser preserves the file, and a photograph captured inside the page is what loses the camera data. See "Entry 58 sections 3 and 4".

**The metadata survives the upload.** Checked on both publishable photographs through `grouplab scrub`, into a scratch copy that was then deleted, printing names and never values:
- **Present in each original:** a `LensModel` tag, and a GPS block.
- **What scrubbing removes:** the GPS block, APP10, the thumbnail, and 36 other EXIF fields.
- **What scrubbing keeps:** ten fields, `Make`, `Model`, `Orientation`, `ExposureTime`, `FNumber`, `ISOSpeedRatings`, `FocalLength`, `PixelXDimension`, `PixelYDimension` and `FocalLengthIn35mmFilm`.

**`LensModel` is not among them.** The scrubber's keep list is entry 29's, and `LensModel` is not on it, so a published iPhone photograph will not carry "iPhone XS Max back dual camera 4.25mm f/1.8".
- **Lens grouping is unaffected:** it is built from the focal length, f-number, 35 mm equivalent, digital zoom and image size, which are kept.
- **What is lost:** only the lens's name. The received files, which are never published, keep it.
- **The keep list is unchanged.** It is entry 29's decision, so it stays until planning asks for `LensModel`.

### Section 5: the stated sheet size, structured

**`statedSheetSize` in the provenance record.** `grouplab intake` reads the notes with `StatedSheetSize.Parse` and writes the size beside the answers, which stay as given: `source` (`answers.notes`), `text`, `width`, `height` and `unit`.
- **What counts as a size:** two numbers joined by x or ×, followed by a unit. The unit is in, inch, inches, an inch mark, mm or cm.
- **The inch mark:** both real notes end in U+201D, the curly quote an iPhone keyboard types for `"`, and it is read as one.
- **Nothing is written for:**
  - a note with no size, or with two;
  - a size with no unit, because a guessed unit is a scale error of 2.54 or 25.4 times.
- **On the real notes:** `5068047f` gives 17.5 by 23 in, and `bf6d885d` gives 23 by 35 in.

**The marking path offers it.**
- **When it appears:** opening an image that sits beside a provenance record listing it by name, as `grouplab-testdata` publishes it, says the record gives the sheet's size. The rectangle tool then offers "Use the stated sheet size" beside its own boxes.
- **What the button does:** it fills the width and height in the user's unit, in the order the contributor wrote them. It asks the user to tap the sheet's own corners, and to swap the numbers if the first side tapped is the other one.
- **What it never does:** set a scale by itself.
- **No lookup table:** there is none of third-party target sizes, and the size is used as the contributor stated it.

**Tests.**
- **`StatedSheetSizeTests`, Core:** seven sizes read, six notes that give nothing, the metric conversions, and the record read beside an image.
- **`IntakeTests`:** the structured size beside the notes as given, and none from empty notes.
- **`StatedSheetSizeTests`, App:** the offer fills 17.5 and 23 in inches, and an image without a record is offered nothing.
- **Counts:** Core 756 passing, App 32 passing, none skipped.

---

## Entry 46 section 3. Shot provenance in the panel, not on the image

`docs/NOTES-FROM-PLANNING.md` entry 46 section 3, planning's decision on the provenance colours entry 42 dropped from the image.

- **Not on the image.** The marks are unchanged: colour there means selected and excluded, and amber means "this one".
- **A count line above the figures.** It names only the kinds of placement present, for example "12 shots: 9 detected, 2 corrected, 1 placed by hand." Any exclusions and shots marked not a shot follow on the same line. The "Placed: automatic, corrected, by hand" line that sat at the foot of the panel is removed, because it said the same thing below the figures instead of above them.
- **A provenance column in the shot list.** Each row carries "detected", "corrected" or "by hand" in faint. That uses a new `faint` text style over the palette's faint colour, which the contrast test already holds to 4.5:1 on every surface in both themes.
- **Why it matters, as planning put it:** provenance is the record of where a human judgement entered a measurement. A group of nine detected shots and one of nine placed by hand deserve the same figures and a different amount of confidence.
- **Tests:** `ProvenanceTests` checks the count line names only the kinds present. It then builds two detected shots, moves one so it counts as corrected, places one by hand, and requires the count line, no "Placed:" line, and a faint word on each row in order. App 34 passing, none skipped.

---

## Entry 47. Build and test red on Linux and macOS: the cause, and identification measured per platform

`docs/NOTES-FROM-PLANNING.md` entry 47.

**What went red.** `build and test` was green on all three platforms at `c622591`, and red on Linux and macOS at `a97bcb0` and `b0091ad`. Three tests were involved, all about identifying a sheet from its codes:
- **Linux:** the clean 300 DPI render of GL-CF25-LTR, and the end-to-end test's rerun without `--target`.
- **macOS:** the end-to-end rerun alone.

It is green again on all three at `eddee00`, with Core 756 and App 34 on each.

**Not a missing contrib module.** Planning's diagnosis was that the Linux and macOS runtime packages lack `opencv_contrib`'s `wechat_qrcode`. Checked two ways:
- **The packages, opened.** `libOpenCvSharpExtern.so` in `OpenCvSharp4.official.runtime.linux-x64` 4.13.0.20260627 and `libOpenCvSharpExtern.dylib` in `OpenCvSharp4.runtime.osx.arm64` both carry the `wechat_qrcode_WeChatQRCode` exports, as `OpenCvSharpExtern.dll` in the Windows package does.
- **The logs.** Each failure read "no code on the sheet could be read", not a missing entry point or a type initializer. On the same runs, identification passed on Linux and macOS for three printed scans, one of them a tile, and for a photograph. That could not have happened had the constructor thrown.

**The cause: detection, on images whose code modules are under five pixels.** A 300 DPI code module is about 4.7 px. The WeChat and plain detectors in the Windows build read both synthetic images at full resolution; the Linux and macOS builds did not. Half and quarter resolution only shrink the modules.

**The fix, `b315853`.**
- **Double resolution is tried second,** upscaled linearly.
- **A failing identification test now says what went wrong:** for each resolution, it reports the WeChat boxes and texts, the plain decoder at those boxes, and the plain detector alone. A failure that happens only on a runner then says which step came back empty.

**Doubling had a cost on large images, now bounded.** On the first Windows sweep after the fix:
- **`gl-cf25-ltr-1-600-dpi.png`:** 52.7 s. It doubled a 4958 by 6458 px scan before reading at quarter resolution.
- **Where no code is read:** 16 to 47 s.

A resolution that would make the image longer than 8000 px, a 4000 px photograph doubled, is now skipped and recorded as skipped. The same sweep then names the same images, and the 600 DPI scans take 0.7 to 9.0 s.

**Identification measured per platform.** `grouplab identify sweep` runs every Phase 0 image through identification against the whole of `targets`, and counts against the definition and tile each was printed from. It runs in the gate record workflow on each platform and puts its table in the run summary. It fails only on a wrong name, since a refusal is the safe outcome.

**On Windows: 33 of 37 named correctly, 0 wrongly, 4 not named.** That is up from 29 before double resolution:

| Images | Named | Read at |
|---|---|---|
| Letter reference sheets, 600 DPI | 6 of 7 | full; sheets 1 and 3 at quarter and half |
| Letter reference sheets, 300 DPI | 6 of 6 | full; the 96.2 percent and filled data block scans at double |
| `GL-LR300-T` tiles, 300 and 600 DPI | 8 of 8, each with its tile | full |
| The four frames of 13 September | 4 of 4 | full |
| `main1-3`, `main_flat1-3` | 6 of 6 | full |
| `ultrawide1-3` | 3 of 3 | full; `ultrawide1` and `ultrawide2` at double |
| `telephoto1-3` | 0 of 3 | none |

- **Not named:** the 600 DPI scan turned 180 degrees, and the three telephoto frames.
- **Time for a readable sheet:** 0.2 to 9.0 s, except `ultrawide2` at 16.5 s.
- **Time where no code is read:** 8.5 s for the rotated scan and 15.5 to 17.9 s for the telephoto frames, which a 4000 px photograph spends at double resolution.
- **Measured on the three CI runners at `826bd15`, none naming a wrong definition:**

  | Platform | Named correctly | Not named |
  |---|---|---|
  | `windows-latest` | 33 of 37 | the rotated 600 DPI scan, `telephoto1-3` |
  | `macos-latest`, arm64 | 33 of 37 | the same four |
  | `ubuntu-latest`, x64 | **32 of 37** | the same four, and `gl-cf25-ltr-1-600-dpi.png` |

- **The one platform difference:** Linux does not name `gl-cf25-ltr-1-600-dpi.png`, which Windows and macOS read only at quarter resolution. Named with `--target`, that scan registers on Linux as it does on Windows: the sheets table of the gate record is identical there. So identification works on all three platforms, and is one sheet short on Linux; it is not the same on all three.
- **The runners are about twice as slow as this machine:** an image that gives no code took 4.1 to 45.4 s there.

**The rule of entry 47 section 4 is taken.** When a required check goes red, the next commit makes it green or deliberately reverts, and an expected red, such as the gate record's, is named in the commit message. `fd05dac` and `eddee00` went up in the same push as the fix, before its CI result was known. Under the rule, the fix would have gone alone.

**Tests:** Core 756 passing, App 34 passing, none skipped, on all three platforms at `eddee00`.

---

## Entry 48. The gate record difference localised, and the lens model kept

`docs/NOTES-FROM-PLANNING.md` entry 48. **How the difference was localised:** every gate record run uploads each platform's records, and the records hold each stage's quantities. Matching them marker by marker and corner by corner, rather than by position in a list, gives the first stage whose output differs from Windows. The comparison is of the records from `826bd15`.

### Section 2, Linux: explained

- **a. Every gate verdict is identical.** Every console table is identical to Windows, line for line, so every verdict on every frame is too.
- **b. The stage is S3, the homography.**
  - **Identical to Windows in every record:** the S2 corner positions, to the last recorded digit, and the corner sets and inlier flags.
  - **The first quantity that differs:** the homography `Cv2.FindHomography` returns over those corners. For a scan it is the only computation between the corners and the mapping.
  - **Downstream of it:** everything else that differs is computed from that mapping: the scale, a photograph's lens coefficients, and the bull and corner errors.
- **c. The mechanism, and its bound.**
  - **The mechanism:** `findHomography` ends its RANSAC with an iterative Levenberg-Marquardt refinement over the inliers. The inputs and the inliers are identical on both platforms, so what differs is the arithmetic of the iterations in a different native build, not which minimum is found.
  - **On the scans:** the homographies differ by at most 3.4e-7 dmm anywhere within the markers' extent.
  - **Bull figures:** a bull figure differs only where its value sits on a rounding boundary of the 0.0001 dmm the records keep.
  - **Where it is amplified:** the largest effect is 0.0071 dmm, on one bull in one random subset of four to six markers in measurement 1, where a poorly conditioned fit magnifies it. On a photograph it is 0.0006 dmm.
  - **Why it cannot grow into a verdict:** a difference that starts at 3.4e-7 dmm stays under a hundredth of a micrometre unless the fit is ill-conditioned, and the gated frames have 9 to 34 markers.

### Section 2, macOS: localised, not yet explained, nothing changed

- **a holds.** The paper gate and photograph gate tables are identical to Windows.
- **b: the first stage that differs is S2, corner refinement.** Planning's guess was an iterative fit; the corners already differ before any fit runs.

**What differs at S2, measured against Windows' corners matched by marker and corner:**

| Images | Refinement | Corners that move | Largest move | Largest bull difference |
|---|---|---|---|---|
| Scans and photographs | subpixel, the shipped window | 0 to 2 of 104 to 136 per image | 0.0005 px | 0.0003 dmm |
| Letter scans | contour | every corner | 0.098 px at 600 DPI, 0.044 px at 300 | 0.0003 dmm |
| Letter scans, 300 DPI | subpixel, quarter module | 1 to 5 per image | 1.4 px | 0.28 dmm |
| Synthetic raster | subpixel, the shipped window | every corner | 0.024 px | 0.0026 dmm |
| Synthetic raster | subpixel, 2 modules | every corner | 0.43 px | 2.68 dmm |

**On the synthetic raster the markers also come back in a different order:** 52 of the 136 corners at 600 DPI and 80 at 300 DPI sit at a different position in the list, with the same set of corners. The order is the order RANSAC samples from, so it can change which subsets the homography fit draws.

**S3 differs as on Linux, more.** On the scans the homographies differ by up to 1.1e-5 dmm within the markers' extent.

**One amplification, and it is the one that matters.**
- **The frame:** `telephoto3.jpg`, excluded from the gate because the sheet overflows the frame, has four markers.
- **What is identical on macOS:** all 16 of its corners, and its inlier flags.
- **What moved:** its scoring bull 24, by 0.30 dmm, because the edge fit kept 29 edge points instead of 30.
- **Why that matters:** a difference of the S3 size tipped the edge fit's acceptance of one edge point. That is how a marginal frame could change a verdict. The step is a threshold in the bull locator, not an iterative fit.

**c: the mechanism is not yet named.**
- **The candidates in S2:** OpenCV's `cornerSubPix` and the ArUco contour refinement in the arm64 build, which is the only native code there, and the order in which the ArUco detector returns its candidates.
- **What would separate them:** feeding macOS Windows' corners in Windows' order, from the records, and rerunning S3 onward. If the bulls then agree as closely as Linux's do, S2 is the cause apart from the edge fit's threshold.
- **The obvious deterministic step:** sort the detected markers by identifier before anything uses them. That removes the ordering effect whatever the detector does.

None of this is changed, as section 2 asks.

### Section 3: the lens model kept

- **The rule, now in `ImageScrubber`:** keep what describes the camera and the exposure, and drop everything that describes where, when, who, or anything a person typed. `LensModel` is kept.
- **The two whitelists agree.**
  - **What the log records now:** the metadata reader now reads the lens model, ISO and exposure time, so `ImageFacts` records the same camera and exposure facts the scrubber keeps.
  - **What holds them together:** `WhitelistTests` requires every camera fact logged to be a kept field and every kept field to be logged. `CameraFieldsTests` checks that scrubbing keeps the lens model, ISO and exposure, and still drops the capture date and the location.
- **Republished.** The owner's photographs were run through `grouplab publish-owner` again from the originals, with the same two photographs held and their published wording kept. Now at `grouplab-testdata` commit `d35ef99`:
  - **Changed:** 13 of the 26 published photographs, all Pixel frames, each only in keeping `LensModel`.
  - **Unchanged:** the other 13 come out byte-identical.
  - **The provenance record:** it carries the new published hashes and kept lists, the new intake time, and the `optedOutIn` field the record format gained under entry 37.
  - **No donated photographs:** none are published, so none needed republishing.
  - **Its README:** it states the rule.
- **Not edited: `tools/scan_analysis/scrub_exif.py`.** It says to keep its whitelist identical to the scrubber's, and it does not keep `LensModel`. It is planning's file.
- **Not done: `LensModel` in the lens grouping key,** as section 3 asks.

### Section 4

`docs/DETECTION-PIPELINE.md` now says the number that must stay at zero is the wrong names, and that every refusal is kept by anyone raising the hit rate.

**Tests:** Core 757 passing, App 35 passing, none skipped.

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

## Entry 49 section 2 and entry 51. The marker sort, held for a question, and the check's fixes

**The gate record under entry 49 section 1's rule, measured at `e03415c`:** the printed tables are identical on Windows and Linux, which pass, and differ on macOS, which fails. Build and test is green on all three.

**Entry 49 section 2: implemented and measured, not committed.**
- **What sorting does on Windows:** it changes every printed Phase 0 table and no gate verdict.
- **The largest effect:** on the mounted photographs, where RANSAC settles on a different consensus set in a different sample order. `ultrawide3.jpg` keeps 51 corners of 128 instead of 42, and has 18 scoring bulls over the gate instead of 21.
- **Why it is held:** committing the sort means regenerating committed records and the figures documents quote from them, including the mounted benchmark of `docs/PHASE0-RESULTS.md` section 4.5.
- **Where it went:** `docs/QUESTIONS-FOR-PLANNING.md` question 15, with the options.
- **Also waiting on it:** the macOS rerun from Windows' corners, whose order the sort decides.

**Entry 51 section 6:**
- `DESIGN.md` section 20 now says .NET 10.
- `docs/PHASE0-SPIKE-BRIEF.md` now cites `DESIGN.md` section 19.
- Entries 36 and 31 have their stale exceptions closed.
- Five paths in entries 41 to 43 now start with `src/`.

---

## Entry 52. The markers sorted before use, every record regenerated, and what the movement measures

`docs/NOTES-FROM-PLANNING.md` entry 52 sections 1 and 2. Sections 3 and 4, how far registration and the edge fit move under reordering and under one point left out, are reported in "Entry 52 sections 3 and 4".

**These figures moved because the markers are now sorted before use. The sheet, the markers and the corners are identical; only their order changed. The movement measures how unstable the registration is on these frames, not an improvement in it.**

**The change.** `MarkerDetection.InIdentifierOrder()` orders markers and rejections by identifier, then by their first corner, and undecoded quads by their first corner, and `SheetMeasurer` and `PageRegistration` take every detection through it. The homography's RANSAC draws its samples by position in its list of correspondences, so until now the answer depended on the order the detector happened to return the markers in. It was chosen for determinism: had sorting made every mounted frame worse, it would still be the change.

**No gate verdict changed.**
- **Paper gate:** ten of ten, worst 0.00325 in, as before.
- **Photograph gate, flat:** fails three of three. No bull figure of `main_flat1-3` moved at the printed precision.
- **Photograph gate, mounted:** fails seven of seven.
- **Print-scale detection:** passes, 0.96195 at 600 DPI where it was 0.96197.
- **Conformance test 43 on the module sweep's five sheets:** passes on every sheet.

**What moved in the whole-sheet registration**, `grouplab spike photos`, on every photograph whose printed figures moved beyond the homography's own RMS:

| Photograph | Gate | Corners kept, before / after | Scoring bulls over the gate, before / after | Worst scoring bull (in), before / after | Worst bull of any kind (in), before / after |
|---|---|---|---|---|---|
| `ultrawide1.jpg` | mounted | 66 / 68 of 136 | 13 / 14 of 25 | 0.03301 / 0.03902 | 0.06983 / 0.07450 |
| `ultrawide2.jpg` | mounted | 54 / 46 of 136 | 20 / 19 of 25 | 0.06983 / 0.08305 | 0.08732 / 0.08305 |
| `ultrawide3.jpg` | mounted | 42 / 51 of 128 | 21 / 18 of 25 | 0.09123 / 0.11331 | 0.09123 / 0.11331 |
| `main1.jpg` | mounted | 90 / 94 of 136 | 8 / 8 of 25 | 0.01532 / 0.01824 | 0.04841 / 0.05183 |
| `main2.jpg` | mounted | 25 / 32 of 104 | 21 / 22 of 25 | 0.04350 at bull 10 / 0.08900 at bull 5 | 0.06231 / 0.08900 |

`main3.jpg` and `telephoto2.jpg` moved in no figure but the homography's RMS. The benchmark of `docs/PHASE0-RESULTS.md` section 4.5 moves with these rows: worst scoring bull 0.018 to 0.113 in where it was 0.015 to 0.091, scoring bulls over the gate 8 to 22 where it was 8 to 21, and corner RMS 0.015 to 0.070 in where it was 0.014 to 0.060.

**What moved in the surface fit**, each frame fitted alone (`grouplab surface frames`), the selected model:

| Photograph | Corners kept, before / after | Worst scoring / sighter (in), before | Worst scoring / sighter (in), after | Scoring bulls over the gate, before / after |
|---|---|---|---|---|
| `ultrawide1.jpg` | 113 / 113 of 136 | 0.01249 / 0.02129 | 0.01044 / 0.02522 | 9 / 9 |
| `ultrawide2.jpg` | 110 / 108 of 136 | 0.05809 / 0.00758 | 0.05784 / 0.00747 | 9 / 8 |
| `ultrawide3.jpg` | 87 / 81 of 128 | 0.06660 / 0.03095 | 0.05509 / 0.03144 | 12 / 13 |

The surface fit starts from the whole-sheet homography, so it inherits the reordering. On `ultrawide1.jpg` it keeps the same 113 corners and converges to a focal length of 2053 px instead of 1940: a second place where the answer depends on where the fit starts, not only on its data.

**Elsewhere, the scans and the studies.**
- **Scans:** the figures move in the last places. The largest move is `gl-cf25-ltr-96.2-600-dpi.png`'s worst edge-fit bull, 0.00234 to 0.00340 in, still inside the gate.
- **Measurement 1, random subsets of markers:** the four-marker rows move most. Sheet 1's 90th percentile worst bull goes from 0.03859 to 0.07296 in, and nine random markers now reach 0.0041 to 0.0065 in at the 90th percentile.
- **Measurement 2, corner refinement:** at 600 DPI the 1.5-module window's worst bull goes from 0.00328 to 0.00517 in, over the gate on one sheet. The shipped window's worst bull, 0.00316 in, does not move.
- **Measurement 3, threshold window:** the shipped window's worst bull on any sheet goes from 0.00325 to 0.00340 in.
- **The surface correlation:** two readings change. `ultrawide1.jpg` goes from structured to not distinguishable from random, at p 0.002, and `main_flat2.jpg` goes from not distinguishable to structured, at p 0.001. M1.11's prose now says two structured mounted frames, not three, and one structured flat control.

**What was regenerated, and what was not.**
- **Records:** every committed record that a command writes: the thirteen in `scans/phase0/measurements`, the Phase 1 records, the module sweep's PDFs, and the committed Windows tables the gate record workflow compares against.
- **`mounted-pair.json` was not regenerated.** `grouplab mounted pair` reads `scans/mounted/`, which entry 35 section 4 moved out of the repository, so its reproduce line no longer runs from a checkout. It locates dots on an OnTarget sheet rather than GroupLab markers, so the sort cannot move it.
- **Documents:** both results documents, `DESIGN.md` section 21 [r5] and `docs/PHASE1-BRIEF.md`'s benchmark carry the regenerated figures; the benchmark keeps the old figures beside the new. The tables from fits that no command reproduces today, M1.5's, M1.7's, M1.8's and M1.9's, are left as measured, and M1 says so at its head.

**How the sort's effect was isolated, and what that found.** Every record was regenerated twice under identical code, once without the sort and once with it, and every figure above compares those two runs. The unsorted run already differed from the committed records in five places, which are now regenerated with the rest and are not the sort's effect:
- **The synthetic hole detection records:** 139 and 93 values, rim closures, elongations and centre errors of up to 0.00008 in.
- **`surface-lens-synthetic.json`:** in one trial of 800 the selection flipped, and its selected worst bull went from 194.85 in to 2.25 in.
- **`surface-lens.json` and `surface-rendered.json`:** they gained the `family` and `turn` fields later code writes.
- **The module sweep's five PDFs:** they gained `/ViewerPreferences << /PrintScaling /None >>`, which entry 25 added to the renderer.

**Tests:** Core 758 passing with the records as committed, none skipped.

---

## Entry 52 sections 3 and 4. How far registration and the edge fit move when nothing that matters changes

`docs/NOTES-FROM-PLANNING.md` entry 52 sections 3 and 4, and entry 49 section 5.

**Reproduce:** `grouplab spike stability`, raw rows `scans/phase1/measurements/stability.json`, every order's result and every bull's leave-one-out figures.

**The method, one for both experiments.**
- **Registration against the order of its inputs.** Each flat and gated mounted photograph is detected once, then registered 200 times. Each time, the same corner correspondences reach the homography fit in a different seeded order, and the inlier flags are put back in the caller's order. The sheet, the markers and the corners are identical in every run.
- **The edge fit against each of its points.** On each photograph's own registration, every located scoring bull is refitted from the same start with each edge point left out in turn. The table gives the largest movement of any bull, and how many bulls have a point within a tenth of the fit's rejection limit.

**What was stated before running it.** Planning's hypothesis: on a flat sheet a homography is nearly the right model, so the consensus is stable. On a mounted sheet no plane fits, so RANSAC returns whichever subset looked best on its draws. And if the mounted frames spread, the mounted gate's 0 of 7 has never had an error bar.

| Frame | Gate | Orders | Distinct outcomes | Corners kept, min / median / max | Scoring bulls over the gate, min / median / max | Worst scoring bull (in), min / median / max | Largest leave-one-out shift of a bull (in) | Bulls with a point near the rejection limit |
|---|---|---|---|---|---|---|---|---|
| `ultrawide1.jpg` | mounted | 200 | 70 | 60 / 69 / 76 of 136 | 10 / 13 / 15 of 25 | 0.02785 / 0.03590 / 0.05062 | 0.00004 at 21, 651 of 651 points used | 4 of 25 |
| `ultrawide2.jpg` | mounted | 200 | 67 | 36 / 50 / 61 of 136 | 14 / 20 / 23 of 25 | 0.03020 / 0.06819 / 0.09631 | 0.00006 at 5, 313 of 313 points used | 8 of 25 |
| `ultrawide3.jpg` | mounted | 200 | 67 | 34 / 48 / 57 of 128 | 18 / 21 / 23 of 25 | 0.05938 / 0.10299 / 0.11680 | 0.00008 at 21, 214 of 214 points used | 3 of 25 |
| `main1.jpg` | mounted | 200 | 25 | 85 / 88 / 95 of 136 | 7 / 11 / 14 of 25 | 0.01342 / 0.01463 / 0.02373 | 0.00003 at 21, 749 of 749 points used | 3 of 25 |
| `main2.jpg` | mounted | 200 | 51 | 22 / 32 / 57 of 104 | 16 / 21 / 23 of 25 | 0.03651 / 0.05826 / 0.09253 | 0.00021 at 5, 106 of 106 points used | 6 of 25 |
| `main3.jpg` | mounted | 200 | 15 | 34 / 58 / 67 of 108 | 12 / 20 / 23 of 25 | 0.03616 / 0.06540 / 0.08938 | 0.00010 at 21, 374 of 374 points used | 2 of 25 |
| `telephoto2.jpg` | mounted | 200 | 46 | 16 / 38 / 45 of 132 | 20 / 22 / 25 of 25 | 0.03122 / 0.04012 / 0.05376 | 0.00004 at 21, 670 of 670 points used | 1 of 25 |
| `main_flat1.jpg` | flat | 200 | 1 | 136 / 136 / 136 of 136 | 0 / 0 / 0 of 25 | 0.00343 / 0.00343 / 0.00343 | 0.00002 at 11, 900 of 900 points used | 10 of 25 |
| `main_flat2.jpg` | flat | 200 | 1 | 100 / 100 / 100 of 100 | 2 / 2 / 2 of 25 | 0.00566 / 0.00566 / 0.00566 | 0.00002 at 4, 777 of 777 points used | 7 of 25 |
| `main_flat3.jpg` | flat | 200 | 2 | 89 / 91 / 91 of 92 | 3 / 6 / 6 of 25 | 0.00832 / 0.01183 / 0.01183 | 0.00003 at 4, 596 of 596 points used | 3 of 25 |
| `telephoto3.jpg` | excluded | not reordered | | | | | 0.00322 at 25, 14 of 14 points used | 0 of 20 |

**The hypothesis holds.**
- **Flat frames:** `main_flat1` and `main_flat2` give one result in 200 orders. `main_flat3` gives two: 89 or 91 corners kept, and a worst scoring bull of 0.00832 or 0.01183 in. Lost markers do not explain the difference, since `main_flat2` decoded 25 of 34 and `main_flat3` 23, and `main_flat2` gives one.
- **Mounted frames:** every one gives 15 to 70 distinct results. The worst scoring bull spans 0.030 to 0.096 in on `ultrawide2`, 0.037 to 0.093 on `main2` and 0.059 to 0.117 on `ultrawide3`. Scoring bulls over the gate span 12 to 23 on `main3`, and corners kept span 16 to 45 of 132 on `telephoto2`.
- **What it means:** the instability comes with the model's mismatch to a curved sheet. It is not a tuning problem in the sampler, and sorting the markers made it repeatable without making it smaller.

**The consequence for the mounted gate, as stated beforehand.**
- **The verdict is robust.** No ordering lets a mounted frame pass. The lowest worst scoring bull in all 1,400 mounted registrations is 0.01342 in, on `main1`.
- **The figures behind it are not.** The corners kept, the bulls over the gate and the worst bull each vary across orderings by more than the differences several M1 comparisons were read from.
  - **M1.5:** "worse on two frames" set `ultrawide2`'s 0.06983 against a surface fit's 0.09183, and that frame's whole-sheet figure alone ranges from 0.030 to 0.096.
  - **The benchmark itself:** its figures, whether 0.015 to 0.091 in before the sort or 0.018 to 0.113 after, are single draws from these ranges.
- **So a conclusion drawn from a change smaller than a frame's spread here was not supported by that frame.** The spreads are the first error bars those figures have had.

**The edge fit, entry 49 section 5's three questions.**
1. **Why one point of thirty was worth 0.30 dmm.** The bull had too few points, and the fit had not converged.
   - **The dense bulls:** on the ten frames above, every bull has 106 to 900 edge points, and leaving any one out moves a bull by at most 0.053 dmm (0.0002 in, on `main2`'s bull 5).
   - **`telephoto3`, where the sheet overflows the frame:** bull 24 has 29 points and its fit reports that it did not converge. Leaving one out moves it by up to 0.53 dmm, more than the 0.30 dmm move seen on the macOS runner. Bull 25 has 14 points and moves by up to 0.82 dmm, 0.0032 in.
2. **Whether a hard include or exclude sits where a weight belongs.** Not in the fit's rejection: at convergence it excluded no point on any bull measured here. The step is earlier. Whether a ray yields an edge point at all turns on a threshold, a profile whose ends differ by less than half the bull's contrast gives none, and on a sparse bull one ray more or less is a large share of the evidence.
3. **Whether the stage record says a fit is near its threshold.** Partly. A bull that did not converge is recorded as a rejection with that reason. Nothing records that a located bull rests on a few dozen points, or that its points sit near the crossing threshold. Nothing was changed, as entry 49 section 5 asks.

**Section 4: the uncertainty the intervals do not include.** `docs/STATISTICS.md` section 2 now records, with these numbers, that every interval takes the coordinates as exact and that they are not. On the flat photographs the registration's instability was nothing on two and 0.0035 in on the third; on a mounted sheet it reaches hundredths, and no figure a user sees includes it. `DESIGN.md` section 14 points at it.

**The code.** `grouplab spike stability` is new.
- **The edge fit's convergence loop** is extracted so its last pass's points can be read. `EdgeFitBullLocator.Locate` performs the same operations in the same order: the `sheets` table reprints identically.
- **`EdgeFitBullLocator.LeaveOneOut`** is a diagnostic beside `Locate`, used only by the spike.

**Tests:** Core 758 passing, App 35 passing, none skipped.

---

## Entry 49 section 2. The macOS rerun from Windows' corners: refinement is the first of two, not the only one

`docs/NOTES-FROM-PLANNING.md` entry 49 section 2, ordered by entry 52 section 5 item 4. **The question:** whether corner refinement is the only place the platforms diverge, or the first of two.

**Reproduce:** `grouplab spike corners --export <file>` on one platform and `--replay <file>` on another. In the gate record workflow the `windows corners` job exports the journal and the `macos-latest, from windows corners` job reruns from it, into the run summary. Both report and neither gates.

**The method.** The export records every detection the `markers` and `refinement` measurements ask for, in the order they ask: the image by hash, the settings it was given, and the corners it returned at full precision. The replay hands those back instead of detecting, and reruns the same two measurements with the homography, the warp and the bull fit still the replaying platform's own. Those two are the tables macOS prints differently. The hash is what makes the answer readable: it separates an input this platform built differently from corners this platform refined differently.

**The control, on Windows:** 78 detections replayed, all 78 images identical, and both tables reprint exactly as committed.

| | macOS, detecting for itself | macOS, from Windows' corners |
|---|---|---|
| `markers`, lines differing from the committed Windows table | 2 | 1 |
| `refinement`, lines differing | 19: 3 paper, 16 synthetic | 1, synthetic |
| Images identical to Windows before detection | | 62 of 78: all 14 of `markers`, 48 of 64 of `refinement` |

**1. Sixteen of the nineteen `refinement` differences were never detection.** The 16 synthetic calls, eight refinement variants at each of 600 and 300 DPI, are handed an image macOS built differently; every paper call and every `markers` call is handed a byte-identical one. The synthetic scan is rendered and then warped by the same native library that detects it, so the difference enters before detection is asked anything. Replaying Windows' corners collapses those 16 rows to one: 300 DPI, subpix 1.5 modules, worst bull 0.00225 in against 0.00226 here, which the bull fit reads off macOS's own raster.

**2. The three differing paper rows were detection, and they come right.** 600 contour, 300 contour and 300 subpix 0.25 module all agree once Windows' corners are used, on byte-identical images. Entry 48 localised this by matching records corner by corner; this shows it directly.

**3. One row differs with the image identical and the corners identical.** `markers`, sheet 3 at 300 DPI, four markers of 34: the 90th percentile worst bull is 0.03100 in on Windows and 0.03099 here. The 600 DPI row above it, which also differed, comes right. Nothing about that row's input differs, so **corner refinement is not the only place the platforms diverge.** It is the first of two, which is what entry 49 section 2 said the rerun would be worth knowing either way.

**What the second divergence is not yet known to be.** That row is a percentile over 40 random four-marker subsets, each refitted by the native homography solve and then read through the bull fit, and both are native code built separately for each platform. Which of the two moves is the same experiment one stage lower, replaying the homography as this replays detection, and it is not claimed here. The difference is one in the last printed digit, 0.00001 in, on the least supported fit in the table.

**What this does not change.** The gate record still fails on macOS, and nothing about the gate is touched (entry 49 section 1). This says where the difference enters, which is what was asked.

**Tests:** Core 758 passing, App 35 passing, none skipped.

---

## Entry 58 sections 3 and 4. The opt-out now survives a re-export, by a key the scrubber already computes

`docs/NOTES-FROM-PLANNING.md` entry 58 sections 3 and 4, its order items 1, 2 and 4, and entry 37 section 2, which this amends.

**The hole.** An opt-out won by content hash, and four uploads of one photograph have four content hashes. iOS rewrites a 36-character identifier inside the Apple maker note on every export from the library, so a contributor who uploads a photograph, thinks better of it, and sends the same picture again with the opt-out ticked was never matched. Entry 37's rule worked only because that contributor uploaded the identical file twice.

**The key, measured before it was chosen.** Planning offered the decoded pixels and asked for the scrubbed bytes to be measured first, as the cheaper option. They are enough:

| | Distinct values across the four uploads |
|---|---|
| File SHA-256 | 4: `7daf9d32`, `0330f435`, `d8b85ced`, `f42b5ba1` |
| SHA-256 of the scrubbed bytes | 1: `8b106005` |

- **Why it works:** scrubbing rebuilds the metadata and copies the compressed image data byte for byte, so two exports of one photograph scrub to the same bytes. The maker note, where the changing identifier lives, is dropped.
- **It is not so loose as to merge different photographs.** The two Android frames of one scene, taken a minute apart by one phone, scrub to `db2863ae` and `93630e53`. The other frame of the same shot sheet, `fc4d1649`, scrubs to `89d53b2f`, distinct from all four above.
- **It is deterministic:** the same file scrubbed twice gives the same key.
- **It needs no image decoder,** which decided it over the pixel hash. Decoding lives in the CLI behind OpenCV and the opt-out check lives in `GroupLab.Core`, so a pixel key would have moved a consent mechanism out of the layer whose tests run on every platform, for a key that measures no better here.

**What changed.** `Intake.PhotographSha256` is the SHA-256 of what a file scrubs to. `Intake.WithheldHashes` records it beside each withheld file's byte hash, and `Intake.Run` holds a file when either key matches, naming which one did. That is the same belt-and-braces reasoning entry 37 section 1 applied to the two opt-out signals: either alone withholds. A hash that only `meta.json` recorded has no file to scrub and still counts by bytes.

**On the real submissions**, read by `grouplab intake --submissions`:

| | Count |
|---|---|
| Distinct byte hashes withheld | 13 |
| Distinct photograph keys withheld | 10 |
| Total keys, from 5 withheld submissions | 23 |

The four browser-test uploads contribute four byte hashes and one photograph key between them. That single key is the hole closed: a publishable fifth export of that photograph is now held, where before it would have been published.

**The limits, written down rather than discovered later.**
- **It does not survive re-encoding.** A messaging app's copy has different compressed data and is a different photograph to this key, as it is to a pixel hash. Only a perceptual hash would match those, and none is proposed.
- **It does not survive a crop or a rotation.**
- **It is a function of the current scrubber,** and cannot drift, because both sides are computed in the same run from the same code rather than stored.

**Entry 58 order item 2, the first real exercise of intake on these files.**
- **The four opted-out uploads:** each refused, "exclude_from_public_dataset is true and a DO-NOT-PUBLISH file is present". Nothing was written.
- **`fc4d1649`,** the stripped iPhone frame: held, "not a camera original: it has no camera make", with 38 GroupLab markers decoded at 4032 by 3024. That is `CameraOriginal` meeting a genuinely stripped real file for the first time, and holding it.
- **The two consented Android photographs,** entry 59 order item 2, the first consented GPS-bearing files to go through the publication path: both published, each with GPS, 31 other EXIF fields, the thumbnail, XMP, the multi-picture index and a trailer removed. `PublicationTests` passes over the published copies, 8 of 8, with `CONTRIBUTORS.md`, `LICENSE` and `README.md` stood in for the testdata checkout. Nothing was published into `grouplab-testdata`: the run wrote to a scratch directory.
- **Entry 59 section 3 falls out of the same run,** printed by triage: lens group `2.20 mm f/2.2, 23 mm equivalent, digital zoom 1.66` for the camera app's frame and `6.25 mm f/1.7, 23 mm equivalent, digital zoom 1.00` for the page capture. One phone, one scene, one 35 mm equivalent, two optical configurations, which is entries 16 and 27 on real files rather than argued.

**A standing note, entry 58 order item 4: the user agent cannot identify a browser on iOS.** DuckDuckGo's user agent is indistinguishable from Safari's, so `user_agent` in `meta.json` is not evidence of which browser a contributor used, and nothing should be concluded from it. It is recorded here, where the donated corpus is described, rather than in `docs/DETECTION-PIPELINE.md` section 2, which describes the `scans/` corpus.

**Tests:** Core 759 passing, App 35 passing, none skipped. The intake fixture's second file is now a second photograph rather than the first with a trailing byte, because under a scrubbed-bytes key those two were one photograph and the test meant them to differ.

---

## Entry 55 section 3 item 1. What each bull rested on, recorded where it can be read

`docs/NOTES-FROM-PLANNING.md` entry 55 section 3 item 1, resting on entry 52 section 3, which measured why the count is worth having.

**Why a count is worth recording.** Leaving one edge point out moves a bull of 106 to 900 points by at most 0.0002 in, and `telephoto3`'s 14-point bull by 0.0032 in, two thirds of the whole gate. That is a sixteenfold difference in how far a bull can move, and it is predictable before anybody looks at the answer, from a number the locator already has and then threw away.

**What is recorded now**, in the `P0.bulls` stage record, for every located bull:
- **`edgePoints[<bull>]`,** the points its fit rested on.
- **`raysNearThreshold[<bull>]`,** how many of its 180 rays per edge had a rise within a tenth of the threshold that decides whether a ray yields an edge point at all.
- **`fewestEdgePoints`** across the sheet, and a detail line naming the sparsest bull, which prints at any verbosity.
- **In `grouplab measure --json`** the count rides beside the existing `edgePoints` field on each bull.

**Why that threshold and not the fit's rejection.** Entry 52 section 3 asked where a hard include or exclude sits, and found it is not in the rejection: at convergence the fit rejects no point on any bull measured. The step is earlier, in the crossing test, where a profile whose ends differ by less than half the bull's ink-to-paper range yields nothing. A ray within a tenth of that line is one the image could flip either way, so it is counted on both sides of the line rather than only where it failed. The tenth is the convention `EdgeFitBullLocator.LeaveOneOut` already uses for a point near the rejection limit.

**Nothing about what is located changed, and it is shown rather than asserted.**
- **The `sheets` table reprints identically,** line for line, against the committed Windows table.
- **No committed record moved.** The new count is in the stage record and in `grouplab measure --json`, and deliberately not in `scans/phase0/measurements`, so the gate record's raw comparison is untouched and no regeneration was needed.

**What it is for, which is not done.** Section 3 item 2 asks for the relationship between the point count and the leave-one-out spread, measured rather than guessed, and says plainly not to pick a cutoff from the three sparse bulls in one frame. That waits for the weekend's frames, where a sheet that overflows the frame is what produces sparse bulls. The field has to exist first, because it cannot be added to data already measured.

**Tests:** Core 760 passing, App 35 passing, none skipped, including a new one that fixes the stage record's per-bull counts against the locator's own.

---

## Entry 61 section 3. The print verb off Windows: checked before it was changed, and the prediction was wrong in its mechanism

`docs/NOTES-FROM-PLANNING.md` entry 61 section 3, which asked for this to be checked rather than assumed, and said the fix differs with the answer.

**The prediction.** That `Verb = "print"` with `UseShellExecute` throws `PlatformNotSupportedException` on Unix, that neither `catch (Win32Exception)` sees it, and that pressing Print therefore offers the person a crash report every time.

**What the runtime actually does**, `dotnet/runtime`, `SafeProcessHandle.Unix.cs`, where `UseShellExecute` is implemented:

```csharp
string verb = startInfo.Verb;
if (verb != string.Empty &&
    !string.Equals(verb, "open", StringComparison.OrdinalIgnoreCase))
{
    throw new Win32Exception(Interop.Errors.ERROR_NO_ASSOCIATION, SR.Format(SR.UseShellExecuteVerbNotSupported, verb));
}
```

**It is a `Win32Exception`,** which the existing catch already handles. The print button does not crash on Linux or macOS, the fallback runs, and the PDF opens in the viewer. That is planning's own second branch: merely useless there rather than broken. `ProcessStartInfo.Verbs` returns an empty array off Windows and throws nothing, and the resource string names the rule: only an empty verb or "open" is supported.

**What is real, and is fixed.** On those platforms every press threw, logged a `print.command` warning, and then told the person "your PDF viewer has no print command GroupLab can call", which blames their viewer for a platform fact. Now `PrintWindow.PrintLaunch` decides by platform:
- **Windows** asks for the shell `print` verb, and falls back to opening the file when the viewer registered no print command, as before.
- **Linux and macOS** are asked only to open the file, and the words say so: "This system has no print command GroupLab can call, so the PDF is open in your viewer."
- **No catch was added for `PlatformNotSupportedException`.** It cannot be thrown here, and a catch for an impossible exception is a claim about behaviour that is not true.

**What is verified and what is not.** The behaviour is read from the runtime source and the branch is chosen by `OperatingSystem.IsWindows()`, and a test pins both branches without starting a process. Nobody has yet pressed the button on Linux: what remains unproven there is only whether a desktop opener exists at all, which the existing failure message already covers, and which is what entry 61 section 2's VM is for.

---

## Entry 64. The line endings: nothing to discard, and the cause is a machine setting rather than the tree

`docs/NOTES-FROM-PLANNING.md` entry 64, which asked for the difference to be verified as whitespace before its fix was taken.

**Measured first, as asked.** `git status --short` lists only the untracked inbox entries. `git diff --shortstat`, `git diff -w --shortstat` and `git diff --cached --shortstat` are all empty. There are no 109 modified files, so `git checkout -- .` would have discarded nothing and was not run.

**The working copies really are CRLF**, as the entry says: `GrayImage.cs` 30 of 30 lines, `.gitattributes` 10 of 10, `RangeStatistics.csv` 594 of 594. The committed blobs are LF.

**Why they nonetheless agree, which the entry has wrong.** `core.autocrlf` is not unset. It is **true**, from `C:/Users/Airwolf/.gitconfig`, so git converts CRLF to LF when staging and back on checkout. A CRLF working copy of an LF blob is therefore not modified, and **`git add -A` cannot bake in the churn the entry warns about** while that setting holds. The warnings git prints on commit, "LF will be replaced by CRLF the next time Git touches it", are that conversion announcing itself on the files this session rewrote as LF.

**The stale lock is deleted.** `.git/index.lock.stale-claude-20260916` was 0 bytes with no live `.git/index.lock` beside it.

**The policy question is left open deliberately.** `* text=auto` in `.gitattributes` would move the normalisation from one machine into the repository, at the cost of one renormalisation commit touching every text file: exactly the churn the entry warns about, paid once on purpose instead of once by accident. It is defensible either way while every clone in use has `core.autocrlf` on, and it stops being defensible the moment a contributor whose git does not sees what entry 64 described. That is a decision for whoever owns `.gitattributes` and nothing was changed there.

**Tests:** Core 760 passing, App 36 passing, none skipped.

---

## Entry 61 section 5 item 2. A Linux tarball, built where its floor is set

`docs/NOTES-FROM-PLANNING.md` entry 61 sections 1 and 5 item 2, and entry 63 section 2, whose constraint decides where it is built.

**Reproduce:** the `linux tarball` job in `.github/workflows/ci.yml`, on every push. The artifact is `grouplab-linux-x64`.

**One format.** The tarball falls out of `dotnet publish --self-contained`, which the build already does, so it costs nothing to keep. AppImage waits for somebody wanting a menu entry, and `.deb`, `.rpm`, snap and flatpak wait for a person to ask by name, because each is a permanent obligation and there are no Linux users yet.

**What the first run produced**, measured from the artifact rather than from the log:

| | |
|---|---|
| Compressed | 90 MB, 94,107,807 bytes |
| Unpacked | 214 MB, 256 files |
| Native imaging | `libOpenCvSharpExtern.so` present |
| Application | `GroupLab.App` launcher, `GroupLab.App.dll`, `grouplab.dll` |
| Target library | 25 definitions, the twenty built-ins and the frozen Phase 0 ones |

**It is built on Linux rather than cross-published,** because `src/GroupLab.Cli/GroupLab.Cli.csproj` references the OpenCV native runtime package conditionally on the build host's platform, so only a Linux host puts the Linux native library in the output. The step fails when that library is absent rather than shipping a tarball that installs and then cannot find a marker, which would be the worst shape of failure: late, and in front of a user.

**Entry 63 section 2's floor, verified rather than assumed.** The runner reports `Image: ubuntu-24.04`, version 20260907.300.1, which is what entry 63 says `ubuntu-latest` still is. A binary built against an older glibc runs on a newer one and not the other way, so this tarball runs on 24.04 and newer, and building it on 26.04 would have raised the floor above the distribution CI itself uses. The step prints the release and the glibc version it built against, so the day `ubuntu-latest` moves the floor moves visibly rather than silently. It is not pinned to `ubuntu-24.04`, as entry 63 section 2 asks: pinning would hold the floor still while the test matrix moved, and the question planning wants asked is which of the two should move.

**Two things the first run taught, both fixed in the same commit.**
- **The figures reached only the run summary,** so reading them back meant downloading the artifact. They now go to the job log as well, which is where anybody diagnosing a build looks first.
- **The tarball carried the IBM Plex licence and not GroupLab's own.** The project file copies the font licence because the SIL Open Font License asks that every copy carry it; nothing copied `LICENSE`. GPL-3.0 section 4 asks the same of the binary, and this is the first build output meant to be handed to anybody, so the step now packs it. **The same question applies to a Windows download when one exists**, and there is no such build output yet to fix.

**What is not verified: nobody has run it.** Whether it launches on a desktop Ubuntu, and whether ICU is present, which the application needs because it reads the system region on first run, is exactly what entry 61 section 2's VM is for.

**Tests:** unchanged, Core 760 and App 36 passing, none skipped. This commit is a workflow and a file copy, with no code in it.

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

## Entry 70. Five decisions taken: LF in the repository, a status line with states, the editor's plumbing, and its matching rule

`docs/NOTES-FROM-PLANNING.md` entry 70, in its section 7 order. No layout work was started.

### Section 1: `* text=auto`, and the duplicate image deleted

`* text=auto` is the first line of `.gitattributes`, so the narrower `eol=lf` rules below it still govern the files compared byte for byte. Measured again before committing: the renormalisation staged only `.gitattributes`, both with this machine's `core.autocrlf` and with it switched off. The untracked duplicate `docs/figures/concept-assignment-editor.png` is deleted; the design target stays where it has been, `docs/figures/screens/assignment-editor.png`.

### Section 6: the print status line says which state it reports

Every message used to carry the alert style. Now each carries one of three: **success** in teal, for a saved PDF or one sent to the Windows print command; **information** in plain text, for a PDF opened in the viewer for the person to print; and **alert** in red, for a missing library, an unprintable sheet, a failed write, or nothing able to open the file. A test fails a save, sees the alert, then saves and sees success rather than a red line left over.

### Section 4: what the matching decided reaches the marking

- **`AutomaticResult.Detections`** is a list of `DetectedShot`, each a position with its whole `AssignedShot`. The `ShotAssignmentResult` travels with them, and the detector's refused candidates come through as `RejectedCandidate` rather than reaching only the trace.
- **`MarkingState.Assignment`** holds an `AssignmentReview`: the method and its reason, each detected shot's figures in inches under its marking id, the refused candidates, and the method detection used. It is state, so undo covers it, and `NeedingReview` is the concept's "2 of 26" counter.
- **The card's sentence is not stored.** `Reason` stays a note about the method; the sentence is composed where it is shown.
- **`BullAim` carries each bull's declared page position** beside its located image position (section 5), and both places they are used say why they differ.
- **Nothing new is saved in a marking file.** A sheet's page mapping is not saved, so the review lives while a detection is loaded, and a reopened marking has none until detection runs again.

### Section 3: the matching rule

Implemented in `MarkingSession`, applied after every change to the shots and on loading a detection:

| Item | As implemented |
|---|---|
| 1. A person's bull is pinned | a shot that is a shot and is manual or corrected keeps its bull |
| 2. The rest re-solve on every edit | the untouched detections are matched against the bulls no pinned shot holds, classifying on declared positions |
| 3. A cascade goes in the queue | each shot keeps the bull detection gave it, and `AssignmentReview.Moved` lists every shot whose bull now differs, for as long as it differs |
| 4. The counts rule holds live | more untouched shots than free bulls gives nearest free bull, every shot flagged, and `MethodChanged` true |
| 5. Undo restores the pins | provenance and the review are both state, so one undo takes back the reassignment, the pin and the cascade together |

**Three consequences worth knowing before the editor is drawn:**
- **A shot placed by hand is pinned to the bull it was given,** which is its nearest. A person adding a missed hole beside an automatic one therefore pushes the automatic one to another bull, and it shows as moved. That is the rule as entry 70 wrote it; if a hand-placed hole should itself be matched, the rule changes, not the code's intent.
- **A shot marked not a shot takes part in nothing.** It holds no bull against the others and is not matched.
- **The rule runs only while a detected sheet is loaded.** A marking made entirely by hand keeps the nearest-bull rule it always had.

**On loading, the rule reaches detection's own answer** when nobody's decision is in the way, since it matches the same shots against the same declared positions; the synthetic end-to-end test checks that nothing shows as moved and the method is unchanged.

**What is not done:** nothing draws any of this yet. The contested card, the amber ring with its dashed lines, the review queue and the method-change notice are the editor's presentation, which waits for Alan's report on using the application.

**Tests:** Core 768 passing, App 37 passing, none skipped. Seven new tests hold the rule: the contested shot and its figures on loading, a person's decision kept and the displaced shot shown as moved, undo restoring the pin, a deletion re-solving the rest, the counts rule switching method and back, not-a-shot left out, and a marking without a detection left alone.

---

## Entry 74 and entry 73 section 1. A chosen bull is its own fact, and sighter holes no longer enter the group

`docs/NOTES-FROM-PLANNING.md` entry 74 section 1, and entry 73 section 1, in that order as Alan set it.

### Entry 74: only a chosen bull is pinned

Entry 70's rule pinned a shot by its provenance, so a hole added by hand took its nearest bull as if a person had chosen it and pushed any detection off that bull. **Placing a hole and choosing a bull are different decisions**, so `MarkedShot.BullChosen` now records the second on its own:
- **Set only where a person chooses a bull:** `AssignBull`, which is click a hole then a bull, including choosing no bull, and `AddShot` when a caller passes a bull. No caller does today.
- **Everything else is matched,** hand-placed shots included. A hand-placed shot's baseline for "moved" is the bull it was placed with.
- **Saved in the marking file** as an optional `bullChosen` field, read as false when absent. The format version does not change.
- **One statement in entry 74 is not how the code behaves:** "a `Corrected` shot has one by definition". A detected shot also becomes corrected when it is moved or marked not a shot, neither of which chooses a bull, so `BullChosen` is not implied by `Corrected` and the rule reads only `BullChosen`.

**Entry 74's scenario, as a test:** one detection 10 dmm from bull 1, and a hole added by hand 60 dmm from it. Under entry 70 the hand-placed hole took bull 1 and the detection was pushed 390 dmm to bull 2. Now both are matched, the detection keeps bull 1, and the hand-placed hole is the one given bull 2, listed as moved from the bull it was placed with. Choosing bull 1 for the hand-placed hole then pins it, and the detection moves, which is a real result of a real decision.

**Entry 74 section 2 is recorded** in `DESIGN.md` section 13: the assignment detail does not survive a save, and the screen must distinguish "reopened, detail not available" from "nothing to review" when the queue is drawn. The file format is not changed.

### Entry 73 section 1: two pools

`ShotAssignment.Assign` takes the bulls' scoring flags. Each shot joins the pool of its nearest bull, each pool is matched on its own with section 13's counts rule applied in it, and the margin is still measured to every bull so a hole near the boundary between the rows is flagged. The automatic path and the live re-solve both pass the flags. Callers that pass none, such as the synthetic holes spike, behave exactly as before, so no committed record moves.

**On `Scan_20260916.png`**, submission 3a493942 at 600 DPI, 38 of 38 markers, the sheet Alan used:

| | Before | After |
|---|---|---|
| Holes detected | 15 | 15 |
| Assignment | one-to-one over all 28 bulls | scoring: 10 shots for 25 bulls, one-to-one; sighter: 5 shots for 3 bulls, nearest-bull |
| Shots 11 and 12 | bulls 22 and 23, a row away | sighters S1 and S2, their nearest bulls at 0.578 and 0.717 in |
| Shots in the group | 12 | 10 |
| Extreme spread | 2.224 in | **1.361 in** |
| Mean radius | 0.617 in | 0.344 in |
| Sigma | 0.492 in | 0.274 in |
| Error ellipse aspect | 2.630, major axis 62.5 degrees | **2.818**, major axis 26.7 degrees |

**Entry 73's prediction, half confirmed.**
- **Extreme spread fell well below 2.224 in, as predicted.** The two sighter holes are out of the group.
- **The ellipse aspect did not fall toward 1. It rose, to 2.818,** and its axis turned from 62.5 to 26.7 degrees. The two sighter holes had set the axis, since they sat 1.2 and 1.5 in below their assigned bulls. Without them, the ten scoring shots are themselves spread along a diagonal: shot 1 at (-0.609, -0.338) in and shot 5 at (+0.591, +0.304) in from their bulls carry most of it. An independent computation from the printed offsets gives the same 2.817 and 26.7 degrees. **So the reading of the dashed lines was right and the explanation of the aspect was not:** the elongation is in the scoring shots. Whether ten shots with this aspect say anything about the rifle is `docs/STATISTICS.md` section 7's circularity test, not something this run asserts.
- **Five sighter-row holes for three sighter bulls.** The sighter pool falls back to nearest-bull, flags all five, and the overall method is reported as nearest-bull with the reason naming both pools. Three of the five share sighter S1.

**Tests:** Core 771 passing, App 37 passing, none skipped.

---

## Entry 73 sections 2 to 8. Alan's first session, after the statistics fix

`docs/NOTES-FROM-PLANNING.md` entry 73, in its section 9 order after section 1, which is reported above with entry 74.

**Section 2: the oversize warning no longer names a cause it cannot know.** On the printed target it said "this is printed ink under the mark rather than a hole". It now gives the measurement and where the mark sits, and names every explanation: printed ink under the mark, two holes read as one, or a hole on a printed line merging with the ink. This reverses entry 40 section 1's choice to name only the ink, and the test that pinned that choice now pins the new wording.

**Section 5: the marks already scale once a calibre is set.** With a calibre and a scale, the impact ring is drawn at the bullet's diameter at the image's local scale times the zoom, never below 3 px, and its alert ring at the measured extent. Before a calibre is set there is no real diameter to draw, and the ring is a fixed 11 px. **Nothing was changed.** A ring drawn at an invented diameter would look like a calibre claim, and the honest alternatives, the detector's own measured diameter for detected shots or a centre mark that makes no size claim, are a decision for planning.

**Section 6: the shot list's rows fit, and a false positive is taken out from its row.** The row was a 230 px text button, the provenance word and Exclude, about 384 px in a column with roughly 340 inside its padding. Rows are now a grid whose text gives way first, trimmed with an ellipsis and shown in full as a tooltip. Each row gains **Not a shot**, with **It is a shot** to undo it. Delete and Not a shot were never missing from the application, only from the list: both were already in the selection panel. A test lays every row out at the column's inner width and checks nothing ends past it.

**Section 3: the off-centre hypothesis is not supported.** For each shot on `Scan_20260916.png` with calibre 0.308 in, the hole's own centre was taken as the darkness-weighted centroid of pixels darker than paper and not printed, using the registered expected artwork to exclude ink, and the reported centre's displacement from it was compared with the direction of the printed ink within 0.4 in.

| | Shots | Median displacement | Pointing at the ink, cosine above 0.5 |
|---|---|---|---|
| Flagged oversize | 5 | 0.0171 in | 1 of 5 |
| Not flagged | 10 | 0.0187 in | 2 of 10 |

The flagged marks are displaced no more than the others and not consistently toward the ink, so the detector is not pulling their centres onto the rings. **No detector change follows.** One control row is an artefact: shot 14's 0.203 in comes from its window reaching the neighbouring hole, shot 15.

**Section 7: the headline figures stay in view.** The placement line, centre from aim, mean radius, sigma and extreme spread with their intervals, and any size warnings stay in the panel; edge to edge, the angular note, the small-group size range, the error ellipse and the worst-shot test sit in a "More figures" expander, closed until opened and remembered in the settings file, as `DESIGN.md` section 19 specifies. **The 94.8% and 95.0% labels are left alone**: entry 24 chose to label each interval with its exact coverage rather than bend its endpoints to make it 95 percent, and making them match would undo that.

**Section 8: the sheet already names its own definition, and Alan's log says so.** His two sessions logged `detect.identify definition=GL-20J3-Y141-0BN3-EYME tile=0 codes=3` and no definition picker; the picker only opens when the codes cannot be read. What each session did do was **Open marking** after opening the image, which is a separate, optional step. So the flow he asked for exists, and what may have misled him is that detection waits for the Detect button. Whether it should run on opening an image is a behaviour decision, not taken here. **The printed human name** changes the renderer, every sheet printed afterwards and the committed module-sweep PDFs, and waits for planning's wording and placement.

**Section 4** waits for the label number from Alan.

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

## Entry 77. What the zones swallow, a standing check on printed artwork, and one test for three symptoms

`docs/NOTES-FROM-PLANNING.md` entry 77, in the order Alan set:
- section 3, both items;
- section 4's test, with no symptom fixed on its own;
- section 5's decisions;
- section 2 recorded.

### Section 3 item 1: the count of what the zones swallow

**What changed.** A blob refused only because it lies inside an exclusion zone now carries that zone's name. It has passed every size, compactness and shape filter a hole must pass. `RenderDifferenceResult.InsideZones` lists these blobs.

**Where the count shows.**
- **The S5-S8 stage** records the count as a metric, and its line always states it: "15 holes inside the registered sheet, 32 candidates rejected, 0 of them hole-sized inside exclusion zones". A detail line names each zone and how many it took.
- **The marking screen's summary** adds the count whenever it is not zero.
- **The stage record** also gains `split halves` and `oversized` metrics, which the standing check below reads.

**What the corpus shows.**
- **Clean 300 and 600 DPI scans:** 0.
- **Clean Phase 0 photographs:** 1 to 18.
- **The friend's scan:** 2. Both are inside the top right code at (7.869, 0.626) and (7.544, 0.783) in. A crop shows no hole there, only code residue.
- **Alan's scan:** 0.
- **The six mounted frames:** 1, 1, 6, 6, 7, and 0 for `IMG_5823`.

**What it would have said about entry 76's caption change.** The change was re-applied on a scratch basis and then removed again.
- **Holes:** 15 → 13.
- **Hole-sized candidates inside zones:** 0 → 1.
- **Split halves:** 2 → 0.

**One swallowed blob held both lost detections.** They were the halves of a single blob: the real sighter hole at (3.070, 10.573) in, joined to the print note's residue at (2.909, 10.784) in. The count would have announced that as a number.

### Section 3 item 2: the corpus comparison is now a standing check

**`grouplab corpus counts [--local <manifest>] [--write]`** runs every corpus image through the automatic path. For each image it prints:
- holes;
- rejected candidates;
- candidates inside zones;
- split halves;
- oversized holes.

**It compares them with a record, `before->after` wherever they differ.** It exits 1 when anything differs, until the change is recorded with `--write`.

**What makes it standing.**
- **Every record carries the artwork it was measured against.** That is `ArtworkFingerprint`: a SHA-256 of each definition's PDF, with and without the actual-size sentence, over every file under `targets/`, frozen ones included.
- **`ArtworkFingerprintTests` fails** as soon as the renderer prints something the record was not measured against. Its message says to run the command, compare the counts, and record them.
- **So artwork cannot change** without the comparison being run.

**The committed corpus is `scans/phase1/measurements/detection-counts.json`.** It holds the 37 Phase 0 images, sheets printed before any change since, and 18 punched runs.
- **The punched runs need explaining.** The Phase 0 sheets have no holes, so on their own they could not see the caption change: every count stayed the same.
- **How a scan is punched.** Each 300 DPI letter scan is punched through its own registration by `SyntheticSheet.Punch`, with synthetic holes 150 dmm apart over the whole page, printed matter included.
- **Why three grids.** One grid did not cross the caption either. So each scan is punched three times, from 25, 75 and 125 dmm, which puts a row every 50 dmm across the three without crowding any one.
- **With the caption change re-applied, the committed corpus alone now catches it.**
  - Six punched runs change. Hole-sized candidates inside zones rise by 2 on each letter sheet and by 4 on each load-block sheet.
  - One run loses two holes, 163 → 161.

**A local corpus stays outside the repository.** It is a manifest of images that cannot be committed, `C:\Dev\grouplab-local\corpus.json` here.
- **What it lists:** Alan's scan, the six mounted frames, and the friend's scan and photograph.
- **Where its record goes:** it is written beside the manifest, with images named by label rather than by path.
- **Definitions:** `IMG_5823` and the friend's photograph have codes that cannot be read, so their definition is named.

**This check ran on section 5's name change,** reported below.

### Section 4: the three symptoms do not rise together, and there are two mechanisms, not one

**The harness is `grouplab holes ink-proximity [--local <manifest>] [-v]`.** For every detection it measures how far the centre is from the nearest printed edge. There are two kinds of edge:
- **Artwork:** the ink-to-paper edges of the expected render, at 1 px per dmm.
- **Text,** which the expected render does not draw: bull numbers, the identifier and the print note. For text the distance is to each run's glyph box.

**Truth.**
- **Clean Phase 0 images:** no holes, so every detection is spurious.
- **The 18 punched runs:** the holes are known.
- **Alan's sheet and the friend's sheet:** 14 and 13 holes, verified by hand against crops.
  - The scan and the six mounted frames are one sheet.
  - A hole seen twice is entered once.
  - Both sheets carry the print note.

**The outcome definitions.**
- **Spurious:** no true hole within 0.15 in.
- **Split:** the same true hole is also another detection's nearest.
- **Oversized,** measured three ways:
  - the detector's diameter against the image's own clear holes, at 1.25 times;
  - the detector's own flag;
  - the marking screen's size check, `HoleSize`'s apparent extent. That is what entry 73 section 2 saw. On the real sheets it is compared with a .308 hole plus allowance, 0.441 in. On punched holes it is normalised by clear holes.

**The committed rows are in `scans/phase1/measurements/ink-proximity.json`.**

| Distance to nearest printed edge (in) | Punched: detections | spurious | split | oversized | size check | Real: detections | spurious | split | oversized | size check |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 to 0.02 | 500 | 0% | 1% | 2% | 78% | 27 | 44% | 4% | 0% | 100% |
| 0.02 to 0.05 | 358 | 1% | 1% | 1% | 53% | 21 | 29% | 5% | 0% | 92% |
| 0.05 to 0.10 | 636 | 0% | 2% | 2% | 56% | 33 | 0% | 12% | 13% | 94% |
| 0.10 to 0.20 | 867 | 1% | 1% | 1% | 14% | 28 | 0% | 18% | 0% | 0% |
| 0.20 and over | 450 | 0% | 2% | 4% | 2% | 24 | 8% | 4% | 0% | 0% |

**The clean photographs' 64 detections all lie within 0.1 in of a printed edge:** 34, 14 and 16 in the first three bins. The detector's own flag stays between 3% and 7% in every bin.

**A hole about 0.3 in across overlaps ink well before its centre reaches the ink,** so the same detections were also split by whether their own extent overlaps printed ink.

| | Punched, overlapping ink | Punched, clear | Real, overlapping ink | Real, clear |
|---|---|---|---|---|
| Detections | 2147 | 664 | 93 | 40 |
| Spurious | 1% | 1% | 19% | 5% |
| Split | 1% | 2% | 6% | 15% |
| Oversized, detector diameter | 3% | 0% | 4% | 0% |
| Oversized, detector flag | 7% | 1% | 1% | 0% |
| Oversized, size check | 41% | 2% | 86% | 0% |

**The answer: spurious detections rise toward printed ink, and splits do not.** The detector's own diameter barely moves. The screen's size check rises steeply. So the three are not one defect in the difference stage.

**What they do share is sharper than proximity.**
- **Every spurious detection in the corpus is a split half.** That is 101 of 101: 64 on clean photographs, 20 on real sheets and 17 punched. They are halves of a blob that stage S8's merged-neighbour split cut in two.
- **So is every real hole detected twice:** 12 of 12 halves on the real sheets.
- **How the split path lets them through.** It exempts a blob from the size and aspect filters once its elongation reaches 1.45. Elongated residue therefore becomes two holes instead of being refused: a printed edge's sliver in a photograph, text the expected render does not draw, or a hole joined to either.
- **How often the split is right.** Across the corpus it cut 109 pairs, and was right on 25:

| Where | Pairs | Two real neighbours | One hole twice | One hole and residue | No hole at all |
|---|---|---|---|---|---|
| Punched | 60 | 25 | 18 | 17 | 0 |
| Real | 17 | 0 | 6 | 2 | 9 |
| Clean photographs | 32 | 0 | 0 | 0 | 32 |

- **Why spurious detections still correlate with distance.** Residue lives on printed edges, so the split halves do too: 48 of 64 on clean photographs and 20 of 34 on real sheets are within 0.05 in.

**The screen's oversize warnings are a second mechanism, in the size check rather than the detector.**
- **The cause.** `HoleSize` measures the dark region connected to the hole, and printed ink touching a hole is dark and connected. The extent therefore includes the ring.
- **The contrast.** On the same detections the difference stage's own diameter is close to flat, 3% against 0%. The size check is 41% against 2%, and 86% against 0% on real sheets.
- **How this fits entry 73 section 3.** Ink does not move centres there, and here it does not inflate the detector's segmentation either. It inflates the screen's measurement.

**Conclusion: two defects, not one and not three.**
- **The first is the split path:** spurious detections and holes found twice. The friend's σ of 0.607 in against 0.390 in comes from it.
- **The second is the size check reading printed ink as hole:** the oversize warnings.

**Nothing was fixed**, as section 4 asked.

**Limits of the test.**
- **Distance is measured from a detection's centre,** and to text by glyph box rather than glyph.
- **The punched holes are synthetic,** on scans only.
- **The real truth comes from two sheets,** 27 holes in all.
- **The committed Phase 0 images carry no print note,** so text there is bull numbers and the identifier only.

**A separate finding.** `IMG_5823` registers with its definition named, and then finds 2 detections, both spurious, where the sheet has 14 holes. It is the most oblique of the six frames. Every one of its holes is missed, which this test does not count.

### Section 5: the decisions

**The printed name is outside the analysed region, and it needs no box.**
- **The shared rule.** `BullCells`, now shared by the detector and the renderer, is the one region render-and-difference looks for holes in.
- **Where the name goes.** It reads name, middle dot, identifier, centred in the top margin, its glyph box starting at the codes' 136 dmm margin, at 25 dmm. It shrinks to no less than 12 dmm.
- **When it is left off.** It is drawn only where it stays 60 dmm from every cell and 30 dmm from everything else printed, and nowhere else.
- **The 60 dmm clearance.** A hole centred on a cell's edge reaches about 40 dmm past it, and the closing joins residue within about 28 dmm.
- **The result on the built-in sheets.** Every sheet carries the name at 25 dmm except the tiles, the four built-in tile files and the frozen Phase 0 tile, whose cells cover the page; they print none.
- **What stays where it was.** The identifier caption at the bottom and the print note are unchanged, and no exclusion zone was added or widened.

**What the standing check said about it.**
- **Committed corpus:** 55 of 55 images unchanged.
- **Local corpus:** all 9 unchanged in every count.
- **Artwork:** 25 definitions changed, as expected.
- **Its own test.** `PrintedNameTests` prints Letter both ways through PDFium, so the text is on the paper, and punches both identically, with a hole inside each top-row cell under the name. Render-and-difference finds every hole, at the same places with the name as without, and swallows nothing more.
- **Recorded in:** `docs/SPEC-ERRATA.md` C6.

**The print note has no new box.** The Alan's scan case above shows why: the note's residue and a real hole were one blob. A box would have hidden the residue in the same way the caption box hid the hole.

**Split holes are section 4's third row,** investigated there with the other two.

**Hand-placed shots keep the calibre ring,** as accepted.

**Cancel now names its worst case.**
- **What it says.** Pressing it disables the button and reads "Cancelling. The step in progress finishes first, which can take up to about 6 seconds on a 600 DPI scan."
- **Where the 6 seconds comes from.** The longest stage measured is hole detection on Alan's 35 megapixel scan, at 5.6 s.

### Section 2, recorded: the angled frames are focus-limited, and the gate is still unexplained

**Entry 76 section 3 here said the cause is optical and meeting the gate becomes a photography instruction. That holds for the angled frames only.**
- **The angled frames.** Entry 77 section 1's depth of field arithmetic explains them: at about 310 mm, 26 mm of sharpness cannot cover the 148 mm an A4 sheet at 30 degrees spans.
- **The square-on frames.** `IMG_5820` at 0.0053 in and `IMG_5819` at 0.0059 in are the two closest to the 0.005 in gate. A square-on sheet has almost no depth range, so focus is not what holds them outside it.
- **What limits them is not identified.**
- **The instruction, stated honestly, is a distance, not an angle.** `DESIGN.md`'s photograph gate is deliberately off-axis.
- **The prediction to check.** An off-axis frame taken from about 750 mm comes inside the gate where the same angle at 310 mm cannot. That needs one more photograph session with Alan's sheet.

**Tests:** Core 808 passing, App 42 passing, none skipped.

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

## Entry 80. The synthetic holes rescaled, the split fit shaped by real holes, and what a detection ran with

`docs/NOTES-FROM-PLANNING.md` entry 80, in its section 6 order.

### Section 2: what the synthetic holes simulate, and the sweep reshaped

**The synthetic holes simulate no single calibre.**
- **Where they come from.** `SyntheticSheet` was fitted so that the baseline detector measures its holes as `docs/SCAN-MEASUREMENTS.md` section 3.2 measured 343 real holes.
- **What that population was.** 260 of those holes had a known calibre: 189 were .264, 42 were .308 and 29 were .338, a mean nominal of 0.279 in.
- **How the two detectors read them.** Render-and-difference reads these synthetic holes at 0.335 in, which is 1.088 of .308 and 1.20 of the survey's mix. On real .308 scan holes it reads 0.944 of the calibre, about 1.03 times what the baseline read on the survey's .308 holes. That suggests the synthetic disturbed zone reads larger to this detector than a real one does; the cause is not measured.
- **The earlier sweep's first line** was 0.338 in at the shipped settings, 1.097 of .308, as entry 80 said. It was stopped, unfinished, after 88 minutes of processor time. Its real rows had used the bullet diameter, not the corrected size.

**Rescaled.** `SyntheticSheet.SampleHole` takes a scale for every length of the hole. The sweep finds the scale at which its single holes read .308 times the scan ratio.
- **The two trial scales:** 0.335 in at 1 and 0.266 in at 0.8.
- **The interpolated scale:** 0.871, at which a single hole reads **0.291 in, 0.944 of .308, the scan figure.**
- **The size the veto is given** is that same figure.

**The fit is in two parts, reported apart.**
- **Coverage:** the synthetic singles and pairs.
- **The veto:** the real holes.
- **The survivors:** the report lists every setting that misclassifies no real hole, ranked by synthetic errors.
- **Where it runs:** from the published copy, as `CONTRIBUTING.md` now says, which was also that convention's first use.

**Per blob, the real holes veto every setting at the shipped elongation of 1.45.**
- **What is left.** Two real blobs, each one hole joined to ink, are still cut in two at 1.45 whatever the size veto. At 1.60, one is.
- **What survives.** Only an elongation of 1.80 misclassifies no real hole: with the size veto at any level from 1.2 to 1.8 holes, 9 real spurious detections, and without it, 17.
- **The synthetic cost of 1.80:**
  - single holes are never split once the veto is on;
  - overlapping pairs almost never separate by shape at any setting, at most 59 of about 840;
  - so 1.80 costs the 5 pairs that split at 1.45 with no veto.
- **What is not measured.** No real merged pair exists in the corpus, so the cost on real neighbours is unknown.
- **The misses.** The 609 synthetic singles and 1304 synthetic pair holes missed at every setting are the punched holes outside every bull's cell, which the position prior refuses by design.

**No threshold is adopted.** Question 17 puts the choice to planning.
- **My recommendation:** 1.80 only when a calibre is named, now, and everywhere once a real merged pair is measured.
- **Why it is planning's decision.** Changing the default without a calibre moves the committed synthetic records.
- **The provisional defaults stay:** 1.45, a veto under 1.5 holes, and a flag at 1.8 holes.

**Not yet measured.** The flag direction: whether merged pairs that stay whole are flagged oversized at 1.8 holes. The sweep does not score it. The synthetic pairs' union is 1.2 to 1.6 holes, which suggests most would not be.

### Section 3: the scan ratio by sheet

**There are two scan sheets, and the sheet is the unit.**

| Sheet | Holes | Ratio |
|---|---|---|
| Alan's | 13 | 0.952 |
| The friend's | 11 | 0.934 |
| Pooled | 24 | 0.944 |

- **The hole-to-hole spread of 0.039** is within sheets that share paper, printer, scanner and bullet, so it says little about the next sheet.
- **The friend's photograph,** which carries no camera data and is taken as a scan, measures 0.924.
- **Both sheets read a scanned hole a few percent smaller than the bullet.** With two sheets, how much smaller is not settled.
- **Recorded in:** `AutomaticMarking.ScanHoleToCalibre`'s documentation.

### Section 4: the harness artefact removed

**`grouplab holes ink-proximity` no longer gives a split half a size ratio**, because a half reports its whole blob's diameter.
- **One row of `ink-proximity.json`** is one detection, and for a split half its diameter is the blob's.
- **Regenerated,** the detector's own oversize rate is 0% in every distance bin, on punched and real sheets alike. Every oversized row before was a half.
- **The marking screen's size check is unchanged** by this, at 78% near ink and 2% clear on punched scans.

### Section 5: what a detection ran with

**The record.** `DetectionRecord` holds the calibre a detection ran with, or none, and the size its holes were taken to measure.
- **Where it is kept:** on the marking, in the marking file as `detection`, and in the report as a sentence.
- **Its three states:**
  - **null** when nothing was detected;
  - **"detected without a calibre, so whether a mark was one hole or two was judged by its shape alone"** when detection ran without one;
  - **"detected with the calibre .308, whose holes were taken to measure 0.291 in"** when it ran with one.
- **Kept apart from the calibre named now,** which a person may change after detecting without detecting again.
- **Where else it appears.** `grouplab analyze` prints it under the group, and the automatic path's summary, shown on the marking screen, says it too.
- **Its test** writes, reads and reports both kinds, and a marking made by hand records none.

**The rule it serves:** two markings of one sheet, one detected with a calibre and one without, are not comparable. Nothing in GroupLab compares markings yet; when something does, it reads this.

### Entry 78 section 2: the photograph residue, measured, with its fix waiting on question 17

**On photographs, every spurious detection is a split half:** 64 on clean sheets and 19 on real ones. The features separate them from real holes cleanly.

| | Blobs | Elongation, median (range) | Solidity, median | Diameter, median |
|---|---|---|---|---|
| Spurious | 83 | 4.6 on clean sheets and 4.2 on real ones (1.54 to 7.4 overall) | 0.65 to 0.68 | 0.18 to 0.19 in |
| True holes | 84 | 1.17 (at most 1.67) | 0.95 | 0.30 in |

- **Why a plain cap will not do.** A merged pair of equal holes cannot exceed an elongation of about 2.24. A cap there would remove 70 of the 83 spurious halves and no true hole. But the closing can join two real holes up to about 0.11 in apart, and those can reach about 2.6. A cap alone could therefore drop two real holes silently.
- **The proposal.** Refuse a blob that the size veto calls too small for two holes and that is more elongated than any single hole. It is described in question 17 and not built.

### Section 1: the friend's earlier sheet, as a measurement

**Measured with `grouplab analyze`, from the published copy,** on the friend's 300 DPI scan of `GL-20J3-Y141-0BN3-EYME`. The scan is local and unpublished.

| | Holes | Sigma (in) | Interval (in), with its coverage |
|---|---|---|---|
| Without a calibre | 15 | 0.607 | 0.470 to 0.859, 94.8% |
| With `--calibre .308` | 13 | 0.391 | 0.295 to 0.578, 94.7% |
| Hand-merged, entry 76 section 2 | 13 | 0.390 | 0.295 to 0.577, 94.7% |

- **Why the two runs differ.** Without a calibre, the two holes that cross printed ink at bulls 3 and 10 each read twice. With .308 each reads once, because their blobs hold 0.78 and 0.89 of a hole's area.
- **Against the truth.** The detected sigma is within 0.001 in of the sigma computed from the hand-merged holes.
- **What it rests on.** The comparison checks the detector against a hand-made answer, on one sheet. Alan's scan reads identically either way, 15 holes and sigma 0.274 in.

**Tests:** Core 811 passing, App 43 passing, none skipped.

---

## Entry 81. Split elongation 1.80 everywhere, merged pairs flagged, and photograph residue refused

`docs/NOTES-FROM-PLANNING.md` entry 81, which answers question 17. The measurements below came from the published copy.

### Section 1: 1.80 is the split threshold, with or without a calibre

**`RenderDifferenceOptions.SplitElongation` is now 1.80.** Entry 81 section 1 gives the reasons:
- the real holes veto 1.45;
- a hole cut in two is the quiet failure and a merged pair the loud one;
- the case for 1.45 rested on the disqualified synthetic population;
- one threshold avoids two detectors.

**The size a single hole is taken to be now applies without a calibre too.** Without one it is the sheet's own 25th percentile whole mark, once there are five. So a calibre changes an answer only by supplying that size, and the same rule runs either way.

### Section 2: merged pairs made from real holes, and the flag that missed them

**`grouplab holes composite-pairs --local <manifest>`.**
- **Where the holes come from.** Of Alan's 14 real holes, the 3 that lie clear of printed ink are lifted from his 600 DPI scan.
- **How they are placed.** They are pasted in pairs, darker pixel winning, 0.70 in below each even scoring bull of the three clean GL-CF25-LTR 600 DPI scans, at centre separations from 0.10 to 0.45 in. Single holes go on the odd bulls.
- **What the holes measure.** Each reads about 0.29 in across.

**Under the old flag, with 1.80, the gap entry 81 feared was real.**
- **The pairs:** every pair 0.10 or 0.15 in apart became one mark, and none of the 78 was flagged, with or without .308.
- **What they measure:** those marks hold 1.39 to 1.72 holes' area, under the calibre flag's 1.8.
- **Without a calibre:** the old rule, the median plus two robust deviations, flagged 12 of 36 ordinary single holes once the pairs split, because three near-identical source holes give a tiny spread.

**The flag is fixed, not the threshold.** `OversizeHoles` is 1.35: a whole mark with the area of 1.35 single holes or more is flagged. The single hole is the calibre's size, or the sheet's 25th percentile whole mark, a quantile a sheet of mostly merged pairs cannot move.

**With it, and with or without .308:**

| Separation (in) | Pairs | One mark | Flagged | Two marks | Singles falsely flagged |
|---|---|---|---|---|---|
| 0.10 | 39 | 39 | **39** | 0 | 0 of 36 |
| 0.15 | 39 | 39 | **39** | 0 | 0 of 36 |
| 0.20 | 39 | 15 | **15** | 24 | 0 of 36 |
| 0.25 to 0.45 | 39 each | 0 | | 39 | 0 of 36 |

**Every pair left as one mark is flagged, and no single hole is.** From 0.20 in apart, 1.80 splits a pair once its elongation reaches 1.8, and from 0.25 in it always does. So the loud failure really is loud.

**What the flag costs on real sheets.** The rule was checked first against the 99 real whole holes of the two sheets:

| Where | Falsely flagged, with the calibre | Without it |
|---|---|---|
| All 99 real whole holes | 6 | 13 |
| Scans (24 holes) | 0 | 0 |
| The three oblique frames 5821, 5822 and 5824 | most | most |

**In the re-recorded local corpus**, 17 whole marks on real sheets are flagged.
- **Two are a hole joined to residue,** not two holes: Alan's sighter hole with the print note, and the friend's photograph's hole beside bull 10. Both used to be split into a real hole and a spurious one.
- **The other 15 are single holes in photographs:** one in `IMG_5819`, and 14 in the three oblique, focus-limited frames.

**The synthetic sheets are a different story.** Their holes carry the survey's mixed-calibre size spread, so the flag fires on 19 to 65 marks a case, where it fired on 0 to 25 before. That population is not one calibre, and this is not read as a real false-alarm rate.

### Section 3: the three populations, in full, and why solidity cannot carry the fix

Quantiles: minimum, 5th, 10th, 25th, 50th, 75th, 90th and 95th percentiles, maximum.
- **Spurious blobs:** 42 blobs from the photographs, each split pair counted once, at 1.45.
- **Real single holes:** 107 from both sheets, 27 of them on scans.
- **Real joined pairs:** 246 composite pairs joined by the closing.

| Elongation | min | 5% | 10% | 25% | 50% | 75% | 90% | 95% | max |
|---|---|---|---|---|---|---|---|---|---|
| Spurious blobs | 1.54 | 1.55 | 1.67 | 3.65 | 4.43 | 5.79 | 7.15 | 7.17 | 7.37 |
| Real single holes | 1.01 | 1.04 | 1.06 | 1.09 | 1.17 | 1.30 | 1.40 | 1.49 | 1.72 |
| Real joined pairs | 1.26 | 1.27 | 1.41 | 1.65 | 1.96 | 2.53 | 2.74 | 2.89 | 3.08 |

| Solidity | min | 5% | 10% | 25% | 50% | 75% | 90% | 95% | max |
|---|---|---|---|---|---|---|---|---|---|
| Spurious blobs | 0.55 | 0.57 | 0.57 | 0.61 | 0.66 | 0.75 | 0.81 | 0.85 | 0.89 |
| Real single holes | 0.59 | 0.74 | 0.88 | 0.92 | 0.95 | 0.97 | 0.99 | 0.99 | 1.00 |
| Real joined pairs | 0.70 | 0.71 | 0.78 | 0.81 | 0.88 | 0.93 | 0.94 | 0.94 | 0.94 |

**Solidity does not separate them.**
- **Spurious blobs** reach 0.89.
- **Real single holes** go down to 0.59. On scans, 7 of 27 fall below 0.8, holes whose residual picks up ink at a ring.
- **Joined pairs** fall to 0.70 as their separation grows, from 0.91 to 0.94 at 0.10 in down to 0.70 to 0.80 at 0.35 in.
- **At solidity under 0.85** the rule would catch 40 of 42 spurious blobs, but also 10 real single holes and 78 joined pairs.

**Size separates them where the shape asks for a split.**
- **Spurious blobs with elongation 1.8 or more:** 36 of 42, holding at most 1.08 holes' area.
- **Joined pairs with elongation 1.8 or more:** 153, holding at least 1.80.
- **So the size veto stays, and it is the size that carries the fix.**

### Entry 78 section 2: the photograph residue fix, built on that

**The rule.** An elongated blob that the size calls too small for two holes, under `SplitMinimumHoles` 1.5, is:
- **refused as residue** when its elongation is also at least `ResidueElongation` 2.2, which is half a unit beyond the most elongated real single hole measured, 1.72;
- **kept as one hole** otherwise.

**How the split is decided now.** It is taken after every other blob is seen, so the sheet's own hole size is known. The S5-S8 stage counts splits the hole size stopped and residue refused.

**Tests.**
- A diagonal sliver, which the bounding-box aspect filter passes, is refused, and the holes beside it stand.
- A merged pair at the shipped settings stays one mark and is flagged, and its single neighbours are not, with a calibre and without.
- With too few marks to know a size, shape alone decides.
- The veto's mechanism test pins elongation 1.45, where its drawn shapes split.

**What it leaves.**
- **The gap:** on a sheet with fewer than five holes and no calibre there is no size, so residue is still split.
- **Where that shows:** the clean Phase 0 photographs, which have no holes, still give 60 spurious detections, down from 64.
- **What might close it:** naming a calibre supplies the size and should close the gap. That is not measured on the clean photographs.

### Section 4

**Recorded where the ratio is defined,** in `AutomaticMarking.ScanHoleToCalibre`'s documentation.
- **What two sheets are enough for:** telling one hole from two, a factor-of-two judgement a few percent cannot flip.
- **What they are not enough for:** anything that needs the absolute size, such as a measured calibre reported back, or hole sizes compared between loads.

### What moved in the committed records, and why

**`holes-synthetic.json` and `holes-synthetic-held-out.json` were re-recorded** because the split threshold moved from 1.45 to 1.80 and the oversize flag and residue rule changed (entry 81). The M2.2 tables above are the 1.45 figures.

**Held-out, render-and-difference:**
- **One hole per bull:** unchanged, 100 percent at 600 and 300 DPI.
- **Two holes per bull:** 94.0 falls to 87.5 percent at 600 DPI, and 96.4 falls to 90.5 percent at 300 DPI. Neighbours closer than the closing's reach, with elongation between 1.45 and 1.80, are now one flagged mark.
- **Overlapping pairs:** 53 of 112 found within 0.15 in, against 54.
- **Arrowheads:** 50 strays, against 53.
- **Registration rows:** unchanged except one hole at 0.040 in, which is now within 0.01 in in 44 cases rather than 43.

**`detection-counts.json`:**
- **The punched scans:** each loses 1 to 4 holes and 2 to 8 split halves, and gains oversize flags, 54 to 72 per sheet against 7 to 13.
- **Two clean photographs** lose detections: `main2` goes from 20 to 19 and `telephoto1` from 8 to 5.
- **Printed artwork** did not change.

**The local record:**
- **The friend's scan** reads 13 holes without a calibre, the sigma fix without one.
- **The friend's photograph** goes from 24 detections to 13.
- **The oblique frames** go from 17 to 14 and from 18 to 13.

**`ink-proximity.json`:** split halves on the real sheets fall from 34 to 2, and spurious detections from 20 to 3.

**Tests:** Core 814 passing, App 43 passing, none skipped.

---

## Entry 82. A graded single-hole size with a physical floor, one sentence for two sizes, and the flag on the screen

`docs/NOTES-FROM-PLANNING.md` entry 82.

### Section 1: why 64 only became 60, checked against the code

**The mechanism is slightly different from entry 82's reading, and the fix is the same.**
- **Round marks only.** The quarter-point is taken over the round marks: those not elongated enough to ask for a split.
- **What the clean photographs have.** Every residue blob there has an elongation of at least 1.54, and all but four at least 1.8. So the round marks number fewer than five.
- **So there was no size, not a noisy one.** The rule fell back to shape alone, and shape alone splits residue.

**The conclusion stands either way.** The rule had nothing to offer exactly where residue is worst: a sheet with no holes, or with more residue than holes.

### Section 2: the floor, and the size graded by what supports it

**`RenderDifferenceHoleDetector.SizeReference`** now says where a single hole's size comes from. The result carries it as `HoleSize`.

| Source | When | Size | Veto | Flags |
|---|---|---|---|---|
| **Calibre** | a calibre is named | the calibre times the measured ratio | it | against it |
| **Sheet** | 12 or more round marks, and no calibre | the quarter-point round mark, clamped to what a bullet can make | it | against it |
| **SheetTentative** | 5 to 11 round marks | the same quarter-point | the floor | quieter (section 7) |
| **Bound** | fewer than 5 round marks | the floor, 0.16 in | the floor | none |

- **The floor** is `SmallestHoleInches`, the smallest hole any bullet makes: 0.16 in, a .17 bullet at the scan ratio.
- **The ceiling** is `LargestHoleInches`, 0.60 in, the detector's own largest hole.
- **Why the floor flags nothing:** every real hole is larger than the smallest.

**What the floor does on a sheet with no holes.** A blob with less area than 1.5 of the smallest holes is not two holes of any calibre, so the veto and the residue rule work there.
- **On the clean Phase 0 photographs:** spurious detections fall from 60 to 24.
- **What is left.** Of the 28 residue blobs at least 2.2 long there, the 10 left are 0.20 to 0.30 in across. That is as large as two joined .17 holes. Without a calibre or real holes on the sheet, they cannot honestly be told from such a pair, and they stay split.
- **On the corpus:** nothing that registers a real sheet with holes moved. `IMG_5823`, which finds no holes, lost one spurious detection.

### Section 3: one calibre per real sheet, and two sizes asked about rather than flagged

**No real sheet in the corpus carries two calibres.**
- **Alan's submission** leaves its calibre field blank. His scan's holes form one group, 0.87 to 1.02 of .308.
- **The friend's sheet** is .308 by entry 56, and its holes range from 0.91 to 0.96.
- **The owner corpus** has one cartridge per file, by name.
- **So the synthetic flag counts are the survey's mixed population, not a sheet's.** They remain high, up to 62 marks a case on the held-out seeds. The synthetic sizes form one continuous spread, not two groups.

**Two sizes.** Where a sheet's round marks fall clearly into two groups, no size fits it:
- **The test:** each group is at least a quarter of the marks; the group medians differ by at least 1.35 in area; the gap is at least five pooled standard deviations.
- **What happens then:** no mark is flagged, the veto falls back to the floor, and the stage and the screen's summary say, in one sentence, "the marks fall into two sizes, about X and Y in across, so no one hole size fits this sheet: name the calibre to have oversized marks flagged".
- **Why five deviations.** An even spread of sizes cut in half is about 3.3 deviations apart, so a lower bar would call any wide spread two sizes. Two tight groups sit far above it: about 10 for the composite pairs below, and about 6 for two calibres on a scan.

**Its cost, stated plainly.** Merged pairs are a second size too. On the composite sheets, pairs are half the marks: at 0.10 and 0.15 in apart all 13 on a sheet merge, and at 0.20 in about 5.
- **Without a calibre** each such sheet now reads as two sizes, flags none of its merged pairs, and asks for the calibre. That is 0 of 93 merged pairs flagged, where entry 81's rule flagged all 93.
- **With .308** all 93 are flagged, and no single hole is.
- **Why it rarely matters.** A real sheet reaches this only when a quarter of its marks are merged pairs. The composite sheets were built at half to test the flag, so this is the worst case, not the common one.

### Section 4: false flags by frame

Real single holes flagged, of the true whole marks in each frame:

| Frame | Without a calibre | With .308 |
|---|---|---|
| Alan's scan | 0 of 14 | 0 of 14 |
| The friend's scan | 0 of 13 | 0 of 13 |
| The friend's photograph | 0 of 13 | 0 of 13 |
| `IMG_5820` | 0 of 13 | 0 of 13 |
| `IMG_5819` | 1 of 14 | 0 of 14 |
| `IMG_5821` | 3 of 14 | 0 of 14 |
| `IMG_5822` | 5 of 14 | 3 of 14 |
| `IMG_5824` | 6 of 12 | 3 of 12 |
| `IMG_5823` | detects no holes | detects no holes |

**On scans, and on the frames a person could reasonably take, the rate is zero or one in fourteen.** The false flags are in the oblique, focus-limited frames of entry 77.

**Two flags are right, and neither is counted above.** Each is a hole joined to residue:
- Alan's sighter hole and the print note, flagged in his scan with or without a calibre;
- the hole beside bull 10 in the friend's photograph.

### Section 5

**Recorded:** solidity does not separate the three populations, and the rule is to ask for distributions before proposing a discriminator.

### Section 6: the flag on the marking screen

**It was not there.** The screen showed only its own size check, which needs a calibre and reads printed ink. The detector's flag reached no further than a count in the trace.

**Now it reaches the marking and every place a person reads.**
- **On the marking:** `DetectedShot` and `MarkedShot` carry it as `DetectedOversize`, the area in single holes and whether it is tentative. Moving the shot clears it with the measurement it described.
- **In the marking file:** as `oversize`.
- **On the canvas:** an alert-coloured dashed ring just outside the mark, faint when tentative.
- **In the panel:** a sentence, in the alert style, or the secondary style when tentative. For example: "Shot 7 covers about 1.9 holes' area: two shots through one hole, or a hole joined to ink, would each read this way. Look at it, and add the second shot if there is one."
- **In `grouplab analyze`:** beside the shot.

**Tested.** A screen test loads a flagged and a tentatively flagged detection, finds both on the canvas and in the panel, and finds the flag gone once the shot is moved.

**The synthetic two-per-bull drop, weighed as entry 82 section 6 asks.** It is acceptable because the merges it leaves are flagged and now seen. Two shots on one bull is the case the sheet is designed to prevent, entry 75's `7a` and `7b`, so a percentage point there weighs less than one on single holes.

### Section 7: the floor on the quarter-point graded

**Grading.**
- **Below 5 round marks:** only the floor, which vetoes and never flags.
- **From 5 to 11:** the quarter-point flags tentatively, and the veto uses the floor.
- **From 12:** it is trusted.

**What a tentative flag looks like.** It is drawn faint and worded "may be two holes ... judged from too few marks to be sure. Name the calibre to check it." The summary says the size came from few marks.

**Where it applies here.** No real frame in the corpus falls in the tentative range: each has 13 or 14 round marks, or none. The grade is exercised by `TheSizeOfASingleHoleIsGradedByWhatSupportsIt`.

### Records

**Re-recorded:**
- `holes-synthetic.json` and `holes-synthetic-held-out.json`, which move only in their oversize counts;
- `detection-counts.json`, where the clean photographs lose 36 detections and three punched sheets lose one or two oversize flags;
- `ink-proximity.json`;
- the local record.

**Held-out recall and strays** are as entry 81 recorded them.

**Tests:** Core 816 passing, App 44 passing, none skipped.

---

## Entry 83. The oblique false flags were a scale defect, 5823's boundary, and the assignment editor

`docs/NOTES-FROM-PLANNING.md` entry 83, in its order:
- section 2's test first;
- section 3's three things left alone, one of them recorded;
- then section 4, the editor.

### Section 2: the falsely flagged holes are not the ones nearest ink

**The test used the per-detection distances already in the local ink-proximity record, which has no calibre. It answers no.**
- **The flagged holes on `IMG_5822`** lie 0.030 to 0.105 in from printed artwork. The unflagged ones lie 0.008 to 0.252 in, and include the nearest of all.
- **On `IMG_5824`,** flagged holes lie 0.039 to 0.190 in away and unflagged ones 0.004 to 0.324 in.
- **So this is not entry 78 section 2's residue reaching real holes.**

**What the flagged holes share is where they sit.**
- **Their size:** they read 0.33 to 0.40 in, where the frame's other holes read about 0.28.
- **Their position:** they are on the side of the sheet nearer the camera.

**The cause is in the conversion to inches.** Render-and-difference converted every blob's pixel size to inches with one scale for the whole image, the registration's scale at the page centre. On an oblique frame the local scale across the sheet runs from 0.85 to 1.30 times that, so near-side holes read up to 30 percent large.

**Measured at each hole's own scale, the false flags without a calibre fall:**

| Frame | Before | After |
|---|---|---|
| `IMG_5819` | 1 | 0 |
| `IMG_5821` | 3 | 0 |
| `IMG_5822` | 5 | 2 |
| `IMG_5824` | 6 | 1 |

**The fix, and it is a correctness fix rather than tuning.** Every size in the detector is converted at the blob's own scale, from the registration's Jacobian. That covers the size gates, the diameter, the calibre area, the veto and the flag.
- **Its test:** identical holes under a perspective that changes the scale by more than a fifth across the page read within 12 percent of each other, and none is flagged.

**At local scale, with the corpus re-recorded:**

| Frame | False flags without a calibre | With .308 |
|---|---|---|
| `IMG_5822` | 2 | 1 |
| `IMG_5824` | 1 | 1 |
| Both scans, the friend's photograph, `IMG_5819`, `IMG_5820`, `IMG_5821` | 0 | 0 |

**What it costs on the clean Phase 0 photographs.** Their spurious detections rise from 24 to 35.
- **Why:** residue on the far side of a sheet now reads at its true, larger size, so fewer slivers fall under the floor's veto.
- **Why it is left:** those are the blobs entry 83 section 3 says cannot be told from two small holes without a calibre, and it is honest to leave them.
- **What else moved:** nothing in the synthetic records, whose sheets are square to the camera.

**The ratio of detected diameter to calibre on photographs**, entry 79's 0.986 with frame means 0.92 to 1.07, was measured with the single scale. Part of its spread is therefore this defect. It should be measured again at local scale before it is used for anything finer than telling one hole from two.

### Section 3: left alone, and 5823's boundary recorded

**Left as they are, as entry 83 asks:**
- the residue blobs a calibre alone could resolve;
- the interaction between two sizes and merged pairs.

**`IMG_5823` is a measured boundary: the whole pipeline finds none of its 14 holes.**

**Its geometry.** It is the most oblique frame of submission 3a493942.

| Measure | Value |
|---|---|
| Bulls 1, 5, 21 and 25 (px per dmm) | 0.695, 0.905, 0.632, 0.809 |
| Top of the sheet against the bottom (scale) | 1.11 times |
| Left against right (scale) | 0.78 |
| Resolution along x against y (at the page centre) | 177 against 202 px per inch |
| Markers decoded | 36 of 38 |
| Marker corners kept by the registration | 42 of 144 |
| Worst bull, flat homography | 0.059 in |
| Worst bull, surface model | 0.029 in |

**What happens in the detector.** The expected artwork does not land on the print. The difference between them becomes one blob 11.1 in across that covers the sheet, and it is refused as too large, taking every hole with it. The 1 detection left is spurious.

**What a contributor can be told.** A frame whose scale changes by more than a fifth from one side of the sheet to the other, and whose registration keeps under a third of the marker corners, is past what the detector can read. Take it more square, or from further away (entry 77 section 1).

### Section 4: the assignment editor

**The detector work stops here.** What was built is the screen DESIGN.md section 13 describes and entries 69 and 70 found the model already supports.

**`ReviewQueue`, in Core, lists what the marking wants a person to look at**, each with a sentence that names no single cause and the choices that settle it, in this order:

| Kind | When | Choices |
|---|---|---|
| **Contested assignment** | the matching gave a shot a bull other than its nearest, its margin is under 0.15 in, or an edit moved it | the bull as matched, its nearest bull, the bull it was detected on, or not a shot |
| **Possibly two holes** | the detector's oversize flag | one shot, or not a shot |
| **Two shots on one bull** | a scoring bull holds more than one shot | keep them |
| **No bull** | a shot has no bull | its nearest bull, not a shot, or leave it |
| **Refused candidate** | a scoring bull with no shot, where a candidate was refused as just too small | add a shot there, or leave it out |

**The contested sentence is the concept's.** For example: "This hole is 0.706 in from bull 2 and 0.934 in from bull 1. Nearest bull says 2, but bull 2 already holds shot 2 at 0.709 in. One-to-one matching gives it to bull 1."

**How an item is settled.**
- **Resolved** once a person has chosen the shot's bull, marked it not a shot, or kept it.
- **"Keep" is remembered** on the marking and in its file as `reviewKept`, so a reopened marking does not ask again, and undo takes it back.

**On the marking screen**, a Review section heads the panel. It shows:
- **the counter**, "k of n need review";
- **Discard edits**, which puts back what detection found as one undoable step;
- **the current item** as a card, with its choices as buttons;
- **the whole queue** in order, each item marked NOW, NEXT or DONE, and each a button that selects its shot and centres the view on it.

**The keys, taken before any focused button sees them:**

| Key | Does |
|---|---|
| **Space** | moves to the next open item |
| **Enter** | takes the current item's first choice |
| **A bull's label, then Enter** | puts the selected shot on that bull; S1 is typed as S then 1 |
| **N** | marks the selected shot not a shot |
| **Escape** | clears what was typed |

**Settling one item can open another,** and the queue shows it. Putting a contested shot on a bull that already has one makes a "two shots on one bull" item.

**On the friend's scan, the fixture entry 83 names:**
- **The detection:** after it, all ten scoring shots are on their true bulls by entry 56's truth, where nearest-bull would put two on the wrong one.
- **The queue:** three items. They are entry 56's two cases, the hole beside bull 2 that is bull 1's and the hole above bull 8 that lies nearer bull 3, plus one sighter.
- **The cost:** three presses of Enter settle all of them.
- **The same measurement,** with any first choice that was wrong, costs a click on the shot, the bull's label and Enter.

**The Phase 3 gate is not met, because it cannot yet be measured.**
- **What it asks for:** DESIGN.md section 21 asks for a full 25-shot target with several misassignments corrected in under two minutes.
- **What the corpus has:** neither real sheet is a 25-shot target; each has ten scoring shots.
- **What the gate times:** a person, so it is Alan's to run.
- **What it would take:** a 25-shot sheet, shot and scanned, opened in this screen, with its misassignments corrected against a clock.
- **What the same session gives:** the ground-truth pass entry 83 asks for, saved as a marking, which every later detector change can be scored against.

**Tests.**
- **`ReviewQueueTests`** check the order, the choices, settling items one at a time, adding a refused candidate as a hand-placed shot, and keeping across a save and an undo.
- **The screen test** settles a contested shot with Enter, undoes it, reassigns by typing a bull, sees the doubled bull that makes, keeps it, and discards the edits.

### Records

**Re-recorded at local scale:**
- **`detection-counts.json`:** ten clean photographs move by a few detections each, and the punched scans do not move.
- **`ink-proximity.json`:** every photographed detection's diameter is now read at its own scale.
- **The local records** are re-recorded as well.
- **`holes-synthetic.json` and `holes-synthetic-held-out.json`** came back identical and are unchanged, because their sheets are square to the camera.

**Tests:** Core 820 passing, App 45 passing, none skipped.

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

## Entry 95. The rounds fired as a check on the count, and why the best square-on frame is six percent outside the mounted gate

`docs/NOTES-FROM-PLANNING.md` entry 95. Section 3 is reported as findings; nothing was changed because of it.

### Section 1: the turret direction, recorded

**The readout keeps where the group sits and what to dial as two labelled things**, and `ZeroingTests` holds them opposite: a group sitting right is dialled left. That shape is now the permanent one.

### Section 2: the rounds fired, and the count checked against them

**The overlapped pair is accepted as a limit of the image, not tuned.** A pair overlapping by two thirds and a torn single hole both hold about 1.3 holes of paper removed, so no size threshold separates them. What a missed second shot costs is the count, not the position.

**The shooter knows the count, so the review queue now asks for it.**
- **One field, "Rounds fired at the group, sighters not counted"**, kept in the session and the marking file as `MarkingState.ExpectedShots`.
- **Every detected mark now carries its size in holes**, flagged or not, with the two halves a split would give, as `MarkSize`. Without a calibre it is measured against the veto's size, and only the flag's size can raise the flag.
- **When the marks disagree with the rounds, the queue's first item says so**, with the candidates ranked:
  - **too few:** "You fired 10 and 9 are marked. Most likely to be two, closest to two holes' size first: shot 4 at 1.37 holes, ...", with the first candidate's "is two shots" as its keyboard choice;
  - **too many:** "You fired 10 and 11 are marked. Least like a hole, smallest first: ...", with "is not a shot" as its choice.
- **The item goes when the count agrees**, and "Leave the count" stops it asking.

### Section 3: why `IMG_5820` reaches 0.00531 in

**Seven images of Alan's sheet, measured afresh:** the scan and all six mounted frames under the surface model, the square-on pair under the homography and the flat-plus-lens model and with the centroid locator, and the four Phase 0 scans of fresh sheets against their frozen definition for comparison. All read in place on this machine; nothing was copied into the repository.

#### 1. The 0.00531 is not one bad bull. It is the ordinary worst of a frame at 0.0031 in rms

| | Scoring bulls, rms | Expected worst of 25 | Chance all 25 are under 0.005 |
|---|---|---|---|
| Fresh Phase 0 sheets, flatbed | 0.0012 to 0.0015 | 0.0025 | 1.00 |
| **Alan's sheet, flatbed** | **0.0027** | **0.0052** | **0.46** |
| **`IMG_5820`** | **0.0031** | **0.0059** | **0.17** |
| `IMG_5819` | 0.0034 | 0.0066 | 0.05 |

The worst of 25 circular-normal errors is 2.73 sigma on average. **For the expected worst to be 0.005 in, the rms has to be 0.0026 in.** `IMG_5820` is 18 percent above that, and its measured worst of 0.00531 is, if anything, a lucky draw from its own error level.

**So the question is not "what is wrong with bull 1".** It is why the whole field sits at 0.0031 in rms.

#### 2. The sheet itself uses most of the gate before any camera sees it

**Alan's scan of this sheet is twice as bad as the Phase 0 sheets on the same scanner settings:**

| | Scoring bulls, rms | Worst | Marker corner residual, rms | Printed scale, y |
|---|---|---|---|---|
| Phase 0 sheets, four scans | 0.0012 to 0.0015 in | 0.0022 to 0.0032 in | 0.0021 to 0.0023 in | 100.04 to 100.07 percent |
| **Alan's sheet** | **0.0027 in** | **0.0047 in** | **0.0035 in** | **100.43 percent** |

**A flatbed scan of this sheet passes the 0.005 in paper gate less than half the time by the same arithmetic.** It was printed on a different printer whose paper feed stretched the page 0.43 percent, seven times the Phase 0 printer's, and it has since been mounted, shot and handled. The scan's error runs smoothly down the page, dy from -0.003 in on the top row to +0.0046 in on the fourth, along the scanner's travel.

**This matters for reading the mounted gate on this sheet**: the photograph is being asked to beat a sheet whose own flatbed scan sits at 94 percent of the gate.

#### 3. The photograph's error is not the sheet's

If the photograph's bull errors were the sheet's print error, they would reproduce the scan's pattern. **They do not.**

| Scoring-bull error vectors, means removed | Vector correlation |
|---|---|
| `IMG_5820` against the scan | +0.27 |
| `IMG_5819` against the scan | -0.04 |
| The other four frames against the scan | +0.20 to +0.35 |

The photograph-minus-scan difference is larger than either image's error on its own. **Whatever sets the photograph's 0.0031 in is something the scan does not see.**

#### 4. It is set by where the camera stood, and it repeats from the same place

**Frames taken from the same position share their error pattern; frames from different positions do not.**

| Scoring-bull error vectors | 5819 | 5820 | 5821 | 5822 | 5823 | 5824 |
|---|---|---|---|---|---|---|
| **5819** | 1 | **0.83** | 0.20 | 0.23 | 0.19 | 0.11 |
| **5821** | 0.20 | 0.39 | 1 | **0.79** | -0.17 | 0.01 |
| **5823** | 0.19 | 0.16 | -0.17 | -0.15 | 1 | **0.72** |

The marker corners show the same structure in their residual magnitudes: 0.74 between the two square-on frames, 0.66 within each of the other two pairs, **zero to negative between 5821 and 5822, whose far corner was bottom right, and 5823 and 5824, whose far corner was bottom left** (-0.32 at the most), and within 0.25 of zero against the scan everywhere.

**An error that repeats from one camera position and changes with the next is systematic and view-dependent.** It is not random noise, which would not repeat, and it is not fixed to the printed sheet, which would not change.

#### 5. It lives in the registration, not in locating the bulls

On the clean bulls 11 to 25, where no hole disturbs either locator, **the edge fit and the ink centroid share each image's error pattern**: vector correlation +0.55 on `IMG_5820`, +0.83 on `IMG_5819`, +0.65 on the scan. Two locators that work differently agreeing on the error means the error is in where the registration says each bull should be.

**And the registration's own residual is what differs.** The marker corner residual under the surface model is **0.0046 to 0.0047 in rms on the square-on frames against 0.0021 to 0.0023 on a flatbed**, twice as large. If those corner errors were independent, a fit of this size over 136 corners would carry about 0.0015 in to each bull; the observed 0.0031 is what spatially correlated corner errors carry, and section 4's repeatability says they are correlated.

#### 6. What it is not

| Candidate | Test | Result |
|---|---|---|
| **The paper's bend** | Flat paper with a lens model against the surface model, same frames | The bend is real and the surface model is doing its job: flat-plus-lens reads worst 0.0127 and 0.0118 in, the surface model 0.0053 and 0.0059 |
| **Print error** | Correlation with the scan | +0.27 and -0.04: not shared |
| **Lighting across a bull** | The paper's brightness gradient around each bull, fitted and regressed against its error | Under one grey level per inch across every bull, and removing it changes `IMG_5820` from 0.00306 to 0.00303 in rms: nothing |
| **Parallax from paper height, or a radial lens residual** | Each bull's error split into the component along the line from the optical axis and across it | Both predict radial error. Radial share of the variance 0.54 on `IMG_5820` and 0.31 on `IMG_5819`, where random would be 0.5, with no trend against distance from the axis |
| **The bull locator** | Edge fit against centroid on clean bulls | They agree on the pattern (section 5) |
| **Focus** | Entry 77 section 2 | Already excluded: a square-on sheet has almost no depth range |

#### 7. What is left, and the photograph that would separate it

**Two explanations survive, and the frames in hand cannot choose between them**, because both square-on frames were taken from nearly the same direction with the sheet centred:
- **the camera:** a distortion the model's radial terms do not represent, such as the residue of the phone's own on-device correction, or corner positions biased by a point spread that changes across the field. Consistent with the corner residual growing toward the image edge on both square-on frames (rank correlation +0.40 and +0.46) and with the pattern changing when the camera moves;
- **the mounted paper:** a shape finer than the surface model's few parameters, where the sheet bows between its fixings. Consistent with the two square-on frames agreeing in page coordinates.

**The test that separates them is one more square-on pair, from the same session as the twelve photographs already asked for:** the same mounted sheet photographed square-on twice, once with the sheet in the left half of the image and once in the right half, or once and then again with the phone rotated a quarter turn. **An error fixed to the camera moves with the image position; an error fixed to the paper stays with the paper.** One of the two correlations goes to zero and the other stays near 0.8.

**Nothing was changed for any of this.** The findings say what to photograph, and one thing about the gate that is worth planning's attention: **on this sheet the mounted gate is being measured against a print that its own flatbed scan passes less than half the time.**

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

- **M3: special functions written for the engine, over a numerics package.** Nothing may be installed, and section 15.3's 1e-12 needs control of every step: the incomplete gamma prefactor through R's deviance form, and every upper tail computed as a tail.
- **M3: CorrNormal CEP through a trapezoid rule over angle on the Hoyt CDF, over Imhof's integral or a series.** The integrand is smooth and periodic, so the rule converges geometrically and matches shotGroups' hit probabilities to 1e-15.
- **M3: the minimum-volume ellipse by Khachiyan's algorithm at shotGroups' 0.001 tolerance, over an exact solver.** Section 15.3 says to match the tolerance or expect disagreement; at the package's tolerance it agrees to 3e-13.
- **M3: range statistics from ten groups per replication with running means, over simulating each group count separately.** Every cell keeps its full 10 million independent replications at a fifth of the cost of fifty-five groups.
- **M3: quantiles from 8,192-bin histograms, over storing ten million values per cell.** One bin is 7.3e-4 of the mean against a 5e-3 tolerance, and 65,536 bins ran three times slower for no measurable gain.
- **M3: the table read between rows by R's fmm spline on shotGroups' own grid, over simulating every n or interpolating linearly.** At n = 92 the spline reproduces shotGroups, and linear interpolation is 4.8e-5 off.
- **M3: a key held back only on evidence checked key by key, over excluding by dataset or loosening a tolerance.** The point of aim is detected from the fixture's own two centres, a Monte Carlo p-value by being an integer over 9999, and a disputed CEP by not being a root of its own distribution.
- **M3: the Fligner-Killeen statistic at 1e-10 relative, over 1e-12.** It subtracts n mean^2 from a sum of squares of up to 530 normal-quantile scores, which costs about three digits; the worst difference is 2.4e-12.
- **M3: the flyer expectation as an integral, over section 10's alternating binomial sum.** At 25 shots the sum's terms reach 5.2 million with alternating signs; the integral has no cancellation.
- **M3: the pre-pooling guard as pairwise F tests with Holm's adjustment, over Bartlett's test of homogeneity.** Section 11 names section 8.1's F test, and section 8.4 names Holm for its family.
- **M3: the engine's own xoshiro256** generator for resampling, over the runtime's Random.** Section 6 requires a recorded seed to reproduce an interval exactly, and the runtime does not promise its stream across versions.

- **Entry 20: bull centres from ring fits confirmed by centre dots, over the survey's annulus matched filter.** Holes break the rings and the scan's rings carry light stripes, so neither survives as a clean blob, while the dots do; the dot centroids are kept beside the ring centres and agree to a median 0.0019 in.
- **Entry 20: grid orientation found from the bulls, over trusting pixel order or reading EXIF orientation.** The photograph's pixels are stored a quarter turn from upright; lattice phase and the sighter line's 0.2-pitch offset place the grid whatever the storage, and an unmirrored layout picks the column direction.
- **Entry 19: the hole detector run on the whole photograph and on the registered sheet, over the whole photograph only.** Untuned on the whole frame it finds nothing, which is the finding; the sheet-only run shows what registration would let the same primitive do, and is labelled as that.

- **M4: the marking screen and the correction screen as one screen over one model, over a separate manual mode.** Entry 21 section 3 and DESIGN.md section 13 describe the same interactions; one immutable model gives both undo and a test surface that needs no window.
- **M4: the image shown from the pipeline's own OpenCV decode, over Avalonia's decoder.** The two can disagree about EXIF orientation, and a mark must land on the pixels the statistics and the detector use.
- **M4: the image sized by its decoded pixel grid, over the bitmap's size.** The bitmap's size follows the file's DPI tag; the headless test showed marks scaling with it.
- **M4: the UI built in C#, over XAML.** Avalonia 12 compiles bindings by default and the screen is one window; code keeps every control's wiring in one place a reader can follow.
- **M4: a headless test through the platform's pointer input, over calling the model directly.** It caught two faults that would have reached a person, which the model tests could not.
- **Entry 24: a dispersion figure withheld below five shots, over printing it with a warning.** A warning beside a three-decimal headline is what Alan read past; the count and the centre offset stay, because they are exact at any count.
- **Entry 24: each interval labelled with its exact coverage, over removing the c4 correction from the endpoints to make them 95 percent.** Brief section 5 has GroupLab match shotGroups' intervals; stating their coverage keeps that and stops the label from lying.
- **Entry 24: extreme spread's interval in the form that covers the expected spread, over shotGroups' `getRangeStat` form.** The latter covers 84.7 percent at two shots; entry 23 section 1 says to stay exact where shotGroups is not, and the harness still checks its form.
- **Entry 26: rotation as a view property of the marking state, over rotating the decoded pixels on load.** Rotating pixels changes the frame every saved position is in; a view property moves no mark, gives undo for free and reopens as it was left.
- **Entry 26: the stored pixel frame as the canonical frame, over the upright displayed frame.** It is the frame M4.1 files were already written in, so version 1 migrates without moving a mark, and it can be checked against the file itself.
- **Entry 24 section 5: the hole-size flag at nominal plus 0.132 in, over a ratio of the calibre.** Section 3.5 found the hole deficit roughly constant in absolute terms rather than proportional.
- **Entry 25: one application-wide unit setting, over a unit dropdown beside each input.** Entry 25 section 1 asks for it, and a setting that every figure obeys cannot leave one figure behind in inches.
- **Entry 25: "mil" as the milliradian, over the 6400 NATO mil of section 12.5's table.** Turrets are marked in milliradians; offering the NATO mil under the same name is the dialling error entry 25 warns about.
- **Entry 25: lengths and distance stored in inches whatever the setting, over storing in the unit the user worked in.** A file then means the same on every machine, which the test asserts by writing one marking under both settings.
- **Entry 25: printing through the system's PDF print command, over drawing pages to a printer from the application.** Avalonia has no printing API and nothing may be installed; the PDF is the path that already works, and what it cannot control is said on the screen and on the sheet.
- **Entry 25: the actual-size note as a render option, on in the print screen and off by default, over adding it to every render.** Phase 0's pages and the gates measured on them stay item for item as they were.
- **Entry 25: `/PrintScaling /None` in every PDF GroupLab writes, over only the print screen's.** A sheet printed from the command line is measured the same way and deserves the same request.
- **Entries 22 and 27: the scrubber in C#, over running `scrub_exif.py`.** Its library is not installed and nothing may be installed; the C# version follows the script's policy, keeps digital zoom for entry 27, and also removes XMP and trailing data, which the script leaves.
- **Entry 27: triage by decoded markers, holding rather than refusing what fails it.** Markers are the check the application already has; a held file costs a person one look and an `--accept`, where a refused one would need resubmitting.
- **Entry 22: the committed-image guard names the 16 photographs with GPS, over failing the suite until history is rewritten.** The rewrite is a decision for question 13; a named list keeps the suite green without letting a seventeenth image in or letting the list go stale.
- **Entry 28: rebuilding the aimed coordinates from the shot and a recovered aim, over excluding the four Fligner-Killeen keys as a known difference.** The rebuild reproduces R's doubles and all four statistics to 5.5e-13, so the gate checks something true rather than recording a gap; regenerating the fixtures at full precision would make it unnecessary.
- **Entry 28: a stated digital zoom of 0 read as 1 in the lens key, over keeping the tag's value.** The EXIF standard defines 0 as digital zoom not used, which is the geometry of 1, and every Pixel photograph in `scans/mounted/` states it.
- **Entry 28: refusing a `meta.json` whose opt-out field is missing, over treating it as false.** A missing opt-out is unknown, and publishing on unknown consent cannot be undone.
- **Entry 33: `grouplab analyze` composing `AutomaticMarking` and `GroupAnalysis`, over a separate pipeline for the command line.** The screen and the command then run the same code, so a fault found by one is fixed in both, which is how the sighter fault reached the marking screen's fix.
- **Entry 33: a printed sheet analysed even when its definition fails today's validator, over refusing it.** Validation decides whether to print a sheet; a sheet already on paper is what it is, and refusing it would make every Phase 0 sheet unanalysable.
- **Entry 33: `--target` required, over guessing the definition from the image.** Nothing reads the printed identifier or codes yet, and the built-in definitions share marker ids.
- **Entry 34: two of the owner's photographs held, over publishing all 28.** A screenshot and a messenger download cannot show who took them. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one until Alan confirms costs nothing.
- **Entry 34: a donated submission whose files were all held published as a provenance record alone, over leaving it out.** The record shows that a consented submission arrived and why none of it is published, which is the question a contributor would ask.
- **Entry 34: a separate `publish-owner` path, over running the owner's photographs through `intake`.** Intake requires a consent record, and entry 34 section 2 rules out inventing one.
- **Entry 35: a missing camera make alone is enough to hold a file, over requiring a copy's file name as well.** Entry 35 section 1 calls the make the stronger signal. A person can still accept a held donated file by name, so a false hold costs a look, and a false publication cannot be undone.
- **Entry 35: an edited phone copy that keeps its camera metadata, the four `~2` photographs, not held.** The rule is about lost camera geometry. Those copies keep their make, model and focal length, and nothing in entry 35 asks for more.
- **Entry 37: a submission whose consent cannot be read withholds its files' hashes, over ignoring it.** The rule exists because publishing under ambiguous consent cannot be undone, and an unreadable `meta.json` is the most ambiguous consent there is.
- **Entry 37: a file held for a consent conflict cannot be accepted by name, unlike a file triage holds.** Triage is a judgement about usefulness that a person can overrule; an opt-out is the contributor's decision, and only the contributor can change it.
- **Entry 37: the two conflicted submissions not published even as provenance records.** Publishing a record of a submission whose consent is in question, with its answers and credit name, waits for the contributor's answer as the photographs do.
- **Entry 36: `DFdistr` left at its committed version, over committing a JSON whose numbers are strings or editing `sg_distr.R` here.** Nothing reads it, `tools/` is the authority and planning's to change, and a fixture committed in a broken shape would be read as correct later.
- **Entries 39 and 40: any unassigned shot on a sheet of several scoring bulls withholds every figure, over quoting the assigned shots alone.** Leaving shots out without saying so would be a different wrong number, and the sentence in their place tells the user exactly what to do.
- **Entry 40: a tap on printed ink placed where it was tapped, with a note, over snapping onto the ink and naming it.** The centre of a printed stroke is never the answer, and a tap placed where the user put it is at worst as wrong as the user.
- **Entry 39: a moved shot follows its nearest bull unless the user assigned it elsewhere, over keeping whatever bull it had.** A shot dragged across to the next bull is almost always a correction of position, and a deliberate reassignment is recognisable because it differs from the nearest.
- **Entry 39: an impact placed by press, drag and release, over click, drag, click.** It is one gesture with a finger or a pointer, and a plain tap still places a shot.
- **Entry 42: a text colour below 4.5:1 moved along its own hue, over changing which role the text takes.** Section 2 says so. The contrast is measured on the three surfaces text sits on, not on the sunk image area, which carries marks. Counting the image area too would have pushed light `faint` almost onto `dim`, erasing the difference between the two.
- **Entry 42: styles rebuilt when the theme changes, over binding each control to a theme resource.** The shell is built in code, and rebuilding one style set keeps every colour decision in `AppStyles` and `Tokens` rather than spread across every control.
- **Entry 42: a unit left at its figure's size for now, over splitting the figure text.** The existing tests read that text, and section 1 requires them to pass unchanged.
- **Entry 42: mark labels in light text on a dark plate, over text in the mark's colour.** The mark's colour as text could not be read on the first screenshots, and the colour survives as a bar beside the number.
- **Entry 41: image facts from the metadata GroupLab already reads, over reading the further facts section 2 permits.** Bit depth, lens model, ISO, exposure and a count of EXIF tags would each mean reading more of a photograph's metadata for the sake of a log, which is the step the rule exists to stop.
- **Entry 41: the directory named in symbols on the first log line, over the resolved path.** Section 3 asks that the first line report where the log is, and section 3 also forbids a path. `%LOCALAPPDATA%\GroupLab\logs` satisfies both.
- **Entry 45: `last_action` as the name of the last event logged, over a separate list of action names.** Every user action already writes a stable event name, such as `print.select`, and a second list would drift from it.
- **Entry 41: Send saves the package before sending, into the log directory when the user has not saved it.** Section 7 says the zip is kept if sending fails, and the only way to guarantee that is for it to exist before the attempt.
- **Entry 41: a crash record not rewritten when a second crash lands in the same second of the same process.** The receiver's name pattern leaves no room for a counter, and the first crash is usually the cause.
- **Entry 46: an alert ring at the measured size on a flagged hole, over every impact ring at its measured size.** The size check reads an extent only for a dark region on paper, so a measured ring for every shot would silently fall back to the calibre on ink and on a dark backer. Every ring stays comparable, and the one that disagrees shows by how much.
- **Entry 35 section 6 item 3: the definition from the sheet's QR codes, over choosing among definitions by registering against each.** A frame that passes its CRC names one definition. A registration that fits well against the wrong definition is possible, because the built-in definitions share marker ids.
- **Entry 35 section 6 item 3: refuse and ask when the codes do not settle it, over falling back to the nearest match.** Eight of the 37 Phase 0 images are not identified and fall back to naming the definition. A wrong definition would produce a plausible wrong group.
- **Entry 35 section 6 item 3: the frozen Phase 0 definitions shipped beside the application, over the live library only.** The sheets already printed carry the frozen identifiers, and the print screen's list does not show them.
- **Entry 37 section 5: no size recorded for a note with no unit, over assuming inches.** Every size in the first notes had its unit, and a wrong assumption is a scale error of 25.4 or 2.54 times, which the marking screen could not detect.
- **Entry 37 section 5: the stated size offered in the rectangle prompt, over setting the scale from it.** The size is the sheet's, and only a person can say which corners are the sheet's and which side was tapped first.
- **Entry 37 section 4: the scrubber's keep list left as entry 29 set it, with `LensModel` reported rather than added.** Adding a field to what is published is a publication decision, and the lens grouping does not need it.
- **Entry 46 section 3: the count line replaces the "Placed:" line, over keeping both.** They carried the same three numbers, and the one planning asked for sits above the figures where it is read first.
- **Entry 35 section 6 item 2: the gate record workflow left failing on Linux and macOS, over passing within a tolerance.** Entry 32 section 3 asks for byte identity or an explained difference, and choosing a tolerance that makes the difference pass would be choosing the gate.
- **Entry 47: double resolution tried second, over a capability fallback.** The WeChat module is present in the runtime packages for all three platforms. The failures were detection on code modules under five pixels, which only a larger working image can help.
- **Entry 47: resolutions past 8000 px skipped, over trying every one.** Doubling a 600 DPI scan cost 52.7 s and gave nothing the scan did not, and the bound lost no image on the sweep.
- **Entry 47: identification counted per platform in the gate record workflow, over a per-platform expectation in the unit tests.** No count is known yet for Linux or macOS, and an expectation written before measuring would be a guess.
- **Entry 48: Linux's gate record difference explained and left red in the workflow, over a Linux reference record or a rule that passes it.** Making the job green needs either Linux's own records committed as its reference, or a rule about which differences pass. The first adds about 12 MB and the second is a gate written after the results, so the choice is planning's.
- **Entry 48: the owner corpus republished from the originals through `publish-owner`, over editing the published files.** Every published file still comes from the one scrubber, and the 13 files without a lens model reproduce byte for byte, which shows nothing else changed.
- **Entry 49 section 1: the printed tables committed as Windows text files, over comparing the platforms with one another inside one run.** A committed reference fails a single platform's job on its own, and it states in the repository what the record is.
- **Entry 49 section 4: the README's platform guard requires equality with the CI matrix, over naming at least as many.** A README naming a platform CI does not build would be false in the other direction.
- **Entry 49 section 2: the marker sort held for question 15, over committing it with regenerated records.** It changes committed evidence and a benchmark the Phase 1 surface work is measured against, which entry 49 did not foresee, and the question file exists for a measurement that contradicts what was written down.
- **Entry 52: every record regenerated twice under identical code, without the sort and with it, over comparing the sorted run with the committed records.** Five records were already stale, and comparing against them would have credited their changes to the sort.
- **Entry 52: tables from fits no command reproduces left as measured and marked, over updating the columns that can be regenerated.** Half a row regenerated disagrees with its other half and with the conclusions written beneath the table.
- **Entry 52 section 3: reordering applied at the homography fit, over shuffling the detector's output.** The sort puts any shuffled detection back in order, so only a shuffle after it measures the registration's sensitivity to order.
- **Entry 52 section 3: the edge fit's leave-one-out run from the converged pass's start, over rerunning the whole locator per point.** It isolates one point's weight in the fit; rerunning the locator would also move the rays and confound the two.
- **Entry 49 section 2: the journal replayed by call order, with the image hash reported beside it, over looking each detection up by its image.** A platform whose raster differs would match nothing and replay nothing, and the rerun exists to hand it Windows' corners anyway.
- **Entry 49 section 2: only the two measurements whose tables differ replayed, over all eight.** The other six already print identically on macOS, so replaying them could only repeat what the gate record shows.
- **Entry 58 section 4: the opt-out's second key is what a file scrubs to, over the decoded pixels.** It collapses the four re-exports to one value as a pixel hash would, and it keeps a consent mechanism inside `GroupLab.Core`, which has no image decoder and whose tests run on every platform.
- **Entry 58 section 3: either key alone withholds, over requiring both.** The same reasoning as entry 37 section 1's two opt-out signals: redundancy is the point, and publishing under ambiguous consent cannot be undone.
- **Entry 55 section 3 item 1: the counts recorded in the stage record and in `grouplab measure --json`, over the committed spike records.** A field in the spike records would regenerate thirteen of them and move the gate record's raw comparison, for a diagnostic that changes no figure anybody reads.
- **Entry 55 section 3 item 1: a ray counted as marginal within a tenth of the crossing threshold on either side, over counting only the rays that failed it.** A ray that just cleared the threshold is as easily flipped by the image as one that just missed it, and the tenth is the convention the leave-one-out already uses for the rejection limit.
- **Entry 61 section 3: no catch added for `PlatformNotSupportedException`, over adding one defensively.** The runtime throws `Win32Exception` for a verb off Windows, so a catch for the other would assert a behaviour that does not exist and would outlive anybody who remembers why it is there.
- **Entry 64: the working tree left alone, over running the fix as written.** Nothing differed, so the command would have been a no-op dressed as a repair, and running it would have left a false record that something was cleaned.
- **Entry 61 section 5 item 2: the tarball built on `ubuntu-latest`, over pinning `ubuntu-24.04`.** Pinning would freeze the glibc floor deliberately and freeze it silently apart from the test matrix, which tracks `ubuntu-latest`; printing the release it built on keeps the day they diverge visible, which is what entry 63 section 2 asks for.
- **Entry 61 section 5 item 2: the step fails when the native imaging library is missing, over shipping whatever publish produced.** A tarball without `libOpenCvSharpExtern.so` installs, launches, and then cannot detect a marker, which is a failure that arrives late and in front of a user rather than in CI.
- **Entries 65 to 69: the concept image left uncommitted, over committing a second copy.** The committed `docs/figures/screens/assignment-editor.png` is the same picture pixel for pixel, and the copy would add 481 KB and a content-credential block to the history for nothing.
- **Entry 65 section 4 step 2: wording that names no platform, over "on Linux".** The branch it describes runs on macOS as well.
- **Entry 70 section 3: a moved shot recorded as the difference from the bull detection gave it, over a list of notices appended at each edit.** The difference is derived from state, so undo, redo and a shot moved back all keep it true without bookkeeping, and it stays visible for as long as it stands rather than until the next edit.
- **Entry 70 section 3: the counts rule falls back to the nearest free bull, over the nearest of all bulls.** A bull a person has decided is not available to the matching in either mode, so the two methods differ only in whether the matching is forced.
- **Entry 70 section 6: three status states, over styling only the failures.** Success needs its own colour for the same reason alert does: a line that reads the same whether it worked or not teaches people to stop reading it.
- **Entry 74: pinning read from `BullChosen` alone, over `Corrected` or a chosen bull.** A shot becomes corrected when it is moved or marked not a shot, and neither of those chooses a bull.
- **Entry 73 section 1: a shot's pool is its nearest bull's, with the margin still measured to every bull, over a pool chosen by page region.** The sheet already says which bulls are sighters, and nearest-bull is the rule's own fallback; a region would be a second geometry to keep in step with every definition.
- **Entry 73 section 1: the overall method reported as nearest-bull when either pool fell back, over reporting the scoring pool's.** Reporting one-to-one would hide that a pool stopped being matched, which is the thing section 13 says must be said; the reason names each pool.
- **Entry 73 section 5: no mark size invented when no calibre is set, over drawing a nominal diameter.** A ring at a made-up diameter reads as a claim about the bullet, which is the kind of implied fact section 2 rules out.
- **Entry 73 section 7: the interval labels left at their exact coverage, over matching them.** Entry 24 decided the label states the coverage the interval actually has.
- **Entry 71: intake run on a copy without the unlisted scan, over adding the scan to the manifest.** Whether the consent covers the scan is the contributor's question, and the submission as received is left as it is.
- **Entry 71: the worst bull reported beside the worst clean bull, over the worst bull alone.** On a shot sheet the holes cut the rings of the bulls they hit, and separating the two shows the error is registration.
- **Entry 76 section 4: the printed name reverted, over landing it with a box that covers both captions.** On a sheet already printed the wider box hid a real hole, and the analyser cannot tell which caption a sheet carries; where the name goes is question 16.
- **Entry 76 section 4: detection cancelled at checkpoints between stages, over interrupting a stage.** A stage stopped halfway leaves nothing a person can use, and the checkpoints are where the work can be dropped cleanly.
- **Entry 76 section 4: a moved shot drops its measured diameter, over keeping it.** The measurement described the point the detector chose, and a ring at that size around a point a person chose would claim a measurement nobody made.
- **Entry 76 section 1: the aspect's null integrated exactly, over a simulated table.** The density has a closed form for every n, so there is no table to extend or to seed.
- **Entry 76 section 2: the two split detections merged at their midpoints for the re-run, over the pipeline's figures or hand-picked positions.** The pipeline's figures count one hole twice, and the midpoint uses only what the pipeline found, which is the question entry 76 asked.
- **Entry 75: a shot with no bull named "unassigned, at x, y" in the list, over "unassigned" alone.** Two such shots would otherwise read the same, and the position is a fact the screen has, not an order.
- **Entry 77 section 3 item 1: a blob counted as swallowed only when it passed every shape filter, over every blob centred in a zone.** Printed matter leaves residue that the size and shape filters refuse anyway, and counting it would bury the one number that means a hole may have been lost.
- **Entry 77 section 3 item 2: the check made standing by an artwork fingerprint test, over running the corpus inside the test suite.** The corpus takes minutes and its counts differ by platform. The fingerprint is fast and exact on every platform, and it forces the comparison to be run where it can be.
- **Entry 77 section 3 item 2: the committed corpus punched with synthetic holes on three offset grids, over committing a real shot sheet.** No shot sheet has consent to be committed, and the Phase 0 scans are real print made before every change since.
- **Entry 77 section 4: oversize measured three ways, over the detector's diameter alone.** Entry 73's warnings came from the screen's size check, which is a different measurement, and the test had to see the one that was reported.
- **Entry 77 section 5: a sheet with no clear place prints no name, over shrinking the name further or trying another margin.** Section 5 states the rule. One place with one clearance is also what the placement test can check on every sheet.
- **Entry 77 section 5: the identifier caption left at the bottom beside the new name line, over moving it.** It is C6's recovery path, and its zone is what every sheet already printed is read with.
- **Entry 78 section 4: the calibre only vetoes a split, over also splitting a round blob of two holes' area.** Splitting on size alone would cut a genuinely odd hole to fit the expectation, which section 4's second guard rules out; the blob is flagged instead.
- **Entry 79 section 1: a measured ratio per image kind, over the calibre or one pooled ratio.** Scans and photographs measure holes differently, 0.944 against 0.986, and section 1 asked for them apart.
- **Entry 79 section 1: a photograph without camera data taken as a scan, over refusing the ratio.** There is nothing in such a file to tell the two apart, and the one such photograph measures 0.924, within the scan ratio's spread.
- **Entry 78 section 4: a calibre named after an uncorrected detection detects again, over asking.** Nothing a person did is lost, and a result found without the size is the one section 3 says is wrong.
- **Entry 80 section 2: the old sweep stopped unfinished, over letting it run.** Its synthetic holes were the wrong size, and its real rows used the bullet diameter, so its result could only have been discarded.
- **Entry 80 section 2: the synthetic holes scaled to read like .308 on a scan, over reweighting the sweep.** A scale fixes what the holes are; a weight would only change how much a wrong population counts.
- **Entry 80 section 2: no split threshold adopted, over adopting the survivor.** The only setting the real holes allow also changes the default without a calibre and moves committed synthetic records, and no real merged pair has been measured to say what it costs.
- **Entry 78 section 2: the residue fix proposed and not built, over a plain elongation cap.** A cap alone would refuse two real holes joined by the closing, a silent loss, and the safe form depends on the threshold still open.
- **Entry 81 section 2: the oversize flag rebuilt around a single-hole size, over keeping the median rule and lowering the calibre flag alone.** The median rule flagged ordinary holes on a tight sheet and let pairs through on a sheet full of them, so a calibre-only fix would have left the loud failure silent whenever no calibre is named.
- **Entry 81 section 2: the single hole without a calibre is the sheet's 25th percentile mark, over its median.** On the composite sheets half the marks were merged pairs, and the median moved to a pair's size and flagged none of them.
- **Entry 81 section 3: the residue fix on size and elongation together, over solidity.** Solidity overlaps across all three populations, real single holes reaching 0.59, while size splits the elongated ones cleanly.
- **Entry 78 section 2: a small elongated blob kept as one hole below 2.2, over refusing every blob the size vetoes.** Refusing a real hole is a silent loss, and the margin between the most elongated real hole, 1.72, and the split threshold, 1.80, is too thin to refuse on.
- **Entry 82 section 2: the floor used to veto and never to flag, over clamping the quarter-point alone.** On the clean photographs there were no round marks to clamp, and a floor that flagged would flag every real hole, since each is larger than the smallest a bullet makes.
- **Entry 82 section 2: the floor at 0.16 in, over 0.17.** 0.17 is the bullet; a .17 hole measures about 0.944 of it on a scan, and the floor must sit below any real hole.
- **Entry 82 section 3: two sizes need a gap of five pooled deviations, over three.** An even spread of sizes cut in half is 3.3 apart, so three would ask for a calibre on any wide one-calibre sheet.
- **Entry 82 section 3: no flags at all on two sizes, over tentative flags on the larger group.** Entry 82 asks for one sentence rather than many flags, and a sheet of two calibres would have every larger hole flagged as a merge.
- **Entry 82 section 6: the detector's flag drawn beside the size check's, over merging the two.** They measure different things, the detector's residual against the size check's dark region, and each says what it measured.
- **Entry 83 section 2: sizes converted at each blob's own scale, over leaving the detector alone as entry 83 section 4 asked.** Section 2's test found a wrong unit conversion rather than a tuning question. A size read 30 percent wrong on any oblique photograph is a defect, and the fix is a few lines.
- **Entry 83 section 2: the clean photographs' rise from 24 to 35 accepted, over restoring the single scale for the veto.** The single scale was wrong for real holes and for residue alike, and the residue it hid is the kind section 3 says cannot be resolved without a calibre.
- **Entry 83 section 4: the review queue computed from the marking, over storing it.** Every edit changes what needs review, and a stored queue would go stale; only a person's "keep it" is stored, because nothing else can know it.
- **Entry 83 section 4: the editor built into the marking screen, over a separate mode with Accept and analyse.** The statistics are already live on every edit, so an accept step would commit nothing. Discard edits is kept, as one undoable step.
- **Entry 83 section 4: the review keys taken on the tunnel route, over the window's key handler.** A focused button would otherwise take Space and Enter, and pressing Space would press the last choice again.
- **Entry 87 section 1: the screen's calibre size check removed, over keeping it and feeding it into the queue.** It measures the dark region connected to a mark, so a printed ring is part of every mark that touches one, and it read five confirmed single holes at about twice their size. Adding those five to the queue would have made the queue wrong rather than the panel.
- **Entry 87 section 1: `HoleSize` kept as a measurement.** The apparent extent is what the harnesses read and what the snap radius needs, and keeping it without a threshold is what makes the removal a removal of a judgement rather than of a number.
- **Entry 86 section 3: the enclosed-ink fraction recorded on every detection although nothing reads it.** It is the quantity that separates a hole on a ring from one beside it, it cost nothing to carry, and it is what proved the hypothesis wrong rather than plausible.
- **Entry 87 section 2: the README's states tied to DESIGN.md by a test, over a review habit.** Entry 60 found the README stale for days, and the two documents can now only disagree by failing a test.
- **Entry 90: the parametric editor and the visual designer deferred, over giving them a phase.** Alan is getting a specification from Jeff, and a phase for a screen nobody has specified would be a date attached to a guess. The deferral is explicit, carries its reason, and is checked by a test, which is the difference between parking something and losing it.
- **Entry 90: assisted hole placement raised as a question, over scheduling it or dropping it.** The detector differences against a definition and a store-bought target has none, so the bullet is not schedulable as written; but two of the three meanings of "assisted" are already built, so it is not droppable either. That is a design answer and not a wording fix.
- **Entry 90: the scope test requires a citation on a deferral.** A bullet could otherwise satisfy the test with the word alone, which is exactly the quiet drop the test exists to catch.
- **Entry 88: the oversize flag left alone although the measurement points at a defect in it.** Four of five flags on real material hold one hole's worth of ink inside a ragged hull, so flagging on ink area would remove them; the threshold is one the Phase 1 gate is measured against, and entry 83 section 3 says stop tuning the detector. Recorded with its numbers instead.
- **Entry 84: `PhotographHoleToCalibre` changed to the re-measured 0.948 and `ScanHoleToCalibre` left at 0.944.** The photograph figure was measured with the single-scale defect in place; the scan figure was not, and its 0.006 move comes from a longer verified hole list rather than from the fix.
- **Entry 84: the 25-shot rehearsal recorded as a rehearsal.** It measures the software's share of the two minutes and nothing about a person, so recording it as the Phase 3 gate would put a state of "done" on a page where the thing being gated has not happened.
- **Entry 94 section 1: the oversize threshold left at 1.35 while the quantity under it changed.** Area reads about six percent lower than the hull-derived reference it is compared against, so the threshold is already slightly stricter; and the value that would catch every 0.10 in pair would also flag S1b again, which is the false flag the change exists to remove.
- **Entry 94 section 1: the hull area kept beside the mark's area rather than dropped.** Solidity is the ratio of the two, and entry 88's shape measurements rest on it.
- **Entry 94 section 4: the split's two centres computed at detection time and carried on the flag, over computing them when the choice is taken.** The review queue has no image, and a choice that needs the pixels is a choice that needs a mouse.
- **Entry 94 section 2: subgroups keyed by bull rather than by shot.** A shot moves between bulls during review and its load does not; the sheet's layout is what holds the loads.
- **Entries 91 and 92: the zero correction refuses rather than rounds.** Where the offset is inside the sampling error the panel gives a shot count instead of a number, because a bare figure will be dialled.
- **Entry 93 section 3: high contrast derived from the dark tokens in code, over a fourth hand-drawn palette.** A palette that cannot be derived is evidence the roles carry values rather than meanings, and deriving it is what proves the concept is a design language rather than one screenshot.
- **Entry 95 section 2: the count item acts on its first candidate only.** Naming three and acting on one keeps it a key press; a person who disagrees with the ranking reaches the right mark through its own item or by selecting it.
- **Entry 95 section 2: every mark gets a size in holes, measured against the veto's size where no flag size exists.** The ranking needs a size on unflagged marks, which are most of the candidates, and only the flag's own size may raise the flag.
- **Entry 95 section 3: nothing changed for the mounted gate.** The investigation found where the error lives and what would separate the two explanations left; a change made before that photograph would be tuning against the frames being gated.
- **Entry 96 section 2: the warp that passes `IMG_5819` not adopted.** It was found on the frames being gated, it makes `IMG_5820` worse, and a model chosen because it passes the frames that prompted it is not a gate result.
- **Entry 97 section 1: a shot a person placed is neutral, not teal.** Teal means the software found it on its own, and a mark a person put down or moved is not that.
- **Entry 97 section 2: the marking keeps a copy of the rifle rather than its name.** A correction read from a saved marking must be the one that was right when it was shot, whatever the record book says since.
- **Entry 97 section 2: a barrel's count grows only on a person's step.** Counting automatically on detection would count a reopened sheet twice.
- **Entry 97 section 3: the per-stage rasters left for the next batch.** The records land live and cost nothing; the rasters would need keeping images the detector currently throws away, which section 19 allows for one interactive analysis and which deserves its own measurement of cost.
- **Entry 97 section 5: the window rehearsal pinned at 10 presses.** It is its own baseline at 300 DPI, and a ceiling the next batch must not raise.
- **Entry 98 section 3: a control added that the entry did not ask for.** The holed half and the clean half are different places on the page, and without the same split on unshot sheets a positional difference would have read as hole damage.
- **Entry 98 section 2: the nominal hole is .30 when nothing better is known.** It is the middle of what the corpus carries, and the sheet's own measured holes replace it as soon as the detector has run.
- **Entry 99: the editor ports the library's solver rather than calling it.** `tools/` is planning's and Python is not shipped; the port is held to the original by rebuilding ten sheets exactly.
- **Entry 99: the fewest markers allowed is 9, the fewest any built-in sheet carries.** It is the one figure the project has shown registration holding at, on GL-LR300-T's tile, and choosing a lower one would be a guess.
- **Entry 98 section 5: only the residual is kept.** The markers, corners and rejections were already in the result, so the interactive run's extra cost is one image.
