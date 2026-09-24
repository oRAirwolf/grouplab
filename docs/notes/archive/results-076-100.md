# Phase 1 results, entries 076 to 100

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

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

