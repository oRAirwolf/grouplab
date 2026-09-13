# GroupLab: Patent search on machine-readable targets for automated shot analysis

**Prepared** 12 September 2026
**Prepared for** Open item 1 of DESIGN.md section 23, and the risk recorded in section 22
**Status** Search report. Not a freedom-to-operate opinion, and not legal advice.

---

## 0. What this document is, and what it is not

This is a documented prior-art search. It records what was found, where it was found, and what each source actually says. Every legal status quoted below is followed by the source that stated it.

It is not a clearance. Three limits matter and none of them can be argued away:

1. **A search is never complete.** US applications are unpublished for eighteen months from priority. Anything filed after roughly March 2025 is invisible to any searcher today. A pending application whose claims have not yet been fixed can issue with claims nobody predicted.
2. **Legal status from Google Patents is a derived field.** Where this report gives a status, it is taken wherever possible from the **Legal Events** table on the Google Patents page, which is populated from USPTO transaction data supplied by IFI CLAIMS and carries specific dated event codes. That is materially stronger evidence than the coloured "Status" badge, which is Google's own inference. Both are still second-hand. Before acting on any status here, confirm it in USPTO Patent Center.
3. **Infringement is decided on claims, not on abstracts.** Where a document below is called "not a blocker", that is a reading of the claim language quoted, not an opinion. A claim chart prepared by a patent attorney is the only thing that settles it.

The instruction for this task was to declare nothing clear that cannot be verified from a primary source. Section 5 lists, explicitly, the things that could not be verified.

---

## 1. Executive summary

**The core GroupLab concept appears to be unoccupied.** The specific combination that defines the project, namely a printed paper target carrying distributed coded fiducials plus a 2D code that contains the complete target definition, analysed from a **single still image or scan taken after shooting**, was not found in any patent or published application located by this search.

**Nearly all of the relevant art requires a live camera.** This is the single most important structural finding, and it recurs with striking consistency. The commercially valuable problem in this field has been real-time scoring at a range: a fixed camera watches a target continuously and reports each hit as it happens. Almost every claim set found is written around comparing successive frames in a sequence. GroupLab does the opposite. It looks at a target once, after the fact, with no prior frame to difference against. That difference is not cosmetic. It is a claim limitation that these patents cannot be read around.

**One item requires a real freedom-to-operate review before Phase 0 freezes anything.** US7769236B2 (Fiala, now Millennium Three Technologies) claims a generic method for detecting square coded markers in an image, with no video limitation, and it is fully paid up and live until 3 May 2029. It is discussed in section 3.1 and it is the only genuine risk item in this report.

**The closest target-design art has just died.** US11257243B2 (Targetscope) claimed a paper target bearing fiducials, a border and a barcode. It lapsed on 30 March 2026 for non-payment of the three and a half year maintenance fee. That lapse is recent enough to be reversible, which matters, and section 3.2 explains why.

**Two adjacent concepts are freely disclosed and therefore unpatentable by anyone, including GroupLab.** Multi-bull composite grouping and target identification by printed barcode are both openly practised and documented by OnTarget's own commercial product. GroupLab may use them. GroupLab could not have protected them, and neither can anyone else.

### Risk table

| Item | Owner | Status | Bearing on GroupLab |
|---|---|---|---|
| US7769236B2 | Millennium Three Technologies | Live to 2029-05-03, all fees paid | **Needs FTO review.** Method claim 12 covers single-image coded-marker detection |
| US11257243B2 | Targetscope, Inc. | Lapsed 2026-03-30, revivable until approx. 2028-02 | Closest target-design art. Watch for revival |
| US20260071854A1 | Sensormetrix | Pending, pre-exam | Claims recite real-time during a shooting session. Monitor, claims not yet fixed |
| US20260094303A1 | Rivalshot Corp | Pending | Continuation in a live family. Monitor |
| US11727666B2 | Rivalshot Corp | Live to 2040-09-09 | Requires image frames in sequence. Not applicable |
| US10648781B1 | A. J. Behiel (individual) | Live to 2038-05-25 | Requires a pulse meter. Not applicable |
| US9230326B1 | Cognex Corp | Live to 2032-12-31 | Camera-calibration system with a motion stage. Not applicable |
| US20110218021A1 | Erik Anderson | Abandoned | Closest conceptual ancestor. Now free prior art |
| US20190063884A1 | Downrange Headquarters | Abandoned 2021-06-30 | Disclosed code-carries-geometry. Free prior art |
| US9135512B2 | Hewlett-Packard | Lapsed 2023-10-23 | Print and scan scale from fiducials. Free prior art |
| US9779323B2 | Whitelines AB | Reported expired | Printed marks give scale from a photograph. Free prior art |

---

## 2. Search methodology

Searches were run on Google Patents full-text (which indexes description as well as claims), on patents.justia.com, and via general web search for commercial products claiming patent protection. Patent pages were read directly rather than through summaries wherever the claim text could be reached.

Query families used:

- `("bullet hole") AND (fiducial)`, country US
- `(fiducial OR "registration mark" OR "reference mark") AND ("bullet hole" OR "shot group" OR "group size")`, country US
- `(fiducial OR "qr code" OR barcode OR "machine readable") AND (F41J)` (F41J is the IPC and CPC class for targets and target ranges), 228 results reviewed by title and snippet
- `("shooting target") AND ("aruco" OR "apriltag")`
- Product-led searches on Targetscope, Rivalshot, SensorMetrix, OnTarget, Ballistic-X, Blackhole, IllumiShot
- Assignee and inventor sweeps on OnTarget's developer, on Mark Fiala, and on Edwin Olson
- Concept searches outside the firearms field for self-describing calibration artefacts, print-scale verification, and photogrammetry coded targets

Forward and backward citation chains were followed from the closest hits, which is how the Fiala family and the Sensormetrix family were reached.

**Not searched.** Non-US national collections other than incidental EP, WO, CN, JP and KR family members surfacing in the same queries. Design patents. Unpublished applications, which are unreachable by anyone.

---

## 3. Findings in order of importance

### 3.1 US7769236B2, Marker and method for detecting said marker. The one to take seriously.

| Field | Value |
|---|---|
| Number | US7769236B2 |
| Title | Marker and method for detecting said marker |
| Inventor | Mark Fiala |
| Original assignee | National Research Council of Canada, recorded 2007-01-11 |
| Current assignee | Millennium Three Technologies Incorporated, recorded 2014-10-22 |
| Priority and filing | 2006-10-31 (application 11/589,726) |
| Granted | 2010-08-03 |
| Status badge | Active |
| Adjusted expiration | 2029-05-03 |
| Maintenance fees | 4th year paid 2014-01-21. 8th year paid 2018-07-25 with late surcharge. **12th year paid 2021-08-06.** No further maintenance fee is due. |
| Source | https://patents.google.com/patent/US7769236B2/en |

Mark Fiala is the author of ARTag, the 2005 fiducial system from which both ArUco and AprilTag descend. This patent is the ARTag patent.

**Apparatus claim 1 is not a problem.** Verbatim:

> 1. A marker detectable by visual means comprising: a. a polygonal border comprising of at least four non collinear salient points; b. a binary digital code on said marker; c. the binary digital code comprising information data, checksum and error correction; d. a first level of binary digital code readable at a first given distance; e. a second level of binary digital code readable at a second given distance; f. where said second given distance is less than said first given distance; and g. where said second level binary digital code is smaller in size than the first level binary digital code and does not interfere with the reading of said first level binary digital code.

Limitations (d) through (g) require **two nested code levels at different scales**. An ordinary ArUco or AprilTag marker carries one code level. On the face of the claim, printing standard ArUco markers on a target does not practise claim 1.

**Method claim 12 is the problem.** Verbatim:

> 12. A method for detecting a marker comprising the steps: a. detecting an image to be evaluated using an image sensor; b. using an edge detector to detect an edge in said image; c. grouping more than one edge into a polygon having salient points; d. calculating homography from polygon salient points; e. generating a list of homographies; f. extracting binary data from input image having homographies; q. verifying if the image is a marker by performing check sum and error correction functions; h. if the image is a marker, identify as a marker and verify binary data.

There is no two-level limitation and no video limitation. Read plainly, that is a description of how ArUco and AprilTag detection work on a single still image: find edges, group them into quadrilaterals, compute a homography per candidate, sample the bits, verify with a checksum and error correction. Claim 15 is broader still, covering edgel detection, grouping into line segments, and grouping segments into polygons, though breadth of that kind usually comes with corresponding vulnerability to pre-2005 prior art.

**Why the open-source licences do not help here.** AprilTag ships under BSD-2-Clause, which contains no patent language whatsoever, neither a grant nor a retaliation clause. OpenCV 4.5 and above ships under Apache-2.0, which does contain an express patent grant, but that grant runs only from OpenCV's own contributors. Millennium Three is not an OpenCV contributor. Neither licence provides any shield against a third party's patent.

**Countervailing points, none of which are a substitute for advice.** The claim is nineteen years past its priority date in a field with substantial pre-2005 art (the patent's own citations include Sony's US6650776B2 on 2D code recognition from 1998, Zhang's coded visual markers work, and ARToolworks). The assignee is a small Canadian entity, not an obviously litigious one. The patent expires 3 May 2029, which is likely to be at or before GroupLab's Phase 4. And a hobbyist open-source project distributed free is a commercially unattractive target. None of that is a defence. It is context.

**Recommendation.** This is the item to put in front of a patent attorney before the fiducial design is frozen. It is a narrow question with a narrow answer: does detecting standard square binary fiducials in a single still image practise claim 12 of US7769236B2, and if so, is claim 12 valid over pre-2005 art. Section 6 sets out the options that avoid the question entirely.

### 3.2 US11257243B2, Target shooting system, Targetscope, Inc. The closest target-design art, and it has just lapsed.

| Field | Value |
|---|---|
| Numbers | US11257243B2, from publication US20210065395A1 |
| Inventors | Stephen Wiley, Phillip Schweiger, Sih-Ying Wu |
| Assignee | Targetscope, Inc., Bear, Delaware, recorded 2022-07-08 |
| Priority and filing | 2019-08-30 (application 16/557,681) |
| Granted | 2022-02-22 |
| Adjusted expiration if maintained | 2040-07-16 |
| Source | https://patents.google.com/patent/US11257243B2/en |

**Legal events, quoted from the USPTO transaction record on that page:**

- 2025-10-13, FEPP: "MAINTENANCE FEE REMINDER MAILED"
- 2026-03-30, LAPS: "PATENT EXPIRED FOR FAILURE TO PAY MAINTENANCE FEES"
- 2026-03-30, STCH: "PATENT EXPIRED DUE TO NONPAYMENT OF MAINTENANCE FEES UNDER 37 CFR 1.362"
- 2026-04-21, FP: "Lapsed due to failure to pay maintenance fee", effective date 2026-02-22

The patent lapsed for non-payment of the three and a half year fee, effective 22 February 2026.

**This is the closest thing in the corpus to a GroupLab target.** Claim 1 as granted covers a shot detection device comprising a paper target with four fiducials at known separations, a continuous border adjacent to those fiducials defining a shooting area, and **a bar code on the target, spaced apart from the shooting area, identifying the known distances**. Dependent claim 3 goes as far as specifying the fiducials' CMYK values (cyan approximately 100, magenta 25 to 100, yellow 0, black 0 to 80, which is to say a blue that a red or infrared channel sees as white).

Even while live, the escape was clear: every apparatus claim requires the fiducials to "allow a machine vision algorithm to ... continuously lock the target ... **in real-time even when the target is moving**", and the method claims are built on frame-by-frame comparison against a reference target image.

**The caution.** A lapse for non-payment can be reversed. Under 37 CFR 1.378 a petition to accept unintentionally delayed payment is available for two years from the date of expiry, so this patent is potentially revivable until roughly February 2028. Intervening rights under 35 USC 41(c)(2) may protect activity begun during the lapse, but that is a question for counsel, not a plan. **Re-check this patent in Patent Center before each public release.**

### 3.3 US20260071854A1 and WO2024049898A1, Sensormetrix. Pending, and the only ArUco-on-a-target claim found.

| Field | Value |
|---|---|
| US publication | US20260071854A1, published 2026-03-12, application 19/106,611 |
| Family | WO2024049898A1 and A8 (PCT/US2023/031527), EP4581326A1 |
| Inventor | Anthony F. Starr |
| Applicant | Sensormetrix |
| Priority | 2022-08-30 (provisionals 63/402,460 and 63/402,451) |
| USPTO legal event | 2025-02-25, STPP: "APPLICATION UNDERGOING PREEXAM PROCESSING" |
| Source | https://patents.google.com/patent/US20260071854A1/en |

The specification is explicit about ArUco on a target: "one or more binary square fiducial markers (e.g., one or more ArUco markers)", "At the four corners of the target are ArUco markers. Each ArUco marker is encoded with a specific number that allows the system to identify which corner of the target is the upper left corner".

**There is exactly one independent claim, claim 1, and it is limited to real time.** It requires one or more computers configured, "**in real-time during a shooting session**", to "compare respective images in a sequence of the images to identify image data representing a projectile having hit the physical target". Every one of claims 2 to 23 depends from it.

Two dependent claims are worth recording because they show how close a competitor has come to GroupLab's ideas while remaining outside them:

> 7. ... the at least four reference points forced to fall exactly on the at least four predefined locations are fiducials located around one of multiple bullseyes on the physical target, and the fiducials are used to establish the metric for the physical target that relates the pixel-to-pixel distance with the real-world distance.

That is per-bull local fiducials establishing scale, and it is the same instinct as GroupLab's distributed marker field. Note however that it corrects distortion locally, per shot, rather than fitting one global lens model from thirty overdetermined points, which is what DESIGN.md section 11 specifies.

> 15. ... the physical target includes one or more binary square fiducial markers, and the one or more computers are configured to: locate points on the one or more binary square fiducial markers as the at least three reference points; and identify the scoring region for the physical target **using data encoded in the one or more binary square fiducial markers to identify the physical target as a specific target from a set of predetermined targets**.

This is identification, not self-description: the code names a target that the software already has in a set. GroupLab's payload carries the definition itself, so that a target the software has never seen can still be analysed. That distinction is the defensible core of the project and it is worth stating in exactly those terms in the repository.

**Because this is a pending application, the claims can still change.** Monitor it. Docket a check on the US and EP members every six months.

### 3.4 Rivalshot Corp: US11727666B2 and a live continuation

| Field | Value |
|---|---|
| Granted patent | US11727666B2, "Automatic target scoring machine", granted 2023-08-15, anticipated expiration 2040-09-09 |
| Inventors | Paul Clauson, Matthew Newell, Max Lee, Teja Veeramacheneni |
| Assignee | Rivalshot Corp |
| CIP | US12530802B2, "Automated image aberration identification system and method", granted 2026-01-20, expires 2041-08-15 |
| **Pending continuation** | **US20260094303A1, filed 2025-12-05, published 2026-04-02, status Pending** |
| Source | https://patents.google.com/patent/US11727666B2/en |

This family deserves attention because claim 1 of US11727666B2 is, in the abstract, uncomfortably similar to render-and-difference. It recites initialising the target by identifying the colours found on it, then "subtracting all colors from the analyzed frame that are in the pallet of existing colors to form a color agnostic frame" and treating the remainder as a potential shot.

**It is nevertheless a different technique and a different claim.** Three distinctions, all of them limitations in the claim itself:

1. The colour palette is **learned from the image at run time**, from "a first subset of the image frames". GroupLab does not learn the artwork. It already knows the artwork exactly, because it generated the target, and renders it geometrically into image space. That is a categorically different operation, and it is why GroupLab can subtract a ring that is partly destroyed by a hole while a palette method cannot.
2. The subtraction is in **colour space**. GroupLab differences in **image space after geometric registration**. A colour-agnostic frame discards where things are. Render-and-difference is entirely about where things are.
3. Every independent claim requires "receiving image frames", an "analyzed frame", and "ensuring that the potential shot remains in a later frame acquired at a specified later time". A single scan has no later frame.

The pending continuation, US20260094303A1, is the item to watch. Its claims are not yet fixed.

### 3.5 US20110218021A1, Visual image scoring, Erik Anderson. Abandoned, and the closest conceptual ancestor.

| Field | Value |
|---|---|
| Number | US20110218021A1, application 12/718,735 |
| Inventor and applicant | Erik Anderson, unassigned |
| Filed | 2010-03-05, published 2011-09-08 |
| Status | Abandoned. Never granted |
| Source | https://patents.google.com/patent/US20110218021A1/en |

Of everything found, this is the document whose author was thinking about the same problem GroupLab is thinking about. It is the only one built around a **scan of a shot target rather than a live feed**: claim 19 recites "an image scanning device ... adapted to accept said target sheet", and the specification prefers a scanner precisely because a camera introduces lens distortion.

It also anticipates two specific GroupLab ideas:

- **Caliber estimated from the holes rather than declared.** Claim 1 calls for "calculating from said captured image a standard puncture dimension based on the puncture edge data of at least two of said punctures". That is DESIGN.md section 12's caliber estimation, disclosed in 2010.
- **A code on the sheet that tells the software the sheet's geometry.** Claim 16: "a target scheme identifier, on said body, adapted to communicate to an electronic reader information that identifies position data of said bull and reference data."

Because it is abandoned and published, it is free prior art. It does not block GroupLab, and it makes it harder for anyone else to patent this territory. It is worth reading in full before the fiducial design is frozen, as a source of ideas already in the public domain and therefore safe to use.

### 3.6 US20190063884A1, Downrange Headquarters LLC. Abandoned, and it discloses the self-describing code.

| Field | Value |
|---|---|
| Number | US20190063884A1, application 16/111,639 |
| Inventors | Nathan Travis McBride, Michael Clark Graham |
| Applicant | Downrange Headquarters dba Spire Ranges LLC |
| Filed | 2018-08-24, published 2019-02-28 |
| Status | 2021-06-30, STCB: "ABANDONED -- FAILURE TO RESPOND TO AN OFFICE ACTION" |
| Source | https://patents.google.com/patent/US20190063884A1/en |

Claim 4, verbatim, and claim 14 in the same terms:

> 4. The system of claim 2, wherein the machine readable target identifier comprises one or more of the target size, the target distance, the target usage, and the target scoring information.

That is a printed code carrying the target's own parameters rather than a pointer to them, claimed in 2018 and now abandoned. Claim 3 recites the opposite arrangement, a code that is "a reference to a target data record ... stored in a data storage system", which shows the applicant understood both models and claimed both.

Both independent claims (1 and 11) require a vision system "coupled in a fixed position to the target carrier" that identifies changes "by comparing a current image of the target to a plurality of prior images of the target". Dead, and inapplicable even if revived.

**This is the most useful defensive prior art in the report.** It puts "printed machine-readable code carries the target's own geometry and scoring information" squarely in the public domain as of February 2019.

### 3.7 Print-scale verification: US9135512B2, Hewlett-Packard. Lapsed.

| Field | Value |
|---|---|
| Number | US9135512B2, from US20120275708A1, application 13/098,454 |
| Inventors | Terry M. Fritz, Jon Karl Lewis |
| Assignee | Hewlett-Packard Development Company, L.P. |
| Filed | 2011-04-30, granted 2015-09-15 |
| Status | 2023-10-23, LAPS and STCH: "PATENT EXPIRED DUE TO NONPAYMENT OF MAINTENANCE FEES UNDER 37 CFR 1.362". Lapse effective 2023-09-15 |
| Source | https://patents.google.com/patent/US9135512B2/en |

Claim 1 sets a unit of measure per axis from the pixel distance between printed fiducials, and locates features "independent of different resolutions of scanning the document along the respective first and second axes". That is anisotropic scanner-resolution correction from printed marks, which is exactly what DESIGN.md section 11 does when it reports measured horizontal and vertical resolution separately.

The lapse became effective 15 September 2023. The two-year window under 37 CFR 1.378 closed in September 2025, so revival is no longer routinely available. Treat this as free.

The related **US9779323B2** (Whitelines AB, Olof Hansson, priority 2012-06-15), covering low-contrast printed markers that let a phone app recover a sheet's size, scale and orientation and compensate for tilt and creases, is reported expired for fee non-payment. Its status was not independently confirmed against the legal-events table and is listed in section 5 as unverified.

### 3.8 US9230326B1, Cognex Corp. Live, and the nearest live art to the self-describing concept.

| Field | Value |
|---|---|
| Number | US9230326B1, application 13/731,308 |
| Inventor | Gang Liu. Assignee: Cognex Corporation |
| Filed | 2012-12-31, granted 2016-01-05 |
| Status | Active. 8th-year maintenance fee paid 2023-11-13. Anticipated expiration 2032-12-31 |
| Source | https://patents.google.com/patent/US9230326B1/en |

A machine-vision calibration plate in which embedded 2D codes carry the real-world coordinates of anchor calibration features, so the vision system learns the plate's metric layout without external data. This is the genuine ancestor of the self-describing-target idea and it is live.

It does not read on GroupLab. Claim 1 is a **system for calibrating at least one camera** and requires, in combination, a first camera, the calibration plate, a vision system process that derives a coordinate system for that camera, and a calibration system process that determines the camera's position and orientation relative to a common coordinate system based on either a predetermined positioning of the plate or "characteristics of a motion-rendering device that moves one of either the calibration plate and the first camera". GroupLab has no motion stage and does not calibrate a camera's extrinsics against one. Different article, different field, different claim.

Its value is as prior art: it puts "2D code on a printed artefact encoding that artefact's own real-world coordinates" in the public domain as of December 2012.

### 3.9 Cleared and inapplicable

| Document | Owner | Status | Why it does not apply |
|---|---|---|---|
| US10648781B1 | Arthur J. Behiel, individual | Active to 2038-05-25 | Both independent claims require sensing the shooter's pulse and correlating it with the image data. Claim 1 also has an antecedent-basis defect ("attached to the wrist band", never introduced). Pre-launch versus post-launch image comparison, no printed codes, no fiducials |
| US20120258432A1 | Outwest Systems Inc | Abandoned | Scope-mounted camera, real-time frames against a stored baseline. Calibration is a user-drawn known distance, not a printed code |
| US20160298930A1 | Squire, Cates, Howarth | Abandoned | Claim 11 recites "aligning the image on the target with fiducials", but the target image is projected and dynamic, and a second sensor watches the shooter |
| US9891028B2 | Rod Ghani, individual | Expired, fee related | Camera above the target at close range, 60 fps, hit found by comparing a frame to one about a second earlier. No codes on the target |
| US12152881B2 | Trimble Inc | Active to 2043-01-13 | AprilTags in surveying, but expressly requires target pose "saved in a remote database". The lookup model, not self-description |
| US10504231B2, US10929980B2, US11100649B2, US11887312B2 | Millennium Three Technologies | Active | Same inventor as 3.1, but every independent claim requires "a consecutive image sequence" and tracking a marker detected in a previous frame. A single scan is outside all four |

---

## 4. Concepts confirmed as free, and why that cuts both ways

### 4.1 Multi-bull composite grouping

No patent was found. The concept is publicly and prominently disclosed by OnTarget's own product literature, which describes creating "virtual groups" that combine multiple shots using a common point of aim from a workflow of printing a predefined target, shooting one shot per bullseye, and scanning. It is further disclosed by the shotGroups R package on CRAN, whose OnTarget import functions read per-shot coordinates paired with per-shot aim points and offer a combine parameter.

GroupLab may use this freely. So may anyone. It cannot be protected by GroupLab or against GroupLab.

### 4.2 Target identification by printed barcode

OnTarget's own site states that when a target is scanned, "TDS will read the barcode and recognize the layout and color of the target for automatic processing". This is the identifier-to-lookup model, publicly disclosed.

Note carefully what this does **not** disclose: a code that carries the definition rather than naming it. That step, which is what makes a GroupLab target analysable by software that has never seen its design, is the one thing in the whole architecture that nobody has occupied. US20190063884A1 claim 4 came closest and is abandoned.

### 4.3 OnTarget Software holds no patents

Inventor and keyword searches on Justia for the developer, and on the product names, returned nothing in this field. No patent marking or "patent pending" statement appears on the product pages that could be read. The developer's location, given in DESIGN.md as Westminster, Colorado, could not be confirmed from the site and should be checked before the attribution in section 4 of DESIGN.md is published.

### 4.4 Competing photo-based apps make no patent claims

Ballistic-X, Blackhole (9oClock Software GmbH) and IllumiShot (LLKM LLC) were checked. None carries a patent notice. Blackhole and IllumiShot both state explicitly that they use no fiducial markers, QR codes or encoded definitions, which is a useful confirmation that the marker-based approach is a genuine differentiator rather than the obvious path.

DESIGN.md section 22 refers to "at least one commercial product [that] analyses shots from photographs of targets it describes as patented". The products fitting that description are **Targetscope** (US11257243B2, now lapsed) and **Rivalshot** (US11727666B2, live), both of which are live-camera range systems rather than photograph-a-target apps. If a different product was meant, please name it and it will be checked.

---

## 5. What could not be verified

Stated plainly, because the instruction was to declare nothing clear that cannot be verified.

1. **No status here was confirmed in USPTO Patent Center directly.** Legal-event tables sourced from IFI CLAIMS were used where available, which is dated transaction data and much stronger than a status badge, but it is still second-hand. Patent Center is the register of record.
2. **US9779323B2 (Whitelines) status is unconfirmed.** Reported as expired, fee related. Its legal-events table was not read.
3. **The claims of EP4581326A1** (the European member of the Sensormetrix family) were not obtained. Only the US member's claims were read.
4. **Whether US11257243B2 will be revived** is unknowable. The petition window is open until roughly February 2028.
5. **OnTarget's corporate location and legal entity** could not be confirmed from primary sources.
6. **Nothing filed after approximately March 2025 is visible.** This is a hard limit of the eighteen-month publication rule and applies to every searcher.
7. **No design-patent search was performed.** Target artwork could in principle be covered by a design patent. This is a low risk given that GroupLab's artwork is original, but it was not searched.
8. **No non-US search** beyond family members surfacing incidentally. If distribution outside the US matters, EP and CA at minimum should be searched separately.

---

## 6. Recommendations before the fiducial design is frozen

**Do these before Phase 0 begins.**

1. **Put US7769236B2 claim 12 in front of a patent attorney.** One narrow question. Everything else in this report is comfortable. This one is not.

2. **Design the fiducial scheme so the question can be avoided if the answer is unwelcome.** Section 6 of the fiducial decision document develops this, but the shape of it is: a marker whose detection does not require the specific pipeline of claim 12 (edge detection, grouping edges into polygons, per-candidate homography, then bit extraction with checksum and error correction) is outside that claim on its face. A field of plain circular dots located by centroid, carrying index by position within a solved lattice rather than by decoded bits, uses none of those steps. It is also, independently, a better fit for GroupLab's actual requirement, since GroupLab needs thirty to forty precise point locations rather than a handful of decoded identities.

3. **Document the self-describing payload publicly and early, with a date.** Publishing the target definition format in a public repository establishes prior art against anyone later attempting to patent it. Given that US20260094303A1 (Rivalshot) is pending with unfixed claims and the Sensormetrix family is at pre-exam, this is worth doing before Phase 1 rather than after. A dated public commit is cheap and effective.

4. **Docket a six-monthly status check** on US11257243B2 (revival), US20260071854A1 and EP4581326A1 (Sensormetrix claims), and US20260094303A1 (Rivalshot claims). Fifteen minutes each time.

5. **Never use the University of Cordoba ArUco library.** It is GPLv3 with a paid commercial licence option, which is a licensing question rather than a patent one, but it is a trap that catches people who assume ArUco is BSD. Use OpenCV 4.5 or later, which is Apache-2.0 and includes an express patent grant from its contributors. This is developed further in the fiducial decision document.

6. **Correct or confirm the OnTarget attribution in DESIGN.md section 4** before the repository goes public.

---

## Sources

Patent documents, all read on Google Patents:

- [US7769236B2, Marker and method for detecting said marker](https://patents.google.com/patent/US7769236B2/en)
- [US11257243B2, Target shooting system](https://patents.google.com/patent/US11257243B2/en) and [US20210065395A1](https://patents.google.com/patent/US20210065395A1/en)
- [US20260071854A1, Camera detection of point of impact](https://patents.google.com/patent/US20260071854A1/en), [WO2024049898A1](https://patents.google.com/patent/WO2024049898A1/en), [EP4581326A1](https://patents.google.com/patent/EP4581326A1/en)
- [US11727666B2, Automatic target scoring machine](https://patents.google.com/patent/US11727666B2/en), [US12530802B2](https://patents.google.com/patent/US12530802B2/en), [US20260094303A1](https://patents.google.com/patent/US20260094303A1/en)
- [US20110218021A1, Visual image scoring](https://patents.google.com/patent/US20110218021A1/en)
- [US20190063884A1, Systems and methods for automated shooting evaluation](https://patents.google.com/patent/US20190063884A1/en)
- [US9135512B2, Fiducial marks on scanned image of document](https://patents.google.com/patent/US9135512B2/en)
- [US9230326B1, Calibration plate employing embedded 2D data codes](https://patents.google.com/patent/US9230326B1/en)
- [US9779323B2, Paper sheet or presentation board with markers](https://patents.google.com/patent/US9779323B2/en)
- [US10648781B1, Systems and methods for automatically scoring shooting sports](https://patents.google.com/patent/US10648781B1/en)
- [US20120258432A1, Target Shooting System](https://patents.google.com/patent/US20120258432A1/en)
- [US20160298930A1, Target practice system](https://patents.google.com/patent/US20160298930A1/en)
- [US9891028B2, Shooting game with dynamic shot position recognition](https://patents.google.com/patent/US9891028B2/en)
- [US12152881B2, Total station re-sectioning using two-dimensional targets](https://patents.google.com/patent/US12152881B2/en)
- [US10504231B2](https://patents.google.com/patent/US10504231B2/en), [US10929980B2](https://patents.google.com/patent/US10929980B2/en), [US11100649B2](https://patents.google.com/patent/US11100649B2/en), [US11887312B2](https://patents.google.com/patent/US11887312B2/en)

Non-patent sources:

- [OnTarget TDS product page](https://ontargetshooting.com/ontarget-tds/) and [TDS Target Image Page](https://ontargetshooting.com/tds-target-image-page/)
- [Justia inventor search, Mark Fiala](https://patents.justia.com/inventor/mark-fiala)
- [Justia inventor search, Edwin Olson](https://patents.justia.com/inventor/edwin-olson)
- [AprilTag licence, BSD-2-Clause](https://github.com/AprilRobotics/apriltag/blob/master/LICENSE.md)
- [ArUco, University of Cordoba AVA group](https://www.uco.es/investiga/grupos/ava/portfolio/aruco/)
- [OpenCV licence](https://opencv.org/license/)
- [shotGroups on CRAN](https://github.com/dwoll/shotGroups)
- [Blackhole app](https://www.blackhole-app.com/digitizing-a-shooting-target/), [IllumiShot](https://illumishot.com/), [Ballistic-X](https://ballistic-x.com/)
