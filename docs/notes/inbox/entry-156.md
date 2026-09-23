# 2026-09-23, entry 156: hit probability on the Ballistics screen, from the shooter's own dispersion

Alan: "The ballistics page should also do a hit probability calculation based on the group statistics.
Use https://www.blackburndefense.com/tools/hit-probability as a guide on how to make it. As far as I am
aware, this is basically a monte carlo simulation. Applied Ballistics Quantum and their desktop
software also offer this functionality."

This is the feature where GroupLab has an advantage over every tool that already does it. The others
ask the shooter to type in a precision figure, usually in MOA, usually remembered from a good day.
GroupLab has the shooter's measured dispersion with its confidence interval sitting in the same
application. So the answer can carry an honest uncertainty, and that is the thing to build the feature
around rather than a prettier scatter plot.

Do this after entries 149 to 155. It is the largest piece here and it is the one that benefits most
from Alan's screenshots, which entry 149 section 5 asks him for.

## 1. Inputs

Pre-populate everything GroupLab already knows. Nothing the application can measure should be typed in.

| input | where it comes from |
|---|---|
| dispersion | Rayleigh sigma of the selected session, load or pooled group, with its chi squared interval and its shot count |
| distance | typed, defaulting to the distance the group was shot at |
| target size | typed: a circle of diameter D, or a rectangle W by H, in inches, centimetres, MOA or mil |
| muzzle velocity and velocity SD | the chronograph data where the load has it, typed otherwise |
| ballistic coefficient, bullet weight, atmosphere | the Ballistics screen already holds these |
| wind speed and direction | typed |
| wind call uncertainty | typed, with a stated default and a sentence saying what it means. At distance this usually dominates everything else |
| range estimation error | typed, defaulting to zero for a known range |
| zero error | typed, defaulting to the uncertainty in the measured centre from aim, which GroupLab knows |
| shots in the string | typed, default 1 |
| trials | default 10000, and a seed, so a result is repeatable and a screenshot can be reproduced |

Hide the last four behind an "advanced" disclosure with sensible defaults, so the screen is usable
before a shooter knows what a wind call uncertainty is. Every one of them has a tooltip from entry 154.

## 2. The model

Each simulated shot is the sum of independent contributions, drawn per trial:

1. **Dispersion**, from the bivariate normal with the measured sigma, drawn **per shot**.
2. **Vertical from velocity**, the velocity SD carried through the drop curve at that distance, drawn
   per shot.
3. **Horizontal from the wind call**, the wind call error carried through the wind deflection, drawn
   **per string** rather than per shot, because a shooter reads the wind once and fires; a per shot
   draw makes wind look like dispersion and flatters the result.
4. **Range estimation error**, through the drop curve, drawn per string for the same reason.
5. **Zero error**, a fixed offset drawn once per string. A zero error does not resample between shots.

The distinction between per shot and per string is the part most tools get wrong, and getting it right
is a defensible reason for this to exist. Write it down where the model lives, not only here.

Also draw sigma itself from its own sampling distribution across trials, so the output interval
includes the uncertainty in the shooter's own precision estimate. A hit probability computed from nine
shots is not as knowable as one computed from ninety, and the screen should show that.

## 3. Outputs

1. Probability of a hit with one shot, **with an interval**, and the interval reflects both the Monte
   Carlo count and the uncertainty in sigma. Show which of the two dominates.
2. Probability of at least one hit in N shots, and the expected number of hits in N.
3. The impact scatter with the target drawn on it.
4. Hit probability against distance, as a curve, with the interval as a band.
5. **A sensitivity list**: which input is costing the most probability. This is the output a shooter can
   act on. It answers whether to practise wind calls, work on the load, or buy a rangefinder, and no
   other output on the screen answers a question the shooter can do anything about.

## 4. Honesty

1. Never show a point estimate without its interval.
2. State the assumptions in one short paragraph on the screen: the dispersion is taken to be the same
   from shot to shot, the error sources are taken to be independent, and the target is taken to be
   engaged from a stable position like the one the group was shot from.
3. Do not report more than two significant figures. A hit probability of 73.42 percent is a claim the
   model cannot support.
4. Refuse, with a plain sentence, when the group behind the sigma is too small to say anything. Say how
   many shots would be needed for the answer to mean something, using the sample size work already in
   `docs/STATISTICS.md`.

## 5. Validation

There is an exact answer for the simplest case, so the simulation must reproduce it. For a circular
target of radius R centred on the point of aim, with only bivariate normal dispersion of parameter
sigma and no other error:

    P(hit) = 1 - exp( -R^2 / (2 * sigma^2) )

1. A test that the Monte Carlo matches that expression to within its own Monte Carlo error across a
   range of R over sigma.
2. A test that the seed makes a run repeatable.
3. A test that adding an error source never increases the hit probability.
4. A test that a per string error source and a per shot error source of the same size give different
   answers for a multi shot string, which is what stops item 2 of section 2 being quietly undone later.

## 6. On the other tools

Blackburn Defense, Applied Ballistics Quantum and the Applied Ballistics desktop software are the
reference for **what a shooter expects to see and which inputs are worth showing first**. Alan will
send screenshots. Use them to decide the layout and the defaults.

Do not copy their wording, their layout, their labels or any code. Build the model from section 2.
Where GroupLab's answer differs from theirs for the same inputs, that is worth investigating and
possibly worth an article under entry 158, but it is not a reason to change the model to agree.
