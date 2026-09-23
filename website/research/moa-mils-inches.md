---
title: "MOA, mils and inches: one group four ways"
description: "Inches and centimetres measure the paper; MOA and mils measure the angle, so they mean the same thing at any distance. What each unit is, how they convert, and why GroupLab will not show an angle until it knows the distance."
group: Measuring groups
number: 15
written: 2026-09-22
data_date: "Worked example, one simulated group, seed 2026"
samples: "One 10-shot example group"
state: draft
found: "nothing new, deliberately. This is a reference page. The one number people most often get wrong: a true minute of angle is 1.047 inches at 100 yards, not 1 inch. The difference is 4.7 percent, enough to matter when you compare groups or count clicks at distance."
sure: "these are definitions. The conversions are exact and match the constants in GroupLab's statistics code."
data:
  - data/example-group.csv
  - data/unit-sizes.csv
sources:
  - "GroupLab statistics reference, sections 12.5 and 13 (docs/STATISTICS.md), and src/GroupLab.Core/Statistics/Angular.cs."
  - "David Wollschlaeger, shotGroups R package, angular conversion functions. https://cran.r-project.org/package=shotGroups"
  - "Minute and second of arc. https://en.wikipedia.org/wiki/Minute_and_second_of_arc"
  - "Milliradian. https://en.wikipedia.org/wiki/Milliradian"
---

## Two kinds of unit

**Linear units** (inches, millimetres, centimetres) measure the paper. A half-inch group is half an inch across, whatever the distance.

**Angular units** (MOA, mils) measure the angle the group covers as seen from the rifle. A 1 MOA group at 100 yards and a 1 MOA group at 500 yards are equally good shooting, even though the second is five times larger on paper. That is why angular units are the fair way to compare groups shot at different distances, and why scope adjustments are made in them.

![One group shown four ways](/research/moa-mils-inches/figures/one-group-four-ways.png)

The same ten shots, measured four ways. The dots do not move. Mean radius reads 0.246 inch, 0.626 cm, 0.235 MOA or 0.068 mil.

## The units

| Unit | What it is | Size at 100 yards | Size at 100 metres |
|---|---|---|---|
| **MOA** (minute of angle) | 1/60 of a degree | 1.047 in | 2.91 cm |
| **IPHY** (inches per hundred yards, "shooter's MOA") | exactly 1 inch at 100 yards | 1.000 in | 2.78 cm |
| **Mil** (milliradian, also MRAD) | 1/1000 of a radian | 3.600 in | 10.0 cm |

A few points that trip people up:

- **MOA is not an inch.** 1 MOA at 100 yards is 1.047 inches. Calling it an inch is close at 100 yards and adds up at distance: at 1,000 yards, 10 MOA is 104.7 inches, not 100.
- **"Mil" on a scope means milliradian.** There is also a military "mil" used for artillery (6,400 to the circle), which is slightly different. GroupLab knows both, but when a shooter or a scope says mil, it means milliradian.
- **Mils are neat in metric.** One mil is 10 centimetres at 100 metres, 1 metre at 1,000 metres. That is why mil scopes and metric distances pair so well.

## What one unit covers at distance

![What one unit covers at each distance](/research/moa-mils-inches/figures/unit-size-by-distance.png)

| Distance | 1 MOA | 1 mil | One 1/4 MOA click | One 0.1 mil click |
|---|---|---|---|---|
| 100 yd | 1.05 in | 3.60 in | 0.26 in | 0.36 in |
| 200 yd | 2.09 in | 7.20 in | 0.52 in | 0.72 in |
| 300 yd | 3.14 in | 10.80 in | 0.79 in | 1.08 in |
| 600 yd | 6.28 in | 21.60 in | 1.57 in | 2.16 in |
| 1,000 yd | 10.47 in | 36.00 in | 2.62 in | 3.60 in |

## Converting between them

- 1 mil = 3.438 MOA
- 1 MOA = 0.2909 mil
- 1 MOA = 1.047 IPHY; 1 IPHY = 0.955 MOA
- Inches to MOA at a distance: inches divided by (1.047 times yards divided by 100)
- Inches to mils at a distance: inches divided by (3.6 times yards divided by 100)

GroupLab does these conversions with the exact trigonometric form, not the small-angle shortcut, so that its numbers match the shotGroups package it validates against. At normal shooting distances the two agree to many decimal places; at very short distances the exact form is the correct one.

## Why GroupLab asks for the distance

An angle needs a distance. Without one, a half-inch group could be 0.5 MOA at 100 yards or 0.1 MOA at 500. So GroupLab stores everything as a real distance on the paper and only shows MOA or mil once it knows how far away the target was. With no distance, the angular columns are not shown at all, rather than shown as zero or guessed.

GroupLab can show inches, centimetres, MOA and mil side by side. It uses true MOA by default and offers IPHY for those who prefer it, and the metric and imperial switch changes every figure in the application at once.
