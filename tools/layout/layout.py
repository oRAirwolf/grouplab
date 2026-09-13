"""GroupLab built-in target layout solver / validator.  Units: dmm (0.1 mm)."""
import itertools, json, math

PAGES = {"letter":(2159,2794),"legal":(2159,3556),"tabloid":(2794,4318),
         "a5":(1480,2100),"a4":(2100,2970),"a3":(2970,4200),
         # plotter roll media: the width is fixed by the roll, the length by the design
         "roll24":(6096,7112),   # 24.0 x 28.0 in
         "roll36":(9144,6096),   # 36.0 x 24.0 in
         "roll42":(10668,6096)}  # 42.0 x 24.0 in
SAFE = 120          # 12.0 mm safe margin: covers 3mm scanner crop + inkjet no-print
MARK = 60           # ArUco 4.0mm marker + 1.0mm quiet zone each side = 6.0mm footprint
QR   = 260          # v10-H at 0.4mm module: 22.8mm symbol + 1.6mm quiet each side

def rects_overlap(a,b,gap=0):
    return not (a[2]+gap<=b[0] or b[2]+gap<=a[0] or a[3]+gap<=b[1] or b[3]+gap<=a[1])

def box(cx,cy,w,h=None):
    h = w if h is None else h
    return (cx-w/2, cy-h/2, cx+w/2, cy+h/2)

class Layout:
    def __init__(self, name, page, ring, pitch, cols, rows,
                 sighter_cols=0, sighter_gap=None, ring_sighter=None, note="",
                 data_block=0, fid_scheme="grid-boundary-1", tile=None, qr_count=4):
        self.name=name; self.page=page; self.W,self.H = PAGES[page]
        self.ring=ring; self.pitch=pitch; self.cols=cols; self.rows=rows
        self.sighter_cols=sighter_cols
        self.sighter_gap = sighter_gap if sighter_gap else int(pitch*1.2)  # 1.2 x pitch, measured convention
        self.ring_s = ring_sighter or ring
        self.note=note
        self.data_block=data_block   # dmm of bottom band reserved for the load block
        self.fid_scheme=fid_scheme   # grid-boundary-1 or grid-boundary-half-1
        self.tile=tile               # (cols, rows) of an assembly this sheet is one tile of
        self.qr_count=qr_count       # 4 = all corners, 2 = top corners only
        assert pitch % 2 == 0, f'{name}: pitch {pitch} dmm must be even so the derived cell-boundary lattice lands on integer dmm'
        if fid_scheme == "grid-boundary-half-1":
            assert pitch % 4 == 0, f'{name}: pitch {pitch} dmm must be divisible by 4 for the half lattice'
        self.solve()

    def solve(self):
        px,py = self.pitch, self.pitch
        span_x = (self.cols-1)*px
        self.x0 = round((self.W - span_x)/2)
        self.xs = [self.x0 + i*px for i in range(self.cols)]
        DBH = self.data_block
        self.db = (SAFE, self.H-SAFE-DBH, self.W-SAFE, self.H-SAFE) if DBH else None
        # the bottom codes sit ABOVE the data block, never on top of it.  The row
        # solver already reserves DB + QR at the foot of the page, so this uses
        # space that was reserved for it rather than taking any from the grid.
        qr_bot_c = self.H - SAFE - DBH - (30 if DBH else 0) - QR/2
        self.qr=[(SAFE+QR/2, SAFE+QR/2),(self.W-SAFE-QR/2, SAFE+QR/2)]
        if self.qr_count==4:
            self.qr += [(SAFE+QR/2, qr_bot_c),(self.W-SAFE-QR/2, qr_bot_c)]
        qr_x0, qr_x1 = SAFE, SAFE+QR
        qr_x2, qr_x3 = self.W-SAFE-QR, self.W-SAFE
        CL = 30  # 3.0 mm clearance
        # does the outermost scoring column overlap a corner QR in x?
        def xclash(halfw, xs):
            # a column "clashes" when it comes within the clearance of a code
            # band, not merely when it overlaps one.  Without the clearance a
            # bull can sit 0.15 mm from a code and the solver calls it clear.
            for x in xs:
                if not (x+halfw+CL <= qr_x0 or x-halfw-CL >= qr_x1): return True
                if not (x+halfw+CL <= qr_x2 or x-halfw-CL >= qr_x3): return True
            return False
        r2 = self.ring/2; rs2 = self.ring_s/2
        top_lim = (SAFE+QR+CL+r2) if xclash(r2,self.xs) else (SAFE+CL+r2)
        span_y = (self.rows-1)*py
        # sighters
        if self.sighter_cols:
            sx_span=(self.sighter_cols-1)*px
            self.xs_s=[round((self.W-sx_span)/2)+i*px for i in range(self.sighter_cols)]
        else: self.xs_s=[]
        DB = self.data_block
        BQ = (QR + (CL if DB else 0)) if self.qr_count==4 else 0
        bot_lim_s = (self.H-SAFE-DB-BQ-CL-rs2) if (BQ and xclash(rs2,self.xs_s)) else (self.H-SAFE-DB-CL-rs2)
        bot_lim_b = (self.H-SAFE-DB-BQ-CL-r2) if (BQ and xclash(r2,self.xs)) else (self.H-SAFE-DB-CL-r2)
        if self.sighter_cols:
            last_scoring_max = bot_lim_s - self.sighter_gap
            bot_lim = min(bot_lim_b, last_scoring_max)
        else:
            bot_lim = bot_lim_b
        free = (bot_lim - top_lim) - span_y
        self.fits = free >= 0
        self.free = free
        self.y0 = round(top_lim + max(0,free)/2)
        self.ys = [self.y0 + j*py for j in range(self.rows)]
        self.ys_s = ([self.ys[-1] + self.sighter_gap] if self.sighter_cols else [])
        hx = px/2; hy = py/2
        step_x, step_y = (px, py) if self.fid_scheme=="grid-boundary-1" else (px//2, py//2)
        nx = self.cols*(px//step_x) + 1
        ny = self.rows*(py//step_y) + 1
        self.fx = [self.xs[0]-hx + i*step_x for i in range(nx)]
        self.fy = [self.ys[0]-hy + j*step_y for j in range(ny)]
        if self.sighter_cols:
            self.fy.append(self.ys_s[0]-hy); self.fy.append(self.ys_s[0]+hy)
        fy=[]
        for v in sorted(set(self.fy)):
            if not fy or v-fy[-1] >= MARK+20: fy.append(v)
        self.fy = fy
        self.marks=[]; self.dropped=0
        rings=[box(x,y,self.ring) for x in self.xs for y in self.ys] + \
              [box(x,y,self.ring_s) for x in self.xs_s for y in self.ys_s]
        qrb=[box(cx,cy,QR) for cx,cy in self.qr]
        for x in self.fx:
            for y in self.fy:
                b=box(x,y,MARK)
                if b[0]<SAFE*0.5 or b[1]<SAFE*0.5 or b[2]>self.W-SAFE*0.5 or b[3]>self.H-SAFE*0.5:
                    self.dropped+=1; continue
                if any(rects_overlap(b,q,20) for q in qrb): self.dropped+=1; continue
                if any(rects_overlap(b,r,10) for r in rings): self.dropped+=1; continue
                if self.db and rects_overlap(b,self.db,10): self.dropped+=1; continue
                self.marks.append((round(x),round(y)))

    def check(self):
        errs=[]; warns=[]
        rings=[(box(x,y,self.ring),f"bull({x},{y})") for x in self.xs for y in self.ys] + \
              [(box(x,y,self.ring_s),f"sighter({x},{y})") for x in self.xs_s for y in self.ys_s]
        qrb=[(box(cx,cy,QR),f"qr{i}") for i,(cx,cy) in enumerate(self.qr)]
        mkb=[(box(x,y,MARK),f"mark({x},{y})") for x,y in self.marks]
        dbb=[(self.db,"dataBlock")] if self.db else []
        allb = rings+qrb+mkb+dbb
        if not self.fits:
            errs.append(f"does not fit: {-self.free:.0f} dmm short of vertical room")
        for (a,na),(b,nb) in itertools.combinations(allb,2):
            if rects_overlap(a,b): errs.append(f"overlap {na} x {nb}")
        CL = 30
        major = rings+qrb+dbb
        for (a,na),(b,nb) in itertools.combinations(major,2):
            if not rects_overlap(a,b) and rects_overlap(a,b,CL):
                warns.append(f"clearance under {CL} dmm: {na} x {nb}")
        for (a,na),(b,nb) in itertools.combinations(mkb,2):
            if not rects_overlap(a,b) and rects_overlap(a,b,20):
                warns.append(f"markers under 20 dmm apart: {na} x {nb}")
        for b,n in allb:
            if b[0]<0 or b[1]<0 or b[2]>self.W or b[3]>self.H: errs.append(f"offpage {n}")
            elif b[0]<SAFE*0.5 or b[1]<SAFE*0.5 or b[2]>self.W-SAFE*0.5 or b[3]>self.H-SAFE*0.5:
                warns.append(f"tight margin {n}")
        return errs,warns

    def report(self):
        e,w=self.check()
        nb=self.cols*self.rows; ns=len(self.xs_s)
        used=(nb*math.pi*(self.ring/2)**2 + ns*math.pi*(self.ring_s/2)**2)
        mk_area=len(self.marks)*MARK*MARK; qr_area=self.qr_count*QR*QR
        pg=self.W*self.H
        return dict(name=self.name, page=self.page, page_in=f"{self.W/254:.2f}x{self.H/254:.2f}",
            grid=f"{self.cols}x{self.rows}", scoring=nb, sighters=ns, total=nb+ns,
            pitch_in=round(self.pitch/254,4), ring_in=round(self.ring/254,4),
            x0=self.xs[0], y0=self.ys[0], xs=self.xs, ys=self.ys,
            sighter_y=self.ys_s, sighter_x=self.xs_s,
            markers=len(self.marks), dropped=self.dropped,
            fits=self.fits, free=round(self.free),
            data_block_rect=self.db, qr=[(round(a),round(b)) for a,b in self.qr],
            marker_pct=round(100*mk_area/pg,2), qr_pct=round(100*qr_area/pg,2),
            overhead_pct=round(100*(mk_area+qr_area)/pg,2),
            errors=e, warnings=w[:4], nwarn=len(w))
