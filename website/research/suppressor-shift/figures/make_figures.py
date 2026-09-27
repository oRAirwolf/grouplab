"""Figures and numbers for 'Did the suppressor move the point of impact?' (NOTES-FROM-PLANNING.md entries 226 and 229).

Reads ../data/offsets-2026-09-26.csv, the offset of every shot from its own bull as GroupLab measured it on the developer's two 6 ARC sheets of
2026-09-26 (right and up positive, inches at 100 yd), and writes ../data/results.json and the figure. Two-sample Hotelling's T-squared on
the mean offsets (docs/STATISTICS.md section 8.2), a permutation test of the same statistic as the backstop of section 8.3, the dispersion
F test of section 8.1, each with every shot and again without the four shots that landed off their bulls.
"""
import csv
import json
import os
import sys

import numpy as np
from scipy import stats

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import _style as s  # noqa: E402
import matplotlib.pyplot as plt  # noqa: E402
from matplotlib.patches import Ellipse  # noqa: E402

here = os.path.dirname(os.path.abspath(__file__))
data = os.path.join(here, "..", "data")
MIL_IN = 3.6          # one mil at 100 yd, inches
MOA_IN = 1.0472       # one MOA at 100 yd, inches

rows = list(csv.DictReader(open(os.path.join(data, "offsets-2026-09-26.csv"), encoding="utf-8")))


def sheet(name, keep_off=True):
    return np.array([[float(r["right_in"]), float(r["up_in"])] for r in rows
                     if r["sheet"] == name and (keep_off or r["off_bull"] == "no")])


def hotelling(a, b):
    na, nb = len(a), len(b)
    d = a.mean(0) - b.mean(0)
    pooled = ((na - 1) * np.cov(a.T) + (nb - 1) * np.cov(b.T)) / (na + nb - 2)
    t2 = na * nb / (na + nb) * d @ np.linalg.solve(pooled, d)
    p_dim = 2
    f = (na + nb - p_dim - 1) / (p_dim * (na + nb - 2)) * t2
    p = stats.f.sf(f, p_dim, na + nb - p_dim - 1)
    return d, pooled, t2, f, p


def permutation(a, b, draws=20000, seed=226):
    rng = np.random.default_rng(seed)
    both = np.vstack([a, b])
    observed = hotelling(a, b)[2]
    hits = 0
    for _ in range(draws):
        idx = rng.permutation(len(both))
        if hotelling(both[idx[:len(a)]], both[idx[len(a):]])[2] >= observed:
            hits += 1
    return (hits + 1) / (draws + 1)


def dispersion(a, b):
    """Section 8.1: the ratio of the Rayleigh sigmas about each sheet's own centre, its F test and 95 percent interval."""
    ssa = ((a - a.mean(0)) ** 2).sum()
    ssb = ((b - b.mean(0)) ** 2).sum()
    dfa, dfb = 2 * (len(a) - 1), 2 * (len(b) - 1)
    f = (ssa / dfa) / (ssb / dfb)
    p = 2 * min(stats.f.cdf(f, dfa, dfb), stats.f.sf(f, dfa, dfb))
    ratio = np.sqrt(f)
    lo = ratio / np.sqrt(stats.f.ppf(0.975, dfa, dfb))
    hi = ratio / np.sqrt(stats.f.ppf(0.025, dfa, dfb))
    return ratio, (lo, hi), p


def sigma(a):
    """Rayleigh sigma about the sheet's own centre, the estimator of docs/STATISTICS.md section 2 without the c4 correction."""
    return float(np.sqrt(((a - a.mean(0)) ** 2).sum() / (2 * (len(a) - 1))))


def mean_radius(a):
    return float(np.linalg.norm(a - a.mean(0), axis=1).mean())


results = {}
for label, keep in (("all 25 shots a sheet", True), ("without the four shots that landed off their bulls", False)):
    a, b = sheet("dominus-k", keep), sheet("magnus-s", keep)
    d, pooled, t2, f, p = hotelling(a, b)
    na, nb = len(a), len(b)
    # 95 percent confidence ellipse of the difference: d' S^-1 d <= c, c from the F quantile.
    c = (2 * (na + nb - 2) / (na + nb - 3)) * stats.f.ppf(0.95, 2, na + nb - 3) * (1 / na + 1 / nb)
    vals, vecs = np.linalg.eigh(pooled)
    half_axes = np.sqrt(vals * c)
    ratio, (lo, hi), pd = dispersion(a, b)
    se = np.sqrt(np.diag(pooled) * (1 / na + 1 / nb))
    tq = stats.t.ppf(0.975, na + nb - 2)
    results[label] = {
        "shots": [na, nb],
        "dominus_mean_in": [round(float(v), 3) for v in a.mean(0)],
        "magnus_mean_in": [round(float(v), 3) for v in b.mean(0)],
        "shift_in": [round(float(v), 3) for v in -d],
        "shift_mil": [round(float(v) / MIL_IN, 3) for v in -d],
        "shift_moa": [round(float(v) / MOA_IN, 3) for v in -d],
        "shift_total_in": round(float(np.linalg.norm(d)), 3),
        "shift_interval_in": {"right": [round(float(-d[0] - tq * se[0]), 3), round(float(-d[0] + tq * se[0]), 3)],
                              "up": [round(float(-d[1] - tq * se[1]), 3), round(float(-d[1] + tq * se[1]), 3)]},
        "ellipse_half_axes_in": [round(float(v), 3) for v in half_axes],
        "hotelling_t2": round(float(t2), 2), "f": round(float(f), 2), "df": [2, na + nb - 3], "p": float(f"{p:.3g}"),
        "permutation_p": float(f"{permutation(a, b):.3g}"),
        "sigma_in": [round(sigma(a), 3), round(sigma(b), 3)],
        "mean_radius_in": [round(mean_radius(a), 3), round(mean_radius(b), 3)],
        "dispersion_ratio": round(float(ratio), 3), "dispersion_interval": [round(float(lo), 3), round(float(hi), 3)],
        "dispersion_p": float(f"{pd:.3g}"),
        "axis_sd_in": {"dominus": [round(float(v), 3) for v in a.std(0, ddof=1)], "magnus": [round(float(v), 3) for v in b.std(0, ddof=1)]},
    }

json.dump(results, open(os.path.join(data, "results.json"), "w", newline="\n"), indent=1)
open(os.path.join(data, "results.json"), "a", newline="\n").write("\n")
for k, v in results.items():
    print(k, json.dumps(v))

# The figure: every shot's offset from its own bull, both sheets, each mean with its 95 percent confidence ellipse.
s.apply()
fig, ax = plt.subplots(figsize=(7.2, 7.2))
for name, colour, title in (("dominus-k", s.BLUE, "Dominus K (shot first)"), ("magnus-s", s.ORANGE, "Magnus S (20 to 30 min later)")):
    pts = sheet(name)
    off = np.array([[float(r["right_in"]), float(r["up_in"])] for r in rows if r["sheet"] == name and r["off_bull"] == "yes"])
    ax.scatter(pts[:, 0], pts[:, 1], s=34, color=colour, alpha=0.75, label=title, edgecolor="none")
    ax.scatter(off[:, 0], off[:, 1], s=90, facecolor="none", edgecolor=colour, linewidth=1.6)
    m = pts.mean(0)
    cov = np.cov(pts.T) / len(pts)
    vals, vecs = np.linalg.eigh(cov)
    scale = np.sqrt(stats.chi2.ppf(0.95, 2))
    angle = np.degrees(np.arctan2(vecs[1, 1], vecs[0, 1]))
    ax.add_patch(Ellipse(m, 2 * scale * np.sqrt(vals[1]), 2 * scale * np.sqrt(vals[0]), angle=angle, facecolor=colour, alpha=0.18,
                         edgecolor=colour, linewidth=1.5))
    ax.scatter([m[0]], [m[1]], marker="+", s=220, color=colour, linewidth=2.4)
ax.axhline(0, color=s.MUTED, lw=1)
ax.axvline(0, color=s.MUTED, lw=1)
ax.set_aspect("equal")
ax.set_xlim(-1.1, 1.1)
ax.set_ylim(-1.0, 1.65)
ax.set_xlabel("right of the bull (in, 100 yd)")
ax.set_ylabel("above the bull (in, 100 yd)")
ax.set_title("Each shot from its own bull, both suppressors")
ax.legend(loc="upper right")
ax.text(-1.05, -0.8, "+ each sheet's mean, shaded its 95% region", color=s.INK2, fontsize=9.5)
ax.text(-1.05, -0.9, "circled: the four shots that landed off their bulls", color=s.INK2, fontsize=9.5)
s.save(fig, os.path.join(here, "offsets.png"))
s.save(fig, os.path.join(here, "lead.png"))
