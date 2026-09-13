#!/usr/bin/env python3
"""s08_line_straightness.py -- dense probe of page warp using the printed
cell-border lines.

The bull-centre lattice gives only 20-30 samples of the page deformation. The
printed grid rectangle on the OnTarget #1 / #3 styles gives a continuous line
across the whole sheet, so its departure from a straight line is a far denser
measurement of local warping.

For each near-horizontal and near-vertical printed rule the script traces the
line centre column-by-column (or row-by-row) at sub-pixel accuracy using the
ink-weighted centroid, fits a straight line by total least squares, and reports
the residual profile: RMS, max, and where the max occurs. It also fits a cubic
to show how much of the residual is smooth bow versus local kink.

Usage:
    python3 s08_line_straightness.py <image> [...] [--dpi N] [--plot-dir DIR]
"""
import argparse
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb, ink_mask, list_images


def trace_line(ink, axis, pos, halfwidth, step, min_ink=25):
    """Trace an ink rule near `pos` along `axis` (0 = horizontal rule).

    Returns (t, c) where t is the along-line coordinate and c the sub-pixel
    cross-line centroid of the ink.
    """
    h, w = ink.shape
    ts, cs = [], []
    n = w if axis == 0 else h
    for t in range(0, n, step):
        a = int(max(0, pos - halfwidth))
        b = int(min((h if axis == 0 else w), pos + halfwidth))
        col = ink[a:b, t].astype(np.float64) if axis == 0 else ink[t, a:b].astype(np.float64)
        if col.max() < min_ink:
            continue
        wgt = np.clip(col - min_ink, 0, None)
        s = wgt.sum()
        if s <= 0:
            continue
        c = a + float((np.arange(len(col)) * wgt).sum() / s)
        ts.append(t)
        cs.append(c)
    return np.array(ts, float), np.array(cs, float)


def find_rules(ink, axis, dpi, min_len_frac=0.55, thresh=45):
    """Return approximate cross-axis positions of long printed rules."""
    bw = (ink >= thresh).astype(np.uint8)
    L = int(min_len_frac * (bw.shape[1] if axis == 0 else bw.shape[0]))
    ker = cv2.getStructuringElement(cv2.MORPH_RECT, (L, 1) if axis == 0 else (1, L))
    lines = cv2.morphologyEx(bw, cv2.MORPH_OPEN, ker)
    prof = lines.sum(axis=1 if axis == 0 else 0)
    peaks = []
    thr = 0.35 * prof.max() if prof.max() else 1e9
    i = 0
    while i < len(prof):
        if prof[i] > thr:
            j = i
            while j < len(prof) and prof[j] > thr:
                j += 1
            peaks.append((i + j - 1) / 2.0)
            i = j
        else:
            i += 1
    return peaks


def analyse(path, dpi_override=None, plot_dir=None):
    rgb, dpi, _ = load_rgb(path)
    if dpi_override:
        dpi = dpi_override
    dpi = dpi or 600.0
    ink = ink_mask(rgb)
    step = max(1, int(round(dpi / 100.0)))
    hw = int(0.05 * dpi)
    print("=" * 78)
    print("%s  dpi=%g" % (os.path.basename(path), dpi))
    out = []
    for axis, name in ((0, "horizontal"), (1, "vertical")):
        rules = find_rules(ink, axis, dpi)
        print("  %s rules found: %d at %s"
              % (name, len(rules), [round(r) for r in rules]))
        for pos in rules:
            t, c = trace_line(ink, axis, pos, hw, step)
            if len(t) < 50:
                continue
            # robust straight-line fit (2 IRLS trims)
            keep = np.ones(len(t), bool)
            for _ in range(3):
                p = np.polyfit(t[keep], c[keep], 1)
                r = c - np.polyval(p, t)
                keep = np.abs(r) <= 3.5 * np.std(r[keep])
                if keep.sum() < 30:
                    break
            r = c - np.polyval(p, t)
            r = r[keep]; tk = t[keep]
            pc = np.polyfit(tk, c[keep], 3)
            rc = c[keep] - np.polyval(pc, tk)
            span = (tk.max() - tk.min()) / dpi
            rec = {
                "axis": name, "pos_px": round(pos, 1), "span_in": round(span, 2),
                "n": int(keep.sum()),
                "lin_rms_in": round(float(np.sqrt((r ** 2).mean())) / dpi, 5),
                "lin_max_in": round(float(np.abs(r).max()) / dpi, 5),
                "lin_max_at_in": round(float(tk[np.argmax(np.abs(r))]) / dpi, 2),
                "cubic_rms_in": round(float(np.sqrt((rc ** 2).mean())) / dpi, 5),
                "cubic_max_in": round(float(np.abs(rc).max()) / dpi, 5),
                "slope_deg": round(float(np.degrees(np.arctan(p[0]))), 4),
            }
            out.append(rec)
            print("    %-10s at %6.1f px  span %5.2f in  n=%4d | "
                  "straight-line residual rms %.5f in max %.5f in (at %.2f in) | "
                  "after cubic rms %.5f max %.5f | slope %+.4f deg"
                  % (name, pos, span, rec["n"], rec["lin_rms_in"], rec["lin_max_in"],
                     rec["lin_max_at_in"], rec["cubic_rms_in"], rec["cubic_max_in"],
                     rec["slope_deg"]))
    if out:
        lm = max(o["lin_max_in"] for o in out)
        lr = float(np.mean([o["lin_rms_in"] for o in out]))
        cm = max(o["cubic_max_in"] for o in out)
        print("  SUMMARY: worst straight-line departure %.5f in; mean rms %.5f in; "
              "worst residual after a smooth cubic %.5f in" % (lm, lr, cm))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("images", nargs="+")
    ap.add_argument("--dpi", type=float)
    ap.add_argument("--plot-dir")
    a = ap.parse_args()
    for t in a.images:
        for f in list_images(t):
            analyse(f, a.dpi, a.plot_dir)


if __name__ == "__main__":
    main()
