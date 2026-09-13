# GLTD 1.0 draft: implementation errata

**For** the Phase 0a implementation in `src/GroupLab.Core`
**Records** every place the C# implementation had to choose because TARGET-SCHEMA.md does not decide, and the open schema questions as they are currently implemented
**Status** Interim. Each entry is replaced by a schema citation once the specification settles it.

Rules the specification does settle are cited to their section in the code and tests, not repeated here. In particular the projection roles, including a shared ink index taking the fiducial role, are TARGET-SCHEMA.md section 6, and marker ids over the assembly lattice with the stored list belonging to tile 0 are section 3.7.

---

## Open schema questions, as implemented

### Q10. Flag bits 2 to 5 have no byte layout

TARGET-SCHEMA.md section 11, question 10.

- The encoder never sets bits 2 (cell block), 3 (label block), 4 (print block) or 5 (extension block).
- The encoder refuses a document that would need one of them: drawn cells, explicit cell polygons, or labels that differ from the default rule of section 5.2. The refusal names the field.
- The decoder rejects a frame with any of those bits set, naming the bit.

This matches the interim rule stated in the question and what `tools/gltd/encode.py` does.

### Q11. Values the body cannot carry

TARGET-SCHEMA.md section 11, question 11.

On decode, the projection writes:

| Field | Value |
|---|---|
| `codes.version` | 10 |
| `codes.quietZone` | 16 |
| `codes.humanReadableId` | true |
| measurement grid `style` byte 1 | `minorStroke` 2, `majorStroke` 3, `axisStroke` 4, `axisInk` and `labelInk` equal to the major ink |
| measurement grid `style` other than 1 | frame rejected, naming the value |
| page `quantum`, parametric mode | 0, meaning 0.1 mm |

One choice goes beyond the reference encoder, which never emits explicit mode: **in explicit mode the encoder picks the smallest quantum in 0.1, 0.2, 0.5 and 1.0 mm that divides every length the body stores in quanta** (bull coordinates, disc diameters, marker size and quiet zone, module size, the data block, tiling and measurement grid lengths, and a custom or roll page) **and keeps every bull within 4095 quanta and every one-byte field within 255**, and refuses the document if none does.

### Q12. Erasure shares

TARGET-SCHEMA.md section 11, question 12.

- Arithmetic over GF(256) with reducing polynomial 0x11D.
- The body is padded with zero bytes to an even length and split into halves D0 and D1.
- Shares 0 to 3 are D0, D1, D0 xor D1, and D0 xor 2*D1, where 2*D1 multiplies each byte by the field element 2.
- Every frame carries `totalLen` and `crc32` of the whole unpadded body, so reconstruction is checked end to end.
- Any two distinct shares reconstruct the body. One share alone is refused, never extrapolated.
- Erasure-coded frames are never compressed, so each share's length follows from `totalLen`. A decoder rejects a share frame with the DEFLATE bit set.

### Q13. Explicit code placement has no byte layout

TARGET-SCHEMA.md section 11, question 13.

**This deliberately diverges from `tools/gltd/encode.py`.** The reference encoder writes placement byte 1 and carries no positions, which produces a body from which no decoder can place the codes, contrary to rule R3. The C# encoder refuses `placement: explicit`, and the decoder rejects a frame whose placement byte is 1. No built-in sheet is affected.

---

## Choices the specification does not make

### C2. DEFLATE framing

Section 5.1 says `totalLen` is the body length, and section 5.7 allows a DEFLATE body, but neither says which length is stored when the body is compressed.

- `totalLen` is the **uncompressed** body length, consistent with `crc32` covering the uncompressed body.
- The compressed payload is the remainder of the frame after the 15-byte header.
- The decoder inflates into a buffer of exactly `totalLen` bytes and rejects the frame if the stream produces more or fewer.

### C3. GLTD-I field length prefix

Section 3.11 says the fields are "length-prefixed UTF-8" without giving the prefix width.

- Each field is a 1-byte length followed by that many bytes of UTF-8, in field-set order. An absent value is a zero-length field, and decodes as absent.
- A field longer than 255 bytes is refused at generation time, naming the field. The 152-byte budget of section 3.11 counts the field bytes including their length prefixes, and also names the overflowing field.
- The section 3.11 example measures 128 bytes of field data and 153 in total under this rule, exactly the figures the section quotes, which is the evidence for a 1-byte prefix.
- `printed` is a little-endian 24-bit count of days from 2000-01-01, and `crc32` covers the uncompressed field bytes.
- The standard nine keys are `date`, `distance`, `cartridge`, `bullet`, `powder`, `brass`, `primer`, `seating` and `notes`, as spelled in the section 3.11 example. `standard-6` drops `bullet`, `brass` and `seating`.

### C5. Conformance tests 26b and 26c on a stored definition

Tests 26b and 26c constrain a solver placing rows. Phase 0a has no generator; the built-in definitions come from `tools/layout/layouts.json`. The validator covers what a stored definition can show:

- Overlap between any two printed elements, and any element off the page, are errors.
- Warnings mirror `check()` in `tools/layout/layout.py` and `tools/layout/zero.py`:
  - two major elements (bulls, codes, data block, grid field) closer than 30 dmm
  - two markers closer than 20 dmm
  - any element within 60 dmm of the page edge
  - a `field-ring-1` band between the grid field and a code row below 120 dmm

### C6. Rendering conventions

The specification states what is printed but not where. These are drawing conventions only: none of them is read back by the analyser, and none changes the definition identifier.

- **Bull labels.** Left of the outermost disc, vertically centred on the bull, 15 dmm gap, 25 dmm cap height, Helvetica. Printed only on sheets with more than one bull.
- **Human-readable identifier.** Centred horizontally, baseline 80 dmm above the bottom page edge.
- **Measurement grid labels.** Inside the field, beside the axes.
- **Data block rows.** Row boundaries are `5 + round((height - 10) * r / rows)` under the tie rule of section 2, which reproduces the 100 dmm rows section 3.10 gives for both built-in block heights. The reserved square sits at the right-hand end, its top at `round((height - reserve) / 2)`.
- **Canonical key order** for top-level blocks section 3.1 does not place: `codes`, `print`, `dataBlock`, `instance`, `tiling`, `grids`, then unknown fields in the order read.
- **Canonical key order inside blocks** follows the `properties` order of the section 9 schema, because the prose examples disagree with it in two places: section 3.8 writes `positions` before `humanReadableId` where the schema has it after, and the section 3.10 table lists `fields` before `reserve` where the schema lists it last. A test derives the expected order from the embedded schema, so a schema change moves the writer with it.
- **`srgb` is written in upper case.** Colour values are case-insensitive, and a canonical form needs one spelling.

### C7. What a decode synthesises

Section 6 fixes ink keys and roles. The rest of what a decode must invent:

- `name` is the definition identifier, since the schema requires a name and the body carries none. `revision` is 0.
- Ring sets are keyed `set0`, `set1` and so on; measurement grids `grid0` and so on.
- The `paper` ink, previewed as `#FFFFFF`, is emitted only when index 15 is referenced.
- `fiducials.markers` is emitted from the scheme's derivation rule for tile 0, with identifiers, as section 3.7 asks writers to do, and omitted only when the rule cannot be derived from the decoded definition. `codes.positions` is always emitted, from `corners-1`.

### C8. Cells

Section 3.6 settles the lattice: on a parametric layout it is derived from the bull grid, `cells.grid` is optional and must equal the derivation (test 24a), and an undrawn cell region is clipped by the sheet. What remains a choice:

- A decode emits no `cells` block, since an undrawn grid lattice carries nothing the bull grid does not, which is also how the built-in library is written.
- The encoder accepts no `cells`, mode `grid` with or without a `cells.grid` that equals the derivation, or mode `none` on a sheet with one bull. Drawn cells, polygons, and any other mode on a multi-bull sheet need the cell block and are refused (question 10).

### C9. Recognising a parametric layout

- The scoring bulls come first and form a complete grid. The lowest order code that reproduces their array order is canonical, so a single row is always row-major.
- The pitch along an axis holding a single bull is TARGET-SCHEMA.md section 6: the other axis's pitch, or 0 for a single bull.
- Sighter rows split wherever `y` or the ring set changes. A row of one sighter stores the grid's `pitchX`, as `check.py` does.
- Anything else is explicit mode.
- An absent label is taken as the default label. A label that differs from the default, or any `labelOffset`, needs the label block and is refused (question 10).

### C10. Explicit mode packing

Section 5.5 gives the attribute run as 4 bits per bull without a bit order. The first bull of each byte is in the low nibble, and a padding nibble must be zero.

### C11. A trimmed named page

A named page whose dimensions differ from the standard, or a roll preset whose width differs from the roll's, is legitimate per section 3.2 but has no standard page code that carries its dimensions. It encodes as `custom` and decodes as `custom`.

### C12. The decoder refuses non-canonical bodies

Section 6 says a body has exactly one legal encoding. The decoder enforces it: a body whose decode would re-encode to different bytes is rejected, as is a body whose decode is not a valid GLTD-J document. This catches a repeated ink colour, an order code that is not the lowest that fits, and a code position the rule would place off the page.

---

## Gaps found while implementing, not yet listed as schema questions

### G1. Explicit fiducial placement has no byte layout

Section 3.7 requires `markers` when `scheme` is `explicit`, and section 5.2 assigns scheme byte 0 to `explicit`, but the fiducial block carries no marker list. This is the same gap as question 13 for codes. The encoder refuses `scheme: explicit` and the decoder rejects scheme byte 0.

### G2. Explicit data block layouts have no complete byte layout

Section 5.2 says explicit layouts "append 8 bytes of rect per field" and explicit field sets "append a length-prefixed key list", but gives neither the width of the length prefix nor anywhere to carry the field labels the schema requires. The encoder refuses `layout: explicit`, `fieldSet: explicit` and any `fields` list, and the decoder rejects byte 255 in either.
