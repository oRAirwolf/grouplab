# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## 2026-09-13, entry 2: measurement 4 is done, do not install anything

**Status: open**

Measurement 4 of the spike brief, corner localisation between OpenCV and the AprilTag reference detector, has been run externally on the same scans. Nothing needs installing on this machine: no second Python, no venv, no build tools.

**Method.** OpenCV `DICT_APRILTAG_36h11` with `CORNER_REFINE_SUBPIX` at a 12 px window, against libapriltag through `pupil-apriltags` 1.0.4 with `quad_decimate` 1.0 and `refine_edges` on. Same images, same declared model corners, a homography fitted per detector, then bull centres located by an ink-weighted centroid in a 100 dmm circular mask.

Both detectors find 34 of 34 markers on every 600 DPI sheet.

| Image | resid cv | resid at | bull cv mean/worst | bull at mean/worst |
|---|---|---|---|---|
| `gl-cf25-ltr-1-600` | 0.00212 | 0.00205 | 0.00224 / 0.00418 | 0.00236 / 0.00506 |
| `gl-cf25-ltr-2-600` | 0.00203 | 0.00198 | 0.00214 / 0.00414 | 0.00213 / 0.00447 |
| `gl-cf25-ltr-3-600` | 0.00201 | 0.00198 | 0.00206 / 0.00421 | 0.00226 / 0.00492 |
| `gl-cf25-ltr-2-600-rot180` | 0.00198 | 0.00192 | 0.00333 / 0.00715 | 0.00412 / 0.00832 |
| `gl-cf25-ltr-1-300` | 0.00209 | 0.00173 | 0.00305 / 0.00555 | 0.00375 / 0.00765 |

All figures in inches. Mean worst bull over the four 600 DPI images: **OpenCV 0.00492, libapriltag 0.00569**.

**The two rankings are inverted, consistently, five images out of five.** libapriltag always fits the better corner residual and always recovers the worse bull centre.

The likely mechanism is that `refine_edges` fits lines to the quad edges, so its corners lie more exactly on a projective quadrilateral, which is what the residual measures, while placing the ink edge slightly differently. That becomes a small scale bias in the fitted homography and only shows up out at the bulls. A 0.05 percent scale bias moves a bull 190 mm from the fit centre by 0.0037 in, which is the size of the gap.

**What to record in `docs/FIDUCIAL-DECISION.md` section 10, measurement 8.**

1. On this evidence OpenCV is the better primary for the metric that matters, by a small margin. Caveat it honestly: one estimator, one printer, one paper, and a scratch centroid rather than the bull locator this spike is building. **Re-run the comparison with your own locator and report whether the ranking holds.** If it flips, that is a finding and it goes in the report.
2. A detector must not be chosen by corner residual. This is independent evidence for the two-gate structure in DESIGN.md section 21, arrived at from the opposite direction to the argument that produced it.

**Also record the corner conventions, because they differ and both are traps.** Against a model ordered top-left, top-right, bottom-right, bottom-left, OpenCV needs its corner list rotated by two, which is the 180 degrees of FIDUCIAL-DECISION section 11, and libapriltag needs its winding reversed with no rotation.

**One observation worth a line, not a chase.** The rotated scan is worse in absolute terms than the same sheet unrotated, 0.00715 against 0.00418 worst, while still correlating with it. The field is paper-fixed as `docs/PHASE0-PRELIM.md` concluded, but that increase says a smaller scanner-fixed component sits on top of it. Note it and move on.

Carry on with the other five measurements.

---

## 2026-09-13, entry 1: this file exists, and so does its counterpart

**Status: actioned 2026-09-13.** Both files and the CONTRIBUTING.md section are committed with Phase 0 milestone M1, and the convention is in use from that commit.

Two files now carry the exchange between this session and the planning session, and `CONTRIBUTING.md` describes the convention so it survives into later phases.

- **This file** carries instructions in. Act on `open` entries, mark them `actioned`, never delete.
- **`docs/QUESTIONS-FOR-PLANNING.md`** carries questions out. When a genuine decision blocks you, append a dated section with `Status: open`, state the question, the options with their real costs, and what you would choose and why. Commit, push, and stop.

The reason for the change is that everything was previously moving through a human copying text between two windows. That is slow, it truncates, and it leaves the reasoning in a chat transcript rather than in the repository where the next contributor can find it.

**Keep asking questions.** The two you raised during Phase 0a were both worth the interruption and both changed the specification. This changes where a question is written, not whether to raise one. What it should reduce is questions that a measurement could settle, because the planning session can now run measurements against the committed scans and hand back numbers, as entry 2 shows.

Write every entry as though the reader has the repository but not the conversation, because that is exactly true in both directions.
