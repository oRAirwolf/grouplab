"""Figures for 'Does your printer print at true size?'. Arithmetic, plus GroupLab's Phase 0 print-scale measurement."""
import sys, os, csv
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
s.apply()
here = os.path.dirname(os.path.abspath(__file__))

# Figure 1: what "fit to page" does
fig, axes = plt.subplots(1, 2, figsize=(8.5, 5.2))
for ax, fit in zip(axes, [False, True]):
    ax.set_xlim(-0.3, 8.8); ax.set_ylim(-0.3, 11.3); ax.set_aspect("equal"); ax.axis("off")
    ax.add_patch(Rectangle((0, 0), 8.5, 11, fill=False, color=s.MUTED, lw=1.5))
    k = 0.941 if fit else 1.0
    w, h = 8.5 * k, 11 * k; x0, y0 = (8.5 - w) / 2, (11 - h) / 2
    ax.add_patch(Rectangle((x0, y0), w, h, color=s.BLUE if not fit else s.ORANGE, alpha=0.12))
    for i in range(5):
        for j in range(5):
            cx = x0 + (1.3 + i * 1.5) * k; cy = y0 + (3.2 + j * 1.5) * k
            ax.add_patch(plt.Circle((cx, cy), 0.5 * k, fill=False, color=s.INK2, lw=1))
    ax.set_title("Actual size, 100 percent" if not fit else "Fit to page, about 94 percent", fontsize=11)
    ax.text(4.25, -0.25, "8.5 x 11 in sheet", ha="center", color=s.INK2, fontsize=9)
fig.suptitle("\"Fit to page\" shrinks everything to the printer's margins", x=0.01, ha="left", fontsize=12, fontweight="bold")
s.save(fig, os.path.join(here, "fit-to-page.png"))

# Figure 2: error in every measurement, by print scale, if nothing corrects it
scales = np.array([0.90, 0.92, 0.94, 0.95, 0.962, 0.97, 0.98, 0.99, 1.0, 1.01])
err = (1 / scales - 1) * 100
with open(os.path.join(here, "..", "data", "error-by-scale.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["print_scale_percent", "measurement_error_percent_if_uncorrected", "a_0.300_in_mean_radius_reads"])
    for sc, e in zip(scales, err): w.writerow([round(sc * 100, 1), round(e, 2), round(0.3 / sc, 4)])
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.bar(scales * 100, err, width=0.7, color=[s.ORANGE if e > 0.5 else s.BLUE for e in err])
for sc, e in zip(scales, err): ax.text(sc * 100, e + (0.2 if e >= 0 else -0.5), f"{e:+.1f}%", ha="center", color=s.INK2, fontsize=9)
ax.axhline(0, color=s.MUTED, lw=1)
ax.set_xlabel("Print scale, percent"); ax.set_ylabel("Every measurement off by, percent")
ax.set_title("If nobody corrected it: how print scale feeds into your numbers")
ax.grid(axis="x", visible=False)
s.save(fig, os.path.join(here, "error-by-scale.png"))

# Figure 3: one thermal printer, true across and short along the feed (entry 391, two check prints, October 2026).
# Every reading is a length GroupLab drew against what was measured on the paper: 150.02 mm between the check page's crosshairs,
# 250.02 mm along its side ruler. Print 1 is nightly 175's page, print 2 nightly 180's; both were drawn the same.
READINGS = [
    # (print, instrument, direction, drawn mm, measured mm)
    (1, "caliper", "across", 150.02, 150.47),
    (2, "caliper", "across", 150.02, 149.71),
    (2, "scan", "across", 150.02, 150.02),
    (1, "caliper", "along the feed", 150.02, 148.81),
    (1, "ruler", "along the feed", 250.02, 248.0),
    (2, "caliper", "along the feed", 150.02, 149.03),
    (2, "ruler", "along the feed", 250.02, 248.0),
    (2, "scan", "along the feed", 150.02, 149.25),
]
with open(os.path.join(here, "..", "data", "thermal-check-prints.csv"), "w", newline="") as f:
    w = csv.writer(f); w.writerow(["print", "instrument", "direction", "drawn_mm", "measured_mm", "printed_percent"])
    for p, inst, d, drawn, got in READINGS: w.writerow([p, inst, d, drawn, got, round(got / drawn * 100, 2)])
fig, ax = plt.subplots(figsize=(8, 3.6))
marks = {"caliper": "o", "ruler": "s", "scan": "D"}
for row, d in enumerate(["across", "along the feed"]):
    vals = [(inst, got / drawn * 100) for p, inst, dd, drawn, got in READINGS if dd == d]
    for k, (inst, v) in enumerate(vals):
        ax.plot(v, row + (k - (len(vals) - 1) / 2) * 0.08, marks[inst], color=s.BLUE if d == "across" else s.ORANGE, ms=8,
                markeredgecolor=s.SURFACE)
    mean = np.mean([v for _, v in vals])
    ax.plot([mean, mean], [row - 0.3, row + 0.3], color=s.INK2, lw=1.4)
    ax.text(mean, row + 0.36, f"mean {mean:.2f}%", ha="center", color=s.INK2, fontsize=9.5)
ax.axvline(100, color=s.MUTED, lw=1, ls="--")
ax.set_yticks([0, 1]); ax.set_yticklabels(["Across", "Along the feed"])
ax.set_ylim(-0.6, 1.6); ax.set_xlim(98.8, 100.8)
ax.set_xlabel("Printed size, percent of what GroupLab drew")
ax.set_title("One thermal printer, two check prints: true across, short along the feed")
from matplotlib.lines import Line2D
ax.legend(handles=[Line2D([], [], marker=m, ls="", color=s.INK2, ms=7, label=k) for k, m in marks.items()], loc="lower right", fontsize=9)
ax.grid(axis="y", visible=False)
s.save(fig, os.path.join(here, "thermal-two-ways.png"))
