---
title: Zeroing on a blank sheet with a hand-drawn cross
description: You do not need a printed target to zero a rifle. You need an aim point and a known distance on the paper, and a flatbed scanner already knows the second one.
group: Reading targets
number: 20
written: 2026-09-22
data_date: 2026-09-20
samples: one blank sheet shot at a marker dot and scanned at 600 dpi, checked against the scanner's stated resolution
state: published
found: A scan states its own resolution, and on a blank sheet that is a complete scale with no ruler needed. A photograph's stated resolution describes the file and not the paper, so it is never offered one.
sure: One sheet, one scanner. The principle is sound because the number is written in the file by the device that made it; what is not established is how often a scanner lies about it.
sources:
  - "What is offered and what is refused: `docs/NOTES-FROM-PLANNING.md`, entry 130 section 4.1."
  - "The blank sheet, scanned and read: `docs/PHASE1-RESULTS.md`, entry 130 section 5."
---

## The thing a target is actually for

A printed target does two jobs. It gives you something to aim at, and it gives the software something to measure by.

For zeroing, the first job can be done by a marker dot, and most people have already discovered that. The second is the one that quietly stops you using a blank sheet with software, because a picture of a piece of paper with holes in it contains no information about how large anything is.

Every figure depends on getting that right. A scale that is out by a factor of two turns a one inch group into two inches, and **nothing on the screen looks unusual** while it does. That is the worst class of error: silent, plausible, and wrong throughout.

## What a scanner already knows

A flatbed scanner knows its own resolution, and it writes it into the file. A PNG carries a `pHYs` chunk; one of Alan's blank sheets carries 23622 pixels per metre, which is exactly 600 dots per inch.

If a scan is 600 dpi, then 600 pixels is one inch of paper. That is a complete scale, from the device that made the image, with no ruler in the picture and nothing for you to measure.

So GroupLab offers it.

## Offered, with the number shown, never applied on its own

It is deliberately an offer rather than a default, and it is worth saying why, because "the file says so, use it" is the tempting shortcut.

A scale is not like other readings. Most of what GroupLab works out, you can sanity-check by looking: a shot in the wrong place looks wrong. A wrong scale changes every number by the same factor, so the group still looks like a group and the figures still look like figures. There is nothing to notice.

So the number is put on the screen, in words, and you say yes. Ten seconds against a whole session's figures being quietly wrong.

## What is refused, and why each one

**A photograph is never offered a scale from its file.** A photograph's stated resolution, where it has one at all, describes the image file: how large someone intended to print it, or what a camera app happened to write. It says nothing whatever about how far the camera was from the paper. Offering it would be offering a number that looks like a measurement and is not one.

**72 and 96 are never offered.** Those are the numbers a file gets when whatever wrote it had nothing to say. A scanner that really did scan at 96 dpi would be producing an image too coarse to find a bullet hole in, so treating 96 as a claim is a mistake either way.

**A stretched scan says so instead of offering a number.** Some scanners record different resolutions horizontally and vertically. There is no single scale for such a scan, and a group measured on one is wrong in one axis only, which is the hardest kind of wrong to see: the group looks slightly oval, and every group is slightly oval anyway.

## What to do if you have no scanner

Draw your cross, shoot, and measure a known distance on the paper afterwards with a ruler: the width of the sheet, or two marks you made before shooting. GroupLab takes a reference length or a reference rectangle, and either is as good as the scanner's number as long as you measure carefully.

If you photograph it, do measure something real. A photograph will not tell you and should not pretend to.

## The honest limit

This rests on one sheet and one scanner. The principle is solid, because the resolution is written by the device that did the scanning rather than inferred from the picture. What is not established here is how often a scanner writes a resolution it did not actually use, which is the failure mode that would matter, and which is exactly why GroupLab shows you the number instead of quietly using it.
