"""The figure for 'Curled, angled and wrinkled paper': NOTES-FROM-PLANNING.md entry 238.

Reads ../data/angled-2026-09-27.csv, nineteen phone photographs of one scanned GroupLab sheet from straight down to 66 degrees off square,
each read by GroupLab and matched shot by shot against the sheet's 600 dpi scan, and draws two panels against the angle GroupLab measured:
the marks that are not shots and the shots missed, and how far the matched shots sit from the scan.
"""
import csv
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", ".."))
import _style as s  # noqa: E402
import matplotlib.pyplot as plt  # noqa: E402

here = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(here, "..", "data", "angled-2026-09-27.csv"), encoding="utf-8")))
angle = [float(r["degrees_off_square"]) for r in rows]
extra = [int(r["extra_marks"]) for r in rows]
missed = [int(r["shots_missed"]) for r in rows]
median = [float(r["median_from_scan_in"]) for r in rows]
worst = [float(r["worst_from_scan_in"]) for r in rows]
LIMIT = 37

s.apply()
fig, (top, bottom) = plt.subplots(2, 1, figsize=(8, 6.4), sharex=True)
for ax in (top, bottom):
    ax.axvspan(LIMIT, 70, color=s.GRID, zorder=0)
    ax.axvline(LIMIT, color=s.MUTED, lw=1)
top.plot(angle, extra, "o", color=s.ORANGE, label="marks that are not shots")
top.plot(angle, missed, "s", color=s.BLUE, label="shots missed")
top.set_ylabel("of 25 shots")
top.set_title("Photographs of one scanned sheet, read at each angle")
top.legend(loc="upper left")
top.text(LIMIT + 1, max(extra) * 0.85, "refused past 37 degrees", color=s.INK2, fontsize=9.5)
bottom.plot(angle, median, "o-", color=s.AQUA, label="median distance from the scan")
bottom.plot(angle, worst, "o", color=s.INK2, label="worst")
bottom.set_ylabel("inches")
bottom.set_xlabel("degrees off square, as GroupLab measured it")
bottom.set_xlim(0, 70)
bottom.set_ylim(0, 0.11)
bottom.legend(loc="upper left")
fig.tight_layout()
s.save(fig, os.path.join(here, "angle.png"))
s.save(fig, os.path.join(here, "lead.png"))
