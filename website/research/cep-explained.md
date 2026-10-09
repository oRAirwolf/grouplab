---
title: "CEP 50 and 90 explained"
description: "Circular error probable is the radius of the circle that holds half, or nine in ten, of your shots. What CEP means, how it relates to mean radius, and what happens when your group is not round."
group: Measuring groups
number: 13
written: 2026-09-22
data_date: "Simulation, seed 2026"
samples: "400,000 simulated shots per shape; 30-shot example groups"
state: published
found: "for a round group, CEP 50, mean radius and CEP 90 are fixed multiples of the same underlying spread: 1.18, 1.25 and 2.15 sigma. They are three views of one number. When a group stretches to twice as wide as it is tall, the round-group CEP 50 circle holds about 54 percent of shots instead of 50, and the CEP 90 circle about 89 percent instead of 90."
sure: "exact for the model; the stretched-group figures are from 400,000 simulated shots per shape. Real groups with flyers break every one of these formulas, which is why GroupLab handles flyers separately."
data:
  - data/coverage-by-shape.csv
sources:
  - "GroupLab statistics reference, sections 3.4 and 4 (docs/STATISTICS.md)."
  - "Daniel Wollschlaeger, shotGroups R package, including its CEP estimators and reference values. https://cran.r-project.org/package=shotGroups"
  - "Circular error probable. https://en.wikipedia.org/wiki/Circular_error_probable"
  - "Rayleigh distribution. https://en.wikipedia.org/wiki/Rayleigh_distribution"
---

## A circle that answers a practical question

Mean radius tells you the average distance of a shot from the group center. CEP answers a question that is often more useful in the field: **how big a circle do I need to hold a given share of my shots?**

- **CEP 50** is the radius of the circle, centered on the group, that holds half your shots.
- **CEP 90** holds nine shots in ten.

If your CEP 90 at 100 yards is 0.6 inch, you can expect nine shots in ten inside a 1.2 inch circle around your group center, as long as nothing changes.

## Three circles, one number

For a round group (shots spread the same amount left and right as up and down, and no favored direction), every circular measure is a fixed multiple of a single spread figure, sigma:

| Measure | Multiple of sigma |
|---|---|
| CEP 50 (also the median radius) | 1.1774 |
| Mean radius | 1.2533 |
| CEP 90 | 2.1460 |
| CEP 95 | 2.4477 |

![A round 30-shot group with its three circles](/research/cep-explained/figures/three-circles.png)

So CEP 50 is always a little smaller than mean radius, and CEP 90 is about 1.7 times mean radius. None of them carries information the others lack. GroupLab estimates sigma from every shot (the Rayleigh estimate) and derives all of them from it, each with its confidence interval. The intervals are the same width in relative terms, because they come from the same estimate.

In the example above, 17 of the 30 shots fall inside the CEP 50 circle and 27 inside the CEP 90 circle. That is what "about half" and "about nine in ten" look like with real-world luck in a 30-shot sample.

## When the group is not round

Many real groups are not round. Vertical stringing from velocity spread or a bad rest, or horizontal spread from wind, stretch the group into an oval. A circle is then the wrong shape for describing it, and the round-group formula starts to drift.

![A group twice as wide as it is tall](/research/cep-explained/figures/stretched-group.png)

How far does it drift? This chart keeps the total spread the same and stretches the group:

![Coverage of the round-group circle by shape](/research/cep-explained/figures/coverage-by-shape.png)

| Long axis to short axis | Shots inside the CEP 50 circle | Inside the CEP 90 circle |
|---|---|---|
| 1 (round) | 50 percent | 90 percent |
| 1.5 | 52 | 90 |
| 2 | 54 | 89 |
| 3 | 57 | 88 |
| 4 | 59 | 88 |

For moderate stretching the round formula holds up surprisingly well. By a ratio of 2 it is off by a few percentage points, which is less than the sampling uncertainty of most groups. Beyond that the circle itself is the problem: an oval group is better described by its width and height separately, which GroupLab also shows.

The shotGroups package, which GroupLab validates against, offers eleven ways to estimate CEP for oval groups. On one reference 20-shot group they disagree by about 15 percent. That spread is worth knowing: for a stretched group, "the CEP" is not one well-defined number.

## CEP 99 from a few shots

GroupLab can draw CEP 99 as well, and a circle for any percent you type under Advanced. They come from the same sigma as the others: CEP 99 is 3.03 sigma, against 2.45 for CEP 95. So the number itself is no harder to compute. What it means is another matter.

**A circle that should hold 99 shots in 100 cannot be checked with 10.** With n shots, about n times one percent of them are expected outside CEP 99: a tenth of a shot from 10, a quarter of one from 25. None of your shots has been out there. The circle is where the circular normal model says the tail is, and real groups have heavier tails than the model: a flyer, a called shot, a cold bore. CEP 99 from a small group is the model's guess about the shots you have not fired.

**Its range is only as wide as sigma's, and that is not the whole uncertainty.** GroupLab shows CEP 99 with the same 95 percent range as the others, taken from sigma: from 10 shots that range runs from 0.76 to 1.48 times the value, from 25 shots from 0.83 to 1.25, from 100 shots from 0.91 to 1.11. That range assumes the model holds out in the tail. When fewer than one shot would be expected outside the circle, GroupLab says so beside the number, and says about how many shots would put one there: 100 for CEP 99, 1000 for 99.9.

**Use it for what it is.** CEP 99 is a fair way to ask "how big a target will this rifle almost never miss", provided you read it as the model's answer and give it the shots to be tested: a few sessions pooled, not one group of five.

## What GroupLab shows

- CEP 50, 90 and 95 as circles on the group plot, centered on the group center, dotted, solid and dashed, each with its own switch beside the plot, next to the mean radius.
- Each with a confidence interval from the same Rayleigh estimate as mean radius.
- Width and height of the group, so a stretched group is visible as stretched rather than hidden inside a circle, with the spread across and up and down drawn on one scale.
- A verdict on whether the group is round at all: not a warning at some aspect ratio, but a circularity test, which says "Round, as far as 24 shots can tell" when the shots cannot separate the two axes and "Not round" when they can. The error ellipse's aspect and the angle of its major axis are given beside it. That distinction matters here, because with few shots almost every group measures oval and almost none of them is.

## What this means

**Stop treating CEP, mean radius and sigma as different measurements.** For a round group they are the same number in three costumes: 1.18, 1.25 and 2.15 times the same spread. If somebody quotes you one and you want another, you multiply. There is no extra information in the second figure.

**So pick one and stay with it.** Switching between them between sessions makes your own numbers incomparable for no benefit.

**Where it stops being true is the part to remember.** Those fixed multiples assume a round group. Stretch the group to twice as wide as it is tall and the CEP 50 circle holds 54 percent rather than 50. That is a small error, and it is the first thing that goes wrong when a group is not round, which is a good reason to look at the shape before quoting any of the three.
