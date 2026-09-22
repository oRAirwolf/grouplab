"""Figures for 'Can you see the bull?'. Geometry only; apparent size = feature angle x magnification."""
import sys, os, csv, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
ARCMIN_PER_IN_100YD = 1 / 3600 * (180 / math.pi) * 60   # 0.955 arcmin per inch at 100 yd

mags = np.linspace(4, 40, 200)
feats = [(0.03, "0.03 in line (current bull rings)", s.ORANGE), (0.10, "0.10 in dot (current bull centre)", s.BLUE), (0.25, "0.25 in feature", s.AQUA)]
fig, ax = plt.subplots(figsize=(8.5, 4.8))
ax.axhspan(0, 1, color="#e9e7e0", lw=0); ax.text(39.6, 0.22, "below what the eye resolves in perfect conditions (about 1 arcminute)", fontsize=8.5, color=s.INK2, ha="right")
ax.axhline(4, color=s.MUTED, lw=1, ls="--"); ax.text(4.5, 4.15, "proposed working minimum, 4 arcminutes", fontsize=9, color=s.INK2)
for w, lab, col in feats:
    ax.plot(mags, w * ARCMIN_PER_IN_100YD * mags, color=col, label=lab)
for m, name in [(25, "Strike Eagle max"), (35, "DNT max"), (36, "Razor max")]:
    ax.axvline(m, color=s.GRID, lw=1.2); ax.text(m, 9.6, name, rotation=90, fontsize=8.5, color=s.INK2, ha="right", va="top")
ax.set_xlim(4, 40); ax.set_ylim(0, 10)
ax.set_xlabel("Magnification"); ax.set_ylabel("Apparent size at your eye, arcminutes")
ax.set_title("How big a printed feature looks through the scope at 100 yards")
ax.legend(loc="upper left", fontsize=9, bbox_to_anchor=(0.0, 0.93))
s.save(fig, os.path.join(here, "apparent-size.png"))

rows = []
for m in (4, 6, 8, 10, 15, 18, 20, 25, 30, 35, 36):
    rows.append([m] + [round(w * ARCMIN_PER_IN_100YD * m, 2) for w, _, _ in feats] + [round(4 / (ARCMIN_PER_IN_100YD * m), 3)])
with open(os.path.join(here, "..", "data", "apparent-size.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["magnification", "arcmin_0.03in", "arcmin_0.10in", "arcmin_0.25in", "smallest_feature_in_for_4_arcmin_at_100yd"]); w.writerows(rows)

# Figure 2: smallest feature for 4 arcmin, by magnification and distance
fig, ax = plt.subplots(figsize=(8, 4.5))
for yd, col in [(25, s.AQUA), (50, s.BLUE), (100, s.ORANGE)]:
    need = 4 / (ARCMIN_PER_IN_100YD * (100 / yd) * mags)
    ax.plot(mags, need, color=col); ax.text(40.5, need[-1], f"{yd} yd", va="center", color=s.INK2)
ax.set_xlim(4, 45); ax.set_ylim(0, 1.1)
ax.set_xlabel("Magnification"); ax.set_ylabel("Smallest feature, inches")
ax.set_title("How big a feature must be printed to look 4 arcminutes wide")
s.save(fig, os.path.join(here, "feature-size-needed.png"))
print(rows)
