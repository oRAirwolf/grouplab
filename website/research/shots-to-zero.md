---
title: "How many shots to zero?"
description: "Landing within one click of a perfect zero takes a handful of shots. Landing on exactly the right click can take hundreds, because a true zero near the line between two clicks is very hard to resolve. The numbers, and what a one-shot zero can honestly say."
group: Measuring groups
number: 32
written: 2026-09-28
data_date: "Worked out exactly, and checked against 200,000 simulated zeroing sessions"
samples: "The zeroing procedure worked out exactly for known sigma; for sigma estimated from a group, 4,000 stratified draws of the sigma the group could have"
state: published
no_figure: "the three rows of the table are the result; the curve against shots is in GroupLab itself, drawn for your own group"
found: "with a rifle whose shot-to-shot spread is one click in each direction, five shots put you within one click of the best zero 99 times in 100, but landing on exactly the closest click needs 242 shots for a 90 percent chance and more than a thousand for 95. Within one click is what most zeroing needs."
sure: "exact, for a round group whose spread is known, and checked against a shot-by-shot simulation of the procedure; where the spread is estimated from a few shots, the answer is a simulation, and it asks for more shots because the spread may be larger than it looks. It assumes the scope moves exactly one click per click."
sources:
  - "GroupLab statistics reference, sections 2 and 8.2 on group centres (docs/STATISTICS.md)."
  - "Suggested by Jylee, a friend of the developer's, who asked for a one-shot zero on the phone."
  - "Standard error of the mean. https://en.wikipedia.org/wiki/Standard_error"
---

## The question

You zero by firing a group, finding its center, and moving the scope by that much, rounded to whole clicks. How many shots should that group be? Jylee asked for the extreme case, a one-shot zero on the phone, and it turns out to be the first row of a table this page works out.

Two things can be meant by "zeroed":

- **The closest click.** Your scope can only stop on whole clicks, and the rifle's true zero is almost always somewhere between two of them. The best any adjustment can do is land on the click nearest the true zero, on both windage and elevation.
- **Within one click.** Landing on that closest click or on one either side of it, on both axes.

## The procedure, worked out

Say the rifle's shot-to-shot spread is sigma in each direction, measured in clicks. The true zero lies anywhere within a click. Fire n shots; their center is off from the true zero by a normal amount with spread sigma divided by the square root of n. You then round that center to the nearest click. The chance you land on the closest click is the chance the rounding picks the right one, averaged over where in its click the true zero sits; the chance you land within one click is the same with a wider window. Both have exact formulas, and GroupLab uses them.

| Spread of the rifle, one direction | Within 1 click, 99 percent | Closest click, 90 percent | Closest click, 95 percent |
|---|---|---|---|
| half a click | 2 shots | 61 shots | 249 shots |
| 1 click | 5 shots | 242 shots | 993 shots |
| 2 clicks | 19 shots | 967 shots | more than 1,000 |

The pattern is the point. Within one click comes quickly, because a group's center only has to be right to about a click. The closest click comes slowly, and sometimes never within any practical group, because a true zero that sits near the halfway line between two clicks is almost equally close to both, and telling them apart takes a center pinned down to a small fraction of a click.

What does a click of spread mean on paper? With quarter-MOA clicks at 100 yards, a rifle that shoots five-shot groups of about 1 MOA has a spread of about 0.35 MOA in each direction, which is 1.4 clicks.

## A group's own spread is uncertain too

The spread is itself measured from shots. A five-shot group can look tighter than the rifle really is, and a zero planned on that luck needs more shots than it expects. GroupLab allows for it by drawing the spread the group could really have, many times over, and averaging: with a spread of one click measured on five shots, within one click at 99 percent needs 9 shots rather than 5. With 25 shots behind the spread, it needs 6. This part is a simulation, so its numbers can move a little between runs; GroupLab shows how many trials it used, the seed, and how far a count could move, and the same seed always gives the same answer.

## What one shot can say

A one-shot zero is the n = 1 row. One shot's offset from the aim is the zero error plus that shot's own spread, so it can only honestly call an error that is clearly bigger than the rifle's spread: about twice sigma. For the 1 MOA rifle above that is an error over about 0.7 MOA, three quarter-MOA clicks. Anything smaller could be the rifle's own scatter, and the right answer is to fire more before adjusting. One shot is for getting on paper and for large errors, not for a fine zero.

## What this means

- **Decide which goal you need.** Within one click is what most zeroing needs, and five to ten shots usually give it. Chasing the exact closest click with a small group is chasing noise.
- **Do not trust a zero from a group of three or four shots to the click.** Its center is uncertain by most of a click for a typical rifle, and its spread is uncertain too.
- **Adjust on one shot only for a big miss.** Anything within about twice the rifle's spread could be the rifle, not the scope.

## Where to find it

GroupLab's analysis screen has a section named Shots Needed to Zero in its Advanced figures, beside the full CEP table. It takes the group you analyzed and your scope's click value, from the rifle record or chosen there, and gives the shots for 90, 95 and 99 percent on both goals, each axis alone, and the curve against shots. A box treats the measured spread as exact, which drops the simulation.
