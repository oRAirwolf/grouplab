## 2026-09-18, entry 104: question 19 confirmed and my error behind it, the flyer card's exceedance measured, and the render that was never made dark

**Status: open.**

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
