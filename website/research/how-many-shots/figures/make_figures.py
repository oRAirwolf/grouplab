"""Figures for 'How many shots do you need?'. Closed-form chi-square and F results as in docs/STATISTICS.md sections 9.1 and 9.2,
plus one simulation (seed 2026) of repeated groups from one rifle."""
import sys, os, csv, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
from scipy import stats
from scipy.special import gammaln
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)

def c4(k): return min(1.0, math.sqrt(2 / (k - 1)) * math.exp(gammaln(k / 2) - gammaln((k - 1) / 2)))

# Figure 1: 95 percent interval on sigma as a multiple of the measured value
ns = np.arange(3, 101)
lo, hi = [], []
for n in ns:
    df = 2 * (n - 1)
    lo.append(math.sqrt(df / stats.chi2.ppf(0.975, df))); hi.append(math.sqrt(df / stats.chi2.ppf(0.025, df)))
lo, hi = np.array(lo), np.array(hi)
with open(os.path.join(here, "..", "data", "interval-by-shots.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["shots", "lower_multiple", "upper_multiple"])
    for n, a, b in zip(ns, lo, hi): w.writerow([n, round(a, 3), round(b, 3)])
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.fill_between(ns, lo, hi, color=s.BLUE, alpha=0.18, linewidth=0)
ax.plot(ns, lo, color=s.BLUE, lw=1.6); ax.plot(ns, hi, color=s.BLUE, lw=1.6)
ax.axhline(1, color=s.MUTED, lw=1)
for n in (5, 10, 25, 50):
    i = n - 3
    ax.plot([n, n], [lo[i], hi[i]], color=s.INK2, lw=1)
    ax.text(n + 1, hi[i] + 0.03, f"{n} shots: {lo[i]:.2f} to {hi[i]:.2f}", color=s.INK2, fontsize=9.5)
ax.set_xscale("log"); ax.set_xticks([3, 5, 10, 20, 30, 50, 100]); ax.set_xticklabels(["3", "5", "10", "20", "30", "50", "100"])
ax.set_xlabel("Shots in the group (log scale)"); ax.set_ylabel("True size as a multiple of what you measured")
ax.set_title("Where the rifle's true dispersion could be, 95 percent of the time")
ax.set_ylim(0.5, 3.0)
s.save(fig, os.path.join(here, "interval-by-shots.png"))

# Figure 2: shots per load to detect a difference in dispersion (80 percent power, 5 percent two-sided), exact F search
def exact_n(k, alpha=0.05, power=0.8):
    for n in range(3, 5000):
        df = 2 * (n - 1)
        crit = stats.f.ppf(1 - alpha / 2, df, df)
        # power: true variance ratio k^2; reject when F > crit (one direction dominates)
        p = 1 - stats.f.cdf(crit / k**2, df, df) + stats.f.cdf(stats.f.ppf(alpha / 2, df, df) / k**2, df, df)
        if p >= power: return n
ks = [1.05, 1.10, 1.15, 1.25, 1.5, 2.0]
nn = [exact_n(k) for k in ks]
with open(os.path.join(here, "..", "data", "shots-to-compare.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["ratio_k", "percent_tighter", "shots_per_load"])
    for k, n in zip(ks, nn): w.writerow([k, round((1 - 1 / k) * 100, 1), n])
fig, ax = plt.subplots(figsize=(8, 4.5))
labels = [f"{(1-1/k)*100:.0f}% tighter" for k in ks]
y = np.arange(len(ks))
ax.barh(y, nn, color=s.BLUE, height=0.6)
for yi, n in zip(y, nn): ax.text(n * 1.08, yi, f"{n} shots per load", va="center", color=s.INK2, fontsize=10)
ax.set_yticks(y); ax.set_yticklabels(labels); ax.invert_yaxis()
ax.set_xscale("log"); ax.set_xlim(5, 8000)
ax.set_xticks([10, 30, 100, 300, 1000, 3000]); ax.set_xticklabels(["10", "30", "100", "300", "1,000", "3,000"])
ax.set_xlabel("Shots per load (log scale)")
ax.set_title("Shots needed to show one load really groups tighter")
ax.grid(axis="y", visible=False)
s.save(fig, os.path.join(here, "shots-to-compare.png"))
print(list(zip(ks, nn)))

# Figure 3: twenty 5-shot groups against twenty 25-shot groups from one rifle (Rayleigh sigma estimate, true sigma = 1)
def sig(n):
    p = rng.normal(0, 1, (n, 2)); r2 = ((p - p.mean(0))**2).sum()
    return math.sqrt(r2 / (2 * (n - 1))) / c4(2 * n - 1)
a = [sig(5) for _ in range(20)]; b = [sig(25) for _ in range(20)]
fig, ax = plt.subplots(figsize=(8, 3.6))
ax.scatter(a, np.zeros(20) + rng.uniform(-0.12, 0.12, 20), s=48, color=s.ORANGE, edgecolor=s.SURFACE, linewidth=1.5)
ax.scatter(b, np.ones(20) + rng.uniform(-0.12, 0.12, 20), s=48, color=s.BLUE, edgecolor=s.SURFACE, linewidth=1.5)
ax.axvline(1, color=s.MUTED, lw=1); ax.text(1.01, 1.45, "true value", color=s.INK2, fontsize=9.5)
ax.set_yticks([0, 1]); ax.set_yticklabels(["20 groups of 5", "20 groups of 25"]); ax.set_ylim(-0.5, 1.6)
ax.set_xlabel("Measured dispersion (Rayleigh sigma), true value 1")
ax.set_title("Same rifle, measured again and again")
ax.grid(axis="y", visible=False)
s.save(fig, os.path.join(here, "repeat-measurements.png"))
with open(os.path.join(here, "..", "data", "repeat-measurements.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["group_size", "measured_sigma"])
    for v in a: w.writerow([5, round(v, 4)])
    for v in b: w.writerow([25, round(v, 4)])
print(min(a), max(a), min(b), max(b))
