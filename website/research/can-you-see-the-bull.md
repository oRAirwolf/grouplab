---
title: "Can you see the bull? Aim points and optics at 100 yards"
description: "Through a top-tier 36x scope the center of GroupLab's original bull was visible at 100 yards; at 10x nobody could center on it. The arithmetic of why, nine candidate aim points, and what two shooters scored through four scopes."
group: Range tests
number: 8
written: 2026-09-22
data_date: "2026-09-20 observations; the test shot 2026-09-26"
samples: "Four optics, nine aim point designs, two observers, 207 scores; three shots at each of four designs"
state: published
found: "at 10x nobody could center on GroupLab's original bull through any of three high power scopes, while the designs whose center subtends about 3.4 arcminutes or more, a black diamond, a black square with a white center and a 2 inch bull, could. Below 6x only the 2 inch bull could be centered. A feature a shooter must see needs roughly 3 to 4 arcminutes at the lowest magnification the sheet is for."
sure: "the geometry is exact. The scores are two people on one afternoon, so they show where the line falls, not a precise threshold. They agree with the 3 to 4 arcminute rule at every magnification tested."
data:
  - data/apparent-size.csv
  - data/scores-2026-09-26.csv
  - data/groups-2026-09-26.csv
sources:
  - "Michael Bach, Visual acuity and hyperacuity (vernier acuity is 5 to 10 times finer than resolution). https://michaelbach.de/ot/lum-hyperacuity/"
  - "The clinical use of vernier acuity, Frontiers in Neuroscience, 2021. https://pubmed.ncbi.nlm.nih.gov/34675763/"
  - "Load development target design discussion, AccurateShooter forum. https://forum.accurateshooter.com/threads/need-a-good-target-design-for-load-development.3943734/"
  - "GroupLab target definition GL-CF25-LTR-D, ring set 'std' (targets/GL-CF25-LTR-D.gltd.json)."
---

## What happened on the range

On 2026-09-20 the developer shot five 25-bull GroupLab sheets at 100 yards with three rifles:

| Rifle | Scope | What the developer saw |
|---|---|---|
| 6.5 Creedmoor | Vortex Razor HD Gen III 6-36x56 FFP, EBR-7D Mil | Could make out the bull centers |
| 6 ARC | DNT TheOne 7-35x56 FFP, TOR Mil | Slightly harder |
| .22 LR | Vortex Strike Eagle 5-25x56 FFP, EBR-7C Mil | Very difficult, from glass quality and lower magnification |

If a target is hard to see through a top-tier scope at 100 yards, it is not a target most shooters can use.

## The arithmetic

What matters is not how big a feature is on paper but how big it looks at your eye. Through a scope, that is the angle the feature covers at the target, multiplied by the magnification. At 100 yards, one inch covers about 0.955 arcminutes, so:

**apparent size (arcminutes) = 0.955 x size in inches x magnification**, at 100 yards.

![Apparent size of printed features through a scope](/research/can-you-see-the-bull/figures/apparent-size.png)

GroupLab's original bull is a 1 inch ring and a half inch ring, each drawn with a line about 0.03 inch wide, and a 0.10 inch center dot.

| Feature | At 25x | At 35x | At 36x |
|---|---|---|---|
| 0.03 in ring line | 0.72 arcmin | 1.00 | 1.03 |
| 0.10 in center dot | 2.39 | 3.34 | 3.44 |
| 0.25 in feature | 5.97 | 8.36 | 8.59 |

A healthy eye resolves detail down to about 1 arcminute when contrast is perfect, the light is good and the air is still. On a range there is mirage, glass that loses a little contrast, and a reticle sitting on top of the target. So the rings sit exactly at the limit through a 36x scope and below it through a 25x scope. The center dot is bigger, but at high magnification it is about the size of the reticle's own center, which covers it.

## Bigger does not mean less precise

It is natural to think a smaller aim point means more precise aiming. It does not, and the reason is how the eye works.

Seeing a tiny detail uses **resolution acuity**, about 1 arcminute. Centering a crosshair on a symmetric shape uses a different ability, **vernier acuity**: judging whether two edges line up or whether gaps are equal. Vernier acuity is 5 to 10 times finer than resolution acuity. You can center a reticle on a bold 1 inch diamond by judging the four corners far more finely than 1 inch. This is why benchrest and load development shooters favor bold squares and diamonds.

So the design rule is not "make it small". It is **make every feature you need to see large enough to see clearly, and make the shape symmetric so you can center on it.**

## A working rule to test

The proposal: every feature a shooter must see should look at least 3 to 4 arcminutes wide at the lowest magnification the sheet is meant for, to leave margin for mirage and ordinary glass.

![Smallest feature for 4 arcminutes](/research/can-you-see-the-bull/figures/feature-size-needed.png)

| Magnification | Smallest feature at 100 yd | At 50 yd | At 25 yd |
|---|---|---|---|
| 8x | 0.52 in | 0.26 in | 0.13 in |
| 10x | 0.42 in | 0.21 in | 0.10 in |
| 15x | 0.28 in | 0.14 in | 0.07 in |
| 25x | 0.17 in | 0.08 in | 0.04 in |

## Nine candidates

The card was made for a test planned for 2026-09-23 and shot on 2026-09-26. Every design except I fits GroupLab's current 1.5 inch bull spacing, so a winner could replace the bull without losing bulls per sheet.

![The aim point test card](/research/can-you-see-the-bull/figures/test-card.png)

| | Design | Idea |
|---|---|---|
| A | Original GroupLab bull | The control |
| B | Same rings, bold lines, bigger dot | Does line weight alone fix it? |
| C | Black diamond, white center | The classic load development shape |
| D | Black square with a thin white cross | Center by the four quadrants |
| E | Black square, white center square, small dot | Holes near the aim stay visible |
| F | Square outline, center dot | Less ink, bold edges |
| G | Open cross with no center | The reticle sits in the gap |
| H | Four pointers aimed at the center | Symmetry without a center to cover |
| I | 2 inch bull | Fewer bulls per page |

## The test

Four optics at 100 yards, each scored 0 (cannot see the center), 1 (can see it but cannot center confidently) or 2 (can center confidently) for every design:

- Vortex Razor HD Gen III 6-36x56 at 10x, 18x, 25x and 36x
- DNT TheOne 7-35x56 at 10x, 18x, 25x and 35x
- Vortex Strike Eagle 5-25x56 at 10x, 18x and 25x
- Primary Arms PLxC 1-8x24 FFP at 4x, 6x and 8x

25x on all three high power scopes is the like-for-like comparison of glass. The test was shot on 2026-09-26, at 100 yards, by the developer and a friend, who scored every scope but the Strike Eagle.

**The PLxC rows on the sheet were wrong, and the sheet is fixed.** The printed sheet asked for the PLxC at 4x, 8x and 8x again, the last a 50 yard row whose different distance was printed like every other cell. Both shooters tested 4x, 6x and 8x at 100 yards and wrote 6x over the second row. The friend's older sheet had its spare scope rows printed 10x, 18x and "max", copied from the high power scopes, and the PLxC's 4, 6 and 8 were written over them. The sheet is now built from a table of each scope's real range: it refuses a magnification the scope does not have or one asked for twice, and a change of distance gets its own bold heading.

## Results

Every score, the developer's first and the friend's second where both scored. 0 cannot see the center, 1 can see it but cannot center on it, 2 can center confidently.

| Scope | Mag | A | B | C | D | E | F | G | H | I |
|---|---|---|---|---|---|---|---|---|---|---|
| Razor HD | 10x | 0 / 0 | 1 / 1 | 2 / 2 | 1 / 0 | 2 / 2 | 1 / 1 | 1 / 2 | 2 / 2 | 2 / 0 |
| Razor HD | 18x | 1 / 1 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 1 | 2 / 1 | 2 / 1 |
| Razor HD | 25x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| Razor HD | 36x | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 1 | 2 / 1 | 2 / 2 |
| DNT | 10x | 0 / 2 | 1 / 1 | 2 / 0 | 1 / 1 | 2 / 2 | 2 / 2 | 1 / 0 | 2 / 1 | 1 / 2 |
| DNT | 18x | 1 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 1 |
| DNT | 25x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| DNT | 35x | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 | 2 / 2 |
| Strike Eagle | 10x | 0 | 1 | 2 | 1 | 2 | 2 | 1 | 1 | 2 |
| Strike Eagle | 18x | 0 | 2 | 2 | 0 | 2 | 2 | 1 | 2 | 2 |
| Strike Eagle | 25x | 1 | 2 | 2 | 1 | 2 | 2 | 1 | 2 | 2 |
| PLxC | 4x | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 1 | 0 / 0 | 0 / 0 | 1 / 0 | 2 / 1 |
| PLxC | 6x | 0 / 0 | 0 / 1 | 1 / 1 | 0 / 0 | 1 / 1 | 0 / 2 | 1 / 0 | 1 / 0 | 2 / 2 |
| PLxC | 8x | 0 / 0 | 0 / 0 | 1 / 0 | 1 / 0 | 1 / 0 | 1 / 1 | 1 / 0 | 2 / 0 | 2 / 0 |

Three of the developer's Strike Eagle cells were written unclearly, A at 10x and 18x like an 8 and D at 18x like a 0 with a tail; the developer confirmed all three are 0. The planning session transcribed both sheets separately from full resolution scans, and the two transcriptions agree in every cell. At 8x the PLxC's image was blurry at 100 yards, and the designs were noticeably harder to see than at 6x, which is where the friend's 8x row comes from.

**The two observers agree only moderately.** In the 81 cells both scored, they gave the same score in 53 and scores within one of each other in 75; Cohen's kappa is 0.44, 0.52 with the near misses given half credit. They part most at 10x: A through the DNT, 0 for the developer and 2 for the friend, and I through the Razor HD, the other way round. Two people on one afternoon is a first look, and the totals should be read with that in mind.

**The current bull cannot be centered at 10x.** A scored 0 for the developer through all three high power scopes at 10x, and 0 for the friend through the Razor HD. At 25x and above almost everything is centered through good glass, so the magnification a shooter zeros and tests at, not the best glass, decides the design.

**The working rule held.** The center of each design, and what it subtends through the scope at 100 yards:

| Design | Center feature | arcminutes at 4x | 6x | 8x | 10x |
|---|---|---|---|---|---|
| A | 0.10 in dot | 0.4 | 0.6 | 0.8 | 1.0 |
| B | 0.22 in dot | 0.8 | 1.3 | 1.7 | 2.1 |
| C | 0.36 in white center | 1.4 | 2.1 | 2.8 | 3.4 |
| D | 0.06 in white cross | 0.2 | 0.3 | 0.5 | 0.6 |
| E | 0.36 in white center | 1.4 | 2.1 | 2.8 | 3.4 |
| F | 0.20 in dot | 0.8 | 1.1 | 1.5 | 1.9 |
| G | 0.40 in open gap | 1.5 | 2.3 | 3.1 | 3.8 |
| H | 0.40 in between the tips | 1.5 | 2.3 | 3.1 | 3.8 |
| I | 0.60 in white center | 2.3 | 3.4 | 4.6 | 5.7 |

At 10x the designs whose center is 3.4 arcminutes or more, C, E and I, are the ones centered through the high power scopes: E by both observers through every one, C with one exception (the friend through the DNT) and I with two. At 4x nothing but the 2 inch bull scores at all, and its white center is the only feature near 3 arcminutes. A feature needs roughly 3 to 4 arcminutes at the lowest magnification a sheet is for, which is about 1 inch at 100 yards through 4x.

**A crosshair covers a small center, whatever the glass.** Through the Razor HD, whose reticle has a fine center crosshair, the centers of D and G were very hard to see: the crosshair sat exactly where the design's center was. The DNT has only a small center dot and did not cover them. So a design whose center is a small feature, a thin cross or an open gap, fails under a crosshair reticle, and G's idea, a gap for the reticle to sit in, works only when the reticle is smaller than the gap.

**What the shooters chose.** The developer: C or E, the edge to C, with "C, F, I good" in the notes. The friend: F the favorite, and I not liked.

**Three shots at four designs.** The developer fired three shots at each of A, C, E and I with one rifle and load. Every group landed up and left of its aim point by about the same amount, which is the rifle's zero on the day rather than the design. Measured center to center from the scan:

| Design | Group across (extreme spread) | Center of the group from the aim |
|---|---|---|
| A | 0.42 in | 0.70 in left, 0.58 in high |
| C | 0.80 in | 0.34 in left, 0.51 in high |
| E | 0.41 in | 0.28 in left, 0.86 in high |
| I | about 0.2 in: one ragged hole about 0.43 in across in the black | 0.19 in left, 0.65 in high |

C's spread comes from one shot in its white center and two a long way up and left. **None of these differ measurably.** With three shots a design, one design's spread would have to be about three times another's before the difference is more than chance; C against E is 2.2 times, p = 0.16. Three shots can only show a very large difference, and there is none here.

## What happens next

The winning design becomes a new ring set in the GroupLab library, tested on real sheets before it replaces anything. This test narrows it: **E or C** for sheets shot at 10x and above, E centered by both observers through every high power scope at 10x and C by all but one; **not D or G**, whose centers a crosshair covers; and **I**, or a design with a center of about an inch, for anything shot below 6x. The choice is the developer's. A follow-up test covers 1x red dots and prisms, low power variables and medium power variables at distances suited to each (see "Aim points for 1x to high power optics").

**Both are now in the library** (entry 243), beside the usual bull and not in its place, until they have been shot on real sheets. E is drawn as discs. C is drawn as tested, a black diamond 1.25 in point to point with a 0.36 in white diamond center, with one change: **the library's C has a small black dot in its center, and the card's C had none.** The dot is 0.10 in on the 0.36 in center, E's proportion. It is not the aim: the aim is still the white diamond and its points. It is there for high power, where a shooter who can see it has something finer to split, and for GroupLab, which finds a bull's center from the edges of what is printed and has one more edge to use. Every C diamond stands on a point, so its points lie on the vertical and horizontal lines through the aim and a crosshair lines up with the shape even where neither the dot nor the center can be made out. "Made for your optic" can make either shape, sized by the same rule.

## What this means

**Try the card before you commit to a design.** The geometry in this article is exact and the four arcminute working rule is a proposal, not a result. What the eye does with a particular mark, in particular light, through particular glass, is a thing to test rather than calculate.

**A bull you cannot see clearly costs you group size that is not your rifle's.** If the aiming mark is at the edge of what you can resolve, your hold varies, and that variation lands in the measurement as dispersion. It is the cheapest source of error on this list to remove.

**And do not choose a bull because it looks good on screen.** A sheet is read at arm's length on a bench, not at 100 percent zoom on a monitor.
