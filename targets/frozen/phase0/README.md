# Frozen definitions: the Phase 0 sample set

These are the definitions the Phase 0 sample set in `scans/phase0/` was printed from, as canonical GLTD-J, each named by the identifier printed on the sheet.

**They are inputs to a measurement, and they are never edited.** Every table in `docs/PHASE0-RESULTS.md` and every file in `scans/phase0/measurements/` was measured against them, and `grouplab spike` resolves its definitions here rather than in the live library in `targets/`. A definition's identifier changes whenever its geometry does, so a measurement against paper stays reproducible only while the geometry it was printed from stays in the tree (`docs/NOTES-FROM-PLANNING.md` entry 11).

**If a future schema or validator change stops one of these loading or validating, that is a finding to report, not a fixture to repair.** `FrozenDefinitionTests` loads each file, checks that its identifier matches its file name, and validates it. It exempts test 26f alone, and only on the three sighters of `GL-YCSK-DZZ1-R0VJ-4T5Y`, which sit outside its marker lattice by construction. That is the defect of `docs/PHASE0-RESULTS.md` section 4.4, fixed in the live library by the geometry change of `docs/NOTES-FROM-PLANNING.md` entry 13, which also made test 26f an error. So this definition no longer validates clean: that is recorded as the finding, and the file stays as printed. Code that renders it as a measurement input sets `AllowInvalid`, because the paper was printed from it regardless.

| File | Live definition when printed | Printed as | Superseded by |
|---|---|---|---|
| `GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json` | `GL-CF25-LTR` | Sheets 1, 2 and 3, and the sheet printed at 96.2 percent | `GL-20J3-Y141-0BN3-EYME`: sighter gap 456 to 454, scoring rows 1 dmm lower and the sighter row 1 dmm higher, 34 markers to 38 (`docs/NOTES-FROM-PLANNING.md` entry 13) |
| `GL-R0T0-384Z-HRBE-M0EW.gltd.json` | `GL-CF25-LTR-D` | The load block, blank and filled | Not superseded |
| `GL-G8JP-FF4D-AE0T-GPMN.gltd.json` | `GL-LR300-T`, 2 by 2 assembly | Tiles 1 to 4 | Not superseded |

When a definition here is superseded, record the superseding identifier in this table in the same commit that changes the live definition. A future sample set gets its own directory beside this one.
