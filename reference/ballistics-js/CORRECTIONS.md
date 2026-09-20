# What `ballistics.corrected.js` changes

`ballistics.corrected.js` is `ballistics.js`, the file pissinhot.com serves today, with five faults put right and **nothing else changed**: the same names, the same call signature and the same returned fields, so the pages that use it keep working untouched.

**It is a file, not a change to anything.** GroupLab does not upload it, and nothing in GroupLab loads it. Alan decides what to do with it.

Each change is marked `CORRECTED` in the file.

## The five changes

1. **The G1 drag table.** The table in the file is not the standard G1 function above Mach 0.85: it gives 0.5210 at Mach 1.0 where the standard is 0.4805, and about half the standard value at Mach 5. Loads given with a G1 BC therefore flew with too little drag: the .308 168 gr case read 31.77 MOA of drop at 1000 yd where it should read 40.33, which is over 8 MOA, or about 90 inches. **The corrected file carries the standard G1 table**, the values two independent transcriptions agree on at all 79 points. G7 is untouched: the file's G7 table agrees with the standard where these loads fly.

2. **The shooting angle.** Drop was measured from the horizontal line through the sight rather than from the line of sight itself, so any angle other than zero reported the line of sight's own rise, about 300 MOA per 5 degrees, as if it were bullet drop. **The corrected file zeroes the rifle on the flat, as a rifle is zeroed at a range, then measures range along the line of sight and drop perpendicular to it.** At zero degrees nothing changes; uphill and downhill shots now read as they should.

3. **Aerodynamic jump.** `aeroJump` returned crosswind times 0.012 divided by the stability factor, in MOA. That is not a published formula, and it runs the wrong way with stability: a more stable bullet should jump less, and it gave more. **The corrected file returns zero and leaves the fields in place**, so anything reading `aeroJumpMOA` still works and no invented number reaches a shooter. GroupLab says "Aerodynamic jump is not modelled" wherever its own solver's output is shown.

4. **The Coriolis vertical term's sign.** Firing east makes a bullet strike high and firing west low, which is the Eötvös effect. The file returned the opposite. **The corrected file carries the term with its sign right.**

5. **The wind's direction in `solveExtended`.** `windDirDeg` is the direction the wind blows *from*, so a wind from the right pushes the bullet to the left. The file added it as drift to the right. **The corrected file drifts the bullet away from the wind.** The simpler `solve`, which takes a full-value crosswind, was already right and is unchanged.

## How it was checked

`cases.corrected.js` runs the corrected file under Node on the six cases in `../ballistics-cases.json`, and `CorrectedJavaScriptTests` compares every row with GroupLab's own solver, which is validated against py-ballisticcalc, an independent implementation. The tolerances are the ones in `docs/BALLISTICS-VALIDATION.md` section 2, written down before any comparison was run, plus the rounding the JavaScript applies to its printed rows.

**The check runs on CI, where Node is installed.** The machine this was written on has no Node, so it was not run here.

## What was deliberately left alone

- The G7 table, the atmosphere, the integrator, the zeroing search, the lag-rule wind drift, Miller's stability factor, Litz's spin drift and the horizontal Coriolis term: all as they were.
- The dispersion helpers `velocityDispersion`, `hitProbability` and `combinedGroupSize`.
- Every name the pages call, and every field they read.
