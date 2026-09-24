---
title: One hole or two?
description: Two shots through nearly the same spot leave one ragged hole. Deciding whether a mark is one shot or two is the single measurement that decides whether your group is real, and for a long time GroupLab got it wrong on photographs.
group: Reading targets
number: 17
written: 2026-09-22
data_date: 2026-09-20
samples: thirteen images of the developer's own sheets, nine photographs and four scans, 14 to 25 marks each, with the caliber actually shot
state: published
no_figure: "The finding is a count of review items, and it is in the article as numbers. The developer's standing consent of 2026-09-24 now allows those range sheets to be shown, and no figure has been drawn from them yet."
found: A stated caliber is the wrong reference for a photograph. On one 15 shot sheet it flagged all fifteen holes as possibly two shots. Measuring the sheet's own marks against each other instead brings that to one, and leaves scans exactly as they were.
sure: Thirteen images from one afternoon and one shooter. Enough to show that the old rule fails on photographs and that the new one does not, but the sizes it produces are not a calibrated measurement of anything.
sources:
  - "The thirteen images and what each one flagged: `docs/PHASE1-RESULTS.md`, \"The sheet's own marks decide what is one hole and what is two\"."
  - "The rule, and where it stops: `docs/NOTES-FROM-PLANNING.md`, entry 141 section 4, and entry 82 sections 1 and 3."
  - "The code: `src/GroupLab.Core/Detection/RenderDifferenceHoleDetector.cs`, `SizeReference`."
---

## The problem is real before any software touches it

Put two shots through a target within a bullet's width of each other and the paper does not record two holes. It records one hole, slightly larger and not quite round. Nothing about the paper says how many bullets went through it.

This matters more than it sounds. A ten shot group where two shots merged is measured as a nine shot group, and every figure that depends on the count is then wrong: the mean radius is computed over nine offsets instead of ten, the extreme spread is between whichever two of the nine are widest, and the shot count printed beside them says nine, so a reader has no way to know.

Worse, it is not random. Shots merge where shots are dense, which is the middle of a good group. **The better you shoot, the more likely the measurement is to undercount**, and a load that is genuinely tighter than another can be penalised for it.

## What a size can tell you, and what it cannot

A merged pair is wider than a single hole. Two circles of the same size overlapping cover more area than one, and if they overlap completely they cover exactly the same. So:

- Two shots far apart: two holes, obviously.
- Two shots touching: one mark, clearly wider than the rest, and area says so.
- Two shots through the same hole: **nothing on the paper can tell you**, ever, by any method.

The third case is not a software limitation. The paper does not hold the information. So the honest thing for GroupLab to do is flag the marks that measure too large to be one shot, say how much too large, and let the person decide.

## The rule GroupLab used, and how it failed

The obvious reference is the caliber. You shot .224, so a hole should be about that wide, and a mark much wider than that is probably two.

It holds up on a scanner. It falls apart on a photograph.

The developer photographed the targets on the bench and opened one in GroupLab with the right caliber entered. **All fifteen of its holes were flagged as possibly two shots.** Fifteen review items on a sheet with nothing wrong with it.

A hole photographed at an angle, in afternoon light, with the paper not quite flat, reads wider than the same hole scanned. Not by a little: the ratio ran from 0.90 to 1.45 across the photographs measured, against scans that agreed with each other closely. There is no single number that corrects a photograph back to a scan, because the number depends on the light, the angle and the distance.

A review queue that flags fifteen of fifteen is worse than one that flags nothing. People learn to dismiss it, and then it is not there on the day one mark really is two.

## What the sheet already knows

The answer was in the sheet the whole time.

You do not need to know how wide a hole is in inches. You need to know **whether one mark is wider than the others on the same sheet**, photographed in the same light, at the same angle, at the same distance. Every distortion that made the caliber useless applies equally to every mark on that sheet, and cancels.

So wherever a sheet has five or more round marks, GroupLab takes its reference from those marks instead of from the caliber, and it does so whether or not you named one.

It used to be twelve, and below twelve a named caliber won. A scan of ten 6.5 Creedmoor shots ended that: every hole was found, and with the right caliber named five of the ten were called possibly two, because on that paper a hole measured 1.14 times the bullet where the caliber predicted 0.945. The sheet was telling the truth and the caliber rule overrode it.

## Why the quarter-point, and not the average

The reference has to survive the very thing it is judging. If a sheet really does have three merged pairs, the average mark size is dragged up by them, and a reference built from the average would quietly excuse the doubles that pulled it up.

So the reference is the lower quartile: line the marks up by size and take the one a quarter of the way along. A merged pair is among the largest marks on the sheet, so it sits at the top of that order and moves the quarter-point not at all. With a sixth of a sheet's marks turned into real doubles the reference moves by less than a thousandth of an inch.

## What it did to the thirteen images

Every one read with the caliber the developer actually shot, stated:

| sheet | marks | flagged before | flagged now |
|---|---|---|---|
| photographs, .22 LR | 24, 24 | 15 to 24 of them | 0, 0 |
| photographs, 6 ARC | 20, 18, 21 | most of them | 1, 1, 0 |
| photograph, 6.5 CM, 25 shots | 25 | most of them | 0 |
| photographs, 6.5 CM, 15 shots | 15, 17, 14 | 15 of 15 on the one the developer opened | 1, 1, 0 |
| the four scans | 15, 25, 24, 20 | 0 | 0 |

The sheet that flagged all fifteen now flags one.

**The scans did not move.** That is the part worth checking rather than assuming: on a scan the sheet's own quarter-point and the stated caliber agree within about five percent, so switching the reference changes nothing there. A change that fixed photographs by breaking scans would have been a worse trade than the problem.

## Still enter the caliber

None of this makes the caliber pointless, and it would be easy to read it that way.

The caliber still does three jobs, and one of them is worth five holes. It sets the smallest mark GroupLab will accept as a shot at all, and on one of the developer's .22 LR scans that is the difference between finding **24 marks with the caliber entered and 19 without it**. Five real holes, lost from a group, because the software did not know it was looking for something small.

It also rescues torn holes, and it is what lets a mark's size be reported as "1.33 holes" rather than as a width in inches.

What it no longer does is decide, on its own, whether a mark is one shot or two.

## Where this stops, honestly

At around a third of a sheet doubled, the marks stop being one population with a few large ones and become two clear groups of sizes. GroupLab then takes its reference from the smaller group and flags the larger marks, and still asks you for the caliber.

That is a limit and it is stated as one: two sizes on a sheet could mean a third of the shots doubled, or two different calibers shot at it, and the sizes alone cannot tell those apart. A merged pair is 1.41 times a single hole across; .224 against .308 is 1.38. Taking the smaller group is right in both cases, because if the smaller marks are singles the doubles are flagged, and if they are the smaller caliber the larger holes are flagged and GroupLab asks one question about the caliber rather than one per hole.

## What this means

**Enter the caliber anyway.** The rule changed because the caliber was the wrong reference for deciding whether a mark is one hole or two, not because the caliber is useless. It still sets the smallest hole GroupLab will accept, and on rimfire that is the difference between finding twenty four marks and nineteen.

**A review queue of fifteen items on a fifteen shot sheet is a defect, not diligence.** If GroupLab ever asks you about every mark on a sheet, the reference it is measuring against is wrong, and the right response is to report it rather than to click through it. A flag that fires on everything teaches you to ignore flags.

**Stop believing that a hole is the size of the bullet.** It is not, it varies with the paper and the light, and the useful reference is the other marks on the same sheet, which carry the same paper and the same light.
