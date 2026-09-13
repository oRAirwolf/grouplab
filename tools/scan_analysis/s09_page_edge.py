#!/usr/bin/env python3
"""s09_page_edge.py -- locate the physical sheet edge and measure how far it
departs from a rectangle.

On these scans the paper and the scanner lid are within a few grey levels of
each other, so plain thresholding does not find the sheet. What does separate
them is TEXTURE: paper carries visible grain and crumple shading, the lid is
smooth. The script therefore segments on local standard deviation, reports the
intensity contrast between the two so the reader can see how weak it is, then
traces the boundary and measures its departure from the best-fit rectangle.

It also reports, for each of the four image borders, whether the sheet edge is
inside the frame at all -- on several of these scans the scanner cropped inside
the page, so there is no page edge to register against.

Usage:
    python3 s09_page_edge.py <image> [...] [--dpi N] [--out-dir DIR]
"""
import argparse
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb, list_images


def local_sd(g, k):
    g = g.astype(np.float32)
    m = cv2.boxFilter(g, -1, (k, k))
    m2 = cv2.boxFilter(g * g, -1, (k, k))
    return np.sqrt(np.maximum(m2 - m * m, 0))


def analyse(path, dpi_override=None, out_dir=None):
    rgb, dpi, _ = load_rgb(path)
    if dpi_override:
        dpi = dpi_override
    dpi = dpi or 600.0
    g = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
    s = max(1, int(round(dpi / 150.0)))
    sm = cv2.resize(g, (g.shape[1] // s, g.shape[0] // s), interpolation=cv2.INTER_AREA)
    d = dpi / s
    h, w = sm.shape
    sd = local_sd(sm, max(3, int(0.02 * d) | 1))
    print("=" * 78)
    print("%s  dpi=%g  working %dx%d px @ %.0f dpi" % (os.path.basename(path), dpi, w, h, d))
    # texture threshold: paper grain vs smooth lid
    thr = float(np.percentile(sd, 35))
    mask = (sd > thr).astype(np.uint8)
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (int(0.06 * d) | 1,) * 2)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, ker)
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, ker)
    n, cc, st, ce = cv2.connectedComponentsWithStats(mask, 8)
    if n < 2:
        print("  no textured region found")
        return
    i = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    page = (cc == i).astype(np.uint8)
    frac = page.mean()
    print("  textured (paper) region covers %.1f%% of the frame" % (100 * frac))
    # intensity contrast paper vs non-paper
    inside = g[cv2.resize(page, (g.shape[1], g.shape[0]), interpolation=cv2.INTER_NEAREST) > 0]
    outside = g[cv2.resize(1 - page, (g.shape[1], g.shape[0]), interpolation=cv2.INTER_NEAREST) > 0]
    if len(outside) > 100:
        print("  grey level: paper median %.1f (sd %.1f) | non-paper median %.1f (sd %.1f)"
              "  -> contrast %.1f levels"
              % (np.median(inside), inside.std(), np.median(outside), outside.std(),
                 abs(float(np.median(inside)) - float(np.median(outside)))))
    # which image borders does the page touch?
    touch = {"left": bool(page[:, 0].mean() > 0.5), "right": bool(page[:, -1].mean() > 0.5),
             "top": bool(page[0, :].mean() > 0.5), "bottom": bool(page[-1, :].mean() > 0.5)}
    print("  page runs off the frame at: %s"
          % (", ".join(k for k, v in touch.items() if v) or "no border (whole sheet visible)"))
    cnts, _ = cv2.findContours(page, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    c = max(cnts, key=cv2.contourArea).reshape(-1, 2).astype(np.float64)
    # drop outline points that lie on the image border -- those are crop, not edge
    m = ((c[:, 0] > 2) & (c[:, 0] < w - 3) & (c[:, 1] > 2) & (c[:, 1] < h - 3))
    real = c[m]
    print("  outline points: %d total, %d are real sheet edge (%.0f%%)"
          % (len(c), int(m.sum()), 100 * m.mean()))
    if m.sum() < 50:
        print("  -> too little real edge to fit a rectangle")
        return
    rect = cv2.minAreaRect(c.astype(np.float32))
    box = cv2.boxPoints(rect).astype(np.float64)
    devs = []
    for p in real:
        best = 1e9
        for k in range(4):
            a, b = box[k], box[(k + 1) % 4]
            ab = b - a
            t = np.clip(np.dot(p - a, ab) / max(np.dot(ab, ab), 1e-9), 0, 1)
            best = min(best, float(np.linalg.norm(p - (a + t * ab))))
        devs.append(best)
    devs = np.array(devs) / d
    print("  min-area rect %.3f x %.3f in, angle %.2f deg"
          % (max(rect[1]) / d, min(rect[1]) / d, rect[2]))
    print("  REAL-EDGE departure from that rectangle: max %.4f in, p95 %.4f in, "
          "rms %.4f in, median %.4f in"
          % (devs.max(), np.percentile(devs, 95), np.sqrt((devs ** 2).mean()),
             np.median(devs)))
    if out_dir:
        os.makedirs(out_dir, exist_ok=True)
        ov = cv2.cvtColor(sm, cv2.COLOR_GRAY2BGR)
        cv2.drawContours(ov, [c.astype(np.int32)], -1, (0, 0, 255), 1)
        cv2.drawContours(ov, [box.astype(np.int32)], -1, (0, 200, 0), 1)
        for p in real[::7]:
            cv2.circle(ov, tuple(p.astype(int)), 1, (255, 0, 255), -1)
        cv2.imwrite(os.path.join(out_dir,
                    os.path.splitext(os.path.basename(path))[0] + "_pageedge.png"), ov)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("images", nargs="+")
    ap.add_argument("--dpi", type=float)
    ap.add_argument("--out-dir")
    a = ap.parse_args()
    for t in a.images:
        for f in list_images(t):
            analyse(f, a.dpi, a.out_dir)


if __name__ == "__main__":
    main()
