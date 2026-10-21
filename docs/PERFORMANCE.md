# Performance

**The Performance phase has begun, and has changed no figure.** Entries 342 and 352 made the first changes, each measured against every gate before it was kept; the record below is measured after them. This page is the baseline the phase started from, the method it follows, what each round changed, and the wasteful things noticed while measuring. `DESIGN.md` section 21 Phase 9 is the phase itself; `NOTES-FROM-PLANNING.md` entry 117 is where it came from.

Alan's words are the reason it exists: "Once the program is working as intended, I want to add an optimization phase to work on making the software perform better. I can already tell it is somewhat slow running." He has not said where, and nothing here guesses: every operation is measured and the answer is left to name itself.

## The baseline gate, entry 388 section 4

Planning said on 2026-10-07 that the application has settled enough to measure. The baseline is in `docs/performance-baseline.json`,
measured 2026-10-08 on TACIT-BLUE (Release, commit ca0075c7, nothing else working), and nothing was changed for speed. A
later measurement on the same machine fails the gate when a figure is **more than a quarter slower and 20 ms slower** than its baseline
(`BenchGate`), and a figure the baseline names that was not measured fails too. CI still reports and does not gate. All three passed on
the code that set the baseline, the bench's eleven figures within 7 percent of it.

```
grouplab bench --gate docs/performance-baseline.json
GROUPLAB_PERF_GATE=1 dotnet test tests/GroupLab.App.Tests -c Release --filter Phase9BaselineTests
pwsh scripts/startup-time.ps1 -Gate
```

Moving the baseline is planning's decision, with `--baseline`, `GROUPLAB_PERF_BASELINE=1` and `-Baseline` on the same three.

| The wait, in a person's terms | Median, ms |
|---|---|
| start-up to a usable window (five launches of the Release build) | 1,081 |
| a new window ready to use, in the process | 147 |
| open a 600 dpi scan, as Open opens it, decoded off the window's thread | 965 |
| switch between three open targets, each with the scan | 7.3 |
| images / load a 600 dpi Letter scan | 634 |
| images / load a phone photograph | 47 |
| measurement / identify the sheet from its codes | 210 |
| measurement / the generated sheet, stage by stage | 437 (462 before entry 392) |
| measurement / the generated sheet, stage by stage: S5-S8.holes | 294 (333 before entry 392) |
| measurement / a 600 dpi scan, stage by stage | 1,671 (2,604 before entry 392) |
| measurement / a 600 dpi scan, stage by stage: S5-S8.holes | 693 (1,769 before entry 392) |
| measurement / find holes on a target GroupLab did not print | 296 |
| statistics / the whole analysis of a marking | 1.2 |
| end to end / one sheet from file to figures | 530 |
| end to end / a batch of ten sheets | 4,150 |

**The emulator** (`android-emulator.yml` with `baseline`, `scripts/android-perf.sh`): GitHub's x86_64 emulator with software drawing,
so a record of the emulator rather than a phone, and never a gate. Its figures are in the baseline file under `emulator`.

**What the gate was planned to cover, and still does:**

- **The times a person actually waits:** start-up to a usable window, opening an image, a full analysis of a 600 dpi Letter scan, a full analysis of a phone photograph, the analysis screen after Accept, and a batch of ten sheets.
- **Per platform**, because the answer differs: this desktop today, and a mid-range Android phone when Phase 6 exists. A phone is several times slower, and an analysis that is merely slow here is unusable there.
- **Responsiveness separately from speed.** A window that freezes for four seconds feels worse than one that works for six and says what it is doing. The interface table below records the freeze as its own figure for exactly that reason.
- **Memory too**, since a 600 dpi colour scan is about 25 MB before anything is derived from it.

## Entry 461 section 2.2, 2026-10-21: a phone photograph's codes, measured, and nothing exact left to take

**Measured** with timers put into `OpenCvSharpBackend.Read`, `LiveSheet.CodePlaces` and `SheetIdentification.Identify` for this
measurement and taken out after: the bench's phone photograph (`scans/phase0/20260913_130543.jpg`, the frozen Phase 0 sheet), `grouplab
analyze`, Release, this desktop, three fresh runs. Reading its codes square on took 662 to 861 ms for 8 places, 2 holding a code:

| step | ms |
|---|---|
| finding the markers in the whole photograph (`PhotographMarkers`, 33 found) | 68 to 92 |
| the sheets sharing those markers (9 of 68) and where each prints its codes | about 35, 16 of it the first sheet's layout |
| cutting out the 8 places square on (`WarpPerspective`) | 4 to 8 each, about 40 |
| WeChat's reader on each of the 6 places with no code | 44 to 68 each |
| the second reader's detect and decode on those 6 | 5 to 35 each |
| the 2 places with a code (WeChat 7 to 9, the decode 6 to 15) | 13 to 24 each |

**Why nothing was changed.** About 450 ms is the two readers failing in full on the six empty places, which are where the other sheets
sharing the frozen sheet's markers print their codes. A failing read is the reader's fixed sequence (two scales and four binarizers in
WeChat), so it cannot be cut short and give the same answer on every picture; reading fewer places changes how many codes a picture is
said to have read, which entry 415 already called a decision, not a speed-up. The marker search is OpenCV's with the sizes the picture
needs, and its result is not the measurement's (that searches again sized from the sheet's own markers). Making the readers once and
keeping them was measured in entry 415 and gained nothing. So the choice goes to planning as question 106, and the bench baseline is
unchanged. The current sheets' photographs read in 167 to 283 ms (entry 415); this photograph is slow because of the sheet it shows.
Nothing reads QR codes on several threads.

**Answered 2026-10-21 (entry 462 section 0): (b), leave it.** Every place is still read, so a picture showing two sheets is still
caught. Phase 9 has no next step queued.

## Phase 9, 2026-10-20: the residual thresholded before it is opened, finding the holes on a 600 dpi scan about a fifth sooner

**Measured first** (`grouplab bench --runs 5`, Release, this desktop): the slowest single step left in reading was finding the holes on the
600 dpi scan, then reading a phone photograph's codes. Timers put into `RenderDifferenceHoleDetector.Detect` for this measurement and taken
out after gave, warm, on the 600 dpi scan: the alignment about 150 to 200 ms (its phase correlations, which entry 433 showed cannot be made
cheaper and keep every shift), drawing the sheet about 90, **the open of the grey residual 69 and the threshold after it 16**, the marks
about 50, the warp about 40, **setting everything beyond the sheet to paper 37, on one thread**, and the residual about 40, 8 of it
zeroing beyond the sheet on one thread.

**The change, and why it is the same answer.** The residual was opened with a disc and then thresholded. For a flat element the two can
be taken in either order: after the erosion a pixel is at or above the threshold exactly where every point of the disc around it was at
or above it before, and after the dilation exactly where some point was, so thresholding first and opening the 0 and 1 image gives every
pixel the old order gave. A 0 and 1 image is opened by `BinaryMorphology.Open`, the same row-run method entry 392 made for the close,
which the imaging backend now takes for an open as well as a close of any 0 and 1 image. Setting the area beyond the sheet to paper now
counts and fills a row at a time on every core, and the residual is zeroed beyond the sheet and thresholded inside its own pass.
`BinaryMorphologyTests.ThresholdingThenOpeningIsOpeningThenThresholdingToTheByte` holds both halves on eight grey pictures with discs on
and past the edges and rows and columns set from side to side: OpenCV's grey open then the threshold against the threshold then the
managed open, and the managed open against OpenCV's own open of the same 0 and 1 image, every pixel.

| | before | after |
|---|---|---|
| bench, a 600 dpi scan (no holes): S5-S8.holes, median of three benches of five, alternated | 492 ms | 378 ms |
| bench, a 600 dpi scan, stage by stage | 1,038 ms | 918 ms |
| bench, a phone photograph: S5-S8.holes | 143 ms | 127 ms |
| bench, one sheet from file to figures | 325 ms | 323 ms |
| bench, a batch of ten sheets | 2,741 ms | 2,690 ms |
| `grouplab analyze` on the 600 dpi sample with 25 holes, a fresh process each, median of five, alternated | 1,134 ms | 1,024 ms |

The generated sheet's figures moved both ways between benches (184 to 270 ms before, 196 to 288 after) while a game was running on the
machine, and are left out. **Nothing moved:** `scoreboard --corpus` on the four local corpus folders, `scoreboard --synthetic`,
`scoreboard --any-target` (whose binary opens now take the managed path too), and `corpus counts`, committed and local, are line for line
the same before and after but for their time columns, and `grouplab analyze` on the 25 hole sample prints the same. The identify sweep
was not run: nothing it calls changed. Nothing reads QR codes on several threads.

## Entry 447 section 4.3: a mark's rows found by halving, finding the holes about a sixth sooner on photographs

**Measured first** (`grouplab bench --runs 5`, Release, this desktop): the slowest single steps left in reading were finding the holes
on the 600 dpi scan (483 ms) and reading a phone photograph's codes (448 ms). In the codes, the 445 ms are 70 ms finding eight places
and about 375 ms reading them: a place with a code reads in about 12 ms, one without in 50 to 70, because the second detector runs only
when the first finds nothing. Stopping once every code of the sheet is read was tried and taken back: the bench's photograph is a
frozen sheet printing four codes of which two are in the picture, and the identify sweep's 148 s did not move. Working a mark's
moments as whole numbers with a table for its ink was tried and taken back too: timed beside the old moments on the same marks, the
two took the same time. What the timing beside them did show is where a mark's moments go: on load-sheet-close, 470 of its 1,006 ms.

**The cause, and the change.** The moments, the split and every other walk over a mark visit its pixels row by row inside its convex
hull, and each row's two ends were found by testing pixels from each end of the mark's box against the hull, every test costing one step
per hull corner: on a large mark with a long hull, millions of tests. A pixel is inside where no edge of the hull puts it on the other
side, and each edge's test as computed changes side at most once along a row, so a row's inside pixels are one run; from one pixel found
inside where the hull's edges cross the row, `RowInside` now finds the run's two ends by halving. A row where that pixel is not inside is
walked as before (`RowInsideWalked`). On every row of every mark in the 19 local corpus pictures, and on 1,500 random hulls with flat
edges and whole-pixel corners (`RowInsideTests`), the ends are the walk's.

| | before | after |
|---|---|---|
| finding the holes, 15 local corpus photographs, median of three each, summed | 13,260 ms | 11,044 ms |
| the same, median picture | 655 ms | 565 ms |
| the same, load-sheet-close (its moments 470 to 15 ms) | 1,055 ms | 702 ms |
| bench, a phone photograph: S5-S8.holes | 172 ms | 140 ms |
| bench, the generated sheet: S5-S8.holes | 222 ms | 189 ms |
| bench, a 600 dpi scan (no holes): S5-S8.holes | 481 ms | 477 ms |

**Nothing moved:** the four local corpus scoreboards, `scoreboard --synthetic`, `corpus counts` and `identify sweep` are line for line
the same before and after but for their time columns. Nothing reads QR codes on several threads.

## Entry 433: finding the holes, measured again, and a cheaper close

**Measured first, from one Release build** (timers put into `RenderDifferenceHoleDetector.Detect` for this measurement and taken out
after), on the 16 corpus photographs the four local scoreboards read and on the 600 dpi sample with 25 shots, warm, median ms:

| stage | photographs (fastest, median, slowest) | 600 dpi scan |
|---|---|---|
| drawing the sheet and the expected picture | 24, 94, 370 | 221 |
| the paper, the sheet's edge and the inks | 16, 46, 353 | 86 |
| the alignment, all cells (phase correlation inside it: 244 ms of work on a photograph, 837 on the scan, spread over threads) | 16, 52, 134 | 159 |
| the residual, the open and the threshold | 18, 32, 100 | 145 |
| the close | 3, 44, 437 | 202 |
| the marks' outlines | 15, 24, 70 | 78 |
| the marks one by one (moments 72 and 127 of it; the moved print check 4 and 90) | 5, 132, 608 | 344 |
| all of finding the holes | 125, 498, 1726 | 1238 |

The note under entry 352 named the close and the phase correlations; the marks one by one are now the largest step, and the close second.

**The close, made cheaper without moving anything.** An erosion keeps a pixel only where it was set, since the element's middle row
reaches the pixel itself, so each row is now worked only from its first set pixel to its last; a dilation sets nothing further than a
row's half width beyond another row's set pixels, so each row in reach is combined only over that stretch (`BinaryMorphology.Pass`).
Every pixel is the answer the whole row gave before: `BinaryMorphologyTests` compares it with OpenCV's own close on ten pictures and six
more with marks inside a wide picture, and a stretch cut too short fails 16 of the 18. On the 600 dpi sample the close went from 202 ms
to 102 and finding the holes from about 1238 to 1133; on a photograph, whose residual has set pixels across most rows, the median close
went from 44 to 42 and the slowest from 437 to 322. `grouplab bench --runs 5` hardly moves (its 600 dpi scan has no holes, so its close
was already cheap: 474 to 472 ms; the generated sheet 193 to 179; the phone photograph 173 to 169). **Nothing moved:** the four corpus
scoreboards, the synthetic one, `corpus counts` (committed and local) and the identify sweep are identical before and after but for their
time columns. **The baseline gate passed** (finding the holes 179 ms on the generated sheet against 294, 472 on the 600 dpi scan against 693).

**The phase correlations, not changed, and why.** OpenCV's `phaseCorrelate` already pads each cell to its best transform size, and the
cells already run at once; what is left is the transforms themselves, about 33 ms of work a cell at 600 dpi. A smaller or differently
sized transform gives different shifts, so it cannot be made cheaper and keep every result. Nothing here reads QR codes on several threads.

**Next, if planning wants it:** the moments of each mark, 72 ms on a typical photograph and up to 590 on one with a large mark, are a
plain sum over each mark's pixels, the order of whose additions fixes the last digit.

## Entry 415 section 3: reading a photograph's codes, still on one thread

**Where the time goes.** `SheetIdentification.Identify` on the 600 dpi sample, five Fold 7 pictures of the 2026-09-27 and 28 sittings and
six camera stills (Alan's, on his computer), median of three runs after one thrown away, this desktop: finding where each code should be
and cutting it out square on (`LiveSheet.CodeViews`) is 40 to 120 ms; the rest is decoding the cut-outs, 8 to 10 of them on a Letter
sheet, each through OpenCV's WeChat locator and decoder and then, every time, its multi-code detector and decoder as well.

**Kept: a cut-out stops at its first code.** The square-on reading uses only the first code a cut-out gives, and the multi-code pass only
ever added codes after it, so the reader now stops once the first detector has one (`IImagingBackend.ReadFirstInCutOut`); where the first
finds none both run as before. The code is the same either way.

| picture | reading the codes before | after |
|---|---|---|
| the 600 dpi sample | 252 ms | 230 ms |
| five Fold 7 pictures | 184 to 383 ms | 167 to 283 ms |
| six camera stills | 79 to 113 ms | 64 to 86 ms |
| the one whose codes read only enlarged | 7285 ms | 7287 ms (its cut-outs read nothing square on, so nothing changes) |

The identify sweep's scans read 10 to 15 percent sooner (293 to 256 ms on scan 1). **Nothing moved:** `scoreboard --corpus` on the four
local corpus folders, `scoreboard --synthetic`, `corpus counts` and `identify sweep` are line for line the same before and after, the time
column aside; every picture names the same sheet, tile and number of codes. Still one thread: nothing here reads two codes at once.

**The baseline gate passed** (`grouplab bench --runs 3 --gate docs/performance-baseline.json`): identifying a 600 dpi scan from its codes
120 ms against the baseline's 171; the bench's phone photograph's identify stage 462 against 463, its codes not read square on.

**Measured and taken back:** making the two detectors once on each thread and keeping them, where each read makes its own, gained nothing
(within 2 percent either way), so making them is cheap and the change was not kept. **Left, and why:** the decoding itself is what is left,
and the only way to do less of it is to read fewer codes, which changes how many codes a picture is said to have read; that is a
decision, not a speed-up. **Section 3.2, finding the holes, not done this round:** its binary closing is already managed code (entry 392),
what is left is inside OpenCV, and entry 416, a feature Alan chose, was taken first under the same budget.

## Entry 414 section 1.1: a scan decoded once

**Measured first.** A 600 dpi scan was still decoded twice: grey, then in colour for its brightest channel (`Load` then
`LoadMaxChannel` on the command line, a grey and a colour decode in the window's `LoadForEditor`). Each decode of the 600 dpi sample is
about 170 ms of the 645 the load took, warm, on this desktop. OpenCV's own BGR to grey conversion does not give the grey decode (entry 130
found it a level off at about eight percent of pixels), because a PNG's grey decode is libpng's conversion, not OpenCV's.

**What changed.** libpng's conversion, with no gamma in the file, is red, green and blue weighted 9797, 19234 and 3737 parts of 32768 and
truncated, a pixel whose three channels are equal left as it is. Made from the colour decode that way (`ImageLoader.GreyLikeLibpng`), it is
the grey decode at every pixel of the 600 dpi sample, two of Alan's 600 dpi scans, a camera PNG, the screen renders, and palette, alpha
and grey PNGs; OpenCV's conversion and two rounded variants were 8 to 60 percent of pixels off. A gAMA or sRGB chunk turns on libpng's
gamma correction and moved 103,817 of 921,600 pixels, so a PNG naming its gamma, colour space or profile, a 16 bit PNG, a JPEG and an
image brought down to a working size are decoded twice as before (`OneDecodeServes`). `LoadGreyAndMax` serves the command line's
analysis, the fingerprint and scale marker readers and the bench; the window's open uses the same grey.
`ImageLoaderSameResultTests.OneDecodeGivesTheSeparateLoadersImagesToTheBit` holds it, the gamma file included.

**Nothing moved.** `scoreboard --corpus` on all four local corpus folders, `scoreboard --synthetic`, `corpus counts` and `identify sweep`
are line for line the same before and after, time columns aside.

**Before and after,** `grouplab bench --runs 5`, median ms, this desktop: loading a 600 dpi Letter scan 645 to 337; the 600 dpi scan,
stage by stage, 1348 to 1034; one sheet from file to figures 338 to 302; a phone photograph, a JPEG, unchanged (48). In the window,
`Phase9BaselineTests`, opening a 600 dpi scan 1020 to 823 (the baseline says 965). The three baseline gates passed.

## Entry 414 section 1.2: the first trajectory

**What it is.** Nothing in the solver is built on first use: there is no table or cache to build. It is .NET's tiered compilation. In a
fresh process the first three trajectories to 1000 yards took 113, 110 and 70 ms and the fourth 6, then 4.3 for every one after: the
solver's loops run as quickly compiled code until they have been used enough to be compiled properly, from how they were used. With
tiered compilation turned off every flight is 14 ms, three times slower for good, so that is not the answer.

**What changed.** Once the window is up (the desktop's `Opened`, the phone's start), `SolverUse.WarmInBackground` flies eight dope tables
on one thread of the lowest priority, once per run. A dope table half a second after start then takes 6.4 ms rather than 114, and the
first three about 15 rather than 300; the Ballistics screen draws more than one. It costs nothing a person waits for: start-up to a
usable window was 1112 ms against the baseline's 1081, inside the gate, and the work starts after the window exists. The arithmetic is
the same whichever way the code was compiled, so no figure changes (`SolverWarmUpTests`).

## Entry 414 section 1.3: the CEP table

**Measured again: 0.4 ms**, not 125. Entry 170 (2026-09-24) found the cost: `GroupStatistics.HoytCdf` doubled its rule until two sums
agreed to 1e-16, below a double's rounding, and ran to a million points; at 1e-14 the CEPs take about 2 ms with every shown digit the
same. The 125 ms in the list below was measured on 2026-09-20, before that; the record since 2026-10-01 has said 0.4. Nothing to change.

## Entry 410 section 3: finding the holes, the marks' own sums

**Measured first, warm, in one process** (`analyze-folder`, the step timings taken inside `RenderDifferenceHoleDetector.Detect` for this
measurement and removed after): the closing of the threshold mask was 3 to 14 ms and the alignment's phase correlations 22 to 130, not the
largest steps the note above names; that note was written before entry 392 moved the closing into managed code. The largest step was the
loop over the candidate marks: 85 to 109 ms on a Fold 7 picture and 550 to 760 ms on the 600 dpi sample, nearly all of it each mark's
moments (about 200 ms there for 34 marks) and the two-centre split (about 280 ms).

**What changed, and why nothing can move.** Both tested every pixel of a mark's box against its hull, a sign check against each edge.
A hull is convex, so on any row the pixels it accepts are one run; the run's two ends are now found from the two sides of the row and the
pixels between are summed in the same order, so the sums are the same additions. The split's squared distances are products rather than
`Math.Pow`, over flat arrays: a product is the correctly rounded square on every platform.

**Nothing moved.** The old and the new code analysed the same fifteen pictures (the 600 dpi sample, the nine corpus pictures and five of
Alan's six range scans of 2026-09-20): 346 holes, the same labels, and every centre the same to six decimals of an inch.

**Before and after,** `grouplab bench --runs 5`, median ms, this desktop: the generated sheet, finding the holes, 261 to 168, and the whole
sheet 381 to 289; a phone photograph, finding the holes, 199 to 181; the 600 dpi scan, which has no holes, 491 to 494. Warm, the
candidate loop on the 600 dpi sample went from about 560 ms to 100, and on a Fold 7 picture from about 90 to 30. The baseline gate passed.

**Put back after nightly 185, and taken back again in entry 414:** reading the codes all at once (entry 400). It went back once the macOS
Mobile suite had passed without it, under a test that read two pictures together ten times inside a time limit. The Phase 0 gate
record's identify sweep on Linux then crashed in native code three times in twelve runs while the codes were read at once (runs
38041674253, 38148249192 and 38246210673: two segmentation faults and "malloc(): unsorted double linked list corrupted"), and never in
the 28 runs while they were read one at a time. OpenCV's QR detectors on several threads at once corrupt memory on Linux, which Android
is, so the codes are read one at a time on every platform, and the test that read two pictures at once went with it.

## Entry 400: the phone's reading of a photograph, about a tenth sooner, part taken back

Planning named it: the phone's whole reading of a camera photograph, from the picture to its result (`PhoneAnalysis.Run`). Measured headless
on this desktop, Release, nothing else running, as entry 342 did: the 600 dpi sample and the nine Fold 7 pictures of the 2026-09-28 and 29
sittings, each stage as the analysis logs it, medians of three rounds before and five after. Entry 399 had just sent these untagged
pictures through the lens fit, which added a new cost on the bent ones: registering them, 95 to 230 ms, nearly all of it the bend's fits
with each marker left out in turn.

**The slowest stage was reading the codes**, 190 to 420 ms a picture: about 90 to find where each code should be and cut it out square on,
then about 300 to read the ten cut-outs one after another, 20 to 60 ms each, six of them at places where another sheet sharing the layout
would put a code. On the one picture whose codes read only enlarged, 7.6 of its 8.0 s.

**What changed, and why no figure can move.** Each code's cut-out is read by detectors of its own, so the ten are read at once and then
taken in their order exactly as before; the cut-outs enlarged on a hard picture likewise. Finding the code places looked for the markers
twice in the same picture and now looks once. The bend's fits with each marker left out are each their own, so they run at once and are
summed in the markers' order, which gives the same result to the last bit.

| picture | before (entry 399's code) | after |
|---|---|---|
| the 600 dpi sample | 765 ms | 661 ms |
| eight ordinary Fold 7 pictures | 689 to 1056 ms, mean 893 | 508 to 684 ms, mean 613 |
| the one whose codes read only enlarged | 8016 ms | 4562 ms |

Over the nine Fold 7 pictures the reading takes 38 percent less in all, and 31 percent less for an ordinary one. Reading the codes is now
85 to 144 ms on an ordinary picture. **Nothing moved:** `compare-photos` on ten photographs, `scoreboard --corpus`, `scoreboard
--synthetic` and `identify sweep` are the same line for line before and after but for their time columns.

**On the bench:** a new figure, "a phone photograph, stage by stage" (the Phase 0 phone photograph, its codes then the whole analysis),
683 ms; "identify the sheet from its codes" moved from 209.5 to 109.6 ms. The emulator's record reads the 600 dpi scan, not a camera
photograph, so it has no before and after for this. What is left on a photograph: finding the holes, 132 to 282 ms; the phone's decoding
and shrinking of the picture, 46 to 126 ms; reading the codes; finding the markers again for the measurement, about 45 ms.

**Taken back the same day, in part.** The build of entry 401 ran every suite clean on Windows and Linux, but on macOS the Mobile suite,
six minutes until then, ran for an hour until the job's limit ended it, with no test named. Reading the codes at once is the change that
puts OpenCV's QR detectors on several threads together, which nothing else in GroupLab does, so the codes are read one at a time again
while the cause is found; the bend's fits at once (plain arithmetic, like the bull locator's since entry 352) and the markers found once
stay. Measured again, five rounds: an ordinary Fold 7 picture 796 ms on average (was 893 before entry 400, 613 with the codes read at
once), the hard picture 7965 ms; the bench's "a phone photograph, stage by stage" 950 ms and "identify the sheet from its codes" 170.8
ms. CI now names a test that runs fifteen minutes (`--blame-hang-timeout`). If the next macOS run passes, the reading at once goes back
behind a test that runs it on every platform; if it hangs again, the test it names says what did it.

## Entry 392 section 2: the first optimization, hole finding on a 600 dpi scan

Planning named it: the hole stage, 1769 ms of the 2604 a 600 dpi scan took. The close of the binary mask by a disc 67 px across was about
1700 ms of it, because OpenCV takes the maximum over every point of the disc at every pixel. `BinaryMorphology.Close` gives the same close
from each row's distance to the nearest set or unset pixel and the element's own row half widths, read from OpenCV, in 17 ms; every
result is identical, pixel for pixel on 133 real closes and in every corpus output (docs/PHASE1-RESULTS.md, entry 392). The hole stage is
now 693 ms and the scan 1671. What is left in it: the alignment, about 220 ms, the open, about 100, the rasterising, about 110. The four
figures above moved; the rest keep their 2026-10-08 values, because the machine ran under another program's load that night.

## The method

- **Measure first, always.** No change is made because something looks slow.
- **The record is committed**, so a change's effect is a difference between two committed tables rather than an opinion.
- **CI reports and does not gate.** Runner speed varies between runs and between machines, so the committed record is measured on Alan's own desktop and any CI figure is for information, exactly as the raw gate records are today.
- **The Phase 0 gate record is the safety rail.** Every gate table must stay identical through every optimisation. A faster routine that moves the last digit of a measurement is a defect, not an improvement, and the record catches it on the same push. An optimisation that does change a figure has to be argued as a correction, with its reasoning written down, the way entry 101's convergence fix was.
- **Profile before choosing.** Name where the time actually goes, per stage, before touching anything: this project has already been surprised by a ten-iteration limit inside a native library.
- **Cheap wins first:** work done twice, images copied that could be shared, work done at 600 dpi that a smaller image would settle, and anything computed for a screen nobody opened.

## How the record is made

Both tables are regenerated by a command, never by hand.

```
grouplab bench --runs 5 --record docs/PERFORMANCE.md
```

`grouplab bench` takes no file from anybody. Every case runs against material committed in this repository or generated by GroupLab from its own sheet: a rendered sheet, a 25 shot sample it shoots at with a seeded generator, Alan's own committed scans and photograph, and a sessions database it fills itself. It runs on a bare checkout, on CI, and on a machine that has never analysed a target. Each case runs once to warm up, then five times; the figure is the median and the brackets are the fastest and slowest run.

The interface table is measured by the same benchmark driven headlessly, because a window needs a windowing platform that the command line build does not carry:

```
GROUPLAB_BENCH_TO_DOCS=1 dotnet test tests/GroupLab.App.Tests --filter ControlBenchTests
```

**Neither list is maintained by hand.** `BenchCoverageTests` enumerates everything in GroupLab that does work and fails when one has no bench case, and `ControlBenchTests` walks the visual tree of every screen and fails when a control a person can click is neither measured nor excluded by name. What is excluded is named in the tables with its reason, because exclusion by silence is how a benchmark rots.

## What was found while measuring, and not changed

Entry 117 optimises nothing: each of these is a candidate with its measurement beside it, for the phase to take up.

- **Hole detection is the analysis.** On the generated 300 dpi sheet, S5-S8 is 622 ms of an 824 ms analysis; on Alan's 600 dpi scan it is 2103 ms of 3649 ms. Nothing else in the pipeline is close.
- **Finding the bulls costs four times what registering the page does, and grows faster than the image.** P0 is 158 ms at 300 dpi and 818 ms at 600 dpi, against 42 ms and 79 ms for the markers. The image grows fourfold between those resolutions and this stage grows fivefold.
- **Identifying the sheet from its codes is 530 ms** on a 2550 by 3300 image, which is more than ten times the marker stage that follows it and reads the same page. Its own spread is wide, 239 ms to 539 ms, which says the cost depends on where in the library the answer is found. Entry 115 section 5 saw the same thing at 600 dpi, where identification was 9.7 to 13.7 s of a 17.7 to 22.8 s analysis.
- **The CEP table was 125 ms** (0.4 since entry 170; entry 414 section 1.3) for two figures, which is twice the whole analysis of a marking that contains it and the only statistic anywhere near the cost of an image. It is a numerical integration.
- **A 600 dpi image was decoded twice** (a PNG once since entry 414 section 1.1). Loading is 634 ms, and the pipeline reads the same file again for its strongest channel: one decode could give both.
- **Saving a session is 65 ms**, almost all of it writing the marking as JSON rather than the database write.
- **The first trajectory costs about a hundred milliseconds and the next costs four** (tiered compilation, warmed in the background since entry 414 section 1.2), so something in the solver is built on first use. A person waiting for a dope table pays it once, on the screen where they are already waiting.
- **A batch of ten sheets is ten separate analyses**, 8.7 s for ten copies of a sheet that takes 958 ms alone. Nothing is shared between them, not even the definition's derived layout.

## The baseline of entry 331, 2026-10-01

Planning asked for a baseline and nothing optimised: the bench record below was measured again today, and three things it does not cover
were measured beside it, on the same desktop (TACIT-BLUE, 16 processors, Release build, commit a21be7c1).

- **Start-up to a usable window:** 1.9 s median over five launches of the desktop application (1.6 to 2.2 s), timed from starting the
  process to its main window existing, with Alan's own settings and records.
- **Memory once idle:** about 180 MB working set four seconds after the window appears (179 to 182 MB), the same in private bytes.
- **The phone's own pipeline on this desktop's processor** (`PhoneAnalysis.Run`, what a phone runs after the picture is taken, measured
  headless): the 600 dpi sample scan 2.7 s median (2.4 to 3.0 s), a Fold 7 photograph from the 2026-09-29 sitting 2.5 s (2.4 to 2.8 s),
  peak 450 MB. A phone is several times slower than this desktop; the time on a phone itself waits for a device (request 50's sitting,
  or Firebase Test Lab, request 62).

**Slowest, in a person's terms:** a batch of ten sheets, 11.8 s; one 600 dpi scan measured stage by stage, 4.5 s, most of it hole
detection, as in the first record; start-up, 1.9 s; one sheet from file to figures, 1.1 s.

**Slower than 2026-09-20, not yet explained:** every image case is 20 to 35 percent slower than the first record (one sheet 0.96 to
1.15 s, ten sheets 8.7 to 11.8 s, a 600 dpi scan's measurement 3.6 to 4.5 s). Work was added since (the lens refit, the second marker
pass for a sheet far away, the printed-shape test), and this machine's load during the run was not controlled; a run on an idle machine
would tell the two apart, and is the first thing the Performance phase should do.

**Entry 342: most of it was the machine, not the code.** The 2026-09-20 code (commit a506d666) and today's (663a29c6), both Release,
were benched by turns on the same desktop, three rounds of three runs, so whatever else the machine was doing fell on both alike. Loading
a 600 dpi scan took 669, 664 and 695 ms on the old code against 662, 653 and 673 on the new; a 600 dpi scan measured stage by stage 4109,
4153 and 3735 ms against 4081, 3973 and 4828. The whole path from a file to the figures is the one place the new code looks slower: one
sheet 987 and 1002 ms against 1053 and 1349, ten sheets 9198 and 9016 against 9987 and 12348, about 7 percent in the quieter round. The
rounds themselves differed by up to 25 percent, because a second worker was building and testing on the machine, so a difference of 7
percent cannot be bisected across nightlies here; the end to end case on an idle machine is still the measurement that would settle it,
and the work added since (the lens refit, the second marker pass, the printed-shape test) is the expected cost if it holds.

## The phone's pipeline, stage by stage, entry 342

`PhoneAnalysis.Run` headless on this desktop (Release, the machine quiet), on the 600 dpi sample and the nine Fold 7 pictures of the
2026-09-28 and 29 sittings (Alan's own, kept on his computer). Each stage's time is what the analysis logs as it goes (`read.stage`).

**Where the time went before.** On the sample, of about 1.6 s: hole detection 0.62 s, reading the sheet's codes 0.49 s, finding the bulls
0.22 s, making the working copy 0.20 s, the rest under 0.03 s each. On the eight ordinary Fold 7 pictures, of 1.3 to 2.0 s: reading the
codes 0.60 to 0.95 s, the largest stage on every one, then holes 0.3 s and bulls 0.15 s. One picture of the nine (20260928-181549) took
13 s, 12.4 of it reading codes: none read square on, so the whole picture was read at full, half and quarter size (3.7 s, finding none, as
the corner search entry 195 needs on a scan) and then each code cut out and enlarged (7.7 s), where all four read.

**What was slow inside reading the codes.** Each code is first cut out square on where the markers say it is, ten places on a sheet, and
read. A cut-out that did not read went on to the search of a whole sheet's corners that the reader falls back on: thirds, quarters and
eighths of the cut-out, eight more reads of pieces that cannot hold a whole code. About 100 ms a failed cut-out, against 30 to 70 ms for
one that read. The cut-outs are now read as themselves only (`IImagingBackend.ReadCutOut`); a whole picture keeps the corner search.

**After,** the old and new reading measured by turns in one process on the same ten pictures, three rounds each:

| picture | reading the codes before | after |
|---|---|---|
| the 600 dpi sample | 462 ms | 208 ms |
| eight ordinary Fold 7 pictures | 467 to 949 ms | 191 to 415 ms |
| the one whose codes read only enlarged | 12.5 s | 8.5 s |

The whole analysis is now 1.27 s on the sample (1.26 to 1.40) and 1.10 s on a Fold 7 picture (1.10 to 1.11). **Nothing moved:** every
picture names the same sheet, tile and number of codes, and the Phase 0 spikes' tables (sheets, photos, markers, refinement, threshold,
scale, field, detectors), the identify sweep, both scoreboards and the corpus counts are identical before and after but for their time
columns (the identify sweep's d-sheet scans read in 300 and 380 ms where they took 553 and 648).

**What is left, and why it was not changed here.** Hole detection (0.3 to 0.8 s) and the bull locator (0.15 to 0.3 s) are now the largest
stages, and both decide figures, so a change there needs its own measurement against every gate. The slow picture's 3.7 s of whole-picture
reading before its cut-outs could be skipped when the markers already placed the codes, but that changes which resolution a sheet is
said to be read at in the identify sweep, so it is a decision rather than a speed-up.

## GroupLab Dev on an idle phone, entry 343

**Not measured on a device.** What is held instead: GroupLab Dev's only background work is the update check, about every six hours,
which WorkManager runs only on an unmetered network, with the battery not low and the storage not low; a download happens only when a
newer build exists. Leaving the screen lets go of the camera, its torch and the level's sensor, and that sensor is the only one, with no
location, wake lock, alarm or foreground service anywhere in the Android project; a test in the Core suite reads the source for all of it,
since no emulator runs in CI. At request 50's sitting, `adb shell dumpsys batterystats` for GroupLab Dev after an idle hour goes here.

## Entry 352: the 7 percent settled, and the next round

**The 7 percent was the machine.** With nothing else running, the 2026-09-20 code (a506d666) and the code of entry 352 (d677caab), both
Release, were benched by turns, four rounds of five runs each (`grouplab bench --area "end to end"`). A batch of ten sheets took 8645 to
8685 ms on the old code and 8377 to 8420 on the new; one sheet from file to figures 966 to 971 against 938 to 949. The rounds agreed within
half a percent, so the new code is about 3 percent faster than the old, not 7 slower: entry 342's difference came from the second worker
building beside it.

**Where the time went.** On the desktop's whole path, one generated sheet from file to figures (about 940 ms): finding the holes 614 ms,
finding the bulls 154, reading the markers 44, the rest decoding and the small stages. Inside finding the holes, timed step by step: 315 ms
was the two-centre split that every mark large enough to be two holes is given, so that a person told "this may be two shots" can take both;
136 ms the alignment of each bull's cell by phase correlation; 84 ms the closing of the threshold mask; drawing and placing the expected
artwork about 50 ms. On the phone's path (`PhoneAnalysis.Run` headless on this desktop, the same ten pictures as entry 342) the holes and
the bulls were again the largest stages after reading the codes.

**What changed, and why no figure can move.**

- **The split** found the same pixels twenty times over, testing each against the mark's hull every time. The pixels that take part are
  now found once, in the order they were always visited, and the twenty rounds run over them; a round that leaves both centres exactly
  where they were ends the loop, since every later round would repeat it. The same additions in the same order give the same centres to
  the last bit.
- **The alignment** measures each bull's cell from images nothing writes to, so the cells are measured at once and gathered in their
  order, which is all that reads them.
- **The bulls** are located the same way: each from the image and the mapping alone, at once, and what the stage records is then written
  bull by bull in the order it always was, so the trace is unchanged too.

**Nothing moved.** The Phase 0 spikes' tables (sheets, photos, markers, refinement, threshold, scale, field, detectors) match
`scans/phase0/measurements/tables` and the run before the change, the synthetic and any-target scoreboards and the local corpus's counts
are the same line for line, all but their time columns, and every picture below finds the same number of holes.

**Before and after on the desktop,** the same machine idle, five runs each, median in ms:

| case | before | after |
|---|---|---|
| a batch of ten sheets | 8279 | 3897 |
| a 600 dpi scan, stage by stage | 3623 | 2550 |
| one sheet from file to figures | 924 | 486 |
| the generated sheet, stage by stage | 838 | 407 |

**Before and after on the phone's path,** median of three runs after one thrown away, in ms:

| picture | whole reading | finding the bulls | finding the holes | reading the codes |
|---|---|---|---|---|
| the 600 dpi sample | 1485 to 1185 | 206 to 48 | 777 to 540 | 210 to 211 |
| eight ordinary Fold 7 pictures | 880 to 1230, now 695 to 871 | 146 to 257, now 21 to 42 | 256 to 492, now 180 to 283 | 190 to 395, unchanged |
| the one whose codes read only enlarged | 7832 to 7563 | 145 to 21 | 305 to 266 | 7163 to 7061 |

**What is now slowest, and left.** On a photograph, reading the sheet's codes is again the largest stage (190 to 395 ms), and on the one
picture read only enlarged it is 7 s of 7.6; skipping its whole-picture reads is the decision entry 342 named, not a speed-up. Finding the
holes is next (180 to 540 ms), most of it now the closing of the threshold mask and the alignment's phase correlations, both inside
OpenCV. On the desktop, decoding a 600 dpi PNG (630 ms) is the largest single step left in reading a scan; entry 414 section 1.1 decodes it once.

## The record

Measured 2026-10-08 on TACIT-BLUE, 16 processors, Microsoft Windows NT 10.0.26300.0, Release build, commit ca0075c7, 5 timed runs of each case after one thrown away.

Every figure is milliseconds: the median, and in brackets the fastest and slowest run. The Performance phase measures every change against this record, and its gate will be written from it.

### The slowest first, which is the work queue

| ms, median (low to high) | Case |
|---|---|
| 4149.8 (4129.1 to 4307.7) | end to end: a batch of ten sheets |
| 2604.2 (2594.7 to 2615.3) | measurement: a 600 dpi scan, stage by stage |
| 633.6 (632.8 to 641.1) | images: load a 600 dpi Letter scan |
| 529.6 (494.4 to 536.1) | end to end: one sheet from file to figures |
| 461.8 (420.1 to 599.8) | measurement: the generated sheet, stage by stage |
| 295.7 (278.3 to 301.4) | measurement: find holes on a target GroupLab did not print |
| 209.5 (206.4 to 226.2) | measurement: identify the sheet from its codes |
| 134.0 (133.4 to 134.3) | images: load the same sheet at 300 dpi |
| 98.1 (91.3 to 102.0) | documents: user-guide PDF |
| 88.6 (85.9 to 97.6) | rendering: the icon set |

### definitions

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| read a sheet definition | 0.3 (0.3 to 0.4) | Reading GL-CF25-LTR.gltd.json from disk into the model. 0 diagnostics. |
| validate a sheet definition | 0.5 (0.5 to 0.5) | Every conformance check the validator makes, on one sheet. 0 diagnostics. |
| derive the layout | 0.1 (0.1 to 0.1) | The bull lattice, the marker positions and ids, the code corners and the data block cells. 38 markers, 4 code corners, no data block, 15 grid lines, layout recognised. |
| encode and decode the code payload | 0.5 (0.4 to 0.6) | The sheet to a GLTD-B body and frame, and back again. 70 byte frame, decoded back. |

### rendering

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| sheet to PDF | 2.5 (2.3 to 9.8) | Building the sheet's pages and writing them as a PDF, which is what Save PDF does. 1 page, 99 kB. |
| sheet drawn for the printer | 0.8 (0.8 to 0.9) | Laying the sheet out for a 600 dpi Letter printer and checking every mark against the paper and the margins. The device itself is excluded. 4443 items, fits. |
| rasterise the sheet at 300 dpi | 52.1 (51.3 to 69.5) | Drawing the sheet into an image, which is what generates a sample and what the tests measure against. 2550 by 3300 px. |
| the icon set | 88.6 (85.9 to 97.6) | Every Windows and Linux icon size from the mark, which is a build step rather than something a person waits for. 10 files. |
| read the target library | 3.8 (3.4 to 4.2) | Every sheet in the library read and laid out, which is what the library screen shows. 54 sheets. |

### images

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| load a 600 dpi Letter scan | 633.6 (632.8 to 641.1) | Decoding the file and building the grey and strongest-channel images the analysis works on. 4958 by 6458 px, PNG. |
| load the same sheet at 300 dpi | 134.0 (133.4 to 134.3) | Decoding the file and building the grey and strongest-channel images the analysis works on. 2479 by 3229 px, PNG. |
| load a phone photograph | 47.0 (46.4 to 47.6) | Decoding the file and building the grey and strongest-channel images the analysis works on. 4000 by 3000 px, JPEG. |
| load the generated sample | 87.8 (86.3 to 89.6) | Decoding the file and building the grey and strongest-channel images the analysis works on. 2550 by 3300 px, PNG. |

### measurement

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| identify the sheet from its codes | 209.5 (206.4 to 226.2) | Reading the printed codes and finding which definition they name, which is what happens before anything is measured. GroupLab 5x5 Load Development, Letter. |
| the generated sheet, stage by stage | 461.8 (420.1 to 599.8) | One analysis of the 25 shot sample, with every stage of the pipeline filed as its own figure from the stage record. 25 holes, 0 markers not found. |
| &nbsp;&nbsp;the generated sheet, stage by stage: S1.scale | 0.0 (0.0 to 0.0) | part of the generated sheet, stage by stage |
| &nbsp;&nbsp;the generated sheet, stage by stage: S2.fiducials | 56.0 (49.0 to 59.0) | part of the generated sheet, stage by stage |
| &nbsp;&nbsp;the generated sheet, stage by stage: S3.register | 0.0 (0.0 to 0.0) | part of the generated sheet, stage by stage |
| &nbsp;&nbsp;the generated sheet, stage by stage: S4.verify | 0.0 (0.0 to 0.0) | part of the generated sheet, stage by stage |
| &nbsp;&nbsp;the generated sheet, stage by stage: P0.bulls | 22.0 (19.0 to 39.0) | part of the generated sheet, stage by stage |
| &nbsp;&nbsp;the generated sheet, stage by stage: S5-S8.holes | 333.0 (311.0 to 422.0) | part of the generated sheet, stage by stage |
| &nbsp;&nbsp;the generated sheet, stage by stage: S9.assign | 5.0 (4.0 to 25.0) | part of the generated sheet, stage by stage |
| a 600 dpi scan, stage by stage | 2604.2 (2594.7 to 2615.3) | The same pipeline on Alan's own 600 dpi scan, which has no holes in it: the registration cost at the resolution a scanner gives. 0 holes, 4 markers not found. |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: S1.scale | 0.0 (0.0 to 0.0) | part of a 600 dpi scan, stage by stage |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: S2.fiducials | 91.0 (79.0 to 94.0) | part of a 600 dpi scan, stage by stage |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: S3.register | 0.0 (0.0 to 0.0) | part of a 600 dpi scan, stage by stage |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: S4.verify | 0.0 (0.0 to 0.0) | part of a 600 dpi scan, stage by stage |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: P0.bulls | 110.0 (106.0 to 122.0) | part of a 600 dpi scan, stage by stage |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: S5-S8.holes | 1769.0 (1759.0 to 1780.0) | part of a 600 dpi scan, stage by stage |
| &nbsp;&nbsp;a 600 dpi scan, stage by stage: S9.assign | 0.0 (0.0 to 0.0) | part of a 600 dpi scan, stage by stage |
| the review queue | 0.1 (0.1 to 0.2) | Everything the analysis wants settled before it will measure, raised from one marking. 4 items, 4 open. |
| find holes on a target GroupLab did not print | 295.7 (278.3 to 301.4) | Find holes, experimental, on a drawn black-bull target at 200 dpi with the scale set by hand: the three ways a hole is told from the print, and their merge. 12 proposed for 16 holes. |
| what to say about the sheet | 0.0 (0.0 to 0.0) | The sentences the marking screen shows when a sheet is imperfect: the print scale, the doubt, and the refusal with what to do next. Measured in the sheet's own inches; if the sheet was not pri. |
| print the stage trace | 0.0 (0.0 to 0.0) | Laying the whole trace out as grouplab analyze prints it, at its fullest. 2625 characters over 9 stages. |

### storage

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| a chronograph string | 0.0 (0.0 to 0.0) | Reading a string of velocities, pairing it with the shots and taking its spread, as the Ballistics screen does. 25 pairs, SD 2.01 fps. |
| save a session | 4.2 (3.9 to 4.8) | Writing one analysed sheet to the sessions database, as Save does. session 6. |
| reopen a session | 0.9 (0.8 to 1.0) | Reading one back, which is what opening a session from the list does. 25 shots. |
| query a few hundred sessions | 9.0 (8.6 to 11.2) | The Session records list over a database the benchmark fills itself with 300 sessions. 300 sessions, 43 on one rifle. |
| export and import the database | 8.1 (7.6 to 9.8) | The whole database out as JSON and back into an empty one, which is what a person's backup is. 376 kB. |

### statistics

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| sigma and mean radius with intervals | 0.0 (0.0 to 0.0) | The Rayleigh estimate and its exact interval, which every headline figure comes from. sigma 0.0949 in. |
| the CEP table | 0.4 (0.4 to 0.4) | CEP 50 and CEP 90 under both the correlated normal and the Grubbs-Patnaik approximations. CEP 50 0.1102 in. |
| the bootstrap interval | 7.9 (7.7 to 8.5) | The resampled interval around the mean radius, at the committed number of resamples. 0.103 to 0.144 in. |
| the shape tests | 0.0 (0.0 to 0.0) | The circularity test with its resamples and the vertical stringing test, which are the two judgement cards. circularity p 0.538, stringing correlation 0.22. |
| the shot order trend | 0.9 (0.9 to 0.9) | Whether the group opened up as it was shot: a rank correlation against 9999 shuffles of the same shots, which is the slowest of the shape answers. correlation -0.13, p 0.520. |
| the flyer calibration | 0.3 (0.3 to 0.3) | What the worst shot of a group this size is expected to be, which the worst-shot card is read against. worst expected at 2.727 sigma. |
| group comparison | 2.1 (2.1 to 2.2) | Two groups compared by dispersion and by centre, with the tests' verdicts and what each could have detected. 1 pair. |
| hit probability | 0.0 (0.0 to 0.0) | The chance of a hit inside a named radius, at both ends of the sigma interval. 100.0 percent inside half an inch. |
| the range statistic intervals | 0.0 (0.0 to 0.0) | Extreme spread and the other range statistics read off the simulated table, with their intervals. sigma 0.2155 in from a 1 in spread. |
| the zero correction | 1.1 (1.1 to 1.2) | The group centre's offset from the point of aim with its uncertainty, and the refusal when it cannot be told from chance. a correction with its verdict. |
| the whole analysis of a marking | 1.2 (1.2 to 1.3) | Every figure the analysis screen shows, from a marking: the path a person waits on after Accept. 25 shots measured. |

### solver

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| one trajectory | 20.7 (4.3 to 128.4) | A single flight to 1000 yards at the solver's own step, which is what every dope table is made of. 11 points, 35.6 MOA at the far end. |
| a dope table | 4.3 (4.3 to 6.4) | The table the Ballistics screen shows, in the person's own units and clicks. 21 rows. |
| a hit probability | 87.6 (82.2 to 93.3) | Entry 156's answer at 600 yards on the middle confidence preset: ten thousand strings, the costs of every source and a curve against distance. 30 percent first round, 10 costs, 3 curve points. |

### documents

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| user-guide PDF | 98.1 (91.3 to 102.0) | Laying USER-GUIDE.md out and writing it as a PDF, pictures and all. 1390 kB. |
| testing-guide PDF | 14.4 (14.1 to 15.6) | Laying TESTING-GUIDE.md out and writing it as a PDF, pictures and all. 276 kB. |
| the volunteer pack | 1.9 (0.9 to 2.8) | The sheet and its page of instructions together, as the print screen gives them. 103 kB. |
| the one-page report | 79.2 (71.1 to 86.7) | One result's one-page report with its picture, and the box's lines and mean radius circle of a shared picture. 216 kB, 9 lines. |

### end to end

| Case | ms, median (low to high) | What, and what it did |
|---|---|---|
| one sheet from file to figures | 529.6 (494.4 to 536.1) | Everything a person waits for after choosing an image: decoding it, identifying the sheet, registering, finding the holes, assigning them and measuring the group. 25 shots, measured. |
| &nbsp;&nbsp;one sheet from file to figures: S1.scale | 0.0 (0.0 to 0.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: S2.fiducials | 51.0 (48.0 to 56.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: S3.register | 0.0 (0.0 to 0.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: S4.verify | 0.0 (0.0 to 0.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: P0.bulls | 20.0 (19.0 to 21.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: S5-S8.holes | 313.0 (294.0 to 332.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: S9.assign | 4.0 (3.0 to 5.0) | part of one sheet from file to figures |
| &nbsp;&nbsp;one sheet from file to figures: S10.group | 5.0 (5.0 to 6.0) | part of one sheet from file to figures |
| a batch of ten sheets | 4149.8 (4129.1 to 4307.7) | Ten analyses one after another, as analyze-folder runs them, where no picture is kept for a timeline. 250 shots over ten sheets. |

### Measured by name, and not measured

Entry 117 section 3b: exclusion is by name in the record, never by silence.

| Not measured | Why |
|---|---|
| printing: print to a printer | It uses paper and can open an application on the machine. The work before the device, laying the sheet out and checking it against the paper, is measured as "sheet checked for a printer". |
| printing: the system print dialog | It waits for a person. |
| files: open, save and export dialogs | They are the operating system's own windows and wait for a person. What happens after the file is chosen is measured. |
| storage: delete a session | It destroys data. Saving, reading back and querying are measured. |
| network: the update check and any link out | GroupLab makes no network call, and a benchmark that made one would be the first. |
| interface: every control on a screen | Measured by the headless interface benchmark, which has a windowing platform; see docs/PERFORMANCE.md. |

## The interface, control by control

Every control a person can click, found by walking the window rather than from a list. **To responsive** is the freeze: the
click handler's own time on the interface thread. **To settled** is the moment nothing further is coming. **To painted** is the
screen finished. Passes is how many layout passes the click caused.

| Screen | Control | Responsive | Settled | Painted | Passes | |
|---|---|---|---|---|---|---|
| analysis | Every figure is shown in these units. Nothing stored changes. | 306.9 | 401.2 | 401.2 | 2 | on the interface thread |
| marking, a sheet detected | Every figure is shown in these units. Nothing stored changes. | 244.5 | 298.1 | 298.1 | 2 | on the interface thread |
| settings | Every figure is shown in these units. Nothing stored changes. | 86.6 | 234.4 | 234.4 | 2 | on the interface thread |
| marking, empty | Target library | 128.3 | 149.9 | 150.0 | 2 | on the interface thread |
| marking, empty | Print a target | 75.4 | 101.2 | 101.3 | 1 | on the interface thread |
| target library | Print a target | 90.6 | 98.1 | 98.1 | 1 | on the interface thread |
| settings | Print a target | 66.1 | 73.6 | 73.6 | 1 | on the interface thread |
| analysis | Target library | 31.0 | 70.9 | 70.9 | 2 | on the interface thread |
| session records | Print a target | 55.4 | 62.7 | 62.7 | 1 | on the interface thread |
| marking, a sheet detected | Print a target | 53.7 | 60.7 | 60.7 | 1 | on the interface thread |
| analysis | Print a target | 51.9 | 59.5 | 59.6 | 1 | on the interface thread |
| marking, empty | Ballistics | 6.0 | 56.7 | 56.7 | 2 |  |
| compare loads | Print a target | 42.7 | 50.6 | 50.6 | 1 | on the interface thread |
| ballistics | Print a target | 42.7 | 49.9 | 49.9 | 1 | on the interface thread |
| marking, a sheet detected | Target library | 32.4 | 48.9 | 48.9 | 2 | on the interface thread |
| target library | Target library | 31.1 | 47.0 | 47.0 | 2 | on the interface thread |
| session records | Target library | 32.1 | 47.0 | 47.0 | 2 | on the interface thread |
| target library | Letter · 36 + 4 | 37.1 | 40.2 | 40.2 | 1 | on the interface thread |
| target library | A4 · 25 + 3 | 28.9 | 35.7 | 35.7 | 2 | on the interface thread |
| target library | 24 in roll · 30 + 3 | 28.6 | 31.6 | 31.6 | 1 | on the interface thread |
| target library | Letter · 25 | 26.3 | 30.7 | 30.7 | 1 | on the interface thread |
| target library | Tabloid · 30 + 3 | 26.7 | 29.8 | 29.8 | 1 | on the interface thread |
| target library | Letter · 25 + 3 | 26.4 | 29.5 | 29.5 | 1 | on the interface thread |
| session records | Ballistics | 3.2 | 29.0 | 29.0 | 2 |  |
| target library | A3 · 25 + 3 | 25.9 | 28.8 | 28.8 | 1 | on the interface thread |
| target library | Letter · 1 | 25.8 | 28.8 | 28.8 | 1 | on the interface thread |
| target library | 36 in roll · 36 + 3 | 25.1 | 28.2 | 28.2 | 1 | on the interface thread |
| target library | Tabloid · 25 + 3 | 24.9 | 28.0 | 28.0 | 1 | on the interface thread |
| analysis | Ballistics | 2.9 | 27.9 | 27.9 | 2 |  |
| target library | Letter · 30 | 24.8 | 27.8 | 27.8 | 1 | on the interface thread |
| compare loads | Analyse | 20.0 | 27.4 | 27.4 | 2 | on the interface thread |
| settings | Target library | 8.2 | 26.5 | 26.5 | 2 |  |
| target library | 42 in roll · 40 + 3 | 23.2 | 26.5 | 26.5 | 1 | on the interface thread |
| marking, a sheet detected | Analyse | 3.0 | 24.4 | 24.4 | 2 |  |
| target library | Letter · 25 + 5 | 19.6 | 22.9 | 22.9 | 1 | on the interface thread |
| target library | A4 · 25 + 5 | 18.5 | 21.5 | 21.5 | 1 | on the interface thread |
| ballistics | Target library | 7.7 | 17.9 | 17.9 | 2 |  |
| marking, empty | Settings | 1.9 | 15.8 | 15.8 | 2 |  |
| compare loads | Target library | 8.4 | 13.6 | 13.6 | 2 |  |
| target library | A4 · 6 | 9.5 | 12.5 | 12.5 | 1 |  |
| target library | Letter · 6 | 11.0 | 11.0 | 11.0 | 1 |  |
| analysis | Distances | 4.0 | 10.9 | 10.9 | 2 |  |
| analysis | Analyse | 2.1 | 9.6 | 9.6 | 2 |  |
| marking, a sheet detected | Lengths | 3.3 | 9.5 | 9.5 | 2 |  |
| marking, empty | Session records | 4.4 | 8.2 | 8.2 | 2 |  |
| marking, empty | Rotate right (]) | 1.6 | 7.0 | 7.0 | 1 |  |
| session records | Analyse | 2.1 | 6.0 | 6.0 | 2 |  |
| marking, empty | A rifle needs a name and its scope's click. A barrel's detail is its round count so far, a load's is its components. | 5.7 | 5.8 | 5.8 | 2 |  |
| analysis | Session records | 3.5 | 5.5 | 5.5 | 2 |  |
| marking, a sheet detected | Session records | 4.2 | 4.8 | 4.8 | 2 |  |
| marking, empty | Compare loads | 3.4 | 4.6 | 4.6 | 2 |  |
| marking, empty | Apply | 4.0 | 4.3 | 4.3 | 2 |  |
| session records | Session records | 3.5 | 4.2 | 4.2 | 2 |  |
| target library | Session records | 3.4 | 4.0 | 4.0 | 2 |  |
| session records | Compare loads | 2.7 | 3.7 | 3.7 | 2 |  |
| marking, a sheet detected | Ballistics | 2.9 | 3.7 | 3.7 | 2 |  |
| target library | Ballistics | 2.6 | 3.6 | 3.6 | 2 |  |
| analysis | Compare loads | 2.4 | 3.5 | 3.5 | 2 |  |
| target library | Analyse | 2.2 | 3.3 | 3.3 | 2 |  |
| marking, a sheet detected | Compare loads | 2.3 | 3.2 | 3.2 | 2 |  |
| target library | Compare loads | 2.3 | 3.2 | 3.2 | 2 |  |
| marking, empty | Show work | 3.1 | 3.1 | 3.1 | 2 |  |
| marking, empty | Rotate left ([) | 2.7 | 2.7 | 2.8 | 1 |  |
| analysis | Angles | 2.5 | 2.7 | 2.7 | 2 |  |
| marking, a sheet detected | Angles | 2.5 | 2.7 | 2.7 | 2 |  |
| marking, empty | Accept and analyse | 2.7 | 2.7 | 2.7 | 1 |  |
| analysis | Lengths | 2.4 | 2.6 | 2.6 | 2 |  |
| marking, a sheet detected | Distances | 2.4 | 2.6 | 2.6 | 2 |  |
| settings | Session records | 2.0 | 2.5 | 2.5 | 2 |  |
| compare loads | Session records | 2.0 | 2.5 | 2.5 | 2 |  |
| ballistics | Session records | 1.9 | 2.4 | 2.4 | 2 |  |
| session records | Settings | 2.2 | 2.4 | 2.4 | 2 |  |
| compare loads | Ballistics | 1.3 | 2.3 | 2.3 | 2 |  |
| target library | Settings | 2.1 | 2.3 | 2.3 | 2 |  |
| analysis | Settings | 2.1 | 2.3 | 2.3 | 2 |  |
| marking, a sheet detected | Settings | 2.0 | 2.2 | 2.2 | 2 |  |
| settings | Ballistics | 1.3 | 2.0 | 2.0 | 2 |  |
| ballistics | Ballistics | 1.2 | 1.9 | 1.9 | 2 |  |
| settings | Distances | 1.8 | 1.9 | 1.9 | 2 |  |
| ballistics | Analyse | 0.9 | 1.9 | 1.9 | 2 |  |
| compare loads | Compare loads | 1.0 | 1.7 | 1.8 | 2 |  |
| settings | Analyse | 0.8 | 1.7 | 1.7 | 2 |  |
| settings | Compare loads | 1.0 | 1.7 | 1.7 | 2 |  |
| ballistics | Compare loads | 1.0 | 1.7 | 1.7 | 2 |  |
| settings | Angles | 1.3 | 1.5 | 1.5 | 2 |  |
| settings | Lengths | 1.3 | 1.5 | 1.5 | 2 |  |
| marking, empty | Detect on a GroupLab sheet | 1.0 | 1.1 | 1.1 | 2 |  |
| session records | Compare the chosen | 0.9 | 1.1 | 1.1 | 2 |  |
| marking, empty | Analyse | 0.5 | 1.1 | 1.1 | 2 |  |
| compare loads | Settings | 0.8 | 1.0 | 1.0 | 2 |  |
| settings | Settings | 0.8 | 1.0 | 1.0 | 2 |  |
| ballistics | Settings | 0.8 | 1.0 | 1.0 | 2 |  |
| marking, empty | Set | 0.9 | 0.9 | 0.9 | 1 |  |
| ballistics | Keep these on the records | 0.7 | 0.8 | 0.8 | 2 |  |
| ballistics | Accept the mapping | 0.6 | 0.6 | 0.6 | 1 |  |
| marking, empty | Scale: rectangle (R) | 0.1 | 0.5 | 0.5 | 2 |  |
| marking, empty | Impact (I) | 0.1 | 0.5 | 0.5 | 2 |  |
| marking, empty | Add rifle | 0.3 | 0.5 | 0.5 | 2 |  |
| marking, empty | Select (V) | 0.1 | 0.4 | 0.4 | 2 |  |
| marking, empty | Scale: length (L) | 0.1 | 0.4 | 0.4 | 2 |  |
| marking, empty | Add this sheet's shots | 0.2 | 0.3 | 0.3 | 2 |  |
| marking, empty | Pan (P) | 0.1 | 0.2 | 0.3 | 2 |  |
| marking, empty | Point of aim (A) | 0.1 | 0.2 | 0.2 | 2 |  |
| marking, empty | Undo (Ctrl+Z) | 0.2 | 0.2 | 0.2 | 1 |  |
| marking, empty | Zoom in | 0.2 | 0.2 | 0.2 | 1 |  |
| ballistics | Start again | 0.1 | 0.2 | 0.2 | 2 |  |
| marking, empty | Add barrel | 0.1 | 0.2 | 0.2 | 2 |  |
| marking, empty | Add load | 0.0 | 0.1 | 0.1 | 2 |  |
| marking, empty | Shots per bull | 0.1 | 0.1 | 0.1 | 2 |  |
| marking, empty | Clear | 0.1 | 0.1 | 0.1 | 1 |  |
| ballistics | Work out the table | 0.1 | 0.1 | 0.1 | 2 |  |
| marking, empty | Redo (Ctrl+Y) | 0.1 | 0.1 | 0.1 | 1 |  |
| marking, empty | dropdown 4, unnamed | 0.1 | 0.1 | 0.1 | 1 |  |
| ballistics | Twist, in per turn | 0.0 | 0.1 | 0.1 | 2 |  |
| ballistics | Target | 0.0 | 0.1 | 0.1 | 2 |  |
| ballistics | Work it out | 0.0 | 0.0 | 0.0 | 2 |  |
| ballistics | Drag model | 0.0 | 0.0 | 0.0 | 2 |  |
| ballistics | Its reference atmosphere | 0.0 | 0.0 | 0.0 | 2 |  |
| marking, empty | Zoom out | 0.0 | 0.0 | 0.0 | 1 |  |
| settings | Detailed logging | 0.0 | 0.0 | 0.0 | 1 |  |
| analysis | Detailed logging | 0.0 | 0.0 | 0.0 | 1 |  |
| marking, a sheet detected | Detailed logging | 0.0 | 0.0 | 0.0 | 1 |  |
| marking, empty | dropdown 1, unnamed | 0.0 | 0.0 | 0.0 | 1 |  |
| session records | Rifle | 0.0 | 0.0 | 0.0 | 1 |  |
| ballistics | Rifle | 0.0 | 0.0 | 0.0 | 1 |  |
| marking, empty | dropdown 3, unnamed | 0.0 | 0.0 | 0.0 | 1 |  |
| marking, empty | Load on the chosen bulls | 0.0 | 0.0 | 0.0 | 1 |  |
| marking, empty | Fit the image to the view | 0.0 | 0.0 | 0.0 | 1 |  |
| session records | Load | 0.0 | 0.0 | 0.0 | 1 |  |
| ballistics | Load | 0.0 | 0.0 | 0.0 | 1 |  |
| marking, empty | dropdown 2, unnamed | 0.0 | 0.0 | 0.0 | 1 |  |

### Controls not clicked, by name

| Control | Why |
|---|---|
| Design your own sheet | It opens the designer in the Targets panel, which is walked as its own screen when the editor benchmark is written. |
| Duplicate | It writes a sheet into the person's own library, which is data a benchmark must not add to. |
| Open image… | It opens the operating system's file picker, which waits for a person. Opening the file afterwards is measured as its own case. |
| Open, export or report a problem | Its items open file pickers, which wait for a person. |
| Print… | It prints to a device. Since entry 155 it is the Targets panel's own, and it is there on Windows only. |
| Read the list | It reads the clipboard, which belongs to whoever is at the machine. |
| Report a problem… | It writes a report package and then opens a file picker. |

## Temporary files, entry 179

| where | before, 2026-09-24 | after |
|---|---|---|
| `%LOCALAPPDATA%\Temp\claude\c--Dev-grouplab` | 17.96 GB, as Alan measured it; 18.4 GB by `du` | 707 MB, this session's one App build folder |
| test leaks in `%TEMP%` | 14,987 `grouplab-settings-*.json` (956 MB), 60 `grouplab-bench-*` (245 MB), 5 `grouplab-end-to-end-*` (23 MB), 4,301 empty random folders | none from a run: every test writes into `grouplab-tests/<run>`, removed at exit, and the runner's own two folders go with its redirected temp |

What filled the scratch area was this session's own work, not the suite: 14 copies of the App test build output at about 705 MB each
(9.9 GB), clones and history-rewrite copies of the repository (1.6 GB), downloaded release assets and packages (1.1 GB), rendered and
rasterised sheets and one-off research folders (about 3 GB), and about 800 small scripts. The 56 MB scan's two copies were among the
downloads. All 927 were deleted in one command once listed; the three entries still in use were kept.

Then, entry 179 section 1.1: ten earlier session folders idle for a day, the suite's 15,456 leftover `grouplab-*` entries and 4,292 empty
random folders in `%TEMP%`, 1.45 GB, went in a second listed command. A full Core run afterwards left nothing, and the scratch area stayed
at 707 MB.

