#!/usr/bin/env python3
"""s10_cell_assignment.py -- do shots stay in their own cell, and would
nearest-bull assignment get them right?

These are one-shot-per-bull load-development targets, so the true assignment is
a ONE-TO-ONE matching between holes and bulls. That gives a defensible ground
truth without asking the shooter: the globally optimal one-to-one matching
(minimum total hole-to-bull distance, Hungarian algorithm) is the assignment a
careful human would make. Greedy nearest-bull is what a naive detector does.
Where the two disagree, nearest-bull is wrong.

For every hole the script reports the nearest bull, the matched bull, both
distances, and the margin to the second-nearest bull. It also reports how far
each hole is from its own cell boundary (cells are the 1.5 in Voronoi squares
of the bull lattice).

Detection false positives (handwriting, barcode) are excluded by a distance
gate: a hole further than --max-dist inches from any bull is not a shot at a
bull and is listed separately.

Usage:
    python3 s10_cell_assignment.py --grid g.json --holes h.json
        [--files a.jpg ...] [--max-dist 1.10] [--scoring-rows 5]
"""
import argparse
import json

import numpy as np

try:
    from scipy.optimize import linear_sum_assignment
    HAVE_SCIPY = True
except ImportError:
    HAVE_SCIPY = False


def label_bulls(C, rmed):
    """Row-major labels r<r>c<c>/#n for a lattice of bull centres."""
    idx = np.lexsort((C[:, 0], np.round(C[:, 1] / (1.5 * rmed))))
    C = C[idx]
    rows, cur = [], [0]
    for i in range(1, len(C)):
        if C[i, 1] - C[cur[-1], 1] > rmed:
            rows.append(cur); cur = [i]
        else:
            cur.append(i)
    rows.append(cur)
    labels = [None] * len(C)
    rowof = [0] * len(C)
    n = 0
    for ri, r in enumerate(rows):
        r = sorted(r, key=lambda k: C[k, 0])
        for ci, k in enumerate(r):
            n += 1
            labels[k] = "r%dc%d/#%d" % (ri + 1, ci + 1, n)
            rowof[k] = ri
    return C, labels, np.array(rowof), len(rows)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--grid", required=True)
    ap.add_argument("--holes", required=True)
    ap.add_argument("--files", nargs="*")
    ap.add_argument("--radius-filter", type=float, default=0.10)
    ap.add_argument("--max-dist", type=float, default=1.10,
                    help="inches; holes further than this from every bull are "
                         "treated as detector false positives, not shots")
    ap.add_argument("--scoring-rows", type=int,
                    help="use only the first N lattice rows (excludes sighters)")
    a = ap.parse_args()
    G = {r["file"]: r for r in json.load(open(a.grid))}
    H = {r["file"]: r for r in json.load(open(a.holes))}
    for fn in (a.files or sorted(H)):
        g, hh = G.get(fn), H.get(fn)
        if not g or not hh or "centres_px" not in g:
            continue
        dpi = g["dpi"]
        C = np.array(g["centres_px"], float)
        rmed = float(np.median(C[:, 2]))
        C = C[np.abs(C[:, 2] - rmed) <= a.radius_filter * rmed]
        C, labels, rowof, nrows = label_bulls(C, rmed)
        if a.scoring_rows:
            m = rowof < a.scoring_rows
            C = C[m]
            labels = [l for l, k in zip(labels, m) if k]
        P = C[:, :2]
        def _pitch(v):
            v = np.sort(v)
            g = [[v[0]]]
            for x in v[1:]:
                (g[-1] if x - g[-1][-1] < 0.4 * rmed else g.append([x]) or g[-1]).append(x)
            c = np.array([np.mean(x) for x in g])
            d = np.diff(c)
            return float(np.median(d)) if len(d) else 0.0
        pitch = float(np.median([_pitch(P[:, 0]), _pitch(P[:, 1])]))
        allh = np.array([[h["cx"], h["cy"]] for h in hh["holes"]], float)
        if len(allh) == 0:
            continue
        D = np.hypot(allh[:, None, 0] - P[None, :, 0],
                     allh[:, None, 1] - P[None, :, 1]) / dpi
        dmin = D.min(axis=1)
        keep = dmin <= a.max_dist
        print("=" * 100)
        print("%s  dpi=%g  bulls=%d  detections=%d  shots kept=%d  "
              "rejected as non-shot (>%.2f in from any bull)=%d"
              % (fn, dpi, len(P), len(allh), int(keep.sum()), a.max_dist,
                 int((~keep).sum())))
        Dk = D[keep]
        holes = [h for h, k in zip(hh["holes"], keep) if k]
        if not len(Dk):
            continue
        near = np.argmin(Dk, axis=1)
        srt = np.argsort(Dk, axis=1)
        d1 = Dk[np.arange(len(Dk)), srt[:, 0]]
        d2 = Dk[np.arange(len(Dk)), srt[:, 1]] if Dk.shape[1] > 1 else d1 * 0 + 99
        if HAVE_SCIPY:
            ri, ci = linear_sum_assignment(Dk)
            matched = np.full(len(Dk), -1)
            matched[ri] = ci
        else:
            matched = near.copy()
            print("  (scipy unavailable: optimal matching skipped)")
        disagree = 0
        print("%-20s %-12s %8s %-12s %8s %8s %8s"
              % ("hole (in)", "nearest", "d_near", "matched", "d_match",
                 "margin", "verdict"))
        for i in range(len(Dk)):
            nb, mb = near[i], matched[i]
            bad = mb >= 0 and mb != nb
            disagree += bool(bad)
            print("%-20s %-12s %8.4f %-12s %8.4f %8.4f %s"
                  % ("(%.3f, %.3f)" % (holes[i]["cx"] / dpi, holes[i]["cy"] / dpi),
                     labels[nb], d1[i],
                     labels[mb] if mb >= 0 else "-",
                     Dk[i, mb] if mb >= 0 else float("nan"),
                     d2[i] - d1[i],
                     "NEAREST-BULL WRONG" if bad else ("ok" if d2[i] - d1[i] > 0.15
                                                       else "ok (thin margin)")))
        # how far outside its own cell
        cell = pitch / dpi if pitch else 1.5
        off = []
        for i in range(len(Dk)):
            b = matched[i] if matched[i] >= 0 else near[i]
            dx = abs(holes[i]["cx"] - P[b, 0]) / dpi
            dy = abs(holes[i]["cy"] - P[b, 1]) / dpi
            over = max(dx, dy) - cell / 2.0
            if over > 0:
                off.append((holes[i], over, labels[b]))
        print("  cell size %.3f in. %d/%d shots land OUTSIDE their own cell; "
              "worst overshoot %.4f in"
              % (cell, len(off), len(Dk), max([o[1] for o in off]) if off else 0.0))
        for h, o, lb in sorted(off, key=lambda t: -t[1]):
            print("     (%.3f, %.3f) belongs to %s, %.4f in beyond the cell edge"
                  % (h["cx"] / dpi, h["cy"] / dpi, lb, o))
        print("  nearest-bull disagrees with the optimal one-to-one matching on "
              "%d of %d shots" % (disagree, len(Dk)))


if __name__ == "__main__":
    main()
