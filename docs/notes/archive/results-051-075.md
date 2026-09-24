# Phase 1 results, entries 051 to 075

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 52. The markers sorted before use, every record regenerated, and what the movement measures

`docs/NOTES-FROM-PLANNING.md` entry 52 sections 1 and 2. Sections 3 and 4, how far registration and the edge fit move under reordering and under one point left out, are reported in "Entry 52 sections 3 and 4".

**These figures moved because the markers are now sorted before use. The sheet, the markers and the corners are identical; only their order changed. The movement measures how unstable the registration is on these frames, not an improvement in it.**

**The change.** `MarkerDetection.InIdentifierOrder()` orders markers and rejections by identifier, then by their first corner, and undecoded quads by their first corner, and `SheetMeasurer` and `PageRegistration` take every detection through it. The homography's RANSAC draws its samples by position in its list of correspondences, so until now the answer depended on the order the detector happened to return the markers in. It was chosen for determinism: had sorting made every mounted frame worse, it would still be the change.

**No gate verdict changed.**
- **Paper gate:** ten of ten, worst 0.00325 in, as before.
- **Photograph gate, flat:** fails three of three. No bull figure of `main_flat1-3` moved at the printed precision.
- **Photograph gate, mounted:** fails seven of seven.
- **Print-scale detection:** passes, 0.96195 at 600 DPI where it was 0.96197.
- **Conformance test 43 on the module sweep's five sheets:** passes on every sheet.

**What moved in the whole-sheet registration**, `grouplab spike photos`, on every photograph whose printed figures moved beyond the homography's own RMS:

| Photograph | Gate | Corners kept, before / after | Scoring bulls over the gate, before / after | Worst scoring bull (in), before / after | Worst bull of any kind (in), before / after |
|---|---|---|---|---|---|
| `ultrawide1.jpg` | mounted | 66 / 68 of 136 | 13 / 14 of 25 | 0.03301 / 0.03902 | 0.06983 / 0.07450 |
| `ultrawide2.jpg` | mounted | 54 / 46 of 136 | 20 / 19 of 25 | 0.06983 / 0.08305 | 0.08732 / 0.08305 |
| `ultrawide3.jpg` | mounted | 42 / 51 of 128 | 21 / 18 of 25 | 0.09123 / 0.11331 | 0.09123 / 0.11331 |
| `main1.jpg` | mounted | 90 / 94 of 136 | 8 / 8 of 25 | 0.01532 / 0.01824 | 0.04841 / 0.05183 |
| `main2.jpg` | mounted | 25 / 32 of 104 | 21 / 22 of 25 | 0.04350 at bull 10 / 0.08900 at bull 5 | 0.06231 / 0.08900 |

`main3.jpg` and `telephoto2.jpg` moved in no figure but the homography's RMS. The benchmark of `docs/PHASE0-RESULTS.md` section 4.5 moves with these rows: worst scoring bull 0.018 to 0.113 in where it was 0.015 to 0.091, scoring bulls over the gate 8 to 22 where it was 8 to 21, and corner RMS 0.015 to 0.070 in where it was 0.014 to 0.060.

**What moved in the surface fit**, each frame fitted alone (`grouplab surface frames`), the selected model:

| Photograph | Corners kept, before / after | Worst scoring / sighter (in), before | Worst scoring / sighter (in), after | Scoring bulls over the gate, before / after |
|---|---|---|---|---|
| `ultrawide1.jpg` | 113 / 113 of 136 | 0.01249 / 0.02129 | 0.01044 / 0.02522 | 9 / 9 |
| `ultrawide2.jpg` | 110 / 108 of 136 | 0.05809 / 0.00758 | 0.05784 / 0.00747 | 9 / 8 |
| `ultrawide3.jpg` | 87 / 81 of 128 | 0.06660 / 0.03095 | 0.05509 / 0.03144 | 12 / 13 |

The surface fit starts from the whole-sheet homography, so it inherits the reordering. On `ultrawide1.jpg` it keeps the same 113 corners and converges to a focal length of 2053 px instead of 1940: a second place where the answer depends on where the fit starts, not only on its data.

**Elsewhere, the scans and the studies.**
- **Scans:** the figures move in the last places. The largest move is `gl-cf25-ltr-96.2-600-dpi.png`'s worst edge-fit bull, 0.00234 to 0.00340 in, still inside the gate.
- **Measurement 1, random subsets of markers:** the four-marker rows move most. Sheet 1's 90th percentile worst bull goes from 0.03859 to 0.07296 in, and nine random markers now reach 0.0041 to 0.0065 in at the 90th percentile.
- **Measurement 2, corner refinement:** at 600 DPI the 1.5-module window's worst bull goes from 0.00328 to 0.00517 in, over the gate on one sheet. The shipped window's worst bull, 0.00316 in, does not move.
- **Measurement 3, threshold window:** the shipped window's worst bull on any sheet goes from 0.00325 to 0.00340 in.
- **The surface correlation:** two readings change. `ultrawide1.jpg` goes from structured to not distinguishable from random, at p 0.002, and `main_flat2.jpg` goes from not distinguishable to structured, at p 0.001. M1.11's prose now says two structured mounted frames, not three, and one structured flat control.

**What was regenerated, and what was not.**
- **Records:** every committed record that a command writes: the thirteen in `scans/phase0/measurements`, the Phase 1 records, the module sweep's PDFs, and the committed Windows tables the gate record workflow compares against.
- **`mounted-pair.json` was not regenerated.** `grouplab mounted pair` reads `scans/mounted/`, which entry 35 section 4 moved out of the repository, so its reproduce line no longer runs from a checkout. It locates dots on an OnTarget sheet rather than GroupLab markers, so the sort cannot move it.
- **Documents:** both results documents, `DESIGN.md` section 21 [r5] and `docs/PHASE1-BRIEF.md`'s benchmark carry the regenerated figures; the benchmark keeps the old figures beside the new. The tables from fits that no command reproduces today, M1.5's, M1.7's, M1.8's and M1.9's, are left as measured, and M1 says so at its head.

**How the sort's effect was isolated, and what that found.** Every record was regenerated twice under identical code, once without the sort and once with it, and every figure above compares those two runs. The unsorted run already differed from the committed records in five places, which are now regenerated with the rest and are not the sort's effect:
- **The synthetic hole detection records:** 139 and 93 values, rim closures, elongations and centre errors of up to 0.00008 in.
- **`surface-lens-synthetic.json`:** in one trial of 800 the selection flipped, and its selected worst bull went from 194.85 in to 2.25 in.
- **`surface-lens.json` and `surface-rendered.json`:** they gained the `family` and `turn` fields later code writes.
- **The module sweep's five PDFs:** they gained `/ViewerPreferences << /PrintScaling /None >>`, which entry 25 added to the renderer.

**Tests:** Core 758 passing with the records as committed, none skipped.

---

## Entry 52 sections 3 and 4. How far registration and the edge fit move when nothing that matters changes

`docs/NOTES-FROM-PLANNING.md` entry 52 sections 3 and 4, and entry 49 section 5.

**Reproduce:** `grouplab spike stability`, raw rows `scans/phase1/measurements/stability.json`, every order's result and every bull's leave-one-out figures.

**The method, one for both experiments.**
- **Registration against the order of its inputs.** Each flat and gated mounted photograph is detected once, then registered 200 times. Each time, the same corner correspondences reach the homography fit in a different seeded order, and the inlier flags are put back in the caller's order. The sheet, the markers and the corners are identical in every run.
- **The edge fit against each of its points.** On each photograph's own registration, every located scoring bull is refitted from the same start with each edge point left out in turn. The table gives the largest movement of any bull, and how many bulls have a point within a tenth of the fit's rejection limit.

**What was stated before running it.** Planning's hypothesis: on a flat sheet a homography is nearly the right model, so the consensus is stable. On a mounted sheet no plane fits, so RANSAC returns whichever subset looked best on its draws. And if the mounted frames spread, the mounted gate's 0 of 7 has never had an error bar.

| Frame | Gate | Orders | Distinct outcomes | Corners kept, min / median / max | Scoring bulls over the gate, min / median / max | Worst scoring bull (in), min / median / max | Largest leave-one-out shift of a bull (in) | Bulls with a point near the rejection limit |
|---|---|---|---|---|---|---|---|---|
| `ultrawide1.jpg` | mounted | 200 | 69 | 60 / 69 / 76 of 136 | 10 / 13 / 15 of 25 | 0.02785 / 0.03590 / 0.05062 | 0.00004 at 21, 651 of 651 points used | 4 of 25 |
| `ultrawide2.jpg` | mounted | 200 | 67 | 36 / 50 / 61 of 136 | 14 / 20 / 23 of 25 | 0.03020 / 0.06819 / 0.09631 | 0.00006 at 5, 313 of 313 points used | 8 of 25 |
| `ultrawide3.jpg` | mounted | 200 | 67 | 34 / 48 / 57 of 128 | 18 / 21 / 23 of 25 | 0.05938 / 0.10299 / 0.11680 | 0.00008 at 21, 214 of 214 points used | 3 of 25 |
| `main1.jpg` | mounted | 200 | 25 | 85 / 88 / 95 of 136 | 7 / 11 / 14 of 25 | 0.01342 / 0.01463 / 0.02373 | 0.00003 at 21, 749 of 749 points used | 3 of 25 |
| `main2.jpg` | mounted | 200 | 49 | 22 / 32 / 57 of 104 | 16 / 21 / 23 of 25 | 0.03651 / 0.05826 / 0.09253 | 0.00021 at 5, 106 of 106 points used | 6 of 25 |
| `main3.jpg` | mounted | 200 | 15 | 34 / 58 / 67 of 108 | 12 / 20 / 23 of 25 | 0.03616 / 0.06540 / 0.08938 | 0.00010 at 21, 374 of 374 points used | 2 of 25 |
| `telephoto2.jpg` | mounted | 200 | 48 | 16 / 38 / 45 of 132 | 20 / 22 / 25 of 25 | 0.03122 / 0.04012 / 0.05376 | 0.00004 at 21, 670 of 670 points used | 1 of 25 |
| `main_flat1.jpg` | flat | 200 | 1 | 136 / 136 / 136 of 136 | 0 / 0 / 0 of 25 | 0.00343 / 0.00343 / 0.00343 | 0.00002 at 11, 900 of 900 points used | 10 of 25 |
| `main_flat2.jpg` | flat | 200 | 1 | 100 / 100 / 100 of 100 | 2 / 2 / 2 of 25 | 0.00566 / 0.00566 / 0.00566 | 0.00002 at 4, 777 of 777 points used | 7 of 25 |
| `main_flat3.jpg` | flat | 200 | 2 | 89 / 91 / 91 of 92 | 3 / 6 / 6 of 25 | 0.00832 / 0.01183 / 0.01183 | 0.00003 at 4, 596 of 596 points used | 3 of 25 |
| `telephoto3.jpg` | excluded | not reordered | | | | | 0.00322 at 25, 14 of 14 points used | 0 of 20 |

**The hypothesis holds.**
- **Flat frames:** `main_flat1` and `main_flat2` give one result in 200 orders. `main_flat3` gives two: 89 or 91 corners kept, and a worst scoring bull of 0.00832 or 0.01183 in. Lost markers do not explain the difference, since `main_flat2` decoded 25 of 34 and `main_flat3` 23, and `main_flat2` gives one.
- **Mounted frames:** every one gives 15 to 69 distinct results. The worst scoring bull spans 0.030 to 0.096 in on `ultrawide2`, 0.037 to 0.093 on `main2` and 0.059 to 0.117 on `ultrawide3`. Scoring bulls over the gate span 12 to 23 on `main3`, and corners kept span 16 to 45 of 132 on `telephoto2`.
- **What it means:** the instability comes with the model's mismatch to a curved sheet. It is not a tuning problem in the sampler, and sorting the markers made it repeatable without making it smaller.

**The consequence for the mounted gate, as stated beforehand.**
- **The verdict is robust.** No ordering lets a mounted frame pass. The lowest worst scoring bull in all 1,400 mounted registrations is 0.01342 in, on `main1`.
- **The figures behind it are not.** The corners kept, the bulls over the gate and the worst bull each vary across orderings by more than the differences several M1 comparisons were read from.
  - **M1.5:** "worse on two frames" set `ultrawide2`'s 0.06983 against a surface fit's 0.09183, and that frame's whole-sheet figure alone ranges from 0.030 to 0.096.
  - **The benchmark itself:** its figures, whether 0.015 to 0.091 in before the sort or 0.018 to 0.113 after, are single draws from these ranges.
- **So a conclusion drawn from a change smaller than a frame's spread here was not supported by that frame.** The spreads are the first error bars those figures have had.

**The edge fit, entry 49 section 5's three questions.**
1. **Why one point of thirty was worth 0.30 dmm.** The bull had too few points, and the fit had not converged.
   - **The dense bulls:** on the ten frames above, every bull has 106 to 900 edge points, and leaving any one out moves a bull by at most 0.053 dmm (0.0002 in, on `main2`'s bull 5).
   - **`telephoto3`, where the sheet overflows the frame:** bull 24 has 29 points and its fit reports that it did not converge. Leaving one out moves it by up to 0.53 dmm, more than the 0.30 dmm move seen on the macOS runner. Bull 25 has 14 points and moves by up to 0.82 dmm, 0.0032 in.
2. **Whether a hard include or exclude sits where a weight belongs.** Not in the fit's rejection: at convergence it excluded no point on any bull measured here. The step is earlier. Whether a ray yields an edge point at all turns on a threshold, a profile whose ends differ by less than half the bull's contrast gives none, and on a sparse bull one ray more or less is a large share of the evidence.
3. **Whether the stage record says a fit is near its threshold.** Partly. A bull that did not converge is recorded as a rejection with that reason. Nothing records that a located bull rests on a few dozen points, or that its points sit near the crossing threshold. Nothing was changed, as entry 49 section 5 asks.

**Section 4: the uncertainty the intervals do not include.** `docs/STATISTICS.md` section 2 now records, with these numbers, that every interval takes the coordinates as exact and that they are not. On the flat photographs the registration's instability was nothing on two and 0.0035 in on the third; on a mounted sheet it reaches hundredths, and no figure a user sees includes it. `DESIGN.md` section 14 points at it.

**The code.** `grouplab spike stability` is new.
- **The edge fit's convergence loop** is extracted so its last pass's points can be read. `EdgeFitBullLocator.Locate` performs the same operations in the same order: the `sheets` table reprints identically.
- **`EdgeFitBullLocator.LeaveOneOut`** is a diagnostic beside `Locate`, used only by the spike.

**Tests:** Core 758 passing, App 35 passing, none skipped.

---

## Entry 58 sections 3 and 4. The opt-out now survives a re-export, by a key the scrubber already computes

`docs/NOTES-FROM-PLANNING.md` entry 58 sections 3 and 4, its order items 1, 2 and 4, and entry 37 section 2, which this amends.

**The hole.** An opt-out won by content hash, and four uploads of one photograph have four content hashes. iOS rewrites a 36-character identifier inside the Apple maker note on every export from the library, so a contributor who uploads a photograph, thinks better of it, and sends the same picture again with the opt-out ticked was never matched. Entry 37's rule worked only because that contributor uploaded the identical file twice.

**The key, measured before it was chosen.** Planning offered the decoded pixels and asked for the scrubbed bytes to be measured first, as the cheaper option. They are enough:

| | Distinct values across the four uploads |
|---|---|
| File SHA-256 | 4: `7daf9d32`, `0330f435`, `d8b85ced`, `f42b5ba1` |
| SHA-256 of the scrubbed bytes | 1: `8b106005` |

- **Why it works:** scrubbing rebuilds the metadata and copies the compressed image data byte for byte, so two exports of one photograph scrub to the same bytes. The maker note, where the changing identifier lives, is dropped.
- **It is not so loose as to merge different photographs.** The two Android frames of one scene, taken a minute apart by one phone, scrub to `db2863ae` and `93630e53`. The other frame of the same shot sheet, `fc4d1649`, scrubs to `89d53b2f`, distinct from all four above.
- **It is deterministic:** the same file scrubbed twice gives the same key.
- **It needs no image decoder,** which decided it over the pixel hash. Decoding lives in the CLI behind OpenCV and the opt-out check lives in `GroupLab.Core`, so a pixel key would have moved a consent mechanism out of the layer whose tests run on every platform, for a key that measures no better here.

**What changed.** `Intake.PhotographSha256` is the SHA-256 of what a file scrubs to. `Intake.WithheldHashes` records it beside each withheld file's byte hash, and `Intake.Run` holds a file when either key matches, naming which one did. That is the same belt-and-braces reasoning entry 37 section 1 applied to the two opt-out signals: either alone withholds. A hash that only `meta.json` recorded has no file to scrub and still counts by bytes.

**On the real submissions**, read by `grouplab intake --submissions`:

| | Count |
|---|---|
| Distinct byte hashes withheld | 13 |
| Distinct photograph keys withheld | 10 |
| Total keys, from 5 withheld submissions | 23 |

The four browser-test uploads contribute four byte hashes and one photograph key between them. That single key is the hole closed: a publishable fifth export of that photograph is now held, where before it would have been published.

**The limits, written down rather than discovered later.**
- **It does not survive re-encoding.** A messaging app's copy has different compressed data and is a different photograph to this key, as it is to a pixel hash. Only a perceptual hash would match those, and none is proposed.
- **It does not survive a crop or a rotation.**
- **It is a function of the current scrubber,** and cannot drift, because both sides are computed in the same run from the same code rather than stored.

**Entry 58 order item 2, the first real exercise of intake on these files.**
- **The four opted-out uploads:** each refused, "exclude_from_public_dataset is true and a DO-NOT-PUBLISH file is present". Nothing was written.
- **`fc4d1649`,** the stripped iPhone frame: held, "not a camera original: it has no camera make", with 38 GroupLab markers decoded at 4032 by 3024. That is `CameraOriginal` meeting a genuinely stripped real file for the first time, and holding it.
- **The two consented Android photographs,** entry 59 order item 2, the first consented GPS-bearing files to go through the publication path: both published, each with GPS, 31 other EXIF fields, the thumbnail, XMP, the multi-picture index and a trailer removed. `PublicationTests` passes over the published copies, 8 of 8, with `CONTRIBUTORS.md`, `LICENSE` and `README.md` stood in for the testdata checkout. Nothing was published into `grouplab-testdata`: the run wrote to a scratch directory.
- **Entry 59 section 3 falls out of the same run,** printed by triage: lens group `2.20 mm f/2.2, 23 mm equivalent, digital zoom 1.66` for the camera app's frame and `6.25 mm f/1.7, 23 mm equivalent, digital zoom 1.00` for the page capture. One phone, one scene, one 35 mm equivalent, two optical configurations, which is entries 16 and 27 on real files rather than argued.

**A standing note, entry 58 order item 4: the user agent cannot identify a browser on iOS.** DuckDuckGo's user agent is indistinguishable from Safari's, so `user_agent` in `meta.json` is not evidence of which browser a contributor used, and nothing should be concluded from it. It is recorded here, where the donated corpus is described, rather than in `docs/DETECTION-PIPELINE.md` section 2, which describes the `scans/` corpus.

**Tests:** Core 759 passing, App 35 passing, none skipped. The intake fixture's second file is now a second photograph rather than the first with a trailing byte, because under a scrubbed-bytes key those two were one photograph and the test meant them to differ.

---

## Entry 55 section 3 item 1. What each bull rested on, recorded where it can be read

`docs/NOTES-FROM-PLANNING.md` entry 55 section 3 item 1, resting on entry 52 section 3, which measured why the count is worth having.

**Why a count is worth recording.** Leaving one edge point out moves a bull of 106 to 900 points by at most 0.0002 in, and `telephoto3`'s 14-point bull by 0.0032 in, two thirds of the whole gate. That is a sixteenfold difference in how far a bull can move, and it is predictable before anybody looks at the answer, from a number the locator already has and then threw away.

**What is recorded now**, in the `P0.bulls` stage record, for every located bull:
- **`edgePoints[<bull>]`,** the points its fit rested on.
- **`raysNearThreshold[<bull>]`,** how many of its 180 rays per edge had a rise within a tenth of the threshold that decides whether a ray yields an edge point at all.
- **`fewestEdgePoints`** across the sheet, and a detail line naming the sparsest bull, which prints at any verbosity.
- **In `grouplab measure --json`** the count rides beside the existing `edgePoints` field on each bull.

**Why that threshold and not the fit's rejection.** Entry 52 section 3 asked where a hard include or exclude sits, and found it is not in the rejection: at convergence the fit rejects no point on any bull measured. The step is earlier, in the crossing test, where a profile whose ends differ by less than half the bull's ink-to-paper range yields nothing. A ray within a tenth of that line is one the image could flip either way, so it is counted on both sides of the line rather than only where it failed. The tenth is the convention `EdgeFitBullLocator.LeaveOneOut` already uses for a point near the rejection limit.

**Nothing about what is located changed, and it is shown rather than asserted.**
- **The `sheets` table reprints identically,** line for line, against the committed Windows table.
- **No committed record moved.** The new count is in the stage record and in `grouplab measure --json`, and deliberately not in `scans/phase0/measurements`, so the gate record's raw comparison is untouched and no regeneration was needed.

**What it is for, which is not done.** Section 3 item 2 asks for the relationship between the point count and the leave-one-out spread, measured rather than guessed, and says plainly not to pick a cutoff from the three sparse bulls in one frame. That waits for the weekend's frames, where a sheet that overflows the frame is what produces sparse bulls. The field has to exist first, because it cannot be added to data already measured.

**Tests:** Core 760 passing, App 35 passing, none skipped, including a new one that fixes the stage record's per-bull counts against the locator's own.

---

## Entry 61 section 3. The print verb off Windows: checked before it was changed, and the prediction was wrong in its mechanism

`docs/NOTES-FROM-PLANNING.md` entry 61 section 3, which asked for this to be checked rather than assumed, and said the fix differs with the answer.

**The prediction.** That `Verb = "print"` with `UseShellExecute` throws `PlatformNotSupportedException` on Unix, that neither `catch (Win32Exception)` sees it, and that pressing Print therefore offers the person a crash report every time.

**What the runtime actually does**, `dotnet/runtime`, `SafeProcessHandle.Unix.cs`, where `UseShellExecute` is implemented:

```csharp
string verb = startInfo.Verb;
if (verb != string.Empty &&
    !string.Equals(verb, "open", StringComparison.OrdinalIgnoreCase))
{
    throw new Win32Exception(Interop.Errors.ERROR_NO_ASSOCIATION, SR.Format(SR.UseShellExecuteVerbNotSupported, verb));
}
```

**It is a `Win32Exception`,** which the existing catch already handles. The print button does not crash on Linux or macOS, the fallback runs, and the PDF opens in the viewer. That is planning's own second branch: merely useless there rather than broken. `ProcessStartInfo.Verbs` returns an empty array off Windows and throws nothing, and the resource string names the rule: only an empty verb or "open" is supported.

**What is real, and is fixed.** On those platforms every press threw, logged a `print.command` warning, and then told the person "your PDF viewer has no print command GroupLab can call", which blames their viewer for a platform fact. Now `PrintWindow.PrintLaunch` decides by platform:
- **Windows** asks for the shell `print` verb, and falls back to opening the file when the viewer registered no print command, as before.
- **Linux and macOS** are asked only to open the file, and the words say so: "This system has no print command GroupLab can call, so the PDF is open in your viewer."
- **No catch was added for `PlatformNotSupportedException`.** It cannot be thrown here, and a catch for an impossible exception is a claim about behaviour that is not true.

**What is verified and what is not.** The behaviour is read from the runtime source and the branch is chosen by `OperatingSystem.IsWindows()`, and a test pins both branches without starting a process. Nobody has yet pressed the button on Linux: what remains unproven there is only whether a desktop opener exists at all, which the existing failure message already covers, and which is what entry 61 section 2's VM is for.

---

## Entry 64. The line endings: nothing to discard, and the cause is a machine setting rather than the tree

`docs/NOTES-FROM-PLANNING.md` entry 64, which asked for the difference to be verified as whitespace before its fix was taken.

**Measured first, as asked.** `git status --short` lists only the untracked inbox entries. `git diff --shortstat`, `git diff -w --shortstat` and `git diff --cached --shortstat` are all empty. There are no 109 modified files, so `git checkout -- .` would have discarded nothing and was not run.

**The working copies really are CRLF**, as the entry says: `GrayImage.cs` 30 of 30 lines, `.gitattributes` 10 of 10, `RangeStatistics.csv` 594 of 594. The committed blobs are LF.

**Why they nonetheless agree, which the entry has wrong.** `core.autocrlf` is not unset. It is **true**, from `C:/Users/Airwolf/.gitconfig`, so git converts CRLF to LF when staging and back on checkout. A CRLF working copy of an LF blob is therefore not modified, and **`git add -A` cannot bake in the churn the entry warns about** while that setting holds. The warnings git prints on commit, "LF will be replaced by CRLF the next time Git touches it", are that conversion announcing itself on the files this session rewrote as LF.

**The stale lock is deleted.** `.git/index.lock.stale-claude-20260916` was 0 bytes with no live `.git/index.lock` beside it.

**The policy question is left open deliberately.** `* text=auto` in `.gitattributes` would move the normalisation from one machine into the repository, at the cost of one renormalisation commit touching every text file: exactly the churn the entry warns about, paid once on purpose instead of once by accident. It is defensible either way while every clone in use has `core.autocrlf` on, and it stops being defensible the moment a contributor whose git does not sees what entry 64 described. That is a decision for whoever owns `.gitattributes` and nothing was changed there.

**Tests:** Core 760 passing, App 36 passing, none skipped.

---

## Entry 61 section 5 item 2. A Linux tarball, built where its floor is set

`docs/NOTES-FROM-PLANNING.md` entry 61 sections 1 and 5 item 2, and entry 63 section 2, whose constraint decides where it is built.

**Reproduce:** the `linux tarball` job in `.github/workflows/ci.yml`, on every push. The artifact is `grouplab-linux-x64`.

**One format.** The tarball falls out of `dotnet publish --self-contained`, which the build already does, so it costs nothing to keep. AppImage waits for somebody wanting a menu entry, and `.deb`, `.rpm`, snap and flatpak wait for a person to ask by name, because each is a permanent obligation and there are no Linux users yet.

**What the first run produced**, measured from the artifact rather than from the log:

| | |
|---|---|
| Compressed | 90 MB, 94,107,807 bytes |
| Unpacked | 214 MB, 256 files |
| Native imaging | `libOpenCvSharpExtern.so` present |
| Application | `GroupLab.App` launcher, `GroupLab.App.dll`, `grouplab.dll` |
| Target library | 25 definitions, the twenty built-ins and the frozen Phase 0 ones |

**It is built on Linux rather than cross-published,** because `src/GroupLab.Cli/GroupLab.Cli.csproj` references the OpenCV native runtime package conditionally on the build host's platform, so only a Linux host puts the Linux native library in the output. The step fails when that library is absent rather than shipping a tarball that installs and then cannot find a marker, which would be the worst shape of failure: late, and in front of a user.

**Entry 63 section 2's floor, verified rather than assumed.** The runner reports `Image: ubuntu-24.04`, version 20260907.300.1, which is what entry 63 says `ubuntu-latest` still is. A binary built against an older glibc runs on a newer one and not the other way, so this tarball runs on 24.04 and newer, and building it on 26.04 would have raised the floor above the distribution CI itself uses. The step prints the release and the glibc version it built against, so the day `ubuntu-latest` moves the floor moves visibly rather than silently. It is not pinned to `ubuntu-24.04`, as entry 63 section 2 asks: pinning would hold the floor still while the test matrix moved, and the question planning wants asked is which of the two should move.

**Two things the first run taught, both fixed in the same commit.**
- **The figures reached only the run summary,** so reading them back meant downloading the artifact. They now go to the job log as well, which is where anybody diagnosing a build looks first.
- **The tarball carried the IBM Plex licence and not GroupLab's own.** The project file copies the font licence because the SIL Open Font License asks that every copy carry it; nothing copied `LICENSE`. GPL-3.0 section 4 asks the same of the binary, and this is the first build output meant to be handed to anybody, so the step now packs it. **The same question applies to a Windows download when one exists**, and there is no such build output yet to fix.

**What is not verified: nobody has run it.** Whether it launches on a desktop Ubuntu, and whether ICU is present, which the application needs because it reads the system region on first run, is exactly what entry 61 section 2's VM is for.

**Tests:** unchanged, Core 760 and App 36 passing, none skipped. This commit is a workflow and a file copy, with no code in it.

---

## Entry 70. Five decisions taken: LF in the repository, a status line with states, the editor's plumbing, and its matching rule

`docs/NOTES-FROM-PLANNING.md` entry 70, in its section 7 order. No layout work was started.

### Section 1: `* text=auto`, and the duplicate image deleted

`* text=auto` is the first line of `.gitattributes`, so the narrower `eol=lf` rules below it still govern the files compared byte for byte. Measured again before committing: the renormalisation staged only `.gitattributes`, both with this machine's `core.autocrlf` and with it switched off. The untracked duplicate `docs/figures/concept-assignment-editor.png` is deleted; the design target stays where it has been, `docs/figures/screens/assignment-editor.png`.

### Section 6: the print status line says which state it reports

Every message used to carry the alert style. Now each carries one of three: **success** in teal, for a saved PDF or one sent to the Windows print command; **information** in plain text, for a PDF opened in the viewer for the person to print; and **alert** in red, for a missing library, an unprintable sheet, a failed write, or nothing able to open the file. A test fails a save, sees the alert, then saves and sees success rather than a red line left over.

### Section 4: what the matching decided reaches the marking

- **`AutomaticResult.Detections`** is a list of `DetectedShot`, each a position with its whole `AssignedShot`. The `ShotAssignmentResult` travels with them, and the detector's refused candidates come through as `RejectedCandidate` rather than reaching only the trace.
- **`MarkingState.Assignment`** holds an `AssignmentReview`: the method and its reason, each detected shot's figures in inches under its marking id, the refused candidates, and the method detection used. It is state, so undo covers it, and `NeedingReview` is the concept's "2 of 26" counter.
- **The card's sentence is not stored.** `Reason` stays a note about the method; the sentence is composed where it is shown.
- **`BullAim` carries each bull's declared page position** beside its located image position (section 5), and both places they are used say why they differ.
- **Nothing new is saved in a marking file.** A sheet's page mapping is not saved, so the review lives while a detection is loaded, and a reopened marking has none until detection runs again.

### Section 3: the matching rule

Implemented in `MarkingSession`, applied after every change to the shots and on loading a detection:

| Item | As implemented |
|---|---|
| 1. A person's bull is pinned | a shot that is a shot and is manual or corrected keeps its bull |
| 2. The rest re-solve on every edit | the untouched detections are matched against the bulls no pinned shot holds, classifying on declared positions |
| 3. A cascade goes in the queue | each shot keeps the bull detection gave it, and `AssignmentReview.Moved` lists every shot whose bull now differs, for as long as it differs |
| 4. The counts rule holds live | more untouched shots than free bulls gives nearest free bull, every shot flagged, and `MethodChanged` true |
| 5. Undo restores the pins | provenance and the review are both state, so one undo takes back the reassignment, the pin and the cascade together |

**Three consequences worth knowing before the editor is drawn:**
- **A shot placed by hand is pinned to the bull it was given,** which is its nearest. A person adding a missed hole beside an automatic one therefore pushes the automatic one to another bull, and it shows as moved. That is the rule as entry 70 wrote it; if a hand-placed hole should itself be matched, the rule changes, not the code's intent.
- **A shot marked not a shot takes part in nothing.** It holds no bull against the others and is not matched.
- **The rule runs only while a detected sheet is loaded.** A marking made entirely by hand keeps the nearest-bull rule it always had.

**On loading, the rule reaches detection's own answer** when nobody's decision is in the way, since it matches the same shots against the same declared positions; the synthetic end-to-end test checks that nothing shows as moved and the method is unchanged.

**What is not done:** nothing draws any of this yet. The contested card, the amber ring with its dashed lines, the review queue and the method-change notice are the editor's presentation, which waits for Alan's report on using the application.

**Tests:** Core 768 passing, App 37 passing, none skipped. Seven new tests hold the rule: the contested shot and its figures on loading, a person's decision kept and the displaced shot shown as moved, undo restoring the pin, a deletion re-solving the rest, the counts rule switching method and back, not-a-shot left out, and a marking without a detection left alone.

---

## Entry 74 and entry 73 section 1. A chosen bull is its own fact, and sighter holes no longer enter the group

`docs/NOTES-FROM-PLANNING.md` entry 74 section 1, and entry 73 section 1, in that order as Alan set it.

### Entry 74: only a chosen bull is pinned

Entry 70's rule pinned a shot by its provenance, so a hole added by hand took its nearest bull as if a person had chosen it and pushed any detection off that bull. **Placing a hole and choosing a bull are different decisions**, so `MarkedShot.BullChosen` now records the second on its own:
- **Set only where a person chooses a bull:** `AssignBull`, which is click a hole then a bull, including choosing no bull, and `AddShot` when a caller passes a bull. No caller does today.
- **Everything else is matched,** hand-placed shots included. A hand-placed shot's baseline for "moved" is the bull it was placed with.
- **Saved in the marking file** as an optional `bullChosen` field, read as false when absent. The format version does not change.
- **One statement in entry 74 is not how the code behaves:** "a `Corrected` shot has one by definition". A detected shot also becomes corrected when it is moved or marked not a shot, neither of which chooses a bull, so `BullChosen` is not implied by `Corrected` and the rule reads only `BullChosen`.

**Entry 74's scenario, as a test:** one detection 10 dmm from bull 1, and a hole added by hand 60 dmm from it. Under entry 70 the hand-placed hole took bull 1 and the detection was pushed 390 dmm to bull 2. Now both are matched, the detection keeps bull 1, and the hand-placed hole is the one given bull 2, listed as moved from the bull it was placed with. Choosing bull 1 for the hand-placed hole then pins it, and the detection moves, which is a real result of a real decision.

**Entry 74 section 2 is recorded** in `DESIGN.md` section 13: the assignment detail does not survive a save, and the screen must distinguish "reopened, detail not available" from "nothing to review" when the queue is drawn. The file format is not changed.

### Entry 73 section 1: two pools

`ShotAssignment.Assign` takes the bulls' scoring flags. Each shot joins the pool of its nearest bull, each pool is matched on its own with section 13's counts rule applied in it, and the margin is still measured to every bull so a hole near the boundary between the rows is flagged. The automatic path and the live re-solve both pass the flags. Callers that pass none, such as the synthetic holes spike, behave exactly as before, so no committed record moves.

**On `Scan_20260916.png`**, submission 3a493942 at 600 DPI, 38 of 38 markers, the sheet Alan used:

| | Before | After |
|---|---|---|
| Holes detected | 15 | 15 |
| Assignment | one-to-one over all 28 bulls | scoring: 10 shots for 25 bulls, one-to-one; sighter: 5 shots for 3 bulls, nearest-bull |
| Shots 11 and 12 | bulls 22 and 23, a row away | sighters S1 and S2, their nearest bulls at 0.578 and 0.717 in |
| Shots in the group | 12 | 10 |
| Extreme spread | 2.224 in | **1.361 in** |
| Mean radius | 0.617 in | 0.344 in |
| Sigma | 0.492 in | 0.274 in |
| Error ellipse aspect | 2.630, major axis 62.5 degrees | **2.818**, major axis 26.7 degrees |

**Entry 73's prediction, half confirmed.**
- **Extreme spread fell well below 2.224 in, as predicted.** The two sighter holes are out of the group.
- **The ellipse aspect did not fall toward 1. It rose, to 2.818,** and its axis turned from 62.5 to 26.7 degrees. The two sighter holes had set the axis, since they sat 1.2 and 1.5 in below their assigned bulls. Without them, the ten scoring shots are themselves spread along a diagonal: shot 1 at (-0.609, -0.338) in and shot 5 at (+0.591, +0.304) in from their bulls carry most of it. An independent computation from the printed offsets gives the same 2.817 and 26.7 degrees. **So the reading of the dashed lines was right and the explanation of the aspect was not:** the elongation is in the scoring shots. Whether ten shots with this aspect say anything about the rifle is `docs/STATISTICS.md` section 7's circularity test, not something this run asserts.
- **Five sighter-row holes for three sighter bulls.** The sighter pool falls back to nearest-bull, flags all five, and the overall method is reported as nearest-bull with the reason naming both pools. Three of the five share sighter S1.

**Tests:** Core 771 passing, App 37 passing, none skipped.

---

## Entry 73 sections 2 to 8. Alan's first session, after the statistics fix

`docs/NOTES-FROM-PLANNING.md` entry 73, in its section 9 order after section 1, which is reported above with entry 74.

**Section 2: the oversize warning no longer names a cause it cannot know.** On the printed target it said "this is printed ink under the mark rather than a hole". It now gives the measurement and where the mark sits, and names every explanation: printed ink under the mark, two holes read as one, or a hole on a printed line merging with the ink. This reverses entry 40 section 1's choice to name only the ink, and the test that pinned that choice now pins the new wording.

**Section 5: the marks already scale once a calibre is set.** With a calibre and a scale, the impact ring is drawn at the bullet's diameter at the image's local scale times the zoom, never below 3 px, and its alert ring at the measured extent. Before a calibre is set there is no real diameter to draw, and the ring is a fixed 11 px. **Nothing was changed.** A ring drawn at an invented diameter would look like a calibre claim, and the honest alternatives, the detector's own measured diameter for detected shots or a centre mark that makes no size claim, are a decision for planning.

**Section 6: the shot list's rows fit, and a false positive is taken out from its row.** The row was a 230 px text button, the provenance word and Exclude, about 384 px in a column with roughly 340 inside its padding. Rows are now a grid whose text gives way first, trimmed with an ellipsis and shown in full as a tooltip. Each row gains **Not a shot**, with **It is a shot** to undo it. Delete and Not a shot were never missing from the application, only from the list: both were already in the selection panel. A test lays every row out at the column's inner width and checks nothing ends past it.

**Section 3: the off-centre hypothesis is not supported.** For each shot on `Scan_20260916.png` with calibre 0.308 in, the hole's own centre was taken as the darkness-weighted centroid of pixels darker than paper and not printed, using the registered expected artwork to exclude ink, and the reported centre's displacement from it was compared with the direction of the printed ink within 0.4 in.

| | Shots | Median displacement | Pointing at the ink, cosine above 0.5 |
|---|---|---|---|
| Flagged oversize | 5 | 0.0171 in | 1 of 5 |
| Not flagged | 10 | 0.0187 in | 2 of 10 |

The flagged marks are displaced no more than the others and not consistently toward the ink, so the detector is not pulling their centres onto the rings. **No detector change follows.** One control row is an artefact: shot 14's 0.203 in comes from its window reaching the neighbouring hole, shot 15.

**Section 7: the headline figures stay in view.** The placement line, centre from aim, mean radius, sigma and extreme spread with their intervals, and any size warnings stay in the panel; edge to edge, the angular note, the small-group size range, the error ellipse and the worst-shot test sit in a "More figures" expander, closed until opened and remembered in the settings file, as `DESIGN.md` section 19 specifies. **The 94.8% and 95.0% labels are left alone**: entry 24 chose to label each interval with its exact coverage rather than bend its endpoints to make it 95 percent, and making them match would undo that.

**Section 8: the sheet already names its own definition, and Alan's log says so.** His two sessions logged `detect.identify definition=GL-20J3-Y141-0BN3-EYME tile=0 codes=3` and no definition picker; the picker only opens when the codes cannot be read. What each session did do was **Open marking** after opening the image, which is a separate, optional step. So the flow he asked for exists, and what may have misled him is that detection waits for the Detect button. Whether it should run on opening an image is a behaviour decision, not taken here. **The printed human name** changes the renderer, every sheet printed afterwards and the committed module-sweep PDFs, and waits for planning's wording and placement.

**Section 4** waits for the label number from Alan.

---

