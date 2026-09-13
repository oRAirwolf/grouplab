# Sample data notes

These are real targets shot and scanned by the project author. They are the
test set for the detection pipeline. Section 6 of DESIGN.md records the
measurements already taken from them.

## Measured facts

- The 600 DPI scans are 4958 by 6458 pixels, which is 8.263 by 10.763 inches.
  The scanner does not capture the full page. Software assuming an 8.5 by 11
  extent is roughly three percent wrong immediately.
- The DPI tag on those scans is accurate. Detected bull grid spacing measured
  1.4992 inches against a 1.5 inch nominal, an error of 0.05 percent. The
  problem is not that metadata lies, it is that there is no way to know when
  it does without something of known size in the frame.
- Bull detection is easy. A naive connected-component approach found 24 of 25
  bulls on 300_nm_hand_load.jpg. The miss was a bull whose ring a shot had
  partly destroyed.
- Hole detection is hard. A filled-contour approach returned 6 of 25 and a
  gradient-energy approach returned 7 of 20. The printed artwork dominates
  every generic signal. This is the baseline that render-and-difference must
  beat by a wide margin in Phase 1.
- Every hole in the set shows a bright core with a dark ragged annulus. The
  bright core is the scanner lid seen through the perforation, which means
  berm backer material does not affect scanned appearance at all. It affects
  photographs only.

## scans/

Detector test set. All are genuine scans of physically shot targets.

| File | Notes |
|---|---|
| `300_nm_hand_load.jpg` | 25 shots, .308, clean. The reference case. Grid measured at 1.5 inch spacing, rings 1.257 inches. |
| `300_nm_factory.jpg` | 20 shots, .308. Same target style, wider dispersion. Pairs with the handload file for load comparison. |
| `28_6_5_cm_*.jpg` | Four 6.5 Creedmoor targets varying primer and charge. Useful for load-versus-load significance testing on real data. |
| `n568.jpg`, `n568-gm210m.jpg`, `n568-ruag.jpg` | **.264, 6.5 mm.** Same load throughout; the primer was the only variable. Several shots land outside their own cells. Corrected 2026-09-13: these were described as .338 class in the original brief, and the measured hole diameters agree with .264 rather than .338. See SCAN-MEASUREMENTS.md. |
| `338lmao.jpg` | Many shots high and left of their bulls, several inside neighbouring cells. Hard assignment case. |
| `6_5retumbo.jpg` | Red annulus target style. Different detection problem from the black ring styles. |
| `retumbo.jpg`, `retumbo.png`, `retumbo_0001.jpg` | The same physical target rendered three ways: grayscale, blue, and pink over blue. Colour invariance test set. |
| `IMG_20250530_0001.jpg` and `.pdf` | The ugly one. Crumpled, torn along two edges, taped, non-rectangular, 300 DPI. Also demonstrates that scanners emit PDF. |
| `1748713494260-*.jpg` | Downscaled 789 by 1024 copy of the crumpled target, likely through a messaging app. Tests degraded input. |

### Cases these files establish

**Cross-cell assignment.** Several targets carry hand-drawn marker arrows from
a bull to a shot that landed outside its cell, sometimes inside a neighbour's.
On those targets the nearest bull is frequently the wrong bull. No geometric
rule recovers the correct answer. This is why section 13 requires an explicit
assignment interface.

**Ink over holes.** Some targets have X marks drawn across holes to indicate
exclusion. Ink on top of a perforation is a real classification case.

**Printing quirks.** At least one target has two bulls both labelled 9.

**Sighters.** Several styles include a row of bulls outside the numbered
scoring grid.

## What is missing

There is no genuine off-axis phone photograph in this set. One is required
before the distortion fitting in section 11 can be validated. It must be an
unresized camera original, shot handheld from normal standing distance and
deliberately off-axis.

## reference/blank-targets/

Unshot target sheets, for understanding what a printed target looks like
before it is used. Note the barcode in the footer, which is how the incumbent
identifies its targets.

## reference/ontarget-output/

Datasheets and composite group images produced by OnTarget TDS from some of
these same targets. Included so the statistics output can be sanity checked
against a known tool.

These are reference only. GroupLab implements no compatibility with OnTarget,
copies none of its target designs, and reads none of its formats.

## excluded/

`Gemini_Generated_Image_*.png` is an AI-processed version of the Retumbo
target, not a real scan. It is kept out of the test set deliberately. A
cleaned or synthetic image would flatter detector results and teach us
nothing. Do not use it for evaluation.
