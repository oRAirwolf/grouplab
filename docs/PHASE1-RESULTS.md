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

## Entries 206 to 208: the phones GroupLab must run on, the minimums, and the survey design

**The phones** (entry 206, approved in entry 207): the planning session's market study is in `docs/ANDROID.md`, "The phones it must run
on", with its sources, and the budget beside what the Fold 7 measured: Android 10 or later, 4 GB, peak under about 400 MB (373 MB at the
8 MP working size), detection about 10 s on the A16 class and 30 s on the A06 class (estimated from Geekbench, not yet run), 8 MP camera,
under 100 MB installed, 360 dp screens. The spike's minimum is now Android 10.

**The minimums table** (entry 207 section 2), in `docs/PLATFORM-SUPPORT.md` and so in the README and on the download page: .NET 10's
operating system floors; the architectures actually published (Windows x64, macOS Apple silicon and Intel, Linux x64; no Arm64 on Windows
or Linux); 4 GB of memory with 8 GB recommended, from the analyzer's measured peak of 733 MB on the 600 dpi sample; installed sizes from
the unpacked nightly 103 downloads, 226 MB Windows, 219 MB Linux and 185 MB macOS on Apple silicon, and about 220 KB a saved session; a
window about 1060 wide, question 58. Android is listed as planned.

**The survey** (entries 207 section 3 and 208): `docs/SURVEY.md`. What is asked and where, what is sent and never sent, the benchmark,
the route, the published page with groups under 10 merged, and the review of the minimums at 200 reports from a platform. Built with the
next desktop work; the Android part with the real application.

**Request 30** asks for the older test phones' models, Android versions and whether they still work.

## Entry 254: question 64 answered from real holes, and C3 released

**The real holes.** `aim test09262026.png` (600 dpi, the aim point card of 2026-09-26), each hole's bright blob on the lid of the
scan: C's shot through the black diamond 0.194 in across (box 0.217 by 0.228 in); C's second mark 0.091 in; I's hole 0.345 in, several
shots through one hole and not used; A and E on white paper 0.139 to 0.347 in, the same-sheet control. The synthetic hole on black
showed 0.05 to 0.085 in, which is why it was refused; a real one's core is as large as on white paper.

**The check** (`ZeroGridC3Tests.ARealHoleInSolidBlackIsFound`, seven cases): C's hole, cut from the scan as a 256 pixel square
(`Fixtures/real-hole-in-black-2026-09-26.png`, consent in `samples/PROVENANCE.md`), set into GroupLab's own render of each C3
diamond and the E and C bulls, at 300 dpi, with and without a calibre: found every time, more than half its area on the black.
`TheSyntheticHoleOnBlackIsSmallerThanARealOne` replaces the old refusal test and holds the difference. **The on-ink floor is unchanged**;
the white print specks in the card's black measure 0.030 to 0.036 in, below every floor.

**Released.** `GL-ZERO-MOA-100Y`, `-MIL-100Y`, `-MOA-100M` and `-MIL-100M` are C3 (grid style 3) in the library, with new identifiers;
the style 2 sheets are frozen in `targets/frozen/zero-grid-2` and still read. `TARGET-LIBRARY.md` section 5 is rewritten; the held
builder and the `library held` command are gone. Tests that held a ring aim's touching pairs read the frozen style 2 sheet. Request 51
is answered unasked; E and C needed no warning.

## Entry 252: Shots Needed to Zero; C3's diamond in angle and the MOA grids

**Sections 1 and 2** are in the C3 sheets of entry 251: the diamond 0.2 mil or 1 MOA point to point, the MOA grids at 0.5 MOA squares
with 1/4 MOA ticks, MOA-100Y 3 MOA across and 3.5 up and down, MOA-100M 3 MOA; held with the mil sheets for question 64.

**Section 3** (`ShotsToZero`, `MainWindow.ShotsToZero.cs`, credited to Jylee). With sigma known, one axis's chance is a closed form
(the true zero uniform in its click, the centre of n shots normal about it); both axes is its square. With sigma estimated from the group,
sigma is drawn from its own uncertainty, 4,000 stratified quantiles offset by the seed, and each draw's chance is the closed form, so the
error of the mean is bounded by the chance's range over 4,000 and the screen says how far a count could move. The procedure simulated
shot by shot (200,000 sessions) agrees within 0.005; the planning session's 42, 56, 68 and 76 percent at 5, 10, 20 and 40 shots are
reproduced; more shots never lower the chance; a wider rifle needs more; an uncertain sigma needs more than a known one (a 5 shot group's
99 percent within one click, 9 shots against 5). Glossary: closest click, within 1 click. Article 32, `shots-to-zero`.

**Section 4.** Cost: trials, times the shot counts the bisection visits (about a dozen for each of twelve thresholds, cached), times two
goals; the centre of n shots is never drawn shot by shot. Desktop, measured 2026-09-28: 150 to 191 ms a calculation for 5, 10, 25 and
100 shot groups at sigma of 0.3, 1 and 3 clicks. Budget held in `ItIsWorkedOutWithinTheBudget` (2 s allowed for a CI runner). The Fold 7,
the tablet and a 4 GB phone are measured in request 50's sitting through GroupLab Dev's `org.grouplab.test.shotstozero`; by the benchmark's
ratios, 1.45 and 2.6 times the desktop, about 0.25 and 0.5 s, and the phone's run is off its interface thread. Nothing had to give.

## Entry 251: the C3 zeroing grids, built and held

**The design, as grid style 3** (`GridStyle3`, TARGET-SCHEMA.md section 3.13's new subsection). 0.2 mil or 0.5 MOA squares; lines
0.5 mm, whole MOA 1.2 mm, axes and frame 2 mm; a click's tick (0.1 mil, 1/4 MOA) halfway between lines on the centre cross and inward
from the frame; each line's distance from the aim outside the frame on all four sides, whole units larger and bold, nothing inside;
the legend above between the two codes in bold ("MIL · 100 YD", a square and "= 0.2 MIL", a tick and "TICK = 0.1 MIL (1 CLICK)"); the
4 in or 10 cm check bar below; the C diamond as the aim, 0.2 mil or 1 MOA point to point (entry 252 section 1), its white centre
kept clear of the cross. Markers stand in the two side columns between the numbers, at the ticks' heights, since the numbers take
the corners and the rows. **Bold** is new to the format's text: Helvetica-Bold in the PDF only when a page uses it (every existing
PDF is byte for byte as it was), and Liberation Sans Bold's outlines for the preview.

**The four sheets** (`LibraryBuilder.ZeroC3Sheets`, `grouplab library held`): MIL-100Y plus or minus 1.0 mil; MIL-100M 0.8 mil both
ways (1.0 up and down collides with the check bar and the identifier); MOA-100Y 3 MOA across and 3.5 up and down; MOA-100M 3 MOA.
None has a load block: there is no room beside the numbers without shrinking the grid, so the load goes on the session. They
validate, round trip through GLTD-B as style byte 3 with their markers, and no number touches a marker or another number.

**The detection check** (`ZeroGridC3Tests`, 300 dpi, .224, .264 and .308): on the grid, 72 of 72 holes found, worst 0.023 in. In the
diamond's black, 0 of 12: refused as too small, because the rim vanishes into the ink and the size floor is set on whole marks on
paper. Touching the diamond 12 of 12, worst 0.053 in; in the white centre 10 of 12. The same refusal happens on the E and C bulls
already released. **So the C3 sheets are not in the library yet:** question 64, and request 51 for a real scan of holes in black.
PDFs for that are in `C:\Dev\grouplab-local\zero-concepts\`.

## Entry 250: the Targets preview shows the words; Letter before A4; the one-shot zero evaluated

**Section 1.** The preview drew a scene's discs and rectangles and skipped its text. `SceneRasterizer` can now draw text runs, from
glyph outlines derived from Liberation Sans (metric-compatible with the PDF's Helvetica) and placed at the PDF's own advances
(`SheetGlyphs`, the outlines written by `scripts/sheet-glyphs.py` with the font's license at their head). Only the previews ask for it;
everything that measures a render still draws no words, so no measured figure moves. Both previews carry the actual-size line when the PDF
does, and the "text is drawn in the PDF" sentence is gone. **Which sheets were wrong: every one.** Every library sheet and a designer sheet
has words, and the old preview differed from its PDF by 1,643 to 43,016 pixels at 150 dpi; with the words, 0 to 4, except the zeroing
grids' large labels at about 1,300, the fine shape of Liberation Sans against the reader's Helvetica (`PreviewMatchesPdfTests`). No two
words overlap on any sheet (`NoWordsOnAnySheetRunTogether`); the mil grid's "1.0" and "0.5" come close without overlapping, and entry
251 replaces that layout.

**Section 2.** One rule in `TargetLibrary.PaperRank` orders each family: Letter, then Legal, Tabloid and the rolls, then A4, A3 and A5,
the catalogue's order within a size; with the region's A4 the ISO sizes lead. The desktop and the phone pass the region
(`LibraryOrderTests`).

**Section 3** is superseded by entry 251 (C3 chosen); **section 4** is `docs/notes/ONE-SHOT-ZERO.md` and its answer in `for-alan.md`.

## Entry 249: Desktop | Mobile on the tour and the Features page

**The switch.** At the top of the tour, every stop and the Features page. Both sides are in each page and the switch shows one: a
link's `?platform=mobile` or `?platform=desktop` first, then the visitor's own last choice (kept in the browser's local storage, no
cookie, never sent), then Mobile on a phone or tablet and Desktop otherwise. It is decided in the head, with the theme, so the page
never draws the wrong side first; without scripts it is Desktop.

**The Mobile side.** Every tour stop has one in `website/tour.json`: the phone's own screenshot from entry 246's sittings, a caption, and
parts and steps written for touch, or words saying it is on the desktop only, for now (the full analysis, compare, equipment and
ballistics). Capture is a new stop the phone alone has, and its Desktop side says so. Marking on the phone is the result screen's Move,
Add a hole and Remove, and says that placing bulls by hand is desktop only. Every feature the phone has shows its picture on the Mobile
side of the Features page, or the reason it has none, and a desktop feature says "On the desktop only, for now".

**Held.** The site build refuses a stop without a Mobile side, a phone screenshot that is not there, a Mobile side without three parts
and two to five steps, and a feature the phone has without a phone picture or a reason. `TourTests` adds `EveryStopHasAMobileSide` and
`TheMobileStepsAreWrittenForTouch` (no click, mouse or hover on the Mobile side; a turret's clicks are allowed).

**Found on the way.** The home page still called GroupLab "a Windows test build" and listed Android as not built; it now names the four
platforms and the things the README says do not exist yet.

## Entry 248: the desktop's mark as the Android icon

`scripts/android-icons.py` draws the icons from `grouplab-mark.svg`: adaptive icons for GroupLab (dark background) and GroupLab Dev
(light), each with a monochrome layer for themed icons and the mark inside the safe zone, square and round PNGs at every density, and the
Play listing's 512 px icon and 1024 by 500 feature graphic in `docs/store/`. Both builds point at theirs (`Entry234Tests`). On nightly 115
both devices show the two side by side (`docs/figures/screens/phone/icons-fold.png`, `icons-tab.png`). The themed version was not
photographed: One UI turns themed icons on for the whole home screen or not at all.

## Entry 247: the desktop Ballistics screen as concept B

**Layout.** Three columns as on the analysis screen (`MainWindow.BallisticsLayout.cs`). The top bar holds the pickers, the unit switch and
the one solid amber button; a chosen switch (units, the two views, the chart's series) is the amber tint, a new `chosen` style, so the
primary stays the only solid amber in every theme (`InEveryThemeThePrimaryIsTheOnlySolidAmber`, dark, light and high contrast). The left
is labeled rows in sections that fold, remember, and say what they hold in one line; a field the solver needs says "needed" and opens its
section. The middle is the chart, now clickable, and the table with velocity and energy columns and the chosen row in the tint; the
windage clicks column holds the count, its direction in the heading. The right is "At one range", elevation at the lead size in amber,
then figure rows, then the analyzed group carried there. A sentence such as what stability needs reads under its name, not squeezed right.

**The hit probability view.** The middle's second view, remembered. In it the rifle, load and air fold to their summaries and come back as
left; The target and What you are unsure of open, The shot and the simulation folded. The answer is a card leading with the first round's
chance, the scatter beside it, and What costs the most under both with a bar for each cost the simulation can tell apart; card and
scatter appear only once there is something in them. The target's distance and the range on the right are one field in effect. "Advanced"
no longer exists, so the preset's sentence says its figures are below.

**Held.** Three columns at 1280, 1400, 1920 and 2560, the right under the middle at 960; a needed field marked in its row; a chosen range
worked out on the right with the hit's distance following; the view switch; the hit view's folding. Nothing the solver or the simulation
computes changed: `Entry112Tests`, `Entry113Tests` and `Entry156Tests` pass with only the table's heading changed.

**Screenshots.** The walk (`Entry109Tests`) now also makes 1920 by 1080, which the Store listing uses and which had fallen out of it at
entry 112; the committed 1920 set is current again, eight new with their `SOURCES.md` rows. Before and after in `for-alan.md`. The tour's
Ballistics page and the user guide's section 8 describe the new layout.

**Also in this commit.** The red CI on 8eb2396 was `StateFileTests.ItsInboxListIsWhatTheInboxHolds`: STATE listed the four uncommitted
inbox files; it now reads what the repository holds.

## Entry 246: the Fold 7 and the tablet, look B, and the two sittings

**First sitting** (edcd707): the Fold's benchmark line, 2.6 s, 438 MB, 25 of 25, on the survey page; GroupLab Dev on both; a 32 MP
scan at the working size, 25 of 25, Fold 429 MB at most in 2.8 s and tablet 523 MB in 4.6 s, against budgets of 724 and 1,527 MB. What
the devices showed was fixed in the same commit: the phone's result order, side by side only in landscape, Letter by region, and a
second activity stacked by a start aimed at a running GroupLab.

**Look B** (eb8a936): the desktop's tokens and faces with `PhoneStyles` on top, on every screen, light and dark.

**Second sitting**, nightly 115 on both: GroupLab updated in place; GroupLab Dev reinstalled, since the one there was a local build signed
differently (announced in the panel first). Every screen in light and dark, the tablet upright and sideways and the Fold's cover screen
upright and sideways; the result from Alan's Dominus K scan through the Dev build's test picture, so no picker showed his own pictures.
The status bar is cut off every picture and the S Pen's floating button painted out with the page around it. What is published is in
`docs/figures/screens/phone/`, and the Features page shows it for the phone's own features (`phoneShot` in `website/features.json`,
checked by the site build). Every setting the sitting changed was put back and compared with the record taken first.

**Found on the way:** the release-notes check refused nightly 115 because "an unfolded phone" contains "folded"; it matches whole words
now (f0a8bbb).

## Entry 245: question 63 answered, and where the load goes on a set

(a), as built: 2 by 4, 8 a sheet, 32 a set, no sighters and no load block. `LibrarySheet.Summary`, which the desktop's Targets screen and
the phone's both show beside a sheet, now ends a set of several sheets with no load block by saying the load is entered on the session in
GroupLab; no separate load block sheet exists to point to. The 300 yd tile sets say the same, since they carry none either
(`PrintNoteTests`).

## The server sitting of 2026-09-27: entry 235 section 3 and entry 241's server half

Every command was written into the panel mirror first. The dry runs were clean. **Request 21:** the server's copy of the grouplab.org
include was already the repository's, so the receiver's five minute block was in and nothing needed reloading; the receiver answers an
empty post with 400, grouplab.org/targets/ 200 and pissinhot.com 200. **Entry 241:** the survey worker and its service replaced and the
site sync script replaced, each with the old copy kept beside it; the worker ran, set the first version's state aside and published an
empty aggregate, counting again under the keyed hash; `survey/aggregate.json` is served and `api/survey.php` refuses an empty post.
**Found:** systemd warned that `RuntimeMaxSec=` does nothing on a oneshot service, and four workers relied on it, the intake worker's cap
on a decompression bomb among them. Each now says `TimeoutStartSec=` with the same seconds, systemd reports 15, 5, 30 and 5 minutes in
force, and `SiteSyncTests` holds every oneshot unit to the limit that applies. Nothing of pissinhot.com was touched.

## Entry 244: the README kept current with every build

**The pass.** Download names the newest build and carries the Android APK, what it does and does not do, and how Play's internal test and
GroupLab Dev fit; "What is new" and "What it does" are new sections; the concept mockups are replaced by the build's own screenshots
from `docs/figures/screens/current/`; Status lists what exists today, load comparison, the solver and the Android app among it, and what
does not; Phase 6 is in progress with its features; the architecture diagram says Android is in testing. The platform statement's
Android row and paragraph, in `docs/PLATFORM-SUPPORT.md`, said "planned" and "not published yet"; both are corrected at the source.

**Generated** by `scripts/readme.py` between `readme:` markers: the newest build and its downloads, and what the newest three builds changed,
from `docs/RELEASE-NOTES.md`, each change linked to its Features page entry where one owns it; the feature list from
`website/features.json`; and the checks before the first beta or stable release from `docs/RELEASE-PLAN.md`. The nightly and the release
workflows rewrite them and commit a `[notes]` commit when they changed. **CI** (`website builds`) fails when a section is stale, or when a
release note of kind "new" has landed since the README's prose last changed and neither the feature list nor the prose names it; a change
only between the markers does not count as a change to the prose, so regenerating cannot silence it. Tried against the history twelve
commits back: it names the one new note no feature owns. The claims register reads the README less its generated copies, whose sources
it reads in their own right. The end-of-entry report now says "README checked" (CLAUDE.md).

## Entry 243: questions 57 to 62 answered, and the night's work: pooled sets, progress, the phone, and the C bull

**Answers built** (e3f24a5, 9df40da). Q57: on a sheet of two to four marks, one at least twice the median area of the others is flagged
tentatively with the "judged from too few marks" sentence (`TightGroupTests`). Q58: below the default width the outer columns shrink and
below about 893 units the right column moves under the image (`NarrowWindowTests`, 960 and 683). Q59: grid style 2 is written into
TARGET-SCHEMA.md as built. Q60: the screenshot walk analyzes Alan's two 6ARC scans from the test data release; the rule is now "no
photograph or scan of anybody else's target". Q61: E as discs on three sheets beside the usual ones (GL-CF25-LTR-E, -LTR-D-E, -A4-E).
Found on the way: opening a second picture did not start a new session, so saving overwrote the previous one; fixed and tested.

**Section 3.1, pooling a set** (8685aaf). A sheet from "Made for your optic" carries its place in the set in its codes; Session records'
"Pool the chosen" reads the ticked sheets as one group by `docs/STATISTICS.md`'s pooling rule and says which sheets are missing
(`SetPoolTests`, `PoolSetTests` with three of a seven sheet set). **3.2, progress and Cancel** (83c2fd5): every analysis on the desktop
and on the phone says what it is doing a step at a time, from the trace's own stages (`StageWords`), and Cancel leaves nothing saved.
**3.3, big screens** (803ebb2): on the phone, a window at least 840 dp wide puts the sheet beside its numbers; narrower is unchanged. The
tablet screenshots wait on request 45. **3.4, Targets on the phone** (70419ed): the library and "Made for your optic", printed through
Android's print dialog or shared as the desktop's PDF. That commit also fixed what three earlier commits had left red unseen, because each
push canceled the last CI run: the E sheets' artwork recorded, and two classes given bench lines.

**Section 3.5, the phone's look** (0d1f069). DESIGN.md section 19 now records the desktop's design language as built. Three concepts,
A the desktop carried over, B cards for the thumb, C readout first, are drawn beside today's screens on a private page and in
`C:\Dev\grouplab-local\design-concepts\`; request 49 asks Alan. The one plain mismatch, Fluent's blue accent on the phone, is now the
desktop's amber.

**Section 4, the C bull** (89a39ac). The format has a square: a disc's `shape` and `rotation`, its diameter the diagonal, turned 0 or
45 degrees, in bits 4 and 5 of the disc's ink byte on the wire, which older builds reject, as their JSON reader rejects the keys. Rules 20a
and 20b in the validator. The renderer, raster, PDF, Windows printer, plot and report draw it; the edge fit measures each ray to the
square's side. **Section 4.5's question, answered:** on the 1.5 in grid the diamond leaves 0.24 in between points, and nothing needs to
sit there. The markers sit on the diagonals, where the diamond leaves more than 100 dmm of paper, but the drop test measured the box
round the circle through the points, which left no markers at all on two of the three sheets; it now measures a square to itself, and the
C sheets keep 34 to 40 markers, as the E sheets do. A hole on a point is 159 dmm from its own bull's center and 221 from the next, and
`DiamondBullTests` finds and assigns every one: holes on the dot, in the black, across both diamonds' sides and on the points, every bull
within the 0.001 in gate. The pitch stays. The one warning left is real: on the Letter sheet the top two diamonds' points come within 1 dmm
of the codes' footprint, quiet zone included. Holes placed across an ink edge come back up to 13.6 dmm off center; the round E bull under
the same test loses one outright and puts two 12 and 14 dmm off, so that is the synthetic holes, drawn wholly on ink or paper by their
center, not the diamond. The generator offers a diamond sized by the same rule, and the designer rings, E or C.
**Found on the way:** OpenCV keeps a code whose bytes happen to be valid UTF-8 as UTF-8, and the reader turned every code back as Latin-1,
so one of the C sheets could not name itself; its CRC was CE 95. The reader now takes the reading that can be right, and in the one truly
ambiguous case the one whose frame decodes (`QrPayloadTests`). Corpus counts: 55 of 55 images unchanged through all of it.

**Section 1.4, the large format sheets** (c25609c). GL-LR25-T, GL-LR25-TA4 and GL-LR30-T: 2 by 2 sets of Letter or A4 sheets, the same
bull and pitch, 8 a sheet and 32 a set; the printed tabloid and A3 sheets are frozen and still name themselves. Question 63 asks whether 8
is right.

## Entry 242: "Made for your optic" on the tour, and a Features page from one file

**The tour stop** `optic`, after Targets: the render walk fills in the generator for 100 yd at 10x and at 4x and photographs the Targets
screen in both themes at every size, and writes the generator's own numbers and sentences to `optic-numbers.json` beside the pictures;
the tour's text takes every number from there with `{optic:...}` tokens, so nothing is typed. At 10x: a 0.37 in white center in a 1.11 in
disc, 25 bulls 1.51 in apart on one sheet; at 4x: 0.92 in in 2.74 in, four a sheet, seven sheets. The second picture is a stop's
`moreShots`, which the build and `TourTests` leave out of the screen list. A new allowed source for published renders, a sheet the
generator made, is recorded in `SOURCES.md` and `PublishedRendersTests`. The middle column still names the list's chosen sheet while the
preview shows the generated one, and the stop says so.

**The Features page**, `/features/`, beside the tour in the top bar: 27 features in six groups from `website/features.json`, each with a
sentence, a screenshot from the walk (four say why they have none), its platforms, the build it arrived in and links to the tour, the
guide section and the article. The build fails when a feature's note is not in its build's section of the release notes, a picture, tour
stop, guide section or published article it names does not exist, or, from nightly 113 on, a note under What you will notice belongs to
no feature and is not listed as not one; tried with a false note and a missing section, both caught. The three newest features are at
the top of the page and under the home page's first section. Every sentence on both pages is in the claims register.

## Entry 240: a memory budget scaled to the device, and the benchmarks Alan ran

**The rule** (`MemoryBudget`): a quarter of what the device says is available above its low memory threshold, never under 400 MB and
never over a tenth of its memory; the floor when it says memory is low. Worked with 45 percent available: PH-1 400 MB, S20 819, Fold 7
1,229, Tab S8 Ultra 1,638. Android reads it before each analysis and logs it with the memory classes (`memory.budget`).

**Whether more memory buys anything, measured at each working size on this machine:**

| Image | 4 MP | 8 MP | 12 MP | 16 MP | 24 MP |
|---|---|---|---|---|---|
| The published scan against its full 32 MP reading, median / worst, in | 0.0019 / 0.0114 | 0.0011 / 0.0040 | 0.0006 / 0.0060 | 0.0007 / 0.0028 | 0.0007 / 0.0027 |

The three kitchen photographs of entry 233 at 8 and at their full 12 MP, against their scans: Dominus K 0.0162 and 0.0154 in median with
two extra marks and none; Magnus S 0.0207 and 0.0211, one and one; the 6.5 sheet 0.0256 and 0.0266, none and one. Nothing that matters,
and the extra marks go both ways, so every phone works at 8 megapixels and the budget holds a big phone's memory in reserve. The desktop
keeps full size and now refuses, below the 400 megapixel cap, an image half its memory cannot hold, with that reason. **The benchmarks:**
Alan's desktop, nightly 111, 1.789 s, 482 MB, 25 of 25, sent: the first desktop benchmark. The Tab S8 Ultra 4.6 s, 446 MB. The Fold 7 is
request 44. Request 47 answers his four questions; the page waits for ten a group as written.

## Entry 239: a picked file decoded at the working size, as the design already said

**It was the decode.** `PhoneAnalysis.Prepare` read a picked file whole with OpenCV, so the published 600 dpi scan existed at its full
32 megapixels in colour before being shrunk to 8, although `WorkingSize` and `docs/ANDROID.md` section 5 both said the phone decodes at
the working size. Now Android decodes it at the largest power of two fraction that still holds 8 megapixels (`WorkingSize.SampleFor`:
a half for that scan, whole for a 12 megapixel photograph, a quarter for 192 megapixels), the pixels go straight into OpenCV from the
bitmap without a managed copy, and only the remainder is resized. Tested for the rule on the desktop; the Android build passes. **Time:**
the desktop reads the sample at 8 megapixels in about 2.4 s, of which finding holes is 1.1 s, decoding 0.37 s and locating the bulls
0.35 s; the phones' 50 s is far more than the speed of their processors explains, so the dev build's logcat lines (`phone.prepare`,
`phone.detect` with its milliseconds) are what will find it, once request 45 has the devices back. The progress line is in entry 243's
work list.

## Entry 238: nineteen angled photographs, and the refusal angle from 40 to 37 degrees

The nineteen photographs of the Dominus K sheet were read by the build with entry 233's detector and each matched against the sheet's
scan; those refused at 40 degrees, or whose codes could not be read, were read again with the limit lifted and the sheet named. **Measured
tilts** (the planning session's thumbnail readings in brackets): first set 2.8 (0), 17.2, 30.4 (15 to 25), 38.5 (55), 47.5 (55 to 60),
13.8, 26.1 (quarter turned), 36.2 (50), 45.9, 50.4 (55); second set 2.0, 17.6, 26.4, 33.9 (45 to 50), 41.0, 61.8, 66.1, 58.5, 63.6.

| degrees | marks found | matched | extra | missed | median from scan | worst |
|---|---|---|---|---|---|---|
| 2.0 to 36.2 (ten photographs) | 25, once 26 | 25 | 0, once 1 | 0 | 0.013 to 0.019 in | 0.027 to 0.057 in |
| 38.5 | 30 | 25 | 5 | 0 | 0.022 | 0.069 |
| 41.0 | 27 | 25 | 2 | 0 | 0.021 | 0.061 |
| 45.9 | 25 | 23 | 2 | 2 | 0.020 | 0.037 |
| 47.5 | 30 | 24 | 6 | 1 | 0.025 | 0.085 |
| 50.4 | 41 | 24 | 17 | 1 | 0.031 | 0.075 |
| 58.5 to 66.1 (four) | 23 to 36 | 8 to 24 | 3 to 28 | 1 to 17 | 0.025 to 0.052 | 0.049 to 0.100 |

Markers decoded fall from 34 of 34 to 24 at 38.5 degrees and 9 at 63.6; the codes went unread on six of the ten from 36.2 degrees. **The
limit** (`OffAxisLimit.Degrees`) is now 37, between the last photograph that agreed with the scan and the first that did not; the
quality score's angle part is worthless at it. **The light:** the square photographs scored 100 of 100, so the dim room cost nothing. The
figure and the data are in the curled-angled-paper article; `docs/MOBILE-CAPTURE.md` section 4 and the user guide say 37.

## Entry 237: the planning session's transcription of both score sheets, against mine

Every one of the 207 cells, 126 of Alan's and 81 of Justin's, reads the same in both transcriptions, and the planning session's totals
match (Alan A 11 to I 27, Justin A 9 to I 11). The three unclear Strike Eagle cells, which Alan confirmed are 0, now say so in the data.
The article tests the planning session's four readings and they hold: at 18x and above nearly every cell is 2; at 10x E is centered in
all five high power cells and C in four; on the PLxC only I, and H for Alan, is reliably visible, and Justin's 8x row is worse than his
6x; and the observers disagree in places. **Agreement, now in the article:** of the 81 cells both scored, 53 the same and 75 within one,
Cohen's kappa 0.44, 0.52 linearly weighted: moderate.

## Entry 235: the first Full backup, requests 32 and 39 closed, and request 20 rewritten

**The first Full Oracle backup** (2026-09-27 09:04:31 UTC, 10 of 47 GB) expires after two days, because the daily and weekly schedules
both fired at 09:00 UTC and Oracle gave the one backup the daily's retention. Alan moved the weekly Full to Sunday 12:00 UTC, kept 13 days;
`docs/RESTORE.md` records the backup, the cause and the new schedule, and that from 2026-09-29 until 2026-10-04 a restore rests on the
incremental chain. Request 39 is closed and request 46 asks for one look on or after Sunday 4 October at 12:00 UTC. **Request 32** is
closed on Alan's answer, "The archive is enough". **Request 20** now leads with the core set that is enough on its own, eight sheets:
.22 LR subsonic and high velocity, .300 Blackout subsonic, and 6.5 Creedmoor or 6 ARC, two each; the extras after; .300 Blackout
supersonic dropped, with why in Program B. `docs/RANGE-PLAN-HOLE-SIZE.md` and its PDF, one page, made by `grouplab user-guide` beside the
guides and held to one page by a test, give the sheets to print, the order to shoot and what to write on each load block. The scanner, a
Brother MFC-J430W Letter flatbed, is in `samples/PROVENANCE.md` and Program B; larger commercial sheets are photographed. GroupLab went
back on the Fold 7 with entry 236.

## Entry 234: GroupLab Dev beside the Play copy, a script for the phone, and logcat

**GroupLab Dev** is `-p:GroupLabDev=true`: `org.grouplab.app.dev`, "GroupLab Dev" under its own icon (the site's amber on its dark ground
with a DEV band; the release keeps Android's default icon, as before), debuggable, its version ending in `-dev`, and its Settings saying
what it is. Checked from the built packages' manifests: the release is `org.grouplab.app`, "GroupLab", not debuggable, provider
`org.grouplab.app.files`; the development build `org.grouplab.app.dev`, "GroupLab Dev" for the application and the launcher, debuggable,
provider `org.grouplab.app.dev.files`. The one place the package was named, the file provider's authority, now follows the application id
in the manifest and in `SessionFiles.Authority`; `Entry234Tests` holds that nothing names it. The nightly builds it after the release APK
and AAB and publishes `grouplab-android-dev.apk` on both releases. The survey worker keeps a `-dev` version's machine apart from every
published figure.

**Logcat.** `DiagnosticLog.Mirror`: every line above DEBUG, formatted and scrubbed as the file's, also goes to logcat under the tag
`GroupLab` on Android. **The script,** `scripts/android/Test-OnPhone.ps1`, connects (mDNS, then the address if somebody is there to type
it), installs GroupLab Dev, starts it, and collects screenshots, its own log through run-as and the logcat into
`C:\Dev\grouplab-local\android-<build>\`. Its first run, unattended, found no phone attached, restarted adb and waited on a prompt; the
tablet left USB and the Fold 7's Wireless debugging no longer answers. The script now restarts adb only when nothing is attached and asks
only when someone can answer. Request 45 asks Alan to reconnect both. `docs/ANDROID.md` section 13 has the Play copy rule.

## Entry 233: phone photos of the same sheets, against their scans

Alan's Fold 7 photographs of the three 2026-09-26 load sheets, on a kitchen counter with a hand's shadow over the bottom third, taped
corners and two turned a quarter, were stripped of all metadata (the orientation applied first), renamed and put on the `test-data`
release with the aim card's. **Before:** 28, 27 and 24 marks against 25, 25 and 23 shots; shot 21, under the shadow, missed on all three;
false holes where the counter showed inside the sheet's nominal edge and along the shadow's edge. Both came from the local paper level:
the 95th percentile of a quarter inch block, averaged with its eight neighbours, took the counter's and the lit side's brightness into
the paper beside them, which then read darker than it was.

**The change** (`RenderDifferenceHoleDetector`): the paper level is measured in eighth of an inch blocks, each then the median of itself
and its eight rather than their mean, and not within 0.15 in of the page's edge (holes are still looked for there). Scored on 15 images
with known shots (the nine local corpus images, the three photographs against their scans, the three scans): hits 249 to 252, misses 21
to 18, false marks 13 to 6, of which three are on one mounted photograph whose registration fails anyway. A rule refusing small marks with
open rims removed the tape tears but cost real holes on the range scans and the zeroing sheets' own renders, and was not kept. The Core
suite, the range scan counts and every sheet's own render pass; the synthetic punched corpus moved by one to four holes a sheet in about
two hundred, recorded. The desktop tests' synthetic scanned sheet (entry 109's) reads 23 of its 25 holes where it read 24: the hole
at the left of the middle row is now refused as not compact. Blocks of 3/16 in keep it but put two more false marks on each 6 ARC photograph, and
the detector's own rule is that an invented hole is worse than a missed one. With 23 shots that report's worst shot card grew a
line and pushed page 1 past its end, where every card is meant to be whole, so the report's plot now gives way a quarter inch at a
time until page 1 fits (`ReportWriter.PlotSizes`); the type never shrinks.

**After:**

| Sheet | Scan | Photo | Median from scan | Worst | At 8 MP |
|---|---|---|---|---|---|
| Dominus K | 25 | 25 | 0.015 in | 0.036 in | 25 of 25, two extra marks |
| Magnus S | 25 | 25 and a tape tear | 0.021 in | 0.040 in | 25 of 25 and the tear |
| 6.5 Creedmoor | 23 | 22 and a tape tear | 0.027 in | 0.057 in | 22 of 23, no tear, mean radius 0.218 in against 0.213 |

The 6.5 photograph gets the whole-sheet assignment (0.70 in high, 0.36 in left, against the scan's 0.76 and 0.39) but the tear at the
bottom right corner takes bull 24 and pushes that bull's shot to the one shot 14 left free; shot 14 touches a marker and is not found.
**The suppressor test from the photographs:** a shift of 0.284 in, p = 0.051, against 0.284 in and p = 0.050 from the scans, positions
from the photographs with the scans' bulls. The camera's live checks look at blur, clipping, angle, flatness and resolution, and none at
light that differs across the sheet, so they would not have flagged the shadow; a line on the Capture screen and the article's
checklist say to shade all of the sheet or none, and to hold it down outside the print. `PhotoAgainstScanTests` holds each photograph
to its scan: every shot found (the 6.5's shot 14 excepted), median within 0.04 in, worst within 0.08, at most one mark more.

## Entry 241: every benchmark run sent, one vote a machine, and the survey page

**On the device.** Every benchmark run is kept with its date and version (`AppSettingsStore.LoadBenchmarkRuns`, thirty kept), Settings
under Sharing lists them newest first, on the desktop and on Android, and every run made while the survey is on goes with the next
report, each with its own version (`grouplab-survey-2`, `SurveyReport.MostBenchmarks` a report); a run the receiver's three reports a
day holds back waits for the next. **Reset my survey number** and **Delete my survey reports** sit under the history; the delete sends
the number with its own schema. The question's wording says what the number is, and a yes given to the earlier wording reads as not
answered (`SurveyReport.WordingVersion`), so the question is asked again once and nothing goes until it is.

**On the server.** `survey.php` stores the number as HMAC-SHA256 with a key made on the server and kept only there; the old code hashed
the number alone with the rate limit's salt, not with the day as `docs/SURVEY.md` said, so Monday's and Tuesday's runs were already one
machine, but the hash was the same salt as the addresses'. The worker keeps per machine and version the run count and quarter second
counts of the times, reads each machine's median, and publishes the median of machines' medians by class from ten machines up, with the
range of runs they rest on; a delete removes the machine at once; twelve months without a report removes it. It writes a copy into the
site, which the site sync now leaves in place. Its first run sets the old state aside, because it is keyed by the old hash.

**The page**, grouplab.org/survey/, linked from the footer and from the article: the three test devices by name, from what each showed
on its own screen (`website/survey-devices.json`): the desktop (Ryzen 7 9800X3D, 64 GB) 1.789 s, peak 482 MB, 25 of 25; the Tab S8
Ultra 4.6 s, 446 MB, 25 of 25; the Fold 7 not read, locked (request 44). Everybody else's half reads the aggregate and says "not enough
reports yet" until a group has ten machines. On Windows the uninstaller leaves the settings, so the number survives an uninstall there;
on Android it does not. Steam's survey is described in `docs/SURVEY.md` section 8.

## Entry 231: the first Play internal testing release, and its two warnings

`docs/ANDROID.md` section 12 records the release as Play read it (version code 110, API 29 and up, target SDK 36, arm64-v8a only, 2
required features), the testers' opt-in link, and that a Play-installed copy and a nightly APK cannot be installed over each other
because Google re-signs the Play copy: uninstall first, which deletes what is on the phone. Request 36 is closed; Alan installed it
from the Play Store on the Fold 7 and it opened to the first-run window.

**The deobfuscation warning has nothing behind it.** The Android SDK pack sets `AndroidLinkTool` only when asked, the project never
asks, and the Release build's intermediate folder has no `proguard` folder or mapping file: the Java side is dexed by D8 unshrunk, so
there is no mapping to upload. **Native symbols:** `build-extern.sh` now keeps the unstripped OpenCV library beside the stripped one
(the change of script also rebuilds the cached library once), and the nightly puts `arm64-v8a/libOpenCvSharpExtern.so` in a zip on the
numbered release. The .NET runtime's libraries ship stripped in Microsoft's packs, with no debug files beside them, so only GroupLab's
own library can be covered. Automatic upload with a Play service account is planned in the same section; the request waits on 38.

## Entry 230: the first Oracle backup, sudo widened, and Alan's answers

The first Oracle boot volume backup is recorded in `docs/RESTORE.md`: 2026-09-26 09:01:42 UTC, Incremental, Available, 10 GB of 47 GB,
expiring 2026-09-28, with the plain words that until a Full backup exists a restore rests on the incremental chain Oracle keeps. Request
35 is closed; request 39 asks Alan to look for the first Full after Sunday 09:00 UTC. The condition of entry 222 is met, so CLAUDE.md's
sudo rule is no longer limited to GroupLab's own files: before any sudo change outside them, what will change and how to undo it are
written down first, and everything about pissinhot.com, `nginx -t` and HestiaCP's folders still holds. Alan's answers (the grid measured
right, "1" means A, the order and timing of the suppressor sheets, Justin by first name only, the score sheets, the PLxC row) were used in
entries 226 and 229.

## Entry 236, with entry 235 section 4: the Tab S8 Ultra, and GroupLab back on the Fold 7

Nightly 110, then 111 when it was published during the sitting, installed on both over adb and started once. **The tablet** (SM-X900,
1848 by 2960 at 320 dpi, Android 16): the first run is a readable column; the sample scan analyzed as on the Fold 7 (25 shots, extreme
spread 2.16 cm) in about 58 s, peak PSS sampled at about 555 MB (the Fold 7, entry 232: about 50 s); landscape keeps the result;
Settings is fine; a resizable window at half and a third of the width works, the tabs clipped only when the window covered the
navigation bar. **Broken, fixed:** an activity recreated before the old one had gone (a cleared task, a window mode change) found the
single Shell still parented and the app stopped, "already has a visual parent"; `MainActivity.OnCreate` now lets go of it first, and the
activity also keeps through keyboard and navigation changes. Four clear-task restarts and a mode change on the tablet with the fixed
build: no stop. **For the plan:** a two-pane analysis on expanded widths (in landscape it is a phone-width column), and a progress line for
a long analysis. A stray tap during the check opened another app on the tablet's home screen at its permission screen; it was backed out
of untouched.

## Entry 229: copies of one design, the whole-sheet wrong-bull case, and spelling

**1. Three sheets, one printed code.** `GL-R0T0-384Z-HRBE-M0EW` is the design's identifier (GL-CF25-LTR-D), which every copy printed from
one PDF shares; the serial box holds the handwritten K, M and C. Checked: the desktop keeps each image as its own session keyed by its own
id with its own image hash, nothing is unique on the identifier, and the server gives every submission its own identifier and archives by
folder, so nothing merged or dropped a copy (`CopiesOfOneDesignTests`). Added: a label of the sheet's own in Load, kept in the marking
file and in the session's name, and, when the sessions already hold one from the same design, "This looks like another copy of a sheet
you have analyzed before" as it is opened. The handwritten letter is not read from the image.

**4. The 6.5 sheet.** Today's one-to-one matching gave 5 of its 23 found shots to the bull above their own, mean radius 0.591 in. The
existing impact-offset solver of entry 130 was only used when a person named the aimed bulls; now `ImpactOffsets.WholeSheet` runs it over
every scoring bull wherever a sheet has at most one shot a bull, and the assignment runs in the frame moved back by the offset only when
the solver is certain and the offset is over a tenth of an inch (so question 46's scan 5 is untouched). On the 6.5 sheet: certain, 0.76 in
high and 0.39 in left; every shot on its own bull; mean radius 0.213 in. The screen says so and offers "Give each shot to its nearest bull
instead". Detection and the session's re-matching use the same rule. The rebuilt scan is on the `test-data` release
(`load-sheet-6.5-wrong-bull-2026-09-26.png`, SHA-256 1f43bab7...) and `WholeSheetWrongBullTests` holds it. The two top row shots were not
found at all, inside the codes' and title's printed-matter zones: a detection defect still open. The wrong-bull article has the sheet
and the zeroing lesson.

**5.4 Spelling.** The check now covers the sheet generator's words too (`src/GroupLab.Cli/Library`): "metres" in the metric zeroing
sheets' descriptions, "neighbouring" in the designer's spacing warning and "analysed" in its marker refusal are fixed; the card and score
sheet were fixed with entry 226.

## Entry 228: several bulls and a scale at each on a target GroupLab did not print (Unholy's suggestion)

**Bulls by hand.** `MarkingSession.AddBull`, `MoveBull`, `DeleteBull` (numbering again) with shots nobody assigned going to their
nearest bull every time; the editor's **Place bulls** (B) taps, drags and deletes them, each bull and its shots in one of eight colors;
**Lasso** (O) holds the shots inside a drawn loop and puts them on the bull tapped next. Under Advanced, **Bull by bull** gives each
bull's shots, center and extreme spread beside the pooled group, which the existing pooling code measures as on a GroupLab sheet.
**Templates** (`BullTemplate`, kept in settings): the bulls in inches from the first; on the next sheet the first two bulls are tapped
and the rest placed by the similarity that carries the template onto them.

**A scale that is right everywhere.** Four corners (`RectangleReference`, with the paper's own size offered) already removed the angle.
New: `PerBullReference`, a length across and one up and down at each bull, each shot measured with its own bull's linear map, all bulls in
one frame taken from the first. It says when a bull's two directions or the bulls' scales differ by more than 3 percent (the photograph
was taken at an angle), when a bull has one length or none, and gives the relative uncertainty, which the figures carry ("a 2 percent
scale error is a 2 percent error in the group"). A single length on an image with camera fields is warned about. Saved in the marking
file. **Tests** (`OtherTargetsTests`, 13): a 3 by 3 target through known perspective transforms, where four corners recover every offset
within 0.002 in, a scale at each bull beats one scale for the sheet by more than three times, a quarter-turned photograph keeps one frame
(it did not at first: the up and down length's sign flipped between bulls on a pixel's tilt, 0.75 in wrong on two real photographs), the
angle is said, a template places the rest, and the file round trip; `Entry228Tests` the screen.

**Measured on real photographs** (entry 233's three load sheets, Fold 7, near straight down, taps placed through the marker registration
so only each method's own error shows): a scale at each bull 0.001 to 0.003 in rms from the markers, sigma within 0.1 percent; four
corners 0.007 to 0.010 in rms, sigma 1.1 to 1.3 percent small on all three (lens distortion at the paper's edge that a planar fit cannot
remove); one length 0.007 to 0.026 in rms, worst 0.087 in. In `photographing-targets`. Glossary: perspective correction extended,
per-bull scale new; the tour's marking page and the user guide credit Unholy (also TNA).

## Entry 232: the Play build driven on the Fold 7 over wireless debugging

`adb mdns services` found nothing with its default backend and found the Fold at once with `ADB_MDNS_OPENSCREEN=0`; `adb connect` then
worked without a new pairing. The Play copy is version code 110, installed by the Play Store. **Observed:** the first run had been answered;
targets were set to send automatically and were set to Ask me each time as the entry gives Alan's choice; the phone has no benchmark on 110;
cold start 0.52 s (`am start -W`); the published sample, pushed to Pictures and chosen in the picker, gave 25 shots, extreme spread 2.16 cm,
mean radius 0.59 cm, about 50 s from choosing to the result, twice; a quarter turn kept the result in the same process; the log has no
crash and no ANR. **Fixed:** units followed no region because the phone runs with invariant globalization (`AppSettingsStore.RegionSource`,
set from `Java.Util.Locale` by the phone, `Entry232Tests`); the phone's informational version was the project default because the
nightly's Android publish never passed `-p:Version`. **For the plan:** a Targets screen on the phone, a progress line for a long analysis.
Screenshots and the log in `C:\Dev\grouplab-local\android-play-110\`.

## Entry 227: the survey offers the benchmark, and CEP 99 with a percent of one's own

**1.** Done with entry 226: the grid spans plus or minus 1.0 mil at 100 yd with 0.25 mil squares, bold half mils and heaviest whole mils,
labels and lines sized as question 59 records, the scale and a ruler printed.

**2. The benchmark.** Answered in request 40, from the code and Alan's own log: in nightly 110 Yes only switched the survey on; the
benchmark was a button under the question, which closed with it, and his log of 2026-09-27 05:18 UTC shows the Yes and a first report
with no benchmark in it. Now the question says before it is answered that Yes does not run the benchmark; Yes asks "Run the benchmark
now?" with Run it now and Later instead of closing; a run shows the stage it has reached, a bar and the seconds so far, can be canceled
(keeping nothing), and ends saying what it found and when it finished. Settings, under Sharing, says when it last ran and what it found,
or that it has not run, with Run the benchmark now. `BenchmarkPanel` is one control the desktop and the phone both use; the phone had no
benchmark at all and now has the same first run question and Settings section. `Benchmark.Run` reports each stage as it ends;
`SaveBenchmark` keeps when it ran. Tests: `Entry227Tests` (Later closes with nothing run, Run it now shows its progress then its result
and time, cancel keeps nothing, Settings says the last run), `Entry208Tests` moved to the Settings panel.

**3. CEP 99 and a percent of one's own.** CEP 99 is a toggle beside CEP 50, 90 and 95, drawn in the same green in dashes and dots, keyed
and explained on hover, listed with the figures with its 95 percent range; under Advanced, "A circle for any percent" takes 1 to 99.9,
draws it in long dashes, lists it, and is remembered; both go to the report and the Compare screen. Both come from the same sigma and
model as the other CEPs (`GroupAnalysis.Cep`), and where n(1 - p) is under one shot, the figure says the circle is the model's tail
rather than something the shots show, and how many shots would put one outside it (`CepTailNote`). `Cep99Tests` checks CEP 99, 97.5, 50
and 99.9 against 40,000 circular normal shots (within 2 percent, and the share inside within half a percent), the range against sigma's,
and the note; `Entry227CepTests` the screen. The glossary's CEP covers 99, and the CEP article gains "CEP 99 from a few shots": from 10
shots its range runs 0.76 to 1.48 times the value, and 100 shots are needed to see even one outside it.

## Entry 226: the zeroing grids redrawn, the suppressor test, the aim point results, a target generator and large sheets

**1. The zeroing grids.** Alan's printout measured right (entry 230: 0.36, 1.80 and 5.76 in), so the design was the fault: 1.6 mil across,
0.1 mil squares that ran together at 6x to 10x, 0.2 to 0.4 mm lines and 1.4 mm labels, and nothing saying what a square was. The format
could not draw the fix, so it gained grid style 2 (TARGET-SCHEMA.md section 3.13, question 59): a drawn field of its own inside the
lattice, a third line weight for the whole unit, and everything else fixed by the style in `GridStyle2`: strokes of 0.6, 2 and 3 mm, 13 mm
labels centred on every heavier line with the line broken behind them, and a three-line scale statement with a 4 in or 10 cm ruler above
the field. The four sheets now have two codes at the top and a field 914 dmm each side: **plus or minus 1.0 mil at 100 yd exactly**, 0.91
mil at 100 m, 3.44 MOA at 100 yd and 3.14 MOA at 100 m, in 0.25 mil or 0.5 MOA squares, the whole unit heaviest. The mil lattice is 1.25 mil
over 5 divisions because 1 mil over 4 rounds 914.4 to 914 and puts the first line 0.6 dmm out; the worst line is now 0.44 dmm (test 36).
The aim is an open 20 mm ring with nothing in its middle. The old four are frozen in `targets/frozen/zero-grid-1/` and still identified,
on Android too, which did not ship frozen definitions until now. Two findings on the way: 1 mm fine lines merged a touching pair of .308
holes on a crossing (`TightGroupTests`), so they are 0.6 mm; and a label beside its line collided at the field's edge, so labels sit on
their lines. The corpus counts were re-run: 55 of 55 committed images unchanged, only the eight zeroing fingerprints moved.

**2. The suppressor test** (`website/research/suppressor-shift`, published). Both 6 ARC sheets read cleanly: 32 and 34 of 34 markers,
registration RMS 0.0022 and 0.0025 in, 25 holes each, every off-bull shot given to the bull it was fired at (Dominus bulls 2 and 24,
Magnus bulls 5 and 24). The Magnus S shots centered **0.28 in lower** (interval 0.06 to 0.51) and 0.04 in right: Hotelling's T-squared 6.55,
F(2, 47) 3.21, **p = 0.049**, permutation p = 0.047; without the four off-bull shots p = 0.009 for the same shift. Spread unchanged, ratio
1.02 (0.76 to 1.35). The article names the order, the heat and fouling, the remounting and the light as confounds next to the result.
Magnus bull 5's shot is up and slightly left, not right as entry 229 read it. **The 6.5 sheet is not pooled** with the 2026-09-20 6.5
sheets: its block says GM205MAR and 2.873 in, theirs 7.5 BR and 2.874 in. **Defects on real sheets:** on the 6.5 sheet 23 of 25 holes
found (2 hole-sized candidates in printed-matter zones, row 1's shots by the codes), and 5 of the 23 given to the bull above their own
(entry 229 section 4). Scans in `C:\Dev\grouplab-originals\range-2026-09-26\`, recorded in `samples/PROVENANCE.md`, backer OSB.

**3. The aim point results** (`can-you-see-the-bull`, now `ready`). Both score sheets transcribed to `data/scores-2026-09-26.csv`, 207
scores, and Alan's groups to `data/groups-2026-09-26.csv`. At 10x the current bull scored 0 through all three high power scopes; C, E and
I, whose centers are 3.4 arcminutes or more, were centered; at 4x only I. The Razor HD's crosshair covered D and G. No design grouped
measurably better: three shots a design need a spread ratio of about 3.1 to show, and the largest was 2.2 (C against E, p = 0.16). **The
repeat on the score sheet** came from hand-typed rows: the PLxC's 8x/100 and 8x/50 differed only in a same-weight Dist cell, and the card's
own page 2 printed the high power scopes' 10x, 18x and "max" on the friend's rows. `scopes.py` now builds the rows from each scope's range,
refuses an impossible or repeated magnification, and heads a change of distance in bold; the card's page 2 is the same sheet;
`tests/python/aim-card-tests.py` in CI holds it. "centre" and "favourite" are gone from both. The ring set choice is question 61.

**4. The target generator** (`TargetGenerator`, the Targets screen's "Made for your optic"). Distance, lowest magnification or a red dot's
size, and shots give a black disc with a white center of 3.5 arcminutes at that magnification (or 1.5 times the dot), three times as wide,
1.35 diameters apart, in the largest 2 by 2 to 5 by 6 grid the page registers with either marker scheme, and as many sheets as the shots
need, a set printed as a tiled assembly so every sheet's codes carry its place. 25 shots at 10x and 100 yd is one Letter sheet; at 4x it is
a set; a 2 MOA dot at 50 yd fits no Letter page and says what would. `TargetGeneratorTests`, `TargetGeneratorScreenTests`.

**5. Large sheets.** `PhotographLimit`: photographed whole with the sheet filling 90 percent of the frame, a 12 MP phone gives Tabloid 212
and A3 218 pixels an inch, enough; the 24, 36 and 42 in rolls 112, 100 and 86, too few, where a 50 MP phone at full resolution gives 230,
204 and 175. The Targets screen says so for each large sheet and that tiled pages are the better choice. `CutSheet` prints a tiled target on
one large page with dashed cut lines, each piece a whole sheet with its markers and codes (`LargeFormatTests`).

## Entries 224 and 225: backups reach GitHub, the Store is built, the whole server can be restored

**Backups** (224 section 1.1). `grouplab-backups` was empty, and GitHub makes no release in a repository with no commit, so the first run
failed and its two reports became issues 4 and 5; the backup now gives an empty repository a README as its first commit. Tonight's backup is
in the repository and the restore test passed against it. Issues 4 and 5 are closed with the fix.

**The archive worker** (1.2). The token script's closing words named the error worker, copied from its script; they now name the archive
worker, and a test holds that. With nothing waiting, each run now checks that its token reaches the archive: on the server it reads
`"token": "ok"` and `"archive": "reachable"`.

**The signed Android build** (1.4). Nightly 109 built and signed the APK and AAB with Alan's key, and the release carried neither: both
upload lists name their files, and the Android ones were not named. They are now, with versioned names on the numbered release and plain
ones on the rolling release. Request 36 names the AAB once a nightly carries it.

**Windows signing** (2): on hold, request 37 closed as not yet; the comparison stays in `docs/RELEASE-PLAN.md`.

**The Microsoft Store** (3). `scripts/package-msix.ps1` makes an MSIX of the self-contained build, stamped as the Store's copy
(`GroupLabDistribution=store`), in which GroupLab's own updater is off and Settings says the Store keeps it up to date (`StoreBuildTests`).
The manifest's floor is Windows 10 version 1809, the Store's, now in the minimums table. CI builds and checks the package on every push with
a stand-in identity. `release.yml` builds it with Partner Center's identity from repository variables, attaches it to the release, and sends
each tagged release to the Store with Microsoft's own tooling (`microsoft/microsoft-store-apppublisher`, `msstore publish`). **The split**:
stable releases to the public listing, nightlies never; a beta flight only if a beta train is ever made. The first submission is by hand,
because the automatic path needs the app already live. The listing, ready to paste, is `docs/store/LISTING.md`; Alan's part is request 38.

**The whole server** (224 section 1.3, 225). `docs/RESTORE.md` has the restore from an Oracle boot volume backup, what is lost, what to check,
and that the backups are crash consistent; the weekly line says the Oracle backups are checked in the console, not by the report. The policy is
on; the first backup is due 2026-09-26 09:00 UTC, and until Alan confirms it, sudo stays limited to GroupLab's own files.

## Entry 223: request 34 done, and the survey opened

Request 34 is closed: Alan's reload and checks passed, and the `503` he saw the second time was the receiver's own "closed". **The survey
is open**: the worker is installed and tested, the desktop's question is tested in `Entry208Tests`, and the phone asks it the same way.
`surveyOpen` in limits.json and `OPEN` in the receiver are true together, as the site's build requires; the receiver test that proves a
closed receiver refuses now makes its own closed copy. The article what-grouplab-sends lists the survey as the fifth thing that can leave
a computer, and says that nothing goes when the program opens except what the person chose to have sent by itself. The tour's and the
user guide's Settings now describe one Sharing section with its three parts; the tour also said error reports were not switched on yet,
which had been untrue since entry 200.

## Entry 222: as much automation as possible, backups first

**Section 6, first.** `docs/RESTORE.md` lists everything Code or the planning session can change, its backup, how often, where, and how to
restore it, and CLAUDE.md now opens with the rule. **The gap it names**: the server as a whole has no copy off the machine. HestiaCP keeps one
backup a user, on the server itself (`BACKUP_SYSTEM='local'`), made daily; the newest is 2026-09-25. Until Oracle's boot volume backups are
on (request 35 step 3), Code's sudo is limited to GroupLab's own files and its installer, so it does not reload nginx, which pissinhot.com
shares; request 34's reload stays Alan's. `ubuntu`'s sudo is already passwordless (`NOPASSWD: ALL`), so no sudoers line is needed.

**Section 2.** `grouplab-archive-worker.py` archives each checked submission, proves it by download and SHA-256, rewrites the manifest and
only then deletes the folder; installed by Code with `install.py --archive` and running every ten minutes, waiting for the token (request
35 step 2). systemd would not start it without a credential file, so the installer leaves an empty root-only one. 14 checks against a
stand-in GitHub, in CI. **The backlog**: Code ran the fixed pull under Windows PowerShell 5.1, and the 9 on grouplab.org and 18 in the old
pissinhot.com folder are archived, proven and off the server; the archive check restored all 27 under both shells. Two more 5.1 faults
were fixed on the way: `Get-FileHash` honored `-WhatIf`, and the check read a JSON array as one object and found no months.

**Section 3.** `scripts/backup.py`, nightly at 03:30: the archive's submissions copied here and checked against the manifest, then git
bundles of this repository and `grouplab-testdata`, `local.zip` of the local-only files, `grouplab-local` and `grouplab-originals`, and
the crash reports, 433 MB tonight with a manifest of every file's SHA-256; kept 7 daily, 4 weekly and 6 monthly in `grouplab-backups`,
or on this computer until that repository exists (request 35 step 1). The weekly restore test checks every file and clones the bundle;
it passed today. A failure is sent as an error report. **Encryption** (section 3.6): not added. The repository is private, as the archive
is; encrypting would put a passphrase in Alan's hands whose loss makes every backup useless, which is a worse failure than the one it
guards against for this data. If ever wanted, a 7-Zip AES archive with the passphrase in his password manager, at no other cost.

**Section 4.** `scripts/cleanup.py`, weekly: only what rebuilds itself is deleted directly (it cleared 5.3 GB of old numbered build
folders today); anything else goes to `C:\Dev\grouplab-trash\<date>\`, emptied after fourteen days and never before a backup since.
`scripts/server-week.py` reads the workers' week and the server's own backup, read-only, into the weekly line and STORAGE.md. The weekly
line sits under the open count in for-alan.md, written by `scripts/automation-report.py`. Two scheduled tasks run all of it as Alan
while he is logged on, with no password: registered, and the weekly one run once, result 0.

## Entry 220: request 31's pull stopped at the archive; fixed, and tested under both shells

**Nothing was removed from the server.** In `Get-TargetSubmissions.ps1`'s last loop each folder is archived (`Add-ToArchive`) before
`sudo rm` runs for it, and the very first `Add-ToArchive` threw, so the script ended before any removal. The 9 are on the server and here.

**The cause** was Windows PowerShell 5.1 turning `gh release view`'s "release not found", on stderr for a month with no release yet, into a
terminating error under `$ErrorActionPreference = 'Stop'`, `2>$null` notwithstanding. `scripts/NativeCommand.ps1`'s `Invoke-Native` now
runs every program in `SubmissionArchive.ps1`, `Get-TargetSubmissions.ps1`, `Remove-ReadSubmissions.ps1` and `Test-SubmissionsArchive.ps1`
(gh, ssh, cmd, tar and the ledger's python): stderr collected, never fatal, the exit code alone deciding. **Tested for the case itself**:
`tests/powershell/archive-tests.ps1` runs the archive against a stand-in gh (`fake-gh.py`) that says "release not found" on stderr as the
real one does; nine checks pass under Windows PowerShell 5.1.26100 and PowerShell 7.6 on this machine, the old script fails it with
Alan's exact error, and CI now runs it under both shells on Windows. `SubmissionScriptsTests` fails if a raw program call comes back.

**Dry runs**: with `-WhatIf` the ledger only prints (`storage-ledger.py --check`), and the pull says "would archive and then remove N".

**Actions artifacts**: both figures were true. Entries 215 to 217 freed the ones older than three days; at about forty pushes a day, each
leaving about 400 MB of Windows packages, three days of new ones came to 81.7 GB in 954. CI's package artifacts now keep a day (they were
three), the ledger frees anything over a day old (it was three), and this run freed 44 GB; 38.1 GB remains, all under a day old.

**The crash reports repository** has three issues. Number 1 is the closed test report. Numbers 2 and 3 arrived at 10:34 UTC from Alan's
machine, from nightlies 35 and 31, about a week old: each closed at start because a file the installation should hold was missing, the
Fluent theme library in one and SQLite's native library in the other. They read as damaged installs of those builds, sent now by the newer
build's queue; current builds do not start that way, and nothing in the code is at fault. Left open, as entry 194 section 4 asks.

**Entry 221**: the planning session's renamed lock file was empty and has been deleted.

## Entry 219, item A5: sharing a session file by hand

`GroupLab.Core.Records.SessionPackage` writes and reads a `.grouplab` file: `session.json` (schema, revision, the writing device as
"GroupLab version on the system or phone model", never a machine's own name), the desktop's marking file with no path from the writer's
machine in it, the sheet, and the picture. `CleanImage.From` re-encodes the picture from its stored pixels, so a photograph's EXIF, GPS,
XMP and comments stay behind; a test writes a location into a real JPEG and finds none of it in the file. Reading a stranger's file
takes only the four known names, each within its size however small it claims to be. The desktop's menu shares and opens one, the
phone's result shares through the share sheet and Sessions opens one; either way it becomes a session of its own, saved in the
records. `SessionPackageTests` (five) and `SessionFileTests`. On a phone: request 33, step 7.

## Entry 219, item A4: capture to result on the phone, corrected by touch

Take a picture or choose one; the working copy (8 MP) becomes the session's image, which settles the question item A1 left of storing a
scale beside the marking: there is none to store. The sheet is named, the holes found, the figures and the photograph with its holes
shown, and the desktop's composite plot drawn; the session saved in the desktop's database, and listed under Sessions. Three pieces
of the desktop moved into shared code on the way, each now called by both: the plot fills itself from a marking
(`CompositePlot.Show`), a session record is built in Core (`SessionRecords.Build`), and the survey's question is on the phone's first
run and in its Settings. Built here and in CI; **not yet on a phone**, which is the next sitting's (request 33 gains two steps).
**Second part**: correcting by touch (move with a magnifier, add, remove, undo, each saved at once), the caliber and distance asked
before the picture and remembered, angles from the distance, and the sheet chosen by name where its codes cannot be read. The caliber
and distance are kept in the desktop's settings file (`LoadShotSetup`), which `AndroidSharingTests` checks. A4 is built; its
measurement on a phone is request 33's step 6.

## Entry 219, item D1: the hardware survey and benchmark, built and switched off

**The benchmark** (`GroupLab.Core.Survey.Benchmark`): GL-CF25-LTR at 300 dpi with one hole in each of its 25 bulls, the same pixels on
every machine (a test compares their hash), analyzed as a photograph is; it finds all 25 here. **The report** (`SurveyReport`) holds
exactly docs/SURVEY.md section 2's list: `Keys` names every field and a test walks a built report against it, and another checks that
the user name, machine name and home folder never appear. A random installation number, replaceable in Settings.

**The desktop**: the survey is the third question on the first run screen, after targets and error reports, with the benchmark offered
under it and nothing chosen; somebody who answered the other two before sees the screen once more with only the survey and a line
saying their answers are kept. Settings gathers all three under one section, **Sharing**. Once the person says yes, each analysis's
sizes, times and memory are kept in a small file beside the settings, and a report goes at most weekly, or when a benchmark is
waiting; saying no deletes that file. The queue (`SurveyQueue`) is shared with the Android application. `Entry208Tests`, seven tests,
and `Entry203Tests` now covers the survey's words at every width.

**The server**: `website/api/survey.php` rebuilds a report from its named fields, stores a salted hash of the installation number and the
day only, and limits an installation to three reports a day; `grouplab-survey-worker.py`, with no network, counts each report into
classes and deletes it, and writes the published totals with every group under ten merged into "other". Receiver tests in
`tests/php/receiver-tests.php`, run by CI; `tests/python/survey-worker-tests.py`, ten checks. `install.py --survey`.

**Switched off**: `surveyOpen` is false in limits.json until request 34 installs the worker. The receiver answered on the live site as
soon as it was published, before any nginx change, so it now refuses everything while `surveyOpen` is false, and the site's build
holds its `OPEN` to that; nothing is stored that no worker would delete. **The release notes** now take a heading
written into a note off its start: nightly 107 carried "Under the hood: the rules ..." under Under the hood. The first try at nightly
107, on 6c976e3, failed creating its release with a 403 from GitHub with nothing changed in the workflow or the repository's settings;
the next, on 27bd109, published.

## Entry 219, item A3: the application project, org.grouplab.app

`android/GroupLab.Android` builds here and in the `android` workflow, which now uploads `grouplab-apk` beside the spike's (the spike stays
until item A4, because request 33 installs it). Navigation along the bottom, the first run's questions, Settings under Sharing, the log,
crash records and the error report queue, all through the desktop's own files compiled as they are. To share them, the desktop's
literals moved into `SharingWords` and its error sending into `ErrorQueue`, which the main window now calls; the desktop behaves as
before. Two things in the shared files only the Android build could see: a settings default named through the main window's own enum,
now compiled only on the desktop, and a comment naming a list by an old name. `AndroidSharingTests`, three tests. **Not yet on a phone**: no device was
attached; it goes on at the next sitting with request 33's.

## Entry 219, item A2: the capture screen spike, built; measured in request 33

**In Core, tested on the desktop**: `CaptureGuidance.Judge` gives docs/MOBILE-CAPTURE.md's one instruction in item C3's order from the
outline and the frame's quality, and `Ready` only when every condition holds (item C1); a condition holds at half its quality part,
the angle within the limit. `CaptureGuidance.JudgeFrame` judges a whole frame the way the desktop judges a photograph: outline, the
sheet's markers, angle and quality. `CaptureScreenTests` are the two tests the document named as still to write, and a whole frame:
a Letter sheet on a dark board is ready, the same sheet edge to edge is told to move back. A render's pure white paper read as blown
out, which it would be in a photograph too, so the test's paper is 225.

**On the phone**: `CameraSession` binds CameraX (1.6.2 bindings) to the activity: a preview on the camera's surface hosted inside the
Avalonia screen, a still at the largest size in maximum quality, and an analysis stream. The sheet is named from its codes on the
stream, then each frame is judged; three ready frames in a row fire the shutter. The lens is chosen by zoom, 0.6x, 1x and 3x; a tap
locks focus and exposure there. The still is analyzed at 8 MP. Every line goes to `spike-log.txt` on the phone. Built here; the
`android` workflow now builds Release, so Alan can install its APK himself. **Not measured yet**: request 33, one sitting.

## Entry 219, item A1: the working resolution in Core

**`WorkingSize`** (Core): 8 megapixels for the phone, always, from entry 209's Fold 7 measurements; 60 for the desktop, only for images
far larger than a Letter or A4 sheet. `ImageLoader.Load` and `LoadMaxChannel` take the limit, decode a JPEG reduced by a power of two
where that does not undershoot, then resample by area to exactly the working size, and scale the resolution with it. `grouplab analyze
--working-megapixels` uses it. `WorkingSizeTests`: the sizes, and the sample read at 8 MP with both channels alike and its resolution
scaled.

**The accuracy cost, full size against 8 MP**, measured with `grouplab analyze` on 2026-09-25. The sample: 25 of 25 shots on the same
bulls, a mean shift of 1.2 thousandths of an inch, 4.2 at most; mean radius 0.232 and sigma 0.185 in, unchanged. Two range photographs,
chosen as the first of the 59 whose full size result is plausible (four have a mean radius under 0.5 in; the others are misread at any
size, which is the photograph detector's known weakness, not the working size): 15 of 15 shots on the same bulls, mean shift 3.6
thousandths, 13.3 at most, mean radius 0.232 to 0.233; and 15 of 16, mean shift 7.9, 38.4 at most, mean radius 0.249 to 0.250.

**Not done**: the desktop application does not bring very large images down yet; the saved session would have to record the working
scale, and it waits for the roll sheet work that needs it. **Next on the roadmap**: A2, the CameraX capture spike.

## Entries 215 to 218: nothing kept on the server, a private archive, and a ledger of GitHub

**The pull** (`scripts/Get-TargetSubmissions.ps1`) now, for every submission still on the server whose copy here verifies, backlog
included: zips it into the private archive's release for its month (`scripts/SubmissionArchive.ps1`), downloads it back and compares
the SHA-256, rewrites that month's `manifest.json` (name, size, SHA-256, consent level), and only then removes the folder from the
server with `sudo rm`, one line a folder. A folder that does not verify, or that the archive does not prove, stays and is listed.
`-KeepOnServer` removes nothing; `-NoArchive` removes after the local check alone. Tested end to end against the real archive with a
synthetic submission dated 1999, uploaded, proven, recognised on a second run, and the release deleted. `SubmissionScriptsTests` holds
the order. `scripts/Test-SubmissionsArchive.ps1` restores and checks everything in the archive; on the empty archive it reports none.

**On the server** the workers now bound every folder: intake refused 7 days and ready 60 (quarantine by attempts, entry 176); error
reports unsent 30 days and set aside 7. `tests/python/error-worker-tests.py` has three new checks (23 in all); `WorkerLimitTests`
holds the numbers. The upload page no longer says submissions wait until read: it says when they leave, where the copies are, and links
to "Where it is kept, and for how long" in `what-grouplab-sends`, the one place the rules are written.

**The ledger** (`scripts/storage-ledger.py`, `docs/notes/STORAGE.md`, budgets in `docs/notes/storage-budgets.json`): GitHub held about
152 GB for the project, of which 140 GB was Actions artifacts, 1,990 of them, mostly per-push Windows and Linux packages kept 30 days.
The packaging workflow's now keep one day, which is all the nightly needs, and CI's three; `--free` deleted about 65 GB older than
three days, oldest first. About 75 GB remains, all from the last three days' pushes and uploaded under the old thirty day retention, so
it stays over budget until later runs of the ledger free it as it ages; new uploads expire in one to three days by themselves. The releases hold 11.6 GB against 20; the archive is empty; the rest is small.

**Entry 218**: the archive confirmed private with Alan's login (`visibility` PRIVATE), and its README replaced with one saying what it
is, that it is never made public, and that the ledger tracks it.

**Waiting on Alan**: request 31, the workers installed and one pull on each server's folder, which clears the backlog; request 32, a
backup of the local copy, his decision.

## Entries 213 and 214: the key off the plot, the rings further back

**The key** (`CompositePlot.KeyLayout`): beside the plot or below it, whichever leaves the plot the larger square, and a small Key button
in a strip of its own where neither leaves 240 units; the button opens the key over the plot only when pressed. Everything is drawn and
clipped in `DataRect`. The first version preferred beside, and on the sample the 410 wide key left the plot a 250 wide strip, so the
larger square decides. `Entry213Tests`: the key and the plot's area never meet, and every shot and the center are inside the area, at
1400 by 900 and at 1060 by 700, and a 320 by 300 plot collapses to the button. The report's key was already the caption under its
square. The toggles along the plot's bottom edge still sit over it, as they have since entry 109; they were not part of this entry.

**The rings** (entry 214): `#282828` on the dark paper, half entry 210's lightness, and `#bdbdbd` on white, moved toward the paper for
the same relationship, since toward black would have made them heavier than the outlines. `Entry210Tests.TheRingsSitBehindTheOutlinesAndStillShow`
holds both themes: fainter against the paper than the half strength outlines, apart from them in tone, and at least 1.2 to 1.

**The caliber in the pictures**: 0.308 was named by the figure test only to draw outlines; the application never read it. The sample's
load block says 6.5 Creedmoor (`samples/sample.json`), and the pictures now use 0.264 in. The 6.5 Creedmoor line in PROVENANCE.md that
entry 213 read belongs to Unholy's 2026-09-23 scan. Pictures: `docs/figures/composite-plot-210-{group,whole}-{light,dark}.png`, replaced.

## Entries 211 and 212: request 29, and the older phones held back

**Request 29**: upside down turned on both of the Fold 7's screens, and the folder picker offered Google Drive, where a folder could be
chosen; OneDrive was not seen. Stage B of the sync plan is possible through Drive; its reliability is for later.

**The older phones** are the reference devices, used only at named milestones (entry 212): once before the first Play closed testing
release, and for a problem the Fold 7 and the emulator cannot show. The Essential PH-1 was already connected; its model, Android 10 (API
29), Snapdragon 835 and 4 GB were read from it and nothing else was done. The Galaxy S20 5G is Android 13 with 8 GB; a OnePlus is to come.
The working size stays the Fold 7's 8 MP. `docs/ANDROID.md`, "The phones it is tested on".

**Also this run: a nightly lost behind a notes commit.** The notes commit for these entries was pushed while entry 210's build ran, and
the nightly for entry 210 stood down because main had moved on, trusting the newer push to bring its own; a notes commit brings none. The
nightly now publishes anyway when every newer commit is a notes or screenshot commit and none touches a workflow, which is the rule that
made it stand down; `WebsiteWorkflowTests` holds both conditions.

## Entry 210: the composite plot's rings, and the whole target

**The rings** are drawn `CompositePlot.BullRingInches` wide, 0.05 in on the page, so they stay in proportion as the view zooms: about 19
pixels on the sample's group view against entry 204's 4, held between 4 and 40 pixels. They are a solid mid grey, `#a0a0a0` on white and
`#505050` on the dark paper, darker than before and 60 to 75 apart (summed channels) from the half strength outlines, which stay thin.
Everything else is drawn over them; on the sample, holes sitting on a ring read as black dots on the grey.

**Framing**: Group, the group alone as before, or Whole target, every ring with the group inside, beside the plot and remembered
(`AppSettingsStore.LoadPlotWholeTarget`); Compare has the same choice. **Zoom and pan**: the wheel zooms about the pointer, a touchpad's
pinch and a touch pinch zoom, a drag that starts on empty paper pans while one that starts on a shot still picks it, and a double click
or tap returns to the fitted view; everything is drawn as vectors, so it stays crisp. The report keeps the whole target, as it always
spanned the rings or the shots, because its page has no clipping to cut a ring at a group framing; its caption says so.

**Tests**, `Entry210Tests`: the ring tone darker than entry 204's and apart from the outlines in both themes; the ring width at least
three times 4 and clearly wider than an outline; the group view holding every shot; the whole target holding every ring's edge and every
shot; the choice remembered; zoom about a point keeping it still and the reset restoring the scale. Entry 204's layer test now holds the
rings below every mark instead of faint.

**Pictures**, the sample in both framings and themes: `docs/figures/composite-plot-210-{group,whole}-{light,dark}.png`; the before
pictures are entry 204's `composite-plot-after-{light,dark}.png`. In the whole target view the key sits over the top left shot, as it
always has at that corner.

**The log split, fixed on the way.** NOTES-FROM-PLANNING.md had grown back to 293 KB, and `scripts/split-logs.py` run a second time
overwrote each archive file with only the entries being moved: the September notes archive lost about 8,400 lines in the working copy,
and the results' "newest" were taken by position in a file no longer in order, so entries 186 to 210 were archived and 154 to 185 kept.
Nothing was committed. The logs were restored from the last commit plus entry 210, and the script now merges into what each archive
holds, chooses what stays live by entry number, keeps a result's unnumbered parts with it, rebuilds each index from everything archived,
and stops before writing if any block would be lost. Checked independently: of 16,475 non-blank lines before, none is missing after
except four index and header lines rewritten with their new counts. `DiscordLinkTests` now exempts the notes archive as it did the live
notes, as CLAUDE.md asks of a test that reads a log.

## Entry 209: the Fold 7 in one sitting

**Resolution against memory and time**, the sample at 600, 400, 367 (12 MP), 300 (8 MP) and 200 dpi, each run in a fresh process on a
file already at that size: 16.1, 6.6, 5.1, 3.3 and 1.6 s; 715, 461, 427, 373 and 307 MB; 25 of 25 holes at every size, with a mean shift
from the full resolution run of 1.6, 1.2, 2.5 and 2.9 thousandths of an inch and at most 11.8. The first attempt shrank the image after
loading it at full size and every size peaked near 530 MB, which is why the real application decodes at the working size. The spike at
rest holds about 274 MB. Full table in `docs/ANDROID.md` section 5.

**The cameras**, read through Camera2 with the camera permission granted over adb and no camera opened: a 22 mm equivalent main camera
built from physical cameras 2 (ultrawide, 14 mm), 5 (wide, 22 mm) and 6 (telephoto, 66 mm); largest ordinary stills 12.5, 12.0, 12.5 and
10.0 MP; every one reports intrinsics and distortion. `docs/ANDROID.md` section 4.

**Built**: `SpikeScaled`, `SpikeCameras`, a launch extra naming a task to run unattended, and a Choose a folder button that logs only the
provider. The spike's minimum is Android 10, entry 207's.

**Request 29**: upside down on both screens and one look at the folder picker; then the phone is no longer needed.

## Entry 205: the Fold 7 passed folding; all four ways up; the start up lines explained

**Request 27, Alan's hands**: folding, unfolding and turning kept every line and rearranged the panels as designed; the largest font size
cut nothing; his run of the sample took 18.9 s at 716 MB. Avalonia is confirmed for the phone.

**Upside down portrait** did not turn, because Android leaves reverse portrait out unless the activity asks. The spike's activity now asks
for `ScreenOrientation.FullUser`, all four directions while honoring the rotation lock, which `FullSensor` would ignore. Checked over adb:
rotation locked at 180 degrees with the spike in front turned the display to 180, `dumpsys` gives the activity's requested orientation as
`SCREEN_ORIENTATION_FULL_USER`, and the phone's own settings (rotation following the sensor, at 0) were put back straight after.
`docs/MOBILE-CAPTURE.md` gains item C5: the photograph stored upright and the overlays turning, whichever way the device is held.

**The repeated start up.** The spike now logs each create and destroy of its activity and each time its view is shown, with counts. Back
finishes the activity with the process still alive, and opening it again creates a second one ("the 2 time in this process"); Avalonia's
single view is the application's, so the list carried over. Going home and back, and opening recents, log nothing. The provisional sizes
(1 by 1, and the full size at 1 pixel a dp) are logged as ignored and not laid out.

**Each image's own peak memory**, in a fresh process with pushed photographs run before the sample: photograph 635 MB, scan 721 MB.

## Entry 204: the composite plot, quieter

**What changed**, back to front as it is drawn: the bull's rings are a wide pale grey band (4 wide; 1.5:1 on white, 1.7:1 on the dark
paper); each shot's outline is at half strength, and so is its point when no caliber is set, while a picked or excluded shot keeps its
own look; CEP 50, 90 and 95 are green, 2.5 wide against the outlines' 1.5, dotted, solid and dashed; the extreme spread stays a red dashed
line; the group centre is a pair of green lines across the whole plot and the aim point a pair of blue ones; a picked shot is drawn last.
The key lists only what is drawn and names each mark by colour and pattern. Toggles beside the plot turn CEP 50, 90, 95 and the extreme
spread on and off, 44 high and reached with Tab, CEP 95 off at first, and `AppSettingsStore.LoadPlotMarks` remembers them. Compare's small
plots follow the same toggles and its caption says what they show. The report draws the same marks in the light theme's inks; its page
has no dashed stroke, so CEP 50 is a ring of dots and CEP 95 a ring of dashes made of dots, and its caption names each and says when the
extreme spread was left out. The glossary's CEP entry covers 95.

**The colours**, in the palette both themes read (`Tokens.Plot`): green `#007a4d` light and `#3ddc84` dark, blue `#0055d4` and
`#5aa9ff`, each at least 5.4:1 on its paper. **Deuteranopia**, simulated with Machado's 2009 matrix: the red and the green come close
(46 and 60 apart on a 0 to 441 scale, light and dark) and are told apart by shape, a dashed line between two shots against circles and
lines across the whole plot; the blue stays at least 150 from both and from the ink.

**Tests**, `Entry204Tests`: the stroke widths, opacity and contrasts in order; the defaults, each toggle adding and removing exactly its
key entry, the choice remembered, and the report caption following it; and a render in which the aim point's row and column read blue and
the group centre's green at the plot's edges. `ThemeTests` now allows the one half strength the entry asks for and holds the new inks to
4.5:1; `Entry105Tests` reads the new key. The figures are drawn from the published sample scan with .308 named, which the sample does not
record, so that the outlines show.

## Entry 203: the consent choices wrap; the analysis screen at narrow windows

**The fault.** A radio button given a plain string shows it on one line, so on nightly 102's first run screen both consent choices ran
off the card mid sentence. Settings' own consent radios already wrapped; the first run card's, the question after Accept and analyze's,
the Sending targets and Error reports choices, and three check boxes did not. Each now takes `MainWindow.Wrapped(words)`, a text block
that wraps, and `MainWindow.WordsOf` reads a button's words either way, so `PressSend` and the Settings text lists read the text block.
The consent wording is unchanged.

**The test**, `Entry203Tests`, looks at every visible text block under the first run card, the question and Settings, including the
ones a radio's template draws, and fails on a line wider than its space or a block past the edge of anything that clips it. It runs at
1400, 960 and 683 units wide: Avalonia lays out in device independent units, so 150 and 200 percent display scale are a narrower window
in them. With the old first run card it fails at 1400 on the testing only line. It passes now at every width for the first run card and
Settings.

**Found on the way: the analysis screen needs about 1060 units.** Its three columns are 300, at least 320 and 372 wide, fixed, and do
not shrink, so at 960 (a 1920 pixel screen at 200 percent) the right column runs 97 past the window, sending question and figures with
it. That is the look of the main screen, so it is question 58; the question's test runs at 1400 and 1060 until it is answered.

**Nothing is chosen for the person**: neither consent level is selected on the first run card, the question, or in Settings while none
has been chosen, and `NeitherConsentLevelIsChosenForThePerson` holds it. The selected level in Alan's screenshot was his own click.

## Entries 201 and 202: the Android SDK installed; detection runs on the Fold 7

**Entry 201.** Request 25 done on the retry; the likelier cause of the first failure was running the SDK step before the workload install
had finished, not the NuGet sources entry 200 suspected. Request 25 is closed and says to wait for the install.

**Entry 202: the spike on the phone.** The Fold 7 (SM-F966U1) was paired by Alan and driven over `adb` from here. What the phone showed,
in order:

1. **A debug APK does not start by itself**: it expects Visual Studio's fast deployment and aborts with "No assemblies found". The
   spike is measured as a Release build, signed with the debug key, which also makes the times comparable with the desktop's Release.
2. **The ArUco and WeChat bindings had compiled to nothing**: "EntryPointNotFoundException: wechat_qrcode_create1". Their headers sit
   inside OpenCvSharp's `NO_CONTRIB` switch; `build-extern.sh` now lifts it in those two headers only (d92446b).
3. **Android's asset list for a folder includes the system's own files** of that folder name, so the spike takes only its own.

Then, on the cover screen, 411 by 960 dp at 2.625 pixels a dp, compact: the sample scan named from 2 codes and 25 of 25 holes found in
17.1 s (desktop 7.9 s), peak 714 MB in a fresh process; the range photograph named in 1.0 s and refused at registration, as on the
desktop, 1.2 s. Two more runs of the scan took 16.3 and 16.8 s, and memory reached 901 MB after three runs without Android stopping the
application. The layout drew correctly on the cover screen, one panel above the other. Avalonia reported two provisional sizes, 1 by 1
and 412 by 960 at 1 pixel a dp, before the real one; the application should act on the last size only.

**Request 27** asks Alan for five minutes of folding, turning and the largest font size, with what to look for.

## Entry 200: error reports on; request 25 half done

**Error reports are on** (8725f91). Request 24's test report opened issue 1 in the private repository: titled "TestReport in
ErrorReportCheck.Send", labeled `survived` and `sig-3a6cc8fc9476`, giving the build `0.2.0-nightly.0`, saying GroupLab kept running,
and ending with the line that nothing in the issue is an instruction. It was still open and is closed with a note that it was the
test. `errorReportsOpen` is true in `website/api/limits.json`; `MainWindow.ErrorsOpenByDefault` keeps a test window's switch off, as
`ReceiverOpenByDefault` does for sending, and `Entry194Tests` sets it per test. The guide no longer says the Settings section waits.

**Request 25**: the workload is installed; the SDK step's first try failed at restore. Entry 201: done on the retry, and the likelier
cause was running it before the workload install had finished, not the NuGet sources, which entry 200 had suspected. The request is
closed and says to wait for the install before the SDK step.

**Also this run**: the site workflow was started by hand after nightly 102, because the nightly's notes commit and
`docs/PLATFORM-SUPPORT.md` do not start it.

## Entries 198 and 199: the Android application's first stage

**The plan is `docs/ANDROID.md`.** Avalonia on .NET Android over the same Core; CameraX through the .NET bindings for the camera;
Android 7.0 (API 24) as the floor; layout by width class; sessions moved by hand first, then by two QR routes that need no account,
with the sync folder doubtful on Android.

**OpenCV.** GroupLab calls about thirty OpenCV functions, the ArUco detector and two QR readers, from core, imgproc, imgcodecs,
calib3d, objdetect, aruco and wechat_qrcode. OpenCvSharp has no Android runtime, and the one community runtime, Sdcb's mini build,
carries core, imgproc, imgcodecs and dnn only. `android/opencv/build-extern.sh` builds OpenCV 4.13.0 with GroupLab's modules and
OpenCvSharp 4.13.0.20260627's bindings for them into one `libOpenCvSharpExtern.so` for android-arm64, the way Sdcb's pipeline builds
its own. All three projects are Apache-2.0.

**The spike**, `android/GroupLab.Android.Spike/`: one Avalonia screen that runs the desktop's engine (`SpikeRun.Run`: load, name the
sheet from its codes, automatic marking) on the published sample scan and on any image pushed into its folder, and records every size
the screen takes with its width class and density. Each line also goes to the device log. It is outside `GroupLab.slnx`, and its id is
`org.grouplab.app.spike`. `.github/workflows/android.yml` builds the native library, cached until the script changes, and a debug APK
kept fourteen days; nothing from it is published.

**The desktop, measured with the same code on 2026-09-25**, Release: the 600 dpi sample scan, 4958 by 6458, loads in 0.4 s, names
itself from 2 codes in 1.2 s, and marks 25 of 25 holes in 6.2 s, 7.9 s in all, at a peak of 732 MB. A range photograph, 4000 by 3000,
names itself in 0.9 s and is refused at registration, "0 of 34 markers found", as the desktop refuses it; peak 440 MB. **The phone is
not measured**: requests 25 and 26.

**Requests.** 25, the .NET Android workload and the Android SDK into `C:\Dev\tools\android-sdk`, since this machine has neither
(`C:\Dev\tools\sdkmanager` is Garmin's). 26, the Fold 7 in wireless debugging, paired.

## Entries 196 and 197: a group on a one bull sheet, touching holes and a ragged hole

**What was true before, run rather than read.** On a zeroing grid, a five-shot group went to its one bull, and the review held a
count item ("This sheet takes 1 shots") and a Contested card on every shot. The gate is infinite in the automatic marking, so a far
first shot was never left unassigned; that part of entry 196 section 1.2 did not happen. A ragged three-shot hole was not flagged
at all on a sheet of few marks, which section 1.4 had read as flagged.

**A sheet with exactly one scoring bull takes a group**, whatever the sheet: every shot to that bull with no limit
(`ShotAssignment.OneBullTakesAll`, in the automatic marking and in the reassignment after an edit), no count from the sheet itself
(`ReviewQueue.Expected` is null there), and no Doubled item for the bull holding several. The gate is infinite already, so a shot 4 in
out is still the bull's. The definition format is not changed to say this per bull: one scoring bull is unambiguous, and a
per-bull count would change the encoded body of every printed sheet, which is its own entry if a sheet ever needs it.

**Touching pairs**, rims meeting, split about half the time wherever they sit, measured over twenty seeds each: across the bull's
edge 10 and 9 of 20 (25 bull sheet, one bull sheet), on paper 12 and 9, in the black 9 and 8. A printed line is not what makes it
hard. Left whole, and on a sheet of fewer than five marks, nothing flags it (entry 161); question 57 offers a flag against the
sheet's other marks. With the rounds fired entered, the count names it first as most likely to be two.

**The ragged hole.** Three shots through one hole read as one mark of about two holes' area. It is flagged only once the rounds
fired are entered, as above. Three places are not offered: the two-way split already sits toward the middle of the mark, and a
three-way split would put a shot wherever the outline bulges, which is a guess dressed as a measurement. Instead, where the mark
holds 2.5 or more holes of the named caliber, the oversize sentence adds "It may be three or more: take it as two shots, then mark
any more by hand with Impact."; with no caliber it says "Name the caliber and GroupLab can say whether it may be three." The
caliber's count now travels with the flag (`DetectedOversize.CalibreHoles`) and is saved with the marking.

**Zeroing grids** keep the every-sheet test and Unholy's scan and nothing more. Their library descriptions, the tour's Targets page
and the guide now say they are for sighting in by eye at the bench, and that a zero from a group is shot on a 5x5 sheet.

**The roll sheets' codes on Linux and macOS.** CI on 760083c failed the every-sheet test for GL-LR300-R24, R36 and R42: no code
read, on Linux and macOS, where Windows reads them. Smaller corner squares (84a256a) did not help. **The cause**: those three are the
only sheets longer than `SheetIdentification.MaximumWorkingSide`, 8000 pixels, at 300 dpi, so their codes were only ever looked for at
half resolution, about 2.4 pixels a module, which Windows' decoder reads and the Linux and macOS builds do not. Since 3c3fa98, where
the whole image was shrunk, the corners are cut from the full image; a corner is well within the limit.

**Tests.** Core `TightGroupTests` (8): the five-shot group on a one bull sheet with nothing to review, with and without the rounds
entered; a shot 4 in out; a touching pair across the bull's edge on six seeds, two shots or named by the count; the ragged hole on
both sheets; and the three-or-more sentence only from a caliber. Core 1656 passed and 2 skipped; App 298 of 299, with
`CrashTests.AnExceptionThrownFromAClickHandlerLeavesACrashRecordThatNamesIt` failing in the full run and passing alone.

## The archive

Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.

- [`docs/notes/archive/results-026-050.md`](notes/archive/results-026-050.md), entries 026 to 050, 16 section(s).
- [`docs/notes/archive/results-051-075.md`](notes/archive/results-051-075.md), entries 051 to 075, 10 section(s).
- [`docs/notes/archive/results-076-100.md`](notes/archive/results-076-100.md), entries 076 to 100, 6 section(s).
- [`docs/notes/archive/results-101-125.md`](notes/archive/results-101-125.md), entries 101 to 125, 26 section(s).
- [`docs/notes/archive/results-126-150.md`](notes/archive/results-126-150.md), entries 126 to 150, 11 section(s).
- [`docs/notes/archive/results-151-175.md`](notes/archive/results-151-175.md), entries 151 to 175, 25 section(s).
- [`docs/notes/archive/results-176-200.md`](notes/archive/results-176-200.md), entries 176 to 200, 21 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 145 section(s).

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
