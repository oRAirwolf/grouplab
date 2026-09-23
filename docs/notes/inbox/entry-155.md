# 2026-09-23, entry 155: one Targets screen, because the library and the print dialog do the same job

Alan: "Should the target library and print dialogue window be two separate screens? They seem to fill
the same role and should be merged. I like the target library layout where targets are grouped and
categorized."

Merge them. The library's grouped and categorised layout is the part he likes and it is the part that
survives.

## 1. The screen

One screen, named **Targets**. Two regions:

- **The library**, as it is today: grouped and categorised, with the description under each sheet
  saying which job and which distance it is for. This is the primary region and it keeps its layout.
- **The selected sheet's panel**: the preview, the print settings and the Print action. It fills with
  the selected sheet and shows nothing useful when nothing is selected.

Selecting a sheet fills the panel immediately. No second window opens at any point in the flow.

## 2. Nothing the print dialog does today may be lost

Enumerate what `PrintWindow` does before you start, and hold the merged screen to that list:

- paper size and orientation, and the refusal when the chosen paper cannot hold the sheet
- copies
- the no-scaling instruction to the PDF viewer
- the printed instruction along the bottom of the sheet, and the ruler check
- the scale check bar
- which bull set, and any other per-sheet option
- the printed markers and codes
- Save PDF as well as Print, and the Linux and macOS viewer path from question 31

Anything on that list that the merged screen cannot do is a regression, and the entry is not done.
Do not add a capability that does not exist today, such as printing several different sheets in one
job, unless it already exists.

## 3. The tour

`/tour/library/` and `/tour/print/` become one page. Redirect the old print address to it so no link
breaks, and note in `website/tour.json` that the two screens merged, because a reader who saw the old
tour will wonder. Entry 152 rewrites the text on both of these pages, so do entry 152 first and then
merge, not the other way round, or the corrected text will be written twice.

## 4. The screenshot job

The weekly screenshot job from entry 144 takes its screen list from the same source the tour does.
Update that list, so the merged screen is captured under one name and no job goes looking for a screen
that no longer exists.

## 5. Remove the old code

The old print dialog is deleted, not left unreachable. A window nothing opens is a window nobody
maintains and it will drift out of step with the one that replaced it.

## 6. Tests

1. The acceptance tests that covered the print dialog now cover the merged screen. Not deleted,
   repointed.
2. `NothingIsCutOffTests` covers the merged screen at both window sizes, since it is now denser than
   either screen was.
3. A test that no code path opens a separate print window.
