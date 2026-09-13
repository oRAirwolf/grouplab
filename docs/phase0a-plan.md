# Phase 0a: GLTD format, built-in library, PDF renderer (revision 3)

## Context

`docs/PHASE0-BRIEF.md` makes Phase 0a the first code in the repository. It delivers:

- the GLTD-J reader, writer and validator
- the GLTD-B encoder and decoder
- the twenty built-in definitions in `targets/`
- a PDF renderer

The gate is conformance test 43: render, analyse the render as a scan, and recover every bull centre within 0.001 in. Secondary gates are tests 1 to 26e and 36 to 42.

Two review rounds changed the documents and tools. Every finding from plan revisions 1 and 2 is fixed at source, `corners-1` now fully specifies code placement, and schema questions 10 to 13 remain open.

**Rechecked against the current working tree:**
- `check.py` passes its `corners-1` cross-check: "22 definitions, largest frame 85 bytes, headroom 34".
- `run.py`: "16 multi-bull layouts, 4 zeroing sheets, 0 failing". The only warnings are the narrow marker band on GL-ZERO-MOA-100Y and GL-ZERO-MIL-100M.
- `layouts.json` matches a fresh run.
- My own pairwise check, independent of the solvers, finds no overlap, no fit failure, and no major pair under 30 dmm on any of the twenty sheets.
- Zeroing sheets: markers in raster order, data block cells at 0/563/1126/1689, rows of 100 dmm.

**Decisions already taken:**
- net10.0, with the .NET 10 SDK installed by winget, and a DESIGN.md section 20 note.
- The projection rule (TARGET-SCHEMA.md section 6).
- Our own PDF writer in Core.
- `Net.Codecrete.QrCodeGenerator` for QR codes.
- `PDFtoImage` for test-only rasterising.
- `OpenCvSharp4` with the full `runtime.win`, behind `IImagingBackend`.

## One precondition: the solver fixes from review 1 are not committed

`git status` shows `tools/layout/layout.py` and `tools/layout/run.py` still modified. Commit 5a5fad6 included the regenerated `layouts.json` but not those two files.

I ran HEAD's own solver in memory, with no files written. It prints "0 failing" but does **not** reproduce HEAD's `layouts.json` on any of the 16 multi-bull sheets:
- the wrong CF30 pitch and tile ring
- CF25-LTR-D rows
- code positions
- marker counts

A fresh clone would therefore have an authority that disagrees with its own output. No geometry needs to change, only the commit. **Please commit those two files, or tell me to, before M0.** I will not touch `tools/` or the untracked `Claude outputs/` folder otherwise.

## Open specification questions: the reference encoder's behaviour, with each choice flagged

Every one of these goes into `docs/SPEC-ERRATA.md`, quoting the schema question it answers.

**Q10: flag bits 2 to 5.** The encoder refuses documents needing a Cell, Label, Print or Extension block: drawn or explicit cells, or non-default labels. The decoder rejects a frame that sets any of those bits, naming the bit.

**Q11: values the body cannot carry.** On decode:
- `codes.version` 10, `quietZone` 16, `humanReadableId` true
- grid `style` 1 means strokes of 2, 3 and 4 dmm, with axis and label in the major ink; any other value is rejected
- parametric mode uses quantum 0
- explicit mode uses the smallest quantum that divides every coordinate and keeps it within 4095

**Q12: erasure shares.**
- The shares are D0, D1, D0 xor D1 and D0 xor 2*D1 over GF(256), polynomial 0x11D.
- Share payload length is ceil(totalLen / 2).
- `totalLen` and the CRC describe the whole body.

**Q13: explicit code placement. This is a divergence, flagged.**
- `encode.py` writes placement byte 1 and silently drops the positions, producing a body no decoder can place codes from, which breaks rule R3.
- The C# encoder refuses `explicit` instead, and the decoder rejects placement 1.
- No built-in sheet is affected.

**Choices no document settles:**
- **C1. Roles on decode.**
  - Decoding emits one synthesised ink per colour and role pair actually referenced: `ink0`, `ink1`, … in order of first use, then `paper`.
  - A black ink used by both discs and fiducials therefore becomes one artwork ink and one fiducial ink. That satisfies the fiducial-role rule and deduplicates back to the same body.
  - The code block has no ink index, so the "code" role cannot be recovered despite section 6. Codes and text render in the declared `code` or `text` ink, or `#000000` if none is declared.
- **C2. DEFLATE framing.** `totalLen` is the uncompressed length, the compressed payload is the rest of the frame, and inflation is capped at `totalLen`.
- **C3. GLTD-I field lengths** use a 1-byte prefix. A field over 255 bytes is refused, naming the field.
- **C4. Marker ids.**
  - Ids number the surviving positions in raster order, in assembly coordinates, so tile ids are unique across the assembly.
  - A tiled definition's stored `markers` list is tile 0's.
- **C5. Tests 26b and 26c** constrain a generator, which Phase 0a doesn't have. The validator covers what a stored definition can show:
  - overlap and off-page are errors
  - warnings mirror the two solvers' `check()`: 30 dmm between major elements, 20 dmm between markers, 60 dmm tight edge, and the `field-ring-1` band below 120 dmm
- **C6. Renderer conventions.**
  - Labels sit left of the outer disc, vertically centred, with a 15 dmm gap and 25 dmm cap height, and only on sheets with more than one bull.
  - The human-readable id is centred with its baseline 80 dmm above the bottom edge.
  - Grid labels sit inside the field beside the axes.
  - Data block rows are `(height - 10) / rows` high, and the reserve square is centred vertically.
  - Canonical key order for blocks with no stated position: codes, print, dataBlock, instance, tiling, grids, then unknown fields.

## Layout

```
Directory.Build.props      net10.0, nullable, warnings as errors, deterministic
GroupLab.slnx
src/GroupLab.Core/         no platform or OpenCV dependency
  Gltd/Model/              immutable records per schema section 3; top-level unknown-field bag (R4)
  Gltd/Json/               reader (JsonNode), CanonicalJsonWriter (2-space, LF, UTF-8 without BOM)
  Gltd/Schema/             gltd-1.schema.json, section 9 verbatim, embedded
  Gltd/Validation/         Validator -> Diagnostic{Severity, TestNo, Path, Message}
  Gltd/Binary/             BodyEncoder/Decoder, FrameCodec, Crc32, Crockford, DefinitionId,
                           Projection, ErasureCoder, InstanceCodec (GLTD-I)
  Gltd/Derivation/         GridBoundary1, GridBoundaryHalf1, FieldRing1, Corners1,
                           DataBlockLayout, MeasurementGridLines (integer, round(e*i/n))
  Rendering/               Scene (dmm display list), SceneBuilder, Pdf/PdfWriter, Pdf/HelveticaMetrics,
                           Markers/Tag36h11 (libapriltag table, BSD-2 notice), SceneRasterizer (no text)
  Imaging/IImagingBackend  DetectMarkers, FindHomography (RANSAC), Warp
src/GroupLab.Cli/          validate, encode, decode, render, library build|verify, selftest
  Imaging/OpenCvSharpBackend.cs
tests/GroupLab.Core.Tests/ xUnit; PDFtoImage test-only
  Fixtures/gltd-check.json      body hex, frame hex, id for all 22 rows, dumped from check.py's sheet()/zero()
  Fixtures/layout-markers.json  multi-bull marker lists dumped from layout.py (not in layouts.json)
targets/                   20 canonical GLTD-J files + 2 tile 3x2 preset files
docs/SPEC-ERRATA.md        Q10 to Q13 as implemented, C1 to C6
THIRD-PARTY-NOTICES.md
```

Fixtures are dumped by `python -B` scripts kept in the scratchpad, at M2 time, from whatever `tools/` holds then. No identifier is copied from any document or earlier list.

## Milestones, each a commit on branch `phase-0a`

**M0. Toolchain and scaffold.** Precondition: the two solver files are committed.
- Install the SDK.
- Create the solution, the three projects and the props file.
- Write the errata document and add the DESIGN.md section 20 note.

**M1. Model and JSON.**
- Reader and canonical writer.
- Schema validation, including `positions` required when `count > 0`.
- Unknown top-level fields preserved (test 4).
- The section 4 example is golden-tested through the projection.

**M2. GLTD-B encoder and decoder.** Built before any derivation or rendering code.
- **Body encoder**, per section 5.2: distinct-colour inks, placement byte 0 for `corners-1`, and `DBLAYOUT` values matching `encode.py`. It infers the grid and sighter rows from `bulls` and `cells.grid`, where the bull origin is `originX + pitchX/2`, and uses explicit mode (section 5.5) otherwise.
- **Frame reader**, checking in order before any allocation:
  1. length
  2. magic
  3. wire version
  4. reserved bits
  5. Q10 bits
  6. placement byte
  7. `totalLen` against the bytes present
  8. share fields

  Inflation is then capped, and the CRC is checked last.
- **Parity with `check.py`:** all 22 rows must match byte for byte (body, frame, id).
- **Tests 1 to 11.** Test 2 uses random valid definitions. Truncated and oversized frames are fuzzed. Test 31 checks that GLTD-I and GLTD-B frames are never accepted in each other's place.

**M3. Derivations, validator, library.**
- **Port exactly:**
  - the `layout.py` lattice and drop test: edge 60 dmm, code 20 dmm, ring box 10 dmm, data block 10 dmm, sighter-band rows merged within 80 dmm
  - `zero.py` `field-ring-1`: candidates, drop test including the data block, raster sort
  - `corners-1` as written in section 3.8
  - data block cells, and grid lines
- **LibraryBuilder** mirrors `check.py` `sheet()` and `zero()`:
  - pages from the page table
  - the `STACKS` disc table and the five inks
  - codes with `version`, `quietZone`, `placement` and `humanReadableId`, and `positions` from `corners-1`
  - `DB9` and `DB6` (zeroing sheets: `fields-3x2-1`, `standard-6`, `reserve` 210)
  - the 3x2 tile presets
  - canonical `markers` lists
- **`library verify` compares against `layouts.json` and the fixtures, and requires all 22 identifiers to equal `check.py`'s:**
  - bull and sighter coordinates
  - `qr` positions
  - `data_block_rect`
  - marker lists, counts and dropped counts
  - zeroing `cx`/`cy`, offsets and maximum deviation
  - stored `markers` and `positions` against their recomputation (tests 16 and 26d), where a mismatch is an error
- **Validator, tests 12 to 26e, 36 and 37.**
  - Expected on all twenty sheets: **zero errors**.
  - Expected warnings: exactly the narrow-band warnings, top and bottom, on GL-ZERO-MOA-100Y and GL-ZERO-MIL-100M.
  - No skips and no known-error assertions.

**M4. Renderer.** Starts after M2 and M3 are green.
- **Page setup.** A single `cm` matrix of 72/254, flipped in y, so paths are written in definition dmm.
- **Disc stacks become annuli.** Each band is disc i minus disc i+1, filled even-odd. Paper bands emit nothing and adjacent bands of the same ink merge. Circles use 8 Bézier segments.
- **Markers** are tag36h11 modules at `markerSize/8`, drawn as merged rectangles, with the quiet zone undrawn.
- **QR codes** are fixed at version 10-H with a 4 dmm module, at `positions`, and each page's frame carries its `tileIndex`.
- **Text** is Helvetica, a standard-14 font.
- **Data block modes.**
  - `blank` draws rules as filled rectangles inside the edge, plus captions.
  - The reserve holds the id and serial as text, in both modes whenever the reserve is under 280 dmm.
  - `filled` adds the typed values, plus a GLTD-I version 11-Q code only when the reserve is at least 280 dmm.
  - Asking for an instance code on a smaller reserve is refused (test 26e).
- **Output options:** tiles as N pages, and `--scale 0.962`.
- **Determinism:** no timestamps, and `/ID` derived from a content hash.
- **Tests 27, 38, 40 and 41.** Test 41 inspects the content stream for no white fills and no paper operators.

**M5. Rendering gates and test 43.**
- **OpenCvSharpBackend** detects tag36h11 with:
  - `CORNER_REFINE_SUBPIX`
  - an adaptive threshold window sized from the expected marker size in pixels
  - the S2 shape gates: area below half the expected area, or side ratio above 2.0, rejected before bit sampling
  - verification of the embedded code table by rendering and detecting all 587 ids
- **Test 42.** At 300 and 600 DPI, 16 radial profiles per bull, with 50 percent crossings to sub-pixel. Annulus edges must match the declared radii within 0.5 px.
- **Test 39.** The PDFium raster and the SceneRasterizer raster must agree on ring and marker edges within 0.5 px.
- **Test 43**, on GL-CF25-LTR at 600 DPI and all twenty sheets at 300 DPI:
  1. Rasterise the PDF.
  2. Apply a known perturbation: 0.7° rotation, 0.15° shear, 0.999 by 1.001 scale, an offset, and a 0.12 in crop.
  3. Detect markers and fit a RANSAC homography; residual must be below 0.001 in.
  4. For each bull, iterate an ink-weighted centroid over a window just larger than the outer disc, predicted through the homography, and map it back to the page.
  5. Require error below 0.254 dmm.
  6. The 96.2 percent render must report a scale of 0.962 ± 0.0005.
- `selftest` prints the same table. The 12-marker GL-ZERO-MIL-100M and the 9-marker tile are reported individually, since they are the thinnest registrations.

## Verification

1. `dotnet build` and `dotnet test` are all green, with no skips.
2. `python -B tools/gltd/check.py` against `dotnet run --project src/GroupLab.Cli -- encode --check-fixtures`: identical sizes and ids on all 22 rows.
3. `library verify`: all 22 ids equal `check.py`, no geometry mismatch against `layouts.json`, and exactly the two expected zeroing warnings.
4. `render targets/GL-CF25-LTR.gltd.json -o out/GL-CF25-LTR.pdf`, opened at 100 percent for inspection, and `validate` on every file in `targets/`.
5. `selftest`: residual and worst bull error under 0.001 in on every sheet, printed.
6. Report the errata (Q10 to Q13 as implemented, C1 to C6) and the brief section 4 print list, all of which validate clean.
