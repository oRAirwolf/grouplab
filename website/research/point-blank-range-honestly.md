---
title: "Point blank range, honestly"
description: "The classic maximum point blank range keeps the bullet's path inside the animal's vital zone, not your shots. At the top of the band and at its far end, about half the shots miss it. A band built on the chance of a hit instead, how much a field position shortens it, and the case where no single zero is enough."
group: Measuring groups
number: 38
written: 2026-10-19
data_date: "Worked out 2026-10-19 by GroupLab's own ballistic solver, for one load"
samples: "One load (140 gr, 2710 ft/s, velocity SD 10 ft/s, G7 0.326, sight 1.75 in, standard air) against an 8 in circle; no shots were fired for this article"
state: published
no_figure: "The numbers are few and the article gives them as numbers."
found: "with the classic zero, 46 percent of shots land in the 8 inch circle at the path's peak and 42 percent at its far end. Built on the shots instead, a 90 percent band is 0 to 263 yd from a 0.63 MOA mean radius, and 0 to 148 yd from 1.5 MOA."
sure: "these are calculations for one load and a still target, from a mean radius you supply. They have not been tested against shots on game or on a range, and they leave out the wind, a moving animal and a misjudged distance."
sources:
  - "americanhunter.org, 2017, the article on maximum point blank range."
  - "Ron Spomer, understanding maximum point blank range."
  - "randywakeman.com, maximum point blank range, with Chuck Hawks's table."
  - "HuntWise, on bears."
  - "Wikipedia, point-blank range."
  - "IPSC rules, January 2024, appendix B2."
  - "USPSA rules, appendix B1."
  - "GroupLab's solver and its tests for these figures, tests/GroupLab.Core.Tests/Ballistics/PointBlankTests.cs."
---

## What the classic number promises

Maximum point blank range is an old idea. Pick the size of the vital zone, say an 8 inch circle for a deer. Zero the rifle so that the bullet's path rises to the top of the circle and comes back down to the bottom of it. Out to that distance you hold dead on and do not think about drop.

For the load used here (140 grain, 2710 ft/s, G7 0.326, sight 1.75 in), the classic answer is a zero that starts at once, peaks 4.0 in above the line of sight at 148 yd, crosses it again at 266 yd, and ends at 313 yd. At 100 yd the path is 3.4 in high.

Notice what that describes. It is the path of one bullet that is aimed perfectly and flies the same way every time. It says nothing about where your shots land.

## What your shots do

A shot does not land on the path. It lands somewhere around it, by an amount that is your rifle and your position. Put the same classic zero in front of a rifle with a 0.63 MOA mean radius, a good bench figure, and ask how many shots land inside the 8 inch circle:

- 46 percent at the path's peak, 148 yd;
- 42 percent at its far end, 313 yd;
- 98 percent at its far zero, 266 yd.

At the top of the band and at its end, about half the shots land outside the zone the band says is safe. The band is not wrong about the path. It simply was never about the shots.

## The band you can promise

Turn the question around. Choose the share of shots that should land in the zone, which is your certainty, and find the distances over which that holds. The chance at each distance comes from your mean radius, which grows with distance, and from the load's velocity spread carried through the solver, aimed at the center of the zone.

For the same load and the same 0.63 MOA mean radius:

| Certainty | Dead on | Height at 100 yd |
|---|---|---|
| 90 percent | 0 to 263 yd | 2.6 in high |
| 95 percent | 0 to 251 yd | |
| 99 percent | 0 to 228 yd | |
| 99.9 percent | 0 to 201 yd | |

Asking for more certainty costs distance, and it costs it fast. A rifle that is merely zeroed at 100 yd, a common choice, holds 90 percent only to 184 yd, where the classic rule would have said 207.

GroupLab will not take 100 percent. No shot can be promised.

## A field position is not the bench

The mean radius is the part you bring. Everything above used 0.63 MOA. The same load, the same zone and a 90 percent certainty give:

| Mean radius | Dead on |
|---|---|
| 0.5 MOA | 0 to 275 yd |
| 0.63 MOA | 0 to 263 yd |
| 1.0 MOA | 0 to 221 yd |
| 1.5 MOA | 0 to 148 yd |
| 2.0 MOA | 0 to 111 yd |

A group from the bench is much tighter than one from sticks or a bipod. If the animal will be shot from sticks, use a group shot from sticks. With a 1.5 MOA mean radius the classic zero puts 41 percent of shots in the circle at its peak and 27 percent at its far end.

## When you know how far

Sometimes the distance is not a mystery: "this is where I will shoot it from." If the likeliest shot is 200 yd, zero there. The group sits on the center of the zone at 200 yd, 99.9 percent of shots land in the circle at that distance, the zero is 1.8 in high at 100 yd, and the 90 percent band runs from 0 to 237 yd.

## When you do not

A window of distances is harder. Take shots anywhere from 150 to 300 yd. The best single zero for that window keeps only 59 percent of shots in the circle at its worst point, 300 yd, so no single zero keeps 9 in 10 across it. Narrow it to 150 to 250 yd and the best zero keeps 95.5 percent at its worst point, which is enough. The honest answer to the wide window is sometimes that one zero will not do, and that you hold over instead.

## What it leaves out

- **The wind.** It is shown beside the band, not inside it, and the view warns when the drift at the far end is more than half the width of the zone.
- **A moving animal.**
- **A misjudged distance.** Inside the band the path is flat enough that an error does not matter much. Outside it, it does.

It also does not say whether an animal is hit where you think it is, or whether a bullet still has the energy to do its job at the end of the band. The view lets you set an energy floor and a least expansion velocity from the bullet's maker, and names whichever comes first. Those are rules of thumb and a maker's figure, not guarantees.

For hunting, check your local law. Some places set a minimum for the cartridge or the bullet, and GroupLab does not know which.

## What this means

If you have been told that a rifle is good "out to 300 yd" because the path stays in the vital zone, ask what your own group does there. Shoot a group from the position you will hunt from, give GroupLab its mean radius, and read the dead-on distance for the certainty you want. Expect it to be shorter than the number on the box, and shorter again from sticks than from the bench. If the distances you may have to shoot are spread widely, expect to hold over for the far ones.

## Where the zone sizes come from

Game sizes are the heart and lung zone, taking the smaller published figure where sources disagree: americanhunter.org (2017), Ron Spomer, Chuck Hawks's table on randywakeman.com, HuntWise for bears, and Wikipedia for the prairie dog. Five animals have no published size we could find, and are labeled as estimates you can edit. The IPSC and USPSA zones are from the rules listed under sources. The steel silhouettes are a vendor's shapes.
