# 2026-09-24, entry 169: the analysis screen, cut down to what a shooter reads

Alan's friend, the first outside user, went through the analysis screen on `Scan_20260923.png` and sent
a long, specific review with screenshots. His verdict: "All of this is just way too dense." He also said
the left side, the target picture with each shot's distance from its bull, "looks great as is". Keep the
left side exactly as it is. This entry is about the right hand panel and the plot.

Do entry 170 first; its defects are worse than the density.

## 1. What stays in view

He named what matters, and it matches what a shooter reads off any target:

1. **Center from aim**, windage and elevation.
2. **Extreme spread.**
3. **Group width by height.**
4. **Mean radius.**
5. **CEP**, 50 and 90.
6. The zero correction, per section 2.

Each shows its value, and its interval behind a tooltip or an expander rather than in a second line of
text. **Everything else goes into one collapsed section named Advanced**: sigma, the permutation tests,
stringing, the across and up and down strips, sessions over time, the flyer test, the pooled degrees of
freedom. None of it is deleted. It is out of the way until somebody opens it, and the application
remembers that they did.

This goes further than entry 163 section 5, which collapsed paragraphs but kept every section on the
screen. Where the two disagree, this entry wins.

## 2. The zero correction box

From his screenshot and comments:

1. **Offer the correction in MOA and in mil, side by side**, both always, whatever the default unit
   setting. Shooters work in both depending on the scope.
2. **The green "Dial ..." sentence repeats the numbers above it.** Replace the two lines and the sentence
   with one compact block: the offset in inches, MOA and mil, and beneath it the clicks for the scope
   the rifle profile names, with the click value stated, such as "2 clicks left at 0.1 mil".
3. **State the distance the correction is for**, in the block itself. Entry 170 section 1 explains why
   that is not cosmetic.
4. **The text below the box goes**: the pooled sigma line and the paragraph about carrying the correction
   move into Advanced or into "why".
5. **"Where it landed" is removed.** He called it basically useless, and it duplicates the main plot.

## 3. The plot: high contrast

He preferred the plot style of another program he uses, and was specific about why: "The best thing to
takeaway is the simplistic contrast ratio." Keep what GroupLab draws, and draw it that way:

1. Keep the CEP circles, the group center, and the line joining the two farthest shots.
2. Draw on white, with thin black or dark outlines at the hole size, one clear accent for the group
   center and the extreme spread line, and no faint pastel rings. Nothing thinner or lighter than can be
   read on a laptop in daylight.
3. A dark theme version that is equally high contrast, not a dimmed copy.
4. `ThemeTests` holds the plot's marks to a contrast ratio against their background, as it already does
   for the interface.

Do not reproduce the other program's layout or name it anywhere in this repository. The lesson is the
contrast, not the design.

## 4. Size and density

1. **Raise the default font size** on the analysis panel. The numbers are the product; they should be the
   largest text on the right hand side.
2. An option to **pop the analysis out into its own window**, for a second monitor. Lower priority than
   everything else in this entry; do it last or leave it for a later entry if it is large.

## 5. The registration badge

The header shows "registered, residual 0.004 in", and hovering shows a paragraph starting "From the
sheet's own printed markers: 38 of 38 markers found...". His reaction: "wtf does this info even mean? And
is it useful? Yeet this info."

1. The badge becomes plain: **"Scale checked"** with a check mark when registration succeeded, and a
   plain warning when it did not. That is the only thing a shooter needs from it.
2. The paragraph moves behind Show work, where somebody who wants it can find it. It also still carries
   the old calibre wording that entry 161 corrects, so check it after 161 has landed.

## 6. A back button

From the analysis screen, a person can return to marking only by clicking the image name in the
breadcrumb. Add a plain **Back** button, top left, that returns to the marking screen with everything as
it was left.

## 7. Right drag pans, and the shortcuts are visible

1. **Right button drag pans in every tool**, as middle button drag already does per entry 163. A right
   click without movement is left free for a context menu later.
2. **Holding Alt shows every button's shortcut** as a small label under it, and releasing Alt hides them.
   The keyboard strip along the top stays.

## 8. Export and import

"idk how useful the json file generated from an export is for any other program."

1. **Export shot coordinates as CSV**: one row per shot, with shot number, bull, x and y from the aim
   point in inches, and the same in MOA and mil at the stated distance, plus a header naming the units
   and the distance. That is what other tools and spreadsheets read. Keep the JSON export; it is the
   complete record and entry 165 uses it.
2. **Import shot coordinates from a CSV** exported by other target analysis software, with a column
   mapping step that asks which column is x, which is y and which unit they are in. Do not name or target
   any one program's format; a mapping step covers them all.

## 9. American spelling

"Center from aim (program spells it centre...)". The application and the site use British spelling:
centre, calibre, analyse, colour. GroupLab's users are overwhelmingly American, and its defaults are
already inches, yards and MOA. **Every string a user reads, in the application and on the site, uses
American spelling**: center, caliber, analyze, color. Code identifiers are not renamed; that is churn with
no reader. Tests that pin user facing text change with it. Add a test that fails on the British forms in
user facing strings, with an allowance for quoted material.

This is decided by the planning session on Alan's behalf; he can reverse it.
