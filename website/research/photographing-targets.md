---
title: "How to photograph a target so it measures well"
description: "GroupLab places a photographed sheet to within a few thousandths of an inch, but light can still fool it about hole size. What we learned from 176 holes on real range photos, and a short checklist for taking a photo that measures like a scan."
group: Guides
number: 21
written: 2026-09-22
data_date: "2026-09-20"
samples: "176 holes in nine phone photos of four sheets, compared with 600 dpi scans of the same sheets"
state: draft
found: "on Alan's range day, GroupLab placed every photographed sheet to within 0.004 to 0.006 inch using its printed markers, so shot positions from photos are sound. Hole size is another matter: the same holes measured anywhere from 0.90 to 1.45 times the bullet diameter in photos, against a steady 0.92 to 0.95 in scans. The cause was not the camera's resolution or angle. It was the light: low sun puts shadow into and beside each hole, and the camera cannot tell shadow from hole."
sure: "nine photos of four sheets on one afternoon is enough to show the effect clearly and not enough to put a precise number on it. The advice below follows from it; the numbers will firm up as more photos come in."
data:
  - data/hole-ratio-by-image.csv
sources:
  - "GroupLab measurements of Alan Hayes's range sheets, 2026-09-20: question 38 in docs/QUESTIONS-FOR-PLANNING.md and the commit 'Question 38 answered by measuring'. Pixels only; no image metadata was read."
  - "GroupLab detection pipeline notes (docs/DETECTION-PIPELINE.md)."
---

## Two separate questions

When GroupLab reads a photo it answers two different questions:

1. **Where is each shot?** It finds the sheet's printed markers, works out exactly how the paper sits in the picture (angle, distance, perspective) and maps every hole back onto the flat sheet. This is where your group size, mean radius and zero come from.
2. **How big is each hole?** This helps GroupLab tell one hole from two overlapping ones, and it feeds the calibre guess.

The first holds up well in photos. The second is where light gets in the way.

## Position: photos are precise

![Marker fit error, scans and photos](/research/photographing-targets/figures/registration.png)

GroupLab reports how well the printed markers fit after it maps the sheet. On the range-day scans the error was 0.0023 to 0.0026 inch; on the photos, 0.0042 to 0.0060 inch. Photos are about twice as loose as scans, but both are small compared with any group a rifle can shoot. A few thousandths of an inch will not change anyone's mean radius.

## Size: photos disagree with themselves

![Hole size in scans and photos of the same sheets](/research/photographing-targets/figures/scan-vs-photo.png)

Each dot is the typical (median) hole size on one image, as a multiple of the bullet's diameter. The blue squares are 600 dpi scans; the orange dots are phone photos of the same sheets.

| Sheet | Bullet | Scan | Photos |
|---|---|---|---|
| .22 LR | 0.224 in | 0.76 | 1.07, 1.08 |
| 6 ARC | 0.243 in | 0.92 | 1.26, 1.33, 1.36 |
| 6.5 Creedmoor, 25-shot sheet | 0.264 in | 0.95 | 0.90 |
| 6.5 Creedmoor, 15-shot sheet | 0.264 in | 0.94 | 1.45, 1.41, 1.45 |

The scans of the three centrefire sheets agree within a few percent. The photos range from 10 percent smaller than the bullet to 45 percent bigger.

**It was not resolution.** Two photos of the 15-shot sheet taken at quite different resolutions (about 280 and 180 pixels per inch on the paper) gave 1.45 and 1.45. Two photos of different sheets at the same 180 pixels per inch gave 0.90 and 1.45.

**It was not the angle.** The worst reading, 1.45, came from the squarest, cleanest photo in the set, with all 34 markers found and the lowest fit error.

**It was the light.** The two 6.5 Creedmoor sheets were the same rifle, the same load and the same day. Their scans agree (0.95 and 0.94). Their photos do not (0.90 and 1.45). The 15-shot sheet was photographed later in the afternoon, with the sun lower. Low, raking light throws a shadow into and beside each hole, and a camera sees shadow and hole as the same dark shape.

![Why a photo can make a hole look bigger](/research/photographing-targets/figures/shadow-schematic.png)

## What GroupLab does about it

Because of this finding, GroupLab no longer judges hole size in a photo against a fixed factor. Where a sheet has enough clean single holes, it uses the sheet's own holes as the reference for what one hole looks like on that image, so shadow that enlarges every hole equally stops mattering. The stated calibre is the fallback. On all thirteen images from this range day, that approach flagged at most one hole, including the photo where the old method flagged all fifteen. The calibre guess from a photo is shown as rough and is never preselected with more confidence than the image allows.

## A checklist for a good photo

1. **Even, soft light.** Open shade, an overcast sky or indoor light from above. Avoid low sun across the paper, and avoid flash, which leaves a hot spot.
2. **Square on.** GroupLab corrects for angle, but a straight-on photo keeps every hole round and every marker sharp.
3. **Fill the frame with the sheet, all markers included.** Every corner marker and both codes in the picture, with a little margin. Do not crop them off.
4. **Flat paper.** Take it off the backer if it is curled, or hold it flat. Waves in the paper move holes.
5. **Hold still and focus on the paper.** Tap to focus on the middle of the sheet. A blurred edge blurs every hole.
6. **Use the main camera, not digital zoom.** Step closer instead of zooming.
7. **One sheet per photo.** Several sheets in one frame make identification harder and give each sheet fewer pixels.
8. **If it matters, scan it.** For hole sizes, for calibre, or for a sheet you will compare against others, a flatbed scan at 600 dpi is the reference. See the scanner article for the traps.
