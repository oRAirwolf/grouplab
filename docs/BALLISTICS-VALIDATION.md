# Ballistic solver validation

How GroupLab's ballistic solver, `src/GroupLab.Core/Ballistics`, is checked, and what each check must meet. `docs/NOTES-FROM-PLANNING.md` entry 110 section 2f asks for two checks. It also asks for **the tolerance to be written down before the comparison is run**, so this file was committed and pushed before the reference tables existed. A tolerance chosen after seeing the numbers is not a gate.

The cases are in `reference/ballistics-cases.json`: six flat-fire loads across G1 and G7, from .223 to .300 Win Mag. Each is zeroed at 100 or 200 yd, in atmospheres from 20 to 90 °F, 26.0 to 30.4 inHg and 0 to 60 percent humidity, with crosswinds of 5 to 15 mph. Each is compared every 100 yd from 100 to 1000 yd.

## 1. Against ballistics.js: the transcription

**What it proves.** That the port does what the JavaScript does. It says nothing about whether either is right, because the two share an author, a method and tables.

**How.** The JavaScript, unchanged at `reference/ballistics-js/ballistics.js`, is run under Node on GitHub's runners by `reference/ballistics-js/cases.js`, and its output is committed as `tests/GroupLab.Core.Tests/Fixtures/ballistics-js.json`. The CI job for Linux runs it again and fails if the output differs from the committed file. The port is run in its JavaScript-compatible mode for this check, which reproduces the three places where it deliberately differs on a flat trajectory:
- it records the first integration step at or past each range;
- it finds the zero with first-order steps;
- it does not interpolate.

So the comparison is made at the JavaScript's own sampled positions.

**Tolerance: the JavaScript's own rounding.**

| Quantity | Rounded to | So allowed |
|---|---|---|
| Velocity | 0.1 fps | 0.1 fps |
| Drop, drop in MOA and mil, wind drift, wind in MOA and mil | 0.01 | 0.01 |
| Time of flight | 0.001 s | 0.001 s |
| Mach | 0.001 | 0.001 |
| Energy | 1 ft-lb | 1 ft-lb |

The allowance is one rounding unit, not half of one, because a value lying on a rounding boundary can round either way under two different floating-point libraries. The helper functions are compared unrounded, to 1 part in 10^9: the drag tables and their interpolation, the atmosphere, Miller's stability factor at standard pressure, Litz's spin drift, and the Coriolis horizontal term.

## 2. Against py-ballisticcalc: the gate

**What it proves.** That the solver's physics agrees with an independent point-mass implementation, which is the Phase 5 gate: "a ballistic solver validated against an independent implementation".

**The reference.** py-ballisticcalc 2.3.1, the RK4 engine, LGPL-3.0-only. It is used only by `reference/py-ballisticcalc/generate.py`, which runs on a GitHub runner and writes `reference/py-ballisticcalc/tables.json`. The tables are committed; the library is not. It is independent in the ways that matter:
- a different author;
- its own copy of the G1 and G7 tables;
- air density by CIPM-2007 rather than the virtual-temperature correction;
- crosswind integrated as a vector in the equations of motion rather than by the lag rule.

**What is compared.** At each 100 yd, drop and wind deflection in MOA and time of flight. Each quantity is converted to MOA the same way on both sides, from the height or windage in inches and the range in inches, so no difference in angular convention can enter.

**Tolerances, and why each figure.**

| Quantity | Allowed difference | Why |
|---|---|---|
| Drop | 0.10 MOA plus 1 percent of the reference drop | The absolute part covers the atmosphere. The two density formulas differ by up to about 0.2 percent at these conditions, and the humidity correction to the speed of sound by up to about 0.15 percent. Each moves drop at 1000 yd by a few hundredths of a MOA. The proportional part covers drag. The two sides tabulate and interpolate the standard drag functions separately. A difference of about half a percent in Cd, sustained over the flight, moves drop by roughly twice that share, about 1 percent, and more through the transonic region. |
| Wind deflection | 0.05 MOA plus 2 percent of the reference deflection | The lag rule is exact for a constant crosswind only to first order in the wind speed over the bullet's speed. At 5 to 15 mph against a bullet above 1200 fps, the neglected terms come to about 1 percent. The drag difference above adds up to about another 1 percent through the time of flight. |
| Time of flight | 0.5 percent | Time of flight depends on drag roughly half as strongly as drop does, and not on gravity or the zero. |

**These apply at every range from 100 to 1000 yd in every case, supersonic or not.** If a case fails, it is reported as failing, with its numbers. The tolerance is not widened after the fact.

## 3. What the port deliberately does differently from ballistics.js

`docs/PHASE1-RESULTS.md` "Entry 110" lists each difference with the case that shows it.
