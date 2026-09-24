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

## The archive

Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the
only written record of why much of this project is the way it is.

- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 139, 136 of them.

---

## 2026-09-24, entry 165: the analysis screen offers to send the target to the project

**Status: actioned 2026-09-24**, every section, built and tested, **and switched off**: `appOpen` is false in `website/api/limits.json`, so the question, the first run screen and the Settings choice do not appear and nothing is sent. **Not done:** turning it on. Section 8 hoped for nothing on the server, and there is one step: the nginx include needs a block for the new receiver, request 21. Once that answers 400 to an empty post, a later entry sets `appOpen` true. The receiver's own tests (section 7 item 5) and the worker's run in CI, because PHP is not installed here.

Alan: "We should add a question to the analysis screen that will send a copy of the picture or scan of
the target along with the logs and analysis of the target to help refine the software and detection. It
should automatically upload to the targets page and can be downloaded via the powershell script already
in place."

This is the feature that turns every user into test data, and it is worth more to detection than any
single change to the detector. Two nights of this project have already shown why: the friend scan of
entry 161 exposed a wrong constant that six of Alan's own scans had not.

**It depends on entry 129.** The receiver side waits on the five server steps in request 1 of
`docs/notes/for-alan.md`. Build the application side and the receiver now; keep the question hidden
until `open` is true in `website/api/limits.json`, exactly as the upload page is.

## 1. The question

After **Accept and analyse**, once the analysis is on screen, one short panel asks:

> Help improve GroupLab's detection? Send this target to the project: the image, what GroupLab found,
> what you corrected, and the log from this session. [What gets sent] [Send] [Not this time]

1. **It is asked, never assumed.** Nothing is sent until the person says yes. "Automatically upload" in
   Alan's words means that once they say yes it goes without any further steps, not that it goes
   without asking.
2. **"What gets sent" shows exactly that**: the image, the list of files, the approximate size, and the
   consent text. No surprises.
3. **Settings has Ask every time, Always send, and Never ask.** Ask every time is the default. Always
   send is available for somebody like Alan's friends, and it can be turned off in one place.
4. **Not this time is final for that target.** The question does not come back for the same session.
5. It never blocks the analysis, never delays it, and never appears on top of the numbers.

### 1.1 Asked once, on first run (added 2026-09-24)

Alan's friend, the first outside user: "when you first open the program, ask if you want to opt in for
sending your targets to grouplab for analysis and if they choose yes, it automatically uploads their
targets, results, and logs."

So the choice is offered **once, on first run**, as well as being in Settings:

1. The first time GroupLab opens, one screen asks whether to send targets to the project, with the same
   two consent levels as section 2 and a plain list of what is sent. The choices are **Send every target
   automatically**, **Ask me each time**, and **Never**. Nothing is preselected; the person must choose.
2. **Send every target automatically** means exactly that: after Accept and analyse, the package goes
   with no further question, and a small unobtrusive line says it was sent.
3. **Ask me each time** is the per target question of section 1.
4. The first run screen is shown only while `open` is true. Before the receiver exists, asking a person
   to send targets to a place that does not answer is the worst of both worlds, so the screen waits and
   appears on the first start after the receiver opens, once.
5. The public address people see for this is `grouplab.org/targets`, which does not exist yet. It is the
   page that explains what is collected and why, and it is built with the receiver.

## 2. Consent, in two levels

`consent_v1` on the upload page is all or nothing: it says the photos may be published. For the
application, a person may be willing to help test and not willing to be published, which is exactly the
distinction the two friend scans needed. So:

- **Level 1, testing only**: used to test and improve detection, kept by the project, never published.
- **Level 2, publishable**: level 1, plus it may be published in GroupLab's public test data and in
  research articles under the GPL-3.0.

The person picks one when they say yes. Record it as `consent_v2` with the level, and make the upload
page offer the same two levels from the same text, per entry 159's one source rule. A submission under
`consent_v1` stays publishable, because that is what the person agreed to.

## 3. What is sent

1. **The image, with its pixels untouched.** Every metadata block is stripped before it leaves the
   machine: no GPS, no location, no timestamp, no camera serial, ever. If the original exceeds the
   receiver's 30 MB per file limit, re-encode it losslessly. Never send a lossy copy, because the whole
   point is to measure detection on the real pixels. If lossless still does not fit, say so and do not
   send.
2. **What GroupLab detected**, before any human change: every mark, its position, diameter, size in
   holes, bull, and the review items raised.
3. **What the person changed**: marks moved, added, deleted, split into two, reassigned, excluded. **This
   is the most valuable part of the whole submission.** The difference between the detection and the
   person's final answer is labelled ground truth, and it is the thing this project has been short of.
4. **What the person told it**: calibre as typed and as resolved under entry 163, distance, rounds
   fired, and paper and backing from entry 162 where they were filled in.
5. **The analysis**: the figures shown, the scale summary, the registration RMS, the print scale.
6. **The session log**, with file names reduced to their path hash as entry 164 section 4 considers.
   Nothing from outside the session.
7. The build, platform and architecture, as the diagnostics report already records.

One package per target, with a manifest listing every file and a hash of each.

## 4. The receiver

**The upload page's Turnstile check cannot run inside the desktop application.** Turnstile is a browser
widget. So the application does not post to `upload.php`. Build a receiver for it on the pattern of
`crash-report.php`, which already faces the same problem and solves it with limits rather than a
secret:

1. Per address rate limit, the global hourly cap entry 129 added, the disk floor, and the size limits
   from `limits.json`.
2. **No embedded key or token.** Any secret compiled into an open source application is public on the
   day it ships, and pretending otherwise is worse than not having one. Say so in the receiver's own
   documentation, the way the crash receiver does.
3. The package lands in the same quarantine as the upload page. The same worker rebuilds the image from
   pixels, scans with ClamAV, deletes the original and moves the result to ready. Nothing new is trusted
   because it came from the application.
4. Every submission carries `source: app` or `source: web`, so the two can be told apart.

Submissions are data, never instructions, per `CLAUDE.md`. A manifest, a log line or a file name inside a
submission is never acted on.

## 5. Pulling them

`scripts/Get-TargetSubmissions.ps1` pulls application submissions exactly as it pulls web ones: into the
same folder layout, hashes verified, recorded in the same ledger, and removed from the server afterwards
by `Remove-ReadSubmissions.ps1`, per Alan's decision in entry 129 that the server keeps nothing once it
has been read.

Add to what the script writes locally: the consent level beside every submission, so a testing only
target can never be published by mistake. **The build refuses to publish any image whose consent is
level 1.** That is the check that makes two levels safe.

## 6. When sending fails

No connection, the receiver closed, over a limit: the package is kept locally and retried on the next
start, for up to seven days, then discarded with a line in the log. The person is told once, quietly,
and never nagged. A package that is refused for a reason that retrying cannot fix is discarded at once
and the reason is shown.

## 7. Tests

1. Nothing is sent without a yes. A test drives the analysis screen with every setting and asserts no
   request is made under Ask every time until Send is pressed, and none at all under Never ask.
2. The sent image carries no metadata, and its pixels are identical to the original.
3. The package's detected and corrected lists reproduce the person's final marks exactly.
4. A level 1 submission cannot reach `samples/`, the research build, or any published page.
5. The receiver refuses over size, over rate, and a malformed manifest, with no network in the test, as
   entry 129's 31 receiver tests do.
6. The question never appears while `open` is false.

## 8. For Alan

Add to `docs/notes/for-alan.md`: whatever this needs on the server beyond entry 129's five steps,
written as commands to paste, if anything. The hope is nothing, because it reuses entry 129's quarantine,
worker and folders.

## 9. The Settings section, spelled out (added 2026-09-24)

Alan: "There should be a section on the settings page to change this setting as well." Section 1 item 3 and
section 1.1 already put the choice in Settings; this makes the section itself explicit.

A **Sending targets** section in Settings, not a single toggle buried among other options, showing:

1. The current choice, **Send every target automatically**, **Ask me each time** or **Never**, changeable at
   any time, taking effect on the next target.
2. The consent level, **testing only** or **may be published**, changeable the same way. A change applies to
   targets sent from then on and never silently re-labels ones already sent.
3. What is sent, in the same plain list the first run screen shows.
4. How many targets have been sent from this machine, and how to ask for one to be removed: the submission
   identifier and `support@grouplab.org`.
5. Anything waiting to be retried under section 6, with a button to send it now or discard it.

The first run screen and this section read and write the same setting, so they cannot disagree.

---

## 2026-09-23, entry 158: two research programs, and how to decide what is worth an article

**Status: actioned 2026-09-24**, sections 1 to 3. **Not done:** program A steps 3 and 4, what five shots tell you and its article, because in the photographs the holes of a group merge and cannot be placed: request 19 asks whether the ST-4 can be scanned. Program B step 4, the article, is held for the test request 20 describes. Program A step 1 was replaced by entry 172, whose ST-4 ground truth is the material; the owner photographs are not used for counts.

Alan: "When more research is performed by claude, it should be evaluated whether the conclusions from
that research are significant and worth writing a research article about. The goal is to advance the
science of shooting, so anything that can help other shooters or developers is worth writing about."

That is a standing rule, and it comes with two specific programs he has the material for.

## 1. The standing rule

After any piece of research, measurement or investigation, decide whether it is worth an article, and
record the decision either way.

**The test:** would this change what another shooter does, or what another developer builds? If yes,
write it. Being a negative result is not a reason to skip it. "We measured this and it turned out not
to matter" is one of the more useful things a shooter can read, because the belief it corrects is
usually costing somebody ammunition.

**Record it.** `docs/RESEARCH.md` gains a short list of investigations considered and not written, each
with one line saying why, so a rejected idea is not reconsidered from scratch every time somebody reads
the results file. The two reasons that should appear most often are "the data cannot separate the
effect from the confounds" and "already covered by article N".

Three things are already sitting in the results files that may pass this test, and should be assessed
under it: question 38's finding that a photographed hole has no size constant, question 44's held out
marker result showing the bent sheet model predicts an unseen marker as well as a fitted one, and
entry 130's photograph against scan agreement. Say for each whether it is an article, and if not, why.

## 2. Program A: the traditional 5 shot and 10 shot groups

Alan: "For the 100 yard precision rifle target pictures, most of the groups were 5 shots with a 6.5
Creedmoor. Some were 10 shots. I think this is worth studying and drawing conclusions from since they
are groups that were shot in a traditional method where a single point of aim was used for 5 or 10
shots with often overlapping holes. I can answer questions about the photos, point out where the point
of aim was, describe the scaling on the target, and verify how many shots were shot at each group.
These are very good examples of traditional groups."

This is the most valuable material in the project, for two reasons. Overlapping holes at a single point
of aim are the hardest case for automatic detection and the most common case in real life. And the
5 shot group is what nearly every shooter uses and nearly every shooter over-reads.

**Step 1, and the answers are already being collected.** The twenty six owner photographs in
`C:\\Dev\\grouplab-testdata\\owner` are the material. The planning session is collecting the shot count,
the point of aim, the scale reference, the cartridge and load, and anything unusual, one line per
photograph, and will deliver them as an inbox entry. Write the request into `docs/notes/for-alan.md`
per entry 149 section 5 and carry on; do not ask him in the panel and do not wait.

**Step 2, detection performance on overlapping holes.** With his counts as ground truth, measure misses
and false positives per group. Report it the way the scan table in question 35 is reported. This is the
first honest measurement of the case that matters most, and whatever it says is worth knowing.

**Step 3, what 5 shots tell you.** On the same load, compare 5 shot and 10 shot groups: extreme spread
growth with sample size against the expected growth, mean radius stability, and the width of the
confidence interval at each. `docs/STATISTICS.md` already carries the theory. This puts real groups
against it.

**Step 4, the article.** What a 5 shot group actually tells you, with these targets as the evidence and
the images in it, per entry 153 section 3. This is probably the single most useful article the section
will carry.

## 3. Program B: hole size against velocity and nose shape

Alan: "Some of these conclusions drawn are about supersonic bullets creating holes in paper. It should
probably be researched how the speed and shape of the bullet affect the size of the hole on the paper."

**What is in hand:**

| cartridge | muzzle velocity | nose | nominal diameter |
|---|---|---|---|
| 22 LR | about 1080 fps | round nose | 0.222 in |
| 6 ARC | about 2500 fps | spitzer | 0.243 in |
| 6.5 Creedmoor | 2845 fps | spitzer | 0.264 in |

**What could be shot later:** subsonic 22 LR, 300 Blackout, 8.6 Blackout and 510 Whisper, all at
roughly 1000 to 1080 fps. That set is the useful one, because it puts large diameter subsonic holes
beside small diameter subsonic holes and beside supersonic holes, which is what separates diameter from
velocity.

**Step 1.** From the existing scans, measure hole diameter against nominal bullet diameter for each
cartridge and report the ratio with its spread. That is a fact worth having regardless of what causes
it.

**Step 2, and this is the part that keeps it honest.** State plainly what is confounded. Paper, backing
material, distance, lighting, scanner against camera, and angle of incidence all differ between these
groups, and every one of them plausibly changes a hole. Do not claim a velocity effect that this data
cannot separate from those. Question 38 already showed that light and shadow, not resolution, drive
the measured size in a photograph, which is exactly the kind of confound at issue.

**Step 3.** Design the test that would separate them and write it out so Alan can shoot it: the same
paper, the same backing, the same distance, the same imaging, varying only the cartridge, with the
subsonic set as the key comparison. Say how many holes per cartridge are needed for the comparison to
mean anything, using the sample size work in `docs/STATISTICS.md`.

**Step 4.** Hold the article until the data supports it. An article that says "here is what we can
measure, here is what we cannot yet separate, and here is the experiment that would settle it" is
publishable and is better than a guess. If that is the article, write that article.

## 4. Where this sits in the queue

After entries 149 to 157. Step 1 of each program is the part that needs Alan, so it goes in the entry
149 section 5 list at the start of the run even though the work itself comes last.

## 2026-09-23, entry 157: how the mobile application takes the photograph

**Status: actioned 2026-09-24**, every section: 1, 2, 3 and 6 as the specification `docs/MOBILE-CAPTURE.md`, and 4 and 5 built on the desktop and measured. **Not done:** the mobile screen itself, which waits on Android; the angle limit is bounded by the measurement rather than fixed by it, question 54 and request 18; the paper's outline cannot be found on a white board, question 54; and entry 172 section 3 item 4 still waits, because the ST-4's paper is on that board and its printed grid is not detected.

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

## 2026-09-23, entry 156: hit probability on the Ballistics screen, from the shooter's own dispersion

**Status: actioned 2026-09-24**, every section. **Not done:** section 8 item 4's fourth shape, a bull from GroupLab's own library, which the entry calls natural rather than asks for; section 8 item 5's vertical and horizontal split is given as figures per source and in total, not as separate graph views. Three judgment calls are question 53: the presets' figures, the refusal threshold and the distance a pooled group's velocity share is taken out at.

Alan: "The ballistics page should also do a hit probability calculation based on the group statistics.
Use https://www.blackburndefense.com/tools/hit-probability as a guide on how to make it. As far as I am
aware, this is basically a monte carlo simulation. Applied Ballistics Quantum and their desktop
software also offer this functionality."

This is the feature where GroupLab has an advantage over every tool that already does it. The others
ask the shooter to type in a precision figure, usually in MOA, usually remembered from a good day.
GroupLab has the shooter's measured dispersion with its confidence interval sitting in the same
application. So the answer can carry an honest uncertainty, and that is the thing to build the feature
around rather than a prettier scatter plot.

Do this after entries 149 to 155. It is the largest piece here and it is the one that benefits most
from Alan's screenshots, which entry 149 section 5 asks him for.

## 1. Inputs

Pre-populate everything GroupLab already knows. Nothing the application can measure should be typed in.

| input | where it comes from |
|---|---|
| dispersion | Rayleigh sigma of the selected session, load or pooled group, with its chi squared interval and its shot count |
| distance | typed, defaulting to the distance the group was shot at |
| target size | typed: a circle of diameter D, or a rectangle W by H, in inches, centimetres, MOA or mil |
| muzzle velocity and velocity SD | the chronograph data where the load has it, typed otherwise |
| ballistic coefficient, bullet weight, atmosphere | the Ballistics screen already holds these |
| wind speed and direction | typed |
| wind call uncertainty | typed, with a stated default and a sentence saying what it means. At distance this usually dominates everything else |
| range estimation error | typed, defaulting to zero for a known range |
| zero error | typed, defaulting to the uncertainty in the measured centre from aim, which GroupLab knows |
| shots in the string | typed, default 1 |
| trials | default 10000, and a seed, so a result is repeatable and a screenshot can be reproduced |

Hide the last four behind an "advanced" disclosure with sensible defaults, so the screen is usable
before a shooter knows what a wind call uncertainty is. Every one of them has a tooltip from entry 154.

## 2. The model

Each simulated shot is the sum of independent contributions, drawn per trial:

1. **Dispersion**, from the bivariate normal with the measured sigma, drawn **per shot**.
2. **Vertical from velocity**, the velocity SD carried through the drop curve at that distance, drawn
   per shot.
3. **Horizontal from the wind call**, the wind call error carried through the wind deflection, drawn
   **per string** rather than per shot, because a shooter reads the wind once and fires; a per shot
   draw makes wind look like dispersion and flatters the result.
4. **Range estimation error**, through the drop curve, drawn per string for the same reason.
5. **Zero error**, a fixed offset drawn once per string. A zero error does not resample between shots.

The distinction between per shot and per string is the part most tools get wrong, and getting it right
is a defensible reason for this to exist. Write it down where the model lives, not only here.

Also draw sigma itself from its own sampling distribution across trials, so the output interval
includes the uncertainty in the shooter's own precision estimate. A hit probability computed from nine
shots is not as knowable as one computed from ninety, and the screen should show that.

## 3. Outputs

1. Probability of a hit with one shot, **with an interval**, and the interval reflects both the Monte
   Carlo count and the uncertainty in sigma. Show which of the two dominates.
2. Probability of at least one hit in N shots, and the expected number of hits in N.
3. The impact scatter with the target drawn on it.
4. Hit probability against distance, as a curve, with the interval as a band.
5. **A sensitivity list**: which input is costing the most probability. This is the output a shooter can
   act on. It answers whether to practise wind calls, work on the load, or buy a rangefinder, and no
   other output on the screen answers a question the shooter can do anything about.

## 4. Honesty

1. Never show a point estimate without its interval.
2. State the assumptions in one short paragraph on the screen: the dispersion is taken to be the same
   from shot to shot, the error sources are taken to be independent, and the target is taken to be
   engaged from a stable position like the one the group was shot from.
3. Do not report more than two significant figures. A hit probability of 73.42 percent is a claim the
   model cannot support.
4. Refuse, with a plain sentence, when the group behind the sigma is too small to say anything. Say how
   many shots would be needed for the answer to mean something, using the sample size work already in
   `docs/STATISTICS.md`.

## 5. Validation

There is an exact answer for the simplest case, so the simulation must reproduce it. For a circular
target of radius R centred on the point of aim, with only bivariate normal dispersion of parameter
sigma and no other error:

    P(hit) = 1 - exp( -R^2 / (2 * sigma^2) )

1. A test that the Monte Carlo matches that expression to within its own Monte Carlo error across a
   range of R over sigma.
2. A test that the seed makes a run repeatable.
3. A test that adding an error source never increases the hit probability.
4. A test that a per string error source and a per shot error source of the same size give different
   answers for a multi shot string, which is what stops item 2 of section 2 being quietly undone later.

## 6. On the other tools

Blackburn Defense, Applied Ballistics Quantum and the Applied Ballistics desktop software are the
reference for **what a shooter expects to see and which inputs are worth showing first**. Alan will
send screenshots. Use them to decide the layout and the defaults.

Do not copy their wording, their layout, their labels or any code. Build the model from section 2.
Where GroupLab's answer differs from theirs for the same inputs, that is worth investigating and
possibly worth an article under entry 158, but it is not a reason to change the model to agree.

## 7. What the Blackburn Defense calculator actually does (added 2026-09-24)

The planning session opened the page and ran it with its defaults. Record this as the reference, and
still build from section 2's model rather than from their layout or wording.

**Inputs, with the page's defaults:**

| input | default |
|---|---|
| target | 20 in circle |
| iterations | 1000 |
| group size | average five shot group 1 MOA, standard deviation 0.5 MOA |
| muzzle velocity | 2800 fps measured, 2800 actual, standard deviation 10 fps |
| BC | 0.3 measured, 0.3 actual, standard deviation 0.003 |
| range | 1000 yd measured, 1000 actual, standard deviation 3 yd |
| wind speed | 10 mph measured, 10 actual, standard deviation 2 mph |
| wind direction | 90 degrees measured, 90 actual, standard deviation 5 degrees |

**Outputs:** a scatter of simulated impacts in two colours, and two numbers. With the defaults above:
first round hit probability 40.8 percent, second round hit probability 75.0 percent. The second round
cloud is much tighter, because the second shot is corrected from where the first one landed.

**Two ideas worth taking, both of which fit section 2's model:**

1. **Measured, actual and uncertainty are three separate things.** For every environmental input the
   page distinguishes what the shooter believes (measured), what is really true (actual), and how
   uncertain the belief is (standard deviation). Setting measured and actual apart models a **bias**,
   such as a chronograph reading 20 fps fast, which a standard deviation alone cannot. Offer the actual
   value behind the advanced disclosure, defaulting to the measured one.
2. **A second round probability, after correcting from the first impact.** This is the number that
   matters in real engagements and on most match stages. Model it honestly: after the first shot, the
   shooter corrects by that shot's observed miss, which removes most of the per string errors of section
   2, the wind call, the range error and the zero error. **But the correction also contains the first
   shot's own random dispersion**, because the shooter cannot tell which part of a miss was the wind and
   which part was the rifle. So the second shot carries its own dispersion plus the first shot's, and a
   model that ignores that will overstate the second round number. Show first round and second round
   probability side by side, and say in the assumptions that the second assumes the first impact was
   seen.

**Where GroupLab should do better than the reference:**

- The reference asks for an average five shot group in MOA. GroupLab has the shooter's measured
  dispersion with its confidence interval, which is a better input than a remembered group size, and
  the extreme spread of five shots is a poor estimator of dispersion in the first place.
- The reference shows a single percentage with no interval. Section 3 item 1 still stands: show the
  interval, including the uncertainty in the shooter's own measured dispersion.
- 1000 iterations gives a Monte Carlo standard error of about 1.6 points on a probability near 50
  percent, far coarser than the tenth of a percent the page prints. Keep section 1's default of 10000,
  and never print more precision than the trial count supports.

## 8. What Applied Ballistics Quantum's WEZ screen does (added 2026-09-24)

Alan sent eight screenshots of the WEZ calculator in the Applied Ballistics Quantum app, with his own
6.5 Creedmoor profile loaded, and pointed at https://appliedballisticsllc.com/weapon-employment-zone-wez/
for the background. This is the tool he uses, so it is the stronger of the two references. Same rule as
section 6: learn from it, copy none of its layout, labels, numbers or code.

**What it shows.** Hit probability sits in a strip at the top of the firing solution, beside energy,
elevation and two wind holds. So a shooter reads the probability with the dope, not on a separate page.
Below that, the WEZ view draws the simulated impacts over the target.

**Its inputs:**

- range, target type, and target width and height, 12 by 12 in at 1000 yd in his screenshots
- target types: IPSC, rectangle, circle, and animal outlines (deer, coyote, elk, prairie dog)
- graph type: shot simulation, vertical uncertainty, horizontal uncertainty, and probability of hit
- **uncertainties**, each as one number: range, muzzle velocity, wind speed, drag (as a percentage of
  the drag model), rifle precision (in mrad), temperature, pressure, humidity, azimuth, inclination and
  latitude

**Confidence presets.** One control, Low, Medium, High or Custom, sets every uncertainty at once. With
his 12 inch target at 1000 yd, the presets gave 4, 25 and 84 percent, and his own custom values gave 51
percent. That spread is the whole lesson of the screen: the uncertainties decide the answer far more than
the rifle does.

**What to take into GroupLab:**

1. **Show the probability with the dope.** Put the hit probability on the Ballistics screen's solution,
   beside the elevation and wind holds, as well as on its own view.
2. **Confidence presets that set every uncertainty at once**, with Custom for anything edited. Define
   GroupLab's own presets, each described in a sentence by the situation it represents, such as "known
   distance, measured wind" against "lasered distance, estimated wind", and document where each number
   comes from. Do not reuse the Quantum preset values.
3. **Widen section 1's inputs** to include the uncertainties Quantum carries that section 1 does not:
   drag model uncertainty as a percentage, and temperature, pressure, humidity, azimuth, inclination and
   latitude. All of them go behind the advanced disclosure; the presets fill them.
4. **Target shapes**: circle, rectangle and IPSC first. A target from GroupLab's own library, by its bull
   size, is a natural fourth. Animal vital zones are a later decision, not part of this entry.
5. **Vertical and horizontal uncertainty views.** Section 3 item 5's sensitivity list is the same idea
   done better, since it names which input costs the most. Offer the vertical and horizontal split as
   well, because it is how Quantum users already think.

**Where GroupLab is ahead, and this is the reason to build it at all:** Quantum asks the shooter to type
"rifle precision" and a velocity uncertainty. GroupLab has measured both. Pre-populate rifle precision
from the selected load's measured dispersion and velocity uncertainty from its chronograph data, each with
its confidence interval, and say on screen where each came from. **State the definition exactly**:
"rifle precision" here must be the per axis standard deviation in mrad, which for circular dispersion is
the Rayleigh parameter. Mixing up a per axis figure with a radial one changes the answer considerably, so
write the conversion down, test it, and never let two numbers with different definitions share a field.

## 2026-09-23, entry 155: one Targets screen, because the library and the print dialog do the same job

**Status: actioned 2026-09-24**, every section. Section 3's old addresses are plain pages with one link to the new one rather than redirects, because entry 151 bans a meta refresh and a server redirect is a server change. Section 2's copies and paper choice were never in the print window, since each sheet fixes its own paper and the copies are chosen in the viewer or the driver, so there was nothing to carry over and nothing was added.

Alan: "Should the target library and print dialogue window be two separate screens? They seem to fill
the same role and should be merged. I like the target library layout where targets are grouped and
categorized."

Merge them. The library's grouped and categorised layout is the part he likes and it is the part that
survives.

## 1. The screen

One screen, named **Targets**. Two regions:

- **The library**, as it is today: grouped and categorised, with the description under each sheet
  saying which job and which distance it is for. This is the primary region and it keeps its layout.
- **The selected sheet's panel**: the preview, the print settings and the Print action. It fills with
  the selected sheet and shows nothing useful when nothing is selected.

Selecting a sheet fills the panel immediately. No second window opens at any point in the flow.

## 2. Nothing the print dialog does today may be lost

Enumerate what `PrintWindow` does before you start, and hold the merged screen to that list:

- paper size and orientation, and the refusal when the chosen paper cannot hold the sheet
- copies
- the no-scaling instruction to the PDF viewer
- the printed instruction along the bottom of the sheet, and the ruler check
- the scale check bar
- which bull set, and any other per-sheet option
- the printed markers and codes
- Save PDF as well as Print, and the Linux and macOS viewer path from question 31

Anything on that list that the merged screen cannot do is a regression, and the entry is not done.
Do not add a capability that does not exist today, such as printing several different sheets in one
job, unless it already exists.

## 3. The tour

`/tour/library/` and `/tour/print/` become one page. Redirect the old print address to it so no link
breaks, and note in `website/tour.json` that the two screens merged, because a reader who saw the old
tour will wonder. Entry 152 rewrites the text on both of these pages, so do entry 152 first and then
merge, not the other way round, or the corrected text will be written twice.

## 4. The screenshot job

The weekly screenshot job from entry 144 takes its screen list from the same source the tour does.
Update that list, so the merged screen is captured under one name and no job goes looking for a screen
that no longer exists.

## 5. Remove the old code

The old print dialog is deleted, not left unreachable. A window nothing opens is a window nobody
maintains and it will drift out of step with the one that replaced it.

## 6. Tests

1. The acceptance tests that covered the print dialog now cover the merged screen. Not deleted,
   repointed.
2. `NothingIsCutOffTests` covers the merged screen at both window sizes, since it is now denser than
   either screen was.
3. A test that no code path opens a separate print window.

## 2026-09-24, entry 166: the macOS tester's answers, two defects, and the platform statement

**Status: actioned 2026-09-24**, sections 1 to 4. **Not done:** section 3.2's measurement on a Mac, which needs the tester (request 16); section 3.3 is read from Avalonia and not measured on a precision touchpad; section 4's claims register line waits on entry 159, which builds `docs/CLAIMS.md`; section 5 waits on request 16, because the project keeps no list of testers.

The tester from entry 164 answered the checklist in entry 163 section 6. Together with his diagnostics
report, this is the first real evidence about macOS. Act on it after entry 164.

## 1. His answers

| check | answer |
|---|---|
| Gatekeeper | blocked on first launch; the `xattr` command on the download page cleared it |
| open the sample, detect, accept and analyse | works, and he called it fast |
| undo with Command Z | **does not work** |
| pinch zoom on the trackpad | **does not work** |
| Command Q | quits |
| print a target to PDF | works |
| text sharpness at Retina scale | sharp |
| anything odd or un-Mac-like | nothing |

He did not say whether two finger drag pans, and did not check the PDF's printed scale with a ruler.
Claim neither.

## 2. Defect: Command Z does nothing on a Mac

The planning session checked the code, and the cause is plain. `MainWindow.cs` line 3681:

    bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);

On macOS the Command key arrives as `KeyModifiers.Meta`, not `Control`. So every shortcut built on
`control` needs the Control key on a Mac, which no Mac user will press. Undo, redo and every other
modified shortcut are affected, not only undo.

1. Use the platform's own command modifier, which Avalonia exposes through the platform hotkey
   configuration, rather than hard coding Control. Every modified shortcut goes through one helper.
2. The on screen labels follow it. Line 515 hard codes "Ctrl Z". On macOS show the Command symbol or the
   word Command, from the same helper.
3. A test on each platform's modifier: the helper returns Meta on macOS and Control elsewhere, and no
   shortcut handler reads `KeyModifiers.Control` directly. A source test that fails on any direct read
   is the cheap way to hold that.

His "I wasn't sure what it would undo anyways" is also worth hearing: undo has nothing to show for it
until a mark has been changed. The undo control should be visibly disabled when there is nothing to
undo, and its tooltip should name what it will undo, such as "Undo: move shot 6".

## 3. Defect: pinch zoom was never built

There is no pinch or magnify gesture handling anywhere in `src/`. It was not broken on the Mac; it does
not exist on any platform.

1. Handle Avalonia's pinch gesture on the sheet, zooming about the point between the fingers.
2. **Resolve the conflict entry 163 section 1 created.** That section said a two finger trackpad drag
   always pans and a scroll always zooms. On a Mac trackpad, a two finger drag *is* a scroll, so those
   two rules contradict each other. The convention a Mac user expects is: two finger drag pans, pinch
   zooms, and Command with scroll zooms. A mouse wheel on Windows is expected to zoom. Find out what
   Avalonia actually delivers for a trackpad scroll against a wheel notch on each platform, measure it
   rather than assume it, and choose behaviour per input device where the platform lets you tell them
   apart. Report what you found and what you chose.
3. Check Windows precision touchpads as well, which deliver pinch differently again.

## 4. The platform statement

`docs/PLATFORM-SUPPORT.md` and every copy generated from it say the macOS builds have never been run on
a Mac. That is now false. Replace the macOS paragraph with a statement of exactly what has been checked
and nothing more, keeping entry 147's rules: no "I" or "you", and the developer is "they".

The substance, to be worded in the statement's own style:

- The Apple Silicon build, nightly 93, has been run by one tester on one MacBook Pro with an M5 Max,
  macOS 27, natively rather than under Rosetta.
- macOS blocks the first launch, and the command on the download page clears it.
- Opening, detecting and analysing the published sample, saving a session, printing to PDF, quitting
  with Command Q and the diagnostics report all worked, and text is sharp on a Retina display.
- Known problems: Command shortcuts such as Command Z do not work yet, and pinch zoom is not built on
  any platform.
- **The Intel build has never been run on a Mac.** Keep saying so.
- It remains a test build: never offered by the updater, and the developer still does not own a Mac.

Remove the known problems line when sections 2 and 3 ship, in the same commit as the fix. Put this
statement in the claims register of entry 159 with its evidence: this entry and the report in entry 164.

## 5. Thank the tester in the right place

If the project keeps a list of testers or contributors, add the macOS tester there by the name Alan
gives, or as an anonymous macOS tester if Alan gives none. Do not invent a name. Put the question in
`docs/notes/for-alan.md` rather than guessing.
## 2026-09-23, entry 154: a word a shooter does not know gets an explanation where they meet it

**Status: actioned 2026-09-24**, every section. Graph axis labels drawn inside a chart carry no affordance of their own, because a drawn word is not a control; each chart's title does.

Alan: "I think anything like the word Sigma that is not commonly understood by a layman in either the
website or application should create a tooltip popup that explains what it means."

`docs/GLOSSARY.md` already exists and already does the hard half of this: twelve figures explained in
plain words, written from one list in the source so the page and the screen cannot disagree. This entry
widens it from the figures panel to everywhere the words appear, and adds the words that are not
figures.

## 1. One source, wider scope

Keep the existing arrangement, which is right. Widen what the list holds:

1. Every entry keeps its plain sentence. Add, optionally, a second sentence with the precise definition
   for a reader who wants it, and a link to the research article that covers it where one exists.
2. Add the terms that are not figures. A starting list, which is not exhaustive and you should add to
   it as you sweep:
   sigma, Rayleigh sigma, radial standard deviation, standard deviation, mean radius, CEP, extreme
   spread, group size, MOA, mil, confidence interval, sample size, degrees of freedom, chi squared
   interval, F test, bias correction, c4, dispersion, point of aim, point of impact, zero, zero
   correction, scale, calibration, DPI, perspective correction, keystone, quarter point, doubles,
   review queue, detection, marker, code, bull, subgroup, load, string, hit probability, ballistic
   coefficient, muzzle velocity, velocity SD, drop, wind deflection.
3. A term whose plain sentence needs a term that is itself in the list is fine. Link it.

## 2. On the website

1. The first appearance of a glossary term in the body of a page gets a dotted underline and shows the
   plain sentence on hover **and on tap**. Tap matters: phones have no hover, and a tooltip that only
   works with a mouse is a tooltip that does not work for half the readers.
2. The popup is dismissible, does not cover the sentence it explains, and carries the link to the full
   entry.
3. Only the first appearance per page is marked. A page where every instance of "sigma" is underlined
   is unreadable.
4. A reader with JavaScript off still sees the term as a link to the glossary entry. The tooltip is the
   enhancement, not the mechanism.

## 3. In the application

1. The same terms in the analysis panel, the statistics table, the graph titles and axis labels, the
   review queue and the settings get an information affordance showing the same text from the same
   source.
2. It is reachable by keyboard, not only by pointer.
3. It reads from the same list the website reads from. If the two ever load from different files, this
   entry has not been done.

## 4. Where the word should not be there at all

While sweeping, some of what you find will not need a tooltip, it will need plainer words. A tooltip is
for a term the reader has to learn because it is the name of the thing. It is not a way to keep jargon
that could simply be replaced. Where the plain phrase would do, use the plain phrase and report which
ones you changed.

## 5. Tests

1. A term in the list that appears in the application or on the site with no affordance is a failure,
   and the failure message names the file and the term.
2. A term in the list that appears nowhere is reported, not failed. It may be waiting for a feature.
3. An affordance whose text does not match the list is a failure. That is the drift this is meant to
   prevent.
4. Every entry has a plain sentence that contains no other capitalised jargon and no symbols. A plain
   sentence written in jargon is the usual way a glossary fails.

---

## 2026-09-23, entry 159: audit the website, the README and the application for claims that are not true

**Status: actioned 2026-09-24**, every section; begun 2026-09-23 in 227917a. It also carries entry 166 section 4's line: the platform statement is in the register, backed by entries 147 and 166. **Not done:** the GitHub release bodies already published are not edited, by the rule that a published release is never quietly corrected; the next nightly's body is generated from the corrected sources.

Alan: "The website and readme should be scrutinized for contradictions that I may have missed as well."

Entry 152 fixes two claims he caught by reading one tour page. One of them is contradicted by a
research article the same site publishes. Two found by eye on one page is not a good rate, and the
right response is not to read harder, it is to build the thing that makes a contradiction fail a build.

Do this after entry 153, so the research standard is in place before the articles are audited, and
before entries 155 to 158 write a great deal of new text.

## 1. Build the claims register first

Read every published surface and write down every factual claim it makes. The surfaces are:

- `website/**`, every page, including all thirty research articles and every tour page
- `README.md` and every other markdown file at the repository root
- `docs/**`
- every string a user reads inside the application, which `OneWayOutTests` and the glossary work of
  entry 154 already give you a way to enumerate
- the release notes and the release bodies on GitHub
- `docs/PLATFORM-SUPPORT.md` and every place entry 147's statement is repeated

A claim is any sentence that asserts something checkable: what the software does, what it cannot do,
a number, a measurement, a limit, a version, an address, a licence, a platform, a name.

Write them into `docs/CLAIMS.md`, one per line, with the file and line it came from.

## 2. Classify every claim

For each one, decide which it is and record it:

1. **Backed by code.** Name the file and the symbol. A claim about behaviour whose code cannot be found
   is not backed, it is remembered.
2. **Backed by a measurement.** Name where in `docs/PHASE0-RESULTS.md`, `docs/PHASE1-RESULTS.md` or
   `docs/STATISTICS.md` the measurement lives, and the date it was taken.
3. **Backed by a decision.** Name the entry in `docs/NOTES-FROM-PLANNING.md`.
4. **Unbacked.** Nothing found. This is the list that matters.

Every unbacked claim gets a line in the report saying what it would take to back it, and then one of
three things happens: it is verified and moved to a backed category, it is reworded so that what it
says is what is true, or it is deleted.

## 3. Then look for the contradictions

With the register built, the contradictions fall out of it rather than having to be spotted by reading.
Look for these shapes in particular:

1. **Two claims that cannot both be true.** The print scale pair in entry 152 is the example: the tour
   says a scale error cannot be recovered and `printer-true-size` says GroupLab detects and corrects it.
2. **A number that appears more than once with different values.** Calibre diameters, tolerances,
   sample sizes, the print scale figures, version numbers, file size limits, the number of articles,
   the number of target sheets, the number of supported platforms.
3. **A claim of a feature that no screen reaches.** Question 37 is exactly this: the point of impact
   correction was built, correct, and reachable from nothing. Anything the site says a shooter can do,
   a shooter must be able to find. Check every "you can" sentence against a screen.
4. **A sentence that appears in several places in slightly different words.** That is a contradiction
   waiting to happen even if the versions agree today. The platform statement from entry 147 is
   supposed to come from one source; verify that it actually does, everywhere, including the GitHub
   release bodies.
5. **Stale counts and stale states.** Anything that says "eighteen articles", "twenty two sheets",
   "nightly NN", "currently", "at the moment", or "not yet". Each one is either generated from the
   thing it counts or it is wrong on some future day.
6. **A claim about another product or another person's work.** Check it is accurate and check it is
   necessary. Remove any comparison that is not both.

## 4. Fix, with the same rule as entry 145

Every wrong claim is corrected in plain words. Where a claim was wrong because two places each wrote
their own version of the same sentence, the fix is not to make the two versions match; it is to make
one of them the source and have the other read from it. Matching by hand is how they came to disagree.

## 5. What stops it happening again

1. **One source per repeated statement.** Extend the mechanism already used for the platform statement
   and for `website/links.json` to every statement that appears in more than one place. A drift test
   for each, the way entry 147 did it.
2. **Generated counts.** Any number that counts a thing in this repository is computed at build time
   from the thing it counts. Never typed.
3. **The banned sentence list grows.** It already holds entry 145's filler sentence and gains entry
   152's two claims. Add every false claim this audit finds, so the exact wording cannot come back.
4. **A claims register test.** `docs/CLAIMS.md` lists every claim with its backing. The build fails on
   a claim marked unbacked, and fails on a published page whose checkable sentences are not in the
   register. That is the check that makes this audit worth doing once rather than every quarter.
5. **The register is part of the definition of done.** A new page or a new article adds its claims to
   the register in the same commit, the way a new figure adds its caption.

## 6. Report

In the run report: how many claims, how many in each of the four categories, how many contradictions
found, and the full list of what was corrected. If the unbacked list is long, say so plainly rather
than working through it quietly; the size of that list is itself the most useful finding here.

---

## 2026-09-24, entry 185: the releases page shows two copies of nightly 94 and a test data release

**Status: actioned 2026-09-24**, sections 1 to 3. **Waiting on Alan:** section 2's one command, making the release a draft, is request 17, because it needs his approval; everything it depends on is pushed. Section 1.3 finishes itself: the next nightly replaces the rolling release, and with it the stale platform line.

Alan, looking at the releases page after entry 168's rewrite: he could not tell why nightly 94 exists or why
it has no notes, and the page lists three releases that look alike. Nightly 94 itself is explained (entry 168:
the old gate counted a non shipping path as the application, and its rewritten body now says honestly that
nothing changed). What is still confusing is the page. Do this after entry 184; it is small.

## 1. The rolling `nightly` release looks like a second build

The page shows "GroupLab 0.2.0-nightly.94" (tag `v0.2.0-nightly.94`) **and** "GroupLab nightly,
0.2.0-nightly.94" (tag `nightly`), with the same commit and the same body. To a reader that is two builds.

1. Say what uses the `nightly` tag release: the updater, the download page, or nothing. If nothing needs it,
   stop creating it and delete it; that deletion is Alan's approval, so put it in `panel.md`.
2. If something does need it, retitle it so it cannot be mistaken for a build: **"Latest nightly (always the
   newest build, moves with every build)"**, with a one line body linking to the numbered release it points
   at, and none of the notes repeated.
3. Its body still carries the platform line saying macOS has never been run on a Mac. Entry 166 corrects the
   statement; make sure this release is regenerated from the one source when it does, like every other copy.

## 2. The test data release sits among the builds

"Test data, not a build", tag `test-data`, from entry 171 section 6, is listed as a pre release between two
builds. It is correctly named, but it should not be on a page people read to find builds.

Move it out of sight without losing what it does: a **draft** release is invisible on the public page, and CI
can still fetch its assets with the workflow token. Check that works, then make it a draft. If it does not,
say so and propose where else the test files can live. Do not move the files anywhere that puts them back
into the repository's history.

## 3. The next nightly

Tonight's work includes real changes to the application: pan by default, the calibre names, the freezes,
the zero correction's distance, the scan in real inches. So the next scheduled nightly will be built, and its
notes must list them under "What you will notice" in plain words. Check that before it publishes; it is the
first nightly since entry 168 fixed the gate and the notes, so it is the proof both fixes work.

---

## 2026-09-24, entry 184: every published build announces itself in Discord, with what changed

**Status: actioned 2026-09-24**, sections 1 to 3. Section 3.3 closed with nightly 95: #builds answered HTTP 200 to its embed, headed GroupLab 0.2.0-nightly.95, with the build's own notes. Section 2.3 for a stable release is question 52.

Alan: "adding a discord channel that has messages automatically sent to it when new builds are published
with the changelog." Entry 148 section 5 noted a release webhook as a later item; this is that item.

## 1. Where the messages go

- A new read only channel, **#builds**, in the Information category, created by Alan. Every published build
  posts there: nightlies and releases alike. People who want every build follow it; everyone else never sees
  them in the busy channels.
- A stable release, when there is one, **also** posts to **#announcements**. A nightly never does, because a
  message a night would bury everything else in that channel.
- `#github` stays for commits and issues if Alan ever connects GitHub's own integration; this entry does not
  touch it.

## 2. How

A step at the end of the nightly workflow, and of the release workflow, that posts through a Discord
channel webhook **only after the release is actually published**. A night the entry 150 gate skips posts
nothing.

1. **The webhook URL is a secret.** Anyone holding it can post to the channel. It lives only in a GitHub
   Actions secret, `DISCORD_BUILDS_WEBHOOK`, and a second one, `DISCORD_ANNOUNCE_WEBHOOK`, for stable releases.
   It is never committed, never printed, never written to a log or to `panel.md`, and the step masks it.
   Adding a secret is a repository setting, so **Alan adds both himself**; the planning session has given him
   the steps. Never ask him to paste the URL anywhere else.
2. **If the secret is missing, the step does nothing and says so in the workflow summary.** It never fails the
   build: an announcement is not part of the product.
3. **The message:** one Discord embed with the version as its title, linked to the release page; the
   release's own notes, "What you will notice" and "Under the hood", generated by the same code as the release
   body so the two can never differ; and a link to grouplab.org/download. Plain words, the rules of entries 145
   and 168: no hashes, no file paths, no platform statement.
4. **Limits.** A Discord embed description holds 4096 characters. If the notes are longer, end at a whole line
   with "Full notes on the release page", never mid sentence. Entry 168 already fixed one set of notes cut off
   in the middle.
5. **Never duplicate.** Record which version was announced, so a rerun of the workflow does not post the same
   build twice.

## 3. Tests

1. The message is built by a function with its own tests: normal notes, notes over the limit, notes with only
   "Under the hood", and a release with no notes at all.
2. A dry run mode prints the exact JSON the webhook would receive, with the URL replaced, so the format can be
   checked without posting.
3. After Alan has added the secret, the first real nightly posts, and the report quotes what appeared.

## 4. Order

After entry 183. It is small, and nothing else depends on it.

## 5. Alan's side is done (added 2026-09-24)

Alan created `#builds` and its webhook, and added the secret to the repository. So the first nightly after
this entry lands should post for real. If `DISCORD_ANNOUNCE_WEBHOOK` is absent, stable releases simply skip
the #announcements post, as section 2 item 2 says. Quote in the report what the first real post contained.

---

## 2026-09-24, entry 169: the analysis screen, cut down to what a shooter reads

**Status: actioned 2026-09-24**, every section, the pop-out window included. Nothing left undone. Section 9 is decided by the planning session on Alan's behalf and he can reverse it: `scripts/american-spelling.py` would do it the other way round with its table turned over.

Alan's friend, the first outside user, went through the analysis screen on `Scan_20260923.png` and sent
a long, specific review with screenshots. His verdict: "All of this is just way too dense." He also said
the left side, the target picture with each shot's distance from its bull, "looks great as is". Keep the
left side exactly as it is. This entry is about the right hand panel and the plot.

Do entry 170 first; its defects are worse than the density.

## 1. What stays in view

He named what matters, and it matches what a shooter reads off any target:

1. **Center from aim**, windage and elevation.
2. **Extreme spread.**
3. **Group width by height.**
4. **Mean radius.**
5. **CEP**, 50 and 90.
6. The zero correction, per section 2.

Each shows its value, and its interval behind a tooltip or an expander rather than in a second line of
text. **Everything else goes into one collapsed section named Advanced**: sigma, the permutation tests,
stringing, the across and up and down strips, sessions over time, the flyer test, the pooled degrees of
freedom. None of it is deleted. It is out of the way until somebody opens it, and the application
remembers that they did.

This goes further than entry 163 section 5, which collapsed paragraphs but kept every section on the
screen. Where the two disagree, this entry wins.

## 2. The zero correction box

From his screenshot and comments:

1. **Offer the correction in MOA and in mil, side by side**, both always, whatever the default unit
   setting. Shooters work in both depending on the scope.
2. **The green "Dial ..." sentence repeats the numbers above it.** Replace the two lines and the sentence
   with one compact block: the offset in inches, MOA and mil, and beneath it the clicks for the scope
   the rifle profile names, with the click value stated, such as "2 clicks left at 0.1 mil".
3. **State the distance the correction is for**, in the block itself. Entry 170 section 1 explains why
   that is not cosmetic.
4. **The text below the box goes**: the pooled sigma line and the paragraph about carrying the correction
   move into Advanced or into "why".
5. **"Where it landed" is removed.** He called it basically useless, and it duplicates the main plot.

## 3. The plot: high contrast

He preferred the plot style of another program he uses, and was specific about why: "The best thing to
takeaway is the simplistic contrast ratio." Keep what GroupLab draws, and draw it that way:

1. Keep the CEP circles, the group center, and the line joining the two farthest shots.
2. Draw on white, with thin black or dark outlines at the hole size, one clear accent for the group
   center and the extreme spread line, and no faint pastel rings. Nothing thinner or lighter than can be
   read on a laptop in daylight.
3. A dark theme version that is equally high contrast, not a dimmed copy.
4. `ThemeTests` holds the plot's marks to a contrast ratio against their background, as it already does
   for the interface.

Do not reproduce the other program's layout or name it anywhere in this repository. The lesson is the
contrast, not the design.

## 4. Size and density

1. **Raise the default font size** on the analysis panel. The numbers are the product; they should be the
   largest text on the right hand side.
2. An option to **pop the analysis out into its own window**, for a second monitor. Lower priority than
   everything else in this entry; do it last or leave it for a later entry if it is large.

## 5. The registration badge

The header shows "registered, residual 0.004 in", and hovering shows a paragraph starting "From the
sheet's own printed markers: 38 of 38 markers found...". His reaction: "wtf does this info even mean? And
is it useful? Yeet this info."

1. The badge becomes plain: **"Scale checked"** with a check mark when registration succeeded, and a
   plain warning when it did not. That is the only thing a shooter needs from it.
2. The paragraph moves behind Show work, where somebody who wants it can find it. It also still carries
   the old calibre wording that entry 161 corrects, so check it after 161 has landed.

## 6. A back button

From the analysis screen, a person can return to marking only by clicking the image name in the
breadcrumb. Add a plain **Back** button, top left, that returns to the marking screen with everything as
it was left.

## 7. Right drag pans, and the shortcuts are visible

1. **Right button drag pans in every tool**, as middle button drag already does per entry 163. A right
   click without movement is left free for a context menu later.
2. **Holding Alt shows every button's shortcut** as a small label under it, and releasing Alt hides them.
   The keyboard strip along the top stays.

## 8. Export and import

"idk how useful the json file generated from an export is for any other program."

1. **Export shot coordinates as CSV**: one row per shot, with shot number, bull, x and y from the aim
   point in inches, and the same in MOA and mil at the stated distance, plus a header naming the units
   and the distance. That is what other tools and spreadsheets read. Keep the JSON export; it is the
   complete record and entry 165 uses it.
2. **Import shot coordinates from a CSV** exported by other target analysis software, with a column
   mapping step that asks which column is x, which is y and which unit they are in. Do not name or target
   any one program's format; a mapping step covers them all.

## 9. American spelling

"Center from aim (program spells it centre...)". The application and the site use British spelling:
centre, calibre, analyse, colour. GroupLab's users are overwhelmingly American, and its defaults are
already inches, yards and MOA. **Every string a user reads, in the application and on the site, uses
American spelling**: center, caliber, analyze, color. Code identifiers are not renamed; that is churn with
no reader. Tests that pin user facing text change with it. Add a test that fails on the British forms in
user facing strings, with an allowance for quoted material.

This is decided by the planning session on Alan's behalf; he can reverse it.

---

## 2026-09-24, entry 183: virus scanning works, and every opted out submission is refused

**Status: actioned 2026-09-24**, sections 1 to 4 in the repository. **Not done here:** installing the worker and moving the refused submission back, which run on the server and are request 15. The pull script's own check runs in the consent test; its SSH transfer does not, because nothing in CI can reach the server.

Do this **immediately**. It is a one line class of fault, and until it is fixed nobody who ticks "Do not
include my photos in the public data set" can send a target.

## 1. What happened

After request 14 (clamd's limits to 400M, debconf management off, the streaming worker installed), Alan
sent a photograph through grouplab.org/targets with **both** boxes ticked, at 04:25 Mountain. The worker's
log:

    2026-09-24_272b33e2: rebuilt 001_20260921_231821.jpg as 001_20260921_231821.png, original deleted, clean, clamdscan
    2026-09-24_272b33e2: refused, DO-NOT-PUBLISH would not decode cleanly: UnidentifiedImageError: cannot identify image file '.../DO-NOT-PUBLISH'

**The first line is the good news: the scan ran and came back clean, through `clamdscan --stream`.** Entry
182 works, and every upload from now on is virus scanned. Close request 14 and record it.

## 2. The fault

`website/api/upload.php` line 705 writes a marker file named `DO-NOT-PUBLISH` into the submission folder
when the opt out box is ticked. The worker treats every file in the folder except `meta.json` as an image to
rebuild, so it tries to decode the marker, fails, and refuses the whole submission. **Every opted out
submission is refused, always.** None of the earlier tests had the box ticked; the first three uploads
tonight did not, which is why this is only appearing now.

Two different pieces of code disagree about what a submission folder may contain, which is the same class
of fault as entry 174's field name and entry 177's `stored` against `stored_name`.

## 3. The fix

1. **One source of truth for the opt out.** `meta.json` already carries `exclude_from_public_dataset`. Decide
   whether the marker file is needed at all. If it is kept, as a belt and braces flag that survives a lost
   `meta.json`, the worker must know it: skip it when choosing files to rebuild, carry it through to
   `ready`, and fail loudly if the marker and `meta.json` ever disagree.
2. **The worker only rebuilds files the receiver recorded as uploads**, from the list in `meta.json`, never
   "every file in the folder". An unexpected file is a refusal with a clear reason, not a decode attempt.
3. `Get-TargetSubmissions.ps1` and the build that publishes images both honor the opt out, and the build
   refuses to publish anything from an opted out submission, whichever of the two records says so.
4. **A contract test** that runs the real receiver, then the real worker, then the real pull script on one
   submission with the opt out ticked and one without, and checks each arrives with its consent intact.
   That single test would have caught this fault, entry 174's and entry 177's.

## 4. The refused submission

`2026-09-24_272b33e2` is in `refused/` now, with its rebuilt and scanned PNG, its `meta.json` and the marker.
Refused folders are kept seven days. After the fix is installed, give Alan the commands to move it back to
`quarantine/` and touch it, so the worker processes it again. Its original was already deleted, so the
worker will be rebuilding the PNG it produced itself; make sure that path works, or pass it straight to
`ready` if the worker can tell it has already been rebuilt and scanned.

## 5. For Alan

The fixed worker, and the receiver if it changes, need copying up and installing the same way as request 14.
Put the commands in the panel and in `panel.md`, with the move back of section 4 after them.

---

## 2026-09-24, entry 182: clamd cannot scan a file handed over from the worker's sandbox

**Status: actioned 2026-09-24**, sections 1 to 4 in the repository. The server side, clamd's limits and the new worker, is request 14.

- **Section 3.1.** `clamdscan --stream`: the bytes go over the socket, clamd opens nothing, and the sandbox and AppArmor are unchanged.
- **Section 3.2.** A file over clamd's limits is never called clean; the worker says so loudly with the size and the lines to raise.
- **Section 3.3.** Request 14 and the panel: back up to the server folder, show the four values, stop debconf regenerating the file,
  change one line each, restart, prove with a 60 MB file. The limit is 400M for `StreamMaxLength`, and for `MaxFileSize` and
  `MaxScanSize` too, with `AlertExceedsMax yes`, because a file over either of those is otherwise skipped and reported clean, which the
  entry did not name and would have made streaming look like it worked. Derived from the worker: the rebuilt PNG at 120 megapixels,
  three bytes a pixel, is about 361 MB.
- **Section 3.4.** `install.py --intake` refuses while clamd.conf says less, naming each line; `WorkerLimitTests` holds the worker's
  figure and the installer's together.
- **Section 3.5.** The CI worker test runs under the unit's own mount sandbox now, `ProtectSystem`, `ProtectHome`, `ReadWritePaths` and
  `PrivateTmp`, which is what caused the refusal, with clamd's limits set as on the server and a photograph whose rebuild is over 25 MB.

---

## 2026-09-24, entry 172: ground truth for the 2026-09-20 ST-4 target, the first real material for entry 158 program A

**Status: actioned 2026-09-24 in part.** Sections 1, 2 and 3.3 are done. **Not done: section 2 item 4 and section 3 items 1, 2, 4 and 5**,
and with section 3 item 1 the entry 149 section 4 re-run that entry 171 attached to it. Every one of them needs hole positions on a
commercial sheet, and GroupLab has no way to find them on a sheet it did not print: no hole detection for it and no grid registration.
Entry 157 section 4 builds the grid registration and entry 158 program A the rest; they carry these, in the queue. Entry 157 built the corner, angle and perspective pieces without the grid: the ST-4's paper cannot be outlined on the white board it was shot on, and its grid is not detected, so section 3 item 4 still waits, with 158. **Updated with entry 158:** section 3 item 4 is done, the grid registered on eight of nine frames with and without the lens; items 1 and 2 are measured with the scan detector, which finds about one shot in seven in the photographs; section 2 item 4 and section 3 item 5 wait on request 19, a scan of the sheet.

- **Sections 1 and 2.** `tests/GroupLab.Core.Tests/Analysis/st4-2026-09-20.json` holds the twenty groups, 115 shots, the answers and
  the unlocated primer mix, the annotated photograph as evidence by hash, and every frame by hash. `185944` and `185946` are the same
  sheet, near square on and from further back, and are included. `St4GroundTruthTests` holds it. Request 3 was closed under entry 171.
- **Section 3.3.** What a shooter must do by hand on this sheet today is recorded in `docs/PHASE1-RESULTS.md`: twenty separate markings,
  about 215 clicks, because a sheet GroupLab did not print has one point of aim.

---

## 2026-09-24, entry 170: two freezes, a zero correction that did not say its distance, and hole centres a person had to move

**Status: actioned 2026-09-24**, all four sections, with section 4's choice of centre left open. Section 4.4, a person's click measured, is
request 9, and which centre GroupLab reports is question 51, which waits on it. Entry 149 section 3 A is built here; D is question 50.

- **Section 1.** The verdict says the distance: "Dial 2 clicks left ..., for a zero at 25.4 yd." Where the rifle is zeroed elsewhere, a
  second line carries the correction to that zero through the solver, allowing for where the bullet should be at the distance shot, or
  names what carrying it needs. Nothing on the screen said 100 yards; the arithmetic used his 25.4, and what he read as a 100 yd
  correction was a 25.4 yd zero correction with no distance on it. The distance box took a typed number only on Set, so a copied 100 yd
  could stay in use; Enter and leaving the box take it now. 0.83 MOA and two 0.1 mil clicks are pinned by a test.
- **Sections 2 and 3.** Profiled on the friend's scan. Refresh took 1.3 s, naming the bulls 2.5 s, an exclusion 5.5 s. The cost was the
  correlated normal CEP asking for more precision than a double holds (a million-point integral, 0.29 s a call, twelve a refresh), the
  worst-shot simulation (0.11 s) and the aspect median (0.11 s) redone on every edit, and a second refresh after naming the bulls. Now
  refresh takes about 0.1 s, the naming 0.2 s and an exclusion 0.18 s; a test holds each under 400 ms on any runner. The rule solve is not
  combinatorial: 22 ms for ten bulls, 14 ms for twenty five.
- **Section 4.** Systematic, not random: the reported centre leans toward the scanner's shadow on all six scans, 0.011 in on average.
  `HoleEdgeFit` measures it, `HoleCentreAgreementTests` holds today's agreement so a change that worsens it fails, and question 51 has the
  two replacements tried and why neither is adopted before request 9's hand markings.

---

## 2026-09-24, entry 181: Windows line endings broke the intake worker on the server

**Status: actioned 2026-09-24**, sections 1 to 4. Alan's hot fix of section 3 put the server right; nothing more is needed there.

- **The cause was my editing, and it is worth knowing.** Python's `write_text` on Windows writes CRLF, so every file under
  `website/server/` I changed through a script this session came out CRLF in the working tree while git stored LF. Alan copies the
  working tree, so the server got CRLF.
- **Section 4.** `website/server/** text eol=lf` and `*.sh text eol=lf` in `.gitattributes`, the working copies rewritten LF, the
  installer refuses any file holding a carriage return, naming it, and `SiteSyncTests.EverythingCopiedToTheServerIsLf` fails on one. No
  script in `scripts/` goes to the server. No request in `for-alan.md` needs to mention line endings.

---

## 2026-09-24, entry 180: Alan does not read the panel, so everything he needs goes where the planning session reads it

**Status: actioned 2026-09-24**, sections 1 to 4.

- **Section 1.** `docs/notes/panel.md`, in `.gitignore`, mirrors every message put in the panel for Alan, newest first, from entry 171
  on, and takes the reason for any command that will stop for his approval before it runs.
- **Section 2.** Requests 1, 2 and 10 are marked answered; `for-alan.md` starts with the open count, 4, and the most urgent, 11.
  `ForAlanTests` holds the count to the requests.
- **Section 3.** STATE.md is rewritten now and at the end of every entry from here; `CLAUDE.md` says so.

---

## 2026-09-24, entry 179: 18 GB of scratch files, and temporary files that clean themselves up

**Status: actioned 2026-09-24**, sections 1 to 4 and the amendment's section 1.1. The scratch area went from 18 GB to 707 MB.

- **Section 1.** The 18 GB was this session's own scratch work: 14 copies of the App test build (9.9 GB), repository clones and rewrite
  copies (1.6 GB), downloaded release assets (1.1 GB), rendered sheets and research folders (about 3 GB). In `%TEMP%` the suite had left
  14,987 settings files (956 MB), 60 bench folders and 4,301 empty random folders. Details in `docs/PERFORMANCE.md`.
- **Section 2.** 927 scratch entries, 18.5 GB, listed and deleted in one command; three in use kept. Every test process now writes into
  its own `grouplab-tests/<run>` folder, removed at exit. The random folders come from `dotnet test` itself, two a run before any test
  code, so the runner is given its own temp folder too. CI fails if the suite leaves anything; `TestTempLeakTests` holds the redirection
  and that no scratch test remains. `scripts/clean-scratch.py` removes earlier sessions' folders idle for seven days: 15 went today.
- **Section 1.1, the amendment.** Done by me, in one listed command: 10 earlier session folders untouched for a day, 15,456 `grouplab-*`
  entries the suite had left in `%TEMP%`, and 4,292 empty random folders from `dotnet test` since 2026-09-13, 1.45 GB. The four session
  folders touched within a day, three older empty folders and the .NET installer's caches were left alone.
- **Section 3.** The rule is in `CLAUDE.md`, and the before and after in `docs/PERFORMANCE.md`: 18 GB to 707 MB, unchanged by a full
  Core run.
- **Section 4.** Confirmed: not this repository's code, `dotnet test`.

---

## 2026-09-24, entry 178: pissinhot.com/targets redirects to grouplab.org, and a backup nginx loaded

**Status: actioned 2026-09-24**, sections 1 to 4. **Entry 129 is complete.** Removing the 18 old submissions from pissinhot.com is
Alan's, when he chooses: request 12.

- **Section 1.** Recorded, including the one 200 straight after the graceful reload.
- **Section 2.** The rule is in `CLAUDE.md` and `docs/WEBSITE.md`: nothing but the include is ever written into a HestiaCP
  `conf/web/<domain>/` folder. `install.py` had the fault waiting: it backed up what it replaced beside itself, which for
  `nginx.ssl.conf_grouplab` would have been a live `nginx.ssl.conf_grouplab.<time>.bak`. No such file exists today, as Alan's listing
  shows, because the include has only ever been installed fresh. Its backups of anything in that folder now go to
  `/home/airwolf/backups/grouplab.org/config/`, and `SiteSyncTests` holds it. Request 1 is corrected to the commands that worked.
- **Sections 3 and 4.** Request 1 is closed, and the last pull found nothing new.
- **Section 5, added afterwards.** Request 10 is closed: the server's sync matched the repository at 03:37 Mountain. The installer now
  keeps only its newest dated backup of each file it replaces, and leaves a copy made by hand under another name alone.

---

## 2026-09-24, entry 177: the first real submissions arrived, and two defects on the way

**Status: actioned 2026-09-24**, sections 1 to 4. Removing the two read submissions from the server runs sudo there, so it is Alan's,
in request 1 with the command; the orientation and contract checks on real worker output run in CI's new worker job.

- **Section 1.** One check, `scripts/SubmissionCheck.ps1`, reads `stored` or `stored_name` and never lets a folder pass as a file. The
  pull script and the removal script both use it, and CI runs it on meta.json as the real worker writes it. The removal script had the
  same fault reading only `stored`, and a second one: it looped over the ledger object instead of its submissions list, so it had never
  run against the ledger as written. It gains `-Only` for naming the submissions to remove. Both pulled submissions verify and are in
  the ledger.
- **Section 2.** The worker writes Orientation 1 on the pixels it turned upright and keeps the camera's value only in the record.
  `ImageScrubber` does not have the fault: it copies the compressed pixels untouched, so the original tag is right there. The pulled
  photograph's tag is rewritten to 1 locally, pixels unchanged, with `orientation-fix.json` beside it.
- **Section 3.** The test image is marked a test and never to be published; the phone photograph is not added to any public set.
- **Section 4.** Request 1's steps 1 to 3 are closed.

---

## 2026-09-24, entry 176: the intake worker was killed for memory, so no submission reached ready

**Status: actioned 2026-09-24**, sections 1 to 9 in the repository. The server side, the ClamAV daemon, HEIC decoding, the committed
worker and removing the 3G drop-in, is request 11 and commands in the panel. Section 7's real worker test runs in CI; this machine has no
systemd or ClamAV, so CI is its first run.

- **Sections 2 to 4.** The daemon, as section 8 settles: the worker uses `clamdscan --fdpass`, so clamd never needs to read quarantine, and
  the unit allows Unix sockets only. The pixel cap and the memory limit are derived together: three copies at four bytes a pixel, 120
  megapixels, `MemoryMax=1600M`. `WorkerLimitTests` holds them to each other.
- **Section 5.** Attempts are counted before each run, so a kill counts; three and the submission goes to refused with `refused.txt`.
  One log line per submission per run. The pull script says how many wait in quarantine and how old the oldest is.
- **Section 9.** The installer checks Pillow, heif-convert, clamdscan and a daemon that answers, naming the package for each. HEIC goes
  through `heif-convert` from `libheif-examples`, because Ubuntu 24.04 has no Pillow HEIC plugin. A scanner that did not complete is
  recorded per file and counted by the pull script. Nothing in quarantine is ever deleted for age.

---

## 2026-09-24, entry 175: the site sync rolled back every deploy from 02:36 Mountain, and why

**Status: actioned 2026-09-24**, sections 1 to 5. Section 4.5, replacing Alan's hot fix with the committed script, is request 10 and a
command in the panel.

- **Sections 1 to 3.** Recorded as found. I had reached the same cause from the sync log under entry 174 and set the window to 80
  seconds in `a77a1c7`; this entry's values replace that.
- **Section 4.** `CHECK_TRIES = 12` and `CHECK_WAIT_SECONDS = 10`, exactly as on the server. The window is derived, not guessed: the sync
  reads `open_file_cache_valid` from `/etc/nginx/nginx.conf` when it runs and waits that long plus 30 seconds, falling back to the fixed
  window where it cannot read it. Nothing reloads nginx or changes its settings. `SiteSyncTests` holds the values, the margin, the read,
  and that the one nginx path is read and nothing else about nginx appears in the code.
- **Section 5.** `317932e` was the first deploy to pass, at 02:54 Mountain on its first attempt, and `a77a1c7` passed at 02:59 the same
  way. The log does not say whether the hot fix was in place by then, because a first-attempt pass reads the same with either window;
  it passed because nobody had read the home page in the half minute before. `/targets/` carries `name="photos[]"`.

---

## 2026-09-24, entry 174: the upload page refused every photograph, because the file field had no brackets

**Status: actioned 2026-09-24**, sections 1 to 3. Section 4, the live test again, is Alan's, and the planning session asks for it once the
fix is published.

- **Section 1.** Confirmed as the entry says: `name="photos"` gave PHP one file's strings, and the receiver refused every submission as
  having no photos. **The page refused every submission from entry 173 opening it until this fix, and no submission was lost, because
  none was ever accepted.**
- **Section 2.** The input is `photos[]`, and the receiver also takes one file under the plain name. The crash receiver reads a single
  file named `report`, which is what the application sends, so it does not have the fault.
- **Section 3.** The site build fails if the built page's file input is not named with `[]`, and `SendATargetTests` says the same. A new
  PHP test serves the real receiver with `php -S` and sends it a real multipart POST built from the page's field name, one file, two
  files and the plain name; CI runs it beside the receiver tests. PHP is not installed on this machine, so CI is its first run.

---

## 2026-09-24, entry 164: the first macOS log, and two things it raises that need no tester

**Status: actioned 2026-09-24**, all four sections.

- **Section 1.** Recorded; nothing to do about the updater.
- **Section 2.** Windows reads 33 of 34 too. Marker 28 is printed with the printer's banding across it. Recorded with the sample's ground truth, and a test holds it on all three CI platforms.
- **Section 3.** Not a slowdown: the baseline was a scan with no holes. On the same file the M5 Max is about a fifth faster. No x86 intrinsics, no ReadyToRun on either platform, and both are faster on the second run.
- **Section 4.** The log records the extension and the path hash, never the name, amending entry 41 section 3.

---

---

## 2026-09-24, entry 173: open the target upload page, and put it in the top bar

**Status: actioned 2026-09-24**, sections 1 to 3, with two parts waiting on Alan rather than on a decision: section 1.3's end to end test
needs a person in a browser, because Turnstile is there to stop scripts, and its pull and removal run `sudo` on the server; section
2.4's redirect is his to run. Both are in request 1 of `docs/notes/for-alan.md` with the commands, and the redirect is in the panel.

- **Section 1.** `open` is true. The test image is generated and labelled as not a target.
- **Section 2.** The page is at `/targets/`. The old path answers with a plain page linking there, with no refresh and no nginx change.
  The top bar says "Send a target" while the page is open and is unchanged while it is closed; the donor pack page keeps its footer
  place and gains a "Send your target" button. The redirect's commands now point at `/targets/`. The printed donor PDFs are untouched.
- **Section 3.** The build checks all three and fails before publishing if any is wrong, and `SendATargetTests` holds them.

---

## 2026-09-24, entry 171: answers to questions 48 and 49, request 4 answered, and stale items closed

**Status: actioned 2026-09-24**, sections 1 to 6 and section 6's two amendments; section 7 is the order of work, which is followed. Three things wait on a later step, not on a decision:
the friend's scan is attached to the `test-data` release once `ci.yml` has created it; `.user.ini` surviving the first real deploy is
checked after this push publishes; and request 5 closes when Alan confirms.

- **Section 1, question 49.** A scan now reports real inches: every distance, bull and hole diameter is multiplied by the measured print
  scale, and the detector is given the calibre in the sheet's own inches. A photograph stays in sheet inches and says so in the entry's
  words. The reason to print at actual size is one sentence the print screen, the statement of record and the tour share. The marking
  file records which it used. Tested at 96 percent both ways round.
- **Section 2, question 48.** The fourteen day clause is gone from the split script's rule; the count stays.
- **Section 3.** The channels and the ten rules are in `website/links.json`, which the community page reads. The moderators' channel is
  not named, and a test says so.
- **Section 4.** Questions 39, 41, 42, 45 and 46 are archived with their answers, as are 48 and 49. Question 44's crash outside the page
  is the one part still open.
- **Section 5.** STATE.md is rewritten, and its test now checks the inbox line against the directory.
- **Section 6.** Alan's standing consent is in `samples/PROVENANCE.md`, quoted and dated, and the shadow crop is on the hole size
  article: one of his photographs, 0.383 in, 1.452 of the bullet. Fenix is recorded for entry 166's credit. The 59 MB scan is a
  `test-data` release download, fetched and hash-checked by CI, and no sample over about 10 MB may be committed. Request 1 says what is
  left of entry 129, with the redirect's commands. The installer's closing lines now say only what is still to do.
- **Section 6.1.** Entry 149 section 3 goes with entry 170 section 2, and section 4 with entry 172 section 3 item 1.

---

## 2026-09-24, entry 167: replace the Equipment icon

**Status: actioned 2026-09-24**, all three sections. Section 2.3, the tour page showing the new icon, is confirmed after the next weekly screenshot run.

- **Section 1.** The baked tilted cartridge, exactly as given, with no run-time rotation.
- **Section 2.** Rendered at 16, 32 and 128 in both themes and looked at; a test holds its bounds, its two parts and its colour.
- **Section 3.** The lesson is written in the icon's own comment: draw a detail of anything long and thin at this size.

---

## 2026-09-24, entry 163: a first real user's feedback on the marking screen, and a cartridge list

**Status: actioned 2026-09-24**, all seven sections, with section 1's trackpad rule and section 6's two Mac defects done under entry 166, which corrects them.

- **Section 1.** Pan selects on a click and pans on a drag, and detection no longer switches tools. The two finger and pinch rule is entry 166's.
- **Section 2.** C pans beside V; P still pans. C conflicts with nothing: not a review key, not bull entry.
- **Section 3.** Names before numbers, **reversing entries 107 and 108 where their rule is written**. Forty cartridges confirmed by two independent sources, thirty three held until a second agrees, three disagreements between the sources each keeping the cartridge out. ".223" stays a diameter.
- **Section 4.** A Setup block first in the panel, each needed field outlined and saying "needed", each answerable "not known", and Accept saying what is still needed.
- **Section 5.** Cards open as their verdict, CEP as its first line, and the cut-off tests run closed and open.
- **Section 6.** The updater offers a Mac no update; Command Q quits by the tester's report; modifiers and pinch are entry 166.
- **Section 7.** One test per offerable row of section 3.2, and the four App tests the section lists.

---

## 2026-09-24, entry 162: consent for the 2026-09-23 friend scan, and what it was shot on

**Status: actioned 2026-09-24**, all three sections, with one part of section 1 waiting on a choice: publishing the scan itself.

- **Section 1.** The consent record is written, distinguishing this scan from the 2026-09-16 one by file and date. The published copy, rebuilt from pixels, is 56 MB, so how to publish it is request 8 rather than a commit that every clone carries for ever.
- **Section 2.** Card stock over cardboard, 6.5 Creedmoor, printed at 100.3 percent, recorded with the scan and in its fixture.
- **Section 3.** The calibre's two remaining jobs hold across 0.76 to 1.14 and a test says so; paper and backing are recorded fields on every marking.

---

## 2026-09-24, entry 168: nightly 94 should not exist, its notes are cut off, and the platform statement leaves the releases

**Status: actioned 2026-09-24**, all seven sections.

- **Section 1 and 2.** Nightly 94 was built by `scripts/claims.py`, `scripts/counts.py`, `scripts/split-logs.py` and five test files, all mine. What ships is now generated from what MSBuild says the published application reads, plus what the packaging copies; tests, tools and the other workflows are checked; the site and the documents are content. CI regenerates the list and fails if it differs. Under the new classes, 9 of the last 31 nightlies changed nothing in the application.
- **Section 3.** A trailer continues until a blank line or the next trailer. The generator's own documented example wrapped, so it would have been cut too. A self-test holds four cases and a test runs it.
- **Section 4.** Only a commit that touched something that ships is in an application's notes, never a `[notes]` commit, and a note saying the application did not change is refused by meaning rather than by phrase.
- **Section 5.** One generated line and the download page's address replace the whole statement on a release.
- **Section 6 and 7.** Every build regenerated: 30 rewritten, two kept as published and named, `0.1.0` untouched, known issues kept, nothing deleted. The GitHub bodies follow the file, and three are read back in the run report.

---

## 2026-09-24, entry 161: naming the calibre makes the reading worse, and the hole to calibre constant is wrong

**Status: actioned 2026-09-24**, all eight sections. It also corrects something entry 152 got wrong, which is in section 6 below.

- **Section 1 and 2.** Both runs reproduced, and the measurement confirmed rather than taken: 0.301 in across the middle, 1.14 of the bullet, 1.43 holes' area against the calibre's 0.249, five false doubles.
- **Section 3.** The sheet's own marks are the reference wherever there are five or more, named calibre or not, which **amends entry 141 section 4's line of twelve**. Below five a calibre vetoes splits and flags nothing. The scale panel says when the marks and the calibre disagree. Question 40's answer agrees.
- **Section 4.** The guess names no cartridge, from a scan or a photograph. It gives the measurement and asks.
- **Section 5.** Five scans now run from 0.765 to 1.14. `HoleToCalibre` stays 0.945 and says why it was not replaced. Worth an article under entry 158 section 1, held for entry 162's consent record.
- **Section 6, and it corrects entry 152.** GroupLab reports every distance in the sheet's own inches and never applies the print scale, so a sheet printed small makes every size read large. **Entry 152 said a shrunk sheet measures correctly; that was wrong, and I wrote it**, from the in-app sentence that said the figures were corrected. Every place it was said is corrected. Question 49 asks whether a scan should report real inches.
- **Section 7.** The rounds fired item says where the answer goes.
- **Section 8.** The regression test, the general test and the both-ways table. On the code before this entry only the friend's sheet got worse, 0 doubles to 5. Two fixture errors corrected: three .22 LR scans recorded as 0.224, and scan 3 recorded as .308 where its load block says 6.5 Creedmoor.

---

## 2026-09-23, entry 153: the standard every research article is held to

**Status: actioned 2026-09-23**, all six sections, section 5 within the one limit this project has on publishing real material.

- **Section 1.** The developer's name is gone from every file under `website/research/`: fourteen articles, two figure scripts and one data file. The sweep also caught the pronouns, which is the half a name search would have missed. One byline everywhere. Scoped to that directory, so the licence, the commit history and `samples/PROVENANCE.md` are untouched.
- **Section 2.** All thirty articles end with "What this means", about what to do differently or stop believing rather than the result again in words. Two that already had the section under their own headings are normalised to the one heading.
- **Section 3.** A figure with no caption fails the build, and an article with no figure fails unless its front matter carries `no_figure` with a written reason, which the page prints where the picture would be.
- **Section 4.** A rimfire 22 is 0.222 and **was not in the pick list at all**: 0.2215 is 5.45x39 and 0.224 is the centrefire 22. Two articles were recomputed rather than edited. `HoleToCalibre` does not move, because the two sheets behind it are 6.5 Creedmoor.
- **Section 5, and the limit is stated rather than worked around.** Three crops of real holes with the caliper line and a scale bar drawn on, at 0.897, 0.949 and 1.039 of the bullet, all from the sample scan, which is the only real material this project may publish. **The shadow case exists only in a photograph and no photograph has a consent record**, so the article says so in a line instead of showing it. Request 6 in `docs/notes/for-alan.md` asks whether one crop of one photographed hole may be published.
- **Section 6.** Four batches, three then three then seven then twelve, gated on a list in the site build that grew to cover every article and was then removed, because a backlog list that outlives its backlog becomes a way to opt out.

---

## 2026-09-24, entry 160: both sessions spend fewer tokens, without doing less work

**Status: actioned 2026-09-23**, all seven sections.

- **Section 1.** The three logs are split by `scripts/split-logs.py` and nothing is deleted: 1,102 KB to 97 KB, 837 KB to 99 KB, 165 KB to 30 KB, with the rest whole and unedited in `docs/notes/archive/`. **The fourteen day clause would have moved nothing**, because this repository is eleven days old and every entry falls inside it, so the fifteen entry rule was applied and the clash is **question 48**.
- **Section 1, and a fault it uncovered before it moved anything.** The entry headings in this log drifted from `##` to `#` at entry 119, and the two tests that read headings look for `##`, so **they had been silently skipping the thirty four newest entries**. Normalised, and `Logs.Notes()` now reads the live file and the archive together so the split cannot produce the same failure by a different route. Waking the test found two folds claiming more sections than they named, entries 150 and 152, both corrected.
- **Section 2.** `docs/notes/STATE.md`, 90 lines, rewritten and never appended to, with a test on the length and on what it has to answer.
- **Sections 3, 4 and 5** are in `CLAUDE.md` under "Tokens are the budget": run the suites quietly, never paste output into a report or a log, never read a file to confirm a write, never read a whole file to find one thing, one commit per entry, and write a script rather than making the same edit fifty times.
- **Section 6.** The command set that covers ordinary work is written out in `CLAUDE.md`, and the one setting change is request 5 in `docs/notes/for-alan.md` with the exact file to create. What stays behind a prompt is listed as deliberately as what does not.
- **Section 7.** A cold start was on the order of 2.1 MB of logs. It is now `STATE.md` at 5 KB plus the live notes file at 97 KB, so about 102 KB, a twentieth of what it was. To be checked weekly.

---

## 2026-09-23, entry 152: say plainly what GroupLab can measure, because two published claims are wrong

**Status: actioned 2026-09-23**, all five sections. Both claims were wrong, and the research article was right.

- **Section 1.** Both quoted claims are wrong, and the research article `printer-true-size` was right about the second. The wrong wordings stay quoted here, because a log that cannot record what was wrong is not a log, and a test bans them everywhere else.
- **Section 2, the measurement, and the answers are in `docs/PHASE1-RESULTS.md` with the file that settles each one.** There are four ways to get a scale, not one. Any target can be measured once the scale is set. A sheet printed at 96.2 percent measures correctly and is put through the same gate as a full size one. The print scale is computed to tell the person their printer shrank the sheet, not to correct anything, because the markers shrank with the sheet and the correction is already in the mapping.
- **Section 3.** `docs/WHAT-CAN-BE-MEASURED.md` is the one source. Both tour paragraphs are rewritten from it, and **the sweep found the same claim printed on every sheet GroupLab prints**: `SceneBuilder.ActualSizeNote` said "a sheet printed at any other scale measures wrong". That is now "a scaled sheet loses the spacing it was designed for", which is the true reason. The print screen's own wording was wrong the same way and is corrected. The ruler check and the printed instruction stay, as the entry asks.
- **Section 4.** Every tour screen carries a `withoutASheet` line, and the build refuses a screen without one. On six of the ten the honest answer is "no difference", and saying so is the point: the question a reader has is whether the application is useless to them without a printed sheet, and it is not.
- **Section 5.** Five phrasings banned across `website`, `src`, `docs` and the README, by the mechanism entry 145 used. One source, pointed at by the tour, the article and the build. The test caught my own quotation of the wrong claim in a docstring, which is the mechanism working.
- **A finding this entry turned up and did not go looking for: the corpus detection-counts record has been stale since entry 101.** Changing the printed note changed the artwork fingerprint, which is what gates that record, and the comparison then showed 26 of 55 images differing. **None of it is this entry's change**: with the old detector restored the same 26 rows differ, so the drift is entries 130 and 141's accepted work, never re-recorded. The record is now current. The gate fires on artwork and not on counts, which is why four entries of change went unrecorded without anything going red.

---

## 2026-09-23, entry 151: the Community link goes to a page, not straight out to Discord

**Status: actioned 2026-09-23**, all four sections, with one part of section 1.2 standing on a request rather than on a guess.

- **Section 1.** The meta refresh is gone and `grouplab.org/discord` is a real page in the site's layout. One thing on it leaves the site, it is marked "Opens Discord in a new tab" before it is clicked, and the invite is visible as text as well as being the link's target.
- **Section 1.2, and this is the part that is not complete.** The five group names are the server's own and the page describes what each group is for. **The channel names inside them are not written anywhere I can read**, so nothing invented them: the page says what each group is for and stops. `docs/notes/for-alan.md` request 4 asks for the channel list and for the server's own rules text, and `website/links.json` is where both go, so the page becomes exact without a rewrite.
- **Section 1.5.** The rules summary on the page is **the project's own expectations**, written here, and it says so. It is not a copy of the server's rules, because nobody has read those out of the server.
- **Section 2.** The footer said "Discord" and the navigation said "Community". Both say Community now.
- **Sections 3 and 4.** `website/links.json` is still the one source, and it now carries the group list as well. The test that the invite appears nowhere else is amended rather than deleted, for the one exception the entry allows. Two new tests: **no page on the built site carries a meta refresh**, which is the general form of the fault rather than the one instance of it, and the community page carries the invite both as a link and as visible text.

---

## 2026-09-23, entry 150: an executable is built only when the application changes

**Status: actioned 2026-09-23**, all six sections. Section 3's proof is a run rather than a reading, and the run it is proved by is the push that carries this entry.

- **Section 1.** `.github/shipping-paths.json` is the one list, read by the nightly's gate and by `ShippingPathsTests`. Twenty seven top level entries, eighteen shipping and nine content, each in exactly one list, with a reason written beside every one. **A path in neither list fails the gate**, which is the entry's own instruction and the right way round: a new top level directory should make somebody decide which side it is on rather than silently picking one.
- **Section 2.** The gate is a step in `name-it`, which is the first job and already resolves which commit the nightly is for, and it outputs `application-changed`. Both `package` and `publish` depend on it. A skipped night writes **"No application change since nightly NN. No build produced."** into the summary and creates no release, no tag and no assets.
- **Section 2.4, and it was a real change rather than a line of prose.** The version came from `github.run_number`, which counts runs, so a cancelled or skipped run consumed a number: that is why the published numbers already jump 14, 16, 18, 25. The number is now the highest nightly tag plus one, so numbers count builds.
- **Section 3, and it is proved by a run rather than by a reading.** Entry 144's guard on the notes commit still holds and now has a second one underneath it: a `[notes] ` commit is skipped by `name-it`'s condition, and it is content, so the gate would refuse it even if that condition went. The push carrying this entry produced a skipped nightly on its notes commit and a real one, nightly 92, on the code.
- **Section 4.** The releases page says that nightly builds are produced on the nights the application changed and that a gap in the numbers means nothing shipped, so a quiet night does not look like a page nobody updated.
- **Section 5, the measurement, and it does not say what it was expected to say.** Five of the last twenty eight nightlies changed nothing that ships: **14, 72, 76, 77 and 78**. **Nightly 84, the one Alan named, would still have been built**: 147 paths changed and they included `src`, `tests`, `.github` and `scripts` as well as 95 website files. So the waste is real but smaller than the releases page makes it look, and what made 84 look like a website build was its release note rather than its diff. That is entry 145's problem, not this one's.
- **Section 6.** Two tests as asked, over the lists and over the workflow's shape. Section 6.3's dry run against the last thirty nightlies is **a step in the nightly rather than a test**, printed into the run summary: it needs the tag history, which a test in CI does not have, and running it every night exercises it against real history exactly as the section asks.

---

## 2026-09-23, entry 149: answers to questions 35, 37, 40 and 47, and the Alan list for this run

**Status: actioned 2026-09-23**, sections 1, 2, 5 and 6. **Sections 3 and 4 are not done**, and both are real work rather than a judgement call: section 3 is question 37's A and D, which is marking-screen interaction, and section 4 is re-running the entry 121 survey's own baselines against the narrowed rule. Both are named in `docs/PHASE1-RESULTS.md` as outstanding, and neither is worked around anywhere.

- **Section 1, question 47: the five kinds stay.** `CLAUDE.md` now names all five, says which of the two headings each one lands under, and says why there are five rather than two: the heading is what the reader sees, the kind is what the writer says. `ReleaseNoteKindsTests` reads the word list out of `CLAUDE.md` and out of `scripts/release-notes.py` and holds them to the same set, because that pair is what drifted, and a third copy inside the test would have drifted the same way.
- **Section 2, question 40: the quarter-point of the smaller group.** Where the round marks fall into two clear sizes, a size is read now instead of refused, and it is the quarter-point of the smaller group. **This amends entry 82 section 3 and says so where the rule lives**, in `RenderDifferenceHoleDetector.SizeReference` and on `HoleSizeSource.TwoSizes`. The description still asks for the calibre, because which of the two sizes a single shot makes is exactly the thing not known. `CryingWolfTests` keeps both rows and the two-sizes row now asserts the doubles are flagged rather than that nothing is.
- **Section 2 item 4, the rimfire diameter,** is entry 153 section 4's sweep and is done there, not here. Both `Calibre.cs` and `CalibreGuessList.cs` already carry 0.222 and 0.224 as separate entries.
- **Section 5: requests for Alan go to `docs/notes/for-alan.md`.** The file exists with the three requests the entry names, newest first. `CLAUDE.md`'s "anything that needs Alan comes first" section is rewritten around it: nothing is asked of him in the panel except a command he pastes into a shell, and a request never stops the run.
- **Section 6:** the five-line report is now the entry number, what changed, the test result, the commit, and whether the site has published it yet, with no request for Alan inside it.

---

## 2026-09-23, entry 143: answers to questions 41 to 46, and the review of research batch 1

**Status: actioned 2026-09-23**, sections 1 and 2 in section 3's order. **Question 43 is answered and deliberately not built**, which the entry allows: it is last of everything here, and the reason is recorded in `docs/PHASE1-RESULTS.md` under "Where entry 137 and the code disagree" so the specification and the code agree rather than only appearing to.

- **Question 45, first as the entry demands, done in 2c51788.** Scan 6 was never ten. No regression; the tenth shot has never been detected, and it is a standing detection target in `range-scan-counts.json` now.
- **Section 1.1, and the live site was worse than the review said.** One card in four had a thumbnail locally; on the site it was one in eighteen, because every article with a chart is still a draft and all eighteen published ones have no figure at all. So the entry's own fallback is what fits: a plain titled panel in the site's colours, the same shape as a thumbnail, with no image to go stale.
- **Section 1.2.** `_style.py` writes both versions and **no figure script had to change**: `save` draws once, writes the light file, recolours the same figure and writes it beside as `-dark.png`. Recolouring rather than redrawing keeps the two identical in everything but colour. Two faults that first run produced, neither visible to a build that checks files exist: every dark figure had a black title on a black background, because an axes has three title artists and this style puts titles on the left; and one figure had a white cross on the chart and a black one in the key, because a legend's sample marks are copies. **So the build looks at the picture**, and two figures are exempt by name, being photographs of white paper.
- **Section 1.3.** Three states, and `website/research/PUBLISHED.md` is the record of publishing having happened. The build refuses both halves of a disagreement, proved both ways.
- **Section 1.4** left alone, as the entry asks.
- **Question 46, measured.** The wide and narrow solves return the same shift, 0.247 in from the one Alan's table implies, and differ only in confidence. `MarkingSession` acts only on a certain offset, so the wide solve does nothing and the restraint comes from the matching. The code was right and the paragraph above it was wrong. `SheetOffsetWideOrNarrowTests` pins it.
- **Question 42, built and tested on scan 5 and scan 6.** A corrected shot now survives a second detection, matched to the nearest fresh detection within one hole's width, with the person's position and chosen bull winning and the detection dropped. A correction moved further than a hole's width survives on its own. The button says how many marks it will keep before anybody presses it.
- **Question 41.** Drag onto a bull was never built, so nothing was removed from the application. Entry 141 section 5.3 item 3 is amended in place below, and `DragNeverAssignsTests` is what stops it arriving by accident.
- **Question 44, measured, and the answer is not to write the spline.** Held out one marker at a time across the 15 paired photographs: **the held-out error is the same as the fit's own residual**, ratio 0.8 to 1.0 on every photograph. The model predicts a marker it has never seen as well as one it was fitted to, so its 0.005 in is not flexibility spent bending to its own markers, and a more flexible surface fitted to the same markers cannot help. The thin-plate spline of entry 130 section 6b item 2 should not be written.
- **The crash, narrowed without a debugger.** The photograph that throws fitted cleanly here, nineteen times over with a different marker held out each time. So the fault is not in the fit and not in `ToPage` over the page: it is `ToPage` at a point outside the page, which only `ExpectedImage.Render` reaches. `SurfaceCrashTests` records it.
- `docs/PHASE1-RESULTS.md` "Entry 143 section 1" and "Entry 143, question 44".

Written by the planning session at 04:30 Mountain on 2026-09-23. The review in section 1 is the planning session's, sent to Alan at the same time as this entry; the answers in section 2 are decisions.

## 1. Research batch 1: approved to publish, with four things to fix

Alan has the same review and will send "publish research batch 1" himself. The writing is good and the honesty is right: the primer article in particular says what nine shots can and cannot support, which is the whole point of the section. Fix these, and take them as the standard for later batches.

1. **Every card on the index needs a lead image, or none of them do.** Today one card in four has a thumbnail and the rest are empty, so the row is as tall as the tallest card with three large blanks in it. Give every article a lead figure, and where an article has no natural chart, a plain titled panel in the site's own style is better than a gap.
2. **The charts are light panels on a dark page.** Every figure is drawn on a near-white surface, which glares against the dark theme and looks pasted on. Have each figure script write both a light and a dark version from the same data, and let the page choose by the reader's theme. This applies to the planning drafts as well: `_style.py` should grow a dark palette and the scripts should call it, rather than each script deciding for itself. If that is more work than it is worth this week, the fallback is a consistent light card behind every figure, so at least they all look deliberate.
3. **Front matter says `status: published` on articles that are not published.** Two different meanings of the word are in play. Use `state: draft | ready | published`, where `published` is set only when a batch actually goes live, and make the build refuse a page whose state says published when it is not in a published batch.
4. **The narrow renders are not phone renders**, as `docs/RESEARCH.md` says plainly, and that is an honest note rather than a fault. Leave it. Alan will look at the real pages on his phone after the first publish and report anything that breaks.

Nothing else blocks publication. Publish batch 1 when Alan's message arrives, then offer batches 2 and 3 for review the same way rather than publishing them with it.

## 2. Questions 41 to 46

### Question 41: dragging a shot onto a bull means two different things

**Dragging always moves the shot, and never changes which bull it belongs to.** A mark's position is a measurement and a drag is how it is corrected; nothing else may ride on that gesture. Entry 141 section 5.3 item 3 is amended: assignment happens through the bull picker in the shots list, through the keyboard (select, type the bull number), and through the multiple-selection assignment you have built. Drag onto a bull is removed from the specification. If you later want a pointer gesture for assignment, it must be a distinct one, such as a drag with a modifier key held, and it must leave the hole where it is and say in the toast which bull it moved the shot to.

### Question 42: a corrected shot does not survive a second detection

**Build the matching you propose**, and do not lose a person's correction silently.

- Match each kept corrected shot to the nearest freshly detected shot within **one hole's width**, using the sheet's own size reference from entry 141 section 4 where it exists and the stated calibre where it does not. Where a match exists, the person's position and chosen bull win over the detector's. Where none exists, keep the corrected shot as it is.
- Never produce two marks for one hole; that remains the rule your current code protects.
- Also make the button honest: detecting again says, in one line, how many hand corrections it will carry over.
- Test on scan 5 and scan 6, and with a generated sheet where a correction is moved beyond one hole's width and must therefore survive on its own.

### Question 43: entry 137 names an image safety the desktop does not have

**Both, as their own item, after the current queue.** Not urgent.

1. A cap at **400 megapixels**, phrased as a limit against a hostile or broken file rather than a judgement about scanning, with the number and the measured size in the message. Alan's 600 dpi letter scans are about 32 megapixels, so the cap is twelve times his largest real file.
2. Move the decode to a background thread with a timeout, because the window freezing on a large scan is a real complaint waiting to happen. Show progress while it runs.

Until then, say in entry 137's record that the pixel cap and decode limit were not built and why, so the specification and the code agree.

### Question 44: the bent-sheet model crashes on one photograph, and improves the wrong points

**Measure first, build nothing.** Run the leave-one-marker-out measurement on the existing surface model across the paired photographs and report it. A model that cannot predict a marker it did not see will not predict a hole, and that decides whether the thin-plate spline in entry 130 section 6b item 2 is worth writing at all. Do not write the spline until that measurement is in.

The crash: spend up to an hour finding it, because an `IndexOutOfRangeException` in registration is worth understanding even in an unreachable model. If it is not obvious in that time, leave it with a test that records the crashing photograph and a note, rather than a speculative fix.

### Question 45: scan 6 reads 9 holes tonight where entry 130 recorded 10

**This is the first thing to do, before any new feature.** A real shot that the software used to find and now does not is the most serious kind of regression this project can have, and shot 6 is the one shot on that sheet that proves a group can contain something far from everything else.

1. Re-run scan 6 at the commit before entry 141 section 4 landed, and at the commit after, and report both counts.
2. If section 4 cost the hole, fix it so the sheet's own size reference does not drop a hole that a stated calibre finds, and say in the fix what the mechanism was.
3. Record the hole counts for all six range scans as a checked expectation somewhere a change like this trips over, without committing the scans: a small file of counts plus each scan's SHA-256, and a test that runs only when the folder is present and is skipped with a clear message when it is not.
4. You were right to say your earlier check was weaker than your sentence implied. Flag counts are not hole counts, and the correction belongs in the record.

### Question 46: the sheet offset is solved over every bull

**Run the measurement you propose**, both ways on scan 5, and compare each result against the offset Alan's table implies.

- If the wide solve is the better estimate, keep the code and rewrite the paragraph: the restraint is delivered by the matching, which only considers aimed bulls, and the solve is allowed to use the whole printed grid because the grid is geometry, not evidence about where the shooter aimed.
- If the narrow solve is as good, narrow it and fix the five-shot test.
- Either way, the test that broke is a case worth keeping: add it with a name that says which reading it is pinning.

Do not leave the code and the comment disagreeing, whichever way it goes.

## 3. Order

Question 45, then the batch 1 fixes in section 1, then questions 46, 42 and 41, then question 44's measurement. Question 43 last. Entry 129, the server work, waits for Alan and does not move.

---

## 2026-09-23, entry 147: macOS test builds, and a plain statement of what is supported

**Status: actioned 2026-09-23**, every section.

- **Section 1.** `osx-arm64` and `osx-x64`, self-contained, built on `macos-latest`, each a real `.app` bundle with `Info.plist`, `PkgInfo` and the icon, packed as `grouplab-macos-arm64.tar.gz` and `grouplab-macos-x64.tar.gz`. `scripts/macos-bundle.py` is the one place the bundle's shape is decided, and `MacBundleTests` builds one and reads it back without needing a Mac.
- **Section 1.5.** The updater already refused to offer a self-install off Windows. It now says plainly that updates are manual there, on the Settings screen and in the update run's own message, instead of reporting that there is nothing to install for this platform, which reads like a fault in the build.
- **Section 1.6.** Labelled untested on both cards, in the file names, and in the bundle's own `Info.plist`, which is the one label that survives unpacking after the download page is long forgotten.
- **Section 2.** The command on the download page and in the README, with what it removes and who should not run it.
- **Sections 3 and 3.2.** `docs/PLATFORM-SUPPORT.md` is the one source. `website/build.py` renders it into the download page, `scripts/platform-support.py` writes it into `README.md` between two markers and `--check` fails CI when it drifts, and the nightly appends it to any release whose assets include a macOS build. Nothing restates it, because a statement in somebody's settled words is exactly the text that gets reworded in one place and not the others.
- **Section 3.1.** The build targets are one list in `package.yml`. **To add one without further questions we need the architecture, and whether a plain tarball or a package built for a named distribution is wanted.**
- **Section 5.** Five tests, none of which need a Mac.
- **What went wrong, and it is the part worth reading.** The first push went red on all three runners and no nightly ran, so grouplab.org was live offering two Mac builds whose files returned 404. Three failures, all mine: a README heading with no contents entry, which was in the wrong section anyway; a README linking five assets where the test allowed three; and, on Windows alone, my own test anchoring a pattern with `$` against a file that has CRLF line endings on a fresh checkout. It passed here because this working copy is LF. Entry 121 section 3 records the same fault in another test, with the same cause.
- `docs/PHASE1-RESULTS.md` "Entry 147".

Written by the planning session at 02:10 Mountain on 2026-09-23. Alan's decision, in his words below. Do this after entry 129.

Today every build is tested on macOS, because `build and test` runs on `macos-latest` as well as Windows and Linux, and no macOS download exists. That is a reasonable state and a confusing one to a reader, because nothing on the site says either half of it. This entry publishes a macOS build, marks it honestly, and says plainly what will and will not happen.

## 1. Build and publish a macOS test build

1. Add macOS to the packaging alongside the Linux tarball: `osx-arm64` for Apple silicon and `osx-x64` for Intel Macs, self-contained, two separate downloads rather than a universal binary.
2. Package each as a proper `.app` bundle inside a `.tar.gz` or `.zip`, with `Info.plist`, the GroupLab icon, and a name that reads correctly in Finder. A bare executable runs from a terminal and behaves like a stranger in the dock, which is not worth publishing.
3. Publish both as nightly assets beside the Windows and Linux ones, named so the architecture is obvious, for example `grouplab-macos-arm64.tar.gz` and `grouplab-macos-x64.tar.gz`.
4. Build them on the `macos-latest` runner, which is already in the matrix, so the packaging is done by the platform it targets.
5. **The updater does not offer these builds.** The silent install and relaunch chain is the Windows installer, and a macOS build must not be offered an update it cannot apply. Check what the update path does on macOS and make it say plainly that updates are manual there.
6. **Label them untested everywhere they appear**: on the GitHub release, on the download page, in the file name if you can do it without making the name silly. Nobody has run this on a Mac.

## 2. The terminal command, and why it is needed

An unsigned application downloaded from the internet is quarantined by macOS, and Gatekeeper refuses to open it. Give the exact command on the download page and in the README, with a sentence saying what it does:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Say that this removes the quarantine flag macOS puts on downloaded files, that it is the standard way to run unsigned software, and that a reader who is not comfortable doing that should not run the build. Adjust the path in the instructions to wherever the pages tell people to put the app.

## 3. The platform statement, word for word

Alan has settled this wording. Publish it as its own section on the download page, titled "What is supported, and what is not", linked from the README, and do not reword it. It avoids the first and second person on purpose, and it says "they" of the author on purpose.

---

**Windows is the supported platform.** It is where GroupLab is developed and tested by hand, and the installer and automatic updates are built for it.

**Linux builds are published and are worth trying.** The download is a self-contained 64-bit tarball, so it runs on most desktop distributions without anything else being installed alongside it. The test suite runs on Linux on every build. Hands-on testing has not started yet. Linux can be tested here on virtual machines under VMware Workstation, and there is no bare metal Linux machine, but the real reason is that the application is still under heavy development, with features, layouts, appearance and internal workings changing daily. Testing a moving target on a second platform would mostly produce findings that are obsolete a week later.

**macOS builds are published and have never been run on a Mac.** The tests run on macOS on every build, so the code works at that level, but nobody has opened the window, printed a target or saved a session on real hardware. These builds are an experiment rather than a release.

### What happens once the application settles

Other platforms get proper attention once the pace of change slows and the Windows application is generally working the way the developer wants it to.

**Android is planned and is a high priority**, because that is the mobile platform in daily use here. Hands-on Linux testing follows, on virtual machines. macOS depends on the hardware question below.

### Running the macOS build

macOS quarantines anything downloaded from the internet and refuses to open software that is not signed by a registered Apple developer. After the application has been moved to the Applications folder, this removes the quarantine flag:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Anyone not comfortable running that command should not run this build.

### Why it is not signed

Signing a macOS application requires the Apple developer programme, which costs 99 dollars a year. The developer of GroupLab does not own a Mac, does not intend to buy one, and is not going to pay a yearly fee for a platform they do not own.

That is the whole reason. It is not a technical obstacle and it is not indifference to Mac users. If a developer or contributor wants signed macOS releases enough to donate a Mac for testing and cover the developer fees, the project will set it up.

### Signing elsewhere

The one-off 25 dollar Google Play developer fee has been paid. A signed Windows version through the Microsoft Store is intended in due course, and a code signing certificate may be bought if the price turns out to be reasonable.

### Apple mobile

An iPad Mini, sixth generation, is available as test hardware, and an iOS version of GroupLab would be tested on it. Building and signing an iOS application requires a Mac and the Apple developer programme, so that version cannot be produced at present, for the same reason the macOS build is unsigned. The hardware to test it exists; the machine to build it does not.

### Other Linux builds

The published Linux build is x86-64. Other targets can be added to the nightly builds on request: Arm64 for a Raspberry Pi or an Arm laptop, or a package built for a particular distribution rather than a tarball. Adding one is a line of configuration rather than a project. The reason a dozen are not published already is simply that nobody has asked for them.

Requests go to support@grouplab.org, naming the distribution and the architecture.

### Reports from Linux and macOS are welcome

A report is useful even when the answer is that it crashed on startup. "It opened and the buttons are the wrong size" is a useful report, and so is a crash report, which GroupLab can send on request. The address is support@grouplab.org.

---

Two notes for Code rather than for the page. Keep the build targets as a single list in the workflow, so adding one really is a line, and say in your report what a person must tell us for a target to be added without further questions. And a test should fail if this section's macOS wording, or the sentence saying nobody has run it on a Mac, disappears while a macOS asset is still published.

## 3.2 The same statement in all three places

The wording in section 3 goes, identically, to:

1. **The website**, as its own section of the download page at `https://grouplab.org/download/`, with its own heading so it can be linked to directly.
2. **The repository README**, which is the first page anyone sees on GitHub. Put a short "What is supported" section there with the same words, or the first two paragraphs and a link to the download page if the README would otherwise get unwieldy. The three facts that must appear on GitHub itself, not only behind a link, are that Windows is supported, that the macOS build has never been run on a Mac, and that other Linux targets can be requested.
3. **Every GitHub release that carries a macOS asset**, as a short note in the release body, next to the downloads, saying the macOS build is untested and unsigned, giving the quarantine command, and linking to the full statement.

Keep one source for the words: hold the statement in a single file in the repository, generate the website section and the README section from it, and have the release note quote from it. A statement that has to be edited in three places is a statement that will disagree with itself within a month. A test fails if the copies drift apart.

## 4. Ask for what would change it

Close the section with a plain invitation: if someone with a Mac wants to run the build and report what happens, that is useful on its own, and the support address is the way to do it. Make it easy to say "it started" or "it crashed at this point", and make it clear that crash reports from macOS are welcome even though macOS is not supported.

## 5. Keep it true

- A test fails if a macOS asset is published without the untested wording on the download page.
- The download page names each build's architecture, so an Apple silicon owner does not take the Intel one by accident.
- If a macOS build ever fails to package, the nightly still publishes the Windows and Linux ones rather than failing entirely, and says which one is missing.

---

## 2026-09-23, entry 148: the Discord server, linked from the site and GitHub

**Status: actioned 2026-09-23**, sections 1 to 5. Section 5 is a note rather than a build, as it asks.

- **Sections 1 and 3.** The invite is in `website/links.json` and nowhere else. Everything published points at `https://grouplab.org/discord`, which the build makes as a redirect page carrying the invite from that file. **Two tests hold it**: the invite is written out in one place only, and the one built page that carries an invite carries the one in that file. Replacing the invite later is a single edit and no published link breaks.
- **Section 2.** Top navigation as "Community", the footer beside the other links, the support page as the first option with the sentence about the address being better for anything private or with a photograph, the README near the download links as a plain line, and one line on the download page.
- **Section 2, last line, observed.** Nothing was put in the application. A link inside the software outlives the server, and that is a different decision.
- **Section 4.** The words are the entry's, unchanged. Nothing calls it official support, nothing promises a response time, nothing implies it is staffed.
- **Section 5.** Noted in `docs/WEBSITE.md` with the one line on how it would work: the nightly already writes the plain-words note before it publishes, so posting it is a single HTTP call to a webhook URL held as a repository secret, in the same job.
- `docs/WEBSITE.md`.

Written by the planning session at 03:40 Mountain on 2026-09-23. Do this after entry 129, alongside entry 147.

Alan has created the GroupLab Discord server and its permanent invite. Use the link in section 1 exactly as written; never invent or guess an invite.

## 1. The link

The permanent invite exists and never expires:

```
https://discord.gg/jY7MrYNN5V
```

Publish `https://grouplab.org/discord` as the canonical link everywhere (site, README, release notes), and have it redirect to the invite above. The invite itself is written down in exactly one place in the repository, so replacing it later is a single edit and no published link ever breaks.

## 2. Where it goes

The same link, from one source in the repository, in these places:

1. **The website's top navigation**, as "Community" or "Discord", so it is reachable from every page.
2. **The website footer**, beside the GitHub link.
3. **The support page**, as the first option for questions, with a sentence saying the support address remains for anything private or anything involving a photograph.
4. **The repository README**, near the top with the download and website links, as a plain line rather than a badge, unless a badge fits the README's existing style.
5. **The download page**, one line: somewhere to ask if something does not work.

Do not put it in the application itself in this entry. A link inside the software is a different decision, because it outlives the server.

## 3. How to hold it

Put the URL in the same single source that entry 147 section 3.2 uses for the platform statement, or beside it: one file, rendered into the page, the README and anywhere else. A test fails if the link appears written out in more than one place, and a test fails if any published page carries an invite that is not the one in that file.

## 4. What the site says about it

Short, and honest about what it is for:

> **Discord.** Questions, bug reports, target sheets, and what people are shooting. The project's developer reads it. For anything private, or anything with a photograph attached, the support address is better.

Do not call it "official support", do not promise a response time, and do not imply it is staffed.

## 5. Not automated yet

Release announcements into Discord are a webhook from the release workflow and are worth doing, but not in this entry. Alan is using GitHub's own webhook to begin with. Note it in `docs/WEBSITE.md` as a possible later item, with one line on how it would work: the release workflow already has the plain-words release note, so posting it is a single HTTP call to a Discord webhook URL held as a repository secret.

---

## 2026-09-23, entry 146: a tour of the application, one page per screen

**Status: actioned 2026-09-23**, sections 1 to 6. Every screen, not the three section 6 allows as a fallback.

- **Sections 1 and 2.** `/tour/`, in the top navigation between Download and Guides. An index of ten cards, then one page per screen: library, print, marking, analysis, showing the work, session records, compare loads, equipment, ballistics, settings. Each carries the screenshot large in both themes at the size the site uses, what the screen is for, a numbered list of its parts, two to five steps, where it sits in the flow with links either side, and links to the guide or research article where one exists.
- **Section 2.3, and the choice I made in it.** The entry offers a numbered overlay on the image or a labelled list beneath. **I used the list.** Every one of these screens has eight to eleven parts worth naming, and eleven numbered badges over a 1400 by 900 screenshot would obscure the thing they point at. The list names each part in the words the screen itself uses, which a reader can match by reading rather than by hunting for a small number.
- **Section 4.1 and 4.2, in two places.** `website/tour.json` is the list, and **the site build refuses to build** when a screen there has no render or a render has no page. `TourTests` says the same from the other side, so a screen added to the render walk fails with the name of the page somebody still has to write. Proved by adding a screen with no picture: `the tour has a page for 'reloading' and no screenshot of it was rendered`, and the build stopped.
- **Section 4.3.** The pictures are the ones `PublishedRendersTests` already holds to a source in `SOURCES.md`: generated sheets, invented rifles and loads, nothing from a range folder and nothing from a submission.
- **Section 4.4** is a rule rather than code, so it is in `CLAUDE.md`: the screenshot job replaces the picture and nothing replaces the words, so an entry that changes a screen says whether its tour page still describes it.
- **Section 5.** The home page had four screenshots: analysis in the hero, then marking, the target library and printing in a row of three. **It now has two**, the hero analysis and marking, and the row of three is a single figure beside the status panel with a link to that screen's tour page, plus a note pointing at the tour. A test fails if it ever shows more than two.
- **Section 3, and what it cost.** No class names and no file paths, checked by a test on the same four patterns the release notes use. Writing them meant reading every screenshot rather than the code, which is the point: the parts are named as they appear, so "Accept and analyse" and "Detect on a GroupLab sheet" are what the page calls them because that is what the button says.
- `docs/PHASE1-RESULTS.md` "Entry 146".

Written by the planning session at 00:20 Mountain on 2026-09-23. Do this after entries 144 and 145, and before the remainder of entry 143.

Alan: "I think there should be a separate page for screenshots of each section of the application that explains what is happening instead of just a few screenshots on the main page."

A handful of pictures on the home page shows that GroupLab exists. It does not show what using it is like, and that is the question somebody has before they download an unknown program. The screenshot job from entry 144 section 4 already produces the pictures; this entry gives them somewhere to live and something to say.

## 1. The section

`https://grouplab.org/tour/`, linked in the top navigation between Download and Guides. An index page, then one page per screen.

The index gives each screen a card: its name, one sentence saying what it is for, and its own screenshot. A reader should be able to understand the shape of the application from the index alone, and go deeper where they care.

## 2. One page per screen

Eleven screens exist as renders today: analysis, analysis with a sheet open, marking, library, print, equipment, compare, ballistics, sessions, settings, and whatever the twelfth becomes as the interface grows. Give every one its own page, driven by the same list that drives the screenshot job, so a new screen cannot appear in one and not the other.

Each page carries:

1. **The screenshot, large**, light and dark, following the reader's theme, at the desktop size.
2. **What this screen is for**, one short paragraph in plain words, from the shooter's side.
3. **What you are looking at**: a numbered list keyed to the picture, naming the parts and saying what each does. Use a numbered overlay on the image, or a labelled list beneath it where an overlay would crowd the picture. A reader must be able to match every item to something they can see.
4. **What you would do here**, two to five steps, in order, as a person would do them.
5. **Where it fits**, a line linking the screens before and after it in the ordinary flow: print a sheet, shoot it, open the image, mark it, read the analysis, record the session, compare loads.
6. **Links to the related guide and any research article**, where one exists.

## 3. The words

Written for somebody who has never opened GroupLab, and never for somebody who has read the code. No class names, no file paths. Name what is on the screen using the same words the screen uses, so a reader can follow along with the application open beside the page.

Keep each page short: a picture, a paragraph, a numbered list, a few steps. Where a screen needs a long explanation, that explanation belongs in a guide or a research article, and the tour page links to it.

Nothing on these pages may claim a feature that is not in the published build the screenshots came from. Say which build the pictures are from, and let the screenshot job keep that current.

## 4. Keeping it true

1. The pages are built from the same screen list as the screenshot job in entry 144 section 4, so a screen that gains or loses a render fails the build rather than going stale quietly.
2. A test fails if a tour page references a screenshot that is not produced, or if a produced screenshot has no tour page.
3. The screenshots use generated data only: a generated sheet, invented rifles and loads, no material from Alan's range folder and nothing from a submission.
4. When the interface changes enough that a picture is wrong, the screenshot job replaces the picture and the entry that changed the interface should say whether the words need changing too.

## 5. The home page

Once the tour exists, the home page keeps one or two pictures at most and links to the tour rather than trying to be it. Say in your report what you removed.

## 6. Order and effort

This is a page-building job, not an application job, and it must not displace entry 143's queue. If it runs long, publish the index and the three screens that matter most to a newcomer (analysis, marking, print), and add the rest in a second pass.

---

## 2026-09-22, entry 145: every build says what changed, in plain words

**Status: actioned 2026-09-23**, sections 1 to 6.

- **Sections 1 and 2.** Two headings, `**What you will notice**` and `**Under the hood**`, and a build shows only the ones it has. There is no third state: a build with no commits behind it is refused outright, because that is the only thing left that could honestly say nothing, and it cannot happen.
- **Section 3.1.** `Release-note-kind:` accepts `internal` for the second heading and `user` for the first. `new`, `fixed` and `changed` are kept and all mean the first heading. **This is a decision I took rather than the reading the entry gives**, which is that the kind is `user` or `internal` and nothing else. Every commit in the history uses the three older words, section 5 says this is a rewording and not a rewrite of history, and dropping them would have invalidated every trailer already written. Raised as **question 47** so the planning session can overrule it.
- **Section 3.2.** A commit with no trailer gets a line of its own, written from its subject with the entry reference taken off, under the second heading. The count is gone.
- **The awkward part of that, said plainly.** A commit subject here is often written for the log, so a generated line can carry a file path or a class name, which section 4 forbids. Two things stop that: a short table translating the handful of repository files whose names appear in subjects into plain words, so "docs slash release notes carries nightlies 71 to 76" becomes "the release notes carries nightlies 71 to 76"; and, for anything the table does not cover, the build fails and names the commit. **Grammar suffers in the translated case**, and I have left it rather than guess: the fix is a trailer on the commit, which is what section 3.1 asks for anyway.
- **Section 3.3.** `--missing` lists every commit since the previous build that made the generator write from a subject, and the nightly puts that list in the build's own report. It reports and does not fail, because a note can be improved after a build and a build cannot be un-published.
- **Section 4.** Four checks on every line, written or generated: a file path, a commit hash, a class or method name, and anything in code style. "GroupLab" is the one word shaped like a class name that belongs in a note. The reference in brackets at the end of a note is the one place an entry may be named, and it is not checked.
- **Section 5, and more than section 5 asked for.** The six builds that said "Nothing in this build changes what you see or do" are rewritten from their own commits: **nightlies 78, 77, 76, 75, 72 and 30**. None of them contains that sentence now, and a test fails if it comes back. Nightly 81, published tonight and not yet in the file, is written in the new shape.
- **Four more builds were silent in a way the entry did not name**, and I fixed them rather than leave them: nightlies 26, 18, 14 and 12 listed their known issues and said nothing at all about what changed. Nightly 12 is the first build GroupLab ever published for itself, and the file did not say so.
- **The one thing I would not write.** Nightly 26 carried the commits behind "where the group actually landed" and "the scan's stated resolution", and `CLAUDE.md` records that nightly 27's note about the first of those was untrue on the day it was published, because the code was wired to nothing. Its new entry says those two are groundwork that could not be reached from any screen in that build, which is what was true.
- **Section 6.** Five tests. The sentence and the count cannot come back, every build lists something, every build's lines sit under a heading, no line names a file, a hash or a class, and the generator itself cannot write the old sentence. The three build jump in the update bar is in the results with its exact text.
- `docs/PHASE1-RESULTS.md` "Entry 145".

Written by the planning session at 23:45 Mountain on 2026-09-22.

Alan, on the release notes as they read today: "I dont like how many of the release notes just say 'Nothing in this build changes what you see or do. It carries internal work only.' No matter what is done, it should be stated plainly what changed."

He is right, and the sentence is not even true. Something changed in every build, or there would be no build. Saying otherwise teaches a reader that the page is filler and trains them to stop reading it.

Do this after entry 144, and before the rest of entry 143.

## 1. The rule

**No build ever says nothing changed.** Every published build lists what is in it, in plain English, whoever it affects. A build that carries one documentation commit says which document and what it now says.

## 2. The shape of an entry

Two headings, and a build shows only the ones it has.

**What you will notice.** Changes a user meets: something on screen, something that behaves differently, a new or removed feature, a fix to something that was wrong, a change to what is installed or downloaded. Written from the user's side, never from the code's.

**Under the hood.** Everything else, still in plain words: tests, documentation, the website, the build, refactoring, performance work that nobody can perceive yet. One line per real change, not a count. "Two internal changes" is the thing this entry exists to remove.

Keep each line to one sentence. Group several commits that did one job into one line, and say so: "three commits finishing the shot editor's undo support". Aim for at most six lines a build, by grouping rather than by leaving things out. Where a build is genuinely one commit, it is one line.

## 3. Where the words come from

1. `Release-note:` stays the first source, and it should now be written for every commit, not only the ones a user notices. `Release-note-kind:` says which of the two headings it belongs under: `user` or `internal`.
2. Where a commit has no trailer, `scripts/release-notes.py` must not fall back to a count. It writes a line from the commit's subject, rewritten as a plain sentence, and marks it so the build reads as complete rather than as boilerplate.
3. Add a check on main: every commit since the previous tag either carries a `Release-note:` trailer or is reported by name in the build's report, so a missing one is noticed at the time rather than months later on the site.

## 4. Plain words, specifically

Write for a shooter who has never read this repository. Name the thing on screen, not the class.

- Not "refactored MarkingSession.Load". Instead: "opening a sheet again keeps the marks you moved by hand".
- Not "added ResearchArticleTests". Instead: "the website now refuses to publish an article whose data file is missing".
- Not "bumped the freshness gate". Instead: "a nightly build is no longer published when a newer commit has already landed".

No class names, no file paths, no commit hashes in the body. The commit is already named in the entry's header for anyone who wants it. Keep the project's other rules: no em dashes, no pseudoscience, no jargon left unexplained.

## 5. Go back over the ones already published

Every entry in `docs/RELEASE-NOTES.md` that says nothing changed, or gives only a count, is rewritten from its own commits under section 2's shape. Do not invent detail: where a build really was one documentation commit, say which document and what changed in it. Keep each build's date and commit as recorded; this is a rewording, not a rewrite of history.

Nightlies 77 and 78 are the two nearest examples, and they are honest ones: 77 carried the release notes for nightlies 71 to 76, and 78 carried a note about where the notes stopped. Both are worth one plain line each, and both are more interesting than "internal work only".

## 6. Tests

- A test fails if any entry contains "nothing in this build changes", "internal work only", or a bare count of changes.
- A test fails if an entry has no lines under either heading.
- A test fails if a line contains a file path, a commit hash, or a bare identifier in code style, in the body of an entry.
- The update bar in the application reads the same source, so check that a multi-build offer still reads well with the new shape, and say in your report what it looks like for a three build jump.

---

## 2026-09-22, entry 144: the site publishes itself, and the release notes keep up

**Status: actioned 2026-09-23**, sections 1 to 5 and 6's documentation. **Section 6's proof is partly open**, and named here rather than left implied: the site content commit that published itself and the failure path are in this commit's report; the nightly whose notes reach the live releases page with no human step cannot be shown until the next nightly runs; and the 5 minute sync cadence cannot be shown until Alan runs `install.py`, which section 3 says is the one manual step.

- **Section 1.** `website.yml` gained a `push` trigger on `main` with a paths filter, keeping `workflow_dispatch`. The filter is `website/**`, `docs/RELEASE-NOTES.md`, `docs/GLOSSARY.md`, `docs/USER-GUIDE.md`, `docs/TESTING-GUIDE.md`, both guide PDFs, `docs/figures/screens/**`, `targets/**` and the workflow file itself. It no longer waits on `build and test`: it installs the .NET SDK, builds the site and runs the site's own tests before publishing anything. Concurrency is one publish at a time with `cancel-in-progress`, so a burst of commits publishes once. `reason` became optional, and a push records the commit subject instead.
- **The two gaps I found in my own first cut of section 1, both while checking rather than while writing.** The filter did not include `docs/figures/screens/**`, so section 4's screenshot commit would have committed new images and published nothing; and the guides' own sources were missing, so editing a guide would have left the site showing the old one. Both are in the filter now.
- **Section 2.** `nightly.yml` writes its build's entry with `scripts/release-notes.py`, prepends it to `docs/RELEASE-NOTES.md`, commits it as `[notes] <version>` and pushes it. That path is in section 1's filter, so the site follows. **The loop is guarded in both directions and proved in both directions**: every job in `ci.yml` and the nightly's first job refuse a subject beginning `[notes] `, and `NotesCommitLoopTests` holds four facts, including the one that would otherwise fail silently, which is that an ordinary commit still runs everything. A guard written the wrong way round turns the whole suite off and nothing says so, so the test reads every clause of every condition and requires each to be a denial.
- **Why the guard is not paranoia.** A run whose jobs all skip still reports success, and the nightly triggers on `build and test` succeeding. Without the marker check in the nightly as well, a notes commit would have started a build, which publishes, which writes notes, which pushes: a release every few minutes for ever, each deleting the oldest to keep thirty.
- **Section 3.** `grouplab-site-sync.timer` is 5 minutes, from 15. The files are on the server and Alan's command is in the report.
- **Section 4.** `screenshots.yml`, weekly on Monday at 06:00 UTC plus dispatch, renders every screen from `main` in both themes at the three sizes and commits what changed as `[screens] ...`. It runs `PublishedRendersTests` before committing, so a render nothing in `SOURCES.md` accounts for stops it. That marker is guarded the same way as `[notes] `: an image-only commit needs no C# test run, and a build of the application from one would be a release of nothing.
- **What section 4 uncovered, which is worse than the drift it was written for.** The site's screenshots were last regenerated on 2026-09-19 and could not have been refreshed by running the walk, because **no test produced the 1400 by 900 size the website actually shows**. The render walk did 1280 by 720 and 2560 by 1440 only. So the live site had been showing a four day old interface with no way to notice: the walk passed, the sizes it produced were current, and the size the site served was not among them. 1400 by 900 is back in the walk, and this commit carries 31 refreshed images including two screens the site had never shown at all.
- **Section 5 and 6's documentation.** `docs/WEBSITE.md` gained "Stopping a publish" and its publishing section now describes the push trigger. `CLAUDE.md`'s website section is rewritten around the same rule. **Entry 128 section 6 is superseded by this entry**, and is marked so below.
- `docs/PHASE1-RESULTS.md` "Entry 144".

Written by the planning session at 23:10 Mountain on 2026-09-22. This replaces entry 128 section 6's rule that nothing publishes the site by itself.

**Why it is changing.** That rule was right when nothing had been proved. The signature check, the live check and the rollback have now all done their jobs on real publishes, including a rollback that saved the live site and a retry that installed it. What is left of the old rule is only cost: Alan watched a night's work sit unpublished, with release notes ending nine builds behind, because publishing needed a person to ask and a full Windows test run to finish first. His words: the fact that it did not happen overnight is annoying. He is right.

**What replaces it.** The machinery runs by itself. What is visible is still controlled, but by the content rather than by the pipeline: a research article appears when its own state says published, which only Alan agrees to, and an unfinished page is simply not marked ready.

Do this after the publish in flight, in this order. Every security constraint stands: no server address anywhere in the repository, no secret in a workflow log, no repository settings changed, no tags but the nightly workflow's.

## 1. A site workflow of its own, triggered by content

1. `website.yml` gains a `push` trigger on `main`, with a paths filter: `website/**`, `docs/RELEASE-NOTES.md`, `targets/**`, and any other file the site is built from. Keep `workflow_dispatch` as well.
2. It must not wait on `build and test`. The site's own gate is its own: run `python website/build.py` and the site's tests (the research front matter, data and figure checks, the em dash and banned term check, the metadata check on published images, the release notes check). Those take a minute or two, and they are the checks that can actually tell whether a page is wrong.
3. Concurrency: one site publish at a time per branch, newest wins, so a burst of commits publishes once.
4. If the build or its tests fail, publish nothing and leave the last good parcel in place.
5. Say in `docs/WEBSITE.md` what a person has to do to stop a publish: mark the page's state, or push a fix. There is no dispatch to withhold any more.

## 2. The release notes write themselves

1. When `nightly.yml` publishes a build, it also generates that build's entry with `scripts/release-notes.py` and appends it to `docs/RELEASE-NOTES.md`, then pushes that one file to main. That push matches section 1's paths filter, so the site follows within a couple of minutes.
2. Guard the loop: the notes commit must not start another `build and test` or another nightly. Use a marker in the commit message and skip on it, or a paths-ignore, whichever is cleaner in this repository, and prove both ways round in the report: a notes commit publishes the site and starts nothing else; an ordinary commit still runs the full suite.
3. The existing test that fails when the notes fall behind the tags stays, and becomes almost impossible to trip.
4. Hand-written wording stays welcome. The generated entry is the floor, not the ceiling: an entry Code improves later is an ordinary site content commit.

## 3. The server checks every 5 minutes

Change the timer from 15 minutes to 5. That makes the whole path, from a commit to a live page, about 7 to 8 minutes. The sync already does nothing when there is nothing new, so the extra runs cost nothing worth measuring. This needs `install.py` run again by Alan: prepare it, scp it, and give him the command in your report. It is the only manual step in this entry.

## 4. Screenshots that keep up with the application

The site shows the interface, and the interface is changing weekly. Add a job that regenerates the site's screenshots from the newest published build and commits them when they differ from what is committed, which publishes them by section 1. Once a week is enough, plus whenever an entry says the interface changed. Renders go through `IOutsideWorld`; nothing is printed and no real printer or paper is touched. If a screenshot shows a sheet or a result, it uses generated data, never Alan's range material.

## 5. What a person still decides

- Whether a research article is published, through its state.
- Whether a page exists at all.
- Anything that would change what the site claims about GroupLab: those are still entries from the planning session.

The pipeline decides nothing except when to run.

## 6. Prove it, and say so

In the report, show: a site content commit that published itself with its timings; a nightly whose notes reached the live releases page with no human step; the failure path, where a deliberately broken page stops the publish and leaves the live site untouched; and the sync log showing a 5 minute cadence. Update `docs/WEBSITE.md` and `CLAUDE.md` so the publishing rule they describe is this one, and note in `docs/NOTES-FROM-PLANNING.md` that entry 128 section 6 is superseded.

---

## 2026-09-21, entry 133: a "light" installer, measured before it is built

**Status: actioned 2026-09-22**, sections 1 to 6. Measured and proposed; nothing was built, as the entry requires.
- **Section 1, measured on this machine:** framework-dependent is 227.9 MB unpacked and 78.2 MB zipped, against 332.6 MB unpacked and a 97.3 MB installer self-contained. Of the framework-dependent build, Avalonia and Skia are 121.4 MB, OpenCV's native library 93.6 MB, everything else 7.9 MB, and GroupLab itself 5.0 MB.
- **What the measuring found, which matters more than the answer.** 100.7 MB of the shipped build was debug symbols, 100 MB of it two files: `libSkiaSharp.pdb` at 80.1 MB and `libHarfBuzzSharp.pdb` at 19.9 MB. Nothing reads them at runtime, no crash report here can use them, and no user will open them in a debugger. Leaving them out took the self-contained build from 332.6 MB to 204.3 MB with the analysis unchanged, so **the self-contained build is now smaller than the framework-dependent one was**.
- **Section 2:** the runtime is not what makes an update large. Avalonia, Skia and OpenCV are 215 of the 228 MB and travel either way.
- **Section 3:** the per-user route works without elevation, with `dotnet-install` and .NET 9's `AppHostDotNetSearch`. It is also a new failure surface on somebody's first run, which is when they decide whether to keep the application, and it does nothing at all for a machine that already has a runtime.
- **Section 4:** a per-user runtime is patched by nobody. Windows Update does not see it and Microsoft's updater does not know about it, so it would fall to GroupLab to watch for a CVE and swap a runtime under a running application.
- **Section 5:** two packages to build, test and support, and the updater keeping each install on its own kind for ever. Entry 123 section 2.7 found three defects in the single-package updater in one night, so doubling its cases is not small.
- **Section 6: question 36**, recommending not yet, and shrinking the one package instead. The symbol fix is done; dropping OpenCV's 27.3 MB video library is the next thing to measure; trimming is last and would need a full control walk on all three platforms.
- `docs/PHASE1-RESULTS.md` "Entry 133".

Alan suggested offering a light installer beside the full one: it would install only GroupLab and download the .NET runtime if the machine does not already have it. **Measure and propose only; do not build it.** Put this at the end of tonight's queue, after everything else.

1. **Size.** Publish GroupLab framework-dependent for win-x64 (no runtime inside) and report: unpacked size, zip size, and installer size, beside today's self-contained figures (about 311 MB unpacked, about 97 MB installer). Say which parts make up the rest (Avalonia, OpenCV's native library, fonts, samples).
2. **Updates.** The biggest gain may be updates rather than first installs: a framework-dependent nightly update would carry only GroupLab, not the runtime each time. Report the size of an update in each model.
3. **No administrator prompt, ever.** The installer and every update are per user with no elevation (entry 116, entry 119). A machine-wide .NET runtime install needs administrator rights, so that route is out. Check the per-user route instead: the runtime installed into a folder GroupLab owns (for example under `%LOCALAPPDATA%\GroupLab\dotnet` with Microsoft's install script, its download verified by hash), and the application host told to look there (.NET's `AppHostDotNetSearch` / `AppHostRelativeDotNet` settings, available since .NET 9). Say whether it works without elevation on a clean Windows user account, and what happens when the machine already has a suitable runtime.
4. **Keeping the runtime patched.** A self-contained build gets .NET security fixes with each GroupLab build. Say how a separately installed per-user runtime would be kept patched, and by whom.
5. **Cost.** Two packages mean two things to build, test and support, and the updater must keep each install on its own kind. Estimate that honestly.
6. **Recommendation**, as a question in `docs/QUESTIONS-FOR-PLANNING.md` with the figures: build it, or not yet, or instead shrink the single installer (for example by trimming, with what trimming would risk for Avalonia).

---

## 2026-09-21, entry 132: release notes that say what changed, and smaller builds

**Status: actioned in part 2026-09-22.** Done: section 1. **Not done: section 2**, named below.
- **Section 1, done, before the next nightly as section 3 requires.** Notes are built from `Release-note:` trailers alone and nothing is guessed from a subject line. A commit with nothing a person would notice carries no trailer and appears only in a count. The script refuses a note that is only a reference, begins with "Entry", is under eight words, or uses words meaning nothing to a shooter, naming the commit each time; checked against seven notes, six bad and one good, and it refuses all six for the right reason. The rule and its three examples are in `CLAUDE.md`.
- **Section 1.6, done:** nightlies 18 to 26 went out with unreadable notes, so the next nightly opens with a hand written account of what was in them, ten lines in plain words. The published releases are not edited.
- **Section 2, not done:** the build output work. Nothing was deleted from Alan's folders, and `C:\Dev\DEV-CLEANUP-REPORT-2026-09-21.md` was not read or changed. One thing was done towards it: no new `altN` folder was created after this entry arrived, and the rule belongs in `CLAUDE.md` when the rest is built.
- `docs/PHASE1-RESULTS.md` "Entry 132".

## 1. Release notes a tester can read

Alan read the notes for `v0.2.0-nightly.26` and they told him nothing. They read, for example, "Entry 130 item 3.3: doubt travels with the number", "Entry 130 folded, with question 34 and the night's write-up" and "Entry 129 section 3.5.1: the worker decodes with no way out". Commit subject lines are written for the log, not for a person deciding whether to install a build. Keep the entry reference, but every note must say **what changed, in plain words, from the user's side**.

1. **Every commit that changes something a person can see or rely on carries a `Release-note:` trailer**: one or two plain sentences saying what is different for someone using GroupLab, ending with the reference in brackets. For example:
   - `Release-note: When GroupLab finds fewer holes than the shots you fired, it now says so and lists the bulls with nothing on them, instead of reporting a clean result. (Entry 130, 2b.2)`
   - `Release-note: For small calibres such as .22 LR, holes are no longer rejected as too small when you have entered the calibre. (Entry 130, 2b.3)`
   - `Release-note: A blank sheet scanned on a flatbed can now use the scan's own resolution as its scale; GroupLab shows the number and you can refuse it. (Entry 130, 4.1)`
2. `scripts/release-notes.py` builds the notes **only from these trailers**, grouped under New, Fixed and Changed by a `Release-note-kind:` trailer (new, fixed or changed) rather than by guessing from words. Commits without a trailer (notes folding, write-ups, tests, internal refactors, CI) do not appear, except as one closing line: "Plus N internal changes (tests, documentation, build)."
3. The script **fails the nightly** if a trailer is only a reference, starts with "Entry", is shorter than eight words, uses internal jargon that means nothing to a shooter (list the words you check: for example "folded", "gate record", "recorder", "harness", "manifest" unless explained), or breaks the existing rules (em dash, private paths, coordinates, server address). A failed check names the commit so it can be fixed.
4. Add the trailer rule to `CLAUDE.md` in your own words, with the three examples.
5. The same notes appear in the in-application update bar (entry 119 section 5.2), so they must read well there too.
6. Write proper notes for **everything since `v0.2.0-nightly.18`** that a tester would notice, as a hand-written list the next nightly's body starts with ("Since nightly 18"), since those builds went out with unreadable notes. Do not edit the published releases.

## 2. Smaller builds, and no more copies

Alan's `C:\Dev\grouplab` folder is about 11 GB, and about 10 GB of it is build output: nine copies of the Core test build (`Debug`, `Release`, `alt` to `alt7`) at about 685 MB each, and in every build about 675 MB of native libraries for some 30 platforms nobody runs (Linux on ARM, RISC-V, s390x, LoongArch, MIPS, WebAssembly, Mac Catalyst and more).

1. **Stop making new `altN` output folders.** If a build needs to avoid a locked file, reuse one alternate folder (for example `bin/alt`) and clear it before reuse. Say in `CLAUDE.md` that no other output folders are created.
2. **Copy native libraries only for the platforms GroupLab builds and tests on**: Windows x64, Linux x64, macOS (x64 and arm64). Do this in the build (for example restricting runtime identifiers for the test and application projects, or trimming the packages' native assets) without changing the published packages' behaviour, and prove it: CI green on all three platforms, the installer and zip still run with nothing installed, and the Phase 0 gate record unchanged. Report the size of one Core test build before and after.
3. **Do not delete anything in Alan's folders yourself**, including the old `alt` folders; Alan will clear them himself in the morning with the commands in `C:\Dev\DEV-CLEANUP-REPORT-2026-09-21.md`. Do not read or change that report.

## 3. Order

Section 1 goes before any further nightly is published, so the next nightly already has readable notes. Section 2 fits in after entry 131, within tonight's queue under entry 130 section 0 and entry 131 section 0.

**Status: actioned 2026-09-22.** Section 1 done and proved on nightly 27, whose notes read as plain sentences. Section 2.1 done, and one honest admission: `alt8` and `alt9` were created before the rule was written, and `alt9` was reused thereafter. Section 2.2 done: a Core test build goes from 685 MB to 262 MB and an App test build from 705 MB to 278 MB, with the published package unchanged at 205 MB. **One correction was needed:** nightly 27's notes claimed GroupLab "now works out where your group actually landed", which was untrue when published because the solver was connected to nothing. The note is corrected for the next nightly and the published release is not edited.

---

## 2026-09-22, entry 142: a Research section on grouplab.org, thirty articles

**Status: in progress 2026-09-22.** Sections 1 and 2 are done: the page, the navigation link, the article format, the build and its checks. Section 3 is four of the eighteen articles code writes (1, 3, 5 and 6), which are batch 1; the planning session's twelve drafts have not appeared in `C:\Dev\grouplab-research-drafts` yet, and nothing has been imported. Section 4 is not done: **nothing is published**, and batch 1 waits for Alan's review and his words "publish research batch 1". Section 5's constant check is in. The state of all thirty is `docs/RESEARCH.md`.

Written by the planning session at 09:10 UTC on 2026-09-22. Alan approved every decision here.

**Order: this comes after entry 141 sections 1 to 5.** Printing, the CI changes, the queued work, question 38 and Alan's interface priorities come first. Nothing in this entry is pushed while the section 1.5 printing hold is in force, and nothing here touches printing, sheet rendering or the library.

## 1. Decisions

1. The page is called **Research**, at `https://grouplab.org/research/`, with a "Research" link in the top navigation of every page.
2. Byline on every article: "GroupLab project, tested by Alan Hayes, researched and written with Claude", with the date it was written and the date of its data.
3. **Images:** articles may show Alan's range scans and photographs, re-encoded from pixels with every piece of metadata removed. Never read, print or publish GPS, location or timestamp data. Two exclusions stand: the orange commercial target (never shown, never named) and the friend's scan until its consent record is published in the repository. Nothing from `C:\Dev\grouplab-submissions` is used in an article without its consent and a later instruction.
4. **Gear:** articles may name Alan's rifles and optics. Alan will supply a list of optics by class (1x, low power variable, medium power variable, high power variable) for the aim point series.
5. Every standing rule applies to the site text as well: no em dashes, no pseudoscience (barrel harmonics, optimal barrel time, velocity or accuracy nodes), no mention of OnTarget.

## 2. How an article is stored and built

1. Each article is `website/research/<slug>.md` with front matter: title, one or two sentence description for the index, topic group, number, date written, date of data, sample size, status (draft or published), sources, and data files.
2. Every chart built from GroupLab data is made by a script beside it, `website/research/<slug>/figures/*.py`, from data in the repository or from derived CSV files that are committed. A reader can download the CSV behind every chart. Concept charts made from simulation state that they are simulated, with the seed.
3. `website/build.py` renders the articles and an index page grouped by topic, each entry showing the title, the description and a small thumbnail of the article's lead graphic.
4. Every article opens with a short box: what we found, how sure we are (sample size and the main limit), and where the data is. Sources are listed at the end with links.
5. Images of Alan's sheets are re-encoded from pixels at a web size, with no metadata, and checked by a test that fails if any published image carries EXIF, XMP or IPTC data.
6. Add tests: every article in the index has its front matter complete, every CSV it links exists, every figure script runs, and no article text contains an em dash or the banned terms.

## 3. Who writes which

**Code writes** the articles built on GroupLab's own data, code and history, eighteen of them: 1, 2, 3, 5, 6, 7, 9, 10, 12, 17, 18, 19, 20, 24, 25, 26, 27, 28.

**The planning session drafts** the concept and research articles, twelve of them: 4, 8, 11, 13, 14, 15, 16, 21, 22, 23, 29, 30. Drafts arrive in `C:\Dev\grouplab-research-drafts\<slug>\` as `article.md`, figures, their scripts or data, and a sources list. Bring each one into `website/research/`, check every number against the repository and the application, regenerate figures from repository data where it exists, and raise any disagreement in `docs/QUESTIONS-FOR-PLANNING.md` rather than changing a claim silently. Do not write, rename or delete anything in the drafts folder.

| # | Slug | Title | Group | Writer |
|---|---|---|---|---|
| 1 | photo-hole-size | Why a photo cannot tell you your bullet's size | Reading targets | Code |
| 2 | hole-is-not-the-bullet | A bullet hole is not the bullet | Reading targets | Code |
| 3 | primer-comparison | Did the primer matter? A real comparison | Range tests | Code |
| 4 | mean-radius-or-extreme-spread | Mean radius or extreme spread? | Measuring groups | Planning |
| 5 | how-grouplab-reads-a-target | How GroupLab reads a target | Reading targets | Code |
| 6 | wrong-bull | When shots land on the wrong bull | Reading targets | Code |
| 7 | uploads-rebuilt-from-pixels | Every upload is rebuilt from pixels | How GroupLab is built | Code |
| 8 | can-you-see-the-bull | Can you see the bull? Aim points and optics at 100 yards | Range tests | Planning |
| 9 | scans-against-photos | Scans against phone photos: how close is close enough? | Reading targets | Code |
| 10 | safe-updates | How GroupLab updates itself safely | How GroupLab is built | Code |
| 11 | how-many-shots | How many shots do you need? | Measuring groups | Planning |
| 12 | pooling-groups | Pooling groups: when two sheets are one load | Measuring groups | Code |
| 13 | cep-explained | CEP 50 and 90 explained | Measuring groups | Planning |
| 14 | velocity-sd-small-samples | Velocity SD from 5, 10 and 20 shots | Measuring groups | Planning |
| 15 | moa-mils-inches | MOA, mils and inches: one group four ways | Measuring groups | Planning |
| 16 | when-to-adjust-zero | Zeroing: when to adjust and when to leave it | Measuring groups | Planning |
| 17 | one-hole-or-two | One hole or two? | Reading targets | Code |
| 18 | curled-angled-paper | Curled, angled and wrinkled paper | Reading targets | Code |
| 19 | wind-or-rifle | Wind or rifle? | Range tests | Code |
| 20 | blank-sheet-zero | Zeroing on a blank sheet with a hand-drawn cross | Reading targets | Code |
| 21 | photographing-targets | How to photograph a target so it measures well | Guides | Planning |
| 22 | printer-true-size | Does your printer print at true size? | Guides | Planning |
| 23 | scanner-traps | Scanner traps: cropping, DPI and colour | Guides | Planning |
| 24 | choosing-the-markers | Choosing the markers | How GroupLab is built | Code |
| 25 | designing-a-readable-target | Designing a target GroupLab can read | How GroupLab is built | Code |
| 26 | what-grouplab-sends | What GroupLab sends from your computer | How GroupLab is built | Code |
| 27 | nightly-builds | Nightly builds, from commit to installer | How GroupLab is built | Code |
| 28 | smaller-installer | Cutting the installer from 97 MB to 81 MB | How GroupLab is built | Code |
| 29 | aim-points-by-optic-class | Aim points for 1x to high power optics | Range tests | Planning |
| 30 | range-test-log | The range test log | Range tests | Planning |

Articles 8 and 29 wait for Alan's range data. Article 9 follows entry 130 section 2c.

## 4. Publishing

1. Batches of five or six. For each batch, build the site locally, render every new page at phone and desktop widths under `docs/figures/research/`, and stop with STATUS: NEEDS YOU asking Alan to review them. Publish a batch only after Alan sends "publish research batch N". The normal site publishing rule applies after that: `docs/RELEASE-NOTES.md` current, the website workflow run by dispatch, the live check confirmed.
2. Batch 1: the Research page and navigation link, plus articles 1, 3, 5 and 6, and any planning drafts that are ready.
3. Keep a table of all thirty with their state in `docs/RESEARCH.md`.

## 5. Accuracy

Every figure states its sample size. Nothing is called proven that five shots cannot prove. Where GroupLab's own behaviour is described, it describes the build on the site, and a test fails if an article quotes a constant (such as the hole to calibre ratio) that no longer matches the code.

---

## 2026-09-22, entry 141: tonight's work, and printing that must work by morning

**Status: in progress 2026-09-22.** Section 1.1 done, the print path held by tests that need no printer. Section 3 was done before this entry arrived. Sections 1.2 to 1.5, 2, 4, 5 and 6 are not done yet; the progress file is `docs/OVERNIGHT-2026-09-22.md`.

Written by the planning session at 08:25 UTC on 2026-09-22 (02:25 Mountain). Alan has read and approved every decision here.

Work through this in the order given, with `/loop` when Alan starts it, keeping `docs/OVERNIGHT-2026-09-22.md` as the progress file (the same shape as `docs/OVERNIGHT-2026-09-21.md`: a "Next step" line at the top, then a queue table with a state per item, kept current after every commit). Never end a turn to wait for CI; do the next item while it runs. Commit small, with `Release-note:` trailers where a user would notice the change. Every CLAUDE.md rule and every standing security constraint stays in force: no server address in any file, the SSH key by path only and never read, no sudo, no repository settings, no v* tags except the nightly workflow's, nothing from `C:\Dev\grouplab-range-2026-09-20` committed, no GPS, location or timestamp metadata read, and no printing to a real printer or anything that uses paper.

## 1. A hard deadline: printing targets from the newest build, by 09:00 Mountain (15:00 UTC)

Alan is going shooting with a friend on 2026-09-23 and leaves between 12:00 and 13:00 Mountain. He will print his targets from the newest nightly before he leaves. That is the one thing tonight that cannot slip.

1. Early in the night, before the big interface work, make sure the print path is covered by tests that would fail if tonight's changes broke it: for every sheet in the library, and for a designed sheet, render the PDF through the same code `PrintWindow.SavePdf` uses, and check the page count, the page size (letter unless the sheet says otherwise), that the fiducial markers and codes are present, and that the printed scale is exact: a known distance on the sheet measures the same in the PDF to within 0.005 in. Also check the Print dialog path opens and hands off without error through `IOutsideWorld`, never to a real printer.
2. At 07:30 Mountain (13:30 UTC), stop pushing to main. Let CI finish on main's head and the nightly publish. If CI is red for a real reason, fix only that and push once. If you are mid-item at 07:30, park it on a local branch or leave it uncommitted and list it; do not push half an item.
3. When that nightly is published, install it with `scripts/Test-RealUpdate.ps1` or the installer, run the print checks from step 1 against the installed build, save a PDF of the GL-CF25-LTR-D sheet and one other sheet to a scratch folder outside the repository, and measure them.
4. By 09:00 Mountain, stop with a status report. The first line after the status must be: "Print from nightly N", with N the verified build, and the direct download link to that numbered release, not only the rolling one. Say plainly if anything about printing is not right.
5. After that report you may carry on, but nothing that touches printing, sheet rendering or the library is pushed until Alan says he has printed. The rolling nightly link can move; the numbered link in your report is the one he uses.

## 2. CI: fewer runs, no lost nightlies

Approved by Alan. Workflow files only, no repository settings.

1. Push to main only from now on. Stop pushing phase-1. Say in the report what, if anything, still depends on phase-1.
2. `build and test`: add a concurrency group per workflow and branch with cancel-in-progress, so a newer push cancels the older run on the same branch.
3. A guaranteed nightly: add a schedule to `nightly.yml`, once a day at 12:00 UTC, that builds the newest commit on main whose `build and test` succeeded, and does nothing if that commit already has a nightly. The freshness skip stays for the `workflow_run` path. Keep the per-build pre-release, the rolling `nightly` release and the 30-release limit as they are.
4. Prove it: show a run of each path in the report, including one where the schedule finds nothing to do.

## 3. Finish what is already queued

1. Step A5 of the last command: the website republish confirmed live, with the new build id in the meta tag, /releases/ listing the newest nightly, and no rollback in the sync log.
2. Entry 139 section 5: the real update test once a nightly carrying 28a3365 exists. Section 1 step 2 will produce one if nothing sooner does.
3. The calibre best guess snapping to Alan's list. Firearm type on the Equipment screen is rifle or pistol only; Alan does not want shotgun or rimfire now.

## 4. Question 38: approved, build it

Alan approves your recommendation. Where a sheet has enough round single marks to speak for itself, its own marks are the reference for telling one hole from two, and the stated calibre is the fallback. This replaces the first rule of entry 82; say so in `NOTES-FROM-PLANNING.md` beside entry 82.

1. Decide "enough" from the evidence you have (thirteen sheets) and write the number and why in the code comment. Below it, fall back to the stated calibre, and with no calibre, to shape alone as today.
2. The sheet's reference is robust to the doubles it is judging: take it from the marks that agree with each other, never a plain mean of all marks.
3. The calibre guess (section 3.3) must use the same reference, and on a photograph it stays rough as already asked.
4. Tests from generated sheets only, including a sheet where a third of the marks are real doubles. Then re-run the thirteen images from question 38, read only, and report flagged marks per image before and after.
5. `HoleToCalibre` for scans stays unchanged.

## 5. Alan's priorities for the interface, in his words, and what they mean

Alan's biggest wants right now: "improving the UI looks with better layout for information, more consistent text sizes, more graphs and graphics that help you understand the data", and "changes made to make it easier to edit shots and tie shots to various bulls". This is the bulk of the night. Take before and after renders of every screen you change at 1280 by 720 and 2560 by 1440 under `docs/figures/screens/`, and look at each one yourself before you call it done.

### 5.1 Consistent text sizes and layout

1. One type scale for the whole application, defined once in `AppStyles` (for example: caption, body, label, section heading, page heading, headline figure). Five or six sizes, no more. Every `FontSize` in the application comes from it. Add a test that fails on a literal font size anywhere outside the style file.
2. One spacing scale the same way (for example 4, 8, 12, 16, 24, 32), and the same margins and gaps on every screen.
3. Layout for information: on every screen, the most important figure first and largest, related figures grouped under one heading, labels and units aligned, and no text cut off or wrapping mid-number at either size. The analysis panel rebuild from `AnalysisPanel` (queue item 1 of entry 135) is the first screen to do.
4. The amber stays for the one headline figure on a screen. Everything else uses the existing neutral and accent colours.

### 5.2 More graphs and graphics that explain the data

Every graphic must answer one question, and its caption says that question in plain words. No decoration, no 3D, no pseudoscience. Suggested set, build in this order and stop where the evidence or the data runs out:

1. The group plot: the shots, the group centre, the mean radius circle, the extreme spread line between the two widest shots, and the aim point, each switchable, with a small legend.
2. Horizontal and vertical spread: two small strips or histograms beside the plot, answering "is my group wider than it is tall".
3. Shot order: distance from the group centre for each shot in the order fired, where the order is known, answering "did the group open up as I shot". Show nothing when the order is not known rather than inventing one.
4. Sessions over time: mean radius per session for one load, with its uncertainty, answering "is this load getting better or worse".
5. Velocity: where velocities are recorded, the shots with the mean and SD marked, and ES; SD shown with its uncertainty for the sample size.
6. Compare loads already has dot-and-whisker charts; bring them to the same type scale and colours.

### 5.3 Editing shots and tying shots to bulls

This is question 37's control (queue item 6 of entry 135) and the shot editor from entry 131, done together as one piece of work.

1. Select a shot by clicking it; the shot is highlighted on the image, in the shots list and in the review queue at once.
2. Move a shot by dragging it; add one by a click in add mode; delete with the Delete key or a button. Every edit goes through undo and redo.
3. Assign a shot to a bull by dragging it onto the bull, by a bull picker in the shots list, or by keyboard (select, then type the bull number). Select several shots and assign them together.
   - **Amended by entry 143, question 41, on 2026-09-23: drag onto a bull is removed.** A drag always moves the shot and never changes which bull it belongs to, because a mark's position is a measurement and a drag is how it is corrected; nothing else may ride on that gesture. Assignment happens through the bull picker, the keyboard and the multiple-selection assignment. A pointer gesture for assignment, if one is ever wanted, must be a distinct one such as a drag with a modifier held, must leave the hole where it is, and must say in the toast which bull it moved the shot to. It was never built, so nothing was removed from the application; `DragNeverAssignsTests` is what stops it arriving by accident.
4. Say which bulls were aimed at: click bulls to mark them aimed or not aimed, with presets for "every bull", "rows", and "bulls 2 to 5 of each row" style patterns, and a shots-per-bull count. Assignment uses this. Scans 4, 5 and 6's ground truth in entry 120 is the test: show that with the aimed bulls set, the assignments match Alan's table.
5. A shot moved or assigned by hand is marked as manual, shown differently, and never changed by a later re-detection or re-assignment.
6. Every statistic and graphic updates as soon as an edit is made.
7. A review queue item opens the shot it is about, selected and ready to edit.

## 6. After that, in this order

1. Entry 137, drag and drop or paste an image into the main window, if it is not finished.
2. The rest of entry 131 sections 2 to 9 and a final checklist pass.
3. Entry 130 sections 2c and 6b: photographs against scans, and the mounted gate.
4. Performance, entry 130 section 6.
5. Questions 34 and 36 answered with a recommendation.
6. Entry 129 server side, prepared only: the receivers, the intake worker and the ClamAV step built and tested locally, with `install.py` or its equivalent ready. Stop short of anything that needs sudo, the Turnstile secret or a change on the server, and list exactly what Alan and you will run together when he is awake.

## 7. Reports

The 09:00 Mountain print report in section 1.4 is required whatever else is happening. Otherwise report only when you stop. If you stop for any reason before section 1 is done, the report still starts with the state of printing.

---

## 2026-09-22, entry 140: a new image is a new target

**Status: actioned in full 2026-09-22.** Every section is done.
- **Section 1.1 and 1.2, done.** The calibre no longer follows the last sheet, and neither does anything else: a test walks `MarkingState`'s own properties and fails if a field is left set after opening a new image, so a field added later is caught here rather than by somebody opening their second target of the day. The rounds fired, calibre and distance boxes are emptied with it.
- **Section 1.3, done.** "Same setup as the last target" copies the rifle, barrel, load, calibre and distance, names what it would copy beside the button, and is offered only where this sheet has none of them.
- **Section 1.4, done.** Every way out of a sheet, opening an image, opening a marking, opening a saved session and New target, asks first where the sheet holds edits that are not in a saved session. It is a row rather than a dialog, and cancel leaves everything exactly as it was. Unsaved work is decided by comparing the marking's own file against the one last saved, so a save, an undo and a redo is not unsaved work.
- **Section 2, done.** New target, in the header menu and on Ctrl+N, clears the sheet exactly as opening an image does, with a toast whose Undo brings the whole sheet back.
- **Section 3, done.** With no calibre the size comes from the sheet's own marks, which section 1 is what makes reachable. Where most of a sheet would be flagged, one item asks about the calibre instead of one an item a shot: three marks and three fifths of the sheet is "most", so three doubles among fifteen are still raised one by one. Four generated sheets hold it.
- **Section 4, done, and it found two things the entry did not have.** First: "You fired 25 and 15 are marked" was never carried over. It was the sheet's own twenty five bulls, worded as though Alan had said it; that wording is gone and the shortfall stays. Second: stating the **correct** calibre on that photograph flags all fifteen too, because photographed holes measure about one and a half times what scanned ones do, which is **question 38**.

Alan opened `20260920_165624.jpg` (a sheet with 15 shots, one on each of bulls 1 to 15) straight after working on a 25-shot sheet. GroupLab carried the last sheet's facts over. His screenshot shows:

- "Count differs from rounds fired: You fired 25 and 15 are marked. Nothing is marked on bulls 16, 17, 18 ..." The 25 was the previous sheet's rounds fired.
- **Every one of the 15 shots flagged "Possibly two holes"**, with sizes of 2.0 to 2.36 holes (shot 15: diameter 0.412 in, "2.36 holes"). A 6.5 mm hole reads as two holes when it is measured against a smaller calibre, which suggests the previous sheet's calibre was kept too. Confirm whether that is the cause.
- 16 review items on a sheet that has nothing wrong with it. That is the review queue crying wolf, which teaches people to ignore it.

## 1. What resets when an image is opened

1. **Everything that describes one sheet starts empty for a new image**: rounds fired, shots per bull, which bulls were aimed at and sight changes (question 37), the load on each bull, the calibre and its confirmation, the shot distance, the scale, every mark, the review queue, the undo history, excluded and flyer marks, notes, and anything else held per sheet. List every field you reset in the results, found by reading the state, not from memory, and add a test that fails if a new per-sheet field is added without being reset.
2. **Nothing from the previous sheet is used silently.** If something must carry over, it is offered, not applied.
3. **"Same setup as the last target"**: a clearly labelled button, shown after opening a new image when a previous sheet exists, that copies only the equipment and conditions: rifle, barrel, load, calibre and shot distance. Never counts, marks, aimed bulls or anything about where shots landed. What it copies is listed beside it so the person can see what they are accepting.
4. **Unsaved work**: if the current sheet has edits that are not saved as a session, opening another image first asks "Save this target, discard it, or cancel", never losing work and never keeping it attached to the new image.

## 2. A reset of one's own

Add **New target** (Ctrl+N) to the Open menu and to the toolbar area: it clears the current sheet exactly as opening a new image does, with the same save, discard or cancel question, and a toast with Undo (entry 131 section 9).

## 3. The "Possibly two holes" check

1. It must never use a calibre the person has not confirmed for this sheet. Without one, judge doubles against the other holes on the same sheet (a hole about twice the area of its neighbours), not against an assumed calibre.
2. If most holes on a sheet would be flagged, the assumption is wrong, not the holes: raise **one** item asking the person to confirm the calibre, instead of one item per shot.
3. Test it with generated sheets: 15 single holes of one calibre with no calibre stated raise no doubles; the same with a wrong smaller calibre stated raise one calibre question, not fifteen items; a real double among singles is still found.

## 4. Proof

Recreate Alan's case in a test: analyse a 25-shot generated sheet with a calibre and rounds fired set, then open a 15-shot generated sheet, and check that no count, calibre, distance, review item or mark from the first appears on the second. Then run `20260920_165624.jpg` from `C:\Dev\grouplab-range-2026-09-20\photos\` (read only, nothing committed) the same way and report its review queue before and after. A plain `Release-note:` trailer. Put this near the top of the queue: it affects every session Alan runs.

---

