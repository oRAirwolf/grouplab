# Marker module sweep

The sheets for `docs/FIDUCIAL-DECISION.md` section 10, measurement 2: residual and bull error against the printed module size of the fiducial markers. `GL-CF25-LTR` at five modules, with everything but the fiducials block identical to the built-in sheet. These are measurement sheets, not built-in library targets (`docs/PHASE1-BRIEF.md` M0).

| Module | `markerSize` | `quietZone` | Markers | Identifier | Definition | PDF |
|---|---|---|---|---|---|---|
| 0.3 mm | 24 dmm | 6 dmm | 44 | `GL-DFA3-H72S-8KKS-A00S` | `module-0.3mm.gltd.json` | `module-0.3mm.pdf` |
| 0.4 mm | 32 dmm | 8 dmm | 44 | `GL-78H9-DKWN-AHDV-4WAK` | `module-0.4mm.gltd.json` | `module-0.4mm.pdf` |
| 0.5 mm | 40 dmm | 10 dmm | 34 | `GL-YCSK-DZZ1-R0VJ-4T5Y` | `module-0.5mm.gltd.json` | `module-0.5mm.pdf` |
| 0.6 mm | 48 dmm | 12 dmm | 34 | `GL-683J-3ZR8-60D5-0FGG` | `module-0.6mm.gltd.json` | `module-0.6mm.pdf` |
| 0.8 mm | 64 dmm | 16 dmm | 34 | `GL-SEBE-5F06-GVTF-3CZK` | `module-0.8mm.gltd.json` | `module-0.8mm.pdf` |

**Five Letter pages in total, one per PDF.** Print each at 100 percent, not fit to page, on the printer and paper of the Phase 0 sample set.

`tag36h11` prints as 8 modules across its black square, and `markerSize` is that square's edge (TARGET-SCHEMA.md section 3.7), so a module of m is a marker of 8m. The quiet zone is two modules, the 1.0 mm at 0.5 mm that FIDUCIAL-DECISION.md settles. The 0.5 mm sheet has the identifier of the built-in `GL-CF25-LTR`, the sheet the Phase 0 sample set was printed from, because an identifier hashes the geometry and not the name.

**Regenerate** with, from the repository root:

```
python -B tools/layout/module_sweep.py
grouplab sweep module tools/layout/layouts.json scans/phase1/module-sweep/layouts.json scans/phase1/module-sweep
```

The first writes `layouts.json`, the lattice `tools/layout/layout.py` derives at each marker footprint. The second builds each definition, checks its derived lattice against that file marker for marker, validates it, runs conformance test 43 at 300 and 600 DPI, and writes the definition and its PDF. `ModuleSweepTests` fails if the committed definitions stop matching.

**Once printed, these become inputs to a measurement.** Per `docs/NOTES-FROM-PLANNING.md` entry 11, freeze the printed definitions under `targets/frozen/` by identifier before any of them changes.
