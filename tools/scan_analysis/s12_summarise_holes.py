#!/usr/bin/env python3
"""s12_summarise_holes.py -- roll the per-hole measurements up into the tables
that go in the report.

Emits
  * per-file summary of hole diameter, core intensity, annulus intensity,
    annulus thickness and raggedness
  * pooled statistics split by on-artwork vs bare paper
  * hole diameter vs nominal bullet diameter, per calibre group
  * the angle-averaged radial intensity profile, pooled, in thousandths of an
    inch, from the profile CSV written by s05

Usage:
    python3 s12_summarise_holes.py --holes holes.json [--profiles p.csv]
        [--calibre '300_nm*=0.308,28_6_5*=0.264,n568*=0.338'] [--min-dia 0.15]
"""
import argparse
import csv
import fnmatch
import json
from collections import defaultdict

import numpy as np

FIELDS = [("blob_dia_in", "hull dia"), ("core_dia_in", "core dia"),
          ("ann_dia_in", "ann dia"), ("core_mean_V", "core V"),
          ("core_peak_V", "coreP V"), ("paper_V", "paper V"),
          ("ann_min_V", "annmin V"), ("ann_mean_V", "annmean"),
          ("ann_thickness_in", "ann thk"), ("ann_ragged_sd_in", "ragged sd"),
          ("ann_ragged_cv", "ragged cv")]


def st(vals):
    v = np.array([x for x in vals if x is not None], float)
    if not len(v):
        return None
    return v.mean(), v.std(ddof=1) if len(v) > 1 else 0.0, v.min(), v.max(), len(v)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--holes", required=True)
    ap.add_argument("--profiles")
    ap.add_argument("--calibre", default="300_nm*=0.308,28_6_5*=0.264,"
                                         "6_5retumbo*=0.264,n568*=0.338,338lmao*=0.338")
    ap.add_argument("--min-dia", type=float, default=0.15)
    ap.add_argument("--max-dia", type=float, default=0.55)
    a = ap.parse_args()
    cal = {}
    for kv in a.calibre.split(","):
        if "=" in kv:
            k, v = kv.split("=")
            cal[k.strip()] = float(v)
    D = json.load(open(a.holes))
    allh = []
    print("PER-FILE SUMMARY (mean +/- sd over accepted holes)")
    print("%-58s %4s %14s %14s %13s %13s %12s"
          % ("file", "n", "hull dia (in)", "core dia (in)", "core V", "ann min V",
             "ragged sd(in)"))
    for rec in sorted(D, key=lambda r: r["file"]):
        hs = [h for h in rec["holes"]
              if a.min_dia <= h["blob_dia_in"] <= a.max_dia]
        if not hs:
            print("%-58s %4d  (none in size band)" % (rec["file"], 0))
            continue
        for h in hs:
            h["_file"] = rec["file"]
        allh += hs
        def f(k):
            s = st([h[k] for h in hs])
            return "%6.4f+-%.4f" % (s[0], s[1]) if s else "-"
        def fv(k):
            s = st([h[k] for h in hs])
            return "%5.1f+-%4.1f" % (s[0], s[1]) if s else "-"
        print("%-58s %4d %14s %14s %13s %13s %12s"
              % (rec["file"], len(hs), f("blob_dia_in"), f("core_dia_in"),
                 fv("core_mean_V"), fv("ann_min_V"), f("ann_ragged_sd_in")))

    print("\nPOOLED, n=%d holes" % len(allh))
    for grp, sel in (("all", lambda h: True),
                     ("on bare paper", lambda h: not h["on_ink"]),
                     ("on printed ink", lambda h: h["on_ink"])):
        hs = [h for h in allh if sel(h)]
        if not hs:
            continue
        print("  %-16s n=%d" % (grp, len(hs)))
        for k, lbl in FIELDS:
            s = st([h.get(k) for h in hs])
            if s:
                print("     %-11s mean %8.4f  sd %8.4f  min %8.4f  max %8.4f"
                      % (lbl, s[0], s[1], s[2], s[3]))

    print("\nHOLE DIAMETER vs NOMINAL BULLET DIAMETER")
    print("%-24s %-10s %4s %10s %10s %10s %10s"
          % ("calibre group", "nominal", "n", "mean dia", "sd", "mean-nom", "ratio"))
    byc = defaultdict(list)
    for h in allh:
        for pat, nom in cal.items():
            if fnmatch.fnmatch(h["_file"], pat):
                byc[(pat, nom)].append(h["blob_dia_in"])
                break
    for (pat, nom), v in sorted(byc.items()):
        v = np.array(v)
        print("%-24s %-10.3f %4d %10.4f %10.4f %+10.4f %10.3f"
              % (pat, nom, len(v), v.mean(), v.std(ddof=1), v.mean() - nom,
                 v.mean() / nom))

    if a.profiles:
        rows = list(csv.DictReader(open(a.profiles)))
        buckets = defaultdict(list)
        bucket_ink = defaultdict(list)
        for r in rows:
            rm = float(r["r_mil"])
            b = int(rm // 5) * 5
            buckets[b].append(float(r["mean_V"]))
            if r["on_ink"] == "True":
                bucket_ink[("ink", b)].append(float(r["mean_V"]))
            else:
                bucket_ink[("paper", b)].append(float(r["mean_V"]))
        print("\nANGLE-AVERAGED RADIAL INTENSITY PROFILE (pooled over all holes)")
        print("%10s %10s %8s %10s %10s %10s"
              % ("r (mil)", "r (in)", "n", "mean V", "V on paper", "V on ink"))
        for b in sorted(buckets)[:60]:
            v = np.array(buckets[b])
            vp = bucket_ink.get(("paper", b), [])
            vi = bucket_ink.get(("ink", b), [])
            print("%10d %10.4f %8d %10.1f %10s %10s"
                  % (b, b / 1000.0, len(v), v.mean(),
                     "%.1f" % np.mean(vp) if len(vp) else "-",
                     "%.1f" % np.mean(vi) if len(vi) else "-"))


if __name__ == "__main__":
    main()
