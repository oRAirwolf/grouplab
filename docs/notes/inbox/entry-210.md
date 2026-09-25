## 2026-09-25, entry 210: the composite plot, second pass: rings five times thicker and darker, and a whole target view

Alan, 2026-09-25, after entry 204: "The new analysis screen is much better, but it is still hard to read." Do this after 206 to 208.

## 1. The bull's rings

1. **About five times thicker.** `CompositePlot.BullStroke` is 4 today; make the rings about 20 at the default view. Better: give the
   ring's width in page units, so it stays in proportion as the view zooms (section 2), and pick the unit so it is about five times today's
   at the default zoom. Say which you chose.
2. **Darker**, so they are clearly told apart from the thin outlines drawn around the impacts: a solid mid gray rather than the light gray
   of entry 204, in both themes, still behind everything else in the draw order. The impacts' outlines stay thin, at their 50 percent
   opacity, so thickness and tone both separate them from the rings.
3. Check that nothing important disappears under a thick ring: the shot outlines, CEP circles, the extreme spread and the center lines are
   drawn on top, and a hole sitting on a ring must still be plainly visible. Look at the sample and at a group that straddles a ring.

## 2. Zoom out to the whole target

Today the plot frames the group only. Add a way to see the whole target:

1. **A toggle beside the plot: "Group" and "Whole target".** Group is today's framing; Whole target fits the entire bull, every ring, with
   the group inside it. The choice is remembered between sessions. Default stays Group.
2. **Free zoom and pan as well:** mouse wheel and trackpad pinch on the desktop, pinch and two finger drag on touch, and a double click or
   tap, or the toggle, to return to a fitted view. Rings, CEP circles and lines stay crisp at every zoom.
3. The saved and printed report follows the chosen framing, or offers both; say which.
4. Compare and anywhere else the composite plot appears get the same toggle.

## 3. Tests and pictures

Extend entry 204's headless render test: the ring stroke against the outline stroke, the ring tone darker than before and still distinct
from the outlines, and the two framings (the whole target view contains every ring; the group view matches today's). Then save before and
after pictures of the sample's plot in both framings and both themes under `docs/figures/` for Alan, and say in the report where they are.
