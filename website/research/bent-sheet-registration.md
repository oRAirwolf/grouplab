---
title: "Registering a sheet whose far margin has lifted"
description: "A lens fit assumes the paper is flat. When one margin lifts toward the camera it cannot follow, and the holes beside it read up to 0.08 inch out. How GroupLab lays a smooth correction over the lens fit, when it takes it, what it fixed on two pictures, and the three places it cannot reach."
group: How GroupLab is built
number: 37
written: 2026-10-10
data_date: "Built 2026-10-01 on pictures of the 2026-09-29 sitting; measured again 2026-10-10 on that sitting, two pictures of 2026-09-26 and seven mounted frames"
samples: "Two angled pictures (9 and 15 degrees) where the correction was taken, two more (1 and 2 degrees) where it was not, and seven frames of one mounted sheet. A small set, from a few sittings."
state: published
no_figure: "The two pictures it rests on are kept off the site, and the correction itself is a few pixels across a page, too small to see; the article gives it as numbers."
found: "The far column's worst error fell from 0.080 to 0.009 in at 9 degrees and from 0.083 to 0.016 in at 15, with the holes found and the false marks unchanged. The correction does not reach a margin past the last row of codes, or a curl that leaves several codes unread; on seven mounted frames the worst bulls were still 0.051 to 0.117 in out."
sure: "two pictures took the correction, and every picture and sheet is from the developer. There has been no second sitting with a lifted sheet since it was built. What the correction does on other sheets is not known."
sources:
  - "The method and its first result, docs/notes/archive/results-301-325.md, \"Entries 324, 325 and 327\", section 1, and the code and its comments in src/GroupLab.Core/Registration/BentSheetMapping.cs."
  - "Where the correction helped and where it cannot reach, docs/PHASE1-RESULTS.md, \"Entry 399: accuracy on photographs (2026-10-10)\", sections 1, 3 and 4 (grouplab photo-parts)."
---

## The problem

GroupLab registers a photograph by finding the sheet's printed codes and fitting a homography, which is the map between a flat plane and a camera, plus a lens model with two radial terms. Both assume the paper is flat. When the paper is not, the codes in the bent part sit where neither a plane nor a lens puts them.

The case that prompted this: on the 9 and 15 degree pictures of a sitting on 2026-09-29, the far column of codes read 10 to 19 pixels from where the lens fit put it, with every marker decoded, each read about a tenth wider than a flat sheet allows. The far margin had lifted toward the camera. The lens fit rightly left those markers out, because they did not fit. But the holes beside them then measured up to 0.08 in off, since they were being mapped by a model that excluded the part of the sheet they were on. The same sheet photographed taped flat kept all 136 of its 136 corners, and richer lens models barely moved the far column's misses, so it was the paper and not the lens.

## The method

The lens fit stays what it was, fitted to the corners it kept. A correction goes on top.

1. At every corner, take what the lens fit leaves: the page position read, less the page position the fit predicts.
2. Fit those residuals with a smoothing thin-plate spline over the page, smoothed so that a corner's own noise is not followed. The page position of any pixel is then the lens fit's plus the correction there. GroupLab tries four smoothing values and takes the one that predicts each marker best from a correction fitted without it.
3. Past the markers the correction fades to nothing over a quarter of an inch. This is deliberate: on the 2 degree picture a correction carried on past the last row moved the printed panel's border enough to raise a false mark.

## When it is taken

A bend is not noise and not a misread, and the correction is only taken where it can be told apart. It is taken only if all of these hold:

- The lens fit left out a marker at least 0.04 in from where it put it.
- The correction fitted without that marker puts it near where it was read, within half the marker's own distance from the lens fit, or within the fit's own threshold if that is larger. A lifted margin's neighbors are lifted with it, so they predict it. A misread's or a noisy corner's neighbors are flat and do not.
- The corners the lens fit kept fit no worse than they did.
- The correction stays within 0.2 in over the markers.
- More corners are kept than the lens fit kept.

A flat sheet's lens fit keeps every corner, or leaves a few out by a whisker, and the correction is never fitted. At least 12 markers (48 corners) must remain after misreads are set aside. On the 1 and 2 degree pictures the far columns were 6 to 10 dmm off (a dmm is a tenth of a millimeter), which is 0.024 to 0.039 in and under the 0.04 in bar, and there the correction moved the holes by less than the hole finder's own spread, one hard hole's error up as often as down. The 9 and 15 degree pictures' far columns were 17 to 30 dmm off, 0.067 to 0.118 in.

## What it did

On the 9 and 15 degree pictures:

| | 9 degrees | 15 degrees |
|---|---|---|
| Far column's worst error before | 0.080 in | 0.083 in |
| Far column's worst error after | 0.009 in | 0.016 in |
| Worst error on the picture before | 0.080 in | 0.083 in |
| Worst error on the picture after | 0.028 in | 0.022 in |

Holes found and false marks were unchanged. Every other real picture and all 27 synthetic lines read identically. The registration figure shown to the person rises from about 0.0036 to 0.0075 in on those two pictures, because it now counts the far column. A later change runs the leave-one-out fits, each marker predicted without itself, at the same time, because on a bent photograph they took most of the time spent registering it.

## Where it cannot reach

Measuring on 2026-10-10 found where the bend stops helping.

**Between the codes on a sheet held flat.** With the lens fit, the worst bulls on the sitting's pictures carried a smooth error of up to 0.0104 in that the codes could not see, and the bend did not remove it. That error is the paper's own relief between the codes, magnified by a wide lens. It is the subject of [How far off square can a photograph be?](/research/how-far-off-square/).

**Past the last row of codes.** On seven mounted frames of one sheet, which hangs from one pin, the median bull was 0.0018 to 0.0046 in with the lens and the bend. The worst was 0.051 to 0.117 in, every time an outer bull on the lower edge. The sheet curls toward the camera at the bottom, beyond where the bend, fitted across the codes and faded past them, reaches. Most of that is the sighter bull an inch below the last row of codes, where the bend fades to nothing on purpose, and where no current GroupLab sheet puts a bull: every current definition keeps every bull inside its codes.

**Where the curl left codes unread.** Inside the codes, the lens and bend left 0.0051 to 0.0122 in on four of the frames and 0.063 to 0.116 in on the other three, where the curl left 2 to 8 codes unread. A bend can only be fitted to the corners that are read.

## A cylinder instead

GroupLab's cylinder model, which bends the whole sheet about one axis, reads those last three frames at 0.034 to 0.055 in. It is worse on three of the four other frames (0.0060, 0.0224 and 0.0578 in, against 0.0051, 0.0122 and 0.0093) and far worse on five of six flat photographs (0.044 to 0.097 in). A rule that chose it, by the number of unread codes say, would be set on seven frames of one sheet, and flat photographs miss a code or two as well, so GroupLab was not changed. There are too few frames to split into a set to tune on and a set to test on.

## What it rests on, and what is not known

- **Two pictures took the correction.** The 9 and 15 degree pictures of one sitting. Two more (1 and 2 degrees) did not need it. The mounted frames are seven of one sheet. That is a small base, and every picture is from the developer's own sittings.
- **No second sitting yet.** When the correction was built, the plan was to wait for a second sitting with a lifted sheet before writing it up. There has not been one, so this is the first two pictures' account and no more.
- **The bound was set from two pictures.** The largest correction taken, 0.2 in, is more than half again the far column's lift on the 15 degree picture; a more strongly lifted margin would be refused, not corrected.
- **What a different sheet does.** A different paper, mounting or camera may bend in ways this correction does not predict. When it cannot predict them it is left out and the lens fit stands, as before.
- **A hole's own error is larger than the bend.** The holes' own centers sit 0.0107 to 0.0180 in from the scan's at the median on photographs, against 0.0018 to 0.0034 in for the bulls, so the holes' own centers, not the shape of the paper, dominate a hole's error.

## What this means

**For a developer:** a model that rightly refuses part of the data can still leave a systematic error behind. Leaving the markers out was correct; mapping the holes beside them with a model that had never seen that part of the sheet was not. A correction that predicts a left-out marker from its neighbors, and is refused where it cannot, fixes the first without the second going wrong. Measure first whether the error is the paper's or the lens's: here richer lens models barely moved the far column's misses.

**For a shooter:** on a sheet lying flat the correction is never needed. A sheet hanging from one pin curled at its bottom edge in all seven frames, beyond what the correction reaches.
