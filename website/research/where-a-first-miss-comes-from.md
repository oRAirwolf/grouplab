---
title: "Where a first-round miss comes from"
description: "At 300 yards a first-round miss is mostly the rifle. At 600 and 1000 it is mostly the wind call, and with a guessed distance the range estimate too, while the velocity spread costs under one hit in a hundred and the air about one at most. What that says about where to spend practice, and when to correct off a single shot."
group: Measuring groups
number: 45
written: 2026-10-21
data_date: "Worked out 2026-10-21 by GroupLab's own hit probability, 10,000 simulated shots a case, its fixed seed"
samples: "One load (140 gr, 2710 ft/s, velocity SD 10 ft/s, G7 0.326, sight 1.75 in, zeroed at 100 yd, standard air), one rifle of 0.63 MOA mean radius measured at 100 yd, a 10 in circle at 300, 600 and 1000 yd under three confidence presets; 10,000 simulated shots a case and no shots fired"
state: published
no_figure: "The numbers are few and the article gives them as tables."
found: "with the wind read from flags at a known distance, the first round hits a 10 in circle 99 percent of the time at 300 yd, 57 at 600 and 16 at 1000. With the distance lasered and the wind estimated, 96, 38 and 8. The wind call costs the most hits at 600 and 1000 yd in every case but one, and the velocity spread never costs as much as one in a hundred."
sure: "these are simulations for one load and one rifle, with the zero taken as perfect and no Earth's rotation. The uncertainty of each reading comes from GroupLab's three confidence presets, which are its own judgment written down, not a measurement; a shooter who reads wind better or worse than a preset moves every figure."
sources:
  - "The hit probability, its error sources and the second round: GroupLab statistics reference, docs/STATISTICS.md section 12.6."
  - "The three confidence presets and the reasoning for each figure: docs/STATISTICS.md section 12.6 and src/GroupLab.Core/Ballistics/HitPresets.cs."
  - "Bryan Litz, Accuracy and Precision for Long Range Shooting (Applied Ballistics, 2012), whose weapon employment zone analysis works out hit chances from the same kinds of error. Nothing here is taken from it."
  - "Every figure in this article: tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs, TheMissArticlesFiguresHold."
---

## The question

You fire one shot at a 10 inch steel plate and miss. Was it the rifle, the wind, the distance, the load, or the air? You cannot tell from one shot. But you can ask, before you go, how much each of them costs you on average, and that says where an hour of practice or a hundred dollars of equipment would do the most good.

GroupLab's **Hit probability** view answers that by simulation. Every simulated shot gets the rifle's own spread and the muzzle velocity's spread, drawn fresh for each shot. The wind call, the range estimate, the drag and the air are drawn once for a string of shots, because a shooter reads them once and fires on that reading. Then each source is taken away in turn, on the same random numbers, and the hits that come back are what that source was costing. The screen lists them as **What costs the most**.

## The case

One ordinary long range load, a 140 grain bullet at 2710 ft/s with a velocity SD of 10 ft/s, zeroed at 100 yd. One rifle with a mean radius of 0.63 MOA measured at 100 yd, which averages about 1.5 MOA for five shots. A 10 in circle at 300, 600 and 1000 yd. The zero is taken as perfect.

How well the shooter knows everything else comes from GroupLab's three confidence presets:

- **Known distance, measured air:** a range with marked distances, the air measured at the line, and the wind read from flags and mirage by somebody who has shot there before. One standard deviation of wind error is 1.5 mph.
- **Lasered distance, estimated wind:** a rangefinder, the air from a phone, the wind estimated from what can be seen downrange. Wind error 3 mph.
- **Estimated distance, estimated wind:** no rangefinder, the distance judged by eye, and nothing to read the wind from. Wind error 4 mph, and the distance wrong by 5 percent of itself.

## The first round

The chance of a first-round hit:

| | 300 yd | 600 yd | 1000 yd |
|---|---|---|---|
| The rifle and its velocity alone, everything else known exactly | 99 percent | 71 percent | 33 percent |
| Known distance, measured air | 99 percent | 57 percent | 16 percent |
| Lasered distance, estimated wind | 96 percent | 38 percent | 8 percent |
| Estimated distance, estimated wind | 91 percent | 17 percent | 1.5 percent |

At 300 yards the reading hardly matters: even a guessed distance and a guessed wind hit nine times in ten. At 600 the same rifle goes from 71 percent with perfect knowledge to 17 with guesses. At 1000 a 10 inch plate is a long shot for this rifle whatever the reading.

## What costs the hits

The two sources that cost the most first-round hits, in percentage points:

| | 300 yd | 600 yd | 1000 yd |
|---|---|---|---|
| Known distance, measured air | rifle 1.1, wind 0.5 | rifle 30, wind 14 | wind 16, rifle 11 |
| Lasered distance, estimated wind | rifle 3.5, wind 3.0 | wind 32, rifle 17 | wind 20, rifle 3.0 |
| Estimated distance, estimated wind | rifle 7.5, wind 7.1 | wind 22, range 14 | wind 6.3, range 3.5 |

The velocity spread of 10 ft/s never costs as much as one hit in a hundred, and the drag, the temperature, the pressure, the humidity and the shot angle never cost more than about one each.

Each cost is the hits that come back when that one source is taken away and everything else stays. Costs do not add up to the total, because removing one error still leaves the others to cause misses, and when every source is large, as at 1000 yd with a guessed distance, taking away any one of them brings back few hits. That is why the costs there look small: not because the wind stopped mattering, but because the shot was already hopeless.

## The second round

After a miss you see where it went and dial off the whole of it. That takes away every error that was the same for both shots: the wind call, the distance, the drag, the air. What it cannot take away is the first shot's own share of the rifle's spread, because nobody can tell which part of a miss was the wind and which the rifle, so the correction carries it into the second shot.

The second-round chance is the same under all three presets, because every error a preset sets has been dialed off: 91 percent at 300 yd, 45 at 600 and 18 at 1000.

Set beside the first round, two things follow:

- **At 600 and 1000 yards, a spotted first shot is worth a great deal** when the reading was poor: 17 to 45 percent at 600 with a guessed distance and wind, 1.5 to 18 at 1000.
- **At 300 yards with a good reading, correcting off one shot makes things worse:** 99 percent on the first round, 91 on the second. The first shot's miss was mostly the rifle's own spread, and dialing it off moves the next shot by the same amount the wrong way. The [zeroing article](/research/when-to-adjust-zero/) says the same about a zero: one shot is not a reason to move the sight.

## What this means

- **Past 500 yards, practice reading the wind.** It costs more first-round hits than anything else at 600 and 1000 yd in every case but one, and the one is a known distance with flags at 600, where it is still second.
- **Without a rangefinder, the distance is the next cost.** With the distance guessed at 600 yd it costs 14 points, as much as the wind call costs at a known distance with flags to read.
- **A lower velocity SD will not show in first-round hits here.** At 10 ft/s it costs under one hit in a hundred, even at 1000 yd. The [velocity article](/research/how-much-vertical-is-velocity/) shows where velocity does show: in the vertical of a good rifle's group at long range.
- **At short range, the rifle is the limit.** At 300 yards the rifle's own spread costs the most in every case, so a better rifle, rest or position is what moves the number.
- **Spot your misses at distance,** and correct off one only when the reading, not the rifle, is likely to be what was wrong.

These figures are for one rifle and one load. Put your own into GroupLab's Hit probability view, with your own measured group behind the precision, and it lists what costs you the most.
