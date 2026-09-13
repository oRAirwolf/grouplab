# GroupLab Target Definition Format

**Version** GLTD 1.0 (draft)
**Implements** DESIGN.md section 8, and the payload described in section 9
**Status** Specification draft for review. No code written.

---

## 1. Scope and design rules

This document specifies two things that describe the same object:

- **GLTD-J**, a versioned JSON document. Canonical, human-readable, hand-authorable, emitted by the parametric and visual designers, and the interchange format for the community library.
- **GLTD-B**, a compact binary encoding of the same definition. This is what goes into the QR codes printed on the sheet, and it is what makes a target self-describing.

The two are required to be **lossless in both directions**. A GLTD-J document encodes to GLTD-B and decodes back to a byte-identical canonical GLTD-J document. This round-trip is a test, not an aspiration, and section 10 makes it a gate.

Five rules govern everything below.

**R1. Every length is an integer number of tenths of a millimetre.** No floating point anywhere in the geometry. See section 2 for why this is not merely a style preference.

**R2. Page size is declared, never assumed.** DESIGN.md section 8 is right that this is what makes A4 support fall out for free, and the sample data proves the wider point: the sample scans cover 8.263 by 10.763 inches, not 8.5 by 11, so software that assumes a page extent is wrong before it starts.

**R3. The definition is complete.** A GLTD-B payload is sufficient on its own to reconstruct the target, render it, and analyse a scan of it. No network access, no database, no lookup. The community library is discovery, never dependency.

**R4. Unknown fields are preserved, unknown majors are refused.** A reader that does not understand a minor-version addition must round-trip it unchanged rather than dropping it. A reader that meets a major version it does not know must refuse the file and say so, rather than guessing.

**R5. The renderer and the analyser read the same numbers.** There is one definition and both sides use it. This is what removes an entire category of silent error.

---

## 2. Units, and why the geometry is integer

All lengths in GLTD are **integer tenths of a millimetre**, written `dmm` throughout. 254 dmm is 25.4 mm is one inch. Angles are integer hundredths of a degree. There are no floating-point geometric values in either encoding.

This looks fussy and it is load-bearing.

**The obvious reason** is that a binary encoding needs a fixed quantum, and having the JSON and the binary share it means the conversion is a cast rather than a rounding decision. JSON Schema's `multipleOf: 0.1` cannot express "one decimal place" reliably, because 0.1 has no exact binary floating-point representation and validators disagree at the edges. Integers have no such problem.

**The non-obvious reason** is that it makes the quantum free of measurement consequence. A 0.1 mm quantum is 0.0039 inches, which sounds alarming next to the Phase 0 gate of one thousandth of an inch of registration residual. It is not, because the quantum limits **where a bull may be placed**, not **how accurately its position is known**. The printed artwork is rendered from the same quantised integer the analyser later uses as the origin for that cell. The two cannot disagree, so the quantisation contributes exactly zero error to any measurement. What it costs is design freedom: a bull centre cannot sit at 25.43 mm, only at 25.4 or 25.5.

This is worth stating in the specification itself, because the first reviewer to see integer tenths will assume it introduces error, and it is important that implementers understand it does the opposite. A format that stored 25.43 mm in JSON and 25.4 mm in the QR would introduce a real 0.3 mm error into every shot in that cell, and it would be invisible.

**Every derived layout rounds to nearest, ties toward zero.** Several rules in section 3 divide an extent into n parts and round each boundary, written `round(extent * i / n)` throughout. Nearest is obvious; the tie is not, and it has to be stated because the two obvious host languages disagree by default. Python's `round` and C#'s `Math.Round` both break ties to even, other stacks break away from zero, and a definition whose line positions depend on which language read it is not a definition. So the rule is **ties toward zero**, and it is written in integer arithmetic so that no floating-point value ever touches a stored coordinate:

```
round(extent * i / n), ties toward zero, for extent >= 0 and n > 0
  = (2 * extent * i + n - 1) / (2 * n), integer division
```

**The rule governs derivation, not layout.** It applies wherever a reader recomputes a value from what the definition stores: the cell lattice of section 3.6, the marker lattices of section 3.7 including `field-ring-1`, the code centres of `corners-1` in section 3.8, the data block cells of section 3.10, and the grid lines of section 3.13. It does **not** apply to a generator's own arithmetic, whose results are stored explicitly and recomputed by nobody. Centring a grid on a page is the example: `(2159 - 1520) / 2` is 319.5, the solver picks 320, and 320 is what the file carries. There is nothing for a decoder to get wrong, so there is nothing for the tie rule to govern. Applying it there would move every bull on the reference sheet to no purpose.

The two derivations where a tie is currently unreachable are worth naming anyway, because they will not stay unreachable. `corners-1` halves a footprint of 65 modules, and `field-ring-1` halves a marker footprint. Both are even in every built-in, at a 4 dmm module and a 60 dmm footprint, so no tie arises and the reference tools in `tools/` agree with the rule by accident rather than by construction. An odd module size or an odd marker footprint would produce one, and then the rule decides.

This is not an arbitrary pick between three equally good conventions. A derived lattice is anchored on an extent that was itself rounded from a real measurement, and where that extent was rounded up, which is the common case because a half-extent is chosen to reach a round angular figure, every derived boundary inherits the same upward bias. Breaking the tie downward cancels part of it. On GL-ZERO-MIL-100Y, the one built-in where ties occur at all, ties toward zero holds the worst line deviation at **0.48 dmm** where ties to even would give 0.80 and ties away from zero 0.92.

**Consequence for implementers.** The renderer must render from the definition's integers. It must not accept a designer's unrounded value, render from that, and store the rounded value. The designer rounds on input, and what the user sees on screen is what is stored.

---

## 3. GLTD-J, the JSON document

### 3.1 Top level

```json
{
  "gltd": 1,
  "revision": 0,
  "id": "GL-4H7K-9P2M-XQ3T-B8N5",
  "name": "GroupLab 5x5 Load Development, Letter",
  "description": "25 scoring bulls on a 38.0 mm grid with a 3-bull sighter row.",
  "author": "GroupLab built-in library",
  "licence": "CC0-1.0",
  "created": "2026-09-13",
  "units": "dmm",
  "page": { },
  "inks": [ ],
  "ringSets": [ ],
  "bulls": [ ],
  "cells": { },
  "fiducials": { },
  "codes": { },
  "print": { }
}
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `gltd` | integer | yes | **Major** format version. A reader that does not know this value refuses the document |
| `revision` | integer | yes | **Minor** format version. Additive only. A reader that does not know it proceeds and preserves unknown fields |
| `id` | string | no | Definition identifier, computed. See section 6. Present in stored files, absent in hand-authored ones, recomputed on load and compared if present |
| `name` | string | yes | Short display name |
| `description` | string | no | Free text |
| `author` | string | no | Free text |
| `licence` | string | no | SPDX identifier. Built-in targets are CC0-1.0 so that nobody has to think about whether they may print one |
| `created` | string | no | ISO 8601 date |
| `units` | string | yes | Must be the literal `"dmm"` in version 1. Present so that a future version can add another unit system without ambiguity |

`gltd` and `revision` are separate integers rather than a semantic-version string because version comparison in a parser should not require parsing.

### 3.2 `page`

```json
"page": {
  "size": "letter",
  "width": 2159,
  "height": 2794,
  "orientation": "portrait"
}
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `size` | string | yes | One of `letter`, `legal`, `tabloid`, `a3`, `a4`, `a5`, `roll-24`, `roll-36`, `roll-42`, `custom` |
| `width` | integer dmm | yes | Always written explicitly, even for a named size |
| `height` | integer dmm | yes | Always written explicitly |
| `orientation` | string | no | `portrait` or `landscape`, purely a hint to the print dialog. `width` and `height` are already final and are not swapped by it |

Named sizes still carry explicit dimensions. This is deliberate redundancy: it means a reader that does not recognise a size name added in a later revision can still render the page correctly, and it means the numbers a human reads in the file are the numbers the software uses. A validator checks that a named size matches its standard dimensions and warns, rather than errors, if it does not, because a user who deliberately trims a sheet has a legitimate reason.

Standard sizes, for reference:

| Name | Width dmm | Height dmm | Inches |
|---|---|---|---|
| `letter` | 2159 | 2794 | 8.5 x 11 |
| `legal` | 2159 | 3556 | 8.5 x 14 |
| `tabloid` | 2794 | 4318 | 11 x 17 |
| `a5` | 1480 | 2100 | 5.83 x 8.27 |
| `a4` | 2100 | 2970 | 8.27 x 11.69 |
| `a3` | 2970 | 4200 | 11.69 x 16.54 |

**Roll media.** A plotter roll fixes the width and leaves the length to the design. The three roll presets fix `width` and require `height` to be written explicitly:

| Name | Width dmm | Inches | Height |
|---|---|---|---|
| `roll-24` | 6096 | 24 | declared |
| `roll-36` | 9144 | 36 | declared |
| `roll-42` | 10668 | 42 | declared |

A roll preset is not the same as `custom`. `custom` says nothing is standard and both dimensions are the designer's; `roll-24` says the width is a media constraint the user cannot change, which is what the generator needs to know before it offers to lengthen a sheet. Anything outside these three widths uses `custom`. All three are used by the built-in library: TARGET-LIBRARY.md section 4.5 ships a 300 yard sheet on each. The `dmm` ceiling of 65535 caps a roll target at 6553.5 mm of length, which is 21.5 feet, and the generator refuses beyond it rather than wrapping.

The coordinate origin is the **top-left corner of the page**, x increasing right, y increasing down. This matches every raster imaging convention the software will touch, and disagreeing with PDF's bottom-left origin in one place at render time is cheaper than disagreeing with images everywhere else.

### 3.3 `inks`

```json
"inks": [
  { "key": "black",  "srgb": "#000000", "role": "artwork" },
  { "key": "red",    "srgb": "#C8102E", "role": "artwork" },
  { "key": "fid",    "srgb": "#000000", "role": "fiducial" },
  { "key": "paper",  "srgb": "#FFFFFF", "role": "paper" }
]
```

An indexed palette rather than colours inline on every element. Three reasons: the binary encoding then stores a 4-bit index instead of a 24-bit colour everywhere; a target can be recoloured in one place; and the detection pipeline needs to know which printed colours exist on the sheet before it looks at a scan, which is exactly a palette.

| Field | Type | Required | Meaning |
|---|---|---|---|
| `key` | string | yes | Referenced by discs and other elements. Unique within the document |
| `srgb` | string | yes | `#RRGGBB` |
| `role` | string | yes | `artwork`, `fiducial`, `code`, `text`, or `paper`. Tells the renderer what may be rescaled or recoloured, and tells the analyser what to expect |

**The `paper` role is a knockout, not a colour.** An element painted in a `paper` ink instructs the renderer to lay **no ink at all** and reveal the substrate. Its `srgb` is a preview value for the screen, and on the sheet it is the absence of printing. This matters in three places. On tinted or buff stock the knockout is the stock's own colour, not white, and a renderer that laid white ink there would produce a visible patch. On a printer that is out of one cartridge, a knockout stays correct. And the render-and-difference stage of the detection pipeline needs to know which regions of the artwork are expected to carry no ink, which is exactly the set of knockouts.

At most one ink may carry the `paper` role. In the binary encoding it is **ink index 15**, always, and it is never written into the ink table, so the knockout costs zero bytes. That leaves indices 0 to 14 for real inks, so the practical limit is **15 declared inks**. This is not a real constraint on a printed target.

### 3.4 `ringSets`

A ring set is a reusable bull design. Most targets have one, referenced by every bull. **A ring set is a stack of concentric filled discs, painted outermost first.** There are no strokes.

```json
"ringSets": [
  {
    "key": "std",
    "discs": [
      { "diameter": 254, "ink": "black" },
      { "diameter": 238, "ink": "paper" },
      { "diameter": 127, "ink": "black" },
      { "diameter": 115, "ink": "paper" },
      { "diameter": 25,  "ink": "black" }
    ]
  }
]
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `key` | string | yes | Unique within the document |
| `discs` | array | yes | Concentric, **outermost first**, painted in array order. 1 to 15 entries |
| `discs[].diameter` | integer dmm | yes | Diameter of the filled disc |
| `discs[].ink` | string | yes | An ink `key`. A `paper` ink knocks out, revealing what is underneath |

That example draws a black annulus from 23.8 to 25.4 mm, a black annulus from 11.5 to 12.7 mm, and a 2.5 mm centre dot. Diameters must be strictly decreasing.

**Why discs and not strokes.** A stroked circle of diameter D and stroke width W has three legitimate interpretations, and every graphics stack picks one: ink from D-W to D (inside), from D-W/2 to D+W/2 (centred, the SVG and PDF default), or from D to D+W (outside). For the reference bull those are 24.6 to 25.4, 25.0 to 25.8, and 25.4 to 26.2 mm. The three differ by 0.8 mm, which is **0.0157 inches at the edge of the ring**. The Phase 0 registration gate is 0.001 inches and the measured noise floor of hole centroids in the sample scans is 0.008 inches, so a stroke convention mismatch between the renderer and the analyser would be sixteen times the gate and twice the noise floor. Rule R5 says the renderer and the analyser read the same numbers; a stroked ring means they read the same number and draw different things.

A disc stack has exactly one interpretation. Every boundary in the artwork is a declared diameter, and the ink extent of every annulus is the difference of two integers in the file. There is nothing left for a graphics library to decide.

This also matches what the incumbent actually prints. Of the 47 shipped OnTarget PDFs measured in ONTARGET-DIMENSIONS.md, **none** uses a stroked annulus; every bull is built from filled discs. The reason is the same one arrived at here from first principles.

The cost is that the palette must contain a `paper` knockout, and that a designer thinks in boundaries rather than in line weights. The visual designer presents ring width as a control and stores the two diameters, which is a UI concern rather than a format one.

Note that the aiming point is the **geometric centre of the ring set**, always, and is not a separate field. A ring set whose discs are not concentric is not expressible, which is intentional.

For scale: the sample OnTarget sheets measure 1.257 inch rings, 319 dmm, on a measured 1.49845 inch grid. The GroupLab reference layout uses a 25.4 mm ring on a 38.0 mm grid, chosen from the angular reasoning in TARGET-LIBRARY.md section 2 rather than copied.

### 3.5 `bulls`

```json
"bulls": [
  { "x": 320,  "y": 523,  "ringSet": "std", "label": "1",  "scoring": true },
  { "x": 700,  "y": 523,  "ringSet": "std", "label": "2",  "scoring": true },
  { "x": 700,  "y": 2499, "ringSet": "std", "label": "S1", "scoring": false }
]
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `x`, `y` | integer dmm | yes | Centre of the bull, from page top-left |
| `ringSet` | string | yes | A ring set `key` |
| `label` | string | no | Printed near the bull. Free text, not required to be unique |
| `scoring` | boolean | yes | `false` marks a sighter, excluded from the composite group by default |
| `labelOffset` | `{x, y}` | no | Label position relative to the bull centre. Defaults to a rendering convention |

**Labels are explicitly not required to be unique**, and are explicitly not identifiers. The sample data contains a target with two bulls both labelled 9, which is a printing quirk in a real product, and a format that refuses to represent reality is a format that cannot describe the test set. The identifier of a bull is its **index in this array**, which is stable, unique, and what every other part of the system references. Labels are for humans.

Bulls are stored in the order they are to be numbered and shot. A renderer draws them in array order and an editor lists them in array order.

### 3.6 `cells`

```json
"cells": {
  "mode": "grid",
  "drawn": true,
  "ink": "black",
  "stroke": 3,
  "grid": { "originX": 130, "originY": 349, "pitchX": 380, "pitchY": 380,
            "cols": 5, "rows": 5 },
  "sighterGap": 456
}
```

| `mode` | Meaning |
|---|---|
| `none` | No cells. Assignment falls back to nearest bull |
| `grid` | Rectangular cells on a regular pitch |
| `voronoi` | Cells are the Voronoi regions of the bull centres. Computed, not stored |
| `explicit` | An array of polygons, one per bull |

`drawn` controls whether the boundaries are printed. Cells can exist for assignment purposes without appearing on the sheet.

`sighterGap` is optional and declares the intended centre-to-centre distance from the last scoring row to the sighter row. The geometry is already absolute in `bulls`, so this field changes nothing about rendering; it exists so that a deliberate departure from the library convention of **1.2 times the grid pitch** does not raise a validator warning on every load. Section 7 gives the rule and ONTARGET-DIMENSIONS.md gives the measurement it came from. Omit the field and the convention applies.

**On a parametric layout the cell lattice is derived, not stored.** Its boundaries sit half a pitch from each bull centre, so the lattice is the bull grid of section 3.5 shifted by `pitchX/2` and `pitchY/2`, and it carries no information the grid does not already have. This matters for a reason beyond tidiness: the binary body has no cell block at all in version 1, so a definition whose cells could not be derived would be one the encoder has to refuse. `cells.grid` is therefore **optional, and the built-in library omits it**. Where a document does carry it, for readability or from a visual designer, it must equal the derivation, and a mismatch is an error rather than a repair, exactly as it is for a stored marker list or a stored code position.

**A cell is a region, and a region is clipped by the paper.** The derived lattice of a small sheet can extend past the sheet edge, and on the 300 yard tiles it does: three rows at a 101.6 mm pitch make a 304.8 mm lattice on a 279.4 mm Letter sheet, so the outer boundaries fall 12.7 mm off each end. That is not an error and it does not need a geometry change. A cell is the set of points assigned to a bull, and the part of it that is not on the paper can never contain a hole, because the scan is of the paper. The cell region is the intersection of the lattice cell with the sheet. Only **drawn** cell boundaries are artwork, and those are clipped at the sheet edge like anything else. A negative derived origin is simply not written, which is also why the schema's `dmm` type can stay non-negative.

**One consequence for assemblies, worth stating before somebody assumes otherwise.** A tiled assembly is not a uniform lattice across its seams. On GL-LR300-T the bulls sit at a 4.0 inch pitch within a sheet, and the last bull of one tile is 3.0 inches from the first bull of the tile below, because three 4.0 inch rows do not divide an 11.0 inch sheet. Nothing measures across a seam, per section 3.12, so this costs no accuracy. It does mean the cell lattice cannot be continuous across the assembly either, which is a second reason cells belong to a sheet rather than to an assembly.

**A note the specification should carry rather than bury.** Cells define the *default* assignment of a hole to a bull. The sample data establishes, and the scan measurements confirm, that on some targets the nearest bull is frequently the wrong bull, because shots land outside their own cell and sometimes inside a neighbour's. No geometric rule recovers the correct answer. Cells are a starting guess for a human to correct, and any implementation that treats them as authoritative is wrong. DESIGN.md section 13 requires the explicit reassignment interface for exactly this reason.

### 3.7 `fiducials`

```json
"fiducials": {
  "scheme": "grid-boundary-1",
  "family": "apriltag-36h11",
  "markerSize": 40,
  "quietZone": 10,
  "ink": "fid",
  "markers": [
    { "id": 0, "x": 130, "y": 333 },
    { "id": 1, "x": 510, "y": 333 }
  ]
}
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `scheme` | string | yes | `explicit`, or a named derivation rule from the table below |
| `family` | string | yes | Marker family. `apriltag-36h11` throughout the built-in library. See the fiducial decision document |
| `markerSize` | integer dmm | yes | Outer edge length of the marker square, excluding quiet zone |
| `quietZone` | integer dmm | yes | Clear margin required around each marker |
| `ink` | string | yes | An ink `key`, whose role must be `fiducial` |
| `markers` | array | conditional | Required when `scheme` is `explicit`. Present but derivable otherwise |

When `scheme` names a derivation rule, the marker list is **computed from the grid** by that rule and is not carried in the binary payload. This is the single largest saving in the encoding, and it is the reason a full parametric target fits in well under a hundred bytes. The rule is versioned in its name so that changing the placement algorithm later cannot silently reinterpret an old target: `grid-boundary-1` means exactly one placement, forever.

**Derivation rules.**

| `scheme` | Lattice | Used by |
|---|---|---|
| `explicit` | none, `markers` is authoritative | hand-placed designs |
| `grid-boundary-1` | cell boundaries: the bull lattice offset by half a pitch in both axes | every sheet at 25.4 to 50.8 mm pitch |
| `grid-boundary-half-1` | as above, subdivided to half-pitch steps in both axes | coarse-pitch sheets, 101.6 mm and above |
| `field-ring-1` | a ring of positions in the clear band around a declared measurement grid, on its major lines | the zeroing sheets |

In every derived rule, a candidate position is **dropped** if it would fall outside the safe margin, or within a clearance of a bull's outermost disc, a code, or another marker. The drop test is part of the rule and is therefore versioned with it, which is what makes recomputation deterministic.

`grid-boundary-half-1` exists because `grid-boundary-1` degenerates at coarse pitch. On the 300 yard tile, a 101.6 mm pitch over a 2 by 3 grid offers only 12 lattice positions and the 38.1 mm rings knock out all but two of them. Two markers is not a usable registration. Subdividing to half-pitch raises the candidate count to 35 and leaves **nine** surviving markers, well spread. The half rule requires the pitch to be divisible by 4 dmm so that the quarter-pitch offsets stay integer, which the validator asserts.

Marker `id` values are the family's own code values, and they are what makes a partial detection unambiguous, per DESIGN.md section 9. Ids are assigned in raster order over the derived lattice, starting at 0. On a tiled assembly the lattice is the **assembly's**, not the sheet's, so ids are unique across the whole set of sheets without any extra machinery. If an assembly needs more positions than the dictionary holds, ids repeat modulo the dictionary size and the tile index in the frame header disambiguates them; the validator warns when this happens, because it costs the single-sheet recovery property. With `apriltag-36h11` and its 587 identifiers this is close to unreachable: the largest assembly the format can express would need more than seventeen Tabloid sheets at the densest lattice before it wrapped.

**On a tiled definition the stored `markers` list is tile 0's**, because one body serves every tile and the tile index lives in the frame header rather than the body. A reader recomputes the lattice for the tile it is holding, and compares against the stored list only for tile 0. This is worth stating because the alternative, storing nothing, loses the check on the one tile where it is free.

**Writers should still emit the computed `markers` array** even for derived schemes, so that a human reading the file can see where the marks are and a third-party tool need not implement the derivation. Readers must recompute and compare, and must treat a mismatch as an error rather than trusting the stored list.

### 3.8 `codes`

The QR codes, which carry the payload.

```json
"codes": {
  "count": 4,
  "version": 10,
  "ecLevel": "H",
  "moduleSize": 4,
  "quietZone": 16,
  "placement": "corners-1",
  "positions": [
    { "x": 250, "y": 250 }, { "x": 1909, "y": 250 },
    { "x": 250, "y": 2544 }, { "x": 1909, "y": 2544 }
  ],
  "humanReadableId": true
}
```

`positions` are the centres of the code squares. `moduleSize` is the printed size of one QR module, and section 7 explains why it has a floor.

**`corners-1` is a derivation rule, versioned in its name exactly as the fiducial schemes are**, and the rule is this:

```
footprint F = 65 * moduleSize      (57 modules of v10 plus 4 quiet each side)
top    y = safeMargin + F/2
bottom y = height - safeMargin - dataBlockHeight
             - (dataBlockHeight ? clearance : 0) - F/2
left   x = safeMargin + F/2
right  x = width - safeMargin - F/2
count 2 emits the top pair only; count 4 emits both pairs
```

with the safe margin at 120 dmm and the clearance at 30 dmm, per section 7. For the reference sheet that is a 260 dmm footprint and centres at 250 and 1909 in x, 250 and 2544 in y.

**The footprint is fixed at 65 modules rather than read from `codes.version`**, which matters because the binary carries `moduleSize` and not the version. If the rule depended on the version, a decoder that assumed version 10 where the generator used version 8 would recompute every corner centre 20 dmm out, and the definition would be quietly wrong rather than loudly broken. Fixing the rule at the library's own version costs a generator emitting a smaller code nothing but the requirement to use `explicit` placement and say where the codes went. The earlier draft named the rule `corners` and stated no offsets at all, which left a decoder to invent the inset: the binary carries one byte of placement and nothing else, so without a versioned rule two implementations could read the same frame and print the codes in different places.

**`positions` is required whenever `count` is greater than zero, including under `corners-1`.** The generator writes the computed centres into the document the way it writes the computed marker list, and a reader recomputes the rule and compares. A stored position that disagrees with the rule is an error, not a repair, which is the same treatment section 3.7 gives a stored marker list. `tools/gltd/check.py` implements `corners-1` and cross-checks it against every sheet the layout solver placed.

**The library standardises on version 10 at error correction level H.** That is 57 modules square, 119 bytes of byte-mode capacity, verified against the `segno` library rather than read from a table. At the 4 dmm module it is a 22.8 mm symbol in a 26.0 mm footprint including the mandatory four-module quiet zone. Section 5.3 shows the largest sheet in the library at 85 bytes, so every built-in fits with at least 34 bytes spare, and one fixed footprint means one set of layout constants rather than a per-sheet reserve. A generator may emit a smaller version for a smaller payload; the built-in library does not, because the saving is 3.2 mm of margin and the cost is a layout that changes shape when a field is added.

`count` may be 4 or 2. Four corner codes is the default. Tiles use 2, in the top corners only, because a tile is a small sheet whose bottom edge is needed for bulls and because an assembly of six sheets already carries twelve copies of the same payload. Rule R3 is still satisfied: one intact code on one sheet reconstructs the definition.

`humanReadableId: true` requires the renderer to print the definition `id` as text on the sheet. This is the last-resort recovery path when every code is destroyed, and it is cheap.

### 3.9 `print`

```json
"print": {
  "scaling": "none",
  "minimumDpi": 300,
  "colourMode": "colour",
  "duplex": false,
  "generator": "GroupLab 0.1.0",
  "notes": "Print at 100 percent. Do not use fit to page."
}
```

`scaling: "none"` is an instruction to the print pipeline to suppress fit-to-page. It is advisory, because no application can force a print driver to obey, which is exactly why print scale is detected and reported after the fact per DESIGN.md section 9. This block records intent; the fiducials record what actually happened.

### 3.10 `dataBlock`

A reserved band, normally along the bottom of the sheet, carrying the load data for the target. Optional; a sheet without one omits the block entirely.

```json
"dataBlock": {
  "x": 120, "y": 2364, "width": 1919, "height": 310,
  "layout": "fields-3x3-1",
  "fieldSet": "standard-9",
  "reserve": 280,
  "ink": "black",
  "labelInk": "text",
  "border": 2
}
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `x`, `y` | integer dmm | yes | Top-left of the block |
| `width`, `height` | integer dmm | yes | Extent of the block |
| `layout` | string | yes | `fields-3x3-1`, `fields-3x2-1`, or `explicit` |
| `fieldSet` | string | yes | `standard-9`, `standard-6`, or `explicit` |
| `fields` | array | conditional | Required when either `layout` or `fieldSet` is `explicit` |
| `reserve` | integer dmm | yes | Edge of the square reserved at the right end of the block. 0 for none |
| `ink` | string | yes | Ink for the rules and boxes |
| `labelInk` | string | no | Ink for the printed field captions. Defaults to `ink` |
| `border` | integer dmm | no | Rule weight. 0 for no border |

**The standard nine fields**, in order: date, distance, cartridge, bullet, powder and charge, brass, primer, seating depth, notes. `standard-6` drops bullet, brass and seating depth, and is what the zeroing sheets use because they have less vertical room and less to record.

**The derived layouts are exact.** `fields-3x3-1` divides the block into three rows of equal height and, within each row, three cells across the content width, where the content width is `width - reserve - gap` and `gap` is fixed at 20 dmm by the rule. Boundaries are computed as `round(contentWidth * j / 3)` for j from 0 to 3, under the tie rule and the integer form of section 2, so every edge is an integer, the cells differ by at most one dmm, and the rule is reproducible from the four numbers in the file. For the reference block that is a content width of 1919 - 280 - 20 = 1619 dmm split at 0, 540, 1079 and 1619, giving cells of 540, 539 and 540 dmm, and three rows of 100 dmm inside a 310 dmm band with 5 dmm of padding at top and bottom. `fields-3x2-1` is the same rule with two rows.

**Print mode is not in the definition.** The user chooses at print time between three modes:

| Mode | What is printed | Where the values come from |
|---|---|---|
| `none` | nothing; the sheet has no data block | a different definition, so a different sheet |
| `blank` | the box, the rules, and the field captions | written in by hand at the range |
| `filled` | the box, the captions, and the typed values, plus the instance code | typed before printing |

`blank` and `filled` produce **the same geometry and the same definition identifier**. The block, the field cells and the reserved square are laid out identically; only what is drawn inside them differs. This is deliberate: a user who prints a blank sheet, shoots it, and later types the load data into the application must get the same analysis as a user who typed it first, and the analyser must not have to care which happened. The `none` case is genuinely a different sheet with different bull positions, which is why the library ships GL-CF25-LTR and GL-CF25-LTR-D as separate definitions rather than as one definition with a flag.

The reserved square is reserved in every mode. In `filled` mode it holds the instance code of section 3.11. In `blank` mode it holds the printed definition identifier and the sheet serial as text, which is the fallback the analyser uses to tie a scan to a session.

**A square too small for the code carries the text instead.** The instance code is a version 11 level Q symbol with a 276 dmm footprint, so **an instance code is printed only where `reserve` is at least 280 dmm**. Below that the square is still reserved and still holds the identifier and the serial as text, in both modes, and the typed values live in the session rather than on the sheet. This is not a hypothetical case: the zeroing sheets have a 210 dmm block, because a taller one eats the clear band that `field-ring-1` needs for its markers, and 210 dmm of square cannot hold 276 dmm of code. The four zeroing sheets therefore use `fields-3x2-1`, `standard-6` and a 210 dmm reserve, and carry no instance code. Their content width is 1919 - 210 - 20 = 1689 dmm, split at 0, 563, 1126 and 1689, in two rows of 100 dmm inside the 210 dmm band.

`reserve` may be 0, which means no square: the block is fields all the way across, there is no gap, and the content width is the full block width. No built-in uses it.

### 3.11 `instance`

The values written into the data block, and the one part of the document that is **not part of the definition**.

```json
"instance": {
  "serial": "A7K3",
  "printed": "2026-09-13",
  "values": {
    "date": "2026-09-13",
    "distance": "100 yd",
    "cartridge": "6.5 Creedmoor",
    "bullet": "140 gr Berger Hybrid",
    "powder": "H4350 41.5 gr",
    "brass": "Lapua, 3x fired",
    "primer": "CCI BR-2",
    "seating": "2.810 in CBTO 2.245",
    "notes": "62 F, 8 mph L-R"
  }
}
```

**`instance` is excluded from the definition identifier, from GLTD-B, and from the canonical form used to compute the hash.** Two sheets carrying the same layout and different load data are the same definition. This is what makes the community library work at all: the definition is the target, and the load is a property of the printing.

The values travel instead in a **separate instance code**, a second QR printed inside the reserved square, carrying a GLTD-I payload with its own magic bytes so it can never be mistaken for a definition frame:

```
GLTD-I frame
+--------+--------------------------------------------------+
| offset | field                                            |
+--------+--------------------------------------------------+
|   0    | magic      2 bytes   0x47 0x49   ASCII "GI"      |
|   2    | wire       1 byte    = 1                         |
|   3    | flags      1 byte    bit 0 = DEFLATE             |
|   4    | defId     10 bytes   the definition identifier   |
|  14    | serial     4 bytes   ASCII, sheet serial         |
|  18    | printed    3 bytes   packed date, days from 2000 |
|  21    | crc32      4 bytes                               |
|  25    | fields     length-prefixed UTF-8, in field order |
+--------+--------------------------------------------------+
```

Header 25 bytes. A realistic filled set of the standard nine fields measures **128 bytes** of field data, so 153 bytes in total. The instance code is specified at **version 11, error correction level Q**, which carries 177 bytes and is 61 modules square: at the 4 dmm module that is a 24.4 mm symbol in a 27.6 mm footprint, which fits the 280 dmm reserve with 2 dmm each side. Level Q rather than H because this code sits in the part of the sheet people write on with a pen and rest their hand against, and because losing it costs a convenience rather than the definition. The generator budgets 152 bytes for field data and refuses to print beyond it, naming the field that overflowed. Two dmm each side is the whole margin, so a definition whose reserve is under 280 dmm carries no instance code at all, per section 3.10.

DEFLATE is available but rarely helps at this size: the sample field set compresses from 128 bytes to 124.

The application reads the instance code to prefill the session, and reads `defId` to confirm the instance belongs to the definition it decoded from the corner codes. A mismatch is reported rather than merged.

### 3.12 `tiling`

Present only on a sheet that is one tile of a larger assembly.

```json
"tiling": {
  "cols": 2,
  "rows": 2,
  "sheetWidth": 2159,
  "sheetHeight": 2794,
  "overlap": 0
}
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `cols`, `rows` | integer | yes | Tiles across and down the assembly |
| `sheetWidth`, `sheetHeight` | integer dmm | yes | The printed sheet, which equals `page` for a tiled definition |
| `overlap` | integer dmm | yes | Printed overlap between adjacent tiles. **0 in the built-in library** |

**The same tile geometry can be assembled at more than one size, and each is a separate definition.** GL-LR300-T ships as a 2 by 2 assembly of four sheets and 24 bulls, which is the default, and as a 3 by 2 of six sheets and 36 bulls. The bulls, discs, fiducials and codes are byte-identical between them; only `cols` and `rows` differ, so the two hash differently and a decoder always knows how many sheets it should be looking for. That last property is what lets S10 of the detection pipeline report "five of six tiles present" rather than silently pooling whatever turned up.

The tile index itself is **not in the body**. It is a byte in the frame header, section 5.1, so that every tile of an assembly carries a byte-identical body and therefore the same definition identifier. One definition, N sheets, distinguished by one header byte.

**Why overlap is zero and why there are no registration marks.** The instinct is that tiles must be aligned accurately, because the assembled target is one large target. That instinct is wrong here, and the reason is worth stating in the specification because it removes a whole subsystem.

Every shot is measured **relative to its own bull**, and every bull sits entirely on one tile, registered by that tile's own fiducials. The composite group is built by translating each shot by its own bull's declared centre. No step in that chain ever uses the position of one tile relative to another. So tile alignment contributes exactly zero error to any group statistic, and tape, staples and eyeballed alignment are all equally good.

What follows from that is the constraint the validator enforces: **no bull, marker, code or drawn cell boundary may cross a tile boundary.** A bull cut in half by a seam is a bull whose centre is not measurable, which is a real error, whereas a seam that is 4 mm out of square is not an error at all. Undrawn cells are the exception and section 3.6 says why: a cell is an assignment region rather than artwork, it is clipped by the paper, and on these tiles the derived lattice does extend past the sheet.

It also follows that tiles are **scanned separately and pooled**, which is what the detection pipeline does. Each sheet is a scan, each scan is registered on its own, and the shots from all sheets are pooled into one composite group at the end. Nobody has to fit a 25 by 22 inch assembled target onto a Letter scanner.

### 3.13 `grids`

A printed measurement grid whose cell size is an exact angular unit at a stated distance. Used by the zeroing sheets; absent from every other layout.

```json
"grids": [
  {
    "key": "zero",
    "centreX": 1079, "centreY": 1292,
    "half": 798,
    "divisions": 6,
    "majorEvery": 2,
    "unit": "moa",
    "distance": 100,
    "distanceUnit": "yd",
    "minorInk": "grey", "majorInk": "black", "axisInk": "black",
    "minorStroke": 2, "majorStroke": 3, "axisStroke": 4,
    "labelStep": 2, "labelInk": "text"
  }
]
```

| Field | Type | Required | Meaning |
|---|---|---|---|
| `centreX`, `centreY` | integer dmm | yes | Centre of the grid, which is also the aiming point |
| `half` | integer dmm | yes | Half-extent of the square field |
| `divisions` | integer | yes | Minor cells from the centre to the edge, per half-axis |
| `majorEvery` | integer | yes | Minor cells per heavy line |
| `unit` | string | yes | `moa`, `mil`, `inch`, `cm`, or `custom` |
| `distance` | integer | yes | The distance the sizing is exact at |
| `distanceUnit` | string | yes | `yd` or `m` |
| `labelStep` | integer | yes | Label every n-th minor line. 0 for no labels |

**Line positions are computed, not stored, and they are integers.** Minor line i, for i from `-divisions` to `+divisions`, sits at

```
centre + sign(i) * round( half * |i| / divisions )
```

with the tie rule and the integer form of section 2.

This is the one place where the obvious approach fails rule R1. Storing a per-cell pitch and multiplying would need a non-integer pitch for three of the four zeroing sheets: 0.1 mil at 100 yards is 9.144 mm, and no integer number of tenths of a millimetre is that. Rounding the pitch to 91 dmm and stepping eight times accumulates to 3.5 dmm of error at the edge of the field. Rounding each line from the stored half instead bounds the error at **half a dmm anywhere in the field**, and it does not accumulate. Measured across the four built-in zeroing sheets the worst line deviation is **0.48 dmm, which is 0.048 mm**; on the 100 metre mil sheet it is exactly zero, because 0.1 mil at 100 m is 10.0 mm on the nose.

**The lines derive from the stored `half`, not from the true angular extent, and the difference is not cosmetic.** `half` is itself a rounded value: 0.8 mil at 100 yards is 731.52 dmm and the field stores 732. A generator that keeps the unrounded 731.52 and rounds each line from that gets a slightly better fit, and it produces a target no decoder can reproduce, because 731.52 is nowhere in the body and cannot be recovered from it. Everything the renderer and the analyser use has to be computable from what the QR code carries. That constraint, not the arithmetic, is what fixes the rule.

On GL-ZERO-MIL-100Y, 732 over 8 divisions puts four of the eight lines exactly on a tie, which is what makes the tie rule of section 2 load-bearing rather than pedantic. Under ties toward zero the derived offsets are 0, 91, 183, 274, 366, 457, 549, 640 and 732, which match the unrounded angle to 0.48 dmm and are what the sheet has always been drawn with. Under ties to even the first line moves to 92 and the fifth to 458, the worst deviation becomes 0.80 dmm, and the `field-ring-1` markers on the 0.5 mil lines move with them.

The grid is drawn from the definition and read back by the analyser from the same numbers, so rule R5 holds and the printed grid is never the thing being measured against. Its accuracy matters only to the human reading a correction off the sheet by eye.

---

## 4. Worked example: the reference 5x5 target

Full document for **GL-CF25-LTR**, the reference layout from TARGET-LIBRARY.md, whose geometry has been placed and overlap-checked by the layout validator.

```json
{
  "gltd": 1,
  "revision": 0,
  "id": "GL-YCSK-DZZ1-R0VJ-4T5Y",
  "name": "GroupLab 5x5 Load Development, Letter",
  "description": "25 scoring bulls on a 38.0 mm grid with a 3-bull sighter row.",
  "author": "GroupLab built-in library",
  "licence": "CC0-1.0",
  "created": "2026-09-13",
  "units": "dmm",

  "page": { "size": "letter", "width": 2159, "height": 2794,
            "orientation": "portrait" },

  "inks": [
    { "key": "black", "srgb": "#000000", "role": "artwork"  },
    { "key": "paper", "srgb": "#FFFFFF", "role": "paper"    },
    { "key": "fid",   "srgb": "#000000", "role": "fiducial" },
    { "key": "code",  "srgb": "#000000", "role": "code"     },
    { "key": "text",  "srgb": "#000000", "role": "text"     }
  ],

  "ringSets": [
    { "key": "std", "discs": [
      { "diameter": 254, "ink": "black" },
      { "diameter": 238, "ink": "paper" },
      { "diameter": 127, "ink": "black" },
      { "diameter": 115, "ink": "paper" },
      { "diameter": 25,  "ink": "black" }
    ]}
  ],

  "bulls": [
    { "x": 320,  "y": 539,  "ringSet": "std", "label": "1",  "scoring": true },
    { "x": 700,  "y": 539,  "ringSet": "std", "label": "2",  "scoring": true },
    { "x": 1080, "y": 539,  "ringSet": "std", "label": "3",  "scoring": true },
    { "x": 1460, "y": 539,  "ringSet": "std", "label": "4",  "scoring": true },
    { "x": 1840, "y": 539,  "ringSet": "std", "label": "5",  "scoring": true },

    { "x": 320,  "y": 919,  "ringSet": "std", "label": "6",  "scoring": true },
    { "x": 700,  "y": 919,  "ringSet": "std", "label": "7",  "scoring": true },
    { "x": 1080, "y": 919,  "ringSet": "std", "label": "8",  "scoring": true },
    { "x": 1460, "y": 919,  "ringSet": "std", "label": "9",  "scoring": true },
    { "x": 1840, "y": 919,  "ringSet": "std", "label": "10", "scoring": true },

    { "x": 320,  "y": 1299, "ringSet": "std", "label": "11", "scoring": true },
    { "x": 700,  "y": 1299, "ringSet": "std", "label": "12", "scoring": true },
    { "x": 1080, "y": 1299, "ringSet": "std", "label": "13", "scoring": true },
    { "x": 1460, "y": 1299, "ringSet": "std", "label": "14", "scoring": true },
    { "x": 1840, "y": 1299, "ringSet": "std", "label": "15", "scoring": true },

    { "x": 320,  "y": 1679, "ringSet": "std", "label": "16", "scoring": true },
    { "x": 700,  "y": 1679, "ringSet": "std", "label": "17", "scoring": true },
    { "x": 1080, "y": 1679, "ringSet": "std", "label": "18", "scoring": true },
    { "x": 1460, "y": 1679, "ringSet": "std", "label": "19", "scoring": true },
    { "x": 1840, "y": 1679, "ringSet": "std", "label": "20", "scoring": true },

    { "x": 320,  "y": 2059, "ringSet": "std", "label": "21", "scoring": true },
    { "x": 700,  "y": 2059, "ringSet": "std", "label": "22", "scoring": true },
    { "x": 1080, "y": 2059, "ringSet": "std", "label": "23", "scoring": true },
    { "x": 1460, "y": 2059, "ringSet": "std", "label": "24", "scoring": true },
    { "x": 1840, "y": 2059, "ringSet": "std", "label": "25", "scoring": true },

    { "x": 700,  "y": 2515, "ringSet": "std", "label": "S1", "scoring": false },
    { "x": 1080, "y": 2515, "ringSet": "std", "label": "S2", "scoring": false },
    { "x": 1460, "y": 2515, "ringSet": "std", "label": "S3", "scoring": false }
  ],

  "cells": {
    "mode": "grid", "drawn": false,
    "grid": { "originX": 130, "originY": 349, "pitchX": 380, "pitchY": 380,
              "cols": 5, "rows": 5 },
    "sighterGap": 456
  },

  "fiducials": {
    "scheme": "grid-boundary-1",
    "family": "apriltag-36h11",
    "markerSize": 40,
    "quietZone": 10,
    "ink": "fid"
  },

  "codes": {
    "count": 4, "version": 10, "ecLevel": "H", "moduleSize": 4, "quietZone": 16,
    "placement": "corners-1", "humanReadableId": true,
    "positions": [
      { "x": 250, "y": 250 }, { "x": 1909, "y": 250 },
      { "x": 250, "y": 2544 }, { "x": 1909, "y": 2544 }
    ]
  },

  "print": {
    "scaling": "none", "minimumDpi": 300, "colourMode": "mono",
    "generator": "GroupLab 0.1.0",
    "notes": "Print at 100 percent. Do not use fit to page."
  }
}
```

**Geometry check.** Five columns at 380 dmm pitch from 320 dmm put the last centre at 320 + 4 x 380 = 1840, and the outer disc extends 127 dmm beyond, reaching 1967 against a page width of 2159. Margins to the outer disc are 192 dmm each side, 19.2 mm. Vertically the last sighter disc reaches 2515 + 127 = 2642 against a page height of 2794, leaving 152 dmm. The corner codes occupy 120 to 380 dmm from each edge; the top row of discs begins at 539 - 127 = 412, clearing the top codes by 32 dmm. Thirty-four fiducial markers survive the placement rule. Verified by the layout validator: zero overlaps, zero tight-margin warnings.

**Why the sighter gap is 456.** The library convention is 1.2 times the grid pitch, which is 456 dmm at a 380 dmm pitch. The ratio is not invented: it is the modal value measured across the shipped OnTarget sheets that have a sighter row, reported in ONTARGET-DIMENSIONS.md. `cells.sighterGap` records it so a reader can see the intent without deriving it from the bull coordinates.

**Why the pitch is 380 and not 381.** The `grid-boundary-1` lattice sits at cell boundaries, half a pitch from each bull centre. An odd pitch puts that half-pitch on a half-dmm, which rule R1 forbids. So **a parametric layout using a derived fiducial scheme must declare an even pitch**, and the validator asserts it; the half-pitch scheme raises that to divisible by four. Here the lattice lands on x = 130, 510, 890, 1270, 1650, 2030 and y = 349, 729, 1109, 1489, 1869, 2249, all integers. The cost is 0.1 mm of design freedom; 38.0 mm is 1.4961 inches, which is closer to the 1.49845 inch pitch actually measured on the sample scans than a nominal 1.500 would be.

---

## 5. GLTD-B, the binary payload

### 5.1 Framing

Every QR code on the sheet carries one **frame**. In the normal case all frames on a sheet are byte-identical and each carries the complete payload, so any one surviving code is sufficient. Frames differ only when a pathological design exceeds single-code capacity and erasure coding is engaged, per section 5.6.

```
Frame
+--------+--------------------------------------------------+
| offset | field                                            |
+--------+--------------------------------------------------+
|   0    | magic       2 bytes   0x47 0x54   ASCII "GT"     |
|   2    | wire        1 byte    wire format version = 1    |
|   3    | flags       2 bytes   uint16 LE                  |
|   5    | shareIndex  1 byte    0..7, code position index  |
|   6    | shareCount  1 byte    1 = replicated, else n     |
|   7    | shareK      1 byte    shares needed to rebuild   |
|   8    | tileIndex   1 byte    0 when the sheet is not a  |
|        |                       tile; otherwise row-major  |
|        |                       index within the assembly  |
|   9    | totalLen    2 bytes   uint16 LE, body length     |
|  11    | crc32       4 bytes   CRC-32/ISO-HDLC of body    |
|  15    | body        totalLen bytes (or a share of it)    |
+--------+--------------------------------------------------+
```

Header is 15 bytes. All multi-byte integers are little-endian.

`flags`:

| Bit | Meaning |
|---|---|
| 0 | Layout mode. 0 = parametric, 1 = explicit |
| 1 | Body is raw DEFLATE compressed |
| 2 | Cell block present |
| 3 | Label block present |
| 4 | Print metadata block present |
| 5 | Extension block present |
| 6 | Data block present |
| 7 | Tiling block present |
| 8 | Measurement grid block present |
| 9-15 | Reserved, must be zero |

Flags is two bytes rather than one because the eight-bit field was already full at six defined bits, and a format that runs out of flag bits in its first revision has to change its framing to add a block. Seven spare bits is enough room for the format's remaining life; if it is not, the extension block of bit 5 takes over.

**`tileIndex` is in the frame and not in the body**, and this is the mechanism that lets an assembly of six printed sheets be one definition. The body is byte-identical on every tile, so the CRC, the definition identifier and the community-library key are all the same, and the only thing that distinguishes sheet 4 from sheet 1 is one header byte. A decoder that reads a tile index greater than zero looks in the body's tiling block for the assembly shape, and knows which part of the assembly it is holding.

`crc32` covers the **uncompressed, unsharded body**. It is therefore an end-to-end check on reconstruction, not merely on one code's transmission, and it is what tells the reader that a 2-of-4 reassembly actually worked. The QR code's own Reed-Solomon handles physical damage; this handles logic errors. Note that it does not cover `tileIndex`, which is deliberate: the tile index is not part of the definition, and a corrupt one is caught by the fiducial identities instead.

### 5.2 Body, parametric mode (flags bit 0 = 0)

The common case, and the one every built-in target uses. Blocks appear in the order below, and an optional block is present only if its flag bit is set.

```
Page block                                        3, 5 or 7 bytes
  pageCode    1 byte   0=custom 1=letter 2=legal 3=tabloid
                       4=a3 5=a4 6=a5
                       7=roll-24 8=roll-36 9=roll-42
  if pageCode == 0:
    width     2 bytes  uint16 quanta
    height    2 bytes  uint16 quanta
  if pageCode >= 7:
    height    2 bytes  uint16 quanta   (width is implied)
  orientation 1 byte   0=portrait 1=landscape
  quantum     1 byte   0 = 0.1 mm, 1 = 0.2 mm,
                       2 = 0.5 mm, 3 = 1.0 mm   (see 5.4)

Ink block                                           1 + 3n bytes
  inkCount    1 byte   n, 0..15
  for each:
    rgb       3 bytes  R, G, B
    (role is implied by use; the paper knockout is the
     reserved index 15 and is never written here.  Each
     DISTINCT sRGB value is stored once, in declaration
     order: the body carries colours, not keys or roles,
     so four inks that are all #000000 are one entry.
     See the projection rule in section 6.)

Ring set block                          1 + sum(1 + 3d) bytes
  setCount    1 byte   1..15
  for each set:
    discCount 1 byte   1..15
    for each disc, outermost first:
      diameter 2 bytes uint16 quanta
      ink      1 byte  bits 0-3 ink index, 15 = paper
                       bits 4-7 reserved, zero

Grid block                                         12 bytes
  cols        1 byte
  rows        1 byte
  originX     2 bytes uint16 quanta, centre of bull [0,0]
  originY     2 bytes
  pitchX      2 bytes
  pitchY      2 bytes
  ringSetIdx  1 byte   applies to all grid bulls
  order       1 byte   0 = row major, 1 = column major,
                       2 = boustrophedon

Sighter block                              1 + 8s bytes
  sighterRows 1 byte   s, 0..4
  for each row:
    count     1 byte
    originX   2 bytes uint16 quanta
    originY   2 bytes
    pitchX    2 bytes
    ringSetIdx 1 byte

Fiducial block                                      6 bytes
  scheme      1 byte   0 = explicit, 1 = grid-boundary-1,
                       2 = grid-boundary-half-1,
                       3 = field-ring-1
  family      1 byte   see table in 5.5
  markerSize  2 bytes  uint16 quanta
  quietZone   1 byte   uint8 quanta
  inkIdx      1 byte

Code block                                          4 bytes
  count       1 byte
  ecLevel     1 byte   0=L 1=M 2=Q 3=H
  moduleSize  1 byte   uint8 quanta
  placement   1 byte   0 = corners-1, 1 = explicit
                       positions are derived, never carried

Data block, flag bit 6                             13 bytes
  x           2 bytes uint16 quanta
  y           2 bytes
  width       2 bytes
  height      2 bytes
  layout      1 byte   0 = fields-3x3-1, 1 = fields-3x2-1,
                       255 = explicit
  fieldSet    1 byte   0 = standard-9, 1 = standard-6,
                       255 = explicit
  reserve     2 bytes  uint16 quanta, instance code square
  inkIdx      1 byte
  (explicit layouts append 8 bytes of rect per field;
   explicit field sets append a length-prefixed key list)

Tiling block, flag bit 7                            9 bytes
  cols        1 byte
  rows        1 byte
  sheetWidth  2 bytes uint16 quanta
  sheetHeight 2 bytes
  overlap     2 bytes
  flags       1 byte   reserved, zero

Measurement grid block, flag bit 8            1 + 15g bytes
  gridCount   1 byte
  for each grid:
    centreX   2 bytes uint16 quanta
    centreY   2 bytes
    half      2 bytes
    divisions 1 byte
    majorEvery 1 byte
    unit      1 byte   0=custom 1=moa 2=mil 3=inch 4=cm
    distance  2 bytes  uint16, in distanceUnit
    distUnit  1 byte   0 = yd, 1 = m
    inkPair   1 byte   bits 0-3 minor ink, bits 4-7 major
    style     1 byte   line weights and axis emphasis
    labelStep 1 byte   0 = no labels

Remaining optional blocks, each behind its flag bit:
  Cell block, Label block, Print block, Extension block
```

Labels are omitted entirely in the common case. The default labelling rule, sequential from 1 in the grid's declared `order`, followed by S1..Sn for sighters, reproduces the reference target exactly and costs nothing. The label block exists for targets that need something else, including the real-world case of two bulls both labelled 9.

### 5.3 Byte budget, measured

The numbers below come from a **working reference encoder** run against the definitions in the built-in library, not from estimates. The encoder lives at `tools/gltd/encode.py`.

| Target | What makes it big | Body | Frame | Fits v8-H (84) | Fits v10-H (119) |
|---|---|---|---|---|---|
| GL-CF30-LTR | six rows, no sighters, no block | 47 | 62 | yes | yes |
| GL-CF25-LTR | reference 5x5 with sighters | 55 | 70 | yes | yes |
| GL-RF36-LTR | densest, 36 scoring plus 4 sighters | 55 | 70 | yes | yes |
| GL-LR300-T | tiling block, half-pitch fiducials | 56 | 71 | yes | yes |
| GL-CF25-LTR-D | data block, no sighters | 60 | 75 | yes | yes |
| GL-RF25-LTR | data block and sighters | 68 | 83 | yes | yes |
| GL-LR300-R42 | roll page, data block, sighters | 70 | 85 | **no** | yes |
| GL-ZERO-MOA-100Y | measurement grid and data block | 70 | 85 | **no** | yes |

**Eighty-five bytes is the worst case in the library**, against DESIGN.md's estimate of 100 to 200 and a version 40 error-correction-level-H capacity of 1273 bytes. The payload uses **6.7 percent** of one maximum-size code.

**The library standardises on version 10 at level H**, whose byte-mode capacity is **119 bytes**, verified against the `segno` library rather than taken from a table. Every built-in fits with at least 34 bytes spare. Version 8 would fit ten of the fourteen multi-bull layouts and none of the zeroing sheets, and the 3.2 mm of margin it saves is not worth a library where four sheets have a different code size from the rest.

**A more useful property than the margin is that the margin does not depend on the number of bulls.** Parametric mode stores a grid, not a bull list, so the payload is the same size whether the grid holds 25 bulls or 36: GL-CF25-LTR and GL-RF36-LTR both encode to 55 bytes of body. Adding bulls costs nothing. What costs bytes is adding *kinds* of thing, which is why the 85-byte sheets are the ones carrying a data block plus one more optional block, and why the smallest is the one sheet with neither a sighter row nor a block.

The figures come from `tools/gltd/check.py`, which builds every definition from `tools/layout/layouts.json` rather than from its own copy of the geometry, so it cannot report sizes for a target that no longer exists.

### 5.4 The quantum, and pages larger than 409.5 mm

Coordinates in the explicit mode of section 5.5 are packed as 12-bit pairs, which addresses 0 to 4095 quanta. At the default 0.1 mm quantum that is 409.5 mm, which covers Letter, Legal, A4 and the short axis of A3, but not the long axis of A3 (420 mm), Tabloid (431.8 mm), or any roll media.

The `quantum` byte in the page block selects 0.1, 0.2, 0.5 or 1.0 mm, addressing 409.5, 819, 2047.5 and 4095 mm respectively. A 24 by 36 inch roll sheet needs 914.4 mm on its long axis and therefore the 0.5 mm quantum. Per section 2 this costs design freedom and no accuracy, since the renderer uses the same quantised values.

Parametric mode stores coordinates as full uint16 and is unaffected, addressing 6553.5 mm at the fine quantum. The 12-bit packing exists only to hit three bytes per bull in explicit mode.

### 5.5 Body, explicit mode (flags bit 0 = 1)

For arbitrary bull placement from the visual designer.

Page, ink, ring set, fiducial, code, data, tiling and grid blocks are as in parametric mode. The grid and sighter blocks are replaced by:

```
Bull block                              2 + 3n bytes
  bullCount   2 bytes  uint16
  for each bull, exactly 3 bytes:
    bits  0-11   x, uint12, in quanta
    bits 12-23   y, uint12, in quanta
  then a packed attribute run:
    for each bull, 4 bits:
      bits 0-2   ring set index, 0..7
      bit  3     scoring flag
    padded to a byte boundary
```

Three bytes of coordinate plus half a byte of attributes is **3.5 bytes per bull**, against DESIGN.md's estimate of roughly three bytes plus a shared geometry table. Forty arbitrarily placed bulls cost 2 + 140 + 20 = 162 bytes, and a complete explicit target with four inks and three ring sets lands at roughly **215 bytes**, comfortably inside DESIGN.md's under-500 estimate and at 17 percent of a single version 40 code.

Attributes are stored as a separate run rather than interleaved so that the coordinate array is a flat, aligned, memcpy-able block. This matters more than it sounds for the decoder on a phone.

**Marker family enumeration.** Assigned in the fiducial decision document; the byte is reserved here.

| Value | Family |
|---|---|
| 0 | none |
| 1 | ArUco 4x4, dictionary of 50 |
| 2 | ArUco 4x4, dictionary of 100 |
| 3 | ArUco 5x5, dictionary of 100 |
| 4 | ArUco 6x6, dictionary of 250 |
| 5 | AprilTag 16h5 |
| 6 | AprilTag 25h9 |
| 7 | AprilTag 36h11 |
| 8 | AprilTag Circle21h7 |
| 9-255 | Reserved |

### 5.6 Erasure coding

Engaged only when the body exceeds what one code can carry. The threshold is the binary capacity of the largest symbol the sheet has room for, which the generator knows.

Scheme: **Reed-Solomon over GF(256), k = 2, n = 4**. The body is split into two equal halves, padded, and two parity shares are computed. Each of the four corner codes carries one share with `shareCount = 4` and `shareK = 2`. Any two intact codes reconstruct the body. A sheet with only two codes, such as a tile, cannot use erasure coding and the generator says so rather than emitting an unreconstructable sheet.

This is strictly worse than replication for the common case, which is why replication is the default: with four identical codes, any **one** surviving code is sufficient. Erasure coding trades that down to needing two, in exchange for doubling capacity to 2 x (1273 - 15) = 2516 bytes. Given that section 5.3 shows the realistic worst case at 85 bytes and the explicit-mode worst case at around 215, this path is expected never to execute in practice. It is specified because DESIGN.md requires it and because a format that has no answer for its own overflow case is incomplete, not because anyone should expect to see it.

If a design exceeds even 2516 bytes, the generator refuses to print it and says why. It does not silently fall back to a lookup, because that would break rule R3.

### 5.7 Compression

Flag bit 1 selects raw DEFLATE on the body. The encoder applies it **only if the compressed body is strictly smaller**, and never for bodies under 128 bytes, where the DEFLATE header costs more than it saves. No built-in target reaches that threshold.

DEFLATE rather than a modern codec because `System.IO.Compression.DeflateStream` is in the .NET base class library on every target platform, and adding a dependency to save bytes on a payload already using seven percent of its budget would be a poor trade. Decoders must support it regardless of whether encoders emit it.

---

## 6. The definition identifier

```
id_bytes = SHA-256(canonical GLTD-B body, uncompressed, unsharded)[0..9]
id_text  = "GL-" + Crockford_Base32(id_bytes) grouped in fours
```

Ten bytes, eighty bits, rendered as sixteen Crockford base-32 characters: `GL-YCSK-DZZ1-R0VJ-4T5Y`, which is the identifier the reference encoder produces for the section 4 example.

Four properties fall out of hashing the binary body rather than the JSON.

**It costs nothing in the payload.** The identifier is not carried in the QR, because the reader computes it from what it decoded. Every byte of the budget goes to actual definition.

**It is canonical by construction.** JSON has whitespace, key ordering and number formatting; two files describing the same target need not be byte-identical. The binary body has exactly one legal encoding, so two definitions that describe the same target hash the same and two that do not, do not.

**It verifies the reconstruction.** DESIGN.md section 9 asks for a content hash so the application can confirm its reconstruction matches what was printed. The sheet carries the identifier as human-readable text. The application decodes the QR, rebuilds the body, hashes it, and compares against the printed text. A mismatch means the reconstruction is wrong, and is reported rather than swallowed.

**It is the library key.** The optional community library is addressed by this identifier, which makes it content-addressed and therefore incapable of serving a definition that is not the one asked for.

Eighty bits gives a fifty percent collision probability at roughly 2^40 distinct definitions, which is a trillion targets. Crockford base-32 was chosen over standard base-32 because it excludes I, L, O and U, so a user reading the identifier off a crumpled sheet cannot confuse one with zero.

**The canonical form is the projection, not the source document.** This needs stating plainly, because the obvious reading of conformance test 1 is not satisfiable and an implementer will otherwise assume a bug.

GLTD-J carries information GLTD-B does not: ink keys, ink roles, the name, description, author, licence and creation date, the cell `drawn` and `stroke` settings, label inks and border weights. None of that survives a trip through the binary body, and none of it should: the body exists to reconstruct a printable, analysable target from a QR code, not to be a serialisation of the document.

So the format defines a **projection**: the subset of a GLTD-J document that GLTD-B can carry, written in canonical form with synthesised keys. Decoding produces the projection. Conformance test 1 compares the projection of the source against the decode of its own encoding, and requires `J to B to J to B` to give identical bytes.

**One consequence is worth spelling out, because it decides an identifier.** The body's ink table stores each **distinct sRGB value** of the non-paper inks once, in declaration order, and every reference maps through the colour. The worked example in section 4 declares five inks: black, paper, fid, code and text. Four of those are `#000000`. Storing them literally would give a 64-byte body and a different identifier from the one printed in section 4. Deduplicating them gives 55 bytes and exactly `GL-YCSK-DZZ1-R0VJ-4T5Y`, which is what the reference encoder produces and what the section 4 document declares. The published identifier is only correct under the projection rule, which is the strongest argument for it.

Roles are partly recoverable on decode rather than stored: an ink referenced by `fiducials.ink` is fiducial, one painting a disc is artwork, and index 15 is paper. Keys are not recoverable and are synthesised as `ink0`, `ink1` and `paper`.

**A pitch along an axis holding one bull is unobservable, and the canonical body mirrors the other axis.** The grid block stores `cols`, `rows`, `pitchX` and `pitchY`. Where `cols` is 1 there is no second column to measure `pitchX` against, and since a decode emits no cells there is nothing else in the body that reveals it, so any value round-trips as well as any other and the identifier would depend on a number nobody can see. The canonical body therefore writes `pitchY` into `pitchX` when `cols` is 1, writes `pitchX` into `pitchY` when `rows` is 1, and writes zero into both when the grid holds a single bull, which is what the four zeroing sheets do. A body carrying anything else is not canonical and is rejected rather than normalised, on the same grounds as any other non-canonical body: silently rewriting it would give two identifiers for one target.

**One decoded ink per stored index, and a shared index takes the fiducial role.** The body stores one entry per distinct colour, so an index referenced by both `fiducials` and a disc, which is every built-in, decodes to a single ink. That ink carries the `fiducial` role, because section 3.7 requires the fiducial ink to have it and nothing anywhere requires a disc's ink to be `artwork`. Synthesising two inks of the same colour to keep the roles apart would re-encode to the same body and the same identifier, so it would buy nothing and cost a key that was never in the file.

**The `code` and `text` roles do not survive**, because the code block of section 5.2 carries no ink index and there is no text block at all. An earlier draft of this section claimed an ink referenced by `codes` decodes as the code role; it cannot, and adding the index would cost a byte and move every identifier in the library to buy back something that does not matter. A decoded definition renders its codes and its text in ink index 0, and a QR code has to be dark on light to scan at all, so the projection loses a colour choice that was never available in practice. A GLTD-J document may still declare `code` and `text` inks, and a renderer working from the document rather than from a decode uses them.

**It is invariant across tiles and across load data.** The body excludes the tile index and the whole `instance` block, so every sheet of a tiled assembly and every printing of a load-development sheet share one identifier. That is the correct behaviour in both cases: they are the same target.

Canonicalisation rules for the JSON side, so that a stored file also round-trips predictably: object keys in the order given in this specification, two-space indentation, no trailing whitespace, LF line endings, UTF-8 without BOM, and arrays in the order specified. Two fields are excluded from the computation. The `id` field is excluded from its own computation, since it is computed from the binary body and not from the JSON at all. The `instance` block is excluded because it is not part of the definition, per section 3.11.

---

## 7. Printing constraints the format has to respect

The format can express targets that cannot be printed usefully. The generator validates against these before it will emit a sheet.

**Two constants are named here and used by rule everywhere else.** The **safe margin is 120 dmm**, which covers a 3 mm scanner crop plus the no-print border of a consumer inkjet with room to spare, and nothing that has to survive printing may cross it. The **clearance is 30 dmm**, the minimum ink-free gap between any two major elements: bulls, codes, the data block and the grid field. Both are properties of paper and printers rather than of a design, which is why they are fixed rather than declared per sheet, and why the derivation rules of sections 3.7 and 3.8 can refer to them by name.

**QR module size has a floor.** A version 10 level H symbol is 57 modules across. At a 4 dmm module that is 22.8 mm, where one module is 4.7 device pixels at 300 DPI and 9.4 at 600. Below roughly 3 dmm per module on a consumer inkjet, dot gain begins to close the gaps between modules and level H's tolerance gets spent on the printer rather than on damage. The generator warns below 4 dmm and refuses below 3 dmm.

**Fiducial marker modules have a harsher floor than QR modules**, because a QR only needs to decode while a fiducial needs to yield a sub-pixel corner or centroid. The fiducial decision document sets the number. The format enforces whatever it sets through `markerSize` validation against `family`.

**Total ink coverage matters on inkjet.** Large filled areas on cheap paper cockle, and cockled paper is exactly the local warp the registration has to absorb. The validator computes coverage and warns above a threshold.

**Nothing may overlap.** Bulls, cell boundaries, fiducials with their quiet zones, codes with their quiet zones, and labels are checked pairwise. An overlap is an error, not a warning, because a fiducial with artwork inside its quiet zone is a fiducial that will not detect.

**Fiducials go where shots do not.** DESIGN.md section 9 places markers on cell boundaries for this reason. The validator checks that no marker centre falls within a configurable radius of a bull centre, defaulting to the outermost disc radius plus a margin.

**A derived fiducial scheme must leave enough markers.** The drop test can starve a coarse-pitch layout: `grid-boundary-1` on the 300 yard tile leaves two markers, which is not a registration. The validator errors below four surviving markers and warns below eight, and the fix is `grid-boundary-half-1`.

**The sighter row sits at 1.2 times the grid pitch.** Measured across the shipped sheets that have one, this is the modal ratio, and the library follows it. A layout whose sighter gap differs by more than 1 dmm from 1.2 times `pitchY`, and which does not declare `cells.sighterGap`, raises a warning. Declaring the field is the override, and it is a warning rather than an error because a deliberate departure is a legitimate design choice.

**The data block is a detection exclusion zone.** Its rectangle is declared geometry, which means the detection pipeline knows before it looks at the scan that everything inside it is printed matter and handwriting rather than bullet holes. Nothing in the format enforces this; the pipeline reads the rectangle and excludes it, and DETECTION-PIPELINE.md says where.

**Codes never overlap the data block, and there is 3 mm between them.** Where a sheet carries a load block, the bottom pair of codes sits above it rather than at the page corner. A generator that reserves room for the block and then draws the codes in the corners puts them inside it; that is not a hypothetical, it is what the first draft of the built-in library did on all eight sheets that carry a block.

**A layout that does not fit is an error, not a squashed layout.** A solver that computes the vertical room it needs must compare it against what the page has and fail, rather than placing the grid at its top limit and letting the last row run into whatever is below.

**Column-to-code clearance uses the clearance, not bare overlap.** A bull column that comes within 3 mm of a code band counts as clashing and forces the grid down. Testing for overlap alone allows a bull 0.15 mm from a code, which is what happened on the 300 yard Letter tile.

**No bull may cross a tile boundary.** On a tiled definition, every bull's outermost disc, its cell, its labels and its fiducials must lie entirely within one sheet of the assembly. A bull split across a seam has no measurable centre. Tile alignment itself is unconstrained, for the reason given in section 3.12.

**A measurement grid must leave a clear band.** The `field-ring-1` scheme needs somewhere to put markers, so the grid field plus a clearance must fit inside the safe margins with room for a marker row on each side. A full row needs the 60 dmm marker footprint plus a clearance each side, so 120 dmm. The built-in zeroing sheets leave 160 to 233 dmm of side band measured to the safe margin, which is comfortable, and 67 to 140 dmm above and below measured to the corner code rows, which is not. They carry 12 to 24 markers each. Where that vertical band is under 120 dmm the top and bottom marker rows survive only away from the code columns, which is a warning rather than an error, and it is the reason the marker counts differ so much between four sheets that look alike.

---

## 8. Versioning policy

**`gltd`, the major.** Incremented only for a change that an existing reader cannot safely ignore: a field whose meaning changes, a field removed, or a structural change. A reader meeting an unknown major refuses the document with a clear message naming the version it found and the versions it supports. It does not attempt a best effort.

**`revision`, the minor.** Incremented for additive changes: new optional fields, new enumerated values, new blocks behind a new flag bit. A reader meeting an unknown minor proceeds, and must preserve unknown fields through a load-and-save cycle so that a user with an older build does not silently strip a newer build's work.

**`wire`, the binary version.** Tracks `gltd` but is separate, because the binary encoding may need a change the JSON does not, or the reverse. A decoder meeting an unknown wire version stops. It cannot do otherwise, since it does not know the framing.

**Reserved bits and enum ranges are reserved, and must be written as zero.** A decoder must reject a frame with a reserved flag bit set rather than ignoring it, because a set reserved bit means the writer used a feature the decoder does not implement, and proceeding would produce a plausible but wrong target.

**Derivation rules are versioned in their names.** `grid-boundary-1` is immutable. An improved placement becomes `grid-boundary-2`. This is the mechanism that lets fiducial placement improve without invalidating targets already printed and shot, which will exist and will be re-analysed years later.

---

## 9. JSON Schema

Published at `https://grouplab.invalid/schema/gltd-1.schema.json`, versioned by path, and shipped in the repository so validation never requires network access. The URI is an identifier, not a fetch instruction.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://grouplab.invalid/schema/gltd-1.schema.json",
  "title": "GroupLab Target Definition, version 1",
  "type": "object",
  "required": ["gltd", "revision", "name", "units", "page",
               "inks", "ringSets", "bulls"],
  "additionalProperties": true,

  "$defs": {
    "dmm":      { "type": "integer", "minimum": 0, "maximum": 65535 },
    "inkKey":   { "type": "string", "pattern": "^[a-zA-Z0-9_-]{1,16}$" },
    "srgb":     { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" }
  },

  "properties": {
    "gltd":     { "const": 1 },
    "revision": { "type": "integer", "minimum": 0 },
    "id":       { "type": "string",
                  "pattern": "^GL-[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}$" },
    "name":        { "type": "string", "minLength": 1, "maxLength": 120 },
    "description": { "type": "string", "maxLength": 2000 },
    "author":      { "type": "string", "maxLength": 120 },
    "licence":     { "type": "string", "maxLength": 64 },
    "created":     { "type": "string", "format": "date" },
    "units":       { "const": "dmm" },

    "page": {
      "type": "object",
      "required": ["size", "width", "height"],
      "properties": {
        "size": { "enum": ["letter","legal","tabloid","a3","a4","a5",
                           "roll-24","roll-36","roll-42","custom"] },
        "width":  { "$ref": "#/$defs/dmm", "minimum": 500 },
        "height": { "$ref": "#/$defs/dmm", "minimum": 500 },
        "orientation": { "enum": ["portrait","landscape"] }
      },
      "additionalProperties": false
    },

    "inks": {
      "type": "array", "minItems": 1, "maxItems": 16,
      "items": {
        "type": "object",
        "required": ["key","srgb","role"],
        "properties": {
          "key":  { "$ref": "#/$defs/inkKey" },
          "srgb": { "$ref": "#/$defs/srgb" },
          "role": { "enum": ["artwork","fiducial","code","text","paper"] }
        },
        "additionalProperties": false
      }
    },

    "ringSets": {
      "type": "array", "minItems": 1, "maxItems": 15,
      "items": {
        "type": "object",
        "required": ["key","discs"],
        "properties": {
          "key": { "$ref": "#/$defs/inkKey" },
          "discs": {
            "type": "array", "minItems": 1, "maxItems": 15,
            "items": {
              "type": "object",
              "required": ["diameter","ink"],
              "properties": {
                "diameter": { "$ref": "#/$defs/dmm", "minimum": 1 },
                "ink":      { "$ref": "#/$defs/inkKey" }
              },
              "additionalProperties": false
            }
          }
        },
        "additionalProperties": false
      }
    },

    "bulls": {
      "type": "array", "minItems": 1, "maxItems": 4095,
      "items": {
        "type": "object",
        "required": ["x","y","ringSet","scoring"],
        "properties": {
          "x": { "$ref": "#/$defs/dmm" },
          "y": { "$ref": "#/$defs/dmm" },
          "ringSet": { "$ref": "#/$defs/inkKey" },
          "label":   { "type": "string", "maxLength": 8 },
          "scoring": { "type": "boolean" },
          "labelOffset": {
            "type": "object",
            "required": ["x","y"],
            "properties": { "x": { "type": "integer" },
                            "y": { "type": "integer" } },
            "additionalProperties": false
          }
        },
        "additionalProperties": false
      }
    },

    "cells": {
      "type": "object",
      "required": ["mode"],
      "properties": {
        "mode":   { "enum": ["none","grid","voronoi","explicit"] },
        "drawn":  { "type": "boolean" },
        "sighterGap": { "$ref": "#/$defs/dmm" },
        "ink":    { "$ref": "#/$defs/inkKey" },
        "stroke": { "type": "integer", "minimum": 0, "maximum": 255 },
        "grid": {
          "type": "object",
          "required": ["originX","originY","pitchX","pitchY","cols","rows"],
          "properties": {
            "originX": { "$ref": "#/$defs/dmm" },
            "originY": { "$ref": "#/$defs/dmm" },
            "pitchX":  { "$ref": "#/$defs/dmm", "minimum": 1 },
            "pitchY":  { "$ref": "#/$defs/dmm", "minimum": 1 },
            "cols":    { "type": "integer", "minimum": 1, "maximum": 255 },
            "rows":    { "type": "integer", "minimum": 1, "maximum": 255 }
          },
          "additionalProperties": false
        },
        "polygons": {
          "type": "array",
          "items": {
            "type": "array", "minItems": 3,
            "items": {
              "type": "object",
              "required": ["x","y"],
              "properties": { "x": { "$ref": "#/$defs/dmm" },
                              "y": { "$ref": "#/$defs/dmm" } },
              "additionalProperties": false
            }
          }
        }
      },
      "additionalProperties": false
    },

    "fiducials": {
      "type": "object",
      "required": ["scheme","family","markerSize","quietZone","ink"],
      "properties": {
        "scheme": { "type": "string",
                    "pattern": "^(explicit|[a-z0-9-]+-[0-9]+)$" },
        "family": { "enum": ["none",
                             "aruco-4x4-50","aruco-4x4-100",
                             "aruco-5x5-100","aruco-6x6-250",
                             "apriltag-16h5","apriltag-25h9",
                             "apriltag-36h11","apriltag-circle21h7"] },
        "markerSize": { "$ref": "#/$defs/dmm", "minimum": 20 },
        "quietZone":  { "type": "integer", "minimum": 0, "maximum": 255 },
        "ink": { "$ref": "#/$defs/inkKey" },
        "markers": {
          "type": "array", "maxItems": 1024,
          "items": {
            "type": "object",
            "required": ["id","x","y"],
            "properties": {
              "id": { "type": "integer", "minimum": 0, "maximum": 1023 },
              "x":  { "$ref": "#/$defs/dmm" },
              "y":  { "$ref": "#/$defs/dmm" }
            },
            "additionalProperties": false
          }
        }
      },
      "additionalProperties": false
    },

    "codes": {
      "type": "object",
      "required": ["count","ecLevel","moduleSize","placement","positions"],
      "properties": {
        "count":      { "type": "integer", "minimum": 0, "maximum": 8 },
        "version":    { "type": "integer", "minimum": 1, "maximum": 40 },
        "ecLevel":    { "enum": ["L","M","Q","H"] },
        "moduleSize": { "type": "integer", "minimum": 3, "maximum": 255 },
        "quietZone":  { "type": "integer", "minimum": 0, "maximum": 255 },
        "placement":  { "enum": ["corners-1","explicit"] },
        "humanReadableId": { "type": "boolean" },
        "positions": {
          "type": "array", "maxItems": 8,
          "items": {
            "type": "object",
            "required": ["x","y"],
            "properties": { "x": { "$ref": "#/$defs/dmm" },
                            "y": { "$ref": "#/$defs/dmm" } },
            "additionalProperties": false
          }
        }
      },
      "additionalProperties": false
    },

    "print": {
      "type": "object",
      "properties": {
        "scaling":    { "enum": ["none","fit"] },
        "minimumDpi": { "type": "integer", "minimum": 72, "maximum": 4800 },
        "colourMode": { "enum": ["mono","greyscale","colour"] },
        "duplex":     { "type": "boolean" },
        "generator":  { "type": "string", "maxLength": 64 },
        "notes":      { "type": "string", "maxLength": 1000 }
      },
      "additionalProperties": false
    },

    "dataBlock": {
      "type": "object",
      "required": ["x","y","width","height","layout","fieldSet","reserve","ink"],
      "properties": {
        "x":         { "$ref": "#/$defs/dmm" },
        "y":         { "$ref": "#/$defs/dmm" },
        "width":     { "$ref": "#/$defs/dmm", "minimum": 200 },
        "height":    { "$ref": "#/$defs/dmm", "minimum": 100 },
        "layout":    { "enum": ["fields-3x3-1","fields-3x2-1","explicit"] },
        "fieldSet":  { "enum": ["standard-9","standard-6","explicit"] },
        "reserve":   { "$ref": "#/$defs/dmm" },
        "ink":       { "$ref": "#/$defs/inkKey" },
        "labelInk":  { "$ref": "#/$defs/inkKey" },
        "border":    { "type": "integer", "minimum": 0, "maximum": 255 },
        "fields": {
          "type": "array", "maxItems": 32,
          "items": {
            "type": "object",
            "required": ["key","label"],
            "properties": {
              "key":    { "$ref": "#/$defs/inkKey" },
              "label":  { "type": "string", "maxLength": 32 },
              "x":      { "$ref": "#/$defs/dmm" },
              "y":      { "$ref": "#/$defs/dmm" },
              "width":  { "$ref": "#/$defs/dmm" },
              "height": { "$ref": "#/$defs/dmm" }
            },
            "additionalProperties": false
          }
        }
      },
      "additionalProperties": false
    },

    "instance": {
      "type": "object",
      "properties": {
        "serial":  { "type": "string", "pattern": "^[0-9A-HJKMNP-TV-Z]{4}$" },
        "printed": { "type": "string", "format": "date" },
        "values": {
          "type": "object",
          "additionalProperties": { "type": "string", "maxLength": 64 }
        }
      },
      "additionalProperties": false
    },

    "tiling": {
      "type": "object",
      "required": ["cols","rows","sheetWidth","sheetHeight","overlap"],
      "properties": {
        "cols":        { "type": "integer", "minimum": 1, "maximum": 16 },
        "rows":        { "type": "integer", "minimum": 1, "maximum": 16 },
        "sheetWidth":  { "$ref": "#/$defs/dmm", "minimum": 500 },
        "sheetHeight": { "$ref": "#/$defs/dmm", "minimum": 500 },
        "overlap":     { "$ref": "#/$defs/dmm" }
      },
      "additionalProperties": false
    },

    "grids": {
      "type": "array", "maxItems": 4,
      "items": {
        "type": "object",
        "required": ["key","centreX","centreY","half","divisions",
                     "majorEvery","unit","distance","distanceUnit"],
        "properties": {
          "key":          { "$ref": "#/$defs/inkKey" },
          "centreX":      { "$ref": "#/$defs/dmm" },
          "centreY":      { "$ref": "#/$defs/dmm" },
          "half":         { "$ref": "#/$defs/dmm", "minimum": 100 },
          "divisions":    { "type": "integer", "minimum": 1, "maximum": 100 },
          "majorEvery":   { "type": "integer", "minimum": 1, "maximum": 100 },
          "unit":         { "enum": ["moa","mil","inch","cm","custom"] },
          "distance":     { "type": "integer", "minimum": 1, "maximum": 5000 },
          "distanceUnit": { "enum": ["yd","m"] },
          "minorInk":     { "$ref": "#/$defs/inkKey" },
          "majorInk":     { "$ref": "#/$defs/inkKey" },
          "axisInk":      { "$ref": "#/$defs/inkKey" },
          "minorStroke":  { "type": "integer", "minimum": 1, "maximum": 255 },
          "majorStroke":  { "type": "integer", "minimum": 1, "maximum": 255 },
          "axisStroke":   { "type": "integer", "minimum": 1, "maximum": 255 },
          "labelStep":    { "type": "integer", "minimum": 0, "maximum": 100 },
          "labelInk":     { "$ref": "#/$defs/inkKey" }
        },
        "additionalProperties": false
      }
    }
  }
}
```

`additionalProperties` is `false` on every inner object but `true` at the top level, which is what implements rule R4: a future revision may add a new top-level block and an older reader will validate the document, preserve the block, and ignore it, whereas a typo inside a known block is caught rather than silently ignored.

Note that JSON Schema cannot express the cross-field constraints: that every `ink` reference resolves, that every `ringSet` reference resolves, that disc diameters within a set strictly decrease, that at most one ink carries the `paper` role, that named page sizes match their standard dimensions, that nothing overlaps, that derived fiducial markers match their recomputation, that no bull crosses a tile boundary, or that all geometry lies within the page. Those are the validator's job and section 10 lists them.

---

## 10. Conformance

An implementation is conformant when it passes all of the following. These are written as a test list because that is what they should become in Phase 2.

**Round trip.**

1. GLTD-J to GLTD-B to GLTD-J returns the byte-identical **canonical projection** of the source, for every target in the built-in library, and a further encode of that projection gives identical bytes. Section 6 defines the projection and explains why comparing against the source document itself is not a satisfiable test.
2. GLTD-B to GLTD-J to GLTD-B returns a byte-identical body, for a corpus of generated random valid definitions.
3. The definition identifier is stable across both round trips.
4. A definition round-tripped by a writer that does not understand a later revision's block retains that block unchanged.

**Decoding robustness.**

5. A frame with a bad CRC-32 is rejected, and the rejection names the reason.
6. A frame with an unknown `wire` version is rejected without a partial parse.
7. A frame with any reserved flag bit set is rejected.
8. A truncated frame is rejected rather than read past its end.
9. A `totalLen` exceeding the available bytes is rejected. A decoder must never allocate on an untrusted length before checking it.
10. With four replicated frames, any one intact frame reconstructs the definition.
11. With four erasure-coded frames, any two reconstruct the definition, and any one does not silently produce a wrong one.

**Validation.**

12. Unresolvable ink and ring set references are errors.
13. Geometry outside the page is an error.
14. Overlapping elements are errors, checked pairwise across bulls, cells, fiducials with quiet zones, codes with quiet zones, and labels.
15. A fiducial marker centre within the exclusion radius of a bull centre is an error.
16. A derived fiducial list that does not match its recomputation is an error.
17. A named page size whose dimensions differ from the standard is a warning, not an error.
18. A QR module size below the floor is an error; below the warning threshold is a warning.
19. A parametric layout whose `cells.grid` pitch is odd, while `fiducials.scheme` is a derived rule, is an error. The derived lattice would fall on non-integer coordinates. Under `grid-boundary-half-1` the pitch must be divisible by four.
20. Disc diameters within a ring set that do not strictly decrease are an error.
21. More than one ink carrying the `paper` role is an error.
22. A derived fiducial scheme leaving fewer than four surviving markers is an error; fewer than eight is a warning.
23. A sighter row whose gap differs from 1.2 times `pitchY` by more than 1 dmm, with no `cells.sighterGap` declared, is a warning.
24. On a tiled definition, any bull, marker, code or **drawn** cell boundary crossing a tile boundary is an error. An undrawn cell region is not artwork and is clipped by the sheet instead, per section 3.6; a derived cell lattice extending past the sheet edge is neither an error nor a warning.
24a. A stored `cells.grid` that does not equal the derivation from the bull grid is an error.
25. A `dataBlock` overlapping any bull, marker or code is an error, and its `reserve` square must fit within its `height`.
26. A measurement grid whose field leaves less than a marker footprint plus clearance inside the safe margins is an error. Less than a full marker row, the footprint plus a clearance each side, between the field and the code rows is a warning.
26a. A code overlapping a `dataBlock` rectangle is an error, and less than 30 dmm between them is a warning.
26b. A parametric layout needing more vertical room than the page provides is an error, reported with the shortfall.
26c. A bull column within 30 dmm of a code band must be treated as clashing when the rows are placed.
26d. Under `corners-1`, a stored `positions` entry that differs from the derived centre is an error. A definition with `count` greater than zero and no `positions` is invalid.
26e. A `dataBlock` whose `reserve` is greater than zero and less than 280 dmm carries no instance code, and a generator asked to print one on such a sheet refuses rather than shrinking the symbol.

**Print mode and instance data.**

27. The `blank` and `filled` print modes of the same definition produce identical geometry, identical fiducial placement and an identical definition identifier. Only the drawn contents of the data block differ.
28. Adding, changing or removing the `instance` block does not change the definition identifier.
29. A GLTD-I instance frame whose `defId` does not match the decoded definition is reported, not merged.
30. An instance payload exceeding the reserved code's capacity is refused at generation time, naming the field that overflowed.
31. A GLTD-I frame is never accepted by the GLTD-B decoder, and the reverse, on the strength of the magic bytes alone.

**Tiling.**

32. Every tile of an assembly encodes to a byte-identical body and therefore one definition identifier.
33. Tiles differ only in the `tileIndex` header byte, and a decoder recovers the correct tile from it.
34. Fiducial ids are unique across the whole assembly, or, where the dictionary is exhausted, the tile index disambiguates them and the generator has warned.
35. Shots pooled from separately scanned tiles produce the same composite group, to within measurement noise, as the same shots from an untiled sheet.

**Measurement grids.**

36. Every minor line of a measurement grid falls within half a dmm of its true angular offset, checked for all four built-in zeroing sheets.
37. Line positions are symmetric about the centre and monotone.
37a. Every derived boundary uses ties toward zero, per section 2. GL-ZERO-MIL-100Y is the fixture: `half` 732 over 8 divisions puts lines 1, 3, 5 and 7 on exact ties, and the correct offsets are 91, 274, 457 and 640. An implementation producing 92 and 458 has inherited its host language's ties-to-even default and is wrong.

**Rendering.**

38. Rendering the same definition twice produces identical output.
39. Rendering to PDF and to a 600 DPI raster produces geometry agreeing within half a device pixel.
40. Every geometric value used by the renderer is traceable to an integer in the definition, with no intermediate rounding.
41. A disc painted in a `paper` ink lays no ink in the PDF output, rather than laying white.
42. The ink extent of every annulus in the rendered output matches the difference of the two declared diameters to within half a device pixel, at 300 and at 600 DPI.

**The cross check that matters most.**

43. Generate a target, render it, analyse the rendered image as if it were a scan, and confirm that every bull centre is recovered at its declared coordinate to within the Phase 0 residual gate. This closes the loop between the format, the renderer and the analyser, and it is the test that catches a disagreement between the two halves of rule R5 before it reaches a user.

---

## 11. Open questions for review

1. **Fifteen inks and fifteen ring sets** are generous limits chosen to keep the packing tight, with index 15 spent on the paper knockout. If the visual designer needs more, the encoding changes. Worth confirming before Phase 2.

2. **Boustrophedon numbering** is listed as grid order 2 because some shooters number a grid in a serpentine. Include, or drop it as unnecessary?

3. **Explicit cell polygons** are specified but have no built-in target that uses them, and they are the only variable-length geometry in the format. There is a case for deferring them to revision 1 and shipping `none`, `grid` and `voronoi` only.

4. **The human-readable identifier is sixteen characters.** That is a lot of text on a sheet and a lot to type. Truncating the printed form to the first eight characters, forty bits, would still be unambiguous within any one user's library while remaining a weak global identifier. The full identifier stays in the file either way. Which behaviour do you want printed?

5. **A revision field for the target, distinct from the format revision.** If a user edits a built-in target, the definition identifier changes, which is correct and content-addressed. But there is then no link between the edited target and its parent. A `derivedFrom` field carrying the parent identifier would cost ten bytes in the payload and make the library's lineage visible. Worth it, or noise?

6. **The sheet serial.** The instance frame carries four Crockford characters, twenty bits, enough that one user will not collide with themselves. It is generated at print time and is the only way to tell two physical printings of the same definition and the same load apart. Should the serial also be printed as text in `blank` mode, or is the definition identifier enough?

7. **The standard field set is fixed at nine.** Adding a tenth standard field changes `fieldSet` enumeration but not the encoding size. Are the nine right? They are date, distance, cartridge, bullet, powder and charge, brass, primer, seating depth, notes.

8. **Instance data is not covered by the definition hash, and it is also not signed.** Anyone can print a sheet whose instance code claims any load. That is fine for a personal tool and it would not be fine for a competition record. Out of scope for now, worth writing down.

9. **The measurement grid block supports four grids per sheet** but every built-in zeroing sheet uses one. Two grids on a sheet, a coarse one for the first shot and a fine one for confirmation, is a design somebody will want. Leave the capacity, or spend the byte elsewhere?

10. **Four flagged blocks have no byte layout.** Flag bits 2 to 5 name a Cell block, a Label block, a Print block and an Extension block, and section 5.2 lists them without specifying their contents. Nothing in the built-in library needs any of them, and a decoder that meets one cannot do anything sensible. Until they are specified, an encoder must refuse a document that would need one, and a decoder must reject a frame that sets the bit. Which of the four are actually wanted?

11. **Values the body cannot carry have to be fixed somewhere.** A decoder has to produce something for `codes.version`, `codes.quietZone` and `codes.humanReadableId`, for the measurement grid `style` byte, and for the choice of quantum. The reference encoder assumes version 10, quiet zone 16, human-readable id true, style 1 meaning 2/3/4 dmm strokes with axis and label in the major ink, and quantum 0 in parametric mode. Those are defaults, not decisions; they should be written into section 5 as one or the other. One of them is now settled: `corners-1` fixes its footprint at 65 modules rather than deriving it from `codes.version`, so the code positions no longer depend on an invented value. The rest still do, and the same treatment probably suits them.

13. **`explicit` code placement has no byte layout.** `corners-1` needs none because the rule derives the centres, but a definition that sets placement to `explicit` has positions the binary cannot carry, in exactly the way flag bits 2 to 5 have blocks the binary cannot carry. Either the code block grows a position list behind a flag, or `explicit` is refused by the encoder and the enumeration exists for GLTD-J only. Nothing in the built-in library uses it.

12. **The erasure shares are named but not defined.** Section 5.6 specifies Reed-Solomon over GF(256) with k=2 and n=4 without saying which four shares. The reference implementation uses D0, D1, D0 xor D1, and D0 xor 2*D1 with polynomial 0x11D, which does let any two reconstruct the body. That should be in the specification rather than in one implementation.
