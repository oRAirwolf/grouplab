# 2026-09-24, entry 172: ground truth for the 2026-09-20 ST-4 target, the first real material for entry 158 program A

Alan sent the shot counts for every group on a commercial 100 yard precision rifle target (National
Target Company ST-4) that he shot on 2026-09-20, marked up on a full sheet photograph. This is the
ground truth entry 158 section 2 step 1 was waiting for, and it is better material than was asked for:
one physical target, photographed seven ways, on a printed one inch grid.

It is Alan's own target, so his standing consent of entry 171 section 6 covers it: it may be published,
stripped of metadata. Never read, print or log GPS, location or timestamp metadata from any of it.

## 1. The files

All on Alan's machine in `C:\Dev\grouplab-range-2026-09-20\photos\`:

| file | what it shows |
|---|---|
| `20260920_185950.jpg` | top left quadrant, close |
| `20260920_185953.jpg` | bottom left quadrant, close |
| `20260920_185956.jpg` | bottom right quadrant, close |
| `20260920_185958.jpg` | top right quadrant, close |
| `20260920_190001.jpg` | the center diamond, close |
| `20260920_190005.jpg` | the whole sheet, near square on |
| `20260920_190009.jpg` | the whole sheet, strongly oblique from the right |
| `ST-4-2026-09-20-annotated.png` | Alan's markup of the whole sheet, with the count beside every group |

`20260920_185944.jpg` and `185946.jpg` are two more frames from the same burst; check whether they show
the same sheet and include them if so.

**Scale: every red grid line on this target is exactly one inch apart**, per Alan, across the whole
sheet. The grid runs 6 inches either side of center in both directions.

## 2. The ground truth

**20 groups, 115 shots: seventeen 5 shot groups and three 10 shot groups.** Positions are read off
Alan's annotated photograph in grid inches from the center of the middle diamond, x to the right and y
up. They are approximate, within about half an inch, because that photograph has perspective in it;
they identify the groups, they are not measurements.

| group | region | position (x, y) in | shots |
|---|---|---|---|
| 1 | top left, above the top tab | (-5.2, 7.1) | 5 |
| 2 | top left, upper right | (-4.0, 5.6) | 5 |
| 3 | top left, at the bottom tab | (-5.3, 3.9) | 5 |
| 4 | top right, above the top tab | (4.8, 6.8) | 5 |
| 5 | top right, left | (3.2, 5.4) | 5 |
| 6 | top right, right | (6.0, 5.2) | 5 |
| 7 | top right, at the bottom tab | (4.8, 3.8) | 5 |
| 8 | center, top point of the large diamond | (-0.3, 3.1) | 10 |
| 9 | center, left point of the large diamond | (-3.2, 0.4) | 10 |
| 10 | center, in the middle square | (-0.3, 0.2) | 10 |
| 11 | center, right point of the large diamond | (2.1, 0.3) | 5 |
| 12 | center, bottom point of the large diamond | (-0.5, -2.5) | 5 |
| 13 | bottom left, above the top tab | (-5.2, -2.8) | 5 |
| 14 | bottom left, left, at the sheet edge | (-6.5, -4.1) | 5 |
| 15 | bottom left, right | (-3.7, -4.2) | 5 |
| 16 | bottom left, at the bottom tab | (-5.1, -5.3) | 5 |
| 17 | bottom right, above the top tab | (4.1, -3.1) | 5 |
| 18 | bottom right, right | (5.2, -4.3) | 5 |
| 19 | bottom right, left | (2.6, -4.4) | 5 |
| 20 | bottom right, at the bottom tab | (4.0, -5.8) | 5 |

Write this into a ground truth file beside the other fixtures' ground truth, and keep Alan's annotated
image as the evidence it came from.

**Alan's answers, 2026-09-24:**

- **Cartridge:** every shot was 6.5 Creedmoor.
- **Distance:** 100 yards.
- **Point of aim:** each group was aimed at the orange tab or diamond point it sits beside.
- **Primers:** 15 shots used Remington 7 1/2 BR primers and 100 used CCI BR-4. **Alan did not record
  which shots were which.** 15 plus 100 is 115, which matches the count.

What that means for the analysis:

1. **Zero offsets can now be computed**, one per group, from each group's center to its own aiming mark.
2. **Treat the sheet as one load with a known, unlocated mix of two primers**, and say so wherever it is
   pooled. **Do not try to work out which groups used the Remington primers.** Picking out the
   groups that look different and calling them the other primer would be a guess dressed up as a
   finding, and a circular one: it would use the very dispersion it then claimed to explain.
3. It is fair to test whether the 20 groups are consistent with one common dispersion, with the
   heterogeneity test already in `docs/STATISTICS.md`. If one or more groups stand out, report it as that
   and nothing more; the primer mix is one possible reason among several, and the data cannot say which.
4. **A clean check falls out of this sheet.** All twenty groups were fired with one zero at twenty
   different aiming marks. So each group's offset from its mark is an estimate of the same zero error,
   and the scatter of those twenty offsets should be close to sigma over the square root of the shot
   count: sigma over root 5 for the 5 shot groups, and over root 10 for the 10 shot ones. Measure it. If
   the scatter matches, it is direct evidence of how well a 5 shot group locates a zero, which is the
   heart of entry 158 section 2 step 3's article. If it is much larger, either the zero wandered during
   the session or GroupLab's group centers are noisier than the statistics say, and entry 170 section 4
   is where to look.

### 2.1 Which photograph shows which group

The planning session matched the groups to the close ups by region. Every group appears in its close up
and in both whole sheet photographs.

| groups | close up | also in |
|---|---|---|
| 1 to 3, top left | `20260920_185950.jpg` | `190005`, `190009` |
| 4 to 7, top right | `20260920_185958.jpg` | `190005`, `190009` |
| 8 to 12, center | `20260920_190001.jpg` | `190005`, `190009` |
| 13 to 16, bottom left | `20260920_185953.jpg` | `190005`, `190009` |
| 17 to 20, bottom right | `20260920_185956.jpg` | `190005`, `190009` |

Some groups sit near a close up's edge, group 14 at the sheet edge and group 1 above the top of the grid
in particular. Confirm each is wholly inside its close up before using that frame for it, and use the
whole sheet frames for any group that is cut off.

### 2.2 This is the only annotated material, and it replaces the earlier plan

Alan, 2026-09-24: "The annotations I gave you for the commercial target were the only annotations I was
planning on giving you." So:

1. **Request 3 in `docs/notes/for-alan.md` is answered by this entry. Close it.**
2. **Entry 158 section 2 step 1 is replaced**: program A runs on this sheet, not on the twenty six
   photographs in `C:\Dev\grouplab-testdata\owner`.
3. Those twenty six photographs stay available, but **without ground truth**. Do not infer a shot count
   for any of them and then measure detection against the inference. Use them only where no count is
   needed, for example the camera and perspective work of entry 157, or say that a result is unverified.

## 3. What this material makes possible

1. **Detection on overlapping holes, entry 158 section 2 step 2.** Seventeen 5 shot and three 10 shot
   groups, many with touching or merged holes, against a known count. Report misses and false holes per
   group, per photograph, in the form question 35 used for the scans.
2. **The same group, measured seven ways.** Every group appears in at least two photographs: a close up
   and one or two full sheet views, one of them strongly oblique. Measure each group's extreme spread,
   mean radius and center in each photograph and report how much they disagree. **That is the first
   direct measurement of how much the photograph itself changes the answer**: distance, angle and lens.
   It is exactly what entry 157's refusal threshold for off axis photographs needs.
3. **A non GroupLab target, measured.** This is the path entry 152 established: any target, once the
   scale is set. The grid makes setting it trivial, and this is the first real sheet to go through it.
   Report every step where a shooter has to do something by hand that the application could have done.
4. **A calibration grid for free.** A 13 by 13 array of one inch intersections is dense ground truth for
   perspective and lens distortion. Use it to test entry 157 section 4's corner detection, perspective
   correction and distortion correction, since every intersection's true position is known.
5. **What 5 shots tell you, entry 158 section 2 step 3**, using section 2 item 4 above as its central evidence.

## 4. Where this sits

After entry 170 and before entry 169 in the queue, because items 1 and 2 of section 3 measure the
detection accuracy entry 170 section 4 is about, and they should be read together.
