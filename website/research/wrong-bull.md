---
title: When shots land on the wrong bull
description: On a 25 bull sheet, a rifle that is not zeroed puts every shot nearer a bull it was not aimed at. The group you get is then real, precise, and about nothing.
group: Reading targets
number: 6
written: 2026-09-22
data_date: 2026-09-20
samples: three sheets of 20, 23 and 10 shots with the shooter's own record of where each was aimed
state: published
no_figure: "The three sheets are the developer's range targets and are not published under a consent record. The article gives the offsets and the counts instead."
found: Nearest bull is the wrong rule whenever the rifle is not zeroed for the load. On one 20 shot sheet every single shot landed nearer a bull it was not aimed at, and the group that comes out of reading it that way is tight, confident and meaningless.
sure: Three sheets from one afternoon, with the shooter's own written record of which bulls were aimed at. Enough to show the failure clearly; the fix is still being built.
sources:
  - "The three sheets and the shooter's own record of them: `docs/NOTES-FROM-PLANNING.md`, entry 120."
  - "What GroupLab did with them: `docs/PHASE1-RESULTS.md`, \"Entry 120\", sections 2 and 7."
---

## Why a bull matters at all

GroupLab measures each shot from the bull it was aimed at, and pools those offsets into one group. That is what lets a 25 bull sheet with one shot per bull give you a 25 shot group: every bull is its own aiming point, and the group is about the rifle rather than about your aim.

It only works if the software knows which bull each shot was aimed at. The obvious rule is "the nearest one".

## Where that falls apart

The developer shot a 6 ARC load at 100 yards through a rifle zeroed for something else. He aimed at bulls 2, 3, 4 and 5 of every row, twenty shots.

Every shot landed high and left of its aim point, by more than the distance between two bulls.

So **every one of the twenty** is nearer a bull it was not aimed at. Nearest bull assigns all twenty to the wrong bulls, and then measures each shot's offset from that wrong bull. The offsets come out small, because each shot is close to the bull it got assigned to. The group comes out tight.

The number is precise and it is about nothing at all. It is not the rifle's dispersion; it is a measure of how neatly a constant offset happens to line up with a grid of bulls.

## The second sheet: two different offsets

The .22 LR sheet was shot in a strong, shifting crosswind, and the windage was adjusted between row 2 and row 3. So rows 1 and 2 share one point of impact, rows 3 to 5 share another, and inside each the wind moved the shots around by a real amount.

There is no single offset to correct here. A sheet-wide correction would be wrong for one half of the sheet whichever half it was fitted to.

## The third sheet: a whole row low, and one shot from nowhere

The primer test sheet had five shots on row 1 where they were aimed and five more aimed at row 2 that landed on row 3. One of those five landed far from everything else, low and left, next to bull 21. The developer fired it, saw where it went, and does not know why.

So this sheet has a group that is where it should be, a group that is a whole row low, and a single shot that is neither.

## What GroupLab does today

It says it is unsure, which is the important part, and it lets you move a shot onto the right bull by hand. It does not quietly hand you a tight group built on wrong assignments.

The review queue names the shots whose nearest bull is not the bull the matching gave them, and the figures carry their doubt. On the 6 ARC sheet it raises the problem rather than reporting a group.

## What it will do

Three things, in the order they matter.

**You tell it which bulls you aimed at.** Click them on the sheet, or use a preset: every bull, a row, bulls 2 to 5 of every row. That one piece of information solves the 6 ARC sheet outright, because with twenty aim points named and twenty shots to place, the assignment is a matching problem with one answer rather than a guess.

**Then it can work out a common offset.** Once it knows which bulls you meant, the shift is measurable, and a sheet where everything landed a bull low becomes a sheet it can read correctly.

**And you can still move any shot by hand**, because the shooter saw where the shot went and the software did not.

## What you can do now

If your rifle is not zeroed for the load you are testing, either zero it first, or expect to move some shots by hand and check the review queue before you trust the figures.

And if a group looks better than you shot, look at the assignment before you believe it. A tight group from a sheet you know you scattered is not good news; it is the software measuring something else.

## What this means

**Zero the rifle for the load before you shoot a multi-bull sheet.** That is the whole practical finding. On one twenty shot sheet every single shot landed nearer a bull it was not aimed at, and what came out was a tight, confident and completely meaningless group.

**Be suspicious of a result that is too good.** Reading shots to the nearest bull turns a consistent offset into twenty small groups, each one clustered around a bull. The numbers look better than the shooting was. That is the dangerous shape of this failure: it does not look like an error.

**Until GroupLab lets you say which bulls you aimed at**, the defence is your own record. Write the bulls down at the bench. It takes a moment and it is the only thing that survives a wrong assumption about the zero.
