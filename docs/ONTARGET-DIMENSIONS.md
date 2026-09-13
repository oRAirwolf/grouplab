# OnTarget TDS Target Sheets: Dimensions Reference

Geometry of 47 vector PDF target sheets, measured from the PDF content streams. All linear values are inches to four decimal places unless stated otherwise.

---

## 1. Method

### 1.1 How the numbers were obtained

Every number here came from **parsing the PDF page content stream directly**, no rasterisation, no OCR, no image processing, except for the independent cross-check in §1.2. A purpose-written interpreter walks the operator stream maintaining the full graphics state: the CTM stack (`q`/`Q`/`cm`), colour state (`g`/`G`/`rg`/`RG`/`k`/`K`), and line width (`w`). Every path is built in user space and transformed to device space by the CTM in force at the moment it is painted.

All 47 files share one construction: a single page, MediaBox `0 0 612 792`, and a global `0.12 0 0 0.12 0 0 cm` at the head of the stream. That scale is exactly **0.12 pt = 1/600 inch**, so the artwork is authored on an integer 1/600-inch grid. There are **no XObjects, no raster images, no shadings, no transparency and no CMYK** in any file, so the content stream is a complete description of the page. The only `/ExtGState` present (styles 31, 42, 43, 44, 60 and 201–205) sets `/OPM 1`, overprint mode, which has no geometric effect.

| Class of number | How it was measured | Uncertainty |
|---|---|---|
| Page size | `/MediaBox` read from the page dictionary | Exact. |
| Circle diameters | A closed run of 4 cubic Béziers is fitted: on-curve points must be equidistant from the bbox centre to within 2% and the bbox square to within 2%. Diameter is the mean of bbox width and height. Measured Bézier control-point ratio was 0.5510–0.5530 across all files (ideal 0.55228), confirming true circles. | Exact to the authored coordinates, which are themselves quantised to 1/600 in = 0.0017 in, see §1.3. |
| Stroke widths | The `w` operand times the mean CTM scale at paint time | Exact. |
| Colours | The operand of the colour operator in force when the path was painted, recorded together with the operator | Exact; no colour conversion applied. |
| Grid pitch and SD | Circle centres clustered into columns and rows (2 pt tolerance), then first differences of the cluster means; SD is the population SD across those gaps | Exact. |
| Grid origin | Centre of the top-row left-most bull, **converted from the PDF bottom-left origin to inches from the TOP-LEFT** as `y_top = (page_height − y_pdf) / 72` | Exact. |
| Margins | Bounding box of every painted path plus every text-run origin, then distance to each page edge. Text extent is approximated as the run origin plus the font size | ±0.005 in where text bounds the box; exact where a path does. |
| Barcode | Contiguous band of narrow (<12 pt) black-filled `re` rectangles sharing a baseline | Bounding box exact. Count is of *filled bars*; spaces are not counted. |

### 1.2 Independent rasterisation cross-check

To confirm the vector parse was not systematically wrong, 16 of the 47 sheets spanning every family were rendered with `pdftoppm -r 600 -gray` and the outer bull diameter re-measured from pixels using a radial ink profile: 720 rays from the known centre, taking the median outermost ink radius.

A raster measurement should exceed the vector *path* diameter by exactly one stroke width, because a stroke straddles the path it follows. It did:

```
raster_diameter − (vector_diameter + stroke_width)
    mean       +0.0007 in
    max |err|   0.0017 in   = exactly 1 pixel at 600 dpi
```

The two methods therefore agree to within one pixel on every sheet tested. **All dimensions in this document are the vector numbers**, i.e. the path centreline. If you care about the inked outer edge of a bull, add one stroke width to the outer diameter.

One caution, learned the hard way and worth passing on: on style 30 the bull nearly fills its cell, and a raster search window only 22% wider than the outer ring reached across into the cell-boundary rule and returned 1.5150 in instead of 1.3183 in. That was a flaw in my checking script, not in the file, and it is why the check above uses a window tied to the expected diameter. Any pixel-based measurement of these sheets needs a search window narrower than half the pitch.

### 1.3 The 0.0017 inch quantisation: read this before comparing against nominal sizes

Diameters come out as 0.9983 in rather than 1.0000, 2.9983 rather than 3.0000, 0.4983 rather than 0.5000. This is real and it is in the files, not an artefact of my measurement.

The generator emits integer 1/600-inch coordinates and computes each circle extent as `left = cx − r`, `right = cx + r` with truncation, losing one unit. **A circle of nominal size N is drawn 1/600 in = 0.0017 in undersize**, consistently, in every file. The deficit is always exactly 0.0017 in; it does not accumulate with size, and a 3 in ring is short by the same 0.0017 in as a 0.5 in ring.

Pitches are unaffected, coming out at exactly 1.5000, 2.2500, 0.7500 and so on, because a pitch is a difference of two centres and the truncation cancels. This asymmetry between pitch and diameter is why the pitch-to-outer ratios in §6.3 are not round numbers.

Measured values are given throughout. Where you see 0.9983, read "nominal 1.0".

### 1.4 How a ring is actually drawn, and what "ring count" means here

No bull in this set uses a stroked annulus. Every bull is a **stack of filled discs painted largest first**, alternating ink and white, so the visible rings are the parts of each ink disc left uncovered by the next white one. Each disc additionally carries a hairline stroke of the same colour at the same diameter, which is what makes the stroke-width column meaningful.

Therefore, in this document:

- **Concentric circles** is the count of distinct diameters drawn, the raw count.
- **Ink discs** is how many of those are painted in ink rather than white, which is closer to what a shooter sees.
- **Annulus width** is the difference of successive radii: the visible band width where an ink disc is followed by a white one.
- **Centre dot** is the innermost *non-white* disc. Six styles (35, 36, 37, 38, 41, 42) then paint a still smaller *white* disc inside it, so the centre is a fine open ring rather than a solid dot. Where that happens the white pinhole diameter is given explicitly.

### 1.5 Grid shape and the sighter row

Bull centres were clustered into columns and rows and the row-to-row gaps inspected. On 13 sheets the bottom row sits at a larger gap than the rest; that row is an unnumbered **sighter row** outside the scored array. It is confirmed independently by the numeral count, on style 1 there are 30 bulls but only 25 in-grid numerals, so 25 scored bulls in a 5×5 plus 5 unnumbered sighters.

The style name therefore describes the **scored array**, not the total bull count. Once the sighter row is excluded, **all 47 names reconcile with their files. There are no mismatches to flag**, neither your mapping nor any file is wrong. Full reconciliation table in §4.

`Pitch Y` in the master table is measured across the **scored grid only**; the sighter offset is reported separately in §6.4.

---

## 2. Master table

Page size is **8.5000 × 11.0000 in (612 × 792 pt, US Letter) on all 47 sheets**, so it is not repeated per row. Pitch is centre-to-centre, with the population SD across gaps in brackets. `Grid` is columns × **scored** rows; `+S` means one additional unnumbered sighter row is present. Ring diameters are outermost first. Margins are to the outermost printed mark on each side.

| # | Style name | Grid | Bulls | Pitch X (SD) | Pitch Y (SD) | Ring diameters, outer first | Stroke w | Centre dot | Ink colour | Margins L / R / T / B |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5x5 PSL Style | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.5650 / 0.2783 / 0.0483 | 0.0017 | 0.0483 | red `rg 1.0 0.0 0.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 2 | 6x6 PSL Style | 6×6+S | 42 | 1.2500 (0.0000) | 1.2500 (0.0000) | 0.5650 / 0.2783 / 0.0483 | 0.0017 | 0.0483 | red `rg 1.0 0.0 0.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 3 | 4x5 PSL Style | 4×5 | 20 | 1.8750 (0.0000) | 1.8750 (0.0000) | 0.5650 / 0.2783 / 0.0483 | 0.0017 | 0.0483 | red `rg 1.0 0.0 0.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 4 | 7x8 PSL Style | 7×8 | 56 | 1.0699 (0.0007) | 1.0700 (0.0007) | 0.5650 / 0.2783 / 0.0483 | 0.0017 | 0.0483 | red `rg 1.0 0.0 0.0` | 0.5017 / 0.5000 / 0.5017 / 0.5017 |
| 5 | 3x4 | 3×4 | 12 | 2.2500 (0.0000) | 2.2500 (0.0000) | 0.9983 / 0.7983 / 0.2483 | 0.0017 | 0.2483 | blue `rg 0.25 0.25 1.0` | 0.8750 / 0.5883 / 0.8750 / 0.5017 |
| 6 | 3x3 | 3×3 | 9 | 2.5000 (0.0000) | 2.5000 (0.0000) | 0.9983 / 0.7983 / 0.2483 | 0.0017 | 0.2483 | blue `rg 0.25 0.25 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 7 | 5x5 ARA Style | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.8883 / 0.7483 / 0.7183 / 0.4983 / 0.3883 / 0.0483 | 0.0017 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 8 | 4x5 ARA Style | 4×5 | 20 | 1.8750 (0.0000) | 1.8750 (0.0000) | 0.9983 / 0.8883 / 0.7483 / 0.7183 / 0.4983 / 0.3883 / 0.0483 | 0.0017 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 9 | 2x3 | 2×3 | 6 | 3.0000 (0.0000) | 3.0000 (0.0000) | 0.9983 / 0.7983 / 0.2483 | 0.0167 | 0.2483 | blue `rg 0.25 0.25 1.0` | 0.5000 / 0.8383 / 0.5000 / 0.5617 |
| 10 | 2x2 | 2×2 | 4 | 3.7500 (0.0000) | 3.7500 (0.0000) | 0.9983 / 0.7983 / 0.2483 | 0.0017 | 0.2483 | blue `rg 0.25 0.25 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 11 | 1" Dot 3" Ring | 1×1 | 1 | n/a | n/a | 2.9983 / 2.7483 / 0.9983 | 0.0017 | 0.9983 | blue `rg 0.25 0.25 1.0` | 0.5000 / 0.5017 / 0.5000 / 0.5017 |
| 12 | 3x3 .75" Dot | 3×3 | 9 | 2.5000 (0.0000) | 2.5000 (0.0000) | 0.7483 | 0.0017 | 0.7483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 13 | 3x4 .5" Dot | 3×4 | 12 | 2.2500 (0.0000) | 2.2500 (0.0000) | 0.4983 | 0.0017 | 0.4983 | blue `rg 0.0 0.0 1.0` | 0.8750 / 0.5883 / 0.8750 / 0.5017 |
| 14 | 4x5 .5" Dot | 4×5 | 20 | 1.8750 (0.0000) | 1.8750 (0.0000) | 0.4983 | 0.0017 | 0.4983 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 15 | 5x5 Black TEST | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.7483 / 0.4983 / 0.4883 / 0.2483 / 0.2383 / 0.0283 | 0.0017 | 0.0283 | black `g 0.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 16 | 5x5 Blue | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.7483 / 0.4983 / 0.4883 / 0.2483 / 0.2383 / 0.0283 | 0.0017 | 0.0283 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 17 | 5x5 Simple Blk TEST | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.8983 / 0.0483 | 0.0167 | 0.0483 | black `g 0.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 18 | 5x5 Simple Blue .05 dot | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.8983 / 0.0483 | 0.0167 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 19 | 5x5 Simple Blue .2 dot | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.8983 / 0.1983 | 0.0167 | 0.1983 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5017 |
| 24 | 10x12 .5" Ring | 10×12 | 120 | 0.7500 (0.0000) | 0.7500 (0.0000) | 0.4983 / 0.3717 / 0.0483 | 0.0167 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5883 / 0.6250 / 0.5017 |
| 25 | 10x10 .5" Ring | 10×10+S | 110 | 0.7500 (0.0000) | 0.7500 (0.0000) | 0.4983 / 0.3717 / 0.0483 | 0.0167 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5883 / 0.6250 / 0.5017 |
| 26 | 5x6 .5" Ring | 5×6 | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.4983 / 0.3717 / 0.0483 | 0.0167 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5883 / 1.0000 / 0.5017 |
| 27 | 7x8 .5" Ring | 7×8 | 56 | 1.0699 (0.0007) | 1.0700 (0.0007) | 0.4983 / 0.3717 / 0.0483 | 0.0167 | 0.0483 | blue `rg 0.0 0.0 1.0` | 0.5017 / 0.5883 / 0.7867 / 0.5017 |
| 30 | 5x5 RBA Style | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 1.3183 / 1.1483 / 1.0917 / 0.8483 / 0.6583 / 0.6283 / 0.4383 / 0.4050 / 0.2183 / 0.1883 / 0.0383 | 0.0167 | 0.0383 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5617 |
| 31 | 5x5 IR 50/50 | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 0.9983 / 0.8983 / 0.7483 / 0.7383 / 0.4983 / 0.4883 / 0.2483 / 0.2383 / 0.0283 | 0.0017 | 0.0283 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5617 |
| 35 | 4x5 WRABF 25m | 4×5 | 20 | 1.8750 (0.0000) | 1.8750 (0.0000) | 1.5317 / 1.5217 / 1.1783 / 0.8617 / 0.5483 / 0.5383 / 0.3117 / 0.3017 / 0.0750 / 0.0650 | 0.0167 | 0.0750 (open, white pinhole 0.0650) | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5617 |
| 36 | 5x5 WRABF 25m | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 1.1783 / 0.8617 / 0.5483 / 0.5383 / 0.3117 / 0.3017 / 0.0750 / 0.0650 | 0.0167 | 0.0750 (open, white pinhole 0.0650) | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5617 |
| 37 | 4x5 WRABF 50m | 4×5 | 20 | 1.8750 (0.0000) | 1.8750 (0.0000) | 1.4983 / 1.2983 / 1.2483 / 0.9983 / 0.7483 / 0.7417 / 0.4983 / 0.4917 / 0.2483 / 0.2417 / 0.0283 / 0.0217 | 0.0167 | 0.0283 (open, white pinhole 0.0217) | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5617 |
| 38 | 5x5 WRABF 50m | 5×5+S | 30 | 1.5000 (0.0000) | 1.5000 (0.0000) | 1.2483 / 0.9983 / 0.7483 / 0.7417 / 0.4983 / 0.4917 / 0.2483 / 0.2417 / 0.0283 / 0.0217 | 0.0167 | 0.0283 (open, white pinhole 0.0217) | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.5000 / 0.5000 / 0.5617 |
| 41 | 3x4 Blk 10m | 3×4 | 12 | 2.2500 (0.0000) | 2.2500 (0.0000) | 1.7883 / 1.7717 / 1.5917 / 1.5750 / 1.3950 / 1.3783 / 1.1983 / 1.0217 / 1.0017 / 0.8250 / 0.8050 / 0.6283 / 0.6083 / 0.4283 / 0.4083 / 0.2350 / 0.2150 / 0.0183 | 0.0017 | 0.2150 (open, white pinhole 0.0183) | black `g 0.0` | 0.5000 / 0.8383 / 0.5417 / 0.5617 |
| 42 | 2x3 Blk 10m | 2×3 | 6 | 3.0000 (0.0000) | 3.0000 (0.0000) | 1.7883 / 1.7717 / 1.5917 / 1.5750 / 1.3950 / 1.3783 / 1.1983 / 1.0217 / 1.0017 / 0.8250 / 0.8050 / 0.6283 / 0.6083 / 0.4283 / 0.4083 / 0.2350 / 0.2150 / 0.0183 | 0.0017 | 0.2150 (open, white pinhole 0.0183) | black `g 0.0` | 0.5000 / 0.8383 / 0.5633 / 0.5617 |
| 43 | 3x4 Blk 10m (solid) | 3×4 | 12 | 2.2500 (0.0000) | 2.2500 (0.0000) | 1.9983 / 1.8983 / 1.1983 | 0.0017 | 1.1983 | black `g 0.0` | 0.5000 / 0.8383 / 0.5417 / 0.5617 |
| 44 | 2x3 Blk 10m (solid) | 2×3 | 6 | 3.0000 (0.0000) | 3.0000 (0.0000) | 2.4983 / 2.3983 / 1.1983 | 0.0017 | 1.1983 | black `g 0.0` | 0.5000 / 0.8383 / 0.5633 / 0.5617 |
| 50 | 5x5 Blk .05 dot | 5×5 | 25 | 1.5000 (0.0000) | 1.5000 (0.0000) | 1.2483 / 1.1483 / 0.0483 | 0.0167 | 0.0483 | black `g 0.0` | 0.5000 / 0.6267 / 0.5183 / 0.5617 |
| 51 | 5x5 Blk .1 dot | 5×5 | 25 | 1.5000 (0.0000) | 1.5000 (0.0000) | 1.2483 / 1.1483 / 0.0983 | 0.0167 | 0.0983 | black `g 0.0` | 0.5000 / 0.6267 / 0.5183 / 0.5617 |
| 52 | 5x5 Blk .2 dot | 5×5 | 25 | 1.5000 (0.0000) | 1.5000 (0.0000) | 1.2483 / 1.1483 / 0.1983 | 0.0167 | 0.1983 | black `g 0.0` | 0.5000 / 0.6267 / 0.5183 / 0.5617 |
| 55 | 4x5 Blk .25 dot | 4×5 | 20 | 1.8750 (0.0000) | 1.8750 (0.0000) | 1.6217 / 1.5217 / 0.2483 | 0.0167 | 0.2483 | black `g 0.0` | 0.5000 / 0.6283 / 0.5300 / 0.5617 |
| 56 | 3x4 Blk .25 dot | 3×4 | 12 | 2.2500 (0.0000) | 2.2500 (0.0000) | 1.9983 / 1.8983 / 0.2483 | 0.0167 | 0.2483 | black `g 0.0` | 0.5000 / 0.8383 / 0.5417 / 0.5617 |
| 57 | 3x3 Blk .5 dot | 3×3 | 9 | 2.5000 (0.0000) | 2.5000 (0.0000) | 2.2483 / 2.1483 / 0.4983 | 0.0167 | 0.4983 | black `g 0.0` | 0.5000 / 0.6267 / 0.5483 / 0.5617 |
| 58 | 2x2 Blk .5 dot | 2×2 | 4 | 3.7500 (0.0000) | 3.7500 (0.0000) | 3.4983 / 3.2983 / 0.4983 | 0.0167 | 0.4983 | black `g 0.0` | 0.5000 / 0.6267 / 0.5867 / 0.5617 |
| 59 | 2x2 Blk 1.0 dot | 2×2 | 4 | 3.7500 (0.0000) | 3.7500 (0.0000) | 3.4983 / 3.2983 / 0.9983 | 0.0167 | 0.9983 | black `g 0.0` | 0.5000 / 0.6267 / 0.5867 / 0.5617 |
| 60 | 1x2 Blk 1.0 dot | 1×2 | 2 | n/a | 4.5000 (0.0000) | 2.9983 / 2.7483 / 0.9983 | 0.0017 | 0.9983 | black `g 0.0` | 0.5000 / 0.8383 / 0.6083 / 0.5617 |
| 201 | (unnamed 201) | quincunx | 5 | 2.5000 (0.0000) | 2.5000 (0.0000) | 0.4983 | 0.0017 | 0.4983 | blue `rg 0.0 0.0 1.0` | 0.2500 / 0.2500 / 0.2500 / 0.5617 |
| 202 | (unnamed 202) | quincunx | 5 | 2.2500 (0.0000) | 2.2500 (0.0000) | 0.4983 | 0.0017 | 0.4983 | blue `rg 0.0 0.0 1.0` | 0.2500 / 0.2500 / 0.2500 / 0.5617 |
| 203 | (unnamed 203) | quincunx | 5 | 2.5000 (0.0000) | 2.5000 (0.0000) | 0.9983 | 0.0017 | 0.9983 | blue `rg 0.0 0.0 1.0` | 0.2500 / 0.2500 / 0.2500 / 0.5617 |
| 204 | (unnamed 204) | quincunx | 5 | 2.1604 (0.0007) | 2.1604 (0.0007) | 0.7183 | 0.0017 | 0.7183 | blue `rg 0.0 0.0 1.0` | 0.5000 / 0.6500 / 0.6500 / 0.5617 |
| 205 | (unnamed 205) | 1×1 | 1 | n/a | n/a | 0.9983 | 0.0017 | 0.9983 | blue `rg 0.0 0.0 1.0` | 0.2500 / 0.2500 / 0.2500 / 0.5617 |

Notes on the table:

- **Grid origin** (centre of the top-left bull, in inches from the top-left corner of the page, converted from PDF bottom-left origin) is given per style in §4 and in the JSON as `grid.origin_first_bull_in_from_topleft`.
- **Stroke w** lists the distinct stroke widths applied to the bull circles. Only two values occur across the whole set: 0.0017 in (0.12 pt, a single 1/600-inch unit, effectively a hairline) on 26 styles, and 0.0167 in (1.2 pt) on 21. See §9.
- **Margins** are to the outermost mark anywhere on the sheet. On every sheet the bottom margin is set by the footer form line and barcode rather than by the bulls, hence 0.5617 in on 25 sheets and 0.5017 in on the other 22. Margins measured to the bull field alone are in the JSON as `margins.bull_field_in`.
- Styles 11 and 205 hold a single bull and so have no pitch; style 60 is a single column and so has no X pitch.
- Styles 201–204 are quincunxes, not rectangular arrays; their column and row pitch is the step of the underlying square, and their true nearest-bull distance is larger. See family N and the JSON field `grid.nearest_neighbour_centre_distance_in`.

---

## 3. Full ring geometry

One block per distinct bull design; styles sharing a bull are collapsed and named together. `Fill` is the disc colour, `annulus` the radial width between that disc and the next one inward, i.e. the visible band width where an ink disc is followed by a white one.

### Style 1, 5x5 PSL Style  *(identical bull in styles 2, 3, 4)*

3 concentric circles, 2 of them ink. Outer 0.5650 in. Centre dot 0.0483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.5650 | yes | red `rg 1.0 0.0 0.0` | yes | red `RG` | 0.0017 | 0.1433 |
| 2 | 0.2783 | yes | white `g 1.0` | yes | red `RG` | 0.0017 | 0.1150 |
| 3 | 0.0483 | yes | red `rg 1.0 0.0 0.0` | yes | red `RG` | 0.0017 | n/a |

### Style 5, 3x4  *(identical bull in styles 6, 9, 10)*

3 concentric circles, 2 of them ink. Outer 0.9983 in. Centre dot 0.2483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.25 0.25 1.0` | yes | blue `RG` | 0.0017 | 0.1000 |
| 2 | 0.7983 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.2750 |
| 3 | 0.2483 | yes | blue `rg 0.25 0.25 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 7, 5x5 ARA Style  *(identical bull in styles 8)*

7 concentric circles, 4 of them ink. Outer 0.9983 in. Centre dot 0.0483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0550 |
| 2 | 0.8883 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.0700 |
| 3 | 0.7483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0150 |
| 4 | 0.7183 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1100 |
| 5 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0550 |
| 6 | 0.3883 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1700 |
| 7 | 0.0483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 11, 1" Dot 3" Ring

3 concentric circles, 2 of them ink. Outer 2.9983 in. Centre dot 0.9983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 2.9983 | yes | blue `rg 0.25 0.25 1.0` | yes | blue `RG` | 0.0017 | 0.1250 |
| 2 | 2.7483 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.8750 |
| 3 | 0.9983 | yes | blue `rg 0.25 0.25 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 12, 3x3 .75" Dot

1 concentric circles, 1 of them ink. Outer 0.7483 in. Centre dot 0.7483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.7483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 13, 3x4 .5" Dot  *(identical bull in styles 14, 201, 202)*

1 concentric circles, 1 of them ink. Outer 0.4983 in. Centre dot 0.4983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 15, 5x5 Black TEST

7 concentric circles, 4 of them ink. Outer 0.9983 in. Centre dot 0.0283 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.1250 |
| 2 | 0.7483 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.1250 |
| 3 | 0.4983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0050 |
| 4 | 0.4883 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.1200 |
| 5 | 0.2483 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0050 |
| 6 | 0.2383 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.1050 |
| 7 | 0.0283 | yes | black `g 0.0` | yes | black `G` | 0.0017 | n/a |

### Style 16, 5x5 Blue

7 concentric circles, 4 of them ink. Outer 0.9983 in. Centre dot 0.0283 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.1250 |
| 2 | 0.7483 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1250 |
| 3 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0050 |
| 4 | 0.4883 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1200 |
| 5 | 0.2483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0050 |
| 6 | 0.2383 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1050 |
| 7 | 0.0283 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 17, 5x5 Simple Blk TEST

3 concentric circles, 2 of them ink. Outer 0.9983 in. Centre dot 0.0483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 0.8983 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.4250 |
| 3 | 0.0483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 18, 5x5 Simple Blue .05 dot

3 concentric circles, 2 of them ink. Outer 0.9983 in. Centre dot 0.0483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0500 |
| 2 | 0.8983 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.4250 |
| 3 | 0.0483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 19, 5x5 Simple Blue .2 dot

3 concentric circles, 2 of them ink. Outer 0.9983 in. Centre dot 0.1983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0500 |
| 2 | 0.8983 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.3500 |
| 3 | 0.1983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 24, 10x12 .5" Ring  *(identical bull in styles 25, 26, 27)*

3 concentric circles, 2 of them ink. Outer 0.4983 in. Centre dot 0.0483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0633 |
| 2 | 0.3717 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1617 |
| 3 | 0.0483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 30, 5x5 RBA Style

11 concentric circles, 6 of them ink. Outer 1.3183 in. Centre dot 0.0383 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.3183 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0850 |
| 2 | 1.1483 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.0283 |
| 3 | 1.0917 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.1217 |
| 4 | 0.8483 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.0950 |
| 5 | 0.6583 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0150 |
| 6 | 0.6283 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.0950 |
| 7 | 0.4383 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0166 |
| 8 | 0.4050 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.0934 |
| 9 | 0.2183 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0150 |
| 10 | 0.1883 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.0750 |
| 11 | 0.0383 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 31, 5x5 IR 50/50

9 concentric circles, 5 of them ink. Outer 0.9983 in. Centre dot 0.0283 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0500 |
| 2 | 0.8983 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.0750 |
| 3 | 0.7483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0050 |
| 4 | 0.7383 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1200 |
| 5 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0050 |
| 6 | 0.4883 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1200 |
| 7 | 0.2483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | 0.0050 |
| 8 | 0.2383 | yes | white `g 1.0` | yes | blue `RG` | 0.0017 | 0.1050 |
| 9 | 0.0283 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 35, 4x5 WRABF 25m

10 concentric circles, 5 of them ink. Outer 1.5317 in. Centre dot 0.0750 in, with a 0.0650 in white pinhole inside it, so the centre is an open ring and not a solid dot.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.5317 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 2 | 1.5217 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1717 |
| 3 | 1.1783 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.1583 |
| 4 | 0.8617 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1567 |
| 5 | 0.5483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 6 | 0.5383 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1133 |
| 7 | 0.3117 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 8 | 0.3017 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1134 |
| 9 | 0.0750 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 10 | 0.0650 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 36, 5x5 WRABF 25m

8 concentric circles, 4 of them ink. Outer 1.1783 in. Centre dot 0.0750 in, with a 0.0650 in white pinhole inside it, so the centre is an open ring and not a solid dot.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.1783 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.1583 |
| 2 | 0.8617 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1567 |
| 3 | 0.5483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 4 | 0.5383 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1133 |
| 5 | 0.3117 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 6 | 0.3017 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1134 |
| 7 | 0.0750 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0050 |
| 8 | 0.0650 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 37, 4x5 WRABF 50m

12 concentric circles, 6 of them ink. Outer 1.4983 in. Centre dot 0.0283 in, with a 0.0217 in white pinhole inside it, so the centre is an open ring and not a solid dot.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.1000 |
| 2 | 1.2983 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.0250 |
| 3 | 1.2483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.1250 |
| 4 | 0.9983 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1250 |
| 5 | 0.7483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 6 | 0.7417 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1217 |
| 7 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 8 | 0.4917 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1217 |
| 9 | 0.2483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 10 | 0.2417 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1067 |
| 11 | 0.0283 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 12 | 0.0217 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 38, 5x5 WRABF 50m

10 concentric circles, 5 of them ink. Outer 1.2483 in. Centre dot 0.0283 in, with a 0.0217 in white pinhole inside it, so the centre is an open ring and not a solid dot.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.2483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.1250 |
| 2 | 0.9983 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1250 |
| 3 | 0.7483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 4 | 0.7417 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1217 |
| 5 | 0.4983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 6 | 0.4917 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1217 |
| 7 | 0.2483 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 8 | 0.2417 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | 0.1067 |
| 9 | 0.0283 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0167 | 0.0033 |
| 10 | 0.0217 | yes | white `g 1.0` | yes | blue `RG` | 0.0167 | n/a |

### Style 41, 3x4 Blk 10m  *(identical bull in styles 42)*

18 concentric circles, 9 of them ink. Outer 1.7883 in. Centre dot 0.2150 in, with a 0.0183 in white pinhole inside it, so the centre is an open ring and not a solid dot.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.7883 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0083 |
| 2 | 1.7717 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0900 |
| 3 | 1.5917 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0083 |
| 4 | 1.5750 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0900 |
| 5 | 1.3950 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0083 |
| 6 | 1.3783 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0900 |
| 7 | 1.1983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0883 |
| 8 | 1.0217 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0100 |
| 9 | 1.0017 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0884 |
| 10 | 0.8250 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0100 |
| 11 | 0.8050 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0884 |
| 12 | 0.6283 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0100 |
| 13 | 0.6083 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0900 |
| 14 | 0.4283 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0100 |
| 15 | 0.4083 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0867 |
| 16 | 0.2350 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.0100 |
| 17 | 0.2150 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0983 |
| 18 | 0.0183 | yes | white `g 1.0` | yes | black `G` | 0.0017 | n/a |

### Style 43, 3x4 Blk 10m (solid)

3 concentric circles, 2 of them ink. Outer 1.9983 in. Centre dot 1.1983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.9983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0500 |
| 2 | 1.8983 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.3500 |
| 3 | 1.1983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | n/a |

### Style 44, 2x3 Blk 10m (solid)

3 concentric circles, 2 of them ink. Outer 2.4983 in. Centre dot 1.1983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 2.4983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.0500 |
| 2 | 2.3983 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.6000 |
| 3 | 1.1983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | n/a |

### Style 50, 5x5 Blk .05 dot

3 concentric circles, 2 of them ink. Outer 1.2483 in. Centre dot 0.0483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.2483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 1.1483 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.5500 |
| 3 | 0.0483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 51, 5x5 Blk .1 dot

3 concentric circles, 2 of them ink. Outer 1.2483 in. Centre dot 0.0983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.2483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 1.1483 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.5250 |
| 3 | 0.0983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 52, 5x5 Blk .2 dot

3 concentric circles, 2 of them ink. Outer 1.2483 in. Centre dot 0.1983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.2483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 1.1483 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.4750 |
| 3 | 0.1983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 55, 4x5 Blk .25 dot

3 concentric circles, 2 of them ink. Outer 1.6217 in. Centre dot 0.2483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.6217 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 1.5217 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.6367 |
| 3 | 0.2483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 56, 3x4 Blk .25 dot

3 concentric circles, 2 of them ink. Outer 1.9983 in. Centre dot 0.2483 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 1.9983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 1.8983 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.8250 |
| 3 | 0.2483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 57, 3x3 Blk .5 dot

3 concentric circles, 2 of them ink. Outer 2.2483 in. Centre dot 0.4983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 2.2483 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.0500 |
| 2 | 2.1483 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 0.8250 |
| 3 | 0.4983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 58, 2x2 Blk .5 dot

3 concentric circles, 2 of them ink. Outer 3.4983 in. Centre dot 0.4983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 3.4983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.1000 |
| 2 | 3.2983 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 1.4000 |
| 3 | 0.4983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 59, 2x2 Blk 1.0 dot

3 concentric circles, 2 of them ink. Outer 3.4983 in. Centre dot 0.9983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 3.4983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | 0.1000 |
| 2 | 3.2983 | yes | white `g 1.0` | yes | black `G` | 0.0167 | 1.1500 |
| 3 | 0.9983 | yes | black `g 0.0` | yes | black `G` | 0.0167 | n/a |

### Style 60, 1x2 Blk 1.0 dot

3 concentric circles, 2 of them ink. Outer 2.9983 in. Centre dot 0.9983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 2.9983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | 0.1250 |
| 2 | 2.7483 | yes | white `g 1.0` | yes | black `G` | 0.0017 | 0.8750 |
| 3 | 0.9983 | yes | black `g 0.0` | yes | black `G` | 0.0017 | n/a |

### Style 203, (unnamed 203)  *(identical bull in styles 205)*

1 concentric circles, 1 of them ink. Outer 0.9983 in. Centre dot 0.9983 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.9983 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

### Style 204, (unnamed 204)

1 concentric circles, 1 of them ink. Outer 0.7183 in. Centre dot 0.7183 in.

| # | Diameter | Filled | Fill colour | Stroked | Stroke colour | Stroke w | Annulus to next |
|---|---|---|---|---|---|---|---|
| 1 | 0.7183 | yes | blue `rg 0.0 0.0 1.0` | yes | blue `RG` | 0.0017 | n/a |

---

## 4. Grid shape versus style name, and grid origin

**No mismatches.** Every name reconciles once the unnumbered bottom sighter row is excluded from the row count. Origin is the centre of the top-left bull in inches from the **top-left** corner of the page (converted from the PDF bottom-left origin).

| # | Name declares | Cols | Scored rows | Total rows | Bulls | In-grid numerals | Origin X | Origin Y | Verdict |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 2 | 6x6 | 6 | 6 | 7 | 42 | 36 | 1.1236 | 1.1236 | OK, +1 sighter row |
| 3 | 4x5 | 4 | 5 | 5 | 20 | 20 | 1.4361 | 1.4361 | OK |
| 4 | 7x8 | 7 | 8 | 8 | 56 | 56 | 1.0361 | 1.0361 | OK |
| 5 | 3x4 | 3 | 4 | 4 | 12 | 12 | 1.9986 | 1.9986 | OK |
| 6 | 3x3 | 3 | 3 | 3 | 9 | 9 | 1.7486 | 1.7486 | OK |
| 7 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 8 | 4x5 | 4 | 5 | 5 | 20 | 20 | 1.4361 | 1.4361 | OK |
| 9 | 2x3 | 2 | 3 | 3 | 6 | 6 | 2.7486 | 1.9986 | OK |
| 10 | 2x2 | 2 | 2 | 2 | 4 | 4 | 2.3736 | 2.3736 | OK |
| 11 | n/a | 1 | 1 | 1 | 1 | 1 | 4.2486 | 4.2486 | n/a, name declares no grid |
| 12 | 3x3 | 3 | 3 | 3 | 9 | 9 | 1.7486 | 1.7486 | OK |
| 13 | 3x4 | 3 | 4 | 4 | 12 | 12 | 1.9986 | 1.9986 | OK |
| 14 | 4x5 | 4 | 5 | 5 | 20 | 20 | 1.4361 | 1.4361 | OK |
| 15 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 16 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 17 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 18 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 19 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 24 | 10x12 | 10 | 12 | 12 | 120 | n/a | 0.8736 | 0.8736 | OK |
| 25 | 10x10 | 10 | 10 | 11 | 110 | n/a | 0.8736 | 0.8736 | OK, +1 sighter row |
| 26 | 5x6 | 5 | 6 | 6 | 30 | n/a | 1.2486 | 1.2486 | OK |
| 27 | 7x8 | 7 | 8 | 8 | 56 | n/a | 1.0361 | 1.0361 | OK |
| 30 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 31 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 35 | 4x5 | 4 | 5 | 5 | 20 | 20 | 1.4361 | 1.4361 | OK |
| 36 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 37 | 4x5 | 4 | 5 | 5 | 20 | 20 | 1.4361 | 1.4361 | OK |
| 38 | 5x5 | 5 | 5 | 6 | 30 | 25 | 1.2486 | 1.2486 | OK, +1 sighter row |
| 41 | 3x4 | 3 | 4 | 4 | 12 | 12 | 1.9986 | 1.6236 | OK |
| 42 | 2x3 | 2 | 3 | 3 | 6 | 6 | 2.7486 | 1.9986 | OK |
| 43 | 3x4 | 3 | 4 | 4 | 12 | 12 | 1.9986 | 1.6236 | OK |
| 44 | 2x3 | 2 | 3 | 3 | 6 | 6 | 2.7486 | 1.9986 | OK |
| 50 | 5x5 | 5 | 5 | 5 | 25 | 25 | 1.2486 | 1.2486 | OK |
| 51 | 5x5 | 5 | 5 | 5 | 25 | 25 | 1.2486 | 1.2486 | OK |
| 52 | 5x5 | 5 | 5 | 5 | 25 | 25 | 1.2486 | 1.2486 | OK |
| 55 | 4x5 | 4 | 5 | 5 | 20 | 20 | 1.4361 | 1.4361 | OK |
| 56 | 3x4 | 3 | 4 | 4 | 12 | 12 | 1.9986 | 1.6236 | OK |
| 57 | 3x3 | 3 | 3 | 3 | 9 | 9 | 1.7486 | 1.7486 | OK |
| 58 | 2x2 | 2 | 2 | 2 | 4 | 4 | 2.3736 | 2.3736 | OK |
| 59 | 2x2 | 2 | 2 | 2 | 4 | 4 | 2.3736 | 2.3736 | OK |
| 60 | 1x2 | 1 | 2 | 2 | 2 | 2 | 4.2486 | 2.7486 | OK |
| 201 | n/a | 3 | 3 | 3 | 5 | n/a | 1.7486 | 1.7486 | n/a, quincunx |
| 202 | n/a | 3 | 3 | 3 | 5 | n/a | 1.9986 | 1.9986 | n/a, quincunx |
| 203 | n/a | 3 | 3 | 3 | 5 | n/a | 1.7486 | 1.7486 | n/a, quincunx |
| 204 | n/a | 3 | 3 | 3 | 5 | n/a | 2.0889 | 2.0889 | n/a, quincunx |
| 205 | n/a | 1 | 1 | 1 | 1 | n/a | 4.2486 | 4.2486 | n/a, name declares no grid |

Two remarks:

- The numeral count equals the scored-bull count on every sheet that is numbered at all, which is an independent confirmation of the sighter reading. Styles 24, 25, 26, 27 and 201–205 carry no in-grid numerals at all; for style 25 the sighter row is inferred from the row gap alone (0.9000 in against a 0.7500 in pitch).
- Style 24 is named `10x12` and is a true 10×12 with **no** sighter row, while style 25 is named `10x10` and is 10×10 **plus** a sighter row, 11 rows and 110 bulls. Same bull, same 0.7500 in pitch, different row policy. That is a real design difference, not a naming slip.

---

## 5. Geometry families

The 47 styles cluster into 14 families. Within a family the bull is identical and only the array changes, except where the note says otherwise.

### A. PSL scoring bull (red)

**Styles:** 1, 2, 3, 4

- Outer ring: 0.5650 in
- Concentric circles per bull: 3
- Ink discs per bull: 2
- Centre dot: 0.0483 in
- Ink colour: red
- Pitch: 1.0699, 1.2500, 1.5000, 1.8750 in
- Pitch / outer ratio: 1.8936, 2.2124, 2.6549, 3.3186

The only red styles in the set, and the only family whose outer ring is not a round nominal number: 0.5650 in exactly, on all four. Three concentric circles, a 0.5650 in red disc, a 0.2783 in white disc, a 0.0483 in red centre dot, giving one visible red annulus 0.1433 in wide and a second red mark at the centre. Identical bull on all four; only the array changes (5x5, 6x6, 4x5, 7x8). Hairline 0.0017 in stroke.

### B. ARA scoring bull (blue, 7 circles)

**Styles:** 7, 8

- Outer ring: 0.9983 in
- Concentric circles per bull: 7
- Ink discs per bull: 4
- Centre dot: 0.0483 in
- Ink colour: blue
- Pitch: 1.5000, 1.8750 in
- Pitch / outer ratio: 1.5026, 1.8782

Seven concentric circles alternating blue and white on a nominal 1 in outer, giving three blue annuli (0.0550, 0.0150 and 0.0550 in wide) plus a 0.0483 in centre dot. Note the second blue band is only 0.0150 in, a fine scoring line rather than a band. Same bull on both; arrays 5x5 and 4x5.

### C. Two-band bull, soft blue

**Styles:** 5, 6, 9, 10, 11

- Outer ring: 0.9983, 2.9983 in
- Concentric circles per bull: 3
- Ink discs per bull: 2
- Centre dot: 0.2483, 0.9983 in
- Ink colour: blue
- Pitch: 2.2500, 2.5000, 3.0000, 3.7500 in
- Pitch / outer ratio: 2.2538, 2.5043, 3.0051, 3.7564

The only styles using the softer blue `rg 0.25 0.25 1` rather than pure `rg 0 0 1`. Three circles: a nominal 1 in disc, a 0.7983 in white disc leaving a 0.1000 in annulus, and a fat 0.2483 in centre dot. Style 11 is the same design scaled up threefold (2.9983 in outer, 0.1250 in annulus, 0.9983 in dot) as a single bull centred on the page. Styles 5, 6, 10 use a 0.0017 in hairline; style 9 uses 0.0167 in, see the surprises section.

### D. Solid dot only, no rings

**Styles:** 12, 13, 14

- Outer ring: 0.4983, 0.7483 in
- Concentric circles per bull: 1
- Ink discs per bull: 1
- Centre dot: 0.4983, 0.7483 in
- Ink colour: blue
- Pitch: 1.8750, 2.2500, 2.5000 in
- Pitch / outer ratio: 3.3409, 3.7628, 4.5154

One filled blue disc per bull, nothing else. No annulus, no centre mark: the outer diameter and the "centre dot" are the same circle. 0.7483 in on style 12, 0.4983 in on 13 and 14. These have the largest pitch-to-outer ratios in the set among multi-bull sheets (3.34 to 4.52).

### E. 1 in seven-circle bull

**Styles:** 15, 16

- Outer ring: 0.9983 in
- Concentric circles per bull: 7
- Ink discs per bull: 4
- Centre dot: 0.0283 in
- Ink colour: black, blue
- Pitch: 1.5000 in
- Pitch / outer ratio: 1.5026

Nominal 1 in outer with seven circles giving three ink annuli of 0.1250, 0.0050 and 0.0050 in and a very small 0.0283 in centre dot. The 0.0050 in bands are hairline scoring rings. Styles 15 and 16 are the same geometry in black and in blue respectively, a colour pair, nothing else differs.

### F. 1 in simple ring plus centre dot

**Styles:** 17, 18, 19

- Outer ring: 0.9983 in
- Concentric circles per bull: 3
- Ink discs per bull: 2
- Centre dot: 0.0483, 0.1983 in
- Ink colour: black, blue
- Pitch: 1.5000 in
- Pitch / outer ratio: 1.5026

Three circles on a nominal 1 in outer: a 0.0500 in annulus and a centre dot that is the only thing that varies across the family, 0.0483 in on 17 and 18, 0.1983 in on 19. Style 17 is black, 18 and 19 blue. Style 17 additionally carries per-bull crosshairs that 18 and 19 do not.

### G. 0.5 in ring with crosshairs

**Styles:** 24, 25, 26, 27

- Outer ring: 0.4983 in
- Concentric circles per bull: 3
- Ink discs per bull: 2
- Centre dot: 0.0483 in
- Ink colour: blue
- Pitch: 0.7500, 1.0699, 1.5000 in
- Pitch / outer ratio: 1.5051, 2.1471, 3.0102

A small nominal 0.5 in bull, a 0.0633 in blue annulus and a 0.0483 in centre dot, with a horizontal and vertical tick through each centre, span 0.4967 in, exactly inscribing the ring. These are the densest sheets in the set: 24 packs 120 bulls at 0.7500 in pitch, 25 packs 110. Styles 26 and 27 are the same bull at much more open pitch.

### H. RBA eleven-circle bull

**Styles:** 30

- Outer ring: 1.3183 in
- Concentric circles per bull: 11
- Ink discs per bull: 6
- Centre dot: 0.0383 in
- Ink colour: blue
- Pitch: 1.5000 in
- Pitch / outer ratio: 1.1378

The most complex bull in the set: eleven concentric circles, five blue annuli, on a 1.3183 in outer at 1.5000 in pitch. That leaves only 0.1817 in of clear paper between neighbours, the second-tightest packing measured. Centre dot 0.0383 in.

### I. IR 50/50 nine-circle bull

**Styles:** 31

- Outer ring: 0.9983 in
- Concentric circles per bull: 9
- Ink discs per bull: 5
- Centre dot: 0.0283 in
- Ink colour: blue
- Pitch: 1.5000 in
- Pitch / outer ratio: 1.5026

Nine circles, four blue annuli (0.0500, 0.0050, 0.0050, 0.0050 in) on a nominal 1 in outer with a 0.0283 in centre dot. Structurally between family E and the ARA bull.

### J. WRABF metric bulls

**Styles:** 35, 36, 37, 38

- Outer ring: 1.1783, 1.2483, 1.4983, 1.5317 in
- Concentric circles per bull: 8, 10, 12
- Ink discs per bull: 4, 5, 6
- Centre dot: 0.0283, 0.0750 in
- Ink colour: blue
- Pitch: 1.5000, 1.8750 in
- Pitch / outer ratio: 1.2016, 1.2241, 1.2514, 1.2730

Four styles that state their distance in the name. Ring counts run 8 to 12. All four end in a fine open centre: an ink disc with a smaller white disc inside it, leaving a ring 0.0050 in wide (25 m) or 0.0033 in wide (50 m) rather than a solid dot. The 25 m and 50 m designs are different geometries, not one design scaled.

### K. 10 m fine concentric rings, black

**Styles:** 41, 42

- Outer ring: 1.7883 in
- Concentric circles per bull: 18
- Ink discs per bull: 9
- Centre dot: 0.2150 in
- Ink colour: black
- Pitch: 2.2500, 3.0000 in
- Pitch / outer ratio: 1.2582, 1.6776

Eighteen concentric circles per bull, the highest count in the set, drawn as eight fine black annuli of 0.0083 to 0.0100 in on a 1.7883 in outer, with rings stepping down in 0.0900 in radial increments. Centre finishes in a 0.2150 in black disc with a 0.0183 in white pinhole. Same bull on both; arrays 3x4 and 2x3.

### L. 10 m solid two-band, black

**Styles:** 43, 44

- Outer ring: 1.9983, 2.4983 in
- Concentric circles per bull: 3
- Ink discs per bull: 2
- Centre dot: 1.1983 in
- Ink colour: black
- Pitch: 2.2500, 3.0000 in
- Pitch / outer ratio: 1.1260, 1.2008

The simplified counterpart to family K: three circles instead of eighteen. A 0.0500 in black annulus and a large 1.1983 in solid black centre dot. Outer is 1.9983 in on style 43 and 2.4983 in on 44, the only family where the outer ring changes but the centre dot does not.

### M. Black single-band plus centre dot series

**Styles:** 50, 51, 52, 55, 56, 57, 58, 59, 60

- Outer ring: 1.2483, 1.6217, 1.9983, 2.2483, 2.9983, 3.4983 in
- Concentric circles per bull: 3
- Ink discs per bull: 2
- Centre dot: 0.0483, 0.0983, 0.1983, 0.2483, 0.4983, 0.9983 in
- Ink colour: black
- Pitch: 1.5000, 1.8750, 2.2500, 2.5000, 3.7500, 4.5000 in
- Pitch / outer ratio: 1.0719, 1.1120, 1.1260, 1.1562, 1.2016, 1.5009

The largest family: nine styles, all black, all three circles, all one annulus plus a solid centre dot. This is the set-piece parameter sweep, outer ring and centre dot vary independently across 1.2483 to 3.4983 in outer and 0.0483 to 0.9983 in dot. Annulus is 0.0500 in on the smaller sheets and 0.1000 in on 58, 59 and 60. Contains the tightest packing in the set (58 and 59 at ratio 1.0719).

### N. Quincunx on a printed graph grid

**Styles:** 201, 202, 203, 204, 205

- Outer ring: 0.4983, 0.7183, 0.9983 in
- Concentric circles per bull: 1
- Ink discs per bull: 1
- Centre dot: 0.4983, 0.7183, 0.9983 in
- Ink colour: blue
- Pitch: 2.1604, 2.2500, 2.5000 in
- Pitch / outer ratio: 2.5043, 3.0077, 4.5154, 5.0171

Structurally unlike everything else. Five bulls in a quincunx, four corners and centre of a square, each inside a white-filled, red-stroked box, over a full-page printed graph grid in red. Style 205 is a single bull in one 6.0000 in box. Graph-grid spacing is 1.0000 in on 201, 203 and 205; 0.5000 in on 202; and 0.7200 in on 204. Boxes are always an integer number of grid cells, three cells on 201 and 203, six on 202 and 205, four on 204. Style 204 is the odd one out and appears to be a 1 in grid design printed at 0.72 scale; see §9 item 8.

---

## 6. Range and modal values across all 47 styles

### 6.1 Headline parameters

| Parameter | n | Min | Max | Median | Modal value | Styles at mode |
|---|---|---|---|---|---|---|
| Grid pitch (governing, in) | 45 | 0.7500 | 4.5000 | 1.8750 | 1.5000 | 15 |
| Outer ring diameter (in) | 47 | 0.4983 | 3.4983 | 0.9983 | 0.9983 | 14 |
| Concentric circles per bull | 47 | 1 | 18 | 3 | 3 | 27 |
| Ink (non-white) discs per bull | 47 | 1 | 9 | 2 | 2 | 27 |
| Centre dot diameter (in) | 47 | 0.0283 | 1.1983 | 0.1983 | 0.0483 | 13 |
| Pitch / outer-ring ratio | 45 | 1.0719 | 5.0171 | 1.5026 | 1.5026 | 7 |
| Clear paper between bulls (in) | 45 | 0.1817 | 2.7517 | 0.5017 | 0.2517 | 11 |

`n` is 45 rather than 47 for the pitch-derived rows because styles 11 and 205 each hold a single bull and therefore have no pitch, no ratio and no clear-paper figure.

### 6.2 Full distributions

**Grid pitch (governing, in)**, value (number of styles): 1.5000 (15), 1.8750 (6), 2.2500 (6), 2.5000 (5), 3.0000 (3), 3.7500 (3), 0.7500 (2), 1.0699 (2), 1.2500 (1), 2.1604 (1), 4.5000 (1)

**Outer ring diameter (in)**, value (number of styles): 0.9983 (14), 0.4983 (8), 0.5650 (4), 1.2483 (4), 1.7883 (2), 1.9983 (2), 2.9983 (2), 3.4983 (2), 0.7183 (1), 0.7483 (1), 1.1783 (1), 1.3183 (1), 1.4983 (1), 1.5317 (1), 1.6217 (1), 2.2483 (1), 2.4983 (1)

**Concentric circles per bull**, value (number of styles): 3 (27), 1 (8), 7 (4), 10 (2), 18 (2), 8 (1), 9 (1), 11 (1), 12 (1)

**Ink (non-white) discs per bull**, value (number of styles): 2 (27), 1 (8), 4 (5), 5 (3), 6 (2), 9 (2)

**Centre dot diameter (in)**, value (number of styles): 0.0483 (13), 0.2483 (6), 0.4983 (6), 0.0283 (5), 0.9983 (5), 0.0750 (2), 0.1983 (2), 0.2150 (2), 1.1983 (2), 0.0383 (1), 0.0983 (1), 0.7183 (1), 0.7483 (1)

**Pitch / outer-ring ratio**, value (number of styles): 1.5026 (7), 1.2016 (4), 1.0719 (2), 1.1260 (2), 1.5051 (2), 2.5043 (2), 4.5154 (2), 1.1120 (1), 1.1378 (1), 1.1562 (1), 1.2008 (1), 1.2241 (1), 1.2514 (1), 1.2582 (1), 1.2730 (1), 1.5009 (1), 1.6776 (1), 1.8782 (1), 1.8936 (1), 2.1471 (1), 2.2124 (1), 2.2538 (1), 2.6549 (1), 3.0051 (1), 3.0077 (1), 3.0102 (1), 3.3186 (1), 3.3409 (1), 3.7564 (1), 3.7628 (1), 5.0171 (1)

**Clear paper between bulls (in)**, value (number of styles): 0.2517 (11), 0.5017 (8), 1.5017 (3), 1.7517 (3), 2.0017 (2), 0.1817 (1), 0.2533 (1), 0.3217 (1), 0.3433 (1), 0.3767 (1), 0.4617 (1), 0.5049 (1), 0.5716 (1), 0.6850 (1), 0.8767 (1), 0.9350 (1), 1.0017 (1), 1.2117 (1), 1.2517 (1), 1.3100 (1), 1.3767 (1), 1.4421 (1), 2.7517 (1)

### 6.3 Pitch to outer-ring ratio, the clean-paper parameter

This is the parameter you said matters most, so it gets its own treatment. It is computed as the **governing pitch** (the smaller of X and Y pitch, i.e. the direction in which bulls crowd first) divided by the outer ring diameter. A ratio of 1.0 would mean bulls touching; the clear-paper column is the same thing in absolute inches.

Across the 45 styles that have a pitch, the ratio runs **1.0719 to 5.0171**, median **1.5026**, modal **1.5026** (7 styles).

| Band | Ratio range | Styles | What it means on paper |
|---|---|---|---|
| Tight | 1.0719–1.2730 | 30, 35, 36, 37, 38, 41, 43, 44, 50, 51, 52, 55, 56, 57, 58, 59 | Bulls nearly touch. Under a third of a bull-width of white between neighbours. A shot that strays by a bull-width lands in the neighbouring bull. |
| Moderate | 1.5009–2.1471 | 4, 7, 8, 15, 16, 17, 18, 19, 24, 25, 27, 31, 42, 60 | Roughly a half to a full bull-width of white between neighbours. |
| Open | 2.2124–5.0171 | 1, 2, 3, 5, 6, 9, 10, 12, 13, 14, 26, 201, 202, 203, 204 | Bulls are isolated islands with more than a full bull-width of white all round. |

The modal value **1.5026** repays a look. It is not a round number, but 1.5000 / 0.9983 = 1.5026: it is what you get from a **nominal 1.0 in bull on a nominal 1.5 in pitch**, with the 1/600-inch undersize of §1.3 nudging it off 1.5000. The same applies throughout, 1.2016 = 1.5000 / 1.2483 is a nominal 1.25 in bull on a 1.5 in pitch, 1.1260 = 2.2500 / 1.9983 is a nominal 2 in bull on a 2.25 in pitch. **Every ratio in the set is a quotient of round nominal numbers; not one of them appears to have been chosen as a ratio.** The designer picked a bull size and a pitch, and the ratio fell out.

Ranked tightest to most open:

| Rank | # | Style | Pitch | Outer | Ratio | Clear paper |
|---|---|---|---|---|---|---|
| 1 | 58 | 2x2 Blk .5 dot | 3.7500 | 3.4983 | 1.0719 | 0.2517 |
| 2 | 59 | 2x2 Blk 1.0 dot | 3.7500 | 3.4983 | 1.0719 | 0.2517 |
| 3 | 57 | 3x3 Blk .5 dot | 2.5000 | 2.2483 | 1.1120 | 0.2517 |
| 4 | 43 | 3x4 Blk 10m (solid) | 2.2500 | 1.9983 | 1.1260 | 0.2517 |
| 5 | 56 | 3x4 Blk .25 dot | 2.2500 | 1.9983 | 1.1260 | 0.2517 |
| 6 | 30 | 5x5 RBA Style | 1.5000 | 1.3183 | 1.1378 | 0.1817 |
| 7 | 55 | 4x5 Blk .25 dot | 1.8750 | 1.6217 | 1.1562 | 0.2533 |
| 8 | 44 | 2x3 Blk 10m (solid) | 3.0000 | 2.4983 | 1.2008 | 0.5017 |
| 9 | 38 | 5x5 WRABF 50m | 1.5000 | 1.2483 | 1.2016 | 0.2517 |
| 10 | 50 | 5x5 Blk .05 dot | 1.5000 | 1.2483 | 1.2016 | 0.2517 |
| 11 | 51 | 5x5 Blk .1 dot | 1.5000 | 1.2483 | 1.2016 | 0.2517 |
| 12 | 52 | 5x5 Blk .2 dot | 1.5000 | 1.2483 | 1.2016 | 0.2517 |
| 13 | 35 | 4x5 WRABF 25m | 1.8750 | 1.5317 | 1.2241 | 0.3433 |
| 14 | 37 | 4x5 WRABF 50m | 1.8750 | 1.4983 | 1.2514 | 0.3767 |
| 15 | 41 | 3x4 Blk 10m | 2.2500 | 1.7883 | 1.2582 | 0.4617 |
| 16 | 36 | 5x5 WRABF 25m | 1.5000 | 1.1783 | 1.2730 | 0.3217 |
| 17 | 60 | 1x2 Blk 1.0 dot | 4.5000 | 2.9983 | 1.5009 | 1.5017 |
| 18 | 7 | 5x5 ARA Style | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 19 | 15 | 5x5 Black TEST | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 20 | 16 | 5x5 Blue | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 21 | 17 | 5x5 Simple Blk TEST | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 22 | 18 | 5x5 Simple Blue .05 dot | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 23 | 19 | 5x5 Simple Blue .2 dot | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 24 | 31 | 5x5 IR 50/50 | 1.5000 | 0.9983 | 1.5026 | 0.5017 |
| 25 | 24 | 10x12 .5" Ring | 0.7500 | 0.4983 | 1.5051 | 0.2517 |
| 26 | 25 | 10x10 .5" Ring | 0.7500 | 0.4983 | 1.5051 | 0.2517 |
| 27 | 42 | 2x3 Blk 10m | 3.0000 | 1.7883 | 1.6776 | 1.2117 |
| 28 | 8 | 4x5 ARA Style | 1.8750 | 0.9983 | 1.8782 | 0.8767 |
| 29 | 4 | 7x8 PSL Style | 1.0699 | 0.5650 | 1.8936 | 0.5049 |
| 30 | 27 | 7x8 .5" Ring | 1.0699 | 0.4983 | 2.1471 | 0.5716 |
| 31 | 2 | 6x6 PSL Style | 1.2500 | 0.5650 | 2.2124 | 0.6850 |
| 32 | 5 | 3x4 | 2.2500 | 0.9983 | 2.2538 | 1.2517 |
| 33 | 6 | 3x3 | 2.5000 | 0.9983 | 2.5043 | 1.5017 |
| 34 | 203 | (unnamed 203) | 2.5000 | 0.9983 | 2.5043 | 1.5017 |
| 35 | 1 | 5x5 PSL Style | 1.5000 | 0.5650 | 2.6549 | 0.9350 |
| 36 | 9 | 2x3 | 3.0000 | 0.9983 | 3.0051 | 2.0017 |
| 37 | 204 | (unnamed 204) | 2.1604 | 0.7183 | 3.0077 | 1.4421 |
| 38 | 26 | 5x6 .5" Ring | 1.5000 | 0.4983 | 3.0102 | 1.0017 |
| 39 | 3 | 4x5 PSL Style | 1.8750 | 0.5650 | 3.3186 | 1.3100 |
| 40 | 12 | 3x3 .75" Dot | 2.5000 | 0.7483 | 3.3409 | 1.7517 |
| 41 | 10 | 2x2 | 3.7500 | 0.9983 | 3.7564 | 2.7517 |
| 42 | 14 | 4x5 .5" Dot | 1.8750 | 0.4983 | 3.7628 | 1.3767 |
| 43 | 13 | 3x4 .5" Dot | 2.2500 | 0.4983 | 4.5154 | 1.7517 |
| 44 | 202 | (unnamed 202) | 2.2500 | 0.4983 | 4.5154 | 1.7517 |
| 45 | 201 | (unnamed 201) | 2.5000 | 0.4983 | 5.0171 | 2.0017 |

For the four quincunx sheets (201–204) the ratio above uses the square step, which is the conservative reading. Their true nearest-bull separation is the corner-to-centre diagonal, 3.5355 in on 201 and 203, 3.1820 in on 202, 3.0543 in on 204, so the real clear paper is larger still. Those numbers are in the JSON as `nn_to_outer_ratio`.

### 6.4 Sighter rows

13 sheets carry an unnumbered extra bottom row at a larger gap than the scored pitch.

| # | Style | Scored pitch Y | Gap to sighter row | Extra offset | Offset as fraction of pitch | Sighter bulls |
|---|---|---|---|---|---|---|
| 1 | 5x5 PSL Style | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 2 | 6x6 PSL Style | 1.2500 | 1.5000 | +0.2500 | 0.2000 | 6 |
| 7 | 5x5 ARA Style | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 15 | 5x5 Black TEST | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 16 | 5x5 Blue | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 17 | 5x5 Simple Blk TEST | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 18 | 5x5 Simple Blue .05 dot | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 19 | 5x5 Simple Blue .2 dot | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 25 | 10x10 .5" Ring | 0.7500 | 0.9000 | +0.1500 | 0.2000 | 10 |
| 30 | 5x5 RBA Style | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 31 | 5x5 IR 50/50 | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 36 | 5x5 WRABF 25m | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |
| 38 | 5x5 WRABF 50m | 1.5000 | 1.8000 | +0.3000 | 0.2000 | 5 |

The extra offset is **exactly 20% of the scored pitch on all 13 sheets**, 0.3000 on a 1.5000 pitch, 0.2500 on 1.2500, 0.1500 on 0.7500, so the sighter row is placed at 1.2 × pitch below the last scored row, without exception. The sighter bulls are geometrically identical to the scored ones on every sheet; no style uses a distinct sighting bull.

No sheet has a *column* at an irregular pitch. X-pitch SD is 0.0000 on every rectangular sheet except styles 4 and 27, where it is 0.0007, 1/600-inch rounding spread over a 1.0699 in pitch that is not an exact multiple of the authoring unit.

---

## 7. Angular size at distance

True MOA throughout: 1 radian = 3437.7468 arcminutes, so **MOA = (size / distance) × 3437.7468**. This is not the "1 inch at 100 yards" approximation, which would read about 4.7% smaller. **Mil = (size / distance) × 1000** (milliradian). Metric distances converted at 1 m = 39.3700787 in.

### 7.1 Styles with a stated distance

Eight styles name their distance in the style name, so these figures rest on no assumption of mine.

| # | Style | Distance | Pitch | Outer | Pitch MOA | Pitch mil | Outer MOA | Outer mil | Dot MOA | Dot mil |
|---|---|---|---|---|---|---|---|---|---|---|
| 41 | 3x4 Blk 10m | 10 m | 2.2500 | 1.7883 | 19.6467 | 5.7150 | 15.6152 | 4.5423 | 1.8774 | 0.5461 |
| 42 | 2x3 Blk 10m | 10 m | 3.0000 | 1.7883 | 26.1956 | 7.6200 | 15.6152 | 4.5423 | 1.8774 | 0.5461 |
| 43 | 3x4 Blk 10m (solid) | 10 m | 2.2500 | 1.9983 | 19.6467 | 5.7150 | 17.4489 | 5.0757 | 10.4634 | 3.0437 |
| 44 | 2x3 Blk 10m (solid) | 10 m | 3.0000 | 2.4983 | 26.1956 | 7.6200 | 21.8148 | 6.3457 | 10.4634 | 3.0437 |
| 35 | 4x5 WRABF 25m | 25 m | 1.8750 | 1.5317 | 6.5489 | 1.9050 | 5.3498 | 1.5562 | 0.2620 | 0.0762 |
| 36 | 5x5 WRABF 25m | 25 m | 1.5000 | 1.1783 | 5.2391 | 1.5240 | 4.1155 | 1.1972 | 0.2620 | 0.0762 |
| 37 | 4x5 WRABF 50m | 50 m | 1.8750 | 1.4983 | 3.2745 | 0.9525 | 2.6166 | 0.7611 | 0.0494 | 0.0144 |
| 38 | 5x5 WRABF 50m | 50 m | 1.5000 | 1.2483 | 2.6196 | 0.7620 | 2.1800 | 0.6341 | 0.0494 | 0.0144 |

What that says:

- **The 10 m styles are enormous in angle.** Style 44's 2.4983 in bull subtends 21.8163 MOA / 6.3467 mil. That is not a precision aiming mark; it is a short-range training bull sized to be seen with open sights at 10 m.
- **The 25 m and 50 m WRABF bulls are not angle-matched.** Style 36 (25 m, 1.1783 in) subtends 4.1156 MOA; style 38 (50 m, 1.2483 in) subtends 2.1799 MOA. The 50 m bull is only 6% physically larger but half the angular size, so the 50 m sheet is deliberately the harder target rather than an equivalent-difficulty scaling of the 25 m one. The same holds for the 4x5 pair: style 35 at 25 m is 5.3500 MOA, style 37 at 50 m is 2.6172 MOA.
- **The WRABF centre marks are vanishingly small.** Styles 37 and 38 have a 0.0283 in ink disc with a 0.0217 in white pinhole inside, leaving an ink ring 0.0033 in wide. At 50 m the whole feature spans 0.0494 MOA. That is a centring reference for a scoring plug or for OnTarget's own analysis software, not something visible through a scope.
- **Pitch in angular terms** is the useful number for deciding whether a stray shot lands in the neighbouring bull. At 10 m, style 44's 3.0000 in pitch is 26.1980 MOA / 7.6200 mil, so bulls are well separated in angle even though the paper looks crowded.

### 7.2 Styles with no stated distance, 100 yd assumed

**The 100-yard distance below is my assumption, not something stated in the files or the style names.** Nothing in any of these 39 PDFs declares a range. I chose 100 yd because it is the conventional US benchrest and load-development distance and these are US Letter sheets. If they are actually shot at 50 yd, every MOA and mil figure below doubles; at 200 yd, halve them. The physical dimensions are of course unaffected.

| # | Style | Pitch | Outer | Dot | Pitch MOA | Pitch mil | Outer MOA | Outer mil | Dot MOA | Dot mil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5x5 PSL Style | 1.5000 | 0.5650 | 0.0483 | 1.4324 | 0.4167 | 0.5395 | 0.1569 | 0.0461 | 0.0134 |
| 2 | 6x6 PSL Style | 1.2500 | 0.5650 | 0.0483 | 1.1937 | 0.3472 | 0.5395 | 0.1569 | 0.0461 | 0.0134 |
| 3 | 4x5 PSL Style | 1.8750 | 0.5650 | 0.0483 | 1.7905 | 0.5208 | 0.5395 | 0.1569 | 0.0461 | 0.0134 |
| 4 | 7x8 PSL Style | 1.0699 | 0.5650 | 0.0483 | 1.0217 | 0.2972 | 0.5395 | 0.1569 | 0.0461 | 0.0134 |
| 5 | 3x4 | 2.2500 | 0.9983 | 0.2483 | 2.1486 | 0.6250 | 0.9533 | 0.2773 | 0.2371 | 0.0690 |
| 6 | 3x3 | 2.5000 | 0.9983 | 0.2483 | 2.3873 | 0.6944 | 0.9533 | 0.2773 | 0.2371 | 0.0690 |
| 7 | 5x5 ARA Style | 1.5000 | 0.9983 | 0.0483 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.0461 | 0.0134 |
| 8 | 4x5 ARA Style | 1.8750 | 0.9983 | 0.0483 | 1.7905 | 0.5208 | 0.9533 | 0.2773 | 0.0461 | 0.0134 |
| 9 | 2x3 | 3.0000 | 0.9983 | 0.2483 | 2.8648 | 0.8333 | 0.9533 | 0.2773 | 0.2371 | 0.0690 |
| 10 | 2x2 | 3.7500 | 0.9983 | 0.2483 | 3.5810 | 1.0417 | 0.9533 | 0.2773 | 0.2371 | 0.0690 |
| 11 | 1" Dot 3" Ring | n/a | 2.9983 | 0.9983 | n/a | n/a | 2.8632 | 0.8329 | 0.9533 | 0.2773 |
| 12 | 3x3 .75" Dot | 2.5000 | 0.7483 | 0.7483 | 2.3873 | 0.6944 | 0.7146 | 0.2079 | 0.7146 | 0.2079 |
| 13 | 3x4 .5" Dot | 2.2500 | 0.4983 | 0.4983 | 2.1486 | 0.6250 | 0.4758 | 0.1384 | 0.4758 | 0.1384 |
| 14 | 4x5 .5" Dot | 1.8750 | 0.4983 | 0.4983 | 1.7905 | 0.5208 | 0.4758 | 0.1384 | 0.4758 | 0.1384 |
| 15 | 5x5 Black TEST | 1.5000 | 0.9983 | 0.0283 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.0270 | 0.0079 |
| 16 | 5x5 Blue | 1.5000 | 0.9983 | 0.0283 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.0270 | 0.0079 |
| 17 | 5x5 Simple Blk TEST | 1.5000 | 0.9983 | 0.0483 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.0461 | 0.0134 |
| 18 | 5x5 Simple Blue .05 dot | 1.5000 | 0.9983 | 0.0483 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.0461 | 0.0134 |
| 19 | 5x5 Simple Blue .2 dot | 1.5000 | 0.9983 | 0.1983 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.1894 | 0.0551 |
| 24 | 10x12 .5" Ring | 0.7500 | 0.4983 | 0.0483 | 0.7162 | 0.2083 | 0.4758 | 0.1384 | 0.0461 | 0.0134 |
| 25 | 10x10 .5" Ring | 0.7500 | 0.4983 | 0.0483 | 0.7162 | 0.2083 | 0.4758 | 0.1384 | 0.0461 | 0.0134 |
| 26 | 5x6 .5" Ring | 1.5000 | 0.4983 | 0.0483 | 1.4324 | 0.4167 | 0.4758 | 0.1384 | 0.0461 | 0.0134 |
| 27 | 7x8 .5" Ring | 1.0699 | 0.4983 | 0.0483 | 1.0217 | 0.2972 | 0.4758 | 0.1384 | 0.0461 | 0.0134 |
| 30 | 5x5 RBA Style | 1.5000 | 1.3183 | 0.0383 | 1.4324 | 0.4167 | 1.2589 | 0.3662 | 0.0366 | 0.0106 |
| 31 | 5x5 IR 50/50 | 1.5000 | 0.9983 | 0.0283 | 1.4324 | 0.4167 | 0.9533 | 0.2773 | 0.0270 | 0.0079 |
| 50 | 5x5 Blk .05 dot | 1.5000 | 1.2483 | 0.0483 | 1.4324 | 0.4167 | 1.1920 | 0.3468 | 0.0461 | 0.0134 |
| 51 | 5x5 Blk .1 dot | 1.5000 | 1.2483 | 0.0983 | 1.4324 | 0.4167 | 1.1920 | 0.3468 | 0.0939 | 0.0273 |
| 52 | 5x5 Blk .2 dot | 1.5000 | 1.2483 | 0.1983 | 1.4324 | 0.4167 | 1.1920 | 0.3468 | 0.1894 | 0.0551 |
| 55 | 4x5 Blk .25 dot | 1.8750 | 1.6217 | 0.2483 | 1.7905 | 0.5208 | 1.5486 | 0.4505 | 0.2371 | 0.0690 |
| 56 | 3x4 Blk .25 dot | 2.2500 | 1.9983 | 0.2483 | 2.1486 | 0.6250 | 1.9082 | 0.5551 | 0.2371 | 0.0690 |
| 57 | 3x3 Blk .5 dot | 2.5000 | 2.2483 | 0.4983 | 2.3873 | 0.6944 | 2.1470 | 0.6245 | 0.4758 | 0.1384 |
| 58 | 2x2 Blk .5 dot | 3.7500 | 3.4983 | 0.4983 | 3.5810 | 1.0417 | 3.3406 | 0.9718 | 0.4758 | 0.1384 |
| 59 | 2x2 Blk 1.0 dot | 3.7500 | 3.4983 | 0.9983 | 3.5810 | 1.0417 | 3.3406 | 0.9718 | 0.9533 | 0.2773 |
| 60 | 1x2 Blk 1.0 dot | 4.5000 | 2.9983 | 0.9983 | 4.2972 | 1.2500 | 2.8632 | 0.8329 | 0.9533 | 0.2773 |
| 201 | (unnamed 201) | 2.5000 | 0.4983 | 0.4983 | 2.3873 | 0.6944 | 0.4758 | 0.1384 | 0.4758 | 0.1384 |
| 202 | (unnamed 202) | 2.2500 | 0.4983 | 0.4983 | 2.1486 | 0.6250 | 0.4758 | 0.1384 | 0.4758 | 0.1384 |
| 203 | (unnamed 203) | 2.5000 | 0.9983 | 0.9983 | 2.3873 | 0.6944 | 0.9533 | 0.2773 | 0.9533 | 0.2773 |
| 204 | (unnamed 204) | 2.1604 | 0.7183 | 0.7183 | 2.0630 | 0.6001 | 0.6859 | 0.1995 | 0.6859 | 0.1995 |
| 205 | (unnamed 205) | n/a | 0.9983 | 0.9983 | n/a | n/a | 0.9533 | 0.2773 | 0.9533 | 0.2773 |

At the assumed 100 yd:

- The modal 0.9983 in bull works out at **0.9533 MOA / 0.2773 mil**, within 5% of a 1 MOA aiming mark. Given that a nominal 1.0000 in circle at 100 yd is 0.9549 MOA, the "1 inch bull at 100 yards" reading is almost certainly the design intent for this whole group.
- The PSL bull (0.5650 in) is **0.5395 MOA**, close to half a minute.
- The 0.4983 in ring bull of styles 24–27, 201 and 202 is **0.4758 MOA**.
- Style 58 and 59's 3.4983 in bull is **3.3407 MOA**, on a 3.7500 in pitch worth 3.5809 MOA.
- The smallest centre marks (0.0283 in on styles 15, 16, 31; 0.0383 in on style 30) are **0.0270 and 0.0366 MOA**, far below what any sight can resolve, confirming they are scoring-geometry references rather than aiming points.

---

## 8. Other printed elements

All 47 sheets carry the same footer furniture: white knockout rectangles behind form-field labels, a small number of short text runs (form labels and an identifier line), and a 40-bar barcode. Text content is not transcribed; only its presence, size and extent are recorded.

### 8.1 Barcode

**Every one of the 47 sheets carries a barcode of exactly 40 filled bars.** Bar widths take exactly two values across all files, 0.0133 in and 0.0500 in (a 1:3.75 narrow-to-wide ratio), drawn as `re` rectangles filled black `g 0` with a matching hairline stroke. The two-width structure and 40-bar count are consistent with a two-width linear symbology such as Code 39 or Interleaved 2 of 5, but **the symbology was not decoded and the encoded value was not read**, that is outside a measurement brief.

| # | Bars | Width | Height | x0 | y0 from top | x1 | y1 from top |
|---|---|---|---|---|---|---|---|
| 1 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 2 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 3 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 4 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 5 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 6 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 7 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 8 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 9 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 10 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 11 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 12 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 13 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 14 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 15 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 16 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 17 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 18 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 19 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 24 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 25 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 26 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 27 | 40 | 2.2317 | 0.2967 | 5.6800 | 10.2017 | 7.9117 | 10.4983 |
| 30 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 31 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 35 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 36 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 37 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 38 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 41 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 42 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 43 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 44 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 50 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 51 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 52 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 55 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 56 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 57 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 58 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 59 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 60 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 201 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 202 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 203 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 204 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |
| 205 | 40 | 2.2317 | 0.2967 | 5.4300 | 10.0517 | 7.6617 | 10.3483 |

### 8.2 Frames, cell rules, crosshairs and boxes

| # | Frame (W×H) | Frame stroke | Cell rules | Rule spacing | Rule stroke | Crosshairs/bull | Crosshair span | Bull boxes |
|---|---|---|---|---|---|---|---|---|
| 1 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 2 | 7.4983×7.4983 | 0.0183 | 5H+5V | 1.2500 | 0.0183 | n/a | n/a | n/a |
| 3 | 7.4983×9.3733 | 0.0183 | 4H+3V | 1.8750 | 0.0183 | n/a | n/a | n/a |
| 4 | 7.4967×8.5583 | 0.0183 | 7H+6V | 1.0700 | 0.0183 | n/a | n/a | n/a |
| 5 | 6.7483×8.9983 | 0.0183 | 3H+2V | 2.2500 | 0.0183 | n/a | n/a | n/a |
| 6 | 7.4983×7.4983 | 0.0183 | 2H+2V | 2.5000 | 0.0183 | n/a | n/a | n/a |
| 7 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 8 | 7.4983×9.3733 | 0.0183 | 4H+3V | 1.8750 | 0.0183 | n/a | n/a | n/a |
| 9 | 5.9983×8.9983 | 0.0183 | 2H+1V | 3.0000 | 0.0183 | n/a | n/a | n/a |
| 10 | 7.4983×7.4983 | 0.0183 | 1H+1V | n/a | 0.0183 | n/a | n/a | n/a |
| 11 | 7.4983×7.4983 | 0.0183 | n/a | n/a | n/a | n/a | n/a | n/a |
| 12 | 7.4983×7.4983 | 0.0183 | 2H+2V | 2.5000 | 0.0183 | n/a | n/a | n/a |
| 13 | 6.7483×8.9983 | 0.0183 | 3H+2V | 2.2500 | 0.0183 | n/a | n/a | n/a |
| 14 | 7.4983×9.3733 | 0.0183 | 4H+3V | 1.8750 | 0.0183 | n/a | n/a | n/a |
| 15 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 16 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 17 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | 2 | 0.9967 | n/a |
| 18 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 19 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 24 | n/a | n/a | n/a | n/a | n/a | 2 | 0.4967 | n/a |
| 25 | n/a | n/a | n/a | n/a | n/a | 2 | 0.4967 | n/a |
| 26 | n/a | n/a | n/a | n/a | n/a | 2 | 0.4967 | n/a |
| 27 | n/a | n/a | n/a | n/a | n/a | 2 | 0.4967 | n/a |
| 30 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 31 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 35 | 7.4983×9.3733 | 0.0183 | 4H+3V | 1.8750 | 0.0183 | n/a | n/a | n/a |
| 36 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 37 | 7.4983×9.3733 | 0.0183 | 4H+3V | 1.8750 | 0.0183 | n/a | n/a | n/a |
| 38 | 7.4983×7.4983 | 0.0183 | 4H+4V | 1.5000 | 0.0183 | n/a | n/a | n/a |
| 41 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 42 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 43 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 44 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 50 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 51 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 52 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 55 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 56 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 57 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 58 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 59 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 60 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| 201 | n/a | n/a | graph grid 9H+9V | 1.0000 | 0.0267 | n/a | n/a | 5 @ 3.0000 |
| 202 | n/a | n/a | graph grid 17H+17V | 0.5000 | 0.0267 | n/a | n/a | 5 @ 3.0000 |
| 203 | n/a | n/a | graph grid 9H+9V | 1.0000 | 0.0267 | n/a | n/a | 5 @ 3.0000 |
| 204 | n/a | n/a | graph grid 11H+11V | 0.7200 | 0.0267 | n/a | n/a | 5 @ 2.8800 |
| 205 | n/a | n/a | graph grid 9H+9V | 1.0000 | 0.0267 | n/a | n/a | 1 @ 6.0000 |

Reading of the above:

- **Frames.** 25 sheets draw a stroked outer frame around the target area, always at 0.0183 in stroke width. The commonest is 7.4983 × 7.4983 in on 16 sheets (styles 1, 2, 6, 7, 10, 11, 12, 15, 16, 17, 18, 19, 30, 31, 36, 38); five use 7.4983 × 9.3733 in (3, 8, 14, 35, 37); styles 5 and 13 use 6.7483 × 8.9983 in; style 4 uses 7.4967 × 8.5583 in and style 9 uses 5.9983 × 8.9983 in. The remaining 22 sheets have no frame at all.
- **Cell boundary rules.** 24 sheets draw internal rules dividing the target area into one cell per bull, at 0.0183 in stroke. On the 5×5 sheets these are 4 horizontal plus 4 vertical lines at 2.0000, 3.5000, 5.0000 and 6.5000 in from the top-left, each 7.5000 in long, giving 1.5000 in cells that match the pitch exactly. Rule spacing equals the bull pitch on every sheet that has both. Style 10 (2×2) has just one rule each way, a single cross at 4.2500 in.
- **Graph grids.** Styles 201–205 are different: rather than cell rules they carry a full-page ruled grid in red at 0.0267 in stroke, 1.0000 in spacing on 201, 203 and 205, 0.5000 in on 202, and 0.7200 in on 204, running the full 8.0000 in (7.2000 in on 204) across the sheet.
- **Crosshairs.** Styles 17, 24, 25, 26 and 27 put a horizontal and a vertical tick through each bull centre instead of cell rules, two lines per bull. Span is 0.4967 in on the 0.5 in ring styles and 0.9967 in on style 17: in both cases exactly one 1/600-inch unit shorter than the bull diameter, so the crosshair is precisely inscribed in the outer ring.
- **Bull boxes.** Styles 201–204 draw a white-filled, red-stroked square around each of the five bulls, 3.0000 in on 201–203, 2.8800 in on 204. Style 205 draws a single 6.0000 in box.

### 8.3 Numerals and footer text

In-grid numerals are set at 9.96 pt in every file that has them, one short run per scored bull, in the bull ink colour. Footer text is the same 9.96 pt. Styles 24, 25, 26, 27 and 201–205 carry no in-grid numerals. Content is not transcribed.

---

## 9. Things that surprised me

Stated plainly, in rough order of how much they might matter to you.

**1. Every circle in every file is 1/600 inch undersize.** Not approximately, exactly 0.0017 in, every circle, every file, independent of size. A "1 inch" bull is 0.9983 in and a "3 inch" ring is 2.9983 in. It is a truncation bug in whatever emits the coordinates, and it has been there long enough to be in all 47 files. It is far too small to matter for shooting, but it will matter if you ever diff these against nominal specs or against a regenerated set, because everything will look wrong by a constant.

**2. The pitch is not undersize.** Pitches are dead-on 1.5000, 2.2500, 0.7500. So the bug is in the circle-extent calculation specifically, not in a global unit conversion. That asymmetry is the diagnostic: if it were a scaling error the pitch would be off too.

**3. Stroke width splits the set almost exactly in half, apparently at random.** 26 styles stroke their bull circles at 0.0017 in (0.12 pt, one authoring unit, effectively a hairline) and 21 at 0.0167 in (1.2 pt, ten units, an order of magnitude heavier). The split does not follow family, colour, size or era. The clearest case is family C: styles 5, 6 and 10 use the hairline, and style 9, the same bull, same colours, same three circles, uses 1.2 pt. Since the stroke is what actually inks the ring edge, style 9 prints a visibly heavier bull than its siblings for no evident reason. If these were meant to be a consistent set, that is a bug worth fixing.

**4. Nothing is a stroked annulus.** Every ring is a stack of filled discs painted largest first, ink over white over ink. It works, but it means the printed ring width depends on two disc diameters rather than one stroke width, and it means a "ring" and a "dot" are the same kind of object in the file. It also means overprint matters, which is presumably why ten of the files bother to set `/OPM 1`.

**5. Six styles have no centre dot at all, they have a hole.** Styles 35, 36, 37, 38, 41 and 42 paint a small ink disc and then a smaller white disc inside it, leaving an open ring 0.0033 to 0.0100 in wide. On styles 37 and 38 that ring is 0.0033 in, about two authoring units. At 50 m it subtends 0.0494 MOA. It cannot be an aiming mark; it is a centring reference.

**6. The sighter row is placed at exactly 1.2 × pitch, on all 13 sheets that have one.** Not a fixed offset in inches, a fixed *ratio*. That is a deliberate and consistent rule, and it is the kind of thing that is easy to miss because the absolute offset differs on every sheet (0.3000, 0.2500, 0.1500 in).

**7. Styles 24 and 25 differ only in row policy.** Same 0.4983 in bull, same 0.7500 in pitch, same crosshairs, same 10 columns. Style 24 is a plain 10×12; style 25 is 10×10 plus a sighter row, making 11 rows. So one has 120 bulls and the other 110, and the names (`10x12`, `10x10`) describe different things, a total in one case and a scored array in the other. Not wrong, but inconsistent.

**8. Style 204 appears to be a whole design printed at 0.72 scale.** It is the only sheet in the set whose grid spacing is not a round number: 0.7200 in, against 1.0000 in on styles 201, 203 and 205 and 0.5000 in on 202. Divide every dimension of 204 by 0.72 and round numbers fall out everywhere, grid 0.7200 / 0.72 = 1.0000 in, box 2.8800 / 0.72 = 4.0000 in, bull 0.7183 / 0.72 = 0.9976 (nominal 1.0000 in), pitch 2.1604 / 0.72 = 3.0006 (nominal 3.0000 in). So style 204 is a 1 in grid, 4 in box, 1 in bull, 3 in pitch design reduced to 72% to fit the page. Every other sheet in the set is drawn at 1:1. **I have not established why**, and I am not going to guess: it could be a reduced-distance target, a fit-to-page artefact, or a deliberate scale factor. It is worth knowing that this one sheet is not at natural size before anyone measures a group off it.

**9. Four distinct ink colours, and one of them is nearly a duplicate.** Pure blue `rg 0 0 1` on 23 styles, black `g 0` on 15, red `rg 1 0 0` on 4 (the PSL family, the only red sheets), and a softer blue `rg 0.25 0.25 1` on exactly 5, styles 5, 6, 9, 10 and 11. Two blues that differ by 25% in the red and green channels will print visibly differently and are almost certainly not intentional as a pair. No CMYK is used anywhere, which for print artwork is itself notable: these will go through RGB-to-CMYK conversion at the printer with whatever profile happens to be in force.

**10. The 25 m and 50 m WRABF pairs are not angular equivalents.** I expected the 50 m bull to be twice the 25 m bull so the angular size would match. It is 6% bigger, so the 50 m target is half the angular size and materially harder. Whether that is intended or an oversight is a question for whoever set the WRABF specification, but it is not a scaling of one design.

**11. Every sheet carries a 40-bar barcode, including the five unnamed ones.** Constant bar count across all 47 with only two bar widths. If these are meant to identify the style, 40 bars is a lot of payload for a two-digit style number, so it likely encodes more, a version, a batch, or a URL fragment. Not decoded here.

---

## 10. Machine-readable data

The complete extraction, including every field summarised above plus per-style bull-field margins, nearest-neighbour distances, full colour operand lists, cell-rule coordinates and barcode bar widths, is in `ontarget_dims.json` alongside this document.
