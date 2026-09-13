# GroupLab Phase 0a and Phase 0: build brief

**For** the first Claude Code session in `C:\Dev\grouplab`
**Prepared** 13 September 2026, at the end of the planning session
**Status** Instructions, not design. Every design decision this refers to is settled elsewhere.

---

## 0. Read these first, in this order

1. `DESIGN.md` revision 3. The shape of the thing. Sections marked **[r3]** are the ones that changed after measurement.
2. `docs/TARGET-SCHEMA.md`. The format you are implementing. Section 10 is the conformance list and it is your test plan.
3. `docs/TARGET-LIBRARY.md` sections 3, 4 and 5. The twenty sheets you have to render, with validated geometry.
4. `docs/FIDUCIAL-DECISION.md` sections 1 and 8. What a marker is and where it goes.

Skim `docs/DETECTION-PIPELINE.md` and `docs/STATISTICS.md`. You do not need them yet, but knowing what S0 through S10 are will stop you designing something the pipeline cannot use.

Do not read the output-format skills or start scaffolding before you have read the schema. The format is unusual in two ways that will bite an implementation written from habit: **every length is an integer number of tenths of a millimetre**, and **rings are stacks of filled discs, not stroked circles**. Both are load-bearing and both are explained in TARGET-SCHEMA.md section 2 and section 3.4.

---

## 1. Why Phase 0a exists

DESIGN.md revision 2 made the registration spike the first phase. It cannot be, because the spike measures fiducial detection against a printed target and no target with fiducials exists. All fifteen scans in `scans/` run the bull-centre fallback path.

So something has to render a definition onto paper first. That is Phase 0a, and its gate is the one test that validates the whole format without a printer or a scanner in the room.

---

## 2. Phase 0a: what to build

Command line only. No UI, no Avalonia, no Windows-specific code.

**Project layout.** `src/GroupLab.Core` as a netstandard or net9.0 class library, `src/GroupLab.Cli` as the console entry point, `tests/GroupLab.Core.Tests` as xUnit. Nothing else yet. The imaging backend interface of DESIGN.md section 7 should exist from the first commit even if it has one implementation, because retrofitting it is the mobile port becoming a rewrite.

**Four deliverables:**

| | Component | Source of truth |
|---|---|---|
| 1 | GLTD-J reader, writer and validator | TARGET-SCHEMA.md sections 3 and 9 |
| 2 | GLTD-B encoder and decoder | TARGET-SCHEMA.md section 5 |
| 3 | The twenty built-in definitions as GLTD-J files in `targets/` | TARGET-LIBRARY.md sections 4 and 5 |
| 4 | PDF renderer | TARGET-SCHEMA.md section 3, plus the print constraints in section 7 |

**Order matters.** Build the encoder before the renderer. The encoder is 200 lines, it is fully testable against `tools/gltd/encode.py`, and getting it right first means the definition identifiers in the built-in library are correct before anything is printed with one on it.

**Things that will catch you out, listed because they caught the planning work out:**

- **The canonical JSON form is specified** in TARGET-SCHEMA.md section 6: key order as given in the spec, two-space indent, LF, UTF-8 without BOM. The definition identifier is the SHA-256 of the binary body, not of the JSON, so it does not depend on any of that. Do not hash the JSON.
- **`instance` and the tile index are excluded from the hash.** Two sheets with the same layout and different load data are the same definition. Two tiles of one assembly are the same definition. TARGET-SCHEMA.md section 3.11 and 3.12.
- **The paper knockout is ink index 15 and is never written into the ink table.** A disc painted in it lays no ink, it reveals the substrate. In PDF that means not drawing, not drawing white.
- **`grid-boundary-1` and its siblings are derivation rules with a versioned name.** The drop test is part of the rule. Recompute the marker list and compare against any stored list; a mismatch is an error, not a repair.
- **Derived layouts use `round(extent * i / n)` per element**, never a stored pitch multiplied out. This is how the data-block field cells and the zeroing grid lines stay integer without accumulating error. TARGET-SCHEMA.md section 3.10 and 3.13.

**Reference implementations to check against, both in `tools/`:**

- `tools/gltd/check.py` prints body size, frame size and definition identifier for six representative sheets. Your C# encoder must produce the same bytes and the same identifiers. If it does not, one of you is wrong and the Python one is not authoritative, so read both.
- `tools/layout/run.py` prints the validated geometry of all twenty sheets and `tools/layout/layouts.json` has it in full. Your built-in definitions must match it exactly. It ends with `16 multi-bull layouts, 4 zeroing sheets, 0 failing`.

---

## 3. Phase 0a gate

**The gate is conformance test 43 of TARGET-SCHEMA.md section 10.**

> Generate a target, render it, analyse the rendered image as if it were a scan, and confirm that every bull centre is recovered at its declared coordinate to within the Phase 0 residual gate.

One thousandth of an inch, on a synthetic render, with no printer and no scanner involved. It closes the loop between the format, the renderer and the analyser, and it is the test that catches a disagreement between the two halves of rule R5 before it reaches a user. Passing it means the renderer and the analyser agree about what the definition says, which is the only thing Phase 0 can then be honest about measuring.

To run it you need enough of the detection side to find markers and solve a homography. That is a subset of S2 and S3 from DETECTION-PIPELINE.md and nothing else: no differencing, no candidate extraction, no classification. Resist building more.

**Secondary gates, all from TARGET-SCHEMA.md section 10:**

- Tests 1 to 4, round trip. Every built-in encodes to GLTD-B and back to a byte-identical canonical document.
- Tests 5 to 11, decoding robustness. Bad CRC, unknown wire version, reserved flag bit set, truncated frame, oversized `totalLen`. **Test 9 matters more than it looks: never allocate on an untrusted length before checking it.**
- Tests 12 to 26, validation. In particular test 20, strictly decreasing disc diameters, and test 24, no bull crossing a tile boundary.
- Tests 38 to 42, rendering. Especially test 41, that a paper-ink disc lays no ink, and test 42, that annulus ink extents match the difference of the declared diameters to within half a device pixel at 300 and 600 DPI.

---

## 4. Between the phases: print something

Print, at 100 percent with fit-to-page off:

- `GL-CF25-LTR`, the reference sheet
- `GL-CF25-LTR-D`, the load-block variant, once blank and once filled, to confirm they produce identical geometry
- `GL-CF25-LTR` again at **96.2 percent**, deliberately, because that is DESIGN.md's own print-scale example and it makes a good regression fixture
- One 300 yard tile assembly, four sheets, so the pooling path has real input eventually
- One roll sheet if you have plotter access. `GL-LR300-R36` is the middle case and the one that will expose any roll-media assumption

Scan each at 600 and at 300 DPI. Photograph at least one off-axis. Keep them in `scans/phase0/` and write a `SAMPLE-NOTES.md` entry for each, because the existing corpus is well documented and the new one should not be worse.

---

## 5. Phase 0: the registration spike

Gate unchanged from DESIGN.md: registration residual under one thousandth of an inch across the entire page including corners, on both the scan and the photograph, and correct detection of the deliberately mis-scaled print.

**Five measurements to take while you are there**, from FIDUCIAL-DECISION.md section 10. They turn a pass-or-fail gate into something informative:

1. **Residual against marker count.** Refit with random subsets from 4 markers up to all 34 and plot residual against count. Run it at **nine** in particular, because that is what a 300 yard tile carries and nobody knows yet whether nine is enough.
2. **Residual against marker module size.** Print at 0.3, 0.4, 0.5, 0.6 and 0.8 mm and measure. This finds the real dot-gain floor on your printer, which no literature can supply.
3. **Corner refinement comparison.** None, subpixel and contour, same images.
4. **Adaptive threshold window on 600 DPI input**, and whether scans should be downsampled before detection.
5. **Corner localisation, OpenCV against the AprilTag reference implementation.** Two detectors can read the sheet and they use different quad-fitting front ends, so they will not give identical corners. Which is better decides which is primary on mobile.

Measurements 5 and 6 from that list are already done and are in FIDUCIAL-DECISION.md section 4.1. Do not redo them; `tools/fiducial/run_all.sh` reruns them if you want to.

---

## 6. What not to do in this session

- **No UI.** Not a window, not a preview pane. DESIGN.md's phase order puts the editor at Phase 3 and it is right.
- **No detection beyond markers and homography.** Render-and-difference is Phase 1 and it has its own five-criterion gate.
- **No statistics.** Phase 2, and the fixtures already exist at `tools/shotgroups/`.
- **No mobile, no Emgu.CV, no OpenCvSharp mobile packages.** Windows only. OpenCvSharp under Apache-2.0 is the desktop binding and `OpenCvSharp4.runtime.win.slim` will not work: it compiles out all 77 ArUco exports and fails at run time only. Use the full `runtime.win`.
- **Do not change the geometry.** If a sheet does not fit, that is a bug in the renderer or a finding worth reporting, not a licence to nudge a coordinate. `tools/layout/` is the authority and it passes.
- **Do not reintroduce a bright-core test, a radial-symmetry test, or an absolute intensity threshold.** All three are struck for measured reasons, in DESIGN.md section 6 and 12.

---

## 7. If something in the specification is wrong

It might be. The planning session found and corrected several of its own errors, including a QR capacity read from memory, a fiducial rule that degenerated silently at coarse pitch, and an escape route that did not exist.

Report it rather than working around it, and prefer a measurement to an argument. The pattern that worked repeatedly was: write the smallest script that answers the question, run it against the real files, and let the number decide. `tools/` is full of those and they are all rerunnable.

---

## 8. Commit conventions

Attribution lines for commits and pull requests are in the session reminder. Repository is public from the first commit, per DESIGN.md section 20, so the history is part of the deliverable. Markdown for anything in the repository.

No em dashes in any written output. No pseudoscience: barrel harmonics, optimal barrel time, velocity nodes and accuracy nodes are not real and must never appear in documentation, UI copy or code comments. No OnTarget compatibility of any kind.
