# Phase 1 results, entries 251 to 275

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 261: is a self-improving detection engine worth building

`docs/DETECTION-LEARNING-STUDY.md`: build a scoreboard now, tune today's settings against it later, not a learned model yet. The measurement found that shadows, dim light, blur, noise and JPEG cost at most two or three holes of 25, and a gentle curl cost the whole sheet, which entry 260 then mended.

## Entry 260: the capture screen rebuilt, and paper that is not flat

**Why it failed:** the preview was a native view over Avalonia's controls, and the phone's log showed "Move back" on every frame because the analysis frames were 640 by 480, too small to read the codes, and the off-white counter gave no outline. **The fix:** a native capture screen (Capture B) laid out with the preview, analysis frames of 1920 by 1440, a search that reads markers without an outline, Guided and Manual, the torch on Auto, and every picture checked and scored 0 to 100 (`PictureCheck`, formula in MOBILE-CAPTURE.md). Frames take about 350 to 480 ms on the Fold 7. `scripts/device-capture-check.py` reads the screen's own `camera.layout` line, because a UI dump cannot see native views inside Avalonia. **Paper that is not flat:** where the radial fit keeps fewer than half the corners, a thin-plate spline through every marker corner registers the sheet (`MarkerMesh`); on a sheet bowed 0.05 in the holes are found with a median error under 0.01 in, and a flat sheet keeps its homography.

## Entry 259: the seven phone screens, as Alan chose them

Full figures with tap to explain, the bulls you fired at, Shots Needed to Zero's own page, compare loads, Ballistics as a fifth tab, the set as a checklist and the scan pill, in four commits (f2b1b81 to ffc5c21), each built to its A concept. Not yet tried on the devices.

## Entry 258: the phone does what the desktop does

Shared pieces moved out of the desktop screens: `ResultFigures`, `CompareSessions`, `HitFromGroup`, the curve drawing of Shots Needed to Zero. `docs/PHONE-PARITY.md` has a row per feature and the site build fails on a feature with none. Still to come: marking by touch, CSV through the share sheet, large sheet advice, and a picture shared from another app.

## Entry 257: the unfolded Fold 7

A standing option; nothing needed the inner screen while nobody could open it.

## Entry 256: each new thing shows itself on the Features page

The E bull, the C bull with its dot, the four C3 grids and the other named sheets are drawn from the generator at print scale (639389c), CLAUDE.md carries the rule, and the stamp covers them.

## Entry 255: the camera logs what it said

Each instruction, the mode, the score and the shutter's timing go to the app's log (106108d). The first test failed; entry 260 is the fix. The test with Alan, and closing request 33, wait for the morning.

## Entry 253: every desktop picture redone, and a stale check

Sections 1, 2, 4 and 5 are in 29c7113: `docs/figures/SCREENSHOTS.md` lists every picture on the site with where it is used and what made it, the desktop walk was rerun at both sizes and themes, the sheet pictures come from the current library, and `scripts/screens-stamp.py` fails the site build when a picture is older than its screen's source files. The phone and tablet pictures (section 3) wait for a sitting: the tablet did not answer adb all night.

## Entry 254: question 64 answered from real holes, and C3 released

**The real holes.** `aim test09262026.png` (600 dpi, the aim point card of 2026-09-26), each hole's bright blob on the lid of the
scan: C's shot through the black diamond 0.194 in across (box 0.217 by 0.228 in); C's second mark 0.091 in; I's hole 0.345 in, several
shots through one hole and not used; A and E on white paper 0.139 to 0.347 in, the same-sheet control. The synthetic hole on black
showed 0.05 to 0.085 in, which is why it was refused; a real one's core is as large as on white paper.

**The check** (`ZeroGridC3Tests.ARealHoleInSolidBlackIsFound`, seven cases): C's hole, cut from the scan as a 256 pixel square
(`Fixtures/real-hole-in-black-2026-09-26.png`, consent in `samples/PROVENANCE.md`), set into GroupLab's own render of each C3
diamond and the E and C bulls, at 300 dpi, with and without a calibre: found every time, more than half its area on the black.
`TheSyntheticHoleOnBlackIsSmallerThanARealOne` replaces the old refusal test and holds the difference. **The on-ink floor is unchanged**;
the white print specks in the card's black measure 0.030 to 0.036 in, below every floor.

**Released.** `GL-ZERO-MOA-100Y`, `-MIL-100Y`, `-MOA-100M` and `-MIL-100M` are C3 (grid style 3) in the library, with new identifiers;
the style 2 sheets are frozen in `targets/frozen/zero-grid-2` and still read. `TARGET-LIBRARY.md` section 5 is rewritten; the held
builder and the `library held` command are gone. Tests that held a ring aim's touching pairs read the frozen style 2 sheet. Request 51
is answered unasked; E and C needed no warning.

## Entry 252: Shots Needed to Zero; C3's diamond in angle and the MOA grids

**Sections 1 and 2** are in the C3 sheets of entry 251: the diamond 0.2 mil or 1 MOA point to point, the MOA grids at 0.5 MOA squares
with 1/4 MOA ticks, MOA-100Y 3 MOA across and 3.5 up and down, MOA-100M 3 MOA; held with the mil sheets for question 64.

**Section 3** (`ShotsToZero`, `MainWindow.ShotsToZero.cs`, credited to Jylee). With sigma known, one axis's chance is a closed form
(the true zero uniform in its click, the centre of n shots normal about it); both axes is its square. With sigma estimated from the group,
sigma is drawn from its own uncertainty, 4,000 stratified quantiles offset by the seed, and each draw's chance is the closed form, so the
error of the mean is bounded by the chance's range over 4,000 and the screen says how far a count could move. The procedure simulated
shot by shot (200,000 sessions) agrees within 0.005; the planning session's 42, 56, 68 and 76 percent at 5, 10, 20 and 40 shots are
reproduced; more shots never lower the chance; a wider rifle needs more; an uncertain sigma needs more than a known one (a 5 shot group's
99 percent within one click, 9 shots against 5). Glossary: closest click, within 1 click. Article 32, `shots-to-zero`.

**Section 4.** Cost: trials, times the shot counts the bisection visits (about a dozen for each of twelve thresholds, cached), times two
goals; the centre of n shots is never drawn shot by shot. Desktop, measured 2026-09-28: 150 to 191 ms a calculation for 5, 10, 25 and
100 shot groups at sigma of 0.3, 1 and 3 clicks. Budget held in `ItIsWorkedOutWithinTheBudget` (2 s allowed for a CI runner). The Fold 7,
the tablet and a 4 GB phone are measured in request 50's sitting through GroupLab Dev's `org.grouplab.test.shotstozero`; by the benchmark's
ratios, 1.45 and 2.6 times the desktop, about 0.25 and 0.5 s, and the phone's run is off its interface thread. Nothing had to give.

## Entry 251: the C3 zeroing grids, built and held

**The design, as grid style 3** (`GridStyle3`, TARGET-SCHEMA.md section 3.13's new subsection). 0.2 mil or 0.5 MOA squares; lines
0.5 mm, whole MOA 1.2 mm, axes and frame 2 mm; a click's tick (0.1 mil, 1/4 MOA) halfway between lines on the centre cross and inward
from the frame; each line's distance from the aim outside the frame on all four sides, whole units larger and bold, nothing inside;
the legend above between the two codes in bold ("MIL · 100 YD", a square and "= 0.2 MIL", a tick and "TICK = 0.1 MIL (1 CLICK)"); the
4 in or 10 cm check bar below; the C diamond as the aim, 0.2 mil or 1 MOA point to point (entry 252 section 1), its white centre
kept clear of the cross. Markers stand in the two side columns between the numbers, at the ticks' heights, since the numbers take
the corners and the rows. **Bold** is new to the format's text: Helvetica-Bold in the PDF only when a page uses it (every existing
PDF is byte for byte as it was), and Liberation Sans Bold's outlines for the preview.

**The four sheets** (`LibraryBuilder.ZeroC3Sheets`, `grouplab library held`): MIL-100Y plus or minus 1.0 mil; MIL-100M 0.8 mil both
ways (1.0 up and down collides with the check bar and the identifier); MOA-100Y 3 MOA across and 3.5 up and down; MOA-100M 3 MOA.
None has a load block: there is no room beside the numbers without shrinking the grid, so the load goes on the session. They
validate, round trip through GLTD-B as style byte 3 with their markers, and no number touches a marker or another number.

**The detection check** (`ZeroGridC3Tests`, 300 dpi, .224, .264 and .308): on the grid, 72 of 72 holes found, worst 0.023 in. In the
diamond's black, 0 of 12: refused as too small, because the rim vanishes into the ink and the size floor is set on whole marks on
paper. Touching the diamond 12 of 12, worst 0.053 in; in the white centre 10 of 12. The same refusal happens on the E and C bulls
already released. **So the C3 sheets are not in the library yet:** question 64, and request 51 for a real scan of holes in black.
PDFs for that are in `C:\Dev\grouplab-local\zero-concepts\`.

## Entry 250: the Targets preview shows the words; Letter before A4; the one-shot zero evaluated

**Section 1.** The preview drew a scene's discs and rectangles and skipped its text. `SceneRasterizer` can now draw text runs, from
glyph outlines derived from Liberation Sans (metric-compatible with the PDF's Helvetica) and placed at the PDF's own advances
(`SheetGlyphs`, the outlines written by `scripts/sheet-glyphs.py` with the font's license at their head). Only the previews ask for it;
everything that measures a render still draws no words, so no measured figure moves. Both previews carry the actual-size line when the PDF
does, and the "text is drawn in the PDF" sentence is gone. **Which sheets were wrong: every one.** Every library sheet and a designer sheet
has words, and the old preview differed from its PDF by 1,643 to 43,016 pixels at 150 dpi; with the words, 0 to 4, except the zeroing
grids' large labels at about 1,300, the fine shape of Liberation Sans against the reader's Helvetica (`PreviewMatchesPdfTests`). No two
words overlap on any sheet (`NoWordsOnAnySheetRunTogether`); the mil grid's "1.0" and "0.5" come close without overlapping, and entry
251 replaces that layout.

**Section 2.** One rule in `TargetLibrary.PaperRank` orders each family: Letter, then Legal, Tabloid and the rolls, then A4, A3 and A5,
the catalogue's order within a size; with the region's A4 the ISO sizes lead. The desktop and the phone pass the region
(`LibraryOrderTests`).

**Section 3** is superseded by entry 251 (C3 chosen); **section 4** is `docs/notes/ONE-SHOT-ZERO.md` and its answer in `for-alan.md`.

## Entry 258, continued: a picture shared in, shots out as CSV, and large-sheet advice on the phone

GroupLab on the phone is now offered for an image sent or opened from another application (`MainActivity`'s two intent filters); the
picture is copied into the cache and read as a chosen photograph with the caliber and distance as typed. A result has "Share the shots as
CSV", the file the desktop's export writes (`ShotCsv.Write`), through the share sheet. The Targets screen gives a sheet too large for a
flatbed the same photograph advice as the desktop, now from one place in Core (`PhotographLimit.ForSheet`). Marking by touch and importing
CSV each need a screen no concept covers, and are in for-alan.md as DESIGN NEEDED.

## Entry 262: torch strength

The Fold 7 runs Android 16 and offers torch strength 1 to 5, default 1 (`docs/ANDROID.md` section 16). The tablet and the burst wait for a sitting.

## Entry 265: Android on the download page

/download/ has the APK beside the desktop builds, and the site build fails when a platform `docs/PLATFORM-SUPPORT.md` publishes has no download there.

## Entry 264: the donor pack

`dotnet run --project src/GroupLab.Cli -- donor-pack` builds one sheet of each style from the library into `website/donor/`, `docs/VOLUNTEER-PACK.md` is rewritten, and /shoot-a-target/ offers each sheet or the pack, with the one most useful now named.

## Entry 263: the night's rules

Followed: nothing stopped for Alan, and what needs him is at the top of `docs/notes/for-alan.md`.

## Entry 261, continued: the server's capacity read

Section 6's one read-only command ran at 17:45 UTC with Alan's approval (request 52): two processors, 11.9 GB of memory with 10.2 GB available and no swap, 36 GB free of 45 GB, load 0.64. The re-reading loop fits at tens of submissions a day as one more worker capped at one processor and about 1.5 GB; with no swap the memory cap is required. `docs/DETECTION-LEARNING-STUDY.md` section 6 has it. Nothing on the server changed.

## Entry 260, continued: the score checked, the off-white counter, and shadows

**The score against how pictures measured.** On the sixteen Phase 0 phone photographs of unshot sheets the first score disagreed with how each measured; with the angle free to 25 degrees and the registration's error a part, the tightest registrations score 91 to 100 and the four worst 40 (MOBILE-CAPTURE.md section 6). **The off-white counter.** `CaptureScreenTests.ASheetOnAnOffWhiteCounterIsFoundAndReady` holds the guidance to a 1920 by 1440 frame of a sheet on a counter barely darker than its paper; at that size the codes are not read and the sheet is found by its markers' layout, which the E and C bull variants share. **Shadows.** The normalisation entry 260 asks for is already render-and-difference's stages S5 and S6: a local paper field from the pixels the render calls paper, and both images divided by it before differencing; the detection study measured hard, soft and hand shadows against it. **Still to do:** the inner Fold screen and landscape, the camera test with Alan, and the torch pair, which need the devices.

## Entry 274: a target GroupLab did not print, shown as a sample

The screenshot walk draws a plain Letter target at 150 dpi (four ring bulls with a solid center, five holes each, "Sample target" along the bottom, no markers or codes), opens it on the marking screen, sets the scale from bull 1's outer ring, 2.0 in across, places the four bulls and twenty shots by hand, and saves `marking-other` in both themes at every size. The home page's "Your own targets", the tour index and the marking stop show it, each saying that marking by hand is on the computer and coming to the phone. Entry 261's wording in the notes log no longer names `tools/study/`.

## Entries 272 and 273: the printer check, and tap a number to switch units

**The printer check.** `GL-SCALE-LTR-1` and `GL-SCALE-A4-1` are grid style 4 (`GridStyle4`, TARGET-SCHEMA.md section 3.13): three
crosshairs in an L 150.00 mm apart, rulers of 250.0 and 190.0 mm, a card outline standing 3 mm outside an ID-1 card (question 67), the
title and four instructions, all derived from the page size; the crosshairs' circles are unscored bulls. `PrinterProfile` now holds across
and down. `CardCheck` finds the card's four sides where the photograph stops matching the printed page, fits each straight, and corrects for
the card's 0.76 mm thickness from the camera's distance; on synthetic photographs at 99.2 percent a dark, a very dark and a light grey card,
shifted up to 0.6 mm and turned up to 1.2 degrees, read 0.992 within 0.0015 across and 0.002 down. `PaperEdgeCheck` measures the paper
through the markers and tells Letter from A4 by its shape; a sheet printed at 96.2 percent on Letter reads 0.955 to 0.969 and is said to
look like Fit to page. The wizard is on both platforms; Printers in Settings lists each printer with its figures and date.

**Tap a number to switch units.** `UnitSwitch` decides what a tap chooses, `UnitTap` makes any value that shows a unit tappable, reading its
kind from the unit after its number; the desktop goes through `UseUnits`, so what is typed on Ballistics is rewritten rather than changing
meaning (the Settings combos now do the same), and the phone's result, Ballistics, Compare and Shots Needed to Zero show a change at once.
`UnitTapTests` taps the mean radius and checks every angle becomes mil, lengths stay, the note says so and it is remembered.

**The curled-sheet mesh, rechecked.** Recording the new pages' artwork showed four Phase 0 phone photographs registering through entry 260's
mesh, where the radial fit had held a third of their corners; the spurious marks on them went from 19 to 12 (ultrawide2 from 1 to 6). The
mesh is now taken only where its error between markers beats the plain homography's over every corner, which these four still pass.

## Entry 271: real inches on photographs

`PrinterProfile` keeps a printer's measured scale, from a scan (its x and y spread, at least 0.1 percent) or one typed ruler distance (a thirty-second of an inch over the span); `RulerSpan` names the two bulls to measure (bull 1 to bull 5 on the 5 by 5 sheets). A photograph is multiplied by the chosen printer's scale and says so in one line; a scan that measured its own ignores it. On a sheet printed at 96.2 percent and photographed, bull 1 to bull 5 reads within 0.002 in of its true size with the profile; a ruler read to a sixteenth agrees with the scan's profile within their stated uncertainty. The session file carries the factor and its line. DESIGN.md no longer says a photograph measures the print scale.

## Entry 270: any target, and the sheet as the fast lane

The README's pitch, first caption and mosaic tile, the home page's lead and a "Your own targets" section, the tour's index, the Features page's first feature ("Any target you already shoot"), and the user guide's opening all lead with any target, with automatic holes on any target said as the goal. The mission line is in "Why it exists" and on the home page. `docs/RETIRED-WORDING.json` now names wording that makes a GroupLab sheet sound required. The picture waits on question 66.

## Entry 269: Shots Needed to Zero's colors

Within one click amber, the closest click teal, on the desktop and the phone.

## Entry 268: dark OLED screens

GroupLab Dev's idle screen is pure black with a dim line of text; Close, Back and Home all leave it; every device check ends on it.

## Entry 267: no lawyer, and a standing consistency audit

`scripts/consistency.py` checks the README, the site and the assets agree, and that retired wording (`docs/RETIRED-WORDING.json`) is gone; CI warns, and a weekly run opens an issue.

## Entry 266: the README as Alan chose it

Product shot, captions as a table, the six-screen mosaic, generated by `scripts/readme-images.py` from the screenshot job's pictures; "What GroupLab is not" is gone.

