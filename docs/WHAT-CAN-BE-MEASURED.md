# What GroupLab can measure, and what the scale comes from

NOTES-FROM-PLANNING.md entry 152. Two published statements said GroupLab can only measure a sheet it
printed, and that a sheet printed at the wrong size cannot be recovered. Neither is true, and one of
them was contradicted by a research article on the same website. A reader who read both learned that
the site cannot be trusted rather than which sentence was right.

**This file is the one source.** The tour pages, the research articles and the README all state what is
here rather than each writing its own version of it, and a test holds them to it. Everything below is a
statement about the code as it is, with the file that settles it.

---

## Every measurement rests on a scale, and there are four ways to get one

A scale is how a distance in pixels becomes a distance in inches at the target. GroupLab always says
which of these it used, because they are not equally good.

| Where the scale comes from | What it needs | What it models |
| --- | --- | --- |
| **A GroupLab sheet's own markers** | A sheet GroupLab printed, photographed or scanned with enough markers visible | Perspective, the lens, and the sheet's own print scale. The best of the four, and the only automatic one |
| **A known rectangle** | Four corners tapped in order, and its real width and height | Perspective exactly. Not a bow in the paper, because the mapping is planar |
| **A known length** | Two points tapped, and the distance between them | One scale over the whole image. It assumes the photograph is square on and the sheet flat, and it says so beside every figure |
| **A scan's stated resolution** | A scan, not a photograph, whose file states a believable resolution | The same as a known length. It is **offered and never applied by itself**: the number is shown and you accept or refuse it |

`src/GroupLab.Core/Marking/ScaleReference.cs` holds the first three.
`src/GroupLab.Core/Marking/StatedResolutionScale.cs` holds the fourth, which refuses a photograph and
refuses the 72 and 96 that a file gets when the thing that wrote it had nothing to say.

## Any target can be measured once the scale is set

A target GroupLab did not print is measured by setting the scale by hand and marking the shots by hand.
Everything downstream of the marks then works exactly as it does on a GroupLab sheet: group size,
extreme spread, mean radius, the standard deviations, the comparison between groups, the units, the
export and the report.

**What a GroupLab sheet adds is that none of it is by hand.** Specifically, and only these:

- **The scale**, from the markers, modelling perspective and the lens rather than assuming them away.
- **Where the holes are.** Automatic hole detection works by rendering the sheet GroupLab printed and
  differencing the photograph against it, so it needs the sheet's definition. On any other target the
  holes are marked by hand. `src/GroupLab.Core/Marking/AutomaticMarking.cs` takes a definition and
  refuses without one.
- **Which bull each shot belongs to**, because the sheet says where its bulls are.
- **The review queue**, which exists to question what the detector decided. Hand-placed marks are not
  guesses, so there is nothing for it to question.
- **The sheet's identity**, which is what ties a photograph to the sheet it is of.

## A sheet printed at the wrong size measures correctly

This is the claim that was published backwards. **The measurement is corrected, and the figures are
right.**

The reason is that the scale comes from the markers, and the markers shrank with everything else. A
sheet printed at 96.2 percent is a smaller sheet, measured by its own smaller markers, and every
distance on it comes out at its true printed value. `SyntheticScanTests.Test43APrintAt962PercentReportsItsScale`
renders the reference sheet at 96.2 percent and puts it through the same gate as a full size one: every
bull centre is recovered to within 0.001 in, and the reported scale is 0.962.

The print scale is worked out separately, by comparing the resolution the markers measure against the
resolution the file claims, and it exists to **tell you your printer shrank the sheet**, not to correct
anything. `src/GroupLab.Core/Marking/DetectionAdvice.cs`: "The measurements are corrected for it, and
the figures are right; print at actual size, 100 percent, to keep the sheet's own spacing."

## So why print at actual size

Three real reasons, none of them the one that was published.

1. **A shrunk sheet is a different sheet.** The bulls are closer together and smaller than the sheet was
   designed for. A sheet meant to be shot at 100 yards with a given bull spacing no longer has that
   spacing, and that is a fact about the shooting rather than about the measurement.
2. **Scaling that is not uniform is not recoverable.** The registration fits a planar mapping, which
   handles a sheet uniformly shrunk, uniformly stretched, or even shrunk by different amounts in x and
   y. What it cannot undo is a printer whose scaling **varies across the page**, and that is the case
   the ruler check catches.
3. **Smaller markers register less well**, and below some size they stop being found at all, at which
   point there is no automatic anything.

The instruction printed along the bottom edge of every sheet, and the ruler check, are how you tell
case 2 from an ordinary uniform shrink. Both stay.

## What genuinely cannot be recovered

- **Scaling that varies across the page.** A planar mapping cannot undo it. It shows up as a rising fit
  residual rather than as a wrong answer that looks right, which is the better of the two failures.
- **A sheet whose markers are cut off, obscured or too few.** Registration says so and refuses rather
  than guessing.
- **A bow in the paper**, for the rectangle and known-length paths. The sheet path models a developable
  surface; a tapped rectangle is planar and cannot.
- **Perspective, on the known-length path.** A photograph taken from an angle is wrong by an amount that
  varies across the frame, and nothing in the figures shows it, which is why that scale declares the
  assumption beside every result it produces.
