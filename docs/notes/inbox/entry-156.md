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

## 7. What the Blackburn Defense calculator actually does (added 2026-09-24)

The planning session opened the page and ran it with its defaults. Record this as the reference, and
still build from section 2's model rather than from their layout or wording.

**Inputs, with the page's defaults:**

| input | default |
|---|---|
| target | 20 in circle |
| iterations | 1000 |
| group size | average five shot group 1 MOA, standard deviation 0.5 MOA |
| muzzle velocity | 2800 fps measured, 2800 actual, standard deviation 10 fps |
| BC | 0.3 measured, 0.3 actual, standard deviation 0.003 |
| range | 1000 yd measured, 1000 actual, standard deviation 3 yd |
| wind speed | 10 mph measured, 10 actual, standard deviation 2 mph |
| wind direction | 90 degrees measured, 90 actual, standard deviation 5 degrees |

**Outputs:** a scatter of simulated impacts in two colours, and two numbers. With the defaults above:
first round hit probability 40.8 percent, second round hit probability 75.0 percent. The second round
cloud is much tighter, because the second shot is corrected from where the first one landed.

**Two ideas worth taking, both of which fit section 2's model:**

1. **Measured, actual and uncertainty are three separate things.** For every environmental input the
   page distinguishes what the shooter believes (measured), what is really true (actual), and how
   uncertain the belief is (standard deviation). Setting measured and actual apart models a **bias**,
   such as a chronograph reading 20 fps fast, which a standard deviation alone cannot. Offer the actual
   value behind the advanced disclosure, defaulting to the measured one.
2. **A second round probability, after correcting from the first impact.** This is the number that
   matters in real engagements and on most match stages. Model it honestly: after the first shot, the
   shooter corrects by that shot's observed miss, which removes most of the per string errors of section
   2, the wind call, the range error and the zero error. **But the correction also contains the first
   shot's own random dispersion**, because the shooter cannot tell which part of a miss was the wind and
   which part was the rifle. So the second shot carries its own dispersion plus the first shot's, and a
   model that ignores that will overstate the second round number. Show first round and second round
   probability side by side, and say in the assumptions that the second assumes the first impact was
   seen.

**Where GroupLab should do better than the reference:**

- The reference asks for an average five shot group in MOA. GroupLab has the shooter's measured
  dispersion with its confidence interval, which is a better input than a remembered group size, and
  the extreme spread of five shots is a poor estimator of dispersion in the first place.
- The reference shows a single percentage with no interval. Section 3 item 1 still stands: show the
  interval, including the uncertainty in the shooter's own measured dispersion.
- 1000 iterations gives a Monte Carlo standard error of about 1.6 points on a probability near 50
  percent, far coarser than the tenth of a percent the page prints. Keep section 1's default of 10000,
  and never print more precision than the trial count supports.

## 8. What Applied Ballistics Quantum's WEZ screen does (added 2026-09-24)

Alan sent eight screenshots of the WEZ calculator in the Applied Ballistics Quantum app, with his own
6.5 Creedmoor profile loaded, and pointed at https://appliedballisticsllc.com/weapon-employment-zone-wez/
for the background. This is the tool he uses, so it is the stronger of the two references. Same rule as
section 6: learn from it, copy none of its layout, labels, numbers or code.

**What it shows.** Hit probability sits in a strip at the top of the firing solution, beside energy,
elevation and two wind holds. So a shooter reads the probability with the dope, not on a separate page.
Below that, the WEZ view draws the simulated impacts over the target.

**Its inputs:**

- range, target type, and target width and height, 12 by 12 in at 1000 yd in his screenshots
- target types: IPSC, rectangle, circle, and animal outlines (deer, coyote, elk, prairie dog)
- graph type: shot simulation, vertical uncertainty, horizontal uncertainty, and probability of hit
- **uncertainties**, each as one number: range, muzzle velocity, wind speed, drag (as a percentage of
  the drag model), rifle precision (in mrad), temperature, pressure, humidity, azimuth, inclination and
  latitude

**Confidence presets.** One control, Low, Medium, High or Custom, sets every uncertainty at once. With
his 12 inch target at 1000 yd, the presets gave 4, 25 and 84 percent, and his own custom values gave 51
percent. That spread is the whole lesson of the screen: the uncertainties decide the answer far more than
the rifle does.

**What to take into GroupLab:**

1. **Show the probability with the dope.** Put the hit probability on the Ballistics screen's solution,
   beside the elevation and wind holds, as well as on its own view.
2. **Confidence presets that set every uncertainty at once**, with Custom for anything edited. Define
   GroupLab's own presets, each described in a sentence by the situation it represents, such as "known
   distance, measured wind" against "lasered distance, estimated wind", and document where each number
   comes from. Do not reuse the Quantum preset values.
3. **Widen section 1's inputs** to include the uncertainties Quantum carries that section 1 does not:
   drag model uncertainty as a percentage, and temperature, pressure, humidity, azimuth, inclination and
   latitude. All of them go behind the advanced disclosure; the presets fill them.
4. **Target shapes**: circle, rectangle and IPSC first. A target from GroupLab's own library, by its bull
   size, is a natural fourth. Animal vital zones are a later decision, not part of this entry.
5. **Vertical and horizontal uncertainty views.** Section 3 item 5's sensitivity list is the same idea
   done better, since it names which input costs the most. Offer the vertical and horizontal split as
   well, because it is how Quantum users already think.

**Where GroupLab is ahead, and this is the reason to build it at all:** Quantum asks the shooter to type
"rifle precision" and a velocity uncertainty. GroupLab has measured both. Pre-populate rifle precision
from the selected load's measured dispersion and velocity uncertainty from its chronograph data, each with
its confidence interval, and say on screen where each came from. **State the definition exactly**:
"rifle precision" here must be the per axis standard deviation in mrad, which for circular dispersion is
the Rayleigh parameter. Mixing up a per axis figure with a radial one changes the answer considerably, so
write the conversion down, test it, and never let two numbers with different definitions share a field.
