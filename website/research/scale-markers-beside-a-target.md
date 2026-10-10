---
title: "Scale markers beside a target you did not print"
description: "A store-bought target has no printed codes for GroupLab to measure against. Four things placed beside it can give the scale instead: a measured board, a scale bar, corner brackets and a bank card. What each gave on computer-made photographs, and why none of it counts as proven until it has been photographed on real paper."
group: Reading targets
number: 35
written: 2026-10-10
data_date: "2026-10-04 and 2026-10-05; computer-made photographs only"
samples: "20 computer-made photographs per case, tilted up to 30 degrees, on two target sizes; 177 computer-made scenes of corner brackets laid beside a target on four surfaces. No photograph of real paper."
state: published
no_figure: "Every figure comes from computer-made photographs, and a picture of one would look like a real result, which none of them is yet; the numbers are in the article as tables."
found: "On computer-made photographs with a perfect print, the scale came out within 0.015 percent (a measured board), 0.05 to 0.07 percent (a scale bar), 0.07 and 0.2 percent (corner brackets on a 12 inch and a 23 by 35 inch target) and 0.14 to 0.16 percent (a bank card), as the median error. Corner brackets gave the scale to 0.03 to 0.13 percent even with the cut edge 10 mm from the paper, where the target's size taken from the cut corners was 9.4 to 12.3 percent out."
sure: "every figure here comes from pictures a computer made, assuming a perfect print. A real printer, real paper and a real card have not been tried. Until they are, these numbers are what the method can do at best, not what you will get."
sources:
  - "The four markers and their accuracy, docs/PHASE1-RESULTS.md, \"Entry 365: scale markers beside a target, A to D\" (grouplab marker-trial --photos 20, seed 365)."
  - "Corner brackets and the cut, docs/PHASE1-RESULTS.md, \"Entry 375: corner brackets that do not depend on the cut (2026-10-05)\" (grouplab surface-trial --brackets)."
  - "The decision on whether to write this up, docs/RESEARCH.md, the rows for scale markers beside a target and corner brackets."
---

## The problem

GroupLab measures a target by the codes printed on its own sheets, whose spacing it knows exactly. A target bought in a shop has no such codes. Its size is printed on the package, but a photograph needs a ruler in the picture.

So GroupLab can read markers placed beside the target. There are four, and the user chooses by what is to hand:

- **A measured board.** Stickers fixed near the corners of the backer the target sits on, measured once.
- **A scale bar.** A printed bar laid along an edge of the target.
- **Corner brackets.** Four L-shaped pieces, each holding two codes, cut out roughly and placed one near each corner of the target.
- **A bank card.** GroupLab finds it by its rounded corners.

## Read this first: none of it has been tried on paper

Every number below comes from photographs that a computer made, assuming that the codes were printed at exactly their designed size. There is no photograph of printed paper behind any of them. That leaves out the things most likely to go wrong: the printer's own scale error, ink spread on a small code, a card that is not quite flat, and the light.

The test that would settle it is written down and has not been done. It prints brackets and bars on Letter paper at actual size, sticks the board stickers to a backer, and photographs one target of known size four times, from where a shooter would stand: with the brackets at its corners, with a bar along its bottom edge, on the measured backer with nothing else, and with a bank card beside it. The line GroupLab then shows for each, such as "Scale from 4 corner brackets in the photo: good to about 0.15 percent", is compared with the known size. That is the check these figures are waiting for.

## What the computer-made photographs gave

The trial made 20 photographs for each case, tilted up to 30 degrees, on a 12 by 12 inch target and a 23 by 35 inch poster, with the markers printed exactly. The error is the larger of the width's and the height's, in percent. "Found" counts the photographs where the markers were found at all.

| Marker | Target | Found | Median error | 95th percentile | Worst |
|---|---|---|---|---|---|
| Corner brackets | 12 by 12 | 20 of 20 | 0.068 | 0.131 | 0.185 |
| Corner brackets | 23 by 35 | 20 of 20 | 0.203 | 0.696 | 0.753 |
| One scale bar | 12 by 12 | 20 of 20 | 0.054 | 0.334 | 0.575 |
| One scale bar | 23 by 35 | 16 of 20 | 0.061 | 0.509 | 0.525 |
| Two scale bars | 12 by 12 | 20 of 20 | 0.074 | 0.444 | 0.452 |
| Two scale bars | 23 by 35 | 19 of 20 | 0.064 | 0.336 | 0.838 |
| Measured board | 12 by 12 | 20 of 20 | 0.015 | 0.025 | 0.026 |
| Measured board | 23 by 35 | 19 of 20 | 0.014 | 0.023 | 0.028 |
| Bank card | 12 by 12 | 19 of 20 | 0.161 | 0.265 | 0.313 |
| Bank card | 23 by 35 | 11 of 20 | 0.135 | 0.251 | 0.251 |
| All markers together | 12 by 12 | 20 of 20 | 0.015 | 0.023 | 0.024 |
| All markers together | 23 by 35 | 20 of 20 | 0.014 | 0.027 | 0.072 |

Three things stand out. The measured board had the smallest errors, 0.014 and 0.015 percent at the median. The bank card was the least often found, 11 of 20 on the large target. And on the poster the corner brackets had the largest median error of any marker, 0.203 percent, with 0.753 worst.

GroupLab also gives each result a claim of how good it is. The brackets on the 12 inch target claimed 0.171 percent and held in 19 of 20 photographs. On the poster they claimed 0.353 percent and held in 17 of 20, so for the poster brackets the claim was too optimistic three times in twenty.

## What the trial changed

The trial changed the design in several places:

- Holding the four brackets to an exact rectangle turned 0.3 mm of placement into 1.2 percent of scale. The rectangle is now held softly.
- Fitting codes by their corners was better than by their centres: 0.04 percent for brackets, against 0.13.
- Codes 22 mm across on 28 mm arms did worse than codes 16 mm across on 22 mm arms, because the white border was too thin.
- The scale bars' codes went from 10 mm to 12 mm for the inch bar and 16 mm for the metric one, which took the poster bars from 4 of 20 found to 16 of 20.
- The card finder at first took a bracket's corner and a silhouette for a card, until it was made to refuse shapes near codes and shapes that are not filled or nearly a parallelogram.

## Brackets and the cut

The brackets are cut out by hand, and a crooked cut would put the piece's inner corner in the wrong place. A later trial made sure the scale does not depend on the cut at all. It laid the pieces on four surfaces, 177 scenes in all, with the cut edge 0, 2, 5 and 10 mm from the paper, each piece up to 2 mm astray and about 2 degrees turned.

| Gap between cut and paper | Target's size read from the cut corners |
|---|---|
| 2 mm | 2.0 to 3.1 percent out |
| 5 mm | 4.8 to 5.6 percent out |
| 10 mm | 9.4 to 12.3 percent out |

The scale taken from the codes was 0.03 to 0.13 percent out at the median, and 0.23 percent at worst, at every gap and laid either way. So the brackets' codes give the scale and the cut gives nothing, which is why the instructions now say to cut roughly and place one near each corner, flat, anywhere close.

The target's own corners can then be found from the paper's edges. They were called sure in 44 of the 177 scenes, and every one of those was within 0.6 mm of the true corner. In the rest, GroupLab starts at the brackets and the person drags the corners into place. Where it is never sure: a white target on a white counter, a target inked right to its edge on a light surface, and a target too small to leave 20 mm of edge between the brackets' arms, which is about 7.8 inches a side.

## Marking by hand, and a card

When someone marks a target by hand, on the computer or the phone, the scale is set from the markers by itself. A bank card is blanked in every copy at once, the marking is made on a blanked copy, and no picture with a card in it is ever sent.

## What is not known

- **Whether any of the figures hold on printed paper.** This is the main thing. The method is only as good as the print, and the print has not been measured here.
- **Real light and real cards.** A shadow across a code, a card with a worn corner, or a glossy card reflecting the sky are not in the computer-made photographs.
- **Tilt beyond 30 degrees.** The trial stopped there.
- **The poster.** The brackets' median of 0.203 percent on a 23 by 35 inch poster is an estimate from 20 photographs, and the claim of 0.353 percent was missed three times in twenty.

## What this means

**If you use one, use the board or the brackets, and hold the figure loosely.** On the computer-made photographs the board had the smallest errors and the card was the least often found. All of that is what a perfect print would give.

**Do not trust a number to the second decimal place from these until a photograph on real paper has agreed with it.** GroupLab shows a "good to about" figure beside the scale it takes from markers; read that, and remember that it was checked only against computer-made photographs.
