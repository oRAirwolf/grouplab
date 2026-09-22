---
title: Pooling groups: when two sheets are one load
description: Forty shots tell you far more than twenty. But two sheets shot an hour apart have two centres, and which centre you pool them around decides whether you are measuring the ammunition or the afternoon.
group: Measuring groups
number: 12
written: 2026-09-22
data_date: 2026-09-20
samples: two scans of one load, 40 shots, plus a 343 hole pooled set from the earlier survey
status: published
found: Pooling is not arithmetic, it is a choice. Centre all the shots together and you measure the rifle over a day; centre each sheet on itself and you measure the ammunition. The two numbers differ, and the difference is itself the useful finding.
sure: The statistics are exact. Which of the two a shooter wants is a judgement, and GroupLab does not yet make it for them, on purpose.
sources:
  - "The three options and why one was not chosen quietly: `docs/QUESTIONS-FOR-PLANNING.md`, question 34."
  - "What was asked for: `docs/NOTES-FROM-PLANNING.md`, entry 130 section 4.2."
---

## Why you would want to

Almost every question worth asking of a target needs more shots than people fire. Five shot groups cannot separate loads. Ten shot groups can barely separate a rifle from itself.

So if you shot the same load on two sheets, twenty shots each, the obvious move is to treat them as one forty shot group. Forty shots is a genuinely better estimate than twenty, and the improvement is not small: the uncertainty in a dispersion figure falls roughly with the square root of the count.

The arithmetic is easy. Gather the shots, keep each one's sheet and bull, compute over the combined set. Nothing about mean radius or sigma cares which sheet a shot came from.

That part is plumbing. The problem is somewhere else.

## Two sheets have two centres

You shot the first sheet in the morning and the second after lunch. Between them the rifle was picked up and put down, the ammunition warmed in the sun, the wind changed, and you sat differently.

The two sheets have two points of impact, and they are not the same point. They are usually close, and they are essentially never identical.

So "the pooled group's centre" is not a fact you look up. It is a decision, and there are three defensible answers that measure **different things**.

## A: one centre for all forty shots

Treat the forty shots as one cloud and find its centre.

The dispersion you get then includes the movement between the two sessions. A shot from the morning is measured from a centre that sits between the morning and afternoon points of impact, so it is further out than it was on its own sheet.

**This measures what you and the rifle will do over a day.** If you are zeroing for a match where you will not re-zero between strings, this is the honest number.

## B: centre each sheet on itself, then pool the radii

Measure each sheet's shots from that sheet's own centre, and pool the resulting distances.

The movement between sessions is thrown away entirely. What is left is the within-session dispersion.

**This measures the ammunition and the rifle**, with the day's wandering removed. If you are comparing two loads, this is the number you want, and it is the one that stays comparable with every ordinary single-sheet group in your records.

## The trap in B, which is why GroupLab has not just picked it

B produces a mean radius. So does an ordinary twenty shot group. They look identical on a screen: same name, same units, same number of decimal places.

They are not estimates of the same quantity. One includes the session-to-session movement and one does not, and nothing on the screen would tell you which you were looking at.

That is the failure this project cares most about: **a number that means something other than what the reader thinks it means**. It is worse than a missing number, because a missing number prompts a question.

## C: report both, and name the difference

Give both figures, and state the gap between them as its own quantity, in inches.

That gap is not an inconvenience to be hidden inside a bigger mean radius. It is a finding:

- **A close to B:** the rifle held its zero between sessions. Your load comparison and your day-long expectation are the same number.
- **A much larger than B:** the rifle or the setup moved between sessions. That is worth knowing on its own, and it is invisible in either figure taken alone.

## Where GroupLab stands

This is not built yet, and that is deliberate rather than a gap in the schedule.

The first thing the code has to do is pick a centre, so the choice cannot be deferred until after the feature exists. Building it with a centre picked quietly and changing it later would mean every pooled figure recorded in between is not comparable with the ones after it, and the record book keeps figures for years.

The recommendation on the table is **C, with B as the headline**: a person pooling two sheets of one load is nearly always comparing loads, so the within-session figure leads, and the session-to-session movement is reported beside it rather than baked into it.

## What to do today

- **Do pool by hand if you need the shots**, and know which of A and B you did. If you measured every shot from one overall centre, you measured A.
- **Do not compare a pooled forty shot figure with a single twenty shot figure** unless you know both were computed the same way. This is the commonest way to make a load look better or worse than it is.
- **Do keep the sheets separate in your records.** Two sheets can always be pooled later; a pooled number can never be unpooled.
