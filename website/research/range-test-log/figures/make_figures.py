"""Timeline figure for 'The range test log'."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import _style as s
import matplotlib.pyplot as plt
s.apply()
here = os.path.dirname(os.path.abspath(__file__))
events = [("Sep 2026", "Print tests", "Does a sheet print true to size,\nand can GroupLab tell when not?", s.BLUE),
          ("First visit", "First range sheet", "9 shots on a 25-bull sheet,\nthe first real-world read", s.BLUE),
          ("2026-09-20", "Range day", "6 sheets, 4 cartridges, 59 photos:\nscans vs photos, holes between bulls,\nprimer comparison", s.ORANGE),
          ("2026-09-23", "Aim point test", "Nine aim points through\nfour scopes at 100 yards", s.AQUA)]
fig, ax = plt.subplots(figsize=(11, 3.6))
ax.axis("off"); ax.set_xlim(-0.5, len(events) - 0.5); ax.set_ylim(-1.6, 1.3)
ax.plot([-0.3, len(events) - 0.7], [0, 0], color=s.MUTED, lw=2)
for i, (d, t, desc, col) in enumerate(events):
    ax.scatter([i], [0], s=160, color=col, zorder=3, edgecolor=s.SURFACE, linewidth=2)
    ax.text(i, 0.35, d, ha="center", color=s.INK2, fontsize=10)
    ax.text(i, 0.7, t, ha="center", color=s.INK, fontsize=11.5, fontweight="bold")
    ax.text(i, -0.35, desc, ha="center", va="top", color=s.INK2, fontsize=9.5)
ax.set_title("GroupLab range and bench tests so far")
s.save(fig, os.path.join(here, "timeline.png"))
