#!/usr/bin/env python3
"""s07_warp.py -- how far a scanned page departs from a flat rectangle.

Two independent measurements:

A. PAGE OUTLINE. Segment the sheet against the scanner background, take the
   outline, fit the minimum-area rectangle, and report the signed deviation of
   the outline from that rectangle as a function of position along each edge
   (max, RMS, in inches).

B. INTERNAL WARP -- the number that matters. The printed bull grid is a
   perfect lattice on the flat original, so its scanned positions are a direct
   sample of the page deformation. Fit, in order:
       * similarity  (rotate + uniform scale + translate)
       * affine
       * homography  (what four corner fiducials would give you)
   and report the residual of each at every bull, in inches. If the homography
   residual is at the noise floor the page is a flat plane and four corners
   suffice; if it is large the warp is local and no global transform can fix
   it. Also fits a thin-plate-spline / biquadratic to show how much of the
   residual is smooth (correctable by more fiducials) versus random.

Usage:
    python3 s07_warp.py --grid work/bull_grid.json --scan-dir DIR
                        [--files a.jpg ...] [--nominal-pitch 1.875]
                        [--outline-dir DIR]
"""
import argparse
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb


def page_outline(rgb, dpi, min_frac=0.3):
    """Return the largest bright quadrilateral-ish contour = the sheet."""
    g = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
    s = max(1, int(round(dpi / 75.0)))
    sm = cv2.resize(g, (g.shape[1] // s, g.shape[0] // s), interpolation=cv2.INTER_AREA)
    sm = cv2.GaussianBlur(sm, (5, 5), 0)
    # the sheet is brighter than the scanner background / shadow gutter
    _, bw = cv2.threshold(sm, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    bw = cv2.morphologyEx(bw, cv2.MORPH_CLOSE,
                          cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))
    cnts, _ = cv2.findContours(bw, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    if not cnts:
        return None, s
    c = max(cnts, key=cv2.contourArea)
    if cv2.contourArea(c) < min_frac * sm.size:
        return None, s
    return c.reshape(-1, 2).astype(np.float64), s


def outline_vs_rect(cnt, s, dpi):
    rect = cv2.minAreaRect(cnt.astype(np.float32))
    box = cv2.boxPoints(rect).astype(np.float64)
    (w, h) = rect[1]
    # signed distance of every outline point to the nearest rectangle edge
    devs = []
    for p in cnt:
        best = 1e9
        for i in range(4):
            a, b = box[i], box[(i + 1) % 4]
            ab = b - a
            t = np.clip(np.dot(p - a, ab) / max(np.dot(ab, ab), 1e-9), 0, 1)
            d = np.linalg.norm(p - (a + t * ab))
            best = min(best, d)
        devs.append(best)
    devs = np.array(devs) * s / dpi
    return {
        "rect_w_in": round(max(w, h) * s / dpi, 4),
        "rect_h_in": round(min(w, h) * s / dpi, 4),
        "rect_angle_deg": round(float(rect[2]), 3),
        "outline_pts": int(len(cnt)),
        "dev_max_in": round(float(devs.max()), 4),
        "dev_rms_in": round(float(np.sqrt((devs ** 2).mean())), 4),
        "dev_p95_in": round(float(np.percentile(devs, 95)), 4),
        "dev_p50_in": round(float(np.percentile(devs, 50)), 4),
    }, box


def index_lattice(pts, tol_frac=0.35):
    """Assign integer (row, col) indices to a set of lattice points."""
    P = np.asarray(pts, float)
    ys = np.sort(P[:, 1])
    xs = np.sort(P[:, 0])
    def pitch(v):
        d = np.diff(v)
        d = d[d > 0]
        if len(d) == 0:
            return 1.0
        big = d[d > 0.4 * d.max()]
        return float(np.median(big)) if len(big) else float(np.median(d))
    py, px = pitch(ys), pitch(xs)
    tol = tol_frac * min(px, py)
    def grp(v, tol):
        o = np.argsort(v)
        g = [[o[0]]]
        for i in o[1:]:
            if v[i] - v[g[-1][-1]] <= tol:
                g[-1].append(i)
            else:
                g.append([i])
        return g
    rg = grp(P[:, 1], tol)
    cg = grp(P[:, 0], tol)
    ri = np.zeros(len(P), int); ci = np.zeros(len(P), int)
    for k, g in enumerate(rg):
        for i in g:
            ri[i] = k
    for k, g in enumerate(cg):
        for i in g:
            ci[i] = k
    return ri, ci, px, py


def fit_and_residual(src, dst, kind):
    """src = ideal lattice (inches), dst = measured (px). Return residual px."""
    S = np.asarray(src, np.float64)
    D = np.asarray(dst, np.float64)
    n = len(S)
    if kind == "similarity":
        A = np.zeros((2 * n, 4)); b = np.zeros(2 * n)
        A[0::2, 0] = S[:, 0]; A[0::2, 1] = -S[:, 1]; A[0::2, 2] = 1
        A[1::2, 0] = S[:, 1]; A[1::2, 1] = S[:, 0]; A[1::2, 3] = 1
        b[0::2] = D[:, 0]; b[1::2] = D[:, 1]
        p, *_ = np.linalg.lstsq(A, b, rcond=None)
        pred = np.column_stack([p[0] * S[:, 0] - p[1] * S[:, 1] + p[2],
                                p[1] * S[:, 0] + p[0] * S[:, 1] + p[3]])
    elif kind == "affine":
        A = np.column_stack([S[:, 0], S[:, 1], np.ones(n)])
        px_, *_ = np.linalg.lstsq(A, D[:, 0], rcond=None)
        py_, *_ = np.linalg.lstsq(A, D[:, 1], rcond=None)
        pred = np.column_stack([A @ px_, A @ py_])
    elif kind == "homography":
        Hm, _ = cv2.findHomography(S.astype(np.float32), D.astype(np.float32), 0)
        if Hm is None:
            return None, None
        q = np.column_stack([S, np.ones(n)]) @ Hm.T
        pred = q[:, :2] / q[:, 2:3]
    elif kind == "biquadratic":
        A = np.column_stack([np.ones(n), S[:, 0], S[:, 1], S[:, 0] ** 2,
                             S[:, 0] * S[:, 1], S[:, 1] ** 2])
        px_, *_ = np.linalg.lstsq(A, D[:, 0], rcond=None)
        py_, *_ = np.linalg.lstsq(A, D[:, 1], rcond=None)
        pred = np.column_stack([A @ px_, A @ py_])
    else:
        raise ValueError(kind)
    res = np.hypot(*(D - pred).T)
    return res, pred


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--grid", required=True)
    ap.add_argument("--scan-dir", required=True)
    ap.add_argument("--files", nargs="*")
    ap.add_argument("--nominal-pitch", type=float, default=None,
                    help="ideal bull pitch in inches; default = measured median")
    ap.add_argument("--outline-dir")
    ap.add_argument("--max-rows", type=int,
                    help="use only the first N lattice rows (drops a sighter row)")
    ap.add_argument("--radius-filter", type=float, default=0.18,
                    help="drop detections whose fitted radius is more than this "
                         "fraction from the median (removes barcode/text FPs)")
    a = ap.parse_args()
    G = {r["file"]: r for r in json.load(open(a.grid))}
    files = a.files or sorted(G)
    for fn in files:
        g = G.get(fn)
        if not g or "centres_px" not in g or not g["centres_px"]:
            continue
        p = os.path.join(a.scan_dir, fn)
        if not os.path.exists(p):
            continue
        dpi = g["dpi"]
        C = np.array(g["centres_px"], float)
        rmed = np.median(C[:, 2])
        keep = np.abs(C[:, 2] - rmed) <= a.radius_filter * rmed
        C = C[keep]
        print("=" * 78)
        print("%s  dpi=%g  bulls used=%d (of %d; %d dropped by radius filter)"
              % (fn, dpi, len(C), len(keep), int((~keep).sum())))
        ri, ci, px, py = index_lattice(C[:, :2])
        if a.max_rows and ri.max() + 1 > a.max_rows:
            m = ri < a.max_rows
            C, ri, ci = C[m], ri[m], ci[m]
            print("  restricted to first %d rows (%d bulls)" % (a.max_rows, len(C)))
        nr, nc = ri.max() + 1, ci.max() + 1
        print("  lattice %d rows x %d cols, measured pitch %.1f x %.1f px"
              % (nr, nc, px, py))
        pitch = a.nominal_pitch or round(float(np.median([px, py])) / dpi, 3)
        ideal = np.column_stack([ci * pitch, ri * pitch])
        for kind in ("similarity", "affine", "homography", "biquadratic"):
            res, _ = fit_and_residual(ideal, C[:, :2], kind)
            if res is None:
                continue
            print("  %-13s residual  rms %7.2f px = %.4f in   max %7.2f px = %.4f in"
                  % (kind, res.mean(), res.mean() / dpi, res.max(), res.max() / dpi))
            if kind == "homography":
                grid = np.full((nr, nc), np.nan)
                for k in range(len(C)):
                    grid[ri[k], ci[k]] = res[k] / dpi
                print("    per-bull homography residual, inches (row-major):")
                for r_ in range(nr):
                    print("      " + " ".join("  .   " if np.isnan(v) else "%6.4f" % v
                                              for v in grid[r_]))
        rgb, d2, _ = load_rgb(p)
        cnt, s = page_outline(rgb, dpi)
        if cnt is not None:
            info, box = outline_vs_rect(cnt, s, dpi)
            print("  page outline: %s" % json.dumps(info))
            if a.outline_dir:
                os.makedirs(a.outline_dir, exist_ok=True)
                sm = cv2.resize(rgb, (rgb.shape[1] // s, rgb.shape[0] // s))
                ov = cv2.cvtColor(sm, cv2.COLOR_RGB2BGR)
                cv2.drawContours(ov, [cnt.astype(np.int32)], -1, (0, 0, 255), 2)
                cv2.drawContours(ov, [box.astype(np.int32)], -1, (0, 255, 0), 2)
                cv2.imwrite(os.path.join(a.outline_dir,
                            os.path.splitext(fn)[0] + "_outline.png"), ov)
        else:
            print("  page outline: NOT SEGMENTED (sheet not separable from background)")


if __name__ == "__main__":
    main()
