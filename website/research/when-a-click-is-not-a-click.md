---
title: "When a click is not what it says: the sight scale factor"
description: "A scope that moves 0.98 MOA when it says 1 is off by 2 percent. That is normal for many scopes, harmless at 300 yards and a miss at 1000. How a tall target test measures it, and what GroupLab does with the number."
group: Measuring groups
number: 43
written: 2026-10-21
data_date: "Worked out 2026-10-21 by GroupLab's own solver and hit probability, for one load, seed 2026"
samples: "One load (140 gr, 2710 ft/s, G7 0.326, sight 1.75 in, zeroed at 100 yd), a 0.63 MOA mean radius and a 10 in circle; no shots were fired for this article"
state: published
no_figure: "The numbers are few and the article gives them as tables."
found: "at 1000 yd, where this load needs 30.2 MOA, a 2 percent tracking error puts the group's center 6.3 in off, and the chance of a first-round hit on a 10 in circle falls from 36 percent to 20; a 4 percent error, to 4. At 600 yd the same 2 percent costs 5 points."
sure: "the hit chances are for a still target, no wind and a perfectly known distance, from a mean radius you supply, so they show what tracking alone costs. Real errors come from the sources below, which are individual tests and a magazine's guidance, not a survey of scopes."
sources:
  - "Gun Digest, \"Why it's crucial to test the tracking of your riflescope\": an error of about 2 percent is typical at its price point, and 5 percent or more is a reason to send a scope back."
  - "The Firearm Blog, \"Know your scope to know your dope\", 2019: one inexpensive scope measured at 110 percent, a tenth more than marked, and the warning that a box test with under a foot of travel barely shows an error."
  - "The test and the factor: GroupLab's tracking test, on the rifle's Scope and mount figures."
  - "Every figure in this article: tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs, TheTrackingArticlesFiguresHold."
---

## What it is

Every click of a scope's turret is marked: a quarter MOA, a tenth of a mil. The scope moves its reticle by about that much. "About" is the point. A scope that moves 0.98 MOA every time it is marked 1 MOA tracks 2 percent short, and every elevation you dial lands that much low. The ratio between what the turret says and what it does is the sight scale factor.

It does not show at 100 yd, where you dial nothing. It shows wherever you dial a lot, which is far away.

## How big real errors are

Published tests are individual ones, not a survey, but they agree on the scale. Gun Digest calls an error of about 2 percent typical for scopes at modest prices, and treats 5 percent or more as a reason to send the scope back. The Firearm Blog measured an inexpensive scope at 110 percent, a full tenth more than its markings, so ten clicks gave eleven.

## How a tall target test measures it

GroupLab's test needs no shots:

1. Hang a tall target with two lines a known angle apart at the distance you test from: 20 mil, or 66 MOA, is usual. Measure that distance exactly, because the lines' angle depends on it.
2. Level the reticle with the target.
3. Aim at the top line, then dial down until the reticle sits on the bottom line.
4. Read how far the turret turned, and type it in.

The factor is the angle the lines really are apart over the angle the turret read. The error is the difference as a percentage, positive when the scope under-tracks, each click moving less than marked. It takes a long dial to see a small error: at 100 yd, 2 percent of 20 mil is 1.4 in, so the test wants all the travel you can give it, and an exact distance.

## What it costs at distance

For the same 140 grain load at 2710 ft/s, a rifle with a 0.63 MOA mean radius, a still target and a perfectly known distance, here is the chance of a first-round hit on a 10 in circle. The first column is GroupLab's hit probability for a centered shot; the others are the same shots moved low by the tracking error times the elevation dialed:

| Distance | Dial | Error moves it, per percent | Tracking true | 1 percent | 2 percent | 4 percent |
|---|---|---|---|---|---|---|
| 600 yd | 13.3 MOA | 0.8 in | 72 percent | 70 | 67 | 53 |
| 800 yd | 21.0 MOA | 1.8 in | 51 percent | 48 | 40 | 19 |
| 1000 yd | 30.2 MOA | 3.2 in | 36 percent | 31 | 20 | 4 |

At 600 yd a typical 2 percent error costs five points of hit chance. At 1000 it costs nearly half your hits, and a scope 4 percent off barely hits at all, even with everything else perfect. The error is also invisible on a range where you shoot at one distance and correct your dope by where the shots land: the correction quietly absorbs it, and then a different distance is wrong by a different amount.

## What this means

Enter the result of your tracking test on the rifle's Scope and mount figures, and GroupLab works out the factor and the error and says what the error means. From then on every click it gives you for that rifle is the true angle over the factor, so a scope that tracks 2 percent short is given 2 percent more clicks. Where the factor has corrected a number, GroupLab says so beside it.

A test takes one tall target and no ammunition. If you dial far, it is the cheapest accuracy you will ever buy.
