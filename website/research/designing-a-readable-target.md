---
title: Designing a target GroupLab can read
description: Everything printed on a target sheet is something a hole detector might mistake for a hole. The design is mostly about telling the software what is ink before it has to guess.
group: How GroupLab is built
number: 25
written: 2026-09-22
data_date: 2026-09-22
samples: not a measurement: the design rules of twenty-two built-in sheets, with the failures that produced each one
state: published
no_figure: "The rules are about layout, and the twenty built-in sheets are the worked examples. Every one of them can be printed from the application and looked at, which is better than a picture of one."
found: The most effective single rule is declaring where printed matter is, so candidates there are dropped rather than classified. A declared rectangle is free and perfectly reliable; classification is neither.
sure: These are design decisions with reasons, several of them driven by real failures on real targets. Where a rule rests on a measurement, that measurement is linked.
sources:
  - "The sheet format and its zones: `DESIGN.md`, sections 9 and 13."
  - "Why sighters are a separate pool: `docs/NOTES-FROM-PLANNING.md`, entry 73 section 1."
---

## The problem in one sentence

A bullet hole is a small dark irregular blob, and so is almost everything else on a target.

Printed ring edges are dark. Numerals are dark, small and irregular. The ruled lines of a load-data block make small dark rectangles. Handwriting in that block is a small dark irregular blob by any definition a detector can express. Even the registration markers are small dark squares.

Any detector good enough to find a .22 hole in a printed ring will find all of those too.

## The rule that does most of the work

**Declare where the printed matter is.**

The sheet's own definition names the regions that are ink by construction: the load-data block, the code squares with their quiet zones, the marker footprints, the identifier text. Candidates found inside those regions are dropped, and recorded as dropped, rather than being classified.

This sounds like giving up. It is the opposite: it is the only part of the problem where the answer is known for certain. GroupLab printed the sheet. It knows exactly where it put the load block. Spending detector cleverness on deciding whether a mark in the load block is handwriting is spending it on a question that was already answered.

**A declared rectangle is free and perfectly reliable. Classification is neither.** That trade is available because the sheet is generated rather than bought, and it is most of the reason a generated sheet is worth having.

## Color, and why the rings are what they are

Printed ink has color. A bullet hole does not: it is a hole, and what shows through is neutral whatever its darkness.

So the detector can use chroma to separate ink from holes, which works well and is one of the reasons the sheets are printed in color rather than black. It stops working where the ring is dark enough that its color is swamped, which is why the ring design matters as much as the detection does.

## Sighters are a different pool, and that is not a nicety

Sighter bulls are separate from scoring bulls, and a shot fired at one can never belong to the other.

This came from a real failure. With both matched as one pool, two sighter holes were given to scoring bulls a row away, and the group statistics counted them. A sighter is a shot you fired to check your zero and deliberately excluded from the group; having it silently join the group is exactly the kind of quiet error this project is built to avoid.

Each shot now joins the pool of its nearest bull, and the counting rule applies within each pool on its own.

Sighters are ignored unless you ask for them, but the matching still runs over them, because matching a sighter is what keeps its hole off the scoring bull above it. And a contest **between** a sighter and a scoring bull is always raised for you to settle, because the scoring bull's shot depends on the answer.

## What the markers need from the layout

The markers sit on the boundaries between cells, where they do not interfere with the bulls and where the lattice gives them positions known to a fraction of a millimeter.

They need a quiet zone around them, 1.0 mm on a 4.0 mm marker. A marker with something printed against its edge is a marker that may not decode, and a sheet that loses too many markers cannot be registered at all.

There is a whole article on the marker choice itself: [Choosing the markers](choosing-the-markers).

## The size of a bull, and what it is for

A bull has two jobs that pull in opposite directions.

It has to be **aimable**: big enough to see and center on at the distance you are shooting. And it has to be **small enough that a sheet holds many of them**, because the whole point of a twenty-five bull sheet is that you get a twenty-five shot group out of one-shot-per-bull, without the holes overlapping and without the group being about your aim instead of your rifle.

That is why the sheets come in families rather than one size: rimfire at 50 yards wants something different from a centrefire at 300, and the built-in library is twenty-two sheets rather than one because those are genuinely different problems.

## What is deliberately not offered

There is no free-form visual designer where you place bulls anywhere you like.

Every one of the twenty-two built-in sheets is parametric: a grid, a spacing, a ring set, a page size. A form covers that space completely. A canvas for arbitrarily placed bulls is a large piece of software for a case nobody has asked for, and it is revisited the day somebody asks for a layout the form cannot express.

The same canvas would also be the route to tracing a definition over a store-bought target, which is a genuinely useful thing. So the two arrive together or not at all, and the decision is recorded rather than left as an absence somebody has to guess the reason for.

## What this means

**If you design a sheet, declare where the printed matter is.** It is the single rule that does the most work, it costs nothing, and it is perfectly reliable where classification is neither. A design that leaves GroupLab to work out that a ring is not a hole will sometimes be wrong, and it will be wrong silently.

**Do not design for how the sheet looks to you.** Several of these rules exist because a sheet that read perfectly well to a person failed on paper. The bull size is for the shot, the marker spacing is for the photograph, and the sighter pool is separate because mixing it changes the answer rather than the appearance.

**What to stop believing:** that a target is a picture. It is a measuring instrument that happens to be printed, and every choice in it either helps a measurement or costs one.
