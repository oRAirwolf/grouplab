---
title: How GroupLab reads a target
description: From a photograph to a group size, in six steps, with what each one can get wrong and how you would know.
group: Reading targets
number: 5
written: 2026-09-22
data_date: 2026-09-20
samples: the six range scans of 2026-09-20 and nine photographs of the same sheets
state: published
no_figure: "The six steps are each shown on screen inside GroupLab, and the tour carries those pictures. Reproducing them here would put a second, staler copy of the same screenshots on the site."
found: GroupLab finds the printed markers, works out exactly where the page is, subtracts the artwork it knows it printed, and calls what is left a hole. Every step reports what it did, and any step can be checked against the picture.
sure: The steps below are what the current build does. The figures quoted are from one range day, six scans and nine photographs of four sheets.
sources:
  - "The pipeline and its stages: `docs/DESIGN.md` section 19."
  - "What each stage reported on the range material: `docs/PHASE1-RESULTS.md`, \"Entry 120\"."
  - "The hole measurements behind the survey: `docs/SCAN-MEASUREMENTS.md`."
---

Most target software asks you to click the holes, or asks you to place a scale by dragging a line across a ruler you photographed beside the sheet. GroupLab prints the ruler **on the target**, and everything below follows from that.

## 1. Find the markers

A GroupLab sheet carries printed square markers around its edges, the same kind robots use to find things: each one is a pattern that decodes to a number, so the software knows not just that it found a square but which square it found.

On a good scan it finds all thirty four. On a phone photograph taken at an angle it finds twenty one to thirty four, and that is enough, because it only needs four to work out where the page is.

**What can go wrong:** a sheet cut off at the edge of the frame, or so blurred that the markers do not decode. GroupLab says how many it found and refuses to go on with too few, rather than guessing.

## 2. Work out where the page is

Knowing the number and the position of each marker, and knowing where those markers were printed, GroupLab can work out the exact mapping between the photograph and the page. Not a scale: a full perspective mapping, so a sheet photographed from an angle, from the side, from above, is handled properly.

It reports how well that mapping fits, in inches. On the 2026-09-20 scans it was 0.0023 to 0.0026 in. On the photographs of the same sheets it was 0.0042 to 0.0060 in.

**This is the number that makes everything else possible.** It means a measurement on the paper is good to a few thousandths of an inch before any hole is found at all, and it means nothing has to be dragged, clicked or calibrated by you.

## 3. Draw the sheet it thinks it printed

GroupLab has the definition of the sheet in front of it: every ring, every line, every bit of text, in exact coordinates. So it renders that artwork through the mapping from step 2, producing a picture of what the paper would look like with no holes in it.

## 4. Subtract

Then it subtracts that from the photograph. What survives is what was not printed: holes, pen marks, dust, staples, the shadow of the bench.

This is why GroupLab can find a hole sitting on a black ring, which is the case that defeats a method based on "find the dark blobs". The ring is dark, and the ring is also expected, so it disappears in the subtraction and the hole in it does not.

## 5. Decide what is a hole

What survives the subtraction gets judged on its size, its shape, how round it is, and how much of it is solid rather than a thin smear. Residue from a printed edge is long and thin; a hole is compact. A staple is small and hard; a hole has a torn crown.

**Deciding one hole from two** is the hardest judgment here, and it has its own article. The short version: GroupLab works out what a single hole looks like **on this sheet, from this sheet's own holes**, rather than from the caliber you typed, because what a hole measures depends on the paper, the backing and, in a photograph, the light. On one sheet the difference between those two methods was fifteen marks flagged as possible doubles against one.

## 6. Decide which bull each shot belongs to

A shot is measured from the bull it was aimed at, so the software has to decide which that was. With one shot per bull, that is a one to one matching rather than "nearest bull", which stops one badly placed shot stealing another's bull and cascading.

**This is the step most likely to be wrong**, and the one we have most work left in. On a sheet where every shot landed a bull low and left, nearest bull is simply the wrong answer, and GroupLab will tell you it is unsure rather than quietly producing a group that means nothing.

## What it shows you

Every one of those steps is in a "Show your work" panel: how many markers, how well the mapping fit, what size a hole was taken to be and where that came from, how many candidates were refused and for what reason.

That is deliberate. A number with no working behind it is a number you have to trust. A number with the working attached is one you can check, and on a target, checking takes ten seconds: does the picture have a mark where the software says it does?

## What this means

**Every number GroupLab gives you can be traced back to a picture.** That is the point of the six steps being separate and each reporting what it did. If a result looks wrong, the useful question is not "is the software wrong" but "which of the six steps did something I can see is wrong", and you can look.

**The step that fails is almost never the last one.** A hole count that is short usually means the page was placed badly, and a page placed badly usually means too few markers were found, and too few markers usually means the photograph. So work backwards through the steps rather than arguing with the answer.

**Stop believing that subtraction is clever.** It is not. It works because GroupLab knows exactly what it printed, which is the whole reason a GroupLab sheet is easier than any other target, and it is why the same trick cannot be used on a target from somewhere else.
