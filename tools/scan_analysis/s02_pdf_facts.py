#!/usr/bin/env python3
"""s02_pdf_facts.py -- page boxes and embedded-image facts for PDFs.

For each PDF: page MediaBox/CropBox in points/inches/mm, and for every XObject
image on the page its pixel size, filter, colour space, bits, the placement
matrix (hence effective DPI), and a sha256 of the raw compressed stream so the
bitmap can be compared against a sibling .jpg.

Usage:
    python3 s02_pdf_facts.py <pdf_or_dir> [...] [--dump-images OUTDIR]
"""
import argparse
import hashlib
import os
import sys

from pypdf import PdfReader


def page_images(page):
    out = []
    res = page.get("/Resources")
    if res is None:
        return out
    res = res.get_object()
    xo = res.get("/XObject")
    if xo is None:
        return out
    xo = xo.get_object()
    for name, ref in xo.items():
        obj = ref.get_object()
        if obj.get("/Subtype") != "/Image":
            continue
        raw = obj.get_data() if False else None
        try:
            data = obj._data  # raw compressed stream bytes
        except Exception:
            data = b""
        cs = obj.get("/ColorSpace")
        out.append({
            "name": str(name),
            "width": int(obj.get("/Width", 0)),
            "height": int(obj.get("/Height", 0)),
            "bpc": obj.get("/BitsPerComponent"),
            "filter": str(obj.get("/Filter")),
            "colorspace": str(cs)[:80],
            "stream_bytes": len(data),
            "stream_sha256_16": hashlib.sha256(data).hexdigest()[:16] if data else None,
            "_obj": obj,
        })
    return out


def analyse(path, dump=None):
    print("=" * 70)
    print(path, os.path.getsize(path), "bytes")
    r = PdfReader(path)
    print("pages:", len(r.pages), " producer:", r.metadata.get("/Producer") if r.metadata else None,
          " creator:", r.metadata.get("/Creator") if r.metadata else None)
    for pi, page in enumerate(r.pages):
        mb = page.mediabox
        w_pt = float(mb.width)
        h_pt = float(mb.height)
        print("  page %d MediaBox = %.2f x %.2f pt = %.4f x %.4f in = %.2f x %.2f mm"
              % (pi, w_pt, h_pt, w_pt / 72, h_pt / 72, w_pt / 72 * 25.4, h_pt / 72 * 25.4))
        try:
            cb = page.cropbox
            print("       CropBox  = %.2f x %.2f pt" % (float(cb.width), float(cb.height)))
        except Exception:
            pass
        print("       rotation =", page.get("/Rotate", 0))
        for im in page_images(page):
            eff_x = im["width"] / (w_pt / 72) if w_pt else 0
            eff_y = im["height"] / (h_pt / 72) if h_pt else 0
            print("       image %s %dx%d bpc=%s filter=%s cs=%s stream=%d B sha=%s"
                  % (im["name"], im["width"], im["height"], im["bpc"], im["filter"],
                     im["colorspace"], im["stream_bytes"], im["stream_sha256_16"]))
            print("             implied DPI if image fills page: %.1f x %.1f" % (eff_x, eff_y))
            if dump:
                os.makedirs(dump, exist_ok=True)
                base = os.path.splitext(os.path.basename(path))[0]
                ext = ".jpg" if "DCTDecode" in im["filter"] else ".bin"
                out = os.path.join(dump, "%s_p%d_%s%s" % (base, pi, im["name"].strip("/"), ext))
                with open(out, "wb") as fh:
                    fh.write(im["_obj"]._data)
                print("             dumped ->", out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("targets", nargs="+")
    ap.add_argument("--dump-images")
    a = ap.parse_args()
    for t in a.targets:
        if os.path.isdir(t):
            for f in sorted(os.listdir(t)):
                if f.lower().endswith(".pdf"):
                    analyse(os.path.join(t, f), a.dump_images)
        else:
            analyse(t, a.dump_images)


if __name__ == "__main__":
    main()
