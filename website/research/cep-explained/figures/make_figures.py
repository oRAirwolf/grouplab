"""Figures for 'CEP 50 and 90 explained'. Simulated, seed 2026."""
import sys, os, csv, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)
K50, K90, KMR = math.sqrt(2 * math.log(2)), math.sqrt(2 * math.log(10)), math.sqrt(math.pi / 2)

# Figure 1: a 30-shot round group with its circles (true sigma 1, drawn at the true values)
p = rng.normal(0, 1, (30, 2))
fig, ax = plt.subplots(figsize=(6.2, 6.2))
for r, col, lab in [(K90, s.ORANGE, "CEP 90: 2.15 sigma"), (KMR, s.AQUA, "Mean radius: 1.25 sigma"), (K50, s.BLUE, "CEP 50: 1.18 sigma")]:
    ax.add_patch(plt.Circle((0, 0), r, fill=False, color=col, lw=2, ls="--" if "CEP" in lab else "-", label=lab))
ax.scatter(p[:, 0], p[:, 1], s=36, color=s.INK2, edgecolor=s.SURFACE, linewidth=1.2, zorder=3)
ax.set_xlim(-3.3, 3.3); ax.set_ylim(-3.3, 3.3); ax.set_aspect("equal")
ax.set_xlabel("Horizontal, in units of sigma"); ax.set_ylabel("Vertical, in units of sigma")
ax.set_title("A round 30-shot group and its three circles")
ax.legend(loc="upper right", fontsize=9.5)
inside50 = (np.hypot(p[:, 0], p[:, 1]) <= K50).sum(); inside90 = (np.hypot(p[:, 0], p[:, 1]) <= K90).sum()
s.save(fig, os.path.join(here, "three-circles.png"))
print("inside", inside50, inside90)

# Figure 2: coverage of the round-group formula when the group is not round
ratios = np.linspace(1, 4, 13); N = 400000
rows = []
for a in ratios:
    sx = math.sqrt(2 * a**2 / (1 + a**2)); sy = sx / a   # keeps sx^2 + sy^2 = 2, same total spread as a round sigma=1 group
    x = rng.normal(0, sx, N); y = rng.normal(0, sy, N); r = np.hypot(x, y)
    rows.append((a, (r <= K50).mean() * 100, (r <= K90).mean() * 100))
rows = np.array(rows)
with open(os.path.join(here, "..", "data", "coverage-by-shape.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["width_to_height", "percent_inside_cep50_circle", "percent_inside_cep90_circle"])
    for r in rows: w.writerow([round(r[0], 2), round(r[1], 2), round(r[2], 2)])
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.plot(rows[:, 0], rows[:, 1], color=s.BLUE, marker="o", ms=5)
ax.plot(rows[:, 0], rows[:, 2], color=s.ORANGE, marker="o", ms=5)
ax.axhline(50, color=s.BLUE, lw=1, ls=":"); ax.axhline(90, color=s.ORANGE, lw=1, ls=":")
ax.text(4.05, rows[-1, 1], "CEP 50 circle", color=s.INK2, va="center"); ax.text(4.05, rows[-1, 2], "CEP 90 circle", color=s.INK2, va="center")
ax.set_xlim(1, 4.9); ax.set_ylim(40, 100)
ax.set_xlabel("How stretched the group is (long axis divided by short axis)")
ax.set_ylabel("Shots really inside the circle, percent")
ax.set_title("The round-group formula drifts as groups stretch")
s.save(fig, os.path.join(here, "coverage-by-shape.png"))
print(rows.round(1))

# Figure 3: a stretched group, 2:1, with the round-formula CEP 50 circle
sx = math.sqrt(2 * 4 / 5); sy = sx / 2
q = np.column_stack([rng.normal(0, sx, 30), rng.normal(0, sy, 30)])
fig, ax = plt.subplots(figsize=(6.2, 6.2))
ax.add_patch(plt.Circle((0, 0), K50, fill=False, color=s.BLUE, lw=2, ls="--", label="Round-formula CEP 50"))
from matplotlib.patches import Ellipse
ax.add_patch(Ellipse((0, 0), 2 * 1.1774 * sx, 2 * 1.1774 * sy, fill=False, color=s.AQUA, lw=2, label="Where half the shots really fall"))
ax.scatter(q[:, 0], q[:, 1], s=36, color=s.INK2, edgecolor=s.SURFACE, linewidth=1.2, zorder=3)
ax.set_xlim(-3.3, 3.3); ax.set_ylim(-3.3, 3.3); ax.set_aspect("equal")
ax.set_xlabel("Horizontal, in units of sigma"); ax.set_ylabel("Vertical")
ax.set_title("A group twice as wide as it is tall"); ax.legend(loc="upper right", fontsize=9.5)
s.save(fig, os.path.join(here, "stretched-group.png"))
