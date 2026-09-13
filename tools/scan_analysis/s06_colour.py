#!/usr/bin/env python3
"""s06_colour.py -- per-channel statistics for paper / artwork / hole core /
hole annulus, across the differently-coloured target styles.

Classes are sampled from geometry, not from hand-drawn boxes:

  paper     : pixels far from any bull centre and far from any detected hole
  artwork   : pixels inside the printed ring band of a bull that has no hole
              near it (ring band taken from s04's fitted radius and stroke)
  core      : pixels inside 0.35 x the detected hole radius
  annulus   : pixels in the 0.8 - 1.15 x hole-radius shell

Reports mean +/- sd of R, G, B, H, S, V, L*, a*, b* for each class, and a
separability score (Fisher discriminant) of hole-vs-artwork for every single
channel and for the neutral-darkness combination Dn = paper - max(R,G,B).

Usage:
    python3 s06_colour.py --grid work/bull_grid.json --holes work/holes_all.json
                          --scan-dir DIR [--files a.jpg b.jpg ...] [--csv out.csv]
"""
import argparse
import csv
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb

CHANNELS = ["R", "G", "B", "H", "S", "V", "L*", "a*", "b*", "Dn", "chroma"]


def channel_stack(rgb):
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
    lab = cv2.cvtColor(rgb, cv2.COLOR_RGB2LAB).astype(np.float32)
    f = rgb.astype(np.float32)
    mx = f.max(axis=2)
    mn = f.min(axis=2)
    paper_mx = float(np.percentile(mx, 90))
    dn = np.clip(paper_mx - mx, 0, 255)
    chroma = mx - mn
    # OpenCV: H 0..179 (deg/2), S/V 0..255, L 0..255, a/b 0..255 offset 128
    return np.dstack([
        f[:, :, 0], f[:, :, 1], f[:, :, 2],
        hsv[:, :, 0] * 2.0, hsv[:, :, 1], hsv[:, :, 2],
        lab[:, :, 0] * 100.0 / 255.0, lab[:, :, 1] - 128.0, lab[:, :, 2] - 128.0,
        dn, chroma,
    ])


def masks_for(rgb, dpi, bulls, holes, ring_r_px, stroke_px):
    h, w = rgb.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    hole_near = np.zeros((h, w), bool)
    core = np.zeros((h, w), bool)
    ann = np.zeros((h, w), bool)
    for hh in holes:
        r = hh["blob_dia_px"] / 2.0
        d = np.hypot(xx - hh["cx"], yy - hh["cy"])
        core |= d < 0.35 * r
        ann |= (d > 0.80 * r) & (d < 1.15 * r)
        hole_near |= d < 2.2 * r
    art = np.zeros((h, w), bool)
    bull_near = np.zeros((h, w), bool)
    for (bx, by, br) in bulls:
        d = np.hypot(xx - bx, yy - by)
        bull_near |= d < br * 1.4
        art |= np.abs(d - br) < stroke_px * 0.30
    art &= ~hole_near
    paper = (~bull_near) & (~hole_near)
    # trim the page border (scanner edge shading, tears)
    m = int(0.35 * dpi)
    edge = np.zeros((h, w), bool)
    edge[m:h - m, m:w - m] = True
    paper &= edge
    return {"paper": paper, "artwork": art, "hole_core": core, "hole_annulus": ann}


def summarise(stack, mask, nmax=400000):
    idx = np.flatnonzero(mask.ravel())
    if len(idx) == 0:
        return None
    if len(idx) > nmax:
        idx = np.random.default_rng(0).choice(idx, nmax, replace=False)
    flat = stack.reshape(-1, stack.shape[2])[idx]
    return {"n": int(len(idx)),
            "mean": flat.mean(axis=0), "sd": flat.std(axis=0)}


def fisher(a, b):
    """Fisher discriminant ratio per channel: (mu_a-mu_b)^2 / (sd_a^2+sd_b^2)."""
    return (a["mean"] - b["mean"]) ** 2 / np.maximum(a["sd"] ** 2 + b["sd"] ** 2, 1e-6)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--grid", required=True)
    ap.add_argument("--holes", required=True)
    ap.add_argument("--scan-dir", required=True)
    ap.add_argument("--files", nargs="*")
    ap.add_argument("--csv")
    a = ap.parse_args()
    G = {r["file"]: r for r in json.load(open(a.grid))}
    H = {r["file"]: r for r in json.load(open(a.holes))}
    files = a.files or sorted(H)
    rows = []
    for fn in files:
        p = os.path.join(a.scan_dir, fn)
        if not os.path.exists(p):
            print("missing", p, file=sys.stderr)
            continue
        g = G.get(fn)
        if not g or "centres_px" not in g:
            print("no grid for", fn, file=sys.stderr)
            continue
        rgb, dpi, _ = load_rgb(p)
        dpi = dpi or 600.0
        bulls = g["centres_px"]
        stroke_px = g.get("ring_stroke_in", 0.05) * dpi
        holes = H[fn]["holes"]
        st = channel_stack(rgb)
        ms = masks_for(rgb, dpi, bulls, holes, g["ring_fit_radius_px"]["mean"], stroke_px)
        stats = {k: summarise(st, v) for k, v in ms.items()}
        print("=" * 78)
        print(fn)
        hdr = "%-13s %7s " % ("class", "n") + " ".join("%8s" % c for c in CHANNELS)
        print(hdr)
        for k in ["paper", "artwork", "hole_core", "hole_annulus"]:
            s = stats[k]
            if s is None:
                print("%-13s   (no pixels)" % k)
                continue
            print("%-13s %7d " % (k, s["n"]) + " ".join("%8.1f" % v for v in s["mean"]))
            print("%-13s %7s " % ("  sd", "") + " ".join("%8.1f" % v for v in s["sd"]))
            if a.csv:
                rows.append([fn, k, s["n"]] +
                            [round(float(v), 2) for v in s["mean"]] +
                            [round(float(v), 2) for v in s["sd"]])
        if stats["artwork"] and stats["hole_annulus"]:
            f1 = fisher(stats["hole_annulus"], stats["artwork"])
            f2 = fisher(stats["hole_core"], stats["artwork"]) if stats["hole_core"] else None
            print("%-13s %7s " % ("F(ann|art)", "") + " ".join("%8.2f" % v for v in f1))
            if f2 is not None:
                print("%-13s %7s " % ("F(core|art)", "") + " ".join("%8.2f" % v for v in f2))
            best = int(np.argmax(f1))
            print("  best single channel for annulus-vs-artwork: %s (F=%.2f)"
                  % (CHANNELS[best], f1[best]))
    if a.csv:
        with open(a.csv, "w", newline="") as fh:
            w = csv.writer(fh)
            w.writerow(["file", "class", "n"] + ["mean_" + c for c in CHANNELS]
                       + ["sd_" + c for c in CHANNELS])
            w.writerows(rows)


if __name__ == "__main__":
    main()
