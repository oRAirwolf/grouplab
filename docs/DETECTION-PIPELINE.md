# GroupLab Detection Pipeline Specification

**Version** 1.0 (draft)
**Implements** DESIGN.md section 12, with the registration of section 11 as its input
**Grounded in** SCAN-MEASUREMENTS.md, which measured 343 holes across 15 files
**Status** Specification for review. No application code written.

---

## 0. Five findings that change section 12

DESIGN.md section 12 was written before the sample scans were measured. Five of its assumptions did not survive. They are listed first because the rest of this document is built on the corrected versions.

**1. The bright core is not a usable signature.** DESIGN.md section 12 lists "bright core on a scan" as a classification signature. Measured across 343 holes, the core is on average **53 grey levels darker than paper**, not brighter, and the maximum core-minus-paper observed anywhere in the corpus is **+4**. Worse than the mean being wrong is the overlap: **81 of 343 holes, 24 percent, have a core peak within 5 grey levels of their own local paper**, because the scanner lid is white and an open perforation reads as page. There is no core-intensity threshold that separates holes from paper. Any cut low enough to catch the darkest ten percent sits far below paper; any cut high enough to catch the brightest quarter sits at paper level.

**Section 6 of DESIGN.md should be corrected too.** It states that "every hole in the sample set showed a bright core surrounded by a dark ragged annulus", and draws from it the useful conclusion that berm backer material does not affect scanned appearance. The conclusion is right. The premise is half right: the dark ragged annulus is universal, the bright core is not.

**2. Radial symmetry is an actively harmful test.** DESIGN.md section 12 lists "radial symmetry within tolerance" as a signature. Measured, the rim radius has a standard deviation of **0.047 in about a mean radius of 0.105 in**, a coefficient of variation of **0.48**. A bullet hole in paper is a lobed star, not a circle. Any circularity, solidity or radial-symmetry acceptance test discards recall in proportion to how tightly it is set. This is exactly why the naive filled-contour baseline fails: it requires the hole to be both a solid blob and a circle, and it is neither.

**3. The naive baseline is far better than recorded, which raises the Phase 1 bar.** DESIGN.md section 21 sets the Phase 1 gate as beating "the naive baseline of roughly 25 percent by a wide margin". On re-measurement, a properly tuned Hough circle detector achieves **26 of 27 true positives with 4 false positives** on `300_nm_hand_load.jpg`. The published 7-of-20 is reproducible only as one operating point on a sweep, not as a property of the method. Section 9 of this document proposes a replacement gate, because a gate of "beat 25 percent" would now be passed by a method nobody should ship.

**4. Colour is style-conditional and gives nothing on the hardest style.** On the greyscale rendering `retumbo.png`, the printed ring and the hole core are the **same grey**: 156.2 against 158.7, with standard deviations of 28 and 58. The best Fisher ratio on any channel is **0.14**. Chroma is the best channel on all five colour-printed styles at Fisher 2.05 to 4.89, and useless on the two achromatic ones. Luminance is the best channel on the black style at 2.78 and useless on all five colour ones. There is no universal channel.

**5. What does hold everywhere is scale.** Every printed feature in the corpus is at most **0.0567 in** wide, the widest being a barcode bar, with the printed ring stroke at 0.0525 to 0.0539 in. Every hole is **0.15 to 0.54 in** across. That is a factor of three to nine, it is present in every style including the greyscale one, and it survives resolution changes because it is expressed in physical units. **Scale, not intensity and not colour, is the primary discriminator.** Everything else in this pipeline is a refinement on top of that.

One further correction, from section 5 of the measurements: **the crumpled target is barely warped**. A four-point homography on `IMG_20250530_0001` leaves rms 0.0027 in and max 0.0093 in, indistinguishable from four pristine sheets. This affects registration rather than detection, and it is dealt with in FIDUCIAL-DECISION.md section 2.

---

## 1. Scope, and an awkward fact about the test set

This document specifies detection for the **primary mode**: a target GroupLab generated, whose definition is known exactly, analysed from a scan or a photograph.

**None of the seventeen sample scans is a GroupLab target.** They are all OnTarget sheets. Render-and-difference, which is the whole architectural advantage, requires knowing the artwork, and the artwork of these sheets is somebody else's.

This is not a reason to defer testing until targets are printed. The geometry of all four OnTarget styles in the corpus **has now been measured to better than a thousandth of an inch**, so a reference model of each style can be reconstructed and rendered, and render-and-difference can be tested against real shot targets today, on paper that has actually been shot, before a single GroupLab sheet exists.

Measured geometry of the four styles, from SCAN-MEASUREMENTS.md section 2.3:

| Style | Files | Grid | Pitch | Ring centreline dia | Outer dia | Inner dia | Stroke | Centre dot |
|---|---|---|---|---|---|---|---|---|
| #51 black thin ring | `300_nm_*`, `28_6_5_*` | 5 x 5 | 1.4985 in | 1.1977 to 1.2010 in | 1.2507 to 1.2530 | 1.1426 to 1.1431 | 0.0525 in | 0.1031 in |
| #18 blue or grey thin ring | `n568*`, `retumbo.jpg`, `retumbo.png` | 5 x 5 + sighters | 1.4981 in | 0.9477 to 0.9499 in | 0.9989 to 1.0003 | 0.8924 to 0.8926 | 0.0533 to 0.0539 | 0.0535 in |
| #1 solid red bull | `338lmao`, `6_5retumbo`, `retumbo_0001` | 5 x 5 + sighters | 1.4959 in | 0.4893 to 0.4899 in | 0.5684 | 0.2720 to 0.2724 | 0.1481 annulus | 0.0530 in |
| #3 solid red bull | `IMG_20250530_0001`, messaging copy | 4 x 5 | 1.8750 in | 0.5001 in | 0.5778 | 0.2649 | 0.1564 annulus | 0.0637 in |

Sighter row, where present, sits **1.799 plus or minus 0.001 in** below the last scoring row, against a 1.500 in normal step. That single number identifies a sighter row without OCR.

**These reconstructions must never ship.** DESIGN.md section 3 excludes any OnTarget compatibility, target library or file format, and START-HERE.md repeats it. These are throwaway test fixtures living in the test project, used to evaluate an algorithm against real shot paper, and the repository should say so where they live. They are not a target library, they are not distributed, and they encode measurements of physical geometry rather than any copied design.

The **secondary mode**, analysing store-bought targets and blank paper with user-defined scale and manual placement, is specified in section 8.

---

## 2. The corpus, and what each file is for

Every file in `scans/` earns its place by testing something specific. This table is the acceptance matrix; section 9 sets the numbers.

| File | Resolution | What it tests | Known difficulty |
|---|---|---|---|
| `300_nm_hand_load.jpg` | 600 DPI, 25 shots, .308 | The reference case. Black rings on white | Two holes overlap at bull 20. One hole outside bull 15. Marker caption "Hand Loads" |
| `300_nm_factory.jpg` | 600 DPI, 20 shots, .308 | Load comparison partner to the above | Wider dispersion |
| `28_6_5_*` (four files) | 600 DPI, 6.5 CM | Style #51 repeatability. Load-versus-load statistics on real data | Smallest holes in the corpus, hull mean 0.220 to 0.241 in |
| `n568.jpg` | 600 DPI, .264 | Blue thin rings. Cross-cell shots. **Holes measure 1.121 of calibre, the only file above 1.0** | 3 shots outside their cell, worst 0.171 in. Blue arrow annotation |
| `n568-gm210m.jpg` | 600 DPI, .264 | Blue rings, shot count exceeds bull count | **27 shots for 25 bulls.** Breaks one-to-one assignment |
| `n568-ruag.jpg` | 600 DPI, .264 | Blue rings | 1 shot outside its cell by 0.273 in |
| `338lmao.jpg` | 600 DPI | Solid red bulls. The hard assignment case | **1 shot where nearest-bull is provably wrong.** Two more with a margin under 0.12 in. Blue caption whose letter bowls read as holes |
| `6_5retumbo.jpg` | 600 DPI | Red annulus style, blue marker strokes | Different colour separation from black styles |
| `retumbo.jpg` | 600 DPI | Pink-over-blue style | Chroma is the only useful channel |
| `retumbo.png` | 600 DPI greyscale | **The hardest file in the corpus** | Ring and hole are the same grey. Fisher 0.14 on every channel. Black marker X over two holes |
| `retumbo_0001.jpg` | 600 DPI | Solid red, blue ballpoint arrows | Genuine multi-shot cells. Arrows detected as holes |
| `IMG_20250530_0001.jpg` | 300 DPI | The ugly one. Crumpled, torn, taped, non-rectangular | 0.76 in of paper missing at the top-right. Page outline unrecoverable |
| `IMG_20250530_0001.pdf` | 300 DPI in PDF | Scanners emit PDF | Lower-quality re-encode of the same scan. Prefer the JPEG |
| `1748713494260-*.jpg` | **93 DPI derived** | Degraded input through a messaging app | **No DPI tag at all.** 19 px across a hole |

Two structural facts about the corpus that the pipeline must respect:

**Thirteen of the sixteen raster files came off one scanner in one configuration:** 4958 by 6458 px, 600 DPI, JPEG quality about 86, 4:4:4 subsampling. Thresholds tuned on one transfer to the others. That is convenient and it is also a trap, because it means the corpus systematically understates variability across scanners.

**The 600 DPI window is 8.2633 by 10.7633 in on an 8.5 by 11 sheet.** The scanner crops 0.237 in inside the page on each axis, so **there is no page edge in any of the fourteen 600 DPI files**, and on the 300 DPI file the paper-to-lid contrast is three grey levels. Any registration or normalisation that needs the sheet outline is dead on arrival with this hardware.

---

## 3. Pipeline stages

Eleven stages, each with a defined input, output, failure mode and observability record. The stage identifiers are stable and are referenced by the observability contract in section 6 and by the UI. S10 runs once per session rather than once per image, and on a single-sheet session it is a pass-through.

```
S0  decode        file bytes ................ normalised raster + provenance
S1  scale         raster ..................... pixels-per-inch estimate + confidence
S2  fiducials     raster ..................... detected markers with ids and corners
S3  register      markers + definition ....... transform, residual, inlier set
S4  verify        transform + metadata ....... print-scale and metadata report
S5  render        definition + transform ..... expected artwork in image space
S6  difference    raster + expected .......... residual field
S7  candidates    residual field ............. candidate regions
S8  classify      candidates ................. scored detections
S9  assign        detections + definition .... shots bound to bulls
S10 pool          per-sheet results .......... one composite group
```

### S0. Decode and normalise

Decodes every format in DESIGN.md section 10 and produces one normalised raster plus a provenance record.

- Honour EXIF orientation. Record whether a rotation was applied.
- Decode the base SDR image and **ignore HDR gain maps**.
- Take the **primary item** from a multi-image HEIF container as the specification defines it, not the first item in the file.
- Recognise and silently skip `.AAE` sidecars.
- For PDF, extract the largest embedded raster. Measured on `IMG_20250530_0001`: the PDF's embedded image is an independent JPEG encode at estimated quality 61 against the sibling JPEG's 76, differs by one pixel in width, and phase-correlates at a sub-pixel offset of +0.57 px, so they were resampled differently. **If a sibling raster exists, prefer it, and record that the choice was made.** For a PDF-only input, extract and proceed.

Output is 8-bit per channel linear-indexed RGB plus, where the source was greyscale, a flag saying so. `retumbo.png` is genuinely mode L and must not be treated as a degenerate colour image, because every chroma test on it returns zero and the pipeline needs to know that in advance rather than discovering it.

**Failure mode.** Unsupported codec, corrupt file. Report the format detected and what was missing.

**Amended 2026-09-15, NOTES-FROM-PLANNING.md entry 35 section 6 item 3: the sheet names its definition.** When no definition is given, `S0.identify` follows the decode. It reads the sheet's QR codes at full, double, half and quarter resolution, stopping at the first that yields a GLTD-B frame passing its CRC, and takes the definition whose identifier the frame's body computes to from the definitions available. Codes that name two definitions or two tiles, or a definition not available, are refused with the reason, and the definition is then asked for. Nothing is chosen from the markers, which the built-in definitions share. A resolution that would make the image longer than 8000 px is skipped. On the 37 Phase 0 scans and photographs, 33 give the definition and tile they were printed from on Windows and macOS and 32 on Linux, and none gives a wrong one on any platform. The QR detectors are native code built separately for each platform, so the count is measured on each platform by `grouplab identify sweep` in the gate record workflow rather than assumed from one (entry 47 section 3). **The number that must stay at zero is the wrong names, not the unnamed ones** (entry 48 section 4). A sheet given the wrong definition would scale every measurement on it by a wrong number and produce a page of confident figures with nothing saying they are wrong, whereas a sheet not named costs its user one choice from a list. Anyone raising the hit rate keeps every refusal: a damaged frame, codes naming two definitions or two tiles, and an identifier that is not among the candidates. The per-platform sweep fails on a single wrong name and on nothing else.

### S1. Resolve scale

Establishing pixels per inch, in strict order of authority.

1. **Fiducials, once S2 and S3 have run.** Authoritative. This is the only source that cannot be wrong about the target plane.
2. **The file's own resolution tag**, used as a starting estimate only. Measured, the tag is accurate to better than 0.1 percent on all fifteen files that carry one: the pooled scoring-grid pitch across 112 gaps is 1.49845 in against a 1.5 in nominal, standard deviation 0.00225 in.
3. **Derivation from content**, when no tag exists. On the messaging file, two independent derivations agree: 789 px over the 8.4833 in page width gives 93.0 DPI, and a 174.46 px measured grid pitch over a 1.875 in nominal gives 93.05 DPI.
4. **Ask the user.**

**There is a bootstrap problem here and it must be handled explicitly.** Fiducial detection needs an approximate scale to size its search, and scale comes from fiducials. The resolution is that S1 produces a *provisional* estimate from the tag or from a default, S2 runs a multi-scale search seeded by it, and S1 is then re-run with the fiducial-derived answer. If the provisional estimate is wrong by more than about a factor of two, S2 must widen rather than fail.

**This is a measured failure, not a hypothetical.** The bull-finding matched filter in the measurement harness failed completely on the messaging file when it assumed the default 600 DPI, reporting a nonsense pitch of 0.279 in, because its radius sweep never reached the true 22.6 px ring radius. **Any scale-dependent search must be driven by a derived scale, never a defaulted one**, and must report which it used.

**Failure mode.** No tag, no fiducials, no user input. The pipeline can still detect holes but cannot report any measurement in real units, and must say exactly that rather than producing numbers in pixels dressed as inches.

### S2. Fiducial detection

Per FIDUCIAL-DECISION.md: AprilTag `tag36h11`, 0.5 mm module, 4.0 mm marker, placement rule `grid-boundary-1`. Two detectors can read it, OpenCV on the desktop and the BSD-2-Clause AprilTag reference implementation on mobile, which is the reason that family was chosen.

**Two shape gates run before the code check, and they are independent of it.** Measured in FIDUCIAL-DECISION.md section 4.1, every false marker the permissive detector found on the real corpus was a sliver: worst longest-to-shortest side ratio 4.57, largest area 1366 px squared against a real marker's 8928 at 600 DPI. Reject any candidate quad below half the expected marker area, or with a side ratio above 2.0, before its bits are sampled. A defence that fails differently from the Hamming check is worth more than a stronger version of the same check, and both belong in the trace as separate rejection reasons.

**One of the two runs after decoding, and OpenCV is the reason.** The area gate is expressible to the detector in advance, as a minimum perimeter, so it does run before bits are sampled. The side-ratio gate is not: OpenCV exposes no pre-decode setting for it, so it runs on the candidates that decoded. Recorded here rather than left in an implementation note because it changes what this stage can promise. The cost is small and worth being explicit about: a sliver that happens to carry a valid code is rejected a few microseconds later than intended rather than not at all, and the measured false-positive rate at default parameters was zero across sixteen scans and nine dictionaries in the first place. The ordering matters again only if the detector is ever replaced by one that can gate on shape up front, at which point this paragraph is the note saying it should.

Two settings that are not the library defaults and whose absence would be a silent, comprehensive failure:

- **Corner refinement must be enabled.** OpenCV's `cornerRefinementMethod` defaults to `CORNER_REFINE_NONE`. Against a Phase 0 gate of one thousandth of an inch, which is 0.6 px at 600 DPI, unrefined corners would not come close.
- **The adaptive-threshold window must be sized for the input.** `adaptiveThreshWinSizeMax` defaults to 23 px, which is tuned for camera-scale markers. At 600 DPI a 4.0 mm marker is 94 px across. Either downsample before detection or widen the window, and settle which in Phase 0.

**Fallback, in order.** Full marker set, then partial set with RANSAC, then QR finder patterns from any surviving code (three per code, twelve if all four survive; see FIDUCIAL-DECISION.md section 8), then visually detected bull centres with reduced accuracy and an explicit warning, per DESIGN.md section 9.

**Failure mode.** Fewer than four markers and no usable QR finder patterns. Degrade to bull-centre registration and say so prominently.

### S3. Register

RANSAC against the known page coordinates from the definition, then solve.

- **Scans: homography.** The measurements settle the model choice. On the crumpled sheet, similarity leaves rms 0.0040 in, affine 0.0028 in, homography 0.0027 in, and a biquadratic 0.0025 in. Homography captures essentially everything; the biquadratic buys 0.0002 in, which is a quarter of the harness's own 0.008 in noise floor.
- **An affine minimum is required, not a similarity.** Rows and columns are consistently non-orthogonal by 0.12 to 0.17 degrees, with the same sign on thirteen of fifteen files. That is systematic scanner shear, and a similarity fit cannot absorb it while an affine fit absorbs it exactly.
- **Photographs: homography plus a radial distortion model.** This is where thirty to forty distributed markers earn their place, since radial distortion is a function of radius from the optical centre and four corner points sample that function at one radius. There is **no genuine off-axis photograph in the corpus**, so nothing here is validated. SAMPLE-NOTES.md already flags this and it remains the largest untested area in the whole pipeline.

Report the residual as an image-quality figure the user sees, per DESIGN.md section 11.

**Failure mode.** Residual above threshold, or too few inliers. Refuse to measure rather than measuring badly, and offer the manual path.

### S4. Verify scale and metadata

DESIGN.md section 11's metadata comparison report, plus section 9's print-scale verification.

For a scan: the resolution the file asserts, the horizontal and vertical resolution actually measured, and the correction applied. Report the axes separately, because the measured shear means they genuinely differ.

For a photograph: the fitted distortion coefficients, the maximum displacement at the frame edge in pixels, and that displacement converted to inches at the target plane. That last figure states how wrong the measurement would have been without the correction, which is the number worth showing.

Print scale is reported, not silently corrected: "this target printed at 96.2 percent of intended size, measurements corrected accordingly."

**Amended 2026-09-15, `docs/NOTES-FROM-PLANNING.md` entry 23 section 4: detection runs inside the registered sheet.**
- **The rule.** From S5 on, only pixels inside the page boundary, mapped into the image through S3's registration, can become a candidate.
- **A property of the detector, not a step a caller performs.** `RenderDifferenceHoleDetector` masks its residual to the sheet itself, so no path can hand S7 a full frame by accident.
- **The measurement that forced it.** On the N568 photograph against its own scan, `docs/PHASE1-RESULTS.md` entries 19 and 20, 0 of 28 holes were found on the whole photograph, where the dark mat merged into one blob twelve inches across, and 26 of 28 on the same pixels cropped to the sheet, untuned.

### S5. Render the expected artwork

Rasterise the known definition through the solved transform into image space, producing an expected image aligned to the observed one.

Requirements:

- Render at the same resolution as the observed raster, anti-aliased, using the ink colours from the definition's palette.
- **Model ink spread.** Printed edges are not step functions. A small dilation plus a Gaussian, with parameters fitted per image from the observed edge profile of a known long straight feature such as a cell rule, will subtract far more cleanly than a hard-edged render.
- **Model the paper level locally, not globally.** Measured paper V ranges from 195.8 to 255.0 across the corpus with a per-hole local mean of 245.65 and standard deviation 9.9. Estimate paper level as a smoothly varying field, for instance a heavily blurred version of the observed image with dark features excluded, and normalise the observed image against it before differencing.
- **Emit the declared exclusion zones alongside the render.** The definition names regions that are printed matter by construction and can never contain a shot: the data block of TARGET-SCHEMA.md section 3.10, the code squares with their quiet zones, the fiducial footprints, and the printed identifier text. These are geometry from the file, not guesses from the image, and S7 masks them out before it looks for candidates.
- Emit the render as an artefact, because comparing it against the observed image by eye is the single most useful debugging view in the pipeline, and, per section 6, the most compelling thing to show a user.

**The data block earns its own mention.** In `blank` print mode it is a box a shooter has written a load into with a ballpoint pen at the range, in wind, on a clipboard. Handwriting is exactly the failure case the corpus already demonstrates: on `338lmao.jpg` five letter bowls in a blue caption read as holes to the naive detectors, and on `n568.jpg` and `retumbo_0001.jpg` marker and ballpoint strokes had to be rejected on chroma. Those rejections cost work and are not perfectly reliable. A declared rectangle costs nothing and is perfectly reliable, which is the whole argument for a self-describing target restated in one place: **the pipeline should never have to classify something the definition could have told it about.**

The exclusion is a hard mask, not a penalty. A candidate whose hull centroid falls inside an exclusion zone is dropped at S7 and recorded as a rejection with the zone named, so the trace says "rejected, inside dataBlock" rather than "rejected, score 0.28". A candidate that straddles a zone boundary is kept and flagged, because a shot can legitimately clip the edge of a printed region.

### S6. Difference

Subtract the expected image from the observed and produce a residual field.

**The residual field is not simply observed minus expected.** Three components, computed and retained separately, because they answer different questions and because the classifier in S8 wants them apart:

- **`D_lum`**, luminance residual against the locally estimated paper level, which is the channel that works on black and greyscale artwork. Measured Fisher on the black style: 2.78 for L*, against 0.26 for chroma.
- **`D_chroma`**, chroma residual, which works on colour-printed artwork. Measured Fisher 2.05 to 4.89 on the five colour styles, and 0.00 to 0.26 on the achromatic ones.
- **`D_neutral`**, defined as local paper level minus max(R, G, B). This is a good general suppressor of coloured ink because red and pink keep one channel near paper. **It fails on blue**, measured Fisher 0.04 on `n568.jpg`, because blue ink's maximum channel sits at 184.7, far below paper. It must not be used alone.

**Which of the three is trusted is decided per image, from a measurement, not from a setting.** Compute the mean chroma of the rendered artwork region:

| Measured artwork chroma | Style | Trust |
|---|---|---|
| Approximately 0 | Greyscale render | `D_lum` only. Chroma carries no information |
| Under about 15 | Black or grey ink on white | `D_lum` primary, `D_neutral` secondary |
| 86 to 133 | Colour ink | `D_chroma` primary, `D_lum` secondary |

The measured values are unambiguous: paper 5.3 to 7.6, colour artwork 86 to 133, greyscale artwork 0. This is a three-way test with no overlap, and it should be a measurement stated in the trace rather than a heuristic buried in a branch.

### S7. Candidate extraction

**The primary operation is morphological, driven by physical scale.** From the measurements: every printed feature is at most 0.0567 in wide, every hand-drawn stroke is 0.037 to 0.102 in wide, and every hole is 0.15 to 0.54 in across.

```
mask   the declared exclusion zones from S5   # data block, codes, markers, id text
open   with a disk of radius 0.032 in         # erases every printed stroke
threshold, relative to local paper level      # never an absolute grey value
close  with a disk of radius 0.055 in         # bridges gaps in a ragged rim
fill holes
convex hull per component
```

Four points, each of which is a measured result rather than a preference.

**The opening radius is set by the widest printed stroke.** A 0.032 in radius gives a 0.064 in disk, which exceeds the 0.0567 in barcode bar and the 0.0525 in ring stroke, so all printed line work is erased. This is what makes the method robust to a style it has never seen.

**The convex hull step is essential and not cosmetic.** The rim is frequently a C rather than an O. Enclosure-based sizing loses those holes; hull-based sizing recovers them.

**The threshold must be relative to local paper, never absolute.** Measured, the same detector needed `dn_thresh` 28 at 600 DPI, 12 at 300 DPI and 18 at 93 DPI on the same physical targets, because rim contrast falls as each rim is averaged over fewer, larger pixels. **Any absolute intensity threshold in this pipeline is a resolution-dependent bug waiting to be filed.** Express thresholds as a fraction of the local paper-to-ink dynamic range, and record the resolved absolute value in the trace so it can be checked.

**All structuring-element radii are in inches and converted using the solved scale.** None is in pixels. This is what lets one parameter set span 93 to 600 DPI.

The hand-drawn stroke width range of 0.037 to 0.102 in **overlaps** the printed ring stroke at 0.0525 in. The 0.032 in opening therefore erases some hand ink and keeps some: the thick black marker caption on `300_nm_hand_load` at 0.102 in survives any opening that also keeps a 0.15 in hole. Hand ink is a classification problem, not a morphology problem, and S8 owns it.

### S8. Classify and score

Every candidate carries a score. The signatures below replace those in DESIGN.md section 12, with the reasons recorded.

| Signature | Status | Basis |
|---|---|---|
| **Size within a window from caliber and solved scale** | **Keep. Primary.** | Hull diameter 0.2655 plus or minus 0.0570 in, range 0.150 to 0.539. Perforation measures 0.85 to 0.96 of caliber |
| **Rim darkness relative to local paper** | **Keep. Primary.** | Annulus minimum V 38.5 plus or minus 35.6 against paper 245.6. This is the reliable signal |
| **Rim closure fraction over angle** | **Add, replacing radial symmetry** | Fraction of 360 rays on which a rim minimum below threshold is found. Tolerant of a C-shaped rim, intolerant of a random dark blob |
| Bright core on a scan | **Drop** | Core is 53 V darker than paper on average. 24 percent of cores reach local paper level |
| Radial symmetry within tolerance | **Drop** | Rim radius CV 0.48. Holes are lobed stars |
| Torn fibre halo at the boundary | **Keep, softened** | Real, but rim thickness is 0.070 plus or minus 0.045 in, a CV of 0.64. Use as a weak positive, never as a gate |
| **Chroma deficit against the local artwork model** | **Add, style-conditional** | Hole annulus chroma 15 to 42 against colour artwork 86 to 133. Blue ballpoint has chroma 97 to 164 and is separable; black marker at chroma 0 to 11 is not |
| **Position relative to the definition** | **Add** | Free, because the definition is known. See below |

**Position deserves its own paragraph because it is nearly free and it eliminated every observed false positive.** Both naive baselines were re-run for the measurements, and **every single false positive in both was in the footer band below the bull grid**: barcode bars, footer text, and a handwritten caption. Not one was a ring segment, a numeral or a centre dot; those were correctly rejected on size at every threshold setting. Because GroupLab generated the target, it knows where the bulls are and knows that the footer is not a scoring area. A prior that down-weights candidates far outside any cell would have removed one hundred percent of the observed false positives at zero cost.

**Ink over holes must be handled, because it is real.** Measured on `retumbo.png`, the hole under an X mark in cell 16 reports a hull diameter of 0.3804 in against a file mean of 0.3110 plus or minus 0.0412, the one in cell 21 reports 0.3588 in, and a marked sighter reports 0.3354 in. Marking a hole inflates its measured diameter by roughly 0.03 to 0.07 in. The hole is still detected, the rim is still visible around and between the strokes, but the **hull is contaminated and the centroid is biased**. The right response is not to reject these but to detect the contamination and flag it: a candidate whose hull diameter exceeds the image median by more than about two standard deviations, and which contains an internal dark structure that is not a rim, goes to the review queue with the reason stated as "possible ink over hole, measured diameter may be inflated".

**Holes on ink measure differently, systematically.** Measured across 114 on-ink against 229 on-paper holes: hull diameter 7 percent smaller, annulus minimum V rises from 34 to 48 with 45 percent more spread, apparent rim 29 percent thicker, and the core unchanged, since the paper carrying the ink is gone. The classifier must know from the render whether a candidate sits on artwork and apply the appropriate expectation. This is another thing render-and-difference gets for free that a generic detector cannot have.

**Sub-pixel centroids** are computed for accepted holes. Given the measured raggedness, the centroid must be **intensity-weighted over the whole disturbed region**, not a fitted circle centre. Fitting a circle to a lobed star puts the centre wherever the lobes happen to be distributed.

**Caliber estimation** follows DESIGN.md section 12, with the measured calibration now available. Median hull diameter over all detections, then divide by the measured ratio to recover caliber:

| Caliber | Nominal | n | Mean hull diameter | Ratio | Deficit |
|---|---|---|---|---|---|
| 6.5 mm, .264 | 0.264 in | 189 | 0.2427 in | **0.919** | −0.0213 in |
| .308 | 0.308 in | 42 | 0.2833 in | **0.920** | −0.0247 in |
| .338 | 0.338 in | 29 | 0.3317 in | **0.981** | −0.0063 in |

The `n568*` files are **.264, confirmed by the author**, not .338 as the original brief stated. On the corrected attribution the .264 and .308 pools agree to three decimal places, 0.919 against 0.920, which they did not before. Across all 260 holes of known caliber the deficit is **−0.0202 in with a standard deviation of 0.0509**, and it is roughly constant in absolute terms rather than proportional. **Model it as a fixed closure allowance, not a scale factor**, and recover caliber as measured diameter plus 0.020 in rather than measured diameter divided by 0.92.

**One file remains anomalous and it is worth a Phase 1 look.** `n568.jpg` measures 0.2961 in against a .264 bullet, a ratio of **1.121**, making it the only file in the corpus whose holes measure **larger** than the projectile. Its two siblings, same powder and differing only in primer, measure 0.896 and 0.922. Its standard deviation is 0.0236, the tightest in the corpus, so the effect is consistent rather than erratic.

The interesting hypothesis is **bullet yaw**: a projectile arriving off-axis cuts an elongated hole. If that is what this is, it is detectable, and no existing tool reports it. An ellipse fit measuring aspect ratio and orientation across a group would show it, since yaw-induced elongation has a consistent orientation while tearing does not.

**This was attempted during the verification pass and the measurement failed.** Two ellipse-fitting passes produced aspect-ratio standard deviations of 0.32 to 1.64, larger than the effect being sought, and major axes inconsistent with the validated harness. The question is open. A trustworthy ovality measurement is a Phase 1 task, and if it works it is a candidate feature rather than merely a diagnostic.

Either way, DESIGN.md's honesty requirement stands: when two calibers are close, present both rather than guessing confidently. The measured spread makes distinguishing 0.264 from 0.284 unreliable, exactly as section 12 predicted.

### S9. Assign shots to bulls

**Nearest-bull is not the rule.** The rule is the **globally optimal one-to-one matching** between detections and bulls, minimising total distance, which encodes the one-shot-per-bull constraint the target was designed around.

Measured, on the files where shot count equals bull count:

- Zero to three shots per target land outside their own 1.5 in cell, by 0.09 to 0.27 in.
- Nearest-bull is correct on all of them **except one**, on `338lmao.jpg`: a hole at (6.040, 1.614) in is 0.627 in from bull 4 and 1.043 in from bull 9. Nearest-bull gives it to bull 4, but bull 4 already has a much closer shot at 0.289 in and bull 9 has none. One-to-one matching gets it right.
- Two further shots on that file have a nearest-versus-second-nearest margin under 0.12 in, which means a 0.05 in registration error would flip them. Those are review-queue candidates by construction: **when the margin is under about 0.15 in, say so rather than deciding silently.**

**The one-to-one method is only valid when the counts match, and this is a measured failure, not a caveat.** On `n568-gm210m.jpg`, which has 27 detections for 25 bulls, the matching is forced to assign two shots to distant unused bulls and manufactures a false cross-cell result at 0.854 in. The same failure voids `retumbo_0001.jpg`, which has genuine multi-shot cells.

The rule the pipeline must follow:

1. If detections equal scoring bulls, use one-to-one matching. Highest confidence.
2. If detections are fewer, use one-to-one matching against the subset, leaving bulls empty. Still sound.
3. **If detections exceed bulls, do not force a matching.** Fall back to nearest-bull within a distance gate, flag every ambiguous case, and tell the user the counts disagree. Forcing produces confident wrong answers, which is the one thing DESIGN.md section 2 says the software must never do.

### S10. Pool across sheets

Runs once per session rather than once per image. On a single-sheet session it copies its input to its output and says so.

A session can span several sheets in three different ways, and only one of them needs anything new.

| Case | What it is | What S10 does |
|---|---|---|
| One sheet | the normal case | pass through |
| Several independent sheets, same definition | the shooter used three copies of GL-CF25-LTR for three loads | keep them separate. This is not pooling, it is three sessions the user happened to start together |
| Several independent sheets, same load | 300 yards, four sheets, same load on all of them | pool, per STATISTICS.md section 11 |
| **Tiles of one assembly** | six sheets of GL-LR300-T making one 36-bull target | **pool, and the tile index says which is which** |

**The tiled case is the one the format supports directly, and it is simpler than it looks.** Each tile is a separate scan that runs S0 through S9 entirely on its own: its own scale, its own fiducials, its own registration, its own residual field, its own assignment. S10 then concatenates the per-bull results, using the `tileIndex` byte from the frame header to map each sheet's local bull indices onto the assembly's global ones.

**Nothing in that chain uses the position of one tile relative to another**, which is the property that makes tiled targets work at all. Every shot is expressed as an offset from its own bull centre, and every bull sits wholly on one tile registered by that tile's own fiducials. Tile alignment error, tape gaps, a sheet stapled 4 mm out of square: all of it contributes exactly zero to the composite group. TARGET-SCHEMA.md section 3.12 makes the same argument from the format side, and section 10 test 35 is the check that it holds in practice.

What S10 does have to be careful about is bookkeeping rather than geometry:

- **Every tile must be present, and each exactly once.** Six tiles of a 3 by 2 assembly means tile indices 0 through 5, no gaps and no repeats. A missing tile is reported as missing bulls, not silently dropped, because a composite of 30 shots that should have been 36 is a biased sample if the missing sheet was the one the shooter fired last.
- **A repeated tile index means two physical sheets of the same tile**, which is a legitimate thing to do (shoot the assembly twice) and an easy thing to do by accident (scan one sheet twice). S10 asks rather than guessing, because the two cases are indistinguishable from the images.
- **Every tile must decode the same definition identifier.** A sheet from a different assembly, or a different printing at a different scale, is rejected with the two identifiers named.
- **Per-tile scale is verified independently and reported per tile.** Printers drift and a six-sheet job can print the last sheet at a slightly different scale from the first. Because each tile registers on its own fiducials this is corrected rather than merely detected, but the trace should say so, since a tile that printed at 96 percent while its neighbours printed at 100 is a printer problem the user wants to know about.
- **Confidence is per shot and survives pooling.** A contested assignment on tile 3 stays contested in the composite. Pooling must not launder a review-queue item into a clean one.

The independent-sheets-same-load case, third row of the table, is the same code path with the tile index absent and the bull indices offset by sheet order instead. The only difference is that S10 cannot verify completeness, because nothing in the definition says how many sheets the user intended to shoot.

**Failure mode.** Mixed definitions, missing or duplicate tiles, or a tile whose registration failed. In every case report what is present, what is missing, and what the composite would look like without it, rather than refusing outright: 30 shots from five of six tiles is still a usable measurement provided the user is told it is five of six.

**The hand-drawn arrows are ground truth and are being thrown away.** Several targets carry marker arrows drawn from a bull to a shot in another cell. They are the shooter's own record of the correct assignment. The pipeline cannot read them, and should not try. But for **evaluation** they are the only independent ground truth for cross-cell assignment that exists in this corpus, and they should be transcribed by hand once into a fixture file so that assignment accuracy can be measured rather than asserted. That is an afternoon of work and it converts an untestable claim into a measured one.

---

## 4. Per-file expected behaviour

What each file should do, and what a failure on it means. These become the integration test suite.

| File | Registration | Detection | Assignment | Specific behaviour required |
|---|---|---|---|---|
| `300_nm_hand_load.jpg` | Bulls only, no fiducials | 27 holes | 25 bulls, 27 shots | **Must detect the overlapping pair at bull 20 as two shots or flag it.** Must not silently report one. Must reject the "Hand Loads" caption |
| `300_nm_factory.jpg` | Bulls only | 20 holes | Clean | Baseline comparison partner |
| `28_6_5_*` | Bulls only | 16 to 25 per file | Clean | Smallest holes. Size gate must reach down to 0.15 in |
| `n568.jpg` | Bulls only | 28 holes | 3 cross-cell, all recoverable | Blue arrow must be rejected. Blue ring must not clip the rim |
| `n568-gm210m.jpg` | Bulls only | 27 holes | **Counts disagree, 27 for 25** | **Must refuse to force a matching and must tell the user.** Path 3 of S9 |
| `n568-ruag.jpg` | Bulls only | 26 holes | 1 cross-cell by 0.273 in | Recoverable |
| `338lmao.jpg` | Bulls only | 18 shots plus 11 non-shots | **1 genuine nearest-bull failure** | Must get the (6.040, 1.614) shot onto bull 9. Must reject the blue caption's letter bowls, of which 5 read as holes |
| `6_5retumbo.jpg` | Bulls only | 19 holes | Clean | Chroma path. Blue marker strokes rejected |
| `retumbo.jpg` | Bulls only | 25 holes | Clean | Pink over blue. Chroma primary |
| `retumbo.png` | Bulls only | 25 holes | Clean | **The hardest file.** Greyscale, chroma unavailable, black marker X over two holes. Both marked holes must be found and flagged for inflated diameter |
| `retumbo_0001.jpg` | Bulls only | 27 detections | **Multi-shot cells** | Must not force one-to-one. Blue ballpoint arrows must be rejected on chroma |
| `IMG_20250530_0001.jpg` | **Must not attempt page-outline registration** | 7 to 22 holes | 20 bulls | Crumpled and torn. Homography from bulls must land within 0.0093 in. Must not fail on the missing corner |
| `IMG_20250530_0001.pdf` | As above | As above | As above | Must prefer the sibling JPEG and record that it did |
| `1748713494260-*.jpg` | Scale derived, not defaulted | About 20 of 22 | 20 bulls | **No DPI tag.** Must derive 93.0 DPI from content. Must not assume 600 |

Note that all of these run through the **bull-centre fallback** registration path, since none carries fiducials. That is useful: it exercises the degraded path against fourteen real targets, which is more coverage than the primary path will have until sheets are printed and shot.

**Two paths have no test file in this corpus and will not until GroupLab sheets are printed and shot: the data-block exclusion of S5 and S7, and the tile pooling of S10.** Both are specified here because the format specifies them, and both must be marked as untested in the Phase 1 report rather than reported alongside the fourteen measured files as though they carried the same evidence. The synthetic path partly covers them: TARGET-SCHEMA.md section 10 test 43 renders a definition and analyses the render, which exercises exclusion-zone masking on real geometry, and test 35 pools synthetic tiles. Neither is a substitute for paper.

---

## 5. The photograph path

Everything above is the scan path. The photograph path differs in ways the corpus cannot test, and the honest position is that **none of it is validated**.

| | Scan | Photograph |
|---|---|---|
| Hole interior | Scanner lid, white, unpredictable | **Whatever is behind the target.** Berm, cardboard, sky |
| Illumination | Uniform | Directional, with shadows in the perforation |
| Distortion | Homography sufficient, measured | Radial distortion model required, unmeasured |
| Scale | Uniform across the frame | Varies across the frame |
| Motion blur | None | Present |

DESIGN.md section 6 gets the consequence right: scans and photographs need genuinely different detectors, not one detector with a parameter. The measurements confirm the reason. On a scan the perforation shows the white lid, so backer material is irrelevant. On a photograph the backer is exactly what shows, so backer material determines the sign and magnitude of the core signal. A detector that assumes either is wrong on the other half of its inputs.

**What is needed before this path can be specified rather than sketched:** an unresized camera original, shot handheld from normal standing distance, deliberately off-axis, of a target with a known backer, plus a second of the same target with a different backer. SAMPLE-NOTES.md already asks for the first. The second is worth adding, because it isolates the one variable that scans cannot exercise at all.

Until those exist, the photograph path in this document is a plan and should not be presented as a specification.

---

## 6. The observability contract

Every stage emits a trace. This exists for three reasons that happen to coincide: it makes Phase 1 debuggable, it gives the command-line spikes their output format, and it is what a "show the work" view renders.

The requirement is that a user can watch the pipeline think, and that what they see is the actual intermediate state rather than an animation of it. A tool arguing that nobody should believe a number without seeing its provenance should be able to show its own.

### 6.1 What each stage emits

```
StageRecord {
  stage         : string        // "S6.difference"
  sequence      : int
  startedUtc    : timestamp
  durationMs    : int
  status        : ok | degraded | failed
  summary       : string        // one line, human, e.g.
                                // "31 of 34 markers, 2 rejected by RANSAC"
  metrics       : { name -> value with unit }
  decisions     : [ Decision ]
  rejections    : [ Rejection ]
  artefacts     : [ Artefact ]
  parameters    : { name -> resolved value }
}

Decision  { what, chosen, alternatives[], because }
Rejection { what, where(x,y in page inches), why, score }
Artefact  { id, kind, mime, extent(page inches), caption }
```

Three properties make this useful rather than decorative.

**`parameters` carries resolved values, not configured ones.** If the opening radius is configured as 0.032 in and the solved scale is 600 DPI, the trace records `openRadius: 0.032 in = 19.2 px`. Section 3 makes the point that absolute thresholds are resolution-dependent bugs; the trace is how that gets caught, because a wrong scale becomes visible as an absurd pixel value rather than as a quietly poor result.

**`rejections` is the interesting half.** What the pipeline threw away and why is more informative than what it kept, and it is what turns "no shot detected on bull 12" from a mystery into "candidate at 0.14 in rejected, below the 0.15 in size gate, score 0.31".

**`decisions` records forks with their alternatives.** "Chose `D_chroma` because artwork mean chroma measured 112, above the 15 threshold; `D_lum` scored 0.05." That is the difference between a tool that is trusted and one that is merely used.

### 6.2 Artefacts by stage

| Stage | Artefacts |
|---|---|
| S0 | Decoded raster. Orientation-corrected overlay if a rotation was applied |
| S1 | Scale card: tag value, derived value, chosen value, method |
| S2 | Marker overlay, each marker boxed with its decoded identifier |
| S3 | Inlier and outlier overlay. **Residual heat map across the page**, which is the single most persuasive image the pipeline produces |
| S4 | Metadata comparison table. Print-scale statement |
| S5 | **The rendered expected artwork.** Also the observed and expected side by side, and a blink comparison. Exclusion zones drawn as hatched overlays |
| S6 | Each of `D_lum`, `D_chroma`, `D_neutral` as a heat map, with the chosen one marked |
| S7 | Post-opening mask, post-closing mask, hull overlay. Three images that make the morphology legible |
| S8 | Candidates coloured by score. Accepted, rejected and review-queue as three layers |
| S9 | Assignment overlay with a line from each shot to its bull. **Contested assignments drawn differently**, so a thin margin is visible rather than buried |
| S10 | Tile map showing which sheets were pooled and which are missing. Per-tile scale table. The composite group plotted with each shot coloured by its source sheet |

The S5 pair and the S6 heat maps are the artefacts that make the argument. Showing a user the target artwork vanishing and the holes remaining is a better explanation of render-and-difference than any paragraph.

### 6.3 Console form

The same records serialise to a text stream, which gives the Phase 0 and Phase 1 spikes their output for free.

```
[S0.decode    ]   142ms  ok        JPEG 4958x6458, EXIF orientation 1, no gain map
[S1.scale     ]     8ms  ok        600.0 DPI from JFIF density; provisional
[S2.fiducials ]   890ms  ok        34 markers sought, 31 decoded, 3 not found
[S3.register  ]    64ms  ok        homography, 29 inliers, 2 rejected
                                     residual rms 0.0007 in, max 0.0021 in
[S1.scale     ]     2ms  ok        599.4 DPI from fiducials; tag was +0.10% high
[S4.verify    ]     5ms  ok        printed at 100.0% of intended size
[S5.render    ]   210ms  ok        1 ring set, 30 bulls, ink spread sigma 1.8 px
[S6.difference]   180ms  ok        artwork chroma 4.1 -> greyscale, using D_lum
[S7.candidates]   240ms  ok        open r=0.032in (19.2px), 31 components
[S8.classify  ]    35ms  degraded  25 accepted, 4 rejected, 2 for review
                                     rejected (2.82, 8.78) marker caption, score 0.19
                                     review  (5.12, 3.40) diameter 0.38in vs median
                                             0.31in, possible ink over hole
[S9.assign    ]    12ms  ok        25 shots to 25 bulls, 1-to-1 matching
                                     1 contested: (6.04, 1.61) bull 9 over bull 4,
                                     margin 0.42 in
[S10.pool     ]     1ms  ok        single sheet, nothing to pool
```

On a tiled session the last line becomes:

```
[S10.pool     ]     4ms  degraded  5 of 6 tiles present, missing index 4
                                     scale by tile: 100.0 100.0 99.8 100.0 -- 100.0 %
                                     30 shots pooled from 36 bulls
                                     composite reported as 5 of 6 sheets
```

Three verbosity levels: summary lines only, plus decisions, plus rejections. `--trace <dir>` writes the artefacts alongside a `trace.json`.

### 6.4 The user-facing view

Two forms of the same data, and the second is the one that matters.

**A stage timeline** the user can scrub, with each stage showing its artefacts, its summary line, and its metrics. Clicking a rejection highlights it on the image. Clicking a detection walks back through the stages that produced it. Because the trace is a data structure rather than a log, all of that is a rendering problem and not a pipeline problem.

**A live run.** Stages complete in a few hundred milliseconds each, so a full analysis runs in a couple of seconds. Showing each stage's artefact as it lands, in order, is both honest and genuinely pleasant to watch: the markers light up, the residual map settles, the artwork vanishes, the holes emerge. That sequence is a real depiction of what happened, and the design does not need to add anything false to it.

Two things to guard against, which are worth writing into the specification rather than discovering in review. **The trace must never be the only place an error appears**: a failed stage produces a normal, prominent error, and the trace is the detail behind it. And **the animation must not slow the pipeline down**. If a user opens twenty targets, the trace should be collected and the theatre skipped. Artefact generation is the expensive part, so it should be a flag that defaults on for a single interactive analysis and off for batch.

### 6.5 Cost

Artefact rasters at full 600 DPI resolution would be roughly 25 MB each, and there are around sixteen of them per sheet. Storing them is not sensible.

- Retain artefacts at a **reduced working resolution**, 150 DPI, giving roughly 400 KB each, which matches the proof-image tier in DESIGN.md section 18.
- Retain the structured records always. They are kilobytes.
- Regenerate full-resolution artefacts on demand by re-running from the stored original when the user zooms in.

Per DESIGN.md section 18, traces are local by default and are not synchronised. They are a debugging and explanation artefact, not part of the record of a session.

---

## 7. Phase 1 gates, revised

DESIGN.md section 21 sets the Phase 1 gate as a measured detection rate and false-positive rate, published honestly, and requires render-and-difference to beat "the naive baseline of roughly 25 percent by a wide margin".

**That gate no longer means what it was written to mean.** A tuned Hough circle detector achieves 26 of 27 true positives with 4 false positives on the reference file, which is 96 percent. A gate of "beat 25 percent by a wide margin" would be cleared by a method that has no stable operating point, which is precisely the wrong thing to reward.

The measurements show what the real problem is. Hough at accumulator threshold 25 gets 26 true positives; at 40 it gets 10; at 55 it gets 0. A factor of 2.2 in one parameter takes it from near-perfect to blind, and the correct value depends on rim contrast, which varies by more than a factor of two with scan resolution and by 45 percent between holes on ink and holes on paper **within a single scan**. The naive methods are not bad at detecting holes. They are bad at detecting holes **without being tuned per image**, which is useless in a product.

**Proposed replacement gate. Render-and-difference must clear all five, on the full corpus, with one parameter set.**

| | Criterion | Rationale |
|---|---|---|
| G1 | **Recall at least 95 percent across all 15 files with a single parameter set, no per-image tuning** | Beats the tuned baseline while forbidding what makes the baseline useless |
| G2 | **False positives at most 1 per target, and zero from target artwork** | Both baselines already reject rings, numerals and dots on size. The remaining false positives are text, barcode and handwriting, and position priors should eliminate them |
| G3 | **Parameter stability: recall stays above 90 percent when every threshold is varied by plus or minus 30 percent** | This is the criterion the naive methods fail. It is the one that actually distinguishes a shippable detector |
| G4 | **Resolution invariance: same parameter set clears G1 at 93, 300 and 600 DPI** | Directly tests that no absolute intensity threshold survived. The corpus has all three |
| G5 | **Style invariance: recall at least 95 percent on `retumbo.png`, the greyscale render** | The hardest file. Chroma is unavailable, artwork and hole are the same grey. If it works here it works everywhere |

Report every number whatever it is, per DESIGN.md section 21. A published failure is worth more than an unpublished pass.

**Two measurements to take alongside the gate,** because they are cheap and they inform Phase 3:

- **Recall against review-queue size.** A detector at 92 percent recall that flags its own eight percent is better than one at 96 percent that silently misses four. DESIGN.md section 13 already builds the editor first for exactly this reason; this measurement quantifies the trade.
- **Centroid repeatability.** `retumbo.jpg` and `retumbo.png` are the same scan encoded twice. Re-detecting both gives a direct measurement of centroid noise with no ground truth required. The measurement harness saw 0.008 in and 10 grey levels of spread between them, which is its own noise floor; the production detector should be well inside that.

---

## 8. Secondary mode

DESIGN.md section 3 requires a mode that analyses any target, including store-bought targets and blank paper, with a user-defined scale and manual or assisted hole placement.

Render-and-difference is unavailable, since the artwork is unknown. What remains is everything in S7 and S8 that does not depend on the definition, and the measurements say that is a surprising amount.

The measurement harness's own detector, which had **no knowledge of the artwork**, achieved **25 true positives, 0 false positives, 2 false negatives out of 27** on `300_nm_hand_load.jpg` using only:

```
D_neutral = local paper level − max(R, G, B)
open r = 0.032 in  →  threshold  →  close r = 0.055 in  →  fill  →  convex hull
```

That is the secondary-mode detector, and it is already good. What it lacks relative to the primary mode is the position prior, the on-ink expectation, and the ability to subtract artwork it cannot predict, which is what would show up as false positives on a busier target than this one.

Scale in secondary mode comes from DESIGN.md section 11's three options, in descending order: a printable calibration strip taped beside the target, camera intrinsics where exposed, and guidance to stand farther back and zoom in. Assignment is manual, since without a definition there are no bulls and no cells.

**The `D_neutral` caveat carries over and must be stated in the interface.** It fails on blue artwork, measured Fisher 0.04, because blue ink's maximum channel is far below paper. A blue-printed store-bought target is the known weak case in secondary mode, and the mode should detect a strongly blue-biased artwork palette and warn rather than quietly under-detecting.

---

## 9. What the detector will not attempt

Per DESIGN.md section 12, with one addition.

**Resolving overlapping holes into separate shots.** On generated targets the one-shot-per-bull rule makes this unnecessary. In secondary mode the user places them by hand.

The corpus contains one instance: `300_nm_hand_load.jpg` bull 20, where two perforations overlap into a single blob. **The detector must not report this as one shot silently.** The right behaviour is to detect that a candidate's hull area substantially exceeds the expected single-hole area for the estimated caliber, and route it to the review queue as "possible multiple impact, place manually". That is the honest answer, it is cheap to implement, and it is exactly the failure mode DESIGN.md section 1 uses to justify the whole one-shot-per-bull design.

**Reading hand-drawn annotations.** The arrows and X marks are semantically meaningful and the pipeline cannot read them. It should not guess. It should, however, **detect that hand ink is present** and say so, since a target carrying arrows is a target where cross-cell assignment probably needs human review. Blue ink is separable on chroma at 97 to 164 against a hole annulus at 15 to 42. Black marker at chroma 0 to 11 on a greyscale target is not separable, and that limitation should be stated rather than papered over.

---

## 10. Changes this implies for DESIGN.md

| Section | Change |
|---|---|
| 6, Findings | Correct "every hole showed a bright core". The dark ragged annulus is universal; the core is 53 V darker than paper on average and reaches paper level in 24 percent of holes |
| 6, Findings | Correct the bull detection figure. A matched-filter finder gets 25 of 25 or 30 of 30 on every 600 DPI file, not 24 of 25 |
| 6, Findings | Add: the scanner crops inside the page on all 14 files, so no page edge exists in any of them. Registration cannot use the sheet outline |
| 12, Signatures | Remove "bright core on a scan". Remove "radial symmetry within tolerance". Add rim closure fraction, chroma deficit, and position prior |
| 12, Candidate classification | Add that colour separation is style-conditional, selected by measured artwork chroma, and that no channel works on a greyscale render |
| 12, Caliber estimation | Add the measured perforation-to-caliber ratios, 0.85 to 0.96, and note the constant absolute deficit |
| 12 | Add a subsection on ink over holes: detected, diameter inflated by 0.03 to 0.07 in, routed to review |
| 13, Assignment | State that the default is one-to-one matching under the one-shot-per-bull constraint, not nearest bull, and that the method must not be forced when counts disagree |
| 19, User interface | Add the stage timeline and live-run view as a first-class feature |
| 21, Phase 1 gate | Replace the "beat 25 percent" gate. The naive baseline reaches 96 percent when tuned per image. The five criteria in section 7 test stability instead |
| New | An observability contract section, or a reference to this document's section 6 |

---

## 11. Open questions

1. ~~What caliber are the `n568*` targets?~~ **Answered: .264, 6.5 mm.** The correction is folded into section 3, S8. It made the .264 and .308 pools agree to three decimal places and left one narrower question in its place: `n568.jpg` measures 1.121 of calibre against its siblings' 0.896 and 0.922. Was that file a different bullet, load or distance from `n568-gm210m` and `n568-ruag`, or genuinely just a different primer?

2. **Is the two-hole overlap at `300_nm_hand_load` bull 20 genuinely two shots?** The harness assumed so from the crop. Confirming it fixes the ground truth for the reference file.

3. **Would you transcribe the hand-drawn arrows once, by hand, into a fixture file?** They are the only independent ground truth for cross-cell assignment in the corpus. Without them, assignment accuracy is asserted rather than measured.

4. **Which backer material do you actually shoot against?** It is irrelevant to scans and decisive for photographs. Two photographs of the same target against two different backers would isolate the one variable the entire scan corpus cannot exercise.

5. **The 300 DPI file is the only one with 4:2:0 chroma subsampling**, which halves colour resolution. If that is a scanner setting rather than a one-off, it changes how much chroma can be relied on at 300 DPI, and it is worth knowing before the chroma path is tuned.

6. **In `blank` print mode, how much of the data block will actually be filled?** The exclusion zone makes handwriting harmless either way, but if most users leave it blank the block is wasted paper, and if most users fill it the instance code in `filled` mode is the more useful default. This is a question about how you shoot, not about the pipeline.

7. **Should a missing tile block the composite or degrade it?** Section 3, S10 currently degrades: it reports 30 shots from five of six sheets and says so. The alternative is to refuse until every tile is present. Degrading is more useful and slightly more dangerous, because a user who ignores the banner has a biased sample.
