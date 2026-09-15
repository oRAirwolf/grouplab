# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

**How entries arrive, from entry 44 on.**
- **Delivery:** the planning session delivers each new entry as its own file in `docs/notes/inbox/`, named `entry-NN.md`, and never writes this log or any other existing file.
- **The only writer:** the Claude Code session is the only writer of this log.
- **Actioning includes three steps:** folding the entry into the top of the log, setting its status, and deleting its inbox file.
- **Several files waiting:** fold them in ascending entry number, so the newest ends up first.
- **Why:** two writers rewriting one file with no locking overwrote this log once, and separate paths cannot collide.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## 2026-09-15, entry 54: the phone, which is not a small desktop

**Status: open, and deferred to Phase 6 as the entry says.** Section 11's first task needs no phone: whether frame-quality thresholds separate pass from fail on the Phase 0 frames. It is queued after entry 52 sections 3 and 4.

Entry 21's remaining scope, the other half, and the last thing owed from it. **Phase 6 work and explicitly not for this week.** It is written now because it has been owed since entry 21, and because one part of it, section 5, should change what the desktop does long before any phone exists.

This entry is more uncertain than entry 53. Section 11 says which parts I am confident about and which need a spike before anybody commits to them, and that division is the most useful thing in it.

### 1. The reframing, which is the whole entry

The obvious plan is GroupLab on a small screen: the same marking, the same figures, a phone-shaped layout. **That is the wrong product and the project's own measurements say so.**

Look at where accuracy is actually lost:

| | Result |
|---|---|
| Conformance test 43, synthetic | pass, worst 0.00026 in |
| Paper gate, scanned sheets | pass, ten of ten, worst 0.00325 in |
| Photograph gate, flat | **fail, three of three** |
| Photograph gate, mounted | **fail, zero of seven** |

**Every gate the project passes is on a scan. Every gate it fails is on a photograph.** A phone is a camera. Its contribution is not another screen for the analysis: it is the only device in the system that can act *before the photograph exists*.

So: **the phone is a capture instrument that tells you the frame is bad while you can still fix it.** Everything else it does is secondary.

Nothing else on the market can do this, and it is not cleverness on our part. It falls out of the sheet knowing what it is: GroupLab holds the marker lattice, the expected artwork and the registration residual, so it can judge a frame against the thing it is a photograph *of*. A generic camera app has nothing to compare against.

### 2. What the capture assistant checks, and where its thresholds come from

Live, on the preview, at whatever frame rate marker detection sustains:

- **Are the markers there?** How many of the sheet's markers are decoding, and which are missing. Missing corner markers are the ones that matter, because they are what extrapolates badly.
- **How oblique is it?** From the marker lattice, without any fit.
- **Is the sheet flat?** The lattice's departure from a plane is measurable from the markers alone, and it is the difference between a frame that will pass and the mounted case that fails zero of seven.
- **Is it sharp, and is it still?** Defocus at the far edge is already a named cause of Phase 0 failures.
- **Is the whole sheet in frame**, including all four edges.
- **Is there glare across the rings?**

**Do not invent the thresholds.** The project has 37 Phase 0 images with measured registration residuals and recorded verdicts, in `scans/phase0/measurements/photos.json`, plus whatever the range session adds. **Fit the thresholds to that set and state the false-accept and false-reject rates**, the same way every other number in this project has to earn its place. A capture assistant that says "good" on a frame that then fails the gate is worse than no assistant, because the person has packed up and gone home.

**Tell the user what to change, not that something is wrong.** "Move left" and "step back" and "the top right corner is cut off" are actions. "Registration residual high" is not.

### 3. The refusal, and the thing it must never do

**The assistant advises. It does not block the shutter.** Somebody standing in the rain at a range they drove two hours to reach must be able to take the picture anyway. A frame captured against advice is captured, marked as such, and analysed like any other.

**And when a frame is analysed later and fails, the record must say whether the assistant warned at the time.** That is how we find out whether the thresholds are any good, and it costs one field.

### 4. Several frames, which is the phone's real advantage

A person holding a phone can take five frames from slightly different positions in two seconds. A scanner cannot, and a person with a camera on a tripod will not.

Two levels, and the first is nearly free:

1. **Pick the best.** Capture a short burst, register each, keep the one with the lowest residual. This requires no new mathematics and would likely have turned some of Phase 0's flat failures into passes on its own.
2. **Use them together.** Several views of the same curved sheet constrain the surface far better than one, because a single view confounds the surface with the lens. **This is the most promising idea in this entry and the least proven**, and it goes directly at the mounted gate, which is the gate the project cannot currently pass at all.

**Level 2 is a spike, not a plan.** It needs proving on real frames before anybody builds a product around it. Level 1 should be done regardless.

### 5. Owning the camera removes a whole class of defect, and this one matters now

The corpus has repeatedly been damaged by not knowing what the camera did. Entry 16 found that a cropped ultrawide keeps its physical focal length and changes only the 35 mm equivalent, so grouping by the physical value alone mixed two pixel geometries and crashed a joint fit. Entry 27 found that `DigitalZoomRatio` has to join the lens grouping key, and that its absence means unknown rather than 1.0. Entry 37 found the first submission unusable because of what the phone had done to the frames.

**Every one of those is a consequence of receiving a photograph rather than taking one.** A capture app owns the camera, and can therefore:

- **refuse digital zoom outright**, which deletes entry 27's problem rather than working around it
- **record the true focal length, the physical camera and the sensor crop**, rather than inferring them
- **lock exposure and focus** across a burst, so frames in one burst are comparable
- **write a provenance block it authored**, rather than parsing one a phone wrote

**And here is the part that is not Phase 6.** A phone the app controls will, over many sessions, accumulate frames from the *same physical camera*. That is a per-device lens prior, and it makes every individual frame better conditioned than a one-frame fit can be. **The desktop can start collecting that now**, from the donated corpus and from Alan's own frames, keyed on the grouping key entries 16, 27 and 48 have between them established: make, model, lens model, physical focal length, 35 mm equivalent and digital zoom ratio. If a per-device prior measurably improves the fit on the frames we already have, that is a Phase 1 finding that arrives years before the phone does, and it is worth testing as soon as there is a spare afternoon.

### 6. What runs on the phone and what does not

**On the phone:** capture, the live assistant, registration, hole detection, and a verdict with its figures. The verdict has to be on the device, because a person at a range wants to know whether to shoot the sheet again before they take it down.

**Not on the phone:** marking by hand, assignment correction, load comparison, the analysis screen. Those need a pointer and a large display, and entry 43 is written for a desktop. **A phone that tries to be an editor will be a bad editor**, and `DESIGN.md` section 13 is explicit that a bad editor is worse than a mediocre detector.

**The handoff is the marking file, not the photograph.** It is small, it is already the project's interchange format, and it means the phone and the desktop disagree about nothing.

### 7. The imaging backend, and a licence constraint that is already decided

`IImagingBackend` exists and the OpenCV backend lives behind it, which was the right call and pays off here. **The mobile backend is a second implementation of that interface, not a port of the first**, because the OpenCvSharp runtime packages do not cover Android or iOS.

`DESIGN.md` section 20 already settled the licensing: the printed marker is AprilTag `tag36h11` specifically so the mobile detector can be **the AprilTag reference implementation under BSD-2-Clause**, through P/Invoke. Emgu.CV is plain GPL-3.0 with no app-store additional permission, and a downstream distributor cannot add one to somebody else's code, so shipping GroupLab plus Emgu.CV through the App Store would reproduce exactly the conflict the fiducial choice exists to avoid.

**That decision is made and this entry does not reopen it.** What it does add: whatever else the mobile backend needs, thresholding, contours, homography, has to be checked against the same constraint one library at a time, and the answer has to be recorded in `THIRD-PARTY-NOTICES.md` before the dependency is taken rather than after.

### 8. Privacy, where the default is the whole design

A phone application that photographs targets is a camera app that knows where you are. The project has already spent two days removing coordinates from its own history.

- **Nothing leaves the device by default.** No account, no upload, no telemetry, no cloud. Synchronisation is Phase 7 and is a separate, explicit choice.
- **The scrubber's rule from entry 48 applies at capture**: keep what describes the camera and the exposure, drop what describes where, when, who, or anything typed. On a device we control, the location is never written in the first place, which is better than removing it.
- **Donating a frame is a deliberate act with its own consent**, the same as the upload page, never a setting somebody leaves on.
- **The diagnostics rules of entry 41 apply unchanged**, and a phone makes them sharper: a crash report from a phone must carry no photograph, no location and no path.

### 9. Sequencing, and the one thing that is genuinely blocked

`DESIGN.md` section 21 puts Android at Phase 6 and iOS at Phase 8, built on GitHub Actions macOS runners, which are free for public repositories.

**Android first, and the reason is not technical.** iOS distribution needs the GPL section 7 additional permission, which is drafted, is not in force, and is with an attorney. **Until that comes back, iOS is blocked on a legal answer rather than on engineering**, and no amount of work moves it. Android has no equivalent barrier.

Nothing here should start before the desktop passes the photograph gates. **A capture assistant is worthless if the analysis behind it cannot measure a photograph correctly**, and today it cannot.

### 10. The gate

Two, and the second is the one that matters.

1. **The same 0.005 in worst bull**, on frames the phone captured, flat and mounted reported separately, as everywhere else.
2. **The fraction of frames a person who has not read anything captures successfully**, measured on people who are not Alan and not me. **That is the product, and it is the only number that says whether the capture assistant works.** A phone build that passes gate 1 and fails gate 2 has moved the problem rather than solved it.

### 11. What I am confident about, and what needs a spike first

Stated plainly, because the rest of this entry reads more certain than it is.

**Confident:**

- the reframing in section 1, which follows from measurements the project already has
- section 5's list of defects that owning the camera removes, every one of which is a defect the corpus actually suffered
- section 6's split, which follows from `DESIGN.md` section 13
- section 8, which is policy rather than engineering
- section 9's ordering, since the iOS blocker is a fact

**Needs proving before anybody plans around it:**

- **Avalonia's maturity on Android**, particularly camera preview and native interop. I have not checked, and this entry should not be read as saying it is fine.
- **AprilTag detection fast enough on a phone for a live preview.** Plausible, unmeasured.
- **Multi-frame surface fitting**, section 4 level 2. The most promising idea here and the least supported.
- **Whether the assistant's thresholds separate pass from fail at all.** Fit them to the 37 Phase 0 frames first. If they do not separate on data we already have, the feature does not work and that is cheap to find out.

**Do the last one first.** It needs no phone, no Avalonia and no camera: it is an afternoon with `photos.json` and it can kill or confirm the central idea of this entry before anybody writes a line of mobile code.

### 12. What I am not specifying

The interface itself, because it should be drawn after the assistant's thresholds exist and not before. Offline target printing from a phone. Chronograph pairing. Anything about synchronisation, which is Phase 7. And the iOS specifics, which wait on the attorney.

---

## 2026-09-15, entry 53: adjust to zero, which is the project's argument applied to the thing everybody actually does

**Status: open, and deferred to Phase 5 as the entry says.** Every closed-form figure in sections 3 and 4 checked against the formulas and reproduced: the multipliers on sigma, the median and 95 percent misses, every cell of the shots-needed table, and the 0.74 in three-shot miss. Section 9's coverage tests are written with the feature.

Entry 21's remaining scope, half of it. Not for this week: this is Phase 5 work and nothing about it is urgent. It is written now because it has been owed since entry 21 and because the numbers in section 3 are worth having in front of us before the range session rather than after.

Every figure below is closed form, and every one was checked against a 200,000 replication simulation before it was written down. The method is in section 9 so you can reproduce it rather than trust it.

### 1. Why this deserves more care than it looks like it needs

Zeroing is the single most common thing anybody does with a paper target. The universal ritual is three shots, measure the offset, turn the turrets, declare it zeroed. Every target program on the market will do that arithmetic. **None of them will tell you that on a half-MOA rifle a three-shot zero leaves you off by up to 0.74 inches at 100 yards, 95 percent of the time.**

That number is the whole reason GroupLab should have this feature. The arithmetic is trivial and everyone has it. The uncertainty is not, nobody has it, and it is the part that changes what a person should do.

**And it is the strongest argument the 25-bull sheet has.** Section 4 works it out: zeroing a half-MOA rifle to within a quarter MOA, at 95 percent confidence, takes 24 shots. A GroupLab sheet gives you 25 on one page, pooled into one centre. The target design pays for itself on the most ordinary task in shooting, and that case has never been made anywhere in the project's documents.

### 2. The arithmetic, which is the easy part

**Input:** the group centre relative to the point of aim, in linear units at the distance shot; the shot distance; the turret's click value; and the angular convention in force.

1. **Linear to angular**, using the actual shot distance. True MOA is 1.047 in per 100 yd; IPHY is 1 in per 100 yd; a mil is 3.6 in per 100 yd. `DESIGN.md` section 20 makes true MOA the default with IPHY available, and both must be labelled, never silently swapped.
2. **Angular to clicks**, by the stated click value: quarter MOA, eighth MOA, tenth mil, twentieth mil.
3. **Express it in the turret's own words**, not signed numbers. A group low and left gives **"UP 3, RIGHT 2"**. Nobody has ever stood at a bench and thought in negative y.
4. **Round to whole clicks and state the residual**, because a turret has no half positions. "UP 3, RIGHT 2, leaving 0.04 MOA low" is honest; silently rounding is not.

**Two things this is not, and the panel should say so.**

- **It is not a ballistic correction.** It puts the point of impact on the point of aim at the distance you shot. If you shot at 200 and want a 100 yard zero, that is the solver's job, and the solver is Phase 5. Until it exists the panel says which distance the correction applies to and stops.
- **It is not a click-value check.** See section 6.

### 3. The honest part: when is a correction worth making at all

The measured centre is an estimate from `n` shots. Its standard error on each axis is `sigma / sqrt(n)`, where `sigma` is the per-axis standard deviation, which is the same Rayleigh sigma the panel already leads with.

**The rule: adjust an axis only when its confidence interval excludes zero.** If the interval spans zero, the honest statement is that the rifle is not measurably off centre on that axis, and turning the turret is turning noise into the rifle.

**Degrees of freedom, and this connects to machinery that already exists.** For a circular group, sigma is estimated from all `2n` coordinates, so `df = 2n - 2`. GroupLab already runs a Pitman-Morgan test for circularity, so use it: **if circularity is not rejected, pool and use `df = 2n - 2`. If it is rejected, estimate each axis separately with `df = n - 1`** and accept the wider interval, because a group that strings vertically genuinely knows less about its vertical centre.

**The smallest offset distinguishable from zero at 95 percent, in units of sigma:**

| Shots | df | multiplier on sigma |
|---|---|---|
| 3 | 4 | **1.603** |
| 5 | 8 | 1.031 |
| 10 | 18 | 0.664 |
| 20 | 38 | 0.453 |
| 25 | 48 | **0.402** |
| 50 | 98 | 0.281 |

Worked for a half-MOA rifle at 100 yards, which is a good rifle:

| Shots | detectable offset | at 100 yd | in quarter-MOA clicks |
|---|---|---|---|
| 3 | 0.80 MOA | 0.84 in | **3.2** |
| 5 | 0.52 MOA | 0.54 in | 2.1 |
| 10 | 0.33 MOA | 0.35 in | 1.3 |
| 25 | 0.20 MOA | 0.21 in | 0.8 |

**Read the first row again.** Off three shots, on a rifle that shoots half MOA, an offset smaller than about three clicks cannot be told from zero. The correction people most often make after a three-shot group is smaller than that.

### 4. Where the zero actually lands afterwards, which is what a person wants to know

Adjust by the measured offset and the remaining error is the sampling error of the centre. Radially that is Rayleigh with parameter `sigma / sqrt(n)`, so the 95th percentile is `2.448 * sigma / sqrt(n)`.

| Shots | median miss | 95% miss |
|---|---|---|
| 3 | 0.680 sigma | **1.413 sigma** |
| 5 | 0.527 | 1.095 |
| 10 | 0.372 | 0.774 |
| 25 | 0.235 | **0.490** |
| 50 | 0.167 | 0.346 |

**And the table that should be on the screen**, shots needed to put the zero within a target radius 95 percent of the time, `n >= (2.448 * sigma / target)^2`:

| Rifle sigma | within 0.5 MOA | within 0.25 MOA | within 0.1 MOA |
|---|---|---|---|
| 0.25 MOA | 2 | 6 | 38 |
| **0.50 MOA** | 6 | **24** | 150 |
| 0.75 MOA | 14 | 54 | 338 |
| 1.00 MOA | 24 | 96 | 600 |

This is the same shape as the compare-loads table in entry 43 section 6, and it should be built from the same code. **The project's one repeated argument is that the number of shots decides what you are allowed to conclude, and this is that argument applied to zeroing.**

Add the click quantisation to the residual: rounding to whole clicks leaves a per-axis error uniform on plus or minus half a click, variance `c^2 / 12`. On a quarter-MOA turret that is small beside the sampling error at any realistic `n`, and on a coarse turret at high `n` it starts to dominate. Say which is dominating, because the answer tells the shooter whether more shots or a finer turret is the thing that would help.

### 5. What the panel says

- **When the interval excludes zero on an axis:** the correction in the turret's words, with its own interval. "UP 3 clicks, somewhere between 2 and 5."
- **When it does not:** "Windage is not measurably off centre on 5 shots. Leave it alone." Not a correction of zero, which reads like a measurement. A sentence.
- **Always:** where the zero will be afterwards, and the shots-needed row for this rifle.
- **Never** a correction without an interval. This is entry 24, entry 39 section 1 and entry 40 for the fifth time: no bare number on a screen.

**Refuse, rather than compute, when:**

- there is no point of aim, or shots are unassigned on a multi-bull sheet, per entry 39 section 1
- the shot distance is not set, because the angular conversion needs it
- the click value is not set
- fewer than 3 shots, where sigma has a single degree of freedom and the interval is meaningless. Say what is missing, in the place the figure would have been.

### 6. Turret tracking, which is nearly free and nobody offers it

Once somebody adjusts and shoots again, GroupLab has both centres and knows how many clicks were dialled. **Measured movement per click falls straight out**, with an interval, from data the user produced anyway.

A turret that moves 0.22 MOA per advertised quarter MOA click is 12 percent slow, which at distance is a miss nobody can explain. It is the sort of defect that gets blamed on the ammunition for years.

Two cautions, both mandatory. **It needs enough shots in both groups to say anything**, and the same interval logic applies to the difference of two centres, so the standard error is `sigma * sqrt(1/n1 + 1/n2)`. And **it must never be offered from two three-shot groups**, where the interval on the ratio will span most of the plausible range and the answer will be noise wearing a decimal point.

### 7. One thing this specification cannot promise

Every interval above assumes the shot coordinates are exact. Entry 52 section 4 is the reason to say so here rather than quietly: the pipeline has at least two places where a continuous input change produces a discontinuous output change, and until those are measured the coordinates have an uncertainty that none of these intervals include.

**It does not invalidate any of this**, because measurement instability is far smaller than shot dispersion on any group worth zeroing from. But the panel should not claim more than it has, and when entry 52's experiments produce a number, this feature is one of the places it belongs.

### 8. What I am not specifying

Holdover and dial-to-distance tables, which need the solver. Multiple zeros, or a zero that is deliberately offset. Canted-reticle correction. First and second focal plane differences, which do not affect the arithmetic but do affect what a reticle measurement means, and reticle measurement is not an input here.

### 9. How to check my numbers, because they are mine and I have been wrong this week

Every figure was verified by simulation before it was written, at 200,000 replications with the group drawn from a circular bivariate normal:

- **The interval is honest.** With the true offset at zero, the rule says "adjust" 4.91, 5.00, 5.10, 4.93 and 5.00 percent of the time at n = 3, 5, 10, 20 and 25. It should be 5.
- **The critical values are right.** The empirical 95th percentile of `|xbar| / (sigma_hat / sqrt(n))` came out 2.767, 2.307, 2.099 and 2.001 at n = 3, 5, 10 and 25, against `t(2n-2, 0.975)` of 2.776, 2.306, 2.101 and 2.011.
- **The residual table is right.** Simulated 95th percentile radial miss: 1.410, 1.097, 0.776, 0.548, 0.489 and 0.345 sigma, against the closed form's 1.413, 1.095, 0.774, 0.547, 0.490 and 0.346.

**Reproduce these as tests rather than taking them from this entry.** A coverage test at n = 5 and n = 25 that fails if the false-adjust rate leaves 4.5 to 5.5 percent is worth more than any assertion in this document, and it is the same pattern as the interval coverage tests the statistics engine already has.

### 10. One line for the README, when this ships

The 24-shots figure in section 4 is the clearest case the target design has ever had, and it currently appears nowhere. When this feature exists, the README's problem statement should carry it: **zeroing a half-MOA rifle to within a quarter MOA takes about 24 shots, and a GroupLab sheet gives you 25 of them on one page.**

---

## 2026-09-15, entry 52: question 15 answered, and the thing underneath it

**Status: actioned 2026-09-15.** The macOS rerun from Windows' corners is next, as section 5 orders.
- **Sections 1 and 2:** the sort is committed with every record regenerated, and no verdict changed. `docs/PHASE1-RESULTS.md` "Entry 52" has the before-and-after tables, headed by your sentence.
- **Two further instabilities show in them,** beyond RANSAC's consensus. `ultrawide1`'s surface fit converges to a focal length of 2053 px instead of 1940 from a slightly different start. Two correlation readings change, `ultrawide1` from structured to not distinguishable from random and `main_flat2` the other way.
- **Records already stale:** regenerating under unchanged code first found five, reported separately there so they are not read as the sort's effect.
- **Section 3, the hypothesis holds.** The same correspondences handed to the homography fit in 200 orders gave one result on `main_flat1` and `main_flat2`, and two on `main_flat3`. The mounted frames gave 15 to 70, with the worst scoring bull spanning up to threefold. The instability is the planar model's mismatch with a curved sheet, not the sampler.
- **Section 3's consequence, as you stated it beforehand.** The mounted gate's 0 of 7 never had an error bar. It is robust: no ordering lets a mounted frame pass, and the lowest worst scoring bull across all 1,400 mounted registrations is 0.013 in. Its corner counts, bulls over the gate and worst bulls are not: they vary by more than several of the M1 comparisons were drawn from.
- **The edge fit, entry 49 section 5.** Leaving one point out moves a bull by at most 0.0002 in on bulls of 106 to 900 points. `telephoto3`'s bull 24 has 29 points and did not converge, and one point moves it by up to 0.53 dmm, 0.0021 in, more than the 0.30 dmm move seen on the macOS runner. No point was rejected at convergence on any bull, so the step is in whether a ray yields an edge point at all. The stage record names the failure to converge, and nothing says a bull rests on few points.
- **Section 4:** `docs/STATISTICS.md` section 2 records the measurement's own instability as a known source of uncertainty the intervals do not include, with these numbers, and `DESIGN.md` section 14 points at it.

Reported in `docs/PHASE1-RESULTS.md` "Entry 52" and "Entry 52 sections 3 and 4".

Section 1 is the decision you asked for. Section 2 is how to land it without wrecking the benchmark. Section 3 answers your deeper question, which is the better question. Section 4 is what sections 1 to 3 add up to, and it is the most important paragraph I have written in this log.

Stopping on this rather than committing it was right. It is exactly the case the blocking-question rule exists for.

### 1. Decision: commit the sort. For determinism, and not for the numbers

**Yes, commit it.** The reason is that a measuring instrument must give the same answer for the same input, and today it does not. Marker order carries no information: the same sheet, the same markers, the same corners, presented in a different sequence, is the same measurement. An output that moves with it is wrong in a way that has nothing to do with which output is better.

**Now the part that matters more than the decision.** `ultrawide3.jpg` goes from 42 kept corners to 51, and from 21 scoring bulls over the gate to 18. **That looks like an improvement and it is not one.** It is the same algorithm on the same data with the inputs in a different order. If those numbers land in `PHASE1-RESULTS.md` without a flag on them, then in a month somebody reads 18 against the old 21 and cites it as the surface work improving, and that will be the project citing noise as progress in its own results document.

So the instruction is not "commit the sort". It is: **commit the sort, and make it impossible to mistake the movement for an improvement.**

I want to be explicit about my own reasoning here, because the trap is close. I am not choosing the sort because it improved `ultrawide3`. If sorting had made every mounted frame worse I would still be asking for it, for the same reason. **The moment the justification becomes "it gives better numbers", it is an algorithm chosen after seeing results, which entry 17 section 2 rules out.**

### 2. How to regenerate without destroying the benchmark

The mounted figures are what the surface work is measured against. Changing a benchmark is sometimes necessary and always dangerous, because afterwards nobody can compare across the change unless the change is recorded.

1. **Regenerate every record and every quoted figure**, as you proposed. Half-regenerated is worse than either state.
2. **Put a before-and-after table in `PHASE1-RESULTS.md`** for every frame whose figures moved: kept corners before and after, bulls over gate before and after, worst bull before and after.
3. **Head that table with one sentence in plain words**, something close to: *"These figures moved because the markers are now sorted before use. The sheet, the markers and the corners are identical; only their order changed. The movement measures how unstable the registration is on these frames, not an improvement in it."*
4. **Say it in the commit message too**, not only in the document, because the commit is what somebody bisecting will read.
5. **No gate verdict changes**, which you have already established. Say that explicitly as well, because it is the reassuring half and it is true.

### 3. Your deeper question: yes, and it is more urgent than the sort

You asked whether registration should stop relying on RANSAC's random consensus, which on a curved sheet shifts with an input that should not matter. **Yes, that needs answering, and the sort does not answer it.**

Sorting removes one source of variation. It does not remove the sensitivity. The same fragility is still there and will move the answer for any other reason the input order or the random draw changes: one more marker found, one corner refined a fraction differently, a different platform, a different seed.

**Here is what I think is happening, offered as a hypothesis for you to test rather than a conclusion.** RANSAC finds consensus for a model. On a flat sheet a homography is very nearly the right model, the inliers are unambiguous, and the consensus is stable. **On a mounted sheet the sheet is curved, so a planar model cannot fit it, there is no clean inlier set, and RANSAC returns whichever subset happened to look best on the draws it tried.** That would explain why the mounted photographs are exactly the frames that swing, and it would mean the instability is a symptom of model mismatch rather than a RANSAC tuning problem.

**The experiment that settles it, and it is cheap.** Run registration on the mounted frames K times, say 200, with the marker order shuffled differently each time, and report the distribution rather than a value:

- kept corners: minimum, median, maximum
- scoring bulls over gate: minimum, median, maximum
- worst bull: minimum, median, maximum

Do the same for three flat frames as a control. **If the flat frames give a single value and the mounted ones give a spread, the hypothesis holds and the fix is about the model, not the sampler.**

**And the result has a consequence that I want stated before you run it, so it cannot be read as convenient afterwards.** If `ultrawide3`'s kept-corner count ranges over anything like 42 to 51 across orderings, then **the mounted gate's 0 of 7 has never had an error bar, and some part of it is sampling noise rather than measurement.** That does not make the mounted gate pass. It means the number the surface work has been measured against for a week has an uncertainty nobody has quantified, and any conclusion drawn from a change smaller than that spread was not supported. I would rather know that than not.

**Do the same experiment for the edge fit of entry 49 section 5 at the same time.** One point of thirty moving a bull 0.30 dmm is the same shape of problem: leave each point out in turn, report the spread. Two experiments, one method, and they share the tooling.

### 4. What these add up to, which is bigger than either

**The pipeline has at least two places where a continuous change in the input produces a discontinuous change in the output.** RANSAC's consensus set, found by reordering markers. The edge fit's kept-point count, found on `telephoto3` with identical corners. Both were found by accident, while chasing something else. **Neither was found by looking for them, which is the reason to assume there are more.**

Now the part that matters, and it goes to what this project is for.

GroupLab's entire argument is that a measurement should come with an honest statement of its uncertainty, and that software which prints a confident number it cannot support is doing harm. Every interval the application prints today describes **shot-to-shot dispersion**: how much the rifle and the shooter scatter. **None of them describes measurement instability: how much the answer would move if the measurement were repeated.**

If a bull's position can jump 0.30 dmm because one edge point of thirty fell the other side of a threshold, then the measurement has an uncertainty that no interval on the screen accounts for. **A 94.7 percent interval computed from perfectly measured coordinates is not a 94.7 percent interval when the coordinates themselves can move.** The project would be doing, in a smaller way, the thing it exists to criticise.

**I am not asking for that to be solved now, and it must not turn into a rewrite before the range weekend.** I am asking for it to be written down, in `DESIGN.md` section 14 or `STATISTICS.md`, as a known and unquantified source of uncertainty that the intervals do not currently include. Naming it costs a paragraph. Leaving it unnamed is how a project ends up believing its own error bars.

The two experiments in section 3 are the first measurement of it.

### 5. Order

1. **The sort, with section 2's regeneration and its before-and-after table.** It unblocks the macOS rerun, which has been waiting on it.
2. **The two spread experiments**, section 3. Edge fit and marker ordering, one method.
3. **The paragraph in section 4**, written once the experiments give it a number to cite, or written without one if they take longer than expected.
4. Then the macOS rerun from Windows' corners, and the rest of the gate record.

**Entry 43 stays where entry 50 put it.** Nothing here changes that, and section 4 is a reason to be glad the interface is not the thing being worked on this week.

---

## 2026-09-15, entry 51: the top to bottom check, and what it found

**Status: actioned 2026-09-15.** Section 6, all five:
1. `DESIGN.md` section 20 says .NET 10.
2. `docs/PHASE0-SPIKE-BRIEF.md` cites section 19.
3. Entry 36's status says entry 38 closed `DFdistr`.
4. Entry 31's status says section 3 was done under entry 33.
5. The paths in entries 41, 42 and 43 carry `src/`.

Sections 1 to 5 and 7 are noted. This is a report rather than a set of instructions, except section 6, which is the short list of things to fix. Alan asked for the project checked end to end after nine days and fifty entries. This is that check, and what it did not cover.

### 1. The result, in one line

**The project is in better shape than I predicted, and the one real defect was on the public front page rather than in the code.**

I wrote down in advance what I expected to find, so that finding nothing would be informative rather than reassuring: at least one status that overstated what was done, documentation drift in more places than I had listed, an uncomfortable gates table, and no privacy failure. **Three of those four were wrong.** There is no status that overstates. The gates table is honest and already says so in the documents. The privacy chain is clean and in two places stricter than I specified. Only the documentation drift was as bad as expected, and worse in one place.

### 2. What was checked, and what it found

| | Checked | Result |
|---|---|---|
| **A0** entries 39 and 40 restored from memory | diffed against my originals | **identical**, line for line |
| **A1** paths referenced by entries 22 to 51 | 81 references against the pushed tree | **no dangling reference.** 5 cosmetic, listed in section 6 |
| **A1** status lines claiming a report | 16 claims against `PHASE1-RESULTS.md` headings | **all 16 exist** |
| **A2** the named entry claims | 8 entries verified individually | **all pass**, two exceed what the entry asked |
| **B1, B2** GPS in published history | unauthenticated download, EXIF parsed properly | **zero GPS tags**, both repositories, both phase-0 branches |
| **B3, B4** opt-out by both signals and by hash | `Intake.cs` and its tests | **passes, and stricter than asked** |
| **B5** the two permanently held files | published tree | **absent** |
| **B6** diagnostics privacy | `DiagnosticsTests`, `ReportPackageTests` | **passes emphatically** |
| **B7** dangerous copies off-repository | `C:\Dev` listing, Alan checked the rest | **clean** |
| **B, unplanned** does scrubbing move a pixel | decoded both copies and compared arrays | **identical, max difference 0** |
| **C** the gates | both results documents | **honest**, see section 4 |
| **D** documentation drift | nine checks | **three findings**, section 6 |
| **E** loose ends on disk | published tree, branches, ignored paths | **clean** |

### 3. The three things worth remembering from it

**The scrub moves no pixel, and that is now measured.** A published owner photograph is 3.9 MB against an 8.1 MB original, which does not look like metadata. It is: the original carries 72 EXIF tags including 11 GPS tags and a Motion Photo video embedded at offset 4,179,647, and the published copy carries 12 tags, no GPS and no video. Same dimensions, maximum pixel difference zero, identical pixel-array hash. **That is the project's most load-bearing claim and it had never had a number behind it.**

**The consent rules are working, and the consequence is that the public corpus has no donated photographs in it.** `donated/` holds one submission and a single provenance record. The first submission was unusable, one of the three later ones opted out, and the other two each had their only usable frame withheld by the cross-submission hash rule. **The entire donated corpus is gated on one question to one contributor**, which is worth knowing because it is one message rather than a pipeline problem.

**Both wrong status lines understate.** Entry 36 still carves out `DFdistr`, which entry 38 fixed and I measured as correct. Entry 31 still carves out section 3, which is `ReadmeTests`, present with four guards and green on three platforms. I expected the opposite error and did not find it.

### 4. The gates, which are honest and mostly not passed

| Gate | Result |
|---|---|
| Conformance test 43 | pass, worst 0.00026 in against 0.001 |
| Paper, scanned sheets | pass, ten of ten, worst 0.00325 in |
| Print-scale detection | pass |
| End-to-end synthetic truth | pass |
| Photograph, flat | **fail**, three of three |
| Photograph, mounted | **fail**, zero of seven |
| Phase 0 record off Windows | **first measured today**, see entries 48 and 49 |

**The documents already said all of this before I looked**, in those words, including that reproducing off Windows was "not yet claimed" and that passing the suite on three platforms is "agreement within tolerance, not the byte-identical comparison". I went looking for a gate recorded as passing that had never been measured and there is not one.

**What the table hides, and it is the important thing: no gate has ever seen paper with holes in it.** The paper gate passes on scanned, unshot sheets. Everything above is the project checking its own arithmetic. The range session is the first time it finds out whether it works.

### 5. Where my own method failed, because that is the useful part

Four times in this check my measurement was the thing that was wrong.

- **A significant-digit counter returned zero** on values that plainly had seventeen, from a bad substitution.
- **A numeric-string detector flagged `gate.section = "15.3"`** as a number stored as text.
- **A GPS byte-scan reported a GPS pointer in all eight photographs.** It was counting the byte pair `88 25` anywhere in a 1.7 MB JPEG, where it occurs five to twenty times by chance. Parsing the actual EXIF segment gave zero. **That one would have been an expensive false alarm.**
- **A citation sweep reported 59 references and missed 26**, because the pattern did not allow for the backtick in `` `DESIGN.md` section 19``. The real count was 85, and re-running it is what found the one genuine error.

Every one was caught by reading the output instead of the verdict. **A check that quietly covers two thirds of its corpus is worse than no check**, because it produces a clean result you believe. That is the habit worth keeping from this exercise, more than any individual finding.

### 6. The fixes, which are small

Everything here is documentation. Nothing in the code needs changing as a result of this check.

1. **`DESIGN.md` section 20 says ".NET 9 with Avalonia".** `Directory.Build.props` says `net10.0`. One line. The README's version is guarded by `ReadmeTests` and stayed right; `DESIGN.md` has no guard and drifted.
2. **`docs/PHASE0-SPIKE-BRIEF.md` cites `DESIGN.md` section 18** for "wants every stage to emit a structured record". That is section 19, the `[r3]` paragraph. Section 18 is Storage and synchronisation. This is the second wrong citation of section 18 in the corpus; entry 39's was the first and entry 41 corrected it.
3. **Entry 36's status line still says "except `DFdistr`".** Entry 38 closed it. Amend the status to say so rather than leaving a closed exception standing.
4. **Entry 31's status line still says "except section 3, which waits for the fresh repository".** `ReadmeTests` exists and passes. Amend it.
5. **Five path references in entries 41, 42 and 43 omit the `src/` prefix**: `GroupLab.App/Theme/Tokens.cs`, `AppStyles.cs`, `Marks.cs`, `GroupLab.Core/Trace/StageRecord.cs` and `GroupLab.Core/Analysis/SheetAnalysis.cs`. Mine, cosmetic, and the same class of error as the section number above: a pointer that does not resolve. Fix them where you are already editing those entries' statuses.

The README fixes are entry 49 section 4 and are not repeated here.

### 7. What this check did not cover

Stated so that nobody mistakes it for complete.

- **Entries 1 to 21 were checked lightly**, by confirming their artefacts exist rather than re-deriving them. They were gated by Phase 0 results measured at the time.
- **Nothing was built or run by me.** Every claim needing a compiler or a test run was taken from your reports and from CI, not independently reproduced.
- **The statistics suite passing from the fixtures alone** is the one gate row I could not fill. The fixtures themselves I did verify: 600 keys, 580 numerics, zero disagreement between CSV and JSON, 441 values at full 17 digits.
- **Anything after `dfe9f40`** is outside this check. You were mid-run when it was written.
- **Whether the marks are legible** is a human question and was answered by eye on one photograph, not measured.

---

## 2026-09-15, entry 50: the README is part of done, and when the analysis screen starts

**Status: actioned 2026-09-15.**
- **Section 1:** the rule is in `CONTRIBUTING.md` under Conventions, with the checklist of claim-bearing sections, and the platform claim is guarded.
- **Section 2:** entry 43 starts when the gate record workflow is green on all three platforms, entry 49 section 5 is measured and reported, and the range session has been through the pipeline. Recorded in `docs/PHASE1-RESULTS.md`.
- **Section 3:** noted.

Reported in `docs/PHASE1-RESULTS.md` "Entries 49 and 50". Short. Two standing rules from Alan, written down so they outlive whoever remembers them.

### 1. The README is updated in the same commit as the change that makes it stale

Alan has asked that the README be kept current with each change as it happens, rather than repaired in batches when somebody notices. Entry 49 section 4 is why: a sentence saying the project builds only on Windows sat on the public front page for days after it stopped being true, and it took a person reading the file to find it.

**The rule: a commit that makes a statement in `README.md` false updates that statement in the same commit.** Not the next one, not a documentation pass later. The same commit, so that the repository is never in a state where its front page contradicts it.

**What this is not.** It is not a requirement to touch the README on every commit, and it is not licence to generate it. Entry 31 section 3 still holds: most of that file is argument and judgement, and generated prose reads like it. Most commits change nothing the README claims, and those commits leave it alone.

**The claim-bearing parts, so this is a checklist rather than a judgement call.** These are the places a change is likely to falsify something:

- **Building**, which platforms build and which are offered
- **Status**, what exists and what does not
- **Planned**, the phase table
- **Concept screens**, how far the application is from them
- **Built with**, versions and dependencies
- **Test data**, the `grouplab-testdata` commit pin
- **Repository layout**, when a directory is added or moved
- **Licence**, which stays as it is until the attorney answers

**Put the rule in `CONTRIBUTING.md`** beside the other conventions, as a line in whatever the definition of done is there. A rule that lives only in this log is a rule that lasts as long as this log is being read.

**And guard what can be guarded**, per entry 49 section 4: the platform claim goes between markers and `ReadmeTests` compares it against the matrix in `.github/workflows/ci.yml`. Every claim that can be put between markers and checked against something the repository already knows should be. The rest is judgement, and judgement is what the rule above is for.

### 2. The analysis screen waits for the measurement work, and here is what that means

Alan has decided the interface work goes in after the measurement work is complete and working. **That is the order I recommended and it is now his decision rather than my preference, so entry 43 does not start early for any reason.**

"Complete and working" needs to be checkable rather than a feeling, so:

**Entry 43 starts when all three of these are true.**

1. **The `phase 0 gate record` workflow is green on Windows, Linux and macOS**, under the rule in entry 49 section 1: every verdict identical, every printed table identical, raw differences reported.
2. **The edge fit sensitivity of entry 49 section 5 is understood and reported.** One point of thirty moving a bull 0.30 dmm is a robustness problem that has nothing to do with platforms, and it is the one most likely to bite on real shot paper. Understood means measured and written down; it does not necessarily mean changed, because the right response might be a warning rather than a different fit.
3. **Alan's range session has been through the pipeline**, because that is the first real paper any gate has ever seen, and it is the only thing that can tell us whether the detector works on holes rather than on renderings.

Item 3 is the one that takes a weekend rather than an afternoon, and it is the one that matters. **Everything before it is the project checking its own arithmetic. Item 3 is the project finding out whether it works.**

**In the meantime, entry 43 is not idle.** When the weekend's frames come back they will change what the analysis screen has to show: how a marginal fit is flagged, what a partly-identified sheet looks like, what happens when a bull has two holes in it. A specification written before that data would have to be rewritten after it. **Waiting is not a delay here; it is the cheaper order.**

### 3. What is not waiting

The styling work of entry 42 is done and does not pause. If something on the existing screens is wrong, ugly or unreadable, fix it as it comes up rather than parking it behind entry 43. The rule is about not starting a new screen, not about tolerating a bad one.

---

## 2026-09-15, entry 49: three decisions, the README is telling visitors something false, and a fragility worth more than the gate that found it

**Status: actioned 2026-09-15 for sections 1, 3 and 4. Section 2 is implemented and measured, and blocked on question 15: sorting the markers changes the Phase 0 record on Windows, no verdict with it. Section 5 is not started, and is the next measurement.**
- **Section 1, measured at `e03415c`:** the gate record workflow passes on Windows and Linux and fails on macOS, as the rule predicts.
- **Section 1:** the gate record workflow now gates on the printed tables, compared line for line with the Windows tables committed in `scans/phase0/measurements/tables`, and reports the raw records without failing. No per-platform reference records.
- **Section 3:** both edits to `tools/scan_analysis/scrub_exif.py`, as written.
- **Section 4:** the three README replacements as written, with the platforms between `<!--platforms-->` markers, and `ReadmeTests` requiring them to equal the matrix in `.github/workflows/ci.yml`. The Planned paragraph that still said "once CI exists" is corrected too.
- **Section 6:** noted.

Reported in `docs/PHASE1-RESULTS.md` "Entries 49 and 50". Sections 1 to 3 answer your three questions. Section 4 is a defect in the README that has been live for days. Section 5 is the finding in your own report that deserves more attention than either gate.

### 1. Decision: Linux goes green, by changing what is compared rather than what is allowed

You offered two ways and refused the second for the right reason. **Take neither.** There is a third and it is better than both.

**Byte-identity of the raw records was never the requirement.** It was a proxy for the requirement, and a stricter one. What entry 32 section 3 actually wants to know is whether another desktop gives the same answers. So state the comparison as that, and state it structurally rather than numerically:

> **The Phase 0 gate record reproduces on a platform when every gate verdict is identical and every printed table is identical. Raw differences below printed precision are reported, not gated.**

**This is not a gate written after seeing the results**, which is what entry 17 section 2 rules out, because it sets no tolerance and names no magnitude. It says which artefact carries the claim. Printed precision is what a person acts on, which is the same argument entry 24 made about the panel: the number somebody reads is the number that has to be right.

Under that rule: **Windows passes. Linux passes. macOS fails**, because two of its study tables differ in print.

**And it still has teeth.** Your `telephoto3.jpg` case moved a bull 0.30 dmm, which is 0.0118 in, more than twice the 0.005 in gate. That shows up in print and it fails. A rule that would have let that through would be worthless, and this one does not.

**Do not commit per-platform reference records.** Three reasons, the first of which is the one that matters:

1. **Two committed records that disagree make the repository assert two truths, with nothing in it saying which is right.** Today Windows is the reference and a difference is a question. With three references, a difference is nobody's problem.
2. It changes the question from "do the platforms agree" to "does each platform still match itself", which is a weaker thing to know.
3. 12 MB per platform, growing with every study added.

**Keep the raw comparison in the workflow as a reported diagnostic.** Print the largest difference per field into the summary as you already do. A future change that moves a number stays visible without failing the run.

**On the emails.** Making Linux green removes half of them. macOS staying red is correct and should stay noisy until it is resolved, because a gate that fails quietly is not a gate. The volume itself has a separate cause worth naming: `main` and `phase-1` are the same commit, so every push runs both workflows twice and sends four notifications for two results. That is Alan's call rather than yours, but it is where the multiplier comes from.

### 2. Decision: yes to both, and the sort is not really about this gate

**Sort the markers by identifier before use. Unconditionally, and not to make a gate green.** An order-dependent result means the same input can produce two answers on one machine. That is a defect on its own terms, and the platform comparison merely found it. This is the same argument as entry 48 section 2 about making an iteration deterministic: a measurement tool that gives the same answer twice is worth having whatever the gate says.

**Yes, rerun macOS from Windows' corners in Windows' order.** It is cheap and it answers a question nothing else answers: whether corner refinement is the only place the platforms diverge, or the first of two. If the records then match, the story is complete. If they still differ, there is a second divergence downstream and we would otherwise have stopped looking.

### 3. Decision: `scrub_exif.py`, and the change is mine to hand you

The script's own docstring says the C# scrubber is authoritative and that the two whitelists must be kept identical, so this is catching up rather than a decision. **Make these two edits and commit them with the rest.**

In `KEEP_EXIF`, add:

```python
    piexif.ExifIFD.LensModel,
```

In the `WHAT IS KEPT` block of the docstring, after the `FNumber` line, add:

```
    LensModel                        which camera in a multi-camera phone took
                                     the frame.  The same 35 mm equivalent can
                                     come from different physical lenses in
                                     different modes, and focal length alone
                                     cannot tell them apart
                                     (NOTES-FROM-PLANNING entry 48)
```

I am handing this over rather than editing the file myself because of entry 44: I create files in the inbox and never write a tracked file. That rule cost a data-loss scare to learn and it is not worth an exception for four lines.

### 4. The README says the project only builds on Windows, and it has been saying it for days

Alan asked whether the README is being kept current. **It is not, and the damage is worse than staleness.** Under `## Building`:

> Windows is the only platform currently buildable: the imaging backend references the Windows-native OpenCV runtime package unconditionally. macOS and Linux need that reference made conditional, which is a known and small fix.

**Every word of that is now false.** Entry 32 conditioned the runtime packages days ago, CI builds and tests on Windows, Linux and macOS at Core 757 and App 35, and a second workflow reruns the Phase 0 gate record on all three. The public front page of the project is telling every visitor it does not run on their machine.

**Why `ReadmeTests` did not catch it.** It guards what is marked: links, the sheet count, the framework, em dashes. This is an unmarked prose claim, and entry 31 section 3 said deliberately not to generate the prose. That was right, and the gap is that **a prose claim contradicting a fact the repository already knows is still a checkable claim.**

**Three replacements. The wording is mine; place it as written or tell me what is wrong with it.**

**a. Replace the whole `## Building` paragraph above with:**

> GroupLab builds and its tests pass on Windows, Linux and macOS, and every push runs the suite on all three. The desktop application is offered as a build for Windows today: before Linux or macOS is offered, the Phase 0 gate record has to reproduce on that platform rather than merely compile, which is tracked in `docs/PHASE1-RESULTS.md`.

**b. Replace the last sentence of `## Concept screens` with:**

> The application today has a marking screen and a print screen. They now carry the palette, the type and the marks these screens are drawn in, and not their layout: there is no navigation rail, no composite plot and no analysis screen yet.

**c. In `## Status`, under "What does not exist yet", remove "the assignment editor" and add to the list of what exists:**

> - a sheet that names its own definition from its printed codes, so no target has to be named by hand
> - an end-to-end `analyze` command, from photograph to report
> - diagnostic logging, crash records and a report package, with no location data in any of them

**And add one guard to `ReadmeTests`:** read the platform matrix out of `.github/workflows/ci.yml`, and fail if the README names fewer platforms as buildable than CI actually runs. It is the same shape as the framework guard and it would have caught this the day it went stale. Put the claim between markers so the test has something to read, the way the sheet count and the framework already are.

### 5. The finding in your report that outranks both gates

Buried in the macOS section:

> `telephoto3.jpg` has identical corners on macOS, yet one bull moved 0.30 dmm because the edge fit kept 29 edge points instead of 30.

**Read that again with the platforms taken out of it.** Identical inputs to the edge fit. One point of thirty included or not. The bull moves 0.0118 in, which is more than twice the gate.

**That is not a platform problem and it will not be fixed by anything in sections 1 or 2.** The same flip can happen between two photographs of the same target, between two frames in one session, or between two runs on one machine if anything upstream moves by a hair. **It is a robustness defect that the platform comparison happened to expose**, and this weekend is about to produce exactly the marginal frames that trigger it: paper with holes in it, shot through printed rings, photographed on a board in daylight.

**Worth understanding before the weekend rather than after.** Three questions, in order:

1. **Why is one point of thirty worth 0.30 dmm?** Either that bull's fit is badly conditioned, or the dropped point is unusually influential. Both are answerable by refitting with each point left out in turn and reporting the spread.
2. **Is it a hard include or exclude where a soft weight belongs?** A threshold that flips a point in or out converts a continuous input into a discontinuous output, which is the mechanism here. Robust weighting instead of rejection would make the output move continuously with the input.
3. **Does the stage record say when a fit is near its threshold?** If a bull is one point away from changing its answer, the person reading the figure should be able to find that out. This is entry 39 section 1 and entry 40 again, from a fourth direction: the pipeline knows something the interface is not using.

**Do not change anything yet.** Measure it, report it, and I will write what it should do. But do it before entry 43, because a marginal-frame sensitivity that moves a bull twice the gate is worth more than any screen.

### 6. Answering Alan's other question honestly, for the record

He also asked whether the interface is being driven toward the concept screens. The honest answer is that entry 42 landed the palette, the type, the density and the marks, and that the layout is entry 43 and has not been started, on my instruction. The window matches the screens in how it looks and not in what is on it: no navigation rail, no three-column analysis screen, no composite plot, and a twenty-button toolbar across two rows where the concept has a 46 pixel top bar.

**That sequencing was and still is right**, because the analysis screen needs the analysis path wired into the window and entry 33 built it as a command. But it is his project and his question, and I have told him plainly where it stands rather than defending the order again.

---

## 2026-09-15, entry 48: my diagnosis was wrong, and the two decisions you asked for

**Status: actioned 2026-09-15.**
- **Section 1:** noted.
- **Section 2, Linux:** explained on your three terms. The S2 corners, corner sets and inlier flags are identical to Windows in every record. The first quantity that differs is S3's homography from `Cv2.FindHomography`, the only computation between those corners and the mapping, and it moves a page point by at most 3.4e-7 dmm on the scans.
- **Section 2, macOS:** localised and not yet explained, and nothing is changed. It diverges one stage earlier, at S2's corner refinement, and a bull on a four-marker photograph moved 0.30 dmm when its edge fit kept one edge point fewer. The workflow stays red on macOS.
- **Section 3:** `LensModel` is kept, and the rule is written into the scrubber. The log's whitelist now reads and records the same camera and exposure facts, and a test holds the two lists equal. The owner corpus is republished at `grouplab-testdata` commit `d35ef99`: 13 of its 26 photographs carry a lens model and changed, and the other 13 are byte-identical. `tools/scan_analysis/scrub_exif.py` says to keep its whitelist identical to the scrubber's and does not keep `LensModel`. It is yours, so it is not edited. `LensModel` is not added to the lens grouping key.
- **Section 4:** `docs/DETECTION-PIPELINE.md` now says the wrong names must stay at zero and every refusal stays.

Reported in `docs/PHASE1-RESULTS.md` "Entry 48". Section 1 is mine to account for. Sections 2 and 3 are the decisions. Section 4 is one number in your report worth more than the rest of it.

### 1. Entry 47's diagnosis was wrong, and the way it was wrong matters more than that it was

You opened the Linux and macOS packages. Both carry the WeChat QR code. The failing tests read no code rather than crashing, and the real cause is that on two synthetic 300 DPI images each QR module is under 5 pixels, too small for those builds to read. Doubling the resolution as a second attempt fixes it, and capping at 8,000 px kept a 600 DPI scan from going from 9.0 to 52.7 seconds.

**Three things went wrong in how I reached the wrong answer, and only the third is about luck.**

1. **I stated a package fact I had not checked.** Entry 47 says official OpenCV releases do not carry contrib modules, in the flat voice of a fact. I could not open the packages, said so two paragraphs later, and let the claim stand anyway. Saying afterwards that you could not verify something is not the same as not asserting it, and a reader takes the assertion.
2. **I had a clue and did not use it.** A missing native symbol throws. Whatever I was looking at produced "exit code 1" with no crash trace in the annotations. I never asked what a missing-symbol failure would have looked like compared with what I was seeing, which is the question that would have killed the hypothesis before I wrote it down. Instead I reasoned from the platform split alone, and the platform split is equally consistent with any behavioural difference between builds, which is what this turned out to be.
3. **I built one mechanism instead of a list.** With a platform split and one suspect commit, the honest output is "the log will name it; here are the two or three mechanisms worth looking at first". I produced one mechanism with supporting detail, which reads as a conclusion no matter how it is labelled.

The instruction to discard it if the log said otherwise did its job, and you did exactly the right thing. **That does not make the entry cost nothing**, because a confident wrong direction is worth negative time even when it is correctly ignored, and if you had been less careful it would have cost you an afternoon removing a detector you needed.

**What I take from it for the rest of this project:** where I cannot reach the evidence, the entry says what would settle it and stops, rather than filling the gap with a mechanism that fits.

The fix you built is better than what I would have specified. Trying double resolution second, rather than switching detectors, keeps the coverage and costs nothing on images that already work, and the 8,000 px cap is the kind of limit that only shows up when somebody actually measures the run time.

### 2. Decision: the gate record. Linux is explained. macOS is not yet

Entry 32 section 3 asks for a byte-identical comparison **or an explained difference**. It never said what explained means, which is my omission, so here is the standard.

**A difference is explained when all three of these hold:**

- **a. Every gate verdict is identical on all three platforms.** Pass is pass and fail is fail, on every frame.
- **b. The computation that diverges is named, down to a stage.** Not "floating point", which is the name of a category rather than a cause. Which stage, which quantity.
- **c. The mechanism is named and bounds the difference**, so that we can say why it cannot grow into a.

**Linux meets this, or is one sentence away from it.** Every printed table is identical and the raw records differ only below printed precision. Name the stage and the mechanism in a sentence and it is explained. **Mark Linux explained and move on.**

**macOS does not meet it, and the gap is real rather than pedantic.** Two measurement studies differing in the third or fourth significant figure is roughly a part in a thousand. That is far above last-bit noise, so something is taking a different path, not just rounding differently. The gate tables being identical is reassuring and it is one sample: if an iterative fit is converging to a slightly different point, then on a marginal frame the same mechanism moves a verdict, and a marginal frame is precisely what the weekend is about to produce.

**So: keep that workflow red on macOS, and do not loosen the gate to make it green.** Entry 17 section 2 already settled the general form of this: a number chosen after seeing the results is not a gate.

**The work, and it should be small.** The stage records exist for exactly this. Run the two diverging studies on all three platforms with the trace on, and find the first stage whose metrics diverge. That localises it to a computation, and the computation will suggest the mechanism. My guess, offered as a guess: an iterative fit, `LevenbergMarquardt` or the surface fit, where a different order of operations changes the convergence path. If that is what it is, **the better answer is to make the iteration deterministic rather than to accept the difference**, with a fixed iteration count, a fixed ordering and a fixed termination test, because a measurement tool that gives the same answer twice is worth having for its own sake, quite apart from this gate.

Report what you find before changing anything. If it turns out to be genuinely below the precision anybody could act on, that is an explanation too, and then macOS is explained on the same terms as Linux.

### 3. Decision: yes, add `LensModel` to the keep list

**Add it.** Four reasons, in order of weight.

1. **It is the same class of information as `Make` and `Model`, which are already kept.** It names a piece of glass, not a person, not a place and not a time. There is no privacy argument against it that does not also argue against the two fields already published.
2. **On a multi-camera phone it is the field that says which camera took the shot.** Entry 27's lesson was that the lens grouping key was missing a dimension, `DigitalZoomRatio`, and that one missing dimension made a submission unusable. The same 35 mm equivalent focal length can come from different physical lenses in different modes, and `LensModel` disambiguates that where focal length alone cannot. **This is the strongest reason and it is a measurement reason, not a completeness reason.**
3. **The corpus is published for work nobody has designed yet.** A lens model string is the first thing an outside researcher would want and the cheapest thing for us to have kept.
4. **Consent is not a barrier.** The contributors agreed to publication with location removed. A camera-technical field is inside what a reasonable person expects when `Make` and `Model` are already there.

**Republish the corpus that is already out, rather than leaving two generations of scrubbing in it.** It is 26 images, the originals are retained in `C:\Dev\grouplab-originals` and `C:\Dev\grouplab-submissions`, and a corpus where some files carry a field and others do not for no reason a reader can see is worse than either choice applied consistently. Doing it now costs an hour. Doing it at two hundred images costs a day and an explanation.

**And write down the principle, so the next field is decided rather than negotiated.** A whitelist that grows one request at a time becomes a blacklist with extra steps. The rule I would state:

> **Keep what describes the camera and the exposure. Drop everything that describes where, when, who, or anything a person typed.**

That keeps make, model, lens, focal length, 35 mm equivalent, f-number, digital zoom, ISO, exposure time and orientation. It drops the GPS block, every timestamp, maker notes, and every free-text field. It gives a straight answer to the next field somebody asks about, and it matches the whitelist in entry 41 section 2 for the diagnostic log, which is the same judgement made about the same data for a different purpose. **Two whitelists that disagree would be a defect; make them agree.**

**One open question rather than a decision:** whether `LensModel` should join the lens grouping key alongside `DigitalZoomRatio`. That is a measurement question, not a judgement, and the corpus can answer it once the field is being kept. Do not add it to the key on my say-so.

### 4. The number in your report that deserves more attention than it got

| Platform | Identified correctly | Wrong |
|---|---|---|
| Windows | 33 of 37 | **0** |
| macOS | 33 of 37 | **0** |
| Linux | 32 of 37 | **0** |

**Zero wrong, on every platform.** The identification either reads the sheet's own codes and names the definition it was printed from, or it declines and asks. It never confidently names the wrong one.

That is the difference between a feature and a hazard. A sheet identified as the wrong definition would scale every measurement on it by a wrong number and produce a page of confident figures that are all wrong, with nothing anywhere saying so, which is the failure mode of entries 24, 39 and 40 arriving by a fourth route. **Four of thirty-seven declining is a small inconvenience. One of thirty-seven being wrong would have been a defect worth stopping for.** Whatever you did to make refusal the default on a damaged frame, two definitions, two tiles or an unknown identifier: that is the part to protect in every future change to it.

Worth a line in `docs/DETECTION-PIPELINE.md` saying so, so that a later contributor tuning the identifier for a better hit rate knows which number is allowed to move.

---

## 2026-09-15, entry 47: build and test is red on Linux and macOS, and I think I know why

**Status: actioned 2026-09-15.**
- **Section 1:** agreed, and the gate record stays red until planning decides. `build and test` is green again on all three platforms at `eddee00`, Core 756 and App 34 on each.
- **Section 2: the diagnosis does not hold, on two kinds of evidence.**
  - **The packages, opened:** `libOpenCvSharpExtern.so` in `OpenCvSharp4.official.runtime.linux-x64` 4.13.0.20260627 and `libOpenCvSharpExtern.dylib` in `OpenCvSharp4.runtime.osx.arm64` both carry the `wechat_qrcode_WeChatQRCode` exports, as the Windows DLL does.
  - **The logs:** the failures read "no code on the sheet could be read", not a missing entry point. On the same runs, the identification tests on three printed scans and a photograph passed on Linux and macOS, which they could not have done if the constructor threw.
  - **The cause:** detection on two synthetic images whose code modules are under five pixels, the clean 300 DPI render of GL-CF25-LTR on Linux and the end-to-end test's rotated render on both.
  - **The fix:** `b315853` tries double resolution second, and a failing identification test now reports what each detector found.
- **Section 3:** no capability fallback, because the module is on all three platforms. The point stands, so coverage is measured per platform. `grouplab identify sweep` runs in the gate record workflow and counts how many of the 37 Phase 0 images name the definition and tile they were printed from. Windows and macOS name 33 and Linux 32, none wrongly on any platform; the one Linux misses is `gl-cf25-ltr-1-600-dpi.png`, which the other two read only at quarter resolution. Double resolution is now skipped past 8000 px, which took the slowest 600 DPI scan from 52.7 s back to 9.0 s and lost no image.
- **Section 4:** the rule is taken. `fd05dac` and `eddee00` went up in the same push as the fix, `b315853`, before this entry arrived and before the fix's CI result. Under the rule the fix would have gone alone. From here, the gate record's red is named as expected in the commit message.
- **Section 5:** gate C2 is written into `docs/PHASE1-RESULTS.md` as a measured result, under the gate table and in "Entry 35 section 6". Entry 46 section 5 is closed.

Reported in `docs/PHASE1-RESULTS.md` "Entry 47". Read this before your next commit. Section 1 separates two failures that look like one. Section 2 is a diagnosis I cannot finish from here and you can confirm in one command. Section 3 is the shape of the fix. Section 4 is a process point that matters more than the bug.

### 1. Two different red lights, and only one of them is a problem

| Commit | build and test | phase 0 gate record |
|---|---|---|
| `48913ff` | green, three platforms | not yet existing |
| `bac369d` | green, three platforms | not yet existing |
| `c622591` | **green, three platforms** | fails ubuntu and macos, windows passes |
| `a97bcb0` | **fails ubuntu and macos**, windows passes | fails ubuntu and macos, windows passes |
| `b0091ad` | **fails ubuntu and macos**, windows passes | fails ubuntu and macos, windows passes |

**The gate record failing is the gate working.** That workflow has never run before, it measures the one thing entry 32 section 3 said was "not yet claimed", and a red result on two platforms is a measurement rather than a defect. Whatever it reports is the first honest answer the project has ever had to that question, and the per-field difference reporting you built into it is exactly right. **Leave that red until it is understood; do not chase it green.**

**`build and test` is a different matter.** It was green on all three platforms at `c622591` and red on two at `a97bcb0`. That is a regression in the suite that has been the project's floor all week, and two further commits have been pushed on top of it.

### 2. The diagnosis, and how to confirm or kill it in one command

`a97bcb0` is the only commit in that range that touches `src/GroupLab.Cli/Imaging/OpenCvSharpBackend.cs`, and what it adds is this:

```csharp
using var locator = new WeChatQRCode("", "", "", "");
```

**`WeChatQRCode` is `opencv_contrib`'s `wechat_qrcode` module, not core OpenCV.** The runtime packages the project references are not the same build on every platform:

- `OpenCvSharp4.runtime.win` is a contrib-bearing build, which is why Windows is green.
- `OpenCvSharp4.official.runtime.linux-x64` is built from the **official** OpenCV releases, and official releases do not carry contrib modules.
- The two `osx` runtime packages are in the same position.

Passing four empty model paths correctly avoids needing the neural network files, but it does not avoid needing the native symbol to exist. If it is absent, the constructor throws at runtime on exactly the two platforms that are red, at exactly the commit that introduced it, leaving the third green. **Every piece of evidence I can reach fits, and I could not reach the last one:** nuget.org is blocked from both of my environments, so I could not open the three packages and list their native libraries. That is the step that turns this from a well-supported hypothesis into a fact, and it is one command on your machine:

```
dotnet test tests/GroupLab.Core.Tests --filter <the QR or identification tests>
```

run on Linux, or simply reading the failing job's log, which you can do and I cannot. **If the failure names a missing entry point, a missing shared library, or a type initializer on `WeChatQRCode`, this entry is right. If it names something else, throw this diagnosis away and tell me**, because then I have reasoned myself into a tidy story and that is worse than having no story.

**This is entry 32 section 1 in a new costume.** That entry found the repository could be built only on Windows because of a Windows-only OpenCV package, and it said in as many words: check the exact package ids on nuget.org rather than taking mine, because these runtime packages differ in what they contain. The same trap, one module deeper.

### 3. The fix, which is not "drop the WeChat locator"

Your own comment in that file says why not: on the Phase 0 scans the plain `QRCodeDetector` missed the codes on the 600 DPI scan, on a tile and on a photograph, and the WeChat locator found all three. **Removing it costs real detection coverage on the platform where it works.** So the fix is not removal.

**Detect the capability once, at runtime, and record the difference.**

1. **Try to construct the locator once and cache the outcome.** If it throws, fall back to the plain detector alone for the life of the process. Catch narrowly and by type, not `Exception`.
2. **Say so where a person will see it.** Identification quietly getting worse on two platforms, with nothing anywhere saying why, is the failure mode entries 39 and 40 were both about: the pipeline knows something the interface is not using. It belongs in the log at `app.start` as a capability line, and in the stage record for identification, and in the message the user gets when identification fails, which should say that the sheet could not be identified and that `--target` will do it.
3. **The tests have to state the expectation per platform rather than skip quietly.** If 29 of 37 images identify with the locator and fewer without it, then the test asserts the number the platform can reach and names the reason. **A test that is silently skipped is a gate that has quietly stopped existing**, which is the thing `docs/PHASE0-RESULTS.md` is careful about everywhere else.
4. **Then record it as a real platform difference** in `docs/DETECTION-PIPELINE.md` and in whatever the README eventually says about platform support. "Works on all three" and "works best on one" are different claims and the project does not get to make the first one.

If it turns out a contrib-bearing runtime package does exist for linux-x64 and osx, that is a better answer than any of the above and it makes points 1 to 4 unnecessary. **Check that first**, and check it by opening the package rather than by reading its description.

### 4. The part that matters more than the bug

`b0091ad` is entry 37 sections 3 to 5. It is good work and it is unrelated to the breakage. **It was committed and pushed onto a suite that was red on two of three platforms.**

The CI workflow's own comment says Linux and macOS "are now required". `DESIGN.md` section 21 says a phase is not complete until its gate passes. A required check that gets committed over becomes an advisory one, and it happens by increments exactly like this one, where each individual commit is defensible and the aggregate is a project that no longer knows whether it builds.

**The rule I would like, and will keep asking for: when a required check goes red, the next commit is the one that makes it green, or a commit that deliberately reverts.** If a red light is expected and understood, as the gate record's is, say so in the commit message so that the exception is a decision rather than a habit.

Nothing needs reverting now. `b0091ad`'s work is wanted and its tests will pass once the underlying cause is fixed.

### 5. Two things worth recording while they are in front of us

- **Gate C2 has been measured for the first time.** Entry 32 section 3 has said "not yet claimed" since it was written, and `docs/PHASE1-RESULTS.md` repeated it. It is now claimed, measured, and failing on two platforms, with per-field differences reported. That is a real milestone even though it is red, and it should be written into `PHASE1-RESULTS.md` as a measured result rather than left as a workflow that exists.
- **Entry 46 section 5 is closed.** Both workflows are on `actions/checkout@v5` and `actions/setup-dotnet@v5`, so the Node.js 20 deprecation is gone.

---

## 2026-09-15, entry 46: two questions off the screenshots, one decision I owe you, and what to do next

**Status: actioned 2026-09-15, in section 4's order.**
- **Sections 0 to 2:** see below.
- **Section 4 items 3 and 4:** entry 35 section 6 and entry 37 sections 3 to 5, reported there.
- **Section 3:** the count line above the figures reads like "3 shots: 1 detected, 1 corrected, 1 placed by hand.", naming only the kinds present, and the old "Placed:" line at the foot of the panel is gone. Each row of the shot list carries its provenance in faint, a new `faint` text style over the palette's faint colour. Nothing on the image changed.
- **Section 5:** both workflows moved to the v5 checkout and setup-dotnet actions when the gate record workflow was written. The artifact upload uses v6, the first version that runs on Node 24.
- **Section 0:** `main` fast-forwarded to `48913ff` and pushed.
- **Section 1, the calibre:** every impact ring was the calibre, not the measured size, and the canvas comment said "true hole diameter" as the entries do. In the close-up all three rings on bull 13 draw at 0.308 in. Shot 2, which reads 0.508 in, was ringed again in alert only 6 screen pixels outside its own ring. The impact ring stays at the calibre, as you offered. The alert ring on a flagged hole is now drawn at the size it reads, 0.508 in on shot 2, so the oversize case is visible on the image at scale, and the screenshot test asserts both diameters.
- **Section 2, a real run logs:** the screenshots come from the headless test, which starts no log. A Debug build run from the repository was driven through UI Automation to open a Phase 0 scan through the real file dialog, then closed. It wrote `out/logs/grouplab-20260915-180437-32256.log` holding `app.start`, `dialog.open`, `dialog.result`, `image.open` by name and hash, and `app.exit code=0`. `out/logs` had not existed before, because no build with logging had been run from the repository.

Reported in `docs/PHASE1-RESULTS.md` "Entry 46". Short. Sections 1 and 2 are questions rather than instructions, and I would rather have the answers than guess. Section 3 settles something entry 42 left open. Section 4 is the order.

Entries 41, 42, 44 and 45 are actioned, CI is green on all three platforms at `f0973ed`, and I have read `docs/PHASE1-RESULTS.md` rather than asking you to repeat yourself. The window now looks like a designed instrument instead of a default one, the marks read on paper, on printed rings and on a dark backer in the donated photograph, and the panel refuses to print a centre from aim when it has no aim. **That is the check that had been open since Friday and it passes.**

### 0. `main` is one commit behind

Alan committed the paper protocol as `48913ff` on `phase-1` and pushed. `main` is still at `f0973ed`. Fast-forward it and push, so the two stay level as they have been all week.

### 1. Is the impact ring drawn at the true hole diameter, or at the calibre?

In `out/screens/marks-closeup-dark.png`, the three impacts on bull 13 are drawn as rings of **the same size**, and shot 2 is the one the panel flags as reading 0.508 in across against a 0.308 in calibre. Entry 42 section 5 says the ring is at the true hole diameter, and entry 43 section 3 repeats it for the composite plot. The stated reason is that the picture should be scale honest, so that two holes marked as one look wrong on the image rather than only in the text.

**I am not calling this a defect from a screenshot.** The check is one number: the drawn diameter of shot 2's ring in inches, against 0.308 and against 0.508.

- If the ring is the **calibre** for every shot, that is a reasonable design and it is not what either entry says, so tell me and I will change the entries rather than have you change the code.
- If it is the **measured size**, then shot 2's ring should be about two thirds again the size of the others and it is not, so something is clamping it.

Either way, **the oversize case should be visible on the image**, because entry 40's whole lesson was that a person looking at the picture had no way to see what the number was telling them.

### 2. Does a real run say "Logging is off: logging has not started"?

That is what the Diagnostics panel reads in both window screenshots. If that is the headless test, where no log directory is wanted, fine and say so. **If that is what a Debug build run from the repository shows, then entry 41 section 4 is not doing the one thing it exists for**, which is that logs appear in `out/logs` without Alan having to do anything, and I would be reading an empty directory the next time he says something misbehaved.

Worth an explicit check rather than an inspection of the code: run the window, open an image, close it, and confirm a file appeared.

### 3. Shot provenance: my decision, since entry 42 dropped it without meaning to

You flagged that entry 42 section 5 gives one impact colour, and that the gold, orange and green coding for automatic, corrected and manual shots is therefore gone from the image. That was my omission rather than your change, and you were right to name it.

**The decision: it does not come back to the image, and it does come back in the list.**

- **Not on the image**, because colour there already carries selected and excluded, and the rule in entry 42 section 2 is that amber means "this one". Five meanings on one ring is how a legend stops being readable, and the image's job is geometry.
- **A provenance column in the shot list**, per shot, in `faint`, so it reads without shouting.
- **A count line above the figures**: something like "12 shots: 9 detected, 2 corrected, 1 placed by hand." That is the thing a person actually wants to know, and it is one line rather than twelve.

**The reason provenance matters at all is worth writing down**, because it is easy to treat as bookkeeping: it is the record of where a human judgement entered a measurement. A group that is nine detected shots and a group that is nine hand-placed ones deserve the same figures and a different amount of confidence, and only the application knows which it is looking at.

### 4. Order

1. **Section 0**, fast-forward `main`. One command.
2. **Sections 1 and 2**, the two answers. Both are quick and both might change what section 3 is worth doing on top of.
3. **Entry 35 section 6**, still the oldest open item: the Phase 0 gate reproduced byte for byte on Linux and macOS, and making `--target` unnecessary by reading the definition identifier off the image. This is also gate C2 of the check I am running, so finishing it turns a "never measured" row into a measured one.
4. **Entry 37 sections 3 to 5**: record what the donated submissions showed, and carry the contributor's stated target dimensions into the provenance as structured fields.
5. **Section 3 of this entry.**

**Entry 43 stays closed this week.** It is Phase 4 and nothing about it has become urgent.

### 5. One piece of housekeeping, not urgent

Every CI job, including the green ones, now carries a GitHub annotation: `actions/checkout@v4` and `actions/setup-dotnet@v4` target Node.js 20, which is deprecated, and are being forced onto Node.js 24. Nothing is broken and nothing needs doing this week. Move both to v5 the next time you are in `.github/workflows` for another reason.

### 6. What is happening on my side, so you do not wait on it

I am working through a top to bottom check of the project: every actioned entry against what is actually in the tree, the privacy chain end to end, the gates, documentation drift, and loose ends on disk. **Findings will arrive as later entries. Do not wait for them and do not go looking for the plan**; it is mine to run, and where it turns up something for you it will turn up as an ordinary entry in the inbox.

---

## 2026-09-15, entry 45: the crash report wire contract, fixed, because the server half now exists

**Status: actioned 2026-09-15, with entry 41.**
- **Sections 1 and 2:** the client builds them exactly. Tests check its entry list against the receiver's patterns and the crash record against the schema.
- **Sections 3 and 4:** it sends one request, and shows the reference or the error verbatim.
- **Section 6 point 1:** `docs/CRASH-REPORTING.md` carries sections 1 to 4 verbatim, with the privacy statement in plain words.
- **Section 6 point 2:** `scripts/Get-TargetSubmissions.ps1` is your version with `-CrashReports`, reviewed and committed.

Reported in `docs/PHASE1-RESULTS.md` "Entries 41 and 45". Depends on entry 41 and should be actioned with it, not before it. This entry exists so that the client you build and the receiver I have written cannot disagree, because the two are being written in different places by different people and that is exactly how wire formats end up mismatched.

The receiver is written and tested. It is at `Claude outputs/crash-report.php`, with its deployment runbook at `Claude outputs/CRASH-REPORT-SERVER.md`, both outside the tracked tree because that directory is ignored. Alan places them when he has twenty minutes. **You do not wait for that**, because entry 41 section 7 already says the client ships with the endpoint blank and the save-to-disk path working.

### 1. The package, which is now a checked contract rather than a description

The receiver refuses any zip containing an entry that is not on this list. The names are matched anchored and case sensitively, and any entry containing a slash, a backslash or `..` is refused outright, so **the package is flat: no directories inside it.**

| Entry | Required | Notes |
|---|---|---|
| `crash-YYYYMMDD-HHmmss-<pid>.json` | when there was a crash | section 2 |
| `grouplab-YYYYMMDD-HHmmss-<pid>.log` | at least one | this run, and the previous run |
| `environment.txt` | yes | the expanded `app.start` block |
| `description.txt` | optional | what the user typed, may be empty |
| `contact.txt` | optional | may be empty |

At most 24 entries, at most 40 MB unpacked, and at most a 100 to 1 compression ratio. The client's own cap is 2 MB for the zip; the server's wall is 5 MB.

**Nothing else is accepted, and that is the privacy guarantee made mechanical.** There is no image type on that list, so a package carrying a photograph is refused by the server even if a future version of the client puts one in by mistake. I tested that case specifically rather than assuming it. If you ever find yourself wanting to add a file to the package, the list in the PHP has to change at the same time, and that friction is the point.

### 2. `crash-*.json`, exact shape

The pull script reads this to print one line per report saying what crashed, so the field names matter. `exceptions` is ordered outermost first.

```json
{
  "schema": 1,
  "created_utc": "2026-09-15T06:42:12.104Z",
  "app":  { "version": "0.1.0+3f9c2a1", "commit": "3f9c2a1", "channel": "debug" },
  "environment": {
    "os": "Windows 10.0.26100", "framework": "net10.0", "renderer": "Direct2D1",
    "culture": "en-US", "display_scale": 1.5
  },
  "last_action": "print.select-target",
  "exceptions": [
    { "type": "System.InvalidOperationException",
      "message": "The control TextBox already has a visual parent.",
      "stack": "   at GroupLab.App.PrintWindow.ShowFields()..." }
  ],
  "stages": []
}
```

`stages` holds the `StageRecord` set when an analysis was in flight and an empty array otherwise, per entry 41 section 5. `last_action` is a short stable identifier rather than prose, so that repeated reports group. **`schema` is 1 and it increments if any of this changes**, because a receiver reading a future format should be able to say so rather than guess.

The example above is not invented. It is the crash you fixed this evening, written in this format, and I used it as the test fixture for the pull script.

### 3. The request

- `POST` to the configured URL, `multipart/form-data`.
- File field name: **`report`**, the zip.
- Text field: **`version`**, the application version string, optional but send it.
- Nothing else is read. No headers are required. No authentication.

### 4. The response

Always JSON, always with an `ok` boolean.

```json
{ "ok": true, "reference": "2026-09-15_1a2b3c4d", "sha256": "6b80184e..." }
```

**Show the reference to the user** so they can quote it. On failure:

```json
{ "ok": false, "error": "That report package contains a file this server does not accept: IMG_1580.jpg" }
```

The `error` string is written to be shown to a person whose application has just crashed, so **display it verbatim rather than mapping status codes to your own wording.** The codes you will see are 400, 405, 413, 415, 422, 429, 500, 503 and 507, and every one of them means keep the file and do not retry automatically. Entry 41 section 7 already says one attempt and no background retry queue; this is the reason it says that.

### 5. What I verified rather than asserted

The receiver was run against nine payloads on a local PHP 8.4 server: a well formed package, one with a photograph added, one with a `../../etc/passwd` entry, one with a subdirectory, one with a `.php` entry, one with a log named outside the pattern, a file that is not a zip at all, a 60 MB zip bomb, and an empty post. The good one stored and returned its reference and hash; the other eight were refused with the right status and a sensible message. The Cloudflare address matcher was tested separately against 19 boundary cases including both ends of every range shape it has to handle.

The pull script's new mode was parsed with the PowerShell 7.4 parser and its new code paths were run: default resolution, override behaviour, the crash verify branch against a real zip and meta, and the summary reader.

**None of that proves the client works.** It proves the far side of the wire is not the thing that will be wrong.

### 6. Two small jobs for you

1. **Write `docs/CRASH-REPORTING.md`** as entry 41 section 7 asks, and put sections 1 to 4 of this entry in it verbatim, plus the privacy statement in plain words. It belongs in the repository because the client is GPL and anybody can read what it sends anyway; writing it down is the difference between a project that can be trusted on this point and one that merely asks to be.
2. **Copy `Claude outputs/Get-TargetSubmissions.ps1` over `scripts/Get-TargetSubmissions.ps1` and commit it.** It is the existing script with a `-CrashReports` switch, the mode block, a verify branch for single-zip items, a summary that prints what crashed, and the sudoers note extended. It parses clean and the changed paths are exercised above. I am not committing it myself, for the reason in entry 44.

---

## 2026-09-15, entry 44: I am what overwrote the notes file, and the mailbox convention changes today

**Status: actioned 2026-09-15.** Entries 41 to 44 are folded into this log from `docs/notes/inbox/`, and their inbox files are deleted. The convention is recorded in "How to use this file" above and in `CONTRIBUTING.md`. `docs/notes/inbox/README.md` is committed, so the directory exists in a clean clone. Do this one first. It is short, it costs about fifteen minutes, and it removes the reason the last three hours contained a data-loss scare.

### 1. Stop looking for what wrote that file

You asked, reasonably, that somebody find out what saved over `docs/NOTES-FROM-PLANNING.md`, in case it does it again. **Do not spend a session on forensics. It was me, and here is the evidence rather than an apology.**

At 05:37:11 UTC on 15 September I took a snapshot of `docs/NOTES-FROM-PLANNING.md`, 229,605 bytes, holding entries 1 to 40. I prepended three new entries to that snapshot in memory and wrote the whole 273,172-byte result back over the file some minutes later, while you were in the middle of editing the same file to mark entries 39 and 40 actioned. **Two processes were rewriting one file with no locking and no merge, and one of them was working from a snapshot that was already stale when it was taken.**

I cannot prove from here that my write is the specific event you found at 23:56 local, because what you describe recovering held only entries 1 to 22 and what I wrote held 1 to 43. So one of two things happened: either there was a second writer as well, or the sequence was more tangled than either of us can reconstruct without the reflog. **What is not in doubt is that my write was unsafe, that it would have destroyed your status edits regardless, and that the recovery work you did was caused by me.** You did the right thing at every step of it, including refusing to overwrite the damaged file before understanding it.

**Nothing was lost.** The only things I wrote after entry 40 were entries 41, 42 and 43, and all three are in this delivery. There is nothing for you to re-request and nothing for Alan to re-send.

### 2. The new convention, which is the actual fix

The defect is not carelessness on either side. It is that a single 270 kilobyte append-only log is being rewritten in full by two writers who cannot see each other. Being more careful does not fix that; changing the shape does.

**From now on I never modify `docs/NOTES-FROM-PLANNING.md`. I only create new files.**

- New entries arrive as **one file per entry** at `docs/notes/inbox/entry-NN.md`, where `NN` is the entry number. Each file holds exactly one entry, beginning with its `## date, entry NN: title` heading and its `**Status: open.**` line, in the same form as the entries in the log.
- **I create those files and nothing else.** I never open an existing file for writing in that repository again. If I need to correct an entry I have already delivered, I deliver a new entry that corrects it, which is what entries 37 and 41 section 0 already did anyway.
- **You own `docs/NOTES-FROM-PLANNING.md` exclusively.** When you action an entry, fold its file into the top of the log in the existing newest-first order, set its status, and delete the file from the inbox. You are then the only writer that file has ever had, and a collision is not possible.
- If two of my files are waiting, fold them in ascending entry number so the log's order stays right.

**Why this and not a lock or a convention about timing.** A lock needs both sides to honour it and I cannot see your process. A timing convention fails the first time Alan asks me for something while you are running, which is most of the time. Separate paths cannot collide at all, which is a property rather than a promise.

### 3. What to do now

1. Fold `entry-41.md`, `entry-42.md`, `entry-43.md` and this file into `docs/NOTES-FROM-PLANNING.md`, newest last to first so entry 44 ends up at the top, and delete the four inbox files.
2. **Amend the "How to use this file" paragraph at the top of the log** to record the convention: entries arrive in `docs/notes/inbox/`, Claude Code is the only writer of the log itself, and folding an entry in is part of actioning it. Written down, it survives both of us forgetting.
3. Add `docs/notes/inbox/` to the repository with a `README.md` of two sentences saying what it is, so the directory exists in a clean clone and nobody deletes it as junk.
4. Commit that as its own change before starting on entry 41.

### 4. Two things from your report, answered

**Press, drag, release was the right call and it stands.** Alan's words were click, drag, then click to set, and you built press-drag-release instead. Press-drag-release is what every drawing tool on every platform does, it works with a finger without a second tap, and a plain press and release with no movement still places a shot, so the simple case is unharmed. Click-drag-click is a modal interaction: between the two clicks the application is in a state the user cannot see and cannot leave except by clicking. **Keep what you built.** If Alan tries it and dislikes it, he will say so and it is a small change, but do not pre-emptively build the other one.

**The marks you have now are a partial version of entry 42 section 5 and that is fine.** You gave every mark a dark outline, which is the right instinct and the right direction. Entry 42 gives the exact form, a 3 pixel dark halo at 55 percent under a 1.6 pixel coloured core, with the specific colour per mark type. Do not redo the outline work now; entry 42 will replace those values wholesale when it lands, and there is no point painting the same wall twice.

---

## 2026-09-15, entry 43: the analysis screen, written down at last

**Status: open.** This is the expensive half of Alan's question about the concept screens, and it is **Phase 4 work**, behind entry 39, entry 41 and entry 42. Do not start it this week. It is written now because he asked for it and because entry 42 needs to know what it is making room for.

**The single most important instruction in this entry: the window is a view over what `grouplab analyze` already computes.** Entry 33 built the end to end command and `src/GroupLab.Core/Analysis/SheetAnalysis.cs` holds the result. The screen renders that object. **There is no second analysis implementation and no statistic is computed in the UI layer.** If a figure the screen needs is not in the result object, it goes into the result object and the command gains it too, so the command and the window can never disagree. That property is worth more than any layout in this entry.

The four concept screens are in `docs/figures/screens/`. They are the reference and they are accurate, but the text below governs where the two differ.

### 1. The shell the screens share

A 56 pixel navigation rail on the left: wordmark, then Analyse, Library, Sessions, Compare, Rifles, then a spacer, then Settings at the bottom. Icons are 19 pixel line drawings at 1.6 stroke, in `faint`, and `amber` on `#221c12` for the current screen.

A 46 pixel top bar: breadcrumb on the left reading wordmark, then the load, then the date and distance in `dim`; on the right the registration pill, then Show work, Export, and Report as the single primary button.

**The registration pill is the honesty indicator and it is always visible.** "registered, residual 0.0007 in" in teal when the sheet registered; a plain state when it did not; **never absent**. Every number on the screen depends on the scale being right, and the pill is the one place that says whether it is. When the scale came from a hand drawn reference rather than from registration, the pill says that instead, in `amber`, and says what the reference was.

### 2. The analysis screen

Three columns: 300, flexible, 372.

**Left column, top to bottom:**

1. **Sheet.** A thumbnail grid of the bulls, each drawn as a small ring set with its impacts as filled dots. This is a map, not a picture: it is the fastest way to see that the shots landed where you think they did, and to notice that one bull has two holes and another has none. Clicking a bull scrolls the shot list and highlights those shots on the plot.
2. **Load, read from the sheet.** Cartridge, bullet, powder and charge, brass, primer, seating with CBTO, and the definition identifier in `faint`. **The heading says "read from the sheet" and it is literal**: these came off the printed load block that the shooter filled in, per the paper protocol, and the screen should not present typed-in data and read-off data as though they were the same thing. Where a field was typed rather than read, mark it.
3. **Shot list**, filling the rest of the column. Columns: number, x, y, r, and a tag. Mono, 11.5 point, tabular, 3 by 14 padding, signed x and y with an explicit plus so the column aligns. Tags are 9.5 point uppercase: `contested` in amber, `worst` in dim, `excluded` in dim struck through. A flagged row gets a `#1e1a12` background.

**This is the same control as entry 39 section 4, and it must be built once.** The editor needs a shot list, this screen needs a shot list, and they are the same list with the same selection behaviour. If two of them get written, the two will diverge within a month.

**Centre column: the composite plot.** Section 3.

**Right column: the figures.** Section 4.

### 3. The composite plot

This is the most important thing the application draws, because it is the picture of the project's whole argument: every shot from every bull, overlaid on its own bull's centre, making one group out of twenty five one-shot groups.

**What is drawn:**

- **Each shot as a ring at its true hole diameter**, in `impact`, stroke 1.6, with a centre pip. Not a dot. The plot is to scale and the holes are to scale, and a reader should be able to see that two shots overlap.
- **The group centre** as a small teal cross.
- **CEP 50 and CEP 90** as dashed teal circles.
- **Extreme spread** as a dashed amber line between the two shots that define it, with both shot numbers labelled. Naming the two shots is what turns extreme spread from a number into something a person can check.
- **The point of aim** as a cross, distinct in shape from the group centre, never as a circle.
- **A scale bar** in the current linear unit, and axis ticks at round values.

**The plot's extent is a round number of units and is not fitted to the data.** This matters more than it sounds. Auto-fitting makes a half inch group and a three inch group look identical, which is precisely the illusion this project exists to dispel. Pick the extent by rounding up to the next step in a fixed ladder, show it, and let a good group look small.

**Every drawn element appears in the legend beneath the plot, and nothing appears in the plot that is not in the legend.** Four entries in the concept screen, one per element. If a fifth thing gets drawn, the legend grows.

**Interaction.** Hovering a shot highlights the matching list row; hovering a row highlights the shot. Clicking either selects, in amber, and the exclusion control acts on the selection. Excluding a shot redraws the plot and every figure immediately, and **the count in the heading changes with it**, because a group of twenty four that was twenty five needs to say so.

**Export** produces PNG and SVG of the plot alone, at a stated size, with the legend, the scale bar and the load line included, because the exported plot ends up in a forum post with no context around it.

### 4. The figure stack

The order is deliberate and it is an argument, so keep it.

| Figure | What is shown beneath it |
|---|---|
| **Rayleigh sigma**, as the lead, at 29 point on `panel2` | the confidence interval, and the angular equivalent |
| Extreme spread | the two shots that define it, the angular equivalent, and **"no interval, ES has no useful one"** |
| Mean radius | the confidence interval and the standard deviation |
| CEP 90 | CEP 50 and CEP 95 beside it |
| Offset from aim | x and y components and the angular equivalent |
| Group width by height | standard deviation in x and in y |

**Sigma leads because it uses every shot.** Extreme spread sits second because it is the number everybody quotes, and the line underneath it doing the teaching is the most valuable sentence on the screen: it is the only place a reader learns, without being lectured, that the figure they have always used has no confidence interval worth printing.

**Every figure carries its interval, or states why it has none.** This is entry 24 and entry 39 section 1 generalised into a layout rule: **no bare number appears anywhere on this screen.** Where a figure cannot be computed, the row says what is missing instead of printing something, in the place where the number would have been, not in a side panel.

**Every interval carries its real coverage**, "94.7% interval" and not a rounded 95, which entry 40 recorded as already working in the current panel. Do not lose it in the rewrite.

**Below the figures, the notes.** Short paragraphs, 11.5 point, `dim`, with the lead clause in `text` and semibold. Two are known already and both are in the concept screen:

- Circularity: "**Circular within tolerance.** Pitman-Morgan p = 0.476, so there is no evidence of vertical stringing in these 25 shots."
- The flyer note, which entry 40 confirmed is already reasoning correctly in the current panel: "**Shot 22 is not a flyer.** It sits at 2.27 mean radii from centre. At n = 25 the expected worst shot is 2.18, and two thirds of honest groups this size contain one past 2.00."

**A note appears only when it has something to say.** A screen of permanently present, permanently hedged sentences teaches nobody anything. If the group is not circular, the note says so; if it is unremarkable, the note is absent.

**At the bottom, one disclosure row**: "Full CEP table, bivariate fit, comparison". It opens the secondary panel of `DESIGN.md` section 19, and that panel remembers that it was opened.

### 5. Show your work

`DESIGN.md` section 19 already specifies this and `src/GroupLab.Core/Trace/StageRecord.cs` already implements the record. **The screen is a second rendering of records that exist, not a new feature.** The console form the CLI prints and the timeline the window shows come from one object; if they can disagree, it has been built wrong.

**Left column:** the stage list. Header reading "Pipeline, 1787 ms total" with a proportional progress strip beneath it, one segment per stage, width by duration, so the expensive stage is visible at a glance. Then one row per stage: id in mono, name, milliseconds in mono, a status dot in teal, amber or alert, and a one line summary. Selecting a stage shows its record.

**Below it, the artefact panes**, two by two: the expected artwork rendered from the definition, the observed image, the difference residual, and the registration residual with its vector field. Each pane has a caption naming the stage and one figure in italics on the right.

**Right column:** the console form, verbatim, in mono, exactly the text `grouplab analyze --trace` prints, including the indented continuation lines that carry the alternatives and the reasons. Not a prettier version of it. The same text, so that a user reading the screen and a user reading a pasted terminal dump are reading the same thing.

**Two constraints, already written in `DESIGN.md` section 19 and repeated here because they are the ones that get lost:** the trace must never be the only place an error appears, so a failed stage also produces a normal, prominent error with the trace as the detail behind it; and the theatre must not slow the pipeline down, so artefact generation defaults on for one interactive analysis and off for batch.

**Clicking a rejection in a stage record highlights it on the image.** That is the feature that turns this from a log viewer into a tool.

### 6. Compare loads

**Cards**, one per load, each with its heading, shot count and date, a small version of the composite plot at the same fixed scale as every other card, and three figures: sigma with its interval, mean radius with its standard deviation, extreme spread with its angular equivalent. **Same scale across cards is not optional**; two plots at different scales side by side is a lie told by a layout.

**The verdict block**, in prose, leading with the conclusion. When the answer is that the loads cannot be told apart, the heading says so in those words: "These two loads are not distinguishable on this evidence". Then the reasoning: the point difference, the overlap of the intervals, the test and its p value, and the sentence that has to be there, that a non-significant result is not evidence that they are the same.

**The sample size table**, and this is the part of GroupLab that does not exist anywhere else:

| To resolve a difference of | Shots per load | Rounds total | Sessions at 25 a sheet |
|---|---|---|---|

with the row matching the observed difference highlighted. **This table is the project's argument in its most useful form** and it belongs on the screen rather than in a help page. A person who came to find out which load is better leaves knowing why fifty rounds could not have told them, and roughly what would.

**Right column:** an interval comparison, one horizontal bar per load showing the confidence interval with the point estimate marked, on a shared axis, with a sentence beneath it about how much they overlap. Then "Tests run", a plain two column list naming each test and its result: sigma ratio by likelihood ratio, group centre by Hotelling's T squared, circularity by Pitman-Morgan, and the bootstrap method and replicate count. **Name the tests.** A reader who wants to check the work can, and a reader who does not is unharmed by four lines of small text.

Then "What would help more": the honest redirect, when the data supports one. If measured velocity standard deviation accounts for only a fraction of the observed vertical at distance, say so, because it means a charge weight search will not move the thing the shooter is trying to move.

### 7. What this entry deliberately does not specify

The Library, Sessions and Rifles screens. The report generator behind the primary button. The light theme's plot colours, which need checking against printed output rather than deciding here. The mobile layouts.

And one thing worth saying plainly: **the README currently carries six concept screens above the fold with one sentence admitting the application looks nothing like them.** That sentence is accurate and it is doing a great deal of work. Once entry 39's editor and entry 42's styling land, the gap narrows enough that the wording should be revisited, and once this entry lands it should be removed. Until then it stays, because it is true.

### 8. Verification

1. **No statistic is computed in `GroupLab.App`.** A test asserting that the application project references no statistics type directly, or a review that says so explicitly.
2. The window's figures and the `grouplab analyze` output agree, to the digit, on the same input. Drive it from the same fixture the command's tests use.
3. Excluding a shot changes the plot, every figure, every interval and the count, in one redraw.
4. The console text in the show-your-work panel is byte identical to the CLI's, asserted by a test rather than by eye.
5. Screenshots against all four concept screens, sent to me.

---

## 2026-09-15, entry 42: the styling pass, with the concept screens' actual values

**Status: actioned 2026-09-15, with three departures named in the report.**
- **What was built:** the palette, type scale, spacing, control styles and marks, in `src/GroupLab.App/Theme` (`Tokens.cs`, `AppStyles.cs` and `Marks.cs`). IBM Plex is embedded with its licence, and dark, light and follow-system themes are remembered.
- **Contrast:** six text colours fell below 4.5:1, and each was adjusted along its own hue.
- **Tests:** every earlier test passes unchanged. New tests cover contrast, colour literals, the theme choice, and screenshots written to `out/screens` for you.
- **The three departures:**
  - units are not yet set smaller than their figures, because the existing tests read the figure text;
  - the image no longer colour codes provenance, because section 5 gives one impact colour;
  - mark labels are light text on a dark plate, because the mark's colour as text could not be read.

Reported in `docs/PHASE1-RESULTS.md` "Entry 42". Alan asked whether the window will come to look like the concept screens on its own, and the honest answer is no, it will not, because nobody ever wrote the screens down as a specification. This entry is the cheap half of fixing that. Entry 43 is the expensive half.

**This ranks behind entry 39.** Do the crash, the bull assignment and the editor first. This is chrome, it changes no number and no behaviour, and it must not be allowed to delay the editor. **It is its own commit.**

### 1. What this pass is and what it is not

The six concept screens in the README were built as HTML and rendered to PNG. They are not decoration: they encode a palette, a type scale, a density and a layout that were chosen deliberately, and `DESIGN.md` section 19 already states the reasoning behind the identity, the typography, the density split and the four themes. **What was never done is the translation from those screens into values the application can use.** So the application drifted into Avalonia's Fluent defaults with `Brushes.OrangeRed` bolted on, and it looks nothing like the pictures, and that is nobody's fault except the absence of this document.

This pass delivers **the shell: palette, typography, spacing, control styles, and the marks drawn on the image**. It does not deliver the composite plot, the figure stack, the stage timeline or the comparison screen. Those are entry 43.

**The constraint that keeps this honest: after this pass, every existing test still passes unchanged.** `MainWindow.StatisticsText` is already exposed for the tests to read. If a styling change moves a number, a label or a sentence, the tests will say so and the change is wrong. Chrome only.

### 2. The palette, exactly

These are the values from the concept screens, not approximations of them. Dark is the primary and the concept screens are dark. Light is the same structure with the roles swapped.

**Dark:**

| Token | Value | Used for |
|---|---|---|
| `bg` | `#131417` | window background |
| `panel` | `#1a1c20` | top bar, panel surfaces |
| `panel2` | `#212429` | raised surfaces, buttons, the lead figure block |
| `sunk` | `#0f1013` | the navigation rail, the image and plot area |
| `line` | `#2c3037` | panel separators, one device pixel |
| `line2` | `#3a3f47` | control borders, the breadcrumb separator |
| `text` | `#e6e8ea` | primary text |
| `dim` | `#9aa1a9` | secondary text, figure names, legends |
| `faint` | `#697079` | section labels, units, row numbers |
| `amber` | `#e0912f` | the one accent: selection, primary button, highlighted shot |
| `teal` | `#6fbfa8` | confirmations, CEP circles, group centre |
| `alert` | `#e0604a` | errors and warnings |
| `paper` | `#eceae4` | printed paper, in drawn illustrations |
| `bull` | `#0b0c0e` | printed ink, in drawn illustrations |
| `impact` | `#c8442f` | a bullet hole, on the image and in the plot |

**Amber is the only accent colour and it means "this one".** Selection, focus, the current tool, the primary action, the highlighted shot. If a second accent appears for a second purpose, the first one stops meaning anything.

**Light** keeps every role and swaps the ramp: `bg #f4f3f0`, `panel #ffffff`, `panel2 #eceae4`, `sunk #e4e2dd`, `line #d3d0c9`, `line2 #bdb9b0`, `text #1a1c20`, `dim #5a6068`, `faint #868c94`. The three signal colours darken slightly for contrast on light: `amber #b46f16`, `teal #3f8873`, `alert #bf4531`. Check each text pair against a 4.5:1 contrast ratio and adjust the value rather than the role if one falls short.

**Deliver dark, light, and follow system in this pass.** Avalonia's `ActualThemeVariant` gives follow-system for free once the two palettes exist. High contrast is the fourth theme in `DESIGN.md` section 19 and it is later work; do not fake it by bumping the contrast of the dark theme.

**Replace `Brushes.OrangeRed` and `Brushes.DarkOrange`** in `MainWindow.cs` with `alert` and `amber`. Those two raw named colours are the loudest thing on the screen right now and neither is in the design.

### 3. Typography

**Faces.** IBM Plex Sans for chrome, IBM Plex Sans Condensed for the wordmark, IBM Plex Mono for every numeric readout. IBM Plex is licensed OFL-1.1, which is GPL compatible, so bundling it is clean. **Embed the three families as resources in `GroupLab.App`** rather than relying on the system, because the concept screens are only reproducible with them and a missing face silently changes every measurement column. Add IBM Plex and its licence to `THIRD-PARTY-NOTICES.md` in the same commit.

Fallback stacks, for the case where embedding fails: sans falls back to the system UI face; mono falls back to `Cascadia Mono, Consolas, Menlo, monospace`, which is what `MainWindow.cs` already declares.

**Scale.** These are the concept screens' sizes and they map one to one onto Avalonia's device independent pixels.

| Role | Size | Weight | Notes |
|---|---|---|---|
| Section label | 10 | 600 | uppercase, letter spacing 0.09em, colour `faint` |
| List header | 10 | 600 | uppercase, letter spacing 0.06em, colour `faint` |
| Table row | 11.5 | 400 | mono, tabular figures |
| Secondary text, notes, legends | 11.5 | 400 | colour `dim`, line height 1.55 |
| Body, labels, buttons | 12 to 13 | 400 to 500 | 13 is the base |
| Wordmark | 13 | 700 | condensed, letter spacing 0.1em |
| Figure value | 21 | 500 | mono, letter spacing -0.01em |
| Lead figure value | 29 | 500 | mono, colour `text` |

**Every numeric readout is mono with tabular figures.** This is already half true in `MainWindow.cs` and it needs to be all true, including the shot list from entry 39 section 4. Set the `tnum` font feature where Avalonia exposes it; IBM Plex Mono is tabular by default, so the mono readouts are safe either way, and the feature matters only if a proportional face is ever used for numbers, which it should not be.

**Units are set in the surrounding text size and the `dim` colour, not in the figure size.** "0.1046 in" is a 21 point number followed by a 13 point unit. A full size "in" next to a measurement competes with the digits for no reason.

### 4. Layout and spacing

The concept screens use a 56 pixel navigation rail, a 46 pixel top bar, and a three column work area of 300, flexible, 372. Hold those numbers. The current window has a wrap panel of buttons across the top and a single 380 pixel right panel, which is close to the 372 by accident and nothing else matches.

**Do not build the navigation rail in this pass.** It navigates to screens that do not exist. Leave the current toolbar in place and style it. The rail arrives with entry 43.

**The spacing scale is 4, 6, 8, 12, 14, 20.** Section padding is 12 vertical by 14 horizontal. Panel separators are one device pixel in `line`. Control margins are 2. Row padding is 3 by 14. Nothing gets a value off this scale without a reason written next to it.

**Corner radius is 3 for surfaces and 4 for buttons.** Not 8, not pill shaped. The application is a measuring instrument and it should read like one.

**Buttons.** Default: background `panel2`, border `line2`, text `text`, 12 point at weight 500, padding 6 by 12. Primary: background and border `amber`, text `#17120a`, weight 600. **At most one primary button is visible at a time.** Toggle buttons in the on state take the rail treatment: text `amber` on `#221c12` with a `#3a2d18` inner border.

**Status pills**, for things like the registration result: mono, 11 point, 4 by 9 padding, 3 radius, border `line2`, text `dim`; in the good state, text `teal`, border `#2c463f`, background `#141f1c`.

### 5. The marks drawn on the image, which is the part that is not cosmetic

Entry 39 section 5 said the white impact ring is unreadable on white paper, and set the general rule: **every mark the application draws must be legible on a photograph of a target**, which is mostly white paper with black printing and coloured rings, sometimes on a dark backer, sometimes in hard sunlight with a shadow across a third of it. That is a harder constraint than looking good against the application's own dark chrome, and it is the one that decides these values.

**The answer is a two tone stroke, not a colour choice.** Any single colour loses against something in the corpus. Every mark is drawn as a 3 pixel stroke in `#0b0c0e` at 55 percent opacity, with the mark's own colour stroked at 1.6 pixels on top of it. The dark halo reads on paper and on bright ring colours; the bright core reads on ink and on a dark backer. Stroke widths are in screen pixels and do not scale with zoom, so a mark stays visible at any magnification.

| Mark | Colour | Shape |
|---|---|---|
| Impact, normal | `impact` `#c8442f` | ring at the true hole diameter, plus a one pixel centre pip |
| Impact, selected | `amber` | same, stroke 2 |
| Impact, excluded | `dim`, dashed | same |
| Scale reference | `teal` | line with a filled circle at each end, per entry 39 section 3 |
| Point of aim | `teal` | a cross, not a circle, so it never reads as a shot |
| Bull centre, when shown | `faint` | a small cross |
| Detector rejection | `alert`, dashed | ring |
| Missing marker | `alert` | the existing red cross is right, keep it |

**The impact ring is drawn at the true hole diameter rather than at a fixed screen size.** It is a measurement and it should look like one, and an oversized mark hides the thing it is marking. Entry 40's snapped-to-artwork case is visible immediately when the ring is the size of a bullet, and invisible when every mark is a fixed twelve pixel circle.

### 6. Where this lives in the code

The shell is built in C# with no XAML at all, and that is a reasonable choice for a window this size. Keep it.

- **`src/GroupLab.App/Theme/Tokens.cs`**: one static class, every value in section 2 and 3 as a named member, two palettes, resolved by theme variant. **No colour literal appears anywhere else in the application.** That is the property that makes the light theme possible and the one that quietly fails if it is not enforced from the start.
- **`src/GroupLab.App/Theme/AppStyles.cs`**: the Avalonia `Styles` collection for buttons, toggle buttons, text blocks, panels and pills, added in `App.Initialize()` after `FluentTheme`.
- **`src/GroupLab.App/Theme/Marks.cs`**: the pen and brush definitions from section 5, used by `MarkingCanvas` and later by the plot, so the image and the plot cannot disagree about what a shot looks like.

Add a test that greps the application sources for colour literals outside `Tokens.cs` and fails if it finds any. It is a crude test and it will save the light theme from dying by a thousand hard coded greys.

### 7. Verification

1. Every existing test passes unchanged. If `StatisticsText` moved, revert and find out why.
2. Screenshots of the marking window in dark and light, side by side with `docs/figures/screens/analysis-dark.png` and `analysis-light.png`. They will not match, because the content is different and that is entry 43. **The palette, the type and the density should match.** Send them to me and I will say whether they do.
3. Open `gl-cf25-ltr-1-600-dpi.png`, place an impact on bare paper, one on a printed ring, and one on a printed numeral, and confirm all three marks are readable. Then do the same on one of the donated photographs with the shadow across it.
4. The colour literal test from section 6.

---

## 2026-09-15, entry 41: the application logs nothing at all, and here is the diagnostics specification

**Status: actioned 2026-09-15, in the order of section 8, one commit a step.**
- **Section 0:** noted. The quotation is `DESIGN.md` section 13, and nothing built on it changes.
- **Sections 3 and 4:** a log per run, written to `out/logs` by a Debug build, rotated to twenty files or 20 MB. It records every dialog, file open and save, detection run, and catch that used to swallow its error.
- **Section 2:** a file is its name and a salted hash, and image facts come through a whitelist. Paths are scrubbed from values, messages and stacks, and tests prove none reaches the log.
- **Section 5:** three handlers, the crash record in entry 45's schema with the stages of a detection in flight, and the next-launch banner.
- **Section 6:** the package, built only from the receiver's whitelist, and the dialog that shows it before saving.
- **Section 7:** the upload behind an empty default address, one attempt, and `docs/CRASH-REPORTING.md`.
- **Not done as written:** the print crash was fixed before logging existed, so there are no log lines from it to paste. A test throws from a click handler instead and checks the record.

Reported in `docs/PHASE1-RESULTS.md` "Entries 41 and 45". Section 0 is a correction to entry 39 that costs you thirty seconds. Section 1 answers a question of fact that Alan asked. Sections 2 to 8 are a specification. **This entry ranks behind entry 39 and entry 35 section 6.** Fix the crash first; build this so that the next crash leaves evidence behind it.

### 0. Correction to entry 39, section 6: the citation is wrong

Entry 39 quoted the editor-before-detector argument and attributed it to `DESIGN.md` section 18. **It is section 13, "Assignment and manual editing", at line 309.** Section 18 is "Storage and synchronisation" and says nothing about editors. The quotation itself is verbatim and correct, and the instruction built on it stands unchanged. Only the section number was wrong, and I would rather correct it than have you go looking in the wrong place.

### 1. The answer to Alan's question is no, nothing, and that is exactly why the print crash told us nothing

He asked whether the application is doing debug logging now. I checked the sources rather than assuming, because I have twice this week stated a fact from one example and been wrong. What I found:

- **`Program.cs` is 1,203 bytes and `Main` is a single line.** The builder chain ends with `.LogToTrace()`. That is Avalonia's own framework logging, written into `System.Diagnostics.Trace`. `OutputType` is `WinExe`, so on Windows there is no console attached, and no trace listener is registered anywhere in the solution. **That output goes nowhere and is discarded.** It is not application logging and it never was.
- **There is no global exception handler.** Nothing in the solution references `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, or Avalonia's dispatcher unhandled exception event.
- **There are eight `catch` blocks in the entire shell.** Two in `MainWindow.cs`, three in `AppSettings.cs`, three in `PrintWindow.cs`. Every one of them is narrow by exception type, which is good practice, and every one of them either sets a status string or silently continues. **Not one of them records anything anywhere.**
- **No logging package is referenced by any project.** `GroupLab.App.csproj` carries `Avalonia.Desktop` and `Avalonia.Themes.Fluent`, and nothing else.

So when Alan selected two targets in the print dialog and the window disappeared, the application had no way to tell anyone what happened, and neither did he. **That is the real cost of having no logging and the project has already paid it once.** It will pay it repeatedly once anyone other than Alan runs this.

One thing does exist and must not be confused with this. `src/GroupLab.Core/Trace/StageRecord.cs` is a structured record of pipeline stages, per `DETECTION-PIPELINE.md` section 6.1, and it is real and populated. **That is an explanation of an analysis, written for the user.** It is not diagnostics, it is not written to disk, and it does not survive a crash. Section 5 joins the two at one seam. They stay separate things with separate purposes.

### 2. The rule that outranks everything else in this entry

**A log file must never contain a GPS coordinate, and must never contain the contents of a photograph's metadata block. A crash package must never contain a photograph.**

I am putting this first, before any design, because the failure mode is not hypothetical and it is not subtle.

`GroupLab.Core/Imaging/ImageMetadata.cs` reads EXIF, and EXIF on a phone photograph routinely carries the latitude and longitude of the place the photograph was taken. Entry 37 section 4 recorded that both iPhone submissions arrived carrying GPS. The single most natural line of code to write when adding logging to an image open is "record what we read from the file", and writing that line puts a contributor's home address, or the location of Alan's range, into a plain text file. Section 6 of this entry then offers to zip that text file up and section 7 offers to upload it to a public web server.

**The project spent two days of rewriting git history to remove exactly those coordinates. Do not reintroduce them through the diagnostics channel.**

The positive form of the rule, which is what you implement:

1. **The logger takes a fixed, enumerated set of image facts and nothing else.** Permitted: pixel width, pixel height, bit depth, channel count, declared DPI and its source, EXIF orientation, camera make, camera model, lens model, focal length, focal length in 35 mm equivalent, f-number, digital zoom ratio, ISO, exposure time, and the count of EXIF tags present. Everything else is excluded, and that includes every tag in the GPS IFD, every maker note, every timestamp, and every free-text field such as `ImageDescription`, `UserComment`, `Artist`, `Copyright` and `XPComment`.
2. **The enumeration is a whitelist in one place**, not a blacklist scattered across call sites, for the same reason `scrub_exif.py` uses a whitelist.
3. **A test asserts it.** Build a synthetic image carrying a GPS block, a maker note and an `Artist` field, run an image open through the logger, and assert that none of those strings and no coordinate appears anywhere in the produced log text. This is the direct sibling of the `PublicationTests` GPS assertion, and it is written the same way: **the test creates the GPS block itself** so that it is testing the logger rather than testing whatever happens to be on disk.
4. **File paths are not logged.** See section 3.

### 3. The log file

**Location.** One directory, chosen per platform by the normal convention, resolved once at startup and reported in the first line of the log itself so nobody has to guess:

| Platform | Directory |
|---|---|
| Windows | `%LOCALAPPDATA%\GroupLab\logs` |
| macOS | `~/Library/Logs/GroupLab` |
| Linux | `$XDG_STATE_HOME/grouplab/logs`, falling back to `~/.local/state/grouplab/logs` |

`GROUPLAB_LOG_DIR` overrides it everywhere. Section 4 uses that.

**One file per run**, named `grouplab-YYYYMMDD-HHmmss-<pid>.log`, UTC in the name so the files sort. A second process starting in the same second gets a distinct name because the pid is in it.

**Format.** Plain text, one event per line, fixed leading columns so it reads as a table in Notepad and parses with a split. After the fixed columns, zero or more `key=value` pairs, values quoted only when they contain a space.

```
2026-09-15T18:42:07.104Z  INFO  app.start      version=0.1.0+3f9c2a1 os="Windows 10.0.26100" framework=net10.0 renderer=Direct2D1 culture=en-US units=inch
2026-09-15T18:42:19.882Z  INFO  image.open     file="gl-cf25-ltr-1-600-dpi.png" pathid=7f3a2c11 w=5100 h=6600 dpi=600 dpisource=png-phys
2026-09-15T18:42:31.507Z  WARN  marking.size   shot=1 measured=0.521in expected=0.338in reason=oversize
2026-09-15T18:43:02.119Z  ERROR print.render   targets=2 ex=System.NullReferenceException at GroupLab.App.PrintWindow.Preview+0x4c
```

Timestamps are UTC with milliseconds, in ISO 8601, always. Local time in a log file that gets mailed to another timezone is a trap.

**Paths are not written.** The `file=` field carries **the file name and extension only, never the directory**, because directories on Windows begin `C:\Users\<the person's actual name>` and frequently continue into folder names that identify people. `pathid=` is the first eight hex digits of a SHA-256 of the full path, salted per run, which lets you see that the same file was opened three times without disclosing where it lives. If you ever genuinely need the directory to diagnose something, ask the user for it in that moment; do not collect it in advance.

**Levels.** `ERROR`, `WARN`, `INFO`, `DEBUG`. `INFO` is the default. `DEBUG` is switched on by a `--verbose` command line flag or a settings checkbox, and the setting is remembered in `AppSettings`.

**Rotation.** Keep the newest twenty files, or 20 MB in total, whichever bites first, and delete the oldest on startup. Never unbounded. A log directory that grows forever is a defect that shows up in a year as a support question about disk space.

**Failure is silent and total.** If the log directory cannot be created or written, the application starts normally with logging disabled and says so nowhere except in the settings screen. **Logging must never be able to prevent the application from running.** Wrap the writer so that an exception inside it cannot propagate.

**Threading.** Writes go through a single background writer with a bounded queue. If the queue fills, drop `DEBUG` first and count the drops, then emit one `WARN` recording the count. A logger that blocks the UI thread is worse than no logger.

**Flush discipline.** `ERROR` flushes immediately. Everything else may buffer, but the buffer flushes at least every two seconds. A crash must not take the last four seconds of context with it, because the last four seconds of context is the whole point.

**What gets logged, at minimum:**

- `app.start`: version including the commit hash, operating system and version, framework, Avalonia rendering backend, display scale, culture, and the resolved unit preferences. This block answers most first questions without anybody asking them.
- `app.exit`: exit code and run duration.
- Every file open and every file save, per the field rules above.
- Every detection and analysis run: a one-line summary, and the whole `StageRecord` set at `DEBUG`. See section 5.
- **Every currently swallowed `catch`.** All eight of them get a line. Today `AppSettings` can fail to read settings and the user is never told and neither are we; that becomes one `WARN`.
- Every dialog opened and its result, because "what was he doing when it died" is the first question every time.
- Unhandled exceptions, per section 5.

### 4. Getting logs to me without Alan doing anything

This is the part Alan asked for specifically and it is nearly free.

**In a `Debug` build, the log directory defaults to `<repository root>/out/logs`** unless `GROUPLAB_LOG_DIR` says otherwise. `out/` is already in `.gitignore`, so nothing leaks into commits, and I can read `C:\Dev\grouplab\out\logs` directly whenever Alan says the application misbehaved. **He pastes nothing.** That is the whole feature.

Resolve the repository root by walking up from the executable's directory looking for `GroupLab.slnx`, and fall back to the platform directory in section 3 if it is not found, so a copied Debug build does not scatter files.

**Release builds never do this.** They use the platform directory only.

Add one line to `CONTRIBUTING.md` saying where the logs are in each case, because the next contributor will ask.

### 5. The crash record

**Install three handlers at startup**, before the window is constructed:

1. `AppDomain.CurrentDomain.UnhandledException`
2. `TaskScheduler.UnobservedTaskException`
3. Avalonia's dispatcher unhandled exception, so that an exception thrown inside a click handler is caught rather than tearing the process down

On any of them, do the smallest possible amount of work, because the process may be moments from dying:

1. Write `ERROR` with the full exception chain, every inner exception, and the full stack, and flush.
2. Write a sibling file `crash-YYYYMMDD-HHmmss-<pid>.json` in the same directory, containing: the exception type, message and stack for every exception in the chain; the same environment block as `app.start`; the name of the last user action; and **the current `StageRecord` set if an analysis was in flight**. That last item is the seam with section 1: the trace already knows the resolved parameters and the decisions taken, and a crash report carrying it is worth ten that do not.
3. Then, and only then, attempt to show a dialog.

**The dialog is best effort and the file is not.** A crashing application often cannot draw. So the reliable path is the other one: **on the next launch, if any `crash-*.json` exists that has not been dealt with, the application says so and offers the same choices.** Build that path first and treat the in-the-moment dialog as a convenience.

Alan's words were that an option should come up asking whether to generate a log package. That is right, and the next-launch prompt is how it actually reaches people, because the in-process one is exactly the thing a hard crash takes with it.

### 6. The log package

A single zip, written wherever the user chooses and offered by default on the desktop, named `grouplab-report-YYYYMMDD-HHmmss.zip`.

**Contents, and this list is exhaustive:**

- `crash-*.json` if there is one.
- The log file for the crashed run.
- The log file for the previous run, which is often where the real cause is.
- `environment.txt`: the `app.start` block, expanded, plus installed .NET version and screen configuration.
- `description.txt` if the user typed one.
- `contact.txt` if the user typed one, which is blank by default and clearly optional.

**Not included, ever:** the image being worked on, any photograph, any EXIF, any GPS, any file path, any marking file, any settings file that contains paths, and anything from the submissions directory. If a future maintainer wants an image attached, that is a separate, explicit, per-file consent, not a checkbox in a crash dialog.

**The consent dialog shows the user what is in the package before it goes anywhere.** Two buttons that matter: "Show me the file", which opens the containing folder, and "Save only". Sending is a third button and it is never the default. Nothing is sent without a click. Nothing is ever sent silently, on a timer, or at startup.

Say in the dialog, in one plain sentence, what the package contains and what it does not: something close to "This report contains error details, your GroupLab log files, and information about your computer. It does not contain your photographs or any location information."

**The zip path works today and needs no server.** Build it first and completely. A user who mails a zip to Alan is already better off than everyone is right now.

### 7. The upload, and the part that is deliberately not being built yet

Alan wants the package to reach his server so we can retrieve them. That splits into a client half and a server half, and **only the client half is yours**.

**The client contract, which you build now:**

- HTTPS `POST`, `multipart/form-data`, one file field, to a URL held in configuration. HTTPS through Cloudflare is fine here; the proxy carries web traffic normally. The SSH problem from the submission-pull script does not apply.
- **The configured URL is empty by default and the send button is hidden when it is empty.** Save-to-disk still works. That means this ships and is useful before the server exists, and it means a fork of GroupLab does not accidentally post to Alan's server.
- Client-side size cap of 2 MB. If the package exceeds it, drop the previous run's log and try again; if it still exceeds, offer save-only and say why.
- One attempt, a short timeout, no background retry queue. **If the upload fails, keep the zip, tell the user exactly where it is, and stop.** A crash reporter that retries in the background is a crash reporter that eventually sends something the user has forgotten about.
- On success, show the reference the server returns so the user can quote it.

**Write down the contract in `docs/CRASH-REPORTING.md`**: the request shape, the response shape, the size cap, the exact contents of the zip, and the privacy statement in plain words. It belongs in the repository because the client is GPL and anyone can read what it sends anyway. Writing it down is the difference between a project that can be trusted on this and one that merely asks to be.

**The server half is mine and Alan's, not yours.** I will write the receiver and he will place it, in one batch with the other server work, rather than piecemeal. Two things about it that affect your side, so you can design against them:

1. **There is no secret.** The endpoint URL ships inside an open-source client, so any token in the client is public the day it is published. The server must therefore assume anyone can post to it: a hard size cap, a rate limit per address, rejection of anything that is not a well formed zip of the expected shape, storage **outside the web root** alongside `target_uploads`, never serving the stored files back, and a switch Alan can flip to stop accepting. Do not invent a token scheme to work around this; it does not work and it creates a false sense of safety.
2. **Retrieval reuses what exists.** Rather than a second script, the crash reports will be pulled by adding a `-CrashReports` switch to `scripts/Get-TargetSubmissions.ps1`, with the same SSH mechanism, the same SHA-256 verification at both ends, and a default local root of `C:\Dev\grouplab-crashreports`. **One command fetches both.** Alan has asked that manual work be batched, and two scripts to run instead of one is exactly the sort of thing that stops getting run.

### 8. Order, and how to know it worked

1. **Section 3, the log file, plus section 4, the dev folder default.** This is the whole of the value for the next week, because the next week is Alan running it alone.
2. **Section 5, the handlers and the crash record**, including the next-launch prompt.
3. **Section 6, the zip and the consent dialog.**
4. **Section 7, the client upload behind an empty default**, and `docs/CRASH-REPORTING.md`.

**Verification, and I want these as tests rather than as assurances:**

- The GPS and metadata test from section 2 point 3.
- A test that asserts no absolute path and no drive letter appears in a log produced by opening a file from a deep directory.
- A test that throws from a click handler and asserts that a `crash-*.json` exists afterwards, is valid JSON, and names the thrown type.
- A test that the rotation policy leaves exactly twenty files after twenty five runs, oldest deleted.
- A test that the package builder produces a zip containing exactly the permitted entries and no others, driven by a list rather than by inspection, so that adding a file to the package without updating the list fails the build.
- **Reproduce the entry 39 print crash with logging in place, before you fix it**, and paste the resulting log lines into your report. That is the first real test of whether this thing does its job, and it costs nothing because you have to reproduce the crash anyway.

---

## 2026-09-15, entry 40: the tap is snapping to printed artwork, and three things are working

**Status: actioned 2026-09-15.**
- **Section 1:** the detector returns its aligned expected artwork. A tap on printed ink is placed where it was tapped, and the status line says so. The size message names both explanations, or only the printed target when the region is mostly artwork.
- **Section 3:** closed with entry 39 section 1.

Reported in `docs/PHASE1-RESULTS.md` "Entries 39 and 40". Amends entry 39, which still stands in full and in its stated order. This is one new finding and one correction to a message.

Alan re-marked the same rendered sheet with a realistic ten-shot group around bull 13, a .338 calibre and 100 yards.

### 1. Two shots snapped to the target, not to a hole

The panel reports, twice, in orange:

> Shot 1 reads 0.521 in across, larger than a single 0.338 in hole should (0.471 in): two holes marked as one?

**The diagnosis in that message is wrong, and the detection behind it is right.**

`gl-cf25-ltr-1-600-dpi.png` is a rendered sheet that has never been shot. **There are no holes in it at all.** Every tap that "snapped to the hole under it" snapped to printed ink, and the two flagged shots sit on bull 13's inner ring, which measures about 0.52 in on that sheet. The size check caught them because 0.521 in is too big for a .338 bullet, and that is the safety net from entry 24 section 5 point 3 working on its first real outing. Good.

But it reached the right conclusion by the wrong route, and it told the user the wrong thing. Two overlapping holes is one explanation for an oversized blob. **Snapping to the target's own artwork is another, and on a GroupLab sheet it is the one we can rule in or out exactly**, because the definition says where every printed ring and dot is.

**Two changes.**

1. **The snap should not land on known artwork.** When a sheet is registered, the renderer knows precisely where the printed rings, dots, numerals and markers are. A tap whose snap target coincides with artwork rather than with a difference from artwork should either not snap at all and place where the user tapped, or snap and say what it snapped to. This is the same architectural point as entry 23 section 4, where the detector had to be confined to the registered sheet: **the pipeline knows things the interaction is not using.**

2. **The message should name both possibilities rather than only the one.** Something closer to: "Shot 1 reads 0.521 in across, larger than one 0.338 in bullet hole (0.471 in). Two holes marked as one, or a tap that snapped to the printed target rather than a hole." When the shot's position coincides with artwork, say the second and not the first.

**Why this matters beyond a rendered sheet.** Entry 25's paper protocol asks Alan to shoot holes on bare paper, through printed ring strokes, and touching printed numerals, on purpose. Those are exactly the cases where a snap can latch onto ink instead of a tear. This weekend will produce the real version of what this screenshot shows by accident.

### 2. Three things worth recording as working

Not everything needs fixing and it is worth the record saying which.

- **The calibre arithmetic is right.** Extreme spread 2.333 in centre to centre, edge to edge 2.671 in, difference 0.338 in, which is exactly one bullet diameter. Entry 24 section 5 point 1 asked for both figures labelled so the number matches whatever the shooter is used to quoting, and that is what the panel does.
- **Every interval carries its real coverage**, "94.7% interval", not a flat 95. That is entry 24 section 1 and entry 23 section 2 landing where they were meant to land: on the screen a person reads.
- **The flyer line reasons correctly and refuses the obvious answer.** "Worst shot at 1.32 mean radii; a group of 10 is expected to put its worst at 1.89, so a shot there is not a flyer by that measure alone." That is the project's whole argument, in one sentence, in front of a user.

### 3. Unchanged from entry 39

Every shot still reads "manual, bull none", so entry 39 section 1 stands exactly as written. In this particular marking it does no harm, because Alan placed a point of aim and the shots cluster around one bull, so the single-aim measurement is the right one. **That is luck rather than design**, and the first person to mark a real 25-bull sheet by hand will get entry 39's wrong answer instead.

---

## 2026-09-15, entry 39: Alan's second session with the window, and the panel is reporting a meaningless number again

**Status: actioned 2026-09-15; section 6's README wording is left until the editor has been used.**
- **Section 2:** the crash was a control shown in two rows. It is fixed, with a test that reproduced it and that selects, toggles, pages and saves every sheet.
- **Section 1:** shots are assigned to their nearest bull when placed, when detection loads bulls, and when moved. Any unassigned shot on a sheet of several scoring bulls withholds the figures, with the reason where they would be, and the rule is in `CONTRIBUTING.md`.
- **Sections 3 to 5:**
  - the scale line is drawn while made, stays while waiting, and its ends drag before and after use;
  - impacts are placed by press, drag and release, and listed as rows that select and exclude;
  - no mark is white, and every mark has a dark outline.

Reported in `docs/PHASE1-RESULTS.md` "Entries 39 and 40". Section 1 is a correctness problem he did not report and probably could not have. Section 2 is a crash. Sections 3 to 6 are interaction work, and they are the current phase rather than a later one. This entry outranks everything except entry 35 section 6.

Alan opened `gl-cf25-ltr-1-600-dpi.png`, ran **Detect on a GroupLab sheet** against the matching definition, and it worked: 34 of 38 markers, registration RMS 0.0022 in, all 25 bulls located, the 4 missing markers crossed in red. That is the primary path working in the window for the first time, and it is worth saying so before the rest of this entry.

### 1. Hand-placed shots are not assigned to bulls, so the composite group silently does not happen

His panel reports **mean radius 2.294 in, sigma 1.830 in, centre from aim 2.770 in right and 1.131 in low**, from 12 shots on a sheet whose bulls sit 1.5 in apart.

**Those numbers are not measuring dispersion. They are measuring the distance between bulls.** He placed one impact near each of twelve different bull centres. The rifle's grouping is not in that figure at all.

`GroupAnalysis` is correct and is not the problem. Line 152 measures each shot from its bull's centre when the shot has a bull, and from the single point of aim otherwise. The Selected shot panel gives the reason in three words: **"Shot 12: manual, bull none"**. Every hand-placed shot on a fully registered sheet came out unassigned, so all twelve were measured from one aim point on bull 1, and the composite premise the entire target design exists to serve quietly did not apply.

**Two things to fix, and the second matters more than the first.**

1. **On a registered sheet, a hand-placed impact should be assigned to a bull automatically**, by the same nearest-bull rule `ShotAssignment` already implements for the detector, with the existing manual override for when it guesses wrong. A person marking a GroupLab sheet by hand should not have to know that assignment is a separate concept.

2. **When the composite does not apply, the panel must say so where the figures are, not in a side panel.** This is the same failure as the two-shot mean radius from entry 24, arriving by a different route: a confident figure, to three decimals, with an interval beside it, that means nothing. The rule from entry 24 generalises and should be written down as such: **whenever the panel cannot compute what the user thinks it is computing, it says what is missing instead of printing a number.** Here that is a line reading something like "12 shots, none assigned to a bull, so these figures measure the spread of your marks rather than the group. Assign them to bulls, or mark them on one bull."

**Why he could not have caught this.** The figure is plausible, the interval is plausible, the honesty line about the reference length is present and reassuring, and nothing anywhere says the shots are unassigned except three words under a heading about the one shot he happened to select.

### 2. The print screen crashes on selecting multiple targets

Selecting more than one target in the print dialog crashes the application. **Reproduce it, fix it, and add a test that selects several.** He is printing his session pack from that screen this weekend and it has been used by a person exactly once.

While you are in there: the print screen is the one piece of this the weekend depends on, so it is worth ten minutes of trying to break it deliberately rather than only fixing the reported path.

### 3. The scale tool shows nothing while it is being used

Two taps and then nothing visible until "use this length" is pressed. **Draw the line as it is being made**, with a circle at each end, and keep it drawn afterwards so the reference stays visible while the rest of the marking happens. A measurement you cannot see is one you cannot check, and this one scales every number on the screen.

**And it should be draggable.** Place by click, then drag either endpoint to adjust, rather than starting again. Same for the rectangle.

### 4. Impacts should be placeable by drag, and listed

His words, and they are right: click, then drag the point to where it belongs, then click to set. Tapping blind and hoping is the wrong interaction for a measurement tool at any zoom level.

**Each shot should also appear as a row in the right-hand panel**, clickable, selecting it on the image, with its number, its bull, and whether it is excluded. At twelve shots the current design already makes finding one specific shot a hunt around the image. At twenty-five it will be worse. The row is also where the flyer and exclusion controls belong, rather than only appearing once something is selected.

### 5. The impact marker must not be white

White circles on white paper. Use something that reads on paper, on black ink and on a dark backer, since all three occur in the corpus. The green fill he has now works; the outer ring does not.

Worth a general rule while you are choosing: **every mark the application draws has to be legible on a photograph of a target**, which is mostly white with black printing and coloured rings. That is a stronger constraint than looking good against the application's own dark chrome.

### 6. The concept screens: what is deferred and what is not

Alan asked whether the difference between the window and the README's concept screens is work set aside for later. **Partly, and the part that is not deferred is the part he is complaining about.**

**Deferred, legitimately.** The composite group plot, the full statistics layout, the stage timeline, the comparison screen, the light theme. Those are the Phase 4 Windows application in `DESIGN.md` section 21, and they need the analysis path wired into the window first, which entry 33 built only as a command.

**Not deferred: the editor.** `DESIGN.md` section 18 says it plainly, and it has said so since revision 1:

> The editor is built before the detector, not after it. A good editor with a mediocre detector is a usable product. A bad editor with a good detector still frustrates users on every target the detector gets wrong, and no detector reaches one hundred percent.

Sections 3, 4 and 5 of this entry are the editor. By the project's own stated order they come before more analysis features, not after. **Treat them as the current milestone rather than as polish**, and do not let the concept screens' absence be used to defer them, because they are a different thing.

**One honest note for the record.** The README puts six concept screens above the fold and says in plain words that they are mockups and the application looks nothing like them. That is accurate but it is doing a lot of work for one sentence, and the gap will widen before it closes. Worth revisiting the wording once the editor work here lands, so the page describes an application somebody could recognise.

### 7. Order

1. Section 2, the crash. It blocks the weekend.
2. Section 1, assignment and the honest panel. It is producing wrong numbers today.
3. Sections 3, 4 and 5, the editor.
4. Then entry 35 section 6 and the rest.

---

## 2026-09-15, entry 38: the DFdistr defect is mine and it is fixed, plus the negative zeros

**Status: actioned 2026-09-15.** Verified independently:
- `DFdistr`'s 9,440 values are 2,360 integers and 7,080 doubles, with no strings. They are bit-identical to the CSV, and none moved by more than 4.44e-16 from the 15-digit version.
- `DFlandy01`'s CSV changed in exactly the three keys named. Its JSON changed in no value, only its generation time, because jsonlite already wrote those zeros as `0`.
- No fixture CSV holds a `-0`, and the 67 statistics tests pass.

Reported in `docs/PHASE1-RESULTS.md` "Entry 38". Small, and it closes entry 36. Files are on disk. Still behind entry 35 section 6.

### 1. You were right, and the cause is exactly what you said

`sg_distr.R` converted its columns to formatted text for the CSV and then built the JSON from the same data frame, so all 8,850 table values came out as quoted strings. In `sg_dump.R` I captured the numeric values into a separate variable before formatting; in `sg_distr.R` I did not, and I did not check the output of the second script the way I checked the first. Finding it by reading the file rather than assuming my script was right is the correct instinct.

**Fixed, and verified rather than asserted:** the JSON now holds 2,360 integers and 7,080 floats and no strings, and all 9,440 values agree with the CSV to the bit.

### 2. The three negative zeros, fixed in both scripts

You found that `DFlandy01` carries three values where the CSV says `-0` and the JSON says `0`. `sprintf("%.17g", -0)` preserves the sign and jsonlite does not.

**Both scripts now normalise negative zero to `0`.** A sign that nothing reads is worth less than the two formats agreeing, and a documented discrepancy between a fixture's two representations is exactly the sort of thing that costs somebody an afternoon in a year's time. The three affected keys are `shots.y.303`, `shots.yPOA.303` and `flignerProbe.FlignerY.input.243`, and nothing else in any of the ten datasets contained one.

### 3. Files on disk

- `tools/shotgroups/sg_distr.R` and `sg_dump.R`, both corrected.
- `shotGroups_DFdistr.csv` and `.json`, regenerated, now numeric.
- `shotGroups_DFlandy01.csv` and `.json`, regenerated. **Exactly three keys changed**, all three `-0` becoming `0`, and I diffed all 33,928 keys to confirm nothing else moved.

No other dataset needs regenerating: I scanned every committed CSV for a bare `-0` value and `DFlandy01` was the only one.

### 4. Your verification answered my question, and the answer is the reassuring one

I asked whether your hand-reconstructed aimed coordinates agreed with the newly stored values. **3,775 of 3,978 matched exactly; the other 203 are all in `DFcm`, differ by at most 3.6e-15, and the stored values are the right ones** because centimetre aim points are not six-decimal numbers and your reconstruction had to round somewhere.

That is the outcome I hoped for and did not assume: the fixture is now the authority and the workaround was a workaround. Removing it was correct.

---

## 2026-09-15, entry 37: I was wrong about DO-NOT-PUBLISH, and the same photographs arrived twice with contradictory consent

**Status: actioned 2026-09-15.** Sections 1 and 2 first; sections 3 to 5 after entry 35 section 6, as entry 46 section 4 orders. Section 3: recorded. Section 4: recorded, with two corrections: the uploads came through Chrome for iOS, not Safari, and the scrubber keeps no `LensModel`. Section 5: `statedSheetSize` in the provenance record, and the rectangle tool offers it. Reported in `docs/PHASE1-RESULTS.md` "Entry 37 sections 3 to 5". Section 1: the `DO-NOT-PUBLISH` check, removed under entry 28, is restored, and either signal withholds. Section 2: `Intake.WithheldHashes` builds the hash set across all submissions, and `Intake.Run` requires it. A matching file is held, with no override by acceptance, and its provenance entry names the withheld submission. On the real submissions, `eac0bae6` is refused on both signals, and `5068047f` and `bf6d885d` each hold their one photograph as a conflict naming `eac0bae6`. Nothing is published to `grouplab-testdata` until Alan has asked the contributor. Reported in `docs/PHASE1-RESULTS.md` "Entry 37". Section 1 corrects an instruction I gave you that was false, and you may have removed working code because of it. Section 2 is a consent conflict sitting in the submissions right now. Both before anything else.

Three submissions arrived after the Discord post was edited. They are the first useful donated data the project has had.

### 1. Correction: the `DO-NOT-PUBLISH` file exists, and entry 28 told you it did not

Entry 28 section 1 said, as a numbered point: *"There is no `DO-NOT-PUBLISH` sentinel file. The opt-out is `exclude_from_public_dataset` ... do not look for a file that the page does not write."*

**That was wrong.** `2026-09-15_eac0bae6` contains a file named `DO-NOT-PUBLISH`, 129 bytes, reading:

```
The contributor asked that these photos are not published.
Testing on a private machine only. Do not add to the public data set.
```

I generalised from the single submission that existed at the time, which had not opted out and therefore had no sentinel. One example, stated as a fact about the schema. If you removed a check for that file on my say-so, **put it back.**

**Both signals must be honoured, and either one alone is sufficient to withhold.** Treat a submission as opted out if `exclude_from_public_dataset` is true **or** a `DO-NOT-PUBLISH` file is present. Do not require agreement between them, and if they disagree, withhold and say so. A belt-and-braces opt-out is the one place where redundancy is the point rather than a smell.

### 2. The same photographs were submitted twice, once publishable and once not

Two files appear in two submissions with **identical SHA-256**:

| SHA-256 (first 16) | In, publishable | In, opted out |
|---|---|---|
| `2fdb872ece4e7088` | `5068047f` / `001_IMG_1580.jpg` | `eac0bae6` / `003_IMG_1580.jpg` |
| `ce5138799f7f23ec` | `bf6d885d` / `001_IMG_1696.jpg` | `eac0bae6` / `004_IMG_1696.jpg` |

Same contributor, same phone, same credit name, within a few minutes. The likely story is that they submitted two carefully with full answers, then uploaded their whole folder for testing and marked that one do not publish.

**The rule, which is not a judgement call: an opt-out wins by content hash, across every submission.** If bytes appear anywhere in an opted-out submission, those bytes are not published from any submission. Implement it as a hash set built across all submissions before anything is published, not as a per-submission check.

**And flag the conflict rather than resolving it silently.** Publishing under ambiguous consent is the one mistake in this pipeline that cannot be undone. Hold both files, record the conflict in the provenance with both submission ids, and let Alan ask the contributor which they meant. Withholding two photographs for a day costs nothing. Publishing one the contributor did not intend costs the project its trustworthiness on the exact point the consent text makes a promise about.

### 3. The post edit worked, and here is the measurement

| | First submission, before the edit | The two after it |
|---|---|---|
| Answers filled | 0 of 6 | **6 of 6, both** |
| Usable frames | 0 of 3 | **2 of 2** |

The answers now carry backing, attachment, distance, calibre, a credit name, and in the notes the exact commercial target model. Two different calibres, 5.56 NATO and 8.6 Blackout, which is the hole-diameter variety the corpus wanted and had none of.

Worth recording in `PHASE1-RESULTS.md` or wherever the corpus is described: the difference between a useless submission and a good one was the wording of the request, not the contributor.

### 4. iOS is answered, passively, as planned

Four uploads from an iPhone XS Max on iOS 18.7 through Safari. The `accept` attribute question from the upload page specification is settled by real use rather than by testing on a borrowed handset.

**And the metadata survives the upload intact**, which was the real risk. 48 EXIF tags including `Make`, `Model`, `LensModel` reading "iPhone XS Max back dual camera 4.25mm f/1.8", focal length, 35 mm equivalent, f-number and orientation. So iPhone submissions are fully usable for the lens and surface work.

**They also carry GPS.** Both of them. The scrubber is doing real work on real contributor data now, not just on Alan's own photographs.

### 5. Both frames meet the brief, and one detail in them is worth building on

Whole target, still stapled to the backer, all four edges in frame, printed concentric rings, genuine deformation from staples and wind, one with hard sunlight and a shadow across the top third, one with a torn corner curling away from the board. This is the case the project has never had.

**The notes field names the exact target: "Action Target PR-BE6 17.5x23" and "Action Target TCT-MK3-MOD2 23x35".** That is a stated overall sheet dimension, from the person who shot it, and it is a scale reference that needs no grid and no measuring.

**Use what the contributor told you rather than building a lookup table.** Carry the stated dimensions into the provenance record as structured fields when they can be parsed, and let the manual marking path offer them as a scale reference: "this sheet is 17.5 by 23 inches, use its edges". That is more honest than a database of third-party target sizes and it improves every time somebody fills the notes in, which section 3 suggests they now will.

### 6. Order

1. Section 1, restore the sentinel check. It is a correctness fix to a mistake I introduced.
2. Section 2, the cross-submission hash rule and the conflict flag.
3. Everything else in this entry can wait behind entry 35 section 6 and entry 36.

---

## 2026-09-15, entry 36: the fixtures are regenerated at full double precision, and my defect is closed

**Status: actioned 2026-09-15; `DFdistr`, excepted here, was closed by entry 38.** Verified on all ten files. The nine datasets have 0 keys added or removed, and no stored number moved by more than 5.53e-16 relative. CSV and JSON agree bit for bit except three negative zeros in `DFlandy01`. The reconstruction is removed, and all 67 statistics tests pass from the fixtures alone. The reconstruction had matched the stored values on 3,775 of 3,978 coordinates. The other 203 are `DFcm`, off by at most 3.6e-15, because its centimetre aims are not six-decimal numbers, so the stored values were right. `STATISTICS.md` section 15.4 item 15 is amended. **Not committed:** `shotGroups_DFdistr.json` stores all 8,850 table values as strings, because `sg_distr.R` formats the columns for the CSV before building the JSON from the same frame. That script and both `DFdistr` files stay uncommitted for you to fix. Reported in `docs/PHASE1-RESULTS.md` "Entry 36". Files are already on disk, unstaged. Your job is to verify and commit, not to regenerate. Lower priority than entry 35 section 6's two remaining items, and it should be its own commit rather than folded into them.

### 1. What was wrong

`sg_dump.R` and `sg_distr.R` wrote through R's defaults: 15 significant digits in the CSV and `digits = 15` in the JSON. For a comparison at 1e-12 that is invisible. For a **rank** statistic it is not: dropping the last bits changes which values are exactly equal, which changes which observations tie, which moves the statistic in the fourth decimal place.

**That is question 14 in one sentence, and it cost you most of a day.** You chased a 1.2e-4 to 4.9e-4 disagreement through tie rules, median definitions, coordinate frames and two implementations, and the cause was the storage format of the fixture. My file, my defect.

### 2. What is fixed

Both scripts now write **17 significant digits**, which is the round-trip precision of an IEEE 754 double: every double formatted `%.17g` and read back yields the identical double. The stored value is now the value.

The two writers needed different fixes and both are in place:

- **CSV**: values formatted with `sprintf("%.17g", v)`, with `NA`, `NaN`, `Inf` and `-Inf` written as those literals, and `quote = FALSE` so the column stays unquoted as before.
- **JSON**: `digits = I(17)`. **Not `digits = NA`**, which the documentation describes as maximum precision but which on jsonlite 2.0.0 still emits 15. I tried `NA` first and caught it because the CSV and JSON then disagreed. The `I()` wrapper means significant digits rather than decimal places, and it is the only setting that works.

### 3. What changed in the data, measured rather than assumed

I diffed the new files against the committed ones across three datasets, 21,598 keys:

| | |
|---|---|
| Keys missing | **0** |
| Keys added | **0** |
| Values whose text changed | 17,000 |
| Values that changed by more than 1e-14 relative | **0** |

**Every difference is digits appearing, not a value moving.** Nothing you have validated has shifted. I also verified that CSV and JSON now agree bit for bit on all 580 numeric values of `DF300BLK`, which is the check that caught the `digits = NA` problem.

### 4. What you need to do

1. **Run the harness.** Every comparison should still pass. If one now fails, that is interesting rather than alarming: it would mean a value your implementation matched against a rounded fixture and does not match against the true one, which is a real defect the old fixtures were hiding.
2. **The Fligner keys are the ones to look at.** You reconstructed those values by hand to work around the precision loss. That workaround should now be unnecessary: the four keys should match straight from the fixture. **Remove the reconstruction rather than leaving it in place**, and confirm the values it produced agree with the ones now stored. If they disagree, tell me, because then one of us is wrong and it matters which.
3. **Update `STATISTICS.md` section 15.4.** It currently records 15-digit precision as a property of the fixtures. That is no longer true, so the entry should say what it was, when it changed, and that question 14's four keys were its only known casualty. Do not delete the note; a defect that was found and fixed is worth more in the record than one that was quietly removed.
4. **Commit as its own change**, and say in the message that no value moved beyond 1e-14.

### 5. Files

Nine datasets plus `DFdistr`, CSV and JSON, already written to `test/fixtures/shotgroups/`. Both scripts updated in `tools/shotgroups/`. Roughly 14 MB, slightly larger than before because the digits are really there now.

---

## 2026-09-15, entry 35: the four decisions, and what the sighter bug proves

**Status: actioned 2026-09-15 for sections 1, 4 and 5. Section 2's README warning and section 3 wait on Alan, because this session's permission check refused both. Section 6 item 3 is actioned: `grouplab analyze` needs no `--target` and the marking screen asks for a definition only when the sheet's codes cannot give one. Section 6 item 2 is measured: Windows reproduces the Phase 0 gate record byte for byte. Linux reproduces every printed table, and its records differ only below the precision any table reports. macOS reproduces the paper and photograph gate tables, and differs in measurements 1 and 2 in the third or fourth significant figure. Neither is byte-identical, so whether the differences count as explained is planning's decision; see `docs/PHASE1-RESULTS.md` "Entry 35 section 6".** Section 1: `CameraOriginal` holds a file with a screenshot or messaging-app name, or no camera make, in both `intake` and `publish-owner`. It flags none of the 26 published owner photographs and none of the donated files. The two photographs are recorded as held permanently at `grouplab-testdata` commit `c80055c`. Section 3: the bundle was verified, and the pack measured at 159.74 MiB, before the refused deletion; nothing was deleted. Section 4: `.gitignore`. Section 5: `CONTRIBUTING.md`. Reported in `docs/PHASE1-RESULTS.md` "Entry 35". Answers your four questions from the entry 33, 32 and 34 report. Section 1 is a decision I am making rather than passing on. Sections 3 and 4 are small and should be done in the next commit.

### 1. Do not publish the two held photographs, and the authorship question does not need answering

`Screenshot_20231029-170033.png` and `signal-2023-07-25-20-37-40-354-1.jpg`. You asked whether Alan took them. **The filenames answer a more useful question first: neither is a camera original.**

One is a screenshot. The other came through Signal, which re-encodes and strips metadata on send. Whatever the underlying photograph was, what we hold is a second-generation copy with no lens information, no orientation tag and recompressed pixels.

**So they fail the corpus's purpose regardless of who took them.** The donated set exists to give the lens and surface work frames whose camera geometry is known, and these two cannot contribute to that. Publishing them would add two files of unknown provenance and no analytical value, under a licence that cannot be revoked.

**Hold them permanently, and record why in the data repository's notes rather than leaving them looking like an open question.** If Alan later confirms he took the originals and still has them, the originals are the thing to publish, not these.

**Generalise it into the intake rule:** a file that is not a camera original is held by default. `Screenshot`, `signal-`, `IMG-\d{8}-WA\d+` and similar are cheap signals, and a missing `Make` tag is a stronger one. That is the same test the Discord post asks contributors to apply, so the tool should apply it too rather than relying on people reading.

### 2. The removal policy as written is right

Saying photographs are removed from the current contents promptly, and that removal from history is decided case by case, is honest and I would not strengthen it. **Do not promise a history rewrite**, because it invalidates every clone anybody has taken and we have just spent a day learning what that costs.

One addition: say in the README that the data repository's history may be rewritten for a removal request, so that anyone building on it knows a rewrite is possible rather than being surprised by one. A warning costs nothing and removes the only reason not to do a rewrite when one is genuinely warranted.

### 3. Delete `refs/original` and garbage collect, now

Those five backup refs are the last copy of the unscrubbed history inside a working repository with a push remote configured. They are one mistaken `--mirror` from publishing the coordinates, and that is a bad thing to leave lying around indefinitely.

**The backup already exists and is verified**: `C:\Dev\grouplab-backup-2026-09-15.bundle`, 158.95 MiB, `git bundle verify` clean, containing all sixteen refs including the originals. That is the copy to keep.

Delete each `refs/original/*` ref, then expire the reflog and garbage collect so the unreachable objects actually leave the pack rather than merely becoming unreferenced. **Report the pack size before and after**, since that is the evidence the objects are gone.

**One thing for Alan rather than you:** that bundle contains his coordinates. It should not sit in a folder that syncs to a cloud drive.

### 4. `scans/mounted/` should be ignored as well as moved

Alan moves the originals out, which is his to do. **Add `scans/mounted/` to `.gitignore` in the same commit as the other work**, so that the folder cannot be committed by accident if it reappears. The `excluded/` lesson from this morning applies exactly: a location that is meant to stay out of the repository should be enforced by a rule, not by everybody remembering.

### 5. What the sighter bug proves, recorded because it will be forgotten

`grouplab analyze` found two bugs on its first run. One crashed hole detection on every Phase 0 sheet. The other **counted shots on sighter bulls as part of the group, and the marking screen had it too.**

That second one is the important one. It does not crash, it does not fail a test, and it produces a plausible number that is wrong. It survived 709 passing tests, a statistics engine validated against 45,476 reference keys, and a detector measured on real scans, because **every stage was correct and the composition was not.** Nobody had ever asked the whole path a question, so nobody had ever seen the wrong answer.

Put a line in `DESIGN.md` section 21 or `CONTRIBUTING.md` to the effect that a stage passing its own tests is not evidence the pipeline is right, and that the synthetic end-to-end test is the one that speaks for the product. It is the most valuable thing learned this week and it is the kind of thing that gets rediscovered expensively.

### 6. Next, in order

1. **Sections 3 and 4 above**, plus section 1's intake rule. Small, and section 3 removes a live risk.
2. **Entry 32 section 3: the Phase 0 gate record reproduced on Linux and macOS.** CI proves the tests pass there, which is not the same claim. Compare the gate record byte for byte against the Windows run and report whether it is identical or explain the difference. Until that is done, neither platform is offered as a build.
3. **`--target` should not stay required.** The whole point of the printed codes is that a sheet describes itself. Reading the identifier or the GLTD-B payload off the image and selecting the definition automatically is the difference between a tool and a demonstration, and it needs no new data.
4. Then wait for the weekend's frames.

---

## 2026-09-15, entry 34: grouplab-testdata exists, and here is what goes in it

**Status: actioned 2026-09-15.** `grouplab-testdata` commit `1544f1d` holds the README, `CONTRIBUTORS.md`, the first donated submission under `donated/2026-09-14_1a8f39ad` (a provenance record only, because triage held all three files), and 26 of the owner's photographs under `owner/` through the new `grouplab publish-owner`. Two are held until Alan confirms taking them: a screenshot and a Signal download. The data test covers everything section 5 lists, and the URL and pinned commit are in the README and `CONTRIBUTING.md`. The originals are still in `scans/mounted/` here, untracked, for Alan to move. Reported in `docs/PHASE1-RESULTS.md` "Entry 34". Unblocks question 13 section 1 and entry 23 section 5. Lower priority than entry 33 section 1, the end-to-end command, which still comes first.

`https://github.com/oRAirwolf/grouplab-testdata` is created: public, GPL-3.0, one commit containing the licence, default branch `main`, no README. **GPL-3.0 is not a choice here and must not be changed**, because it is the licence named in the consent text every contributor agreed to.

### 1. The data repository's README is the important file, not an afterthought

It is the only document a contributor or a researcher will read, and it has to answer, without them having to ask:

- **What this is**: photographs of shot targets, donated, for developing and testing GroupLab.
- **What was done to them**: location metadata removed, pixels untouched, and the hash of both the original and the published file recorded so either can be checked.
- **What was agreed**: the consent text, quoted verbatim, with its version. Not a summary.
- **What is not here**: submissions whose contributor ticked the do-not-publish box. State plainly that such submissions exist, are used for testing only, and are never published. A reader should not have to infer that from silence.
- **How to cite it**, and how a contributor asks for their photographs to be removed. Somebody will, eventually, and an answer written now is calmer than one written then.

### 2. Layout, and one thing that does not fit the submission shape

**One directory per submission**, named as the upload page names it, `YYYY-MM-DD_<id>`, holding the images and a provenance record. That keeps the published tree aligned with what arrives, so a question about any file has one place to look.

**The provenance record carries**: submission id, submitted timestamp, consent version and the consent text verbatim, the six answers as given including empty ones, the original filename, the hash as uploaded and the hash as published, and what the scrubber removed. `grouplab intake` already writes most of this; make sure the consent text itself is in there and not just its version.

**`scans/mounted/` does not fit that shape and should not be forced into it.** Those 23 photographs are Alan's own, taken before the upload page existed, with no submission id and no consent record because none was needed. Give them their own directory, `owner/`, with a provenance record that says what they are, who took them, and that they are published by the copyright holder directly. Inventing a fake submission record for them would be worse than having two shapes.

They still go through the scrubber before they land there, per entry 23 section 5, and Alan keeps the originals outside both repositories.

### 3. Credit the people who asked to be credited

`meta.json` has a `credit_name` field and some contributors will fill it in. **Maintain a `CONTRIBUTORS.md` in the data repository listing those who gave a name**, and say in the README that a name is included only when the contributor supplied one. People who donate their work should be named if they wanted to be, and the field is pointless if nothing reads it.

### 4. Plain git, no LFS, and say the size out loud

Images are added once and never modified, so there is no delta churn and plain git handles this well. LFS would add a requirement on every contributor and a quota question, for a corpus that does not change. **State the current and expected size in the README** so nobody clones half a gigabyte by surprise.

### 5. Wiring it to the code repository

- **Record the URL and a pinned commit** in `grouplab`, somewhere a reader will find it: the README's test-data section already describes this repository, so put it there and in `CONTRIBUTING.md`.
- **`PublicationTests` already looks for a checkout at `../grouplab-testdata` or the path in `GROUPLAB_TESTDATA`**, and does nothing when absent. Leave that behaviour: CI should not need half a gigabyte to run.
- **Add one test that runs when the checkout is present**: every published submission has a provenance record, no image carries a location, no do-not-publish submission is present, and every published file matches its recorded hash. That last one is what makes the hashes worth recording.

### 6. Order

Entry 33 section 1 first. Then this. Nothing here is blocked by the weekend, but the end-to-end command is worth more before Monday than a populated data repository is.

---

## 2026-09-15, entry 33: nothing has ever run end to end, and that is what to do before the weekend

**Status: actioned 2026-09-15, except section 6, which stays blocked as the entry says (entry 34 has since unblocked `grouplab-testdata`).** Section 1: `grouplab analyze` runs the whole path with the stage trace, and `EndToEndTests` gates it on synthetic truth: 28 of 28 shots within 0.15 in, worst centre 0.0047 in, 1.7 s. It found two integration faults, both fixed: an invalid definition crashed hole detection, and sighter shots were pooled into the group. Section 3: `ReadmeTests` and a three-platform workflow. Section 4: the runtime packages conditioned per platform. Section 5: `scripts/` and the paper protocol committed, the branch convention rewritten, and a gate status table at the top of `docs/PHASE1-RESULTS.md`. Reported in `docs/PHASE1-RESULTS.md` "Entry 33".

### 1. The pipeline has never been run as one thing

Registration works. The hole detector works. `ShotAssignment` exists. The statistics engine matches 45,476 reference keys. Every piece is green.

**Nothing joins them.** `src/GroupLab.Cli` has verbs for intake, scrubbing, the library and a dozen spikes, and no verb that takes a photograph of a GroupLab sheet and returns a group. The parts have only ever been exercised separately, by spikes that each build their own inputs.

That is the most dangerous state a project of this shape can be in, because every part reports success and the thing the parts exist for has never been attempted. The failures waiting there are interface failures: a coordinate frame that means something different on each side of a call, a unit assumed in one place and converted in another, an ordering that only matters once two stages are composed. None of them can be found by testing the stages.

**Build `grouplab analyze <image> [--target <definition>]`.** One command, the whole path: load, register, detect inside the registered sheet, assign each hole to its bull, pool the offsets into one group, compute the statistics, emit a report. It should run against the sheets already in `scans/`, which means **it needs no paper, no printer and no range trip.**

**Emit the `StageRecord` trace `DESIGN.md` section 19 already specifies**, so the console form exists before any analysis screen does. That is not extra work bolted on; it is how the spikes have been reporting all along, and it is what makes a wrong answer diagnosable rather than merely wrong.

**Then gate it on synthetic truth, which is exact.** `SyntheticSheet` can already render a sheet, so render one with shots placed at coordinates chosen by the test, run the whole command against the rendered image, and require every recovered shot within the conformance threshold of where it was placed. **That is the first test in this project that measures the thing GroupLab actually does**, rather than a stage of it. If the composed answer disagrees with the truth it was built from, no amount of green stages matters.

Report the outcome honestly, including how long it takes and where it is slow. A first end-to-end run that finds three integration bugs is a successful run.

### 2. Why this specifically, and why before the weekend

Alan shoots the Phase 1 paper protocol this weekend and comes back with mounted sheets, which are the frames the photograph gate has been waiting on since entry 17.

**If the end-to-end path does not exist when those frames arrive, they sit unanalysed while somebody writes it**, and the integration bugs get found while the interesting measurement waits. If it does exist, the frames go in on Monday and produce a number the same day.

There is also a smaller reason worth saying: the application currently cannot analyse a GroupLab sheet at all. It can mark a commercial target by hand, which is the fallback path. The primary path, the one the whole target format and fiducial design exists to serve, has no user-facing route. That gap is invisible from the test counts.

### 3. The README guard and three-platform CI

Entry 31 section 3 and entry 32 section 4 are one job, and the repository now exists to run them against.

- **`ReadmeTests`** per entry 31 section 3: every relative link resolves, every referenced image exists, stated counts match reality with the number between marker comments, the stated framework matches `Directory.Build.props`, and no em dash.
- **A GitHub Actions workflow** on push and pull request, a matrix over `windows-latest`, `ubuntu-latest` and `macos-latest`, running `dotnet build` and `dotnet test`. Windows required; the other two allowed to fail until entry 32 section 1 lands, then required too. Report test counts per platform, because building and agreeing on the numbers are different claims.

### 4. Entry 32 section 1, the one-file defect

The unconditional `OpenCvSharp4.runtime.win` reference. Three lines, and it is the difference between a public repository that a Linux developer can build and one that fails at restore. Do it before the CI matrix, so the matrix has a chance of going green.

### 5. Housekeeping, all small and all currently untracked or stale

- **`scripts/` has never been committed.** `Get-TargetSubmissions.ps1` pulls the donated submissions and is the only copy. It holds no secret: the key path is a parameter and the key lives outside the repository. Commit it.
- **`docs/PHASE1-PAPER-PROTOCOL.md` and its PDF are untracked.** Alan shoots that protocol this weekend. It should be in the repository before it is used, not after.
- **`CONTRIBUTING.md` states a branch convention that no longer matches reality.** `main` and `phase-1` are now identical and both are pushed by hand every time. Either say that `main` is the trunk and `phase-1` is retained until Phase 1 formally closes, or propose retiring `phase-1`. Do not leave a document describing a workflow nobody follows.
- **`docs/PHASE1-RESULTS.md` should say plainly which Phase 1 gates are met and which are not.** The mounted photograph gate is not met and cannot be until the weekend. A results document that does not distinguish "passed" from "not yet measured" is the kind of thing that later gets read as the former.

### 6. Blocked, so that it is clear what is not on this list

- **The mounted photograph gate**, and the backer measurement on a GroupLab sheet. Both need frames that do not exist yet.
- **`grouplab-testdata`.** The repository has to be created by Alan before anything can be wired to it. Once it exists, the work is a pinned commit and URL recorded here, `scans/mounted/` moved across scrubbed, and the publication test pointed at it.
- **Adjust-to-zero and the phone specification**, entry 21's remaining scope. Mine to write, not yours to start.

---

## 2026-09-15, entry 32: macOS and Linux, which is one defect today and one gate later

**Status: actioned 2026-09-15 for sections 1 and 4; sections 2 and 3 are the shape of later work, as the entry says.** Section 1: each OpenCvSharp runtime package is conditioned on its platform, with the ids checked on nuget.org. Section 4: the CI matrix is in `.github/workflows/ci.yml`. Its first run passed on all three platforms, with Core 714 and App 4 on each, so `WinExe` is confirmed harmless and Linux and macOS are now required. Section 3's gate, the Phase 0 gate record reproducing on macOS and Linux, is not yet done. Reported in `docs/PHASE1-RESULTS.md` "Entry 33". Alan has asked for macOS and Linux support on the list. Section 1 is a real defect that exists now. Sections 2 to 4 are the shape of the work, not a request to start it this week. Entry 31 section 3's README guard and CI come first, and section 4 here is part of the same CI job.

### 1. The repository cannot be built anywhere except Windows, today

`src/GroupLab.Cli/GroupLab.Cli.csproj` references `OpenCvSharp4.runtime.win` **unconditionally**. That package carries the Windows native OpenCV binaries and exists for no other platform, so `dotnet build` on macOS or Linux fails at restore. Nothing else in the tree is Windows-specific as far as I can see, which makes this a one-file problem rather than a port.

**Fix it now rather than when somebody asks**, because it is three lines and because it is currently a lie by omission: the README says the project is C# on .NET 10 with Avalonia, which reads as cross-platform, and it is not.

The shape is a conditioned `PackageReference` per runtime, something like a Windows condition on the existing one and sibling entries for the osx and linux runtime packages. **Check the exact package ids on nuget.org rather than taking mine**, because OpenCvSharp's runtime packages have been renamed more than once and some are pinned to specific distribution versions, which matters for what a Linux user can actually restore.

`src/GroupLab.App/GroupLab.App.csproj` also sets `OutputType` to `WinExe`. On .NET that is harmless off Windows, where it behaves as `Exe`, but confirm rather than assume.

### 2. What is genuinely easy, and why

Avalonia runs on macOS and Linux already and renders through Skia on all three, so the interface is not the problem. `GroupLab.Core` has no platform types and no OpenCV dependency by design, so the measurement code should need nothing. The realistic work is the imaging backend, the packaging, and the differences nobody predicts.

**Three of those worth naming in advance:**

- **Case sensitivity.** Linux filesystems are case-sensitive and Windows is not, so any path with the wrong case works on your machine and fails there. This is a benefit rather than a cost: a Linux build is the cheapest detector of a class of bug that is otherwise invisible until a contributor hits it.
- **Fonts.** The PDF renderer embeds what it needs, so printing should be unaffected, but the interface picks up system fonts and will look different. Not a correctness problem; worth knowing before somebody reports it as one.
- **Printing.** The print screen's "save a PDF" path is portable. Driving a printer with scaling disabled, which entry 25 section 2 made a hard requirement, is platform-specific and may simply not be possible on one of them. If so, say so in the dialog on that platform rather than silently printing at whatever scale the driver chooses.

### 3. Where it sits in the plan

**Not a phase.** `DESIGN.md` section 21 numbers phases by capability, and "runs on another desktop" is not a capability, it is a property that either holds continuously or rots. A phase would mean it is allowed to be broken until that phase arrives, which is how a project ends up with a three-week port.

**So: fix section 1 now, and from the moment CI exists, keep all three green on every push.** The cost of that is close to zero while the code is small and rises every month it is deferred.

**The one thing that does need a gate, later:** a build that compiles is not a build that measures. Before macOS or Linux is offered to anybody, **the Phase 0 gate record must reproduce on that platform**, and the comparison is byte-identical or the difference is explained. Floating point, image decoding and font rasterisation all vary by platform, and a quarter-thousandth disagreement in a bull centre is the kind of thing that would otherwise be discovered by a stranger with a scanner.

### 4. CI covers all three, and going public just made that free

Entry 31 section 3 holds a GitHub Actions workflow until the fresh repository exists. It does now, and it is public, so **the hosted runners for `ubuntu-latest`, `macos-latest` and `windows-latest` are free with no minute limit.** That changes the calculation: three-platform CI costs nothing but the yaml.

**When you write that workflow, make it a matrix over the three**, with Windows required and the other two allowed to fail at first so the build is not blocked before section 1 lands. Once they pass, make them required too. Report the test counts per platform, because "it built" and "it produced the same numbers" are different claims and only the second one matters.

### 5. There is a person waiting

Alan has a friend who wants the macOS build, so this is not hypothetical demand. **That is a reason to fix section 1 promptly and not a reason to promise a release.** A macOS build that compiles and has never had its measurements checked is worse than no macOS build, because the friend would trust the numbers.

Also worth knowing: distributing a macOS build that people can open without fighting Gatekeeper needs a paid Apple Developer account and notarisation, which is a separate decision Alan has parked. A build somebody compiles themselves needs none of that, and is the right first offer.

---

## 2026-09-15, entry 31: the gate is clear, the failing test deserves a better fix than deletion, and the README needs a guard

**Status: actioned 2026-09-15. Section 3 waited for the fresh repository, and was done under entry 33: `ReadmeTests` and the three-platform workflow.** Section 2: the real-photograph test now writes its own location into a copy of `main1.jpg` and passes. Section 4: the hash map and citations were already done, the allowlist removal, the section 15.4 wording and the results figures are in, and the worktree branch is deleted. Reported in `docs/PHASE1-RESULTS.md` "Entries 29 and 30". Nothing pushed. Section 1 unblocks the push. Section 3 is new work and waits until the fresh repository exists.

### 1. The gate, and the go-ahead

**Byte-identical `photos.json`, byte-identical console table, zero lines differing, and the post-rewrite run also matches the record committed before the scrub.** That is the claim entry 29 section 5 existed to test, tested properly, and it passes. Scrubbing the metadata moved no measurement. Thank you for running it as a comparison rather than a spot check.

I verified the sixteen photographs independently, from fresh copies, with a different EXIF library: zero GPS blocks, zero maker notes, dates, unique ids or software strings, and `DigitalZoomRatio` present on all sixteen. Two methods, same answer.

**So the rewrite is accepted.** What remains before Alan deletes and recreates the repository is the failing test, the hash map, and the three pending edits you listed.

### 2. `ARealPhonePhotographScrubsToIdenticalPixels`: do not delete the assertion, move it

Your diagnosis is right: line 173 asserts `main1.jpg` still carries an EXIF GPS block, and after the rewrite it does not. Your proposed fix is to drop that assumption. **I would not, because of what the test is for.**

That test exists to prove the scrubber removes a location from a **real camera JPEG**, with a real maker note, a real thumbnail and whatever else a phone writes, and leaves the pixels untouched. The synthetic phone image does not exercise that: a file we constructed contains only what we thought to put in it, which is exactly the assumption a real file is there to challenge. Deleting the assertion leaves the test running on a file with no location, where it can no longer fail for the reason it was written.

**Make the test build its own input instead.**

1. Copy a committed photograph to a temporary path.
2. **Write a GPS block into the copy**, with coordinates the test chooses, plus whatever else is worth proving gets removed.
3. Run the scrubber on it.
4. Assert the location is gone, the camera fields including `DigitalZoomRatio` survive, and the decoded pixels are identical to the original.

That keeps the real-file coverage, removes the dependency on a committed file carrying something we have just spent a day removing, and cannot rot the same way again. It also means the test still passes in the `grouplab-testdata` world, where no committed image will ever carry a location by policy.

**If writing a GPS block from C# is awkward with the library you have, say so and take your version**, with a comment saying what coverage was traded away and why. A worse test that is honest about being worse beats a silently weaker one.

### 3. Keep the README honest automatically, because it has already gone stale twice

Alan has asked for the GitHub front page to stay current without anybody remembering to update it. The README is now the project's public face, and it has already been wrong twice in two days: it claimed twenty built-in sheets when there are twenty-two, and it claimed the application and statistics were not built after both existed. Both were caught by a human reading it, which is the mechanism we are trying to replace.

**Do not try to generate the README.** Most of it is argument and judgement, and generated prose reads like it. Guard the parts that are facts.

**Add `ReadmeTests`, in the Core test project, asserting:**

1. **Every relative link resolves.** Each `[...](path)` pointing inside the repository names a file that exists. This would have caught nothing so far, which is luck rather than design.
2. **Every referenced image exists.** Each `![...](path)`. This one has already bitten: the README was committed referencing six screens that were not in the repository yet, and the page rendered with six broken images on `phase-1` for several hours.
3. **Stated counts match reality.** The number of built-in sheets the README states equals the count of `targets/*.gltd.json`. Put the number between marker comments so the test can find it without parsing prose, for example `<!--count:sheets-->22<!--/count-->`.
4. **The stated target framework matches `Directory.Build.props`.** The README says .NET 10; the build is the authority.
5. **No em dash appears anywhere in the file**, which is a project rule and is cheaper to enforce than to remember.

Failures should name the line and say what to change. A test that says "the README claims 20 sheets, `targets/` holds 22" is worth ten that say "assertion failed".

**Then add the CI workflow, once the fresh repository exists.** A GitHub Actions job on push and pull request, running `dotnet build` and `dotnet test` on Windows. That gives the README guard somewhere to run without anybody choosing to run it, and it gives the repository the badge a visitor looks for. **Do not add it before the push**, because a workflow file in a repository that is about to be deleted and recreated is a workflow that runs against a history that will not exist.

**One rule for the text the tests cannot check.** When a commit changes something the README states in prose, change the README in the same commit. The status section is the one that rots fastest, because it is the one a visitor reads first and the one nobody editing code thinks about.

### 4. Order, so nothing waits on the wrong thing

1. Fix the failing test per section 2, re-run Core, confirm 709 of 709.
2. Write `docs/REWRITE-HASH-MAP.md` and correct the eight citations, per entry 30 section 2. **This is still the most fragile item in the project**, because the map lives only in your scratchpad.
3. The three pending edits: the allowlist removal, the `STATISTICS.md` section 15.4 wording on fixture precision, and the results section figures.
4. Delete the `worktree-agent-a5825dfa6aad44e1d` branch.
5. Mark entries 29, 30 and 31 actioned. Commit locally. Report.
6. **Stop. Do not push.** Alan deletes and recreates the repository, and pushes.
7. After the push: `ReadmeTests`, then the CI workflow.

---

## 2026-09-15, entry 30: the rebase is aborted, the scrub is independently verified, and one map must leave your scratchpad before it is lost

**Status: actioned 2026-09-15.** Section 2: `docs/REWRITE-HASH-MAP.md` with all 42 changed ids, nine citations corrected, and the rule in `CONTRIBUTING.md`. Section 4: `docs/STATISTICS.md` section 15.4 item 15 states the fixtures' precision as a property of the fixtures, and the harness says how the aimed values are rebuilt. Section 5: the gate record is byte-identical before and after, and the suite passes with the allowlist removed. Section 6: the worktree branch is deleted, and `refs/original` is left for Alan not to push. Section 7: the refused commands are listed in `docs/PHASE1-RESULTS.md` "Entries 29 and 30". Answers your entry 29 step 5 report. Section 2 is the urgent one and should be done before anything else, including finishing step 5.

### 1. The rebase you found is gone, and you were right to stop

Alan ran `git rebase --abort`. `phase-1` is back at `4b5c9a4`, which is the rewritten history with the README diagram commit on top. `main` is at `95fe9b0`, also rewritten, carrying both README commits and the six concept screens. Nothing has been pushed or force pushed. `git status` reports the two branches diverged from their remotes by 42 and 41 commits, which is the expected shape of a rewritten local history against a stale remote.

**The pull was mine.** I told Alan to run it without first checking for `refs/original`, which is the thing that would have told me a rewrite had happened in that clone. Your read of the consequence was exactly right: continuing it would have replayed the scrubbed history onto the unscrubbed one and brought the coordinates back. Recording it here so the next person understands why the rule below exists.

**The standing rule until the fresh repository exists: no `git pull`, no `git push`, no `git fetch` that could fast-forward a local branch, from anybody, in that clone.** The remote is stale by design.

**One thing to check rather than assume.** Your uncommitted edit to `PublicationTests.cs` removing the sixteen-file allowlist did not survive into the post-abort working tree; Alan's `git status` afterwards showed no modified tracked files at all. Redo it rather than looking for it.

### 2. Get the hash map out of your scratchpad, now, into a tracked file

This is the most fragile thing in the project at this moment.

**The rewrite changed 40 commit hashes and 8 of them are cited in the documentation**, 6 in `NOTES-FROM-PLANNING.md` and 2 in `PHASE1-RESULTS.md`. That is an excellent catch and it is the half of entry 29 step 1 I did not think to ask for: I asked you to look for recorded file digests and you found none, correctly, but a cited commit id is the same failure wearing different clothes and the record is already wrong in eight places.

**The map lives in your scratchpad, which does not survive you.** If this session ends before it is written down, the correspondence between the old and new ids is unrecoverable, and eight citations in the permanent record become unresolvable references to commits that no longer exist in any repository.

**Do this first, before finishing step 5.**

1. **Write the full 40-entry map to `docs/REWRITE-HASH-MAP.md`** and commit it. Old id, new id, subject line, one row each. Include the date, the tool (`git filter-branch`), and one sentence on why the rewrite happened, so the file explains itself to somebody reading it in a year.
2. **Correct the 8 citations in place**, in the same commit or the next one, leaving the old id visible where the sentence needs it: `5f8dafd` (was `b9b117b` before the 2026-09-14 rewrite) reads better than a silent substitution, because a reader with an old clone or an old bundle needs to be able to find it.
3. **Then add a rule to `CONTRIBUTING.md`**: do not cite a bare commit id in a document unless it is worth maintaining through a rewrite. Prefer a document reference, a test name or a milestone label.

### 3. The scrub, verified independently

I checked all sixteen photographs in the working tree myself, from a fresh copy, reading the EXIF with a different library than yours.

**Zero GPS blocks. Zero maker notes, dates, unique ids or software strings.** What survives on every one of the sixteen is exactly: `Make`, `Model`, `Orientation`, `FocalLength`, `FocalLengthIn35mmFilm`, `FNumber`, `ExposureTime`, `ISOSpeedRatings`, `DigitalZoomRatio`, and the pixel dimensions. `DigitalZoomRatio` is present on all sixteen, which is what entry 29 section 2 required and what my Python whitelist would have destroyed.

That corroborates your step 3 from a second direction. **It does not replace the rest of step 5**, which is the part that proves the measurement did not move.

### 4. The Fligner-Killeen answer is a finding about my tooling, not about R

Your diagnosis is right and it is worse than the four keys it surfaced in. `sg_dump.R` writes its values through R's default 15 significant digits, in both the CSV and the JSON. For a comparison at 1e-12 that is invisible. For a statistic whose value depends on the ordering of nearly-equal numbers, the dropped bits change which values tie, and the statistic moves in the fourth decimal place. That is exactly the 1.2e-4 to 4.9e-4 relative gap question 14 recorded.

**So the fixtures are lossy, and every future tie-sensitive key will hit the same wall.** Three things follow:

1. **Record it in `STATISTICS.md` section 15.4** as a property of the fixtures rather than a difference between implementations: values are stored at 15 significant digits, which is insufficient to reproduce rank-based statistics with near-ties, and the four Fligner keys are the worked example.
2. **I will regenerate the fixtures at 17 significant digits**, which is the round-trip precision of a double, so the stored value is the value. That is my file to fix.
3. **Not yet.** A 14 MB fixture change in the middle of a history rewrite is the worst possible timing. It waits until the fresh repository exists and the rewrite is behind us.

Your rebuilt values and the 45,476 keys matching with nothing pending is the right outcome in the meantime. Note in the harness how those values were reconstructed, so the next regeneration can be checked against it.

### 5. Finish step 5, in this order

1. The hash map, per section 2.
2. The post-rewrite Phase 0 gate record. **This is the one that matters most**, because it is the claim that scrubbing changed no measurement. Before and after must agree exactly, not approximately.
3. The full suite, with the allowlist removed from `PublicationTests`, and the counts.
4. A short report: gate before and after, Core and App counts, and the `PublicationTests` result.

**Then stop again.** The deletion of the GitHub repository and the push of the rewritten history remain Alan's, and he has not done either.

### 6. Two things for the fresh push, so they are not discovered afterwards

- **`worktree-agent-a5825dfa6aad44e1d` is a live local branch.** It is a leftover from one of your worktrees and it appears in the bundle alongside the real branches. Delete it before the push rather than publishing it.
- **`refs/original/*` must not be pushed.** They are `git filter-branch`'s backups and they point at the unscrubbed history. An ordinary `git push origin main phase-1` will not carry them; `git push --mirror` or `git push --all` with a stale refspec could. They are kept locally, and in Alan's bundle, until the fresh repository is verified.

### 7. The permission refusals

You said the classifier refused `git ls-remote` and two other read-only checks. Tell Alan exactly which commands, in one line each, and he can allow them. Read-only git queries are worth having available, and right now the inability to inspect the remote is a real handicap on the one task where the remote's state matters.

---

## 2026-09-14, entry 29: Alan has approved scrubbing the coordinates out of history, and one scrubber is already out of date

**Status: actioned 2026-09-15, steps 1 to 5.** No recorded digest; the bundle verified; all sixteen photographs scrubbed to identical pixels; the local history rewritten; the gate record, the suite and `PublicationTests` verified. Steps 6 onward, the push and the repository deletion, are Alan's and have not been run. Reported in `docs/PHASE1-RESULTS.md` "Entries 29 and 30". Answers question 13 section 2. **Read section 4 before running anything: this entry stops short of the irreversible steps on purpose.**

### 1. The decision

**Option (a). Replace the sixteen files in history with scrubbed copies, before the repository goes public.** Alan has approved it. Your reasoning was right on every point, including that the rewrite and the push are not yours to run.

### 2. A catch that has to be handled first, or the rewrite destroys something we just decided we need

`tools/scan_analysis/scrub_exif.py`, which entry 23 section 5 told you to commit and which the planning record has been treating as the scrubber, **whitelists only these tags:** Make, Model, Orientation, FocalLength, FocalLengthIn35mmFilm, FNumber, ExposureTime, ISO, PixelXDimension and PixelYDimension.

**`DigitalZoomRatio` is not on that list.** Entry 27 section 2 established that it has to join the lens-fit grouping key, because `FocalLengthIn35mmFilm` is not updated for digital zoom on every device. So the Python scrubber, run as it stands, destroys the tag the grouping now depends on. Any image already scrubbed with it has lost that tag and cannot get it back.

**Three things follow.**

1. **The C# `ImageScrubber` is the definition from now on**, since you have already given it `DigitalZoomRatio`. Use it for the history rewrite, not the Python script.
2. **Align or retire `scrub_exif.py`.** Two scrubbers with different whitelists is a trap that will be sprung by whoever reaches for the wrong one. My preference is to keep it, add `DigitalZoomRatio`, and put a line at the top saying the C# implementation is authoritative and this one exists for ad hoc use. If you would rather delete it, say so and I will agree.
3. **Check whether anything has already been scrubbed with the Python version and lost the tag.** `scans/mounted/` is the candidate. If it has, the originals are outside the repository on Alan's machine and can be rescrubbed; flag it rather than quietly accepting the loss.

### 3. Scrub all sixteen, not the thirteen with coordinates

`main_flat1` to `main_flat3` carry an empty GPS block. Scrub them too.

**The reason is the test, not the privacy.** If three files keep a GPS block, `PublicationTests` has to carry a permanent allowlist of three names, and an allowlist is a thing that goes stale and that somebody eventually adds a fourth name to. **After the rewrite the list should be empty and the rule should be absolute: no committed image carries a GPS block of any kind.** A rule with no exceptions cannot rot.

### 4. The order of operations, and where to stop

Steps 1 to 5 are yours. **Step 6 onwards is Alan's, and you must not run any of it.**

1. **Find every recorded hash first, and report before touching anything.** Search the whole repository, documentation, tests, fixtures and code, for any recorded SHA-256 or other digest of the sixteen files. The scrub changes their bytes, so any recorded hash becomes wrong the moment the rewrite lands, and it has to change in the same rewrite or the record silently lies. **If you find any, stop and report them rather than proceeding.** This is the step most likely to turn a clean rewrite into a mess discovered a week later.

2. **Make the bundle.** `git bundle create ../grouplab-prerewrite-2026-09-14.bundle --all` from the repository root, so it lands **outside** the working tree, then `git bundle verify` it and report the result. It is the only way back if the rewrite goes wrong, so it is worth the thirty seconds to confirm it is readable rather than assuming.

3. **Produce the scrubbed copies and prove the pixels are untouched, on all sixteen.** A test already shows `main1.jpg` decodes identically. **Extend that to every one of the sixteen**, comparing decoded pixel data rather than file size, and report the count. One file proves the method; sixteen prove the job.

4. **Run the rewrite**, replacing those paths' contents in every commit that contains them.

5. **Verify, and report the numbers.**
   - `PublicationTests` finds zero committed images with a GPS block, with an empty allowlist.
   - The full suite passes: Core and App, with counts.
   - The Phase 0 gate records still reproduce from the rewritten files, because that is the claim entry 11 made about frozen fixtures and this is the first thing that could break it.
   - Report the pack size before and after.

6. **Stop there. Do not push, do not force push, do not delete any remote.** Report that steps 1 to 5 are done and what they found.

**Why the hard stop.** A force push leaves the old objects reachable by hash on GitHub for an indefinite period, so it does not actually remove the coordinates from the remote. The clean route is for Alan to delete the private repository on GitHub and push the rewritten history to a fresh one. Deleting a repository is his to do and cannot be undone, so it happens with him at the keyboard, after he has read your report from step 5.

### 5. What does not change

The scrub touches metadata only, so every measurement, every gate result and every table in `PHASE0-RESULTS.md` stands unchanged. Nothing in the planning record needs revisiting because of this. If step 3 finds a file whose pixels do change, that is a defect in the scrubber and the rewrite stops until it is fixed.

---

## 2026-09-14, entry 28: questions 12, 13 and 14 answered, and every field name the intake tool guessed is wrong

**Status: actioned 2026-09-15.** Section 1: `grouplab intake` reads `meta.json` schema 1 exactly as recorded, with no sentinel file, the consent text verbatim and `original_name` kept but never a path. Section 2: question 12 answered and the extreme spread coverage table in `docs/STATISTICS.md` section 15.4 item 13. Section 3: the regenerated fixtures committed, and the four Fligner-Killeen keys and every probe key compared; the difference was the fixtures' 15-digit JSON, section 15.4 item 15. Section 4: the README's "Test data" and `PublicationTests` read a `grouplab-testdata` checkout, which Alan has yet to create. Section 5: section 15.4 item 14. Reported in `docs/PHASE1-RESULTS.md` "Entry 28". Section 1 is the urgent one: it stops the intake tool refusing every real submission. Section 4 needs Alan and is not mine or yours to decide.

Entries 22 to 27 are all actioned and the work behind them is good. Question 12's correction of my own entry 24 is right and I have taken it. What follows answers all three open questions, and adds one finding that came out of answering question 14.

### 1. Question 13 section 3: the real `meta.json`, which does not match a single assumed name

You asked me to confirm or correct the fields, because no document specifies them. **Every one of them is different.** A real submission is now on Alan's disk, pulled and hash-verified, and this is its `meta.json` in full, field for field:

```json
{
    "schema_version": 1,
    "submission_id": "1a8f39ad",
    "submitted_utc": "2026-09-14T20:41:55Z",
    "exclude_from_public_dataset": false,
    "consent": {
        "agreed": true,
        "version": "consent_v1",
        "agreed_at_utc": "2026-09-14T20:41:55Z",
        "text": "I took these photos, or I have permission to share them. ..."
    },
    "answers": {
        "target_backing": "", "attachment_method": "", "shot_distance": "",
        "caliber": "", "notes": "", "credit_name": ""
    },
    "user_agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ... OPR/135.0.0.0",
    "files": [
        {
            "index": 1,
            "stored_name": "001_20180623_104930.jpg",
            "original_name": "20180623_104930.jpg",
            "bytes": 3043798,
            "sniffed_type": "image/jpeg",
            "sha256": "870dac2119a5c701cc19980189e0f332aba11b4af310c2ee92fc2775ca638857"
        }
    ]
}
```

The corrections, against what question 13 section 3 lists:

| Assumed | Actual |
|---|---|
| `submissionId` | `submission_id` |
| `submittedAt` | `submitted_utc` |
| `consentVersion` | `consent.version`, nested |
| `doNotPublish` or `optOut` | `exclude_from_public_dataset` |
| `files` as object of name to hash, or array of `{name, sha256}` | array of objects keyed `stored_name`, with `sha256`, plus `index`, `original_name`, `bytes`, `sniffed_type` |
| `answers` | `answers`, correct, with the six keys above |

**The whole file is `snake_case`.** Read it that way rather than adding a camelCase fallback, and fail loudly on an unknown `schema_version` rather than guessing, since the page writes `schema_version: 1` precisely so a later change can be detected.

**Three more things the real file settles.**

1. **There is no `DO-NOT-PUBLISH` sentinel file.** The opt-out is `exclude_from_public_dataset`, a boolean in `meta.json`, and nothing else. Check that field; do not look for a file that the page does not write. Treat a missing field as unknown and refuse, not as false.
2. **`consent.agreed` and `consent.text` exist and should be required and recorded.** The provenance record should carry the consent text verbatim, because it is what the contributor actually agreed to and a later version will say something different. `consent.version` alone is a pointer to a document we would then have to keep.
3. **`original_name` is the contributor's own filename** and may carry a date, a camera prefix, or nothing. Keep it in the provenance record and never use it as a path.

**Also worth knowing before you test against it:** on the first real submission all six answers came back as empty strings, not absent keys. Empty is the normal case, so an empty answer must never be a refusal reason.

### 2. Question 12: A, five shots, and entry 24 got the coverage wrong

**Take option A.** Section 9.1 names the five-shot row for exactly this purpose, and printing its range in words from 5 to 19 shots covers what entry 24 asked for with "between that minimum and about twenty". B withholds the count most people actually fire, and your objection to C is the right one: hiding the interval hides the very thing that says how little five shots know.

**Entry 24 section 1 cited the wrong figure and you were right to say so.** I attributed the bootstrap BCa interval's 79.5 percent coverage to the panel, which does not use it. The panel's own intervals cover 92.5 percent at two shots and 94.3 at five. So my sentence "an interval whose real coverage is 80 percent must not be labelled 95" was true of the bootstrap and false of the thing Alan was looking at. What actually misled him was a headline printed to three decimals above an interval spanning a factor of twelve, which is exactly the width problem you identify, and withholding the headline fixes it.

**Two things I want kept from entry 24 regardless.** Below the minimum, still show the shot positions and the centre from the aim, because both are exact at any count. And label every interval with its real coverage rather than a bare 95, which you have already done.

**On extreme spread, diverge from shotGroups and say so.** Your measurement is that shotGroups' form covers 84.66 percent at two shots and 92.31 at five while claiming 95, and that `RangeStatistics.MeanInterval` covers 94.9 to 95.2 across the range. Question 11's principle applies unchanged: keep GroupLab correct where the reference is not, keep the harness comparing the reference's own form against the reference, and record the difference. Put the coverage table itself into `STATISTICS.md` section 15.4 alongside the entry, not only in `SmallGroupCoverageTests.cs`, because a number that lives only in a test is a number nobody reads.

### 3. Question 14: the input was never the difference, and the probe is now in the repository

**I ran it.** `tools/shotgroups/sg_dump.R` has a new `flignerProbe` block and all nine fixtures are regenerated. **The regeneration is purely additive: 68,672 pre-existing keys are byte identical and 4,414 keys are new.** I verified that by diffing every key and value against the committed files before writing them, so nothing you have already validated has moved.

New keys, on the five datasets with three or more series:

- `flignerProbe.FlignerX.input.<i>` and `.FlignerY.input.<i>`, the exact vectors;
- `.groupMedian.<series>`, `.scoreMean.<series>`, `.n.<series>`, `.scoreVar`, `.tiedValues`;
- `.recomputed`, the statistic rebuilt from those vectors, which matches `compareGroups.FlignerX.statistic` to 1e-13 on every dataset;
- `flignerProbe.rowsSortedBySeries`, for the reason in section 5.

**The answer: the input is identical to `shots.xPOA` and `shots.yPOA`, which you already had.** `compareGroups` calls `getXYmat` per series without passing `relPOA`, and that argument defaults to `TRUE`, so the test sees aimed coordinates. I confirmed the per-series construction is bit-identical to the whole-frame one on `DFinch`: `identical()` returns true and the maximum absolute difference is exactly zero.

**And `coin` is not the difference either.** `compareGroups` calls `coin::fligner_test` when `coin` is installed, which it was when these fixtures were generated, and base R's `fligner.test` otherwise. I measured both on all three datasets and they agree to 1e-13, so that branch does not matter here.

**So the disagreement is inside your implementation, and here is the formula to check against.** `stats:::fligner.test.default` is not the textbook form, and the difference is in what gets centred:

```
x  <- x - tapply(x, g, median)[g]        # centre by group median
a  <- qnorm((1 + rank(abs(x)) / (n + 1)) / 2)
a  <- a - mean(a)                        # centre the SCORES, before anything else
v  <- sum(a^2) / (n - 1)
stat <- sum( n_i * mean(a_i)^2 ) / v     # a_i are the centred scores
```

`rank` uses its default `ties.method = "average"`. I checked the textbook form, `sum(n_i (Abar_i - abar)^2) / var(a)`, and the group-sums form against this on `DFinch`: all three agree to 4e-15. **So the formula is not a 2e-3 effect and your tie hypothesis does not explain the gap.**

**What I swept and could not make produce your 10.073187875409229**, so you need not repeat it: grouping by `series`, `orgser` and `group`; `ties.method` of average, first, min and max; group centring by median, lower median, upper median, type-7 quantile and mean; and raw against aimed coordinates. Aimed gives 10.075167218103, raw gives 10.072777268233, and your value sits between them and matches neither. **That shape, between the two, is what a partially aimed vector looks like**, so I would look first at whether the aim is being applied to some rows and not others, or to x and not y, rather than at tie handling.

**If the probe shows your input matching and the statistic still differing**, then it is genuinely R's last bits and section 15.4 gains a thirteenth known difference. I do not expect that, given the size of the gap.

### 4. Question 13 section 1: option A, the separate data repository

**Take A, `grouplab-testdata`.** Your reasoning and entry 22's agree and I have nothing to add to it. Two conditions:

1. **It must be GPL-3.0.** Not a choice. The consent text the contributors actually agreed to says "published as part of GroupLab's public test data on GitHub under the GPL-3.0 license". Licensing the data anything else, including something more conventional for images, would publish it on terms nobody consented to.
2. **The pinned commit and URL live in this repository**, as you propose, and the publication test continues to no-op when the checkout is absent. Say so in the README of both repositories, because the failure mode is a contributor cloning one and wondering why tests skip.

`scans/mounted/` goes there, scrubbed, as the first contents. The Phase 0 and Phase 1 scans stay here because committed gate records read them by path.

### 5. A finding from writing the probe: `compareGroups` misaligns its own coordinates

`compareGroups` builds the coordinate columns with `split()` then `rbind()`, which returns rows in **factor level order**, and attaches them with `cbind()` to a frame still in its **original row order**. When those two orders differ, every coordinate is paired with the wrong series label.

**`DFlandy01` is such a dataset**, and it is in the fixture set. 519 of its 530 rows carry a coordinate from a different row than the label beside them.

**It changes no value here, and the reason is worth understanding rather than trusting.** Every series in `DFlandy01` is a contiguous block of exactly ten shots, so the misalignment permutes whole blocks and leaves the partition intact. The Fligner, Kruskal and MANOVA statistics are invariant under relabelling groups of equal size, so they come out identical: 44.0020356303247 either way. The per-series outputs are unaffected for a different reason, that they are computed from the correctly named `xyL` list rather than from the pasted columns. I checked three series centres against a correct alignment and they agree exactly.

**So no fixture key is wrong.** But it is one interleaved dataset away from being wrong, and a reimplementation that sorts internally would be doing the right thing and disagreeing. `flignerProbe.rowsSortedBySeries` now records it per dataset, and it is 0 only for `DFlandy01`. **Record it in `STATISTICS.md` section 15.4 as a known difference**, with the note that GroupLab should align coordinates to labels correctly and that the fixtures happen not to distinguish the two.

### 6. Question 13 section 2 is Alan's, not ours

The coordinates already in history are his, the rewrite is irreversible and the force push changes what every clone has. I have put the recommendation and its conditions to him directly rather than deciding it here. **Until he answers, commit no image**, which is what you are already doing.

---

## 2026-09-14, entry 27: the first donated submission, and a correction to how frames must be grouped for a joint lens fit

**Status: actioned 2026-09-15.** Amends entry 16. Section 1: intake triages every file by decoded markers and holds what it cannot use with the reason. Section 2: `DigitalZoomRatio` is read, is in the lens grouping key as unknown when absent, and the key is recorded with the fits. Section 3 is recorded per file. Reported in `docs/PHASE1-RESULTS.md` "Entries 22 and 27". Section 2 is the one with code consequences; sections 1 and 3 are intake findings that belong on the record before the donated set grows.

The public upload page at `pissinhot.com/targets` took its first submission from somebody outside this project on 2026-09-14. Three photographs, all Samsung, all camera originals. The pull script verified all three SHA-256 hashes against the `meta.json` written at upload, and every one matched, so the no-re-encoding requirement in the page specification is holding in practice and not merely on paper. That part worked.

### 1. None of the three photographs is usable, and that is the finding

One is a playing card held in a hand. One is a blank cardboard silhouette on stakes at distance, backlit against trees. One is a stack of silhouettes lying on grass, perforated by several hundred pellets.

No reference grid, no known scale in frame, no flat sheet, and in one case no mounting at all. The submitter answered none of the six optional questions: backing, attachment, distance, calibre, notes and credit were all empty strings.

**Do not treat this as a bad contributor.** Treat it as the measurement of what an open request produces. The uploaded set will contain a large fraction of material like this, and the consequence for the code is specific: **the intake path must assume most submissions are unusable and must say why, per file, before anything reaches the repository.** Entry 22 asked for intake to be a gate rather than a habit. This is the evidence for that, arriving on day one.

The cheapest useful triage is the registration and scale check the application already performs. A frame with no decodable markers and no detectable rectangular sheet boundary is not a candidate, and the tool can say so without a human looking at it. A human then looks only at what survives.

### 2. `FocalLengthIn35mmFilm` is not a sufficient grouping key, because digital zoom does not always update it

This is a correction to entry 16, and it matters for any joint fit across frames.

Entry 16 concluded that Alan's table frames were a cropped ultrawide, that both the physical focal length and the 35 mm equivalent tags were correct, and that frames must be grouped for a joint lens fit by physical focal length, f-number, 35 mm equivalent and image size. The first three of those held on the hardware in front of us. This submission breaks the assumption underneath them.

| File | Device | Physical focal | f-number | 35 mm equivalent | `DigitalZoomRatio` | Stored size |
|---|---|---|---|---|---|---|
| 001 | SM-G965U | 4.3 mm | f/1.5 | 26 mm | absent | 4032 x 1960 |
| 002 | Galaxy S24+ | 2.2 mm | f/2.2 | **13 mm** | **1.64** | 4000 x 1848 |
| 003 | SM-G996U | 5.4 mm | f/1.8 | 26 mm | 1.0 | 4032 x 1816 |

File 002 is the ultrawide lens with 1.64x digital zoom applied, and its 35 mm equivalent tag still reads 13 mm, which is the **unzoomed** figure. The effective field of view corresponds to roughly 21 mm. Alan's own frames reported a cropped equivalent of 23 mm on a 2.2 mm lens, meaning his device updates the tag and this one does not.

**So the tag cannot be trusted to describe the frame, and two frames that agree on all four of entry 16's key fields can still have different effective geometry.**

Two consequences:

1. **Add `DigitalZoomRatio` to the grouping key**, and treat its absence as unknown rather than as 1.0. File 001 omits the tag entirely.
2. **Treat a frame with `DigitalZoomRatio` greater than 1 as a separate group from an otherwise identical frame**, even when every other tag matches. Digital zoom crops and usually upscales, which changes both the effective focal length in pixels and the interpolation the pixels have been through.

**A third consequence worth stating plainly: a joint fit keyed on metadata is only as good as the metadata, and this is the second time in this project that a tag has meant something other than what it says.** Where a fit can be validated from image content rather than from tags, prefer that. Where it cannot, record the key that was used alongside the result, so a wrong grouping can be found later rather than being baked invisibly into a calibration.

### 3. The aspect ratios are a phone camera mode, not a crop by the user

All three are far wider than a sensor's native 4:3, at 2.06:1, 2.16:1 and 2.22:1. Those match the Samsung full-screen capture modes at 18.5:9, 19.5:9 and 20:9 for the three handsets involved. The `Software` tag on each file is a camera firmware build string rather than an editor name, and the stored dimensions match the EXIF dimensions, so these are camera output and not gallery-app edits.

That is good news for provenance and bad news for field of view: the mode crops the sensor top and bottom, so a whole mounted target is harder to get in frame and the submitter will tend to back further away. Expect donated phone photographs to be wider and lower resolution in the vertical axis than a specification written around 4:3 would assume. Nothing needs changing today; it is a fact to have when a detection threshold is tuned against donated frames.

**None of the three carried GPS.** That does not retire `scrub_exif.py`, since one submission is not a sample, and the scrub must still run at publication regardless.

---

## 2026-09-15, entry 26: the rotate control is a requirement, not a fallback, and it has one trap

**Status: actioned 2026-09-15.** Amends entry 24 section 4. Rotation is a view property of the marking state in the stored pixel frame, undoable and recorded in `grouplab-marking-2`, with the section's test in `tests/GroupLab.App.Tests/MarkingScreenTests.cs`; reported in `docs/PHASE1-RESULTS.md` M4.2.

Entry 24 identified the cause of Alan's sideways image correctly, an ignored EXIF Orientation tag, and then drew the wrong conclusion from it: that honouring the tag was the fix and a manual control was a secondary convenience. **Alan pushed back and he is right.** Build both, and treat the control as a first-class feature rather than a safety net.

### Why the control is required on its own terms

**A flatbed scan carries no orientation tag.** Scans are half this project's input path, a letter sheet goes into a scanner the wrong way round constantly, and there is no metadata to consult. On that input the tag is not merely unreliable, it does not exist.

Three more, any one of which would be enough:

- **A wrong tag is worse than a missing one**, because it rotates confidently in the wrong direction. Editors that rewrite pixels while leaving the tag, or the reverse, produce exactly this.
- **Images that have been through a chat app or a screenshot** usually arrive stripped, which the mounted collection already demonstrates.
- **A correct tag is not always what the user wants.** Someone may simply prefer the sheet a different way up, and that is a preference rather than a correction. Alan's point stands as he made it: people will be annoyed, and being annoyed by software that is technically right is still being annoyed.

So: honour the tag on load, because it makes the common case correct with no interaction, and provide rotate left and rotate right controls that work at any time, on any image, whether or not a tag was present and whether or not it was obeyed.

### The trap, which is the part that matters

**Rotating must never move a mark that is already placed.**

Somebody will load a scan, mark twelve impacts, notice it is sideways, and rotate. If rotation is implemented as anything other than a view transform, those twelve marks land in the wrong places and the user has to start again, which is worse than never offering the control.

So:

1. **Rotation is a property of the view, not of the image.** Never re-encode the pixels and never rewrite the file. The image on disk is the contributor's original and is not ours to modify.
2. **Shot and scale coordinates live in one canonical frame**, independent of what the view is doing. Rotating changes where a mark is drawn and not what it is.
3. **The canonical frame is the stored pixel frame**, which is the simplest thing that can be checked later against the file itself. Entry 24 section 4 already asks for the convention to be recorded in the export; this makes that a hard requirement rather than a nicety, because there are now two ways the displayed frame can differ from the stored one.
4. **Record the display rotation in the export** as well, so reopening a marking file shows the sheet the way the person left it.
5. **Undo covers rotation**, like every other action on that screen.

**A test worth having:** mark several shots, rotate, and assert every shot's page coordinates are unchanged and every drawn position has moved as expected. That is the regression that protects the feature from being quietly broken later.

### Not asking for

A free-angle rotation, and a mirror flip. Ninety degree steps in both directions cover every real case, and a sheet photographed in a mirror is not a case anybody has met. If one turns up, it is one line.

---

## 2026-09-15, entry 25: units, and the fact that the application cannot print a target

**Status: actioned 2026-09-15.** Section 1 in `2665c05` (was `f4f5263` before the 2026-09-14 rewrite), reported in `docs/PHASE1-RESULTS.md` M4.3; section 2 as the print screen, M4.4; adjust to zero still waits, per section 3. Two findings from Alan's second pass. The first is a small fix with a larger shape behind it. The second is a missing pillar rather than a missing button.

**Entry 24 section 7 is answered and needs nothing.** The scale entry does take a value, "Distance between the two taps, inches", and he used 1.5 against his grid. Good. The detection message is also doing its job: "Detection failed: 0 of 38 markers found; registration needs 4. Mark this image by hand with a reference length or rectangle" is the right thing to say to someone who loaded a commercial target, and the 38 confirms the corrected `GL-CF25-LTR` geometry is in the library.

### 1. Units, which is larger than the one input he noticed

He asked for a dropdown beside the scale length for centimetres or inches. He is right, and the same gap runs through everything the application prints: the panel reads `0.914 in` and `Centre from aim: 0.140 in right`, with no way to ask for anything else.

**Do not add a dropdown to one box. Give the application a unit setting and make every value obey it.**

Three axes, because they vary independently:

| Axis | Choices | Where it shows |
|---|---|---|
| **Linear** | inches, cm, mm | Scale entry, group size, mean radius, sigma, centre offset, export |
| **Angular** | MOA, mil, SMOA | Anywhere a figure is quoted at distance |
| **Distance** | yards, metres | The shot distance input, and it drives the angular conversion |

`docs/STATISTICS.md` section 13 is Units and section 12.5 holds the angular constants, so the arithmetic exists. This is exposing it, not building it.

**The rule that keeps this from becoming a bug farm: the unit setting changes display only, never storage.** Keep one canonical representation internally and convert at the edge. A marking file must mean the same thing whoever opens it, and the classic failure here is a saved file that reads differently depending on a setting on the machine that opens it.

**The export carries both.** Canonical values, plus the unit the user was working in, so a reader can reproduce what was on screen without guessing.

**Default from the system locale on first run, then remember the choice.** Alan is in the United States and wants inches, yards and MOA. A metric shooter wants cm, metres and mil, and this project already takes that seriously enough that two of its nine statistical fixtures exist solely to prove the conversion is right. It would be odd to prove it in the test suite and not offer it in the window.

**One detail worth getting right:** mil and MOA are not interchangeable and a turret is marked in one or the other. When adjust-to-zero is built, per entry 21 section 5, it must use the angular unit the user picked rather than a default, because a shooter dialling MOA clicks from a mil number puts the next group in the wrong place.

### 2. The application cannot print a target, and that is where a new user starts

This is the one worth stopping on. GroupLab's whole premise is that you print its target, shoot it, and photograph it. The library holds twenty-two definitions. A person who installs the application and wants to use it properly has to start by printing a sheet, and **there is no way to do that from the window.** Today the answer is a command line, or asking me to have you generate a PDF pack, which is fine for the two of us and absurd for anybody else.

The machinery all exists: `Rendering/`, `PdfWriter.cs`, and the CLI already produces the PDFs. **This is a screen that calls code that works, not new capability.**

**What the screen needs.**

1. **Pick a sheet.** The twenty-two built-ins listed by something a shooter recognises, not by identifier: what it is for, how many bulls, the sheet size, the distance it was designed around. `docs/TARGET-LIBRARY.md` has all of it.
2. **A preview**, so nobody prints eight pages to find out what they chose.
3. **The load block choice**, blank to write on later or filled in before shooting, which Alan asked for during the design and which `docs/TARGET-SCHEMA.md` makes a print-time decision. If it is filled in, the fields come from this screen.
4. **Print, or save a PDF.** Both, because some people want to print elsewhere.
5. **Scale, handled rather than warned about.** This is the part that matters most and the part a warning will not save. Every measurement in this project depends on the sheet being printed at exactly 100 percent, and a printer driver will silently shrink a page to its own margins given the chance. **Drive the print with scaling disabled rather than telling the user to check a box.** Where the platform will not allow that, say so in the dialog in plain words, and put it in the PDF's own margin as printed text so a sheet that came out wrong carries the evidence.
6. **A multi-page set**, since the roll and tiled targets are several pages that assemble, and printing them one at a time by hand is how they end up in the wrong order.

**It is also the natural home for the volunteer kit.** Entry 21 and the paper protocol both want a set of sheets plus a short instruction sheet in other people's hands. A print screen that can emit a pack is that, without a second mechanism.

**Where it sits in the plan.** Not urgent this week: Alan prints from the command line for the weekend and that is already arranged. But **it belongs before anyone outside this project is asked to use the application**, because it is the first thing they would do and the first thing they would fail at. Put it in `docs/PHASE1-RESULTS.md` or wherever the M4 scope lives as a named gap rather than leaving it implied by its absence.

### 3. Order

Entry 24 section 1, the statistics reported for two shots, still comes first. It is the one that can mislead somebody. Then the rest of entry 24. Then units, then printing.

**Do not build entry 21's adjust-to-zero until units land**, because it is the one feature where getting the unit wrong sends a shooter's next group somewhere else rather than merely displaying an odd number.

---

## 2026-09-15, entry 24: first human use of M4, and the app printed confident statistics for two shots

**Status: actioned 2026-09-15.** Sections 1 to 3 in `5f8dafd` (was `b9b117b` before the 2026-09-14 rewrite), with question 12 for the threshold; sections 4 and 7 with entry 26, and section 5; reported in `docs/PHASE1-RESULTS.md` M4.2.

Alan opened the marking screen, loaded `scans/mounted/20260329_183028.jpg`, set a scale, marked a point of aim and two impacts, and exported. His own notes were rotation, no calibre input, and that it needs refinement. The export and the screenshots carry four more findings he did not flag, and the first one is the serious one.

### 1. Two shots, and the panel reported a mean radius to three decimals

From his export:

```
shots: 2
meanRadius 0.914 in   (95% 0.476 to 5.744)
sigma      0.729 in   (95% 0.380 to 4.583)
```

**The interval spans a factor of twelve and the headline is printed as 0.914.** Nothing on screen says the number is meaningless. He read the panel, saw a figure with a confidence interval beside it, and reported that the app seemed to be working.

That is the failure this project exists to prevent, appearing on first contact. `docs/STATISTICS.md` section 6 already flags groups under ten shots as unreliable and the interface does not honour it. Entry 23 section 2 has the measured coverage: 79.5 percent at ten shots against a nominal 95, which means even ten is optimistic, and two is not a sample at all.

**What the panel must do.**

- **Below the useful minimum, do not print a headline figure.** Say what is missing, in the shooter's terms: something like "2 shots. At least 5 are needed for a group size worth quoting, and 10 before the interval means much." Show the shot positions and the centre offset, which are exact and useful at any count, and withhold the dispersion statistics rather than dressing them up.
- **Between that minimum and about twenty, print the figure with its coverage stated**, not a bare 95 percent. An interval whose real coverage is 80 percent must not be labelled 95.
- **Pick the thresholds from `STATISTICS.md` section 9**, which already models how well sigma is known from n shots, rather than from anybody's taste. If section 9 does not give a clean answer, raise it as a question rather than choosing a round number.

**This is the same finding as entry 23 section 2 arriving from the other direction.** That one was a coverage table in a report nobody runs. This one is a number on the screen that a user believed. Fix them together, and treat the interface as where it matters.

### 2. The statistics panel is clipping its own text

The screenshot shows `0.914 in  (95% 0.476 to 5` with the rest cut off at the panel edge. The headline is the one line guaranteed to overflow because it is the largest type. Let it wrap, or size the panel to its content.

### 3. `NaN` is being exported, as a quoted string

```
"aspectRatio": "NaN",
"angleDegrees": "NaN",
```

Those are degenerate at two shots, which is correct, but `"NaN"` is not a value. A consumer reading this file gets a string where it expects a number, and the quoting only exists because bare `NaN` is not legal JSON, which is the language telling you the same thing.

Emit `null` and add a sibling field saying why, for instance `"aspectRatioUnavailable": "needs at least 3 shots"`. Apply it to every statistic that can be undefined, and check the schema for others with the same problem before Alan meets one.

### 4. The rotation is an ignored EXIF tag, not a missing button

`20260329_183028.jpg` stores 4000 by 3000 pixels and carries **EXIF Orientation 6**, which means the pixels are landscape and a viewer is expected to rotate them 90 degrees clockwise. Every normal viewer does. The app does not, which is why it looked turned on its side.

**Honour the orientation tag on load.** That fixes it for nearly every phone photograph at once, rather than asking the user to correct each one by hand. Then add a manual rotate control as well, because some images carry no tag and a few carry a wrong one.

**One thing to get right while doing it.** The export records shot positions as `image.x` and `image.y` in the stored pixel frame. Once the app rotates on load, that frame changes, and every marking file saved before the change silently points at the wrong place. **Record the convention in the file**, and either migrate old files or refuse to load one whose convention is unknown. There are only a handful in existence today, which makes this the cheapest moment it will ever be to fix.

### 5. Calibre: what it is for, since Alan has now asked twice

It is entry 21 section 5 and I said I would specify it once he had used the screen. He has.

**Make it an optional property of the group**, entered once beside the shot distance, not per shot. Free text with a short pick list of common ones, because somebody will want a wildcat.

**Three things it does, in order of value.**

1. **Report extreme spread both ways.** GroupLab measures centre to centre, and shooters at a range measure outside edge to outside edge and subtract one bullet diameter to get the same thing. With the calibre known, print both and label them, so the number matches whatever the person is used to quoting. Without it, print centre to centre and say so. This is the whole reason Ballistic X asks for it.
2. **Size the tap snap radius.** Marking a hole is a tap that snaps to the hole under it. How far it should look is a function of hole size, and hole size tracks bullet diameter, which `docs/SCAN-MEASUREMENTS.md` section 3.5 measured across 343 holes. A .22 and a .338 should not use the same search radius.
3. **Flag a marked hole whose apparent size is wrong for the calibre**, once a scale is set. Two overlapping holes marked as one read as far too large, which is exactly the failure `PHASE1-RESULTS.md` M2.2 found reported silently 50 times out of 52. This is the cheapest available detector for it and it needs no new measurement.

**Do not gate anything on it.** No calibre means no edge-to-edge figure and a default snap radius, never a refusal to work.

### 6. What Alan saw that was right

Worth recording, because it is the part not to change. The scale honesty line appears and reads well: "From a single 1 in reference length, which assumes the photograph is square on and the sheet flat", in orange, above the numbers it qualifies. That is entry 21 section 4 working exactly as intended, and it is the difference between a number and a number you can trust.

The flyer line is also good and correctly reasoned, though at two shots it is noise like everything else in that panel.

### 7. One question I could not answer from the screenshots

**Can the reference length be anything other than one inch?** The export says "a single 1 in reference length" and the toolbar button shows no value. Most targets have no convenient one inch feature, and his has a 1.5 inch grid, so if the length is fixed at one inch then anybody measuring across a grid square is out by fifty percent and nothing tells them.

If it is fixed, make it an entry. If it is already an entry, the export should record the value used rather than the phrase, so a reader can check it.

---

## 2026-09-15, entry 23: question 11 answered, the fixtures are regenerated, and the surface model is not as dead as entry 17 left it

**Status: actioned 2026-09-15**, except committing `scans/mounted/`, which section 5 holds until entry 22 section 1 is answered, `docs/QUESTIONS-FOR-PLANNING.md` question 13. Section 1: the fixtures and `sg_dump.R` committed, the harness reads the point of aim, question 11 marked answered and recorded in `docs/STATISTICS.md` sections 15.2 and 15.4, four Fligner-Killeen keys pending as question 14. Section 2: the M3.1 amendment and `Bootstrap`. Section 3: the M1.11 amendment. Section 4: the detector runs inside the registered sheet, `docs/DETECTION-PIPELINE.md` before S5. Section 5: both scripts committed.

### 1. Question 11: A, A and A, and the first one is already done

Your reasoning is right in all three cases, and the common thread is the one that matters: **keep GroupLab exact where shotGroups is not, and keep the gate checking something true.** A gate that reproduces another implementation's root-finder tolerance is not measuring correctness, it is measuring agreement with a defect.

**1, the point of aim. Done rather than decided.** The fixtures are regenerated and committed, and `sg_dump.R` now emits `shots.xPOA` and `shots.yPOA` beside `shots.x` and `shots.y`. That was my error in the first version: `getXYmat(..., relPOA = FALSE)` does not carry the aim, so a fixture built from the matrix alone cannot reproduce anything `groupLocation`, `groupSpread` or `groupShape` computed from the frame. Your diagnosis was exactly right and the arithmetic confirms it:

| Dataset | Distinct aim points | Frame-based centre x | Matrix-based centre x |
|---|---|---|---|
| `DFinch` | **9** | -0.9195 | 6.1847 |
| `DFcm` | **9** | -2.3356 | 15.7091 |
| `DF300BLK` | 1, at the origin | -0.0004 | -0.0004 |

The 7.1 in gap on `DFinch` sits inside its aim range of 5.518 to 7.806, and `DF300BLK` agrees to four decimals because its aim is zero. That is the disagreement you cited, with the missing datum now supplied.

**Both coordinate forms are emitted rather than one**, because `xyTopLeft = TRUE` flips y and anyone deriving either from the other has a sign convention to get wrong, which is the kind of thing that costs a day. Row counts grew: 600, 520, 1857, 2553, 9464, 9464, 6563, 5108, 32543. Section 15.5 point 2 is now reachable.

**2, the CorrNormal CEP. Option A.** Gate the distribution through the hit probabilities, which already match to 1e-15, require GroupLab's own CEP to satisfy that distribution at 1e-12, and compare shotGroups' CEP at 1e-4 relative. Replicating its root finder would mean shipping its tolerance, and a CEP that misses its own probability by 3e-6 is a defect rather than a convention. Record it in section 15.4 with the measured misses so nobody re-derives it.

**3, the SMOA round trip. Option A.** Section 15.4, with the constant. The anchor holds for `getMOA` and fails only on the inverse, which is the definition of a one-directional bug.

**Your four handled findings are handled correctly**, and the MANOVA one is the sharpest. `sg_dump.R` taking `MANOVA[1, ]` gives R's intercept row, which tests whether the mean over all shots is the origin rather than section 8.2's test of the group centres. Reproducing that row for the gate and computing the real group test separately is right. **Note it in `STATISTICS.md` section 15.4 as a tenth known difference**, because the next person to read the fixture will assume row 1 is the group test, exactly as I did when I wrote the script.

**`DFcm` and `DFinch` are not the same data**, and that is a finding about the package rather than about us. The README now says so. Section 15.2 should stop calling them the same data and say what they are: the same shots, differently grouped, with one shot in a different series.

### 2. The bootstrap coverage is the most important thing in question 11, and it is filed as an aside

79.5, 89.1 and 92.7 percent actual coverage at 10, 25 and 50 shots, against a nominal 95. **At ten shots a "95 percent interval" is a 79.5 percent interval.**

That is not a footnote. It is the exact error this project exists to prevent. A shooter comparing two loads on ten-shot groups, shown an interval that claims 95 and delivers 80, will conclude one load beats the other when the data does not support it. `STATISTICS.md` section 6 flags groups under ten shots as unreliable, which does not cover this: the problem is at ten, twenty-five and fifty.

**Three things follow, and none is a research project.**

1. **Prefer a closed form wherever one exists.** Section 3.3 has a closed-form interval for sigma, and section 15.3 already gates it at 1e-12. The bootstrap should be the fallback for quantities with no closed form, not the default.
2. **Where the bootstrap is used, the interface must not print a bare "95 percent".** Either state the measured coverage at that sample size, or label the interval as approximate and optimistic at small n. A number that is wrong and confident is worse than one that is wide and honest.
3. **Add coverage to the gate.** Section 15.5 point 4 already requires 94.0 to 96.0 percent coverage for the known-truth synthetic test on 25-shot groups. Measure the bootstrap the same way and record the number rather than leaving it in a command nobody runs.

Put the table in `PHASE1-RESULTS.md` where it is, and raise a question if any of that changes what you have already built.

### 3. The photograph against its own scan: the surface model is not dead

This is the entry 19 and 20 measurement, and it changes the picture that entry 17 left.

**A flat-plane fit leaves 0.021 in RMS and the general developable surface brings it to 0.006 in.** On the nine pinned frames the same model took up almost nothing, which is what stopped the surface work. The difference between the two cases is the mounting: those frames were a sheet hanging from a single pin, free to twist, and this one was lying on a mat with a gentle sag. **A developable surface handles the gentle case and fails the twisted one**, which is exactly what your own synthetic sweep said when it broke at a quarter inch of twist.

So entry 17's conclusion stands as measured and its scope was wider than the evidence. The right statement now is that no developable surface fits a sheet twisting on a pin, and that the model does most of the work on a sheet deformed gently. **It does not pass the gate even here**, at 0.006 against 0.005, on a 25-bull constraint far coarser than 136 marker corners. But it is close on a case nobody had measured, where it was nowhere on the case that stopped it.

**This still does not settle the mounted question**, because that sheet was lying on a mat and no photograph in the collection is both a whole sheet and mounted. It does mean the mounted case deserves the measurement rather than being written off, and next weekend's session produces exactly the frames it needs.

Record this in `PHASE1-RESULTS.md` beside M1.11 as an amendment with its date, not as a replacement. M1.11 was right about what it measured.

### 4. Detection must run inside the sheet, and that is a cheap large win

**0 of 28 holes on the whole photograph, 26 of 28 cropped to the sheet, untuned.** The dark mat merges into one twelve inch blob that swallows everything.

The fix is architectural rather than a tuning parameter: **the detector runs inside the registered sheet boundary, never on the whole image.** Registration already knows where the sheet is, so the crop is free. Make it a property of the pipeline rather than a step a caller can forget, so that no future path can hand the detector a full frame by accident. `docs/DETECTION-PIPELINE.md` should say so in the stage that precedes S5.

**The 0.023 in median centre difference is the backer material, measured for the first time.** The photograph sees the dark mat through each hole where the scan sees the white scanner lid, and 0.023 in is nearly three times the 0.008 in noise floor. That is a real limit on the photograph path and it is not a defect: it is what a hole looks like against something dark. It also says the backer question in the planning record is not a preference, it is a term in the error budget. Next weekend's sheets are shot against your normal backer, so that number gets a second measurement on a GroupLab sheet.

### 5. The uncommitted files: commit them, with one exception

`scans/mounted/` and my two scripts in `tools/scan_analysis/` are mine to call.

**Commit both scripts.** `scrub_exif.py` is about to become load-bearing, per entry 22, and `straightness.py` is a measurement that failed its own control and is worth keeping as a record of an attempt rather than being silently dropped.

**Commit `scans/mounted/`**, but run every file through `scrub_exif.py` first and commit the scrubbed copies, not the originals. 22 of those 23 carry GPS. They are Alan's own photographs so there is no consent question, but the repository is going public and there is no reason for his range coordinates to be in it. Keep the originals outside the repository.

**That is 28 files and roughly 80 MB**, which is the size question of entry 22 arriving early. If your answer to entry 22 section 1 is a separate data repository, these belong in it and should wait. If it is Git LFS, configure it first. **Answer entry 22 section 1 before committing the images**, and commit the two scripts either way.

### 6. M4, and the thing to do next

Nobody has opened the window. That is Alan's next step and it is the first time the project has been something he can use rather than read about. Everything else waits on what he finds.

**Entry 21's remaining scope is mine**, and I will specify adjust-to-zero, the calibre input and the phone question once he has actually used what exists. Specifying a second round of interface before anyone has touched the first round is how you get features nobody wanted.

---

## 2026-09-15, entry 22: donated photographs are arriving, and the repository is not ready to receive them

**Status: actioned 2026-09-15.** Section 1 raised as `docs/QUESTIONS-FOR-PLANNING.md` question 13, recommending a separate data repository, and no image is committed until it is answered. Section 2 is `grouplab intake`, section 3 is `PublicationTests`, reported in `docs/PHASE1-RESULTS.md` "Entries 22 and 27". The committed-image check found GPS in 16 Phase 0 photographs, also question 13.

### 1. The size problem, which has to be decided before anything lands, not after

A public upload page at `pissinhot.com/targets` is built and about to be announced to roughly 400 people across two Discord servers. If even fifty of them submit three photographs each, that is on the order of **half a gigabyte to a gigabyte of binary files**.

**Git handles that badly and the repository cannot absorb it.** Every clone pulls every byte of every version forever, binaries do not delta-compress, and a photograph that is later scrubbed of GPS is a second full copy in history rather than a small diff. The current repository is a few tens of megabytes; this would make it one to two orders of magnitude larger and make a fresh clone a chore.

**Decide now, because the cost of deciding later is a second history rewrite.** This project has already done one, with `git filter-repo`, to purge another company's files. Doing it again over donated photographs would be worse, because by then the images will be other people's contributions rather than Alan's own files.

Three options, and I have not chosen for you because this is an infrastructure decision and you can see the repository:

- **A separate data repository**, say `grouplab-testdata`, referenced from the main one by URL and commit. The code repository stays small and clonable, and the data carries its own licence and provenance. My inclination, because the two have genuinely different lifecycles and the test data will keep growing while the code churns.
- **Git LFS on the main repository.** One repository, but it needs LFS configured before the first image lands, and it puts a dependency on every future contributor.
- **Keep a small curated subset in the repository** and the full set outside it. Cheapest, but somebody has to curate, and the whole value of a donated corpus is its breadth.

Raise this as a question with your recommendation once you have looked at what the repository actually is. It is genuinely yours to call.

### 2. The intake pipeline, which must be a gate and not a habit

Submissions arrive as a directory per submission holding the original files and a `meta.json` carrying the answers, the consent record, and a SHA-256 per file. Nothing may enter public test data except through a single tool that does all of this:

1. **Refuse any directory containing a `DO-NOT-PUBLISH` file.** The page writes that file when a contributor ticks the opt-out box, alongside a flag in `meta.json`. Honour the file, not just the flag, because a file is harder to miss.
2. **Scrub GPS.** `tools/scan_analysis/scrub_exif.py` already does it: it keeps `Make`, `Model`, `Orientation`, focal length, the 35 mm equivalent, f-number, exposure and ISO, and drops everything else including every GPS field, maker notes, serial numbers and dates. It is what found that 22 of Alan's own 23 photographs carried coordinates, so treat the unscrubbed state as the normal one.
3. **Record both checksums.** Scrubbing changes the bytes, so the SHA-256 in `meta.json` will not match the published file by design. Carry the received hash and the published hash side by side. That is what makes it provable later that the image in the repository is the image that was consented to, rather than something that drifted.
4. **Carry the provenance with the image.** Submission ID, the consent text version, the submission timestamp, and the answers. If anyone ever asks under what terms a photograph is published, the answer has to be in the repository and not in a server directory nobody kept.

### 3. Make it a test, because a step someone remembers is a step someone forgets

**A test must fail if any file under the public test data path carries a GPS tag**, or sits in a directory marked `DO-NOT-PUBLISH`, or lacks a provenance record. Not a documented procedure. A failing test.

The reasoning is the same one that made the `reference/` files a blocker: publishing is irreversible in a way that local mistakes are not, and this time the material belongs to other people who were given a specific promise about it. The consent text says GPS is removed before publication. A test is how that promise stops depending on anybody's memory.

### 4. What this does not change

M3 and M4 continue. None of this is urgent enough to interrupt them, and no image can arrive until the page is live and announced. But section 1 wants an answer before the first commit rather than after, and section 3 wants to exist before the first image, not before the first release.

---

## 2026-09-14, entry 21: a manual marking path, which is a second product and mostly already built

**Status: actioned 2026-09-14.** M4's first screen is the marking screen of section 3, built as both the manual path and the correction interface, with section 4's rectangle offered beside the length; reported in `docs/PHASE1-RESULTS.md` M4.1. Sections 5 and 6 are left for specification, as section 8 says.

### 1. What Alan asked for

A workflow like Ballistic X, on mobile in particular: photograph the group, establish a one inch reference, give the calibre, mark the aiming point, mark each impact by hand, get the statistics. He supplied his own exports from that application. They show numbered reticles on each impact, a separate marker on the point of aim, a group-centre dot and a mean-radius circle, and a statistics panel carrying group extreme spread in inches and MOA, bounding width and height, adjust-to-zero in MOA with its direction, elevation and windage offsets, mean radius, CEP, radial, vertical and horizontal standard deviations, and windage and elevation extremes.

### 2. This resolves entry 20's problem rather than adding to the pile

Entry 20 found that twelve of twenty-four of Alan's own photographs are close-ups of a single group with no sheet edge in frame, and that GroupLab as specified can use none of them, because registration needs a fiducial lattice that is not in the picture. It also found a whole target family, the fluorescent splatter targets, where a hole is a bright saturated halo and the neutral-darkness detector will find nothing.

**A manual marking path uses every one of those photographs.** It needs no fiducials, no registration, no surface model and no hole detection. It works on a close-up, on a splatter target, on a commercial bullseye, on a heavily compressed picture forwarded through a chat app, and on a sheet bowed on a board. The photographs Alan actually takes are exactly the input this path is for, and that is not a coincidence: it is what he has been using such an application for.

### 3. So GroupLab has two paths, and they meet in the middle

| | **Manual marking** | **Automatic** |
|---|---|---|
| Input | Any photograph of any target | A GroupLab sheet, whole, in frame |
| Scale from | The user, against something of known size | The printed fiducials |
| Impacts from | The user, tapping each one | Detection |
| Accuracy | The user's tap and the scale reference | Measured: 0.0033 in on a scan |
| Works today on | Everything in `scans/mounted/` | Nothing in `scans/mounted/` |
| Needs | M3, and a marking screen | M1, M2, M3, and the printed sheet |

The middle is the useful part and it already has a requirement: `DESIGN.md` section 13 mandates a manual assignment interface because real targets carry hand-drawn arrows no geometric rule recovers. **The marking screen and the correction screen are the same screen.** Build it once. Automatic detection then becomes a way of pre-filling marks the user can accept, move or delete, rather than a separate mode.

**The manual path is close to free.** It needs the statistics engine, which is M3 and being built now, plus a screen with a scale tool, a point-of-aim tool and an impact tool. It needs nothing from M1 or M2. It could therefore be usable before the automatic path is, which inverts the current plan's order of value.

### 4. Where GroupLab can be honestly better, for two extra taps

**A single one inch reference assumes the photograph is square on and the sheet is flat.** It is a uniform scale applied to the whole image. Off-axis, that is wrong and wrong by a varying amount across the frame, which is the same perspective problem this project has spent Phase 0 and half of Phase 1 on. A group measured near the far edge of an off-axis frame comes out smaller than one measured near the camera, and nothing in the output tells the user that happened.

**If the user marks a known rectangle instead of a known length, the perspective goes away exactly.** Four taps rather than two, and it yields a homography rather than a scale factor: the same mapping the automatic path fits from fiducials, from four user-supplied points instead. Most targets make this easy, because most carry a printed grid, so the rectangle is a grid square or a block of them with a known size. The splatter targets in Alan's collection have a one inch grid. The orange sight-in sheets have a finer one. The OnTarget sheet has a ruled box per bull.

The machinery exists. `Registration/PageMapping.cs` and the homography fitting already take four correspondences and produce exactly this. **Offer both: a length for speed, a rectangle for accuracy, and say which one a given result used.** A result that knows it came from a single scale on an off-axis frame can say so, which is more honest than a number that quietly absorbed the error.

**What a rectangle still cannot fix is a bowed sheet**, because a homography is planar. That is the open problem of entry 17 and it does not go away here. But it is strictly better than a single scale, and it costs two taps.

### 5. Two things this adds that `STATISTICS.md` does not cover

1. **Adjust to zero.** Converting the group centre's offset from the point of aim into scope adjustment. It needs the click value of the user's turret, a quarter MOA, a tenth of a mil and so on, and it needs a direction convention stated once and never got wrong. `STATISTICS.md` section 12 has the angular conversion constants and section 8.2 has group location; the missing pieces are the turret click value, the up-or-down convention, and the arithmetic between them. Small, useful, and the single most-used number in the exports Alan supplied.
2. **Calibre as an input.** Ballistic X asks for it, and it is what turns a marked impact centre into an edge-to-edge group measurement, because the conventional group size is measured outside edge to outside edge minus one bullet diameter. `docs/SCAN-MEASUREMENTS.md` section 3.5 already measured hole diameter against nominal calibre on 343 real holes, so the relationship is characterised. Decide and document which convention GroupLab reports, because centre-to-centre and edge-to-edge differ by exactly one calibre and shooters argue about it.

### 6. The mobile question, flagged and not decided

Alan said "especially the android and ios versions". The current plan is Avalonia, which does target both, but mobile is the less-travelled path for that toolkit and none of it has been tried. **The manual path is the natural thing to put on a phone**, because the input is a photograph the phone just took and the compute is trivial, where the automatic path wants a scanner or a careful full-sheet capture and real processing.

That is a scope decision with real cost and it is not being taken here. **M4 stays as the brief has it: a desktop shell.** Build the marking screen so that it is not gratuitously desktop-only, which mostly means not assuming a mouse, and leave the decision until there is something worth putting on a phone.

### 7. The intellectual property line, which is the same line as OnTarget

Implementing a similar workflow is fine. A manual marking interface with a scale reference is a generic interaction pattern, and the statistics are published mathematics that `docs/STATISTICS.md` already derives from the literature and validates against shotGroups. **What is not fine is copying their branding, their icons, their export layout, their wording or any file format they read or write.** Same rule as OnTarget, same reason.

Two specific cautions:

- **Alan's Ballistic X exports must not go into the repository.** They carry that product's logo and export layout. This is the `reference/` problem exactly, which already cost a git history rewrite, and it would be worse in a public repository because the files are a competitor's branded output. Keep them out, or under the already-ignored `excluded/`. They are reference for the planning session and nothing more.
- **Worth an attorney question alongside the existing one.** `docs/PATENT-SEARCH.md` already carries US7769236B2 for attorney review. Add a second: whether any live patent covers photograph-based group measurement with a user-supplied scale reference. I have not searched it and am not asserting it is clear.

### 8. What to do

Nothing yet. Finish M3. When M4 starts, its first screen is the marking screen of section 3, built as both the manual path and the correction interface `DESIGN.md` section 13 requires, with section 4's rectangle option offered beside the length. Sections 5 and 6 are scope to be specified before they are built, not now.

---

## 2026-09-14, entry 20: what the mounted photographs actually contain, which is not what entry 19 asked for and is more useful

**Status: actioned 2026-09-14.** Section 5 measured by `grouplab mounted pair`, reported in `docs/PHASE1-RESULTS.md` "Entries 19 and 20": the flat-control frame needs a surface model, 0.0206 in RMS planar against 0.0063 in developable, and the scan's holes give the photograph's detector 0 of 28 whole-frame and 26 of 28 on the registered sheet.

`scans/mounted/` now holds 28 photographs. I have looked at all of them, which is the one thing this session can do that yours cannot, and the looking is worth more than the measuring. Four of the 28 have a `~` in the filename and could not be staged here; they are on disk and you can reach them.

### 1. My own measurement failed its control, and is not reported as a result

I tried to bound the deformation without fiducials. A flat sheet through a rectilinear lens maps printed straight lines to straight lines, so the bend in a line that was printed straight is lens plus paper and nothing else. Both target families here carry a printed rectangular grid, so the grid supplies the straight lines.

**The control kills it.** Run against `scans/n568-gm210m.jpg`, a flatbed scan with no lens and no perspective and therefore physically flat, the method reports a median worst-deviation of **0.16 percent of frame** and a worst line at 0.38 percent. The photographs run 0.26 to 0.57 percent at the median and 0.52 to 0.77 at the worst. The photographs are above the floor, so real bend is there, but the floor is over half the signal and the per-image spread is narrower than the floor's own variation. It cannot rank images and it cannot give a figure in inches.

This is the `PHASE0-PRELIM.md` lesson again: a scratch measurement next to a validated pipeline is worth reporting only when it has a control it passes. It did not, so the numbers stay here as a record of the attempt and go nowhere near a results document. **Section 5 hands the real measurement to you.**

### 2. The collection is mostly close-ups of one group, and that is the finding

Of the 24 I could examine, **twelve are close photographs of a single shot group**, filling the frame, with no sheet edge and often no second bullseye in view. Two of those have fingers holding the paper in shot. This is how Alan actually photographs a target, and it is nothing like the nine pinned full-sheet frames the mounted gate is measured on, or like anything in the brief.

**GroupLab as specified cannot use any of them.** Registration needs the fiducial lattice, the lattice is spread across the sheet, and the sheet is not in frame. That is not an argument that the design is wrong: he shoots commercial targets, and there is no reason to photograph a commercial target any other way. But "the user photographs one group up close" is a real habit, it was not in any requirement, and a full-sheet requirement cuts directly against it.

**One thing makes this less bleak, and it comes from a decision already taken.** Entry 13 chose option A, the half lattice, which roughly triples marker density on the large sheets. A close photograph of one bull on a dense lattice may still contain three or four markers, which is enough to register that neighbourhood even when the sheet is not in frame. That was chosen for bracketing and for the surface fit. It may turn out to matter more for this. **Do not build anything for it now.** Record it as a possibility against the day someone asks whether a partial-sheet photograph can work.

### 3. Four target families, and one of them breaks the hole detector outright

| Family | Frames | Hole appearance |
|---|---|---|
| Orange sight-in sheets, fine orange grid on white | 12, all close-ups | Ragged tear, **bright yellow-green** where the layer behind shows through |
| Black splatter targets, yellow-green grid, red aiming squares | 7, full or part sheet | **Bright yellow-green halo**, the opposite of dark |
| NRA A-26 bullseye, buff card, hanging indoors with open air behind | 1 | Dark, with bright specks of metal on the rim |
| OnTarget 25-bull grid sheet on a mat | 1 | Dark, brown mat behind |

**`NeutralDarknessHoleDetector` is built on the survey's finding that a hole is a neutral dark region, and on seven of these frames a hole is a bright saturated one.** On a splatter target the paper's coating flakes away to reveal a fluorescent layer, so the hole is the brightest and most saturated thing in the cell. Neutral darkness is near zero there and the opening step will erase what is left. Expect it to find nothing, and treat that as the correct answer from a detector that was never asked to handle this.

That is not a defect to fix now. It is a target family nobody specified, and the honest reading is that GroupLab's detection is specified for ink-on-paper targets and this collection contains a second class of target it does not cover.

### 4. Five things in these frames that exist nowhere else in the corpus

1. **A photograph and a scan of the same physical sheet.** `scans/mounted/20260329_183028.jpg` is the `N568 GM210M` sheet, handwritten label and all, and `scans/n568-gm210m.jpg` is a 600 DPI flatbed scan of it. **This is the first ground truth for a photograph that the project has ever had.** The scan gives hole positions on a flat reference; the photograph is the same holes through a lens at an angle. Section 5 is built on it.
2. **Hand-drawn assignment arrows, in a photograph.** That same frame carries a blue arrow from bull 14 to a shot in bull 4, and a second arrow into bull 18. `SAMPLE-NOTES.md` records these on scans and `DESIGN.md` section 13 requires the manual assignment interface because of them. Here they are on a photograph, in ink, over the printed grid.
3. **A keyhole.** Bull 4 of the same frame holds an elongated gash, a tumbling bullet through the paper sideways, several times longer than it is wide. Every hole model in this project assumes a roughly round perforation with a lobed rim. Nothing would size or centre that correctly, and `getMaxPairDist` style measures would be badly wrong if it were taken as one round hole.
4. **Backer material, answered with real examples and not one answer.** The open question in the planning record asks which backer Alan shoots against. These show at least four: a fluorescent splatter layer, a second target stapled underneath, a brown mat, and open air at an indoor range. It changes the hole's appearance completely and there is no single answer to design to.
5. **Google Photos did not strip the EXIF.** Full resolution, 4000 by 3000 and 3072 by 4080, with model, focal length, f-number and 35 mm equivalent intact. **And the 2.2 mm at f/2.2 tagged as a 23 mm equivalent appears again on most of the Samsung frames**, which is the cropped-ultrawide signature entry 16 identified from three table photographs. It is his habitual capture mode, not an accident on one session, so the joint-fit grouping fix of entry 16 section 2 is load-bearing rather than an edge case.

### 5. The measurement to run, when M3 is reported

**Use the photograph and scan pair.** This needs no fiducials and no assumption about the target's geometry, because the scan is the reference.

1. Locate the bull centres in `scans/n568-gm210m.jpg` with the connected-component approach `docs/SCAN-MEASUREMENTS.md` section 2.1 already measured at 24 of 25, and in `scans/mounted/20260329_183028.jpg` the same way.
2. Match them by grid position, 25 correspondences on a 5 by 5 grid.
3. Fit the Phase 0 photograph model, homography with the two-term radial lens, scan to photograph, using the EXIF grouping rule as amended.
4. **Report the residual**, in scan inches, with its spatial correlation, exactly as M1.11 did for the pinned frames.
5. Then fit the generalised cylinder and the general developable surface from M1.10 to the same correspondences and report whether either takes the residual up.

That is entry 19's question asked properly, on one frame, against real ground truth: **how far from flat is a sheet Alan actually photographed, and is that deviation a shape paper can bend into?** Twenty-five bulls is a coarser constraint than 136 marker corners and the answer will be correspondingly rougher, but it is measured rather than eyeballed, and it is the only frame in the collection that can be measured at all.

One caution to carry into it. That sheet is **lying on a mat, not mounted**. Its corners show four staple tears, so it was on a board and came off. So it answers "how flat is a sheet Alan laid down and photographed", which is the flat control case, not the mounted one. Say so in the report rather than letting it stand as the mounted answer. **No frame in this collection is both a full sheet and mounted**, which is the frame entry 19 was asking for and did not get, and that is worth stating plainly rather than working around.

---

## 2026-09-14, entry 19: the mounted benchmark is nine photographs of a sheet hanging from one pin, and that may be the worst case rather than the normal one

**Status: actioned 2026-09-14.** Both measurements run on the one frame that supports them, the N568 GM210M photograph and scan pair, and reported in `docs/PHASE1-RESULTS.md` "Entries 19 and 20". Measurement A has no stapled full sheet in the collection to run on, as entry 20 section 5 also found; measurement B has truth on that frame only.

### 1. What Alan said, and why it matters more than it sounds

He has a large existing collection of photographs of shot targets **stapled to target boards**, taken the way he actually photographs targets. They are not GroupLab sheets, so they carry no fiducials and cannot be registered by the pipeline at all.

**The obvious reading is that they are useless to the mounted gate. That reading is wrong, and the reason is a sampling problem I introduced.**

Every frame in `scans/phase0/` that the mounted gate is measured on is the same sheet **hanging from a single pin**. I asked for that, in the Phase 0 print protocol, and entry 8 records that the original wall photographs were my fault for the same reason. Nine frames, one sheet, one mounting, and that mounting is the most deformation-prone arrangement a sheet of paper can be put in: unsupported, free at three edges, and free to twist about the pin. A sheet stapled flat against a rigid backer board is a different object. It is held against something solid, it bulges slightly between fixings, and it has very little freedom to twist.

**So `PHASE1-RESULTS.md` section M1.11 may be a true statement about the wrong population.** The finding stands exactly as measured: on those nine frames a general developable surface takes up almost none of the residual, so what is left is not a bendable shape. What is not established, and what I have been writing as though it were, is that a **stapled** target deforms the same way. A sheet twisting on a pin is precisely the case a developable surface handles worst, and it is the only case anyone has measured.

Alan did say, in entry 10, that the pin-hung photographs were a realistic scenario, and he is right that it happens. The error is not that the case is unreal. It is that it is the **only** case in the corpus, and it was chosen by me rather than sampled from what people do.

### 2. Two measurements those photographs support, neither of which needs a fiducial

**Measurement A: how much does a real mounted target actually deviate from flat?**

A commercial target sheet carries a **regular grid of bullseyes at a known nominal spacing**, which `docs/SCAN-MEASUREMENTS.md` section 2 already measured across this corpus: 1.5 in pitch on the 300 yard sheets, with the rings at 1.257 in, and a detector that finds 24 of 25 bulls by connected components on a scan. That grid is a known-geometry object. It is coarser than the fiducial lattice and it does not give a scale, but it is enough to fit a plane-to-plane mapping and look at what is left over.

For each photograph: detect bull centres, fit a homography over them, and report the residual, its spatial correlation, and whether a developable surface takes any of it up. That is the same machinery M1 already built, pointed at a coarser set of points.

What it answers:
- **How far from flat is a stapled target**, against the 0.048 to 0.114 in the pinned frames showed.
- **Is the residual developable on a stapled target?** If a cylinder or a cone takes it up where nothing took up the pinned frames' residual, then the surface work was sound and was benchmarked on an outlier.
- **What do real photographs look like** as inputs: angle, distance, framing, lens, lighting.

Report it against the pinned frames in the same table. Do not fit anything new; use what M1 has.

**Measurement B: holes in photographs, which the corpus does not contain at all.**

Every one of the 343 holes in `docs/SCAN-MEASUREMENTS.md` is from a flatbed scan. The survey's own section 3.1 says why that matters: the bright core of a scanned hole **is the scanner lid seen through the perforation**, and `SAMPLE-NOTES.md` says the same, that berm backer material does not affect scanned appearance at all and affects photographs only. **So the one measured fact the hole detector rests on does not hold in a photograph.** A hole photographed against a target board is dark, not bright, and its appearance depends on what is behind it.

`NeutralDarknessHoleDetector` has never met that case. Run it over these photographs and report recall and what it confuses, with no tuning. Whatever it does is the baseline for photographed holes, and it is a gap the synthetic work in M2.2 explicitly could not cover: M2.2 section 6 lists the dark backing as the one case where render-and-difference lost to the baseline, and this is the real version of it.

### 3. What is being asked for

A sample, not the collection. Roughly twenty photographs in `scans/mounted/`, camera originals with EXIF intact, chosen for variety rather than quality: different boards and mountings, different angles including deliberately off-axis, different distances, different light, and a couple that are frankly bad. A README naming, as far as he remembers, how each was mounted.

**Do not ask for more paper work on the back of this.** It is a file copy, it is bounded at twenty, and the range session next weekend is unchanged.

### 4. What this does not do

It does not reopen the gate, which entry 17 section 2 settled and which stays at 0.005 in. It does not resume the surface models, which entry 16 section 5 stopped. It is a measurement of the input distribution, and its purpose is to establish whether the mounted requirement is as far out of reach as nine frames of one pinned sheet suggest, or whether the benchmark was unrepresentative and the requirement is closer than it looks.

If measurement A shows stapled targets are near flat and their residual is developable, that is a finding that changes the roadmap and it should come back here as a question before anyone acts on it.

---

## 2026-09-14, entry 18: the shotGroups fixtures are generated and committed, M3 is unblocked, and question 9 is accepted

**Status: actioned 2026-09-14.** Fixtures and scripts committed in `dbea97b` (was `20f562e` before the 2026-09-14 rewrite) with the brief's section 4.4 amendment and questions 7 to 10 marked answered; M3 built against the fixtures and reported in `docs/PHASE1-RESULTS.md` M3.1, with the differences found raised as question 11.

### 1. Question 10: done, not delegated back

You were right that this belonged here, for the reason question 2 belonged here: the planning session has a Linux container and your machine does not have R. It turned out this container already had **R 4.3.3 and shotGroups 0.8.4**, the exact versions `STATISTICS.md` section 15.1 names, and `coin` besides.

**`test/fixtures/shotgroups/` now holds 64,694 rows across nine datasets plus the Monte Carlo table**, with a README carrying provenance, versions, licence and regeneration instructions. `tools/shotgroups/sg_dump.R` is extended and `tools/shotgroups/sg_distr.R` is new. Both are committed with the fixtures.

Every gap you listed is filled:

| Gap | Now |
|---|---|
| Input shot coordinates | `shots.x`, `shots.y`, `shots.distance` per shot, plus series and group indices |
| Per-group results for multi-group datasets | The whole battery per series, on all seven multi-group fixtures |
| Group-comparison tests | `compareGroups`, both branches, with the test names probed rather than assumed |
| Monte Carlo reference tables | `shotGroups_DFdistr`, 590 cells, 490 of them inside section 15.3's gate range, with complete coverage of n 2 to 50 and nGroups 1 to 10 |

**The old keys are unchanged.** Scope is empty for whole-dataset results, so every key the earlier script produced is still there with the same value. Verified on `DF300BLK`: all 454 original keys present, all 454 values matching, excepting the one stochastic key below. Build the comparison harness against the documented scheme and nothing you have already reasoned about has moved.

### 2. Four things the generation turned up, each of which would have cost you a day

**One value is not reproducible and must be excluded by name.**
`groupShape.multNorm.p.value` is a Monte Carlo energy test. Two identical runs gave 0.5424 and 0.5590 on `DF300BLK`, and 0.8346 and 0.8379 on `DFcciHV`. The script now seeds the generator so regeneration reproduces, but **a seed does not make it comparable**, because your implementation draws from a different generator. Each JSON lists the key under `stochastic`. Exclude it there rather than meeting it as a 1e-12 failure. Every other value in every fixture is deterministic, which I checked by running each dataset twice and diffing rather than by assuming.

**`DFsavage` produces no angular keys at all, and that absence is the test passing.** Section 15.5 point 3 wants multiple distances in one frame to suppress angular output rather than produce a wrong number. `angular.nDistances` is 3 and there is no `getMOA` or `fromMOA`. The count is emitted either way, so your harness can assert on positive evidence instead of on a missing key, which is the difference between a passing test and an untested path.

**Range statistics stop at n = 100**, the largest cell shotGroups tabulates. `getRangeStat` runs at any size, warning and returning NA intervals past the table, but `range2sigma`, `range2CEP` and `getRangeStatEff` raise an error. The pooled scope of the five large datasets carries `_error` keys for those three. Every per-series scope is well inside the table. Expect the errors; they are the package's limit, not a generation failure.

**`DFlandy01` does not exercise what section 15.2 chose it for**, and this one is a defect in the plan rather than in the data. It was picked for "range statistics with many groups", but `getRangeStat` takes a coordinate matrix and has no group argument: handed a 53-group frame it pools all 530 shots into one. The package's real multi-group path is the `nGroups` argument of `range2sigma`, `range2CEP` and `getRangeStatEff`, now emitted under `multiGroup.*`. **Those tabulate only to 10 groups**, so `DFlandy01` at 53 is past the table and carries `multiGroup.beyondTable` rather than values. **`DFlandy04`, at 6 groups, is the fixture that actually exercises the multi-group range path.** Amend section 15.2's stated purpose for both rather than leaving a fixture described as testing something it cannot reach.

I also found and fixed a bug of my own while generating these, which is worth one line because it is the same class of error the project keeps meeting: a single out-of-range call inside one error handler was discarding three working results alongside it. Split handlers, and a section that could not run records an `_error` key instead of vanishing, because a fixture that silently omits a section looks identical to one whose section produced nothing.

### 3. Question 9: accepted, and my brief was internally inconsistent

Change gate 2's matching tolerance to **0.15 in**, and keep centre accuracy reported rather than gated, judged against real paper.

You are right and the brief was contradicting itself. `docs/PHASE1-BRIEF.md` section 4.4 point 2 set a "hole-centre tolerance of 0.01 in" for counting a detection as a true positive, and point 3 said centre accuracy is "reported, not gated". The first is a matching radius, deciding whether a detection and a truth hole are the same hole, and the second is an accuracy requirement. I wrote a matching radius at the value of an accuracy requirement, and at 0.01 in it sits on the 0.008 in noise floor, so a correct detection of a real-looking hole would be scored a miss and a false positive at once.

**0.15 in is the right number for a better reason than being looser**: it is the hit tolerance `SCAN-MEASUREMENTS.md` section 8 used for the naive baselines. Adopting it makes your figures directly comparable with the 1 of 27 and the 7 of 20 already on record, which is worth more than any number I would pick now. It is also unambiguous at this geometry, being about half a hole diameter and a tenth of the 1.5 in bull spacing, so it can pair a detection with the right truth hole and cannot pair it with a neighbour.

Amend section 4.4 in the brief itself, marked as an amendment with this entry as the reason, rather than silently. A brief is a record of what was asked.

### 4. Questions 7 and 8 are already answered

Entry 16 answered both, section 3 for question 7 and section 2 for question 8, and your last report confirmed you did the work: the joint-fit key now includes the 35 mm equivalent and the image size, the disagreement warning is gone, and the sweep and frozen READMEs state that their sheets fail test 26f by design. Only the `Status:` lines in `docs/QUESTIONS-FOR-PLANNING.md` were not flipped. Mark both answered, pointing at entry 16.

**Nothing about next weekend's printing is waiting on anybody.** Question 7's answer has been standing since entry 16: print the five marker-size sheets as they are.

### 5. Carry on

M3 is unblocked. Build the engine against `docs/STATISTICS.md` section 15 with these fixtures. The two open questions of section 16 were answered in `docs/PHASE1-BRIEF.md` section 5 and still stand: apply `c4` to the interval endpoints and match shotGroups, and make mean radius the headline with sigma beneath and extreme spread subordinate.

Then M4. The rest of the brief is unchanged.

---

## 2026-09-14, entry 17: the gate does not move, the mounted case is an open requirement, and M2 starts now

**Status: actioned 2026-09-14.** Sections 2 to 4 are `docs/PHASE1-RESULTS.md` M1.11 and `DESIGN.md` section 21 [r6] (the gate record, the open requirement, frames fitted alone by default, the plane below eight corners); section 5's diagnostic is M1.11 (the leftover is mostly not structured, with a structured part on three mounted frames); section 6's baseline is M2.1 (the port reproduces all 343 holes exactly), and M2 continues.

### 1. The surface models are finished and the answer is no

Recorded as the result it is. A cylinder fails all seven mounted frames. A general developable surface, which covers the cone and the twist your own shape test could not separate, also fails all seven, and it moves the corner residual by at most 0.16 px. That last figure is the informative one: **a model with strictly more freedom took up almost none of the leftover error, so the leftover is not a shape paper can bend into.** Paper bends without stretching, the fit now allows any such bend, and the error stayed. Whatever is left is not developable.

You built it, swept it, ran it once, and reported a negative. That is the outcome the brief was written to allow and it is worth more than a model that passed because it was tuned.

### 2. I proposed loosening the gate and I am withdrawing it

Alan was asked whether to argue a separate, looser gate for mounted photographs, on the grounds that the 0.005 in figure was inherited from the paper gate and never argued on its own. He declined to decide and asked me to. So I worked the error budget properly, and **it does not support loosening.**

The finest quantity this system measures is a hole centre, whose noise floor on real paper is 0.008 in. Standard practice is that a subsystem should contribute no more than about a third of the dominant term, so that it adds under five percent in quadrature. A third of 0.008 is 0.0027 in. Half is 0.004 in. **A principled budget argues for 0.003 to 0.005 in, which is where the gate already sits, at the loose end.** From the statistics the constraint is looser still, since registration error of even 0.02 in inflates an estimated sigma by under one percent on a typical group, and assignment is safe at a 1.5 in cell, but neither of those is the right anchor: the gate exists so that the instrument is not the limiting factor in what it reports.

There is a real argument I could keep pulling on, that "worst bull of 28" is a bound where the budget above is about typical error, and a worst-of-28 on a distribution with a 0.004 in typical value would plausibly run to 0.008 or 0.010. **I am not making that argument, because I am making it after seeing the results, and the best mounted frame came in at 0.00604 in.** A gate that lands within a thousandth of the number that makes one frame pass is not a gate. The Phase 0 gate is trustworthy precisely because `docs/PHASE0-PRELIM.md` set it before the results existed.

**So the gate stays at 0.005 in.** If it should move, the time to argue it is when there are more mounted frames, the argument gets written down and the number fixed **before** the new frames are measured, and it is then tested on frames that were not used to set it. Record this entry in the results document as the reason the number did not change, because a reader in six months should be able to see that loosening was considered and refused, and why.

### 3. The mounted photograph gate becomes an open, unmet requirement

Not a failure to fix now, and not a promise to withdraw. What is true today:

- **The scan path passes**, ten of ten at 0.00325 in worst.
- **The flat photograph path nearly passes.** Every scoring bull on the one frame that decoded all its markers is inside, and the failures are named.
- **The mounted photograph path does not pass**, by any developable surface, and the residual is not a bendable shape.

`DESIGN.md` section 21 already carries the mounted gate as Phase 1's and expects it to fail until a surface model exists. Amend it to say that a surface model now exists, that it is a general developable fit, that it does not meet the gate, and that the requirement is open with piecewise registration as the recorded fallback. Do not attempt piecewise now. It costs days budgeted for hole detection, it cannot help a bull outside the lattice, and the sensible time to try it is when real shot targets exist.

### 4. Two changes to make before M2, both cheap and both yours

1. **Fit each frame alone by default.** Your own finding: `main1` gives 0.00604 in alone and 0.01177 in the joint fit, a factor of two for sharing a camera across frames. A user photographs one target at a time, so alone is also the real usage. Joint fitting was only ever a way to constrain the lens, and entry 16's measurement showed the lens barely matters, moving the worst bull by at most three percent. Keep joint fitting available for a set known to be identical; make alone the default.
2. **Fix the selection defect.** Model selection defaulting to "bend kept" below eight corners is backwards: fewer corners means less evidence, so the default must be the model with fewer parameters. Default to the plane. Nothing triggers it today, which is the cheapest possible moment to fix it.

### 5. One bounded diagnostic, alongside M2, because it changes what we ask Alan to photograph

**Is the leftover residual on the mounted frames structured or random?** One command, using machinery this project already has: measurement 6 of Phase 0 computed spatial correlation on a displacement field to prove the printer error was paper-fixed, and `field.json` is its output. Run the same correlation over each mounted frame's post-fit corner residual.

- **Structured**, meaning neighbouring corners deviate together: there is real unmodelled geometry, it is not developable, and the advice to a user is about how the sheet is held.
- **Random**, meaning white across the sheet: the limit is corner quality on a foreshortened, defocused frame, and the advice is about light, aperture and distance.

Those are different sentences in the paper protocol, which is why it is worth one command now. It is a measurement and not a model; it does not reopen section 1 and it does not authorise a fourth surface. Report it in `PHASE1-RESULTS.md` and continue to M2 whatever it says.

There is a clue already. The flat controls sit at 0.60 to 0.84 px of corner residual and pass. The mounted frames sit at 0.96 to 1.88 px and miss by six to twenty times. **Roughly double the residual producing twenty times the bull error is the signature of correlated error rather than noise**, because independent noise averages out over a hundred-odd corners and structure does not. I expect structured. Measure it rather than taking it.

### 6. M2 starts now

`docs/PHASE1-BRIEF.md` section 4, unchanged. Port the neutral-darkness primitive of `SCAN-MEASUREMENTS.md` section 3.1, revalidate on all 343 holes, commit that as the baseline, then build the synthetic GroupLab sheets with holes drawn from the survey's own measured distributions so render-and-difference can be tested against truth before real shot targets exist.

### 7. On the three runs and the minimiser change

Running the frames three times so that the committed code reproduces the committed rows, and discarding the first run's raw rows because the committed code no longer produces them, is correct and is the rule from entry 11 applied to your own output rather than to a definition. Reporting that the minimiser change altered fourteen of a hundred and sixty synthetic trials and turned a marginal pass into a fail, rather than quietly keeping the better table, is the same discipline. Both are worth more to this project than a passing gate would have been.

---

## 2026-09-14, entry 16: two corrections to entry 15, both mine, and the cone goes before M2

**Status: actioned 2026-09-14.** Section 5's table is `docs/PHASE1-RESULTS.md` M1.8 (mounted frames at 0.96 to 1.88 px, so shape); section 2 is M1.9 (joint fits keyed on pixel geometry, no warning, the product finding); section 3 is in the module sweep and frozen READMEs; section 4's note is in M1.2; the general developable surface is M1.10, which recovers a synthetic cone and passes no mounted frame, so the surface models stop and M2 is next. Originally: answers questions 7 and 8, and decides the order.

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

**Status: actioned 2026-09-14.** `grouplab surface frames` ran in the foreground with a progress line per frame, its crash recorded at `430081b` (was `ab42e33` before the 2026-09-14 rewrite) and the completed run and M1 report at `e30fbeb` (was `d574a7e` before the 2026-09-14 rewrite), and the standing rule is in `CONTRIBUTING.md` under "Long-running steps" with entry 15 section 6's sentence appended.

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

**Everything else about how you are working is right**, and this is a mechanical fault rather than a judgement one. Measuring entry 11's change before making it, parking the blocked instruction and carrying on with the independent one, committing the surface fit at `88dc0f9` (was `d78c17c` before the 2026-09-14 rewrite) before any real frame was run, and making both M1.3 changes on synthetic evidence with the reasons recorded, are all exactly right.

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
