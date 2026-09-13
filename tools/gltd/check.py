"""Payload size and identifier check for the GroupLab built-in library.

Every sheet is built from tools/layout/layouts.json, so this file cannot drift
away from the validated geometry the way a hand-maintained copy did.  Run
tools/layout/run.py first if layouts.json is older than run.py.
"""
import json, os, sys
from encode import frame, defid, HDR

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "layout"))
from layout import PAGES          # the page table, not a rounded inch string
LJ   = os.path.join(HERE, "..", "layout", "layouts.json")
if not os.path.exists(LJ):
    sys.exit("layouts.json not found: run tools/layout/run.py first")
LAY = json.load(open(LJ))

PAGE_SIZE = {"letter":"letter","a4":"a4","a3":"a3","tabloid":"tabloid",
             "roll24":"roll-24","roll36":"roll-36","roll42":"roll-42"}
INKS = [{"srgb":"#000000","role":"artwork"}, {"srgb":"#FFFFFF","role":"paper"},
        {"srgb":"#000000","role":"fiducial"}, {"srgb":"#000000","role":"code"},
        {"srgb":"#000000","role":"text"}]
DB9 = {"layout":"fields-3x3-1","fieldSet":"standard-9","reserve":280,"inkIdx":0}
# the zeroing block is 210 dmm tall, so its reserved square is 210 dmm and holds
# the identifier and serial as text rather than an instance code: a v11-Q symbol
# is 276 dmm across and does not fit.  See TARGET-SCHEMA.md 3.10.
DB6 = dict(DB9, layout="fields-3x2-1", fieldSet="standard-6", reserve=210)
SAFE, QR, CLR = 120, 260, 30

# The documented disc stacks, from TARGET-LIBRARY.md sections 4 and 5.  Kept as
# a table rather than a formula because the library states them explicitly and a
# formula that almost reproduces them is worse than useless.
STACKS = {
    254: [254, 238, 127, 115,  25],   # centrefire 100 yd
    222: [222, 208, 111, 101,  25],   # centrefire dense
    152: [152, 142,  76,  68,  20],   # rimfire
    127: [127, 117,  64,  56,  18],   # rimfire dense
    381: [381, 365, 191, 179,  38],   # large format
    356: [356, 342, 178, 166,  36],   # large format dense
    320: [320, 312, 160, 154,  32],   # 300 yd tile
    635: [635, 613, 318, 302,  64],   # roll media
}

def discs(outer):
    if outer not in STACKS:
        raise SystemExit(f"no documented disc stack for a {outer} dmm ring")
    return [{"diameter": d, "inkIdx": 0 if i % 2 == 0 else 15}
            for i, d in enumerate(STACKS[outer])]

MODULE = 4                        # dmm per QR module, the library standard
FOOT   = 65 * MODULE              # v10 is 57 modules, plus 4 quiet each side

def corners1(W, H, db_height, count, footprint=FOOT):
    """The corners-1 derivation rule of TARGET-SCHEMA.md 3.8, in full.

    Centres are inset from the page edge by the safe margin plus half the code
    footprint.  Where the sheet carries a data block the bottom pair sits above
    it with a clearance, never at the page corner.
    """
    h = footprint / 2
    top = SAFE + h
    bot = H - SAFE - db_height - (CLR if db_height else 0) - h
    p = [(SAFE + h, top), (W - SAFE - h, top)]
    if count == 4:
        p += [(SAFE + h, bot), (W - SAFE - h, bot)]
    return [(round(a), round(b)) for a, b in p]

def sheet(r):
    W, H = PAGES[r["page"]]
    pitch = round(r["pitch_in"]*254); ring = round(r["ring_in"]*254)
    d = {"page":{"size":PAGE_SIZE[r["page"]],"width":W,"height":H},
         "inks":INKS,
         "ringSets":[{"discs":discs(ring)}],
         "grid":{"cols":int(r["grid"].split("x")[0]),"rows":int(r["grid"].split("x")[1]),
                 "originX":r["xs"][0],"originY":r["ys"][0],
                 "pitchX":pitch,"pitchY":pitch,"ringSetIdx":0,"order":0},
         "sighters":[],
         "fiducials":{"scheme":r["fid_scheme"],"family":"apriltag-36h11",
                      "markerSize":40,"quietZone":10,"inkIdx":0},
         "codes":{"count":r["qr_count"],"version":10,"ecLevel":"H","moduleSize":4,
                  "quietZone":16,"placement":"corners-1","humanReadableId":True,
                  "positions":[{"x":a,"y":b} for a,b in
                               corners1(W,H,r["data_block"],r["qr_count"])]}}
    if r["sighter_x"]:
        d["sighters"] = [{"count":len(r["sighter_x"]),"originX":r["sighter_x"][0],
                          "originY":r["sighter_y"][0],"pitchX":pitch,"ringSetIdx":0}]
    if r["data_block"]:
        d["dataBlock"] = dict(DB9, x=SAFE, y=H-SAFE-r["data_block"],
                              width=W-2*SAFE, height=r["data_block"])
    if r["tile"]:
        d["tiling"] = {"cols":r["tile"][0],"rows":r["tile"][1],
                       "sheetWidth":W,"sheetHeight":H,"overlap":0}
    return d

def zero(z):
    W, H = PAGES[z["page"]]
    d = {"page":{"size":"letter","width":W,"height":H},
         "inks":INKS,
         "ringSets":[{"discs":[{"diameter":127,"inkIdx":0},
                               {"diameter":114,"inkIdx":15},
                               {"diameter":25, "inkIdx":0}]}],
         "grid":{"cols":1,"rows":1,"originX":z["cx"],"originY":z["cy"],
                 "pitchX":0,"pitchY":0,"ringSetIdx":0,"order":0},
         "sighters":[],
         "fiducials":{"scheme":"field-ring-1","family":"apriltag-36h11",
                      "markerSize":40,"quietZone":10,"inkIdx":0},
         "codes":{"count":4,"version":10,"ecLevel":"H","moduleSize":4,
                  "quietZone":16,"placement":"corners-1","humanReadableId":True,
                  "positions":[{"x":a,"y":b} for a,b in
                               corners1(W,H,z["data_block"],4)]},
         "dataBlock":dict(DB6, x=SAFE, y=H-SAFE-z["data_block"],
                          width=W-2*SAFE, height=z["data_block"]),
         "grids":[{"centreX":z["cx"],"centreY":z["cy"],"half":z["half"],
                   "divisions":z["divisions"],"majorEvery":z["major_every"],
                   "unit":"moa" if "MOA" in z["unit"] else "mil",
                   "distance":100,"distanceUnit":"yd" if "yd" in z["unit"] else "m",
                   "inkPair":0x00,"style":1,"labelStep":z["major_every"]}]}
    return d

def crosscheck():
    """corners-1 must reproduce exactly what the layout solver placed."""
    bad = []
    for r in LAY["layouts"] + LAY["zero"]:
        W, H = PAGES[r["page"]]
        n = r.get("qr_count", 4)
        want = [tuple(p) for p in r["qr"]]
        got  = corners1(W, H, r["data_block"], n)
        if sorted(want) != sorted(got):
            bad.append(f"{r['name']}: solver {sorted(want)} rule {sorted(got)}")
    return bad

if __name__ == "__main__":
    bad = crosscheck()
    for b in bad: print("POSITION MISMATCH", b)
    if bad: sys.exit(1)
    rows = [(r["name"], sheet(r)) for r in LAY["layouts"]]
    rows += [(z["name"], zero(z)) for z in LAY["zero"]]
    # the second tile assembly preset is a separate definition
    for r in LAY["layouts"]:
        if r["tile"]:
            d = sheet(r); d["tiling"] = dict(d["tiling"], cols=3, rows=2)
            rows.append((r["name"] + " (3x2)", d))
    print(f"{'target':22s} {'body':>5s} {'frame':>6s} {'v8-H':>5s} {'v10-H':>6s}   id")
    worst = 0
    for name, d in rows:
        fr, bd = frame(d)
        worst = max(worst, len(fr))
        print(f"{name:22s} {len(bd):5d} {len(fr):6d} "
              f"{'ok' if len(fr)<=84 else 'NO':>5s} {'ok' if len(fr)<=119 else 'NO':>6s}   {defid(bd)}")
    print(f"\n{len(rows)} definitions, largest frame {worst} bytes, "
          f"v10-H capacity 119, headroom {119-worst}")
