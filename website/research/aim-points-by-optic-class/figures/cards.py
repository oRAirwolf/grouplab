# NOTES-FROM-PLANNING.md entries 412 and 414 section 5: the aim point cards for the classes not yet scored (request 86), the red dots
# and the prism at 25 and 50 yards and the medium power scopes at 100. The plan in aim-points-by-optic-class.md, item 1: each design is
# drawn in angular units and printed for its distance, so it looks the same through the scope at any of them. The nine designs are those
# of can-you-see-the-bull/figures/card.py, which drew them for 100 yards; here each is scaled by the distance over 100. The 1x cards add
# the article's rings for a red dot, at 2, 3, 4 and 6 times a 2 MOA dot, sized for their distance. Each card's last page is its score
# sheet, listing the optics to score at that distance. Nothing here is analyzed by GroupLab: the cards have no markers.
#
# Usage: python cards.py <output folder>. Needs reportlab. Writes one PDF a distance; with no folder it writes nothing.
from pathlib import Path
import math
import sys

from reportlab.lib.pagesizes import letter
from reportlab.lib.units import inch
from reportlab.pdfgen import canvas

W, H = letter
K, WT = (0, 0, 0), (1, 1, 1)
MM = 1 / 25.4
# One minute of angle at 100 yards, in inches.
MOA_100 = 1.047

# The optics request 86 asks for, by distance: (name, magnifications to score).
OPTICS = {
    25: [("Sig Sauer ROMEO 5 (2 MOA dot)", ["1x"]), ("Holosun SCRS MRS, red", ["1x"]), ("Holosun SCRS MRS, green", ["1x"]),
         ("Primary Arms SLx 1x MicroPrism", ["1x"])],
    50: [("Sig Sauer ROMEO 5 (2 MOA dot)", ["1x"]), ("Holosun SCRS MRS, red", ["1x"]), ("Holosun SCRS MRS, green", ["1x"]),
         ("Primary Arms SLx 1x MicroPrism", ["1x"])],
    100: [("Vortex Venom 3-15x44", ["4x", "6x", "8x", "10x", "15x"]), ("Nightforce NX8 2.5-20x50", ["4x", "6x", "8x", "10x", "15x", "20x"])],
}
RINGS = [2, 3, 4, 6]


class Card:
    def __init__(self, c, scale):
        self.c, self.s = c, scale

    def disc(self, x, y, d, col):
        self.c.setFillColorRGB(*col)
        self.c.circle(x, y, d * self.s / 2 * inch, stroke=0, fill=1)

    def poly(self, pts, col):
        self.c.setFillColorRGB(*col)
        p = self.c.beginPath()
        p.moveTo(*pts[0])
        for q in pts[1:]:
            p.lineTo(*q)
        p.close()
        self.c.drawPath(p, stroke=0, fill=1)

    def sq(self, x, y, side, col, rot=False):
        h = side * self.s / 2 * inch
        if rot:
            self.poly([(x, y + h), (x + h, y), (x, y - h), (x - h, y)], col)
        else:
            self.c.setFillColorRGB(*col)
            self.c.rect(x - h, y - h, 2 * h, 2 * h, stroke=0, fill=1)

    def rect(self, x, y, w, h, col):
        self.c.setFillColorRGB(*col)
        self.c.rect(x - w * self.s / 2 * inch, y - h * self.s / 2 * inch, w * self.s * inch, h * self.s * inch, stroke=0, fill=1)

    # The nine designs of card.py, in inches at 100 yards.
    def A(self, x, y):
        for d, col in [(25.4, K), (23.8, WT), (12.7, K), (11.5, WT), (2.5, K)]:
            self.disc(x, y, d * MM, col)

    def B(self, x, y):
        for d, col in [(1.0, K), (0.84, WT), (0.55, K), (0.39, WT), (0.22, K)]:
            self.disc(x, y, d, col)

    def C(self, x, y):
        self.sq(x, y, 1.25, K, True)
        self.sq(x, y, 0.36, WT, True)

    def D(self, x, y):
        self.sq(x, y, 1.0, K)
        self.rect(x, y, 1.0, 0.06, WT)
        self.rect(x, y, 0.06, 1.0, WT)

    def E(self, x, y):
        self.sq(x, y, 1.0, K)
        self.sq(x, y, 0.36, WT)
        self.disc(x, y, 0.1, K)

    def F(self, x, y):
        self.sq(x, y, 1.0, K)
        self.sq(x, y, 0.8, WT)
        self.disc(x, y, 0.2, K)

    def G(self, x, y):
        for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1)]:
            cx, cy = x + dx * 0.4 * self.s * inch, y + dy * 0.4 * self.s * inch
            self.rect(cx, cy, 0.4 if dx else 0.1, 0.1 if dx else 0.4, K)

    def Hh(self, x, y):
        for a in range(4):
            t = a * math.pi / 2
            tip = (x + 0.2 * self.s * inch * math.cos(t), y + 0.2 * self.s * inch * math.sin(t))
            bc = (x + 0.65 * self.s * inch * math.cos(t), y + 0.65 * self.s * inch * math.sin(t))
            px, py = -math.sin(t) * 0.22 * self.s * inch, math.cos(t) * 0.22 * self.s * inch
            self.poly([tip, (bc[0] + px, bc[1] + py), (bc[0] - px, bc[1] - py)], K)

    def I(self, x, y):
        self.disc(x, y, 2.0, K)
        self.disc(x, y, 0.6, WT)
        self.disc(x, y, 0.15, K)


DESIGNS = [("A", "Current GroupLab bull (control)", "A"), ("B", "Same bull, bold lines", "B"), ("C", "Black diamond, white center", "C"),
           ("D", "Black square, white cross", "D"), ("E", "Black square, white center", "E"), ("F", "Square outline, center dot", "F"),
           ("G", "Open cross, no center", "G"), ("H", "Four pointers", "Hh"), ("I", "Large bull", "I")]


def check_bars(c):
    c.setFillColorRGB(0, 0, 0)
    c.rect(0.5 * inch, 0.45 * inch, 2 * inch, 0.08 * inch, stroke=0, fill=1)
    c.setFont("Helvetica", 8)
    c.drawString(2.6 * inch, 0.46 * inch, "2 in check bar")
    c.rect(4.0 * inch, 0.45 * inch, 50 * MM * inch, 0.08 * inch, stroke=0, fill=1)
    c.drawString(4.0 * inch + 50 * MM * inch + 0.1 * inch, 0.46 * inch, "50 mm check bar")


def heading(c, title, lines):
    c.setFillColorRGB(0, 0, 0)
    c.setFont("Helvetica-Bold", 14)
    c.drawString(0.5 * inch, H - 0.55 * inch, title)
    c.setFont("Helvetica", 8.5)
    for i, t in enumerate(lines):
        c.drawString(0.5 * inch, H - (0.8 + i * 0.15) * inch, t)


def designs_page(c, yards):
    card = Card(c, yards / 100)
    heading(c, f"GroupLab aim point card: {yards} yards", [
        "Print at Actual size / 100 percent (not Fit to page). Check the 2 in bar at the bottom measures 2 in.",
        f"Hang it at {yards} yards." + ("" if yards == 100 else f" Each design is the angle it is on the 100 yard card, so it is drawn at {yards} percent of that size."),
        "Score each letter for each optic on the score sheet: 0 cannot see the center, 1 can see it but cannot center on it, 2 center confidently.",
        "This card is for looking, not for GroupLab analysis: it has no markers."])
    colx, rowy = [1.5, 4.25, 7.0], [8.35, 5.35, 2.35]
    for n, (letter_name, words, fn) in enumerate(DESIGNS):
        x, y = colx[n % 3] * inch, rowy[n // 3] * inch
        getattr(card, fn)(x, y)
        c.setFillColorRGB(0, 0, 0)
        c.setFont("Helvetica-Bold", 20)
        c.drawString(x - 1.25 * inch, y + 1.05 * inch, letter_name)
        c.setFont("Helvetica-Bold", 8.5)
        c.drawCentredString(x, y - 1.22 * inch, words)
    check_bars(c)
    c.showPage()


def ring(c, x, y, diameter, line):
    c.setStrokeColorRGB(0, 0, 0)
    c.setLineWidth(line * inch)
    c.circle(x, y, (diameter - line) / 2 * inch, stroke=1, fill=0)


def rings_pages(c, yards):
    """The article's rings for a red dot: 2, 3, 4 and 6 times a 2 MOA dot, sized for the distance, each with a small center dot."""
    dot = 2 * MOA_100 * yards / 100
    line = max(0.04, 0.25 * MOA_100 * yards / 100)
    # As many rings to a page as fit between the heading and the check bars, each with its name under it.
    pages, page, room = [], [], 8.4
    for k in RINGS:
        need = k * dot + 0.6
        if page and need > room:
            pages.append(page)
            page, room = [], 8.4
        page.append((k, k * dot))
        room -= need
    pages.append(page)
    for page in pages:
        heading(c, f"GroupLab aim point rings for a red dot: {yards} yards", [
            "Print at Actual size / 100 percent. Each ring is a number of times a 2 MOA dot across, at this distance.",
            "Put the dot in the ring's middle: score each ring 0 cannot center, 1 can but not confidently, 2 centers confidently.",
            f"A 2 MOA dot covers {dot:.2f} in at {yards} yards."])
        top = H - 1.5 * inch
        for k, d in page:
            y = top - d / 2 * inch
            ring(c, W / 2, y, d, line)
            c.setFillColorRGB(0, 0, 0)
            c.circle(W / 2, y, line / 2 * inch, stroke=0, fill=1)
            c.setFont("Helvetica-Bold", 12)
            c.drawCentredString(W / 2, y - d / 2 * inch - 0.25 * inch, f"R{k}: {k} times the dot, {d:.2f} in")
            top = y - d / 2 * inch - 0.6 * inch
        check_bars(c)
        c.showPage()


def score_page(c, yards):
    with_rings = yards < 100
    cols = ["Optic", "Mag", "Light, mirage"] + list("ABCDEFGHI") + ([f"R{k}" for k in RINGS] if with_rings else [])
    first = 4.0 if with_rings else 4.4
    step = (8.0 - first) / (len(cols) - 3)
    xs = [0.5, 2.6, 3.05] + [first + step * k for k in range(len(cols) - 3)]
    c.setFont("Helvetica-Bold", 13)
    c.drawString(0.5 * inch, H - 0.55 * inch, f"Aim point score sheet, {yards} yards: 0 cannot see center, 1 see but not center, 2 center")
    c.setFont("Helvetica", 8.5)
    c.drawString(0.5 * inch, H - 0.78 * inch, "Score only at magnifications the optic has. A second observer, if one is with you, scores on the row under yours.")
    rows = []
    for name, mags in OPTICS[yards]:
        rows.append(("optic", name))
        for m in mags:
            rows.append(("score", m, "you"))
            rows.append(("score", m, "second observer"))
    rows.append(("optic", "Another optic:"))
    rows.extend([("score", "", "")] * 2)
    y0, rh = H - 1.0 * inch, (0.3 if len(rows) < 28 else 0.26) * inch
    c.setFont("Helvetica-Bold", 9)
    for k, name in enumerate(cols):
        c.drawString(xs[k] * inch + 3, y0 - rh + 9, name)
    c.setLineWidth(0.5)
    for r in range(len(rows) + 2):
        c.line(0.5 * inch, y0 - rh * r, 8.0 * inch, y0 - rh * r)
    for r, row in enumerate(rows):
        yy = y0 - rh * (r + 2)
        if row[0] == "optic":
            c.setFont("Helvetica-Bold", 9)
            c.drawString(0.5 * inch + 3, yy + 9, row[1])
        else:
            c.setFont("Helvetica", 9)
            c.drawString(0.5 * inch + 12, yy + 9, row[2])
            c.drawString(2.6 * inch + 3, yy + 9, row[1])
            for x in xs[1:]:
                c.line(x * inch, yy, x * inch, yy + rh)
            c.line(8.0 * inch, yy, 8.0 * inch, yy + rh)
    for x in [0.5, 8.0]:
        c.line(x * inch, y0, x * inch, y0 - rh * (len(rows) + 1))
    yy = y0 - rh * (len(rows) + 1) - 0.35 * inch
    c.setFont("Helvetica", 9)
    c.drawString(0.5 * inch, yy, "Also note: the time, the sun's direction, and which letter you would choose to shoot load development on.")
    c.drawString(0.5 * inch, yy - 0.25 * inch, "Notes:")
    c.showPage()


def main(folder):
    out = Path(folder)
    out.mkdir(parents=True, exist_ok=True)
    names = {25: "aim point card 25 yd, red dots and prism", 50: "aim point card 50 yd, red dots and prism", 100: "aim point card 100 yd, medium power scopes"}
    pages = {}
    for yards in (25, 50, 100):
        path = out / f"{names[yards]}.pdf"
        c = canvas.Canvas(str(path), pagesize=letter, invariant=1)
        c.setTitle(f"GroupLab aim point card, {yards} yards")
        designs_page(c, yards)
        if yards < 100:
            rings_pages(c, yards)
        score_page(c, yards)
        c.save()
        pages[yards] = path
    return pages


if __name__ == "__main__":
    # The site build runs every figure script with no argument; the cards are for printing, not for a page, so then it draws nothing.
    if len(sys.argv) < 2:
        print("cards.py <output folder>: the aim point cards are printed for a range kit, not drawn for the site")
        sys.exit(0)
    for yards, path in main(sys.argv[1]).items():
        print(yards, path)
