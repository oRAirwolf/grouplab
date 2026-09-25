## 2026-09-25, entry 204: the composite plot is still too busy: Alan's changes

Alan, 2026-09-25, on the analysis screen's composite plot (`src/GroupLab.App/CompositePlot.cs`). He showed a reference plot from another
program; it is not to be named, copied or committed, and nothing below depends on it beyond the description here. In that plot the bull's
ring is one thick, light gray band, the shot outlines are thin and plain, and the two sets of center lines run the full width and height
of the plot, one black, one blue.

His words: "The composite group is still very busy and hard to read."

## 1. What to change

1. **Shot outlines at half their current opacity.** The caliber circles of the shots, and their points when no caliber is set, drawn at
   50 percent of today's opacity. Selected and excluded shots keep their own distinct look.
2. **CEP circles in their own color, and wider than the shot outlines.** CEP 50 and CEP 90 are no longer the shot ink: they are **green**
   (Alan: "Bring back the green color for the group center and the CEP circles") and drawn with a clearly thicker stroke than any shot
   outline. Keep them distinguishable from each other as well, by dash or weight, and say which in the key.
3. **Add CEP 95**, in the same green family, distinguishable from 50 and 90.
4. **Toggles.** CEP 50, CEP 90, CEP 95 and the extreme spread line each get an on and off toggle beside the plot, touch sized and
   keyboard reachable. Defaults: CEP 50 and CEP 90 on, CEP 95 off, extreme spread on. The choices are remembered between sessions. The key
   lists only what is shown.
5. **Extreme spread stays red.**
6. **The group center: green, as full length lines.** Instead of a small cross, one horizontal and one vertical line across the whole plot,
   through the group center, in green.
7. **The bull's center (the point of aim): full length lines too, in a second high contrast color** that cannot be confused with the green,
   the red, or the shot ink. Alan: "They should also be high contrast colors that are separate and easy to identify." Choose it for both
   the light and dark themes and check its contrast against the paper in each.
8. **The bull's rings: wider and low contrast.** Draw the composite bull's rings as a wider stroke in a light gray (dark theme: the
   equivalent low contrast gray), so they read as background and the shots, CEPs and lines read in front of them.

## 2. Constraints

- **Draw order**, back to front: rings, shot outlines, CEP circles, extreme spread, center lines, selection. Nothing important hidden
  under the rings.
- **Color vision.** Red and green alone must not be the only difference between the extreme spread and the group's marks. They already
  differ in shape (a line against circles and crosshairs), which is enough, but check the whole plot through a deuteranopia simulation and
  say what you checked.
- **Theme tokens.** New colors go in the palette the plot already reads (`inks`), for both themes, not as literals in the drawing code.
- **The saved and printed report** draws the plot the same way, with the same toggles as on screen, or the report says what it left out.
- **Compare** and any other place the composite plot is drawn follows the same rules.

## 3. Tests and the report

A headless render test that checks the stroke widths and opacities are in the order above, that the toggles add and remove exactly their
mark and key entry, and that the defaults are as listed. Then a before and after picture of the sample scan's composite plot, in both themes,
saved under `docs/figures/` for Alan to look at, and the report in plain words: what changed and which build it is in.
