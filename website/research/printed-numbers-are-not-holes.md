---
title: "A printed number is not a bullet hole"
description: "On a clean store-bought target, GroupLab's hole finder marked the printed ring numbers as shots. Telling a bold 6 from a torn hole turned out to need two measures, because the obvious one also throws away real holes."
group: Reading targets
number: 33
written: 2026-10-02
data_date: "2026-09-30"
samples: "five store-bought targets scanned blank at 600 dpi by the developer, synthetic holes drawn into them, the any-target scoreboard's drawn targets, and fifteen scans of shot commercial targets with their holes checked"
state: ready
found: "on a clean scoring bullseye, the finder took six printed marks for holes: two white 7s, two dark 6s and two letters of the maker's logo. Measuring how even a mark's strokes are, and whether a dark mark closes around a small counter as a 6 does, removed four of the six and lost no real hole. Evenness alone would have thrown away 44 real holes on scans of shot targets."
sure: "five blank targets, one with numbers, and synthetic holes drawn into them; fifteen real shot targets as the check. The thresholds have room of a few hundredths on each side, and no target with a rounded or a thin font has been measured."
sources:
  - "The measurements and the before and after: `docs/DETECTION-LEARNING-STUDY.md`, \"Store-bought blanks\" and \"Entry 352, 2026-10-02: the Eze-Scorer's numbers\"."
  - "Why the earlier test left dark marks alone: `docs/NOTES-FROM-PLANNING.md`, entry 331 section 1."
  - "The code: `src/GroupLab.Core/Detection/PrintedShape.cs`, and the drawn shapes it is held to in `tests/GroupLab.Core.Tests/Detection/PrintedShapeTests.cs`."
---

## The problem

GroupLab's "Find holes" for a target it did not print looks for marks that differ from the paper around them and are about the size of a bullet hole. On its own sheets it knows where every printed line is, and subtracts them. On a store-bought target it knows nothing about the print, so everything printed is a candidate.

Most printing gives itself away. A ring line is long and thin, a solid bull is far larger than a hole, a crosshair runs off the mark it makes. A scoring number does not. A bold 6 printed between two rings is about the size of a bullet hole, about as dark, and sits exactly where shots land.

The developer scanned five store-bought targets blank, at 600 dpi, before shooting them. On a blank sheet every mark is a false one. Three of the five gave none. One gave three, solid black diamonds, which an earlier change already refuses. The fifth, a scoring bullseye with bold numbers on its rings, gave six: two white 7s in the black ring, two dark 6s on the white paper, and two letters of the maker's logo.

## The first measure, and why it stopped at light marks

A hole is a compact patch. Even torn, the widest circle that fits inside it is most of the size of a disc of the same area. A printed number is strokes, so the widest circle inside it is only a stroke wide.

That measure already removed the thinner white numbers. It was never used on dark marks, for a reason found the hard way: in a scan, a hole on white paper often shows as a dark rim around a light center, and a rim is a thin ring. Judged by its strokes, a real hole looks like print. Four real holes on the scoreboard's drawn targets were refused that way before the test was limited to light marks.

So the bold 7s, whose strokes are wide, passed as holes, and the dark 6s were never asked.

## What a printed stroke has that a hole does not

Bold type is drawn with one pen width all along. Walk down the middle of a 6 and the distance to its edge is nearly the same everywhere. A torn hole is a wide core with spikes of torn paper that narrow to nothing.

GroupLab now measures that directly: the distance from every pixel of the mark to its edge, the ridge of those distances down the middle of each stroke, and the median of the ridge over its widest point. Printed numbers and letters measured 0.63 to 0.92. The synthetic holes on GroupLab's scoreboards measured at most 0.60.

For light marks that was enough. Both white 7s are now refused, and nothing else on any scoreboard changed.

## The trap in the obvious answer

Applied to dark marks as well, the same measure refused 44 real holes on fifteen scans of shot commercial targets. Those holes show as a crescent of dark rim, open on one side, and a crescent is drawn with one pen width too: its evenness reached 0.84.

What a 6 has and a crescent never has is a small closed counter: the light hole inside the loop. On the printed 6s the counter was 5 to 7 percent of the filled mark. A crescent encloses nothing. A rim that closes all the way around encloses most of itself, the hole's own center, which is far more than a counter. On the fifteen real scans no hole met both tests: every hole found before the change was still found after it.

So a dark mark is refused as print only when its strokes are even and it closes around a small counter, as a 6, 8, 9 or 0 does.

![A printed 6, a torn hole and a hole's rim, with their two measures](/research/printed-numbers-are-not-holes/figures/three-shapes.png)

The shapes are drawn for the picture, not taken from a target; the numbers under them are the ranges measured on the real marks.

## What it did

- The clean scoring bullseye went from six false marks to two. The two left are the logo's letters, which have no counter.
- Holes drawn into the five blanks at three calibers, on paper, on the ink, on ring lines and on the colored centers: false marks on the numbered target fell from 32 to 6, and every count of holes found stayed exactly as it was.
- Every line of the any-target scoreboard, and the fifteen shot scans (341 of 355 holes found, 352 marks), are unchanged.

No maker's artwork is used anywhere. GroupLab does not keep a picture of the target to compare against: it judges each mark by its own shape.

## What did not work

Straight edges looked promising, since a 7 or an E is mostly straight lines. Measured, the drawn holes' torn spikes are straight too, and the share of a synthetic hole's outline lying on straight runs overlapped the letters' share. There was no line to draw between them, so the logo's letters stay.

## What is still not known

- Every number measured came from one bold font. A thin or rounded font may not have strokes even enough, or may close its counters differently.
- The holes drawn into the blanks are synthetic. Scans of the same targets once shot will say whether a real hole on them ever meets both tests.
- The margins are a few hundredths wide on each measure, which is enough for these sheets and not yet a rule for every target.

## What this means

For a shooter, a proposed hole on a printed number is still worth a glance before it is accepted: Find holes on a target GroupLab did not print is Experimental, and a maker's logo can still be proposed as a shot.

For anyone building a hole finder, two lessons travel. A test that removes print by its shape has to be checked against real holes in the light they are scanned in, because a rim is drawn like a stroke. And a feature that print has and holes lack, here a small closed counter, is worth more than a sharper version of a feature both can have.
