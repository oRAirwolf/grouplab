#!/usr/bin/env python3
"""s01_file_facts.py -- image/file-level facts for a directory of scans.

Reports pixel dimensions, DPI tag (EXIF / JFIF / PNG pHYs), implied physical
size, colour mode, JPEG quantisation-table quality estimate, and file size.

Usage:
    python3 s01_file_facts.py <scan_dir> [more_dirs...] [--json out.json]
"""
import argparse
import hashlib
import json
import os
import sys

from PIL import Image, ExifTags

Image.MAX_IMAGE_PIXELS = None

# Annex K standard luminance quantisation table (JPEG spec, quality 50 base)
STD_LUM_Q = [
    16, 11, 10, 16, 24, 40, 51, 61,
    12, 12, 14, 19, 26, 58, 60, 55,
    14, 13, 16, 24, 40, 57, 69, 56,
    14, 17, 22, 29, 51, 87, 80, 62,
    18, 22, 37, 56, 68, 109, 103, 77,
    24, 35, 55, 64, 81, 104, 113, 92,
    49, 64, 78, 87, 103, 121, 120, 101,
    72, 92, 95, 98, 112, 100, 103, 99,
]


def estimate_jpeg_quality(im):
    """Estimate the IJG quality setting from the luminance quantisation table.

    For each coefficient, invert the IJG scaling formula
        q = floor((std * S + 50) / 100), S = 5000/Q (Q<50) or 200-2Q (Q>=50)
    and take the median of the recovered Q values. Returns (quality, method).
    """
    qt = getattr(im, "quantization", None)
    if not qt:
        return None, "no quantization table"
    lum = list(qt.get(0, []))
    if len(lum) != 64:
        return None, "unexpected table length %d" % len(lum)
    ests = []
    for std, q in zip(STD_LUM_Q, lum):
        if q <= 0:
            continue
        # S such that round-ish (std*S+50)/100 == q  ->  S ~ (100*q - 50)/std
        S = (100.0 * q - 50.0) / std
        if S <= 0:
            continue
        if S >= 100:  # Q < 50
            Q = 5000.0 / S
        else:
            Q = (200.0 - S) / 2.0
        if 1 <= Q <= 100:
            ests.append(Q)
    if not ests:
        return None, "no usable coefficients"
    ests.sort()
    med = ests[len(ests) // 2]
    return round(med, 1), "median over %d coeffs, spread %.1f-%.1f" % (
        len(ests), ests[0], ests[-1])


def dpi_info(im):
    """Return (dpi_x, dpi_y, source) or (None, None, 'absent')."""
    # PNG pHYs
    if im.format == "PNG":
        phys = im.info.get("dpi")
        if phys:
            return float(phys[0]), float(phys[1]), "PNG pHYs"
        return None, None, "absent (no pHYs)"
    # JFIF density
    src = []
    jfif_unit = im.info.get("jfif_unit")
    jfif_dx = im.info.get("jfif_density")
    if jfif_dx:
        src.append("JFIF unit=%s density=%s" % (jfif_unit, jfif_dx))
    exif_dpi = None
    try:
        ex = im.getexif()
        xr = ex.get(0x011A)
        yr = ex.get(0x011B)
        unit = ex.get(0x0128)
        if xr:
            exif_dpi = (float(xr), float(yr) if yr else float(xr))
            src.append("EXIF XResolution=%s unit=%s" % (xr, unit))
    except Exception:
        pass
    dpi = im.info.get("dpi")
    if dpi:
        return float(dpi[0]), float(dpi[1]), "; ".join(src) or "PIL info['dpi']"
    if exif_dpi:
        return exif_dpi[0], exif_dpi[1], "; ".join(src)
    return None, None, "absent"


def analyse(path):
    st = os.stat(path)
    with open(path, "rb") as fh:
        digest = hashlib.sha256(fh.read()).hexdigest()[:16]
    rec = {
        "file": os.path.basename(path),
        "path": path,
        "bytes": st.st_size,
        "sha256_16": digest,
    }
    try:
        im = Image.open(path)
    except Exception as e:
        rec["error"] = str(e)
        return rec
    rec["format"] = im.format
    rec["mode"] = im.mode
    rec["width_px"], rec["height_px"] = im.size
    dx, dy, src = dpi_info(im)
    rec["dpi_x"], rec["dpi_y"], rec["dpi_source"] = dx, dy, src
    if dx:
        rec["width_in"] = round(im.size[0] / dx, 4)
        rec["height_in"] = round(im.size[1] / dy, 4)
        rec["width_mm"] = round(im.size[0] / dx * 25.4, 2)
        rec["height_mm"] = round(im.size[1] / dy * 25.4, 2)
    if im.format == "JPEG":
        q, how = estimate_jpeg_quality(im)
        rec["jpeg_quality_est"] = q
        rec["jpeg_quality_method"] = how
        rec["jpeg_subsampling"] = _subsampling(im)
        rec["jpeg_progressive"] = bool(im.info.get("progressive"))
    if im.format == "PNG":
        rec["png_bits"] = im.info.get("bits")
        rec["png_interlace"] = im.info.get("interlace")
    rec["bytes_per_px"] = round(st.st_size / (im.size[0] * im.size[1]), 4)
    # software / scanner tags
    try:
        ex = im.getexif()
        tags = {}
        for k, v in ex.items():
            name = ExifTags.TAGS.get(k, str(k))
            if isinstance(v, bytes):
                v = v[:40]
            tags[name] = str(v)[:120]
        if tags:
            rec["exif"] = tags
    except Exception:
        pass
    im.close()
    return rec


def _subsampling(im):
    try:
        from PIL import JpegImagePlugin
        s = JpegImagePlugin.get_sampling(im)
        return {0: "4:4:4", 1: "4:2:2", 2: "4:2:0"}.get(s, "raw=%s" % s)
    except Exception:
        return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dirs", nargs="+")
    ap.add_argument("--json")
    a = ap.parse_args()
    out = []
    for d in a.dirs:
        if os.path.isfile(d):
            files = [d]
        else:
            files = sorted(
                os.path.join(d, f) for f in os.listdir(d)
                if f.lower().endswith((".jpg", ".jpeg", ".png", ".tif", ".tiff"))
            )
        for f in files:
            out.append(analyse(f))
    for r in out:
        print(json.dumps(r, indent=1, sort_keys=True))
    if a.json:
        with open(a.json, "w") as fh:
            json.dump(out, fh, indent=1, sort_keys=True)
        print("wrote", a.json, file=sys.stderr)


if __name__ == "__main__":
    main()
