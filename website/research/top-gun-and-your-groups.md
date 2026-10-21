---
title: "Does your rifle group as well as Top Gun says it should?"
description: "Top Gun is a trend of how small a rifle and load should group, from the recoil it makes and the rifle's weight. One or two groups cannot tell you whether you beat it or fall short. Here is how many shots can, and what to check before you blame the rifle."
group: Measuring groups
number: 41
written: 2026-10-21
data_date: "Worked out 2026-10-21 by simulation, seed 2026, and GroupLab's own statistics"
samples: "Simulated shots from a round normal spread, 20,000 sets of groups and 4,000 sets of shots at each size; no shots were fired for this article"
state: published
no_figure: "The numbers are few and the article gives them as tables."
found: "one five-shot group lands anywhere from 0.67 to 1.36 times the rifle's true average five-shot group, 8 times in 10. From 20 shots a rifle 25 percent worse than the trend is shown clearly wider only 58 percent of the time; from 50 shots, 90 percent."
sure: "these are exact properties of a round normal spread, worked out by simulation that reruns to the same numbers. A real rifle adds flyers, a walking zero and the shooter, which only make the picture noisier. The trend itself is a trend: real rifles sit on both sides of it."
sources:
  - "Modern Advancements in Long Range Shooting, Volume III, chapter 3, for the trend and what it measures (the average extreme spread of five-shot groups). Cited by title and chapter only; nothing is quoted from it."
  - "What GroupLab shows beside the trend, and how: docs/STATISTICS.md, the range statistic table and the sigma's interval."
  - "Every figure in this article: tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs, TopGunsArticleFiguresHold, which reruns the simulation."
---

## What the trend says

A book on long range shooting, Modern Advancements in Long Range Shooting, Volume III, chapter 3, plots how well many rifles grouped against how hard they kicked for their weight. The trend it draws is simple: take the muzzle energy of the load, divide by the rifle's weight as it is fired, and divide again by 200. The answer is the average group, in MOA, that a conventional bolt rifle with good ammunition shoots from a steady rest. GroupLab calls it Top Gun and shows it beside a group's figures, on Compare and on the rifle's Scope and mount figures when it knows the bullet weight, the velocity and the rifle's weight.

For a few common rifles it says:

| Rifle and load | Weight as fired | Top Gun |
|---|---|---|
| 6.5 Creedmoor, 140 gr at 2700 ft/s | 12 lb | 0.94 MOA |
| .308 Winchester, 175 gr at 2600 ft/s | 11 lb | 1.19 MOA |
| .223 Remington, 77 gr at 2750 ft/s | 8 lb | 0.81 MOA |

The group in that trend is a particular thing: the extreme spread of five-shot groups, averaged over several groups. That matters, because it is easy to hold the wrong number up against it.

## One group is a coin toss

Suppose your rifle is exactly on the trend. Its true average five-shot group is the Top Gun figure. Shoot one five-shot group and measure it. How close is it to that average?

Not very. Across 20,000 simulated groups, 8 in 10 land between these multiples of the true average, and the other 2 in 10 land outside:

| Five-shot groups averaged | Low end | High end |
|---|---|---|
| 1 | 0.67 | 1.36 |
| 2 | 0.76 | 1.25 |
| 3 | 0.80 | 1.20 |
| 5 | 0.85 | 1.16 |

So a rifle exactly on the trend shows one group a third smaller or a third bigger than it, often. One good group is not proof the rifle beats the trend, and one bad group is not proof it falls short. Even the average of five groups is still 15 percent either way.

## What GroupLab does instead

For this comparison GroupLab does not average extreme spreads. It works out the spread of the whole group from every shot, a sigma, which uses every hole and not just the two farthest apart, and then turns it into the five-shot average that sigma implies, so it can sit beside the trend on the same basis. With the sigma comes its interval, the range the true value is likely in, carried through the same way:

| Shots | Interval around the estimate |
|---|---|
| 5 | 0.68 to 1.92 times |
| 10 | 0.76 to 1.48 times |
| 20 | 0.82 to 1.29 times |
| 30 | 0.85 to 1.22 times |
| 50 | 0.88 to 1.16 times |

GroupLab says a rifle is clearly wider or clearly tighter than the trend only when the whole interval sits on one side of it. Otherwise it says the two are not clearly different at this many shots. That is not caution for its own sake. It is the honest answer.

## How many shots it takes

Two questions decide whether the comparison is worth anything. How often does a rifle that is truly on the trend get called wider? And how often does a rifle that is truly worse get caught?

The first stays small at every size: between 2 and 4 times in 100. The second is the one that costs shots. For a rifle whose true groups are 25 percent bigger than the trend, GroupLab's interval clears the trend:

| Shots | Caught |
|---|---|
| 5 | 25 percent of the time |
| 10 | 35 percent |
| 20 | 58 percent |
| 30 | 73 percent |
| 50 | 90 percent |

Twenty shots, four five-shot groups pooled, catches a 25 percent shortfall a little over half the time. To be fairly sure, it takes about 50. A smaller shortfall takes more. This is why GroupLab lets you pool several groups shot with the same rifle and load into one.

## What this means

From one or two groups, conclude nothing about the rifle against the trend. Shoot more and pool them.

If the pooled interval sits clearly above the trend, the rifle and load are probably grouping worse than typical. What is usually checked is the action's bedding and how it returns to battery, the crown, the scope's mount and rings, the barrel, how evenly the bullet is engraved, and your technique and rest. GroupLab lists these where your group is clearly wider.

If the interval sits clearly below the trend, enjoy it. The trend is not a limit, and real rifles sit on both sides of it.

If the interval straddles the trend, which is what most rifles will show from a box of ammunition, the answer is that you cannot yet tell. That is a result too.
