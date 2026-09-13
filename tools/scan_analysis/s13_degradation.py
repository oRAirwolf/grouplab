#!/usr/bin/env python3
"""s13_degradation.py -- what a messaging-app round trip costs.

Compares a low-resolution / re-encoded copy of a target against its
higher-resolution original. Measures:

  * effective sampling: pixels across one hole diameter, and pixels across the
    printed ring stroke
  * JPEG 8x8 blockiness: the standard blocking metric -- mean absolute
    difference across block boundaries vs across non-boundary columns/rows.
    A ratio of 1.0 means no visible blocking.
  * high-frequency energy retained, by radially averaging the FFT magnitude of
    both images after resampling the original to the small image's grid
  * hole-vs-artwork separability at each resolution, as a Fisher ratio, so the
    question "are holes still separable at all" gets a number
  * smallest reliably resolved feature: the finest printed feature (ring stroke,
    numeral stroke, barcode bar) still measurable, found by sweeping a bar
    width and checking modulation depth

Usage:
    python3 s13_degradation.py --small SMALL.jpg --large LARGE.jpg
        [--small-dpi 93] [--holes holes.json] [--grid grid.json]
"""
import argparse
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb


def blockiness(gray, period=8):
    g = gray.astype(np.float32)
    dh = np.abs(np.diff(g, axis=1))
    dv = np.abs(np.diff(g, axis=0))
    cols = np.arange(dh.shape[1])
    rows = np.arange(dv.shape[0])
    bh = dh[:, (cols % period) == (period - 1)].mean()
    nh = dh[:, (cols % period) != (period - 1)].mean()
    bv = dv[(rows % period) == (period - 1), :].mean()
    nv = dv[(rows % period) != (period - 1), :].mean()
    return {"h_boundary": float(bh), "h_interior": float(nh),
            "v_boundary": float(bv), "v_interior": float(nv),
            "ratio_h": float(bh / nh) if nh else None,
            "ratio_v": float(bv / nv) if nv else None}


def radial_fft(gray):
    g = gray.astype(np.float32)
    g = g - g.mean()
    n = min(g.shape)
    n = 1 << int(np.floor(np.log2(n)))
    g = g[:n, :n] * np.outer(np.hanning(n), np.hanning(n))
    F = np.abs(np.fft.fftshift(np.fft.fft2(g)))
    yy, xx = np.mgrid[0:n, 0:n] - n / 2
    r = np.hypot(xx, yy).astype(int)
    prof = np.bincount(r.ravel(), F.ravel()) / np.maximum(np.bincount(r.ravel()), 1)
    return prof[:n // 2], n


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--small", required=True)
    ap.add_argument("--large", required=True)
    ap.add_argument("--small-dpi", type=float)
    ap.add_argument("--holes")
    ap.add_argument("--grid")
    a = ap.parse_args()
    S, sd, sknown = load_rgb(a.small)
    L, ld, lknown = load_rgb(a.large)
    sdpi = a.small_dpi or sd
    print("small: %s %dx%d dpi=%s (tag present: %s)"
          % (os.path.basename(a.small), S.shape[1], S.shape[0], sdpi, sknown))
    print("large: %s %dx%d dpi=%s (tag present: %s)"
          % (os.path.basename(a.large), L.shape[1], L.shape[0], ld, lknown))
    print("linear resolution ratio: %.3f  (area ratio %.3f)"
          % (L.shape[1] / S.shape[1], (L.shape[1] * L.shape[0]) /
             float(S.shape[1] * S.shape[0])))

    gs = cv2.cvtColor(S, cv2.COLOR_RGB2GRAY)
    gl = cv2.cvtColor(L, cv2.COLOR_RGB2GRAY)

    if a.grid:
        G = {r["file"]: r for r in json.load(open(a.grid))}
        for tag, fn, dpi in (("small", os.path.basename(a.small), sdpi),
                             ("large", os.path.basename(a.large), ld)):
            g = G.get(fn)
            if not g:
                continue
            print("  %s: fitted ring radius %.2f px, stroke %.4f in = %.2f px"
                  % (tag, g["ring_fit_radius_px"]["mean"],
                     g.get("ring_stroke_in", 0), g.get("ring_stroke_in", 0) * dpi))
    if a.holes:
        H = {r["file"]: r for r in json.load(open(a.holes))}
        for tag, fn, dpi in (("small", os.path.basename(a.small), sdpi),
                             ("large", os.path.basename(a.large), ld)):
            h = H.get(fn)
            if not h or not h["holes"]:
                print("  %s: no holes in the supplied JSON" % tag)
                continue
            d = np.array([x["blob_dia_in"] for x in h["holes"]])
            print("  %s: %d holes, mean dia %.4f in = %.1f px across"
                  % (tag, len(d), d.mean(), d.mean() * dpi))

    print("\nJPEG 8x8 BLOCKINESS (mean |gradient| across block edges vs inside)")
    for tag, g in (("small", gs), ("large", gl)):
        b = blockiness(g)
        print("  %-6s horiz boundary %.3f vs interior %.3f -> ratio %.3f | "
              "vert boundary %.3f vs interior %.3f -> ratio %.3f"
              % (tag, b["h_boundary"], b["h_interior"], b["ratio_h"],
                 b["v_boundary"], b["v_interior"], b["ratio_v"]))

    print("\nSPATIAL FREQUENCY CONTENT")
    # bring the large one to the small one's sampling grid
    ld_ = cv2.resize(gl, (gs.shape[1], gs.shape[0]), interpolation=cv2.INTER_AREA)
    ps, n = radial_fft(gs)
    pl, _ = radial_fft(ld_)
    print("  comparing both at %dx%d (%.0f dpi effective)" % (n, n, sdpi))
    print("%12s %12s %12s %12s %10s"
          % ("cyc/px", "cyc/in", "lp/in", "small |F|", "orig |F|"))
    for frac in (0.05, 0.10, 0.20, 0.30, 0.40, 0.45, 0.49):
        k = int(frac * len(ps) * 2)
        if k >= len(ps):
            continue
        cpp = k / float(n)
        print("%12.4f %12.1f %12.1f %12.1f %10.1f"
              % (cpp, cpp * sdpi, cpp * sdpi, ps[k], pl[k]))
    hi = slice(int(0.3 * len(ps)), len(ps))
    print("  high-frequency energy (top 40%% of band) retained: %.1f%% of the "
          "resampled original" % (100 * ps[hi].sum() / max(pl[hi].sum(), 1e-9)))

    print("\nSMALLEST RESOLVED FEATURE (modulation depth of a printed bar of "
          "given width)")
    for tag, g, dpi in (("small", gs, sdpi), ("large", gl, ld)):
        rows = []
        for wmil in (5, 10, 15, 20, 30, 40, 55, 75):
            wpx = wmil / 1000.0 * dpi
            if wpx < 0.8:
                rows.append((wmil, wpx, None))
                continue
            k = max(3, int(round(wpx)) | 1)
            blur = cv2.GaussianBlur(g, (0, 0), max(0.4, wpx / 2.5))
            mod = float(np.percentile(np.abs(g.astype(np.float32) -
                                             blur.astype(np.float32)), 99.9))
            rows.append((wmil, wpx, mod))
        print("  %-6s " % tag + "  ".join(
            "%dmil=%.1fpx:%s" % (w, p, "n/a" if m is None else "%.0f" % m)
            for w, p, m in rows))


if __name__ == "__main__":
    main()
