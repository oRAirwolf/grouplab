# What `ballistics.corrected.js` changes

`ballistics.corrected.js` is `ballistics.js`, the file pissinhot.com serves today, with six faults put right and **nothing else changed**: the same names, the same call signature and the same returned fields, so the pages that use it keep working untouched.

**It is a file, not a change to anything.** GroupLab does not upload it, and nothing in GroupLab loads it. Alan decides what to do with it.

Each change is marked `CORRECTED` in the file.

## The six changes

1. **The G1 drag table.** The table in the file is not the standard G1 function above Mach 0.85: it gives 0.5210 at Mach 1.0 where the standard is 0.4805, and about half the standard value at Mach 5. Loads given with a G1 BC therefore flew with too little drag: the .308 168 gr case read 31.77 MOA of drop at 1000 yd where it should read 40.33, which is over 8 MOA, or about 90 inches. **The corrected file carries the standard G1 table**, the values two independent transcriptions agree on at all 79 points. G7 is untouched: the file's G7 table agrees with the standard where these loads fly.

2. **The shooting angle.** Drop was measured from the horizontal line through the sight rather than from the line of sight itself, so any angle other than zero reported the line of sight's own rise, about 300 MOA per 5 degrees, as if it were bullet drop. **The corrected file zeroes the rifle on the flat, as a rifle is zeroed at a range, then measures range along the line of sight and drop perpendicular to it.** At zero degrees nothing changes; uphill and downhill shots now read as they should.

3. **Aerodynamic jump.** `aeroJump` returned crosswind times 0.012 divided by the stability factor, in MOA. That is not a published formula, and it runs the wrong way with stability: a more stable bullet should jump less, and it gave more. **The corrected file returns zero and leaves the fields in place**, so anything reading `aeroJumpMOA` still works and no invented number reaches a shooter. GroupLab says "Aerodynamic jump is not modelled" wherever its own solver's output is shown.

4. **The Coriolis vertical term's sign.** Firing east makes a bullet strike high and firing west low, which is the Eötvös effect. The file returned the opposite. **The corrected file carries the term with its sign right.**

5. **The wind's direction in `solveExtended`.** `windDirDeg` is the direction the wind blows *from*, so a wind from the right pushes the bullet to the left. The file added it as drift to the right. **The corrected file drifts the bullet away from the wind.** The simpler `solve`, which takes a full-value crosswind, was already right and is unchanged.

6. **Every row was the bullet a fraction of a step further on than the range it was labelled with.** The loop recorded at the first integration step whose range had passed the next range in the table, and then printed that state under the range it had passed. **The corrected file interpolates each row to the range it is labelled with.** The sixth fault is the one the check found rather than the reading: at 100 yards it overstated the wind drift by about a tenth, 0.99 MOA against 0.91 on the .223 case, and the time of flight by about half a percent; at 1000 yards the same overshoot is a small share of a long flight and disappears into the rounding, which is why reading the file did not show it. The time of flight is now printed to four places rather than three, because three cannot hold a tenth-second flight to half a percent.

## How it was checked

`cases.corrected.js` runs the corrected file under Node on the six cases in `../ballistics-cases.json`, and `CorrectedJavaScriptTests` compares every row with GroupLab's own solver, which is validated against py-ballisticcalc, an independent implementation. The tolerances are the ones in `docs/BALLISTICS-VALIDATION.md` section 2, written down before any comparison was run, plus the rounding the JavaScript applies to its printed rows.

**The check runs on CI, where Node is installed.** The machine this was written on has no Node. The sixth fault above was found by that check on its first run and confirmed here without Node: GroupLab's own solver keeps a JavaScript-compatible mode that reproduces the original's sampling, and it gives the JavaScript's figures to the digit, 1.040 inches of drift at 100 yards against the 0.956 its exact sampling gives.

## What was deliberately left alone

- The G7 table, the atmosphere, the integrator, the zeroing search, the lag-rule wind drift, Miller's stability factor, Litz's spin drift and the horizontal Coriolis term: all as they were.
- The dispersion helpers `velocityDispersion`, `hitProbability` and `combinedGroupSize`.
- Every name the pages call, and every field they read.
