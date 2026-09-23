---
title: "Velocity SD from 5, 10 and 20 shots"
description: "A five-shot chronograph string that reads SD 10 is consistent with a true SD anywhere from 6 to 29 ft/s. How much a velocity SD can tell you at each string length, and why extreme spread in velocity grows with every shot."
group: Measuring groups
number: 14
written: 2026-09-22
data_date: "Closed-form statistics and simulation, seed 2026"
samples: "Exact chi-square intervals; 60 simulated strings; 200,000 simulated strings per row of the ES table"
state: draft
found: "if you chronograph five shots and measure an SD of 10 ft/s, the true SD of that ammunition is, with 95 percent confidence, somewhere between 6.0 and 28.7 ft/s. With 20 shots the same reading narrows to 7.6 to 14.6. Single-digit SD claims from five-shot strings are mostly luck."
sure: "exact for normally distributed velocities, which is a good description of well-made ammunition. A string with a genuine outlier (a bad primer, a light charge) is worse than this, not better."
data:
  - data/sd-interval-by-shots.csv
  - data/repeat-strings.csv
  - data/es-over-sd.csv
sources:
  - "NIST/SEMATECH e-Handbook of Statistical Methods, chi-square test and confidence interval for the standard deviation. https://www.itl.nist.gov/div898/handbook/eda/section3/eda358.htm"
  - "The range of a sample and its relation to the standard deviation. https://en.wikipedia.org/wiki/Range_(statistics)"
  - "GroupLab statistics reference, section 12 on velocity and vertical dispersion (docs/STATISTICS.md)."
---

## Why velocity SD matters, and why it is hard to measure

Velocity variation turns into vertical spread at distance: a slower bullet drops more. So handloaders chase a low standard deviation (SD) of muzzle velocity, and chronographs make it easy to get a number.

The number is easy. Knowing whether it means anything is the hard part, because an SD measured from a handful of shots is itself very uncertain.

## The interval, string by string

Suppose your chronograph says SD 10 ft/s. How far could the truth be from that?

![Interval on true SD after measuring 10 ft/s](/research/velocity-sd-small-samples/figures/sd-interval.png)

| Shots in the string | True SD is between (95 percent) |
|---|---|
| 3 | 5.2 and 62.8 ft/s |
| 5 | 6.0 and 28.7 |
| 10 | 6.9 and 18.3 |
| 20 | 7.6 and 14.6 |
| 30 | 8.0 and 13.4 |
| 50 | 8.4 and 12.5 |

The interval is lopsided: it stretches much further above your reading than below it. Small samples are more likely to understate variation than to overstate it, because a few shots rarely include the extremes.

## The same ammunition, measured sixty times

Here one simulated batch of ammunition with a true SD of exactly 10 ft/s is chronographed thirty times in strings of five, and thirty times in strings of twenty.

![Repeated chronograph strings of one load](/research/velocity-sd-small-samples/figures/repeat-strings.png)

The five-shot strings read anywhere from 2.1 to 19.4 ft/s. Somebody who shot the 2.1 string would believe they had world-class ammunition; somebody who shot the 19.4 string would start over. The twenty-shot strings stay between 6.3 and 13.2.

## Velocity extreme spread grows with every shot

Extreme spread (ES) in velocity, the fastest minus the slowest, has the same problem as extreme spread on paper: it only looks at two shots, and it grows just because you fired more.

| Shots in the string | Average ES as a multiple of the true SD |
|---|---|
| 3 | 1.7 |
| 5 | 2.3 |
| 10 | 3.1 |
| 20 | 3.7 |
| 30 | 4.1 |

So an ES of 25 ft/s over five shots and an ES of 40 ft/s over twenty shots can describe the same ammunition. Comparing ES values from strings of different lengths is comparing different things.

## What to do with this

- **Chronograph more rounds.** Ten is much better than five; twenty is better again. If you are shooting a 25-shot GroupLab sheet anyway, record every shot's velocity.
- **Compare SDs with their intervals.** Two loads with five-shot SDs of 8 and 12 ft/s are not shown to be different. With twenty shots each, they might be.
- **Look at the paper too.** Velocity SD is one input to vertical spread at distance. GroupLab can compare the vertical spread you measured with what your velocity spread predicts, which tells you whether velocity is really the limit or whether the rifle, the rest or the shooter is.
- **Do not chase a number the sample cannot support.** Nothing in this article says small SDs do not matter. It says a five-shot string cannot tell you whether you have one.
