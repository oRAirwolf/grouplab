# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

**How entries arrive, from entry 44 on.**
- **Delivery:** the planning session delivers each new entry as its own file in `docs/notes/inbox/`, named `entry-NN.md`, and never writes this log or any other existing file.
- **The only writer:** the Claude Code session is the only writer of this log.
- **Actioning includes three steps:** folding the entry into the top of the log, setting its status, and deleting its inbox file.
- **Several files waiting:** fold them in ascending entry number, so the newest ends up first.
- **Why:** two writers rewriting one file with no locking overwrote this log once, and separate paths cannot collide.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## 2026-09-20, entry 116: an installer and a zip somebody else can download and run, with no GitHub account and no .NET

**Status: actioned 2026-09-20**, sections 1 to 5. **Section 2a's installer is built and has never been built or run**, because Inno Setup is not installed on this machine and nothing was installed on it to find out; the release workflow requires it and fails without it. Question 29 was raised and nothing waited on it.
- **Section 1:** `scripts/package-windows.ps1` publishes GroupLab self-contained for win-x64 as a folder rather than a single file, so the OpenCV native library sits where the loader expects it and nothing unpacks itself on first run. 120 MB zipped, 311 MB and 270 files unpacked, named by version and commit and again under a stable name. Tested with no .NET: `dotnet` off `PATH` and `DOTNET_ROOT` pointed at an empty folder, the package's own `grouplab.exe` analysed the sample it generates. The script makes that check itself, and CI runs the script on every push.
- **Section 2:** two samples, both allowed. Alan's own unshot 300 dpi scan, already public here, and a 25 shot sheet generated at packaging time by the new `grouplab sample`. Nothing donated, nothing without a consent record, and a test holds the script to the one image it may copy. The repository has no shot sheet that may be published, which is question 29.
- **Section 2a:** an Inno Setup script, per user, no administrator rights, Start menu, Add or remove programs, an uninstall that leaves `%APPDATA%\GroupLab` alone and says so. Built, not proven: `ISCC.exe` was never run here, and `-RequireInstaller` makes a release fail rather than ship without it.
- **Section 3:** `.github/workflows/release.yml`, on a tag and by hand, where a run by hand makes a draft. Every asset twice, versioned and stable, so the README's links keep working with nobody editing the README. A test holds those links to the names the workflow and the script write.
- **Section 4:** the SmartScreen wording, the antivirus warning, where GroupLab writes, how to remove it and how to report a problem, in the same plain words in `README.txt` and in the README's Download section, each held by a test.
- **Section 5:** `docs/TESTING-GUIDE.md` and its PDF, one page for somebody who has never seen GroupLab, with what is not done taken from the README's Planned section. Nothing holds the two together, which is named in the results.
- `docs/PHASE1-RESULTS.md` "Entry 116". After entries 114 and 115.

**Alan wants other people to try GroupLab.** Today nobody can, unless he builds it himself and hands them a file. CI builds a Linux tarball as a workflow artifact, which only a signed-in GitHub user can download and which expires in 30 days. **There is no Windows package at all, and the audience is Windows shooters.**

### 1. A Windows package that runs on a machine with nothing installed

- **Self-contained, win-x64**, so no .NET runtime is needed. **Check the native OpenCV libraries survive** whatever packaging is chosen: `GroupLab.Cli` carries the OpenCV native runtime, and a single-file publish needs `IncludeNativeLibrariesForSelfExtract` or it fails at the first analysis rather than at start-up. **Test the package on a machine, or a container, with no .NET installed**, and say how you tested it.
- **A zip** holding the executable, `LICENSE`, `THIRD-PARTY-NOTICES.md`, and a short `README.txt`. A single loose executable is worse: people lose the licence and the instructions.
- **Name it with the version and the commit**, for example `grouplab-0.1.0-win-x64-<short sha>.zip`, so a bug report names a build.
- **GPL-3.0 section 4 travels with it.** The zip carries the licence, and `README.txt` names the exact commit and the public repository it was built from.

### 2. Samples in the zip, so a tester can do something in the first minute

A tester with no printer, no scanner and no rifle should still be able to open GroupLab, analyse a sheet and read the figures.
- **A `samples` folder** with one or two scans **already committed in the public repository**, so nothing new is published, and the sheet definitions they belong to.
- **Nothing from `scans/` that is not already public, and nothing donated.** The friend's scan has no consent record and must not be in it.
- **`README.txt` says which sample to open and what to press.**

### 2a. An installer as well as the zip

**Alan asked whether people will be able to download a Windows installer. The zip alone is not that**, so add one.
- **An installer that a shooter recognises:** a `setup.exe` that installs into the user's own profile with no administrator rights, puts GroupLab in the Start menu, and appears in Add or remove programs with a working uninstall. **Inno Setup** builds this, is free, and runs on the Windows CI runner; if you prefer WiX or another tool, say why. **The build tool's own licence does not touch GroupLab's**, since nothing of it is linked into the application.
- **The uninstall removes the program and leaves the person's data alone**, and says so: sessions, settings and logs stay in AppData until the person deletes them. Name that folder in the finish page or the README.
- **Both assets ship:** the installer for people who want one, and the zip for people who would rather unzip and run. **The zip stays the one this project tests**, because it is what the automated checks produce.
- **Unsigned either way.** An installer does not remove the SmartScreen warning; it moves it to the setup file. Say that in the same plain words section 4 uses. **The only real fix is a signed build**, which `DESIGN.md` section 20 already plans through the Microsoft Store's one-off fee, and that is not this entry.

### 3. Releases anyone can download

- **A release workflow**, triggered by a tag **and runnable by hand from the Actions tab**, that builds the Windows zip, the installer and the Linux tarball, and attaches all three to a GitHub release. A run by hand makes a draft release, so a test build can be checked before anyone sees it. **A GitHub release is a plain download link: no account, no sign-in.** That is the answer to "no GitHub".
- **The macOS build stays out** until somebody can run it: the gate record passes there, but nobody has opened the application on a Mac.
- **The README gains a Download section** at the top, and **its links must keep working without anyone editing the README after a release.** GitHub serves a fixed address for the newest release's assets, `https://github.com/oRAirwolf/grouplab/releases/latest/download/<asset name>`, so:
  - **every release attaches a copy of each asset under a stable name** with no version or commit in it, for example `grouplab-setup-win-x64.exe`, `grouplab-win-x64.zip` and `grouplab-linux-x64.tar.gz`, beside the versioned copies section 1 names;
  - **the README links to those stable addresses**, plus one link to the releases page for older builds;
  - **a test holds the README's links to the asset names the workflow attaches**, so renaming an asset fails the build rather than leaving a dead link on the front page. That is the same rule the README's other tests already follow.
  - **The Download section says what the build is:** an unsigned test build, what Windows will say, what to click, and that the newest one is whatever the link gives, with the releases page for the rest.
- **Keep the 30-day CI artifact as it is**, for Alan's own testing between releases.

### 4. What a tester will hit, and what to say about it

- **SmartScreen.** The executable is unsigned, so Windows says "Windows protected your PC". The person clicks "More info" and then "Run anyway". Say so plainly in `README.txt` and in the README's Download section, **with no reassurance beyond what is true**: it is unsigned because signing costs money the project has not spent, and the source is public at the named commit.
- **Antivirus** may quarantine an unsigned single-file executable. Mention it.
- **Where GroupLab writes:** its settings, database and logs under the user's own AppData, and nothing else. Say where, so somebody can remove it cleanly. **Include an uninstall paragraph**: delete the folder, delete that AppData folder.
- **What to send back when something is wrong:** the report package the Diagnostics screen already produces, which carries no location data.

### 5. A one-page tester's sheet

**`docs/TESTING-GUIDE.md`, and its PDF**, alongside the user guide, aimed at somebody who has never seen GroupLab:
- what it is, in three sentences;
- download, unzip, run, and the SmartScreen step;
- open a sample and read the analysis;
- print a sheet and shoot it, in short, pointing at the volunteer pack for the detail;
- what is unfinished today, honestly: the gates not yet met, and that Linux and macOS are built but unused;
- how to report a problem.

**Do not overstate what works.** The README's Planned section is the authority on states, and this page must not contradict it.

---

## 2026-09-20, entry 115: questions 26 and 27 answered, chronograph strings by hand, and the paths a real sheet takes when something is wrong

**Status: actioned 2026-09-20**, sections 1 to 7. Questions 28 and 29 were raised and nothing waited on them.
- **Section 1:** question 26 answered, the Ballistics slot stays, nothing changed.
- **Section 2:** question 27 answered and built. With the select tool, shift and click chooses bulls on the sheet, and one field sets a load on all of them from the records' loads. Shift rather than a plain click, because every bull on a shot sheet has a hole on it and a plain click there is the hole. The bulls sharing a load are a subgroup, a bull with no load belongs to none and the panel says how many those are, and the marking keeps it.
- **Section 3:** a string of velocities entered by hand for a session, reconciled with the shots rather than assumed to line up with them: the in-order pairing is a proposal, a reading of no shot or a shot with no reading is marked and the rest pair in order, and what is accepted is kept on the session with its mapping. The readings' spread becomes the load's velocity SD with a note of where it came from, which needed schema version 2 and an upgrade that runs when an older database is opened.
- **Section 4:** a sheet whose codes cannot be read is offered by name from the library rather than refused; a scan turned 90, 180 or 270 degrees gives the same holes to within 0.02 in; and the four messages each say what to do next, driven by an image that provokes them. Two findings: the detector copes down to about 75 dpi, so the low-resolution message is keyed at 120 rather than guessed, and a sheet read as the wrong definition used to give numbers with nothing said, which is now doubted out loud with its evidence.
- **Section 5:** measured on this machine. A 600 dpi Letter scan takes 17.7 to 22.8 s, of which identification is 9.7 to 13.7 s; the same sheet at 300 dpi takes 2.6 s; a phone photograph takes 4.2 to 4.4 s. Identification grows about twentyfold between 300 and 600 dpi while the image grows fourfold. Nothing was optimised.
- **Section 6:** `reference/ballistics-js/ballistics.corrected.js` with the five faults put right and nothing else changed, `CORRECTIONS.md` beside it, and a test that holds it to GroupLab's own solver under the committed tolerances. This machine has no Node, so the comparison runs on CI, where the test fails rather than skips if its rows are missing. Nothing was sent anywhere.
- **Section 7:** the support link is question 28 and nothing was built for it. Found in passing and fixed: a sheet read as the wrong definition gave numbers silently. Found and recorded, not changed: identification's cost at 600 dpi.
- `docs/PHASE1-RESULTS.md` "Entry 115".

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

---

## 2026-09-20, entry 114: the in-app print path drops every rectangle, and a hole that is not detected

**Status: actioned 2026-09-20**, section 1 only. **Section 2 is not done**: the scan it names is not on this machine, and the entry says to stop in that case. Section 3 is a record and needed nothing built.
- **Section 1:** the cause was `FillRect`, a pattern blit, for every filled rectangle, where the disc bands and text were drawing calls. Microsoft Print to PDF honoured it and the Brother MFC-J430W dropped every one, which is why the markers, codes and load block rules were missing and the rings and text were not. Both drivers report the same RASTERCAPS, so the capability bits do not tell them apart. Every filled shape is now a closed path filled with `FillPath`, proven by printing the sheet to a file through both drivers with its rectangles and without them: the Brother wrote the same 612,632 bytes either way before the fix and 919,905 against 612,632 after it. A page the driver will not draw in full is no longer committed. `PrintedItemsTests` makes both comparisons permanent, and they were checked by putting the fault back. "Open to print" is now the primary on the print screen, with the reason beside it, until a sheet from the fixed path has been looked at on paper.
- **Section 2:** not done. `C:\Dev\grouplab-range-2026-09-20\sheet1\sheet1-scan.png` does not exist; that folder holds only `grouplab-range-day-2026-09-20.pdf`. Nothing was read from it, nothing was added to the corpus, and no threshold was changed.
- **Section 3:** recorded as stated: one sheet, nine shots, scanned flat, no photographs; the mounted photograph, 25-shot editor and blank-paper gates have nothing.
- `docs/PHASE1-RESULTS.md` "Entry 114". **Do these before anything else.** Alan is going back to the range today, and the first one cost him most of yesterday's trip.

### 1. Every rectangle is missing from sheets printed through the application

**Alan photographed a sheet that the in-app Windows print path produced, and it is worse than "the QR codes are missing".** Comparing that photograph with the scan of a sheet printed the old way, through Save PDF and a viewer:

| Drawn as | On the paper |
|---|---|
| Bull rings and centre dots | **present** |
| Bull numbers, the title, the identifier, the load block's field labels | **present** |
| The two QR codes | **absent** |
| **Every AprilTag marker, all 38 of them** | **absent** |
| The load block's box and its dividing rules | **absent** |
| The printed sheet's paper size and layout | correct, and the text is in the right places |

**Everything the scene draws as a filled rectangle is missing. Everything drawn as a circle or as text is there.** Question 21's plan names exactly those three kinds: "Bull bands: GDI paths from two ellipses. Rectangles: markers, codes and rules, each a GDI rectangle. Text: set in Arial."

**This is not a cosmetic fault. A sheet with no markers cannot be registered or measured at all**, and it looks normal until it comes back from the range. Alan lost most of a range trip to it, and he lives over an hour away.

**The other half of the evidence:** entry 107 section 2's printed-size test prints through the same path to "Microsoft Print to PDF" and finds all 38 markers within 0.015 mm. **So the rectangles reach that driver and not the Brother.** The fault is in how they are drawn, in a way one driver tolerates and another does not, not in whether they are drawn at all. Look at the brush, the pen and the raster capabilities: `GetDeviceCaps` with `RASTERCAPS`, whether a null pen with a solid brush is being relied on, and whether anything goes through a bitmap or a raster operation rather than a drawing call.

**What to do.**
- **Find the cause and say what it was.** The pattern above is the evidence; do not stop at "it works on my printer".
- **Prefer a drawing call every driver must support.** A filled rectangle drawn as a closed path or polygon is honoured by every printer driver; raster operations are not.
- **Then prove it on more than one driver.** Print every built-in sheet through the in-app path to at least two different printer drivers available on the machine, and compare each against the same sheet from Save PDF: the count and position of every code, marker, ring, rule and text item. **That comparison becomes a permanent test.**
- **A sheet that cannot be verified must not be printed silently.** Consider what the application can check before it commits a page: it knows how many markers the scene holds.
- **Alan is printing through Save PDF today**, which is unaffected. **Until this is fixed and he has confirmed a real print, the in-app Print button must not be the one people reach for**: make it say so, or disable it, your judgement, and say which you chose.

### 2. A hole on Alan's scan is not detected

**The sheet:** 9 shots on `GL-CF25-LTR-D`, scanned flat. **Alan will put it at `C:\Dev\grouplab-range-2026-09-20\sheet1\sheet1-scan.png`.** If it is not there when you read this, do section 1 and stop.

**What he reports:** the shot above bull 2 is not picked up. On the scan, that hole sits high and left of its bull, above the row of small markers that runs above bulls 1 to 5, near the top edge of the sheet. Every shot on that sheet landed high and left, so several holes sit outside their bull's outer ring, and this one sits furthest out.

**Find where it is lost, and report before changing anything.** Run the analysis with detailed logging and say which stage drops it:
- outside the region the detector looks at;
- inside a printed-matter exclusion zone, one of the markers or codes;
- refused by a size, shape or solidity filter;
- found but left unassigned and then not shown.

**The requirement, whichever it is:** a hole anywhere on the paper is a shot the person fired. It must be found and offered, even with no bull near it. `ReviewQueue` already has an item for a shot with no bull, and that is where a hole like this belongs. **A hole that is silently dropped is the one failure mode the Phase 1 gate exists to prevent**, and it would count against the 99 percent.

**If the fix is a one-line threshold or region change, make it, with a test using this scan's hole.** If it is a design question, for example how far outside the marker lattice the detector should look, raise it in `docs/QUESTIONS-FOR-PLANNING.md` with the measurement and build nothing.

**Add the scan to the corpus only through the intake tool**, as with any image, and only if the intake tool is happy. It is Alan's own sheet, so consent is not in question, but location data and the process are.

### 3. What yesterday actually produced

For the record, so the gates are not thought to have material they do not have: one sheet, nine shots, scanned flat, no photographs. **The mounted photograph gate, the 25-shot editor gate and the blank-paper gate all still have nothing.** He is going back today.

---

## 2026-09-19, entry 113: the queue after entry 112, for a long unattended run

**Status: actioned 2026-09-19**, sections 1 to 8, after entry 112. Two questions were raised and not waited on: 26, the Ballistics screen's rail slot, and 27, nothing on screen assigning bulls to subgroups.
- **Section 1:** the sheet's thumbnail from its definition, where a click on a bull selects its shots, and the full CEP table and bivariate fit behind a remembered disclosure. The renders are regenerated and the README's analysis line is Done.
- **Section 2:** load comparison on the rail's chart slot, from sessions chosen in Session records or a sheet's subgroups. It shows plots, figures with intervals, and tests with verdicts and what each could have detected. It never ranks by point estimate, and says the data do not separate loads whose intervals overlap.
- **Section 3:** hit probability at distance and distance normalisation as specified, with the entry's four tests. On screen it is labelled a prediction.
- **Section 4:** `grouplab compare-photos`, tested on a synthetic scan and photograph. The doubles sheet: the queue raises every pushed shot, and a Shots per bull setting now reads it by nearest bull or with two on named bulls, both paths tested. The blank sheet stays Not started.
- **Section 5:** the volunteer pack, the sheet and one page generated from `docs/VOLUNTEER-PACK.md`, from "Print a volunteer pack" on the print screen.
- **Section 6:** `docs/USER-GUIDE.md` and its PDF, illustrated with the renders, with a test that every screenshot it names exists.
- **Section 7:** the enum-name test extended to every new screen and the report; a theme and keyboard test over every new screen in all four themes; renders of every new screen.
- **Section 8:** this record. `docs/PHASE1-RESULTS.md` "Entry 112" and "Entry 113".

**Alan is asleep and then at the range.** Nobody will answer a question for about twelve hours. **Do entry 112 completely first, then this entry, in the order of its sections.** The rule for the whole run: when a design question comes up that neither entry answers, write it in `docs/QUESTIONS-FOR-PLANNING.md`, build everything that does not depend on the answer, and move to the next section. **Do not stop the run for a question.** Commit and push at every clean point, so a stop at any hour leaves main green and nothing half-built.

### 1. The analysis screen's three unbuilt parts

The README lists them as not built: the sheet's thumbnail, and the full CEP table and bivariate fit behind a link.
- **The thumbnail** is the concept's top-left panel: the sheet drawn small from its definition, every shot on it, and a click on a bull selects its shot. It is drawn from the definition, not the photograph, as `DESIGN.md` section 18's archived view is.
- **The full CEP table and bivariate fit** go behind the link `DESIGN.md` section 19 describes, "one click away in a panel that remembers it was opened". Everything in it already exists in the engine. It is layout, not new statistics.
- **Regenerate the renders**, and move the README line to Done if nothing on it remains.

### 2. Comparing loads

`docs/figures/screens/compare-loads.png` is the concept. `GroupComparison` already holds the rank and dispersion tests, MANOVA and the dispersion ratio with its interval. The README marks load comparison as built in the engine.
- **A comparison screen over sessions or subgroups.** Pick two or more from the Session records list, or the subgroups of one sheet, and see them side by side. Show the composite plots, the headline figures with intervals, the tests, and each test's verdict.
- **Every negative result carries what it could have detected**, as the stringing card does. "No significant difference" between two ten-shot groups means very little, and `docs/STATISTICS.md` gives the power figures.
- **Never rank loads by point estimate alone.** When the intervals overlap, the screen says the data do not separate them. That sentence matters more than any chart on the screen.

### 3. Hit probability at distance, and distance normalisation

`DESIGN.md` section 3 promises both "propagated through the solver rather than by scaling a group linearly". Entry 112 section 4 deferred them for want of a specification. **This is it.**

**Inputs:**
- the session's sigma and its interval, from the engine, at the distance shot, d₀;
- the rifle and load's solver fields;
- **the muzzle velocity standard deviation**, a new optional field on the load;
- **the crosswind uncertainty in mph**, optional, entered at the time;
- the target's size and shape, a circle or a rectangle;
- the distance d.

**Model.** At distance d, shots are bivariate normal about the aim point plus the zero offset carried to d, from entry 112 section 4.
- σ_x(d)² = (σ_x,ang × d)² + ((∂drift/∂wind) × σ_wind)²
- σ_y(d)² = (σ_y,ang × d)² + ((∂drop/∂V) × σ_V)²
- **The partial derivatives come from the solver** by finite difference, at d.
- **The measured angular sigma already contains the velocity contribution at d₀.** When σ_V is given, subtract that contribution in quadrature before propagating. If the subtraction goes negative, the entered σ_V is too large for the group measured: refuse, and say that.
- **With neither σ_V nor σ_wind given, the result reduces exactly to angular scaling, and the screen says that is all it is.**

**Output:**
- **P(hit) at each end of the sigma interval, as well as at the point estimate.** It is a range, not a single number.
- A circle uses the engine's existing estimators. A rectangle uses the product of normal distributions when its axes align with the dispersion, and otherwise numerical integration.
- **Distance normalisation** shows a group as its solver-propagated equivalent at another distance. It is labelled as a prediction, never as a measurement.

**Tests:**
- with σ_V and σ_wind zero, the result equals angular scaling exactly;
- with σ_V positive, vertical sigma grows faster than linearly with distance;
- a centred circle with equal axes matches the Rayleigh closed form;
- the quadrature subtraction refuses when it should.

### 4. Tooling for the range material, without reading it

Entry 111 section 4 built `analyze-folder` and `timing`. Two things are still missing before Alan's material can be used, and both can be built against committed test data.

**A photograph-against-scan comparison.** Tomorrow gives, for each sheet, a flat 600 dpi scan and four photographs of the same sheet as it hung. **The scan is the truth for the photographs**: same holes, measured flat.
- **A command** that takes a sheet's scan and its photographs.
- It uses the scan's marking as truth once the person has corrected it, or the scan's detection otherwise, and says which.
- For each photograph it reports: the registration model used; bull-centre error against the scan, worst and median; holes found, missed and false against the scan's holes; and the hole-position error, median, 95th percentile and worst.
- **It reports against the gates' thresholds, 0.005 in for bulls and 0.15 in for hole matching, and does not decide the gate.** Planning reads the table.
- Test it on committed images, a synthetic photograph of a committed scan if nothing better exists.

**The doubles sheet breaks one shot per bull on purpose.** Two shots go into each of bulls 1 to 10 and none into 11 to 25. One-to-one matching will push the second shot of a bull onto an empty neighbour.
- **Make sure the way to say so exists**, so that a sheet can be marked as expecting two shots on named bulls, or analysed by nearest bull.
- **Confirm the review queue raises the doubled bulls** when it is analysed without that setting. That is the queue's job, and this sheet tests it.
- Build the setting if it is missing, and test both paths on a synthetic sheet.

**The blank sheet stays Not started.** Nothing for it yet.

### 5. The volunteer print pack

**The README's Phase 4 line, Not started.** A person who wants to contribute a target needs the sheet and one page of instructions.
- **A one-page instruction PDF generated by GroupLab**, from a Markdown source in the repository, alongside the sheet. Its content:
  - print at actual size through Print, and measure bull 1 to bull 5;
  - mount flat;
  - one shot per bull, in order;
  - write only in the load block;
  - the four photographs at about 2.5 ft on the main camera;
  - no cropping or messaging apps;
  - a flat 600 dpi scan if they have a scanner;
  - how to submit at `https://pissinhot.com/targets`.
- **Consent is not in the pack.** The upload page collects consent. The pack says that submitting means following that page's terms.
- **The print screen offers "Print a volunteer pack"**, the sheet and the page together.

### 6. A user guide

**`docs/USER-GUIDE.md`, and a PDF of it**, the project's rule for documents meant for reading. It walks through:
- printing a sheet;
- shooting it;
- photographing or scanning it;
- marking it and settling the review queue;
- reading the analysis screen, including what each "why" says in plain words;
- sessions and the report;
- comparing loads;
- the zero correction and the dope table.

**Illustrate it with the committed renders** under `docs/figures/screens/current/`, and describe only what the build actually does. **A test that every screenshot the guide references exists** keeps the two from drifting. No pseudoscience anywhere in it, as everywhere.

### 7. Housekeeping on everything built in entries 112 and 113

- **Keyboard:** every new screen fully usable without the mouse, as the marking screen is.
- **Themes:** every new screen passes the contrast tests in all four themes.
- **Enum names:** extend the test from entry 111 to every new screen.
- **Renders:** every new screen photographed in dark and light at both sizes, committed under `docs/figures/screens/current/`.

### 8. When the run ends

Whatever is done, record it in `docs/PHASE1-RESULTS.md` and fold both entries into the notes log with status lines that name every section not done. Delete an inbox file only when its entry is complete. Leave a short summary at the top of your final report: what was built, which questions were raised, and what CI says on the final commit.

---

## 2026-09-19, entry 112: session records, the report, the target library, and the solver on screen

**Status: actioned 2026-09-19**, sections 1 to 5. Every section is built; section 5's three things were not done, as it says.
- **Section 1:** session records in one SQLite database, `grouplab.db`, with the schema and its version in `docs/SESSION-SCHEMA.md`, held by a test. The old record file is migrated once and kept. Full JSON export and import round-trip byte for byte. The chronograph has its tables. A session keeps its marking, figures, definition, a 150 dpi proof image, and the original by path and hash. The marking file now keeps the sheet's registration, so no image is needed to reopen. Session records lists sessions newest first, filters by rifle and load, opens one to its analysis, and asks before deleting.
- **Section 2:** the report PDF from GroupLab's own writer, two pages as the entry lists. Every line on it is one the screen shows, with figures given with and without exclusions, and the stringing power statement kept. It is tested with an exclusion and without.
- **Section 3:** the target library on the rail. The built-in sheets are read only, and your own are saved as GLTD in the data folder, to rename, duplicate and delete after asking. The print screen lists both. A sheet a session used stays readable because the session keeps its own copy of the definition, so deleting is allowed and the question says so.
- **Section 4:** the solver's fields on the records, the zero correction carried to a second distance with its uncertainty and its refusal kept, and a dope table on a new Ballistics screen. The screen's rail slot is question 26.
- `docs/PHASE1-RESULTS.md` "Entry 112".

**Alan is at the range on 20 September, and nothing here waits on what he brings back.** This is the largest stretch of Phase 4 still marked Not started, plus the payoff of Phase 5's solver. **It is more than one run.** Work in the order of the sections, finish each one to a clean state with its tests, and say in the status line where you stopped. If a section raises a design question this entry does not answer, raise it in `docs/QUESTIONS-FOR-PLANNING.md`, build what does not depend on the answer, and move on.

### 1. Session records, and the storage `DESIGN.md` section 15 names

**Today an analysis lives only as a marking file the person saves somewhere, and nothing lists them.** The rail's Session records destination still says it is not built.

**Storage.** `DESIGN.md` section 15 says: "Storage is SQLite with a documented schema and full JSON export." Nothing uses SQLite yet, and the rifles, barrels and loads live in a JSON file beside the settings. **This is the moment to follow section 15**, before sessions start accumulating in some other form:
- **One SQLite database in the application's data folder**, holding sessions and the rifle, barrel and load records. The existing record file migrates into it on first run and is kept as a backup, not deleted.
- **A documented schema**, as a document in `docs/`, with its version number. A test holds the document to the schema the code creates.
- **Full JSON export** of everything, and import of that export, with a test that the two round-trip exactly.
- **Section 18's three tiers.** Geometry is always kept. A proof image of about 150 dpi is kept by default. The full-resolution original stays where the person has it, and the session stores its path and its hash, not a copy. No image is ever required to reopen and read a session, because the sheet re-renders from its definition.
- **Section 15's chronograph rule is the reason the schema matters now:** "the shot sequence and the chronograph sequence are separate ordered lists that get reconciled, never assumed to align." **Leave room for both lists and a mapping between them in the schema now**, even though Xero import is Phase 5, so adding it does not require a migration of every session.
- **The library:** `Microsoft.Data.Sqlite` is MIT and SQLite itself is public domain. Note both in `THIRD-PARTY-NOTICES.md`. If you prefer a different binding, say why.

**What a session is.** One analysed sheet with its date, distance, rifle, barrel, load and calibre, the marking with every edit and exclusion, the figures as computed, and the proof image. **Accept and analyse saves it.** Reopening it returns to the analysis state exactly as it was.

**The Session records screen**, the rail's destination. A list, newest first: date, sheet name, rifle, load, distance, shot count, mean radius with its interval. Filter by rifle and by load. Open one to its analysis. Delete asks first. The concept images do not draw this screen, so follow the concept's language: the table style of the shot list, the row and hairline pattern from entry 109, and no new visual devices.

### 2. The report

**The Report button on the analysis screen is in the concept and not built.** A report is a PDF of one session, drawn by GroupLab's own PDF writer, for printing or sending to someone.

**Page one, the result:**
- sheet, date, distance, rifle, barrel, load and calibre;
- the composite plot;
- the headline figures, **each with its interval**;
- the zero correction and its verdict;
- the two judgement cards, verdict and test.

**Page two, the evidence:**
- the shot table with bulls;
- every exclusion **with its reason**;
- any decision left unmade, stated as the analysis screen states it;
- the registration's quality;
- the "why" text of each figure and card, which fits on paper where it did not fit on screen;
- GroupLab's version and the sheet's identifier.

**Two rules the report must keep:**
- **Exclusions are never hidden.** `docs/STATISTICS.md` section 10: "every report prints the full and reduced figures side by side so an exclusion can never be hidden." When anything is excluded, every figure appears twice, with and without. Test it.
- **Nothing on paper is more certain than the screen.** Every interval, hedge and power statement the screen carries goes into the report. The stringing power statement in particular stays beside its result.

**A test** renders a report for a session with an exclusion and one without, and checks that the text it contains is what the rules require.

### 3. The target library

**The rail's Target library destination.** `docs/figures/screens/library-and-print.png` is the concept.
- The built-in sheets, read only, and the person's own sheets from the parametric editor, saved as GLTD files in the application's data folder.
- A person's sheet can be renamed, duplicated as the start of a new one, and deleted after asking.
- The print screen lists both, and a person's sheet prints exactly as a built-in one does, through the same refusals.
- **A sheet a session was analysed against must stay readable.** Deleting a person's sheet that a session uses either refuses with the reason or keeps the definition inside the session. Say which you chose and why.

### 4. The solver on screen

**Phase 5's solver is validated and invisible.** `DESIGN.md` section 3 and entry 110 section 2g name what it is for. The first two uses are ready now:
- **The records gain what the solver needs**, all optional:
  - on the rifle: sight height and zero distance;
  - on the load: muzzle velocity, BC, drag model G1 or G7, the BC's reference atmosphere, and bullet weight;
  - twist rate, bullet length and bullet diameter, for spin drift.
  
  A record missing any of these simply cannot use the solver, and the screen says which field is missing.
- **The zero correction carried to another distance.** The analysis screen's zero block already says "moving a zero between distances needs the ballistic solver." When the rifle and load carry what it needs, give the correction at a second distance the person chooses, with the uncertainty carried through, not dropped. Keep the existing refusal when the offset cannot be told from zero: a correction at 500 yd derived from an offset nobody can distinguish from zero is still nothing.
- **A dope table** for a rifle and load: range, drop and wind per 10 mph in the person's units and clicks, from `grouplab trajectory`'s engine, with the atmosphere as an input. Show "Aerodynamic jump is not modelled" beside it, as the command does.
- **Hit probability at distance and distance normalisation wait.** They need the propagation of the group's sigma through the solver to be specified, which is its own entry.

**The README's states and `DESIGN.md` move with each section.**

### 5. Things not to do in this entry

- **Do not touch anything from `C:\Dev\grouplab-range-2026-09-20\`.** It arrives with its own entry.
- **Do not start Garmin Xero import.** It needs a real export file, and none is in the repository. The schema leaves room for it, as section 1 says.
- **Do not start Android.** Alan wants the Windows application right first.

---

## 2026-09-19, entry 111: questions 24 and 25 answered, four small screen fixes, and the range material that arrives next

**Status: actioned 2026-09-19**, sections 1 to 4. Section 4 asked for nothing to be processed, and nothing was; its blank-paper route is reported and not built, as it says.
- **Section 1:** the standard G1 and G7 tables carried. They are the values py-ballisticcalc 2.3.1 and poncelet, from JBM's McCoy tables, agree on at every point, both committed in `reference/drag-tables`, with a test holding the carried tables to both. G1 now passes the gate under the tolerances as first committed, and the known-failure markers are removed. Question 25 is set to answered.
- **Section 2:** the Coriolis vertical term ported with its sign corrected and tested east, west, north, south and at +3.0 in. The five corrections are recorded in DESIGN.md section 16. Question 24 is set to answered.
- **Section 3:** the exclusion reasons in words, with a test over every compound enum name on every screen, which found a second one, "OneToOne" in the assignment stage's decision; each "why" beside its item's last line; the Load labels at the top of their rows; the shot count once; the renders regenerated.
- **Section 4:** `grouplab analyze-folder` and `grouplab timing` built for the first two paths; the blank-paper route is reported as not started.
- `docs/PHASE1-RESULTS.md` "Entry 111".

### 1. Question 25 answered: A, carry the standard G1 table

**The G1 table in ballistics.js is wrong above Mach 0.85, and the error is mine: I wrote that file, and entry 110 section 2a told you to port it as it stands.** Your comparison found it. A G1 load flies with too little drag, 8.6 MOA short at 1000 yd for the .308 case, and the website serves that table to every visitor today.

**Carry the standard G1 function.** Neither of us has McCoy's *Modern Exterior Ballistics* or the BRL report to hand, so the source rule is this:
- **Take the values where two independent transcriptions agree.** py-ballisticcalc is one. The second must have its own transcription, not a copy of py-ballisticcalc's: another open-source exterior-ballistics library, or a published table from a named source. Name both in the code and in `docs/BALLISTICS-VALIDATION.md`.
- **A test holds the committed table to both transcriptions**, point by point, at the precision they publish.
- **Where the two disagree, stop and list the points.** Do not pick one.
- **Rerun the independent gate for G1** with the tolerances as committed before the first run, unchanged. Remove the two known-failure markers only if the cases pass. The transcription check lists the G1 table as an intentional difference from ballistics.js.
- **Check the G7 table the same way while you are there.** The comparison found it differs from py-ballisticcalc only at Mach 3.5 and above. Hold it to the same two-transcription rule, and carry the agreed values.

### 2. Question 24 answered: A, the Coriolis vertical term with its sign corrected

**Your physics is right.** For fire toward the east the Coriolis acceleration's vertical component is +2ΩV cos φ, so the bullet strikes high. The file's comment and sign are both backwards. Port the term with the corrected sign. **Tests:** fire due east strikes high, fire due west strikes low, north and south give zero, and 45 degrees north, east, 1000 yd, 1.6 s gives about +3.0 in.

**Section 3 of the question.**
- **Wind direction:** a signed crosswind, positive from the left, drifting the bullet right. Accepted.
- **Aerodynamic jump:** stays out until someone has the page. Alan may own *Applied Ballistics for Long Range Shooting*. If he sends a photograph of the page with the fit, a later entry will carry it.
- **The BC reference atmosphere:** your reading is the one I meant. A BC stated against Army Standard Metro is flown with the density ratio taken against Army Standard Metro density, and the test holding the ratio at 1.018 stays.

**Record in `DESIGN.md` section 16** that the port corrected ballistics.js in five places: the G1 table, shooting angle, aerodynamic jump left out, the Coriolis vertical sign, and the wind-direction convention. A later reader should not restore the JavaScript's behaviour thinking the port drifted from its source.

### 3. Four small things on the screens

From the renders under `docs/figures/screens/current/` at `44b34e0`. Alan's own notes on the screens come after he has used the build with real scans; these four are the ones I can see now.
- **The exclusion reason shows the enum name "CalledFlyer"** in the marking screen's dropdown. Show "Called flyer", and do the same for every other reason. **Check every place an enum name reaches a person**, as "OneToOne" did before entry 109. A test over the enums shown in the interface would stop the next one.
- **Each "why" takes a full row** with its arrow at the far right. There are four of them on the analysis screen. Put a small "why" with its arrow directly under, or beside, the item it explains, so it takes no row of its own.
- **The Calibre line in the Load block wraps**, and the label sits below the value it names. Let the value wrap under itself with the label at the top of the row, or give the value its own line under the label.
- **The Group section states the shot count twice:** "Shots 24", then "24 shots: 24 detected." Keep one.

Regenerate the renders after these, as entry 109 section 4 set up.

### 4. What arrives next, so nothing waits on it

**Alan is at the range on 20 September** with a one-page plan. The plan is at `C:\Dev\grouplab-range-2026-09-20\grouplab-range-day-2026-09-20.pdf`, outside the repository. He is shooting:
- **four 25-shot sheets** of `GL-CF25-LTR-D`, the load-block version, possibly a different rifle on each, three at 100 yd and one at 200 yd if the range allows;
- **a doubles sheet**, two shots into each of bulls 1 to 10;
- **a blank sheet**, 10 shots at a marker dot, photographed with a tape measure across it;
- **four photographs of each sheet as it hangs**, at about 2.5 ft square on, 30 degrees left and right, and about 5 ft;
- **600 dpi flat scans of every sheet** afterwards at home;
- **Garmin Xero sessions**, if he has the unit, one per sheet, with shots fired in bull order so shot number equals bull number.

**The material lands in `C:\Dev\grouplab-range-2026-09-20\`**, one folder per sheet. **Do not process it under this entry.** The next entry will say how it enters the corpus. It carries location data in its photographs, so it goes through the intake tool like every donated image: nothing from that folder is committed directly.

**What would help, if this entry's work leaves time:** make sure the paths it will need are ready.
- Running the Phase 1 detection gate over a folder of new scans, from one command.
- The Phase 3 timing measurement on one sheet, from one command.
- The blank-paper route: its gate names the material, and the feature is still Not started.

**Say which of these exist and which do not.** Build nothing for the blank paper yet.

---

## 2026-09-19, entry 110: question 23 answered, and the ballistic solver ported from ballistics.js, with two of its functions not carried over

**Status: actioned 2026-09-19**, sections 1 and 2, with three parts of section 2 not done as written:
- **Section 1:** question 23 set to answered.
- **Section 2:** the solver ported into `GroupLab.Core`, `grouplab trajectory`, and both validations, with ballistics.js committed unchanged under `reference/ballistics-js/` with its README.
  - **Not done as written, 2a:** the Coriolis vertical term, which the section lists to port as it stands, has its sign reversed. It is held out pending question 24.
  - **Not done as written, 2a and 2f:** the G1 table, also listed to port as it stands, is not the standard G1 function above Mach 0.85. The port carries it meanwhile. G1 fails the independent gate, and question 25 asks which table to carry. G7 passes the gate well inside its tolerances.
  - **2c:** aerodynamic jump is left out, the section's second option, because the publication was not to hand; the output says so.
- **Section 2g's later work** waits for its own entry, as the section says.
- `docs/PHASE1-RESULTS.md` "Entry 110".

**Do not start this until entry 109 is committed, pushed and folded.** If entry 109's run is still going when you read this, finish it first. This entry does not touch the screens.

### 1. Question 23 answered: yes to all of it, and the three corrections are mine

**Section 1 of the question.** All three statements were wrong, and for the same reason: I judged from a render rather than reading the code behind it. The crumb fell back only because the test never opened a file. The framing was a 35 percent margin, not the bull. The text labels on the tools were entry 93's recorded decision. **What you did at each point is accepted as done.** The new renders open a real file, so they show what a person sees, and that closes the gap I read wrongly.

**Section 2.** The stringing power sentence stays whole and wraps; `STATISTICS.md` section 7 outranks my "one line". The group's inputs stay in the panel below the scale; they are the task.

**Section 3.** The lead figure as the one named exception to the five styles is right.

Set question 23 to answered, pointing here.

### 2. The ballistic solver: Phase 5 begins

**Alan wants the solver from his website in GroupLab.** `DESIGN.md` section 16 has planned exactly this port since revision 1. The source is delivered beside this entry as `entry-110-ballistics.js`, taken from the pissinhot.com web server on 2026-09-19. Its SHA-256 begins `581fab43209367af`.

**Provenance.** Claude wrote it for Alan in his Pissin Hot Precision project, so it is Alan's code, and he can license it into GroupLab under GPL-3.0 with the section 7 app-store permission. **The G1 and G7 drag tables** are from the US Army Ballistic Research Laboratory and are public domain. **The spin-drift and stability formulas** are Litz's and Miller's published formulas; cite them in the code. **Commit the file** unchanged as `reference/ballistics-js/ballistics.js`, with a short README in that folder giving its source, date, hash and what GroupLab uses it for. It is already served to every visitor of the website, so nothing in it is private.

**I read all of it and ran it under Node before writing this.** Its core is sound and ports directly. Two functions are wrong and must not be carried over as they are.

#### 2a. Port as it stands

- **The G1 and G7 tables** and their linear interpolation in Mach.
- **The atmosphere:** station pressure from altitude by the ICAO formula, the density ratio with its humidity correction, and the speed of sound with the moist-air correction. `pressureFromAltitude` computes four variables it never uses; leave them out.
- **The drag deceleration**, `(ρ/ρ₀) × Cd(M) × V² × 0.00020856 / BC`. I checked the constant: π × 0.0764742 / 1152 = 0.00020856, with the BC in lb/in² and ρ₀ as mass density in lb/ft³. It is right.
- **The point-mass RK4 integration** of downrange and vertical position and velocity.
- **The zeroing search**, the bisection on launch angle.
- **Wind drift by the lag-time method**, `Vw × (t − x/V₀)`.
- **Miller's stability factor, Litz's spin-drift fit, and the Coriolis horizontal and vertical (Eötvös) terms.**

**Its flat-fire output is plausible.** A 175 grain .308 at 2700 fps with a G7 BC of 0.243, zeroed at 100 yd, reads 37.4 MOA at 1000 yd and 105 in of drift in a 10 mph full-value wind. Its drop at the zero range reads 0.01 in.

#### 2b. Shooting angle: broken, and live on the website

**Any non-zero angle gives nonsense.** Run under Node with the case above:

| Angle | 100 yd | 500 yd | 1000 yd |
|---|---|---|---|
| 0 | −0.01 MOA | 11.06 | 37.37 |
| +5 | **−297.6 MOA** | −285.6 | −258.9 |
| +10 | **−604.4 MOA** | −590.8 | −563.0 |
| −10 | **+260.8 MOA** | +272.0 | +298.5 |

**The cause:** the angle tilts the launch, but drop is still measured from the horizontal line `y = 0` at horizontal distance `x`. The trajectory is never compared with the inclined line of sight, so the table reports the line of sight's own rise, about 300 MOA per 5 degrees.

**In the port:** measure range along the line of sight and drop perpendicular to it. **Tests:** angle 0 reproduces the flat table exactly. A small angle gives drop close to the flat-fire drop at the same horizontal distance, as the rifleman's rule predicts, within a tolerance you state. Uphill and downhill at the same angle agree closely at short range.

#### 2c. Aerodynamic jump: not physics, so not carried over

`aeroJump` returns `crosswindFps × 0.012 / SG` MOA. Its own comment says "Empirical: ~0.01 MOA per fps ... This is a rough approximation based on published AB data". **The number is not from any published source I can identify, and it runs the wrong way.** It shrinks as stability rises, where Litz's published fit for aerodynamic jump grows with stability. At SG 1.5 and a 10 mph crosswind it gives about 0.12 MOA. Litz's fit, as I recall it, gives about three times that for a typical bullet.

**Do not port it.** Two acceptable outcomes:
- **Implement Litz's published aerodynamic jump fit, from the source**, with its coefficients, sign convention and units checked against the publication itself and cited in the code. My recollection is MOA per mph of crosswind = 0.01 × SG − 0.0024 × L + 0.032, with L the bullet's length in calibers. **Do not take that from me; take it from the book.**
- **Or leave aerodynamic jump out** and say so wherever the solver's output is shown: "Aerodynamic jump is not modelled."

**A made-up number presented as a correction is the one outcome that is not acceptable.** It is the same rule GroupLab applies to pseudoscience.

#### 2d. Do not port the dispersion utilities

`velocityDispersion`, `hitProbability` and `combinedGroupSize` estimate dispersion their own way. For example, `hitProbability` takes a group size in MOA and treats half of it as one standard deviation. **GroupLab's statistics engine estimates sigma properly, with its interval, from the marked shots.** Phase 5's hit probability at another distance propagates that sigma through the solver, as `DESIGN.md` section 3 says. Port the trajectory, not these.

#### 2e. Smaller changes for the port

- **Interpolate to the exact range.** The JavaScript records the first integration step at or past each range. That is up to 1.5 ft late at 2700 fps: about 0.04 MOA at 1000 yd, and the reason the zero reads 0.01 in rather than 0.
- **Use RK4 for the zeroing pass too.** The JavaScript zeroes with a first-order step and then flies the trajectory with RK4. The measured effect is small, but the zero should be found on the same trajectory it is applied to.
- **Keep full precision and round only for display.** The JavaScript rounds every column as it records it.
- **Miller's stability factor:** its atmosphere correction here uses temperature only. Miller's correction also scales with pressure; include it.
- **The BC's reference atmosphere.** The solver's ρ₀ is ICAO, 59 °F, 29.92 inHg, dry. Many published G1 BCs are referenced to Army Standard Metro instead, and using one against the other misstates drag by a percent or two. **Make the reference atmosphere a stated input, defaulting to ICAO**, with Army Standard Metro available. Test the density ratio between the two.

#### 2f. Validation: the gate needs an independent implementation, and the JavaScript is not one

The Phase 5 gate is "a ballistic solver validated against an independent implementation". **ballistics.js has the same author, the same method and the same tables as the port, so matching it proves the transcription, not the physics.** Two checks, therefore:

1. **Against the JavaScript, for the transcription.** Run the original under Node in CI; GitHub's runners have it. Compare flat-fire cases across G1 and G7, several loads, atmospheres and winds, at the JavaScript's own sampled positions so the interpolation change does not blur the comparison. **List every intentional difference** (angle, aerodynamic jump, interpolation, the RK4 zero, the Miller pressure term) with the case that shows it.
2. **Against an independent solver, for the gate.** Pick an open-source point-mass solver, for example py-ballisticcalc. Check its licence yourself, use it only in a script that generates reference tables, and commit the tables, not the solver. **Write down the tolerance before you run the comparison**, drop and wind deflection in MOA to 1000 yd and time of flight in percent, with the reason for each figure. A tolerance chosen after seeing the numbers is not a gate.

#### 2g. What to build now, and what waits

- **Now:** the solver in `GroupLab.Core`, both validations, and a `grouplab trajectory` command that prints a table from stated inputs, so it can be checked by hand against any calculator.
- **Waits for its own entry:** the solver in the interface, the zero correction carried to another distance, hit probability at distance, and distance normalisation. Each needs muzzle velocity, BC and sight height on the rifle and load records, and a decision about where they appear.
- **The README's Phase 5 line** for the solver moves to **In progress**, or to **Built, not proven** once the independent comparison passes. Record the port and the two exclusions in `DESIGN.md` section 16.

**No pseudoscience.** The solver models physics only, as section 16 says. Nothing in ballistics.js touches barrel timing, and nothing in the port should.

---

## 2026-09-19, entry 109: a layout and readability pass on both screens, measured against the concept

**Status: actioned 2026-09-19**, sections 1 to 4, the marking screen and the settings screen first and then the analysis screen, in one run. Three statements the code does not bear out, and two places where the entry's own limits meet, are raised as question 23, each built meanwhile.
- **Section 1:** the five text styles as tokens with mean radius's lead the one exception, and a test holding every text on the marking, analysis and settings screens to them; a spacing grid on a base of 4; one row shape for every figure, divided by hairlines; and a "why" disclosure on each item it explains, closed by default and remembered, holding the explanations as they stood.
- **Section 2:** the tools as icons named with their keys, Undo and Redo at the strip's end and the review's keys on its right; the view controls over the canvas; Open image, Open marking, Export and Report a problem in one header menu; the rail's Print slot opening the print screen; a Settings screen behind a gear with the units, theme, log and crash records; the scale in one line with its detail behind "why"; the summary saying "one-to-one matching" with no colon when there is no reason; and a one-line status.
- **Section 3:** the unmade decisions as a compact banner with a link back; the zero correction's verdict in one line; the judgement cards as a verdict, the test and its p value, with the stringing power statement in view and the rest behind "why"; the plot's shots as dots with faint calibre outlines and a toggle, framed on the group with the artwork lighter; the shot table as plain shaded rows with one number per shot; and the crumb naming the sheet when no image is recorded.
- **Section 4:** every screen in dark and light at 1400 by 900 and 1920 by 1080, the analysis with its disclosures closed and open, committed under `docs/figures/screens/current/`.
- `docs/PHASE1-RESULTS.md` "Entry 109".

**Alan: "I want a lot of UI refinement and layout done. It is still hard to read."** He is right, and it is not one thing. I read the current renders in `out/screens/` at `d22d7fc` side by side with `docs/figures/screens/analysis-dark.png` and `assignment-editor.png`, which Alan approved as the guideline. What follows is what differs and why each difference costs readability.

**What this pass must not change.** No figure, interval, verdict or refusal changes its meaning, and nothing true is deleted. The findings from entries 91 to 108 stay. **What changes is how much of each one is on screen at once.** `DESIGN.md` section 19 already sets the rule, and the screens have drifted from it:

> The primary panel shows the composite group, the headline figures, and the confidence interval on each, because those change decisions and burying them would defeat the project's premise. Reference material, the full CEP table, the bivariate fit, and the comparison machinery live one click away in a panel that remembers it was opened.

**Today the explanation of every figure is on screen beside the figure.** That is the main reason both screens read as walls of text.

### 1. The principles, so each screen is judged the same way

1. **One line per fact on the surface; the reasoning is one click away.** Each figure shows its value, its unit and its interval. Each judgement shows its one-sentence verdict. **The sentences that explain them move behind a disclosure on that item:** a "why" chevron that expands in place and remembers it was opened, as `moreFiguresOpen` already does. Move the wording as it stands; do not rewrite it.
2. **A type scale of five styles, no more:** screen title, section heading, label, value, and detail. Today there are more sizes and weights than that on each screen, and prose appears in the value style. Define the five as tokens and use nothing else. Section headings need to be readable, not dim capitals: they are the structure.
3. **One row pattern for every figure.** The label is on the left in the label style. The value is on the right in mono at the value size. One detail line sits beneath for the interval and the other unit. A hairline divides rows. The concept's right column is exactly this, and it reads cleanly because every row has the same shape.
4. **A spacing grid.** Pick a base unit, 4 or 8 pixels, and use multiples of it for every gap and padding. Sections are separated by space and a rule, not by boxes around everything.
5. **Settings are not part of the task.** Units, theme and diagnostics do not belong in the marking panel between the review queue and the scale.
6. **Diagnostics are not the status line.** A detection summary that runs to two lines of technical prose belongs in Show work, not across the bottom of the window.

### 2. The marking screen

**a. Too many buttons, in two wrapped rows of text.** The header has five actions and the review pill. The tool strip below it has seventeen text buttons, from Open marking to Report a problem, and wraps onto a second row at 1400 pixels. The concept's editor has one row: seven icon tools on the left and the keycap hints on the right. Entry 93 marks the icon tool strip as done, but the render shows text labels with small keycaps beside them. **Regroup by what each button is:**
- **Tools** (Pan, Scale length, Scale rectangle, Point of aim, Impact, Select): icons only, a tooltip naming each with its key, and the active tool highlighted. That is the strip.
- **Edit** (Undo, Redo): two icons, at the end of the strip, as the concept has them.
- **View** (Zoom in, Zoom out, Fit, Rotate left, Rotate right): a small cluster floating over the bottom-right corner of the canvas, the usual place for view controls. They act on the view, not the document.
- **Document actions** (Open image, Open marking, Export): a single menu behind an overflow button in the header, beside Accept and analyse.
- **Print a target:** the rail already has a Print destination that says it is not built. **The print screen is built, so wire the rail's Print slot to it** and drop the toolbar button.
- **Report a problem:** into the same overflow menu, and into the Settings screen below.
- **Detect on a GroupLab sheet** stays a header button, since it is the step after opening an image. **Show work** stays where entry 105 put it.

The header should end up with the pill, Detect, Show work, Discard edits, Accept and analyse, and the overflow button. That fits on one line.

**b. The right panel mixes the task with the settings.** Under the review queue sit UNITS, THEME, DIAGNOSTICS and "Detailed logging", then SCALE. **Move units, theme and logging to a Settings screen**, reached by a gear at the bottom of the rail, the usual place. Crash reporting's controls and "Report a problem" go there too. The panel is then the review queue, the selected detection and the scale, which is what the concept shows.

**c. The SCALE block is a raw diagnostic in mono teal.** It reads: "From the sheet's own printed markers: 34 of 34 markers found, detected without a calibre, so whether a mark was one hole or two was judged by its shape alone, registration RMS 0.0022 in over 34 markers, 0 holes detected, assigned by OneToOne: ." **Show one line**, "Scale from the printed markers, 34 of 34", with a mark for good or bad, and put the rest behind its disclosure and in Show work.

**d. A text defect in the same sentence.** It ends "assigned by OneToOne: ." with nothing after the colon. `AutomaticMarking.cs` line 196 appends `assignment.Reason` unconditionally, and here the reason is empty. **Omit the colon when there is no reason.** Also say "one-to-one matching", not the enum name `OneToOne`, anywhere a person reads it.

**e. The status line** carries the same summary and wraps. **Make it one line** that says what just happened, "Detected 25 holes on 25 bulls", with the detail in Show work.

### 3. The analysis screen

**a. The right column is an essay.** Top to bottom it runs: an amber paragraph about unmade decisions, a zero correction block of six lines including a bold four-line paragraph, the group figures, and two judgement cards of five to eight lines each that run off the bottom. **Apply principle 1 throughout:**
- **The unmade-decisions line** becomes a compact banner: "15 decisions left unmade. Review them" with the second half as a link back to the editor. It stays amber and it stays at the top.
- **Zero correction:** the two readouts, the uncertainty in one detail line, and the verdict as one line, "Not distinguishable from zero. About 90 shots would settle it." The degrees of freedom and the ballistic-solver note go behind "why".
- **The figures:** principle 3's row. Mean radius keeps its lead size, as entries 73 and 92 set.
- **The two judgement cards:** the bold verdict and one line of evidence, the test name and its p value. The rest goes behind "why". **The stringing power statement is an exception and stays visible**: `STATISTICS.md` section 7 requires it beside the result, not in a footnote. Keep it to one line.

**b. The composite plot is dominated by the bull, and the shots are a tangle.** The white disc and heavy grey ring fill the frame. The group, 0.123 in mean radius, sits in the middle half. Every shot is drawn as a dark red circle at the .308 calibre, and 25 circles each larger than the mean radius overlap into a single mass in which no individual shot can be picked out.
- **Draw each shot's centre as a solid dot**, the thing the statistics use, and its calibre outline thin and faint behind it. A reader can then count shots and see the spread. **Keep the calibre outlines on by default** but give the key a toggle for them.
- **Frame the group, not the bull**, as entry 103 section 1 asked: fit the shots, including their calibre outlines, with a margin, and let the rings run off the frame when the group is smaller than the bull.
- **Draw the bull's artwork lighter**, as the concept does. It is context, and today it is the loudest thing on the screen.

**c. The shot table is 25 boxed rows.** Each row has its own border, which makes the column heavy and slow to scan. **Use plain rows with a hairline or alternate shading, as the concept does.** The shot and bull columns repeat each other, 1 and 1, 2 and 2, because shots are named by their bull. **Show one number** and show the bull only where it differs from the shot's name. Right-align the numeric columns under their headers on the decimal point.

**d. The LOAD block** reads well. Keep it.

**e. The breadcrumb's middle crumb says "the sheet".** Use the sheet's file name, as the editor's breadcrumb does. The crumb is the way back, so it should name what it goes back to.

### 4. How to show the result

**Alan should judge this by looking, and so should I.** The renders in `out/screens/` are how I read the screens between runs, so extend them:
- **Every screen, in dark and light:** the marking screen, the analysis screen, the print screen and the new Settings screen, at 1400 by 900 and at 1920 by 1080, since Alan's display is wide.
- **The analysis screen once with its disclosures closed and once with them all open**, so the default state and the full state can both be judged.
- **Commit the renders under `docs/figures/screens/current/`** so they can be compared with the concept files beside them. Regenerate them in the same run as any UI change, and keep them out of the drift tests, since pixel output varies between machines.

**Split the work across runs if it is too much for one**, in this order: section 2's marking screen and the Settings screen first, then section 3. Say in the status line which parts are done.

---

## 2026-09-19, entry 108: question 22 answered, calibre designations refused in both units, and two errors of mine in entry 107

**Status: actioned 2026-09-19**, sections 1 and 2.
- **Section 1:** the count corrected to 37 in entry 107's own text below, the one place 36 stood as the pick list's count; DESIGN.md, the README and `docs/CALIBRES.md` never carried it.
- **Section 2:** question 22 answered. The designations are refused in inches and in millimetres with the sentence that names the problem, the real diameters beside them still read, a test holds both lists apart from the 37 diameters in both units, and the test that pinned "9mm" as read is replaced. `docs/CALIBRES.md` and `.pdf` list the refused values.
- `docs/PHASE1-RESULTS.md` "Entry 108".

### 1. Question 22 is right on both counts, and both errors are mine

**The contradiction.** Entry 107 section 1 said a number marked mm is read as millimetres, and then listed "9mm" among the names to refuse. Those cannot both hold. Building the rule as written and pinning "9mm" with a pointer to the question was correct.

**The count.** Alan's two lists hold **37** distinct diameters, not 36. I left .356 out when I counted. All 37 in the pick list is correct. If the number 36 appears anywhere in `DESIGN.md`, the README, `docs/CALIBRES.md` or the notes, correct it.

### 2. The answer: refuse calibre designations, in inches as well as millimetres

**Your recommendation is right, and the same trap exists in inches, which question 22 does not mention.** The accepting pattern takes any decimal below one, so ".38" reads as 0.380 in when a .38 bullet is .357 or .358. ".45" reads as 0.450 against .451 to .458, ".22" as 0.220 against .223 or .224, ".30" as 0.300 against .308, and ".270" as 0.270 against .277. **These are designations, as "7.62 mm" is.** A shooter who types the calibre they know, with a decimal point in front of it, gets a diameter that no bullet has.

**The rule:** a typed value that equals a common calibre designation, and is not itself a bullet diameter, is refused.
- **Inches:** 0.17, 0.20, 0.22, 0.25, 0.27, 0.28, 0.30, 0.303, 0.32, 0.35, 0.38, 0.44, 0.45. This covers ".270", ".280" and ".300" typed with three digits, since they are the same values.
- **Millimetres:** 5.45, 5.56, 6, 6.5, 6.8, 7, 7.5, 7.62, 7.65, 8, 9, 10.

**Deliberately not refused**, because each is also a real bullet diameter: .40 and .41 (.400 and .410), .50 (.500), .308, .338, .375, .416, 9.3 mm (.366) and 12.7 mm (.500).

**The refusal names the problem without naming a cartridge**, in keeping with Alan's decision: "7.62 mm is a calibre's name, not the bullet's diameter. Enter the bullet's diameter, such as 7.82 mm or 0.308." The example stays generic; the message does not try to guess which bullet was meant.

**This is not the name table returning.** It is a short list of numbers that are refused, not matched to anything. Nothing is ever read from it, and it cannot produce a diameter.

**A test holds the two lists apart.** No refused value may equal any of the 37 pick-list diameters, in either unit, at the precision it is typed. If a future diameter is added that collides with a designation, the test says so rather than the input silently refusing a real bullet.

**Tests:**
- every refused value in both lists, including "9mm", "7.62mm", ".38" and ".270", is refused with the sentence;
- ".357", ".452", "0.308", "7.82 mm" and "9.3 mm" read correctly;
- every pick-list diameter still reads.

**Set question 22 to answered**, pointing here, and replace the test that pins "9mm" as read.

---

## 2026-09-19, entry 107: the calibre is a diameter and nothing else, and question 21 answered so printing can be built

**Status: actioned 2026-09-19**, sections 1 and 2. **One test in section 1's list is not met: "9mm" is read as a diameter, not refused**, because the section's own rule reads any number marked mm; that conflict, and a pick list of 37 diameters where the section says 36, are raised as question 22, with the written rule built meanwhile.
- **Section 1:** the calibre is a diameter and nothing else, recorded as Alan's decision in `Calibre` and in DESIGN.md. Inches, bare below one or marked, and millimetres marked mm; names and bare numbers of one or more refused with the one sentence; the pick list the 37 distinct diameters in both units with .223 gone; the LOAD panel and every other place shows ".308 in (7.82 mm)"; a marking saved under a name loads with its diameter; the name table, the candidates and the ambiguity sentence removed; `docs/CALIBRES.md` and `docs/CALIBRES.pdf` regenerated as the diameters and the rule.
- **Section 2:** question 21 answered and built. On Windows, "Print…" is the amber primary beside Open to print, through `PrintDlgEx` and GDI, the scene drawn as vector at actual size and shifted by the printer's physical offsets; refused with the reason on the wrong paper or with ink in the margin, the quiet zone not counted; a confirmation after `EndDoc` naming the printer, the sheet, the page count and "at actual size". The geometry is plain functions tested on every platform; a sheet printed to "Microsoft Print to PDF" puts its 38 markers within 0.015 mm of the definition, and the Windows CI job lists its printers.
- `docs/PHASE1-RESULTS.md` "Entry 107".

Two sections. **Do section 1 first**; it is small. Section 2 is the run Claude Code costed in question 21.

### 1. Alan's decision: the calibre input takes a diameter in inches or millimetres, and no names

**Alan has decided that the application offers the calibre only as a diameter**, in inches or millimetres. No cartridge names and no "30 Cal." style names. **This reverses entry 105 section 7 and entry 106 section 3**, and changes entry 106 section 4's list. Record it as a decision so nobody restores the names later thinking they were lost.

**Why it is the better design, not only a preference.** Every defect entries 105 and 106 fixed came from reading a name as a diameter: 36 of Alan's 44 names read wrong, nine meant more than one diameter, and 300 Blackout read as 0.300. The table fixed the names it knew, and every name it does not know still falls back to a guess. **A diameter has one meaning.** Asking for it removes the whole class of error rather than one more instance of it.

**What the input accepts.**
- **Inches:** a decimal below one, with or without its leading zero, or any number marked `in` or `"`. So ".308", "0.308" and "0.308 in" all read as 0.308 in.
- **Millimetres, only when marked:** "7.82 mm" or "7.82mm" reads as 7.82 mm.
- **Everything else is refused, with one sentence that says what to type:** "Enter the bullet diameter in inches, such as 0.308, or in millimetres with mm, such as 7.82 mm."

**A bare number of one or more is refused, not guessed. This matters most.** The old rule read a bare 1 to 14 as millimetres, and that is the calibre-name trap in another form. "7.62" as millimetres is 0.300 in, a diameter no 7.62 bullet has; "6.5" is 0.256 in, and the bullet is .264. **Requiring the unit makes the person state a diameter rather than a name that happens to be a number.** Remove the hundredths and thousandths guesses too, so "308" and "22" are refused rather than read.

**The range check stays:** 0.1 to 1.0 in, however it was entered.

**The pick list becomes diameters.** It holds the distinct diameters in Alan's two lists, 37 of them (written as 36 here and corrected by entry 108 section 1), from .172 to .510, each shown in both units, for example ".308 in (7.82 mm)", ordered by size. No names beside them. **.223, which came from the old ".22 LR" entry, is not in Alan's list**, so it leaves the pick list. It can still be typed.

**How the calibre is shown everywhere else** is the same form, the diameter in both units. **The analysis screen's LOAD panel currently reads "Calibre .308, 7.62 mm, set after detection"**, which shows a cartridge name's "7.62 mm" beside a .308 in diameter, when .308 in is 7.82 mm. That line becomes ".308 in (7.82 mm), set after detection".

**Saved markings.** A marking that stored a calibre name keeps its diameter. It loads with the diameter shown, and the old name is not displayed. Test it with a marking saved under a name.

**Remove what only the names needed:** the `Cartridges` list, the key matching, the candidates and the ambiguity sentence, and the tests that pinned them. **Keep `grouplab calibres` and its drift test**, now writing `docs/CALIBRES.md` and `docs/CALIBRES.pdf` as the diameter list and the input rule in plain words.

**Tests:**
- every pick-list diameter reads exactly, in both units;
- names are refused with the sentence, including "300 Blackout", "6.5 Creedmoor", "30 Cal." and "9mm";
- a bare "7.62", "308" and "22" are refused;
- "7.82 mm" and "0.308" read correctly;
- a marking saved under a name loads with its diameter.

### 2. Question 21 answered: build printing from inside GroupLab, on Windows

**Build it in this run, after section 1, as question 21's plan lays out**: Win32 through P/Invoke, `PrintDlgEx` and GDI, drawn as vector from the scene, shifted by the printer's physical offsets, with the paper and margin refusals and the confirmation after `EndDoc`. The reasoning against `System.Drawing.Printing` and the WinRT manager is sound.

**The three decisions.**

**1. The margin refusal covers inked items only, not the quiet zone.** This is the one place I disagree with the recommendation, and the reason is physical. **A printer's unprintable margin leaves bare paper, and the quiet zone is white by design:** it is the blank paper around a marker, and nothing dark is printed in it. A quiet zone that falls in the margin comes out exactly as it would have printed, white, so refusing on it refuses a sheet that would work. **Refuse when any item drawn in a non-paper ink falls in the margin, wholly or partly**: markers, codes, bull artwork, rules and text. Text is included because the printed name and identifier are part of the sheet's record. **If you know a reason the quiet zone matters that this misses**, such as printers that smudge toner near the edge, say so in the write-up and refuse on it instead. I would rather be corrected than refuse sheets for nothing.

**2. Linux and macOS keep the viewer path.** As you recommend. GroupLab is used on Windows, and a CUPS path is worth building only once Windows proves the approach.

**3. Print sits beside Open to print on Windows, and Print is the primary.** As you recommend: Print is the amber button, and Open to print stays as the secondary for anyone whose workflow goes through their viewer. **Alan chose printing from inside GroupLab so that no sheet can come out at the wrong size**, so Print must be the obvious choice and Open to print the deliberate one.

**The tests.**
- **The geometry, testable anywhere with no printer:** the physical-offset shift, the paper-size refusal, and the ink-in-margin refusal. Keep this logic as plain functions over the scene and a described printer, so it runs on every platform's CI.
- **The printed size**, as question 21 describes: print to "Microsoft Print to PDF", rasterise, register, and require the markers within 0.1 mm of their definition coordinates.
- **Find out whether the runner has the printer rather than guessing.** Add a step to the Windows CI job that lists the installed printers. If "Microsoft Print to PDF" is there, the size test runs on CI. If it is not, the test skips with that reason named, and the write-up gives Alan the one-time manual check step by step: what to click, where the file goes, and the exact `grouplab measure` command to run on it.

**The confirmation wording** follows question 21: the printer, the sheet, the page count and "at actual size", and it says the job was sent to the print queue, never that paper came out.

---

## 2026-09-19, entry 106: entry 105 section 9 was folded as actioned and not done, question 20 answered, the "300" cartridges still read as 0.300, the full calibre list generated from the code, and printing from inside GroupLab

**Status: actioned 2026-09-19**, sections 1 to 4. **Section 5 is not built: it is scoped and raised as question 21**, with its plan, its cost and the three decisions it needs.
- **Section 1:** entry 105 section 9 built: Open to print opens the PDF in the viewer on every platform with no print verb, a confirmation dialog says to print at actual size, and the launch is logged. Entry 105's status line is corrected, and `NotesStatusTests` checks any "all N sections" claim against the entry.
- **Section 2:** question 20 answered, option B: three brand roles in each palette at the files' values, the mark drawn from them, files and roles held to each other and to 3:1, the tightest light rings on the light background at 3.06:1.
- **Section 3:** every diameter checked and correct; the .300s, 280s, Noslers, 7mm Rem Mag, the hyphenated .30s, 50 BMG, 45-70, 7.62x51 and 7.62x39 read as their bullets, and the bare 30, 7.62 and 50 still ask.
- **Section 4:** `grouplab calibres` writes `docs/CALIBRES.md` and `docs/CALIBRES.pdf` from the code, and a test holds the committed list to it.
- `docs/PHASE1-RESULTS.md` "Entry 106".


### 1. Entry 105 section 9 is in the log, marked actioned, and not built

**The log's status line for entry 105 reads "All eight items, with one conflict raised as question 20."** The entry has nine sections. Section 9, the Print button, was folded into the log with the rest, and `PrintWindow.PrintLaunch` still starts the PDF with `Verb = "print"` at line 574. **So the log says a defect is dealt with that is still in the build.** That is the same class of error as question 15's status line, the one `QuestionStatusTests` now guards against, one file over.

**The cause is mine.** I appended section 9 to the inbox file after writing a covering command that listed the sections by number and said "all eight". The command and the entry disagreed, and the command won. **From here my covering commands do not enumerate sections; they say to action every numbered section of the entry, and the entry is the only list.**

**What to do.**
- **Action entry 105 section 9 now, as written there.** Its text is in the log. The short version: stop using the shell print verb on every platform, open the PDF in the viewer, say in the status line to print from there at Actual size, rename the button for what it does, and log the launch on success. **Do not build an in-app print path under it.**
- **Correct entry 105's status line** to say that section 9 was not actioned with the rest and was actioned under entry 106. Leave the rest of the entry as it is.
- **A guard, if it is cheap:** when an entry is folded as actioned, its status line either says every section was done or names the ones that were not. A test can count an entry's numbered `###` sections and fail when an actioned status line quotes a smaller number. **If status lines are too free-form for that to hold, say so and skip it.** The real fix is the covering command, which is now mine to get right.

**It did print, silently.** After this section was written, Alan reported that the sheets he tried to print came out of his printer, with no dialog and nothing on screen to say they had been sent. **That is the worst outcome section 9 described:** a target printed with nobody choosing Actual size, at whatever scaling his PDF program uses by default, and the screen's green line claiming a dialog would follow. He is measuring those sheets before shooting any of them.

**Two changes to section 9 as written, from Alan:**
- **He wants a confirmation when something is sent.** On the viewer path GroupLab only opens a file, so the honest confirmation is a dialog saying exactly that: "The target is open in your PDF viewer. Print it from there, choosing Actual size or 100 percent, never Fit." The status line alone is not enough; he missed it.
- **This path is the interim, not the end state.** Section 5 is the print path he chose. Build section 9's viewer path now anyway, because it stops silent printing today.

**Why it matters before the weekend.** Alan is about to print sheets for a measurement session. Until this lands he has been told to use Save PDF and print from his viewer.

### 2. Question 20 answered: B, and the error was mine

**B.** The tokens gain the mark's colours as brand roles in each palette, set to the committed files' values, the mark is drawn from them, and a test holds the files and the tokens to each other.

**How the conflict arose.** Entry 105 section 4 called `#a9660f` "the light theme's amber" and said the greys were existing tokens. I had read the dark palette in `Tokens.cs` and not the light one, and I never checked the greys at all. Drawing the mark from the committed files' own colours until this was answered was the right call.

**The roles**, names at your discretion:

| Role | Dark, also high contrast | Light |
|---|---|---|
| Mark rings | `#6b727b` | `#8f8b83` |
| Mark holes and LAB | `#e0912f` | `#a9660f` |
| GROUP | `#8a9199` | `#6f6b64` |

**The contrast rule that applies to them is not the body-text rule.** `ThemeTests` holds the text tokens to body-text contrast, which is why light `Amber` moved to `#965d12`. The mark is only ever drawn at header size and larger, and the rings are a graphic, so hold the brand roles to 3:1 against the panel each theme puts behind them. **By my arithmetic they all clear it**, the tightest being the light rings on the light background at about 3.1:1. The test should compute this rather than trust my figure.

**Keep the brand roles out of general use.** They exist for the mark. If `Amber` and the mark's amber ever drift apart in the light theme, that is intended: one is tuned for text, the other is the logo.

### 3. The "300" cartridges, and a few others, still fall through to the old guess

**Entry 105 section 7 is done as specified, and I specified too little.** A name that matches no table row or cartridge key still reaches the leading-number rule, and that rule reads a cartridge name as a diameter. I traced `Calibre.Read` at `471f91c` by hand for common names:

| Typed | Reads as | Bullet |
|---|---|---|
| **300 Blackout** | 0.300 | **.308** |
| 300 Win Mag, 300 PRC, 300 WSM, 300 Norma | 0.300 | .308 |
| 280 Rem, 280 Ackley | 0.280 | .284 |
| 28 Nosler | 0.280 | .284 |
| 26 Nosler | 0.260 | .264 |
| 30-06, 30-30 | asks: .308 or .309 | .308 |
| 50 BMG | asks: .500 or .510 | .510 |

**The first row is the cartridge Alan's friend shot on the sheet the whole analysis screen has been built against:** 300 Blackout, 220 grain subsonic. Typed by name, it gets an edge-to-edge extreme spread 0.008 in short and a snap radius and oversize threshold for a bullet that does not exist. `Calibre.cs` already says in its own comment that ".300 Win Mag fires a .308 bullet"; the table just does not act on it.

**Add cartridge keys for these, as the `Cartridges` list already does for .223 and .270:**
- **Any name beginning "300"** resolves to .308. Every .300 cartridge in common use fires a .308 bullet: Blackout, AAC, Win Mag, WSM, PRC, Norma, Weatherby, H&H, RUM, Savage.
- **"280"** resolves to .284, and so do **"28 Nosler"** and **"7mm Rem Mag"**.
- **"26 Nosler"** resolves to .264.
- **"30-06", "30-30" and "30-40"** resolve to .308. The hyphenated name is the cartridge; a bare "30" stays ambiguous as now.
- **"50 BMG"** resolves to .510, and **"45-70"** to .458.
- **"7.62x51"** resolves to .308 and **"7.62x39"** to .310, Alan's table value for 7.62mm. A bare "7.62" stays ambiguous as now.

**Check each diameter before committing it.** These are standard bullet diameters as I know them. The principle matters more than my list: a cartridge name is not its bullet diameter, and where the name is common, the table should know that rather than guess.

**Tests:** each name above reads as its diameter, and "30", "7.62" and "50" alone still return candidates.

### 4. A complete list of every calibre and cartridge GroupLab knows, generated from the code

**Alan wants to see the whole list**: every name the calibre input recognises and the diameter it gives. **Build this after section 3**, so the list includes the names section 3 adds.

**Generate it; do not write it by hand.** A hand-written list is out of date the first time someone edits `Calibre.cs`. Follow the pattern `grouplab icons` just set:
- **A command, `grouplab calibres`**, prints the list from `Calibre.Table` and the cartridge keys as `Calibre.Read` actually resolves them, and writes it to `docs/CALIBRES.md`.
- **A test fails when the committed `docs/CALIBRES.md` differs from what the command produces**, the way the gate record's tables are held to their committed copies. The list then cannot drift from the code.

**What the document holds**, in this order:
1. **Every table row:** the name, the diameter in inches to four places, the diameter in millimetres, and whether Alan's list gave it as rifle, pistol or both.
2. **Every cartridge name and every short key that reaches it**, with the diameter each resolves to. For example: "300 Blackout, 300 BLK, 300 AAC: .308".
3. **Every ambiguous name with its candidates**, so a reader can see which names will ask them to choose. For example: "45: .451, .452, .454, .458".
4. **One paragraph on how typed text is read**, in plain words: a typed diameter always wins, a known name gives its diameter, an ambiguous name asks, and anything else falls to the leading-number guess, which says what it read.

**Also produce it as a PDF**, `docs/CALIBRES.pdf`, since the project rule is that documents meant for reading ship as docx or PDF as well as Markdown. **The Markdown is the source; generate the PDF from it** in whatever way the repository already produces its other PDFs.

**Paste the list into your report as well**, or at least its first two parts, so Alan can read it without opening the repository.

### 5. Printing from inside GroupLab, which Alan chose

**Alan's decision:** the Print button prints from inside GroupLab. GroupLab shows the real print dialog, sets actual size itself so no sheet can come out shrunk, and confirms when the job has been sent. I offered this beside the viewer path and a popup on the current silent print. **He chose it knowing it is the larger job.**

**Why it is the right end state and not only a nicer button.** Every other path hands scaling to a program GroupLab cannot see, and the print screen's warning exists because that goes wrong: a sheet printed at 97 percent measures 3 percent small. **Printing it ourselves is the only way GroupLab can guarantee the size.**

**Scope it first, then decide.** If the plan is clear, has no open design decision and fits one run with its tests, build it. Otherwise raise it as a question with the plan and its cost, and leave section 9's viewer path in place meanwhile. Either way, say which.

**What it must do.**
- **Draw from the target definition with the existing renderer, at the printer's resolution, as vector.** Do not rasterise the PDF. One dmm in the definition is one dmm on the paper, and the renderer already draws in those units.
- **Place the sheet on true page coordinates.** A printer's drawing origin is its printable area, not the paper's edge, so offset by the printer's hard margins. **If any artwork, marker or code falls inside the unprintable margin, refuse with the reason** rather than print a sheet that cannot register.
- **Never scale.** Do not offer a scaling choice. If the paper size in the dialog is not the sheet's page size, for example a Letter sheet on A4, say so and refuse rather than fit.
- **The real print dialog**, for the printer, copies and pages. Candidates on Windows: `System.Drawing.Printing`, which is Windows-only and would sit behind an OS check; Win32 `PrintDlgEx` with GDI through P/Invoke; or the WinRT print manager through its desktop interop. Weigh them and say why you chose one.
- **Confirm honestly after the job is spooled.** A dialog naming the printer, the sheet, the page count and "at actual size". GroupLab can know the job reached the print queue. It cannot know the paper came out, so do not say it did.
- **Linux and macOS:** printing through CUPS with scaling disabled, or keep the viewer path there. Your call, stated in the write-up.

**The test that matters:** print a sheet through the new path to a PDF printer, "Microsoft Print to PDF" if the Windows CI runner has one, and check that the markers in the output sit at their definition coordinates to within 0.1 mm. **That checks the size is right, not merely that a job was sent.** If no runner has a PDF printer, say so and describe how Alan can run the same check once by hand.

---

## 2026-09-19, entry 105: the side columns resizeable, the figure panel made readable, the plot's marks identified on hover, the work bar on a toggle, an application mark, the calibre names read as their real diameters, sighters ignored unless asked for, and a Print button that cannot promise a dialog

**Status: actioned 2026-09-19**, sections 1 to 8, with one conflict raised as question 20. **Section 9 was not actioned with the rest**: it was added to the inbox file after the file was read, and this line counted eight items where the entry has nine. It was actioned under entry 106 section 1.
- **Item 1:** the form fits 372 px on its own, every row wraps, and both states have splitters with a resize cursor, limits and remembered widths.
- **Item 2:** sentences in the sans, one shape for all five figure rows with the angle beneath, the zero readouts in aligned columns with the verdict at full strength, and ruled headings. No finding reworded.
- **Item 3:** the plot names what is under the pointer and each shot's bull, in the plot and the table; every shot has one outline and a halo; the legend is a key with swatches, CEP 50 dotted and CEP 90 dashed.
- **Item 4:** the four SVGs are in `src/GroupLab.App/Assets/`, drawn as committed, the lockup in the header and the mark in the rail. Question 20: the light amber and the four greys are not tokens, so the mark keeps the files' colours until you choose.
- **Item 5:** `grouplab icons` draws the `.ico`, the Linux PNGs and the `.icns` from the mark at each size; the csproj and the window set the icon.
- **Item 6:** Show work toggles the bar in both states and is remembered, defaulted closed. A failed stage stays a prominent error and turns Show work red.
- **Item 7:** the calibre table, every row tested by name and by diameter; nine ambiguous names return candidates and never one.
- **Item 8:** sighters are set aside unless analysed; a sighter-to-scoring contest is always raised; analysed, they are their own group. `DESIGN.md` sections 13 and 14 record it.
- `docs/PHASE1-RESULTS.md` "Entry 105".


Nine things, from Alan running the build at `c2bed06` and sending two screenshots. **Items 1, 6, 7, 8 and 9 are defects or near enough. Item 2 is the one he asked for most directly. Item 5 waits on item 4, which waits on him.**

**I am not committing this file.** My shell cannot delete, so every commit I made left `index.lock`, `HEAD.lock` and `maintenance.lock` behind for you to clear, and one of them got pushed by accident. From here the planning session writes the inbox file and nothing else, which is what `docs/notes/inbox/README.md` says anyway.

### 1. The right column clips its own contents, and both side columns should be draggable

**The defect first, because a splitter alone does not fix it.** On the marking screen, expanding **New rifle, barrel or load** cuts off the right edge: the button reads "Add rif". `Tokens.RightColumnWidth` is 372 and the expander's form is wider than that. **A person who never touches a splitter still sees a clipped button on first run**, so the form has to fit 372 first: wrap the rows, or stack the name field above its buttons, or shorten the row. Fix that on its own terms.

**Then make both side columns draggable**, as Alan asked: a `GridSplitter` between each side column and the centre, a visible cursor change on hover, a sensible minimum on each side so neither can be dragged shut by accident, and the widths remembered. `AppSettingsStore` already has the pattern to copy in `LoadMoreFigures` and `SaveMoreFigures`.

**This applies to both states.** `analysisBody` and `editorBody` both use a `DockPanel` with fixed-width `Border`s, the left at 300 and the right at `Tokens.RightColumnWidth`. Both become a `Grid`.

**The space is already there.** In the analysis screenshot the composite plot sits in a very wide area with several hundred pixels of empty background on each side, while the figure column is cramped at 372 and wrapping its own numbers. The screen is not short of room; the room is allocated wrong, and a splitter lets Alan settle that himself rather than us guessing a constant.

### 2. The figure panel is hard to read, and most of it is fixable by rules the project already wrote down

Alan says the information on the right needs more structure, formatting and clear labelling. He is right, and here is what the screenshot actually shows, worst first.

**a. Prose is being set in monospace, against `DESIGN.md` section 19.** The rule is:

> A neutral UI sans for chrome and labels, and a monospace with tabular figures for every numeric readout so digits align in columns and values do not jitter as they update.

These are sentences, not readouts, and every one of them is in mono in the screenshot:

> give or take 0.234 in 0.89 MOA across and 0.142 in 0.54 MOA up and down, at 95 percent

> each axis on its own, 9 degrees of freedom, the group not being circular

> Choose a rifle to have this in clicks. It corrects the zero at the distance shot; moving a zero between distances needs the ballistic solver.

**Mono prose at the secondary size in a 372 pixel column is the single largest readability cost on that panel.** Sentences go in the UI sans. Where a sentence contains a figure, either set the figure in a mono run inside it or accept the sans, but the sentence does not become a readout because it has a number in it. `Detail()` is where this lives, and it is used for both jobs.

**b. The two lead figures break their own layout.** Mean radius renders as `0.344 in` on one line with `MOA` alone on the next, and the label "Mean radius" sits *below* its own number. At the lead figure size, 372 pixels does not hold a number plus two units. **Put the angular value on the detail line beneath with the interval, not beside the lead number**, and put the label above or left of the number, the same way every time.

**c. Five figure rows, three different shapes.** Mean radius and Sigma put the number on a line of its own; Extreme spread, CEP 90 and Group width by height put the label left and the number right. Pick one pattern and apply it to all five. The concept's stack is the guideline here and it uses one.

**d. The zero correction block is five kinds of thing in one voice.** It holds two readouts, an uncertainty, a finding in bold, a statistical note about degrees of freedom, and an instruction about choosing a rifle. Separate them by what they are: the readouts as readouts, the finding as the block's verdict, and the two notes quieter or behind a disclosure. The finding is the line that matters and it is currently the same size as the note beneath it.

**e. The section headings are the only structure and the least visible thing on the panel.** `ZERO CORRECTION` and `GROUP` are small dim capitals. They are carrying the whole hierarchy. Give them enough weight, or a rule above them, to actually divide the column.

**f. The two zero readouts do not align.** `0.097 in  0.37 MOA left` over `0.060 in  0.23 MOA high`: the linear and angular values start at different x positions because the line is laid out as text. These are exactly the tabular figures section 19 asks for, in columns.

**None of this changes a number or a word of the statistics.** It is typography, alignment and grouping. Do not take it as licence to reword the findings, which are the product of entries 91, 92, 103 and 104.

### 3. The composite plot should say what the cursor is over

Alan wants a tooltip naming what is under the pointer: which shot and which bull it came from, what the dashed circle is, and so on. **Most of what this needs is already in `CompositePlot`.** `Pick(Point)` resolves a shot within its drawn circle and the extreme-spread line within six pixels; `PlotDisc`, `Cep50Inches`, `Cep90Inches`, `Centre` and `SpreadPair` are all held on the control.

**What to add.**
- **A `Describe(Point)` beside `Pick`**, resolving in the same priority order and then continuing past it: a shot, then the extreme-spread line, then the group centre, then the CEP 50 and CEP 90 circles within a few pixels of the stroke, then the bull's rings, and nothing when the pointer is on empty paper. Update `ToolTip` on pointer move.
- **`PlotShot` does not carry the bull, and Alan asked for it by name.** It is `(Id, Label, Offset, Excluded)`. Add the bull so the tooltip can read, for example, "Shot 3, bull 14. 0.591 in across, 0.303 in down, 0.778 in from the group centre." **The offset table on the left has the same gap**: its columns are shot, across, up/down, radius, and a reader cannot get from a row back to a bull on the sheet. Add the bull there too.
- **Say what a thing is, not only its value.** "CEP 90: half of a very large number of shots from this rifle would land inside this circle nine times in ten" is the sort of line that earns a tooltip. The figure is already in the stack; the tooltip's job is to connect the ring on the screen to it.
- **An excluded shot says so in its tooltip**, since it is drawn differently and the difference needs a name.

**Two things the screenshot shows that a tooltip does not fix.**
- **The same shot mark reads as two different objects depending on what is behind it.** Shots over the bull's white disc render light pink; the two outside it render dark maroon on the dark background. They are the same kind of thing and must look it. Give every shot the same outline weight and colour and a halo behind it, the way `Marks.MarkHalo` already does on the marking canvas.
- **The legend is text with no swatches**, set small and dim, centred at the bottom of a wide empty area, far from anything it names. A reader cannot tell which of the two dashed circles is CEP 50. Make it a key: the mark as drawn, then its name, beside the plot rather than stranded under it. **A tooltip helps the person who already suspects there is something to hover over. The key is what tells them.**

### 4. The application mark: Alan chose A

**Decided.** Two concentric rings in the neutral grey and three overlapping amber holes sitting up and to the right of centre, overlapping enough that the middle of the group is solid. It was chosen from eight candidates and then six combinations, each drawn large, at 32 and 16 pixels, and on both themes. **The off-centre group is the idea**: GroupLab exists to show where a rifle actually hits against where it was aimed.

**The source, exactly as chosen.** Commit it as `src/GroupLab.App/Assets/grouplab-mark.svg`, and the light variant beside it:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <title>GroupLab</title>
  <circle cx="50" cy="50" r="40" fill="none" stroke="#6b727b" stroke-width="6"/>
  <circle cx="50" cy="50" r="21" fill="none" stroke="#6b727b" stroke-width="6"/>
  <circle cx="61.94" cy="32.75" r="11" fill="#e0912f"/>
  <circle cx="52.75" cy="41.94" r="11" fill="#e0912f"/>
  <circle cx="65.31" cy="45.31" r="11" fill="#e0912f"/>
</svg>
```

**The light variant** is the same geometry with the rings at `#8f8b83` and the holes at `#a9660f`, the light theme's amber.

**The wordmark: GROUP in grey, LAB in amber, beside the mark.** Alan chose this after seeing other treatments. It is set in Poppins Bold, **converted to outlines**, so the lockup needs no font at runtime and nothing new is bundled. Poppins is under the SIL Open Font License, which permits that. Its cap height is 44 percent of the mark's height, centred on it.

- **Dark theme:** rings `#6b727b`, holes and LAB `#e0912f`, GROUP `#8a9199`. **GROUP is deliberately lighter than the rings.** At the ring grey, the word measures about 3.5:1 on the panel colour, which holds for large bold text and turns faint at header size. This was a choice made while drawing it, not something Alan asked for, so it can go back to the ring grey if he prefers one grey.
- **Light theme:** rings `#8f8b83`, holes and LAB `#a9660f`, GROUP `#6f6b64`, darker than the rings for the same reason in reverse.

**Four files are delivered beside this entry in the inbox**, and they are the source of truth. Move them into `src/GroupLab.App/Assets/` and delete them from the inbox with the entry:

| Inbox file | Becomes |
|---|---|
| `entry-105-grouplab-mark-dark.svg` | `grouplab-mark.svg` |
| `entry-105-grouplab-mark-light.svg` | `grouplab-mark-light.svg` |
| `entry-105-grouplab-lockup-dark.svg` | `grouplab-lockup.svg` |
| `entry-105-grouplab-lockup-light.svg` | `grouplab-lockup-light.svg` |

**The header uses the lockup**, at about 26 pixels tall, in place of the plain GroupLab crumb, and the breadcrumb continues after it. **The icon uses the mark alone**, since a word cannot survive at 16 pixels.

**Where it goes in the application.**
- **The header**, as the lockup above, taking the place of the amber "GL" and plain title the concept shows.
- **The rail's top slot**, where `\u25c9` stands in today.
- **The window icon**, per item 5.
- **The colours are tokens**, not literals: the grey is the rail and ring grey and the amber is `Tokens.Amber` in each theme, so the in-app mark follows the theme. The icon files cannot follow a theme and use the dark variant, whose grey and amber both hold contrast on a light taskbar as well as a dark one.

**`DESIGN.md` section 19's identity rule holds**: the mark is GroupLab's own, not a Pissin Hot Precision mark.

### 5. The executable has no icon at all

There is no `.ico` in the repository and no `ApplicationIcon` in `GroupLab.App.csproj`, so the build ships the default. Once item 4 lands:

- **Windows:** an `.ico` carrying 16, 24, 32, 48, 64 and 256 pixel images, each one drawn at its size rather than downsampled from the largest, because the 16 pixel image is the one Windows shows in the taskbar and the title bar. Set `ApplicationIcon` in the csproj and the window's own `Icon`.
- **The other two platforms, since CI builds them:** a PNG set for Linux and an `.icns` for macOS, from the same SVG.
- **The source of truth is the SVG**, committed, with the raster sets generated from it by a script in the repository rather than hand-exported, so the mark can be changed in one place.
- **A test that the icon files exist and that the window sets one**, in the manner of the other App tests.

### 6. The work bar should be on a toggle, not always present

The stage timeline is docked at the bottom permanently and takes a strip of vertical space on both screens. Alan wants it shown and hidden by a button in the top bar. **The button already exists**: the analysis state's header carries **Show work**, which is exactly the right control and currently does something else or nothing.

- **Show work toggles the bar**, the button shows which state it is in, and the choice is remembered in `AppSettingsStore` alongside `moreFiguresOpen`.
- **Give the editor state the same control.** The first screenshot is the marking screen and the bar is there too.
- **One rule from `DESIGN.md` section 19 must survive this**, and it is the reason to be careful:

> the trace must never be the only place an error appears, so a failed stage produces a normal prominent error with the trace as the detail behind it

**So a hidden bar must never hide a failure.** When a stage fails while the bar is collapsed, the error appears where errors appear, and the bar should draw attention to itself rather than stay silent. Test that a failed stage is visible with the bar hidden.

**Default it open or closed as you judge**, but say which you chose and why in the write-up.

### 7. The calibre input reads 36 of 44 common calibre names as the wrong diameter

**Alan sent two lists of bullet diameters and asked that every one be accepted by the calibre input:** 31 rifle and 13 pistol, 44 rows in all, 42 distinct name and diameter pairs once the two shared rows are counted once. They are in the table at the end of this section.

**I ran every row through a transcription of `Calibre.Parse` as it stands at `c2bed06`.** Typed as a diameter, all 44 read correctly: the numeric path is sound. **Typed as the name a shooter would type, 36 of 44 read wrong.** The pick list has twelve entries, so almost everything falls through to the leading-number rule, and that rule reads a calibre name as if it were a diameter, which it usually is not.

| Typed | Reads as | Actually | Error, in |
|---|---|---|---|
| 38 Cal. | 0.380 | 0.357 or 0.358 | **+0.023** |
| 32 Cal. (pistol) | 0.320 | 0.312 | +0.008 |
| 44 Cal. | 0.440 | 0.430 | +0.010 |
| 7.62mm | 0.300 | 0.310 | −0.010 |
| 400 Cal. | 0.400 | 0.410 | −0.010 |
| 50 Cal. (rifle) | 0.500 | 0.510 | −0.010 |
| 6.5mm | 0.2559 | 0.264 | −0.0081 |
| 30 Cal. | 0.300 | 0.308 | −0.008 |
| 270 Cal. | 0.270 | 0.277 | −0.007 |

and so on through the list; only 338, 9.3mm, 375, 416, 423, 505, 41 and pistol 50 happen to come out right.

**This is not cosmetic.** The calibre drives three things (entry 24 section 5): edge-to-edge extreme spread, which adds one diameter, the snap radius, and the flag on a hole too large for the calibre. **Somebody who types "38" gets an edge-to-edge figure 0.023 in too large and an oversize threshold set for a bullet that does not exist.** The input does tell them what it read, "Read as a 0.380 in bullet diameter", which is honest, but a shooter who typed their calibre has no reason to doubt it.

**Eight names mean more than one diameter, so no parser can get them right from the name alone:**

| Name | Diameters |
|---|---|
| 30 Cal. | 0.308, 0.309 |
| 303 Cal. | 0.3105, 0.312 |
| 32 Cal. | 0.312, 0.321 |
| 35 Cal. | 0.355, 0.357, 0.358 |
| 38 Cal. | 0.357, 0.358 |
| 45 Cal. | 0.451, 0.452, 0.454, 0.458 |
| 50 Cal. | 0.500, 0.510 |
| 9mm | 0.355, 0.356 |

**And one the existing pick list already disagrees with.** `Common` has `".308, 7.62 mm"` at 0.308, which is right for 7.62x51. Alan's list has `7.62mm` at 0.310, which is right for 7.62x39. Both are real, so **7.62 is a ninth ambiguous name** once both are in the table.

**What to build.**
- **The pick list becomes the full table**, every pair below, each shown as its name and its diameter together, "35 Cal. .357", because the diameter is the only thing that tells two 35s apart. Rifle and pistol can be grouped or merged; the diameter is what matters.
- **Keep the existing cartridge entries** (".17 HMR", ".22 LR", ".223 Rem, 5.56 NATO", ".270 Win" and the rest) as names that resolve to the same diameters. Nobody should lose a name they already use.
- **A name that means one diameter resolves to it.** "270", "6.5mm", "6.5 mm", "8mm", "10mm", "9.3mm", "5.45" should all land on the table's diameter. Match tolerantly: case, a space before "mm", "Cal" with or without the full stop, a leading point.
- **A name that means several diameters does not pick one.** It shows the candidates with their diameters and asks. Silently choosing the commonest is exactly the error this section exists to remove, just rarer.
- **A typed diameter always wins**, read exactly as now. All 44 already read correctly that way.
- **The leading-number rule stays as the fallback** for text that matches nothing, which is where the wildcats entry 24 section 5 cared about will land, and it keeps saying what it read.
- **Tests: every one of the 44 rows**, once typed as its name and once as its diameter, and each of the nine ambiguous names returning candidates rather than a calibre. `CalibreTests.cs` is where these go.

**The table**, from Alan's two lists, diameters in inches:

| Rifle | | | Pistol | |
|---|---|---|---|---|
| 17 Cal. | .172 | | 30 Cal. | .309 |
| 20 Cal. | .204 | | 32 Cal. | .312 |
| 5.45 Cal. | .2215 | | 9mm | .355 |
| 22 Cal. | .224 | | 9mm | .356 |
| 6mm | .243 | | 38 Cal. | .357 |
| 25 Cal. | .257 | | 38 Cal. | .358 |
| 6.5mm | .264 | | 10mm | .400 |
| 270 Cal. | .277 | | 41 Cal. | .410 |
| 7mm | .284 | | 44 Cal. | .430 |
| 30 Cal. | .308 | | 45 Cal. | .451 |
| 7.62mm | .310 | | 45 Cal. | .452 |
| 303 Cal. | .3105 | | 45 Cal. | .454 |
| 303 Cal. | .312 | | 50 Cal. | .500 |
| 32 Cal. | .321 | | | |
| 8mm | .323 | | | |
| 338 Cal. | .338 | | | |
| 35 Cal. | .355 | | | |
| 35 Cal. | .357 | | | |
| 35 Cal. | .358 | | | |
| 9.3mm | .366 | | | |
| 375 Cal. | .375 | | | |
| 400 Cal. | .410 | | | |
| 405 Cal. | .411 | | | |
| 416 Cal. | .416 | | | |
| 423 Cal. | .423 | | | |
| 44 Cal. | .430 | | | |
| 45 Cal. | .452 | | | |
| 45 Cal. | .458 | | | |
| 470 Cal. | .474 | | | |
| 505 Cal. | .505 | | | |
| 50 Cal. | .510 | | | |

### 8. Sighters ignored by default, analysed only when asked for

**Alan's decision:** sighter targets are optional. **By default they are ignored entirely**, and there is an option to analyse them as well.

**Where it stands at `c2bed06`.** Sighters are already kept out of the figures and the plot: `CompositePlot` says "Sighters are never given to it", and the figure count in `MainWindow` leaves them out. **But they are not ignored.** They still generate work and noise everywhere else, which is what Alan has been reporting since his first complaint about "a lot of warnings for the sighter bulls at the bottom":
- **Review items.** `ReviewQueue`'s contested and oversize passes run over every shot. The analysis screenshot shows the result: "1 mark flagged as possibly two holes: Shot S1b covers about 1.4 holes' area", a review item about a shot that affects no figure on the screen.
- **Counts.** The marking screen's header reads "14 shots" and "0 of 14 need review" for a sheet with 10 scoring shots and 4 sighters.
- **The canvas** draws S1a, S1b, S2 and S3 with the same marks and labels as the scoring shots.

**The one thing "ignored" must not mean: do not stop detecting or assigning them.** Sighter holes still have to be found and matched to the sighter bulls, because that is what keeps them out of the scoring bulls. The sighter row sits directly under bulls 21 to 25 on the standard sheets, and the sighter-pooling defect was exactly a sighter shot ending up in the wrong pool. **Drop the sighter pool and a hole under S2 becomes a candidate for bull 22.** So detection and one-to-one matching run over the whole sheet as now, and "ignored" is everything after that.

**By default, with sighters ignored.**
- **No review item that concerns only sighter bulls**: no oversize flag on a sighter mark, no count question, no contested item between two sighters.
- **A contested item between a sighter bull and a scoring bull still appears**, because the scoring bull's shot depends on it. This is the case to test hardest.
- **Counts are scoring shots only**, in the breadcrumb, the review pill and anywhere else a shot count is shown.
- **The canvas shows sighter marks faint and unlabelled, or not at all**, your judgement. They should read as "found and set aside", not as work.
- **The marking still stores them.** Because detection ran, turning the option on later needs no new detection and loses no edits.

**With sighters analysed.**
- **They become their own group with their own figures and are never pooled with the scoring shots.** That is the rule entry 73 section 1's sighter fix established and it holds in both modes.
- **Their own zero readout is the useful part.** Sighters are usually fired to confirm zero before the group, so the offset from aim is what a shooter wants from them. With three or four sighters, `MinimumShotsForDispersion` withholds the dispersion figures, and the zero block's existing "not distinguishable from zero at N shots" answer is the honest result. Nothing new is needed for that.
- **Their review items return**, since they now affect figures on screen.

**The control.**
- **One toggle, "Analyse sighters", off by default**, remembered in `AppSettingsStore` the way `moreFiguresOpen` is.
- **The CLI `analyze` verb follows the same default** with a flag to include them, so the command line and the window agree.
- **A sheet with no sighter bulls shows no toggle**, or shows it disabled with the reason.

**Tests.** With sighters ignored: no review items and no oversize flags from sighter marks, counts that exclude them, and a contested sighter-to-scoring item that still appears. With them analysed: a separate group, never pooled. And turning the option on after marking restores their items without a new detection.

**Record the decision in `DESIGN.md`**, section 13 on assignment and section 14 on statistics, so a later reader knows sighters are ignored by choice and not by oversight.

### 9. The Print button reports a print dialog it cannot guarantee, and on Alan's machine none appeared

**What happened.** On the print screen, with "GroupLab Zeroing Grid, mil at 100 yd" selected, Alan pressed **Print...**. The status line turned green and read "Sent to your PDF viewer's print command. In its print dialog choose Actual size, or 100%." **No print dialog opened.**

**What the code does.** `PrintLaunch` on Windows starts the saved PDF with `UseShellExecute = true, Verb = "print"`, and `Process.Start` returning without an exception is taken as success: the green status is set and nothing is logged. The fallback, opening the PDF in the viewer, runs only when Windows reports that no print verb is registered. **Here a print verb was registered, it ran, and the screen said a dialog would follow.**

**Why that cannot be relied on.** The `print` verb is whatever command the default PDF application registered for it. GroupLab cannot see what that command does. Depending on the application it can show a print dialog, show one behind GroupLab's window or on another monitor while the application itself stays hidden, **print straight to the default printer at the application's own default scaling**, or appear to do nothing. No Windows shell verb means "show the print dialog", and `printto` is no better. **The status line promises a dialog the code has no way to know about.** The worst of those outcomes is the one the print screen exists to prevent: a sheet printed with nobody choosing Actual size, possibly at a shrink-to-fit scale. The screen's own warning says a sheet printed at 97 percent measures 3 percent small.

**What to do.**
- **Stop using the print verb.** The button opens the PDF in the default viewer, `UseShellExecute = true` with no verb, on every platform. The status says what is true: "The target is open in your PDF viewer. Print it from there, choosing Actual size or 100 percent, never Fit." This is what the fallback already does, so the path is proven. It becomes the only path.
- **Name the button for what it does.** "Open to print" or similar, not "Print...". A label that ends in an ellipsis says a dialog is coming.
- **Log the launch on success, not only on failure:** the path, the verb or its absence, and that it returned. Today a successful launch leaves no trace, which is why this report cannot be diagnosed from the log.
- **`PrintLaunch`'s comment and its tests** change with it. The comment documents the verb at length, and entry 61 section 3 and the correction to it recorded in entry 65 both shaped this code, so read them before rewriting it.

**Not now: printing from inside GroupLab.** A print path GroupLab owns would let it set actual size itself, which is the real fix for the 97 percent problem. Avalonia has no printing API, though, so this means Windows' own print APIs and rasterising the PDF at the printer's resolution. **That is a separate decision with a real cost.** If it looks cheaper than I expect, raise it as a question. Do not build it under this item.

**Test:** the launch uses no verb on any platform, and the status line no longer mentions a print dialog.

---

## 2026-09-18, entry 104: question 19 confirmed and my error behind it, the flyer card's exceedance measured, and the render that was never made dark

**Status: actioned 2026-09-18.**
- **Section 1: question 19 closed.** The option C risk is in `DESIGN.md` section 22, pointing at question 15, not scheduled until the mounted gate has real frames. The guard was cheap, because the headings say "question N answered" or "questions 12, 13 and 14 answered" every time: `QuestionStatusTests` fails on a question marked open that a heading names as answered. Run over the two files as they stood before entry 103, it names question 15 and entry 52's heading.
- **Section 2: the card is calibrated by simulation, and the bias is larger than measured here.** The screen divides by the Rayleigh mean radius, not the arithmetic mean your simulation used, and against that statistic the closed form's five percent line is 2.42 at five shots where no group can go past about 1.96. The true lines are 1.733, 2.204, 2.409 and 2.623 at 5, 10, 15 and 25 shots, where the closed form gives p 0.391, 0.199, 0.146 and 0.107. `Flyers.CalibrateWorst` simulates 9,999 groups of the same size, seeded, measured the screen's way, at every count; the card reads its share and mean. No count needed withholding. `docs/STATISTICS.md` section 10 now says its table's MR is the population's and adds the screen's table beside it, with your arithmetic-mean figures named as a third statistic.
- **Section 3: the zero block survived and I had moved it.** It sat below the cards and the flags, off the bottom of the column, where entry 92 had put it above the figures; it is back above them. The fixture now has a distance and a rifle, a test reads its clicks line and its place above the group, and the render is taken in dark and in light, each named after the theme set.
- **Section 4: all four.** The shot table is in shot order; the bull's inked rings are faded over full-strength paper, a judgement, and the paper-on-dark contrast survives; the load panel says ".308, set after detection"; the size flags sit behind one disclosure that counts them.
- `docs/PHASE1-RESULTS.md` "Entry 104".


### 1. Question 19 is right, section 3 was my mistake, and one item survives it

**Confirmed. There is nothing to commit and nothing to regenerate. Close question 19.**

The sort landed on 15 September in the commit titled "Entry 52: sort the markers before use, and regenerate every record and quoted figure with them". `SheetMeasurer` takes both detection passes through `InIdentifierOrder()` at lines 183 and 194, and `PageRegistration` at line 63. `docs/PHASE0-RESULTS.md` section 4.5 already carries the before and after, and it carries better figures than the ones I quoted: the benchmark is 0.018 to 0.113 in and 8 to 22 of 25, against 0.015 to 0.091 in and 8 to 21 before the sort.

**How I got it wrong.** I read question 15's status line, saw `open`, and treated it as live. I then built an argument that entry 101 had made this the cheap moment to do work that had already been done four days earlier, and quoted the pre-sort figures as current. **One grep for `InIdentifierOrder` would have settled it, and I had the repository open and grepped it several times in the same turn.** This is the same failure as entry 64, where I reported a git setting I had not looked at. Stopping was the right call and I would rather see a question than a worked-around instruction.

**A guard worth having, because a status line is a claim like any other.** Entry 52's own heading contains "question 15 answered". A test over the two documents can fail when any question marked `Status: open` is named as answered by a `NOTES-FROM-PLANNING.md` entry heading. That is a string match over two files and it catches this exact case in both directions. **Build it if it is genuinely that cheap; if the headings are too loose to match reliably, say so and skip it rather than inventing a convention to satisfy a test.**

**The one real item from section 3, now confirmed: add the option C risk to `DESIGN.md` section 22.** RANSAC's random consensus moving the printed figures with an input that should not matter is a fragility the sort made repeatable rather than removed, established by "Entry 52 sections 3 and 4" and untouched by entry 101, which left the inlier choice native. Documents only, no regeneration, and it points at question 15. It is not scheduled for change: the case where it bit hardest is the mounted one, whose gate has no proven material, so the time to weigh a deterministic robust fit is after the mounted gate has real frames.

### 2. The flyer card's exceedance is a known-parameter formula fed a sample ratio, and I measured what that costs

**The card is right at 25 shots and wrong enough to fix at five.** This is not a criticism of the change, which is an improvement on what entry 103 asked for. It is the one assumption underneath it.

`Flyers.ProbabilityWorstBeyond(n, multiple)` computes `1 − (1 − exp(−r²/2))ⁿ` with `r = multiple × √(π/2)`. That is exact for the worst of `n` Rayleigh radii **measured against a known mean radius**, and it reproduces `docs/STATISTICS.md` section 10's table to the digit: 0.669 at `n = 25` past two mean radii, 0.357 at `n = 10`. **The screen feeds it the worst shot measured against the sample mean radius, computed from the same shots, about the sample centroid.** The worst shot inflates the denominator it is being judged against, and the centroid is fitted to the same points.

**Simulated, 400,000 replications per cell, circular bivariate normal, radii from the sample centroid as the application computes them.**

| n | closed form flags beyond | truth flags beyond | closed-form p at the true 5 percent cut |
|---|---|---|---|
| 5 | 2.416 MR | 2.109 MR | **0.143** |
| 10 | 2.592 MR | 2.474 MR | 0.079 |
| 15 | 2.689 MR | 2.621 MR | 0.066 |
| 25 | 2.807 MR | 2.775 MR | 0.057 |

**The direction is conservative and the size depends on `n`.** In the upper tail, where the card operates, dividing by a mean the maximum itself inflates compresses the statistic, so the card under-flags: it says "not a flyer" where the true null says one time in twenty. At 25 shots the cut is out by 0.03 mean radii and nobody would notice. **At five shots the card is asking for a p of 0.05 and getting 0.143, so it stays silent on shots a circular group of five produces about three times as often as the label implies.** Five-shot groups are the most common thing anybody shoots.

**The bias is not one-directional across the whole range**, which is worth knowing before anyone patches it by eye: at 1.5 mean radii and `n = 25` the sample ratio exceeds *more* often than the closed form, 0.9992 against 0.9907. The lower tail stretches as the upper tail compresses.

**What to do, following the precedent that already exists.** `docs/STATISTICS.md` section 7 uses a likelihood-ratio test above `n = 20` and a simulation calibration below it, for the same reason. Do the same here: **calibrate the worst-shot exceedance by simulation against the sample mean radius about the sample centroid**, in the manner of `RangeStatisticsSimulation` and its committed table, and keep the closed form where it is exact. Two document changes go with it:

- **Section 10's table must say which mean radius it means.** The column reads `P(worst > 2 × MR)` and MR there is the population value, `σ√(π/2)`. That ambiguity is what let a known-parameter formula be fed a sample ratio without anybody noticing, including me when I specified the card.
- **Keep the sentence the table's dialog text is built on.** The point of section 10 is that a shooter who calls anything past twice the mean radius a flyer discards an ordinary shot two times in three. Nothing here changes that; it sharpens where the card's own threshold sits.

**Treat this as a measured finding, not an instruction to rewrite the card.** If the calibration turns out to cost more than it is worth below some `n`, withholding the verdict there is an honest answer too, in the manner of `DispersionWithheld`.

### 3. Nobody has seen the analysis state in dark, including me

`out/screens/analysis-state-dark.png` renders in the light theme. **`AnalysisStateTests` never calls `SetTheme`**, where `ScreenshotTests` loops over `ThemeChoice.Dark` and `ThemeChoice.Light` and names each file after the theme it set. So the file name asserts a theme the test did not set.

This matters more than a file name. **The concept is drawn as dark chrome around a paper-white document, and that contrast is most of its character.** A light render cannot be compared with it, so the comparison the screenshot exists to invite cannot actually be made. Capture both, the way `ScreenshotTests` already does, and name each after the theme that was set.

**And the render is missing the one thing Alan asked to be prominent.** He asked for the scope offset to be defined separately from the group statistics and easy to find, and entry 92 put the zero block above them for that reason. In this render there is no zero block at all: the offset appears only as "Centre from aim: 0.021 in right, 0.013 in high", a small secondary line inside the GROUP block, smaller than every figure beside it and smaller than the concept's own "Offset from aim" row.

**Check both halves of that before changing anything.** First, whether the zero block survived the split into the analysis state, or whether `Zeroing.For` simply returned nothing because the fixture sets no distance, no rifle and no point of aim, in which case the fallback line should still have appeared and did not. Second, **the fixture**: give the render a distance and a rifle so the block is exercised, because a screenshot that omits the feature Alan singled out is not the screenshot to compare against the concept. If the block is present and correct and only the fixture is bare, say so and fix the fixture alone.

### 4. Four smaller things the render shows

- **The shot table is not in shot order.** Its first column is headed `shot` and reads 2, 5, 1, 4, 3, 6, 10, 9, 7, 8, and so on, which is detection order. A column headed with a number is read as sorted by it. Sort by shot number, or head the column with what the order actually is.
- **The bull's outer ring dominates the composite plot.** It is the heaviest mark on the screen and the data sits inside it. The artwork is context and should recede behind the shots rather than compete with them; the concept draws it lighter. This is a styling judgement, so treat it as one.
- **One screen says two things about the calibre.** The LOAD panel reads `Calibre .308` and the status line reads "detected without a calibre, so whether a mark was one hole or two was judged by its shape alone". Both may be true if the calibre was set after detection, but read together they contradict. Say when the calibre was set, or say that detection ran before it.
- **The oversize notes stack under the cards** and there are two already. With several they will push the judgements out of view. Give them a bound or a disclosure.

---

## 2026-09-18, entry 103: the analysis screen split from the editor, the composite plot, two concept errors not to inherit, and questions 15 and 18 answered

**Status: actioned 2026-09-18.**
- **Section 1: one destination, two states.** The editor goes forward by Accept and analyse, the amber primary, with Discard edits beside it, which now asks first; the sheet crumb comes back with every edit intact; the pill is the review count in one state and the registration and its residual in the other. Accepting with items open puts an amber line above the figures naming how many. The composite plot draws one bull from the definition with every scoring shot from its own aim point, sighters left out, excluded shots hollow, not-a-shot marks absent, calibre circles or points by the legend, the centre with CEP 50 and 90 dashed, and extreme spread as the line between its two shots, which a click picks together. CEP and width by height join the stack after the existing figures, whose order is unchanged.
- **Section 2: the two judgements are cards.** The round card takes the circularity verdict and names the likelihood-ratio test; stringing is a separate labelled line by Pitman-Morgan, and when it finds nothing it says what the shot count could have caught, from section 7's power table. The flyer card keeps "by that measure alone". Tests assert each.
- **Section 3: held, as question 19.** The sort it asks for was committed by entry 52, which answered question 15, and section 4.5 already carries the before and after figures. The `DESIGN.md` section 22 risk for option C is the one thing still to do, and it waits on your confirmation there.
- **Section 4: in `DESIGN.md` sections 3 and 21 and the README.** "Assisted" is the snap, with a feature line marked Done; blank-paper detection is Phase 4, Not started, its gate naming the photograph it needs; the designer's deferral now carries the bought-target promise too. No range list exists in the repository to add blank paper to, so the material is named in the gate and the feature line.
- **Section 5: both README claims corrected, and the guard built.** macOS reproduces the record and the paragraph says packaging is what remains; the Concept screens paragraph says what exists and ends in one "Not built yet:" sentence that `ReadmeTests` checks against the Planned section's states.
- Questions 15 and 18 are marked answered. `docs/PHASE1-RESULTS.md` "Entry 103".


Alan cannot print, shoot, scan or photograph anything before the weekend, so this entry is deliberately all work that needs none of that. He has also said the concept screenshots are the design guideline and that the assignment editor is the beginning of the process and what to work towards.

**Order for this run.** Sections 1, 2 and 5 are the work. Section 4 is documents only and is cheap. **Section 3 regenerates committed evidence and belongs in its own commit, after the rest, and it is fine for it to wait for the next run.** Do not let the regeneration sweep eat a run that was meant for the screen.

### 1. The one screen becomes the two the concept shows, and the composite plot goes in the space that makes

**What is there now.** `MainWindow` is one 2161-line screen that marks, reviews and reports figures at the same time. It already carries the concept's chrome from entry 93: the rail, the breadcrumb header, the review pill, the tool strip with keycaps, the paper sheet on dark chrome and the accents.

**What the concept has instead.** Two screens behind one rail destination. `docs/figures/screens/assignment-editor.png` is the first: the sheet, the tool strip, the needs-a-decision card, the selected detection, the review queue, and **Discard edits** and **Accept and analyse** at the top right. `docs/figures/screens/analysis-dark.png` is the second: the composite plot in the centre, the sheet thumbnail and the load block on the left, the figure stack and the judgement cards on the right, and **Show work**, **Export** and **Report** at the top right with a registration pill where the review count was.

**Both concepts show the rail's first icon active, so this is one destination in two states, not two destinations.** The rail stays as entry 93 built it.

**What to build.**

| | |
|---|---|
| **The split** | The marking and review controls are the editor state. The figures, the plot and the judgements are the analysis state. One window, one document, two states |
| **Forward** | **Accept and analyse**, amber primary, top right of the editor |
| **Back** | The breadcrumb's sheet crumb returns to the editor with every edit intact. A person who sees a figure they distrust must be able to go and look at the mark behind it in one click |
| **Discard edits** | Beside it, and it asks before discarding |
| **The pill** | Review count in the editor, registration and its residual in the analysis, as the two concepts show |

**One rule the concept does not show and the screen needs.** A person may accept with items still open. When they do, **the analysis state carries an amber line naming how many decisions were left unmade**, because every figure below it inherits them. A figure whose inputs were never settled must not be presented as though they were. Test it.

**The composite plot.** This is the centre of the analysis state and the largest thing in the concept that does not exist.

- **One bull's artwork drawn from the definition**, centred on the aim point, with every scoring shot's offset from **its own** bull's aim point plotted on it.
- **Sighter bulls are not in it, and a test asserts that.** The sighter pooling defect was precisely this mistake one layer up, and the plot is a second place it can be made.
- **Each shot is a circle at the calibre's diameter when a calibre is set, and a point when it is not**, with the legend saying which. This follows the rule edge-to-edge extreme spread already uses: print the reason in place of the thing rather than a plausible substitute.
- **Excluded shots are drawn hollow and dim, not removed.** Excluding a shot is a human judgement, and a plot that hides it makes the group look better than the evidence does. Marks set to not-a-shot are absent, because they are not shots. Test both.
- **The group centre is marked**, and **CEP 50 and CEP 90 are dashed circles about it**.
- **Extreme spread is drawn as the line between the two shots that produce it, not as a circle.** This is a deliberate departure from the concept and the reason is that a circle of that diameter reads as a containment region, which extreme spread is not: it is the distance between two particular shots. Drawing those two shots joined is the honest depiction, and clicking the line should select them both. If the concept's circle is wanted anyway, say so and it goes back, but the reason belongs in the record either way.
- **Framing.** The view frames the shots with a margin and draws the bull's rings behind them at true relative scale, running off the frame when the group is much smaller than the bull. A group of 0.10 in sigma scaled to fit a one inch ring is a dot, and a dot tells the reader nothing.
- **A shot clicked on the plot selects it**, the same selection the shot list drives.
- **A legend**, as the concept has one.

**Two figures the stack is missing**, both already in the engine: **CEP**, with 90 as the row's figure and 50 and 95 beneath it, from `GroupStatistics.Cep`, and **group width by height** with the per-axis standard deviations.

**Do not reorder the existing figures to match the concept.** The concept leads with Rayleigh sigma; the screen leads with mean radius at `Tokens.LeadFigureSize`, which came from entries 73 and 92 and is written down. The concept was drawn before those entries. Add the two missing rows and leave the order alone.

### 2. The two judgement cards, and two errors in the concept that must not be inherited

The README's caption promises "two plain-language judgements: whether the group is round, and whether that one wide shot is really a flyer". Both judgements exist in the engine and both are currently prose lines inside the **More figures** expander. The concept puts them in the main column as cards, and that is right: they are the two lines a shooter actually reads.

**Promote both.** A card is a bold verdict sentence and then its evidence, and the verdict never appears without the evidence in the same card.

**Error one in the concept: it names the wrong test.** The card reads "Circular within tolerance. Pitman-Morgan p = 0.476, so there is no evidence of vertical stringing in these 25 shots." Pitman-Morgan is not the circularity test. `docs/STATISTICS.md` section 7 separates them and says why:

> **Question A, is the group circular?** `H0: Σ = σ²I`. This is rotation-invariant, so it also rejects a group elongated diagonally.

> **Question B, is the group stringing vertically?** `H0: σ_x = σ_y` in **target coordinates**, with the correlation left free. A group tilted 45 degrees is non-circular but is not stringing, and a shooter told "your group is not circular" when the elongation is diagonal has been told something true and useless.

and then, in the same section:

> Use the **Bartlett-corrected likelihood-ratio test for circularity at `n ≥ 20`**, and a parametric bootstrap calibration below that. Use **Pitman-Morgan for vertical stringing at any `n`**. Label them differently in the interface, because they answer different questions.

`ShapeTests.Circularity` and `ShapeTests.VerticalStringing` are both built and they are different methods. **The card takes the circularity verdict and names the circularity test.** If the stringing answer is wanted as well it is a second card or a second line, labelled as stringing. The concept's wording is the exact conflation the document warns against, and it reached a screenshot, which is how it would reach the product.

**Error two in the concept: the stringing card states a negative result without its power, which the document forbids.** Same section:

> A 25-shot group has roughly 49 percent power against 1.5 times stringing. **Half the time, a group that really is stringing by fifty percent will not be flagged.** That belongs in the interface next to the result, not in a footnote, because "no significant stringing detected" from 25 shots means very little and users will read it as meaning a lot.

So **any card reporting no evidence of stringing carries, in the card, what the group's shot count could have detected.** The table in section 7 gives 155 shots for 1.25 times, 50 for 1.50 and 19 for 2.00; interpolate or state the nearest honest sentence, but do not print a bare negative.

**The flyer card keeps its hedge.** The concept's headline is "Shot 22 is not a flyer." The line in `MainWindow` today ends "so a shot there is not a flyer **by that measure alone**". The headline may be short, but that qualification survives into the card's body. A test should assert each card names the test behind it.

### 3. Question 15 answered: commit the sort, and this is the cheap moment to do it

`docs/QUESTIONS-FOR-PLANNING.md` question 15 asks whether to commit `MarkerDetection.InIdentifierOrder()` when doing so changes every printed table on Windows and no gate verdict. **Option A. Commit the sort and regenerate everything it touches.**

**Three reasons, one of which is new since the question was written.**

1. **No verdict changes**, measured on Windows, and a record that follows one stated order is worth more than figures that came out of an arbitrary one.
2. **The regeneration path is warm and the records are all currently consistent.** Entry 101 regenerated every record a command writes, the eight committed Windows tables and the documents that print them, four days after the question was raised. The afternoon that option A costs is mostly that sweep, and the sweep has just been rehearsed. Deferring it means running it twice.
3. **The sort makes the three-platform gate record a cleaner instrument.** An order dependence is exactly the kind of thing that differs between two platforms for reasons that have nothing to do with arithmetic, and the record now passes on all three, so this is the moment to remove a confound rather than add one.

**What the commit must carry.**
- **The mounted baseline moves and it is quoted as a benchmark.** `docs/PHASE0-RESULTS.md` section 4.5 states "the figures to beat: worst scoring bull 0.015 to 0.091 in, 8 to 21 of 25 scoring bulls over the gate", and `ultrawide3.jpg` goes from 21 to 18. Amend section 4.5 in the same commit, with dated before and after figures, and put the new figure beside the old wherever Phase 1 surface work reports against the old one.
- **Dated before and after, verdicts shown unchanged**, as the question proposes.
- **Its own commit**, separate from section 1's screen work. One changes evidence and the other changes the interface, and a reader six months from now should not have to separate them.

**Option C is the real question and it is not scheduled.** Replacing RANSAC's random consensus with a deterministic robust fit answers the fragility rather than one ordering of it, and entry 101 left RANSAC's inlier choice native, so nothing in that work touched this. It is not scheduled now for two reasons: it moves the figures again, and the case where the instability bit hardest is the mounted one, whose gate has no proven material yet. **Record it as a named risk in `DESIGN.md` section 22**, pointing at question 15, so the decision is deferred in writing rather than forgotten. Revisit it when the mounted gate has real material, which is the weekend session at the earliest.

### 4. Question 18 answered: what "assisted" means, where blank paper goes, and that bought targets and the designer are one item

Four answers, to the three things section 6 of the question asks to settle plus the option it asks to refuse.

**1. "Assisted" means the snap, and section 3 should say so.** Option A. `Snapping.ToHole` takes artwork as an optional argument and snaps to the dark centroid within a calibre-sized radius when there is none. That is assistance, it works with no definition, and it is built. Make `DESIGN.md` section 3's bullet say what it means, in one sentence, and give the README a feature line with a state. A promise that is already kept and never defined is the worst state for a promise to be in.

**2. Blank-paper detection is worth a phase and the phase is 4.** Not 5. It belongs in the desktop application where the secondary mode already lives, and Phase 5 is chronograph and solver work with nothing to do with it. It starts **Not started**, and **its gate names the material it needs**: one photograph, at a known scale, of a sheet of plain paper with real holes in it. No such image is in the corpus. Naming the material in the gate is what stops it being marked done without it, which is how the mounted gate is already handled. It goes on the range list and it blocks nothing.

**3. A user-traced definition is the intended route to the full detector on bought targets, and it is the same item as the deferred designer.** Say so in both places: the deferral entry gains the sentence, and the bullet points at it. **The consequence is worth writing down explicitly: the designer's deferral now carries two promises rather than one.** A deferral carrying two promises is a different object from one carrying one, and the moment anybody asks for either, both arrive together.

**4. Option C is refused for now, and option D is refused outright.** Unmodelled artwork in the detector is the direct route to the false positives gate G2 exists to forbid, and there is no corpus material to measure it on. Refused until somebody asks, and then as research with a gate of its own rather than as a feature. D is refused because nothing about the promise turned out to be wrong; the word was simply never defined, and section 3's first answer defines it.

Set question 18's status to answered, pointing at this entry.

### 5. Two claims in the README that are no longer true

Both have survived several commits that touched the file, which is how a public page ends up describing a build that no longer exists.

**Claim one: macOS does not reproduce the Phase 0 record.** The Platforms table's macOS row reads "not yet", and the paragraph under it reads "macOS differs on a small number of measurement rows, traced to corner refinement inside the native imaging library and to one further divergence below it."

**That was fixed by entry 101 and confirmed twice.** The `phase 0 gate record` workflow succeeded on commit `39facee` and again on `60fffc6`, and on the second run every job succeeded: `windows-latest`, `ubuntu-latest`, `macos-latest`, `windows corners`, and `macos-latest, from windows corners`. Under the workflow's own definition in its header, the record reproduces on a platform when every gate verdict and every printed table is identical, so macOS reproduces it.

- **The row becomes yes.**
- **The paragraph is rewritten.** What stands between Linux and macOS and a download is no longer the record. Say what it actually is, which is packaging and the fact that nobody uses either day to day, and keep the distinction the workflow makes: the printed tables are gated and identical, and the raw records are compared and reported rather than gated, so differences below printed precision may remain and are not failures.

**Claim two: there is no navigation rail.** The Concept screens paragraph says "there is no navigation rail, no composite plot and no analysis screen yet." **The rail was built in `5dad2e2` and has a test**, `TheRailIsBuiltAndItsOtherDestinationsAreNot`. Rewrite the sentence to say what is true now: the rail exists with one destination built and four naming the phase that builds them, and what is absent is the composite plot and the analysis screen. Section 1 changes that sentence again, so make this edit in the same commit as section 1 rather than separately.

**And a small guard, if it can be made cheaply.** `ReadmeTests` already fails when a phase and its state disagree between the README and `DESIGN.md` section 21, and prose drifted anyway for three commits. A cheap version of the check: every feature the Concept screens paragraph names as absent must appear in the Planned section with a state that is not **Done**. That catches this exact class. **If it turns out awkward or brittle, say so and skip it rather than building something elaborate to enforce one sentence.**

---

## 2026-09-18, entry 102: what cross-platform has cost, and why the run that just finished is the argument for keeping it

**Status: actioned 2026-09-18.**
- **Sections 1 to 5: recorded.** All three platforms stay, and the three-platform gate record stays the guard on the four routines entry 101 ported. Nothing asked for code.
- **Section 6: both experiment branches deleted after the check.** Nothing on either was absent from `main` or unrecorded: "Entry 101" in `docs/PHASE1-RESULTS.md` carries the method, the four steps, and the contour port reproducing OpenCV's corners on all 136 corners of each of the eight sheet images once its arithmetic is imitated. `docs/PHASE1-RESULTS.md` "Entry 102".

Alan asked how much time and how many tokens would be saved by developing Windows alone. **This answers it with the evidence, and the answer runs against the question.**

### 1. The cost so far, honestly counted

Cross-platform work has been substantially the subject of roughly **five entries out of about a hundred**, and about **three Claude Code sessions**: the OpenCV build failure of entries 47 and 48, the macOS rerun of entry 49, the Linux VM and tarball of entries 61 and 63, the print path of entry 77, and the macOS numerical port that just landed.

**Call it eight to ten percent of everything spent so far.** That is a real number and it is not nothing.

**Going forward it is close to zero.** CI on three platforms costs no attention while it is green, and as of this run it is green on all three for the first time. **The expensive part has just been paid.**

### 2. The bill arrived and it bought something that is not about macOS

Four floating-point steps now run in GroupLab's own code rather than OpenCV's. **The reason that matters has nothing to do with Apple.**

**It found a real numerical defect in the measurement core.** OpenCV stops the homography's final fit after ten iterations. Ours runs to convergence. The consequences, on Windows as much as anywhere:

| | Before | After |
|---|---|---|
| Paper gate, worst bull | 0.00340 in | **0.00319 in** |
| Print-scale detection | 0.96195 | 0.96200 |
| 1.5-module refinement window | **0.00517 in, over the gate** | **0.00290 in, inside it** |

**A Phase 0 finding reversed.** A refinement window this project had recorded as failing the paper gate does not fail it, and the reason it looked like failing was a truncated fit.

**None of that would have been found by a Windows-only project**, because there would have been nothing to disagree with. **Two implementations disagreeing is the oldest correctness test there is**, and three platforms have been running one continuously without anybody calling it that.

**The cost is 10 to 16 milliseconds per sheet.** For a defect found in the number the entire application exists to produce, that is cheap.

### 3. Dropping platforms now would remove the guard on what the platforms just bought

**GroupLab now owns four numerical routines that used to be OpenCV's.** That is a real maintenance liability, and the thing protecting it is the gate record reproducing on three platforms.

**Cut to one platform and that guard goes**, on code that is four days old and was written precisely because platforms disagreed. **The moment to stop paying for a test is not the moment after it caught something.**

### 4. What dropping would actually save, and what it would cost later

**Save:** CI minutes, which are not Alan's tokens and not his wall clock while green. Occasional platform-specific defects like the print path, which was a real bug on a platform GroupLab intends to ship to.

**Cost:** `DESIGN.md` and the README both now say in public that all three are held correct continuously so that neither becomes a port later. **Entry 60 made that argument and the page states it.** Resuming after a gap means paying the port, and a port is what these four numerical routines would have been discovered during, at a much worse moment.

### 5. The recommendation

**Keep all three.** Not out of loyalty to the plan, and not because the README says so. **Because the practice just found a bug in the Windows numbers and is now the only thing guarding the code written to fix it.**

**If the cost ever needs cutting, cut the right thing.** The gate record on three platforms is the valuable half and it is cheap. **The Linux VM, the tarball and the packaging are the discretionary half**, and entry 61 already argued for keeping those minimal: one format, no store accounts, no review queues, and nothing added until somebody asks by name. That restraint is where the savings are, and it is already in force.

### 6. The two experiment branches

`experiment/opencv-unoptimized` and `experiment/portable-imaging` should be deleted **after one check**: that nothing in either is absent from `main` and unrecorded.

**The diagnosis is worth more than the branches.** What matters is that `PHASE1-RESULTS.md` under "Entry 101" carries the method, the four steps, and the verification that the contour port reproduces OpenCV's corners on every corner of all eight sheet images once OpenCV's arithmetic is imitated. **If that is all written down, the branches are scaffolding and should go.** If any of it lives only in a branch, move it first.

---

## 2026-09-18, entry 101: the control is what answered it, the error's location names its cause, and a warning claims four digits it does not have

**Status: actioned 2026-09-18.**
- **Section 4: "about 27 percent".** The spacing warning rounds its rate to a whole percentage, and a test refuses a decimal one.
- **Section 3: the rule is written above the canvas's constants**, with which constant falls under which half of it.
- **Section 5: the README calls the stage timeline in progress**, a gallery at the end, with the live run of `DESIGN.md` section 19 not built.
- **The macOS gate record:** the synthetic warp, both corner refinements and the homography's final fit are managed code, so every platform computes them the same way. Before this commit, a run carrying the first three printed Linux identical and macOS different only in the three contour rows, and the managed contour step reproduces the native one on Windows once OpenCV's single precision is put back. Every gate verdict is unchanged; the records, the Windows tables and the documents that print them are regenerated. `docs/PHASE1-RESULTS.md` "Entry 101".
- **Sections 2 and 1: recorded.** The rescan with a weight on the lid is compared the way entry 98 section 3 was, with the same control.

Section 2 has a two-minute test Alan can run without printing or shooting anything. Section 4 is a small correction to user-facing prose that matters because people will act on it.

### 1. I asked for a comparison and it needed a control

I asked for the scan's residual on the holed bulls against the clean ones. **Claude Code ran that and added three unshot Phase 0 sheets as a control, and the control is the only reason the result means anything.**

| rms, in | Holed half | Clean half |
|---|---|---|
| Alan's sheet | 0.0019 | **0.0030** |
| Unshot Phase 0 sheets | 0.0019 to 0.0021 | **0.0010 to 0.0013** |

**Without the control, 0.0019 against 0.0020 reads as "the holes are not the cause" and stops there.** That conclusion is correct and it is half the answer. **With the control it becomes "the lower middle of a sheet is always the worse half, Alan's matches everyone's, and his excess is entirely in the clean bulls"**, which locates the problem instead of merely acquitting a suspect.

**The rule: a comparison between two halves cannot say which half is abnormal.** It can only say they differ. **Naming the abnormal one needs a sheet where nothing is wrong**, and I should have asked for that rather than being handed it.

This is the same shape as entry 82's lesson about proposing a discriminator from medians. **Both times the missing thing was a reference distribution, and both times it was supplied by somebody else.**

### 2. The location favours lift over the printer, and there is a test that takes two minutes

**The excess is in bulls 11 to 25, which are rows three, four and five: the middle of the page.** Not the bottom, not the edges.

**That is what a sheet which will not lie flat does on a flatbed.** The lid presses the edges and the middle stands off the glass, so the middle of the page is imaged at a slightly wrong magnification. **Alan's sheet was taped at four corners to bowed cardboard, shot, and peeled off. The Phase 0 sheets were never mounted.**

**A printer error would not choose the middle.** A 0.43 percent vertical stretch is monotonic down the page and would show at the bottom, which is where the error is not.

**So Claude Code's own alternative is the better one and the location argues for it specifically.**

**The test needs no printing, shooting or photographing.** **Scan the friend's sheet again with a heavy flat weight on the lid**, a large book, and compare the middle rows. **If the excess drops, it was lift.** If it does not, it is the printer and the weekend's clean-sheet scan becomes a confirmation rather than the experiment.

**Two minutes, a sheet Alan already has, and it decides a question that has been open since entry 96.**

### 3. Interaction is physical, legibility is screen-space, and that distinction should be written down

The tap snap is now a hole's width on the paper plus four screen pixels for aim. Selecting a shot reaches its drawn ring. Clicking a bull covers half the distance to its neighbour. **And drawing minimums and gaps stayed in pixels.**

**That split is exactly right and it is a rule worth stating rather than leaving as three decisions.**

> **A target the person is aiming at is measured on the paper. A minimum the person has to see is measured on the screen.**

The first must not change when a window resizes. The second must not shrink below what an eye can resolve. **Anything measured in the wrong one of those two will break the first time a layout moves**, which is precisely how this was found.

### 4. The spacing warning claims four significant figures it does not have

> 1.00 in between bulls is 2.9 sigma: about one shot in 4 (26.58 percent) would land nearer a neighbouring bull than its own.

**"About one shot in 4" is right. "26.58 percent" is not.** That figure comes from a simulation, and its input is a sigma the person has estimated or that came from a group with an interval of roughly plus or minus a third. **Two decimal places on it is a precision the number does not have**, and this project has caught itself doing that before.

**Cut it to "about one shot in four", or if a percentage is wanted, "about 27 percent".** The sentence is otherwise very good: it gives the number, says what it means, and leaves the decision alone.

### 5. The stage pictures are a gallery, not yet the thing the design describes

Claude Code says plainly that the pictures appear when the analysis finishes rather than stage by stage. `DESIGN.md` section 19 [r3] describes a live run where "the markers light up, the residual map settles, the artwork vanishes and the holes emerge".

**A gallery at the end is most of the value and none of the theatre.** Record the difference rather than letting the feature read as done.

**It goes below the macOS gate record**, which is the last open red in CI and has now been deferred twice.

---

## 2026-09-18, entry 100: a form needs token roles the concept never showed, and the rehearsal ceiling applies to screens that are not the editor

**Status: actioned 2026-09-18.**
- **Section 1: six roles, three palettes, one change.** Field background, field border, focus ring, disabled, warning text and error text, with high contrast derived like the rest, and the contrast tests extended to cover them: text at 4.5:1 on fields too, edges and disabled at 3:1, high contrast a step higher. Warning is amber and error red in every theme, held by a test. The application's fields now use these roles.
- **Section 2: the window rehearsal ran after the editor**, and after everything else in the batch: 10 presses, no taps.
- **Section 3: the spacing warning reads like the flyer line.** "... 1.00 in between bulls is 2.9 sigma: about one shot in 4 (26.58 percent) would land nearer a neighbouring bull than its own. Six sigma is where that falls to one in 185." A test checks it carries no should.

Short, and it lands before the parametric editor is built rather than after.

### 1. The concept has no form on it, so the form will invent its own colours

The approved concept shows a review card, readouts, a queue and a tool strip. **It shows no text field, no dropdown, no validation state and no warning.**

**The parametric editor is a form**, and a form needs roles the token set has never had to produce: a field background, a field border, a focus ring, a disabled control, warning text and error text.

**Those roles will be needed on the spot, and the easy thing is to pick a colour that looks right on the one screen being built.** That is how a token set quietly becomes a dark theme with names, which entry 93 section 3 was written to prevent.

**So: add the new roles to the token set, not to the screen.** Every one gets its light and high-contrast values in the same change, and the existing contrast tests extend to cover them. **If a role cannot be given all three values, it is not a role.**

**One role deserves naming now because the project already has a meaning for it.** Entry 93 fixed amber as "this needs a decision" and red as "this is wrong". **A form's validation warning is a decision and its refusal is an error**, so the spacing warning in entry 99 section 4 is amber and a layout that cannot register is red. That mapping is already established and the form should inherit it rather than choose again.

### 2. The rehearsal ceiling is not about the editor screen

The window rehearsal holds the working loop at 10 key presses. **A new screen elsewhere in the application can still move that number**, by taking focus, by adding a tab stop in the wrong place, or by changing what has keyboard focus when the marking screen opens.

**Run it after the parametric editor lands too.** It is the guard for the application, not for one screen, and the bug it already caught was exactly a focus problem.

### 3. The warnings are prose and the project has a voice for it

Entry 99 section 4's spacing warning is a sentence a person reads while deciding something, and this project has two good models already in the application:

> a group of 12 is expected to put its worst at 1.95, so a shot there is not a flyer by that measure alone

> Print at actual size, 100 percent. Never fit to page: a sheet printed at any other scale measures wrong.

**Both give the number, say what it means, and leave the decision with the reader.** The spacing warning should read the same way: what the chosen spacing is against the stated dispersion, what proportion of shots that puts nearest the wrong bull, and nothing about whether to proceed.

---

## 2026-09-18, entry 99: Jeff's answer is not coming, which blocks nothing, and the parametric editor should be built while the visual designer stays deferred

**Status: actioned 2026-09-18.**
- **Section 1: recorded**, and the deferral that rested on it is gone from `DESIGN.md` section 3 and the README.
- **Section 3: the parametric editor is built**, in the print screen, and it places sheets by the library's own rule: a test rebuilds ten built-in sheets from their parameters and gets back every bull and marker exactly, including the two that needed the library's fallbacks for the sighter gap and the codes. **The visual designer stays deferred** with your reason and your citation, and the scope-to-phase test passes.
- **Section 4: all three checks.** Page fit and validation refuse in red, with the shortfall; fewer than 9 markers is refused and fewer than 16 warned; a spacing tight for the stated group is an amber warning with entry 56's rate, computed from its closed form, and no verdict.

Section 1 is my error. Section 3 is the work this releases.

Jeff cannot remember the conversation, so the specification entries 89 and 90 were waiting for does not exist.

### 1. I tied the designer to an answer that was never going to decide it

Entry 89 deferred the parametric editor and the visual designer because "Jeff's is coming", and entry 90 recorded that deferral as the reason both sit outside every phase.

**Jeff's question was about subgroups on a sheet. The designer is about authoring a sheet. I ran two separate things together and made the second wait on the first.** The designer was always Alan's to specify, and the deferral has been resting on a citation that will not arrive.

### 2. Nothing is actually blocked, and the work already done covers it

**The subgroup capability is built.** Bulls map to load names in the session and the marking file, each subgroup reports its own figures, and they are compared by Fligner-Killeen and MANOVA. **That happened without Jeff's answer and does not need it.**

**Thirty and thirty-six bull sheets already exist** as `GL-CF30-LTR`, `GL-LR30-TAB` and `GL-RF36-LTR`.

**And the 30-against-25 argument stands on its own**, from entry 89 section 3: the precision gain is ten percent and not worth a sheet redesign, while 30 divides as 5 by 6, 6 by 5, 3 by 10 and 2 by 15 where 25 divides only as 5 by 5. **That is a property of the number and needed no testimony.**

**So the only thing lost is a corroborating anecdote**, and the deferral it was propping up should be re-decided on its merits.

### 3. Build the parametric editor. Keep the visual designer deferred

`DESIGN.md` section 3 names them as two things and they are two sizes of job. **The parametric editor is a form. The visual designer is a canvas.**

**The evidence that a form covers the space is the library itself.** Section 9 [r3] records that parametric mode stores a grid rather than a bull list, and that the 25-bull reference sheet and the 36-bull rimfire sheet both encode to 55 bytes. **Every one of the twenty-two built-in sheets is parametric.** A person wanting a layout the library does not carry almost certainly wants a different grid, spacing, ring set or page size, not arbitrarily placed bulls.

**What it exposes:** page size, grid rows and columns, bull spacing, the ring set, whether there is a sighter row and how many, and whether there is a load block. **That set produces every sheet in the library and a great many that are not in it**, including a 6 by 5 at a spacing nobody has printed.

**The visual designer stays deferred, now with an honest reason rather than a borrowed one:** nothing downstream needs arbitrary bull placement, the format already carries it when something does, and a canvas is a large screen to build for a case nobody has asked for. **Revisit it when somebody asks for a layout the form cannot express.**

### 4. A custom target can be a bad target, and the editor already has the measurements to say so

This is the part worth getting right, because **a form that lets a person build a sheet that cannot be analysed is worse than no form at all.**

**Three checks, and all three use work already measured:**

1. **Spacing against dispersion.** Entry 56's table gives the misassignment rate by spacing over sigma, verified against 400,000 simulated shots: 24.9 percent at three, 8.9 at four, 0.54 at six. **The editor should say, in words, what a chosen spacing means for a shooter of a given dispersion at a given distance.** A person setting 25 mm spacing for a rifle that shoots an inch should be told one shot in four will land nearest the wrong bull, before they print fifty copies.
2. **Marker coverage.** `IMG_5823` failed outright because too few markers decoded. **A layout that leaves too little room for markers, or spreads them too thinly, fails registration and should be refused rather than printed.**
3. **Page fit and actual size.** The sheet already refuses to measure correctly when printed at any other scale, and a layout that does not fit its page is the same failure earlier.

**Refuse the third, warn on the first two with the numbers.** The project's whole character is telling people what a number means rather than deciding for them, and a spacing warning that quotes entry 56's measured rate is exactly that.

### 5. Where this leaves Alan's queue

**The weekend session is now the only outstanding thing that needs a person**, and it needs only him. No Jeff, no friend, no weather beyond his own printer and his own cardboard.

---

## 2026-09-18, entry 98: the snap radius is in the wrong units, a test that bypasses its surface protects nothing, and a discriminating test for the leftover scan error

**Status: actioned 2026-09-18**, except section 5's second item, the macOS divergence, which gets its own turn next.
- **Section 1: recorded.** Both rehearsals stay, the window one is the guard, and 10 presses is its ceiling. It held through everything below.
- **Section 2: fixed at the cause.** The snap's reach is one hole in sheet units (the calibre, else the sheet's own measured holes, else a nominal .30) plus four screen pixels of pointing tolerance, and a test shows zooming in by four moves it by less than that. The audit found two more: clicking a shot now reaches its drawn ring, which a fixed 18 pixels did not once zoomed in, and clicking a bull uses half the distance to its neighbour rather than 54 screen pixels.
- **Section 3: answered, and not the way the hypothesis predicted.** After the warp the holed half reads 0.0019 in and the clean half 0.0020. The control settles it: on the unshot Phase 0 sheets the "holed" half is already the worse half, and Alan's reads like theirs; his excess is all in the clean bulls 11 to 25, 0.0030 against 0.0010 to 0.0013. The holes are ruled out; the lower middle of the sheet, where the scan's drift ran, is where it lives. The clean-sheet scan stays the experiment.
- **Section 4: recorded.**
- **Section 5, first item: done.** Each stage shows its own picture: markers lit at the fiducial stage, corners ringed by their residual at registration, and the residual with the artwork gone at the difference stage. Only an interactive run keeps it, and a test shows a batch run carries none.

Section 2 is a defect whose symptom was fixed. Section 3 is a test on data in hand that would settle something the warp result left open.

### 1. The window rehearsal earned itself on its first run, and the reason generalises

It caught a real bug immediately: after detection the first item's shot was not selected, so typing a bull number went nowhere. **The keyboard path advertised on the card did not work, and nobody would have found it except by using the application.**

**The existing rehearsal could never have caught it**, because it drove the review queue directly and never touched the window.

**The rule worth keeping: a test that bypasses the surface it is meant to protect protects nothing.** The queue rehearsal tests the queue, which is right and was never the risk. **The risk was always the window**, and for three batches there was a guard in place that could not see it.

**Both rehearsals should stay**, with the window one as the guard entry 97 section 5 asked for, and 10 presses as the ceiling it now enforces.

### 2. The snap radius is in screen pixels and should not be

Claude Code found that taking height from the image made taps snap to neighbouring holes, and fixed it by keeping the timeline strip to one row until opened. **That fixes this instance and leaves the cause in place.**

**A radius in screen pixels means the snap covers a different amount of paper at every zoom level and every window size.** Zoom out and it reaches across neighbouring bulls. Zoom in and it stops reaching the hole under the cursor. **Every future layout change is one more chance to move it silently**, which is exactly the note Claude Code ended on.

**The right decomposition is two terms, and only one of them is in pixels:**

- **The hole's own extent, in sheet units.** A tap inside a hole is on that hole, at any zoom, and the detector already reports each mark's size.
- **A small pointing tolerance, in screen pixels**, because a person's aim with a finger or a mouse really is a screen-space quantity and does not shrink when they zoom in.

**With the physical term dominant, a layout change cannot move the behaviour**, and the pointing tolerance stays where it belongs.

**This is the same class as entry 73's marks not scaling with zoom**, which was also a screen-space quantity standing in for a physical one, and it is worth checking whether anything else on that canvas is measured in pixels that should not be.

### 3. The leftover scan error has a testable cause and the data is already here

After a degree-four warp the sheet still sits at 0.0020 in against 0.0009 to 0.0013 for a good sheet, and **the photographs do not show that leftover error.** Claude Code's suggestion is that the scan happened after the sheet was mounted, shot and handled, which the clean-sheet scan at the weekend would settle.

**There is a test that does not wait for the weekend, and it uses the sheet already scanned.** If the leftover error comes from shooting and handling rather than from printing, **it should be worse where the holes are.** Bulls 1 to 10 and the three sighters carry holes. **Bulls 11 to 25 are clean paper.**

**Compare the scan's post-warp residual on the holed bulls against the clean ones.** A bullet stretches paper locally, and fourteen holes through a sheet that was then unstapled and fed through a scanner is a plausible source of exactly this kind of local, non-smooth error.

**If the clean half is at 0.0012 and the holed half is at 0.0028, the question is answered today** and the weekend's clean-sheet scan becomes a confirmation rather than the experiment.

### 4. Declining the warp was right and deserves saying so

The degree-four warp brings `IMG_5819` inside the gate and makes `IMG_5820` worse, and **Claude Code recorded it and did not adopt it, because it was tried on the frames being gated.**

**That is entry 17's rule applied without being asked**, on a change that would have produced a passing number. It is the most valuable thing in the report and it is the kind of thing that never appears in a feature list.

### 5. Two things to schedule rather than leave floating

**The per-stage images are the half of the timeline that makes it worth having.** A list of stages is a log; markers lighting up and the artwork vanishing is the thing `DESIGN.md` section 19 describes. The detector currently discards those images, so the increment is keeping them for interactive runs only, with batch paying nothing, which is the constraint already honoured.

**The macOS divergence needs a slot, not a gap.** Entry 97 put it in "any gap" and the report says plainly that nothing blocked long enough to leave one. **A task assigned to spare time never runs when there is no spare time.** It is the last open red in CI and it should be given its own turn rather than waiting for one that will not come.

---

## 2026-09-18, entry 97: what to build while nothing can be printed, shot or photographed

**Status: actioned 2026-09-18.**
- **Section 1: the framing is done.** The document is a paper sheet on dark chrome, the accents follow entry 93's rule on the image as well as the panel (teal found, neutral placed, amber needs a person, red wrong), the review card is amber with an amber primary, and the header, list, rail and selected shot match the concept.
- **Section 2: records built, small.** A rifle's scope click, a barrel's round count, a load's components, in one file beside the settings. The zero correction now says "5 clicks left" with what rounding leaves, where the marking names a rifle and a distance.
- **Section 3: the timeline is in, the per-stage images are not.** Every stage's record under the image, scrubbed by slider or button, landing live as each stage files, with every positioned rejection a button that finds it on the image. Both of section 19's constraints hold and each has a test. The intermediate rasters are the part left.
- **Section 4: not picked up**, because no batch blocked.
- **Section 5: the guard now presses the window's keys**, and on its first run it found the first item after detection had no shot selected, so a typed bull went nowhere. Fixed. Nothing in the three batches cost a key press: the Core rehearsal stays at 15 and 0 taps, the window rehearsal at 10 and 0 taps, and 10 is now its ceiling.

Alan is unavailable for anything physical until the weekend. **This is the order of work until then**, and the reasoning for the order matters as much as the list.

### 1. First: finish the concept's framing

Entry 93 section 5 split the appearance work in two. **The working loop is done**: icon tool strip, keycaps, breadcrumb, primary action, label-value readouts, the rail without its destinations, and three palettes held to their contrast ratios by tests.

**The framing is what is left**, and entry 93 already named it: the paper-coloured sheet on dark chrome, the accent palette applied consistently, and the rest of the concept's surface.

**It goes first because it is approved, specified and started.** The concept is the reference, the tokens exist, and leaving a design half-applied is the state most likely to be re-litigated later.

### 2. Second: rifle, barrel and load records

**Entry 91's zero correction currently stops at MOA and mil**, because turning an angle into clicks needs the scope's click value and no rifle record exists. Entry 90 found that gap: `DESIGN.md` section 3 promises records for rifles, barrels, loads and sessions, and only sessions reached a phase.

**Clicks are what a shooter actually dials.** MOA and mil are usable because every turret is marked in one of them, and **the feature Alan asked for is not finished until it says "twelve clicks right".**

**It is pure data work with no physical dependency**, it is already Phase 4, and it is the shortest path from something he requested to something complete.

**Keep it small.** A rifle with a name and a scope click value, a barrel with a round count, a load with its components. **Not a reloading database.** The records exist to make other features honest, and the temptation to build a whole inventory system should be refused now rather than halfway through.

### 3. Third: the analysis showing its work

`DESIGN.md` section 19 [r3] specifies it in detail and entry 90 found it in no phase: **a stage timeline the user can scrub, clicking a rejection to highlight it on the image, and a live run where each stage's artefact appears as it lands.**

**Every stage already emits a structured record**, which is what gave the Phase 0 and Phase 1 spikes their console output. **The data flows and nothing displays it.**

**It runs on any image already in the corpus**, so it is entirely unblocked, and it is the largest unbuilt interface feature in the design.

**Two constraints the design already states and that should not be rediscovered in review**: the trace must never be the only place an error appears, so a failed stage still produces a normal prominent error with the trace as detail behind it; and artefact generation defaults on for one interactive analysis and off for batch, so the theatre never slows the pipeline.

**It goes third because it is visual**, and building it before the palette in section 1 is settled means styling it twice.

### 4. Alongside, when any of the above is blocked: the macOS gate record

Still the only red in CI, still open with a named cause in corner refinement and one divergence below it, still needing nobody.

**It is not interface work and it has waited a long time**, which makes it the right thing to pick up in any gap rather than the thing to start with.

### 5. The guard, because Alan will not be in the application while this lands

Several days of interface changes will arrive without the person who uses it looking at any of them. **Entry 84's lesson was that a screen built without use goes wrong quietly.**

**The synthetic 25-shot rehearsal is the substitute and it already exists.** It measured 15 key presses and 0 taps on the current build.

**Re-run it after every batch and report the numbers.** If presses or taps rise, the working loop has regressed and it will be visible immediately rather than when Alan next opens the application. **A styling pass that costs the loop three key presses has not improved anything.**

### 6. One thing Alan can still do this week that needs no weather

**Jeff's three questions.** They turn the 30-bull request into a specification, they are a text message, and the target designer stays deferred until they are answered.

---

## 2026-09-18, entry 96: the sheet is part of the instrument, the two numbers do not reconcile, and the gate's statistic contradicts the project's own reasoning

**Status: actioned 2026-09-18.**
- **Section 1: agreed and adopted as practice.** From here a gate measurement is reported beside the sheet's own print quality, and a sheet is scanned flat before it is shot, which is the measurement this sheet never had.
- **Section 2: tested, and the answer is part, not most.** Warps of rising degree fitted to the scan's markers take its 0.0027 in to 0.0020 at degree 4, where the Phase 0 sheets go to 0.0008 to 0.0011. About 0.0018 in of the print error is smooth and absorbable; the rest is twice a good sheet's. It still does not reconcile with the photographs, which do not carry it, so part of what the scan reports belongs to a shot and handled sheet on a platen, and the clean-sheet scan separates the two. The same warp brings `IMG_5819` inside the gate and makes `IMG_5820` worse; recorded, not adopted, because it was tried on the frames being gated.
- **Section 3: recorded, not acted on**, exactly as you frame it. Every frame reported from here carries its rms beside its worst bull.
- **Section 4: for Alan's session**, fourteen frames and a flatbed scan of the clean sheet first.

Section 1 answers the question Claude Code raised. Section 2 is an arithmetic disagreement inside its own findings that changes what section 1 means. Section 3 is a principle this project already wrote down and did not apply here, raised deliberately without acting on it.

**This is the best investigation in the project so far.** Six causes ruled out with evidence, two left standing, and a test named that separates them. Nothing below detracts from that.

### 1. Yes: a gate measured on a bad print is measuring the printer

Claude Code's line deserves to be the headline. **The friend's sheet was printed 0.43 percent long vertically against 0.06 percent on the Phase 0 printer, and its own flatbed scan averages 0.0027 in against 0.0012 to 0.0015 for the Phase 0 sheets.**

**The sheet is part of the instrument.** A gate on bull-centre recovery measures the whole chain: printer, paper, mounting, lens, registration. **If the print alone eats half the budget, the gate is no longer asking what it was written to ask.**

**Two things follow and neither is a change to the gate.**

1. **Record every sheet's print quality alongside every gate measurement**, as a first-class figure. A flatbed scan of the unshot sheet gives it in one pass, and it should be taken before a sheet is shot rather than reconstructed afterwards.
2. **Measure the gate on sheets whose print quality is known and good.** The friend's sheet remains excellent for detection, assignment and the editor, and it is the wrong ruler for registration.

**This also explains something that had no explanation.** Entry 71 called the friend's mounting the closest anything had come to the gate. **Part of that closeness was luck and part of the remaining gap was his printer**, and neither was visible until somebody scanned the sheet and looked.

### 2. The two numbers do not reconcile, and the resolution matters

**Point 2 and point 3 of the findings disagree with each other**, and I think the disagreement is informative rather than an error.

If the photograph's error simply contained the print error plus something independent, then with a photograph at 0.0031 rms and a scan at 0.0027:

- the photograph's own contribution would be only **0.0015 in**, and
- the two error patterns would correlate at about **0.87**.

**The observed correlation is 0.27.** Those cannot both be true, so the print error is not sitting inside the photograph's error as an independent additive term.

**The explanation I would test first: the surface fit absorbs it.** A 0.43 percent vertical stretch is a smooth, low-order distortion, and the photograph's registration has free parameters that will happily take up a smooth stretch into the fitted surface. **A flatbed scan has no such fit**, so it reports the print error in full.

**The test is cheap and uses data in hand: fit the same surface model to the scan and see what its residual becomes.** If 0.0027 falls toward 0.0015 under a low-order warp, the print error is smooth and absorbable, and section 1's concern is real for the scan gate and much smaller for the photograph gate.

**Which would be good news worth having**, because it would mean the mounted gate is not being spoiled by the printer, and the remaining 0.0031 really is the two causes Claude Code has left standing.

### 3. The gate is on the worst of 25, and this project already argued against that

`DESIGN.md` section 21 [r4], on the registration residual:

> RMS is the statistic because a maximum over a point set grows with the number of points for any noise distribution: gating on the maximum would hold a dense sheet to a stricter standard than a sparse one for no physical reason.

**The bull-centre gate is on the worst bull.** The same argument applies to it exactly and was never carried across, and it is now biting: at 0.0031 rms the expected worst of 25 is 0.0059, so a 25-bull sheet is held to a stricter standard than a 9-bull sheet for no physical reason, and **whether a frame passes depends partly on the draw.**

**I am raising this and not acting on it.** Entry 17 fixed the rule that a number chosen after the results is not a gate, and rewriting the gate's statistic while staring at a frame that just failed by six percent is exactly what that rule exists to prevent.

**What to do instead costs nothing and preserves the option.** **Record both numbers for every frame from now on**, the worst bull and the rms, in `PHASE1-RESULTS.md`. If the gate's statistic is ever revisited, the decision will rest on a body of frames rather than on the one that prompted the question, and if it is never revisited the extra column costs a line.

### 4. Two more photographs, in the same session as the twelve

Claude Code's test for separating a camera-side distortion from a fine paper shape is right and free: **the same mounted sheet, square on, photographed twice at the same distance, once with the sheet in the left half of the frame and once in the right.** An error fixed to the camera moves with the sheet's position in the image. An error fixed to the paper stays put.

**Add it to Alan's session, so it is fourteen frames rather than twelve.** He is shooting a clean sheet on cardboard indoors, which serves this test exactly as well as a shot one.

**And take a flatbed scan of that clean sheet before anything else**, per section 1 item 1. It gives the print quality of the sheet the whole session is measured on, from his own printer, which is also the printer every future Phase 1 sheet will come from.

---

## 2026-09-18, entry 95: I had the turret backwards, the overlapped-pair gap should be accepted, and the inbox is empty for the first time

**Status: actioned 2026-09-18.**
- **Section 1: recorded.** Where the group sits and what to dial stay two labelled things, and a test holds them opposite.
- **Section 2: built as asked.** One field for the rounds fired, sighters not counted. Every detected mark now carries its size in holes, and when the count disagrees the queue's first item says so and ranks the candidates: largest first when there are too few, with "is two shots" as its keyboard choice, smallest first when there are too many, with "is not a shot". The overlapped-pair limit is stated, not tuned.
- **Section 3: chased, and reported as findings, with nothing changed.**
  - **0.00531 is not one bad bull.** It is the ordinary worst of 25 at 0.0031 in rms, whose expected worst is 0.0059; the gate needs 0.0026 in rms.
  - **The sheet uses most of the gate on its own.** Alan's flatbed scan of it reads 0.0027 in rms against 0.0012 to 0.0015 on the Phase 0 sheets, printed 0.43 percent long in y against 0.06, and passes the paper gate less than half the time by the same arithmetic.
  - **The photograph's error is not the sheet's** (correlation with the scan +0.27 and -0.04), **it repeats from the same camera position** (0.83, 0.79 and 0.72 between frames from one place, near zero across places), and **it is in the registration, not the locator**: the marker corner residual is twice a scan's, and edge fit and centroid share the pattern.
  - **Ruled out:** print error, lighting across a bull (under one grey level per inch, explains nothing), parallax from paper height or a radial lens residual (the error is not radial), the locator, and focus.
  - **Left, and not separable with these frames:** a camera-side distortion the radial terms miss, or a paper shape finer than the surface model. One square-on pair with the sheet moved across the image separates them: a camera-fixed error moves with the image, a paper-fixed one stays with the paper.

Section 1 is an error of mine that would have hurt somebody. Section 2 accepts a limit rather than chasing it, and names a better lever. Section 3 is where the project now stands.

### 1. I wrote the turret correction backwards

Entry 92 used "dial 0.44 MOA left" for a group sitting left of aim. **A turret moves the point of impact, so a group sitting left is corrected by dialling right.** Claude Code caught it and split the readout into where it sits and what to dial.

**This one deserves more than a note.** Every other wrong thing I have written this week cost time. **A shooter following that sentence would have doubled their error rather than removed it**, and they would have trusted it because it came from software whose whole claim is that it does not give confident wrong answers.

**The split it chose is the right permanent shape.** Keeping "where it sits" and "what to dial" as separate labelled rows makes the reversal visible instead of implicit, so the next person to touch it cannot make my mistake silently.

### 2. The overlapped pair cannot be separated by area either, and that is a limit to state rather than a threshold to tune

With the flag counting the mark, a 0.10 in pair reads 1.30 to 1.41 holes and S1b reads 1.37. **They overlap, so no threshold divides them.**

**That is not a tuning failure, it is the truth about the measurement.** Two holes overlapping by two thirds genuinely contain about 1.37 holes of paper removed, because the overlap is not there twice. A ragged single hole reads the same. **The information needed to tell them apart is not in the image.**

**Claude Code was right to leave 1.35 alone**, and fitting into that gap would be fitting to one mark.

**What the failure actually costs is worth writing down, because it is small and specific.** A missed second shot at 0.10 in separation moves the group centre and sigma barely at all, since the two holes are nearly in the same place. **What it changes is the count**, and the count drives every interval and the shots-needed advice. **So the cost is an interval that is slightly too wide, not a position that is wrong.** That is the mild direction to fail in.

**And there is a lever the detector can never have: the shooter knows how many rounds they fired.**

**Let a person state the expected shot count.** Then the software can say "you fired ten and I found nine, and these are the marks most likely to be two", ranked by how close each sits to the threshold. **That turns an unanswerable image question into an answerable arithmetic one**, it costs one field, and it belongs to the review queue rather than the detector.

**It also fixes the reverse case**, where the detector finds eleven marks for ten rounds and nobody currently gets told.

### 3. The inbox is empty, and everything on the critical path now needs a person

**This has not happened before in this project.** Every entry is actioned and nothing is queued.

What remains outstanding is almost entirely physical:

- **Jeff's three questions**, which turn the 30-bull request into a specification.
- **Twelve photographs at two distances**, which settle whether the off-axis mounted gate is reachable at all.
- **A real 25-shot sheet**, which is the only thing that can time the Phase 3 gate.
- **The attorney**, for the App Store permission.

**Two pieces of software work remain that need nobody**, and one of them has been sitting since entry 77:

1. **Why the best square-on frame is six percent outside the mounted gate.** `IMG_5820` reaches 0.00531 in against 0.005, and a square-on sheet has almost no depth range, so focus is not what limits it. **Entry 77 section 2 named this as unexplained and nothing has chased it since.** Six frames of real data are already in hand. **This is the Phase 1 headline and it is the best use of the next session.**
2. **The macOS gate record divergence**, open with a named cause in corner refinement and one further divergence below it, and unfixed.

**Item 1 is the one I would take.** The mounted gate is what Phase 1 is for, the data exists, and it is the last question in that phase that can be answered without anybody driving anywhere.

---

## 2026-09-18, entry 94: the oversize flag measures the hull instead of the mark, and four decisions

**Status: actioned 2026-09-18.**
- **Section 1: the flag counts the mark's own area.** Flags on real material fall from 5 to 2, and the two left are S1b at 1.37 holes and the friend's photograph's mark at 1.90. Entry 81's sweep re-run: single holes read 0.86 to 1.01 holes and are never flagged; pairs 0.15 in apart and wider are caught as before; **pairs 0.10 in apart, which overlap by two thirds, are caught 69 percent of the time rather than 100**, because their ink genuinely is 1.37 holes. The 1.35 threshold is unchanged deliberately, since the threshold that would catch them all would bring S1b back.
- **Section 2: the session route, built.** Bulls map to load names in the session and the marking file, each subgroup reports its own figures, and the two are compared by Fligner-Killeen and MANOVA. No format change, so it works on the thirty-bull sheets today.
- **Section 3: one constant.** `HoleToCalibre` 0.945 over 102 holes, and the treat-a-photograph-as-a-scan branch is gone with it.
- **Section 4: "Two shots" is on the item**, reachable with T, placing both shots where the detector's own split puts them. The rehearsal now settles everything with 15 key presses and **no taps**.
- **Section 5: both recorded.** The synthetic-to-real flag ratio fell from about ten to one to about two to one, so most of the excess was the hull rather than the hole model.

Section 1 overrides a "leave it alone" I wrote, and gives the reason. Sections 2 to 4 are decisions Claude Code asked for or earned.

### 1. The flag counts the convex hull, and that is the wrong quantity

**S1b is answered and both candidate causes are dead, including mine.** Elongation 1.60 and solidity 0.59, the least convex mark on the sheet. **A yawed bullet makes a convex oval, so yaw predicts high solidity and S1b has the lowest on the page.** A composited pair of that size reads elongation above 2. Neither fits.

**What fits is the measurement itself.** S1b's hull holds 2.10 holes and its ink holds 1.23, and its equivalent diameter is 0.329 in. **It is an ordinary hole with a ragged rim, and the flag is counting the area of a convex hull thrown around the spikes.**

**Four of the five oversize flags on real material are the same shape.**

**Change it to count the mark's own area, and I am overriding entry 83 section 3's leave-it-alone to say so.** That list was about ten irreducible blobs and a benign bimodality interaction. **A flag that fires falsely on four of five ordinary holes is a different thing: it is not a loud failure, it is noise, and noise is what teaches people to ignore a flag.**

**Area is strictly better than hull for this job and the arithmetic says why:**

| | Own area | Convex hull |
|---|---|---|
| Ragged single hole | about 1 hole | **about 2.1 holes** |
| Genuinely merged pair | about 2 holes | about 2.2 holes |

**Hull cannot tell them apart, 2.1 against 2.2. Area separates them cleanly, 1 against 2.** Entry 81 chose 1.80 everywhere on the argument that a loud failure beats a quiet one, and that argument only holds while the loud failure is rare.

**Re-run entry 81's confirming sweep afterwards**, because the thresholds were fitted against a flag that was measuring the wrong quantity.

### 2. Subgroups: take the session-level route

**The analysis half is already built and nothing can feed it.** `KruskalWallis`, `FlignerKilleen`, `ManovaGroups` and `DispersionRatio` all take a group label per shot, and no layer of the format can supply one.

**Take the cheapest route: a session-level mapping of bulls to subgroups, with no format change.**

Three reasons. **It works on today's 30-bull sheets**, so Jeff can shoot one before anything is designed. **It needs no format change**, so it cannot be wrong in a way that outlives a definition identifier. **And the right format answer depends on Jeff's specification**, which has not arrived, so committing the format now would be deciding without the thing that decides it.

**When the specification arrives, the format question reopens with the session mapping as the fallback that already works.**

### 3. Collapse the two hole-size constants into one

Photographs re-measure at **0.948 with a 4 percent spread**, scans at **0.938**. **They agree within 0.010 and the spread is four times that.**

**Two constants that cannot be told apart are one constant with extra branching.** Keeping them also keeps the fudge that sits beside them, where a photograph with no camera data is treated as a scan, which was only ever needed because the two differed.

**Collapse them, and record that the difference was largely the single-scale defect rather than a property of the two media.** If a later measurement separates them, split them again with the evidence attached.

### 4. The merged pair has no keyboard path, and that is what the rehearsal was for

Twenty-two key presses, one tap, and **the one tap is the merged pair**, whose queue item offers only "One shot" and "Not a shot".

**So if a mark really is two holes, the two-minute loop breaks at exactly the item the flag exists to raise.** A person is sent to the mouse to place a second shot by hand, and the concept's whole premise is that verifying twenty-five shots takes seconds.

**Fix it before the Phase 3 gate is timed for real**, or the gate measures a loop with a hole in it. The choice needs a third option, something that splits the mark into two shots and puts both on the queue for placement, reachable from the keyboard like the others.

**The rehearsal earned its keep.** It also showed one extra shot in a cell displacing a chain of three assignments with all three reaching the queue carrying the right bull, which is entries 70 and 74 working under load and is the first evidence of that.

### 5. Two things worth recording rather than acting on

**The synthetic hole model flags 11 of 25 oversized against 1 of 14 on real paper.** So it rehearses assignment honestly and the flag load not at all. **After section 1, re-check that ratio**: if the synthetic sheets still flag ten times more often than real ones, the synthetic hole shape is wrong in a way entry 80's size rescaling did not fix.

**Entry 90's sweep found the README understating the engine, not only overstating it.** Significance testing and hit probability are built, with rank and dispersion tests, MANOVA, a dispersion ratio, Holm correction and three hit-probability estimators, and the page had lost both.

**That is the more interesting half of that finding.** A page can be wrong in two directions, and I went looking for only one of them. **The test that now ties scope to phases catches both**, which is worth more than the ten items it found.

---

## 2026-09-18, entry 93: the concept is approved as the guideline, which is a design language rather than one screenshot

**Status: actioned 2026-09-18**, for the working loop; the framing is in progress.
- **Section 1: recorded.** Building to an approved reference is a different activity from inventing one.
- **Section 2: the language, not the screenshot.** In: the icon tool strip with its keys as keycaps, the breadcrumb header with the document's identity and counts, one primary action in amber with a secondary beside it, label and value readouts, and the status words the queue already carried. Not yet: the paper-coloured sheet on dark chrome and the rest of the framing, which section 5 puts second.
- **Section 3: the guard is met and tested.** High contrast is derived from the dark tokens in code, every hue kept and every text colour lifted along it to 7:1, and two tests hold all three palettes to their ratios and to their roles. The theme choice is four: dark, light, high contrast, follow system.
- **Section 4: the rail is built and its destinations are not.** Four of its five icons name the phase that builds them rather than opening an empty screen.
- **Section 5: taken in that order**, and the gate has not moved.

Alan: the concept screenshots are liked, they are the guideline for the UI, and refinement follows from there. **Entry 84's hold on appearance is lifted.**

### 1. Why this release is legitimate and not just me relenting

Entry 84 held the styling pass because **a screen styled before it is used is styled against a guess.** That reason is satisfied differently here rather than waived: **the concept is not a guess, it is Alan's own approved reference**, and building to an approved reference is a different activity from inventing one.

**Entry 92 section 4 already released one piece of this** on the other route, Alan's twice-repeated report that the statistics block is hard to read. **Both routes are now open and the whole appearance job is unblocked.**

### 2. There is one concept screen, and most screens are not it

`docs/figures/screens/assignment-editor.png` shows the assignment editor. **The print window, the analysis screen, the target library, session records and reporting have no concept and are not going to get one before they are built.**

**So the instruction is not "match the screenshot". It is "extract the design language from it and apply that language everywhere."** Those are different jobs and the second is the larger one.

What the concept actually specifies, and what should be written down as rules rather than copied pixel by pixel:

- **The chrome**: a narrow icon rail, a breadcrumb header carrying the document's identity and counts, one primary action in the top right, and a secondary beside it.
- **A tool strip of icons with keyboard hints as keycaps**, rather than a wrapped row of text buttons.
- **The document is light and the application is dark.** The sheet is paper-coloured on dark chrome, which is the single largest visual difference from today and the one that most defines the look.
- **Two accent colours with fixed meanings**: teal for what the software found on its own, amber for what needs a person and for the primary action.
- **Readouts as label and value rows**, which is entry 92 section 3's fix arrived at independently.
- **Status as a word, not a colour alone**: NOW, NEXT, ADDED, KEPT OUT.

**`DESIGN.md` section 19 already sets the typography**, a neutral UI sans for chrome and a monospace with tabular figures for every numeric readout, and that is already true of the application today and should not regress.

### 3. Four themes, or the fourth one is a rewrite

**The concept is one theme and the design promises four**: dark, light, high contrast and follow system.

**A palette extracted by matching a dark screenshot will not produce a light one.** Colours picked to look right on dark chrome carry no information about their light equivalents, and discovering that later means redoing every value.

`Theme/Tokens.cs` and `Theme/AppStyles.cs` already exist, so the structure is there.

**The guard: extract the concept's values into tokens, and produce the light and high-contrast variants from those same tokens in the same piece of work.** Not afterwards. **If a token set cannot produce the light theme, it is not a token set, it is a dark theme with names on it.**

### 4. The left rail's other four icons still do not get drawn

Entry 69 section 5 said it and it matters more now that the concept is the approved guideline: **the rail implies five destinations and only one exists.**

**Build the rail. Do not build its destinations.** A UI pass that starts inventing the screens behind four icons is how this becomes a rewrite rather than a styling job.

### 5. Order, and what still judges it

**The parts of the concept that touch the working loop come first**, because they are the parts that can move `DESIGN.md` section 21's Phase 3 gate of twenty-five shots corrected in under two minutes: the icon tool strip, the keycap hints, the review card and queue, and entry 92's readout rows.

**The framing comes second**: the breadcrumb, the rail, the paper-coloured sheet, the accent palette. **Those change how it looks and not how long the job takes.**

**The gate has not moved.** Appearance does not pass Phase 3, a person with a stopwatch and a 25-shot sheet does, and entry 84 section 2's synthetic rehearsal is still the cheap way to find the obvious problems before anybody drives to a range.

---

## 2026-09-18, entry 92: the zero correction is its own section, and Alan's report unblocks the statistics panel specifically

**Status: actioned 2026-09-18.**
- **Section 1: the correction sits above the group statistics, in its own section**, and the reason is in the code rather than in a layout: it answers a different question at a different moment, and it is the one figure on the screen that is a claim about the next group.
- **Section 2: the section holds those five things and nothing else.** Sigma, mean radius and extreme spread stay where they are.
- **Section 3: the block is rows now.** One row per figure, label left and value right, with the angular conversion on the same row as its linear value. Sigma and mean radius keep an interval line; extreme spread's moved behind the same disclosure as everything else.
- **Section 4: taken as the styling input it is**, and applied to this piece. Entry 93 then lifted the hold on the rest.

Section 1 is a placement decision with a reason that should outlive the styling pass. Section 3 says why the statistics block is still hard to read after the fix that was supposed to fix it.

### 1. The zero correction is separated because it answers a different question, not because it is prettier

**Alan wants the scope offset defined very separately from the group statistics so people can find it. Agreed, and the reason matters more than the layout.**

**They answer different questions and are read at different moments.** The group statistics answer "how well does this rifle and load shoot", which is a judgement made afterwards, sitting down, comparing one load against another. **The zero correction answers "what do I dial right now", which is read standing at a bench with a turret cap in one hand.**

**A number read at a bench must be findable in one glance and must not require reading past three confidence intervals to reach.** That is the whole argument, and it is why this is not a styling preference that a later pass can decide differently.

**It also has a different truth condition.** Every group statistic is true of the shots on the sheet. **The zero correction is a claim about what the rifle will do next**, and entry 91 established it is frequently not supportable at the shot counts people actually fire. **Mixing a prediction in among descriptions invites it to be read with the same confidence as them.**

### 2. What the section holds

**Its own heading, above the group statistics rather than below them**, because it is the thing most people opened the application to get.

- **The correction, or the refusal.** Either "dial 0.44 MOA left and 0.23 MOA down", or "not distinguishable from zero at 10 shots", never a bare number.
- **Linear and angular together**, per entry 91 section 3.
- **Windage and elevation as separate rows**, because a turret has two knobs and nobody dials a diagonal.
- **The uncertainty, in the same units.**
- **When the answer is a refusal, the shot count that would settle it.**

**Nothing else belongs in it.** Sigma, mean radius and extreme spread are group statistics and stay where they are.

### 3. The statistics block is still hard to read after the fix that was meant to fix it, and that is information

Entry 73 section 7 said the panel was a wall of prose and asked for `DESIGN.md` section 19's split: headline figures in the primary panel, reference material behind a disclosure that remembers it was opened. **That landed as the "More figures" expander.** Alan has used it since and still reports the statistics as hard to read.

**So the disclosure was not the problem.** What is left in the primary panel is three headline figures, each followed by two lines of interval, in monospace: **nine lines before a reader reaches anything else.** Every line is correct and the block is dense.

**The concept screen already shows the pattern that fixes it.** Its Selected Detection panel is a label and value table: Position, Diameter, Score, Margin, one row each, label left and value right. **Four facts in four lines.**

So, for the three headline figures:

1. **One row per figure**, label left and value right.
2. **The angular conversion on the same row as its linear value**, not on a line of its own.
3. **The interval as a second line only where it changes a decision**, and otherwise behind the same disclosure as everything else. Sigma's interval earns its place. Extreme spread's probably does not, since extreme spread is already the least informative of the three.

### 4. This is the styling input I said I was waiting for, and it applies to this piece only

Entry 84 section 1 said appearance waits until Alan has used the editor, because a screen styled before it is used is styled against a guess.

**He has now used it and reported the same difficulty twice, unprompted, which is exactly the evidence that was missing.** So the statistics panel can be specified now and this entry does it.

**It does not unblock the rest.** The tool strip, the breadcrumb, the left rail and the paper-coloured sheet still have no report behind them, and they stay where entry 84 left them until there is one.

---

## 2026-09-18, entry 91: the zero correction exists, and shipping it without its uncertainty would invite people to chase noise

**Status: actioned 2026-09-18.**
- **Section 1: built, and the label says group centre.** The correction is its own section with windage and elevation as separate rows, each in a linear and an angular unit at once, following the unit setting, so nobody converts by hand.
- **Section 2: built as the part that decides it.** The uncertainty is on screen in the same units, and where the offset is smaller than the shots can resolve the answer is a refusal and a shot count rather than a number: entry 53 section 3's t(0.975, df) / sqrt(n) times sigma, computed rather than copied, with df = 2n - 2 for a circular group and n - 1 per axis where circularity is rejected. A test reproduces all six rows of your table from the formula.
- **Section 3: all four items.** Named, both units, the uncertainty, and a verdict in words.
- **Section 4: recorded and stopped at MOA and mil.** Clicks need the scope's click value, which is the rifle record entry 90 scheduled in Phase 4, and the panel says so.
- **One correction to your example, in section 1's spirit:** a turret moves the point of impact, so a group sitting left is corrected by dialling right. The readout keeps the two words apart, where the group sits and what to dial.

Alan wants the statistical point of impact and its offset from the point of aim, in mils, MOA, centimetres and inches, so a shooter can dial a scope. **Most of it is already on screen.** Section 2 is the part that decides whether the feature is any good.

### 1. What already exists, and one correction to the wording

The panel already reads:

> Centre from aim: 0.097 in left, 0.060 in high (0.37 MOA left, 0.23 MOA high)

**So the statistical point of impact and its offset from aim are computed and displayed today**, in inches and MOA, split into the two axes.

**Missing from the request: mil and centimetres.** Both are already specified and parked in the README's trailing orphan line, as "a three-axis unit setting: inches, centimetres and millimetres; MOA, mil and SMOA; yards and metres". **So this part of Alan's request is entry 90 section 3's orphan line**, which is a good argument for giving those four items states rather than deleting the line.

**One wording correction, gently.** Alan described it as the centre of the mean radius. **The mean radius is a distance, not a place**: it is the average distance of the shots from the group centre. The place is the group centre, which is the mean of the shot coordinates. **The label on screen should say group centre and not mean radius centre**, because a shooter who confuses the two will misread every number beside it.

### 2. On Alan's own sheet, the correction he would dial is smaller than its own uncertainty

Ten shots, sigma 0.274 in, at 25 yards, offset 0.097 left and 0.060 high.

| | Inches | MOA | Mil | cm |
|---|---|---|---|---|
| **Offset from aim** | 0.114 | **0.44** | 0.127 | 0.29 |
| **95 percent region for the centre** | 0.212 | **0.81** | 0.236 | 0.54 |
| **Smallest detectable offset at 10 shots** | 0.182 | **0.70** | 0.202 | 0.46 |

**The measured offset is 0.63 of the smallest offset ten shots can distinguish from zero.** Dialling it would be correcting for something the data cannot establish is there.

**This matters more than the units do.** A readout that says "0.44 MOA left" with nothing beside it will be dialled, and the shooter will have moved their zero to chase sampling noise. **That is the confident wrong answer `DESIGN.md` section 2 exists to prevent**, and it is the single most common error in practical zeroing.

**The machinery is already built.** Entry 53's detectable-offset table gives the multiplier on sigma at each shot count, verified by a 200,000 replication simulation: 1.603 at three shots, 1.031 at five, 0.664 at ten, 0.402 at twenty-five. **The feature needs no new statistics, only the table it already has.**

**And it gives the shooter something better than a correction: a number of shots.** On this sheet an offset of 0.114 in becomes detectable at 25 shots and not before. **"Shoot fifteen more before you touch the turret" is more useful than a false correction**, and a GroupLab sheet holds exactly 25.

### 3. What the readout should say

1. **The group centre, named as the group centre**, with its offset from aim split into windage and elevation, because a turret has two knobs.
2. **Both a linear and an angular unit at once**, following the person's unit setting: the target is measured in inches or centimetres and the turret is marked in MOA or mil, and making somebody convert between them by hand is the one thing this readout exists to avoid.
3. **The uncertainty on the offset**, in the same units, as a plain interval rather than a symbol.
4. **A verdict in words.** Either the offset is larger than this many shots can resolve, so it is real and here is the correction, or it is not, so here is the number of shots that would settle it.

**The verdict is the feature.** Points 1 to 3 are arithmetic the application already nearly does.

### 4. What is needed to give a correction in clicks, and it is already a known gap

**The output a shooter actually wants is clicks, not MOA.** Turning 0.44 MOA into clicks needs the scope's click value, a quarter MOA or a tenth of a mil or something else.

**That is a rifle record, and entry 90 found rifle records missing from the plan entirely.** `DESIGN.md` section 3 promises records for rifles, barrels, loads and sessions, and only sessions survived into any phase.

**So this request and entry 90's gap are the same gap seen from two directions**, and it is worth saying so rather than treating clicks as a small addition later. **Until a rifle record exists the readout stops at MOA and mil**, which is still usable because every turret is marked in one of them.

**Adjust-to-zero turret corrections is also in the orphan line**, which makes three of that line's four items load-bearing for this one request.

---

## 2026-09-18, entry 90: ten promises with no phase behind them, and three tiers of commitment of which only one is tracked

**Status: actioned 2026-09-18.**
- **Sections 1 and 2: all ten given a phase or a deferral, and which is which is recorded.** Eight scheduled: hit probability in Phase 2 with its at-distance half in Phase 5, distance normalisation in Phase 5, records for rifles, barrels and loads in Phase 4, the secondary mode in Phase 3 where it already works, and the support link, the four themes and the stage timeline in Phase 4. **Two deferred with their reasons:** the parametric editor and the visual designer, which wait on a specification, and assisted hole placement, which is section 5's question.
- **Section 3: both gaps closed.** Significance testing is back on the page, and so is hit probability, which was missing from both plans while being implemented: the engine carries rank and dispersion tests, MANOVA and the dispersion ratio, and three hit-probability estimators. The trailing orphan line is deleted and its four items are features with states.
- **Section 4: the test now reads the scope.** Every In scope bullet must name a phase that exists in section 21, or say it is deferred and cite where the reason is written. Verified by breaking it both ways.
- **Section 5: question 18.** It also found that two of the three things "assisted" could mean are already built and need no definition: a tap snaps to the hole under it, at a radius set by the calibre. Only unprompted detection needs a definition, and that is the part a store-bought target cannot give.

Alan asked whether anything else is missing. **Ten things are**, and section 4 is why they were able to go missing.

I read `DESIGN.md` section 3 bullet by bullet against section 21 and against the README's Planned section, and checked section 19's interface commitments the same way.

### 1. In scope, and in no phase

| Promise in section 3 | In section 21 | In the README's Planned section |
|---|---|---|
| **A parametric target editor** | no | no |
| **A full visual target designer** | no | no |
| **Hit probability** | **no** | **no** |
| **Distance normalisation** | **no** | **no** |
| **Records for rifles, barrels and loads** | sessions only | **sessions only** |
| **The secondary mode for any target**, including store-bought and blank paper | no | no |
| **Assisted hole placement** in that mode | no | no |
| **An unobtrusive support link** | no | **no** |

**Hit probability is named twice in scope**, once as a statistic and once as a thing the ballistic solver exists to serve, and it appears in no phase and nowhere in the README. **Distance normalisation is the same.** Both are reasons the solver is being ported at all, so Phase 5 may intend them without saying so, and that needs confirming rather than assuming.

**The records bullet names four things and the plan carries one.** Rifles, barrels and loads appear nowhere in the Planned section. A load record in particular is what the whole load-development premise rests on, and the word "load" appears there only inside sheet names.

### 2. Promised in section 19, and in no phase

| | In the Planned section |
|---|---|
| **Four themes: dark, light, high contrast, follow system** | no |
| **The analysis showing its work**: a stage timeline the user can scrub, artefacts appearing as they land, clicking a rejection to highlight it on the image | **no** |

**The second of those is one of the most distinctive things in the whole design.** The machinery under it exists, because every stage already emits a structured record and the console form of it gave the Phase 0 and Phase 1 spikes their output. **The screen that makes it visible is in no phase at all**, which is how a headline feature becomes an internal diagnostic by default.

### 3. Things dropped between `DESIGN.md` and the README

**Significance testing appears in section 21 and not in the README's Planned section.** That is a different kind of miss from the ones above: the plan has it and the page lost it.

**The README's Planned section ends with a line of orphans:** "a three-axis unit setting, adjust-to-zero turret corrections, calibre-aware edge-to-edge spread, and a volunteer print pack". **Four named features with no phase and no state**, sitting below the table that exists to give everything a state.

### 4. Why all of this was able to happen, which matters more than the list

**There are three tiers of commitment in this repository and only the middle one is tracked.**

| Tier | What it is | Tracked by |
|---|---|---|
| `DESIGN.md` section 3 | what the project promises to do | **nothing** |
| `DESIGN.md` section 21 | the phases | entry 87's test, against the README |
| The README's trailing line | acknowledged and parked | **nothing** |

**Entry 87's test ties phase names and states between two documents. It does not check that the phases cover the scope, and it does not check the features inside a phase.** So a promise can sit in scope for the life of the project, and a feature can be dropped from a phase's list, and the test stays green while the page looks complete.

**Three changes, in order of value:**

1. **Every section 3 bullet names at least one phase, or carries an explicit deferral with its reason.** A promise with neither is a bug in the plan.
2. **Extend entry 87's test upward**, so a scope bullet with no phase fails the same way a missing phase does. The check that catches this should not be a person reading two documents.
3. **Delete the trailing orphan line or give its four items states.** As it stands it is a second, untracked planning system sitting under the tracked one.

### 5. What I am not claiming

**Some of these may be deliberate.** Hit probability and distance normalisation might be understood as inside Phase 5's solver work, and the secondary mode might be understood as inside Phase 3's marking by hand.

**That is exactly the point.** If they are covered, the phase should say so in words, because a reader cannot tell the difference between a deliberate omission and a forgotten promise, and neither can a test.

**One of them needs a real answer rather than a wording fix.** Assisted hole placement on a store-bought target has no definition to render and difference against, so the detector as built cannot do it. **Whether that bullet is achievable at all is a design question**, and it should be answered before it is either scheduled or quietly dropped.

---

## 2026-09-18, entry 89: the target designer is in scope and in no phase, and the case for 30 bulls is about subgroups rather than precision

**Status: actioned 2026-09-18.**
- **Section 1: the hole is confirmed and the designer is deferred rather than scheduled**, on Alan's instruction that a specification is coming from Jeff first. `DESIGN.md` section 3 and the README both carry the deferral with its reason, which is a state the status table can see.
- **Section 2: swept, and the designer was not the only one.** Ten promises had no phase. Eight now name one and two are deferred, listed in `docs/PHASE1-RESULTS.md`. Entry 87's test is extended upward: a scope bullet with no phase and no deferral now fails.
- **Section 3: recorded.** The case for 30 is what the number divides into rather than the interval it buys.
- **Section 4: checked, and the answer is no at every level.** `dataBlock` has one field set for the sheet, `instance` is a flat map excluded from the definition, and a bull carries only `scoring` as a partition. Labels cannot serve, being explicitly non-unique. **The analysis half already exists**, since the comparison tests all take a group label per shot, so six loads on one sheet could be compared today and nothing can tell them which shot is which. Three routes with their costs are in the results, and the cheapest by far is a session-level mapping that needs no format change and works on the existing 30-bull sheets.
- **Section 5: for Alan and Jeff.** The format question section 4 raises is what their answers decide.

Section 1 is a hole in the plan that the new status table cannot see. Section 3 is a product requirement hiding behind a sheet size.

### 1. `DESIGN.md` section 3 already promises a designer, and no phase builds it

Section 3, In scope, first bullet:

> Generating printable targets with embedded registration markers, **in a built-in library, through a parametric editor, and through a full visual designer**

**A parametric editor and a full visual designer are both in scope, and neither appears anywhere in section 21's phases or in the README's feature list.** Phase 4 carries "a target library", which is browsing what exists, not authoring something new.

**The format is already ready for it.** Section 9 [r3] measured a fully custom target with forty arbitrarily placed bulls at roughly 215 bytes, inside a QR payload with room to spare. **The thing that would carry a user's own design is built and tested. Nothing plans the screen that makes one.**

### 2. The status table is honest and its foundation is not, and that is a gap in the guard

Entry 87's test ties the README to section 21. **Nothing ties section 21 to section 3.** So a promise made in scope can sit there for the life of the project without ever becoming a phase, and the status table will faithfully report the phases that exist while saying nothing about the ones that should.

**Sweep section 3 against section 21 and list every in-scope item with no phase that builds it.** The designer is one. **I would not assume it is the only one**, and the sweep costs one pass through two sections.

**Then extend entry 87's test one step**: every In scope bullet must name at least one phase, or be explicitly marked as covered elsewhere. That closes the loop at the top rather than only at the bottom.

### 3. Thirty bulls already exist, and the statistical case for them is not the obvious one

**The library already carries `GL-CF30-LTR` at 30 bulls, `GL-LR30-TAB` at 30, and `GL-RF36-LTR` at 36.** Whatever Jeff wants to shoot, a sheet for it is already printable.

**The precision argument is weak.** The 95 percent interval on sigma, as a multiple of the estimate:

| Shots | Interval | Width | Against 25 |
|---|---|---|---|
| 25 | 0.834 to 1.249 | 0.415 | 1.000 |
| **30** | 0.847 to 1.222 | **0.375** | **0.904** |
| 36 | 0.858 to 1.198 | 0.340 | 0.818 |
| 50 | 0.877 to 1.163 | 0.285 | 0.687 |

**Going from 25 shots to 30 buys a 10 percent narrower interval.** Real, and not worth redesigning a sheet for. Getting the interval down to a third of sigma takes 46 shots.

**The strong argument is what the number divides into:**

| Sheet | Subgroup splits available |
|---|---|
| **25** | **5 x 5 only** |
| **30** | 2 x 15, 3 x 10, **5 x 6, 6 x 5**, 10 x 3, 15 x 2 |
| 36 | 2 x 18, 3 x 12, 4 x 9, **6 x 6**, 9 x 4, 12 x 3, 18 x 2 |

**Six charge weights at five shots each, or three groups of ten compared against one another, fit on 30 and cannot fit on 25.** That is almost certainly the analysis Jeff means, and it is a structural property of the number rather than a statistical one.

### 4. The requirement that implies, which is not a bigger sheet

**A 30-bull sheet on its own changes nothing.** If the software treats all 30 as one composite group, six charge weights at five shots each is still reported as a single group of 30 and the comparison Jeff wants cannot be made.

**What is actually needed is for a sheet to know which bulls belong to which subgroup**, so that one sheet can carry six loads and the analysis can compare them. Some sheets already carry a nine-field load block, so there is a foothold, and whether the load block ties fields to specific bulls is worth checking rather than assuming.

**So the request is not 30 bulls. It is subgroups within a sheet**, and that is a format question, an analysis question and a designer question at once, which is a further reason section 1's gap matters.

### 5. What to ask Jeff, and it is not "why 30"

**Ask for the analysis by name and for what it needs the software to know.** Three questions:

1. **Which analysis**, specifically, does 30 allow that 25 does not.
2. **How are the 30 divided** when he shoots them: six loads of five, three of ten, or something else.
3. **What does the result compare**: each subgroup against each other, each against a reference, or a trend across an ordered ladder.

**Those three answers turn a sheet preference into a specification.** "Thirty is better" cannot be built from; "six charge weights of five shots each, compared pairwise for a significant difference in dispersion" can, and it also says what the designer in section 1 has to let somebody express.

---

## 2026-09-18, entry 88: the one unexplained flag may be a yawed bullet, and my attempt to measure it was not trustworthy

**Status: actioned 2026-09-18.**
- **Section 1: measured, and the answer is a third cause that is neither of the two proposed.** Every detection now carries its aspect, elongation, solidity and hull area. S1b reads elongation 1.60 with solidity 0.59, the least convex mark on the sheet. A yawed bullet makes an oval and an oval is convex, so yaw predicts a high solidity; and a composited pair of real holes the size of S1b reads an elongation over 2. **What the shape says is one hole with a ragged rim, counted by its convex hull:** S1b's hull holds 2.10 single holes' area and its ink holds 1.23, an equivalent diameter of 0.329 in. Four of the five oversize flags on real material are the same shape. The candidate fix, flagging on ink area or requiring a solidity, is recorded with its numbers and not applied, per entry 83 section 3.
- **Section 2: accepted, and the measurement is in the code as you ask.** The figures from the guessed crops are withdrawn. Nothing was inferred from them.
- **Section 3: entry 84 done in this commit**, both items.

Small. Section 1 is a candidate cause for the only detector flag left unexplained on the corpus's best sheet, with a one-line test. Section 2 is a measurement of mine that should not be believed.

### 1. S1b is a single hole covering 2.18 holes' area with no ink in it, and nothing has named a third cause

Claude Code's own correction leaves this open: **S1b encloses no printed artwork at all, measures 0.429 in, and is confirmed by the shooter as one shot.** So neither of the two causes the flag names applies. It is not two holes and it is not a hole joined to ink.

**There is a third cause and this project has never written it down: a bullet that arrives yawed rather than point-first makes an oval hole considerably larger than its calibre.** A 220 grain subsonic 300 Blackout at 25 yards is exactly the load where that happens, because a heavy subsonic can be marginally stabilised at short range, and a sighter fired low is exactly where somebody finds out.

**The test costs one line and belongs where the footprints already are.** `ink-proximity.json` already carries per-detection geometry. **Add each detection's aspect ratio and its area beside its diameter**, and the three causes separate by shape without any new machinery:

| | Shape |
|---|---|
| Clean hole | round, aspect near 1 |
| **Yawed or angled hit** | **oval, aspect well above 1, single lobe** |
| Two holes read as one | two lobes with a waist between them |

**If S1b is a clean oval, the flag is behaving correctly and only its wording is wrong.** The message currently offers two causes, neither of which fits, which is the same failure entry 73 section 2 corrected once already: naming causes that exclude the real one.

**And it would be a finding about shooting rather than about software.** A target that can say "this hole is oval, the bullet was not flying straight" is telling a handloader something they want to know and cannot easily see by eye.

### 2. I tried to measure it from the scan and the numbers were wrong

I cropped the scan at guessed page coordinates, thresholded for anything darker than paper, and took the largest connected region. **On the scoring bulls that region is the printed ring, not the hole**, which is why one of my rows reported a major axis of 1.2 inches. On S1b I caught a fragment and reported an equivalent diameter of 0.129 in against the detector's 0.429.

**None of those numbers mean what the row said they meant**, and I am recording that rather than quietly dropping it, because every figure I quoted this week that turned out to be wrong had the same shape: a measurement whose construction I had not checked.

**I also could not reliably tell which crop was which**, because I was locating marks by guessing page coordinates while the detector knows exactly where each one is. **That is the reason section 1's test belongs in the code and not in an analysis of mine.**

### 3. Entry 84 is still in the inbox and still worth doing

It was committed by accident, un-tracked, and left in place. Its two items stand:

- **A synthetic 25-shot sheet with injected errors**, timed as a rehearsal of the Phase 3 gate and explicitly not recorded as passing it, so that when a real 25-shot sheet exists the session measures the editor rather than finding something obvious.
- **Re-measuring the photograph ratio**, because entry 79's 0.986 was taken before the single-scale defect was fixed.

---

## 2026-09-18, entry 84: the editor behaves like the concept and does not look like it, and the gate needs 25 shots nobody has fired

**Status: actioned 2026-09-18.**
- **Section 1: recorded, and nothing styled.** The appearance waits on an hour of Alan's use, and the concept screen's look is a Phase 3 feature with the state "not started" rather than an unwritten intention.
- **Section 2: rehearsed and explicitly not gated.** A synthetic 25-bull sheet with the five injected errors: 15 queue items, settled with the right answers in 22 key presses, one tap and 9 ms of software time. Four findings, of which two matter: one extra shot in a cell displaced a chain of three assignments and all three reached the queue with the right bull offered, and the merged pair cannot be finished from the keyboard because its item offers no way to add the second shot.
- **Section 3: the photograph ratio re-measured at each blob's own scale, 0.986 to 0.948**, with the spread falling from 11 percent to 4 and the frame means from a 15 percent range to 6. `PhotographHoleToCalibre` changed to the new figure. The rise from 24 to 35 spurious detections on the clean photographs is recorded deliberately as the correct direction.
- **Section 4: recorded.** Each hypothesis written with the test that would kill it. It is the practice being kept.

Section 2 is a blocker with a cheap way around it. Section 3 is a small correctness follow-up that should not be forgotten.

### 1. Behaviour first was the right order, and appearance is now the remaining half

The review queue, the counter, the card with its choices, the keyboard path of Space, Enter, a bull label, and N, and a "keep it" decision that saves and undoes: **that is the concept's behaviour, and it is in.**

**On the friend's scan all ten scoring shots land on their true bulls, where nearest-bull would have put two wrong, and three presses of Enter settle the queue.** That is the thing working.

**What is not in is the concept's appearance**: the icon tool strip, the breadcrumb header, the paper-coloured sheet on dark chrome, the left rail, the typography. **That was not asked for and it should not have been**, because a screen styled before it is used is styled against a guess.

**The order from here is Alan first, not planning first.** An hour in the current editor, doing the job it exists for, produces the list a styling pass should be written against. Anything I specify before that is me describing a picture.

### 2. The Phase 3 gate needs a 25-shot sheet and both real sheets have ten

`DESIGN.md` section 21 sets the gate at a full 25-shot target with several misassignments corrected in under two minutes. **Neither real sheet can test it**, and a range session is the critical path it has always been.

**There is a useful halfway step that costs nobody a trip to the range.** Build a synthetic 25-shot sheet with errors deliberately injected: two contested assignments, one merged pair, one false candidate, one bull with two shots and one with none. **Time the workflow against that now.**

**It is not the gate and it must not be recorded as passing it.** What it does is let the two-minute workflow be exercised, timed and improved before anybody drives anywhere, so that when a real 25-shot sheet exists the session measures the editor rather than discovering an obvious problem with it.

**When the real sheet is shot, one session produces three things at once**: the Phase 3 gate timed properly, the ground-truth marking entry 83 wants for scoring detector changes, and a second real sheet for the corpus.

### 3. Two things the scale fix leaves behind

**The photograph ratio must be re-measured.** Entry 79's 0.986 was taken with a single scale for the whole image, which section 2 of this week's work has just shown is wrong on any oblique frame. **Anything resting on that number rests on a measurement made with the defect in place.**

**Spurious detections on the clean photographs rose from 24 to 35**, because far-side residue now reads at its true larger size and fewer slivers fall under the floor. **That is the correct direction and it should be left alone**, per entry 83 section 3. Worth recording the rise deliberately, so that a future reader does not see 24 become 35 and treat it as a regression.

### 4. The third explanation was the right one, and none of the first two were mine by accident

The false flags on the oblique frames were explained, in order, as out-of-focus corners, then as residue on printed ink, then as **one scale applied to an image whose true scale varies from 0.85 to 1.30 across the sheet.** Only the third was right, and it was found by testing the second rather than by reasoning further.

**All three were mine, and the only reason the wrong two cost minutes instead of days is that each was written with the test that would kill it.** That is now the fourth time this week the pattern has held, and it is the single practice worth keeping from it.

**It is also the strongest argument for section 1's ordering.** A styling specification written from a screenshot would be the same kind of guess, and unlike a detector hypothesis it would take a week to disprove.

---

## 2026-09-18, entry 87: there are two size checks disagreeing five to one, and a status table that must not be allowed to lie

**Status: actioned 2026-09-18.**
- **Section 1: the table scored the detector's flag**, which the queue counts, so it stands. Reproduced on Alan's scan: the detector fires once, the screen's check five times, on holes reading 0.267 to 0.310 in that the check reads at 0.508 to 0.607 in because it takes in the ring.
  - The screen's check is gone. The detector's flag is the only size opinion, and it is a queue item.
  - Its evidence is kept as a test: the dark region under a mark on a printed line reads over three times wider than on a hole beside one.
- **Section 2: the README's Planned section** lists every phase of DESIGN.md section 21 with one of the four states and its gate, and the features inside each phase with a state each. `ReadmeTests` fails if the two documents disagree, and refuses test counts, percentages and dates.

Reported in `docs/PHASE1-RESULTS.md` "Entries 87 and 86".

Section 1 corrects entry 86 section 2 before it is actioned. Section 2 is Alan's README request with the part that makes it survive.

### 1. The detector counts one oversized mark and the screen prints five warnings

The log from Alan's session says, on every one of four runs:

> 14 holes inside the registered sheet, 32 candidates rejected, 0 of them hole-sized inside exclusion zones, **1 oversized**

**One.** That is S1b, and Alan kept it, which the log records as `review.choose kind=Oversized action=Keep`.

**The panel underneath prints five oversize warnings**, on shots 1, 4, 6, 7 and 10.

**So there are two different size checks running on the same image and they disagree five to one.** The detector's own oversized flag fires once. The screen's calibre size check fires five times. Only the first is in the review queue.

**That explains the inconsistency entry 86 section 4 complained about.** The queue reads "0 of 14 need review" honestly, because the queue tracks the detector's flag and that flag is satisfied. The five red sentences below it come from a check the queue does not know about.

**And it partly retracts entry 86 section 2.** I said the ground-truth list behind the per-frame false-flag table was wrong on five marks. **If that table scored the detector's flag rather than the screen's check, it was right and I was comparing two different measurements.** Entry 82 section 4 said as much in passing: "the on-screen size check still rises near ink, 78 percent against 2 percent on punched scans", and I read past it.

**So do not rewrite the ground truth yet. Establish which check the per-frame table scored.** If it scored the detector's flag, the table stands, the five screen warnings are the defect, and the ground truth is fine.

**Either way the five screen warnings are false**, because Alan has confirmed with the shooter that all five are single holes. **What changes is where the defect lives.**

**Two size checks is one too many.** Whichever survives should be the only one, it should feed the review queue, and nothing should print a size opinion that the queue has not counted. **A number on screen that no queue item accounts for is how a person learns to ignore the queue.**

### 2. The README status table, and the two things that stop it becoming untrue

Alan wants a detailed planned-feature section with status marks, kept current.

**The README already has a Planned section with the eight phases.** What is missing is the features inside each phase and a state on each.

**Use four states, not three.** Three collapses a distinction this project is living inside right now:

| Mark | Means |
|---|---|
| **Not started** | no code |
| **In progress** | being built, not usable |
| **Built, not proven** | the code exists and works, and its gate has not been met or cannot yet be run |
| **Done** | its gate has been met and recorded |

**The assignment editor is the reason.** It was built this week, it works, and `DESIGN.md` section 21's Phase 3 gate cannot be run because no sheet with 25 shots exists. **Calling that finished would make a public page untrue, and calling it in progress would be equally wrong.** The four-state version says exactly what is true.

**Two guards, because a hand-maintained status list goes stale and this one already has form.** Entry 60 found the README claiming Windows was the only buildable platform for days after that stopped being true, and deliberately left test counts out because they go stale.

1. **A test ties it to `DESIGN.md`.** Every phase in section 21 must appear in the README's table with exactly one state, and every feature listed under a phase must carry one. **The test fails if a phase or feature exists in one document and not the other**, so the two cannot drift apart silently.
2. **Changing a state belongs in the commit that changes the thing.** Not a separate chore run at each push, because a chore gets skipped and then the page lies. **A gate met is a state change in the same commit that records the gate.**

**Three things to leave out**, following entry 60: **no test counts, no percentages, no dates.** All three go stale within days and none of them tells a reader anything the state marks do not.

**And say what "Done" is measured against.** Each phase already has its gate in `DESIGN.md` section 21. **Put the gate's one-line summary beside the state**, so a reader can see what the mark is claiming rather than trusting it. The Planned table already does this for some phases and should do it for all.

---

## 2026-09-18, entry 86: I told Alan to delete a real shot, and the ground truth behind the threshold work is wrong on five marks

**Status: actioned 2026-09-18.**
- **Section 1: recorded.** S1b is a bullet hole, Alan has restored it, and the rule is that a recorded defect is evidence about the past: check it is still there before using it to explain something on screen. It cost one wrong figure of mine, corrected below.
- **Section 2: retracted by entry 87 section 1.** The per-frame table scored the detector's flag, not the screen's check, so the ground truth stands. One correction: the table counted the flag on S1b as a hole joined to the print note, which was my assumption. S1b encloses no ink, so Alan's scan has one false detector flag of 14.
- **Section 3: measured, and the answer is no for the detector.** Enclosed ink correlates with diameter at -0.53 on Alan's scan and -0.28 on the friend's, and flagged punched marks enclose slightly less ink than unflagged ones. It is what inflated the screen's check, which is gone. Every detection now carries the quantity.
- **Section 4:** settled by entry 87 section 1. There is one size opinion and the queue counts it. The cascade is recorded as designed behaviour.

Reported in `docs/PHASE1-RESULTS.md` "Entries 87 and 86".

Section 1 is my error and needs undoing in the application. Section 2 invalidates a measurement the recent threshold work rested on. Section 3 is the refinement that explains why the corpus test missed it.

### 1. S1b is a real bullet hole and I said it was ink

I told Alan the fourth sighter detection was the print-note ink from entry 77 and to press N. **He did, and it was a shot.**

**Three independent confirmations:**

- **The pixels.** At 600 DPI the mark above the "Print at actual size, 100 percent." line is a torn brown-grey hole with a ragged crown. The sentence beside it is flat, sharp-edged and blue-grey. **They do not look remotely alike at full resolution.**
- **The shooter.** Alan reports ten shots at the scoring bulls and four at the sighters.
- **The log.** `detect.run` reports "4 sighter shots for 3 sighter bulls", and separately rejects a string of blobs at y = 10.65 and y = 10.80 as too small or outside every bull's cell. **That is the print-note ink being correctly refused.**

**Undo it**: the selected-shot panel has "It is a shot", or Undo in the toolbar.

**How I got it wrong, precisely, because the shape of it is worth more than the mistake.** Entry 77 recorded a false detection from the print-note sentence at page (2.909, 10.784). S1b sat near that position, so I matched it by proximity and did not look at the pixels. **The log shows that defect is already fixed.** So I took a finding that had since been resolved, treated it as the current state, and used it to explain a mark I had not examined.

**The rule that follows: a recorded defect is evidence about the past. Before using one to explain something on screen, check it is still there.**

### 2. The five oversize flags are false, and the ground truth used to validate the thresholds says otherwise

Alan has looked at shots 1, 4, 6, 7 and 10 and **all five are single holes.** No doubles anywhere on the sheet.

They read 0.547, 0.607, 0.600, 0.607 and 0.508 in against 0.441 for one .308 hole. **So five of ten scoring shots on a clean 600 DPI scan are falsely flagged as possibly two holes.**

**The per-frame table reports the friend's scan as 0 of 13 false flags, with or without a calibre.** Those two statements cannot both be true.

**The likely reconciliation is the one that matters**: the ground-truth list used for that table records those five marks as genuinely oversized, because nobody had asked the shooter. **The shooter has now answered, and the ground truth is wrong on five of thirteen marks on the sheet the thresholds were validated against.**

**Correct the ground truth first, then re-run the per-frame table and the confirming sweep.** Every threshold decision from entry 81 onward was checked against real holes holding the veto, and five of those real holes were labelled wrongly.

**These same five, with the same five diameters, appear in the entry 73 screenshots.** They predate all of this week's work and have survived every change to it, which is consistent with a defect nothing has yet addressed rather than a regression.

### 3. The corpus test asked how far a detection is from ink, and the question is how much ink is inside it

Entry 78 measured each detection's distance to the nearest printed edge and found no association on the scans. **That is the wrong quantity, and this sheet shows why.**

**A hole centred on a ring line and a hole sitting beside one both have a distance to ink of nearly zero.** Only the first has a large amount of ink within its own footprint. The corpus test pooled them and the effect cancelled.

**The visible evidence, side by side at full resolution:** shot 1 straddles bull 1's inner ring and is flagged at 0.547 in. Shot 2 is a hole of similar size sitting beside bull 2's inner ring rather than on it, and is not flagged.

**The test, and it is a different measurement rather than a re-run:** for every detection, compute the area of expected printed artwork falling **inside the detection's own footprint**, and compare that between flagged and unflagged marks. **If the flagged ones enclose substantially more ink, the mechanism is the blob absorbing the line it sits on**, which inflates the measured diameter while leaving the centre where it was, and that is consistent with entry 73 section 3 finding no centre displacement.

**I am offering this as a hypothesis with its test attached, and it is the fourth explanation I have proposed for these five marks.** The previous three were focus, residue proximity and image scale, and the third was right about the oblique photographs and is not what is happening here, on a flatbed scan with no scale variation at all.

### 4. Two smaller things from the same screen

**The review queue says "Nothing needs review" while five red warnings sit below it.** Either the oversize warnings belong in the queue as items, or the queue should not claim the sheet is settled. **As it stands the counter is telling a person something the panel beneath it contradicts**, which is how a queue stops being trusted.

**The cascade is real and worth keeping as a design observation.** One detection's status changed four other shots from needing review to settled, because the sighter counts went from four-for-three to three-for-three and matching became possible. **That is the counts rule working exactly as designed**, and it is worth recording that a single decision can legitimately resolve several queue items at once, because it will look like a bug to somebody one day.

---

## 2026-09-17, entry 83: the per-frame table says obliquity, not focus, and the detector has reached the point of diminishing returns

**Status: actioned 2026-09-17, with the Phase 3 gate left for Alan to time.**
- **Section 2: the falsely flagged holes are not the ones nearest ink.** They are the ones on the near side of an oblique frame, which the detector read large because it converted sizes with one scale for the whole image. Sizes are now read at each blob's own scale.
  - False flags without a calibre fall to 2 on 5822 and 1 on 5824, and to 0 on every other frame.
  - The clean photographs' residue rises from 24 to 35.
- **Section 3:** left alone. 5823's boundary is recorded with its geometry: one blob covering the sheet.
- **Section 4: the assignment editor is built.**
  - A review queue lists contested assignments, possible merges, doubled bulls, shots with no bull and refused candidates, each with its choices.
  - The screen shows a counter, a card and Discard edits. Space, Enter, typed bull labels and N work it.
  - On the friend's scan, all ten scoring shots are on their true bulls, and three presses of Enter settle the queue.
  - The two-minute gate needs a 25-shot sheet and a person.

Reported in `docs/PHASE1-RESULTS.md` "Entry 83".

Section 4 is a direction call and is the one that matters most.

60 spurious to 24 on the clean photographs, and the merged-pair flag now reaches the screen. **Both of those are good and neither is what this entry is about.**

### 1. I repeated a characterisation without checking it, and the table I asked for contradicts it

Entry 82 section 4 said almost all the false flags sit in the three oblique out-of-focus frames. **I took that from the previous report and passed it on. The per-frame numbers say something different:**

| Frame | Without calibre | With .308 |
|---|---|---|
| Both scans, the friend's photograph, **5820** | **0** | **0** |
| 5819 | 1 of 14 | **0** |
| 5821 | 3 of 14 | **0** |
| **5822** | **5 of 14** | **3 of 14** |
| **5824** | **6 of 12** | **3 of 12** |

**5822 and 5824 are the worst, and entry 71 called them the two useful off-axis frames.** 5819 and 5821, the wide ones with the sheet small in frame, are better and a calibre clears them entirely.

**So the pattern is obliquity, not the sheet being small and not focus.** The square-on frame is clean, both scans are clean, and every off-axis frame has false flags without a calibre.

### 2. A calibre does not rescue an oblique frame, and that is the interesting part

**5822 and 5824 keep 3 false flags each even with .308 named.** That is roughly a quarter of their real holes, on the two frames a contributor would think were their best.

**A mechanism that fits, and it connects to work already done.** Entry 77 measured the flat-homography worst bull at 0.011 to 0.059 in on these frames against 0.005 on the square ones. **A larger registration residual means the expected artwork lands slightly off, leaving residue along printed edges, and residue touching a real hole merges with it into a blob that reads oversize.** Entry 78 section 2 already found that photograph residue clusters on printed artwork at a median of 0.023 in.

**The test needs no new data.** The per-detection distance to nearest artwork already exists for these frames. **Check whether the falsely flagged holes on 5822 and 5824 are the ones nearest printed ink.** If they are, this is the same residue defect reaching real holes rather than a separate size problem, and the fix belongs with entry 78 section 2 rather than with the flag.

### 3. Three things that are right and should not be pushed further

**The remaining 24 are honestly irreducible.** Ten residue blobs of 0.20 to 0.30 in could be two joined .17 holes, and saying so rather than guessing is the correct answer. Leave them.

**The bimodality and merged-pair interaction is benign.** A sheet where a quarter of the marks are merged pairs is a sheet in real trouble, and asking for the calibre is exactly the right response to it. Nobody should try to make bimodality distinguish the two cases without a calibre, because the information is not there.

**5823 detecting no holes at all is a measured boundary and is worth recording as one.** There is now a real frame where the whole pipeline finds nothing, and knowing where that edge sits is guidance we can give contributors. `PHASE1-RESULTS.md` should name it with its geometry rather than treating it as a bad file.

### 4. The detector has had its due, and the editor has not

**This was right to do.** It began because Alan used the application and found the numbers were wrong, and since then a 55 percent sigma error has been fixed and checked against hand-merged truth, real spurious detections have gone from 17 to two or three, split and merge are handled with real holes holding the veto, and the size rule has an absolute floor. **That is a serious week of work on the thing that was actually broken.**

**It is now producing right answers on good inputs**, and the remaining items are a quarter of the holes on the two most oblique frames in the corpus, ten blobs that genuinely cannot be resolved without more information, and one frame that fails outright for optical reasons already quantified.

**`DESIGN.md` section 13 opens by saying the editor is built before the detector, not after it.** Entry 69 found that most of the assignment editor already exists in Core, entry 70's plumbing is done, entry 74's pinning rule is done, entry 75's numbering is done. **What does not exist is the screen**, and it is what Alan named as the target four days ago.

**So: after section 2's test, stop tuning the detector and build the assignment editor.** The gate is already written in `DESIGN.md` section 21, a full 25-shot target with several misassignments corrected in under two minutes, and the friend's scan is now a fixture that gives real material to correct.

**One thing that should come with it.** Alan has been asked twice for a ground truth pass and has not had a tool worth doing it in. **Every detector change from here should be scored against that pass**, and the editor is what makes producing it bearable. The two pieces of work want each other.

---

## 2026-09-17, entry 82: the quarter-point rule calibrates on noise, which is why 64 only became 60

**Status: actioned 2026-09-17.**
- **Section 1: checked.** The quarter-point is taken over round marks only, and the clean photographs' residue is all elongated, so there the rule had no size rather than a noisy one. The conclusion is the same.
- **Section 2:**
  - The single-hole size is graded: calibre, sheet from 12 round marks, tentative from 5, and otherwise the floor of 0.16 in. It is always clamped to 0.16 to 0.60 in, and the floor vetoes but never flags.
  - The clean photographs go from 60 spurious detections to 24. The 10 residue blobs left are as large as two joined .17 holes.
- **Section 3:** no real sheet carries two calibres.
  - Two clearly separate sizes now flag nothing and ask for the calibre in one sentence.
  - On the composite sheets, where merged pairs are half the marks, that means none of their pairs is flagged without a calibre.
- **Section 4:** false flags by frame are 0 on both scans and on the friend's photograph, and 0 to 6 on the mounted frames, the most in the oblique ones.
- **Section 6: the flag was not on the screen.** It now reaches the marking, the file, the canvas, the panel and `analyze`.
- **Section 7:** the quarter-point is graded, and its flags are tentative and drawn faint below 12 marks.
- **Section 5:** recorded.

Reported in `docs/PHASE1-RESULTS.md` "Entry 82".

Section 1 is a logical flaw rather than a tuning question, and the unshot-sheet result is its proof. Section 2 is a fix available without a calibre. Section 5 retracts my solidity suggestion.

### 1. The size rule assumes most marks are holes, and the case that needs it most is the case where they are not

Without a calibre, the single-hole size is the quarter-point of the sheet's own marks once there are five of them. **That assumes the marks are mostly real holes.**

**The clean Phase 0 photographs are sheets with no holes at all.** Every mark on them is residue. The quarter-point of sixty residue blobs is the size of a residue blob, so the rule adopts noise as its reference and then judges the noise against itself. **The veto cannot fire because the thing it would veto is what set its scale.**

**That is why the residue fix moved 64 spurious detections to 60.** It is not a weak fix, it is a fix that cannot engage on that image, and it will equally fail to engage on any real photograph where residue outnumbers holes. A three-shot group photographed badly, with twenty residue blobs and three holes, has its hole size set by residue.

**A rule that degrades as the problem gets worse is the wrong shape**, and this one is at its weakest exactly where it is needed most.

### 2. The sheet already knows what a bullet hole can be, without anybody typing a calibre

Registration gives the scale in real units and the definition gives the geometry, so **the software knows how many inches a pixel is before it looks at a single mark.**

**Every bullet hole in existence is between roughly 0.17 in and 0.60 in across**, from a .17 centrefire to a .58 muzzleloader. That is an absolute prior, it needs no calibre, it needs no marks, and it holds on a sheet with nothing on it.

**Bound the inferred size by that range**, so the quarter-point rule can refine within it but never adopt a reference outside it. On the clean photographs the residue blobs would then fail the bound outright rather than becoming the standard everything else is judged against.

**This does not replace the quarter-point rule**, which is doing real work on sheets that are mostly holes. It stops it running off the end.

### 3. The mixed-size flood is probably the same mistake entry 80 caught

The flag firing on 19 to 65 marks a case on the mixed-size synthetic holes is alarming, and **those sheets were built from a survey mixing .264, .308 and .338**, which is a model of the corpus rather than of a sheet.

**A real sheet is one rifle and one calibre.** A quarter-point taken across three calibres sits near the smallest, so everything larger reads oversize, and the flood follows. **Check whether any real sheet in the corpus carries more than one calibre before treating that number as a defect.** I expect none does.

**The genuine case is rare and worth handling honestly rather than by flagging half the sheet.** Somebody changes load mid-sheet, or shoots a rimfire sighter row beside centrefire. **When the mark sizes are clearly bimodal, the quarter-point rule is invalid and the software should say so and ask for a calibre**, rather than emitting forty red flags. One sentence beats forty.

### 4. Report the false-flag rate by frame, not pooled

Six real holes falsely flagged with a calibre and thirteen without, out of 99, reads as 6 and 13 percent. **Almost all of them are in the three oblique out-of-focus frames**, which entry 77 measured as optically incapable of being sharp.

**So the pooled rate is a statement about a set that includes three frames we already know are unusable.** Report it per frame. The number that matters to a user is the rate on a frame they could reasonably have taken, and on scans it is zero.

**Same correction as the scan ratio in entry 80 section 3, for the same reason**: the sheet or the frame is the unit, not the hole.

### 5. Solidity does not separate, and I should not have proposed it from medians

Spurious blobs reach 0.89, real single holes fall to 0.59, joined pairs to 0.70. **The ranges overlap completely and my entry 81 section 3 suggestion is dead.**

**I proposed it from two medians, 0.65 against 0.95, and medians cannot show separation.** Two well-separated medians are consistent with total overlap, which is exactly what the distributions show. The right call was made by asking for the distributions before building on it, and the rule that follows is simple: **never propose a discriminator from summary statistics. Ask for the distributions first, or do not raise it.**

Size does the job instead, and the reported figures are clean: among blobs elongated 1.8 or more, spurious ones hold at most 1.08 holes' area and joined pairs at least 1.80. **That is a real gap with nothing in it**, which is what a discriminator looks like.

### 6. The synthetic two-per-bull drop is acceptable, and its weight should be lower than it looks

94.0 to 87.5 percent at 600 DPI, and 96.4 to 90.5 at 300.

**Acceptable, on one condition that is now met**: the merges are flagged. Entry 81 argued a loud failure beats a quiet one, and the new 1.35-hole flag catching all 78 composited pairs is what makes it loud. **Confirm the flag is visible on the marking screen and not only in the analysis output**, because a flag a person does not see is a quiet failure wearing a loud one's clothes.

**And two holes in one bull is a case the sheet is designed to prevent.** It is entry 75's `7a` and `7b` exception. It happens when somebody fires more rounds than there are bulls, which is real but is not the ordinary case, so **a percentage point there is not worth a percentage point on single holes.** Worth saying because that table will be read again later without this context.

### 7. The five-mark floor is too low for a quarter-point

The quarter-point of five marks is the second smallest of five. **That is an extremely noisy estimate of a percentile**, and the rule switches on at exactly that count as though it were reliable.

**Grade it rather than switching it.** Below about a dozen marks the estimate deserves a wide tolerance or a fallback to section 2's absolute bound alone. **Where the rule is running but unreliable, the flag should be quieter, not the same red as on a full sheet.**

---

## 2026-09-17, entry 81: question 17, and 1.80 goes in everywhere rather than only when a calibre is named

**Status: actioned 2026-09-17.**
- **Section 1:** the split elongation is 1.80 everywhere, and the single-hole size now applies without a calibre too, as the sheet's own 25th percentile mark. The synthetic records are re-recorded, with the reason in `PHASE1-RESULTS.md`.
- **Section 2: pairs composited from Alan's real holes showed the gap was real.**
  - Under the old flag, every pair 0.10 or 0.15 in apart was one unflagged mark.
  - The flag is fixed: a whole mark of 1.35 single holes or more is flagged. Now all 78 such marks are flagged and no composite single hole is.
  - On the real sheets, the new flag falsely flags 13 of 99 single holes without a calibre, 6 with one, almost all in the oblique frames.
- **Section 3: solidity does not separate the three populations.** Real single holes go down to 0.59 and joined pairs to 0.70. Size does, where the shape asks for a split: spurious blobs hold at most 1.08 holes and joined pairs at least 1.80.
  - Entry 78 section 2 is built on that: an elongated blob too small for two holes and at least 2.2 long is refused as residue.
  - Without a calibre, it still cannot act on a sheet with fewer than five holes.
- **Section 4:** recorded where the ratio is defined.

Reported in `docs/PHASE1-RESULTS.md` "Entry 81".

Section 1 answers question 17 and disagrees with the recommendation attached to it. Section 2 is a test that fills the gap the answer depends on. Section 3 is a discriminator nobody has tried that the data already suggests.

### 1. Adopt 1.80 everywhere, now

Claude Code recommends 1.80 only when a calibre is named, and everywhere once a real merged pair has been measured. **I think it should go in everywhere immediately**, for four reasons, the last of which is the strongest.

**The real holes veto, and that was the rule.** Entry 80 section 2 set it: synthetic material for coverage, real holes with the veto. **1.45 splits two real holes in two. 1.80 misclassifies none.** On the evidence that has authority under the rule we agreed, 1.45 is simply wrong and is shipping.

**The two failure modes are not symmetric, and the quiet one is the one 1.45 causes.** Splitting a real hole invents a shot that was never fired. It raises the count, it inflated sigma from 0.390 to 0.607 on the one sheet where this was measured, and **two ordinary-looking marks on a torn hole give a person nothing to notice.** Failing to split a genuinely merged pair loses a shot and leaves one mark of roughly double area, which is the thing the size check exists to report. **A loud failure is worth several quiet ones.**

**The case for 1.45 rests on five synthetic pairs, on the population entry 80 just disqualified**, and on a mechanism that "almost never separates overlapping pairs by shape at any setting". Five pairs from a method that does not work on that case is not evidence against a threshold that no real hole disputes.

**And splitting the rule by whether a calibre is named would create two detectors.** Entry 80 section 5 made the calibre a provenance field precisely because it already changes the answer. **Adding a second mechanism that also changes the answer, keyed off the same field, compounds exactly the problem that field was added to disclose.** One sheet would give different shot counts depending on whether somebody typed .308, for two unrelated reasons at once. One threshold, always.

**On the committed synthetic records: re-record them.** A gate record that encodes a threshold known to cut real holes in half is not worth preserving, and `PHASE1-RESULTS.md` should say in one line why the synthetic figures moved.

### 2. The gap this answer rests on, and how to close it this week

My second reason assumes a merged pair is reported as oversized. **Claude Code says plainly that this is not scored, so I am leaning on an unverified property**, which is the error I have made repeatedly this week. It needs measuring rather than assuming.

**There is no real merged pair in the corpus and one does not need to be shot.** Composite one from real holes: take two real hole images from the scans, at their real scale and texture, and overlay them at a range of centre separations from touching to heavily overlapped. **That is synthetic geometry with real texture, which is much closer to the truth than the synthetic holes entry 80 disqualified**, and it is honest as a bridge rather than as a substitute.

Score two things across that range:

1. **Does 1.80 leave the pair as one mark**, and from what separation onwards.
2. **Is that mark flagged oversized**, which is the whole basis for calling this the loud failure.

**If merged pairs are not reliably flagged, that is what to fix**, and not by reverting the threshold, because reverting reinstates a defect that is measured against real holes in exchange for one that is not.

### 3. Solidity separates better than elongation and is not being used

Entry 78 section 2's photograph numbers:

| | Elongation, median | Solidity, median |
|---|---|---|
| Spurious blobs | 4.6 | **0.65** |
| Real holes | 1.17, at most 1.67 | **0.95** |

**The proposed fix uses elongation and size. It does not use solidity, and solidity is the cleaner signal here.** A real hole is a hole: near convex, 0.95. A split fragment is a crescent or a sliver, 0.65.

**Elongation has a known trap that solidity may not share.** Two real holes joined by the closing step reach about 2.6, which is why a plain elongation cap could silently drop them, and that is the reason the proposal has to add a size veto at all. **A pair of joined round holes is still a reasonably convex shape**, so solidity may not fall for them the way elongation does.

**Measure it before believing it.** Report the full distributions, not medians, of elongation and solidity for three populations: spurious split halves, real single holes, and real pairs joined by closing. **Then pick whichever gives a clean gap**, or both. If solidity separates all three cleanly, the fix is simpler than the proposal and does not need the size veto to carry it.

### 4. The two-sheet ratio is good enough for this job and not for the next one

Alan's sheet 0.952, the friend's 0.934, pooled 0.944, and two sheets cannot settle it.

**That limitation does not block anything here**, and it is worth saying so before it becomes a reason to wait. **The ratio is being used to tell one hole from two, which is a factor-of-two judgement.** An error of a few percent in 0.944 cannot flip it. Two sheets is ample.

**It would not be ample for anything that needs the absolute size**, such as reporting a measured calibre back to the person or comparing hole sizes between loads. **Record the distinction where the ratio is defined**, so that the next use of it does not inherit a precision that was never established.

---

## 2026-09-17, entry 80: the split fix lands on truth, and the sweep about to set its thresholds is running on the wrong population

**Status: actioned 2026-09-17, with the split threshold put to planning as question 17.**
- **Section 2:**
  - The synthetic holes simulate the survey's mix of .264, .308 and .338, and read at 1.088 of .308. They are scaled by 0.871 to read 0.944 of it, the scan figure.
  - The sweep now reports synthetic coverage and the real veto apart. The real holes veto every setting at elongation 1.45; only 1.80 misclassifies none.
  - Nothing is adopted.
- **Section 3:** two scan sheets, 0.952 and 0.934, pooled 0.944, recorded with the sheet as the unit.
- **Section 4:** the harness no longer sizes a split half. With the halves excluded, the detector's own oversize rate is 0% everywhere.
- **Section 5:** a marking, its file and its report record what the detection ran with, a calibre or none, apart from the calibre named now.
- **Entry 78 section 2:** measured. Every spurious detection on a photograph is a split half, with a median elongation of 4.6 against 1.17 for true holes. The fix is proposed in question 17 and not built.
- **Section 1:** recorded as a measurement.

Reported in `docs/PHASE1-RESULTS.md` "Entry 80".

Section 1 records a result. Section 2 is a question that must be answered before the sweep's thresholds are adopted, and it is the reason this entry exists tonight rather than tomorrow.

### 1. The worst known defect is fixed and checked against hand-merged truth

| Friend's earlier sheet | Holes | Sigma |
|---|---|---|
| Without a calibre | 15 | **0.607 in** |
| At .308 | 13 | **0.391 in** |
| **Hand-merged truth** | **13** | **0.390 in** |

**0.391 against 0.390.** A 55 percent error in the headline statistic is gone, and it was verified against a hand-made answer rather than against a plausible-looking output. **That is the strongest thing to happen to this pipeline this week** and it should be recorded in `PHASE1-RESULTS.md` as a measurement rather than as a fix.

### 2. The sweep's own first line says the synthetic holes are not the right size

`split-cal.txt` reports its synthetic single-hole size as **0.338 in over 848 holes**.

The ratio of detected diameter to stated calibre, just measured on real material:

| | Holes | Mean | Spread |
|---|---|---|---|
| Scans | 24 | **0.944** | 0.039 |
| Photographs | 75 | **0.986** | 0.110, frame means 0.92 to 1.07 |

**If the synthetic sheets simulate .308, their ratio is 0.338 / 0.308 = 1.097.** That is 16 percent above the scans and 11 percent above the photographs, and **it sits outside the entire range of frame means measured on real paper.**

**The sweep has 848 synthetic holes against 99 real ones**, so synthetic material outnumbers real by more than eight to one and will dominate anything fitted to it. **A split threshold tuned on holes that read 10 percent large relative to their calibre is tuned for a population that does not exist.**

**So, before the sweep's numbers are adopted, answer one question: what calibre do the synthetic sheets simulate, and what is their detected-diameter ratio against it?** If that ratio is not close to 0.944, the synthetic holes need rescaling or the sweep needs reweighting.

**And whatever the answer, change the shape of the fit.** Synthetic material is for coverage, because 848 holes explore the parameter space in a way 99 cannot. **Real holes get the veto.** A threshold that the synthetic sweep prefers but that misclassifies a single real hole is the wrong threshold, and the report should say so explicitly rather than presenting one optimum.

### 3. The uncertainty on those ratios is smaller than the holes deserve

**24 scan holes are not 24 independent measurements.** They come from a small number of sheets, and paper, printer, scanner, lighting and the stated calibre are all shared within a sheet. So the hole-to-hole spread of 0.039 understates the uncertainty in the mean, possibly by a lot.

**Claude Code already did this correctly for the photographs** by reporting frame means of 0.92 to 1.07 alongside the pooled figure. **Do the same for the scans: report the per-sheet means and let the spread between sheets carry the uncertainty.** If there are only two or three sheets, say so, because "0.944 with a spread of 0.039" reads far more settled than three numbers would.

**One thing that is already clear and worth keeping**: 0.944 on scans sits well below 1, which says **the detector reads a scanned hole a few percent smaller than the bullet that made it.** That is a small, real, systematic effect, and having it measured at all is new.

### 4. My evidence for entry 78 section 3 was partly an artefact, and the conclusion survives

Claude Code found that all 54 "oversized" punched detections were halves of split blobs, each reporting the parent blob's diameter. **So my oversized median of 0.7529 in was measuring a blob, not a mark**, and the neat picture of "split at 0.29, oversized at 0.75, two directions of one threshold" was partly built on that.

**The conclusion stands and the evidence for it was weaker than I presented.** One threshold can still fail both ways, `grouplab holes split-calibration` now measures it per blob, and the sigma result in section 1 is what actually establishes it.

**This is the second time today a number I quoted came from a harness whose construction I had not checked.** The rule that follows: when I cite a figure from a measurement file, say what one row of it represents. Had I written "each oversized row is one detection's own diameter", I would have had to check whether that was true.

### 5. A calibre now changes the answer, so the record has to say which one was used

Thirteen holes at .308 and fifteen without. **The same image now analyses differently depending on a field the person may or may not have filled in**, which is correct behaviour and a provenance obligation.

**A marking and an exported analysis must record the calibre they were produced with, including its absence.** Two markings of one sheet, one with a calibre and one without, are not comparable and nothing downstream should treat them as though they were.

### 6. What is left, in order

1. **Section 2's question**, before the sweep's thresholds are adopted. Everything else waits on it.
2. Section 3's per-sheet means.
3. Section 5's provenance field.
4. Entry 78 section 2, the photograph residue, which was already waiting on thresholds.
5. Section 1 into `PHASE1-RESULTS.md`.

---

## 2026-09-17, entry 79: the bullet is not the hole, a handoff rule for long jobs, and a self-lock

**Status: actioned 2026-09-17.**
- **Section 1: measured.** Detected diameter over calibre is 0.944, sd 0.039, on 24 scan holes, and 0.986, sd 0.110, on 75 photograph holes. The detector uses that ratio times the calibre, and the calibre only separates one hole from two.
- **Sections 2 and 3:** `CONTRIBUTING.md` carries the rule and the convention of running long command-line jobs from a published copy.

Reported in `docs/PHASE1-RESULTS.md` "Entries 78 and 79".

Section 1 must reach the threshold work before its sweep result does. Section 2 is a process change, because the same thing has now happened twice.

### 1. Entry 78 section 4 is unsafe as written, and Claude Code's own probe says why

Its probe found that **"calibre must match hole size: at 0.30 in, overlapping 0.235 in holes read as less than one."**

**That is the flaw in what I proposed.** Entry 78 section 4 said to feed the calibre into segmentation, and I wrote as though the calibre and the hole were the same measurement. **They are not, and this project established that months ago**: a .308 bullet leaves a bright aperture of roughly 0.21 in inside a torn crown reaching about the calibre. So a detector handed 0.308 as the expected hole size may be looking for something half again larger than what it will find, depending on which part of the hole the segmentation actually latches onto.

**Do not use the calibre as the expected diameter. Measure the relationship first.**

The corpus can answer it directly and no new data is needed. **For every sheet whose calibre is known, compare the detected diameters against the stated bullet diameter, and report the ratio with its spread.** That gives one number and an uncertainty, and it is what the segmentation should be given rather than the calibre itself.

**Two things worth knowing before that runs**, because they will shape how the ratio is read:

- **The ratio is probably not a constant.** Paper weight, backing, velocity and how square the bullet arrives all change how much the crown tears. If the spread is wide, a single ratio is the wrong model and the calibre becomes a loose prior rather than a target.
- **It may differ between scans and photographs**, because a flatbed sees the crown lit differently from a camera. Report the two corpora separately rather than pooled.

**If the spread turns out to be wide, entry 78 section 4 should be narrowed to using the calibre only to separate the two failure directions** (a mark near half the expected size is a split, a mark near double is a merge) **rather than to set an absolute expected size.** That weaker use survives a variable ratio and still fixes the defect that matters.

### 2. A long job should end a turn, not be waited on

Claude Code has now twice finished its work, started something long, set a monitor, and gone quiet when the monitor expired. Both times the report read as a crash and was not one. **Both times real results were sitting committed and unread.**

**The rule: when a job will outlast the turn, stop the turn.** Report what was started, the exact path its output will appear at, and the one command that reads it. Then stop. The next instruction reads the result.

**That is better than waiting even when the monitor works**, because a blocked session cannot be given anything else to do, and because a summary written before the long job finishes is still a summary, which is what was missing both times.

**Two specific cases to treat this way**: a CI run, and any sweep or corpus pass that takes more than a few minutes.

### 3. The CLI locks its own build output

Building while a sweep ran failed with the CLI binary locked by the running `grouplab` process, and it was worked around with an alternate output path.

**That is the same shape as the problem Alan has with the application**, which is why he now runs from `C:\Dev\grouplab-run` rather than from the build output. **A long CLI run should not execute out of `bin`**, for the same reason: it blocks every build for as long as it runs.

Worth a published copy or a documented convention rather than a per-incident workaround, and it is small.

---

## 2026-09-17, entry 78: the ink test answers, my one-mechanism hypothesis is wrong, and split and oversized are one threshold in two directions

**Status: actioned 2026-09-17 in part.** Section 3's thresholds wait on the split calibration sweep, and section 2 follows them.
- **Section 1:** accepted. No difference-stage fix for split or oversized was pursued.
- **Section 3: corrected before it was built on.** The 54 oversized punched detections are all split halves, most of correctly separated neighbours, and read as oversized only because a half reports its blob's diameter. The split decision is still one threshold with two failures. `grouplab holes split-calibration` measures it per blob, and it was still running at the report.
- **Section 4:** the size a hole of the named calibre is detected at reaches segmentation, as entry 79 section 1 corrected it.
  - It only vetoes a split of a blob too small to be two holes, and flags a whole blob of two or more holes rather than cutting it.
  - Without a calibre, nothing changes.
  - The friend's earlier sheet at .308 reads 13 holes with sigma 0.391 in, against 0.390 in hand-merged.
- **Section 2:** not started. **Section 6:** both CI runs passed on all three platforms, so the fingerprint test holds.

Reported in `docs/PHASE1-RESULTS.md` "Entries 78 and 79".

I read `scans/phase1/measurements/ink-proximity.json` directly rather than waiting for a summary. 2,875 detections, 2,811 from punched scans and 64 from photographs of unshot sheets. **The test entry 77 section 4 asked for has answered, and it answers against me.**

### 1. Inside the scans, proximity to printed ink explains nothing at all

| Detections in punched scans | n | median distance to artwork | within 0.05 in of artwork |
|---|---|---|---|
| **all** | 2811 | 0.0972 in | **28.9%** |
| spurious | 17 | 0.1181 in | 29.4% |
| split | 36 | 0.0807 in | 27.8% |
| oversized | 54 | 0.1329 in | **25.9%** |
| flagged oversize | 162 | 0.1417 in | 25.3% |
| none of the above | 2542 | 0.0950 in | 29.2% |

**Every symptom sits at or slightly below the baseline.** Oversized marks are if anything *farther* from printed ink than a typical detection.

**Entry 77 section 4 proposed that all three symptoms were one defect in the difference stage, near printed edges. On the scans that is simply false**, and it was my hypothesis from eyeballing five marks in one screenshot and seeing ring lines under them.

**It also means the original warning text was wrong on the facts, not only presumptuous.** "It sits on the printed target: this is printed ink under the mark" named the one explanation the corpus contradicts.

### 2. All of the ink association lives in the photographs, and every lens does it

| Detections on photographs of unshot sheets | n | median distance to artwork | within 0.05 in |
|---|---|---|---|
| all, and every one is false by construction | **64** | **0.0234 in** | **68.8%** |

**An unshot sheet photographs with sixty-four holes in it, and two thirds of them are within a twentieth of an inch of printed ink.** Across `main`, `telephoto` and `ultrawide` frames alike, so it is not a lens property.

**That is a real mechanism and it is confined to photographs.** The scans register well enough that the expected artwork cancels; the photographs do not, and what survives along a printed edge becomes a hole. **This is the one thing entry 77 section 4 got right, and it is a difference-stage or registration-residue question rather than a segmentation one.**

**It also removes the last reason to box the "Print at actual size" sentence.** That was one false detection in a scan, and at corpus scale scans show no ink association, so a box there would have been built for a pattern that does not exist.

### 3. Split and oversized never co-occur, and their sizes say why

| | n | median diameter |
|---|---|---|
| all scan detections | 2811 | **0.3356 in** |
| split | 36 | **0.2946 in** |
| oversized | 54 | **0.7529 in** |

**Zero detections are both.** Split fragments measure just under one .308 bullet. Oversized marks measure more than two.

**These are not two defects. They are one segmentation threshold failing in each direction**: too aggressive and a torn hole breaks into pieces, too permissive and two neighbouring holes become one mark. **So they must be calibrated together**, because tightening to fix the merges will make more splits and the reverse.

**Split is the one to lead on**, because it is the defect that put sigma at 0.607 in where the truth is 0.390 in on the friend's earlier sheet. **A 55 percent error in the headline number is the most serious thing currently known about this pipeline.**

### 4. The calibre is already known and is being used too late

The person supplies a calibre. Today it is used **after** segmentation, to print a warning about a mark that is already wrong.

**A .308 bullet makes a hole of a known size. Two marks of 0.29 in sitting 0.2 in apart are one hole split. A single mark of 0.75 in is two holes merged.** The information that resolves both is the number the user typed before the analysis ran.

**Feed the calibre into segmentation rather than into a message about its output.** That is not new machinery and it is the natural fix for section 3.

**Two constraints, so this does not turn into a detector that only finds what it expects.** It must still work with no calibre set, because the field is optional and most sheets will not have one. And a mark that disagrees with the calibre must still be reported rather than reshaped into agreement, because a genuinely odd hole is a finding and not an error.

### 5. Order

1. **Section 3, split and oversized, calibrated together**, with split leading because of the sigma error.
2. **Section 4's use of the calibre**, as the mechanism for doing item 1 well.
3. **Section 2's photograph residue**, which is a different stage and a different fix.
4. **Nothing about boxing the print note.** Section 2 closed it.

### 6. One thing left hanging that is not mine

Claude Code stopped without a summary because it was waiting on CI for the entry 77 push, specifically because **the new fingerprint test assumes the PDF comes out byte-identical on Linux and macOS as on Windows.** It was right not to guess. That result is still unknown and should be read before anything else is built on top of it.

---

## 2026-09-17, entry 77: depth of field explains the angled frames and does not explain the gate, and the exclusion box nearly ate a real shot

**Status: actioned 2026-09-17.**
- **Section 3 item 1:** a blob refused only for lying inside an exclusion zone is counted, and the S5-S8 stage line always states the count. It would have shown entry 76's caption change as one swallowed blob that held both lost detections.
- **Section 3 item 2:** `grouplab corpus counts` compares detection counts with a record that carries each definition's artwork fingerprint, and a test fails whenever the printed artwork no longer matches.
  - The committed corpus is the Phase 0 set plus 18 runs of its 300 DPI letter scans punched with synthetic holes. On its own it now catches the caption change.
  - A local manifest holds the real shot sheets outside the repository.
- **Section 4: the three do not rise together.**
  - **The first mechanism is S8's merged-neighbour split.** Every spurious detection in the corpus, 101 of 101, is a split half, and so is every real hole detected twice. Spurious detections rise toward ink because the residue lives there; splits do not rise.
  - **The second is the marking screen's size check,** which reads printed ink joined to a hole. The detector's own diameter barely moves. Nothing was fixed.
- **Section 5:**
  - The name reads name, middle dot, identifier, in the top margin, only where it is 60 dmm from every cell and 30 dmm from everything printed. The tiles carry none.
  - The standing check showed every count unchanged.
  - No box was added over the print note.
  - Cancel names its worst case, about 6 seconds on a 600 DPI scan.
- **Section 2:** recorded. The angled frames are focus-limited; the square-on frames are not, and the gate is still unexplained.

Reported in `docs/PHASE1-RESULTS.md` "Entry 77".

Section 1 is a measurement that turns "meeting the gate is a photography instruction" into an instruction with numbers, and then takes most of it back. Section 2 is a near-miss that should change how printed artwork is changed. Section 4 answers question 16 and the other decisions.

### 1. The out-of-focus corner is forced by the optics, and I can say by how much

The far corner being soft is not bad luck. **At the distance these were shot from, it could not have been sharp.**

Measured from `IMG_5820` itself rather than assumed: the sheet spans 3647 of 5712 pixels, which at a 35 mm equivalent of 24 mm puts the camera at **about 310 mm from the paper**, giving **312 pixels per inch**.

**Depth of field at that distance, for the iPhone 17 Pro main camera at f/1.78:**

| Distance | Depth of field, 2-pixel criterion |
|---|---|
| **310 mm, as shot** | **26 mm** |
| 500 mm | 67 mm |
| 750 mm | 150 mm |
| 1000 mm | 266 mm |

**And the depth an angled sheet occupies:**

| A4 tilted | Depth range across the sheet |
|---|---|
| 15 degrees | 77 mm |
| **30 degrees** | **148 mm** |
| 45 degrees | 210 mm |

**At 310 mm and 30 degrees, 26 mm of sharpness has to cover 148 mm of sheet.** About a sixth of the paper can be in focus. The far corner's markers failing to decode is the arithmetic, not an accident.

**The distance that fixes it is about 750 mm**, roughly 30 inches, where depth of field just covers a 30 degree tilt. **Resolution there falls to 129 pixels per inch**, which is still comfortably above anything the markers need.

Assumptions, because they matter: a 2-pixel circle of confusion, chosen because this is corner localisation rather than a pleasing picture, and sensor dimensions inferred from the 24 mm equivalent and the 6.765 mm actual focal length rather than measured. **A pictorial criterion would be two and a half times more generous and would still not save 310 mm at 30 degrees.**

### 2. And none of that explains the gate, which is the part I want to be careful about

Claude Code's conclusion was that the cause is optical and meeting the gate becomes a photography instruction. **That is right for the angled frames and it is not right for the gate.**

**The two square-on frames are the ones closest to passing, at 0.0053 and 0.0059 in.** A square-on sheet has almost no depth range, so depth of field is not what limits them. **Whatever is holding the best frame 6 percent outside 0.005 in, it is not focus.**

So the picture splits in two and the halves need different work:

- **The angled frames are focus-limited**, quantified above, and that is now a shooting instruction rather than an open question.
- **The square-on frames are limited by something unidentified**, and they are the ones at the gate.

**There is also a tension worth stating rather than stepping around.** `DESIGN.md`'s photograph gate is deliberately an **off-axis** photograph, because that is what a shooter actually takes. "Shoot square" removes the problem by removing the test. **The honest form of the instruction is a distance, not an angle**, and the prediction to check is that an off-axis frame taken from about 750 mm comes inside the gate where the same angle at 310 mm cannot.

**That is one more photograph session and it would settle a question the corpus has never been able to answer.** It also needs no new shooter: the sheet is still in Alan's hands.

### 3. The exclusion box swallowed a real hole, and the next one will too

Adding the printed name widened the exclusion box over the caption, and on the existing scan **the wider box hid a real sighter hole: 15 detections became 13.** It was caught, reverted, and it should not be filed as a near-miss and forgotten.

**An exclusion box does not solve a detection problem, it converts it into a blindness problem.** Anything inside is invisible, and a shot that lands there is dropped from every statistic with nothing on screen to say so. That is the worst failure this pipeline can have, because a missing shot is silent and a wrong shot is not.

**So the proposal to add another box over the "Print at actual size" sentence is the wrong reflex**, even though it would remove the false detection. It is the same mechanism that just lost a real hole, applied a second time.

**Two things regardless of what else is decided:**

1. **Report what the boxes swallow.** The pipeline should count candidate blobs falling inside exclusion regions and say so. A swallowed hole then appears as a number rather than as nothing, and the near-miss above would have announced itself instead of needing to be noticed.
2. **Make the accident into a standing check.** Any change to printed artwork re-runs the corpus and compares detection counts before and after. Claude Code ran that test by chance and it is the only reason the change was reverted.

### 4. Three symptoms on printed ink, and a single test that would say whether they are one thing

Three separate findings this week all sit on printed ink:

| Symptom | Where |
|---|---|
| Marks reading 0.5 to 0.6 in against a 0.441 in bullet hole | entry 73 section 2, five shots, all touching ring lines |
| A false detection with no hole under it | the "Print at actual size" sentence |
| **One hole detected twice**, giving sigma 0.607 in where the truth is 0.390 in | bulls 3 and 10 on the friend's earlier sheet |

**That last one is a 55 percent error in the headline statistic**, which makes it the most serious detector defect currently known.

**Entry 73 section 3 already told us something precise about the shape of this.** Flagged marks sit 0.017 in from their hole's own centre against 0.019 in for unflagged ones. **So ink does not move centres. It breaks segmentation**: sizes, splits, and spurious blobs. That is one mechanism, not three, and it narrows the search a great deal.

**The test, on data already in hand:** over the whole corpus, measure each detection's distance to the nearest printed edge in the expected artwork, and plot oversize, split and spurious rates against it. **If all three rise together as that distance goes to zero, they are one defect in the difference stage** and the fix is there rather than in three places. If they do not, they are three defects and each needs its own.

**Do this before fixing any of them individually.** Three separate fixes to one cause is how a detector becomes untunable.

### 5. Question 16 and the remaining decisions

**The printed name: outside the analysed region entirely, not in a larger box.** Claude Code's margin recommendation is right and the reason is stronger than stated. A name inside the region needs an exclusion box, an exclusion box is what section 3 just described, and the margin removes the need for one. **If the name cannot go somewhere no box is required, it does not go on the sheet.** Name first and identifier second on that line, as entry 76 section 4 said.

**Every sheet already printed still reads correctly**, which is the property that makes the margin the answer rather than a compromise.

**The print-note false detection: no new box.** Section 4's test first. If the residue turns out to be general, the box would have hidden one instance of a problem that is on every sheet.

**Split holes: yes, its own entry, and it is section 4's third row.** It should be investigated with the other two rather than alone.

**Hand-placed shots keeping the calibre ring: accept, and it is correct rather than a limitation.** A hand-placed mark makes no measurement, so it should make no size claim, and the calibre ring says what a bullet of that calibre would measure. That is the right thing to draw.

**Cancel taking effect between stages: accept.** Name the worst-case wait in the progress text so a person watching a long stage knows the button worked. A Cancel that looks ignored is worse than a slow one.

---

## 2026-09-17, entry 76: the ellipse aspect needs its null, entry 56 lost its only real corroboration, and five decisions

**Status: actioned 2026-09-17, except section 4's printed name, which is question 16.**
- **Section 1:** `CircularAspect` gives the aspect's circular null exactly, and it matches the simulation here. The report and the panel print the median and how often circular shots exceed the measured aspect, in section 10's form. `STATISTICS.md` section 7 has the table.
- **Section 2: the hypothesis does not hold.**
  - Re-run with the two split duplicate detections merged, the earlier sheet gives σ 0.390 in and 2 of 10 misassigned, at ratio 3.84.
  - Its sighters were never in its figures.
  - `STATISTICS.md` section 9.3 records the table with its one real-paper corroboration, and says how weak that is.
  - The pipeline itself reports σ 0.607 in on that sheet, because two holes crossing printed ink were each found twice. That is a detector defect for its own entry.
- **Section 3: the camera was always nearer the paper's top**, so top against bottom cannot separate the two causes. Left against right can: in three of four oblique frames, the bad bottom corner is the one farther from the camera, and its markers failed to decode where the photograph is out of focus. That is optical. A small lift at the bottom corners is not excluded; a frame taken from below would settle it.
- **Section 4:**
  - The scan is not published.
  - The marker check is fixed, and all six frames pass on merit.
  - Rings are drawn at the measured diameter, with the calibre beside them.
  - Detection starts on opening a recognised sheet, with progress and Cancel.
  - **The printed name was built and reverted.** Its wider exclusion box hid a real hole on Alan's already-printed scan, 15 holes to 13, so question 16 asks where the name can go. The check also found the scan's print-note false positive.

Reported in `docs/PHASE1-RESULTS.md` "Entries 75 and 76".

Sections 1 and 2 are measurements. Section 3 is a test that separates two explanations using frames already in hand. Section 4 answers the five decisions Claude Code asked for.

Two of my three predictions were wrong. The sighter fix took extreme spread from 2.224 in to 1.361 in as predicted. **The ellipse aspect rose rather than fell, and the scan has zero misassigned shots against my two to three.** Both are worth more than the predictions were.

### 1. The error ellipse aspect is reported without any reference, and it misleads in both directions

The panel prints **"error ellipse aspect 2.818, major axis at 26.7 degrees"** as a bare fact. A person has no way to know what that number does when nothing is wrong.

**Simulated, 400,000 replications, shots drawn from a perfectly circular process:**

| Shots | median aspect | 75th | 90th | 95th | 99th |
|---|---|---|---|---|---|
| **10** | **1.53** | 1.83 | 2.21 | 2.51 | 3.25 |
| 12 | 1.46 | 1.71 | 2.02 | 2.25 | 2.80 |

**A perfectly round ten-shot group reports an aspect of about 1.5 half the time.** Anyone reading the current panel would see elongation in a group that has none, on every second target they analyse.

**And for this sheet the number does mean something.** An aspect of 2.818 or more happens in **2.5 percent** of ten-shot circular groups, so the elongation is probably real and the 26.7 degree axis is probably worth looking at. **Nothing on the screen distinguishes those two cases**, and they are opposite conclusions from the same readout.

**The project already solves this exact problem one line below.** The flyer test reads "worst shot at 2.40 mean radii; a group of 12 is expected to put its worst at 1.95", which is a statistic with its null attached, from `STATISTICS.md` section 10. **The ellipse line should follow the same pattern**: aspect 2.82, where ten circular shots give about 1.5 and exceed 2.82 one time in forty.

**This is not a new statistic and not new machinery.** It is the same treatment already applied next door, withheld from the one readout most likely to make somebody chase a problem that is not there.

### 2. My misassignment prediction was wrong, and the interesting part is why entry 56 no longer has any real support

Zero of ten scoring shots are misassigned. Sigma is 0.274 in, the bulls are 5.5 sigma apart, and entry 56's model expects about 0.15 of a shot in ten at that spacing. **The model and the observation agree. My input to the model was wrong**, because I assumed this sheet would sit where the friend's earlier sheet did, at a ratio of 3.89.

**It does not, and the reason may be the defect that was just fixed.** The earlier sheet implied a sigma of 0.385 in. This one, same rifle, same load, same distance, measures 0.274 in. **The earlier sheet was analysed before the sighter pooling was fixed**, and if sighter holes were assigned to scoring bulls there as well, its sigma was inflated by exactly the mechanism entry 73 section 1 removed, its spacing-to-sigma ratio was pushed down, and its "2 of 10 misassigned" was partly counting those same sighter holes.

**Re-run the friend's earlier target through the fixed pipeline.** If sigma falls and the misassignment count drops toward zero, that settles it.

**The consequence matters more than the cause.** Entry 56's misassignment table was validated against 400,000 simulated shots, so the model itself stands. Its one corroboration on real paper was that earlier sheet. **If that measurement was contaminated, then after this fix there is no real-paper corroboration of the table at all**, because both real sheets now report zero. **Say so in `docs/STATISTICS.md` rather than leaving the table looking field-tested.** A table validated only in simulation is fine; a table that looks validated on paper and is not, is the kind of thing this project exists to avoid.

### 3. The mounted gate is 6 percent away, and the six frames can tell us why

`IMG_5820` reaches **0.00531 in** against a 0.005 in gate under the surface model, with `IMG_5819` at 0.00587 in. Alan's own seven frames ranged 0.006 to 0.067 in. **An independent mounting, by a different person, with tape on cardboard, is the closest anything has come.**

The finding to pull on is that **the large errors sit on clean bulls at the bottom corners of the sheet rather than on the bulls with holes in them.** Two explanations, and they are not close to each other:

- **Optical.** The camera was above the sheet, so its lower edge is farther and more foreshortened, fewer pixels land per inch of paper there, and every marker in that region localises worse. This predicts nothing about the paper.
- **Mechanical.** Tape at four corners pins the sheet at four points and the paper puckers near the fixings, and a pucker involves stretching, which a developable surface cannot represent by definition. This predicts a real limit on the model.

**The six frames separate them, and no new data is needed.** They were taken from different positions within twenty-five seconds of each other, of one sheet that did not move.

> **If the error is optical, the worst region moves with the camera** and is always the part of the sheet farthest from it in that frame. **If it is mechanical, the worst region stays at the bottom corners of the paper** in all six, whatever the viewpoint.

**Report the per-bull residual for each of the six frames as a map over the sheet**, and the answer will be visible without any statistics. **If it is optical, the gate is a photography instruction** and the guidance becomes shoot square to the sheet and fill the frame. **If it is mechanical, the surface model has a named limit** and the fallback in `PHASE0-RESULTS.md` section 4.5 comes back onto the table.

### 4. The five decisions

**Scan consent: no, and do not reason about intent.** The consent record lists six files and the scan is not one of them. Intake refusing the submission is the guard working, and overriding it once teaches everybody that the guard is a nuisance. **The route is to raise the page's size limit, have the shooter upload the scan through the page, and get a real record.** The scan stays local for testing until then, which costs nothing because it is already local.

**The four held photographs: no, do not accept by name. Fix the check.** They were held because intake's quick check found 0 to 3 markers on frames that all register with 33 to 38. **That is a defect in the check, and accepting its output by hand is working around a bug in the one mechanism that decides what gets published.** Fix it, re-run, and let the four pass or fail on their merits.

**Rings before a calibre is set: draw the measured diameter.** The detector measured one, it is real, and it is in sheet units so it scales for free. **A centre mark hides the single most diagnostic number the detector produces**, which is the same number that catches merged holes and ink, as this week's oversize warnings showed. Draw the measured diameter always, and when a calibre is set draw the expected one beside it in a visibly different style, so the comparison that generates the warning is visible rather than described.

**Detect on open: yes, when the image is recognised as a GroupLab sheet.** The sheet self-describes and the software already knows. A button that makes a person ask for the only sensible next step is friction with no decision in it. Three conditions: **do nothing and say so** when the image is not a recognised sheet, **keep the manual button** for re-running, and **make it cancellable with visible progress**, because the scan is 35 megapixels and a frozen window during a long detect is worse than a button.

**The printed name: name first, identifier second, same caption line.** Something of the form `GroupLab 5x5 Load Development, A4 · GL-20J3-Y141-0BN3-EYME`. The name is for the person holding the paper and the identifier is for the software and for support, so the name leads. Keep the actual-size sentence exactly where it is, because it is the more important instruction on the sheet.

**One thing to check before touching the renderer, and it is not obvious.** Render-and-difference compares the image against the expected artwork. **The caption is inside the analysed region** on this sheet, and there is a detection near it. **So changing the caption changes what the detector expects for every sheet already printed**, and a sheet printed today and analysed next month would differ from its own definition. Establish whether the caption is excluded from the difference region before changing it; if it is not, that exclusion is part of this change rather than a follow-up.

### 5. On my own accuracy, briefly, because the pattern is now clear

Yesterday and today I was wrong about the OpenCV packaging, a browser, a git config, the concept image being absent, five screenshot observations, the print mechanism, the ink-merging hypothesis, the ellipse aspect and the misassignment count.

**The pattern is not random. Every prediction I got right came from reading the source or the data first; every one I got wrong was a cause I supplied for an effect I had only observed.** The spread prediction held because I had read `ShotAssignment` and seen the dashed lines. The aspect prediction failed because I assumed one cause for an elongation I had not decomposed.

**So the working rule, stronger than entry 66's:** when I state a cause, say what I read that supports it, and when I have read nothing, say that instead and attach the test. **Every wrong call in that list cost minutes because it carried a test. That is the only reason the list is survivable.**

---

## 2026-09-16, entry 75: one numbering system on the screen, and it is the one printed on the paper

**Status: actioned 2026-09-17.**
- **Sections 1 to 4:** a shot is labelled by its bull on the canvas, in the SHOTS list, in `grouplab analyze` and in the marking file. The list is in the sheet's order, a doubled bull reads `7a`, `7b` by position, and a shot with no bull reads `unassigned` in the alert colour. No per-detection index is shown anywhere.

Reported in `docs/PHASE1-RESULTS.md` "Entries 75 and 76".

A rule from Alan, and it is not negotiable: **a shot on bull 1 must be 1 everywhere it appears.** Anything else is confusing and should not be done.

### 1. What the screen does now

Two numbering systems are drawn in identical boxes, side by side, and nothing distinguishes them.

- The box at a bull's centre is **the bull number**, printed on the paper.
- The box beside a hole is **that shot's position in the SHOTS list**, in the order the detector emitted it.

The SHOTS list reads `1 bull 5`, `2 bull 4`, `3 bull 1`, `4 bull 2`, `5 bull 3`. So bull 1's hole is labelled 3, bull 2's is labelled 4, and bull 5's is labelled 1.

**Alan's first reading was that the software had assigned bull 1's hole to bull 3.** That is the natural reading and the display gave him no way to reach another one.

### 2. Why it is worse than confusing

**A number beside a bullet hole reads as shot order**, the sequence the rounds were fired in. That is a real quantity in load development and a shooter will assume it is what they are looking at.

**The detector cannot know firing order and never will.** So the screen implies a fact it does not have, in the place a person is most likely to act on it. `DESIGN.md` section 2 says this software must never give a confident wrong answer, and an implication counts.

### 3. The rule

**One numbering system, and it is the bull's.** A shot is identified by the bull it sits on: in the panel, on the canvas, in the review queue, and in anything exported. On a one-shot-per-bull sheet that identifier is unique, and the person already has it because it is printed beside the bull on the paper in their hand.

1. **The SHOTS list is ordered by bull and labelled by bull.** Bull 1's shot is row 1. Sighters follow the scoring bulls, S1 to S3.
2. **The canvas carries bull numbers only.** No per-detection index anywhere.
3. **Detection order is an implementation detail and is not shown.** If something internal needs a stable identity, it stays internal.

### 4. The two exceptions, and they must not look like bull numbers

The rule holds because the sheet is one shot per bull. Two cases break that, and both are what the review queue exists for.

- **A bull with more than one shot.** Label them so they cannot be read as bull numbers: **`7a` and `7b`**, never `26` and `27`.
- **A shot with no bull**, which is what the holes below the sighter row are. These need a name that is not an integer: **the word unassigned**, or a mark carrying no number at all.

**Both cases are abnormal and should look abnormal.** A person glancing at the sheet should see at once that something needs a decision, and a plain integer in a box is the one thing that says the opposite.

### 5. The concept already did this, and I did not notice until Alan asked

In `docs/figures/screens/assignment-editor.png` there are **no per-detection numbers on the canvas at all**: bull numbers, a ring on each detection, and a review queue naming things as "contested between bull 4 and bull 9" and "bull 17 had no detection".

**That is twice today the concept has answered a question I was treating as open.** Entry 66's lesson again: read what the project already holds before reasoning from first principles.

### 6. Consequence for the ground truth baseline

The current indices are stable within a run and renumber as soon as the shot set changes, which entry 73 section 1's sighter fix will do.

**So the ground truth lists must record the bull alongside the index**, as `label 3 on bull 1`, against a named commit. Once this entry is implemented the index is gone and the bull is all that remains, which is the right long-term form for that baseline anyway.

---

## 2026-09-16, entry 74: entry 70 conflated two different decisions, and a hand-placed shot must not pin its bull

**Status: actioned 2026-09-16.**
- **Section 1:** `MarkedShot.BullChosen` records whether a person chose the bull, set by click a hole then a bull, and saved in the marking file. The matching reads it alone, so a hole added by hand is matched like a detection. **One sentence here is not how the code works:** a corrected shot does not have a chosen bull by definition, because moving a shot or marking it not a shot also corrects it.
- **Section 2:** recorded in `DESIGN.md` section 13 beside the rule, with the requirement that the screen tell a reopened marking from one with nothing to review. The format is unchanged.

Reported in `docs/PHASE1-RESULTS.md` "Entry 74 and entry 73 section 1".

Section 1 corrects entry 70 section 3 and answers the question Claude Code raised. Section 2 records a limitation its report named in passing that should not be discovered later.

Claude Code implemented entry 70 section 3 exactly as written and then asked the right question:

> A hand-placed shot is pinned to its nearest bull. So adding a missed hole next to a detection pushes that detection to another bull, and it shows as moved. That follows entry 70 as written; if a hand-placed hole should be matched like a detection instead, the rule needs changing.

**The rule needs changing, and the reason is that entry 70 treated two different decisions as one.**

### 1. Placing a hole and choosing a bull are not the same decision

When a person clicks to add a hole the detector missed, they are saying **"there is a hole here"**. That is an observation about the image, and it is theirs absolutely: nothing should ever move a mark a person placed.

**They are usually not saying anything at all about which bull it belongs to.** The software chose that for them by snapping to the nearest one, and entry 70 then treated that software choice as a human decision and gave it the strongest constraint in the system. **A choice the person never made ends up shoving a real detection off its bull.**

That is backwards, and it is worst in exactly the situation the editor exists for. Somebody working through a sheet correcting misses would find each correction silently rearranging the shots around it.

**The rule, restated:**

1. **Position is the person's whenever they placed or moved a mark.** Nothing re-solves a position, ever. This part of entry 70 was right.
2. **A bull is pinned only when a person actually chose that bull.** Clicking a hole and then clicking a bull is a chosen assignment. Adding a mark and accepting whatever it snapped to is not.
3. **Everything else takes part in the matching**, including hand-placed shots whose bull nobody chose. They compete for bulls like any detection.

**So `ShotProvenance` is not sufficient to decide pinning and entry 70 was wrong to use it.** What is needed is a separate fact on the shot: **did a person choose this bull?** One boolean, and deliberately not the same thing as how the mark got there. A `Manual` shot may or may not have a chosen bull. A `Corrected` shot has one by definition.

**What this fixes in Claude Code's scenario.** Adding a missed hole beside a detection no longer displaces that detection by fiat. Both are free, the matching weighs them together, and whatever it returns is the globally optimal answer for the shots now on the sheet. **If a detection still moves, that is a real result rather than an artefact**, and it belongs in the review queue as a `Moved` item exactly as implemented.

**It also fixes the counts test.** Under the old rule a hand-placed shot consumed a bull the moment it was placed, which could trip the more-shots-than-bulls fallback earlier than the sheet warranted. Applying the count to the whole free set is the behaviour `DESIGN.md` section 13 describes.

**Everything else in entry 70 section 3 stands**: re-solve on every edit, surface moved shots, honour the counts rule live and say when the method changes, and undo removes the pin along with the reassignment.

### 2. The assignment detail does not survive a save, and that should be said out loud

Claude Code's report notes, without making anything of it, that a saved marking does not keep the sheet's page mapping, so the assignment detail exists only while a detection is loaded.

**The consequence is worth stating plainly: save a marking, reopen it, and the contested card has nothing to say.** The margins, the alternatives and the reason are all gone, and the review queue with them.

**That is acceptable for now and it is not acceptable silently.** Somebody will hit it, and the failure looks like a bug rather than a limit. Two things follow:

- **The screen should say so** rather than showing an empty queue that looks like there is nothing to review. A marking reopened without its image is a different state from a marking with no outstanding questions.
- **Record it in `DESIGN.md` section 13 beside the rule**, so the decision to carry the mapping in the marking file later is made deliberately rather than forced by a bug report.

**I am not asking for the file format to change now.** The editor is not built yet and changing a format to support a screen that does not exist is the wrong order.

### 3. What else in that report was right and needs nothing

Noted so it is not re-examined: the three print status states with a test that proves red does not persist after a success; the declared and located bull positions now carried side by side with a comment saying why they differ, which closes entry 70 section 5; and `* text=auto` verified against both `core.autocrlf` settings before committing, which is entry 66's rule applied without being asked.

---

## 2026-09-16, entry 73: the first hour anybody has spent using GroupLab, and the headline numbers on screen are wrong

**Status: actioned 2026-09-16 for sections 1, 2, 3, 6 and 7, in section 9's order. Section 5 needs a decision, section 8 is answered from Alan's log, and section 4 waits for the label.**
- **Section 1:** matching now runs over separate scoring and sighter pools. On the scan, extreme spread went from 2.224 to 1.361 in, as you predicted, but **the ellipse aspect rose, 2.630 to 2.818**, its axis turning from 62.5 to 26.7 degrees: the elongation is in the ten scoring shots, not the two sighter holes.
- **Section 2:** the warning names every explanation instead of declaring ink.
- **Section 3:** not supported. Flagged marks sit 0.017 in from the hole's own centre, like the unflagged ones, and only 1 of 5 points toward the ink.
- **Section 5:** rings already scale with zoom once a calibre is set; the fixed ring is the no-calibre state. Choose what to draw then: the detector's measured diameter for detected shots, or a centre mark that claims no size.
- **Section 6:** rows fit the column, and Not a shot is on each row. Delete and Not a shot were already in the selection panel.
- **Section 7:** the reference figures are behind a remembered expander. The two interval labels are left at their exact coverages, as entry 24 decided.
- **Section 8:** the application identified the sheet from its codes in both of Alan's sessions and never asked for a definition; he opened a saved marking after each image. Whether detection should run on open, and the printed name's wording and placement, are yours.

Reported in `docs/PHASE1-RESULTS.md` "Entry 74 and entry 73 section 1" and "Entry 73 sections 2 to 8".

Alan ran the application against the friend's 600 DPI scan and reported eight things. **One of them makes the statistics wrong**, and it is first because everything else is cosmetic beside it. The others are ordered after it by severity.

Registration was excellent: **38 of 38 markers, RMS 0.0036 in**. Nothing below is about registration.

### 1. Sighter holes are being assigned to scoring bulls, and the group statistics are inflated by it

**The screen shows dashed lines running from two sighter-row holes up to bulls 22 and 23.** They are labelled 11 and 12. The sheet's sighter row sits below row 5, and two holes fired at sighters have been matched to scoring bulls a full row away.

**The model knows about sighters and the assignment does not use it.** `Bull` carries a `Scoring` flag, `BullAim` carries it through, and `GroupAnalysis` builds a `sighterBulls` set and excludes shots assigned to them. **`ShotAssignment` never mentions `Scoring` at all**, so the one-to-one matching pools sighter bulls and scoring bulls into a single set and is free to give a sighter's hole to a scoring bull.

**Excluding by bull cannot save this**, because by the time the statistics run the shot is sitting on a scoring bull. The exclusion happens one layer too late.

**The consequence is visible in the numbers.** A hole fired at a sighter and attributed to bull 22 enters the composite group with an offset of roughly a full row spacing. That is why extreme spread reads **2.224 in** while the scatter inside bulls 1 to 10 is plainly a fraction of that, and it is why the **error ellipse aspect is 2.63 with its major axis at 62 degrees**. The group is not elongated. It has two points a row and a half away from the rest.

**The fix is the physical constraint the sheet already encodes.** A shot fired at a sighter can never belong to a scoring bull, and the reverse. **Run the matching over two separate pools**, scoring and sighter, and apply section 13's counts rule to each pool independently.

**A prediction to check rather than believe:** removing shots 11 and 12 should take extreme spread well below 2.224 in and bring the ellipse aspect from 2.63 down toward 1. **If it does not, my reading of those dashed lines is wrong and I want to know.**

**So the answer to Alan's question is: yes, in effect.** The sighter bulls themselves are excluded correctly. Two sighter holes got onto scoring bulls and are being counted.

### 2. The oversize warnings state a cause they cannot know

Setting the calibre produces five warnings of this form:

> Shot 2 reads 0.607 in across, larger than one 0.308 in bullet hole (0.441 in), and it sits on the printed target: **this is printed ink under the mark rather than a hole.**

**The measurement is fine and the conclusion is asserted.** The application cannot know it is ink. Several of the flagged marks are plainly real holes that happen to sit on a printed ring.

`DESIGN.md` section 2 says this software must never give a confident wrong answer. **A sentence that names one cause out of several, in red, is that.** The honest form states the measurement and lists what would explain it: ink under the mark, two holes read as one, or a hole on a printed line merging with it. Shot 9's message already does this correctly and the others do not, so the right wording exists in the same file.

### 3. A hypothesis for the off-centre markers, with the test attached

Alan reports marker centres not aligning with hole centres when zoomed in. **Two different things are mixed together here and they need separating before anything is changed.**

The first is certain and is section 5 below: the marks do not scale, so at high zoom a fixed-size glyph sits over a large hole and cannot be judged by eye at all.

The second is a hypothesis. **Every mark flagged as oversize in section 2 sits on or touching a printed ring line**, and reads 0.5 to 0.6 in across against a 0.441 in bullet hole. That is consistent with the detector merging the hole with the ink it touches, which would both inflate the measured diameter and **pull the reported centre toward the ink**.

**Test it rather than assume it.** For each flagged shot, compute the centre of the bright aperture alone, excluding anything as dark as printed ink, and report the displacement from the reported centre and its direction. **If the displacement points consistently at the nearest ring line, the mechanism is confirmed and the fix is in the detector.** If it does not, the marks are correct and only section 5 is real.

### 4. At least one detection has no hole under it

Alan reports a mark pointing at a spot with no hole, and there is a circle below the caption at the bottom of the sheet with nothing visible under it.

**This needs the label number from Alan before anybody hunts for it**, and it is worth having because the corpus has no recorded false positive on real paper at 600 DPI.

### 5. The marks do not scale with zoom

Confirmed from the screenshots: the mark glyphs are the same size on screen at fit and at high zoom.

**This is worse than cosmetic. It is what stops a person checking the detector's work**, which is the entire purpose of the editor. A mark whose size means nothing cannot be compared against the hole it claims to be on.

**Marks should be drawn in sheet units and scale with the image**, so that at high zoom the mark and the hole can be compared directly. The calibre ring already has a real diameter to draw.

### 6. A shot cannot be deleted from the list, and the button is clipped

The right column's shot list offers Exclude, **its text is cut off by the panel width**, and there is no delete.

**Exclude and delete are different things and both are needed.** Excluding keeps a real shot out of the statistics, which is `ExclusionReason`'s job. Deleting removes something that is not a shot at all, which is what a false positive needs. `MarkedShot.NotAShot` exists for exactly this and is not reachable from the list.

**This blocks the Phase 3 gate directly**: twenty-five shots with several misassignments corrected in under two minutes is not possible when a wrong mark cannot be removed.

### 7. The statistics panel is a wall of prose, and section 19 already says what to do

Alan calls it hard to read and confusing. `DESIGN.md` section 19 specifies the answer and the application is not following it:

> The primary panel shows the composite group, the headline figures, and the confidence interval on each. **Reference material, the full CEP table, the bivariate fit, and the comparison machinery live one click away in a panel that remembers it was opened.**

Currently everything is in the primary panel: the aim offset, mean radius, sigma, extreme spread, edge to edge, the 0.77 to 1.42 sample-size caveat, the error ellipse aspect and axis, and the worst-shot flyer test, as continuous prose.

**Headline figures with their intervals stay. The rest goes behind the disclosure.** This is not layout work being pulled forward; it is a specification the screen is currently ignoring.

One detail worth fixing while there: the intervals read **94.8%** and **95.0%**. Two different coverages side by side invites a question nobody meant to raise.

### 8. The sheet does not carry its own name, and opening a definition should not be manual

The caption prints `GL-20J3-Y141-0BN3-EYME` and no human name, so a person holding the sheet cannot tell which target it is.

**Two separate asks, and Alan is right on both:**

- **The application should identify the sheet itself.** The corner blocks carry GLTD-B, registration found 38 of 38 markers, and the definition identifier is in the payload. Opening an image of a GroupLab sheet should load its definition without anybody choosing a file.
- **The printed caption should carry the human name as well as the identifier**, because the identifier is for the software and the name is for the person holding the paper. This changes the renderer and therefore every sheet printed afterwards, so it is a deliberate change rather than a quick one.

### 9. Order

1. **Section 1.** The numbers are wrong until it is fixed.
2. **Section 2.** One wording change, and it is the difference between honest and not.
3. **Sections 5 and 6.** Together they are what makes the editor usable enough to judge anything else.
4. **Section 3's test**, which may or may not turn into detector work.
5. **Section 7**, following section 19 rather than inventing a layout.
6. **Section 8**, the identification flow first and the printed name second.
7. Section 4 once Alan supplies the label number.

---

## 2026-09-16, entry 72: the shooting details for 3a493942, and a warning about what its sigma will mean

**Status: actioned 2026-09-16 for sections 1 to 3. Section 4 is a question for Alan.**
- **Section 2 is in `docs/STATISTICS.md`** where the composite group is defined: a sigma from a one-shot-per-bull sheet is the sheet's dispersion, with re-aiming in it, and is never reported as the rifle's.
- **Section 3, against the prediction:** 0 of 10 scoring shots misassigned, where you predicted 2 to 3. The ratio moved: after entry 73's fix the sigma is 0.274 in, putting the spacing at 5.5 sigma, where your model expects about 0.15 of a shot in ten. The sigma comes from the same shots, so the agreement is weaker than it looks.
- **Section 1's details** are not written into the submission's provenance: they came from Alan, not from the shooter, which is entry 57 section 5's distinction.

Reported in `docs/PHASE1-RESULTS.md` "Entries 71 and 72".

Short. It fills the blank answers entry 71 section 6 item 1 asked for, and it carries one caveat that has to reach the analysis before anybody quotes a number from this sheet.

Alan has the details from the shooter: **300 Blackout, 220 grain subsonic, at 25 yards.**

### 1. Use these in place of the blank fields

| Field | Value |
|---|---|
| `caliber` | **300 Blackout**, bullet diameter **0.308 in** |
| Load | 220 grain subsonic |
| `shot_distance` | **25 yd** |
| `target_backing` | cardboard silhouette on a wooden stake, read from the photographs |
| `attachment_method` | packing tape at the four corners, over the chamfered corner tips only |

0.308 is the number the hole size check wants. It is the same bullet diameter as the friend's earlier target, so the calibre gate has a second real sample rather than a first.

### 2. A one-shot-per-bull sheet does not measure the rifle, and at 25 yards that gap is widest

**This matters more than the numbers themselves.** A load development sheet gives one shot per bull, so what the composite group contains is **dispersion plus the shooter's ability to re-aim on each of ten different bulls**. Those two are not separable from one sheet, and nothing in the statistics can separate them.

At 25 yards the aiming term is proportionally at its worst: the target subtends a large angle, the bulls are small on the retina at that distance only in absolute terms, and any optic is almost certainly zeroed for somewhere else. **A subsonic 220 grain load at 25 yards with a sight zeroed at 50 or 100 yards will not print where it is aimed**, and a per-bull aiming correction by the shooter puts a different error on every bull.

**So whatever sigma comes out of this sheet is not the rifle's sigma**, and it must not be reported as though it were. The honest form is that it is the sheet's dispersion, with the sources named. **`DESIGN.md` section 2 says this software must never give a confident wrong answer, and quoting a rifle's precision from a sheet like this would be exactly that.**

**This is a general property of the load development sheet rather than a fault of this target**, and it should be written into `docs/STATISTICS.md` wherever the composite group is described, because every load development sheet GroupLab prints has it.

### 3. A prediction, so it can be killed

The shooter used **bulls 1 to 10 consecutively rather than every other bull**, so the suggestion to double the effective spacing was not taken.

His previous sheet sat at a spacing-to-sigma ratio of 3.89, entry 56 predicted roughly 21 percent misassignment at that ratio, and 2 of 10 were observed. **Same rifle, same load, same distance, same sheet geometry.**

**So I expect roughly 2 to 3 of the fourteen-odd shots on this sheet to be nearest to a bull other than their own.** If `analyze` comes back with none, either the ratio has moved or entry 56's model is wrong, and both are worth knowing. If it comes back with far more, the same. **State the observed count against this prediction when the run is reported**, rather than just reporting the count.

### 4. One question worth one message

**Was the friend's earlier target also subsonic?** If it was supersonic, the two targets give the hole size check the same bullet diameter at two very different velocities, which is a contrast the corpus has nothing else like. If it was subsonic too, this is a repeat, which is still useful and worth knowing as a repeat rather than mistaken for a contrast.

---

## 2026-09-16, entry 71: submission 3a493942 is the mounted gate's first real material, and it is good

**Status: actioned 2026-09-16 for order items 1, 2 and 3. Items 4 and 5 are not ones this side can do.**
- **Item 1:** intake refuses the submission as received, because the scan is not in `meta.json`. Run on a copy without the scan, it publishes `IMG_5820` and `IMG_5822` scrubbed and holds the other four on triage, which decoded 0 to 3 markers on them. **Triage is wrong about all four**: every frame registers with 33 to 38 markers, so the gate's fixed marker-size guesses are a defect, recorded and not fixed. Nothing reached `grouplab-testdata`, and the scan's consent is still your decision.
- **Item 2:** all six register. The flat homography's worst scoring bull is 0.011 to 0.059 in; the surface model's 0.0053 to 0.046 in, with `IMG_5820` at 0.00531 and `IMG_5819` at 0.00587, the closest any mounted frame has come and still outside 0.005 in. The large errors are on clean bulls at the bottom corners, not on the holed ones.
- **Item 3:** the scan's 10 scoring shots are all matched to their nearest bull, and 5 sighter-row holes share 3 sighter bulls. Reported against entry 72 below.
- **Section 3's tape instruction** is noted for the upload page, with section 6's upload limit.

Reported in `docs/PHASE1-RESULTS.md` "Entries 71 and 72".

This is the thing the critical path has been waiting on since Phase 1 opened. Six camera originals of a GroupLab sheet **still taped where it was shot**, plus a 600 DPI scan of the same sheet, consented and not opted out.

Sheet **`GL-20J3-Y141-0BN3-EYME`**, 5 by 5 with a three-bull sighter row. Submitted 2026-09-16T19:05:24Z.

### 1. What arrived

| | |
|---|---|
| Photographs | 6, `IMG_5819` to `IMG_5824`, taken 15:01:24 to 15:01:49 |
| Camera | iPhone 17 Pro, back triple camera, **main lens on all six** (6.765 mm, f/1.78, 35 mm equivalent 24) |
| Pixels | **5712 x 4284, 24.5 MP each** |
| EXIF | **68 tags**, `LensModel` present, `DigitalZoomRatio` absent as always |
| Exposure | ISO 80, 1/361 to 1/563. Bright daylight, no flash |
| GPS | **15 tags each.** Values not read. To be scrubbed |
| Scan | `Scan_20260916.png`, 5100 x 7013 at **600 DPI**, 54.7 MB |
| Consent | agreed, `consent_v1`, `exclude_from_public_dataset` false, no `DO-NOT-PUBLISH` |

**All six are camera originals chosen from the camera roll.** Nobody used the page's own capture button, so entry 58's stripped-file case does not arise. **These are the first consented, publishable, mounted photographs in the corpus.**

### 2. It is mounted the way the gate means

Taped at the four corners to a **bowed cardboard silhouette target on a wooden stake**, outdoors on a berm, in dappled daylight. The cardboard's bow is plainly visible in `IMG_5822` and `IMG_5824`.

**That is the case `DESIGN.md` section 21 [r6] says no frame has ever come inside 0.005 in on**, and every measurement behind that statement came from Alan's own seven frames of a sheet hanging from a single pin. **This is a second, independent mounting, by a different person, with a different fixing method, on a different backing.** Whatever the numbers say, they say something the corpus could not say this morning.

### 3. The tape is placed perfectly, and that was luck

The four tape squares sit diagonally across the **chamfered paper corner tips only**. Every one of the four corner data blocks is fully clear, and so is every AprilTag I can see. At full resolution the blocks and tags are crisp.

**My instructions to the shooter never mentioned this**, and tape across a corner block would have cost the entire session with nothing to show for it. **Add it to the instruction**: tape or staple outside the printed area, and never across a corner block or one of the small square markers.

### 4. Frame quality is not uniform, and three of the six are the useful ones

| Frame | Angle | Sheet in frame |
|---|---|---|
| `IMG_5820` | near straight on | fills the frame, roughly **400 px per inch of paper** |
| `IMG_5822`, `IMG_5824` | off-axis, useful | good fill |
| `IMG_5819`, `IMG_5821`, `IMG_5823` | wider angles | **sheet small in frame**, so far fewer pixels on each marker |

Worth measuring rather than assuming: **the three wide frames may fail on marker size** the way the synthetic images in entry 48 did. If they do, that is a finding about how far back a contributor can stand, which is guidance we currently do not have and cannot get any other way.

### 5. The shots are exactly the material the editor work needs

Impacts fall on **bulls 1 to 10 and the three sighters**, roughly fourteen to sixteen holes, with bulls 11 to 25 clean.

**Several sit outside their bull's rings.** One is above bull 5 and clear of it, one sits on bull 8's upper ring, and one near bull 4 lies between 4 and 5. **So this sheet carries genuine assignment ambiguity on real paper**, which is the material entry 70's contested-assignment work has to be built against. It is a better fixture than anything synthetic, and better than entry 56's sheet because it comes with a mounted photograph as well as a scan.

### 6. Four problems, none fatal

1. **Every substantive answer is blank.** `target_backing`, `attachment_method`, `shot_distance`, `caliber` and `credit_name` are all empty strings. The backing and the attachment are readable from the photographs. **The distance and the calibre are not recoverable from anything** and both matter: distance sets the scale of everything reported, and calibre drives the hole size check. **Ask the shooter.**
2. **The scan is not in `meta.json`.** It was placed in the folder by hand, so it has no manifest entry and no recorded hash, and the intake path will not treat it as part of this submission. Decide deliberately whether it is covered by the same consent record, rather than assuming.
3. **The page refused the scan for size.** A 600 DPI scan is **the single most useful artefact a contributor can give us**, and the upload page rejects it. That limit should rise, or the page should offer another route.
4. **`notes` reads "Safari"**, carried over from the browser test. Harmless, and worth knowing before anybody reads it as a comment about the target.

### 7. Order

1. **`grouplab intake` on the submission**, which scrubs the GPS. First consented mounted frames through the publication path.
2. **Register all six frames and report worst scoring-bull error per frame**, flat homography and the developable surface both, against the `PHASE1-RESULTS.md` tables. **This is the mounted gate measured on somebody else's mounting**, and it is the first independent test of the Phase 1 result.
3. **`analyze` the scan**, and report detections, assignment method, and every ambiguous shot with its margin. Keep it as the fixture for entry 70's contested card.
4. **Raise the upload size limit** so the next scan does not need a person in the loop.
5. Ask Alan for the distance and the calibre.

---

## 2026-09-16, entry 70: four decisions, so the editor is not blocked on me

**Status: actioned 2026-09-16 for sections 1, 3, 4, 5 and 6, in section 7's order. Layout is not started.**
- **Section 1:** `* text=auto` is the first line of `.gitattributes`, and the renormalisation staged only that file, with and without `core.autocrlf`. The duplicate image is deleted.
- **Section 6:** the print status line carries success, information or alert, each styled for what it is.
- **Section 4:** each detection reaches the marking with its whole assignment, the method and reason travel with them, and the refused candidates come through. The card's sentence is left to be composed where it is shown.
- **Section 3:** your five items, as written. Pinned means manual or corrected; the untouched detections re-solve on every edit against the bulls nobody holds; a moved shot is listed for as long as it stays moved, derived from the bull detection gave it rather than kept as a notice; the counts rule switches method live and says so; undo takes the pin back with the reassignment.
- **One consequence of item 1 to confirm:** a hand-placed shot is pinned to its nearest bull, so adding a missed hole beside a detection pushes the detection elsewhere, shown as moved. If a hand-placed hole should instead be matched like a detection, that is a change to the rule.
- **Section 5:** the split is kept and written down in both places it is used, and in `DESIGN.md` section 13.
- **Section 2:** noted. The index-first rule is taken on this side too.

Reported in `docs/PHASE1-RESULTS.md` "Entry 70".

Each section is a decision rather than a finding. Section 3 is the one Claude Code asked for and the one that blocks layout work.

### 1. Adopt `* text=auto`, and delete the image I wrongly added

**`* text=auto` goes in.** The measurement settles it: on a git without `core.autocrlf` the repository is 109 files and 21,634 lines away from clean, and adding the line stages exactly one file, `.gitattributes` itself. **One line, zero content churn, and the 109-file state stops existing for everybody rather than for one machine.** Entry 67 section 3 predicted an empty renormalisation and that is what came back.

**Delete `docs/figures/concept-assignment-editor.png`.** I added it on the strength of a claim that was false.

### 2. What I got wrong, because the failure is worth more than the file

I searched the working tree for `*concept*`, found nothing, and reported that **the design target existed only in chat** and that every instruction to match the concept had been unactionable. Both statements were confident and both were wrong. **The concept has been committed all along at `docs/figures/screens/assignment-editor.png`, and the README carries a table linking it.**

Entry 66 section 3, written earlier the same day, says: **when I state that something is absent, say how I looked.** I did not apply it one entry later. "No file matches `*concept*`" is a fact about a glob. "The design target is not in the repository" is a claim about the repository, and the index that would have answered it is the README, which I never opened.

**The rule needs a second half: search the index before searching the filesystem.** A repository with a README table of its own figures has already answered the question, and a filename pattern is a guess about what somebody chose to call something.

### 3. The decision: matching reruns, and a person's decision is a constraint rather than an input

Claude Code asked whether matching reruns as the person edits. **Neither plain yes nor plain no is right, and the failure mode of each is instructive.**

**Never rerunning** means the assignment goes stale the moment a shot is added, moved or deleted, and a person who adds a missed hole gets an answer computed for a different set of shots.

**Always rerunning** is worse, and specifically worse in one-to-one matching. Pushing a shot onto bull 9 pushes whatever held bull 9 somewhere else. **A person resolving one contested case would silently cause a second one**, and the software would have reversed a decision the person just made. That is the behaviour `DESIGN.md` section 2 says this application must never have.

**The rule:**

1. **Every bull a person sets is pinned.** `ShotProvenance` already records this: `Corrected` and `Manual` are pinned, `Automatic` is free.
2. **Matching re-solves on every edit, over the unpinned shots only**, against the bulls the pinned shots have not taken.
3. **When the re-solve moves an automatic shot, it goes in the review queue as its own item**, naming what moved and why. The cascade is the part that must never be invisible.
4. **The counts rule of section 13 is honoured live.** If an edit takes the detections above the number of bulls, matching stops being forced, the method falls back to nearest bull within the gate, every shot is flagged, and **the screen says the method changed**. A method that changes without saying so is a confident wrong answer wearing the previous answer's clothes.
5. **Undo restores the pins, not just the positions.** Undoing a reassignment has to unpin it, or the matching stays constrained by a decision that no longer exists.

**This costs no new machinery.** Provenance exists, undo exists, the matching exists, and the queue is being built anyway.

### 4. The plumbing, named precisely

`AutomaticResult.Detections` is `IReadOnlyList<(PointD Image, int? Bull)>`. **`AutomaticMarking` computes the margin, the nearest bull and the ambiguity flag, and then throws all three away at that tuple.** Everything the contested card and the "2 of 26 need review" counter need is calculated and discarded one line before it could be used.

**Carry it instead.** The tuple becomes the `AssignedShot` record that already exists, the `ShotAssignmentResult` travels with it for its method and reason, and the rejected candidates come through rather than reaching only the trace, because the concept's queue shows two of them: a candidate below the size gate, and a marker caption rejected on stroke width.

**Two things the card needs that are not there yet**, both small and both better named now than discovered later:

- **The card's prose is composed, not stored.** `ShotAssignmentResult.Reason` is a one-line note about the method. The sentence in the concept is built from one shot's own numbers, and it should be built where it is shown.
- **Nothing rings a contested assignment.** `FlaggedShots` rings oversized holes from the calibre check. The concept draws a contested detection as an amber ring with a dashed line to each candidate bull, which is a second mark type rather than a reuse of the first.

### 5. Declared versus located bull positions: keep the split, and write it down

Claude Code reports that assignment uses declared bull positions. `AutomaticMarking` builds `BullAim` from `b.Recovered ?? b.Declared`. **So classification uses the declared geometry and the aim point uses the located one, and I think that is correct rather than an oversight.**

- **Assignment is a classification**, and the declared positions are the definition's exact geometry. Registration error is of order 0.005 in and printing error 0.003 in, against a 0.15 in ambiguity margin, so the choice cannot flip an assignment that was not already flagged.
- **The offset is a measurement**, and the shooter aimed at the printed bull rather than the declared one, so it must use the recovered position. `BullAim` already does.

**Say so in a comment where each is used.** An undocumented split between two nearly identical quantities is exactly the thing somebody unifies in six months for tidiness.

### 6. The red success message is a defect, not layout

The print window styles its status line as an alert for every message, so a successful send shows in red. **That is not a styling preference to defer**, it is the mechanism by which people learn to ignore red, and the next red message that matters is the one about a sheet printing three percent small.

**Fix it with the wording change rather than with the layout**: success, information and alert are three states and the line should carry which one it is.

### 7. Order

1. `* text=auto`, and delete the duplicate image. Both one-liners.
2. Section 6, the status line states. Small, and it stops a habit forming.
3. Section 4's plumbing. **Nothing about the editor can be drawn until the detail reaches the session**, so this is the real first step of the editor.
4. Section 3's rule, implemented against that plumbing.
5. Layout last, and still after Alan has used the application.

---

## 2026-09-16, entry 69: the assignment editor is the target, the concept is section 13 drawn, and most of it already exists in Core

**Status: actioned 2026-09-16 for order items 1 and 2. Items 3 and 4 wait for Alan's report on using the application.**
- **Item 1: the design target was already committed,** at `docs/figures/screens/assignment-editor.png` and linked from the README. Your file is the same picture pixel for pixel, re-encoded at 2.5 times the size with a content-credential block, so it is left uncommitted rather than added as a second copy.
- **Item 2: the table is six rows right and two wrong.** `Reason` is a one-line method explanation, not the card's prose, and `FlaggedShots` rings oversized holes, not contested assignments.
- **The larger finding:** the per-shot figures the card needs are computed in `AutomaticMarking` and dropped before they reach the session, and matching is never rerun after an edit. So the job is presentation plus that plumbing and one decision, and both come before layout. Reported in `docs/PHASE1-RESULTS.md` "Entries 65 to 69".

Alan has named the assignment editor as what the UI work builds towards, on the grounds that it is the beginning of the process. **`DESIGN.md` section 13 opens by saying the same thing in stronger terms**, so this is the document's own sequencing reasserting itself rather than a new preference:

> The editor is built before the detector, not after it. A good editor with a mediocre detector is a usable product. A bad editor with a good detector still frustrates users on every target the detector gets wrong, and no detector reaches one hundred percent.

### 1. The concept image is now in the repository, because it was not anywhere Claude Code could see it

I searched the working tree for concept artwork and found none: no file matching `*concept*` outside `.git`. **The design target existed only in chat**, which means every instruction to "make it look like the concept" has been unactionable by anyone who cannot see the picture.

I have placed it at **`docs/figures/concept-assignment-editor.png`**. It is untracked; commit it or move it, but do not leave the target outside the repository again.

### 2. It is not a mood board. It is section 13 rendered, down to the measured numbers

The contested-assignment card in the concept reads:

> This hole is 0.627 in from bull 4 and 1.043 in from bull 9. Nearest bull says 4, but bull 4 already holds a shot at 0.289 in and bull 9 holds none. One-to-one matching gives it to bull 9.

`DESIGN.md` section 13 [r3] says:

> a hole 0.627 inches from bull 4 belongs to bull 9 at 1.043 inches because bull 4 already has a closer shot

**Same case, same figures, from `338lmao.jpg` in the corpus.** The hint text "Rough clicks snap to the local centroid" is also section 13's requirement quoted verbatim, and the three provenance pills are section 13's "automatic, automatic then corrected, or manual".

**So the concept can be built from the document**, and where the two disagree the document is not automatically right: the picture is newer and Alan drew it. Say which one you followed when they differ.

### 3. The encouraging part: the model already has nearly all of this

I read the source rather than assuming. What is already there:

| Concept element | Where it already lives |
|---|---|
| One-to-one matching, and why it beats nearest bull | `Core/Detection/ShotAssignment.cs`, `AssignmentMethod.OneToOne` |
| The numbers in the contested card | `AssignedShot`: `Bull`, `Distance`, `NearestBull`, `NearestDistance`, `Margin`, `Ambiguous` |
| The card's explanatory prose | `ShotAssignmentResult.Reason`, which already exists to carry exactly this |
| The `automatic` / `corrected` / `manual` pills | `ShotProvenance` in `Core/Marking/MarkingSession.cs` |
| "Not a shot" | `MarkedShot.NotAShot` |
| Click a hole, then click a bull | `MarkingCanvas.cs`, already citing section 13 in a comment |
| Flagged shots with an alert ring | `MarkingCanvas.FlaggedShots` |
| Counting by provenance | `Core/Marking/GroupAnalysis.cs` |

**The gap between the application and the concept is mostly presentation, not capability.** That is a much smaller job than the picture suggests, and it is the single most useful thing I learned today.

### 4. What I did not find, and therefore what is actually new

Stated as "did not find" rather than "does not exist", per entry 66:

- **An ordered review queue** with per-item status. `Ambiguous` exists per shot; a queue with `NOW`, `NEXT`, `ADDED` and `KEPT OUT`, ordered by what to look at next, I did not find.
- **An edit session with a commit point.** The concept's header carries "Discard edits" and "Accept and analyse", and a "2 of 26 need review" counter. That implies the editor is a mode you enter and leave, and analysis happens on acceptance.
- **A `Score` readout** beside position, diameter and margin.
- **The chrome**: the icon tool strip, the keycap hints, the left mode rail, the breadcrumb.
- **The paper-cream sheet on dark chrome**, which is the largest visual difference and is a theme question rather than a layout one.

### 5. The gate already exists and should not be reinvented

`DESIGN.md` section 21, Phase 3: **"a full 25-shot target with several misassignments corrected in under two minutes."**

That is a better gate than anything I would write, because it measures the thing section 13 says the editor is for. **Every styling decision should be argued against it**: if a change does not make that two minutes easier, it is taste rather than design, which is fine as long as it is labelled.

**The left rail's other four icons imply screens that do not exist**, and drawing them is how a UI pass turns into a rewrite. Build the assignment editor. Leave the rail as a rail.

### 6. What I am deliberately not specifying yet

**I am not writing a layout specification from one screenshot.** I have been wrong five times today by supplying a cause for a difference I had not looked into, and a screen is a worse place to do that than a toolbar separator.

What I want first is the thing Alan has not been able to do, which is **use the application**. His own words: he has not spent much time in it because Claude Code has been working and he did not want to cause conflicts. **That blocker is solvable and it is the highest-value thing in this entry**, because an hour of him using it will produce better direction than anything I can infer from a picture.

### 7. Order

1. **Commit the concept image**, or tell planning where it should live instead.
2. **Read section 3's table and confirm or correct it.** If any of those already exist in a form I missed, the job is smaller again; if any is less complete than it looks, I want that before anything is designed around it.
3. **Then the editor**, argued against section 21's two-minute gate.
4. The chrome last, because it is the part most likely to be redrawn once somebody has actually used the screen.

---

## 2026-09-16, entry 68: Windows first, then Android, and the two things that must not be shelved with Linux

**Status: taken 2026-09-16.** Entry 65 section 5, entry 67 section 4 and entry 65 section 4 step 3 were not touched. Both workflows still run all three platforms, and the macOS gate record stays open as a measurement question beside entry 55 section 5.

A direction change from Alan, recorded so it is not re-argued, plus two carve-outs that matter more than the change itself.

Alan's order, in his words: get the Windows application dialled and the UI fixed before testing on other platforms, and **Android has priority over Linux**.

### 1. What this shelves

- **Entry 65 section 5's remaining items.** The crash package on Linux, "Show me the file", and the gate record run locally. Parked, not cancelled.
- **Entry 67 section 4 entirely.** Running the tarball, and the ICU question the SDK install may already have spoiled. The VM stays built and the questions keep.
- **Entry 65 section 4 step 3**, calling `lp`. It was never urgent and it is now clearly not.

**The VM was not wasted.** It answered all seven of entry 61 section 2's questions in one sitting and found one defect. Leaving it there with nothing further asked of it is the correct end state for a tool that has done its job.

### 2. What must not be shelved with it: CI on all three platforms

**Shelving Linux testing means shelving the VM. It does not mean shelving the workflows**, and the difference is the whole argument of entry 60.

`ci.yml` and `gate-record.yml` run all three platforms on every push and cost nobody any attention while they stay green. **That is what stops Linux and macOS becoming a port later**, which entry 60's README text calls the expensive way to do it, and that text is now published and says so to anybody reading the repository. Turning the matrix down to Windows to move faster would be the single easiest way to make the published page untrue and to hand a future self a month of work.

**Nothing in Alan's message asks for that, and I am writing it down anyway**, because "we are shelving Linux" is the kind of sentence that quietly becomes a matrix change three weeks later.

### 3. What must not be shelved with it: the macOS gate record

macOS is currently the only red job, on a small number of measurement rows traced to corner refinement inside the native imaging library plus one further divergence below it.

**That is not a macOS problem. It is a question about the measurement**, and it would be exactly as interesting if it had shown up between two Windows machines. Three platforms running the same code on the same input produce two different answers, and we know where but not why.

**Shelving it as platform work would be a category error.** It belongs with entry 55's precision derivation, which is already queued and which comes to planning before anything prints differently, because both are asking the same question: how much of the last digit is real.

### 4. What Android priority actually buys, and what it does not

**The prerequisite for mobile needs no device and is already queued**: entry 54 section 11's first task, whether the capture assistant's thresholds separate pass from fail on the Phase 0 frames. Entry 62 said plainly that if they do not, most of entry 54 falls. **So Android moving up the order changes nothing about what happens next**, which is a good sign that the order was roughly right.

**What it does not remove is the dependency.** Entry 54's phone case is a person standing at a range holding a camera, and entry 62 section 4 said a green result on the tablet says nothing about that. **The range session and a photograph of a sheet still mounted where it was shot remain the critical path**, and they are the critical path for Android more than for anything else.

**The Galaxy Tab S8 Ultra remains usable immediately** when Phase 6 starts, with no account and no signing, which is the one thing entry 62 established that this reordering makes relevant sooner.

### 5. What I still need before UI work can be specified

**"Dialled" and "fixed" are not yet specific enough for me to write an entry against**, and I have spent today being wrong five times by supplying a cause for a difference I had not looked into. I am not going to do it again with a screen.

Asked of Alan separately: which of the concept screenshots the current window is furthest from, and whether the complaint is layout, density, typography, the panel, or the marking canvas itself.

**Entry 43, the analysis screen, stays closed.** Entry 50's three conditions gate it and one of them is the gate record green on all three platforms, which macOS still fails. **A styling pass on the screens that already exist is not gated by that** and can proceed as soon as it is specified.

---

## 2026-09-16, entry 67: `lp` is there, the XDG log path works, and the VM may already have spoiled the one test the tarball needs

**Status: actioned 2026-09-16 for section 3. Section 4 is shelved by entry 68.**
- **`git add --renormalize .` moves nothing on this machine.** Measured again with `core.autocrlf` off: 109 files as committed, and only the `.gitattributes` line itself once `* text=auto` is added. Your prediction holds; the adopting commit would be one line.
- **Nothing was committed.** Adopting it is Alan's call, and the index was reset after each measurement. Reported in `docs/PHASE1-RESULTS.md` "Entries 65 to 69".

Sections 1 and 2 close open questions. Section 4 is a warning about the machine rather than about the code, and it is the reason this entry is not just two lines appended to entry 65.

Everything here was measured on Alan's Ubuntu 24.04 desktop VM, by him, at my request. Per entry 66, how I looked is part of each claim.

### 1. `lp` is present, so entry 65 section 4 applies in full

`which lp lpr lpstat` returns `/usr/bin/lp`, `/usr/bin/lpr` and `/usr/bin/lpstat` on a stock 24.04 desktop with nothing installed but the .NET SDK.

**So the sentence "This system has no print command GroupLab can call" is false on the commonest Linux desktop there is**, and it is false on the machine we are testing on. Entry 65 section 4's first step is answered and its steps 2 and 3 both stand:

- **The wording is wrong now** and should describe GroupLab rather than the person's computer, whatever is decided about `lp`.
- **`lp` is worth calling**, because GroupLab's premise is a sheet printed at exactly 100 percent and `lp` takes the scaling option directly. This is the one platform where GroupLab could guarantee actual size instead of asking for it.

Still not urgent. Still the only place the Linux path could be better than the Windows one.

### 2. The XDG log path works, and entry 61 section 2 is now fully answered

A Release run wrote `~/.local/state/grouplab/logs/grouplab-20260916-181326-3564.log`. `XDG_STATE_HOME` is unset on a stock 24.04, so this is `LogDirectory`'s fallback branch and it is correct.

**That was the last of the seven things entry 61 section 2 said the VM existed to answer.** All seven are answered and the only defect found among them is the print wording in section 1.

### 3. `* text=auto` should go in, and the renormalisation commit should be empty

Claude Code left this to Alan with the cost stated as "one renormalisation commit". **I think that cost is very likely zero, and if it is, the decision is easy.**

The committed blobs are already LF: `git show :<path>` returned zero CRLF lines on every file I sampled. `* text=auto` normalises on commit, and blobs that are already normalised have nothing to change, **so `git add --renormalize .` should report no changes at all.** That is a prediction from three sampled files and the rule, not a measurement of all 476, so check it rather than believe it: run `git add --renormalize .` and look at `git status` before committing anything.

**If it is empty, take it**, because the repository currently depends on one line in one person's global git config on one machine, and the first contributor who clones on Windows without `core.autocrlf` gets the state entry 64 wrongly described as already true. **If it is not empty, stop and report what moved**, because then something about the tree is not what either of us thinks.

### 4. The SDK install may have pulled in ICU, which is the thing the tarball test was for

Claude Code's report says nobody has run the tarball, and names the two questions it answers: whether it launches on a desktop Ubuntu, and whether ICU is present.

**Installing the .NET SDK on that VM may have already answered the second question by changing it.** The SDK package depends on ICU, so if `libicu` was not there before, it is there now, and a tarball that launches on this machine proves nothing about a machine that never had the SDK. **That is exactly the user we are trying to model**, since the whole point of a self-contained tarball is somebody with no .NET at all.

**Check before drawing any conclusion from a successful launch:**

```
grep -B2 -A6 dotnet /var/log/apt/history.log | head -60
apt list --installed 'libicu*'
```

If the history shows `libicu` among the packages the SDK pulled in, **this VM can no longer answer the ICU question** and a second machine or a reverted snapshot is needed for it. If ICU was already present on the stock desktop image, the VM is fine and the tarball can be tested here.

**Either way the first question is still worth answering here today**, because "does the tarball launch and draw a window" is not affected by any of this. Download the artifact from the `linux tarball` job, unpack it somewhere outside the repository, and run it. **Run it from a directory that is not a git checkout**, because `LogDirectory` walks up looking for a repository root and we have already been fooled once by the difference between its Debug and Release branches.

### 5. What this does not change

The range session and the mounted photograph gate are still the critical path, and nothing in this entry is on it. The VM has now found one wording defect and confirmed six things that were already right, which is roughly what a first look at a new platform should produce.

---

## 2026-09-16, entry 66: entry 64 was an artifact of where I was standing, and that invalidates a class of thing I measure

**Status: noted 2026-09-16, and measured.** The one sentence worth acting on was measured under entry 67 section 3: a git without `core.autocrlf` sees 109 files and 21,634 lines, exactly entry 64, so entry 64 described a real state from a machine where it is real. The rule in section 3 is taken on this side too: the report says how each absence was looked for.

One sentence of it is worth acting on. The rest is a limit on what my measurements of this repository mean.

Entry 64 reported 109 tracked files permanently modified, 21,634 lines of line-ending churn waiting for a `git add -A`, and a working tree that was neither normalized nor clean. **It was wrong, and Claude Code's correction names the reason: `core.autocrlf` is `true` in the global config, so staging normalizes and the churn cannot be committed.**

I reported it as unset because I ran `git config --get core.autocrlf` and got nothing back.

### 1. Why that command lied, and what else it takes with it

**The shell I reach this repository through is not Alan's machine.** The folder is mounted into a separate Linux VM, and that VM has its own `$HOME` and its own `~/.gitconfig`. Repository-local config is shared, because it lives in `.git/config` inside the folder. **Global config is not.** So a global setting on Alan's Windows machine reads as absent to me, and I reported an absence I had no way to see.

The same applies to `git status` and `git diff` themselves, which is the part that actually matters. **Those commands do not report the state of the repository. They report the state of the repository as evaluated by the git I am running, with the config that git can see.** Every line of entry 64's evidence is a true measurement taken from the wrong machine.

**The class this invalidates, so I do not walk into it again:**

- **`git status` and `git diff` cleanliness**, which depend on `core.autocrlf`, `core.eol` and `core.fileMode`. On Alan's machine the tree is clean. Through the bridge it shows 109 modified files. **Neither of us is misreading git; we are running two different gits.**
- **Line endings as git sees them.** Line endings on disk are real and I measured them correctly. What git does with them is not.
- **File modes and the executable bit**, for the same reason.
- **Path case sensitivity.** The mount is case sensitive and NTFS is not, so a casing bug could appear here that never appears there, or the reverse.
- **Anything reading an environment variable, a user profile, a home directory or an installed tool.** `which dotnet` through the bridge says nothing about Alan's machine.

**What stays reliable:** file contents, file sizes, timestamps, `git log`, `git show`, committed blobs, and anything else answered purely by bytes in the folder.

### 2. The one thing still worth doing

**Nothing about line endings needs fixing, and entry 64's section 4 should not be run.** Claude Code already established that and did not run it, which was right.

But entry 64's last paragraph asked a real question that survives its wrong premise. **The tree is clean because of a setting in one person's global git config.** Anyone who clones without `core.autocrlf` set, including CI, a second machine, or a contributor, gets the state entry 64 described for real. `* text=auto` in `.gitattributes` moves that guarantee from a machine into the repository, at the cost of one renormalization commit.

**That is Alan's call and not mine**, and it is worth making deliberately rather than discovering the first time somebody else clones.

### 3. The general form, because this is the third time

Entry 47 asserted an unverified fact about OpenCV packaging. Entry 57 asserted a browser. Entry 64 asserted a git config. **All three were confident statements about something I could not see from where I was standing, and all three were cheap to check and expensive to believe.**

The rule that would have caught every one: **when I state that something is absent, say how I looked.** "`core.autocrlf` is unset" would have become "`git config --get core.autocrlf` returned nothing, from a shell that may not be reading the same config", and the flaw would have been visible in the sentence as I wrote it.

---

## 2026-09-16, entry 65: GroupLab renders on Linux, five of my five screenshot hypotheses were wrong, and one real thing is left

**Status: actioned 2026-09-16 for section 4 step 2. Section 4 step 3 and section 5 are shelved by entry 68.**
- **Step 2:** off Windows the print window now says "GroupLab cannot send this to a printer itself here, so the PDF is open in your viewer." It names no platform, because the same branch runs on macOS.
- **Why it is red:** the status line carries the alert style for every message, including the successful ones. Noted and not changed, with the rest of the chrome. Reported in `docs/PHASE1-RESULTS.md` "Entries 65 to 69".

Section 4 is the only item asking for work. Sections 1 to 3 are the record of the first time anybody looked.

Alan built the 24.04 VM and ran the application. `XDG_SESSION_TYPE` is `wayland`, so everything below is Avalonia's X11 backend through XWayland. He sent screenshots of Ubuntu and of Windows at a comparable size, which is what made the rest of this entry possible.

### 1. It works, and more of it works than anybody had grounds to assume

Confirmed by looking, not by inference:

| | Result |
|---|---|
| Window, toolbar, panel, status bar | draw correctly, and the two-row toolbar wrap is identical on Windows |
| Embedded IBM Plex | renders, including the monospace scale block |
| Theme, set to Follow system | dark, and `gsettings get org.gnome.desktop.interface color-scheme` returns `prefer-dark`, so it followed correctly |
| File dialog | the GTK portal chooser, with GroupLab's own title, the Images filter applied, and the home directory reachable |
| Print window | target list, description, preview artwork, sheet paging, all correct |
| PDF handoff | the sheet opened in the system document viewer and looks right |
| Crash report dialog | opens, lists both run logs plus `environment.txt`, `description.txt` and `contact.txt` |
| Log file | written, one per run, correct name pattern |

**Entry 61 section 2 listed seven things the VM existed to answer. Six of them are answered and passed on the first attempt.**

### 2. Everything I flagged from the screenshots was already right

I raised five things. **All five were correct behaviour and four of them were already reasoned about in code comments I had not read.**

| What I flagged | What it actually is |
|---|---|
| Toolbar separators render as a horizontal dash | identical on Windows. Not a platform difference |
| The scale block is monospace while its surroundings are not | identical on Windows. Deliberate |
| Follow system may not detect the Linux theme | it detected it. `prefer-dark` |
| The log path ignores entry 41's XDG location | `LogDirectory.Resolve` puts logs under `<repository>/out/logs` **in a Debug build** by design, and the platform paths including `$XDG_STATE_HOME/grouplab/logs` are implemented below it. Alan ran Debug on Linux and Release on Windows, so the two panels differ by build configuration and not by platform |
| The crash report has no Send button on Linux | the Send button is hidden while the send URL is empty, which is entry 41 section 7's own design |

**Worth recording rather than quietly dropping.** The pattern is that I read a screenshot, found a difference, and proposed a cause, five times, without opening the code that explains it. Each one carried a stated check, which is why the whole set cost one message instead of a week, and that is the only part of this I would keep.

### 3. Entry 61 section 3 was wrong about the mechanism, and its caveat is what saved it

Entry 61 predicted that setting `Verb` on Unix throws `PlatformNotSupportedException`, that neither `catch (Win32Exception)` sees it, and that pressing Print offers the user a crash report.

**It throws `Win32Exception` with `ERROR_NO_ASSOCIATION`, so the original catch would have caught it and the fallback would have run.** The button was never going to crash. `PrintWindow.PrintLaunch` now carries that correction in its own documentation, and the fix taken is the one entry 61 asked for anyway: do not set `Verb` off Windows at all.

Entry 61 said "check it before changing anything, because if `Verb` is silently ignored on Unix rather than throwing, the code is merely useless there rather than broken, and the fix is different". **That sentence is the only reason a wrong mechanism did not become a wrong fix.**

### 4. The one live finding: the print message tells a Linux user something untrue, and `lp` is the opportunity

Pressing Print on Linux shows, in red:

> This system has no print command GroupLab can call, so the PDF is open in your viewer. Print from there at Actual size, or 100%.

**Read as a statement about GroupLab it is true. Read as a statement about the user's system it is false**, and it is the second reading a person will take. Any Ubuntu desktop with CUPS has `lp`, and the sentence tells its owner their machine cannot print.

**The wording is the small half. The large half is that `lp` exists and nothing calls it.**

GroupLab's whole premise is a sheet printed at exactly 100 percent, and the print window says in its own words that it "cannot set your printer driver". On Windows that is a real limit of the shell verb. **On Linux it is not a limit at all**: `lp` takes the scaling options directly, so this is the one platform where GroupLab could guarantee actual size rather than ask for it, and it currently does the weakest thing available.

So, in order:

1. **Establish whether `lp` is there.** `which lp lpr lpstat` in the VM. If it is absent on a stock Ubuntu desktop, the message is closer to true than I think and most of this section falls.
2. **Fix the wording either way**, so it describes GroupLab rather than the person's computer. Something like: *GroupLab cannot hand this to a printer on Linux, so the PDF is open in your viewer.*
3. **Then consider calling `lp` when it exists**, with the no-scaling option set explicitly, falling back to the viewer when it does not. Not urgent, since there are still zero Linux users, and worth writing down because it is the one place where the Linux path could be better than the Windows one rather than merely equal.

### 5. What the VM has not answered yet

- **Whether the XDG log path works in practice.** The code intends `~/.local/state/grouplab/logs`, and only the Debug path has been seen. A Release run would confirm it, and it is the last of entry 61 section 2's seven items still open.
- **Whether the crash package actually builds on Linux.** The dialog was opened and no report was saved, so the zip, the temp directory and "Show me the file" are all still untested on a platform whose temp handling differs.
- **The gate record locally**, which CI already covers and which therefore comes last.

---

## 2026-09-16, entry 64: 109 tracked files have shown as modified for two days, and the whole difference is line endings

**Status: actioned 2026-09-16, and the diagnosis is corrected.** Verified before anything was taken, as you asked: `git status --short` lists only the untracked inbox entries, and `git diff --shortstat`, `git diff -w --shortstat` and `git diff --cached --shortstat` are all empty. There was nothing to discard, so `git checkout -- .` was not run.
- **`core.autocrlf` is not unset. It is true,** from the global config, so staging converts CRLF to LF and the CRLF working copies you found are not modified against their LF blobs. `git add -A` cannot bake in the churn while that holds.
- **The working copies are CRLF exactly as you measured them,** 30 of 30, 10 of 10 and 594 of 594 on the three files you named.
- **The stale lock is deleted.** It was 0 bytes with no live `.git/index.lock` beside it.
- **`.gitattributes` is untouched,** and the `* text=auto` question is yours: it moves normalisation from one machine into the repository at the cost of one renormalisation commit, and it becomes urgent the day somebody clones without `core.autocrlf`. Reported in `docs/PHASE1-RESULTS.md` "Entry 64".

Found incidentally while checking which inbox entries were still outstanding. It is not breaking anything today and it is a landmine, so it is worth half an hour now rather than a bad commit later.

### 1. What the working tree looks like

`git status` reports **109 modified tracked files**, and `git diff --shortstat` reports **21,634 insertions and 21,634 deletions**, exactly equal.

**`git diff -w --shortstat` reports nothing at all.** Every one of those 21,634 line pairs differs only in whitespace, and specifically in line endings: the working copies are CRLF and the committed blobs are LF.

| | Working tree | Index and HEAD |
|---|---|---|
| `src/GroupLab.Core/Imaging/GrayImage.cs` | 30 CRLF of 30 lines | 0 CRLF |
| `.gitattributes` | 5 CRLF of 10 lines | 0 CRLF |
| `src/GroupLab.Core/Statistics/RangeStatistics.csv` | 594 CRLF of 594 lines | 0 CRLF |

`core.autocrlf` and `core.eol` are both unset, so git converts nothing on its own and a CRLF working file will differ from an LF blob permanently.

### 2. What caused it, on the evidence of the timestamps

**Every modified file was last written on 13 or 14 September. Every file that agrees was last written on 15 September or later.** Nothing is mixed.

So this is not a bulk rewrite that happened recently. It is the residue of the repository being normalized to LF around the 14 September history rewrite, after which **the files that have been edited since were rewritten as LF and now agree, and the files nobody has touched since still hold their original CRLF bytes.** The working tree was never renormalized to match.

`.gitattributes` is the clearest single piece of evidence: its first five lines are CRLF and its last five are LF, because part of it was rewritten and part was not.

### 3. Why it matters even though nothing is broken

1. **`git status` is unusable.** 112 lines of output of which three are real, which is how a genuine uncommitted change goes unnoticed.
2. **One `git add -A` bakes in 21,634 lines of line-ending churn** and destroys `git blame` across most of the repository. Every commit for two days has evidently staged named paths, which is the only reason this has not happened already.
3. **`RangeStatistics.csv` is not covered by any `text eol=lf` rule** in `.gitattributes`, and nor are the `.cs` files. The rules cover `targets/**`, `tests/**/Fixtures/**`, `src/**/*.json` and `scans/phase0/measurements/**`. So the byte-for-byte comparisons are protected and the rest of the tree is not.

### 4. The fix, and I have deliberately not run it

Because the whole difference is whitespace, verified above, the committed content is authoritative and nothing is lost by taking it:

```
git checkout -- .
```

**Verify before and after rather than trusting the paragraph above.** `git diff -w --shortstat` should be empty before, which is the claim that no real change is being discarded, and `git status --short` should show only the untracked inbox entries after.

**Then decide whether to prevent the recurrence**, which is a policy choice rather than a cleanup and belongs to whoever owns `.gitattributes`. Adding `* text=auto` normalizes on commit and makes the working tree's endings a local matter, at the cost of one renormalization commit that touches everything once. Doing nothing is also defensible now that the tree is consistent. **What is not defensible is leaving it as it stands**, where the state is neither normalized nor clean.

### 5. One thing I left behind, which I cannot remove myself

A read-only `git status` of mine created `.git/index.lock` and could not remove it, because the bridge I use cannot delete files. A stale lock blocks every subsequent git command, so I renamed it out of the way to **`.git/index.lock.stale-claude-20260916`** and confirmed git works again.

**Delete that file.** It is inside `.git` so it never appears in `git status`, and it has no purpose.

---

## 2026-09-16, entry 63: the Linux VM is 24.04, not 26.04, and the reason has a packaging consequence

**Status: actioned 2026-09-16.** The tarball step exists now, and section 2's floor is written on it rather than in a document nobody reads while editing a workflow.
- **24.04 is confirmed from the runner itself,** which reports `Image: ubuntu-24.04`, version 20260907.300.1, so your reading of `ubuntu-latest` holds today.
- **The step prints the release and the glibc it built against,** into the job log and the run summary, so the day `ubuntu-latest` moves to 26.04 the floor moves visibly. It is deliberately not pinned: pinning would hold the tarball still while the test matrix moved, and the question you want asked is which of the two should move.
- **Not added to the CI matrix,** as you say: a preview label buys early warning at the price of a flaky job.

Entry 61 section 2 said "Ubuntu LTS first" without naming a version, which is the sort of gap that gets re-argued in three months. This names it and records why, and section 2 is a build constraint that outlives the VM.

Ubuntu 26.04.1 LTS is out and it is the obvious choice if newer is better. It is the wrong one here.

### 1. The reason the VM exists is CI reproduction, and CI is 24.04

`.github/workflows/ci.yml` and `.github/workflows/gate-record.yml` both run `ubuntu-latest`. **`ubuntu-latest` is still Ubuntu 24.04.** Ubuntu 26.04 exists on GitHub Actions only behind the explicit `ubuntu-26.04` label, which is marked preview, and no migration date for `ubuntu-latest` has been published. The 22.04 images only began deprecation on 17 September 2026, which is the rhythm: 24.04 has just become the settled default rather than the new thing.

Entry 61 section 2 justified the VM on one property, that a CI failure becomes reproducible in seconds instead of through a ten minute push cycle. **A 26.04 VM does not have that property.** It is a third platform to reason about rather than a local copy of the second.

### 2. glibc runs forward and not backward, so the tarball must be built on the oldest supported distribution

Entry 61 section 2 named the native OpenCV runtime's glibc as the most likely thing to break across distributions. That asymmetry has a direction that was not written down.

**A binary built against an older glibc runs on a newer one. A binary built against a newer glibc does not run on an older one.** So a newer distribution is the forgiving end of the range, and proving GroupLab on 26.04 proves almost nothing about 24.04, while the reverse is close to guaranteed.

**The consequence for entry 61 section 5 item 2, the self-contained tarball:** the build host sets the floor. Built on `ubuntu-latest` at 24.04 the tarball runs on 24.04 and newer. Built on 26.04 it would run on 26.04 and newer and would fail on the distribution CI itself uses, which is the kind of defect nobody finds until a stranger reports it.

**Write the floor down wherever the tarball is produced**, as a comment on the workflow step, so that the day `ubuntu-latest` moves to 26.04 the question is asked deliberately rather than answered by a silent runner upgrade. That day is the whole risk here, and it will arrive without an announcement in this repository.

### 3. 26.04 has no X11 session at all, which narrows what a result there means

GNOME 50 removed the X11 session and Ubuntu 26.04 ships Wayland only, with XWayland for X11 clients. **Avalonia's Linux backend is X11.** Native Wayland support is in progress and existing applications are carried by XWayland.

So on 26.04 GroupLab necessarily runs through XWayland, and on 24.04 it can run natively on X11 or through XWayland depending on which session is chosen at the login screen. **24.04 can test both paths and 26.04 can test only one.** Since DPI scaling and the file dialogs are two of the things entry 61 section 2 wanted looked at, and both are exactly where that distinction shows up, the older release is the more capable test machine.

### 4. Where 26.04 does belong

**Later, and as a second machine rather than a replacement.** Entry 61 section 2 asked for the second VM to be older or differently built, on the argument that a newer distribution proves the happy path and an older one proves the range. 26.04 is on the wrong side of that for now and becomes the right side the day `ubuntu-latest` moves.

**Not in the CI matrix yet.** Adding `ubuntu-26.04` while it is a preview label buys early warning at the cost of a job that can queue and flake, which is the maintenance tax entry 61 section 1 argued against. The trigger to add it is the label going generally available, not the distribution existing.

---

## 2026-09-16, entry 62: two tablets, and a scope correction to what the licence actually blocks

**Status: actioned 2026-09-16 for section 2. Sections 1, 3 and 4 are recorded.**
- **Section 2's correction is in the README,** in the paragraph entry 60 supplied, rather than as a later amendment to it. Entry 54 section 9 keeps its original wording until that entry is actioned, and this correction applies to it then.
- **Section 3 is noted and nothing is built.** Entry 54's two-way split is recorded as resting on a phone rather than on a tablet with a pen, and the question waits on section 11's first task, which needs no device.
- **Section 4's warning is taken:** a green result on a tablet says the software runs on the operating system and says nothing about a person at a range holding a camera.

Section 2 corrects something I have now written twice, in entry 54 and in entry 60's README text. Section 3 is a product question entry 54 never asked because I was thinking about the wrong device.

Alan has a **Samsung Galaxy Tab S8 Ultra** and an **iPad Mini 6**. Both are real hardware for Phase 6 and Phase 8 and neither existed in my picture when entry 54 was written.

### 1. What each one can actually do today

| | Galaxy Tab S8 Ultra | iPad Mini 6 |
|---|---|---|
| Build produced on | Windows, directly | needs a Mac, or the macOS runners `DESIGN.md` section 21 already names |
| Installed by | `adb install`, no account | signing, and a Mac in the loop |
| Usable now | **yes** | not without a Mac |

**The Android tablet is usable immediately and the iPad is not**, and the obstacle for the iPad is a Mac rather than a licence. That matters for section 2.

### 2. The licence blocks distribution, not testing, and I have said otherwise twice

Entry 54 section 9 says iOS "is blocked on a legal answer rather than on engineering" and that "no amount of work moves iOS until that comes back". Entry 60's replacement README text repeats it.

**That is true of App Store distribution and false of everything else.** The GPL section 7 additional permission exists because plain GPL-3.0 conflicts with Apple's App Store terms. It has nothing to say about building GroupLab, installing it on a device you own, or testing it there.

**So iOS work is not blocked. iOS shipping is.** The distinction matters because the current wording would have somebody conclude there is no point starting, and the whole reason `DESIGN.md` section 9 chose AprilTag `tag36h11` over an ArUco dictionary was to keep the mobile detector on a BSD-2-Clause implementation so that the App Store path stays open when the permission lands. **That work can be proved on the iPad long before the lawyer answers.**

**Amend entry 60's README text.** Where it says "No amount of work moves iOS until that comes back", use instead:

> The permission gates distribution through the App Store, not development. Building and testing on a device can proceed without it.

And the same correction applies to entry 54 section 9 when it is actioned.

### 3. A tablet is not a big phone, and entry 54 assumed it was

Entry 54 section 6 drew a line: capture and a verdict on the device, and **not** marking by hand, assignment correction or the analysis screen, because "a phone that tries to be an editor will be a bad editor".

**That reasoning was about a phone and I applied it to mobile.** A 14.6 inch tablet with a stylus is a different proposition, and for the one task in this project that is pure pointing, it may be a better one than a desktop.

**Marking impacts is tap-to-place on a zoomed image.** Entry 39 section 4 asked for press, drag and release precisely so it works with a finger. A pen on a large screen is the most natural input that interaction could have, and better than a mouse, not worse.

**I am not proposing to build it.** I am recording that entry 54's split was drawn on an assumption that does not survive contact with this hardware, and that the honest shape may be three-way rather than two:

- **phone**: capture, and the assistant that says the frame is bad while it can still be fixed
- **tablet**: marking and correction, where a pen beats a mouse
- **desktop**: analysis, comparison, reporting, the library

**The question to answer before any of that matters is entry 54 section 11's first task**, which needs no device at all: whether the capture assistant's thresholds separate pass from fail on the Phase 0 frames. If they do not, most of entry 54 falls and this question falls with it.

### 4. What these tablets are good for now, and what they are not

**Good for: the platform.** Does the application build, install, launch, draw, find its files, write its log, survive a crash, and render the embedded fonts. That is most of what Phase 6 is, and the Android tablet can start answering it whenever somebody wants to.

**Not good for: the use case.** Nobody photographs a target with a 14.6 inch tablet. **These test whether the software runs on the operating system. They do not test the thing entry 54 says the phone is for**, which is a person standing at a range holding a camera. Proving the capture assistant needs a phone, in a hand, in daylight, with a target on a stand.

Worth saying plainly so that a green result on a tablet is not mistaken for the mobile case working.

### 5. Order

Unchanged, and nothing here moves up. The range session and the mounted photograph gate are still the critical path. When mobile does start, the Android tablet removes the first obstacle to Phase 6, and the iPad removes one of two for Phase 8 with a Mac still needed for the other.

---

## 2026-09-16, entry 61: one Linux package format, not five, and a printing defect I can predict without the VM

**Status: actioned 2026-09-16 for sections 3 and 5 item 2. Sections 1, 2 and 4 are recorded as direction, and section 5 items 3 and 4 are Alan's to call.**
- **Section 3, checked before it was changed, and your prediction is wrong in its mechanism.** .NET throws `Win32Exception(ERROR_NO_ASSOCIATION)` for a verb off Windows, not `PlatformNotSupportedException`, so the existing catch already handles it and the print button does not crash on Linux or macOS. It is your own second branch: useless there rather than broken.
- **The real defect, and it is fixed.** Every press off Windows threw, logged a warning, and then blamed the person's PDF viewer for a platform fact. Windows now keeps the print verb, everywhere else is asked only to open the file, and the words say which happened. Reported in `docs/PHASE1-RESULTS.md` "Entry 61 section 3".
- **Section 1 is taken, and section 5 item 2 is done.** The `linux tarball` job publishes self-contained on `ubuntu-latest` and uploads the artifact: 90 MB compressed, 214 MB unpacked, 256 files, carrying the native imaging library, the target library and now GroupLab's own licence, which it was not carrying. AppImage waits for somebody wanting a menu entry. Reported in `docs/PHASE1-RESULTS.md` "Entry 61 section 5 item 2".
- **Section 2 is Alan's to do when he wants it.** The VM is not on the critical path, as you say.

Section 3 is checkable today and does not need a VM. Sections 1 and 2 are a direction decision so it does not get re-argued every few months.

Alan has offered to build a Linux VM on VMware Workstation, and assumed we would want `.deb`, `.rpm`, snap, flatpak and AppImage.

### 1. Five formats is four too many, and the reason is maintenance rather than effort

Each packaging format is not a one-off job. It is a permanent obligation: its own build, its own update path, its own bug reports from people whose distribution does something slightly different, and in two cases an account and a review queue belonging to somebody else.

| Format | What it costs, forever |
|---|---|
| Self-contained tarball | **nothing.** `dotnet publish --self-contained` already produces the directory; tar it |
| AppImage | a small recipe, plus a desktop entry and an icon. No account, no review |
| `.deb` | a control file and dependency declarations per distribution version, and a repository if updates are to work |
| `.rpm` | the same again, differently, for the Fedora and RHEL family |
| snap | a Canonical account, a manifest, a review queue, confinement rules that will fight a file picker |
| flatpak | a Flathub account, a manifest, a review queue, a runtime to track |

**There are currently zero Linux users.** Not few: zero. Linux is not offered as a download, nobody runs it day to day, and nothing outside CI has ever launched the window on it.

**So: the tarball now, because it falls out of a build that already happens and costs nothing. AppImage when somebody wants a menu entry. The other four when a person asks for one by name.** Adding a format later because somebody wanted it is a good day's work. Maintaining four nobody uses is a tax paid every release forever, and it is exactly the sort of thing that makes a small project feel like a chore.

This also matches what Windows already does. `DESIGN.md` section 20 chose unsigned direct downloads plus a store listing, not every installer technology available.

### 2. The VM is worth building, and not for packaging

**The reason to build it is that nobody has ever seen GroupLab render on Linux.** CI is headless. It proves the code runs and the numbers agree. It has never drawn a window.

**Ubuntu LTS first**, because CI runs `ubuntu-latest` and matching it makes a CI failure reproducible in seconds rather than through a ten-minute push cycle. That single property is worth more than distribution coverage.

**A second VM later, on Debian stable or a RHEL-family distribution**, and the reason is specific: the native OpenCV runtime is built against a particular glibc, and that is the most likely thing to break across distributions. A newer Ubuntu proves the happy path. An older or differently-built distribution proves the range. That is a later luxury.

**What the VM would actually be used for**, so it is not just set up and stared at:

- **The window, looked at by a person.** Fonts, spacing, dialogs, DPI scaling. The application embeds IBM Plex precisely so it does not depend on system fonts, and that has never been tested anywhere the system fonts differ.
- **The file dialogs**, which are a different implementation on Linux and have never been opened.
- **Case sensitivity.** `DESIGN.md` and entry 32 section 2 both name it. The target library loads `targets/*.gltd.json` by path; any wrong case works on Windows and fails only here.
- **The log directory.** Entry 41 specified `$XDG_STATE_HOME/grouplab/logs` with a fallback, and no one has ever checked that a log appears there.
- **The crash handler and the report package**, on a platform whose paths and temp directories differ.
- **The gate record**, locally and quickly, instead of through CI.
- **Printing**, which is section 3 and which I think is already broken.

### 3. The print button is probably broken on Linux, and this does not need the VM to check

`src/GroupLab.App/PrintWindow.cs` launches the print like this:

```csharp
Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "print" })?.Dispose();
```

with two fallbacks, both `catch (Win32Exception)`.

**`Verb` is a Windows shell concept.** On Unix, `UseShellExecute = true` maps to `xdg-open`, and my understanding is that setting `Verb` to a non-empty string there throws `PlatformNotSupportedException` rather than doing anything. **`PlatformNotSupportedException` is not a `Win32Exception`, so neither catch block sees it.**

If that is right, then on Linux and macOS:

1. the print button throws,
2. **nothing catches it**, so it reaches the dispatcher handler entry 41 installed,
3. and the user is offered a crash report for pressing Print.

**That is worse than not printing**, because entry 41's handler will faithfully report it as a crash every single time, and the fallback path that exists specifically to open the PDF instead never runs.

**I could not test this.** There is no .NET runtime in either environment I can reach, so this is a prediction from reading the code and not a measurement. **Check it before changing anything**, because if `Verb` is silently ignored on Unix rather than throwing, the code is merely useless there rather than broken, and the fix is different.

**If it is right, the fix has two parts.** Catch the platform exception as well, so the fallback runs. And on Unix do not set `Verb` at all: open the PDF in the default viewer and say so, which is what the first fallback already does and what a Linux user would expect anyway.

**This is worth doing whatever happens with the VM**, because the same code path runs on macOS, and macOS is a platform we already build and test.

### 4. Where this sits

**Not on the critical path and it should not displace anything.** The range session and the mounted photograph gate are the critical path. The VM is cheap for Alan and it unblocks a class of work that otherwise waits indefinitely, which is a good reason to do it soon and not a reason to do it first.

### 5. Order

1. **Section 3**, the print path. Checkable now, affects macOS too, and it is a defect rather than a feature.
2. **The self-contained tarball** as a build output, since it costs nothing and gives the VM something to install.
3. The VM itself, whenever Alan has half an hour.
4. AppImage if and when somebody wants a menu entry.

---

## 2026-09-16, entry 60: the README undersells Linux and macOS, and here is the replacement text

**Status: actioned 2026-09-16.** Your text is in the README as written, with one change: the iOS sentence carries entry 62 section 2's correction instead of "No amount of work moves iOS until that comes back", so the retraction lands with the paragraph rather than after it. The phase table's iOS row says the permission gates distribution and not development, for the same reason.
- **What I did not touch:** the `<!--platforms-->` markers further down, which `ReadmeTests` holds equal to the CI matrix, and the test counts you deliberately left out.

Small and self-contained. Alan's decision, my wording. Place it or tell me what is wrong with it.

### 1. What is wrong with the current text

The Planned section ends with:

> **macOS and Linux are wanted and are not a phase.** Nothing in the measurement core is Windows-specific and Avalonia runs on all three, so this is packaging and an imaging-backend reference rather than a port. It is tracked as a continuous requirement: CI builds and tests all three on every push, and before either is offered as a build, the Phase 0 gate record has to reproduce on that platform rather than merely compile.

**Every fact in it is true and the framing is wrong.** "Wanted and not a phase" reads as a nice-to-have somebody might get to. What is actually happening is that every commit builds and tests all three platforms, and a second workflow reruns the entire Phase 0 measurement record on all three and compares it against the Windows record. **That is a harder bar than most projects apply to their primary platform**, and the page describes it as an aspiration.

It also buries the ordering. Alan's priority is Windows 10 and 11 first, then Android, then iOS, with Linux and macOS built alongside Windows rather than after it, and not used as test platforms.

### 2. Replace the paragraph above with this

> ### Platforms
>
> **Windows 10 and 11 is what GroupLab is built for.** It is where the application is developed and used, where every screenshot comes from, and the only platform offered as a download today.
>
> **Linux and macOS are built and tested alongside it, not after it.** Every push builds and runs the whole suite on all three. A second workflow reruns the complete Phase 0 measurement record on all three and compares every printed table against the Windows record, which is a harder question than whether the code compiles: it asks whether the three platforms produce the same answers.
>
> | Platform | Built and tested | Reproduces the Phase 0 record | Offered as a download | Used day to day |
> |---|---|---|---|---|
> | Windows 10 and 11 | every push | the reference | **yes** | yes |
> | Linux | every push | **yes** | not yet | no |
> | macOS | every push | not yet | not yet | no |
>
> **What stands between Linux and macOS and a download is the record, not the build.** Linux reproduces it. macOS differs on a small number of measurement rows, traced to corner refinement inside the native imaging library and to one further divergence below it. That is an open item with a named cause rather than an unknown, and it is tracked in `docs/PHASE1-RESULTS.md`.
>
> **Neither is used as a test platform, deliberately.** Targets are printed, shot, photographed and marked on Windows, so that is where the application meets real data. Linux and macOS are held correct continuously so that neither turns into a port later, which is the expensive way to do it.
>
> **Mobile comes after the desktop, Android first.** Android is Phase 6. iOS is Phase 8 and is held up by a licence question rather than by engineering: distribution through the App Store needs the GPL section 7 additional permission described under Licence, which is drafted and with a lawyer and not in force. No amount of work moves iOS until that comes back.

### 3. One line in the phase table

The table's row **8. iOS** reads "Built and signed on CI." Make it:

> | **8. iOS** | Built and signed on CI. Waits on the licence permission under Licence, not on engineering. |

So the ordering has its reason attached where somebody reads the order.

### 4. What I deliberately left out

**No test counts.** They change every few commits and `ReadmeTests` has no marker to guard them, so a number there goes stale the way the build-platform sentence did. The table says "every push", which stays true.

**No claim that Linux or macOS is supported.** They build, they test, and one of them reproduces the record. None of that is the same as somebody being able to download and run it, and the table keeps those four things in separate columns on purpose.

**No date for either.** There is not one, and the last thing that section needs is another aspiration.

---

## 2026-09-16, entry 59: Android keeps what iOS drops, and two frames from one phone prove entries 16 and 27 live

**Status: open, with order item 2 done.** Item 4 is taken: entry 58 section 3 came first.
- **Both files are published clean** into a scratch directory, with GPS and 31 other EXIF fields removed from each, and `PublicationTests` passes over the published copies. First consented GPS-bearing files through the publication path.
- **Section 3 is visible in the tool's own output:** triage printed lens group `2.20 mm f/2.2, 23 mm equivalent, digital zoom 1.66` for the camera app's frame and `6.25 mm f/1.7, 23 mm equivalent, digital zoom 1.00` for the page capture. One phone, one scene, one 35 mm equivalent, two optical configurations.
- **Section 4's lens-key check is queued** in place of entry 58 section 5, and section 1's narrowing is taken: the iOS story is iOS only, and the Android in-page capture is not a degraded file.

Section 1 narrows a claim in entry 58 that I made too broadly. Section 3 is the valuable part and it is a demonstration, on real files, of the thing entries 16 and 27 were written about. Section 4 changes what the upload page should check.

Alan submitted two photographs from a Samsung Galaxy Z Fold7 on Android 16 through Firefox: `2853d64d` taken with the camera app and chosen from files, and `b5f6b92e` taken with the page's own picture button. Both consented, neither opted out.

### 1. On Android the in-page capture keeps everything, so entry 58's story is iOS only

| | iOS, captured in page | Android, captured in page | Android, from files |
|---|---|---|---|
| EXIF tags | **8** | **58** | **58** |
| GPS tags | **0** | **6** | **6** |
| Make, Model | absent | present | present |
| Focal length, 35 mm equivalent | absent | present | present |
| `DigitalZoomRatio` | absent | present | present |

**So the stripping is a property of iOS's capture path, not of in-page capture.** Entry 58 section 1 said the least useful capture path is also the most private one and that those were the same fact. **That is true on iOS and false on Android**, where the page's own capture attached a position like any other photograph. Read that paragraph as scoped to iOS.

**The Android in-page capture is not a degraded file at all.** Same 12 MP, same tag count, same fields. There is nothing to warn anybody about.

### 2. `LensModel` is not universal

The iPhone files carry `LensModel` reading "iPhone 17 Pro back triple camera 6.765mm f/1.78". **Both Samsung files have no `LensModel` at all.**

Entry 48 added it to the scrubber's keep list on the argument that it names which camera in a multi-camera phone took the frame. **That argument holds and the field is still worth keeping, and it cannot be relied on**, because a large share of Android phones do not write it. Anything that needs to know which camera took a frame has to work without it, which section 3 shows is possible.

### 3. Two frames, one phone, one minute, the same 35 mm equivalent, and different optics

This is the finding.

| | Camera app, from files | Page's picture button |
|---|---|---|
| Physical focal length | **2.2 mm** | **6.25 mm** |
| Aperture | **f/2.2** | **f/1.7** |
| `DigitalZoomRatio` | **1.66** | **1.0** |
| 35 mm equivalent | **23** | **23** |
| Pixels | 4000 x 3000 | 4000 x 3000 |

**The same phone, minutes apart, used two different physical cameras, and both frames report an identical 35 mm equivalent of 23.** One is the ultrawide cropped 1.66 times to reach that framing; the other is the main camera at no zoom.

**That is entry 16 and entry 27 demonstrated on real files rather than argued.** Entry 16 found that a cropped ultrawide keeps its physical focal length and changes only the equivalent, and that grouping by the physical value alone mixes two pixel geometries into one joint fit and crashes it. Entry 27 found that `DigitalZoomRatio` has to join the grouping key and that its absence means unknown rather than 1.0.

**Here both fire at once, and the pair is a test case.** Anything grouping by 35 mm equivalent pools these two, and they should never be pooled: one is an upscaled crop of a small sensor region and the other is a native frame. **Only the combination of physical focal length, aperture and digital zoom separates them**, and on this phone there is no `LensModel` to fall back on.

**One consequence nobody had written down: which camera takes the picture is a property of the capture path, not of the phone.** The camera app chose the ultrawide and the browser chose the main camera, on the same device, for the same scene, within a minute. Any assumption that a contributor's frames come from one optical configuration is wrong, and it is wrong in a way that is invisible unless these fields are read.

**And the better-provenanced frame is the optically worse one.** The camera app file has the cleaner story and is an upscaled 1.66 times crop of the ultrawide. The page capture, which I was ready to warn people away from, used the better lens. That is worth remembering the next time I am confident about which path to recommend.

### 4. So stop classifying the capture path and test for the fields

Entry 58 section 5 asked for three provenance states and a page message about camera originals. **Narrow it.**

**The question that matters is not how the file was captured. It is whether the file carries the fields the work needs.** That question has one answer on every platform, needs no signature table, and does not go stale when Apple or Google change something.

1. **`CameraOriginal` keeps its job**, which is deciding whether a file is a camera original for publication purposes. That is a different question and entry 35 section 1 answers it well.
2. **Add a separate, plainly named check: does this file carry a usable lens grouping key?** Make, model, physical focal length, 35 mm equivalent, aperture, and digital zoom, with `LensModel` as a bonus rather than a requirement. Record the answer in the provenance as a field.
3. **The page says something only when the answer is no**, which on the evidence so far means iOS in-page captures and messaging copies and nothing else. On Android it will say nothing, correctly.
4. **The lens and surface work filters on that field** rather than inferring anything from filenames or tag counts.

**That is less code than entry 58 asked for and it survives platform changes**, which the signature table would not.

### 5. Both of these carry a position, and they are consented

Six GPS tags each, `exclude_from_public_dataset` false, no `DO-NOT-PUBLISH`. They are Alan's own photographs so the location is his and he knows it is there.

**They are the first consented, publishable, GPS-bearing files to reach the corpus.** Everything before them was either opted out or already stripped. **So they are the first real end-to-end test of the scrubber on the publication path**, rather than on files that were never going to be published. Worth running deliberately rather than incidentally, and worth checking the published copies with `PublicationTests` afterwards.

### 6. Order

1. **Section 4's lens-key check**, replacing entry 58 section 5's three states.
2. **`grouplab intake` on `2853d64d` and `b5f6b92e`**, then `PublicationTests` on the result. First consented GPS-bearing files through the publication path.
3. **Keep the pair as a fixture for the grouping key.** Two frames, one phone, one scene, identical 35 mm equivalent, different optics, and the correct behaviour is that they are never pooled. **That is a better test than anything synthetic**, and it exists only because Alan happened to take the same picture twice.
4. Entry 58 section 3's pixel hash still comes before all of this.

---

## 2026-09-16, entry 58: the browser was innocent, and the content-hash opt-out has a hole

**Status: actioned 2026-09-16 for sections 3 and 4, and order items 1, 2 and 4.**
- **The hole is closed, by the cheaper key you asked to be measured first.** The four uploads scrub to one value, `8b106005`, where their bytes give four. `Intake.PhotographSha256` is that value, `WithheldHashes` records it beside each withheld file's byte hash, and either match alone withholds.
- **It does not collide:** the two Android frames of one scene scrub to different values, and so does the other frame of the shot sheet. It is deterministic, and it needs no image decoder, which keeps the consent check inside `GroupLab.Core`.
- **Order item 2 is done:** the four opted-out uploads are refused with nothing written; `fc4d1649` is held as not a camera original; the two consented Android photographs publish clean and `PublicationTests` passes over them.
- **Order item 4 is done,** as a standing note where the donated corpus is described rather than in `docs/DETECTION-PIPELINE.md`, whose section 2 describes the `scans/` corpus.
- **Section 5 is not done, and is narrowed by entry 59 section 4** into one check for whether a file carries a usable lens grouping key. That is the next thing after entry 55 section 3 item 1.

Reported in `docs/PHASE1-RESULTS.md` "Entry 58 sections 3 and 4".

Section 1 retracts entry 57 section 2. Section 3 is a defect in a consent mechanism and it is the reason this entry exists. Section 4 is a fix I have tested.

Alan's friend uploaded the same camera original through Chrome for iOS, Firefox for iOS, DuckDuckGo and Safari, all four marked do not publish. Four submissions, `21ea25de`, `60d5c524`, `b73d9520` and `d2425244`.

### 1. My hypothesis was wrong. No browser strips anything

| | Chrome for iOS | Firefox for iOS | DuckDuckGo | Safari |
|---|---|---|---|---|
| Bytes | 5,272,027 | 5,272,027 | 5,272,027 | 5,272,027 |
| EXIF tags | **69** | **69** | **69** | **69** |
| GPS tags | 15 | 15 | 15 | 15 |
| Make, Model, LensModel | present | present | present | present |

**Every iOS browser preserved everything.** The upload path is sound and was never the problem.

**The real cause is that the damaged photograph was never a file at all.** Alan established it: his friend followed the link from Discord and used the **"Take Photo"** option in the iOS picker rather than choosing a file. The photograph was captured straight into the page and never reached the camera roll.

Every piece of evidence fits, and two of them fit nothing else:

| | In-page capture | Chosen from the camera roll |
|---|---|---|
| Name | `image.jpg` | `IMG_5818.jpeg` |
| Resolution | 4032 x 3024, **12.2 MP** | 5712 x 4284, **24.5 MP** |
| EXIF tags | 8 | 69 |
| GPS tags | **0** | 15 |

**12 MP is the capture preset a page gets. 24 MP is what the Camera app writes on a 17 Pro.** A page-side capture never runs the Camera app's pipeline, so the lens model, the maker note and the rest are never written rather than being stripped afterwards.

**And it carries no position at all, by construction**, because a web page has no location permission. That is worth noticing on its own: **the least useful capture path is also the most private one**, and those two facts are the same fact. Anybody proposing to push contributors away from it should know they are trading a privacy property for lens data.

**I got this wrong and I want to be precise about what was and was not right about it.** The conclusion was wrong. The form was right: I gave the hypothesis a two-minute test, said in advance what each outcome would mean, and the test killed it the same afternoon rather than anybody acting on it. That is entry 47's lesson applied, and it is the only reason a wrong idea of mine cost twenty minutes instead of a week.

**One thing that would have misled anybody: DuckDuckGo reports itself as Safari.** Its user agent is indistinguishable, so the `user_agent` field cannot identify it, and the original damaged upload recorded "Safari" while possibly being a chat app's in-app browser. **The user agent is not evidence of which browser was used on iOS**, and nothing should be concluded from it.

### 2. What the pristine originals contain

iPhone 17 Pro, back triple camera 6.765 mm f/1.78, 35 mm equivalent 24, ISO 800, 1/60, **5712 by 4284**, which is 24 megapixels and a good deal larger than anything in the corpus.

**`DigitalZoomRatio` is absent even here, on an untouched camera original.** That is entry 27's point demonstrated on pristine data: absence means unknown, and the grouping key must treat it as unknown rather than as 1.0.

**They carry 15 GPS tags.** I did not read the values and no one should; the count is the only part that matters. These are the first files since entry 37 to give the scrubber real work, and they are marked do not publish, so running intake on them is a test rather than a publication.

**All four carry `DO-NOT-PUBLISH` and `exclude_from_public_dataset` was not needed to stop them.** The belt-and-braces opt-out of entry 37 section 1 did its job on its first real outing.

### 3. The defect: entry 37's opt-out by content hash does not survive a re-export

**The four files are the same photograph and have four different SHA-256 values.**

The only difference between them is **29 bytes, a 36-character UUID inside the Apple maker note**, which iOS regenerates every time the photo is exported from the library. Everything else in 5.27 MB is byte-identical: same pixels, same EXIF, same GPS.

Entry 37 section 2 established the rule after the same photographs arrived twice with contradictory consent:

> An opt-out wins by content hash, across every submission. If bytes appear anywhere in an opted-out submission, those bytes are not published from any submission.

**That rule silently fails whenever the two uploads are separate exports rather than the same file.** A contributor who uploads a photograph, thinks better of it, and re-sends the same picture with the opt-out ticked, will not be matched. The hash set will hold bytes that no longer describe the photograph we are trying to withhold.

**It worked in entry 37 only because that contributor uploaded the identical file twice.** Had he re-exported it from his camera roll, the conflict would never have been detected and one of those photographs would have been published against his intention. **That is the one mistake in this pipeline that cannot be undone**, and it has been one re-export away since the rule was written.

### 4. The fix, tested rather than proposed

**Add a pixel hash beside the byte hash and match on either.**

| | Distinct values across the four uploads |
|---|---|
| File SHA-256 | **4** |
| SHA-256 of the decoded pixels | **1** |

One value for all four. And it does not collide with the other frame of the same sheet taken moments earlier, so it is not so loose as to merge different photographs.

**Keep both.** The byte hash is exact and cheap and catches the identical-file case. The pixel hash catches the same-photograph-re-exported case, which is the one that is currently open. **Either matching is enough to withhold**, which is the same belt-and-braces reasoning entry 37 section 1 applied to the two opt-out signals and for the same reason: redundancy is the point rather than a smell.

**Two limits worth writing down rather than discovering later.** A pixel hash does not survive re-encoding, so a messaging app's copy will not match its original; nothing matches those except a perceptual hash, and I am not proposing one. And it does not survive a crop or a rotation. **It closes the re-export hole and no other**, which is worth saying so nobody assumes more of it.

The scrubber offers a third option worth measuring while you are in there: **it strips the maker note, so scrubbed copies of these four may well be byte-identical.** If they are, the scrubbed hash is a cheaper key than decoding pixels. Check it; if it holds, prefer it.

### 5. The upload page fix is now precise

Entry 57 section 2 asked the page to check whether a file is a camera original. **That is still right and now it is the whole of the fix**, since no browser needs naming.

`CameraOriginal` from entry 35 section 1 already makes this judgement in `grouplab intake`, after the contributor has gone. The same check belongs in the page while they are still there, and the signals are all in the damaged file: **no camera make, a generic name, and a resolution below what that camera writes.**

**Do not refuse it and do not nag.** A frame with no lens data is still good for the geometry, the registration and the gates, which is most of what the corpus is for. It is useless only for the lens and surface work. So one sentence at the moment of choosing, something like: *"Taking the photo here is fine, and choosing one from your camera roll instead keeps the lens details that make a photo most useful to us."* Then let them decide.

**Record which path it came from, rather than inferring it later.** `CameraOriginal` returns a boolean today. Three states carry more: **camera original**, **captured in this page**, and **unknown or altered**. The first two are cleanly separable by the table in section 1. The third is the residue. A field costs nothing and it lets the lens work exclude in-page captures without guessing, which is the thing that will otherwise be re-derived by somebody in a year.

### 6. Order

1. **Section 4's pixel hash**, with the scrubbed-hash check first in case it is cheaper. This closes a consent hole and it comes before anything else in this entry.
2. **Run `grouplab intake` on the four browser-test submissions.** First real exercise of the scrubber on GPS-bearing files since entry 37, and first real exercise of `CameraOriginal` against a genuine stripped file if you include `fc4d1649`.
3. **Section 5's page check.**
4. **Record in `docs/DETECTION-PIPELINE.md` or wherever the corpus is described that the user agent cannot identify an iOS browser**, so nobody draws a conclusion from it later.

### 7. What this does not change

The four test submissions are all the same photograph of the same flat sheet, so they add nothing to the corpus beyond this finding. **The mounted gate still has no real example.** Entry 56's scan and photographs are still the only real-holes material, and the flat photograph case is still the only gate this has moved.

---

## 2026-09-16, entry 57: submission fc4d1649, and a two-minute test that matters more than the submission

**Status: open, and section 2 is closed by entry 58 section 1.** The browser test was run by you and no browser strips anything; the damaged file was captured inside the page.
- **Order item 2 is done:** `grouplab intake` on `fc4d1649` holds it, "not a camera original: it has no camera make", with 38 markers decoded at 4032 by 3024. First real stripped file `CameraOriginal` has met, and it held it. Reported in `docs/PHASE1-RESULTS.md` "Entry 58 sections 3 and 4".
- **Item 3,** analyze on photograph A against the scan of the same sheet, is queued with entry 56's runs.
- **Item 4, section 5's provenance distinction,** is queued and nothing has been written into a provenance record that the contributor did not supply. The three answers Alan knows by conversation are not in `fc4d1649`'s record.

Section 2 is a hypothesis with a test attached, and if it is right it changes what the upload page has to do. Section 5 is a provenance integrity point that must not be skipped.

### 1. What arrived

`2026-09-16_fc4d1649`, submitted 15:15 UTC. **Consent agreed, `exclude_from_public_dataset` false, no `DO-NOT-PUBLISH`.** One file, `001_image.jpg`, 2,135,426 bytes, SHA-256 `50182f7cac67247a`.

It is the same physical sheet as entry 56: `GL-20J3-Y141-0BN3-EYME`, the same thirteen holes, the same concrete.

**It is not the same photograph Alan was messaged.** Normalised correlation between the two is 0.59, so they are two separate frames taken moments apart from slightly different positions.

**So the project now holds three views of one physical sheet with real holes in it:**

| | Resolution | Path |
|---|---|---|
| 300 DPI scan | 2550 x 3506 | flatbed, entry 56 |
| Photograph A | 4032 x 3024 | this submission, consented |
| Photograph B | 2160 x 2880 | messaged to Alan, downscaled, no consent |

**That is the first flat photograph gate material with real holes, and it comes with a scan of the same sheet.** The scan is the strongest reference the project has ever had for a photograph, because measuring a photograph against a scan of the same paper separates the photograph's error from the printing's.

### 2. The headline: this iPhone upload lost its camera data, and I think the browser did it

**Full resolution survived. The camera metadata did not.**

| | Entry 37's iPhone uploads | This one |
|---|---|---|
| Browser | **Chrome for iOS** | **Safari** |
| Resolution | full | full, 4032 x 3024 |
| EXIF tags | **48** | **8** |
| Make, Model, LensModel | present | **absent** |
| Focal length, 35 mm equivalent, f-number | present | **absent** |
| GPS | present, and scrubbed | absent |

Eight tags and the only useful one is Orientation.

**The hypothesis: Safari's file picker strips EXIF where Chrome for iOS does not.** The alternative is that this contributor stripped it himself before uploading. Those two have completely different fixes and I cannot tell them apart from one file.

**The test, and it takes two minutes.** Alan photographs anything with his own iPhone, then uploads the same file to `pissinhot.com/targets` twice, once through Safari and once through Chrome for iOS. Compare the tag counts on the two submissions.

- **If Safari strips it**, then a large share of iPhone submissions have been arriving without lens data and nobody knew why, and no amount of instructing contributors will fix it. The upload page would need to say which browser to use, which is an unpleasant thing to have to say and better than silently collecting unusable frames.
- **If Safari does not strip it**, the contributor did, and the fix is the page telling him so.

**Either way the page should check and say so at upload time.** `CameraOriginal` from entry 35 section 1 already makes this judgement in `grouplab intake`, after the fact, when the contributor has gone. The same check belongs in the page while the person is still standing there: *"This photo has had its camera information removed, which makes it much less useful. A photo straight from the camera roll keeps it."*

**Entries 16, 27 and 48 are why this matters.** They established between them that the lens grouping key needs make, model, lens model, focal length, 35 mm equivalent and digital zoom ratio. **This frame carries none of them.** It is good for the geometry, where `LensFit` recovers the lens from the image, and useless for the lens and surface work.

### 3. The answers went backwards, and the reason is probably structural

| | First submission | After the Discord edit | This one |
|---|---|---|---|
| Answers filled | 0 of 6 | 6 of 6, both | **1 of 6** |

Only `target_backing` is set, to "Other or not sure". No attachment method, no distance, no calibre, no notes, no credit name.

Entry 37 section 3 recorded that the difference between a useless submission and a good one was the wording of the request rather than the contributor. **This submission is evidence for that and against where the wording currently lives.** The two good ones came from people who read the edited Discord post. This one came from a person Alan asked directly, who therefore never saw it.

**So the page is carrying less than the post is**, and the post is the part that worked. Whatever the post says that the page does not should move onto the page. I am not specifying the wording here because I have not read the current page text; if somebody stages it for me I will write it.

### 4. What this does and does not unblock

**Does:** the flat photograph gate now has a real-holes case, twice over, with a scan of the same sheet as reference.

**Does not:** the mounted gate. This sheet is flat on concrete. **The gate that fails zero of seven still has no real example**, and it remains the single most valuable thing anybody could send.

### 5. Provenance: we know things the contributor did not tell us, and the record must say which is which

Alan knows from conversation that this is **300 Blackout at 25 yards with three sighters**. **The submission says none of it.**

**That knowledge must not be written into the provenance as though the contributor supplied it.** Entry 37 section 5 established that a stated sheet dimension is valuable precisely because it came from the person who shot it. The same principle cuts the other way here: a figure attributed to a contributor who never gave it is a corpus that lies about its own sources, and it is the sort of error that is invisible later and impossible to unpick.

**Record it as third-party stated, with who said it, or leave the fields empty.** If the provenance record has no way to express that distinction, it needs one, and that is a small schema change worth making now while there is exactly one instance rather than fifty.

The clean fix is to ask him to resubmit with the answers filled in. That is better than any amount of annotation.

### 6. The two files that are not consented

The scan and photograph B, both in `C:\Dev\grouplab-originals\friend-2026-09-16\`, have no consent record. **Nothing changes about them: no consent, no publication.** Photograph A, this submission, is consented and may be published once intake clears it.

**Worth noticing that the consented copy is also the better one**, at full resolution against photograph B's downscale. The rule and the quality happen to point the same way here, which is luck rather than design.

### 7. Order

1. **Section 2's browser test.** Two minutes, and it decides whether the upload page needs to name a browser.
2. **Run `grouplab intake` on `fc4d1649`.** It is the first submission to exercise `CameraOriginal` against a real stripped file, and I want to see what it says.
3. **Run `grouplab analyze` on photograph A**, against the scan of the same sheet as reference. First flat photograph gate case with real holes.
4. **Section 5's provenance distinction**, before any of this reaches `grouplab-testdata`.
5. Section 3's page wording, once somebody stages the current page text for me.

---

## 2026-09-16, entry 56: the first GroupLab sheet anybody has shot, and a design rule it hands us

**Status: open.** Queued behind entry 55 section 3 item 1. Nothing from these two files is published or committed: they carry no consent record, and section 10 is right that this changes nothing.
- **Order items 1 and 2,** `grouplab analyze` on the scan and on the photograph against your thirteen holes, are the next measurement after the stage-record field.
- **Section 7's table and section 8's disagreement flag** follow them, so the rule lands with the pipeline's own numbers beside yours rather than before them.

Alan's friend shot a `GL-CF25-LTR` with **300 Blackout at 25 yards, three sighters**, scanned it, and sent a photograph. **This is the first GroupLab sheet in existence with holes in it, and the first photograph of real holes.** Section 7 is the finding that matters most and it is a product rule the project does not have.

Everything measured below is mine, from my own hole finder on the scan. **It is a cross-check for the pipeline, not an answer.** Where the pipeline disagrees, assume the pipeline and tell me.

### 1. What arrived

- **A 300 DPI flatbed scan.** 2550 by 3506 px, exactly 8.5 in wide, so the scanner overran the page bottom.
- **A photograph**, sheet flat on concrete, taken from above, slightly oblique, with a shadow across the lower left. Whole sheet in frame with margin, markers crisp. **This is flat photograph gate material with real holes, which has never existed before.**
- **No before-shooting scan**, so no clean per-sheet reference to difference against. The sheet declares its own definition, `GL-20J3-Y141-0BN3-EYME`, the live `GL-CF25-LTR`, so the expected artwork is derivable.
- **Known now:** .308 bullet, 25 yards, three sighters, so the ten holes in the top two rows are one shot each at bulls 1 to 10.

### 2. The photograph has no metadata at all, and that is a finding

2160 by 2880 px and **zero EXIF tags**. No make, no model, no lens, no focal length, nothing. An iPhone frame is 4032 by 3024; this has been downscaled, re-encoded and stripped, which is what a messaging app does.

**`CameraOriginal` from entry 35 section 1 would flag this correctly**, and this is the first real file to exercise it.

**The consequence is specific.** Entries 16, 27 and 48 between them established that the lens grouping key needs make, model, lens model, focal length, 35 mm equivalent and digital zoom ratio. **This frame carries none of them**, so it is useless for the lens and surface work and useful only for the geometry, where `LensFit` recovers the lens from the image itself.

This is the clearest argument yet for the upload page: **the messaging path destroys exactly the data two entries were written to protect.** Ask him for the original, sent as a file rather than in a message.

### 3. The sheet was printed correctly, and proves it itself

| | Measured | Design | Scale |
|---|---|---|---|
| Across | 448.9 px, 1.4963 in | 1.5000 in | **99.75%** |
| Down | 449.5 px, 1.4983 in | 1.5000 in | **99.89%** |

Printed at actual size within a quarter of a percent. The 0.14 percent difference between axes is printer or scanner and this data cannot separate them.

### 4. Thirteen holes, found without a miss

My finder keys on the torn-fibre crown, mid-grey over a wide ragged area, unlike printed ink which is near-binary. **Thirteen blobs found, thirteen holes present, no false positive and no miss.** That count is the first thing the detector should be checked against.

| # | x in | y in | | # | x in | y in |
|---|---|---|---|---|---|---|
| 1 | 3.30 | 1.57 | | 8 | 3.21 | 2.98 |
| 2 | 7.25 | 1.85 | | 9 | 6.27 | 3.07 |
| 3 | 5.24 | 1.93 | | 10 | 7.53 | 3.53 |
| 4 | 4.26 | 1.96 | | 11 | 3.10 | 9.18 |
| 5 | 2.14 | 2.37 | | 12 | 5.29 | 9.45 |
| 6 | 4.68 | 2.71 | | 13 | 6.02 | 9.45 |

Origin is the scan's top left at 300 DPI.

### 5. What a .308 hole in paper actually measures, which the size check needs

Overlaying a true 0.308 in circle on an isolated hole: **the torn crown matches the calibre closely, reaching a little beyond it in places, and the bright aperture inside is roughly 0.21 in, about 0.68 of the bullet diameter.** Paper is elastic and closes behind the bullet.

**So a detector reporting the crown measures about the calibre, and one reporting the bright core measures about seven tenths of it.** Entry 40's size check compares a measured hole against what the calibre should give, and **which of those two quantities it is comparing changes the answer by a third.** That needs stating explicitly wherever the check lives.

**A correction to my own work, because it nearly went into this entry as fact.** My first pass reported the disturbed disc as 0.214 in equivalent diameter, which would have made a .308 hole look far too small and the size check look broken. That was a gap-filled mask underestimating the area. The overlay picture is what caught it. **Fourth time this week my own measurement was the thing that was wrong, and the only reason it did not reach you is that I looked at the picture.**

**On a white-lid flatbed scan the aperture is about as bright as the paper**, so the only reliable signal is the crown. Anything requiring a dark centre will reject these outright. Worth confirming against the shipped detector rather than assuming either way.

### 6. The detector cases in it, better than anything we could have staged

- **A hole through the centre dot.** Hole 4 is 0.078 in from bull 3's centre and has destroyed the printed dot. **Any locator using the centre dot has just lost it on that bull.**
- **Holes on the inner ring.** Holes 2 and 10, at 0.190 and 0.285 in from their centres.
- **Holes that break the outer ring.** Holes 12 and 13 sit on S3's ring, and my own ring finder failed to find S3 because of it.
- **A hole beside a fiducial**, hole 1, close to a tag36h11 marker without touching it.
- **Not present: no two holes overlap.** Closest pair 0.73 in. Entry 40's two-holes-as-one case is still unexercised on real paper.

### 7. The design rule this hands us, which nothing in the project states

**A 25-bull sheet has an implicit accuracy requirement and we have never written it down.**

On a square lattice of spacing `s`, a shot is nearest its own bull while it stays inside that bull's Voronoi cell, a square of side `s`, so misassignment under nearest-bull is a per-axis excursion and is closed form. Verified against 400,000 simulated shots at three ratios, agreeing to three decimals:

| Spacing / sigma | Misassigned | |
|---|---|---|
| 3 | 24.9% | 1 in 4 |
| 4 | 8.9% | 1 in 11 |
| 5 | 2.5% | 1 in 41 |
| **6** | **0.54%** | 1 in 185 |
| 7 | 0.09% | 1 in 1,075 |
| 8 | 0.013% | 1 in 7,894 |

**Being off zero makes it worse**, which is its own argument for zeroing first: at 6 sigma spacing, a centre 1 sigma off aim takes misassignment from 0.54 to 2.54 percent.

**This sheet sits at spacing over sigma of 3.89.** Expected misassignment 10 percent zeroed, 21 percent with its actual 0.38 in offset. Observed: 2 of 10. The arithmetic and the paper agree.

**The rule: bull spacing wants to be at least 6 sigma at the shooting distance, and 7 is comfortable.** For `GL-CF25-LTR` at 1.5 in that means sigma at or under 0.25 in on the paper, whatever the distance.

### 8. The two assignment methods fail in opposite ways, and that is the product finding

With the shot count now known, this is confirmed rather than hypothesised.

| | Nearest bull | One to one |
|---|---|---|
| Distinct bulls claimed | 8 of 10 | **10 of 10** |
| Holes assigned differently | | 2 (holes 5 and 6) |

**Nearest-bull silently double-assigns. One-to-one silently forces a bijection that may not exist.** Had he fired 13 at 10 bulls rather than 10, one-to-one would have produced a confident wrong answer and nothing on the sheet would distinguish the cases. **Only the shooter knows.**

So, the same shape as entries 39, 40, 52 and 55 from a sixth direction: **when the two methods disagree, that is information and it must reach the user.** Here they disagree on 2 of 10, and that disagreement is the flag.

**In order of value:**

1. **Compute both and say when they disagree**, naming the shots. Change no default.
2. **Report spacing over sigma beside the figures**, and say plainly when the sheet was too fine: "your group is 0.39 in sigma and the bulls are 1.5 in apart, so about one shot in ten lands closer to a neighbour than its own bull. A coarser sheet would measure this rifle better."
3. **Put it on the print screen**, where it does most good, before the ammunition is spent.
4. **Put the rule in `DESIGN.md` section 9 and `TARGET-LIBRARY.md`**, per family, so every sheet states the dispersion it suits.

### 9. The group, as a sanity check only

Taking the one-to-one assignment as correct: **Rayleigh sigma 0.386 in, mean radius 0.471 in, extreme spread 1.496 in centre to centre and 1.804 in edge to edge, centre 0.236 in right and 0.300 in high.**

At 25 yards, where one MOA is 0.2618 in, that is **sigma 1.47 MOA, mean radius 1.80 MOA, extreme spread 5.71 MOA.** Treat all of it as something for the pipeline to disagree with.

### 10. Consent, which is not optional even though the risk here is nil

**Neither file came through the upload page and neither carries a consent record.** A target scan has no location data and the photograph has no metadata at all, so the privacy risk is about as low as it gets, and that changes nothing: **no consent record, no publication.** Not into `grouplab-testdata`, not committed here.

Both are at `C:\Dev\grouplab-originals\friend-2026-09-16\`, for local testing only. Scan `eb62a183ea291186`, photograph `a621dabe4507b57b`.

**It is worth asking properly**, because this is the first real shot sheet and it would be a valuable committed fixture.

### 11. What to ask the friend for, in one message

1. **The original photograph as a file**, not through a messaging app, so it keeps its camera data. Section 2 is why.
2. **More photographs**, especially of a sheet still mounted where it was shot. **That is the gate the project cannot pass and there is no real example of it.**
3. **Consent through `pissinhot.com/targets`**, which also collects the rifle and load we are missing.

### 12. Order

1. **Run `grouplab analyze` on the scan** and compare against section 4's thirteen holes and section 9's figures. Report where the pipeline and I disagree.
2. **Run it on the photograph**, which is the first flat photograph gate case with real holes.
3. **Section 8 item 1**, the two-method disagreement flag.
4. **Section 7's table into `DESIGN.md` and `TARGET-LIBRARY.md`.**
5. Section 8 items 2 and 3 after the weekend, with more than one real sheet to reason from.

---

## 2026-09-16, entry 55: the spread result, an edge count that predicts trust, and a precision the tables do not have

**Status: open, with section 3 item 1 done.** It came after entry 58 section 3, as Alan directed and as entry 59 order item 4 agrees.
- **Section 3 item 1 is in, before the weekend.** Every located bull's edge point count and its count of rays that sat within a tenth of the crossing threshold are in the `P0.bulls` stage record, with the sparsest bull named in a line that prints at any verbosity. No behaviour changed: the `sheets` table reprints identically and no committed record moved. Reported in `docs/PHASE1-RESULTS.md` "Entry 55 section 3 item 1".
- **Next:** section 4's deterministic raster, then section 5's precision derivation, then entry 54 section 11's first task. Section 3 item 2 waits for the weekend's sparse bulls, as you ordered.
- **Section 4 stands and is accepted:** the synthetic raster is warped by the native library that then detects it, so those sixteen rows measure the raster and not the detector. Generating it in managed code is the fix, not an explanation.
- **Section 5 is understood as conditional:** a digit count derived from the measured instability, applied to every table and platform, decided without reference to the macOS difference, and macOS still failing if it still differs. The derivation comes to you before anything is printed differently.

Section 3 is a product change. Section 4 is a harness defect worth removing rather than explaining. Section 5 revises something I wrote in entry 49, and I flag the risk in it explicitly because it could be mistaken for moving a gate to suit a result.

First, the work itself. Recording the hypothesis before the run, keeping the sheet, markers and corners identical across 200 orderings, hashing the images in the replay so an input difference could not be mistaken for a corner difference, and finding the one M1 comparison the spread invalidates without being asked to look: that is the standard the rest of this should be held to.

### 1. The prediction held, including the part that cost something

Flat frames give one registration in 200 orderings, two on `main_flat3`. Mounted frames give 15 to 70. The instability comes from the model's mismatch to a curved sheet, and sorting made it repeatable without making it smaller. **That is the hypothesis confirmed, and confirming it is the cheap half.**

The expensive half is the one I wrote down in advance so it could not be argued away afterwards: **the mounted figures were single draws from wide spreads, and the spreads are the first error bars they have ever had.** `ultrawide2`'s worst scoring bull ranges 0.030 to 0.096 in. M1.5 read a conclusion from 0.06983 against 0.09183, and both of those sit inside that one frame's range. You found that yourself and recorded it. **Any other M1 comparison resting on a mounted-frame difference smaller than that frame's spread needs the same treatment**, and the spread table is now the thing to check each against.

**The verdict is robust and that matters.** The lowest worst bull in 1,400 mounted registrations is 0.01342 in, against a 0.005 in gate. No ordering lets a mounted frame pass. The gate's answer was never in doubt; only the numbers underneath it were.

**And the earlier observation reconciles.** The 0.30 dmm move seen on the macOS runner sits inside `telephoto3` bull 24's leave-one-out range of 0.53 dmm. Two numbers that had been floating separately in this log are now one finding.

### 2. The edge fit answer is better than my question was

I asked whether a hard include or exclude sat where a weight belongs, and assumed the fit's rejection. You found it is not there: at convergence the fit rejects no point on any bull measured. **The step is earlier, in whether a ray yields an edge point at all**, and the reason a sparse bull is fragile is that one ray is a large share of its evidence.

That is a better answer than the question deserved, and it moves where any fix would go.

### 3. The finding that should become a product change: edge count predicts how much to trust a bull

The numbers separate cleanly:

| Bull | Edge points | Largest leave-one-out shift |
|---|---|---|
| dense, across ten frames | 106 to 900 | **0.0002 in** |
| `telephoto3` bull 24 | 29, and did not converge | 0.53 dmm, 0.0021 in |
| `telephoto3` bull 25 | 14 | 0.82 dmm, **0.0032 in** |

**A 14-point bull's sensitivity is about two thirds of the whole gate. A 106-point bull's is a twentieth of it.** That is a sixteenfold difference in how much a bull's position can be trusted, and it is predictable before anybody looks at the answer, from a count the locator already has.

Non-convergence is already recorded as a rejection with a reason, which is right. **The point count is recorded nowhere, and neither is whether a bull's points sit near the crossing threshold.** This is entry 39 section 1, entry 40 and entry 52 section 4 arriving from a fifth direction: the pipeline knows something the interface is not using. The difference is that this time there is a number attached.

**Two things, and the first is small.**

1. **Record the edge point count per bull in the stage record**, and the count of its rays that fell near the crossing threshold. Change no behaviour. It is a field, and without it nothing downstream can ever know the difference between a bull measured from 900 points and one measured from 14.
2. **Measure the relationship rather than picking a threshold.** Plot the leave-one-out spread against the point count for every bull in every frame you have, and find where it crosses some stated fraction of the gate. **Do not guess a cutoff from the three sparse bulls in one frame**, which is all the sparse data that exists today. The weekend will add more, and a sheet that overflows the frame is exactly the case that produces them.

When there is a relationship, a bull below the line gets said out loud, in the place its figure appears. I will write what that looks like once the curve exists.

### 4. Sixteen of the nineteen macOS differences are a harness defect, not a platform difference

This is the most useful thing in your report and I want to be sure it is read as a defect rather than an explanation.

**A test whose input differs by platform is not testing what it claims.** Sixteen refinement rows differ because the synthetic raster is warped by the same native library that then detects it, and macOS builds the raster differently. The detector was looking at a different picture. Whatever those rows measured, it was not the detector's behaviour.

**So do not explain them. Remove them.** Generate the synthetic raster deterministically in managed code so the input is byte-identical on every platform, and the test measures the thing its name claims. That is worth doing whether or not any gate cares, and it removes sixteen of nineteen differences as a side effect rather than as a goal.

**Unless the native warp is essential to what that test measures**, in which case say so and I will withdraw this, because I am reasoning from your summary rather than from the code.

What remains after that is three paper rows where detection genuinely differs on byte-identical images, and one markers row at 0.03100 against 0.03099 with the image byte-identical and the corners Windows' own. **That is a real second divergence and you were right not to claim a cause for it.** It is also about three parts in a hundred thousand, which section 5 is about.

### 5. The gate tables print more precision than the measurement has, and I need to be careful here

Entry 49 section 1 set the rule: a platform passes when every verdict and every printed table match. **I wrote that before the spread experiment existed, and the experiment has made one of its assumptions visible.**

The tables print five decimal places of an inch. The spread experiment just measured what those numbers are actually worth: on a mounted frame the registration's own instability reaches hundredths of an inch, and on the best flat frame it is 0.0035 in. **Printing 0.03100 for a quantity whose instability is in the second or third decimal is printing three digits of noise**, and a comparison that fails on the fifth decimal is failing on something nobody could act on.

**So the tables should print the precision the measurement supports, and that precision is now measurable rather than a matter of taste.**

**Here is the risk, stated plainly, because it is the same trap entry 17 section 2 named and I do not get an exemption from it.** Reducing printed precision would make the macOS markers row stop differing. If the number of digits were chosen because it makes macOS pass, that is a gate rewritten to suit a result and it would be dishonest.

**What keeps it legitimate, and these are conditions rather than assurances:**

- **The digit count is derived from the measured instability**, from section 1's table, and the derivation is written down where anybody can check it.
- **It applies to every table and every platform**, including Windows against itself.
- **It is decided without reference to the macOS difference**, and then whatever happens to that difference happens.
- **If the honest digit count still leaves macOS differing, macOS still fails.** That is the test of whether this was done in good faith.

**So: propose the digit count from the instability figures, show the derivation, and tell me what it does to all three platforms.** I will decide after seeing it, not before, and if it looks like it was reverse-engineered I will say so.

This is also the better fix for a reason that has nothing to do with platforms. A project whose entire argument is that software should not print confident numbers it cannot support has been printing five decimals of an inch in its own gate tables for weeks. **That is the sin the project exists to criticise, in its own results document.**

### 6. Where entry 43 stands

Entry 50's first condition, the gate record green on all three platforms, is still unmet and stays unmet. **Nothing in this entry relaxes it.** Section 4 removes a defect in the harness. Section 5 proposes a precision derived from measurement, to be judged on its derivation.

If after both macOS still differs on the three paper rows, then it differs, the workflow stays red, and entry 43 stays closed. That is the arrangement working rather than failing.

### 7. Order

1. **Section 3 item 1**, the edge count in the stage record. Small, and it needs to exist before the weekend's frames arrive, because those frames are where sparse bulls will come from and the field cannot be added retrospectively to data already measured.
2. **Section 4**, the deterministic raster.
3. **Section 5**, the precision proposal with its derivation.
4. **Entry 54 section 11's first task**, whether the capture thresholds separate pass from fail on the Phase 0 frames. It needs no phone and it can kill or confirm that entry cheaply.
5. **Section 3 item 2**, the point-count relationship, after the weekend adds sparse bulls.

Item 1 before the weekend. The rest can wait for it.

---

## 2026-09-15, entry 54: the phone, which is not a small desktop

**Status: open, and deferred to Phase 6 as the entry says.** Section 11's first task needs no phone: whether frame-quality thresholds separate pass from fail on the Phase 0 frames. It is queued after entry 52 sections 3 and 4.

Entry 21's remaining scope, the other half, and the last thing owed from it. **Phase 6 work and explicitly not for this week.** It is written now because it has been owed since entry 21, and because one part of it, section 5, should change what the desktop does long before any phone exists.

This entry is more uncertain than entry 53. Section 11 says which parts I am confident about and which need a spike before anybody commits to them, and that division is the most useful thing in it.

### 1. The reframing, which is the whole entry

The obvious plan is GroupLab on a small screen: the same marking, the same figures, a phone-shaped layout. **That is the wrong product and the project's own measurements say so.**

Look at where accuracy is actually lost:

| | Result |
|---|---|
| Conformance test 43, synthetic | pass, worst 0.00026 in |
| Paper gate, scanned sheets | pass, ten of ten, worst 0.00325 in |
| Photograph gate, flat | **fail, three of three** |
| Photograph gate, mounted | **fail, zero of seven** |

**Every gate the project passes is on a scan. Every gate it fails is on a photograph.** A phone is a camera. Its contribution is not another screen for the analysis: it is the only device in the system that can act *before the photograph exists*.

So: **the phone is a capture instrument that tells you the frame is bad while you can still fix it.** Everything else it does is secondary.

Nothing else on the market can do this, and it is not cleverness on our part. It falls out of the sheet knowing what it is: GroupLab holds the marker lattice, the expected artwork and the registration residual, so it can judge a frame against the thing it is a photograph *of*. A generic camera app has nothing to compare against.

### 2. What the capture assistant checks, and where its thresholds come from

Live, on the preview, at whatever frame rate marker detection sustains:

- **Are the markers there?** How many of the sheet's markers are decoding, and which are missing. Missing corner markers are the ones that matter, because they are what extrapolates badly.
- **How oblique is it?** From the marker lattice, without any fit.
- **Is the sheet flat?** The lattice's departure from a plane is measurable from the markers alone, and it is the difference between a frame that will pass and the mounted case that fails zero of seven.
- **Is it sharp, and is it still?** Defocus at the far edge is already a named cause of Phase 0 failures.
- **Is the whole sheet in frame**, including all four edges.
- **Is there glare across the rings?**

**Do not invent the thresholds.** The project has 37 Phase 0 images with measured registration residuals and recorded verdicts, in `scans/phase0/measurements/photos.json`, plus whatever the range session adds. **Fit the thresholds to that set and state the false-accept and false-reject rates**, the same way every other number in this project has to earn its place. A capture assistant that says "good" on a frame that then fails the gate is worse than no assistant, because the person has packed up and gone home.

**Tell the user what to change, not that something is wrong.** "Move left" and "step back" and "the top right corner is cut off" are actions. "Registration residual high" is not.

### 3. The refusal, and the thing it must never do

**The assistant advises. It does not block the shutter.** Somebody standing in the rain at a range they drove two hours to reach must be able to take the picture anyway. A frame captured against advice is captured, marked as such, and analysed like any other.

**And when a frame is analysed later and fails, the record must say whether the assistant warned at the time.** That is how we find out whether the thresholds are any good, and it costs one field.

### 4. Several frames, which is the phone's real advantage

A person holding a phone can take five frames from slightly different positions in two seconds. A scanner cannot, and a person with a camera on a tripod will not.

Two levels, and the first is nearly free:

1. **Pick the best.** Capture a short burst, register each, keep the one with the lowest residual. This requires no new mathematics and would likely have turned some of Phase 0's flat failures into passes on its own.
2. **Use them together.** Several views of the same curved sheet constrain the surface far better than one, because a single view confounds the surface with the lens. **This is the most promising idea in this entry and the least proven**, and it goes directly at the mounted gate, which is the gate the project cannot currently pass at all.

**Level 2 is a spike, not a plan.** It needs proving on real frames before anybody builds a product around it. Level 1 should be done regardless.

### 5. Owning the camera removes a whole class of defect, and this one matters now

The corpus has repeatedly been damaged by not knowing what the camera did. Entry 16 found that a cropped ultrawide keeps its physical focal length and changes only the 35 mm equivalent, so grouping by the physical value alone mixed two pixel geometries and crashed a joint fit. Entry 27 found that `DigitalZoomRatio` has to join the lens grouping key, and that its absence means unknown rather than 1.0. Entry 37 found the first submission unusable because of what the phone had done to the frames.

**Every one of those is a consequence of receiving a photograph rather than taking one.** A capture app owns the camera, and can therefore:

- **refuse digital zoom outright**, which deletes entry 27's problem rather than working around it
- **record the true focal length, the physical camera and the sensor crop**, rather than inferring them
- **lock exposure and focus** across a burst, so frames in one burst are comparable
- **write a provenance block it authored**, rather than parsing one a phone wrote

**And here is the part that is not Phase 6.** A phone the app controls will, over many sessions, accumulate frames from the *same physical camera*. That is a per-device lens prior, and it makes every individual frame better conditioned than a one-frame fit can be. **The desktop can start collecting that now**, from the donated corpus and from Alan's own frames, keyed on the grouping key entries 16, 27 and 48 have between them established: make, model, lens model, physical focal length, 35 mm equivalent and digital zoom ratio. If a per-device prior measurably improves the fit on the frames we already have, that is a Phase 1 finding that arrives years before the phone does, and it is worth testing as soon as there is a spare afternoon.

### 6. What runs on the phone and what does not

**On the phone:** capture, the live assistant, registration, hole detection, and a verdict with its figures. The verdict has to be on the device, because a person at a range wants to know whether to shoot the sheet again before they take it down.

**Not on the phone:** marking by hand, assignment correction, load comparison, the analysis screen. Those need a pointer and a large display, and entry 43 is written for a desktop. **A phone that tries to be an editor will be a bad editor**, and `DESIGN.md` section 13 is explicit that a bad editor is worse than a mediocre detector.

**The handoff is the marking file, not the photograph.** It is small, it is already the project's interchange format, and it means the phone and the desktop disagree about nothing.

### 7. The imaging backend, and a licence constraint that is already decided

`IImagingBackend` exists and the OpenCV backend lives behind it, which was the right call and pays off here. **The mobile backend is a second implementation of that interface, not a port of the first**, because the OpenCvSharp runtime packages do not cover Android or iOS.

`DESIGN.md` section 20 already settled the licensing: the printed marker is AprilTag `tag36h11` specifically so the mobile detector can be **the AprilTag reference implementation under BSD-2-Clause**, through P/Invoke. Emgu.CV is plain GPL-3.0 with no app-store additional permission, and a downstream distributor cannot add one to somebody else's code, so shipping GroupLab plus Emgu.CV through the App Store would reproduce exactly the conflict the fiducial choice exists to avoid.

**That decision is made and this entry does not reopen it.** What it does add: whatever else the mobile backend needs, thresholding, contours, homography, has to be checked against the same constraint one library at a time, and the answer has to be recorded in `THIRD-PARTY-NOTICES.md` before the dependency is taken rather than after.

### 8. Privacy, where the default is the whole design

A phone application that photographs targets is a camera app that knows where you are. The project has already spent two days removing coordinates from its own history.

- **Nothing leaves the device by default.** No account, no upload, no telemetry, no cloud. Synchronisation is Phase 7 and is a separate, explicit choice.
- **The scrubber's rule from entry 48 applies at capture**: keep what describes the camera and the exposure, drop what describes where, when, who, or anything typed. On a device we control, the location is never written in the first place, which is better than removing it.
- **Donating a frame is a deliberate act with its own consent**, the same as the upload page, never a setting somebody leaves on.
- **The diagnostics rules of entry 41 apply unchanged**, and a phone makes them sharper: a crash report from a phone must carry no photograph, no location and no path.

### 9. Sequencing, and the one thing that is genuinely blocked

`DESIGN.md` section 21 puts Android at Phase 6 and iOS at Phase 8, built on GitHub Actions macOS runners, which are free for public repositories.

**Android first, and the reason is not technical.** iOS distribution needs the GPL section 7 additional permission, which is drafted, is not in force, and is with an attorney. **Until that comes back, iOS is blocked on a legal answer rather than on engineering**, and no amount of work moves it. Android has no equivalent barrier.

Nothing here should start before the desktop passes the photograph gates. **A capture assistant is worthless if the analysis behind it cannot measure a photograph correctly**, and today it cannot.

### 10. The gate

Two, and the second is the one that matters.

1. **The same 0.005 in worst bull**, on frames the phone captured, flat and mounted reported separately, as everywhere else.
2. **The fraction of frames a person who has not read anything captures successfully**, measured on people who are not Alan and not me. **That is the product, and it is the only number that says whether the capture assistant works.** A phone build that passes gate 1 and fails gate 2 has moved the problem rather than solved it.

### 11. What I am confident about, and what needs a spike first

Stated plainly, because the rest of this entry reads more certain than it is.

**Confident:**

- the reframing in section 1, which follows from measurements the project already has
- section 5's list of defects that owning the camera removes, every one of which is a defect the corpus actually suffered
- section 6's split, which follows from `DESIGN.md` section 13
- section 8, which is policy rather than engineering
- section 9's ordering, since the iOS blocker is a fact

**Needs proving before anybody plans around it:**

- **Avalonia's maturity on Android**, particularly camera preview and native interop. I have not checked, and this entry should not be read as saying it is fine.
- **AprilTag detection fast enough on a phone for a live preview.** Plausible, unmeasured.
- **Multi-frame surface fitting**, section 4 level 2. The most promising idea here and the least supported.
- **Whether the assistant's thresholds separate pass from fail at all.** Fit them to the 37 Phase 0 frames first. If they do not separate on data we already have, the feature does not work and that is cheap to find out.

**Do the last one first.** It needs no phone, no Avalonia and no camera: it is an afternoon with `photos.json` and it can kill or confirm the central idea of this entry before anybody writes a line of mobile code.

### 12. What I am not specifying

The interface itself, because it should be drawn after the assistant's thresholds exist and not before. Offline target printing from a phone. Chronograph pairing. Anything about synchronisation, which is Phase 7. And the iOS specifics, which wait on the attorney.

---

## 2026-09-15, entry 53: adjust to zero, which is the project's argument applied to the thing everybody actually does

**Status: open, and deferred to Phase 5 as the entry says.** Every closed-form figure in sections 3 and 4 checked against the formulas and reproduced: the multipliers on sigma, the median and 95 percent misses, every cell of the shots-needed table, and the 0.74 in three-shot miss. Section 9's coverage tests are written with the feature.

Entry 21's remaining scope, half of it. Not for this week: this is Phase 5 work and nothing about it is urgent. It is written now because it has been owed since entry 21 and because the numbers in section 3 are worth having in front of us before the range session rather than after.

Every figure below is closed form, and every one was checked against a 200,000 replication simulation before it was written down. The method is in section 9 so you can reproduce it rather than trust it.

### 1. Why this deserves more care than it looks like it needs

Zeroing is the single most common thing anybody does with a paper target. The universal ritual is three shots, measure the offset, turn the turrets, declare it zeroed. Every target program on the market will do that arithmetic. **None of them will tell you that on a half-MOA rifle a three-shot zero leaves you off by up to 0.74 inches at 100 yards, 95 percent of the time.**

That number is the whole reason GroupLab should have this feature. The arithmetic is trivial and everyone has it. The uncertainty is not, nobody has it, and it is the part that changes what a person should do.

**And it is the strongest argument the 25-bull sheet has.** Section 4 works it out: zeroing a half-MOA rifle to within a quarter MOA, at 95 percent confidence, takes 24 shots. A GroupLab sheet gives you 25 on one page, pooled into one centre. The target design pays for itself on the most ordinary task in shooting, and that case has never been made anywhere in the project's documents.

### 2. The arithmetic, which is the easy part

**Input:** the group centre relative to the point of aim, in linear units at the distance shot; the shot distance; the turret's click value; and the angular convention in force.

1. **Linear to angular**, using the actual shot distance. True MOA is 1.047 in per 100 yd; IPHY is 1 in per 100 yd; a mil is 3.6 in per 100 yd. `DESIGN.md` section 20 makes true MOA the default with IPHY available, and both must be labelled, never silently swapped.
2. **Angular to clicks**, by the stated click value: quarter MOA, eighth MOA, tenth mil, twentieth mil.
3. **Express it in the turret's own words**, not signed numbers. A group low and left gives **"UP 3, RIGHT 2"**. Nobody has ever stood at a bench and thought in negative y.
4. **Round to whole clicks and state the residual**, because a turret has no half positions. "UP 3, RIGHT 2, leaving 0.04 MOA low" is honest; silently rounding is not.

**Two things this is not, and the panel should say so.**

- **It is not a ballistic correction.** It puts the point of impact on the point of aim at the distance you shot. If you shot at 200 and want a 100 yard zero, that is the solver's job, and the solver is Phase 5. Until it exists the panel says which distance the correction applies to and stops.
- **It is not a click-value check.** See section 6.

### 3. The honest part: when is a correction worth making at all

The measured centre is an estimate from `n` shots. Its standard error on each axis is `sigma / sqrt(n)`, where `sigma` is the per-axis standard deviation, which is the same Rayleigh sigma the panel already leads with.

**The rule: adjust an axis only when its confidence interval excludes zero.** If the interval spans zero, the honest statement is that the rifle is not measurably off centre on that axis, and turning the turret is turning noise into the rifle.

**Degrees of freedom, and this connects to machinery that already exists.** For a circular group, sigma is estimated from all `2n` coordinates, so `df = 2n - 2`. GroupLab already runs a Pitman-Morgan test for circularity, so use it: **if circularity is not rejected, pool and use `df = 2n - 2`. If it is rejected, estimate each axis separately with `df = n - 1`** and accept the wider interval, because a group that strings vertically genuinely knows less about its vertical centre.

**The smallest offset distinguishable from zero at 95 percent, in units of sigma:**

| Shots | df | multiplier on sigma |
|---|---|---|
| 3 | 4 | **1.603** |
| 5 | 8 | 1.031 |
| 10 | 18 | 0.664 |
| 20 | 38 | 0.453 |
| 25 | 48 | **0.402** |
| 50 | 98 | 0.281 |

Worked for a half-MOA rifle at 100 yards, which is a good rifle:

| Shots | detectable offset | at 100 yd | in quarter-MOA clicks |
|---|---|---|---|
| 3 | 0.80 MOA | 0.84 in | **3.2** |
| 5 | 0.52 MOA | 0.54 in | 2.1 |
| 10 | 0.33 MOA | 0.35 in | 1.3 |
| 25 | 0.20 MOA | 0.21 in | 0.8 |

**Read the first row again.** Off three shots, on a rifle that shoots half MOA, an offset smaller than about three clicks cannot be told from zero. The correction people most often make after a three-shot group is smaller than that.

### 4. Where the zero actually lands afterwards, which is what a person wants to know

Adjust by the measured offset and the remaining error is the sampling error of the centre. Radially that is Rayleigh with parameter `sigma / sqrt(n)`, so the 95th percentile is `2.448 * sigma / sqrt(n)`.

| Shots | median miss | 95% miss |
|---|---|---|
| 3 | 0.680 sigma | **1.413 sigma** |
| 5 | 0.527 | 1.095 |
| 10 | 0.372 | 0.774 |
| 25 | 0.235 | **0.490** |
| 50 | 0.167 | 0.346 |

**And the table that should be on the screen**, shots needed to put the zero within a target radius 95 percent of the time, `n >= (2.448 * sigma / target)^2`:

| Rifle sigma | within 0.5 MOA | within 0.25 MOA | within 0.1 MOA |
|---|---|---|---|
| 0.25 MOA | 2 | 6 | 38 |
| **0.50 MOA** | 6 | **24** | 150 |
| 0.75 MOA | 14 | 54 | 338 |
| 1.00 MOA | 24 | 96 | 600 |

This is the same shape as the compare-loads table in entry 43 section 6, and it should be built from the same code. **The project's one repeated argument is that the number of shots decides what you are allowed to conclude, and this is that argument applied to zeroing.**

Add the click quantisation to the residual: rounding to whole clicks leaves a per-axis error uniform on plus or minus half a click, variance `c^2 / 12`. On a quarter-MOA turret that is small beside the sampling error at any realistic `n`, and on a coarse turret at high `n` it starts to dominate. Say which is dominating, because the answer tells the shooter whether more shots or a finer turret is the thing that would help.

### 5. What the panel says

- **When the interval excludes zero on an axis:** the correction in the turret's words, with its own interval. "UP 3 clicks, somewhere between 2 and 5."
- **When it does not:** "Windage is not measurably off centre on 5 shots. Leave it alone." Not a correction of zero, which reads like a measurement. A sentence.
- **Always:** where the zero will be afterwards, and the shots-needed row for this rifle.
- **Never** a correction without an interval. This is entry 24, entry 39 section 1 and entry 40 for the fifth time: no bare number on a screen.

**Refuse, rather than compute, when:**

- there is no point of aim, or shots are unassigned on a multi-bull sheet, per entry 39 section 1
- the shot distance is not set, because the angular conversion needs it
- the click value is not set
- fewer than 3 shots, where sigma has a single degree of freedom and the interval is meaningless. Say what is missing, in the place the figure would have been.

### 6. Turret tracking, which is nearly free and nobody offers it

Once somebody adjusts and shoots again, GroupLab has both centres and knows how many clicks were dialled. **Measured movement per click falls straight out**, with an interval, from data the user produced anyway.

A turret that moves 0.22 MOA per advertised quarter MOA click is 12 percent slow, which at distance is a miss nobody can explain. It is the sort of defect that gets blamed on the ammunition for years.

Two cautions, both mandatory. **It needs enough shots in both groups to say anything**, and the same interval logic applies to the difference of two centres, so the standard error is `sigma * sqrt(1/n1 + 1/n2)`. And **it must never be offered from two three-shot groups**, where the interval on the ratio will span most of the plausible range and the answer will be noise wearing a decimal point.

### 7. One thing this specification cannot promise

Every interval above assumes the shot coordinates are exact. Entry 52 section 4 is the reason to say so here rather than quietly: the pipeline has at least two places where a continuous input change produces a discontinuous output change, and until those are measured the coordinates have an uncertainty that none of these intervals include.

**It does not invalidate any of this**, because measurement instability is far smaller than shot dispersion on any group worth zeroing from. But the panel should not claim more than it has, and when entry 52's experiments produce a number, this feature is one of the places it belongs.

### 8. What I am not specifying

Holdover and dial-to-distance tables, which need the solver. Multiple zeros, or a zero that is deliberately offset. Canted-reticle correction. First and second focal plane differences, which do not affect the arithmetic but do affect what a reticle measurement means, and reticle measurement is not an input here.

### 9. How to check my numbers, because they are mine and I have been wrong this week

Every figure was verified by simulation before it was written, at 200,000 replications with the group drawn from a circular bivariate normal:

- **The interval is honest.** With the true offset at zero, the rule says "adjust" 4.91, 5.00, 5.10, 4.93 and 5.00 percent of the time at n = 3, 5, 10, 20 and 25. It should be 5.
- **The critical values are right.** The empirical 95th percentile of `|xbar| / (sigma_hat / sqrt(n))` came out 2.767, 2.307, 2.099 and 2.001 at n = 3, 5, 10 and 25, against `t(2n-2, 0.975)` of 2.776, 2.306, 2.101 and 2.011.
- **The residual table is right.** Simulated 95th percentile radial miss: 1.410, 1.097, 0.776, 0.548, 0.489 and 0.345 sigma, against the closed form's 1.413, 1.095, 0.774, 0.547, 0.490 and 0.346.

**Reproduce these as tests rather than taking them from this entry.** A coverage test at n = 5 and n = 25 that fails if the false-adjust rate leaves 4.5 to 5.5 percent is worth more than any assertion in this document, and it is the same pattern as the interval coverage tests the statistics engine already has.

### 10. One line for the README, when this ships

The 24-shots figure in section 4 is the clearest case the target design has ever had, and it currently appears nowhere. When this feature exists, the README's problem statement should carry it: **zeroing a half-MOA rifle to within a quarter MOA takes about 24 shots, and a GroupLab sheet gives you 25 of them on one page.**

---

## 2026-09-15, entry 52: question 15 answered, and the thing underneath it

**Status: actioned 2026-09-15.** Section 5 item 4's macOS rerun from Windows' corners is done, under entry 49 section 2.
- **Sections 1 and 2:** the sort is committed with every record regenerated, and no verdict changed. `docs/PHASE1-RESULTS.md` "Entry 52" has the before-and-after tables, headed by your sentence.
- **Two further instabilities show in them,** beyond RANSAC's consensus. `ultrawide1`'s surface fit converges to a focal length of 2053 px instead of 1940 from a slightly different start. Two correlation readings change, `ultrawide1` from structured to not distinguishable from random and `main_flat2` the other way.
- **Records already stale:** regenerating under unchanged code first found five, reported separately there so they are not read as the sort's effect.
- **Section 3, the hypothesis holds.** The same correspondences handed to the homography fit in 200 orders gave one result on `main_flat1` and `main_flat2`, and two on `main_flat3`. The mounted frames gave 15 to 70, with the worst scoring bull spanning up to threefold. The instability is the planar model's mismatch with a curved sheet, not the sampler.
- **Section 3's consequence, as you stated it beforehand.** The mounted gate's 0 of 7 never had an error bar. It is robust: no ordering lets a mounted frame pass, and the lowest worst scoring bull across all 1,400 mounted registrations is 0.013 in. Its corner counts, bulls over the gate and worst bulls are not: they vary by more than several of the M1 comparisons were drawn from.
- **The edge fit, entry 49 section 5.** Leaving one point out moves a bull by at most 0.0002 in on bulls of 106 to 900 points. `telephoto3`'s bull 24 has 29 points and did not converge, and one point moves it by up to 0.53 dmm, 0.0021 in, more than the 0.30 dmm move seen on the macOS runner. No point was rejected at convergence on any bull, so the step is in whether a ray yields an edge point at all. The stage record names the failure to converge, and nothing says a bull rests on few points.
- **Section 4:** `docs/STATISTICS.md` section 2 records the measurement's own instability as a known source of uncertainty the intervals do not include, with these numbers, and `DESIGN.md` section 14 points at it.

Reported in `docs/PHASE1-RESULTS.md` "Entry 52" and "Entry 52 sections 3 and 4".

Section 1 is the decision you asked for. Section 2 is how to land it without wrecking the benchmark. Section 3 answers your deeper question, which is the better question. Section 4 is what sections 1 to 3 add up to, and it is the most important paragraph I have written in this log.

Stopping on this rather than committing it was right. It is exactly the case the blocking-question rule exists for.

### 1. Decision: commit the sort. For determinism, and not for the numbers

**Yes, commit it.** The reason is that a measuring instrument must give the same answer for the same input, and today it does not. Marker order carries no information: the same sheet, the same markers, the same corners, presented in a different sequence, is the same measurement. An output that moves with it is wrong in a way that has nothing to do with which output is better.

**Now the part that matters more than the decision.** `ultrawide3.jpg` goes from 42 kept corners to 51, and from 21 scoring bulls over the gate to 18. **That looks like an improvement and it is not one.** It is the same algorithm on the same data with the inputs in a different order. If those numbers land in `PHASE1-RESULTS.md` without a flag on them, then in a month somebody reads 18 against the old 21 and cites it as the surface work improving, and that will be the project citing noise as progress in its own results document.

So the instruction is not "commit the sort". It is: **commit the sort, and make it impossible to mistake the movement for an improvement.**

I want to be explicit about my own reasoning here, because the trap is close. I am not choosing the sort because it improved `ultrawide3`. If sorting had made every mounted frame worse I would still be asking for it, for the same reason. **The moment the justification becomes "it gives better numbers", it is an algorithm chosen after seeing results, which entry 17 section 2 rules out.**

### 2. How to regenerate without destroying the benchmark

The mounted figures are what the surface work is measured against. Changing a benchmark is sometimes necessary and always dangerous, because afterwards nobody can compare across the change unless the change is recorded.

1. **Regenerate every record and every quoted figure**, as you proposed. Half-regenerated is worse than either state.
2. **Put a before-and-after table in `PHASE1-RESULTS.md`** for every frame whose figures moved: kept corners before and after, bulls over gate before and after, worst bull before and after.
3. **Head that table with one sentence in plain words**, something close to: *"These figures moved because the markers are now sorted before use. The sheet, the markers and the corners are identical; only their order changed. The movement measures how unstable the registration is on these frames, not an improvement in it."*
4. **Say it in the commit message too**, not only in the document, because the commit is what somebody bisecting will read.
5. **No gate verdict changes**, which you have already established. Say that explicitly as well, because it is the reassuring half and it is true.

### 3. Your deeper question: yes, and it is more urgent than the sort

You asked whether registration should stop relying on RANSAC's random consensus, which on a curved sheet shifts with an input that should not matter. **Yes, that needs answering, and the sort does not answer it.**

Sorting removes one source of variation. It does not remove the sensitivity. The same fragility is still there and will move the answer for any other reason the input order or the random draw changes: one more marker found, one corner refined a fraction differently, a different platform, a different seed.

**Here is what I think is happening, offered as a hypothesis for you to test rather than a conclusion.** RANSAC finds consensus for a model. On a flat sheet a homography is very nearly the right model, the inliers are unambiguous, and the consensus is stable. **On a mounted sheet the sheet is curved, so a planar model cannot fit it, there is no clean inlier set, and RANSAC returns whichever subset happened to look best on the draws it tried.** That would explain why the mounted photographs are exactly the frames that swing, and it would mean the instability is a symptom of model mismatch rather than a RANSAC tuning problem.

**The experiment that settles it, and it is cheap.** Run registration on the mounted frames K times, say 200, with the marker order shuffled differently each time, and report the distribution rather than a value:

- kept corners: minimum, median, maximum
- scoring bulls over gate: minimum, median, maximum
- worst bull: minimum, median, maximum

Do the same for three flat frames as a control. **If the flat frames give a single value and the mounted ones give a spread, the hypothesis holds and the fix is about the model, not the sampler.**

**And the result has a consequence that I want stated before you run it, so it cannot be read as convenient afterwards.** If `ultrawide3`'s kept-corner count ranges over anything like 42 to 51 across orderings, then **the mounted gate's 0 of 7 has never had an error bar, and some part of it is sampling noise rather than measurement.** That does not make the mounted gate pass. It means the number the surface work has been measured against for a week has an uncertainty nobody has quantified, and any conclusion drawn from a change smaller than that spread was not supported. I would rather know that than not.

**Do the same experiment for the edge fit of entry 49 section 5 at the same time.** One point of thirty moving a bull 0.30 dmm is the same shape of problem: leave each point out in turn, report the spread. Two experiments, one method, and they share the tooling.

### 4. What these add up to, which is bigger than either

**The pipeline has at least two places where a continuous change in the input produces a discontinuous change in the output.** RANSAC's consensus set, found by reordering markers. The edge fit's kept-point count, found on `telephoto3` with identical corners. Both were found by accident, while chasing something else. **Neither was found by looking for them, which is the reason to assume there are more.**

Now the part that matters, and it goes to what this project is for.

GroupLab's entire argument is that a measurement should come with an honest statement of its uncertainty, and that software which prints a confident number it cannot support is doing harm. Every interval the application prints today describes **shot-to-shot dispersion**: how much the rifle and the shooter scatter. **None of them describes measurement instability: how much the answer would move if the measurement were repeated.**

If a bull's position can jump 0.30 dmm because one edge point of thirty fell the other side of a threshold, then the measurement has an uncertainty that no interval on the screen accounts for. **A 94.7 percent interval computed from perfectly measured coordinates is not a 94.7 percent interval when the coordinates themselves can move.** The project would be doing, in a smaller way, the thing it exists to criticise.

**I am not asking for that to be solved now, and it must not turn into a rewrite before the range weekend.** I am asking for it to be written down, in `DESIGN.md` section 14 or `STATISTICS.md`, as a known and unquantified source of uncertainty that the intervals do not currently include. Naming it costs a paragraph. Leaving it unnamed is how a project ends up believing its own error bars.

The two experiments in section 3 are the first measurement of it.

### 5. Order

1. **The sort, with section 2's regeneration and its before-and-after table.** It unblocks the macOS rerun, which has been waiting on it.
2. **The two spread experiments**, section 3. Edge fit and marker ordering, one method.
3. **The paragraph in section 4**, written once the experiments give it a number to cite, or written without one if they take longer than expected.
4. Then the macOS rerun from Windows' corners, and the rest of the gate record.

**Entry 43 stays where entry 50 put it.** Nothing here changes that, and section 4 is a reason to be glad the interface is not the thing being worked on this week.

---

## 2026-09-15, entry 51: the top to bottom check, and what it found

**Status: actioned 2026-09-15.** Section 6, all five:
1. `DESIGN.md` section 20 says .NET 10.
2. `docs/PHASE0-SPIKE-BRIEF.md` cites section 19.
3. Entry 36's status says entry 38 closed `DFdistr`.
4. Entry 31's status says section 3 was done under entry 33.
5. The paths in entries 41, 42 and 43 carry `src/`.

Sections 1 to 5 and 7 are noted. This is a report rather than a set of instructions, except section 6, which is the short list of things to fix. Alan asked for the project checked end to end after nine days and fifty entries. This is that check, and what it did not cover.

### 1. The result, in one line

**The project is in better shape than I predicted, and the one real defect was on the public front page rather than in the code.**

I wrote down in advance what I expected to find, so that finding nothing would be informative rather than reassuring: at least one status that overstated what was done, documentation drift in more places than I had listed, an uncomfortable gates table, and no privacy failure. **Three of those four were wrong.** There is no status that overstates. The gates table is honest and already says so in the documents. The privacy chain is clean and in two places stricter than I specified. Only the documentation drift was as bad as expected, and worse in one place.

### 2. What was checked, and what it found

| | Checked | Result |
|---|---|---|
| **A0** entries 39 and 40 restored from memory | diffed against my originals | **identical**, line for line |
| **A1** paths referenced by entries 22 to 51 | 81 references against the pushed tree | **no dangling reference.** 5 cosmetic, listed in section 6 |
| **A1** status lines claiming a report | 16 claims against `PHASE1-RESULTS.md` headings | **all 16 exist** |
| **A2** the named entry claims | 8 entries verified individually | **all pass**, two exceed what the entry asked |
| **B1, B2** GPS in published history | unauthenticated download, EXIF parsed properly | **zero GPS tags**, both repositories, both phase-0 branches |
| **B3, B4** opt-out by both signals and by hash | `Intake.cs` and its tests | **passes, and stricter than asked** |
| **B5** the two permanently held files | published tree | **absent** |
| **B6** diagnostics privacy | `DiagnosticsTests`, `ReportPackageTests` | **passes emphatically** |
| **B7** dangerous copies off-repository | `C:\Dev` listing, Alan checked the rest | **clean** |
| **B, unplanned** does scrubbing move a pixel | decoded both copies and compared arrays | **identical, max difference 0** |
| **C** the gates | both results documents | **honest**, see section 4 |
| **D** documentation drift | nine checks | **three findings**, section 6 |
| **E** loose ends on disk | published tree, branches, ignored paths | **clean** |

### 3. The three things worth remembering from it

**The scrub moves no pixel, and that is now measured.** A published owner photograph is 3.9 MB against an 8.1 MB original, which does not look like metadata. It is: the original carries 72 EXIF tags including 11 GPS tags and a Motion Photo video embedded at offset 4,179,647, and the published copy carries 12 tags, no GPS and no video. Same dimensions, maximum pixel difference zero, identical pixel-array hash. **That is the project's most load-bearing claim and it had never had a number behind it.**

**The consent rules are working, and the consequence is that the public corpus has no donated photographs in it.** `donated/` holds one submission and a single provenance record. The first submission was unusable, one of the three later ones opted out, and the other two each had their only usable frame withheld by the cross-submission hash rule. **The entire donated corpus is gated on one question to one contributor**, which is worth knowing because it is one message rather than a pipeline problem.

**Both wrong status lines understate.** Entry 36 still carves out `DFdistr`, which entry 38 fixed and I measured as correct. Entry 31 still carves out section 3, which is `ReadmeTests`, present with four guards and green on three platforms. I expected the opposite error and did not find it.

### 4. The gates, which are honest and mostly not passed

| Gate | Result |
|---|---|
| Conformance test 43 | pass, worst 0.00026 in against 0.001 |
| Paper, scanned sheets | pass, ten of ten, worst 0.00325 in |
| Print-scale detection | pass |
| End-to-end synthetic truth | pass |
| Photograph, flat | **fail**, three of three |
| Photograph, mounted | **fail**, zero of seven |
| Phase 0 record off Windows | **first measured today**, see entries 48 and 49 |

**The documents already said all of this before I looked**, in those words, including that reproducing off Windows was "not yet claimed" and that passing the suite on three platforms is "agreement within tolerance, not the byte-identical comparison". I went looking for a gate recorded as passing that had never been measured and there is not one.

**What the table hides, and it is the important thing: no gate has ever seen paper with holes in it.** The paper gate passes on scanned, unshot sheets. Everything above is the project checking its own arithmetic. The range session is the first time it finds out whether it works.

### 5. Where my own method failed, because that is the useful part

Four times in this check my measurement was the thing that was wrong.

- **A significant-digit counter returned zero** on values that plainly had seventeen, from a bad substitution.
- **A numeric-string detector flagged `gate.section = "15.3"`** as a number stored as text.
- **A GPS byte-scan reported a GPS pointer in all eight photographs.** It was counting the byte pair `88 25` anywhere in a 1.7 MB JPEG, where it occurs five to twenty times by chance. Parsing the actual EXIF segment gave zero. **That one would have been an expensive false alarm.**
- **A citation sweep reported 59 references and missed 26**, because the pattern did not allow for the backtick in `` `DESIGN.md` section 19``. The real count was 85, and re-running it is what found the one genuine error.

Every one was caught by reading the output instead of the verdict. **A check that quietly covers two thirds of its corpus is worse than no check**, because it produces a clean result you believe. That is the habit worth keeping from this exercise, more than any individual finding.

### 6. The fixes, which are small

Everything here is documentation. Nothing in the code needs changing as a result of this check.

1. **`DESIGN.md` section 20 says ".NET 9 with Avalonia".** `Directory.Build.props` says `net10.0`. One line. The README's version is guarded by `ReadmeTests` and stayed right; `DESIGN.md` has no guard and drifted.
2. **`docs/PHASE0-SPIKE-BRIEF.md` cites `DESIGN.md` section 18** for "wants every stage to emit a structured record". That is section 19, the `[r3]` paragraph. Section 18 is Storage and synchronisation. This is the second wrong citation of section 18 in the corpus; entry 39's was the first and entry 41 corrected it.
3. **Entry 36's status line still says "except `DFdistr`".** Entry 38 closed it. Amend the status to say so rather than leaving a closed exception standing.
4. **Entry 31's status line still says "except section 3, which waits for the fresh repository".** `ReadmeTests` exists and passes. Amend it.
5. **Five path references in entries 41, 42 and 43 omit the `src/` prefix**: `GroupLab.App/Theme/Tokens.cs`, `AppStyles.cs`, `Marks.cs`, `GroupLab.Core/Trace/StageRecord.cs` and `GroupLab.Core/Analysis/SheetAnalysis.cs`. Mine, cosmetic, and the same class of error as the section number above: a pointer that does not resolve. Fix them where you are already editing those entries' statuses.

The README fixes are entry 49 section 4 and are not repeated here.

### 7. What this check did not cover

Stated so that nobody mistakes it for complete.

- **Entries 1 to 21 were checked lightly**, by confirming their artefacts exist rather than re-deriving them. They were gated by Phase 0 results measured at the time.
- **Nothing was built or run by me.** Every claim needing a compiler or a test run was taken from your reports and from CI, not independently reproduced.
- **The statistics suite passing from the fixtures alone** is the one gate row I could not fill. The fixtures themselves I did verify: 600 keys, 580 numerics, zero disagreement between CSV and JSON, 441 values at full 17 digits.
- **Anything after `dfe9f40`** is outside this check. You were mid-run when it was written.
- **Whether the marks are legible** is a human question and was answered by eye on one photograph, not measured.

---

## 2026-09-15, entry 50: the README is part of done, and when the analysis screen starts

**Status: actioned 2026-09-15.**
- **Section 1:** the rule is in `CONTRIBUTING.md` under Conventions, with the checklist of claim-bearing sections, and the platform claim is guarded.
- **Section 2:** entry 43 starts when the gate record workflow is green on all three platforms, entry 49 section 5 is measured and reported, and the range session has been through the pipeline. Recorded in `docs/PHASE1-RESULTS.md`.
- **Section 3:** noted.

Reported in `docs/PHASE1-RESULTS.md` "Entries 49 and 50". Short. Two standing rules from Alan, written down so they outlive whoever remembers them.

### 1. The README is updated in the same commit as the change that makes it stale

Alan has asked that the README be kept current with each change as it happens, rather than repaired in batches when somebody notices. Entry 49 section 4 is why: a sentence saying the project builds only on Windows sat on the public front page for days after it stopped being true, and it took a person reading the file to find it.

**The rule: a commit that makes a statement in `README.md` false updates that statement in the same commit.** Not the next one, not a documentation pass later. The same commit, so that the repository is never in a state where its front page contradicts it.

**What this is not.** It is not a requirement to touch the README on every commit, and it is not licence to generate it. Entry 31 section 3 still holds: most of that file is argument and judgement, and generated prose reads like it. Most commits change nothing the README claims, and those commits leave it alone.

**The claim-bearing parts, so this is a checklist rather than a judgement call.** These are the places a change is likely to falsify something:

- **Building**, which platforms build and which are offered
- **Status**, what exists and what does not
- **Planned**, the phase table
- **Concept screens**, how far the application is from them
- **Built with**, versions and dependencies
- **Test data**, the `grouplab-testdata` commit pin
- **Repository layout**, when a directory is added or moved
- **Licence**, which stays as it is until the attorney answers

**Put the rule in `CONTRIBUTING.md`** beside the other conventions, as a line in whatever the definition of done is there. A rule that lives only in this log is a rule that lasts as long as this log is being read.

**And guard what can be guarded**, per entry 49 section 4: the platform claim goes between markers and `ReadmeTests` compares it against the matrix in `.github/workflows/ci.yml`. Every claim that can be put between markers and checked against something the repository already knows should be. The rest is judgement, and judgement is what the rule above is for.

### 2. The analysis screen waits for the measurement work, and here is what that means

Alan has decided the interface work goes in after the measurement work is complete and working. **That is the order I recommended and it is now his decision rather than my preference, so entry 43 does not start early for any reason.**

"Complete and working" needs to be checkable rather than a feeling, so:

**Entry 43 starts when all three of these are true.**

1. **The `phase 0 gate record` workflow is green on Windows, Linux and macOS**, under the rule in entry 49 section 1: every verdict identical, every printed table identical, raw differences reported.
2. **The edge fit sensitivity of entry 49 section 5 is understood and reported.** One point of thirty moving a bull 0.30 dmm is a robustness problem that has nothing to do with platforms, and it is the one most likely to bite on real shot paper. Understood means measured and written down; it does not necessarily mean changed, because the right response might be a warning rather than a different fit.
3. **Alan's range session has been through the pipeline**, because that is the first real paper any gate has ever seen, and it is the only thing that can tell us whether the detector works on holes rather than on renderings.

Item 3 is the one that takes a weekend rather than an afternoon, and it is the one that matters. **Everything before it is the project checking its own arithmetic. Item 3 is the project finding out whether it works.**

**In the meantime, entry 43 is not idle.** When the weekend's frames come back they will change what the analysis screen has to show: how a marginal fit is flagged, what a partly-identified sheet looks like, what happens when a bull has two holes in it. A specification written before that data would have to be rewritten after it. **Waiting is not a delay here; it is the cheaper order.**

### 3. What is not waiting

The styling work of entry 42 is done and does not pause. If something on the existing screens is wrong, ugly or unreadable, fix it as it comes up rather than parking it behind entry 43. The rule is about not starting a new screen, not about tolerating a bad one.

---

## 2026-09-15, entry 49: three decisions, the README is telling visitors something false, and a fragility worth more than the gate that found it

**Status: actioned 2026-09-15.** Section 2's sort is committed under entry 52, and its rerun from Windows' corners is done: refinement is the first of two divergences, not the only one. Section 5's three questions are answered under entry 52 section 3.
- **Section 1, measured at `e03415c`:** the gate record workflow passes on Windows and Linux and fails on macOS, as the rule predicts.
- **Section 1:** the gate record workflow now gates on the printed tables, compared line for line with the Windows tables committed in `scans/phase0/measurements/tables`, and reports the raw records without failing. No per-platform reference records.
- **Section 2, the rerun:** `grouplab spike corners` records what Windows' detector returned and replays it elsewhere. On macOS the three differing paper rows of `refinement` come right, and its sixteen synthetic rows collapse to one, because the synthetic raster is warped by the same native library that detects and macOS builds it differently. One `markers` row still differs with the image byte-identical and the corners Windows', so there is a second divergence below detection. Reported in `docs/PHASE1-RESULTS.md` "Entry 49 section 2".
- **Section 3:** both edits to `tools/scan_analysis/scrub_exif.py`, as written.
- **Section 4:** the three README replacements as written, with the platforms between `<!--platforms-->` markers, and `ReadmeTests` requiring them to equal the matrix in `.github/workflows/ci.yml`. The Planned paragraph that still said "once CI exists" is corrected too.
- **Section 6:** noted.

Reported in `docs/PHASE1-RESULTS.md` "Entries 49 and 50". Sections 1 to 3 answer your three questions. Section 4 is a defect in the README that has been live for days. Section 5 is the finding in your own report that deserves more attention than either gate.

### 1. Decision: Linux goes green, by changing what is compared rather than what is allowed

You offered two ways and refused the second for the right reason. **Take neither.** There is a third and it is better than both.

**Byte-identity of the raw records was never the requirement.** It was a proxy for the requirement, and a stricter one. What entry 32 section 3 actually wants to know is whether another desktop gives the same answers. So state the comparison as that, and state it structurally rather than numerically:

> **The Phase 0 gate record reproduces on a platform when every gate verdict is identical and every printed table is identical. Raw differences below printed precision are reported, not gated.**

**This is not a gate written after seeing the results**, which is what entry 17 section 2 rules out, because it sets no tolerance and names no magnitude. It says which artefact carries the claim. Printed precision is what a person acts on, which is the same argument entry 24 made about the panel: the number somebody reads is the number that has to be right.

Under that rule: **Windows passes. Linux passes. macOS fails**, because two of its study tables differ in print.

**And it still has teeth.** Your `telephoto3.jpg` case moved a bull 0.30 dmm, which is 0.0118 in, more than twice the 0.005 in gate. That shows up in print and it fails. A rule that would have let that through would be worthless, and this one does not.

**Do not commit per-platform reference records.** Three reasons, the first of which is the one that matters:

1. **Two committed records that disagree make the repository assert two truths, with nothing in it saying which is right.** Today Windows is the reference and a difference is a question. With three references, a difference is nobody's problem.
2. It changes the question from "do the platforms agree" to "does each platform still match itself", which is a weaker thing to know.
3. 12 MB per platform, growing with every study added.

**Keep the raw comparison in the workflow as a reported diagnostic.** Print the largest difference per field into the summary as you already do. A future change that moves a number stays visible without failing the run.

**On the emails.** Making Linux green removes half of them. macOS staying red is correct and should stay noisy until it is resolved, because a gate that fails quietly is not a gate. The volume itself has a separate cause worth naming: `main` and `phase-1` are the same commit, so every push runs both workflows twice and sends four notifications for two results. That is Alan's call rather than yours, but it is where the multiplier comes from.

### 2. Decision: yes to both, and the sort is not really about this gate

**Sort the markers by identifier before use. Unconditionally, and not to make a gate green.** An order-dependent result means the same input can produce two answers on one machine. That is a defect on its own terms, and the platform comparison merely found it. This is the same argument as entry 48 section 2 about making an iteration deterministic: a measurement tool that gives the same answer twice is worth having whatever the gate says.

**Yes, rerun macOS from Windows' corners in Windows' order.** It is cheap and it answers a question nothing else answers: whether corner refinement is the only place the platforms diverge, or the first of two. If the records then match, the story is complete. If they still differ, there is a second divergence downstream and we would otherwise have stopped looking.

### 3. Decision: `scrub_exif.py`, and the change is mine to hand you

The script's own docstring says the C# scrubber is authoritative and that the two whitelists must be kept identical, so this is catching up rather than a decision. **Make these two edits and commit them with the rest.**

In `KEEP_EXIF`, add:

```python
    piexif.ExifIFD.LensModel,
```

In the `WHAT IS KEPT` block of the docstring, after the `FNumber` line, add:

```
    LensModel                        which camera in a multi-camera phone took
                                     the frame.  The same 35 mm equivalent can
                                     come from different physical lenses in
                                     different modes, and focal length alone
                                     cannot tell them apart
                                     (NOTES-FROM-PLANNING entry 48)
```

I am handing this over rather than editing the file myself because of entry 44: I create files in the inbox and never write a tracked file. That rule cost a data-loss scare to learn and it is not worth an exception for four lines.

### 4. The README says the project only builds on Windows, and it has been saying it for days

Alan asked whether the README is being kept current. **It is not, and the damage is worse than staleness.** Under `## Building`:

> Windows is the only platform currently buildable: the imaging backend references the Windows-native OpenCV runtime package unconditionally. macOS and Linux need that reference made conditional, which is a known and small fix.

**Every word of that is now false.** Entry 32 conditioned the runtime packages days ago, CI builds and tests on Windows, Linux and macOS at Core 757 and App 35, and a second workflow reruns the Phase 0 gate record on all three. The public front page of the project is telling every visitor it does not run on their machine.

**Why `ReadmeTests` did not catch it.** It guards what is marked: links, the sheet count, the framework, em dashes. This is an unmarked prose claim, and entry 31 section 3 said deliberately not to generate the prose. That was right, and the gap is that **a prose claim contradicting a fact the repository already knows is still a checkable claim.**

**Three replacements. The wording is mine; place it as written or tell me what is wrong with it.**

**a. Replace the whole `## Building` paragraph above with:**

> GroupLab builds and its tests pass on Windows, Linux and macOS, and every push runs the suite on all three. The desktop application is offered as a build for Windows today: before Linux or macOS is offered, the Phase 0 gate record has to reproduce on that platform rather than merely compile, which is tracked in `docs/PHASE1-RESULTS.md`.

**b. Replace the last sentence of `## Concept screens` with:**

> The application today has a marking screen and a print screen. They now carry the palette, the type and the marks these screens are drawn in, and not their layout: there is no navigation rail, no composite plot and no analysis screen yet.

**c. In `## Status`, under "What does not exist yet", remove "the assignment editor" and add to the list of what exists:**

> - a sheet that names its own definition from its printed codes, so no target has to be named by hand
> - an end-to-end `analyze` command, from photograph to report
> - diagnostic logging, crash records and a report package, with no location data in any of them

**And add one guard to `ReadmeTests`:** read the platform matrix out of `.github/workflows/ci.yml`, and fail if the README names fewer platforms as buildable than CI actually runs. It is the same shape as the framework guard and it would have caught this the day it went stale. Put the claim between markers so the test has something to read, the way the sheet count and the framework already are.

### 5. The finding in your report that outranks both gates

Buried in the macOS section:

> `telephoto3.jpg` has identical corners on macOS, yet one bull moved 0.30 dmm because the edge fit kept 29 edge points instead of 30.

**Read that again with the platforms taken out of it.** Identical inputs to the edge fit. One point of thirty included or not. The bull moves 0.0118 in, which is more than twice the gate.

**That is not a platform problem and it will not be fixed by anything in sections 1 or 2.** The same flip can happen between two photographs of the same target, between two frames in one session, or between two runs on one machine if anything upstream moves by a hair. **It is a robustness defect that the platform comparison happened to expose**, and this weekend is about to produce exactly the marginal frames that trigger it: paper with holes in it, shot through printed rings, photographed on a board in daylight.

**Worth understanding before the weekend rather than after.** Three questions, in order:

1. **Why is one point of thirty worth 0.30 dmm?** Either that bull's fit is badly conditioned, or the dropped point is unusually influential. Both are answerable by refitting with each point left out in turn and reporting the spread.
2. **Is it a hard include or exclude where a soft weight belongs?** A threshold that flips a point in or out converts a continuous input into a discontinuous output, which is the mechanism here. Robust weighting instead of rejection would make the output move continuously with the input.
3. **Does the stage record say when a fit is near its threshold?** If a bull is one point away from changing its answer, the person reading the figure should be able to find that out. This is entry 39 section 1 and entry 40 again, from a fourth direction: the pipeline knows something the interface is not using.

**Do not change anything yet.** Measure it, report it, and I will write what it should do. But do it before entry 43, because a marginal-frame sensitivity that moves a bull twice the gate is worth more than any screen.

### 6. Answering Alan's other question honestly, for the record

He also asked whether the interface is being driven toward the concept screens. The honest answer is that entry 42 landed the palette, the type, the density and the marks, and that the layout is entry 43 and has not been started, on my instruction. The window matches the screens in how it looks and not in what is on it: no navigation rail, no three-column analysis screen, no composite plot, and a twenty-button toolbar across two rows where the concept has a 46 pixel top bar.

**That sequencing was and still is right**, because the analysis screen needs the analysis path wired into the window and entry 33 built it as a command. But it is his project and his question, and I have told him plainly where it stands rather than defending the order again.

---

## 2026-09-15, entry 48: my diagnosis was wrong, and the two decisions you asked for

**Status: actioned 2026-09-15.**
- **Section 1:** noted.
- **Section 2, Linux:** explained on your three terms. The S2 corners, corner sets and inlier flags are identical to Windows in every record. The first quantity that differs is S3's homography from `Cv2.FindHomography`, the only computation between those corners and the mapping, and it moves a page point by at most 3.4e-7 dmm on the scans.
- **Section 2, macOS:** localised and not yet explained, and nothing is changed. It diverges one stage earlier, at S2's corner refinement, and a bull on a four-marker photograph moved 0.30 dmm when its edge fit kept one edge point fewer. The workflow stays red on macOS.
- **Section 3:** `LensModel` is kept, and the rule is written into the scrubber. The log's whitelist now reads and records the same camera and exposure facts, and a test holds the two lists equal. The owner corpus is republished at `grouplab-testdata` commit `d35ef99`: 13 of its 26 photographs carry a lens model and changed, and the other 13 are byte-identical. `tools/scan_analysis/scrub_exif.py` says to keep its whitelist identical to the scrubber's and does not keep `LensModel`. It is yours, so it is not edited. `LensModel` is not added to the lens grouping key.
- **Section 4:** `docs/DETECTION-PIPELINE.md` now says the wrong names must stay at zero and every refusal stays.

Reported in `docs/PHASE1-RESULTS.md` "Entry 48". Section 1 is mine to account for. Sections 2 and 3 are the decisions. Section 4 is one number in your report worth more than the rest of it.

### 1. Entry 47's diagnosis was wrong, and the way it was wrong matters more than that it was

You opened the Linux and macOS packages. Both carry the WeChat QR code. The failing tests read no code rather than crashing, and the real cause is that on two synthetic 300 DPI images each QR module is under 5 pixels, too small for those builds to read. Doubling the resolution as a second attempt fixes it, and capping at 8,000 px kept a 600 DPI scan from going from 9.0 to 52.7 seconds.

**Three things went wrong in how I reached the wrong answer, and only the third is about luck.**

1. **I stated a package fact I had not checked.** Entry 47 says official OpenCV releases do not carry contrib modules, in the flat voice of a fact. I could not open the packages, said so two paragraphs later, and let the claim stand anyway. Saying afterwards that you could not verify something is not the same as not asserting it, and a reader takes the assertion.
2. **I had a clue and did not use it.** A missing native symbol throws. Whatever I was looking at produced "exit code 1" with no crash trace in the annotations. I never asked what a missing-symbol failure would have looked like compared with what I was seeing, which is the question that would have killed the hypothesis before I wrote it down. Instead I reasoned from the platform split alone, and the platform split is equally consistent with any behavioural difference between builds, which is what this turned out to be.
3. **I built one mechanism instead of a list.** With a platform split and one suspect commit, the honest output is "the log will name it; here are the two or three mechanisms worth looking at first". I produced one mechanism with supporting detail, which reads as a conclusion no matter how it is labelled.

The instruction to discard it if the log said otherwise did its job, and you did exactly the right thing. **That does not make the entry cost nothing**, because a confident wrong direction is worth negative time even when it is correctly ignored, and if you had been less careful it would have cost you an afternoon removing a detector you needed.

**What I take from it for the rest of this project:** where I cannot reach the evidence, the entry says what would settle it and stops, rather than filling the gap with a mechanism that fits.

The fix you built is better than what I would have specified. Trying double resolution second, rather than switching detectors, keeps the coverage and costs nothing on images that already work, and the 8,000 px cap is the kind of limit that only shows up when somebody actually measures the run time.

### 2. Decision: the gate record. Linux is explained. macOS is not yet

Entry 32 section 3 asks for a byte-identical comparison **or an explained difference**. It never said what explained means, which is my omission, so here is the standard.

**A difference is explained when all three of these hold:**

- **a. Every gate verdict is identical on all three platforms.** Pass is pass and fail is fail, on every frame.
- **b. The computation that diverges is named, down to a stage.** Not "floating point", which is the name of a category rather than a cause. Which stage, which quantity.
- **c. The mechanism is named and bounds the difference**, so that we can say why it cannot grow into a.

**Linux meets this, or is one sentence away from it.** Every printed table is identical and the raw records differ only below printed precision. Name the stage and the mechanism in a sentence and it is explained. **Mark Linux explained and move on.**

**macOS does not meet it, and the gap is real rather than pedantic.** Two measurement studies differing in the third or fourth significant figure is roughly a part in a thousand. That is far above last-bit noise, so something is taking a different path, not just rounding differently. The gate tables being identical is reassuring and it is one sample: if an iterative fit is converging to a slightly different point, then on a marginal frame the same mechanism moves a verdict, and a marginal frame is precisely what the weekend is about to produce.

**So: keep that workflow red on macOS, and do not loosen the gate to make it green.** Entry 17 section 2 already settled the general form of this: a number chosen after seeing the results is not a gate.

**The work, and it should be small.** The stage records exist for exactly this. Run the two diverging studies on all three platforms with the trace on, and find the first stage whose metrics diverge. That localises it to a computation, and the computation will suggest the mechanism. My guess, offered as a guess: an iterative fit, `LevenbergMarquardt` or the surface fit, where a different order of operations changes the convergence path. If that is what it is, **the better answer is to make the iteration deterministic rather than to accept the difference**, with a fixed iteration count, a fixed ordering and a fixed termination test, because a measurement tool that gives the same answer twice is worth having for its own sake, quite apart from this gate.

Report what you find before changing anything. If it turns out to be genuinely below the precision anybody could act on, that is an explanation too, and then macOS is explained on the same terms as Linux.

### 3. Decision: yes, add `LensModel` to the keep list

**Add it.** Four reasons, in order of weight.

1. **It is the same class of information as `Make` and `Model`, which are already kept.** It names a piece of glass, not a person, not a place and not a time. There is no privacy argument against it that does not also argue against the two fields already published.
2. **On a multi-camera phone it is the field that says which camera took the shot.** Entry 27's lesson was that the lens grouping key was missing a dimension, `DigitalZoomRatio`, and that one missing dimension made a submission unusable. The same 35 mm equivalent focal length can come from different physical lenses in different modes, and `LensModel` disambiguates that where focal length alone cannot. **This is the strongest reason and it is a measurement reason, not a completeness reason.**
3. **The corpus is published for work nobody has designed yet.** A lens model string is the first thing an outside researcher would want and the cheapest thing for us to have kept.
4. **Consent is not a barrier.** The contributors agreed to publication with location removed. A camera-technical field is inside what a reasonable person expects when `Make` and `Model` are already there.

**Republish the corpus that is already out, rather than leaving two generations of scrubbing in it.** It is 26 images, the originals are retained in `C:\Dev\grouplab-originals` and `C:\Dev\grouplab-submissions`, and a corpus where some files carry a field and others do not for no reason a reader can see is worse than either choice applied consistently. Doing it now costs an hour. Doing it at two hundred images costs a day and an explanation.

**And write down the principle, so the next field is decided rather than negotiated.** A whitelist that grows one request at a time becomes a blacklist with extra steps. The rule I would state:

> **Keep what describes the camera and the exposure. Drop everything that describes where, when, who, or anything a person typed.**

That keeps make, model, lens, focal length, 35 mm equivalent, f-number, digital zoom, ISO, exposure time and orientation. It drops the GPS block, every timestamp, maker notes, and every free-text field. It gives a straight answer to the next field somebody asks about, and it matches the whitelist in entry 41 section 2 for the diagnostic log, which is the same judgement made about the same data for a different purpose. **Two whitelists that disagree would be a defect; make them agree.**

**One open question rather than a decision:** whether `LensModel` should join the lens grouping key alongside `DigitalZoomRatio`. That is a measurement question, not a judgement, and the corpus can answer it once the field is being kept. Do not add it to the key on my say-so.

### 4. The number in your report that deserves more attention than it got

| Platform | Identified correctly | Wrong |
|---|---|---|
| Windows | 33 of 37 | **0** |
| macOS | 33 of 37 | **0** |
| Linux | 32 of 37 | **0** |

**Zero wrong, on every platform.** The identification either reads the sheet's own codes and names the definition it was printed from, or it declines and asks. It never confidently names the wrong one.

That is the difference between a feature and a hazard. A sheet identified as the wrong definition would scale every measurement on it by a wrong number and produce a page of confident figures that are all wrong, with nothing anywhere saying so, which is the failure mode of entries 24, 39 and 40 arriving by a fourth route. **Four of thirty-seven declining is a small inconvenience. One of thirty-seven being wrong would have been a defect worth stopping for.** Whatever you did to make refusal the default on a damaged frame, two definitions, two tiles or an unknown identifier: that is the part to protect in every future change to it.

Worth a line in `docs/DETECTION-PIPELINE.md` saying so, so that a later contributor tuning the identifier for a better hit rate knows which number is allowed to move.

---

## 2026-09-15, entry 47: build and test is red on Linux and macOS, and I think I know why

**Status: actioned 2026-09-15.**
- **Section 1:** agreed, and the gate record stays red until planning decides. `build and test` is green again on all three platforms at `eddee00`, Core 756 and App 34 on each.
- **Section 2: the diagnosis does not hold, on two kinds of evidence.**
  - **The packages, opened:** `libOpenCvSharpExtern.so` in `OpenCvSharp4.official.runtime.linux-x64` 4.13.0.20260627 and `libOpenCvSharpExtern.dylib` in `OpenCvSharp4.runtime.osx.arm64` both carry the `wechat_qrcode_WeChatQRCode` exports, as the Windows DLL does.
  - **The logs:** the failures read "no code on the sheet could be read", not a missing entry point. On the same runs, the identification tests on three printed scans and a photograph passed on Linux and macOS, which they could not have done if the constructor threw.
  - **The cause:** detection on two synthetic images whose code modules are under five pixels, the clean 300 DPI render of GL-CF25-LTR on Linux and the end-to-end test's rotated render on both.
  - **The fix:** `b315853` tries double resolution second, and a failing identification test now reports what each detector found.
- **Section 3:** no capability fallback, because the module is on all three platforms. The point stands, so coverage is measured per platform. `grouplab identify sweep` runs in the gate record workflow and counts how many of the 37 Phase 0 images name the definition and tile they were printed from. Windows and macOS name 33 and Linux 32, none wrongly on any platform; the one Linux misses is `gl-cf25-ltr-1-600-dpi.png`, which the other two read only at quarter resolution. Double resolution is now skipped past 8000 px, which took the slowest 600 DPI scan from 52.7 s back to 9.0 s and lost no image.
- **Section 4:** the rule is taken. `fd05dac` and `eddee00` went up in the same push as the fix, `b315853`, before this entry arrived and before the fix's CI result. Under the rule the fix would have gone alone. From here, the gate record's red is named as expected in the commit message.
- **Section 5:** gate C2 is written into `docs/PHASE1-RESULTS.md` as a measured result, under the gate table and in "Entry 35 section 6". Entry 46 section 5 is closed.

Reported in `docs/PHASE1-RESULTS.md` "Entry 47". Read this before your next commit. Section 1 separates two failures that look like one. Section 2 is a diagnosis I cannot finish from here and you can confirm in one command. Section 3 is the shape of the fix. Section 4 is a process point that matters more than the bug.

### 1. Two different red lights, and only one of them is a problem

| Commit | build and test | phase 0 gate record |
|---|---|---|
| `48913ff` | green, three platforms | not yet existing |
| `bac369d` | green, three platforms | not yet existing |
| `c622591` | **green, three platforms** | fails ubuntu and macos, windows passes |
| `a97bcb0` | **fails ubuntu and macos**, windows passes | fails ubuntu and macos, windows passes |
| `b0091ad` | **fails ubuntu and macos**, windows passes | fails ubuntu and macos, windows passes |

**The gate record failing is the gate working.** That workflow has never run before, it measures the one thing entry 32 section 3 said was "not yet claimed", and a red result on two platforms is a measurement rather than a defect. Whatever it reports is the first honest answer the project has ever had to that question, and the per-field difference reporting you built into it is exactly right. **Leave that red until it is understood; do not chase it green.**

**`build and test` is a different matter.** It was green on all three platforms at `c622591` and red on two at `a97bcb0`. That is a regression in the suite that has been the project's floor all week, and two further commits have been pushed on top of it.

### 2. The diagnosis, and how to confirm or kill it in one command

`a97bcb0` is the only commit in that range that touches `src/GroupLab.Cli/Imaging/OpenCvSharpBackend.cs`, and what it adds is this:

```csharp
using var locator = new WeChatQRCode("", "", "", "");
```

**`WeChatQRCode` is `opencv_contrib`'s `wechat_qrcode` module, not core OpenCV.** The runtime packages the project references are not the same build on every platform:

- `OpenCvSharp4.runtime.win` is a contrib-bearing build, which is why Windows is green.
- `OpenCvSharp4.official.runtime.linux-x64` is built from the **official** OpenCV releases, and official releases do not carry contrib modules.
- The two `osx` runtime packages are in the same position.

Passing four empty model paths correctly avoids needing the neural network files, but it does not avoid needing the native symbol to exist. If it is absent, the constructor throws at runtime on exactly the two platforms that are red, at exactly the commit that introduced it, leaving the third green. **Every piece of evidence I can reach fits, and I could not reach the last one:** nuget.org is blocked from both of my environments, so I could not open the three packages and list their native libraries. That is the step that turns this from a well-supported hypothesis into a fact, and it is one command on your machine:

```
dotnet test tests/GroupLab.Core.Tests --filter <the QR or identification tests>
```

run on Linux, or simply reading the failing job's log, which you can do and I cannot. **If the failure names a missing entry point, a missing shared library, or a type initializer on `WeChatQRCode`, this entry is right. If it names something else, throw this diagnosis away and tell me**, because then I have reasoned myself into a tidy story and that is worse than having no story.

**This is entry 32 section 1 in a new costume.** That entry found the repository could be built only on Windows because of a Windows-only OpenCV package, and it said in as many words: check the exact package ids on nuget.org rather than taking mine, because these runtime packages differ in what they contain. The same trap, one module deeper.

### 3. The fix, which is not "drop the WeChat locator"

Your own comment in that file says why not: on the Phase 0 scans the plain `QRCodeDetector` missed the codes on the 600 DPI scan, on a tile and on a photograph, and the WeChat locator found all three. **Removing it costs real detection coverage on the platform where it works.** So the fix is not removal.

**Detect the capability once, at runtime, and record the difference.**

1. **Try to construct the locator once and cache the outcome.** If it throws, fall back to the plain detector alone for the life of the process. Catch narrowly and by type, not `Exception`.
2. **Say so where a person will see it.** Identification quietly getting worse on two platforms, with nothing anywhere saying why, is the failure mode entries 39 and 40 were both about: the pipeline knows something the interface is not using. It belongs in the log at `app.start` as a capability line, and in the stage record for identification, and in the message the user gets when identification fails, which should say that the sheet could not be identified and that `--target` will do it.
3. **The tests have to state the expectation per platform rather than skip quietly.** If 29 of 37 images identify with the locator and fewer without it, then the test asserts the number the platform can reach and names the reason. **A test that is silently skipped is a gate that has quietly stopped existing**, which is the thing `docs/PHASE0-RESULTS.md` is careful about everywhere else.
4. **Then record it as a real platform difference** in `docs/DETECTION-PIPELINE.md` and in whatever the README eventually says about platform support. "Works on all three" and "works best on one" are different claims and the project does not get to make the first one.

If it turns out a contrib-bearing runtime package does exist for linux-x64 and osx, that is a better answer than any of the above and it makes points 1 to 4 unnecessary. **Check that first**, and check it by opening the package rather than by reading its description.

### 4. The part that matters more than the bug

`b0091ad` is entry 37 sections 3 to 5. It is good work and it is unrelated to the breakage. **It was committed and pushed onto a suite that was red on two of three platforms.**

The CI workflow's own comment says Linux and macOS "are now required". `DESIGN.md` section 21 says a phase is not complete until its gate passes. A required check that gets committed over becomes an advisory one, and it happens by increments exactly like this one, where each individual commit is defensible and the aggregate is a project that no longer knows whether it builds.

**The rule I would like, and will keep asking for: when a required check goes red, the next commit is the one that makes it green, or a commit that deliberately reverts.** If a red light is expected and understood, as the gate record's is, say so in the commit message so that the exception is a decision rather than a habit.

Nothing needs reverting now. `b0091ad`'s work is wanted and its tests will pass once the underlying cause is fixed.

### 5. Two things worth recording while they are in front of us

- **Gate C2 has been measured for the first time.** Entry 32 section 3 has said "not yet claimed" since it was written, and `docs/PHASE1-RESULTS.md` repeated it. It is now claimed, measured, and failing on two platforms, with per-field differences reported. That is a real milestone even though it is red, and it should be written into `PHASE1-RESULTS.md` as a measured result rather than left as a workflow that exists.
- **Entry 46 section 5 is closed.** Both workflows are on `actions/checkout@v5` and `actions/setup-dotnet@v5`, so the Node.js 20 deprecation is gone.

---

## 2026-09-15, entry 46: two questions off the screenshots, one decision I owe you, and what to do next

**Status: actioned 2026-09-15, in section 4's order.**
- **Sections 0 to 2:** see below.
- **Section 4 items 3 and 4:** entry 35 section 6 and entry 37 sections 3 to 5, reported there.
- **Section 3:** the count line above the figures reads like "3 shots: 1 detected, 1 corrected, 1 placed by hand.", naming only the kinds present, and the old "Placed:" line at the foot of the panel is gone. Each row of the shot list carries its provenance in faint, a new `faint` text style over the palette's faint colour. Nothing on the image changed.
- **Section 5:** both workflows moved to the v5 checkout and setup-dotnet actions when the gate record workflow was written. The artifact upload uses v6, the first version that runs on Node 24.
- **Section 0:** `main` fast-forwarded to `48913ff` and pushed.
- **Section 1, the calibre:** every impact ring was the calibre, not the measured size, and the canvas comment said "true hole diameter" as the entries do. In the close-up all three rings on bull 13 draw at 0.308 in. Shot 2, which reads 0.508 in, was ringed again in alert only 6 screen pixels outside its own ring. The impact ring stays at the calibre, as you offered. The alert ring on a flagged hole is now drawn at the size it reads, 0.508 in on shot 2, so the oversize case is visible on the image at scale, and the screenshot test asserts both diameters.
- **Section 2, a real run logs:** the screenshots come from the headless test, which starts no log. A Debug build run from the repository was driven through UI Automation to open a Phase 0 scan through the real file dialog, then closed. It wrote `out/logs/grouplab-20260915-180437-32256.log` holding `app.start`, `dialog.open`, `dialog.result`, `image.open` by name and hash, and `app.exit code=0`. `out/logs` had not existed before, because no build with logging had been run from the repository.

Reported in `docs/PHASE1-RESULTS.md` "Entry 46". Short. Sections 1 and 2 are questions rather than instructions, and I would rather have the answers than guess. Section 3 settles something entry 42 left open. Section 4 is the order.

Entries 41, 42, 44 and 45 are actioned, CI is green on all three platforms at `f0973ed`, and I have read `docs/PHASE1-RESULTS.md` rather than asking you to repeat yourself. The window now looks like a designed instrument instead of a default one, the marks read on paper, on printed rings and on a dark backer in the donated photograph, and the panel refuses to print a centre from aim when it has no aim. **That is the check that had been open since Friday and it passes.**

### 0. `main` is one commit behind

Alan committed the paper protocol as `48913ff` on `phase-1` and pushed. `main` is still at `f0973ed`. Fast-forward it and push, so the two stay level as they have been all week.

### 1. Is the impact ring drawn at the true hole diameter, or at the calibre?

In `out/screens/marks-closeup-dark.png`, the three impacts on bull 13 are drawn as rings of **the same size**, and shot 2 is the one the panel flags as reading 0.508 in across against a 0.308 in calibre. Entry 42 section 5 says the ring is at the true hole diameter, and entry 43 section 3 repeats it for the composite plot. The stated reason is that the picture should be scale honest, so that two holes marked as one look wrong on the image rather than only in the text.

**I am not calling this a defect from a screenshot.** The check is one number: the drawn diameter of shot 2's ring in inches, against 0.308 and against 0.508.

- If the ring is the **calibre** for every shot, that is a reasonable design and it is not what either entry says, so tell me and I will change the entries rather than have you change the code.
- If it is the **measured size**, then shot 2's ring should be about two thirds again the size of the others and it is not, so something is clamping it.

Either way, **the oversize case should be visible on the image**, because entry 40's whole lesson was that a person looking at the picture had no way to see what the number was telling them.

### 2. Does a real run say "Logging is off: logging has not started"?

That is what the Diagnostics panel reads in both window screenshots. If that is the headless test, where no log directory is wanted, fine and say so. **If that is what a Debug build run from the repository shows, then entry 41 section 4 is not doing the one thing it exists for**, which is that logs appear in `out/logs` without Alan having to do anything, and I would be reading an empty directory the next time he says something misbehaved.

Worth an explicit check rather than an inspection of the code: run the window, open an image, close it, and confirm a file appeared.

### 3. Shot provenance: my decision, since entry 42 dropped it without meaning to

You flagged that entry 42 section 5 gives one impact colour, and that the gold, orange and green coding for automatic, corrected and manual shots is therefore gone from the image. That was my omission rather than your change, and you were right to name it.

**The decision: it does not come back to the image, and it does come back in the list.**

- **Not on the image**, because colour there already carries selected and excluded, and the rule in entry 42 section 2 is that amber means "this one". Five meanings on one ring is how a legend stops being readable, and the image's job is geometry.
- **A provenance column in the shot list**, per shot, in `faint`, so it reads without shouting.
- **A count line above the figures**: something like "12 shots: 9 detected, 2 corrected, 1 placed by hand." That is the thing a person actually wants to know, and it is one line rather than twelve.

**The reason provenance matters at all is worth writing down**, because it is easy to treat as bookkeeping: it is the record of where a human judgement entered a measurement. A group that is nine detected shots and a group that is nine hand-placed ones deserve the same figures and a different amount of confidence, and only the application knows which it is looking at.

### 4. Order

1. **Section 0**, fast-forward `main`. One command.
2. **Sections 1 and 2**, the two answers. Both are quick and both might change what section 3 is worth doing on top of.
3. **Entry 35 section 6**, still the oldest open item: the Phase 0 gate reproduced byte for byte on Linux and macOS, and making `--target` unnecessary by reading the definition identifier off the image. This is also gate C2 of the check I am running, so finishing it turns a "never measured" row into a measured one.
4. **Entry 37 sections 3 to 5**: record what the donated submissions showed, and carry the contributor's stated target dimensions into the provenance as structured fields.
5. **Section 3 of this entry.**

**Entry 43 stays closed this week.** It is Phase 4 and nothing about it has become urgent.

### 5. One piece of housekeeping, not urgent

Every CI job, including the green ones, now carries a GitHub annotation: `actions/checkout@v4` and `actions/setup-dotnet@v4` target Node.js 20, which is deprecated, and are being forced onto Node.js 24. Nothing is broken and nothing needs doing this week. Move both to v5 the next time you are in `.github/workflows` for another reason.

### 6. What is happening on my side, so you do not wait on it

I am working through a top to bottom check of the project: every actioned entry against what is actually in the tree, the privacy chain end to end, the gates, documentation drift, and loose ends on disk. **Findings will arrive as later entries. Do not wait for them and do not go looking for the plan**; it is mine to run, and where it turns up something for you it will turn up as an ordinary entry in the inbox.

---

## 2026-09-15, entry 45: the crash report wire contract, fixed, because the server half now exists

**Status: actioned 2026-09-15, with entry 41.**
- **Sections 1 and 2:** the client builds them exactly. Tests check its entry list against the receiver's patterns and the crash record against the schema.
- **Sections 3 and 4:** it sends one request, and shows the reference or the error verbatim.
- **Section 6 point 1:** `docs/CRASH-REPORTING.md` carries sections 1 to 4 verbatim, with the privacy statement in plain words.
- **Section 6 point 2:** `scripts/Get-TargetSubmissions.ps1` is your version with `-CrashReports`, reviewed and committed.

Reported in `docs/PHASE1-RESULTS.md` "Entries 41 and 45". Depends on entry 41 and should be actioned with it, not before it. This entry exists so that the client you build and the receiver I have written cannot disagree, because the two are being written in different places by different people and that is exactly how wire formats end up mismatched.

The receiver is written and tested. It is at `Claude outputs/crash-report.php`, with its deployment runbook at `Claude outputs/CRASH-REPORT-SERVER.md`, both outside the tracked tree because that directory is ignored. Alan places them when he has twenty minutes. **You do not wait for that**, because entry 41 section 7 already says the client ships with the endpoint blank and the save-to-disk path working.

### 1. The package, which is now a checked contract rather than a description

The receiver refuses any zip containing an entry that is not on this list. The names are matched anchored and case sensitively, and any entry containing a slash, a backslash or `..` is refused outright, so **the package is flat: no directories inside it.**

| Entry | Required | Notes |
|---|---|---|
| `crash-YYYYMMDD-HHmmss-<pid>.json` | when there was a crash | section 2 |
| `grouplab-YYYYMMDD-HHmmss-<pid>.log` | at least one | this run, and the previous run |
| `environment.txt` | yes | the expanded `app.start` block |
| `description.txt` | optional | what the user typed, may be empty |
| `contact.txt` | optional | may be empty |

At most 24 entries, at most 40 MB unpacked, and at most a 100 to 1 compression ratio. The client's own cap is 2 MB for the zip; the server's wall is 5 MB.

**Nothing else is accepted, and that is the privacy guarantee made mechanical.** There is no image type on that list, so a package carrying a photograph is refused by the server even if a future version of the client puts one in by mistake. I tested that case specifically rather than assuming it. If you ever find yourself wanting to add a file to the package, the list in the PHP has to change at the same time, and that friction is the point.

### 2. `crash-*.json`, exact shape

The pull script reads this to print one line per report saying what crashed, so the field names matter. `exceptions` is ordered outermost first.

```json
{
  "schema": 1,
  "created_utc": "2026-09-15T06:42:12.104Z",
  "app":  { "version": "0.1.0+3f9c2a1", "commit": "3f9c2a1", "channel": "debug" },
  "environment": {
    "os": "Windows 10.0.26100", "framework": "net10.0", "renderer": "Direct2D1",
    "culture": "en-US", "display_scale": 1.5
  },
  "last_action": "print.select-target",
  "exceptions": [
    { "type": "System.InvalidOperationException",
      "message": "The control TextBox already has a visual parent.",
      "stack": "   at GroupLab.App.PrintWindow.ShowFields()..." }
  ],
  "stages": []
}
```

`stages` holds the `StageRecord` set when an analysis was in flight and an empty array otherwise, per entry 41 section 5. `last_action` is a short stable identifier rather than prose, so that repeated reports group. **`schema` is 1 and it increments if any of this changes**, because a receiver reading a future format should be able to say so rather than guess.

The example above is not invented. It is the crash you fixed this evening, written in this format, and I used it as the test fixture for the pull script.

### 3. The request

- `POST` to the configured URL, `multipart/form-data`.
- File field name: **`report`**, the zip.
- Text field: **`version`**, the application version string, optional but send it.
- Nothing else is read. No headers are required. No authentication.

### 4. The response

Always JSON, always with an `ok` boolean.

```json
{ "ok": true, "reference": "2026-09-15_1a2b3c4d", "sha256": "6b80184e..." }
```

**Show the reference to the user** so they can quote it. On failure:

```json
{ "ok": false, "error": "That report package contains a file this server does not accept: IMG_1580.jpg" }
```

The `error` string is written to be shown to a person whose application has just crashed, so **display it verbatim rather than mapping status codes to your own wording.** The codes you will see are 400, 405, 413, 415, 422, 429, 500, 503 and 507, and every one of them means keep the file and do not retry automatically. Entry 41 section 7 already says one attempt and no background retry queue; this is the reason it says that.

### 5. What I verified rather than asserted

The receiver was run against nine payloads on a local PHP 8.4 server: a well formed package, one with a photograph added, one with a `../../etc/passwd` entry, one with a subdirectory, one with a `.php` entry, one with a log named outside the pattern, a file that is not a zip at all, a 60 MB zip bomb, and an empty post. The good one stored and returned its reference and hash; the other eight were refused with the right status and a sensible message. The Cloudflare address matcher was tested separately against 19 boundary cases including both ends of every range shape it has to handle.

The pull script's new mode was parsed with the PowerShell 7.4 parser and its new code paths were run: default resolution, override behaviour, the crash verify branch against a real zip and meta, and the summary reader.

**None of that proves the client works.** It proves the far side of the wire is not the thing that will be wrong.

### 6. Two small jobs for you

1. **Write `docs/CRASH-REPORTING.md`** as entry 41 section 7 asks, and put sections 1 to 4 of this entry in it verbatim, plus the privacy statement in plain words. It belongs in the repository because the client is GPL and anybody can read what it sends anyway; writing it down is the difference between a project that can be trusted on this point and one that merely asks to be.
2. **Copy `Claude outputs/Get-TargetSubmissions.ps1` over `scripts/Get-TargetSubmissions.ps1` and commit it.** It is the existing script with a `-CrashReports` switch, the mode block, a verify branch for single-zip items, a summary that prints what crashed, and the sudoers note extended. It parses clean and the changed paths are exercised above. I am not committing it myself, for the reason in entry 44.

---

## 2026-09-15, entry 44: I am what overwrote the notes file, and the mailbox convention changes today

**Status: actioned 2026-09-15.** Entries 41 to 44 are folded into this log from `docs/notes/inbox/`, and their inbox files are deleted. The convention is recorded in "How to use this file" above and in `CONTRIBUTING.md`. `docs/notes/inbox/README.md` is committed, so the directory exists in a clean clone. Do this one first. It is short, it costs about fifteen minutes, and it removes the reason the last three hours contained a data-loss scare.

### 1. Stop looking for what wrote that file

You asked, reasonably, that somebody find out what saved over `docs/NOTES-FROM-PLANNING.md`, in case it does it again. **Do not spend a session on forensics. It was me, and here is the evidence rather than an apology.**

At 05:37:11 UTC on 15 September I took a snapshot of `docs/NOTES-FROM-PLANNING.md`, 229,605 bytes, holding entries 1 to 40. I prepended three new entries to that snapshot in memory and wrote the whole 273,172-byte result back over the file some minutes later, while you were in the middle of editing the same file to mark entries 39 and 40 actioned. **Two processes were rewriting one file with no locking and no merge, and one of them was working from a snapshot that was already stale when it was taken.**

I cannot prove from here that my write is the specific event you found at 23:56 local, because what you describe recovering held only entries 1 to 22 and what I wrote held 1 to 43. So one of two things happened: either there was a second writer as well, or the sequence was more tangled than either of us can reconstruct without the reflog. **What is not in doubt is that my write was unsafe, that it would have destroyed your status edits regardless, and that the recovery work you did was caused by me.** You did the right thing at every step of it, including refusing to overwrite the damaged file before understanding it.

**Nothing was lost.** The only things I wrote after entry 40 were entries 41, 42 and 43, and all three are in this delivery. There is nothing for you to re-request and nothing for Alan to re-send.

### 2. The new convention, which is the actual fix

The defect is not carelessness on either side. It is that a single 270 kilobyte append-only log is being rewritten in full by two writers who cannot see each other. Being more careful does not fix that; changing the shape does.

**From now on I never modify `docs/NOTES-FROM-PLANNING.md`. I only create new files.**

- New entries arrive as **one file per entry** at `docs/notes/inbox/entry-NN.md`, where `NN` is the entry number. Each file holds exactly one entry, beginning with its `## date, entry NN: title` heading and its `**Status: open.**` line, in the same form as the entries in the log.
- **I create those files and nothing else.** I never open an existing file for writing in that repository again. If I need to correct an entry I have already delivered, I deliver a new entry that corrects it, which is what entries 37 and 41 section 0 already did anyway.
- **You own `docs/NOTES-FROM-PLANNING.md` exclusively.** When you action an entry, fold its file into the top of the log in the existing newest-first order, set its status, and delete the file from the inbox. You are then the only writer that file has ever had, and a collision is not possible.
- If two of my files are waiting, fold them in ascending entry number so the log's order stays right.

**Why this and not a lock or a convention about timing.** A lock needs both sides to honour it and I cannot see your process. A timing convention fails the first time Alan asks me for something while you are running, which is most of the time. Separate paths cannot collide at all, which is a property rather than a promise.

### 3. What to do now

1. Fold `entry-41.md`, `entry-42.md`, `entry-43.md` and this file into `docs/NOTES-FROM-PLANNING.md`, newest last to first so entry 44 ends up at the top, and delete the four inbox files.
2. **Amend the "How to use this file" paragraph at the top of the log** to record the convention: entries arrive in `docs/notes/inbox/`, Claude Code is the only writer of the log itself, and folding an entry in is part of actioning it. Written down, it survives both of us forgetting.
3. Add `docs/notes/inbox/` to the repository with a `README.md` of two sentences saying what it is, so the directory exists in a clean clone and nobody deletes it as junk.
4. Commit that as its own change before starting on entry 41.

### 4. Two things from your report, answered

**Press, drag, release was the right call and it stands.** Alan's words were click, drag, then click to set, and you built press-drag-release instead. Press-drag-release is what every drawing tool on every platform does, it works with a finger without a second tap, and a plain press and release with no movement still places a shot, so the simple case is unharmed. Click-drag-click is a modal interaction: between the two clicks the application is in a state the user cannot see and cannot leave except by clicking. **Keep what you built.** If Alan tries it and dislikes it, he will say so and it is a small change, but do not pre-emptively build the other one.

**The marks you have now are a partial version of entry 42 section 5 and that is fine.** You gave every mark a dark outline, which is the right instinct and the right direction. Entry 42 gives the exact form, a 3 pixel dark halo at 55 percent under a 1.6 pixel coloured core, with the specific colour per mark type. Do not redo the outline work now; entry 42 will replace those values wholesale when it lands, and there is no point painting the same wall twice.

---

## 2026-09-15, entry 43: the analysis screen, written down at last

**Status: open.** This is the expensive half of Alan's question about the concept screens, and it is **Phase 4 work**, behind entry 39, entry 41 and entry 42. Do not start it this week. It is written now because he asked for it and because entry 42 needs to know what it is making room for.

**The single most important instruction in this entry: the window is a view over what `grouplab analyze` already computes.** Entry 33 built the end to end command and `src/GroupLab.Core/Analysis/SheetAnalysis.cs` holds the result. The screen renders that object. **There is no second analysis implementation and no statistic is computed in the UI layer.** If a figure the screen needs is not in the result object, it goes into the result object and the command gains it too, so the command and the window can never disagree. That property is worth more than any layout in this entry.

The four concept screens are in `docs/figures/screens/`. They are the reference and they are accurate, but the text below governs where the two differ.

### 1. The shell the screens share

A 56 pixel navigation rail on the left: wordmark, then Analyse, Library, Sessions, Compare, Rifles, then a spacer, then Settings at the bottom. Icons are 19 pixel line drawings at 1.6 stroke, in `faint`, and `amber` on `#221c12` for the current screen.

A 46 pixel top bar: breadcrumb on the left reading wordmark, then the load, then the date and distance in `dim`; on the right the registration pill, then Show work, Export, and Report as the single primary button.

**The registration pill is the honesty indicator and it is always visible.** "registered, residual 0.0007 in" in teal when the sheet registered; a plain state when it did not; **never absent**. Every number on the screen depends on the scale being right, and the pill is the one place that says whether it is. When the scale came from a hand drawn reference rather than from registration, the pill says that instead, in `amber`, and says what the reference was.

### 2. The analysis screen

Three columns: 300, flexible, 372.

**Left column, top to bottom:**

1. **Sheet.** A thumbnail grid of the bulls, each drawn as a small ring set with its impacts as filled dots. This is a map, not a picture: it is the fastest way to see that the shots landed where you think they did, and to notice that one bull has two holes and another has none. Clicking a bull scrolls the shot list and highlights those shots on the plot.
2. **Load, read from the sheet.** Cartridge, bullet, powder and charge, brass, primer, seating with CBTO, and the definition identifier in `faint`. **The heading says "read from the sheet" and it is literal**: these came off the printed load block that the shooter filled in, per the paper protocol, and the screen should not present typed-in data and read-off data as though they were the same thing. Where a field was typed rather than read, mark it.
3. **Shot list**, filling the rest of the column. Columns: number, x, y, r, and a tag. Mono, 11.5 point, tabular, 3 by 14 padding, signed x and y with an explicit plus so the column aligns. Tags are 9.5 point uppercase: `contested` in amber, `worst` in dim, `excluded` in dim struck through. A flagged row gets a `#1e1a12` background.

**This is the same control as entry 39 section 4, and it must be built once.** The editor needs a shot list, this screen needs a shot list, and they are the same list with the same selection behaviour. If two of them get written, the two will diverge within a month.

**Centre column: the composite plot.** Section 3.

**Right column: the figures.** Section 4.

### 3. The composite plot

This is the most important thing the application draws, because it is the picture of the project's whole argument: every shot from every bull, overlaid on its own bull's centre, making one group out of twenty five one-shot groups.

**What is drawn:**

- **Each shot as a ring at its true hole diameter**, in `impact`, stroke 1.6, with a centre pip. Not a dot. The plot is to scale and the holes are to scale, and a reader should be able to see that two shots overlap.
- **The group centre** as a small teal cross.
- **CEP 50 and CEP 90** as dashed teal circles.
- **Extreme spread** as a dashed amber line between the two shots that define it, with both shot numbers labelled. Naming the two shots is what turns extreme spread from a number into something a person can check.
- **The point of aim** as a cross, distinct in shape from the group centre, never as a circle.
- **A scale bar** in the current linear unit, and axis ticks at round values.

**The plot's extent is a round number of units and is not fitted to the data.** This matters more than it sounds. Auto-fitting makes a half inch group and a three inch group look identical, which is precisely the illusion this project exists to dispel. Pick the extent by rounding up to the next step in a fixed ladder, show it, and let a good group look small.

**Every drawn element appears in the legend beneath the plot, and nothing appears in the plot that is not in the legend.** Four entries in the concept screen, one per element. If a fifth thing gets drawn, the legend grows.

**Interaction.** Hovering a shot highlights the matching list row; hovering a row highlights the shot. Clicking either selects, in amber, and the exclusion control acts on the selection. Excluding a shot redraws the plot and every figure immediately, and **the count in the heading changes with it**, because a group of twenty four that was twenty five needs to say so.

**Export** produces PNG and SVG of the plot alone, at a stated size, with the legend, the scale bar and the load line included, because the exported plot ends up in a forum post with no context around it.

### 4. The figure stack

The order is deliberate and it is an argument, so keep it.

| Figure | What is shown beneath it |
|---|---|
| **Rayleigh sigma**, as the lead, at 29 point on `panel2` | the confidence interval, and the angular equivalent |
| Extreme spread | the two shots that define it, the angular equivalent, and **"no interval, ES has no useful one"** |
| Mean radius | the confidence interval and the standard deviation |
| CEP 90 | CEP 50 and CEP 95 beside it |
| Offset from aim | x and y components and the angular equivalent |
| Group width by height | standard deviation in x and in y |

**Sigma leads because it uses every shot.** Extreme spread sits second because it is the number everybody quotes, and the line underneath it doing the teaching is the most valuable sentence on the screen: it is the only place a reader learns, without being lectured, that the figure they have always used has no confidence interval worth printing.

**Every figure carries its interval, or states why it has none.** This is entry 24 and entry 39 section 1 generalised into a layout rule: **no bare number appears anywhere on this screen.** Where a figure cannot be computed, the row says what is missing instead of printing something, in the place where the number would have been, not in a side panel.

**Every interval carries its real coverage**, "94.7% interval" and not a rounded 95, which entry 40 recorded as already working in the current panel. Do not lose it in the rewrite.

**Below the figures, the notes.** Short paragraphs, 11.5 point, `dim`, with the lead clause in `text` and semibold. Two are known already and both are in the concept screen:

- Circularity: "**Circular within tolerance.** Pitman-Morgan p = 0.476, so there is no evidence of vertical stringing in these 25 shots."
- The flyer note, which entry 40 confirmed is already reasoning correctly in the current panel: "**Shot 22 is not a flyer.** It sits at 2.27 mean radii from centre. At n = 25 the expected worst shot is 2.18, and two thirds of honest groups this size contain one past 2.00."

**A note appears only when it has something to say.** A screen of permanently present, permanently hedged sentences teaches nobody anything. If the group is not circular, the note says so; if it is unremarkable, the note is absent.

**At the bottom, one disclosure row**: "Full CEP table, bivariate fit, comparison". It opens the secondary panel of `DESIGN.md` section 19, and that panel remembers that it was opened.

### 5. Show your work

`DESIGN.md` section 19 already specifies this and `src/GroupLab.Core/Trace/StageRecord.cs` already implements the record. **The screen is a second rendering of records that exist, not a new feature.** The console form the CLI prints and the timeline the window shows come from one object; if they can disagree, it has been built wrong.

**Left column:** the stage list. Header reading "Pipeline, 1787 ms total" with a proportional progress strip beneath it, one segment per stage, width by duration, so the expensive stage is visible at a glance. Then one row per stage: id in mono, name, milliseconds in mono, a status dot in teal, amber or alert, and a one line summary. Selecting a stage shows its record.

**Below it, the artefact panes**, two by two: the expected artwork rendered from the definition, the observed image, the difference residual, and the registration residual with its vector field. Each pane has a caption naming the stage and one figure in italics on the right.

**Right column:** the console form, verbatim, in mono, exactly the text `grouplab analyze --trace` prints, including the indented continuation lines that carry the alternatives and the reasons. Not a prettier version of it. The same text, so that a user reading the screen and a user reading a pasted terminal dump are reading the same thing.

**Two constraints, already written in `DESIGN.md` section 19 and repeated here because they are the ones that get lost:** the trace must never be the only place an error appears, so a failed stage also produces a normal, prominent error with the trace as the detail behind it; and the theatre must not slow the pipeline down, so artefact generation defaults on for one interactive analysis and off for batch.

**Clicking a rejection in a stage record highlights it on the image.** That is the feature that turns this from a log viewer into a tool.

### 6. Compare loads

**Cards**, one per load, each with its heading, shot count and date, a small version of the composite plot at the same fixed scale as every other card, and three figures: sigma with its interval, mean radius with its standard deviation, extreme spread with its angular equivalent. **Same scale across cards is not optional**; two plots at different scales side by side is a lie told by a layout.

**The verdict block**, in prose, leading with the conclusion. When the answer is that the loads cannot be told apart, the heading says so in those words: "These two loads are not distinguishable on this evidence". Then the reasoning: the point difference, the overlap of the intervals, the test and its p value, and the sentence that has to be there, that a non-significant result is not evidence that they are the same.

**The sample size table**, and this is the part of GroupLab that does not exist anywhere else:

| To resolve a difference of | Shots per load | Rounds total | Sessions at 25 a sheet |
|---|---|---|---|

with the row matching the observed difference highlighted. **This table is the project's argument in its most useful form** and it belongs on the screen rather than in a help page. A person who came to find out which load is better leaves knowing why fifty rounds could not have told them, and roughly what would.

**Right column:** an interval comparison, one horizontal bar per load showing the confidence interval with the point estimate marked, on a shared axis, with a sentence beneath it about how much they overlap. Then "Tests run", a plain two column list naming each test and its result: sigma ratio by likelihood ratio, group centre by Hotelling's T squared, circularity by Pitman-Morgan, and the bootstrap method and replicate count. **Name the tests.** A reader who wants to check the work can, and a reader who does not is unharmed by four lines of small text.

Then "What would help more": the honest redirect, when the data supports one. If measured velocity standard deviation accounts for only a fraction of the observed vertical at distance, say so, because it means a charge weight search will not move the thing the shooter is trying to move.

### 7. What this entry deliberately does not specify

The Library, Sessions and Rifles screens. The report generator behind the primary button. The light theme's plot colours, which need checking against printed output rather than deciding here. The mobile layouts.

And one thing worth saying plainly: **the README currently carries six concept screens above the fold with one sentence admitting the application looks nothing like them.** That sentence is accurate and it is doing a great deal of work. Once entry 39's editor and entry 42's styling land, the gap narrows enough that the wording should be revisited, and once this entry lands it should be removed. Until then it stays, because it is true.

### 8. Verification

1. **No statistic is computed in `GroupLab.App`.** A test asserting that the application project references no statistics type directly, or a review that says so explicitly.
2. The window's figures and the `grouplab analyze` output agree, to the digit, on the same input. Drive it from the same fixture the command's tests use.
3. Excluding a shot changes the plot, every figure, every interval and the count, in one redraw.
4. The console text in the show-your-work panel is byte identical to the CLI's, asserted by a test rather than by eye.
5. Screenshots against all four concept screens, sent to me.

---

## 2026-09-15, entry 42: the styling pass, with the concept screens' actual values

**Status: actioned 2026-09-15, with three departures named in the report.**
- **What was built:** the palette, type scale, spacing, control styles and marks, in `src/GroupLab.App/Theme` (`Tokens.cs`, `AppStyles.cs` and `Marks.cs`). IBM Plex is embedded with its licence, and dark, light and follow-system themes are remembered.
- **Contrast:** six text colours fell below 4.5:1, and each was adjusted along its own hue.
- **Tests:** every earlier test passes unchanged. New tests cover contrast, colour literals, the theme choice, and screenshots written to `out/screens` for you.
- **The three departures:**
  - units are not yet set smaller than their figures, because the existing tests read the figure text;
  - the image no longer colour codes provenance, because section 5 gives one impact colour;
  - mark labels are light text on a dark plate, because the mark's colour as text could not be read.

Reported in `docs/PHASE1-RESULTS.md` "Entry 42". Alan asked whether the window will come to look like the concept screens on its own, and the honest answer is no, it will not, because nobody ever wrote the screens down as a specification. This entry is the cheap half of fixing that. Entry 43 is the expensive half.

**This ranks behind entry 39.** Do the crash, the bull assignment and the editor first. This is chrome, it changes no number and no behaviour, and it must not be allowed to delay the editor. **It is its own commit.**

### 1. What this pass is and what it is not

The six concept screens in the README were built as HTML and rendered to PNG. They are not decoration: they encode a palette, a type scale, a density and a layout that were chosen deliberately, and `DESIGN.md` section 19 already states the reasoning behind the identity, the typography, the density split and the four themes. **What was never done is the translation from those screens into values the application can use.** So the application drifted into Avalonia's Fluent defaults with `Brushes.OrangeRed` bolted on, and it looks nothing like the pictures, and that is nobody's fault except the absence of this document.

This pass delivers **the shell: palette, typography, spacing, control styles, and the marks drawn on the image**. It does not deliver the composite plot, the figure stack, the stage timeline or the comparison screen. Those are entry 43.

**The constraint that keeps this honest: after this pass, every existing test still passes unchanged.** `MainWindow.StatisticsText` is already exposed for the tests to read. If a styling change moves a number, a label or a sentence, the tests will say so and the change is wrong. Chrome only.

### 2. The palette, exactly

These are the values from the concept screens, not approximations of them. Dark is the primary and the concept screens are dark. Light is the same structure with the roles swapped.

**Dark:**

| Token | Value | Used for |
|---|---|---|
| `bg` | `#131417` | window background |
| `panel` | `#1a1c20` | top bar, panel surfaces |
| `panel2` | `#212429` | raised surfaces, buttons, the lead figure block |
| `sunk` | `#0f1013` | the navigation rail, the image and plot area |
| `line` | `#2c3037` | panel separators, one device pixel |
| `line2` | `#3a3f47` | control borders, the breadcrumb separator |
| `text` | `#e6e8ea` | primary text |
| `dim` | `#9aa1a9` | secondary text, figure names, legends |
| `faint` | `#697079` | section labels, units, row numbers |
| `amber` | `#e0912f` | the one accent: selection, primary button, highlighted shot |
| `teal` | `#6fbfa8` | confirmations, CEP circles, group centre |
| `alert` | `#e0604a` | errors and warnings |
| `paper` | `#eceae4` | printed paper, in drawn illustrations |
| `bull` | `#0b0c0e` | printed ink, in drawn illustrations |
| `impact` | `#c8442f` | a bullet hole, on the image and in the plot |

**Amber is the only accent colour and it means "this one".** Selection, focus, the current tool, the primary action, the highlighted shot. If a second accent appears for a second purpose, the first one stops meaning anything.

**Light** keeps every role and swaps the ramp: `bg #f4f3f0`, `panel #ffffff`, `panel2 #eceae4`, `sunk #e4e2dd`, `line #d3d0c9`, `line2 #bdb9b0`, `text #1a1c20`, `dim #5a6068`, `faint #868c94`. The three signal colours darken slightly for contrast on light: `amber #b46f16`, `teal #3f8873`, `alert #bf4531`. Check each text pair against a 4.5:1 contrast ratio and adjust the value rather than the role if one falls short.

**Deliver dark, light, and follow system in this pass.** Avalonia's `ActualThemeVariant` gives follow-system for free once the two palettes exist. High contrast is the fourth theme in `DESIGN.md` section 19 and it is later work; do not fake it by bumping the contrast of the dark theme.

**Replace `Brushes.OrangeRed` and `Brushes.DarkOrange`** in `MainWindow.cs` with `alert` and `amber`. Those two raw named colours are the loudest thing on the screen right now and neither is in the design.

### 3. Typography

**Faces.** IBM Plex Sans for chrome, IBM Plex Sans Condensed for the wordmark, IBM Plex Mono for every numeric readout. IBM Plex is licensed OFL-1.1, which is GPL compatible, so bundling it is clean. **Embed the three families as resources in `GroupLab.App`** rather than relying on the system, because the concept screens are only reproducible with them and a missing face silently changes every measurement column. Add IBM Plex and its licence to `THIRD-PARTY-NOTICES.md` in the same commit.

Fallback stacks, for the case where embedding fails: sans falls back to the system UI face; mono falls back to `Cascadia Mono, Consolas, Menlo, monospace`, which is what `MainWindow.cs` already declares.

**Scale.** These are the concept screens' sizes and they map one to one onto Avalonia's device independent pixels.

| Role | Size | Weight | Notes |
|---|---|---|---|
| Section label | 10 | 600 | uppercase, letter spacing 0.09em, colour `faint` |
| List header | 10 | 600 | uppercase, letter spacing 0.06em, colour `faint` |
| Table row | 11.5 | 400 | mono, tabular figures |
| Secondary text, notes, legends | 11.5 | 400 | colour `dim`, line height 1.55 |
| Body, labels, buttons | 12 to 13 | 400 to 500 | 13 is the base |
| Wordmark | 13 | 700 | condensed, letter spacing 0.1em |
| Figure value | 21 | 500 | mono, letter spacing -0.01em |
| Lead figure value | 29 | 500 | mono, colour `text` |

**Every numeric readout is mono with tabular figures.** This is already half true in `MainWindow.cs` and it needs to be all true, including the shot list from entry 39 section 4. Set the `tnum` font feature where Avalonia exposes it; IBM Plex Mono is tabular by default, so the mono readouts are safe either way, and the feature matters only if a proportional face is ever used for numbers, which it should not be.

**Units are set in the surrounding text size and the `dim` colour, not in the figure size.** "0.1046 in" is a 21 point number followed by a 13 point unit. A full size "in" next to a measurement competes with the digits for no reason.

### 4. Layout and spacing

The concept screens use a 56 pixel navigation rail, a 46 pixel top bar, and a three column work area of 300, flexible, 372. Hold those numbers. The current window has a wrap panel of buttons across the top and a single 380 pixel right panel, which is close to the 372 by accident and nothing else matches.

**Do not build the navigation rail in this pass.** It navigates to screens that do not exist. Leave the current toolbar in place and style it. The rail arrives with entry 43.

**The spacing scale is 4, 6, 8, 12, 14, 20.** Section padding is 12 vertical by 14 horizontal. Panel separators are one device pixel in `line`. Control margins are 2. Row padding is 3 by 14. Nothing gets a value off this scale without a reason written next to it.

**Corner radius is 3 for surfaces and 4 for buttons.** Not 8, not pill shaped. The application is a measuring instrument and it should read like one.

**Buttons.** Default: background `panel2`, border `line2`, text `text`, 12 point at weight 500, padding 6 by 12. Primary: background and border `amber`, text `#17120a`, weight 600. **At most one primary button is visible at a time.** Toggle buttons in the on state take the rail treatment: text `amber` on `#221c12` with a `#3a2d18` inner border.

**Status pills**, for things like the registration result: mono, 11 point, 4 by 9 padding, 3 radius, border `line2`, text `dim`; in the good state, text `teal`, border `#2c463f`, background `#141f1c`.

### 5. The marks drawn on the image, which is the part that is not cosmetic

Entry 39 section 5 said the white impact ring is unreadable on white paper, and set the general rule: **every mark the application draws must be legible on a photograph of a target**, which is mostly white paper with black printing and coloured rings, sometimes on a dark backer, sometimes in hard sunlight with a shadow across a third of it. That is a harder constraint than looking good against the application's own dark chrome, and it is the one that decides these values.

**The answer is a two tone stroke, not a colour choice.** Any single colour loses against something in the corpus. Every mark is drawn as a 3 pixel stroke in `#0b0c0e` at 55 percent opacity, with the mark's own colour stroked at 1.6 pixels on top of it. The dark halo reads on paper and on bright ring colours; the bright core reads on ink and on a dark backer. Stroke widths are in screen pixels and do not scale with zoom, so a mark stays visible at any magnification.

| Mark | Colour | Shape |
|---|---|---|
| Impact, normal | `impact` `#c8442f` | ring at the true hole diameter, plus a one pixel centre pip |
| Impact, selected | `amber` | same, stroke 2 |
| Impact, excluded | `dim`, dashed | same |
| Scale reference | `teal` | line with a filled circle at each end, per entry 39 section 3 |
| Point of aim | `teal` | a cross, not a circle, so it never reads as a shot |
| Bull centre, when shown | `faint` | a small cross |
| Detector rejection | `alert`, dashed | ring |
| Missing marker | `alert` | the existing red cross is right, keep it |

**The impact ring is drawn at the true hole diameter rather than at a fixed screen size.** It is a measurement and it should look like one, and an oversized mark hides the thing it is marking. Entry 40's snapped-to-artwork case is visible immediately when the ring is the size of a bullet, and invisible when every mark is a fixed twelve pixel circle.

### 6. Where this lives in the code

The shell is built in C# with no XAML at all, and that is a reasonable choice for a window this size. Keep it.

- **`src/GroupLab.App/Theme/Tokens.cs`**: one static class, every value in section 2 and 3 as a named member, two palettes, resolved by theme variant. **No colour literal appears anywhere else in the application.** That is the property that makes the light theme possible and the one that quietly fails if it is not enforced from the start.
- **`src/GroupLab.App/Theme/AppStyles.cs`**: the Avalonia `Styles` collection for buttons, toggle buttons, text blocks, panels and pills, added in `App.Initialize()` after `FluentTheme`.
- **`src/GroupLab.App/Theme/Marks.cs`**: the pen and brush definitions from section 5, used by `MarkingCanvas` and later by the plot, so the image and the plot cannot disagree about what a shot looks like.

Add a test that greps the application sources for colour literals outside `Tokens.cs` and fails if it finds any. It is a crude test and it will save the light theme from dying by a thousand hard coded greys.

### 7. Verification

1. Every existing test passes unchanged. If `StatisticsText` moved, revert and find out why.
2. Screenshots of the marking window in dark and light, side by side with `docs/figures/screens/analysis-dark.png` and `analysis-light.png`. They will not match, because the content is different and that is entry 43. **The palette, the type and the density should match.** Send them to me and I will say whether they do.
3. Open `gl-cf25-ltr-1-600-dpi.png`, place an impact on bare paper, one on a printed ring, and one on a printed numeral, and confirm all three marks are readable. Then do the same on one of the donated photographs with the shadow across it.
4. The colour literal test from section 6.

---

## 2026-09-15, entry 41: the application logs nothing at all, and here is the diagnostics specification

**Status: actioned 2026-09-15, in the order of section 8, one commit a step.**
- **Section 0:** noted. The quotation is `DESIGN.md` section 13, and nothing built on it changes.
- **Sections 3 and 4:** a log per run, written to `out/logs` by a Debug build, rotated to twenty files or 20 MB. It records every dialog, file open and save, detection run, and catch that used to swallow its error.
- **Section 2:** a file is its name and a salted hash, and image facts come through a whitelist. Paths are scrubbed from values, messages and stacks, and tests prove none reaches the log.
- **Section 5:** three handlers, the crash record in entry 45's schema with the stages of a detection in flight, and the next-launch banner.
- **Section 6:** the package, built only from the receiver's whitelist, and the dialog that shows it before saving.
- **Section 7:** the upload behind an empty default address, one attempt, and `docs/CRASH-REPORTING.md`.
- **Not done as written:** the print crash was fixed before logging existed, so there are no log lines from it to paste. A test throws from a click handler instead and checks the record.

Reported in `docs/PHASE1-RESULTS.md` "Entries 41 and 45". Section 0 is a correction to entry 39 that costs you thirty seconds. Section 1 answers a question of fact that Alan asked. Sections 2 to 8 are a specification. **This entry ranks behind entry 39 and entry 35 section 6.** Fix the crash first; build this so that the next crash leaves evidence behind it.

### 0. Correction to entry 39, section 6: the citation is wrong

Entry 39 quoted the editor-before-detector argument and attributed it to `DESIGN.md` section 18. **It is section 13, "Assignment and manual editing", at line 309.** Section 18 is "Storage and synchronisation" and says nothing about editors. The quotation itself is verbatim and correct, and the instruction built on it stands unchanged. Only the section number was wrong, and I would rather correct it than have you go looking in the wrong place.

### 1. The answer to Alan's question is no, nothing, and that is exactly why the print crash told us nothing

He asked whether the application is doing debug logging now. I checked the sources rather than assuming, because I have twice this week stated a fact from one example and been wrong. What I found:

- **`Program.cs` is 1,203 bytes and `Main` is a single line.** The builder chain ends with `.LogToTrace()`. That is Avalonia's own framework logging, written into `System.Diagnostics.Trace`. `OutputType` is `WinExe`, so on Windows there is no console attached, and no trace listener is registered anywhere in the solution. **That output goes nowhere and is discarded.** It is not application logging and it never was.
- **There is no global exception handler.** Nothing in the solution references `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, or Avalonia's dispatcher unhandled exception event.
- **There are eight `catch` blocks in the entire shell.** Two in `MainWindow.cs`, three in `AppSettings.cs`, three in `PrintWindow.cs`. Every one of them is narrow by exception type, which is good practice, and every one of them either sets a status string or silently continues. **Not one of them records anything anywhere.**
- **No logging package is referenced by any project.** `GroupLab.App.csproj` carries `Avalonia.Desktop` and `Avalonia.Themes.Fluent`, and nothing else.

So when Alan selected two targets in the print dialog and the window disappeared, the application had no way to tell anyone what happened, and neither did he. **That is the real cost of having no logging and the project has already paid it once.** It will pay it repeatedly once anyone other than Alan runs this.

One thing does exist and must not be confused with this. `src/GroupLab.Core/Trace/StageRecord.cs` is a structured record of pipeline stages, per `DETECTION-PIPELINE.md` section 6.1, and it is real and populated. **That is an explanation of an analysis, written for the user.** It is not diagnostics, it is not written to disk, and it does not survive a crash. Section 5 joins the two at one seam. They stay separate things with separate purposes.

### 2. The rule that outranks everything else in this entry

**A log file must never contain a GPS coordinate, and must never contain the contents of a photograph's metadata block. A crash package must never contain a photograph.**

I am putting this first, before any design, because the failure mode is not hypothetical and it is not subtle.

`GroupLab.Core/Imaging/ImageMetadata.cs` reads EXIF, and EXIF on a phone photograph routinely carries the latitude and longitude of the place the photograph was taken. Entry 37 section 4 recorded that both iPhone submissions arrived carrying GPS. The single most natural line of code to write when adding logging to an image open is "record what we read from the file", and writing that line puts a contributor's home address, or the location of Alan's range, into a plain text file. Section 6 of this entry then offers to zip that text file up and section 7 offers to upload it to a public web server.

**The project spent two days of rewriting git history to remove exactly those coordinates. Do not reintroduce them through the diagnostics channel.**

The positive form of the rule, which is what you implement:

1. **The logger takes a fixed, enumerated set of image facts and nothing else.** Permitted: pixel width, pixel height, bit depth, channel count, declared DPI and its source, EXIF orientation, camera make, camera model, lens model, focal length, focal length in 35 mm equivalent, f-number, digital zoom ratio, ISO, exposure time, and the count of EXIF tags present. Everything else is excluded, and that includes every tag in the GPS IFD, every maker note, every timestamp, and every free-text field such as `ImageDescription`, `UserComment`, `Artist`, `Copyright` and `XPComment`.
2. **The enumeration is a whitelist in one place**, not a blacklist scattered across call sites, for the same reason `scrub_exif.py` uses a whitelist.
3. **A test asserts it.** Build a synthetic image carrying a GPS block, a maker note and an `Artist` field, run an image open through the logger, and assert that none of those strings and no coordinate appears anywhere in the produced log text. This is the direct sibling of the `PublicationTests` GPS assertion, and it is written the same way: **the test creates the GPS block itself** so that it is testing the logger rather than testing whatever happens to be on disk.
4. **File paths are not logged.** See section 3.

### 3. The log file

**Location.** One directory, chosen per platform by the normal convention, resolved once at startup and reported in the first line of the log itself so nobody has to guess:

| Platform | Directory |
|---|---|
| Windows | `%LOCALAPPDATA%\GroupLab\logs` |
| macOS | `~/Library/Logs/GroupLab` |
| Linux | `$XDG_STATE_HOME/grouplab/logs`, falling back to `~/.local/state/grouplab/logs` |

`GROUPLAB_LOG_DIR` overrides it everywhere. Section 4 uses that.

**One file per run**, named `grouplab-YYYYMMDD-HHmmss-<pid>.log`, UTC in the name so the files sort. A second process starting in the same second gets a distinct name because the pid is in it.

**Format.** Plain text, one event per line, fixed leading columns so it reads as a table in Notepad and parses with a split. After the fixed columns, zero or more `key=value` pairs, values quoted only when they contain a space.

```
2026-09-15T18:42:07.104Z  INFO  app.start      version=0.1.0+3f9c2a1 os="Windows 10.0.26100" framework=net10.0 renderer=Direct2D1 culture=en-US units=inch
2026-09-15T18:42:19.882Z  INFO  image.open     file="gl-cf25-ltr-1-600-dpi.png" pathid=7f3a2c11 w=5100 h=6600 dpi=600 dpisource=png-phys
2026-09-15T18:42:31.507Z  WARN  marking.size   shot=1 measured=0.521in expected=0.338in reason=oversize
2026-09-15T18:43:02.119Z  ERROR print.render   targets=2 ex=System.NullReferenceException at GroupLab.App.PrintWindow.Preview+0x4c
```

Timestamps are UTC with milliseconds, in ISO 8601, always. Local time in a log file that gets mailed to another timezone is a trap.

**Paths are not written.** The `file=` field carries **the file name and extension only, never the directory**, because directories on Windows begin `C:\Users\<the person's actual name>` and frequently continue into folder names that identify people. `pathid=` is the first eight hex digits of a SHA-256 of the full path, salted per run, which lets you see that the same file was opened three times without disclosing where it lives. If you ever genuinely need the directory to diagnose something, ask the user for it in that moment; do not collect it in advance.

**Levels.** `ERROR`, `WARN`, `INFO`, `DEBUG`. `INFO` is the default. `DEBUG` is switched on by a `--verbose` command line flag or a settings checkbox, and the setting is remembered in `AppSettings`.

**Rotation.** Keep the newest twenty files, or 20 MB in total, whichever bites first, and delete the oldest on startup. Never unbounded. A log directory that grows forever is a defect that shows up in a year as a support question about disk space.

**Failure is silent and total.** If the log directory cannot be created or written, the application starts normally with logging disabled and says so nowhere except in the settings screen. **Logging must never be able to prevent the application from running.** Wrap the writer so that an exception inside it cannot propagate.

**Threading.** Writes go through a single background writer with a bounded queue. If the queue fills, drop `DEBUG` first and count the drops, then emit one `WARN` recording the count. A logger that blocks the UI thread is worse than no logger.

**Flush discipline.** `ERROR` flushes immediately. Everything else may buffer, but the buffer flushes at least every two seconds. A crash must not take the last four seconds of context with it, because the last four seconds of context is the whole point.

**What gets logged, at minimum:**

- `app.start`: version including the commit hash, operating system and version, framework, Avalonia rendering backend, display scale, culture, and the resolved unit preferences. This block answers most first questions without anybody asking them.
- `app.exit`: exit code and run duration.
- Every file open and every file save, per the field rules above.
- Every detection and analysis run: a one-line summary, and the whole `StageRecord` set at `DEBUG`. See section 5.
- **Every currently swallowed `catch`.** All eight of them get a line. Today `AppSettings` can fail to read settings and the user is never told and neither are we; that becomes one `WARN`.
- Every dialog opened and its result, because "what was he doing when it died" is the first question every time.
- Unhandled exceptions, per section 5.

### 4. Getting logs to me without Alan doing anything

This is the part Alan asked for specifically and it is nearly free.

**In a `Debug` build, the log directory defaults to `<repository root>/out/logs`** unless `GROUPLAB_LOG_DIR` says otherwise. `out/` is already in `.gitignore`, so nothing leaks into commits, and I can read `C:\Dev\grouplab\out\logs` directly whenever Alan says the application misbehaved. **He pastes nothing.** That is the whole feature.

Resolve the repository root by walking up from the executable's directory looking for `GroupLab.slnx`, and fall back to the platform directory in section 3 if it is not found, so a copied Debug build does not scatter files.

**Release builds never do this.** They use the platform directory only.

Add one line to `CONTRIBUTING.md` saying where the logs are in each case, because the next contributor will ask.

### 5. The crash record

**Install three handlers at startup**, before the window is constructed:

1. `AppDomain.CurrentDomain.UnhandledException`
2. `TaskScheduler.UnobservedTaskException`
3. Avalonia's dispatcher unhandled exception, so that an exception thrown inside a click handler is caught rather than tearing the process down

On any of them, do the smallest possible amount of work, because the process may be moments from dying:

1. Write `ERROR` with the full exception chain, every inner exception, and the full stack, and flush.
2. Write a sibling file `crash-YYYYMMDD-HHmmss-<pid>.json` in the same directory, containing: the exception type, message and stack for every exception in the chain; the same environment block as `app.start`; the name of the last user action; and **the current `StageRecord` set if an analysis was in flight**. That last item is the seam with section 1: the trace already knows the resolved parameters and the decisions taken, and a crash report carrying it is worth ten that do not.
3. Then, and only then, attempt to show a dialog.

**The dialog is best effort and the file is not.** A crashing application often cannot draw. So the reliable path is the other one: **on the next launch, if any `crash-*.json` exists that has not been dealt with, the application says so and offers the same choices.** Build that path first and treat the in-the-moment dialog as a convenience.

Alan's words were that an option should come up asking whether to generate a log package. That is right, and the next-launch prompt is how it actually reaches people, because the in-process one is exactly the thing a hard crash takes with it.

### 6. The log package

A single zip, written wherever the user chooses and offered by default on the desktop, named `grouplab-report-YYYYMMDD-HHmmss.zip`.

**Contents, and this list is exhaustive:**

- `crash-*.json` if there is one.
- The log file for the crashed run.
- The log file for the previous run, which is often where the real cause is.
- `environment.txt`: the `app.start` block, expanded, plus installed .NET version and screen configuration.
- `description.txt` if the user typed one.
- `contact.txt` if the user typed one, which is blank by default and clearly optional.

**Not included, ever:** the image being worked on, any photograph, any EXIF, any GPS, any file path, any marking file, any settings file that contains paths, and anything from the submissions directory. If a future maintainer wants an image attached, that is a separate, explicit, per-file consent, not a checkbox in a crash dialog.

**The consent dialog shows the user what is in the package before it goes anywhere.** Two buttons that matter: "Show me the file", which opens the containing folder, and "Save only". Sending is a third button and it is never the default. Nothing is sent without a click. Nothing is ever sent silently, on a timer, or at startup.

Say in the dialog, in one plain sentence, what the package contains and what it does not: something close to "This report contains error details, your GroupLab log files, and information about your computer. It does not contain your photographs or any location information."

**The zip path works today and needs no server.** Build it first and completely. A user who mails a zip to Alan is already better off than everyone is right now.

### 7. The upload, and the part that is deliberately not being built yet

Alan wants the package to reach his server so we can retrieve them. That splits into a client half and a server half, and **only the client half is yours**.

**The client contract, which you build now:**

- HTTPS `POST`, `multipart/form-data`, one file field, to a URL held in configuration. HTTPS through Cloudflare is fine here; the proxy carries web traffic normally. The SSH problem from the submission-pull script does not apply.
- **The configured URL is empty by default and the send button is hidden when it is empty.** Save-to-disk still works. That means this ships and is useful before the server exists, and it means a fork of GroupLab does not accidentally post to Alan's server.
- Client-side size cap of 2 MB. If the package exceeds it, drop the previous run's log and try again; if it still exceeds, offer save-only and say why.
- One attempt, a short timeout, no background retry queue. **If the upload fails, keep the zip, tell the user exactly where it is, and stop.** A crash reporter that retries in the background is a crash reporter that eventually sends something the user has forgotten about.
- On success, show the reference the server returns so the user can quote it.

**Write down the contract in `docs/CRASH-REPORTING.md`**: the request shape, the response shape, the size cap, the exact contents of the zip, and the privacy statement in plain words. It belongs in the repository because the client is GPL and anyone can read what it sends anyway. Writing it down is the difference between a project that can be trusted on this and one that merely asks to be.

**The server half is mine and Alan's, not yours.** I will write the receiver and he will place it, in one batch with the other server work, rather than piecemeal. Two things about it that affect your side, so you can design against them:

1. **There is no secret.** The endpoint URL ships inside an open-source client, so any token in the client is public the day it is published. The server must therefore assume anyone can post to it: a hard size cap, a rate limit per address, rejection of anything that is not a well formed zip of the expected shape, storage **outside the web root** alongside `target_uploads`, never serving the stored files back, and a switch Alan can flip to stop accepting. Do not invent a token scheme to work around this; it does not work and it creates a false sense of safety.
2. **Retrieval reuses what exists.** Rather than a second script, the crash reports will be pulled by adding a `-CrashReports` switch to `scripts/Get-TargetSubmissions.ps1`, with the same SSH mechanism, the same SHA-256 verification at both ends, and a default local root of `C:\Dev\grouplab-crashreports`. **One command fetches both.** Alan has asked that manual work be batched, and two scripts to run instead of one is exactly the sort of thing that stops getting run.

### 8. Order, and how to know it worked

1. **Section 3, the log file, plus section 4, the dev folder default.** This is the whole of the value for the next week, because the next week is Alan running it alone.
2. **Section 5, the handlers and the crash record**, including the next-launch prompt.
3. **Section 6, the zip and the consent dialog.**
4. **Section 7, the client upload behind an empty default**, and `docs/CRASH-REPORTING.md`.

**Verification, and I want these as tests rather than as assurances:**

- The GPS and metadata test from section 2 point 3.
- A test that asserts no absolute path and no drive letter appears in a log produced by opening a file from a deep directory.
- A test that throws from a click handler and asserts that a `crash-*.json` exists afterwards, is valid JSON, and names the thrown type.
- A test that the rotation policy leaves exactly twenty files after twenty five runs, oldest deleted.
- A test that the package builder produces a zip containing exactly the permitted entries and no others, driven by a list rather than by inspection, so that adding a file to the package without updating the list fails the build.
- **Reproduce the entry 39 print crash with logging in place, before you fix it**, and paste the resulting log lines into your report. That is the first real test of whether this thing does its job, and it costs nothing because you have to reproduce the crash anyway.

---

## 2026-09-15, entry 40: the tap is snapping to printed artwork, and three things are working

**Status: actioned 2026-09-15.**
- **Section 1:** the detector returns its aligned expected artwork. A tap on printed ink is placed where it was tapped, and the status line says so. The size message names both explanations, or only the printed target when the region is mostly artwork.
- **Section 3:** closed with entry 39 section 1.

Reported in `docs/PHASE1-RESULTS.md` "Entries 39 and 40". Amends entry 39, which still stands in full and in its stated order. This is one new finding and one correction to a message.

Alan re-marked the same rendered sheet with a realistic ten-shot group around bull 13, a .338 calibre and 100 yards.

### 1. Two shots snapped to the target, not to a hole

The panel reports, twice, in orange:

> Shot 1 reads 0.521 in across, larger than a single 0.338 in hole should (0.471 in): two holes marked as one?

**The diagnosis in that message is wrong, and the detection behind it is right.**

`gl-cf25-ltr-1-600-dpi.png` is a rendered sheet that has never been shot. **There are no holes in it at all.** Every tap that "snapped to the hole under it" snapped to printed ink, and the two flagged shots sit on bull 13's inner ring, which measures about 0.52 in on that sheet. The size check caught them because 0.521 in is too big for a .338 bullet, and that is the safety net from entry 24 section 5 point 3 working on its first real outing. Good.

But it reached the right conclusion by the wrong route, and it told the user the wrong thing. Two overlapping holes is one explanation for an oversized blob. **Snapping to the target's own artwork is another, and on a GroupLab sheet it is the one we can rule in or out exactly**, because the definition says where every printed ring and dot is.

**Two changes.**

1. **The snap should not land on known artwork.** When a sheet is registered, the renderer knows precisely where the printed rings, dots, numerals and markers are. A tap whose snap target coincides with artwork rather than with a difference from artwork should either not snap at all and place where the user tapped, or snap and say what it snapped to. This is the same architectural point as entry 23 section 4, where the detector had to be confined to the registered sheet: **the pipeline knows things the interaction is not using.**

2. **The message should name both possibilities rather than only the one.** Something closer to: "Shot 1 reads 0.521 in across, larger than one 0.338 in bullet hole (0.471 in). Two holes marked as one, or a tap that snapped to the printed target rather than a hole." When the shot's position coincides with artwork, say the second and not the first.

**Why this matters beyond a rendered sheet.** Entry 25's paper protocol asks Alan to shoot holes on bare paper, through printed ring strokes, and touching printed numerals, on purpose. Those are exactly the cases where a snap can latch onto ink instead of a tear. This weekend will produce the real version of what this screenshot shows by accident.

### 2. Three things worth recording as working

Not everything needs fixing and it is worth the record saying which.

- **The calibre arithmetic is right.** Extreme spread 2.333 in centre to centre, edge to edge 2.671 in, difference 0.338 in, which is exactly one bullet diameter. Entry 24 section 5 point 1 asked for both figures labelled so the number matches whatever the shooter is used to quoting, and that is what the panel does.
- **Every interval carries its real coverage**, "94.7% interval", not a flat 95. That is entry 24 section 1 and entry 23 section 2 landing where they were meant to land: on the screen a person reads.
- **The flyer line reasons correctly and refuses the obvious answer.** "Worst shot at 1.32 mean radii; a group of 10 is expected to put its worst at 1.89, so a shot there is not a flyer by that measure alone." That is the project's whole argument, in one sentence, in front of a user.

### 3. Unchanged from entry 39

Every shot still reads "manual, bull none", so entry 39 section 1 stands exactly as written. In this particular marking it does no harm, because Alan placed a point of aim and the shots cluster around one bull, so the single-aim measurement is the right one. **That is luck rather than design**, and the first person to mark a real 25-bull sheet by hand will get entry 39's wrong answer instead.

---

## 2026-09-15, entry 39: Alan's second session with the window, and the panel is reporting a meaningless number again

**Status: actioned 2026-09-15; section 6's README wording is left until the editor has been used.**
- **Section 2:** the crash was a control shown in two rows. It is fixed, with a test that reproduced it and that selects, toggles, pages and saves every sheet.
- **Section 1:** shots are assigned to their nearest bull when placed, when detection loads bulls, and when moved. Any unassigned shot on a sheet of several scoring bulls withholds the figures, with the reason where they would be, and the rule is in `CONTRIBUTING.md`.
- **Sections 3 to 5:**
  - the scale line is drawn while made, stays while waiting, and its ends drag before and after use;
  - impacts are placed by press, drag and release, and listed as rows that select and exclude;
  - no mark is white, and every mark has a dark outline.

Reported in `docs/PHASE1-RESULTS.md` "Entries 39 and 40". Section 1 is a correctness problem he did not report and probably could not have. Section 2 is a crash. Sections 3 to 6 are interaction work, and they are the current phase rather than a later one. This entry outranks everything except entry 35 section 6.

Alan opened `gl-cf25-ltr-1-600-dpi.png`, ran **Detect on a GroupLab sheet** against the matching definition, and it worked: 34 of 38 markers, registration RMS 0.0022 in, all 25 bulls located, the 4 missing markers crossed in red. That is the primary path working in the window for the first time, and it is worth saying so before the rest of this entry.

### 1. Hand-placed shots are not assigned to bulls, so the composite group silently does not happen

His panel reports **mean radius 2.294 in, sigma 1.830 in, centre from aim 2.770 in right and 1.131 in low**, from 12 shots on a sheet whose bulls sit 1.5 in apart.

**Those numbers are not measuring dispersion. They are measuring the distance between bulls.** He placed one impact near each of twelve different bull centres. The rifle's grouping is not in that figure at all.

`GroupAnalysis` is correct and is not the problem. Line 152 measures each shot from its bull's centre when the shot has a bull, and from the single point of aim otherwise. The Selected shot panel gives the reason in three words: **"Shot 12: manual, bull none"**. Every hand-placed shot on a fully registered sheet came out unassigned, so all twelve were measured from one aim point on bull 1, and the composite premise the entire target design exists to serve quietly did not apply.

**Two things to fix, and the second matters more than the first.**

1. **On a registered sheet, a hand-placed impact should be assigned to a bull automatically**, by the same nearest-bull rule `ShotAssignment` already implements for the detector, with the existing manual override for when it guesses wrong. A person marking a GroupLab sheet by hand should not have to know that assignment is a separate concept.

2. **When the composite does not apply, the panel must say so where the figures are, not in a side panel.** This is the same failure as the two-shot mean radius from entry 24, arriving by a different route: a confident figure, to three decimals, with an interval beside it, that means nothing. The rule from entry 24 generalises and should be written down as such: **whenever the panel cannot compute what the user thinks it is computing, it says what is missing instead of printing a number.** Here that is a line reading something like "12 shots, none assigned to a bull, so these figures measure the spread of your marks rather than the group. Assign them to bulls, or mark them on one bull."

**Why he could not have caught this.** The figure is plausible, the interval is plausible, the honesty line about the reference length is present and reassuring, and nothing anywhere says the shots are unassigned except three words under a heading about the one shot he happened to select.

### 2. The print screen crashes on selecting multiple targets

Selecting more than one target in the print dialog crashes the application. **Reproduce it, fix it, and add a test that selects several.** He is printing his session pack from that screen this weekend and it has been used by a person exactly once.

While you are in there: the print screen is the one piece of this the weekend depends on, so it is worth ten minutes of trying to break it deliberately rather than only fixing the reported path.

### 3. The scale tool shows nothing while it is being used

Two taps and then nothing visible until "use this length" is pressed. **Draw the line as it is being made**, with a circle at each end, and keep it drawn afterwards so the reference stays visible while the rest of the marking happens. A measurement you cannot see is one you cannot check, and this one scales every number on the screen.

**And it should be draggable.** Place by click, then drag either endpoint to adjust, rather than starting again. Same for the rectangle.

### 4. Impacts should be placeable by drag, and listed

His words, and they are right: click, then drag the point to where it belongs, then click to set. Tapping blind and hoping is the wrong interaction for a measurement tool at any zoom level.

**Each shot should also appear as a row in the right-hand panel**, clickable, selecting it on the image, with its number, its bull, and whether it is excluded. At twelve shots the current design already makes finding one specific shot a hunt around the image. At twenty-five it will be worse. The row is also where the flyer and exclusion controls belong, rather than only appearing once something is selected.

### 5. The impact marker must not be white

White circles on white paper. Use something that reads on paper, on black ink and on a dark backer, since all three occur in the corpus. The green fill he has now works; the outer ring does not.

Worth a general rule while you are choosing: **every mark the application draws has to be legible on a photograph of a target**, which is mostly white with black printing and coloured rings. That is a stronger constraint than looking good against the application's own dark chrome.

### 6. The concept screens: what is deferred and what is not

Alan asked whether the difference between the window and the README's concept screens is work set aside for later. **Partly, and the part that is not deferred is the part he is complaining about.**

**Deferred, legitimately.** The composite group plot, the full statistics layout, the stage timeline, the comparison screen, the light theme. Those are the Phase 4 Windows application in `DESIGN.md` section 21, and they need the analysis path wired into the window first, which entry 33 built only as a command.

**Not deferred: the editor.** `DESIGN.md` section 18 says it plainly, and it has said so since revision 1:

> The editor is built before the detector, not after it. A good editor with a mediocre detector is a usable product. A bad editor with a good detector still frustrates users on every target the detector gets wrong, and no detector reaches one hundred percent.

Sections 3, 4 and 5 of this entry are the editor. By the project's own stated order they come before more analysis features, not after. **Treat them as the current milestone rather than as polish**, and do not let the concept screens' absence be used to defer them, because they are a different thing.

**One honest note for the record.** The README puts six concept screens above the fold and says in plain words that they are mockups and the application looks nothing like them. That is accurate but it is doing a lot of work for one sentence, and the gap will widen before it closes. Worth revisiting the wording once the editor work here lands, so the page describes an application somebody could recognise.

### 7. Order

1. Section 2, the crash. It blocks the weekend.
2. Section 1, assignment and the honest panel. It is producing wrong numbers today.
3. Sections 3, 4 and 5, the editor.
4. Then entry 35 section 6 and the rest.

---

## 2026-09-15, entry 38: the DFdistr defect is mine and it is fixed, plus the negative zeros

**Status: actioned 2026-09-15.** Verified independently:
- `DFdistr`'s 9,440 values are 2,360 integers and 7,080 doubles, with no strings. They are bit-identical to the CSV, and none moved by more than 4.44e-16 from the 15-digit version.
- `DFlandy01`'s CSV changed in exactly the three keys named. Its JSON changed in no value, only its generation time, because jsonlite already wrote those zeros as `0`.
- No fixture CSV holds a `-0`, and the 67 statistics tests pass.

Reported in `docs/PHASE1-RESULTS.md` "Entry 38". Small, and it closes entry 36. Files are on disk. Still behind entry 35 section 6.

### 1. You were right, and the cause is exactly what you said

`sg_distr.R` converted its columns to formatted text for the CSV and then built the JSON from the same data frame, so all 8,850 table values came out as quoted strings. In `sg_dump.R` I captured the numeric values into a separate variable before formatting; in `sg_distr.R` I did not, and I did not check the output of the second script the way I checked the first. Finding it by reading the file rather than assuming my script was right is the correct instinct.

**Fixed, and verified rather than asserted:** the JSON now holds 2,360 integers and 7,080 floats and no strings, and all 9,440 values agree with the CSV to the bit.

### 2. The three negative zeros, fixed in both scripts

You found that `DFlandy01` carries three values where the CSV says `-0` and the JSON says `0`. `sprintf("%.17g", -0)` preserves the sign and jsonlite does not.

**Both scripts now normalise negative zero to `0`.** A sign that nothing reads is worth less than the two formats agreeing, and a documented discrepancy between a fixture's two representations is exactly the sort of thing that costs somebody an afternoon in a year's time. The three affected keys are `shots.y.303`, `shots.yPOA.303` and `flignerProbe.FlignerY.input.243`, and nothing else in any of the ten datasets contained one.

### 3. Files on disk

- `tools/shotgroups/sg_distr.R` and `sg_dump.R`, both corrected.
- `shotGroups_DFdistr.csv` and `.json`, regenerated, now numeric.
- `shotGroups_DFlandy01.csv` and `.json`, regenerated. **Exactly three keys changed**, all three `-0` becoming `0`, and I diffed all 33,928 keys to confirm nothing else moved.

No other dataset needs regenerating: I scanned every committed CSV for a bare `-0` value and `DFlandy01` was the only one.

### 4. Your verification answered my question, and the answer is the reassuring one

I asked whether your hand-reconstructed aimed coordinates agreed with the newly stored values. **3,775 of 3,978 matched exactly; the other 203 are all in `DFcm`, differ by at most 3.6e-15, and the stored values are the right ones** because centimetre aim points are not six-decimal numbers and your reconstruction had to round somewhere.

That is the outcome I hoped for and did not assume: the fixture is now the authority and the workaround was a workaround. Removing it was correct.

---

## 2026-09-15, entry 37: I was wrong about DO-NOT-PUBLISH, and the same photographs arrived twice with contradictory consent

**Status: actioned 2026-09-15.** Sections 1 and 2 first; sections 3 to 5 after entry 35 section 6, as entry 46 section 4 orders. Section 3: recorded. Section 4: recorded, with two corrections: the uploads came through Chrome for iOS, not Safari, and the scrubber keeps no `LensModel`. Section 5: `statedSheetSize` in the provenance record, and the rectangle tool offers it. Reported in `docs/PHASE1-RESULTS.md` "Entry 37 sections 3 to 5". Section 1: the `DO-NOT-PUBLISH` check, removed under entry 28, is restored, and either signal withholds. Section 2: `Intake.WithheldHashes` builds the hash set across all submissions, and `Intake.Run` requires it. A matching file is held, with no override by acceptance, and its provenance entry names the withheld submission. On the real submissions, `eac0bae6` is refused on both signals, and `5068047f` and `bf6d885d` each hold their one photograph as a conflict naming `eac0bae6`. Nothing is published to `grouplab-testdata` until Alan has asked the contributor. Reported in `docs/PHASE1-RESULTS.md` "Entry 37". Section 1 corrects an instruction I gave you that was false, and you may have removed working code because of it. Section 2 is a consent conflict sitting in the submissions right now. Both before anything else.

Three submissions arrived after the Discord post was edited. They are the first useful donated data the project has had.

### 1. Correction: the `DO-NOT-PUBLISH` file exists, and entry 28 told you it did not

Entry 28 section 1 said, as a numbered point: *"There is no `DO-NOT-PUBLISH` sentinel file. The opt-out is `exclude_from_public_dataset` ... do not look for a file that the page does not write."*

**That was wrong.** `2026-09-15_eac0bae6` contains a file named `DO-NOT-PUBLISH`, 129 bytes, reading:

```
The contributor asked that these photos are not published.
Testing on a private machine only. Do not add to the public data set.
```

I generalised from the single submission that existed at the time, which had not opted out and therefore had no sentinel. One example, stated as a fact about the schema. If you removed a check for that file on my say-so, **put it back.**

**Both signals must be honoured, and either one alone is sufficient to withhold.** Treat a submission as opted out if `exclude_from_public_dataset` is true **or** a `DO-NOT-PUBLISH` file is present. Do not require agreement between them, and if they disagree, withhold and say so. A belt-and-braces opt-out is the one place where redundancy is the point rather than a smell.

### 2. The same photographs were submitted twice, once publishable and once not

Two files appear in two submissions with **identical SHA-256**:

| SHA-256 (first 16) | In, publishable | In, opted out |
|---|---|---|
| `2fdb872ece4e7088` | `5068047f` / `001_IMG_1580.jpg` | `eac0bae6` / `003_IMG_1580.jpg` |
| `ce5138799f7f23ec` | `bf6d885d` / `001_IMG_1696.jpg` | `eac0bae6` / `004_IMG_1696.jpg` |

Same contributor, same phone, same credit name, within a few minutes. The likely story is that they submitted two carefully with full answers, then uploaded their whole folder for testing and marked that one do not publish.

**The rule, which is not a judgement call: an opt-out wins by content hash, across every submission.** If bytes appear anywhere in an opted-out submission, those bytes are not published from any submission. Implement it as a hash set built across all submissions before anything is published, not as a per-submission check.

**And flag the conflict rather than resolving it silently.** Publishing under ambiguous consent is the one mistake in this pipeline that cannot be undone. Hold both files, record the conflict in the provenance with both submission ids, and let Alan ask the contributor which they meant. Withholding two photographs for a day costs nothing. Publishing one the contributor did not intend costs the project its trustworthiness on the exact point the consent text makes a promise about.

### 3. The post edit worked, and here is the measurement

| | First submission, before the edit | The two after it |
|---|---|---|
| Answers filled | 0 of 6 | **6 of 6, both** |
| Usable frames | 0 of 3 | **2 of 2** |

The answers now carry backing, attachment, distance, calibre, a credit name, and in the notes the exact commercial target model. Two different calibres, 5.56 NATO and 8.6 Blackout, which is the hole-diameter variety the corpus wanted and had none of.

Worth recording in `PHASE1-RESULTS.md` or wherever the corpus is described: the difference between a useless submission and a good one was the wording of the request, not the contributor.

### 4. iOS is answered, passively, as planned

Four uploads from an iPhone XS Max on iOS 18.7 through Safari. The `accept` attribute question from the upload page specification is settled by real use rather than by testing on a borrowed handset.

**And the metadata survives the upload intact**, which was the real risk. 48 EXIF tags including `Make`, `Model`, `LensModel` reading "iPhone XS Max back dual camera 4.25mm f/1.8", focal length, 35 mm equivalent, f-number and orientation. So iPhone submissions are fully usable for the lens and surface work.

**They also carry GPS.** Both of them. The scrubber is doing real work on real contributor data now, not just on Alan's own photographs.

### 5. Both frames meet the brief, and one detail in them is worth building on

Whole target, still stapled to the backer, all four edges in frame, printed concentric rings, genuine deformation from staples and wind, one with hard sunlight and a shadow across the top third, one with a torn corner curling away from the board. This is the case the project has never had.

**The notes field names the exact target: "Action Target PR-BE6 17.5x23" and "Action Target TCT-MK3-MOD2 23x35".** That is a stated overall sheet dimension, from the person who shot it, and it is a scale reference that needs no grid and no measuring.

**Use what the contributor told you rather than building a lookup table.** Carry the stated dimensions into the provenance record as structured fields when they can be parsed, and let the manual marking path offer them as a scale reference: "this sheet is 17.5 by 23 inches, use its edges". That is more honest than a database of third-party target sizes and it improves every time somebody fills the notes in, which section 3 suggests they now will.

### 6. Order

1. Section 1, restore the sentinel check. It is a correctness fix to a mistake I introduced.
2. Section 2, the cross-submission hash rule and the conflict flag.
3. Everything else in this entry can wait behind entry 35 section 6 and entry 36.

---

## 2026-09-15, entry 36: the fixtures are regenerated at full double precision, and my defect is closed

**Status: actioned 2026-09-15; `DFdistr`, excepted here, was closed by entry 38.** Verified on all ten files. The nine datasets have 0 keys added or removed, and no stored number moved by more than 5.53e-16 relative. CSV and JSON agree bit for bit except three negative zeros in `DFlandy01`. The reconstruction is removed, and all 67 statistics tests pass from the fixtures alone. The reconstruction had matched the stored values on 3,775 of 3,978 coordinates. The other 203 are `DFcm`, off by at most 3.6e-15, because its centimetre aims are not six-decimal numbers, so the stored values were right. `STATISTICS.md` section 15.4 item 15 is amended. **Not committed:** `shotGroups_DFdistr.json` stores all 8,850 table values as strings, because `sg_distr.R` formats the columns for the CSV before building the JSON from the same frame. That script and both `DFdistr` files stay uncommitted for you to fix. Reported in `docs/PHASE1-RESULTS.md` "Entry 36". Files are already on disk, unstaged. Your job is to verify and commit, not to regenerate. Lower priority than entry 35 section 6's two remaining items, and it should be its own commit rather than folded into them.

### 1. What was wrong

`sg_dump.R` and `sg_distr.R` wrote through R's defaults: 15 significant digits in the CSV and `digits = 15` in the JSON. For a comparison at 1e-12 that is invisible. For a **rank** statistic it is not: dropping the last bits changes which values are exactly equal, which changes which observations tie, which moves the statistic in the fourth decimal place.

**That is question 14 in one sentence, and it cost you most of a day.** You chased a 1.2e-4 to 4.9e-4 disagreement through tie rules, median definitions, coordinate frames and two implementations, and the cause was the storage format of the fixture. My file, my defect.

### 2. What is fixed

Both scripts now write **17 significant digits**, which is the round-trip precision of an IEEE 754 double: every double formatted `%.17g` and read back yields the identical double. The stored value is now the value.

The two writers needed different fixes and both are in place:

- **CSV**: values formatted with `sprintf("%.17g", v)`, with `NA`, `NaN`, `Inf` and `-Inf` written as those literals, and `quote = FALSE` so the column stays unquoted as before.
- **JSON**: `digits = I(17)`. **Not `digits = NA`**, which the documentation describes as maximum precision but which on jsonlite 2.0.0 still emits 15. I tried `NA` first and caught it because the CSV and JSON then disagreed. The `I()` wrapper means significant digits rather than decimal places, and it is the only setting that works.

### 3. What changed in the data, measured rather than assumed

I diffed the new files against the committed ones across three datasets, 21,598 keys:

| | |
|---|---|
| Keys missing | **0** |
| Keys added | **0** |
| Values whose text changed | 17,000 |
| Values that changed by more than 1e-14 relative | **0** |

**Every difference is digits appearing, not a value moving.** Nothing you have validated has shifted. I also verified that CSV and JSON now agree bit for bit on all 580 numeric values of `DF300BLK`, which is the check that caught the `digits = NA` problem.

### 4. What you need to do

1. **Run the harness.** Every comparison should still pass. If one now fails, that is interesting rather than alarming: it would mean a value your implementation matched against a rounded fixture and does not match against the true one, which is a real defect the old fixtures were hiding.
2. **The Fligner keys are the ones to look at.** You reconstructed those values by hand to work around the precision loss. That workaround should now be unnecessary: the four keys should match straight from the fixture. **Remove the reconstruction rather than leaving it in place**, and confirm the values it produced agree with the ones now stored. If they disagree, tell me, because then one of us is wrong and it matters which.
3. **Update `STATISTICS.md` section 15.4.** It currently records 15-digit precision as a property of the fixtures. That is no longer true, so the entry should say what it was, when it changed, and that question 14's four keys were its only known casualty. Do not delete the note; a defect that was found and fixed is worth more in the record than one that was quietly removed.
4. **Commit as its own change**, and say in the message that no value moved beyond 1e-14.

### 5. Files

Nine datasets plus `DFdistr`, CSV and JSON, already written to `test/fixtures/shotgroups/`. Both scripts updated in `tools/shotgroups/`. Roughly 14 MB, slightly larger than before because the digits are really there now.

---

## 2026-09-15, entry 35: the four decisions, and what the sighter bug proves

**Status: actioned 2026-09-15 for sections 1, 4 and 5. Section 2's README warning and section 3 wait on Alan, because this session's permission check refused both. Section 6 item 3 is actioned: `grouplab analyze` needs no `--target` and the marking screen asks for a definition only when the sheet's codes cannot give one. Section 6 item 2 is measured: Windows reproduces the Phase 0 gate record byte for byte. Linux reproduces every printed table, and its records differ only below the precision any table reports. macOS reproduces the paper and photograph gate tables, and differs in measurements 1 and 2 in the third or fourth significant figure. Neither is byte-identical, so whether the differences count as explained is planning's decision; see `docs/PHASE1-RESULTS.md` "Entry 35 section 6".** Section 1: `CameraOriginal` holds a file with a screenshot or messaging-app name, or no camera make, in both `intake` and `publish-owner`. It flags none of the 26 published owner photographs and none of the donated files. The two photographs are recorded as held permanently at `grouplab-testdata` commit `c80055c`. Section 3: the bundle was verified, and the pack measured at 159.74 MiB, before the refused deletion; nothing was deleted. Section 4: `.gitignore`. Section 5: `CONTRIBUTING.md`. Reported in `docs/PHASE1-RESULTS.md` "Entry 35". Answers your four questions from the entry 33, 32 and 34 report. Section 1 is a decision I am making rather than passing on. Sections 3 and 4 are small and should be done in the next commit.

### 1. Do not publish the two held photographs, and the authorship question does not need answering

`Screenshot_20231029-170033.png` and `signal-2023-07-25-20-37-40-354-1.jpg`. You asked whether Alan took them. **The filenames answer a more useful question first: neither is a camera original.**

One is a screenshot. The other came through Signal, which re-encodes and strips metadata on send. Whatever the underlying photograph was, what we hold is a second-generation copy with no lens information, no orientation tag and recompressed pixels.

**So they fail the corpus's purpose regardless of who took them.** The donated set exists to give the lens and surface work frames whose camera geometry is known, and these two cannot contribute to that. Publishing them would add two files of unknown provenance and no analytical value, under a licence that cannot be revoked.

**Hold them permanently, and record why in the data repository's notes rather than leaving them looking like an open question.** If Alan later confirms he took the originals and still has them, the originals are the thing to publish, not these.

**Generalise it into the intake rule:** a file that is not a camera original is held by default. `Screenshot`, `signal-`, `IMG-\d{8}-WA\d+` and similar are cheap signals, and a missing `Make` tag is a stronger one. That is the same test the Discord post asks contributors to apply, so the tool should apply it too rather than relying on people reading.

### 2. The removal policy as written is right

Saying photographs are removed from the current contents promptly, and that removal from history is decided case by case, is honest and I would not strengthen it. **Do not promise a history rewrite**, because it invalidates every clone anybody has taken and we have just spent a day learning what that costs.

One addition: say in the README that the data repository's history may be rewritten for a removal request, so that anyone building on it knows a rewrite is possible rather than being surprised by one. A warning costs nothing and removes the only reason not to do a rewrite when one is genuinely warranted.

### 3. Delete `refs/original` and garbage collect, now

Those five backup refs are the last copy of the unscrubbed history inside a working repository with a push remote configured. They are one mistaken `--mirror` from publishing the coordinates, and that is a bad thing to leave lying around indefinitely.

**The backup already exists and is verified**: `C:\Dev\grouplab-backup-2026-09-15.bundle`, 158.95 MiB, `git bundle verify` clean, containing all sixteen refs including the originals. That is the copy to keep.

Delete each `refs/original/*` ref, then expire the reflog and garbage collect so the unreachable objects actually leave the pack rather than merely becoming unreferenced. **Report the pack size before and after**, since that is the evidence the objects are gone.

**One thing for Alan rather than you:** that bundle contains his coordinates. It should not sit in a folder that syncs to a cloud drive.

### 4. `scans/mounted/` should be ignored as well as moved

Alan moves the originals out, which is his to do. **Add `scans/mounted/` to `.gitignore` in the same commit as the other work**, so that the folder cannot be committed by accident if it reappears. The `excluded/` lesson from this morning applies exactly: a location that is meant to stay out of the repository should be enforced by a rule, not by everybody remembering.

### 5. What the sighter bug proves, recorded because it will be forgotten

`grouplab analyze` found two bugs on its first run. One crashed hole detection on every Phase 0 sheet. The other **counted shots on sighter bulls as part of the group, and the marking screen had it too.**

That second one is the important one. It does not crash, it does not fail a test, and it produces a plausible number that is wrong. It survived 709 passing tests, a statistics engine validated against 45,476 reference keys, and a detector measured on real scans, because **every stage was correct and the composition was not.** Nobody had ever asked the whole path a question, so nobody had ever seen the wrong answer.

Put a line in `DESIGN.md` section 21 or `CONTRIBUTING.md` to the effect that a stage passing its own tests is not evidence the pipeline is right, and that the synthetic end-to-end test is the one that speaks for the product. It is the most valuable thing learned this week and it is the kind of thing that gets rediscovered expensively.

### 6. Next, in order

1. **Sections 3 and 4 above**, plus section 1's intake rule. Small, and section 3 removes a live risk.
2. **Entry 32 section 3: the Phase 0 gate record reproduced on Linux and macOS.** CI proves the tests pass there, which is not the same claim. Compare the gate record byte for byte against the Windows run and report whether it is identical or explain the difference. Until that is done, neither platform is offered as a build.
3. **`--target` should not stay required.** The whole point of the printed codes is that a sheet describes itself. Reading the identifier or the GLTD-B payload off the image and selecting the definition automatically is the difference between a tool and a demonstration, and it needs no new data.
4. Then wait for the weekend's frames.

---

## 2026-09-15, entry 34: grouplab-testdata exists, and here is what goes in it

**Status: actioned 2026-09-15.** `grouplab-testdata` commit `1544f1d` holds the README, `CONTRIBUTORS.md`, the first donated submission under `donated/2026-09-14_1a8f39ad` (a provenance record only, because triage held all three files), and 26 of the owner's photographs under `owner/` through the new `grouplab publish-owner`. Two are held until Alan confirms taking them: a screenshot and a Signal download. The data test covers everything section 5 lists, and the URL and pinned commit are in the README and `CONTRIBUTING.md`. The originals are still in `scans/mounted/` here, untracked, for Alan to move. Reported in `docs/PHASE1-RESULTS.md` "Entry 34". Unblocks question 13 section 1 and entry 23 section 5. Lower priority than entry 33 section 1, the end-to-end command, which still comes first.

`https://github.com/oRAirwolf/grouplab-testdata` is created: public, GPL-3.0, one commit containing the licence, default branch `main`, no README. **GPL-3.0 is not a choice here and must not be changed**, because it is the licence named in the consent text every contributor agreed to.

### 1. The data repository's README is the important file, not an afterthought

It is the only document a contributor or a researcher will read, and it has to answer, without them having to ask:

- **What this is**: photographs of shot targets, donated, for developing and testing GroupLab.
- **What was done to them**: location metadata removed, pixels untouched, and the hash of both the original and the published file recorded so either can be checked.
- **What was agreed**: the consent text, quoted verbatim, with its version. Not a summary.
- **What is not here**: submissions whose contributor ticked the do-not-publish box. State plainly that such submissions exist, are used for testing only, and are never published. A reader should not have to infer that from silence.
- **How to cite it**, and how a contributor asks for their photographs to be removed. Somebody will, eventually, and an answer written now is calmer than one written then.

### 2. Layout, and one thing that does not fit the submission shape

**One directory per submission**, named as the upload page names it, `YYYY-MM-DD_<id>`, holding the images and a provenance record. That keeps the published tree aligned with what arrives, so a question about any file has one place to look.

**The provenance record carries**: submission id, submitted timestamp, consent version and the consent text verbatim, the six answers as given including empty ones, the original filename, the hash as uploaded and the hash as published, and what the scrubber removed. `grouplab intake` already writes most of this; make sure the consent text itself is in there and not just its version.

**`scans/mounted/` does not fit that shape and should not be forced into it.** Those 23 photographs are Alan's own, taken before the upload page existed, with no submission id and no consent record because none was needed. Give them their own directory, `owner/`, with a provenance record that says what they are, who took them, and that they are published by the copyright holder directly. Inventing a fake submission record for them would be worse than having two shapes.

They still go through the scrubber before they land there, per entry 23 section 5, and Alan keeps the originals outside both repositories.

### 3. Credit the people who asked to be credited

`meta.json` has a `credit_name` field and some contributors will fill it in. **Maintain a `CONTRIBUTORS.md` in the data repository listing those who gave a name**, and say in the README that a name is included only when the contributor supplied one. People who donate their work should be named if they wanted to be, and the field is pointless if nothing reads it.

### 4. Plain git, no LFS, and say the size out loud

Images are added once and never modified, so there is no delta churn and plain git handles this well. LFS would add a requirement on every contributor and a quota question, for a corpus that does not change. **State the current and expected size in the README** so nobody clones half a gigabyte by surprise.

### 5. Wiring it to the code repository

- **Record the URL and a pinned commit** in `grouplab`, somewhere a reader will find it: the README's test-data section already describes this repository, so put it there and in `CONTRIBUTING.md`.
- **`PublicationTests` already looks for a checkout at `../grouplab-testdata` or the path in `GROUPLAB_TESTDATA`**, and does nothing when absent. Leave that behaviour: CI should not need half a gigabyte to run.
- **Add one test that runs when the checkout is present**: every published submission has a provenance record, no image carries a location, no do-not-publish submission is present, and every published file matches its recorded hash. That last one is what makes the hashes worth recording.

### 6. Order

Entry 33 section 1 first. Then this. Nothing here is blocked by the weekend, but the end-to-end command is worth more before Monday than a populated data repository is.

---

## 2026-09-15, entry 33: nothing has ever run end to end, and that is what to do before the weekend

**Status: actioned 2026-09-15, except section 6, which stays blocked as the entry says (entry 34 has since unblocked `grouplab-testdata`).** Section 1: `grouplab analyze` runs the whole path with the stage trace, and `EndToEndTests` gates it on synthetic truth: 28 of 28 shots within 0.15 in, worst centre 0.0047 in, 1.7 s. It found two integration faults, both fixed: an invalid definition crashed hole detection, and sighter shots were pooled into the group. Section 3: `ReadmeTests` and a three-platform workflow. Section 4: the runtime packages conditioned per platform. Section 5: `scripts/` and the paper protocol committed, the branch convention rewritten, and a gate status table at the top of `docs/PHASE1-RESULTS.md`. Reported in `docs/PHASE1-RESULTS.md` "Entry 33".

### 1. The pipeline has never been run as one thing

Registration works. The hole detector works. `ShotAssignment` exists. The statistics engine matches 45,476 reference keys. Every piece is green.

**Nothing joins them.** `src/GroupLab.Cli` has verbs for intake, scrubbing, the library and a dozen spikes, and no verb that takes a photograph of a GroupLab sheet and returns a group. The parts have only ever been exercised separately, by spikes that each build their own inputs.

That is the most dangerous state a project of this shape can be in, because every part reports success and the thing the parts exist for has never been attempted. The failures waiting there are interface failures: a coordinate frame that means something different on each side of a call, a unit assumed in one place and converted in another, an ordering that only matters once two stages are composed. None of them can be found by testing the stages.

**Build `grouplab analyze <image> [--target <definition>]`.** One command, the whole path: load, register, detect inside the registered sheet, assign each hole to its bull, pool the offsets into one group, compute the statistics, emit a report. It should run against the sheets already in `scans/`, which means **it needs no paper, no printer and no range trip.**

**Emit the `StageRecord` trace `DESIGN.md` section 19 already specifies**, so the console form exists before any analysis screen does. That is not extra work bolted on; it is how the spikes have been reporting all along, and it is what makes a wrong answer diagnosable rather than merely wrong.

**Then gate it on synthetic truth, which is exact.** `SyntheticSheet` can already render a sheet, so render one with shots placed at coordinates chosen by the test, run the whole command against the rendered image, and require every recovered shot within the conformance threshold of where it was placed. **That is the first test in this project that measures the thing GroupLab actually does**, rather than a stage of it. If the composed answer disagrees with the truth it was built from, no amount of green stages matters.

Report the outcome honestly, including how long it takes and where it is slow. A first end-to-end run that finds three integration bugs is a successful run.

### 2. Why this specifically, and why before the weekend

Alan shoots the Phase 1 paper protocol this weekend and comes back with mounted sheets, which are the frames the photograph gate has been waiting on since entry 17.

**If the end-to-end path does not exist when those frames arrive, they sit unanalysed while somebody writes it**, and the integration bugs get found while the interesting measurement waits. If it does exist, the frames go in on Monday and produce a number the same day.

There is also a smaller reason worth saying: the application currently cannot analyse a GroupLab sheet at all. It can mark a commercial target by hand, which is the fallback path. The primary path, the one the whole target format and fiducial design exists to serve, has no user-facing route. That gap is invisible from the test counts.

### 3. The README guard and three-platform CI

Entry 31 section 3 and entry 32 section 4 are one job, and the repository now exists to run them against.

- **`ReadmeTests`** per entry 31 section 3: every relative link resolves, every referenced image exists, stated counts match reality with the number between marker comments, the stated framework matches `Directory.Build.props`, and no em dash.
- **A GitHub Actions workflow** on push and pull request, a matrix over `windows-latest`, `ubuntu-latest` and `macos-latest`, running `dotnet build` and `dotnet test`. Windows required; the other two allowed to fail until entry 32 section 1 lands, then required too. Report test counts per platform, because building and agreeing on the numbers are different claims.

### 4. Entry 32 section 1, the one-file defect

The unconditional `OpenCvSharp4.runtime.win` reference. Three lines, and it is the difference between a public repository that a Linux developer can build and one that fails at restore. Do it before the CI matrix, so the matrix has a chance of going green.

### 5. Housekeeping, all small and all currently untracked or stale

- **`scripts/` has never been committed.** `Get-TargetSubmissions.ps1` pulls the donated submissions and is the only copy. It holds no secret: the key path is a parameter and the key lives outside the repository. Commit it.
- **`docs/PHASE1-PAPER-PROTOCOL.md` and its PDF are untracked.** Alan shoots that protocol this weekend. It should be in the repository before it is used, not after.
- **`CONTRIBUTING.md` states a branch convention that no longer matches reality.** `main` and `phase-1` are now identical and both are pushed by hand every time. Either say that `main` is the trunk and `phase-1` is retained until Phase 1 formally closes, or propose retiring `phase-1`. Do not leave a document describing a workflow nobody follows.
- **`docs/PHASE1-RESULTS.md` should say plainly which Phase 1 gates are met and which are not.** The mounted photograph gate is not met and cannot be until the weekend. A results document that does not distinguish "passed" from "not yet measured" is the kind of thing that later gets read as the former.

### 6. Blocked, so that it is clear what is not on this list

- **The mounted photograph gate**, and the backer measurement on a GroupLab sheet. Both need frames that do not exist yet.
- **`grouplab-testdata`.** The repository has to be created by Alan before anything can be wired to it. Once it exists, the work is a pinned commit and URL recorded here, `scans/mounted/` moved across scrubbed, and the publication test pointed at it.
- **Adjust-to-zero and the phone specification**, entry 21's remaining scope. Mine to write, not yours to start.

---

## 2026-09-15, entry 32: macOS and Linux, which is one defect today and one gate later

**Status: actioned 2026-09-15 for sections 1 and 4; sections 2 and 3 are the shape of later work, as the entry says.** Section 1: each OpenCvSharp runtime package is conditioned on its platform, with the ids checked on nuget.org. Section 4: the CI matrix is in `.github/workflows/ci.yml`. Its first run passed on all three platforms, with Core 714 and App 4 on each, so `WinExe` is confirmed harmless and Linux and macOS are now required. Section 3's gate, the Phase 0 gate record reproducing on macOS and Linux, is not yet done. Reported in `docs/PHASE1-RESULTS.md` "Entry 33". Alan has asked for macOS and Linux support on the list. Section 1 is a real defect that exists now. Sections 2 to 4 are the shape of the work, not a request to start it this week. Entry 31 section 3's README guard and CI come first, and section 4 here is part of the same CI job.

### 1. The repository cannot be built anywhere except Windows, today

`src/GroupLab.Cli/GroupLab.Cli.csproj` references `OpenCvSharp4.runtime.win` **unconditionally**. That package carries the Windows native OpenCV binaries and exists for no other platform, so `dotnet build` on macOS or Linux fails at restore. Nothing else in the tree is Windows-specific as far as I can see, which makes this a one-file problem rather than a port.

**Fix it now rather than when somebody asks**, because it is three lines and because it is currently a lie by omission: the README says the project is C# on .NET 10 with Avalonia, which reads as cross-platform, and it is not.

The shape is a conditioned `PackageReference` per runtime, something like a Windows condition on the existing one and sibling entries for the osx and linux runtime packages. **Check the exact package ids on nuget.org rather than taking mine**, because OpenCvSharp's runtime packages have been renamed more than once and some are pinned to specific distribution versions, which matters for what a Linux user can actually restore.

`src/GroupLab.App/GroupLab.App.csproj` also sets `OutputType` to `WinExe`. On .NET that is harmless off Windows, where it behaves as `Exe`, but confirm rather than assume.

### 2. What is genuinely easy, and why

Avalonia runs on macOS and Linux already and renders through Skia on all three, so the interface is not the problem. `GroupLab.Core` has no platform types and no OpenCV dependency by design, so the measurement code should need nothing. The realistic work is the imaging backend, the packaging, and the differences nobody predicts.

**Three of those worth naming in advance:**

- **Case sensitivity.** Linux filesystems are case-sensitive and Windows is not, so any path with the wrong case works on your machine and fails there. This is a benefit rather than a cost: a Linux build is the cheapest detector of a class of bug that is otherwise invisible until a contributor hits it.
- **Fonts.** The PDF renderer embeds what it needs, so printing should be unaffected, but the interface picks up system fonts and will look different. Not a correctness problem; worth knowing before somebody reports it as one.
- **Printing.** The print screen's "save a PDF" path is portable. Driving a printer with scaling disabled, which entry 25 section 2 made a hard requirement, is platform-specific and may simply not be possible on one of them. If so, say so in the dialog on that platform rather than silently printing at whatever scale the driver chooses.

### 3. Where it sits in the plan

**Not a phase.** `DESIGN.md` section 21 numbers phases by capability, and "runs on another desktop" is not a capability, it is a property that either holds continuously or rots. A phase would mean it is allowed to be broken until that phase arrives, which is how a project ends up with a three-week port.

**So: fix section 1 now, and from the moment CI exists, keep all three green on every push.** The cost of that is close to zero while the code is small and rises every month it is deferred.

**The one thing that does need a gate, later:** a build that compiles is not a build that measures. Before macOS or Linux is offered to anybody, **the Phase 0 gate record must reproduce on that platform**, and the comparison is byte-identical or the difference is explained. Floating point, image decoding and font rasterisation all vary by platform, and a quarter-thousandth disagreement in a bull centre is the kind of thing that would otherwise be discovered by a stranger with a scanner.

### 4. CI covers all three, and going public just made that free

Entry 31 section 3 holds a GitHub Actions workflow until the fresh repository exists. It does now, and it is public, so **the hosted runners for `ubuntu-latest`, `macos-latest` and `windows-latest` are free with no minute limit.** That changes the calculation: three-platform CI costs nothing but the yaml.

**When you write that workflow, make it a matrix over the three**, with Windows required and the other two allowed to fail at first so the build is not blocked before section 1 lands. Once they pass, make them required too. Report the test counts per platform, because "it built" and "it produced the same numbers" are different claims and only the second one matters.

### 5. There is a person waiting

Alan has a friend who wants the macOS build, so this is not hypothetical demand. **That is a reason to fix section 1 promptly and not a reason to promise a release.** A macOS build that compiles and has never had its measurements checked is worse than no macOS build, because the friend would trust the numbers.

Also worth knowing: distributing a macOS build that people can open without fighting Gatekeeper needs a paid Apple Developer account and notarisation, which is a separate decision Alan has parked. A build somebody compiles themselves needs none of that, and is the right first offer.

---

## 2026-09-15, entry 31: the gate is clear, the failing test deserves a better fix than deletion, and the README needs a guard

**Status: actioned 2026-09-15. Section 3 waited for the fresh repository, and was done under entry 33: `ReadmeTests` and the three-platform workflow.** Section 2: the real-photograph test now writes its own location into a copy of `main1.jpg` and passes. Section 4: the hash map and citations were already done, the allowlist removal, the section 15.4 wording and the results figures are in, and the worktree branch is deleted. Reported in `docs/PHASE1-RESULTS.md` "Entries 29 and 30". Nothing pushed. Section 1 unblocks the push. Section 3 is new work and waits until the fresh repository exists.

### 1. The gate, and the go-ahead

**Byte-identical `photos.json`, byte-identical console table, zero lines differing, and the post-rewrite run also matches the record committed before the scrub.** That is the claim entry 29 section 5 existed to test, tested properly, and it passes. Scrubbing the metadata moved no measurement. Thank you for running it as a comparison rather than a spot check.

I verified the sixteen photographs independently, from fresh copies, with a different EXIF library: zero GPS blocks, zero maker notes, dates, unique ids or software strings, and `DigitalZoomRatio` present on all sixteen. Two methods, same answer.

**So the rewrite is accepted.** What remains before Alan deletes and recreates the repository is the failing test, the hash map, and the three pending edits you listed.

### 2. `ARealPhonePhotographScrubsToIdenticalPixels`: do not delete the assertion, move it

Your diagnosis is right: line 173 asserts `main1.jpg` still carries an EXIF GPS block, and after the rewrite it does not. Your proposed fix is to drop that assumption. **I would not, because of what the test is for.**

That test exists to prove the scrubber removes a location from a **real camera JPEG**, with a real maker note, a real thumbnail and whatever else a phone writes, and leaves the pixels untouched. The synthetic phone image does not exercise that: a file we constructed contains only what we thought to put in it, which is exactly the assumption a real file is there to challenge. Deleting the assertion leaves the test running on a file with no location, where it can no longer fail for the reason it was written.

**Make the test build its own input instead.**

1. Copy a committed photograph to a temporary path.
2. **Write a GPS block into the copy**, with coordinates the test chooses, plus whatever else is worth proving gets removed.
3. Run the scrubber on it.
4. Assert the location is gone, the camera fields including `DigitalZoomRatio` survive, and the decoded pixels are identical to the original.

That keeps the real-file coverage, removes the dependency on a committed file carrying something we have just spent a day removing, and cannot rot the same way again. It also means the test still passes in the `grouplab-testdata` world, where no committed image will ever carry a location by policy.

**If writing a GPS block from C# is awkward with the library you have, say so and take your version**, with a comment saying what coverage was traded away and why. A worse test that is honest about being worse beats a silently weaker one.

### 3. Keep the README honest automatically, because it has already gone stale twice

Alan has asked for the GitHub front page to stay current without anybody remembering to update it. The README is now the project's public face, and it has already been wrong twice in two days: it claimed twenty built-in sheets when there are twenty-two, and it claimed the application and statistics were not built after both existed. Both were caught by a human reading it, which is the mechanism we are trying to replace.

**Do not try to generate the README.** Most of it is argument and judgement, and generated prose reads like it. Guard the parts that are facts.

**Add `ReadmeTests`, in the Core test project, asserting:**

1. **Every relative link resolves.** Each `[...](path)` pointing inside the repository names a file that exists. This would have caught nothing so far, which is luck rather than design.
2. **Every referenced image exists.** Each `![...](path)`. This one has already bitten: the README was committed referencing six screens that were not in the repository yet, and the page rendered with six broken images on `phase-1` for several hours.
3. **Stated counts match reality.** The number of built-in sheets the README states equals the count of `targets/*.gltd.json`. Put the number between marker comments so the test can find it without parsing prose, for example `<!--count:sheets-->22<!--/count-->`.
4. **The stated target framework matches `Directory.Build.props`.** The README says .NET 10; the build is the authority.
5. **No em dash appears anywhere in the file**, which is a project rule and is cheaper to enforce than to remember.

Failures should name the line and say what to change. A test that says "the README claims 20 sheets, `targets/` holds 22" is worth ten that say "assertion failed".

**Then add the CI workflow, once the fresh repository exists.** A GitHub Actions job on push and pull request, running `dotnet build` and `dotnet test` on Windows. That gives the README guard somewhere to run without anybody choosing to run it, and it gives the repository the badge a visitor looks for. **Do not add it before the push**, because a workflow file in a repository that is about to be deleted and recreated is a workflow that runs against a history that will not exist.

**One rule for the text the tests cannot check.** When a commit changes something the README states in prose, change the README in the same commit. The status section is the one that rots fastest, because it is the one a visitor reads first and the one nobody editing code thinks about.

### 4. Order, so nothing waits on the wrong thing

1. Fix the failing test per section 2, re-run Core, confirm 709 of 709.
2. Write `docs/REWRITE-HASH-MAP.md` and correct the eight citations, per entry 30 section 2. **This is still the most fragile item in the project**, because the map lives only in your scratchpad.
3. The three pending edits: the allowlist removal, the `STATISTICS.md` section 15.4 wording on fixture precision, and the results section figures.
4. Delete the `worktree-agent-a5825dfa6aad44e1d` branch.
5. Mark entries 29, 30 and 31 actioned. Commit locally. Report.
6. **Stop. Do not push.** Alan deletes and recreates the repository, and pushes.
7. After the push: `ReadmeTests`, then the CI workflow.

---

## 2026-09-15, entry 30: the rebase is aborted, the scrub is independently verified, and one map must leave your scratchpad before it is lost

**Status: actioned 2026-09-15.** Section 2: `docs/REWRITE-HASH-MAP.md` with all 42 changed ids, nine citations corrected, and the rule in `CONTRIBUTING.md`. Section 4: `docs/STATISTICS.md` section 15.4 item 15 states the fixtures' precision as a property of the fixtures, and the harness says how the aimed values are rebuilt. Section 5: the gate record is byte-identical before and after, and the suite passes with the allowlist removed. Section 6: the worktree branch is deleted, and `refs/original` is left for Alan not to push. Section 7: the refused commands are listed in `docs/PHASE1-RESULTS.md` "Entries 29 and 30". Answers your entry 29 step 5 report. Section 2 is the urgent one and should be done before anything else, including finishing step 5.

### 1. The rebase you found is gone, and you were right to stop

Alan ran `git rebase --abort`. `phase-1` is back at `4b5c9a4`, which is the rewritten history with the README diagram commit on top. `main` is at `95fe9b0`, also rewritten, carrying both README commits and the six concept screens. Nothing has been pushed or force pushed. `git status` reports the two branches diverged from their remotes by 42 and 41 commits, which is the expected shape of a rewritten local history against a stale remote.

**The pull was mine.** I told Alan to run it without first checking for `refs/original`, which is the thing that would have told me a rewrite had happened in that clone. Your read of the consequence was exactly right: continuing it would have replayed the scrubbed history onto the unscrubbed one and brought the coordinates back. Recording it here so the next person understands why the rule below exists.

**The standing rule until the fresh repository exists: no `git pull`, no `git push`, no `git fetch` that could fast-forward a local branch, from anybody, in that clone.** The remote is stale by design.

**One thing to check rather than assume.** Your uncommitted edit to `PublicationTests.cs` removing the sixteen-file allowlist did not survive into the post-abort working tree; Alan's `git status` afterwards showed no modified tracked files at all. Redo it rather than looking for it.

### 2. Get the hash map out of your scratchpad, now, into a tracked file

This is the most fragile thing in the project at this moment.

**The rewrite changed 40 commit hashes and 8 of them are cited in the documentation**, 6 in `NOTES-FROM-PLANNING.md` and 2 in `PHASE1-RESULTS.md`. That is an excellent catch and it is the half of entry 29 step 1 I did not think to ask for: I asked you to look for recorded file digests and you found none, correctly, but a cited commit id is the same failure wearing different clothes and the record is already wrong in eight places.

**The map lives in your scratchpad, which does not survive you.** If this session ends before it is written down, the correspondence between the old and new ids is unrecoverable, and eight citations in the permanent record become unresolvable references to commits that no longer exist in any repository.

**Do this first, before finishing step 5.**

1. **Write the full 40-entry map to `docs/REWRITE-HASH-MAP.md`** and commit it. Old id, new id, subject line, one row each. Include the date, the tool (`git filter-branch`), and one sentence on why the rewrite happened, so the file explains itself to somebody reading it in a year.
2. **Correct the 8 citations in place**, in the same commit or the next one, leaving the old id visible where the sentence needs it: `5f8dafd` (was `b9b117b` before the 2026-09-14 rewrite) reads better than a silent substitution, because a reader with an old clone or an old bundle needs to be able to find it.
3. **Then add a rule to `CONTRIBUTING.md`**: do not cite a bare commit id in a document unless it is worth maintaining through a rewrite. Prefer a document reference, a test name or a milestone label.

### 3. The scrub, verified independently

I checked all sixteen photographs in the working tree myself, from a fresh copy, reading the EXIF with a different library than yours.

**Zero GPS blocks. Zero maker notes, dates, unique ids or software strings.** What survives on every one of the sixteen is exactly: `Make`, `Model`, `Orientation`, `FocalLength`, `FocalLengthIn35mmFilm`, `FNumber`, `ExposureTime`, `ISOSpeedRatings`, `DigitalZoomRatio`, and the pixel dimensions. `DigitalZoomRatio` is present on all sixteen, which is what entry 29 section 2 required and what my Python whitelist would have destroyed.

That corroborates your step 3 from a second direction. **It does not replace the rest of step 5**, which is the part that proves the measurement did not move.

### 4. The Fligner-Killeen answer is a finding about my tooling, not about R

Your diagnosis is right and it is worse than the four keys it surfaced in. `sg_dump.R` writes its values through R's default 15 significant digits, in both the CSV and the JSON. For a comparison at 1e-12 that is invisible. For a statistic whose value depends on the ordering of nearly-equal numbers, the dropped bits change which values tie, and the statistic moves in the fourth decimal place. That is exactly the 1.2e-4 to 4.9e-4 relative gap question 14 recorded.

**So the fixtures are lossy, and every future tie-sensitive key will hit the same wall.** Three things follow:

1. **Record it in `STATISTICS.md` section 15.4** as a property of the fixtures rather than a difference between implementations: values are stored at 15 significant digits, which is insufficient to reproduce rank-based statistics with near-ties, and the four Fligner keys are the worked example.
2. **I will regenerate the fixtures at 17 significant digits**, which is the round-trip precision of a double, so the stored value is the value. That is my file to fix.
3. **Not yet.** A 14 MB fixture change in the middle of a history rewrite is the worst possible timing. It waits until the fresh repository exists and the rewrite is behind us.

Your rebuilt values and the 45,476 keys matching with nothing pending is the right outcome in the meantime. Note in the harness how those values were reconstructed, so the next regeneration can be checked against it.

### 5. Finish step 5, in this order

1. The hash map, per section 2.
2. The post-rewrite Phase 0 gate record. **This is the one that matters most**, because it is the claim that scrubbing changed no measurement. Before and after must agree exactly, not approximately.
3. The full suite, with the allowlist removed from `PublicationTests`, and the counts.
4. A short report: gate before and after, Core and App counts, and the `PublicationTests` result.

**Then stop again.** The deletion of the GitHub repository and the push of the rewritten history remain Alan's, and he has not done either.

### 6. Two things for the fresh push, so they are not discovered afterwards

- **`worktree-agent-a5825dfa6aad44e1d` is a live local branch.** It is a leftover from one of your worktrees and it appears in the bundle alongside the real branches. Delete it before the push rather than publishing it.
- **`refs/original/*` must not be pushed.** They are `git filter-branch`'s backups and they point at the unscrubbed history. An ordinary `git push origin main phase-1` will not carry them; `git push --mirror` or `git push --all` with a stale refspec could. They are kept locally, and in Alan's bundle, until the fresh repository is verified.

### 7. The permission refusals

You said the classifier refused `git ls-remote` and two other read-only checks. Tell Alan exactly which commands, in one line each, and he can allow them. Read-only git queries are worth having available, and right now the inability to inspect the remote is a real handicap on the one task where the remote's state matters.

---

## 2026-09-14, entry 29: Alan has approved scrubbing the coordinates out of history, and one scrubber is already out of date

**Status: actioned 2026-09-15, steps 1 to 5.** No recorded digest; the bundle verified; all sixteen photographs scrubbed to identical pixels; the local history rewritten; the gate record, the suite and `PublicationTests` verified. Steps 6 onward, the push and the repository deletion, are Alan's and have not been run. Reported in `docs/PHASE1-RESULTS.md` "Entries 29 and 30". Answers question 13 section 2. **Read section 4 before running anything: this entry stops short of the irreversible steps on purpose.**

### 1. The decision

**Option (a). Replace the sixteen files in history with scrubbed copies, before the repository goes public.** Alan has approved it. Your reasoning was right on every point, including that the rewrite and the push are not yours to run.

### 2. A catch that has to be handled first, or the rewrite destroys something we just decided we need

`tools/scan_analysis/scrub_exif.py`, which entry 23 section 5 told you to commit and which the planning record has been treating as the scrubber, **whitelists only these tags:** Make, Model, Orientation, FocalLength, FocalLengthIn35mmFilm, FNumber, ExposureTime, ISO, PixelXDimension and PixelYDimension.

**`DigitalZoomRatio` is not on that list.** Entry 27 section 2 established that it has to join the lens-fit grouping key, because `FocalLengthIn35mmFilm` is not updated for digital zoom on every device. So the Python scrubber, run as it stands, destroys the tag the grouping now depends on. Any image already scrubbed with it has lost that tag and cannot get it back.

**Three things follow.**

1. **The C# `ImageScrubber` is the definition from now on**, since you have already given it `DigitalZoomRatio`. Use it for the history rewrite, not the Python script.
2. **Align or retire `scrub_exif.py`.** Two scrubbers with different whitelists is a trap that will be sprung by whoever reaches for the wrong one. My preference is to keep it, add `DigitalZoomRatio`, and put a line at the top saying the C# implementation is authoritative and this one exists for ad hoc use. If you would rather delete it, say so and I will agree.
3. **Check whether anything has already been scrubbed with the Python version and lost the tag.** `scans/mounted/` is the candidate. If it has, the originals are outside the repository on Alan's machine and can be rescrubbed; flag it rather than quietly accepting the loss.

### 3. Scrub all sixteen, not the thirteen with coordinates

`main_flat1` to `main_flat3` carry an empty GPS block. Scrub them too.

**The reason is the test, not the privacy.** If three files keep a GPS block, `PublicationTests` has to carry a permanent allowlist of three names, and an allowlist is a thing that goes stale and that somebody eventually adds a fourth name to. **After the rewrite the list should be empty and the rule should be absolute: no committed image carries a GPS block of any kind.** A rule with no exceptions cannot rot.

### 4. The order of operations, and where to stop

Steps 1 to 5 are yours. **Step 6 onwards is Alan's, and you must not run any of it.**

1. **Find every recorded hash first, and report before touching anything.** Search the whole repository, documentation, tests, fixtures and code, for any recorded SHA-256 or other digest of the sixteen files. The scrub changes their bytes, so any recorded hash becomes wrong the moment the rewrite lands, and it has to change in the same rewrite or the record silently lies. **If you find any, stop and report them rather than proceeding.** This is the step most likely to turn a clean rewrite into a mess discovered a week later.

2. **Make the bundle.** `git bundle create ../grouplab-prerewrite-2026-09-14.bundle --all` from the repository root, so it lands **outside** the working tree, then `git bundle verify` it and report the result. It is the only way back if the rewrite goes wrong, so it is worth the thirty seconds to confirm it is readable rather than assuming.

3. **Produce the scrubbed copies and prove the pixels are untouched, on all sixteen.** A test already shows `main1.jpg` decodes identically. **Extend that to every one of the sixteen**, comparing decoded pixel data rather than file size, and report the count. One file proves the method; sixteen prove the job.

4. **Run the rewrite**, replacing those paths' contents in every commit that contains them.

5. **Verify, and report the numbers.**
   - `PublicationTests` finds zero committed images with a GPS block, with an empty allowlist.
   - The full suite passes: Core and App, with counts.
   - The Phase 0 gate records still reproduce from the rewritten files, because that is the claim entry 11 made about frozen fixtures and this is the first thing that could break it.
   - Report the pack size before and after.

6. **Stop there. Do not push, do not force push, do not delete any remote.** Report that steps 1 to 5 are done and what they found.

**Why the hard stop.** A force push leaves the old objects reachable by hash on GitHub for an indefinite period, so it does not actually remove the coordinates from the remote. The clean route is for Alan to delete the private repository on GitHub and push the rewritten history to a fresh one. Deleting a repository is his to do and cannot be undone, so it happens with him at the keyboard, after he has read your report from step 5.

### 5. What does not change

The scrub touches metadata only, so every measurement, every gate result and every table in `PHASE0-RESULTS.md` stands unchanged. Nothing in the planning record needs revisiting because of this. If step 3 finds a file whose pixels do change, that is a defect in the scrubber and the rewrite stops until it is fixed.

---

## 2026-09-14, entry 28: questions 12, 13 and 14 answered, and every field name the intake tool guessed is wrong

**Status: actioned 2026-09-15.** Section 1: `grouplab intake` reads `meta.json` schema 1 exactly as recorded, with no sentinel file, the consent text verbatim and `original_name` kept but never a path. Section 2: question 12 answered and the extreme spread coverage table in `docs/STATISTICS.md` section 15.4 item 13. Section 3: the regenerated fixtures committed, and the four Fligner-Killeen keys and every probe key compared; the difference was the fixtures' 15-digit JSON, section 15.4 item 15. Section 4: the README's "Test data" and `PublicationTests` read a `grouplab-testdata` checkout, which Alan has yet to create. Section 5: section 15.4 item 14. Reported in `docs/PHASE1-RESULTS.md` "Entry 28". Section 1 is the urgent one: it stops the intake tool refusing every real submission. Section 4 needs Alan and is not mine or yours to decide.

Entries 22 to 27 are all actioned and the work behind them is good. Question 12's correction of my own entry 24 is right and I have taken it. What follows answers all three open questions, and adds one finding that came out of answering question 14.

### 1. Question 13 section 3: the real `meta.json`, which does not match a single assumed name

You asked me to confirm or correct the fields, because no document specifies them. **Every one of them is different.** A real submission is now on Alan's disk, pulled and hash-verified, and this is its `meta.json` in full, field for field:

```json
{
    "schema_version": 1,
    "submission_id": "1a8f39ad",
    "submitted_utc": "2026-09-14T20:41:55Z",
    "exclude_from_public_dataset": false,
    "consent": {
        "agreed": true,
        "version": "consent_v1",
        "agreed_at_utc": "2026-09-14T20:41:55Z",
        "text": "I took these photos, or I have permission to share them. ..."
    },
    "answers": {
        "target_backing": "", "attachment_method": "", "shot_distance": "",
        "caliber": "", "notes": "", "credit_name": ""
    },
    "user_agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ... OPR/135.0.0.0",
    "files": [
        {
            "index": 1,
            "stored_name": "001_20180623_104930.jpg",
            "original_name": "20180623_104930.jpg",
            "bytes": 3043798,
            "sniffed_type": "image/jpeg",
            "sha256": "870dac2119a5c701cc19980189e0f332aba11b4af310c2ee92fc2775ca638857"
        }
    ]
}
```

The corrections, against what question 13 section 3 lists:

| Assumed | Actual |
|---|---|
| `submissionId` | `submission_id` |
| `submittedAt` | `submitted_utc` |
| `consentVersion` | `consent.version`, nested |
| `doNotPublish` or `optOut` | `exclude_from_public_dataset` |
| `files` as object of name to hash, or array of `{name, sha256}` | array of objects keyed `stored_name`, with `sha256`, plus `index`, `original_name`, `bytes`, `sniffed_type` |
| `answers` | `answers`, correct, with the six keys above |

**The whole file is `snake_case`.** Read it that way rather than adding a camelCase fallback, and fail loudly on an unknown `schema_version` rather than guessing, since the page writes `schema_version: 1` precisely so a later change can be detected.

**Three more things the real file settles.**

1. **There is no `DO-NOT-PUBLISH` sentinel file.** The opt-out is `exclude_from_public_dataset`, a boolean in `meta.json`, and nothing else. Check that field; do not look for a file that the page does not write. Treat a missing field as unknown and refuse, not as false.
2. **`consent.agreed` and `consent.text` exist and should be required and recorded.** The provenance record should carry the consent text verbatim, because it is what the contributor actually agreed to and a later version will say something different. `consent.version` alone is a pointer to a document we would then have to keep.
3. **`original_name` is the contributor's own filename** and may carry a date, a camera prefix, or nothing. Keep it in the provenance record and never use it as a path.

**Also worth knowing before you test against it:** on the first real submission all six answers came back as empty strings, not absent keys. Empty is the normal case, so an empty answer must never be a refusal reason.

### 2. Question 12: A, five shots, and entry 24 got the coverage wrong

**Take option A.** Section 9.1 names the five-shot row for exactly this purpose, and printing its range in words from 5 to 19 shots covers what entry 24 asked for with "between that minimum and about twenty". B withholds the count most people actually fire, and your objection to C is the right one: hiding the interval hides the very thing that says how little five shots know.

**Entry 24 section 1 cited the wrong figure and you were right to say so.** I attributed the bootstrap BCa interval's 79.5 percent coverage to the panel, which does not use it. The panel's own intervals cover 92.5 percent at two shots and 94.3 at five. So my sentence "an interval whose real coverage is 80 percent must not be labelled 95" was true of the bootstrap and false of the thing Alan was looking at. What actually misled him was a headline printed to three decimals above an interval spanning a factor of twelve, which is exactly the width problem you identify, and withholding the headline fixes it.

**Two things I want kept from entry 24 regardless.** Below the minimum, still show the shot positions and the centre from the aim, because both are exact at any count. And label every interval with its real coverage rather than a bare 95, which you have already done.

**On extreme spread, diverge from shotGroups and say so.** Your measurement is that shotGroups' form covers 84.66 percent at two shots and 92.31 at five while claiming 95, and that `RangeStatistics.MeanInterval` covers 94.9 to 95.2 across the range. Question 11's principle applies unchanged: keep GroupLab correct where the reference is not, keep the harness comparing the reference's own form against the reference, and record the difference. Put the coverage table itself into `STATISTICS.md` section 15.4 alongside the entry, not only in `SmallGroupCoverageTests.cs`, because a number that lives only in a test is a number nobody reads.

### 3. Question 14: the input was never the difference, and the probe is now in the repository

**I ran it.** `tools/shotgroups/sg_dump.R` has a new `flignerProbe` block and all nine fixtures are regenerated. **The regeneration is purely additive: 68,672 pre-existing keys are byte identical and 4,414 keys are new.** I verified that by diffing every key and value against the committed files before writing them, so nothing you have already validated has moved.

New keys, on the five datasets with three or more series:

- `flignerProbe.FlignerX.input.<i>` and `.FlignerY.input.<i>`, the exact vectors;
- `.groupMedian.<series>`, `.scoreMean.<series>`, `.n.<series>`, `.scoreVar`, `.tiedValues`;
- `.recomputed`, the statistic rebuilt from those vectors, which matches `compareGroups.FlignerX.statistic` to 1e-13 on every dataset;
- `flignerProbe.rowsSortedBySeries`, for the reason in section 5.

**The answer: the input is identical to `shots.xPOA` and `shots.yPOA`, which you already had.** `compareGroups` calls `getXYmat` per series without passing `relPOA`, and that argument defaults to `TRUE`, so the test sees aimed coordinates. I confirmed the per-series construction is bit-identical to the whole-frame one on `DFinch`: `identical()` returns true and the maximum absolute difference is exactly zero.

**And `coin` is not the difference either.** `compareGroups` calls `coin::fligner_test` when `coin` is installed, which it was when these fixtures were generated, and base R's `fligner.test` otherwise. I measured both on all three datasets and they agree to 1e-13, so that branch does not matter here.

**So the disagreement is inside your implementation, and here is the formula to check against.** `stats:::fligner.test.default` is not the textbook form, and the difference is in what gets centred:

```
x  <- x - tapply(x, g, median)[g]        # centre by group median
a  <- qnorm((1 + rank(abs(x)) / (n + 1)) / 2)
a  <- a - mean(a)                        # centre the SCORES, before anything else
v  <- sum(a^2) / (n - 1)
stat <- sum( n_i * mean(a_i)^2 ) / v     # a_i are the centred scores
```

`rank` uses its default `ties.method = "average"`. I checked the textbook form, `sum(n_i (Abar_i - abar)^2) / var(a)`, and the group-sums form against this on `DFinch`: all three agree to 4e-15. **So the formula is not a 2e-3 effect and your tie hypothesis does not explain the gap.**

**What I swept and could not make produce your 10.073187875409229**, so you need not repeat it: grouping by `series`, `orgser` and `group`; `ties.method` of average, first, min and max; group centring by median, lower median, upper median, type-7 quantile and mean; and raw against aimed coordinates. Aimed gives 10.075167218103, raw gives 10.072777268233, and your value sits between them and matches neither. **That shape, between the two, is what a partially aimed vector looks like**, so I would look first at whether the aim is being applied to some rows and not others, or to x and not y, rather than at tie handling.

**If the probe shows your input matching and the statistic still differing**, then it is genuinely R's last bits and section 15.4 gains a thirteenth known difference. I do not expect that, given the size of the gap.

### 4. Question 13 section 1: option A, the separate data repository

**Take A, `grouplab-testdata`.** Your reasoning and entry 22's agree and I have nothing to add to it. Two conditions:

1. **It must be GPL-3.0.** Not a choice. The consent text the contributors actually agreed to says "published as part of GroupLab's public test data on GitHub under the GPL-3.0 license". Licensing the data anything else, including something more conventional for images, would publish it on terms nobody consented to.
2. **The pinned commit and URL live in this repository**, as you propose, and the publication test continues to no-op when the checkout is absent. Say so in the README of both repositories, because the failure mode is a contributor cloning one and wondering why tests skip.

`scans/mounted/` goes there, scrubbed, as the first contents. The Phase 0 and Phase 1 scans stay here because committed gate records read them by path.

### 5. A finding from writing the probe: `compareGroups` misaligns its own coordinates

`compareGroups` builds the coordinate columns with `split()` then `rbind()`, which returns rows in **factor level order**, and attaches them with `cbind()` to a frame still in its **original row order**. When those two orders differ, every coordinate is paired with the wrong series label.

**`DFlandy01` is such a dataset**, and it is in the fixture set. 519 of its 530 rows carry a coordinate from a different row than the label beside them.

**It changes no value here, and the reason is worth understanding rather than trusting.** Every series in `DFlandy01` is a contiguous block of exactly ten shots, so the misalignment permutes whole blocks and leaves the partition intact. The Fligner, Kruskal and MANOVA statistics are invariant under relabelling groups of equal size, so they come out identical: 44.0020356303247 either way. The per-series outputs are unaffected for a different reason, that they are computed from the correctly named `xyL` list rather than from the pasted columns. I checked three series centres against a correct alignment and they agree exactly.

**So no fixture key is wrong.** But it is one interleaved dataset away from being wrong, and a reimplementation that sorts internally would be doing the right thing and disagreeing. `flignerProbe.rowsSortedBySeries` now records it per dataset, and it is 0 only for `DFlandy01`. **Record it in `STATISTICS.md` section 15.4 as a known difference**, with the note that GroupLab should align coordinates to labels correctly and that the fixtures happen not to distinguish the two.

### 6. Question 13 section 2 is Alan's, not ours

The coordinates already in history are his, the rewrite is irreversible and the force push changes what every clone has. I have put the recommendation and its conditions to him directly rather than deciding it here. **Until he answers, commit no image**, which is what you are already doing.

---

## 2026-09-14, entry 27: the first donated submission, and a correction to how frames must be grouped for a joint lens fit

**Status: actioned 2026-09-15.** Amends entry 16. Section 1: intake triages every file by decoded markers and holds what it cannot use with the reason. Section 2: `DigitalZoomRatio` is read, is in the lens grouping key as unknown when absent, and the key is recorded with the fits. Section 3 is recorded per file. Reported in `docs/PHASE1-RESULTS.md` "Entries 22 and 27". Section 2 is the one with code consequences; sections 1 and 3 are intake findings that belong on the record before the donated set grows.

The public upload page at `pissinhot.com/targets` took its first submission from somebody outside this project on 2026-09-14. Three photographs, all Samsung, all camera originals. The pull script verified all three SHA-256 hashes against the `meta.json` written at upload, and every one matched, so the no-re-encoding requirement in the page specification is holding in practice and not merely on paper. That part worked.

### 1. None of the three photographs is usable, and that is the finding

One is a playing card held in a hand. One is a blank cardboard silhouette on stakes at distance, backlit against trees. One is a stack of silhouettes lying on grass, perforated by several hundred pellets.

No reference grid, no known scale in frame, no flat sheet, and in one case no mounting at all. The submitter answered none of the six optional questions: backing, attachment, distance, calibre, notes and credit were all empty strings.

**Do not treat this as a bad contributor.** Treat it as the measurement of what an open request produces. The uploaded set will contain a large fraction of material like this, and the consequence for the code is specific: **the intake path must assume most submissions are unusable and must say why, per file, before anything reaches the repository.** Entry 22 asked for intake to be a gate rather than a habit. This is the evidence for that, arriving on day one.

The cheapest useful triage is the registration and scale check the application already performs. A frame with no decodable markers and no detectable rectangular sheet boundary is not a candidate, and the tool can say so without a human looking at it. A human then looks only at what survives.

### 2. `FocalLengthIn35mmFilm` is not a sufficient grouping key, because digital zoom does not always update it

This is a correction to entry 16, and it matters for any joint fit across frames.

Entry 16 concluded that Alan's table frames were a cropped ultrawide, that both the physical focal length and the 35 mm equivalent tags were correct, and that frames must be grouped for a joint lens fit by physical focal length, f-number, 35 mm equivalent and image size. The first three of those held on the hardware in front of us. This submission breaks the assumption underneath them.

| File | Device | Physical focal | f-number | 35 mm equivalent | `DigitalZoomRatio` | Stored size |
|---|---|---|---|---|---|---|
| 001 | SM-G965U | 4.3 mm | f/1.5 | 26 mm | absent | 4032 x 1960 |
| 002 | Galaxy S24+ | 2.2 mm | f/2.2 | **13 mm** | **1.64** | 4000 x 1848 |
| 003 | SM-G996U | 5.4 mm | f/1.8 | 26 mm | 1.0 | 4032 x 1816 |

File 002 is the ultrawide lens with 1.64x digital zoom applied, and its 35 mm equivalent tag still reads 13 mm, which is the **unzoomed** figure. The effective field of view corresponds to roughly 21 mm. Alan's own frames reported a cropped equivalent of 23 mm on a 2.2 mm lens, meaning his device updates the tag and this one does not.

**So the tag cannot be trusted to describe the frame, and two frames that agree on all four of entry 16's key fields can still have different effective geometry.**

Two consequences:

1. **Add `DigitalZoomRatio` to the grouping key**, and treat its absence as unknown rather than as 1.0. File 001 omits the tag entirely.
2. **Treat a frame with `DigitalZoomRatio` greater than 1 as a separate group from an otherwise identical frame**, even when every other tag matches. Digital zoom crops and usually upscales, which changes both the effective focal length in pixels and the interpolation the pixels have been through.

**A third consequence worth stating plainly: a joint fit keyed on metadata is only as good as the metadata, and this is the second time in this project that a tag has meant something other than what it says.** Where a fit can be validated from image content rather than from tags, prefer that. Where it cannot, record the key that was used alongside the result, so a wrong grouping can be found later rather than being baked invisibly into a calibration.

### 3. The aspect ratios are a phone camera mode, not a crop by the user

All three are far wider than a sensor's native 4:3, at 2.06:1, 2.16:1 and 2.22:1. Those match the Samsung full-screen capture modes at 18.5:9, 19.5:9 and 20:9 for the three handsets involved. The `Software` tag on each file is a camera firmware build string rather than an editor name, and the stored dimensions match the EXIF dimensions, so these are camera output and not gallery-app edits.

That is good news for provenance and bad news for field of view: the mode crops the sensor top and bottom, so a whole mounted target is harder to get in frame and the submitter will tend to back further away. Expect donated phone photographs to be wider and lower resolution in the vertical axis than a specification written around 4:3 would assume. Nothing needs changing today; it is a fact to have when a detection threshold is tuned against donated frames.

**None of the three carried GPS.** That does not retire `scrub_exif.py`, since one submission is not a sample, and the scrub must still run at publication regardless.

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

**Status: actioned 2026-09-15.** Section 1 in `2665c05` (was `f4f5263` before the 2026-09-14 rewrite), reported in `docs/PHASE1-RESULTS.md` M4.3; section 2 as the print screen, M4.4; adjust to zero still waits, per section 3. Two findings from Alan's second pass. The first is a small fix with a larger shape behind it. The second is a missing pillar rather than a missing button.

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

**Status: actioned 2026-09-15.** Sections 1 to 3 in `5f8dafd` (was `b9b117b` before the 2026-09-14 rewrite), with question 12 for the threshold; sections 4 and 7 with entry 26, and section 5; reported in `docs/PHASE1-RESULTS.md` M4.2.

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

**Status: actioned 2026-09-15**, except committing `scans/mounted/`, which section 5 holds until entry 22 section 1 is answered, `docs/QUESTIONS-FOR-PLANNING.md` question 13. Section 1: the fixtures and `sg_dump.R` committed, the harness reads the point of aim, question 11 marked answered and recorded in `docs/STATISTICS.md` sections 15.2 and 15.4, four Fligner-Killeen keys pending as question 14. Section 2: the M3.1 amendment and `Bootstrap`. Section 3: the M1.11 amendment. Section 4: the detector runs inside the registered sheet, `docs/DETECTION-PIPELINE.md` before S5. Section 5: both scripts committed.

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

**Status: actioned 2026-09-15.** Section 1 raised as `docs/QUESTIONS-FOR-PLANNING.md` question 13, recommending a separate data repository, and no image is committed until it is answered. Section 2 is `grouplab intake`, section 3 is `PublicationTests`, reported in `docs/PHASE1-RESULTS.md` "Entries 22 and 27". The committed-image check found GPS in 16 Phase 0 photographs, also question 13.

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

**Status: actioned 2026-09-14.** Fixtures and scripts committed in `dbea97b` (was `20f562e` before the 2026-09-14 rewrite) with the brief's section 4.4 amendment and questions 7 to 10 marked answered; M3 built against the fixtures and reported in `docs/PHASE1-RESULTS.md` M3.1, with the differences found raised as question 11.

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

**Status: actioned 2026-09-14.** `grouplab surface frames` ran in the foreground with a progress line per frame, its crash recorded at `430081b` (was `ab42e33` before the 2026-09-14 rewrite) and the completed run and M1 report at `e30fbeb` (was `d574a7e` before the 2026-09-14 rewrite), and the standing rule is in `CONTRIBUTING.md` under "Long-running steps" with entry 15 section 6's sentence appended.

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

**Everything else about how you are working is right**, and this is a mechanical fault rather than a judgement one. Measuring entry 11's change before making it, parking the blocked instruction and carrying on with the independent one, committing the surface fit at `88dc0f9` (was `d78c17c` before the 2026-09-14 rewrite) before any real frame was run, and making both M1.3 changes on synthetic evidence with the reasons recorded, are all exactly right.

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
