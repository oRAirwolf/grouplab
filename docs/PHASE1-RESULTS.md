# Phase 1 results

**Brief** `docs/PHASE1-BRIEF.md`
**Branch** `phase-1`
**Reproduce** each table with the command named above it

---

## Where the Phase 1 gates stand

Stated plainly, `docs/NOTES-FROM-PLANNING.md` entry 33 section 5, so that "not yet measured" is never read as "passed". The gates are `docs/PHASE1-BRIEF.md` section 2's.

| Gate | Threshold | Status |
|---|---|---|
| Conformance test 43 | 0.001 in worst bull on a synthetic raster | **Met**, and checked on every test run. Must not regress |
| Paper gate | 0.005 in worst bull on the ten printed sheets | **Met.** Ten of ten through the selected model, worst bull 0.00319 in (M1.5) |
| Photograph gate, flat | 0.005 in worst bull on `main_flat1-3` | **Not met.** The flat frames nearly pass: the frame that decoded every marker is inside on every scoring bull, and the failures are named (M1.11) |
| Photograph gate, mounted | 0.005 in worst bull on the seven usable pinned frames | **Not met: 0 of 7**, and an open requirement (M1.11). It cannot be settled until mounted GroupLab sheets are photographed, which is this weekend's paper session |
| Hole detection, point 1 | At least 25 of 27 on `300_nm_hand_load` with zero false positives | **Met,** as a reproduction of the verified survey result rather than a fresh position check (M2.1) |
| Hole detection, point 2 | 99 percent of holes with no false positives on synthetic sheets, matched at 0.15 in | **Not met,** under the amended tolerance too (M2.2) |
| Hole detection, point 3 | Centre accuracy against truth | Reported, not gated |
| Statistics | `docs/STATISTICS.md` section 15.5 | **Met on points 1 and 3 to 6**: 45,476 keys compared with nothing pending ("Entry 28"), coverage 94.73 percent, the Monte Carlo table within tolerance on all 490 cells. **Point 2 is met on the shots and not on the series**, because the two fixtures group shot 242 differently |

**Not a brief gate, and new:** the whole path from image to group now runs as one command and matches synthetic truth ("Entry 33" below).

**Not a brief gate, measured for the first time: the Phase 0 gate record on Linux and macOS**, entry 32 section 3.
- **Windows** reproduces it byte for byte.
- **Linux** reproduces every table, and its records differ only below the precision any table reports.
- **macOS** reproduces the paper and photograph gate tables, and differs in measurements 1 and 2.
- **Linux: explained,** on entry 48 section 2's three terms. **macOS: not yet explained,** and localised to S2 ("Entry 48" below).

---

## What is not done, and why

| section | what | why |
|---|---|---|
| 4.3, 4.4, 4.5 | the server purge, deletion as part of the intake run, and the backup question | all need SSH; the backup question is a read on Alan's list |
| 6.1, second half | deleting the six from the old server | SSH, after the install |
| 6.2 | the redirect | SSH, and only after the new page is live and tested |
| 8.2 | one real test submission through the live page, and one real crash report | the page is not live until the install has run |


# Entry 147: macOS test builds, and one source for the platform statement

`docs/NOTES-FROM-PLANNING.md` entry 147, actioned 2026-09-23. Every section.

## What is published now

| download | for | tested |
|---|---|---|
| `grouplab-setup-win-x64.exe`, `grouplab-win-x64.zip` | Windows 10 and 11 | by hand, daily |
| `grouplab-linux-x64.tar.gz` | x86-64 Linux | by the suite, on every build |
| `grouplab-macos-arm64.tar.gz` | Apple silicon | by the suite only. **Nobody has run it on a Mac** |
| `grouplab-macos-x64.tar.gz` | Intel Macs | by the suite only. **Nobody has run it on a Mac** |

Both macOS downloads are real `.app` bundles: `Info.plist`, `PkgInfo`, the icon, and every published file underneath. A bare executable runs from a terminal and behaves like a stranger in the dock, which is not worth publishing.

## One source, three readers

Section 3.2. The statement is Alan's settled wording and is not to be reworded, which is exactly the text that gets edited in one place and not the others. `docs/PLATFORM-SUPPORT.md` is the source; the download page renders it, `scripts/platform-support.py --readme` writes it into the README between two markers, `--check` fails CI when it drifts, and the nightly appends it to any release carrying a macOS asset. Nothing restates it.

## To add a Linux target without further questions

The page promises other targets on request, so it is worth saying what makes a request actionable: **the architecture, and whether a plain tarball or a package built for a named distribution is wanted.** With those two the change is one row in `package.yml`'s matrix. Without the second, a request for "arm64" could mean a tarball or a `.deb`, and those are different amounts of work.

## The push that went red, which is the part worth reading

The first push failed on all three runners, so no nightly ran, so the download page was live offering two builds whose files returned 404. I published a page that promises a download before anything had built it.

Three failures, all mine:

1. **A README heading with no contents entry** — and the heading was inside the Planned section rather than top level, so adding the entry in the obvious place did not fix it.
2. **The README linked five assets where the test allowed three.**
3. **On Windows alone, my own test anchored a pattern with `$`** against the workflow file. A fresh checkout on Windows has CRLF line endings, so `- name: Publish$` matches nothing there and passes everywhere else. It passed here because this working copy is LF and only a fresh checkout converts. Entry 121 section 3 records the same fault in another test, with the same cause, which is the part that should have stopped me writing it again.


## Entry 149: four answers, and requests stop going through the panel

### Question 47, the five kind words (section 1)

The planning session kept all five. The reason it gives is the one that decides it: a change whose only benefit is a shorter list of words, paid for by rewriting 45 commit messages, is not worth making.

What was actually wrong was not the vocabulary but that there were three descriptions of one rule. Entry 145 named two words, because two headings are what a reader sees. `CLAUDE.md` named some of them. `scripts/release-notes.py` accepted five. None of the three was wrong on its own, and a person writing a commit trailer had no way to tell which one to believe.

- `CLAUDE.md` now names `new`, `fixed`, `changed`, `user` and `internal`, says which of the two headings each lands under, and says why there are five: **the heading is what the reader sees, the kind is what the writer says.** Somebody marking a change `fixed` rather than `changed` is saying something true about it even though both land in the same place.
- `ReleaseNoteKindsTests` reads the list out of `CLAUDE.md` and out of `release-notes.py` and requires the same set. It carries no copy of its own, because a third copy is a third thing to drift.
- A third test catches the quiet half: a word the pattern accepts but `KINDS` and `SAME` sort nowhere would be accepted on a trailer and then vanish from the notes, with nothing failing.

### Question 40, two sizes on one sheet (section 2)

**The quarter-point of the smaller group is the hole size, and entry 82 section 3 is amended by this rather than worked around.** The amendment is written where the rule lives, in `RenderDifferenceHoleDetector.SizeReference` and on `HoleSizeSource.TwoSizes`, not only here.

Before, a sheet whose round marks fell into two clear sizes got no size at all and a request for the calibre. That refusal flags nothing, and a sheet carrying five doubles is exactly the sheet where flagging nothing is worst.

The code cannot tell the two cases apart, and taking the smaller group is right in both:

| What the two sizes really are | What taking the smaller group does |
| --- | --- |
| Small marks are single shots, large ones are doubles | The doubles are flagged, which is what they are |
| Small marks are a second, smaller calibre | The larger holes are flagged, and entry 140 section 3.2's guard turns that flood into one question about the calibre rather than a page of them |

The description still asks for the calibre. Taking a reading does not stop the question being worth asking, because which of the two sizes a single shot makes is precisely what is not known.

`TwoSizes` now returns the cut as well as the two medians, so the quarter-point is taken inside the smaller group rather than across the sheet. `CryingWolfTests` keeps both rows of the table and the two-sizes row asserts the new behaviour: a size is read, it sits below the larger group, the doubles are flagged, and `oversized:all` is not raised.

Item 4 of the section, that a rimfire 22 is nominally 0.222 in and not 0.224, is entry 153 section 4's sweep and belongs there. Both `Calibre.cs` and `CalibreGuessList.cs` already carry the two figures as separate entries.

### Questions 37 and 35: not done in this pass (sections 3 and 4)

Both are accepted as written and neither is started.

- **Section 3, question 37: build A and D, not C.** A is clicking the bulls on the sheet, on the marking screen, with a way to select a row, a column and everything. D is offering it where a certain offset would move shots. That is marking-screen interaction and it is not a small change. **Until A exists the release note wording stays as it is**, which the entry requires: no note may claim this is fixed for anybody who has not read the code.
- **Section 4, question 35: keep one bull's width, then finish it.** The honest completion is re-running the entry 121 survey's own baselines against the narrowed rule, because that survey is where the false positives were counted. Six real scans are not that survey. **Until it has run, "no false holes anywhere" is not a sentence this project may publish**, and that limit belongs beside any claim made from the six scans.

### Requests for Alan go to a file (sections 5 and 6)

Alan has said plainly that the Claude Code panel is hard to read and that answering a question there is harder than answering it in the planning session. So `docs/notes/for-alan.md` exists, newest first, each request saying what is needed, why, and what a good answer looks like. An answered request is marked answered with the date and left in place.

- **A request never stops the run.** It is written down and the work carries on. If an entry cannot finish without an answer, everything else in it is done and the report says which part is waiting.
- **The one exception is a command he pastes into a shell**, because he runs those from the panel. It goes there written out in full, with which shell and what a good result looks like. He works inside MobaXterm and does not need the connection commands.
- The file starts with three real requests: entry 129's server work, the hit probability screenshots entry 156 asks for, and the photograph annotations entry 158 section 2 asks for. The last two are already in hand with the planning session, so they are recorded and not chased.
- The report after an entry is now the entry number, what changed, the test result, the commit, and whether the site has published it, with no request for Alan inside it.

**Tests:** the 17 affected tests pass, `ReleaseNoteKindsTests`, `CryingWolfTests` and `CalibreSplitTests` together. The full suites run with the next entry.

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

## Entry 175: the site sync's window follows nginx

The planning session confirmed on the server what the sync log had shown under entry 174: nginx's `open_file_cache_valid 60s` lets it
serve a file rsync has already replaced for up to a minute, the check requested the home page every three seconds for fifteen, and so it
kept the stale entry warm and read it every time. Alan applied a hot fix on the server, 12 tries 10 seconds apart.

**The repository now matches and goes further.** `grouplab-site-sync.py` has the same two values as its floor, and reads
`open_file_cache_valid` from the nginx configuration when it runs, in any unit nginx accepts, and makes enough tries to wait that long plus
30 seconds. A server set to 60 seconds gets the 12 tries the hot fix set; one set to three minutes would get 22. An unreadable file falls
back to 12. It only reads: nothing reloads nginx or changes its settings, which the whole server and pissinhot.com share.

**First deploy through.** `317932e` at 02:54 Mountain, first attempt, then `a77a1c7` at 02:59, first attempt. Whether the hot fix was in
by then the log cannot say. The live `/targets/` page carries `name="photos[]"`.

## Entry 176: the intake worker, made to finish

**What killed it.** `clamscan` loads the whole signature database into its own memory, about a gigabyte, inside the worker's 1 GB
limit. Every run was killed about 13 seconds in, and the worker tried again every two minutes. Nothing had caught it because no test had
run the scanner.

**What changed in the worker.**

1. **The daemon.** `clamdscan --fdpass`, so clamd, which runs as its own user, is handed an open file and quarantine stays `0750 airwolf`.
   The unit now has `RestrictAddressFamilies=AF_UNIX` beside `PrivateNetwork=yes`: the only socket it may open is clamd's.
2. **Limits that agree.** The cap was 600 megapixels, which needs about 7 GB to rebuild; it is 120 megapixels, 1.44 GB at the worker's
   peak of three copies at four bytes a pixel, and `MemoryMax=1600M`. A 108 megapixel phone and a 1200 dpi letter scan fit; a 200
   megapixel phone photograph is refused with the reason.
3. **Nothing stuck in silence.** The attempt count is written before each run, so a run the kernel kills still counts, and the third
   failed start sends the submission to `refused/` with `refused.txt`. Each submission gets one log line per run. The sweep no longer
   touches quarantine at all: a folder there is one the worker has not finished, and the hour rule would have deleted the first two real
   submissions.
4. **A scanner that did not scan is loud.** Each file's record says `clean, clamdscan` or `not scanned: ...`, the submission carries
   `notScanned`, the log says SCANNER DID NOT COMPLETE, and `Get-TargetSubmissions.ps1` prints how many files the scanner did not run on.
5. **HEIC.** Ubuntu 24.04 has no Pillow plugin for it, so `heif-convert` from `libheif-examples` decodes it to a PNG with the image
   already upright, and that is rebuilt like any file. Its camera facts do not come across; the record says it was HEIC.

**The installer** refuses `--intake` while Pillow, heif-convert, clamdscan or an answering clamd is missing, naming the package.

**The pull script** also reports what is waiting in quarantine and how long the oldest has waited.

**The test entry 176 section 7 asks for.** `tests/python/worker-tests.py`, in a CI job on `ubuntu-24.04`: the real worker, the real
clamd and clamdscan with a test signature, under `systemd-run -p MemoryMax=1600M -p RestrictAddressFamilies=AF_UNIX -p PrivateNetwork=yes`.
A phone-shaped JPEG with Orientation 6 and a GPS block must come out upright, without GPS, recorded as scanned; a HEIC must come out;
a file carrying the signature must be refused with the reason. It uses a generated photograph rather than a real one, because a real phone
file carries a location and would put it in the repository.

## Entry 177: the pull script, the removal script and the upright photograph

**The web path works end to end**, from a desktop browser and a phone, entry 173 section 1.3.

**The pull crash, and what else had it.** The worker writes each file under `stored`; the pull script read `stored_name`, joined the
null to the submission's folder, and a folder passed `Test-Path`. The check is now one function, `Test-SubmissionFolder` in
`scripts/SubmissionCheck.ps1`, which reads either key and requires a file. `Remove-ReadSubmissions.ps1` had the mirror fault, reading only
`stored`, which the old pissinhot.com submissions do not have, and a worse one: it iterated over the ledger object rather than its
`submissions` list, so under strict mode it stopped at the first `.id` and had never worked against the ledger as written. It now uses
the same check, reads the right list, and takes `-Only` to name submissions; a parameter called `-Id` would have been silently overwritten
by the loop's `$id`, because PowerShell names ignore case. CI's worker job runs the shared check on meta.json from a real worker run.

**Orientation.** The worker turned the pixels upright and then wrote the camera's Orientation, 6, into the new file, so GroupLab and
any viewer turned the photograph a second time. It now writes 1 and keeps the camera's value as `OriginalOrientation` in the JSON
record, which never reaches the file. The worker test asserts a phone JPEG with Orientation 6 comes out upright with the tag saying 1.
`ImageScrubber` keeps the original tag correctly, because it does not touch the pixels.

**The two submissions.** Both verify against their meta.json with the fixed check. `2026-09-24_58d94b23`'s tag is rewritten 6 to 1 on
this machine with every pixel and every other tag unchanged; its `meta.json` keeps the pulled hash as `sha256AsPulled` beside the new
one, and `orientation-fix.json` records why. The ledger records both: the test image as a test that is never published, the photograph
as Alan's own duplicate of a range frame, not added to any public set. Removing them from the server is Alan's command in request 1.

## Entry 178: the move from pissinhot.com is done, and a rule about HestiaCP's folders

**Entry 129 is complete.** `pissinhot.com/targets` and `www.pissinhot.com/targets` answer 301 to `https://grouplab.org/targets/`, the old
receiver answers 410, both sites answer 200, and the last pull from pissinhot.com found 18 on the server and 18 here.

**The fault was in my instructions.** Request 1 told Alan to back the include up beside itself. HestiaCP loads every `nginx.conf_` and
`nginx.ssl.conf_` file in a domain's `conf/web` folder, so the backup was loaded too and `nginx -t` failed on a duplicate
`client_max_body_size`. The `&&` kept the reload from running, so nothing live broke, but any other reload in between, a certificate
renewal for one, would have failed.

**The same fault was waiting in `install.py`.** Its `put` kept the old copy of anything it replaced as `<name>.<time>.bak` beside it, and
one of the files it installs is `nginx.ssl.conf_grouplab` in exactly such a folder. It has never fired, because the include was installed
fresh each time, but the second install that changed it would have left a live duplicate. Backups of anything under
`/home/airwolf/conf/web` now go to `/home/airwolf/backups/grouplab.org/config/`, named with the domain, and the dry run says where.

**The rule is written down** in `CLAUDE.md`'s standing constraints and in `docs/WEBSITE.md`, and request 1 now carries the commands that
worked, with a minute's wait before the checks, because a graceful reload lets an old worker answer a request or two.

## Entry 179: temporary files

**What filled 18 GB.** This session's scratchpad, nothing else: every time a test build had to avoid a locked folder I made a new
`altbinN`, fourteen of them at about 705 MB each; repository clones and the three copies made for the history rewrite; downloaded release
zips, installers and packages; rendered and rasterised sheets and one folder per research question. The largest single files were the git
packs of those clones, 157 to 158 MB each, and nightly zips and installers of 97 to 133 MB. **The suite's own leaks were in `%TEMP%`**:
14,987 `grouplab-settings-*.json` from App tests making a settings store and never removing it, 60 bench folders, 5 end to end images,
and 4,301 empty folders with random eight dot three names.

**The empty folders are `dotnet test`'s, not any test's.** No code in the repository makes a random temporary name, and a run of a few
Core tests left two new ones with every test process's temporary directory already redirected. So they are made by the runner process
before any test starts, two a run.

**What changed.**

1. `tests/Shared/TestTempRoot.cs`, a module initializer in both test projects, points `TMP`, `TEMP` and `TMPDIR` at a new
   `grouplab-tests/<pid>-<guid>` folder before any test runs and deletes it at process exit; a killed run's folder is swept by the next
   run after a day. Every test, the code under test and any child process writes there, so a forgotten cleanup line no longer leaks.
2. CI runs `dotnet test` with the runner's own temporary directory redirected to a folder it removes, and brackets the suite with
   `scripts/temp-leak-check.py snapshot` and `check`, which fails on any new `grouplab-*` entry, any run folder that did not remove itself,
   or any new empty random folder. Locally it passed on a full Core run.
3. `TestTempLeakTests` checks the redirection is in force and that no `Scratch*.cs` test is left in the suite.
4. `scripts/clean-scratch.py` removes earlier Claude Code sessions' scratch folders on this repository that nothing has touched for seven
   days, never this session's and nothing outside `c--Dev-grouplab`. It removed 15 empty ones today.
5. The scratchpad went from 18.4 GB to 707 MB in one listed delete, keeping only the App build in use and two small folders of work in
   progress.

**Entry 178 section 5, done with it.** `install.py` keeps only its newest `name.YYYYMMDD-HHMMSS.bak` of each file it replaces; a copy
made by hand under another name, such as the sync script's `.before-window`, is left alone.

## Entry 180: what the panel says is written down too

The planning session cannot see the panel, and Alan relies on it to say what needs him. So every message for him in the panel is now
also written to `docs/notes/panel.md`, which is ignored by git and read by the planning session, and a command that will stop for his
approval has its reason written there first. `docs/notes/for-alan.md` starts with the number of open requests and the most urgent;
requests 1, 2 and 10 were finished and still read as open, and are closed. `ForAlanTests` fails if the count and the requests disagree.
STATE.md is rewritten at the end of every entry.

## Entry 181: nothing copied to the server can carry a carriage return

The worker would not start after request 11 because its shebang read `python3` followed by a carriage return. The files under
`website/server/` were CRLF in the working tree Alan copies from. **The cause was this session's editing**: Python's `write_text` on
Windows translates line endings, so each script edit wrote CRLF, and git's `text=auto` normalised it on commit, so the repository never
showed it. `.gitattributes` now forces LF for `website/server/**` and shell scripts in every working tree, the copies here are rewritten
LF, the installer refuses a file with a carriage return and says which and how to strip it (tried on a CRLF file and an LF one), and a
test fails if any file in that folder holds one. My own edit scripts write bytes from now on.

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
## Entry 185: the releases page, and the next nightly's notes

**The rolling `nightly` release is needed.** The updater reads its manifest from
`releases/download/nightly/update-manifest.json`, and the download page and the README link its stable asset names. So it stays, and
the nightly now titles it "Latest nightly (always the newest build, moves with every build)" with one line naming the numbered release
it points at. No notes, and no platform line, are repeated on it, so nothing there can go stale; `rewrite-release-notes.py --github`
writes the same line. The rolling release that still shows nightly 94 and the old platform line is replaced by the next nightly.

**The test data release.** A draft is off the public page, but its files are not at the public download address, and a workflow token
with read access cannot see a draft at all. So `scripts/test-data.py` reads the release through the API when it has a token, which finds
a draft too, and the one CI job that holds `contents: write` fetches and verifies the files and passes them to the test jobs as a one day
artifact. Both routes fetched and verified the 59 MB scan here. The test jobs' own token is not widened. The release is created as a
draft from now on; making the existing one a draft is request 17, with the command that undoes it.

**The next nightly.** Nightly 95 had already failed, at Write the notes: entry 173's note names grouplab.org/targets, and the path check
refused it as a file in this repository. A commit cannot be edited, and the note was right, so the check now passes an address on
grouplab.org and still refuses any other path; the self-test holds both (42688df). The notes it will publish were checked before it
did: nineteen lines under What you will notice, among them pan by default, the caliber names, the freezes, the zero correction's
distance and the scan in real inches. The report quotes them once it has published.

## Entry 184: each published build in #builds

`scripts/discord-announce.py` builds one embed: the version as its title, linked to the build's release page; the notes from
`scripts/release-notes.py`, the generator of the release body, less the platform line, with both headings kept; and the download page.
A description over Discord's 4096 characters ends at a whole line with "Full notes on the release page". Nobody is pinged. The
nightly runs it after its release is published, in a step that cannot fail the build; a night the gate skips never reaches it. A tagged
release, and never a draft, posts to #builds and #announcements. The webhook addresses come only from the two secrets Alan added,
masked in the log, and the script never prints one; with a secret missing it says so and posts nothing to that channel.
`.github/announced-builds.txt` records each version once posted, and a rerun posts nothing.

Tested: the script's self-test (normal notes, notes over the limit, only Under the hood, no notes at all, no address in a message);
`DiscordAnnounceTests` for the ordering, the masking, the stable-only channel and no webhook address anywhere in the repository. The
dry run on nightly 95's real notes printed a 25 line embed. Question 52: a stable release's own body is fixed text, not these notes.

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

## Entry 183: the opt out travels with the submission

**Scanning works.** After request 14 the first upload's log read `clean, clamdscan`: the stream through clamd's own socket, at the
new limits. Request 14 is closed.

**The fault.** The receiver writes a `DO-NOT-PUBLISH` marker beside `meta.json` when the opt out is ticked. The worker took every file
in the folder but `meta.json` for an upload, tried to decode the marker, and refused the submission. Every opted out submission was
refused; the three before it had the box clear.

**The marker is kept**, because a person listing the folder should see it without opening a file, and because the publishing build
already withholds on either record. So the worker knows it now:

- It reads `meta.json` first and refuses a submission without one, since neither its files nor its consent are then known.
- It rebuilds only the uploads the receiver recorded by `stored_name`. Anything else in the folder, except the receiver's record and
  marker and its own bookkeeping, is a refusal naming the file, never a decode attempt.
- `exclude_from_public_dataset` must be true or false, and must agree with the marker. Where they disagree it refuses and says which
  way, rather than choosing one.
- The marker moves with the folder to ready, and the log's ready line says the submission opted out.
- An upload whose bytes no longer match the SHA-256 the receiver recorded is refused.
- A folder moved back from refused, whose original the worker already rebuilt and deleted, is rebuilt again from the worker's own PNG,
  through the same scan, and recorded as `rebuiltAgain`; `refused.txt` is dropped.

**The pull script's check** reads the opt out from either record, treats a missing flag as opted out rather than as false, and
reports the two disagreeing. The publishing build already withheld on either record, and its tests already covered each case.

**The consent test.** CI's worker job now runs the real receiver, through the harness its own tests use, which moved to one shared
file so the two cannot drift. It sends one photograph with the opt out ticked and one without, and one more that is left as the refused
submission was. The real worker then runs under the unit's sandbox and the real clamd, and the pull script's own check reads each
result. Each must arrive with its flag, its marker or lack of one, one scanned PNG and no original. A disagreeing pair of records and an
unrecorded file must each be refused with its reason. Run on Windows without clamd against the same shapes, the worker did exactly
that. This test would have caught this fault, entry 174's field name and entry 177's key.

## Entry 182: the scanner takes a stream

**Why nothing was scanned.** The worker runs in its own mount namespace, from `ProtectSystem=strict`, `ProtectHome=read-only`,
`ReadWritePaths` and `PrivateTmp`. A descriptor it opened and passed with `--fdpass` referred to a mount clamd cannot see from its own
root, and clamd's AppArmor profile refused it as a disconnected path, so every scan ended "Not a regular file". Entry 176's loud
reporting is what caught it: the worker's log and the pull script both said the scanner did not complete.

**The fix, and the part the entry left out.** `clamdscan --stream` sends the bytes over the socket, so nothing about the sandbox or the
distribution's AppArmor profile changes. Streaming needs clamd's `StreamMaxLength` above the largest file scanned, which is the rebuilt
PNG, not the upload: at the 120 megapixel cap, three bytes a pixel, about 361 MB. **But `StreamMaxLength` is not the only limit.**
`MaxFileSize` and `MaxScanSize` skip anything larger and report it clean unless `AlertExceedsMax` is on, and Ubuntu sets them at 25 MB and
100 MB. So all three go to 400M, `AlertExceedsMax yes` makes an oversized file a finding rather than a pass, and the worker treats an
over-limit answer as not scanned, never clean. `install.py --intake` refuses to finish while `clamd.conf` says less, naming each line.

**Tested where it failed.** The CI worker job now runs the worker under the unit's own mount sandbox, not only its memory limit, with
clamd configured as the server will be and a photograph whose rebuilt PNG is over 25 MB, and it must come out recorded `clean, clamdscan`.

## The archive

Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.

- [`docs/notes/archive/results-026-050.md`](notes/archive/results-026-050.md), entries 026 to 050, 16 section(s).
- [`docs/notes/archive/results-051-075.md`](notes/archive/results-051-075.md), entries 051 to 075, 10 section(s).
- [`docs/notes/archive/results-076-100.md`](notes/archive/results-076-100.md), entries 076 to 100, 6 section(s).
- [`docs/notes/archive/results-101-125.md`](notes/archive/results-101-125.md), entries 101 to 125, 26 section(s).
- [`docs/notes/archive/results-126-150.md`](notes/archive/results-126-150.md), entries 126 to 150, 9 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 138 section(s).

## Decision log

One line per method choice where there was a real alternative: what was rejected, and why.

- **M0: `markerSize` is 8 modules, over the brief's 10.** TARGET-SCHEMA.md section 3.7, the renderer and FIDUCIAL-DECISION.md all put 8 modules in `markerSize`; 10 would print the wrong modules and two of the five sheets would not render.
- **M0: quiet zone of two modules, over a fixed 10 dmm.** It keeps each marker in the proportion FIDUCIAL-DECISION.md settled at 0.5 mm, so only the module scale changes.
- **M0: `GL-CF25-LTR` as printed, over the 454 dmm sighter gap of the pending geometry change.** The 0.5 mm sheet is then the Phase 0 sheet by identifier, which makes it the sweep's control; the sighter geometry does not bear on the module floor.
- **M0: each sheet's lattice derived at its own footprint, over forcing the 0.5 mm lattice onto all five.** The derivation rule is the format's, and an explicit lattice would be a different fiducial scheme; the count difference is handled at measurement by registering from the shared positions.
- **M0: a footprint parameter in `tools/layout/layout.py`, over a C#-only generator.** CONTRIBUTING.md makes the layout tool the authority; the C# derivation is checked against it marker for marker, and `layouts.json` is byte-identical with the parameter at its default.
- **M0: sheets named by module, frozen by identifier once printed.** Names a person can match to a printout while the set is live, and the entry 11 rule when it becomes a measurement input.
- **M1: the cross-section as a tangent angle integrated along arc length, over a height function of page position.** Integration makes the page-to-sheet map an isometry by construction; a height function stretches the sheet wherever it slopes.
- **M1: a cubic tangent angle, three coefficients, over five.** The brief's lower bound; noise-free recovery is exact at every bend swept, and each extra coefficient is more bend for noise to fit.
- **M1: corner residuals in image pixels, over page dmm.** A detector's corner error is a pixel quantity; reclassification and selection stay in page dmm so the Phase 0 distances mean what they meant.
- **M1: six starting ruling angles, over a single start.** The ruling angle has no gradient until the sheet bends, so one start can settle on the wrong family.
- **M1: focal length refined from the brief's EXIF estimate, over fixing it at the estimate.** Starts from 0.55 to 1.80 times truth all refine to within 1.4 percent; a fixed wrong focal length leaves the pose unable to reproduce even a flat sheet's homography.
- **M1: reclassify every corner at 12.7 then 2.54 dmm, over 2.54 only.** The rendered 1.00 in bow: 96 of 136 corners and a fail, against 136 and 0.00040 in.
- **M1: an F test at p = 0.001 for keeping the bend, over always fitting it or a fixed residual threshold.** Always fitting doubled a flat sheet's worst bull; a residual threshold would depend on scale and marker count, which an F test does not.
- **M1: synthetic corner noise from `main_flat1`'s residual, 0.52 px per axis, over the 0.16 px a clean render gives.** The render's figure would flatter the model; the flat frame's includes whatever its flat model left, so it is a ceiling.
- **M1: a forward-difference Jacobian, over analytic derivatives.** Fourteen parameters, and noise-free recovery to under 0.00001 in shows it is accurate enough.
- **M1: a bull whose edge profile has under 3 or over 4096 samples fails with a reason, over clamping the sample count.** A clamped count would still sample a mapping already known to be degenerate and report a centre from it.
- **M1: one starting focal length per joint fit, the EXIF candidate with the lower alone-fit residual, over the group median or a vote.** Four of the seven 2.2 mm frames carry one 35 mm equivalent and three the other, so a median or vote decides by frame count. Measured on those frames the choice does not matter: the costs are 0.1 percent apart and the fitted results are identical from either start.
- **M1: the joint-fit lens key left as focal length and f-number, with the table frames reported rather than regrouped.** Entry 6 settled the key, and regrouping after seeing these frames degenerate would be fitting the method to them.
- **M1: the lens held on the mounted frames was fitted flat and shared on the camera's flat frames, over the median of Phase 0's per-frame lenses or a fit on the full-marker bent frames.** A flat frame has no bend for the lens to absorb, a median breaks the pairing of k1 and k2, and a bent frame is the absorption being tested.
- **M1: the leftover-shape diagnostic on two terms, curvature along the rulings and a saddle, over a saddle alone.** A twist is a saddle only in the truth's rulings: the synthetic fit turned its rulings to 72 degrees, and in turned rulings a saddle is a difference of squares, half of which the bend absorbs.
- **M1: the real-frame lens run written and started before the sweep's numbers were read, over adjusting it to them.** Brief section 3.3's rule applied a second time.
- **M1: M1.2's "at random" marker rows relabelled as the first markers in raster order, over re-running them at random.** The label was wrong and the numbers right, and they are cited in entry 13; M1.7's sweep carries a random coverage of its own.
- **M1: corner quality placed on the sweep by a robust sigma over all corners, over the RMS of the kept corners.** The kept corners are truncated at 2.54 dmm, and their RMS stays between 0.9 and 1.2 px from 1 px of noise to 5.
- **M1: joint fits keyed on physical focal length, f-number, 35 mm equivalent and image size, over the equivalent and image size alone.** Entry 16 section 2 asks for the pixel geometry; the equivalent alone would join the cropped table frames to the main camera, which shares their 23 mm and their image size but not their distortion.
- **M1: the general developable surface as folds along turning rulings 10 dmm apart, over a smooth parametrisation of a tangent developable.** Rigid strips keep the isometry exact for any turn and reduce to the cylinder, within 0.018 dmm, when the rulings do not turn.
- **M1: a general surface whose rulings cross inside the page is undefined, over letting it fold through itself.** No sheet of paper takes that shape, and a fit that could reach it would report a shape that cannot exist.
- **M1: the minimiser takes a backward difference, and holds a parameter, at an undefined boundary, over a smooth barrier penalty.** The change leaves every fit that never meets the boundary unchanged, which the cylinder's raw rows confirm; a barrier would change every general fit's cost.
- **M1: stopped at the general surface with its joint fit unconverged and its alone fits as the measurement, over constraining its turn on weakly bent frames.** Entry 16 section 5 stops at the general developable surface, and a constraint would be a further model decision.
- **M1: one frame at a time by default, over the joint fit.** A user photographs one target at a time, the lens barely matters (M1.7), and sharing a camera cost `main1` a factor of two; the joint fit stays behind `--joint`.
- **M1: fewer than eight kept corners select the plane, over the bend.** Less evidence has to mean fewer parameters; the old default preferred the model with more.
- **M1: the correlation diagnostic on per-marker mean residuals between neighbouring markers, with a permutation null, over corner pairs or a variogram.** Corners of one marker share their detection and would read as structure that is not the sheet, and a permutation test needs no model of the noise.
- **M1: the mounted gate left at 0.005 in and recorded as open, over a separate looser gate.** Entry 17 section 2: the error budget argues for 0.003 to 0.005 in, and a number chosen after seeing the results is not a gate.
- **M2: morphology and blob extraction behind the imaging backend, the arithmetic, filters and measures in Core, over the whole primitive in Core or the whole primitive in the backend.** Core keeps no OpenCV dependency, and the port can be checked against the survey step by step because the backend calls are the survey's own.
- **M2: the baseline compared through the survey's own roll-up band, 0.15 to 0.55 in, over the detector's 0.60 in.** The survey's tables were computed through that band, and two detected holes fall between the two.
- **M2: render-and-difference opens with a 0.012 in disk, over the survey's 0.032 in.** After differencing only registration slivers remain to remove, and the survey's disk erased thin rims around pale cores, which were both detectors' misses.
- **M2: each bull's cell aligned to the observed image by phase correlation, over trusting the registration or opening wider.** The rings are fiducials already printed, and a wider opening would bring back the faint-hole misses.
- **M2: rim closure recorded and not gated, over a threshold between arrowheads and holes.** The two overlapped on synthetic sheets, and the synthesis draws no C-shaped rim, the case the signature exists to tolerate.
- **M2: merged neighbours split at an elongation of 1.45 by two-means, over refusing them as too large.** Refusal lost both holes of a pair; a split reports two and flags them.
- **M2: the oversized flag against the sheet's median, over a calibre size window.** The synthetic sheet carries no calibre; the median's failure when every bull holds a pair is reported.
- **M2: the synthesis calibration stopped at four iterations with its gaps reported, over iterating until it matched.** Each iteration moved one quantity at the cost of another, and the gaps' direction says which way the recall errs.
- **M2: thresholds set on seeds 1 to 3 and the gate read on seeds 1001 to 1003, over one seed set.** A threshold read off the seeds it is gated on is fitted to them.
- **M2: truth matched nearest pairs first within 0.15 in, over an optimal one-to-one matching.** Scoring should not share the rule of the assignment it is used to evaluate.

- **M3: special functions written for the engine, over a numerics package.** Nothing may be installed, and section 15.3's 1e-12 needs control of every step: the incomplete gamma prefactor through R's deviance form, and every upper tail computed as a tail.
- **M3: CorrNormal CEP through a trapezoid rule over angle on the Hoyt CDF, over Imhof's integral or a series.** The integrand is smooth and periodic, so the rule converges geometrically and matches shotGroups' hit probabilities to 1e-15.
- **M3: the minimum-volume ellipse by Khachiyan's algorithm at shotGroups' 0.001 tolerance, over an exact solver.** Section 15.3 says to match the tolerance or expect disagreement; at the package's tolerance it agrees to 3e-13.
- **M3: range statistics from ten groups per replication with running means, over simulating each group count separately.** Every cell keeps its full 10 million independent replications at a fifth of the cost of fifty-five groups.
- **M3: quantiles from 8,192-bin histograms, over storing ten million values per cell.** One bin is 7.3e-4 of the mean against a 5e-3 tolerance, and 65,536 bins ran three times slower for no measurable gain.
- **M3: the table read between rows by R's fmm spline on shotGroups' own grid, over simulating every n or interpolating linearly.** At n = 92 the spline reproduces shotGroups, and linear interpolation is 4.8e-5 off.
- **M3: a key held back only on evidence checked key by key, over excluding by dataset or loosening a tolerance.** The point of aim is detected from the fixture's own two centres, a Monte Carlo p-value by being an integer over 9999, and a disputed CEP by not being a root of its own distribution.
- **M3: the Fligner-Killeen statistic at 1e-10 relative, over 1e-12.** It subtracts n mean^2 from a sum of squares of up to 530 normal-quantile scores, which costs about three digits; the worst difference is 2.4e-12.
- **M3: the flyer expectation as an integral, over section 10's alternating binomial sum.** At 25 shots the sum's terms reach 5.2 million with alternating signs; the integral has no cancellation.
- **M3: the pre-pooling guard as pairwise F tests with Holm's adjustment, over Bartlett's test of homogeneity.** Section 11 names section 8.1's F test, and section 8.4 names Holm for its family.
- **M3: the engine's own xoshiro256** generator for resampling, over the runtime's Random.** Section 6 requires a recorded seed to reproduce an interval exactly, and the runtime does not promise its stream across versions.

- **Entry 20: bull centres from ring fits confirmed by centre dots, over the survey's annulus matched filter.** Holes break the rings and the scan's rings carry light stripes, so neither survives as a clean blob, while the dots do; the dot centroids are kept beside the ring centres and agree to a median 0.0019 in.
- **Entry 20: grid orientation found from the bulls, over trusting pixel order or reading EXIF orientation.** The photograph's pixels are stored a quarter turn from upright; lattice phase and the sighter line's 0.2-pitch offset place the grid whatever the storage, and an unmirrored layout picks the column direction.
- **Entry 19: the hole detector run on the whole photograph and on the registered sheet, over the whole photograph only.** Untuned on the whole frame it finds nothing, which is the finding; the sheet-only run shows what registration would let the same primitive do, and is labelled as that.

- **M4: the marking screen and the correction screen as one screen over one model, over a separate manual mode.** Entry 21 section 3 and DESIGN.md section 13 describe the same interactions; one immutable model gives both undo and a test surface that needs no window.
- **M4: the image shown from the pipeline's own OpenCV decode, over Avalonia's decoder.** The two can disagree about EXIF orientation, and a mark must land on the pixels the statistics and the detector use.
- **M4: the image sized by its decoded pixel grid, over the bitmap's size.** The bitmap's size follows the file's DPI tag; the headless test showed marks scaling with it.
- **M4: the UI built in C#, over XAML.** Avalonia 12 compiles bindings by default and the screen is one window; code keeps every control's wiring in one place a reader can follow.
- **M4: a headless test through the platform's pointer input, over calling the model directly.** It caught two faults that would have reached a person, which the model tests could not.
- **Entry 24: a dispersion figure withheld below five shots, over printing it with a warning.** A warning beside a three-decimal headline is what Alan read past; the count and the centre offset stay, because they are exact at any count.
- **Entry 24: each interval labelled with its exact coverage, over removing the c4 correction from the endpoints to make them 95 percent.** Brief section 5 has GroupLab match shotGroups' intervals; stating their coverage keeps that and stops the label from lying.
- **Entry 24: extreme spread's interval in the form that covers the expected spread, over shotGroups' `getRangeStat` form.** The latter covers 84.7 percent at two shots; entry 23 section 1 says to stay exact where shotGroups is not, and the harness still checks its form.
- **Entry 26: rotation as a view property of the marking state, over rotating the decoded pixels on load.** Rotating pixels changes the frame every saved position is in; a view property moves no mark, gives undo for free and reopens as it was left.
- **Entry 26: the stored pixel frame as the canonical frame, over the upright displayed frame.** It is the frame M4.1 files were already written in, so version 1 migrates without moving a mark, and it can be checked against the file itself.
- **Entry 24 section 5: the hole-size flag at nominal plus 0.132 in, over a ratio of the calibre.** Section 3.5 found the hole deficit roughly constant in absolute terms rather than proportional.
- **Entry 25: one application-wide unit setting, over a unit dropdown beside each input.** Entry 25 section 1 asks for it, and a setting that every figure obeys cannot leave one figure behind in inches.
- **Entry 25: "mil" as the milliradian, over the 6400 NATO mil of section 12.5's table.** Turrets are marked in milliradians; offering the NATO mil under the same name is the dialling error entry 25 warns about.
- **Entry 25: lengths and distance stored in inches whatever the setting, over storing in the unit the user worked in.** A file then means the same on every machine, which the test asserts by writing one marking under both settings.
- **Entry 25: printing through the system's PDF print command, over drawing pages to a printer from the application.** Avalonia has no printing API and nothing may be installed; the PDF is the path that already works, and what it cannot control is said on the screen and on the sheet.
- **Entry 25: the actual-size note as a render option, on in the print screen and off by default, over adding it to every render.** Phase 0's pages and the gates measured on them stay item for item as they were.
- **Entry 25: `/PrintScaling /None` in every PDF GroupLab writes, over only the print screen's.** A sheet printed from the command line is measured the same way and deserves the same request.
- **Entries 22 and 27: the scrubber in C#, over running `scrub_exif.py`.** Its library is not installed and nothing may be installed; the C# version follows the script's policy, keeps digital zoom for entry 27, and also removes XMP and trailing data, which the script leaves.
- **Entry 27: triage by decoded markers, holding rather than refusing what fails it.** Markers are the check the application already has; a held file costs a person one look and an `--accept`, where a refused one would need resubmitting.
- **Entry 22: the committed-image guard names the 16 photographs with GPS, over failing the suite until history is rewritten.** The rewrite is a decision for question 13; a named list keeps the suite green without letting a seventeenth image in or letting the list go stale.
- **Entry 28: rebuilding the aimed coordinates from the shot and a recovered aim, over excluding the four Fligner-Killeen keys as a known difference.** The rebuild reproduces R's doubles and all four statistics to 5.5e-13, so the gate checks something true rather than recording a gap; regenerating the fixtures at full precision would make it unnecessary.
- **Entry 28: a stated digital zoom of 0 read as 1 in the lens key, over keeping the tag's value.** The EXIF standard defines 0 as digital zoom not used, which is the geometry of 1, and every Pixel photograph in `scans/mounted/` states it.
- **Entry 28: refusing a `meta.json` whose opt-out field is missing, over treating it as false.** A missing opt-out is unknown, and publishing on unknown consent cannot be undone.
- **Entry 33: `grouplab analyze` composing `AutomaticMarking` and `GroupAnalysis`, over a separate pipeline for the command line.** The screen and the command then run the same code, so a fault found by one is fixed in both, which is how the sighter fault reached the marking screen's fix.
- **Entry 33: a printed sheet analysed even when its definition fails today's validator, over refusing it.** Validation decides whether to print a sheet; a sheet already on paper is what it is, and refusing it would make every Phase 0 sheet unanalysable.
- **Entry 33: `--target` required, over guessing the definition from the image.** Nothing reads the printed identifier or codes yet, and the built-in definitions share marker ids.
- **Entry 34: two of the owner's photographs held, over publishing all 28.** A screenshot and a messenger download cannot show who took them. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one until Alan confirms costs nothing.
- **Entry 34: a donated submission whose files were all held published as a provenance record alone, over leaving it out.** The record shows that a consented submission arrived and why none of it is published, which is the question a contributor would ask.
- **Entry 34: a separate `publish-owner` path, over running the owner's photographs through `intake`.** Intake requires a consent record, and entry 34 section 2 rules out inventing one.
- **Entry 35: a missing camera make alone is enough to hold a file, over requiring a copy's file name as well.** Entry 35 section 1 calls the make the stronger signal. A person can still accept a held donated file by name, so a false hold costs a look, and a false publication cannot be undone.
- **Entry 35: an edited phone copy that keeps its camera metadata, the four `~2` photographs, not held.** The rule is about lost camera geometry. Those copies keep their make, model and focal length, and nothing in entry 35 asks for more.
- **Entry 37: a submission whose consent cannot be read withholds its files' hashes, over ignoring it.** The rule exists because publishing under ambiguous consent cannot be undone, and an unreadable `meta.json` is the most ambiguous consent there is.
- **Entry 37: a file held for a consent conflict cannot be accepted by name, unlike a file triage holds.** Triage is a judgement about usefulness that a person can overrule; an opt-out is the contributor's decision, and only the contributor can change it.
- **Entry 37: the two conflicted submissions not published even as provenance records.** Publishing a record of a submission whose consent is in question, with its answers and credit name, waits for the contributor's answer as the photographs do.
- **Entry 36: `DFdistr` left at its committed version, over committing a JSON whose numbers are strings or editing `sg_distr.R` here.** Nothing reads it, `tools/` is the authority and planning's to change, and a fixture committed in a broken shape would be read as correct later.
- **Entries 39 and 40: any unassigned shot on a sheet of several scoring bulls withholds every figure, over quoting the assigned shots alone.** Leaving shots out without saying so would be a different wrong number, and the sentence in their place tells the user exactly what to do.
- **Entry 40: a tap on printed ink placed where it was tapped, with a note, over snapping onto the ink and naming it.** The centre of a printed stroke is never the answer, and a tap placed where the user put it is at worst as wrong as the user.
- **Entry 39: a moved shot follows its nearest bull unless the user assigned it elsewhere, over keeping whatever bull it had.** A shot dragged across to the next bull is almost always a correction of position, and a deliberate reassignment is recognisable because it differs from the nearest.
- **Entry 39: an impact placed by press, drag and release, over click, drag, click.** It is one gesture with a finger or a pointer, and a plain tap still places a shot.
- **Entry 42: a text colour below 4.5:1 moved along its own hue, over changing which role the text takes.** Section 2 says so. The contrast is measured on the three surfaces text sits on, not on the sunk image area, which carries marks. Counting the image area too would have pushed light `faint` almost onto `dim`, erasing the difference between the two.
- **Entry 42: styles rebuilt when the theme changes, over binding each control to a theme resource.** The shell is built in code, and rebuilding one style set keeps every colour decision in `AppStyles` and `Tokens` rather than spread across every control.
- **Entry 42: a unit left at its figure's size for now, over splitting the figure text.** The existing tests read that text, and section 1 requires them to pass unchanged.
- **Entry 42: mark labels in light text on a dark plate, over text in the mark's colour.** The mark's colour as text could not be read on the first screenshots, and the colour survives as a bar beside the number.
- **Entry 41: image facts from the metadata GroupLab already reads, over reading the further facts section 2 permits.** Bit depth, lens model, ISO, exposure and a count of EXIF tags would each mean reading more of a photograph's metadata for the sake of a log, which is the step the rule exists to stop.
- **Entry 41: the directory named in symbols on the first log line, over the resolved path.** Section 3 asks that the first line report where the log is, and section 3 also forbids a path. `%LOCALAPPDATA%\GroupLab\logs` satisfies both.
- **Entry 45: `last_action` as the name of the last event logged, over a separate list of action names.** Every user action already writes a stable event name, such as `print.select`, and a second list would drift from it.
- **Entry 41: Send saves the package before sending, into the log directory when the user has not saved it.** Section 7 says the zip is kept if sending fails, and the only way to guarantee that is for it to exist before the attempt.
- **Entry 41: a crash record not rewritten when a second crash lands in the same second of the same process.** The receiver's name pattern leaves no room for a counter, and the first crash is usually the cause.
- **Entry 46: an alert ring at the measured size on a flagged hole, over every impact ring at its measured size.** The size check reads an extent only for a dark region on paper, so a measured ring for every shot would silently fall back to the calibre on ink and on a dark backer. Every ring stays comparable, and the one that disagrees shows by how much.
- **Entry 35 section 6 item 3: the definition from the sheet's QR codes, over choosing among definitions by registering against each.** A frame that passes its CRC names one definition. A registration that fits well against the wrong definition is possible, because the built-in definitions share marker ids.
- **Entry 35 section 6 item 3: refuse and ask when the codes do not settle it, over falling back to the nearest match.** Eight of the 37 Phase 0 images are not identified and fall back to naming the definition. A wrong definition would produce a plausible wrong group.
- **Entry 35 section 6 item 3: the frozen Phase 0 definitions shipped beside the application, over the live library only.** The sheets already printed carry the frozen identifiers, and the print screen's list does not show them.
- **Entry 37 section 5: no size recorded for a note with no unit, over assuming inches.** Every size in the first notes had its unit, and a wrong assumption is a scale error of 25.4 or 2.54 times, which the marking screen could not detect.
- **Entry 37 section 5: the stated size offered in the rectangle prompt, over setting the scale from it.** The size is the sheet's, and only a person can say which corners are the sheet's and which side was tapped first.
- **Entry 37 section 4: the scrubber's keep list left as entry 29 set it, with `LensModel` reported rather than added.** Adding a field to what is published is a publication decision, and the lens grouping does not need it.
- **Entry 46 section 3: the count line replaces the "Placed:" line, over keeping both.** They carried the same three numbers, and the one planning asked for sits above the figures where it is read first.
- **Entry 35 section 6 item 2: the gate record workflow left failing on Linux and macOS, over passing within a tolerance.** Entry 32 section 3 asks for byte identity or an explained difference, and choosing a tolerance that makes the difference pass would be choosing the gate.
- **Entry 47: double resolution tried second, over a capability fallback.** The WeChat module is present in the runtime packages for all three platforms. The failures were detection on code modules under five pixels, which only a larger working image can help.
- **Entry 47: resolutions past 8000 px skipped, over trying every one.** Doubling a 600 DPI scan cost 52.7 s and gave nothing the scan did not, and the bound lost no image on the sweep.
- **Entry 47: identification counted per platform in the gate record workflow, over a per-platform expectation in the unit tests.** No count is known yet for Linux or macOS, and an expectation written before measuring would be a guess.
- **Entry 48: Linux's gate record difference explained and left red in the workflow, over a Linux reference record or a rule that passes it.** Making the job green needs either Linux's own records committed as its reference, or a rule about which differences pass. The first adds about 12 MB and the second is a gate written after the results, so the choice is planning's.
- **Entry 48: the owner corpus republished from the originals through `publish-owner`, over editing the published files.** Every published file still comes from the one scrubber, and the 13 files without a lens model reproduce byte for byte, which shows nothing else changed.
- **Entry 49 section 1: the printed tables committed as Windows text files, over comparing the platforms with one another inside one run.** A committed reference fails a single platform's job on its own, and it states in the repository what the record is.
- **Entry 49 section 4: the README's platform guard requires equality with the CI matrix, over naming at least as many.** A README naming a platform CI does not build would be false in the other direction.
- **Entry 49 section 2: the marker sort held for question 15, over committing it with regenerated records.** It changes committed evidence and a benchmark the Phase 1 surface work is measured against, which entry 49 did not foresee, and the question file exists for a measurement that contradicts what was written down.
- **Entry 52: every record regenerated twice under identical code, without the sort and with it, over comparing the sorted run with the committed records.** Five records were already stale, and comparing against them would have credited their changes to the sort.
- **Entry 52: tables from fits no command reproduces left as measured and marked, over updating the columns that can be regenerated.** Half a row regenerated disagrees with its other half and with the conclusions written beneath the table.
- **Entry 52 section 3: reordering applied at the homography fit, over shuffling the detector's output.** The sort puts any shuffled detection back in order, so only a shuffle after it measures the registration's sensitivity to order.
- **Entry 52 section 3: the edge fit's leave-one-out run from the converged pass's start, over rerunning the whole locator per point.** It isolates one point's weight in the fit; rerunning the locator would also move the rays and confound the two.
- **Entry 49 section 2: the journal replayed by call order, with the image hash reported beside it, over looking each detection up by its image.** A platform whose raster differs would match nothing and replay nothing, and the rerun exists to hand it Windows' corners anyway.
- **Entry 49 section 2: only the two measurements whose tables differ replayed, over all eight.** The other six already print identically on macOS, so replaying them could only repeat what the gate record shows.
- **Entry 58 section 4: the opt-out's second key is what a file scrubs to, over the decoded pixels.** It collapses the four re-exports to one value as a pixel hash would, and it keeps a consent mechanism inside `GroupLab.Core`, which has no image decoder and whose tests run on every platform.
- **Entry 58 section 3: either key alone withholds, over requiring both.** The same reasoning as entry 37 section 1's two opt-out signals: redundancy is the point, and publishing under ambiguous consent cannot be undone.
- **Entry 55 section 3 item 1: the counts recorded in the stage record and in `grouplab measure --json`, over the committed spike records.** A field in the spike records would regenerate thirteen of them and move the gate record's raw comparison, for a diagnostic that changes no figure anybody reads.
- **Entry 55 section 3 item 1: a ray counted as marginal within a tenth of the crossing threshold on either side, over counting only the rays that failed it.** A ray that just cleared the threshold is as easily flipped by the image as one that just missed it, and the tenth is the convention the leave-one-out already uses for the rejection limit.
- **Entry 61 section 3: no catch added for `PlatformNotSupportedException`, over adding one defensively.** The runtime throws `Win32Exception` for a verb off Windows, so a catch for the other would assert a behaviour that does not exist and would outlive anybody who remembers why it is there.
- **Entry 64: the working tree left alone, over running the fix as written.** Nothing differed, so the command would have been a no-op dressed as a repair, and running it would have left a false record that something was cleaned.
- **Entry 61 section 5 item 2: the tarball built on `ubuntu-latest`, over pinning `ubuntu-24.04`.** Pinning would freeze the glibc floor deliberately and freeze it silently apart from the test matrix, which tracks `ubuntu-latest`; printing the release it built on keeps the day they diverge visible, which is what entry 63 section 2 asks for.
- **Entry 61 section 5 item 2: the step fails when the native imaging library is missing, over shipping whatever publish produced.** A tarball without `libOpenCvSharpExtern.so` installs, launches, and then cannot detect a marker, which is a failure that arrives late and in front of a user rather than in CI.
- **Entries 65 to 69: the concept image left uncommitted, over committing a second copy.** The committed `docs/figures/screens/assignment-editor.png` is the same picture pixel for pixel, and the copy would add 481 KB and a content-credential block to the history for nothing.
- **Entry 65 section 4 step 2: wording that names no platform, over "on Linux".** The branch it describes runs on macOS as well.
- **Entry 70 section 3: a moved shot recorded as the difference from the bull detection gave it, over a list of notices appended at each edit.** The difference is derived from state, so undo, redo and a shot moved back all keep it true without bookkeeping, and it stays visible for as long as it stands rather than until the next edit.
- **Entry 70 section 3: the counts rule falls back to the nearest free bull, over the nearest of all bulls.** A bull a person has decided is not available to the matching in either mode, so the two methods differ only in whether the matching is forced.
- **Entry 70 section 6: three status states, over styling only the failures.** Success needs its own colour for the same reason alert does: a line that reads the same whether it worked or not teaches people to stop reading it.
- **Entry 74: pinning read from `BullChosen` alone, over `Corrected` or a chosen bull.** A shot becomes corrected when it is moved or marked not a shot, and neither of those chooses a bull.
- **Entry 73 section 1: a shot's pool is its nearest bull's, with the margin still measured to every bull, over a pool chosen by page region.** The sheet already says which bulls are sighters, and nearest-bull is the rule's own fallback; a region would be a second geometry to keep in step with every definition.
- **Entry 73 section 1: the overall method reported as nearest-bull when either pool fell back, over reporting the scoring pool's.** Reporting one-to-one would hide that a pool stopped being matched, which is the thing section 13 says must be said; the reason names each pool.
- **Entry 73 section 5: no mark size invented when no calibre is set, over drawing a nominal diameter.** A ring at a made-up diameter reads as a claim about the bullet, which is the kind of implied fact section 2 rules out.
- **Entry 73 section 7: the interval labels left at their exact coverage, over matching them.** Entry 24 decided the label states the coverage the interval actually has.
- **Entry 71: intake run on a copy without the unlisted scan, over adding the scan to the manifest.** Whether the consent covers the scan is the contributor's question, and the submission as received is left as it is.
- **Entry 71: the worst bull reported beside the worst clean bull, over the worst bull alone.** On a shot sheet the holes cut the rings of the bulls they hit, and separating the two shows the error is registration.
- **Entry 76 section 4: the printed name reverted, over landing it with a box that covers both captions.** On a sheet already printed the wider box hid a real hole, and the analyser cannot tell which caption a sheet carries; where the name goes is question 16.
- **Entry 76 section 4: detection cancelled at checkpoints between stages, over interrupting a stage.** A stage stopped halfway leaves nothing a person can use, and the checkpoints are where the work can be dropped cleanly.
- **Entry 76 section 4: a moved shot drops its measured diameter, over keeping it.** The measurement described the point the detector chose, and a ring at that size around a point a person chose would claim a measurement nobody made.
- **Entry 76 section 1: the aspect's null integrated exactly, over a simulated table.** The density has a closed form for every n, so there is no table to extend or to seed.
- **Entry 76 section 2: the two split detections merged at their midpoints for the re-run, over the pipeline's figures or hand-picked positions.** The pipeline's figures count one hole twice, and the midpoint uses only what the pipeline found, which is the question entry 76 asked.
- **Entry 75: a shot with no bull named "unassigned, at x, y" in the list, over "unassigned" alone.** Two such shots would otherwise read the same, and the position is a fact the screen has, not an order.
- **Entry 77 section 3 item 1: a blob counted as swallowed only when it passed every shape filter, over every blob centred in a zone.** Printed matter leaves residue that the size and shape filters refuse anyway, and counting it would bury the one number that means a hole may have been lost.
- **Entry 77 section 3 item 2: the check made standing by an artwork fingerprint test, over running the corpus inside the test suite.** The corpus takes minutes and its counts differ by platform. The fingerprint is fast and exact on every platform, and it forces the comparison to be run where it can be.
- **Entry 77 section 3 item 2: the committed corpus punched with synthetic holes on three offset grids, over committing a real shot sheet.** No shot sheet has consent to be committed, and the Phase 0 scans are real print made before every change since.
- **Entry 77 section 4: oversize measured three ways, over the detector's diameter alone.** Entry 73's warnings came from the screen's size check, which is a different measurement, and the test had to see the one that was reported.
- **Entry 77 section 5: a sheet with no clear place prints no name, over shrinking the name further or trying another margin.** Section 5 states the rule. One place with one clearance is also what the placement test can check on every sheet.
- **Entry 77 section 5: the identifier caption left at the bottom beside the new name line, over moving it.** It is C6's recovery path, and its zone is what every sheet already printed is read with.
- **Entry 78 section 4: the calibre only vetoes a split, over also splitting a round blob of two holes' area.** Splitting on size alone would cut a genuinely odd hole to fit the expectation, which section 4's second guard rules out; the blob is flagged instead.
- **Entry 79 section 1: a measured ratio per image kind, over the calibre or one pooled ratio.** Scans and photographs measure holes differently, 0.944 against 0.986, and section 1 asked for them apart.
- **Entry 79 section 1: a photograph without camera data taken as a scan, over refusing the ratio.** There is nothing in such a file to tell the two apart, and the one such photograph measures 0.924, within the scan ratio's spread.
- **Entry 78 section 4: a calibre named after an uncorrected detection detects again, over asking.** Nothing a person did is lost, and a result found without the size is the one section 3 says is wrong.
- **Entry 80 section 2: the old sweep stopped unfinished, over letting it run.** Its synthetic holes were the wrong size, and its real rows used the bullet diameter, so its result could only have been discarded.
- **Entry 80 section 2: the synthetic holes scaled to read like .308 on a scan, over reweighting the sweep.** A scale fixes what the holes are; a weight would only change how much a wrong population counts.
- **Entry 80 section 2: no split threshold adopted, over adopting the survivor.** The only setting the real holes allow also changes the default without a calibre and moves committed synthetic records, and no real merged pair has been measured to say what it costs.
- **Entry 78 section 2: the residue fix proposed and not built, over a plain elongation cap.** A cap alone would refuse two real holes joined by the closing, a silent loss, and the safe form depends on the threshold still open.
- **Entry 81 section 2: the oversize flag rebuilt around a single-hole size, over keeping the median rule and lowering the calibre flag alone.** The median rule flagged ordinary holes on a tight sheet and let pairs through on a sheet full of them, so a calibre-only fix would have left the loud failure silent whenever no calibre is named.
- **Entry 81 section 2: the single hole without a calibre is the sheet's 25th percentile mark, over its median.** On the composite sheets half the marks were merged pairs, and the median moved to a pair's size and flagged none of them.
- **Entry 81 section 3: the residue fix on size and elongation together, over solidity.** Solidity overlaps across all three populations, real single holes reaching 0.59, while size splits the elongated ones cleanly.
- **Entry 78 section 2: a small elongated blob kept as one hole below 2.2, over refusing every blob the size vetoes.** Refusing a real hole is a silent loss, and the margin between the most elongated real hole, 1.72, and the split threshold, 1.80, is too thin to refuse on.
- **Entry 82 section 2: the floor used to veto and never to flag, over clamping the quarter-point alone.** On the clean photographs there were no round marks to clamp, and a floor that flagged would flag every real hole, since each is larger than the smallest a bullet makes.
- **Entry 82 section 2: the floor at 0.16 in, over 0.17.** 0.17 is the bullet; a .17 hole measures about 0.944 of it on a scan, and the floor must sit below any real hole.
- **Entry 82 section 3: two sizes need a gap of five pooled deviations, over three.** An even spread of sizes cut in half is 3.3 apart, so three would ask for a calibre on any wide one-calibre sheet.
- **Entry 82 section 3: no flags at all on two sizes, over tentative flags on the larger group.** Entry 82 asks for one sentence rather than many flags, and a sheet of two calibres would have every larger hole flagged as a merge.
- **Entry 82 section 6: the detector's flag drawn beside the size check's, over merging the two.** They measure different things, the detector's residual against the size check's dark region, and each says what it measured.
- **Entry 83 section 2: sizes converted at each blob's own scale, over leaving the detector alone as entry 83 section 4 asked.** Section 2's test found a wrong unit conversion rather than a tuning question. A size read 30 percent wrong on any oblique photograph is a defect, and the fix is a few lines.
- **Entry 83 section 2: the clean photographs' rise from 24 to 35 accepted, over restoring the single scale for the veto.** The single scale was wrong for real holes and for residue alike, and the residue it hid is the kind section 3 says cannot be resolved without a calibre.
- **Entry 83 section 4: the review queue computed from the marking, over storing it.** Every edit changes what needs review, and a stored queue would go stale; only a person's "keep it" is stored, because nothing else can know it.
- **Entry 83 section 4: the editor built into the marking screen, over a separate mode with Accept and analyse.** The statistics are already live on every edit, so an accept step would commit nothing. Discard edits is kept, as one undoable step.
- **Entry 83 section 4: the review keys taken on the tunnel route, over the window's key handler.** A focused button would otherwise take Space and Enter, and pressing Space would press the last choice again.
- **Entry 87 section 1: the screen's calibre size check removed, over keeping it and feeding it into the queue.** It measures the dark region connected to a mark, so a printed ring is part of every mark that touches one, and it read five confirmed single holes at about twice their size. Adding those five to the queue would have made the queue wrong rather than the panel.
- **Entry 87 section 1: `HoleSize` kept as a measurement.** The apparent extent is what the harnesses read and what the snap radius needs, and keeping it without a threshold is what makes the removal a removal of a judgement rather than of a number.
- **Entry 86 section 3: the enclosed-ink fraction recorded on every detection although nothing reads it.** It is the quantity that separates a hole on a ring from one beside it, it cost nothing to carry, and it is what proved the hypothesis wrong rather than plausible.
- **Entry 87 section 2: the README's states tied to DESIGN.md by a test, over a review habit.** Entry 60 found the README stale for days, and the two documents can now only disagree by failing a test.
- **Entry 90: the parametric editor and the visual designer deferred, over giving them a phase.** Alan is getting a specification from Jeff, and a phase for a screen nobody has specified would be a date attached to a guess. The deferral is explicit, carries its reason, and is checked by a test, which is the difference between parking something and losing it.
- **Entry 90: assisted hole placement raised as a question, over scheduling it or dropping it.** The detector differences against a definition and a store-bought target has none, so the bullet is not schedulable as written; but two of the three meanings of "assisted" are already built, so it is not droppable either. That is a design answer and not a wording fix.
- **Entry 90: the scope test requires a citation on a deferral.** A bullet could otherwise satisfy the test with the word alone, which is exactly the quiet drop the test exists to catch.
- **Entry 88: the oversize flag left alone although the measurement points at a defect in it.** Four of five flags on real material hold one hole's worth of ink inside a ragged hull, so flagging on ink area would remove them; the threshold is one the Phase 1 gate is measured against, and entry 83 section 3 says stop tuning the detector. Recorded with its numbers instead.
- **Entry 84: `PhotographHoleToCalibre` changed to the re-measured 0.948 and `ScanHoleToCalibre` left at 0.944.** The photograph figure was measured with the single-scale defect in place; the scan figure was not, and its 0.006 move comes from a longer verified hole list rather than from the fix.
- **Entry 84: the 25-shot rehearsal recorded as a rehearsal.** It measures the software's share of the two minutes and nothing about a person, so recording it as the Phase 3 gate would put a state of "done" on a page where the thing being gated has not happened.
- **Entry 94 section 1: the oversize threshold left at 1.35 while the quantity under it changed.** Area reads about six percent lower than the hull-derived reference it is compared against, so the threshold is already slightly stricter; and the value that would catch every 0.10 in pair would also flag S1b again, which is the false flag the change exists to remove.
- **Entry 94 section 1: the hull area kept beside the mark's area rather than dropped.** Solidity is the ratio of the two, and entry 88's shape measurements rest on it.
- **Entry 94 section 4: the split's two centres computed at detection time and carried on the flag, over computing them when the choice is taken.** The review queue has no image, and a choice that needs the pixels is a choice that needs a mouse.
- **Entry 94 section 2: subgroups keyed by bull rather than by shot.** A shot moves between bulls during review and its load does not; the sheet's layout is what holds the loads.
- **Entries 91 and 92: the zero correction refuses rather than rounds.** Where the offset is inside the sampling error the panel gives a shot count instead of a number, because a bare figure will be dialled.
- **Entry 93 section 3: high contrast derived from the dark tokens in code, over a fourth hand-drawn palette.** A palette that cannot be derived is evidence the roles carry values rather than meanings, and deriving it is what proves the concept is a design language rather than one screenshot.
- **Entry 95 section 2: the count item acts on its first candidate only.** Naming three and acting on one keeps it a key press; a person who disagrees with the ranking reaches the right mark through its own item or by selecting it.
- **Entry 95 section 2: every mark gets a size in holes, measured against the veto's size where no flag size exists.** The ranking needs a size on unflagged marks, which are most of the candidates, and only the flag's own size may raise the flag.
- **Entry 95 section 3: nothing changed for the mounted gate.** The investigation found where the error lives and what would separate the two explanations left; a change made before that photograph would be tuning against the frames being gated.
- **Entry 96 section 2: the warp that passes `IMG_5819` not adopted.** It was found on the frames being gated, it makes `IMG_5820` worse, and a model chosen because it passes the frames that prompted it is not a gate result.
- **Entry 97 section 1: a shot a person placed is neutral, not teal.** Teal means the software found it on its own, and a mark a person put down or moved is not that.
- **Entry 97 section 2: the marking keeps a copy of the rifle rather than its name.** A correction read from a saved marking must be the one that was right when it was shot, whatever the record book says since.
- **Entry 97 section 2: a barrel's count grows only on a person's step.** Counting automatically on detection would count a reopened sheet twice.
- **Entry 97 section 3: the per-stage rasters left for the next batch.** The records land live and cost nothing; the rasters would need keeping images the detector currently throws away, which section 19 allows for one interactive analysis and which deserves its own measurement of cost.
- **Entry 97 section 5: the window rehearsal pinned at 10 presses.** It is its own baseline at 300 DPI, and a ceiling the next batch must not raise.
- **Entry 98 section 3: a control added that the entry did not ask for.** The holed half and the clean half are different places on the page, and without the same split on unshot sheets a positional difference would have read as hole damage.
- **Entry 98 section 2: the nominal hole is .30 when nothing better is known.** It is the middle of what the corpus carries, and the sheet's own measured holes replace it as soon as the detector has run.
- **Entry 99: the editor ports the library's solver rather than calling it.** `tools/` is planning's and Python is not shipped; the port is held to the original by rebuilding ten sheets exactly.
- **Entry 99: the fewest markers allowed is 9, the fewest any built-in sheet carries.** It is the one figure the project has shown registration holding at, on GL-LR300-T's tile, and choosing a lower one would be a guess.
- **Entry 98 section 5: only the residual is kept.** The markers, corners and rejections were already in the result, so the interactive run's extra cost is one image.
- **Entry 101: the homography's final fit carried to convergence, over stopping at OpenCV's ten iterations.** A fixed iteration count reproduces the native figures only as far as every other detail of its solver does, and convergence is a definition every platform reaches the same way.
- **Entry 101: native code kept for the integer steps.** Candidate detection, decoding and RANSAC's inlier choice were identical on every platform in every gate record run; porting them would add risk to steps that already agree.
- **Entry 101: the contour lines fitted in double precision, over emulating OpenCV's single precision.** The emulation reproduces native and proves the contours are the same, but its answer is up to 0.09 px from the least-squares line it sets out to compute.
- **Entry 101 section 5: the picture carried on the stage record, over a separate artefact channel.** The record already reached the timeline live, so attaching the picture to it makes the two arrive together, and a trace put on the timeline after its run draws its pictures by the same path.
- **Entry 103 section 1: extreme spread drawn as the line between its two shots, over the concept's circle.** A circle that size reads as a region containing the shots; extreme spread is a distance between two of them.
- **Entry 103 section 1: Show work opens the timeline in the editor, over a second timeline in the analysis state.** The timeline already shows the work, and one copy cannot disagree with itself.
- **Entry 103 section 2: the flyer card says "further out than a group this size usually puts its worst" below one time in twenty, over always saying "not a flyer".** The old line said not a flyer whatever the distance; the card still leaves the call to the shooter.
- **Entry 103 section 3: held as a question, over running the sweep.** The sort was already committed by entry 52, and regenerating would have reproduced entry 101's records.
- **Entry 104 section 2: the worst shot calibrated by simulation at every count, over withholding the verdict below some count.** The calibration is valid from the dispersion minimum up and costs milliseconds, so there was no count where withholding was the more honest answer.
- **Entry 104 section 4: only the inked discs faded, over fading the whole bull.** Fading the paper as well turned it grey on dark chrome, and the paper-on-dark contrast is most of the concept's character.
- **Entry 105 section 6: the work bar defaulted closed, over open.** Alan asked for the strip off the screen; a failure stays a prominent error without it, and Show work turns red and says so when a stage fails.
- **Entry 105 section 7: a name read by its leading number only after the table, over the table alone.** "6.5 Creedmoor" and "30-06" are what shooters type, and their leading number is the table's name; a wildcat still falls through to the old rule and says what it read.
- **Entry 105 section 4: the mark drawn in the files' own colours until question 20 is answered, over recolouring it to the nearest tokens.** Recolouring would change a mark Alan chose to a set of colours nobody chose.
- **Entry 105 section 5: the icons drawn by a command from the committed mark, over a script outside the build.** The CLI already carries the imaging library, and nothing new is installed.
- **Entry 106 section 1: the viewer path on every platform with a confirmation dialog, over keeping the print verb anywhere.** The verb printed silently at the viewer's own scaling on Alan's machine, and GroupLab cannot see what any registered print command does.
- **Entry 106 section 4: the PDF drawn by the renderer's own writer, over printing the Markdown through a browser.** The other PDFs came from Chromium by hand; a command in the repository keeps the list and its PDF in step with the code, and installs nothing.
- **Entry 106 section 5: raised as question 21, over building it now.** It is about a run of its own, three decisions are open, and its test needs a PDF printer the CI runner may not have.
- **Entry 107 section 1: "9mm" read as a diameter, over refusing it.** The section's rule reads any number marked mm, and its test list refuses "9mm"; the rule is built and the conflict is question 22.
- **Entry 107 section 2: the quiet zone not counted in the margin refusal, over refusing on it.** The margin leaves bare paper and the quiet zone is bare paper, so it prints as intended, and a smudge near the edge lands on ink, which is refused on its own account.
- **Entry 107 section 2: `WindowsPrinter` in the CLI project, over the application.** The Core tests reach it there to print and measure a real job, and they already reference that project for the imaging backend.
- **Entry 107 section 2: markers located on the printed page with no fitting, over registering through a homography.** A homography absorbs scale and offset, which are the errors the test exists to catch.
- **Entry 108 section 2: designations compared as decimals at the value typed, over matching the text.** ".270" and ".27" are one value, and matching text would let one form through that the other refuses.
- **Entry 108 section 2: the refused value shown as typed with its unit, over normalising it.** The person sees their own entry named, which is what the refusal is about.
- **Entry 109 section 1: "why" as a compact disclosure on each item, remembered per item, over one panel of explanations.** An explanation read beside the figure it explains needs no hunting, and closed it is one short line.
- **Entry 109 section 1: the older size names mapped onto the five, over replacing every use.** Every existing use lands on the scale at once, and the test catches any new size.
- **Entry 109 section 2: settings as a screen in the main window, over a dialog.** The rail is navigation, and the gear is a destination like Print.
- **Entry 109 section 3: the flyer card's hedge kept in view, over moving it behind "why".** Without "by that measure alone" the verdict would say more than the test can.
- **Entry 109 section 3: excluded rows struck through, over a word in the row.** One number per shot leaves no room for a word, and the tooltip says it.
- **Entry 110 section 2f: the tolerances committed and pushed before the reference tables were generated, over writing them in the same commit.** The history then shows the order, which is the point of stating them first.
- **Entry 110 section 2f: both references generated on a GitHub runner, over installing Node and py-ballisticcalc here.** Nothing is installed on the development machine, and the same runner re-checks the JavaScript on every push.
- **Entry 110 section 2f: the G1 cases held as named known failures, over a red check or a wider tolerance.** A red check would block every other change, and a wider tolerance would make the gate mean nothing; the named list fails the day it is no longer true.
- **Entry 110 section 2a: the Coriolis vertical term held out rather than ported with its sign corrected.** The entry said to port it as it stands, so the conflict is a question, not a quiet fix.
- **Entry 110 section 2c: aerodynamic jump left out, over Litz's fit from memory.** The entry required the coefficients from the publication, which was not to hand.
- **Entry 110 section 2b: the rifle zeroed on the flat and then tilted, over zeroing at the shooting angle.** A rifle is zeroed at a range and then carried to the hill.
- **Entry 111 section 1: poncelet as the second transcription, over the gehtsoft ports.** Those are py-ballisticcalc's own ancestry, so agreeing with them would prove nothing; poncelet reached JBM's McCoy tables through JBM's files.
- **Entry 111 section 1: the JavaScript's tables kept only for the compatible mode, over deleting them.** The transcription check compares the port with the file as it is, and that needs the file's tables.
- **Entry 111 section 3: "why" as a plain button beside the item's last line, over a toggle.** The theme paints a checked toggle amber, and an open explanation needs no one's attention.
- **Entry 111 section 3: the count kept in the placement sentence, over the "Shots" row.** The sentence carries the count and how the shots were placed; the row carried the count alone.
- **Entry 111 section 4: the timing read from the log, over a stopwatch in the window.** The log already records every step with its time and no path, so the measurement needed no change to the application.
- **Entry 112 section 1: the sheet's registration kept in the marking file, over re-detecting on reopen.** Section 18 says no image is ever needed to reopen, and without the mapping a session reopened from its marking had no scale.
- **Entry 112 section 1: the proof image as a JPEG in the database, over a file beside it.** One file is the whole record, and the export carries it.
- **Entry 112 section 2: the report's words from the screen's own functions, over a second wording.** Two wordings drift, and the rule is that paper says nothing the screen does not.
- **Entry 112 section 3: deleting a sheet a session used allowed, over refusing.** Every session keeps its own copy of the definition, and refusing would make a sheet undeletable while any session of it is kept.
- **Entry 112 section 4: elevation carried by a central difference of the zero range, over a new solver input.** It needed no change to the validated solver, and a test holds it to a directly flown change within 1 percent.
- **Entry 112 section 4: the dope table on a Ballistics screen with its own rail slot, over a panel in the analysis.** The analysis column is 372 pixels, and the table has seven columns; question 26 asks.
- **Entry 113 section 2: the chart slot as Compare loads, over Reports.** The concept's chart icon is Compare loads, and a report is written from its analysis.
- **Entry 113 section 2: sessions at different distances compared as angles, over refusing.** Dispersion scales with distance, and the screen says it compared them as angles.
- **Entry 113 section 3: an optional fixed launch angle on the solver's input, over a zero-range difference for velocity.** A change of velocity with the zero kept would move the bore; the angle must be held, and without the field nothing changes.
- **Entry 113 section 4: the rule re-baselines the detected reading, over counting its moves as edits.** A rule is how the sheet is read, and flagging every shot it placed as moved would raise the doubles again.
- **Entry 113 section 6: JPEG pictures in GroupLab's own PDF writer, over another PDF tool.** The project's reading documents already come from that writer, and a JPEG goes in as it is.
- **Entry 113 section 7: the new screens held to their themes' tested text roles, over pixel contrast measurement.** The roles are what ThemeTests holds to their ratios, and a render's pixels vary between machines.
- **Entry 114 section 1: every filled shape drawn as a closed path, over keeping `FillRect` with a capability check.** Both drivers report the same RASTERCAPS, so a capability check cannot tell them apart; a path is honoured by every driver.
- **Entry 114 section 1: the page aborted when a drawing call fails, over printing what did draw.** A sheet with a marker missing looks normal and cannot be measured, and the fault is found only when it comes back from the range.
- **Entry 114 section 1: the in-app Print button demoted, over disabling it.** Hiding it would leave a person who wants it with no path and no reason; the screen now says what was wrong and what is safer today.
- **Entry 114 section 1: the drivers the tests print through are a fixed list, over enumerating the machine's printers.** A driver can open an application when it is printed to, as the OneNote one does.
- **Entry 115 section 2: bulls chosen with shift and click, over a plain click.** Every bull on a shot sheet has a hole on it, and a plain click there is the hole, which is what the editor has always done with it.
- **Entry 115 section 3: the velocity SD written to the load record with its provenance, over showing it on screen alone.** A figure that looks typed and a figure that was measured are different things, and the record is where the solver reads it.
- **Entry 115 section 3: the schema upgraded in place on open, over refusing an older database.** A person's sessions are not something to make them re-create, and the upgrade is one statement in one transaction.
- **Entry 115 section 4: a sheet read as the wrong definition warned about, over refused.** A damaged or marked-up sheet looks the same to the evidence, and the person can see the sheet.
- **Entry 115 section 4: the low-resolution threshold measured, over assumed.** The detector reads a sheet at 96 dpi and fails at 60, so the message is keyed at 120 rather than at the 150 that seemed obvious.
- **Entry 115 section 6: the corrected JavaScript generated from the original by a script, over edited by hand.** Every change is then exactly the five, and the file can be regenerated when the original changes.
- **Entry 116 section 1: a folder publish, over a single file.** The native OpenCV library then sits beside the executable where the loader expects it, nothing unpacks itself into a temporary folder on first run, and an antivirus sees an ordinary folder rather than the self-extracting shape it distrusts.
- **Entry 116 section 2: the shot sample generated by GroupLab, over shipping no shot sheet.** The repository holds no shot sheet that may be published, and a tester who cannot see the figures in the first minute has not seen GroupLab. It is question 29 all the same.
- **Entry 116 section 2a: the packaging script warns about a missing Inno Setup locally and refuses in a release.** A working copy should still build a zip on a machine with no installer tooling; a release that quietly shipped one asset of two would be worse than a failed release.
- **Entry 116 section 3: every asset attached twice, versioned and stable.** A bug report then names a build, and the README's front-page links keep working with nobody editing the README after a release.
- **Entry 117: the benchmark generates its own material, over reading anything a person has.** It then runs on a bare checkout, on CI and on a machine that has never analysed a target, and no case can quietly depend on one machine's folder.
- **Entry 117: the stage timings come from the stage record, over a second timing path.** Two clocks disagree eventually, and the one the application already files is the one a person sees in Show work.
- **Entry 117 section 3a: coverage checked by reflection over what does work, over a list of cases.** A hand list rots the moment a feature is added; this one named 25 gaps the first time it ran.
- **Entry 117 section 3b: the interface benchmark lives in the test project, over the command line.** Timing a control needs a windowing platform, and shipping a headless one inside the application to measure it would be a cost carried by every person to serve a benchmark.
- **Entry 117 section 3b: a dropdown measured by choosing from it, over opening its popup.** Choosing is the work; opening a popup headlessly crashes in the toolkit, and it would have been measuring the platform rather than GroupLab.
- **Entry 118: the anchors derived in the test from the heading text, over read from the list.** A renamed heading then fails the test rather than leaving a link that scrolls nowhere, which is the failure a reader cannot see.
- **Entry 118: the contents list after the Download section, over at the top.** Somebody who came to get the program should not have to read past a contents list to find it.
- **Entry 119 section 1: the updater ranks the trains, over SemVer's own text ordering.** SemVer puts beta below nightly because "b" sorts before "n", and section 4.5 needs the opposite. Question 30.
- **Entry 119 section 3: ECDSA P-256, over the Ed25519 the entry asks for.** .NET 10 has no Ed25519 and this machine has no NuGet source, so no package can be restored to provide one. The algorithm is named in every manifest, so a later change is one older builds refuse by name. Question 31.
- **Entry 119 section 3: the workflow fails loudly with no key, over publishing unsigned.** An unsigned manifest is one the application would have to trust without being able to check it.
- **Entry 120 section 4: the failing result carries its definition, over the screen falling back to the pipeline's words.** The advice written for a sheet that will not register was unreachable in exactly the case it was written for.
- **Entry 120 section 10: the preview sits in the grid row, over inside the scroll viewer.** Inside one it measured its own natural size and left the window two thirds empty; the scroll viewer is now used only when zoomed, where panning is the point of it.
- **Entry 121: the version raised to 0.2.0, over renaming what is already published.** v0.1.0 is history and stays where it is; the train moves above it instead.
- **Entry 122: one interface for everything outside the process, over telling the benchmark not to click that button.** An exclusion list would have fixed this button and left the next one to be found by somebody's browser opening.
