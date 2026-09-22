"""Figures for 'Scanner traps'. Scan-bed geometry from Alan's range-day scan 2; synthetic hole image (seed 2026) for the settings illustration."""
import sys, os, csv, io
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)

# Figure 1: the scanner bed is smaller than the paper
fig, ax = plt.subplots(figsize=(5.6, 7))
ax.set_xlim(-0.4, 9.0); ax.set_ylim(-0.4, 11.4); ax.set_aspect("equal"); ax.axis("off")
ax.add_patch(Rectangle((0, 0), 8.5, 11, fill=False, color=s.MUTED, lw=1.5))
ax.add_patch(Rectangle((0, 11 - 10.76), 8.26, 10.76, color=s.BLUE, alpha=0.12, lw=0))
ax.add_patch(Rectangle((0, 11 - 10.76), 8.26, 10.76, fill=False, color=s.BLUE, lw=1.6, ls="--"))
ax.text(4.1, 5.5, "what the scanner\ncaptured\n8.26 x 10.76 in", ha="center", color=s.BLUE, fontsize=11)
ax.text(8.6, 5.5, "0.24 in\nlost", color=s.ORANGE, fontsize=9.5, va="center")
ax.text(4.25, -0.3, "0.24 in lost", color=s.ORANGE, fontsize=9.5, ha="center")
ax.text(0.1, 11.1, "8.5 x 11 in letter sheet", color=s.INK2, fontsize=9.5)
ax.set_title("A letter sheet on a common flatbed")
s.save(fig, os.path.join(here, "scan-bed.png"))

# Figure 2: the same hole through three scanner settings (synthetic)
n = 160; y, x = np.mgrid[0:n, 0:n]; r = np.hypot(x - n / 2, y - n / 2)
paper = 0.93 + rng.normal(0, 0.015, (n, n))
hole = 1 / (1 + np.exp((r - 34) / 1.6))                 # dark centre with a soft edge
wipe = np.exp(-((r - 40) / 5.0) ** 2) * 0.35              # grey bullet wipe ring
img = np.clip(paper - hole * 0.8 - wipe, 0, 1)
pencil = (np.abs(y - 128) < 1.2) & (x > 20) & (x < 140)
img[pencil] = np.minimum(img[pencil], 0.62)
thr = (img > 0.5).astype(float)
from PIL import Image
buf = io.BytesIO(); Image.fromarray((img * 255).astype(np.uint8)).save(buf, format="JPEG", quality=8); jpg = np.asarray(Image.open(io.BytesIO(buf.getvalue()))) / 255
fig, axes = plt.subplots(1, 3, figsize=(10, 3.9))
for ax, im, t in zip(axes, [img, thr, jpg], ["Greyscale or colour photo mode", "Black-and-white document mode", "Heavy JPEG compression"]):
    ax.imshow(im, cmap="gray", vmin=0, vmax=1); ax.set_xticks([]); ax.set_yticks([]); ax.grid(False); ax.set_title(t, fontsize=10.5)
fig.suptitle("One synthetic bullet hole, with its grey wipe ring and a pencil line, through three settings", x=0.01, ha="left", fontsize=12, fontweight="bold")
s.save(fig, os.path.join(here, "settings.png"))
