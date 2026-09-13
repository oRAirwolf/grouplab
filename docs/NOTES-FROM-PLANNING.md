# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## 2026-09-13, entry 9: the bracketing rule, accepted with one change to finding 3

**Status: actioned 2026-09-13.** Question 4 answered. The rule, its evidence and tests 23 and 26f are in TARGET-SCHEMA.md sections 7 and 10; the validator warns on the four sheets and exempts a bracketing shortening from test 23; the finding and the deferred commit are `docs/PHASE0-RESULTS.md` section 4.4.

This answers question 4. The measurement across the whole library is exactly what the rule needed before it landed, and it found two things entry 5 did not.

**Both of your decisions, as you recommended them.**

**Inclusive**, so a bull centre on the lattice boundary conforms. Both 300 yard tiles sit at a margin of zero on every side, and a strict rule would fail them with no fix short of a denser scheme, for a bull that is interpolated along the edge it sits on. Your wording already reads inclusive with "on or inside"; keep it.

**A warning first, an error once the three sheets are fixed.** A rule that cannot be satisfied by any sheet in the library on the day it lands is a rule nobody will believe. Warn now, name the three sheets in the warning text, and promote it to an error in the same change that fixes them. Add the promotion to the deferred geometry work so it does not become permanent: `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` join `GL-CF25-LTR` in one geometry commit after Phase 0 closes, and test 26f becomes an error in that commit.

**Finding 1 is worse than it looks and should be recorded as such.** On `GL-CF25-100M-A4` the outermost bull columns are 200 dmm outside the lattice and on the two rolls they are 508 dmm, against the 266 dmm that produced the sighter failure. These are not marginal cases riding on a technicality; they are the same defect at the same scale, on the horizontal axis, and the reason nobody saw them is that no photograph of those sheets exists. Say that in the results document, because it is the strongest argument for the rule: the defect was found once by accident and the sweep found three more.

**Finding 3 has a cheaper answer than any of your three.** Do not add `sighterGap` to the body, do not exempt decoded documents, and do not scope test 23 by provenance. **Change what test 23 warns about.** The sighter gap is already recoverable from the body: the sighter block carries its origin and the grid block carries the last scoring row, so any decoder can compute the gap. What it cannot recover is whether the departure was deliberate, and that is the only thing `cells.sighterGap` declares. But a departure that makes the lattice bracket is self-evidently deliberate, because that is the sole reason the solver shortens it.

So:

> 23. A sighter row whose gap differs from 1.2 times `pitchY` by more than 1 dmm, with no `cells.sighterGap` declared, is a warning **unless the shortened gap is what brings the sighter row inside the fiducial lattice, in which case it is not**.

No new byte, no new field, no provenance test, and the two rules stop contradicting each other. Propose the exact wording alongside 26f.

**Your proposed wording for section 7 is good and I would change one thing.** Drop "and the Phase 0 photographs measured the cost" down to a following sentence rather than embedding it in the rule, so the rule reads as a rule. The evidence belongs immediately after it and should now cite the sweep as well as the photographs.

---

## 2026-09-13, entry 8: the wall photographs are my fault, and A is right

**Status: actioned 2026-09-13.** Question 5 answered; the curved-sheet registration requirement, with its figures and its limit, is `docs/PHASE0-RESULTS.md` section 4.5, and the wall set is cited there and in section 3a as the Phase 1 baseline. The photograph gate is the spike's one open item, awaiting two frames of a flat sheet.

This answers question 5. You are right, the set is unusable for its purpose, and the reason is the instruction rather than the photography.

`docs/PHASE0-PRINT-PROTOCOL.md` section 7 says "Pin or tape it to a wall", and entry 6 repeated "taped flat to a wall" as though those were the same thing. They are not. A sheet hanging from one pin at the top centre curls away from the wall under its own weight, and your measurement shows it: the two lowest marker rows sit 0.026 to 0.138 in from the fit on every wall frame against 0.0025 to 0.0033 on the table frames, and the wall set is six to sixteen times worse by worst bull with the same lens. The protocol will be corrected.

**Option A, and it is the last photograph request.** Two frames, main camera, sheet 3, one square-on and one about twenty degrees off-axis, whole sheet in frame with a margin, no flash, camera originals.

**Flat means all four edges restrained.** Masking tape along each of the four edges onto a wall or a door, or the sheet laid on a table under a sheet of glass or clear acrylic, or under a pane from a picture frame. A clipboard or a single pin is not flat. If tape on all four edges is awkward, the glass-on-a-table version is easier and better, and an off-axis frame of a sheet lying on a table is just as valid a test of perspective as one on a wall.

**Do not wait for it to run the rest.** Everything else in the spike is done or unblocked, so close out what you can and treat the photograph gate as the one open item.

**Option C is a Phase 1 requirement, and record it now.** You are right that a pinned sheet is the normal case at a range, so local or piecewise registration from nearby markers is not a workaround for a badly mounted test, it is the real-world path. Your own numbers already show what it buys: nearest-marker registration took the worst scoring bull from 0.0053 to 0.0035 in and 0.0066 to 0.0029 on the table frames. Write it into `docs/PHASE0-RESULTS.md` as a Phase 1 requirement with those figures attached, and note the limit you found: it cannot help a bull that no nearby marker brackets, which is the sighters, which is entry 5 again from a third direction.

**One thing the wall set does establish, so do not discard it.** It is the first evidence in the project of how badly a curved sheet registers under a global homography, and it is a realistic curvature rather than a contrived one. Keep all nine committed and cite them as the baseline that piecewise registration has to beat at Phase 1.

---

## 2026-09-13, entry 7: commit the raw measurements, not just the summaries

**Status: actioned 2026-09-13**

A request, and the reasoning behind it, because it changes what you write rather than how much.

`docs/PHASE0-RESULTS.md` and your three questions are well written and I can act on them, which is the point. What I cannot do from them is check your arithmetic. When you reported in question 3 that the split is 0.0013 systematic and 0.0005 random, I accepted it, because your rebuttal of my estimator argument was clearly right and the direction was obvious. But I accepted it rather than verified it, and that is the wrong relationship for numbers this project will lean on for years.

**So commit the rows, not only the tables.** For every measurement that produces per-element data, write the raw values as a file under `scans/phase0/measurements/`, one file per measurement, JSON or CSV, whichever is natural. Per-bull error with its label and its declared coordinate. Per-marker residual with its id. Per-image, per-locator, per-detector. Include the fitted transform where a reader would need it to reproduce the numbers.

The summary tables stay exactly as they are. This is additional, and it should cost you almost nothing, because the arrays already exist inside the harness at the moment the table is printed.

**Why this and not a transcript.** The suggestion was raised that you log everything in the panel so the planning session can read it. That would be a great deal of text for very little signal: the conclusions are already written down, and what a transcript adds is mostly the path taken to reach them. Raw measurement rows are the opposite trade. They are small, they are exactly the thing a second party needs, and they turn every figure in the results document from something to be trusted into something to be checked. Your catch on my centroid is the argument for it: that happened because my method was written down in enough detail to be attacked. Give me the same target.

**One thing a transcript would genuinely add**, so capture it deliberately instead: a short decision log. Where you had a real choice and took one branch, one line saying what the alternative was and why you rejected it. You already do this in `docs/SPEC-ERRATA.md` for specification choices. Extend the habit to method choices, like picking the edge fit over the centroid, or the refinement window, or the shape gate ordering. A sentence each, in `docs/PHASE0-RESULTS.md`.

---

## 2026-09-13, entry 6: nine photographs across three lenses, and the lens identity is settled

**Status: actioned 2026-09-13**

Nine new photographs of sheet 3, the clean control, taped flat to a wall, are committed in `scans/phase0/`. Three lenses, three frames each, named by lens. This is more than option A asked for and it separates the lens question completely.

**The lenses are genuinely distinct**, and the naming is correct:

| Lens | f-number | Focal length | 35 mm equivalent |
|---|---|---|---|
| `ultrawide1-3` | f/2.2 | 2.20 mm | 13 |
| `main1-3` | **f/1.7** | **6.25 mm** | **23** |
| `telephoto1-3` | f/2.4 | 7.00 mm | 69 |

**The original four frames were the ultrawide, and your EXIF puzzle has an answer.** `20260913_130543.jpg` reports f/2.2 at 2.20 mm with a 35 mm equivalent of 23. The physical focal length and aperture match `ultrawide1-3` exactly; only the equivalent tag disagrees, and that tag is computed by the camera app rather than read from the hardware. **Identify the lens from `FocalLength` and `FNumber`, not from `FocalLengthIn35mmFilm`**, which this phone writes inconsistently. So your section 3 uncertainty in question 1 resolves to: it was the ultrawide, and the lens term you were fitting was real.

**Marker detection, OpenCV with subpixel refinement, out of 34:**

| Frame | Markers | Keystone, top-to-bottom width ratio |
|---|---|---|
| `main1` | **34** | 0.985 |
| `main2` | 27 | 0.908 |
| `main3` | 31 | 1.129 |
| `ultrawide1` | **34** | 0.996 |
| `ultrawide2` | 31 | 0.831 |
| `ultrawide3` | 24 | 1.225 |
| `telephoto1` | **6** | unusable |
| `telephoto2` | 32 | 1.064 |
| `telephoto3` | **3** | unusable |

**Telephoto 1 and 3 do not contain the whole sheet.** At a 69 mm equivalent from standing distance the sheet overflows the frame, so those two are not a detector failure and should be excluded rather than reported as one. `telephoto2` is usable. It is worth keeping all three committed, because "the user zoomed in and cut off two corner codes" is a real failure mode the application will meet, and it is now in the corpus.

**No measurement figures from me on these.** I ran my scratch pipeline over them and it produced bull errors six times worse than yours on the original four frames, which means my tool is wrong for perspective images rather than that these photographs are bad. It was written for flatbed scans, where the transform is nearly affine, and its distortion fit does not converge. Use your own pipeline and disregard anything my earlier photograph numbers might have implied.

**What this set is for.** Three questions your data could not previously separate:

1. **Lens.** Same sheet, same session, same flatness, three focal lengths spanning 13 to 69 mm equivalent. If the gate tracks focal length, the distortion model is the problem. If it does not, the lens was never the cause.
2. **Flatness.** These are taped flat to a wall; the original four were lying on a table. `ultrawide1-3` against `20260913_1305*` is a clean paired comparison, same lens, only the flatness differing. That is the direct test of your section 2 finding about local registration.
3. **Sighter geometry.** If the scoring bulls pass on a flat sheet with a well-behaved lens and only the three sighters fail, entry 5 is the whole answer and it is confirmed rather than inferred.

Report the photograph gate across all nine, grouped by lens, with the scoring bulls and the sighters called out separately.

---

## 2026-09-13, entry 5: the sighter row misses the marker lattice by 2 dmm, and that is the photograph finding

**Status: actioned 2026-09-13.** The geometry is unchanged. The finding, the sweep reproducing this entry's gaps, and the deferral are `docs/PHASE0-RESULTS.md` section 4.4; the section 7 wording, the conformance test and the three sheets a sighter gap cannot fix are `docs/QUESTIONS-FOR-PLANNING.md` question 4.

This answers question 1. Your diagnosis is right, your ranking of the causes is right, and the geometry cause is far cheaper to fix than you costed it, because it does not need a rule change.

**The measurement.** On `GL-CF25-LTR` the fiducial lattice would place a marker row at y 2705, below the sighters. Its box bottom lands at 2735 and `layout.py`'s edge test rejects anything past 2734, which is half the 120 dmm safe margin. **The row is dropped by one dmm.** That single lost row is why the three sighters are the only bulls on the sheet outside the lattice, and it is why they are the worst bull on three of your four photographs and on two of the three flat scans.

**It is not a defect in `grid-boundary-1`.** Sweeping the whole library, thirteen of sixteen sheets bracket their sighters already. Three do not:

| Sheet | Default gap | Gap that brackets | Move |
|---|---|---|---|
| GL-CF25-LTR | 456 | **454** | **2 dmm, 0.2 mm** |
| GL-LR300-R24 | 1219 | 1142 | 77 dmm |
| GL-LR300-R36 | 1219 | 1142 | 77 dmm |

So the reference sheet, the one everything is measured against, misses bracketing its own sighters by two tenths of a millimetre.

**The fix is option B, and it costs almost nothing.** Not a change to the placement rule, which stays immutable as `grid-boundary-1`, but a change to the sighter gap on three sheets, declared through `cells.sighterGap`, which section 3.6 already provides as the sanctioned override for a deliberate departure from the 1.2 convention. At a gap of 454 on `GL-CF25-LTR` the validator passes with zero errors, the marker count rises from 34 to 38, and the new row sits at y 2704, below the sighters.

**Add this as a rule, because it is the general statement of the fault.** The fiducial lattice must bracket every bull, sighters included. A bull outside the lattice is interpolated on a flat scan and extrapolated on anything that is not flat, and your photographs are the first thing that ever made the difference visible. Propose the wording for TARGET-SCHEMA.md section 7 and a conformance test alongside it, and put the general form in the solver: default to 1.2 times pitch, reduce minimally until the lattice brackets, declare `sighterGap` when it differs.

**What it costs.** The three sheets get new definition identifiers. On `GL-CF25-LTR` the scoring rows also shift by 1 dmm, from 539 to 540 and so on, because the row solver's bottom limit depends on the sighter position. That is 0.1 mm, which is a fifth of the printer's own measured placement error, so **the committed sample set stays valid evidence for everything except the sighters**. No reprint is needed now; it goes on the batched paper list.

**Do not apply the geometry change during this spike.** I had this the wrong way round when I first wrote the entry. The sample set on disk was printed from the current definitions, so changing `targets/GL-CF25-LTR.gltd.json` now would move every declared bull by 1 dmm while the scans still show the old print, and every number in `docs/PHASE0-RESULTS.md` would shift under you for a reason that has nothing to do with registration. Finish the spike against the geometry that was actually printed.

What to do now instead: record the finding in `docs/PHASE0-RESULTS.md` with the table above, propose the wording for the TARGET-SCHEMA.md section 7 rule and its conformance test, and say in the report that the fix is identified, costed and deferred. The change itself lands as its own commit after Phase 0 reports, and the next print run is the first to carry it. That also keeps the brief's "do not change the geometry" instruction intact rather than quietly overriding it mid-spike.

**Sequence.** This does not remove the need for option A. Your flatness evidence is independent and strong: local registration from the nearest six or eight markers improves the worst scoring bull on three photographs and the sheets were shot lying on a table rather than taped flat as the protocol asks. Do the geometry fix, and the two main-camera frames are being requested separately. Report the photograph gate against both, and if the scoring bulls pass while the sighters still fail, that is the finding and it stands on its own.

Do not adopt option C. Gating on scoring bulls only would have hidden this.

---

## 2026-09-13, entry 4: PHASE0-PRELIM was wrong about its own estimator

**Status: actioned 2026-09-13.** Dated amendments in `docs/PHASE0-PRELIM.md` sections 3, 5a and 6 and DESIGN.md section 21; the corrected split is `docs/PHASE0-RESULTS.md` measurement 6.

This answers question 3, and you are right. Take option A.

`docs/PHASE0-PRELIM.md` section 3 claimed "not the measurement method", and the argument given for it was stability across mask radii of 90, 100 and 110 dmm. Your rebuttal is correct and I should have seen it: all three masks hold the same ink, the inner ring and the dot, so the comparison could only ever show that the estimator was insensitive to how much paper surrounded it. It said nothing about the estimator class. The edge fit uses the outer ring as well and locates each edge against its own local ink and paper levels, which is a better instrument, and it was chosen on the synthetic raster before it saw paper exactly as the brief required.

**Amend both documents**, with a dated note citing the spike rather than rewriting the original text, so the record shows what the scratch measurement claimed and what the real one found:

- `docs/PHASE0-PRELIM.md` section 3, the "not the measurement method" bullet, and section 5a's split.
- `DESIGN.md` section 21, which repeats 0.0019 and 0.0010.

The corrected split is **0.0013 in systematic and 0.0005 in random**. Also record the third point, which is new information rather than a correction: a quadratic over the page leaves only 0.0004 mean and 0.0007 worst, so most of the systematic field is smooth, and a per-printer calibration would recover most of it rather than a fraction. That is worth a sentence in section 6 of `docs/PHASE0-PRELIM.md` where the prize is described.

Nothing about the gate changes. Five thousandths holds with more margin than before.

---

## 2026-09-13, entry 3: libapriltag corners, as requested

**Status: actioned 2026-09-13.** Re-run through the Phase 0 pipeline as `grouplab spike detectors`: `docs/PHASE0-RESULTS.md` measurement 4 and `docs/FIDUCIAL-DECISION.md` section 10, measurement 8, including the half-pixel convention finding.

This answers question 2. `scans/phase0/apriltag-corners.json` is committed, covering all five images from the same run as entry 2, all 34 markers detected in every one.

The file carries the convention note in its `_note` field: against a model ordered top-left, top-right, bottom-right, bottom-left, reverse the winding with no rotation, where OpenCV needs its list rotated by two.

Re-rank the detectors with your edge-fit locator and report whether the ranking holds. If it flips, that is a finding and it belongs in the report, because the ranking under the scratch centroid was consistent five images out of five and a reversal would say the estimator, not the detector, was driving it.

---

## 2026-09-13, entry 2: measurement 4 is done, do not install anything

**Status: actioned 2026-09-13.** Recorded in `docs/FIDUCIAL-DECISION.md` section 10, measurement 8, with the corner conventions; nothing was installed.

Measurement 4 of the spike brief, corner localisation between OpenCV and the AprilTag reference detector, has been run externally on the same scans. Nothing needs installing on this machine: no second Python, no venv, no build tools.

**Method.** OpenCV `DICT_APRILTAG_36h11` with `CORNER_REFINE_SUBPIX` at a 12 px window, against libapriltag through `pupil-apriltags` 1.0.4 with `quad_decimate` 1.0 and `refine_edges` on. Same images, same declared model corners, a homography fitted per detector, then bull centres located by an ink-weighted centroid in a 100 dmm circular mask.

Both detectors find 34 of 34 markers on every 600 DPI sheet.

| Image | resid cv | resid at | bull cv mean/worst | bull at mean/worst |
|---|---|---|---|---|
| `gl-cf25-ltr-1-600` | 0.00212 | 0.00205 | 0.00224 / 0.00418 | 0.00236 / 0.00506 |
| `gl-cf25-ltr-2-600` | 0.00203 | 0.00198 | 0.00214 / 0.00414 | 0.00213 / 0.00447 |
| `gl-cf25-ltr-3-600` | 0.00201 | 0.00198 | 0.00206 / 0.00421 | 0.00226 / 0.00492 |
| `gl-cf25-ltr-2-600-rot180` | 0.00198 | 0.00192 | 0.00333 / 0.00715 | 0.00412 / 0.00832 |
| `gl-cf25-ltr-1-300` | 0.00209 | 0.00173 | 0.00305 / 0.00555 | 0.00375 / 0.00765 |

All figures in inches. Mean worst bull over the four 600 DPI images: **OpenCV 0.00492, libapriltag 0.00569**.

**The two rankings are inverted, consistently, five images out of five.** libapriltag always fits the better corner residual and always recovers the worse bull centre.

The likely mechanism is that `refine_edges` fits lines to the quad edges, so its corners lie more exactly on a projective quadrilateral, which is what the residual measures, while placing the ink edge slightly differently. That becomes a small scale bias in the fitted homography and only shows up out at the bulls. A 0.05 percent scale bias moves a bull 190 mm from the fit centre by 0.0037 in, which is the size of the gap.

**What to record in `docs/FIDUCIAL-DECISION.md` section 10, measurement 8.**

1. On this evidence OpenCV is the better primary for the metric that matters, by a small margin. Caveat it honestly: one estimator, one printer, one paper, and a scratch centroid rather than the bull locator this spike is building. **Re-run the comparison with your own locator and report whether the ranking holds.** If it flips, that is a finding and it goes in the report.
2. A detector must not be chosen by corner residual. This is independent evidence for the two-gate structure in DESIGN.md section 21, arrived at from the opposite direction to the argument that produced it.

**Also record the corner conventions, because they differ and both are traps.** Against a model ordered top-left, top-right, bottom-right, bottom-left, OpenCV needs its corner list rotated by two, which is the 180 degrees of FIDUCIAL-DECISION section 11, and libapriltag needs its winding reversed with no rotation.

**One observation worth a line, not a chase.** The rotated scan is worse in absolute terms than the same sheet unrotated, 0.00715 against 0.00418 worst, while still correlating with it. The field is paper-fixed as `docs/PHASE0-PRELIM.md` concluded, but that increase says a smaller scanner-fixed component sits on top of it. Note it and move on.

Carry on with the other five measurements.

---

## 2026-09-13, entry 1: this file exists, and so does its counterpart

**Status: actioned 2026-09-13.** Both files and the CONTRIBUTING.md section are committed with Phase 0 milestone M1, and the convention is in use from that commit.

Two files now carry the exchange between this session and the planning session, and `CONTRIBUTING.md` describes the convention so it survives into later phases.

- **This file** carries instructions in. Act on `open` entries, mark them `actioned`, never delete.
- **`docs/QUESTIONS-FOR-PLANNING.md`** carries questions out. When a genuine decision blocks you, append a dated section with `Status: open`, state the question, the options with their real costs, and what you would choose and why. Commit, push, and stop.

The reason for the change is that everything was previously moving through a human copying text between two windows. That is slow, it truncates, and it leaves the reasoning in a chat transcript rather than in the repository where the next contributor can find it.

**Keep asking questions.** The two you raised during Phase 0a were both worth the interruption and both changed the specification. This changes where a question is written, not whether to raise one. What it should reduce is questions that a measurement could settle, because the planning session can now run measurements against the committed scans and hand back numbers, as entry 2 shows.

Write every entry as though the reader has the repository but not the conversation, because that is exactly true in both directions.
