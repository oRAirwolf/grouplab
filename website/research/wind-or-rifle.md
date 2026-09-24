---
title: Wind or rifle?
description: A group wider than it is tall looks like wind. Most of the time it is not wind, it is what a handful of shots from a perfectly round rifle looks like, and telling those apart needs a test rather than an opinion.
group: Range tests
number: 19
written: 2026-09-22
data_date: 2026-09-20
samples: one 24 shot .22 LR sheet shot in a shifting crosswind with the windage adjusted mid-sheet, plus the circularity test run against simulated round groups
state: published
no_figure: "The distribution the test is built on is exact and is given as numbers. The developer's standing consent of 2026-09-24 now allows those range sheets to be shown, and no figure has been drawn from them yet, including the one windy sheet that would illustrate it."
found: Every group is lopsided. Whether a group is lopsided enough to blame the wind is a question with a numerical answer, and at the shot counts people fire the answer is usually no.
sure: The simulation behind the test is exact for a round normal distribution. The one real windy sheet is an illustration, not evidence about wind in general.
sources:
  - "The circularity test: `src/GroupLab.Core/Statistics/CircularAspect.cs`, and `docs/STATISTICS.md`."
  - "The windy sheet and what it did to assignment: `docs/NOTES-FROM-PLANNING.md`, entry 120, and `docs/PHASE1-RESULTS.md`, scan 4."
---

## Every group is lopsided

Take a rifle whose dispersion is perfectly round, with no wind at all, and fire ten shots. Measure how far they spread left and right, and how far up and down.

The two numbers will not be equal. They are never equal. With ten shots they are routinely different by half again, and sometimes by double.

That is not the rifle and not the wind. It is what ten samples from a round distribution look like. Ten darts thrown at a round target do not land in a circle; they land in a blob, and the blob is longer one way than the other, and which way is arbitrary.

## Why this particular illusion is expensive

A group wider than it is tall has an obvious story attached: it was windy. A group taller than it is wide has one too: vertical dispersion, so the load is not consistent, so go back to the bench and work up a new one.

Both stories are available for free on any group, because every group is lopsided one way or the other. So the shooter who looks for wind finds wind, and the shooter who looks for vertical finds vertical, and each walks away with a conclusion the shots did not support.

This is the same error as reading a barrel warming into ten shots fired in a random order, and it has the same fix: not a better eye, a test.

## The question, asked properly

"Is this group wider than it is tall?" is nearly always yes and is not worth asking.

The question worth asking is: **if this rifle's dispersion were perfectly round, how often would a group of this many shots come out at least this lopsided?**

That has an exact answer for a round normal distribution, and GroupLab computes it. Feed it the number of shots and the ratio of the two spreads, and it returns the probability that chance alone would do at least that.

The answers are sobering:

- Ten shots, one spread half again the other: happens **often**, by chance, from a round rifle.
- Five shots: almost nothing is distinguishable from round.
- Twenty-four shots with one spread twice the other: now you have something.

## What GroupLab shows

The analysis draws the two spreads on **one scale**, because the whole question is which is larger and two scales would answer it by drawing rather than by measuring. Each shot is a dot rather than a bar, because a spread where one shot is a long way out is a different thing from an even spread, and the two have the same standard deviation.

And the caption always carries the answer to the second question:

> Across 0.112 in, up and down 0.097 in. It measures wider than it is tall, but 24 shots cannot tell that from an ordinary round group.

Where the shots **can** separate them it says so instead. It never states the first sentence without the second.

## The sheet that really was windy

The developer shot a .22 LR sheet in a strong, shifting crosswind, and adjusted the windage between row 2 and row 3.

That sheet is a useful illustration of something different: the wind was real and it still was not a single fact about the sheet. Rows 1 and 2 share one point of impact, rows 3 to 5 share another, and within each the wind moved shots around by a genuine amount. There is no one correction that describes it, and software that fitted a single offset would be wrong for one half of the sheet whichever half it fitted.

So even when wind is unambiguously present, "the wind pushed my group right by this much" is often not a well-formed statement about a sheet shot over twenty minutes.

## What to do about it

- **Shoot more before blaming anything.** The ratio test needs shots, and so does every other question worth asking of a target.
- **Read the caption, not the picture.** The picture always shows a lopsided group.
- **If you change your sights mid-sheet, write down where.** A sheet with two points of impact is two sheets, and no amount of statistics recovers the information that they were different if nobody recorded it.
- **A windy day's group measures the day, not the rifle.** That is fine, as long as you do not later compare it with a calm day's group and call the difference a load.

## What this means

**Your group is lopsided and that means nothing.** Every group is. A round distribution produces lopsided-looking groups at the shot counts people actually fire, and the eye is extremely good at finding a direction in noise.

**Before you blame the wind, ask the question properly.** GroupLab gives the answer numerically, and at five or ten shots the answer is usually that the group is no more lopsided than chance would produce. That is not a failure to detect wind; it is an honest statement that these shots cannot tell.

**So do not adjust anything on the strength of a shape.** A correction made for a pattern that was not there costs you the zero you had, and you will not find out for another session.
