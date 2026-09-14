# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## 2026-09-14, entry 16: two corrections to entry 15, both mine, and the cone goes before M2

**Status: open.** Answers questions 7 and 8, and decides the order.

### 1. Entry 15 section 4 was wrong, and it was wrong for an instructive reason

The lens is not what limits the mounted frames. You measured it: the M1 fit already shared one lens across all six main-camera frames, holding it fixed moves the worst bull by at most 3 percent and changes no verdict, and on synthetic sheets holding a lens rescues nothing a free lens fails.

**My evidence was real and my inference was not.** `main2`'s inflated k1 of -0.1597 and k2 of +0.3165 is a true fact about **Phase 0's per-frame planar fit**, which is what `photos.json` records, and in that model the lens does absorb bend, which is why the bent frame with 26 markers fits a wild lens where the flat frame with 25 fits a sane one. I then carried that straight across to the surface fit without checking what the surface fit does, and the surface fit had never used a per-frame lens. I read one model's output and drew a conclusion about a different model. That is the same error as reading a summary instead of the raw rows, one level up, and I have spent this project telling you not to make it.

**Corner noise deciding the synthetic gate between 1 and 2 px per axis is the more useful finding**, and it leads directly to section 5.

### 2. Question 8: accepted, and it corrects entry 15 section 1 as well

You are right that grouping by physical focal length and f-number mixes two pixel geometries, and right to add the 35 mm equivalent. **The reason is better than either of us said, and it means neither EXIF tag is wrong.**

Entry 15 said the table frames' 35 mm equivalent of 23 was the odd tag out because two tags of three said ultrawide. The distortion in `photos.json` says otherwise:

| Frames | `focalLengthMm` | `focalLength35mm` | fitted k1 |
|---|---|---|---|
| `ultrawide1-3` | 2.2 | 13 | -0.0296, -0.0765, -0.0679 |
| `20260913_1305xx` | 2.2 | 23 | **+0.0097, -0.0020, +0.0049, +0.0034** |

Four frames on the same physical 2.2 mm lens at the same f/2.2 show essentially **no distortion**, while three on that lens show a lot. A lens does not change. What changes is which part of its image circle is used. **The table photographs are a cropped or digitally zoomed ultrawide**: the physical focal length of 2.2 mm is correct, the 35 mm equivalent of 23 is correct as the effective field of view after the crop, the ratio 23 over 13 is 1.77 which is the crop factor, and a centre crop is exactly the low-distortion part of an ultrawide's frame. Both tags are true and they describe different things.

**So do not warn when they disagree.** Entry 15 asked for that warning and it would fire on every cropped or zoomed photograph a user ever takes, which will be a great many of them. The disagreement is information, not corruption. Group on the pixel geometry, which is what the 35 mm equivalent and the image width give you, and let the physical focal length be what it is.

**This is worth a line in the results document beyond the fix**, because it is a product finding: a phone that crops keeps the physical lens tag and changes the equivalent, so anything in this pipeline that reasons about a camera has to reason about the equivalent and the image width together, never about the physical focal length alone.

### 3. Question 7: print the marker-size sheets as they are

Agreed, and the reasoning is entry 11's. Those sheets are measurement inputs, not library sheets. The sweep measures the printer's dot gain against marker module size, and whether a sighter row is bracketed has nothing to do with dot gain. Changing them to conform would give all five new identifiers and would break the one property that makes the set cheap: the 0.5 mm sheet **is** the frozen Phase 0 sheet by identifier, so it carries its own control and inherits a second print session's worth of scans for free. Keep that.

Mark them the way the frozen fixtures are marked, in the sweep's README: these are inputs to a measurement, they are expected to fail test 26f, they are not to be brought into conformance, and here is which live sheet supersedes the geometry they are built on. **A repository that ships definitions failing its own conformance test needs that said out loud in every place it happens**, which now includes `targets/frozen/phase0/` as well. Narrowing the frozen sheet's exemption to exactly its three sighters rather than exempting it wholesale was the right call and is the pattern to follow.

### 4. Entry 13, and the row it cited

The roll check came back negative, so 1142 stands. That is what the check was for and I would rather have asked and been told no.

**The M1.2 shuffle correction does not undermine entry 13.** My fifth argument for option A cited the "23 at random" row for the proposition that markers spread across a sheet hold the fit where markers missing from one region do not. The corrected finding is that the rows were always the top 23 and that losing one region hurts, which **is** that proposition, stated more directly than the mislabelled row stated it. The argument is unchanged and option A stands on it. Finding the bug and relabelling without changing the numbers is the right handling; note in M1.2 that entry 13 cited it, so a reader tracing the decision lands on the correction rather than on the original claim.

### 5. The cone goes next, before M2, but measure one thing first

**Before building anything, put the mounted frames on the noise sweep.** You have found that with every marker present the synthetic gate breaks between 1 and 2 px of corner noise per axis, whatever the lens. `surface.json` now contains, for every mounted frame, the post-fit corner residual. Convert it to pixels per axis and place each frame on that sweep.

- **If the mounted frames sit above 2 px post-fit, no surface model will bring them inside the gate**, because the gate is already lost to corner quality before shape is considered. The limit would then be detection on a foreshortened, defocused, distorted frame, which is Phase 1 inheritance item 2 of `PHASE0-RESULTS.md` section 8, and the cone would be work aimed at the wrong target.
- **If they sit near or below it, the shape is the limit** and the cone is the right next step.

That is one table from data you already have, it costs nothing, and it decides whether the cone is worth building at all. Report it either way.

**If the answer says shape, build the cone**, on the same discipline as the cylinder: a general developable surface with a ruling direction that varies rather than a single angle, so it covers both the twist and the cone your shape test cannot separate. Synthetic truth first, swept until it breaks, then the seven mounted frames once, with the three flat frames as the control they have been throughout. Your F test on the two shapes a cylinder cannot take, at 9 to 77 on six mounted frames against 0.8 to 2.9 on the flat ones, is exactly the evidence entry 15 asked for before anyone built a cone, and it is a better test than the one I described.

**Stop after that.** If the general developable does not bring the mounted frames inside 0.005 in, report it and go to M2. Do not try a third surface model. The fallback is the piecewise registration already diagnosed in `PHASE0-RESULTS.md` section 4.5, and a mounted gate that needs more than a developable fit is a finding about the product rather than a modelling problem to keep pushing at. I would rather have a clean negative this week than a fourth model next week.

**Why before M2**, since M2 is unblocked and this is not: the mounted gate is the product requirement, Alan said so himself and entry 10 records it, and the paper protocol for next weekend has to say what photographs to ask for. Most of that protocol does not depend on this, and I am writing it now. The part that does is whether the photographs are the benchmark for a surface model that works, or evidence for one that does not, and that is worth knowing before he stands in front of a target board.

### 6. Everything else in that report

Entries 13, 14 and 15 actioned, 554 tests, 22 definitions with 0 errors, the geometry commit reviewed and merged from its own worktree, the merge conflict resolved by keeping both changes, and two questions raised that were not blocking and were raised anyway. That is the working pattern holding up under a week of being told to change direction, and it is worth saying so.

---

## 2026-09-14, entry 15: the crash is on ungated frames, the gate is already measured, and the lens is eating the bend

**Status: actioned 2026-09-14.** Section 3 is in `docs/PHASE1-RESULTS.md` M1.5, where the run shows the seeding was not the cause and the lens key puts two pixel geometries in one fit; section 4 is M1.7: the flat-fitted lens changes the mounted frames by 3 percent at most, and six of seven carry a shape no generalised cylinder takes. Originally: answers the M1.4 crash and the question at the end of it. Do this after entry 14's rule and alongside entry 13.

### 1. Your EXIF hypothesis is right, and here is the evidence from `photos.json`

| Frames | `focalLengthMm` | `fNumber` | `focalLength35mm` |
|---|---|---|---|
| `ultrawide1-3` | 2.2 | 2.2 | **13** |
| `20260913_130543`, `130550`, `130554`, `130559` | 2.2 | 2.2 | **23** |
| `main1-3`, `main_flat1-3` | 6.25 | 1.7 | 23 |
| `telephoto1-3` | 7 | 2.4 | 69 |

The four table photographs carry the ultrawide's physical focal length and the ultrawide's f-number, and the main camera's 35 mm equivalent. Two of the three tags say ultrawide and one says main, so the odd one out is the 35 mm equivalent and it is wrong. Grouping on `focalLengthMm` and `fNumber` correctly put all seven 2.2 mm frames in one joint fit; seeding the focal length per frame from `focalLength35mm` then started three of them at 2556 px and four at 1444 px for the same physical lens, a factor of 1.77 inside one fit. That is the degenerate start.

**The fix is a seeding rule, not a model change.** Within a joint-fit group there is one lens, so there is one starting estimate: take it from `focalLengthMm` and the sensor dimension, or from the group's median, and never per frame. Then cross-check the two EXIF fields and **warn when they disagree**, naming the frame.

**This is the DPI finding again, and it belongs next to it.** `SAMPLE-NOTES.md` already says of scanner DPI that the problem is not that metadata lies but that there is no way to know when it does without something of known size in the frame. Here there is something of known size in the frame, because it is a GroupLab target, so a recovered focal length that disagrees with EXIF by a factor of 1.77 is detectable rather than merely suspected. Say so in the results document: **the sheet validates its own EXIF**, and that is a property no generic photograph has.

### 2. The crash is on four frames that are not in any gate, and every gated frame finished

`photos.json` marks all four `20260913_1305xx` frames `photographGate: none`, `gated: false`. They are sheet 1 lying loose on a table, kept as a diagnostic, and `PHASE0-RESULTS.md` section 7 already records the decision not to count them as flat. The three that crashed are among them.

**So all seven mounted-gate frames and all three flat-control frames completed.** The mounted gate and its control are measured. What is missing is not the measurement but its raw rows, because the command aborted before writing `surface.json`, and I check reports against raw rows rather than against summaries.

**That changes the order.** You do not need to understand the degenerate fits before you can report. You need the run to survive them.

### 3. Do these, in this order

1. **Add the locator guard**, and treat it as a requirement rather than a workaround. A bull whose edge profile has fewer than three samples fails that bull, with a reason recorded in the trace, and the sheet carries on. A user will feed this application a photograph where registration goes wrong, and the answer has to be a named failure on one bull, not an exception that loses the other twenty-seven. `EdgeFitBullLocator` should have no input that throws.
2. **Fix the focal-length seeding** per section 1, with the disagreement warning.
3. **Re-run `grouplab surface frames`**, foreground, per entry 14. Everything should complete, `surface.json` should be written, and the four table frames either fit or fail with reasons.
4. **Write the M1 report.** Send it whatever the numbers are.
5. **Then** add the synthetic regression test: two frames with disagreeing focal-length starts in one joint fit. Build it to lock the fix, not to investigate the bug, because section 1 has already settled the cause.

### 4. The result that matters is not the crash

Worst scoring bull 0.012 to 0.092 in against a gate of 0.005, better than Phase 0 on five of seven frames and worse on two. The synthetic sweep put a one inch bow at 0.00040 in. Real frames are thirty to two hundred times worse than synthetic at bends the sweep says are comfortable, and **that gap is the finding**, not the pass or fail.

Two things say the machinery is sound, so the gap is somewhere specific. The F test declined the bend on all three flat frames and handed Phase 0's figures back unchanged, which is the control doing exactly what brief section 3.4 asked. And the surface keeps 66 to 120 corners where the planar model kept 25 to 90, so it is explaining real geometry rather than merely fitting noise.

**My first candidate is that the lens and the bend are competing for the same error, and `photos.json` already shows it happening.** Among the main camera's six frames, five fit a consistent lens and one does not:

| Frame | Flat or bent | Markers | k1 | k2 |
|---|---|---|---|---|
| `main_flat1` | flat | 34 | -0.0512 | +0.0659 |
| `main_flat2` | flat | **25** | -0.0436 | +0.0531 |
| `main_flat3` | flat | 23 | -0.0721 | +0.1058 |
| `main1` | bent | 34 | -0.0729 | +0.0770 |
| `main3` | bent | 27 | -0.0532 | +0.0699 |
| **`main2`** | **bent** | **26** | **-0.1597** | **+0.3165** |

`main2` fits a lens three to five times the rest of the same physical camera. It is not a marker-count effect: `main_flat2` has 25 markers, one fewer, and fits a normal lens. The difference is that `main_flat2` is flat and `main2` is bent. **On a bent sheet the radial lens term absorbs the bend**, because both are smooth and roughly radial over the marker region, and `main2` is one of the two frames the surface fit made worse. The same pattern shows on `telephoto1` and `telephoto3`, which are excluded for other reasons.

**So try freezing the lens.** It is a property of the camera and not of the frame. Fit one lens per camera on the frames where it is well determined, which is the flat frames and the full-marker frames, confirm it sits inside Phase 0's measured range of k1 -0.044 to -0.073 and k2 +0.053 to +0.106 for the main camera, then hold it fixed while fitting the bend. That removes a degeneracy rather than adding freedom, which is the same reasoning that chose a developable surface in the first place.

**Sweep what the synthetic run never swept.** The sweep in M1.2 varied bend, marker count, keystone and focal start, all at one noise level, 0.52 px per axis taken from `main_flat1`. It never varied noise, and it never varied the two together. At fixed bend, sweep corner noise from 0.5 px to 5 px, and sweep marker coverage that is clustered rather than random, with the lens free and with the lens frozen. If frozen-lens results hold where free-lens results fall apart, that settles it in one run.

**Twist is still the other candidate and I am not dropping it.** The sweep puts the break at a quarter inch, the frames are a sheet hanging from one pin, and that twists. The raw rows will tell you: a cylinder fitted to a twisted sheet leaves a residual that rotates systematically along the rulings. Look for that pattern in `surface.json` once it exists, and report it, before anyone builds a general developable surface. If it is twist and not the lens, the cone comes next; if both, the lens is still worth freezing first because it is cheaper and it is correct regardless.

### 5. Yes to entry 13, in parallel

Option A and the `CONTRIBUTING.md` rule are independent of all of this. Land them.

### 6. A correction from me

I told Alan the earlier run had been abandoned and that nothing was being computed. You have established that it was alive at ten and a half minutes and merely far too slow to finish. What I could see was that nothing had been **written**, and I stated it as though nothing was **running**. The conclusion happened to lead to the right action, since the run could not have finished and killing it was correct, but the inference was wider than the evidence and I should have said that a long job with no progress output is indistinguishable from a dead one. Which is, in the end, the better argument for entry 14's rule than the one entry 14 actually makes: **print progress not because it is tidy, but because without it nobody, including you, can tell a slow run from a stopped one.** Add that sentence to the `CONTRIBUTING.md` rule when you land it.

Caching the mapping's rotation and bend profile, holding it to 1e-6 px with a test, and stating that the numbers are unchanged, was the right response to finding it too slow.

---

## 2026-09-14, entry 14: the real-frame run is not running, and the turn that started it is the reason

**Status: actioned 2026-09-14.** `grouplab surface frames` ran in the foreground with a progress line per frame, its crash recorded at `ab42e33` and the completed run and M1 report at `d574a7e`, and the standing rule is in `CONTRIBUTING.md` under "Long-running steps" with entry 15 section 6's sentence appended.

**What the repository shows.** `scans/phase1/measurements/surface-synthetic.json` and `surface-rendered.json` are written. `scans/phase0/measurements/surface.json`, which `grouplab surface frames` produces, **does not exist**, and nothing under `scans/` or `docs/` has changed since `docs/PHASE1-RESULTS.md` was last written. Alan reports the session is idle. So the run that was described as "still going" is not going: it was started in a turn that then ended, and it went with the turn.

**This is the second time.** The same thing happened during Phase 0a, and Alan noticed it then too. It is worth fixing as a habit rather than as an incident, so the standing rule is at the bottom of this entry.

**Do this now.**

1. **Run `grouplab surface frames` in the foreground, in one turn, and wait for it.** Do not background it, do not schedule it, and do not end the turn expecting to be told it finished. Nine frames at 4000 by 3000, six starting ruling angles each, with detection, reclassification and a refit at each stage, is genuinely long. Long is fine. A turn that sits there for twenty minutes and returns a number is worth more than four turns that return a status.
2. **Print progress per frame as it goes**, one line each: frame name, markers matched, corners kept, fitted deflection, worst scoring bull, whether the F test kept the bend. If it dies part way, that output says which frame killed it, and it costs nothing.
3. **If it fails, report the failure and stop.** Do not retry it unchanged, and do not work around it by reducing the frame set or loosening the fit. A crash on a real frame after a clean synthetic sweep is itself a result and I want to see the error rather than a recovered run.
4. **Then M1 reports**, per the brief. That report is wanted as soon as it exists because the paper protocol depends on it.

**What I am expecting, so you know what would be surprising.** `PHASE1-RESULTS.md` M1.2 says the model holds to two inches of bow and a keystone of 0.70, and that what breaks it is a twist of a quarter inch or more, or markers lost from where the bend is. The nine pinned frames are a sheet hanging from a single pin, which is the geometry most likely to twist rather than bow, and `PHASE0-RESULTS.md` section 3a records that they keep only 25 to 90 of 104 to 136 corners. So a frame or two failing on twist would be consistent with what you have already measured, and would point at the general developable surface of brief section 3.1 rather than at anything being wrong. Report whichever way it comes out, per brief section 3.3, and do not adjust the model to improve a real-frame number.

**The standing rule, which belongs in `CONTRIBUTING.md` and should be added there in the same commit.**

> **Do not end a turn waiting to be notified that something finished.** If a step is long, run it in the foreground and wait for it in that turn, printing progress as it goes. If the tooling offers a real background mechanism with a handle you can poll, poll it in the same turn until it completes. A turn that ends while work is outstanding does not pause the work, it abandons it, and the session then reports progress that is not happening. The only correct reason to end a turn with work outstanding is a blocking question in `docs/QUESTIONS-FOR-PLANNING.md`, and that is a stop, not a wait.

**Everything else about how you are working is right**, and this is a mechanical fault rather than a judgement one. Measuring entry 11's change before making it, parking the blocked instruction and carrying on with the independent one, committing the surface fit at `d78c17c` before any real frame was run, and making both M1.3 changes on synthetic evidence with the reasons recorded, are all exactly right.

---

## 2026-09-14, entry 13: question 6 answered, option A, and entry 11 was wrong to bundle the promotion

**Status: actioned 2026-09-14.** Option A landed in one geometry commit after the roll check was measured first (at 1219 the half lattice still leaves both rolls' sighters 508 dmm below it, so 1142 stands): `GL-CF25-LTR` at gap 454 with 38 markers, `GL-CF25-100M-A4` on `grid-boundary-half-1` with 88, `GL-LR300-R24` and `GL-LR300-R36` on `grid-boundary-half-1` at gap 1142 with 113 and 151, `cells.sighterGap` declared on the three departures, test 26f an error, and TARGET-SCHEMA.md section 4 kept as printed with its parity test on the frozen definition.

**You are right and entry 11 was wrong.** It asked for one commit that fixed four sheets and promoted test 26f to an error, and it asserted that a sighter gap did the job on all four. Question 4's own finding 1 said otherwise a day earlier, in this file, and entry 9 accepted it: the outermost bull columns of `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` lie outside the lattice horizontally, and no sighter gap reaches a column. I carried the sighter fix forward and did not carry the column finding with it. Measuring the change before making it, and stopping, was the right call, and the margin table is the form this question should take.

**Take option A. The half lattice on the three sheets, in the one geometry commit.**

The commit, then:

| Sheet | Lattice | Sighter gap | Markers |
|---|---|---|---|
| `GL-CF25-LTR` | unchanged | 456 to **454** | 34 to 38 |
| `GL-CF25-100M-A4` | to `grid-boundary-half-1` | **unchanged at 480**, its sighters already bracket | 32 to 88 |
| `GL-LR300-R24` | to `grid-boundary-half-1` | 1219 to 1142, subject to the check below | 35 to 113 |
| `GL-LR300-R36` | to `grid-boundary-half-1` | 1219 to 1142, subject to the check below | 48 to 151 |

**Check whether the rolls still need the gap change at all.** Your table measures `grid-boundary-half-1` at gap 1142 and not at 1219, and the half lattice adds rows as well as columns. If it brackets the sighters at the conventional 1219, which is 1.2 times the pitch, then leave the gap alone: two fewer departures from the library convention, two fewer `cells.sighterGap` declarations, and two sheets whose geometry moves for one reason instead of two. Measure it before you change it. If 1219 does not bracket, 1142 stands.

**Why A, in the order the reasons matter.**

1. **`grid-boundary-half-1` is not a new scheme, it is the scheme that passed the paper gate.** The 300 yard tiles ship it, they were printed, scanned and measured in Phase 0, and they passed at 0.00325 in worst on tile 3. Option A commits three sheets nobody can print this cycle to a lattice that has already been through paper. That is the strongest argument and it is worth more than the counting arguments.
2. **Margin zero is already the accepted standard**, settled in entry 9: bracketing is inclusive, and the tiles conform at exactly zero. The outer columns becoming interpolated along the lattice edge rather than surrounded is the tiles' condition, not a new concession.
3. **Option B breaks a library requirement.** `GL-CF25-100M-A4` at 20 scoring bulls is below the 25 Alan asked for, and a third documented exception to buy a geometry fix is the wrong trade. Rejected.
4. **Option C is two geometry commits and two identifier changes on the rolls.** Entry 9 and entry 11 both ask for one, and the reason is that each change invalidates anything printed from the old geometry. Rejected, and see the deadline note below for why the pressure C relieves does not exist.
5. **`docs/PHASE1-RESULTS.md` section M1.2, measured today, is new evidence for A.** Your own marker-count sweep found that 23 markers spread across a sheet hold the fit and 23 at random do not, "because a random 23 can leave a region unconstrained". The three sheets needing the half lattice are the largest in the library, they bend most when mounted, and they currently carry the sparsest lattices relative to their size. A surface fit on a 300 yard roll is exactly where an unconstrained region is most likely and most costly.

**The counting arguments against A, weighed and not dismissed.** Tripling the markers puts more of them within a plausible miss of a bull, so on the 100 metre sheet at a 200 dmm half pitch some will be shot through. That is a gain rather than a loss: measurement 1 of Phase 0 showed the worst bull still improving above sixteen markers with 34 available, and M1.2 puts the failure point at sixteen or fewer, so 88 markers absorb damage that 32 would not. The ink is about two square inches of marker on an A4 sheet. The sheets will look busier, and that is a real change to how they look rather than only to how they measure; it is a change I am making on Alan's behalf and he can overrule it, which is why it is in his hands as well as yours.

**There is no print deadline on any of the three.** Alan has no plotter, so `GL-LR300-R24` and `GL-LR300-R36` cannot be printed here at all, and `GL-CF25-100M-A4` needs A4 stock he is unlikely to have. Nothing in next weekend's session prints them. `GL-CF25-LTR` is the only sheet of the four with a print date, and its fix is the uncontested one. So take the time to measure rather than to hurry, and if the roll check turns up something that argues against A, ask again rather than proceeding.

**Test 26f becomes an error in that same commit**, which entry 11 got right for the wrong reason: it works not because a sighter gap fixes four sheets, but because option A fixes the three the gap cannot reach.

**Section 4 and the parity test: your proposal, accepted.** Keep TARGET-SCHEMA.md section 4 as the as-printed `GL-CF25-LTR`, identifier `GL-YCSK-DZZ1-R0VJ-4T5Y` and gap 456, and re-point `Section4DocumentEncodesToTheReferenceSheetBytes` at `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, which is byte-identical to it. A worked example's job is to be a complete real document with a verifiable identifier, and the frozen file is permanent by construction where the live sheet is not; re-pointing it once is better than rewriting section 4 every time geometry moves.

**Add one sentence to section 4, which is prose and not schema.** A reader who meets `GL-YCSK-DZZ1-R0VJ-4T5Y` in section 4 and then finds a different identifier for `GL-CF25-LTR` in `TARGET-LIBRARY.md` will think one of them is wrong. Say that the example is the definition the Phase 0 sample set was printed from, that it is frozen at that path, which live sheet supersedes it, and that it is kept because an identifier hashes geometry, so a worked example that tracks the library would change with every geometry commit. That sentence is also the cleanest place in the whole specification to make the point that identifiers move when geometry does.

**Three corrections accepted, all three mine.**

1. `cells.sighterGap` is specified in TARGET-SCHEMA.md sections 3.6 and 7, not 3.10. Entry 11 is wrong.
2. `docs/TARGET-LIBRARY.md` carries no identifiers; the commit changes its marker counts. Entry 11 is wrong.
3. **`markerSize` is 8 modules, not 10**, per TARGET-SCHEMA.md section 3.7, the renderer and FIDUCIAL-DECISION.md's own 0.5 mm module in a 4.0 mm marker. `docs/PHASE1-BRIEF.md` section M0 and entry 12 both say ten, both counting the quiet zone the schema keeps separate, and both are wrong. The sweep sizes are 24, 32, 40, 48 and 64 dmm and your M0 table is the correct one. I will not edit the brief, because a brief is a record of what was asked; `PHASE1-RESULTS.md` M0 already carries the correction and the protocol will quote your numbers.

**The frozen fixtures landing while this was open was the right call**, and reproducing every measured value in `scans/phase0/measurements/` against them, with only detection times differing, is the check I would have asked for.

---

## 2026-09-14, entry 12: Phase 1 is briefed, and it is four milestones with no paper in any of them

**Status: open.**

`docs/PHASE1-BRIEF.md` is committed. Read it after finishing entry 11 and work the milestones in the order it gives. The short version, and the reasoning that is not in the brief:

**Alan cannot shoot until next weekend.** That is a week with no paper in it, and every one of the four things Phase 1 needs turns out to be unblocked without paper, so nothing is waiting on him. He was asked which of the four to build and chose all four.

**M0 first, and it is smaller than it looks.** The marker module sweep of `FIDUCIAL-DECISION.md` section 10 measurement 2 needed a renderer change in every previous plan, including the one I wrote yesterday. It does not. `fiducials.markerSize` is already a declared field with validation against `family`, and `tag36h11` is ten modules across, so the sweep is five definitions at `markerSize` 30, 40, 50, 60 and 80 dmm. An hour, not a day, and it clears the last thing standing between the paper protocol and being writable.

**M1 is first among the real milestones because its result has a deadline.** The paper protocol for next weekend has to say what photographs to take, and what to ask for depends on whether a developable fit works. If it works, the protocol asks for mounted frames that exercise it. If it fails, the protocol asks for something else, possibly including a second sheet in frame as a scale reference, which is a different thing to ask a person to do while they are at a range. So M1 reports as soon as it exists rather than at a convenient point.

**The focal-length trap in brief section 3.2 is the one I most expect to cost you a day**, so it is written out in full. A homography with a radial term never needed to know anything about the camera. A surface in three dimensions does. The EXIF has it, `photos.json` already carries it, and the estimate is about 2556 pixels for the main camera. If a frame has no focal-length tag it cannot use this path at all, and that limitation belongs in the report rather than being worked around.

**Brief section 3.3 is not advice.** Build the surface fit against synthetic bent sheets with known truth, sweep it until it breaks, and only then run the nine pinned frames, once. Your own decision-log line, "far-edge marker loss reported, not tuned against", is the same principle and it was the right call. Nine frames is a small enough set to fit a model to by accident, and the sweep is worth more than the pass or fail anyway, because it tells us how much bend the model survives rather than whether it survived these nine.

**M2 starts from something that already works, which is not how the last plan described it.** I told Alan yesterday that hole detection was the hardest unsolved problem in the project, and I was quoting `SCAN-MEASUREMENTS.md` section 8, the naive baselines, at 1 of 27 and 7 of 20. Section 3.1 of the same document already has a primitive that gets 25 of 27 with zero false positives. The correction matters because it changes what M2 is: not a search for a method, but a port of a known one, a baseline committed, and then the question of whether render-and-difference beats it. Render-and-difference cannot be tested on any existing file, because none of them is a GroupLab target, which is why brief section 4.3 builds synthetic ones from the survey's own measured hole statistics.

**M3 is the statistics engine, pulled forward from Phase 2**, because `STATISTICS.md` section 15 is a complete build specification and needs nothing from anybody. Two things in it were open and I have answered both in brief section 5 so they do not come back as questions: apply `c4` to the interval endpoints and match shotGroups, and make mean radius the headline with sigma beneath and extreme spread subordinate. The Monte Carlo tables want hours of compute; start them early and build something else while they run.

**M4 is the application shell**, and the part of it that is not negotiable is the manual assignment interface, `DESIGN.md` section 13. The survey found hand-drawn arrows on real targets recording the shooter's own cross-cell assignment, which no geometric rule recovers. Everything else in M4 is scope that can be cut.

**One thing to send back early rather than at the end.** The paper protocol has a print deadline. Anything you want from next weekend's session, extra sheets, a particular mounting, a photograph taken a particular way, goes into `docs/QUESTIONS-FOR-PLANNING.md` the moment you know you want it. A request that arrives after the protocol is printed costs a full week, because the next session after next weekend is the one after that.

---

## 2026-09-14, entry 11: Phase 0 closes, the geometry commit lands, and the printed definitions become fixtures

**Status: actioned 2026-09-14.** The frozen fixtures landed while question 6 was open, and the geometry commit landed as entry 13's option A, which records `GL-20J3-Y141-0BN3-EYME` as superseding `GL-YCSK-DZZ1-R0VJ-4T5Y` in `targets/frozen/phase0/README.md` and `docs/PHASE0-RESULTS.md` section 8.

**Phase 0 is closed on the verdict in `docs/PHASE0-RESULTS.md` section 8, as written.** I checked its arithmetic against `scans/phase0/measurements/photos.json` rather than taking it, the way entry 7 exists for. Every figure in section 3b reproduces from the raw rows: marker counts 34, 25 and 23; corners kept 136 of 136, 100 of 100 and 91 of 92; residual RMS 0.00270, 0.00315 and 0.00395 in over kept corners and 0.00424 in over all 92 on `main_flat3`; bull means 0.00187, 0.00270 and 0.00392 in; worst scoring bulls 0.00343 at 21, 0.00566 at 13 and 0.01183 at 1; scoring bulls over the gate 0, 2 and 6 of 25; worst sighters 0.00661, 0.01016 and 0.00496 in at S1. I also re-applied each frame's stored `mapping` to its own corner image points and recovered the stored per-corner error to better than 0.0002 dmm on all 328 corners, so the transforms in the file are the transforms the numbers came from and not a summary written beside them.

**I checked the claim that carries the verdict, and it holds.** Section 3b says every failure other than a sighter is a bull outside the markers that decoded, or within a thousandth of the gate. Taking the convex hull of the decoded corner page positions on each frame: on `main_flat1` the only failing bull, S1, is outside it. On `main_flat2` the missing markers take the decoded x extent from 2050 down to 1670 dmm, the far right side, and S1 and S2 are outside the hull, leaving 13 and 18 inside it and over the gate by 0.00066 and 0.00009 in. On `main_flat3` the decoded y extent starts at 709 instead of 329, the top rows, and bulls 1, 2, 4 and 5 are outside the hull, leaving 6 and 13 inside it and over by 0.00029 and 0.00026 in. Four bulls inside the lattice fail, by 0.0001 to 0.0007 in, against a gate that has 0.008 in of hole-centroid noise underneath it. That is not a registration failure. It is the gate resolving finer than the thing it will eventually measure, which is what a control is for.

**So the flat gate fails three of three and none of the three failures is unexplained.** One is the sighter geometry, already found, costed and fixed on paper in section 4.4. Two are the far-edge defocus of an f/1.7 lens on a foreshortened sheet, which is a detection task and belongs to Phase 1. The remainder is four bulls at the width of the noise floor. Section 8 says exactly this and I am not asking you to soften it. A gate that fails for reasons you can name is worth more than a gate that passes because it was tuned, and your decision not to tune detection against these three frames is the right one and is on the record in the decision log.

**The geometry commit lands now.** `GL-CF25-LTR` sighter gap 456 to 454, `GL-LR300-R24` and `GL-LR300-R36` 1219 to 1142, `GL-CF25-100M-A4` per the sweep, `cells.sighterGap` declared on each, test 26f promoted from warning to error, `docs/TARGET-LIBRARY.md` regenerated with the new identifiers. `cells.sighterGap` is already in `docs/TARGET-SCHEMA.md` at sections 3.10 and 7 and in the schema at test 23, so this commit changes definitions and one test severity and does not change the schema.

**It lands without a verification print, and that is deliberate.** Whether a sheet whose lattice brackets its sighters passes the flat gate is not measured and cannot be measured without paper. Printing one sheet now to answer that, and then printing again for Phase 1, is two paper sessions where one will do, and Alan has been explicit that manual work is batched. The verification print folds into the single Phase 1 paper session, alongside the shot targets, the mounted photograph set and the marker module sweep. Until then section 8's sentence stands: it is not measured.

**One thing the commit must carry that is not yet written down anywhere.** The geometry commit gives four sheets new identifiers, and the sample set in `scans/phase0/` was printed from the old ones. The moment it lands, `grouplab spike sheets` and every other spike command either fails to resolve the definition the paper was printed from, or silently measures the scans against geometry that moved by 1 dmm, and every table in `docs/PHASE0-RESULTS.md` stops reproducing. The document's own second line promises those commands reproduce every table. Fix it in the same commit:

1. Freeze the printed definitions as fixtures, canonical GLTD-J bodies under a path such as `targets/frozen/phase0/`, each named by its identifier as printed, with a short README saying these are the geometry the Phase 0 sample set was printed from, that they are inputs to a measurement and are never edited, and which live definition superseded each one.
2. Make the Phase 0 scan metadata and the spike commands resolve against the frozen fixtures rather than the live library, so every table in `PHASE0-RESULTS.md` still reproduces after the commit.
3. Add a line to `docs/PHASE0-RESULTS.md` section 8 saying the tables are measured against the frozen definitions, naming the path and the superseding identifiers.
4. Check the fixtures still load and validate against the current schema, and if a future schema change ever breaks one, that is a finding and not a fixture to edit.

Do the same for any definition a future sample set is printed from. A measurement against paper is only reproducible while the geometry it was printed from still exists in the tree, and a definition identifier changes whenever its geometry does, which is the point of the identifier.

**What happens after the geometry commit.** Report on it, then stop and wait. Phase 1 has a brief to write and a paper session to plan, and neither is yours to start. The four items in section 8's inheritance list are the right list and their order is right.

---

## 2026-09-14, entry 10: the flat control set, and the photograph gate now has two halves

**Status: actioned 2026-09-14.** The flat gate is run on `main_flat1-3` and fails three of three; the frame with every marker passes every scoring bull and fails only on S1. `docs/PHASE0-RESULTS.md` section 3b has the result, section 4.5 the developable-surface requirement with piecewise registration as fallback, and section 8 the Phase 0 verdict. DESIGN.md section 21 [r5] is committed with one figure corrected: the 0.006 to 0.011 in it quoted was a different sheet and lens, and is now the same sheet and lens lying flat, 0.0066 to 0.0118 in.

**Three flat frames are committed**, `scans/phase0/main_flat1.jpg` through `main_flat3.jpg`. Sheet 3, the clean control, lying flat, photographed with the main camera: f/1.7 at 6.25 mm, 23 mm equivalent, which matches `main1-3` and confirms the lens. All three are camera originals at 4000 by 3000 with intact EXIF and a fine quantisation table.

| Frame | Markers | Keystone |
|---|---|---|
| `main_flat1` | **34 of 34** | 0.997, square on |
| `main_flat2` | 29 of 34 | 0.926 |
| `main_flat3` | 23 of 34 | **0.802**, the most off-axis frame in the corpus |

No glass was available, so these are a sheet lying flat rather than mechanically restrained. They are the flattest photographs in the set and they are the control the gate needs. `main_flat3` losing eleven markers at a 0.802 keystone is a result in its own right and worth reporting rather than treating as a bad frame.

**Run the photograph gate on these three and close Phase 0 on the result.** No further photographs are being requested.

**The gate itself has changed, and the change is more than bookkeeping.** DESIGN.md section 21 now carries two photograph gates instead of one, marked `[r5]`:

- **Flat**, 0.005 in worst bull, on a sheet held flat. This is Phase 0's, and these three frames are what it is measured on.
- **Mounted**, 0.005 in worst bull, on a sheet mounted the way a shooter mounts it. This is **Phase 1's, and it is expected to fail until a surface model exists.**

The reasoning, which came from Alan and is right: a target stapled to a board is the application's actual input. It bows between its fixings, curls at a free edge and moves in wind. A sheet held flat is not a user scenario at all. So the mounted case is the product requirement and the flat case is a control that isolates the lens and the estimator, which is the reverse of how entry 8 framed it.

**The model that mounted case needs is already named in this project's own documents.** DESIGN.md section 6: **paper is a developable surface.** It bends without stretching, so distance along the sheet is preserved even where the projection is not planar. That is a far stronger constraint than a generic warp or a patchwork of local homographies, and it is what Phase 1 should build on rather than piecewise registration. Your own evidence supports it from the other direction: you showed that three radial coefficients with a free centre, and quadratic and cubic warps evaluated leave-one-marker-out, all fail to bring any pinned frame inside the gate. A model with more freedom is not what is missing. A model with the right constraint is.

Record that in `docs/PHASE0-RESULTS.md` where the Phase 1 requirement currently sits, and revise it from "local or piecewise registration from nearby markers" to a developable-surface fit, with piecewise registration noted as the fallback if the constrained fit proves impractical. The nine pinned frames are its benchmark and the figures to beat are in your section 3a.

---

## 2026-09-13, entry 9: the bracketing rule, accepted with one change to finding 3

**Status: actioned 2026-09-13.** Question 4 answered. The rule, its evidence and tests 23 and 26f are in TARGET-SCHEMA.md sections 7 and 10; the validator warns on the four sheets and exempts a bracketing shortening from test 23; the finding and the deferred commit are `docs/PHASE0-RESULTS.md` section 4.4.

This answers question 4. The measurement across the whole library is exactly what the rule needed before it landed, and it found two things entry 5 did not.

**Both of your decisions, as you recommended them.**

**Inclusive**, so a bull centre on the lattice boundary conforms. Both 300 yard tiles sit at a margin of zero on every side, and a strict rule would fail them with no fix short of a denser scheme, for a bull that is interpolated along the edge it sits on. Your wording already reads inclusive with "on or inside"; keep it.

**A warning first, an error once the three sheets are fixed.** A rule that cannot be satisfied by any sheet in the library on the day it lands is a rule nobody will believe. Warn now, name the three sheets in the warning text, and promote it to an error in the same change that fixes them. Add the promotion to the deferred geometry work so it does not become permanent: `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` join `GL-CF25-LTR` in one geometry commit after Phase 0 closes, and test 26f becomes an error in that commit.

**Finding 1 is worse than it looks and should be recorded as such.** On `GL-CF25-100M-A4` the outermost bull columns are 200 dmm outside the lattice and on the two rolls they are 508 dmm, against the 266 dmm that produced the sighter failure. These are not marginal cases riding on a technicality; they are the same defect at the same scale, on the horizontal axis, and the reason nobody saw them is that no photograph of those sheets exists. Say that in the results document, because it is the strongest argument for the rule: the defect was found once by accident and the sweep found three more.

**Finding 3 has a cheaper answer than any of your three.** Do not add `sighterGap` to the body, do not exempt decoded documents, and do not scope test 23 by provenance. **Change what test 23 warns about.** The sighter gap is already recoverable from the body: the sighter block carries its origin and the grid block carries the last scoring row, so any decoder can compute the gap. What it cannot recover is whether the departure was deliberate, and that is the only thing `cells.sighterGap` declares. But a departure that makes the lattice bracket is self-evidently deliberate, because that is the sole reason the solver shortens it.

So:

> 23. A sighter row whose gap differs from 1.2 times `pitchY` by more than 1 dmm, with no `cells.sighterGap` declared, is a warning **unless the shortened gap is what brings the sighter row inside the fiducial lattice, in which case it is not**.

No new byte, no new field, no provenance test, and the two rules stop contradicting each other. Propose the exact wording alongside 26f.

**Your proposed wording for section 7 is good and I would change one thing.** Drop "and the Phase 0 photographs measured the cost" down to a following sentence rather than embedding it in the rule, so the rule reads as a rule. The evidence belongs immediately after it and should now cite the sweep as well as the photographs.

---

## 2026-09-13, entry 8: the wall photographs are my fault, and A is right

**Status: actioned 2026-09-13.** Question 5 answered; the curved-sheet registration requirement, with its figures and its limit, is `docs/PHASE0-RESULTS.md` section 4.5, and the wall set is cited there and in section 3a as the Phase 1 baseline. The photograph gate is the spike's one open item, awaiting two frames of a flat sheet.

This answers question 5. You are right, the set is unusable for its purpose, and the reason is the instruction rather than the photography.

`docs/PHASE0-PRINT-PROTOCOL.md` section 7 says "Pin or tape it to a wall", and entry 6 repeated "taped flat to a wall" as though those were the same thing. They are not. A sheet hanging from one pin at the top centre curls away from the wall under its own weight, and your measurement shows it: the two lowest marker rows sit 0.026 to 0.138 in from the fit on every wall frame against 0.0025 to 0.0033 on the table frames, and the wall set is six to sixteen times worse by worst bull with the same lens. The protocol will be corrected.

**Option A, and it is the last photograph request.** Two frames, main camera, sheet 3, one square-on and one about twenty degrees off-axis, whole sheet in frame with a margin, no flash, camera originals.

**Flat means all four edges restrained.** Masking tape along each of the four edges onto a wall or a door, or the sheet laid on a table under a sheet of glass or clear acrylic, or under a pane from a picture frame. A clipboard or a single pin is not flat. If tape on all four edges is awkward, the glass-on-a-table version is easier and better, and an off-axis frame of a sheet lying on a table is just as valid a test of perspective as one on a wall.

**Do not wait for it to run the rest.** Everything else in the spike is done or unblocked, so close out what you can and treat the photograph gate as the one open item.

**Option C is a Phase 1 requirement, and record it now.** You are right that a pinned sheet is the normal case at a range, so local or piecewise registration from nearby markers is not a workaround for a badly mounted test, it is the real-world path. Your own numbers already show what it buys: nearest-marker registration took the worst scoring bull from 0.0053 to 0.0035 in and 0.0066 to 0.0029 on the table frames. Write it into `docs/PHASE0-RESULTS.md` as a Phase 1 requirement with those figures attached, and note the limit you found: it cannot help a bull that no nearby marker brackets, which is the sighters, which is entry 5 again from a third direction.

**One thing the wall set does establish, so do not discard it.** It is the first evidence in the project of how badly a curved sheet registers under a global homography, and it is a realistic curvature rather than a contrived one. Keep all nine committed and cite them as the baseline that piecewise registration has to beat at Phase 1.

---

## 2026-09-13, entry 7: commit the raw measurements, not just the summaries

**Status: actioned 2026-09-13**

A request, and the reasoning behind it, because it changes what you write rather than how much.

`docs/PHASE0-RESULTS.md` and your three questions are well written and I can act on them, which is the point. What I cannot do from them is check your arithmetic. When you reported in question 3 that the split is 0.0013 systematic and 0.0005 random, I accepted it, because your rebuttal of my estimator argument was clearly right and the direction was obvious. But I accepted it rather than verified it, and that is the wrong relationship for numbers this project will lean on for years.

**So commit the rows, not only the tables.** For every measurement that produces per-element data, write the raw values as a file under `scans/phase0/measurements/`, one file per measurement, JSON or CSV, whichever is natural. Per-bull error with its label and its declared coordinate. Per-marker residual with its id. Per-image, per-locator, per-detector. Include the fitted transform where a reader would need it to reproduce the numbers.

The summary tables stay exactly as they are. This is additional, and it should cost you almost nothing, because the arrays already exist inside the harness at the moment the table is printed.

**Why this and not a transcript.** The suggestion was raised that you log everything in the panel so the planning session can read it. That would be a great deal of text for very little signal: the conclusions are already written down, and what a transcript adds is mostly the path taken to reach them. Raw measurement rows are the opposite trade. They are small, they are exactly the thing a second party needs, and they turn every figure in the results document from something to be trusted into something to be checked. Your catch on my centroid is the argument for it: that happened because my method was written down in enough detail to be attacked. Give me the same target.

**One thing a transcript would genuinely add**, so capture it deliberately instead: a short decision log. Where you had a real choice and took one branch, one line saying what the alternative was and why you rejected it. You already do this in `docs/SPEC-ERRATA.md` for specification choices. Extend the habit to method choices, like picking the edge fit over the centroid, or the refinement window, or the shape gate ordering. A sentence each, in `docs/PHASE0-RESULTS.md`.

---

## 2026-09-13, entry 6: nine photographs across three lenses, and the lens identity is settled

**Status: actioned 2026-09-13**

Nine new photographs of sheet 3, the clean control, taped flat to a wall, are committed in `scans/phase0/`. Three lenses, three frames each, named by lens. This is more than option A asked for and it separates the lens question completely.

**The lenses are genuinely distinct**, and the naming is correct:

| Lens | f-number | Focal length | 35 mm equivalent |
|---|---|---|---|
| `ultrawide1-3` | f/2.2 | 2.20 mm | 13 |
| `main1-3` | **f/1.7** | **6.25 mm** | **23** |
| `telephoto1-3` | f/2.4 | 7.00 mm | 69 |

**The original four frames were the ultrawide, and your EXIF puzzle has an answer.** `20260913_130543.jpg` reports f/2.2 at 2.20 mm with a 35 mm equivalent of 23. The physical focal length and aperture match `ultrawide1-3` exactly; only the equivalent tag disagrees, and that tag is computed by the camera app rather than read from the hardware. **Identify the lens from `FocalLength` and `FNumber`, not from `FocalLengthIn35mmFilm`**, which this phone writes inconsistently. So your section 3 uncertainty in question 1 resolves to: it was the ultrawide, and the lens term you were fitting was real.

**Marker detection, OpenCV with subpixel refinement, out of 34:**

| Frame | Markers | Keystone, top-to-bottom width ratio |
|---|---|---|
| `main1` | **34** | 0.985 |
| `main2` | 27 | 0.908 |
| `main3` | 31 | 1.129 |
| `ultrawide1` | **34** | 0.996 |
| `ultrawide2` | 31 | 0.831 |
| `ultrawide3` | 24 | 1.225 |
| `telephoto1` | **6** | unusable |
| `telephoto2` | 32 | 1.064 |
| `telephoto3` | **3** | unusable |

**Telephoto 1 and 3 do not contain the whole sheet.** At a 69 mm equivalent from standing distance the sheet overflows the frame, so those two are not a detector failure and should be excluded rather than reported as one. `telephoto2` is usable. It is worth keeping all three committed, because "the user zoomed in and cut off two corner codes" is a real failure mode the application will meet, and it is now in the corpus.

**No measurement figures from me on these.** I ran my scratch pipeline over them and it produced bull errors six times worse than yours on the original four frames, which means my tool is wrong for perspective images rather than that these photographs are bad. It was written for flatbed scans, where the transform is nearly affine, and its distortion fit does not converge. Use your own pipeline and disregard anything my earlier photograph numbers might have implied.

**What this set is for.** Three questions your data could not previously separate:

1. **Lens.** Same sheet, same session, same flatness, three focal lengths spanning 13 to 69 mm equivalent. If the gate tracks focal length, the distortion model is the problem. If it does not, the lens was never the cause.
2. **Flatness.** These are taped flat to a wall; the original four were lying on a table. `ultrawide1-3` against `20260913_1305*` is a clean paired comparison, same lens, only the flatness differing. That is the direct test of your section 2 finding about local registration.
3. **Sighter geometry.** If the scoring bulls pass on a flat sheet with a well-behaved lens and only the three sighters fail, entry 5 is the whole answer and it is confirmed rather than inferred.

Report the photograph gate across all nine, grouped by lens, with the scoring bulls and the sighters called out separately.

---

## 2026-09-13, entry 5: the sighter row misses the marker lattice by 2 dmm, and that is the photograph finding

**Status: actioned 2026-09-13.** The geometry is unchanged. The finding, the sweep reproducing this entry's gaps, and the deferral are `docs/PHASE0-RESULTS.md` section 4.4; the section 7 wording, the conformance test and the three sheets a sighter gap cannot fix are `docs/QUESTIONS-FOR-PLANNING.md` question 4.

This answers question 1. Your diagnosis is right, your ranking of the causes is right, and the geometry cause is far cheaper to fix than you costed it, because it does not need a rule change.

**The measurement.** On `GL-CF25-LTR` the fiducial lattice would place a marker row at y 2705, below the sighters. Its box bottom lands at 2735 and `layout.py`'s edge test rejects anything past 2734, which is half the 120 dmm safe margin. **The row is dropped by one dmm.** That single lost row is why the three sighters are the only bulls on the sheet outside the lattice, and it is why they are the worst bull on three of your four photographs and on two of the three flat scans.

**It is not a defect in `grid-boundary-1`.** Sweeping the whole library, thirteen of sixteen sheets bracket their sighters already. Three do not:

| Sheet | Default gap | Gap that brackets | Move |
|---|---|---|---|
| GL-CF25-LTR | 456 | **454** | **2 dmm, 0.2 mm** |
| GL-LR300-R24 | 1219 | 1142 | 77 dmm |
| GL-LR300-R36 | 1219 | 1142 | 77 dmm |

So the reference sheet, the one everything is measured against, misses bracketing its own sighters by two tenths of a millimetre.

**The fix is option B, and it costs almost nothing.** Not a change to the placement rule, which stays immutable as `grid-boundary-1`, but a change to the sighter gap on three sheets, declared through `cells.sighterGap`, which section 3.6 already provides as the sanctioned override for a deliberate departure from the 1.2 convention. At a gap of 454 on `GL-CF25-LTR` the validator passes with zero errors, the marker count rises from 34 to 38, and the new row sits at y 2704, below the sighters.

**Add this as a rule, because it is the general statement of the fault.** The fiducial lattice must bracket every bull, sighters included. A bull outside the lattice is interpolated on a flat scan and extrapolated on anything that is not flat, and your photographs are the first thing that ever made the difference visible. Propose the wording for TARGET-SCHEMA.md section 7 and a conformance test alongside it, and put the general form in the solver: default to 1.2 times pitch, reduce minimally until the lattice brackets, declare `sighterGap` when it differs.

**What it costs.** The three sheets get new definition identifiers. On `GL-CF25-LTR` the scoring rows also shift by 1 dmm, from 539 to 540 and so on, because the row solver's bottom limit depends on the sighter position. That is 0.1 mm, which is a fifth of the printer's own measured placement error, so **the committed sample set stays valid evidence for everything except the sighters**. No reprint is needed now; it goes on the batched paper list.

**Do not apply the geometry change during this spike.** I had this the wrong way round when I first wrote the entry. The sample set on disk was printed from the current definitions, so changing `targets/GL-CF25-LTR.gltd.json` now would move every declared bull by 1 dmm while the scans still show the old print, and every number in `docs/PHASE0-RESULTS.md` would shift under you for a reason that has nothing to do with registration. Finish the spike against the geometry that was actually printed.

What to do now instead: record the finding in `docs/PHASE0-RESULTS.md` with the table above, propose the wording for the TARGET-SCHEMA.md section 7 rule and its conformance test, and say in the report that the fix is identified, costed and deferred. The change itself lands as its own commit after Phase 0 reports, and the next print run is the first to carry it. That also keeps the brief's "do not change the geometry" instruction intact rather than quietly overriding it mid-spike.

**Sequence.** This does not remove the need for option A. Your flatness evidence is independent and strong: local registration from the nearest six or eight markers improves the worst scoring bull on three photographs and the sheets were shot lying on a table rather than taped flat as the protocol asks. Do the geometry fix, and the two main-camera frames are being requested separately. Report the photograph gate against both, and if the scoring bulls pass while the sighters still fail, that is the finding and it stands on its own.

Do not adopt option C. Gating on scoring bulls only would have hidden this.

---

## 2026-09-13, entry 4: PHASE0-PRELIM was wrong about its own estimator

**Status: actioned 2026-09-13.** Dated amendments in `docs/PHASE0-PRELIM.md` sections 3, 5a and 6 and DESIGN.md section 21; the corrected split is `docs/PHASE0-RESULTS.md` measurement 6.

This answers question 3, and you are right. Take option A.

`docs/PHASE0-PRELIM.md` section 3 claimed "not the measurement method", and the argument given for it was stability across mask radii of 90, 100 and 110 dmm. Your rebuttal is correct and I should have seen it: all three masks hold the same ink, the inner ring and the dot, so the comparison could only ever show that the estimator was insensitive to how much paper surrounded it. It said nothing about the estimator class. The edge fit uses the outer ring as well and locates each edge against its own local ink and paper levels, which is a better instrument, and it was chosen on the synthetic raster before it saw paper exactly as the brief required.

**Amend both documents**, with a dated note citing the spike rather than rewriting the original text, so the record shows what the scratch measurement claimed and what the real one found:

- `docs/PHASE0-PRELIM.md` section 3, the "not the measurement method" bullet, and section 5a's split.
- `DESIGN.md` section 21, which repeats 0.0019 and 0.0010.

The corrected split is **0.0013 in systematic and 0.0005 in random**. Also record the third point, which is new information rather than a correction: a quadratic over the page leaves only 0.0004 mean and 0.0007 worst, so most of the systematic field is smooth, and a per-printer calibration would recover most of it rather than a fraction. That is worth a sentence in section 6 of `docs/PHASE0-PRELIM.md` where the prize is described.

Nothing about the gate changes. Five thousandths holds with more margin than before.

---

## 2026-09-13, entry 3: libapriltag corners, as requested

**Status: actioned 2026-09-13.** Re-run through the Phase 0 pipeline as `grouplab spike detectors`: `docs/PHASE0-RESULTS.md` measurement 4 and `docs/FIDUCIAL-DECISION.md` section 10, measurement 8, including the half-pixel convention finding.

This answers question 2. `scans/phase0/apriltag-corners.json` is committed, covering all five images from the same run as entry 2, all 34 markers detected in every one.

The file carries the convention note in its `_note` field: against a model ordered top-left, top-right, bottom-right, bottom-left, reverse the winding with no rotation, where OpenCV needs its list rotated by two.

Re-rank the detectors with your edge-fit locator and report whether the ranking holds. If it flips, that is a finding and it belongs in the report, because the ranking under the scratch centroid was consistent five images out of five and a reversal would say the estimator, not the detector, was driving it.

---

## 2026-09-13, entry 2: measurement 4 is done, do not install anything

**Status: actioned 2026-09-13.** Recorded in `docs/FIDUCIAL-DECISION.md` section 10, measurement 8, with the corner conventions; nothing was installed.

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
