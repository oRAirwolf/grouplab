# Questions for the planning session

Questions going out from the Claude Code session to the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** When a genuine decision blocks you, append a dated section at the top with `Status: open`. State the question, the options with their real costs, and what you would choose and why. Commit it, push it, and stop. The answer comes back as an `open` entry in `docs/NOTES-FROM-PLANNING.md`, and once it does, change this entry's status to `answered <date>` with a one-line pointer to the notes entry that answered it. Never delete an entry.

**What belongs here.** A decision that changes the specification, changes a gate, changes geometry, or commits the project to something that is expensive to reverse. A conflict between two documents. A measurement that contradicts something written down.

**What does not.** Anything a measurement can settle, because the planning session can run measurements against the committed scans and hand back numbers. Permission for work the brief already authorises. Anything answerable by reading the documents the brief points at.

**Write it for a reader who has the repository and not the conversation.** Quote the section you are citing rather than paraphrasing it, give the numbers rather than describing them, and name the file and line where the conflict lives. A question that arrives with its evidence attached usually comes back answered in one round trip rather than three.

---

## 2026-09-19, question 24: ballistics.js's Coriolis vertical term has its sign reversed, and a smaller wind-direction fault beside it

**Status: open.** Blocks only the Coriolis vertical term, which the port leaves out meanwhile. Everything else in entry 110 section 2 is built.

### 1. The conflict

Entry 110 section 2a lists "the Coriolis horizontal and vertical (Eötvös) terms" under **port as it stands**. The vertical term cannot be ported as it stands, because its sign is reversed.

`reference/ballistics-js/ballistics.js`, `coriolisVertical`:

> Firing east increases apparent gravity (bullet drops more), firing west decreases it.

It returns `-0.5 × 2Ω V cos(lat) sin(az) × t²`, which is downward for fire toward the east.

**The physics says the opposite.** In local east, north and up coordinates, the Earth's rotation is Ω(0, cos φ, sin φ). For a bullet moving east at speed V, v = (V, 0, 0). The Coriolis acceleration is −2Ω × v = (0, −2ΩV sin φ, +2ΩV cos φ).
- **Its vertical component is +2ΩV cos φ, upward.** This is the Eötvös effect: a body moving east is lighter, so fire toward the east strikes high and fire toward the west strikes low.
- **The size of the term is right:** half of 2ΩV cos φ times t², with V the average speed, is Ω X t cos φ. The fault is only the sign.
- **The horizontal term is right:** −2ΩV sin φ toward the south is to the right of a shooter facing east, in the northern hemisphere, which is what the file says.

At 45 degrees north, fire due east, 1000 yd and 1.6 s, the term is Ω × 3000 ft × 1.6 s × cos 45°, about 3.0 in. So the file puts the impact about 5.9 in from where the physics does, about 0.6 MOA.

### 2. The options

- **A. Port it with the sign corrected**, and a test that fire toward the east strikes high. About fifteen lines. This is what I would choose: the term is small, and its sign is not in doubt.
- **B. Leave it out** and say so wherever the solver's output is shown, as for aerodynamic jump.

**Meanwhile it is left out.** `grouplab trajectory` says "The Coriolis vertical (Eötvös) term is not modelled."

### 3. Beside it, not blocking

- **`solveExtended`'s wind direction.** It takes the crosswind as `speed × sin(windDir − azimuth)`, from a wind's from-direction, and adds it to the horizontal total with the sign that means right. So a wind from the east, fired north, drifts the bullet right, when it blows it left. Spin drift and Coriolis both use positive for right, so the total mixes conventions. The port does not carry the mapping over: it takes a signed crosswind, positive from the left, drifting the bullet right. The lag rule itself is ported.
- **Aerodynamic jump.** Entry 110 section 2c allows either Litz's published fit, checked against the book, or nothing. I have no copy of *Applied Ballistics for Long Range Shooting* to check the coefficients against, so it is left out, and the output says "Aerodynamic jump is not modelled." If you have the book, the fit, its units and its sign convention from the page would let it go in.
- **The BC's reference atmosphere, section 2e: how I read it.** The drag constant carries ICAO density. A BC stated against Army Standard Metro is flown with the density ratio taken against Army Standard Metro density: the density of 59 °F, 29.5275 inHg and 78 percent humidity, by the same formula. At the same air that is 1.8 percent more drag than the same number read as ICAO, the "percent or two" section 2e names. A test holds the ratio of the two densities at 1.018. If you meant a different convention, say which.

---

## 2026-09-19, question 23: entry 109, three statements the code does not bear out, and two places where the entry's own limits meet

**Status: open.** Blocks nothing. Entry 109 is built; what was done at each point is below, and sections 2 and 3 need only a yes or a correction.

### 1. Statements the code does not bear out

- **Section 3e, the crumb.** "The breadcrumb's middle crumb says 'the sheet'. Use the sheet's file name, as the editor's breadcrumb does." It already did: `ShowAnalysis` set the crumb to the image's file name whenever the marking has one, and fell back to "the sheet" only without one. The render entry 109 was read from came from a test that applied a detection without ever opening a file. **Done:** the fallback is now the sheet's own name from its definition, and the new renders open a file, so they show what a person sees.
- **Section 3b, the framing.** "Frame the group, not the bull, as entry 103 section 1 asked." The plot already framed the shots with their calibre outlines and never the bull. What made it look bull-sized was a margin of 35 percent of the group's extent on each side, which with .308 outlines on a 25-shot group came to about the bull's own size. **Done:** the margin is 10 percent, and the rings now run off the frame.
- **Section 2a, the tool strip.** "Entry 93 marks the icon tool strip as done, but the render shows text labels." The labels were entry 93's own choice, recorded at the strip: "The name stays beside the icon, because an icon alone is a guess for anyone who has not used the application before." So the strip matched its decision rather than falling short of it. **Done as entry 109 asks:** icons alone, each named with its key in a tooltip. The reason entry 93 gave is now answered by the tooltip, not by the strip.

### 2. Where the entry's own limits meet

- **The stringing power statement "stays visible ... Keep it to one line."** The statement is `ShapeTests.StringingPowerSentence`, one sentence of two or three clauses, for example "From 24 shots this test catches stringing of 2 times or more at least 8 times in 10, and often misses less: 1.5 times needs about 50 shots." At the 372 pixel column it wraps to three or four lines. Shortening it would reword the finding STATISTICS.md section 7 requires beside the result, which the covering message rules out. **Done:** it is kept whole, in view, as the card's one stringing line, and it wraps.
- **The panel "is then the review queue, the selected detection and the scale."** The panel also holds the group's inputs, the calibre, the shot distance, the rifle, barrel and load and the rounds fired, and the shot list. They are the task, not settings, and the entry names nowhere else for them. **Done:** they stay, below the scale, each section divided by a rule.

### 3. A size outside the five

Principle 2 says five styles and no more, and section 3a says mean radius keeps its lead size. The lead size is a sixth. **Done:** the five are tokens, the lead figure is the one named exception, and a test holds every text on the marking, analysis and settings screens to those six sizes.

---

## 2026-09-19, question 22: entry 107 section 1 asks for "9mm" refused, but its own rule reads it as a diameter; and the pick list is 37 diameters, not 36

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 108: option B, extended to inches. Calibre designations are refused in both units, the real diameters beside them still read, and the count is 37.

### 1. The conflict

Entry 107 section 1 gives the rule:

> **Millimetres, only when marked:** "7.82 mm" or "7.82mm" reads as 7.82 mm.

and, in its tests:

> names are refused with the sentence, including "300 Blackout", "6.5 Creedmoor", "30 Cal." and "9mm";

**"9mm" is a number marked mm, so the rule reads it, and the test says refuse it.** No syntax tells a metric calibre name from a diameter in millimetres: "9mm" and "9.02mm" have the same form. And the trap the section removes for bare numbers comes back for marked ones:

| Typed | Read as | The bullet |
|---|---|---|
| 9mm | 0.354 in | .355 in (9.02 mm), 0.03 mm off |
| 7.62mm | 0.300 in | .308 or .310 in, 0.2 mm off |
| 6.5mm | 0.256 in | .264 in, 0.2 mm off |
| 5.56mm | 0.219 in | .224 in, 0.13 mm off |

### 2. The options

- **A. Keep the rule as written.** "9mm" reads 0.354 in, near enough; "7.62mm" and "6.5mm" read 0.2 mm small. Costs nothing, and leaves the name trap open for anyone who adds mm to a metric name.
- **B. Refuse a millimetre value that is a metric calibre designation**, such as 5.45, 5.56, 5.7, 6, 6.5, 6.8, 7, 7.5, 7.62, 7.65, 8, 9, 9.3, 10 and 12.7, with the same refusal sentence. About twenty lines and a test. It is a short list of numbers, not names, but it is a list that can fall behind, which is what section 1 set out to remove.
- **C. Require hundredths in millimetres**, since bullet diameters in millimetres are quoted as 7.82, 9.02, 6.71. It refuses "9mm" and "6.5mm" but not "7.62mm" or "5.56mm", so it does not close the trap on its own.

**What I would choose: B**, because 7.62 and 6.5 are the numbers most likely to be typed with mm after them, and a refusal costs the person one retype while a wrong diameter costs a wrong edge-to-edge figure without any sign.

### 3. The count

The section says the pick list holds "the distinct diameters in Alan's two lists, 36 of them, from .172 to .510". Counting the distinct diameters in entry 105 section 7's two lists gives **37**, from .172 to .510 with .223 excluded. All 37 are in the pick list, and `docs/CALIBRES.md` lists them. If one should not be there, name it.

---

## 2026-09-19, question 21: printing from inside GroupLab, scoped, with the plan, its cost and three decisions it needs

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 107 section 2: build it. The margin refusal covers inked items and not the quiet zone, Linux and macOS keep the viewer path, and Print is the primary beside Open to print on Windows.

### 1. What entry 106 section 5 asks

> Scope it first. If the plan is clear, has no open design decision and fits one run with its tests, build it. Otherwise raise it as a question with the plan and its cost.

**It is raised, for three reasons.** It does not fit one run beside the rest of entry 106. Three design decisions in section 4 below are open. And the test the entry calls the one that matters depends on a printer the CI runner may not have, which I cannot check from here.

### 2. The plan

- **Windows, through Win32 by P/Invoke:** `PrintDlgEx` for the real dialog (printer, copies, pages), then GDI (`StartDoc`, `StartPage`, `EndPage`, `EndDoc`) to draw.
  - **Against `System.Drawing.Printing`:** it needs the `System.Drawing.Common` package, a new dependency, and it is Windows-only anyway, so it buys nothing over the Win32 calls it wraps.
  - **Against the WinRT print manager:** it needs a print document source built for its preview model, which is more machinery for the same result.
- **Drawn as vector from the scene, never a rasterised PDF.** The renderer's scene is in half-dmm. GDI can be set to a mapping mode with those units, so one unit of the definition is one unit on the paper. The scene's items are:
  - **Bull bands:** GDI paths from two ellipses, even-odd filled.
  - **Rectangles:** markers, codes and rules, each a GDI rectangle.
  - **Text:** set in Arial, the metric match of the Helvetica the PDF names.
- **True page coordinates.** `GetDeviceCaps` gives the printer's physical offsets, `PHYSICALOFFSETX` and `PHYSICALOFFSETY`, and every item is shifted by them, because GDI's origin is the printable area, not the paper's edge.
- **Refusals.**
  - **The paper:** a paper size in the dialog that is not the sheet's page, such as a Letter sheet on A4, is refused with both sizes named. Nothing is ever scaled and no scaling is offered.
  - **The margin:** any marker, code or bull that falls in the printer's unprintable margin is refused with the item and the margin named.
- **The confirmation, after `EndDoc` returns:** the printer, the sheet, the page count and "at actual size". It says the job was sent to the print queue, never that the paper came out.
- **Linux and macOS:** the viewer path stays, because a CUPS path needs its own dialog and its own margin query. That is the smaller half of the value and worth doing only once Windows proves the approach.

### 3. The cost

- **About one full run.** Roughly 300 lines of P/Invoke declarations and dialog handling, 200 of scene drawing and margin checks, the confirmation, and the tests.
- **The test the entry names:** print a sheet to a PDF printer and check that the markers in the output sit at their definition coordinates within 0.1 mm. It would print to "Microsoft Print to PDF" with an output file named in the job, bypassing the dialog, then rasterise the output with the test project's existing PDF rasteriser and run the registration on it.
- **The risk in that test:** whether `windows-latest` has "Microsoft Print to PDF" installed. It is an optional Windows feature on the server editions. If it is absent, the test skips on CI and the same check is run once by hand on Alan's machine: print the sheet to "Microsoft Print to PDF", then `grouplab measure` the saved file.

### 4. The three decisions it needs

1. **The margin rule's reach.** Refuse when a marker's quiet zone falls in the unprintable margin, or only when the marker itself does? The quiet zone is part of what makes a marker decode, so I would refuse on the quiet zone too.
2. **Linux and macOS.** Keep the viewer path there, as above, or build CUPS printing in the same work?
3. **Whether it replaces "Open to print" or sits beside it.** I would put Print beside it on Windows and keep Open to print everywhere, since a person with a colour-managed viewer workflow may still prefer it.

**What I would choose:** refuse on the quiet zone, keep the viewer path on Linux and macOS for now, and put Print beside Open to print on Windows. Given those answers, it is one run.

---

## 2026-09-19, question 20: the mark's light amber and its grey are not the tokens entry 105 section 4 says they are

**Status: answered** by `docs/NOTES-FROM-PLANNING.md` entry 106 section 2: option B. The palettes gain the mark's brand roles at the files' values, the mark is drawn from them, and `ThemeTests` holds the files and the roles to each other and the roles to 3:1.

### 1. What entry 105 section 4 says

> **The light variant** is the same geometry with the rings at `#8f8b83` and the holes at `#a9660f`, the light theme's amber.

> **The colours are tokens**, not literals: the grey is the rail and ring grey and the amber is `Tokens.Amber` in each theme, so the in-app mark follows the theme.

### 2. What the tokens are

In `src/GroupLab.App/Theme/Tokens.cs`:
- **Dark `Amber` is `#e0912f`.** It matches the dark mark.
- **Light `Amber` is `#965d12`, not `#a9660f`.** It was set for text contrast on the light panel, and `ThemeTests` holds it to that.
- **No token is `#6b727b`, `#8f8b83`, `#8a9199` or `#6f6b64`.** The nearest grey, `MarkFaint`, is `#697079`. The rail's icons are drawn in the text colours, not in a grey of their own.

So "the amber is `Tokens.Amber` in each theme" and "the light variant's holes are `#a9660f`" cannot both hold. The same goes for "the grey is the rail and ring grey" and a ring grey that no token has.

### 3. What I did meanwhile

The in-app mark and lockup are drawn from the committed SVG for the theme showing: the dark file in dark and high contrast, the light file in light. Each is drawn with the colours in the file, exactly as chosen. You named the files the source of truth, and redrawing the mark in colours nobody chose would be a guess. The icons use the dark file, as the entry says.

### 4. The options

- **A. The mark keeps its colours, as a fixed brand asset.** The files stay the source; the mark does not follow the theme beyond choosing its file. Nothing else changes.
- **B. The tokens gain the mark's colours as brand roles**, a ring grey, a word grey and a mark amber in each palette, set to the files' values. The mark is drawn from the tokens, and a test holds the files and the tokens to each other.
- **C. The mark takes the existing tokens.** Light amber becomes `#965d12` in the mark, and a grey is chosen from the palette. That changes a mark Alan chose.

**I would choose B.** It keeps what Alan chose, and it makes "the colours are tokens" true, so the mark follows a future palette change instead of silently diverging from it.

---

## 2026-09-18, question 19: entry 103 section 3 asks for a sort that entry 52 already committed

**Status: answered** by `docs/NOTES-FROM-PLANNING.md` entry 104 section 1: confirmed, nothing to commit and nothing to regenerate. The one surviving item, the option C risk, is in `DESIGN.md` section 22, and `QuestionStatusTests` now fails when a question marked open is one a notes heading says was answered.

### 1. What entry 103 section 3 asks

> `docs/QUESTIONS-FOR-PLANNING.md` question 15 asks whether to commit `MarkerDetection.InIdentifierOrder()` when doing so changes every printed table on Windows and no gate verdict. **Option A. Commit the sort and regenerate everything it touches.**

It also asks that `docs/PHASE0-RESULTS.md` section 4.5 be amended "with dated before and after figures", because "`ultrawide3.jpg` goes from 21 to 18".

### 2. What the repository shows

- **The sort is committed**, by the commit titled "Entry 52: sort the markers before use, and regenerate every record and quoted figure with them". `SheetMeasurer.DetectFiducials` takes both detection passes through `InIdentifierOrder()` (`src/GroupLab.Core/Measurement/SheetMeasurer.cs`, lines 183 and 194).
- **Question 15 was answered by entry 52**, whose heading is "question 15 answered, and the thing underneath it", with its status "actioned 2026-09-15" and its first bullet "the sort is committed with every record regenerated, and no verdict changed". Question 15's own status line was never changed from open, which is what made it look unanswered.
- **Section 4.5 already carries the before and after figures.** It reads: "Before entry 52 sorted the markers they read 0.015 to 0.091 in, 8 to 21 and 0.014 to 0.060 in. The frames, the markers and the corners are the same; only their order changed". Since entry 101 the figures to beat are 0.018 to 0.113 in and 8 to 22 of 25.
- **`docs/PHASE1-RESULTS.md` "Entry 52"** has the before and after table for every frame whose figures moved, headed by the sentence entry 52 section 2 asked for.

So there is no sort to commit and no regeneration it would cause. Running the sweep would produce the records entry 101 already committed.

### 3. What is still open in section 3

**The `DESIGN.md` section 22 risk for option C.** Section 22 has no entry for RANSAC's consensus. Entry 52 section 3 and the stability measurement ("Entry 52 sections 3 and 4") established the fragility, and entry 101 left RANSAC's inlier choice native. Adding the risk is documents only and needs no regeneration.

### 4. What I would choose

- **Question 15:** marked answered, pointing at entry 52 as well as entry 103. Done in this commit, because leaving it open is how section 3 came to be written.
- **Section 3's sort and sweep:** nothing to do. Confirm, and I will close this question.
- **The section 22 risk for option C:** add it in the next run, alone, as section 3 intended. It is held here only because section 3 as a whole rests on a premise that turned out to be stale, and the instruction was to stop and ask rather than act on part of it.

---

## 2026-09-18, question 18: assisted hole placement on a target with no definition, which the detector as built cannot do

**Status: answered** by `docs/NOTES-FROM-PLANNING.md` entry 103 section 4. "Assisted" means the snap, which is built; blank-paper detection is Phase 4 with a gate naming the material it needs; a user-traced definition on a bought target is the visual designer's second promise; option C is refused until somebody asks, and then as research with its own gate; option D is refused.

### 1. The promise, and why it has no phase

`DESIGN.md` section 3, In scope:

> A secondary mode that analyses any target, including store-bought targets and blank paper, using a user-defined scale and **manual or assisted hole placement**

Entry 90 section 5:

> Assisted hole placement on a store-bought target has no definition to render and difference against, so the detector as built cannot do it. **Whether that bullet is achievable at all is a design question**, and it should be answered before it is either scheduled or quietly dropped.

**That is correct about the detector.** `RenderDifferenceHoleDetector` renders the definition through the fitted registration and differences the image against it. With no definition there is no render, no artwork mask, no exclusion zones and no bull-cell position prior. Nothing in it degrades gracefully; it has no input.

**Everything else in the bullet is built.** The scale is set by a reference length or rectangle, marking by hand works on any photograph, and the whole editor, the queue and the statistics run off hand-marked shots.

### 2. What is already built and what it means for the word "assisted"

**Two of the three things "assisted" could mean already exist, and they exist without a definition.**

| What | Built | Needs a definition |
|---|---|---|
| **A tap snapped to the hole under it.** `Snapping.ToHole` takes the artwork as an optional argument. With no artwork it snaps to the dark centroid within the radius | **yes** | no |
| **A snap radius from the calibre**, so the snap is the size of a hole rather than an arbitrary distance | **yes** | no |
| **Finding holes unprompted**, with no tap to start from | yes, `RenderDifferenceHoleDetector` | **yes, absolutely** |

So the question is not whether assistance is possible. **It is whether "assisted placement" means the second column or the third**, and the document does not say.

### 3. Three cases, and they are not equally hard

**Blank paper is not the hard case and it may be nearly free.** A sheet with nothing printed on it has a predictable appearance: paper. Render-and-difference degenerates to finding dark blobs on a light field, which is what the difference stage does once the render is a constant. The size filters work, because the user has given a scale; the shape and solidity filters work unchanged. What is lost is the artwork mask, the exclusion zones and the bull-cell prior, and on blank paper there is nothing for them to do.

**A store-bought printed target is the hard case, and it is the one the bullet names first.** Rings, numbers, a logo and a scoring legend are dark, connected and shaped like nothing in particular. They are exactly what the artwork mask exists to remove, and there is no mask. The clean-photograph residue figures give the scale of the problem: on sheets where the artwork *is* modelled, 35 spurious detections survive on the clean frames. Unmodelled artwork would not add a few; each ring edge is a candidate the pipeline has no reason to refuse.

**A definition the user creates for a store-bought target is the third case, and it loops straight back into the deferred designer.** If the user can trace the rings of a bought target once, the sheet has a definition, the full detector applies, and this bullet becomes a use of the visual designer rather than a separate mode. That is the only route that gets the full detector onto bought targets, and it cannot be scheduled while the designer is deferred.

### 4. The options, with their real costs

| | Option | Cost | What the user gets |
|---|---|---|---|
| **A** | **Define "assisted" as the snap that exists**, and say so in section 3: a tap lands on the hole under it, at a radius set by the calibre. No unprompted detection without a definition | **none, it is built.** One sentence in section 3 and one feature line with a state | Every hole still needs a tap. On a 25-shot sheet that is 25 taps, which is the current secondary mode |
| **B** | **Definition-free detection on blank paper only.** The difference stage against a constant paper model, the existing size, shape and solidity filters, and every candidate into the review queue | **moderate.** A second entry point into the pipeline and a scale-only registration path. Measurable on donated material only if anybody shoots blank paper, and nobody in the corpus has | Unprompted detection where the sheet has no artwork, which is a real workflow: plain paper at a known scale is what people use for a quick ladder |
| **C** | **Definition-free detection on printed bought targets**, by learning the artwork from the image: fitting concentric rings, or exploiting the symmetry a bought target has | **high, and it is research.** No corpus material at all, and the failure mode is false positives on artwork, which is the one thing the Phase 1 gate forbids outright (G2: zero false positives from target artwork) | The bullet as literally written |
| **D** | **Drop "assisted" from the bullet** and promise manual placement in that mode | **none** | Honest, and it removes a capability the design has promised since revision 1 |

### 5. What I would choose, and why

**A now, B named in Phase 4 or Phase 5, C refused until somebody asks for it, and never D.**

- **A is already true and unsaid**, which is the worst state for a promise to be in. The snap is assistance, it works with no definition, and one sentence makes the bullet accurate today.
- **B is worth having and is small**, but it should be scheduled against a real photograph of a real sheet of paper with real holes in it, and no such image exists in the corpus. Naming it in a phase and shooting one sheet of plain paper at the next range session costs almost nothing and turns it into an ordinary measured feature.
- **C would put unmodelled artwork into the detector**, and the Phase 1 gate's G2 exists precisely to prevent that. It is also the case the user can solve for themselves under option three of section 3, by tracing the target once in the designer, which is a better answer than a ring-fitting heuristic.
- **D is a last resort** because nothing about the promise turned out to be wrong. The word was simply never defined.

### 6. What the answer needs to settle

1. **Which meaning of "assisted" section 3 intends**, so the bullet can be made accurate rather than hopeful.
2. **Whether blank-paper detection is worth a phase**, and if so which one. It is the only one of the three that is both useful and cheap.
3. **Whether a user-traced definition for a bought target is the intended route to the full detector on bought targets.** If it is, the bullet and the deferred designer are one item and should say so, and the designer's specification should carry it.

Until the answer comes back, the bullet is recorded as deferred in `DESIGN.md` section 3 and in the README's Planned section, both pointing at this question.

---

## 2026-09-17, question 17: the real holes veto the shipped split threshold, and only a higher one survives

**Status: answered 2026-09-17**, by NOTES-FROM-PLANNING.md entry 81. 1.80 goes in everywhere, not only with a calibre, and the gap it rests on is measured with pairs made from real holes.

### 1. What entry 80 section 2 asked

> Synthetic material is for coverage, because 848 holes explore the parameter space in a way 99 cannot. **Real holes get the veto.** A threshold that the synthetic sweep prefers but that misclassifies a single real hole is the wrong threshold.

### 2. What the sweep found

**The synthetic holes were rescaled first.** Unscaled, the current detector reads them at 0.335 in, which is 1.088 of .308. The survey they were fitted to mixed .264, .308 and .338. Scaled by 0.871, they read 0.291 in, 0.944 of .308, matching real scans. The run is `grouplab holes split-calibration --local <manifest>`.

**Blobs misclassified, split elongation against size veto.**
- **Synthetic** is the six scans punched with single holes: a single hole split in two.
- **Real** is Alan's scan, the friend's scan and the friend's photograph, which has no camera data: a single hole split in two.
- **Real photos** is the six mounted frames.

| Elongation | Size veto | Synthetic singles split | Real split | Real photos split | Real spurious |
|---|---|---|---|---|---|
| 1.45 (shipped) | none | 16 | 4 | 4 | 18 |
| 1.45 | under 1.2 to 1.8 holes | 0 to 3 | **2** | 0 | 9 |
| 1.60 | under 1.2 to 1.8 holes | 0 to 2 | **1** | 0 | 9 |
| **1.80** | **under 1.2 to 1.8 holes** | **0** | **0** | **0** | **9** |
| 1.80 | none | 3 | 0 | 0 | 17 |

**The two real blobs still split at 1.45 are each one hole joined to printed or photographed ink.**
- Alan's sighter hole beside the print note.
- A hole beside bull 10 in the friend's photograph.

Both hold more than two holes' area, so the size veto cannot see them. Only a higher elongation threshold leaves them whole.

**The synthetic pairs say what 1.80 costs.** Of about 840 merged overlapping pairs, 5 split at 1.45 with no veto and none at 1.80. The corpus holds no real merged pair, so the cost on real neighbours is not measured.

### 3. The options

1. **Elongation 1.80 with the size veto at 1.5 holes.** It misclassifies no real hole, and it halves real spurious detections, 18 to 9. **Cost:** it changes the default without a calibre as well, so the committed synthetic records move, and a real pair of neighbours whose shape alone separated them would stay one mark.
2. **Keep 1.45, and use 1.80 only when a calibre is named.** Nothing changes without a calibre. **Cost:** the two real holes stay split whenever no calibre is named.
3. **Keep 1.45 everywhere** until a sheet with real merged pairs exists. **Cost:** two real holes in nine images stay split.

**What I would choose: option 2 now, and option 1 once a real merged pair has been measured.** It is the one change that no real hole argues against and that moves nothing already recorded.

### 4. The photograph residue, entry 78 section 2, which depends on the answer

**On photographs every spurious detection is a split half:** 64 on clean sheets and 19 on real ones.

| | Elongation, median (range) | Solidity, median |
|---|---|---|
| Spurious blobs | 4.6 (1.54 to 7.4) | 0.65 |
| True holes | 1.17 (at most 1.67) | 0.95 |

**Why a plain cap is not enough.**
- **How elongated a real pair can be.** Two equal round holes that touch have an elongation of at most about 2.24.
- **What a cap would remove.** A cap there would remove 70 of the 83 spurious halves and no true hole.
- **Why it is not safe alone.** The difference stage's closing can join two real holes up to about 0.11 in apart, whose elongation can reach about 2.6. A plain cap could therefore silently drop two real holes.

**The proposed fix combines both conditions.** A blob the size veto calls too small for two holes, and more elongated than any single hole measured, is refused as residue rather than kept as one hole. Without a calibre, the size would come from the sheet's own round holes. It is not built, because it sits on the threshold chosen above.

---

## 2026-09-17, question 16: the printed name cannot go on the caption line without hiding holes on sheets already printed

**Status: answered 2026-09-17**, by NOTES-FROM-PLANNING.md entry 77 section 5. The name goes outside the analysed region, so it needs no exclusion box; a sheet with no such place carries no name. No box is added over the print note.

### 1. What entry 76 section 4 asked

> **The printed name: name first, identifier second, same caption line.** Something of the form `GroupLab 5x5 Load Development, A4 · GL-20J3-Y141-0BN3-EYME`.

> Establish whether the caption is excluded from the difference region before changing it; if it is not, that exclusion is part of this change rather than a follow-up.

### 2. What the check found

**The caption is inside the analysed region, and it is excluded only by a box sized to its own text.**
- **Why a box is needed at all.** `RenderDifferenceHoleDetector` builds the expected artwork with `SceneRasterizer`, which draws no text. Every printed word would read as a difference unless a zone covers it.
- **How the caption's box is sized.** The identifier's box is estimated from the text's length, at 0.6 times the font size per character, 25 dmm tall, plus 10 dmm all round. Its baseline is 80 dmm above the bottom edge.

**The change was built and measured.**
- **What was built.** The caption became name, middle dot, identifier. The box was widened to cover both the new caption and the identifier alone, which is what every sheet printed so far carries. It was measured with the exact Helvetica widths.
- **What happened on Alan's scan**, a sheet printed before the change: the detector found **13 holes instead of 15**. The wider box covers blank paper beside the old caption, and a real sighter hole sits there, at page (3.070, 10.573) in.
- **What was done.** The change was reverted in full, and the scan reads 15 again.

**So any caption wider than the one already printed hides holes on every sheet already printed**, because the analyser has no way to know which caption a given sheet carries.

### 3. The options

1. **Put the name on a line of its own, outside every region a hole can reach**, for example in the top margin beside a code. **Cost:** a new exclusion zone there, which on old sheets covers paper nobody shoots near. It also departs from "same caption line".
2. **Keep the name on the caption line, and exclude it only where ink is actually present:** threshold the band against the paper, and exclude only the glyph blobs that are found. **Cost:** a new mechanism in the detector, and a hole that touches a glyph could be excluded with it.
3. **Keep the name on the caption line, and accept that sheets printed before the change lose holes in the widened band.** **Cost:** a known silent miss on real sheets. I would not choose this.
4. **Encode which caption a sheet carries**, so the analyser excludes the right box. **Cost:** a GLTD change, and every sheet printed so far has no such field, so it only helps sheets printed afterwards. Old sheets would need the current box as their default.

**What I would choose: option 1.** It keeps every printed sheet reading as it does today, and it needs no new detector mechanism. The name is for the person holding the paper, and a line of its own is at least as easy to read.

### 4. A related exclusion, for the same decision

**The scan's detection at page (2.909, 10.784) in is not a hole.** It sits on the printed sentence "Print at actual size, 100 percent", whose baseline is 45 dmm above the bottom edge. That sentence is in no exclusion zone, which also answers entry 73 section 4.

- **What would fix it.** A box around that sentence alone, covering its glyph band plus 10 dmm. It would remove the false positive without reaching the sighter hole above it.
- **Why it waits.** It changes what the detector reads on every printed sheet, which is the same kind of change as section 3. It belongs with that decision.
- **What I would choose:** add the box, sized exactly to the sentence as printed today, which has not changed.

---

## 2026-09-15, question 15: sorting the markers changes the Phase 0 record on Windows, not only on macOS

**Status: answered**, option A, by `docs/NOTES-FROM-PLANNING.md` entry 103 section 3, and first by entry 52, which committed the sort and regenerated every record with it. This status line was left open when entry 52 was actioned, which is why entry 103 answered it again; question 19 records that the sort was already in.

### 1. What entry 49 section 2 asked

> Sort the markers by identifier before use. Unconditionally, and not to make a gate green.

### 2. What happened

**Implemented, measured, not committed.**
- **The change:** `MarkerDetection.InIdentifierOrder()` orders markers and rejections by identifier, then by their first corner. `SheetMeasurer` and `PageRegistration` take the detection through it.
- **Where it is:** the patch is kept outside the repository, and the tree is as committed.
- **The measurement:** the eight Phase 0 spike commands were rerun on Windows, and their printed tables compared with the ones committed in `scans/phase0/measurements/tables`.

**Every printed table changes on Windows, and no gate verdict does.**

| Table | Lines that change | Gate verdicts that change |
|---|---|---|
| `sheets` | 10 | none: the paper gate column is identical on every sheet |
| `photos` | 9 | none: the flat gate frames change in no count, and the mounted frames still fail |
| `markers` | 53 | not a gate |
| `refinement` | 21 | not a gate |
| `threshold` | 8 | none: every row still has 10 of 10 sheets inside the paper gate |
| `scale` | 1 | none |
| `field` | 4 | not a gate |
| `detectors` | 1 | not a gate |

**On the scans the figures move in the last places.** `gl-cf25-ltr-1-600-dpi.png`:
- **Residual maximum:** 0.00504 against 0.00507 in.
- **Worst bull by edge fit:** 0.00254 against 0.00251 in.

**On the mounted photographs the counts move:**

| Frame | Corners kept | Scoring bulls over the gate |
|---|---|---|
| `ultrawide1.jpg` | 66 against 68 of 136 | 13 against 14 of 25 |
| `ultrawide2.jpg` | 54 against 46 of 136 | 20 against 19 of 25 |
| `ultrawide3.jpg` | 42 against 51 of 128 | 21 against 18 of 25 |
| `main1.jpg` | 90 against 94 of 136 | unchanged |
| `main2.jpg` | 25 against 32 of 104 | 21 against 22 of 25 |

`ultrawide1.jpg`'s worst scoring bull goes from 0.03301 to 0.03902 in.

**Why.**
- **The mechanism:** `Cv2.FindHomography` with RANSAC draws its samples by position in the correspondence list.
- **On a flat scan:** the consensus is the same whatever the order, and only the refinement's last digits move.
- **On a curved sheet:** many corners sit near the RANSAC threshold, so a different sample path settles on a different consensus set, 42 corners against 51 on the same frame.
- **What that means:** the mounted figures are one arbitrary ordering of the detector's output. That is the order dependence entry 49 section 2 calls a defect, and it is larger than the platform comparison suggested.

### 3. Why this is a decision rather than a fix

**The mounted figures are a benchmark.** `docs/PHASE0-RESULTS.md` section 4.5 states them as "the figures to beat: worst scoring bull 0.015 to 0.091 in, 8 to 21 of 25 scoring bulls over the gate", and the Phase 1 surface work reports against them.

**Committing the sort changes committed evidence.**
- **Certainly regenerated:** the eight gate records and their tables.
- **Probably regenerated, not measured:** the other committed records computed through registration, such as the `surface-*.json`, `mounted-pair.json` and hole detection records.
- **Also re-derived:** every document figure that quotes those records.

**Not committing it** leaves a known order dependence in the pipeline.

### 4. The options

- **A. Commit the sort and regenerate everything it touches.**
  - **What it involves:** every committed record computed through registration, and every document figure quoting them, amended with dated before and after figures and the verdicts shown unchanged.
  - **Cost:** about an afternoon.
  - **Consequence:** the mounted baseline moves; for example `ultrawide3.jpg` goes from 21 to 18 scoring bulls over the gate.
- **B. Commit the sort and regenerate only the gate record.**
  - **What it involves:** the other records are marked as measured in the detector's order until their next rerun.
  - **Cost:** smaller.
  - **Consequence:** the committed records then hold two orderings, the kind of unmarked disagreement entry 49 section 1 warned against in another form.
- **C. Remove the dependence at its source.**
  - **What it involves:** replace RANSAC's random consensus in registration with a deterministic robust fit, such as an iteratively reweighted homography over all corners.
  - **Cost:** larger, since it changes the figures as well.
  - **Consequence:** it answers the fragility in section 2 rather than fixing one ordering of it.
- **D. Do not sort now.** Record the order dependence as a known defect beside entry 49 section 5's edge fit finding, and decide after the range session.

### 5. What I would choose, and why

**A, and C as the question behind it.**
- **Why A:**
  - A record that follows one stated order is worth more than figures that were one arbitrary order.
  - No verdict changes.
  - The cost is an afternoon now, against more later, when the range session's frames have been measured under the old order too.
- **Why C matters:** RANSAC's consensus on a curved sheet moving with an input that should not matter is the same class of fragility as the edge fit's one point of thirty. The sort makes it repeatable; it does not make it stable.

---

## 2026-09-15, question 13: where donated images live, and coordinates that are already in the repository's history

**Status: answered 2026-09-15**: section 1 by `docs/NOTES-FROM-PLANNING.md` entry 28 section 4 (option A, a GPL-3.0 data repository), section 3 by entry 28 section 1 (the real `meta.json`), and section 2 by entry 29 (option (a), approved by Alan). Before that it read: blocks committing any image: `scans/mounted/`, held by entry 23 section 5, and every donated submission. Nothing else waits. The intake tool, its tests and the publication test are built and committed, and they work against whatever directory the answer names.

### 1. Entry 22 section 1: where the images live

**What the repository is.**
- **History:** one pack of 157.5 MiB, after one rewrite already.
- **Tracked images:** 78, which with the PDFs come to 163 MB on disk, most of it the Phase 0 scans the gates and tests read.
- **What is arriving:** the mounted set held back is 28 files and 80 MB, and entry 22 estimates half a gigabyte to a gigabyte of donations.

**The options, with what each costs here.**

- **A. A separate data repository, `grouplab-testdata`.**
  - **For it:** the code repository stays near its present size and keeps its history. The data carries its own licence, provenance and lifecycle.
  - **How the code finds it:** a pinned commit and URL recorded in this repository, and a checkout beside it. The publication test already runs against a directory and does nothing when the directory is absent.
  - **Cost:** two repositories to keep in step, and tests that need donated images say they did not run on a machine without the checkout.
- **B. Git LFS here.**
  - **For it:** one repository.
  - **Cost:** every contributor needs LFS. A hosted LFS quota of the usual size is smaller than the corpus entry 22 expects, so it becomes a billing question as well. Adding LFS after images land means rewriting history again.
- **C. A curated subset here, the rest elsewhere.**
  - **For it:** cheapest to set up.
  - **Cost:** a person curates every submission, and the breadth that makes a donated corpus worth having stays outside.

**What I would choose: A,** for the reason entry 22 gives: the two have different lifecycles. `scans/mounted/` would go there too, scrubbed, as the first contents. The Phase 0 and Phase 1 scans stay here, because committed tests and gate records read them by path.

### 2. A finding: coordinates are already committed

`PublicationTests` reads every committed image. **16 of the 78 carry an EXIF GPS block, all of them Phase 0 phone photographs, and 13 of those hold a non-zero latitude and longitude:**
- `scans/phase0/20260913_130543.jpg`, `_130550`, `_130554` and `_130559`;
- `main1` to `main3`;
- `telephoto1` to `telephoto3`;
- `ultrawide1` to `ultrawide3`.

The other three, `main_flat1` to `main_flat3`, carry an empty position. The coordinates were not printed anywhere in this session.

They are Alan's photographs, so there is no consent question, but they are his coordinates in a repository that is going public. Entry 23 section 5 gave the same reason for scrubbing `scans/mounted/`.

**The options.**

- **(a) Rewrite history before the repository goes public.** Replace the 16 files in every commit with scrubbed copies.
  - **What stays the same:** `ImageScrubber` changes no pixel, and a test shows `main1.jpg` decodes identically scrubbed. So every measurement stands.
  - **What changes:** the file bytes, and so any recorded hash of them.
  - **The cost:** a second `git filter-repo` and a force push, which rewrites every clone.
- **(b) Scrub at the tip only.** One ordinary commit. The coordinates stay in history for anyone who looks.
- **(c) Leave them.**

**What I would choose: (a), before the repository is public,** because after that no rewrite takes them back. The rewrite and the force push are not mine to do. They are irreversible and they change what everyone has cloned, so I have not run either.

**Meanwhile the test holds the line.** It names these 16 files and fails if any other committed image carries GPS. It also fails if one of the 16 is scrubbed and left on the list, so the list cannot go stale.

### 3. What the intake tool assumed about the upload page, to confirm or correct

`grouplab intake <submission> <public directory> [--accept <file>]...` implements entry 22 section 2 with entry 27 section 1's triage. It reads these fields of `meta.json`, which no document specifies:
- **Provenance:** `submissionId`, `consentVersion` and `submittedAt`, all required.
- **Answers:** `answers`, copied into the provenance record as they are.
- **Opt-out:** `doNotPublish` or `optOut`, as true, beside the `DO-NOT-PUBLISH` file, which it checks first.
- **Hashes:** `files`, either an object of name to SHA-256 or an array of `{ "name", "sha256" }`.

If the page writes other names, it is a small change. A submission missing any of these is refused with the reason, never published.

**Triage, entry 27 section 1.**
- **The rule:** a file is a candidate when at least four GroupLab markers decode, which is what registration needs.
- **Everything else is held, not published,** with the reason written per file into the provenance record, until a person accepts it by name with `--accept`.
- **Not built:** the rectangular sheet boundary check entry 27 mentions. A commercial target a person judges usable for the manual path is exactly what `--accept` is for.

---

## 2026-09-15, question 14: shotGroups' Fligner-Killeen statistic on the two frames with a point of aim

**Status: answered 2026-09-15**, by `docs/NOTES-FROM-PLANNING.md` entry 28 section 3: the input is `shots.xPOA`. The difference turned out to be the fixture's 15-digit JSON, which drops the last bits of every aimed coordinate and so changes the ties; with the aims recovered and the subtraction redone, all four statistics match to 5.5e-13, `docs/STATISTICS.md` section 15.4 item 15. Before that it read: nothing waits on it. Four keys are pending with this question named, and every other key of the regenerated fixtures is compared.

**Where it stands.** With `shots.xPOA` and `shots.yPOA`, question 11's 2,163 awaiting keys are compared, and all of them pass. The exceptions are `compareGroups.FlignerX.statistic` and `compareGroups.FlignerY.statistic` on `DFinch` and `DFcm`, the two frames with a point of aim. On every frame whose aim is the origin, `GroupComparison.FlignerKilleen` matches shotGroups to 1e-13, `DF300BLKhl` at 0.09095279082138376 against 0.09095279082139468.

**What was tried**, on the aimed coordinates unless stated, with series as the groups, in a probe not committed:

| Variant | `DFinch` X | `DFinch` Y | `DFcm` X | `DFcm` Y |
|---|---|---|---|---|
| **shotGroups** | **10.075167218103388** | **2.804615516292583** | **10.864287796660232** | **2.808136916489293** |
| GroupLab: median by double (a + b) / 2, mid-ranks | 10.073187875409229 | 2.8032500664687734 | 10.860814627148528 | 2.8084659551083533 |
| Average scores for ties instead of mid-ranks | 10.073188286646975 | 2.803252133360352 | 10.860810402714064 | 2.808471773103334 |
| Median averaged in extended precision | 10.07066704956823 | 2.8051598279443146 | 10.865351991682529 | 2.811102334457731 |
| Raw coordinates, `shots.x` and `shots.y` | 10.072777268231373 | 2.801201252282855 | 10.945938713945823 | 2.839215001709699 |
| y negated, for `xyTopLeft` | | 2.8032500664687734 | | 2.8084659551083533 |

- **Nothing reaches 1e-5.** The differences, 1.2e-4 to 4.9e-4 relative, are the size a handful of ties produce, and these frames have them: series of 46 to 92 shots with up to 9 exactly tied absolute deviations.
- **The other compareGroups keys pass on the aimed coordinates,** the MANOVA intercept row included. That row is not shift-invariant, so compareGroups does read the aimed frame.
- **So the input vector is the unknown.**

**The ask: one R run.** In `sg_dump.R`, emit the vector compareGroups hands to its Fligner-Killeen test for each axis, as `compareGroups.FlignerX.input.<i>` and `compareGroups.FlignerY.input.<i>`, beside the statistic. That will say whether the values are centred or rounded differently, or tied differently.

**What I would do with the answer.**
- **If the input differs:** match it, and the four keys are compared like the rest.
- **If the input is the same and the statistic still differs:** it is R's tie handling at the last bit, and it goes to `STATISTICS.md` section 15.4 as a thirteenth known difference.

---

## 2026-09-15, question 12: the fewest shots a group size is quoted for, and entry 24's coverage premise

**Status: answered 2026-09-15**, by `docs/NOTES-FROM-PLANNING.md` entry 28 section 2: option A, five shots. Before that it read: nothing waits on it. The marking panel withholds every dispersion figure below 5 shots in the interim, which is one constant, `GroupAnalysis.MinimumShotsForDispersion` in `src/GroupLab.Core/Marking/GroupAnalysis.cs`.

**What entry 24 section 1 asks.** "Pick the thresholds from `STATISTICS.md` section 9, which already models how well sigma is known from n shots, rather than from anybody's taste. If section 9 does not give a clean answer, raise it as a question rather than choosing a round number."

**What section 9 gives.** Section 9.1's table is continuous in n and sets no threshold. It names one row: "**The five-shot row is the one to put in front of a user.** A five-shot group locates the rifle's true dispersion somewhere between 0.675 and 1.916 times the measured value, a factor of 2.84." Five is taken from that sentence. The same interval at the counts in question, from `SampleSize.SigmaIntervalMultiples`:

| n | True sigma, as multiples of the measured | Factor |
|---|---|---|
| 2 | 0.521 to 6.285 | 12.1 |
| 3 | 0.599 to 2.874 | 4.8 |
| 5 | 0.675 to 1.916 | 2.84 |
| 10 | 0.756 to 1.479 | 1.96 |
| 20 | 0.817 to 1.289 | 1.58 |

**The coverage premise is not the one entry 24 cites.** Entry 24 section 1 cites entry 23 section 2's 79.5 percent at ten shots. That figure is the bootstrap BCa interval for the Grubbs-Patnaik CEP, which the panel does not show. The panel's intervals are closed form or come from the simulated range-statistic table, and their coverage, measured over 40,000 circular normal groups at each n in `tests/GroupLab.Core.Tests/Statistics/SmallGroupCoverageTests.cs`, is:

| n | Mean radius and sigma, exact | Simulated | Extreme spread, shotGroups' `getRangeStat` form | Extreme spread, the form the panel now uses |
|---|---|---|---|---|
| 2 | 92.51 % | 92.60 % | 84.66 % | 95.10 % |
| 3 | 93.70 % | 93.50 % | 89.39 % | 94.96 % |
| 5 | 94.34 % | 94.42 % | 92.31 % | 95.17 % |
| 10 | 94.71 % | 94.78 % | 93.91 % | 94.93 % |
| 20 | 94.86 % | 94.92 % | 94.68 % | 94.94 % |

- **Mean radius and sigma** are under nominal only because the c4 correction multiplies both endpoints, which is how shotGroups does it and what brief section 5 has GroupLab match. The exact coverage is `P(q_lo / k² ≤ χ²(2(n − 1)) ≤ q_hi / k²)` with `k = 1 / c4(2n − 1)`, now `IntervalCoverage.RayleighSigma`, and the panel labels each interval with it, "94.3% interval" at five shots, never a bare 95.
- **Extreme spread's shotGroups form** scales the observed spread by the table's quantiles over its mean, and so does not cover the expected spread at its stated level. The panel now uses `RangeStatistics.MeanInterval`, observed times mean over the quantiles, which does. The harness still compares shotGroups' form against shotGroups. This is handled and not a question; it goes to section 15.4 as a known difference with the next statistics amendment unless you say otherwise.

**So Alan's two-shot interval was not overconfident about coverage.** 0.476 to 5.744 in covers the truth 92.5 percent of the time. What misled was a headline to three decimals over an interval spanning a factor of twelve, which is a width problem, and withholding the figure fixes it either way.

**Options.**

- **A. Five, as built.** Section 9.1's named row. Between 5 and 19 shots the panel also prints section 9.1's range in words: "From 5 shots the true group size could be anywhere from 0.68 to 1.92 times what they measure."
- **B. Ten.** The factor falls to 1.96, and it matches your example sentence, "10 before the interval means much". It withholds every five-shot group, which is most of what people fire.
- **C. Five for the figure and ten for the interval.** Print the figure from five and its interval only from ten. This hides the one thing that says how little five shots know, so I would not.

**What I would choose: A.** It is the row section 9.1 already tells us to put in front of a user, and the range sentence up to twenty carries the rest of entry 24's "between that minimum and about twenty". If you pick B it is one constant and one test.

---

## 2026-09-14, question 11: three differences the M3 harness found between shotGroups and its own fixtures

**Status: answered 2026-09-15**, by `docs/NOTES-FROM-PLANNING.md` entry 23 section 1: A, A and A.

**Where it stands.** `tests/GroupLab.Core.Tests/Statistics/ShotGroupsFixtureTests.cs` accounts for every key of the nine fixtures:

- **Compared:** 38,899 keys, including the range statistics through GroupLab's own Monte Carlo table and `compareGroups`, 0 outside section 15.3's tolerances.
- **Excluded, with the specification's or the fixture's reason:** 23,602.
- **Pending, not yet built:** none.
- **Awaiting or disputed:** 2,163 and 998 keys, for the three reasons below. The counts grew from this question's first version because `compareGroups` also takes the frame and also reports a CorrNormal CEP.

**1. The fixture cannot reproduce anything shotGroups computed from the data frame for `DFcm` and `DFinch`.**

- **The mechanism:** `groupLocation`, `groupSpread` and `groupShape` take the frame, which carries each shot's point of aim. The fixture carries `shots.x` and `shots.y` from `getXYmat(..., relPOA = FALSE)` and no aim.
- **The fixture's own evidence:** its frame-based centre disagrees with its matrix-based centre (`groupLocation.ctr` against `getConfEll.ctr`) in 10 of 10 scopes of `DFcm` and 10 of 10 of `DFinch`. For example, `DFinch` series 8 gives x = 0.3607 against 7.7067.
- **The other seven datasets:** agree in every scope.
- **Where spread is affected too:** covariance, `groupSpread.covXY` against `getConfEll.cov`, differs in the pooled scope of both. It also differs in `DFcm` series 5 but not in `DFinch` series 5, although section 15.2 calls them the same data.
- **What the harness does:** these 2,163 keys, including the two datasets' `compareGroups`, are routed to "awaiting" by that test on the fixture, not by dataset name.
- **Why it matters for the gate:** section 15.5 point 2, `DFcm` and `DFinch` agreeing after conversion, is exactly what they would show.
- **And the two are not the same data.** Every `DFcm` shot is a `DFinch` shot times 2.54 to 1e-15, but shot 242, at (15.25016, -10.90422) cm, is in series 5 of `DFcm` and series 4 of `DFinch`, so those two series hold different shots in the two frames (series 5 has 47 shots against 46). `tests/GroupLab.Core.Tests/Statistics/UnitSystemTests.cs` asserts that this one shot is the only difference, then shows GroupLab's results agree to 1e-12 after conversion with both grouped the inch file's way, and its angular results differ by exactly the rounding of 25 m to 27.34 yd, 1.2e-5. Point 2 is met on the shots; it cannot be met on the frames as shipped.

**2. shotGroups' CorrNormal CEP is looser than its own distribution, and section 15.3 compares it at 1e-8.**

- **The distribution agrees:** its CorrNormal hit probabilities, `getHitProb`, match GroupLab's Hoyt CDF to 1e-15 on every fixture.
- **The quantile does not:** under that same CDF, shotGroups' own CEPs miss their probability. On `DF300BLK` the misses are +3.7e-6, +2.9e-6 and -5.8e-7 at 0.50, 0.90 and 0.95. GroupLab's CEP meets it to 1e-12.
- **The size of the difference:** 3e-6 to 2.3e-5 relative, across 899 keys, each checked by the harness for exactly that evidence.
- **The likely cause:** a root finder with a coarse default tolerance.

**3. shotGroups' `fromMOA` for SMOA is 1 + 6.21288e-10 times the exact inverse of its own `getMOA`.**

- **Where:** in every scope of every fixture, 99 keys.
- **The tolerance it misses:** section 12.5's anchor, "1 inch at 100 yards is exactly 1.000000 SMOA", holds for `getMOA`, but the round trip misses section 15.3's 1e-12 for angular conversions.

**Four findings, handled and not questions.**

- **`getMinBBox`:** its angle is the direction of the longer side, while its width is always the first side.
- **`getMaxPairDist`:** it reports whichever tied pair R met first, which on `DFlandy04` differs from GroupLab's, at the same 0.442108583947428.
- **The fixture's MANOVA is the intercept row.** `sg_dump.R` takes `MANOVA[1, ]`, which in R's `anova.mlm` tests whether the mean over all shots is the origin, not section 8.2's test of the group centres. GroupLab reproduces that row for the gate, to 1e-13 on the five datasets whose frames are not shifted, and computes the group test separately. Row 2 would be the group test.
- **The multi-group p-values are Monte Carlo.** Every Fligner-Killeen and Kruskal-Wallis p-value is an exact multiple of 1/9999 (`DF300BLKhl`: 9554, 2385 and 6623 over 9999), so `coin` resampled for them, and like `groupShape.multNorm.p.value` no other generator can reproduce them. Their statistics match, and the harness excludes each p-value only after checking that it is such a multiple. The two-group Ansari-Bradley and Wilcoxon p-values are exact and match to 1e-15.

**Options.**

- **For 1:**
  - **A.** Regenerate the two fixtures with each shot's point-of-aim-relative coordinates as well, `getXYmat(..., relPOA = TRUE)`, as `shots.xPOA` and `shots.yPOA`. It is one R run.
  - **B.** Leave the 2,163 keys out of the gate and say so.
- **For 2:**
  - **A.** Gate the CorrNormal distribution at 1e-8 through the hit probabilities, which already pass. Require GroupLab's CEP to satisfy that distribution at 1e-12, and compare shotGroups' CEP at 1e-4 relative.
  - **B.** Keep 1e-8 on the CEP and replicate shotGroups' root finder, including whatever tolerance it happens to use.
- **For 3:**
  - **A.** Add it to section 15.4's known differences.
  - **B.** Reproduce shotGroups' constant.

**What I would choose: A, A and A.** Each keeps GroupLab exact where shotGroups is not, and keeps the gate checking something true.

**Not a question, but yours to know.** `grouplab stats coverage` measures GroupLab's BCa bootstrap covering a known truth 79.5, 89.1 and 92.7 percent of the time at 10, 25 and 50 shots, against a nominal 95. Section 6 flags only groups under 10 shots as unreliable. Nothing in section 15.5 gates it; it bears on what the interface should say beside a bootstrap interval. `docs/PHASE1-RESULTS.md` M3.1 has the table.

---

## 2026-09-14, question 10: the shotGroups fixtures M3 is gated on are not in the repository, and R cannot run here

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 18 section 1.

**What the documents say exists.**

- `DESIGN.md` line 520, Phase 2: "The shotGroups fixtures are already generated, by `tools/shotgroups/sg_dump.R`."
- `docs/PHASE1-BRIEF.md` section 5: "Generated fixtures under `test/fixtures/shotgroups/` with a README stating provenance, package version, R version and licence".
- `docs/STATISTICS.md` section 15.1: "The fixtures are **generated output**, checked in for reproducibility so that contributors need no R installation to run the test suite."

**What the repository has.**

- **Only the driver:** `tools/shotgroups/sg_dump.R`, added in `b53c606`.
- **No output:** no `shotGroups_*.json` or `.csv` anywhere in the tree, and no `test/fixtures/`.
- **No R:** `where Rscript` finds nothing.
- **Why I don't install it:** entry 2 says "Nothing needs installing on this machine".

**What the gate needs that `sg_dump.R` does not write, even once run.** This is for `docs/STATISTICS.md` section 15.5, items 1, 2, 3 and 5.

1. **The inputs.**
   - **What the dump holds:** shotGroups' outputs keyed `<function>.<component>.<row>.<col>`, and no coordinates.
   - **What GroupLab needs:** each dataset's shots, to compute its own side. That is `point.x`, `point.y`, `distance`, the units, and both `group` and `series`, since section 15.4 item 8 says the savage frames key on `series`.
   - **Licence:** these are shotGroups' data. Section 15.1's arrangement, generated fixtures with provenance in the test tree, is the one I would apply to them, but that is yours to confirm.
2. **Groups.** The script passes the whole data frame to `getXYmat` and to every function. So `DFcciHV`, `DF300BLKhl`, `DFlandy04`, `DFlandy01` and `DFsavage` each give one pooled result, not per-group results.
3. **`compareGroups`.** It is never called. The two-group and multi-group branches of section 15.2 have no reference, and the `coin` branch, section 15.4 item 6, is unrecorded.
4. **The Monte Carlo reference.** Item 5 checks GroupLab's tables against `DFdistr` "to **within 0.2 percent on the mean and 0.5 percent on the 2.5 and 97.5 percent quantiles**, for `n` from 2 to 50 and `nGroups` from 1 to 10". Nothing exports those cells.
5. **The eight datasets of section 15.2.** Each is one invocation.

**Options.**

- **A. The planning session extends the dump, runs it with R 4.3.3 and shotGroups 0.8.4, and commits the output.** This is how question 2's libapriltag corners came back.
  - **What it holds:** the inputs, per-group outputs, `compareGroups` with the `coin` branch recorded, and the `DFdistr` cells, with section 15.1's README.
  - **Where it lands:** the test project is `tests/GroupLab.Core.Tests/`, so I'd put it in `tests/GroupLab.Core.Tests/Fixtures/shotgroups/`, unless you want the brief's `test/fixtures/shotgroups/`.
  - **Cost:** one R session.
- **B. Allow R and shotGroups to be installed on this machine.**
  - **Cost:** it reverses entry 2 for a toolchain that is not part of the build.
  - **A second cost:** I would then write the fixture extension against an R API I cannot check against its documentation here.
- **C. Build M3 without the comparisons, and read them in Phase 2.**
  - **What needs nothing:**
    - sections 3.4, 9.1, 9.2, 10 and 12.5 publish exact values that depend only on `n` and constants;
    - items 4 and 6 are synthetic truth;
    - the Monte Carlo tables can be generated.
  - **Cost:** gate items 1, 2, 3 and 5 go unread. `DF300BLK`'s values quoted in sections 3.3 and 4 cannot be reproduced without its 20 shots.

**What I would choose: A, with C's build going ahead while A is run.** The brief and the specification authorise the build; only the gate reading needs the files. If you agree, say so in the answer and I will start the build without waiting for them.

---

## 2026-09-14, question 9: gate 2's 0.01 in hole-centre tolerance sits at the noise floor measured on real paper

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 18 section 3.

`docs/PHASE1-BRIEF.md` section 4.4:
- **Item 2:** "render-and-difference recovers at least 99 percent of holes with no false positives at a hole-centre tolerance of 0.01 in".
- **Item 3:** "The 0.008 in centroid noise floor measured on real paper is the number it will eventually be judged against, and a synthetic figure far below it means the synthesis is too clean."

**The measurement.** From `docs/PHASE1-RESULTS.md` M2.2, held-out seeds 1001 to 1003, run once, three sheets per row:

| DPI | Holes per bull | Holes | Within 0.15 in | Within 0.01 in | Strays | Centre error median / 95th pct (in) |
|---|---|---|---|---|---|---|
| 600 | 1 | 84 | 100.0% | 76.2% | 1 | 0.0063 / 0.0192 |
| 600 | 2 | 168 | 94.0% | 61.9% | 2 | 0.0074 / 0.0375 |
| 300 | 1 | 84 | 100.0% | 82.1% | 0 | 0.0067 / 0.0148 |
| 300 | 2 | 168 | 96.4% | 56.5% | 0 | 0.0082 / 0.0328 |

**Why the tolerance decides it, not the detector.**

- **Where 0.01 in falls:** the median centre error is the paper floor item 3 names, which puts 0.01 in near the 75th percentile of the error.
- **What 99 percent would take:** the 99th percentile of the error under 0.01 in. That is a centre error well below the floor, which item 3 says means a synthesis too clean.
- **Why a better detector can't fix it:** the synthetic truth centre is the point a lobed rim was drawn around. A centroid of a lobed star reaches that point only as closely as the lobes allow.

**Options.**

- **A. Keep 0.01 in.** The gate then fails on any synthesis that passes item 3's realism test.
- **B. Gate recall and false positives at a match tolerance, and keep centre accuracy under item 3.**
  - **The tolerance:** 0.15 in, the survey's hit tolerance. Centre accuracy is judged against paper.
  - **How the held-out run reads under B:**
    - one hole per bull meets 99 percent recall at both resolutions, but has 1 stray over three sheets at 600 DPI;
    - two per bull does not meet it, at 94.0 and 96.4 percent.
- **C. Set the tolerance from the floor, for example 0.02 in.** One hole per bull's 95th percentile is 0.019 and 0.015 in, so it would also sit near the line.

**What I would choose: B.** It separates finding a hole from locating it. Locating is then judged against paper, which no synthesis can stand in for.

---

## 2026-09-14, question 8: the sheet contradicts entry 15's reading of the table frames' EXIF, and the joint-fit lens key mixes two pixel geometries

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 16 section 2.

Entry 15 section 1: "Two of the three tags say ultrawide and one says main, so the odd one out is the 35 mm equivalent and it is wrong."

**`docs/PHASE1-RESULTS.md` M1.5 measured the opposite on three of the four table frames.**

- **Fitted alone:** `20260913_130543`, `130550` and `130554` recover 2366, 2353 and 2771 px. That is near the 2556 px their 23 mm tag gives, and far from the 1621 to 1940 px that `ultrawide1-3` recover.
- **Fitted jointly:** forced into the ultrawide's shared 1659 px, they degenerate, keeping 8, 4 and 0 of 136 corners.
- **The fourth frame:** `130559` recovers 906 px on a nearly frontal flat sheet, which constrains its focal length least.
- **Not the start:** `ultrawide1-3` give the same figures to five digits whether they start at 1444 or 2556 px.

**One explanation fits all three tags being true.** The phone took the close-up table frames on the ultrawide sensor and cropped them to the main camera's field of view. The physical focal length and f-number then describe the lens, and the 35 mm equivalent describes the pixels. A joint fit keyed on focal length and f-number (entry 6) then puts two pixel geometries into one fit, and their normalised distortion differs too. That explanation is not established. The mixing is: the shared 2.2 mm lens, k1 -0.031 and k2 +0.022, includes the four ungated table frames.

**Options.**

- **A. Key joint fits on focal length, f-number and 35 mm equivalent together.**
  - The table frames become their own group, and `ultrawide1-3` get a lens of their own.
  - It is one grouping key in `SurfaceFrames.FitByLens`.
  - It changes the M1.5 ultrawide figures through the shared lens, and they would be re-run and reported.
- **B. Keep entry 6's key, and leave out of joint fits any frame whose tags disagree with the rest of its group.** The effect on these frames is the same, but a frame with a wrong tag is lost instead of grouped.
- **C. Keep the key as it is.** The ultrawide's shared lens stays contaminated by four ungated frames.

**What I would choose: A.** A joint fit shares pixel geometry, and the 35 mm equivalent is the tag that describes it. Separately, for the protocol: record which camera took each frame, and shoot from far enough that the phone keeps that camera.

---

## 2026-09-14, question 7: three of the five marker module sweep sheets for next weekend now fail test 26f as an error

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 16 section 3.

**How the sweep came to fail.**

1. **Built on the Phase 0 sheet.** `docs/PHASE1-RESULTS.md` M0 built the sweep on `GL-CF25-LTR` as printed for Phase 0, gap 456. So its 0.5 mm sheet is the Phase 0 sheet by identifier, `GL-YCSK-DZZ1-R0VJ-4T5Y`, and the sweep carries its own control.
2. **The geometry commit moved the live sheet.** Entry 13's geometry commit, `3dcc814` (was `d73b9a4` before the 2026-09-14 rewrite), made test 26f an error and moved the live sheet to gap 454.
3. **Rebased on the frozen definition.** The sweep's base is now `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, the first argument of `grouplab sweep module`. It reproduces all five definitions and PDFs byte for byte.
4. **Three sheets fail.** The validator now reports 26f errors on the 0.5, 0.6 and 0.8 mm sheets: `GL-YCSK-DZZ1-R0VJ-4T5Y`, `GL-683J-3ZR8-60D5-0FGG` and `GL-SEBE-5F06-GVTF-3CZK`. Their sighters sit outside the marker lattice, and the sweep renders them only by accepting an invalid definition. The 0.3 and 0.4 mm sheets carry an extra marker row and conform.

**Options.**

- **A. Print the five as they are.**
  - The sweep measures the module floor, which the sighters do not bear on, and the 0.5 mm sheet stays the Phase 0 sheet.
  - Cost: three printed sheets the validator rejects, which the sweep README and the M0 section would have to call measurement sheets, not library sheets.
- **B. Rebase the sweep on the live `GL-CF25-LTR`, gap 454.**
  - Five sheets that conform, with five new identifiers, and the M0 table and PDFs regenerated. It is a few minutes of work.
  - The 0.5 mm sheet is no longer the Phase 0 sheet, so the control sits one 2 dmm sighter change away from what Phase 0 printed.

**What I would choose: A.** For a module-floor measurement, a control identical by identifier is worth more than sighters that conform when the measurement never uses them. B is right if nothing printed should fail the validator.

---

## 2026-09-14, question 6: a sighter gap cannot make three of the four sheets conform, so test 26f cannot become an error in the geometry commit as written

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 13.

Notes entry 11 asks for one commit that sets `GL-CF25-LTR` to a sighter gap of 454, `GL-LR300-R24` and `GL-LR300-R36` to 1142, `GL-CF25-100M-A4` "per the sweep", declares `cells.sighterGap` on each, and promotes test 26f to an error. Before changing anything I ran those changes through `tools/layout/layout.py`, unmodified. **Only `GL-CF25-LTR` conforms afterwards.** The other three keep bull columns outside the lattice horizontally, which is question 4's finding 1, and no sighter gap reaches a column. Promoting 26f to an error in that commit fails three built-in sheets.

**The measurement.** Margins are how far the outermost marker centre lies beyond the outermost bull centre on each side, in dmm; negative is outside, and zero conforms under the inclusive rule. Every row places with zero layout errors and zero warnings.

| Sheet | Change | Scoring bulls | Markers | Left / right / top / bottom | 26f |
|---|---|---|---|---|---|
| `GL-CF25-LTR` | as printed, gap 456 | 25 | 34 | +190 / +190 / +190 / **-266** | fails |
| `GL-CF25-LTR` | **gap 454, entry 11** | 25 | 38 | +190 / +190 / +190 / +190 | **conforms** |
| `GL-CF25-100M-A4` | as printed, gap 480 | 25 | 32 | **-200 / -200** / +200 / +200 | fails |
| `GL-CF25-100M-A4` | any gap from 480 down to 301 | 25 | | columns unchanged | **fails** |
| `GL-LR300-R24` | as printed, gap 1219 | 30 | 35 | **-508 / -508** / +508 / **-508** | fails |
| `GL-LR300-R24` | **gap 1142, entry 11** | 30 | 40 | **-508 / -508** / +508 / +508 | **fails** |
| `GL-LR300-R36` | as printed, gap 1219 | 36 | 48 | **-508 / -508** / +508 / **-508** | fails |
| `GL-LR300-R36` | **gap 1142, entry 11** | 36 | 56 | **-508 / -508** / +508 / +508 | **fails** |

**Why no gap reaches the columns.** `layout.py` places the lattice columns half a pitch outside the outermost bull columns and drops any marker whose box crosses half the safe margin. On `GL-CF25-100M-A4`, five columns at a 400 dmm pitch on a 2100 dmm page put the outer marker columns at x = 50 and 2050, boxes 20 to 80 against a limit of 60, so both columns go. The rolls are the same at a 1016 dmm pitch.

**Two changes do make them conform**, measured the same way:

| Sheet | Change | Scoring bulls | Markers | Left / right / top / bottom |
|---|---|---|---|---|
| `GL-CF25-100M-A4` | `grid-boundary-half-1`, gap 480 | 25 | 88 | 0 / 0 / +200 / +200 |
| `GL-LR300-R24` | `grid-boundary-half-1`, gap 1142 | 30 | 113 | 0 / 0 / +508 / +508 |
| `GL-LR300-R36` | `grid-boundary-half-1`, gap 1142 | 36 | 151 | 0 / 0 / +508 / +508 |
| `GL-CF25-100M-A4` | 4 columns | **20** | 36 | +200 all round |
| `GL-LR300-R24` | 5 columns | **25** | 48 | +508 all round |
| `GL-LR300-R36` | 8 columns | **32** | 63 | +508 all round |

**Options, with their costs.**

- **A. The half lattice on the three sheets, in the one geometry commit.** Every bull brackets, no scoring bull is lost, and the scheme already ships on the 300 yard tiles, which conform at the same margin of zero. It takes the marker count to 2.75, 3.2 and 3.1 times what it was, which is more ink near the bulls and more identifiers, though R36's 151 is well inside the 587 the family holds. The outer columns become interpolated along the lattice edge rather than surrounded, which is the tiles' condition.
- **B. Drop a column.** Every bull brackets with margin to spare, but `GL-CF25-100M-A4` falls to 20 scoring bulls, below the library's 25, and the rolls lose 5 and 4 bulls. `TARGET-LIBRARY.md` section 1 would need a third documented exception.
- **C. Land `GL-CF25-LTR` now, keep 26f a warning, and design the columns separately.** Cheapest today, but it is two geometry commits where entry 9 and entry 11 ask for one, and the rolls' identifiers would change twice if their fix changes the scheme.

**What I would choose:** A, because it is the only option that makes every sheet conform without losing a bull or a library requirement, and it uses a scheme the library already ships.

**A second, smaller conflict with "does not change the schema".** The worked example of TARGET-SCHEMA.md section 4 is `GL-CF25-LTR` as printed: identifier `GL-YCSK-DZZ1-R0VJ-4T5Y`, `sighterGap` 456, and a paragraph explaining the 456. `ReferenceEncoderParityTests.Section4DocumentEncodesToTheReferenceSheetBytes` asserts that it encodes to `tools/gltd/check.py`'s bytes for the live `GL-CF25-LTR`, and four test files pin the identifier. Once the live sheet moves to 454 that test fails. Either section 4 changes, with a new identifier, or it stays as the as-printed reference and the test compares it with the frozen definition, `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, which is byte-identical to it. **I would keep section 4 as it is** and re-point the one test, so the schema text is untouched, as entry 11 intends.

**Two small corrections to entry 11, for the record.** `cells.sighterGap` is specified in TARGET-SCHEMA.md sections 3.6 and 7, not 3.10. `docs/TARGET-LIBRARY.md` lists no identifiers; what the commit changes there is the marker counts in its sheet table, 34 to 38 for `GL-CF25-LTR` and the counts of whichever option is chosen for the other three.

**What landed while this is open.** The frozen-fixture part of entry 11 does not depend on the answer, so it is committed: the three definitions the sample set was printed from are in `targets/frozen/phase0/` with a README, `grouplab spike` and the paper gate test resolve against them, a test loads, identifies and validates each, and rerunning every spike command against them reproduces every measured value in `scans/phase0/measurements/`; only the detection times in `threshold.json` differ, as they do between any two runs. Nothing in `tools/layout`, `targets/*.gltd.json` or the validator's severities has changed. Entry 11 stays open until this is answered.

---

## 2026-09-13, question 5: the wall photographs are not of a flat sheet

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 8.

Notes entry 6 describes the nine new photographs as sheet 3 "taped flat to a wall", and sets them three questions: lens, flatness, and sighter geometry. In every frame the sheet hangs from a single pin at the top centre and its edges are visibly curved (`ultrawide1.jpg` shows the fixing at the top edge, `telephoto2.jpg` an orange pin); nothing holds the lower half. The measurement agrees, and as a result the set cannot answer any of the three questions. `docs/PHASE0-RESULTS.md` section 3a has the full table; `scans/phase0/measurements/photos.json` has every corner and bull.

**The measurement, shipped pipeline, inches.**

| Set | Frames | Corners the fit keeps within 0.01 in | Corner RMS over all corners | Worst bull | Scoring bulls over the gate |
|---|---|---|---|---|---|
| Sheet 1 on a table, ultrawide | 4 | 135 to 136 of 136 | 0.0025 to 0.0038 | 0.0057 to 0.0108 | 0 to 3 of 25 |
| Sheet 3 on a wall, ultrawide | 3 | 42 to 66 of 128 to 136 | 0.023 to 0.060 | 0.070 to 0.091 | 13 to 21 of 25 |
| Sheet 3 on a wall, main | 3 | 25 to 90 of 104 to 136 | 0.014 to 0.057 | 0.048 to 0.114 | 8 to 21 of 25 |
| Sheet 3 on a wall, telephoto | 1 (two excluded) | 38 of 132 | 0.031 | 0.096 | 21 of 25 |

The misfit is largest at the free bottom edge: the two lowest marker rows sit 0.026 to 0.138 in from the fit on every wall frame, against 0.0025 to 0.0033 in on the table frames. `telephoto2`, on the longest lens, needs 0.020 in RMS from a homography alone.

**What that does to entry 6's three questions.** Lens: every lens fails by the size of the surface misfit, so a lens effect of 0.004 to 0.011 in cannot be seen; focal length does not rescue a curved sheet, which is all the set shows. Flatness: the table frames are the flatter set, 6 to 16 times better by worst bull with the same lens, so the paired comparison is inverted. Sighters: scoring bulls fail on every frame, so the sighters cannot be isolated.

**Options, with their costs.**

- **A. Reshoot sheet 3 held flat**, taped along all four edges or laid under a sheet of glass, main camera, one square-on and one about 20 degrees off-axis, whole sheet in frame. About two minutes, no new paper. It answers entry 6's questions as intended, and the pipeline, raw-row output and grouped table are already in place, so the report follows the same day.
- **B. Close Phase 0 on the evidence as it stands.** The paper gate passes, the photograph gate fails, and what is known is that the table frames fail on the sighters and by a thousandth or two on the scoring bulls, and that a hanging sheet fails by a tenth. The photograph path moves to Phase 1 with the flatness and sighter questions open. Costs nothing now; Phase 1 then starts without knowing whether a flat photograph passes.
- **C. Register a curved sheet**, by local or piecewise registration from nearby markers. It would make hanging sheets usable, which matters because users will pin targets up this way, but it goes beyond "markers, homography and bull location" in the brief, and it cannot help the sighters, which no nearby marker brackets.

**What I would choose:** A, because it is the experiment entry 6 intended and costs two minutes. C is worth recording as a Phase 1 requirement whatever A shows, because a pinned sheet is the normal case at a range. The spike report carries the photograph gate as a failure on all eleven usable frames.

---

## 2026-09-13, question 4: the bracketing rule, proposed, and three sheets a sighter gap cannot fix

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 9.

Notes entry 5 asks for the wording of a TARGET-SCHEMA.md section 7 rule and a conformance test, from the finding that the sighters of `GL-CF25-LTR` sit outside the marker lattice. The geometry change is deferred, as entry 5 now says, and nothing in `tools/layout`, `targets/` or the validator has been changed. Measuring the rule against the whole library before proposing it turned up two things it has to decide.

**Every sheet, measured on the geometry that was printed.** How far a marker centre lies beyond the outermost bull centre on each side, in dmm; negative means a bull lies outside the lattice on that side. The last column is entry 5's sighter-only criterion, reproduced by sweeping `sighterGap` down from 1.2 times the pitch in `tools/layout/layout.py`.

| Sheet | Left | Right | Top | Bottom | Smallest gap with a marker row below the sighters |
|---|---|---|---|---|---|
| GL-CF25-LTR | +190 | +190 | +190 | **-190** | **454**, 38 markers |
| GL-CF25-LTR-D | +190 | +190 | +190 | +190 | no sighters |
| GL-CF25-A4 | +190 | +190 | +190 | +190 | 456, unchanged |
| GL-CF25-100M-A4 | **-200** | **-200** | +200 | +200 | 480, unchanged |
| GL-CF30-LTR | +175 | +175 | +175 | +175 | no sighters |
| GL-RF25-LTR, GL-RF25-A4 | +127 | +127 | +127 | +127 | 304, unchanged |
| GL-RF36-LTR | +127 | +127 | +127 | +127 | 304, unchanged |
| GL-LR25-TAB, GL-LR25-A3 | +254 | +254 | +254 | +254 | 609, unchanged |
| GL-LR30-TAB | +254 | +254 | +254 | +254 | 609, unchanged |
| GL-LR300-T | **0** | **0** | **0** | **0** | no sighters |
| GL-LR300-TA4 | **0** | **0** | +508 | **0** | no sighters |
| GL-LR300-R24 | **-508** | **-508** | +508 | **-508** | **1142**, 40 markers |
| GL-LR300-R36 | **-508** | **-508** | +508 | **-508** | **1142**, 56 markers |
| GL-LR300-R42 | +508 | +508 | +508 | +508 | 1219, unchanged |

The GL-CF25-LTR bottom figure is before the fix; at 454 it becomes +190. Entry 5's table is reproduced exactly.

**Finding 1: a sighter gap cannot bracket three sheets.** On `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` the outermost bull columns sit outside the lattice horizontally, and on R24 and R36 that stays true at a gap of 1142. The mechanism is the one entry 5 found for the row: the lattice column beyond the outermost bulls is dropped by the half-safe-margin edge test. On `GL-CF25-100M-A4` the columns would be at x = 50 and 2050, whose boxes reach 20 and 2080 against limits of 60 and 2040. A rule worded "the lattice must bracket every bull" makes these three sheets non-conforming, and the fix is not a declared sighter gap. It would move the bull grid or the page, which is a geometry decision.

**Finding 2: the tiles sit exactly on the edge.** On `GL-LR300-T` and `GL-LR300-TA4` the outermost markers share coordinates with the outermost bulls, a margin of 0. They are bracketed if the rule is inclusive and not if it is strict.

**Finding 3: GLTD-B does not carry `sighterGap`.** Section 5 has no field for it, so a definition decoded from a sheet's codes loses the declaration and raises test 23's warning on every decode of the three changed sheets, which section 3.6 says the field exists to prevent. Either the body gains the field, or test 23 is scoped to documents that were not decoded, or a decoded sighter gap that brackets is exempt.

**Proposed wording, for section 7**, after the sighter-gap rule:

> **The fiducial lattice must bracket every bull.** Every bull centre, sighters included, must lie on or inside the rectangle bounded by the outermost surviving marker centres. A bull outside it is interpolated on a flat scan and extrapolated on anything that is not flat, and the Phase 0 photographs measured the cost: the sighters of GL-CF25-LTR, one dropped marker row outside the lattice, were the worst bull on three photographs of four. Where a derived scheme leaves the sighter row outside, a generator shortens the sighter gap from 1.2 times the pitch, one dmm at a time, until the lattice brackets, and declares `cells.sighterGap`.

**Proposed conformance test**, numbered to sit with the other layout tests:

> 26f. A bull centre outside the rectangle bounded by the outermost surviving marker centres is an error.

**Options for the two decisions.**

- **Inclusive or strict.** Inclusive, as worded above, keeps the tiles conforming; a bull on the lattice's edge is interpolated along that edge. Strict would also fail both tiles, whose only fix is a denser scheme. **I would choose inclusive.**
- **Error or warning, given finding 1.** As an error, three more sheets need a geometry change before the rule can land. As a warning first, it can land with the sighter fix and name the three. **I would choose a warning until the three sheets are fixed, then an error**, so the rule is not blocked on geometry nobody has designed yet.

---

## 2026-09-13, question 3: PHASE0-PRELIM's split of the paper error belongs to its centroid

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 4.

`docs/PHASE0-PRELIM.md` section 3: "**Not the measurement method.** The result is unchanged across three window radii, and a symmetric estimator applied to a symmetric object cannot manufacture a spatially structured field." Section 5a: "roughly 0.0019 inches of it is systematic and reproducible across sheets, and roughly 0.0010 inches is random from sheet to sheet." DESIGN.md section 21 repeats both figures.

The spike reproduces the preliminary centroid and measures a second locator against it on the same registration. The document is yours, so this is raised rather than edited.

**The measurement**, sheets 1 to 3 of `GL-CF25-LTR` at 600 DPI, the same homography for both locators, inches:

| Locator | Single sheet mean / worst | Systematic mean / worst | Random RMS | Systematic after a quadratic over the page, mean / worst |
|---|---|---|---|---|
| Thresholded centroid, the preliminary method | 0.00217 / 0.00431 | 0.00201 / 0.00374 | 0.00103 | 0.00147 / 0.00314 |
| Edge fit to the declared disc radii, shipped | 0.00132 / 0.00316 | 0.00128 / 0.00279 | 0.00047 | 0.00039 / 0.00071 |

The edge fit was chosen on the synthetic raster before it saw paper, as `docs/PHASE0-SPIKE-BRIEF.md` section 5 requires: 0.00022 in worst at 300 DPI and 0.00013 at 600, against the centroid's 0.00069 and 0.00025.

**What that does to the written account.**

1. **The method was part of the limit.** Stability across mask radii of 90, 100 and 110 dmm could not show otherwise, because all three masks hold the same ink: the inner ring and the dot. The edge fit also uses the outer ring and locates edges at the midpoint of their own local ink and paper levels, so ink density does not move it.
2. **The split is about 0.0013 in systematic and 0.0005 in random**, not 0.0019 and 0.0010.
3. **Most of the systematic part is smooth.** A quadratic over the page leaves 0.0004 in mean and 0.0007 worst. The prize a printer calibration could claim, section 6 of the brief's last item, is most of the systematic field rather than a fraction of it.
4. **The field is still paper-fixed.** The rotated rescan correlates with sheet 2 at +0.63 under the edge fit, against -0.11 for the scanner-fixed prediction, so section 5a's conclusion stands. The gate conclusion stands too: five thousandths holds with more margin.

**Options.** A: amend `docs/PHASE0-PRELIM.md` and the DESIGN.md section 21 figures with a dated note citing the spike, leaving the original text. B: leave both as a record of what the scratch measurement showed, and let `docs/PHASE0-RESULTS.md` carry the corrected figures. **I would choose A**, because DESIGN.md is what the next phase reads, and it currently states a random component twice the measured one.

---

## 2026-09-13, question 2: libapriltag's corners, to re-rank the detectors with the Phase 0 locator

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 3.

Notes entry 2 asks for measurement 8 of `docs/FIDUCIAL-DECISION.md` section 10 to be re-run with the Phase 0 bull locator and the ranking reported. Only the OpenCV half can be re-run here: nothing was installed, as entry 2 instructs, so libapriltag does not run on this machine.

**What would settle it.** For each of `gl-cf25-ltr-1-600-dpi.png`, `gl-cf25-ltr-2-600-dpi.png`, `gl-cf25-ltr-3-600-dpi.png`, `gl-cf25-ltr-2-600-dpi-rot180.png` and `gl-cf25-ltr-1-300-dpi.png`, the libapriltag detections from the same run as entry 2, committed as `scans/phase0/apriltag-corners.json`:

```
{ "<image file>": [ { "id": 0, "corners": [[x, y], [x, y], [x, y], [x, y]] }, ... ], ... }
```

Corners as `pupil-apriltags` returns them, in its own winding and pixel convention. The harness applies the conversion entry 2 records (winding reversed, no rotation) and fits each detector's corners through the same registration and the same shipped edge-fit locator. No decision is needed, only the file.

---

## 2026-09-13, question 1: the photograph gate fails on all four photographs, and the lens is not the main suspect

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 5.

`docs/PHASE0-SPIKE-BRIEF.md` section 7: "If the photograph gate fails, **the lens is the first suspect, not the code.** Two frames from the main camera settle it in about a minute". It fails, it was diagnosed before anything else, and the diagnosis points somewhere else, so the choice of what to do next is yours.

**The result, shipped pipeline** (homography with radial distortion, edge-fit locator), inches:

| Photograph | Markers | Corner residual RMS | Homography alone RMS | Distortion at the frame edge | Bull mean / worst | Worst bull |
|---|---|---|---|---|---|---|
| `20260913_130543.jpg` | 33/34 | 0.00319 | 0.00343 | 0.992 in | 0.00229 / 0.00530 | 15 |
| `20260913_130550.jpg` | 32/34 | 0.00329 | 0.00390 | 0.344 in | 0.00262 / 0.00973 | S3 |
| `20260913_130554.jpg` | 34/34 | 0.00370 | 0.00387 | 0.370 in | 0.00309 / 0.01083 | S3 |
| `20260913_130559.jpg` | 34/34 | 0.00248 | 0.00297 | 0.171 in | 0.00198 / 0.00922 | S3 |

**What it is not.**

- **Not the printer.** The photographs' bull fields correlate with sheet 1's own 600 DPI scan field at -0.07 to +0.33, where scans of different sheets correlate at +0.85 to +0.94. The error is added by the photograph path.
- **Not a lens term the model misses, nor any smooth global warp.** Refitting with three radial coefficients and a free distortion centre, or a quadratic or cubic warp on top of the homography, evaluated leave one marker out, improves the corner residual on no photograph consistently and brings no photograph inside the gate: the best worst bull, whichever model gives it, is 0.0050, 0.0062, 0.0063 and 0.0076 in.

**What it is, as far as the data goes.**

1. **The sighters are extrapolated.** In `targets/GL-CF25-LTR.gltd.json` the lowest marker row is at y = 8.854 in and the sighter row at y = 9.902 in. The three sighters are the only bulls on the sheet outside the marker lattice, by 1.05 in, and they are the worst bull on three of the four photographs. On a flat scan this costs little (S3 is also the worst bull on sheets 2 and 3, at 0.0032 and 0.0029 in); on a photograph it amplifies whatever the planar model gets wrong.
2. **The sheet is not one plane.** Registering each bull from only its nearest 6 or 8 markers, instead of the whole sheet, brings the worst scoring bull from 0.0053 to 0.0035 in, 0.0066 to 0.0029, and 0.0034 to 0.0021 on three photographs, and leaves the fourth at 0.0057. The sighters stay at 0.008 to 0.013 in whatever the markers, because every choice still extrapolates to them. In all four frames the sheet lies on a table rather than pinned to a wall as `docs/PHASE0-PRINT-PROTOCOL.md` section 7 asks, and a free sheet of paper does not lie flat.
3. **The lens reading is uncertain.** EXIF says f/2.2 and 2.2 mm, which reads as the ultra-wide, but also a 35 mm equivalent of 23 mm, which is normally the main camera. The fitted distortion at the outermost marker is 0.004 to 0.011 in, so the lens term is real but it is being fitted.

**Options, with their costs.**

- **A. Two frames with the main camera of sheet 3, the control, taped flat to a wall**, one square-on and one off-axis, per protocol section 7. About a minute, no new paper. It separates flatness and lens from the sighter geometry: if the scoring bulls then pass and only the sighters fail, the geometry is the finding.
- **B. Put markers below the sighter row.** This is a change to the `grid-boundary-1` placement in `tools/layout`, which the brief says not to touch, and it changes the derived marker lattice of every sheet with a sighter band. Expensive, and it should wait for A.
- **C. Gate photographs on scoring bulls only.** A redefinition of the gate. Not recommended without A; with A it may still be the wrong answer, since a sighter is still a bull a shooter fires at.

**What I would choose:** A first, then B or C with its result. The spike report carries the photograph gate as a failure, not as a pass on a narrower definition.
