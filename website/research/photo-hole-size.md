---
title: Why a photo cannot tell you your bullet's size
description: The same holes measure 0.90 to 1.45 times the bullet depending on the light they were photographed in. Scans of the same sheets agree within three percent.
group: Reading targets
number: 1
written: 2026-09-22
data_date: 2026-09-20
samples: 176 holes photographed and 78 scanned, over four sheets of known calibre
status: published
found: A hole in paper photographed with a phone measures anywhere from nine tenths to one and a half times the bullet that made it, and which end you land on depends on the light rather than on the bullet. Scanned, the same four sheets all read between 0.92 and 0.95.
sure: Four sheets, nine photographs, 176 holes, one shooter, one afternoon. Enough to show that no single correction exists; not enough to tell you what the correction would be if one did.
data:
  - hole-ratios.csv
sources:
  - "The measurement and what it changed: `docs/PHASE1-RESULTS.md`, \"What a hole measures in a photograph\"."
  - "The question it answered: `docs/QUESTIONS-FOR-PLANNING.md`, question 38."
  - "Where the scan figures came from: `docs/SCAN-MEASUREMENTS.md` section 3.5."
---

![Measured hole size as a fraction of the bullet, for four sheets, scanned and photographed](/research/photo-hole-size/figures/lead.svg)

Every dot is one image of one sheet. Left of the dashed line the holes measured smaller than the bullet; right of it, larger.

## The question

If you photograph a target and GroupLab measures the holes, can it tell you what you were shooting?

It is a fair thing to want. The hole is right there, the software has already measured it to a thousandth of an inch, and a calibre is one more number it could hand you. It would also be useful in the other direction: if GroupLab knows the calibre, it can tell one hole from two, because two bullets through one hole make a mark about twice the area of one.

So we measured it, on four sheets whose calibre we knew for certain because Alan loaded them.

## What we did

Four sheets from one afternoon: a .22 LR block, a 6 ARC block, and two 6.5 Creedmoor sheets. Each was scanned at 600 dpi on a flatbed and photographed with a phone, some square on, some at an angle, some close, some from across the bench.

Every image went through GroupLab's ordinary path: find the printed markers, work out where the page is, and measure each hole. Then we divided the median hole by the bullet diameter Alan actually fired.

Every sheet registered cleanly. The scans came out at 0.0023 to 0.0026 inches of registration error and the photographs at 0.0042 to 0.0060, so nothing below is a sheet GroupLab could not read.

## What came out

| sheet | bullet | scanned | photographed |
|---|---|---|---|
| .22 LR block | 0.224 in | 0.758 | 1.069, 1.077 |
| 6 ARC block | 0.243 in | 0.923 | 1.256, 1.333, 1.360 |
| 6.5 Creedmoor, 25 shots | 0.264 in | 0.949 | 0.898 |
| 6.5 Creedmoor, 15 shots | 0.264 in | 0.937 | 1.452, 1.405, 1.449 |

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

The difference between them is not the hole. It is that one was photographed at half past three and the other at five, with the sun an hour and a half lower.

A hole in paper is not a flat black disc. It is a torn crown with a shadow in it, and the size of that shadow depends entirely on where the light is coming from. A scanner has a lamp at a fixed angle two centimetres from the paper and a white lid behind it, which is why its numbers are boring and repeatable. An afternoon is not like that.

So the thing being measured in a photograph is not the hole. It is the hole plus however much shadow was in it, and no constant can carry that.

## What GroupLab does about it

Three things, all of them consequences of the table above.

**It does not guess a calibre from a photograph.** It says so plainly and asks you what you were shooting, rather than offering a number it cannot support.

**It works out what one hole looks like from the sheet itself.** When a sheet has enough holes, GroupLab takes the size of a single hole from the holes on that sheet rather than from the calibre you typed. Whatever the light did to one hole, it did to all of them, so the sheet carries its own correction.

That change is worth a number. On the photograph that started all this, a 6.5 Creedmoor sheet with fifteen shots, GroupLab used to flag **all fifteen holes** as possibly two shots when the correct calibre was entered, because each hole measured about twice the area a 6.5 mm hole "should" be. Measured against the sheet's own holes instead, it flags **one**, which is the widest mark on the sheet and worth a look.

**It still wants your calibre**, for a different job. On the .22 LR scan, GroupLab finds 24 marks when it knows the calibre and 19 when it does not: knowing that a .22 hole is small is what stops it throwing small marks away. What the calibre no longer does is decide whether a mark is one hole or two.

## What we still do not know

The .22 LR scan reads 0.758 where the three centrefire scans read 0.92 to 0.95. A rimfire hole in paper closes up far more than a centrefire one, which makes sense, and means the "holes are about 0.94 of the bullet" figure that works for .264, .308 and .338 is not a law. We have exactly one rimfire sheet, which is not enough to say what the right number is.

We also cannot tell you how to photograph a target so that its holes measure true, because we do not know that a way exists. What we can tell you is that scanning works, and that if you photograph, GroupLab will read your sheet against itself and not against an assumption.
