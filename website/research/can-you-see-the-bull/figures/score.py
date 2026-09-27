# Entry 143 section 1.2. This wrote to /mnt/user-data/outputs, which is a folder in the session that first
# ran it and exists on no machine this repository is checked out on. It failed on every build here and the
# failure was hidden, because reportlab is not installed on the runner either, so the build reported a
# missing package and moved on. It writes beside itself now, like every other script in the research folder.
#
# Entries 226 section 3 and 229 section 5: the rows come from scopes.py, which refuses a magnification a scope does
# not have and one asked for twice, and a change of distance is a bold heading of its own. The card's page 2 is this
# same sheet, drawn by draw_score_sheet, so there is one score sheet and not two. American spelling throughout.
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from reportlab.lib.pagesizes import letter  # noqa: E402
from reportlab.pdfgen import canvas  # noqa: E402
from reportlab.lib.units import inch  # noqa: E402
import scopes  # noqa: E402

W, H = letter


def draw_score_sheet(c):
    c.setFont("Helvetica-Bold", 13)
    c.drawString(0.5 * inch, H - 0.55 * inch, "Aim point score sheet: 0 cannot see center, 1 see but not center, 2 center confidently")
    c.setFont("Helvetica", 8.5)
    c.drawString(0.5 * inch, H - 0.78 * inch,
                 "Card at 100 yd unless a row says otherwise. Score only at magnifications your scope has; 25x on all three high power scopes")
    c.drawString(0.5 * inch, H - 0.93 * inch, "is the like-for-like glass comparison.")
    cols = ["Scope", "Mag", "Light / mirage"] + list("ABCDEFGHI")
    xs = [0.5, 2.3, 2.8, 4.4] + [4.4 + 0.4 * (k + 1) for k in range(8)]
    y0 = H - 1.1 * inch
    rh = 0.31 * inch
    c.setFont("Helvetica-Bold", 9)
    for k, cn in enumerate(cols):
        c.drawString(xs[k] * inch + 3, y0 - rh + 9, cn)
    table = scopes.rows()
    n = len(table) + 1
    c.setLineWidth(0.5)
    c.line(0.5 * inch, y0, 8.0 * inch, y0)
    for r in range(1, n + 1):
        yy = y0 - rh * r
        opening = r < n and table[r - 1][0] in ("scope", "distance")
        c.setLineWidth(1.2 if opening else 0.5)
        c.line(0.5 * inch, yy, 8.0 * inch, yy)
    c.setLineWidth(0.5)
    for r, row in enumerate(table):
        yy = y0 - rh * (r + 2)
        if row[0] == "scope":
            c.setFont("Helvetica-Bold", 9)
            c.drawString(0.5 * inch + 3, yy + 9, row[1])
        elif row[0] == "distance":
            c.setFont("Helvetica-Bold", 10)
            c.drawString(0.5 * inch + 3, yy + 9, f"At {row[1]} yd, not 100: move the card")
        elif row[0] == "score":
            c.setFont("Helvetica", 9)
            c.drawString(2.3 * inch + 3, yy + 9, f"{row[1]}x")
        else:
            c.setFont("Helvetica", 8)
            c.drawString(2.3 * inch + 3, yy + 9, "____x")
    for x in xs[:3] + [8.0]:
        c.line(x * inch, y0, x * inch, y0 - rh * n)
    # The letter columns only through the rows that are scored, so a heading row reads as a heading.
    for x in xs[3:]:
        for r, row in enumerate(table):
            if row[0] in ("score", "friend"):
                c.line(x * inch, y0 - rh * (r + 1), x * inch, y0 - rh * (r + 2))
        c.line(x * inch, y0, x * inch, y0 - rh)
    yy = y0 - rh * n - 0.35 * inch
    c.setFont("Helvetica-Bold", 10)
    c.drawString(0.5 * inch, yy, "Also note: time of day, sun direction, and which letter you would choose to shoot load development on.")
    c.setFont("Helvetica", 9)
    c.drawString(0.5 * inch, yy - 0.2 * inch, "If there is time: 3 shots at A and 3 at your favorite, same rifle and load. A first look, not a proof.")
    c.drawString(0.5 * inch, yy - 0.45 * inch, "Friend's scope make, model, magnification range, focal plane:")
    c.drawString(0.5 * inch, yy - 0.75 * inch, "Notes:")


if __name__ == "__main__":
    c = canvas.Canvas(str(HERE / "GroupLab-aim-point-score-sheet-v2.pdf"), pagesize=letter, invariant=1)
    c.setTitle("GroupLab aim point score sheet")
    draw_score_sheet(c)
    c.save()
