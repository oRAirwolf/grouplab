---
title: "What a canted scope costs at distance"
description: "Tilt the rifle a few degrees and the elevation you dialed pushes the shot sideways and a little low. At 300 yards it hardly matters; at 1000 it is a miss. And a scope's own travel decides how far you can dial at all, which a canted rail changes."
group: Measuring groups
number: 42
written: 2026-10-21
data_date: "Worked out 2026-10-21 by GroupLab's own ballistic solver, for one load"
samples: "One load (140 gr, 2710 ft/s, G7 0.326, sight 1.75 in, zeroed at 100 yd, standard air); no shots were fired for this article"
state: published
no_figure: "The numbers are few and the article gives them as tables."
found: "2 degrees of cant moves the shot 0.8 in sideways at 300 yd, 7.3 in at 800 and 12.5 in at 1000, while the height lost stays under a quarter of an inch. A scope with 60 MOA of travel on a flat rail runs out of elevation at 910 yd with this load; on a 20 MOA rail it reaches 1270."
sure: "the geometry is exact for a rifle tilted about the line of sight, and the fall of the bullet comes from GroupLab's solver for one load in still air. It leaves out wind, which a cant also turns, and the error of reading a level."
sources:
  - "The bullet's path, the elevation and the zero's angle: GroupLab's ballistic solver, docs/BALLISTICS.md."
  - "How far a scope's travel and a rail reach: the rifle's Scope and mount figures in GroupLab."
  - "Every figure in this article: tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs, TheCantArticlesFiguresHold."
---

## Why a tilt moves the shot sideways

When you dial elevation, you tilt the barrel up so the bullet climbs above your line of sight and falls back onto the target. The higher the barrel points, the farther the bullet falls on the way. Held level, all of that tilt is straight up and the fall is straight down, and they cancel.

Cant the rifle, tilt it about the line of sight, and the barrel's upward tilt now leans to one side. Gravity still pulls straight down. The two no longer cancel: the shot lands to the side the rifle leans, and a little low. The size of it is simple geometry. The sideways miss is the bullet's fall from the bore line times the sine of the cant, and the height lost is that fall times one less the cosine.

## How big it is

For one ordinary long range load, a 140 grain bullet at 2710 ft/s zeroed at 100 yd, GroupLab's solver gives the elevation to dial and the bullet's fall from the bore line. The sideways miss for a cant of 1, 2 and 5 degrees follows:

| Distance | Dial | 1 degree | 2 degrees | 5 degrees |
|---|---|---|---|---|
| 300 yd | 4.1 MOA | 0.4 in | 0.8 in | 2.1 in |
| 500 yd | 10.0 MOA | 1.2 in | 2.5 in | 6.2 in |
| 800 yd | 21.0 MOA | 3.6 in | 7.3 in | 18.1 in |
| 1000 yd | 30.2 MOA | 6.2 in | 12.5 in | 31.1 in |

The height lost is much smaller: at 5 degrees it is 0.09 in at 300 yd and 1.36 in at 1000. A cant is a sideways error, not a vertical one.

Two degrees is not much, and it is hard to see without a level. At 300 yd it is nothing a shooter would notice. At 1000 it is a foot, which is the width of most steel plates you would shoot there.

The miss grows with the elevation dialed, so it grows faster than the distance does. That is the whole reason a scope level matters more the farther you shoot, and hardly at all at 100 yd.

## The other cant: the rail

A scope rail is often made with a slope of its own, 20 MOA being common. That is a different thing from tilting the rifle. It points the scope down a little against the barrel, so more of the scope's elevation is left for going up.

Why it matters: a scope adjusts only so far either way from the middle of its travel. Zeroing uses some of that, because the barrel has to point up a little even at 100 yd. With this load the zero alone uses 4.0 MOA. What is left above the zero is how far you can dial, and so how far you can shoot without holding over in the reticle:

| Scope travel | Flat rail | 20 MOA rail |
|---|---|---|
| 50 MOA | 21.0 MOA left, to 790 yd | 41.0 MOA left, to 1190 yd |
| 60 MOA | 26.0 MOA left, to 910 yd | 46.0 MOA left, to 1270 yd |

A rail can be too steep, too. If its slope is more than half the scope's travel plus the zero's angle, the scope cannot come down far enough to zero at all. GroupLab works out both on the rifle's Scope and mount figures: the most rail cant that still zeroes, and how far the elevation you have left reaches with your load.

## What this means

- **Level the scope to the rifle once,** with the rifle level in a vise or on a flat bench, so the reticle's vertical line is square to the bore's travel.
- **Use a level on the rifle when you shoot far,** and look at it before each shot past a few hundred yards.
- **Check your elevation reach before a long shot,** not at it. If the distance needs more than the scope has left, you will be holding the rest in the reticle, which is where a cant costs you twice.
