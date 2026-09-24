# Phase 1 results, entries 101 to 125

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 101. The macOS gate record made portable, the warning's whole percentage, the canvas's rule, and the gallery named

`docs/NOTES-FROM-PLANNING.md` entry 101, in the order its covering message set: section 4, section 3, section 5, then the macOS gate record, which the entry says has been deferred twice. Sections 2 and 1 record rather than ask.

### Sections 4, 3 and 5

- **Section 4: the spacing warning says "about 27 percent".** The rate is rounded to a whole percentage, and under 1 percent it says so. A test refuses a decimal percentage in the sentence. The rate comes from a sigma estimated from one stated group, known to about a third either way.
- **Section 3: the rule is written where the canvas's lengths are.** `MarkingCanvas` states it above its constants: a target the person aims at is measured on the paper, and a minimum the person has to see is measured on the screen. It also says which constant is which. The hit radius is a whole reach only for the scale-reference handles, which have no size on paper, and a floor everywhere else.
- **Section 5: the README does not call the stage timeline done.** Its entry is in progress. It names what exists, a gallery of each stage's picture when the analysis finishes, and what does not, the live run `DESIGN.md` section 19 describes.

### The macOS gate record: four native steps moved to managed code

**Where it stood.** Entry 48 found the gated tables already identical on macOS, with the differences in measurement 1 and measurement 2. The rerun from Windows' corners (entry 49 section 2) placed them in three native steps:
- **the synthetic scan's perspective warp**, which made a different image before anything was detected;
- **sub-pixel corner refinement**, which moved corners on byte-identical images;
- **the homography's final refinement**, which moved the last printed digit on identical corners and inliers.

Turning OpenCV's optimised paths off changed nothing on macOS and broke a line on Linux, so the difference is in the native arithmetic itself rather than in a path that can be switched off.

**What changed.** `PortableImaging`, in `src/GroupLab.Core/Imaging/`, does each floating-point step in scalar managed arithmetic, in a fixed order, with no call into a platform's maths library. IEEE 754 rounds those operations identically on x64 and arm64.

| Step | Now | Kept native |
|---|---|---|
| Perspective warp | Inverse map, bilinear, border of paper, as `warpPerspective` | |
| Sub-pixel refinement | A port of `cornerSubPix`: single-precision window and weights, double-precision sums, the Gaussian weight's exponential as a series | Finding and decoding the candidates |
| The homography's final fit | A normalised linear estimate, then Levenberg-Marquardt to convergence, over the inliers RANSAC chose | RANSAC's choice of inliers |
| Contour refinement | The side lines fitted and crossed in double precision | Finding each marker's contour again: the same thresholds, contours and polygon test |

The native steps kept are integer work, and every gate record run has shown them identical on all three platforms.

**How each was checked.**
- **The first three, on all three platforms.** A gate record run on a branch carrying only them printed Linux identical to Windows in all eight tables. macOS differed in 3 lines, all of them contour rows of measurement 2.
- **Contour refinement, against the native step on Windows.** OpenCV does not return the contour a marker was found on, so the managed step finds it again the way the detector does and keeps the first candidate whose corners are the marker's. The check put OpenCV's own single-precision normal equations and elimination back into the managed fit. It then reproduced the native corners to within one single-precision step, 0.0005 px at coordinates near 5000 px, on all 136 corners of each of the eight sheet images at 600 and 300 DPI. **Every contour recovered is the one OpenCV used.** Without that emulation the managed fit differs from native by up to 0.09 px. That is how far OpenCV's single-precision solve sits from the least-squares line, and it is also where clang's fused multiply-adds on arm64 change its answer.
- **Unit tests.** `PortableImagingTests` has five: the series exponential against the library's, the warp's identity and a translation, a corner found between pixels, the contour fit's corners on a known square, and a known homography recovered with an outlier excluded.
- **The confirmation is this commit's gate record run** on all three platforms, against the regenerated Windows tables.

**No gate verdict changed.**
- **Paper gate:** ten of ten. Worst 0.00319 in on tile 3, where it was 0.00340 on the 96.2 percent sheet.
- **Print-scale detection:** 0.96200 at 600 DPI, where it was 0.96195, and 0.96201 at 300, unchanged.
- **Photograph gates:** flat fails three of three and mounted seven of seven, and not one bull figure moved.
- **Conformance test 43:** passes.

**What moved, and why.** The largest movements come from the final fit.
- **What OpenCV does.** `findHomography` refits linearly on RANSAC's inliers, runs at most ten Levenberg-Marquardt iterations, and then reclassifies the inliers.
- **What the managed fit does.** It iterates to convergence over the inliers returned.
- **The evidence it is the difference:** on the scans, the photographs, measurement 2's paper rows and measurements 3 and 4, the corner residual RMS is equal or lower in every row, as a least-squares fit carried to convergence should be. The rows with no refinement at all, where the corners are identical by construction, move too, so the fit alone moves figures.
- **The exception:** measurement 1's four-marker subsets, whose median residual rises on sheet 1 and falls on sheets 2 and 3. With four markers, the inliers OpenCV refits on and the ones it returns after reclassifying can differ, and the managed fit uses the second.
- **Corroboration:** M1.5's scan table already carried GroupLab's own converged plane fit beside the homography, and on the four scans where they disagreed it read 0.00260, 0.00267, 0.00319 and 0.00314 in. The managed homography now gives exactly those.

| Figure | Before | After |
|---|---|---|
| Worst edge-fit bull, `gl-cf25-ltr-96.2-600-dpi.png` | 0.00340 | 0.00260 |
| Homography alone, RMS, `main_flat3.jpg` | 0.01107 | 0.00945 |
| Measurement 1, sheet 1, four markers, worst bull at the 90th percentile | 0.07296 | 0.04192 |
| Measurement 2, 600 DPI, no refinement, worst bull | 0.00291 | 0.00279 |
| Measurement 2, 600 DPI, 1.5-module window, worst bull | **0.00517, over the gate** | **0.00290** |
| Measurement 2, synthetic 600 DPI, no refinement, corner error | 0.632 px | 0.641 px |
| Measurement 4, `gl-cf25-ltr-3-600`, libapriltag as returned, worst bull | 0.00245 | 0.00280 |

**One finding changed:** the 1.5-module refinement window no longer puts a sheet over the paper gate. Its corner error against truth is 2.326 px where it was 2.327, so the corners are as broken as before, and the bull breaks at 2 modules instead. The synthetic rows move because the warp builds a slightly different synthetic image. Measurement 4's conclusion holds with the half pixel removed, now within 0.00015 in rather than 0.00014.

**Elsewhere.**
- **The surface fits:** they iterate to their own convergence from the homography, so they move in the eighth significant digit. The exception is the joint general-surface fit, which M1.10 records does not converge: its worst sighter on `main1.jpg` goes from 0.07217 to 0.05150 in, and every verdict stays fail.
- **Stability:** three mounted frames give one or two more or fewer distinct outcomes in 200 orders, and no range moved.
- **Corpus counts:** 27 values move, on `telephoto3.jpg`, the excluded four-marker frame, which now gives 6 detections where it gave none, and on six punched 300 DPI runs. The ink-proximity rows follow `telephoto3.jpg`.
- **Detection time:** managed sub-pixel refinement adds 10 to 16 ms per 600 DPI sheet wherever markers are found.

**What was stale before this change, separated as entry 52 separated it.** Each record was regenerated from the previous commit's code before being compared with this change. `holes-synthetic.json` and `holes-synthetic-held-out.json` differ from their committed versions only there, and so do 18 of `detection-counts.json`'s values. All of them are entry 94's area-based oversize flag, which no one regenerated them after. Every other record was current, so everything above is this change.

**What was regenerated.** Every record a command writes, the eight committed Windows tables the gate record workflow compares against, and the documents that print them:
- `docs/PHASE0-RESULTS.md`: every table, and the prose that reads them. Its section 1 had quoted 0.00325 in since before entry 52 moved the 96.2 percent sheet to 0.00340.
- M1.10's joint-fit rows, and the stability table in "Entry 52 sections 3 and 4".
- **Left as measured, as entry 52 left them:** M1.5's tables, and `docs/FIDUCIAL-DECISION.md` measurement 8.
- **Not regenerated:** `mounted-pair.json`, whose command reads a directory no longer in the repository.

### Sections 2 and 1: recorded

- **Section 2:** the excess sits in the middle rows, which favours the sheet not lying flat over the printer. Alan is rescanning the friend's sheet with a weight on the lid. When the scan arrives, the comparison is the one entry 98 section 3 ran, the same split with the same control, so a drop in bulls 11 to 25 reads directly against the three unshot sheets.
- **Section 1:** a comparison between two halves can say they differ, not which one is abnormal. Naming the abnormal half needs a sheet where nothing is wrong. The control in entry 98 section 3 was that sheet.

**Tests:** Core 867 and App 62 passing, none skipped, the window rehearsal among them.

---

## Entry 102, and entry 101 section 5's remaining half. The experiment branches retired, and the live run

`docs/NOTES-FROM-PLANNING.md` entry 102 answers whether to keep three platforms and asks for no code. Its section 6 retires the two experiment branches after a check. The covering message then takes the half of the stage timeline entry 101 section 5 recorded as not built.

### Entry 102 section 6: nothing on the branches was missing from `main` or from the record

- **`experiment/opencv-unoptimized`:** one commit, three lines, turning OpenCV's optimised paths off. Its result, no change on macOS and a line broken on Linux, is in "Entry 101" and in `PortableImaging`'s own comment.
- **`experiment/portable-imaging`:** one commit with the warp, the sub-pixel port and the managed final fit. `main` carries all of it, with contour refinement added. The only lines on the branch that are not on `main` are comments describing three steps where there are now four, and a backend comment saying contour refinement was still native.
- **The record:** "Entry 101" carries the method, the four ported steps with what each keeps native, and the check that the contour port reproduces OpenCV's corners on all 136 corners of each of the eight sheet images once OpenCV's single-precision arithmetic is imitated.
- **Both branches are deleted** from the remote and locally.

### The live run: each stage's picture arrives with its record

**Before:** each stage's record already landed on the timeline as it filed, but its picture was read from the finished analysis. So the pictures appeared only when the analysis finished: a gallery at the end.

**Now the picture travels on the record.**
- **Which stages carry one:** the fiducial stage attaches the markers it matched, the registration its corners with their residuals and the fit, and the difference stage its residual.
- **When:** each is attached before the stage files, so the timeline's handler, which already moved to each stage as it landed, now draws its picture at that moment. The markers light up, the corners are ringed, and the photograph gives way to the residual while the analysis is still running.
- **Where they come from:** the timeline no longer reads the finished result at all.

**Batch pays nothing.**
- **The switch:** a trace keeps pictures only when asked, and only the marking screen asks.
- **What a stage does:** it passes its picture as a function the trace calls only when pictures are kept, so a batch run never builds one.
- **The residual:** as before, the only picture that costs an image, and kept only on the same switch.

**Tests.**
- **The new one:** runs the analysis with the window subscribed to the trace and never gives it the finished result. It reads the picture at the moment each stage files: markers at S2, corners at S3, residual at S5, and none at S9. Before this change it could not pass, because every picture came from the finished result.
- **The existing picture test:** now also requires that no record of a batch run carries a picture.

**The README** marks the timeline done: it has no gate of its own, and every part of it `DESIGN.md` section 19 [r3] names is built and tested.

**Tests:** Core 867 and App 63 passing, none skipped, the window rehearsal among them.

---

## Entry 103. The analysis screen split from the editor, the composite plot, two judgement cards, and questions 15 and 18

`docs/NOTES-FROM-PLANNING.md` entry 103, in the order it sets: sections 1, 2 and 5 are the work, section 4 is documents, and section 3 was to follow in its own commit. **Section 3 is held as question 19**: the sort it asks for was committed by entry 52, which answered question 15, so there was nothing to regenerate.

### Section 1: one destination in two states

**The split.** `MainWindow` keeps one document and shows it two ways, as `docs/figures/screens/assignment-editor.png` and `analysis-dark.png` do behind the rail's first icon.

| | Editor | Analysis |
|---|---|---|
| **Body** | the tool strip, the sheet, the review column, the timeline | the shots and the load on the left, the composite plot in the centre, the figures, the cards and the zero correction on the right |
| **Header actions** | review pill, Open image, Detect, Discard edits, **Accept and analyse** as the amber primary | registration pill, Show work, Export |
| **Pill** | "2 of 26 need review" | "registered, residual 0.001 in", or "scale set by hand" in amber |
| **Breadcrumb** | the document and its counts | GroupLab, then the sheet as a button back to the editor, then analysis |

- **Back is one click.** The sheet crumb returns to the editor, and a test holds the marking to the very state it left.
- **Discard edits asks first.** It stands beside Accept and analyse, and a click turns it into "Discard every edit since detection?" with Discard and Keep them. The existing keyboard test now keeps the edits once and then discards them.
- **Accepting with items open.** The analysis carries an amber line above the figures, for example "15 decisions were left unmade when this was accepted, and every figure here inherits them. The sheet crumb goes back to them." A test accepts a marking with its items open, reads the count, settles them and sees the line go, then opens one more and reads "1 decision".
- **Show work** returns to the editor with the stage timeline opened, because the timeline is where the work is shown. **Report** is not built: reporting is Phase 4's and not started.

**The composite plot** (`CompositePlot`).
- **What it draws:** one scoring bull's discs from the sheet's own definition, at true relative scale behind the shots, centred on the aim point. Every scoring shot sits at its offset from its own bull's aim point.
- **What it leaves out, by test:** sighter shots, and marks set to not a shot. Excluded shots stay, drawn hollow and dashed, and the legend counts them.
- **Shots:** circles at the calibre's diameter when one is set, points when it is not, and the legend says which: "25 shots, each drawn at the 0.308 in calibre", or "drawn as points: no calibre is set, so there is no hole size to draw".
- **The group:** its centre marked, CEP 50 and CEP 90 as dashed circles about it.
- **Extreme spread is the line between its two shots, not a circle.** A circle that size reads as a region the shots are contained in, and extreme spread is the distance between two particular shots. Clicking the line picks both shots, and the table and the status line name them. The concept draws a circle, so this departs from it on purpose. If the circle is wanted after all it goes back, and this paragraph is the reason on record either way.
- **Framing:** the view frames the shots with a margin and lets the rings run off the frame, so a small group is not a dot on a large ring.
- **Selection:** a shot clicked on the plot, or its row in the table, selects it on the sheet as the shot list does.
- **Without a definition**, for a marking done by hand, no bull is drawn and the legend says the sheet's definition is not known.

**The stack gains two rows after the existing three.** CEP 90, with CEP 50 and 95 beneath, is sigma times its circular multiple, the same model as sigma's interval. Group width by height is each axis's extent, with its standard deviation beneath. Mean radius still leads at the lead size. A test holds the order: mean radius, then CEP, then width by height.

### Section 2: the two judgements as cards

A card is the verdict in bold and then its evidence. The verdict never appears alone.

**The round card takes the circularity verdict and names the circularity test.**
- **Verdict:** "Round, as far as 25 shots can tell", or "Not round".
- **Its test:** the Bartlett-corrected likelihood ratio from 20 shots, and the simulation-calibrated likelihood ratio below that, with the p-value. That is `ShapeTests.Circularity`, and the card prints its method.
- **The error ellipse's reference line** moved out of More figures into this card as evidence, since it is what a reader checks the verdict against.
- **Stringing is a separate line labelled as such:** "Vertical stringing, a separate question, by Pitman-Morgan".
- **A negative stringing result carries its power.** Section 7's table gives 19 shots for 2 times, 50 for 1.5 and 155 for 1.25, at 80 percent power, and `ShapeTests.StringingPowerSentence` states what the count could have caught. At 25 shots it reads "From 25 shots this test catches stringing of 2 times or more at least 8 times in 10, and often misses less: 1.5 times needs about 50 shots and 1.25 times needs about 155 shots." The card adds "No evidence is not evidence of none".
- **The test asserts:** the round card's verdict and test lines never name Pitman-Morgan, and the stringing line starts with "Vertical stringing".

**The flyer card keeps its hedge, and now has a second verdict.**
- **When the worst shot is ordinary**, it says "Shot 14 is not a flyer.", then where the shot sits in mean radii, where a group of that size is expected to put its worst, and how often circular groups put theirs that far out or farther. It ends "So a shot there is not a flyer by that measure alone (STATISTICS.md section 10)."
- **What the old line did wrong:** it said "not a flyer by that measure alone" whatever the size of the worst shot.
- **When the worst shot is unusual**, where circular groups put their worst that far out less than one time in twenty, the card says the shot "is further out than a group of 25 usually puts its worst". It still does not call it a flyer: whether the shot was called or pulled is the shooter's to say.

### Section 5: the README's two false claims

- **macOS reproduces the record.** The Platforms row is yes. The paragraph now says that packaging, and nobody using either platform day to day, is what stands between Linux and macOS and a download. It also keeps the workflow's distinction: printed tables gated and identical, raw records compared and reported, and differences below printed precision not failures.
- **The rail exists.** The Concept screens paragraph now says what is built: the rail with one destination and four naming their phase, and the editor and analysis as two states behind it. It ends in one sentence, "Not built yet: the target library, session records, reporting, and load against load."
- **The guard.** `ReadmeTests` now reads that sentence and fails unless each item it names is a feature in the Planned section whose state is not Done. It needs only that the absent items are listed in one sentence beginning "Not built yet:", which the test's own message asks for when it is missing. It is cheap and it is not brittle, so it was built rather than skipped.

### Section 4: question 18's answers in the documents

- **"Assisted" is the snap.** `DESIGN.md` section 3's secondary-mode bullet says so in one sentence, and the README has a Phase 3 feature line for it, Done.
- **Blank-paper detection is Phase 4, Not started.** It is named in section 21's Phase 4 line and in the README's Phase 4 list. Both say that its gate needs one photograph at a known scale of plain paper with real holes, and that the corpus has none. Entry 103 asks for it to go "on the range list"; no such list exists in the repository, so the material is named in the gate and the feature line instead.
- **The designer's deferral carries two promises.** Both its entries, in section 3 and in the README's deferral, now say that a traced definition on a bought target is the same canvas, so both arrive together. Section 21's sweep records assisted placement as answered and no longer deferred, with options C and D refused and why.

**Tests:** Core 875 and App 69 passing, none skipped, the window rehearsal among them.

---

## Entry 104. Question 19 closed, the flyer card calibrated to what the screen measures, and the analysis state seen in dark

`docs/NOTES-FROM-PLANNING.md` entry 104, in its order.

### Section 1: question 19 closed, the option C risk written down, and a guard on status lines

- **Question 19 is answered**: nothing to commit, nothing to regenerate.
- **`DESIGN.md` section 22 carries the risk.** On a curved sheet, RANSAC's consensus moves the figures with inputs that should not matter. The sort made that repeatable, not stable: 15 to 69 distinct results in 200 orders on the mounted frames. Option C, a deterministic robust fit, is the answer, pointed at question 15 and not scheduled until the mounted gate has real frames.
- **The guard, `QuestionStatusTests`,** fails when a question marked open in `docs/QUESTIONS-FOR-PLANNING.md` is one a notes heading names as answered. **It was cheap because the headings are regular**: every one that answers questions says "question 15 answered" or "questions 12, 13 and 14 answered". It matches that phrase and nothing looser, and it would miss a heading that answered a question in other words. **Run over the two files as they stood at entry 102, it names question 15 and entry 52's heading**, which is the case it exists for.

### Section 2: the flyer card, calibrated to the screen's own statistic

**The measurement in the entry was right in direction and understated in size, because the screen does not use the mean radius it simulated.**
- **What the screen divides by:** the worst shot's distance from the group's centre, over the Rayleigh mean radius, `√(π/2)` times sigma estimated from the sum of squared radii with its c4 correction. That is `GroupFigures.MeanRadius`, the figure the screen prints.
- **What the entry divided by:** the arithmetic mean of the radii. Its lines reproduce exactly: 2.108, 2.477, 2.621 and 2.774 at 5, 10, 15 and 25 shots.
- **Why the screen's statistic is compressed further:** about its own centre, the worst of `n` shots is at most `√((n−1)/n)` of the root sum of squared radii. That is about 1.96 group mean radii at five shots.

| n | closed form's 5 percent line | the screen's true 5 percent line | closed-form p at that line | mean of the screen's statistic |
|---|---|---|---|---|
| 5 | 2.416 | **1.733** | 0.391 | 1.440 |
| 10 | 2.592 | 2.204 | 0.199 | 1.783 |
| 15 | 2.689 | 2.409 | 0.146 | 1.947 |
| 25 | 2.807 | 2.623 | 0.107 | 2.130 |

200,000 simulated circular groups a count, seed 7.

**At five shots the card could never flag anything**: its line was beyond the most extreme group five shots can make. At 25 shots it flagged at a true rate of 1.7 percent where it claimed 5.

**Decided: calibrate at every count, and withhold nowhere.**
- **What it does:** `Flyers.CalibrateWorst` simulates 9,999 circular groups of the same size and measures each exactly as the screen does. It returns the share at least as extreme, plus one over the resamples plus one, and their mean.
- **Cost and repeatability:** it is seeded, so the same group always reads the same, as the circularity test is below twenty shots. It costs a few milliseconds at the counts people shoot.
- **Why nothing is withheld:** the calibration is valid down to the dispersion minimum of five, so there is no count where withholding would be the honest answer.
- **The card now reads:** "It sits at W of the group's own mean radii from its centre. Circular groups of N, measured the same way, put their worst at E on average, and this far out or farther …, from 9999 simulated groups." It still ends with "by that measure alone".
- **Tests.** `WorstShotCalibrationTests` holds four things:
  - the calibration's statistic is the one the report prints;
  - it puts one group in twenty past the independent simulation's lines at 5 and 25 shots;
  - five shots cannot reach the closed form's line;
  - the same group always reads the same.

**`docs/STATISTICS.md` section 10 no longer leaves the mean radius ambiguous.**
- **The table:** it says its MR is the population value, `σ√(π/2)`, with radii from the true centre.
- **Beside it:** a second table and paragraph say what the screen judges by and why.
- **The entry's own figures** are named as a third statistic the screen does not quote.
- **The dialog sentence stands:** calling anything past twice the true mean radius a flyer discards an ordinary shot two times in three at 25 shots.

### Section 3: the render in both themes, and the zero block

- **Themes.** The test now sets dark and then light and captures each, named after the theme it set, as `ScreenshotTests` does. It restores the theme afterwards.
- **The zero block had survived the split, and I had moved it.** Entry 92 put it above the group statistics. The split put it below the cards and the size flags, off the bottom of the column, which is why the render showed no zero block. It is back above the figures.
- **The fixture now carries a 100 yd distance and a rifle with quarter-MOA clicks.** The block shows the offset in inches and MOA, its uncertainty, the refusal with "about 90 shots would settle it", and the clicks line.
- **A test** reads the clicks line and asserts the ZERO CORRECTION heading comes before GROUP in the analysis column.

### Section 4: the four observations

- **Shot order.** The table is sorted by shot number, not detection order.
- **The bull recedes: a judgement.** Fading the whole bull turned the paper a dull grey on dark chrome, which lost the concept's paper-on-dark contrast. So only the inked discs are faded, to 35 percent over the paper's own colour, and the paper stays at full strength. The shots now lead, and the sheet still reads as paper.
- **The calibre.** The load panel reads ".308, set after detection" when detection ran without one, so it no longer contradicts the status line.
- **Size flags.** They sit behind one disclosure that counts them, "15 marks flagged as possibly two holes", so the cards stay in view however many there are.

**Seen in the dark render, not changed.** At the lead size, mean radius with its MOA figure wraps onto a second line in the 372 px column. That column width is entry 42's, and the wrap is entry 24's rule against clipping, so it is left for a decision rather than altered here.

**Git.** The three lock files planning's inbox commit left, `HEAD.lock`, `index.lock` and `objects/maintenance.lock`, were removed at the start, with no git process running.

**Tests:** Core 881 and App 69 passing, none skipped, the window rehearsal among them.

---

## Entry 105. The columns fit and resize, the figure panel set by its own rules, the plot keyed and described, the mark, the work bar on a toggle, calibre names read as bullets, and sighters set aside

`docs/NOTES-FROM-PLANNING.md` entry 105, all eight items. One conflict with the code is question 20. The stale `index.lock` the planning session's shell left was removed at the start, with no git process running.

### Item 1: the column's own overflow first, then splitters

- **The defect.** The new-record form put a 150 px name field, a 110 px click list and a button on one row, wider than the 372 px column, and the button was clipped to "Add rif" on a first run. Each field now has the column's width with its buttons beneath it.
- **Every row wraps.** `Row()` is a wrapping panel now, so no row in either column can run past its edge whatever it holds.
- **The test:** it expands the form at the default width and holds every button's right edge inside the column, which it failed before.
- **Splitters.** Both states' bodies are grids now. The editor has a splitter between the sheet and the review column; the analysis state has one on each side of the plot.
  - Each splitter shows the resize cursor.
  - Each side column has a minimum of 260 px and a maximum of 760, and the centre keeps 320.
  - Each column opens at the width a person last dragged it to, remembered in `AppSettingsStore` beside the other settings.

### Item 2: the figure panel set by the rules the project already wrote

No finding is reworded; only type, alignment and grouping changed.
- **Sentences in the UI sans.** `Note()` sets a sentence in the sans at the secondary size, and `Detail()` keeps the mono for readouts only. The zero block's uncertainty, its degrees-of-freedom note and its instruction about the rifle are notes now. So is the CEP row's "from sigma under the circular normal model".
- **One shape for all five figure rows.** Label on the left, value alone on the right, and a mono line beneath carrying the angular value first and then the interval. The lead figures no longer wrap their unit onto a line below the label.
- **The zero block by kind.**
  - The two readouts are cells in columns, linear under linear, angular under angular, direction under direction.
  - The uncertainty is a note beneath them.
  - The verdict is at body size and full strength, no longer dimmed.
  - The two notes after it are quieter than the verdict.
- **Headings.** Section headings are drawn in the dim colour rather than faint, and in the figure column each has a hairline above it.

### Item 3: the plot says what the cursor is over, and keys its marks

- **Every shot carries its bull,** in the plot and in the offset table. Shots are already named by their bull's number (entry 75), so the column mostly repeats the label; it tells a doubled bull's "14a" or a shot with no bull apart.
- **`CompositePlot.Describe(Point)`** resolves in the order `Pick` does and then goes further:
  - **a shot:** "Shot 3, bull 3. 0.070 in left and 0.036 in low of its bull's aim point, 0.103 in from the group centre.", plus "Excluded" when it is;
  - **the extreme spread's line:** a distance between two shots, "not a region the group sits inside";
  - **the group centre;**
  - **the CEP circles near their stroke,** each by what it means, "half the time" and "nine times in ten";
  - **the aim point, then the bull's rings,** and nothing on empty background.
  - The tooltip updates as the pointer moves.
- **One look for every shot.** The translucent fill was what made a shot pink over the paper and maroon over the dark. Shots are now rings only, at one weight, with the halo the marking canvas uses.
- **A key, not a caption.** It is a panel at the plot's top left with each mark drawn as it is on the plot, then its name. CEP 50 is dotted and CEP 90 dashed, so the key can tell them apart.

### Item 4: the mark

- **The files.** The four SVGs are committed in `src/GroupLab.App/Assets/` under the names the entry gives, as delivered, including their content-credentials metadata.
- **`BrandMark`** reads a committed file and draws it: circles and filled paths, the only elements the files use, with no new package.
  - The header carries the lockup at 26 pixels, in place of the plain title, and the crumbs continue after it without repeating the name.
  - The rail's top slot carries the mark.
  - The dark file is drawn in the dark and high-contrast themes and the light file in the light theme.
- **Question 20:** the entry says the colours are tokens, and they are not. The light theme's amber token is `#965d12`, not the file's `#a9660f`, and no token holds any of the four greys. Until that is decided, the mark is drawn in the file's own colours, exactly as chosen.

### Item 5: the executable's icon

- **The source.** `grouplab icons <mark.svg> <directory>` draws the committed mark at each size, eight times over and averaged down to that size, so each size is drawn at its size rather than shrunk from the largest. It writes:
  - `grouplab.ico` at 16, 24, 32, 48, 64 and 256 px;
  - a Linux PNG set from 16 to 512 px;
  - `grouplab.icns` from 16 to 1024 px.
- **The build.** `ApplicationIcon` is set in the csproj and the window sets its own `Icon`.
- **The test** holds the files present and the icon file's six sizes.

### Item 6: the work bar on a toggle

- **Show work** is a toggle in both states' headers now. It shows and hides the bar in place, the two toggles always agree, and the choice is remembered.
- **Defaulted closed.** Alan asked for the strip off the screen, and nothing depends on it being open:
  - the stage pictures still play on the sheet as each stage lands;
  - a failure is still a prominent error in the panel and the status line, as `DESIGN.md` section 19 requires;
  - Show work itself turns red and reads "Show work: 1 stage failed", or amber for a degraded stage, whenever the bar is hidden and a stage did not end well.
- **The test** runs a blank page, whose registration fails, with the bar hidden. It holds the "Detection failed" error effectively visible, and Show work saying a stage failed.

### Item 7: calibre names read as the bullets they fire

- **`Calibre.Table`** is Alan's 44 rows, 42 distinct pairs, and the pick list shows every pair as its name with its diameter, "35 Cal. .357". The twelve cartridge names the list carried before resolve as they did.
- **`Calibre.Read`** resolves in this order:
  1. **A typed diameter wins,** a decimal below one or a number marked in inches, and reads exactly as before.
  2. **A pick-list entry** is itself.
  3. **A name is matched tolerantly:** case, a space before "mm", "Cal" with or without its stop, a leading point.
  4. **Then the name's leading number on its own,** so "6.5 Creedmoor" is the 6.5mm row and "30-06" is 30 Cal.
  5. **Then the old leading-number rule** as the fallback, where a wildcat still lands.
- **Ambiguous names never pick one.** A name fired as more than one diameter returns its candidates. The window shows them as buttons and the command line lists them. The nine are 30, 303, 32, 35, 38, 45, 50, 9mm and 7.62.
- **Tests:** every one of the 44 rows by name and by diameter, each ambiguous name returning its candidates in the forms a shooter types, and a chosen candidate reading as itself.
- **Readings the old tests pinned and this item called wrong:**

| Typed | Old reading | Now |
|---|---|---|
| "6.5 Creedmoor" | 0.256 | 0.264 |
| "22" | 0.220 | 0.224 |
| "17 HMR" | 0.170 | 0.172 |
| "9mm" | 0.354 | candidates .355 and .356 |
| "7.62 mm" | 0.300 | candidates .308 and .310 |
| "30-06" | 0.300 | candidates .308 and .309 |

  "300 Win Mag" still reads 0.300, because 300 is a leading number and not a name in the table, and it says what it read.

### Item 8: sighters set aside unless analysed

- **What ignoring does not touch.** Detection and one-to-one matching run over the sighter bulls exactly as before.
- **`ReviewQueue.For(state, analyseSighters)`**, off by default, raises nothing that concerns only sighter bulls: no size flag on a sighter's mark and no contest between two sighters.
- **A contest between a sighter bull and a scoring bull is always raised.** The test checks both ways round, a sighter's hole nearest a scoring bull and a scoring shot nearest a sighter, in both modes.
- **In the window.**
  - Counts are of scoring shots.
  - Sighter marks are drawn faint, dashed and unlabelled, and can still be selected.
  - "Analyse sighters", remembered, appears only on a sheet that has sighter bulls.
- **Analysed,** the sighters are a group of their own on a view of the marking that holds only them. Their section shows their centre from aim and their own zero readout, and they are never pooled with the scoring shots.
- **The command line** follows the same default: `analyze` sets sighters aside and says how many, and `--sighters` prints them and their own group.
- **The decision is in `DESIGN.md`** sections 13 and 14.

**Tests:** Core 987 and App 77 passing, none skipped, the window rehearsal among them.

---

## Entry 106. Nothing prints silently, the mark's colours are tokens, the .300s read as .308, the calibre list generated, and printing from inside GroupLab scoped

`docs/NOTES-FROM-PLANNING.md` entry 106, every numbered section. Section 5 is raised as question 21 rather than built.

### Section 1: entry 105 section 9, built, and its status line corrected

**How it was missed.** Section 9 was added to entry 105's inbox file after the file was read, so the work and the fold covered the eight sections that were there, and the status line counted eight. The log now says section 9 was not actioned with the rest and was actioned here.

**The print screen no longer asks any program to print.**
- **No print verb on any platform.** Windows' "print" verb ran whatever the default PDF program registered, and on Alan's machine that printed the sheet straight to the printer at the program's own scaling, with no dialog, while the screen said a dialog would follow. The button now opens the PDF in the viewer, `UseShellExecute` with no verb, on every platform.
- **Named for what it does:** "Open to print", not "Print…".
- **A confirmation, not only a status line,** because Alan missed the status line: a dialog reading "The target is open in your PDF viewer. Print it from there, choosing Actual size or 100 percent, never Fit." GroupLab only opened a file, and the dialog says no more than that.
- **Logged when it works:** the launch writes `print.open` with the file's name and hash, no verb, and that it returned. A silent success is no longer invisible in the log.
- **Tests:** the launch carries no verb and its words promise no dialog, and the button is named "Open to print".

**The guard.** `NotesStatusTests` checks an actioned status line that says "all N items" or "all N sections" against the entry's numbered sections. It failed on entry 105's old line, nine sections and "eight", and passes on the corrected one. It checks only that claim, because status lines are otherwise free prose. The fix that matters is a covering command that no longer counts sections.

### Section 2: question 20 answered, option B

- **The palettes gain three brand roles,** `MarkRing`, `MarkAmber` and `MarkWord`, at the committed files' values. High contrast takes the dark values.
- **`BrandMark` draws from them.** Each colour in a file is the role it matches in that file's palette, drawn in that role's colour for the theme showing.
- **`ThemeTests` holds files and roles to each other.** Every colour in a mark file is one of its palette's roles, and the lockups use all three.
- **Held to 3:1 for a graphic, computed, against the panel and the window background in every theme.** The tightest is the light rings on the light background at 3.06:1:

| Theme | Rings | Amber | GROUP |
|---|---|---|---|
| Dark | 3.51 / 3.79 | 6.71 / 7.25 | 5.35 / 5.78 |
| Light | 3.39 / 3.06 | 4.57 / 4.12 | 5.30 / 4.78 |
| High contrast | 4.07 / 4.32 | 7.79 / 8.26 | 6.22 / 6.59 |

Each cell is on the panel, then on the window background.

- **Light `Amber`, tuned for text, stays `#965d12`.** The mark's amber, `#a9660f`, differs from it on purpose.

### Section 3: the cartridges that fell to the leading-number guess

Every diameter the entry lists was checked against the standard bullet diameters and is right:
- **.308:** every .300 in common use (Blackout, AAC, Win Mag, WSM, PRC, Norma, Weatherby, H&H, RUM, Savage); 30-06, 30-30 and 30-40 Krag; and 7.62x51.
- **.284:** the 280s, 28 Nosler and 7mm Rem Mag.
- **.264:** 26 Nosler.
- **.510:** 50 BMG.
- **.458:** 45-70.
- **.310:** 7.62x39, the value Alan's table gives 7.62mm.

**How the names reach them.**
- **Any name beginning "300" reaches .308** by its leading number, "300 Weatherby Magnum" included.
- **The bare "30", "7.62" and "50" still ask.** The hyphen or the case length is what makes a cartridge of them.
- **"300 Blackout" reads .308,** where it read 0.300 on the very sheet the analysis screen was built against. Entry 105's test had pinned "300 Win Mag" at 0.300 as a known limit, and it is .308 now.

**Tests:** every name above reads as its diameter, and the three bare names return candidates.

### Section 4: the whole list, generated from the code

- **`grouplab calibres`** prints the list and writes `docs/CALIBRES.md` and `docs/CALIBRES.pdf`, from `Calibre.TableRows`, the cartridge names and their keys, and the ambiguous keys as `Calibre.Read` resolves them.
- **The document's order:** the table rows with inches, millimetres and which of Alan's lists each came from; every cartridge name with the short names that reach it; every ambiguous name with its candidates; and one paragraph on how typed text is read.
- **The PDF is drawn from the Markdown** by the renderer's own PDF writer, in Helvetica on Letter pages, with the widest table column wrapped where a table is wider than the page. The repository's other PDFs were printed from Chromium by hand, with no script behind them, so this follows the `grouplab icons` pattern instead: a command in the repository, and nothing installed.
- **`CalibreListTests` holds three things:**
  - the committed Markdown equals what the code produces;
  - every table row, cartridge and ambiguous name is in it;
  - every line and rule of the PDF lies inside its page's margins.

### Section 5: printing from inside GroupLab, scoped and raised as question 21

**The plan.**
- **The dialog and the drawing:** Win32 `PrintDlgEx` and GDI through P/Invoke, drawing the renderer's scene as vector in half-dmm units.
- **The page:** placed on true page coordinates from the printer's physical offsets.
- **Refusals:** a sheet on the wrong paper size, and any marker, code or bull in the unprintable margin, each refused with the reason.
- **The confirmation:** the printer, the sheet, the page count and "at actual size", saying the job reached the queue and nothing more.

**Why question 21 rather than building it now:**
- It is about one full run, which this one could not also hold.
- Three decisions are open: whether the margin rule reaches a marker's quiet zone, Linux and macOS, and whether it replaces Open to print or sits beside it.
- The test that checks the printed size needs "Microsoft Print to PDF" on the CI runner, which I cannot confirm from here.

**Meanwhile** Open to print is the path, and the README lists in-app printing as not started.

**Tests:** Core 1016 and App 80 passing, none skipped, the window rehearsal among them.

---

## Entry 107. The calibre is a diameter and nothing else, and printing from inside GroupLab on Windows

`docs/NOTES-FROM-PLANNING.md` entry 107, both numbered sections, section 1 first. Two conflicts in section 1 are raised as question 22 and built to the section's written rule meanwhile.

### Section 1: the calibre input takes a diameter and no names

**Recorded as Alan's decision** in `Calibre`'s own comment and in DESIGN.md, so nobody restores the names thinking they were lost. It reverses entry 105 section 7 and entry 106 section 3.

**What the box reads.**
- **Inches:** a decimal below one, with or without its leading zero, or any number marked `in` or `"`. ".308", "0.308", "0.308 in" and "1 in" all read. So does the pick list's own form, ".308 in (7.82 mm)", so choosing from the list and editing it both work.
- **Millimetres, only when marked:** "7.82 mm" and "7.82mm".
- **Everything else is refused with one sentence:** "Enter the bullet diameter in inches, such as 0.308, or in millimetres with mm, such as 7.82 mm." That covers every name and every bare number of one or more: "300 Blackout", "6.5 Creedmoor", "30 Cal.", "9mm Luger", "7.62", "6.5", "308" and "22". The old millimetre, hundredths and thousandths guesses are gone.
- **The range stays** 0.1 to 1 in however the diameter was entered, and says so before the same sentence.

**Shown the same way everywhere,** the diameter in both units: the pick list, the box after a choice, and the analysis screen's LOAD panel, which read "Calibre .308, 7.62 mm, set after detection" and now reads ".308 in (7.82 mm), set after detection".

**The pick list is the 37 distinct diameters** in Alan's rifle and pistol lists, .172 to .510, smallest first, with no names. .223 has left it and can still be typed.

**Saved markings.** A marking that stored a name keeps its diameter and loads showing ".308 in (7.82 mm)", with the old name not displayed.

**Removed:** the name table, the cartridge list, the key matching, the candidates, the ambiguity sentence, and the tests that pinned them.

**`grouplab calibres` and its drift test stay.** `docs/CALIBRES.md` and `docs/CALIBRES.pdf` are now three parts: how to enter a calibre, with the refusal sentence; the 37 diameters in inches and millimetres; and why there are no names. The PDF is one page.

**Tests:**
- every pick-list diameter reads exactly in every form, in both units;
- every name and bare number above is refused with the sentence;
- "7.82 mm" and "0.308" read correctly;
- a marking saved under ".308, 7.62 mm" loads as 0.308 in, shown ".308 in (7.82 mm)";
- the committed list equals the generated one.

**Question 22, two conflicts, blocking nothing.**
- **"9mm" is in the section's list of names to refuse, and the section's own rule reads it.** It is a number marked mm, so it reads as 0.354 in. No syntax tells "9mm" the name from "9mm" the diameter. "7.62mm" reads 0.300 in the same way, which is the trap the section removes for bare numbers. The rule is built as written, a test pins "9mm" at 0.354 in with a pointer to the question, and the question offers refusing the metric designations as the fix.
- **The section counts 36 diameters, and Alan's lists hold 37.** All 37 are in the pick list.

### Section 2: printing from inside GroupLab, on Windows

Question 21's plan, built with entry 107's three answers.

**On the print screen, on Windows,** the row is "Print…", the amber primary, then "Open to print", then "Save PDF…". Print opens the real Windows print dialog, `PrintDlgEx`, owned by the window:
- the sheet's paper is chosen in advance when it is Letter, A4, Legal, Tabloid or A3;
- copies go through the driver;
- a tiled set offers page ranges.

Linux and macOS show Open to print and Save PDF only, as before.

**How it draws.** GDI through P/Invoke, with no package added.
- **The scene is drawn as vector,** the same scene the PDF writer draws, in half-dmm through GDI's anisotropic mapping: 508 units to the printer's dots per inch, so one unit of the definition is one unit on the paper.
- **The origin is shifted by `PHYSICALOFFSETX` and `PHYSICALOFFSETY`,** because GDI's origin is the printable area's corner, not the paper's.
- **What is drawn:** rectangles for markers, codes and rules; each bull band as two ellipses filled even-odd; text in Arial, the metric match for Helvetica, on its baseline with the same anchor.

**Refused before anything is sent, with the reason, and never scaled.**
- **The paper.** A paper that is not the sheet's page names both, for example "Test Printer is set to A4 (210 x 297 mm) paper, and this sheet is Letter (215.9 x 279.4 mm)", and says to choose that paper or print the PDF on a printer that takes it.
- **The margin.** Any inked item that falls in the printer's unprintable margin, wholly or partly: markers, codes, bull artwork, rules and text. The refusal names each edge, the margin there and what reaches into it, and on a tiled set, which sheet.

**The quiet zone is not counted, as entry 107 decides, and I know nothing its reason misses.**
- **The margin leaves bare paper and the quiet zone is bare paper,** so a quiet zone in the margin prints exactly as intended.
- **Printers that smudge or smear near the edge** do so where they print, and the driver's margin is what keeps ink out of that band. An item that would be smudged is inked, so it is refused on its own account.
- **A quiet zone meeting the paper's edge** is a matter of the sheet's layout, not the printer. The layout rules and the validator's edge check govern that, whatever the printer does.

**The confirmation, after `EndDoc` returns, as a dialog and in the status line.** For example: "GroupLab 5x5 Load Development, Letter, 1 page at actual size, was sent to the print queue of Brother MFC-J430W Printer." It says nothing about paper, because GroupLab cannot see it.
- **A cancel** says nothing was sent.
- **A refusal or a failure** is an alert and a dialog titled "Not printed".
- **Every print is logged:** `dialog.open` for the dialog, and `print.send` with the sheet file's name, the outcome and the page count. The log holds no paths.

**Where the code lives.**
- **`PrintFit`, in Core,** holds everything decided: the mapping, the paper and margin refusals and the confirmation's words. It is plain functions over a scene and a described printer.
- **`WindowsPrinter`, in the CLI project,** holds only the dialog, the driver queries and the drawing. It sits there because the application and the Core tests already reference that project.

**Tests.**
- **On every platform, with no printer:**
  - the offset shift, including unequal resolutions;
  - the paper refusal, Letter on A4 and Letter landscape;
  - the margin refusal for a marker, a disc crossing the bottom, text on the right, a code and a rule;
  - a marker whose quiet zone alone falls in the margin, not refused;
  - an item exactly on the printable edge, not refused;
  - a tiled refusal naming its sheet;
  - every built-in sheet printing where the whole paper is printable;
  - the confirmation's wording.
- **The printed size, on Windows with "Microsoft Print to PDF".** GL-CF25-LTR is printed through the same GDI path with no dialog, and the output is rasterised at 600 dpi, where its page is 5100 by 6599 pixels: the driver's page box rounds to one pixel short of Letter's 6600, 0.04 mm. Its markers are then detected and located on the page with no fitting. **All 38 markers lie within 0.1 mm of their definition coordinates; the worst is 0.015 mm** on this machine. Where the printer is absent the test is skipped, with that reason named.
- **App:** on Windows "Print…" is the primary and first in the row, with Open to print beside it and not primary; elsewhere there is no Print button.

**Alan's own printer, described and not printed to.** The Brother MFC-J430W reports 600 dpi and a 2.96 mm margin on every edge on Letter and A4.
- **Every built-in sheet on those papers, and on A3 and Tabloid, passes the margin check.**
- **The three roll sheets are refused on paper,** because neither printer takes their sizes: GL-LR300-R24 at 24 by 28 in, R36 and R42.

**The CI runner.** The Windows job lists the installed printers into the run summary before it builds, and says whether "Microsoft Print to PDF" is there, so the printed-size test either runs or is skipped with its reason. **The first run answered it: `windows-latest` has one printer, "Microsoft Print to PDF", so the printed-size test runs and passes on CI**, Core 905 with none skipped. Linux and macOS skip that one test with its reason, Core 904 and one skipped, and App 80 on all three.

**The check by hand, on paper.** The size test covers the PDF printer and runs on this machine. The check that matters on paper is:
1. In GroupLab, open the print screen, choose "GroupLab 5x5 Load Development, Letter" (GL-CF25-LTR), and click **Print…**.
2. Choose the Brother, leave the paper at Letter, and click **Print**. The confirmation names the printer and says one page at actual size.
3. Scan the printed sheet flat at 600 dpi to a PNG or TIFF, for example `C:\Users\<you>\Documents\print-check.png`.
4. Run `grouplab measure C:\Users\<you>\Documents\print-check.png targets\GL-CF25-LTR.gltd.json -v 3` from the repository. Add `--dpi 600` if the scanner did not record its resolution.
5. **Read stage S4.** It says "printed at N% of intended size". Within 0.05 percent of 100 is actual size. The bull table below it gives each bull's error from its definition.

`grouplab measure` reads images, not PDFs, which is why the check on the PDF printer is the automated test rather than this command.

**Tests:** Core 905 and App 80 passing, none skipped on this machine, the printed-size test among them.

## Entry 108. Question 22 answered: calibre designations refused in both units, and the count is 37

`docs/NOTES-FROM-PLANNING.md` entry 108, both numbered sections.

### Section 1: the count

**37, not 36:** entry 107 left out .356. The only place 36 stood as the pick list's count was entry 107's own text in the notes log, and it now reads 37 with a note that entry 108 corrected it. DESIGN.md, the README and `docs/CALIBRES.md` never carried it. The "36 of Alan's 44 names read wrong" in `Calibre`'s comment is a different fact and stays.

### Section 2: designations refused, in inches as well as millimetres

**The lists, in `Calibre`,** numbers that only refuse and are never read:
- **Inches:** .17, .20, .22, .25, .27, .28, .30, .303, .32, .35, .38, .44 and .45.
- **Millimetres:** 5.45, 5.56, 6, 6.5, 6.8, 7, 7.5, 7.62, 7.65, 8, 9 and 10.

**How a value is compared.** It is compared as a decimal, at the value typed, so ".270", "0.270" and ".27" are one value and all refused. "7.620 mm" and "7.62mm" are refused the same way.

**Left off on purpose,** because each is a real diameter: .40, .41 and .50 in, .308, .338, .375 and .416, and 9.3 and 12.7 mm.

**The refusal names the problem and guesses no bullet.** Two examples:
- "7.62 mm is a calibre's name, not the bullet's diameter. Enter the bullet's diameter, such as 7.82 mm or 0.308."
- ".38 in is a calibre's name, not the bullet's diameter. Enter the bullet's diameter, such as 7.82 mm or 0.308."

The value is shown as typed, with its unit.

**Checked before building, and held by a test.** None of the 25 refused values equals any of the 37 pick-list diameters: in inches exactly, and in millimetres to the hundredth, the pick list's own display. None of the values the entry says to keep is on either list. `NoRefusedValueIsAPickListDiameter` fails the day a diameter is added that collides with a designation, and every diameter still reads in three forms: its displayed form, its inches and its millimetres.

**Tests:**
- every refused value in both lists is refused with the sentence, in its short, long, zero-led and unit-marked forms, "9mm", "7.62mm", ".38" and ".270" among them;
- ".357", ".452", "0.308", "7.82 mm", "9.3 mm" and "12.7 mm" read, and so do .40, .41, .50, .338, .375 and .416;
- the lists never meet the pick list.

The test that pinned "9mm" as 0.354 in is gone.

**`docs/CALIBRES.md` and `.pdf`** now list both sets of refused values and the example refusal, generated from the lists themselves, and the drift test holds them. DESIGN.md's decision paragraph and the README's Phase 3 line say designations are refused.

**Tests:** Core 918 and App 80 passing, none skipped.

## Entry 109. A layout and readability pass on both screens, measured against the concept

`docs/NOTES-FROM-PLANNING.md` entry 109, every numbered section, in the order it asks: the marking screen and the new settings screen, then the analysis screen, in one run. No figure, interval, verdict or refusal changed its meaning. The explanations moved, as they stood, behind a "why" on the item they explain. Three statements in the entry that the code does not bear out, and two places where its limits meet, are question 23.

### Section 1: the principles

- **"Why".** A disclosure on each item it explains, labelled "why" with a chevron beside it.
  - It is closed by default and remembers whether it was opened, one flag per item in the settings file, as More figures does.
  - It holds the sentences that were on screen, moved as they stand.
  - The only visible sentence shortened is the zero correction's verdict. Its two remaining clauses, the smallest callable offset and "Shoot more before touching the turret", are the first line behind its "why".

**Five text styles:**

| Style | Size |
|---|---|
| Title | 16 |
| Heading | 14 semibold, in the text colour and sentence case, not dim capitals |
| Label | 13 |
| Value | 18 mono |
| Detail | 11.5 |

Mean radius keeps its 29 point lead, the one named exception. The older size names now each equal one of the five, so nothing uses a size of its own. **`EveryTextOnTheScreensIsOneOfTheFiveStyles`** walks every text on the marking screen, the analysis with every "why" open, and the settings, and fails on any size off the scale.

- **One row shape.** Every figure is a label left in the label style, a value right in mono, one detail line beneath, and a hairline under the row: shots, centre from aim, mean radius, sigma, extreme spread, CEP 90, and width by height.
- **The spacing grid.** A base of 4. The scale's 6 and 14 are gone, and paddings are multiples of 4. A control's margin stays 2, so two side by side sit 4 apart.
- **Sections are divided by a rule and space.** The judgement cards lost their boxes. The review card keeps its amber box, as the concept draws it.

### Section 2: the marking screen

- **The tool strip is one row.**
  - The six tools are icons alone, each named with its key in a tooltip, "Pan (P)" and so on, the active one lit amber.
  - Undo and Redo follow as icons.
  - On the right are the review's keys as keycaps: Space next item, Enter first choice, N not a shot, Ctrl Z undo.
  - The icons are drawn as geometry, so none depends on a font having the glyph.
- **The view controls** float over the canvas's bottom right corner: Zoom in, Zoom out, Fit, Rotate left and Rotate right.
- **The header** is the review pill, Detect on a GroupLab sheet, Show work, Discard edits, Accept and analyse, and one menu. The menu holds Open image, Open marking, Export and Report a problem.
  - It fits on one line at 1400 pixels.
  - Before an image is open, the canvas says "Open a photograph or scan of a target" with an Open image button, since opening is now in the menu.
- **The rail.** Print opens the print screen. The gear at the foot opens **Settings**:
  - units on three axes;
  - the theme;
  - detailed logging and where the log is;
  - Report a problem;
  - the crash records not yet dealt with.

  The marking panel still offers a pending crash in its banner, because a notice belongs where the person is.
- **The panel** is Review, Selected shot and Scale, then the group's inputs and the shots. It no longer holds the units, the theme or the log.
- **The scale is one line.** For example "Scale from the printed markers, 38 of 38", with a teal tick when every marker was found and an amber mark otherwise. Where it came from sits behind its "why", and the stages are in Show work. The sheet reference now carries the marker counts to make this possible.
- **The summary's text defect is fixed in Core.** It reads "assigned by one-to-one matching", with no colon when the reason is empty. The stage record's line says the method in words too.
- **The status line is one line**, trimmed, with the whole text in its tooltip. After detection it reads, for example, "Detected 24 holes on 25 bulls and 3 sighters."

### Section 3: the analysis screen

- **The unmade decisions** are an amber banner at the top, "1 decision left unmade. Review them", the second half a link back to the editor. Entry 103's sentence is behind its "why".
- **Zero correction.** The two readouts, the uncertainty as one detail line, and the verdict in one line, such as "Not distinguishable from zero at 24 shots. Nothing this rifle can shoot would settle an offset this small." The smallest callable offset, the degrees of freedom and the clicks or solver note are behind "why".
- **The judgement cards.** The bold verdict, then the test and its p value, then what must stay in view:
  - **Round:** "Circularity test, Bartlett-corrected likelihood ratio, chi-square on 2 degrees of freedom: p = 0.549". It still names the test, as STATISTICS.md section 7 requires. Its reading and the error ellipse are behind "why".
  - **The stringing line** stays in view with its power statement, whole. At 372 pixels it wraps to several lines, because shortening it would reword it. That is question 23.
  - **Flyer:** "Worst shot against N simulated circular groups: p = ...", with the hedge "by that measure alone" in view. Where the shot sits is behind "why".
- **The composite plot.**
  - Each shot is a solid dot at its centre, with its calibre outline thin and faint behind it.
  - A "Calibre outlines" toggle over the plot's corner hides the outlines, and the key says so.
  - The frame's margin went from 35 to 10 percent of the group's extent, so the group fills the plot and the rings run off it.
  - The inked rings are drawn at 18 percent over the paper, where they were at 35.
- **The shot table.**
  - Plain rows with every other one shaded, no border round each.
  - One number per shot. A bull column appears only when some shot sits on a bull other than its name, and then shows only those bulls.
  - The numbers are right-aligned, so their decimal points line up.
  - An excluded row is struck through and dim.
- **LOAD** is unchanged.
- **The crumb** names the image's file, as it already did, or the sheet's name when no image is recorded. The render entry 109 was read from had applied a detection without opening a file. That is question 23 too.

### Section 4: the renders

**`EveryScreenIsPhotographedInBothThemesAtBothSizes`** renders a GL-CF25-LTR scan with a hole on every bull, opened and detected by the real pipeline. It writes 20 frames: the marking screen, the analysis with its disclosures closed and open, the settings, and the print screen, in dark and light, at 1400 by 900 and 1920 by 1080.
- They always go to `out/screens/current`.
- With `GROUPLAB_SCREENS_TO_DOCS` set, they also go to `docs/figures/screens/current/`, which is how the committed set was made.
- No drift test reads them.

**Tests:** Core 918 and App 85 passing, none skipped.

## Entry 110. Question 23 answered, the ballistic solver ported from ballistics.js, and its two validations

`docs/NOTES-FROM-PLANNING.md` entry 110, both numbered sections. **Three parts of section 2 are not done as written,** and each is a question or the entry's own second option:
- the Coriolis vertical term is held out (question 24);
- the G1 table fails the independent gate (question 25);
- aerodynamic jump is left out.

### Section 1: question 23

Set to answered, pointing at entry 110.

### Section 2: the solver

**Where things are.**
- **The source,** unchanged, is `reference/ballistics-js/ballistics.js`, SHA-256 `581fab43209367af53f5271b7dbc3256c52aafe9c6b6a20a3bb4db0c24e7aa14`, with a README giving its source, date, licence and what GroupLab takes and does not take from it. `.gitattributes` keeps it byte for byte on any checkout.
- **`reference/` was ignored as a whole,** to keep local material such as exports out. It is now `reference/*`, with only `ballistics-js/`, `ballistics-cases.json` and `py-ballisticcalc/` un-ignored; everything that was already there stays ignored.
- **The port** is `src/GroupLab.Core/Ballistics`:
  - `DragTables`, copied from the JavaScript by a script and not retyped;
  - `Atmosphere`;
  - `Stability`, for Miller's factor, Litz's spin drift and the Coriolis horizontal term;
  - `BallisticSolver`.
- **`grouplab trajectory`** prints a table from stated inputs. Every assumed input is printed above the table, and below it: "Aerodynamic jump is not modelled." and "The Coriolis vertical (Eötvös) term is not modelled."

**Ported as it stands:**
- the drag constant, checked: π × 0.0764742 / 1152 = 0.00020856;
- the RK4 integration and the zeroing search;
- lag-rule wind;
- the atmosphere, without `pressureFromAltitude`'s four unused variables;
- Miller's stability factor;
- Litz's spin drift;
- the Coriolis horizontal term.

**Intentional differences from ballistics.js, each with the case that shows it.** The case is the .308, 175 gr, G7 0.243, 2700 fps, zeroed at 100 yd, 59 °F, 29.92 inHg, 50 percent, 10 mph, unless named:

| Difference | What the case shows |
|---|---|
| Shooting angle measured along the line of sight, drop perpendicular to it, the rifle zeroed on the flat | At 5 degrees the port reads −0.01, 11.19 and 37.46 MOA at 100, 500 and 1000 yd, where the JavaScript read −297.6, −285.6 and −258.9. The rifleman's rule holds within the stated 5 percent plus 0.25 in to 600 yd at 5 and 10 degrees, and uphill and downhill agree within 0.05 MOA to 300 yd. |
| The zero by RK4, and rows interpolated to their exact range | The zero angle is 3.8199 MOA against the JavaScript's 3.8342. Drop at the zero range is 0.000 in against 0.012 in. At 1000 yd, 37.508 MOA against 37.497. |
| Full precision, rounded only for display | The table rounds; the rows do not. |
| Miller's pressure correction | 10 in twist, 0.308 in, 1.5 in, 215 gr, 2900 fps, 90 °F: SG 2.146 at 26.0 inHg against 1.865 without the pressure term. At 29.92 inHg the two agree exactly, and the transcription check holds that. |
| The BC's reference atmosphere, ICAO by default, Army Standard Metro available | The Metro density by the solver's own formula is within 0.1 percent of its defined 0.0751265 lb/ft³. The density ratio differs by 1.018, and the case reads 38.10 MOA at 1000 yd against 37.51 with an ICAO coefficient. How section 2e was read is in question 24 section 3. |
| Aerodynamic jump not modelled | Section 2c's second option. The publication was not to hand to check Litz's fit against, and the output says so. |
| Coriolis vertical not modelled | Its sign is reversed in the JavaScript (question 24). |
| The dispersion utilities not ported | Section 2d. |
| `solveExtended`'s compass wind not ported | It adds a wind from the right as drift to the right. The port takes a signed crosswind (question 24 section 3). |

**Validation 1, against ballistics.js, for the transcription.**
- ballistics.js ran unchanged under Node on a GitHub runner, and its output is `tests/GroupLab.Core.Tests/Fixtures/ballistics-js.json`.
- The Linux CI job now runs it again on every push and fails if its output differs.
- **All 66 rows of the six cases match within the JavaScript's own rounding,** with the port in its JavaScript-compatible mode.
- The drag tables at every hundredth of Mach to 5.2, the atmosphere at 175 conditions and seven altitudes, Miller's factor, spin drift and the Coriolis horizontal term all match to 1 part in 10^9.

**The entry's own figures for the .308 case were not the file's.** The JavaScript, run on the case as section 2a states it, reads 11.23 MOA at 500 yd and 37.50 at 1000, where the entry has 11.06 and 37.37. Its drop at the zero range, 0.01 in, is as the entry says. The angle fault is the same either way.

**Validation 2, against py-ballisticcalc, for the gate.**
- The tolerances were committed and pushed before either reference table existed:
  - drop 0.10 MOA plus 1 percent;
  - wind 0.05 MOA plus 2 percent;
  - time of flight 0.5 percent;
  - each with its reason, in `docs/BALLISTICS-VALIDATION.md`.
- py-ballisticcalc 2.3.1, LGPL-3.0-only, ran on a runner. Its tables are committed and the library is not.
- **All four G7 cases pass at every range to 1000 yd.** The largest share of any allowance used is 4 percent for drop and for wind and 7 percent for time of flight.
- **Both G1 cases fail from 100 yd on:** at 1000 yd, 31.77 MOA against 40.33 and 22.70 against 28.47, with time of flight 13 percent short.

**Why G1 fails: the JavaScript's G1 table is not the standard G1 function.**
- Compared point by point with py-ballisticcalc's, 60 of 79 shared Mach points differ above Mach 0.85:
  - 0.5210 against 0.4805 at Mach 1.0;
  - 0.5295 against 0.6625 at Mach 1.4;
  - 0.2571 against 0.4988 at Mach 5.0.
- **The G7 table matches** except at Mach 3.5 and above, where no rifle bullet here flies.
- The gate test holds the two G1 cases by name as known failures, pointing at question 25, and fails if either starts to pass. **The tolerance was not widened.**
- **The website serves this table now,** so its G1 results are affected independently of GroupLab.

**The README's Phase 5 solver line is In progress,** not "Built, not proven": the gate is met for G7 and not for G1. DESIGN.md section 16 records the port and what it did not take.

**Tests:** Core 938 and App 85 passing, none skipped.

## Entry 111. Questions 24 and 25 answered, four small screen fixes, and the paths ready for the range material

`docs/NOTES-FROM-PLANNING.md` entry 111, every numbered section. Nothing from the range material was processed, as section 4 says.

### Section 1: the standard G1 and G7 tables

**The second transcription.** The rule needs one that is not a copy of py-ballisticcalc's. py-ballisticcalc's own README gives its lineage: Nikolay Gekht's C# port of JBM Ballistics' C code. So anything in that line, including the gehtsoft ports and projects copied from them, would not do. I searched for tables with another lineage:
- **poncelet**, MIT, carries G1 and G7 as it took them from JBM's published `mcg1.txt` and `mcg7.txt`, attributed there to McCoy's *Modern Exterior Ballistics* appendix A. It reached JBM's tables through JBM's text files, not its code, and not through py-ballisticcalc.
- **W. J. Jurens's naval tables,** in `master_exterior_ballistics`, have another lineage again, but carry only artillery functions, no G1 or G7.

**The two agree at every point:** 79 of 79 in G1 and 84 of 84 in G7, Mach and Cd, to the four decimals both publish. None disagreed, so none had to be listed.
- **Committed:** both, as the strings they print, in `reference/drag-tables`, each with its source file, commit and licence. The carried tables were generated from them.
- **`DragTableSourceTests`** holds the carried tables to both, point by point.
- **Named** in the code and in `docs/BALLISTICS-VALIDATION.md` section 3, which says plainly that both trace to McCoy through JBM, by different routes.

**What changed against ballistics.js:**
- **G1:** 60 of 79 points differ above Mach 0.85.
- **G7:** it agreed below Mach 3.5; the standard carries 84 points to its 73 and differs at Mach 3.5, 4.0 and 5.0.
- **The JavaScript's own tables** stay only for the transcription check's compatible mode, which still matches all 66 rows. The standard tables are listed there as an intentional difference.

**The gate, rerun with the tolerances committed before the first run, unchanged: every case passes.**

| Case at 1000 yd | Drop, port | Drop, py-ballisticcalc | Before the change |
|---|---|---|---|
| .308 168 gr G1 | 40.327 MOA | 40.333 MOA | 31.770 MOA |
| 6mm 105 gr G1 | 28.470 MOA | 28.474 MOA | 22.696 MOA |

The largest share of any allowance used across all six cases is 4 percent for drop and wind and 7 percent for time of flight. **The two known-failure markers are removed**, and the gate test holds every case. The README's Phase 5 solver line is now **Built, not proven**: it is validated, and not yet in the interface.

### Section 2: the Coriolis vertical term

**Ported with the sign corrected:** half of 2ΩV cos(latitude) sin(azimuth) times t², positive up, with V the average horizontal speed. `BallisticInput` gains the azimuth, and `grouplab trajectory` gains `--azimuth` and a column for the term.
- **Tests:** due east rises and due west falls by the same amount, north and south give zero, and 45 degrees north, east, 1000 yd and 1.6 s gives +2.97 in. Through the solver the same holds, with the rise above 2.5 in at 1000 yd for the .308 case.
- **"Aerodynamic jump is not modelled."** is now the only sentence under the solver's output.
- **DESIGN.md section 16** records the five corrections, so no later reader restores the JavaScript's behaviour: the G1 table, the shooting angle, aerodynamic jump left out, the Coriolis vertical sign, and the wind direction.

### Section 3: four things on the screens

- **Enum names.** The exclusion reasons read "Called flyer", "Bad round" and "Pulled shot", in the list and in "excluded as called flyer". The saved marking keeps the enum's name, which is a file format.
  - **`NoEnumNameReachesAPerson`** walks every screen and fails on any compound enum name anywhere a person reads: the marking screen with a shot excluded and selected, every stage of the timeline, the analysis with every "why" open, and the settings.
  - **It found a second leak on its first run:** the assignment stage's decision in Show work read "Decided assignment: OneToOne". It now reads "one-to-one matching".
  - Two older tests had pinned "excluded as CalledFlyer" and now expect the words.
- **Each "why" takes no row of its own.** It is a small "why ▸" set beside the last line of the item it explains, in room the line leaves for it, and it opens the explanation beneath the item.
  - It is a plain button rather than a toggle, because the theme paints a checked toggle amber, and amber means something needs a person.
  - A test checks each sits in its item's row.
- **The Load block.** Each label sits at the top of its row, so "Calibre" is beside the first line of ".308 in (7.82 mm), set after detection", not under the last.
- **The shot count once.** The "Shots 24" row is gone; the sentence "24 shots: 24 detected." stays, since it also says how the shots were placed.

The renders under `docs/figures/screens/current/` are regenerated.

### Section 4: the paths for the range material

**Nothing from `C:\Dev\grouplab-range-2026-09-20\` was read or processed.** Its photographs carry location data, so they go through the intake tool first, under the next entry.

| Path | Before this entry | Now |
|---|---|---|
| The Phase 1 detection gate over a folder of new scans, one command | Did not exist. `grouplab analyze` takes one image, and the gate records run over the fixed corpus folders | **Built:** `grouplab analyze-folder <directory> [--markings <directory>]`. One line per image: the definition its codes named, markers found of expected, holes detected, review items open, registration residual and time. With `--markings`, each result is saved as a marking file, which once corrected in the screen is that sheet's truth pass. |
| The Phase 3 timing on one sheet, one command | Did not exist. Entry 84's rehearsal measured only the software's share | **Built:** `grouplab timing <log file>`. It reads the log GroupLab already writes, and for each image opened prints the seconds from the marks appearing to Accept and analyse, the seconds from opening, the review choices made and the items left open. The gate is two minutes by a person, so this reports the time and judges nothing. |
| The blank-paper route | Not started, and its gate names the material it needs | **Not built**, as the entry says. The blank sheet shot at a marker dot, photographed with a tape measure across it, is the material the gate names. |

**Tests:** Core 944 and App 87 passing, none skipped.

## Entry 112. Session records, the report, the target library and the solver on screen

`docs/NOTES-FROM-PLANNING.md` entry 112, every numbered section. Nothing under `C:\Dev\grouplab-range-2026-09-20\` was read. Garmin Xero import and Android were not started.

### Section 1: session records in SQLite

**One database, `grouplab.db`, beside the settings in the application data folder,** through Microsoft.Data.Sqlite, which THIRD-PARTY-NOTICES.md now lists with SQLitePCLRaw and SQLite.
- **The schema is documented** in `docs/SESSION-SCHEMA.md` with its version, 1. A test holds the document, the statements the code runs, and the schema SQLite keeps to one another.
- **The old `records.json` is read in** on the first open and kept beside the database as `records.pre-database.json`. A second open leaves it alone.
- **Full JSON export and import.** An export read into an empty database exports again byte for byte the same.
- **The chronograph has room.** Its strings are their own ordered list, with an explicit shot-to-reading mapping, in tables of their own. Deleting a session takes its strings with it. Import is Phase 5 and is not built.

**What a session keeps:**
- the date, distance, rifle, barrel, load and calibre;
- the marking with every edit and exclusion;
- the mean radius with its interval;
- the sheet's definition;
- a proof image;
- the original image by its path and SHA-256, never copied.

**Section 18's three tiers:**
- **The geometry** is the marking itself.
- **The proof image** is a JPEG at 150 dpi, or 1650 pixels on the long side where there is no scale. It is about 200 KB on the test sheet.
- **The original** is referenced and never copied.

**No image is needed to reopen.** The marking file now keeps the sheet's registration as numbers, for each of the three models: the homography's nine values, the radial model's centre, scale and two coefficients, and the surface model's parameters and page. So a reopened session has its scale and plot without the photograph. A marking from before this still reads, with its old note to detect again. A reopened session uses the original image only where it is still where it was with the same hash, and it says so when it is not.

**Accept and analyse saves the session.** A second Accept on the same marking updates it and keeps the day it was first saved. The Session records screen, on the rail:
- lists every session newest first: date, sheet, rifle, load, distance, shot count, and mean radius with its interval;
- filters by rifle and by load;
- opens a session back to its analysis from its row;
- asks before deleting one.

### Section 2: the report

**The analysis's Report button saves a PDF from GroupLab's own writer.**
- **Page 1:** the particulars; the composite plot, drawn as dots in the bullet's outline, excluded ones hollow, with the centre and the CEP 50 and CEP 90 circles; every figure with its interval; the zero correction with its verdict; and the two cards.
- **Page 2:** the shot table, with every bull and each shot's standing, an excluded one struck through with its reason; the exclusions with their reasons; the decisions left unmade; the registration; every "why"; and the version and identifiers.
- A report longer than that continues on a further page rather than being cut, and every page is numbered with the sheet and the date.

**Nothing on paper is more certain than the screen.** The report takes every sentence from the functions the screen lays out: the figure lines, the cards, the zero block and the "why" texts were moved into shared functions for it. A test holds every figure and card line on paper to the screen's text.
- **Every figure is given with and without exclusions.** CEP 90 and width by height had no without-exclusions line on screen, and gained one there too.
- **The stringing power statement stays** with the negative on page 1.
- The test covers the same session with an exclusion and without.

### Section 3: the target library

**The rail's library slot opens it.** It lists the built-in sheets by family, read only, and the person's own sheets after them. An own sheet is saved as canonical GLTD-J in a `sheets` folder of the data folder.
- **An own sheet can be renamed, duplicated and deleted after asking.** Duplicate also works on a built-in sheet, as the start of an own one.
- **The designer saves** into the own sheets.
- **The print screen lists both,** and an own sheet prints through exactly the refusals a built-in one does.
- **The name is not in the printed codes,** so a rename changes no definition identifier. A test checks that.

**A sheet a session used stays readable because the session keeps its own copy of the definition.** Deleting is therefore allowed, and the question before it says how many sessions were analysed against the sheet and that each keeps its copy. The alternative, refusing, would make a sheet undeletable for as long as any session of it is kept. A test deletes a sheet a session used and reopens the session.

### Section 4: the solver on screen

- **The records gain the solver's fields,** all optional:
  - the rifle: sight height, zero distance and twist;
  - the load: muzzle velocity and its SD, BC, drag model, the BC's reference atmosphere, and bullet weight, length and diameter.
  A record missing what the solver needs says which field, both on the analysis and on the new Ballistics screen.
- **The zero correction carried to a second distance,** beneath the zero block's verdict:
  - Windage carries in proportion to range, since drag leaves the sideways share of the velocity unchanged.
  - Elevation carries by the solver's own linearisation: the ratio of the path's changes at the two distances under a small change of the bore's angle, by a central difference of the zero range. A test holds it within 1 percent of a directly flown change.
  - The half-width carries by the same factor.
  - An axis that could not be told from zero is not carried, and it says so.
- **A dope table on the Ballistics screen:** range, drop, the elevation and its clicks, and a 10 mph crosswind's wind with its windage and clicks, in the display units, with the air as an input. It shows "Aerodynamic jump is not modelled." beneath it, and says whether spin drift is available. A test holds its rows to the solver's own.
- **The screen has no slot in the concept's rail,** so it has a seventh one. That is question 26.

## Entry 113. The analysis finished, load comparison, hit probability, the range tooling, the volunteer pack and the user guide

`docs/NOTES-FROM-PLANNING.md` entry 113, every numbered section, after entry 112. Nothing under `C:\Dev\grouplab-range-2026-09-20\` was read.

### Section 1: the analysis screen's unbuilt parts

- **The sheet's thumbnail** heads the analysis's left column. It is drawn from the definition, not the photograph, and every shot sits at its bull's centre plus its offset. A click on a bull selects its shots on the plot, the table and the sheet.
- **The full CEP table and bivariate fit** sit behind one disclosure that remembers it was opened.
  - The table gives CEP 50, 90, 95 and 99 three ways: the circular estimate with its interval, the correlated normal, and Grubbs-Patnaik.
  - The fit gives the centre and each axis's spread with their intervals, the correlation, the error ellipse, and the ellipse holding 95 percent of shots.
  - Both are given with every shot and again without exclusions.
- **The renders are regenerated,** and the README's analysis line is **Done**.

### Section 2: comparing loads

**The rail's chart slot is the concept's Compare loads.** Two or more sessions are ticked in Session records, or a sheet's subgroups are chosen, and they appear side by side. Each has its plot and figures with intervals: sigma, mean radius, and the subordinate extreme spread. Below them are the tests with their verdicts:
- **Two loads:** the sigma ratio's F test and Hotelling's T squared.
- **More than two:** Fligner-Killeen and MANOVA, with every pair's ratio and its Holm-adjusted p.

**Every negative carries what it could have detected.**
- The dispersion test gives the smallest difference its shot counts detect with 80 percent power.
- The centre test gives the smallest shift it detects, from the large-sample noncentrality of a 2-degree test.

**Nothing is ranked by point estimate.** The groups stay in the order chosen, and when the sigma intervals overlap, the headline says the data do not separate them. The planning table gives the shots per load for 10, 25 and 50 percent. Sessions at different distances are compared as angles, and the screen says so.

**Subgroups can be compared, but nothing on screen assigns them.** That is question 27.

### Section 3: hit probability at distance, and distance normalisation

Built as specified.
- **The partial derivatives come from the solver:** drift per mph, and drop per ft/s with the bore's angle held. For that, the solver's input gained an optional fixed launch angle; without it, nothing changes.
- **The velocity's share at the distance shot comes out in quadrature first,** and is refused when larger than the group.
- **P(hit) at both ends of the sigma interval and at its estimate,** for a circle or a rectangle about the aim plus the carried zero offset.
- **Everything is labelled a prediction.** With neither spread, it says it is angular scaling and nothing more.

**The entry's four tests pass:**
- with no spread, it is angular scaling exactly;
- with a velocity SD, vertical grows faster than distance;
- a centred circle with equal axes is the Rayleigh closed form, by the closed form and by the integration;
- the refusal happens when it should.

The screen is a section of the Ballistics screen, carrying the group open in the analysis.

### Section 4: tooling for the range material, without reading it

**`grouplab compare-photos <scan> <photograph>... [--truth <marking>]`.**
- It uses the scan's corrected marking as truth when given, or its own detection otherwise, and says which on its first line.
- **For each photograph it reports:** the registration model; bull-centre error, worst and median; holes found, missed and false; and hole-position error, median, 95th percentile and worst.
- **Holes are paired nearest first within 0.15 in.**
- **The table is read against 0.005 in and 0.15 in and decides neither gate.**
- **Tested on synthetic data:** a synthetic scan of GL-CF25-LTR, and a photograph of the same holes turned 1.5 degrees at 280 dpi. The truth is checked both ways, with a corrected marking missing one hole giving one false.

**The doubles sheet.**
- **Without a rule,** one-to-one matching pushes each second shot onto an empty bull, and the review queue raises every pushed shot as contested, naming the bull that already holds a shot. The matching can chain, so more than ten shots can be pushed, and every one is raised.
- **The setting was missing and is built:** Shots per bull, in the editor's side panel. It offers one a bull, matched; nearest bull, however many; or two on the bulls named.
- **The rule is how the sheet is read, not an edit,** so it re-baselines the detected reading and nothing it places is flagged as moved.
- **A bull holding what the rule expects is not raised as doubled.** The rule is kept in the marking file.
- **Both paths are tested** on a synthetic doubles sheet.
- **The blank sheet stays Not started.**

### Section 5: the volunteer print pack

**"Print a volunteer pack", on the print screen,** gives the sheet and one page of instructions in one PDF, opened to print at actual size.
- **The page is generated from `docs/VOLUNTEER-PACK.md`,** which is embedded so the two cannot drift, at the sheet's own paper size.
- **It carries everything the entry lists,** including the sheet's own distance from bull 1 to bull 5: 5.98 in (152.0 mm) on the Letter 5x5 sheet, whose pitch is 38.0 mm.
- **It points to `https://pissinhot.com/targets`,** where submitting means following that page's terms.
- **Consent is not in the pack.**
- A test holds all of that, and refuses a pack that runs past one page.

### Section 6: the user guide

**`docs/USER-GUIDE.md` and `docs/USER-GUIDE.pdf`.** The guide walks through printing, shooting, photographing and scanning, marking and the review queue, reading the analysis, sessions and the report, comparing loads, and the dope table. It says what each "why" says in plain words, and it is illustrated with the committed renders.
- **The PDF comes from GroupLab's own writer,** which gained JPEG pictures for it. `grouplab user-guide` scales each render to 1400 pixels across.
- **A test fails if a screenshot the guide names does not exist.**

### Section 7: housekeeping

- **Enum names.** `NoEnumNameReachesAPerson` now also walks Session records, the target library, Ballistics, Compare loads and the report's pages.
  - It found nothing new.
  - The roll sheets' "Roll24" in the print screen's and library's paper was found by looking, and now reads "24 in roll".
- **Themes and keyboard.** A test walks every new screen in all four themes and requires every visible text colour to be one of that theme's text roles, which `ThemeTests` holds to their ratios on every surface. Every control a person operates there takes the keyboard's focus.
- **Renders:** every new screen in dark and light at both sizes, under `docs/figures/screens/current/`: sessions, library, ballistics and compare.

**Tests:** Core 973 and App 98 passing, none skipped. Questions 26 and 27 are raised and nothing waited on them.

## Entry 114. The in-app print path dropped every rectangle, and the hole that is not detected

`docs/NOTES-FROM-PLANNING.md` entry 114. Section 1 is done. Section 2 could not be started: the scan is not on this machine, and the entry says to stop in that case. Section 3 is a record and needs nothing built.

### Section 1: what the fault was

**Every filled rectangle was drawn with `FillRect`, which is a pattern blit rather than a drawing call.** `WindowsPrinter.Draw` drew the disc bands as closed paths filled with `FillPath`, and the text with `TextOut`, but the rectangles, which are every AprilTag marker, both QR codes and the load block's rules, went through the USER32 `FillRect`. That is a blit of a brush pattern, and a driver is free to handle it differently from a path. Microsoft Print to PDF honoured it. **The Brother MFC-J430W driver dropped every one of them**, which is exactly the pattern Alan photographed: rings, dots and text present, markers, codes and rules gone.

**The capability bits do not tell the two drivers apart.** Asked through `GetDeviceCaps`, both report the same `RASTERCAPS` of 0x6E99, blits included, and the same curve, line, polygon and text capabilities:

| Driver | TECHNOLOGY | RASTERCAPS | RC_BITBLT |
|---|---|---|---|
| Microsoft Print to PDF | 2, a raster printer | 0x6E99 | claimed |
| Brother MFC-J430W | 2, a raster printer | 0x6E99 | claimed |

So a capability check would not have caught it, and neither would "it works on my printer".

### Section 1: the measurement that proved it, with no paper used

A job printed with `DOCINFO.lpszOutput` set goes to a file instead of the port, so a driver can be exercised without printing. The sheet was printed twice through each driver, once as it is and once with every `RectFill` removed, and the jobs compared:

| Driver | The sheet, 4,329 rectangles | The same sheet with every rectangle removed | Verdict |
|---|---|---|---|
| Microsoft Print to PDF | 145,666 bytes | 92,076 bytes | the rectangles reach it |
| **Brother MFC-J430W** | **612,632 bytes** | **612,632 bytes** | **every rectangle dropped** |

Byte for byte the same job, with and without 4,329 marks on the page.

### Section 1: the fix, and the same measurement after it

**Every filled shape is now a closed path.** A rectangle is `BeginPath`, `Polygon` over its four corners, `EndPath`, `FillPath`, which is what the disc bands always did and what every driver honours.

| Driver | The sheet | Without its rectangles |
|---|---|---|
| Microsoft Print to PDF | 145,934 bytes | 92,076 bytes |
| **Brother MFC-J430W** | **919,905 bytes** | **612,632 bytes** |

**A page the driver will not draw in full is no longer committed.** Every drawing call's answer is now read. If any item fails, the document is aborted and the message names how many of the sheet's marks were refused, because a sheet with a marker missing looks normal and cannot be measured.

### Section 1: the comparison as a permanent test

`PrintedItemsTests`, two tests, neither needing paper:
- **Every built-in sheet, item by item.** Each sheet is printed through the in-app path to "Microsoft Print to PDF", rasterised at 200 dpi, and compared with the same sheet from Save PDF rasterised the same way. Every marker, code module, ring band, load block rule and text item must carry its ink in the same place, to half of what the reference has there. **19 sheets, 104,317 items**; 3 sheets are refused by that driver's paper and margins before anything is drawn, which is `PrintFit`'s own test.
- **Every driver that writes a file silently.** The sheet is printed with its rectangles and without them, and a driver that keeps them must write a larger job.

**The tests were checked against the fault.** With the rectangle fill put back to what it was, both fail, and they name what Alan saw:
- "Brother MFC-J430W Printer wrote 612632 bytes for the sheet and 612632 for the same sheet with every rectangle taken out: its driver is dropping them";
- "GroupLab 5x5 Load Development with Load Block, Letter page 1: 642 of 642 markers are missing from the printed sheet", with its 1,696 code modules and 44 load block rules beside it.

**Which printers the tests may use.** A fixed list: "Microsoft Print to PDF", "Microsoft XPS Document Writer", and anything named in `GROUPLAB_PRINT_DRIVERS`. **Nothing enumerates the machine's printers**, because a driver can open an application when it is printed to, as the OneNote one does. The Brother figures above were measured by naming it in that variable. Where fewer than two of those printers are installed, the driver comparison skips with its reason, which is what CI does.

### Section 1: the Print button

**Demoted, not disabled, and the screen says why.** "Open to print" is now the amber primary and comes first; "Print" stays beside it, because hiding it would leave a person who wants it with no path and no explanation. Above the buttons, in the warning role:

> Print from inside GroupLab lost every marker and code on a Brother printer on 19 September. The drawing is fixed, and two drivers are held to it by a test, but no sheet from the fixed version has been looked at on paper yet, so Open to print is the safer path today.

That sentence comes out when a sheet from the fixed path has been checked on paper.

### Section 2: the hole that is not detected

**Not started, and nothing was changed.** The entry says the scan will be at `C:\Dev\grouplab-range-2026-09-20\sheet1\sheet1-scan.png` and to stop if it is not there. That folder holds one file, `grouplab-range-day-2026-09-20.pdf`, and no `sheet1` folder. Nothing was read from it, nothing was added to the corpus, and no threshold was touched. The measurement the entry asks for, which stage drops the hole, waits for the scan.

### Section 3: what yesterday produced

Recorded as the entry states it: one sheet, nine shots, scanned flat, no photographs. **The mounted photograph gate, the 25-shot editor gate and the blank-paper gate have nothing.**

## Entry 115. Questions 26 and 27 answered, chronograph strings by hand, and the paths a real sheet takes

`docs/NOTES-FROM-PLANNING.md` entry 115, every numbered section, after entry 114.

### Section 1: question 26, the Ballistics slot

**Answered and nothing changed.** The slot stays, and the rail has seven buttons with the gear. Question 26 is set to answered.

### Section 2: question 27, a load per bull

**Built as recommended, with the addition the entry asks for.**
- **With the select tool, shift and click chooses a bull**, and clicking again takes it back; the chosen bulls are ringed on the sheet.
- **Shift and click, rather than a plain click, because every bull on a shot sheet has a hole on it.** A plain click there selects the hole, which is what the editor has always done, and a ladder sheet would be unusable if that changed.
- **One field sets the load on all of them**, from the loads the records carry, so a load named twice is not two loads.
- **The bulls sharing a load are a subgroup.** The panel lists each load with its bull count, and says how many bulls carry no load and so belong to no subgroup.
- **It is kept in the marking**, so a session carries its subgroups, a reopened session still has them, and the comparison screen takes them.

Question 27 is set to answered.

### Section 3: chronograph strings by hand, and the reconciliation

**Built on the Ballistics screen, for the session open in the analysis.** A string is pasted or typed with its source and date; a list that is not velocities is refused by naming the first thing that is not one.

**The reconciliation is `DESIGN.md` section 15's, not a pairing by position.**
- With the counts equal, the in-order pairing is offered as a proposal, with the sentence that the counts agree and that a chronograph can miss a shot and record a neighbour's.
- A reading that belongs to no shot, the fouling round fired into the berm, is marked and left out, and the rest pair in order.
- A shot the chronograph missed is marked and keeps no reading.
- What is accepted is kept on the session with its mapping. A reading of no shot stays in the string, because it was recorded; it simply belongs to no shot.

**The spread is fed in rather than typed.** The readings kept give their own mean and standard deviation, and the load's velocity SD is set from them with a note of where it came from, which the Ballistics screen shows beside the field: "24 readings, Garmin Xero C1, 2026-09-20". That is the input the hit probability of entry 113 section 3 takes.

**The schema goes to version 2** for that note, with an upgrade that runs when an older database is opened, in one transaction with the version. `docs/SESSION-SCHEMA.md` carries the statement and the table of upgrades, and a test opens a version 1 database, checks it comes up to version 2, and checks its records are still there.

**Tests:** the entry's four cases in the engine, equal counts, a missing reading, an extra reading and no string at all, and the same four on screen.

### Section 4: what happens when the sheet is not perfect

**A sheet with no codes is offered by name.** Where the codes cannot be read, which is what a sheet printed before the entry 114 fix looks like, the screen asks "Which sheet is this?" with the library and the person's own sheets in a list, and says that a sheet whose codes did not print still registers from its markers. Choosing it detects as any other sheet does. Marking it by hand stays beside it. It used to open a file picker for a `.gltd.json`, or, on the automatic path, stop with the identification failure as the reason.

**A scan turned any way round reads the same.** Tested at 90, 180 and 270 degrees on a synthetic GL-CF25-LTR: every hole lands within 0.02 in of where it lands upright, against the 0.15 in the gate matches holes by.

**The messages, each driven by an image that provokes it.**

| What is wrong | What GroupLab now says |
|---|---|
| Too few pixels | "This image is about 60 pixels to the inch at the sheet, and the detector needs 120. Scan it at 300 dpi, or photograph it closer so the sheet fills the frame." |
| Too few markers | "6 of the sheet's 38 markers were found, and registering needs 4..." with what to do |
| Not the sheet chosen | "This may not be GroupLab 6x6 Rimfire, Letter: only 21 of its 40 bulls were found where it puts them. Check the sheet, and choose the one this image really is; the figures mean nothing if it is another sheet." |
| Printed at the wrong scale | "This sheet was printed at 97.0 percent of its intended size. The measurements are corrected for it, and the figures are right; print at actual size, 100 percent, to keep the sheet's own spacing." |

**Two things the measurement found along the way.**
- **The detector copes further down in resolution than expected.** On a synthetic sheet: 300 and 150 dpi find every marker and every hole; 96 dpi finds 31 of 38 markers and 24 of 25 holes; 75 dpi finds 15 markers and the same 24 holes; at 60 dpi nothing is found. So the low-resolution message is keyed at 120 dpi, and the figure is recorded in the code rather than guessed.
- **Reading a sheet as the wrong definition used to give numbers with nothing said.** Its markers are the same family, so it registers and then measures the wrong bulls. Measured, reading a GL-CF25-LTR scan as four other sheets: as the 6x6 rimfire, 38 of 54 markers and 21 of 40 bulls; as the 5x6, all 38 markers but a fit of 1.63 dmm against its own sheet's 0.14; as the zeroing grid, 14 markers that grid does not have; as the A4 5x5, 17 of 28 bulls and **15 holes detected**. Each of those is now doubted out loud.

### Section 5: how long an analysis takes

**Measured on this machine**, a Windows 11 workstation, with `AnalyzeVerb.Analyze` end to end, twice each, on images already in the corpus. Times are wall clock for the whole analysis, and the stages are the trace's own.

| Image | Total | The slowest stages |
|---|---|---|
| `gl-cf25-ltr-1-600-dpi.png`, a 600 dpi Letter scan | 17.7 s and 22.8 s | identify 9.7 to 13.7 s, holes 4.1 to 4.9 s, bulls 3.0 s, decode 0.7 to 0.9 s |
| `gl-cf25-ltr-1-300-dpi.png`, the same sheet at 300 dpi | 2.6 s both runs | holes 1.0 s, bulls 0.9 s, identify 0.5 s, decode 0.2 s |
| `20260913_130543.jpg`, a phone photograph | 4.4 s and 4.2 s | holes 1.6 s, identify 1.3 s, bulls 1.2 s, fiducials 0.11 s |

**The figure worth keeping:** identification is over half the time of a 600 dpi scan, and it grows about twentyfold between 300 and 600 dpi while the image grows fourfold. Nothing was changed: entry 117 is where candidates are recorded with their measurements.

### Section 6: a corrected ballistics.js

**`reference/ballistics-js/ballistics.corrected.js`**, generated from the file the website serves with the five faults put right and nothing else changed: the same names, call signature and returned fields. Each change is marked `CORRECTED` in the file, and `reference/ballistics-js/CORRECTIONS.md` says what each one is in plain words.

| The fault | What the corrected file does |
|---|---|
| The G1 table is not the standard function above Mach 0.85 | Carries the standard table, the values two transcriptions agree on at all 79 points |
| Drop measured from the horizontal, so an angle reported the line of sight's own rise | Zeroes on the flat, then measures range along the line of sight and drop perpendicular to it |
| Aerodynamic jump from a formula that is not published and runs the wrong way with stability | Returns zero, with the fields left in place so anything reading them still works |
| The Coriolis vertical term's sign | Firing east strikes high |
| A wind from the right added as drift to the right | The bullet drifts away from the wind |

**The check.** `cases.corrected.js` runs it under Node on the six cases, and `CorrectedJavaScriptTests` holds every row to GroupLab's own solver under the tolerances of `docs/BALLISTICS-VALIDATION.md` section 2, plus the rounding the JavaScript applies to its printed rows. **This machine has no Node**, so the comparison could not be run here; CI runs it before the tests on every push, and the test fails rather than skips when it is missing there.

**Nothing was sent anywhere.** The file is for Alan to upload or not.

**A sixth fault, found by the check on its first real run.** This machine has no Node, so the comparison first ran on CI, and it failed: at 100 yards the corrected JavaScript gave 0.99 MOA of wind drift where GroupLab gave 0.91, and a time of flight half a percent long. The cause is not physics. The JavaScript's table loop recorded at the first integration step whose range had passed the next range in the table, and printed that state under the range it had passed, so every row was the bullet a fraction of a step further on. At 1000 yards that overshoot is a small share of a long flight and vanishes into the printed rounding; at 100 yards it is a tenth of the drift, which is why reading the file did not show it and running it did.

**It was confirmed here without Node.** GroupLab's own solver keeps a JavaScript-compatible mode for the transcription check, which reproduces the original's sampling, and it gives the JavaScript's figures to the digit: 1.040 inches of drift at 100 yards on the .223 case against the 0.956 its exact sampling gives, which are 0.993 and 0.913 MOA, the two numbers CI printed. **The corrected file now interpolates each row to the range it is labelled with**, and prints the time of flight to four places, since three cannot hold a tenth-second flight to half a percent. `CORRECTIONS.md` carries six changes.

**GroupLab's own port never had this fault**, and the five corrections recorded in `DESIGN.md` section 16 are unchanged: they are corrections of physics, and this one is a fault in how the JavaScript fills its table.

### Section 7: the rest

- **The support link** is question 28: it needs an address, and nothing was built.
- **Found in passing and fixed:** a sheet read as the wrong definition gave numbers silently, which section 4 now says out loud.
- **Found in passing and recorded, not changed:** identification's cost at 600 dpi, above.
- **An older instruction superseded, named here rather than worked around.** Entry 76 section 4 said an image that is not a GroupLab sheet "does nothing and says so, with no definition asked for", and three window tests pinned that. Section 4 of this entry asks for the opposite, and is newer: the screen now asks which sheet it is, by name. The three tests were rewritten to the new behaviour rather than the new behaviour bent to them, and two of those tests also pinned the words "Detection failed", which section 4 replaced with what to do next.

## Entry 116. A Windows package, an installer, a release anybody can download, and a page for a tester

`docs/NOTES-FROM-PLANNING.md` entry 116, every numbered section, after entries 114 and 115.

### Section 1: a Windows package that runs on a machine with nothing installed

**`scripts/package-windows.ps1`**, one command, which publishes GroupLab self-contained for win-x64, puts the licence, the notices, a read me and the samples beside it, checks the package with itself, and zips it.

| What the package is | |
|---|---|
| Zip | 120 MB |
| Unpacked | 311 MB, 270 files |
| Named | `grouplab-0.1.0-win-x64-<short commit>.zip`, and again as `grouplab-win-x64.zip` |
| Carries | `GroupLab.App.exe`, `grouplab.exe`, the .NET runtime, `OpenCvSharpExtern.dll`, the twenty sheets, `LICENSE`, `THIRD-PARTY-NOTICES.md`, `README.txt`, two samples |

**Not a single-file publish.** The entry warns that a single file needs `IncludeNativeLibrariesForSelfExtract` or it fails at the first analysis. A folder publish avoids the question: the native library sits beside the executable where the loader expects it, nothing is extracted to a temporary directory on first run, and an antivirus has one ordinary folder to look at rather than a self-extracting executable, which is the shape that gets quarantined.

**How it was tested with no .NET installed.** Not by hoping: `dotnet` was taken off `PATH` and `DOTNET_ROOT` pointed at an empty folder, and the package's own `grouplab.exe` was run from the package to analyse the sample it generates. It found the sheet, registered it and printed the figures, which exercises the runtime it carries and the OpenCV native library both. The packaging script does that check itself on every run, so a package that cannot analyse fails the build rather than a tester. CI's new `windows package` job runs the same script on every push and keeps the zip for 30 days.

**GPL-3.0 section 4 travels with it.** `README.txt` names the version, the exact commit and the public repository the build came from.

### Section 2: samples in the zip

- **`samples/gl-cf25-ltr-300-dpi.png`**, Alan's own printed sheet scanned at 300 dpi, copied from `scans/phase0`, already public in this repository.
- **`samples/sample-25-shots.png`**, generated at packaging time by the new `grouplab sample`, which renders GroupLab's own sheet and shoots at it with a seeded random number generator.

**Nothing donated and nothing without a consent record is in it.** `ReleaseAssetTests` holds the script to that: the only image it copies from the repository is the unshot sheet, and the test fails if a second path into `scans/` appears.

**Why a generated sample at all.** The repository holds no shot GroupLab sheet that may be published: the `scans/phase0` images are print-quality references with no holes in them, and `scans/phase1` is the donated material the entry excludes. A tester who opens the unshot scan sees registration and no figures. That is question 29, raised rather than decided.

### Section 2a: the installer

**Inno Setup**, as the entry suggests, in `packaging/windows/grouplab.iss`: a per-user install with `PrivilegesRequired=lowest`, so no administrator rights, a Start menu entry, an entry in Add or remove programs, and an uninstaller. The uninstall removes the program and leaves `%APPDATA%\GroupLab` alone, and the finish page and `README.txt` both say so by name.

**Built, on the runner rather than here.** Inno Setup is not installed on this machine, so `ISCC.exe` was never run locally: the script warns and builds the zip alone. That is right for a working copy and wrong for a release, so `-RequireInstaller` makes it fail instead, and the release workflow passes it.

**Corrected 2026-09-21, entry 120 section 9.** The report from the run that built this said the installer had never been built. It had: Alan ran the release workflow by hand on commit 5a4cd07 and the draft release "GroupLab 0.1.0, draft" carries `grouplab-setup-win-x64.exe` and its versioned copy, identical by SHA-256, built on the GitHub Windows runner where Inno Setup is installed. He is installing it. **"Never built on this machine" and "never built" are different statements**, and the report made the stronger one.

### Section 3: a release anybody can download

**`.github/workflows/release.yml`**, on a tag and by hand from the Actions tab. A run by hand makes a draft, so a test build is looked at before anyone sees it; a tag publishes.

Every asset is attached twice: once under a name carrying the version and the commit, and once under a stable name, so `https://github.com/oRAirwolf/grouplab/releases/latest/download/<name>` keeps working with nobody editing the README after a release. **`ReleaseAssetTests` holds the README's three download links to the names the workflow and the packaging script actually write**, so renaming an asset fails the build rather than leaving a dead link on the front page.

The macOS build stays out. The 30-day CI artifact stays as it is.

### Section 4: what a tester will hit

`README.txt` in the package and the README's Download section say the same things in the same words: the build is unsigned because signing costs money the project has not spent, Windows will say "Windows protected your PC", the way through is More info and then Run anyway, antivirus may quarantine it and the file it takes is named, GroupLab writes to `%APPDATA%\GroupLab` and nowhere else, uninstalling leaves that folder until the person deletes it, and a problem comes back as the report package the Diagnostics screen already produces, which carries no location data. A test holds `README.txt` to each of those.

### Section 5: the tester's page

**`docs/TESTING-GUIDE.md`** and its PDF, one page: what GroupLab is in three sentences, getting it and the SmartScreen step, the first minute with no rifle, reading what it shows, printing a sheet and shooting it, what is not done yet, and how to report a problem. `grouplab user-guide` now writes both guides, and the guide test holds each to its PDF.

**What is not done yet is taken from the README's Planned section rather than written afresh**, so the page does not contradict the authority on states: the in-app print path is named as fixed but unproven on paper, blank-paper detection and the Xero import as not built, and the gates with no material yet as exactly that.

**Held by a test, and not held by a test.** Both guides are held to their pictures and to their PDFs. Nothing holds the tester's page's "what is not done yet" to the README's Planned section, so the two can drift apart; that is a gap, named here rather than left unsaid.

## Entry 117. A performance phase, the benchmark that measures GroupLab against itself, and the baseline

`docs/NOTES-FROM-PLANNING.md` entry 117, every numbered section. **Nothing was optimised.** Everything wasteful noticed while measuring is a candidate in `docs/PERFORMANCE.md` with its measurement beside it.

### Section 1: where it goes in the plan

**Phase 9, Performance**, in `DESIGN.md` section 21 and the README's Planned section, after the phases that exist. The phase line says it may run alongside Phase 6, Android, and why: a phone is several times slower than a desktop, and an analysis that is merely slow on Windows is unusable there. It starts only when the application works as intended, because a fast wrong answer is worthless.

### Section 2: the gate, which cannot be written yet

**It is not written, and the document says so rather than inventing a number.** `docs/PERFORMANCE.md` carries the gate's shape in the terms a person waits in, per platform, with responsiveness held separately from speed and memory beside the times, and says the threshold will be written from the baseline below.

### Section 3 and 4: `grouplab bench`, and the committed record

**`grouplab bench` takes no file from anybody.** Every case runs against material committed in this repository or generated by GroupLab from its own sheet: the sheet rendered, a 25 shot sample shot at by a seeded generator, Alan's own committed scans and photograph, and a sessions database the benchmark fills itself with 300 sessions. It runs on a bare checkout, on CI, and on a machine that has never analysed a target. One run of each case is thrown away, then five are timed; the figure is the median with the fastest and slowest beside it.

**The stage timings are the stage record's**, not a second timing path: an analysis case runs once and files each stage of the pipeline as a row of its own, so marker detection, registration and hole detection are visible separately rather than inside a total.

**The record is `docs/PERFORMANCE.md`**, written in place by `grouplab bench --record docs/PERFORMANCE.md`, so a change's effect is a difference between two committed tables rather than an opinion. It names the date, the machine, the processor count, the build configuration and the commit. **CI runs the benchmark and does not gate it**, printing the figures to the run summary for information, exactly as the raw gate records are treated.

### Section 3a: it covers everything, and a test proves the list is complete

**`BenchCoverageTests` enumerates every class in GroupLab that does work** and fails when one has no bench case. A thing that can be measured is a static class of methods or a class with public methods of its own; records are data and are measured through whatever computes them. Everything is covered by a case naming it, or sits in a named list with a reason in one line.

**It earned its keep immediately.** On its first run it named 25 things with no case: the review queue, the sentences the screen says about an imperfect sheet, the trace as the console prints it, the target library, chronograph strings, the stability factor, pooling, the range moments, the artwork fingerprint and the warp rasteriser among them. Five new cases were written and the rest were folded into the cases that already exercise them. Three are excluded by name: two pieces of geometry that run under a person's finger, which the interface benchmark measures as controls, and one that is a point on a page.

### Section 3b: every control, from the click to the moment it is finished

**The unit is a control, not a screen, and the controls are found by walking the window.** `ControlWalk` collects everything a person can click on each screen: buttons, toggles, tabs, disclosures, dropdowns and lists, each named by what it says, then by its tip, then by the caption above it. Nothing is listed by hand except the journey to each screen, which is not a list of controls but how the application is got into a state.

**Three times are recorded for every control**, because they answer different complaints: to responsive, which is the click handler's own time on the interface thread and is exactly the freeze a person feels; to settled, which is the moment nothing further is coming rather than the moment something appeared; and to final paint. The layout passes the click caused are recorded beside them, and work that ran on the interface thread is flagged as such, so a control that is fast because it painted nothing is visible as that.

**`ControlBenchTests` fails when a control is neither measured nor excluded by name**, and a second test fails when something is excluded that no screen has, so a stale excuse cannot sit there looking like coverage. The excluded controls are in the record with a line each: the file pickers and the print screen, which wait for a person or use paper, the editor's own window, a sheet duplicate that would write into the person's library, and the clipboard.

**A finding, from building it.** Choosing an item from a single-item dropdown by opening its popup crashes a headless run with a stack overflow, in Avalonia's popup code rather than GroupLab's. The benchmark measures a dropdown by choosing from it, which is the work, rather than by opening the platform's popup.

### Section 5: what Alan is noticing

He says it is "somewhat slow running" and has not said where. Nothing here guesses. Every operation in section 2's list was measured and the answers are in the record; the candidates below are what the measurements name, not what seemed likely.

### What was found, and not changed

- **Hole detection is the analysis.** S5-S8 is about two thirds of a 300 dpi analysis and well over half of a 600 dpi one. Nothing else is close.
- **Finding the bulls costs several times what registering the page does**, and grows faster than the image does between 300 and 600 dpi.
- **Identifying the sheet from its codes costs about half as much again as the marker stage that follows it**, and reads the same page. Entry 115 section 5 saw the same thing at 600 dpi.
- **The CEP table is slower than the whole analysis of a marking that contains it.**
- **A 600 dpi image is decoded twice**, once for grey and once for its strongest channel.
- **Saving a session is mostly writing the marking as JSON**, not the database write.
- **The first trajectory costs about twenty times the next**, so something in the solver is built on first use and a person waiting for a dope table pays it.

## Entry 118. A contents list on the README, held to the headings by a test

`docs/NOTES-FROM-PLANNING.md` entry 118, both numbered sections, and section 3's candidates named without moving anything.

### Section 1: what was added

**A plain bulleted list under a short bold line, "On this page."**, placed after the Download section and before "The problem GroupLab exists to solve". Download stays first: somebody who came to get the program does not read past a contents list to find it.

One level, the `##` headings, in the order they appear, each linking to its anchor. **The single exception is Planned**, whose three `###` children are listed indented beneath it, because that section is 140 of the page's 380 lines. No heading of its own, so the list does not have to contain itself.

### Section 2: it cannot go stale

**`ReadmeTests.TheContentsListIsTheHeadings`** reads the README's own headings and its contents list and fails when they differ: a heading missing from the list, an entry naming no heading, or the two in a different order all land as one difference. It also holds the list where the entry puts it, after Download and before the section that follows it.

**The anchors are derived in the test from the heading text** by GitHub's rule, lower case with punctuation dropped and spaces hyphenated, rather than read from the list, so a renamed heading fails the test instead of leaving a link that scrolls nowhere. **Two headings that would generate the same anchor fail with both names**, rather than the test guessing at GitHub's numbering.

**Checked by breaking it.** One entry was deleted from the list and the test failed with the difference; the entry was put back and it passed.

### Section 3: what looks like a document of its own

**Nothing was moved and no section's content was touched.** Named as candidates, with their share of the page:

- **Planned, 140 lines of 380.** It is the authority on states, `ReadmeTests` holds it to `DESIGN.md` section 21, and the planning session reads it. It is a document by any measure, and moving it is the only change that would shorten the page materially. It is also the one with the most attached to it, so it is a decision rather than a tidy.
- **Architecture, 39 lines.** The projects, the layering and the imaging backend. `DESIGN.md` already carries the reasoning; this is the map, and a map belongs beside the reasoning rather than on the front page.
- **Repository layout, Test data and Building, 35 lines together.** Three sections that are one thing: what a person who has cloned the repository needs. They read as a contributor's page, and the front page could keep a line pointing at it.
- **Concept screens, 20 lines.** A gallery of six renders. It sells the idea well, so it earns its place; it is named here only because it is the next largest.

**The contents list treats the symptom**, as the entry says. The length is the complaint, and the list makes the length navigable rather than smaller.

## Entry 119. Nightly builds that publish themselves, and an updater with three trains

`docs/NOTES-FROM-PLANNING.md` entry 119. **Sections 1, 2, 3, 5, 7, 8 and 9 are built; section 6 in part; section 4's mechanism is not built and section 10 could not be run.** What is missing is named here and in the status line, not buried.

### Section 1: versions

A build knows three things, all stamped in at build time: its version, its train and its commit. The train is an MSBuild property that becomes assembly metadata, so only a publishing workflow sets it, and a build made on somebody's own machine keeps `development` and never offers to update itself. A nightly is `<Version>-nightly.<N>` where N is the nightly workflow's own run number, which only ever increases.

`SemanticVersion` is strictly SemVer, because it also reads tags and a type called SemanticVersion that is not one would be a trap.

**The ordering the entry asks for is not the ordering SemVer gives**, and that is question 30. SemVer compares pre-release identifiers as text, so `beta` sorts below `nightly`; the entry's own example, and section 4.5's rule that a nightly user may take a newer beta, both need the opposite. `UpdateOrder` ranks the trains by how steady they are and is what the updater uses. Both facts are tests, so nobody later folds one into the other by accident.

### Section 2: a nightly after every green push

`nightly.yml` follows `build and test`, runs only where that run succeeded on `phase-1` or `main`, and builds the run's own head commit rather than the branch head. Two pre-releases per build: `v<version>`, which keeps the versioned assets so a bug report names something that still exists, and the rolling `nightly`, which keeps the stable names and whose addresses never change. It keeps the newest thirty and deletes older nightlies with their tags, matching only tags of the shape its own builds make, so a beta, a release or anything else is left alone.

The packaging is `package.yml`, which `release.yml` calls too. A test fails if a third workflow starts building downloads of its own.

### Section 3: the manifest

Every build publishes a signed `update-manifest.json`: version, train, commit, publish time, the notes, and for each platform the asset's name, size and SHA-256. One fixed address per train, which GitHub serves without its API, so a check is one small request and cannot meet a rate limit. A check sends nothing about the person: a plain GET with a User-Agent naming GroupLab and its version.

**The algorithm is ECDSA P-256 with SHA-256, not Ed25519**, and that is question 31. .NET 10 has no Ed25519, and this machine has no NuGet source configured at all, so no package can be restored to provide one. Every manifest names its algorithm, so a later move to Ed25519 is a manifest older builds refuse by name rather than a silent substitution.

**No key, no publishing**, in three places: the workflow stops before it builds anything when the secret is missing, `grouplab update-manifest` refuses to write an unsigned manifest, and a build whose compiled-in public key is empty installs nothing and says why.

### Section 4: the updater in the application

**What is built:** the rules, away from the network and away from the window, and tested as rules. Check intervals, the train rules, skip and later, signature refusal, hash refusal, a truncated download, a manifest from the wrong train, a manifest from a newer format, and a development build that never offers to update itself.

**What is not built: the mechanism.** Downloading in the background, running the installer silently, closing and reopening on the same screen, and proving an open session survives it. That is the half of section 4 that needs the installer, a real download and a real relaunch, and it is not started. **Until it is, nothing in the application installs anything**, which is the safe state to leave it in: the settings page can look and can say what it found.

### Section 5: notes for every build

`scripts/release-notes.py` writes each build's notes from the commits since the previous nightly: the first line of each, grouped under New, Fixed and Changed by a rule written at the top of the script, with merge commits and notes-folding commits skipped and trailers removed. Before publishing it checks the notes against the repository's own rules and fails rather than publishing: no em dash, nothing from the private range folder or a submission, no coordinates, no server address.

### Section 6: the settings page

**Built:** "This build" names the version, the train and the commit, selectable, with a link to the repository. "Updates" offers the three trains, with Release and Beta shown and refused with "Not available yet", the four check intervals, Check now, and the one-sentence privacy note.

**Not built:** section 6.4's renders of the settings page at 1280 by 720 and 2560 by 1440. The library's renders at those sizes are in `docs/figures/screens/current/` from entry 120 section 10; the settings page's are not.

### Section 7: the README

One table, the latest build, linking only to `releases/download/nightly/<stable name>`. No release table and no mention of `releases/latest` until Alan asks for the first release. `ReleaseAssetTests` holds exactly that, and fails if `releases/latest` or `v0.1.0` appears.

**Those three links return 404 today**, checked over HTTP: the rolling `nightly` release does not exist, because nothing has been published, because the signing secret does not exist. **The front page therefore offers a download nobody can take**, which is worse than the state before this entry in one respect and better in another: before, the links pointed at whatever was last tagged, which was nothing at all until `v0.1.0` and is now a pre-release the trains deliberately ignore. They begin working the moment the first nightly publishes, which is the first green push after the secret is set, and nothing else has to change for that to happen.

### Section 8: tagged releases stay deliberate

Nothing here pushes a `v*` tag. The nightly workflow tags its own pre-releases and nothing else; `release.yml` is unchanged in that respect, and the draft made by hand on 2026-09-21 is left for Alan.

### Section 9: tests without the network

Twelve rules, each against a manifest the test signs with a key the test makes, so a run needs no network and no secret and a signature that should fail can be made to fail on purpose. The save, close, install and reopen sequence is not among them, because the mechanism it would drive is not built.

### Section 10: prove it

**Nothing was published, because the signing secret does not exist.** `gh secret list` returns nothing for this repository. Section 10.6 says to stop there, report the steps and leave publishing to the next run, and that is what was done. The exact steps are in the report and in `docs/UPDATES.md`.

## Entry 120. The second range day, run through GroupLab

`docs/NOTES-FROM-PLANNING.md` entry 120. Six 600 dpi scans and 59 photographs, none of them in this repository except scan 3, which is published under the consent record in `samples/PROVENANCE.md`. Every count below marked "holes" is GroupLab's reading; every count marked "shots" is Alan's own account from the entry's ground truth table.

**How they were run.** Each scan was opened by the application's ordinary Open path, driven headlessly the way the window rehearsal tests drive it, so what is measured is what a person would see: the same `OpenImage`, the same detection, the same review queue, the same Accept and analyse. Renders of every result were written outside the repository and looked at hole by hole against the scan.

### Section 1: every scan, against the ground truth

| Scan | Sheet, identified | Shots (Alan) | Holes (GroupLab) | Assigned | Open to first result | To final result |
|---|---|---|---|---|---|---|
| 1 | GL-CF25-LTR-D, from its codes | 15, bulls 1 to 15 | 14 | one each on bulls 1, 3 to 15 | 20.3 s | 22.1 s |
| 2 | none: codes unreadable, offered by name | a zero group | 0 | none | 8.5 s | 8.6 s |
| 3 | GL-CF25-LTR-D, from its codes | 25, one per bull | 25 | one each on bulls 1 to 25 | 23.9 s | 26.0 s |
| 4 | GL-CF25-LTR-D, from its codes | 23 | 19 | 19 bulls, 5 refusals named | 13.7 s | 15.7 s |
| 5 | GL-CF25-LTR-D, from its codes | 20, bulls 2 to 5 of each row | 18 | 18 bulls, mostly the wrong ones | 19.0 s | 21.0 s |
| 6 | GL-CF25-LTR-D, from its codes | 10, bulls 1 to 10 | 9 | bulls 1 to 5 and 12 to 15 | 14.2 s | 16.3 s |

**Scan 3 is exactly right:** 25 holes, one on each of 25 bulls, which is the shooter's account to the shot. That is why it is the published sample and why the package's self-test holds the package to it.

**The load block was never read and never offered.** GroupLab does not read handwriting and does not ask for the load block's contents on opening a sheet; the load is entered on the records. Nothing written in the block became a hole on any sheet: the detector reports "hole-sized candidates inside printed-matter zones not looked at", 4 on scan 1, 7 on scan 4, 2 on scan 5, 0 on scans 3 and 6, and the thick marker crossing the rules produced none.

**Missed holes, by eye against the render:**

- **Scan 1, bull 2.** A clear hole, no candidate raised anywhere near it, and nothing said: the review queue was empty. GroupLab reported 14 on 25 bulls and a person who fired 15 has no prompt.
- **Scan 4, five refusals.** Bulls 2, 10, 11 and 21 twice, each "a 0.11 to 0.14 in candidate there was refused: too small". Naming a calibre does not rescue them: re-run with `.223` the detection says "with the calibre .223 in, whose holes were taken to measure 0.211 in" and still finds 19 and still refuses the same five. A .22 LR hole at 100 yards reads far smaller than the size gate expects.
- **Scan 5, two holes.** Both outside the bull grid: one above bull 2 near the top edge, one left of bull 12. A third mark at the top left corner beside the QR code was not detected and may be a scanner artefact rather than a hole.
- **Scan 6, shot 6.** At the extreme left edge beside bull 21, partly cut by the scan's crop. Alan's account names it: "aimed at bull 6, landed left of bull 21, much lower than the rest, cause unknown. It is a real shot."

**False holes: none.** No handwriting, no blue ink, no scanner dust and no paper edge became a hole on any of the six.

### Section 2: holes between bulls

**What GroupLab does today.** It matches one-to-one where the sheet declares one shot per bull, so with fewer holes than bulls it matches against a subset, and each hole goes to the bull that makes the total distance smallest. On scan 5 that is wrong in a particular, systematic way: every shot is high and left of its aim point by about a bull's spacing, so hole after hole is nearer the bull up and left of the one aimed at. GroupLab put 18 holes on bulls 1 to 9, 12 to 19 and 22, when the shooter aimed at bulls 2 to 5 of each row. Bulls 1, 6, 11 and 16, which were never aimed at, hold shots.

**Is it producing groups that are wrong? Yes, and in the worst way: quietly.** Each shot's offset is measured from a bull it was not aimed at, so the composite group looks tight and centred when the rifle is in fact shooting about a bull's spacing high and left. The zero correction, which exists to say what to dial, would say there is nothing to dial. Seven review items were raised on scan 4 and eight on scan 5, each naming a contested hole, so the queue is not silent; but the figures that follow Accept do not carry the doubt.

**The proposal, not built.** A sheet-wide offset before assignment: find the one translation that, applied to every hole, makes the total distance to bulls smallest, then assign each hole to its nearest bull in that shifted frame, and report the offset as the sheet's point of impact.

Tested on paper against Alan's ground truth:

- **Scan 5, right.** One offset for the whole sheet is exactly what happened: one load, one zero, every shot displaced the same way. A common offset of about a bull's spacing up and left recovers bulls 2 to 5 of each row and leaves the left column empty, which is the truth.
- **Scan 4, wrong as stated.** The windage was changed after row 2, so there are two offsets, not one. A single offset would split the difference and misassign both halves. It works if the shooter can say "rows 1 and 2, then rows 3 to 5", which is a second thing to enter.
- **Scan 6, wrong, and interestingly so.** Two loads with different points of impact on one sheet: bulls 1 to 5 low on their own bulls, bulls 6 to 10 landing a whole row lower. One offset for the sheet is meaningless; one offset per subgroup is right, and GroupLab already knows which bulls are in which subgroup once the loads are set. Shot 6, which landed far from everything, defeats any offset rule and must stay a hole the shooter places by hand.

**What the shooter would have to tell GroupLab** for the offset rule to work: how many shots were fired, which bulls were aimed at, and where the sheet is divided into groups that share a point of impact. That is three things, and two of them are already in the application: shots per bull, and the load per bull from entry 115 section 2.

**What must be true whatever is chosen:** GroupLab must say on screen that an assignment is uncertain, let a hole be moved to another bull by hand, and never present a group statistic built on an uncertain assignment as if it were certain. Today it does the first two through the review queue and fails the third.

### Section 3: holes outside the grid

**Scan 3 has none.** The marks the planning session saw "in the left margin beside bulls 11 and 16" are the shots for bulls 11 and 16, landing high and left of their bulls but inside the sheet; GroupLab assigned both correctly, and the sheet reads 25 on 25.

**What GroupLab does with a hole that is on the sheet and near no bull:** it keeps it, counts it as unassigned, and raises a review item saying "Shot N has no bull, so it is left out of the group", offering its nearest bull, "Not a shot", or leaving it without one. It is never silently dropped and never pulled into a bull's group. That is the behaviour the section asks for, and it is already there.

### Section 4: the blank zero sheet

Scan 2 has no codes and no markers. GroupLab did not guess: it offered the sheet by name from the library, exactly as entry 115 section 4 asks. Choosing a definition then fails honestly, because the sheet has none of that definition's markers.

**The scale: GroupLab asks, and takes nothing by itself.** The status line says "Set a scale before the group can be measured: a reference length, a reference rectangle, or a GroupLab sheet's markers". It does not offer the scan's own resolution, **although the file states it**: the PNG carries a `pHYs` chunk of 23622 pixels per metre, which is exactly 600 dpi, and the application reads that chunk elsewhere. **Proposal, not built:** offer the stated resolution as a scale on a blank sheet, with the number shown so a person can refuse it, since a flatbed scanner's stated resolution is as good a reference as a ruler.

**A defect found here and fixed.** After choosing a sheet by name, the failure showed the pipeline's own words, "0 of 34 markers found; registration needs 4", run together with the next sentence and with no full stop between them. The cause was that a failing result was built without its definition, so entry 115 section 4's advice could not be asked for. The definition now travels with a failure and the panel ends one sentence before starting the next, with a test built from a generated blank page.

The group centre against a hand-drawn cross is only as good as the drawing, and nothing in GroupLab claims otherwise: with no scale it measures nothing at all.

### Section 5: photographs against scans

**Partly done, and the part that is done is reported here.** One photograph from each burst was run through the analysis; the burst-by-burst pairing against Alan's own reading, hole by hole in inches, is not finished.

| Burst | What it is (Alan) | What GroupLab made of one frame |
|---|---|---|
| 11:06 | Unshot sheet printed before the entry 114 fix, no codes, no markers | Refused by name: "no code on the sheet could be read; name the sheet's definition". No crash and no guess, which is what the section asks |
| 14:14 | Unshot sheets on the board | Identified from 1 code at full resolution, 34 of 34 markers, 0 holes. The board's own holes around the sheet became nothing |
| 15:33 | The sheets after shooting | Identified from 2 codes, 31 of 34 markers with 5 unexpected, 26 holes, mean radius 0.475 in |
| 16:15 | The blank sheet and the orange target | No code readable; the by-name path |
| 16:56 | The zero sheet after shooting | No code readable; the by-name path |
| 18:59 | The commercial target | No code readable; the by-name path. It was never matched to a GroupLab definition |

**The 15:33 frame found 26 holes where the scan of that sheet found 25**, and 5 markers "unexpected", which is the sign of another sheet in frame. That is the several-sheets case and it is not resolved: GroupLab picks one sheet, the one whose codes it read, and counts holes inside that sheet's registered page; it does not ask which.

**Not done:** the per-photograph pairing tables, the agreement in inches against each scan, the 14:14 burst's angle-by-angle identification report, and what the blank-sheet path makes of the two frames with a tape measure in them. The submission `2026-09-21_86926341` was read in place and nothing in it was altered, run through intake, or published; its ten photographs are the scan 6 sheet, with consent agreed and 6mm Creedmoor at 100 yards recorded in its `meta.json`.

**No GPS or location metadata was read, printed or logged** from any photograph. The copies looked at were made with GroupLab's own `scrub`, which strips location data, and the metadata printed by the analysis is format, size, camera make and model and lens group, which is what the application already logs.

### Section 6: timing

On this machine, through the command line so the figures are comparable with entry 115 section 5, all six scans at 600 dpi:

| Scan | Decode | Identify | Markers | Bulls | Holes | Total |
|---|---|---|---|---|---|---|
| 1 | 731 ms | 1501 ms | 115 ms | 1215 ms | 4266 ms | 8016 ms |
| 3 | 680 ms | 1244 ms | 108 ms | 959 ms | 4782 ms | 7980 ms |
| 4 | 683 ms | 903 ms | 111 ms | 984 ms | 3026 ms | 5922 ms |
| 5 | 664 ms | 1599 ms | 111 ms | 1069 ms | 3905 ms | 7549 ms |
| 6 | 674 ms | 1009 ms | 106 ms | 893 ms | 3309 ms | 6173 ms |

**Identification is 15 to 20 percent of the total here, against the 55 to 60 percent entry 115 section 5 measured.** The difference is that these sheets' codes read at full resolution on the first try; entry 115's sheet needed downscaling passes. Hole detection is half to three quarters of every one of them, which is what `docs/PERFORMANCE.md` already names as the first thing the performance phase should look at.

**The application is two to three times slower than the command line on the same scan:** 13.7 to 23.9 seconds to first result against 5.9 to 8.0. The application keeps every stage's picture for the timeline and builds the display image; the command line keeps none. That is a candidate for the performance phase, recorded and not changed.

### Section 7: compare loads

**Scan 6, the real primer comparison.** GroupLab assigns the CCI BR-4 shots to row 3's bulls, 12 to 15, not to bulls 6 to 10 where they were aimed, because they landed a whole row low. To put that right a shooter must move four holes by hand, one at a time, from the review queue or the editor; there is no "these shots belong to those bulls" for a whole row.

Setting the loads per bull as entry 115 section 2 allows, GM205MAR on bulls 1 to 5 and CCI BR-4 on the bulls that hold its shots:

| Subgroup | Bulls | Shots | Mean radius |
|---|---|---|---|
| GM205MAR | 1 to 5 | 5 | 0.168 in |
| CCI BR-4 | 12 to 15 | 4 | withheld, fewer than five shots |
| Comparison | | | dispersion p 0.434, centre p 0.001 |

**What that says, plainly.** The centres differ, strongly: p = 0.001 is the point-of-impact shift Alan can see with his own eyes. The dispersions do not differ detectably: p = 0.434 from five shots against four, which is no evidence either way rather than evidence of sameness. **GroupLab withholds the four-shot mean radius rather than printing one**, which is exactly right, and it draws no verdict about which primer is better.

**The one caveat, and it matters:** those four BR-4 shots are measured from row 3's bulls, not from bulls 6 to 10 where they were aimed, so the shift the comparison reports is the shift relative to whatever bull each shot landed near, not the true shift of about one row. The number is real and it understates. Moving the shots by hand first would give the true figure.

**Scans 1 and 3 as one load across two sheets: not possible today.** A session is one sheet. Two sheets of the same load cannot be pooled into one group of 40, and the comparison screen compares sessions rather than pooling them. **Proposal, not built.**

**Correcting a load block after the fact:** the load is entered on the records and on the session, not read from the sheet, so correcting scan 3's primer from GM205MAR to 7.5BR is editing the session's load, which is kept with the session. The sheet's own handwriting is not read and is not corrected; the published sample carries the mistake and `samples/PROVENANCE.md` records the correction beside it.

### Section 10: the target library uses the whole window

Built, with its own results written above in the commit that made it: the list column wide enough for the longest name and its paper and bulls with nothing cut or overlapping, resizable with a remembered width, names wrapping rather than cutting; the sheet taking everything the list does not, with the preview filling it and growing with the window; Zoom in, Zoom out and Fit under it; and the status line saying what is on this screen. Renders at 1280 by 720 and 2560 by 1440 are in `docs/figures/screens/current/`.

**Section 10.4's own measure could not be held**, and question 32 asks for it to be confirmed: a portrait page fitted to the full height of a landscape window is 9 percent of a 1280 by 720 window and 22 percent of a 2560 by 1440 one, and no layout reaches half without cropping the page or stretching it. What is held instead is that the preview fills the room it is given and that the room is everything the list does not take.

## Entry 121. v0.1.0 was published, so the nightly train starts at 0.2.0

`docs/NOTES-FROM-PLANNING.md` entry 121.

### Section 2: what was done

**The version is 0.2.0.** With it at 0.1.0 every nightly would have been `0.1.0-nightly.N`, which sorts below the published `0.1.0`, so a nightly user's updater would have offered the release as an upgrade to older code and the nightly train would never have looked newer than anything public. `VersionTests.TheBuildsVersionIsAboveEveryTagAlreadyPublished` reads the tags in the clone and fails if the version in `Directory.Build.props` is not above every non-pre-release one, so it cannot happen twice.

**`v0.1.0` is left alone.** No manifest is published for it, no train points at it, the tag and the release are untouched, and nothing in the repository links to it or to `releases/latest`; `ReleaseAssetTests` fails if either appears. Alan marks it a pre-release himself when he chooses.

**No `0.1.0-nightly.N` was ever published**, because the nightly train has published nothing at all: the signing secret does not exist, so the workflow stops before it builds.

**The workflow built for entry 119's earlier draft was named "test build"** and appeared twice in the Actions list, both runs skipped because the CI run they followed had failed. It is now `nightly.yml`, it is the only workflow of its kind, and the two skipped runs are history.

### Section 3: why CI on main was red

**It was mine, from the commit before it.** `ReleaseAssetTests` read a workflow's name with the regular expression `^name: (?<name>.+)$` under `RegexOptions.Multiline`. On a checkout with CRLF line endings, which is what a Windows runner gets and what a Windows working copy has, `.` matches the carriage return, so the captured name was `build and test\r` and the test then looked for a workflow following a workflow of that name. Linux and macOS check out with LF and passed; Windows alone failed, which is why the same commit was green on two platforms and red on the third.

The expression is now `[^\r\n]+` and the comment says why, because the next person to write one will meet the same thing. **Nothing was published from that commit**, which is the rolling build's own rule working: its job was skipped because the run it follows did not succeed.

### Section 4: what can be proved and what cannot

- **The newest nightly: there is none.** Nothing has been published on the nightly train.
- **`releases/latest` returns nothing at all**, checked with the API at 2026-09-21: Alan has marked `v0.1.0` a pre-release, so there is no release for it to point at, which is what entry 121 section 2.2 asked for. The README links only to the rolling `nightly` release in any case. The draft `v0.1.0-draft` made by hand is still there with its six assets, left for Alan as entry 119 section 8 says.
- **The updater cannot be shown refusing `v0.1.0`**, because there is no manifest anywhere for it to check against. What can be shown, and is, is that the rule holds in the tests: a build on 0.2.0 offered a 0.1.0 manifest refuses it as not newer.

## Entry 122. The tests opened GitHub in Alan's browser

`docs/NOTES-FROM-PLANNING.md` entry 122. **The fault was mine, made in this run**, and it is the same class as entry 114's print to the OneNote driver: a test reaching out of the process and into the person's own applications.

### What happened

Entry 119 section 6.1 asks for a link to the repository on the settings page. The button called a private helper that started the address with `UseShellExecute = true`, which is the real default browser. Entry 117 section 3b's control walk clicks every control it finds on every screen, and the settings screen is one of the screens it walks, so **every run of the interface benchmark opened a tab on whatever machine it ran on**. Three runs today, three tabs.

### What is built

**One way out of the process.** `IOutsideWorld` has three members, opening an address, a file and a folder, and one real implementation. `TheOutsideWorld.Current` is what the application uses; a test replaces it. The application tests install a recorder from the module initialiser, so it is in place for every test in the assembly rather than for the tests that remember.

**Three call sites moved behind it:** the link to the repository, the crash record's "show me the folder", and opening a saved PDF to print.

**A guard.** `OneWayOutTests` reads every source file and fails if anything outside `OutsideWorld.cs` starts a process with the shell or uses a launcher API. It caught a second case as it was written: the print screen's `PrintLaunch` still built a shell start, even though nothing ran it any more, so it now names the file and leaves opening to the one way out. `PrintScreenTests` still holds what a person is told.

**Measured rather than excluded.** The walk clicks these buttons against the recorder, and `OutsideWorldTests` checks what each asked for: the repository link asks for that one address, the support placeholder asks for nothing, and Check now asks for nothing while there is no key.

### The count, before and after

| | Real browser openings | Other launches | Network requests |
|---|---|---|---|
| Before | one per full run of the application tests | none | none |
| After | zero | zero | zero |

**Zero by construction rather than by counting.** The recorder is in place for the whole assembly, and the guard test means no other code path can start anything. The update check has no implementation yet, so it makes no request; when it is built it goes behind the same interface, and that is written down in the entry's status line rather than left to be remembered.

### What is not behind it yet

Named rather than implied: the print device, which entry 114 already keeps behind a fixed allowlist of drivers that write a file silently; the update check's HTTPS request, which does not exist yet; and the installer, which is entry 119 section 4's unbuilt mechanism. Each goes behind this interface when it is built.

## Entry 123. The 403 tested rather than assumed, and the updater's missing half

### 1. The 403 was the repository default, and the setting is required

Entry 123 section 1 asked for the next nightly to be treated as the test of the diagnosis rather than the fix. It was, and the diagnosis held.

| | before the setting change | after it |
|---|---|---|
| run | <https://github.com/oRAirwolf/grouplab/actions/runs/35572294871> | <https://github.com/oRAirwolf/grouplab/actions/runs/35573031594> |
| commit | 862aab2 | 862aab2, the same one |
| `default_workflow_permissions` | `read` | `write` |
| "Publish this build" | `HTTP 403: Resource not accessible by integration` on `POST /repos/oRAirwolf/grouplab/releases` | success |
| "Move the nightly release" | never reached | success |
| published | nothing | `v0.2.0-nightly.12` and the rolling `nightly` |

Nothing else changed between the two runs: same commit, same workflow file, same secret, same assets. That is as close to a controlled experiment as this gets, and it settles the question entry 123 section 1 raised about the workflow-level `permissions: contents: write` block and about `release.yml` having succeeded under the same default.

**Why the workflow block was not enough.** A workflow's `permissions:` can only narrow what the repository default already grants; it cannot raise it. A workflow asking for more than the default is given the default, and the job log's token summary prints **what was asked for**, not what was granted. That is why the first diagnosis could only ever be a guess: the log said `Contents: write` while the token held read. It is recorded in `docs/UPDATES.md` under "What the workflow needs from the repository", with both run URLs, so the next person to see a 403 does not repeat the reasoning.

No ruleset, tag protection or `GH_DEBUG=api` evidence was needed, because section 1 step 3 is conditional on the publish still failing. No other repository setting or permission was touched.

### 2. The mechanism: the Inno Setup installer, run silently

Entry 119 section 4.6 offered two: the existing installer run silently, or a maintained framework that meets every requirement and needs no administrator rights. **The framework was not a choice that could be made: this repository has no NuGet source configured**, so no package can be added to it at all. That is a fact about the build, not a view about any framework, and it is stated that way in `docs/UPDATES.md` rather than dressed up as a preference.

On its own merits the installer is still the right answer here:

- It already exists and is built by `package.yml` on every commit, so it is the same file a tester downloads by hand. One artefact, one code path.
- `PrivilegesRequired=lowest` means a per-user install, so nothing asks for administrator rights, which was a requirement rather than a preference.
- Its silent switches are documented and stable. `/relaunch=yes` is read by a five-line `[Code]` function in `packaging/windows/grouplab.iss`, which is the whole of the new installer code.
- Nothing new has to be trusted between a signed manifest and the files on disk.

**Off Windows it does not apply.** The zip and the tarball were unpacked wherever their owner chose, and GroupLab does not write over a folder it did not make, so `UpdateAssets.CanInstallItself` is false there and the bar points at the download instead of offering to install.

### 3. What was built

**`UpdateRun` (Core) takes an update from a check to a verified file and stops.** It reads the manifest through `IOutsideWorld`, decides with the rules `UpdatePolicy` already held, downloads the asset for this platform with progress, checks SHA-256 and length against the manifest, and hands back the file and the switches. **It never starts anything.** That separation is the reason the whole thing is testable and the reason nothing can install itself behind a person's back.

**The bar (App), under the header, never a dialog.** Update now, Later, Skip this version, What changed; then the share downloaded with a Stop beside it; then Install and restart. Skip silences one version until something newer appears, and is remembered in `settings.json` along with the train and the interval, which entry 119 had left in memory. A check on launch happens by default and says nothing when it finds nothing.

**The install sequence, in this order:** save the session, write down what version this was and what screen the person was on, say in one line that GroupLab will close and reopen, start the installer through `IOutsideWorld` with `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /relaunch=yes`, close. The installer's new `[Run]` line brings GroupLab back, and the new build reads the note, says "GroupLab updated from A to B" with a link to the notes, returns to that screen, and clears the note so it is said once.

**A file that does not match is deleted, not kept.** A stopped download and a dropped connection both leave nothing behind, and the next attempt starts from nothing rather than resuming into a part file, because a half file with the right name is exactly what would pass a length check by accident.

**The check goes through `IOutsideWorld`**, which closes the gap entry 122 named and left open: `GetTextAsync`, `DownloadAsync` and `StartInstaller` joined the interface, and `OutsideWorld.cs` moved to `src/GroupLab.Core/Updates/` so Core can reach it. `OneWayOutTests` guards the new path as it guarded the old.

### 4. If a new build will not start

**It cannot roll back, and `docs/UPDATES.md` says so.** The installer writes over `{app}`; the previous build's files are gone once it has run. What is guaranteed is narrower and worth more: `%APPDATA%\GroupLab` is never touched, so no session, sheet, setting or log is at risk; every nightly keeps its own `v<version>` release, the newest thirty; and going back means downloading that build's installer and running it over the broken one.

Keeping the previous install aside so a failed start could roll itself back costs a second copy of the program on disk and new untested installer code. It is **question 33**, with the three options and their costs, rather than a decision taken quietly here.

### 5. The tests, none of which touch a network or start an installer

15 new tests: 9 in `UpdateRunTests` (Core) and 6 in `UpdateBarTests` (App). The recorder answers the check and the download and writes down the installer it was told to start.

| what | where |
|---|---|
| a whole update to a checked file and the silent switches | `UpdateRunTests`, `UpdateBarTests` |
| hash mismatch: deleted, said so, nothing started | both |
| a download that arrives short | `UpdateRunTests` |
| stop part way through: nothing kept | both |
| a dropped connection, then a clean second attempt | `UpdateRunTests` |
| a manifest signed by a stranger: refused before anything is downloaded | `UpdateRunTests` |
| a development build asks the train nothing at all | `UpdateRunTests` |
| a platform with nothing to install is told so | `UpdateRunTests` |
| Skip silences one version and not the next | `UpdateBarTests` |
| **save, close, install, reopen on the screen they were on** | `UpdateBarTests` |

The last one is entry 123 section 2.6's sequence end to end: it drives the window to Install and restart, checks the session was saved and the one line said, reads the installer path and switches off the recorder, closes, opens a newer build against the same settings file, and checks it says what it updated from, lands on the library screen the person was on, and says it once and not twice.

The window gained two seams for this, `MainWindow.ThisBuild` and `MainWindow.TrustedKey`, because a working copy is a development build that trusts Alan's key and so would refuse every manifest a test could sign. Nothing in the application writes to them.

## Entry 124. Two questions closed, and a stale download link found while closing one

### 1. Question 33, rollback: A now, B on a named trigger, C never

Entry 124 section 2 took the recommendation. `docs/UPDATES.md` now carries the trigger under "When this changes": the previous install is kept beside the new one, with a "Roll back to <version>" shortcut, at the first of the beta train opening or Alan saying a second person is testing. Proving the new build starts before the old one is removed is recorded as not to be built.

The "If a new build will not start" paragraph is now also in `docs/TESTING-GUIDE.md`, under a new "It keeps itself up to date" section, because that is where a tester looks rather than in a document about how updating works.

**Found while writing it:** the testing guide's download step still sent people to `https://github.com/oRAirwolf/grouplab/releases/tag/test-build`. That tag was replaced by the nightly train in entry 119 and does not exist, so the guide's first instruction led to a 404. It now points at the nightly release and says that every nightly also keeps a `v<version>` release of its own, so a build named in a report still exists later.

### 2. Question 32, the library preview: the figure was real, and so was the test

Entry 124 section 3 offered two possibilities: either the preview really renders at 240 by 340 and the 95 percent test should be failing, or the figure is a slip in the question's text. **It is neither.** Measured by `LibraryLayoutTests`, which prints these on every run:

| | 1280 by 720 | 2560 by 1440 |
|---|---|---|
| preview drawn | 242 by 342 | 750 by 1062 |
| room it was given | 337 | 1057 |
| share of the window | 9 percent | 22 percent |

So the figure is right, and the 95 percent test is passing correctly, because it asks whether the preview fills **the room it is given**, not whether it fills the window. 342 in 337 of room is a preview doing everything it can.

The 450 to 550 pixels entry 124 expected would need the page to get most of the window's height after chrome. It gets 342 of 720, because a fixed 378 goes elsewhere and none of it grows:

| | at both sizes |
|---|---|
| everything above and below the split: header, status line, the screen's heading block, margins | 210 |
| the chosen sheet's detail block: name, summary, identifier and file, the read-only line, Print and Duplicate | 129 |
| the zoom row under the preview | 39 |

378 of 720 is 53 percent of a small window and 26 percent of a large one, which is the whole of why the page looks right on one and cramped on the other. Nothing was changed on the strength of it: making the page bigger at 720 means taking room from one of the two blocks of prose, and which of those a person needs less is a design question, not a measurement. The budget is recorded as question 32 section 5 and in the test's own comment, so the next person to ask has the answer without re-deriving it.

## Entry 123 section 2.7. The real update test, and what it found

The real test was worth every minute it took. **It found that the updater could not install anything at all, and had already shipped that way.**

### 1. What happened, in order

| step | what was seen |
|---|---|
| Install `v0.2.0-nightly.16` on this machine | Silent, 8.5 seconds, exit code 0. **No installer window and no elevation prompt**, as entry 123 section 2.3 requires. It upgraded the existing install in place and registered as `GroupLab 0.2.0-nightly.16` in Add or remove programs. |
| Check the installed build's stamp | `train: nightly`, `version: 0.2.0-nightly.16+b39b7af`. Entry 125's defect is fixed in a published artefact. |
| Ask the installed build to read its own train's manifest | **`Refused: BadSignature`.** |

That is where the walkthrough stopped, because there was nothing further to walk through: a build that refuses its train's manifest never offers an update, never downloads one and never installs one.

### 2. Why, and why nothing had caught it

The bytes that get signed were indented JSON. From .NET 9 the JSON writer's newline follows `Environment.NewLine`, so the same manifest serialises with a carriage return on Windows and without one on Linux. Measured on the two files:

| | line breaks in the manifest |
|---|---|
| written by the Linux runner, published | `\n` |
| written by the same code on this Windows machine | `\r\n` |

The nightly is signed on a Linux runner. Every GroupLab on Windows recomputes those bytes to verify, gets different ones, and refuses. So **every manifest the project has ever published was unverifiable by every Windows build of it.**

CI did not catch it because CI signs and verifies on the same Linux runner, where the two agree with each other. No unit test caught it for the same reason: a test signs and verifies in one process on one machine. **Both halves were consistently wrong together**, which is the failure mode a round-trip test cannot see, and the exact reason entry 123 section 2.7 asks for a real install on a real machine.

It is also why the earlier evidence looked so convincing. The nightly workflow's own `update-check` step passed on every run, printing a green tick beside a manifest no user could verify.

### 3. The fix

The signature is now over compact JSON, which has no line break to differ over. The file a person downloads is still indented, and that no longer decides anything.

`SignableBytesTests` holds the bytes rather than the round trip, which is the only kind of test that could have caught this:

1. the signed bytes carry no carriage return and no line feed at all;
2. they are exactly a recorded string, so changing them is a deliberate act (every manifest signed before such a change is refused after it, and the other way round);
3. a signature still verifies through the written file;
4. a manifest verifies whether the file it arrived in has `\n`, `\r\n` or `\r` line endings, which is the case that was broken.

### 4. What it cost, honestly

`v0.2.0-nightly.12`, `.14` and `.16` are installed on at least two machines and none of them can update itself: `.12` and `.14` because they call themselves development builds (entry 125), and `.16` because it refuses the signature. Every one of those copies has to be replaced by hand once a good nightly exists. The fix cannot reach them, because reaching them is the thing that is broken.

That is the plainest possible argument for entry 123 section 2.7 existing at all. Three nightlies were published, each one passing a green workflow, and not one of them could have updated itself.

## Entry 123 section 2.7. The real update, done

`v0.2.0-nightly.25` to `v0.2.0-nightly.26`, on Alan's machine, driven through UI Automation so every press was a real click on the real window rather than a harness standing in for one.

### 1. What happened, step by step

| time | what was seen |
|---|---|
| 11:46:23 | `GroupLab 0.2.0-nightly.25` starts, installed silently 12.3 s earlier with no window and no elevation prompt |
| 11:46:24 | `update.check result=Offered refusal=None`. **It found the newer build on its own, on launch, with nobody asking.** |
| | The bar reads: **"GroupLab 0.2.0-nightly.26 is ready to install."** with Update now, What changed, Later, Skip this version |
| 11:48:05 | **Update now** pressed |
| 11:48:20 | 97.3 MB downloaded into GroupLab's own folder and checked against the manifest's SHA-256. **Under 15 seconds.** The bar reads "GroupLab 0.2.0-nightly.26 is downloaded and checked." |
| 11:49:07 | **Install and restart** pressed |
| 11:49:09 | the old process is gone, 2 seconds later |
| 11:49:17 | GroupLab is back, as a new process |
| 11:49:18 | `update.arrived from=0.2.0-nightly.25 to=0.2.0-nightly.26` |

**No installer window and no elevation prompt appeared at any point.** That was watched for explicitly, by polling every visible top-level window for a title matching Setup, Install or User Account Control throughout, rather than assumed from the absence of a complaint.

### 2. What survived

| | before | after |
|---|---|---|
| sessions database | 561152 bytes | **561152 bytes, byte identical** |
| settings file | present | present |
| Add or remove programs | `GroupLab 0.2.0-nightly.25` | `GroupLab 0.2.0-nightly.26` |
| installed assembly stamp | `nightly`, `0.2.0-nightly.25` | `nightly`, `0.2.0-nightly.26` |

### 3. The rolling manifest verifies from a Windows build

This is what entry 130 section 1 asked to be confirmed separately, and it is the fix from entry 123 proved on the live file:

```
0.2.0-nightly.26 on the nightly train, commit b089122, published 2026-09-21T11:45:38Z
The signature verifies against the key given.
```

`v0.2.0-nightly.16` refused the same file with `BadSignature`. Nightly 25 and 26 accept it.

### 4. And it found a third defect

The real test has now found three faults that no unit test could, and this is the third.

The launch after an update ran the ordinary update check. It found nothing newer, said so silently, and **in saying so hid the "updated from A to B" line that had been put there a moment earlier**. The log proved the line had been set; the screen no longer showed it. The one launch where that message matters was the one launch that threw it away.

The launch after an update no longer checks again. It has just installed the newest build, so there is nothing to find, and the message survives. A test holds it.

The three, together, are the argument for this test existing:

| found by the real test | why no unit test caught it |
|---|---|
| the published nightly called itself a development build | a working copy is a development build anyway, so both sides agreed while both were wrong |
| every manifest was refused on Windows | a test signs and verifies on one machine, where the two agree however wrong they are |
| the after-update line was wiped by the next check | both steps worked; only their order was wrong, and only on a real second launch |

### 5. What testers should do

**`v0.2.0-nightly.25` is the first build that can update itself.** Everything before it has to be replaced by hand, once:

- `.12` and `.14` call themselves development builds and never offer anything (entry 125).
- `.16` and `.18` name their train correctly but refuse every manifest as `BadSignature` (entry 123 section 2.7).

From 25 onwards the updater works, and no further manual install should ever be needed.

