# 2026-09-22, entry 140: a new image is a new target

Alan opened `20260920_165624.jpg` (a sheet with 15 shots, one on each of bulls 1 to 15) straight after working on a 25-shot sheet. GroupLab carried the last sheet's facts over. His screenshot shows:

- "Count differs from rounds fired: You fired 25 and 15 are marked. Nothing is marked on bulls 16, 17, 18 ..." The 25 was the previous sheet's rounds fired.
- **Every one of the 15 shots flagged "Possibly two holes"**, with sizes of 2.0 to 2.36 holes (shot 15: diameter 0.412 in, "2.36 holes"). A 6.5 mm hole reads as two holes when it is measured against a smaller calibre, which suggests the previous sheet's calibre was kept too. Confirm whether that is the cause.
- 16 review items on a sheet that has nothing wrong with it. That is the review queue crying wolf, which teaches people to ignore it.

## 1. What resets when an image is opened

1. **Everything that describes one sheet starts empty for a new image**: rounds fired, shots per bull, which bulls were aimed at and sight changes (question 37), the load on each bull, the calibre and its confirmation, the shot distance, the scale, every mark, the review queue, the undo history, excluded and flyer marks, notes, and anything else held per sheet. List every field you reset in the results, found by reading the state, not from memory, and add a test that fails if a new per-sheet field is added without being reset.
2. **Nothing from the previous sheet is used silently.** If something must carry over, it is offered, not applied.
3. **"Same setup as the last target"**: a clearly labelled button, shown after opening a new image when a previous sheet exists, that copies only the equipment and conditions: rifle, barrel, load, calibre and shot distance. Never counts, marks, aimed bulls or anything about where shots landed. What it copies is listed beside it so the person can see what they are accepting.
4. **Unsaved work**: if the current sheet has edits that are not saved as a session, opening another image first asks "Save this target, discard it, or cancel", never losing work and never keeping it attached to the new image.

## 2. A reset of one's own

Add **New target** (Ctrl+N) to the Open menu and to the toolbar area: it clears the current sheet exactly as opening a new image does, with the same save, discard or cancel question, and a toast with Undo (entry 131 section 9).

## 3. The "Possibly two holes" check

1. It must never use a calibre the person has not confirmed for this sheet. Without one, judge doubles against the other holes on the same sheet (a hole about twice the area of its neighbours), not against an assumed calibre.
2. If most holes on a sheet would be flagged, the assumption is wrong, not the holes: raise **one** item asking the person to confirm the calibre, instead of one item per shot.
3. Test it with generated sheets: 15 single holes of one calibre with no calibre stated raise no doubles; the same with a wrong smaller calibre stated raise one calibre question, not fifteen items; a real double among singles is still found.

## 4. Proof

Recreate Alan's case in a test: analyse a 25-shot generated sheet with a calibre and rounds fired set, then open a 15-shot generated sheet, and check that no count, calibre, distance, review item or mark from the first appears on the second. Then run `20260920_165624.jpg` from `C:\Dev\grouplab-range-2026-09-20\photos\` (read only, nothing committed) the same way and report its review queue before and after. A plain `Release-note:` trailer. Put this near the top of the queue: it affects every session Alan runs.
