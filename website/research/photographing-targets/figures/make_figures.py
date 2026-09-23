"""Figures for 'How to photograph a target so it measures well'. Data: GroupLab measurements of the developer's 2026-09-20 range
sheets (question 38, docs/QUESTIONS-FOR-PLANNING.md), pixels only, no image metadata read."""
import sys, os, csv
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
from matplotlib.patches import Polygon, Rectangle
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(here, "..", "data", "hole-ratio-by-image.csv"))))
sheets = [".22 LR", "6 ARC", "6.5 Creedmoor 25-shot", "6.5 Creedmoor 15-shot"]

# Figure 1: hole size as a multiple of bullet diameter, scan against photos, per sheet
fig, ax = plt.subplots(figsize=(8.5, 4.6))
for i, sh in enumerate(sheets):
    sc = [float(r["median_hole_over_bullet"]) for r in rows if r["sheet"] == sh and r["kind"] == "scan"]
    ph = [float(r["median_hole_over_bullet"]) for r in rows if r["sheet"] == sh and r["kind"] == "photo"]
    ax.scatter([i - 0.12] * len(sc), sc, s=80, marker="s", color=s.BLUE, edgecolor=s.SURFACE, linewidth=1.5, zorder=3, label="Scan (600 dpi)" if i == 0 else None)
    ax.scatter(np.full(len(ph), i + 0.12) + np.linspace(-0.05, 0.05, len(ph)), ph, s=70, color=s.ORANGE, edgecolor=s.SURFACE, linewidth=1.5, zorder=3, label="Phone photo" if i == 0 else None)
ax.axhline(1, color=s.MUTED, lw=1); ax.text(3.45, 1.01, "bullet diameter", color=s.INK2, fontsize=9, ha="right", va="bottom")
ax.set_xticks(range(4)); ax.set_xticklabels([".22 LR", "6 ARC", "6.5 CM\n25-shot sheet", "6.5 CM\n15-shot sheet"])
ax.set_ylabel("Measured hole / bullet diameter"); ax.set_ylim(0.6, 1.6)
ax.set_title("Same sheets: scans agree with each other, photos do not"); ax.legend(loc="upper left", fontsize=9.5)
ax.grid(axis="x", visible=False)
s.save(fig, os.path.join(here, "scan-vs-photo.png"))

# Figure 2: why, seen from the camera (schematic)
from matplotlib.patches import Circle, Wedge
fig, axes = plt.subplots(1, 2, figsize=(9, 4.4))
for ax, low in zip(axes, [False, True]):
    ax.set_xlim(-2, 2); ax.set_ylim(-2, 2); ax.set_aspect("equal"); ax.axis("off")
    ax.add_patch(Rectangle((-2, -2), 4, 4, color="#f2f1ec"))
    if low:
        ax.add_patch(Circle((0.28, -0.12), 1.0, color="#6d6c68"))
        ax.add_patch(Circle((0, 0), 1.0, color="#262624"))
        ax.add_patch(Circle((0.14, -0.06), 1.16, fill=False, color=s.ORANGE, lw=2, ls="--"))
        ax.annotate("", xy=(-1.1, 0.5), xytext=(-1.9, 0.85), arrowprops=dict(arrowstyle="->", color=s.ORANGE, lw=1.6))
        ax.text(-1.95, 1.05, "low sun", color=s.ORANGE, fontsize=10)
        ax.set_title("Low, raking light: shadow joins the hole", fontsize=11)
        ax.text(0, -1.75, "orange: what the software measures", ha="center", color=s.INK2, fontsize=9.5)
    else:
        ax.add_patch(Circle((0, 0), 1.0, color="#262624"))
        ax.add_patch(Circle((0, 0), 1.0, fill=False, color=s.BLUE, lw=2, ls="--"))
        ax.set_title("Even light: the hole is the hole", fontsize=11)
        ax.text(0, -1.75, "blue: what the software measures", ha="center", color=s.INK2, fontsize=9.5)
fig.suptitle("Why a photo can make a hole look bigger (schematic)", x=0.01, ha="left", fontsize=12, fontweight="bold")
s.save(fig, os.path.join(here, "shadow-schematic.png"))

# Figure 3: registration residual, scans against photos
fig, ax = plt.subplots(figsize=(8, 3.8))
sc = [float(r["registration_rms_in"]) * 1000 for r in rows if r["kind"] == "scan"]
ph = [float(r["registration_rms_in"]) * 1000 for r in rows if r["kind"] == "photo"]
ax.scatter(sc, np.zeros(len(sc)), s=80, marker="s", color=s.BLUE, edgecolor=s.SURFACE, linewidth=1.5)
ax.scatter(ph, np.ones(len(ph)), s=70, color=s.ORANGE, edgecolor=s.SURFACE, linewidth=1.5)
ax.set_yticks([0, 1]); ax.set_yticklabels(["Scans", "Photos"]); ax.set_ylim(-0.6, 1.6); ax.set_xlim(0, 7)
ax.set_xlabel("Marker fit error, thousandths of an inch (RMS)")
ax.set_title("Both are precise where it matters: the sheet itself is placed to within a few thousandths")
ax.grid(axis="y", visible=False)
s.save(fig, os.path.join(here, "registration.png"))
