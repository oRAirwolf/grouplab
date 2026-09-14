#!/usr/bin/env python3
"""
How far from flat is a real photographed target?

A flat sheet photographed through a rectilinear lens maps printed straight lines
to straight lines.  Any bend in a line that was printed straight is therefore
lens distortion plus paper deformation, and nothing else.  Measuring the bend
gives an UPPER BOUND on the deformation without needing fiducials, without
knowing the target's geometry, and without registering anything.

Both target families in this collection carry a printed rectangular grid: a fine
orange grid on the white sight-in sheets, and a yellow grid on the black splatter
sheets.  Both are strongly saturated against their ground, so one mask finds
either.

Method, per image:
  1. Mask saturated ink.
  2. Hough for the dominant straight lines.
  3. Walk each line and take the mask's centroid perpendicular to it in a narrow
     window, which recovers where the printed line actually goes.
  4. Fit a straight line to those centroids by total least squares.
  5. Report the deviation, in pixels and as a fraction of the frame.

Reported as a fraction of frame width so that frames at different distances and
resolutions compare.  On a sheet filling the frame, 1 percent of frame width is
roughly 0.1 in on a letter sheet.
"""
import cv2, numpy as np, glob, os, sys, json

MAXDIM   = 1600     # working resolution
MINFRAC  = 0.35     # a line must span this fraction of the frame to count
STEP     = 6        # sample spacing along a line, working px
HALFWIN  = 7        # perpendicular half window for the centroid


def ink_mask(bgr):
    """Saturated printed ink, either colour family."""
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    s, v = hsv[:, :, 1], hsv[:, :, 2]
    m = ((s > 70) & (v > 45)).astype(np.uint8) * 255
    return cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))


def line_points(mask, rho, theta):
    """Walk the line and return where the ink actually is, perpendicular to it."""
    h, w = mask.shape
    ct, st = np.cos(theta), np.sin(theta)
    d = np.array([-st, ct])                  # along the line
    n = np.array([ct, st])                   # across it
    p0 = np.array([ct * rho, st * rho])
    span = int(np.hypot(h, w))
    pts = []
    for t in range(-span, span, STEP):
        c = p0 + d * t
        acc, wsum = np.zeros(2), 0.0
        for k in range(-HALFWIN, HALFWIN + 1):
            q = c + n * k
            x, y = int(round(q[0])), int(round(q[1]))
            if 0 <= x < w and 0 <= y < h and mask[y, x]:
                acc += q
                wsum += 1
        # A clean crossing is a few pixels wide.  A blob of ink is not a line.
        if 0 < wsum <= (2 * HALFWIN + 1) * 0.7:
            pts.append(acc / wsum)
    return np.array(pts)


def straightness(pts):
    """Total-least-squares line fit; returns rms and max perpendicular deviation."""
    c = pts.mean(axis=0)
    u, s, vt = np.linalg.svd(pts - c, full_matrices=False)
    n = vt[1]                                 # normal to the best-fit line
    dev = (pts - c) @ n
    return float(np.sqrt((dev ** 2).mean())), float(np.abs(dev).max()), dev


def measure(path):
    img = cv2.imread(path)
    if img is None:
        return None
    H0, W0 = img.shape[:2]
    sc = MAXDIM / max(H0, W0)
    img = cv2.resize(img, (int(W0 * sc), int(H0 * sc)), interpolation=cv2.INTER_AREA)
    h, w = img.shape[:2]
    mask = ink_mask(img)

    edges = cv2.Canny(mask, 50, 150)
    lines = cv2.HoughLines(edges, 1, np.pi / 360, threshold=int(min(h, w) * 0.22))
    if lines is None:
        return dict(file=os.path.basename(path), lines=0)

    # Keep one line per (angle, offset) neighbourhood: Hough gives two edges per
    # printed stroke, and many near-duplicates.
    kept, rows = [], []
    for rho, theta in lines[:, 0]:
        if any(abs(theta - t) < np.deg2rad(2.5) and abs(rho - r) < 14
               for r, t in kept):
            continue
        pts = line_points(mask, rho, theta)
        if len(pts) < 25:
            continue
        span = np.hypot(*(pts.max(axis=0) - pts.min(axis=0)))
        if span < MINFRAC * max(h, w):
            continue
        kept.append((rho, theta))
        rms, mx, dev = straightness(pts)
        # Sign pattern says whether the line bows one way (a bend) or snakes
        # (noise / a broken fit).
        half = len(dev) // 2
        bow = float(abs(dev[half // 2:half + half // 2].mean()))
        rows.append(dict(theta_deg=round(float(np.rad2deg(theta)), 1),
                         n=len(pts), span_px=round(float(span), 1),
                         rms_px=round(rms, 2), max_px=round(mx, 2),
                         bow_px=round(bow, 2),
                         max_frac=round(mx / max(h, w), 4)))
        if len(rows) >= 14:
            break

    if not rows:
        return dict(file=os.path.basename(path), lines=0)

    mx = np.array([r["max_px"] for r in rows])
    fr = np.array([r["max_frac"] for r in rows])
    return dict(file=os.path.basename(path), lines=len(rows),
                median_max_px=round(float(np.median(mx)), 2),
                p90_max_px=round(float(np.percentile(mx, 90)), 2),
                median_max_frac=round(float(np.median(fr)), 4),
                worst_max_frac=round(float(fr.max()), 4),
                rows=rows)


if __name__ == "__main__":
    out = []
    for p in sorted(glob.glob(sys.argv[1] if len(sys.argv) > 1 else "*.jpg")):
        r = measure(p)
        if r:
            out.append(r)
            if r["lines"]:
                print("%-34s lines=%-3d median max dev %5.2f px  %5.2f%% of frame"
                      "   worst %5.2f%%"
                      % (r["file"][:33], r["lines"], r["median_max_px"],
                         100 * r["median_max_frac"], 100 * r["worst_max_frac"]))
            else:
                print("%-34s no usable straight lines" % r["file"][:33])
    json.dump(out, open("/tmp/straightness.json", "w"), indent=1)
