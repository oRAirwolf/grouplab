# scan_analysis

Measurement harness for scanned multi-bull shooting targets. Produced the
numbers in `../SCAN-MEASUREMENTS.md`.

These scripts **measure**; they are not the detection pipeline. They are written
to be re-runnable as a regression harness: point them at a directory of scans
and diff the output against a stored baseline. Every script takes the scan
directory (or individual files) as an argument. **There are no hardcoded
absolute paths.**

## Requirements

```
pip install --break-system-packages opencv-python-headless numpy pillow scipy pypdf
```

`scipy` is used only by `s10_cell_assignment.py` (Hungarian matching); the
script degrades gracefully to nearest-bull if it is missing. Everything else
needs only opencv, numpy, Pillow and pypdf.

Tested with Python 3.11, opencv 4.x, numpy 2.4.4, Pillow 12.2.0.

## Scripts

| Script | What it measures | Report section |
|---|---|---|
| `common.py` | shared loaders, ink mask, matched-filter and circle-fit helpers — no CLI | — |
| `s01_file_facts.py` | pixel size, DPI tag and its source, physical size, colour mode, JPEG quality/subsampling, file size | §1.1 |
| `s02_pdf_facts.py` | PDF page boxes, embedded image size/filter/colourspace, stream hash, implied DPI | §1.2 |
| `s03_ref_target_geometry.py` | ground-truth bull geometry from a **vector** blank-target PDF | §1.3 |
| `s04_bull_grid.py` | bull detection, ring diameters, grid pitch, rotation | §2 |
| `s05_holes.py` | hole detection and per-hole characterisation, radial profiles | §3 |
| `s06_colour.py` | per-channel stats for paper / artwork / hole core / hole annulus, and Fisher separability | §4 |
| `s07_warp.py` | similarity / affine / homography / biquadratic residuals against the ideal bull lattice | §6.2 |
| `s08_line_straightness.py` | dense warp probe: departure of printed cell-border rules from straight | §6.2 |
| `s09_page_edge.py` | sheet-vs-background segmentation and departure of the outline from a rectangle | §6.1 |
| `s10_cell_assignment.py` | nearest-bull vs optimal one-to-one hole↔bull matching | §7.2 |
| `s11_naive_baselines.py` | reproduces the filled-contour and Hough-circle baselines, classifies every false positive | §8 |
| `s12_summarise_holes.py` | rolls `s05` output into the report tables | §3 |
| `s13_degradation.py` | resolution, blockiness, spectral loss and feature-size limit for a low-res copy | §5 |
| `s14_make_crops.py` | cuts the illustrative crops in `crops/` from page coordinates in inches | — |

## Full run

`$SCANS` is a directory of target scans; `$REF` a directory of blank-target
PDFs; `$OUT` a scratch directory for intermediates.

```sh
SCANS=/path/to/scans
REF=/path/to/reference/blank-targets
OUT=./work
mkdir -p "$OUT"

# 1. file and PDF facts
python3 s01_file_facts.py "$SCANS" --json "$OUT/file_facts.json"
python3 s02_pdf_facts.py "$SCANS" "$REF" --dump-images "$OUT/pdfimg"
python3 s03_ref_target_geometry.py "$REF"/*.pdf

# 2. bull grid  (~20 s per 600 DPI page; the whole set takes a few minutes)
python3 s04_bull_grid.py "$SCANS" --json "$OUT/bull_grid.json" \
        --overlay-dir "$OUT/overlays"

# an image with no DPI tag needs one supplied, and a matching work scale:
python3 s04_bull_grid.py "$SCANS/small.jpg" --dpi 93 --work-dpi 93

# 3. holes + radial profiles + crops
python3 s05_holes.py "$SCANS" --json "$OUT/holes_all.json" \
        --profiles "$OUT/hole_profiles.csv" \
        --overlay-dir "$OUT/overlays_holes"
python3 s12_summarise_holes.py --holes "$OUT/holes_all.json" \
        --profiles "$OUT/hole_profiles.csv"

# 4. colour
python3 s06_colour.py --grid "$OUT/bull_grid.json" --holes "$OUT/holes_all.json" \
        --scan-dir "$SCANS" --csv "$OUT/colour.csv"

# 5. degradation (low-res copy vs its higher-res original)
python3 s13_degradation.py --small "$SCANS/small.jpg" --large "$SCANS/orig.jpg" \
        --small-dpi 93 --grid "$OUT/bull_grid.json" --holes "$OUT/holes_all.json"

# 6. warp
python3 s07_warp.py --grid "$OUT/bull_grid.json" --scan-dir "$SCANS" --max-rows 5
python3 s08_line_straightness.py "$SCANS"/*.jpg
python3 s09_page_edge.py "$SCANS"/*.jpg --out-dir "$OUT/pageedge"

# 7. cell assignment
python3 s10_cell_assignment.py --grid "$OUT/bull_grid.json" \
        --holes "$OUT/holes_all.json" --scoring-rows 5

# 8. naive baselines (needs a ground-truth hole list, see below)
python3 s11_naive_baselines.py "$SCANS/300_nm_hand_load.jpg" \
        --grid "$OUT/bull_grid.json" --truth "$OUT/truth.json" \
        --overlay-dir "$OUT/baselines"

# crops
python3 s14_make_crops.py "$SCANS" crops \
        --dpi-override small.jpg=93
```

## Gotchas that will bite you

* **DPI.** Almost everything is expressed in inches and derived from the DPI
  tag. `s04` and `s05` fall back to 600 when there is no tag, and on a 93 DPI
  image that produces silent nonsense (the radius sweep never reaches the real
  ring size). Always pass `--dpi` for untagged images. Derive it from page width
  or from the known grid pitch — both agreed to 0.05% on the one untagged file
  in this corpus.
* **`s05 --dn-thresh` is resolution-dependent.** 28 works at 600 DPI, 18 at
  93 DPI, 12 at 300 DPI on the low-quality Canon scan. This is a property of the
  data (rim contrast falls as pixels get larger), not a bug, and it is one of
  the findings in §5.3.
* **The sighter row.** Files with a sighter row have 30 bulls in a 6-row
  lattice, but the last row is 1.799 in below the previous one rather than
  1.500 in. Pass `--max-rows 5` to `s07` and `--scoring-rows 5` to `s10` or the
  pitch statistics and cell sizes come out wrong.
* **`s10`'s one-to-one matching is only valid when the shot count equals the
  bull count.** With more detections than bulls it is forced to assign shots to
  distant unused bulls and reports spurious "nearest-bull wrong" calls. Check
  the "shots kept" line before believing the verdict column.
* **`s04` false positives on dense text.** The annulus matched filter responds to
  footer text and barcodes at the wrong scale. Filter detections by fitted
  radius (`--radius-filter` in `s07`/`s10`, default ±10–18% of median); ±6%
  cleanly separated 20 real bulls from 5 text artefacts on the 300 DPI file.
* **`s09` cannot segment the page on this corpus** and says so. The 600 DPI
  scans are cropped inside the sheet (no edge exists) and on the 300 DPI scan
  paper and backing differ by 3 grey levels. Its rectangle-departure numbers are
  only meaningful when the "real sheet edge" fraction it prints is high.
* **`core_dia_in` in `s05` output is unreliable** and is excluded from the
  report. Its estimator has no defined answer when the core is as dark as the
  rim. Use `blob_dia_in` (convex-hull equivalent diameter) as the hole diameter.
* **Runtime.** `s04` is about 20 s per 600 DPI page and `s05` about 12 s; a
  16-file set takes 5–10 minutes. Run them in the background rather than under a
  2-minute tool timeout.

## Ground truth for the baselines

`s11_naive_baselines.py --truth` takes a JSON file mapping filename to a list of
`[x_inches, y_inches]` hole centres:

```json
{ "300_nm_hand_load.jpg": [[2.353, 0.5555], [0.8647, 1.1078], ...] }
```

The list used for the report was built from `s05`'s 25 verified detections plus
2 holes located by eye from saved crops (an isolated hole outside bull 15 and
the second lobe of the overlapping pair at bull 20). Rebuild it the same way:
run `s05`, check the overlay, and add whatever it missed.

## Output conventions

* Lengths are reported in **inches** unless a column says `px`. Radial profiles
  use **mil** (thousandths of an inch).
* Intensities are the **HSV V channel, 0–255**, unless a column names another
  channel. L* is 0–100; a*/b* are signed around 0; H is degrees 0–360.
* "Fisher" is the Fisher discriminant ratio `(μa−μb)² / (σa²+σb²)` — larger is
  more separable; below about 1 the two classes overlap heavily.
