"""Figures for 'MOA, mils and inches'. One simulated 10-shot group at 100 yd (seed 2026), shown in four units."""
import sys, os, csv, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)
p = rng.normal(0, 0.3, (10, 2))  # inches at 100 yd
d = 3600.0  # inches in 100 yd
def to_moa(x): return 2 * np.degrees(np.arctan(x / (2 * d))) * 60
def to_mil(x): return 2 * np.arctan(x / (2 * d)) * 1000
units = [("Inches at the target", lambda x: x, "in"), ("Centimetres at the target", lambda x: x * 2.54, "cm"),
         ("MOA (1 MOA = 1.047 in at 100 yd)", to_moa, "MOA"), ("Mil (1 mil = 3.6 in at 100 yd)", to_mil, "mil")]
fig, axes = plt.subplots(1, 4, figsize=(13, 3.8))
mr_in = np.linalg.norm(p - p.mean(0), axis=1).mean()
rows = []
for ax, (title, f, u) in zip(axes, units):
    q = f(p); lim = f(np.array(1.0))
    ax.scatter(q[:, 0], q[:, 1], s=36, color=s.BLUE, edgecolor=s.SURFACE, linewidth=1.2, zorder=3)
    ax.add_patch(plt.Circle(q.mean(0), float(f(np.array(mr_in))), fill=False, color=s.AQUA, lw=1.6))
    ax.set_xlim(-lim, lim); ax.set_ylim(-lim, lim); ax.set_aspect("equal")
    ax.set_title(title, fontsize=10.5, loc="left"); ax.set_xlabel(u)
    ax.text(0.03, 0.03, f"mean radius {float(f(np.array(mr_in))):.3f} {u}", transform=ax.transAxes, fontsize=9.5, color=s.INK2)
    rows.append((u, round(float(f(np.array(mr_in))), 4)))
fig.suptitle("One 10-shot group at 100 yards, four ways. The shots do not move; only the ruler changes.", x=0.01, ha="left", fontsize=12, fontweight="bold")
s.save(fig, os.path.join(here, "one-group-four-ways.png"))
with open(os.path.join(here, "..", "data", "example-group.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["shot", "x_in", "y_in", "x_moa", "y_moa", "x_mil", "y_mil"])
    for i, (x, y) in enumerate(p): w.writerow([i + 1, round(x, 4), round(y, 4), round(float(to_moa(x)), 4), round(float(to_moa(y)), 4), round(float(to_mil(x)), 4), round(float(to_mil(y)), 4)])
print(rows)

# Figure 2: what one unit covers at distance
yards = np.array([25, 50, 100, 200, 300, 500, 600, 1000])
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.plot(yards, yards / 100 * 1.0472, color=s.BLUE, marker="o", ms=5)
ax.plot(yards, yards / 100 * 3.6, color=s.ORANGE, marker="o", ms=5)
ax.text(1010, 10 * 1.0472, "1 MOA", color=s.INK2, va="center"); ax.text(1010, 36, "1 mil", color=s.INK2, va="center")
ax.set_xlim(0, 1120); ax.set_ylim(0, None)
ax.set_xlabel("Distance, yards"); ax.set_ylabel("Size at the target, inches")
ax.set_title("What one unit covers at each distance")
s.save(fig, os.path.join(here, "unit-size-by-distance.png"))
with open(os.path.join(here, "..", "data", "unit-sizes.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["yards", "inches_per_moa", "inches_per_mil", "inches_per_quarter_moa_click", "inches_per_tenth_mil_click"])
    for y in yards: w.writerow([y, round(y / 100 * 1.0472, 3), round(y / 100 * 3.6, 3), round(y / 100 * 1.0472 / 4, 3), round(y / 100 * 0.36, 3)])
