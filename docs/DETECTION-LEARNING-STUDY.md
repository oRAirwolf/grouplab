# Is a self-improving detection engine worth building?

NOTES-FROM-PLANNING.md entry 261, a study. Option (a), the scoreboard, has since been built (entry 291, section 9); nothing else here is. Alan: "Do you think we need to build an engine that can analyze
photos and refine the detection and machine vision models without relying on claude itself to analyze? Is that possible?"

## The answer, in short

- **Yes, it is possible, and it needs no Claude.** An engine that measures GroupLab against every sent target and every synthetic
  degradation, and tunes itself against the result, is ordinary code that runs on its own. Claude Code would only write and maintain it.
- **Build now: the scoreboard (option a).** A standing evaluation harness with synthetic degradations, run in CI, and later on every sent
  target. It is cheap, it is what the other two options need anyway, and on its first run it found a real failure (below).
- **Build later: automatic tuning of the classical parameters (option b)**, once the scoreboard has real sent targets in it. It keeps every
  result explainable and reproducible.
- **Do not build yet: a learned model (option c).** The measured failures are not ones a learned hole classifier fixes, and the labelled
  data does not exist in the volume one needs. The conditions that would change this are at the end.
- **The first measurement already pays for itself.** On a rendered sheet with synthetic holes, the detector kept 22 to 25 of 25 holes
  through hard, soft and hand shadows, dim and uneven light, blur, noise and JPEG, with no false marks and a median centre error under
  0.01 in. It **failed completely on curl**: a sheet bowed by 15 pixels across its width at 300 dpi (about 0.05 in) did not register at all.
  That is not a detection problem; it is the registration's, and entry 260's tolerance work (a warp that follows the tags) is the fix.

## 1. Where the current detector fails (measured 2026-09-28, synthetic)

GL-CF25-LTR rendered at 300 dpi, one synthetic hole on each of its 25 bulls (`SyntheticSheet`, scanner-lid backing), two seeds, each
degraded, and read by the whole pipeline (`AutomaticMarking.Run`) as a photograph. A hole counts as found when a mark lies within 0.1 in.

| Condition | Found (two seeds) | False marks | Median centre error | Note |
|---|---|---|---|---|
| clean | 24, 25 of 25 | 0 | 0.006 to 0.007 in | the baseline misses one hole on one seed |
| hard shadow, lower third at 55 percent | 24, 25 | 0 | 0.006 to 0.007 in | as clean |
| soft shadow, a ramp to 55 percent | 23, 24 | 0 | 0.006 to 0.007 in | one more lost |
| a hand's shadow, an ellipse at 50 percent | 24, 25 | 0 | 0.006 to 0.007 in | as clean |
| dim, 45 percent light | 24, 25 | 0 | 0.006 to 0.007 in | as clean |
| uneven, a diagonal to 65 percent | 24 of 25 twice | 0 | 0.006 to 0.007 in | one lost on the good seed |
| glare, a bright ellipse | 22, 23 | 0 | 0.006 to 0.008 in | two or three lost under the hot spot |
| **curl, 15 px across** | **0 of 25 twice** | 0 | none | **registration failed** |
| wave, 8 px, two waves across | 24 of 25 twice | 0 | 0.007 to 0.008 in | followed |
| blur, sigma 1.5 px | 25 of 25 twice | 0 | 0.006 to 0.007 in | |
| blur, sigma 3 px | 25 of 25 twice | 0 | 0.008 to 0.009 in | |
| noise, sd 12 levels | 25, 24 | 0 | 0.007 to 0.008 in | |
| JPEG quality 40 | 24, 25 | 0 | 0.007 in | worst 0.06 in on one hole |

Against DETECTION-PIPELINE.md's gates this is a pass on everything but curl and glare. **What parameters can fix:** glare (a hot-spot mask
and the torch's own glare check of entry 262), and the one hole lost in soft shadow and uneven light (the local paper level entry 260 asks
for). **What they cannot:** curl, which needs a registration model that bends; that is geometry, not parameters, and not learning either.

**The limits of this measurement.** Synthetic holes are clean circles with a model rim; real holes tear, merge and sit on ink. It says how
the pipeline responds to each condition alone, not how often each happens at a range. Real photographs are the next step (section 2).
**Real targets already measured** (docs/SCAN-MEASUREMENTS.md, docs/RESEARCH.md): 25 of 27 hand-verified holes with no false marks on a
commercial target scan (`300_nm_hand_load.jpg`), by the neutral darkness detector; and where holes overlap in tight groups, only 54 of 400
shots as marks of their own. **Overlapping holes are the largest real failure GroupLab has**, and it is not in the table above.

## 2. The data we have and will get

- **Labelled holes today, an estimate:** about 450. The 343 surveyed holes of docs/SCAN-MEASUREMENTS.md section 3, the 27 hand-verified
  ones on the commercial scan, and the 75 shots of Alan's three 25 shot load sheets of 2026-09-26, whose truth is known by count and by
  eye; plus the synthetic corpus, which is unlimited and labels itself.
- **Labels for free:** every sent target carries "what GroupLab found" and "what you changed". A moved, added or removed mark is a
  labelled hole or a labelled mistake.
- **Consent:** the text shown for both levels (`website/api/limits.json`, consent_v2) says GroupLab "may use them to test and improve its
  detection". **Tuning parameters and training a model against them are both improving detection, so both are covered.** "Testing only"
  and "may be published" differ only in publishing: a testing-only photograph may be used this way and must never appear in a public log,
  artifact or dataset. A trained model's weights are not the photographs, but whether weights count as publishing them is a question for
  the planning session before option (c) is ever built.
- **What a learned hole classifier needs, an estimate:** thousands of labelled holes across calibres, papers, backings, lights and
  phones, and hundreds of the hard cases (overlaps, holes on ink, torn edges). At today's rate of sent targets, that is many months at
  least; the survey and the send-targets counts will turn this into a date once there are some to count.

## 3. The options, from cheapest up

| | (a) Scoreboard | (b) Automatic tuning | (c) Learned hole classifier |
|---|---|---|---|
| **What it is** | synthetic degradations and every sent target re-read by each build, recall, false marks and centre error by condition | the classical parameters searched against (a), G3's stability rule kept | a small model that only accepts or refines hole candidates; markers, codes, geometry and every measurement stay classical |
| **Effort, an estimate** | days | a week or two, after (a) | weeks, plus the data collection |
| **What it gains** | every regression caught, the failure conditions named with numbers | a few holes where lighting or paper sits at the edge of today's thresholds | possibly the overlaps and holes on ink, if the data exists |
| **Size and speed on a 4 GB phone** | nothing ships | nothing new ships; the same code, other constants | ONNX Runtime adds about 10 to 20 MB (an estimate), and each candidate costs a small inference |
| **Reproducible and explainable** | fully | fully: a constant and why | less: a weight file, explained by its tests |
| **Licensing (GPL-3.0)** | no issue | no issue | the runtime is MIT; any pretrained weights and every training set need a licence compatible with shipping them under GPL-3.0 |

## 4. Whether Claude is needed

No. The scoreboard, the tuning and a trained model all run as programs, on a schedule, with no one watching. Claude Code writes and
maintains that code, reads the scoreboard, and proposes changes; a change reaches people only through the usual path, code, tests and the
nightly. Nothing changes the shipped detector by itself.

## 5. The recommendation

1. **Now:** the scoreboard, starting with the synthetic conditions above in CI, with the curl case as its first failing line, and entry
   260's tolerance work fixing it.
2. **Next:** the server loop of section 6, adding real sent targets to the scoreboard.
3. **Then:** automatic tuning against it.
4. **Only if** the scoreboard shows a failure that tuning cannot move, most likely overlapping holes, **and** there are a few thousand
   labelled holes with a few hundred of that failure among them, is a learned classifier worth building. Those are the numbers that would
   change this answer.

## 6. Fully automatic, and where it runs

**The flow.** Receiver, then quarantine, then the intake worker rebuilds each submission from its pixels into `ready`, then the archive
worker puts it in the private `grouplab-submissions-archive` releases and deletes it from the server. The hook is one more worker between
intake and archive: it runs the command line analysis on the rebuilt submission, compares GroupLab's finding with the person's own changes,
keeps only numbers and labels (never the photograph, no GPS, no names), and appends a row to the scoreboard. A regression past a set margin
opens an issue in the private error-report repository, as error reports already do.

**The server's capacity, read 2026-09-28 17:45 UTC** (one read-only command, request 52, approved by Alan). Two processors, 11.9 GB of
memory of which 10.2 GB was available with both sites running and no swap, 36 GB free of the 45 GB volume, and a load average of 0.64,
0.22 and 0.13 over one, five and fifteen minutes. **So yes, it has room**, for the loop as sized here: a 300 dpi Letter scan takes a few
seconds and a few hundred MB on the desktop, perhaps two to four times the time on the server's cores, so tens of submissions a day is
minutes of one processor. It runs as one more worker under the same systemd limits as the others, capped at one processor and about 1.5 GB
(CPUQuota 50%, MemoryMax, Nice, IO weight), so it can never take the second processor from the two sites. **The one thing the read
changes:** there is no swap, so the memory cap is not optional; without it one oversized picture could push the sites' own processes out
of memory. At thousands a day it would not fit, and the heavier jobs below stay off the server. The results are numbers only, a few kB a
submission, so the disk is not the limit; a corpus copy is not kept on the server.

**Arm64.** The command line references OpenCV's native library for linux-x64 only (`OpenCvSharp4.official.runtime.linux-x64`). The phone's
arm64 library is Android's and does not run on the server's Linux. A linux-arm64 build needs OpenCV's native part built for linux-arm64,
once, in CI on GitHub's arm64 runners, and published as an asset, an estimated day of work. Until then the loop can run in the private
archive repository's Actions on x64 instead.

**The heavier jobs.** The nightly re-run of every labelled submission and the synthetic corpus, and any tuning, run best in the private
archive repository's GitHub Actions, never the public one, because testing-only photographs must never reach a public log or artifact; the
private minutes allowance has to be counted before it is relied on. The server at night, niced and capped, is the second choice; Alan's
PC is the last. Training a model, if it is ever justified, needs a GPU or many CPU hours, which means a rented machine for the run.

**What stays human.** The loop's output is a scoreboard and issues. Code reads them and proposes changes; people get them through the
nightly.

## 7. Holes found automatically on other people's targets (Alan, 2026-09-28)

On a commercial or other non-GroupLab target, the person sets the scale by hand and GroupLab has no printed artwork to subtract. The
detector for that already exists and is measured: the neutral darkness detector found **25 of 27** hand-verified holes with **no false
marks** on a commercial target scan, and it is what GroupLab's own pipeline grew from. It is not yet wired to the "Targets GroupLab did not
print" path. **What it would take to be usable:** wire it in with the person's scale and bulls, flag uncertain marks in the review queue so
the misses are corrected by touch, and measure it on colored, grid and splatter targets, which it has not been; splatter targets, whose
hits are bright rings, need their own rule. Tuning (option b) is what gets it from "found most" to "found almost all" on ordinary paper;
a learned classifier would only be worth it for the kinds of target the tuned detector still cannot read, measured first.

The measurement script was a scratch test, run once and deleted; its conditions are listed in section 1 so it can be rebuilt as the
scoreboard. Entry 291 rebuilt it as the scoreboard of section 9.

## 8. A card in the frame as the ruler a photograph lacks (entry 271 section 3, a study; nothing is built)

A photograph cannot measure how large a sheet was printed, because nothing in the frame has a known absolute size
(`docs/WHAT-CAN-BE-MEASURED.md`). Entry 271 asked whether a card the size of a bank card, ISO/IEC 7810 ID-1, 85.60 by 53.98 mm, laid flat
on the sheet, could supply one to better than about 0.5 percent at phone resolutions. **This is an estimate from the geometry, not a
measurement, and nothing is promised until it has been measured on real photographs.**

- **Resolution is not the limit.** A phone photograph of a Letter sheet from about 75 cm gives roughly 400 pixels an inch, so the card's
  long side spans about 1,350 pixels. A straight edge fitted along its whole length places itself to a few tenths of a pixel, which is a
  few hundredths of a percent of the card.
- **The card is not on the paper.** Its top face is 0.76 mm above the sheet, so from 75 cm it reads about 0.1 percent large. That is a
  known bias and can be taken out, but only if the distance is known roughly, which the sheet's markers give.
- **The standard allows the card itself a tolerance** of roughly 0.15 percent in its width, so no card is a better ruler than that.
- **The edges are the real risk.** The rounded corners (3.18 mm radius) mean only the straight runs can be used; a white card on white
  paper has little contrast; a card's edge is often bevelled and throws a thin shadow; and a card with printing to its edge blurs the line
  between card and paper. A dark card, or one laid on the sheet's dark printed area, would help.
- **Taken together** the expected error is about 0.2 to 0.3 percent on a good photograph, inside the 0.5 percent asked, and far worse on a
  poor one. Phone depth estimates and autofocus distance are percent-level at best and are not used for scale.

**The measurement that would settle it:** one GroupLab sheet scanned at 600 dpi (the truth), then about ten photographs of it with a card
flat on it, in ordinary light, at different heights and angles, with a light card and a dark one. The card's measured size against the
scan's scale, photograph by photograph, gives the error directly. Entry 271 holds any card or coin detection until Alan's choices on the
check page arrive, so the photographs are not asked for yet.

## 9. The scoreboard, built (entry 291 section 7)

Option (a) now exists. Every build re-reads the same pictures and records, by condition, how many holes were found, how many false marks
were made, how far off the found holes were, how far off the registration was at every bull, and how long each picture took. Nothing in it
tunes anything: it measures, and a person reads it and proposes a change through the usual path.

**What is built.** The scoring and the synthetic conditions are in Core (`Scoreboard`, in `src/GroupLab.Core/Evaluation`); the command
line adds OpenCV's JPEG encoder and the reading of real photographs (`grouplab scoreboard`, `src/GroupLab.Cli/ScoreboardVerb.cs`).
The synthetic picture is the one of section 1: GL-CF25-LTR at 300 dpi with one synthetic hole on each of its 25 bulls, two seeds, each
condition alone, read as a photograph by the whole pipeline. A hole is found when a mark lies within 0.1 in of it, each mark counting for
one hole at most; a mark that matches no hole is a false mark. The registration error is the distance at every bull between where the truth
mapping puts it and where the registration put it, so it exists only where the truth mapping is known: the synthetic pictures. On a real
photograph the table gives the registration's own error between its markers instead.

**In every build.** `ScoreboardTests` reads all 13 conditions on both seeds (about 40 seconds in a Release build) and fails when any line
falls beyond the margin against `docs/scoreboard/synthetic-baseline.json`, naming the condition and both numbers: more than one hole lost
over the condition, more than one false mark gained, the median center error grown by more than 0.005 in, the worst by more than 0.03 in,
or the median registration error by more than 0.005 in. Time is reported and never failed on, because it depends on the machine. A line
may be marked `expectedToFail` in the baseline, so a known failure stays on the board rather than being hidden, and a fix shows as an
improvement; when a change makes a line better, the baseline moves in the same commit with the reason.

**The first synthetic table** (2026-09-29, seeds 291 and 292):

| Condition | Found | False marks | Median center error | Worst center error | Registration error at the bulls, median and worst |
|---|---|---|---|---|---|
| clean | 24, 25 of 25 | 0 | 0.006 in | 0.073 in | 0.0001, 0.0003 in |
| hard shadow, lower third at 55 percent | 24, 25 | 0 | 0.006 in | 0.073 in | 0.0001, 0.0003 in |
| soft shadow, a ramp to 55 percent | 24, 25 | 0 | 0.006 in | 0.074 in | 0.0001, 0.0003 in |
| a hand's shadow, a tilted ellipse at 50 percent | 22, 22 | 0 | 0.007 in | 0.074 in | 0.0001, 0.0003 in |
| dim, 45 percent | 24, 25 | 0 | 0.006 in | 0.073 in | 0.0001, 0.0003 in |
| uneven, a diagonal to 65 percent | 25, 25 | 0 | 0.006 in | 0.073 in | 0.0001, 0.0003 in |
| glare, clipped to white at its center | 22, 23 | 0 | 0.007 in | 0.073 in | 0.0001, 0.0003 in |
| curl, 15 px | 25, 24 | 0 | 0.006 in | 0.071 in | 0.014, 0.033 in |
| wave, 8 px, two waves across | 25, 24 | 0 | 0.006 in | 0.068 in | 0.014, 0.027 in |
| blur, sigma 1.5 px | 25, 25 | 1 | 0.006 in | 0.085 in | 0.0001, 0.0001 in |
| blur, sigma 3 px | 25, 25 | 0 | 0.008 in | 0.020 in | 0.0002, 0.0009 in |
| noise, sd 12 levels | 25, 24 | 0 | 0.007 in | 0.067 in | 0.0002, 0.0004 in |
| JPEG quality 40 | 25, 24 | 0 | 0.006 in | 0.068 in | 0.0002, 0.0003 in |

**Against section 1's table.** The scratch script of 2026-09-28 was deleted, so the exact geometry of each condition was not kept; the
scoreboard now fixes it in code. Most lines agree within a hole. The differences, and why:

- **Curl: 0 of 50 then, 49 of 50 now.** Entry 260 registered a bent sheet through every marker the day after the study. The curl is the
  one entry 260's own test uses, which reproduced the study's failure. What remains is geometry: the bulls between the markers are placed
  up to about 0.03 in off (median 0.014 in), where a flat sheet's are placed to 0.0003 in.
- **A hand's shadow: 22 of 25 on both seeds, where the study lost at most one.** This ellipse's soft edge crosses bulls; the study's was
  placed elsewhere. A hard-edged version of the same ellipse lost seven holes a picture. It is the local paper level of entry 260 again: a
  hole just inside a shadow's edge is judged against the paper outside it.
- **Glare** reproduces the study's 22 and 23 once the hot spot is clipped to white, as a camera clips it; the holes under it are lost.
- **Soft shadow and uneven light** lose one hole fewer than the study did, **blur 1.5 px** makes one false mark on one seed, and the
  seeds differ; these are within the margin and are the conditions' exact definitions, not a change in the detector.

**The real corpus, local only.** Real photographs are read where they lie, on the machine that has them, and never enter the public
repository, a public CI log or a public artifact. The corpus lives in `C:\Dev\grouplab-local\corpus\`, one folder per sitting, one folder
per picture, each holding the picture (no metadata: no GPS, no timestamps), the phone's own marking with its file path removed, the
picture's live-frame and analysis lines from the phone's log with their times removed, and a `truth.json`. The truth is the scan of the
same sheet where one exists (every hole, in the sheet's own inches), or the shot count on the sheet (count only), or nothing
(count unknown). To add a sitting:

1. Pull the phone's `files` folder into `C:\Dev\grouplab-local\camera-<date>\`, and strip any metadata from the pictures.
2. For each target picture, make a folder under `corpus\sitting-<date>\` with `picture.jpg`, `live.log`, `phone-marking.json` and
   `truth.json` (`picture`, `sheet`, `calibre`, `truth`, and `shots` in sheet inches or `count`; `condition` names the line it counts in).
3. Where the sheet was scanned, `grouplab scoreboard truth --scan <scan> --sheet <file.gltd.json> --calibre <in> --out <file>` writes
   every hole the scan reads, in the sheet's own inches; check it by hand against the scan before trusting it.
4. `grouplab scoreboard --corpus C:\Dev\grouplab-local\corpus --table <corpus>\scoreboard.md --out <corpus>\scoreboard.json`, and
   `--baseline` against the last run's file to see what a change did.

**The first real table** (2026-09-29, seven photographs of three sheets, each against its own 600 dpi scan). The four pictures of the
second camera sitting are all of one 6 ARC load sheet, taken with the torch at 1, 2, 9 and 15 degrees off square; the three of 2026-09-26
are the kitchen-counter photographs of entry 233.

| Picture | Found | False marks | Median center error | Worst center error | Marker residual | Time |
|---|---|---|---|---|---|---|
| 2026-09-29, 2 degrees off square | 25 of 25 | 0 | 0.012 in | 0.025 in | 0.0045 in | 1.0 s |
| 2026-09-29, 1 degree | 25 of 25 | 0 | 0.013 in | 0.045 in | 0.0041 in | 1.0 s |
| 2026-09-29, 9 degrees | 23 of 25 | 3 | 0.012 in | 0.081 in | 0.0034 in | 0.8 s |
| 2026-09-29, 15 degrees | 24 of 25 | 1 | 0.014 in | 0.086 in | 0.0035 in | 1.0 s |
| 2026-09-26, 6 ARC, Dominus K | 25 of 25 | 0 | 0.015 in | 0.034 in | 0.0038 in | 1.9 s |
| 2026-09-26, 6 ARC, Magnus S | 25 of 25 | 1 | 0.021 in | 0.037 in | 0.0046 in | 1.2 s |
| 2026-09-26, 6.5 Creedmoor | 22 of 23 | 1 | 0.026 in | 0.055 in | 0.0037 in | 1.5 s |

Together: 169 of 173 holes found, 6 false marks, a median center error between 0.012 and 0.026 in. The desktop's reading of the four new
pictures agrees with what the phone found on each. **What it says:** square-on pictures are clean; the two taken well off square lose
holes and make false marks, all in the right-hand column of bulls, where the stray marks are 0.3 to 0.5 in across, a hole and the paper
beside it read as one, placed up to 0.18 in from the hole. A mark twice the calibre and more is not a single hole, and it should be shown
for review rather than placed. That is a measured change to propose, not one made here.

**The first proposed fix, made** (entry 291 section 7 item 4). The four marks behind those failures measured 2.1 to 2.3 single holes
across, wider than any merge of whole holes, and in each the hole was the one part of it a disc 0.35 to 0.45 of a hole across could fill;
the rings' slivers could not hold it. So the data supported something better than holding the mark back: a mark twice a hole across or
more is opened that way, and where one hole-sized part is left the shot is placed on it and raised for review with the whole mark's size
(docs/DETECTION-PIPELINE.md, S8). Before and after, each picture against its scan:

| Picture | Found, before | False marks, before | Worst center error, before | Found, after | False marks, after | Worst center error, after |
|---|---|---|---|---|---|---|
| 2026-09-29, 2 degrees off square | 25 of 25 | 0 | 0.025 in | 25 of 25 | 0 | 0.025 in |
| 2026-09-29, 1 degree | 25 of 25 | 0 | 0.045 in | 25 of 25 | 0 | 0.045 in |
| 2026-09-29, 9 degrees | 23 of 25 | 3 | 0.081 in | 25 of 25 | 2 | 0.081 in |
| 2026-09-29, 15 degrees | 24 of 25 | 1 | 0.086 in | 25 of 25 | 0 | 0.083 in |
| 2026-09-26, 6 ARC, Dominus K | 25 of 25 | 0 | 0.034 in | 25 of 25 | 0 | 0.034 in |
| 2026-09-26, 6 ARC, Magnus S | 25 of 25 | 1 | 0.037 in | 25 of 25 | 1 | 0.037 in |
| 2026-09-26, 6.5 Creedmoor | 22 of 23 | 1 | 0.055 in | 22 of 23 | 1 | 0.055 in |

Together: 172 of 173 holes found where it was 169, and 4 false marks where it was 6. On the synthetic board every line is as it was except
a hand's shadow, which gained one hole (44 to 45 of 50, the second seed 22 to 23): a hole joined to the shadow's edge and refused as not
compact is now placed on its hole. The baseline moved with it. What is left on the 9 degree picture is two slivers of printed ring about
0.3 in long with no hole in them, and the worst center errors of 0.08 in are one hole 1.3 holes across, below the rule's reach; both are
the rings failing to cancel on the side of the sheet farthest from square, and that is the next thing to look at.

The 59 range photographs of 2026-09-20 are not in the corpus yet: their truth is per sheet, not per hole, and they need their own truth
files before they can be scored the same way.
