# ballistics.js

The point-mass ballistic solver from Alan's website, the source of GroupLab's own solver in `src/GroupLab.Core/Ballistics` (`docs/NOTES-FROM-PLANNING.md` entry 110 section 2, `DESIGN.md` section 16).

- **Source:** the pissinhot.com web server, where it is served to every visitor of the website's ballistic tools, taken on 2026-09-19.
- **SHA-256:** `581fab43209367af53f5271b7dbc3256c52aafe9c6b6a20a3bb4db0c24e7aa14`. The file here is unchanged.
- **Licence:** Claude wrote it for Alan in his Pissin Hot Precision project, so it is Alan's code, licensed into GroupLab under GPL-3.0 with the section 7 app-store permission. Its drag tables are given as the US Army Ballistic Research Laboratory's, which are public domain, but its G1 table is not the standard G1 function (below).

## What GroupLab uses it for

**It is a reference, not part of the build.** Nothing in the application loads it.
- **The port.** GroupLab's solver ports its atmosphere, drag deceleration, RK4 integration, zeroing search, lag-rule wind drift, Miller's stability factor, Litz's spin drift and the two Coriolis terms.
- **The transcription check.** `cases.js` runs this file under Node on the cases in `../ballistics-cases.json`, and the port is compared with its output (`docs/BALLISTICS-VALIDATION.md` section 1). CI runs it again on every push and fails if the output has changed.

## What GroupLab does not take from it

- **Shooting angle, `angleDeg`.** Drop is measured from the horizontal line, not the inclined line of sight, so any non-zero angle reports the line of sight's own rise, about 300 MOA per 5 degrees. The port measures range along the line of sight and drop perpendicular to it.
- **Aerodynamic jump, `aeroJump`.** It returns `crosswind × 0.012 / SG` MOA, which is not a published formula and runs the wrong way with stability. The port leaves aerodynamic jump out and says so wherever its output is shown.
- **The G1 drag table, `G1_TABLE`.** It is not the standard G1 function above Mach 0.85: 0.5210 where the standard is 0.4805 at Mach 1.0, and half the standard at Mach 5. The port carries the standard G1 and G7 tables instead (`docs/BALLISTICS-VALIDATION.md` section 3). The website serves this table, so its G1 results are affected.
- **The Coriolis vertical term's sign, `coriolisVertical`.** Fire toward the east strikes high, and it returns low. The port carries the term with the sign corrected.
- **`solveExtended`'s wind direction.** It adds a wind from the right as drift to the right. The port takes a signed crosswind instead.
- **The dispersion utilities,** `velocityDispersion`, `hitProbability` and `combinedGroupSize`. GroupLab's statistics engine estimates sigma from the marked shots, with its interval.
