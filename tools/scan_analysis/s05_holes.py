#!/usr/bin/env python3
"""s05_holes.py -- find bullet holes and characterise their appearance.

Detection primitive
-------------------
Two facts about these scans do the work:

  * A bullet hole is a compact SOLID BLOB in the "darkness" map roughly one
    calibre across -- the perforation plus its ragged rim never reads as paper
    white -- while every piece of printed artwork is a THIN STROKE (measured
    ring stroke ~0.05 in, numerals ~0.02 in). A morphological OPEN with a disk
    wider than the widest stroke therefore erases the artwork and keeps holes.

  * Holes are ACHROMATIC. Red, pink and blue artwork all keep one channel near
    paper level, so "neutral darkness" Dn = paper - max(R,G,B) is near zero on
    coloured artwork and large on a hole. Black/grey artwork is not separated
    this way, which is exactly why the opening step is also needed.

So:  hole_map = open( Dn , disk of diameter > widest print stroke )
then threshold, fill, and keep blobs whose equivalent diameter is in
[dmin, dmax] inches and which are reasonably compact.

Characterisation
----------------
For each hole: bright-core peak/mean intensity and diameter, dark-annulus
min/mean intensity, radius and radial thickness, raggedness (sd of the
darkest-ring radius over angle), the angle-averaged radial intensity profile
in thousandths of an inch, and whether the hole overlaps printed artwork.

Usage:
    python3 s05_holes.py <scan_dir_or_file> [...] [--json out.json]
        [--profiles out.csv] [--crop-dir DIR] [--overlay-dir DIR]
        [--dpi N] [--open-in 0.032] [--dmin 0.15] [--dmax 0.60]
"""
import argparse
import csv
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import load_rgb, list_images, ink_mask

DMIN_IN = 0.15
DMAX_IN = 0.60
OPEN_IN = 0.032   # disk RADIUS in inches; must exceed half the widest print stroke


def neutral_darkness(rgb):
    """Dn = paper_level - max(R,G,B), clipped at 0.

    Near zero on any single-hue print (red/pink/blue keep one channel high) and
    on paper; large on black print and on the grey interior of a bullet hole.
    """
    mx = rgb.max(axis=2).astype(np.float32)
    paper = float(np.percentile(mx, 90))
    return np.clip(paper - mx, 0, 255).astype(np.uint8), paper


def angular_profile(img, cx, cy, rlist, nang=360):
    ang = np.linspace(0, 2 * np.pi, nang, endpoint=False)
    R, A = np.meshgrid(rlist, ang)
    xs = (cx + R * np.cos(A)).astype(np.float32)
    ys = (cy + R * np.sin(A)).astype(np.float32)
    return cv2.remap(img.astype(np.float32), xs, ys, cv2.INTER_LINEAR,
                     borderMode=cv2.BORDER_REPLICATE)


def characterise(v, ink, cx, cy, r_hole, dpi):
    """Measure one hole from the V channel. r_hole = blob equivalent radius."""
    rmax = max(r_hole * 2.6, r_hole + 0.08 * dpi)
    step = 0.5
    rl = np.arange(0.0, rmax, step)
    P = angular_profile(v, cx, cy, rl)
    prof = P.mean(axis=0)
    paper = float(np.nanmedian(prof[int(len(rl) * 0.88):]))

    # dark annulus: per-ray darkest sample beyond 0.3 r
    i0 = max(1, int(0.30 * r_hole / step))
    ray_i = np.argmin(P[:, i0:], axis=1) + i0
    ray_v = P[np.arange(P.shape[0]), ray_i]
    ann_r = rl[ray_i]
    ann_r_med = float(np.median(ann_r))
    ragged_sd = float(np.std(ann_r))
    ann_min = float(np.percentile(ray_v, 5))
    ann_mean = float(np.mean(ray_v))

    # bright core: inside 0.45 * annulus radius
    ci = max(2, int(0.45 * ann_r_med / step))
    core = P[:, :ci]
    core_mean = float(core.mean())
    core_peak = float(np.percentile(core, 98))
    core_sd = float(core.std())

    # core diameter: first radius where the ray falls below halfway
    # between the core level and the annulus minimum
    thr = (core_mean + ann_min) / 2.0
    cr = []
    for k in range(P.shape[0]):
        row = P[k]
        j = 0
        while j < len(row) and row[j] > thr:
            j += 1
        cr.append(rl[min(j, len(rl) - 1)])
    cr = np.array(cr)
    core_r_med = float(np.median(cr))

    # annulus radial thickness: run length below (paper+ann_min)/2 per ray
    hd = (paper + ann_min) / 2.0
    ws = []
    for k in range(P.shape[0]):
        below = np.where(P[k] < hd)[0]
        if len(below):
            ws.append((below[-1] - below[0] + 1) * step)
    ann_w = float(np.median(ws)) if ws else float("nan")

    return {
        "cx": round(float(cx), 1), "cy": round(float(cy), 1),
        "paper_V": round(paper, 1),
        "core_mean_V": round(core_mean, 1),
        "core_peak_V": round(core_peak, 1),
        "core_sd_V": round(core_sd, 1),
        "core_minus_paper_V": round(core_mean - paper, 1),
        "core_dia_px": round(2 * core_r_med, 1),
        "core_dia_in": round(2 * core_r_med / dpi, 4),
        "ann_min_V": round(ann_min, 1),
        "ann_mean_V": round(ann_mean, 1),
        "ann_dia_px": round(2 * ann_r_med, 1),
        "ann_dia_in": round(2 * ann_r_med / dpi, 4),
        "ann_ragged_sd_px": round(ragged_sd, 2),
        "ann_ragged_sd_in": round(ragged_sd / dpi, 4),
        "ann_ragged_cv": round(ragged_sd / ann_r_med, 4) if ann_r_med else None,
        "ann_thickness_px": round(ann_w, 1),
        "ann_thickness_in": round(ann_w / dpi, 4),
        "_r": rl, "_p": prof,
    }


def detect(path, dpi_override=None, open_in=OPEN_IN, dmin=DMIN_IN, dmax=DMAX_IN,
           dn_thresh=28, min_solidity=0.55):
    rgb, dpi, known = load_rgb(path)
    if dpi_override:
        dpi = dpi_override
    if dpi is None:
        dpi = 600.0
    v = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)[:, :, 2]
    ink = ink_mask(rgb)
    dn, paper_mx = neutral_darkness(rgb)
    rad = max(2, int(round(open_in * dpi)))
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * rad + 1, 2 * rad + 1))
    op = cv2.morphologyEx(dn, cv2.MORPH_OPEN, ker)
    bw = (op >= dn_thresh).astype(np.uint8)
    # Bridge the gaps in a ragged / broken rim. The rim -- not the core -- is
    # the reliable signal: an open perforation shows the scanner's white lid
    # and is indistinguishable from paper, so the core cannot be used.
    crad = max(3, int(round(0.055 * dpi)))
    ck = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * crad + 1, 2 * crad + 1))
    bw = cv2.morphologyEx(bw, cv2.MORPH_CLOSE, ck)
    cnts, _ = cv2.findContours(bw, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    filled = np.zeros_like(bw)
    cv2.drawContours(filled, cnts, -1, 1, -1)
    n, cc, stats, cents = cv2.connectedComponentsWithStats(filled, connectivity=8)
    amin = np.pi * (dmin * dpi / 2) ** 2
    amax = np.pi * (dmax * dpi / 2) ** 2
    holes, rejects = [], []
    for i in range(1, n):
        a_raw = int(stats[i, cv2.CC_STAT_AREA])
        x0 = stats[i, cv2.CC_STAT_LEFT]; y0 = stats[i, cv2.CC_STAT_TOP]
        bw_ = stats[i, cv2.CC_STAT_WIDTH]; bh_ = stats[i, cv2.CC_STAT_HEIGHT]
        sub = (cc[y0:y0 + bh_, x0:x0 + bw_] == i).astype(np.uint8)
        cn, _ = cv2.findContours(sub, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        hull = cv2.convexHull(np.vstack(cn))
        a = float(cv2.contourArea(hull))
        M = cv2.moments(hull)
        if M["m00"] <= 0:
            continue
        cx = x0 + M["m10"] / M["m00"]
        cy = y0 + M["m01"] / M["m00"]
        deq = 2 * np.sqrt(a / np.pi)
        solidity = a_raw / a if a else 0.0
        why = None
        if a < amin:
            why = "too_small(%.3fin)" % (deq / dpi)
        elif a > amax:
            why = "too_large(%.3fin)" % (deq / dpi)
        elif solidity < min_solidity:
            why = "not_compact(hull_solidity=%.2f)" % solidity
        elif max(bw_, bh_) / max(1.0, min(bw_, bh_)) > 2.2:
            why = "elongated(ar=%.2f)" % (max(bw_, bh_) / max(1.0, min(bw_, bh_)))
        if why:
            rejects.append({"cx": round(float(cx), 1), "cy": round(float(cy), 1),
                            "dia_in": round(deq / dpi, 4), "why": why})
            continue
        h = characterise(v, ink, cx, cy, deq / 2, dpi)
        h["hull_solidity"] = round(solidity, 3)
        # does the hole overlap printed artwork?  sample the ink map on a ring
        # 1.3-1.8 x the blob radius, where the hole's own rim has ended
        rp = angular_profile(ink, cx, cy, np.arange(deq / 2 * 1.35, deq / 2 * 1.9, 1.0))
        frac = float((rp.mean(axis=1) > 70).mean())
        h["on_ink"] = bool(frac > 0.12)
        h["on_ink_frac"] = round(frac, 3)
        h["blob_dia_px"] = round(float(deq), 1)
        h["blob_dia_in"] = round(float(deq) / dpi, 4)
        h["hull_area_px"] = int(a)
        holes.append(h)
    return rgb, v, ink, dn, op, dpi, holes, rejects


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("targets", nargs="+")
    ap.add_argument("--json")
    ap.add_argument("--profiles")
    ap.add_argument("--crop-dir")
    ap.add_argument("--overlay-dir")
    ap.add_argument("--dpi", type=float)
    ap.add_argument("--open-in", type=float, default=OPEN_IN)
    ap.add_argument("--dmin", type=float, default=DMIN_IN)
    ap.add_argument("--dmax", type=float, default=DMAX_IN)
    ap.add_argument("--dn-thresh", type=float, default=28)
    ap.add_argument("--crop-every", type=int, default=0)
    a = ap.parse_args()
    allrecs, prof_rows = [], []
    for t in a.targets:
        for f in list_images(t):
            rgb, v, ink, dn, op, dpi, holes, rejects = detect(
                f, a.dpi, a.open_in, a.dmin, a.dmax, a.dn_thresh)
            base = os.path.splitext(os.path.basename(f))[0]
            print("%-62s holes=%3d rejects=%3d dpi=%g on_ink=%d"
                  % (os.path.basename(f), len(holes), len(rejects), dpi,
                     sum(h["on_ink"] for h in holes)))
            for i, h in enumerate(holes):
                rl, pv = h.pop("_r"), h.pop("_p")
                h["file"] = os.path.basename(f)
                h["idx"] = i
                if a.profiles:
                    for r_, v_ in zip(rl, pv):
                        prof_rows.append([os.path.basename(f), i, h["on_ink"],
                                          round(r_ / dpi * 1000, 2), round(float(v_), 2)])
                if a.crop_dir and a.crop_every and i % a.crop_every == 0:
                    os.makedirs(a.crop_dir, exist_ok=True)
                    R = int(h["blob_dia_px"] * 1.5) + 8
                    x0 = max(0, int(h["cx"]) - R); x1 = min(rgb.shape[1], int(h["cx"]) + R)
                    y0 = max(0, int(h["cy"]) - R); y1 = min(rgb.shape[0], int(h["cy"]) + R)
                    cv2.imwrite(os.path.join(a.crop_dir, "%s_hole%02d.png" % (base, i)),
                                cv2.cvtColor(rgb[y0:y1, x0:x1], cv2.COLOR_RGB2BGR))
            allrecs.append({"file": os.path.basename(f), "dpi": dpi,
                            "n_holes": len(holes), "holes": holes,
                            "rejects": rejects})
            if a.overlay_dir:
                os.makedirs(a.overlay_dir, exist_ok=True)
                s = 4
                ov = cv2.cvtColor(cv2.resize(rgb, (rgb.shape[1] // s, rgb.shape[0] // s)),
                                  cv2.COLOR_RGB2BGR)
                for h in holes:
                    cv2.circle(ov, (int(h["cx"] / s), int(h["cy"] / s)),
                               max(3, int(h["blob_dia_px"] / 2 / s)),
                               (0, 0, 255) if h["on_ink"] else (0, 190, 0), 2)
                for r in rejects:
                    if not r["why"].startswith("too_small"):
                        cv2.drawMarker(ov, (int(r["cx"] / s), int(r["cy"] / s)),
                                       (255, 160, 0), cv2.MARKER_TILTED_CROSS, 12, 2)
                cv2.imwrite(os.path.join(a.overlay_dir, base + "_holes.png"), ov)
    if a.json:
        with open(a.json, "w") as fh:
            json.dump(allrecs, fh, indent=1, sort_keys=True)
    if a.profiles:
        with open(a.profiles, "w", newline="") as fh:
            w = csv.writer(fh)
            w.writerow(["file", "hole_idx", "on_ink", "r_mil", "mean_V"])
            w.writerows(prof_rows)


if __name__ == "__main__":
    main()
