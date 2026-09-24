# How the mobile application takes the photograph

NOTES-FROM-PLANNING.md entry 157. This is the contract the mobile work is held to, written before Android starts, as
`docs/PLATFORM-SUPPORT.md` plans it. Every requirement is numbered, and each names the test that holds it: a test that exists
already where the piece is built on the desktop, and the test the mobile work must write where it is not.

Section 4 is built now, on the desktop, because the desktop import path needs the same pieces and the mobile screen will call them.
Each of its parts is reported as what it measured, not as an assertion that it works.

## 1. What the capture screen does

**C1. It takes the picture itself.** The user frames the sheet and the shutter fires when every condition of C2 holds. A manual
shutter exists as an override and is never the primary path. *Test to write: `CaptureScreenTests.TheShutterFiresOnlyWhenEveryConditionHolds`.*

**C2. The conditions, all live on screen**, shown as one closing ring or a short checklist, never as six separate warnings:

| condition | what decides it | built |
|---|---|---|
| the whole sheet is inside the frame with margin on every side | the outline or the markers are found, and the outline does not touch the frame | `SheetOutline`, `CaptureTests.WhereThereIsNoSheetToFindItSaysWhy` |
| the sheet's edges or its printed markers are detected | `SheetOutline.Find`, or the markers read | `CaptureTests.ThePapersCornersAreFoundOnADarkBoard` |
| the off-axis angle is within the limit | `OffAxisLimit.Degrees`, section 4.2 | `CaptureTests.TheRefusalNamesTheAngleAndTheLimit` |
| the image is in focus | the quality score's focus part, section 5 | `CaptureTests.TheQualityScoreIsItsWeakestPart` |
| the exposure is within range, with no blown highlights on white paper | the exposure part, section 5 | the same |
| enough scale markings or codes are readable | the markings part, section 5 | the same |

**C3. Guidance is one instruction at a time**, in plain words, and never a number the user cannot act on. When more than one condition
fails, the first in this order is the one said: *move back* (the sheet runs out of the frame), *move closer* (the least resolution is
below the resolution part's useless level), *less angle* (beyond the limit), *hold steadier* (focus), *more light* or *less light*
(exposure), *flatten the paper* (the outline is not four straight sides). *Test to write:
`CaptureScreenTests.OnlyOneInstructionIsShownAtATime`.*

**C4. A visual outline** shows where the paper should sit, and it snaps to the detected sheet as it comes into position, so the user can
see that the application has found it. *Test to write: `CaptureScreenTests.TheOutlineSnapsToTheFoundSheet`.*

## 2. Light, lens and geometry

**L1. Flash.** Use it only when the metered light is below a threshold, and prefer the torch at low power to a burst: a burst makes hard
shadows, and question 38 established that shadow, not resolution, is what makes a hole in a photograph measure larger than the bullet.
Suppress it when it would put a specular reflection on glossy paper. Record whether it fired. *Test to write:
`CaptureScreenTests.TheFlashIsATorchAndIsRecorded`.*

**L2. Lens choice.** On a phone with several rear cameras, choose the longest focal length that still frames the sheet with margin, because
the wide and ultrawide lenses carry the most distortion. Say which lens was chosen, in the capture record and briefly on screen. *Test to
write: `CaptureScreenTests.TheLongestLensThatFramesTheSheetIsChosen`.*

**L3. Distortion.** Record the lens identity and the intrinsics the platform reports, and correct barrel or pincushion distortion before
measurement. Where the intrinsics are not available and the sheet is a GroupLab sheet, solve it from the printed markers: section 4.4.

**L4. Perspective.** Correct from the printed markers where they exist, and from the detected paper corners where they do not. Refuse
outright beyond the angle at which correction stops being trustworthy, and say what the angle was and what the limit is: section 4.2.

**L5. What is recorded with the image**: the lens, the focal length and its 35 mm equivalent, the intrinsics where reported, the measured
off-axis angle and the focal length it was measured with, the correction applied and the lens's radial terms, whether the flash or torch
fired, and the quality score with every part of it. `CaptureRecord` holds all of these except the flash, which the desktop cannot know, and
the marking file keeps it. **Never GPS, never location, never a published timestamp**; the record reads nothing but the lens tags and the
pixels. *Test: `CaptureTests.TheRecordIsKeptWithTheMarking`.*

## 3. Targets GroupLab did not print

Everything above applies except the marker based steps. The application still frames the sheet, checks focus and exposure, detects the
paper's edges, corrects perspective from them, chooses the lens and scores the result. **The scale is asked for afterwards rather than
read from the sheet**, which is what entry 152 settled: any target, once the scale is set. Where the paper's shape matches a standard size
the application offers that size and never assumes it, because Letter and A4 are 9 percent apart. If this section and entry 152 ever
disagree, entry 152 wins.

## 4. Built now, on the desktop, and what each measured

Two sets of material. A **synthetic sweep**, `grouplab capture-check --sweep`: GL-CF25-LTR rendered at 150 dpi on a dark board and
photographed through a known 4000 by 3000 camera of 26 mm equivalent, tilted from 0 to 75 degrees with the camera stepping back until the
sheet fits, so every true corner, angle and bull is known exactly. And **real photographs**, `grouplab capture-check <images>`: the 59 of
the 2026-09-20 range day, 15 of them paired with their sheet's 600 dpi scan in entry 130, and the 26 owner photographs, which have no
ground truth and are used only where none is needed, as entry 172 section 2.2 allows.

### 4.1 The paper's corners, and perspective from them

`SheetOutline.Find` splits the photograph by Otsu's threshold, takes the largest light region, and when that is not a sheet splits it
again, up to three times, so white paper can be told from a light board behind it. The region's convex hull gives four corners; each side
is fitted as a line through the boundary between them and refined at full resolution to the strongest edge across it. It refuses, saying
which, when nothing stands out, when the region runs out of the frame, and when the boundary is not four straight sides.

| synthetic tilt, degrees | 0 | 10 | 20 | 30 | 40 | 45 | 50 | 60 | 70 | 75 |
|---|---|---|---|---|---|---|---|---|---|---|
| worst corner error, pixels | 0.06 | 0.08 | 0.08 | 0.09 | 0.13 | 0.28 | 0.12 | 0.11 | 0.20 | not found |
| angle from the corners, degrees | 0.00 | 10.00 | 20.00 | 30.00 | 40.00 | 45.00 | 50.00 | 60.00 | 70.00 | |

**On real photographs it rarely has a sheet to find, and that is the measurement.** Of 85 photographs, most are close ups where the sheet
runs past the frame, which it refuses correctly. The one owner photograph with a whole sheet on a darker mat is outlined with its corners
on the paper's (`CaptureTests.ARealSheetOnAMatIsOutlined`). **On Alan's white backer board it cannot separate the paper from the board**:
the range frames with a whole sheet in view, commercial and GroupLab sheets alike, are all refused, because the light across the sheet varies more than the paper and the
board differ, so no threshold divides them; an edge based search was tried and lost the paper's faint edges among the printed lines and the
board's ribs. One frame of the whole board with four small sheets on it outlined the board itself. So: **a sheet photographed against a
darker background is found; against a white board, a person taps the four corners**, as before. The capture screen's C3 guidance gains
*put something darker behind the sheet* for a sheet it cannot find.

On the desktop this is the **Find the paper's edges** button beside the rectangle scale tool: it places the four corners as if tapped,
still draggable, and offers a standard paper size where the camera's focal length gives the shape and it matches one within 3 percent.

### 4.2 The off-axis angle, its limit, and the refusal

`CameraGeometry` reads the angle from the page-to-image homography: K^-1 H's first two columns are the page's axes as the camera sees
them, their cross product is the sheet's normal, and its angle to the camera's axis is how far off square the photograph is. The focal
length comes from the file's 35 mm equivalent. Without one it is solved from the sheet by the whiteboard method of Zhang and He, but only
once the sheet is 20 degrees or more off square, and a typical 26 mm equivalent is taken otherwise.

- **Synthetic:** the markers give the angle to within 0.05 degrees from 0 to 65, and the corners to within 0.01 with the camera's focal
  length. Tilted about one of the sheet's own axes, as the sweep is, the outline alone cannot give the focal length, because one vanishing
  point is at infinity; the markers' true lengths give it exactly, and a camera turned about both axes lets the outline give it too
  (`CaptureTests.TheFocalLengthIsSolvedWhereTheSheetSaysIt`).
- **Real:** 31 of the 59 range photographs registered, from 3.1 to 35.1 degrees off square. Solved without the file's focal length, the
  angle came within 2 degrees of the camera's own above 20 degrees, and was wrong by up to 28 degrees below it, which is where the
  20 degree rule comes from.

**What the angle costs**, on the twelve fully framed photographs measured against their scans, inches on the page:

| off square, degrees | photographs | bull center, median | bull center, worst | hole position, median |
|---|---|---|---|---|
| 3 to 12 | 6 | 0.0024 to 0.0076 | 0.009 to 0.035 | 0.025 to 0.035 |
| 27 to 32 | 6 | 0.0088 to 0.0138 | 0.022 to 0.065 | 0.023 to 0.045 |

The bull centers are about three times worse at 30 degrees than square on; the hole positions are not measurably worse, because the
holes' own error is larger. And in the synthetic sweep, with no blur and no lens, the markers keep every bull within 0.001 in to 60 degrees,
lose a quarter of their markers by 50 and three quarters by 65, and cannot register at 70.

**The limit is 40 degrees.** Every real photograph up to 35 degrees registered, and the steepest measured against its scan, 32 degrees,
kept the squarest photographs' hole error; nothing real has been measured beyond 35. The ideal case breaks at 65. The limit sits above
what the real photographs showed working and well inside where the ideal case fails, and question 54 asks planning to read it. Photographs
of a scanned sheet at 40 to 60 degrees would move it with evidence, and request 18 asks for them.

The refusal names both numbers: "This photograph was taken 52 degrees off square to the sheet, and GroupLab corrects up to 40 degrees. Hold
the camera more squarely over the sheet and take it again." The automatic path gives it before anything is measured from the photograph
(`CaptureTests.TheAutomaticPathRefusesAPhotographTooFarOffSquare`), and Find the paper's edges gives it too.

### 4.3 The quality score

Section 5 says how it is computed. On the 31 range photographs that registered it reads **good on 10, usable on 6 and poor on 15**.
The poor are set by the angle on 7, all between 28 and 35 degrees off square, by the paper blown out on 6, and by markers missed on 2.
Among the blown out are the two photographs of scan 1's sheet that matched only 5 of its 14 holes. Focus set none of them: every photograph's blur read under 0.0013 in, so on this material the focus part never
told a photograph apart, and whether its levels are right is untested (question 54).

### 4.4 Lens distortion solved from the printed markers

This was built in Phase 0: `LensFit` fits a homography with two radial terms to the markers' corners by Levenberg-Marquardt, the
distortion centered on the image, and the automatic path uses it on every photograph. What it is worth, on the same twelve photographs
registered once with it and once with a plain homography, inches on the page:

| | with the lens fitted | plain homography |
|---|---|---|
| worst bull center, median of the twelve | 0.029 | 0.054 |
| photographs where it is the better | 11 of 12 | |
| hole position, median of the twelve | 0.030 | 0.031 |
| photographs where it is the better | 10 of 12 | |

It halves the worst bull's error and helps the holes a little. The phone's reported intrinsics, L3, would let a sheet with no markers be
corrected too; the desktop never has them.

## 5. The quality score, to recompute by hand

One number from 0 to 100 kept with every photograph, so later analysis can weight or exclude poor photographs and a support conversation
can tell a poor photograph from a poor detection. A person is shown words, never the number: **good** at 70 and above, **usable** at 40 and
above, **poor** below.

**Each part is between 0 and 1**, linear between the level where it is perfect and the level where it is worthless, and clamped. **The
score is 100 times the least of the parts, rounded half away from zero**: a photograph is as good as its weakest part. A part that cannot
be measured, the markings on a sheet with none, takes no part.

1. **The sheet, rectified.** The page is resampled bilinearly from the photograph at the least of its own resolution and 200 pixels an
   inch, leaving 3 percent of the shorter side off every edge.
2. **Focus.** On the rectified sheet, the slope at every pixel is the central difference, and pixels with a slope of 12 levels a pixel or
   more are edges. Of those, the strongest fiftieth, and at least 50, are kept. At each, the contrast A is the highest level less the
   lowest in the 9 by 9 pixels around it, and the blur is sigma = A / (slope times the root of 2 pi). The median sigma, less 0.798 pixel in
   quadrature, the floor a central difference puts under a perfect step, divided by the rectified resolution, is the blur in inches.
   **Perfect at 0.004 in, worthless at 0.015.**
3. **Exposure.** Otsu's threshold on the rectified sheet; the pixels above it are paper. Two figures: the share of the paper at 250 or
   above, **perfect at 2 percent and worthless at 20**; and the paper's median level, **perfect at 140 and worthless at 70**. The part is
   the lesser.
4. **Angle.** The off-axis angle of section 4.2: **perfect at 10 degrees, worthless at the limit, 40.**
5. **Resolution.** The fewest image pixels an inch of the sheet gets: the smaller singular value of the page-to-image homography's
   derivative, taken at the four corners and the center. **Perfect at 150, worthless at 50.**
6. **Markings.** The markers read over the markers printed. **Perfect at 90 percent, worthless at 50.**

**On the 2026-09-20 range photographs that registered**, the words and what set them are in section 4.3.
