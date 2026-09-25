# Phase 1 results, entries 126 to 150

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 125. A nightly that called itself a development build

### 1. The cause, in one sentence

`AppInfo.Build` was a static field initialiser written above `AppInfo.Train`, and C# runs static initialisers in the order they appear, so it read the train while it was still null and every published build called itself a development build.

### 2. What was not the cause

The MSBuild chain was suspected and is innocent. Built locally with `-p:GroupLabTrain=nightly`, the application assembly reads:

```
train: nightly
version: 0.2.0-nightly.99+d2334e826b2ea4016f3f75c155f573db629eec88
```

So `nightly.yml` to `package.yml` to `package-windows.ps1` to `Directory.Build.props` was carrying the value correctly the whole time. The value was always stamped in, and always read too early. This matters beyond the fix: had the chain been "fixed" on suspicion, the defect would have survived and the workflow would have grown a change it never needed.

**What it cost.** `v0.2.0-nightly.12` is installed on at least two machines and will never offer an update, because a development build refuses every manifest before it asks anything. Those copies have to be replaced by hand once a stamped nightly exists. Nothing else was affected: the version, the commit and the signing were all correct, so the build is sound in every way except the one that matters for updating itself.

### 3. The fix, and the guard that stops it returning

`AppInfo.Build` is worked out on first use rather than in a field initialiser, so no declaration order can bring it back.

That fixes one place. The claim can only really be checked on the thing being shipped, so `grouplab build-stamp <assembly> [--expect <train>]` reads the train and the informational version straight out of a built assembly using `System.Reflection.Metadata`, which is in the shared framework and needs no package. It never loads or runs the assembly, so a Linux build is checked from a Windows runner and an architecture the runner cannot execute is checked all the same.

`package.yml` runs it on the published tree for the Windows package and for the Linux tarball, before either is packed. It is there rather than in `nightly.yml` for two reasons: `nightly.yml`'s publish job needs `package`, so a failure stops the publish anyway, and `release.yml` calls the same reusable workflow and had exactly the same exposure. A check in one caller would have been a check somebody had to remember to copy.

Four tests hold the checker and three hold the rule underneath it. A checker that passed everything would be worse than no checker, because it would be believed.

### 4. Every screen says its own words

`Go` now sets the status line for whatever screen is being arrived at, rather than the library alone as entry 120 section 10.3 left it. That is why the fault came back somewhere else: it was fixed one screen at a time, so the next screen inherited it.

`ScreenStatusTests` visits all five screens and fails if any shows the marking screen's words, if any leaves the bar empty, or if two screens say the same thing. **It caught a second case while being written:** coming back **to** the marking screen kept the settings page's line, because the marking screen's words belong to the tool in hand and nothing restored them. `ToolStatus` is now separate from `SetTool`, so arriving at the marking screen says what the tool in hand says.

### 5. The Updates rows, and two more faults behind them

Train and Check were two wrapping rows; Lengths, Angles and Distances were a grid with the label centred against its control. That is the whole of the misalignment. They are now the same grid, and measured at both sizes every label on the page sits within half a pixel of its control's centre:

```
1280x720  Lengths: label centre 183.5, control centre 183.0
1280x720  Angles:  label centre 223.5, control centre 223.0
1280x720  Distances: label centre 263.5, control centre 263.0
1280x720  Train:   label centre 609.5, control centre 609.0
1280x720  Check:   label centre 649.5, control centre 649.0
```

The gap below the Check row was an empty text block waiting for a check to run. It now carries what the last check found, remembered across launches in `settings.json`, and hides itself when there is nothing to say, so a fresh installation has no gap rather than an empty one.

**Two further faults were found while fixing that**, both worth more than the one that was asked about:

1. **The settings page was built before the saved update preferences were loaded.** So the Train and Check boxes always showed their defaults rather than what had been chosen, and the last check could never have appeared however well it was written. Preferences are now loaded before anything is built.
2. **Nothing saved the preferences at all.** Entry 119 left them in memory with a note to move them to the settings file later; an update that closes the application would have forgotten the train and the skipped version at the moment they matter most. They are saved now, by every control that changes them.

Fault 1 is the same shape as section 1's: a thing read before the thing it depends on was ready. Two of them in one screen in one day is worth naming as a pattern rather than two accidents.

### 6. The renders, looked at

`docs/figures/screens/current/settings-light-1280x720.png` and `settings-light-2560x1440.png`, rendered as `0.2.0-nightly.14, nightly build, commit 9db6500`. They also close entry 119 section 6.4, which had been outstanding since that entry.

What they confirm: the build line names a nightly as a nightly; Train and Check line up with the Units rows above them; there is no gap under the Check row; the status bar reads "Units, theme, updates and the log. Every choice here is remembered." rather than the marking screen's words.

**Three things they show that nobody asked about**, reported rather than changed:

1. **At 2560 by 1440 the page sits in a column about 545 pixels wide with the rest of the window empty.** It is readable and nothing is cut, but roughly three quarters of a large screen is blank. The column is deliberate, because settings prose should not run to 2000 pixels a line, but nothing else uses the room either.
2. **Theme has no label beside its box**, where Units and Updates both do. It is the only control on the page without one.
3. **At 1280 by 720 the privacy note is clipped mid-sentence** by the status bar. The page scrolls, so nothing is lost, but the cut lands inside a sentence rather than between items.

None of the three is a defect the entry raised, and none was changed without being asked.

## Entry 130. The overnight queue

Eight of the queue's items were finished, and the rest are named below with why. Three of them were defects that could mislead a shooter, and those are worth reading first.

### 1. A shortfall that said nothing at all

Alan fired fifteen at scan 1. GroupLab found fourteen, put nothing in the review queue, and presented the figures as a clean result. He had no reason to look. It was the same bull as the hole missed on his first sheet the day before.

**The cause was narrower than it looked.** The count check worked, and had worked all along; it only ran when somebody had **typed** a number. Nobody had typed one, because the sheet already said it. Analysing one shot to a bull across fifteen scoring bulls is a statement that fifteen were fired, and nothing was reading it.

The count now comes from the sheet's own arithmetic when nobody has typed one, and the shortfall names the bulls with nothing on them, because that is where a missing shot is. Nearest-bull still claims no count: that is the person saying they are not counting, and inventing one there would tell somebody they had lost a shot they never fired.

Eight tests, all from generated sheets.

### 2. Five real holes called too small

Scan 4 refused five .22 LR holes measuring 0.11 to 0.14 in, and naming the calibre did not help, because the gate was a fixed 0.15 in and nothing read the calibre.

**The mistake was assuming a hole is about as wide as the bullet.** It is not. Paper stretches ahead of a bullet and closes behind it, so the hole is reliably narrower than the bullet that made it. The smallest of those five was 0.49 of its calibre, which is not a marginal case but a normal one.

| | |
|---|---|
| floor with no calibre named | 0.150 in, unchanged |
| floor with a .224 in bullet named | 0.101 in |
| absolute floor, whatever the calibre | 0.060 in |

0.45 of the calibre rather than 0.49, because a gate set exactly at the worst case seen so far refuses the next one slightly worse. The absolute floor is there because a nonsense calibre must not open the gate to everything; at 600 dpi it is 36 pixels across, which no fibre or speck reaches. Seven tests.

### 3. Where the group actually landed

This is the fix for the worst defect this project has found. On scan 5 every shot was measured against a bull it was not aimed at, and **nothing looked wrong**: the group came out tight, it came out centred, and the zero correction said there was nothing to dial. All three were false and a shooter would have believed all three.

`ImpactOffsets` finds one translation per subgroup before any hole is assigned. It works the way a person would: guess that some hole belongs to some bull, shift everything by that much, see which bull each hole is nearest to now, re-centre, repeat until it stops moving. Every hole-to-bull pair is tried as a start, so a group that landed a whole bull away is found as easily as one that landed slightly low.

**Three things took a failing test each to get right, and each is a real trap:**

1. **The median, not the mean.** Scan 6 had one shot far from everything else. A mean lets that one shot pull the point of impact, and a point of impact pulled by one wild shot moves every other shot's measurement with it. One bad shot becomes a whole bad group. The median ignores it.
2. **Only the bulls the shooter says they aimed at.** Without that constraint a twenty five bull sheet where ten were shot has a translation for almost any answer, and the scan 4 row-split case came out uncertain when it is not.
3. **Certainty needs a gap in dmm as well as in proportion.** Two readings that both explain the holes perfectly both cost nothing, and nothing is eighty percent of nothing, so a ratio alone called a genuinely ambiguous sheet certain.

Seven tests, all generated arithmetic: scan 5 recreated, scan 4's sight change recreated, scan 6's flyer recreated, a sheet shot where it was aimed, an ambiguous sheet called uncertain, an answer that does not depend on where the search began, and nothing to place.

### 4. Doubt travels with the number

Entry 120's third point was that GroupLab already said an assignment was contested and already let a hole be moved, and then presented the group size, the composite and the zero correction as though none of that had happened. A person reads the figures. They do not read the review queue.

`AssignmentCertainties` says whether an analysis rests on something nobody has confirmed, from two sources: shots that could belong to more than one bull, and a point of impact that did not settle. The second matters on its own, because scan 5 had **no single contested shot** and the whole group was still in the wrong place.

**The zero correction refuses rather than qualifies.** Every other figure is something a person reads; the zero correction is something they act on, by turning a turret. A correction worked out from shots that may belong to other bulls is worse than none.

Seven tests. Wiring it into `GroupFigures` and the screen is the rest of the item and is not done.

### 5. The stated resolution, offered

A scan usually states its own resolution, and on a blank sheet that is a scale. It is offered with the number shown and never applied by itself, because a scale decides what every figure means: get it wrong and a one inch group reads as two with nothing on the screen looking unusual.

A photograph is never offered one, since its stated resolution describes the file rather than the paper. 72 and 96 are not offered, being what a file gets when whatever wrote it had nothing to say. A stretched scan says so instead, because one number cannot describe it and a group measured on one is wrong in a single axis, which is the hardest kind of wrong to notice.

### 6. What was not done, and why

| item | why |
|---|---|
| 1, the real update test | needs a nightly that publishes; tonight's 403 was diagnosed and fixed, and the freshness check was proved skipping cleanly on run 35591532352, but no nightly had published by the end of the night |
| 2, entry 129's receivers and page | the night ran out; the quarantine worker and its units were built earlier and are in |
| 2b.1, 2b.4, 2b.5, 2b.6 | each needs the real scans read and re-run, which was not reached |
| 2c, photographs against scans | same |
| 3.2, row breaks | the solver takes a subgroup and solves it; letting the shooter mark where a sight change happened is interface work that was not reached |
| 3.5, the three cases run against the real scans | explicitly to be done only after 2b, which is not finished |
| 4.2, pooling | **deliberately not built: question 34** |
| 6, performance | not reached |
| 7, the guides | not reached |

Nothing on that list was started and left half built.

### 7. Question 34, and why pooling was not begun

Pooling two sheets of one load is not plumbing. **A pooled group has no single centre**, and the three defensible choices measure different things: one centre for all forty shots includes the movement between sessions, each sheet centred on itself measures the ammunition alone, and reporting both names that movement as its own quantity.

`docs/STATISTICS.md` says a figure has to say what it is an estimate of. Pooled within-session radii read exactly like a twenty shot group's mean radius and are not an estimate of the same thing, and nothing on the screen would distinguish them. Choosing quietly would put a number in front of somebody that means something other than what they think it means, which is the failure entry 120 section 2 was about.

So nothing was built, because the first thing the code must do is pick a centre, and a pooled figure recorded before the choice would not be comparable with the ones after it. The recommendation is in question 34: report both, with the within-session figure as the headline.

## Entry 128. grouplab.org moves into the repository

### 1. The port, and what changed

`website/build.py` is the website chat's builder with every path relative to the repository root. It writes to `website/_site/`, which git ignores.

Entry 128 section 1.1 asked for both to be built and the HTML compared. **Seven of the eight pages are byte-identical** once the new fingerprints and the new build tag are set aside. The eighth is `guides/testing-guide/index.html`, and all three of its changes are mine:

| line | what changed |
|---|---|
| the download step | a link that had to go, below |
| the rollback paragraph | entry 126's support page sentence |
| the reporting paragraph | entry 126's two ways to send a report |

**The link is worth naming.** Entry 121 section 2.4 said nothing in the repository may point at `releases/latest`, and the test written for it checked the README alone. So `docs/TESTING-GUIDE.md` went on sending testers to a page that returns nothing, because no numbered release exists, from then until now. The new builder check caught it on its first run. `ReleaseAssetTests` now covers both guides too, which is where the rule should have been from the start.

### 2. What the build refuses

Every check the builder had is kept: an em dash, a banned term, anything shaped like an address. Two are added, and both are for faults this project has already had:

1. **A link to `releases/latest` or `v0.1.0`.** Neither serves a build.
2. **Download buttons that do not point at `releases/download/nightly/<stable name>`.** A versioned asset name is correct for about a day.

A third was added while proving the second works: the site may contain no `.php` file that is not one of the two receivers (entry 129 section 3.3), so there is nothing for a misconfigured nginx to execute.

**And proving that one works turned up a fourth thing.** The builder never cleaned its output folder. A page deleted from the builder would have stayed in `website/_site/` and been published in every archive afterwards, and no diff would have shown it, because the diff is of the builder rather than of the output. It starts from nothing now.

### 3. Fingerprints, and the commit in every page

Every asset URL carries a hash of its own contents: screenshots, PDFs and fonts as well as the stylesheet and the script. Cloudflare cannot serve a stale file after a publish. The hand-rolled `ASSET_VERSION` is gone, because nothing is left for it to be out of step with.

Every page carries `<meta name="grouplab-site-build" content="<commit>">`, and the publish workflow fails if any page does not carry the commit being published. The server checks the live page for it afterwards, which is what makes "the new site is up" a measurement rather than an assumption.

### 4. The donor PDFs

Copied to `website/donor/` and checked before committing:

| file | bytes | sha256 |
|---|---|---|
| `grouplab-donor-pack.pdf` | 220701 | `7253703eadf421465720bc2c5c6937199fa4c99222f108fefe8db65d82107310` |
| `grouplab-donor-instructions.pdf` | 52368 | `52348a0187e487fc28bce468c1df6b1f52efbabd6855aa453519b1a55185acbb` |
| `GL-CF25-LTR-D.pdf` | 67021 | `5a009acc71b33ec28aeb7860bbafbabffaf3df75e3556f8d2c30647560d7ff95` |
| `GL-CF25-LTR.pdf` | 102340 | `8977fbc903b35a2f194f2d45b4b1e060e997163256f0b15bffee446101a4b460` |

**They carry nothing private.** Read out of the content streams and out of the raw file, including link annotations: no email address, no path, no address, no credential word, and the only URL in any of them is the DejaVu font licence inside the embedded font.

That answers entry 129 section 6.3 early: **the donor instructions PDF does not name the old upload address**, so it does not need regenerating for that reason.

### 5. Publishing, and why only a person does it

`.github/workflows/website.yml` has one trigger, `workflow_dispatch`, with a required `reason` that goes in the release notes.

The reason it is worth a test rather than a comment is that the failure would be silent and public: a site republishing itself on every push would put a half-finished change on the public web the moment it was committed, with nobody deciding. Four tests hold it:

| what | why |
|---|---|
| the only trigger is `workflow_dispatch` | read from the `on:` block, not grepped for |
| no other workflow calls or dispatches it | naming it in a comment is not starting it, so the test looks for `uses:`, `gh workflow run` and `workflow_run` |
| nothing else writes to the `site` release | |
| the nightly's cleanup pattern cannot match `site` | **the pattern is run, not read**: it is extracted from `nightly.yml` and tested against `site`, `nightly`, `v0.1.0` and a real nightly tag |

It signs the archive with the key that already exists and **verifies that signature against the public half compiled into `UpdateKeys.cs` before publishing anything**. A key that cannot verify its own signature fails in the workflow, not on the server, where the only symptom would be a site that quietly stopped updating. Given what entry 123 section 2.7 found about signatures that verify on one machine and not another, that check earns its place.

### 6. The server pulls

`website/server/` holds the sync script, a systemd service and timer, and an installer.

Running as root and unpacking an archive off the internet is the most dangerous thing this project does, so most of the script is refusals, in this order: the SHA-256, then the signature, then every member of the archive (absolute paths, `..`, links, devices, pipes, anything not a plain file or directory), then the build itself (the pages that make it a site, the build commit in every page, a file count between 20 and 2000).

Only then is the live site replaced, and it is backed up first. The server then asks **itself**, over TLS, whether `/`, `/download/` and `/support/` return 200 and whether the home page carries the new commit. If not, it restores the backup it just took. The deployed hash is written last, so a failed run tries again rather than believing itself.

A missing release is nothing to do rather than an error, so the timer is quiet until the first publish.

**It cannot reach anything but grouplab.org.** The script never mentions nginx or another domain, which a test holds by reading the code with its comments and docstrings stripped out, and the unit's `ReadWritePaths` are grouplab.org's own folders, its backups, its log and its state.

Nine tests: seven drive the refusals with no network and no root against the script's own functions, including three hostile paths and a symlink; one holds what it must never do; one holds what the unit lets it write to. A whole site is accepted too, so the checks are not passing by refusing everything.

### 7. What is not done, and why

**Sections 5 and 6 need SSH to Alan's server**, which he approves one command at a time, and the first publish only makes sense after the install. `install.py --dry-run` is ready and says exactly what it would do. Nothing was run on the server, and `C:\Dev\grouplab-site` was read for the builder and nothing else.

## Entry 127. Saying plainly whether the work is done

Alan could not tell the difference between finished, waiting, and stuck. Several runs ended with a line like "I'll hold here" while CI was still going, which from his side is indistinguishable from being finished, and nothing continued until he sent a message. That is a real cost: a run that was fifteen minutes from done sat idle until he happened to look.

`CLAUDE.md` now exists at the repository root, which Claude Code reads at the start of every session. It carries:

1. **Do not stop to wait for what can be waited for.** A CI run, a test suite, a nightly publishing, a download: poll it in the same run and carry on. Only a wait longer than an hour, or one needing Alan, may end a turn. This is the rule that changes behaviour most, and it is first for that reason.
2. **Three status lines**, one of which begins the last message of every turn: `STATUS: DONE`, `STATUS: WAITING, NOT FINISHED`, `STATUS: NEEDS YOU`. The last two carry the exact message for Alan to send back, in a code block, so he does not have to compose one.
3. **A "What Alan needs to do" block** directly under it: one action, or "Nothing."
4. **No hedging anywhere else.** If the work is not continuing, the status line is what says so.
5. **A checklist for `DONE`**: every section finished or reported as not done with its reason, the inbox empty, everything committed and pushed, CI green on the last commit, and any nightly the work should produce published.

It also carries what was previously only in this log: where work comes from, what actioning an entry means, and the standing constraints (`tools/` is read only, never touch `C:\Dev\grouplab-site`, no GPS or metadata in logs, no `v*` tags by hand, no repository settings, CI on three platforms, everything outward through `IOutsideWorld`, no em dashes).

## Entry 126. The support link gets its address

### 1. The support button, before and after

| | |
|---|---|
| **before** | "There is no support address yet. When there is one it will open here, and GroupLab will never take a payment inside the application." The button asked for nothing and opened nothing. |
| **after** | "This opens the support page at grouplab.org, which says how to help and how to get in touch. GroupLab will never take a payment inside the application." The button asks for `https://grouplab.org/support/` through `IOutsideWorld`. |

The address is still written in exactly one place, `SupportLink.Address`, with `SupportLink.Email` beside it. Nothing else in the source may write either.

### 2. Where to send a report

The report dialog now ends with the two ways the support page names: open an issue at the repository, or email the package to `support@grouplab.org`, either way with the build line from the settings page. That last part is not decoration: a report that does not name its build names nothing, because a nightly changes whenever the code does.

### 3. The test changed from "none" to "exactly these two"

`SupportLinkTests` held that **no** support address appeared anywhere, because there was none and a made-up one would have sent somebody who wanted to help the project to a stranger's website. That risk did not disappear when the domain went live; it changed shape. A stale address, a typo, or a second address somewhere nobody looks is the same harm. So it is now three tests:

1. **The address is written in one place.** `SupportLink.cs` has both constants, and no other source file contains either string.
2. **Nothing a person receives carries any other support address.** By donation host, by URL shape (`support`, `donate`, `sponsor`), and by email at any other domain.
3. **grouplab.org is the only GroupLab domain named anywhere.** This one was not asked for and is the most useful of the three: the website is built from this repository, so a wrong domain written here would be published on it.

The third test needed care. Matching `grouplab\.[a-z]+` case-insensitively matches `GroupLab.Core`, `GroupLab.App` and `grouplab.db` several hundred times. It matches case-sensitively against a list of real extensions instead, because that is exactly the line between `grouplab.com` and `grouplab.json`.

`OutsideWorldTests` was the entry 122 recorder test that held the button asking for nothing; it now holds it asking for that one address, and still opening nothing, because the recorder is in its place.

Question 28 is closed, having been answered twice: once with a placeholder, once with the address.

### 4. Only synthetic material is published

Everything in `docs/figures/screens/current/` is published on grouplab.org by the site build. That folder is the one place the project's standing rule about photographs could be broken by accident rather than on purpose: a test opens an image, photographs the window, and the picture goes to a website. Nobody would catch it reading the diff, because a PNG diff shows nothing.

`SOURCES.md` in that folder records what each of the 40 images was made from, against four allowed sources: the Entry109Tests synthetic sheet, a built-in library sheet, no sheet at all, or scan 3 under its consent record. All 40 today are the first three; nothing uses scan 3.

`PublishedRendersTests` holds four things:

| what | why |
|---|---|
| every published image has a line in `SOURCES.md` | otherwise nobody can tell what a picture shows |
| every line names an allowed source | the list is a list and not a pattern, so adding to it is a decision |
| only three named test files may write into the folder | **a manifest alone is a promise about the past** |
| none of those three may read an image from outside this repository | a range folder or a submission by absolute path is exactly how a photograph would arrive |

The last two are the ones that matter. The harm here would not arrive as somebody editing `SOURCES.md` dishonestly; it would arrive as a new test that nobody read closely, rendering something it should not, with a plausible line added to the manifest afterwards.

Writing that guard turned up two things worth naming. `UserGuideTests` reads the folder path to check the guide's links, so naming the path is not publishing into it: the guard looks for a file that both names the folder and saves a rendered frame. And this guard names both itself, so it excludes itself by name, as `OneWayOutTests` does.

### 5. What the site reads, unmoved

Nothing was renamed or moved. `docs/figures/screens/current/`, `docs/USER-GUIDE.md` and `docs/TESTING-GUIDE.md` are where they were, and entry 118's contents list and `ReleaseAssetTests` are untouched because no heading moved. `C:\Dev\grouplab-site` was never read, written or run, and that is now a standing rule in `CLAUDE.md` rather than something to remember.

### 6. The renders

`settings-light-1280x720.png` and `settings-light-2560x1440.png` were made again after the change and looked at. The support note reads as above. At 1280 by 720 the support section is below the fold and the page scrolls to it; at 2560 by 1440 the whole page is visible, which is where the note was read.

## Entry 130 section 2b.6. The six scans re-run, and what the fixes actually recovered

Read only, nothing committed, on this machine, through the command line so the figures are comparable with entry 120 section 1.

| Scan | Shots (Alan) | Entry 120 | Now, no calibre | **Now, calibre named** |
|---|---|---|---|---|
| 1 | 15 | 14 | 14 | **14** |
| 2 | a zero group | 0 | 0, codes still unreadable | — |
| 3 | 25 | 25 | **25** | **25** |
| 4 | 23 | 19 | 19 | **24** |
| 5 | 20 | 18 | **20** | **20** |
| 6 | 10 | 9 | 9 | **10** |

**Three of the four missed holes are back, and two of them only when the calibre is named.**

- **Scan 5, recovered without a calibre.** The two holes outside the bull grid are found: 18 to 20, which is Alan's own count exactly. That is `OutsideTheGrid`, entry 130 section 2b.4.
- **Scan 6, recovered with the calibre.** The shot cut by the edge of the scan is detected as a partial hole: 9 to 10, Alan's count exactly. Without a calibre it is still missed, because the hole is judged by shape alone and a rim cut by the crop has less of a shape to judge.
- **Scan 4, recovered with the calibre, and one too many.** The five .22 LR holes refused as "too small" are admitted: 19 to 24. Alan fired 23. **Going from four short to one over is better but it is not right**, and the extra is now a false hole, a split counted twice, or an error in the count of a sheet nobody has re-examined since. It needs the bull-by-bull comparison against the render that entry 120 did by eye.
- **Scan 1, not recovered.** Bull 2's clear hole raises no candidate with or without a calibre. Entry 130 section 2b.1 was not done, and this confirms it is not fixed by anything else that landed: it is still the priority, and it is still the same bull as the hole missed on Alan's first sheet on 2026-09-20.

**The gate record is unchanged.** Scan 3, the published sample, reads 25 holes on 25 bulls with a mean radius of 0.232 in, with the calibre named and without it, exactly as before.

**The one thing to take from this table** is that naming the calibre is now worth a great deal: it is the difference between 19 and 24 on scan 4 and between 9 and 10 on scan 6. That is an argument for entry 131 section 6.3, the calibre confirmation before Accept, which is still not built.

## Entry 130 section 3.5. Scans 4, 5 and 6 assigned, and why the offset solver did not help them

Read only, with the calibre named, so the hole counts are the best ones GroupLab can reach today.

**The sheet-wide offset solver of section 3.1 is built, tested and not connected.** `ImpactOffsets.Solve` is called by nothing in the assignment path; the only caller anywhere is `AssignmentCertainty.Of`, which reports offsets it is handed rather than finding them. So these three scans are assigned exactly as entry 120 found them: one shot to one bull, nearest first. Section 3.2 was the item that would have wired it in, and it was not done.

What that costs, scan by scan:

**Scan 6, ten shots into bulls 1 to 10.** GroupLab puts them on bulls 1 to 5 and 12 to 15, with one on 21. The first five sit about half an inch low on their own bulls; the next four are a whole row down from where they were fired, because a shot that lands low enough is nearer the bull beneath than the one aimed at. The tenth, the shot far from everything, lands on bull 21 with an offset of 1.216 in.

**Scan 5, twenty shots into bulls 2 to 5 of each row.** GroupLab puts shots on bulls 1, 6, 11, 16 and 21, none of which was fired at. One is 4.666 in from the bull it was given.

> **And the figures follow the assignment.** Scan 5 reads a mean radius of **1.120 in** with an extreme spread of 5.304 in. Those are not this sheet's numbers: they are the numbers of a group measured from the wrong centres. Twenty shots that were tight around their own bulls are reported as a group over five inches across.

**Scan 4, twenty-three shots with two offsets split at a row.** 24 holes found, assigned across bulls 2 to 25 with bull 1 empty, and offsets running from 0.013 in to 2.076 in. The mean radius reads 0.731 in.

### Finished the same night: the solver is connected

The wiring was done after the measurement above, so the table describes the state before it.

**Where the shooter has said which bulls they aimed at, the sheet's point of impact is solved before assignment and the matching runs in that frame.** On a synthetic GL-CF25-LTR shot at the second to fifth bull of every row with the whole group landing one bull to the left, all twenty shots now find the bull they were fired at. Nothing stored moves: the shift is a frame the matching runs in, and every shot keeps the position it was detected at.

**The restraint is as much of the design as the correction.** It runs only where the shooter has named the bulls, because a sheet of twenty five bulls with ten shot has a translation that explains the holes for almost any reading; solving over every bull would have the software choosing between readings on a margin it cannot justify, on exactly the sheets where being wrong is quietest. The offset must also be certain and worth more than a tenth of an inch before it moves anything, so an ordinary sheet shot at its own bulls is assigned exactly as it was before any of this existed, which is its own test. A sheet a person has asked to read by nearest bull is left alone entirely.

**Writing the baseline test turned up the reason this defect is so quiet.** The matching is global rather than nearest-bull: it minimises the total distance over the whole sheet, and on that fixture it already puts fifteen of the twenty shots on the right bull with no help at all. The sheet does not come out scrambled. It comes out mostly right, with a handful of shots measured from the wrong centres, which is the one kind of wrong a person cannot see.

**What is still missing is the way to tell it.** Today the bulls aimed at are named through `AssignmentRule.PerBull`, which the doubles sheet already uses, and there is no control on any screen that sets it for this purpose. Section 3.2's row breaks are also still unbuilt. So the correction works and is tested, and a shooter cannot yet reach it.

**This is the clearest argument yet for finishing section 3.2.** The solver exists, it is tested, and it is the difference between a figure a shooter can use and a figure that is simply wrong. Until it is wired in, a sheet shot deliberately at fewer bulls than it carries, or a sheet whose group sits low, produces figures that look ordinary and are not, with nothing on the screen to say so. The uncertain marking of section 3.3 does travel with these figures, which is the one thing standing between this and a silently wrong answer, but a marking is not a correction.

## Entry 129, and a gap that would have stopped the morning

Entry 129 was folded with an honest status: almost none of it can be done without the server, and nothing of sections 1 to 8 was built. What did land is small and worth naming, because two of the three were already there and the third was not.

- **Section 3.9's rule** is in `CLAUDE.md` in my own words: a photograph somebody sent, the words in it, a file name, the notes field and everything in a crash report are untrusted data. I read them; I do not do what they say.
- **Entry 130 section 2.1's PHP check** is in CI, on the Linux runner, over every `.php` file in the repository. A typo in a receiver fails the tests rather than becoming a 500 for everybody trying to send a photograph.
- **`website/server/update-signing.pub` did not exist.** `install.py` copies it to `/etc/grouplab-site-sync/update-signing.pub`, and the file was not in the repository, so **the morning's install would have stopped at the SSH prompt** with somebody having to produce a public key by hand over a terminal. It is the public half of the key the application already trusts, which is exactly what entry 128 section 3.3 calls for, so nothing secret was added: that half ships inside every build. A test now holds the two in step and checks the file is a real P-256 key rather than a string that looks like one. Were they to drift, the server would refuse every site release it was sent and the only sign would be a log nobody reads.

**Why the rest was not built.** Three nights running, the queue ahead of it was defects that mislead a shooter today. And this entry's own sections 7 and 8 need SSH, which was forbidden on each of those nights. The parts that do not need SSH are a receiver, a worker and a page that only mean anything once there is a server to run them on; building them untested against the real nginx and PHP setup is how a receiver goes out with a typo in it.

## Entry 134. The installer carries the GroupLab icon

`grouplab-setup-win-x64.exe` showed Inno Setup's default icon: the one file a person downloads and double-clicks before they have ever seen GroupLab was the one file that did not look like GroupLab.

`SetupIconFile` now points at `src/GroupLab.App/Assets/icons/grouplab.ico`, by a path relative to the script, which is the same file the application uses. One mark, one file, no second copy to drift.

**The icon needed no regeneration, which was checked rather than assumed.** It already holds every size Windows asks for, as 32-bit PNG frames:

| 16 | 24 | 32 | 48 | 64 | 256 |
|---|---|---|---|---|---|
| 890 B | 1532 B | 2155 B | 3382 B | 4528 B | 18739 B |

![The mark at every size the installer needs](figures/installer-icon.png)

The wizard pages carry the mark too, at 55 and 110 pixels. Inno Setup takes only BMP there, so `packaging/windows/make-wizard-images.py` generates them from that same icon rather than anybody drawing again. `UninstallDisplayIcon` already pointed at `GroupLab.App.exe`, so Add or remove programs was already right.

**How it is held.** Two checks, because they catch different failures.

1. `InstallerIconTests` fails if the setting is missing, if its path rots, if it stops being the application's own icon, if a size Windows asks for goes missing, or if a wizard image is absent or is not a BMP. That runs everywhere the tests run.
2. A CI step on the windows package job reads the icon back out of the setup executable **that was actually built**, and prints its size beside the source icon's sizes in the run summary. The first check proves the intent; this one proves the artefact.

**Why it is worth a test at all.** Nothing breaks when `SetupIconFile` goes missing. The installer still builds, still installs, and still works; it just quietly goes back to Inno Setup's icon. That is the kind of fault nobody reports and nobody notices for months.

# A new image is a new target: the rest of entry 140

Entry 140 sections 1.4, 2, 3 and 4. Section 1's reset and section 1.3's offer landed in `75ff2e8`; this is everything else.

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

