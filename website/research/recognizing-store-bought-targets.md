---
title: "Recognizing a store-bought target, and the scale you still check"
description: "A store-bought target can be recognized from a photograph without naming the wrong one, but only once its colors are checked as well as its shapes. Its scale is good on the sheet it was learned from. Whether your sheet was printed the same was never measured, so GroupLab says so."
group: Reading targets
number: 39
written: 2026-10-21
data_date: "2026-10-01 and 2026-10-07"
samples: "five store-bought blanks from one maker, scanned at 600 dpi; 200 computer-made pictures of them (150 phone views and 50 flatbed scans); 123 pictures of none of them, 81 of them real photographs and scans; and two range photographs of 4 October. Every picture of the five was made from the very sheet that was learned"
state: published
no_figure: "The targets are another company's printing and are not shown; the findings are counts, and the article gives them as numbers."
found: "Small image features alone named the wrong store-bought target, and with a picture's own target missing from the library they named another one for 111 to 138 of 200 pictures. Reading the target's colors through the fit as well removed every wrong name: none in 200, and none in 123 pictures of something else."
sure: "Computer-made pictures from one scanned sheet of each target, so lenses, light and shot holes are imitated, not met. The scale is measured only against the sheet the fingerprint came from; how much one printed sheet differs from the next was not measured and is in no number here."
sources:
  - "The trial, its pictures and every number: `docs/notes/fingerprint-trial.md` (entry 332, 2026-10-01), with its section on a second line for fits carried by many features (question 87, 2026-10-07)."
  - "The decision to use the printed size as the scale, with a warning: `docs/notes/archive/notes-2026-10.md`, entries 340 and 341."
  - "The code: `src/GroupLab.Core/StoreTargets/StoreTargetRecognizer.cs`, `Recognize`, and the warning `StoreTargetMatch.Warning` in `src/GroupLab.Core/StoreTargets/StoreTargetMatch.cs`."
---

## Why a store-bought target needs a fingerprint

A GroupLab sheet carries printed codes that say which sheet it is and how big it is, so a photograph of one measures itself. A target from a shop has no codes. To measure one without asking you to type its size, GroupLab has to recognize it from its printing alone and remember where its bulls are and how big it is.

That memory is a fingerprint, learned once from a flat 600 dpi scan of a blank target. It holds 830 to 2115 small image features with their places in inches on the target, the bulls' centers in the same inches, and a coarse color layout: the smoothed color of every half inch of the printing. The five fingerprints in the trial were 33 to 84 KB each.

To match a photograph, GroupLab finds features in it, fits the target's inches to the picture's pixels, looks again for every fingerprint feature where that fit puts it, and fits again from what it found. When the fit holds, the bulls and the scale come with it.

## Shapes alone name the wrong target

The five targets came from one maker: a sight-in grid, a 6 inch and an 8 inch scoring bull, an 8 inch crosshair, and a scoring bullseye from another of its lines. They share a black disc on a bright halo, repair pasters, and the maker's words and ring numbers in the same type. Features find those shared parts on all of them, which is exactly the trouble.

With features alone, at any threshold from 15 to 40 agreeing features, 3 to 7 of the 200 pictures were named as the wrong target. 2 to 6 of the 81 real pictures of other targets, and up to 2 of 12 pictures of a GroupLab sheet, were named as a store-bought one. And in the case a real library meets most, a target it has never learned, it was worse: with each picture's own target taken out of the library, another was named for 111 to 138 of the 200.

## The colors, read through the fit

What features cannot see is the layout of color across the whole sheet. So once a fit is found, GroupLab reads the picture's colors through it, half inch by half inch, and compares them with the fingerprint's layout. The true target, once found, mostly agreed at 0.95 to 1.00. The best agreement on a picture of none of the five was 0.21, and on a wrong target 0.81. GroupLab names a target only at 0.85 or more, and only 0.1 ahead of the next best.

| | result |
|---|---|
| phone views named | 121 of 150 |
| flatbed scans named | 42 of 50 |
| wrong target named | 0 of 200 |
| named on pictures of none of the five | 0 of 123 |
| named when the picture's own target is not in the library | 0 of 200 |

A miss is the safe outcome: GroupLab asks, and you pick the target from the list. Most misses were the sight-in grid, 24 of its 40 pictures (nearly all black, half of it close to symmetric, and shot holes with their halos change the few cells that differ), and the 8 inch bull, 11 of 40.

## A used target, and why many features are not enough

Two range photographs of 4 October showed targets GroupLab knew, already shot. The right target was found, but refused: shooting had pulled the color agreement down to 0.58 and 0.76, though 370 and 730 features agreed with the fit. So a second rule was tried, naming a target carried by many features at a lower agreement.

None was safe. The 8 inch crosshair fitted the 6 inch and 8 inch bulls with 570 to 590 features, because they share the same disc, halo and type, while on pictures of none of the five no fit passed 148 features. A rule low enough to reach the refused photograph, near 0.55, named the wrong target for 16 to 23 of the 200 pictures once their own target was taken out. The rule stayed as it was: a used target whose colors have been pulled down is asked about.

## The scale, and why it carries a warning

For the pictures named right, the scale from the fit was within a median of 0.06 percent on the phone views (0.45 percent at the 95th percentile) and 0.05 percent on the scans (0.74 percent). A bull's place was within a median of 0.007 inch on the phone views and 0.010 inch on the scans.

Those numbers say the fit is good. They do not say your target is printed like the one that was learned, because every picture was made from that very sheet. Measuring how much one printed sheet differs from the next would take a second sheet of each target; that was decided against, and the size as printed on the learned sheet is taken as every sheet's size. So wherever that scale is used, GroupLab says: "Scale from this target's printed size. Printed targets can vary a little from sheet to sheet; check it against a ruler or a GroupLab sheet if the numbers matter." **Check the scale** is one click away, and the warning blocks nothing.

There is one more trap. The 6 inch and 8 inch bulls are close to the same artwork at two sizes, a third apart. A photograph cut so that the few parts that do not scale are out of view could fit the wrong one, at the wrong scale. When both fit, GroupLab asks which you have rather than choosing.

## What this means

**If you shoot store-bought targets:** a recognized target gives you its bulls and a scale with nothing typed. If the numbers matter, check the scale once against a ruler or a GroupLab sheet beside the target. If GroupLab asks which size of a bull you have, answer it, because to the software the sizes look alike.

**If you build something like this:** small features match a maker's shared artwork, not the product. Confirm a fit with something the features cannot see, such as the color layout read through the fit, and test with the true product removed from the library, since that is where features fail worst and where a real library spends most of its life. Matching took about 100 ms a fingerprint on a desktop processor, so a library of more than a few dozen targets needs an index before it goes near a phone.
