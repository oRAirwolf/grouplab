#!/usr/bin/env python3
"""s11_naive_baselines.py -- reproduce the two published naive hole detectors
and count what they actually get right and wrong.

Baseline A -- FILLED CONTOUR. Binarise (Otsu on inverted grey), find external
contours, fill them, keep blobs whose equivalent diameter is roughly one
calibre. This is the "obvious" approach and the one reported as 6/25.

Baseline B -- GRADIENT ENERGY. Canny edges + cv2.HoughCircles over a calibre
radius range. Reported as 7/20.

Both are scored against a hand-verified ground-truth list of hole centres
(--truth), with a hit radius of --tol inches. Every false positive is
CLASSIFIED by what it actually sits on, using the fitted bull geometry:

    ring        -- on the printed bullseye ring band
    centre_dot  -- on the printed centre dot
    numeral     -- in the cell-number zone (upper-left corner of a cell)
    gridline    -- on a printed cell border
    barcode     -- in the barcode strip at the foot of the page
    text        -- in the footer text band
    handwriting -- elsewhere in the annotation band
    paper       -- none of the above

Usage:
    python3 s11_naive_baselines.py <image> --grid g.json --truth truth.json
        [--tol 0.15] [--overlay-dir DIR]
"""
import argparse
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb, ink_mask


def baseline_filled_contour(rgb, dpi, dmin=0.18, dmax=0.45, grey_cut=None,
                            min_circ=0.55):
    """grey_cut=None uses Otsu (the textbook default); pass a level to sweep."""
    g = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
    if grey_cut is None:
        _, bw = cv2.threshold(255 - g, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    else:
        bw = ((255 - g) >= (255 - grey_cut)).astype(np.uint8) * 255
    cnts, _ = cv2.findContours(bw, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    out = []
    amin = np.pi * (dmin * dpi / 2) ** 2
    amax = np.pi * (dmax * dpi / 2) ** 2
    for c in cnts:
        a = cv2.contourArea(c)
        if not (amin <= a <= amax):
            continue
        p = cv2.arcLength(c, True)
        circ = 4 * np.pi * a / (p * p) if p else 0
        if circ < min_circ:
            continue
        M = cv2.moments(c)
        if M["m00"] <= 0:
            continue
        out.append((M["m10"] / M["m00"], M["m01"] / M["m00"],
                    2 * np.sqrt(a / np.pi)))
    return out


def baseline_hough(rgb, dpi, rmin=0.09, rmax=0.22, param2=40):
    g = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
    s = max(1, int(round(dpi / 300.0)))
    sm = cv2.resize(g, (g.shape[1] // s, g.shape[0] // s), interpolation=cv2.INTER_AREA)
    d = dpi / s
    sm = cv2.medianBlur(sm, 5)
    c = cv2.HoughCircles(sm, cv2.HOUGH_GRADIENT, dp=1,
                         minDist=int(0.20 * d), param1=120, param2=param2,
                         minRadius=int(rmin * d), maxRadius=int(rmax * d))
    if c is None:
        return []
    return [(x * s, y * s, 2 * r * s) for x, y, r in c[0]]


def classify_fp(x, y, dpi, bulls, stroke_px, page_h, page_w):
    """What is this false positive actually sitting on?"""
    for (bx, by, br) in bulls:
        d = np.hypot(x - bx, y - by)
        if d < max(stroke_px * 1.2, 0.05 * dpi):
            return "centre_dot"
        if abs(d - br) < stroke_px * 1.2:
            return "ring"
        # OnTarget numerals sit up-left of each bull, about 0.8 x pitch away
        if (-1.05 * br < x - bx < -0.55 * br) and (-1.15 * br < y - by < -0.6 * br):
            return "numeral"
    if y > 0.955 * page_h and x > 0.55 * page_w:
        return "barcode"
    if y > 0.93 * page_h:
        return "text"
    ys = [b[1] for b in bulls]
    if bulls and y > max(ys) + 1.2 * bulls[0][2]:
        return "handwriting/annotation"
    return "paper/other"


def score(dets, truth, tol_px):
    used = set()
    tp = []
    fp = []
    for x, y, d in dets:
        best, bi = 1e18, -1
        for i, (tx, ty) in enumerate(truth):
            if i in used:
                continue
            dd = np.hypot(x - tx, y - ty)
            if dd < best:
                best, bi = dd, i
        if bi >= 0 and best <= tol_px:
            used.add(bi)
            tp.append((x, y, d, best))
        else:
            fp.append((x, y, d))
    fn = [t for i, t in enumerate(truth) if i not in used]
    return tp, fp, fn


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("image")
    ap.add_argument("--grid", required=True)
    ap.add_argument("--truth", required=True)
    ap.add_argument("--tol", type=float, default=0.15)
    ap.add_argument("--overlay-dir")
    a = ap.parse_args()
    rgb, dpi, _ = load_rgb(a.image)
    dpi = dpi or 600.0
    fn = os.path.basename(a.image)
    G = {r["file"]: r for r in json.load(open(a.grid))}[fn]
    bulls = G["centres_px"]
    stroke_px = G.get("ring_stroke_in", 0.05) * dpi
    T = json.load(open(a.truth))
    truth = [(t[0] * dpi, t[1] * dpi) for t in T[fn]]
    h, w = rgb.shape[:2]
    print("%s  dpi=%g  ground-truth holes: %d  hit tolerance %.3f in"
          % (fn, dpi, len(truth), a.tol))
    variants = [("A filled-contour (Otsu, circ>=0.55)",
                 baseline_filled_contour(rgb, dpi))]
    for cut in (180, 200, 215, 230, 240, 247, 250):
        for mc in (0.55, 0.30, 0.10):
            variants.append(("A filled-contour (grey<%d, circ>=%.2f)" % (cut, mc),
                             baseline_filled_contour(rgb, dpi, grey_cut=cut,
                                                     min_circ=mc)))
    for p2 in (25, 32, 40, 55):
        variants.append(("B Hough-circle (param2=%d)" % p2,
                         baseline_hough(rgb, dpi, param2=p2)))
    for name, dets in variants:
        tp, fp, fnl = score(dets, truth, a.tol * dpi)
        print("\n  %s: %d detections -> TP %d / %d, FP %d, FN %d"
              % (name, len(dets), len(tp), len(truth), len(fp), len(fnl)))
        cats = {}
        for x, y, d in fp:
            c = classify_fp(x, y, dpi, bulls, stroke_px, h, w)
            cats[c] = cats.get(c, 0) + 1
        if cats:
            print("    false positives by what they sit on: "
                  + ", ".join("%s=%d" % (k, v) for k, v in
                              sorted(cats.items(), key=lambda t: -t[1])))
        if fp[:8]:
            print("    example FPs (in): "
                  + "; ".join("(%.2f,%.2f) d=%.3f [%s]"
                              % (x / dpi, y / dpi, d / dpi,
                                 classify_fp(x, y, dpi, bulls, stroke_px, h, w))
                              for x, y, d in fp[:8]))
        if a.overlay_dir:
            os.makedirs(a.overlay_dir, exist_ok=True)
            s = 4
            ov = cv2.cvtColor(cv2.resize(rgb, (w // s, h // s)), cv2.COLOR_RGB2BGR)
            for x, y in truth:
                cv2.drawMarker(ov, (int(x / s), int(y / s)), (255, 200, 0),
                               cv2.MARKER_SQUARE, 14, 1)
            for x, y, d in fp:
                cv2.circle(ov, (int(x / s), int(y / s)), max(3, int(d / 2 / s)),
                           (0, 0, 255), 2)
            for x, y, d, _ in tp:
                cv2.circle(ov, (int(x / s), int(y / s)), max(3, int(d / 2 / s)),
                           (0, 200, 0), 2)
            cv2.imwrite(os.path.join(a.overlay_dir, "%s_%s.png"
                        % (os.path.splitext(fn)[0], name.split()[0])), ov)


if __name__ == "__main__":
    main()
