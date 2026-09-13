# GroupLab

GroupLab measures how accurately a rifle shoots, and reports the result with the statistical honesty the measurement deserves. GroupLab is a working name and may change.

You print a target sheet that GroupLab generates: a grid of small bullseyes and machine-readable registration markers. You fire one shot per bullseye, then scan the sheet or photograph it. GroupLab then:

- finds every bullet hole
- corrects for scanner skew and lens distortion
- measures each hole against its own bullseye
- combines all the shots into one group, with confidence intervals, significance tests and sample-size planning

The printed sheet describes itself. Every sheet carries QR codes that hold the complete target definition, so software that has never seen the design can still analyse it.

## Status

**Phase 0a is complete.** It delivers:

- the GLTD target definition format, in JSON (GLTD-J) and in the binary QR payload (GLTD-B)
- a validator
- the twenty built-in target sheets
- a PDF renderer
- a synthetic-scan check that renders each sheet, analyses the image as if it were a scan, and recovers every bull centre within one thousandth of an inch

Not built yet: the application, the user interface, hole detection and statistics. The phase plan and each phase's gate are in DESIGN.md section 21.

## What GroupLab is not

- **Not compatible with OnTarget, in any way.** GroupLab works with no OnTarget PC or OnTarget TDS file format, target design or import path, and never will. The files under `reference/ontarget-output/` show what the incumbent produces; they are not something to replicate.
- **Not a home for pseudoscience.** Barrel harmonics, optimal barrel time, velocity nodes and accuracy nodes are not real. They appear nowhere in GroupLab: not in the ballistic solver, the statistics, the documentation, the interface or the code.
- **Not a commercial product.** There is no paid tier, licence key or commercial offering.
- **Not an electronic target system.** GroupLab does no acoustic scoring or electronic target hardware integration.

## Where to start

1. **[DESIGN.md](DESIGN.md)**, the design document: what GroupLab is for, how it works, and the build plan.
2. **[docs/TARGET-SCHEMA.md](docs/TARGET-SCHEMA.md)**, the GLTD 1.0 format that everything in Phase 0a implements, including the conformance tests of section 10.

After those two:

| Document | Covers |
|---|---|
| [docs/PHASE0-BRIEF.md](docs/PHASE0-BRIEF.md) | The brief for Phases 0a and 0 |
| [docs/FIDUCIAL-DECISION.md](docs/FIDUCIAL-DECISION.md) | Why the markers are AprilTag `tag36h11`, and the measurements behind it |
| [docs/DETECTION-PIPELINE.md](docs/DETECTION-PIPELINE.md) | The eleven-stage analysis pipeline |
| [docs/TARGET-LIBRARY.md](docs/TARGET-LIBRARY.md) | The twenty built-in sheets |
| [docs/STATISTICS.md](docs/STATISTICS.md) | Estimators, tests, and validation against shotGroups |
| [docs/SCAN-MEASUREMENTS.md](docs/SCAN-MEASUREMENTS.md) | Measurements from the real scans under `scans/` |
| [docs/SPEC-ERRATA.md](docs/SPEC-ERRATA.md) | Where the implementation had to choose because the specification does not |

## Repository layout

| Path | Contents |
|---|---|
| `src/GroupLab.Core` | Format, validation, derivations, renderer and registration, with no platform or OpenCV dependency |
| `src/GroupLab.Cli` | The `grouplab` command line, and the OpenCV imaging backend |
| `tests/GroupLab.Core.Tests` | The conformance tests of TARGET-SCHEMA.md section 10 |
| `targets/` | The built-in definitions, generated from `tools/layout/layouts.json` |
| `tools/` | Python and R reference tools; the authority for geometry, identifiers and statistics fixtures |
| `scans/`, `SAMPLE-NOTES.md` | Real scanned targets, and notes on each |

## Building

See [CONTRIBUTING.md](CONTRIBUTING.md). In short, on Windows with the .NET 10 SDK:

```
dotnet build
dotnet test
dotnet run --project src/GroupLab.Cli -- render targets/GL-CF25-LTR.gltd.json -o out/GL-CF25-LTR.pdf
```

## Licence

DESIGN.md section 20 sets the licence as GPL-3.0, with an additional permission under section 7 for app-store distribution. The licence file itself has not been added yet. Work by others that GroupLab includes or depends on is listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
