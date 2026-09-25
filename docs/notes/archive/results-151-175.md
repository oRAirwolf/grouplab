# Phase 1 results, entries 151 to 175

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 150: an executable is built only when the application changes

Alan, reading the releases page: *"it seems like a lot of builds and releases are extremely minor, like just updating release note pages being brought up to date or research articles were written. Why does this need a new executable? Can't these things be done without creating a new executable and having to get compiled and tested? That seems like a total waste of time."*

### One list, read by the gate and by the tests

`.github/shipping-paths.json` names every top level entry and says which side it is on and why. Twenty seven entries, eighteen shipping and nine content. `scripts/shipping-gate.py` reads it and so does `ShippingPathsTests`, so the rule and the gate cannot drift apart, which is the failure that actually happens: not somebody deleting the gate, but the gate going on answering after it has stopped being right.

**A path in neither list fails.** That is the entry's instruction and it is the right way round. Default to shipping and every website change wastes a build; default to content and something untested is published. Failing makes a new top level directory a decision somebody takes once.

### The gate

A scheduled workflow gets no paths filter, so the gate is a job output rather than a trigger filter. It is a step in `name-it`, which is the first job and is already where the nightly works out which commit it is for, and it outputs `application-changed`. Both `package` and `publish` depend on it.

A skipped night creates no release, no tag and no assets, and writes one line into the run summary: **"No application change since nightly NN. No build produced."**

**Nightly numbers now count builds rather than runs.** The version came from `github.run_number`, which increments for every run including the ones that build nothing, and that is why the published numbers already jump: 14, 16, 18, 25. It is now the highest existing nightly tag plus one.

### What the waste actually was

Of the last twenty eight nightlies, **five changed nothing that ships inside the executable: 14, 72, 76, 77 and 78.** Each of those compiled and tested on three operating systems to produce a build identical to the one before it.

**Nightly 84, the build Alan named, would still have been built.** Its diff against 81 is 147 paths, and although 95 of them are the website, it also changed `src`, `tests`, `.github` and `scripts`. So what made 84 look like a website build was not its diff but its release note, which described the website work and nothing else. That is entry 145's territory rather than this entry's, and it is worth saying plainly: the gate fixes the builds that should not have happened, and it does not fix a note that describes the wrong half of a build.

No published release was edited or deleted.

### The two loops

Entry 144's guard on the notes commit still holds and now has a second one underneath it: a `[notes] ` commit is skipped by `name-it`'s condition, and it is content, so the gate would refuse it even if the condition were removed. The site publish and the nightly remain connected only through the release note append entry 144 defined. Proved by the run that carries this entry rather than by reading the YAML.

### Tests

- Every top level entry git tracks is in exactly one list, taken from `git ls-tree` rather than from the filesystem, so an untracked scratch folder beside the repository does not fail the build.
- The nightly has a gate whose output is `application-changed`, and both `package` and `publish` depend on it. The workflow's text is normalised to LF first: a pattern anchored on a line ending has already cost this repository two red pushes on Windows runners.
- The releases page says why the numbers skip.
- Section 6.3's dry run is a step in the nightly rather than a test. It needs the tag history, which a CI checkout does not have, and running it nightly exercises the logic against real history exactly as the section asks.

## Entry 151: the community page, and nothing on the site navigates on its own

Alan: *"make it so the community link at the top of the page does not automatically redirect to the discord server. People will not appreciate this. Make a page that has a link to the invite and let people choose if they want to click on it."*

He is right, and entry 148 built the thing he is describing: `grouplab.org/discord` was a meta refresh that threw the visitor off the site before they had read a word. A navigation item that ejects you from the site is remembered badly.

### What is there now

A real page, in the site's own layout, with the navigation, the footer and the theme. It says what the Discord is for, what is in it, what is expected there, and that Discord is a third party service nobody has to join to use GroupLab or to get help.

**One thing on the page leaves the site.** It is marked "Opens Discord in a new tab" before it is clicked, it opens in a new tab, and the invite address is printed as visible text beside it, because some people want to see where a link goes before they follow it.

### What the page does not claim

**The channel names are not on it, because nobody has read them out of the server.** The five group names came with the entry, so the page names the groups and says in a line what each one is for. Inventing channel names to fill a list would have been the same fault entry 159 is about: something published that is not true, in a place nobody would think to check.

**The rules section is the project's own expectations**, written here, and the page says so rather than presenting them as the server's. `docs/notes/for-alan.md` request 4 asks for both, and `website/links.json` is where both go, so the page becomes exact without a rewrite.

### The one source, and the tests

`website/links.json` still holds the invite and the canonical address, and now the group list as well. The test that the invite is written out in exactly one place is amended for the one exception the entry allows rather than deleted, and the published-site test now names the community page instead of the redirect.

Two new tests:

- **No page on the built site carries a `<meta http-equiv="refresh">`.** That is the general form of the fault rather than the one instance of it, so the next page that tries to navigate on its own fails the build.
- **The community page carries the invite as a link and as visible text**, and says "new tab" before it is clicked. A page carrying it only as an `href` would have lost half of what section 1.3 asks for with nothing looking wrong.

## Entry 152: what GroupLab can actually measure, and two published claims that were wrong

Alan, reading the tour, on two statements: *"This is not true. It can read any target as long as a scale is defined, right?"* and *"Doesn't GroupLab correct for sheets being printed at the wrong scale? I know we emphasize the importance of this, but isn't it technically a non issue?"*

He is right on both, and the second was contradicted by `printer-true-size`, a research article published on the same site. A reader who read both learned that the site cannot be trusted rather than which sentence was right.

### Section 2 item 1: what can establish scale

Four things, all of them real today.

| Source | Where | What it needs | What it models |
| --- | --- | --- | --- |
| A GroupLab sheet's markers | `SheetReference`, `Marking/ScaleReference.cs` | A sheet GroupLab printed, with enough markers visible | Perspective, the lens, and the sheet's own print scale. The only automatic one |
| A known rectangle | `RectangleReference`, same file | Four corners tapped in order, and its real size | Perspective exactly. Not a bow in the paper: a homography is planar |
| A known length | `LengthReference`, same file | Two points tapped, and the distance | One scale over the whole image; declares that it assumes square on and flat |
| A scan's stated resolution | `Marking/StatedResolutionScale.cs` | A scan, not a photograph, stating a believable resolution | The same as a known length. Offered, never applied by itself |

The fourth refuses a photograph outright, because a photograph's stated resolution describes the file and not the paper, and refuses anything below 100 or above 4800 dpi, since 72 and 96 are what a file gets when the thing that wrote it had nothing to say.

### Section 2 items 2 and 3: a target GroupLab did not print

**It can be measured.** The user sets the scale by tapping a known length or a known rectangle, and marks the shots by hand. Everything downstream then works identically: group size, extreme spread, mean radius, the standard deviations, the comparison between sessions, the units, the export and the report.

**Exactly five things need a GroupLab sheet**, and nothing else does:

1. **The scale**, automatically and with perspective and lens modelled rather than assumed away.
2. **Finding the holes.** `AutomaticMarking.Run` takes a `TargetDefinition` and cannot run without one, because detection works by rendering the sheet GroupLab printed and differencing the photograph against it. `NeutralDarknessHoleDetector` needs no definition, but it is reached only from the CLI spikes and from no screen, so it is not an answer for a user today.
3. **Which bull each shot belongs to**, because the sheet is what says where its bulls are.
4. **The review queue**, which exists to question what the detector decided. Hand-placed marks are not guesses, so it has nothing to question.
5. **The sheet's identity**, which ties a photograph to the sheet it is of.

### Section 2 item 4: a wrong print scale is corrected, not merely reported

**The measurement is corrected and the figures are right.** The scale comes from the markers, and the markers shrank with everything else, so a sheet printed at 96.2 percent is a smaller sheet measured by its own smaller markers.

The proof is already in the suite and was not written for this entry. `SyntheticScanTests.Test43APrintAt962PercentReportsItsScale` renders the reference sheet at 96.2 percent and puts it through **the same gate as a full size sheet**: every bull centre recovered within 0.001 in. It then additionally asserts the reported scale is 0.962. The research article's "detects this and corrects every figure for it" is exactly right.

The print scale itself is computed by comparing the resolution the markers measure against the resolution the file states, in `ScaleReport`, and exists to say so to the person: `DetectionAdvice.PrintScale` writes "The measurements are corrected for it, and the figures are right; print at actual size, 100 percent, to keep the sheet's own spacing."

So Alan's "technically a non issue" is right for the uniform case, which is the ordinary case.

### Section 2 item 5: what genuinely cannot be recovered

- **Scaling that varies across the page.** The registration fits a planar mapping, which handles a uniform shrink and even different amounts in x and y, because those are still planar. A printer whose scaling drifts across the page is not planar and cannot be undone. It shows as a rising fit residual rather than as a wrong answer that looks right, which is the better of the two failures.
- **Markers cut off, obscured or too few.** Registration says so and refuses rather than guessing.
- **A bow in the paper**, on the rectangle and known-length paths.
- **Perspective, on the known-length path**, where the error varies across the frame and nothing in the figures shows it. That scale declares the assumption beside every result it produces, which is the only honest thing to do about it.

### So the real reason to print at actual size

Not that a shrunk sheet measures wrong, because it does not. Three reasons that are true:

1. **A shrunk sheet is a different sheet.** Its bulls are closer together than the distance it was designed for. That is a fact about the shooting, not the measurement.
2. **Scaling that varies across the page is the unrecoverable case**, and a ruler against the printed edge is how you tell it from an ordinary uniform shrink.
3. **Smaller markers register less well**, and below some size stop being found at all.

The instruction printed along the bottom edge and the ruler check both stay. **What was wrong was the reason printed on the sheet itself**: `SceneBuilder.ActualSizeNote` read "Never fit to page: a sheet printed at any other scale measures wrong", on every sheet GroupLab prints. It now reads "a scaled sheet loses the spacing it was designed for". The print screen's own wording said a 97 percent print "measures 3 percent small" and is corrected the same way.

### One source, and the tests

`docs/WHAT-CAN-BE-MEASURED.md` holds all of the above and is published at `/what-can-be-measured/`. The tour pages, the research article and the build all point at it rather than writing their own version, which is what section 5.2 asks for and is the whole reason the two claims came to disagree with an article in the first place.

- **Five phrasings are banned** across `website`, `src`, `docs` and the README, by the mechanism entry 145 used for "nothing in this build changes". The logs and the inbox are excluded, because a log that cannot record what was wrong is not a log. The test caught my own quotation of one of the claims in a docstring on its first run.
- **Every tour screen carries a `withoutASheet` line** and the site build refuses a screen without one. On six of the ten the answer is "no difference", which is the line most worth having: the question a reader actually has is whether the application is useless to them without a printed sheet.

### A stale record this entry turned up

Changing the printed note changed the printed artwork, which is what gates `scans/phase1/measurements/detection-counts.json`. Comparing then showed **26 of 55 corpus images with different counts**.

**None of it is this entry's work.** The question 40 change from entry 149 was the obvious suspect, so it was measured directly: with the previous detector restored, the same 26 rows differ by the same amounts. The record was last written at entry 101, and entries 130 and 141 changed detection substantially since, each measured and accepted at the time and none of them re-recorded here.

The record is now current. **The gate fires on artwork and not on counts**, which is how four entries of accepted change went unrecorded without anything going red.

## Entry 153: the standard every research article is held to, sections 1 to 4

**Done, all six sections.** Section 5 is complete within the one limit this project has on publishing real material, which is stated below rather than worked around.

### Section 1: the developer is not named

Alan: *"For all of the research documents, I would prefer if my name is not mentioned. Just say the author or developer."*

His name is gone from every file under `website/research/`: fourteen articles, two figure scripts and one data file. The sweep also caught the pronouns, which are the half that would have survived a name search: "the developer photographed his targets", "what he saw", "the calibre he actually shot". Those read as a name to anybody who knows whose project this is.

The byline is one form, used everywhere: **"GroupLab project. Researched and written with Claude. Testing and data collection by the developer."** It replaces a per-page sentence that named him. A byline that varies from page to page reads as carelessness about attribution, which is the opposite of what a byline is for.

**Scoped to that directory**, as section 1.4 asks. His name belongs in the licence, in the commit history and in `samples/PROVENANCE.md`, where it sits under a consent record, and that file is untouched. A test that banned it everywhere would fail on work nobody should change and would eventually be switched off, taking the useful half with it.

### Section 4: a rimfire 22 is 0.222, not 0.224

**The pick list did not have it at all.** It held 0.2215, which is 5.45x39, and 0.224, which is the centrefire 22 of 5.56x45 and 22 ARC. So the most commonly shot cartridge in the world had nothing in the list to choose, and the nearest thing was 0.9 percent too wide. Three cartridges within a thousandth of each other is exactly the shape of thing that looks like a duplicate and gets tidied away, so the test says all three are there and says why.

Two articles reported a rimfire ratio computed against 0.224, and the divisor moved, so they were **recomputed rather than edited**: `photo-hole-size` 0.758 to 0.765 scanned and 1.069, 1.077 to 1.079, 1.087 photographed; `photographing-targets` the same figures to two places. Nothing else in either article changes, because the three centrefire sheets are unaffected and the range quoted for the photographs still holds.

**`AutomaticMarking.HoleToCalibre` does not move, and here is why**, which section 4.3 asks for specifically. It is 0.945, pooled over 102 holes on eight frames of **two sheets** with sheet means 0.949 and 0.932. The rimfire sheet in the same session read 0.758, nowhere near either, and the two sheets behind the constant are the 6.5 Creedmoor pair at 0.264. No rimfire measurement is in that constant, so correcting the rimfire diameter cannot change it.

Question 40's arithmetic in `docs/QUESTIONS-FOR-PLANNING.md` uses .224 against .308, which is correct because it is about two centrefire cartridges. A note now says so in place, or the next sweep will helpfully break it.

### Sections 2 and 3: what the numbers mean, and showing the evidence

Alan: *"they should explain how to interpret the numbers or what they mean, rather than just presenting the numbers"* and *"Being able to visualize something is much easier than just reading about it."*

Every published article now ends with **"What this means"**, and it is about what to do differently or stop believing rather than the result again in words. Two articles already had the section under their own headings, "What this means for you" and "What this means when you photograph a target"; those are normalised to the one heading, because a reader who learns where to look should find it in the same place every time.

**The figure rule has an exemption that somebody had to write.** Several of these articles are about a build pipeline, a list of network calls or what is removed from a file, and there is genuinely nothing to photograph. Rather than a silent pass, the front matter carries `no_figure` with the reason, the page prints it where the picture would be, and the build fails on an article that has neither. A gap where a reader expects a figure reads as something forgotten; a line saying why reads as a decision.

**Applied in the batches section 6 asks for, and the scaffolding is gone.** `STANDARD_153` in `website/build.py` named the articles brought up to the standard and the build failed if one of them lost its section or its caption; an article not yet on it was printed at the end of the build as still to come. The list grew from three to all thirty over four batches, so it has been removed and the checks are unconditional. A new article meets the standard from its first commit rather than joining a backlog, which is the point: a backlog list that outlives its backlog becomes a way to opt out.

### Not done

### Section 5: three real holes, with the measurement drawn on

The article said a hole measures 0.9 to 1.5 times the bullet and showed nobody a hole. It now carries three crops from the sample scan, each with the caliper line across what GroupLab measured and a tenth-inch bar so every other length can be checked:

| crop | measured | of the 0.264 in bullet |
|---|---|---|
| smallest on the sheet | 0.237 in | 0.897 |
| the median hole | 0.251 in | 0.949 |
| largest on the sheet | 0.274 in | 1.039 |

The median crop is the 0.949 the results table quotes for that sheet, so the picture and the number cannot drift. The numbers are not typed into the script: `grouplab analyze` produced them once and they are committed as `hole-crops.json`, which the article also offers for download, and the figure script reads that rather than re-deriving them on every site build.

**What the pictures show that the numbers could not.** The smallest hole has closed back in behind the bullet with no torn edge; the largest has a tear that is inside the measurement. Nothing about the two shots differed. That is the mechanism the article had only asserted.

**Two of the three cases section 5 names are not what it asked for, and the reason is a rule rather than an oversight.** The only real material this project may publish is that one scan, under the consent record in `samples/PROVENANCE.md`; every photograph from that range day stays private. So:

- **The shadow case cannot be shown.** It is the whole finding of question 38 and it exists only in a photograph. The article says so in a line where the picture would be, and **request 6** in `docs/notes/for-alan.md` asks whether one crop of one photographed hole may be published.
- **A synthetic shadow was not drawn instead.** Entry 153 section 3.1 says a chart of simulated data is not a substitute where real material exists, and a simulated hole would be evidence of nothing at all.

### Not done

- **Section 3.1 and 3.2 for the articles that carry `no_figure`.** Each exemption is honest today, and several of them would be better served by a figure that does not exist yet: the six steps of reading a target, the two ways of pooling. The exemption is a statement that nothing exists to show, not that nothing could.

## Entry 160: both sessions spend fewer tokens, without doing less work

Alan: *"I would like going forward is for cowork and code to be more efficient with tokens without sacrificing the quality of research or the application."*

### Section 7, the measurement, first because it is the point

| file | before | after |
|---|---|---|
| `docs/NOTES-FROM-PLANNING.md` | 1,102 KB | 95 KB |
| `docs/PHASE1-RESULTS.md` | 836 KB | 99 KB |
| `docs/QUESTIONS-FOR-PLANNING.md` | 165 KB | 30 KB |
| **total** | **2,103 KB** | **224 KB** |

**A cold start was on the order of 2.1 MB of logs.** It is now `docs/notes/STATE.md` at about 5 KB plus a first read of the live notes file at 95 KB: **about 100 KB, a twentieth of what it was.** In practice it is less again, because `STATE.md` is often the whole of it.

Nothing was deleted. The archive holds 1,021 KB of notes, 749 KB of results and 137 KB of answered questions, whole and unedited.

**To be checked weekly**, and `StateFileTests` fails if any of the three live files climbs back over 300 KB.

### Section 1: the split

`scripts/split-logs.py` does it, and can be run again when the live files have grown back. Running it twice is safe.

- **Notes:** the newest fifteen entries stay, the other 136 are in `docs/notes/archive/notes-2026-09.md`, one file per month, with an index in the live file naming the entry numbers each archive holds.
- **Results:** the gates, the newest ten sections and the decision log stay; 205 sections are banded by the entry they belong to, `results-026-050.md` and so on, with everything predating per-entry results in `results-milestones.md`.
- **Questions:** the nine open ones stay; 38 answered are in `questions-answered.md` and **every one of their numbers is listed in the live file**, so a number is never reused and never lost.
- **`docs/STATISTICS.md` stays whole**, as section 1.4 asks. It is a reference and it is read on purpose.

Verified by counting headings before and after: 151 entries, 47 questions and 217 result sections, all present.

**The fourteen day clause would have moved nothing.** Section 1.1 offers "the last fifteen entries or the last fourteen days, whichever is longer". This repository is eleven days old, so every entry falls inside fourteen days and the clause selects the whole file. The fifteen entry rule was applied, because the section's own first paragraph says what it is for, and the clash is **question 48**.

### The fault the split uncovered

**Entry headings in the planning log drifted from `##` to `#` at entry 119**, and `NotesStatusTests` and `QuestionStatusTests` both look for `## `. So for the last four days **they had been skipping the thirty four newest entries**, passing on the older ones, with nothing going red.

Normalised before anything moved, because a log whose headings are two different things cannot be split reliably either. Then:

- **`Logs.Notes()` and `Logs.Questions()`** read the live file and the archive together, so a test that reads a log cannot quietly stop checking anything as material moves out of the live file. That is the same failure by a different route, and it was worth spending a class on.
- **Waking the test found two folds claiming more sections than they named.** Entry 150's status said "all six sections" and its fold named four; entry 152's said "all five" and named four. Both were done and both folds were incomplete, so both are corrected rather than the claims reduced.
- **The test now reads both fold styles.** Folds up to entry 143 used a numbered heading; folds from entry 144 use a bold run inside a bullet, and `**Sections 3 and 4.**` names two.

### Section 2: `docs/notes/STATE.md`

90 lines. What is in flight, the next three things, what is blocked and on whom, the nine open question numbers one line each, the last nightly, whether the site is current with main, the inbox, and six things that would surprise somebody who was not here yesterday.

**It is rewritten at the end of every run, never appended to**, and it says outright that if it disagrees with the logs the logs are right. `StateFileTests` holds it under 120 lines and requires it to answer what section 2 lists. A state file that grows is a fourth log.

### Sections 3, 4 and 5: the disciplines

In `CLAUDE.md` under "Tokens are the budget". The ones with teeth: run the suites quietly and print the summary and the failures only; never paste test output, build output or file contents into a report, a commit message or a log; never read a file to confirm a write succeeded; never read a whole file to find one thing; one commit per entry; and where a task is mechanical, write a script and run it once.

### Section 6: asked once, not fifty times

The command set that covers ordinary work here is written out in `CLAUDE.md`, so what is pre-approved is on the record rather than only inside a settings file. Request 5 in `docs/notes/for-alan.md` gives Alan the exact file to create and the exact content.

**What deliberately stays behind a prompt** is listed as carefully as what does not: anything with `sudo`, any `ssh` or `scp`, `rm -rf`, `git push --force`, any `git tag`, any repository setting, and anything writing outside the repository. Those are the ones worth reading before saying yes.

## Entry 161: naming the calibre made the reading worse, and the print scale was never corrected

A friend shot `Scan_20260923.png` on 2026-09-23: ten 6.5 Creedmoor shots, 0.264 in, one per bull on bulls 1 to 10. In nightly 93, told nothing, it raised one review item. Told the truth, it raised six, five of them calling a single hole possibly two. **Software that gets worse when it is told the truth has the wrong model in it.**

The scan is a local fixture, recorded in `tests/GroupLab.Core.Tests/Analysis/friend-scan-2026-09-23.json` by its SHA-256 and read from where it sits. Nothing from it is committed here; entry 162 brings its consent record.

### Section 2: the measurement, confirmed rather than taken on trust

| | measured here | the entry |
|---|---|---|
| detection | 10 holes, one per bull, bulls 1 to 10 | the same |
| round marks across the middle | 0.301 in | 0.301 in |
| hole over bullet | 1.14 | 1.140 |
| the reference the calibre made | 0.249 in, 0.264 times 0.945 | the same |
| marks against that reference, by area | about 1.43 | 1.46 |
| flagged as possibly two, calibre named | 5 | 5 |

The false alarms are fully explained by the constant. A hole on this paper is 14 percent **wider** than the bullet, and the constant assumes 5.5 percent narrower.

### Section 3: the sheet's own marks are the reference, named calibre or not

`RenderDifferenceHoleDetector.SizeReference` now takes the reference from the sheet wherever it has five round marks or more, whether or not a calibre was named. **This amends entry 141 section 4**, which set the line at twelve and said that below twelve there was no evidence either way; this scan, at ten marks, is that evidence. The option `MarksToOutrankACalibre` is gone because it no longer decides anything.

- **Five to eleven marks:** the flag is the marks' quarter-point, tentative, and a named calibre is what vetoes a split.
- **Twelve or more:** the marks' quarter-point, trusted, or the smaller group's where there are two sizes, named calibre or not.
- **Fewer than five:** a named calibre keeps a single hole from being split and **flags nothing**. A flag from the calibre alone is the constant again.
- **Where the marks and the calibre disagree by more than a tenth**, the scale panel says so, beside the print scale line: *"These holes measure 0.301 in across, and a .264 in (6.71 mm) bullet would be expected to make about 0.249 in. GroupLab is judging one hole from two against the sheet's own marks, not the calibre."* `DetectionAdvice.CalibreDisagrees`.
- **Question 40's answer agrees.** The two-sizes rule was already the sheet's own reference; it now applies whether or not a calibre was named, and the calibre still vetoes splits there, which is the one job it does that the sheet cannot.

On the friend's scan, with .264 named: ten holes, reference 0.284 in, **no mark flagged**, and the disagreement sentence shown.

### Section 4: the guess names no cartridge

`CalibreConfirmation.Guess` returns the measurement and the question, from a scan or a photograph, and no diameter, no nearest calibre and no buttons: *"These 10 holes measure 0.301 in across the middle. A hole is not the bullet: the reading moves with the paper, the backing and how fast the bullet was going, and on scanned sheets of known calibre it has run from about three quarters of the bullet to more than the bullet. So GroupLab does not guess a calibre from it. Name what you fired."*

This retires Alan's 2026-09-22 requirement that the guess snap to a real cartridge and offer its neighbours, and with it the thing question 39 was about. The snapping tests were rewritten rather than deleted: they now hold that nothing is named, and one of them is the friend's sheet in numbers, which must not be called .308.

### Section 5: the ratio is not a constant, and not even on one side of 1

| scan | cartridge | hole over bullet |
|---|---|---|
| range day, .22 LR | 0.222 | 0.765 |
| range day, 6 ARC | 0.243 | 0.923 |
| range day, 6.5 Creedmoor, 15 shots | 0.264 | 0.937 |
| range day, 6.5 Creedmoor, 25 shots | 0.264 | 0.949 |
| **friend's sheet, 6.5 Creedmoor, about 2845 fps** | 0.264 | **1.14** |

All five are 600 dpi scans, so imaging does not explain the spread; the photograph finding of question 38 did not have that protection. `HoleToCalibre` stays 0.945 and says in its own comment why it was not replaced with 1.14, and what it still does.

**Under entry 158 section 1, this is worth an article**: it would change what a shooter does, which is stop reading anything off a hole's size, and what a developer builds, which is no hole-to-bullet constant. What it cannot yet say is what the ratio depends on, because paper, backing and velocity all changed together between these sheets. That separating test is entry 158 program B's, and the article waits for the scan's consent record in entry 162 so its first figure can be the sheet that showed it.

### Section 6: the print scale, and an answer I got wrong under entry 152

The friend printed with no scaling and GroupLab reported 100.3 percent. Alan: *"I dont think this needs correction because the apriltags are the source of truth."* **For this sheet he is right**: the markers solved 38 of 38 at 0.0035 in, and 0.3 percent is 0.003 in on a one inch group.

**What the code does, which the entry asked me to say plainly:** GroupLab reports every distance in **the sheet's own inches**. `SheetReference.ToTarget` returns page coordinates and nothing multiplies them by the print scale. Which bull a shot belongs to and where it sits are right on any uniformly scaled print, because the markers moved with everything else. Every size is off by the print scale: a sheet printed at 96.2 percent makes a real 0.96 in group read 1.00 in. A scan measures the scale and says so; a photograph cannot measure it.

**Entry 152 said the opposite and I wrote it.** I took the in-app sentence "The measurements are corrected for it, and the figures are right" at its word, and read `Test43APrintAt962PercentReportsItsScale`, which proves bull centres are recovered in the sheet's coordinates, as proof that sizes are physically right. It proves no such thing. The research article `printer-true-size` had the physics right ("a shrunken sheet makes groups look bigger, because the bullets are full size and the ruler they are measured against has shrunk") and I declared its opposite sentence the correct one.

Corrected in every place it was said: `DetectionAdvice.PrintScale`, which now says which way sizes read and by how much; the print screen's own words; the tour's print page; `docs/WHAT-CAN-BE-MEASURED.md`; the article; the state file; and two code comments. The banned sentences in `ClaimsAboutMeasuringTests` now hold the false ones, which were mine, and no longer ban "any other scale measures wrong", which was true. Whether a scan should report real inches is **question 49**.

### Section 7: the rounds fired item

It works as designed and now says where the answer goes: *"Nobody has said how many rounds were fired: type it into Rounds fired at the group and this settles itself."*

### Section 8: tests, and whether this was there all along

`CalibreNeverMakesItWorseTests` reads every local scan with a known calibre, both ways, and fails if naming the correct one ever adds an open review item. It writes this table on every run.

| scan | calibre | holes without, with | open items without, with | doubles without, with | doubles with, before entry 161 |
|---|---|---|---|---|---|
| friend's sheet | 0.264 | 10, 10 | 1, 1 | 0, 0 | **5** |
| range 1 | 0.222 | 14, 15 | 1, 1 | 0, 0 | 0 |
| range 3 | 0.264 | 25, 25 | 0, 0 | 0, 0 | 0 |
| range 4 | 0.222 | 19, 24 | 13, 9 | 0, 0 | 0 |
| range 5 | 0.222 | 20, 20 | 13, 13 | 0, 0 | 0 |
| range 6 | 0.243 | 9, 9 | 2, 2 | 0, 0 | 0 |

The last column was measured on the code as it stood before this entry, in a separate checkout outside the repository. **The defect was latent everywhere and fired once**: it needed fewer than twelve marks and holes wider than 0.945 of the bullet together, and none of the range scans had both.

**Two fixture errors turned up and are corrected.** `range-scan-counts.json` recorded scans 1, 4 and 5 as 0.224, the centrefire figure entry 153 section 4 had swept everywhere else, and **scan 3 as 0.308, where its own load block reads 6.5 Creedmoor**. Every count was re-measured at the right calibre and none moved.

`CalibreSplitTests` gains the synthetic form of the rule, so it runs everywhere: ten marks shaped like the friend's with the calibre's 0.249 in named give the sheet's reference, the calibre as the veto, and the disagreement in the description.

## Entry 168: nightly 94 should not exist, its notes were cut off, and the platform statement leaves the releases

Alan, on nightly 94: *"It looks like nightlies are still getting published when there are no changes to the application. Also the notes are getting cut off. I dont think the 'what is supported' section needs to be added to every release note. Can you go back and fix all of the release notes?"*

### The three answers Alan asked for

1. **What made nightly 94 build:** `scripts/claims.py`, `scripts/counts.py`, `scripts/split-logs.py` and five test files, from my own entries 159 and 160. None of them is in the executable. The gate counted `scripts` and `tests` as shipping by directory.
2. **Past nightlies with no application change, under the corrected classes: 9 of the last 31**: nightlies 14, 30, 72, 75, 76, 77, 78, 81 and 94. Entry 150's gate would have caught five of those; 30, 75, 81 and 94 changed only tests, scripts or workflows, which it counted as shipping.
3. **Release bodies rewritten:** given in the run report, from the rewrite's own count, with three read back from GitHub.

### Section 2: what ships is what the build reads

Three classes now, in `.github/shipping-paths.json`:

- **ships**: generated, not judged. `scripts/shipping-gate.py --generate` asks MSBuild what `dotnet publish src/GroupLab.App` reads, follows its project references into Core and Cli, and adds what the packaging copies, which MSBuild never sees: the licence and notices, the two samples the Windows package carries, the two packaging scripts, `package.yml`, the macOS icon. The result is `.github/shipping-generated.json`, 80 entries.
- **checked**: tests, tools, the build and release tooling, the other workflows. CI runs the suite and a failure blocks main, and **no release is produced**.
- **content**: the website, the documentation, the research, the release notes.

**The build had two surprises.** `docs/VOLUNTEER-PACK.md` ships: Core embeds it. And the eight Linux icon sizes do not: nothing in any package reads them. Both are what the build says, which is the point of asking it.

A folder whose every tracked file ships is one entry, so a new source file there is covered by its folder. CI regenerates the list after publishing and fails if it differs, so a new embedded resource cannot arrive without the gate knowing. An unclassified path still fails.

### Section 3: why the notes were cut off

`read()` kept the line matching `Release-note:` and nothing after it. **The generator's own documented example wraps**, so its own example would have been cut, and every trailer I have written this week wraps. A trailer now continues until a blank line or the next `Key:` trailer, indented or not, joined with single spaces; a commit may carry several notes, each with its own kind.

`--self-test` holds the docstring's own two-line example, a three-line unindented note, two notes in one commit and a trailer ending a note, and `ShippingPathsTests` runs it, failing in CI if Python is missing rather than skipping.

### Section 4: only what ships goes in an application release

The generator includes a commit only where it touched a path the gate classes as ships, and never a `[notes]` or `[screens]` commit. **The meaning is banned, not the phrase**: a note saying the application did not change is refused and names its commit, because entry 145 banned "nothing in this build changes" and nightly 94 said "Nothing in this changes the application". A build with nothing that ships says, once: *"This build has no change to the application; it behaves exactly as nightly NN does."*

### Section 5: the platform statement leaves the release bodies

A release carrying a macOS asset now carries one line made from the statement's own lead sentences and the download page's address, where the whole statement is always current:

> Windows is the supported platform. Linux builds are published and are worth trying. macOS builds are published and have never been run on a Mac. How to open the unsigned macOS build, and the whole statement, always current: https://grouplab.org/download/

That macOS sentence is out of date since the tester, and entry 166 changes the statement; the line follows it, because it is generated from it. The README and the download page keep the whole statement. `MacBuildsTests` asserts the link is there and the section is not.

### Section 6: every published release rewritten

`scripts/rewrite-release-notes.py` regenerates each build against the build published before it. **Nothing is deleted**: no release, asset or tag is touched.

- **30 entries rewritten** in `docs/RELEASE-NOTES.md`. Nine say they changed nothing in the application.
- **Two kept as published, and named in the file**: nightly 12, the first tagged build, which has no earlier build to diff against, and nightly 25, whose trailer uses "manifest", a word today's checks refuse. `0.1.0` is not a nightly and is untouched.
- **Known issues** are kept exactly: 13 before, 13 after.
- **The website had failed to publish on four pushes in a row, from `06b6afb` on**, and I had reported each as publishing without confirming it, which the rule requires within 20 minutes. So entry 153 section 5's crops, entry 159's corrections and entries 161 and 168 were not live. The cause was not in any of them: `docs/RELEASE-NOTES.md` lists nightlies 12 and 14, whose tags and GitHub releases no longer exist, and the check that every listed build has a tag passed on this machine, which still has the two tags, and failed in the site build, which does not. I did not remove them and do not know who did. **Nothing is deleted**: both entries stay, each says its release no longer exists in place of a download link that would lead nowhere, and the check accepts exactly that.
- **Nightly 93 still carries entry 152's false note** that a shrunk sheet measures correctly. It is regenerated as it was written, because a published release is not edited to hide a mistake; entry 161's note in the next build is the correction.

## Entry 162: consent for the friend's 2026-09-23 scan, and what it was shot on

### Section 1: consent, and the one thing not done with it yet

The consent record is in `samples/PROVENANCE.md`, in the same form as scan 3's, with Alan's relayed words quoted exactly, the date 2026-09-24, and the fact that Alan relayed it on the friend's behalf. The friend is not named. **It says in as many words that it is not the 2026-09-16 friend scan**, which is never published, and names each by file and date, because two scans from one person with different consent is the case where one gets published by mistake.

**The scan is not committed yet.** Rebuilt from its pixels with only the resolution chunk, it is still 55,971,430 bytes, because scanner noise does not compress; scan 3 is 16.8 MB. A file in git is in every clone for ever and can only be removed by rewriting history, which this project has done once already. The entry permits publishing and does not require it, so the choice between committing it whole, at 300 dpi, or as a release download is **request 8** in `docs/notes/for-alan.md`. The tests already run it wherever it is on the machine.

The consent record and request 8 were committed inside entry 168's commit, `8270d11`, because they were already written when that commit swept the working tree. Nothing about them changed.

### Section 2: what it was shot on

Card stock, cardboard behind it, 6.5 Creedmoor at 0.264 in, printed with no scaling and measured by GroupLab at 100.3 percent. Recorded in the consent record and in the fixture file the tests read, `tests/GroupLab.Core.Tests/Analysis/friend-scan-2026-09-23.json`.

### Section 3: the ratio depends on paper and backing at least

**3.1.** Where a calibre is still used without the sheet's own marks, it now holds across 0.76 to 1.14 and says so. Only two things still use it: the smallest mark accepted as a hole, which is 0.43 of the bullet and so below the low end, and the split veto, which keeps a single hole whole up to 1.16 of the bullet by area and so includes the high end. `CalibreRangeTests` holds both against `AutomaticMarking.HoleToCalibreLow` and `HoleToCalibreHigh`, and the detection trace now says the range rather than one figure. No constant replaced 0.945.

**3.2.** Paper and backing are recorded fields on a marking, saved and read back with it: paper is copy paper, card stock or other, backing is cardboard, foam board, none or other, both optional and never guessed. A value outside the choices is recorded as not said. They are two optional choices on the marking screen under "Rounds fired at the group". The tour's marking page does not name every field on that panel and still describes it truthfully.

## Entry 163: the first real user's feedback on the marking screen, and a cartridge list

The friend who shot the 2026-09-23 sheet used nightly 93 and sent five points. He is the first person other than the developer to use GroupLab for real.

### Section 1: pan by default, and a click selects

Pan was already the tool on open; **detection switched to select**, which is what he met. Detection no longer changes the tool. The pan tool now selects a mark on a click and pans on a drag: a press becomes a selection only when it is let go within four pixels of where it began, so a drag that starts on a mark pans and never moves the mark, which is entry 143's rule that a stray drag must not move a measurement. Only the select tool drags shots. The middle button pans in every tool, as it did.

**Two finger drag, scroll and pinch are not decided here.** Entry 166 section 3 corrects this section's rule, because on a Mac trackpad a two finger drag is a scroll, and it asks for the behaviour to be measured per device first. That is done under entry 166.

### Section 2: pan and select on neighbouring keys

**C pans, beside V, which selects.** V is the convention in most design tools, C sits beside it under the left hand, and C was free: it is not a review key (Space, Enter, T, N), not bull entry (digits and S), and not a modified shortcut. P still pans, so nobody who learned it is broken. The keyboard strip shows both, and the user guide's key list says so.

### Section 3: cartridge names, matched before numbers

**This reverses entries 107 and 108, and says so where their rule was written**, in `docs/CALIBRES.md` and in the tests that held it. Typing 6.5 now offers *"6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and others: 0.264 in (6.71 mm)"* first and, under it, *"not the same as .25 calibre, 0.257 in (6.53 mm)"*. It used to offer .257 because the pick list was filtered by "contains", and "6.53 mm" contains "6.5".

**Every diameter is checked against two independent published sources**, as the section asks, and the table records which:

- **A**: Wikipedia's table of handgun and rifle cartridges, which cites SAAMI and CIP.
- **B**: Nosler's load data index, which files each cartridge under its bullet diameter from the bullet maker's own load development.

**Forty cartridges in fourteen families are confirmed by both. Thirty three are held**, offered nowhere, until a second source agrees. They include 6.5 PRC, 6mm Creedmoor, .22 Long Rifle, 7mm PRC, .300 PRC, .303 British, .44 Magnum, .45 ACP and .50 BMG, which are exactly the ones a shooter types most, so this is the list most worth a second source next. Typing one of them works as before: GroupLab asks for the diameter.

**The two sources disagree in three places**, and each disagreement keeps the cartridge out rather than picking a side: 7.62x39 is 0.312 in A and 0.310 in B; A gives 7.62x54R as 0.308, which is not what it is loaded with, and B does not list it; and A gives .22 LR as 0.223 against entry 153's nominal 0.222, with no second source either way. The planning session's seed table, written from memory, had 7.62x54R at 0.311; A says otherwise, which is the reason a seed is not a source.

Two sources were tried and failed: a Graf & Sons chart, whose text is an image inside its PDF, and a reloading page whose certificate the fetch refused.

**Choices the section left open, taken and stated:**

- A family name counts as a name even with a leading point where it is a designation: ".38" is 0.357 in and ".270" is 0.277 in, which is the very case entry 108 existed for.
- **".223" is not a name**: with its leading point it is a diameter under rule 3, and 0.223 in is a real .22 LR figure in source A. "223" without the point is .223 Remington.
- Family shorthands never point at a held cartridge or a trap: "7.62" could be 7.62x39, "25" could be .25 ACP at 0.251 in, and "9" could be anything from 9mm to 9.3.

`CartridgeTableTests` holds one case per row of section 3.2 that the table can offer, checks the held rows are offered at no diameter, and checks every offered cartridge names two sources.

### Section 4: what has to be filled in goes at the top, and says so

A **Setup** block is the first thing in the panel, above the review: calibre, shot distance and rounds fired, then rifle, barrel and load, "Same setup as the last target", and the paper and backing from entry 162. Each says in a line what it unlocks, worded after entry 161 so the calibre is not claimed to change which holes are flagged. An empty one that matters is outlined in the alert red **and** says "needed", because colour alone fails a colour blind user. Each has **Not known**, which is an answer and clears the mark. **Accept and analyse** says what is still needed in one line, whether it stops for the calibre or goes ahead without the distance.

### Section 5: the analysis screen explains less by default

The explanations were already behind "why" toggles that remember being opened. What showed by default was each judgement card's evidence and the extra CEP lines. A card now opens as its verdict alone, with its evidence and reasoning behind the verdict's "why", and CEP shows its first line. Nothing is removed. `NothingIsCutOffTests` now runs with every explanation closed and with every one open.

### Section 6: what a Mac changes

- **Shortcut modifiers and the trackpad**: entry 166 sections 2 and 3, which name the cause, every handler reading `KeyModifiers.Control`.
- **The application menu**: the tester reports Command Q quits.
- **The updater** offers a macOS build no update: there is no macOS asset in the manifest, and the update step says updates are manual on macOS and where to download.
- **The platform statement is stale**, as the section says, and is rewritten under entry 166 from what was actually checked.

### The tour and the guide

The tour's marking page named neither the setup block nor the new keys nor that pan selects. It does now, and its steps start with the setup. The user guide's key list says C or P. Both guide PDFs are regenerated; the testing guide's had been stale since entry 159's wording change.

### Two tests I broke under entry 162 and found here

`c8bbf9e` was committed after running only its new tests. Two older tests failed on it: the detection trace's calibre wording, fixed in `c09ad1e`, and the bench coverage test, which wanted `TargetMaterial` either benched or named as not worth benching, fixed in this commit. **The rule this repeats is to run the full Core suite before every commit**, not the tests that look related.

## Entry 167: the Equipment icon is a tilted cartridge

Alan: *"The equipment icon is cursed and needs to be replaced. Was it supposed to be a rifle? If so, it is bad and looks like a lego rpg."* It was a rifle, and at 16 pixels a rifle's long thin shape leaves no room for proportion.

The new `Icons.Equipment` is the planning session's baked path exactly: a bullet and its case with the gap between them that makes it read as loaded, scaled to 97 percent and turned 40 degrees in the path itself rather than at run time, because the baked path is the one checked for clipping. Its bounds are 0.96 to 12.74 across and 2.43 to 15.58 down, inside the square. Rendered at 16, 32 and 128 pixels in both themes, it reads as a cartridge at every size.

`EquipmentIconTests` holds it inside the square with its two parts, and checks the rail draws it in the theme's foreground rather than a colour of its own, so the contrast the theme tests hold for the rail holds for it in both states. The weekly screenshot job and the tour pick it up on their own; the Equipment tour page is to be checked after the next run.

The Ballistics icon's summary comment had sat above Equipment's, so each read as the other's; each is above its own now.

## Entry 171: a scan reports real inches, and the stale items closed

### Section 1: question 49, option 3

**On a scan, every distance is real inches.** `SheetReference` carries the measured print scale, and `ToTarget` multiplies by it, so
bull centres, shots, groups and point of aim all scale together, and a hole's diameter is reported in real inches too. The detector
itself works in the sheet's inches, so a named calibre reaches it divided by the scale: a 0.308 in bullet is 0.320 of a 96.2 percent
sheet's inches. Two choices the entry left open, taken and stated:

- **The scale is applied by area, uniformly.** It is the one figure the screen reports, and a scanner's own x and y differ by more than
  a printer's do.
- **Outside 85 to 115 percent, nothing is corrected.** A stated resolution that puts the sheet there is more likely wrong than the
  print, a resampled scan or a screenshot saying 96 DPI, and multiplying by it would make every figure wrong. The screen says so.

**On a photograph**, the figures stay in the sheet's inches and the results panel says, word for word from the entry: "Measured in the
sheet's own inches; if the sheet was not printed at actual size, the figures are off by the same percentage." The reason to print at
actual size is `DetectionAdvice.WhyActualSize`, which the print screen uses directly; `WHAT-CAN-BE-MEASURED.md` and the tour's print
page carry the same sentence, and `ClaimsAboutMeasuringTests` holds all three to it. The sentences entry 161 banned for saying the
figures were corrected are unbanned where they are now true, and the ones that said nothing is corrected are banned instead.

**Tested both ways round** in `ImperfectSheetTests`: a sheet printed at 96 percent, scanned, measures the two holes furthest apart
within 0.2 percent of their true distance on the paper; the same pixels with no stated resolution, as a photograph, measure them
1/0.96 large and say why; and the scan's holes measure 0.96 of the photograph's. The marking file records `"inches": "real"` or
`"sheet"` and the scale, and reopens with it.

### Sections 2 to 5

- **Question 48.** The split script's comment now says the fourteen day clause is dropped. It never did anything.
- **The community page** lists every channel under its category and the ten rules, from `website/links.json`. `DiscordLinkTests` checks
  the five categories, the ten rules, and that no moderators' or staff channel is named.
- **Questions closed:** 39, 41, 42, 45 and 46, with a pointer to entry 143 or 161; and 48 and 49. **Question 44 keeps one open part**:
  `SurfaceMapping.ToPage` throws at a point outside the page, recorded by `SurfaceCrashTests` and reachable only through
  `compare-photos --model surface`.
- **STATE.md** is rewritten, and `StateFileTests` reads its `**Holds:**` line and the inbox directory and fails when they differ.

### Section 6: the requests

- **Request 6.** Alan's standing consent is in `samples/PROVENANCE.md` with the exact words, the date and the exception clause. It does
  not reach anything a friend shot. **The shadow crop is published**: the photographs of the 6.5 Creedmoor 15 shot sheet were told
  apart from the rest by running the analyser over the range folder into the scratchpad, fifteen holes at a median 0.383 in. The crop
  is shot 1, the median hole, 0.383 in and 1.452 of the bullet, cut from the stored pixels at the local 293 pixels per inch, turned
  as GroupLab displays it, and saved with no metadata. Only its 220 pixel square is committed. Seven research articles whose reason
  for having no figure was "not published under a consent record" now give their real reason instead.
- **Request 7.** Fenix. The credit is placed under entry 166 section 5.
- **Request 8.** `scripts/test-data.py rebuild` made the published copy: the original's pixels byte for byte and its resolution,
  nothing else, 59,215,934 bytes, SHA-256 `c2b2e595...` It is larger than the 56 MB the request quoted because PIL's best compression
  is not the original's. `ci.yml` creates the `test-data` release once, as a pre-release that is never latest, and the nightly's pruning
  only touches tags shaped like a nightly. CI caches the file, fetches it by URL on a miss, and checks its hash before a test reads it.
  `TestDataTests` fails if a file over about 10 MB is committed, other than scan 3, which predates the rule.
- **Request 1** now says exactly what is left of entry 129: opening the page, the end to end test, the waiting submissions and the
  redirect, with its commands written out. Entry 173 then changes the address to `grouplab.org/targets/`.
- **Request 5** is marked as being applied, with the planning session's correction to the push rule.
- **The installer's closing lines** say the Turnstile secret is present, from the file's size without opening it, and print the nginx
  steps only when this run wrote the include. `SiteSyncTests` holds it.

## Entry 173: the target upload page is open, at grouplab.org/targets

**Opened.** `"open": true` in `website/api/limits.json`, with entry 171's record that the server side is finished and Alan's check that
the Turnstile secret is present. The build now writes the page, the two receivers and the page's script.

**One address.** The page is `/targets/`. `/shoot-a-target/send/` answers with a plain page saying it has moved and linking there; I
chose that over a 301 because it needs no nginx change and a reload of Alan's, and it moves nobody on its own, which entry 151's rule
is about. The page now says in plain words that a photograph is kept on the server until the developer has read it and then deleted
from the server.

**The top bar.** While `open` is true, "Shoot a target" becomes **"Send a target"**, to `/targets/`, in both the desktop bar and the
phone menu. The footer keeps "Shoot a target", and the donor pack page gains a "Send your target" button at the top as well as the one
beside step 5. While `open` is false, `nav()` returns the bar exactly as it was.

**Checked before publishing.** `send_problems()` in the site build fails the build if the bar offers the page while it is closed, if
any page's bar still says "Shoot a target" while it is open, if the page is missing while the link is there, if the old path does not
link to the new one, or if the page's consent differs from `limits.json` or leaves out what happens to a photograph.
`SendATargetTests` holds the same against the built site.

**Everything else that named an address.** The application's printed volunteer pack told people to upload at `pissinhot.com/targets`;
it and the README now say `grouplab.org/targets`, and `docs/WEBSITE.md` names the new path. The community page, which I had pointed at
a `/send/` that never existed under entry 171, links to the page while it is open. The website's donor pack PDFs name no address and
are not regenerated, as section 2.5 says.

**The end to end test waits on a person.** A script cannot pass Turnstile, which is its point, and `Get-TargetSubmissions.ps1` and
`Remove-ReadSubmissions.ps1` both run `sudo` on the server, which this session does not. So the test image is generated and labelled
"Not a target", and request 1 gives Alan the three steps: send it from a browser, pull it with the command given, and then I verify it,
mark it read and hand him the removal command. `Remove-ReadSubmissions.ps1` gained `-RemoteRoot`, because it could only ever delete from
pissinhot.com's two folders and would have left every grouplab.org submission on the server.

**Entry 171's `.user.ini` check.** The deploy of `9ade3bf` was the first that changed the site since the new exclusion. Read over SSH
without sudo: `public_html/.user.ini` is still there, owned by airwolf, 2383 bytes, unchanged since the install.

## Entry 164: the first macOS log, and the two questions it raised

A friend ran nightly 93 on a MacBook Pro with an M5 Max and sent a diagnostics report. Native Arm64, not Rosetta; the sample opened, identified itself and read 25 holes on 25 bulls; the session saved; the updater refused as intended; no errors or warnings. Nothing here needed the tester to answer.

### Section 2: 33 of 34 markers is the sample, not the Mac

**Windows reads 33 of 34 from the same file**, at 0.0026 in, exactly as the Mac did, so it is not a platform difference. The one missed is **marker 28**, 0.512 in from the left and 9.142 in down, above the load block. It is printed with horizontal white streaks across it, the printer's banding, so its black border is not a closed square and the detector finds no quad there. No hole is near it. It is recorded with the sample's ground truth in `samples/sample.json`, and `SampleMarkerTests` holds it at 33 of 34 with marker 28 missed. **CI runs that test on Windows, Linux and macOS on every push**, which is the three-platform comparison the section asked for, repeated rather than done once.

### Section 3: the Mac is faster, and the comparison was two different files

The 3,649 ms in `docs/PERFORMANCE.md` is **a different scan**: a 600 dpi sheet with no holes in it and four markers unread, where hole detection is 2.1 s. The sample has 25 holes, and hole detection on it is 4.7 s. The comparison was never like for like.

Timed on the Windows baseline machine, same file, three runs: 7.5, 6.9 and 6.9 s for every stage. The Mac's figure is the application's `detect.run`, which starts after the image is decoded, so the matching Windows figure is those less the 380 ms decode: **about 6.5 s here against 5.4 s and 4.8 s on the M5 Max.** The Mac is about a fifth faster on the same file.

The three things the section asked to check, each answered from the repository:

1. **No x86 intrinsics anywhere in `src`**, and no explicit vectors, so there is no scalar fallback on Arm64 to remove. The heavy work is OpenCV's native code, which has its own Arm paths.
2. **Neither the Windows nor the macOS publish is ReadyToRun.** It is not a difference between them.
3. **The second run is faster on both**: 12 percent on the Mac, and about 8 percent here, which is start-up and just-in-time compilation.

### Section 4: a report carries no file names

**Decided: the hash and the extension only.** The log recorded an opened image's name beside its salted path hash. A report is saved and shared by hand, so the risk is small, but a file name can carry a person's name or a place, and nothing the log is for needs it: the hash ties one report's lines to the same file and the extension says what kind of file it was. Somebody helping can ask for the name, which leaves it the owner's to give. `DiagnosticLog.File` now records `ext` and `pathid`, amending entry 41 section 3, and `docs/CRASH-REPORTING.md` says so. The two log tests check the name is gone, not only that the extension is there.

## Entry 174: the upload page refused every photograph

**The fault.** The send page's file input was `name="photos"`. PHP builds the per-file arrays `$_FILES['photos']['name'][0..n]` only for
a field whose name ends in `[]`; for a plain name it keeps one file and makes `name` a string. The receiver checks `is_array(name)`, so
every submission, one photograph or ten, was answered "No photos were attached to that submission." It was live from entry 173's
`423e1c2` until this commit. **Nothing was lost**: the receiver refuses before it writes, so no photograph reached the server.

**Why 31 receiver tests passed.** They build `$_FILES` themselves, in the array shape the receiver hoped for, so they tested the receiver
against its own expectations rather than against what PHP makes of the real form. My entry 173 checks tested the page's words and links,
not whether the form and the receiver agreed on a field name.

**The fix and what now holds it.**

1. The input is `name="photos[]"`.
2. The receiver turns the single-file shape into a one-element array before its checks, so a form that sends one file without brackets
   is taken rather than refused.
3. The site build reads the file input's name out of the page it has just built and fails if it does not end in `[]`, and
   `SendATargetTests` asserts the same.
4. `tests/php/multipart-tests.php` serves the real receiver under `php -S`, sends real multipart bodies built from the page's own field
   name, with Turnstile faked by a file as the receiver tests fake it, and checks one photo, two photos and the plain name all reach
   quarantine. That is PHP's own parsing under test, the part that failed. CI runs it on Linux after the receiver tests.
5. `crash-report.php` reads one file named `report`, which is what the application sends, and it checks that shape, so it is not affected.

**Published at 08:54 UTC, nineteen minutes after the publish, and why it took that long.** The server installed the parcel four
times and rolled it back each time, because its live check read the old home page. The parcel was right, its home page carried the new
commit. nginx on the server has `open_file_cache_valid 60s` with `inactive=30s`: a file served twice within half a minute keeps being
served from its old handle for up to a minute after the web root is swapped, and the sync's check gave up after fifteen seconds. I was
the steady reader, polling the home page every twenty seconds to see the deploy. When I stopped, the next sync installed it. **The
lesson for me: watch a deploy through the sync log or a single request, never by polling the home page.** The repository's sync now
checks for seventy seconds and `SiteSyncTests` holds it above nginx's minute; request 10 puts it on the server.

## Entry 170: the zero's distance, two freezes, and where a hole's centre is

### Section 1: the zero correction

The numbers on his screen were right for 25.4 yd: 0.221 in there is 0.83 MOA, 0.241 mil, two 0.1 mil clicks leaving 0.04. What was
missing was the distance. The verdict now ends "for a zero at 25.4 yd". Where the rifle names a zero distance that differs, a second
line gives the correction for that zero: `SolverUse.ToZeroDistance` subtracts where a correctly zeroed rifle's bullet should be at the
distance shot, below the aim at 25 yd for a 100 yd zero, and carries only the rest, elevation along the solver's path and windage in
proportion to range. Where the records lack sight height, velocity or BC, the line says the correction is for the distance shot and names
what carrying it needs. **What said 100 yards:** nothing on the screen. The one real hazard found was the distance box, which took a typed
number only when Set was pressed, so a distance copied from the last target could stay in use while the box showed another; Enter and
leaving the box now take it.

### Sections 2 and 3: the freezes

| step on the friend's scan | before | after |
|---|---|---|
| one refresh | 1.3 s | about 0.1 s |
| naming bulls 1 to 10, These ones | 2.5 s | 0.2 s |
| excluding a shot | 5.5 s | 0.18 s |

What it was, found by timing rather than reading: `GroupStatistics.HoytCdf` doubled its rule until two sums agreed to 1e-16, below a
double's rounding, so it ran to a million points, and the correlated normal CEP called it through a Newton loop that asked for 1e-15; four
CEPs took 1.15 s and the full figures table asked for twelve. At 1e-14 and 1e-13 they take 2 ms, every shown digit unchanged. The simulated
worst-shot distribution and the circular aspect's median depended only on the shot count and were recomputed every edit, 0.11 s each; they
are cached by count, identical values, and the counts one edit away are worked out on a background thread. "These ones" refreshed twice.
The rule solve itself was never the cost.

### Section 4: where the centre of a hole is

| scan | reported against edge-fitted centre, mean | extreme spread, reported / edge | mean radius, reported / edge |
|---|---|---|---|
| friend's 2026-09-23 | 0.0057 left, 0.0091 low | 0.4217 / 0.4504 | 0.1098 / 0.1133 |
| sample, scan 3 | 0.0074 left, 0.0062 low | 0.8491 / 0.8130 | 0.2313 / 0.2264 |
| range scan 1 | 0.0080 left, 0.0108 low | 0.8358 / 0.7991 | 0.2462 / 0.2410 |
| range scan 4 | 0.0079 left, 0.0113 low | 2.9536 / 2.9045 | 0.6807 / 0.6784 |
| range scan 5 | 0.0137 left, 0.0075 low | 5.3055 / 5.3374 | 0.8705 / 0.8732 |
| range scan 6 | 0.0084 left, 0.0089 low | 1.3320 / 1.3255 | 0.4335 / 0.4335 |

**It is systematic.** The same direction on every scan, which is the direction of the shadow the lamp throws: the detector's centre is
the residual-weighted centroid, and the shadow differs from paper far more than the lid seen through the hole, so it carries the weight.
A shift the same for every hole moves the group's centre, and so the zero correction, by about 0.01 in, and changes the size figures by
up to 0.04 in where the extreme holes lean differently. On the friend's scan the edge-fitted extreme spread is 0.029 in larger, which is
the 0.11 MOA he found by moving the holes.

**Not adopted yet, and why.** The edge fit moved some synthetic holes, whose centres are known, by up to 0.039 in; the plain area centroid
passed the synthetic tests but was no closer to the edges on the sample. Neither is ground truth, and choosing between them without a
person's clicks is how the weighted centroid was chosen. Request 9 asks for the same scan marked twice by hand; question 51 decides by it.
`HoleEdgeFit` stays as the measuring tool, with a drawn-hole test that shows the mechanism, and `HoleCentreAgreementTests` holds today's
agreement on the sample and the friend's scan, 0.0080 in and 0.0069 in, so a change that makes it worse fails.

### Entry 149 section 3, done here

A: a row or a column is chosen from one clicked bull. D: question 50, because the offset cannot be found without being told the bulls.

## Entry 172: the ST-4 target, its ground truth, and what GroupLab can do with a sheet it did not print

**The ground truth** is in `tests/GroupLab.Core.Tests/Analysis/st4-2026-09-20.json`: twenty groups, seventeen of five and three of ten,
115 shots of 6.5 Creedmoor at 100 yd, each aimed at its own orange tab or diamond point, positions in grid inches read off Alan's
annotated photograph, the 15 and 100 primer mix recorded as unlocated, and all nine frames and the annotated image by SHA-256. The two
extra burst frames are the same sheet: `185944` whole and near square on, `185946` from further back with the board around it.

**Section 3.3, a sheet GroupLab did not print, as it stands.** A person can open the photograph, set the scale by two taps and a length
or by the four corners of a known rectangle, which the one inch grid makes easy, set one point of aim, and click each hole. What the
application cannot do on this sheet:

1. **More than one point of aim.** Bulls, and so aiming marks, come only from GroupLab's own definitions, so twenty groups at twenty marks
   are twenty separate markings of the same photograph: the scale set twenty times and 115 holes clicked, about 215 clicks.
2. **Find the holes.** There is no hole detection for a sheet GroupLab did not print; `NeutralDarknessHoleDetector` exists only as a
   research spike.
3. **Read the grid.** A printed one inch grid is the best scale and perspective reference a photograph could have, and nothing uses it.
4. **Correct perspective or the lens** on the known-length path; the rectangle path corrects perspective only.

These are what entry 157 section 4 and entry 158 program A are for, and they are why the measurements of section 3 and the zero-offset
check of section 2 item 4 wait for them: each needs the position of every hole on this sheet, and today those come only from a person
clicking 115 of them.

## Entry 166: Command on a Mac, pinch zoom, and what the first Mac run checked

**Command Z.** Every shortcut read `KeyModifiers.Control`, and a Mac's Command key arrives as Meta. The application now asks one
place, `CommandKey`, which takes Avalonia's platform hotkey configuration and falls back to Meta on macOS and Control elsewhere. Undo is
Command Z without Shift; redo is Command Y and Shift Command Z, which is what a Mac user presses. The review keys ignore any modifier but
Shift, so Command with a number is never typed as a bull's label. Every label that named Ctrl, the undo and redo tooltips, the key strip,
the menu's New target and Paste, the empty sheet's paste line and the clipboard message, now shows the Command symbol on a Mac. A source
test fails on any direct read of the Control key, or a string saying Ctrl, anywhere in the application outside that one class.

**Undo says what it will undo.** The buttons are disabled when there is nothing to take back or put back, and the tooltip names the step,
"Undo: move shot 6". The words are read from the two markings either side of the step, by the label the shot wears on the sheet, so no
operation has to describe itself.

**Pinch, scroll, and what Avalonia delivers.** Read from Avalonia 12.1.2 as shipped, not measured on hardware:

| input | what arrives |
|---|---|
| Mac trackpad pinch | its own magnify gesture; the native library handles `magnification` |
| Mac trackpad two finger drag | a wheel event with fractional steps; the native library reads `hasPreciseScrollingDeltas` and scales precise and line steps differently |
| Mac mouse wheel | a wheel event, in steps the same code scales from lines, so not whole units |
| Windows wheel notch | a wheel event of exactly one unit; the Windows backend divides by 120 |
| Windows precision touchpad two finger drag | wheel events in fractions of a unit |
| Windows precision touchpad pinch | Control with a wheel event: the Windows backend has no gesture handling, and Windows sends a pinch that way to an application that does not ask for it |
| touch screen pinch | Avalonia's pinch recognizer, a scale against where the fingers started and the point between them, touch pointers only |

No wheel event carries a pointer type, so on Windows and Linux the size of the step is the only thing that tells a wheel from a touchpad,
and on a Mac it tells nothing, because a mouse's steps are fractional too.

**Chosen.** Command or Control with any scroll zooms everywhere, which also makes a Windows touchpad's pinch zoom. On a Mac a plain scroll
pans, trackpad or mouse, and the trackpad's pinch zooms about the pointer. On Windows and Linux a whole notch zooms, as it always has, and a
fractional scroll pans, so a touchpad's two finger drag pans; a free-spinning or high resolution wheel will pan too, and Control with it
still zooms. A touch screen's pinch zooms about the point between the fingers. Every scroll, magnify and pinch goes into the detailed log
with its numbers and what it did, so request 16 measures a real Mac instead of this table being trusted.

**The platform statement** says what the tester checked, that the two defects are fixed in builds after nightly 94 and not yet checked on
a Mac, and that the Intel build has never been run on one. The download page's Apple silicon card says it has been run on one real Mac;
the Intel card still says untested. The line in the statement that said the updater never offers Mac builds would not have been true:
it tells a Mac user a newer build exists and does not install it, so the statement says it does not install them. The user and testing
guides say Command on a Mac and how scrolling and pinching move the sheet, and both PDFs are regenerated.

**Not done.** The claims register line waits on entry 159, which creates the register. The thanks waits on request 16: there is no list
of testers to add him to, and no name is invented.

## Entry 165: sending a target to the project, built and switched off

**What a person will see, once it is switched on.** After Accept and analyze, a panel at the foot of the figures asks "Help improve
GroupLab's detection?" with Send, Not this time and What gets sent. Send cannot be pressed until a consent level is chosen, testing only or
may be published, in the words of `limits.json`; Not this time is final for that target. The first time GroupLab opens after the receiver
does, one screen asks once: Send every target automatically, Ask me each time or Never, nothing preselected, and automatic sending refused
until a level is chosen. Settings has its own Sending targets section reading and writing the same setting, with what is sent, the
references sent from this computer and support@grouplab.org, and anything waiting with Send them now and Discard them.

**What is sent.** One multipart POST: the image as the file field and one JSON package, schema `grouplab-app-submission-1`, with a
manifest naming the image by size and SHA-256, the consent level and its text, and the parts detected, corrected, told, analysis,
environment and log. The corrected list marks each shot kept, moved, reassigned, excluded or added, and lists what was removed; a test
rebuilds the person's final marks from it exactly. The image goes through the scrubber that already cleans photographs for publication,
so location, time and serial are gone and the pixels are the original's; a file the scrubber does not rewrite is saved again as PNG
without loss, and one too large even then is not sent, with the reason.

**The receiver**, `website/api/app-submission.php`, has no Turnstile and no key, and says why in its own header, as the crash receiver
does. It has a 30 MB image and a 4 MB package limit, 10 an hour and 40 a day for each address, the upload page's hourly cap and disk floor,
and a kill switch file. The package is checked against its manifest and stored in the same quarantine, with the parts in `meta.json`
under `app` and `source: app`, so the worker takes it unchanged. The upload page now records `source: web` and the level. Consent is
`consent_v2` with two texts, and the page offers the two levels as a choice. A testing only submission gets `DO-NOT-PUBLISH` from both
receivers, `CONSENT.txt` from the pull script, and a refusal from the intake that feeds `samples/`, the research build and the site.

**When it cannot go**: no answer, a limit or a closed receiver keeps the package beside the settings file, tried again at each start for
seven days and then let go with a line in the log; a refusal that retrying cannot fix is let go at once and the reason shown.

**Switched off.** `appOpen` is false. Nothing is shown and nothing is sent while it is, which a test holds, and the guide, the tour and
the "what GroupLab sends" article say so. The receiver publishes with the site, and the live one answered an empty post with its own 400 through the server's nginx as it stands; question 55 asks when to switch it on.

**Tests.** Core `TargetSendingTests`, 3, and an `IntakeTests` case; App `Entry165Tests`, 6: nothing without a yes, Never and Always,
Not this time, nothing while closed, the first run screen and Settings, kept then sent. `receiver-tests.php` gains the consent_v2 checks
and the application receiver's refusals, over size, over rate, a malformed or mismatched manifest, closed; `worker-tests.py` takes two
application packages through the worker. App 281 passed; Core 1620 passed, 2 skipped.

## Entry 158: two research programs, and what is worth an article

**The standing rule** is in `CLAUDE.md` and `docs/RESEARCH.md`, "Worth an article?": would it change what a shooter does or a developer
builds. Question 38 and entry 130 are already articles; question 44 is not written, because the bent-sheet model helped the bulls and hurt
the holes on seven of seven photographs; entry 157's off-square finding is worth an article once request 18's steeper photographs say where
it stops working.

**Program A, on the ST-4** (entry 172 replaced step 1 with it), `grouplab st4`:

- **The sheet's own grid registered.** A new `GridRegistration` finds the printed grid's crossings, votes for the lattice's step by the
  crossings each step joins, and walks the lattice. It registered eight of the nine frames.
- **The lens terms lower the crossings' residual on every frame**: close ups from 0.018 to 0.044 in down to 0.012 to 0.029, whole sheet
  frames from 0.018 to 0.050 down to 0.013 to 0.045. This is entry 172 section 3 item 4.
- **Detection on overlapping holes, step 2**: the scan detector finds 54 of the 400 shots in view as a mark of their own, about one in
  seven. A five shot group reads as one or two merged marks 0.3 to 0.6 in across. Two close ups and the oblique frame misaligned their
  lattice to the groups and are not to be read.
- **The same group across frames** agreed within 0.006 to 0.17 in where the frames aligned.
- **Steps 3 and 4 wait** for every shot to be placed. Request 19 asks for a scan of the sheet.

**Program B**:

- **Step 1** is already articles 1 and 2: 0.765 of the bullet for the .22 LR against 0.92 to 0.95 centerfire.
- **Step 2.** The confounds: speed, nose, lead against a jacket, width.
- **Step 3.** The test that separates them, sized by the per-hole spread: 50 holes a cartridge on two sheets, the subsonic set beside the
  supersonic.

Both are in `docs/RESEARCH.md` and in article 1's "What we still do not know". The article waits for the data; request 20 asks for it.

**Tests.** `GridRegistrationTests`, 2: a grid drawn through a known lens with a thick bar across it is found, counted without a skip, and
fitted, the lens's residual under half the homography's. App 275 passed; Core 1617 passed, 2 skipped.

## Entry 157: how the mobile application takes the photograph

**The specification**, `docs/MOBILE-CAPTURE.md`: the capture screen's conditions, one instruction at a time and the outline that
snaps, the flash, the lens, distortion and perspective, what is recorded, and targets GroupLab did not print. Each requirement names the
test that holds it, or the test the mobile work must write.

**Built on the desktop, in `src/GroupLab.Core/Capture`, and measured with `grouplab capture-check`:**

- **The paper's corners.** `SheetOutline` finds the paper by its brightness, splitting again where a light board surrounds it, and refines
  each edge to a fraction of a pixel. On a rendered sheet photographed through a known camera the worst corner is under 0.3 px from 0 to 70
  degrees. On 85 real photographs it found the one whole sheet on a darker mat and nothing on Alan's white board: the light across a sheet
  varies more than paper and board differ. On the marking screen, **Find the paper's edges** places the corners for the rectangle scale.
- **The off-axis angle, its limit and the refusal.** `CameraGeometry` reads the angle from the page-to-image homography with the
  camera's focal length, or one solved from the sheet above 20 degrees. The markers give it within 0.05 degrees to 65 degrees in the
  sweep. On the range photographs 31 of 59 registered, at 3 to 35 degrees. Of the 12 fully framed ones measured against their scans:
  - the bull centers' median error rose from 0.0024 to 0.0076 in square on, to 0.0088 to 0.0138 at 27 to 32 degrees
  - the holes' error did not change

  The synthetic sheet keeps its bulls within 0.001 in to 60 degrees and cannot register at 70. **The limit is 40 degrees**, and the
  refusal names the angle and the limit; question 54 asks planning to read it, and request 18 asks for steeper photographs.
- **The quality score**, 0 to 100 and shown as good, usable or poor, is the least of focus, exposure, angle, resolution and markings read.
  Its formula is in the document to recompute by hand. On the 31 range photographs:
  - good 10, usable 6, poor 15: set by the angle on 7, blown-out paper on 6, missed markers on 2
  - the blown out include the two photographs of scan 1's sheet that matched 5 of its 14 holes
  - focus decided none of them
- **Lens distortion from the markers** was built in Phase 0. On the 12 paired photographs it halves the worst bull error: a median of
  0.029 in against 0.054 with a plain homography, and better on 11 of 12.
- **Kept with the photograph.** Every photograph that registers keeps a `CaptureRecord` in its marking: lens, focal lengths, angle,
  correction, radial terms and quality, never a place or a time. The registration line on screen says how far off square it was and how
  good it is.

**Tests.** `CaptureTests`, 18, and `Entry157Tests`, 2. App 275 passed; Core 1615 passed, 2 skipped.

**Also.** a70338a fixed entry 156's red Linux tarball check: the ships list is generated from the files git tracks, and the two new
files were not yet added.

## Entry 156: hit probability from the shooter's own dispersion

**The model**, in `HitProbability` and written down there and in `docs/STATISTICS.md` section 12.6. Each simulated shot is the sum of
independent contributions. The dispersion and the muzzle velocity's spread are drawn per shot. The wind call, the range estimate, the
zero, the drag, the air, the shot angle and the Earth's rotation are drawn once per string. Each reaches the target through the solver:
the trajectory with that input moved, less the believed one, with the dial set from the belief and the zero angle held, fitted by a
quadratic either side. Sigma is drawn for every string from its own sampling distribution at stratified quantiles. The group's velocity
share is taken out in quadrature before the velocity goes back in per shot. The second round is the second shot's per-shot error less
the first's, because the whole miss is dialed off. A seed makes every run repeatable, and every run draws the same random numbers in the
same order, so each source's cost is a clean difference.

**On the screen**, a Hit probability section on Ballistics below the group carried to another distance, whose own analytic hit line it
replaces:

- **Rifle precision** from the group open in the analysis, from the chosen load's saved sessions pooled after centering each, or typed. It
  is the per axis sigma as an angle, with the conversion tested, and the words under it say where it came from.
- **Filled from what GroupLab measured:** the velocity spread from the load, the zero error from the center's uncertainty, and the distance
  from the one the group was shot at.
- **The target**: a circle, a rectangle or GroupLab's own IPSC outline, sized as a length or in MOA or mil.
- **Three confidence presets** of GroupLab's own, each with its situation in a sentence, set every uncertainty nobody can measure.
  Advanced holds each with a bias beside it, the zero error, the latitude, and the trials and seed; an edit makes the preset Custom.
- **The answer** sits beside the elevation and wind for the distance: the first and the second round, each with an interval that holds
  both the sigma's uncertainty and the simulation's, and a sentence on which is the larger.
- **Beneath it** come the string's chance of at least one hit and its expected hits, every source by the probability it costs with its
  spread up and down and across, the total split, the assumptions in one paragraph, and the scatter over the target with the second round
  in its own color, and the curve against distance with its band.
- **Honesty.** No point without its interval, at most two significant figures and never a digit finer than the trial count supports. A
  refusal names the shots in one group that would make the answer mean something.

Eight glossary terms explain the new inputs where they appear. The user guide, the tour's Ballistics page and the bench have them.

**Tests.** `HitProbabilityTests`, 24:
- the Rayleigh closed form at five radii
- the second round's closed form at sigma times the root of two
- the seed repeating a run
- no added source ever raising the chance
- a per-string error and a per-shot one of the same size agreeing on the first shot and not on a string
- the interval holding sigma's uncertainty, the refusal, the ranking of costs, a bias against a standard deviation, and the velocity
  refusal
- the precision's definition, the rounding, the IPSC outline and the curve

`Entry156Tests`, 4, on screen. App 273 passed; Core 1597 passed, 2 skipped.

**Also in this commit.** Entry 155 left the interface benchmark's stale-exclusion test red on Linux and macOS: "Print…" is now the
Targets panel's print-to-device button, which is there on Windows only. It is excluded as Windows-only, and its reason says so.

## Entry 155: one Targets screen

**The screen.** The rail has one **Targets** button where it had the target library and Print a target. The list keeps its groups and
the description under each sheet. Choosing a sheet fills the column beside it at once: its name and description, the load block, the
actual-size instruction, Open to print, Save PDF, Print on Windows, the volunteer pack, Previous and Next sheet for a tiled sheet, and the
status line. The sheet's artwork fills the rest of the screen, with zoom. Design your own sheet opens in the same column. No second
window opens at any point: the print window became `PrintPanel`, a part of the screen, and the old window is gone, not left unreachable.

**Nothing lost.** Every item section 2 lists that the print window had is on the panel, and `Entry155Tests` names each one. Copies and
a paper chooser were never in the print window, since a sheet fixes its own paper and copies are chosen in the viewer or the driver, so
nothing was added. The paper refusal is the print gate's, unchanged, and the printed markers and codes come from the same renderer.

**Layout.** The list gives up the width it can spare, 520 to 360, so the preview still fills its height at 1280 by 720. A check box's
words now take the theme's text color in every state; the panel's was the first to show the gap.

**The tour.** One page, `/tour/targets/`, whose words describe the merged screen and say it was two. `/tour/library/` and
`/tour/print/` are each a plain page with one link to it, the way the send page's old address is kept, because entry 151 bans a meta
refresh. The user guide, the testing guide and the crash reporting page say Targets. The screenshot job needs no list change: it runs
the screenshot test, which now photographs Targets with a sheet chosen, and the old library and print pictures are removed.

**Tests.** The print screen's tests are repointed at the panel through a `TargetsScreen` helper, not deleted. `NothingIsCutOffTests`
covers Targets at both window sizes; the rail test counts seven buttons. `Entry155Tests`: choosing a sheet fills the panel with no
second window, section 2's list is all there, and no type in the application is a print window. App 269 passed, Core 1573 passed, 2
skipped.

**Also in this commit.** Entry 154's commit left the Linux tarball check red: its new source file was not in the ships list. The list is
regenerated here.

## Entry 154: every word a shooter may not know explains itself

**One list.** `src/GroupLab.Core/Marking/glossary.json` holds 54 terms: section 1.2's starting list, the ten figures that were already
explained, and the words the sweep found, such as the circularity and stringing tests, the error ellipse and the load comparison. Each has
the forms it is found by, a plain sentence, an optional precise one and a link to the published article that covers it. The application
reads it through `Glossary` and `FigureExplanations`, the report through those, `docs/GLOSSARY.md` is generated from it by `grouplab
glossary`, and the site builder reads the same file for its glossary page and its tooltips. There is no second copy anywhere.

**On the website.** The first appearance of each term in a page's body becomes a link to its entry at `/guides/glossary/`, a new page,
drawn with a dotted underline and carrying the plain sentence. `terms.js` shows it on hover and on tap, below the line so it never covers
the sentence, with a way to the full entry and a Close button; Escape and a tap elsewhere dismiss it. A second tap or click follows the
link. Without scripts the word is still the link. Headings, links, code and controls are never marked. The build fails if a page names a
term without explaining it, or explains it in words that are not the list's, and reports the terms no page uses yet (eleven, such as bullet
drop and hit probability, whose screens are not described on the site).

**In the application.** A label or heading that names a term is itself the affordance, drawn with the same dotted underline: the plain
sentence on hover; Tab reaches it and shows the sentence; Enter, Space or a click opens the entry, with the precise definition and More in
the glossary. It is decorated in place after every refresh, so no screen can be built without it. The review keys let a focused term keep
its Enter and Space.

**Plainer words instead.** "Bivariate fit" became "the fitted ellipse", the full table's note says "the group's own shape" for "covariance",
Show work says the markers "fit to within" a figure rather than "residual", and the home page and README say "true spread" for "true
dispersion".

**Tests.** `GlossaryTests`: every plain sentence has no symbol and no capitalized word mid-sentence, two to four sentences, each word
belongs to one term, section 1.2's list is all there, article links go to published articles, and the document says what the list says.
`Entry154Tests`: every label and heading on the analysis, its Advanced section, the marking screen and the settings that names a term is
explained, in the list's words, and reachable by keyboard; focus shows the sentence and Enter opens the entry. The site's own build check
covers every page. The user guide now describes the dotted underline where it described a question mark that had never been built, which
the claims register had not caught because that sentence has no checkable shape.

**Also in this commit.** Nightly 95 published at 12:15 UTC: its notes list nineteen changes under What you will notice, the rolling
release reads "Latest nightly (always the newest build, moves with every build)" with one line pointing at it, and #builds answered HTTP 200
to its embed. Entries 184 section 3.3 and 185 section 3 are closed by that. Its publishing also showed two faults, fixed in c107fab: the C#
copy of the notes path check still refused grouplab.org, and the thirty-release rule had deleted nightly 16's release while its entry still
linked to it.

## Entry 159: every published claim, and what backs it

**The register.** `scripts/claims.py` reads what a reader sees, the built site, the README, the root documents and `docs/`, and finds
**3161 checkable sentences** on 94 surfaces. Every one is backed: **851 by code**, **1279 by a measurement**, **1031 by a decision**,
**0 unbacked**. `docs/CLAIMS.md` lists them all with their backing; `docs/claims-backing.json` is where the backing is written.

**How they were backed, said plainly.** 389 were read one sentence at a time, and each has its own line naming the file and symbol, the
measurement and its date, or the entry. They are everything a shooter reads: the README, the home, download, support, upload and tour
pages, both guides, the platform statement and what-can-be-measured. The other 2772 are classified by eleven document rules and thirty
article rules, which say what their document is. The logs and briefs are dated records. The schema and library are specifications the
code implements and the conformance tests hold. The caliber list and release notes are generated. Each research article is backed by
the data files, data date and sources in its own front matter. **A rule is not a reading**, and the register says how many rest on one.
Sixteen research articles have no sources in their front matter and are not published; if one is published, its sentences have no rule,
and the check fails until they are backed.

**What was wrong, and is corrected.** Before this run, in 227917a: the home page's twenty-two sheets against the tour's twenty; the
README saying Windows only; both guides calling the caliber the single most useful thing, which entry 161 contradicted; a stale commit
count. This run:

| where | what it said | what is true |
|---|---|---|
| README, Platforms | Windows is "the only platform offered as a download today"; macOS "nobody has ever run one", "labeled untested" | all three are published, and the Apple silicon build has run on one Mac; the section now defers to the generated statement instead of being a second copy of it |
| README, download table | the Apple silicon build "Untested on a real Mac" | run on one real Mac; a test now holds the table to the download page and the statement |
| README, Planned | the caliber is a diameter and nothing else | since entry 163 a cartridge name is accepted where two sources agree |
| testing guide | "the macOS build is not offered for download" | both macOS builds are on the download page |
| testing guide | "no mounted photograph set" | the gate has 59 photographs of 2026-09-20, and about half could not be read |
| user guide, section 10 | GroupLab "can read them as one group" | pooling several sheets is not built; question 34 is still open. **A claim no screen reaches** |
| home page | the analysis shows sigma among the figures in view | since entry 169 sigma is under Advanced |
| research article, designing a readable target | "twenty-two built-in sheets", twice | twenty; the two tiled layouts are the same sheets on six pages |
| DESIGN.md, a dated protocol | twenty typed; twenty-two typed | counted, and "every built-in sheet" |
| DESIGN.md | a measurement with its unit missing, where FIDUCIAL-DECISION.md has it | the unit added |
| the cartridge list and the library | ".22 centrefire", "Centrefire load development" | the spelling entry 169 set, which its sweep had missed |

**Contradictions by shape.** Three count flags, all legitimate (different sheets have different bull counts). Nineteen stale-worded
sentences, "today", "not yet", "at present": each read, and every one is either a dated record or true by a mechanism, such as the guides'
pictures, which are rendered from the newest build every week; none changed. Twelve near-duplicates: templated release lines differing in
their values, and the one missing unit above. Claims about another product: the README's "not compatible with OnTarget" is Alan's
decision and states what GroupLab is not; the patent and trademark searches are dated records. All kept.

**What stops it happening again.** The README's Platforms section reads from the platform statement instead of restating it. Counts are
spans from `scripts/counts.py`: the tour's screens, the donor PDFs' pages, read from the files; DESIGN.md and the research article joined
its documents. `ClaimsAboutMeasuringTests` bans every false sentence above so the wording cannot come back, and holds the README's Mac rows to
the download page. **CI's site job runs `scripts/claims.py --check`**, so a new or changed sentence with nothing behind it fails the build,
and CLAUDE.md makes the backing part of the commit that writes the sentence. The README is now in the spelling sweep too; the drift it had
from its platform source, which the sweep caused, was caught and fixed on the way.

## Entry 169: the analysis screen, cut down

**What stays in view** is what the first outside user named: center from aim, extreme spread, group width by height, mean radius, CEP
50 and CEP 90, and the zero block. Each is its value alone, at a larger size than before (22 against 18; mean radius stays at the lead
size), with its angle, its interval and the figure without exclusions in a tooltip. Everything else is in one section, **Advanced**,
closed until it is opened and remembered once it is: sigma, the strips across and up and down, the order fired, the shape and flyer
cards, the flags, the full CEP table and bivariate fit, the sighters, and carrying the correction to another distance. Nothing was
deleted. The report is unchanged and still prints every line.

**The zero block** is one grid: a row an axis, in the length unit, MOA and mil side by side whatever the angular setting; the distance
it is for on a line of its own; and one line saying what to dial, in clicks with the click value stated, "Dial 2 clicks left, at 0.1 mil
a click", or the refusal with the shots that would settle it. The give or take, what rounding to clicks leaves and the degrees of
freedom are behind that line's why. **Where it landed is gone**; its class went with it.

**The plot** draws on white with black marks, or on black with white ones, never a dimmed copy: rings as outlines in a grey, the holes
as dark outlines at the hole size, CEP circles in the ink, and one accent, red, for the group center, the extreme spread line and a
picked shot. Nothing is faded or translucent. `ThemeTests` holds the ink to 7:1 and the ring grey and accent to 4.5:1 on the plot's
paper in every theme, and fails if the plot draws with an opacity again.

**The badge** reads "Scale checked" with a tick when the sheet's markers set the scale, and a plain warning otherwise; the markers, the
residual and the rest sit at the top of Show work. **Back** is top left. **The right button** drags the sheet in every tool, and a right
click that does not move is left free. **Alt** shows each tool button's key under it, and undo, redo and the rotations', until it is let go.

**CSV.** Export offers the complete record or the shot coordinates: one row a shot, its label and bull, across and up from the aim point
in inches, MOA and mil, a header naming the units and the distance, and whether it was excluded. Import reads comma, semicolon or tab
separated files, guesses the across and up columns from their headers, and asks: which column is across, which is up and down, the unit
(inches, millimeters, centimeters, MOA, mil), which way is up, and the distance for an angle. Rows without two numbers are counted and
left out. The marking it makes has no image, a scale of a thousand pixels an inch and the aim point placed by hand, so every figure works
on it. No other program's format is named anywhere.

**Own window** moves the figure column into a window of its own and back when that window closes.

**American spelling.** `scripts/american-spelling.py` changed 352 British forms in what a user reads: string literals holding a space in
the application and its engine, outside interpolation holes; the research articles, the guides and glossary the site renders, the tour,
and the site builder's text. Left alone: identifiers, comments, keys and file formats (a literal with no space), quoted material, block
quotes and code, the command line tool, whose usage names options its parser reads, and the published release notes. Two lines had to
keep a British form and say so: the cartridge table strips a typed " calibre", and the session store's SQL names a `$calibre`
parameter; the first pass changed that parameter, and the round-trip tests caught it. `AmericanSpellingTests` runs the script's check.
The guide PDFs and the caliber list are regenerated. The tour's analysis page and the user guide describe the new screen.

