# 2026-09-23, entry 157: how the mobile application takes the photograph

Alan, on the mobile versions: the application should take the picture itself, the way a QR scanner, a
document scanner or a banking app photographing a cheque does. It should snap when the alignment is
right, use the flash if needed, read as many scale markings as it can, keep the picture in focus,
correct for angle or refuse the photograph if it is too far off axis, record the lens data, remove the
distortion computationally where it can, choose the lens, and guide the user with visual outlines and
instructions. It should help even when the target is not a GroupLab target.

**This entry is mostly a specification, and it is deliberately written before Android starts.** Android
is planned and is a high priority once the Windows application is working to the developer's liking,
per `docs/PLATFORM-SUPPORT.md`. Section 4 is the part to build now, because the desktop import path
needs the same pieces and building them now means the mobile work starts from working code rather than
from a document.

## 1. The specification document

Write `docs/MOBILE-CAPTURE.md`. It is the contract the mobile work is held to, and every item below is
a requirement in it, with a test named for each where a test is possible.

## 2. What the capture screen does

1. **It takes the picture itself.** The user frames the sheet and the shutter fires when every
   condition is met. A manual shutter exists as an override and is never the primary path.
2. **The conditions, all live on screen:**
   - the whole sheet is inside the frame with margin on every side
   - the sheet edges or the printed markers are detected
   - the off axis angle is within the limit
   - the image is in focus
   - the exposure is within range, with no blown highlights on white paper
   - enough scale markings or codes are readable
   Show them as one closing ring or a short checklist, not as six separate warnings.
3. **Guidance is one instruction at a time**, in plain words: move back, move closer, hold steadier,
   more light, less angle, flatten the paper. Never two at once, and never a number the user cannot
   act on.
4. **A visual outline** shows where the paper should sit, and it snaps to the detected sheet as it
   comes into position, so the user can see that the application has found it.

## 3. Light, lens and geometry

1. **Flash.** Use it only when the metered light is below a threshold, and prefer the torch at low
   power to a burst. A burst makes hard shadows, and question 38 established that shadow, not
   resolution, is what makes a hole in a photograph measure larger than the bullet. Suppress it
   entirely when it would put a specular reflection on glossy paper. Record whether it fired.
2. **Lens choice.** On a phone with several rear cameras, choose the longest focal length that still
   frames the sheet with margin, because the wide and ultrawide lenses carry the most distortion. Say
   which lens was chosen, in the capture record and, briefly, on screen.
3. **Distortion.** Record the lens identity and the reported intrinsics where the platform exposes
   them, and correct barrel or pincushion distortion before measurement. Where the intrinsics are not
   available and the sheet is a GroupLab sheet, solve it from the printed markers, which give enough
   points.
4. **Perspective.** Correct from the detected sheet corners, or from the printed markers where they
   exist. **Refuse outright beyond the angle at which correction stops being trustworthy**, and say
   what the angle was and what the limit is. A refusal that does not say the number is a refusal the
   user cannot act on. Measure that limit rather than choosing it: entry 130's paired photographs and
   scans are the material.
5. **What is recorded with the image:** lens, focal length, reported intrinsics, measured off axis
   angle, the correction applied, whether the flash or torch fired, and the quality score. **Never
   GPS, never location, never a published timestamp.** That rule is already in `CLAUDE.md` and it
   applies here without exception.

## 4. Build these now, on the desktop

These are platform independent, the desktop import path needs them, and they are what the mobile screen
will call:

1. Sheet corner detection and perspective correction from a photograph of a sheet with no usable
   markers.
2. The off axis angle measurement, the refusal threshold, and the refusal message that names the angle.
3. The quality score of section 5.
4. Lens distortion correction solved from the printed markers.

Each with tests against the existing photographs, and each reported as a measurement rather than as an
assertion that it works.

## 5. The quality score

One number stored with every image, so that later analysis can weight or exclude poor photographs, and
so that a support conversation can tell a bad photograph from a bad detection. It combines focus,
exposure, off axis angle, resolution across the sheet, and how many markings were readable. Say how it
is computed, in the document, in enough detail that a reader could recompute it by hand.

It is shown to the user as words, not as a number out of a hundred: good, usable, poor. The number
lives in the record.

## 6. Targets GroupLab did not print

Everything above applies except the marker based steps. The application still frames the sheet, still
checks focus and exposure, still detects the paper edges, still corrects perspective from them, still
chooses the lens and still scores the result. The scale is asked for afterwards rather than read from
the sheet. See entry 152, which settles what GroupLab can measure without its own markers; this section
must agree with whatever entry 152 finds, and if the two disagree, entry 152 wins.
