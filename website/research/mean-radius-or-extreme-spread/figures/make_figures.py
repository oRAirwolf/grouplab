"""Figures for 'Mean radius or extreme spread?'. Simulated, seed 2026. Circular normal shots, sigma = 1."""
import sys, os, csv
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
from scipy.spatial.distance import pdist
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)

def es(p): return pdist(p).max()
def mr(p): c = p.mean(0); return np.linalg.norm(p - c, axis=1).mean()

# Figure 1: ten 5-shot groups from one rifle
fig, axes = plt.subplots(2, 5, figsize=(11, 4.8))
rows = []
for i, ax in enumerate(axes.flat):
    p = rng.normal(0, 1, (5, 2))
    e, m = es(p), mr(p)
    rows.append((i + 1, round(e, 3), round(m, 3)))
    c = p.mean(0)
    ax.scatter(p[:, 0], p[:, 1], s=36, color=s.BLUE, edgecolor=s.SURFACE, linewidth=1.5, zorder=3)
    d = pdist(p); import itertools
    pairs = list(itertools.combinations(range(5), 2)); a, b = pairs[int(d.argmax())]
    ax.plot(p[[a, b], 0], p[[a, b], 1], color=s.ORANGE, lw=1.6, zorder=2)
    ax.add_patch(plt.Circle(c, m, fill=False, color=s.AQUA, lw=1.6, zorder=2))
    ax.set_xlim(-3.2, 3.2); ax.set_ylim(-3.2, 3.2); ax.set_aspect("equal")
    ax.set_xticks([]); ax.set_yticks([]); ax.grid(False)
    for sp in ax.spines.values(): sp.set_visible(True); sp.set_color(s.GRID)
    ax.set_title(f"ES {e:.2f}   MR {m:.2f}", fontsize=10, fontweight="normal", color=s.INK2, loc="center")
fig.suptitle("Ten 5-shot groups from the same simulated rifle (sigma = 1). Orange: extreme spread. Aqua: mean radius.",
             x=0.01, ha="left", fontsize=11.5, fontweight="bold", color=s.INK)
s.save(fig, os.path.join(here, "ten-groups.png"))
with open(os.path.join(here, "..", "data", "ten-groups.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["group", "extreme_spread_sigma", "mean_radius_sigma"]); w.writerows(rows)

# Figure 2: how much each figure wanders, by shots per group
ns = [3, 5, 7, 10, 15, 20, 25, 30]; N = 20000
out = []
for n in ns:
    E = np.empty(N); M = np.empty(N)
    for k in range(N):
        p = rng.normal(0, 1, (n, 2)); E[k] = es(p); M[k] = mr(p)
    er = np.percentile(E, 95) / np.percentile(E, 5); mrr = np.percentile(M, 95) / np.percentile(M, 5)
    out.append((n, E.mean(), M.mean(), er, mrr, E.std() / E.mean(), M.std() / M.mean()))
out = np.array(out)
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.plot(out[:, 0], out[:, 5] * 100, color=s.ORANGE, marker="o", ms=6, label="Extreme spread")
ax.plot(out[:, 0], out[:, 6] * 100, color=s.BLUE, marker="o", ms=6, label="Mean radius")
ax.set_xlabel("Shots in the group"); ax.set_ylabel("Typical wander, percent (coefficient of variation)")
ax.set_title("How much the figure changes from one group to the next")
ax.text(30.6, out[-1, 5] * 100, "Extreme spread", color=s.INK2, va="center")
ax.text(30.6, out[-1, 6] * 100, "Mean radius", color=s.INK2, va="center")
ax.set_xlim(2, 36)
ax.set_ylim(0, None)
s.save(fig, os.path.join(here, "wander-by-shots.png"))

# Figure 3: extreme spread grows with shot count; mean radius does not
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.plot(out[:, 0], out[:, 1], color=s.ORANGE, marker="o", ms=6)
ax.plot(out[:, 0], out[:, 2], color=s.BLUE, marker="o", ms=6)
ax.text(out[-1, 0] - 0.5, out[-1, 1] + 0.12, "Extreme spread", color=s.INK2, ha="right")
ax.text(out[-1, 0] - 0.5, out[-1, 2] + 0.12, "Mean radius", color=s.INK2, ha="right")
ax.set_xlabel("Shots in the group"); ax.set_ylabel("Average value, in units of sigma")
ax.set_title("Same rifle, more shots: extreme spread keeps growing")
ax.set_ylim(0, None)
s.save(fig, os.path.join(here, "growth-by-shots.png"))
with open(os.path.join(here, "..", "data", "by-shot-count.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["shots", "mean_es_sigma", "mean_mr_sigma", "es_p95_over_p5", "mr_p95_over_p5", "es_cv", "mr_cv"])
    for r in out: w.writerow([int(r[0])] + [round(x, 4) for x in r[1:]])
print(out.round(3))
