# 2026-09-24, entry 170: two freezes, a zero correction that does not say its distance, and hole centers a person had to move

From the same outside user as entry 169, on `Scan_20260923.png`. These are defects, not preferences, so
this entry goes ahead of entry 169 and ahead of the rest of the queue after entry 164.

## 1. The zero correction and its distance

He entered the shot distance as 25.4 yards and reported: "the zero correction was wrong for the distance
I put in. It is giving me click to adjust the zero at 100 yards."

**The planning session checked the arithmetic, and the numbers on screen are right for 25.4 yards.**
0.221 in at 25.4 yd is 0.83 MOA, since one MOA at that distance is 0.266 in; at 100 yd the same offset
would be 0.21 MOA. And 0.83 MOA is 0.241 mil, so two 0.1 mil clicks leaves 0.04 mil, which is exactly
what the screen says. So the calculation used his distance.

That does not make his report wrong. Something told him the correction was for 100 yards, and there are
two real problems behind that:

1. **Nothing in the box says which distance the correction is for.** Say it, in the block entry 169
   section 2 describes: "at 25.4 yd".
2. **A correction measured at 25 yards is not the correction for a 100 yard zero.** Windage transfers
   as an angle; elevation does not, because the bullet crosses the line of sight at a different height at
   each distance, and the difference depends on sight height, velocity and ballistic coefficient. Where
   the rifle profile names a zero distance different from the shot distance:
   - if the profile has sight height, muzzle velocity and ballistic coefficient, carry the correction to
     the zero distance through the ballistics solver and show both, clearly labelled;
   - if it does not, say plainly that this correction is for a zero at 25.4 yd, and name the three values
     needed to carry it to the zero distance.
3. **Find what said 100 yards.** Check "Same setup as the last target", which on Alan's machine copies
   "100 yd", the carried-distance section, and any default, and report which one he saw. If a copied
   setup silently put a distance in a field he then changed, check that the change actually took.
4. A test at 25.4 yd, pinning the 0.83 MOA and the two clicks, so this stays right.

## 2. Freeze: "bulls you fired at", These ones

"Program hung up and nearly crashed after I selected all the bulls associated with the load and clicked
'bulls you fired at - These ones'." He selected the ten bulls he shot, bulls 1 to 10 of 25.

That control feeds the point of impact solve of question 37. The likely cause is the solve doing far
more work than it needs, in the interface thread, once the named set is large. Find it with a profiler
rather than by reading:

1. Reproduce on `Scan_20260923.png` with bulls 1 to 10 named, and time it.
2. Whatever the solve costs, it runs off the interface thread with a progress indication, and the
   window never stops responding.
3. If its cost grows combinatorially with the number of named bulls, that is the bug. Naming the bulls
   should make the problem smaller, per question 37's own reasoning, not larger.
4. A test with all 25 bulls named that finishes within a stated budget.

## 3. Freeze: excluding a shot

"Choosing to exclude a shot causes the program to freeze for about 3 seconds but resumes fine."

`Refresh` recomputes the whole analysis and rebuilds every figure on every edit, per entry 141 section
5.3.6. Since then the analysis has gained permutation tests and a velocity calibration against 2000
simulated strings, and **the likely cost is those simulations rerunning on the interface thread for
every single edit.**

1. Profile the exclude on this scan and report where the three seconds go.
2. The group figures a shooter reads, center, extreme spread, width by height, mean radius and CEP,
   update at once. Anything slow runs in the background and fills in when ready, marked as updating.
3. Simulations whose inputs have not changed are not rerun. A calibration against simulated strings
   depends on the shot count, not on which shot was excluded, and can be cached by its inputs.
4. **A budget, enforced by a test**: no edit on the marking or analysis screens blocks the interface
   thread for more than 100 ms on the reference machine in `docs/PERFORMANCE.md`.

## 4. The hole centers were close, and not close enough

"most importantly the detection of the holes was...ok. I had to manually adjust the holes ever so
slightly for nearly all 10 holes and that change my calculated extreme spread by .11 MOA after I fixed
it."

At 25.4 yards, 0.11 MOA is **0.029 in**. Extreme spread depends on just two shots, so it is the figure
most sensitive to a center error, and a shooter comparing loads by extreme spread will be misled by
errors of this size. He is right that this is the most important point in his review.

1. **Get his corrected marks.** The planning session is asking Alan for his session export and
   diagnostics report; they will arrive as an entry. Until then, measure on the scan itself.
2. **Look at the direction of the corrections, not only their size.** A flatbed scanner lights from one
   side, so every hole carries a shadow on the same edge, and question 38 already showed that shadow is
   what drives measured hole size. If the detector's center is pulled toward the shadow, every center
   moves the same way. That shifts the group center and the zero correction without changing the group
   size much. Errors in random directions change the size. Report which it is.
3. **Estimate the center from the hole's edge, not its dark area.** Fit a circle to the torn edge, weighted
   against the shadowed side, and compare the fitted centers with the current ones on this scan and on
   the six earlier scans. Report the per-hole difference in inches and the resulting change in extreme
   spread, mean radius and group center.
4. **Say how good a person's click is, too.** A person placing a center by eye at normal zoom is itself
   uncertain by a few thousandths of an inch, so his corrections are evidence, not ground truth. Measure
   the repeatability: mark the same scan twice by hand, report the spread, and hold the detector to that
   standard rather than to an impossible zero.
5. **Hold it with a test**: on the fixture scans, the detected centers agree with the edge-fitted
   centers within a stated tolerance, and a change that worsens that agreement fails.

This work also feeds entry 158 program B and entry 161's finding: all of them are about what a hole in
card stock actually looks like to a scanner.
