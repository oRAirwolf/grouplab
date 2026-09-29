## 2026-09-29, entry 289: the 2 MOA sheets: Alan chose C with 2.00 in bulls

Answers entry 279 section 4's DESIGN NEEDED (Unholy's request). Alan: "2 MOA: C, 2.00 in bulls".

The concepts are on the Design canvas "GroupLab 2 MOA sheets"; their sources are copied, local only, to `C:\Dev\grouplab-local\design-concepts\2moa-2026-09-29\` (`Main.dc.html` the single page drawn to scale in millimeters, `SetB.dc.html` the set of three, `Detail.dc.html` the reasons). Take the geometry from them; build the sheets as library definitions like every other sheet.

### 1. The page (both sheets use it)

- **Letter, portrait. Nine bulls, 3 by 3, on a 2.5 in (63.5 mm) grid**: the cell lattice 190.5 mm square, lines at x 12.7, 76.2, 139.7, 203.2 mm and y 40.0, 103.5, 167.0, 230.5 mm from the top left. Bull centers at x 44.45, 107.95, 171.45 and y 71.75, 135.25, 198.75 mm.
- **Why not 3 by 4** (Code's option A as first written): twelve 2 in bulls on a 2.5 in grid fill the page, and the first and last bulls overlap the corner QR codes by more than half an inch. Keep the codes where every sheet keeps them.
- **The bull:** 2.00 in outer ring (the family's "1 MOA" is the 1.00 in bull, so this is "2 MOA" in the same sense: 1.91 MOA at 100 yd, 1.75 at 100 m, stated so in the description and on the Features page), an inner ring at 1.00 in, a center dot, ring strokes as the 1 MOA sheets. Half an inch between neighboring bulls, as the family keeps.
- **Markers:** AprilTag 36h11 at every lattice intersection except the two top corners, which sit against the top codes: 14 markers. Check that 14 register photographs within the gates (the 5x5 has 34); if the lens fit needs more, add markers at the midpoints of the outer cell edges and say so.
- **Codes:** the four corner codes as on every sheet. **Title band** between the top codes; **load block** between the bottom codes (date, distance, rifle, caliber, load, notes) as a declared exclusion zone.
- **A4:** the same grid; it sits about 8 mm from the side edges. Check it against the printer check's margins and the renderer's safe area; if it does not clear them, say so rather than shrinking the bulls.
- **Bull styles:** plain first, then the C and E bulls, as the 1 MOA family has them, sized to the 2.00 in bull.

### 2. The two sheets (option C: both)

1. **The single page**: nine scoring bulls, 1 to 9. For a quick group or a zero check.
2. **The set of three**: the same page three times, tile indices 1 to 3, bulls numbered 1 to 25 across the set, and bulls 8 and 9 of the third page as sighters S1 and S2. For load development. Each page reads itself; S10 pools them by tile index, and a missing page is reported, as for the large format sets.

Names and identifiers follow the library's scheme; Code chooses them and says which in the results.

### 3. The rest, in the same change (rule c)

The library's list, the designer, Made for your optic if it lists families, the Features page with each sheet's own picture (entry 256), the tour's Targets screen if it lists sheets, the README's sheet list, `docs/TARGET-LIBRARY.md`, and the conformance tests (render and read back each sheet, both page sizes, all three styles). Credit Unholy for the request, as the credits allow.

### 4. Order

After entry 288 (the updater) and the entry 280 sitting. Report it in `for-alan.md` with a line for Unholy saying where to print them.
