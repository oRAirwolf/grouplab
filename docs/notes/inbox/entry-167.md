# 2026-09-24, entry 167: replace the Equipment icon

Alan: "The equipment icon is cursed and needs to be replaced. Was it supposed to be a rifle? If so, it is
bad and looks like a lego rpg."

It was meant to be a rifle, per its own summary in `src/GroupLab.App/Theme/Icons.cs`, and he is right
about how it reads. At 16 pixels a rifle's long thin shape leaves no room for proportion, so it comes out
as blocks. The planning session rendered the current icon and three replacements at 16, 32 and 128
pixels and in the rail beside the other destinations. A riflescope read as a bow tie at small sizes, and
a scoped rifle was still toy like. **A cartridge read cleanly at every size**, and Alan chose it tilted, and sits at the same visual
weight as Library, Print, Records, Ballistics and Reports. Alan has the comparison.

## 1. The new icon

Alan chose the **tilted** cartridge. The first tilted render clipped its tip and rim at the corners of
the square, so the planning session scaled it to 97 percent about the centre before rotating it 40
degrees, and baked the result into plain coordinates. Every point, including the curve control points,
now sits between 0.9 and 15.6, so nothing clips. Alan has the final render in both themes.

Replace `Icons.Equipment` with this path, on the same 16 pixel square, filled in the foreground colour
like every other icon:

    M12.68,2.43 C12.94,4.3 12.34,5.69 11.47,6.73 L8.65,4.36 C9.52,3.32 10.79,2.49 12.68,2.43 Z
    M8.2,4.75 L11.17,7.24 L10.42,8.13 L10.24,10.01 L6.69,14.24 L5.87,14.31 L5.94,15.13 L5.56,15.58
    L0.96,11.71 L1.33,11.27 L2.15,11.2 L2.08,10.38 L5.63,6.14 L7.45,5.64 Z

The first subpath is the bullet and the second is the case: neck, shoulder, body, extractor groove and
rim. The thin gap between them is what makes it read as a loaded cartridge rather than a bottle, so keep
it. Do not apply a rotation transform at run time instead; the baked path is what was checked for
clipping.

Update the summary comment to say what it now is and why: a cartridge, tilted, for the screen that holds
rifles, barrels and loads, replacing a rifle outline that could not hold its proportions at 16 pixels.

## 2. Check it where it lives

1. Render it at 16 and 32 pixels in the rail, in both themes, and look at it beside the other
   destinations. `ThemeTests` already holds contrast; make sure it covers this icon.
2. The selected state, in the accent colour, reads as clearly as the unselected one.
3. The weekly screenshot job and the tour will pick it up on their own. Confirm the Equipment tour page
   shows the new icon after the next run.

## 3. The general lesson

Icons that depict a long object at 16 pixels will always look like this. If another icon is ever needed
for a rifle, a barrel or anything long and thin, draw a detail of it rather than the whole thing, and
render it at 16 pixels before committing.

Priority: small and self contained. Do it whenever it fits between larger entries.
