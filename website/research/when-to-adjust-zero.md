---
title: "Zeroing: when to adjust and when to leave it"
description: "Adjusting your scope after every small group can make your zero worse, not better. How to tell a real zero error from ordinary shot-to-shot noise, and how many shots the decision needs."
group: Measuring groups
number: 16
written: 2026-09-22
data_date: "Simulation, seed 2026"
samples: "200,000 simulated zeroing sessions; worked examples"
state: draft
found: "the centre of a small group is itself uncertain. After a full correction based on three shots, your new zero misses the true one by about 0.72 sigma on average, whatever the old error was. So if your zero is already closer than that, adjusting on three shots makes it worse. A rifle that starts perfectly zeroed and is 'corrected' after every three-shot group settles at an average error of 0.72 sigma."
sure: "exact for round groups; the chasing figure is from 200,000 simulated sessions. It assumes the scope moves exactly as marked, which a scope that tracks badly makes worse."
data:
  - data/error-after-adjusting.csv
  - data/chasing-path.csv
sources:
  - "GroupLab statistics reference, sections 2 and 8.2 on group centres (docs/STATISTICS.md)."
  - "Standard error of the mean. https://en.wikipedia.org/wiki/Standard_error"
  - "David Wollschlaeger, shotGroups R package, group centre and its confidence region. https://cran.r-project.org/package=shotGroups"
---

## The centre of a group is an estimate

When you zero, you shoot a group, find its centre, and move the reticle by the distance from the centre to your aim point. The catch is that a group's centre moves around from group to group just as its size does. With a small group, the centre you measured is not exactly where the rifle really shoots.

How far off can it be? For shots with a spread of sigma in each direction, the centre of n shots has a spread of sigma divided by the square root of n. Averaged over direction:

| Shots in the zeroing group | Average error of the measured centre |
|---|---|
| 3 | 0.72 sigma |
| 5 | 0.56 sigma |
| 10 | 0.40 sigma |

If your rifle's 100 yard sigma is 0.25 inch (a mean radius of about 0.31 inch), a three-shot zero is off by about 0.18 inch on average after you adjust, and sometimes by double that.

## When adjusting helps

A correction replaces your current error with the error of the measurement. That is worth doing only when the current error is bigger.

![Adjust or leave it](/research/when-to-adjust-zero/figures/adjust-or-not.png)

The dashed line is "leave it alone": your error stays what it is. Each flat line is "adjust fully on this many shots": your new error is the measurement's error, however far off you started. Where the dashed line is below a flat line, adjusting makes things worse.

So a zero that is off by half a sigma should not be adjusted on three shots. It should be confirmed with more shots first.

## Chasing the zero

Many shooters adjust after every small group. This is what that does to a rifle that was perfectly zeroed to begin with:

![Chasing the zero](/research/when-to-adjust-zero/figures/chasing.png)

Each correction chases the noise of the last group, and the zero wanders. Over many rounds of this, the average error settles at the three-shot figure, about 0.72 sigma, where it would have been zero if nobody had touched the turrets.

## How to decide

The better question is not "where is the centre?" but "is my aim point inside the area where the centre could really be?"

![Deciding whether to adjust](/research/when-to-adjust-zero/figures/decide.png)

- **Left:** five shots. The blue area is where the true centre lies, 95 percent of the time, which is the level every zero figure GroupLab shows is quoted at. The aim point is inside it. The data cannot tell a real error from noise, so leave the scope alone and shoot more.
- **Right:** ten shots, a group centre further from the aim, and a smaller blue area because there are more shots. The aim point is outside it. The error is real: adjust.

This is what GroupLab's zero offset picture shows: the aim as a cross, the group centre with its uncertainty, and an arrow with the clicks to move only when the uncertainty does not cover the aim. When it does, there is no arrow, because there is nothing honest to tell you to do.

## Practical rules

- **Zero on more shots.** Five is better than three, ten better than five. The error of the measured centre falls with the square root of the shot count.
- **Adjust once, then confirm.** Make one correction from a decent group, then shoot to confirm rather than correcting again straight away.
- **Know your click size.** A 0.1 mil click is 0.36 inch at 100 yards and a 1/4 MOA click is 0.26 inch. A zero error smaller than one click cannot be corrected anyway.
- **Pool zero groups.** Shots from several sessions with the same zero, rifle and load all count towards knowing where it really shoots.
