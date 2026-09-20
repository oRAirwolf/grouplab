## 2026-09-20, entry 115: questions 26 and 27 answered, chronograph strings by hand, and the paths a real sheet takes when something is wrong

**Status: open.**

**Do entry 114 first.** Then this, in section order. Alan is at the range for several hours and cannot answer anything. **Do not stop the run for a question:** write it in `docs/QUESTIONS-FOR-PLANNING.md`, build what does not depend on it, and carry on. Commit and push at every clean point.

### 1. Question 26 answered: the Ballistics slot stays

The concept's rail was drawn before the solver existed, and a dope table belongs to a rifle and a load rather than to any sheet. **Keep the Ballistics slot.** Seven buttons including the gear is not a crowded rail. If it ever gets crowded, the answer is grouping, not hiding a feature inside a screen it does not belong to.

### 2. Question 27 answered: a load per bull, set on more than one bull at once

**Your recommendation is right, with one addition.** A ladder sheet is usually five bulls to a load, so setting them one at a time is five times the work it should be.
- **A load field in the editor's selected-bull panel**, as you propose.
- **Selecting several bulls sets them together.** Whatever the editor's existing way of selecting more than one is, the load field applies to all of them.
- **The subgroups follow from the field:** bulls sharing a load are one subgroup, and the analysis compares them as `GroupComparison` already can.
- **A bull with no load set belongs to no subgroup**, and the screen says how many bulls are unassigned rather than inventing a default.
- **It is stored with the marking and in the session**, so reopening a session keeps the subgroups.
- **The load names come from the records**, so a load typed twice is not two loads.

### 3. Chronograph strings, entered by hand, and the reconciliation `DESIGN.md` section 15 requires

**Garmin Xero import waits for a sample file, but nothing else does.** Entry 112 gave the schema its tables for chronograph strings and their mapping. The interface on top of them needs no Xero file at all, and once it exists, reading a Xero export is a thin reader rather than a feature.

- **Enter a string of velocities by hand** for a session: paste or type a list, with a name and the date.
- **Reconcile it against the shots, never by position alone.** Section 15 is explicit: "the shot sequence and the chronograph sequence are separate ordered lists that get reconciled, never assumed to align. Chronographs drop shots, record a neighbour's shot from the adjacent bench, and log the fouling round fired into the berm."
  - When the counts agree, offer the in-order mapping **as a proposal the person accepts**, not as a fact.
  - When they disagree, show both lists side by side and let the person mark a reading as belonging to no shot, or a shot as having no reading.
  - **Nothing downstream may assume every shot has a velocity.**
- **What it is for, once it exists:** the velocity spread that entry 113 section 3's hit probability already takes as an input, and the velocity regression Phase 5 names. Feed the measured standard deviation in rather than making the person type it, and say where it came from.
- **Tests:** equal counts, one reading missing, one extra reading, and a session with no string at all.

### 4. What happens when the sheet is not perfect

**Yesterday proved this matters.** A sheet printed without its codes, or scanned crooked, or photographed at the wrong size is what real use looks like. Each of these should end with the person knowing what to do.

- **No codes on the sheet, or unreadable ones.** Registration runs off the markers, so the analysis can proceed once the person says which sheet it is. **Check that path works and that the screen offers it plainly**, rather than failing with the identity as the reason. Alan's sheets today may have no codes at all.
- **A scan that is rotated or upside down.** The markers carry orientation, so this should just work. **Test it:** take a committed scan, rotate it by 90, 180 and 270 degrees, and check the analysis returns the same hole positions on the sheet to within the gate's tolerance.
- **The messages when it cannot proceed**, each saying what to do next rather than what failed: too few markers found, the resolution too low for the sheet, the image not of the sheet that was chosen, the sheet printed at the wrong scale. **The scale one matters most**: the detector already measures print scale, so a sheet printed at 97 percent should be named as such, with the figure, not analysed silently.
- **Tests for each message**, driven by an image that provokes it.

### 5. How long an analysis takes

**Measure it and write it down**, on a 600 dpi Letter scan and on a phone photograph, on this machine: total, and the three or four slowest stages. No target to hit yet. **It is a number we do not have**, and Alan is about to put a folder of sheets through it, so it is worth knowing before rather than after.

### 6. A corrected ballistics.js for Alan's website, as a file he can choose to upload

**Not a change to his website.** Produce a file; he decides what to do with it.

Entry 110 and questions 24 and 25 found five faults in `reference/ballistics-js/ballistics.js`, which is the file his website serves today: the G1 table, the shooting angle, aerodynamic jump, the Coriolis vertical sign, and the wind-direction convention. **GroupLab's port is now correct and validated against an independent solver.**

- **Write `reference/ballistics-js/ballistics.corrected.js`**, the same file with those five faults fixed and nothing else changed: same names, same call signature, same returned fields, so the website's pages keep working untouched.
- **Check it against GroupLab's own solver**, running it under Node in the same comparison the transcription check already uses, and require it to agree with the C# within the same tolerances.
- **A short note beside it** listing the five changes in plain words, so Alan can see what he would be uploading.
- **Do not change anything else about it**, however tempting, and do not attempt to reach his server.

### 7. If there is still time

- **The support link** needs a page address Alan has not given, so raise it as a question and build nothing.
- **Anything you find in passing that is wrong**, in the manner of the "Roll24" fix: fix it, and list it.
