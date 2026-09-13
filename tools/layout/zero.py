"""GroupLab zeroing-sheet layout solver / validator.  Units: dmm (0.1 mm).

A zeroing sheet is one aiming mark on a printed measurement grid whose cell
size is an exact angular unit at a stated distance.  Geometry differs enough
from the multi-bull layouts that it gets its own solver.
"""
import itertools, math
from layout import PAGES, SAFE, MARK, QR, box, rects_overlap

CL   = 30    # 3.0 mm clearance
BAND = 150   # minimum clear band around the grid field, for the fiducial ring
AIM  = 127   # outer diameter of the central aiming ring

# angular units, dmm subtended at the stated distance
MOA_100Y = 100.0 * 36 * 25.4 * math.tan(math.radians(1/60.0)) * 10
MIL_100Y = 100.0 * 36 * 25.4 * 0.001 * 10
MOA_100M = 100.0 * 1000    * math.tan(math.radians(1/60.0)) * 10
MIL_100M = 100.0 * 1000    * 0.001 * 10


class ZeroSheet:
    def __init__(self, name, page, unit_dmm, unit_name, half_units, divisions,
                 major_every, data_block=0, note=""):
        self.name = name; self.page = page
        self.W, self.H = PAGES[page]
        self.unit_dmm = unit_dmm; self.unit_name = unit_name
        self.half_units = half_units          # angular half-extent, in units
        self.divisions = divisions            # minor cells from centre to edge
        self.major_every = major_every        # minor cells per major line
        self.data_block = data_block
        self.note = note
        self.solve()

    def solve(self):
        # every minor line is rounded from its own true angular offset, so the
        # error is bounded at half a dmm everywhere and does not accumulate
        self._off = [round(self.unit_dmm * self.half_units * i / self.divisions)
                     for i in range(self.divisions + 1)]
        self.half = self._off[-1]
        self.field = 2 * self.half
        self.cx = self.W // 2
        # the grid field is wide enough to reach under both corner QR columns on
        # every sheet in the library, so both QR rows are reserved unconditionally
        top = SAFE + QR + CL
        bot = self.H - SAFE - self.data_block - QR - CL
        self.cy = (top + bot) // 2
        self.v_top, self.v_bot = top, bot
        # minor line offsets from centre, integer, error spread across the field
        self.offsets = self._off
        self.ideal   = [self.unit_dmm * self.half_units * i / self.divisions
                        for i in range(self.divisions + 1)]
        self.maxdev  = max(abs(a - b) for a, b in zip(self.offsets, self.ideal))
        self.cell    = self.unit_dmm * self.half_units / self.divisions
        # corner QR centres
        self.qr = [(SAFE + QR / 2, SAFE + QR / 2), (self.W - SAFE - QR / 2, SAFE + QR / 2),
                   (SAFE + QR / 2, self.H - SAFE - self.data_block - QR / 2),
                   (self.W - SAFE - QR / 2, self.H - SAFE - self.data_block - QR / 2)]
        # fiducial ring: markers in the band around the field, on major lines
        f0, f1 = self.cx - self.half, self.cx + self.half
        g0, g1 = self.cy - self.half, self.cy + self.half
        offx = (self.W - 2 * SAFE - self.field) / 2          # side band width
        offy = 0                                             # computed below
        mx = self.cx - self.half - MARK / 2 - CL             # marker column, left
        mX = self.cx + self.half + MARK / 2 + CL
        my = self.cy - self.half - MARK / 2 - CL
        mY = self.cy + self.half + MARK / 2 + CL
        maj = [self.offsets[i] for i in range(0, self.divisions + 1, self.major_every)]
        cand = []
        for o in maj:                       # left and right columns
            for s in (-1, 1):
                cand.append((mx, self.cy + s * o))
                cand.append((mX, self.cy + s * o))
        for o in maj:                       # top and bottom rows
            for s in (-1, 1):
                cand.append((self.cx + s * o, my))
                cand.append((self.cx + s * o, mY))
        cand.append((mx, my)); cand.append((mX, my))
        cand.append((mx, mY)); cand.append((mX, mY))
        qrb = [box(a, b, QR) for a, b in self.qr]
        seen = set(); self.marks = []; self.dropped = 0
        for x, y in cand:
            k = (round(x), round(y))
            if k in seen: continue
            seen.add(k)
            b = box(x, y, MARK)
            if (b[0] < SAFE * 0.5 or b[1] < SAFE * 0.5
                    or b[2] > self.W - SAFE * 0.5
                    or b[3] > self.H - SAFE * 0.5 - self.data_block):
                self.dropped += 1; continue
            if any(rects_overlap(b, q, 20) for q in qrb):
                self.dropped += 1; continue
            self.marks.append(k)
        self.band_x = (self.W - 2 * SAFE - self.field) / 2
        self.band_y_top = (self.cy - self.half) - self.v_top
        self.band_y_bot = self.v_bot - (self.cy + self.half)

    def check(self):
        errs = []; warns = []
        items = [(box(self.cx, self.cy, AIM), "aim")]
        items += [(box(a, b, QR), f"qr{i}") for i, (a, b) in enumerate(self.qr)]
        items += [(box(x, y, MARK), f"mark({x},{y})") for x, y in self.marks]
        grid = (self.cx - self.half, self.cy - self.half,
                self.cx + self.half, self.cy + self.half)
        for b, n in items:
            if n.startswith("mark") and rects_overlap(b, grid, CL):
                errs.append(f"{n} intrudes on grid field")
            if n.startswith("qr") and rects_overlap(b, grid, CL):
                errs.append(f"{n} intrudes on grid field")
        for (a, na), (b, nb) in itertools.combinations(items, 2):
            if rects_overlap(a, b): errs.append(f"overlap {na} x {nb}")
        for b, n in items:
            if b[0] < 0 or b[1] < 0 or b[2] > self.W or b[3] > self.H:
                errs.append(f"offpage {n}")
        if grid[0] < SAFE or grid[2] > self.W - SAFE:
            errs.append("grid field crosses the safe margin in x")
        if grid[1] < SAFE or grid[3] > self.H - SAFE - self.data_block:
            errs.append("grid field crosses the safe margin in y")
        if self.band_x < BAND: warns.append(f"side band {self.band_x:.0f} dmm below {BAND}")
        if self.maxdev > 1.0: warns.append(f"line rounding {self.maxdev:.2f} dmm")
        if len(self.marks) < 8: warns.append(f"only {len(self.marks)} markers")
        return errs, warns

    def report(self):
        e, w = self.check()
        return dict(name=self.name, page=self.page, unit=self.unit_name,
                    unit_dmm=round(self.unit_dmm, 3),
                    half_units=self.half_units, half=self.half, field=self.field,
                    divisions=self.divisions, major_every=self.major_every,
                    cell_dmm=round(self.cell, 3), maxdev_dmm=round(self.maxdev, 3),
                    cx=self.cx, cy=self.cy, offsets=self.offsets,
                    markers=len(self.marks), marks=self.marks,
                    band_x=round(self.band_x, 1),
                    band_y=(round(self.band_y_top, 1), round(self.band_y_bot, 1)),
                    data_block=self.data_block, errors=e, warnings=w, note=self.note)
