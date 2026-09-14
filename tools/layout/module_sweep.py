"""Marker module sweep: FIDUCIAL-DECISION.md section 10, measurement 2, and PHASE1-BRIEF.md M0.

GL-CF25-LTR, with the arguments run.py declares for it, at 0.3, 0.4, 0.5, 0.6 and 0.8 mm modules. tag36h11 prints
as 8 modules across, a 6 by 6 data field inside a one-module black border, and TARGET-SCHEMA.md section 3.7 defines
markerSize as that square's edge, excluding the quiet zone; the quiet zone stays at two modules, the 1.0 mm at a
0.5 mm module that FIDUCIAL-DECISION.md settles. Only the marker footprint changes, so the bulls, codes and page are
the reference sheet's, and the lattice is re-derived with each footprint's drop test.

Writes scans/phase1/module-sweep/layouts.json; `grouplab sweep module` builds the definitions from it and checks
its own derivation against the marker lists here.
"""
import json, os
from layout import Layout

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "scans", "phase1", "module-sweep", "layouts.json")
MODULES_DMM = [3, 4, 5, 6, 8]
TAG_MODULES = 8      # tag36h11: a 6 by 6 data field in a one-module black border
QUIET_MODULES = 2    # FIDUCIAL-DECISION.md: a 1.0 mm quiet zone at a 0.5 mm module

rows = []
for m in MODULES_DMM:
    size, quiet = TAG_MODULES * m, QUIET_MODULES * m
    footprint = size + 2 * quiet
    l = Layout("GL-CF25-LTR", "letter", ring=254, pitch=380, cols=5, rows=5, sighter_cols=3, mark=footprint,
               note="Centrefire, 100 yd, 38.0 mm grid, Letter. Sighter variant")
    r = l.report()
    r.update(module_dmm=m, marker_size=size, quiet_zone=quiet, footprint=footprint,
             marks=[[x, y] for x, y in l.marks], all_errors=l.check()[0], all_warnings=l.check()[1])
    rows.append(r)
    print(f"module {m / 10:.1f} mm  marker {size:3d} dmm  quiet {quiet:2d}  footprint {footprint:3d}  "
          f"markers {r['markers']:3d}  dropped {r['dropped']:3d}  errors {len(r['all_errors'])}  warnings {len(r['all_warnings'])}")
    for e in r['all_errors']:
        print("      ERR", e)
    for w in r['all_warnings']:
        print("      warn", w)

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", newline="\n") as f:
    json.dump({"base": "GL-CF25-LTR", "sheets": rows}, f, indent=1)
    f.write("\n")
