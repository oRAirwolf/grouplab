---
title: "How much of your vertical is your velocity?"
description: "A velocity SD of 10 ft/s adds two hundredths of an inch to a group at 100 yards and three inches at 1000. So a 100 yard group cannot show you your velocity, and at long range a good rifle's vertical can be half velocity. What that means for chasing a low SD, and how to measure it."
group: Measuring groups
number: 44
written: 2026-10-21
data_date: "Worked out 2026-10-21 by GroupLab's own ballistic solver, for one load"
samples: "One load (140 gr, 2710 ft/s, G7 0.326, sight 1.75 in, zeroed at 100 yd, standard air) and two rifles described by their own spread; no shots were fired for this article"
state: published
no_figure: "The numbers are few and the article gives them as tables."
found: "a velocity SD of 10 ft/s adds a vertical SD of 0.02 in at 100 yd, 0.18 in at 300, 1.66 in at 800 and 2.93 in at 1000. For a rifle that averages about three quarters of a MOA for five shots, that is 5 percent of its vertical at 300 yd and 56 percent at 1000."
sure: "the vertical velocity adds is exact for this load in still air, from GroupLab's solver with the sight left where the average velocity zeroes it. The rifles are described by their own spread, not shot; a real rifle's spread is measured with an interval, and so is a velocity SD."
sources:
  - "The change of height per ft/s at each distance: GroupLab's ballistic solver, src/GroupLab.Core/Ballistics, checked as docs/BALLISTICS-VALIDATION.md describes."
  - "Predicted against measured vertical, and the intervals: GroupLab statistics reference, docs/STATISTICS.md sections 12.1 and 12.2."
  - "Every figure in this article: tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs, TheVelocityArticlesFiguresHold."
---

## Why velocity shows up as vertical

Every bullet from the same rifle leaves at a slightly different speed. A slower one takes a little longer to reach the target, so gravity has a little longer to pull it down, and it lands a little low. A faster one lands a little high. The sight is set once, for the average, so the spread in speed becomes a spread up and down on the paper. In still air it does nothing sideways.

How much depends on the distance. At 100 yards the time of flight is short and the difference between a fast and a slow round is tiny. At 1000 yards this article's bullet is in the air for about a second and a half and arrives at about 1490 ft/s, and the same difference in muzzle speed is a large difference in drop.

## How big it is

For one ordinary long range load, a 140 grain bullet at 2710 ft/s with a G7 BC of 0.326, zeroed at 100 yd, GroupLab's solver gives the change of height for each ft/s of muzzle velocity. Multiply by the velocity SD and you have the vertical SD that velocity alone adds:

| Distance | Height per ft/s | SD 5 ft/s | SD 10 ft/s | SD 15 ft/s |
|---|---|---|---|---|
| 100 yd | 0.002 in | 0.01 in | 0.02 in | 0.03 in |
| 300 yd | 0.018 in | 0.09 in | 0.18 in | 0.27 in |
| 500 yd | 0.055 in | 0.28 in | 0.55 in | 0.83 in |
| 800 yd | 0.166 in | 0.83 in | 1.66 in | 2.49 in |
| 1000 yd | 0.293 in | 1.46 in | 2.93 in | 4.39 in |

These are standard deviations, not group sizes. A five-shot group's height, top to bottom, is on average about 2.3 times its vertical SD, so 2.93 in of SD is about 7 in of height from velocity alone at 1000 yd.

Notice how fast it grows. From 100 to 1000 yards the distance is ten times longer and the vertical from velocity is more than a hundred times larger.

## Against the rifle's own spread

Velocity is never the only thing spreading a group. The rifle, the rest and the shooter spread it too, and they spread it as an angle: twice the distance, twice the inches. Spreads like these add as squares, not as inches, so a small part adds almost nothing to a large one.

Take two rifles. The first has a spread of its own, apart from velocity, of 0.25 MOA in each direction, which is a rifle that averages about three quarters of a MOA for five shots. The second has 0.5 MOA, which averages about 1.5 MOA. Both shoot the load above with a velocity SD of 10 ft/s:

| Distance | First rifle's own vertical | With velocity added | Velocity's share | Second rifle: velocity's share |
|---|---|---|---|---|
| 100 yd | 0.26 in | 0.26 in | under 1 percent | under 1 percent |
| 300 yd | 0.79 in | 0.81 in | 5 percent | 1 percent |
| 500 yd | 1.31 in | 1.42 in | 15 percent | 4 percent |
| 800 yd | 2.09 in | 2.67 in | 39 percent | 14 percent |
| 1000 yd | 2.62 in | 3.93 in | 56 percent | 24 percent |

The share is of the vertical's variance, the square of its SD, which is the honest way to say how much of a spread one cause accounts for.

Two things stand out. At 100 and 300 yards velocity is lost in the rifle's own spread, even for the better rifle. And the better the rifle, the more velocity matters: at 1000 yards it is more than half of the first rifle's vertical and a quarter of the second's.

## The air changes it a little

Thinner air slows the bullet less, so it spends less time falling. The same load at 90 F and 6000 ft gives 2.53 in of vertical from a 10 ft/s SD at 1000 yd, against 2.93 in on a standard day at sea level. That is a real difference, and a small one next to the distance. GroupLab works it out in the conditions you enter for the session, or on a standard day if you enter none.

## Measuring it on your own rifle

To see velocity in a group you need two things: a distance where it is large enough to matter, and enough shots for the numbers to mean something.

Both the velocity SD and the group's vertical SD are measured from a handful of shots, and each carries its own uncertainty. At GroupLab's 90 percent, a velocity SD that reads 10 ft/s from 10 shots could truly be anywhere from 7.3 to 16.5; from 20 shots, 7.9 to 13.7. The [velocity SD article](/research/velocity-sd-small-samples/) goes into that.

The share inherits both. From 10 readings and a 10 shot group, a measured share of 30 percent could truly be anywhere from 9 to 95 percent. From 20 readings and 20 shots it narrows to 14 to 65 percent. That is why GroupLab shows the share with its range, and says plainly that the data cannot tell when the range reaches all of the vertical.

In GroupLab, **Velocity and the vertical** sits under a group's figures. Give it the session's chronograph readings and the load's BC, and it sets the vertical spread measured on the shots beside the spread velocity alone would make at that distance, in the conditions on the day, and says how much of the vertical comes from velocity. Where each reading was paired with its shot, it also compares the measured change of height with speed against the solver's, which is a check on the distance, the BC and the pairing at once.

## What this means

- **A 100 yard group cannot show you your velocity.** Two loads with SDs of 5 and 15 ft/s add 0.01 and 0.03 in of vertical SD at 100 yards, far inside any rifle's own spread, so they print the same group there. Judge a load's velocity from the chronograph, not from a 100 yard group.
- **Chasing a lower SD pays at long range, and pays more with a better rifle.** At 1000 yards, halving the SD from 10 to 5 ft/s takes the vertical velocity adds from 2.93 to 1.46 in. Whether that shows on the target depends on how large the rifle's own spread is.
- **Measure it at the distance you care about,** with readings for every shot, and more shots than feels necessary. From ten, a 30 percent share could be almost anything; from twenty, the range is still wide but no longer reaches nearly all of the vertical.
- **Do not read velocity into vertical stringing at short range.** A group that strings up and down at 100 yards is not doing it because of velocity; look at the rest, the shooter or the rifle.
