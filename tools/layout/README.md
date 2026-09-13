# GroupLab layout validators

Geometry solvers and validators for the built-in target library. Units are
**dmm**, tenths of a millimetre, per TARGET-SCHEMA.md rule R1. No application
code: these exist to prove the library's geometry is placeable before any
renderer is written.

## Files

| File | What it does |
|---|---|
| `layout.py` | `Layout`, the multi-bull solver. Centres the grid, places rows against the code bands, reserves the load block, generates the fiducial lattice, checks every pair of elements for overlap |
| `zero.py` | `ZeroSheet`, the zeroing-sheet solver. Computes integer grid lines from true angular offsets, places the `field-ring-1` marker ring, checks clearances |
| `run.py` | The shipped library. Builds all twenty sheets and prints the report |
| `layouts.json` | Output of the last run: full geometry for every sheet |

## Running

```
python3 run.py
```

Expected output ends with:

```
16 multi-bull layouts, 4 zeroing sheets, 0 failing
```

Any other number is a regression.

## Constants

```
SAFE = 120   # 12.0 mm safe margin: 3 mm scanner crop + 3 mm inkjet no-print, x4
MARK =  60   # ArUco 4.0 mm marker + 1.0 mm quiet zone each side
QR   = 260   # v10-H at 0.4 mm module: 22.8 mm symbol + 1.6 mm quiet each side
CL   =  30   # 3.0 mm clearance between any two printed elements
```

`QR` is the one to watch. It is a v10-H footprint because the largest payload
in the library is 85 bytes and v8-H holds 84. Changing it moves every row on
every sheet.

## Rules asserted

- Grid pitch must be even in dmm, so the `grid-boundary-1` half-pitch lattice
  lands on integers.
- Under `grid-boundary-half-1` the pitch must be divisible by 4 dmm.
- No two printed elements may overlap, including quiet zones.
- Nothing may sit inside the safe margin.
- On a zeroing sheet, no marker or code may intrude on the grid field.
