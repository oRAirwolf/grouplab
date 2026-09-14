# GroupLab Built-in Target Library

**Version** 1.1 (draft)
**Expressed in** the format specified in TARGET-SCHEMA.md
**Uses** the fiducial scheme decided in FIDUCIAL-DECISION.md
**Cross-checked against** the dimensional survey in ONTARGET-DIMENSIONS.md
**Status** Specification for review. No application code written.

Every sheet below was placed and checked by geometry validators written for this document, which centre the grid, enforce margins, position the codes and the fiducial lattice, and then check every pair of printed elements for overlap. **All twenty sheets pass with zero errors**, and the only two warnings in the library are the narrow marker band on two zeroing sheets, described in section 10. The validators are delivered alongside this document as `tools/layout/layout.py` for the multi-bull sheets and `tools/layout/zero.py` for the zeroing sheets, and the placement they produced is the geometry in sections 4 and 5.

---

## 1. What ships, and why these

Twenty sheets, in six families.

| Family | Sheets | Purpose |
|---|---|---|
| Centrefire load development | 5 | The core case. Multi-bull composite grouping at 100 yards or 100 metres |
| Rimfire | 3 | 50 yards and 50 metres, tighter geometry |
| Large format | 3 | Tabloid and A3, 100 to 200 yards |
| Long range, tiled | 2 | 300 yards, assembled from ordinary sheets. Two assembly presets each |
| Long range, roll media | 3 | 300 yards on a plotter, at 24, 36 and 42 inch roll widths |
| Zeroing | 4 | MOA and mil, at 100 yards and 100 metres |

Every multi-bull layout carries **at least 25 scoring bulls**, per the requirement, with two documented exceptions. The zeroing sheets carry one aiming mark each, for the reason section 5 gives. The tiled 300 yard target carries 24 in its default 2 by 2 assembly and 36 in its 3 by 2, and section 4.4 makes the case for defaulting one short.

Four design rules govern all of them.

**Licence CC0-1.0.** Nobody should have to think about whether they may print, modify or redistribute a target. The built-in library is public domain.

**Original geometry.** These are not derived from OnTarget's designs and share none of their dimensions, ring counts or numbering. The 47 shipped OnTarget sheets measured in ONTARGET-DIMENSIONS.md were used to understand what real targets look like and how they behave when shot; the geometry below was derived from the angular reasoning in section 2, not copied. Section 8 reports where the two independently agree, which is a validation of the reasoning rather than a borrowing. DESIGN.md section 3 and START-HERE.md both exclude OnTarget compatibility and this library respects that.

**Metric and imperial as first-class equivalents.** Letter and A4 versions of the same design are separate definitions with their own identifiers, not one design squeezed onto two pages. A4 is the default outside North America, and a library that only handles Letter is quietly North America only.

**Every sheet is self-describing.** Codes, fiducials and a printed identifier on all twenty. There is no secondary-mode sheet in the built-in library.

---

## 2. The sizing principle: cells are angular, not linear

This is the single design decision everything else follows from, and it is not the obvious one.

A shot lands near its own bull. How near, in inches, scales with distance. So a cell that comfortably contains a shot at 100 yards is half as adequate at 200 and a sixth as adequate at 600. **The correct unit for cell pitch is angular, and the correct question for each layout is what distance it is for.**

**The criterion.** Nearest-bull assignment is safe while the half-pitch exceeds the radius within which shots actually fall. From the measurements: 0 to 3 shots per target landed outside their own 1.5 inch cell, by 0.09 to 0.27 inches, and the worst shot-to-own-bull distance observed was **1.02 inches at 100 yards, which is 0.97 MOA**. That worst case is from `338lmao.jpg`, a target SAMPLE-NOTES describes as having many shots high and left, so it mixes dispersion with a zero error and overstates dispersion alone.

A cleaner criterion from the model: a rifle with a Rayleigh sigma of 0.25 MOA puts 99 percent of its shots within `3.03 sigma = 0.76 MOA` of its centre. So **a half-pitch of about 0.75 MOA is the design target**, and it leaves a little room for imperfect zero.

Half-pitch in true MOA, by layout and distance:

| Layout | Pitch | 25 yd | 50 yd | 100 yd | 150 yd | 200 yd | 300 yd |
|---|---|---|---|---|---|---|---|
| GL-CF25 | 38.0 mm | 2.86 | 1.43 | **0.71** | 0.48 | 0.36 | 0.24 |
| GL-CF25-100M | 40.0 mm | 3.01 | 1.50 | **0.69** at 100 m | 0.50 | 0.38 | 0.25 |
| GL-CF30 | 35.0 mm | 2.63 | 1.32 | **0.66** | 0.44 | 0.33 | 0.22 |
| GL-RF25 | 25.4 mm | 1.91 | **0.95** | 0.48 | 0.32 | 0.24 | 0.16 |
| GL-RF36 | 25.4 mm | 1.91 | **0.95** | 0.48 | 0.32 | 0.24 | 0.16 |
| GL-LR25 | 50.8 mm | 3.82 | 1.91 | 0.95 | 0.64 | **0.48** | 0.32 |
| GL-LR30 | 50.8 mm | 3.82 | 1.91 | 0.95 | 0.64 | **0.48** | 0.32 |
| GL-LR300 | 101.6 mm | 7.64 | 3.82 | 1.91 | 1.27 | 0.95 | **0.64** |

Bold marks each layout's intended distance. Note that the centrefire and rimfire layouts land at 0.71 and 0.95 MOA respectively, which brackets the 0.75 target, and that this is why the rimfire pitch is 1.0 inch at 50 yards rather than 0.75: at 50 yards a 1.0 inch cell is *angularly larger* than a 1.5 inch cell at 100.

The measured cross-cell rate on the sample scans, which used exactly this 1.5 inch at 100 yards geometry, is the empirical confirmation: a handful of shots per target land outside, and DESIGN.md section 13's explicit assignment interface handles them. That is the intended behaviour, not a failure.

**Aiming ring subtension** follows the same logic. The outer ring should be large enough to aim at through a scope and small enough not to swallow the group:

| Layout | Ring OD | At its intended distance | MOA |
|---|---|---|---|
| GL-CF25 | 1.000 in | 100 yd | **0.96** |
| GL-CF30 | 0.874 in | 100 yd | **0.83** |
| GL-RF25 | 0.598 in | 50 yd | **1.14** |
| GL-RF36 | 0.500 in | 50 yd | **0.96** |
| GL-LR25 | 1.500 in | 200 yd | **0.72** |
| GL-LR30 | 1.402 in | 200 yd | **0.67** |
| GL-LR300-R24 | 2.500 in | 300 yd | **0.80** |
| GL-LR300-T | 1.260 in | 300 yd | **0.40** |

Every layout's outer ring subtends between 0.66 and 1.14 MOA at its intended distance, with one exception. **The 300 yard tile is capped by its page, not chosen.** On a Letter or A4 tile at a 4.0 inch pitch, the two top codes sit directly beside the outer bull columns, and the ring can only grow until the top row comes within the 3 mm clearance of a code. That limit is **32.0 mm, 1.26 inches**. Anything larger either collides with a code or pushes the third row off the page, which the validator now reports rather than tolerating. The roll-media sheets, which have no such constraint, use the 2.5 inch ring the angular argument asks for. A 0.40 MOA aiming mark is smaller than ideal but still 1.26 inches of solid black, which is a usable hold at 300 yards through a scope, and since every bull takes exactly one shot no hole ever obscures a later aiming point.

---

## 3. Common elements

Shared by every sheet, so sections 4 and 5 do not repeat them.

**Fiducials.** AprilTag `tag36h11`, 0.5 mm module, 4.0 mm marker, 1.0 mm quiet zone, 6.0 mm footprint, pure black, no anti-aliasing, no halftone screening. 8 by 8 modules including the mandatory border, 587 identifiers, minimum Hamming distance 11. Placement rule `grid-boundary-1` on the sheets from 25.4 to 50.8 mm pitch, `grid-boundary-half-1` on the 300 yard tiles, the 24 and 36 inch rolls and GL-CF25-100M-A4, `field-ring-1` on the zeroing sheets. TARGET-SCHEMA.md section 3.7 defines all three.

**QR codes.** Version 10 at error correction level H, 0.4 mm module, 22.8 mm symbol, 1.6 mm quiet zone, **26.0 mm footprint**. Four, one per page corner, except on the 300 yard tiles, which carry two in the top corners because the sheet is small and the assembly already holds twelve copies. All codes on a sheet carry the same complete payload, so any one survivor is sufficient.

The library standardises on one code size rather than fitting each sheet its own. Version 10 holds 119 bytes; the largest payload in the library is 85 bytes, so every sheet has at least 34 bytes of headroom, and one footprint means one set of layout constants. TARGET-SCHEMA.md section 5.3 has the measured payload sizes.

**Safe margin: 12.0 mm on all four edges**, and this number is measured rather than conventional. The author's scanner captures only 8.2633 by 10.7633 inches of an 8.5 by 11 sheet, cropping **0.237 inches of width and height inside the page**, which is roughly 3 mm per side if centred. A consumer inkjet's non-printable margin is another 3 mm or so. Twelve millimetres is four times the observed crop and covers both. **Nothing that matters is printed within 12 mm of the page edge**, which is why the codes sit where they do.

**Clearance.** 3.0 mm minimum between any two printed elements including their quiet zones. The validators enforce it.

**Ring construction: filled disc stacks, not stroked circles.** Each bull is a stack of concentric filled discs painted outermost first, alternating between the artwork ink and a paper knockout. The standard centrefire bull is five discs: 25.4, 23.8, 12.7, 11.5 and 2.5 mm. That draws a 0.8 mm outer annulus, a 0.6 mm mid annulus and a 2.5 mm centre dot, and every ink boundary in the printed sheet is the difference of two integers in the definition. TARGET-SCHEMA.md section 3.4 gives the full argument; the short version is that a stroked circle has three legitimate ink extents that differ by 0.0157 inches, which is sixteen times the Phase 0 registration gate, and a disc stack has one. The survey of 47 shipped OnTarget PDFs found the same: not one of them uses a stroked annulus.

**Why a dot rather than a cross or an open centre.** The measurements settle this. A filled dot is a compact, high-contrast feature that the bull-finding matched filter locates reliably, and, critically, the naive detector baselines correctly rejected every printed centre dot on size at every threshold setting: measured at 0.101 to 0.107 inches against a hole size gate of 0.15 inches and up. A dot is easy to find and impossible to confuse with a hole. A fine cross would be neither.

**Sighter rows sit at 1.2 times the grid pitch** below the last scoring row. The ratio is the modal value measured across the shipped sheets that have a sighter row, and it is a good number for an independent reason: at 0.2 pitch beyond a normal step it is unambiguously distinguishable from a scoring row by geometry alone, with no OCR and no label parsing, which makes a sighter row identifiable even from a partial scan. Adopting a measured constant for a functional reason is not copying a design. TARGET-SCHEMA.md section 7 makes it a validator warning with `cells.sighterGap` as the override. Three sheets depart from it, GL-CF25-LTR at 454 dmm and GL-LR300-R24 and GL-LR300-R36 at 1142 dmm, each shortened only as far as brings the sighter row inside the fiducial lattice, and each declares `cells.sighterGap`.

**Load-data block, where present**, is a 31.0 mm band across the bottom of the sheet holding nine fields in three rows plus a 28.0 mm square reserved for the instance code. Section 6 covers it.

**Human-readable identifier** printed as text in the bottom margin, per TARGET-SCHEMA.md section 6.

**Labels.** Sequential from 1, row-major, for scoring bulls. `S1` upward for sighters. No printed numeral is placed where it can be confused with a hole; the measurements confirm the naive detectors rejected all 25 printed numerals on size, at 0.143 to 0.144 inches, but the label is kept clear of the ring regardless.

---

## 4. The multi-bull layouts

Geometry as placed and validated. All coordinates in dmm, tenths of a millimetre, from the page top-left corner, per TARGET-SCHEMA.md.

| Layout | Page | Grid | Scoring | Sighters | Pitch | Ring OD | Codes | Markers | Block | Overhead |
|---|---|---|---|---|---|---|---|---|---|---|
| **GL-CF25-LTR** | Letter | 5 x 5 | 25 | 3 | 38.0 mm | 25.4 mm | 4 | 38 | no | 6.75 % |
| **GL-CF25-LTR-D** | Letter | 5 x 5 | 25 | 0 | 38.0 mm | 25.4 mm | 2 | 34 | yes | 4.27 % |
| **GL-CF25-A4** | A4 | 5 x 5 | 25 | 3 | 38.0 mm | 25.4 mm | 4 | 40 | no | 6.64 % |
| **GL-CF25-100M-A4** | A4 | 5 x 5 | 25 | 3 | 40.0 mm | 25.4 mm | 4 | 88 | no | 9.41 % |
| **GL-CF30-LTR** | Letter | 5 x 6 | 30 | 0 | 35.0 mm | 22.2 mm | 4 | 38 | no | 6.75 % |
| **GL-RF25-LTR** | Letter | 5 x 5 | 25 | 5 | 25.4 mm | 15.2 mm | 4 | 42 | yes | 6.99 % |
| **GL-RF25-A4** | A4 | 5 x 5 | 25 | 5 | 25.4 mm | 15.2 mm | 4 | 42 | yes | 6.76 % |
| **GL-RF36-LTR** | Letter | 6 x 6 | 36 | 4 | 25.4 mm | 12.7 mm | 4 | 54 | no | 7.71 % |
| **GL-LR25-TAB** | Tabloid | 5 x 5 | 25 | 3 | 50.8 mm | 38.1 mm | 4 | 46 | yes | 3.61 % |
| **GL-LR25-A3** | A3 | 5 x 5 | 25 | 3 | 50.8 mm | 38.1 mm | 4 | 46 | yes | 3.50 % |
| **GL-LR30-TAB** | Tabloid | 5 x 6 | 30 | 3 | 50.8 mm | 35.6 mm | 4 | 52 | no | 3.79 % |
| **GL-LR300-T** | Letter tile | 2 x 3 | 6 per tile | 0 | 101.6 mm | 32.0 mm | 2 | 9 | no | 2.78 % |
| **GL-LR300-TA4** | A4 tile | 2 x 3 | 6 per tile | 0 | 101.6 mm | 32.0 mm | 2 | 12 | no | 2.86 % |
| **GL-LR300-R24** | 24 in roll | 6 x 5 | 30 | 3 | 101.6 mm | 63.5 mm | 4 | 113 | yes | 1.56 % |
| **GL-LR300-R36** | 36 in roll | 9 x 4 | 36 | 3 | 101.6 mm | 63.5 mm | 4 | 151 | yes | 1.46 % |
| **GL-LR300-R42** | 42 in roll | 10 x 4 | 40 | 3 | 101.6 mm | 63.5 mm | 4 | 73 | yes | 0.82 % |

"Overhead" is the fraction of page area taken by fiducials and codes together.

### 4.1 Centrefire load development

**GL-CF25-LTR**, the reference layout. 100 yards, centrefire, with a sighter row.

```
page      letter, 2159 x 2794 dmm
columns x 320, 700, 1080, 1460, 1840          pitch 380 dmm = 38.0 mm = 1.496 in
rows    y 540, 920, 1300, 1680, 2060          pitch 380 dmm
sighters  y 2514, x 700, 1080, 1460           gap 454 dmm, shortened to bracket
discs     254 / 238 / 127 / 115 / 25 dmm
lattice x 130, 510, 890, 1270, 1650, 2030     all integer, see the even-pitch rule below
lattice y 350, 730, 1110, 1490, 1870, 2250, 2704
fiducials 38 markers, 4 candidates dropped for code and ring clearance
```

**GL-CF25-LTR-D**, the same design with the load block instead of the sighter row.

```
page      letter
columns x 320, 700, 1080, 1460, 1840          pitch 380 dmm
rows    y 612, 992, 1372, 1752, 2132
sighters  none
codes     2, top corners only
data block  x 120, y 2364, 1919 x 310 dmm, nine fields, 280 dmm code reserve
fiducials 34 markers, 2 candidates dropped
```

**These are two definitions rather than one with a switch, and the reason is arithmetic.** On Letter, after the safe margin, the bottom code band and 3 mm of clearance, the space below the last scoring row is **4.8 mm**. A usable nine-field load block needs 31 mm. They cannot both exist on a Letter sheet at this pitch, so the choice is made when you pick the sheet, not when you print it.

**Why this variant carries two codes rather than four.** The load block owns the bottom band, so the bottom pair of codes has nowhere to sit that is not either on top of the block or eating a row of bulls. Twenty-five bulls at a 38.0 mm pitch plus a 31 mm block plus four codes is **11 mm more than Letter has**, measured by the validator. Dropping to the top pair frees 29 mm and the sheet fits with 15 mm to spare. Rule R3 still holds: two replicated codes mean either one alone reconstructs the definition. The Tabloid, A3 and roll sheets have room for both and carry both. TARGET-SCHEMA.md section 3.10 explains why `blank` and `filled` are print-time modes of one definition while `none` is a different definition.

**The even-pitch rule.** Grid pitch is **380 dmm, not 381**, and that is deliberate. The `grid-boundary-1` fiducial lattice sits at cell boundaries, which are half a pitch from each bull centre. With an odd pitch the half-pitch is not a whole number of dmm, and TARGET-SCHEMA.md rule R1 requires every stored length to be an integer. So **a parametric layout using a derived fiducial scheme must have an even pitch in dmm**, and a pitch divisible by four under the half-pitch scheme. The validator asserts it.

The cost is 0.1 mm of design freedom and the loss of the round 1.500 inch figure. Neither matters. 38.0 mm is 1.4961 inches, which is in fact **closer to the 1.49845 inch pitch actually measured on the sample scans** than a nominal 1.500 would be, and since the definition is the truth and the renderer prints from it, the printed grid is exactly 38.0 mm whatever it is called.

**GL-CF25-A4** is the same design on A4. Note that it carries **40 markers against Letter's 38**. A4 is 2.3 mm narrower, which drops fewer lattice points at the corner codes than the taller page adds along its length. Both counts are far above the fifteen or so a radial distortion model wants, so the photograph path is unaffected either way.

```
page      a4, 2100 x 2970 dmm
columns x 290, 670, 1050, 1430, 1810          pitch 380 dmm
rows    y 627, 1007, 1387, 1767, 2147
sighters  y 2603, x 670, 1050, 1430
```

**GL-CF25-100M-A4**, a true 100 metre sheet rather than a 100 yard sheet printed in a metric country.

```
page      a4
columns x 250, 650, 1050, 1450, 1850          pitch 400 dmm = 40.0 mm = 1.575 in
rows    y 575, 975, 1375, 1775, 2175
sighters  y 2655, x 650, 1050, 1450
fiducials 88 markers, grid-boundary-half-1, 55 candidates dropped
```

**Why this sheet takes the half lattice.** Under `grid-boundary-1` the marker columns half a pitch outside the outer bull columns fall at x = 50 and 2050, inside half the safe margin, and both are dropped, which leaves the outer bull columns 20.0 mm outside the lattice. No sighter gap reaches a column. `grid-boundary-half-1` brackets them at a margin of zero, as on the 300 yard tiles, and the sighters already sit inside it at the conventional gap.

One hundred metres is 9.36 percent further than 100 yards, so everything subtends 9.36 percent less and the pitch should grow by the same factor: 38.0 mm becomes 41.6 mm. **A4 cannot hold it.** Five columns at 41.6 mm pitch plus a 25.4 mm ring span 191.8 mm against 186.0 mm of usable width. Forty millimetres is the largest even pitch that fits, and it gives a half-pitch of **0.69 MOA at 100 m** against the Letter sheet's 0.71 MOA at 100 yd. Three percent short of parity, and the page cannot do better without dropping a column or shrinking the ring. This is written down rather than rounded away because it is the kind of detail that later looks like a bug.

**GL-CF30-LTR**, thirty scoring bulls on Letter, for the shooter who wants a larger sample per sheet and will accept a slightly tighter cell.

```
page      letter
columns x 380, 730, 1080, 1430, 1780          pitch 350 dmm = 35.0 mm = 1.378 in
rows    y 522, 872, 1222, 1572, 1922, 2272
discs     222 / 208 / 111 / 101 / 25 dmm
sighters  none, the sixth row uses the space
```

Half-pitch is 0.66 MOA at 100 yards against the reference layout's 0.71, so cross-cell shots will be marginally more common. The pitch is 35.0 mm rather than the 35.6 mm an earlier draft used, because at 35.6 the sixth row sat 2.8 mm below where the bottom code band allows; the validator now catches that and did not before. That is a real trade and it is worth making, because thirty shots against twenty-five narrows the confidence interval on sigma from 0.834 to 1.249 down to 0.847 to 1.222 times the estimate, per STATISTICS.md section 9.1.

### 4.2 Rimfire

**GL-RF25-LTR** for 50 yards, **GL-RF25-A4** for 50 metres. Identical geometry on their respective pages, both with the load block.

```
GL-RF25-LTR   letter
columns x 572, 826, 1080, 1334, 1588          pitch 254 dmm = 25.4 mm = 1.000 in
rows    y 582, 836, 1090, 1344, 1598
sighters  y 1902, all five columns             gap 304 dmm = 1.2 x pitch
discs     152 / 142 / 76 / 68 / 20 dmm
data block  x 120, y 2364, 1919 x 310 dmm
fiducials 42 markers, none dropped

GL-RF25-A4    a4
columns x 542, 796, 1050, 1304, 1558
rows    y 670, 924, 1178, 1432, 1686
sighters  y 1990, all five columns
data block  x 120, y 2540, 1860 x 310 dmm
```

The tighter rimfire grid leaves room for both the sighter row and the load block, which is why these two carry both while the centrefire Letter sheet has to choose. Five sighters rather than three, because rimfire fouling and the first shots from a cold barrel matter more, and there is room.

**GL-RF36-LTR**, thirty-six scoring bulls at the same 1.0 inch pitch with smaller rings.

```
page      letter
columns x 444, 698, 952, 1206, 1460, 1714     pitch 254 dmm
rows    y 740, 994, 1248, 1502, 1756, 2010
sighters  y 2314, x 698, 952, 1206, 1460
discs     127 / 117 / 64 / 56 / 18 dmm
fiducials 54 markers
```

Thirty-six shots is where the statistics start to be genuinely informative: it takes the coefficient of variation on sigma to about 8.5 percent. Rimfire ammunition is cheap enough that this is a realistic session.

### 4.3 Large format, 100 to 200 yards

**GL-LR25-TAB** and **GL-LR25-A3**, twenty-five bulls at a 2.0 inch pitch, both with sighters and a load block.

```
GL-LR25-TAB   tabloid, 2794 x 4318 dmm
columns x 381, 889, 1397, 1905, 2413          pitch 508 dmm = 50.8 mm = 2.000 in
rows    y 814, 1322, 1830, 2338, 2846
sighters  y 3455, x 889, 1397, 1905            gap 609 dmm = 1.2 x pitch
discs     381 / 365 / 191 / 179 / 38 dmm
data block  x 120, y 3888, 2554 x 310 dmm
fiducials 46 markers, 2 candidates dropped

GL-LR25-A3    a3, 2970 x 4200 dmm
columns x 469, 977, 1485, 1993, 2501
rows    y 754, 1262, 1770, 2278, 2786
sighters  y 3395, x 977, 1485, 1993
data block  x 120, y 3770, 2730 x 310 dmm
```

**GL-LR30-TAB**, thirty scoring bulls on Tabloid.

```
page      tabloid
columns x 381, 889, 1397, 1905, 2413          pitch 508 dmm
rows    y 714, 1222, 1730, 2238, 2746, 3254
sighters  y 3863, x 889, 1397, 1905
discs     356 / 342 / 178 / 166 / 36 dmm
fiducials 52 markers
```

Overhead on the large formats is 3.6 to 3.8 percent against 6.5 to 7.8 percent on Letter, because the fiducial and code footprints are absolute while the page grew. Large format is simply more efficient, and if page area is ever a concern the answer is a bigger sheet rather than smaller markers.

### 4.4 Long range, tiled

**GL-LR300-T** on Letter and **GL-LR300-TA4** on A4. Each is one tile, six scoring bulls at a 4.0 inch pitch, printed as many times as the assembly needs.

**Two assembly presets ship, and 2 by 2 is the default.**

| Preset | Sheets | Scoring bulls | Assembled size, Letter | When |
|---|---|---|---|---|
| **2 x 2** | **4** | **24** | **17.0 x 22.0 in** | **Default.** One session's shooting, four sheets to tape and scan |
| 3 x 2 | 6 | 36 | 25.5 x 22.0 in | When the extra twelve shots are worth two more sheets |

The 2 by 2 default is one bull short of the 25-bull rule and that is a deliberate exception, made on the grounds that the difference between 24 and 36 shots is small where it matters and the difference between four sheets and six is not. From STATISTICS.md section 9.1, the 95 percent confidence interval on Rayleigh sigma runs **0.831 to 1.256** times the estimate at n=24 and **0.858 to 1.198** at n=36. Fifty percent more ammunition, taping and scanning buys about three percentage points on each end of the interval. At 300 yards, where each shot costs more and conditions drift over a longer string, that is not an obvious trade, so the library makes the cheaper one the default and leaves the other one click away.

Both presets are the same tile definition with a different `tiling` block, so switching costs nothing and the definition identifier tells them apart.

```
GL-LR300-T    letter, one tile of a tiled assembly
columns x 572, 1588                            pitch 1016 dmm = 101.6 mm = 4.000 in
rows    y 381, 1397, 2413
discs     320 / 312 / 160 / 154 / 32 dmm       1.260 in outer ring
codes     2, top corners only
fiducials 9 markers, grid-boundary-half-1, 26 candidates dropped
assembly  2 x 2 tiles, 17.0 x 22.0 in, 24 scoring bulls   (default)
          3 x 2 tiles, 25.5 x 22.0 in, 36 scoring bulls

GL-LR300-TA4  a4, one tile of a tiled assembly
columns x 542, 1558
rows    y 599, 1615, 2631
discs     320 / 312 / 160 / 154 / 32 dmm
fiducials 12 markers
assembly  2 x 2 tiles, 16.5 x 23.4 in, 24 scoring bulls   (default)
          3 x 2 tiles, 24.8 x 23.4 in, 36 scoring bulls
```

**Three things about tiling that are not obvious.**

*Tile alignment does not matter.* Every shot is measured relative to its own bull, and every bull sits entirely on one tile registered by that tile's own fiducials. No step in the chain uses the position of one tile relative to another, so tile alignment contributes exactly zero error to any group statistic. Tape, staples and an eyeballed edge are all equally good. What the validator does enforce is that no bull, cell, marker or code crosses a tile boundary, because a bull cut by a seam has no measurable centre. TARGET-SCHEMA.md section 3.12 has the full argument.

*Tiles are scanned separately and pooled.* Nobody has to fit a 25 by 22 inch assembled target onto a Letter scanner. Each sheet is its own scan and its own registration, and the shots from all six are pooled into one composite group at the end.

*All six tiles are one definition.* The body of the payload is byte-identical on every tile; only the `tileIndex` byte in the frame header differs. One definition identifier, one entry in the library, six printed sheets.

**Why the fiducial scheme changes here.** At a 101.6 mm pitch the `grid-boundary-1` lattice offers only twelve candidate positions on a 2 by 3 tile, and the 38.1 mm rings knock out all but **two** of them. Two markers is not a registration. Subdividing the lattice to half-pitch steps raises the candidates to 35 and leaves **nine** surviving markers, well spread across the sheet. This is the reason `grid-boundary-half-1` exists, and it is a case where a rule that works everywhere else degenerates quietly rather than failing loudly, which is exactly what the validator is for.

### 4.5 Long range, roll media

Three sheets, one per plotter roll width. The width is fixed by the media and the length is chosen so that one sheet is one session.

```
GL-LR300-R24   roll-24, 6096 x 7112 dmm            24.00 x 28.00 in
columns x 508, 1524, 2540, 3556, 4572, 5588        pitch 1016 dmm = 101.6 mm = 4.000 in
rows    y 928, 1944, 2960, 3976, 4992
sighters  y 6134, x 2032, 3048, 4064               gap 1142 dmm, shortened to bracket
discs     635 / 613 / 318 / 302 / 64 dmm           2.500 in outer ring
data block  x 120, y 6682, 5856 x 310 dmm
fiducials 113 markers, grid-boundary-half-1, 56 candidates dropped
                                                   30 scoring, overhead 1.56 percent

GL-LR300-R36   roll-36, 9144 x 6096 dmm            36.00 x 24.00 in
columns x 508 to 8636 in nine steps of 1016
rows    y 928, 1944, 2960, 3976
sighters  y 5118, x 3556, 4572, 5588               gap 1142 dmm, shortened to bracket
data block  x 120, y 5666, 8904 x 310 dmm
fiducials 151 markers, grid-boundary-half-1        36 scoring, overhead 1.46 percent

GL-LR300-R42   roll-42, 10668 x 6096 dmm           42.00 x 24.00 in
columns x 762 to 9906 in ten steps of 1016
rows    y 760, 1776, 2792, 3808
sighters  y 5027, x 4318, 5334, 6350
data block  x 120, y 5666, 10428 x 310 dmm
fiducials 73 markers                               40 scoring, overhead 0.82 percent
```

**Each sheet uses every column its roll width allows.** That is the rule, and it is why the three differ in shape rather than being scaled copies: 24 inches holds six columns at a 4.0 inch pitch, 36 holds nine, 42 holds ten. Rows are then chosen so the total lands in a plausible session: 30, 36 and 40 scoring bulls plus three sighters. Nobody shoots 70 rounds at 300 yards in one sitting, so filling the roll lengthwise would waste media rather than gather data.

**Overhead stays under two percent on all three**, against 4.3 to 7.7 percent on Letter, because the fiducial and code footprints are absolute while the page grows. 113 markers on the 24 inch sheet and 73 on the 42 inch one are far more than any registration needs; the surplus is what makes a torn or shot-out corner a non-event.

**Why the 24 and 36 inch sheets take the half lattice and a shorter sighter gap.** Under `grid-boundary-1` the marker columns outside their outer bull columns are dropped at the page edge, which leaves those bulls 50.8 mm outside the lattice, and no sighter gap reaches a column. `grid-boundary-half-1` brackets the columns at a margin of zero. At the conventional 1219 dmm gap the sighters still sit 50.8 mm below the half lattice, measured, so the gap is shortened to 1142 dmm, which brings them inside it. The 42 inch sheet brackets every bull under `grid-boundary-1` and is unchanged.

The `roll-24`, `roll-36` and `roll-42` page presets fix the width and leave the length to the design. TARGET-SCHEMA.md section 3.2 explains why a roll preset is not the same as a custom page: the width is a media constraint the user cannot change, which is what the generator needs to know before it offers to make a sheet longer.

**A note on how wide is too wide.** GL-LR300-R42 spans 11.5 MOA of windage at 300 yards, which is the widest sight picture in the library. That is a shooting ergonomics question rather than a measurement one, and section 7 works through why.

---

## 5. The zeroing sheets

Four sheets, one for each combination of adjustment unit and distance unit, because a scope turret is calibrated in MOA or in mil and a range is marked in yards or in metres, and the four combinations do not convert into one another by scaling a printed grid.

| Sheet | Unit | Minor cell | Major line | Range | Field | Markers |
|---|---|---|---|---|---|---|
| **GL-ZERO-MOA-100Y** | MOA at 100 yd | 0.5 MOA, 13.30 mm | 1 MOA | plus or minus 3.0 MOA | 159.6 mm | 24 |
| **GL-ZERO-MIL-100Y** | mil at 100 yd | 0.1 mil, 9.144 mm | 0.5 mil | plus or minus 0.8 mil | 146.4 mm | 16 |
| **GL-ZERO-MOA-100M** | MOA at 100 m | 0.5 MOA, 14.54 mm | 1 MOA | plus or minus 2.5 MOA | 145.4 mm | 24 |
| **GL-ZERO-MIL-100M** | mil at 100 m | 0.1 mil, 10.00 mm | 0.5 mil | plus or minus 0.8 mil | 160.0 mm | 12 |

All four are Letter, portrait, with four corner codes, a single central aiming mark and a six-field load block.

```
common    letter, 2159 x 2794 dmm
centre  x 1079, y 1277
aim mark  discs 127 / 114 / 25 dmm    12.7 mm ring, 2.5 mm centre dot
data block  x 120, y 2464, 1919 x 210 dmm, fields-3x2-1, standard-6,
            210 dmm reserved square holding the identifier and serial as text
fiducials field-ring-1, on the grid's major lines in the band around the field
codes     4, corners-1, centres (250,250) (1909,250) (250,2304) (1909,2304)

GL-ZERO-MOA-100Y   half 798 dmm, 6 divisions   offsets 0 133 266 399 532 665 798
GL-ZERO-MIL-100Y   half 732 dmm, 8 divisions   offsets 0 91 183 274 366 457 549 640 732
GL-ZERO-MOA-100M   half 727 dmm, 5 divisions   offsets 0 145 291 436 582 727
GL-ZERO-MIL-100M   half 800 dmm, 8 divisions   offsets 0 100 200 300 400 500 600 700 800
```

**These are the documented exception to the 25-bull rule, and the exception is the whole point.** A zeroing sheet answers a different question from a load-development sheet. Load development asks how much a rifle disperses, which needs many shots and therefore many bulls. Zeroing asks where the group centre is relative to the aiming point, which needs one aiming point and a printed ruler. Putting 25 bulls on a zeroing sheet would leave no room for the grid and would answer neither question well. The earlier draft of this library shipped a 25-bull sheet called GL-ZR25-LTR that was a load-development sheet with finer rings; it has been dropped, because it was a compromise nobody asked for.

**Line positions are integers, and getting there needed care.** The obvious construction is to store a cell pitch and step it. That fails rule R1 for three of the four sheets: 0.1 mil at 100 yards is 9.144 mm, and no integer number of tenths of a millimetre is that. Rounding the pitch to 91 dmm and stepping eight times puts the edge of the field 3.5 dmm from where it should be. **Each line is instead rounded from the stored half-extent**, `round(half * i / divisions)` with ties toward zero, which bounds the error at half a dmm anywhere in the field and stops it accumulating. It derives from the stored half rather than from the unrounded angle because the unrounded angle is not in the payload and cannot be recovered from it, so a rule that used it would produce sheets no decoder could reproduce. Measured across all four sheets the worst deviation is **0.48 dmm, which is 0.048 mm**. On GL-ZERO-MIL-100M it is exactly zero, because 0.1 mil at 100 metres is 10.0 mm on the nose, which is the one place in this entire library where the metric system pays for itself outright.

**The tie rule is why that figure is 0.48 and not 0.80.** On GL-ZERO-MIL-100Y the stored half is 732 dmm over 8 divisions, so four of the eight lines land on an exact half. Ties toward zero, which TARGET-SCHEMA.md section 2 now specifies for every derived boundary in the format, gives 91, 274, 457 and 640 and a worst deviation of 0.48 dmm. The ties-to-even default of both Python and C# would give 92 and 458, a worst deviation of 0.80 dmm, and would move the `field-ring-1` markers on the 0.5 mil lines by a dmm. The rule was unstated until the first implementation hit it, which is exactly the kind of thing a second implementation is for.

**Why the ranges differ between sheets.** The field has to leave a clear band on all four sides for the `field-ring-1` markers, at least 15 mm, and the widest field Letter allows is therefore about 162 mm. Within that, each sheet takes the largest whole number of its own unit that fits: 3.0 MOA at 100 yards, 0.8 mil at 100 yards, 2.5 MOA at 100 metres, 0.8 mil at 100 metres. These are not equal in angle, and they are not meant to be. Every one of them is more adjustment range than a zeroed scope should ever need; a scope that is 3 MOA out at 100 yards is not being confirmed, it is being zeroed from scratch, and that is what the sighter row on a load-development sheet is for.

**The grid is read by a human, not by the analyser.** The application measures the group centre offset from the aiming point using the fiducials and the definition, exactly as on any other sheet, and reports the correction in whatever unit the user asked for. The printed grid is there so a shooter standing at the bench without a phone can read the correction off the paper. Its accuracy therefore matters to the eye, not to the measurement, which is why 0.048 mm of line placement error is comfortably irrelevant.

---

## 6. The load-data block

The user chooses at print time between three modes.

| Mode | What is printed | Where the values come from |
|---|---|---|
| **none** | nothing | a sheet without a block, which is a different definition |
| **blank** | box, rules, field captions, definition identifier and serial as text | written in by hand at the range |
| **filled** | box, captions, typed values, and the instance code | typed before printing |

`blank` and `filled` are the same definition with the same geometry and the same identifier. Only the contents of the block differ. A user who prints blank, shoots, and types the load data in afterwards gets exactly the same analysis as a user who typed it first.

**The nine standard fields**, in the order they are printed: date, distance, cartridge, bullet, powder and charge, brass, primer, seating depth, notes. The zeroing sheets use a six-field variant, dropping bullet, brass and seating depth, because they have less vertical room and less to record.

**The block layout is derived, not hand-placed.** Three rows of 10.0 mm inside the 31.0 mm band, and three columns across the content width, which is the block width less the 28.0 mm code reserve and a 2.0 mm gap. On Letter that is 161.9 mm of content split into cells of 54.0, 53.9 and 54.0 mm. Every boundary is an integer and the rule is reproducible from four numbers in the file.

**The instance code** is a second QR inside the reserved square, version 11 at level Q, 24.4 mm symbol in a 27.6 mm footprint. It carries a GLTD-I payload with its own magic bytes so it can never be confused with a definition frame: the definition identifier, a four-character sheet serial, the print date and the field values. A realistic filled set of all nine fields measures 153 bytes against a capacity of 177. Level Q rather than H because this square sits in the part of the sheet people rest a hand on and write across, and because losing it costs a convenience rather than the definition. TARGET-SCHEMA.md section 3.11 has the frame layout.

**The zeroing sheets carry no instance code**, and this is the one place in the library where the block differs by more than its field count. Their block is 21.0 mm tall, because a taller one eats the clear band that `field-ring-1` needs for its marker rows, and a 27.6 mm code does not fit a 21.0 mm square. The square is still reserved and still carries the identifier and the serial as text in both modes; the load values live in the session rather than on the paper. Growing the block to make room was costed and rejected. A 27.6 mm code needs a 28.0 mm square, and a 28.0 mm square needs a block at least 28.0 mm tall, which takes the vertical band on GL-ZERO-MOA-100Y from 6.9 mm to 3.4 mm and on GL-ZERO-MIL-100M from 6.7 mm to 3.2 mm. At the 31.0 mm block the other sheets use, the bands are 1.9 mm and 1.7 mm and GL-ZERO-MOA-100Y loses four of its 24 markers. A zeroing sheet's analysis does not depend on load data in the first place, so the trade is registration quality against a convenience, and registration wins. TARGET-SCHEMA.md section 3.10 states the rule: below a 28.0 mm reserve, no instance code.

**The block is a declared detection exclusion zone.** Its rectangle is in the definition, so the pipeline knows before it looks at the scan that everything inside it is printed matter and handwriting rather than bullet holes. This is worth more than it sounds: handwriting is exactly the kind of small dark irregular blob that a naive detector reports as a shot.

---

## 7. The long-range problem, stated plainly

**The Tabloid and A3 layouts are not long-range layouts. They are 100 to 200 yard layouts on a bigger sheet, and calling them anything else would be dishonest.** Past 200 yards the answer is tiles or roll media, which is what sections 4.4 and 4.5 add.

The arithmetic. Take the design criterion from section 2, a half-pitch of about 0.75 MOA, and ask what pitch that needs at each distance and how many bulls then fit. Usable area after the 12 mm safe margin, the top and bottom code bands and their clearances is **10.06 by 13.77 inches on Tabloid**, **7.56 by 7.77 inches on Letter**, and **23.06 by 32.77 inches on a 24 inch roll**. Ring diameter is taken at two thirds of pitch throughout, which is what the library uses.

| Distance | Half-pitch needed | Pitch needed | Bulls on Tabloid | Bulls on Letter | Bulls on a 24 x 36 in roll |
|---|---|---|---|---|---|
| 100 yd | 0.79 in | 1.57 in | 6 x 9 = **54** | 5 x 5 = **25** | 15 x 21 = **315** |
| 150 yd | 1.18 in | 2.36 in | 4 x 6 = **24** | 3 x 3 = **9** | 10 x 14 = **140** |
| 200 yd | 1.57 in | 3.14 in | 3 x 4 = **12** | 2 x 2 = **4** | 7 x 10 = **70** |
| 300 yd | 2.36 in | 4.71 in | 2 x 3 = **6** | 1 x 1 = **1** | 5 x 7 = **35** |
| 600 yd | 4.71 in | 9.42 in | 1 x 1 = **1** | 1 x 1 = **1** | 2 x 3 = **6** |

**Twenty-five bulls at the full design criterion is achievable to about 137 yards on Tabloid, about 103 yards on Letter, and about 314 yards on a 24 inch roll.** The group grows linearly with distance and the sheet does not. This is geometry, not a design failure, and no target layout fixes it on a single ordinary sheet.

Two caveats in opposite directions. The 0.75 MOA criterion assumes a 0.25 MOA rifle and a good zero; the **measured** worst shot-to-own-bull distance in the corpus was 0.97 MOA, about 30 percent larger, so a shooter with an imperfect zero should read one row further down the table. Against that, the criterion is deliberately conservative, because it asks nearest-bull assignment to be safe, and DETECTION-PIPELINE.md section 3, S9 does better than nearest-bull whenever shot count equals bull count.

**What the library now does about it.** The GL-LR300 family shipped in sections 4.4 and 4.5 sits slightly inside the criterion at 300 yards: a 4.0 inch pitch gives a 0.64 MOA half-pitch against the 0.75 target, which is 15 percent tight. That is a deliberate trade, because the alternative at 4.71 inch pitch is 24 bulls on the roll sheet instead of 30, or five tiles instead of six for the same count. Cross-cell shots at 300 yards will be occasional rather than rare, and the assignment interface handles them.

### 7.1 How wide a sheet can usefully be

Cell pitch is angular, per section 2. So is the whole grid, and that is worth checking separately because it is what the shooter has to swing the rifle across.

| Layout | At | Grid W | Grid H | Widest hold from centre |
|---|---|---|---|---|
| GL-RF36-LTR | 50 yd | 9.55 MOA | 11.84 MOA | 7.60 MOA |
| GL-RF25-LTR | 50 yd | 7.64 | 9.93 | 6.26 |
| GL-CF25-LTR | 100 yd | 5.71 | 7.43 | 4.69 |
| GL-LR25-TAB | 200 yd | 3.82 | 4.96 | 3.13 |
| GL-LR300-T, 2 x 2 | 300 yd | 3.82 | 6.37 | 3.71 |
| GL-LR300-R24 | 300 yd | 6.37 | 6.62 | 4.59 |
| GL-LR300-R42 | 300 yd | **11.46** | 5.35 | 6.32 |

**The first thing this table settles is that it is not a long-range problem.** The widest sight picture in the library is not the 42 inch roll at 300 yards, it is the densest rimfire sheet at 50 yards, at 11.84 MOA of elevation. Rimfire shooters use grids like that routinely, which is the empirical answer to whether it is a problem at all.

**The second thing is that it costs no accuracy, and it is worth writing down why**, because a reader will otherwise assume it does. The shooter re-centres the reticle on each bull rather than holding off, so nothing about the aim is offset. The scope's zero error is common to every bull, so it shifts the whole composite equally and appears in the offset-from-aim figure rather than in the dispersion. Parallax and reticle linearity would matter if the reticle were being used to measure, and it is not. The one second-order term that does scale with angle, the cosine of the hold angle, is 0.99999 at 6 MOA.

**What it does cost is comfort.** Swinging 11.5 MOA of windage from a bipod means breaking position and rebuilding it, and a rebuilt position has a different natural point of aim. That is a real effect on the shooter rather than on the measurement, and the mitigation is to shoot column by column rather than row by row on the widest sheets. Worth a line in the printed instructions, not a change to the geometry.

### 7.2 Why there is still no 600 yard sheet

The question is live now that 36 and 42 inch rolls are in the library, because a 600 yard grid does fit one. At the 0.75 MOA criterion the pitch is 9.42 inches, and both the 36 and the 42 inch roll hold **four columns**, since the roll gets wider faster than the pitch does not. Twenty-eight bulls needs seven rows, which is a sheet **42 by 78 inches**, six and a half feet tall.

Three reasons that does not ship:

**The frame.** A six and a half foot target frame is not something most people have, and a sheet that arrives rolled and cannot be hung flat defeats the fiducial registration it depends on.

**Four columns is a bad shape.** The grid would be 4.5 MOA wide and 9.0 MOA tall, so almost all the swing is elevation, which is the axis where a bipod position is least repeatable.

**The measurement is the wrong one anyway.** At 600 yards the useful questions are hit probability and vertical dispersion against predicted, not extreme spread of a composite group, and those are answered by the solver-coupled analyses in STATISTICS.md section 12. The format expresses a 600 yard sheet perfectly well and the generator will make one on request; the built-in library does not include one because shipping it would imply it is the right tool.

Beyond 300 yards the honest answer remains that this is a different measurement. At 600 yards the useful questions are hit probability and vertical dispersion against predicted, not extreme spread of a composite group, and those are answered by the solver-coupled analyses in STATISTICS.md section 12 rather than by a denser target. Section 7.2 works through what a 600 yard sheet would actually look like and why the library does not ship one.

---

## 8. Cross-check against the shipped OnTarget sheets

ONTARGET-DIMENSIONS.md measures all 47 target PDFs shipped with OnTarget TDS. It exists as a sanity check on the reasoning in section 2, not as a source of geometry, and none of its numbers were copied into this library. What it found is worth recording because two of the three findings changed this document.

**The sizing agrees, independently.** The modal shipped bull is a **1.000 inch outer ring on a 1.500 inch pitch**, a ratio of 1.5026. GL-CF25 arrived at **25.4 mm on 38.0 mm**, a ratio of 1.4961, from the angular argument alone. Two different routes to the same proportion is the strongest available evidence that the proportion is right, and it is also a reminder that convergent geometry is not copying: a 1 inch ring at 100 yards subtends 0.96 MOA whoever draws it.

**No shipped bull uses a stroked annulus.** Every one is built from filled discs. This was arrived at here from the ambiguity argument in TARGET-SCHEMA.md section 3.4 and adopted before the survey was read; finding that the incumbent does the same, presumably for the same reason, raised confidence enough to make it a format rule rather than a convention.

**The sighter gap is 1.2 times the pitch.** Of the shipped sheets that carry a sighter row, the modal gap is 1.2 pitches. The earlier draft of this library used a measured absolute figure of 45.6 mm, which happened to be the right number for a 38.0 mm pitch and the wrong number for every other pitch in the library. The survey turned an accident into a rule, and all fourteen multi-bull sheets now use it, three of them shortened only as far as brings the sighter row inside the fiducial lattice (section 3).

Nothing else from the survey was adopted. Ring counts, numbering schemes, page furniture, colour and naming are all different here, and deliberately so.

---

## 9. What is deliberately not in the library

**No silhouettes, no scoring rings, no competition faces.** GroupLab measures dispersion. Scoring faces are a different product and several are subject to national federation specifications.

**No OnTarget layouts.** Excluded by DESIGN.md section 3 and START-HERE.md.

**No sub-0.5 inch pitch.** The measured hull diameter of a hole runs to 0.539 inches at the top of the range, and the disturbed zone is larger still. Cells that small would guarantee overlapping perforations across cell boundaries, which is exactly what the one-shot-per-bull design exists to prevent.

**No layouts without fiducials.** A target without registration marks is a secondary-mode target, and the whole point of the library is that its targets are self-describing.

**No colour in version one.** Every layout is pure black on white. The colour analysis in SCAN-MEASUREMENTS.md section 4 is the reason: on the greyscale rendering, printed artwork and hole core are the same grey, Fisher ratio 0.14, and no channel separates them. Colour would help a colour scan and actively hurt a user who prints or scans in greyscale, which is common and free. When the detector has a measured colour path, coloured variants can follow.

**No 600 yard sheet.** Section 7 explains why.

---

## 10. Validation

Two validators, delivered as `tools/layout/layout.py` and `tools/layout/zero.py`, are the acceptance test.

For each multi-bull layout, `layout.py`:

1. Centres the grid horizontally and places the scoring rows below the top code band with a 3.0 mm clearance, computing the required offset from whether the outer bull columns overlap the code corners in x.
2. Places the sighter row at 1.2 times the pitch and checks it clears the bottom code band and the load block.
3. Reserves the load block band where one is declared.
4. Generates the fiducial lattice for the declared scheme and drops any marker that would fall inside the 12 mm safe margin, within 2.0 mm of a code quiet zone, within 1.0 mm of a ring, or within 2.0 mm of another marker.
5. Checks **every pair** of printed elements for overlap: rings, sighter rings, code boxes, marker footprints **and the data block rectangle**.
6. Treats its own fit as an error: a grid needing more vertical room than the page allows fails rather than being silently squashed.
7. Warns when any two large elements come within the 3.0 mm clearance, and when two markers come within 2.0 mm.
8. Asserts the even-pitch rule, and the divisible-by-four rule under the half-pitch scheme.
9. Reports margins, marker count, dropped count and page-area overhead.

**Checks 4 to 7 were tightened after the first Claude Code session read this document and found what the validator was missing.** They are recorded here because each had let a real fault through, and a validator that has never caught anything is not evidence of anything.

| Tightened check | What it had been hiding |
|---|---|
| The data block is an element in the overlap test | The two bottom codes were drawn **inside** the load block on all eight sheets that carry one. The solver reserved room for the block as though the codes sat above it, then drew them at the page corners |
| The solver's own fit flag is an error | Three sheets did not fit and said nothing: GL-CF25-LTR-D by 11.0 mm, GL-CF30-LTR by 2.8 mm, GL-LR300-TA4 by 0.3 mm |
| Markers are dropped inside the data block | Five markers on the 24 inch roll sheet and eight on the 36 inch one sat inside the load block |
| The column-versus-code test uses the clearance, not bare overlap | On the 300 yard Letter tile a bull sat **0.15 mm** from a code and the solver called it clear |

The fixes: the bottom codes now sit above the data block with 3 mm of clearance, exactly as `zero.py` always did; GL-CF25-LTR-D drops to two codes; GL-CF30-LTR goes to a 35.0 mm pitch; both 300 yard tiles go to a 32.0 mm ring. No other sheet changed shape, though marker counts moved on several as the stricter drop test took effect.

For each zeroing sheet, `zero.py`:

1. Computes each minor line from its own true angular offset and reports the worst rounding deviation.
2. Centres the field between the top and bottom code bands and the load block.
3. Places the `field-ring-1` markers in the band around the field, on the grid's major lines, dropping any that clash.
4. Checks that no marker or code intrudes on the grid field, that the field is inside the safe margins, and that no two elements overlap, **the load block among them**.
5. Warns if the side band falls below 15 mm, if the band between the field and the code rows is too narrow for a full marker row, if line rounding exceeds 1 dmm, if two markers come within 2.0 mm, or if fewer than eight markers survive.
6. Emits markers in raster order, y then x, as TARGET-SCHEMA.md section 3.7 requires. An earlier draft emitted them in candidate order, which would have given every zeroing sheet the wrong marker identities.

Current status: **sixteen multi-bull layouts and four zeroing sheets, zero errors.** Two zeroing sheets carry a warning apiece, and the warning is true rather than spurious: GL-ZERO-MOA-100Y and GL-ZERO-MIL-100M leave 6.9 mm and 6.7 mm between the grid field and the code rows, where a full marker row wants 12.0 mm, so their top and bottom rows survive only away from the code columns. That is why they carry 24 and 12 markers where the other two carry 16 and 24.

**A second review found three more faults and one specification gap**, in the same way and with the same result:

| Fault | What it had been hiding |
|---|---|
| `check.py` derived the page size from a rounded inch string | A4 came out 2101 x 2969 rather than 2100 x 2970, which moved the data block and the tiling on GL-RF25-A4, GL-LR25-A3, GL-LR300-TA4 and the TA4 3 by 2 preset. It now reads the page table |
| The zeroing block was declared as `fields-3x3-1` with a 28.0 mm reserve | It is two rows, not three, and a 28.0 mm reserve does not fit inside a 21.0 mm block. Both were wrong in the same four lines |
| `zero.py` put the bottom codes hard against the load block | The multi-bull solver had been fixed to leave 3.0 mm and this one had not, so the two solvers disagreed about the same rule |
| `codes.placement` named no offsets at all | A decoder had to invent the inset from the page edge. It is now the versioned rule `corners-1`, stated in full, and `check.py` cross-checks it against every sheet the layout solver placed |

**Payload size: checked, and it is fine.** The reference encoder at `tools/gltd/encode.py` gives 47 to 70 bytes of body across all twenty sheets plus the two extra tile-assembly presets. With the 15 byte frame header the worst case is **85 bytes** against the 119 byte capacity of a version 10 level H symbol, leaving 34 bytes of headroom. The counts do not scale with bull count, because parametric mode stores a grid rather than a bull list; they scale with the number of *kinds* of block a sheet carries. No layout needs a larger symbol.

`tools/gltd/check.py` now **builds every definition from `tools/layout/layouts.json`** rather than from a hand-maintained copy of the geometry. The hand-maintained version had drifted: three of its sheets were built from superseded positions and ring sizes, so the identifiers it printed belonged to targets that no longer existed. Deriving them removes the failure mode rather than fixing one instance of it.

Two checks to add in Phase 2, which need a renderer and therefore cannot be done here:

- **Round trip.** Every layout encodes to GLTD-B and back to a byte-identical canonical document, per TARGET-SCHEMA.md section 10.
- **Render and re-detect.** Render each layout, analyse the render as if it were a scan, and confirm every bull centre is recovered at its declared coordinate within the Phase 0 residual gate. This is test 43 of TARGET-SCHEMA.md section 10 and it closes the loop between the library, the renderer and the analyser.

---

## 11. Questions

1. **Is a 3 by 2 assembly the right default for 300 yards?** Six sheets is a lot of taping. Two by two gives 24 bulls, which is one short of the rule, on four sheets. Three by two gives 36 on six. If 24 is acceptable in practice the default should probably be four sheets.

1. **Should the load block appear on more sheets?** At present it is on twelve of the twenty. The centrefire Letter sheet has to choose between sighters and a block, and ships as two definitions. Would you rather the block were the default and sighters the variant?

2. **Ring count.** Every layout uses two annuli plus a centre dot. Would you rather have a single ring and a dot, which is cleaner and leaves more white space for the difference operation, or three rings, which gives more aiming reference at long range? The measurements say the render-and-difference step subtracts printed artwork cleanly either way, so this is aesthetics and aiming preference rather than a detection constraint.

3. **Is 1.0 inch the right ring for centrefire at 100 yards?** It subtends 0.96 MOA. Some shooters prefer a much smaller aiming point, a 0.25 inch dot or a fine diamond, on the grounds that a scope's reticle centres more precisely on a small mark. That would be a straightforward variant and it changes nothing else in the layout.

4. **The zeroing sheets have one aiming mark each.** Some shooters confirm zero at two or three settings on one sheet. Three grids on one Letter sheet would each be about 50 mm across, which is 1.9 MOA of range at 100 yards, enough for confirmation but not for zeroing from scratch. Worth a variant?

5. **Should the widest sheets carry a printed shooting-order note?** Section 7.1 concludes that GL-LR300-R42, at 11.5 MOA of windage, is better shot column by column than row by row. That could be a line of text on the sheet, or it could be left to the shooter. It is the only place in the library where the geometry implies a technique.
