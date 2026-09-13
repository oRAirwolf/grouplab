# GroupLab: Fiducial marker decision

**Prepared** 13 September 2026
**Revised** 13 September 2026, after the section 10 measurements were actually run. The family changed.
**Decides** Open item 6 of DESIGN.md section 23, "adopt an existing scheme or design a bespoke one"
**Depends on** PATENT-SEARCH.md, and the measurements in SCAN-MEASUREMENTS.md
**Status** Recommendation for approval. No application code written; the validation harness is `tools/fiducial/`.

---

## 1. Decision

**Adopt AprilTag `tag36h11`, printed at a 0.5 mm module, giving a 4.0 mm marker, on the cell-boundary lattice.**

| | Decision |
|---|---|
| **Family** | AprilTag `tag36h11`. 8 x 8 modules including the mandatory border, 587 identifiers, minimum Hamming distance 11, error correction capped at 5 bits |
| **Printed size** | 0.5 mm module, 4.0 mm marker, 1.0 mm quiet zone all round, 6.0 mm total footprint |
| **Count and placement** | Every intersection of the cell-boundary lattice. **34 markers** on the reference layout GL-CF25-LTR, 32 to 56 across the fourteen multi-bull sheets, 16 to 32 on the zeroing sheets, and **9 on a 300 yard tile** where the lattice has to be subdivided |
| **Detector, Windows** | OpenCV `DICT_APRILTAG_36h11` via **OpenCvSharp** (Apache-2.0), with corner refinement explicitly enabled |
| **Detector, mobile** | The **AprilTag reference implementation**, BSD-2-Clause, through a P/Invoke layer. No longer a deferred decision, and no longer dependent on Emgu.CV |

**This reverses the first draft of this document, which chose ArUco `DICT_6X6_250`.** The reversal came from running the two measurements section 10 said could be done in an afternoon. One of them destroyed the assumption the original decision rested on. Section 4 records what changed and section 4.1 has the measurements.

The short version: the two families are indistinguishable on every property GroupLab cares about, including a false-positive test on the real scan corpus where both scored zero, and `tag36h11` additionally has 587 identifiers against 250 and is the **only strong family that both OpenCV and the BSD-licensed AprilTag reference implementation can read**. Choosing it costs nothing and removes the Emgu.CV licence conflict of section 5 from the critical path entirely.

The bespoke option was considered seriously and is rejected in section 6.

---

## 2. What the requirement actually is, restated from the data

Before comparing families it is worth being precise about the job, because the answer changes once the measurements are in and it is not the job DESIGN.md assumed.

**GroupLab needs many precise point locations, not a few decoded identities.** This is the opposite of the robotics use case both families were designed for. A robot wants to know *which* tag it is looking at and *where that tag is in three dimensions*, from one or two tags, at range, in motion. GroupLab wants forty two-dimensional point correspondences on a flat sheet held still under a scanner lid. Identity matters only enough to disambiguate which marker is which.

**The distortion is small.** This is the finding that changes the argument. Measured on the worst sheet in the sample set, `IMG_20250530_0001`, which is crumpled, torn along two edges, taped and non-rectangular, the residual of a plain four-point homography is **rms 0.0027 in, maximum 0.0093 in**. Four pristine sheets measured 0.0026 to 0.0032 in. Independently, eleven printed rules traced over roughly 100 inches of line depart from straight by at most 0.0061 in, and two of the three flat sheets are worse than the crumpled one.

Paper, it turns out, is a developable surface. It bends, it does not stretch. A crumpled sheet flattened under a scanner lid is very nearly planar, and a homography absorbs it.

**So the original justification for thirty to forty markers is wrong.** DESIGN.md section 9 says forty markers are needed because "a homography needs four points; a radial distortion model wants roughly fifteen" and because the solution must "survive losing half the page". The first half of that does not survive contact with the data for the scan path. Four corners meet the Phase 0 gate on the worst sheet in the collection.

**Three justifications do survive, and together they are enough.**

*Availability.* The page outline is unusable as a reference. All fourteen 600 DPI scans in the sample set are cropped **inside** the sheet, so the paper edge is not even in the image, and on the 300 DPI scan the paper-versus-lid contrast is three grey levels. Registration must therefore come from printed marks. And a torn corner takes a printed mark with it: the sample set contains exactly that case. Four markers means one tear is a total failure. Forty means one tear is a shrug.

*Photographs, which are a different problem.* DESIGN.md section 11 fits a full lens model including radial distortion from the image itself, with no camera metadata. That genuinely wants fifteen or more well-distributed points, and preferably many more, because radial distortion is a function of radius from the optical centre and four corner points sample that function at one radius. The scan path does not need forty markers. The photograph path does.

*Overdetermination buys accuracy, not just robustness.* Thirty-four markers give 136 corners constraining an eight-parameter homography. Residual falls roughly as the square root of the point count, so 136 points rather than 16 is a factor of about 2.9 on the fit. Given a Phase 0 gate of one thousandth of an inch, which is 0.6 pixels at 600 DPI, that margin is worth having and it is nearly free.

**The honest summary is that the marker count is justified by robustness and by the photograph path, not by warp on scans.** DESIGN.md should be amended to say so, because a rationale that the project's own data contradicts is a liability the first time a contributor checks it.

---

## 3. Detection reliability at print resolution

### 3.1 The floor is set by the printer, not by the detector

This is the single most useful thing to understand about sizing, and it inverts the intuition.

Pixels available per marker module, page filling the frame:

| Imaging path | Effective ppi | 0.4 mm module | 0.5 mm | 0.6 mm | 0.8 mm |
|---|---|---|---|---|---|
| 300 DPI scan | 300 | 4.7 px | 5.9 px | 7.1 px | 9.4 px |
| 600 DPI scan | 600 | 9.4 px | 11.8 px | 14.2 px | 18.9 px |
| 12 MP phone, page fills frame | 367 | 5.8 px | 7.2 px | 8.7 px | 11.5 px |
| 48 MP phone, page fills frame | 733 | 11.5 px | 14.4 px | 17.3 px | 23.1 px |

Printer dots available per module:

| Print resolution | 0.3 mm | 0.4 mm | 0.5 mm | 0.6 mm |
|---|---|---|---|---|
| 300 DPI | 3.5 dots | 4.7 dots | 5.9 dots | 7.1 dots |
| 600 DPI | 7.1 dots | 9.4 dots | 11.8 dots | 14.2 dots |
| 1200 DPI | 14.2 dots | 18.9 dots | 23.6 dots | 28.3 dots |

Measured detection thresholds, from a controlled test run on OpenCV 4.13.0 with pixel-aligned synthetic markers on white, twenty identifiers per dictionary, default detector with the perimeter-rate gate lowered so it does not bind:

| Dictionary | Total modules | 10 px | 12 px | 14 px | 16 px | 18 px | 20 px |
|---|---|---|---|---|---|---|---|
| DICT_4X4_50 | 6 x 6 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| DICT_5X5_50 | 7 x 7 | 0.00 | 0.30 | 1.00 | 0.30 | 1.00 | 1.00 |
| DICT_6X6_250 | 8 x 8 | 0.00 | 0.75 | 0.10 | 1.00 | 1.00 | 1.00 |
| DICT_7X7_50 | 9 x 9 | 0.00 | 0.00 | 0.15 | 0.35 | 1.00 | 0.90 |
| DICT_APRILTAG_36h11 | 8 x 8 as OpenCV renders it | 0.00 | 0.05 | 0.00 | 1.00 | 1.00 | 1.00 |

Sizes are the whole marker in pixels, including the border. With Gaussian blur at sigma 0.8 and noise at sigma 8 added, 4x4 held above 0.85 from 12 px, while 6x6 and 36h11 needed 16 px.

The rule this supports is **roughly two pixels per module as an absolute floor and three pixels per module for reliability**, which agrees with the independent practitioner guidance from the Find-GCP photogrammetry tool ("minimum 20 x 20 pixels to be detected, optimal 30 x 30" for six-module markers). Note the non-monotonic dips, 5x5 at 16 px and 6x6 at 14 px: those are aliasing artefacts of pixel-aligned synthetic rendering and are a reminder that this test is a best case.

**Put the two tables together.** Three pixels per module at 600 DPI is a 0.13 mm module. Nothing about detection forces a module larger than that. What forces a larger module is **inkjet dot gain**: below about five printer dots per module, ink spread starts to close the gap between adjacent modules, the black-to-white transition shifts, and the fitted quad edge moves with it. Since the corner position is the measurement, a systematic edge shift is a systematic measurement error, not merely a detection risk.

**Therefore: 0.5 mm module.** That is 5.9 dots at 300 DPI and 11.8 at 600 DPI, which is safe on a consumer inkjet, and it yields 11.8 pixels per module on a 600 DPI scan and 7.2 on a 12 MP phone photograph filling the frame. Both are comfortably above the three-pixel reliability threshold, with the phone path retaining margin for a user who frames loosely.

The evidence base for the print side is thin and should be recorded as such. There is no peer-reviewed literature on fiducial marker survivability under printing, dot gain, paper stock, moisture or UV. The available guidance is practitioner documentation: TagSLAM insists on matte rather than glossy stock and on a white frame of about two modules, calling it "crucial for detection"; the FIRST Robotics tag guide explicitly advises **against** lamination and sheet protectors because of specular reflection, recommending a rigid backing instead. Both are relevant. Neither is a measurement.

### 3.2 Scans are the easy case, and the defaults are wrong for them

Nothing published addresses square fiducial detection from flatbed scans as distinct from camera capture, which is a gap worth naming because the scan is GroupLab's primary path.

A 600 DPI flatbed scan has near-zero perspective, no motion blur, uniform illumination and, at 0.5 mm modules, nearly twelve pixels per module. It is a far easier problem than the camera case both families were tuned for. That has one non-obvious consequence: OpenCV's `adaptiveThreshWinSizeMax` default of 23 pixels is sized for camera-scale markers and is far too small relative to a 600 DPI marker, where the whole marker is 94 pixels across. Detection on scans should either downsample first or widen the adaptive-threshold window, and Phase 0 should test both.

Two further defaults must be changed and both are easy to miss:

- **`cornerRefinementMethod` defaults to `CORNER_REFINE_NONE`.** Sub-pixel refinement is off. For a system whose gate is 0.6 pixels of residual at 600 DPI, shipping with integer-ish corners would be a silent, comprehensive failure. Set `CORNER_REFINE_SUBPIX`, and evaluate `CORNER_REFINE_CONTOUR` against it in Phase 0.
- **`errorCorrectionRate` defaults to 0.6.** For `DICT_6X6_250`, whose maximum correction is 5 bits, that permits 3 bits of correction. Reasonable. Section 3.3 explains why this parameter deserves a second look for one particular dictionary.

### 3.3 Choosing the dictionary

The requirement is 42 identifiers on the reference layout, with headroom for larger sheets. Options, with module counts computed directly from OpenCV 4.13.0's own dictionaries rather than from documentation:

| Dictionary | Total modules | Identifiers | Min Hamming | Max correction | Marker at 0.5 mm | Verdict |
|---|---|---|---|---|---|---|
| DICT_4X4_50 | 6 x 6 | 50 | 4 | 1 bit | 3.0 mm | Smallest, weakest. 50 identifiers is no headroom |
| DICT_5X5_50 | 7 x 7 | 50 | 8 | 3 bits | 3.5 mm | Good code, no headroom |
| DICT_5X5_250 | 7 x 7 | 250 | 6 | 2 bits | 3.5 mm | Reasonable compromise |
| DICT_6X6_250 | 8 x 8 | 250 | 11 | 5 bits | 4.0 mm | Chosen in the first draft, superseded by section 4 |
| **APRILTAG_36h11** | **8 x 8** | **587** | **11** | **5 bits** | **4.0 mm** | **Chosen.** Same code strength and footprint, more identifiers, two independent readers |
| APRILTAG_36h10 | 8 x 8 | 2320 | 10 | 4 bits | 4.0 mm | More identifiers still, but not in libapriltag, so it forfeits the whole reason for the change |
| DICT_ARUCO_MIP_36h12 | 8 x 8 | 250 | 12 | see below | 4.0 mm | Marginally better code, one caveat |
| DICT_7X7_50 | 9 x 9 | 50 | 19 | 9 bits | 4.5 mm | Overkill, and needed 18 px to detect |
| DICT_ARUCO_ORIGINAL | 7 x 7 | 1024 | **1** | 0 | 3.5 mm | **Never use.** Hamming distance 1 |

Every Hamming figure in that table for a dictionary OpenCV ships was recomputed from rendered markers in section 4.1 and agrees.

A minimum Hamming distance of 11 is the requirement, because the false-positive cost is asymmetric and high. A false marker detection injects a wrong correspondence into the fit. RANSAC is there precisely to reject it, and with 34 markers where 4 would do there is enormous redundancy, so a single false positive is survivable. But the page is covered in exactly the things that generate false quadrilaterals: printed rings, numerals, cell boundaries, the ruled cells of a load-data block, handwritten marker ink, and bullet holes. Hamming distance 11 makes an accidental valid decode essentially impossible; Hamming distance 4 with one bit of correction does not. Section 4.1 measures this rather than assuming it: under a permissive detector the 4 x 4 and 5 x 5 dictionaries each produced false positives on real target artwork, and every dictionary at Hamming 11 or above produced none.

The identifier ceiling also matters. Thirty-four markers on Letter, and 56 on the densest rimfire layout, scale to well over a hundred on Tabloid with a denser lattice, and identifiers must be unique across the sheet, or across the whole assembly on a tiled target. Fifty is not enough. Two hundred and fifty is enough for every sheet but tight for a large assembly, which is why TARGET-SCHEMA.md section 3.7 had to specify a wrap-and-disambiguate rule. Five hundred and eighty-seven makes that rule a formality.

**A caveat worth recording about `DICT_ARUCO_MIP_36h12`**, which is the ArUco authors' own recommended dictionary and is marginally better on paper, at Hamming 12 in the same 8 x 8 footprint. Its `maxCorrectionBits` field in OpenCV reads 12, where every other dictionary stores the correct floor((tau - 1) / 2). If that value is used as intended, the default `errorCorrectionRate` of 0.6 would permit 7 bits of correction on a code with a minimum distance of 12, which is beyond the unambiguous decoding radius of 5 and would manufacture false positives. This may be a metadata defect or it may be a field with different semantics for that dictionary. Until somebody checks, it is not worth the investigation, and section 4 has made the point moot: `tag36h11` gives Hamming 11 in the same printed area with a correctly stated correction cap of 5 bits and a second reader.

### 3.4 Do the corners meet the accuracy gate?

The Phase 0 gate is one thousandth of an inch of residual across the page, which at 600 DPI is **0.6 pixels**.

At 0.5 mm modules the marker is 4.0 mm, which is 94 pixels across on a 600 DPI scan. Sub-pixel corner refinement on a high-contrast quad edge with that much support typically delivers 0.1 to 0.3 pixels of corner localisation error. Thirty-four markers give 136 corners constraining eight homography parameters, a ratio of 17 to 1, so the fit residual should sit comfortably below the per-corner error rather than above it.

The arithmetic says the gate is reachable with room to spare. Phase 0 exists to find out whether the arithmetic is right, and the honest position is that this is a prediction, not a result.

One measurement worth taking in Phase 0 that is not in DESIGN.md: **the residual as a function of marker count**, computed by refitting with random subsets of the detected markers. That single curve answers the availability question empirically, tells you how many markers may be lost before the gate fails, and would let a later revision reduce the count if thirty-four proves to be more than the job needs. It matters more now than when it was first written, because the 300 yard tile carries **nine** markers and the question of whether nine is enough is exactly what that curve answers.

---

## 4. Why the family changed, and the measurements that changed it

The first draft of this document chose ArUco `DICT_6X6_250` and rejected AprilTag. The rejection rested on four claims. Three were about the .NET ecosystem and remain true. The fourth was load-bearing, and it was false.

**What the first draft said, and what is now known.**

| Original claim | Status |
|---|---|
| There is no maintained C# binding for AprilTag; a NuGet search returns zero packages | **True, unchanged.** Choosing AprilTag still means a P/Invoke layer over the BSD-2-Clause C library |
| OpenCV cannot decode `tagStandard41h12`, the best AprilTag family, because it places data bits outside the black quad | **True, unchanged.** That family stays unavailable through the only route with working .NET bindings |
| Restricted to what OpenCV can decode, `tag36h11` is 10 x 10 modules against `DICT_6X6_250` at 8 x 8, so 25 percent more linear size and 56 percent more area | **False at GroupLab's geometry.** Measured, OpenCV renders `DICT_APRILTAG_36h11` as 6 x 6 data in an 8 x 8 footprint, exactly the same as `DICT_6X6_250`. The 10 x 10 figure counts AprilTag's recommended one-module white surround, which GroupLab already prints as part of its 1.0 mm quiet zone. Section 4.1 confirms both detectors read the marker with a one-module quiet zone even against solid black artwork outside it |
| The AprilTag reference implementation ships the ArUco dictionaries as families, including `tagAruco6x6_250`, so a future AprilTag-based detector could read the markers chosen here without reprinting | **False.** libapriltag 3.1.0 ships exactly eight families: `tag16h5`, `tag25h9`, `tag36h11`, `tagCircle21h7`, `tagCircle49h12`, `tagCustom48h12`, `tagStandard41h12`, `tagStandard52h13`. None is an ArUco dictionary. The containment runs the other way: OpenCV's aruco module ships AprilTag families |

That last row is the whole reversal. The first draft called it "the best thing in this document" and said it "turns the deferred mobile decision from a risk into a genuinely open option". It was the reason the mobile detector could be left undecided. It does not exist.

**What follows from that.** With `DICT_6X6_250` printed on the sheet, the only software that can read a GroupLab target is OpenCV. On Windows that is fine, because OpenCvSharp is Apache-2.0. On Android and iOS the only OpenCV binding is Emgu.CV, which is plain GPL-3.0 with no app-store additional permission, for the reasons section 5 sets out. So `DICT_6X6_250` puts a licence conflict on the critical path for Phases 6 and 8 with no escape route short of porting six to eight thousand lines of numerical C.

With `tag36h11` printed on the sheet, two independent implementations can read it: OpenCV everywhere, and the BSD-2-Clause reference library through P/Invoke on mobile. The licence conflict stops being a blocker and becomes a preference.

**The intersection is the choice.** OpenCV ships four AprilTag families and libapriltag ships eight. Only three are in both, and `tag36h11` is decisively the strongest of the three.

| Family | Codes | Total modules | Min Hamming | In OpenCV | In libapriltag |
|---|---|---|---|---|---|
| `tag16h5` | 30 | 6 x 6 | 5 | yes | yes |
| `tag25h9` | 35 | 7 x 7 | 9 | yes | yes |
| `tag36h10` | 2320 | 8 x 8 | 10 | yes | **no** |
| **`tag36h11`** | **587** | **8 x 8** | **11** | **yes** | **yes** |

Thirty identifiers is not enough for a 56-marker sheet. Thirty-five is not either. Five hundred and eighty-seven is, with room for the largest tiled assembly the format can express.

**Head to head with the family it replaces**, every figure measured rather than quoted:

| | `DICT_6X6_250` | `tag36h11` |
|---|---|---|
| Data bits | 6 x 6 | 6 x 6 |
| Total modules with border | 8 x 8 | 8 x 8 |
| Printed footprint at 0.5 mm | 4.0 mm plus quiet zone | 4.0 mm plus quiet zone |
| Identifiers | 250 | **587** |
| Minimum Hamming distance | 11 | 11 |
| Correctable bits | 5 | 5 |
| False positives on the scan corpus | 0 | 0 |
| Smallest reliable printed size | 28 px per marker | 28 px per marker |
| Works with a 1-module quiet zone | yes | yes |
| Readable by OpenCV | yes | yes |
| Readable by a BSD-licensed detector | **no** | **yes** |

It is the same marker, in the same space, with more identifiers and one more way to read it.

**What AprilTag also brings, which the first draft already credited.** Published false-positive rates, which ArUco does not have: Wang and Olson measured 36h11 with up to two bits corrected at 0.000044 percent across 421,049 LabelMe images. Better rotational accuracy in the 2026 FMAC ray-traced benchmark, about 0.1 degrees on all axes. Neither of those decided anything here, but neither argues the other way.

**The cost of the change.** Three documents change a string and one enum value: `fiducials.family` becomes `apriltag-36h11`, which was already value 7 in the GLTD-B family table because the format was written to allow exactly this. No geometry changes, no layout changes, no marker count changes, no page-area change. Nothing printed yet, so nothing is stranded.

### 4.1 The measurements

Run by `tools/fiducial/validate.py` against the fifteen scans in `scans/`, with OpenCV 5.0.0, libapriltag 3.1.0 and `segno`. The full suite is `tools/fiducial/run_all.sh`.

**False positives on real target artwork.** None of the corpus scans carries a fiducial marker, so every detection is a false positive, generated by printed rings, numerals, barcodes, cell rules, handwritten marker ink and bullet holes. This is the population that matters and it needed no printer.

| Dictionary | Default params | Strict | Loose | Loose at 1600 px | Loose at 900 px |
|---|---|---|---|---|---|
| DICT_4X4_50 | 0 | 0 | 0 | **1** | 0 |
| DICT_4X4_100 | 0 | 0 | **3** | **1** | 0 |
| DICT_5X5_100 | 0 | 0 | **1** | 0 | 0 |
| DICT_6X6_250 | 0 | 0 | 0 | 0 | 0 |
| DICT_7X7_250 | 0 | 0 | 0 | 0 | 0 |
| **APRILTAG_36h11** | **0** | **0** | **0** | **0** | **0** |

Sixteen files, 665 quadrilateral candidates found and rejected per dictionary at default parameters. **At default parameters no dictionary produced a single false positive**, which is a better result than this document assumed when it argued from Hamming distance alone.

The informative column is "loose", a deliberately permissive detector with full error correction, a tolerant border check and a wide adaptive threshold window. There the weak dictionaries fail and the strong ones do not, exactly as the Hamming argument predicts. Every one of the five false positives is a **fragment of a printed ring stroke** read as a foreshortened quad, which is the failure mode section 3.3 named.

**Minimum Hamming distance, computed from rendered markers** rather than quoted. Worth stating that the first attempt at this computation unpacked OpenCV's rotation-interleaved `bytesList` incorrectly and returned 1 for `DICT_4X4_50`, whose true distance is 4. Reading the bits back out of the rendered image is slower and unambiguous.

| Dictionary | Codes | Data | Total | Min Hamming | Correctable |
|---|---|---|---|---|---|
| DICT_4X4_50 | 50 | 4 x 4 | 6 x 6 | 4 | 1 |
| DICT_4X4_100 | 100 | 4 x 4 | 6 x 6 | 3 | 1 |
| DICT_5X5_100 | 100 | 5 x 5 | 7 x 7 | 7 | 3 |
| DICT_6X6_250 | 250 | 6 x 6 | 8 x 8 | 11 | 5 |
| DICT_7X7_250 | 250 | 7 x 7 | 9 x 9 | 17 | 8 |
| APRILTAG_36h11 | 587 | 6 x 6 | 8 x 8 | 11 | 5 |

The Hamming 11 figure this document has claimed for `DICT_6X6_250` since the first draft is **confirmed**, and `tag36h11` matches it in the same footprint with more than twice the identifiers.

**The false positives are degenerate quads, which gives a second and independent line of defence.** Measured across all five:

| | Worst false positive | A real 4.0 mm marker at 600 DPI |
|---|---|---|
| Longest side over shortest side | 4.57 | 1.0 head on, under about 1.5 at any realistic tilt |
| Shorter diagonal over longer | 0.25 | close to 1.0 |
| Area | 1366 px² | 8928 px² |
| Shortest side | 30.6 px | 94.5 px |

A minimum-area gate at half the expected marker area, or a side-ratio gate at 2.0, rejects every one of them without reference to the dictionary at all. The detector should apply both, because they fail differently from the code check and a defence that fails differently is worth more than a stronger version of the same defence.

**Detection against printed size and degradation.** Four markers per cell, blurred, noised and tilted. A 4.0 mm marker is 95 px at 600 DPI, 47 px at 300 DPI, and roughly 37 px in a phone photograph where the sheet fills a 4000 px frame.

| Marker side | Clean | Blur 2, noise 4 | Plus 15 percent tilt | Blur 4, noise 8, 25 percent tilt |
|---|---|---|---|---|
| 95 px, 600 DPI | 4/4 | 4/4 | 4/4 | 4/4 |
| 64 px | 4/4 | 4/4 | 4/4 | 4/4 |
| 47 px, 300 DPI | 4/4 | 4/4 | 4/4 | 3/4 to 4/4 |
| 37 px, phone | 4/4 | 4/4 | 4/4 | 0/4 |
| 28 px | 4/4 | 4/4 | 4/4 | 0/4 |
| 20 px | 4/4 | 3/4 | 0/4 to 2/4 | 0/4 |

Both families give the same table to within one cell. **The 4.0 mm marker has roughly a factor of two in hand** before size becomes the limiting factor, and the thing that actually breaks detection is the combination of heavy blur with tilt, not size alone. That is a photograph-path finding rather than a scan-path one.

**The 1.0 mm quiet zone is enough.** Two modules of guaranteed white at a 0.5 mm module, with solid black artwork immediately outside it, at 6, 8 and 12 pixels per module: 12 of 12 for `tag36h11` under both detectors, 12 of 12 for `DICT_6X6_250` under OpenCV. It still works at a single module. This is what settles the footprint question in the table above, and it means the quiet zone is sized by the layout validator's clearance rule rather than by the detector.

**Cross-family decode.** Every combination of printed family against detector family, one per process because the AprilTag binding aborts if several family tables are allocated in one process:

| Printed | AprilTag `tag36h11` | `tag25h9` | `tag16h5` |
|---|---|---|---|
| ArUco `DICT_6X6_250` | none | none | none |
| ArUco `DICT_4X4_50` | none | none | none |
| AprilTag `tag36h11` | **id 7** | none | none |

Families are not interchangeable, and the escape route the first draft relied on is not there.

---

## 5. Library availability, and a licence problem that has to be raised

This section is uncomfortable and it is the reason section 1 defers the mobile detector.

### 5.1 What exists

| Wrapper | Licence | Windows | Android | iOS | ArUco included | Notes |
|---|---|---|---|---|---|---|
| **OpenCvSharp4** 4.13.0 | Apache-2.0 | yes, x64 and arm64 | **no** | **no** | yes in `runtime.win`, **no** in `runtime.win.slim` | README states it will not work on Xamarin platforms. 38 MB native |
| **Emgu.CV** 4.13.0 | **GPL-3.0** or paid commercial | yes | yes | yes, genuine static `.a` and `.xcframework` | yes, in the main assembly via `objdetect` | 82 MB Windows runtime |
| OpenCV.Net 3.4.2 | MIT | yes | no | no | **no**, wraps the C API only | Last published 2023 |
| Aruco.Net 3.0.0 | MIT | yes | no | no | yes | Wraps the native ArUco C++ library, depends on OpenCV.Net, last release 2022, net462 only |

**There is no pure-managed C# detector for either family.** This was checked directly against the NuGet search index rather than inferred.

**A trap worth writing down before somebody falls into it.** `OpenCvSharp4.runtime.win.slim` lists `objdetect` as enabled in its package description, which reads as "ArUco works". It does not. The native build sets `NO_CONTRIB=ON` and all 77 ArUco P/Invoke exports are compiled out, so calls throw `EntryPointNotFoundException` at run time and only at run time. The full 38 MB `runtime.win` is required.

### 5.2 The licence problem

DESIGN.md section 20 chooses GPL-3.0 **with an additional permission under section 7 permitting distribution through app stores**, and gives the correct reason: plain GPL-3.0 conflicts with Apple's terms, and GPL applications have been removed from the App Store before.

**Emgu.CV's open-source licence is plain GPL-3.0 with no such additional permission.**

That matters because of how GPL section 7 works. A copyright holder may grant additional permissions covering their own code. A downstream distributor may **remove** additional permissions, but cannot **add** them to somebody else's code. GroupLab can grant an app-store exception for GroupLab's own source. It cannot grant one covering Emgu.CV. Shipping GroupLab plus Emgu.CV through the App Store would therefore reproduce exactly the conflict the licence choice was designed to avoid.

This is precisely the failure mode DESIGN.md section 20 anticipated when it said adding the permission later "would require the agreement of every contributor". Here the contributor is a company with a commercial licensing business, and the whole point of its dual licence is that the GPL side is deliberately inconvenient.

**It does not block anything now, and after the section 4 family change it does not block anything later either.** Emgu.CV was only ever needed for Android and iOS, Phases 6 and 8, because it was the only OpenCV binding covering them and OpenCV was the only software that could read a `DICT_6X6_250` marker. Printing `tag36h11` instead means the mobile detector can be the AprilTag reference implementation under BSD-2-Clause, which imposes no distribution restriction at all. Emgu.CV becomes one option among several rather than the only one.

The problem is recorded here in full anyway, for two reasons. It is the reason the family changed, so removing it would make the reversal in section 4 unintelligible. And it still applies to any future use of OpenCV on mobile, for instance if the hole detector wants an OpenCV routine that libapriltag does not provide, which is a live possibility for Phase 6 and is exactly the kind of thing that gets rediscovered painfully.

### 5.3 The ways out, for the record

The family change in section 4 adds a fifth option and makes it the plan of record: **print `tag36h11` and use the BSD-2-Clause AprilTag reference implementation on mobile**, which needs no permission from anybody. The original four remain relevant only if OpenCV turns out to be needed on mobile for something other than marker detection.

1. **Ask Emgu Corporation for an additional permission.** Cheapest if it works. A polite, specific request from a free GPL-3 project, referencing the app-store exception by name, costs one email. Worth sending early, because the answer changes the plan.
2. **Buy an Emgu commercial licence.** Solves it outright and contradicts DESIGN.md section 3's no-paid-tier, no-commercial-offering stance only in that somebody has to pay. For a project with no revenue, this is a personal expense.
3. **Do not ship to the App Store.** Android allows sideloading and F-Droid; Windows is unaffected. This costs the iOS audience, which is a real cost for a phone-camera workflow.
4. **Port an AprilTag-style detector to managed C#.** The reference detector core is about 4 KLOC of BSD-2 C (`apriltag.c` at 1,553 lines and `apriltag_quad_thresh.c` at 2,017), plus perhaps 2 to 4 KLOC of supporting containers: `zarray`, `unionfind`, `matd`, `svd22`, `g2d`, `homography`, `image_u8`, `math_util`. The pipeline is adaptive threshold, connected components by union-find, boundary clustering, line and quad fitting, homography, bit sampling, Hamming decode against a family table.

Option 4 is now much cheaper than it was, because the marker family is one libapriltag already implements: a managed port would be a port of code that is known to read GroupLab's own targets, rather than a port that also has to acquire an ArUco family table. It deserves more than a mention, because it dissolves five separate problems at once: the licence conflict, iOS static linking and AOT, trimming, the single-file executable size that DESIGN.md section 20 cares about, and the dependency on any one library being present, which DESIGN.md section 7 already lists as an architectural goal for the imaging backend. It would also unlock `tagStandard41h12`. And a managed port would decode the markers chosen in this document without reprinting a single target, which after section 4 is true because the family is `tag36h11` and not because of anything the AprilTag repository does with ArUco dictionaries. It does not ship them.

It is six to eight KLOC of numerical C to port and validate, which is not small. But DESIGN.md section 22 already lists "imaging library availability differs across platforms" as a risk and specifies the mitigation as "the fiducial design avoids depending on any single library's marker implementation". Option 4 is what that mitigation looks like when it is actually built. It should be scoped properly at Phase 5 or 6, not decided now.

---

## 6. Why not a bespoke scheme

A bespoke design was considered seriously. For GroupLab's actual requirement, as restated in section 2, it is genuinely attractive, and the reasoning for rejecting it is worth recording because it is not obvious.

**The case for bespoke.** GroupLab needs precise point locations, not decoded identities. A field of plain filled circles located by intensity-weighted centroid gives better sub-pixel accuracy than a corner does, because a centroid averages over hundreds of pixels while a corner is a two-line intersection. Circles have no orientation to get wrong, no bit sampling to alias, degrade gracefully under blur and dot gain rather than failing a checksum, and print far smaller than any coded marker. Identity can come from position within a solved lattice rather than from decoded bits, which is viable because the lattice is known from the definition. And, as PATENT-SEARCH.md section 3.1 notes, a scheme that does not perform edge detection, polygon grouping, per-candidate homography and checksum-verified bit extraction is on its face outside claim 12 of US7769236B2.

**The case against, which wins.**

*Bootstrapping is a genuinely hard problem, and it is the whole problem.* Position-based identity requires a solved lattice, and solving the lattice requires knowing which dot is which. With a complete, undamaged dot field this is easy. With a torn corner, three missing dots, a shot through the field and ink over two more, it becomes a combinatorial correspondence search. Coded markers make this trivial because every marker states its own index. That is not a convenience; it is the property DESIGN.md section 9 specifically asked for when it wrote "each marker encodes its own index so that a partial set remains unambiguous". Sacrificing it to save printed area is a bad trade.

*Validation cost is the real expense.* An adopted scheme arrives with a decade of published false-positive analysis, a reference implementation, and thousands of users who have found its failure modes. A bespoke scheme arrives with none of that, and GroupLab would have to establish its reliability itself, on its own test set, as project work. DESIGN.md section 22 already identifies detection accuracy as "the project". Adding a second unvalidated detection problem alongside the hole detector, when a validated one is available, is the wrong allocation of the only scarce resource here, which is the author's time.

*It does not save enough.* Section 7 puts the whole marker field at 2.27 percent of a Letter sheet. A bespoke dot field might halve that. Saving one percent of a page is not worth a bespoke detection subsystem.

*The patent argument is weaker than it looks.* Designing around a claim on the basis of a lay reading is not a defence. If the claim genuinely bites, the sound response is advice, not a redesign chosen by a non-lawyer. That decision has been made separately and is recorded in section 9.

**The middle option, also rejected but worth naming.** Coded markers at the lattice corners for unambiguous bootstrapping, plus a dense field of plain dots for precision, taking identity from the solved lattice. This is technically the best of both and is what a mature version of this system might eventually look like. It is rejected for version one on complexity grounds: two detectors, two failure modes, two things to validate, to buy accuracy that section 3.4 suggests is already sufficient. It is worth revisiting if and only if Phase 0 misses the accuracy gate. The format supports it without change, since `fiducials.scheme` is a versioned string and `grid-boundary-2` can mean whatever a later revision needs.

---

## 7. Page area consumed

Marker field, at the 34 markers the validator places on GL-CF25-LTR, 8 x 8 modules plus a 2-module quiet zone on each side, so 12 modules of footprint:

| Module | Marker | Footprint | 34 markers | Percent of Letter |
|---|---|---|---|---|
| 0.4 mm | 3.2 mm | 4.8 mm | 1.36 in² | 1.45 % |
| **0.5 mm** | **4.0 mm** | **6.0 mm** | **2.12 in²** | **2.27 %** |
| 0.6 mm | 4.8 mm | 7.2 mm | 3.05 in² | 3.27 % |
| 0.8 mm | 6.4 mm | 9.6 mm | 5.43 in² | 5.81 % |

QR codes. The largest payload in the library measures **85 bytes**, from the reference encoder in `tools/gltd/encode.py`, and the library standardises on a **version 10 symbol at error correction level H**, 57 modules square, byte-mode capacity **119 bytes** (verified against the `segno` library, not read from a table), leaving 34 bytes spare on the worst sheet and 49 on the reference one. With a 4-module quiet zone each side:

| Module | Symbol | Footprint | 4 codes | Percent of Letter |
|---|---|---|---|---|
| 0.3 mm | 17.1 mm | 19.5 mm | 2.36 in² | 2.5 % |
| **0.4 mm** | **22.8 mm** | **26.0 mm** | **4.19 in²** | **4.5 %** |
| 0.5 mm | 28.5 mm | 32.5 mm | 6.55 in² | 7.0 % |

**Recommended combination: 0.5 mm marker modules and 0.4 mm QR modules, for 6.09 in², or 6.51 percent of a Letter sheet.** That is the figure the layout validator reports for GL-CF25-LTR, and it ranges from 0.83 percent on a 24 inch roll and 3.55 percent on A3 to 7.82 percent on the densest rimfire layout.

The version-10 decision cost 0.9 percent of a Letter page against version 8 and bought a single code size for the whole library. Version 8 would have fitted ten of the fourteen multi-bull layouts and none of the four zeroing sheets, which means either two code sizes and two sets of layout constants, or a library where the sheets with the most to say are the ones that cannot say it.

The QR module can be smaller than the fiducial module because the two have different jobs. A QR needs only to decode, and Reed-Solomon at level H tolerates a great deal of dot gain. A fiducial corner *is the measurement*, so an edge shifted by ink spread is an error rather than a nuisance. 0.4 mm is 4.7 dots at 300 DPI, which is marginal, and 9.4 at 600 DPI, which is fine. If the target audience prints at 300 DPI, use 0.5 mm QR modules and accept 7.6 percent total.

**This is more than DESIGN.md estimated and the estimate should be corrected.** Section 9 says four codes cost "about 1.5 square inches out of 93". That implies 0.61 inch codes, which even for a version 8 symbol means a 0.32 mm module, or 3.7 dots at 300 DPI. Achievable at 600 DPI print, not at 300. The honest number for a design that prints reliably on a consumer inkjet is 4.2 to 6.6 square inches for the codes, and 6.1 to 7.3 square inches for codes and markers together.

Six to eight percent of a Letter page for complete self-description, automatic distortion correction and print-scale verification is a good trade, and it is a better story told accurately than told optimistically. On large format it is under four percent, and on roll media under one, because the cost is absolute while the page grows.

---

## 8. Placement, and what the renderer must guarantee

**Rule `grid-boundary-1`**, referenced by `fiducials.scheme` in TARGET-SCHEMA.md and immutable once published:

1. Markers are placed at every intersection of the cell-boundary lattice, which for an `n` by `m` grid is `(n+1)` by `(m+1)` positions, plus a row bounding the sighter band. Lattice points that would clash with a code quiet zone, a printed disc, the safe margin, a declared data block, or another marker are dropped. The reference layout GL-CF25-LTR yields 42 lattice points, of which **34 survive**; across the fourteen multi-bull sheets the surviving count runs from 32 to 56.
2. Identifiers are assigned row-major from 0, so identity is derivable from position and position from identity.
3. Marker centres sit exactly on lattice intersections, which are the points furthest from every bull centre and therefore least likely to be shot.
4. The renderer must verify that no marker footprint, including its quiet zone, overlaps any ring, cell line, label, or QR quiet zone. TARGET-SCHEMA.md section 10 makes this a hard error.
5. Markers are printed pure black, at 100 percent coverage, with no anti-aliasing and no halftone screening. A halftoned marker module is a grey stipple, and its edge position becomes a function of the screen phase.

**Rule `grid-boundary-half-1`**, added after the layout validator found that `grid-boundary-1` degenerates at coarse pitch. Identical to `grid-boundary-1` in every respect except that the lattice steps by half a pitch rather than a full pitch in both axes, requiring the pitch to be divisible by 4 dmm so the offsets stay integer.

The failure it fixes is worth recording, because it is the kind of thing a rule looks immune to until it is measured. On the 300 yard tile, a 2 by 3 grid at 101.6 mm pitch, the full lattice offers twelve candidate positions and the 38.1 mm outer discs knock out all but **two**. Two markers is four corners, which is formally enough for a homography and nowhere near enough for a trustworthy one, and nothing in the rule as written raises an alarm: it drops the clashing points exactly as specified and returns a valid answer. Halving the step raises the candidates to 35 and leaves **nine** surviving markers spread across the sheet. TARGET-SCHEMA.md section 7 now makes fewer than four survivors an error and fewer than eight a warning, so the next layout that trips this fails loudly.

**Rule `field-ring-1`**, for the zeroing sheets, which have one bull and therefore no useful bull lattice. Markers are placed in the clear band around the declared measurement grid, on the extensions of the grid's major lines, plus one at each corner of the band. The same drop test applies. The four built-in zeroing sheets carry 16 to 32 markers each.

Two additions to what DESIGN.md specifies:

**Use the QR finder patterns as a fallback registration source.** DESIGN.md section 9 says the QR codes perform no registration work, and as a primary design that is right: separating identity from geometry is clean, and the codes sit in the corners where tearing is likeliest. But a QR finder pattern is an excellent, precisely localisable feature, and every decoder already returns its position. When too few markers survive to fit a homography, four finder patterns from one surviving code are better than failing. This costs nothing, changes no artwork, and only ever runs in the degraded case. Recommend adding it as an explicit fallback rather than a primary path.

**Print marker identifiers in tiny human-readable text beside a few markers.** Roughly four of them, at the page corners. When somebody reports that registration failed on a particular sheet, being able to say "marker 0 is at the top left" from a photograph of the sheet is worth the eight square millimetres.

---

## 9. The patent position, recorded plainly

PATENT-SEARCH.md section 3.1 identifies **US7769236B2** (Fiala, assigned to Millennium Three Technologies), live until 3 May 2029 with all maintenance fees paid, whose method claim 12 describes detecting a coded square marker in an image by edge detection, grouping edges into polygons, computing a homography per candidate, extracting binary data, and verifying with checksum and error correction. On a lay reading that is how both ArUco and AprilTag detectors work on a still image.

**You have decided to adopt ArUco and accept this risk.** That decision is recorded here rather than argued, and the document is written to it.

What the file should also record, so that the decision is legible later:

- Neither open-source licence helps. AprilTag's BSD-2-Clause contains no patent language at all, neither grant nor retaliation. OpenCV's Apache-2.0 does contain an express patent grant, but only from OpenCV's own contributors, and Millennium Three is not one.
- The patent expires **3 May 2029**, which on DESIGN.md's build plan is around Phase 4 to 6. Exposure is bounded and shrinking.
- The claim is nineteen years past its priority date in a field with substantial pre-2005 art, including references cited in the patent itself.
- Nothing about the printed target changes if this position is ever revisited. The exposure, if any, is in the **detector**, not in the artwork. A managed detector built along different lines, or the passage of May 2029, resolves it without reprinting anything or invalidating any target already shot and archived. That is a genuinely comfortable position to be in and it is worth noting explicitly.
- The one action still worth taking is the cheap one: docket a note to re-check the patent's status before the first public release, in case anything changes.

---

## 10. What Phase 0 must measure

DESIGN.md's Phase 0 gate is registration residual under one thousandth of an inch across the whole page, on both a scan and an off-axis photograph, plus correct detection of a deliberately mis-scaled print. These are the additional measurements that would make the gate informative rather than merely pass or fail.

1. **Residual against marker count.** Refit with random subsets from 4 markers up to all 34 and plot residual against count. This is the empirical answer to the question section 2 could only reason about, and it either justifies 34 markers or tells you 16 would do. Run it at nine markers in particular, which is what a 300 yard tile carries.
2. **Residual against marker module size.** Print the same target at 0.3, 0.4, 0.5, 0.6 and 0.8 mm modules and measure. This finds the real dot-gain floor on the author's actual printer, which no literature can supply.
3. **Corner refinement comparison.** `CORNER_REFINE_NONE`, `SUBPIX` and `CONTOUR`, same images, residual for each. Confirms that refinement is doing what section 3.4 assumes. **Partly done on synthetic renders, and it moved from a nicety to a priority.** On a 300 DPI render OpenCV's default subpixel window, 0.3 of a module, leaves corners 0.24 px RMS out; one module gives 0.16 px; two modules gives 2.0 px; `NONE` and `CONTOUR` give 0.7 to 0.8 px. Phase 0a therefore ships a one-module window. What survives that is a **0.10 px inward bias present on every marker**, which a homography cannot absorb precisely because it is common to all of them, and which accounts for much of the 0.00091 inch worst single corner in the Phase 0a table. Redo this on paper: the optimum on a clean synthetic raster is not necessarily the optimum on an inkjet print through a flatbed, and the bias may be the renderer, the detector, or both.
4. **Adaptive threshold window on 600 DPI input.** Detection rate against `adaptiveThreshWinSizeMax`, and against downsampling factor, to settle whether scans should be downsampled before detection.
5. ~~**False positives on a shot target.**~~ **Done. Zero at default parameters**, across sixteen files and nine dictionaries, with 665 quad candidates rejected per dictionary. Section 4.1 has the table and the permissive-detector variant that separates the strong dictionaries from the weak ones.
6. ~~**Cross-decode check.**~~ **Done, and it destroyed the escape route**, which is why the family changed. Section 4 records the whole thing.
7. **Print-scale detection.** Print at 96.2 percent and confirm the reported scale, which is DESIGN.md's own example and makes a good regression test.
8. **Corner localisation against the AprilTag detector as well as OpenCV's.** New, and it exists because there are now two detectors that can read the sheet. They use different quad-fitting front ends, so they will not give identical corners. Measure both against the same printed target and record which is better, because that decides which is primary on mobile rather than merely which is available. **Read section 11 before running this one**, because the two detectors do not agree about which corner is first.

Measurements 5 and 6 needed no printer and no scanner and have been done; `tools/fiducial/run_all.sh` reruns them. The rest still need paper.

---

## 11. OpenCV's tag36h11 is rotated 180 degrees, and the printed sheet is not

Found by the Phase 0a implementation and verified against the published AprilRobotics marker images for ids 0 and 1.

**OpenCV's `DICT_APRILTAG_36h11` holds every code turned 180 degrees** from libapriltag's own `apriltag_to_image` output and from the official images. Because the family is closed under rotation for detection purposes, ids still decode correctly and nothing looks wrong. What differs is **corner order**: OpenCV reports as its first corner the one that is printed bottom-right.

**GroupLab prints the official orientation.** That is the right way round and it is not a close call. The printed sheet is the artefact that outlives every piece of software that reads it, so it matches the family as its authors define it, and the library quirk is corrected in the one place that can be changed later without reprinting anything. The imaging backend rotates OpenCV's corner list back, and a test checks the correction against all 587 codes.

**This would have been invisible until it was expensive.** A uniform 180 degree corner-order error does not stop a marker decoding and does not obviously break a homography; it inflates the residual and shifts the fit in a way that looks like poor detection rather than like a bug. It would have surfaced at Phase 0 as a target that registers slightly badly on paper, which is exactly the symptom everyone would have blamed on the printer.

**Consequence for `tools/fiducial/`.** That harness draws its markers with OpenCV, so its rendered markers are rotated relative to what GroupLab prints. The measurements already recorded in section 4.1 are unaffected, because false-positive counts and Hamming distances are rotation-invariant, and the Hamming figures were recomputed from rendered images rather than from `bytesList`. Anything that depends on corner order is affected, which means **measurement 8 above must render its test markers from the GroupLab renderer rather than from OpenCV**, or it will measure the rotation rather than the two detectors.

---

## 12. Summary of changes this implies for DESIGN.md

| Section | Change |
|---|---|
| 9, Fiducials | Marker count rationale changes from surviving distortion to surviving loss and serving the photograph path. The measured warp figures should replace the assumption |
| 9, Fiducials | Add the specific choice: **AprilTag `tag36h11`**, 0.5 mm module, 4.0 mm marker, `grid-boundary-1` placement, with `grid-boundary-half-1` for coarse-pitch sheets and `field-ring-1` for zeroing sheets |
| 9, Fiducials | Add the two shape gates from section 4.1, minimum area and side ratio, as a defence independent of the code check |
| 9, QR codes | Correct the area estimate from 1.5 in² to 3.2 in² at 0.4 mm modules, and state the print-resolution dependence |
| 9, QR codes | Add the QR finder patterns as an explicit degraded-case registration fallback |
| 20, Stack | Record that Emgu.CV's GPL-3.0 has no app-store additional permission. The mobile detector is now **decided**: the BSD-2-Clause AprilTag reference implementation through P/Invoke, which is possible only because the printed family is `tag36h11` |
| 22, Risks | Add the licence conflict as a distinct risk from imaging library availability. They have different mitigations |
| 23, Open items | Close item 6. The mobile detector item that the first draft would have opened is closed before it was written, by the family change |
