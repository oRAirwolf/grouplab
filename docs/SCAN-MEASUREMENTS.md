# SCAN-MEASUREMENTS

Hard measurements from 17 real scanned shooting targets plus 2 blank reference
target PDFs, made to ground detector thresholds in data.

All measurements were produced by the scripts in `scan_analysis/` and can be
reproduced with the commands in `scan_analysis/README.md`. Nothing in this
document is a guess. Where a number is an estimate or an eyeball it says so, in
these words:

* **measured**, computed by a script from pixel data, reproducible.
* **estimated**, derived from measured quantities through a model that could
  be wrong (e.g. JPEG quality inferred from the quantisation table).
* **eyeballed**, read off a rendered crop by a human. Used only for
  ground-truth hole locations that a script could not find.
* **not measured**, stated explicitly, with the reason.

Environment: Python 3.11, OpenCV 4.x, numpy 2.4.4, Pillow 12.2.0, pypdf.

---

## 0. Corrections to the stated premises

Four things in the brief did not survive contact with the files. They are
listed first because two of them change what the pipeline has to do.

| Premise as stated | What the data says |
|---|---|
| "the 600 DPI scans are 4958 x 6458 px (8.263 x 10.763 in)" | **Confirmed exactly.** All 14 of the 600 DPI files are 4958 x 6458 px, 8.2633 x 10.7633 in, 209.89 x 273.39 mm. |
| "the DPI tag is accurate (bull grid measured 1.4992 in against 1.5 in nominal)" | **Confirmed and tightened.** Scoring-grid pitch measured across 14 files, 112 individual gaps: pooled mean **1.49845 in**, sd 0.00225 in, extremes 1.4911 and 1.5038 in. See §2. |
| "a naive connected-component approach found 24 of 25 bulls on 300_nm_hand_load.jpg" | My matched-filter bull finder gets **25 of 25** on that file and on every other 600 DPI OnTarget scan. See §2. |
| "retumbo.jpg, retumbo.png and retumbo_0001.jpg are the same physical target rendered grayscale, blue, and pink-over-blue" | **Wrong.** `retumbo.jpg` and `retumbo.png` are the same scan of the same sheet (OnTarget Target #18, grey/pink-blue rings, caption "Retumbo"); they are pixel-aligned to 0.005 px and correlate at r = 0.99952. `retumbo_0001.jpg` is a **different physical target**, OnTarget Target #1, solid red bullseyes, caption "300 Atip 49 Retumbo", a completely different hole pattern and blue ballpoint arrows. The three-way colour comparison in §4 is therefore across three *styles*, not three renderings of one sheet. |
| "every hole shows a BRIGHT CORE ... surrounded by a DARK RAGGED ANNULUS" | **Half right, and the half that is wrong matters.** The dark ragged annulus is universal. The bright core is **not reliably brighter than paper, and is often indistinguishable from it**: core intensity spans V = 75 to 255, with 24% of holes having a core peak within 5 grey levels of their own local paper (the white scanner lid seen through the perforation) while the darkest 10% sit at V < 155. Pooled core mean V = 192.5, paper mean V = 245.6, core sd = 26.7, core peak reaching 255. A detector that keys on "core brighter than surround" will fail; a detector that keys on the rim will not. See §3 and `scan_analysis/crops/hole_open_perforation.png`. |

One further surprise, reported in full in §6: **the crumpled, torn, taped target
warps the printed artwork by essentially nothing.**

---

## 1. File and image facts

Script: `s01_file_facts.py` (images), `s02_pdf_facts.py` (PDFs).

### 1.1 Raster files

All dimensions measured. Physical size = pixels / DPI tag. JPEG quality is
**estimated** by inverting the IJG scaling formula on the luminance
quantisation table (median over the 64 coefficients).

| File | px (W x H) | DPI tag | source of tag | inches | mm | mode | JPEG q (est) | subsamp | bytes |
|---|---|---|---|---|---|---|---|---|---|
| 300_nm_hand_load.jpg | 4958 x 6458 | 600 | JFIF unit=1 density=(600,600) | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 029 276 |
| 300_nm_factory.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 1 855 750 |
| 28_6_5..cci_450..jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 1 839 123 |
| 28_6_5..40_3..gm205mar..jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 1 814 316 |
| 28_6_5..rem_7_5_br..jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 1 870 886 |
| 28_6_5..42_4..gm205mar..jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 1 951 825 |
| n568.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 191 738 |
| n568-gm210m.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 291 213 |
| n568-ruag.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 112 101 |
| 338lmao.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 361 940 |
| 6_5retumbo.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 474 447 |
| retumbo.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 330 095 |
| retumbo_0001.jpg | 4958 x 6458 | 600 | JFIF | 8.2633 x 10.7633 | 209.89 x 273.39 | RGB | 86.4 | 4:4:4 | 2 389 821 |
| **retumbo.png** | 4958 x 6458 | **599.9988** | **PNG pHYs** | 8.2633 x 10.7634 | 209.89 x 273.39 | **L (grey)** | n/a | n/a | 3 444 907 |
| IMG_20250530_0001.jpg | 2545 x 3296 | **300** | JFIF | 8.4833 x 10.9867 | 215.48 x 279.06 | RGB | **75.3** | **4:2:0** | 610 006 |
| **1748713494260-…_1.jpg** | **789 x 1024** | **ABSENT** | no JFIF density, no EXIF resolution | **unknown from metadata** | | RGB | 89.5 | 4:4:4 | 108 181 |

Notes, all measured:

* **Thirteen of the sixteen raster files are byte-for-byte homogeneous in
  encoder settings**: same 600 DPI, same 4958 x 6458 geometry, same
  quantisation table (q ≈ 86.4), same 4:4:4 subsampling. They are one scanner
  in one configuration. Threshold work done on one of them transfers to the
  others.
* The 600 DPI window is **8.2633 x 10.7633 in on an 8.5 x 11 in sheet**. The
  scanner crops 0.237 in of width and 0.237 in of height *inside* the page.
  Consequence: **there is no page edge anywhere in these 14 files.** Any
  registration scheme that needs the sheet outline is unusable on this scanner.
* `retumbo.png` is **8-bit greyscale, mode L**, and its pHYs chunk encodes
  599.9988 DPI (2362 px/m rounded). It is not a colour file.
* The messaging-app file carries **no resolution tag at all**. Effective DPI
  must be derived from content. Derived two independent ways, both measured:
  789 px / 8.4833 in page width = **93.0 DPI**; and 174.46 px measured grid
  pitch / 1.875 in nominal = **93.05 DPI**. Use 93.0.
* `IMG_20250530_0001.jpg` is the only file with **4:2:0 chroma subsampling**,   which halves colour resolution. It is also the lowest JPEG quality (75.3).

### 1.2 PDFs

| PDF | MediaBox | inches | producer | content |
|---|---|---|---|---|
| IMG_20250530_0001.pdf | 610.56 x 791.04 pt | 8.4800 x 10.9867 | IJ Scan Utility / Canon SC1011 | one DCTDecode image, **2544 x 3296**, 8 bpc, DeviceRGB, 568 428 B stream |
| reference/4x5x_5dot.pdf | 612 x 792 pt | 8.5000 x 11.0000 | Microsoft: Print To PDF | **vector only, no images** |
| reference/target3x20.pdf | 612 x 792 pt | 8.5000 x 11.0000 | Microsoft: Print To PDF | **vector only, no images** |

**Is the PDF image the same bitmap as the sibling .jpg?** No, and it is worth
being precise about how close it is. Measured:

* Sizes differ by one pixel: PDF 2544 x 3296, JPG 2545 x 3296.
* Different quantisation tables, PDF luminance starts `[13, 9, 8, 13, …]`
  (q ≈ 61 estimated), JPG starts `[8, 6, 5, 8, …]` (q ≈ 76 estimated). They are
  **two independent JPEG encodes**, and the one inside the PDF is the lower
  quality of the pair.
* Content is the same scan: mean |difference| 1.59 grey levels, 99th percentile
  15, Pearson r = 0.9914 at zero offset.
* Sub-pixel phase correlation puts the true horizontal alignment at **+0.57 px**
  (not 0 and not 1), so the extra JPG column is not a simple edge pad, the two
  were resampled slightly differently.

Practical consequence: **use the .jpg, not the PDF.** Same picture, 15 quality
points better, no extra decode step.

### 1.3 The reference blank targets are not the targets in the scans

This was not asked for but it changes what "reference" means. Both blank PDFs
were parsed as vector geometry (`s03_ref_target_geometry.py`) and both are
**4 columns x 5 rows = 20 bulls on a 1.875 in pitch**:

| Reference PDF | grid | x pitch (in) | y pitch (in) | ring diameters (in) |
|---|---|---|---|---|
| 4x5x_5dot.pdf | 4 x 5 | 1.8743 ± 0.0019 | 1.8750 ± 0.0000 | dot 0.2698; ring pair 1.6531 / 1.7617 (centreline 1.7074, stroke 0.0543) |
| target3x20.pdf | 4 x 5 | 1.8753 ± 0.0009 | 1.8750 ± 0.0000 | dot 0.0525; 0.3024; 0.6138 |

Column centres, both files: x = 1.43, 3.30, 5.18, 7.05 in.
Row centres, both files: y = 2.064, 3.939, 5.814, 7.689, 9.564 in.

**No scan in the set uses either of these two layouts.** Fourteen of the scans
are 5 x 5 at 1.500 in pitch; `IMG_20250530_0001` is 4 x 5 at 1.875 in pitch and
*does* match the reference pitch, but it is OnTarget "Target #3" and neither
blank is that. Treat the blanks as unrelated reference material until someone
confirms otherwise.

---

## 2. Bull grid geometry

Script: `s04_bull_grid.py`. Method: ink mask → annulus matched filter swept over
ring radius at a 150 DPI working scale → non-max suppression → per-bull robust
circle fit at full resolution (3 IRLS passes trimming the worst 15% of
residuals) → row/column clustering → pitch and rotation.

Detection was **25 of 25 or 30 of 30 on every 600 DPI file**, verified visually
against overlays. All numbers below are measured.

### 2.1 Detection and pitch

Pitch is quoted for the **scoring grid only**; the sighter row sits at a
different, larger gap and is listed separately.

| File | bulls found / real | grid | x pitch (in) | y pitch (in) | ring fit rms (px) |
|---|---|---|---|---|---|
| 300_nm_hand_load.jpg | 25 / 25 | 5 x 5 | 1.4982–1.5012 (mean 1.5002 ± 0.0032) | 1.4997–1.5036 (mean 1.5011 ± 0.0032) | 7.7 |
| 300_nm_factory.jpg | 25 / 25 | 5 x 5 | 1.5003 ± 0.0045 | 1.5004 ± 0.0026 | 7.5 |
| 28_6_5..cci_450 | 25 / 25 | 5 x 5 | 1.4989 ± 0.0016 | 1.5003 ± 0.0033 | 7.5 |
| 28_6_5..40_3 gm205mar | 25 / 25 | 5 x 5 | 1.4985 ± 0.0021 | 1.5003 ± 0.0026 | 7.2 |
| 28_6_5..rem_7_5_br | 25 / 25 | 5 x 5 | 1.4997 ± 0.0031 | 1.5002 ± 0.0051 | 7.7 |
| 28_6_5..42_4 gm205mar | 25 / 25 | 5 x 5 | 1.4998 ± 0.0018 | 1.4994 ± 0.0026 | 7.3 |
| n568.jpg | 30 / 30 | 5 x 5 + 1 sighter row | 1.4965–1.5012 | 1.4956–1.4998 | 7.9 |
| n568-gm210m.jpg | 30 / 30 | 5 x 5 + sighters | 1.4975 ± 0.0017 | (scoring rows) ≈1.499 | 7.4 |
| n568-ruag.jpg | 30 / 30 | 5 x 5 + sighters | 1.4971 ± 0.0035 | ≈1.498 | 9.6 |
| 338lmao.jpg | 30 / 30 | 5 x 5 + sighters | 1.4940–1.4997 | 1.4964–1.4997 | 9.6 |
| 6_5retumbo.jpg | 30 / 30 | 5 x 5 + sighters | 1.4926–1.4997 | 1.4927–1.5010 | 9.9 |
| retumbo.jpg | 30 / 30 | 5 x 5 + sighters | 1.4981 ± 0.0039 | ≈1.498 | 7.5 |
| retumbo.png | 30 / 30 | 5 x 5 + sighters | 1.4981 ± 0.0039 | ≈1.498 | 7.5 |
| retumbo_0001.jpg | 30 / 30 | 5 x 5 + sighters | 1.4959–1.4976 | 1.4937–1.5020 | 9.8 |
| IMG_20250530_0001.jpg | **20 / 20** (+5 false, see below) | 4 x 5, **no sighter row** | 1.875 nominal | **1.8718–1.8784** | 5.2 |
| 1748713494260-…_1.jpg | 20 / 20 (+1 false) | 4 x 5 | **1.87596 ± 0.00579** | 1.875 (scoring rows) | 1.48 |

**Pooled scoring-grid pitch across all 14 OnTarget 1.5 in targets (112
row-to-row and column-to-column gaps): mean 1.49845 in, sd 0.00225 in, extremes
1.4911 and 1.5038, i.e. every measured gap is within ±0.009 in of nominal and
the mean is 0.0016 in under it.** This confirms
the project author's 1.4992 in figure and confirms the 600 DPI tag is accurate
to better than 0.1%.

### 2.2 Sighter row position

Measured on the five files that have one (row centre y, inches, from the top of
the scan window):

| File | scoring rows y (in) | gap row5 → sighter (in) |
|---|---|---|
| 338lmao.jpg | 1.1018, 2.5988, 4.0957, 5.5921, 7.0918 | **1.7979** (sighter at 8.8898) |
| n568.jpg | 1.1052, 2.6046, 4.1002, 5.5990, 7.0988 | **1.7984** (sighter at 8.8972) |
| retumbo_0001.jpg | 1.0998, 2.5935, 4.0956, 5.5912, 7.0903 | **1.8007** (8.8909) |
| 6_5retumbo.jpg | 1.1086, 2.6079, 4.1056, 5.5983, 7.0993 | **1.7987** (8.8980) |
| retumbo.jpg / .png | same as n568 family | ≈1.798 |

So the sighter row sits **1.799 ± 0.001 in below the last scoring row**, i.e.
0.299 in further than a normal 1.500 in step. That single number distinguishes
the sighter row from a scoring row without any OCR.

### 2.3 Ring geometry by target style

Ring diameters are from the angle-averaged ink profile (half-maximum crossings)
at every detected bull, median across bulls. Stroke is cross-checked two ways:
angle-averaged FWHM and **per-radial-ray FWHM** (720 rays), which agreed to
0.0013 in on 300_nm_hand_load (32.25 px vs 31.5 px median).

| Style (OnTarget #) | files | fitted centreline dia (in) | outer dia (in) | inner dia (in) | stroke (in) | centre dot dia (in) |
|---|---|---|---|---|---|---|
| **#51** black thin ring | 300_nm_*, 28_6_5_* | **1.1977–1.2010** | 1.2507–1.2530 | 1.1426–1.1431 | **0.0525** (per-ray median; 0.0539–0.0551 by profile FWHM) | 0.1031 |
| **#18** blue/grey thin ring | n568*, retumbo.jpg, retumbo.png | **0.9477–0.9499** | 0.9989–1.0003 | 0.8924–0.8926 | 0.0533–0.0539 | 0.0535 |
| **#1** solid red bull | 338lmao, 6_5retumbo, retumbo_0001 | 0.4893–0.4899 | **0.5684–0.5685** | **0.2720–0.2724** (the white annulus) | 0.1481 (annulus radial width) | 0.0530 |
| **#3** solid red bull | IMG_20250530_0001, messaging copy | 0.5001 | 0.5778 | 0.2649 | 0.1564 | 0.0637 |

The **narrowest printed feature that matters** is the #51/#18 ring stroke at
**0.0525–0.0539 in** (31–32 px at 600 DPI, 16 px at 300 DPI, 5 px at 93 DPI).
The barcode is finer: measured dark bar widths on 300_nm_hand_load run
**0.0050 to 0.0567 in** (3 to 34 px), 30 bars across 2.3 in.

### 2.4 Grid rotation relative to the image axes

Fitted independently from rows (line through each row of centres) and columns.
Positive = counter-clockwise. All measured.

| File | rotation from rows (deg) | rotation from cols (deg) | rows−cols non-orthogonality (deg) |
|---|---|---|---|
| 300_nm_hand_load | −0.079 ± 0.023 | +0.043 ± 0.024 | 0.122 |
| 300_nm_factory | +0.023 | +0.192 | 0.169 |
| 28_6_5 cci_450 | −0.095 | +0.058 | 0.153 |
| 28_6_5 40_3 gm205mar | −0.069 | +0.057 | 0.126 |
| 28_6_5 rem_7_5_br | −0.150 | +0.002 | 0.152 |
| 28_6_5 42_4 gm205mar | −0.110 | +0.039 | 0.149 |
| n568 | −0.148 | +0.000 | 0.148 |
| n568-gm210m | −0.164 | −0.041 | 0.123 |
| n568-ruag | −0.199 | −0.064 | 0.135 |
| 338lmao | −0.145 | −0.003 | 0.142 |
| 6_5retumbo | −0.144 | +0.014 | 0.158 |
| retumbo.jpg / .png | −0.091 | +0.039 | 0.130 |
| retumbo_0001 | −0.244 | −0.089 | 0.155 |
| IMG_20250530_0001 | −0.148 | −0.086 | 0.062 |
| messaging copy | −0.098 ± 0.034 | −0.182 ± 0.276 | 0.084 |

Two things fall out, both measured:

1. **Skew is tiny**: |rotation| ≤ 0.244 deg on every file. Over the 7.5 in
   diagonal of the scoring grid that is at most 0.032 in of displacement.
2. **Rows and columns are consistently non-orthogonal by 0.12–0.17 deg**, with
   the same sign on 13 of 15 files. That is a systematic scanner shear, not
   paper placement, it is the same machine each time. It is absorbed exactly by
   an affine fit and *not* by a similarity fit (see §6).

### 2.5 Bulls the detector misses, and why

* **No misses on any 600 DPI OnTarget file** (25/25 or 30/30, all 14 files).
* `IMG_20250530_0001.jpg`: **20 real bulls found, plus 5 false positives.** The
  false positives have fitted radii of 82.5–90.2 px against 71.1–73.9 px for the
  real bulls; they sit in the footer text/barcode band. A radius-consistency
  filter at ±6% of the median removes all 5 and keeps all 20. **Reason for the
  false positives: the annulus matched filter is scale-selective but not
  scale-exclusive, dense text at the right spatial frequency produces a
  comparable response.**
* Messaging copy: 20 real + 1 false, same cause, same fix.
* If the DPI tag is absent and the default 600 is assumed, this detector fails
  completely on the messaging file (it reported nonsense pitch of 0.279 in
  because the radius sweep never reached the true 22.6 px ring radius). **The
  radius sweep must be driven by a derived DPI, not a defaulted one.**

---

## 3. Hole appearance

Script: `s05_holes.py` (detection + characterisation), `s12_summarise_holes.py`
(roll-up). Sample: **343 holes across 15 files** (every file except the
messaging copy, which was measured separately at its own DPI). That is well
above the requested 30 holes across 4 files.

### 3.1 The detection primitive that actually works, and why the obvious one does not

The obvious primitive, "bright region enclosed by dark", was implemented first
and returned **11 detections out of 27** on 300_nm_hand_load, mostly fragments.
The reason is visible in `crops/hole_open_perforation.png`: **the scanner lid is
white, so an open perforation reads at paper level.** The core is not a separate
bright object; it is the same brightness as the page it sits in, and it is
topologically connected to the interior of the printed ring whenever the ragged
rim has a gap.

What does work, and what the measurements below are based on:

```
Dn        = paper_level − max(R, G, B)          # "neutral darkness"
hole_map  = morphological_open(Dn, disk r = 0.032 in)
          → threshold → close (disk r = 0.055 in) → fill → convex hull
```

* The **open** step erases every printed stroke, because the widest printed
  stroke measured anywhere is 0.0567 in (a barcode bar) and the ring stroke is
  0.0525 in, both narrower than the 0.064 in opening disk.
* **Dn** near-zeroes red, pink and blue artwork, because those keep one channel
  at paper level. It does *not* suppress black or grey artwork, that is what
  the opening is for.
* The **convex hull** step is essential: the rim is frequently a C, not an O
  (see the four-panel debug in §3.7), and hull-based sizing recovers the hole
  where enclosure-based sizing does not.

With that, detection on 300_nm_hand_load is **25 true positives, 0 false
positives, 2 false negatives** out of 27 hand-verified holes.

### 3.2 Pooled hole measurements (n = 343)

All measured. V is the HSV Value channel, 0–255.

| Quantity | mean | sd | min | max |
|---|---|---|---|---|
| hull diameter (in) | **0.2655** | 0.0570 | 0.1504 | 0.5387 |
| darkest-rim diameter (in) | 0.2099 | 0.0518 | 0.0600 | 0.3375 |
| **paper V** (local, per hole) | **245.65** | 9.93 | 195.8 | 255.0 |
| **core mean V** | **192.55** | 26.70 | 74.5 | 249.6 |
| **core peak V** (98th pct) | **221.95** | 20.18 | 101.4 | 255.0 |
| core − paper (V) | **−53.1** | | −171 | +4 |
| **annulus minimum V** | **38.53** | 35.59 | 2.1 | 168.7 |
| annulus mean V (per-ray darkest) | 96.08 | 36.40 | 13.7 | 186.1 |
| **annulus radial thickness (in)** | **0.0702** | 0.0451 | 0.0142 | 0.2550 |
| **raggedness, sd of rim radius over angle (in)** | **0.0470** | 0.0250 | 0.0106 | 0.1428 |
| raggedness, coefficient of variation (sd / mean radius) | **0.477** | 0.277 | 0.095 | 1.785 |

Read the raggedness number carefully: **the rim radius varies by ±0.047 in
(1 sd) around a mean radius of ~0.105 in.** That is a coefficient of variation
of 0.48, the rim is not approximately circular, it is a lobed star. Any
circular-fit hole detector is fitting the wrong shape, and any threshold
expressed as "rim radius within X% of nominal" needs X ≥ 100.

`core − paper = −53 V` is the headline correction to the brief: **the core is,
on average, 53 grey levels DARKER than paper, not brighter.** The maximum
observed core−paper across 343 holes is +4, i.e. no hole in this corpus has a
core meaningfully brighter than the page.

### 3.3 Core intensity: a broad unimodal distribution with a long dark tail

I first wrote "bimodal" here from looking at crops, then computed the
distribution and it is not bimodal. Corrected, measured over the 343 holes:

| core mean V band | count | fraction | histogram |
|---:|---:|---:|---|
| 70–100 | 4 | 1.2% | `#` |
| 100–130 | 8 | 2.3% | `##` |
| 130–160 | 27 | 7.9% | `#######` |
| 160–190 | 68 | 19.8% | `#################` |
| **190–210** | **160** | **46.6%** | `########################################` |
| 210–230 | 70 | 20.4% | `#################` |
| 230–250 | 6 | 1.7% | `#` |

Percentiles of core mean V: p1 = 98.8, p10 = 154.2, p50 = 199.0, p90 = 217.6,
p99 = 239.6.

So the core is a **single broad mode centred at V ≈ 200 with a long tail down to
V ≈ 75**. The important fact for a detector is not the mode but the overlap
with paper:

* **core peak V** (98th percentile within the core) has p50 = 223, p95 = 255.
* **81 of 343 holes (24%) have a core peak within 5 grey levels of their own
  local paper level**, i.e. in a quarter of all holes, part of the core *is*
  paper-white.
* 18 of 343 (5%) have a core *mean* within 20 levels of local paper.
* The distribution of (core peak − local paper) runs from −86 at p1 to **+26 at
  p99**: some cores are brighter than the surrounding page.

**That overlap, not the shape of the distribution, is what kills a
bright-core detector.** There is no core-intensity threshold that separates
holes from paper: any cut low enough to catch the dark 10% is far below paper,
and any cut high enough to catch the bright 24% is at paper level.

### 3.4 Angle-averaged radial intensity profile

Pooled over all 343 holes, sampled on 360 rays per hole at 0.5 px steps, binned
to 5 thousandths of an inch. Split by whether the hole overlaps printed artwork.
All measured.

| r (mil) | r (in) | mean V, all | mean V, on bare paper | mean V, on printed ink |
|---:|---:|---:|---:|---:|
| 0 | 0.000 | 199.8 | 202.2 | 194.9 |
| 10 | 0.010 | 197.6 | 201.0 | 190.6 |
| 20 | 0.020 | 193.3 | 197.1 | 185.7 |
| 30 | 0.030 | 188.1 | 191.2 | 181.8 |
| 40 | 0.040 | 181.7 | 183.7 | 177.8 |
| 50 | 0.050 | 174.8 | 175.6 | 173.3 |
| 60 | 0.060 | 168.3 | 167.5 | 170.1 |
| 70 | 0.070 | 162.9 | 160.1 | 168.5 |
| 80 | 0.080 | 158.5 | 154.3 | 166.8 |
| **90** | **0.090** | **156.0** | **150.8** | 166.3 |
| **95** | **0.095** | **156.0** | **150.3** | 167.5 |
| 100 | 0.100 | 156.9 | 150.9 | 169.1 |
| 110 | 0.110 | 162.0 | 155.9 | 174.5 |
| 120 | 0.120 | 171.7 | 166.6 | 182.1 |
| 130 | 0.130 | 185.0 | 181.1 | 192.9 |
| 140 | 0.140 | 199.1 | 197.3 | 202.7 |
| 150 | 0.150 | 218.6 | 221.2 | 213.3 |
| 160 | 0.160 | 230.4 | 233.6 | 223.8 |
| 170 | 0.170 | 236.4 | 238.9 | 231.4 |
| 180 | 0.180 | 240.1 | 242.2 | 235.7 |
| 190 | 0.190 | 242.1 | 244.4 | 237.4 |
| 200 | 0.200 | 243.5 | 245.6 | 239.0 |
| 210 | 0.210 | 244.2 | 246.2 | 240.0 |

Shape of the pooled profile, measured: V starts at **200 at r = 0**, falls
monotonically to a **minimum of 156 at r = 0.090–0.095 in**, and recovers to
paper level (243) by **r = 0.200 in**. The angle-averaged trough is shallow
(156) compared with the per-hole per-ray minimum (38.5) precisely because the
rim is ragged, averaging over angle smears a deep narrow trough into a shallow
wide one. **Do not set a detector threshold from the angle-averaged profile.**

### 3.5 Hole diameter versus nominal bullet diameter

Two different diameters, both measured, both useful, on 300_nm_hand_load's
isolated bull-19 hole (.308 nominal), by direct radial ray measurement:

| definition | measured | vs .308 nominal |
|---|---|---|
| **perforation edge** (per-ray darkest point, median over 720 rays) | **0.2608 in** | 0.847 × |
| **outer edge of the disturbed zone** (last radius below paper − 25 V) | **0.3242 in** median, p10 0.230, p90 0.421 | 1.053 × |
| detector hull diameter for the same hole | 0.2668 in | 0.866 × |

The detector's hull diameter tracks the perforation edge, so the population
statistics below are perforation diameters, not disturbed-zone diameters.

**Calibre correction, 13 September 2026.** The brief attributed the `n568*`
targets to .338. The author has confirmed they are **.264, 6.5 mm**. The table
below is recomputed from `work/holes_all.json` on that basis. Note that `n568.jpg`
is broken out from its two siblings, because it behaves differently from them.

| calibre group | nominal (in) | n | mean hull dia (in) | sd | mean − nominal | ratio |
|---|---|---|---|---|---|---|
| 28_6_5_* and 6_5retumbo | 0.264 | 104 | 0.2293 | 0.0430 | −0.0347 | **0.869** |
| n568-gm210m, n568-ruag | 0.264 | 56 | 0.2401 | 0.0263 | −0.0239 | **0.909** |
| **n568.jpg** | **0.264** | **29** | **0.2961** | **0.0236** | **+0.0321** | **1.121** |
| 300_nm_* | 0.308 | 42 | 0.2833 | 0.0414 | −0.0247 | **0.920** |
| 338lmao | 0.338 | 29 | 0.3317 | 0.0856 | −0.0063 | **0.981** |

Pooled by calibre: **.264 gives 0.919 (n=189), .308 gives 0.920 (n=42), .338
gives 0.981 (n=29)**. The .264 and .308 pools now agree to three decimal places,
which they did not under the old attribution. Across all 260 holes with a known
calibre the deficit is **−0.0202 in, sd 0.0509**, roughly constant in absolute
terms rather than proportional, consistent with a fixed elastic closure.

**`n568.jpg` is now the anomaly, and it is a sharper one than before.** It is the
**only file in the corpus whose holes measure larger than the bullet**, at 1.121
of calibre, where every other file measures smaller. Its two siblings, shot with
the same powder and differing only in primer, measure 0.896 and 0.922. The
difference is not noise: `n568.jpg`'s standard deviation is 0.0236, the tightest
in the corpus, so the holes are consistently larger rather than erratically so.

Candidate explanations, none eliminated:

* **Bullet yaw.** A projectile arriving off-axis cuts an elongated hole. This is
  the explanation that would be most interesting if true, because it is
  detectable and nobody's software reports it.
* **A different bullet or load** than the sibling files, despite the filenames
  suggesting a primer comparison.
* **A different distance**, where residual yaw or stability differs.

**I attempted to settle this by fitting ellipses to the holes and measuring
aspect ratio and orientation, and the measurement was not trustworthy.** Two
passes, one ungated and one gated on size and border contact, produced major
axes inconsistent with the validated harness and aspect-ratio standard
deviations of 0.32 to 1.64, which is larger than the effect being looked for. The
ovality question is therefore **open and unmeasured**, not answered. A proper
measurement needs the same care the main harness got, and it belongs in Phase 1
alongside the detector rather than in a quick pass here.

### 3.6 On printed ink versus on bare paper

Measured, split of the 343-hole sample (114 on ink, 229 on bare paper):

| Quantity | on bare paper (n=229) | on printed ink (n=114) | difference |
|---|---|---|---|
| hull diameter (in) | 0.2717 ± 0.0537 | **0.2530 ± 0.0615** | −0.0187 (holes on ink measure 7% smaller) |
| core mean V | 195.6 ± 24.5 | **186.5 ± 29.8** | −9.0 |
| core peak V | 222.1 | 221.6 | −0.5 (no change) |
| local paper V | 246.3 | 244.3 | −2.0 |
| **annulus minimum V** | 33.8 ± 30.0 | **48.1 ± 43.4** | **+14.3, and sd grows 45%** |
| annulus mean V | 91.7 | 104.8 | +13.1 |
| **annulus thickness (in)** | 0.0641 ± 0.0389 | **0.0824 ± 0.0538** | **+0.0183 (29% thicker)** |
| raggedness sd (in) | 0.0456 | 0.0496 | +0.0040 |
| raggedness CV | 0.446 | 0.539 | +0.093 |

What a detector sees differently on ink, all measured:

1. **The rim gets shallower and less consistent.** Annulus minimum V rises from
   34 to 48 and its spread widens by 45%. The printed ink is *already* dark, so
   the rim's contrast against its immediate surround collapses on the ink side
   of the hole while staying full on the paper side. The rim becomes
   half-visible.
2. **The apparent rim thickens by 29%**, the hole's dark rim merges with the
   printed stroke and the two are no longer separable radially.
3. **Measured diameter shrinks by 7%**, for the same reason: the merged region
   is no longer a clean hull boundary.
4. The core is unaffected (core peak V identical at 221.6 vs 222.1). **The
   inside of the hole does not care what was printed there**, the paper carrying
   the ink is gone.

Crops: `crops/hole_on_black_ring.png`, `crops/hole_on_red_bull.png`,
`crops/hole_on_blue_ring.png` versus `crops/hole_open_perforation.png`.

### 3.7 Per-file hole statistics

Measured, mean ± sd over accepted holes.

| File | n | hull dia (in) | core mean V | annulus min V | raggedness sd (in) |
|---|---|---|---|---|---|
| 28_6_5 cci_450 | 16 | 0.2201 ± 0.0319 | 195.4 ± 25.7 | 24.8 ± 13.4 | 0.0578 ± 0.0127 |
| 28_6_5 40_3 gm205mar | 25 | 0.2388 ± 0.0349 | 194.1 ± 33.4 | 31.5 ± 16.3 | 0.0505 ± 0.0195 |
| 28_6_5 rem_7_5_br | 24 | 0.2188 ± 0.0279 | 190.0 ± 38.3 | 36.3 ± 40.9 | 0.0561 ± 0.0147 |
| 28_6_5 42_4 gm205mar | 20 | 0.2410 ± 0.0715 | 194.8 ± 20.5 | 48.0 ± 43.9 | 0.0616 ± 0.0236 |
| 300_nm_factory | 17 | 0.2649 ± 0.0408 | 205.3 ± 21.7 | 16.4 ± 5.6 | 0.0619 ± 0.0245 |
| 300_nm_hand_load | 25 | 0.2958 ± 0.0376 | 198.0 ± 26.1 | 18.9 ± 11.1 | 0.0714 ± 0.0298 |
| 338lmao | 28 | 0.3236 ± 0.0768 | 187.8 ± 25.5 | 68.0 ± 29.3 | 0.0512 ± 0.0262 |
| 6_5retumbo | 19 | 0.2256 ± 0.0572 | 193.6 ± 16.7 | 54.6 ± 51.6 | 0.0339 ± 0.0240 |
| IMG_20250530_0001 (300 DPI) | 7 | 0.1988 ± 0.0761 | 169.9 ± 54.3 | 21.7 ± 22.7 | 0.0480 ± 0.0158 |
| n568-gm210m | 28 | 0.2366 ± 0.0259 | 197.8 ± 16.5 | 41.7 ± 45.1 | 0.0282 ± 0.0137 |
| n568-ruag | 28 | 0.2435 ± 0.0267 | 199.3 ± 19.4 | 48.1 ± 33.3 | 0.0326 ± 0.0143 |
| n568 | 29 | 0.2961 ± 0.0241 | 194.4 ± 17.3 | 41.2 ± 33.1 | 0.0385 ± 0.0231 |
| retumbo.jpg | 25 | 0.3033 ± 0.0213 | 186.6 ± 24.9 | 21.6 ± 20.6 | 0.0306 ± 0.0205 |
| retumbo.png | 25 | 0.3110 ± 0.0421 | 178.6 ± 30.6 | 18.9 ± 20.7 | 0.0497 ± 0.0333 |
| retumbo_0001 | 27 | 0.2773 ± 0.0559 | 191.0 ± 29.0 | 60.4 ± 42.6 | 0.0468 ± 0.0163 |

Note `retumbo.jpg` and `retumbo.png` are the *same scan*: hull diameter 0.3033
vs 0.3110 in, core V 186.6 vs 178.6. **The 0.008 in / 8 V spread between two
measurements of literally the same holes is the measurement noise floor of this
harness.** Treat any difference smaller than 0.01 in or 10 V as not meaningful.

### 3.8 What I could not measure

* **Core diameter** as a separate quantity from hull diameter. My core-diameter
  estimator (walk outward until intensity halves between core and rim minimum)
  returns garbage when the core is dark: pooled mean 0.2857 in with sd 0.2248 in
  and a minimum of 0.0000 in. The estimator, not the data, is at fault: it has
  no defined answer when core ≈ rim. The column is present in
  `work/holes_all.json` as `core_dia_in` and **should not be used.** Use hull
  diameter (§3.5) instead.
* **Hole depth / whether the flap is still attached.** Not observable from a
  flatbed scan of one side.

---

## 4. Colour behaviour

Script: `s06_colour.py`. Classes are sampled from geometry, not hand-drawn
boxes: `paper` = far from every bull and every hole; `artwork` = the fitted ring
band of bulls with no nearby hole; `hole_core` = inside 0.35 r of a detected
hole; `hole_annulus` = the 0.80–1.15 r shell. Channel conventions: R/G/B and
S/V 0–255, H in degrees 0–360, L* 0–100, a*/b* signed around 0,
Dn = paper − max(R,G,B), chroma = max−min. All measured; 400 000 pixels per
class where available.

### 4.1 The four requested styles

**retumbo.png, greyscale rendering, OnTarget #18**

| class | R=G=B | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|
| paper | 246.6 ± 29.8 | 0 | 246.6 | 96.8 | 0 | 0 | 9.0 | 0 |
| artwork | **156.2 ± 28.2** | 0 | 156.2 | 64.3 | 0 | 0 | 98.8 | 0 |
| hole core | **158.7 ± 58.1** | 0 | 158.7 | 64.5 | 0 | 0 | 96.3 | 0 |
| hole annulus | 185.4 ± 72.5 | 0 | 185.4 | 74.2 | 0 | 0 | 69.7 | 0 |

**Fisher ratio, annulus vs artwork: 0.14 on every channel. Core vs artwork:
0.00.** In the greyscale rendering the printed ring and the hole are the *same
grey* (156.2 vs 158.7, a 2.5-level difference against sds of 28 and 58).
**There is no intensity-based separation of hole from artwork in this file at
all.** Only geometry separates them.

**retumbo.jpg, pink-over-blue, same scan**

| class | R | G | B | H° | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|---|---|---|
| paper | 249.5 | 245.6 | 249.5 | 101 | 6.7 | 251.1 | 96.9 | 2.1 | −1.3 | 4.7 | 5.7 |
| artwork | 197.5 | 133.4 | 187.1 | 298 | **100.7** | 217.2 | 64.3 | **33.5** | −17.2 | **37.9** | **86.5** |
| hole core | 162.7 | 164.3 | 167.0 | 200 | 15.1 | 168.4 | 66.7 | −0.1 | −1.5 | **86.6** | 7.5 |
| hole annulus | 179.0 | 175.0 | 183.1 | 172 | 27.6 | 187.5 | 71.1 | 3.9 | −3.5 | 67.7 | 15.6 |

Fisher, annulus vs artwork: **chroma 2.96**, a* 2.40, S 1.69, H 0.91, R 0.04.
Fisher, core vs artwork: **S 8.14**, chroma 7.47, a* 4.16.

**retumbo_0001.jpg, solid red, OnTarget #1** (a *different* sheet, see §0)

| class | R | G | B | H° | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|---|---|---|
| paper | 252.7 | 248.0 | 249.6 | 125 | 5.8 | 253.2 | 98.0 | 1.8 | 0.1 | 2.6 | 5.3 |
| artwork | **249.1** | 123.3 | 124.2 | 157 | 133.9 | 249.7 | 67.0 | **47.9** | 23.3 | **5.5** | **130.9** |
| hole core | 153.2 | 160.2 | 189.9 | 223 | 61.8 | 192.3 | 66.4 | 9.1 | −16.0 | **62.7** | 42.1 |
| hole annulus | 175.3 | 172.2 | 190.2 | 181 | 50.5 | 200.0 | 71.0 | 8.2 | −8.3 | 55.2 | 35.5 |

Fisher, annulus vs artwork: **a* 3.50**, chroma 2.05, S 0.94, B 0.89, Dn 0.75.

**6_5retumbo.jpg, solid red, OnTarget #1**

| class | R | G | B | H° | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|---|---|---|
| paper | 250.8 | 247.0 | 249.0 | 66 | 7.6 | 252.9 | 97.5 | 2.0 | −0.3 | 2.9 | 6.6 |
| artwork | 248.9 | 122.3 | 124.8 | 182 | 134.5 | 249.5 | 66.8 | **48.3** | 22.7 | 5.7 | 131.3 |
| hole core | 156.4 | 164.7 | 192.0 | 220 | 55.4 | 193.5 | 68.1 | 7.7 | −14.8 | **61.5** | 39.0 |
| hole annulus | 171.4 | 166.7 | 179.0 | 171 | 45.4 | 190.0 | 68.8 | 6.9 | −5.1 | **65.1** | 30.3 |

Fisher, annulus vs artwork: **a* 3.81**, chroma 2.57, S 1.23, Dn 0.85.

### 4.2 Two more styles for completeness

**n568.jpg, blue thin ring, OnTarget #18**

| class | R | G | B | H° | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|---|---|---|
| paper | 243.7 | 244.4 | 249.1 | 109 | 8.8 | 249.3 | 96.2 | 0.8 | −2.3 | 6.4 | 6.9 |
| artwork | 83.4 | 121.3 | **184.7** | 218 | 140.2 | 184.7 | 50.9 | 5.7 | **−37.4** | 70.4 | **101.4** |
| hole core | 176.2 | 180.3 | 191.8 | 219 | 24.2 | 192.1 | 73.4 | 2.3 | −6.4 | 62.9 | 16.6 |
| hole annulus | 181.5 | 184.6 | 194.9 | 201 | 28.0 | 196.6 | 74.5 | 2.1 | −5.6 | 58.5 | 18.1 |

Fisher, annulus vs artwork: **chroma 4.89**, a* 4.44, S 4.11, R 1.74, **Dn
0.04**. Note that Dn *fails* here: blue print and hole rims land at almost the
same Dn (70.4 vs 58.5) because blue's max channel (B = 184.7) is well below
paper.

**338lmao.jpg, solid red, OnTarget #1**

| class | R | G | B | H° | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|---|---|---|
| paper | 252.8 | 248.4 | 250.5 | 86 | 6.8 | 254.1 | 98.0 | 2.0 | −0.2 | 1.7 | 6.0 |
| artwork | 250.8 | 121.5 | 123.9 | 185 | 135.6 | 250.8 | 66.9 | **49.2** | 23.3 | 4.5 | 133.2 |
| hole core | 148.5 | 153.6 | 186.2 | 217 | 66.4 | 187.6 | 64.1 | 10.6 | −17.4 | 67.4 | 41.6 |
| hole annulus | 191.6 | 186.2 | 198.9 | 201 | 38.7 | 207.6 | 76.1 | 6.8 | −5.2 | 47.7 | 26.2 |

Fisher, annulus vs artwork: **a* 4.47**, chroma 3.34, S 1.65.

**300_nm_hand_load.jpg, black thin ring, OnTarget #51**

| class | R | G | B | H° | S | V | L* | a* | b* | Dn | chroma |
|---|---|---|---|---|---|---|---|---|---|---|---|
| paper | 244.4 | 243.4 | 244.2 | 98 | 3.3 | 244.7 | 95.6 | 0.4 | −0.2 | 11.0 | 1.6 |
| artwork | **64.0** | 60.6 | 56.2 | 85 | 39.3 | **65.2** | **25.7** | 0.6 | 3.3 | **189.8** | 9.8 |
| hole core | 190.2 | 189.4 | 190.8 | 223 | 11.1 | 193.3 | 76.4 | 0.5 | −0.5 | 61.7 | 6.9 |
| hole annulus | 183.7 | 183.3 | 182.7 | 151 | 13.6 | 186.2 | 73.4 | −0.1 | 0.6 | 69.0 | 6.1 |

Fisher, annulus vs artwork: **L* 2.78**, G 2.72, B 2.69, V 2.66, Dn 2.65,
**chroma 0.26, a* 0.03**. On black artwork the chromatic channels are useless
and only luminance separates, and only because the black print (V = 65) is much
darker than the hole rim (V = 186). Note this is the *opposite* sign to the
colour targets, where the hole is the darker thing.

### 4.3 The answer to the question the pipeline needs

**No, there is no single channel that separates holes from artwork across all
these styles.** The evidence:

| style | best single channel, annulus vs artwork | Fisher | chroma Fisher | L*/V Fisher | Dn Fisher |
|---|---|---|---|---|---|
| retumbo.png (grey ring) | R (= L*, no colour exists) | **0.14** | 0.00 | 0.11 | 0.14 |
| retumbo.jpg (pink+blue) | chroma | 2.96 | **2.96** | 0.05 | 0.15 |
| retumbo_0001.jpg (red) | a* | 3.50 | 2.05 | 0.02 | 0.75 |
| 6_5retumbo.jpg (red) | a* | 3.81 | 2.57 | 0.00 | 0.85 |
| 338lmao.jpg (red) | a* | 4.47 | 3.34 | 0.11 | 0.61 |
| n568.jpg (blue) | chroma | 4.89 | **4.89** | 0.89 | **0.04** |
| 300_nm_hand_load.jpg (black) | L* | 2.78 | **0.26** | **2.78** | 2.65 |

Reading across:

* **chroma (or a*) is the best channel on all five colour-printed styles**
  (Fisher 2.05–4.89) and is **useless on the two achromatic styles** (0.26 and
  0.00).
* **L* / V is the best channel on the black-printed style** (2.78) and is
  **useless on all five colour styles** (0.00–0.11).
* **Dn = paper − max(R,G,B) is not the universal answer either.** It is strong
  on black (2.65) and moderate on red (0.61–0.85) but collapses on blue (0.04),
  because blue ink's maximum channel is itself far below paper.
* **The worst case is the greyscale rendering, where the best of any channel is
  Fisher 0.14**, hole and artwork are the same grey. On that style, colour
  cannot help at all.

The honest conclusion for the pipeline: **colour is a useful accelerator on
colour-printed targets and provides zero information on the achromatic ones.**
A separation that works across all six styles must be primarily
morphological, the measured discriminator that does hold everywhere is
*stroke width*: every printed feature is ≤ 0.0567 in wide and every hole is
0.15–0.54 in across (§2.3, §3.2). That is a factor of 3–9 in scale and it is
present in every style including the greyscale one. Colour should be used as a
second, style-conditional vote, selected by first measuring the artwork's mean
chroma (5.3–7.6 on paper, 86–133 on colour artwork, 0 on greyscale, an
unambiguous three-way test).

---

## 5. Degradation through a messaging app

Script: `s13_degradation.py`. Comparing
`1748713494260-cf6994ff-96c5-4eda-8647-424356f59979_1.jpg` (789 x 1024, no DPI
tag, 108 KB) against `IMG_20250530_0001.jpg` (2545 x 3296, 300 DPI, 610 KB).

**These are the same physical target**, confirmed measured: after resampling the
300 DPI original to the small grid, phase correlation gives a residual shift of
(0.25, 0.52) px with response 0.88, and normalised correlation 0.9492.

### 5.1 What is lost

| Quantity | 300 DPI original | messaging copy | ratio |
|---|---|---|---|
| pixel dimensions | 2545 x 3296 | 789 x 1024 | 0.310 linear, 0.0963 area |
| effective DPI | 300 (tagged) | **93.0 (derived; tag absent)** | 0.310 |
| **px across one hole diameter** (mean hull dia) | **65.8 px** (0.219 in) | **19.0 px** (0.205 in) | 0.289 |
| **px across the printed ring stroke** (0.156 in) | 46.9 px | 14.5 px | 0.310 |
| fitted ring radius | 75.0 px | 22.6 px | 0.302 |
| **circle-fit rms residual on the bull ring** | 5.2 px = 0.0173 in | 1.48 px = **0.0159 in** | 0.92 |
| Nyquist limit | 150 cyc/in | **46.3 cyc/in** | 0.31 |
| **spectral energy above the 93 DPI Nyquist** (lost outright) | | **42.8% of the original's total** | |
| JPEG quality (estimated) | 75.3 | 89.5 | |
| chroma subsampling | 4:2:0 | 4:4:4 | |

### 5.2 JPEG blocking

Standard blocking metric: mean |gradient| across 8 x 8 block boundaries versus
across interior columns/rows. Ratio 1.0 = no visible blocking. Measured.

| image | horiz boundary | horiz interior | ratio | vert boundary | vert interior | ratio |
|---|---|---|---|---|---|---|
| messaging copy (93 DPI) | 4.384 | 3.690 | **1.188** | 3.845 | 3.011 | **1.277** |
| 300 DPI original | 3.308 | 1.925 | **1.719** | 3.330 | 1.706 | **1.952** |

**This is the opposite of what I expected and I checked it twice.** The 300 DPI
"original" has roughly *twice* the blocking of the messaging copy (ratio 1.72
and 1.95 vs 1.19 and 1.28). The explanation is in the encoder settings: the
original is q ≈ 75 with 4:2:0 subsampling straight out of the Canon; the
messaging app downsampled by 3.2x, which averages away the old block
grid, and then re-encoded at q ≈ 90 with 4:4:4, which adds very little new
blocking. **Downsampling is a very effective de-blocker.** The messaging app did
not add blocking artefacts; it removed them, along with the resolution.

### 5.3 Are holes still separable from artwork?

Yes. Measured, with the same detector, only the `dn_thresh` parameter changed:

| image | dn_thresh 28 | dn_thresh 18 | dn_thresh 12 | holes actually present |
|---|---|---|---|---|
| 300 DPI original | 7 | 17 | **26** | ~22 (eyeballed from the overlay) |
| messaging copy (93 DPI) | 12 | **25** | 31 | ~22 |

At its tuned setting each finds around 20 of the ~22 holes, verified visually in
`work/deg_panel.png`. **Holes remain separable at 93 DPI.** But note the tuning
requirement: **the same threshold does not work at both resolutions.** The
300 DPI file needs `dn_thresh = 12` where the 600 DPI files use 28, and the
93 DPI file needs 18. Contrast falls with resolution because each hole rim is
averaged over fewer, larger pixels. **Any fixed absolute intensity threshold in
the pipeline is a resolution-dependent bug waiting to happen.**

### 5.4 Smallest reliably detectable feature

Measured by high-pass modulation depth (image minus Gaussian blur at the feature
scale, 99.9th percentile of |difference|). A modulation depth below ~20 grey
levels is not reliably detectable against JPEG and paper-grain noise.

| feature width | 93 DPI copy: px / modulation | 300 DPI original: px / modulation |
|---|---|---|
| 5 mil (0.005 in) | 0.5 px / **not resolvable** | 1.5 px / 24 |
| 10 mil | 0.9 px / **13** (below noise) | 3.0 px / 52 |
| 15 mil | 1.4 px / 47 | 4.5 px / 73 |
| 20 mil | 1.9 px / 73 | 6.0 px / 91 |
| 30 mil | 2.8 px / 102 | 9.0 px / 117 |
| 40 mil | 3.7 px / 118 | 12.0 px / 131 |
| 55 mil | 5.1 px / 130 | 16.5 px / 143 |
| 75 mil | 7.0 px / 140 | 22.5 px / 152 |

**Smallest reliably detectable feature: ~0.015 in (15 mil, 1.4 px) at 93 DPI;
~0.005 in (5 mil, 1.5 px) at 300 DPI.** Both work out to about 1.4 px, which is
the expected answer, the limit is sampling, not compression.

Against that limit: the ring stroke on this target style is 0.156 in (10x the
93 DPI limit, safe), the hole diameter is ~0.21 in (14x, safe), and the finest
barcode bar is 0.005 in (**0.3x, the barcode is not resolvable at 93 DPI at
all**, which conveniently removes the biggest false-positive source, §8).

Crops: `crops/hole_300dpi_crumpled.png` vs `crops/hole_93dpi_messaging.png`
(same hole, both files).

---

## 6. The ugly one, how far from a rectangle, and does it matter

`IMG_20250530_0001.jpg`: crumpled, torn along the top-right, taped at the top,
visibly non-rectangular. Scripts: `s07_warp.py`, `s08_line_straightness.py`,
`s09_page_edge.py`.

**This section contains the single most important number in the report, and it
came out the opposite way to what the photograph suggests.**

### 6.1 The page outline is badly non-rectangular, and unmeasurable by thresholding

Measured facts about the sheet boundary:

* **The paper and the scanner backing differ by 3 grey levels.** Median grey
  inside the textured (paper) region 246.0, outside 249.0. Otsu, and every fixed
  threshold I tried, segments the printed artwork instead of the sheet. Local
  standard deviation (paper grain vs smooth lid) does better but still leaks.
  **The page outline of this scan cannot be recovered by intensity segmentation.
  I am reporting this as a measurement, not as a failure to try.**
* **The sheet runs off all four image borders** except where the tear cuts in.
  The scan window is 8.4833 x 10.9867 in on an 8.5 x 11 sheet.
* **The tear, traced directly** (the tear reads as a thin line below grey 215,
  traced every 4 rows): visible from y = 0 to y ≈ 2.1 in, over which the sheet
  edge moves from **x = 7.72 in to x = 8.48 in**, i.e. **0.76 in of missing
  paper at the top-right corner.** Below y ≈ 2.2 in the sheet runs past the
  frame and no edge exists to measure.
* A straight line fitted to the traced tear has residual **rms 0.265 in, max
  0.750 in**, the tear is not even approximately a straight edge.
* Black tape crosses the top edge, roughly x = 3.4 to 5.0 in at y < 0.1 in
  (eyeballed from `crops/page_tape_top.png`; the tape reads at grey 1–5, so it
  *is* trivially segmentable, unlike the paper).

**Verdict on the outline: if the pipeline registers on page corners or the page
outline, this scan destroys it, the top-right corner is 0.76 in away from where
a rectangle would put it, and three of the four edges are not in the image at
all.**

### 6.2 The printed artwork, however, is barely warped

Two independent measurements, both measured, both agreeing.

**(a) Bull lattice residuals.** Fit an ideal 1.875 in lattice to the 20 fitted
bull centres under progressively richer transforms, and look at the residual.

| Transform | rms residual | max residual |
|---|---|---|
| similarity (rotate + scale + translate) | 1.20 px = **0.0040 in** | 2.32 px = 0.0077 in |
| affine | 0.84 px = **0.0028 in** | 2.68 px = 0.0089 in |
| **homography (what 4 corner fiducials give you)** | **0.81 px = 0.0027 in** | **2.80 px = 0.0093 in** |
| biquadratic (a 30-fiducial-class model) | 0.76 px = 0.0025 in | 2.49 px = 0.0083 in |

Per-bull homography residual, in inches, laid out as the grid:

```
0.0037  0.0037  0.0039  0.0024
0.0025  0.0010  0.0093  0.0013
0.0016  0.0021  0.0025  0.0005
0.0013  0.0024  0.0027  0.0019
0.0025  0.0019  0.0057  0.0014
```

**Now the comparison that matters.** The same measurement on the *flat,
undamaged* scans:

| File | condition | homography rms | homography max | biquadratic max |
|---|---|---|---|---|
| **IMG_20250530_0001** | **crumpled, torn, taped** | **0.0027 in** | **0.0093 in** | 0.0083 in |
| 300_nm_hand_load | flat, pristine | 0.0029 in | 0.0073 in | 0.0058 in |
| 300_nm_factory | flat, pristine | 0.0026 in | 0.0066 in | 0.0070 in |
| n568 | flat | 0.0032 in | 0.0062 in | 0.0060 in |
| retumbo | flat | 0.0029 in | 0.0082 in | 0.0076 in |

**The crumpled sheet's homography residual (rms 0.0027 in, max 0.0093 in) is
statistically indistinguishable from the four pristine sheets (rms
0.0026–0.0032 in, max 0.0062–0.0082 in).** Going from a homography to a
biquadratic buys 0.0002 in on the crumpled sheet, nothing.

**(b) Straightness of the printed cell-border rules.** The bull lattice gives
only 20 samples. The printed grid rectangle gives a continuous line across the
sheet. Each rule was traced at sub-pixel accuracy by ink-weighted centroid every
3 px and fitted with a robust straight line.

`IMG_20250530_0001.jpg`, 6 horizontal rules (722–749 samples each), 5 vertical
rules (888–964 samples each):

| rule | span (in) | n | straight-line rms (in) | straight-line max (in) | after cubic, max (in) | slope (deg) |
|---|---|---|---|---|---|---|
| horiz @ 138 px | 7.35 | 722 | 0.00119 | 0.00321 | 0.00260 | −0.1109 |
| horiz @ 700 | 7.46 | 744 | 0.00126 | 0.00346 | 0.00397 | −0.1127 |
| horiz @ 1264 | 7.53 | 749 | 0.00149 | 0.00447 | 0.00704 | −0.1060 |
| horiz @ 1826 | 7.45 | 742 | 0.00094 | 0.00309 | 0.00276 | −0.1290 |
| horiz @ 2388 | 7.45 | 743 | 0.00127 | 0.00356 | 0.00421 | −0.1449 |
| horiz @ 2950 | 7.50 | 738 | 0.00187 | **0.00614** | 0.00780 | −0.1543 |
| vert @ 139 | 9.31 | 919 | 0.00065 | 0.00191 | 0.00193 | +0.0300 |
| vert @ 702 | 9.65 | 917 | 0.00083 | 0.00300 | 0.00309 | +0.0252 |
| vert @ 1264 | 10.93 | 964 | 0.00067 | 0.00233 | 0.00288 | +0.0232 |
| vert @ 1826 | 9.38 | 936 | 0.00091 | 0.00351 | 0.00313 | +0.0390 |
| vert @ 2387 | 9.55 | 888 | 0.00080 | 0.00275 | 0.00203 | +0.0259 |

**Summary for the crumpled sheet: worst departure from a straight line anywhere
along 11 printed rules totalling ~100 in of length is 0.00614 in; mean rms
0.00108 in.**

Comparison on flat sheets, same method:

| File | condition | worst straight-line departure | mean rms |
|---|---|---|---|
| **IMG_20250530_0001** | **crumpled/torn/taped** | **0.00614 in** | **0.00108 in** |
| 338lmao | flat | 0.00607 in | 0.00076 in |
| retumbo_0001 | flat | 0.01021 in | 0.00084 in |
| 6_5retumbo | flat | 0.02539 in | 0.00174 in |

**Two of the three flat sheets are *worse* than the crumpled one.**

### 6.3 The answer

**A global homography from four fiducials is sufficient for this scan, and the
residual at the worst point is 0.0093 in.**

There *is* local warping, the horizontal rules' slopes drift monotonically from
−0.1060 deg at the top to −0.1543 deg at the bottom, a real 0.048 deg shear
gradient, but it is a smooth, low-order deformation that an affine or
homography absorbs completely. The visible crumpling is out-of-plane wrinkling
of a stiff sheet lying flat under a scanner lid: the ridges cast shadows and
look dramatic, but they do not stretch the paper in-plane by a measurable
amount. `crops/page_crumple.png` shows a printed rule running straight through a
crumple ridge.

**On DESIGN.md's 30-to-40-distributed-fiducial approach:** on the evidence of
this corpus it is **not necessary for warp correction**. Going from 4 corners
(homography) to a 6-parameter-per-axis smooth model buys 0.0002–0.001 in, which
is a factor of 20 below the ~0.02 in that matters for group measurement and
below this harness's own 0.008 in noise floor (§3.7).

Two caveats I will not paper over:

1. **This is one damaged sample.** A sheet that has been rolled, folded in half,
   or wetted could behave completely differently. The measurement says
   "flatbed-scanned crumpled paper does not warp"; it does not say "no target
   ever warps". A photographed (not scanned) target is a different problem
   entirely and is not represented in this corpus at all.
2. **Distributed fiducials are still needed for a different reason.** §6.1
   shows the *page outline* is unusable on this scanner, cropped inside the
   page on all 14 600 DPI files, and 3-grey-levels-from-the-background on the
   300 DPI one. So registration cannot come from page corners; it must come from
   printed marks. If the printed marks used are the four grid corners, four is
   enough. If they might be occluded by a shot or a tear, and on
   `IMG_20250530_0001` the top-right corner region *is* torn away, then
   redundancy matters, not for accuracy but for **availability**. Thirty
   fiducials buy robustness against occlusion, not against warp. That is a
   defensible reason to keep them; "the page might be bent" is not.

---

## 7. Hand-drawn ink, and shots that cross cells

### 7.1 Hand-drawn annotations: what a detector sees

Measured. Stroke width = 4 x mean distance transform over the ink mask (exact
for a long uniform bar); "widest blob" = 2 x max distance transform.

| Annotation | file | stroke width (in) | widest blob (in) | grey median | chroma median |
|---|---|---|---|---|---|
| black marker X marks (cells 16, 21) | retumbo.png | **0.0625** | 0.1446 | 98 | 0 |
| black marker arrows (cells 17, 22) | retumbo.png | **0.0502** | 0.1152 | 105 | 0 |
| black marker caption "Hand Loads" | 300_nm_hand_load.jpg | **0.1021** | 0.1467 | 54 | 9 |
| blue ballpoint arrows | retumbo_0001.jpg | 0.0684 | 0.1086 | 62 | 151 |
| blue marker strokes | 6_5retumbo.jpg | 0.0491 | 0.0633 | 75 | 164 |
| blue caption "338 LMAO 300 Atip…" | 338lmao.jpg | 0.0799 | 0.1398 | 50 | 154 |
| blue arrow (cell 23) | n568.jpg | 0.0369 | 0.0706 | 120 | 97 |
| *(reference) printed ring stroke #51* | 300_nm_hand_load.jpg | *0.0525* | | 65 | 11 |

Crops: `crops/ink_X_over_hole.png`, `crops/ink_arrow_1.png`,
`crops/ink_X_sighter.png`, `crops/ink_arrow_blue.png`,
`crops/ink_arrow_over_grid.png`, `crops/ink_marker_text.png`,
`crops/ink_blue_caption.png`.

**What a detector sees, stated plainly:**

1. **Hand-drawn strokes are 0.037–0.102 in wide. The printed ring stroke is
   0.0525 in. They overlap.** The 0.032 in-radius opening that erases printed
   artwork therefore erases *some* hand-drawn strokes and keeps others. The
   thick black marker caption on `300_nm_hand_load` (0.102 in) survives every
   opening that keeps a 0.15 in hole.
2. **Blue and black hand ink are not equivalent.** Blue ballpoint has chroma
   97–164, so a chroma test separates it from a hole (hole annulus chroma
   15–42). Black marker has chroma 0–11, indistinguishable from a hole on a
   greyscale target. **Marker on a greyscale render is the single hardest case
   in the corpus.**
3. **X marks and arrowheads are locally compact.** An arrowhead is two strokes
   meeting at a point; after the closing step it becomes a blob of roughly the
   right size and roughly the right darkness for a hole. In the observed
   detections, five of the closed loops in the blue caption on `338lmao.jpg`
   (the "3", "8", "O", "A", "O" bowls) were detected as holes.
4. **X marks drawn *over* holes** (retumbo cells 16, 21 and one sighter) do not
   remove the hole signal, the hole rim remains visible around and between the
   marker strokes, but they change the hull, inflating measured diameter.
   Measured on `retumbo.png`: the hole under the X in cell 16 reports a hull
   diameter of **0.3804 in** (92nd percentile of that file's 25 holes), cell 21
   **0.3588 in** (88th), the marked sighter **0.3354 in** (76th), against a file
   mean of 0.3110 ± 0.0412 in. Marking a hole inflates its measured diameter by
   roughly 0.03–0.07 in.
5. **The marks are semantically meaningful and being thrown away.** The arrows
   in `retumbo.png` cells 17/22 and in `retumbo_0001.jpg` point *from* one cell
   *to* a hole in another, they are the shooter's own record of cross-cell
   assignment. A pipeline that discards them is discarding ground truth. Not a
   measurement, but it is what the crops show.

### 7.2 Cross-cell shots

Script: `s10_cell_assignment.py`. Because these are one-shot-per-bull targets,
the defensible ground truth is the **globally optimal one-to-one matching**
between detected holes and bulls (Hungarian, minimising total distance).
Nearest-bull is what a naive detector does. Where the two differ, nearest-bull
is wrong. Detections further than 1.10 in from any bull are excluded as
detector false positives (handwriting, barcode) rather than counted as shots.

**338lmao.jpg**, 25 scoring bulls, 29 detections, 18 kept as shots, 11 rejected
as non-shots (blue caption loops and barcode bars). Cell size 1.498 in.

| hole (in) | nearest bull | d_near (in) | matched bull | d_match (in) | margin to 2nd (in) | verdict |
|---|---|---|---|---|---|---|
| (7.734, 0.552) | r1c5/#5 | 0.7718 | r1c5/#5 | 0.7718 | 1.3379 | ok |
| (2.760, 0.729) | r1c2/#2 | 0.3771 | r1c2/#2 | 0.3771 | 1.0998 | ok |
| (0.918, 0.942) | r1c1/#1 | 0.3163 | r1c1/#1 | 0.3163 | 1.3760 | ok |
| (5.395, 1.053) | r1c4/#4 | 0.2889 | r1c4/#4 | 0.2889 | 0.9190 | ok |
| (3.519, 1.311) | r1c3/#3 | 0.7006 | r1c3/#3 | 0.7006 | 0.1505 | ok |
| **(6.040, 1.614)** | **r1c4/#4** | **0.6267** | **r2c4/#9** | **1.0430** | 0.4163 | **NEAREST-BULL WRONG** |
| (6.718, 1.882) | r2c5/#10 | 0.8460 | r2c5/#10 | 0.8460 | **0.0699** | ok (thin margin) |
| (4.154, 1.908) | r2c3/#8 | 0.6913 | r2c3/#8 | 0.6913 | **0.1134** | ok (thin margin) |
| (2.523, 2.215) | r2c2/#7 | 0.4201 | r2c2/#7 | 0.4201 | 0.7066 | ok |
| (1.292, 2.360) | r2c1/#6 | 0.2712 | r2c1/#6 | 0.2712 | 0.9838 | ok |
| (0.965, 3.476) | r3c1/#11 | 0.6676 | r3c1/#11 | 0.6676 | 0.2263 | ok |
| (2.862, 3.548) | r3c2/#12 | 0.5762 | r3c2/#12 | 0.5762 | 0.3878 | ok |
| (7.049, 3.554) | r3c5/#15 | 0.5510 | r3c5/#15 | 0.5510 | 0.4231 | ok |
| (5.688, 3.943) | r3c4/#14 | 0.1495 | r3c4/#14 | 0.1495 | 1.1986 | ok |
| (4.001, 4.096) | r3c3/#13 | 0.1788 | r3c3/#13 | 0.1788 | 1.1298 | ok |
| (1.153, 5.049) | r4c1/#16 | 0.5516 | r4c1/#16 | 0.5516 | 0.3958 | ok |
| (4.193, 5.087) | r4c3/#18 | 0.5040 | r4c3/#18 | 0.5040 | 0.4870 | ok |
| (2.973, 5.165) | r4c2/#17 | 0.5127 | r4c2/#17 | 0.5127 | 0.5893 | ok |

**Result for 338lmao: 1 of 18 shots lands outside its own cell, by 0.2316 in,
and nearest-bull assigns it wrongly.** The shot at (6.040, 1.614) is 0.627 in
from bull #4 and 1.043 in from bull #9. Nearest-bull gives it to #4, but #4
already has a much closer shot at 0.289 in, and #9 has none. **Nearest-bull
gets it wrong; one-to-one matching gets it right.** Two further shots have a
nearest/second-nearest margin under 0.15 in (0.0699 and 0.1134), i.e. a 0.05 in
registration error would flip them.

**The n568 family** (`--scoring-rows 5`, cell size 1.497–1.500 in):

| File | bulls | detections | kept as shots | outside own cell | worst overshoot | nearest-bull wrong |
|---|---|---|---|---|---|---|
| n568.jpg | 25 | 29 | 28 | **3** | 0.1715 in | **0** |
| n568-gm210m.jpg | 25 | 28 | 27 | 2 | 0.8539 in* | 1* |
| n568-ruag.jpg | 25 | 28 | 26 | **1** | 0.2725 in | **0** |

n568.jpg, the three cross-cell shots, all measured:

| hole (in) | belongs to | distance beyond cell edge (in) |
|---|---|---|
| (4.610, 0.187) | r1c3/#3 | 0.1707 |
| (4.188, 0.189) | r1c3/#3 | 0.1687 |
| (8.002, 7.426) | r5c5/#25 | 0.0908 |

n568-ruag.jpg: one shot, (5.372, 8.126), belongs to r5c4/#24, 0.2725 in beyond
the cell edge.

***I do not trust the n568-gm210m "0.8539 in / nearest-bull wrong" result.** It
has 27 shots for 25 bulls, so the Hungarian matching is forced to assign two
shots to distant unused bulls, and the "wrong" call at (4.045, 5.035), nearest
bull 0.578 in, matched bull 1.855 in, is an artefact of that forcing, not a
real cross-cell shot. **The one-to-one matching method is only valid when the
shot count equals the bull count.** The same caveat voids the
`retumbo_0001.jpg` row of the full run (13/27 outside cell, 2.73 in overshoot,
10 "wrong"), that file has genuine multi-shot cells plus detector false
positives from the blue arrows, and the matching output there is not
interpretable. Full output for every file is in `work/out/cells.txt` with these
caveats applying.

**Across the whole corpus** (the files where the matching is valid): 0 to 3
shots per target land outside their own 1.5 in cell, by **0.09 to 0.27 in**, and
**nearest-bull assignment is correct on all of them except the single 338lmao
case**. Nearest-bull fails specifically when a cell contains two shots and a
neighbouring cell contains none, geometry alone cannot resolve that; the
one-shot-per-bull constraint can.

Crop: `crops/hole_double_overlap.png` shows the same pathology on
`300_nm_hand_load.jpg` bull 20, where two perforations overlap into one blob.

---

## 8. Reproducing the naive baselines

Script: `s11_naive_baselines.py`. Target: `300_nm_hand_load.jpg`. Ground truth:
**27 holes**, the 25 found and visually verified by the §3 detector, plus 2
located by eye from saved crops (`work/miss_b15.png`, `work/miss_b20.png`): an
isolated hole outside bull 15 at (8.02, 4.42) in, and the second lobe of the
overlapping pair at bull 20 at (7.98, 5.88) in. Hit tolerance 0.15 in.

### 8.1 Baseline A, filled contour

Binarise, external contours, fill, keep blobs with equivalent diameter
0.18–0.45 in and circularity above a threshold. The threshold and circularity
were both swept because a single arbitrary setting proves nothing.

| variant | detections | TP / 27 | FP | FN | what the FPs are |
|---|---|---|---|---|---|
| Otsu (grey < 163), circ ≥ 0.55 | 0 | **0** | 0 | 27 | |
| grey < 180, circ ≥ 0.55 / 0.30 / 0.10 | 0 | 0 | 0 | 27 | |
| grey < 200, circ ≥ 0.55 / 0.30 / 0.10 | 0 | 0 | 0 | 27 | |
| grey < 215, circ ≥ 0.55 / 0.30 | 0 | 0 | 0 | 27 | |
| grey < 215, circ ≥ 0.10 | 1 | 1 | 0 | 26 | |
| grey < 230, circ ≥ 0.55 / 0.30 | 1 | 1 | 0 | 26 | |
| grey < 230, circ ≥ 0.10 | 3 | 1 | 2 | 26 | **footer text** |
| grey < 240, circ ≥ 0.55 / 0.30 | 1 | 1 | 0 | 26 | |
| grey < 240, circ ≥ 0.10 | 6 | 1 | 5 | 26 | **footer text** |
| grey < 247, circ ≥ 0.30 | 5 | 1 | 4 | 26 | footer text |
| grey < 247, circ ≥ 0.10 | 9 | 1 | **8** | 26 | **footer text** |
| grey < 250, circ ≥ 0.10 | 6 | 1 | 5 | 26 | footer text ×3, **barcode** ×1, paper ×1 |

**Best achievable anywhere in this family: 1 true positive out of 27.** It never
gets better; raising the threshold only adds false positives.

**Why it fails, measured.** Otsu on this page cuts at grey 163. At that cut:

* the 25 printed rings appear as contours of equivalent diameter **1.243–1.309
  in**, far above the 0.45 in size gate, correctly rejected;
* the 25 printed numerals appear as contours of **0.143–0.144 in** with
  circularity 0.305–0.342, below the 0.18 in size gate, correctly rejected;
* three contours at **0.101–0.107 in**, circularity 0.73–0.85, the printed
  centre dots (measured dot diameter on this style is 0.1031 in), below the
  size gate;
* **and zero contours in the 0.18–0.45 in band, because the hole rims are
  lighter than grey 163 over most of their circumference and simply are not
  ink at that threshold.**

Raising the cut to 240 puts 6 contours in the size band, but their circularity
is 0.1–0.3 (they are ragged lobed rims, not circles) so a circularity filter
that is loose enough to accept them is loose enough to accept the footer text
too. **The failure is not a bad threshold; it is that a bullet hole's rim is
neither a solid blob nor a circle, and this detector requires it to be both.**

That result is consistent with, and slightly worse than, the published
6-of-25. My reading is that the published run must have used a lower size floor
or a per-cell adaptive threshold; I could not find any setting of the
straightforward version that reaches 6, and I am reporting what I measured
rather than tuning until I hit the published number.

**What the false positives are: every one of them is in the footer band below
the bull grid.** At the most permissive setting (grey < 247, circ ≥ 0.10) the
8 false positives sit at y = 10.11–10.63 in on a 10.76 in page: seven of them at
y = 10.11, x = 5.60–7.45 in with equivalent diameters 0.219–0.400 in, which is
the **barcode**, and one at (0.27, 10.63) in the bottom margin. (The script's
`classify_fp` labels the y = 10.11 group "text" because its barcode band starts
at y > 0.955 x page height = 10.28 in; the band threshold is slightly too low.
The locations are measured; the sub-label is not to be trusted.) **Not** ring
segments and **not** centre dots, those are correctly rejected on size at every
setting, the rings at 1.243–1.309 in and the dots at 0.101–0.107 in.
`crops/barcode_footer.png` shows the barcode.

### 8.2 Baseline B, gradient energy (Hough circles)

Median blur, `cv2.HoughCircles` (HOUGH_GRADIENT, dp=1, param1=120, radius
0.09–0.22 in, minDist 0.20 in), swept over the accumulator threshold.

| param2 | detections | TP / 27 | FP | FN | what the FPs are |
|---|---|---|---|---|---|
| 25 | 30 | **26** | 4 | 1 | footer text ×3, handwriting/annotation ×1 |
| 32 | 23 | **23** | 0 | 4 | |
| 40 | 10 | 10 | 0 | 17 | |
| 55 | 0 | 0 | 0 | 27 | |

**This surprised me and I re-ran it.** A properly tuned Hough circle detector
finds **26 of 27 holes with 4 false positives** on this file, far better than
the published 7-of-20, and better than I expected given how ragged the rims are.
The gradient-energy approach is genuinely picking up the rim.

But look at the sensitivity: **param2 = 25 → 26 TP; param2 = 40 → 10 TP;
param2 = 55 → 0 TP.** A 2.2x change in one accumulator threshold takes the
detector from near-perfect to completely blind. The published 7-of-20 is exactly
what you get somewhere in the 38–42 range. **The 7-of-20 figure is reproducible
as an operating point, not as a property of the method.** The real finding is
that the method has no stable operating point: the correct param2 depends on rim
contrast, which §5.3 showed varies by more than 2x with scan resolution alone,
and §3.6 showed varies by 45% between holes on ink and holes on paper *within a
single scan*.

**What the Hough false positives are:** at param2 = 25 the four are measured at
(6.04, 10.14), (7.09, 10.07) and (2.02, 10.31), all in the footer band, the
first two on barcode bars, and **(2.82, 8.78), which is the "Hand Loads" marker
caption**. **Again, not ring segments and not centre dots.** The centre dots are 0.1031 in in diameter, i.e. radius
0.052 in, well below the 0.09 in minimum radius. The rings are radius 0.60 in,
well above the 0.22 in maximum. **The radius gate does its job; the things that
get through are the round-ish blobs in text and handwriting, which are the same
size as holes.**

### 8.3 What both baselines tell the pipeline

Measured, and consistent across both:

1. **The false-positive population is text, barcode and handwriting, not target
   artwork.** Every naive detector tested rejected rings, numerals and centre
   dots correctly, purely on size. Excluding the footer band below the bull grid
   would eliminate 100% of the observed false positives on this file at zero
   cost. That is a cheap, high-value guard.
2. **Solidity/circularity assumptions are what kill recall.** The measured rim
   raggedness (CV 0.48, §3.2) means a bullet hole is *not* a circle and *not* a
   filled disk. Any detector whose acceptance test assumes either will
   under-detect no matter how it is thresholded.
3. **Absolute grey thresholds do not transfer.** Baseline A needed grey < 240 to
   see anything at all on a page whose paper sits at 254 and whose printed ink
   sits at 65, a 14-level window against a 190-level dynamic range. §5.3 shows
   the same fragility across resolutions.

---

## 9. Numbers most likely to become detector constants

Collected for convenience. All measured, all from this corpus, all with the
caveats in their own sections.

| Constant | Value | Source |
|---|---|---|
| 600 DPI scan window | 4958 x 6458 px = 8.2633 x 10.7633 in | §1.1 |
| DPI tag reliability | accurate to < 0.1% where present; **absent on the messaging file** | §1.1, §2.1 |
| Scoring grid pitch | **1.49845 ± 0.00225 in** (n=112 gaps, range 1.4911–1.5038) | §2.1 |
| Sighter row offset | **1.799 ± 0.001 in** below the last scoring row | §2.2 |
| Widest printed stroke anywhere | **0.0567 in** (barcode bar); ring stroke 0.0525–0.0539 in | §2.3 |
| Grid rotation | ≤ 0.244 deg; row/col non-orthogonality 0.12–0.17 deg (systematic) | §2.4 |
| Hole hull diameter | **0.2655 ± 0.0570 in** (range 0.150–0.539) | §3.2 |
| Perforation vs calibre | **deficit −0.0202 in, sd 0.0509** (ratio 0.919 at .264, 0.920 at .308, 0.981 at .338; `n568.jpg` anomalous at 1.121) | §3.5 |
| Disturbed-zone vs calibre | **1.05 x nominal** | §3.5 |
| Paper V | 245.6 ± 9.9 | §3.2 |
| Hole core V | 192.5 ± 26.7, range 75–250; **24% of cores reach local paper level** | §3.2, §3.3 |
| Hole rim minimum V | **38.5 ± 35.6** (paper 34, on-ink 48) | §3.2, §3.6 |
| Rim radial thickness | 0.070 ± 0.045 in (paper 0.064, on-ink 0.082) | §3.6 |
| Rim raggedness | **sd 0.047 in, CV 0.48**, a lobed star, not a circle | §3.2 |
| Hand-drawn stroke width | 0.037–0.102 in, **overlaps the printed ring stroke** | §7.1 |
| Best colour channel | **style-dependent**: chroma/a* on colour print, L* on black, **nothing on greyscale** | §4.3 |
| Homography residual, worst sheet | **0.0093 in max, 0.0027 in rms** | §6.2 |
| Page outline usability | **unusable**: cropped inside the page on all 14 600 DPI files; 3 grey levels of contrast on the 300 DPI one | §1.1, §6.1 |
| Effective resolution floor | **1.4 px per feature**; 0.015 in at 93 DPI, 0.005 in at 300 DPI | §5.4 |
| Measurement noise floor of this harness | **0.008 in / 10 grey levels** (from re-measuring the same scan twice) | §3.7 |

---

## 10. Files

* `SCAN-MEASUREMENTS.md`, this document.
* `scan_analysis/`, the scripts (see `scan_analysis/README.md`).
* `scan_analysis/crops/`, 23 illustrative crops with `MANIFEST.txt` giving the
  source file, page coordinates in inches and a caption for each.
* `work/`, intermediate JSON/CSV/overlays from the runs that produced this
  report. Not part of the deliverable but kept so every number can be traced.
