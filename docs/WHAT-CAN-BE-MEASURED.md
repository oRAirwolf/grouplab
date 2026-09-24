# What GroupLab can measure, and what the scale comes from

NOTES-FROM-PLANNING.md entries 152, 161 and 171. Two published statements said GroupLab can only measure a
sheet it printed, and that a sheet printed at the wrong size cannot be recovered. The first is false.
The second was half true, and entry 152 got the other half wrong **in this file**: it said a shrunk
sheet measures correctly, and entry 161 read the code and found that it does not. The corrected
statement is below, and the entry 152 version is quoted in the log rather than left standing here.
Entry 171 then answered question 49: a scan now corrects for the print scale and reports real inches.

**This file is the one source.** The tour pages, the research articles and the README all state what is
here rather than each writing its own version of it, and a test holds them to it. Everything below is a
statement about the code as it is, with the file that settles it.

---

## Every measurement rests on a scale, and there are four ways to get one

A scale is how a distance in pixels becomes a distance in inches at the target. GroupLab always says
which of these it used, because they are not equally good.

| Where the scale comes from | What it needs | What it models |
| --- | --- | --- |
| **A GroupLab sheet's own markers** | A sheet GroupLab printed, photographed or scanned with enough markers visible | Perspective and the lens, and on a scan it measures the print scale and corrects every figure for it. The best of the four, and the only automatic one |
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

## A sheet printed at the wrong size is read correctly, and a scan measures it in real inches

**Where every shot is on the sheet comes out right.** The markers shrank with the sheet, so which bull
a shot belongs to, where it sits against that bull and the shape of the group are all correct.
`SyntheticScanTests.Test43APrintAt962PercentReportsItsScale` proves exactly this: on a sheet printed at
96.2 percent, every bull centre is recovered in the sheet's own coordinates to within 0.001 in.

**On a scan, every distance is in real inches.** A scan's stated resolution is an absolute ruler, so
GroupLab measures how large the sheet was printed and multiplies every distance by it: a group on a sheet
printed at 96 percent reads its true size, and a hole's diameter is its true diameter. The results panel
names the scale once it is more than a quarter of a percent from 100. This is entry 171's answer to
question 49, and `ImperfectSheetTests.AScanPrintedAt96PercentReadsTrueSizeAndAPhotographSaysWhyItDoesNot`
holds it. Where a file's stated resolution puts the sheet below 85 or above 115 percent, which is more
likely a wrong resolution than a real print, nothing is corrected and the screen says so.

**On a photograph, every distance is in the sheet's own inches.** There is no absolute ruler in a
photograph, so the print scale cannot be measured. On a sheet printed at 96 percent a sheet inch is 0.96
of a real one, so every size, spread and radius reads about 4 percent large, and the screen says in one
line: "Measured in the sheet's own inches; if the sheet was not printed at actual size, the figures are off by the same percentage."

A saved marking records which of the two it used, so a group from a scan and one from a photograph can
be told apart.

How much it matters on a photograph is the size of the print error. At the 100.3 percent one sheet here
was printed at, it is 0.003 in on a one inch group, which is below everything else in the measurement.
At the 94 to 97 percent a "fit to page" print dialog produces, it is 3 to 6 percent, which is larger than
the difference between two good loads.

## So why print at actual size

1. **It matters for photographs, because a photograph cannot measure the print scale; a scan can and corrects for it.** On a photograph a sheet printed small makes every group
   read large by the same fraction, and nothing on the photograph can tell you how much.
2. **A shrunk sheet is a different sheet.** The bulls are closer together and smaller than the sheet was
   designed for, which is a fact about the shooting rather than the measurement.
3. **Scaling that is not uniform is not recoverable even in principle.** The registration fits a planar
   mapping, which follows a uniform shrink and even different shrinks in x and y. A printer whose
   scaling varies across the page is not planar.
4. **Smaller markers register less well**, and below some size they stop being found at all.

The instruction printed along the bottom edge of every sheet, and the ruler check, are how you know.
Both stay.

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
