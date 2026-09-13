#!/usr/bin/env python3
"""s04_bull_grid.py -- locate the printed bullseyes and measure the grid.

Method
------
1. Build an ink mask (common.ink_mask) at a 150 DPI working scale.
2. Sweep an annulus matched filter over a wide radius range; pick the radius
   whose top-N peak response is strongest -> the ring radius.
3. Non-max-suppress the response at that radius -> candidate bull centres.
4. Refine every centre at full resolution by fitting a circle to the ink
   pixels of the ring band.
5. Cluster centres into rows and columns, report pitch (px and in) with
   standard deviation, and fit the grid rotation.
6. Measure ring outer/inner diameter from the angle-averaged radial ink
   profile around each refined centre.

Usage:
    python3 s04_bull_grid.py <scan_dir_or_file> [...] [--json out.json]
                             [--work-dpi 150] [--overlay-dir DIR]
"""
import argparse
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import (ink_mask, load_rgb, list_images, downscale,
                    annulus_template, nms_peaks, fit_circle)

WORK_DPI = 150.0


def detect_radius(ink_s, dpi_s, rmin_in=0.15, rmax_in=1.05, steps=36):
    """Sweep ring radius; return (best_r_px, response_map, all_scores)."""
    best = None
    scores = []
    for r_in in np.linspace(rmin_in, rmax_in, steps):
        r = r_in * dpi_s
        if r < 4:
            continue
        w = max(1.5, 0.012 * dpi_s)  # ring stroke ~0.012 in
        t = annulus_template(r, w)
        if t.shape[0] >= min(ink_s.shape[:2]):
            continue
        resp = cv2.filter2D(ink_s.astype(np.float32), -1, t,
                            borderType=cv2.BORDER_REPLICATE)
        # score = mean of the top 20 well-separated peaks
        pk = nms_peaks(resp, int(r * 1.6), -1e9)[:20]
        sc = float(np.mean([p[2] for p in pk])) if pk else 0.0
        scores.append((r_in, sc))
        if best is None or sc > best[1]:
            best = (r, sc, resp, r_in)
    return best, scores


def refine_centre(ink_full, cx, cy, r, band=0.28):
    """Fit a circle to the strongest ink pixels in the ring band at full res."""
    R = int(r * (1 + band)) + 3
    h, w = ink_full.shape
    x0, x1 = max(0, int(cx - R)), min(w, int(cx + R) + 1)
    y0, y1 = max(0, int(cy - R)), min(h, int(cy + R) + 1)
    sub = ink_full[y0:y1, x0:x1].astype(np.float32)
    if sub.size == 0:
        return None
    yy, xx = np.mgrid[y0:y1, x0:x1]
    d = np.hypot(xx - cx, yy - cy)
    m = (d > r * (1 - band)) & (d < r * (1 + band))
    if m.sum() < 50:
        return None
    vals = sub[m]
    thr = max(40.0, np.percentile(vals, 70))
    sel = m & (sub >= thr)
    if sel.sum() < 40:
        return None
    pts = np.column_stack([xx[sel], yy[sel]])
    # robust: two IRLS passes trimming 15% worst residuals
    for _ in range(3):
        cxf, cyf, rf, rms = fit_circle(pts)
        res = np.abs(np.hypot(pts[:, 0] - cxf, pts[:, 1] - cyf) - rf)
        keep = res <= np.percentile(res, 85)
        if keep.sum() < 40:
            break
        pts = pts[keep]
    return cxf, cyf, rf, rms, int(len(pts))


def radial_profile(img, cx, cy, rmax, nbins):
    h, w = img.shape
    x0, x1 = max(0, int(cx - rmax)), min(w, int(cx + rmax) + 1)
    y0, y1 = max(0, int(cy - rmax)), min(h, int(cy + rmax) + 1)
    sub = img[y0:y1, x0:x1].astype(np.float32)
    yy, xx = np.mgrid[y0:y1, x0:x1]
    d = np.hypot(xx - cx, yy - cy)
    b = np.clip((d / rmax * nbins).astype(int), 0, nbins - 1)
    s = np.bincount(b.ravel(), weights=sub.ravel(), minlength=nbins)
    n = np.bincount(b.ravel(), minlength=nbins)
    return np.where(n > 0, s / np.maximum(n, 1), np.nan), n


def ring_diameters(ink_full, cx, cy, r):
    """Outer/inner ring diameter from the angle-averaged ink profile."""
    rmax = r * 1.5
    nb = int(rmax)
    prof, n = radial_profile(ink_full, cx, cy, rmax, nb)
    rr = (np.arange(nb) + 0.5) / nb * rmax
    lo, hi = int(0.55 * nb / 1.5), int(1.45 * nb / 1.5)
    seg = prof[lo:hi]
    if not np.isfinite(seg).any():
        return None
    pk = np.nanmax(seg)
    base = np.nanpercentile(prof[int(0.15 * nb):int(0.55 * nb / 1.5)], 20)
    half = (pk + base) / 2.0
    i = lo + int(np.nanargmax(seg))
    a = i
    while a > 0 and prof[a] > half:
        a -= 1
    b = i
    while b < nb - 1 and prof[b] > half:
        b += 1
    return 2 * rr[a], 2 * rr[b], float(pk), float(base)


def lattice(cx, cy, tol):
    """Cluster into rows/cols; return (rows, cols, row_ids, col_ids)."""
    def cl(vals):
        order = np.argsort(vals)
        g = [[order[0]]]
        for i in order[1:]:
            if vals[i] - vals[g[-1][-1]] <= tol:
                g[-1].append(i)
            else:
                g.append([i])
        return g
    rg = cl(cy)
    cg = cl(cx)
    rid = np.zeros(len(cx), int)
    cid = np.zeros(len(cx), int)
    for k, g in enumerate(rg):
        for i in g:
            rid[i] = k
    for k, g in enumerate(cg):
        for i in g:
            cid[i] = k
    return len(rg), len(cg), rid, cid


def analyse(path, work_dpi=WORK_DPI, overlay_dir=None, dpi_override=None):
    rgb, dpi, dpi_known = load_rgb(path)
    if dpi_override:
        dpi = dpi_override
    if dpi is None:
        dpi = 600.0
    ink_full = ink_mask(rgb)
    ink_s, f = downscale(ink_full, dpi, work_dpi)
    dpi_s = dpi * f
    best, scores = detect_radius(ink_s, dpi_s)
    rec = {"file": os.path.basename(path), "dpi": dpi, "dpi_tag_present": dpi_known,
           "work_dpi": dpi_s, "radius_sweep": [(round(a, 3), round(b, 1)) for a, b in scores]}
    if best is None:
        rec["error"] = "no ring radius found"
        return rec
    r_s, sc, resp, r_in = best
    rec["ring_radius_in_coarse"] = round(r_in, 4)
    peaks = nms_peaks(resp, int(r_s * 1.5), 0.45 * sc)
    rec["coarse_peaks"] = len(peaks)
    ref = []
    for x, y, v in peaks:
        X, Y = x / f, y / f
        out = refine_centre(ink_full, X, Y, r_s / f)
        if out is None:
            continue
        cxf, cyf, rf, rms, npts = out
        if abs(rf - r_s / f) > 0.25 * r_s / f:
            continue
        ref.append((cxf, cyf, rf, rms, npts, v))
    # de-duplicate refined centres
    ref.sort(key=lambda t: -t[5])
    keep = []
    for c in ref:
        if all(np.hypot(c[0] - k[0], c[1] - k[1]) > 0.6 * c[2] for k in keep):
            keep.append(c)
    ref = keep
    rec["bulls_found"] = len(ref)
    if not ref:
        return rec
    A = np.array(ref)
    cx, cy, rr = A[:, 0], A[:, 1], A[:, 2]
    rec["ring_fit_radius_px"] = {"mean": round(float(rr.mean()), 2),
                                 "sd": round(float(rr.std()), 2),
                                 "min": round(float(rr.min()), 2),
                                 "max": round(float(rr.max()), 2)}
    rec["ring_fit_rms_px"] = round(float(A[:, 3].mean()), 2)
    tol = 0.7 * float(np.median(rr))
    nr, nc, rid, cid = lattice(cx, cy, tol)
    rec["grid_rows"], rec["grid_cols"] = nr, nc
    # pitch: per-row consecutive x gaps, per-col consecutive y gaps
    dxs, dys = [], []
    for k in range(nr):
        xs = np.sort(cx[rid == k])
        dxs += list(np.diff(xs))
    for k in range(nc):
        ys = np.sort(cy[cid == k])
        dys += list(np.diff(ys))
    # drop gaps that are integer multiples > 1 (missing bull) by median filtering
    def clean(v):
        v = np.array(v, float)
        if len(v) == 0:
            return v
        m = np.median(v)
        return v[(v > 0.6 * m) & (v < 1.5 * m)]
    dxs, dys = clean(dxs), clean(dys)
    for nm, d in (("x", dxs), ("y", dys)):
        if len(d):
            rec["pitch_%s_px" % nm] = {"mean": round(float(d.mean()), 2),
                                       "sd": round(float(d.std(ddof=1)) if len(d) > 1 else 0.0, 2),
                                       "n": int(len(d))}
            rec["pitch_%s_in" % nm] = {"mean": round(float(d.mean() / dpi), 5),
                                       "sd": round(float(d.std(ddof=1) / dpi) if len(d) > 1 else 0.0, 5)}
    # rotation: fit a line through each row (>=3 pts) and each column
    angs_r, angs_c = [], []
    for k in range(nr):
        m = rid == k
        if m.sum() >= 3:
            p = np.polyfit(cx[m], cy[m], 1)
            angs_r.append(np.degrees(np.arctan(p[0])))
    for k in range(nc):
        m = cid == k
        if m.sum() >= 3:
            p = np.polyfit(cy[m], cx[m], 1)
            angs_c.append(-np.degrees(np.arctan(p[0])))
    if angs_r:
        rec["rotation_from_rows_deg"] = {"mean": round(float(np.mean(angs_r)), 4),
                                         "sd": round(float(np.std(angs_r)), 4)}
    if angs_c:
        rec["rotation_from_cols_deg"] = {"mean": round(float(np.mean(angs_c)), 4),
                                         "sd": round(float(np.std(angs_c)), 4)}
    # ring outer / inner diameters
    od, idd = [], []
    for c in ref:
        rd = ring_diameters(ink_full, c[0], c[1], c[2])
        if rd:
            idd.append(rd[0])
            od.append(rd[1])
    if od:
        rec["ring_outer_dia_px"] = round(float(np.median(od)), 2)
        rec["ring_inner_dia_px"] = round(float(np.median(idd)), 2)
        rec["ring_outer_dia_in"] = round(float(np.median(od)) / dpi, 4)
        rec["ring_inner_dia_in"] = round(float(np.median(idd)) / dpi, 4)
        rec["ring_stroke_in"] = round((np.median(od) - np.median(idd)) / 2 / dpi, 4)
    rec["centres_px"] = [[round(float(c[0]), 1), round(float(c[1]), 1),
                          round(float(c[2]), 1)] for c in ref]
    if overlay_dir:
        os.makedirs(overlay_dir, exist_ok=True)
        ov = cv2.cvtColor(cv2.resize(rgb, (rgb.shape[1] // 4, rgb.shape[0] // 4)),
                          cv2.COLOR_RGB2BGR)
        for c in ref:
            cv2.circle(ov, (int(c[0] / 4), int(c[1] / 4)), int(c[2] / 4), (0, 255, 0), 2)
            cv2.drawMarker(ov, (int(c[0] / 4), int(c[1] / 4)), (255, 0, 255),
                           cv2.MARKER_CROSS, 10, 2)
        cv2.imwrite(os.path.join(overlay_dir,
                    os.path.splitext(os.path.basename(path))[0] + "_bulls.png"), ov)
    return rec


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("targets", nargs="+")
    ap.add_argument("--json")
    ap.add_argument("--work-dpi", type=float, default=WORK_DPI)
    ap.add_argument("--overlay-dir")
    ap.add_argument("--dpi", type=float,
                    help="override the DPI tag (needed for images with none)")
    a = ap.parse_args()
    out = []
    for t in a.targets:
        for f in list_images(t):
            r = analyse(f, a.work_dpi, a.overlay_dir, a.dpi)
            r.pop("radius_sweep", None)
            cents = r.pop("centres_px", None)
            print(json.dumps(r, sort_keys=True))
            r["centres_px"] = cents
            out.append(r)
    if a.json:
        with open(a.json, "w") as fh:
            json.dump(out, fh, indent=1, sort_keys=True)


if __name__ == "__main__":
    main()
