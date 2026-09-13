#!/usr/bin/env python3
"""s03_ref_target_geometry.py -- ground-truth grid geometry from the vector
blank-target PDFs.

The blank targets are vector PDFs produced by "Print To PDF". This walks the
content stream, collects every stroked/filled path, converts to page
coordinates (points, origin bottom-left), clusters the circles by centre, and
reports nominal bull centres, radii and grid pitch in inches. These numbers are
the ground truth that the scan measurements are compared against.

Usage:
    python3 s03_ref_target_geometry.py <pdf> [...]
"""
import argparse
import os
import re
from collections import defaultdict

from pypdf import PdfReader


def content_ops(page):
    data = page.get_contents().get_data()
    try:
        txt = data.decode("latin-1")
    except Exception:
        txt = str(data)
    return txt


TOKEN = re.compile(rb"|".join([]) ) if False else None


def parse_paths(txt):
    """Very small content-stream walker: tracks cm matrix and m/l/c/re paths."""
    toks = txt.replace("\n", " ").replace("\r", " ").split()
    stack = []
    ctm = (1, 0, 0, 1, 0, 0)
    nums = []
    cur = []
    paths = []
    start = None

    def apply(m, x, y):
        a, b, c, d, e, f = m
        return (a * x + c * y + e, b * x + d * y + f)

    i = 0
    while i < len(toks):
        t = toks[i]
        try:
            nums.append(float(t))
            i += 1
            continue
        except ValueError:
            pass
        op = t
        if op == "q":
            stack.append(ctm)
        elif op == "Q":
            if stack:
                ctm = stack.pop()
        elif op == "cm" and len(nums) >= 6:
            m = tuple(nums[-6:])
            a, b, c, d, e, f = ctm
            a2, b2, c2, d2, e2, f2 = m
            ctm = (a2 * a + b2 * c, a2 * b + b2 * d,
                   c2 * a + d2 * c, c2 * b + d2 * d,
                   e2 * a + f2 * c + e, e2 * b + f2 * d + f)
        elif op == "m" and len(nums) >= 2:
            cur = [apply(ctm, nums[-2], nums[-1])]
            start = cur[0]
        elif op == "l" and len(nums) >= 2:
            cur.append(apply(ctm, nums[-2], nums[-1]))
        elif op == "c" and len(nums) >= 6:
            for j in (0, 2, 4):
                cur.append(apply(ctm, nums[-6 + j], nums[-6 + j + 1]))
        elif op == "v" or op == "y":
            if len(nums) >= 4:
                for j in (0, 2):
                    cur.append(apply(ctm, nums[-4 + j], nums[-4 + j + 1]))
        elif op == "re" and len(nums) >= 4:
            x, y, w, h = nums[-4:]
            pts = [apply(ctm, x, y), apply(ctm, x + w, y),
                   apply(ctm, x + w, y + h), apply(ctm, x, y + h)]
            paths.append(("re", pts))
            cur = []
        elif op == "h":
            if cur and start:
                cur.append(start)
        elif op in ("S", "s", "f", "F", "f*", "B", "B*", "b", "b*", "n"):
            if cur:
                paths.append((op, cur))
            cur = []
        if op not in ("m", "l", "c", "v", "y", "re", "cm"):
            pass
        nums = []
        i += 1
    return paths


def circle_fit(pts):
    """Algebraic circle fit. Returns (cx, cy, r, rms_residual)."""
    import numpy as np
    P = np.array(pts, dtype=float)
    if len(P) < 5:
        return None
    x, y = P[:, 0], P[:, 1]
    A = np.column_stack([x, y, np.ones(len(x))])
    b = x ** 2 + y ** 2
    try:
        sol, *_ = np.linalg.lstsq(A, b, rcond=None)
    except Exception:
        return None
    cx, cy = sol[0] / 2, sol[1] / 2
    r = (sol[2] + cx ** 2 + cy ** 2) ** 0.5
    res = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5 - r
    return cx, cy, r, float((res ** 2).mean() ** 0.5)


def main():
    import numpy as np
    ap = argparse.ArgumentParser()
    ap.add_argument("pdfs", nargs="+")
    a = ap.parse_args()
    for p in a.pdfs:
        print("=" * 70)
        print(os.path.basename(p))
        r = PdfReader(p)
        page = r.pages[0]
        mb = page.mediabox
        print("page %.2f x %.2f pt (%.3f x %.3f in)"
              % (float(mb.width), float(mb.height), float(mb.width) / 72, float(mb.height) / 72))
        paths = parse_paths(content_ops(page))
        print("path ops parsed:", len(paths))
        circles = []
        for op, pts in paths:
            if op == "re" or len(pts) < 6:
                continue
            f = circle_fit(pts)
            if f is None:
                continue
            cx, cy, rad, rms = f
            if rms < 0.15 * max(rad, 1e-6) and 0.5 < rad < 200:
                circles.append((cx, cy, rad, rms, len(pts)))
        print("circle-like subpaths:", len(circles))
        # group concentric circles by centre (1 pt tolerance)
        groups = defaultdict(list)
        for cx, cy, rad, rms, n in circles:
            key = (round(cx / 2.0), round(cy / 2.0))
            groups[key].append((cx, cy, rad))
        cents = []
        for k, g in sorted(groups.items()):
            cxs = np.mean([q[0] for q in g])
            cys = np.mean([q[1] for q in g])
            rads = sorted(q[2] for q in g)
            cents.append((cxs, cys, rads))
        print("distinct bull centres:", len(cents))
        for cx, cy, rads in sorted(cents, key=lambda t: (-t[1], t[0])):
            print("   centre (%.2f, %.2f) pt = (%.4f, %.4f) in  radii pt %s  = dia in %s"
                  % (cx, cy, cx / 72, cy / 72,
                     [round(v, 2) for v in rads],
                     [round(2 * v / 72, 4) for v in rads]))
        if len(cents) >= 4:
            xs = sorted(set(round(c[0] / 72, 3) for c in cents))
            ys = sorted(set(round(c[1] / 72, 3) for c in cents))
            xs = _cluster(xs, 0.05)
            ys = _cluster(ys, 0.05)
            print("column x (in):", [round(v, 4) for v in xs])
            print("row    y (in):", [round(v, 4) for v in ys])
            if len(xs) > 1:
                dx = np.diff(xs)
                print("x pitch in: mean %.4f sd %.4f  values %s"
                      % (dx.mean(), dx.std(ddof=0) if len(dx) > 1 else 0,
                         [round(v, 4) for v in dx]))
            if len(ys) > 1:
                dy = np.diff(ys)
                print("y pitch in: mean %.4f sd %.4f  values %s"
                      % (dy.mean(), dy.std(ddof=0) if len(dy) > 1 else 0,
                         [round(v, 4) for v in dy]))


def _cluster(vals, tol):
    out = []
    for v in vals:
        if out and abs(v - out[-1][-1]) <= tol:
            out[-1].append(v)
        else:
            out.append([v])
    return [sum(g) / len(g) for g in out]


if __name__ == "__main__":
    main()
