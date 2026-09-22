"""Figures for 'Aim points for 1x to high power optics'. Geometry and illustrative reticle values, no measured data yet."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import numpy as np
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
IN_PER_MOA_100 = 1.0472

# Figure 1: a 2 MOA dot centred in rings of 2, 3, 4 and 6 times its size (angular, so it looks the same at any distance)
fig, axes = plt.subplots(1, 4, figsize=(11, 3.4))
for ax, k in zip(axes, [2, 3, 4, 6]):
    ax.set_xlim(-7, 7); ax.set_ylim(-8.2, 7); ax.set_aspect("equal"); ax.axis("off")
    ax.add_patch(plt.Circle((0, 0), k, fill=False, color=s.INK, lw=3))
    ax.add_patch(plt.Circle((0, 0), 1, color="#e34948"))
    ax.set_title(f"Ring {k}x the dot ({2*k} MOA)", fontsize=10.5, loc="center")
    ax.text(0, -7.8, f"{2*k*IN_PER_MOA_100*0.25:.2f} in at 25 yd, {2*k*IN_PER_MOA_100*0.5:.2f} in at 50 yd", ha="center", fontsize=9, color=s.INK2)
fig.suptitle("A 2 MOA red dot inside rings of different sizes: which one can you centre it in most repeatably?", x=0.01, ha="left", fontsize=12, fontweight="bold")
s.save(fig, os.path.join(here, "dot-in-rings.png"))

# Figure 2: first versus second focal plane, apparent reticle line width (illustrative values)
m = np.linspace(1, 10, 100)
ffp_line_mil = 0.06   # illustrative line width in mil, fixed against the target
sfp_line_arcmin = 1.2 # illustrative apparent width at the eye, constant
ffp_apparent = ffp_line_mil * 3.438 * m   # mil to arcmin at the target, times magnification
fig, ax = plt.subplots(figsize=(8, 4.5))
ax.plot(m, ffp_apparent, color=s.BLUE); ax.plot(m, np.full_like(m, sfp_line_arcmin), color=s.ORANGE)
ax.text(10.1, ffp_apparent[-1], "First focal plane", va="center", color=s.INK2)
ax.text(10.1, sfp_line_arcmin, "Second focal plane", va="center", color=s.INK2)
ax.axhspan(0, 1, color="#e9e7e0", lw=0); ax.text(9.9, 0.3, "hard to see", ha="right", fontsize=9, color=s.INK2)
ax.set_xlim(1, 12.5); ax.set_ylim(0, 2.4)
ax.set_xlabel("Magnification"); ax.set_ylabel("Apparent reticle line width, arcmin")
ax.set_title("Why the focal plane matters on a low power scope (illustrative)")
s.save(fig, os.path.join(here, "ffp-vs-sfp.png"))
