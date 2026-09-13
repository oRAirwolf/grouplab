from layout import Layout
from zero import ZeroSheet, MOA_100Y, MIL_100Y, MOA_100M, MIL_100M
import json

DB_FULL = 310   # 31.0 mm: 9 fields in 3 rows plus the instance code
DB_ZERO = 210   # 21.0 mm: 6 fields in 2 rows plus the instance code
DB_NONE = 0
HALF    = "grid-boundary-half-1"

L = [
 # --- centrefire load development ---
 Layout("GL-CF25-LTR", "letter", ring=254, pitch=380, cols=5, rows=5, sighter_cols=3,
        note="Centrefire, 100 yd, 38.0 mm grid, Letter. Sighter variant"),
 Layout("GL-CF25-LTR-D", "letter", ring=254, pitch=380, cols=5, rows=5, sighter_cols=0,
        data_block=DB_FULL,
        note="Centrefire, 100 yd, Letter. Load-block variant, no sighters"),
 Layout("GL-CF25-A4", "a4", ring=254, pitch=380, cols=5, rows=5, sighter_cols=3,
        note="Centrefire, 100 yd geometry on A4"),
 Layout("GL-CF25-100M-A4", "a4", ring=254, pitch=400, cols=5, rows=5, sighter_cols=3,
        note="Centrefire, TRUE 100 m angular sizing, A4"),
 Layout("GL-CF30-LTR", "letter", ring=222, pitch=356, cols=5, rows=6, sighter_cols=0,
        note="Centrefire dense, 30 scoring, Letter"),
 # --- rimfire ---
 Layout("GL-RF25-LTR", "letter", ring=152, pitch=254, cols=5, rows=5, sighter_cols=5,
        data_block=DB_FULL, note="Rimfire 50 yd, Letter, with load block"),
 Layout("GL-RF25-A4", "a4", ring=152, pitch=254, cols=5, rows=5, sighter_cols=5,
        data_block=DB_FULL, note="Rimfire 50 m, A4, with load block"),
 Layout("GL-RF36-LTR", "letter", ring=127, pitch=254, cols=6, rows=6, sighter_cols=4,
        note="Rimfire dense, 36 scoring, Letter"),
 # --- large format ---
 Layout("GL-LR25-TAB", "tabloid", ring=381, pitch=508, cols=5, rows=5, sighter_cols=3,
        data_block=DB_FULL, note="100 to 200 yd, Tabloid, with load block"),
 Layout("GL-LR25-A3", "a3", ring=381, pitch=508, cols=5, rows=5, sighter_cols=3,
        data_block=DB_FULL, note="100 to 200 yd, A3, with load block"),
 Layout("GL-LR30-TAB", "tabloid", ring=356, pitch=508, cols=5, rows=6, sighter_cols=3,
        note="100 to 200 yd dense, 30 scoring, Tabloid"),
 # --- long range, tiled: one sheet of a 3 wide by 2 tall assembly ---
 Layout("GL-LR300-T", "letter", ring=381, pitch=1016, cols=2, rows=3, sighter_cols=0,
        fid_scheme=HALF, tile=(2, 2), qr_count=2,
        note="300 yd tile, 4.0 in pitch. 2x2 default = 24 bulls, 3x2 = 36"),
 Layout("GL-LR300-TA4", "a4", ring=381, pitch=1016, cols=2, rows=3, sighter_cols=0,
        fid_scheme=HALF, tile=(2, 2), qr_count=2,
        note="300 yd tile on A4. 2x2 default = 24 bulls, 3x2 = 36"),
 # --- long range on roll media ---
 Layout("GL-LR300-R24", "roll24", ring=635, pitch=1016, cols=6, rows=5, sighter_cols=3,
        data_block=DB_FULL, note="300 yd, 24 in roll, 30 scoring bulls at 4.0 in pitch, 2.5 in rings"),
 Layout("GL-LR300-R36", "roll36", ring=635, pitch=1016, cols=9, rows=4, sighter_cols=3,
        data_block=DB_FULL, note="300 yd, 36 in roll, 36 scoring bulls at 4.0 in pitch, 2.5 in rings"),
 Layout("GL-LR300-R42", "roll42", ring=635, pitch=1016, cols=10, rows=4, sighter_cols=3,
        data_block=DB_FULL, note="300 yd, 42 in roll, 40 scoring bulls at 4.0 in pitch, 2.5 in rings"),
]

Z = [
 ZeroSheet("GL-ZERO-MOA-100Y", "letter", MOA_100Y, "MOA at 100 yd", 3.0, 6, 2, DB_ZERO,
           "0.5 MOA minor, 1 MOA major, plus or minus 3.0 MOA"),
 ZeroSheet("GL-ZERO-MIL-100Y", "letter", MIL_100Y, "mil at 100 yd", 0.8, 8, 5, DB_ZERO,
           "0.1 mil minor, 0.5 mil major, plus or minus 0.8 mil"),
 ZeroSheet("GL-ZERO-MOA-100M", "letter", MOA_100M, "MOA at 100 m", 2.5, 5, 2, DB_ZERO,
           "0.5 MOA minor, 1 MOA major, plus or minus 2.5 MOA"),
 ZeroSheet("GL-ZERO-MIL-100M", "letter", MIL_100M, "mil at 100 m", 0.8, 8, 5, DB_ZERO,
           "0.1 mil minor, 0.5 mil major, plus or minus 0.8 mil"),
]

rows = []
fails = 0
print("== multi-bull layouts " + "=" * 76)
for l in L:
    r = l.report()
    r['note'] = l.note; r['data_block'] = l.data_block
    r['fid_scheme'] = l.fid_scheme; r['tile'] = l.tile; r['qr_count'] = l.qr_count
    rows.append(r)
    st = "FAIL" if r['errors'] else ("warn" if r['nwarn'] else "OK")
    if r['errors']: fails += 1
    print(f"{r['name']:16s} {r['page']:9s}{r['page_in']:>13s} {r['grid']:4s} "
          f"sc {r['scoring']:3d} sg {r['sighters']:2d} = {r['total']:3d}  "
          f"pitch {r['pitch_in']*25.4:5.1f}mm ring {r['ring_in']*25.4:5.1f}mm  "
          f"mk {r['markers']:3d}  ovh {r['overhead_pct']:5.2f}%  "
          f"{'DB' if l.data_block else '  '} {'T' if l.tile else ' '} q{l.qr_count} {st}")
    for e in r['errors'][:2]: print("      ERR", e)
    if r['nwarn']: print(f"      {r['nwarn']} tight-margin warnings")

print("\n== zeroing sheets " + "=" * 79)
zrows = []
for z in Z:
    r = z.report(); zrows.append(r)
    st = "FAIL" if r['errors'] else ("warn" if r['warnings'] else "OK")
    if r['errors']: fails += 1
    print(f"{r['name']:18s} {r['page']:7s} unit {r['unit_dmm']:8.3f} dmm  "
          f"cell {r['cell_dmm']:7.3f} dmm  half {r['half']:4d}  field {r['field']:5d}  "
          f"dev {r['maxdev_dmm']:4.2f}  mk {r['markers']:3d}  {st}")
    for e in r['errors'][:2]: print("      ERR", e)
    for w in r['warnings'][:2]: print("      warn", w)

print("\n== tiled assembly presets " + "=" * 71)
for l in L:
    if not l.tile: continue
    for c, r in ((2, 2), (3, 2)):
        n = l.cols * l.rows * c * r
        w = l.W * c / 254.0; h = l.H * r / 254.0
        d = "default" if (c, r) == l.tile else ""
        print(f"{l.name:14s} {c} x {r} tiles = {c*r} sheets, {n:3d} scoring bulls, "
              f"{w:5.1f} x {h:5.1f} in assembled  {d}")

print(f"\n{len(L)} multi-bull layouts, {len(Z)} zeroing sheets, {fails} failing")
json.dump({"layouts": rows, "zero": zrows}, open("layouts.json", "w"), indent=1)
