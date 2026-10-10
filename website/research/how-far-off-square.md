---
title: "How far off square can a photograph be?"
description: "GroupLab corrects for a photograph taken at an angle, and refuses one past 37 degrees. What the measurements say about angle, what a wide phone lens adds on top, and what is still unmeasured: whether a narrower lens from farther away does better."
group: Reading targets
number: 36
written: 2026-10-10
data_date: "Range photographs of 2026-09-20; a sitting of 2026-09-29; nineteen angled photographs of 2026-09-27; measured again 2026-10-10"
samples: "31 registered range photographs of 59, 12 of them fully framed and measured against their scans; nineteen photographs of one scanned sheet from straight down to 66 degrees; four phone pictures of one sitting and two of 2026-09-26, against their scans"
state: published
no_figure: "The photographs behind it show personal papers in the background and are kept off the site; the angles and errors are in the article as tables."
found: "Up to 36 degrees off square, nineteen photographs of one sheet found every shot and placed them as well as one taken straight down. From 38.5 degrees marks that are not shots began to appear, and GroupLab refuses a photograph past 37 degrees. On the range photographs the holes kept their error as the angle grew to 32 degrees, while the bulls' centres drifted. What is left on a photograph held flat is the paper's own small relief between the printed codes, seen through a wide lens."
sure: "one sheet for the angle sweep, one phone, and a few photographs for the rest. No one has yet measured whether a narrower lens from farther away does better, so nothing here says it does."
sources:
  - "The capture work and the first angle measurements, docs/notes/archive/results-151-175.md, \"Entry 157: how the mobile application takes the photograph\" (grouplab capture-check)."
  - "Where the remaining error comes from, docs/PHASE1-RESULTS.md, \"Entry 399: accuracy on photographs (2026-10-10)\", sections 1 and 2 (grouplab photo-parts, grouplab compare-photos)."
  - "The refusal, src/GroupLab.Core/Capture/CaptureQuality.cs, OffAxisLimit. The nineteen angled photographs are in the published article Curled, angled and wrinkled paper."
---

## Two questions about a tilted photograph

A photograph taken from a little off to one side is not a problem for the reading itself. GroupLab finds the sheet's printed codes, works out how the paper sits in the picture, and maps every hole back onto the flat sheet. Two questions remain: how far can that go before it stops working, and what error does the photograph carry even when it is taken square on?

This article takes the angle first. [How to photograph a target so it measures well](/research/photographing-targets/) and [Curled, angled and wrinkled paper](/research/curled-angled-paper/) cover the light and the bend of the paper; [Scans against phone photos](/research/scans-against-photos/) covers how far a photograph sits from a scan overall.

## How GroupLab knows the angle

GroupLab reads the angle from how the sheet's codes are placed in the picture, together with the camera's focal length (or one solved from the sheet itself above 20 degrees). In GroupLab's own measurements the codes gave the angle to within 0.05 degrees up to 65 degrees. That says the angle is known well, not that the photograph is read well at that angle. They are separate questions, and the second needs real photographs.

## What real photographs showed

**The range photographs.** Of 59 photographs taken at the range, 31 registered, at 3 to 35 degrees off square. Twelve were fully framed and could be measured against their scans. Their bull centers' median error ran from 0.0024 to 0.0076 in square on, and from 0.0088 to 0.0138 in at 27 to 32 degrees. The holes' error did not change over the same range. So at those angles the holes were not the part that moved.

**The angle sweep.** On 2026-09-27 the developer photographed one scanned sheet nineteen times, from straight down to 66 degrees off square, and every shot was matched against where the 600 dpi scan puts it. The full account is in [Curled, angled and wrinkled paper](/research/curled-angled-paper/). In short:

| Angle off square | What happened |
|---|---|
| Up to 36.2 degrees (ten photographs) | All 25 shots found, each 0.013 to 0.019 in from the scan at the median, the same as straight down; one of the ten found one mark more |
| From 38.5 degrees | Marks that are not shots appear: 5 on that photograph, then 2, 6 and 17 as the angle grows |
| From 46 degrees | Shots start to be missed |
| Almost to 60 degrees | The shots that were matched still held to about 0.03 in |

What fails first is not where a hole is, but telling a hole from a mark on the paper that the angle has squashed into the same shape.

## The limit GroupLab uses

GroupLab refuses a photograph taken more than 37 degrees off square, and says both the angle and the limit. The limit sits between the last photograph that agreed with the scan (36.2 degrees) and the first that did not (38.5). It was 40 before anything real had been measured past 35. A photograph that is a little over, 37.3 degrees say, is described as "a little over 37".

One more measurement bears on it. A computer-made sheet turned through the lens GroupLab assumes found all 50 holes with no false mark at every angle from 5 to 45 degrees in steps of 5, on two random seeds. That is a rendered sheet with no lens to fit, so it shows no weakness in the method over that range and says nothing about a phone.

## What is left when the photograph is square

Even a photograph taken close to square has an error in where it puts the printed bulls, and that was measured on 2026-10-10.

**First, a finding about the photographs themselves.** GroupLab decides that a picture is a photograph by its focal-length tag. The photographs kept for testing had been stripped of every tag, so each was read as a flat scan with no lens correction. Read through the lens, the worst bull error on the four phone pictures of that sitting and the two of 2026-09-26 fell, in inches:

| Picture | Before | After |
|---|---|---|
| Sitting picture 1 | 0.0155 | 0.0124 |
| Sitting picture 2 | 0.0176 | 0.0117 |
| Sitting picture 3 | 0.0216 | 0.0077 |
| Sitting picture 4 | 0.0186 | 0.0143 |
| 2026-09-26, first sheet | 0.0070 | 0.0057 |
| 2026-09-26, second sheet | 0.0133 | 0.0102 |

GroupLab now reads a picture with no camera tags through the lens when its codes fit a flat page worse than 0.003 in rms. No photograph got worse at its worst bull. The phone and the upload page keep the camera's tags, so they already read through the lens. A photograph that loses its tags on the way, sent through a messaging app or taken as a screenshot, did not.

**Second, what remains.** The codes fit to 0.0022 to 0.0029 in, yet the smooth error at the worst bull was up to 0.0104 in. So it is not in the fit at the codes: it lies between them. It points along the image's radius, but its sign changes from bull to bull and from picture to picture, which a lens profile that was simply wrong would not do. What is left is the paper's own relief between the codes, seen through a wide lens. The sitting used the phone's 13 mm equivalent ultrawide, where a ray to the outer bulls is about 40 degrees off the lens's axis, and 0.1 mm of paper height there moves a bull about 0.003 in.

That 40 degrees is a different thing from the 37 above: it is how far a ray at the edge of the picture leans from the lens, not how far the phone leans from the sheet.

The worst bulls were always on the outside of the grid, 1.06 in from the nearest code.

## What this means

- **Hold the phone within about 35 degrees of square.** Past 37 GroupLab refuses the picture; nothing past 36.2 degrees, against a scan, read as well as a square one.
- **Square is better still.** The range photographs' bull centers had a larger error at 27 to 32 degrees than square on. The holes' error did not change.
- **A picture that lost its camera tags is no longer a loss.** GroupLab now reads one through the lens when its codes do not fit a flat page, so nothing needs doing.
- **Do not expect a flat-looking sheet to be flat.** What remains on a photograph held flat is the paper's relief, not the camera's resolution.

## What is not known

- **Whether a narrower lens from farther away helps.** Every picture so far was taken with the ultrawide at close range, and the leftover error is the paper's relief magnified by that wide lens. A narrower lens would put the outer bulls nearer its axis, which is a reason to try it and not a result. The photographs that would show it have been asked for and not yet taken.
- **More than one sheet, one phone and one sitting.** The angle sweep is one sheet and one phone. A different phone, paper or room may move the line.
- **Photographs of mounted sheets at an angle.** The 37 degree limit comes from a sheet laid on a desk. A sheet hanging at the range is bent differently, and was measured separately.
- **Anything between 36.2 and 38.5 degrees.** The limit is a line drawn through that gap, not a measurement inside it.
