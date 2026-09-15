# Phase 0: the registration spike, build brief

**For** the Claude Code session that follows Phase 0a in `C:\Dev\grouplab`
**Prepared** 13 September 2026, after the sample set was printed, scanned and measured
**Status** Instructions, not design. Every decision this refers to is settled elsewhere.

---

## 0. Read these first, in this order

1. **`docs/PHASE0-PRELIM.md`.** The whole thing. It is a preliminary measurement of the sample set, made with a scratch script before this session existed, and it has already answered the question everybody assumed Phase 0 would answer. Read it before you plan anything, because it changes what the spike is for.
2. `DESIGN.md` section 21, the phase table, where the gate is now two gates. Sections marked **[r4]** are what changed after the measurement.
3. `docs/FIDUCIAL-DECISION.md` sections 10 and 11. Section 10 is your measurement list. Section 11 is the OpenCV rotation, which you found and which the printed sheet has now confirmed independently.
4. `docs/PHASE0-PRINT-PROTOCOL.md` sections 5 and 6, for how the sample set was produced and what its known limitations are.

`docs/PHASE0-BRIEF.md` was the Phase 0a brief. Its section 5 is superseded by this document.

---

## 1. What Phase 0 is now for

The original framing was that Phase 0 would find out whether registration works on paper. That question is largely answered: it works, and what limits it is the printer rather than the software. Bull centres recover at 0.0021 in mean and 0.0042 worst on three separately printed sheets, and the residual is a displacement field that travels with the paper.

So the spike's job has shifted from "does it work" to four things:

1. **Reproduce the preliminary measurement with the real pipeline** rather than with a scratch script, and beat it.
2. **Run the same measurement on the off-axis photographs**, which no version of this project has ever been tested against.
3. **Take the measurements in FIDUCIAL-DECISION.md section 10** that need paper, which are now available.
4. **Verify the print-scale detection** against the deliberately mis-scaled sheet.

## 2. The gates

| Gate | Measured on | Threshold |
|---|---|---|
| Conformance test 43, unchanged | Synthetic raster of the PDF | 0.001 in worst bull-centre error |
| **Paper gate** | 600 DPI scan of each printed sheet | **0.005 in worst bull-centre error** |
| **Photograph gate** | Each off-axis photograph | **0.005 in worst bull-centre error** |
| Print-scale detection | `gl-cf25-ltr-96.2-*` against `gl-cf25-ltr-1-*` | Reported ratio 0.962 within 0.001 |

The registration residual over marker corners is **reported, not gated**, as RMS with the maximum alongside. DESIGN.md section 21 explains why, and `docs/PHASE0-PRELIM.md` section 3 shows what happens to anyone who gates on it: the residual is dominated by ink edge raggedness at roughly 1.3 pixels at 600 DPI, it does not improve with the corner refinement window, and it does not improve with a higher-order geometric model.

**A gate that fails is a finding, and a finding is diagnosed rather than redefined.** That rule has now been applied three times in this project and once, on this gate, it produced a real change. It earns that only after diagnosis.

## 3. The sample set

`scans/phase0/` holds ten printed sheets, each scanned at 600 and 300 DPI, plus one extra scan and four photographs. Printed on a Brother MFC-J430W, plain paper, Normal quality, colour, no scaling. Measured print scale x 1.00005, y 1.00058. Measured ink spread 0.01 mm per edge, which is far better than the 0.05 to 0.15 mm the documents had assumed.

| Files | What they are |
|---|---|
| `gl-cf25-ltr-{1,2,3}-{600,300}-dpi.png` | Three separately printed copies of the reference sheet. Sheet 3 is the permanent control and will never be shot |
| `gl-cf25-ltr-2-600-dpi-rot180.png` | Sheet 2 rescanned upside down on the platen. This is the experiment that proved the error field is fixed to the paper |
| `gl-cf25-ltr-96.2-{600,300}-dpi.png` | The deliberately mis-scaled sheet, scaled inside the PDF and printed at 100 percent |
| `gl-cf25-ltr-d-{blank,filled}-{600,300}-dpi.png` | The load-block variant in both print modes. Same definition, same identifier |
| `gl-lr300-t-{1,2,3,4}-{600,300}-dpi.png` | The four tiles of the 2 by 2 assembly, row major: 1 and 2 across the top, 3 and 4 across the bottom. Nine markers each |
| `20260913_1305{43,50,54,59}.jpg` | Four handheld photographs of the sheet 1 print. Samsung Galaxy Z Fold 7, 4000 by 3000, camera originals |

**Known limitations of the set, established rather than suspected.**

- **The scanner captures 8.263 by 10.763 inches of an 8.5 by 11 page**, losing 0.237 inches in each dimension. All 34 markers survive on `GL-CF25-LTR` at both resolutions. Do not treat the scan extent as the page extent anywhere.
- **The photographs are all from the same lens**, f/2.2 at 2.2 mm, which reads as the ultra-wide rather than the main camera. Two of the four are meaningfully off-axis, with top-to-bottom width ratios of 0.901 and 1.144; the other two are close to perpendicular. Marker detection finds 34, 34, 34 and 32 of 34.
- **There is no roll-media sheet and no tinted stock.** No plotter and no buff paper were available. Recorded as a gap rather than substituted.

## 4. What to build

Nothing new architecturally. Phase 0a already has `IImagingBackend`, `OpenCvSharpBackend`, the derivations and the definitions. This spike adds a measurement harness and a report, and it stays command line only.

- A `measure` verb on the CLI that takes an image and a definition, registers, locates every bull, and emits the per-bull error table plus the residual diagnostics as structured output.
- Whatever the report needs to be legible: DESIGN.md section 19 wants every stage to emit a structured record, and this is the first thing that consumes one.
- **No UI. No detection beyond markers, homography and bull location. No statistics.** Render-and-difference is Phase 1 and has its own five-criterion gate.

## 5. Bull location, which is the one method question

The preliminary measurement used an ink-weighted centroid over a 100 dmm circular window, thresholded locally at the midpoint between the 3rd and 97th percentiles inside the window. That window holds the inner annulus and the centre dot entirely and touches neither the outer annulus nor the printed label, so the ink inside it is symmetric about the bull centre. The result was stable to 0.00002 in across window radii of 90, 100 and 110 dmm.

Three mistakes were made getting there and they are recorded so you do not repeat them:

- **A square window reaches into the neighbouring bull.** At a 38.0 mm pitch the half-pitch is 19 mm, and the corners of a 15 mm square window reach 21.2 mm. Use a circular mask.
- **An unthresholded ink weight is dominated by the paper.** The paper area inside the mask is forty times the ink area, so scanner illumination falloff moves the centroid by millimetres. Threshold first.
- **The mask must clear the outer annulus, not merely contain it.** A mask at 135 dmm clips the 127 dmm outer disc as soon as the estimate is a few dmm out, and the resulting asymmetry feeds back into the next iteration.

You are free to do better than a centroid. An edge fit to the declared disc diameters would use more of the ink and is the obvious next step. Whatever you use, validate it on the synthetic raster first, where test 43 already reaches 0.0002 in, so you know the estimator is not the limit before you point it at paper.

## 6. Measurements to take, all of which now have data

From `docs/FIDUCIAL-DECISION.md` section 10, renumbered here by what is possible today.

1. **Residual against marker count.** Refit with random subsets from 4 markers up to all 34, plot residual and worst bull error against count. **Run it at nine in particular**, because that is what a 300 yard tile carries and the four tiles in the sample set are the only real test of it. This is the empirical answer to a question DESIGN.md section 9 could only reason about.
2. **Corner refinement comparison, on paper.** `NONE`, `SUBPIX` and `CONTOUR`, and for `SUBPIX` a window sweep. The preliminary work found the window makes no difference at all between 0.25 and 2.0 modules on a 600 DPI scan, which contradicts the synthetic result from Phase 0a where one module beat 0.3 modules. Find out which regime the difference lives in.
3. **Adaptive threshold window on 600 DPI input**, and whether scans should be downsampled before detection. The 300 DPI scans in the set are the natural comparison: the preliminary measurement got 0.0031 in mean at 300 against 0.0021 at 600.
4. **Corner localisation, OpenCV against the AprilTag reference implementation.** Read section 11 of the fiducial document first. `tools/fiducial` renders its markers with OpenCV, so they are rotated relative to what is printed; this measurement must render from the GroupLab renderer or it will measure the rotation.
5. **Print-scale detection**, against the 96.2 percent sheet.
6. **The systematic and random split of the displacement field.** Three sheets give you this directly, and the rotated scan gives you the paper-fixed confirmation. Report both components. A printer whose systematic component is known is a printer whose systematic component could one day be calibrated out, and knowing the size of that prize is worth one table even though nobody should build it now.

**Not possible yet: marker module size.** Measurement 2 of the fiducial document's own list wants the same target printed at 0.3, 0.4, 0.5, 0.6 and 0.8 mm modules. The renderer cannot vary the module yet and no such sheets exist. It is queued for the next time paper is involved, along with the shot targets. **Do not ask for a print run.** Paper requests are batched deliberately.

## 7. The photograph path, which has never been tested

This is the part of the spike with the most unknown in it. Nothing in this project has ever processed a camera image. DESIGN.md section 11 specifies a distortion model on the assumption that many well-spread markers make one fittable, and that assumption has never met a photograph.

Expect to find things. In particular:

- A homography alone will not be enough if the lens has real barrel distortion, and an ultra-wide does.
- The four photographs bracket the problem usefully: two near-perpendicular frames isolate lens distortion from perspective, and two off-axis frames at opposite tilts stress the perspective term.
- If the photograph gate fails, **the lens is the first suspect, not the code.** Two frames from the main camera settle it in about a minute and the control sheet is clean and available for exactly that. Say so in the report rather than working around it.

## 8. What not to do

- **Do not change the geometry.** `tools/layout/` is the authority and it passes.
- **Do not relax a gate to make it pass.** Diagnose, report, and propose with the measurement attached.
- **Do not request paper.** Anything needing a new print goes on the batched list for the next session.
- **Do not build detection beyond what the gate needs.** No differencing, no candidate extraction, no classification.
- **Do not reintroduce a bright-core test, a radial-symmetry test, or an absolute intensity threshold.** All three are struck for measured reasons in DESIGN.md sections 6 and 12.

## 9. Reporting

Report once when the spike is done, or immediately if something blocks you. Keep asking when you hit a genuine decision; the two questions raised during Phase 0a were both worth the interruption and both changed the specification.

The report should carry, per sheet and per photograph: worst and mean bull-centre error, marker count detected, residual RMS and maximum, and pass or fail against the two gates. Plus the six measurements from section 6 as their own tables. Push to `phase-0` after each milestone commit.
