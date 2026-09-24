---
title: Why a photo cannot tell you your bullet's size
description: The same holes measure 0.90 to 1.45 times the bullet depending on the light they were photographed in. Scans of the same sheets agree within three percent.
group: Reading targets
number: 1
written: 2026-09-22
data_date: 2026-09-20
samples: 176 holes photographed and 78 scanned, over four sheets of known caliber
state: published
found: A hole in paper photographed with a phone measures anywhere from nine tenths to one and a half times the bullet that made it, and which end you land on depends on the light rather than on the bullet. Scanned, the same four sheets all read between 0.92 and 0.95.
sure: Four sheets, nine photographs, 176 holes, one shooter, one afternoon. Enough to show that no single correction exists; not enough to tell you what the correction would be if one did.
data:
  - hole-ratios.csv
  - hole-crops.json
sources:
  - "The measurement and what it changed: `docs/PHASE1-RESULTS.md`, \"What a hole measures in a photograph\"."
  - "The question it answered: `docs/QUESTIONS-FOR-PLANNING.md`, question 38."
  - "Where the scan figures came from: `docs/SCAN-MEASUREMENTS.md` section 3.5."
---

![Measured hole size as a fraction of the bullet, for four sheets, scanned and photographed](/research/photo-hole-size/figures/lead.svg)

Every dot is one image of one sheet. Left of the dashed line the holes measured smaller than the bullet; right of it, larger.

## The question

If you photograph a target and GroupLab measures the holes, can it tell you what you were shooting?

It is a fair thing to want. The hole is right there, the software has already measured it to a thousandth of an inch, and a caliber is one more number it could hand you. It would also be useful in the other direction: if GroupLab knows the caliber, it can tell one hole from two, because two bullets through one hole make a mark about twice the area of one.

So we measured it, on four sheets whose caliber we knew for certain because the developer loaded them.

## What we did

Four sheets from one afternoon: a .22 LR block, a 6 ARC block, and two 6.5 Creedmoor sheets. Each was scanned at 600 dpi on a flatbed and photographed with a phone, some square on, some at an angle, some close, some from across the bench.

Every image went through GroupLab's ordinary path: find the printed markers, work out where the page is, and measure each hole. Then we divided the median hole by the bullet diameter the developer actually fired.

Every sheet registered cleanly. The scans came out at 0.0023 to 0.0026 inches of registration error and the photographs at 0.0042 to 0.0060, so nothing below is a sheet GroupLab could not read.

## What came out

| sheet | bullet | scanned | photographed |
|---|---|---|---|
| .22 LR block | 0.222 in | 0.765 | 1.079, 1.087 |
| 6 ARC block | 0.243 in | 0.923 | 1.256, 1.333, 1.360 |
| 6.5 Creedmoor, 25 shots | 0.264 in | 0.949 | 0.898 |
| 6.5 Creedmoor, 15 shots | 0.264 in | 0.937 | 1.452, 1.405, 1.449 |

## What a hole actually looks like

These three are from one sheet, the 6.5 Creedmoor scanned at 600 dpi, and they are the same three numbers the row above reports, drawn where they came from. The bullet was 0.264 in across. The red line is what GroupLab measured; the bar at the corner is a tenth of an inch, so every other length in the picture can be checked against something.

![A bullet hole in paper, 0.237 inches across, with a caliper line drawn across the width GroupLab measured and a tenth-inch scale bar](/research/photo-hole-size/figures/hole-smallest.png)

The smallest hole on the sheet, 0.237 in, which is **0.897 of the bullet that made it**. Look at the rim: the paper has closed back in behind the bullet rather than been punched out cleanly, and there is no torn edge to add width. This is the end of the range that makes a hole measure small.

![The same kind of hole, 0.251 inches across, with the caliper line crossing a ragged rim](/research/photo-hole-size/figures/hole-typical.png)

The median hole, 0.251 in, **0.949 of the bullet**, and it is the number the table quotes for this sheet. Look at the ring of torn fibres around the top of it: that fringe is paper that has been pushed aside rather than removed, and whether a measurement includes it is exactly what makes one hole read wider than another.

![A hole 0.274 inches across, wider than the bullet, with the caliper line spanning a torn edge](/research/photo-hole-size/figures/hole-largest.png)

The largest, 0.274 in, **1.039 of the bullet**, so this one measures wider than the bullet that made it. Nothing about the shot was different. The paper tore instead of closing, and the tear is inside the measurement.

**That is the whole finding in three pictures, and these are scans.** The spread from 0.897 to 1.039 is what one sheet does under a flatbed, with the light constant and the scale absolute. A photograph adds the shadow on top of this, which is how the same holes reach 1.45.

![A photographed hole measured at 0.383 inches, its torn crown in shadow, with the caliper line reaching well past the dark core](/research/photo-hole-size/figures/hole-shadow.png)

**And this is a photograph.** One of the developer's, of the 6.5 Creedmoor 15 shot sheet, square on and close, and published under the developer's standing consent. GroupLab measured this hole at 0.383 in, **1.452 of the bullet**, which is the median hole on that sheet and not an outlier. Look at what is dark: the core, and around it a ring of torn paper standing up out of the sheet and throwing its own shadow into the hole. To the camera, that shadow is hole. On the scanner, whose lamp sits two centimeters away at a fixed angle, the same sheet's holes measured 0.937 of the bullet.

The three centrefire scans agree with each other to within three percent. The photographs do not agree with anything: they run from 0.90 to 1.45, a spread of more than half the bullet's width.

## It is not the camera

The obvious answer is resolution. A phone photograph has fewer pixels on the sheet than a 600 dpi scan, blur spreads the dark edge of a hole outwards, and the hole measures fat.

The numbers say no.

| photograph | pixels per inch on the sheet | ratio |
|---|---|---|
| 6.5 Creedmoor 15 shot, close and square on | 278 | 1.452 |
| the same sheet, from further back | 177 | 1.449 |
| a different sheet, same distance | 177 | 0.898 |

Two photographs of one sheet at 278 and 177 pixels per inch give 1.452 and 1.449. Two photographs of **different** sheets at the same 177 give 0.898 and 1.449. Resolution moves the number by about a hundredth; the sheet moves it by half.

Nor is it the angle. The widest reading in the whole set, 1.452, came from the squarest and cleanest photograph we have: all thirty four printed markers found, the lowest registration error of any photograph in the set.

## It is the light

Look at the two 6.5 Creedmoor sheets. Same rifle, same load, same box of bullets, same afternoon. Scanned, they read 0.949 and 0.937, which is as close as two measurements of this kind get. Photographed, one reads 0.898 and the other 1.45.

The difference between them is not the hole. It is that one was photographed at 15:33 and the other at 16:56, an hour and twenty minutes later, with the sun that much lower.

A hole in paper is not a flat black disc. It is a torn crown with a shadow in it, and the size of that shadow depends entirely on where the light is coming from. A scanner has a lamp at a fixed angle two centimeters from the paper and a white lid behind it, which is why its numbers are boring and repeatable. An afternoon is not like that.

So the thing being measured in a photograph is not the hole. It is the hole plus however much shadow was in it, and no constant can carry that.

## What GroupLab does about it

Three things, all of them consequences of the table above.

**It does not guess a caliber from a photograph.** It says so plainly and asks you what you were shooting, rather than offering a number it cannot support.

**It works out what one hole looks like from the sheet itself.** When a sheet has enough holes, GroupLab takes the size of a single hole from the holes on that sheet rather than from the caliber you typed. Whatever the light did to one hole, it did to all of them, so the sheet carries its own correction.

That change is worth a number. On the photograph that started all this, a 6.5 Creedmoor sheet with fifteen shots, GroupLab used to flag **all fifteen holes** as possibly two shots when the correct caliber was entered, because each hole measured about twice the area a 6.5 mm hole "should" be. Measured against the sheet's own holes instead, it flags **one**, which is the widest mark on the sheet and worth a look.

**It still wants your caliber**, for a different job. On the .22 LR scan, GroupLab finds 24 marks when it knows the caliber and 19 when it does not: knowing that a .22 hole is small is what stops it throwing small marks away. What the caliber no longer does is decide whether a mark is one hole or two.

## What we still do not know

The .22 LR scan reads 0.765 where the three centrefire scans read 0.92 to 0.95. A rimfire hole in paper closes up far more than a centrefire one, which makes sense, and means the "holes are about 0.94 of the bullet" figure that works for .264, .308 and .338 is not a law. We have exactly one rimfire sheet, which is not enough to say what the right number is.

We also cannot tell you how to photograph a target so that its holes measure true, because we do not know that a way exists. What we can tell you is that scanning works, and that if you photograph, GroupLab will read your sheet against itself and not against an assumption.

## What this means

**Do not read a hole size off a photograph and act on it.** Not to check a bullet's diameter, not to compare one load's holes with another's. The same holes in this test measured from nine tenths to one and a half times the bullet, and which you get depends on the light you happened to shoot in.

**Positions from photographs are fine.** This article is about size and only size. Where the shots landed, and therefore the group, came out sound from the same photographs, which is why GroupLab still reads them.

**If you need a size, scan.** The three centrefire scans agreed with each other to within three percent, in the same session, on the same sheets, with the same holes.

**And stop believing the 0.94 figure is a law.** It describes centrefire holes on this paper. The one rimfire sheet here read 0.765, and one sheet is not enough to say what the right rimfire number is.
