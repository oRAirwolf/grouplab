"""Figures for 'Zeroing: when to adjust and when to leave it'. Simulation seed 2026, sigma = 1 per axis."""
import sys, os, csv, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
from matplotlib.patches import Ellipse
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)

# Figure 1: expected distance of the point of impact from the aim after adjusting on n shots, vs leaving it
offs = np.linspace(0, 2, 41)
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.plot(offs, offs, color=s.INK2, lw=1.6, ls="--")
ax.text(1.95, 1.9, "leave it alone", color=s.INK2, ha="right", va="bottom")
rows = []
for n, col in [(3, s.ORANGE), (5, s.BLUE), (10, s.AQUA)]:
    after = math.sqrt(math.pi / 2) / math.sqrt(n)   # expected radial error of the estimated centre
    ax.plot(offs, np.full_like(offs, after), color=col)
    ax.text(2.02, after, f"adjust after {n} shots", color=s.INK2, va="center")
    rows.append((n, round(after, 3)))
ax.set_xlim(0, 2.7); ax.set_ylim(0, 2.1)
ax.set_xlabel("How far off the zero really is, in units of shot-to-shot sigma")
ax.set_ylabel("Average miss of the new zero, sigma")
ax.set_title("Adjusting only helps when the real error is bigger than the noise")
s.save(fig, os.path.join(here, "adjust-or-not.png"))
with open(os.path.join(here, "..", "data", "error-after-adjusting.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["shots_before_adjusting", "average_miss_after_full_correction_sigma"]); w.writerows(rows)
print(rows)

# Figure 2: chasing the zero. A perfectly zeroed rifle, adjusted to the centre of every 3-shot group, 12 times
N = 12; true = np.zeros(2); zero = np.zeros(2); path_chase = []; path_leave = []
for k in range(N):
    shots = rng.normal(zero, 1, (3, 2))
    path_chase.append(zero.copy()); zero = zero - shots.mean(0)  # correct by the whole observed offset
fig, ax = plt.subplots(figsize=(6.2, 6.2))
pc = np.array(path_chase)
ax.plot(pc[:, 0], pc[:, 1], color=s.ORANGE, lw=1.4, marker="o", ms=6, label="Point of impact, adjusted after every 3 shots")
ax.scatter([0], [0], marker="+", s=300, color=s.INK, linewidths=2, zorder=4, label="Aim point (the rifle started perfectly zeroed)")
ax.set_xlim(-2.2, 2.2); ax.set_ylim(-2.2, 2.2); ax.set_aspect("equal")
ax.set_xlabel("Horizontal, sigma"); ax.set_ylabel("Vertical, sigma")
ax.set_title("Chasing the zero"); ax.legend(loc="upper left", fontsize=9)
s.save(fig, os.path.join(here, "chasing.png"))
# long-run average for the article
M = 200000; errs = []
z = np.zeros((M, 2)); tot = 0
for k in range(20):
    sh = rng.normal(z[:, None, :], 1, (M, 3, 2)); z = z - sh.mean(1)
d = np.hypot(z[:, 0], z[:, 1]).mean(); print("chase long-run mean miss", round(d, 3))
with open(os.path.join(here, "..", "data", "chasing-path.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["step", "x_sigma", "y_sigma"])
    for i, (x, y) in enumerate(pc): w.writerow([i, round(x, 3), round(y, 3)])

# Figure 3: the picture to make the decision: group centre with 90 percent uncertainty ellipse
fig, axes = plt.subplots(1, 2, figsize=(10, 4.8))
for ax, n, title in [(axes[0], 5, "5 shots: the aim is inside the uncertainty. Leave it."), (axes[1], 10, "10 shots: the aim is outside. Adjust.")]:
    want = np.array([0.6, 0.3]) if n == 5 else np.array([1.0, 0.6])
    sh = rng.normal(0, 1, (n, 2)); sh = sh - sh.mean(0) + want; c = sh.mean(0)
    rad = math.sqrt(-2 * math.log(0.10)) / math.sqrt(n)  # 90 percent region radius for the centre, sigma known
    ax.scatter(sh[:, 0], sh[:, 1], s=34, color=s.MUTED, edgecolor=s.SURFACE, linewidth=1.2)
    ax.add_patch(Ellipse(c, 2 * rad, 2 * rad, color=s.BLUE, alpha=0.18, lw=0))
    ax.add_patch(Ellipse(c, 2 * rad, 2 * rad, fill=False, color=s.BLUE, lw=1.6))
    ax.scatter([c[0]], [c[1]], s=60, color=s.BLUE, zorder=4)
    ax.scatter([0], [0], marker="+", s=300, color=s.INK, linewidths=2, zorder=4)
    ax.set_xlim(-2.6, 3.8); ax.set_ylim(-2.6, 3.2); ax.set_aspect("equal")
    ax.set_title(title, fontsize=10.5); ax.set_xlabel("sigma")
fig.suptitle("Group centre (blue dot), where the true centre probably is (blue area, 90 percent), and the aim (cross)", x=0.01, ha="left", fontsize=11.5, fontweight="bold")
s.save(fig, os.path.join(here, "decide.png"))
