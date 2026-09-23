---
title: Curled, angled and wrinkled paper
description: A target you have laid flat on a bench is not flat. Measured against thirty known points, a sheet that looked flat was bent by two thirds of an inch, and that bend is larger than the groups people argue about.
group: Reading targets
number: 18
written: 2026-09-22
data_date: 2026-09-20
samples: one commercial sheet photographed flat on a bench, fitted against thirty known bull centres, compared with pinned and flat frames from an earlier survey
state: published
found: A flat model of a laid-down sheet leaves 0.018 to 0.021 in of error. A bent surface takes that to 0.0063 in. The sheet had a fold across its bottom edge, and fitting for it was worth three times the accuracy.
sure: One sheet, one camera, one afternoon. The size of the effect is measured; how much a different sheet on a different bench bends is not.
sources:
  - "The fit, and what each model left behind: `docs/PHASE1-RESULTS.md`, the flatness table."
  - "The earlier pinned and flat frames it is compared against: the same document, measurement M1.11."
---

## Flat is a word, not a measurement

Put a paper target on a bench, smooth it with your hand, and photograph it from above. You would describe that sheet as flat.

It is not flat, and the amount by which it is not flat matters.

A commercial sheet handled that way was photographed and then fitted against the thirty bull centres printed on it, whose true positions are known. Fitting a **flat plane** through them, allowing for the lens, leaves an error of 0.018 to 0.021 inches at RMS, and up to 0.060 inches at the worst bull.

Fitting a **bent surface** takes the same photograph to 0.0063 inches RMS, with its worst bull at 0.017.

Whatever the remaining 0.006 is, it is not distinguishable from random. The 0.02 was not random at all. It was a shape.

## The shape had a cause you could see

The fitted surface says the sheet deflected by 0.694 inches. That is two thirds of an inch of bend in a sheet somebody had laid flat and would have called flat.

And it was visible in the photograph: the sheet had a fold across its bottom edge, presumably from a range bag. The fitted number is not a statistical artefact; it is the fold, measured.

Fitting a simple cylinder, which is what a sheet curling up at one edge looks like, got a deflection of 0.332 inches and an RMS of 0.0078. Allowing the more general bend the fold actually had halved the remaining error again.

## Why two hundredths of an inch is a lot

It sounds small. Set it against the things people use targets to decide.

A two hundredths of an inch systematic distortion across a sheet is the same order as the difference people report between two loads and treat as meaningful. If that distortion varies across the sheet, as a bend does, it does not cancel: it stretches one part of the group and squashes another, and the group's measured shape changes.

So a bent sheet does not merely add noise. It adds a **pattern** that looks like a result.

## Where a laid-down sheet sits

An earlier survey measured how much registration error was left after fitting, on sheets that were pinned to a board and on sheets that were genuinely flat. Per point, after the fit:

| how the sheet was held | error left after fitting |
|---|---|
| flat frames | 0.6 to 0.75 dmm |
| this sheet, laid on a bench, with a bent surface fitted | 1.60 dmm |
| pinned to a board | 1.3 to 3.3 dmm |

A sheet you laid on a bench and photographed sits **between** a truly flat one and a pinned one, and needs the bent-surface fit as much as a pinned one does. Pinning a target does not make it flat; it makes it a different shape.

Those are not quite the same measurement, since one set is marker corners on a GroupLab sheet and the other is ring centres on a commercial one. They are the same kind of number.

## The failure that had nothing to do with flatness

The same photograph produced a more dramatic failure worth knowing about, because it is the one that will bite you first.

Run on the **whole photograph**, the hole detector found nothing at all. Not fewer holes: zero.

The sheet was lying on a dark mat. The detector looks for dark marks on light paper, and the mat is dark everywhere, so it became a single blob twelve inches across that swallowed every hole and was then refused for being far too large.

Cropped to the sheet alone, the same detector on the same image found 26 of the 28 holes.

The assumption that broke is one a scanner always satisfies and a photograph often does not: **the paper fills the frame**. On a scanner the paper is the whole image. On a bench it is a rectangle in the middle of whatever else is there.

## What this means when you photograph a target

- **Light background, or crop to the sheet.** A dark bench mat is the single most effective way to get nothing at all out of a photograph.
- **A fold costs you more than a wrinkle.** The fitted bend followed the crease. Store targets flat if you can, and if you cannot, expect the fitted correction to be doing real work.
- **Two of 28 holes were still missed**, and both were a round hole and a keyhole that merged into one blob in the photograph. See [One hole or two?](one-hole-or-two).
- **The centres moved.** Holes found on the photograph sat 0.023 inches from the same holes found on a scan of the same sheet, four times the model's own residual. A scanned hole is lit by the scanner lid, a photographed one by whatever is behind the paper, and a ragged rim reads differently against each.

## The limit

One sheet, one camera, one bench. The size of the bend is measured properly against known points, and the conclusion that a laid-down sheet needs a curved fit is solid for that sheet.

How much any *other* sheet bends is not established, and cannot be from one measurement. What you should take is not the number 0.694 but the finding that the number is not zero, and is large enough to matter.
