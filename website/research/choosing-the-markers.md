---
title: Choosing the markers
description: The little black squares on a GroupLab target are AprilTag 36h11. The choice came down to one number, and the weaker options were measured failing on real target artwork rather than rejected on theory.
group: How GroupLab is built
number: 24
written: 2026-09-22
data_date: 2026-09-22
samples: six marker dictionaries compared, with false-positive counts measured on real scanned target artwork under a permissive detector
state: published
found: Dictionaries with a minimum Hamming distance of 4 or less produced false marker detections on ordinary target artwork. Every dictionary at Hamming 11 or above produced none. That one number decided it.
sure: The false-positive test used one corpus of real scans. The Hamming figures were recomputed from rendered markers rather than taken from documentation, and agreed with it.
sources:
  - "The comparison and the measurement: `docs/FIDUCIAL-DECISION.md`, sections 4.1 and 9."
  - "Why registration matters this much: `DESIGN.md`, section 9."
---

## What the squares are for

Every measurement GroupLab makes depends on knowing exactly where the paper is in the picture. A scan is skewed a little; a photograph is skewed a lot and bent as well. The markers are the known points that let the software work out the transformation and measure in inches on the paper rather than pixels in the image.

Get that wrong and everything downstream is wrong in a way that looks fine.

## The requirement that decided it

A marker dictionary has a **minimum Hamming distance**: how many bits must be misread before one valid marker can be mistaken for another. Higher is more robust and costs more space on the page.

The usual trade-off is robustness against size. For GroupLab the trade-off is lopsided, because of what a target page contains.

A page is covered in **exactly the things that generate false squares**: printed rings, numerals, cell boundaries, the ruled lines of a load-data block, handwriting where somebody drew an arrow from a hole to its bull, and bullet holes themselves. A false detection injects a wrong correspondence into the fit.

There is protection against that, and there is a lot of redundancy: 34 markers where 4 would do. One false positive is survivable. But the cost of a false positive is not symmetrical with the cost of a slightly larger marker, and a page like this generates candidates constantly.

## What was measured

Six dictionaries, rendered and run under a deliberately permissive detector against real scanned target artwork:

| dictionary | modules | identifiers | min Hamming | false positives on real artwork |
|---|---|---|---|---|
| ArUco original | 7 x 7 | 1024 | **1** | **never use** |
| 4 x 4 | 6 x 6 | 1000 | 4 | **produced false positives** |
| 5 x 5 | 7 x 7 | 1000 | 4 | **produced false positives** |
| 6 x 6 | 8 x 8 | 1000 | 6 | none |
| ArUco 36h11 | 8 x 8 | 250 | 11 | none |
| **AprilTag 36h11** | **8 x 8** | **587** | **11** | **none** |

The Hamming figures were recomputed from rendered markers rather than believed from documentation, and agreed with it.

**The 4 x 4 and 5 x 5 dictionaries failed on real target artwork.** Not on a contrived adversarial image: on scans of targets people had shot. That is the measurement that ended the discussion, and it is the reason this was measured rather than argued.

## Between the two that were left

ArUco 36h11 and AprilTag 36h11 are the same size, the same strength and indistinguishable on every property GroupLab cares about. Both scored zero false positives.

Two things separated them:

**Identifiers.** 587 against 250. A large sheet, or a multi-page assembly, uses a lot of markers, and running out of unique identifiers is an unpleasant constraint to discover late.

**Two independent readers.** AprilTag 36h11 is the only strong family that both OpenCV and the BSD-licensed AprilTag reference implementation can read. That removed a licence conflict from the critical path and means the format does not depend on one library's continued existence.

Choosing it cost nothing and removed a problem. That is the easiest kind of decision, and it only looked easy because the hard part, the false-positive measurement, had already been done.

## The size on the page

Printed at a 0.5 mm module, a marker is 4.0 mm square, and they sit on the boundaries between cells where they do not interfere with the bulls.

Small enough that a page of them does not look like a page of them, large enough to survive a phone photograph from a reasonable distance. When the sheet is too small in the frame, the markers stop decoding, and that turns out to be the commonest reason a photograph cannot be read at all: 27 of 59 range photographs failed with no code found.

## The general lesson

The interesting part of this decision was not picking the winner. It was that **the plausible cheap options were measured failing** on the actual material, rather than being ruled out by an argument about Hamming distances.

An argument would have reached the same answer here. It does not always, and the cost of finding out in the field, on somebody's targets, is that you never find out at all: a false marker does not announce itself, it just moves your group slightly.
