# Questions for the planning session

Questions going out from the Claude Code session to the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** When a genuine decision blocks you, append a dated section at the top with `Status: open`. State the question, the options with their real costs, and what you would choose and why. Commit it, push it, and stop. The answer comes back as an `open` entry in `docs/NOTES-FROM-PLANNING.md`, and once it does, change this entry's status to `answered <date>` with a one-line pointer to the notes entry that answered it. Never delete an entry.

**What belongs here.** A decision that changes the specification, changes a gate, changes geometry, or commits the project to something that is expensive to reverse. A conflict between two documents. A measurement that contradicts something written down.

**What does not.** Anything a measurement can settle, because the planning session can run measurements against the committed scans and hand back numbers. Permission for work the brief already authorises. Anything answerable by reading the documents the brief points at.

**Write it for a reader who has the repository and not the conversation.** Quote the section you are citing rather than paraphrasing it, give the numbers rather than describing them, and name the file and line where the conflict lives. A question that arrives with its evidence attached usually comes back answered in one round trip rather than three.

---

## 2026-09-14, question 11: three differences the M3 harness found between shotGroups and its own fixtures

**Status: open.** Nothing waits on it. The M3 build continues, and every affected key is reported as awaiting or disputed, never as passed.

**Where it stands.** `tests/GroupLab.Core.Tests/Statistics/ShotGroupsFixtureTests.cs` accounts for every key of the nine fixtures:

- **Compared:** 38,899 keys, including the range statistics through GroupLab's own Monte Carlo table and `compareGroups`, 0 outside section 15.3's tolerances.
- **Excluded, with the specification's or the fixture's reason:** 23,602.
- **Pending, not yet built:** none.
- **Awaiting or disputed:** 2,163 and 998 keys, for the three reasons below. The counts grew from this question's first version because `compareGroups` also takes the frame and also reports a CorrNormal CEP.

**1. The fixture cannot reproduce anything shotGroups computed from the data frame for `DFcm` and `DFinch`.**

- **The mechanism:** `groupLocation`, `groupSpread` and `groupShape` take the frame, which carries each shot's point of aim. The fixture carries `shots.x` and `shots.y` from `getXYmat(..., relPOA = FALSE)` and no aim.
- **The fixture's own evidence:** its frame-based centre disagrees with its matrix-based centre (`groupLocation.ctr` against `getConfEll.ctr`) in 10 of 10 scopes of `DFcm` and 10 of 10 of `DFinch`. For example, `DFinch` series 8 gives x = 0.3607 against 7.7067.
- **The other seven datasets:** agree in every scope.
- **Where spread is affected too:** covariance, `groupSpread.covXY` against `getConfEll.cov`, differs in the pooled scope of both. It also differs in `DFcm` series 5 but not in `DFinch` series 5, although section 15.2 calls them the same data.
- **What the harness does:** these 2,163 keys, including the two datasets' `compareGroups`, are routed to "awaiting" by that test on the fixture, not by dataset name.
- **Why it matters for the gate:** section 15.5 point 2, `DFcm` and `DFinch` agreeing after conversion, is exactly what they would show.
- **And the two are not the same data.** Every `DFcm` shot is a `DFinch` shot times 2.54 to 1e-15, but shot 242, at (15.25016, -10.90422) cm, is in series 5 of `DFcm` and series 4 of `DFinch`, so those two series hold different shots in the two frames (series 5 has 47 shots against 46). `tests/GroupLab.Core.Tests/Statistics/UnitSystemTests.cs` asserts that this one shot is the only difference, then shows GroupLab's results agree to 1e-12 after conversion with both grouped the inch file's way, and its angular results differ by exactly the rounding of 25 m to 27.34 yd, 1.2e-5. Point 2 is met on the shots; it cannot be met on the frames as shipped.

**2. shotGroups' CorrNormal CEP is looser than its own distribution, and section 15.3 compares it at 1e-8.**

- **The distribution agrees:** its CorrNormal hit probabilities, `getHitProb`, match GroupLab's Hoyt CDF to 1e-15 on every fixture.
- **The quantile does not:** under that same CDF, shotGroups' own CEPs miss their probability. On `DF300BLK` the misses are +3.7e-6, +2.9e-6 and -5.8e-7 at 0.50, 0.90 and 0.95. GroupLab's CEP meets it to 1e-12.
- **The size of the difference:** 3e-6 to 2.3e-5 relative, across 899 keys, each checked by the harness for exactly that evidence.
- **The likely cause:** a root finder with a coarse default tolerance.

**3. shotGroups' `fromMOA` for SMOA is 1 + 6.21288e-10 times the exact inverse of its own `getMOA`.**

- **Where:** in every scope of every fixture, 99 keys.
- **The tolerance it misses:** section 12.5's anchor, "1 inch at 100 yards is exactly 1.000000 SMOA", holds for `getMOA`, but the round trip misses section 15.3's 1e-12 for angular conversions.

**Four findings, handled and not questions.**

- **`getMinBBox`:** its angle is the direction of the longer side, while its width is always the first side.
- **`getMaxPairDist`:** it reports whichever tied pair R met first, which on `DFlandy04` differs from GroupLab's, at the same 0.442108583947428.
- **The fixture's MANOVA is the intercept row.** `sg_dump.R` takes `MANOVA[1, ]`, which in R's `anova.mlm` tests whether the mean over all shots is the origin, not section 8.2's test of the group centres. GroupLab reproduces that row for the gate, to 1e-13 on the five datasets whose frames are not shifted, and computes the group test separately. Row 2 would be the group test.
- **The multi-group p-values are Monte Carlo.** Every Fligner-Killeen and Kruskal-Wallis p-value is an exact multiple of 1/9999 (`DF300BLKhl`: 9554, 2385 and 6623 over 9999), so `coin` resampled for them, and like `groupShape.multNorm.p.value` no other generator can reproduce them. Their statistics match, and the harness excludes each p-value only after checking that it is such a multiple. The two-group Ansari-Bradley and Wilcoxon p-values are exact and match to 1e-15.

**Options.**

- **For 1:**
  - **A.** Regenerate the two fixtures with each shot's point-of-aim-relative coordinates as well, `getXYmat(..., relPOA = TRUE)`, as `shots.xPOA` and `shots.yPOA`. It is one R run.
  - **B.** Leave the 2,163 keys out of the gate and say so.
- **For 2:**
  - **A.** Gate the CorrNormal distribution at 1e-8 through the hit probabilities, which already pass. Require GroupLab's CEP to satisfy that distribution at 1e-12, and compare shotGroups' CEP at 1e-4 relative.
  - **B.** Keep 1e-8 on the CEP and replicate shotGroups' root finder, including whatever tolerance it happens to use.
- **For 3:**
  - **A.** Add it to section 15.4's known differences.
  - **B.** Reproduce shotGroups' constant.

**What I would choose: A, A and A.** Each keeps GroupLab exact where shotGroups is not, and keeps the gate checking something true.

**Not a question, but yours to know.** `grouplab stats coverage` measures GroupLab's BCa bootstrap covering a known truth 79.5, 89.1 and 92.7 percent of the time at 10, 25 and 50 shots, against a nominal 95. Section 6 flags only groups under 10 shots as unreliable. Nothing in section 15.5 gates it; it bears on what the interface should say beside a bootstrap interval. `docs/PHASE1-RESULTS.md` M3.1 has the table.

---

## 2026-09-14, question 10: the shotGroups fixtures M3 is gated on are not in the repository, and R cannot run here

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 18 section 1.

**What the documents say exists.**

- `DESIGN.md` line 520, Phase 2: "The shotGroups fixtures are already generated, by `tools/shotgroups/sg_dump.R`."
- `docs/PHASE1-BRIEF.md` section 5: "Generated fixtures under `test/fixtures/shotgroups/` with a README stating provenance, package version, R version and licence".
- `docs/STATISTICS.md` section 15.1: "The fixtures are **generated output**, checked in for reproducibility so that contributors need no R installation to run the test suite."

**What the repository has.**

- **Only the driver:** `tools/shotgroups/sg_dump.R`, added in `b53c606`.
- **No output:** no `shotGroups_*.json` or `.csv` anywhere in the tree, and no `test/fixtures/`.
- **No R:** `where Rscript` finds nothing.
- **Why I don't install it:** entry 2 says "Nothing needs installing on this machine".

**What the gate needs that `sg_dump.R` does not write, even once run.** This is for `docs/STATISTICS.md` section 15.5, items 1, 2, 3 and 5.

1. **The inputs.**
   - **What the dump holds:** shotGroups' outputs keyed `<function>.<component>.<row>.<col>`, and no coordinates.
   - **What GroupLab needs:** each dataset's shots, to compute its own side. That is `point.x`, `point.y`, `distance`, the units, and both `group` and `series`, since section 15.4 item 8 says the savage frames key on `series`.
   - **Licence:** these are shotGroups' data. Section 15.1's arrangement, generated fixtures with provenance in the test tree, is the one I would apply to them, but that is yours to confirm.
2. **Groups.** The script passes the whole data frame to `getXYmat` and to every function. So `DFcciHV`, `DF300BLKhl`, `DFlandy04`, `DFlandy01` and `DFsavage` each give one pooled result, not per-group results.
3. **`compareGroups`.** It is never called. The two-group and multi-group branches of section 15.2 have no reference, and the `coin` branch, section 15.4 item 6, is unrecorded.
4. **The Monte Carlo reference.** Item 5 checks GroupLab's tables against `DFdistr` "to **within 0.2 percent on the mean and 0.5 percent on the 2.5 and 97.5 percent quantiles**, for `n` from 2 to 50 and `nGroups` from 1 to 10". Nothing exports those cells.
5. **The eight datasets of section 15.2.** Each is one invocation.

**Options.**

- **A. The planning session extends the dump, runs it with R 4.3.3 and shotGroups 0.8.4, and commits the output.** This is how question 2's libapriltag corners came back.
  - **What it holds:** the inputs, per-group outputs, `compareGroups` with the `coin` branch recorded, and the `DFdistr` cells, with section 15.1's README.
  - **Where it lands:** the test project is `tests/GroupLab.Core.Tests/`, so I'd put it in `tests/GroupLab.Core.Tests/Fixtures/shotgroups/`, unless you want the brief's `test/fixtures/shotgroups/`.
  - **Cost:** one R session.
- **B. Allow R and shotGroups to be installed on this machine.**
  - **Cost:** it reverses entry 2 for a toolchain that is not part of the build.
  - **A second cost:** I would then write the fixture extension against an R API I cannot check against its documentation here.
- **C. Build M3 without the comparisons, and read them in Phase 2.**
  - **What needs nothing:**
    - sections 3.4, 9.1, 9.2, 10 and 12.5 publish exact values that depend only on `n` and constants;
    - items 4 and 6 are synthetic truth;
    - the Monte Carlo tables can be generated.
  - **Cost:** gate items 1, 2, 3 and 5 go unread. `DF300BLK`'s values quoted in sections 3.3 and 4 cannot be reproduced without its 20 shots.

**What I would choose: A, with C's build going ahead while A is run.** The brief and the specification authorise the build; only the gate reading needs the files. If you agree, say so in the answer and I will start the build without waiting for them.

---

## 2026-09-14, question 9: gate 2's 0.01 in hole-centre tolerance sits at the noise floor measured on real paper

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 18 section 3.

`docs/PHASE1-BRIEF.md` section 4.4:
- **Item 2:** "render-and-difference recovers at least 99 percent of holes with no false positives at a hole-centre tolerance of 0.01 in".
- **Item 3:** "The 0.008 in centroid noise floor measured on real paper is the number it will eventually be judged against, and a synthetic figure far below it means the synthesis is too clean."

**The measurement.** From `docs/PHASE1-RESULTS.md` M2.2, held-out seeds 1001 to 1003, run once, three sheets per row:

| DPI | Holes per bull | Holes | Within 0.15 in | Within 0.01 in | Strays | Centre error median / 95th pct (in) |
|---|---|---|---|---|---|---|
| 600 | 1 | 84 | 100.0% | 76.2% | 1 | 0.0063 / 0.0192 |
| 600 | 2 | 168 | 94.0% | 61.9% | 2 | 0.0074 / 0.0375 |
| 300 | 1 | 84 | 100.0% | 82.1% | 0 | 0.0067 / 0.0148 |
| 300 | 2 | 168 | 96.4% | 56.5% | 0 | 0.0082 / 0.0328 |

**Why the tolerance decides it, not the detector.**

- **Where 0.01 in falls:** the median centre error is the paper floor item 3 names, which puts 0.01 in near the 75th percentile of the error.
- **What 99 percent would take:** the 99th percentile of the error under 0.01 in. That is a centre error well below the floor, which item 3 says means a synthesis too clean.
- **Why a better detector can't fix it:** the synthetic truth centre is the point a lobed rim was drawn around. A centroid of a lobed star reaches that point only as closely as the lobes allow.

**Options.**

- **A. Keep 0.01 in.** The gate then fails on any synthesis that passes item 3's realism test.
- **B. Gate recall and false positives at a match tolerance, and keep centre accuracy under item 3.**
  - **The tolerance:** 0.15 in, the survey's hit tolerance. Centre accuracy is judged against paper.
  - **How the held-out run reads under B:**
    - one hole per bull meets 99 percent recall at both resolutions, but has 1 stray over three sheets at 600 DPI;
    - two per bull does not meet it, at 94.0 and 96.4 percent.
- **C. Set the tolerance from the floor, for example 0.02 in.** One hole per bull's 95th percentile is 0.019 and 0.015 in, so it would also sit near the line.

**What I would choose: B.** It separates finding a hole from locating it. Locating is then judged against paper, which no synthesis can stand in for.

---

## 2026-09-14, question 8: the sheet contradicts entry 15's reading of the table frames' EXIF, and the joint-fit lens key mixes two pixel geometries

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 16 section 2.

Entry 15 section 1: "Two of the three tags say ultrawide and one says main, so the odd one out is the 35 mm equivalent and it is wrong."

**`docs/PHASE1-RESULTS.md` M1.5 measured the opposite on three of the four table frames.**

- **Fitted alone:** `20260913_130543`, `130550` and `130554` recover 2366, 2353 and 2771 px. That is near the 2556 px their 23 mm tag gives, and far from the 1621 to 1940 px that `ultrawide1-3` recover.
- **Fitted jointly:** forced into the ultrawide's shared 1659 px, they degenerate, keeping 8, 4 and 0 of 136 corners.
- **The fourth frame:** `130559` recovers 906 px on a nearly frontal flat sheet, which constrains its focal length least.
- **Not the start:** `ultrawide1-3` give the same figures to five digits whether they start at 1444 or 2556 px.

**One explanation fits all three tags being true.** The phone took the close-up table frames on the ultrawide sensor and cropped them to the main camera's field of view. The physical focal length and f-number then describe the lens, and the 35 mm equivalent describes the pixels. A joint fit keyed on focal length and f-number (entry 6) then puts two pixel geometries into one fit, and their normalised distortion differs too. That explanation is not established. The mixing is: the shared 2.2 mm lens, k1 -0.031 and k2 +0.022, includes the four ungated table frames.

**Options.**

- **A. Key joint fits on focal length, f-number and 35 mm equivalent together.**
  - The table frames become their own group, and `ultrawide1-3` get a lens of their own.
  - It is one grouping key in `SurfaceFrames.FitByLens`.
  - It changes the M1.5 ultrawide figures through the shared lens, and they would be re-run and reported.
- **B. Keep entry 6's key, and leave out of joint fits any frame whose tags disagree with the rest of its group.** The effect on these frames is the same, but a frame with a wrong tag is lost instead of grouped.
- **C. Keep the key as it is.** The ultrawide's shared lens stays contaminated by four ungated frames.

**What I would choose: A.** A joint fit shares pixel geometry, and the 35 mm equivalent is the tag that describes it. Separately, for the protocol: record which camera took each frame, and shoot from far enough that the phone keeps that camera.

---

## 2026-09-14, question 7: three of the five marker module sweep sheets for next weekend now fail test 26f as an error

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 16 section 3.

**How the sweep came to fail.**

1. **Built on the Phase 0 sheet.** `docs/PHASE1-RESULTS.md` M0 built the sweep on `GL-CF25-LTR` as printed for Phase 0, gap 456. So its 0.5 mm sheet is the Phase 0 sheet by identifier, `GL-YCSK-DZZ1-R0VJ-4T5Y`, and the sweep carries its own control.
2. **The geometry commit moved the live sheet.** Entry 13's geometry commit, `d73b9a4`, made test 26f an error and moved the live sheet to gap 454.
3. **Rebased on the frozen definition.** The sweep's base is now `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, the first argument of `grouplab sweep module`. It reproduces all five definitions and PDFs byte for byte.
4. **Three sheets fail.** The validator now reports 26f errors on the 0.5, 0.6 and 0.8 mm sheets: `GL-YCSK-DZZ1-R0VJ-4T5Y`, `GL-683J-3ZR8-60D5-0FGG` and `GL-SEBE-5F06-GVTF-3CZK`. Their sighters sit outside the marker lattice, and the sweep renders them only by accepting an invalid definition. The 0.3 and 0.4 mm sheets carry an extra marker row and conform.

**Options.**

- **A. Print the five as they are.**
  - The sweep measures the module floor, which the sighters do not bear on, and the 0.5 mm sheet stays the Phase 0 sheet.
  - Cost: three printed sheets the validator rejects, which the sweep README and the M0 section would have to call measurement sheets, not library sheets.
- **B. Rebase the sweep on the live `GL-CF25-LTR`, gap 454.**
  - Five sheets that conform, with five new identifiers, and the M0 table and PDFs regenerated. It is a few minutes of work.
  - The 0.5 mm sheet is no longer the Phase 0 sheet, so the control sits one 2 dmm sighter change away from what Phase 0 printed.

**What I would choose: A.** For a module-floor measurement, a control identical by identifier is worth more than sighters that conform when the measurement never uses them. B is right if nothing printed should fail the validator.

---

## 2026-09-14, question 6: a sighter gap cannot make three of the four sheets conform, so test 26f cannot become an error in the geometry commit as written

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 13.

Notes entry 11 asks for one commit that sets `GL-CF25-LTR` to a sighter gap of 454, `GL-LR300-R24` and `GL-LR300-R36` to 1142, `GL-CF25-100M-A4` "per the sweep", declares `cells.sighterGap` on each, and promotes test 26f to an error. Before changing anything I ran those changes through `tools/layout/layout.py`, unmodified. **Only `GL-CF25-LTR` conforms afterwards.** The other three keep bull columns outside the lattice horizontally, which is question 4's finding 1, and no sighter gap reaches a column. Promoting 26f to an error in that commit fails three built-in sheets.

**The measurement.** Margins are how far the outermost marker centre lies beyond the outermost bull centre on each side, in dmm; negative is outside, and zero conforms under the inclusive rule. Every row places with zero layout errors and zero warnings.

| Sheet | Change | Scoring bulls | Markers | Left / right / top / bottom | 26f |
|---|---|---|---|---|---|
| `GL-CF25-LTR` | as printed, gap 456 | 25 | 34 | +190 / +190 / +190 / **-266** | fails |
| `GL-CF25-LTR` | **gap 454, entry 11** | 25 | 38 | +190 / +190 / +190 / +190 | **conforms** |
| `GL-CF25-100M-A4` | as printed, gap 480 | 25 | 32 | **-200 / -200** / +200 / +200 | fails |
| `GL-CF25-100M-A4` | any gap from 480 down to 301 | 25 | | columns unchanged | **fails** |
| `GL-LR300-R24` | as printed, gap 1219 | 30 | 35 | **-508 / -508** / +508 / **-508** | fails |
| `GL-LR300-R24` | **gap 1142, entry 11** | 30 | 40 | **-508 / -508** / +508 / +508 | **fails** |
| `GL-LR300-R36` | as printed, gap 1219 | 36 | 48 | **-508 / -508** / +508 / **-508** | fails |
| `GL-LR300-R36` | **gap 1142, entry 11** | 36 | 56 | **-508 / -508** / +508 / +508 | **fails** |

**Why no gap reaches the columns.** `layout.py` places the lattice columns half a pitch outside the outermost bull columns and drops any marker whose box crosses half the safe margin. On `GL-CF25-100M-A4`, five columns at a 400 dmm pitch on a 2100 dmm page put the outer marker columns at x = 50 and 2050, boxes 20 to 80 against a limit of 60, so both columns go. The rolls are the same at a 1016 dmm pitch.

**Two changes do make them conform**, measured the same way:

| Sheet | Change | Scoring bulls | Markers | Left / right / top / bottom |
|---|---|---|---|---|
| `GL-CF25-100M-A4` | `grid-boundary-half-1`, gap 480 | 25 | 88 | 0 / 0 / +200 / +200 |
| `GL-LR300-R24` | `grid-boundary-half-1`, gap 1142 | 30 | 113 | 0 / 0 / +508 / +508 |
| `GL-LR300-R36` | `grid-boundary-half-1`, gap 1142 | 36 | 151 | 0 / 0 / +508 / +508 |
| `GL-CF25-100M-A4` | 4 columns | **20** | 36 | +200 all round |
| `GL-LR300-R24` | 5 columns | **25** | 48 | +508 all round |
| `GL-LR300-R36` | 8 columns | **32** | 63 | +508 all round |

**Options, with their costs.**

- **A. The half lattice on the three sheets, in the one geometry commit.** Every bull brackets, no scoring bull is lost, and the scheme already ships on the 300 yard tiles, which conform at the same margin of zero. It takes the marker count to 2.75, 3.2 and 3.1 times what it was, which is more ink near the bulls and more identifiers, though R36's 151 is well inside the 587 the family holds. The outer columns become interpolated along the lattice edge rather than surrounded, which is the tiles' condition.
- **B. Drop a column.** Every bull brackets with margin to spare, but `GL-CF25-100M-A4` falls to 20 scoring bulls, below the library's 25, and the rolls lose 5 and 4 bulls. `TARGET-LIBRARY.md` section 1 would need a third documented exception.
- **C. Land `GL-CF25-LTR` now, keep 26f a warning, and design the columns separately.** Cheapest today, but it is two geometry commits where entry 9 and entry 11 ask for one, and the rolls' identifiers would change twice if their fix changes the scheme.

**What I would choose:** A, because it is the only option that makes every sheet conform without losing a bull or a library requirement, and it uses a scheme the library already ships.

**A second, smaller conflict with "does not change the schema".** The worked example of TARGET-SCHEMA.md section 4 is `GL-CF25-LTR` as printed: identifier `GL-YCSK-DZZ1-R0VJ-4T5Y`, `sighterGap` 456, and a paragraph explaining the 456. `ReferenceEncoderParityTests.Section4DocumentEncodesToTheReferenceSheetBytes` asserts that it encodes to `tools/gltd/check.py`'s bytes for the live `GL-CF25-LTR`, and four test files pin the identifier. Once the live sheet moves to 454 that test fails. Either section 4 changes, with a new identifier, or it stays as the as-printed reference and the test compares it with the frozen definition, `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, which is byte-identical to it. **I would keep section 4 as it is** and re-point the one test, so the schema text is untouched, as entry 11 intends.

**Two small corrections to entry 11, for the record.** `cells.sighterGap` is specified in TARGET-SCHEMA.md sections 3.6 and 7, not 3.10. `docs/TARGET-LIBRARY.md` lists no identifiers; what the commit changes there is the marker counts in its sheet table, 34 to 38 for `GL-CF25-LTR` and the counts of whichever option is chosen for the other three.

**What landed while this is open.** The frozen-fixture part of entry 11 does not depend on the answer, so it is committed: the three definitions the sample set was printed from are in `targets/frozen/phase0/` with a README, `grouplab spike` and the paper gate test resolve against them, a test loads, identifies and validates each, and rerunning every spike command against them reproduces every measured value in `scans/phase0/measurements/`; only the detection times in `threshold.json` differ, as they do between any two runs. Nothing in `tools/layout`, `targets/*.gltd.json` or the validator's severities has changed. Entry 11 stays open until this is answered.

---

## 2026-09-13, question 5: the wall photographs are not of a flat sheet

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 8.

Notes entry 6 describes the nine new photographs as sheet 3 "taped flat to a wall", and sets them three questions: lens, flatness, and sighter geometry. In every frame the sheet hangs from a single pin at the top centre and its edges are visibly curved (`ultrawide1.jpg` shows the fixing at the top edge, `telephoto2.jpg` an orange pin); nothing holds the lower half. The measurement agrees, and as a result the set cannot answer any of the three questions. `docs/PHASE0-RESULTS.md` section 3a has the full table; `scans/phase0/measurements/photos.json` has every corner and bull.

**The measurement, shipped pipeline, inches.**

| Set | Frames | Corners the fit keeps within 0.01 in | Corner RMS over all corners | Worst bull | Scoring bulls over the gate |
|---|---|---|---|---|---|
| Sheet 1 on a table, ultrawide | 4 | 135 to 136 of 136 | 0.0025 to 0.0038 | 0.0057 to 0.0108 | 0 to 3 of 25 |
| Sheet 3 on a wall, ultrawide | 3 | 42 to 66 of 128 to 136 | 0.023 to 0.060 | 0.070 to 0.091 | 13 to 21 of 25 |
| Sheet 3 on a wall, main | 3 | 25 to 90 of 104 to 136 | 0.014 to 0.057 | 0.048 to 0.114 | 8 to 21 of 25 |
| Sheet 3 on a wall, telephoto | 1 (two excluded) | 38 of 132 | 0.031 | 0.096 | 21 of 25 |

The misfit is largest at the free bottom edge: the two lowest marker rows sit 0.026 to 0.138 in from the fit on every wall frame, against 0.0025 to 0.0033 in on the table frames. `telephoto2`, on the longest lens, needs 0.020 in RMS from a homography alone.

**What that does to entry 6's three questions.** Lens: every lens fails by the size of the surface misfit, so a lens effect of 0.004 to 0.011 in cannot be seen; focal length does not rescue a curved sheet, which is all the set shows. Flatness: the table frames are the flatter set, 6 to 16 times better by worst bull with the same lens, so the paired comparison is inverted. Sighters: scoring bulls fail on every frame, so the sighters cannot be isolated.

**Options, with their costs.**

- **A. Reshoot sheet 3 held flat**, taped along all four edges or laid under a sheet of glass, main camera, one square-on and one about 20 degrees off-axis, whole sheet in frame. About two minutes, no new paper. It answers entry 6's questions as intended, and the pipeline, raw-row output and grouped table are already in place, so the report follows the same day.
- **B. Close Phase 0 on the evidence as it stands.** The paper gate passes, the photograph gate fails, and what is known is that the table frames fail on the sighters and by a thousandth or two on the scoring bulls, and that a hanging sheet fails by a tenth. The photograph path moves to Phase 1 with the flatness and sighter questions open. Costs nothing now; Phase 1 then starts without knowing whether a flat photograph passes.
- **C. Register a curved sheet**, by local or piecewise registration from nearby markers. It would make hanging sheets usable, which matters because users will pin targets up this way, but it goes beyond "markers, homography and bull location" in the brief, and it cannot help the sighters, which no nearby marker brackets.

**What I would choose:** A, because it is the experiment entry 6 intended and costs two minutes. C is worth recording as a Phase 1 requirement whatever A shows, because a pinned sheet is the normal case at a range. The spike report carries the photograph gate as a failure on all eleven usable frames.

---

## 2026-09-13, question 4: the bracketing rule, proposed, and three sheets a sighter gap cannot fix

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 9.

Notes entry 5 asks for the wording of a TARGET-SCHEMA.md section 7 rule and a conformance test, from the finding that the sighters of `GL-CF25-LTR` sit outside the marker lattice. The geometry change is deferred, as entry 5 now says, and nothing in `tools/layout`, `targets/` or the validator has been changed. Measuring the rule against the whole library before proposing it turned up two things it has to decide.

**Every sheet, measured on the geometry that was printed.** How far a marker centre lies beyond the outermost bull centre on each side, in dmm; negative means a bull lies outside the lattice on that side. The last column is entry 5's sighter-only criterion, reproduced by sweeping `sighterGap` down from 1.2 times the pitch in `tools/layout/layout.py`.

| Sheet | Left | Right | Top | Bottom | Smallest gap with a marker row below the sighters |
|---|---|---|---|---|---|
| GL-CF25-LTR | +190 | +190 | +190 | **-190** | **454**, 38 markers |
| GL-CF25-LTR-D | +190 | +190 | +190 | +190 | no sighters |
| GL-CF25-A4 | +190 | +190 | +190 | +190 | 456, unchanged |
| GL-CF25-100M-A4 | **-200** | **-200** | +200 | +200 | 480, unchanged |
| GL-CF30-LTR | +175 | +175 | +175 | +175 | no sighters |
| GL-RF25-LTR, GL-RF25-A4 | +127 | +127 | +127 | +127 | 304, unchanged |
| GL-RF36-LTR | +127 | +127 | +127 | +127 | 304, unchanged |
| GL-LR25-TAB, GL-LR25-A3 | +254 | +254 | +254 | +254 | 609, unchanged |
| GL-LR30-TAB | +254 | +254 | +254 | +254 | 609, unchanged |
| GL-LR300-T | **0** | **0** | **0** | **0** | no sighters |
| GL-LR300-TA4 | **0** | **0** | +508 | **0** | no sighters |
| GL-LR300-R24 | **-508** | **-508** | +508 | **-508** | **1142**, 40 markers |
| GL-LR300-R36 | **-508** | **-508** | +508 | **-508** | **1142**, 56 markers |
| GL-LR300-R42 | +508 | +508 | +508 | +508 | 1219, unchanged |

The GL-CF25-LTR bottom figure is before the fix; at 454 it becomes +190. Entry 5's table is reproduced exactly.

**Finding 1: a sighter gap cannot bracket three sheets.** On `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` the outermost bull columns sit outside the lattice horizontally, and on R24 and R36 that stays true at a gap of 1142. The mechanism is the one entry 5 found for the row: the lattice column beyond the outermost bulls is dropped by the half-safe-margin edge test. On `GL-CF25-100M-A4` the columns would be at x = 50 and 2050, whose boxes reach 20 and 2080 against limits of 60 and 2040. A rule worded "the lattice must bracket every bull" makes these three sheets non-conforming, and the fix is not a declared sighter gap. It would move the bull grid or the page, which is a geometry decision.

**Finding 2: the tiles sit exactly on the edge.** On `GL-LR300-T` and `GL-LR300-TA4` the outermost markers share coordinates with the outermost bulls, a margin of 0. They are bracketed if the rule is inclusive and not if it is strict.

**Finding 3: GLTD-B does not carry `sighterGap`.** Section 5 has no field for it, so a definition decoded from a sheet's codes loses the declaration and raises test 23's warning on every decode of the three changed sheets, which section 3.6 says the field exists to prevent. Either the body gains the field, or test 23 is scoped to documents that were not decoded, or a decoded sighter gap that brackets is exempt.

**Proposed wording, for section 7**, after the sighter-gap rule:

> **The fiducial lattice must bracket every bull.** Every bull centre, sighters included, must lie on or inside the rectangle bounded by the outermost surviving marker centres. A bull outside it is interpolated on a flat scan and extrapolated on anything that is not flat, and the Phase 0 photographs measured the cost: the sighters of GL-CF25-LTR, one dropped marker row outside the lattice, were the worst bull on three photographs of four. Where a derived scheme leaves the sighter row outside, a generator shortens the sighter gap from 1.2 times the pitch, one dmm at a time, until the lattice brackets, and declares `cells.sighterGap`.

**Proposed conformance test**, numbered to sit with the other layout tests:

> 26f. A bull centre outside the rectangle bounded by the outermost surviving marker centres is an error.

**Options for the two decisions.**

- **Inclusive or strict.** Inclusive, as worded above, keeps the tiles conforming; a bull on the lattice's edge is interpolated along that edge. Strict would also fail both tiles, whose only fix is a denser scheme. **I would choose inclusive.**
- **Error or warning, given finding 1.** As an error, three more sheets need a geometry change before the rule can land. As a warning first, it can land with the sighter fix and name the three. **I would choose a warning until the three sheets are fixed, then an error**, so the rule is not blocked on geometry nobody has designed yet.

---

## 2026-09-13, question 3: PHASE0-PRELIM's split of the paper error belongs to its centroid

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 4.

`docs/PHASE0-PRELIM.md` section 3: "**Not the measurement method.** The result is unchanged across three window radii, and a symmetric estimator applied to a symmetric object cannot manufacture a spatially structured field." Section 5a: "roughly 0.0019 inches of it is systematic and reproducible across sheets, and roughly 0.0010 inches is random from sheet to sheet." DESIGN.md section 21 repeats both figures.

The spike reproduces the preliminary centroid and measures a second locator against it on the same registration. The document is yours, so this is raised rather than edited.

**The measurement**, sheets 1 to 3 of `GL-CF25-LTR` at 600 DPI, the same homography for both locators, inches:

| Locator | Single sheet mean / worst | Systematic mean / worst | Random RMS | Systematic after a quadratic over the page, mean / worst |
|---|---|---|---|---|
| Thresholded centroid, the preliminary method | 0.00217 / 0.00431 | 0.00201 / 0.00374 | 0.00103 | 0.00147 / 0.00314 |
| Edge fit to the declared disc radii, shipped | 0.00132 / 0.00316 | 0.00128 / 0.00279 | 0.00047 | 0.00039 / 0.00071 |

The edge fit was chosen on the synthetic raster before it saw paper, as `docs/PHASE0-SPIKE-BRIEF.md` section 5 requires: 0.00022 in worst at 300 DPI and 0.00013 at 600, against the centroid's 0.00069 and 0.00025.

**What that does to the written account.**

1. **The method was part of the limit.** Stability across mask radii of 90, 100 and 110 dmm could not show otherwise, because all three masks hold the same ink: the inner ring and the dot. The edge fit also uses the outer ring and locates edges at the midpoint of their own local ink and paper levels, so ink density does not move it.
2. **The split is about 0.0013 in systematic and 0.0005 in random**, not 0.0019 and 0.0010.
3. **Most of the systematic part is smooth.** A quadratic over the page leaves 0.0004 in mean and 0.0007 worst. The prize a printer calibration could claim, section 6 of the brief's last item, is most of the systematic field rather than a fraction of it.
4. **The field is still paper-fixed.** The rotated rescan correlates with sheet 2 at +0.63 under the edge fit, against -0.11 for the scanner-fixed prediction, so section 5a's conclusion stands. The gate conclusion stands too: five thousandths holds with more margin.

**Options.** A: amend `docs/PHASE0-PRELIM.md` and the DESIGN.md section 21 figures with a dated note citing the spike, leaving the original text. B: leave both as a record of what the scratch measurement showed, and let `docs/PHASE0-RESULTS.md` carry the corrected figures. **I would choose A**, because DESIGN.md is what the next phase reads, and it currently states a random component twice the measured one.

---

## 2026-09-13, question 2: libapriltag's corners, to re-rank the detectors with the Phase 0 locator

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 3.

Notes entry 2 asks for measurement 8 of `docs/FIDUCIAL-DECISION.md` section 10 to be re-run with the Phase 0 bull locator and the ranking reported. Only the OpenCV half can be re-run here: nothing was installed, as entry 2 instructs, so libapriltag does not run on this machine.

**What would settle it.** For each of `gl-cf25-ltr-1-600-dpi.png`, `gl-cf25-ltr-2-600-dpi.png`, `gl-cf25-ltr-3-600-dpi.png`, `gl-cf25-ltr-2-600-dpi-rot180.png` and `gl-cf25-ltr-1-300-dpi.png`, the libapriltag detections from the same run as entry 2, committed as `scans/phase0/apriltag-corners.json`:

```
{ "<image file>": [ { "id": 0, "corners": [[x, y], [x, y], [x, y], [x, y]] }, ... ], ... }
```

Corners as `pupil-apriltags` returns them, in its own winding and pixel convention. The harness applies the conversion entry 2 records (winding reversed, no rotation) and fits each detector's corners through the same registration and the same shipped edge-fit locator. No decision is needed, only the file.

---

## 2026-09-13, question 1: the photograph gate fails on all four photographs, and the lens is not the main suspect

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 5.

`docs/PHASE0-SPIKE-BRIEF.md` section 7: "If the photograph gate fails, **the lens is the first suspect, not the code.** Two frames from the main camera settle it in about a minute". It fails, it was diagnosed before anything else, and the diagnosis points somewhere else, so the choice of what to do next is yours.

**The result, shipped pipeline** (homography with radial distortion, edge-fit locator), inches:

| Photograph | Markers | Corner residual RMS | Homography alone RMS | Distortion at the frame edge | Bull mean / worst | Worst bull |
|---|---|---|---|---|---|---|
| `20260913_130543.jpg` | 33/34 | 0.00319 | 0.00343 | 0.992 in | 0.00229 / 0.00530 | 15 |
| `20260913_130550.jpg` | 32/34 | 0.00329 | 0.00390 | 0.344 in | 0.00262 / 0.00973 | S3 |
| `20260913_130554.jpg` | 34/34 | 0.00370 | 0.00387 | 0.370 in | 0.00309 / 0.01083 | S3 |
| `20260913_130559.jpg` | 34/34 | 0.00248 | 0.00297 | 0.171 in | 0.00198 / 0.00922 | S3 |

**What it is not.**

- **Not the printer.** The photographs' bull fields correlate with sheet 1's own 600 DPI scan field at -0.07 to +0.33, where scans of different sheets correlate at +0.85 to +0.94. The error is added by the photograph path.
- **Not a lens term the model misses, nor any smooth global warp.** Refitting with three radial coefficients and a free distortion centre, or a quadratic or cubic warp on top of the homography, evaluated leave one marker out, improves the corner residual on no photograph consistently and brings no photograph inside the gate: the best worst bull, whichever model gives it, is 0.0050, 0.0062, 0.0063 and 0.0076 in.

**What it is, as far as the data goes.**

1. **The sighters are extrapolated.** In `targets/GL-CF25-LTR.gltd.json` the lowest marker row is at y = 8.854 in and the sighter row at y = 9.902 in. The three sighters are the only bulls on the sheet outside the marker lattice, by 1.05 in, and they are the worst bull on three of the four photographs. On a flat scan this costs little (S3 is also the worst bull on sheets 2 and 3, at 0.0032 and 0.0029 in); on a photograph it amplifies whatever the planar model gets wrong.
2. **The sheet is not one plane.** Registering each bull from only its nearest 6 or 8 markers, instead of the whole sheet, brings the worst scoring bull from 0.0053 to 0.0035 in, 0.0066 to 0.0029, and 0.0034 to 0.0021 on three photographs, and leaves the fourth at 0.0057. The sighters stay at 0.008 to 0.013 in whatever the markers, because every choice still extrapolates to them. In all four frames the sheet lies on a table rather than pinned to a wall as `docs/PHASE0-PRINT-PROTOCOL.md` section 7 asks, and a free sheet of paper does not lie flat.
3. **The lens reading is uncertain.** EXIF says f/2.2 and 2.2 mm, which reads as the ultra-wide, but also a 35 mm equivalent of 23 mm, which is normally the main camera. The fitted distortion at the outermost marker is 0.004 to 0.011 in, so the lens term is real but it is being fitted.

**Options, with their costs.**

- **A. Two frames with the main camera of sheet 3, the control, taped flat to a wall**, one square-on and one off-axis, per protocol section 7. About a minute, no new paper. It separates flatness and lens from the sighter geometry: if the scoring bulls then pass and only the sighters fail, the geometry is the finding.
- **B. Put markers below the sighter row.** This is a change to the `grid-boundary-1` placement in `tools/layout`, which the brief says not to touch, and it changes the derived marker lattice of every sheet with a sighter band. Expensive, and it should wait for A.
- **C. Gate photographs on scoring bulls only.** A redefinition of the gate. Not recommended without A; with A it may still be the wrong answer, since a sighter is still a bull a shooter fires at.

**What I would choose:** A first, then B or C with its result. The spike report carries the photograph gate as a failure, not as a pass on a narrower definition.
