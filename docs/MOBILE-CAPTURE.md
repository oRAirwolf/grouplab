# How the mobile application takes the photograph

NOTES-FROM-PLANNING.md entry 157. This is the contract the mobile work is held to, written before Android starts, as
`docs/PLATFORM-SUPPORT.md` plans it. Every requirement is numbered, and each names the test that holds it: a test that exists
already where the piece is built on the desktop, and the test the mobile work must write where it is not.

Section 4 is built now, on the desktop, because the desktop import path needs the same pieces and the mobile screen will call them.
Each of its parts is reported as what it measured, not as an assertion that it works.

## 1. What the capture screen does

**C1. Guided and Manual (entry 260).** In **Guided**, the default, the user frames the sheet and the shutter fires by itself after three
ready frames in a row, when every condition of C2 holds; the shutter can be pressed sooner. In **Manual** it never fires by itself: the
user frames it and presses, and the guidance still shows, as a hint that never blocks. The mode is chosen under the shutter and
remembered. Both check every picture afterwards (section 6). `CaptureScreenTests.TheShutterFiresOnlyWhenEveryConditionHolds`.

**C0. Everything on the capture screen can be seen over the live camera (entry 260).** On 2026-09-28 the Fold 7's capture screen showed
only the camera: the instruction, Take and Back were Avalonia controls laid over CameraX's preview, and a native view hosted in an Avalonia
screen is drawn above whatever Avalonia draws in its place. The screen is now Android's own views around the preview (`CaptureScreen`):
at the top a floating panel with a round Back, the instruction, the torch (Auto, On, Off), the live checks (focus, light, tags read of
how many, QR codes read of how many, the torch) and the quality bar's forecast; a level near the bottom of the camera; under the camera, not
over it, the shutter, the photo picker to its left and the lens to its right, and GUIDED and MANUAL beneath. The app's bar along the
bottom is hidden while the camera shows, and Android's back closes the camera. `scripts/device-capture-check.py` opens GroupLab Dev's
camera over adb and fails unless the instruction, the shutter and Back are on the screen, inside it and at least 44 pixels, in both modes.

**C2. The conditions, all live on screen**, shown as one closing ring or a short checklist, never as six separate warnings:

| condition | what decides it | built |
|---|---|---|
| the whole sheet is inside the frame | once registered, the sheet's four corners, from its markers, fall inside the frame; before that, the outline does not touch the frame (entry 260: a white sheet on an off-white counter has no outline, and was told to move back) | `PictureCheckTests.ASheetOnAnOffWhiteCounterIsNotToldToMoveBack` |
| the sheet's edges or its printed markers are detected | `SheetOutline.Find`, or the markers read | `CaptureTests.ThePapersCornersAreFoundOnADarkBoard` |
| the off-axis angle is within the limit | `OffAxisLimit.Degrees`, section 4.2 | `CaptureTests.TheRefusalNamesTheAngleAndTheLimit` |
| the image is in focus | the quality score's focus part, section 5 | `CaptureTests.TheQualityScoreIsItsWeakestPart` |
| the exposure is within range, with no blown highlights on white paper | the exposure part, section 5 | the same |
| enough scale markings or codes are readable | the markings part, section 5 | the same |

**C3. Guidance is one instruction at a time**, in plain words, and never a number the user cannot act on. When more than one condition
fails, the first in this order is the one said: *move back* (the sheet runs out of the frame), *move closer* (the least resolution is
below the resolution part's useless level), *less angle* (beyond the limit), *hold steadier* (focus), *more light* or *less light*
(exposure). Paper that is not four straight sides no longer holds the shutter (entry 260): curl is followed by the registration and
reported afterwards. `CaptureScreenTests.OnlyOneInstructionIsShownAtATime`.

**C3a. Knowing the sheet from a live frame (entry 260).** The analysis stream is asked for 1920 by 1440, not CameraX's 640 by 480, at
which a code's module is about a pixel. The codes are tried first; failing them, `LiveSheet.ByLayout` fits each library sheet's marker
layout to the markers found, and the best fit guides the camera; the picture itself is identified from its codes afterwards. Markers too
small to read the codes from mean *move closer*.

**C4. A visual outline** shows where the paper should sit, and it snaps to the detected sheet as it comes into position, so the user can
see that the application has found it. *Test to write: `CaptureScreenTests.TheOutlineSnapsToTheFoundSheet`.*

**C5. Any way up.** The capture screen works whichever of the four ways the phone or tablet is held, upside down portrait included (a
phone on a bench or a tripod mount often is, and a foldable's inner screen is near square), while honoring the person's rotation lock:
the activity asks for `FullUser`, not `FullSensor`, which ignores the lock (entry 205). The photograph is stored the right way up however
the device was held, from the camera's own rotation of the frame and never from a file's tags that other software may drop, and the
outline and guidance turn with the screen. *Test to write: `CaptureScreenTests.APhotographTakenUpsideDownIsStoredUpright`, and the same
for the outline's corners at each of the four rotations.*

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

**The limit is 37 degrees** (entry 238; it was 40). Every real photograph up to 35 degrees registered, and the steepest measured against
its scan, 32 degrees, kept the squarest photographs' hole error. Then nineteen photographs of the scanned Dominus K sheet, from straight down
to 66 degrees in dim room light, measured each against the scan: up to 36.2 degrees every one found all 25 shots, 0.013 to 0.019 in from
the scan at the median, with one extra mark in ten; from 38.5 degrees marks that are not holes appeared (5 at 38.5, 2 at 41, 2 at 46 with
2 shots missed, 6 at 47.5, 17 at 50.4) and from 36 degrees the codes were missed on six of the ten. The positions themselves held to about 0.03 in almost to
60 degrees; what fails first is telling a hole from a mark. The limit sits between the last photograph that agreed and the first that did
not.

The refusal names both numbers: "This photograph was taken 52 degrees off square to the sheet, and GroupLab corrects up to 37 degrees. Hold
the camera more squarely over the sheet and take it again." The automatic path gives it before anything is measured from the photograph
(`CaptureTests.TheAutomaticPathRefusesAPhotographTooFarOffSquare`), and Find the paper's edges gives it too.

### 4.3 The quality score

Section 5 says how it is computed. On the 31 range photographs that registered it reads **good on 10, usable on 6 and poor on 15**.
The poor are set by the angle on 7, all between 28 and 35 degrees off square, by the paper blown out on 6, and by markers missed on 2.
Among the blown out are the two photographs of scan 1's sheet that matched only 5 of its 14 holes. Focus set none of them: every photograph's blur read under 0.0013 in, so on this material the focus part never
told a photograph apart, and whether its levels are right is untested. **The levels are judgment**, kept as they are by entry 187 section 2, and every part of every score is sent with a target so real submissions can tune them.

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
4. **Angle.** The off-axis angle of section 4.2: **perfect at 10 degrees, worthless at the limit, 37.**
5. **Resolution.** The fewest image pixels an inch of the sheet gets: the smaller singular value of the page-to-image homography's
   derivative, taken at the four corners and the center. **Perfect at 150, worthless at 50.**
6. **Markings.** The markers read over the markers printed. **Perfect at 90 percent, worthless at 50.**

**On the 2026-09-20 range photographs that registered**, the words and what set them are in section 4.3.

## 6. The check after every picture, and its bar (entry 260)

Alan: "I want the app to want good pictures but it should be able to handle less than ideal pictures." Every picture, taken in Guided
or Manual or chosen from the phone's files, is checked after it is analysed (`PictureCheck.Of`) and shown before its result (Feedback
B): the photograph with each note's place outlined and numbered, the verdict, the bar, the notes, what was fine, and "Take it again"
beside "Use this picture". Entry 260 replaces section 5's rule that the number is never shown: the number and the band word are always
written beside the bar.

1. **The score** is section 5's, the weakest of its parts, with a sixth: **the evenness of the light**, the dimmest bull's paper over the
   brightest. A bull's paper is the median of eight samples at 1.2 times its outer disc's radius, each the brightest pixel within two.
   **Perfect at 0.85, worthless at 0.40.**
2. **The band agrees with the decision.** GroupLab asks for a retake only where it cannot measure: the codes name no sheet, the
   markers do not register, the sheet is past the angle limit, the focus part is 0 (blur of 0.015 in or more), or the resolution part is
   0 (under 50 pixels an inch). A picture it can measure never scores below 40; one it cannot never scores 40 or above. So the bar's red
   band (under 40) means take it again, amber (40 to 69) usable, green (70 and above) good.
3. **The verdict** is Retake, Good (green with no notes), or Good with notes. The lead for amber is "Good enough to measure. Here is what
   would make the next one better:".
4. **The notes**, numbered, say what GroupLab corrected where it did: a bull's paper under 80 percent of the median bull's is in shadow,
   "A shadow falls across bulls 21 to 25, evened out: check those 5 holes if you like"; off square beyond 10 degrees; resolution under
   150 pixels an inch; soft focus; dim or washed-out paper; markers not all read. **What was fine** lists sharp focus, even light, tags
   and codes read of how many, and whether the torch was on.
5. **Live.** The capture screen's bar is the same score for the frame in view, placed in the band the picture would get
   (`PictureCheck.Forecast`).
6. **Not yet measured:** whether the score agrees with how well each picture actually measured across the test photographs, and the
   tolerance conditions (hard and soft shadows, a hand's and a phone's shadow, dim, warm and mixed light, curl and wave, blur, noise,
   JPEG). Entry 260 asks for both; they are the next part of it.

`PictureCheckTests`: a clean sheet is green; a shadow across the bottom row is named, "21 to 25", and stays out of the red; an unread
sheet is a red retake.
