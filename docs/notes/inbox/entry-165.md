# 2026-09-24, entry 165: the analysis screen offers to send the target to the project

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
