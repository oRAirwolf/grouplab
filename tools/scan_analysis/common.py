#!/usr/bin/env python3
"""common.py -- shared loading / ink-mask helpers for the scan analysis scripts."""
import os

import cv2
import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None

IMG_EXT = (".jpg", ".jpeg", ".png", ".tif", ".tiff")


def list_images(d):
    if os.path.isfile(d):
        return [d]
    return sorted(os.path.join(d, f) for f in os.listdir(d)
                  if f.lower().endswith(IMG_EXT))


def load_rgb(path):
    """Return (rgb uint8 HxWx3, dpi float). dpi falls back to 600 with a warning flag."""
    im = Image.open(path)
    dpi = im.info.get("dpi")
    if dpi:
        d = float(dpi[0])
        dpi_known = True
    else:
        d = None
        dpi_known = False
    rgb = np.asarray(im.convert("RGB"))
    im.close()
    return rgb, d, dpi_known


def paper_level(v):
    """Robust paper brightness = 90th percentile of V."""
    return float(np.percentile(v, 90))


def ink_mask(rgb):
    """Grayscale 'printedness' map: how far a pixel is from paper white, in 0..255.

    Combines darkness below the paper level with HSV saturation, so black,
    red, blue and grey printing all score high while paper scores ~0.
    Holes score high too (their dark annulus) -- that is intentional; the
    hole/artwork separation is a later stage's job.
    """
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    v = hsv[:, :, 2].astype(np.float32)
    s = hsv[:, :, 1].astype(np.float32)
    pv = paper_level(v)
    dark = np.clip(pv - v, 0, 255)
    return np.clip(np.maximum(dark, s), 0, 255).astype(np.uint8)


def downscale(img, src_dpi, dst_dpi):
    f = dst_dpi / src_dpi
    if abs(f - 1.0) < 1e-6:
        return img, 1.0
    h, w = img.shape[:2]
    out = cv2.resize(img, (max(1, int(round(w * f))), max(1, int(round(h * f)))),
                     interpolation=cv2.INTER_AREA)
    return out, f


def annulus_template(r, width, pad=1.35):
    """Matched filter: +1 on the ring band, -1 on flanking bands, zero mean."""
    R = int(np.ceil(r * pad)) + 2
    yy, xx = np.mgrid[-R:R + 1, -R:R + 1]
    d = np.sqrt(xx ** 2 + yy ** 2)
    t = np.zeros_like(d, dtype=np.float32)
    on = np.abs(d - r) <= width / 2.0
    off = (np.abs(d - r) > width / 2.0) & (np.abs(d - r) <= width * 1.6)
    t[on] = 1.0
    if off.sum():
        t[off] = -float(on.sum()) / off.sum()
    t -= t.mean()
    n = np.sqrt((t ** 2).sum())
    return t / (n if n else 1.0)


def nms_peaks(resp, min_dist, thresh):
    """Greedy non-maximum suppression over a response map."""
    r = resp.copy()
    pts = []
    k = int(min_dist)
    while True:
        idx = int(np.argmax(r))
        y, x = np.unravel_index(idx, r.shape)
        v = r[y, x]
        if v < thresh:
            break
        pts.append((x, y, float(v)))
        y0, y1 = max(0, y - k), min(r.shape[0], y + k + 1)
        x0, x1 = max(0, x - k), min(r.shape[1], x + k + 1)
        r[y0:y1, x0:x1] = -1e9
        if len(pts) > 400:
            break
    return pts


def cluster_1d(vals, tol):
    """Cluster sorted scalars with a gap threshold; return list of lists of index."""
    order = np.argsort(vals)
    groups = []
    cur = [order[0]]
    for i in order[1:]:
        if vals[i] - vals[cur[-1]] <= tol:
            cur.append(i)
        else:
            groups.append(cur)
            cur = [i]
    groups.append(cur)
    return groups


def fit_circle(pts):
    """Algebraic (Kasa) circle fit -> (cx, cy, r, rms)."""
    P = np.asarray(pts, dtype=np.float64)
    x, y = P[:, 0], P[:, 1]
    A = np.column_stack([x, y, np.ones(len(x))])
    b = x ** 2 + y ** 2
    sol, *_ = np.linalg.lstsq(A, b, rcond=None)
    cx, cy = sol[0] / 2, sol[1] / 2
    r = float(np.sqrt(max(sol[2] + cx ** 2 + cy ** 2, 0)))
    res = np.hypot(x - cx, y - cy) - r
    return float(cx), float(cy), r, float(np.sqrt((res ** 2).mean()))
