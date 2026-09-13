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

One choice goes beyond the reference encoder, which never emits explicit mode: **in explicit mode the encoder picks the smallest quantum in 0.1, 0.2, 0.5 and 1.0 mm that divides every bull coordinate exactly and keeps every coordinate within 4095 quanta**, and refuses the document if none does.

### Q12. Erasure shares

TARGET-SCHEMA.md section 11, question 12.

- Arithmetic over GF(256) with reducing polynomial 0x11D.
- The body is padded with zero bytes to an even length and split into halves D0 and D1.
- Shares 0 to 3 are D0, D1, D0 xor D1, and D0 xor 2*D1, where 2*D1 multiplies each byte by the field element 2.
- Every frame carries `totalLen` and `crc32` of the whole unpadded body, so reconstruction is checked end to end.
- Any two distinct shares reconstruct the body. One share alone is refused, never extrapolated.

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

- Each field is a 1-byte length followed by that many bytes of UTF-8, in field-set order.
- A field longer than 255 bytes is refused at generation time, naming the field. The 152-byte budget of section 3.11 is enforced separately and also names the overflowing field.

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
- **Data block rows.** Each row is `(height - 10) / rows` dmm tall, with 5 dmm above and below, which reproduces the 100 dmm rows section 3.10 gives for both built-in block heights. The reserved square is centred vertically at the right-hand end.
- **Canonical key order** for top-level blocks section 3.1 does not place: `codes`, `print`, `dataBlock`, `instance`, `tiling`, `grids`, then unknown fields in the order read.
- **Canonical key order inside blocks** follows the `properties` order of the section 9 schema, because the prose examples disagree with it in two places: section 3.8 writes `positions` before `humanReadableId` where the schema has it after, and the section 3.10 table lists `fields` before `reserve` where the schema lists it last. A test derives the expected order from the embedded schema, so a schema change moves the writer with it.
- **`srgb` is written in upper case.** Colour values are case-insensitive, and a canonical form needs one spelling.
