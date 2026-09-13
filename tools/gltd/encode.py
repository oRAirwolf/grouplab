"""GLTD-B reference encoder, wire version 1.  Sizing check only, not production code.
Units: dmm.  All multi-byte integers little-endian."""
import struct, zlib, hashlib

MAGIC = b"GT"
WIRE  = 1
HDR   = 15

PAGECODE = {"custom":0,"letter":1,"legal":2,"tabloid":3,"a3":4,"a5":6,"a4":5,
            "roll-24":7,"roll-36":8,"roll-42":9}
ROLLW    = {"roll-24":6096,"roll-36":9144,"roll-42":10668}
FAMILY   = {"none":0,"aruco-4x4-50":1,"aruco-4x4-100":2,"aruco-5x5-100":3,
            "aruco-6x6-250":4,"apriltag-16h5":5,"apriltag-25h9":6,
            "apriltag-36h11":7,"apriltag-circle21h7":8}
SCHEME   = {"explicit":0,"grid-boundary-1":1,"grid-boundary-half-1":2,"field-ring-1":3}
ECLEVEL  = {"L":0,"M":1,"Q":2,"H":3}
FIELDSET = {"standard-9":0,"standard-6":1,"explicit":255}
DBLAYOUT = {"fields-3x3-1":0,"fields-3x2-1":1,"explicit":255}
PLACE    = {"corners-1":0,"explicit":1}
UNIT     = {"custom":0,"moa":1,"mil":2,"inch":3,"cm":4}

u8  = lambda v: struct.pack("<B", v)
u16 = lambda v: struct.pack("<H", v)

def body(d):
    b = b""
    # 1 page
    pc = PAGECODE[d["page"]["size"]]
    b += u8(pc)
    if pc == 0:   b += u16(d["page"]["width"]) + u16(d["page"]["height"])
    elif pc >= 7: b += u16(d["page"]["height"])
    b += u8(0 if d["page"].get("orientation","portrait")=="portrait" else 1)
    b += u8(d.get("quantum",0))
    # 2 inks.  The paper knockout is index 15 and is never stored.  Each
    # DISTINCT sRGB value of the remaining inks is stored once, in declaration
    # order: the body carries colours, not keys or roles, so four inks that are
    # all #000000 are one entry.  See the projection rule in TARGET-SCHEMA 6.
    seen = []
    for i in d["inks"]:
        if i["role"] == "paper": continue
        if i["srgb"].upper() not in seen: seen.append(i["srgb"].upper())
    b += u8(len(seen))
    for c in seen: b += bytes(int(c[k:k+2],16) for k in (1,3,5))
    # 3 ring sets, disc stacks
    b += u8(len(d["ringSets"]))
    for s in d["ringSets"]:
        b += u8(len(s["discs"]))
        for c in s["discs"]: b += u16(c["diameter"]) + u8(c["inkIdx"])
    # 4 grid
    g = d["grid"]
    b += u8(g["cols"]) + u8(g["rows"]) + u16(g["originX"]) + u16(g["originY"]) \
       + u16(g["pitchX"]) + u16(g["pitchY"]) + u8(g["ringSetIdx"]) + u8(g["order"])
    # 5 sighters
    sg = d.get("sighters", [])
    b += u8(len(sg))
    for s in sg:
        b += u8(s["count"]) + u16(s["originX"]) + u16(s["originY"]) \
           + u16(s["pitchX"]) + u8(s["ringSetIdx"])
    # 6 fiducials
    f = d["fiducials"]
    b += u8(SCHEME[f["scheme"]]) + u8(FAMILY[f["family"]]) + u16(f["markerSize"]) \
       + u8(f["quietZone"]) + u8(f["inkIdx"])
    # 7 codes
    c = d["codes"]
    b += u8(c["count"]) + u8(ECLEVEL[c["ecLevel"]]) + u8(c["moduleSize"]) \
       + u8(PLACE[c["placement"]])
    flags = 0
    # 8 data block
    if "dataBlock" in d:
        flags |= 1 << 6
        x = d["dataBlock"]
        b += u16(x["x"]) + u16(x["y"]) + u16(x["width"]) + u16(x["height"]) \
           + u8(DBLAYOUT[x["layout"]]) \
           + u8(FIELDSET[x["fieldSet"]]) + u16(x["reserve"]) + u8(x["inkIdx"])
    # 9 tiling
    if "tiling" in d:
        flags |= 1 << 7
        t = d["tiling"]
        b += u8(t["cols"]) + u8(t["rows"]) + u16(t["sheetWidth"]) \
           + u16(t["sheetHeight"]) + u16(t["overlap"]) + u8(0)
    # 10 measurement grids
    if "grids" in d:
        flags |= 1 << 8
        b += u8(len(d["grids"]))
        for m in d["grids"]:
            b += u16(m["centreX"]) + u16(m["centreY"]) + u16(m["half"]) \
               + u8(m["divisions"]) + u8(m["majorEvery"]) + u8(UNIT[m["unit"]]) \
               + u16(m["distance"]) + u8(0 if m["distanceUnit"]=="yd" else 1) \
               + u8(m["inkPair"]) + u8(m["style"]) + u8(m["labelStep"])
    return b, flags

def frame(d, tile_index=0):
    bd, flags = body(d)
    h = MAGIC + u8(WIRE) + u16(flags) + u8(0) + u8(1) + u8(1) + u8(tile_index) \
        + u16(len(bd)) + struct.pack("<I", zlib.crc32(bd))
    assert len(h) == HDR, len(h)
    return h + bd, bd

def defid(bd):
    h = hashlib.sha256(bd).digest()[:10]
    A = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
    n = int.from_bytes(h, "big"); s = ""
    for _ in range(16):
        s = A[n & 31] + s; n >>= 5
    return "GL-" + "-".join(s[i:i+4] for i in range(0,16,4))
