"""Figure for 'A printed number is not a bullet hole'. Drawn shapes, not a maker's artwork; seed 2026 for the torn edge.

The numbers printed under each shape are the measured ranges from entry 352 item 2 (commit 9421ee13) and the constants in
src/GroupLab.Core/Detection/PrintedShape.cs, not measurements of these drawings.
"""
import sys, os, shutil
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
from matplotlib.patches import Polygon, Wedge
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(2026)

fig, axes = plt.subplots(1, 3, figsize=(10, 4.4))
for ax in axes:
    ax.set_xlim(-1.5, 1.5); ax.set_ylim(-1.9, 1.5); ax.set_aspect("equal"); ax.axis("off")

# A bold 6: strokes of one width, closing round a small counter.
axes[0].text(0, 0, "6", fontsize=120, fontweight="bold", ha="center", va="center", color=s.INK, family="DejaVu Sans")
axes[0].set_title("A printed 6", fontsize=11.5)
axes[0].text(0, -1.45, "Even strokes: 0.63 to 0.92\n(printed numbers and letters)\nA 6's counter: 5 to 7 percent", ha="center", va="center",
             fontsize=9.5, color=s.INK2)

# A torn hole: a wide core with spikes of paper that narrow to nothing.
t = np.linspace(0, 2 * np.pi, 400, endpoint=False)
r = 0.62 + 0.04 * rng.standard_normal(400).cumsum() / 20
for k in rng.choice(400, 7, replace=False):
    r += 0.32 * np.exp(-((np.angle(np.exp(1j * (t - t[k])))) / 0.05) ** 2)
axes[1].add_patch(Polygon(np.column_stack([r * np.cos(t), r * np.sin(t)]), closed=True, color=s.INK))
axes[1].set_title("A torn hole", fontsize=11.5)
axes[1].text(0, -1.45, "Even strokes: at most 0.60\n(synthetic holes)", ha="center", va="center", fontsize=9.5, color=s.INK2)

# A crescent of dark rim, open on one side: as even as a stroke, enclosing nothing.
axes[2].add_patch(Wedge((0, 0), 0.8, 40, 320, width=0.2, color=s.INK))
axes[2].set_title("A hole's rim on a scan", fontsize=11.5)
axes[2].text(0, -1.45, "Even strokes: up to 0.84\n(real holes, fifteen scans)\nEncloses nothing", ha="center", va="center", fontsize=9.5, color=s.INK2)

fig.suptitle("A dark mark is refused as print only when its strokes are even and it closes round a small counter",
             x=0.01, ha="left", fontsize=11.5, fontweight="bold", color=s.INK)
s.save(fig, os.path.join(here, "three-shapes.png"))
shutil.copyfile(os.path.join(here, "three-shapes.png"), os.path.join(here, "lead.png"))
