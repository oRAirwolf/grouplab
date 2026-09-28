# Phase 1 results, entries 226 to 250

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

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

