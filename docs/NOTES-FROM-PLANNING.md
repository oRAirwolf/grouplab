# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## 2026-09-15, entry 26: the rotate control is a requirement, not a fallback, and it has one trap

**Status: actioned 2026-09-15.** Amends entry 24 section 4. Rotation is a view property of the marking state in the stored pixel frame, undoable and recorded in `grouplab-marking-2`, with the section's test in `tests/GroupLab.App.Tests/MarkingScreenTests.cs`; reported in `docs/PHASE1-RESULTS.md` M4.2.

Entry 24 identified the cause of Alan's sideways image correctly, an ignored EXIF Orientation tag, and then drew the wrong conclusion from it: that honouring the tag was the fix and a manual control was a secondary convenience. **Alan pushed back and he is right.** Build both, and treat the control as a first-class feature rather than a safety net.

### Why the control is required on its own terms

**A flatbed scan carries no orientation tag.** Scans are half this project's input path, a letter sheet goes into a scanner the wrong way round constantly, and there is no metadata to consult. On that input the tag is not merely unreliable, it does not exist.

Three more, any one of which would be enough:

- **A wrong tag is worse than a missing one**, because it rotates confidently in the wrong direction. Editors that rewrite pixels while leaving the tag, or the reverse, produce exactly this.
- **Images that have been through a chat app or a screenshot** usually arrive stripped, which the mounted collection already demonstrates.
- **A correct tag is not always what the user wants.** Someone may simply prefer the sheet a different way up, and that is a preference rather than a correction. Alan's point stands as he made it: people will be annoyed, and being annoyed by software that is technically right is still being annoyed.

So: honour the tag on load, because it makes the common case correct with no interaction, and provide rotate left and rotate right controls that work at any time, on any image, whether or not a tag was present and whether or not it was obeyed.

### The trap, which is the part that matters

**Rotating must never move a mark that is already placed.**

Somebody will load a scan, mark twelve impacts, notice it is sideways, and rotate. If rotation is implemented as anything other than a view transform, those twelve marks land in the wrong places and the user has to start again, which is worse than never offering the control.

So:

1. **Rotation is a property of the view, not of the image.** Never re-encode the pixels and never rewrite the file. The image on disk is the contributor's original and is not ours to modify.
2. **Shot and scale coordinates live in one canonical frame**, independent of what the view is doing. Rotating changes where a mark is drawn and not what it is.
3. **The canonical frame is the stored pixel frame**, which is the simplest thing that can be checked later against the file itself. Entry 24 section 4 already asks for the convention to be recorded in the export; this makes that a hard requirement rather than a nicety, because there are now two ways the displayed frame can differ from the stored one.
4. **Record the display rotation in the export** as well, so reopening a marking file shows the sheet the way the person left it.
5. **Undo covers rotation**, like every other action on that screen.

**A test worth having:** mark several shots, rotate, and assert every shot's page coordinates are unchanged and every drawn position has moved as expected. That is the regression that protects the feature from being quietly broken later.

### Not asking for

A free-angle rotation, and a mirror flip. Ninety degree steps in both directions cover every real case, and a sheet photographed in a mirror is not a case anybody has met. If one turns up, it is one line.

---

## 2026-09-15, entry 25: units, and the fact that the application cannot print a target

**Status: actioned 2026-09-15.** Section 1 in f4f5263, reported in `docs/PHASE1-RESULTS.md` M4.3; section 2 as the print screen, M4.4; adjust to zero still waits, per section 3. Two findings from Alan's second pass. The first is a small fix with a larger shape behind it. The second is a missing pillar rather than a missing button.

**Entry 24 section 7 is answered and needs nothing.** The scale entry does take a value, "Distance between the two taps, inches", and he used 1.5 against his grid. Good. The detection message is also doing its job: "Detection failed: 0 of 38 markers found; registration needs 4. Mark this image by hand with a reference length or rectangle" is the right thing to say to someone who loaded a commercial target, and the 38 confirms the corrected `GL-CF25-LTR` geometry is in the library.

### 1. Units, which is larger than the one input he noticed

He asked for a dropdown beside the scale length for centimetres or inches. He is right, and the same gap runs through everything the application prints: the panel reads `0.914 in` and `Centre from aim: 0.140 in right`, with no way to ask for anything else.

**Do not add a dropdown to one box. Give the application a unit setting and make every value obey it.**

Three axes, because they vary independently:

| Axis | Choices | Where it shows |
|---|---|---|
| **Linear** | inches, cm, mm | Scale entry, group size, mean radius, sigma, centre offset, export |
| **Angular** | MOA, mil, SMOA | Anywhere a figure is quoted at distance |
| **Distance** | yards, metres | The shot distance input, and it drives the angular conversion |

`docs/STATISTICS.md` section 13 is Units and section 12.5 holds the angular constants, so the arithmetic exists. This is exposing it, not building it.

**The rule that keeps this from becoming a bug farm: the unit setting changes display only, never storage.** Keep one canonical representation internally and convert at the edge. A marking file must mean the same thing whoever opens it, and the classic failure here is a saved file that reads differently depending on a setting on the machine that opens it.

**The export carries both.** Canonical values, plus the unit the user was working in, so a reader can reproduce what was on screen without guessing.

**Default from the system locale on first run, then remember the choice.** Alan is in the United States and wants inches, yards and MOA. A metric shooter wants cm, metres and mil, and this project already takes that seriously enough that two of its nine statistical fixtures exist solely to prove the conversion is right. It would be odd to prove it in the test suite and not offer it in the window.

**One detail worth getting right:** mil and MOA are not interchangeable and a turret is marked in one or the other. When adjust-to-zero is built, per entry 21 section 5, it must use the angular unit the user picked rather than a default, because a shooter dialling MOA clicks from a mil number puts the next group in the wrong place.

### 2. The application cannot print a target, and that is where a new user starts

This is the one worth stopping on. GroupLab's whole premise is that you print its target, shoot it, and photograph it. The library holds twenty-two definitions. A person who installs the application and wants to use it properly has to start by printing a sheet, and **there is no way to do that from the window.** Today the answer is a command line, or asking me to have you generate a PDF pack, which is fine for the two of us and absurd for anybody else.

The machinery all exists: `Rendering/`, `PdfWriter.cs`, and the CLI already produces the PDFs. **This is a screen that calls code that works, not new capability.**

**What the screen needs.**

1. **Pick a sheet.** The twenty-two built-ins listed by something a shooter recognises, not by identifier: what it is for, how many bulls, the sheet size, the distance it was designed around. `docs/TARGET-LIBRARY.md` has all of it.
2. **A preview**, so nobody prints eight pages to find out what they chose.
3. **The load block choice**, blank to write on later or filled in before shooting, which Alan asked for during the design and which `docs/TARGET-SCHEMA.md` makes a print-time decision. If it is filled in, the fields come from this screen.
4. **Print, or save a PDF.** Both, because some people want to print elsewhere.
5. **Scale, handled rather than warned about.** This is the part that matters most and the part a warning will not save. Every measurement in this project depends on the sheet being printed at exactly 100 percent, and a printer driver will silently shrink a page to its own margins given the chance. **Drive the print with scaling disabled rather than telling the user to check a box.** Where the platform will not allow that, say so in the dialog in plain words, and put it in the PDF's own margin as printed text so a sheet that came out wrong carries the evidence.
6. **A multi-page set**, since the roll and tiled targets are several pages that assemble, and printing them one at a time by hand is how they end up in the wrong order.

**It is also the natural home for the volunteer kit.** Entry 21 and the paper protocol both want a set of sheets plus a short instruction sheet in other people's hands. A print screen that can emit a pack is that, without a second mechanism.

**Where it sits in the plan.** Not urgent this week: Alan prints from the command line for the weekend and that is already arranged. But **it belongs before anyone outside this project is asked to use the application**, because it is the first thing they would do and the first thing they would fail at. Put it in `docs/PHASE1-RESULTS.md` or wherever the M4 scope lives as a named gap rather than leaving it implied by its absence.

### 3. Order

Entry 24 section 1, the statistics reported for two shots, still comes first. It is the one that can mislead somebody. Then the rest of entry 24. Then units, then printing.

**Do not build entry 21's adjust-to-zero until units land**, because it is the one feature where getting the unit wrong sends a shooter's next group somewhere else rather than merely displaying an odd number.

---

## 2026-09-15, entry 24: first human use of M4, and the app printed confident statistics for two shots

**Status: actioned 2026-09-15.** Sections 1 to 3 in b9b117b, with question 12 for the threshold; sections 4 and 7 with entry 26, and section 5; reported in `docs/PHASE1-RESULTS.md` M4.2.

Alan opened the marking screen, loaded `scans/mounted/20260329_183028.jpg`, set a scale, marked a point of aim and two impacts, and exported. His own notes were rotation, no calibre input, and that it needs refinement. The export and the screenshots carry four more findings he did not flag, and the first one is the serious one.

### 1. Two shots, and the panel reported a mean radius to three decimals

From his export:

```
shots: 2
meanRadius 0.914 in   (95% 0.476 to 5.744)
sigma      0.729 in   (95% 0.380 to 4.583)
```

**The interval spans a factor of twelve and the headline is printed as 0.914.** Nothing on screen says the number is meaningless. He read the panel, saw a figure with a confidence interval beside it, and reported that the app seemed to be working.

That is the failure this project exists to prevent, appearing on first contact. `docs/STATISTICS.md` section 6 already flags groups under ten shots as unreliable and the interface does not honour it. Entry 23 section 2 has the measured coverage: 79.5 percent at ten shots against a nominal 95, which means even ten is optimistic, and two is not a sample at all.

**What the panel must do.**

- **Below the useful minimum, do not print a headline figure.** Say what is missing, in the shooter's terms: something like "2 shots. At least 5 are needed for a group size worth quoting, and 10 before the interval means much." Show the shot positions and the centre offset, which are exact and useful at any count, and withhold the dispersion statistics rather than dressing them up.
- **Between that minimum and about twenty, print the figure with its coverage stated**, not a bare 95 percent. An interval whose real coverage is 80 percent must not be labelled 95.
- **Pick the thresholds from `STATISTICS.md` section 9**, which already models how well sigma is known from n shots, rather than from anybody's taste. If section 9 does not give a clean answer, raise it as a question rather than choosing a round number.

**This is the same finding as entry 23 section 2 arriving from the other direction.** That one was a coverage table in a report nobody runs. This one is a number on the screen that a user believed. Fix them together, and treat the interface as where it matters.

### 2. The statistics panel is clipping its own text

The screenshot shows `0.914 in  (95% 0.476 to 5` with the rest cut off at the panel edge. The headline is the one line guaranteed to overflow because it is the largest type. Let it wrap, or size the panel to its content.

### 3. `NaN` is being exported, as a quoted string

```
"aspectRatio": "NaN",
"angleDegrees": "NaN",
```

Those are degenerate at two shots, which is correct, but `"NaN"` is not a value. A consumer reading this file gets a string where it expects a number, and the quoting only exists because bare `NaN` is not legal JSON, which is the language telling you the same thing.

Emit `null` and add a sibling field saying why, for instance `"aspectRatioUnavailable": "needs at least 3 shots"`. Apply it to every statistic that can be undefined, and check the schema for others with the same problem before Alan meets one.

### 4. The rotation is an ignored EXIF tag, not a missing button

`20260329_183028.jpg` stores 4000 by 3000 pixels and carries **EXIF Orientation 6**, which means the pixels are landscape and a viewer is expected to rotate them 90 degrees clockwise. Every normal viewer does. The app does not, which is why it looked turned on its side.

**Honour the orientation tag on load.** That fixes it for nearly every phone photograph at once, rather than asking the user to correct each one by hand. Then add a manual rotate control as well, because some images carry no tag and a few carry a wrong one.

**One thing to get right while doing it.** The export records shot positions as `image.x` and `image.y` in the stored pixel frame. Once the app rotates on load, that frame changes, and every marking file saved before the change silently points at the wrong place. **Record the convention in the file**, and either migrate old files or refuse to load one whose convention is unknown. There are only a handful in existence today, which makes this the cheapest moment it will ever be to fix.

### 5. Calibre: what it is for, since Alan has now asked twice

It is entry 21 section 5 and I said I would specify it once he had used the screen. He has.

**Make it an optional property of the group**, entered once beside the shot distance, not per shot. Free text with a short pick list of common ones, because somebody will want a wildcat.

**Three things it does, in order of value.**

1. **Report extreme spread both ways.** GroupLab measures centre to centre, and shooters at a range measure outside edge to outside edge and subtract one bullet diameter to get the same thing. With the calibre known, print both and label them, so the number matches whatever the person is used to quoting. Without it, print centre to centre and say so. This is the whole reason Ballistic X asks for it.
2. **Size the tap snap radius.** Marking a hole is a tap that snaps to the hole under it. How far it should look is a function of hole size, and hole size tracks bullet diameter, which `docs/SCAN-MEASUREMENTS.md` section 3.5 measured across 343 holes. A .22 and a .338 should not use the same search radius.
3. **Flag a marked hole whose apparent size is wrong for the calibre**, once a scale is set. Two overlapping holes marked as one read as far too large, which is exactly the failure `PHASE1-RESULTS.md` M2.2 found reported silently 50 times out of 52. This is the cheapest available detector for it and it needs no new measurement.

**Do not gate anything on it.** No calibre means no edge-to-edge figure and a default snap radius, never a refusal to work.

### 6. What Alan saw that was right

Worth recording, because it is the part not to change. The scale honesty line appears and reads well: "From a single 1 in reference length, which assumes the photograph is square on and the sheet flat", in orange, above the numbers it qualifies. That is entry 21 section 4 working exactly as intended, and it is the difference between a number and a number you can trust.

The flyer line is also good and correctly reasoned, though at two shots it is noise like everything else in that panel.

### 7. One question I could not answer from the screenshots

**Can the reference length be anything other than one inch?** The export says "a single 1 in reference length" and the toolbar button shows no value. Most targets have no convenient one inch feature, and his has a 1.5 inch grid, so if the length is fixed at one inch then anybody measuring across a grid square is out by fifty percent and nothing tells them.

If it is fixed, make it an entry. If it is already an entry, the export should record the value used rather than the phrase, so a reader can check it.

---

## 2026-09-15, entry 23: question 11 answered, the fixtures are regenerated, and the surface model is not as dead as entry 17 left it

**Status: open.**

### 1. Question 11: A, A and A, and the first one is already done

Your reasoning is right in all three cases, and the common thread is the one that matters: **keep GroupLab exact where shotGroups is not, and keep the gate checking something true.** A gate that reproduces another implementation's root-finder tolerance is not measuring correctness, it is measuring agreement with a defect.

**1, the point of aim. Done rather than decided.** The fixtures are regenerated and committed, and `sg_dump.R` now emits `shots.xPOA` and `shots.yPOA` beside `shots.x` and `shots.y`. That was my error in the first version: `getXYmat(..., relPOA = FALSE)` does not carry the aim, so a fixture built from the matrix alone cannot reproduce anything `groupLocation`, `groupSpread` or `groupShape` computed from the frame. Your diagnosis was exactly right and the arithmetic confirms it:

| Dataset | Distinct aim points | Frame-based centre x | Matrix-based centre x |
|---|---|---|---|
| `DFinch` | **9** | -0.9195 | 6.1847 |
| `DFcm` | **9** | -2.3356 | 15.7091 |
| `DF300BLK` | 1, at the origin | -0.0004 | -0.0004 |

The 7.1 in gap on `DFinch` sits inside its aim range of 5.518 to 7.806, and `DF300BLK` agrees to four decimals because its aim is zero. That is the disagreement you cited, with the missing datum now supplied.

**Both coordinate forms are emitted rather than one**, because `xyTopLeft = TRUE` flips y and anyone deriving either from the other has a sign convention to get wrong, which is the kind of thing that costs a day. Row counts grew: 600, 520, 1857, 2553, 9464, 9464, 6563, 5108, 32543. Section 15.5 point 2 is now reachable.

**2, the CorrNormal CEP. Option A.** Gate the distribution through the hit probabilities, which already match to 1e-15, require GroupLab's own CEP to satisfy that distribution at 1e-12, and compare shotGroups' CEP at 1e-4 relative. Replicating its root finder would mean shipping its tolerance, and a CEP that misses its own probability by 3e-6 is a defect rather than a convention. Record it in section 15.4 with the measured misses so nobody re-derives it.

**3, the SMOA round trip. Option A.** Section 15.4, with the constant. The anchor holds for `getMOA` and fails only on the inverse, which is the definition of a one-directional bug.

**Your four handled findings are handled correctly**, and the MANOVA one is the sharpest. `sg_dump.R` taking `MANOVA[1, ]` gives R's intercept row, which tests whether the mean over all shots is the origin rather than section 8.2's test of the group centres. Reproducing that row for the gate and computing the real group test separately is right. **Note it in `STATISTICS.md` section 15.4 as a tenth known difference**, because the next person to read the fixture will assume row 1 is the group test, exactly as I did when I wrote the script.

**`DFcm` and `DFinch` are not the same data**, and that is a finding about the package rather than about us. The README now says so. Section 15.2 should stop calling them the same data and say what they are: the same shots, differently grouped, with one shot in a different series.

### 2. The bootstrap coverage is the most important thing in question 11, and it is filed as an aside

79.5, 89.1 and 92.7 percent actual coverage at 10, 25 and 50 shots, against a nominal 95. **At ten shots a "95 percent interval" is a 79.5 percent interval.**

That is not a footnote. It is the exact error this project exists to prevent. A shooter comparing two loads on ten-shot groups, shown an interval that claims 95 and delivers 80, will conclude one load beats the other when the data does not support it. `STATISTICS.md` section 6 flags groups under ten shots as unreliable, which does not cover this: the problem is at ten, twenty-five and fifty.

**Three things follow, and none is a research project.**

1. **Prefer a closed form wherever one exists.** Section 3.3 has a closed-form interval for sigma, and section 15.3 already gates it at 1e-12. The bootstrap should be the fallback for quantities with no closed form, not the default.
2. **Where the bootstrap is used, the interface must not print a bare "95 percent".** Either state the measured coverage at that sample size, or label the interval as approximate and optimistic at small n. A number that is wrong and confident is worse than one that is wide and honest.
3. **Add coverage to the gate.** Section 15.5 point 4 already requires 94.0 to 96.0 percent coverage for the known-truth synthetic test on 25-shot groups. Measure the bootstrap the same way and record the number rather than leaving it in a command nobody runs.

Put the table in `PHASE1-RESULTS.md` where it is, and raise a question if any of that changes what you have already built.

### 3. The photograph against its own scan: the surface model is not dead

This is the entry 19 and 20 measurement, and it changes the picture that entry 17 left.

**A flat-plane fit leaves 0.021 in RMS and the general developable surface brings it to 0.006 in.** On the nine pinned frames the same model took up almost nothing, which is what stopped the surface work. The difference between the two cases is the mounting: those frames were a sheet hanging from a single pin, free to twist, and this one was lying on a mat with a gentle sag. **A developable surface handles the gentle case and fails the twisted one**, which is exactly what your own synthetic sweep said when it broke at a quarter inch of twist.

So entry 17's conclusion stands as measured and its scope was wider than the evidence. The right statement now is that no developable surface fits a sheet twisting on a pin, and that the model does most of the work on a sheet deformed gently. **It does not pass the gate even here**, at 0.006 against 0.005, on a 25-bull constraint far coarser than 136 marker corners. But it is close on a case nobody had measured, where it was nowhere on the case that stopped it.

**This still does not settle the mounted question**, because that sheet was lying on a mat and no photograph in the collection is both a whole sheet and mounted. It does mean the mounted case deserves the measurement rather than being written off, and next weekend's session produces exactly the frames it needs.

Record this in `PHASE1-RESULTS.md` beside M1.11 as an amendment with its date, not as a replacement. M1.11 was right about what it measured.

### 4. Detection must run inside the sheet, and that is a cheap large win

**0 of 28 holes on the whole photograph, 26 of 28 cropped to the sheet, untuned.** The dark mat merges into one twelve inch blob that swallows everything.

The fix is architectural rather than a tuning parameter: **the detector runs inside the registered sheet boundary, never on the whole image.** Registration already knows where the sheet is, so the crop is free. Make it a property of the pipeline rather than a step a caller can forget, so that no future path can hand the detector a full frame by accident. `docs/DETECTION-PIPELINE.md` should say so in the stage that precedes S5.

**The 0.023 in median centre difference is the backer material, measured for the first time.** The photograph sees the dark mat through each hole where the scan sees the white scanner lid, and 0.023 in is nearly three times the 0.008 in noise floor. That is a real limit on the photograph path and it is not a defect: it is what a hole looks like against something dark. It also says the backer question in the planning record is not a preference, it is a term in the error budget. Next weekend's sheets are shot against your normal backer, so that number gets a second measurement on a GroupLab sheet.

### 5. The uncommitted files: commit them, with one exception

`scans/mounted/` and my two scripts in `tools/scan_analysis/` are mine to call.

**Commit both scripts.** `scrub_exif.py` is about to become load-bearing, per entry 22, and `straightness.py` is a measurement that failed its own control and is worth keeping as a record of an attempt rather than being silently dropped.

**Commit `scans/mounted/`**, but run every file through `scrub_exif.py` first and commit the scrubbed copies, not the originals. 22 of those 23 carry GPS. They are Alan's own photographs so there is no consent question, but the repository is going public and there is no reason for his range coordinates to be in it. Keep the originals outside the repository.

**That is 28 files and roughly 80 MB**, which is the size question of entry 22 arriving early. If your answer to entry 22 section 1 is a separate data repository, these belong in it and should wait. If it is Git LFS, configure it first. **Answer entry 22 section 1 before committing the images**, and commit the two scripts either way.

### 6. M4, and the thing to do next

Nobody has opened the window. That is Alan's next step and it is the first time the project has been something he can use rather than read about. Everything else waits on what he finds.

**Entry 21's remaining scope is mine**, and I will specify adjust-to-zero, the calibre input and the phone question once he has actually used what exists. Specifying a second round of interface before anyone has touched the first round is how you get features nobody wanted.

---

## 2026-09-15, entry 22: donated photographs are arriving, and the repository is not ready to receive them

**Status: open. Not blocking M3 or M4.** Act on section 1 before the first donated image is committed, which could be within days.

### 1. The size problem, which has to be decided before anything lands, not after

A public upload page at `pissinhot.com/targets` is built and about to be announced to roughly 400 people across two Discord servers. If even fifty of them submit three photographs each, that is on the order of **half a gigabyte to a gigabyte of binary files**.

**Git handles that badly and the repository cannot absorb it.** Every clone pulls every byte of every version forever, binaries do not delta-compress, and a photograph that is later scrubbed of GPS is a second full copy in history rather than a small diff. The current repository is a few tens of megabytes; this would make it one to two orders of magnitude larger and make a fresh clone a chore.

**Decide now, because the cost of deciding later is a second history rewrite.** This project has already done one, with `git filter-repo`, to purge another company's files. Doing it again over donated photographs would be worse, because by then the images will be other people's contributions rather than Alan's own files.

Three options, and I have not chosen for you because this is an infrastructure decision and you can see the repository:

- **A separate data repository**, say `grouplab-testdata`, referenced from the main one by URL and commit. The code repository stays small and clonable, and the data carries its own licence and provenance. My inclination, because the two have genuinely different lifecycles and the test data will keep growing while the code churns.
- **Git LFS on the main repository.** One repository, but it needs LFS configured before the first image lands, and it puts a dependency on every future contributor.
- **Keep a small curated subset in the repository** and the full set outside it. Cheapest, but somebody has to curate, and the whole value of a donated corpus is its breadth.

Raise this as a question with your recommendation once you have looked at what the repository actually is. It is genuinely yours to call.

### 2. The intake pipeline, which must be a gate and not a habit

Submissions arrive as a directory per submission holding the original files and a `meta.json` carrying the answers, the consent record, and a SHA-256 per file. Nothing may enter public test data except through a single tool that does all of this:

1. **Refuse any directory containing a `DO-NOT-PUBLISH` file.** The page writes that file when a contributor ticks the opt-out box, alongside a flag in `meta.json`. Honour the file, not just the flag, because a file is harder to miss.
2. **Scrub GPS.** `tools/scan_analysis/scrub_exif.py` already does it: it keeps `Make`, `Model`, `Orientation`, focal length, the 35 mm equivalent, f-number, exposure and ISO, and drops everything else including every GPS field, maker notes, serial numbers and dates. It is what found that 22 of Alan's own 23 photographs carried coordinates, so treat the unscrubbed state as the normal one.
3. **Record both checksums.** Scrubbing changes the bytes, so the SHA-256 in `meta.json` will not match the published file by design. Carry the received hash and the published hash side by side. That is what makes it provable later that the image in the repository is the image that was consented to, rather than something that drifted.
4. **Carry the provenance with the image.** Submission ID, the consent text version, the submission timestamp, and the answers. If anyone ever asks under what terms a photograph is published, the answer has to be in the repository and not in a server directory nobody kept.

### 3. Make it a test, because a step someone remembers is a step someone forgets

**A test must fail if any file under the public test data path carries a GPS tag**, or sits in a directory marked `DO-NOT-PUBLISH`, or lacks a provenance record. Not a documented procedure. A failing test.

The reasoning is the same one that made the `reference/` files a blocker: publishing is irreversible in a way that local mistakes are not, and this time the material belongs to other people who were given a specific promise about it. The consent text says GPS is removed before publication. A test is how that promise stops depending on anybody's memory.

### 4. What this does not change

M3 and M4 continue. None of this is urgent enough to interrupt them, and no image can arrive until the page is live and announced. But section 1 wants an answer before the first commit rather than after, and section 3 wants to exist before the first image, not before the first release.

---

## 2026-09-14, entry 21: a manual marking path, which is a second product and mostly already built

**Status: actioned 2026-09-14.** M4's first screen is the marking screen of section 3, built as both the manual path and the correction interface, with section 4's rectangle offered beside the length; reported in `docs/PHASE1-RESULTS.md` M4.1. Sections 5 and 6 are left for specification, as section 8 says.

### 1. What Alan asked for

A workflow like Ballistic X, on mobile in particular: photograph the group, establish a one inch reference, give the calibre, mark the aiming point, mark each impact by hand, get the statistics. He supplied his own exports from that application. They show numbered reticles on each impact, a separate marker on the point of aim, a group-centre dot and a mean-radius circle, and a statistics panel carrying group extreme spread in inches and MOA, bounding width and height, adjust-to-zero in MOA with its direction, elevation and windage offsets, mean radius, CEP, radial, vertical and horizontal standard deviations, and windage and elevation extremes.

### 2. This resolves entry 20's problem rather than adding to the pile

Entry 20 found that twelve of twenty-four of Alan's own photographs are close-ups of a single group with no sheet edge in frame, and that GroupLab as specified can use none of them, because registration needs a fiducial lattice that is not in the picture. It also found a whole target family, the fluorescent splatter targets, where a hole is a bright saturated halo and the neutral-darkness detector will find nothing.

**A manual marking path uses every one of those photographs.** It needs no fiducials, no registration, no surface model and no hole detection. It works on a close-up, on a splatter target, on a commercial bullseye, on a heavily compressed picture forwarded through a chat app, and on a sheet bowed on a board. The photographs Alan actually takes are exactly the input this path is for, and that is not a coincidence: it is what he has been using such an application for.

### 3. So GroupLab has two paths, and they meet in the middle

| | **Manual marking** | **Automatic** |
|---|---|---|
| Input | Any photograph of any target | A GroupLab sheet, whole, in frame |
| Scale from | The user, against something of known size | The printed fiducials |
| Impacts from | The user, tapping each one | Detection |
| Accuracy | The user's tap and the scale reference | Measured: 0.0033 in on a scan |
| Works today on | Everything in `scans/mounted/` | Nothing in `scans/mounted/` |
| Needs | M3, and a marking screen | M1, M2, M3, and the printed sheet |

The middle is the useful part and it already has a requirement: `DESIGN.md` section 13 mandates a manual assignment interface because real targets carry hand-drawn arrows no geometric rule recovers. **The marking screen and the correction screen are the same screen.** Build it once. Automatic detection then becomes a way of pre-filling marks the user can accept, move or delete, rather than a separate mode.

**The manual path is close to free.** It needs the statistics engine, which is M3 and being built now, plus a screen with a scale tool, a point-of-aim tool and an impact tool. It needs nothing from M1 or M2. It could therefore be usable before the automatic path is, which inverts the current plan's order of value.

### 4. Where GroupLab can be honestly better, for two extra taps

**A single one inch reference assumes the photograph is square on and the sheet is flat.** It is a uniform scale applied to the whole image. Off-axis, that is wrong and wrong by a varying amount across the frame, which is the same perspective problem this project has spent Phase 0 and half of Phase 1 on. A group measured near the far edge of an off-axis frame comes out smaller than one measured near the camera, and nothing in the output tells the user that happened.

**If the user marks a known rectangle instead of a known length, the perspective goes away exactly.** Four taps rather than two, and it yields a homography rather than a scale factor: the same mapping the automatic path fits from fiducials, from four user-supplied points instead. Most targets make this easy, because most carry a printed grid, so the rectangle is a grid square or a block of them with a known size. The splatter targets in Alan's collection have a one inch grid. The orange sight-in sheets have a finer one. The OnTarget sheet has a ruled box per bull.

The machinery exists. `Registration/PageMapping.cs` and the homography fitting already take four correspondences and produce exactly this. **Offer both: a length for speed, a rectangle for accuracy, and say which one a given result used.** A result that knows it came from a single scale on an off-axis frame can say so, which is more honest than a number that quietly absorbed the error.

**What a rectangle still cannot fix is a bowed sheet**, because a homography is planar. That is the open problem of entry 17 and it does not go away here. But it is strictly better than a single scale, and it costs two taps.

### 5. Two things this adds that `STATISTICS.md` does not cover

1. **Adjust to zero.** Converting the group centre's offset from the point of aim into scope adjustment. It needs the click value of the user's turret, a quarter MOA, a tenth of a mil and so on, and it needs a direction convention stated once and never got wrong. `STATISTICS.md` section 12 has the angular conversion constants and section 8.2 has group location; the missing pieces are the turret click value, the up-or-down convention, and the arithmetic between them. Small, useful, and the single most-used number in the exports Alan supplied.
2. **Calibre as an input.** Ballistic X asks for it, and it is what turns a marked impact centre into an edge-to-edge group measurement, because the conventional group size is measured outside edge to outside edge minus one bullet diameter. `docs/SCAN-MEASUREMENTS.md` section 3.5 already measured hole diameter against nominal calibre on 343 real holes, so the relationship is characterised. Decide and document which convention GroupLab reports, because centre-to-centre and edge-to-edge differ by exactly one calibre and shooters argue about it.

### 6. The mobile question, flagged and not decided

Alan said "especially the android and ios versions". The current plan is Avalonia, which does target both, but mobile is the less-travelled path for that toolkit and none of it has been tried. **The manual path is the natural thing to put on a phone**, because the input is a photograph the phone just took and the compute is trivial, where the automatic path wants a scanner or a careful full-sheet capture and real processing.

That is a scope decision with real cost and it is not being taken here. **M4 stays as the brief has it: a desktop shell.** Build the marking screen so that it is not gratuitously desktop-only, which mostly means not assuming a mouse, and leave the decision until there is something worth putting on a phone.

### 7. The intellectual property line, which is the same line as OnTarget

Implementing a similar workflow is fine. A manual marking interface with a scale reference is a generic interaction pattern, and the statistics are published mathematics that `docs/STATISTICS.md` already derives from the literature and validates against shotGroups. **What is not fine is copying their branding, their icons, their export layout, their wording or any file format they read or write.** Same rule as OnTarget, same reason.

Two specific cautions:

- **Alan's Ballistic X exports must not go into the repository.** They carry that product's logo and export layout. This is the `reference/` problem exactly, which already cost a git history rewrite, and it would be worse in a public repository because the files are a competitor's branded output. Keep them out, or under the already-ignored `excluded/`. They are reference for the planning session and nothing more.
- **Worth an attorney question alongside the existing one.** `docs/PATENT-SEARCH.md` already carries US7769236B2 for attorney review. Add a second: whether any live patent covers photograph-based group measurement with a user-supplied scale reference. I have not searched it and am not asserting it is clear.

### 8. What to do

Nothing yet. Finish M3. When M4 starts, its first screen is the marking screen of section 3, built as both the manual path and the correction interface `DESIGN.md` section 13 requires, with section 4's rectangle option offered beside the length. Sections 5 and 6 are scope to be specified before they are built, not now.

---

## 2026-09-14, entry 20: what the mounted photographs actually contain, which is not what entry 19 asked for and is more useful

**Status: actioned 2026-09-14.** Section 5 measured by `grouplab mounted pair`, reported in `docs/PHASE1-RESULTS.md` "Entries 19 and 20": the flat-control frame needs a surface model, 0.0206 in RMS planar against 0.0063 in developable, and the scan's holes give the photograph's detector 0 of 28 whole-frame and 26 of 28 on the registered sheet.

`scans/mounted/` now holds 28 photographs. I have looked at all of them, which is the one thing this session can do that yours cannot, and the looking is worth more than the measuring. Four of the 28 have a `~` in the filename and could not be staged here; they are on disk and you can reach them.

### 1. My own measurement failed its control, and is not reported as a result

I tried to bound the deformation without fiducials. A flat sheet through a rectilinear lens maps printed straight lines to straight lines, so the bend in a line that was printed straight is lens plus paper and nothing else. Both target families here carry a printed rectangular grid, so the grid supplies the straight lines.

**The control kills it.** Run against `scans/n568-gm210m.jpg`, a flatbed scan with no lens and no perspective and therefore physically flat, the method reports a median worst-deviation of **0.16 percent of frame** and a worst line at 0.38 percent. The photographs run 0.26 to 0.57 percent at the median and 0.52 to 0.77 at the worst. The photographs are above the floor, so real bend is there, but the floor is over half the signal and the per-image spread is narrower than the floor's own variation. It cannot rank images and it cannot give a figure in inches.

This is the `PHASE0-PRELIM.md` lesson again: a scratch measurement next to a validated pipeline is worth reporting only when it has a control it passes. It did not, so the numbers stay here as a record of the attempt and go nowhere near a results document. **Section 5 hands the real measurement to you.**

### 2. The collection is mostly close-ups of one group, and that is the finding

Of the 24 I could examine, **twelve are close photographs of a single shot group**, filling the frame, with no sheet edge and often no second bullseye in view. Two of those have fingers holding the paper in shot. This is how Alan actually photographs a target, and it is nothing like the nine pinned full-sheet frames the mounted gate is measured on, or like anything in the brief.

**GroupLab as specified cannot use any of them.** Registration needs the fiducial lattice, the lattice is spread across the sheet, and the sheet is not in frame. That is not an argument that the design is wrong: he shoots commercial targets, and there is no reason to photograph a commercial target any other way. But "the user photographs one group up close" is a real habit, it was not in any requirement, and a full-sheet requirement cuts directly against it.

**One thing makes this less bleak, and it comes from a decision already taken.** Entry 13 chose option A, the half lattice, which roughly triples marker density on the large sheets. A close photograph of one bull on a dense lattice may still contain three or four markers, which is enough to register that neighbourhood even when the sheet is not in frame. That was chosen for bracketing and for the surface fit. It may turn out to matter more for this. **Do not build anything for it now.** Record it as a possibility against the day someone asks whether a partial-sheet photograph can work.

### 3. Four target families, and one of them breaks the hole detector outright

| Family | Frames | Hole appearance |
|---|---|---|
| Orange sight-in sheets, fine orange grid on white | 12, all close-ups | Ragged tear, **bright yellow-green** where the layer behind shows through |
| Black splatter targets, yellow-green grid, red aiming squares | 7, full or part sheet | **Bright yellow-green halo**, the opposite of dark |
| NRA A-26 bullseye, buff card, hanging indoors with open air behind | 1 | Dark, with bright specks of metal on the rim |
| OnTarget 25-bull grid sheet on a mat | 1 | Dark, brown mat behind |

**`NeutralDarknessHoleDetector` is built on the survey's finding that a hole is a neutral dark region, and on seven of these frames a hole is a bright saturated one.** On a splatter target the paper's coating flakes away to reveal a fluorescent layer, so the hole is the brightest and most saturated thing in the cell. Neutral darkness is near zero there and the opening step will erase what is left. Expect it to find nothing, and treat that as the correct answer from a detector that was never asked to handle this.

That is not a defect to fix now. It is a target family nobody specified, and the honest reading is that GroupLab's detection is specified for ink-on-paper targets and this collection contains a second class of target it does not cover.

### 4. Five things in these frames that exist nowhere else in the corpus

1. **A photograph and a scan of the same physical sheet.** `scans/mounted/20260329_183028.jpg` is the `N568 GM210M` sheet, handwritten label and all, and `scans/n568-gm210m.jpg` is a 600 DPI flatbed scan of it. **This is the first ground truth for a photograph that the project has ever had.** The scan gives hole positions on a flat reference; the photograph is the same holes through a lens at an angle. Section 5 is built on it.
2. **Hand-drawn assignment arrows, in a photograph.** That same frame carries a blue arrow from bull 14 to a shot in bull 4, and a second arrow into bull 18. `SAMPLE-NOTES.md` records these on scans and `DESIGN.md` section 13 requires the manual assignment interface because of them. Here they are on a photograph, in ink, over the printed grid.
3. **A keyhole.** Bull 4 of the same frame holds an elongated gash, a tumbling bullet through the paper sideways, several times longer than it is wide. Every hole model in this project assumes a roughly round perforation with a lobed rim. Nothing would size or centre that correctly, and `getMaxPairDist` style measures would be badly wrong if it were taken as one round hole.
4. **Backer material, answered with real examples and not one answer.** The open question in the planning record asks which backer Alan shoots against. These show at least four: a fluorescent splatter layer, a second target stapled underneath, a brown mat, and open air at an indoor range. It changes the hole's appearance completely and there is no single answer to design to.
5. **Google Photos did not strip the EXIF.** Full resolution, 4000 by 3000 and 3072 by 4080, with model, focal length, f-number and 35 mm equivalent intact. **And the 2.2 mm at f/2.2 tagged as a 23 mm equivalent appears again on most of the Samsung frames**, which is the cropped-ultrawide signature entry 16 identified from three table photographs. It is his habitual capture mode, not an accident on one session, so the joint-fit grouping fix of entry 16 section 2 is load-bearing rather than an edge case.

### 5. The measurement to run, when M3 is reported

**Use the photograph and scan pair.** This needs no fiducials and no assumption about the target's geometry, because the scan is the reference.

1. Locate the bull centres in `scans/n568-gm210m.jpg` with the connected-component approach `docs/SCAN-MEASUREMENTS.md` section 2.1 already measured at 24 of 25, and in `scans/mounted/20260329_183028.jpg` the same way.
2. Match them by grid position, 25 correspondences on a 5 by 5 grid.
3. Fit the Phase 0 photograph model, homography with the two-term radial lens, scan to photograph, using the EXIF grouping rule as amended.
4. **Report the residual**, in scan inches, with its spatial correlation, exactly as M1.11 did for the pinned frames.
5. Then fit the generalised cylinder and the general developable surface from M1.10 to the same correspondences and report whether either takes the residual up.

That is entry 19's question asked properly, on one frame, against real ground truth: **how far from flat is a sheet Alan actually photographed, and is that deviation a shape paper can bend into?** Twenty-five bulls is a coarser constraint than 136 marker corners and the answer will be correspondingly rougher, but it is measured rather than eyeballed, and it is the only frame in the collection that can be measured at all.

One caution to carry into it. That sheet is **lying on a mat, not mounted**. Its corners show four staple tears, so it was on a board and came off. So it answers "how flat is a sheet Alan laid down and photographed", which is the flat control case, not the mounted one. Say so in the report rather than letting it stand as the mounted answer. **No frame in this collection is both a full sheet and mounted**, which is the frame entry 19 was asking for and did not get, and that is worth stating plainly rather than working around.

---

## 2026-09-14, entry 19: the mounted benchmark is nine photographs of a sheet hanging from one pin, and that may be the worst case rather than the normal one

**Status: actioned 2026-09-14.** Both measurements run on the one frame that supports them, the N568 GM210M photograph and scan pair, and reported in `docs/PHASE1-RESULTS.md` "Entries 19 and 20". Measurement A has no stapled full sheet in the collection to run on, as entry 20 section 5 also found; measurement B has truth on that frame only.

### 1. What Alan said, and why it matters more than it sounds

He has a large existing collection of photographs of shot targets **stapled to target boards**, taken the way he actually photographs targets. They are not GroupLab sheets, so they carry no fiducials and cannot be registered by the pipeline at all.

**The obvious reading is that they are useless to the mounted gate. That reading is wrong, and the reason is a sampling problem I introduced.**

Every frame in `scans/phase0/` that the mounted gate is measured on is the same sheet **hanging from a single pin**. I asked for that, in the Phase 0 print protocol, and entry 8 records that the original wall photographs were my fault for the same reason. Nine frames, one sheet, one mounting, and that mounting is the most deformation-prone arrangement a sheet of paper can be put in: unsupported, free at three edges, and free to twist about the pin. A sheet stapled flat against a rigid backer board is a different object. It is held against something solid, it bulges slightly between fixings, and it has very little freedom to twist.

**So `PHASE1-RESULTS.md` section M1.11 may be a true statement about the wrong population.** The finding stands exactly as measured: on those nine frames a general developable surface takes up almost none of the residual, so what is left is not a bendable shape. What is not established, and what I have been writing as though it were, is that a **stapled** target deforms the same way. A sheet twisting on a pin is precisely the case a developable surface handles worst, and it is the only case anyone has measured.

Alan did say, in entry 10, that the pin-hung photographs were a realistic scenario, and he is right that it happens. The error is not that the case is unreal. It is that it is the **only** case in the corpus, and it was chosen by me rather than sampled from what people do.

### 2. Two measurements those photographs support, neither of which needs a fiducial

**Measurement A: how much does a real mounted target actually deviate from flat?**

A commercial target sheet carries a **regular grid of bullseyes at a known nominal spacing**, which `docs/SCAN-MEASUREMENTS.md` section 2 already measured across this corpus: 1.5 in pitch on the 300 yard sheets, with the rings at 1.257 in, and a detector that finds 24 of 25 bulls by connected components on a scan. That grid is a known-geometry object. It is coarser than the fiducial lattice and it does not give a scale, but it is enough to fit a plane-to-plane mapping and look at what is left over.

For each photograph: detect bull centres, fit a homography over them, and report the residual, its spatial correlation, and whether a developable surface takes any of it up. That is the same machinery M1 already built, pointed at a coarser set of points.

What it answers:
- **How far from flat is a stapled target**, against the 0.048 to 0.114 in the pinned frames showed.
- **Is the residual developable on a stapled target?** If a cylinder or a cone takes it up where nothing took up the pinned frames' residual, then the surface work was sound and was benchmarked on an outlier.
- **What do real photographs look like** as inputs: angle, distance, framing, lens, lighting.

Report it against the pinned frames in the same table. Do not fit anything new; use what M1 has.

**Measurement B: holes in photographs, which the corpus does not contain at all.**

Every one of the 343 holes in `docs/SCAN-MEASUREMENTS.md` is from a flatbed scan. The survey's own section 3.1 says why that matters: the bright core of a scanned hole **is the scanner lid seen through the perforation**, and `SAMPLE-NOTES.md` says the same, that berm backer material does not affect scanned appearance at all and affects photographs only. **So the one measured fact the hole detector rests on does not hold in a photograph.** A hole photographed against a target board is dark, not bright, and its appearance depends on what is behind it.

`NeutralDarknessHoleDetector` has never met that case. Run it over these photographs and report recall and what it confuses, with no tuning. Whatever it does is the baseline for photographed holes, and it is a gap the synthetic work in M2.2 explicitly could not cover: M2.2 section 6 lists the dark backing as the one case where render-and-difference lost to the baseline, and this is the real version of it.

### 3. What is being asked for

A sample, not the collection. Roughly twenty photographs in `scans/mounted/`, camera originals with EXIF intact, chosen for variety rather than quality: different boards and mountings, different angles including deliberately off-axis, different distances, different light, and a couple that are frankly bad. A README naming, as far as he remembers, how each was mounted.

**Do not ask for more paper work on the back of this.** It is a file copy, it is bounded at twenty, and the range session next weekend is unchanged.

### 4. What this does not do

It does not reopen the gate, which entry 17 section 2 settled and which stays at 0.005 in. It does not resume the surface models, which entry 16 section 5 stopped. It is a measurement of the input distribution, and its purpose is to establish whether the mounted requirement is as far out of reach as nine frames of one pinned sheet suggest, or whether the benchmark was unrepresentative and the requirement is closer than it looks.

If measurement A shows stapled targets are near flat and their residual is developable, that is a finding that changes the roadmap and it should come back here as a question before anyone acts on it.

---

## 2026-09-14, entry 18: the shotGroups fixtures are generated and committed, M3 is unblocked, and question 9 is accepted

**Status: actioned 2026-09-14.** Fixtures and scripts committed in `20f562e` with the brief's section 4.4 amendment and questions 7 to 10 marked answered; M3 built against the fixtures and reported in `docs/PHASE1-RESULTS.md` M3.1, with the differences found raised as question 11.

### 1. Question 10: done, not delegated back

You were right that this belonged here, for the reason question 2 belonged here: the planning session has a Linux container and your machine does not have R. It turned out this container already had **R 4.3.3 and shotGroups 0.8.4**, the exact versions `STATISTICS.md` section 15.1 names, and `coin` besides.

**`test/fixtures/shotgroups/` now holds 64,694 rows across nine datasets plus the Monte Carlo table**, with a README carrying provenance, versions, licence and regeneration instructions. `tools/shotgroups/sg_dump.R` is extended and `tools/shotgroups/sg_distr.R` is new. Both are committed with the fixtures.

Every gap you listed is filled:

| Gap | Now |
|---|---|
| Input shot coordinates | `shots.x`, `shots.y`, `shots.distance` per shot, plus series and group indices |
| Per-group results for multi-group datasets | The whole battery per series, on all seven multi-group fixtures |
| Group-comparison tests | `compareGroups`, both branches, with the test names probed rather than assumed |
| Monte Carlo reference tables | `shotGroups_DFdistr`, 590 cells, 490 of them inside section 15.3's gate range, with complete coverage of n 2 to 50 and nGroups 1 to 10 |

**The old keys are unchanged.** Scope is empty for whole-dataset results, so every key the earlier script produced is still there with the same value. Verified on `DF300BLK`: all 454 original keys present, all 454 values matching, excepting the one stochastic key below. Build the comparison harness against the documented scheme and nothing you have already reasoned about has moved.

### 2. Four things the generation turned up, each of which would have cost you a day

**One value is not reproducible and must be excluded by name.**
`groupShape.multNorm.p.value` is a Monte Carlo energy test. Two identical runs gave 0.5424 and 0.5590 on `DF300BLK`, and 0.8346 and 0.8379 on `DFcciHV`. The script now seeds the generator so regeneration reproduces, but **a seed does not make it comparable**, because your implementation draws from a different generator. Each JSON lists the key under `stochastic`. Exclude it there rather than meeting it as a 1e-12 failure. Every other value in every fixture is deterministic, which I checked by running each dataset twice and diffing rather than by assuming.

**`DFsavage` produces no angular keys at all, and that absence is the test passing.** Section 15.5 point 3 wants multiple distances in one frame to suppress angular output rather than produce a wrong number. `angular.nDistances` is 3 and there is no `getMOA` or `fromMOA`. The count is emitted either way, so your harness can assert on positive evidence instead of on a missing key, which is the difference between a passing test and an untested path.

**Range statistics stop at n = 100**, the largest cell shotGroups tabulates. `getRangeStat` runs at any size, warning and returning NA intervals past the table, but `range2sigma`, `range2CEP` and `getRangeStatEff` raise an error. The pooled scope of the five large datasets carries `_error` keys for those three. Every per-series scope is well inside the table. Expect the errors; they are the package's limit, not a generation failure.

**`DFlandy01` does not exercise what section 15.2 chose it for**, and this one is a defect in the plan rather than in the data. It was picked for "range statistics with many groups", but `getRangeStat` takes a coordinate matrix and has no group argument: handed a 53-group frame it pools all 530 shots into one. The package's real multi-group path is the `nGroups` argument of `range2sigma`, `range2CEP` and `getRangeStatEff`, now emitted under `multiGroup.*`. **Those tabulate only to 10 groups**, so `DFlandy01` at 53 is past the table and carries `multiGroup.beyondTable` rather than values. **`DFlandy04`, at 6 groups, is the fixture that actually exercises the multi-group range path.** Amend section 15.2's stated purpose for both rather than leaving a fixture described as testing something it cannot reach.

I also found and fixed a bug of my own while generating these, which is worth one line because it is the same class of error the project keeps meeting: a single out-of-range call inside one error handler was discarding three working results alongside it. Split handlers, and a section that could not run records an `_error` key instead of vanishing, because a fixture that silently omits a section looks identical to one whose section produced nothing.

### 3. Question 9: accepted, and my brief was internally inconsistent

Change gate 2's matching tolerance to **0.15 in**, and keep centre accuracy reported rather than gated, judged against real paper.

You are right and the brief was contradicting itself. `docs/PHASE1-BRIEF.md` section 4.4 point 2 set a "hole-centre tolerance of 0.01 in" for counting a detection as a true positive, and point 3 said centre accuracy is "reported, not gated". The first is a matching radius, deciding whether a detection and a truth hole are the same hole, and the second is an accuracy requirement. I wrote a matching radius at the value of an accuracy requirement, and at 0.01 in it sits on the 0.008 in noise floor, so a correct detection of a real-looking hole would be scored a miss and a false positive at once.

**0.15 in is the right number for a better reason than being looser**: it is the hit tolerance `SCAN-MEASUREMENTS.md` section 8 used for the naive baselines. Adopting it makes your figures directly comparable with the 1 of 27 and the 7 of 20 already on record, which is worth more than any number I would pick now. It is also unambiguous at this geometry, being about half a hole diameter and a tenth of the 1.5 in bull spacing, so it can pair a detection with the right truth hole and cannot pair it with a neighbour.

Amend section 4.4 in the brief itself, marked as an amendment with this entry as the reason, rather than silently. A brief is a record of what was asked.

### 4. Questions 7 and 8 are already answered

Entry 16 answered both, section 3 for question 7 and section 2 for question 8, and your last report confirmed you did the work: the joint-fit key now includes the 35 mm equivalent and the image size, the disagreement warning is gone, and the sweep and frozen READMEs state that their sheets fail test 26f by design. Only the `Status:` lines in `docs/QUESTIONS-FOR-PLANNING.md` were not flipped. Mark both answered, pointing at entry 16.

**Nothing about next weekend's printing is waiting on anybody.** Question 7's answer has been standing since entry 16: print the five marker-size sheets as they are.

### 5. Carry on

M3 is unblocked. Build the engine against `docs/STATISTICS.md` section 15 with these fixtures. The two open questions of section 16 were answered in `docs/PHASE1-BRIEF.md` section 5 and still stand: apply `c4` to the interval endpoints and match shotGroups, and make mean radius the headline with sigma beneath and extreme spread subordinate.

Then M4. The rest of the brief is unchanged.

---

## 2026-09-14, entry 17: the gate does not move, the mounted case is an open requirement, and M2 starts now

**Status: actioned 2026-09-14.** Sections 2 to 4 are `docs/PHASE1-RESULTS.md` M1.11 and `DESIGN.md` section 21 [r6] (the gate record, the open requirement, frames fitted alone by default, the plane below eight corners); section 5's diagnostic is M1.11 (the leftover is mostly not structured, with a structured part on three mounted frames); section 6's baseline is M2.1 (the port reproduces all 343 holes exactly), and M2 continues.

### 1. The surface models are finished and the answer is no

Recorded as the result it is. A cylinder fails all seven mounted frames. A general developable surface, which covers the cone and the twist your own shape test could not separate, also fails all seven, and it moves the corner residual by at most 0.16 px. That last figure is the informative one: **a model with strictly more freedom took up almost none of the leftover error, so the leftover is not a shape paper can bend into.** Paper bends without stretching, the fit now allows any such bend, and the error stayed. Whatever is left is not developable.

You built it, swept it, ran it once, and reported a negative. That is the outcome the brief was written to allow and it is worth more than a model that passed because it was tuned.

### 2. I proposed loosening the gate and I am withdrawing it

Alan was asked whether to argue a separate, looser gate for mounted photographs, on the grounds that the 0.005 in figure was inherited from the paper gate and never argued on its own. He declined to decide and asked me to. So I worked the error budget properly, and **it does not support loosening.**

The finest quantity this system measures is a hole centre, whose noise floor on real paper is 0.008 in. Standard practice is that a subsystem should contribute no more than about a third of the dominant term, so that it adds under five percent in quadrature. A third of 0.008 is 0.0027 in. Half is 0.004 in. **A principled budget argues for 0.003 to 0.005 in, which is where the gate already sits, at the loose end.** From the statistics the constraint is looser still, since registration error of even 0.02 in inflates an estimated sigma by under one percent on a typical group, and assignment is safe at a 1.5 in cell, but neither of those is the right anchor: the gate exists so that the instrument is not the limiting factor in what it reports.

There is a real argument I could keep pulling on, that "worst bull of 28" is a bound where the budget above is about typical error, and a worst-of-28 on a distribution with a 0.004 in typical value would plausibly run to 0.008 or 0.010. **I am not making that argument, because I am making it after seeing the results, and the best mounted frame came in at 0.00604 in.** A gate that lands within a thousandth of the number that makes one frame pass is not a gate. The Phase 0 gate is trustworthy precisely because `docs/PHASE0-PRELIM.md` set it before the results existed.

**So the gate stays at 0.005 in.** If it should move, the time to argue it is when there are more mounted frames, the argument gets written down and the number fixed **before** the new frames are measured, and it is then tested on frames that were not used to set it. Record this entry in the results document as the reason the number did not change, because a reader in six months should be able to see that loosening was considered and refused, and why.

### 3. The mounted photograph gate becomes an open, unmet requirement

Not a failure to fix now, and not a promise to withdraw. What is true today:

- **The scan path passes**, ten of ten at 0.00325 in worst.
- **The flat photograph path nearly passes.** Every scoring bull on the one frame that decoded all its markers is inside, and the failures are named.
- **The mounted photograph path does not pass**, by any developable surface, and the residual is not a bendable shape.

`DESIGN.md` section 21 already carries the mounted gate as Phase 1's and expects it to fail until a surface model exists. Amend it to say that a surface model now exists, that it is a general developable fit, that it does not meet the gate, and that the requirement is open with piecewise registration as the recorded fallback. Do not attempt piecewise now. It costs days budgeted for hole detection, it cannot help a bull outside the lattice, and the sensible time to try it is when real shot targets exist.

### 4. Two changes to make before M2, both cheap and both yours

1. **Fit each frame alone by default.** Your own finding: `main1` gives 0.00604 in alone and 0.01177 in the joint fit, a factor of two for sharing a camera across frames. A user photographs one target at a time, so alone is also the real usage. Joint fitting was only ever a way to constrain the lens, and entry 16's measurement showed the lens barely matters, moving the worst bull by at most three percent. Keep joint fitting available for a set known to be identical; make alone the default.
2. **Fix the selection defect.** Model selection defaulting to "bend kept" below eight corners is backwards: fewer corners means less evidence, so the default must be the model with fewer parameters. Default to the plane. Nothing triggers it today, which is the cheapest possible moment to fix it.

### 5. One bounded diagnostic, alongside M2, because it changes what we ask Alan to photograph

**Is the leftover residual on the mounted frames structured or random?** One command, using machinery this project already has: measurement 6 of Phase 0 computed spatial correlation on a displacement field to prove the printer error was paper-fixed, and `field.json` is its output. Run the same correlation over each mounted frame's post-fit corner residual.

- **Structured**, meaning neighbouring corners deviate together: there is real unmodelled geometry, it is not developable, and the advice to a user is about how the sheet is held.
- **Random**, meaning white across the sheet: the limit is corner quality on a foreshortened, defocused frame, and the advice is about light, aperture and distance.

Those are different sentences in the paper protocol, which is why it is worth one command now. It is a measurement and not a model; it does not reopen section 1 and it does not authorise a fourth surface. Report it in `PHASE1-RESULTS.md` and continue to M2 whatever it says.

There is a clue already. The flat controls sit at 0.60 to 0.84 px of corner residual and pass. The mounted frames sit at 0.96 to 1.88 px and miss by six to twenty times. **Roughly double the residual producing twenty times the bull error is the signature of correlated error rather than noise**, because independent noise averages out over a hundred-odd corners and structure does not. I expect structured. Measure it rather than taking it.

### 6. M2 starts now

`docs/PHASE1-BRIEF.md` section 4, unchanged. Port the neutral-darkness primitive of `SCAN-MEASUREMENTS.md` section 3.1, revalidate on all 343 holes, commit that as the baseline, then build the synthetic GroupLab sheets with holes drawn from the survey's own measured distributions so render-and-difference can be tested against truth before real shot targets exist.

### 7. On the three runs and the minimiser change

Running the frames three times so that the committed code reproduces the committed rows, and discarding the first run's raw rows because the committed code no longer produces them, is correct and is the rule from entry 11 applied to your own output rather than to a definition. Reporting that the minimiser change altered fourteen of a hundred and sixty synthetic trials and turned a marginal pass into a fail, rather than quietly keeping the better table, is the same discipline. Both are worth more to this project than a passing gate would have been.

---

## 2026-09-14, entry 16: two corrections to entry 15, both mine, and the cone goes before M2

**Status: actioned 2026-09-14.** Section 5's table is `docs/PHASE1-RESULTS.md` M1.8 (mounted frames at 0.96 to 1.88 px, so shape); section 2 is M1.9 (joint fits keyed on pixel geometry, no warning, the product finding); section 3 is in the module sweep and frozen READMEs; section 4's note is in M1.2; the general developable surface is M1.10, which recovers a synthetic cone and passes no mounted frame, so the surface models stop and M2 is next. Originally: answers questions 7 and 8, and decides the order.

### 1. Entry 15 section 4 was wrong, and it was wrong for an instructive reason

The lens is not what limits the mounted frames. You measured it: the M1 fit already shared one lens across all six main-camera frames, holding it fixed moves the worst bull by at most 3 percent and changes no verdict, and on synthetic sheets holding a lens rescues nothing a free lens fails.

**My evidence was real and my inference was not.** `main2`'s inflated k1 of -0.1597 and k2 of +0.3165 is a true fact about **Phase 0's per-frame planar fit**, which is what `photos.json` records, and in that model the lens does absorb bend, which is why the bent frame with 26 markers fits a wild lens where the flat frame with 25 fits a sane one. I then carried that straight across to the surface fit without checking what the surface fit does, and the surface fit had never used a per-frame lens. I read one model's output and drew a conclusion about a different model. That is the same error as reading a summary instead of the raw rows, one level up, and I have spent this project telling you not to make it.

**Corner noise deciding the synthetic gate between 1 and 2 px per axis is the more useful finding**, and it leads directly to section 5.

### 2. Question 8: accepted, and it corrects entry 15 section 1 as well

You are right that grouping by physical focal length and f-number mixes two pixel geometries, and right to add the 35 mm equivalent. **The reason is better than either of us said, and it means neither EXIF tag is wrong.**

Entry 15 said the table frames' 35 mm equivalent of 23 was the odd tag out because two tags of three said ultrawide. The distortion in `photos.json` says otherwise:

| Frames | `focalLengthMm` | `focalLength35mm` | fitted k1 |
|---|---|---|---|
| `ultrawide1-3` | 2.2 | 13 | -0.0296, -0.0765, -0.0679 |
| `20260913_1305xx` | 2.2 | 23 | **+0.0097, -0.0020, +0.0049, +0.0034** |

Four frames on the same physical 2.2 mm lens at the same f/2.2 show essentially **no distortion**, while three on that lens show a lot. A lens does not change. What changes is which part of its image circle is used. **The table photographs are a cropped or digitally zoomed ultrawide**: the physical focal length of 2.2 mm is correct, the 35 mm equivalent of 23 is correct as the effective field of view after the crop, the ratio 23 over 13 is 1.77 which is the crop factor, and a centre crop is exactly the low-distortion part of an ultrawide's frame. Both tags are true and they describe different things.

**So do not warn when they disagree.** Entry 15 asked for that warning and it would fire on every cropped or zoomed photograph a user ever takes, which will be a great many of them. The disagreement is information, not corruption. Group on the pixel geometry, which is what the 35 mm equivalent and the image width give you, and let the physical focal length be what it is.

**This is worth a line in the results document beyond the fix**, because it is a product finding: a phone that crops keeps the physical lens tag and changes the equivalent, so anything in this pipeline that reasons about a camera has to reason about the equivalent and the image width together, never about the physical focal length alone.

### 3. Question 7: print the marker-size sheets as they are

Agreed, and the reasoning is entry 11's. Those sheets are measurement inputs, not library sheets. The sweep measures the printer's dot gain against marker module size, and whether a sighter row is bracketed has nothing to do with dot gain. Changing them to conform would give all five new identifiers and would break the one property that makes the set cheap: the 0.5 mm sheet **is** the frozen Phase 0 sheet by identifier, so it carries its own control and inherits a second print session's worth of scans for free. Keep that.

Mark them the way the frozen fixtures are marked, in the sweep's README: these are inputs to a measurement, they are expected to fail test 26f, they are not to be brought into conformance, and here is which live sheet supersedes the geometry they are built on. **A repository that ships definitions failing its own conformance test needs that said out loud in every place it happens**, which now includes `targets/frozen/phase0/` as well. Narrowing the frozen sheet's exemption to exactly its three sighters rather than exempting it wholesale was the right call and is the pattern to follow.

### 4. Entry 13, and the row it cited

The roll check came back negative, so 1142 stands. That is what the check was for and I would rather have asked and been told no.

**The M1.2 shuffle correction does not undermine entry 13.** My fifth argument for option A cited the "23 at random" row for the proposition that markers spread across a sheet hold the fit where markers missing from one region do not. The corrected finding is that the rows were always the top 23 and that losing one region hurts, which **is** that proposition, stated more directly than the mislabelled row stated it. The argument is unchanged and option A stands on it. Finding the bug and relabelling without changing the numbers is the right handling; note in M1.2 that entry 13 cited it, so a reader tracing the decision lands on the correction rather than on the original claim.

### 5. The cone goes next, before M2, but measure one thing first

**Before building anything, put the mounted frames on the noise sweep.** You have found that with every marker present the synthetic gate breaks between 1 and 2 px of corner noise per axis, whatever the lens. `surface.json` now contains, for every mounted frame, the post-fit corner residual. Convert it to pixels per axis and place each frame on that sweep.

- **If the mounted frames sit above 2 px post-fit, no surface model will bring them inside the gate**, because the gate is already lost to corner quality before shape is considered. The limit would then be detection on a foreshortened, defocused, distorted frame, which is Phase 1 inheritance item 2 of `PHASE0-RESULTS.md` section 8, and the cone would be work aimed at the wrong target.
- **If they sit near or below it, the shape is the limit** and the cone is the right next step.

That is one table from data you already have, it costs nothing, and it decides whether the cone is worth building at all. Report it either way.

**If the answer says shape, build the cone**, on the same discipline as the cylinder: a general developable surface with a ruling direction that varies rather than a single angle, so it covers both the twist and the cone your shape test cannot separate. Synthetic truth first, swept until it breaks, then the seven mounted frames once, with the three flat frames as the control they have been throughout. Your F test on the two shapes a cylinder cannot take, at 9 to 77 on six mounted frames against 0.8 to 2.9 on the flat ones, is exactly the evidence entry 15 asked for before anyone built a cone, and it is a better test than the one I described.

**Stop after that.** If the general developable does not bring the mounted frames inside 0.005 in, report it and go to M2. Do not try a third surface model. The fallback is the piecewise registration already diagnosed in `PHASE0-RESULTS.md` section 4.5, and a mounted gate that needs more than a developable fit is a finding about the product rather than a modelling problem to keep pushing at. I would rather have a clean negative this week than a fourth model next week.

**Why before M2**, since M2 is unblocked and this is not: the mounted gate is the product requirement, Alan said so himself and entry 10 records it, and the paper protocol for next weekend has to say what photographs to ask for. Most of that protocol does not depend on this, and I am writing it now. The part that does is whether the photographs are the benchmark for a surface model that works, or evidence for one that does not, and that is worth knowing before he stands in front of a target board.

### 6. Everything else in that report

Entries 13, 14 and 15 actioned, 554 tests, 22 definitions with 0 errors, the geometry commit reviewed and merged from its own worktree, the merge conflict resolved by keeping both changes, and two questions raised that were not blocking and were raised anyway. That is the working pattern holding up under a week of being told to change direction, and it is worth saying so.

---

## 2026-09-14, entry 15: the crash is on ungated frames, the gate is already measured, and the lens is eating the bend

**Status: actioned 2026-09-14.** Section 3 is in `docs/PHASE1-RESULTS.md` M1.5, where the run shows the seeding was not the cause and the lens key puts two pixel geometries in one fit; section 4 is M1.7: the flat-fitted lens changes the mounted frames by 3 percent at most, and six of seven carry a shape no generalised cylinder takes. Originally: answers the M1.4 crash and the question at the end of it. Do this after entry 14's rule and alongside entry 13.

### 1. Your EXIF hypothesis is right, and here is the evidence from `photos.json`

| Frames | `focalLengthMm` | `fNumber` | `focalLength35mm` |
|---|---|---|---|
| `ultrawide1-3` | 2.2 | 2.2 | **13** |
| `20260913_130543`, `130550`, `130554`, `130559` | 2.2 | 2.2 | **23** |
| `main1-3`, `main_flat1-3` | 6.25 | 1.7 | 23 |
| `telephoto1-3` | 7 | 2.4 | 69 |

The four table photographs carry the ultrawide's physical focal length and the ultrawide's f-number, and the main camera's 35 mm equivalent. Two of the three tags say ultrawide and one says main, so the odd one out is the 35 mm equivalent and it is wrong. Grouping on `focalLengthMm` and `fNumber` correctly put all seven 2.2 mm frames in one joint fit; seeding the focal length per frame from `focalLength35mm` then started three of them at 2556 px and four at 1444 px for the same physical lens, a factor of 1.77 inside one fit. That is the degenerate start.

**The fix is a seeding rule, not a model change.** Within a joint-fit group there is one lens, so there is one starting estimate: take it from `focalLengthMm` and the sensor dimension, or from the group's median, and never per frame. Then cross-check the two EXIF fields and **warn when they disagree**, naming the frame.

**This is the DPI finding again, and it belongs next to it.** `SAMPLE-NOTES.md` already says of scanner DPI that the problem is not that metadata lies but that there is no way to know when it does without something of known size in the frame. Here there is something of known size in the frame, because it is a GroupLab target, so a recovered focal length that disagrees with EXIF by a factor of 1.77 is detectable rather than merely suspected. Say so in the results document: **the sheet validates its own EXIF**, and that is a property no generic photograph has.

### 2. The crash is on four frames that are not in any gate, and every gated frame finished

`photos.json` marks all four `20260913_1305xx` frames `photographGate: none`, `gated: false`. They are sheet 1 lying loose on a table, kept as a diagnostic, and `PHASE0-RESULTS.md` section 7 already records the decision not to count them as flat. The three that crashed are among them.

**So all seven mounted-gate frames and all three flat-control frames completed.** The mounted gate and its control are measured. What is missing is not the measurement but its raw rows, because the command aborted before writing `surface.json`, and I check reports against raw rows rather than against summaries.

**That changes the order.** You do not need to understand the degenerate fits before you can report. You need the run to survive them.

### 3. Do these, in this order

1. **Add the locator guard**, and treat it as a requirement rather than a workaround. A bull whose edge profile has fewer than three samples fails that bull, with a reason recorded in the trace, and the sheet carries on. A user will feed this application a photograph where registration goes wrong, and the answer has to be a named failure on one bull, not an exception that loses the other twenty-seven. `EdgeFitBullLocator` should have no input that throws.
2. **Fix the focal-length seeding** per section 1, with the disagreement warning.
3. **Re-run `grouplab surface frames`**, foreground, per entry 14. Everything should complete, `surface.json` should be written, and the four table frames either fit or fail with reasons.
4. **Write the M1 report.** Send it whatever the numbers are.
5. **Then** add the synthetic regression test: two frames with disagreeing focal-length starts in one joint fit. Build it to lock the fix, not to investigate the bug, because section 1 has already settled the cause.

### 4. The result that matters is not the crash

Worst scoring bull 0.012 to 0.092 in against a gate of 0.005, better than Phase 0 on five of seven frames and worse on two. The synthetic sweep put a one inch bow at 0.00040 in. Real frames are thirty to two hundred times worse than synthetic at bends the sweep says are comfortable, and **that gap is the finding**, not the pass or fail.

Two things say the machinery is sound, so the gap is somewhere specific. The F test declined the bend on all three flat frames and handed Phase 0's figures back unchanged, which is the control doing exactly what brief section 3.4 asked. And the surface keeps 66 to 120 corners where the planar model kept 25 to 90, so it is explaining real geometry rather than merely fitting noise.

**My first candidate is that the lens and the bend are competing for the same error, and `photos.json` already shows it happening.** Among the main camera's six frames, five fit a consistent lens and one does not:

| Frame | Flat or bent | Markers | k1 | k2 |
|---|---|---|---|---|
| `main_flat1` | flat | 34 | -0.0512 | +0.0659 |
| `main_flat2` | flat | **25** | -0.0436 | +0.0531 |
| `main_flat3` | flat | 23 | -0.0721 | +0.1058 |
| `main1` | bent | 34 | -0.0729 | +0.0770 |
| `main3` | bent | 27 | -0.0532 | +0.0699 |
| **`main2`** | **bent** | **26** | **-0.1597** | **+0.3165** |

`main2` fits a lens three to five times the rest of the same physical camera. It is not a marker-count effect: `main_flat2` has 25 markers, one fewer, and fits a normal lens. The difference is that `main_flat2` is flat and `main2` is bent. **On a bent sheet the radial lens term absorbs the bend**, because both are smooth and roughly radial over the marker region, and `main2` is one of the two frames the surface fit made worse. The same pattern shows on `telephoto1` and `telephoto3`, which are excluded for other reasons.

**So try freezing the lens.** It is a property of the camera and not of the frame. Fit one lens per camera on the frames where it is well determined, which is the flat frames and the full-marker frames, confirm it sits inside Phase 0's measured range of k1 -0.044 to -0.073 and k2 +0.053 to +0.106 for the main camera, then hold it fixed while fitting the bend. That removes a degeneracy rather than adding freedom, which is the same reasoning that chose a developable surface in the first place.

**Sweep what the synthetic run never swept.** The sweep in M1.2 varied bend, marker count, keystone and focal start, all at one noise level, 0.52 px per axis taken from `main_flat1`. It never varied noise, and it never varied the two together. At fixed bend, sweep corner noise from 0.5 px to 5 px, and sweep marker coverage that is clustered rather than random, with the lens free and with the lens frozen. If frozen-lens results hold where free-lens results fall apart, that settles it in one run.

**Twist is still the other candidate and I am not dropping it.** The sweep puts the break at a quarter inch, the frames are a sheet hanging from one pin, and that twists. The raw rows will tell you: a cylinder fitted to a twisted sheet leaves a residual that rotates systematically along the rulings. Look for that pattern in `surface.json` once it exists, and report it, before anyone builds a general developable surface. If it is twist and not the lens, the cone comes next; if both, the lens is still worth freezing first because it is cheaper and it is correct regardless.

### 5. Yes to entry 13, in parallel

Option A and the `CONTRIBUTING.md` rule are independent of all of this. Land them.

### 6. A correction from me

I told Alan the earlier run had been abandoned and that nothing was being computed. You have established that it was alive at ten and a half minutes and merely far too slow to finish. What I could see was that nothing had been **written**, and I stated it as though nothing was **running**. The conclusion happened to lead to the right action, since the run could not have finished and killing it was correct, but the inference was wider than the evidence and I should have said that a long job with no progress output is indistinguishable from a dead one. Which is, in the end, the better argument for entry 14's rule than the one entry 14 actually makes: **print progress not because it is tidy, but because without it nobody, including you, can tell a slow run from a stopped one.** Add that sentence to the `CONTRIBUTING.md` rule when you land it.

Caching the mapping's rotation and bend profile, holding it to 1e-6 px with a test, and stating that the numbers are unchanged, was the right response to finding it too slow.

---

## 2026-09-14, entry 14: the real-frame run is not running, and the turn that started it is the reason

**Status: actioned 2026-09-14.** `grouplab surface frames` ran in the foreground with a progress line per frame, its crash recorded at `ab42e33` and the completed run and M1 report at `d574a7e`, and the standing rule is in `CONTRIBUTING.md` under "Long-running steps" with entry 15 section 6's sentence appended.

**What the repository shows.** `scans/phase1/measurements/surface-synthetic.json` and `surface-rendered.json` are written. `scans/phase0/measurements/surface.json`, which `grouplab surface frames` produces, **does not exist**, and nothing under `scans/` or `docs/` has changed since `docs/PHASE1-RESULTS.md` was last written. Alan reports the session is idle. So the run that was described as "still going" is not going: it was started in a turn that then ended, and it went with the turn.

**This is the second time.** The same thing happened during Phase 0a, and Alan noticed it then too. It is worth fixing as a habit rather than as an incident, so the standing rule is at the bottom of this entry.

**Do this now.**

1. **Run `grouplab surface frames` in the foreground, in one turn, and wait for it.** Do not background it, do not schedule it, and do not end the turn expecting to be told it finished. Nine frames at 4000 by 3000, six starting ruling angles each, with detection, reclassification and a refit at each stage, is genuinely long. Long is fine. A turn that sits there for twenty minutes and returns a number is worth more than four turns that return a status.
2. **Print progress per frame as it goes**, one line each: frame name, markers matched, corners kept, fitted deflection, worst scoring bull, whether the F test kept the bend. If it dies part way, that output says which frame killed it, and it costs nothing.
3. **If it fails, report the failure and stop.** Do not retry it unchanged, and do not work around it by reducing the frame set or loosening the fit. A crash on a real frame after a clean synthetic sweep is itself a result and I want to see the error rather than a recovered run.
4. **Then M1 reports**, per the brief. That report is wanted as soon as it exists because the paper protocol depends on it.

**What I am expecting, so you know what would be surprising.** `PHASE1-RESULTS.md` M1.2 says the model holds to two inches of bow and a keystone of 0.70, and that what breaks it is a twist of a quarter inch or more, or markers lost from where the bend is. The nine pinned frames are a sheet hanging from a single pin, which is the geometry most likely to twist rather than bow, and `PHASE0-RESULTS.md` section 3a records that they keep only 25 to 90 of 104 to 136 corners. So a frame or two failing on twist would be consistent with what you have already measured, and would point at the general developable surface of brief section 3.1 rather than at anything being wrong. Report whichever way it comes out, per brief section 3.3, and do not adjust the model to improve a real-frame number.

**The standing rule, which belongs in `CONTRIBUTING.md` and should be added there in the same commit.**

> **Do not end a turn waiting to be notified that something finished.** If a step is long, run it in the foreground and wait for it in that turn, printing progress as it goes. If the tooling offers a real background mechanism with a handle you can poll, poll it in the same turn until it completes. A turn that ends while work is outstanding does not pause the work, it abandons it, and the session then reports progress that is not happening. The only correct reason to end a turn with work outstanding is a blocking question in `docs/QUESTIONS-FOR-PLANNING.md`, and that is a stop, not a wait.

**Everything else about how you are working is right**, and this is a mechanical fault rather than a judgement one. Measuring entry 11's change before making it, parking the blocked instruction and carrying on with the independent one, committing the surface fit at `d78c17c` before any real frame was run, and making both M1.3 changes on synthetic evidence with the reasons recorded, are all exactly right.

---

## 2026-09-14, entry 13: question 6 answered, option A, and entry 11 was wrong to bundle the promotion

**Status: actioned 2026-09-14.** Option A landed in one geometry commit after the roll check was measured first (at 1219 the half lattice still leaves both rolls' sighters 508 dmm below it, so 1142 stands): `GL-CF25-LTR` at gap 454 with 38 markers, `GL-CF25-100M-A4` on `grid-boundary-half-1` with 88, `GL-LR300-R24` and `GL-LR300-R36` on `grid-boundary-half-1` at gap 1142 with 113 and 151, `cells.sighterGap` declared on the three departures, test 26f an error, and TARGET-SCHEMA.md section 4 kept as printed with its parity test on the frozen definition.

**You are right and entry 11 was wrong.** It asked for one commit that fixed four sheets and promoted test 26f to an error, and it asserted that a sighter gap did the job on all four. Question 4's own finding 1 said otherwise a day earlier, in this file, and entry 9 accepted it: the outermost bull columns of `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` lie outside the lattice horizontally, and no sighter gap reaches a column. I carried the sighter fix forward and did not carry the column finding with it. Measuring the change before making it, and stopping, was the right call, and the margin table is the form this question should take.

**Take option A. The half lattice on the three sheets, in the one geometry commit.**

The commit, then:

| Sheet | Lattice | Sighter gap | Markers |
|---|---|---|---|
| `GL-CF25-LTR` | unchanged | 456 to **454** | 34 to 38 |
| `GL-CF25-100M-A4` | to `grid-boundary-half-1` | **unchanged at 480**, its sighters already bracket | 32 to 88 |
| `GL-LR300-R24` | to `grid-boundary-half-1` | 1219 to 1142, subject to the check below | 35 to 113 |
| `GL-LR300-R36` | to `grid-boundary-half-1` | 1219 to 1142, subject to the check below | 48 to 151 |

**Check whether the rolls still need the gap change at all.** Your table measures `grid-boundary-half-1` at gap 1142 and not at 1219, and the half lattice adds rows as well as columns. If it brackets the sighters at the conventional 1219, which is 1.2 times the pitch, then leave the gap alone: two fewer departures from the library convention, two fewer `cells.sighterGap` declarations, and two sheets whose geometry moves for one reason instead of two. Measure it before you change it. If 1219 does not bracket, 1142 stands.

**Why A, in the order the reasons matter.**

1. **`grid-boundary-half-1` is not a new scheme, it is the scheme that passed the paper gate.** The 300 yard tiles ship it, they were printed, scanned and measured in Phase 0, and they passed at 0.00325 in worst on tile 3. Option A commits three sheets nobody can print this cycle to a lattice that has already been through paper. That is the strongest argument and it is worth more than the counting arguments.
2. **Margin zero is already the accepted standard**, settled in entry 9: bracketing is inclusive, and the tiles conform at exactly zero. The outer columns becoming interpolated along the lattice edge rather than surrounded is the tiles' condition, not a new concession.
3. **Option B breaks a library requirement.** `GL-CF25-100M-A4` at 20 scoring bulls is below the 25 Alan asked for, and a third documented exception to buy a geometry fix is the wrong trade. Rejected.
4. **Option C is two geometry commits and two identifier changes on the rolls.** Entry 9 and entry 11 both ask for one, and the reason is that each change invalidates anything printed from the old geometry. Rejected, and see the deadline note below for why the pressure C relieves does not exist.
5. **`docs/PHASE1-RESULTS.md` section M1.2, measured today, is new evidence for A.** Your own marker-count sweep found that 23 markers spread across a sheet hold the fit and 23 at random do not, "because a random 23 can leave a region unconstrained". The three sheets needing the half lattice are the largest in the library, they bend most when mounted, and they currently carry the sparsest lattices relative to their size. A surface fit on a 300 yard roll is exactly where an unconstrained region is most likely and most costly.

**The counting arguments against A, weighed and not dismissed.** Tripling the markers puts more of them within a plausible miss of a bull, so on the 100 metre sheet at a 200 dmm half pitch some will be shot through. That is a gain rather than a loss: measurement 1 of Phase 0 showed the worst bull still improving above sixteen markers with 34 available, and M1.2 puts the failure point at sixteen or fewer, so 88 markers absorb damage that 32 would not. The ink is about two square inches of marker on an A4 sheet. The sheets will look busier, and that is a real change to how they look rather than only to how they measure; it is a change I am making on Alan's behalf and he can overrule it, which is why it is in his hands as well as yours.

**There is no print deadline on any of the three.** Alan has no plotter, so `GL-LR300-R24` and `GL-LR300-R36` cannot be printed here at all, and `GL-CF25-100M-A4` needs A4 stock he is unlikely to have. Nothing in next weekend's session prints them. `GL-CF25-LTR` is the only sheet of the four with a print date, and its fix is the uncontested one. So take the time to measure rather than to hurry, and if the roll check turns up something that argues against A, ask again rather than proceeding.

**Test 26f becomes an error in that same commit**, which entry 11 got right for the wrong reason: it works not because a sighter gap fixes four sheets, but because option A fixes the three the gap cannot reach.

**Section 4 and the parity test: your proposal, accepted.** Keep TARGET-SCHEMA.md section 4 as the as-printed `GL-CF25-LTR`, identifier `GL-YCSK-DZZ1-R0VJ-4T5Y` and gap 456, and re-point `Section4DocumentEncodesToTheReferenceSheetBytes` at `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, which is byte-identical to it. A worked example's job is to be a complete real document with a verifiable identifier, and the frozen file is permanent by construction where the live sheet is not; re-pointing it once is better than rewriting section 4 every time geometry moves.

**Add one sentence to section 4, which is prose and not schema.** A reader who meets `GL-YCSK-DZZ1-R0VJ-4T5Y` in section 4 and then finds a different identifier for `GL-CF25-LTR` in `TARGET-LIBRARY.md` will think one of them is wrong. Say that the example is the definition the Phase 0 sample set was printed from, that it is frozen at that path, which live sheet supersedes it, and that it is kept because an identifier hashes geometry, so a worked example that tracks the library would change with every geometry commit. That sentence is also the cleanest place in the whole specification to make the point that identifiers move when geometry does.

**Three corrections accepted, all three mine.**

1. `cells.sighterGap` is specified in TARGET-SCHEMA.md sections 3.6 and 7, not 3.10. Entry 11 is wrong.
2. `docs/TARGET-LIBRARY.md` carries no identifiers; the commit changes its marker counts. Entry 11 is wrong.
3. **`markerSize` is 8 modules, not 10**, per TARGET-SCHEMA.md section 3.7, the renderer and FIDUCIAL-DECISION.md's own 0.5 mm module in a 4.0 mm marker. `docs/PHASE1-BRIEF.md` section M0 and entry 12 both say ten, both counting the quiet zone the schema keeps separate, and both are wrong. The sweep sizes are 24, 32, 40, 48 and 64 dmm and your M0 table is the correct one. I will not edit the brief, because a brief is a record of what was asked; `PHASE1-RESULTS.md` M0 already carries the correction and the protocol will quote your numbers.

**The frozen fixtures landing while this was open was the right call**, and reproducing every measured value in `scans/phase0/measurements/` against them, with only detection times differing, is the check I would have asked for.

---

## 2026-09-14, entry 12: Phase 1 is briefed, and it is four milestones with no paper in any of them

**Status: open.**

`docs/PHASE1-BRIEF.md` is committed. Read it after finishing entry 11 and work the milestones in the order it gives. The short version, and the reasoning that is not in the brief:

**Alan cannot shoot until next weekend.** That is a week with no paper in it, and every one of the four things Phase 1 needs turns out to be unblocked without paper, so nothing is waiting on him. He was asked which of the four to build and chose all four.

**M0 first, and it is smaller than it looks.** The marker module sweep of `FIDUCIAL-DECISION.md` section 10 measurement 2 needed a renderer change in every previous plan, including the one I wrote yesterday. It does not. `fiducials.markerSize` is already a declared field with validation against `family`, and `tag36h11` is ten modules across, so the sweep is five definitions at `markerSize` 30, 40, 50, 60 and 80 dmm. An hour, not a day, and it clears the last thing standing between the paper protocol and being writable.

**M1 is first among the real milestones because its result has a deadline.** The paper protocol for next weekend has to say what photographs to take, and what to ask for depends on whether a developable fit works. If it works, the protocol asks for mounted frames that exercise it. If it fails, the protocol asks for something else, possibly including a second sheet in frame as a scale reference, which is a different thing to ask a person to do while they are at a range. So M1 reports as soon as it exists rather than at a convenient point.

**The focal-length trap in brief section 3.2 is the one I most expect to cost you a day**, so it is written out in full. A homography with a radial term never needed to know anything about the camera. A surface in three dimensions does. The EXIF has it, `photos.json` already carries it, and the estimate is about 2556 pixels for the main camera. If a frame has no focal-length tag it cannot use this path at all, and that limitation belongs in the report rather than being worked around.

**Brief section 3.3 is not advice.** Build the surface fit against synthetic bent sheets with known truth, sweep it until it breaks, and only then run the nine pinned frames, once. Your own decision-log line, "far-edge marker loss reported, not tuned against", is the same principle and it was the right call. Nine frames is a small enough set to fit a model to by accident, and the sweep is worth more than the pass or fail anyway, because it tells us how much bend the model survives rather than whether it survived these nine.

**M2 starts from something that already works, which is not how the last plan described it.** I told Alan yesterday that hole detection was the hardest unsolved problem in the project, and I was quoting `SCAN-MEASUREMENTS.md` section 8, the naive baselines, at 1 of 27 and 7 of 20. Section 3.1 of the same document already has a primitive that gets 25 of 27 with zero false positives. The correction matters because it changes what M2 is: not a search for a method, but a port of a known one, a baseline committed, and then the question of whether render-and-difference beats it. Render-and-difference cannot be tested on any existing file, because none of them is a GroupLab target, which is why brief section 4.3 builds synthetic ones from the survey's own measured hole statistics.

**M3 is the statistics engine, pulled forward from Phase 2**, because `STATISTICS.md` section 15 is a complete build specification and needs nothing from anybody. Two things in it were open and I have answered both in brief section 5 so they do not come back as questions: apply `c4` to the interval endpoints and match shotGroups, and make mean radius the headline with sigma beneath and extreme spread subordinate. The Monte Carlo tables want hours of compute; start them early and build something else while they run.

**M4 is the application shell**, and the part of it that is not negotiable is the manual assignment interface, `DESIGN.md` section 13. The survey found hand-drawn arrows on real targets recording the shooter's own cross-cell assignment, which no geometric rule recovers. Everything else in M4 is scope that can be cut.

**One thing to send back early rather than at the end.** The paper protocol has a print deadline. Anything you want from next weekend's session, extra sheets, a particular mounting, a photograph taken a particular way, goes into `docs/QUESTIONS-FOR-PLANNING.md` the moment you know you want it. A request that arrives after the protocol is printed costs a full week, because the next session after next weekend is the one after that.

---

## 2026-09-14, entry 11: Phase 0 closes, the geometry commit lands, and the printed definitions become fixtures

**Status: actioned 2026-09-14.** The frozen fixtures landed while question 6 was open, and the geometry commit landed as entry 13's option A, which records `GL-20J3-Y141-0BN3-EYME` as superseding `GL-YCSK-DZZ1-R0VJ-4T5Y` in `targets/frozen/phase0/README.md` and `docs/PHASE0-RESULTS.md` section 8.

**Phase 0 is closed on the verdict in `docs/PHASE0-RESULTS.md` section 8, as written.** I checked its arithmetic against `scans/phase0/measurements/photos.json` rather than taking it, the way entry 7 exists for. Every figure in section 3b reproduces from the raw rows: marker counts 34, 25 and 23; corners kept 136 of 136, 100 of 100 and 91 of 92; residual RMS 0.00270, 0.00315 and 0.00395 in over kept corners and 0.00424 in over all 92 on `main_flat3`; bull means 0.00187, 0.00270 and 0.00392 in; worst scoring bulls 0.00343 at 21, 0.00566 at 13 and 0.01183 at 1; scoring bulls over the gate 0, 2 and 6 of 25; worst sighters 0.00661, 0.01016 and 0.00496 in at S1. I also re-applied each frame's stored `mapping` to its own corner image points and recovered the stored per-corner error to better than 0.0002 dmm on all 328 corners, so the transforms in the file are the transforms the numbers came from and not a summary written beside them.

**I checked the claim that carries the verdict, and it holds.** Section 3b says every failure other than a sighter is a bull outside the markers that decoded, or within a thousandth of the gate. Taking the convex hull of the decoded corner page positions on each frame: on `main_flat1` the only failing bull, S1, is outside it. On `main_flat2` the missing markers take the decoded x extent from 2050 down to 1670 dmm, the far right side, and S1 and S2 are outside the hull, leaving 13 and 18 inside it and over the gate by 0.00066 and 0.00009 in. On `main_flat3` the decoded y extent starts at 709 instead of 329, the top rows, and bulls 1, 2, 4 and 5 are outside the hull, leaving 6 and 13 inside it and over by 0.00029 and 0.00026 in. Four bulls inside the lattice fail, by 0.0001 to 0.0007 in, against a gate that has 0.008 in of hole-centroid noise underneath it. That is not a registration failure. It is the gate resolving finer than the thing it will eventually measure, which is what a control is for.

**So the flat gate fails three of three and none of the three failures is unexplained.** One is the sighter geometry, already found, costed and fixed on paper in section 4.4. Two are the far-edge defocus of an f/1.7 lens on a foreshortened sheet, which is a detection task and belongs to Phase 1. The remainder is four bulls at the width of the noise floor. Section 8 says exactly this and I am not asking you to soften it. A gate that fails for reasons you can name is worth more than a gate that passes because it was tuned, and your decision not to tune detection against these three frames is the right one and is on the record in the decision log.

**The geometry commit lands now.** `GL-CF25-LTR` sighter gap 456 to 454, `GL-LR300-R24` and `GL-LR300-R36` 1219 to 1142, `GL-CF25-100M-A4` per the sweep, `cells.sighterGap` declared on each, test 26f promoted from warning to error, `docs/TARGET-LIBRARY.md` regenerated with the new identifiers. `cells.sighterGap` is already in `docs/TARGET-SCHEMA.md` at sections 3.10 and 7 and in the schema at test 23, so this commit changes definitions and one test severity and does not change the schema.

**It lands without a verification print, and that is deliberate.** Whether a sheet whose lattice brackets its sighters passes the flat gate is not measured and cannot be measured without paper. Printing one sheet now to answer that, and then printing again for Phase 1, is two paper sessions where one will do, and Alan has been explicit that manual work is batched. The verification print folds into the single Phase 1 paper session, alongside the shot targets, the mounted photograph set and the marker module sweep. Until then section 8's sentence stands: it is not measured.

**One thing the commit must carry that is not yet written down anywhere.** The geometry commit gives four sheets new identifiers, and the sample set in `scans/phase0/` was printed from the old ones. The moment it lands, `grouplab spike sheets` and every other spike command either fails to resolve the definition the paper was printed from, or silently measures the scans against geometry that moved by 1 dmm, and every table in `docs/PHASE0-RESULTS.md` stops reproducing. The document's own second line promises those commands reproduce every table. Fix it in the same commit:

1. Freeze the printed definitions as fixtures, canonical GLTD-J bodies under a path such as `targets/frozen/phase0/`, each named by its identifier as printed, with a short README saying these are the geometry the Phase 0 sample set was printed from, that they are inputs to a measurement and are never edited, and which live definition superseded each one.
2. Make the Phase 0 scan metadata and the spike commands resolve against the frozen fixtures rather than the live library, so every table in `PHASE0-RESULTS.md` still reproduces after the commit.
3. Add a line to `docs/PHASE0-RESULTS.md` section 8 saying the tables are measured against the frozen definitions, naming the path and the superseding identifiers.
4. Check the fixtures still load and validate against the current schema, and if a future schema change ever breaks one, that is a finding and not a fixture to edit.

Do the same for any definition a future sample set is printed from. A measurement against paper is only reproducible while the geometry it was printed from still exists in the tree, and a definition identifier changes whenever its geometry does, which is the point of the identifier.

**What happens after the geometry commit.** Report on it, then stop and wait. Phase 1 has a brief to write and a paper session to plan, and neither is yours to start. The four items in section 8's inheritance list are the right list and their order is right.

---

## 2026-09-14, entry 10: the flat control set, and the photograph gate now has two halves

**Status: actioned 2026-09-14.** The flat gate is run on `main_flat1-3` and fails three of three; the frame with every marker passes every scoring bull and fails only on S1. `docs/PHASE0-RESULTS.md` section 3b has the result, section 4.5 the developable-surface requirement with piecewise registration as fallback, and section 8 the Phase 0 verdict. DESIGN.md section 21 [r5] is committed with one figure corrected: the 0.006 to 0.011 in it quoted was a different sheet and lens, and is now the same sheet and lens lying flat, 0.0066 to 0.0118 in.

**Three flat frames are committed**, `scans/phase0/main_flat1.jpg` through `main_flat3.jpg`. Sheet 3, the clean control, lying flat, photographed with the main camera: f/1.7 at 6.25 mm, 23 mm equivalent, which matches `main1-3` and confirms the lens. All three are camera originals at 4000 by 3000 with intact EXIF and a fine quantisation table.

| Frame | Markers | Keystone |
|---|---|---|
| `main_flat1` | **34 of 34** | 0.997, square on |
| `main_flat2` | 29 of 34 | 0.926 |
| `main_flat3` | 23 of 34 | **0.802**, the most off-axis frame in the corpus |

No glass was available, so these are a sheet lying flat rather than mechanically restrained. They are the flattest photographs in the set and they are the control the gate needs. `main_flat3` losing eleven markers at a 0.802 keystone is a result in its own right and worth reporting rather than treating as a bad frame.

**Run the photograph gate on these three and close Phase 0 on the result.** No further photographs are being requested.

**The gate itself has changed, and the change is more than bookkeeping.** DESIGN.md section 21 now carries two photograph gates instead of one, marked `[r5]`:

- **Flat**, 0.005 in worst bull, on a sheet held flat. This is Phase 0's, and these three frames are what it is measured on.
- **Mounted**, 0.005 in worst bull, on a sheet mounted the way a shooter mounts it. This is **Phase 1's, and it is expected to fail until a surface model exists.**

The reasoning, which came from Alan and is right: a target stapled to a board is the application's actual input. It bows between its fixings, curls at a free edge and moves in wind. A sheet held flat is not a user scenario at all. So the mounted case is the product requirement and the flat case is a control that isolates the lens and the estimator, which is the reverse of how entry 8 framed it.

**The model that mounted case needs is already named in this project's own documents.** DESIGN.md section 6: **paper is a developable surface.** It bends without stretching, so distance along the sheet is preserved even where the projection is not planar. That is a far stronger constraint than a generic warp or a patchwork of local homographies, and it is what Phase 1 should build on rather than piecewise registration. Your own evidence supports it from the other direction: you showed that three radial coefficients with a free centre, and quadratic and cubic warps evaluated leave-one-marker-out, all fail to bring any pinned frame inside the gate. A model with more freedom is not what is missing. A model with the right constraint is.

Record that in `docs/PHASE0-RESULTS.md` where the Phase 1 requirement currently sits, and revise it from "local or piecewise registration from nearby markers" to a developable-surface fit, with piecewise registration noted as the fallback if the constrained fit proves impractical. The nine pinned frames are its benchmark and the figures to beat are in your section 3a.

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
