"""Figures for 'Velocity SD from 5, 10 and 20 shots'. Chi-square intervals on n-1 degrees of freedom; simulation seed 2026."""
import sys, os, csv, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
from scipy import stats
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)

# Figure 1: interval on the true SD as a multiple of the measured SD
ns = np.arange(3, 51); lo = []; hi = []
for n in ns:
    df = n - 1
    lo.append(math.sqrt(df / stats.chi2.ppf(0.975, df))); hi.append(math.sqrt(df / stats.chi2.ppf(0.025, df)))
with open(os.path.join(here, "..", "data", "sd-interval-by-shots.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["shots", "lower_multiple", "upper_multiple", "example_lower_fps_for_sd_10", "example_upper_fps_for_sd_10"])
    for n, a, b in zip(ns, lo, hi): w.writerow([n, round(a, 3), round(b, 3), round(10 * a, 1), round(10 * b, 1)])
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.fill_between(ns, np.array(lo) * 10, np.array(hi) * 10, color=s.BLUE, alpha=0.18, linewidth=0)
ax.plot(ns, np.array(lo) * 10, color=s.BLUE, lw=1.6); ax.plot(ns, np.array(hi) * 10, color=s.BLUE, lw=1.6)
ax.axhline(10, color=s.MUTED, lw=1)
for n in (5, 10, 20, 40):
    i = n - 3
    ax.plot([n, n], [lo[i] * 10, hi[i] * 10], color=s.INK2, lw=1)
    ax.text(n + 0.8, hi[i] * 10 + 0.6, f"{n} shots: {lo[i]*10:.1f} to {hi[i]*10:.1f}", color=s.INK2, fontsize=9.5)
ax.set_ylim(0, 40); ax.set_xlim(3, 50)
ax.set_xlabel("Shots over the chronograph"); ax.set_ylabel("True SD, ft/s (you measured 10)")
ax.set_title("You measured an SD of 10 ft/s. The true SD is probably in here.")
s.save(fig, os.path.join(here, "sd-interval.png"))
print([(n, round(lo[n-3]*10,1), round(hi[n-3]*10,1)) for n in (3,5,10,20,30,40,50)])

# Figure 2: one load, true SD 10 ft/s, measured 30 times with 5 shots and 30 times with 20 shots
a = [rng.normal(2800, 10, 5).std(ddof=1) for _ in range(30)]
b = [rng.normal(2800, 10, 20).std(ddof=1) for _ in range(30)]
fig, ax = plt.subplots(figsize=(8, 3.6))
ax.scatter(a, np.zeros(30) + rng.uniform(-0.12, 0.12, 30), s=48, color=s.ORANGE, edgecolor=s.SURFACE, linewidth=1.5)
ax.scatter(b, np.ones(30) + rng.uniform(-0.12, 0.12, 30), s=48, color=s.BLUE, edgecolor=s.SURFACE, linewidth=1.5)
ax.axvline(10, color=s.MUTED, lw=1); ax.text(10.2, 1.45, "true SD, 10 ft/s", color=s.INK2, fontsize=9.5)
ax.set_yticks([0, 1]); ax.set_yticklabels(["30 strings of 5", "30 strings of 20"]); ax.set_ylim(-0.5, 1.6)
ax.set_xlabel("Measured SD, ft/s"); ax.set_title("The same ammunition, chronographed again and again")
ax.grid(axis="y", visible=False)
s.save(fig, os.path.join(here, "repeat-strings.png"))
with open(os.path.join(here, "..", "data", "repeat-strings.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["string_length", "measured_sd_fps"])
    for v in a: w.writerow([5, round(v, 2)])
    for v in b: w.writerow([20, round(v, 2)])
print(round(min(a),1), round(max(a),1), round(min(b),1), round(max(b),1))

# Table: expected velocity ES as a multiple of SD (d2 constants by simulation)
rows = []
for n in (3, 5, 10, 20, 30):
    x = rng.normal(0, 1, (200000, n)); r = x.max(1) - x.min(1)
    rows.append((n, r.mean()))
with open(os.path.join(here, "..", "data", "es-over-sd.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["shots", "average_es_as_multiple_of_true_sd"])
    for n, v in rows: w.writerow([n, round(v, 3)])
print([(n, round(v, 2)) for n, v in rows])
