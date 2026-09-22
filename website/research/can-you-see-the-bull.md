---
title: "Can you see the bull? Aim points and optics at 100 yards"
description: "Through a top-tier 36x scope the centre of GroupLab's original bull was visible at 100 yards; through a good 25x scope it nearly disappeared. The arithmetic of why, nine candidate aim points, and a side-by-side test across four scopes."
group: Range tests
number: 8
written: 2026-09-22
data_date: "2026-09-20 observations; test results 2026-09-23 (pending)"
samples: "Four optics, nine aim point designs, two observers (results pending)"
status: draft
found: "see the article"
sure: "the geometry is exact. The 4 arcminute working rule is a proposal to test. The side-by-side results from the 2026-09-23 range test will be added below."
data:
  - data/apparent-size.csv
sources:
  - "Michael Bach, Visual acuity and hyperacuity (vernier acuity is 5 to 10 times finer than resolution). https://michaelbach.de/ot/lum-hyperacuity/"
  - "The clinical use of vernier acuity, Frontiers in Neuroscience, 2021. https://pubmed.ncbi.nlm.nih.gov/34675763/"
  - "Load development target design discussion, AccurateShooter forum. https://forum.accurateshooter.com/threads/need-a-good-target-design-for-load-development.3943734/"
  - "GroupLab target definition GL-CF25-LTR-D, ring set 'std' (targets/GL-CF25-LTR-D.gltd.json)."
---

## What happened on the range

On 2026-09-20 Alan shot five 25-bull GroupLab sheets at 100 yards with three rifles:

| Rifle | Scope | What he saw |
|---|---|---|
| 6.5 Creedmoor | Vortex Razor HD Gen III 6-36x56 FFP, EBR-7D Mil | Could make out the bull centres |
| 6 ARC | DNT TheOne 7-35x56 FFP, TOR Mil | Slightly harder |
| .22 LR | Vortex Strike Eagle 5-25x56 FFP, EBR-7C Mil | Very difficult, from glass quality and lower magnification |

If a target is hard to see through a top-tier scope at 100 yards, it is not a target most shooters can use.

## The arithmetic

What matters is not how big a feature is on paper but how big it looks at your eye. Through a scope, that is the angle the feature covers at the target, multiplied by the magnification. At 100 yards, one inch covers about 0.955 arcminutes, so:

**apparent size (arcminutes) = 0.955 x size in inches x magnification**, at 100 yards.

![Apparent size of printed features through a scope](/research/can-you-see-the-bull/figures/apparent-size.png)

GroupLab's original bull is a 1 inch ring and a half inch ring, each drawn with a line about 0.03 inch wide, and a 0.10 inch centre dot.

| Feature | At 25x | At 35x | At 36x |
|---|---|---|---|
| 0.03 in ring line | 0.72 arcmin | 1.00 | 1.03 |
| 0.10 in centre dot | 2.39 | 3.34 | 3.44 |
| 0.25 in feature | 5.97 | 8.36 | 8.59 |

A healthy eye resolves detail down to about 1 arcminute when contrast is perfect, the light is good and the air is still. On a range there is mirage, glass that loses a little contrast, and a reticle sitting on top of the target. So the rings sit exactly at the limit through a 36x scope and below it through a 25x scope. The centre dot is bigger, but at high magnification it is about the size of the reticle's own centre, which covers it.

## Bigger does not mean less precise

It is natural to think a smaller aim point means more precise aiming. It does not, and the reason is how the eye works.

Seeing a tiny detail uses **resolution acuity**, about 1 arcminute. Centring a crosshair on a symmetric shape uses a different ability, **vernier acuity**: judging whether two edges line up or whether gaps are equal. Vernier acuity is 5 to 10 times finer than resolution acuity. You can centre a reticle on a bold 1 inch diamond by judging the four corners far more finely than 1 inch. This is why benchrest and load development shooters favour bold squares and diamonds.

So the design rule is not "make it small". It is **make every feature you need to see large enough to see clearly, and make the shape symmetric so you can centre on it.**

## A working rule to test

We propose that every feature a shooter must see should look at least 3 to 4 arcminutes wide at the lowest magnification the sheet is meant for, to leave margin for mirage and ordinary glass.

![Smallest feature for 4 arcminutes](/research/can-you-see-the-bull/figures/feature-size-needed.png)

| Magnification | Smallest feature at 100 yd | At 50 yd | At 25 yd |
|---|---|---|---|
| 8x | 0.52 in | 0.26 in | 0.13 in |
| 10x | 0.42 in | 0.21 in | 0.10 in |
| 15x | 0.28 in | 0.14 in | 0.07 in |
| 25x | 0.17 in | 0.08 in | 0.04 in |

## Nine candidates

Alan took this card to the range on 2026-09-23. Every design except I fits GroupLab's current 1.5 inch bull spacing, so a winner could replace the bull without losing bulls per sheet.

![The aim point test card](/research/can-you-see-the-bull/figures/test-card.png)

| | Design | Idea |
|---|---|---|
| A | Original GroupLab bull | The control |
| B | Same rings, bold lines, bigger dot | Does line weight alone fix it? |
| C | Black diamond, white centre | The classic load development shape |
| D | Black square with a thin white cross | Centre by the four quadrants |
| E | Black square, white centre square, small dot | Holes near the aim stay visible |
| F | Square outline, centre dot | Less ink, bold edges |
| G | Open cross with no centre | The reticle sits in the gap |
| H | Four pointers aimed at the centre | Symmetry without a centre to cover |
| I | 2 inch bull | Fewer bulls per page |

## The test

Four optics at 100 yards, each scored 0 (cannot see the centre), 1 (can see it but cannot centre confidently) or 2 (can centre confidently) for every design:

- Vortex Razor HD Gen III 6-36x56 at 10x, 18x, 25x and 36x
- DNT TheOne 7-35x56 at 10x, 18x, 25x and 35x
- Vortex Strike Eagle 5-25x56 at 10x, 18x and 25x
- Primary Arms PLxC 1-8x24 FFP at 4x and 8x at 100 yards, and 8x at 50 yards

25x on all three high power scopes is the like-for-like comparison of glass. A friend's scope was scored as well.

## Results

**[Placeholder: results from 2026-09-23. Table of scores by design, scope and magnification; light and mirage; each observer's preferred design; any shots fired at A and the favourite.]**

## What happens next

The winning design becomes a new ring set in the GroupLab library, tested on real sheets before it replaces anything. A follow-up test covers 1x red dots and prisms, low power variables and medium power variables at distances suited to each (see "Aim points for 1x to high power optics").
