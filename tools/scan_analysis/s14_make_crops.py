#!/usr/bin/env python3
"""s14_make_crops.py -- cut the illustrative crops referenced by the report.

Each entry in CROPS is (output name, source file, centre x in inches,
centre y in inches, half-size in inches, caption). Coordinates are in page
inches so they are independent of the scan DPI.

Usage:
    python3 s14_make_crops.py <scan_dir> <out_dir> [--dpi-override FILE=DPI ...]
"""
import argparse
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb

CROPS = [
    # --- hole appearance
    ("hole_open_perforation.png", "300_nm_hand_load.jpg", 5.72, 5.74, 0.30,
     ".308 hole on bare paper. Measured: core mean V 216.8, core peak V 231.0, "
     "local paper V 254.5, rim minimum V 34.1, hull dia 0.2668 in. The rim, "
     "not the core, is the signal"),
    ("hole_context_wide.png", "300_nm_hand_load.jpg", 5.65, 5.65, 0.55,
     "the same hole in its bull, showing how far the printed ring is from the "
     "hole (ring radius 0.60 in, hole radius 0.13 in)"),
    ("hole_on_black_ring.png", "300_nm_hand_load.jpg", 4.42, 3.95, 0.42,
     "hole overlapping the printed black ring"),
    ("hole_double_overlap.png", "300_nm_hand_load.jpg", 7.90, 5.90, 0.55,
     "two overlapping perforations at bull 20 -- the one case where the "
     "one-shot-per-bull assumption is visibly violated"),
    ("hole_outside_ring.png", "300_nm_hand_load.jpg", 8.02, 4.42, 0.40,
     "shot outside its printed ring"),
    ("hole_on_red_bull.png", "338lmao.jpg", 5.69, 3.94, 0.45,
     ".338 hole on the solid red bullseye"),
    ("hole_on_blue_ring.png", "n568.jpg", 1.20, 0.55, 0.45,
     ".338 hole on the thin blue ring"),
    ("hole_300dpi_crumpled.png", "IMG_20250530_0001.jpg", 1.10, 1.25, 0.55,
     "hole in the 300 dpi crumpled scan"),
    ("hole_93dpi_messaging.png",
     "1748713494260-cf6994ff-96c5-4eda-8647-424356f59979_1.jpg", 1.10, 1.25, 0.55,
     "the same hole after a messaging-app round trip (93 dpi effective)"),
    # --- hand-drawn ink
    ("ink_X_over_hole.png", "retumbo.png", 1.20, 5.10, 0.62,
     "hand-drawn X drawn straight over a bullet hole (cell 16)"),
    ("ink_arrow_1.png", "retumbo.png", 2.75, 5.55, 0.75,
     "marker arrows pointing at holes (cells 17/22)"),
    ("ink_X_sighter.png", "retumbo.png", 1.22, 8.60, 0.62,
     "X over a hole in the sighter row"),
    ("ink_arrow_blue.png", "retumbo_0001.jpg", 1.90, 1.20, 0.85,
     "blue ballpoint arrows on the red target"),
    ("ink_arrow_over_grid.png", "6_5retumbo.jpg", 2.10, 4.20, 0.95,
     "long marker strokes crossing cell boundaries"),
    ("ink_marker_text.png", "300_nm_hand_load.jpg", 4.00, 8.85, 1.60,
     "thick marker caption; strokes are wider than the printed ring stroke"),
    ("ink_blue_caption.png", "338lmao.jpg", 2.50, 8.85, 1.60,
     "ballpoint caption across the page; loops read as circles"),
    # --- page condition
    ("page_tear_topright.png", "IMG_20250530_0001.jpg", 7.90, 0.80, 0.95,
     "torn top-right corner: the sheet edge and the scanner lid differ by "
     "about 3 grey levels"),
    ("page_tape_top.png", "IMG_20250530_0001.jpg", 4.20, 0.12, 1.20,
     "black tape across the top edge"),
    ("page_crumple.png", "IMG_20250530_0001.jpg", 3.80, 3.60, 1.30,
     "crumple ridges; the printed rule through them stays straight to "
     "0.003 in"),
    # --- artwork
    ("bull_target51.png", "300_nm_hand_load.jpg", 1.15, 1.15, 0.85,
     "OnTarget Target #51 bull: 1.199 in ring, 0.052 in stroke"),
    ("bull_target1_red.png", "338lmao.jpg", 7.18, 7.09, 0.60,
     "OnTarget Target #1 bull: solid red, 0.489 in outer"),
    ("bull_target18_blue.png", "n568.jpg", 7.16, 7.10, 0.75,
     "OnTarget Target #18 bull: thin blue ring, 0.949 in"),
    ("barcode_footer.png", "300_nm_hand_load.jpg", 6.60, 10.05, 1.10,
     "footer barcode: the dominant false-positive source for every naive "
     "detector"),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("scan_dir")
    ap.add_argument("out_dir")
    ap.add_argument("--dpi-override", nargs="*", default=[])
    a = ap.parse_args()
    ov = {}
    for kv in a.dpi_override:
        k, v = kv.split("=")
        ov[k] = float(v)
    os.makedirs(a.out_dir, exist_ok=True)
    cache = {}
    manifest = []
    for name, src, cx, cy, half, cap in CROPS:
        p = os.path.join(a.scan_dir, src)
        if not os.path.exists(p):
            print("MISSING", p)
            continue
        if src not in cache:
            cache[src] = load_rgb(p)
        rgb, dpi, _ = cache[src]
        dpi = ov.get(src, dpi) or 600.0
        h, w = rgb.shape[:2]
        x0 = max(0, int((cx - half) * dpi)); x1 = min(w, int((cx + half) * dpi))
        y0 = max(0, int((cy - half) * dpi)); y1 = min(h, int((cy + half) * dpi))
        if x1 <= x0 or y1 <= y0:
            print("OUT OF RANGE", name)
            continue
        crop = rgb[y0:y1, x0:x1]
        # cap the written size so the crops stay small but stay legible
        if max(crop.shape[:2]) > 700:
            f = 700.0 / max(crop.shape[:2])
            crop = cv2.resize(crop, (int(crop.shape[1] * f), int(crop.shape[0] * f)),
                              interpolation=cv2.INTER_AREA)
        elif max(crop.shape[:2]) < 240:
            f = 240.0 / max(crop.shape[:2])
            crop = cv2.resize(crop, (int(crop.shape[1] * f), int(crop.shape[0] * f)),
                              interpolation=cv2.INTER_NEAREST)
        cv2.imwrite(os.path.join(a.out_dir, name),
                    cv2.cvtColor(crop, cv2.COLOR_RGB2BGR))
        manifest.append((name, src, cx, cy, 2 * half, cap))
        print("%-32s <- %-30s centre (%.2f, %.2f) in, %.2f in wide"
              % (name, src, cx, cy, 2 * half))
    with open(os.path.join(a.out_dir, "MANIFEST.txt"), "w") as fh:
        fh.write("crop\tsource\tcentre_x_in\tcentre_y_in\twidth_in\tcaption\n")
        for m in manifest:
            fh.write("%s\t%s\t%.2f\t%.2f\t%.2f\t%s\n" % m)


if __name__ == "__main__":
    main()
