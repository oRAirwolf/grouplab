---
title: A bullet hole is not the bullet
description: A .308 bullet does not leave a .308 hole. Paper stretches and springs back, so the hole measures smaller than the bullet, and how much smaller depends on things nobody records.
group: Reading targets
number: 2
written: 2026-09-22
data_date: 2026-09-20
samples: 176 holes photographed and 78 scanned, over the four sheets of known calibre the developer shot on 2026-09-20
state: published
no_figure: "The finding is a ratio measured over many holes, and it is in the article as a table. The developer's standing consent of 2026-09-24 now allows those range sheets to be shown, and no figure has been drawn from them yet."
found: On a scanner a hole measures about 0.94 of the bullet's diameter, consistently enough to tell one hole from two. On a photograph the same ratio runs from 0.90 to 1.45 depending on the light and the angle, so there is no photograph constant to be had.
sure: Four sheets, one afternoon, one scanner and one phone. The scanner ratio was first fixed on earlier sheets that are not published here, and rests on very few of them. Enough to tell one hole from two; nowhere near enough to report a calibre back to you from a measurement.
sources:
  - "Where the ratio came from, and what it is good for: `src/GroupLab.Core/Marking/AutomaticMarking.cs`, `HoleToCalibre`."
  - "The 176 hole re-measurement on photographs: `docs/PHASE1-RESULTS.md`, question 38."
---

## The hole is smaller than the bullet

Paper is not a solid that a bullet cuts a neat disc out of. It stretches as the bullet passes, tears, and springs partly back. What is left measures **less** than the bullet that made it.

That is the first thing to know if you ever try to work out what calibre someone shot by measuring a hole with callipers: your answer will come out small, and the amount it comes out small by is not a constant of nature.

## GroupLab's number, and where it came from

For a scanned sheet, GroupLab uses **0.945**: a hole measures about 94.5 percent of the bullet's diameter.

That figure is not from a textbook. It was measured on scanned sheets of known calibre, and on very few of them: it began as a measurement over two, and was re-measured later with more.

**Two sheets is not much**, and it is worth being plain about what a number from so few can and cannot do.

## What a rough number is enough for

Telling one hole from two is a **factor-of-two** judgement. Two bullets through nearly the same spot leave a mark about 1.4 times as wide as one, and if they are further apart, wider still. Against a difference that large, an error of a few percent in the ratio cannot flip the answer.

So a handful of sheets is enough for the job GroupLab actually uses it for.

## What it is not enough for

Reporting a size back to you as a measurement. "These holes measure .243" is a claim about absolute size, and the spread between sheets is larger than the gap between several calibres people actually shoot. GroupLab does not make that claim, and the ratio's own documentation says so in as many words, so that nobody later borrows the number for a job it cannot do.

This is a general point worth more than the specific number: **a measurement good enough for one decision can be useless for another**, and the difference is not how carefully it was taken but how large a difference it has to resolve.

## And then photographs

The obvious next step was to measure a second ratio for photographs, since a photograph is a different medium from a scan.

176 holes were measured, on nine photographs of four sheets whose calibre was known. The result killed the idea.

| what was measured | ratio |
|---|---|
| the four sheets, scanned | 0.76, 0.92, 0.94, 0.95 |
| the same four sheets, photographed | 0.90 to 1.45, sheet by sheet |

Photographs of **one** sheet agree with each other to about 0.10. Photographs of **different** sheets do not agree at all. And the spread of hole sizes within a single photograph is three to ten times the spread within a scan.

A ratio of 1.45 is not a hole smaller than the bullet. It is a hole reading half again as wide as the bullet that made it, because of light, angle and paper that is not flat.

## There is no photograph constant

That is the finding, and it is a negative one. What a hole measures in a photograph is not a property of *photographs*. It is a property of **that photograph** on that afternoon, and no constant can carry across it.

So GroupLab does not have a photograph ratio, and it should not be given one. What it does instead is stop asking the question: on a photograph with enough marks, it compares each mark against the other marks on the same sheet, which were photographed in the same light at the same angle. Every distortion that makes the absolute number useless cancels when the marks are compared with each other.

That is covered in [One hole or two?](one-hole-or-two).

## What this means

- **Do not measure a hole to identify a calibre.** Not with GroupLab, not with callipers, not at all if the answer matters.
- **Do enter your calibre.** GroupLab uses it for the smallest mark it will accept as a shot, which is worth real holes on a .22 sheet, and for reporting a mark's size in holes rather than in inches.
- **A scan is a measurement; a photograph is a comparison.** Both work. They are not the same kind of evidence, and it is worth knowing which one you handed over.
