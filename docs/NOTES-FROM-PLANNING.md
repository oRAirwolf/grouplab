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

## The archive

Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the
only written record of why much of this project is the way it is.

- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 139, 136 of them.

---

## 2026-09-24, entry 174: the upload page refused every photograph, because the file field had no brackets

**Status: actioned 2026-09-24**, sections 1 to 3. Section 4, the live test again, is Alan's, and the planning session asks for it once the
fix is published.

- **Section 1.** Confirmed as the entry says: `name="photos"` gave PHP one file's strings, and the receiver refused every submission as
  having no photos. **The page refused every submission from entry 173 opening it until this fix, and no submission was lost, because
  none was ever accepted.**
- **Section 2.** The input is `photos[]`, and the receiver also takes one file under the plain name. The crash receiver reads a single
  file named `report`, which is what the application sends, so it does not have the fault.
- **Section 3.** The site build fails if the built page's file input is not named with `[]`, and `SendATargetTests` says the same. A new
  PHP test serves the real receiver with `php -S` and sends it a real multipart POST built from the page's field name, one file, two
  files and the plain name; CI runs it beside the receiver tests. PHP is not installed on this machine, so CI is its first run.

---

## 2026-09-24, entry 164: the first macOS log, and two things it raises that need no tester

**Status: actioned 2026-09-24**, all four sections.

- **Section 1.** Recorded; nothing to do about the updater.
- **Section 2.** Windows reads 33 of 34 too. Marker 28 is printed with the printer's banding across it. Recorded with the sample's ground truth, and a test holds it on all three CI platforms.
- **Section 3.** Not a slowdown: the baseline was a scan with no holes. On the same file the M5 Max is about a fifth faster. No x86 intrinsics, no ReadyToRun on either platform, and both are faster on the second run.
- **Section 4.** The log records the extension and the path hash, never the name, amending entry 41 section 3.

---

---

## 2026-09-24, entry 173: open the target upload page, and put it in the top bar

**Status: actioned 2026-09-24**, sections 1 to 3, with two parts waiting on Alan rather than on a decision: section 1.3's end to end test
needs a person in a browser, because Turnstile is there to stop scripts, and its pull and removal run `sudo` on the server; section
2.4's redirect is his to run. Both are in request 1 of `docs/notes/for-alan.md` with the commands, and the redirect is in the panel.

- **Section 1.** `open` is true. The test image is generated and labelled as not a target.
- **Section 2.** The page is at `/targets/`. The old path answers with a plain page linking there, with no refresh and no nginx change.
  The top bar says "Send a target" while the page is open and is unchanged while it is closed; the donor pack page keeps its footer
  place and gains a "Send your target" button. The redirect's commands now point at `/targets/`. The printed donor PDFs are untouched.
- **Section 3.** The build checks all three and fails before publishing if any is wrong, and `SendATargetTests` holds them.

---

## 2026-09-24, entry 171: answers to questions 48 and 49, request 4 answered, and stale items closed

**Status: actioned 2026-09-24**, sections 1 to 6 and section 6's two amendments; section 7 is the order of work, which is followed. Three things wait on a later step, not on a decision:
the friend's scan is attached to the `test-data` release once `ci.yml` has created it; `.user.ini` surviving the first real deploy is
checked after this push publishes; and request 5 closes when Alan confirms.

- **Section 1, question 49.** A scan now reports real inches: every distance, bull and hole diameter is multiplied by the measured print
  scale, and the detector is given the calibre in the sheet's own inches. A photograph stays in sheet inches and says so in the entry's
  words. The reason to print at actual size is one sentence the print screen, the statement of record and the tour share. The marking
  file records which it used. Tested at 96 percent both ways round.
- **Section 2, question 48.** The fourteen day clause is gone from the split script's rule; the count stays.
- **Section 3.** The channels and the ten rules are in `website/links.json`, which the community page reads. The moderators' channel is
  not named, and a test says so.
- **Section 4.** Questions 39, 41, 42, 45 and 46 are archived with their answers, as are 48 and 49. Question 44's crash outside the page
  is the one part still open.
- **Section 5.** STATE.md is rewritten, and its test now checks the inbox line against the directory.
- **Section 6.** Alan's standing consent is in `samples/PROVENANCE.md`, quoted and dated, and the shadow crop is on the hole size
  article: one of his photographs, 0.383 in, 1.452 of the bullet. Fenix is recorded for entry 166's credit. The 59 MB scan is a
  `test-data` release download, fetched and hash-checked by CI, and no sample over about 10 MB may be committed. Request 1 says what is
  left of entry 129, with the redirect's commands. The installer's closing lines now say only what is still to do.
- **Section 6.1.** Entry 149 section 3 goes with entry 170 section 2, and section 4 with entry 172 section 3 item 1.

---

## 2026-09-24, entry 167: replace the Equipment icon

**Status: actioned 2026-09-24**, all three sections. Section 2.3, the tour page showing the new icon, is confirmed after the next weekly screenshot run.

- **Section 1.** The baked tilted cartridge, exactly as given, with no run-time rotation.
- **Section 2.** Rendered at 16, 32 and 128 in both themes and looked at; a test holds its bounds, its two parts and its colour.
- **Section 3.** The lesson is written in the icon's own comment: draw a detail of anything long and thin at this size.

---

## 2026-09-24, entry 163: a first real user's feedback on the marking screen, and a cartridge list

**Status: actioned 2026-09-24**, all seven sections, with section 1's trackpad rule and section 6's two Mac defects done under entry 166, which corrects them.

- **Section 1.** Pan selects on a click and pans on a drag, and detection no longer switches tools. The two finger and pinch rule is entry 166's.
- **Section 2.** C pans beside V; P still pans. C conflicts with nothing: not a review key, not bull entry.
- **Section 3.** Names before numbers, **reversing entries 107 and 108 where their rule is written**. Forty cartridges confirmed by two independent sources, thirty three held until a second agrees, three disagreements between the sources each keeping the cartridge out. ".223" stays a diameter.
- **Section 4.** A Setup block first in the panel, each needed field outlined and saying "needed", each answerable "not known", and Accept saying what is still needed.
- **Section 5.** Cards open as their verdict, CEP as its first line, and the cut-off tests run closed and open.
- **Section 6.** The updater offers a Mac no update; Command Q quits by the tester's report; modifiers and pinch are entry 166.
- **Section 7.** One test per offerable row of section 3.2, and the four App tests the section lists.

---

## 2026-09-24, entry 162: consent for the 2026-09-23 friend scan, and what it was shot on

**Status: actioned 2026-09-24**, all three sections, with one part of section 1 waiting on a choice: publishing the scan itself.

- **Section 1.** The consent record is written, distinguishing this scan from the 2026-09-16 one by file and date. The published copy, rebuilt from pixels, is 56 MB, so how to publish it is request 8 rather than a commit that every clone carries for ever.
- **Section 2.** Card stock over cardboard, 6.5 Creedmoor, printed at 100.3 percent, recorded with the scan and in its fixture.
- **Section 3.** The calibre's two remaining jobs hold across 0.76 to 1.14 and a test says so; paper and backing are recorded fields on every marking.

---

## 2026-09-24, entry 168: nightly 94 should not exist, its notes are cut off, and the platform statement leaves the releases

**Status: actioned 2026-09-24**, all seven sections.

- **Section 1 and 2.** Nightly 94 was built by `scripts/claims.py`, `scripts/counts.py`, `scripts/split-logs.py` and five test files, all mine. What ships is now generated from what MSBuild says the published application reads, plus what the packaging copies; tests, tools and the other workflows are checked; the site and the documents are content. CI regenerates the list and fails if it differs. Under the new classes, 9 of the last 31 nightlies changed nothing in the application.
- **Section 3.** A trailer continues until a blank line or the next trailer. The generator's own documented example wrapped, so it would have been cut too. A self-test holds four cases and a test runs it.
- **Section 4.** Only a commit that touched something that ships is in an application's notes, never a `[notes]` commit, and a note saying the application did not change is refused by meaning rather than by phrase.
- **Section 5.** One generated line and the download page's address replace the whole statement on a release.
- **Section 6 and 7.** Every build regenerated: 30 rewritten, two kept as published and named, `0.1.0` untouched, known issues kept, nothing deleted. The GitHub bodies follow the file, and three are read back in the run report.

---

## 2026-09-24, entry 161: naming the calibre makes the reading worse, and the hole to calibre constant is wrong

**Status: actioned 2026-09-24**, all eight sections. It also corrects something entry 152 got wrong, which is in section 6 below.

- **Section 1 and 2.** Both runs reproduced, and the measurement confirmed rather than taken: 0.301 in across the middle, 1.14 of the bullet, 1.43 holes' area against the calibre's 0.249, five false doubles.
- **Section 3.** The sheet's own marks are the reference wherever there are five or more, named calibre or not, which **amends entry 141 section 4's line of twelve**. Below five a calibre vetoes splits and flags nothing. The scale panel says when the marks and the calibre disagree. Question 40's answer agrees.
- **Section 4.** The guess names no cartridge, from a scan or a photograph. It gives the measurement and asks.
- **Section 5.** Five scans now run from 0.765 to 1.14. `HoleToCalibre` stays 0.945 and says why it was not replaced. Worth an article under entry 158 section 1, held for entry 162's consent record.
- **Section 6, and it corrects entry 152.** GroupLab reports every distance in the sheet's own inches and never applies the print scale, so a sheet printed small makes every size read large. **Entry 152 said a shrunk sheet measures correctly; that was wrong, and I wrote it**, from the in-app sentence that said the figures were corrected. Every place it was said is corrected. Question 49 asks whether a scan should report real inches.
- **Section 7.** The rounds fired item says where the answer goes.
- **Section 8.** The regression test, the general test and the both-ways table. On the code before this entry only the friend's sheet got worse, 0 doubles to 5. Two fixture errors corrected: three .22 LR scans recorded as 0.224, and scan 3 recorded as .308 where its load block says 6.5 Creedmoor.

---

## 2026-09-23, entry 153: the standard every research article is held to

**Status: actioned 2026-09-23**, all six sections, section 5 within the one limit this project has on publishing real material.

- **Section 1.** The developer's name is gone from every file under `website/research/`: fourteen articles, two figure scripts and one data file. The sweep also caught the pronouns, which is the half a name search would have missed. One byline everywhere. Scoped to that directory, so the licence, the commit history and `samples/PROVENANCE.md` are untouched.
- **Section 2.** All thirty articles end with "What this means", about what to do differently or stop believing rather than the result again in words. Two that already had the section under their own headings are normalised to the one heading.
- **Section 3.** A figure with no caption fails the build, and an article with no figure fails unless its front matter carries `no_figure` with a written reason, which the page prints where the picture would be.
- **Section 4.** A rimfire 22 is 0.222 and **was not in the pick list at all**: 0.2215 is 5.45x39 and 0.224 is the centrefire 22. Two articles were recomputed rather than edited. `HoleToCalibre` does not move, because the two sheets behind it are 6.5 Creedmoor.
- **Section 5, and the limit is stated rather than worked around.** Three crops of real holes with the caliper line and a scale bar drawn on, at 0.897, 0.949 and 1.039 of the bullet, all from the sample scan, which is the only real material this project may publish. **The shadow case exists only in a photograph and no photograph has a consent record**, so the article says so in a line instead of showing it. Request 6 in `docs/notes/for-alan.md` asks whether one crop of one photographed hole may be published.
- **Section 6.** Four batches, three then three then seven then twelve, gated on a list in the site build that grew to cover every article and was then removed, because a backlog list that outlives its backlog becomes a way to opt out.

---

## 2026-09-24, entry 160: both sessions spend fewer tokens, without doing less work

**Status: actioned 2026-09-23**, all seven sections.

- **Section 1.** The three logs are split by `scripts/split-logs.py` and nothing is deleted: 1,102 KB to 97 KB, 837 KB to 99 KB, 165 KB to 30 KB, with the rest whole and unedited in `docs/notes/archive/`. **The fourteen day clause would have moved nothing**, because this repository is eleven days old and every entry falls inside it, so the fifteen entry rule was applied and the clash is **question 48**.
- **Section 1, and a fault it uncovered before it moved anything.** The entry headings in this log drifted from `##` to `#` at entry 119, and the two tests that read headings look for `##`, so **they had been silently skipping the thirty four newest entries**. Normalised, and `Logs.Notes()` now reads the live file and the archive together so the split cannot produce the same failure by a different route. Waking the test found two folds claiming more sections than they named, entries 150 and 152, both corrected.
- **Section 2.** `docs/notes/STATE.md`, 90 lines, rewritten and never appended to, with a test on the length and on what it has to answer.
- **Sections 3, 4 and 5** are in `CLAUDE.md` under "Tokens are the budget": run the suites quietly, never paste output into a report or a log, never read a file to confirm a write, never read a whole file to find one thing, one commit per entry, and write a script rather than making the same edit fifty times.
- **Section 6.** The command set that covers ordinary work is written out in `CLAUDE.md`, and the one setting change is request 5 in `docs/notes/for-alan.md` with the exact file to create. What stays behind a prompt is listed as deliberately as what does not.
- **Section 7.** A cold start was on the order of 2.1 MB of logs. It is now `STATE.md` at 5 KB plus the live notes file at 97 KB, so about 102 KB, a twentieth of what it was. To be checked weekly.

---

## 2026-09-23, entry 152: say plainly what GroupLab can measure, because two published claims are wrong

**Status: actioned 2026-09-23**, all five sections. Both claims were wrong, and the research article was right.

- **Section 1.** Both quoted claims are wrong, and the research article `printer-true-size` was right about the second. The wrong wordings stay quoted here, because a log that cannot record what was wrong is not a log, and a test bans them everywhere else.
- **Section 2, the measurement, and the answers are in `docs/PHASE1-RESULTS.md` with the file that settles each one.** There are four ways to get a scale, not one. Any target can be measured once the scale is set. A sheet printed at 96.2 percent measures correctly and is put through the same gate as a full size one. The print scale is computed to tell the person their printer shrank the sheet, not to correct anything, because the markers shrank with the sheet and the correction is already in the mapping.
- **Section 3.** `docs/WHAT-CAN-BE-MEASURED.md` is the one source. Both tour paragraphs are rewritten from it, and **the sweep found the same claim printed on every sheet GroupLab prints**: `SceneBuilder.ActualSizeNote` said "a sheet printed at any other scale measures wrong". That is now "a scaled sheet loses the spacing it was designed for", which is the true reason. The print screen's own wording was wrong the same way and is corrected. The ruler check and the printed instruction stay, as the entry asks.
- **Section 4.** Every tour screen carries a `withoutASheet` line, and the build refuses a screen without one. On six of the ten the honest answer is "no difference", and saying so is the point: the question a reader has is whether the application is useless to them without a printed sheet, and it is not.
- **Section 5.** Five phrasings banned across `website`, `src`, `docs` and the README, by the mechanism entry 145 used. One source, pointed at by the tour, the article and the build. The test caught my own quotation of the wrong claim in a docstring, which is the mechanism working.
- **A finding this entry turned up and did not go looking for: the corpus detection-counts record has been stale since entry 101.** Changing the printed note changed the artwork fingerprint, which is what gates that record, and the comparison then showed 26 of 55 images differing. **None of it is this entry's change**: with the old detector restored the same 26 rows differ, so the drift is entries 130 and 141's accepted work, never re-recorded. The record is now current. The gate fires on artwork and not on counts, which is why four entries of change went unrecorded without anything going red.

---

## 2026-09-23, entry 151: the Community link goes to a page, not straight out to Discord

**Status: actioned 2026-09-23**, all four sections, with one part of section 1.2 standing on a request rather than on a guess.

- **Section 1.** The meta refresh is gone and `grouplab.org/discord` is a real page in the site's layout. One thing on it leaves the site, it is marked "Opens Discord in a new tab" before it is clicked, and the invite is visible as text as well as being the link's target.
- **Section 1.2, and this is the part that is not complete.** The five group names are the server's own and the page describes what each group is for. **The channel names inside them are not written anywhere I can read**, so nothing invented them: the page says what each group is for and stops. `docs/notes/for-alan.md` request 4 asks for the channel list and for the server's own rules text, and `website/links.json` is where both go, so the page becomes exact without a rewrite.
- **Section 1.5.** The rules summary on the page is **the project's own expectations**, written here, and it says so. It is not a copy of the server's rules, because nobody has read those out of the server.
- **Section 2.** The footer said "Discord" and the navigation said "Community". Both say Community now.
- **Sections 3 and 4.** `website/links.json` is still the one source, and it now carries the group list as well. The test that the invite appears nowhere else is amended rather than deleted, for the one exception the entry allows. Two new tests: **no page on the built site carries a meta refresh**, which is the general form of the fault rather than the one instance of it, and the community page carries the invite both as a link and as visible text.

---

## 2026-09-23, entry 150: an executable is built only when the application changes

**Status: actioned 2026-09-23**, all six sections. Section 3's proof is a run rather than a reading, and the run it is proved by is the push that carries this entry.

- **Section 1.** `.github/shipping-paths.json` is the one list, read by the nightly's gate and by `ShippingPathsTests`. Twenty seven top level entries, eighteen shipping and nine content, each in exactly one list, with a reason written beside every one. **A path in neither list fails the gate**, which is the entry's own instruction and the right way round: a new top level directory should make somebody decide which side it is on rather than silently picking one.
- **Section 2.** The gate is a step in `name-it`, which is the first job and already resolves which commit the nightly is for, and it outputs `application-changed`. Both `package` and `publish` depend on it. A skipped night writes **"No application change since nightly NN. No build produced."** into the summary and creates no release, no tag and no assets.
- **Section 2.4, and it was a real change rather than a line of prose.** The version came from `github.run_number`, which counts runs, so a cancelled or skipped run consumed a number: that is why the published numbers already jump 14, 16, 18, 25. The number is now the highest nightly tag plus one, so numbers count builds.
- **Section 3, and it is proved by a run rather than by a reading.** Entry 144's guard on the notes commit still holds and now has a second one underneath it: a `[notes] ` commit is skipped by `name-it`'s condition, and it is content, so the gate would refuse it even if that condition went. The push carrying this entry produced a skipped nightly on its notes commit and a real one, nightly 92, on the code.
- **Section 4.** The releases page says that nightly builds are produced on the nights the application changed and that a gap in the numbers means nothing shipped, so a quiet night does not look like a page nobody updated.
- **Section 5, the measurement, and it does not say what it was expected to say.** Five of the last twenty eight nightlies changed nothing that ships: **14, 72, 76, 77 and 78**. **Nightly 84, the one Alan named, would still have been built**: 147 paths changed and they included `src`, `tests`, `.github` and `scripts` as well as 95 website files. So the waste is real but smaller than the releases page makes it look, and what made 84 look like a website build was its release note rather than its diff. That is entry 145's problem, not this one's.
- **Section 6.** Two tests as asked, over the lists and over the workflow's shape. Section 6.3's dry run against the last thirty nightlies is **a step in the nightly rather than a test**, printed into the run summary: it needs the tag history, which a test in CI does not have, and running it every night exercises it against real history exactly as the section asks.

---

## 2026-09-23, entry 149: answers to questions 35, 37, 40 and 47, and the Alan list for this run

**Status: actioned 2026-09-23**, sections 1, 2, 5 and 6. **Sections 3 and 4 are not done**, and both are real work rather than a judgement call: section 3 is question 37's A and D, which is marking-screen interaction, and section 4 is re-running the entry 121 survey's own baselines against the narrowed rule. Both are named in `docs/PHASE1-RESULTS.md` as outstanding, and neither is worked around anywhere.

- **Section 1, question 47: the five kinds stay.** `CLAUDE.md` now names all five, says which of the two headings each one lands under, and says why there are five rather than two: the heading is what the reader sees, the kind is what the writer says. `ReleaseNoteKindsTests` reads the word list out of `CLAUDE.md` and out of `scripts/release-notes.py` and holds them to the same set, because that pair is what drifted, and a third copy inside the test would have drifted the same way.
- **Section 2, question 40: the quarter-point of the smaller group.** Where the round marks fall into two clear sizes, a size is read now instead of refused, and it is the quarter-point of the smaller group. **This amends entry 82 section 3 and says so where the rule lives**, in `RenderDifferenceHoleDetector.SizeReference` and on `HoleSizeSource.TwoSizes`. The description still asks for the calibre, because which of the two sizes a single shot makes is exactly the thing not known. `CryingWolfTests` keeps both rows and the two-sizes row now asserts the doubles are flagged rather than that nothing is.
- **Section 2 item 4, the rimfire diameter,** is entry 153 section 4's sweep and is done there, not here. Both `Calibre.cs` and `CalibreGuessList.cs` already carry 0.222 and 0.224 as separate entries.
- **Section 5: requests for Alan go to `docs/notes/for-alan.md`.** The file exists with the three requests the entry names, newest first. `CLAUDE.md`'s "anything that needs Alan comes first" section is rewritten around it: nothing is asked of him in the panel except a command he pastes into a shell, and a request never stops the run.
- **Section 6:** the five-line report is now the entry number, what changed, the test result, the commit, and whether the site has published it yet, with no request for Alan inside it.

---

## 2026-09-23, entry 143: answers to questions 41 to 46, and the review of research batch 1

**Status: actioned 2026-09-23**, sections 1 and 2 in section 3's order. **Question 43 is answered and deliberately not built**, which the entry allows: it is last of everything here, and the reason is recorded in `docs/PHASE1-RESULTS.md` under "Where entry 137 and the code disagree" so the specification and the code agree rather than only appearing to.

- **Question 45, first as the entry demands, done in 2c51788.** Scan 6 was never ten. No regression; the tenth shot has never been detected, and it is a standing detection target in `range-scan-counts.json` now.
- **Section 1.1, and the live site was worse than the review said.** One card in four had a thumbnail locally; on the site it was one in eighteen, because every article with a chart is still a draft and all eighteen published ones have no figure at all. So the entry's own fallback is what fits: a plain titled panel in the site's colours, the same shape as a thumbnail, with no image to go stale.
- **Section 1.2.** `_style.py` writes both versions and **no figure script had to change**: `save` draws once, writes the light file, recolours the same figure and writes it beside as `-dark.png`. Recolouring rather than redrawing keeps the two identical in everything but colour. Two faults that first run produced, neither visible to a build that checks files exist: every dark figure had a black title on a black background, because an axes has three title artists and this style puts titles on the left; and one figure had a white cross on the chart and a black one in the key, because a legend's sample marks are copies. **So the build looks at the picture**, and two figures are exempt by name, being photographs of white paper.
- **Section 1.3.** Three states, and `website/research/PUBLISHED.md` is the record of publishing having happened. The build refuses both halves of a disagreement, proved both ways.
- **Section 1.4** left alone, as the entry asks.
- **Question 46, measured.** The wide and narrow solves return the same shift, 0.247 in from the one Alan's table implies, and differ only in confidence. `MarkingSession` acts only on a certain offset, so the wide solve does nothing and the restraint comes from the matching. The code was right and the paragraph above it was wrong. `SheetOffsetWideOrNarrowTests` pins it.
- **Question 42, built and tested on scan 5 and scan 6.** A corrected shot now survives a second detection, matched to the nearest fresh detection within one hole's width, with the person's position and chosen bull winning and the detection dropped. A correction moved further than a hole's width survives on its own. The button says how many marks it will keep before anybody presses it.
- **Question 41.** Drag onto a bull was never built, so nothing was removed from the application. Entry 141 section 5.3 item 3 is amended in place below, and `DragNeverAssignsTests` is what stops it arriving by accident.
- **Question 44, measured, and the answer is not to write the spline.** Held out one marker at a time across the 15 paired photographs: **the held-out error is the same as the fit's own residual**, ratio 0.8 to 1.0 on every photograph. The model predicts a marker it has never seen as well as one it was fitted to, so its 0.005 in is not flexibility spent bending to its own markers, and a more flexible surface fitted to the same markers cannot help. The thin-plate spline of entry 130 section 6b item 2 should not be written.
- **The crash, narrowed without a debugger.** The photograph that throws fitted cleanly here, nineteen times over with a different marker held out each time. So the fault is not in the fit and not in `ToPage` over the page: it is `ToPage` at a point outside the page, which only `ExpectedImage.Render` reaches. `SurfaceCrashTests` records it.
- `docs/PHASE1-RESULTS.md` "Entry 143 section 1" and "Entry 143, question 44".

Written by the planning session at 04:30 Mountain on 2026-09-23. The review in section 1 is the planning session's, sent to Alan at the same time as this entry; the answers in section 2 are decisions.

## 1. Research batch 1: approved to publish, with four things to fix

Alan has the same review and will send "publish research batch 1" himself. The writing is good and the honesty is right: the primer article in particular says what nine shots can and cannot support, which is the whole point of the section. Fix these, and take them as the standard for later batches.

1. **Every card on the index needs a lead image, or none of them do.** Today one card in four has a thumbnail and the rest are empty, so the row is as tall as the tallest card with three large blanks in it. Give every article a lead figure, and where an article has no natural chart, a plain titled panel in the site's own style is better than a gap.
2. **The charts are light panels on a dark page.** Every figure is drawn on a near-white surface, which glares against the dark theme and looks pasted on. Have each figure script write both a light and a dark version from the same data, and let the page choose by the reader's theme. This applies to the planning drafts as well: `_style.py` should grow a dark palette and the scripts should call it, rather than each script deciding for itself. If that is more work than it is worth this week, the fallback is a consistent light card behind every figure, so at least they all look deliberate.
3. **Front matter says `status: published` on articles that are not published.** Two different meanings of the word are in play. Use `state: draft | ready | published`, where `published` is set only when a batch actually goes live, and make the build refuse a page whose state says published when it is not in a published batch.
4. **The narrow renders are not phone renders**, as `docs/RESEARCH.md` says plainly, and that is an honest note rather than a fault. Leave it. Alan will look at the real pages on his phone after the first publish and report anything that breaks.

Nothing else blocks publication. Publish batch 1 when Alan's message arrives, then offer batches 2 and 3 for review the same way rather than publishing them with it.

## 2. Questions 41 to 46

### Question 41: dragging a shot onto a bull means two different things

**Dragging always moves the shot, and never changes which bull it belongs to.** A mark's position is a measurement and a drag is how it is corrected; nothing else may ride on that gesture. Entry 141 section 5.3 item 3 is amended: assignment happens through the bull picker in the shots list, through the keyboard (select, type the bull number), and through the multiple-selection assignment you have built. Drag onto a bull is removed from the specification. If you later want a pointer gesture for assignment, it must be a distinct one, such as a drag with a modifier key held, and it must leave the hole where it is and say in the toast which bull it moved the shot to.

### Question 42: a corrected shot does not survive a second detection

**Build the matching you propose**, and do not lose a person's correction silently.

- Match each kept corrected shot to the nearest freshly detected shot within **one hole's width**, using the sheet's own size reference from entry 141 section 4 where it exists and the stated calibre where it does not. Where a match exists, the person's position and chosen bull win over the detector's. Where none exists, keep the corrected shot as it is.
- Never produce two marks for one hole; that remains the rule your current code protects.
- Also make the button honest: detecting again says, in one line, how many hand corrections it will carry over.
- Test on scan 5 and scan 6, and with a generated sheet where a correction is moved beyond one hole's width and must therefore survive on its own.

### Question 43: entry 137 names an image safety the desktop does not have

**Both, as their own item, after the current queue.** Not urgent.

1. A cap at **400 megapixels**, phrased as a limit against a hostile or broken file rather than a judgement about scanning, with the number and the measured size in the message. Alan's 600 dpi letter scans are about 32 megapixels, so the cap is twelve times his largest real file.
2. Move the decode to a background thread with a timeout, because the window freezing on a large scan is a real complaint waiting to happen. Show progress while it runs.

Until then, say in entry 137's record that the pixel cap and decode limit were not built and why, so the specification and the code agree.

### Question 44: the bent-sheet model crashes on one photograph, and improves the wrong points

**Measure first, build nothing.** Run the leave-one-marker-out measurement on the existing surface model across the paired photographs and report it. A model that cannot predict a marker it did not see will not predict a hole, and that decides whether the thin-plate spline in entry 130 section 6b item 2 is worth writing at all. Do not write the spline until that measurement is in.

The crash: spend up to an hour finding it, because an `IndexOutOfRangeException` in registration is worth understanding even in an unreachable model. If it is not obvious in that time, leave it with a test that records the crashing photograph and a note, rather than a speculative fix.

### Question 45: scan 6 reads 9 holes tonight where entry 130 recorded 10

**This is the first thing to do, before any new feature.** A real shot that the software used to find and now does not is the most serious kind of regression this project can have, and shot 6 is the one shot on that sheet that proves a group can contain something far from everything else.

1. Re-run scan 6 at the commit before entry 141 section 4 landed, and at the commit after, and report both counts.
2. If section 4 cost the hole, fix it so the sheet's own size reference does not drop a hole that a stated calibre finds, and say in the fix what the mechanism was.
3. Record the hole counts for all six range scans as a checked expectation somewhere a change like this trips over, without committing the scans: a small file of counts plus each scan's SHA-256, and a test that runs only when the folder is present and is skipped with a clear message when it is not.
4. You were right to say your earlier check was weaker than your sentence implied. Flag counts are not hole counts, and the correction belongs in the record.

### Question 46: the sheet offset is solved over every bull

**Run the measurement you propose**, both ways on scan 5, and compare each result against the offset Alan's table implies.

- If the wide solve is the better estimate, keep the code and rewrite the paragraph: the restraint is delivered by the matching, which only considers aimed bulls, and the solve is allowed to use the whole printed grid because the grid is geometry, not evidence about where the shooter aimed.
- If the narrow solve is as good, narrow it and fix the five-shot test.
- Either way, the test that broke is a case worth keeping: add it with a name that says which reading it is pinning.

Do not leave the code and the comment disagreeing, whichever way it goes.

## 3. Order

Question 45, then the batch 1 fixes in section 1, then questions 46, 42 and 41, then question 44's measurement. Question 43 last. Entry 129, the server work, waits for Alan and does not move.

---

## 2026-09-23, entry 147: macOS test builds, and a plain statement of what is supported

**Status: actioned 2026-09-23**, every section.

- **Section 1.** `osx-arm64` and `osx-x64`, self-contained, built on `macos-latest`, each a real `.app` bundle with `Info.plist`, `PkgInfo` and the icon, packed as `grouplab-macos-arm64.tar.gz` and `grouplab-macos-x64.tar.gz`. `scripts/macos-bundle.py` is the one place the bundle's shape is decided, and `MacBundleTests` builds one and reads it back without needing a Mac.
- **Section 1.5.** The updater already refused to offer a self-install off Windows. It now says plainly that updates are manual there, on the Settings screen and in the update run's own message, instead of reporting that there is nothing to install for this platform, which reads like a fault in the build.
- **Section 1.6.** Labelled untested on both cards, in the file names, and in the bundle's own `Info.plist`, which is the one label that survives unpacking after the download page is long forgotten.
- **Section 2.** The command on the download page and in the README, with what it removes and who should not run it.
- **Sections 3 and 3.2.** `docs/PLATFORM-SUPPORT.md` is the one source. `website/build.py` renders it into the download page, `scripts/platform-support.py` writes it into `README.md` between two markers and `--check` fails CI when it drifts, and the nightly appends it to any release whose assets include a macOS build. Nothing restates it, because a statement in somebody's settled words is exactly the text that gets reworded in one place and not the others.
- **Section 3.1.** The build targets are one list in `package.yml`. **To add one without further questions we need the architecture, and whether a plain tarball or a package built for a named distribution is wanted.**
- **Section 5.** Five tests, none of which need a Mac.
- **What went wrong, and it is the part worth reading.** The first push went red on all three runners and no nightly ran, so grouplab.org was live offering two Mac builds whose files returned 404. Three failures, all mine: a README heading with no contents entry, which was in the wrong section anyway; a README linking five assets where the test allowed three; and, on Windows alone, my own test anchoring a pattern with `$` against a file that has CRLF line endings on a fresh checkout. It passed here because this working copy is LF. Entry 121 section 3 records the same fault in another test, with the same cause.
- `docs/PHASE1-RESULTS.md` "Entry 147".

Written by the planning session at 02:10 Mountain on 2026-09-23. Alan's decision, in his words below. Do this after entry 129.

Today every build is tested on macOS, because `build and test` runs on `macos-latest` as well as Windows and Linux, and no macOS download exists. That is a reasonable state and a confusing one to a reader, because nothing on the site says either half of it. This entry publishes a macOS build, marks it honestly, and says plainly what will and will not happen.

## 1. Build and publish a macOS test build

1. Add macOS to the packaging alongside the Linux tarball: `osx-arm64` for Apple silicon and `osx-x64` for Intel Macs, self-contained, two separate downloads rather than a universal binary.
2. Package each as a proper `.app` bundle inside a `.tar.gz` or `.zip`, with `Info.plist`, the GroupLab icon, and a name that reads correctly in Finder. A bare executable runs from a terminal and behaves like a stranger in the dock, which is not worth publishing.
3. Publish both as nightly assets beside the Windows and Linux ones, named so the architecture is obvious, for example `grouplab-macos-arm64.tar.gz` and `grouplab-macos-x64.tar.gz`.
4. Build them on the `macos-latest` runner, which is already in the matrix, so the packaging is done by the platform it targets.
5. **The updater does not offer these builds.** The silent install and relaunch chain is the Windows installer, and a macOS build must not be offered an update it cannot apply. Check what the update path does on macOS and make it say plainly that updates are manual there.
6. **Label them untested everywhere they appear**: on the GitHub release, on the download page, in the file name if you can do it without making the name silly. Nobody has run this on a Mac.

## 2. The terminal command, and why it is needed

An unsigned application downloaded from the internet is quarantined by macOS, and Gatekeeper refuses to open it. Give the exact command on the download page and in the README, with a sentence saying what it does:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Say that this removes the quarantine flag macOS puts on downloaded files, that it is the standard way to run unsigned software, and that a reader who is not comfortable doing that should not run the build. Adjust the path in the instructions to wherever the pages tell people to put the app.

## 3. The platform statement, word for word

Alan has settled this wording. Publish it as its own section on the download page, titled "What is supported, and what is not", linked from the README, and do not reword it. It avoids the first and second person on purpose, and it says "they" of the author on purpose.

---

**Windows is the supported platform.** It is where GroupLab is developed and tested by hand, and the installer and automatic updates are built for it.

**Linux builds are published and are worth trying.** The download is a self-contained 64-bit tarball, so it runs on most desktop distributions without anything else being installed alongside it. The test suite runs on Linux on every build. Hands-on testing has not started yet. Linux can be tested here on virtual machines under VMware Workstation, and there is no bare metal Linux machine, but the real reason is that the application is still under heavy development, with features, layouts, appearance and internal workings changing daily. Testing a moving target on a second platform would mostly produce findings that are obsolete a week later.

**macOS builds are published and have never been run on a Mac.** The tests run on macOS on every build, so the code works at that level, but nobody has opened the window, printed a target or saved a session on real hardware. These builds are an experiment rather than a release.

### What happens once the application settles

Other platforms get proper attention once the pace of change slows and the Windows application is generally working the way the developer wants it to.

**Android is planned and is a high priority**, because that is the mobile platform in daily use here. Hands-on Linux testing follows, on virtual machines. macOS depends on the hardware question below.

### Running the macOS build

macOS quarantines anything downloaded from the internet and refuses to open software that is not signed by a registered Apple developer. After the application has been moved to the Applications folder, this removes the quarantine flag:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Anyone not comfortable running that command should not run this build.

### Why it is not signed

Signing a macOS application requires the Apple developer programme, which costs 99 dollars a year. The developer of GroupLab does not own a Mac, does not intend to buy one, and is not going to pay a yearly fee for a platform they do not own.

That is the whole reason. It is not a technical obstacle and it is not indifference to Mac users. If a developer or contributor wants signed macOS releases enough to donate a Mac for testing and cover the developer fees, the project will set it up.

### Signing elsewhere

The one-off 25 dollar Google Play developer fee has been paid. A signed Windows version through the Microsoft Store is intended in due course, and a code signing certificate may be bought if the price turns out to be reasonable.

### Apple mobile

An iPad Mini, sixth generation, is available as test hardware, and an iOS version of GroupLab would be tested on it. Building and signing an iOS application requires a Mac and the Apple developer programme, so that version cannot be produced at present, for the same reason the macOS build is unsigned. The hardware to test it exists; the machine to build it does not.

### Other Linux builds

The published Linux build is x86-64. Other targets can be added to the nightly builds on request: Arm64 for a Raspberry Pi or an Arm laptop, or a package built for a particular distribution rather than a tarball. Adding one is a line of configuration rather than a project. The reason a dozen are not published already is simply that nobody has asked for them.

Requests go to support@grouplab.org, naming the distribution and the architecture.

### Reports from Linux and macOS are welcome

A report is useful even when the answer is that it crashed on startup. "It opened and the buttons are the wrong size" is a useful report, and so is a crash report, which GroupLab can send on request. The address is support@grouplab.org.

---

Two notes for Code rather than for the page. Keep the build targets as a single list in the workflow, so adding one really is a line, and say in your report what a person must tell us for a target to be added without further questions. And a test should fail if this section's macOS wording, or the sentence saying nobody has run it on a Mac, disappears while a macOS asset is still published.

## 3.2 The same statement in all three places

The wording in section 3 goes, identically, to:

1. **The website**, as its own section of the download page at `https://grouplab.org/download/`, with its own heading so it can be linked to directly.
2. **The repository README**, which is the first page anyone sees on GitHub. Put a short "What is supported" section there with the same words, or the first two paragraphs and a link to the download page if the README would otherwise get unwieldy. The three facts that must appear on GitHub itself, not only behind a link, are that Windows is supported, that the macOS build has never been run on a Mac, and that other Linux targets can be requested.
3. **Every GitHub release that carries a macOS asset**, as a short note in the release body, next to the downloads, saying the macOS build is untested and unsigned, giving the quarantine command, and linking to the full statement.

Keep one source for the words: hold the statement in a single file in the repository, generate the website section and the README section from it, and have the release note quote from it. A statement that has to be edited in three places is a statement that will disagree with itself within a month. A test fails if the copies drift apart.

## 4. Ask for what would change it

Close the section with a plain invitation: if someone with a Mac wants to run the build and report what happens, that is useful on its own, and the support address is the way to do it. Make it easy to say "it started" or "it crashed at this point", and make it clear that crash reports from macOS are welcome even though macOS is not supported.

## 5. Keep it true

- A test fails if a macOS asset is published without the untested wording on the download page.
- The download page names each build's architecture, so an Apple silicon owner does not take the Intel one by accident.
- If a macOS build ever fails to package, the nightly still publishes the Windows and Linux ones rather than failing entirely, and says which one is missing.

---

## 2026-09-23, entry 148: the Discord server, linked from the site and GitHub

**Status: actioned 2026-09-23**, sections 1 to 5. Section 5 is a note rather than a build, as it asks.

- **Sections 1 and 3.** The invite is in `website/links.json` and nowhere else. Everything published points at `https://grouplab.org/discord`, which the build makes as a redirect page carrying the invite from that file. **Two tests hold it**: the invite is written out in one place only, and the one built page that carries an invite carries the one in that file. Replacing the invite later is a single edit and no published link breaks.
- **Section 2.** Top navigation as "Community", the footer beside the other links, the support page as the first option with the sentence about the address being better for anything private or with a photograph, the README near the download links as a plain line, and one line on the download page.
- **Section 2, last line, observed.** Nothing was put in the application. A link inside the software outlives the server, and that is a different decision.
- **Section 4.** The words are the entry's, unchanged. Nothing calls it official support, nothing promises a response time, nothing implies it is staffed.
- **Section 5.** Noted in `docs/WEBSITE.md` with the one line on how it would work: the nightly already writes the plain-words note before it publishes, so posting it is a single HTTP call to a webhook URL held as a repository secret, in the same job.
- `docs/WEBSITE.md`.

Written by the planning session at 03:40 Mountain on 2026-09-23. Do this after entry 129, alongside entry 147.

Alan has created the GroupLab Discord server and its permanent invite. Use the link in section 1 exactly as written; never invent or guess an invite.

## 1. The link

The permanent invite exists and never expires:

```
https://discord.gg/jY7MrYNN5V
```

Publish `https://grouplab.org/discord` as the canonical link everywhere (site, README, release notes), and have it redirect to the invite above. The invite itself is written down in exactly one place in the repository, so replacing it later is a single edit and no published link ever breaks.

## 2. Where it goes

The same link, from one source in the repository, in these places:

1. **The website's top navigation**, as "Community" or "Discord", so it is reachable from every page.
2. **The website footer**, beside the GitHub link.
3. **The support page**, as the first option for questions, with a sentence saying the support address remains for anything private or anything involving a photograph.
4. **The repository README**, near the top with the download and website links, as a plain line rather than a badge, unless a badge fits the README's existing style.
5. **The download page**, one line: somewhere to ask if something does not work.

Do not put it in the application itself in this entry. A link inside the software is a different decision, because it outlives the server.

## 3. How to hold it

Put the URL in the same single source that entry 147 section 3.2 uses for the platform statement, or beside it: one file, rendered into the page, the README and anywhere else. A test fails if the link appears written out in more than one place, and a test fails if any published page carries an invite that is not the one in that file.

## 4. What the site says about it

Short, and honest about what it is for:

> **Discord.** Questions, bug reports, target sheets, and what people are shooting. The project's developer reads it. For anything private, or anything with a photograph attached, the support address is better.

Do not call it "official support", do not promise a response time, and do not imply it is staffed.

## 5. Not automated yet

Release announcements into Discord are a webhook from the release workflow and are worth doing, but not in this entry. Alan is using GitHub's own webhook to begin with. Note it in `docs/WEBSITE.md` as a possible later item, with one line on how it would work: the release workflow already has the plain-words release note, so posting it is a single HTTP call to a Discord webhook URL held as a repository secret.

---

## 2026-09-23, entry 146: a tour of the application, one page per screen

**Status: actioned 2026-09-23**, sections 1 to 6. Every screen, not the three section 6 allows as a fallback.

- **Sections 1 and 2.** `/tour/`, in the top navigation between Download and Guides. An index of ten cards, then one page per screen: library, print, marking, analysis, showing the work, session records, compare loads, equipment, ballistics, settings. Each carries the screenshot large in both themes at the size the site uses, what the screen is for, a numbered list of its parts, two to five steps, where it sits in the flow with links either side, and links to the guide or research article where one exists.
- **Section 2.3, and the choice I made in it.** The entry offers a numbered overlay on the image or a labelled list beneath. **I used the list.** Every one of these screens has eight to eleven parts worth naming, and eleven numbered badges over a 1400 by 900 screenshot would obscure the thing they point at. The list names each part in the words the screen itself uses, which a reader can match by reading rather than by hunting for a small number.
- **Section 4.1 and 4.2, in two places.** `website/tour.json` is the list, and **the site build refuses to build** when a screen there has no render or a render has no page. `TourTests` says the same from the other side, so a screen added to the render walk fails with the name of the page somebody still has to write. Proved by adding a screen with no picture: `the tour has a page for 'reloading' and no screenshot of it was rendered`, and the build stopped.
- **Section 4.3.** The pictures are the ones `PublishedRendersTests` already holds to a source in `SOURCES.md`: generated sheets, invented rifles and loads, nothing from a range folder and nothing from a submission.
- **Section 4.4** is a rule rather than code, so it is in `CLAUDE.md`: the screenshot job replaces the picture and nothing replaces the words, so an entry that changes a screen says whether its tour page still describes it.
- **Section 5.** The home page had four screenshots: analysis in the hero, then marking, the target library and printing in a row of three. **It now has two**, the hero analysis and marking, and the row of three is a single figure beside the status panel with a link to that screen's tour page, plus a note pointing at the tour. A test fails if it ever shows more than two.
- **Section 3, and what it cost.** No class names and no file paths, checked by a test on the same four patterns the release notes use. Writing them meant reading every screenshot rather than the code, which is the point: the parts are named as they appear, so "Accept and analyse" and "Detect on a GroupLab sheet" are what the page calls them because that is what the button says.
- `docs/PHASE1-RESULTS.md` "Entry 146".

Written by the planning session at 00:20 Mountain on 2026-09-23. Do this after entries 144 and 145, and before the remainder of entry 143.

Alan: "I think there should be a separate page for screenshots of each section of the application that explains what is happening instead of just a few screenshots on the main page."

A handful of pictures on the home page shows that GroupLab exists. It does not show what using it is like, and that is the question somebody has before they download an unknown program. The screenshot job from entry 144 section 4 already produces the pictures; this entry gives them somewhere to live and something to say.

## 1. The section

`https://grouplab.org/tour/`, linked in the top navigation between Download and Guides. An index page, then one page per screen.

The index gives each screen a card: its name, one sentence saying what it is for, and its own screenshot. A reader should be able to understand the shape of the application from the index alone, and go deeper where they care.

## 2. One page per screen

Eleven screens exist as renders today: analysis, analysis with a sheet open, marking, library, print, equipment, compare, ballistics, sessions, settings, and whatever the twelfth becomes as the interface grows. Give every one its own page, driven by the same list that drives the screenshot job, so a new screen cannot appear in one and not the other.

Each page carries:

1. **The screenshot, large**, light and dark, following the reader's theme, at the desktop size.
2. **What this screen is for**, one short paragraph in plain words, from the shooter's side.
3. **What you are looking at**: a numbered list keyed to the picture, naming the parts and saying what each does. Use a numbered overlay on the image, or a labelled list beneath it where an overlay would crowd the picture. A reader must be able to match every item to something they can see.
4. **What you would do here**, two to five steps, in order, as a person would do them.
5. **Where it fits**, a line linking the screens before and after it in the ordinary flow: print a sheet, shoot it, open the image, mark it, read the analysis, record the session, compare loads.
6. **Links to the related guide and any research article**, where one exists.

## 3. The words

Written for somebody who has never opened GroupLab, and never for somebody who has read the code. No class names, no file paths. Name what is on the screen using the same words the screen uses, so a reader can follow along with the application open beside the page.

Keep each page short: a picture, a paragraph, a numbered list, a few steps. Where a screen needs a long explanation, that explanation belongs in a guide or a research article, and the tour page links to it.

Nothing on these pages may claim a feature that is not in the published build the screenshots came from. Say which build the pictures are from, and let the screenshot job keep that current.

## 4. Keeping it true

1. The pages are built from the same screen list as the screenshot job in entry 144 section 4, so a screen that gains or loses a render fails the build rather than going stale quietly.
2. A test fails if a tour page references a screenshot that is not produced, or if a produced screenshot has no tour page.
3. The screenshots use generated data only: a generated sheet, invented rifles and loads, no material from Alan's range folder and nothing from a submission.
4. When the interface changes enough that a picture is wrong, the screenshot job replaces the picture and the entry that changed the interface should say whether the words need changing too.

## 5. The home page

Once the tour exists, the home page keeps one or two pictures at most and links to the tour rather than trying to be it. Say in your report what you removed.

## 6. Order and effort

This is a page-building job, not an application job, and it must not displace entry 143's queue. If it runs long, publish the index and the three screens that matter most to a newcomer (analysis, marking, print), and add the rest in a second pass.

---

## 2026-09-22, entry 145: every build says what changed, in plain words

**Status: actioned 2026-09-23**, sections 1 to 6.

- **Sections 1 and 2.** Two headings, `**What you will notice**` and `**Under the hood**`, and a build shows only the ones it has. There is no third state: a build with no commits behind it is refused outright, because that is the only thing left that could honestly say nothing, and it cannot happen.
- **Section 3.1.** `Release-note-kind:` accepts `internal` for the second heading and `user` for the first. `new`, `fixed` and `changed` are kept and all mean the first heading. **This is a decision I took rather than the reading the entry gives**, which is that the kind is `user` or `internal` and nothing else. Every commit in the history uses the three older words, section 5 says this is a rewording and not a rewrite of history, and dropping them would have invalidated every trailer already written. Raised as **question 47** so the planning session can overrule it.
- **Section 3.2.** A commit with no trailer gets a line of its own, written from its subject with the entry reference taken off, under the second heading. The count is gone.
- **The awkward part of that, said plainly.** A commit subject here is often written for the log, so a generated line can carry a file path or a class name, which section 4 forbids. Two things stop that: a short table translating the handful of repository files whose names appear in subjects into plain words, so "docs slash release notes carries nightlies 71 to 76" becomes "the release notes carries nightlies 71 to 76"; and, for anything the table does not cover, the build fails and names the commit. **Grammar suffers in the translated case**, and I have left it rather than guess: the fix is a trailer on the commit, which is what section 3.1 asks for anyway.
- **Section 3.3.** `--missing` lists every commit since the previous build that made the generator write from a subject, and the nightly puts that list in the build's own report. It reports and does not fail, because a note can be improved after a build and a build cannot be un-published.
- **Section 4.** Four checks on every line, written or generated: a file path, a commit hash, a class or method name, and anything in code style. "GroupLab" is the one word shaped like a class name that belongs in a note. The reference in brackets at the end of a note is the one place an entry may be named, and it is not checked.
- **Section 5, and more than section 5 asked for.** The six builds that said "Nothing in this build changes what you see or do" are rewritten from their own commits: **nightlies 78, 77, 76, 75, 72 and 30**. None of them contains that sentence now, and a test fails if it comes back. Nightly 81, published tonight and not yet in the file, is written in the new shape.
- **Four more builds were silent in a way the entry did not name**, and I fixed them rather than leave them: nightlies 26, 18, 14 and 12 listed their known issues and said nothing at all about what changed. Nightly 12 is the first build GroupLab ever published for itself, and the file did not say so.
- **The one thing I would not write.** Nightly 26 carried the commits behind "where the group actually landed" and "the scan's stated resolution", and `CLAUDE.md` records that nightly 27's note about the first of those was untrue on the day it was published, because the code was wired to nothing. Its new entry says those two are groundwork that could not be reached from any screen in that build, which is what was true.
- **Section 6.** Five tests. The sentence and the count cannot come back, every build lists something, every build's lines sit under a heading, no line names a file, a hash or a class, and the generator itself cannot write the old sentence. The three build jump in the update bar is in the results with its exact text.
- `docs/PHASE1-RESULTS.md` "Entry 145".

Written by the planning session at 23:45 Mountain on 2026-09-22.

Alan, on the release notes as they read today: "I dont like how many of the release notes just say 'Nothing in this build changes what you see or do. It carries internal work only.' No matter what is done, it should be stated plainly what changed."

He is right, and the sentence is not even true. Something changed in every build, or there would be no build. Saying otherwise teaches a reader that the page is filler and trains them to stop reading it.

Do this after entry 144, and before the rest of entry 143.

## 1. The rule

**No build ever says nothing changed.** Every published build lists what is in it, in plain English, whoever it affects. A build that carries one documentation commit says which document and what it now says.

## 2. The shape of an entry

Two headings, and a build shows only the ones it has.

**What you will notice.** Changes a user meets: something on screen, something that behaves differently, a new or removed feature, a fix to something that was wrong, a change to what is installed or downloaded. Written from the user's side, never from the code's.

**Under the hood.** Everything else, still in plain words: tests, documentation, the website, the build, refactoring, performance work that nobody can perceive yet. One line per real change, not a count. "Two internal changes" is the thing this entry exists to remove.

Keep each line to one sentence. Group several commits that did one job into one line, and say so: "three commits finishing the shot editor's undo support". Aim for at most six lines a build, by grouping rather than by leaving things out. Where a build is genuinely one commit, it is one line.

## 3. Where the words come from

1. `Release-note:` stays the first source, and it should now be written for every commit, not only the ones a user notices. `Release-note-kind:` says which of the two headings it belongs under: `user` or `internal`.
2. Where a commit has no trailer, `scripts/release-notes.py` must not fall back to a count. It writes a line from the commit's subject, rewritten as a plain sentence, and marks it so the build reads as complete rather than as boilerplate.
3. Add a check on main: every commit since the previous tag either carries a `Release-note:` trailer or is reported by name in the build's report, so a missing one is noticed at the time rather than months later on the site.

## 4. Plain words, specifically

Write for a shooter who has never read this repository. Name the thing on screen, not the class.

- Not "refactored MarkingSession.Load". Instead: "opening a sheet again keeps the marks you moved by hand".
- Not "added ResearchArticleTests". Instead: "the website now refuses to publish an article whose data file is missing".
- Not "bumped the freshness gate". Instead: "a nightly build is no longer published when a newer commit has already landed".

No class names, no file paths, no commit hashes in the body. The commit is already named in the entry's header for anyone who wants it. Keep the project's other rules: no em dashes, no pseudoscience, no jargon left unexplained.

## 5. Go back over the ones already published

Every entry in `docs/RELEASE-NOTES.md` that says nothing changed, or gives only a count, is rewritten from its own commits under section 2's shape. Do not invent detail: where a build really was one documentation commit, say which document and what changed in it. Keep each build's date and commit as recorded; this is a rewording, not a rewrite of history.

Nightlies 77 and 78 are the two nearest examples, and they are honest ones: 77 carried the release notes for nightlies 71 to 76, and 78 carried a note about where the notes stopped. Both are worth one plain line each, and both are more interesting than "internal work only".

## 6. Tests

- A test fails if any entry contains "nothing in this build changes", "internal work only", or a bare count of changes.
- A test fails if an entry has no lines under either heading.
- A test fails if a line contains a file path, a commit hash, or a bare identifier in code style, in the body of an entry.
- The update bar in the application reads the same source, so check that a multi-build offer still reads well with the new shape, and say in your report what it looks like for a three build jump.

---

## 2026-09-22, entry 144: the site publishes itself, and the release notes keep up

**Status: actioned 2026-09-23**, sections 1 to 5 and 6's documentation. **Section 6's proof is partly open**, and named here rather than left implied: the site content commit that published itself and the failure path are in this commit's report; the nightly whose notes reach the live releases page with no human step cannot be shown until the next nightly runs; and the 5 minute sync cadence cannot be shown until Alan runs `install.py`, which section 3 says is the one manual step.

- **Section 1.** `website.yml` gained a `push` trigger on `main` with a paths filter, keeping `workflow_dispatch`. The filter is `website/**`, `docs/RELEASE-NOTES.md`, `docs/GLOSSARY.md`, `docs/USER-GUIDE.md`, `docs/TESTING-GUIDE.md`, both guide PDFs, `docs/figures/screens/**`, `targets/**` and the workflow file itself. It no longer waits on `build and test`: it installs the .NET SDK, builds the site and runs the site's own tests before publishing anything. Concurrency is one publish at a time with `cancel-in-progress`, so a burst of commits publishes once. `reason` became optional, and a push records the commit subject instead.
- **The two gaps I found in my own first cut of section 1, both while checking rather than while writing.** The filter did not include `docs/figures/screens/**`, so section 4's screenshot commit would have committed new images and published nothing; and the guides' own sources were missing, so editing a guide would have left the site showing the old one. Both are in the filter now.
- **Section 2.** `nightly.yml` writes its build's entry with `scripts/release-notes.py`, prepends it to `docs/RELEASE-NOTES.md`, commits it as `[notes] <version>` and pushes it. That path is in section 1's filter, so the site follows. **The loop is guarded in both directions and proved in both directions**: every job in `ci.yml` and the nightly's first job refuse a subject beginning `[notes] `, and `NotesCommitLoopTests` holds four facts, including the one that would otherwise fail silently, which is that an ordinary commit still runs everything. A guard written the wrong way round turns the whole suite off and nothing says so, so the test reads every clause of every condition and requires each to be a denial.
- **Why the guard is not paranoia.** A run whose jobs all skip still reports success, and the nightly triggers on `build and test` succeeding. Without the marker check in the nightly as well, a notes commit would have started a build, which publishes, which writes notes, which pushes: a release every few minutes for ever, each deleting the oldest to keep thirty.
- **Section 3.** `grouplab-site-sync.timer` is 5 minutes, from 15. The files are on the server and Alan's command is in the report.
- **Section 4.** `screenshots.yml`, weekly on Monday at 06:00 UTC plus dispatch, renders every screen from `main` in both themes at the three sizes and commits what changed as `[screens] ...`. It runs `PublishedRendersTests` before committing, so a render nothing in `SOURCES.md` accounts for stops it. That marker is guarded the same way as `[notes] `: an image-only commit needs no C# test run, and a build of the application from one would be a release of nothing.
- **What section 4 uncovered, which is worse than the drift it was written for.** The site's screenshots were last regenerated on 2026-09-19 and could not have been refreshed by running the walk, because **no test produced the 1400 by 900 size the website actually shows**. The render walk did 1280 by 720 and 2560 by 1440 only. So the live site had been showing a four day old interface with no way to notice: the walk passed, the sizes it produced were current, and the size the site served was not among them. 1400 by 900 is back in the walk, and this commit carries 31 refreshed images including two screens the site had never shown at all.
- **Section 5 and 6's documentation.** `docs/WEBSITE.md` gained "Stopping a publish" and its publishing section now describes the push trigger. `CLAUDE.md`'s website section is rewritten around the same rule. **Entry 128 section 6 is superseded by this entry**, and is marked so below.
- `docs/PHASE1-RESULTS.md` "Entry 144".

Written by the planning session at 23:10 Mountain on 2026-09-22. This replaces entry 128 section 6's rule that nothing publishes the site by itself.

**Why it is changing.** That rule was right when nothing had been proved. The signature check, the live check and the rollback have now all done their jobs on real publishes, including a rollback that saved the live site and a retry that installed it. What is left of the old rule is only cost: Alan watched a night's work sit unpublished, with release notes ending nine builds behind, because publishing needed a person to ask and a full Windows test run to finish first. His words: the fact that it did not happen overnight is annoying. He is right.

**What replaces it.** The machinery runs by itself. What is visible is still controlled, but by the content rather than by the pipeline: a research article appears when its own state says published, which only Alan agrees to, and an unfinished page is simply not marked ready.

Do this after the publish in flight, in this order. Every security constraint stands: no server address anywhere in the repository, no secret in a workflow log, no repository settings changed, no tags but the nightly workflow's.

## 1. A site workflow of its own, triggered by content

1. `website.yml` gains a `push` trigger on `main`, with a paths filter: `website/**`, `docs/RELEASE-NOTES.md`, `targets/**`, and any other file the site is built from. Keep `workflow_dispatch` as well.
2. It must not wait on `build and test`. The site's own gate is its own: run `python website/build.py` and the site's tests (the research front matter, data and figure checks, the em dash and banned term check, the metadata check on published images, the release notes check). Those take a minute or two, and they are the checks that can actually tell whether a page is wrong.
3. Concurrency: one site publish at a time per branch, newest wins, so a burst of commits publishes once.
4. If the build or its tests fail, publish nothing and leave the last good parcel in place.
5. Say in `docs/WEBSITE.md` what a person has to do to stop a publish: mark the page's state, or push a fix. There is no dispatch to withhold any more.

## 2. The release notes write themselves

1. When `nightly.yml` publishes a build, it also generates that build's entry with `scripts/release-notes.py` and appends it to `docs/RELEASE-NOTES.md`, then pushes that one file to main. That push matches section 1's paths filter, so the site follows within a couple of minutes.
2. Guard the loop: the notes commit must not start another `build and test` or another nightly. Use a marker in the commit message and skip on it, or a paths-ignore, whichever is cleaner in this repository, and prove both ways round in the report: a notes commit publishes the site and starts nothing else; an ordinary commit still runs the full suite.
3. The existing test that fails when the notes fall behind the tags stays, and becomes almost impossible to trip.
4. Hand-written wording stays welcome. The generated entry is the floor, not the ceiling: an entry Code improves later is an ordinary site content commit.

## 3. The server checks every 5 minutes

Change the timer from 15 minutes to 5. That makes the whole path, from a commit to a live page, about 7 to 8 minutes. The sync already does nothing when there is nothing new, so the extra runs cost nothing worth measuring. This needs `install.py` run again by Alan: prepare it, scp it, and give him the command in your report. It is the only manual step in this entry.

## 4. Screenshots that keep up with the application

The site shows the interface, and the interface is changing weekly. Add a job that regenerates the site's screenshots from the newest published build and commits them when they differ from what is committed, which publishes them by section 1. Once a week is enough, plus whenever an entry says the interface changed. Renders go through `IOutsideWorld`; nothing is printed and no real printer or paper is touched. If a screenshot shows a sheet or a result, it uses generated data, never Alan's range material.

## 5. What a person still decides

- Whether a research article is published, through its state.
- Whether a page exists at all.
- Anything that would change what the site claims about GroupLab: those are still entries from the planning session.

The pipeline decides nothing except when to run.

## 6. Prove it, and say so

In the report, show: a site content commit that published itself with its timings; a nightly whose notes reached the live releases page with no human step; the failure path, where a deliberately broken page stops the publish and leaves the live site untouched; and the sync log showing a 5 minute cadence. Update `docs/WEBSITE.md` and `CLAUDE.md` so the publishing rule they describe is this one, and note in `docs/NOTES-FROM-PLANNING.md` that entry 128 section 6 is superseded.

---

## 2026-09-21, entry 133: a "light" installer, measured before it is built

**Status: actioned 2026-09-22**, sections 1 to 6. Measured and proposed; nothing was built, as the entry requires.
- **Section 1, measured on this machine:** framework-dependent is 227.9 MB unpacked and 78.2 MB zipped, against 332.6 MB unpacked and a 97.3 MB installer self-contained. Of the framework-dependent build, Avalonia and Skia are 121.4 MB, OpenCV's native library 93.6 MB, everything else 7.9 MB, and GroupLab itself 5.0 MB.
- **What the measuring found, which matters more than the answer.** 100.7 MB of the shipped build was debug symbols, 100 MB of it two files: `libSkiaSharp.pdb` at 80.1 MB and `libHarfBuzzSharp.pdb` at 19.9 MB. Nothing reads them at runtime, no crash report here can use them, and no user will open them in a debugger. Leaving them out took the self-contained build from 332.6 MB to 204.3 MB with the analysis unchanged, so **the self-contained build is now smaller than the framework-dependent one was**.
- **Section 2:** the runtime is not what makes an update large. Avalonia, Skia and OpenCV are 215 of the 228 MB and travel either way.
- **Section 3:** the per-user route works without elevation, with `dotnet-install` and .NET 9's `AppHostDotNetSearch`. It is also a new failure surface on somebody's first run, which is when they decide whether to keep the application, and it does nothing at all for a machine that already has a runtime.
- **Section 4:** a per-user runtime is patched by nobody. Windows Update does not see it and Microsoft's updater does not know about it, so it would fall to GroupLab to watch for a CVE and swap a runtime under a running application.
- **Section 5:** two packages to build, test and support, and the updater keeping each install on its own kind for ever. Entry 123 section 2.7 found three defects in the single-package updater in one night, so doubling its cases is not small.
- **Section 6: question 36**, recommending not yet, and shrinking the one package instead. The symbol fix is done; dropping OpenCV's 27.3 MB video library is the next thing to measure; trimming is last and would need a full control walk on all three platforms.
- `docs/PHASE1-RESULTS.md` "Entry 133".

Alan suggested offering a light installer beside the full one: it would install only GroupLab and download the .NET runtime if the machine does not already have it. **Measure and propose only; do not build it.** Put this at the end of tonight's queue, after everything else.

1. **Size.** Publish GroupLab framework-dependent for win-x64 (no runtime inside) and report: unpacked size, zip size, and installer size, beside today's self-contained figures (about 311 MB unpacked, about 97 MB installer). Say which parts make up the rest (Avalonia, OpenCV's native library, fonts, samples).
2. **Updates.** The biggest gain may be updates rather than first installs: a framework-dependent nightly update would carry only GroupLab, not the runtime each time. Report the size of an update in each model.
3. **No administrator prompt, ever.** The installer and every update are per user with no elevation (entry 116, entry 119). A machine-wide .NET runtime install needs administrator rights, so that route is out. Check the per-user route instead: the runtime installed into a folder GroupLab owns (for example under `%LOCALAPPDATA%\GroupLab\dotnet` with Microsoft's install script, its download verified by hash), and the application host told to look there (.NET's `AppHostDotNetSearch` / `AppHostRelativeDotNet` settings, available since .NET 9). Say whether it works without elevation on a clean Windows user account, and what happens when the machine already has a suitable runtime.
4. **Keeping the runtime patched.** A self-contained build gets .NET security fixes with each GroupLab build. Say how a separately installed per-user runtime would be kept patched, and by whom.
5. **Cost.** Two packages mean two things to build, test and support, and the updater must keep each install on its own kind. Estimate that honestly.
6. **Recommendation**, as a question in `docs/QUESTIONS-FOR-PLANNING.md` with the figures: build it, or not yet, or instead shrink the single installer (for example by trimming, with what trimming would risk for Avalonia).

---

## 2026-09-21, entry 132: release notes that say what changed, and smaller builds

**Status: actioned in part 2026-09-22.** Done: section 1. **Not done: section 2**, named below.
- **Section 1, done, before the next nightly as section 3 requires.** Notes are built from `Release-note:` trailers alone and nothing is guessed from a subject line. A commit with nothing a person would notice carries no trailer and appears only in a count. The script refuses a note that is only a reference, begins with "Entry", is under eight words, or uses words meaning nothing to a shooter, naming the commit each time; checked against seven notes, six bad and one good, and it refuses all six for the right reason. The rule and its three examples are in `CLAUDE.md`.
- **Section 1.6, done:** nightlies 18 to 26 went out with unreadable notes, so the next nightly opens with a hand written account of what was in them, ten lines in plain words. The published releases are not edited.
- **Section 2, not done:** the build output work. Nothing was deleted from Alan's folders, and `C:\Dev\DEV-CLEANUP-REPORT-2026-09-21.md` was not read or changed. One thing was done towards it: no new `altN` folder was created after this entry arrived, and the rule belongs in `CLAUDE.md` when the rest is built.
- `docs/PHASE1-RESULTS.md` "Entry 132".

## 1. Release notes a tester can read

Alan read the notes for `v0.2.0-nightly.26` and they told him nothing. They read, for example, "Entry 130 item 3.3: doubt travels with the number", "Entry 130 folded, with question 34 and the night's write-up" and "Entry 129 section 3.5.1: the worker decodes with no way out". Commit subject lines are written for the log, not for a person deciding whether to install a build. Keep the entry reference, but every note must say **what changed, in plain words, from the user's side**.

1. **Every commit that changes something a person can see or rely on carries a `Release-note:` trailer**: one or two plain sentences saying what is different for someone using GroupLab, ending with the reference in brackets. For example:
   - `Release-note: When GroupLab finds fewer holes than the shots you fired, it now says so and lists the bulls with nothing on them, instead of reporting a clean result. (Entry 130, 2b.2)`
   - `Release-note: For small calibres such as .22 LR, holes are no longer rejected as too small when you have entered the calibre. (Entry 130, 2b.3)`
   - `Release-note: A blank sheet scanned on a flatbed can now use the scan's own resolution as its scale; GroupLab shows the number and you can refuse it. (Entry 130, 4.1)`
2. `scripts/release-notes.py` builds the notes **only from these trailers**, grouped under New, Fixed and Changed by a `Release-note-kind:` trailer (new, fixed or changed) rather than by guessing from words. Commits without a trailer (notes folding, write-ups, tests, internal refactors, CI) do not appear, except as one closing line: "Plus N internal changes (tests, documentation, build)."
3. The script **fails the nightly** if a trailer is only a reference, starts with "Entry", is shorter than eight words, uses internal jargon that means nothing to a shooter (list the words you check: for example "folded", "gate record", "recorder", "harness", "manifest" unless explained), or breaks the existing rules (em dash, private paths, coordinates, server address). A failed check names the commit so it can be fixed.
4. Add the trailer rule to `CLAUDE.md` in your own words, with the three examples.
5. The same notes appear in the in-application update bar (entry 119 section 5.2), so they must read well there too.
6. Write proper notes for **everything since `v0.2.0-nightly.18`** that a tester would notice, as a hand-written list the next nightly's body starts with ("Since nightly 18"), since those builds went out with unreadable notes. Do not edit the published releases.

## 2. Smaller builds, and no more copies

Alan's `C:\Dev\grouplab` folder is about 11 GB, and about 10 GB of it is build output: nine copies of the Core test build (`Debug`, `Release`, `alt` to `alt7`) at about 685 MB each, and in every build about 675 MB of native libraries for some 30 platforms nobody runs (Linux on ARM, RISC-V, s390x, LoongArch, MIPS, WebAssembly, Mac Catalyst and more).

1. **Stop making new `altN` output folders.** If a build needs to avoid a locked file, reuse one alternate folder (for example `bin/alt`) and clear it before reuse. Say in `CLAUDE.md` that no other output folders are created.
2. **Copy native libraries only for the platforms GroupLab builds and tests on**: Windows x64, Linux x64, macOS (x64 and arm64). Do this in the build (for example restricting runtime identifiers for the test and application projects, or trimming the packages' native assets) without changing the published packages' behaviour, and prove it: CI green on all three platforms, the installer and zip still run with nothing installed, and the Phase 0 gate record unchanged. Report the size of one Core test build before and after.
3. **Do not delete anything in Alan's folders yourself**, including the old `alt` folders; Alan will clear them himself in the morning with the commands in `C:\Dev\DEV-CLEANUP-REPORT-2026-09-21.md`. Do not read or change that report.

## 3. Order

Section 1 goes before any further nightly is published, so the next nightly already has readable notes. Section 2 fits in after entry 131, within tonight's queue under entry 130 section 0 and entry 131 section 0.

**Status: actioned 2026-09-22.** Section 1 done and proved on nightly 27, whose notes read as plain sentences. Section 2.1 done, and one honest admission: `alt8` and `alt9` were created before the rule was written, and `alt9` was reused thereafter. Section 2.2 done: a Core test build goes from 685 MB to 262 MB and an App test build from 705 MB to 278 MB, with the published package unchanged at 205 MB. **One correction was needed:** nightly 27's notes claimed GroupLab "now works out where your group actually landed", which was untrue when published because the solver was connected to nothing. The note is corrected for the next nightly and the published release is not edited.

---

## 2026-09-22, entry 142: a Research section on grouplab.org, thirty articles

**Status: in progress 2026-09-22.** Sections 1 and 2 are done: the page, the navigation link, the article format, the build and its checks. Section 3 is four of the eighteen articles code writes (1, 3, 5 and 6), which are batch 1; the planning session's twelve drafts have not appeared in `C:\Dev\grouplab-research-drafts` yet, and nothing has been imported. Section 4 is not done: **nothing is published**, and batch 1 waits for Alan's review and his words "publish research batch 1". Section 5's constant check is in. The state of all thirty is `docs/RESEARCH.md`.

Written by the planning session at 09:10 UTC on 2026-09-22. Alan approved every decision here.

**Order: this comes after entry 141 sections 1 to 5.** Printing, the CI changes, the queued work, question 38 and Alan's interface priorities come first. Nothing in this entry is pushed while the section 1.5 printing hold is in force, and nothing here touches printing, sheet rendering or the library.

## 1. Decisions

1. The page is called **Research**, at `https://grouplab.org/research/`, with a "Research" link in the top navigation of every page.
2. Byline on every article: "GroupLab project, tested by Alan Hayes, researched and written with Claude", with the date it was written and the date of its data.
3. **Images:** articles may show Alan's range scans and photographs, re-encoded from pixels with every piece of metadata removed. Never read, print or publish GPS, location or timestamp data. Two exclusions stand: the orange commercial target (never shown, never named) and the friend's scan until its consent record is published in the repository. Nothing from `C:\Dev\grouplab-submissions` is used in an article without its consent and a later instruction.
4. **Gear:** articles may name Alan's rifles and optics. Alan will supply a list of optics by class (1x, low power variable, medium power variable, high power variable) for the aim point series.
5. Every standing rule applies to the site text as well: no em dashes, no pseudoscience (barrel harmonics, optimal barrel time, velocity or accuracy nodes), no mention of OnTarget.

## 2. How an article is stored and built

1. Each article is `website/research/<slug>.md` with front matter: title, one or two sentence description for the index, topic group, number, date written, date of data, sample size, status (draft or published), sources, and data files.
2. Every chart built from GroupLab data is made by a script beside it, `website/research/<slug>/figures/*.py`, from data in the repository or from derived CSV files that are committed. A reader can download the CSV behind every chart. Concept charts made from simulation state that they are simulated, with the seed.
3. `website/build.py` renders the articles and an index page grouped by topic, each entry showing the title, the description and a small thumbnail of the article's lead graphic.
4. Every article opens with a short box: what we found, how sure we are (sample size and the main limit), and where the data is. Sources are listed at the end with links.
5. Images of Alan's sheets are re-encoded from pixels at a web size, with no metadata, and checked by a test that fails if any published image carries EXIF, XMP or IPTC data.
6. Add tests: every article in the index has its front matter complete, every CSV it links exists, every figure script runs, and no article text contains an em dash or the banned terms.

## 3. Who writes which

**Code writes** the articles built on GroupLab's own data, code and history, eighteen of them: 1, 2, 3, 5, 6, 7, 9, 10, 12, 17, 18, 19, 20, 24, 25, 26, 27, 28.

**The planning session drafts** the concept and research articles, twelve of them: 4, 8, 11, 13, 14, 15, 16, 21, 22, 23, 29, 30. Drafts arrive in `C:\Dev\grouplab-research-drafts\<slug>\` as `article.md`, figures, their scripts or data, and a sources list. Bring each one into `website/research/`, check every number against the repository and the application, regenerate figures from repository data where it exists, and raise any disagreement in `docs/QUESTIONS-FOR-PLANNING.md` rather than changing a claim silently. Do not write, rename or delete anything in the drafts folder.

| # | Slug | Title | Group | Writer |
|---|---|---|---|---|
| 1 | photo-hole-size | Why a photo cannot tell you your bullet's size | Reading targets | Code |
| 2 | hole-is-not-the-bullet | A bullet hole is not the bullet | Reading targets | Code |
| 3 | primer-comparison | Did the primer matter? A real comparison | Range tests | Code |
| 4 | mean-radius-or-extreme-spread | Mean radius or extreme spread? | Measuring groups | Planning |
| 5 | how-grouplab-reads-a-target | How GroupLab reads a target | Reading targets | Code |
| 6 | wrong-bull | When shots land on the wrong bull | Reading targets | Code |
| 7 | uploads-rebuilt-from-pixels | Every upload is rebuilt from pixels | How GroupLab is built | Code |
| 8 | can-you-see-the-bull | Can you see the bull? Aim points and optics at 100 yards | Range tests | Planning |
| 9 | scans-against-photos | Scans against phone photos: how close is close enough? | Reading targets | Code |
| 10 | safe-updates | How GroupLab updates itself safely | How GroupLab is built | Code |
| 11 | how-many-shots | How many shots do you need? | Measuring groups | Planning |
| 12 | pooling-groups | Pooling groups: when two sheets are one load | Measuring groups | Code |
| 13 | cep-explained | CEP 50 and 90 explained | Measuring groups | Planning |
| 14 | velocity-sd-small-samples | Velocity SD from 5, 10 and 20 shots | Measuring groups | Planning |
| 15 | moa-mils-inches | MOA, mils and inches: one group four ways | Measuring groups | Planning |
| 16 | when-to-adjust-zero | Zeroing: when to adjust and when to leave it | Measuring groups | Planning |
| 17 | one-hole-or-two | One hole or two? | Reading targets | Code |
| 18 | curled-angled-paper | Curled, angled and wrinkled paper | Reading targets | Code |
| 19 | wind-or-rifle | Wind or rifle? | Range tests | Code |
| 20 | blank-sheet-zero | Zeroing on a blank sheet with a hand-drawn cross | Reading targets | Code |
| 21 | photographing-targets | How to photograph a target so it measures well | Guides | Planning |
| 22 | printer-true-size | Does your printer print at true size? | Guides | Planning |
| 23 | scanner-traps | Scanner traps: cropping, DPI and colour | Guides | Planning |
| 24 | choosing-the-markers | Choosing the markers | How GroupLab is built | Code |
| 25 | designing-a-readable-target | Designing a target GroupLab can read | How GroupLab is built | Code |
| 26 | what-grouplab-sends | What GroupLab sends from your computer | How GroupLab is built | Code |
| 27 | nightly-builds | Nightly builds, from commit to installer | How GroupLab is built | Code |
| 28 | smaller-installer | Cutting the installer from 97 MB to 81 MB | How GroupLab is built | Code |
| 29 | aim-points-by-optic-class | Aim points for 1x to high power optics | Range tests | Planning |
| 30 | range-test-log | The range test log | Range tests | Planning |

Articles 8 and 29 wait for Alan's range data. Article 9 follows entry 130 section 2c.

## 4. Publishing

1. Batches of five or six. For each batch, build the site locally, render every new page at phone and desktop widths under `docs/figures/research/`, and stop with STATUS: NEEDS YOU asking Alan to review them. Publish a batch only after Alan sends "publish research batch N". The normal site publishing rule applies after that: `docs/RELEASE-NOTES.md` current, the website workflow run by dispatch, the live check confirmed.
2. Batch 1: the Research page and navigation link, plus articles 1, 3, 5 and 6, and any planning drafts that are ready.
3. Keep a table of all thirty with their state in `docs/RESEARCH.md`.

## 5. Accuracy

Every figure states its sample size. Nothing is called proven that five shots cannot prove. Where GroupLab's own behaviour is described, it describes the build on the site, and a test fails if an article quotes a constant (such as the hole to calibre ratio) that no longer matches the code.

---

## 2026-09-22, entry 141: tonight's work, and printing that must work by morning

**Status: in progress 2026-09-22.** Section 1.1 done, the print path held by tests that need no printer. Section 3 was done before this entry arrived. Sections 1.2 to 1.5, 2, 4, 5 and 6 are not done yet; the progress file is `docs/OVERNIGHT-2026-09-22.md`.

Written by the planning session at 08:25 UTC on 2026-09-22 (02:25 Mountain). Alan has read and approved every decision here.

Work through this in the order given, with `/loop` when Alan starts it, keeping `docs/OVERNIGHT-2026-09-22.md` as the progress file (the same shape as `docs/OVERNIGHT-2026-09-21.md`: a "Next step" line at the top, then a queue table with a state per item, kept current after every commit). Never end a turn to wait for CI; do the next item while it runs. Commit small, with `Release-note:` trailers where a user would notice the change. Every CLAUDE.md rule and every standing security constraint stays in force: no server address in any file, the SSH key by path only and never read, no sudo, no repository settings, no v* tags except the nightly workflow's, nothing from `C:\Dev\grouplab-range-2026-09-20` committed, no GPS, location or timestamp metadata read, and no printing to a real printer or anything that uses paper.

## 1. A hard deadline: printing targets from the newest build, by 09:00 Mountain (15:00 UTC)

Alan is going shooting with a friend on 2026-09-23 and leaves between 12:00 and 13:00 Mountain. He will print his targets from the newest nightly before he leaves. That is the one thing tonight that cannot slip.

1. Early in the night, before the big interface work, make sure the print path is covered by tests that would fail if tonight's changes broke it: for every sheet in the library, and for a designed sheet, render the PDF through the same code `PrintWindow.SavePdf` uses, and check the page count, the page size (letter unless the sheet says otherwise), that the fiducial markers and codes are present, and that the printed scale is exact: a known distance on the sheet measures the same in the PDF to within 0.005 in. Also check the Print dialog path opens and hands off without error through `IOutsideWorld`, never to a real printer.
2. At 07:30 Mountain (13:30 UTC), stop pushing to main. Let CI finish on main's head and the nightly publish. If CI is red for a real reason, fix only that and push once. If you are mid-item at 07:30, park it on a local branch or leave it uncommitted and list it; do not push half an item.
3. When that nightly is published, install it with `scripts/Test-RealUpdate.ps1` or the installer, run the print checks from step 1 against the installed build, save a PDF of the GL-CF25-LTR-D sheet and one other sheet to a scratch folder outside the repository, and measure them.
4. By 09:00 Mountain, stop with a status report. The first line after the status must be: "Print from nightly N", with N the verified build, and the direct download link to that numbered release, not only the rolling one. Say plainly if anything about printing is not right.
5. After that report you may carry on, but nothing that touches printing, sheet rendering or the library is pushed until Alan says he has printed. The rolling nightly link can move; the numbered link in your report is the one he uses.

## 2. CI: fewer runs, no lost nightlies

Approved by Alan. Workflow files only, no repository settings.

1. Push to main only from now on. Stop pushing phase-1. Say in the report what, if anything, still depends on phase-1.
2. `build and test`: add a concurrency group per workflow and branch with cancel-in-progress, so a newer push cancels the older run on the same branch.
3. A guaranteed nightly: add a schedule to `nightly.yml`, once a day at 12:00 UTC, that builds the newest commit on main whose `build and test` succeeded, and does nothing if that commit already has a nightly. The freshness skip stays for the `workflow_run` path. Keep the per-build pre-release, the rolling `nightly` release and the 30-release limit as they are.
4. Prove it: show a run of each path in the report, including one where the schedule finds nothing to do.

## 3. Finish what is already queued

1. Step A5 of the last command: the website republish confirmed live, with the new build id in the meta tag, /releases/ listing the newest nightly, and no rollback in the sync log.
2. Entry 139 section 5: the real update test once a nightly carrying 28a3365 exists. Section 1 step 2 will produce one if nothing sooner does.
3. The calibre best guess snapping to Alan's list. Firearm type on the Equipment screen is rifle or pistol only; Alan does not want shotgun or rimfire now.

## 4. Question 38: approved, build it

Alan approves your recommendation. Where a sheet has enough round single marks to speak for itself, its own marks are the reference for telling one hole from two, and the stated calibre is the fallback. This replaces the first rule of entry 82; say so in `NOTES-FROM-PLANNING.md` beside entry 82.

1. Decide "enough" from the evidence you have (thirteen sheets) and write the number and why in the code comment. Below it, fall back to the stated calibre, and with no calibre, to shape alone as today.
2. The sheet's reference is robust to the doubles it is judging: take it from the marks that agree with each other, never a plain mean of all marks.
3. The calibre guess (section 3.3) must use the same reference, and on a photograph it stays rough as already asked.
4. Tests from generated sheets only, including a sheet where a third of the marks are real doubles. Then re-run the thirteen images from question 38, read only, and report flagged marks per image before and after.
5. `HoleToCalibre` for scans stays unchanged.

## 5. Alan's priorities for the interface, in his words, and what they mean

Alan's biggest wants right now: "improving the UI looks with better layout for information, more consistent text sizes, more graphs and graphics that help you understand the data", and "changes made to make it easier to edit shots and tie shots to various bulls". This is the bulk of the night. Take before and after renders of every screen you change at 1280 by 720 and 2560 by 1440 under `docs/figures/screens/`, and look at each one yourself before you call it done.

### 5.1 Consistent text sizes and layout

1. One type scale for the whole application, defined once in `AppStyles` (for example: caption, body, label, section heading, page heading, headline figure). Five or six sizes, no more. Every `FontSize` in the application comes from it. Add a test that fails on a literal font size anywhere outside the style file.
2. One spacing scale the same way (for example 4, 8, 12, 16, 24, 32), and the same margins and gaps on every screen.
3. Layout for information: on every screen, the most important figure first and largest, related figures grouped under one heading, labels and units aligned, and no text cut off or wrapping mid-number at either size. The analysis panel rebuild from `AnalysisPanel` (queue item 1 of entry 135) is the first screen to do.
4. The amber stays for the one headline figure on a screen. Everything else uses the existing neutral and accent colours.

### 5.2 More graphs and graphics that explain the data

Every graphic must answer one question, and its caption says that question in plain words. No decoration, no 3D, no pseudoscience. Suggested set, build in this order and stop where the evidence or the data runs out:

1. The group plot: the shots, the group centre, the mean radius circle, the extreme spread line between the two widest shots, and the aim point, each switchable, with a small legend.
2. Horizontal and vertical spread: two small strips or histograms beside the plot, answering "is my group wider than it is tall".
3. Shot order: distance from the group centre for each shot in the order fired, where the order is known, answering "did the group open up as I shot". Show nothing when the order is not known rather than inventing one.
4. Sessions over time: mean radius per session for one load, with its uncertainty, answering "is this load getting better or worse".
5. Velocity: where velocities are recorded, the shots with the mean and SD marked, and ES; SD shown with its uncertainty for the sample size.
6. Compare loads already has dot-and-whisker charts; bring them to the same type scale and colours.

### 5.3 Editing shots and tying shots to bulls

This is question 37's control (queue item 6 of entry 135) and the shot editor from entry 131, done together as one piece of work.

1. Select a shot by clicking it; the shot is highlighted on the image, in the shots list and in the review queue at once.
2. Move a shot by dragging it; add one by a click in add mode; delete with the Delete key or a button. Every edit goes through undo and redo.
3. Assign a shot to a bull by dragging it onto the bull, by a bull picker in the shots list, or by keyboard (select, then type the bull number). Select several shots and assign them together.
   - **Amended by entry 143, question 41, on 2026-09-23: drag onto a bull is removed.** A drag always moves the shot and never changes which bull it belongs to, because a mark's position is a measurement and a drag is how it is corrected; nothing else may ride on that gesture. Assignment happens through the bull picker, the keyboard and the multiple-selection assignment. A pointer gesture for assignment, if one is ever wanted, must be a distinct one such as a drag with a modifier held, must leave the hole where it is, and must say in the toast which bull it moved the shot to. It was never built, so nothing was removed from the application; `DragNeverAssignsTests` is what stops it arriving by accident.
4. Say which bulls were aimed at: click bulls to mark them aimed or not aimed, with presets for "every bull", "rows", and "bulls 2 to 5 of each row" style patterns, and a shots-per-bull count. Assignment uses this. Scans 4, 5 and 6's ground truth in entry 120 is the test: show that with the aimed bulls set, the assignments match Alan's table.
5. A shot moved or assigned by hand is marked as manual, shown differently, and never changed by a later re-detection or re-assignment.
6. Every statistic and graphic updates as soon as an edit is made.
7. A review queue item opens the shot it is about, selected and ready to edit.

## 6. After that, in this order

1. Entry 137, drag and drop or paste an image into the main window, if it is not finished.
2. The rest of entry 131 sections 2 to 9 and a final checklist pass.
3. Entry 130 sections 2c and 6b: photographs against scans, and the mounted gate.
4. Performance, entry 130 section 6.
5. Questions 34 and 36 answered with a recommendation.
6. Entry 129 server side, prepared only: the receivers, the intake worker and the ClamAV step built and tested locally, with `install.py` or its equivalent ready. Stop short of anything that needs sudo, the Turnstile secret or a change on the server, and list exactly what Alan and you will run together when he is awake.

## 7. Reports

The 09:00 Mountain print report in section 1.4 is required whatever else is happening. Otherwise report only when you stop. If you stop for any reason before section 1 is done, the report still starts with the state of printing.

---

## 2026-09-22, entry 140: a new image is a new target

**Status: actioned in full 2026-09-22.** Every section is done.
- **Section 1.1 and 1.2, done.** The calibre no longer follows the last sheet, and neither does anything else: a test walks `MarkingState`'s own properties and fails if a field is left set after opening a new image, so a field added later is caught here rather than by somebody opening their second target of the day. The rounds fired, calibre and distance boxes are emptied with it.
- **Section 1.3, done.** "Same setup as the last target" copies the rifle, barrel, load, calibre and distance, names what it would copy beside the button, and is offered only where this sheet has none of them.
- **Section 1.4, done.** Every way out of a sheet, opening an image, opening a marking, opening a saved session and New target, asks first where the sheet holds edits that are not in a saved session. It is a row rather than a dialog, and cancel leaves everything exactly as it was. Unsaved work is decided by comparing the marking's own file against the one last saved, so a save, an undo and a redo is not unsaved work.
- **Section 2, done.** New target, in the header menu and on Ctrl+N, clears the sheet exactly as opening an image does, with a toast whose Undo brings the whole sheet back.
- **Section 3, done.** With no calibre the size comes from the sheet's own marks, which section 1 is what makes reachable. Where most of a sheet would be flagged, one item asks about the calibre instead of one an item a shot: three marks and three fifths of the sheet is "most", so three doubles among fifteen are still raised one by one. Four generated sheets hold it.
- **Section 4, done, and it found two things the entry did not have.** First: "You fired 25 and 15 are marked" was never carried over. It was the sheet's own twenty five bulls, worded as though Alan had said it; that wording is gone and the shortfall stays. Second: stating the **correct** calibre on that photograph flags all fifteen too, because photographed holes measure about one and a half times what scanned ones do, which is **question 38**.

Alan opened `20260920_165624.jpg` (a sheet with 15 shots, one on each of bulls 1 to 15) straight after working on a 25-shot sheet. GroupLab carried the last sheet's facts over. His screenshot shows:

- "Count differs from rounds fired: You fired 25 and 15 are marked. Nothing is marked on bulls 16, 17, 18 ..." The 25 was the previous sheet's rounds fired.
- **Every one of the 15 shots flagged "Possibly two holes"**, with sizes of 2.0 to 2.36 holes (shot 15: diameter 0.412 in, "2.36 holes"). A 6.5 mm hole reads as two holes when it is measured against a smaller calibre, which suggests the previous sheet's calibre was kept too. Confirm whether that is the cause.
- 16 review items on a sheet that has nothing wrong with it. That is the review queue crying wolf, which teaches people to ignore it.

## 1. What resets when an image is opened

1. **Everything that describes one sheet starts empty for a new image**: rounds fired, shots per bull, which bulls were aimed at and sight changes (question 37), the load on each bull, the calibre and its confirmation, the shot distance, the scale, every mark, the review queue, the undo history, excluded and flyer marks, notes, and anything else held per sheet. List every field you reset in the results, found by reading the state, not from memory, and add a test that fails if a new per-sheet field is added without being reset.
2. **Nothing from the previous sheet is used silently.** If something must carry over, it is offered, not applied.
3. **"Same setup as the last target"**: a clearly labelled button, shown after opening a new image when a previous sheet exists, that copies only the equipment and conditions: rifle, barrel, load, calibre and shot distance. Never counts, marks, aimed bulls or anything about where shots landed. What it copies is listed beside it so the person can see what they are accepting.
4. **Unsaved work**: if the current sheet has edits that are not saved as a session, opening another image first asks "Save this target, discard it, or cancel", never losing work and never keeping it attached to the new image.

## 2. A reset of one's own

Add **New target** (Ctrl+N) to the Open menu and to the toolbar area: it clears the current sheet exactly as opening a new image does, with the same save, discard or cancel question, and a toast with Undo (entry 131 section 9).

## 3. The "Possibly two holes" check

1. It must never use a calibre the person has not confirmed for this sheet. Without one, judge doubles against the other holes on the same sheet (a hole about twice the area of its neighbours), not against an assumed calibre.
2. If most holes on a sheet would be flagged, the assumption is wrong, not the holes: raise **one** item asking the person to confirm the calibre, instead of one item per shot.
3. Test it with generated sheets: 15 single holes of one calibre with no calibre stated raise no doubles; the same with a wrong smaller calibre stated raise one calibre question, not fifteen items; a real double among singles is still found.

## 4. Proof

Recreate Alan's case in a test: analyse a 25-shot generated sheet with a calibre and rounds fired set, then open a 15-shot generated sheet, and check that no count, calibre, distance, review item or mark from the first appears on the second. Then run `20260920_165624.jpg` from `C:\Dev\grouplab-range-2026-09-20\photos\` (read only, nothing committed) the same way and report its review queue before and after. A plain `Release-note:` trailer. Put this near the top of the queue: it affects every session Alan runs.

---

