---
title: "Did the primer matter? A real comparison"
description: Five shots against four, two primers, one rifle. The point of impact moved and the group size did not, and only one of those two statements is worth anything.
group: Range tests
number: 3
written: 2026-09-22
data_date: 2026-09-20
samples: 5 shots with one primer and 4 with the other
state: published
no_figure: "The two groups are on one sheet, and the sheet is a real range target rather than a render. It has not been photographed for publication under a consent record yet, so the article gives the coordinates instead of the picture."
found: Changing the primer moved where the shots landed, by about a whole row of bulls, and that shift is about as certain as anything in this article. Whether it changed how tightly the rifle grouped, nine shots cannot say, and GroupLab refuses to say it.
sure: Nine shots in total, one rifle, one sitting. This is a worked example of how to read a small comparison, not a finding about primers.
sources:
  - "The sheet and its ground truth: `docs/NOTES-FROM-PLANNING.md`, entry 120."
  - "The analysis as GroupLab produced it: `docs/PHASE1-RESULTS.md`, \"Entry 120\", section 7."
  - "The statistics behind the two tests: `docs/STATISTICS.md` sections 9.2 and 10."
---

## The test

The developer loaded ten rounds of 6mm Creedmoor identically except for the primer: five with Federal GM205MAR, five with CCI BR-4. He fired the GM205MAR rounds at bulls 1 to 5 and the BR-4 rounds at bulls 6 to 10, one shot per bull, at 100 yards.

This is the comparison every reloader makes, and it is the one where it is easiest to fool yourself.

## What happened on the paper

The BR-4 shots did not land on bulls 6 to 10. They landed a whole row low, on row 3, and one of them landed a long way from everything else, left of bull 21. That shot is not a flyer to be deleted: The developer fired it, it went where it went, and it belongs to the BR-4 group.

Nine of the ten shots are on the sheet. One is not, which is itself worth saying rather than quietly analysing nine and calling it ten.

## What GroupLab says

| primer | shots | mean radius |
|---|---|---|
| GM205MAR | 5 | 0.168 in |
| CCI BR-4 | 4 | withheld |
| | | centres differ, p = 0.001 |
| | | dispersions differ, p = 0.434 |

Three things in that table are worth more than the numbers.

**The BR-4 mean radius is withheld.** Four shots is not enough to state a group size, so GroupLab does not state one. It would be easy to print 0.2 something and let you read it as a measurement. A figure with four shots behind it and no warning is worse than no figure, because you will compare it with the five shot number above it as though the two meant the same thing.

**The centres differ and that is a real result.** p = 0.001 means a shift this large would almost never happen by chance if the two primers put their shots in the same place. It matches what the developer could see standing at the bench: the BR-4 shots were a row low.

**The dispersions do not differ, and that is not a result at all.** p = 0.434 means the data cannot tell the two apart. It does **not** mean the primers group the same. Five shots against four can only detect an enormous difference, so failing to detect one tells you almost nothing. GroupLab says "no evidence either way" rather than "no difference", and those are different sentences.

## The caveat that makes the number smaller than the truth

The BR-4 shots are measured from the bulls they **landed** on, row 3, not the bulls they were **aimed** at, 6 to 10. A shot's offset is the distance from its own bull, so measuring from a nearer bull makes the shift look smaller than it was.

So the p = 0.001 shift is real and it **understates**. The true shift is about a whole row of bulls, and to get that number out of GroupLab somebody has to move those four shots onto the bulls they were aimed at, by hand, one at a time.

That is a gap in GroupLab, not in the ammunition, and it is one we are fixing: telling the software which bulls you aimed at, so a whole row of shots can be assigned at once.

## What you should take from this

If you change one component and the point of impact moves, you can often see that with very few shots, because a shift is a difference of means and means settle quickly.

If you change one component and want to know whether it groups better, you need far more shots than you think, and nine is not close. The honest answer from this test is: the primer moved the zero, and we do not know what it did to the group.

Do not let that disappoint you into reading the p = 0.434 as "they are the same". It says "we looked, with nine shots, and nine shots cannot see it".

## What this means

**One certain result and one that is not, from the same nine shots.** The shift in where the shots landed is about as solid as a small sample gets. Whether the primer changed how tightly the rifle grouped is a question nine shots cannot answer, and GroupLab says so rather than giving you a number that looks like an answer.

**So do not test two things at once and read the one you were hoping for.** The temptation with a comparison like this is to take the significant result as evidence for the whole change. It is evidence for exactly what it measured: the centre moved.

**If you want the dispersion question answered, shoot more.** Not a little more. The number of shots needed to tell two dispersions apart is far larger than the number needed to tell two centres apart, and that is a fact about the arithmetic rather than about your rifle.
