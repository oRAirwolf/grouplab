---
title: "Did the suppressor move the point of impact?"
description: Two 25 shot sheets from one rifle and one load, one with each suppressor. The second suppressor's shots centered about a quarter of an inch lower at 100 yards, and the test just sees it.
group: Range tests
number: 31
written: 2026-09-27
data_date: 2026-09-26
samples: 25 shots with a Thunder Beast Dominus K and 25 with a Magnus S, one 6 ARC rifle and load, 100 yd
state: published
found: With the Magnus S the shots centered 0.28 in lower at 100 yards than with the Dominus K (0.27 MOA), and no different across. Hotelling's test puts that at p = 0.049 with every shot counted, and p = 0.009 without the four shots that landed off their bulls. The spread did not change.
sure: One sheet of each, shot in one order half an hour apart. The test says the two sheets' centers differ; it cannot say whether the suppressor, the barrel's heat after 25 shots, the swap itself or the light did it.
data:
  - data/offsets-2026-09-26.csv
  - data/results.json
sources:
  - "The two sheets, their load blocks and consent: `samples/PROVENANCE.md`, \"The 2026-09-26 range day\"."
  - "What the developer asked, and the order the sheets were shot in: `docs/NOTES-FROM-PLANNING.md`, entries 226, 229 and 230."
  - "The tests: `docs/STATISTICS.md` sections 8.1 to 8.3. The numbers are worked out by `figures/make_figures.py` from the offsets GroupLab measured."
---

## The test

The developer shot two 25 bull load development sheets at 100 yards from the same 18 inch AR-15 with the same ammunition: 105 gr Aeromatch over 24.2 gr of N135 in Starline brass, GM205MAR primers, 2.250 in. One shot per bull on each sheet. The only change between the sheets was the suppressor: a **Thunder Beast Dominus K** on the first sheet, a **Magnus S** on the second, about 20 to 30 minutes later. Same rifle, same position, same rest.

The question every shooter with two cans asks: does swapping them move the zero?

## What happened on the paper

![Every shot's offset from its own bull on both sheets, with each sheet's mean and its 95 percent region](/research/suppressor-shift/figures/offsets.png)

Each dot is one shot, measured from the bull it was fired at, so all 25 bulls on a sheet stack onto one point of aim. By eye the Dominus K shots sit high and the Magnus S shots sit low, and that is what the chart shows: the two means, the crosses, are about a quarter of an inch apart, one above the other.

Four shots landed off their bulls altogether: on the Dominus K sheet, bull 2's shot hit about 1.4 in above it near the title line, and bull 24's landed below it; on the Magnus S sheet, bull 5's hit about an inch above it and half an inch left, toward the corner code, and bull 24's landed below it. GroupLab gave each of the four to the bull it was fired at, because it matches every shot to a bull at once rather than each to its nearest, and a sheet with one shot per bull leaves only one way to do that.

## What the numbers say

| | Dominus K | Magnus S |
|---|---|---|
| shots | 25 | 25 |
| center, from the aim | 0.04 in right, 0.21 in high | 0.08 in right, 0.07 in low |
| spread (sigma about its own center) | 0.345 in | 0.340 in |

**The shift, Magnus S against Dominus K:** 0.28 in lower (95 percent interval 0.06 to 0.51 in lower) and 0.04 in right (0.13 in left to 0.20 in right). That is **0.27 MOA or 0.08 mil**, about one quarter MOA click and less than one 0.1 mil click.

**Is it real?** Hotelling's two-sample test on the 25 by 25 offsets, the standard test for whether two groups share a center, gives **p = 0.049**. A permutation test of the same statistic, which assumes nothing about the shape of the groups, gives p = 0.047. Taking out the four shots that landed off their bulls, the shift is the same size, 0.28 in lower, and the test gives **p = 0.009**.

Read the first number as the result. Removing shots after seeing where they went is choosing the answer, and the four off-bull shots were fired like the rest. What the second number adds is that the shift does not depend on them: they widen the spread, and the spread is what hid the shift.

**Did the spread change?** No sign of it. The ratio of the two spreads is 1.02, with a 95 percent interval from 0.76 to 1.35, p = 0.91. That is not proof they group the same; 25 shots a side can only see a difference of about a third either way.

## What the test cannot separate

The test says the two sheets' centers differ, by about a quarter of an inch, and that chance alone would do that about one time in twenty. It does not say the suppressor did it. Everything else that changed between the sheets changed at the same moment:

- **The order.** The Dominus K sheet was shot first. The Magnus S sheet came after 25 more shots through the barrel and a 20 to 30 minute cool down, so the barrel's temperature and fouling were not the same.
- **The swap itself.** Taking one suppressor off and threading the other on can move the point of impact by itself, and a second mounting of the same can may not return to the first.
- **The light**, over half an hour at the range.
- **One sheet of each.** One sheet is one sample of each suppressor on one afternoon.

**How to separate them.** Shoot the pair again in the other order, Magnus S first, and then once more with the same can on both sheets. If the shift follows the can, it is the can. If it follows the order, it is the barrel. If the same can on both sheets shifts as much, it is the remounting.

## What this means

**A quarter of an inch at 100 yards is one click.** If you swap between these two cans, expect to check your zero, and do not be surprised by a click of elevation. Whether it is always the same click, in the same direction, this test cannot tell you.

**The spread, not the shift, is what needs the shots.** A shift of means shows up with far fewer shots than a difference in group size, which is why 25 a side can see a quarter of an inch but could not see a difference in spread smaller than about a third.

**No shift that this test can detect** would also have been a result, and it would have been written up the same way.
