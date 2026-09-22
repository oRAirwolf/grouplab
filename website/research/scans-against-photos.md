---
title: "Scans against phone photos: how close is close enough?"
description: We paired 59 range photographs with their flatbed scans and measured the gap. A hole in a photograph sits about 0.03 in from where the scan puts it, which is a fifth of a decent group.
group: Reading targets
number: 9
written: 2026-09-22
data_date: 2026-09-20
samples: 59 photographs of sheets stapled to corrugated plastic outdoors, paired against six 600 dpi flatbed scans of the same sheets
status: published
found: Of 59 photographs, 28 could not be read at all. Of the 15 that paired with a scan, a hole's position was out by about 0.03 in at the median and 0.07 in at the 95th percentile, against a mean radius of about 0.17 in.
sure: One shooter, one afternoon, one phone and one scanner. The gap is measured properly against scans of the same sheets; how a different phone or a different day would compare is not established.
sources:
  - "The pairing and the figures: `docs/PHASE1-RESULTS.md`, \"Each photograph paired with its own scan\"."
  - "The registration half: the same document, \"The mounted photograph gate, measured for the first time\", and `DESIGN.md` [r10]."
---

## The question people actually ask

Scanning a target is a nuisance. You have to take it home, flatten it, and feed it through a machine. Photographing it takes four seconds at the bench.

So: how much do you lose?

Until now GroupLab could not answer that, because nobody had photographed a set of targets and scanned the same ones. In September a shooter did: 59 photographs of sheets stapled to corrugated plastic outdoors at many angles, and flatbed scans at 600 dpi of the same sheets.

## The first answer is not about accuracy

**28 of the 59 photographs could not be read at all.**

Twenty-seven of them because GroupLab could not find the printed code that says which target it is looking at. One because it found the code and then no markers.

That is the headline, and it is not a rounding error: a photograph of a mounted sheet failed to be read about as often as it was read. Before any question of how accurate a photograph is, roughly half of a real day's photographs never got as far as producing a number.

**And it is not only a photograph problem.** One of the six flatbed scans could not be read either, for the same reason. A 600 dpi scan, flat, evenly lit, and GroupLab could not identify the sheet.

## For the ones that did read

Of the 31 that registered, 15 could be paired with a scan of the same sheet by matching their hole patterns. Those 15 are the only honest comparison available, because a photograph compared against a scan of a *different* sheet tells you nothing.

| how far a hole sits from where the scan puts it | across the 15 photographs |
|---|---|
| median | **0.032 in** |
| 95th percentile | **0.070 in** |
| worst | **0.085 in** |

Set that against a group. Scan 6's mean radius is about 0.17 in. So a shot's position in a photograph is out by roughly **18 percent of the group's own size** at the median, and 40 percent at the 95th percentile.

## What that means you can and cannot do

**Counting shots: yes.** Every fully framed photograph matched 12 to 20 of the 14 to 20 holes on its scan. If you want to know how many shots are on the paper and roughly where, a photograph does that.

**Seeing a flyer: yes.** A shot an inch from the others is an inch from the others in a photograph too. Three hundredths of an inch does not hide that.

**Zeroing: probably.** A zero correction is the group's centre, and averaging twenty shots shrinks a random error considerably. This was not measured directly, so "probably" is the honest word.

**Comparing two loads: no.** The differences people are trying to detect between loads are smaller than the error a photograph adds. You would be comparing photographs, not ammunition.

## Why a photograph is worse, and it is not the megapixels

It would be easy to assume this is resolution. It is not.

A sheet on a bench is **not flat**. Fitted against thirty known bull positions, a sheet somebody had laid down and smoothed with their hand turned out to be bent by two thirds of an inch, following a fold from a range bag. Fitting a flat plane to it leaves three times the error that fitting a curved surface does.

A scanner presses the paper flat against glass and lights it evenly from a known distance. That is the whole of the difference, and no camera fixes it.

There is more on that in [Curled, angled and wrinkled paper](curled-angled-paper).

## If you are going to photograph

- **Fill the frame with the sheet.** The commonest failure by far was the code not being found, and a sheet occupying a third of the picture is the reason.
- **Get square on.** The photographs that located only four or five bulls were the oblique ones.
- **Flatten it.** A fold costs more than a wrinkle, because the fitted correction has to follow it.
- **Light background.** A dark bench mat made the hole detector find nothing at all on one frame: the mat became a single dark blob twelve inches across that swallowed every hole.
- **Scan it if the answer matters.** Not because photographs are useless, but because you now know what they cost.

## The honest limits of this

One shooter, one afternoon, one phone, one scanner. The gap between photograph and scan is measured properly, against scans of the same sheets, with the pairing done by matching hole patterns rather than by trusting file names.

What is not established: how a different phone compares, how much of the 0.03 in is the bend and how much is the light, and whether a photograph taken deliberately well does better than these, which were taken the way somebody actually photographs a target at a range.
