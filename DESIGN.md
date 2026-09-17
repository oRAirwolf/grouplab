# GroupLab Design Document

**Working name.** GroupLab is provisional and may change before public release. Repository names and code namespaces are cheap to rename; Android package IDs and iOS bundle IDs are permanent once published, so those are deliberately deferred to Phase 6 of the build plan.

**Status.** Design draft, **revision 3**. No application code has been written.

**Author.** Alan Hayes

**What changed in revision 3.** Seven planning documents were produced against revision 2, and several of them measured things this document had assumed. Where they disagree, they win, and revision 3 folds their conclusions back in. The corrections are marked **[r3]** throughout so a reader of revision 2 can find them. The planning documents are the detail; this document remains the shape of the thing.

| Document | Implements | Settles |
|---|---|---|
| `docs/PATENT-SEARCH.md` | open item 1 | Prior art on machine-readable targets. One live patent, recorded in section 22 |
| `docs/TRADEMARK-SEARCH.md` | open item 2 | No USPTO record for GroupLab in any form |
| `docs/TARGET-SCHEMA.md` | sections 8 and 9 | GLTD 1.0, the JSON format and the binary QR payload |
| `docs/FIDUCIAL-DECISION.md` | section 9 | AprilTag `tag36h11`, and the measurements behind it |
| `docs/DETECTION-PIPELINE.md` | section 12 | Eleven stages, written against the real scans |
| `docs/STATISTICS.md` | section 14 | Estimators, tests, and validation against shotGroups |
| `docs/TARGET-LIBRARY.md` | section 9 | Twenty built-in sheets, all geometry-validated |
| `docs/SCAN-MEASUREMENTS.md` | section 6 | 343 holes measured across 15 files |
| `docs/ONTARGET-DIMENSIONS.md` | section 9 | Dimensional survey of 47 shipped competitor sheets |

---

## 1. Purpose

GroupLab measures how accurately a rifle shoots, and reports the result with the statistical honesty the measurement deserves.

The user prints a target sheet that GroupLab generates, containing a grid of small bullseyes and machine-readable registration markers. They fire one shot per bullseye, then scan the sheet on a flatbed scanner or photograph it with a phone. GroupLab locates every bullet hole, corrects for scanner skew and camera lens distortion, measures each hole relative to its own bullseye, and overlays all shots onto a single composite bullseye. From that composite it computes group statistics.

The one-shot-per-bullseye method exists because overlapping holes cannot be resolved. A tight five-shot group at 100 yards may print as a single ragged opening, and no software can recover five coordinates from it. Spreading the shots across separate aiming points removes that ambiguity while still producing a single high-shot-count group, which is worth far more statistically than several small ones.

## 2. What makes this different

Existing tools report a number and stop. A shooter is handed a group size and left to draw the inference themselves, which they almost always do badly, because the statistics of small samples are unintuitive.

GroupLab reports uncertainty alongside every figure. It estimates the rifle's underlying dispersion rather than the spread of one small sample. It runs significance tests, so a handloader can answer whether load A genuinely beats load B or whether the difference is noise. It performs sample size planning, telling the user how many more shots are needed to resolve a difference of a given size. It pairs each shot with its chronograph velocity so muzzle velocity spread can be regressed against vertical dispersion, separating ammunition variation from rifle variation. And when a user wants to discard a shot as a flyer, it tells them what the mathematics actually expects from a group that size before letting them do it.

The governing principle is that the software should never help a shooter believe something the data does not support.

## 3. Scope

### In scope

**[r7] Every bullet names the phase or phases of section 21 that build it, or is marked deferred and says where the reason is recorded.** A promise with neither is a bug in the plan: `docs/NOTES-FROM-PLANNING.md` entry 90 found ten of them sitting in scope with no phase behind them, and a reader could not tell a deliberate omission from a forgotten promise. `ReadmeTests` now fails on a bullet with neither, so the check is no longer a person reading two sections against each other.

- Generating printable targets with embedded registration markers, in a built-in library (Phase 0a for the format, the sheets and the renderer; Phase 4 for the library screen). **The parametric editor and the full visual designer are deferred**, with the reason in `docs/NOTES-FROM-PLANNING.md` entry 90 section 1: the format carries a fully custom sheet already, and what is missing is a specification of the authoring screen, which is being written with the shooter asking for it rather than guessed at
- Analysing scans and photographs of those targets, fully automatically where possible (Phase 0 for registration, Phase 1 for detection and assignment)
- A secondary mode that analyses any target, including store-bought targets and blank paper, using a user-defined scale and manual hole placement (Phase 3, where marking by hand against a reference length or rectangle is what that mode is). **Assisted placement in that mode is deferred** pending question 18 of `docs/QUESTIONS-FOR-PLANNING.md`: a store-bought target has no definition to render and difference against, so whether the detector as built can assist at all is a design question rather than a scheduling one
- A manual editor capable of correcting, adding, removing, and reassigning every detection (Phase 3)
- Group statistics with confidence intervals, significance testing, and hit probability (Phase 2, all three in the engine; hit probability at a distance other than the one shot needs the solver and is Phase 5)
- Records for rifles, barrels, loads, and sessions (Phase 4, all four, and the load record is what the load-development premise rests on)
- Chronograph import, beginning with Garmin Xero (Phase 5)
- A ported ballistic trajectory solver, used by hit probability, distance normalisation, and velocity-to-vertical analysis (Phase 5, and distance normalisation is named in that phase rather than assumed inside it)
- An unobtrusive link for supporting the project financially (Phase 4, one menu item, on the terms in section 20)
- Optional synchronisation through the user's own cloud storage (Phase 7)

### Out of scope

- Any compatibility with OnTarget PC or OnTarget TDS: no file formats, no target library, no import path
- Any paid tier, licence key, or commercial offering
- Electronic target systems and acoustic scoring
- Any in-application payment processing

### Deliberate non-goal

GroupLab is not a replacement for OnTarget in the sense of a drop-in migration. It is an independent tool built on the same underlying idea. The author uses OnTarget TDS regularly and holds no ill will toward its developer.

## 4. Position relative to existing software

Three pieces of prior art matter and should be credited in the repository.

**OnTarget PC and OnTarget TDS**, by OnTarget Software of Westminster, Colorado. The originator of the printed multi-bull composite group workflow in this space, and the direct inspiration for this project. GroupLab does not copy its designs, formats, or terminology.

**shotGroups**, an R package by Daniel Wollschlaeger, published on CRAN under an open licence. It analyses group shape, precision, and accuracy using descriptive statistics and inference tests including non-parametric and robust methods, implements distributions for radial error in bivariate normal variables, and ships lookup tables for the distribution of range statistics and Rayleigh sigma. This is substantially the statistics layer GroupLab needs, already built and peer-reviewed. GroupLab should port its formulae with attribution and, more importantly, validate its own implementation against shotGroups output. Reimplementing statistical code without a reference oracle is how silent errors enter a measurement tool.

**Taran**, which argues the same position this project takes: extreme spread uses only two data points from a sample, is badly skewed by fliers, and is close to the least efficient precision measure available. Aggregating individual impacts across targets reaches the same confidence for meaningfully less ammunition.

## 5. Glossary

| Term | Meaning |
|---|---|
| Bull | A single aiming point printed on the target sheet |
| Cell | The region of the sheet belonging to one bull |
| Sighter | A bull outside the numbered scoring grid, used for fouling or zero confirmation |
| Composite group | All shots translated so their own bull centre becomes a common origin |
| Fiducial | A printed machine-readable mark at a known page coordinate, used to solve scale and distortion |
| Registration | Solving the transform from image pixels to real page coordinates |
| Mean radius | Average distance of shots from the group centre |
| Rayleigh sigma | Scale parameter of the radial error distribution; the preferred dispersion estimator |
| Extreme spread | Largest centre-to-centre distance between any two shots; the traditional measure |
| CEP | Circular error probable; the radius containing a given fraction of shots |
| Aerodynamic jump | Vertical deflection induced by crosswind acting on a spinning projectile |

## 6. Findings from sample data

Seventeen real scans were analysed during design. The following are measurements, not assumptions. **[r3]** Three of them were wrong, and the corrections below come from `docs/SCAN-MEASUREMENTS.md`, which measured 343 holes across 15 files rather than eyeballing a handful.

**Scan geometry.** Sample scans were 4958 by 6458 pixels tagged at 600 DPI. That resolves to 8.263 by 10.763 inches, which is not US Letter. The scanner captures a region smaller than the page. Any software assuming the image spans 8.5 by 11 inches would be roughly three percent wrong immediately.

**DPI metadata honesty.** On the same scans, the detected bull grid measured 1.4992 inches against a 1.5 inch nominal spacing, an error of 0.05 percent. So the DPI tag was accurate. The problem is not that metadata lies; it is that there is no way to know when it does without something of known size in the frame.

**Bull detection is easy.** A naive connected-component approach located 24 of 25 bulls on a sample target. The single miss was a bull whose printed ring a shot had partly destroyed. Registration is not the technical risk.

**Hole detection is hard.** Two standard approaches, a filled-contour method and a gradient-energy method, returned 6 of 25 and 7 of 20 respectively. **[r3]** A properly tuned Hough circle detector does far better, 26 of 27, but only when tuned per image, which is the finding that forced the Phase 1 gate to be rewritten in section 21. In both cases the printed artwork dominated every generic signal: rings, numerals, and centre dots all present stronger features than bullet holes do. This is the central technical risk of the project and the direct justification for the render-and-difference strategy in section 12.

**Hole appearance on a flatbed. [r3] The bright core is not real, and this document was wrong about it.** Measured across 343 holes, the core of a hole averages **53 grey levels darker than the surrounding paper**, and 24 percent of cores never reach local paper level at all. The scanner lid is not reliably visible through the perforation, because the torn fibre lifts and shadows it. What is reliable is the **dark ragged rim**, and that is the signal the detector uses. The claim that a bright core distinguishes a hole from ink should be struck wherever it appears.

The consequence for backer material is unchanged: it has no effect on scanned appearance and matters only for photographs. Scans and photographs still require genuinely different detectors.

**[r3] Radial symmetry is an actively harmful test.** Rim radius coefficient of variation is 0.48 across the corpus. A bullet hole is a lobed star, not a circle, and a symmetry gate rejects real holes faster than it rejects dirt.

**[r3] Scale is the primary discriminator, not intensity and not colour.** Every printed feature in the corpus is at most 0.0567 inches wide; every hole is 0.15 to 0.54 inches across. That is a factor of three to nine, it holds in every printing style including greyscale, and it survives resolution changes because it is expressed in physical units. Every other signal is a refinement on top of it.

**[r3] Paper is a developable surface, and the crumpled target is barely warped.** On the worst sheet in the set, crumpled, torn along two edges, taped and non-rectangular, a plain four-point homography leaves a residual of rms 0.0027 inches. Four pristine sheets measured 0.0026 to 0.0032. This changes the justification for the marker count in section 9.

**[r3] Sample attribution corrected.** The `n568*` targets are .264, 6.5 mm, with the primer as the only variable, not the .338 class this document and `SAMPLE-NOTES.md` originally recorded.

**Real-world annotation.** Several sample targets carried hand-drawn marker arrows linking a bull to a shot that had landed outside its own cell, sometimes inside a neighbouring cell. On those targets the nearest bull is frequently the wrong bull. No geometric rule recovers the correct assignment. Others carried X marks drawn over holes to indicate exclusion, meaning ink over perforations is a real detection case. One target was crumpled, torn along two edges, taped, non-rectangular, and scanned at 300 DPI.

## 7. Architecture

Four layers, with a hard rule that nothing below the UI layer knows the UI exists.

**Core.** A platform-neutral library containing the target definition model, registration, detection, assignment, statistics, and the data schema. No UI types. No platform APIs. This is the expensive part of the project and it is written once.

**Imaging backend.** An interface the Core depends on for decode, resize, filtering, and geometric solving. Concrete implementations differ between desktop and mobile because the available bindings differ. Putting this behind an interface from the first commit is what prevents the mobile port becoming a rewrite, and it also lets the fiducial detector avoid depending on any one library being present.

**Persistence and sync.** Local database, file layout, export, and the optional cloud provider adapters.

**Shells.** One per platform, thin, containing only presentation and interaction.

## 8. Target definition format

The format is a documented, versioned, human-readable schema. The community can author targets in it, and the visual designer emits it. Every geometric value is expressed in real units, never pixels.

**[r3] Specified in full as GLTD 1.0 in `docs/TARGET-SCHEMA.md`**, as a JSON document plus a lossless compact binary encoding for the QR payload. Contents:

- Schema version and definition identifier
- Page size, with letter, legal, tabloid, A4, A3, **plotter roll widths of 24, 36 and 42 inches** and arbitrary custom sizes all expressed the same way
- Ink palette, including a **paper knockout** role
- Ring sets, expressed as **stacks of filled concentric discs** rather than stroked circles
- Bull list, each with centre coordinate, ring set, label, and a flag for scoring or sighter
- Cell boundaries, where drawn
- Fiducial layout
- Code layout
- **Load-data block**, where the sheet carries one
- **Tiling block**, where the sheet is one tile of a larger assembly
- **Measurement grid**, where the sheet is a zeroing target
- Print metadata block

**[r3] Every length is an integer number of tenths of a millimetre.** Not a style preference: it makes the JSON and the binary share one quantum, so conversion is a cast rather than a rounding decision, and it guarantees the renderer and the analyser read the same number. The quantisation limits where a bull may be placed and contributes exactly zero error to any measurement, because the artwork is printed from the same integer the analyser later uses as that cell's origin.

**[r3] Rings are disc stacks, and this is a correctness matter rather than a drawing convention.** A stroked circle of diameter D and width W has three legitimate ink extents depending on whether the stroke sits inside, centred or outside, and for a 25.4 mm ring those differ by 0.0157 inches. The Phase 0 registration gate is 0.001 inches. A stack of filled discs has exactly one interpretation, and every ink boundary is the difference of two integers in the file.

Page size being a declared property rather than an assumption is what makes A4 support fall out for free. That matters more than it sounds, because A4 is the default everywhere outside North America, and a tool that only handles letter is quietly North America only.

## 9. Target generation and printing

### Fiducials

Registration markers are the foundation of every measurement GroupLab makes, and the design is driven by the fact that targets get shot, rained on, and handled with dirty hands.

**[r3] The family is AprilTag `tag36h11`**, printed at a 0.5 mm module giving a 4.0 mm marker with a 1.0 mm quiet zone, so a 6.0 mm footprint. Eight by eight modules including the mandatory border, 587 identifiers, minimum Hamming distance 11, correction capped at 5 bits. `docs/FIDUCIAL-DECISION.md` has the comparison; the reason it is AprilTag rather than ArUco is that `tag36h11` is the only strong family that **both** OpenCV and the BSD-2-Clause AprilTag reference implementation can read, which removes the licence problem recorded in section 20 from the critical path. It is otherwise identical to the ArUco dictionary it replaced: same data bits, same footprint, same Hamming distance, same measured false-positive behaviour, and more than twice the identifiers.

Thirty to forty small markers are distributed across the page rather than four in the corners. **[r3] The original justification for that count was wrong and the count is still right.** This document argued that a radial distortion model wants roughly fifteen points and that forty means surviving the loss of half the page. The first half does not survive contact with the data: four corners meet the Phase 0 gate on the worst sheet in the collection, because paper bends without stretching and a homography absorbs it. What does justify the count is marker loss to tearing, staples and shots, and the photograph path, where a genuine lens model does want many well-spread points. The layout validator places **34 markers** on the reference Letter sheet and 32 to 56 across the library.

Each marker encodes its own index so that a partial set remains unambiguous. Fitting uses RANSAC, so smeared, torn, or misread markers are rejected as outliers automatically rather than corrupting the result.

Markers are placed on cell boundaries rather than near bull centres, because that is where shots cluster. They are pure black with generously sized modules, no anti-aliasing and no halftone screening, which survives inkjet bleed on cheap paper and remains readable when damp. **[r3] Three placement rules exist**, versioned in their names so that improving one cannot reinterpret a target already printed and shot: `grid-boundary-1` for ordinary pitches, `grid-boundary-half-1` for coarse-pitch long-range sheets where the plain lattice degenerates to two surviving markers, and `field-ring-1` for zeroing sheets, which have one bull and therefore no useful bull lattice.

**[r3] Two shape gates run before the code check.** Measured against the real scan corpus, every false marker a deliberately permissive detector found was a sliver of a printed ring arc: worst side ratio 4.57 and largest area 1366 square pixels against a real marker's 8928 at 600 DPI. Rejecting candidates below half the expected area, or above a side ratio of 2.0, kills all of them without reference to the dictionary. A defence that fails differently from the Hamming check is worth more than a stronger version of the same check.

**[r3] The false-positive rate against real printed target artwork is zero.** Sixteen scans, nine dictionaries, 665 quadrilateral candidates found and rejected per dictionary, no accepted markers at default detector parameters. This needed no printer and is the measurement that most directly de-risks the fiducial approach.

**[r3] Four QR codes sit in the four corners at version 10, error correction level H.** That is 119 bytes of capacity in a 26.0 mm footprint at a 0.4 mm module. All carry an identical payload, so any single survivor is sufficient. Four rather than two because corners are precisely where targets are stapled, taped, and torn. Long-range tiles carry two rather than four, because the sheet is small and an assembly of four to six sheets already holds eight to twelve copies of the same payload. The QR codes perform no registration work; that is entirely the fiducials' job, though their finder patterns are available as a degraded-case fallback when too few markers survive. The definition identifier is additionally printed as human-readable text.

### Self-describing targets

The QR payload carries the complete target definition, not a pointer to it. This is essential: a user may print a target designed in the parametric or visual designer, and another user with no access to that design must still be able to analyse the result. There is no central database, and none is required.

**[r3] Payload sizing is now measured rather than estimated**, by a reference encoder delivered as `tools/gltd/encode.py`. A parametric target is **55 to 70 bytes of body**, 70 to 85 including the frame header, against this document's estimate of 100 to 200. Crucially the size does not scale with bull count, because parametric mode stores a grid rather than a bull list: the 25-bull reference sheet and the 36-bull rimfire sheet both encode to 55 bytes. What costs bytes is the number of *kinds* of block a sheet carries, so the largest payloads in the library are the two sheets carrying a load block plus one more optional block. A fully custom target with forty arbitrarily placed bulls lands at roughly 215 bytes, inside the under-500 estimate. A version 40 QR code holds 1,273 bytes at error correction level H, so the worst case uses 6.7 percent of one.

**[r3] The area estimate in this document was wrong.** Section 9 said four codes cost "about 1.5 square inches out of 93", which implies a 0.32 mm module, or 3.7 printer dots at 300 DPI. That does not print reliably on a consumer inkjet. The honest figure at a 0.4 mm module and version 10 is **4.2 square inches for the codes**, and 6.1 to 7.3 square inches for codes and markers together, which is six to eight percent of a Letter page and under one percent on roll media.

A content hash accompanies the definition so the application can verify that its reconstruction matches what was printed. It is the SHA-256 of the canonical binary body truncated to 80 bits and rendered in Crockford base-32, which costs nothing in the payload because the reader computes it from what it decoded.

**[r3] Load data travels in a second, separate code.** A sheet may carry a load-data block, chosen at print time as a blank to write in at the range or pre-filled with typed values. The values are **not part of the definition** and do not change its identifier, because two sheets with the same layout and different loads are the same target. They travel instead in an instance code with its own magic bytes, version 11 at level Q, which cannot be confused with a definition frame. The blank and filled print modes produce identical geometry, so a user who prints blank, shoots, and types the load in afterwards gets exactly the same analysis as one who typed it first.

If a pathological design ever exceeds single-code capacity, the payload is erasure-coded across the four corners so that any two reconstruct it. Failing even that, the application degrades to visually detected bulls with reduced accuracy and says so.

An optional community library hosted by the project lets users share designs by hash. It is a convenience for discovery, never a dependency for analysis.

### Print scale verification

Fit-to-page shrinks an A4 target printed on letter by about four percent, and the user will not notice. Because the fiducials are at known coordinates, GroupLab detects this automatically. The correct behaviour is not to silently correct it but to say so: this target printed at 96.2 percent of intended size, measurements corrected accordingly. That converts a silent accuracy bug into visible reassurance.

### The built-in library [r3]

Twenty sheets ship, all geometry-validated, specified in `docs/TARGET-LIBRARY.md`: five centrefire load-development layouts, three rimfire, three large-format, two long-range tiles, three plotter-roll sheets at 24, 36 and 42 inch widths, and four zeroing sheets.

**Cells are angular, not linear**, which is the one sizing decision everything else follows from. A shot lands near its own bull, and how near, in inches, scales with distance, so the correct unit for cell pitch is MOA and the correct question for each layout is what distance it is for. The design criterion is a half-pitch of about 0.75 MOA at the sheet's intended distance.

**That criterion has a geometric ceiling, and it is worth stating plainly.** Twenty-five bulls at the full criterion is achievable to about 137 yards on Tabloid and 103 on Letter. The group grows linearly with distance and the sheet does not. Past 200 yards the answer is **tiled multi-page targets or plotter roll media**, not a denser sheet.

**Tiles need no alignment.** Every shot is measured relative to its own bull, and every bull sits entirely on one tile registered by that tile's own fiducials, so no step in the chain uses the position of one tile relative to another. Tile alignment contributes exactly zero error to any group statistic; tape and an eyeballed edge are sufficient. Tiles are scanned separately and pooled. The constraint that does matter is that no bull may cross a seam, which the validator enforces.

**Zeroing sheets are the documented exception to the 25-bull rule**, because a zeroing sheet answers a different question: where the group centre sits relative to the aiming point, which needs one aiming mark and a printed angular grid, not twenty-five bulls. Four ship, covering MOA and mil at 100 yards and 100 metres, since a turret is calibrated in one and a range is marked in the other and the four combinations do not convert by scaling a printed grid.

## 10. Image acquisition

### Formats

Phones produce a wider variety of files than most software expects, and a target photograph that will not open is a total failure from the user's point of view. The application decodes all of the following.

| Format | Where it comes from |
|---|---|
| JPEG | Universal. Includes Ultra HDR and ISO 21496-1 gain-map JPEGs from Android, which decode as ordinary JPEGs |
| PNG | iPhone screenshots and Markup exports |
| HEIC / HEIF | iPhone default since iOS 11, including HEIF Max; increasingly present on mid-range and premium Android devices |
| AVIF | Android and sharing pipelines. No camera captures it directly, but processed images arrive in it |
| WebP | Android sharing and processing |
| DNG | Apple ProRAW, and raw capture on Android |
| JPEG XL | ProRAW on newer iPhone Pro models, which renders as DNG with embedded JPEG XL; also standalone |
| TIFF, BMP | Scanner output |
| PDF | Common scanner output, including multi-page |

Three handling rules that prevent silent failures:

- **Honour EXIF orientation.** The classic bug that silently rotates phone photographs.
- **Ignore HDR gain maps.** Decode the base SDR image. Applying the gain map alters tone response, and the detector wants consistent input.
- **Take the primary item from multi-image HEIF containers** as the specification defines it, not simply the first item in the file.

The application also recognises and silently ignores .AAE sidecar files from iOS, which are edit instructions rather than images and would otherwise produce a confusing error.

HEIC and JPEG XL decoding both require explicit dependency decisions on Windows, since neither ships enabled by default and HEIC carries patent considerations. This is an open item.

**Scanner acquisition** through WIA 2.0 for USB devices and eSCL over HTTP for network devices. TWAIN is deliberately excluded unless a specific device forces it. When no device is present the scan control is simply absent.

**Camera capture** on mobile, with live guidance for framing and a warning when the viewing angle is extreme enough to degrade the fit.

## 11. Registration

A single pipeline handles both scans and photographs, because the mathematics is the same and only the severity differs.

1. Detect fiducials and read their indices.
2. Reject outliers with RANSAC against the known page coordinates.
3. Solve the transform. For scans this is an affine or homographic fit absorbing skew, rotation, placement offset, and any scanner scaling error. For photographs it extends to a full lens model including radial distortion coefficients.
4. Report the residual as an image quality figure the user can see.

### Metadata comparison report

The application shows the user what the fiducials corrected relative to what the file's own metadata claimed. For a scan: the DPI the file asserts, the horizontal and vertical resolution actually measured, and the correction applied. For a photograph: the fitted distortion coefficients, the maximum displacement at the frame edge in pixels, and that displacement converted to inches at the target plane.

That final figure is the important one, because it states how wrong the measurement would have been without the correction. It is both a useful diagnostic and the clearest possible demonstration of why the fiducial approach beats trusting metadata.

The important consequence for photographs is that **no camera metadata is required**. Thirty known points overdetermine the model, so the distortion is fitted from the image itself. Camera intrinsics are a fallback, not the primary method.

This directly solves the failure mode seen in phone-based tools where a scale set at one point in the frame is wrong elsewhere, causing composite groups to be systematically distorted. Scale ceases to be a single number and becomes a continuous corrected field across the image.

For the secondary any-target mode there is nothing of known geometry to fit, so three options apply in descending order of accuracy: a printable calibration strip taped beside the target, camera intrinsics where the device exposes them, and user guidance to stand farther back and zoom in, which reduces distortion substantially.

## 12. Hole detection

### Render-and-difference

Because GroupLab generates the target, it knows the artwork exactly. After registration it renders the known artwork into image space and differences it against the observed image. Everything that belongs on the page subtracts out: rings, numerals, centre dots, cell boundaries, fiducials. What remains is holes, dirt, ink, and paper texture.

This is the architectural advantage OnTarget cannot have, because it analyses targets it did not create. It is also the direct answer to the measured finding that printed artwork dominates every generic detection signal.

### Candidate classification

**[r3] Two of the four signatures this document originally listed do not survive measurement.** The revised list, from `docs/DETECTION-PIPELINE.md`:

| Signature | Status |
|---|---|
| **Size within a window derived from caliber and the solved scale** | **Kept, and promoted to primary.** It is the discriminator that works in every printing style at every resolution |
| ~~Bright core on a scan~~ | **Removed.** Measured 53 grey levels darker than paper on average, with 24 percent of cores never reaching paper level |
| **Torn fibre halo at the boundary** | **Kept, and promoted.** The ragged rim is the reliable signal the bright core was assumed to be |
| ~~Radial symmetry within tolerance~~ | **Removed.** Rim radius coefficient of variation is 0.48. A hole is a lobed star |
| **Chroma residual against the rendered artwork** | **Added.** Fisher ratio 2.05 to 4.89 on colour-printed styles, and near zero on achromatic ones, so which residual is trusted is decided per image from a measurement rather than from a setting |

**[r3] No absolute intensity threshold may appear anywhere in this pipeline.** The same naive detector needed three different threshold values across 93, 300 and 600 DPI scans of the same physical targets, because rim contrast falls as each rim is averaged over fewer, larger pixels. Thresholds are expressed as a fraction of the local paper-to-ink dynamic range, and structuring-element radii are expressed in inches and converted through the solved scale. The resolved absolute value is recorded in the trace so it can be checked.

**[r3] Declared exclusion zones come before any of this.** The definition names regions that are printed matter by construction: the load-data block, the code squares with their quiet zones, the marker footprints and the identifier text. Candidates inside them are dropped and recorded as such, rather than being classified. Handwriting in a load block is exactly the kind of small dark irregular blob a detector reports as a shot, and a declared rectangle is free and perfectly reliable where classification is neither.

Every detection carries a confidence score. Marginal candidates enter a review queue rather than being silently accepted or silently dropped. Sub-pixel centroids are computed for accepted holes.

### Caliber estimation

Caliber can be estimated rather than required. The application takes the median hole diameter across all detections, which is robust to torn outliers, and snaps to the nearest standard caliber.

Honest limits apply, and they are now quantified. **[r3]** Measured across 260 holes of known caliber, the ratio of hull diameter to bullet diameter is 0.919 for 6.5 mm, 0.920 for .308 and 0.981 for .338, with a mean deficit of 0.0202 inches and a standard deviation of 0.0509. That standard deviation is two and a half times the twenty-thousandths that separates .264 from .284, so **distinguishing those two is not reliable and the application must not pretend otherwise**; distinguishing .224 from .338 is trivial. When two candidates are close the application presents both rather than guessing confidently.

**[r3] Ovality was attempted as a yaw signature and the measurement failed.** Two ellipse-fitting passes produced aspect-ratio standard deviations of 0.32 to 1.64, larger than the effect being sought. The question is open and is a Phase 1 task; it is recorded here as a failure rather than omitted, because an unrecorded failed measurement gets attempted again.

The consequence of a wrong guess is small. Caliber sets the size validation window and the drawn hole circles, not the centroid, so the measurement itself is barely affected. When a rifle is selected from the user's records the caliber is already known, making estimation relevant mainly for one-off analysis.

### What the detector will not attempt

Resolving overlapping holes into separate shots. On generated targets the one-shot-per-bull rule makes this unnecessary. In the secondary mode the user places them manually.

**[r3] Reading the hand-drawn arrows.** Several sample targets carry marker arrows linking a bull to a shot in another cell. They are the shooter's own record of the correct assignment and the pipeline should not try to read them. They are, however, the only independent ground truth for cross-cell assignment that exists in the corpus, and transcribing them once by hand into a fixture file would convert an asserted accuracy claim into a measured one.

### Pooling across sheets [r3]

A session can span several sheets, and the pipeline gains a final stage for it. On a single sheet it is a pass-through. On a tiled assembly it concatenates the per-bull results using the tile index carried in the frame header, checks that every tile is present exactly once and that all of them decode the same definition identifier, and reports per-tile print scale. A missing tile degrades the composite and says so rather than pooling silently, because thirty shots that should have been thirty-six is a biased sample if the missing sheet was the one fired last.

## 13. Assignment and manual editing

The editor is built before the detector, not after it. A good editor with a mediocre detector is a usable product. A bad editor with a good detector still frustrates users on every target the detector gets wrong, and no detector reaches one hundred percent.

Requirements:

- Every detection is an editable object: move, delete, add, reassign, flag
- Reassignment by clicking a hole then clicking a bull, because the sample data proves nearest-bull is frequently wrong
- Keyboard-driven operation, since verifying twenty-five shots should take seconds
- Rough clicks snap to the local centroid, so manual placement is not limited by mouse precision
- A fully manual mode that places every hole by hand
- Undo and redo throughout
- Provenance recorded per shot: automatic, automatic then corrected, or manual

Provenance matters because it keeps the statistics honest about where the numbers came from.

**[r3] Nearest-bull is not the assignment rule.** The rule is the globally optimal one-to-one matching between detections and bulls, which encodes the one-shot-per-bull constraint the target was designed around. Measured, nearest-bull is correct on every cross-cell shot in the corpus except one, where a hole 0.627 inches from bull 4 belongs to bull 9 at 1.043 inches because bull 4 already has a closer shot. One-to-one matching gets it right; nearest-bull does not.

**[r3] The method is only valid when the counts match, and that is a measured failure rather than a caveat.** On a file with 27 detections for 25 bulls, forcing a matching manufactures a false cross-cell result. So: equal counts, use matching and report high confidence; fewer detections, match against the subset; **more detections than bulls, do not force a matching** but fall back to nearest-bull within a distance gate, flag every ambiguous case and tell the user the counts disagree. Forcing produces confident wrong answers, which section 2 says the software must never do.

**[r3] A margin under about 0.15 inches between nearest and second-nearest bull is a review-queue item by construction**, because a 0.05 inch registration error would flip it.

**Amended 2026-09-16, `docs/NOTES-FROM-PLANNING.md` entry 70 section 3: matching reruns as the person edits, and a person's decision is a constraint rather than an input.** Never rerunning leaves an answer computed for a different set of shots the moment one is added, moved or deleted. Always rerunning is worse: in one-to-one matching, pushing a shot onto a bull pushes whatever held it somewhere else, so resolving one contested case would silently reverse a decision the person had just made. So:

- A bull is pinned only when a person chose it, by clicking a hole and then a bull. **Amended by entry 74 section 1:** placing a hole says there is a hole there, not which bull it belongs to, so a hole added by hand whose bull the software picked is matched like a detection. Whether a person chose the bull is recorded on the shot on its own, separately from how the mark got there.
- Every shot whose bull nobody chose re-solves on every edit, against the bulls no chosen shot holds. Positions are never re-solved.
- A shot the re-solve moves off its detected bull is a review item for as long as it stays moved.
- The counts rule above holds live: more untouched shots than free bulls, and matching stops being forced, every shot is flagged, and the screen says the method changed.
- Undo restores the pins as well as the positions.
- **Sighter and scoring bulls are two pools, entry 73 section 1.** A shot fired at a sighter can never belong to a scoring bull, nor the reverse, so each shot joins the pool of its nearest bull and the counts rule above applies in each pool on its own. Matching both as one pool gave two sighter holes to scoring bulls a row away, and the group statistics counted them.

**A known limit, entry 74 section 2: the assignment detail does not survive a save.** A marking file keeps each shot's bull and whether a person chose it, but not the sheet's page mapping, so a reopened marking has no margins, alternatives or reasons until detection runs again, and its review queue is empty for that reason rather than because nothing needs review. The screen must say which of the two it is. Carrying the mapping in the file is a later, deliberate decision.

Assignment classifies against the definition's declared bull positions, and offsets are measured from the located ones. The two are kept apart on purpose: registration and printing error are a few thousandths of an inch against a 0.15 inch margin, so the choice cannot flip an assignment that was not already flagged, and the shooter aimed at the bull as printed.

## 14. Statistics

### Baseline

Extreme spread, mean radius, group width and height, offset from point of aim, standard deviation in x and y, standard deviation of radius, and CEP at 50, 90, and 95 percent.

### The part that matters

- Confidence intervals on every figure, by bootstrap where no closed form exists
- Rayleigh sigma with confidence interval, as the preferred dispersion estimator
- Bivariate normal fit with a test for circularity, which detects vertical stringing
- Two-sample significance testing between loads
- Sample size planning: how many more shots are needed to resolve a difference of a stated size
- Hit probability for a target of given size at a given distance, with interval, computed through the ballistic solver rather than by scaling the group linearly
- Pooled and virtual groups across multiple targets and sessions
- Velocity regressed against vertical dispersion
- Predicted versus measured vertical dispersion: the solver converts measured muzzle velocity standard deviation into expected vertical spread at a chosen distance, which is then compared against the vertical actually measured. When measured vertical greatly exceeds prediction, the limit is the rifle or the shooter and tightening extreme spread will not help. When they agree, the ammunition is the limit. This question is argued constantly and almost never answered numerically.
- Distance normalisation, so groups shot at different distances can be compared honestly

**[r3] Specified in full in `docs/STATISTICS.md`**, with every closed-form estimator reimplemented and checked against the shotGroups R package at a difference of exactly zero. Two results from that work belong in this document because they change what the software should say to a user.

**Sample sizes are larger than anyone expects.** To resolve a ten percent difference between two loads at conventional power requires **434 shots per load**. Twenty-five percent requires 81. Fifty percent requires 26. A five-shot group comparison resolves nothing, and the application should say so rather than printing a p-value that invites the wrong conclusion.

**The worst shot in a group is farther out than shooters believe**, which is the number the flyer dialog below needs. At n=25 the expected maximum radius is 2.176 times the mean radius, and the probability that the worst shot exceeds twice the mean radius is 0.669. Two thirds of honest 25-shot groups contain a shot that looks like a flyer and is not.

### Units

Everything is stored canonically as linear distance at the target plane. Output supports inches, centimetres, MOA, and mil as independently toggleable columns displayed simultaneously. True MOA is the default, at 1.047 inches per 100 yards, with IPHY available. Angular columns require a known distance and are not offered when distance is unset.

### Flyer handling

Exclusion is permitted, because the legitimate case exists. It is gated.

When a user marks a shot for exclusion, GroupLab first states what the mathematics expects. In a group of a given size drawn from a given dispersion, the worst shot has a known expected distance from centre, and it is farther out than most shooters believe. The dialog says so plainly. Exclusion then requires a reason selected from a short list, which is recorded. Every report prints both the full and reduced figures side by side, so an exclusion can never be hidden.

### What the intervals do not include

Every interval describes shot-to-shot dispersion and takes the measured coordinates as exact. The measurement's own instability is not in them yet: a sheet that is not flat registers differently when nothing but the order of its markers changes, and a bull seen by few edge points moves when one point is left out. Both are measured, and recorded as a known source of uncertainty, in `docs/STATISTICS.md` section 2 (`docs/NOTES-FROM-PLANNING.md` entry 52 section 4).

## 15. Data model

Entities: Rifle, Barrel with round count, Load, Session, Target, Shot, Group, Chronograph String.

Critical constraint: **the shot sequence and the chronograph sequence are separate ordered lists that get reconciled, never assumed to align.** Chronographs drop shots, record a neighbour's shot from the adjacent bench, and log the fouling round fired into the berm. Hardcoding shot N to bull N produces numbers that are quietly wrong. The model stores both sequences with an explicit mapping and provides a reconciliation interface for when counts disagree.

Storage is SQLite with a documented schema and full JSON export.

## 16. Ballistic solver

The solver is ported from the existing ballistics.js engine used across the Pissin Hot Precision tools: a point-mass RK4 integrator with G1 and G7 drag models, BRL drag tables, ICAO atmosphere, spin drift, Coriolis, and aerodynamic jump.

Porting rather than rewriting gives a free validation oracle, since the C# implementation can be run against the JavaScript on identical inputs and any divergence is a bug in one of them.

It is used for hit probability at distance, distance normalisation between groups, the predicted-versus-measured vertical analysis, aerodynamic jump contribution from crosswind variability, and transonic range warnings.

The solver models physics only. Barrel harmonics, optimal barrel time, velocity nodes, and accuracy nodes are not represented anywhere in GroupLab, because they are not real.

## 17. Chronograph integration

Garmin Xero first. All chronographs sit behind one import interface, with LabRadar, MagnetoSpeed, Athlon, and Caldwell as follow-ons.

Other imports worth building:

- GRT load files, which already carry powder, charge, bullet, and predicted velocity, and which would populate a load record without retyping
- Kestrel or similar for environmental conditions
- Generic CSV, since most users migrating to GroupLab will arrive from a spreadsheet

## 18. Storage and synchronisation

### The size problem

A fully analysed target expressed as pure geometry is approximately 5 KB of JSON, or 1.5 KB compressed. A 600 DPI colour scan of the same target is approximately 25 MB. That is a factor of roughly five thousand.

| Population | Geometry per year | Images per year |
|---|---|---|
| 1 active user, 200 targets | 1 MB | 5 GB |
| 100 active users | 100 MB | 500 GB |
| 10,000 active users | 10 GB | 50 TB |

Geometry is free at any plausible scale. Images are ruinous almost immediately.

### Three tiers

1. **Geometry**, always synchronised. The archived analysis view needs no image at all, because GroupLab re-renders the target vectorially from its own definition and overlays the shots.
2. **Proof image**, roughly 400 KB at 150 DPI, synchronised by default, sufficient for eyeballing and audit.
3. **Full resolution original**, local by default, optionally placed in the user's own cloud storage.

### Provider model

Synchronisation uses the user's own Google Drive, OneDrive, or iCloud rather than project-operated storage. Free tiers of 15 GB, 5 GB, and 5 GB respectively hold hundreds of full scans and effectively unlimited geometry. This costs the project nothing, removes any custody of other people's data, has no scaling cliff, and survives indefinitely without anyone paying a hosting bill.

Authentication is Google, Microsoft, or Apple OIDC using PKCE with a loopback redirect, so no client secret is embedded in a downloadable binary. Simple accounts are a fallback.

Identity and storage are separate concerns. Sign in with Apple is standard OIDC and works on Windows and Android as well as Apple devices, so it is available everywhere. iCloud Drive, by contrast, has no supported third-party API on Windows or Android, so Apple can serve as an identity provider on every platform but as a storage backend only on Apple hardware. Sign in with Apple also requires the Apple Developer Program, so it arrives alongside the iOS work rather than before it.

**The application is fully functional with no account, forever.** Synchronisation is an opt-in convenience and never a gate.

A small project-operated service remains useful for distributing the target definition library and for share links in the pattern already used by the BVA tool. Both are measured in kilobytes.

## 19. User interface

**Identity.** GroupLab has its own visual identity, related to Pissin Hot Precision but not dressed as it. A free community tool wearing a commercial business's skin reads as marketing, and outside contributors engage more readily with a project that has its own face. The app carries a credit line, not a costume.

**Typography.** A neutral UI sans for chrome and labels, and a monospace with tabular figures for every numeric readout so digits align in columns and values do not jitter as they update. The site's display faces are built for headlines and are poor for dense numeric data.

**Density.** Progressive, with a specific split. The primary panel shows the composite group, the headline figures, and the confidence interval on each, because those change decisions and burying them would defeat the project's premise. Reference material, the full CEP table, the bivariate fit, and the comparison machinery live one click away in a panel that remembers it was opened.

**Themes.** Four, matching the site: dark, light, high contrast, and follow system. **[r7]** Phase 4.

**[r3] The analysis shows its work.** Every pipeline stage emits a structured record carrying its resolved parameters, the decisions it made with their alternatives, what it rejected and why, and its artefacts. That gives three things from one contract: a stage timeline the user can scrub, with clicking a rejection highlighting it on the image; a live run where each stage's artefact appears as it lands, so the markers light up, the residual map settles, the artwork vanishes and the holes emerge; and a console form that gives the Phase 0 and Phase 1 spikes their output for free before any UI exists.

**[r7] The contract is built and the screen is Phase 4.** Every stage already emits its record, and the console form of it gave the Phase 0 and Phase 1 spikes their output as intended. The scrubbable timeline, the artefacts appearing as they land and the rejection clicked to highlight it on the image are the Phase 4 half, and they are now listed there rather than left implied by the contract underneath them, which is how a headline feature becomes an internal diagnostic by default (`docs/NOTES-FROM-PLANNING.md` entry 90 section 2).

The sequence is a real depiction of what happened and nothing false needs adding to it. Two constraints, written down here rather than discovered in review: **the trace must never be the only place an error appears**, so a failed stage produces a normal prominent error with the trace as the detail behind it, and **the theatre must not slow the pipeline down**, so artefact generation defaults on for a single interactive analysis and off for batch.

## 20. Platform, stack, and distribution

**Stack.** .NET 10 with Avalonia. One language for the Core and all three shells. Math.NET Numerics for statistics. Windows ships as a single self-contained executable with no runtime for the user to install, which matters because the audience is shooters rather than developers.

**Windows floor.** Windows 10 21H2. Avalonia renders through Skia rather than Windows 11 compositor APIs, so Windows 10 costs nothing in appearance or performance. This would not be true of WinUI 3.

**Sequencing.** Windows first. Android second. iOS third, built on GitHub Actions macOS runners, which are free for public repositories, so no Apple hardware is required. The Apple Developer Program fee of 99 dollars per year applies only when shipping to the App Store or TestFlight.

**Licence.** GPL-3.0, with an additional permission under section 7 permitting distribution through app stores. Plain GPL-3.0 conflicts with Apple's terms and GPL applications have been removed from the App Store before. Adding this now costs nothing; adding it later would require the agreement of every contributor.

**[r3] That licence choice nearly collided with the imaging stack, and the fiducial family is what saved it.** Emgu.CV is the only .NET OpenCV binding covering Windows, Android and iOS, and its open-source licence is **plain GPL-3.0 with no app-store additional permission**. A downstream distributor may remove additional permissions but cannot add them to somebody else's code, so shipping GroupLab plus Emgu.CV through the App Store would reproduce exactly the conflict this licence choice exists to avoid. Windows is unaffected, because OpenCvSharp is Apache-2.0.

The resolution is in section 9: because the printed marker is AprilTag `tag36h11` rather than an ArUco dictionary, **the mobile detector can be the AprilTag reference implementation under BSD-2-Clause**, through a P/Invoke layer. Emgu.CV becomes one option among several rather than the only one. The conflict is recorded here anyway, because it still applies to any future use of OpenCV on mobile for something other than marker detection, and that is a live possibility for Phase 6.

**Distribution.** Unsigned direct downloads plus a Microsoft Store listing. The Store's one-time individual developer fee of 19 dollars gets Microsoft-signed binaries, which bypasses SmartScreen entirely and provides automatic updates. Paid code signing remains available as a build-step switch if ever wanted.

**Supporting the project.** A single unobtrusive menu item opens a browser to a support page listing PayPal, Venmo, Cash App, and similar. No payment processing occurs inside the application. A FUNDING.yml places a Sponsor button on the repository at no cost. There is no nagging, no countdown, and no reminder, because the moment it behaves like nagware the goodwill that motivates tipping disappears. Apple and Google both regulate collecting money and have historically restricted linking out, so store policy must be verified before the mobile releases; Windows and direct download are unaffected.

**Repository.** Public from the first commit. Markdown for files inside the repository, since GitHub renders them. Documents intended for reading are additionally produced as docx or PDF.

## 21. Build plan

Each phase has a gate. A phase is not complete until its gate passes.

**[r3] Phase 0 gained a predecessor, because it turned out not to be buildable first.** The registration spike measures fiducial detection against a printed target, and no target with fiducials exists: every scan in the corpus runs the bull-centre fallback path. Something has to render a GLTD definition onto paper before anything can register against it.

**Phase 0a: Format and renderer.** No UI. GLTD-J parser and validator, GLTD-B encoder and decoder, and a PDF renderer, per `docs/TARGET-SCHEMA.md`. Gate: **conformance test 43**. Render a definition, analyse the rendered image as if it were a scan, and confirm every bull centre is recovered at its declared coordinate to within the Phase 0 residual gate. That closes the loop between the format, the renderer and the analyser with no printer and no scanner involved, and it is the test that catches a disagreement between the two halves of rule R5 before it reaches a user. Secondary gates: the round-trip and decoding-robustness tests of `docs/TARGET-SCHEMA.md` section 10, and the twenty built-in sheets all rendering without a validator error.

**Phase 0: Registration spike.** No UI. Command line only. Print the Phase 0a output, scan it, and photograph it off-axis.

**[r4] Gate, restructured after measurement.** Two gates, measuring two different things, replacing the single one-thousandth-of-an-inch criterion this document carried from the start:

| Gate | Measured on | Threshold |
|---|---|---|
| Conformance test 43 | A synthetic raster of the PDF | 0.001 in, worst bull-centre error |
| **Phase 0 paper gate** | A 600 DPI scan of a printed sheet | **0.005 in, worst bull-centre error** |
| **Phase 0 photograph gate, flat** | An off-axis photograph of a sheet held flat | **0.005 in, worst bull-centre error** |
| **Photograph gate, mounted** | An off-axis photograph of a sheet mounted as a shooter mounts it | **0.005 in, worst bull-centre error. Phase 1, and it needs a surface model** |

**[r5] The photograph gate has two halves, and only the first belongs to Phase 0.** A sheet held flat isolates the lens and the estimator. A sheet stapled to a target board is what the application will actually be given, and it is not flat: it bows between its fixings, it curls at a free edge, and it moves in wind. The nine Phase 0 photographs of a sheet hanging from a single pin measured the cost of pretending otherwise, and it is an order of magnitude: worst bull 0.052 to 0.114 in with the main camera (0.048 to 0.114 before the markers were sorted, `docs/NOTES-FROM-PLANNING.md` entry 52), against 0.0066 to 0.0118 in for the same sheet and the same lens lying flat.

**A global homography cannot represent that surface and no amount of lens modelling rescues it**, which the spike established by refitting with three radial coefficients and a free distortion centre, and with quadratic and cubic warps evaluated leave-one-marker-out, without bringing any frame inside the gate. The mounted case therefore needs a surface model, and section 6 of this document already names the right one without having connected it: **paper is a developable surface**. It bends without stretching, so distance along the sheet is preserved even where the projection is not planar. That is a far stronger constraint than a generic warp or a patchwork of local homographies, and it is the basis Phase 1 should build on.

Until that exists the mounted gate is expected to fail, and it is recorded as a gate rather than as a future nicety because it is the product requirement. A tool that measures groups from photographs of targets that must first be flattened is not a tool anyone will use at a range.

**[r6, 14 September 2026] A surface model now exists, and the mounted gate is still unmet.** Phase 1 built a generalised cylinder and then a general developable surface whose rulings turn across the page, which is as much freedom as a sheet of paper has, and fitted both to the seven mounted frames. No frame comes inside 0.005 in under either: worst scoring bull 0.006 to 0.067 in, one frame at a time. The general surface moves the corner residual by at most 0.16 px, so what is left is not a shape paper can bend into (`docs/PHASE1-RESULTS.md` M1.10 and M1.11). The requirement stays open at 0.005 in. Loosening it was considered and refused, because the error budget against the 0.008 in hole-centre noise floor argues for 0.003 to 0.005 in, and a number chosen after the results is not a gate (`docs/NOTES-FROM-PLANNING.md` entry 17). The recorded fallback is piecewise registration, `docs/PHASE0-RESULTS.md` section 4.5, and it is not attempted in Phase 1. If the gate is argued again, the number is fixed before new frames are measured and tested on frames not used to set it.

plus correct detection of a deliberately mis-scaled print, unchanged.

**Why the paper figure is not one thousandth.** `docs/PHASE0-PRELIM.md` has the measurement. On three separately printed sheets the bull centres recover at 0.0021 in mean and 0.0042 worst, and the error is a displacement field that **travels with the paper**: rotating a sheet 180 degrees on the platen rotates the field with it, at a correlation of +0.769 against its own unrotated scan, while the scanner-fixed prediction scores +0.185. It is the printer laying ink about 0.05 mm from where it was asked to, of which roughly 0.0019 in is systematic and reproducible and 0.0010 in is random. Software cannot recover it, because the fiducials are printed by the same head on the same pass and carry their own share of the same error.

**[r4, amended 13 September 2026]** The 0.0021 mean, 0.0042 worst, 0.0019 systematic and 0.0010 random figures above belong to the preliminary centroid. The Phase 0 spike's edge-fit locator, chosen on the synthetic raster before it saw paper, measures the same three sheets at 0.0013 in mean and 0.0032 in worst, split into **0.0013 in systematic and 0.0005 in random**, and a quadratic over the page absorbs most of the systematic part (`docs/PHASE0-RESULTS.md`). The field is still fixed to the paper, and the gate is unchanged.

This document has carried a clue to that since revision 1. Section 6 records 0.0026 to 0.0032 in rms on pristine sheets under a four-point homography, and nobody connected it to the gate.

**Why five thousandths is the right number.** It is set by what the measurement is for rather than by what the hardware happens to manage. A bull printed 0.05 mm off contributes 0.05 mm to that shot's offset, and across a composite group the bull-to-bull variation adds in quadrature with the true dispersion: on a rifle holding a sigma of 2.5 mm at 100 yards it inflates the estimated sigma by **0.02 percent**. For proportion, the measured noise floor of hole centroids in the sample scans is **0.008 in**, per `docs/TARGET-SCHEMA.md` section 3.4, so the bull is not the dominant instrument error and the gate sits comfortably below the thing that is. Every human and environmental term is larger again: a 5 mph crosswind at 100 yards moves a match bullet about half an inch.

**[r4] What the residual is measured on, because the first implementation had to ask.** The gate has two halves and only one of them is the pass criterion.

- **The measurement gate is bull-centre recovery**, worst case, every bull on the sheet: the distance between a bull's declared centre and the centre recovered from the image through the fitted homography. This is the quantity the application's accuracy actually rests on, it is what conformance test 43 measures, and one thousandth of an inch is the threshold.
- **The registration residual over marker corners is a diagnostic**, quoted as RMS with the maximum reported alongside. RMS is the statistic because a maximum over a point set grows with the number of points for any noise distribution, and the sheets carry between nine and seventy-three markers: gating on the maximum would hold a dense sheet to a stricter standard than a sparse one for no physical reason.

The distinction is not a convenience. On the Phase 0a synthetic renders the corner residual **halves when the render goes from 300 to 600 DPI**, from 0.00051 to 0.00026 inches RMS, which is the signature of an error measured in pixels rather than in millimetres: it is detector corner-localisation error, not geometry error. A geometry fault would not care about the raster. Gating a geometry pipeline on a detector-limited quantity at a fixed physical threshold would mean the gate could be passed by scanning at a higher resolution, which is not a property a correctness gate should have. Bull centres, found by a centroid over many pixels rather than by a corner fit over few, do not scale that way.

Both halves are still reported, and the corner residual is worth watching for a different reason: on synthetic renders its worst single corner is 0.00091 inches against the 0.001 inch figure, so there is very little headroom before paper, print scale and scanner noise are added. Part of that is a measured 0.10 pixel inward bias on every marker corner, which a homography cannot absorb because it is common to all of them. Chasing that bias is measurement 3 of `docs/FIDUCIAL-DECISION.md` section 10 and it is now a priority rather than a nicety. **[r3]** Additional measurements that make the gate informative rather than merely pass or fail are listed in `docs/FIDUCIAL-DECISION.md` section 10; the two that needed no printer have already been done.

**Phase 1: Detection spike.** Still no UI. Implement the eleven-stage pipeline of `docs/DETECTION-PIPELINE.md` against the Phase 0 images and the fifteen-file corpus.

**[r3] The Phase 1 gate as originally written no longer means what it was written to mean.** A tuned Hough circle detector reaches 26 of 27 true positives on the reference file, which is 96 percent, so "beat the naive baseline of roughly 25 percent by a wide margin" would be cleared by a method with no stable operating point. The measurements show what the real problem is: Hough at accumulator threshold 25 gets 26 true positives, at 40 it gets 10, at 55 it gets 0. A factor of 2.2 in one parameter takes it from near-perfect to blind, and the correct value depends on rim contrast, which varies by more than a factor of two with scan resolution and by 45 percent between holes on ink and holes on paper within a single scan. The naive methods are not bad at detecting holes; they are bad at detecting holes without being tuned per image, which is useless in a product.

**[r3] The replacement gate. Render-and-difference must clear all five, on the full corpus, with one parameter set.**

| | Criterion |
|---|---|
| G1 | Recall at least 95 percent across all 15 files with a single parameter set, no per-image tuning |
| G2 | False positives at most 1 per target, and zero from target artwork |
| G3 | **Parameter stability:** recall stays above 90 percent when every threshold is varied by plus or minus 30 percent |
| G4 | **Resolution invariance:** the same parameter set clears G1 at 93, 300 and 600 DPI |
| G5 | **Style invariance:** recall at least 95 percent on `retumbo.png`, the greyscale render where chroma is unavailable and artwork and hole are the same grey |

G3 is the one the naive methods fail and the one that actually distinguishes a shippable detector.

**Phase 2: Core and statistics.** Data model and statistics engine. Gate: statistical output matches shotGroups to within numerical tolerance on shared test data. **[r3]** The target definition format moves out of this phase into Phase 0a, where the renderer needs it. The shotGroups fixtures are already generated, by `tools/shotgroups/sg_dump.R`.

**Phase 3: Editor.** The manual editing and assignment interface, built against real detections including bad ones. Gate: a full 25-shot target with several misassignments corrected in under two minutes.

**Phase 4: Windows application.** Target library, generation, printing, analysis, reporting, session records. **[r7]** Also, named here because section 3 and section 19 promise them and no phase carried them: records for rifles, barrels and loads beside the sessions, the four themes of section 19, the stage timeline of section 19 that shows the analysis doing its work, the support link of section 20, the three-axis unit setting, adjust-to-zero turret corrections, and the volunteer print pack. The gate is unchanged.

**Phase 5: Chronograph, solver, and comparison.** Xero import, reconciliation interface, ballistic solver port validated against ballistics.js, load-versus-load significance testing, velocity regression, predicted-versus-measured vertical analysis. **[r7]** And the two things the solver is being ported to serve, which section 3 promises and this phase had left to inference: hit probability propagated to a distance other than the one shot, and distance normalisation. The gate is unchanged.

**Phase 6: Android.** Camera capture path, distortion fitting on device. Package ID chosen here and permanent thereafter.

**Phase 7: Synchronisation.** Cloud provider adapters, three-tier storage.

**Phase 8: iOS.** CI-based build and signing.

**[r7, 18 September 2026] The sweep of section 3 and section 19 against these phases, and the two deferrals it produced.** `docs/NOTES-FROM-PLANNING.md` entry 90 read every scope bullet and every interface commitment against this section and against the README's Planned section, and found ten promises with no phase behind them. Eight are now scheduled in the phase that was already the right home for them, and they are spelled out in the phase lines above and in the README's per-phase feature lists with a state each: hit probability and distance normalisation in Phase 5, records for rifles, barrels and loads in Phase 4, the secondary mode in Phase 3, the support link, the four themes and the stage timeline in Phase 4. **Two are deferred rather than scheduled**, because scheduling either would be guessing:

- **The parametric editor and the full visual designer.** The format is ready, and section 9 [r3] measured a fully custom sheet of forty arbitrarily placed bulls at roughly 215 bytes inside a QR payload with room to spare, so nothing in the format blocks it. What is missing is a specification of the authoring screen, and one is being written with the shooter who asked for it. Entry 84 section 1 makes the same argument for the editor's appearance: a screen specified before it is used is specified against a picture.
- **Assisted hole placement on a target with no definition.** The detector works by rendering the definition and differencing against it, and a store-bought target or a sheet of blank paper has no definition, so the method as built has nothing to difference. Whether anything weaker is worth having is question 18 of `docs/QUESTIONS-FOR-PLANNING.md`, and it is a design answer rather than a wording fix.

A deferral is a statement that the promise is still made and is not being worked on, with the reason recorded. It is not a quiet drop, and the test cannot tell the difference between a deferral and a scheduled phase except by the words, which is the point: both are explicit.

## 22. Risks

**Detection accuracy is the project.** Measured naive performance was 6 of 25. Render-and-difference should transform this, but that is a hypothesis until Phase 1 measures it. Mitigation: the editor is built early and is good enough that a mediocre detector still yields a usable product.

**Avalonia mobile maturity** is lower than its desktop maturity. Mitigation: the Core contains no UI types, so the Android shell can be replaced with .NET MAUI without touching measurement code.

**Imaging library availability differs across platforms.** Mitigation: the imaging backend is an interface from the first commit, and the fiducial design avoids depending on any single library's marker implementation. **[r3]** That mitigation is now concrete rather than aspirational: the printed marker family is readable by two independent implementations under two different licences.

**[r3] Licence conflict in the imaging stack**, which is a distinct risk from availability and has a different mitigation. Emgu.CV is plain GPL-3.0 with no app-store additional permission, and it is the only .NET OpenCV binding covering mobile. Mitigated by the AprilTag family choice, per sections 9 and 20. Residual risk: any Phase 6 requirement for an OpenCV routine on mobile that libapriltag does not provide reopens it.

**[r3] Patent exposure, now specific.** `docs/PATENT-SEARCH.md` found **US7769236B2** (Fiala, now Millennium Three Technologies), live to **2029-05-03** with all maintenance fees paid, whose method claim 12 describes single-image coded-marker detection with no video limitation. That claim reads onto what any ArUco or AprilTag detector does. The decision taken is to adopt a standard fiducial scheme and accept the risk, which is recorded with its bounds in `docs/FIDUCIAL-DECISION.md` section 9. **An attorney should read claim 12 before the design is frozen.** The closest target-design art, US11257243B2 (Targetscope), lapsed on 2026-03-30 for non-payment and is revivable until roughly February 2028.

**Trademark.** **[r3] Searched.** `docs/TRADEMARK-SEARCH.md` found zero USPTO records for GROUPLAB in any form, exact, wildcard, pseudo-mark or component. The three GROUP LABS marks on file are all dead. The real exposure is common-law: a GroupLab human-computer-interaction research group at the University of Calgary. No AI assurance of availability should be treated as a clearance.

## 23. Open items

**Closed in revision 3:**

- ~~Patent search on machine-readable target designs~~ `docs/PATENT-SEARCH.md`. One live patent, recorded in section 22
- ~~USPTO search on the chosen name~~ `docs/TRADEMARK-SEARCH.md`. Clear at USPTO, common-law exposure noted
- ~~Fiducial marker design: adopt an existing scheme or design a bespoke one~~ `docs/FIDUCIAL-DECISION.md`. AprilTag `tag36h11`, adopted rather than bespoke

**Still open:**

- Audit of the Pissin Hot Precision tool suite for components that apply here, particularly the ballistics engine, the cost calculator, and the BVA share API pattern
- HEIC, AVIF, and JPEG XL decoder and dependency decisions on Windows, including patent considerations for HEVC-based decoding
- Store policy verification for the support link before the Android and iOS releases
- Final name

**[r3] Opened in revision 3:**

- **Attorney review of US7769236B2 claim 12** before the fiducial design is frozen. Not blocking any code
- **An email to Emgu Corporation** asking for an app-store additional permission. Costs one email and the answer changes the Phase 6 plan
- **Transcribe the hand-drawn assignment arrows** into a fixture file. The only independent ground truth for cross-cell assignment in the corpus
- **Which backer material is actually used.** Irrelevant to scans, decisive for photographs. Two photographs of one target against two backers would isolate the one variable the scan corpus cannot exercise
- **A trustworthy ovality measurement**, deferred from the caliber work in section 12 to Phase 1
