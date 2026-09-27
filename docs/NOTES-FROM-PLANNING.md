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

- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 195, 194 of them.

---

## 2026-09-27, entry 238: the angled photographs for request 18 have arrived

**Status: done 2026-09-27.** The measured tilts leave no gap where the results change (36.2 degrees clean, 38.5 not), so no more photographs are asked for; the limit is now 37 degrees; the dim light did no harm. The photographs are not published.

Alan photographed the 6 ARC Dominus K sheet (serial box K; its 600 dpi scan is `6.arc.dominus.k09262026.png`, entry 229) on his desk,
corners held down with round weights, in fairly dim room light, with the Fold 7's stock camera. Files:
`C:\Users\Airwolf\Downloads\Photos-1-001(1)\`, ten JPEGs, 4000 by 3000. Some were saved landscape because the phone turned its
orientation as it tilted; the markers make that irrelevant. Same metadata rules as entry 233 (never read, print or log location or time;
strip and rename before anything is committed). Copy them to `C:\Dev\grouplab-originals\range-2026-09-26\photos-angled\`.

Alan did not label the angles, and does not need to: compute each photo's tilt from the sheet's plane (the homography from the markers,
with the focal length from the camera fields, which is allowed; nothing else from the metadata). The planning session's rough reading from
thumbnails, by the sheet's foreshortening, to check yours against:

| # | file | rough tilt from straight down |
|---|---|---|
| 1 | `20260927_033928.jpg` | about 0 (the reference) |
| 2 | `20260927_033932.jpg` | about 15 to 25 |
| 3 | `20260927_033934.jpg` | about 15 to 25 |
| 4 | `20260927_033937.jpg` | about 55 |
| 5 | `20260927_033940.jpg` | about 55 to 60 |
| 6 | `20260927_033943.jpg` | about 15 to 25, sheet turned a quarter |
| 7 | `20260927_033948.jpg` | about 25 to 35, sheet turned a quarter |
| 8 | `20260927_033952.jpg` | about 50, from the opposite side |
| 9 | `20260927_033955.jpg` | about 55, opposite side |
| 10 | `20260927_034000.jpg` | about 55, opposite side |

If that reading is right there may be nothing between about 35 and 50 degrees, which is where the refusal limit (question 54, now 40)
most likely belongs. Measure first. Only if the gap is real and the result needs it, ask Alan in for-alan.md for two or three more photos
of the same sheet at 40 to 45 degrees, with a simple way to judge the angle (for example: the phone held level with a point about as far
above the table as it is away from the sheet's center gives 45).

Then, per entry 235 section 5: every photo against the scan, shot by shot; holes found and missed; marker reading; quality score; the
limit set from where the results stop agreeing with the scan. Say whether the dim light hurt anything before asking for any retake.

**A second set, same sheet, same desk:** `C:\Users\Airwolf\Downloads\Photos-1-001(2)\`, nine JPEGs, 4000 by 3000, taken a few minutes
after the first set, corners held down with different weights. Same rules. Copy them next to the first set. Rough tilts from thumbnails:

| # | file | rough tilt |
|---|---|---|
| 1 | `20260927_034550.jpg` | about 0 |
| 2 | `20260927_034553.jpg` | about 15 to 25 |
| 3 | `20260927_034555.jpg` | about 20 to 25 |
| 4 | `20260927_034558.jpg` | about 45 to 50 |
| 5 | `20260927_034600.jpg` | about 55 |
| 6 | `20260927_034602.jpg` | about 60 to 65 |
| 7 | `20260927_034604.jpg` | about 65 |
| 8 | `20260927_034605.jpg` | about 65 to 70 |
| 9 | `20260927_034607.jpg` | about 70 |

Together the two sets run from straight down to about 70 degrees, which should bracket the limit on both sides. Treat them as one series of
19 photos of one sheet, report the measured tilt of each, and plot shot position error and holes found against tilt. Ask for more only if
the measured tilts still leave a gap exactly where the results change.

## 2026-09-27, entry 237: the planning session's transcription of both aim point score sheets (to check against yours)

**Status: done 2026-09-27.** The two transcriptions agree in every one of the 207 cells, so no score changed; the article gains the three confirmed cells and the observers' agreement.

Entry 229 section 5.2 asks you to transcribe both score sheets from the originals. Here is the planning session's reading from a full
resolution crop, to compare with yours; where the two differ, the scan decides. Scores: 0 cannot see the center, 1 can see it but not
center on it confidently, 2 can center confidently. All at 100 yd. Columns A to I.

Alan (score sheet version with a Dist column):

| scope | mag | A | B | C | D | E | F | G | H | I |
|---|---|---|---|---|---|---|---|---|---|---|
| Razor HD Gen III 6-36 | 10x | 0 | 1 | 2 | 1 | 2 | 1 | 1 | 2 | 2 |
| | 18x | 1 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| | 25x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| | 36x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| DNT TheOne 7-35 | 10x | 0 | 1 | 2 | 1 | 2 | 2 | 1 | 2 | 1 |
| | 18x | 1 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| | 25x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| | 35x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| Strike Eagle 5-25 | 10x | 0* | 1 | 2 | 1 | 2 | 2 | 1 | 1 | 2 |
| | 18x | 0* | 2 | 2 | 0* | 2 | 2 | 1 | 2 | 2 |
| | 25x | 1 | 2 | 2 | 1 | 2 | 2 | 1 | 2 | 2 |
| PLxC 1-8 | 4x | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 2 |
| | 6x (written over 8x) | 0 | 0 | 1 | 0 | 1 | 0 | 1 | 1 | 2 |
| | 8x (printed as 50 yd, shot at 100) | 0 | 0 | 1 | 1 | 1 | 1 | 1 | 2 | 2 |

\* Written like an 8 (Strike Eagle A at 10x and 18x) and like a 0 with a tail (Strike Eagle D at 18x). **Alan confirmed all three are 0.**


Alan's totals over 14 rows (max 28): A 11, B 19, C 24, D 17, E 24, F 22, G 19, H 25, I 27.

Justin (older version, no Dist column; "max" means the scope's top power; the PLxC rows had 18x and max printed and he wrote 4, 6, 8):

| scope | mag | A | B | C | D | E | F | G | H | I |
|---|---|---|---|---|---|---|---|---|---|---|
| Razor HD Gen III 6-36 | 10x | 0 | 1 | 2 | 0 | 2 | 1 | 2 | 2 | 0 |
| | 18x | 1 | 2 | 2 | 2 | 2 | 2 | 1 | 1 | 1 |
| | 36x | 2 | 2 | 2 | 2 | 2 | 2 | 1 | 1 | 2 |
| DNT TheOne 7-35 | 10x | 2 | 1 | 0 | 1 | 2 | 2 | 0 | 1 | 2 |
| | 18x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 1 |
| | 35x | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| PLxC 1-8 | 4x | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 1 |
| | 6x | 0 | 1 | 1 | 0 | 1 | 2 | 0 | 0 | 2 |
| | 8x | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 |

Justin's totals over 9 rows (max 18): A 9, B 11, C 11, D 9, E 14, F 14, G 8, H 9, I 11.

What the planning session reads from it (the article should test these, not assume them):
1. At 18x and above nearly everything scores 2 for both people; the designs only separate at 10x and below.
2. At 10x on the high power scopes, **E scored 2 in all five cells** (Alan's three scopes, Justin's two); C scored 2 in four of five
   (Justin 0 on the DNT). A, the current bull, scored 0 for Alan on all three scopes at 10x.
3. On the PLxC at 4x to 8x, only the large bull I (and H for Alan) is reliably visible; Justin's 8x row is worse than his 6x row, matching
   the blurry 8x both reported.
4. The two observers disagree in places (A on the DNT at 10x: Alan 0, Justin 2; I on the Razor at 10x: Alan 2, Justin 0). Two people and
   one day is a first look; report agreement between them, not only totals.

## 2026-09-27, entry 235: the first Full backup, requests 32 and 21 closed by Alan's answers, the Fold 7 needs GroupLab back, angled photos coming

**Status: done 2026-09-27, apart from one part; section 4 was done first, with entry 236.** **Not done yet:** section 3, the upload timeouts with sudo, which waits for one server sitting at the end of this run together with entry 241's server half, so that an approval nobody is awake to give holds up nothing else; request 21 stays open until then. Section 5's angled photographs arrived as entry 238 and are compared there.

## 1. The first Full Oracle backup exists (closes request 39)

Alan's screenshot, Boot volume backups: "Auto-backup for instance-20260324-2036 (Boot Volume) via policy: grouplab-daily on 2026-09-27
09:00:00", **Full**, Available, 10 of 47 GB, created 2026-09-27 09:04:31 UTC, **expires 2026-09-29 09:04:27 UTC**. The incremental of
26 September is still listed (expires 2026-09-28).

The Full backup expires after two days. Entry 225's policy was a weekly Full kept two weeks, so either the weekly schedule's retention was
entered in days, or Oracle gave the coinciding daily and weekly runs the daily retention. Either way the server would have no Full backup
from Tuesday until next Sunday. Record the backup in RESTORE.md, close request 39, and open a short request for Alan: in the Oracle
console, Backup Policies, `grouplab-daily`, a screenshot of the schedules (type, period, time, retention for each), with what to change
if the weekly retention is two days (weekly Full, retention 2 weeks; daily Incremental, retention 2 days; stays within the 5 backups
Always Free allows). Explain it in plain words. Do not ask for it urgently; he is busy with photos today.

## 2. Request 32: "The archive is enough"

Alan's answer. Close request 32. No second copy of `C:\Dev\grouplab-submissions` is set up.

## 3. Request 21: do it yourself

Alan: "Go ahead on the upload timeouts." You may now run the server side yourself (sudo is allowed since entry 230): install the
receiver's longer timeout block and the server copy of the include that matches the repository, dry run first, `nginx -t` before the
reload, then the three checks (receiver 400, grouplab.org/targets/ 200, pissinhot.com 200). Touch nothing of pissinhot.com. If anything
answers differently, restore the previous include from the backup the installer makes, reload, and report. Close request 21 with the
output in the panel mirror.

## 4. Put GroupLab back on the Fold 7

Alan uninstalled the Play copy (entry 234) and now has no GroupLab on the phone. Install the newest nightly APK over wireless debugging now
(the release APK signed with the upload key, until entry 234's dev build exists), start it once, and say in for-alan.md that it is back.
If the phone is not reachable, ask Alan in the panel in one line to turn Wireless debugging on.

## 5. Request 18: angled photographs are coming

Alan is taking the 40 to 60 degree photographs today. The planning session suggested the 6 ARC Dominus K sheet (scan
`6.arc.dominus.k09262026.png`, entry 229), one straight-down photo as a reference, then about 40, 45, 50, 55 and 60 degrees, and asked
him to say which angle each photo is. When they arrive, compare each against the scan shot by shot as in entry 233, and let the result set
the refusal angle (question 54). Their metadata rules are the same as entry 233's.

**Update to section 1, same day:** Alan sent the policy's Schedules tab. It is set as intended: Daily, Incremental, 09:00 UTC, 2 days;
Weekly, Full, Sunday 09:00 UTC, 14 days. So the cause is the two schedules firing at the same moment on Sundays: Oracle made one backup,
typed Full but with the daily schedule's 2 day retention. Oracle's documentation does not say what happens when two schedules coincide.
The planning session asked Alan to move the Weekly schedule to Sunday 12:00 UTC and set its retention to 13 days (so at most two Full and
about two or three Incremental backups exist at once, within the 5 Always Free allows). Do not open a request for the policy screenshot;
instead, after Sunday 2026-10-04 12:00 UTC, ask Alan in for-alan.md for one look at Boot Volume Backups to confirm a Full backup dated
4 October that expires about 17 October. Record all of this in RESTORE.md.

**Second update, same day:**
1. Alan made the policy change: Weekly, Full, Sunday 12:00 UTC, 13 days (his screenshot of the Edit schedule page). Daily stays
   Incremental, 09:00 UTC, 2 days.
2. **Request 20 changes.** Alan's scanner is a Brother MFC-J430W, a Letter size flatbed. Store-bought targets are far larger than it, so
   "scan every commercial gridded sheet at 600 dpi" is impossible: remove it from request 20 and from any other request. The research on
   sheets GroupLab did not print uses photographs instead, measured with entry 228's four-point method and per-bull scale, with a ruler or
   a GroupLab scale card laid on the sheet when it has no printed scale. Record the scanner model in the test data provenance.
3. Alan may shoot request 20 next weekend and says it is a lot of rifles. Rewrite request 20 so the minimum useful set comes first and is
   clearly enough on its own (for example the .22 LR subsonic, the .300 Blackout subsonic and supersonic, and the 6.5 Creedmoor, two sheets
   each), with the rest as optional extras, and a printable one-page range plan: sheets to print, order to shoot, what to write on each load
   block.
4. The angled photographs are being taken now at his desk, in fairly dim room light. When they arrive, say whether the light hurt the
   result (quality score, marker reading, holes found against the scan) before asking for any retake.

**Third update, same day (request 20's core set):** Alan has no supersonic .300 Blackout and would have to load it. Do not ask for it.
The .22 LR pair (subsonic and high velocity) is the cleanest speed comparison available: same width, same kind of bullet, different
speed, and it is the rimfire question itself. Core set: **.22 LR subsonic, .22 LR high velocity, .300 Blackout subsonic, and 6.5
Creedmoor (or 6 ARC)**, two sheets each. Optional extras: 8.6 Blackout subsonic, .510 Whisper, the other of 6 ARC and 6.5 Creedmoor.
.300 Blackout supersonic is dropped; the article says so and what it would have added. Note in the method that a .300 Blackout subsonic
and supersonic pair would not isolate speed anyway, since their bullets differ in weight and shape.

## 2026-09-27, entry 234: a separate development build that installs beside the Play copy, and readable logs

**Status: done 2026-09-27, apart from one part.** **Not done yet:** GroupLab Dev on the phone itself. Both devices dropped off the desktop when the script's first, unattended run restarted adb, and request 45 asks Alan to reconnect them; the first nightly after this entry publishes `grouplab-android-dev.apk`, and the script installs it. Built and checked on this machine: the release and development manifests (ids, names, icon, debuggable, the provider's authority), and the log's copy to logcat.

Alan finds installing nightlies over adb much easier than going through Play, and says reading logs from the Play copy was a problem.
The Play copy is signed by Google's app signing key, the nightly APK by the upload key, and both use `org.grouplab.app`, so one cannot be
installed over the other. The Play copy is also a release build, so it is not debuggable: `adb shell run-as` cannot reach its private
files (the app's own log, settings, results), and only what it writes to logcat is visible.

Alan is uninstalling the Play copy from the Fold 7 now, so for the moment you may install nightly APKs over adb as testing needs (no Play
upload for day-to-day testing). Then make that permanent and tidy:

1. **A development build, `org.grouplab.app.dev`**, named "GroupLab Dev" on the phone, with a visibly different icon (a "DEV" band or a
   different color), debuggable, built by the nightly alongside the release AAB and APK and published as `grouplab-android-dev.apk`. It
   installs beside the Play copy, so both can live on the phone at once, and `adb shell run-as org.grouplab.app.dev` can read its log and
   files. Settings in the dev build say plainly that it is a development build.
2. Make sure nothing in the code assumes the package name (file provider authorities, intent filters, the survey and error report
   identity), so the dev build is its own app in every way. Its error reports and survey reports are marked as dev, so they never mix with
   real users' numbers.
3. **A script for driving the phone**, for example `scripts/android/Test-OnPhone.ps1`: connect (mDNS first, then ask Alan for the address
   only if needed), install or update the newest dev APK, start it, take screenshots, pull its own log with run-as and the logcat, and put
   everything in `C:\Dev\grouplab-local\android-<build>\`. Entry 232's steps become this script.
4. Release builds keep logging enough to logcat to diagnose a problem without run-as (errors, analysis timings, no personal data), since
   Play testers' phones will never be debuggable.
5. The Play copy is only needed for testing the Play path itself: once before the closed test, and whenever the release build changes in a
   way the dev build would not show. Put that rule in `docs/ANDROID.md`.
6. Update request 36 in for-alan.md: done, and the Play copy was uninstalled on purpose.

**Update, 2026-09-27:** Alan has uninstalled the Play copy from the Fold 7. Nightly APKs can be installed over adb from now on.

## 2026-09-27, entry 233: phone photos of the same four sheets: the best photo-against-scan test we have

**Status: done 2026-09-27, apart from three parts.** **Not done:** 2.4, the aim card by the four-point method and the per-bull scale (it needs the card's corners and check bars placed by hand, and is next with entry 228's Android work); 2.1's Android half was measured on the desktop at the phone's 8 MP working size, not on the phone; and 2.5's live check for a shadow was not built, a capture tip was instead, because the fix to detection made the shadow cost nothing on these three. 2.3 holds only once the tape tear is deleted: with it, the tear takes a real shot's place in the matching.

Alan photographed the same four sheets he scanned (entry 229) lying on his kitchen counter, with the Fold 7's stock camera app, default
lens and settings, "as a normal user would do". Files: `C:\Users\Airwolf\Downloads\Photos-1-001\` (also `Photos-1-001.zip` beside it),
four JPEGs, 4000 by 3000 (12 MP). Alan's own photos: publishable under his standing consent, like the scans.

Copy them into `C:\Dev\grouplab-originals\range-2026-09-26\photos\` next to the scans. The files carry the phone's EXIF block and the
file names are date and time stamps: never read, print or log location or time metadata; strip all metadata and rename (for example
`dominus-k-photo.jpg`) before anything goes into the repository, a test fixture or an article. Reduced grayscale previews are in
`C:\Dev\grouplab-local\planning\photos-0926\` (local only).

## 1. Which photo is which (from the planning session's look at the previews)

| file | sheet | framing |
|---|---|---|
| `20260926_222550.jpg` | 6 ARC, Dominus K (serial box K) | portrait, nearly straight down, slight keystone (top edge narrower) |
| `20260926_222606.jpg` | 6.5 Creedmoor, Magnus S (serial box C) | portrait, nearly straight down |
| `20260926_222616.jpg` | aim point test card | landscape, sheet turned 90 degrees in the frame |
| `20260926_222626.jpg` | 6 ARC, Magnus S (serial box M) | landscape, sheet turned 90 degrees, rows running top to bottom |

What makes them a good test, all of it normal for real users:
- Strong shadows from Alan's hand and phone across the bottom third of both portrait photos, and across the aim card and the Magnus S
  sheet, running over holes, rings and markers. Detection and marker reading must cope with a shadow edge crossing a bull.
- Overhead indoor light makes the paper mid gray, not white.
- Taped, slightly lifted corners (tape tabs at the top), so the paper is not quite flat.
- Resolution: the sheet fills most of the frame, about 300 pixels an inch, well above the quality score's 150.
- Two photos are rotated a quarter turn.

## 2. What to do with them

1. **Run all three load sheets through the normal pipeline**, desktop and Android, and compare each against its own 600 dpi scan, shot by
   shot. The scan is the reference: map both into sheet coordinates by the markers, pair the shots, and report per shot the position
   difference (mean, largest), and per sheet the difference in group center, extreme spread, mean radius and CEP. Report any shot found in
   one and not the other, and any wrong-bull assignment.
2. **The suppressor comparison from photos:** repeat entry 226 section 2's test on the two 6 ARC photos and say whether the conclusion
   matches the scans. That goes in the suppressor article as a check a reader can reproduce with a phone.
3. **The 6.5 photo** is the wrong-bull case again (entry 229 section 4), now under shadow: it must give the same assignment as the scan.
4. **The aim card photo has no markers.** Use it as the first real test of entry 228's four-point method and per-bull scale: the paper
   corners, the printed 2 in check bar and the 50 mm check bar are known lengths. Measure the four shot groups (A, C, E, I) from the photo
   and from the scan and compare.
5. **Shadows:** if a shadow edge breaks marker reading or hole detection, fix it (for example local contrast normalization before
   thresholding), and add a capture tip in the app and the photographing-targets article ("shade the whole sheet or none of it"). Say
   whether the in-app camera's live checks would have flagged the shadow.
6. **Keep all four as regression fixtures** (metadata stripped, renamed) with the scans as their expected results, and a tolerance for the
   photo-to-scan difference that the tests enforce.
7. **An article section or a short article:** "A phone photo against a flatbed scan, same sheets", with the per-shot differences, one
   overlay figure (photo shots over scan shots), and plain advice. State the numbers as measured, whatever they are.

Do this after entry 232 and before the target generator work in entry 226 section 4.

## 2026-09-27, entry 241: repeated benchmarks, one vote per device, and the survey page now (Alan's questions)

**Status: done 2026-09-27 at Alan's request ahead of 233 to 240, apart from two parts.** **Not done yet:** the server side (the new survey worker, its unit and the site sync that keeps the aggregate in place) is committed and waits to be installed with sudo in the same sitting as request 21, so until then the page's everyone-else half shows nothing; and the Fold 7's result, which request 44 asks Alan to read off the phone, because it was locked. The page and the three devices' section are published.

Alan ran the benchmark on his desktop (nightly 111) as well as the Fold 7 and the Tab S8 Ultra. He asks: how the same device can benchmark
several times and have every run sent; whether every run should be shown or only the best counted; whether each device needs an ID, how
it survives a reinstall, whether that breaks trust, how Steam does it; and he wants the survey page built now with the three results.
The planning session's answers below are the design. Update `docs/SURVEY.md`, the survey question's text (`SurveyReport.WhatIsSent`) and
the article `what-grouplab-sends` to match, and tell Alan in for-alan.md when it is live.

## 1. Every run is sent; the median counts, never the best

1. Every benchmark run is sent (the existing limit of three reports a day per installation stays, and a run the limit holds back is sent
   the next day). A run records the app version, whether it was the sample or the synthetic sheet, and the stage times, as now.
2. On the device: Settings, Sharing shows the device's own runs as a short history (date, version, time), all of them.
3. In the published numbers each device counts **once per app version**, by the **median** of its runs on that version. Not the best run:
   picking the best is the same cherry-picking as quoting a shooter's best group, and it would make every machine look faster than it is.
   Not the mean: one run slowed by something else in the background should not move it much. Show how many runs each median rests on in
   aggregate (for example "median of 1 to 5 runs per device").

## 2. A random installation number, not a device fingerprint

1. Counting a device once needs a stable number. Use a **random number made on the device the first time the survey is switched on**,
   with nothing derived from the hardware, the account, the phone number or the network. It lives in the application's own settings.
2. The server never stores it as sent: it stores a keyed hash of it (HMAC with a secret kept only on the server), so the stored value
   cannot be matched to anything outside GroupLab's survey. Replace today's "salted hash of the installation number and the day", which
   cannot tell that Monday's run and Tuesday's came from the same machine.
3. **It does not try to survive an uninstall.** An update keeps it (the settings are kept); an uninstall and reinstall makes a new one. Making
   it survive would mean fingerprinting the hardware, which is exactly what would break trust, and the cost of not doing so is small: a
   reinstalled machine counts twice, and the medians barely move. On Windows, say in `docs/SURVEY.md` whether the settings folder survives
   the uninstaller, and leave it as it falls.
4. Settings, Sharing gets two buttons: **Reset my survey number** (starts fresh; old runs stay counted under the old number) and **Delete my
   survey reports** (the server removes everything stored under this number from the per-device store; the aggregate is recomputed at the
   next run of the worker). A stable number is what makes a real delete possible; say so in the article.
5. **Trust:** this changes what is sent, so the survey question must say it before anyone sends it: "a random number made by GroupLab for
   this installation, so that repeated runs count once; it is not tied to your device, account or network, and you can reset it or delete
   your reports in Settings." Everyone who already said yes sees the changed text once and confirms, or the survey switches off for them.

## 3. What the server keeps

1. Per device (keyed hash) and app version: the hardware class fields the report already has, the run count, the median and the stage
   medians, and the month of the latest run. No time of day, no address, no individual runs once the median is updated.
2. Per-device records with no run for twelve months are deleted. The thirty-day limit on raw reports stays.
3. The public aggregate is computed from the per-device medians, never from runs, and the "merge any group under 10 into other" rule stays for
   everyone else's devices.

## 4. How Steam does it, for `docs/SURVEY.md`

Valve's own page says the Steam Hardware & Software Survey is monthly, optional and anonymous. In practice a random sample of users is asked
each month, and a user cannot choose to take it; it reports shares of hardware and software, not benchmarks. GroupLab differs on purpose: it
asks once and remembers the answer, lets people rerun the benchmark when they like, and publishes speed, which Steam does not.
Source: https://store.steampowered.com/hwsurvey/En

## 5. The page now, with three results

Alan wants to see the page now even with three results. Build `grouplab.org/survey/` from the aggregate:
1. A section **"The project's own test devices"**, shown by name with Alan's permission because they are his: his desktop (the CPU and memory
   the report holds), the Galaxy Z Fold 7, and the Galaxy Tab S8 Ultra, each with its benchmark median, runs, app version, and the stage
   times. Mark them clearly as the developer's machines, not users.
2. A section for everyone else, which says "not enough reports yet; groups of fewer than 10 are not shown" until there are.
3. Operating system and memory shares, from all reports, obeying the same rule of 10.
4. The date range and the number of reports and devices at the top; a link to what is sent and to Settings, Sharing.
5. Charts in the site's existing chart style, readable on a phone. Linked from the footer and from the first run question's "what is sent".
If Alan's three results are not yet in the aggregate (the old hash scheme may have split them), take them from the devices' own screens
over adb and from the desktop's local survey log, and say which. Publish, and put the link in for-alan.md.

## 2026-09-27, entry 231: the first Play internal testing release, and its two warnings (low priority)

**Status: done 2026-09-27, apart from one part.** **Not done yet:** the request for the Play service account, which the entry puts after the Store work; request 38 is still open, so the plan is in `docs/ANDROID.md` section 12 and the request is written when 38 is done. No mapping file exists to produce (no R8); the symbols zip covers GroupLab's own OpenCV library only.

Alan created the app in the Play Console (package `org.grouplab.app`) and uploaded nightly 110's AAB to internal testing. Play read it as
version code 110 (0.2.0-nightly.110), API 29 and up, target SDK 36, arm64-v8a only, 2 required features. Record that in `docs/ANDROID.md`
and close request 36 once Alan says the Play Store install on the Fold 7 works.

Play showed two warnings, neither blocking:
1. No deobfuscation file. Say in `docs/ANDROID.md` whether the .NET Android build uses R8 on the Java side at all; if it does, produce
   the mapping file in CI alongside the AAB.
2. Native code without debug symbols (OpenCV and the .NET runtime `.so` files). Produce the native debug symbols zip in CI with each AAB,
   so that native crashes in Play's Android vitals are readable.

Both files should be uploaded automatically once Play publishing is automated. Plan that the same way as the Microsoft Store (request 38):
a Google Play service account with release rights only, its key as a repository secret that Alan adds himself, and a workflow step that
uploads each nightly's AAB, mapping and symbols to internal testing. Write the request for Alan in for-alan.md with exact steps, after
the Store work; do not start it before he says the Play Store install works.

The internal testing opt-in link is https://play.google.com/apps/internaltest/4701684356677501640 (only accounts on the testers list can use it). Record it in docs/ANDROID.md.

**Update, 2026-09-27:** Alan installed GroupLab on the Fold 7 from the Play Store through that link, and it opened to the first-run sharing window. Request 36 is done; close it. Note that the Play-installed copy is signed by Google's app signing key, so a nightly APK cannot be installed over it; document how testers move between the two (uninstall first).

## 2026-09-27, entry 230: Alan's answers, and the first Oracle backup is confirmed

**Status: done 2026-09-27.** Section 1: the backup is in `docs/RESTORE.md` without anything that locates the server, request 35 is closed and request 39 asks for the Full backup; the sudo rule in CLAUDE.md is widened with the undo-first condition. Section 2's answers were used in entries 226 and 229 as they came.

## 1. The first Oracle boot volume backup exists

Alan's screenshot of the Oracle console (Boot volume backups, compartment spetsnaz (root), US West San Jose) shows one backup:
"Auto-backup for instance-20260324-2036 (Boot Volume) via policy: grouplab-daily on 2026-09-26 09:00:00", state **Available**, type
Incremental, 10 GB of the 47 GB volume, source Scheduled, created 2026-09-26 09:01:42 UTC, expires 2026-09-28 09:01:39 UTC.

1. Record it in `docs/RESTORE.md` (the whole-server section) and STATE.md. Do not record the instance's IP or anything that locates it.
2. The first weekly full backup is due Sunday 2026-09-27 09:00 UTC. Say in for-alan.md when there is a full backup to rely on, and what
   Alan would look for (a second row, type Full). Until a full backup exists, a restore depends on the incremental chain, which Oracle
   manages; say so in plain words in RESTORE.md.
3. The condition in entry 222 is met: sudo on the server is no longer limited to GroupLab's own files. Everything else stays: nothing that
   belongs to pissinhot.com is touched apart from the approved /targets redirect, `nginx -t` passes before any reload and both sites are
   checked after, and nothing is written into HestiaCP's `conf/web/<domain>/` folders. Before any sudo change outside GroupLab's files,
   write down what will change and how to undo it, in the commit or in RESTORE.md.

## 2. Answers to entries 226 and 229

1. **Zeroing grid:** Alan measured the printout; one small square, one bold square and the whole grid were exactly 0.36 in, 1.80 in and
   5.76 in. The print scale is correct; the redesign in entries 226 and 227 is the whole fix.
2. **"1" means A** (Alan confirmed).
3. **Suppressor sheets:** the Dominus K sheet was shot **first**, then the Magnus S sheet, both at 100 yd, about 20 to 30 minutes apart.
   Nothing else changed (same rifle, load, position and rest). So the remaining confounders are the order (barrel temperature and fouling
   after 25 shots and a 20 to 30 minute cool down), the suppressor swap itself (remounting), and light over half an hour. Say that in the
   article next to the result.
4. **Justin** may be credited by his first name only: "Justin". No surname, anywhere.
5. **Score sheets:** both are in the zip, as described in entry 229. Nothing more to wait for.
6. The PLxC third row: record it as 8x at 100 yd per Alan's description of the test (entry 229 section 5.3).

## 2026-09-27, entry 236: the Galaxy Tab S8 Ultra, one check of the large-screen layout

**Status: done 2026-09-27, at Alan's request ahead of 230, 231, 233 to 235 (with 235 section 4).** DeX was not tried: it is off on the tablet and turning it on is setup. Split screen was checked as a resizable window at half and a third of the width, which is what split screen gives an app. The report is request 43. The addresses the entry names are in no committed file.

Alan offered his Samsung Galaxy Tab S8 Ultra (14.6 in, 16 GB). It is the "expanded" width class in `docs/ANDROID.md` section 7, which
nothing has been checked on yet. Alan is pairing it with adb now (the planning session gave him the adb pair and connect lines in
PowerShell), so it should appear in `adb devices` next to the Fold 7.

Once, in the same sitting as entry 235 section 4 (putting GroupLab back on the Fold 7):
1. Install the newest nightly APK on the tablet over wireless debugging.
2. Check, with screenshots in `C:\Dev\grouplab-local\android-tabs8u-<build>\`: portrait and landscape; the first-run window; the analysis
   screen with the sample scan (the plot should use the space, not sit as a phone-sized column in the middle); Settings; split-screen with
   another app (half and a third of the screen); Samsung DeX if it is on, only if it takes no setup from Alan. Note analysis time and peak
   memory against the Fold 7.
3. Fix what is plainly broken; put layout improvements into the plan rather than doing them now.
4. Report in for-alan.md in five lines or fewer. After this the tablet is used only when a layout change needs it, and the request list
   says so, as for the older phones.

**Update:** paired. `adb devices -l` shows the Fold 7 as `(a LAN address, not recorded)` (SM_F966U1) and the Tab S8 Ultra twice, as
`(a LAN address, not recorded)` and as `(its mDNS name)` (SM_X900): the same tablet over the direct connection and over
mDNS. Always pass `-s` with one serial, and prefer the mDNS name, which survives the port changing. Do not write these addresses into any
committed file.

## 2026-09-27, entry 229: what the planning session saw in the 2026-09-26 scans (read with entry 226)

**Status: done 2026-09-27, apart from one part.** **Not done:** 1.2's reading of the handwritten serial box letter; a person types it as the sheet's label instead. Sections 2, 3 and 5 were done with entry 226 (load blocks, the suppressor test, the transcriptions and the card).

Alan connected his Downloads folder, so the planning session looked at the zip itself
(`C:\Users\Airwolf\Downloads\drive-download-20260927T044320Z-1-001.zip`, six PNGs, each 4958 by 6458, about 600 dpi). This entry settles
some of entry 226's open questions and adds problems the scans show. Reduced grayscale previews are in
`C:\Dev\grouplab-local\planning\range-0926\` (local only, not for the repository). Work from the originals.

The six files:
- `6.arc.dominus.k09262026.png`, `6.arc.magnus09262026.png`, `6.5.creedmoor09262026.png`: the three 5x5 load development sheets.
- `aim test09262026.png`: the aim point test card with Alan's shots on it.
- `aim.test.alan09262026.png`: Alan's score sheet (the newer version, with a Dist column).
- `aim.test.justin09262026.png`: Justin's score sheet (an older version, with no Dist column and "max" rows).

## 1. All three load sheets carry the same serial

All three sheets print `GL-R0T0-384Z-HRBE-M0EW`. Alan printed one sheet three times and wrote K, M and C in the serial box to tell them
apart. People will do this all the time (print a PDF several times, photocopy a sheet).

1. Check what the intake, the application and the archive do with three different scans that share one serial: nothing may merge them,
   dedupe them away, or attach one sheet's shots to another's analysis. Fix whatever does.
2. Treat the serial as identifying the printed design, not a unique physical sheet. Tell the sheets apart by the scan itself (a hash of the
   image and the detected hole pattern), keep the hand-written serial box letter if it can be read, and let the user label each one.
3. When a serial has been seen before with different holes, say so in plain words ("this looks like another copy of a sheet you already
   scanned") and continue.

## 2. The load data (from the load blocks, all dated 9/26/2026, 100 yd)

- Both 6 ARC sheets: 105 gr Aeromatch, 24.2 gr N135, Starline brass, GM205MAR primer, 2.250 in seating depth (the block's field). Notes
  "Dominus K" and "Magnus S". So the load matches exactly and only the suppressor note differs.
- 6.5 Creedmoor: 153.5 gr LRHT, 42.4 gr H4350, Alpha SRP brass, GM205MAR, 2.873 in. Notes: **Magnus S**, so the Magnus S was on the 6.5
  as well. Confirm that load against the earlier 6.5 sheets before pooling.

## 3. The suppressor comparison: first impression, not a result

By eye, one shot per bull on each sheet. On the Dominus K sheet many shots sit high on the outer ring. On the Magnus S sheet many sit low and
to the right. That suggests a vertical shift of perhaps half an inch or more, but do the measurement in entry 226 section 2 and let it
decide. Each sheet has one or two shots well off their bull:
- Dominus K: bull 2 has no hole in it; a hole about 1.5 in above it, near the title line, is almost certainly bull 2's shot. Bull 24 has no
  hole; the hole below it, between 24 and 25, is bull 24's shot.
- Magnus S: bull 5 has no hole; the hole above and to the right, near the top right QR code, is bull 5's. Bull 24 has no hole; the hole
  below it is bull 24's.
Report the test with and without those shots, and say which shots the detector assigned to which bull.

## 4. The 6.5 Creedmoor sheet is a real wrong-bull case

Every shot on the 6.5 sheet landed high, roughly 0.8 to 1 in above its own bull's center, just outside the 1.0 in outer ring (the rifle's
zero on that day, not scatter). With bulls about 1.5 in apart, each shot is **closer to the bull above it** than to its own. Row 1's shots
sit up near the QR codes and the title, and there are no shots below row 5.

So nearest-bull assignment gets 20 of the 25 wrong and makes the group look huge. This is exactly what the "wrong bull" research article is
about, now on a real target.
1. Run it through the pipeline and report what happens today.
2. Assignment should use the whole sheet: when the load block or the sheet says one shot per bull, find the assignment that gives every bull
   one shot with a common offset (for example, test whole-row and whole-column shifts and pick the one with the smallest spread), and show
   the user the chosen offset with a clear "all shots are about 0.9 in high: assigned to the bulls below them" message they can undo.
3. Keep this scan as a regression test, and add it to the wrong-bull article as the real example.
4. It is also a zeroing lesson worth one line in the article: zero first, or the 5x5 sheet cannot tell whose shot is whose.

## 5. The aim point card and score sheets

1. **"1" means A.** The card itself says "3 shots at A and 3 at your favourite". The card shows three shot groups plus one: A (3 holes up and
   left of A), C (one hole in the white center, two up and left), E (3 holes up and left of the square), and I (one large hole, probably
   several shots through one hole: check at full resolution). Measure each group relative to its own aim point, and state that 3 shots a
   design can only show a large difference.
2. **Transcribe both score sheets into the research data** (CSV, one row per scope, magnification, distance, design, score, scorer). A first
   reading, to check against the originals:
   - Alan at 10x (the hardest high power setting): A scored 0 on all three high power scopes (Razor HD, DNT, Strike Eagle). C and E scored
     2 on all three. That is strong evidence the current bull is too fine at 10x, as the visibility rule predicts.
   - PLxC at 4x: only H (1) and I (2) scored above 0. At 6x and 8x, I scored 2 and most others 0 or 1. So at low power only the large bull
     works, again as the rule predicts (4 arcmin at 4x needs about 1 in at 100 yd).
   - Alan's notes: "9/26, 12:27 pm, facing north. G hard to see with reticle in the way, and D. C, F, I good." (In chat he said C or E, edge
     to C. Record both.)
   - Justin's sheet: Razor HD, DNT and PLxC only; his notes say he does not like I and F is his favorite. His Razor 10x row has A, D and I
     at 0.
   - One of Alan's Strike Eagle cells for A looks like "8"; it is almost certainly 0 (the scale is 0 to 2).
3. **The PLxC rows were not a duplicate.** Alan's printed sheet lists PLxC at 4x/100, 8x/100 and **8x/50** (the third row is a 50 yd row).
   Alan wrote 6x over the second row, and he says both of them tested 4x, 6x and 8x at 100 yd, so record the third row as 8x at 100 unless
   he says otherwise. Justin's older sheet had "18x" and "max" printed on the PLxC rows and he wrote 4, 6 and 8 over them. Fix the
   generator so every row's magnification is possible for that scope, and so a change of distance stands out (its own heading, bold).
4. **American spelling.** The card and score sheet print "centre" and "favourite". The project uses American spelling: fix the generator
   and search every generated sheet and document for British spellings.
5. The card's printed sizes let the article state each design's angular size at each magnification next to its scores, which is the
   check of the 3 to 4 arcmin rule this test was for.

## 2026-09-27, entry 228: other people's targets with several bulls, and a scale for each bull (Unholy's suggestion)

**Status: done 2026-09-27 on the desktop, apart from two parts.** **Not done:** 1.5 on Android, which has no way to mark a target by hand at all yet, so the phone half waits for that screen; and 2's measurement at 40 to 60 degrees, which waits on request 18's photographs. It was measured on the three near-straight phone photographs of entry 233 instead. The four-corner method (the rectangle) already existed and was extended rather than replaced.

Credit: suggested by Unholy (also TNA). Credit him by that name in the release notes and the tour, as with his earlier feedback.

When someone analyzes a target GroupLab did not print (a commercial target, a hand-drawn one), there are no markers or codes, so today the
user sets one scale and one aim point. Unholy suggests letting the user place several bulls and assign each shot to its bull. Alan adds that
a photographed target needs a scale near each bull, because a phone photo is taken at an angle and has some distortion, so one scale for the
whole sheet is wrong away from where it was set.

## 1. Several bulls on a non-GroupLab target

1. The user taps to place each bull's aim point, and can move or delete it. Bulls are numbered and color coded.
2. Detected shots are assigned to the nearest bull automatically. The user can reassign any shot by tapping it and choosing a bull, or by
   drawing a lasso around several shots. Shots are drawn in their bull's color, so a wrong assignment is easy to see.
3. Analysis offers the same two views as a GroupLab multi-bull sheet: each bull as its own group, and all bulls pooled as one composite
   group, each shot measured from its own bull's aim point. Reuse the existing pooling code and `docs/STATISTICS.md`'s pooling rule; do not
   write a second one.
4. The layout (bull positions, assignments, scales) is saved with the analysis, and can be saved as a reusable template for that commercial
   target, so the next sheet of the same target only needs the bulls nudged into place.
5. Works the same on desktop and Android, with touch targets big enough for a finger.

## 2. Scale that is right everywhere on a photo

A single scale is only correct for a flat scan. For a photo, offer these, best first, and say in the application which one the analysis
used:

1. **Flatten the whole sheet from four known points (preferred).** The user marks the four corners of something rectangular of known size
   on the target: the paper edge (Letter, A4 or a size they type), the printed border, or four grid intersections. GroupLab computes the
   perspective transform (homography) and removes the angle from the whole sheet, so every bull is measured correctly. This handles the
   angle exactly for flat paper; say so, and say it does not fix curled paper.
2. **A scale at each bull.** Where four points are not available, the user draws a known length near each bull: a ring diameter, a grid
   square, or a ruler lying in the photo. Ask for two lengths at right angles (horizontal and vertical), because an angled photo shrinks one
   direction more than the other; a single length is accepted with a warning. Each shot is measured with its own bull's scale.
3. **One scale for the whole sheet.** Only for flatbed scans; if the image looks like a photo (EXIF camera fields present, or the per-bull
   scales disagree), warn.

Checks the user can see:
- If per-bull scales differ from each other by more than a few percent, or horizontal and vertical differ at one bull, say the photo was
  taken at an angle and suggest the four-point method or a rescan.
- Show the estimated scale uncertainty in the numbers, and carry it into the group size result (a 2 percent scale error is a 2 percent size
  error).
- Lens distortion: modern phone cameras correct most of it in the saved image; measure the remaining error on a GroupLab sheet photographed
  at an angle and at a slant (Code's own test images, and request 18's angle photos when Alan sends them), comparing the four-point and
  per-bull methods against the marker-based result as ground truth. Put the result in the photographing-targets research article.

## 3. Order and tests

1. Do this after entry 227. It builds on the existing manual scale from Unholy's earlier feedback; extend that rather than replacing it.
2. Tests: synthetic images of a known multi-bull target warped by known perspective transforms, checking that the four-point method
   recovers true distances within a stated tolerance and that per-bull scales do better than one global scale.
3. Tour and glossary entries for "perspective correction" and "per-bull scale".

## 2026-09-27, entry 232: drive the Play Store build on the Fold 7 over wireless debugging (do this first if Alan asks)

**Status: done 2026-09-27, ahead of 228 to 231 as Alan asked.** Section 3's first run could not be watched: it had been answered before the run began, so the choices were checked and set in Settings instead. Section 4's Targets screen does not exist on the phone, which is recorded for the plan. The report is request 41.

Alan installed GroupLab from the Play internal test (version code 110) on the Fold 7. It opened to the first-run sharing window. The Fold is
on the same Wi-Fi as the desktop with wireless debugging on, and Alan is at his desk. Drive the app yourself instead of asking him to tap.

1. Connect: `adb devices`; if the Fold is not listed, `adb mdns services` and `adb connect` to the `_adb-tls-connect` address it shows
   (the phone was paired before, so no new pairing code should be needed; the connect port changes every time wireless debugging is turned
   on). Only if that fails, ask Alan in the panel for the IP address and port on the phone's Wireless debugging screen, one line, and wait.
2. Screenshots with `adb exec-out screencap -p > file.png`, taps with `adb shell input tap x y`, scrolling with `adb shell input swipe`.
   Keep the screenshots in `C:\Dev\grouplab-local\android-play-110\`, not in the repository. Take `adb logcat` for the app's process
   during the run.
3. First-run choices, Alan's own (he said his targets may be published and he wants error reports sent): targets **May be published**
   and **Ask me each time**; error reports **Yes**; hardware survey **Yes**. Record what happens after the survey Yes (on 110 the benchmark
   is expected not to be offered: confirm).
4. Then check: the main screen appears; the Targets screen opens; a sample target from the app (or a scan pushed with `adb push` into the
   app's picker location) analyzes; rotation works; no crash, no ANR. Note time to first screen and analysis time.
5. Report in for-alan.md in five lines or fewer, with anything that looked wrong, and add fixes to the plan. Do not uninstall the Play
   build or install a nightly APK over it (the signatures differ).

## 2026-09-27, entry 227: the zeroing grid expectation, the survey window skipping the benchmark, and CEP 99 with a custom percent

**Status: done 2026-09-27.** Section 1 was done with entry 226, whose grid redesign it specified. Section 2's answer is request 40 (the benchmark did not run: Alan's log shows his Yes and a report with no benchmark in it); the flow is fixed on the desktop and the phone. Section 3 is done.

## 1. The zeroing grid (adds to entry 226 section 1)

Alan measured the printout: the print scale is correct. His words: "I was expecting a 1.0 x 1.0 mil grid and the 0.5 mil marks are so
small that there was no way to make them out. I agree that the grid should be redrawn to be an easier size with thicker lines."

So the redesign in entry 226 section 1 goes ahead, with these specifics:
1. The mil grid spans at least plus or minus 1.0 mil at 100 yd (a full 1.0 by 1.0 mil square each side of center, or as much as the page
   allows), with bold lines at every 0.5 mil and the whole mil lines boldest.
2. Coarser fine lines (0.2 or 0.25 mil), or none, rather than 0.1 mil.
3. Lines and labels sized by the visibility rule (at least 3 to 4 arcmin at the lowest magnification the sheet is meant for); the labels
   large enough to read at 6x to 10x at 100 yd.
4. The same treatment for the MOA grids (whole MOA boldest).
5. The scale stated on the sheet and a printed ruler bar, as in entry 226.

## 2. The hardware survey window did not offer the benchmark

On nightly 110 the first-run window asked Alan to take part in the hardware survey. He clicked Yes, the window closed, and he was never
offered the benchmark. He asks: "Did the benchmark run when I clicked yes?"

1. Answer him in `docs/notes/for-alan.md` from the code: does Yes run the benchmark, when, in the background or not, and did it run on his
   PC (check his local log or the survey receiver, without reading anything that identifies him beyond what the survey already sends).
2. Whatever the answer, fix the experience so nobody has to wonder:
   - If Yes includes the benchmark, the window says so before he clicks, and afterward the application shows that it is running (with
     progress and the ability to cancel) and when it finished.
   - If the benchmark is separate, the window offers it (Run now, Later) instead of closing.
   - Either way, Settings gets a place to see whether the survey and benchmark are on, when the benchmark last ran, its results, and a
     "Run the benchmark now" button.
3. Check the same flow on Android.

## 3. CEP 99, and a custom percent

Alan: "add CEP 99 to the analyze page and also add a box that lets you specify the percent as an advanced option near the bottom or
somewhere out of the way."

1. Add CEP 99 as a toggle beside CEP 50, 90 and 95, in the same color family, drawn on the plot and listed in the numbers.
2. Add an advanced option, out of the way (a collapsed "Advanced" section near the bottom of the Analyze page): a box for any percent, for
   example 1 to 99.9, drawn and listed like the others, remembered between sessions.
3. Be honest about small samples: a CEP 99 from 10 or 25 shots is an extrapolation into the tail. Use the same estimator as the other CEPs,
   show its confidence interval (or a short note when the shot count is too small for the chosen percent), and add the glossary tooltip.
   Update the CEP research article to explain why CEP 99 from few shots is uncertain.
4. Tests for the new values against known distributions.

## 2026-09-27, entry 226: after Alan's range day: the zeroing grid's scale, three new scans with a suppressor question, the aim point results, a target generator, large format sheets, and the Oracle and Store items

**Status: done 2026-09-27, apart from three parts.** Sections 1, 2, 3 and 5.2 and 5.3 are done, and 4.2, 4.4 and 6 (6.1 was overtaken by entry 230). **Not done:** 4.1's page on grouplab.org (what it would take is question 62 (b)); the pooling half of 4.3, since a set of separately scanned sheets is still analyzed one sheet at a time, though each sheet's codes carry its place in the set; and 5.1 for the six single large sheets, which cannot be cut without being redrawn as tiles (question 62 (a)); tiled targets print with cut lines. The redesign needed a format change, grid style 2, put to the planning session as question 59; the screenshot rule is question 60 and the ring set is question 61.

Alan came back from his latest range day (message of 2026-09-27). Everything below is from his message. Do sections 1 and 2 first.

## 1. The mil zeroing grid "is not scaled to 1 mil at 100 yards"

Alan: "The mil at 100yd zeroing grid is not scaled to 1 mil at 100 yards. It seems closer to 1.6 mils to 1.8 mils across. I looked at it
with both my Vortex Razor HD 6-36 and DNT 7-35 TOR Mil and both scopes showed that the scaling was vastly off."

**What the planning session found, before any measurement:** `GL-ZERO-MIL-100Y` is a 0.1 mil grid spanning plus or minus 0.8 mil at 100 yd
(half 73.2 mm, 8 divisions a side, bold every 5, labels "0.5" at the bold lines). The whole grid is therefore **1.6 mil across by
design**, which is exactly what Alan saw through both reticles. Unholy's scan of his own print of the same design confirms it: 16 by 16 small squares, bold
lines at 5, labeled 0.5. So the printed scale is very likely right and the design is what failed him:

- nothing on the sheet says what a square is ("each small square 0.1 mil, 0.36 in at 100 yd; bold lines every 0.5 mil");
- the labels are tiny and only say 0.5;
- plus or minus 0.8 mil is too small to zero on: an unzeroed rifle's first shot is often more than 0.8 mil out, off the grid entirely;
- 0.1 mil lines at 100 yd are 0.36 in apart, which blur together at 6x to 10x and are only legible at high power.

Alan has the printout and will measure it (the planning session asked him for one small square, one bold square and the whole grid, in
inches or mm), which settles whether the printer scaled it. Then redesign all four zeroing grids:

1. **Say the scale on the sheet**, large: the unit of a small square and of a bold square, in angle and in inches or cm at the stated
   distance, and "print at 100 percent" with a printed check length (for example a 4 in or 10 cm bar to measure with a ruler, to catch a
   printer that scaled the page).
2. **Cover more angle.** Use the whole printable page, not a square in the middle: at 100 yd a Letter page's printable area is only about
   2 mil across and under 3 mil tall before the markers and codes, and the MOA grid can be widened the same way. Consider coarser squares (0.2 or 0.25 mil, 0.5 or 1 MOA) with
   bold lines at whole mils or whole MOA, labeled at every bold line with the value (0.5, 1.0 ... or 1, 2, 3), because a coarser grid is
   readable at the magnifications people zero at.
3. Apply the visibility rule from the aim point work (section 3): every line and label must subtend at least about 3 to 4 arcmin at the
   lowest magnification the sheet is meant for.
4. Put the zeroing grids through the target generator of section 4 when it exists, so a grid can be made for another distance or optic.

## 2. Three new 25 bull scans and a suppressor question

The files are in `C:\Users\Airwolf\Downloads\drive-download-20260927T044320Z-1-001.zip` (the planning session cannot reach Downloads; you
can). Copy the zip's contents into `C:\Dev\grouplab-originals\range-2026-09-26\` (leave the zip where it is), keep them out of the repository
except what is published, and record them in `samples/PROVENANCE.md`. Alan's standing consent covers publishing his own scans.

1. Two sheets of **6 ARC from the same 18 in AR-15 with the same ammunition**, one shot with a **Thunder Beast Dominus K** suppressor and one
   with a **Magnus S** (Alan named it only as Magnus S). Alan wants to know whether there is a **statistically significant point of impact shift** between the two
   suppressors, and to feature it on the tour and the research pages with these scans as the example.
   - Each shot is measured from its own bull, so compare the two sheets' mean offsets from aim, as vectors: a two-sample Hotelling's T² on the 25
     by 25 shots (or a permutation test if its assumptions look shaky), the shift in inches and mil at the distance, its confidence ellipse, and
     each sheet's own dispersion, so a shift is judged against the spread.
   - Say what the test cannot separate: the order the sheets were shot, time and barrel heat between them, any change of position, rest or
     light, and that one sheet each is one sample of each suppressor. The planning session is asking Alan for the order, the distance, the
     time between, and whether anything else changed; use his answers when they come, and do not wait for them to start.
   - The article states the result plainly either way. "No shift that this test can detect" is a result.
2. A third sheet, **6.5 Creedmoor with the same load data as the earlier 6.5 sheets** (the published sample in `samples/PROVENANCE.md` and any later range sheets of that load),
   so it adds to that load's pooled record. Pool it with them only as `docs/STATISTICS.md`'s pooling rule allows.
3. Alan forgot to photograph them on the backer, which this time was **OSB** rather than corrugated plastic. Record the backer anyway (it
   matters for hole appearance and is a field in the load block).
4. These are real 25 bull scans: run them through the normal pipeline, and report anything the detector got wrong as ordinary defects.

## 3. The aim point test results

Alan and his friend **Justin** ran the aim point test card on that range day at 100 yd. Record in the research notes, with the score sheets if
they are in the zip:

- **Optics:** Alan used the Razor HD Gen III 6-36, the DNT TheOne 7-35 TOR, the Primary Arms PLxC 1-8, and the Strike Eagle 5-25 (Justin did
  not test the Strike Eagle).
- **The PLxC:** the score sheet said to test it at 4x, then 8x, then 8x again. **That is a mistake on the sheet: the second should have been
  6x.** Both tested 4x, 6x and 8x. At 8x the image was blurry at 100 yd and targets were noticeably harder to see than at 6x. Fix the sheet
  generator so a magnification is never repeated, and say where the repeat came from.
- **Shots:** Alan shot 3 shots at each of four designs; he wrote "1, C, E, and I" (the planning session is asking whether "1" means A). He thinks
  **C or E** are the best, the edge probably to **C**. Justin disliked **I**; his favorite was **F**.
- **Reticles covering the aim point:** the Razor HD's small center crosshair covered the centers of **D** and **G**, which were very hard to see.
  The DNT has only a small center dot and did not cover them. So a design whose center is a small feature fails under a crosshair reticle
  whatever the glass. Put this into the ring set decision.
- Measure the shot groups on the card if they were scanned, per design and shooter, and say whether any design grouped measurably better. With
  3 shots a design, say how little that can show.
- Justin is to be credited only as Alan says; the planning session is asking.

## 4. A target generator (new feature, after the zeroing grid fix)

Alan's idea: an interactive generator that asks the distance, the scope, its magnification range (down to a 1x red dot) and how many shots,
then produces a target sized and shaped for that optic at that distance, scaled to be easy to see, and as many sheets as the shot count needs
for a composite group. The QR code says how many shots to expect, and how the sheets combine when they are tiled on one board or scanned
separately.

1. In the application's Targets screen, and as a page on grouplab.org if it can share the same code (say what that would take).
2. Size every feature by the visibility rule (at least about 3 to 4 arcmin at the lowest magnification chosen, and a 1x dot's own size for red
   dots, sizing a ring as a multiple of the dot), choose the bull design from section 3's results, and never put a small feature at the center
   where a crosshair will cover it.
3. The shot count decides bulls per sheet and the number of sheets; the codes carry the sheet's place in the set and the expected total, and
   the analysis pools the set as one composite group.
4. Keep the fixed library for people who just want a sheet.

## 5. Large format targets are not practical to scan whole

Alan: a large format sheet from a plotter cannot go on a flatbed scanner, and photographing it from far enough away may exceed what a phone
camera can resolve across the whole sheet and all its codes.

1. For any sheet larger than the flatbed sizes the library supports, print **cut lines** that divide it into scanner sized pieces, with
   markers and a code on every piece so each piece registers and is identified on its own and the pieces pool by the same rule as tiled
   sheets. No bull crosses a cut line.
2. Work out the photograph limit honestly: for each large format size, the pixels an inch a 12 MP and a 50 MP phone photo gives when the
   whole sheet fills the frame, against the quality score's levels and the smallest marker and code that must read. Say which sizes can be
   photographed whole and which must be cut or tiled, and have the application say so.
3. Prefer tiled Letter or A4 pages (which the library already offers for long range) as the default for large targets, and say so where
   large format is offered.

## 6. Oracle and the Store

1. The first Oracle boot volume backup was due 2026-09-26 09:00 UTC; the planning session has asked Alan to check the console. Keep sudo to
   GroupLab's files until he confirms.
2. Request 38 (the Microsoft Store) and request 36 (the Play Console) are his, when he has time.

## 2026-09-25, entry 225: request 35 step 3 done: the server's boot volume has a daily backup policy

**Status: done 2026-09-26, apart from the proof.** The whole-server restore, the crash-consistency note and the report's line about the Oracle console are in `docs/RESTORE.md` and the weekly line. Closing request 35 and widening sudo wait on Alan confirming the first boot volume backup in the console, after 2026-09-26 09:00 UTC; request 35 says so.


Alan set it up in the Oracle Cloud console on 2026-09-25, with the planning session. What his screenshot shows:

- The instance's only volume is its 47 GB boot volume (Ubuntu 24.04 Minimal, aarch64, compartment "spetsnaz (root)", region US West,
  San Jose). No block volumes are attached. There were no earlier volume backups, so the Always Free allowance of five is untouched.
- A custom backup policy `grouplab-daily`: incremental daily kept 2 days, full weekly on Sunday kept 2 weeks, both at 09:00 UTC. At most
  four backups exist at a time.
- The boot volume's **Backup policy** reads `grouplab-daily`, and **Upcoming scheduled backups** lists 2026-09-26 09:00 UTC and
  2026-09-27 09:00 UTC (twice, the daily and the weekly full).

So request 35 is complete except for proof. The first backup should exist after 2026-09-26 09:00 UTC. Alan will confirm it from the
console; until he does, keep sudo to GroupLab's own files. After he confirms, widen it as entry 222 section 6.2 allows, and:

1. Close request 35.
2. In `docs/RESTORE.md`, the whole-server restore: create a boot volume from a boot volume backup in the console, then **Replace boot
   volume** on the instance (the console's own button, which Alan's screenshot shows on the instance's Storage tab), with what is lost
   (anything written since the backup) and what to check after (both sites, the workers, the timers).
3. Record in `docs/RESTORE.md` that these backups are crash consistent, like pulling the power: fine for the web sites and HestiaCP, and
   MySQL recovers on start as it would after a power cut.
4. The weekly check in the automation report cannot see the Oracle console. Say so in `docs/RESTORE.md`, and add a line to the weekly
   report reminding Alan that the Oracle backups are checked by him in the console if he ever wants to, not by the report.

---

## 2026-09-25, entry 224: requests 35 steps 1 and 2 and request 36 steps 1 and 2 done; the Microsoft Store with automatic releases; Windows signing on hold

**Status: done 2026-09-26, apart from two waits.** Section 1.3's widening of sudo waits on Alan confirming the first Oracle backup (entry 225). Section 1.4: nightly 109 built and signed the APK and AAB but did not attach them, because neither upload list named them; fixed, and the next nightly carries them, after which request 36 names the AAB. Section 3's Store automation is built and waits on request 38, Alan's account, name and keys.


## 1. What Alan did

1. **Request 35 step 1:** `gh repo create oRAirwolf/grouplab-backups --private ...` printed `Created repository oRAirwolf/grouplab-backups`.
   Point the nightly backup at it, run it once now, run the restore test against it, and say both passed.
2. **Request 35 step 2:** Alan made the fine-grained token and ran `sudo /usr/local/sbin/grouplab-set-archive-token`, which printed
   `Written to /etc/grouplab/archive-token, owned by root, mode 600.` **Its closing lines are wrong:** it said "The error worker picks it up
   on its next run" and suggested starting and tailing `grouplab-error-worker`. That is the error worker's script text copied into the archive
   one. Fix the wording to name the archive worker. Then start the archive worker yourself, check its log shows it read the token and can
   reach the archive (an upload of nothing, or listing the releases), and say so.
3. **Request 35 step 3,** the Oracle boot volume backup policy: Alan is doing it now with the planning session's help. The planning session
   told him: a custom policy, daily incremental keep 2 and weekly full keep 2, which stays inside the Always Free limit of five volume backups
   (backups do not count against the 200 GB and are not charged on an Always Free account; a sixth backup simply fails to be created). Write
   the restore procedure for a whole-server restore from a boot volume backup into `docs/RESTORE.md`. When Alan confirms the first backup
   exists, widen Code's sudo use as entry 222 section 6.2 allows.
4. **Request 36 steps 1 and 2:** the upload keystore is `C:\Dev\keys\grouplab-upload.jks` (alias `grouplab-upload`, RSA 4096, CN=Alan,
   OU=GroupLab, O=GroupLab, L=Centennial, ST=CO, C=US, 10000 days), and `ANDROID_UPLOAD_KEYSTORE`, `ANDROID_UPLOAD_KEYSTORE_PASSWORD` and
   `ANDROID_UPLOAD_KEY_PASSWORD` are set in the repository's secrets. Never read the key file or ask for the password; CI has what it needs.
   Confirm the next nightly carries a signed `grouplab-android.aab`, then rewrite request 36 as step 3 only (the Play Console), with the
   exact AAB to upload.

## 2. Windows signing: on hold

Alan: "As of right now, nobody is getting windows smart screen warnings. Lets hold off for now." Close request 37 as "not yet"; keep
`docs/RELEASE-PLAN.md`'s comparison for when it is revisited.

## 3. The Microsoft Store, with new releases pushed to it automatically

Alan: "I do want to setup the Microsoft store version and new releases should be automatically pushed to it."

1. **An MSIX package** of GroupLab built in CI beside the existing packages. The Store re-signs it, so no certificate is needed for the
   Store copy. The Store build switches GroupLab's own updater off (the Store updates it) and says so in Settings.
2. **What is pushed, and when:** stable releases, when Alan asks for one by name as today, go to the Store's public listing automatically.
   If a beta train exists by then, it can go to a Store package flight for testers; nightlies stay on GitHub only. Say if a different split is
   better.
3. **The automation:** publish from the release workflow with Microsoft's Store developer tooling (the `msstore` CLI or the Store submission
   API), authenticated with an Entra ID app registration that Partner Center trusts as a Manager. Store its tenant id, client id, client
   secret and seller id as repository secrets set by Alan. Research the current method and its exact steps; Alan administers Entra ID for a
   living, so the app registration steps can be brief.
4. **What only Alan can do, as one request:** create his free individual Microsoft Store developer account in Partner Center (identity
   verification), reserve the name GroupLab, complete the first submission's one-time parts (age rating questionnaire, store listing text and
   screenshots that you prepare, the privacy policy URL `https://grouplab.org/research/what-grouplab-sends/`), create the Entra app
   registration and add it in Partner Center, and set the four secrets. The first submission may have to be made by hand in Partner Center;
   everything after it is automatic.
5. Store listing text, screenshots in both themes, and the feature list come from you, ready to paste, in `docs/store/`.
6. Keep the Store in `docs/RELEASE-PLAN.md` and the minimums table (the Store requires Windows 10 version 1809 or later for MSIX if that is
   higher than .NET's floor; check).

## 4. A note on secrets

Alan pasted the Android keystore password into the planning chat and asked for it to be saved. The planning session declined to store it
anywhere (memory, project documents, the repository), because it is already in Alan's Bitwarden and in the repository's secrets, which is
everything that needs it. Nothing to do; do not look for it.

---

## 2026-09-25, entry 223: request 34 is done

**Status: done 2026-09-25.** Request 34 closed. The survey is open: `surveyOpen` and the receiver's `OPEN` are true, the article what-grouplab-sends lists it as the fifth thing sent, and the tour's and the guide's Settings describe the one Sharing section. The empty POST is checked over curl once the site has published.


Alan ran request 34's reload and checks on 2026-09-25, twice. The first time: `200`, `200`, `400` for the empty survey POST, and the survey
timer listed with its next run. The second time the survey POST answered `503`, which the planning session confirmed is the receiver's
own "closed" answer after 5160a77 published (the survey refuses everything until it is opened), not a fault. Close request 34. Open the
survey when you judge it ready, and then the empty POST should answer `400` again; check that yourself over curl.

---

## 2026-09-25, entry 222: as much automation as possible: the server archives by itself, nightly backups to GitHub, cleanup with a safety net, and Code may use sudo

**Status: done 2026-09-25, apart from what waits on request 35**, Alan's one sitting: the backups repository (so backups are kept on this computer until it exists), the archive token (so submissions wait in ready until it is set), and the Oracle boot volume backups (so sudo stays limited to GroupLab's own files and nginx is not reloaded by Code; request 34's reload stays Alan's). Section 1: ubuntu's sudo is already passwordless, so no sudoers line is needed. Everything else is built, run once, and in `docs/RESTORE.md`.


Alan, 2026-09-25, in his words: "I want to do the busy work as little as possible and want you and code to do as much as possible without
me. I do want to make sure that things like old files and unneeded files are deleted from my PC and server. I also want to make sure that
we have a backup in place every so often so if there is a problem where you delete something you should not have ... we have a backup we
can recover from. Basically, I want as much automation as possible."

Do this after entry 220 (the pull fix). Parts of it replace parts of 215 to 218, as marked.

## 1. Standing rules that change

1. **Code may run sudo on the server**, over ssh as `ubuntu@ssh.pissinhot.com` with the key passed by path, exactly as the pull script
   already does. Alan: "You can run sudo commands for all I care if there is a way to allow it." Find what `ubuntu`'s sudo allows without
   a password today; if it is not enough for GroupLab's installs, write the one sudoers line needed as a request, limited to what GroupLab
   needs if a limit is practical. From then on **Code runs the server installs, checks and reloads itself** instead of writing them as
   requests. What stays exactly as it was:
   - never read, copy, print or move the key file, and never open `C:\Dev\keys`;
   - the server IP never appears in any file, commit, log or report;
   - nothing belonging to pissinhot.com is touched, apart from the approved `/targets` redirect;
   - the Turnstile secret and the GitHub tokens are typed by Alan into the set-secret scripts; Code never sees them;
   - backups never go into a HestiaCP `conf/web/<domain>/` folder; `nginx -t` always passes before any reload, and both sites are checked
     afterward;
   - every server change is announced in `panel.md` with what was run and what it printed, and anything unexpected stops and is reported.
2. **The planning session may run git commands** in the repository (entry 221's apology stands for the lock it left; Alan does not mind git
   being used). Nothing to do.
3. **Alan's busy work is the thing to minimise.** A request to Alan is for what only he can do: a secret, an identity check, a physical
   device, a decision. Everything else Code does.

## 2. The server archives submissions by itself (replaces the PC's part in entries 215 to 218)

Alan: "I would rather the automation occur outside my desktop." So the archive step moves to the server:

1. A new worker (or the intake worker's next stage), after a submission reaches `ready`, zips it exactly as the PC pull does today, uploads
   it to the month's release in the private `grouplab-submissions-archive`, downloads it back and compares the SHA-256, updates the release's
   `manifest.json`, and only then deletes the folder from the server. A failure leaves it in `ready` and is retried, and after a set number of
   failures it is reported (an error report issue is a good channel, since that path already exists and reaches Code).
2. It needs a fine-grained GitHub token limited to that one repository with **Contents: Read and write** (releases need it) and nothing
   else, typed by Alan into a `grouplab-set-archive-token` script like the error worker's. That is his one step. Write the exact GitHub
   clicks as entry 194 section 5 did.
3. **The copy on the PC** comes from the archive instead of from the server: a scheduled task on the PC (or the start of each Code run)
   downloads any archived submission not yet in `C:\Dev\grouplab-submissions` and verifies it against the manifest. So there are still two
   copies, and the PC is never needed for anything to leave the server.
4. The PC pull (`Get-TargetSubmissions.ps1`) stays as a manual fallback. Once the server worker has run cleanly, the backlog (the 9 on
   grouplab.org and the 18 on pissinhot.com) is archived by it or by one run of the fixed pull; Code runs that itself now that it may.

## 3. Nightly backups to a private GitHub repository

Alan asked for the backup to go to another GitHub repository. Proposed `oRAirwolf/grouplab-backups`, private, which Alan creates (Code does
not create repositories).

1. **What is backed up, nightly:** the whole repository as a git bundle of every branch and tag (so history survives a bad force push or
   a deleted branch), plus the files git does not hold that matter: `docs/notes/panel.md` and anything else local only in the repository,
   `C:\Dev\grouplab-local`, `C:\Dev\grouplab-originals`, and the `.claude` settings in the repository. **Never** `C:\Dev\keys`, the SSH key,
   any token or secret file, build outputs (`bin`, `obj`, `out`), or `%TEMP%`. `C:\Dev\grouplab-submissions` is covered by the archive and
   the PC sync of section 2, so it is left out to avoid a third copy; say if you disagree. Nothing from `C:\Dev\grouplab-site`.
2. **How:** one zip a night as a release asset, like the archive (release assets can be deleted one by one and are not metered; each under
   2 GB, so split if ever needed), with a manifest of every file and its SHA-256. Keep 7 daily, 4 weekly and 6 monthly; older ones are
   deleted by the same run and recorded in `STORAGE.md`, with a budget there.
3. **Where it runs:** a Windows scheduled task on Alan's PC, since the files are here, set up by Code without Alan (a task that runs as
   Alan when he is logged on is enough; say if it needs his password to run while logged off, and prefer the version that does not).
4. **A restore test**, weekly: download the newest backup, check every file against its manifest, and restore the bundle into a temporary
   clone and compare it with the remote. Report failures through the error report path so they reach Code.
5. **A written restore procedure** in `docs/RESTORE.md`, short enough to follow in a panic.
6. Encryption is not added for now: the repository is private, as the archive is. Say if you think it should be, and what it would cost
   Alan to hold the key.

## 4. Cleanup with a safety net, on the PC and the server

1. **The PC.** A scheduled cleanup, weekly, that removes only what it knows is generated: build outputs (`bin`, `obj`, `out` older than the
   newest), test leftovers in `%TEMP%` (as entry 179 does), downloaded APKs and spike builds, scratch folders from Code's own sessions, old
   log files beyond a set age, and anything else Code itself created that has no further use. It never touches Alan's own files outside
   those, `C:\Dev\grouplab-site`, `C:\Dev\keys`, `Downloads`, or the submissions and originals folders.
2. **The safety net.** Anything the cleanup removes that is not plainly regenerable (build output and `%TEMP%` test files are) goes first
   into `C:\Dev\grouplab-trash\<date>\` and is deleted from there after 14 days, and never before a successful nightly backup has run
   since. Code uses the same rule for any deletion it makes by hand on the PC.
3. **The server.** The retention rules of entry 215 section 3 and 216 section 1 stand; add the same log and a line in `STORAGE.md` of what the
   workers deleted each week.
4. A short weekly line in for-alan.md, not a request: what was backed up, cleaned and archived that week, and anything that failed.

## 5. Alan's one sitting for all of this

Gather every step only he can do into one request: create `grouplab-backups`; create the archive token and type it into the set-token
script (Code runs everything else on the server now); approve whatever the scheduled tasks need, if anything. Write it once the parts it
serves are built, and the planning session will walk him through it.

## 6. The rule above all the others: nothing Code or the planning session can change goes without a backup

Alan, the same evening: "The most important thing to me is that anything that you have access to delete or change, that there are backups
in place to minimize the damage if something bad happens." So this section outranks every other part of this entry, and it is written into
CLAUDE.md as a standing rule:

1. **Inventory.** List in `docs/RESTORE.md` everything Code or the planning session can delete or change, and against each, its backup,
   how often, where, and how to restore it. At least: the repository and its local only files; `C:\Dev\grouplab-local`,
   `-originals` and `-submissions`; the GitHub repositories (`grouplab`, `grouplab-crash-reports`, `grouplab-submissions-archive`,
   `grouplab-testdata`, `grouplab-backups`) including releases and issues; the server's GroupLab files (scripts, units, the nginx include,
   `.user.ini`, the private folders) and, now that Code may use sudo there, **the server as a whole**, pissinhot.com included. Anything with no
   backup is a gap, listed as such, and closed before Code acts on it.
2. **The server as a whole.** The GroupLab parts can be rebuilt from the repository, but a mistake made with sudo could reach pissinhot.com.
   Find what already protects it: HestiaCP's own scheduled user backups (where they are kept, how many, and whether any copy leaves the
   machine) and Oracle Cloud's boot volume backups (a backup policy on the instance's boot volume; the free tier includes a number of volume
   backups). Recommend the smallest arrangement that gives a whole-server restore point from the last day or two that lives off the machine,
   and put the steps only Alan can do (for example turning on a boot volume backup policy in the Oracle Cloud console) into the one sitting
   of section 5. Until a whole-server backup exists, Code's sudo is limited to GroupLab's own files and the installer, as today.
3. **Before anything destructive**, Code checks that the newest backup covering it is less than a day old and passed its last restore test;
   if not, it makes one first (a fresh backup run, or for a server file a dated copy outside the HestiaCP folders, as the installer does).
4. **GitHub.** Issues in `grouplab-crash-reports` are exported to the nightly backup (their text and comments); the archive's release assets
   are backed by the PC copy (section 2.3) and nothing is deleted from the archive unless the PC copy verifies (entry 217); the main repository's
   history is in the nightly bundle; nothing is force pushed to `main` ever.
5. **Proof, not assumption.** The weekly restore test of section 3.4 covers the server backup too, as far as it can be checked without
   restoring a whole machine (the newest HestiaCP backup file exists, is recent and lists the expected contents; the newest boot volume backup
   exists and is recent). The weekly line in for-alan.md says so in plain words.

---

## 2026-09-25, entry 221: a stale git lock from the planning session, already moved aside

**Status: done 2026-09-25**: the renamed lock file was empty and has been deleted.


At about 10:47 UTC on 2026-09-25 the planning session ran `git status` in this repository from its own shell, which it should not do and will
not do again. It left an empty `.git/index.lock` that its shell could not delete. It has been renamed to
`.git/index.lock.stale-from-cowork-status`, so it cannot block a commit. If a git command reported "index.lock exists" around that time, that
was the cause; retry it. The renamed empty file is harmless; delete it whenever convenient. Nothing else in the repository was touched.

---

## 2026-09-25, entry 220: request 31's pull stopped at the archive; fix it first, then it runs again

**Status: done 2026-09-25**, every section. Confirmed from the code: removal follows the archive in the pull's last loop, and the first archive call threw, so nothing left the server. One helper, `scripts/NativeCommand.ps1`, runs every program in the four scripts; tested under Windows PowerShell 5.1 and PowerShell 7 here and in CI. Dry runs write nothing and say what they would do. Artifacts: both figures were true; 44 GB freed, retention now a day. The crash issues are reported in PHASE1-RESULTS.md. Request 31 rewritten for the rerun.


**Do this before anything else in the roadmap.** Other people's photographs are waiting on the server for it.

## 1. What happened

Alan ran request 31 on 2026-09-25. The server side went cleanly: `--intake`, `--errors` and `--survey` all ended `done`, the first
replaced the nginx include (backup in `/home/airwolf/backups/grouplab.org/config/...20260925-043724.bak`), the other two found it
current, the survey folders, worker and timer were created, and `nginx -t` passed. (The reload and the curl checks are still for Alan
to run; he has them.)

The grouplab.org dry run listed 3 new and 6 to archive, and **wrote `docs/notes/STORAGE.md` even though it was a dry run**.

The real run pulled 2026-09-25_2eeac6a3, 43dbb982 and dd6e3543 (15.2 MB each, all checksums match), then stopped:

```
gh.exe : release not found
At C:\Dev\grouplab\scripts\SubmissionArchive.ps1:71 char:9
+         & gh release view $tag -R $Repo 2>$null | Out-Null
    + CategoryInfo          : NotSpecified: (release not found:String) [], RemoteException
    + FullyQualifiedErrorId : NativeCommandError
```

That is the Windows PowerShell 5.1 behavior `Get-TargetSubmissions.ps1` itself already guards against at lines 253 and 362: with
`$ErrorActionPreference = 'Stop'`, a native command writing to stderr becomes a terminating error even with `2>$null`. `gh release view`
on a month with no release yet writes "release not found", so the very first archive call threw, and the script ended there. As far as
the planning session can tell, nothing was removed from the server (removal comes after the archive), and all 9 are here. Confirm that
from the code path, and say so.

## 2. What to do

1. Every native call in `SubmissionArchive.ps1`, `Get-TargetSubmissions.ps1`, `Remove-ReadSubmissions.ps1`, `Test-SubmissionsArchive.ps1`
   and the storage ledger's PowerShell side runs with stderr non-fatal and is judged by `$LASTEXITCODE` only, the way lines 253 and 362
   already do it. One helper, used everywhere, rather than the fix repeated.
2. **Test the scripts under Windows PowerShell 5.1 as well as PowerShell 7**, since 5.1 is what Alan's shell runs. The test that would have
   caught this: an archive run against a month with no release yet. If CI cannot run 5.1, run it on this machine before handing the request
   back, and say you did.
3. **A dry run changes nothing:** with `-WhatIf`, `STORAGE.md` is not written (print what it would say instead).
4. With the dry run, say what would be removed: "would archive and then remove N from the server", rather than "0 removed".
5. **The Actions artifacts:** `STORAGE.md` shows 81.7 GB in 954 unexpired artifacts against a 5 GB budget, after entries 215 to 217 said 140
   GB had been freed. Say which is true, set the artifact retention short in the workflows if it is not already (per upload, `retention-days`),
   and free the oldest as entry 217 section 3 allows. They are all disposable build output.
6. **The crash reports repository has 3 issues.** Read them as entry 194 section 4 says, and report them in plain words.
7. Then rewrite request 31's step 2 for a rerun: the same two pulls with their dry runs, which will now archive the 9 on grouplab.org and the
   pissinhot.com backlog, then `Test-SubmissionsArchive.ps1`. The server steps are done and must not be repeated. The planning session checks
   it before Alan runs it.

---

## 2026-09-25, entry 219: a standing roadmap for Android and the desktop, so work does not wait on the next entry

**Status: standing, taken up 2026-09-25.** The roadmap is in `docs/notes/STATE.md` in place of "The next three", each item with its state, and is worked through without waiting for an entry. A1, the working resolution in Core, is under way.

Alan asked whether any work is planned for the Android and Windows builds. Honestly, beyond the queued entries, none was: STATE.md's next
item is "the capture screen's CameraX spike on the Fold 7, when an entry asks for it". This entry asks for it, and for what follows, as a
standing order of work. Do it after entries 215 to 218. When an item finishes, carry on to the next without waiting for a new entry; stop
only for a request to Alan (batched, as entry 212 says) or a decision that is his. Keep this list, with each item's state, in STATE.md in
place of "The next three".

## 1. Android, in order (the Fold 7 is the development phone; entry 212's rules on the older phones stand)

1. **The working resolution in Core.** From entry 209's measurements, choose the capped working size (8 MP gave 3.3 s, 373 MB, 25 of 25 and a
   2.5 thousandth mean shift on the Fold 7) and make it a Core setting the phone always uses and the desktop can use for very large images.
   Say what it costs in accuracy on the sample and on two real range photographs.
2. **The capture screen spike:** CameraX preview inside the Avalonia screen, the lens choice from entry 209's camera listing, tap and
   automatic focus and exposure with a lock, a full resolution still, and MOBILE-CAPTURE.md's live conditions (sheet in frame, markers,
   angle, focus, exposure) on the analysis stream, with its one-instruction guidance and the automatic shutter. Measured on the Fold 7 in one
   sitting, announced in advance.
3. **The real application project**, `org.grouplab.app`, replacing the spike as what CI builds: navigation, the first run window with the
   three choices of entry 208, Settings, the error report and target sending queues shared with the desktop's code.
4. **Capture to result:** capture or pick a photograph, detect at the working size, review and correct by touch (entry 199: 48 dp targets,
   pinch and pan, the offset handle with magnifier), the group figures and the composite plot, save the session.
5. **Sessions between devices, stage A:** share and open a session file (Android share sheet, Google Drive).
6. **Release builds:** a signed APK and AAB on the nightly train, and the Play internal testing track. This needs Alan's upload keystore and
   the Play Console app entry: write both as one request when the build is otherwise ready, with the exact commands, and never read the
   keystore.
7. **Milestone:** the older phones in one sitting (entry 212), then a closed test with testers from Discord.

## 2. The desktop (Windows first, macOS and Linux builds as today)

1. **The survey and benchmark, desktop part** (entries 207 and 208, `docs/SURVEY.md`), with its receiver and worker on the server as one
   install request for Alan, batched with any other server step pending.
2. **The hole center choice** (question 51) as soon as request 9's hand markings arrive; nothing before.
3. **Ongoing feedback** from Alan, Unholy and Fenix comes first whenever it arrives, as now.
4. **A plan for a first beta or stable release:** what must be true for it (a checklist in `docs/RELEASE-PLAN.md`: open defects, the
   minimums table, the privacy text, the user guide), and for Windows the signing choices (Alan intends the Microsoft Store eventually and may
   buy a code signing certificate if the cost is reasonable): research the current options and costs and put them to him as a request with a
   recommendation. Plan only; no release without Alan asking for one by name.

## 3. How to share the time

Android is the higher priority; alternate so desktop feedback never waits more than one Android item. Each item ends with its report in
plain words and the nightly it ships in.

---

## 2026-09-25, entry 218: the archive repository exists

**Status: actioned 2026-09-25, every part.** `gh repo view ... --json visibility` says PRIVATE. The README Alan's creation left is replaced with one saying what the repository is, that it is never made public, and that `docs/notes/STORAGE.md` tracks it. Entries 216 and 217 were built in full the same run; the first real pull with the backlog is request 31, for the planning session to check first.

Alan created `oRAirwolf/grouplab-submissions-archive` on 2026-09-25, private, with a README. From outside, without signing in, its address
answers 404, which is what a private repository looks like to strangers. Before the first upload, confirm with Alan's gh login that it is
private (`gh repo view oRAirwolf/grouplab-submissions-archive --json visibility` says PRIVATE); if it is not, stop and say so in
for-alan.md before anything is sent to it.

Entries 216 and 217 can now be built in full: the monthly release per batch with one zip asset per submission and a manifest, the
verify, upload, verify, then remove order, and the ledger and budgets. Replace the README Alan's creation left with one saying what the
repository is, that it is never made public, and that `docs/notes/STORAGE.md` in the main repository tracks it. The first pull that uses it,
with the backlog of entry 215 section 2, is one request for Alan, dry run first, and the planning session checks its commands before he
runs them.

---

## 2026-09-25, entry 217: track everything stored on GitHub, and free space by deleting the oldest when needed

**Status: actioned 2026-09-25, except freeing categories 2 to 4.** The archive is one release a month with a zip a submission and a manifest, tested end to end with a synthetic submission that was then deleted. `scripts/storage-ledger.py` writes `docs/notes/STORAGE.md`; budgets are in `docs/notes/storage-budgets.json`. It found 140 GB of Actions artifacts against a 5 GB budget; package artifacts now keep 1 to 3 days instead of 30, and `--free` deleted the old ones, oldest first. **Not done**: automatic freeing of old builds, unused test-data files and old archived submissions; none is near its budget, and the ledger shows each. The ledger runs from the pull and from this session, not from the nightly, whose token cannot read the private repositories.

Read with entry 216, which this changes in one important way. Alan, 2026-09-25: "We should keep track of what is being stored on github
and delete old submissions, builds, or files as space is needed."

## 1. Store the archive as release assets, not Git LFS

The planning session checked GitHub's documentation after entry 216 and found a reason to change it:

- **Git LFS space cannot be freed by deleting files.** GitHub: "To remove Git LFS objects from a repository, delete and recreate the
  repository." Deleted LFS files keep counting against the 10 GiB. That defeats deleting old submissions as space is needed.
- **Release assets can be deleted one by one, and are not metered.** GitHub: each asset under 2 GiB, up to 1000 assets a release, and
  "There is no limit on the total size of a release, nor bandwidth usage."

So, in the private `grouplab-submissions-archive` repository (still created by Alan):

1. One release per month, for example `archive-2026-09`, created by the pull. Each submission is one asset: a zip of its folder exactly as
   pulled (`meta.json`, consent, `DO-NOT-PUBLISH`, the image), named by the submission's folder name. Plus a `manifest.json` asset per
   release listing every submission, its size, its SHA-256 and its consent level, rewritten when the release changes.
2. The order of entry 216 section 2 stands: verify here, upload, verify the uploaded asset by its SHA-256 (download it back, or compare
   GitHub's reported digest if it gives one), and only then remove the server copy.
3. No Git LFS in the archive. The repository's own files are just a README saying what it is and that it is never made public.
4. The release tags there are the archive repository's own and are not `v*`; the no `v*` tags rule is about the main repository.

## 2. A ledger of everything on GitHub

A script, run by the pull and by any run that publishes, that writes `docs/notes/STORAGE.md` (committed, no secrets, no submission contents
beyond names, sizes and consent level) with, per repository:

- **grouplab** (public): repository size; releases and their assets (the nightlies kept by the thirty release rule, the rolling `nightly`,
  `test-data`); Actions artifacts and caches.
- **grouplab-crash-reports** (private): issue count and repository size.
- **grouplab-submissions-archive** (private): each month's release, its asset count and total size, and the total.
- **grouplab-testdata** (public): repository size.

It shows totals against a budget, and STATE.md carries one line with the grand total. The Actions storage and minutes that count against
the account's free allowance for private repositories are listed with that allowance.

## 3. Budgets, and what is deleted first when one is reached

GitHub does not cap release storage, but the project keeps itself to a budget so it stays a reasonable use of a free service: propose one
per category (for example the archive at 25 GB), say why, and let Alan change them in one place. When a category reaches its budget, the
pull or the publishing run frees space by itself, oldest first, in this order, and records every deletion in `STORAGE.md` and the log:

1. **Actions artifacts and caches** older than they need to be (set their retention short so this rarely happens).
2. **Old builds:** the thirty release rule already keeps the nightlies in hand; nothing more unless the budget says so, and never the newest
   nightly, the rolling `nightly`, or anything a stable release needs.
3. **`test-data` assets no test or CI job references any more.** Never one a test uses.
4. **Old submissions in the archive**, oldest month first, with these guards: only one whose copy in `C:\Dev\grouplab-submissions` still
   verifies against the manifest's SHA-256 (so a copy remains), never one used as a fixture, in `test-data`, or referred to by a document or
   article, and never one marked "may be published" that has not yet been reviewed for the public data set. A deletion is listed, with the
   reason, in for-alan.md in plain words the same run ("removed 12 submissions from September 2026 to stay under the archive's 25 GB").
   Alan decided this can happen without asking first.

## 4. Order of work

Build the ledger first (it needs nothing from Alan), then the archive once the repository exists, then the budgets. Entry 215's server
retention rules and entry 216's privacy text stand.

---

## 2026-09-25, entry 216: nothing stays on the server; the long term copy goes to a private GitHub repository

**Status: actioned 2026-09-25 as entry 217 changed it; the first real use waits on request 31.** Archive in `grouplab-submissions-archive`, which Alan had created: release assets, not Git LFS. Order: verified here, uploaded, downloaded back and compared, then removed from the server; `-NoArchive` is the switch. Quota is the ledger's budget. CI never downloads it. Privacy text added to the upload page and the article; judged not to change what anyone agreed to, since a private copy with the project is what sending to the project already meant, and the consent sentences are untouched. Restore check: `scripts/Test-SubmissionsArchive.ps1`.

Read with entry 215, which this extends. Alan, 2026-09-25: "I want anything that is submitted to my pissinhot server to be deleted from it
after being ingested or processed. I dont want anything left on there longer than is needed. Can submissions be backed up to github for
long term storage?"

## 1. Nothing left on the server longer than needed

Entry 215 stands and this makes it firmer: every kind of thing people send (targets from the page and the application, error reports,
survey reports) is removed from the server as soon as it has been processed and a verified copy exists elsewhere, and every holding
folder has a stated maximum age after which the worker deletes it regardless, with the reason in the log. No folder on the server may grow
without bound.
The server here means the one machine that hosts grouplab.org and pissinhot.com. The privacy text names grouplab.org only.

## 2. The long term copy: a private GitHub repository, if Alan creates it

The planning session told Alan this is workable, with these facts (GitHub's own documentation, September 2026): an ordinary file in a
repository is limited to 100 MB and a repository should stay under about 1 to 5 GB; Git LFS on a free personal account includes 10 GiB of
storage and 10 GiB of download a month, with files up to 2 GB, and over that uploads stop until paid for. Submissions are roughly 3 to 60
MB each, so 10 GiB is several hundred of them. Alan has been asked to create the repository; until he does, build the parts that do not
need it.

1. **The repository:** private, created by Alan (Code never creates repositories or changes their settings), name suggested
   `oRAirwolf/grouplab-submissions-archive`. Git LFS for every image. One folder per submission as pulled, with its `meta.json`, consent
   file and `DO-NOT-PUBLISH` marker kept exactly. It is never made public and never merged into the public test data repository; the
   consent recorded in each folder decides what may ever be published, and nothing is published from it without a separate entry.
2. **The order in the pull**, so there are always two copies before the server's is removed:
   1. pull and verify the checksums here (as today);
   2. commit the new folders to a local clone of the archive and push;
   3. verify the push (the remote holds the same objects, by hash);
   4. only then remove the folders from the server (entry 215 section 1).
   If step 2 or 3 fails, the server copy stays and the run says why. A switch runs the old behavior without the archive.
3. **Quota:** the pull reports the archive's LFS use against the 10 GiB allowance each run, and at 80 percent adds a line to for-alan.md
   with the choices (pay GitHub for more, or move the archive to other storage such as Cloudflare R2, which Alan's Cloudflare account can
   hold). Nothing is ever deleted from the archive to make room without Alan saying so.
4. **CI never downloads the archive.** Test data stays on the `test-data` release; the archive's download allowance is for Alan's own
   restores.
5. **Privacy text:** the upload page, the application's consent wording and the privacy page say where a submission ends up: removed from
   the web server once processed, kept by the project in a private repository hosted by GitHub, published only if the sender chose "may be
   published". Wording change only; no change to what anyone has already agreed to, because a private copy with the project is what
   sending to the project already meant. If you judge the new wording changes the meaning for people who already sent, say so and stop.
6. **A restore test:** a script that clones the archive to a temporary folder and checks every folder's checksums against its
   `meta.json`, run once when the archive is first filled and then on demand.
7. The backlog of entry 215 section 2 goes into the archive first, then leaves the servers.

---

## 2026-09-25, entry 215: submissions leave the server as soon as a verified copy is here

**Status: actioned 2026-09-25, with entries 216 and 217; the backlog waits on Alan.** The pull removes each submission from the server once its copy here verifies and the archive has proven it holds it, one line a folder, with `-KeepOnServer`. The workers' limits: quarantine by attempts as before, refused 7 days, ready 60, error reports 30, set-aside files 7; the survey's in SURVEY.md. The rules are written once, in the article `what-grouplab-sends`, and the upload page links to them. **Not done: the backlog**, which is request 31, one sitting of Alan's, replacing request 12. Request 32 suggests a backup of the local copy.

Alan, 2026-09-25: "Shouldn't target submissions be deleted from the pissinhot server after they are downloaded and processed?"

His entry 129 decision already says the server keeps nothing once read. In practice it keeps everything until he runs
`Remove-ReadSubmissions.ps1` by hand (request 12, marked optional), and that removes only what the ledger marks ingested: 6 of the 18 old
pissinhot.com submissions, and none of the grouplab.org ones he has pulled since. So photographs people sent sit on the web server
indefinitely. Make the rule happen by itself.

1. **The pull removes what it has verified.** `Get-TargetSubmissions.ps1`, after a folder's checksums match here, removes that folder from
   the server in the same run, and says so per folder. The copy here, rebuilt and scanned by the worker, is what "downloaded and processed"
   means; waiting for a later ingest step is what left them there. A folder whose checksums do not match is left on the server and
   reported. A `-KeepOnServer` switch keeps the old behavior for a run when wanted. Same for grouplab.org's `ready` and pissinhot.com's old
   folder.
2. **The backlog, once:** every submission already pulled and verified here, on both servers (the 18 on pissinhot.com, the ones on
   grouplab.org including request 22's test target and request 15's photograph), is removed by the first run of the new pull, or by one
   `Remove-ReadSubmissions.ps1` line that you write into for-alan.md with its dry run first. Folders not yet pulled are pulled first, then
   removed. Replace request 12 with that one request, and make it the next thing for Alan, since it is about other people's photographs.
3. **The other places submissions sit on the server**, say what each keeps and for how long, and make each finite:
   - `quarantine`, while the worker runs: gone once the worker moves the folder on;
   - `refused`: kept long enough to look into (say 14 days, your call with a reason), then deleted by the worker, and the log keeps only the
     reason and the folder name;
   - `error-reports/incoming`: deleted once its issue is opened or updated;
   - the survey's stored reports (entry 207): kept only as long as the aggregate page needs, and never individual records beyond that.
   Write the retention rules in one place (the upload page's privacy text and `docs/PRIVACY.md` or wherever the site says what happens to
   what people send) so what the site promises is what the server does.
4. **The copy here becomes the only copy.** Say so in for-alan.md, and suggest how Alan might back up `C:\Dev\grouplab-submissions` (it is
   outside the repository and outside any sync today, as far as the planning session knows). His decision; do not set up a backup yourself.
5. Deletion on the server needs sudo, which Code never runs: the pull and removal run from Alan's PowerShell as today.

---

## 2026-09-25, entry 214: the bull's rings about half as bright again

**Status: actioned 2026-09-25, every section.** Dark theme rings #505050 to #282828, half the lightness; light theme #a0a0a0 to #bdbdbd, toward the paper, so in both the rings sit behind the half strength outlines and still show. Width unchanged. Pictures `docs/figures/composite-plot-210-*` replaced with both changes; the tone test follows the new values.

Alan, 2026-09-25, on the composite plot after entry 210: "Make the gray on the target circles about 50% darker again."

1. **Dark theme, which Alan uses:** take the rings' gray (`inks.Bull`, the mid gray of entry 210) to about half its current lightness, so
   they sit further back against the dark paper and the light shot outlines stand clearly in front of them. Keep them visible: they must
   still read as rings at a glance, not vanish into the background.
2. **Light theme:** "darker" there means toward black, which would make the rings heavier than the shot outlines, the opposite of the
   intent. Keep the light theme's relationship the same as the dark theme's after this change: rings clearly behind, outlines clearly in
   front. Say what you set for each theme.
3. Width stays as entry 210 made it.
4. Render the sample's plot in both themes and both framings again under `docs/figures/`, replacing entry 210's pictures, so the planning
   session can show Alan the result. The render test's tone check follows the new values.
5. Do it together with entry 213 (the legend moved off the plot), so the pictures show both changes.

---

## 2026-09-25, entry 213: the fourth test phone, and the plot's legend covers the shots

**Status: actioned 2026-09-25, with entry 214, every section.** The OnePlus 6T is a reference device, optional at the milestone. The key goes beside the plot or below it, whichever leaves the larger square, or collapses to a Key button in its own strip; the plot is drawn and clipped in what is left. The saved report's key was already outside the plot, its caption under the square. Section 3: the 0.308 was the figure test's, named only to draw outlines; the application never read it, and the figures now use 6.5 Creedmoor from `samples/sample.json`. The Creedmoor line in PROVENANCE.md is Unholy's 2026-09-23 scan, not the sample.

## 1. The OnePlus is a OnePlus 6T on Android 11

Alan, 2026-09-25. As the planning session understands it: Snapdragon 845 (2018), 6 or 8 GB of memory, and Android 11 was its last update;
confirm from the phone only when it is next connected. Add it to the reference devices in `docs/ANDROID.md` beside the Essential PH-1
(Android 10), the Galaxy S20 5G (Android 13) and the Fold 7. Entry 212 holds: it comes out only at the named milestone, and only if it adds
something the PH-1 and the S20 do not. It fills the Android 11 and 12 gap, so it is optional at that milestone, not required. Nothing to ask
Alan now.

## 2. The composite plot's legend sits on top of the data

The planning session looked at `docs/figures/composite-plot-210-group-dark.png` and `-whole-dark.png`. The key box in the top left
corner is drawn over the plot and hides shot outlines under it (in the sample, the outline of the top left shot and its neighbours).

1. The key never covers data: put it outside the plot area (beside or below it, depending on the width available), or, where there is no
   room, let it collapse to a small button that opens it, and make it movable if that is simpler. On a phone it collapses by default.
2. The saved and printed report places the key outside the plot as well.
3. A test: the key's rectangle does not intersect any drawn shot, CEP circle, or center line, at the smallest and a large window size.

## 3. One thing to check

The picture's key says the outlines are drawn "at the 0.308 in caliber", while the sample scan is Alan's 6.5 Creedmoor sheet
(`samples/PROVENANCE.md`). If the figure was simply rendered with 0.308 set, render the documentation figures with the sample's own
caliber so pictures published in the guide match the sheet. If the application itself read 0.308 from the sample's session, that is a
defect: say which it was.

---

## 2026-09-25, entry 212: the older phones only when absolutely needed; the Fold 7 is the development phone

**Status: actioned 2026-09-25, every section.** Request 30 is a note, not counted open. `docs/ANDROID.md` has the reference devices, the milestone before the first closed test, the rule for an unreproducible problem, and the Fold 7 batching rule; the working size is chosen on the Fold 7. for-alan.md says the Fold 7 and the PH-1 can be put away.

**Read before entry 211, and let this override its section 3.** Alan, 2026-09-25: "we should keep the testing on the older phones to an
absolute minimum because I dont want to keep switching phones around. Do the primary development on the Fold 7 and the other phones we will
test with once it is absolutely needed."

1. **Do not ask Alan to connect the Essential PH-1 or the Galaxy S20 now.** Entry 211 section 3 items 1 and 2 are withdrawn. If request 30
   (the older phones) asks him to set them up now, rewrite it as a note of what the phones are and when they will be needed, not as
   something to do; it should not count as open or urgent.
2. **All development and routine testing runs on the Fold 7.** Choose the capped working resolution from the Fold 7's measurements and
   entry 206's market figures, and use the emulator with limited cores and memory to estimate the floor.
3. **The older phones come out at named milestones only**, each a single sitting that does everything needing them at once, and never for
   one small check:
   - **Once before the first Play closed testing release:** the PH-1 (Android 10, the floor) and the S20 (Android 13). Install, start,
     run detection at the chosen working size, time and peak memory, the capture screen once. This is also where Android 10 is confirmed or
     the minimum is revisited.
   - Otherwise only if a problem is reported that cannot be reproduced on the Fold 7 or the emulator.
   Write these milestones in `docs/ANDROID.md` so they are not forgotten or expanded.
4. **Keep entry 211's record of the phones** (models, Android versions, memory) in `docs/ANDROID.md` as the reference devices for those
   milestones, and entry 211 section 1 (request 29's answers) stands.
5. **For the Fold 7 too, batch.** When a stage needs the phone, gather everything that needs it into one sitting and tell Alan in advance
   through for-alan.md, as entry 209 did, so he connects it once rather than repeatedly.

---

## 2026-09-25, entry 211: request 29 done; Alan's older phones become the test devices

**Status: actioned 2026-09-25 as entry 212 narrowed it.** Request 29 closed; stage B is possible through Google Drive, OneDrive unknown; the phones are recorded as reference devices. The PH-1 was found connected: its model, Android 10 and 4 GB were read from it and nothing was installed or run. **Section 3 items 1 and 2 were withdrawn by entry 212**, not done.

## 1. Request 29, answered 2026-09-25

1. Folded, upside down: **turned.**
2. Unfolded, upside down: **turned.**
3. The folder picker: **Google Drive is listed, and Alan could choose a folder once he drilled down into his Drive.** He did not mention
   OneDrive; it may not be installed on the Fold 7. So `docs/ANDROID.md` section 8 stage B, the sync folder, is possible on Android through
   Google Drive. Record it, with OneDrive unknown. Whether writes into that folder sync reliably and what happens with an edit on both sides
   is the next question for stage B, when the app has sessions to move; not now.

Close request 29. **The Fold 7 is no longer needed for now**; say so in for-alan.md so Alan can turn Wireless debugging off.

## 2. Alan's other test phones

Entry 207 said Alan's older phones replace the Galaxy A16 and A06 classes as the low end references. What he has:

1. **Essential PH-1**, on **Android 10**, turns on. As the planning session understands it: Snapdragon 835 from 2017 and 4 GB of memory,
   and Android 10 was its last update. Confirm from the phone itself. If so it is exactly the floor: the approved minimum Android, the
   approved minimum memory, and a processor in the same class as the budget phones (entry 206). **It is the device that decides whether
   Android 10 can stay GroupLab's minimum** despite .NET 10 listing Android 14 (entry 207 section 2).
2. **Samsung Galaxy S20 5G**, 8 GB and 128 GB, **Android 13** with One UI 5.1. A middle reference: Android below 14, a 2020 flagship
   processor, 8 GB.
3. **A OnePlus**, model not yet known, which needs charging before it turns on. Alan will send its model and Android version later.

## 3. What to do

1. A request in for-alan.md, at the top, to get the PH-1 and the S20 onto adb in one sitting. **Android 10 has no Wireless debugging
   pairing**, so the PH-1 needs a USB cable (and possibly Google's USB driver on Windows; say if so, with the exact step). The S20 on Android
   13 can pair wirelessly like the Fold 7. Write the Developer options steps for each phone's own menus, and what `adb devices -l` should
   show.
2. When they are connected, in one sitting as entry 209 did, so the phones are not left waiting:
   - install the spike on both and confirm it starts on Android 10 and 13, which answers the .NET support question;
   - the working resolution runs of entry 209 on both, with time and peak memory, so the capped resolution is chosen against the PH-1 and not
     the Fold 7; say whether the 8 MP working size meets the approved budget (peak under about 400 MB, detection about 30 s on the floor
     device) on the PH-1;
   - the camera listing on both;
   - then say plainly that the phones can be put away.
3. Record all three phones in `docs/ANDROID.md` as the reference devices, with their measured results beside the Fold 7's, and replace the
   A16 and A06 classes as the targets with the S20 and the PH-1, keeping the market figures of entry 206 as the reason.

---

## 2026-09-25, entry 210: the composite plot, second pass: rings five times thicker and darker, and a whole target view

**Status: actioned 2026-09-25, every section.** The rings' width is in page units, 0.05 in, about 19 pixels on the sample's group view, held between 4 and 40 pixels; they are a solid mid grey in both themes. Group and Whole target beside the plot, remembered, and in Compare; wheel, touchpad and touch pinch zoom, a drag on empty paper pans, a double click fits again. The saved report keeps the whole target, because its page cannot clip a ring at a group framing, and its caption says so. Pictures: `docs/figures/composite-plot-210-*`.

Alan, 2026-09-25, after entry 204: "The new analysis screen is much better, but it is still hard to read." Do this after 206 to 208.

## 1. The bull's rings

1. **About five times thicker.** `CompositePlot.BullStroke` is 4 today; make the rings about 20 at the default view. Better: give the
   ring's width in page units, so it stays in proportion as the view zooms (section 2), and pick the unit so it is about five times today's
   at the default zoom. Say which you chose.
2. **Darker**, so they are clearly told apart from the thin outlines drawn around the impacts: a solid mid gray rather than the light gray
   of entry 204, in both themes, still behind everything else in the draw order. The impacts' outlines stay thin, at their 50 percent
   opacity, so thickness and tone both separate them from the rings.
3. Check that nothing important disappears under a thick ring: the shot outlines, CEP circles, the extreme spread and the center lines are
   drawn on top, and a hole sitting on a ring must still be plainly visible. Look at the sample and at a group that straddles a ring.

## 2. Zoom out to the whole target

Today the plot frames the group only. Add a way to see the whole target:

1. **A toggle beside the plot: "Group" and "Whole target".** Group is today's framing; Whole target fits the entire bull, every ring, with
   the group inside it. The choice is remembered between sessions. Default stays Group.
2. **Free zoom and pan as well:** mouse wheel and trackpad pinch on the desktop, pinch and two finger drag on touch, and a double click or
   tap, or the toggle, to return to a fitted view. Rings, CEP circles and lines stay crisp at every zoom.
3. The saved and printed report follows the chosen framing, or offers both; say which.
4. Compare and anywhere else the composite plot appears get the same toggle.

## 3. Tests and pictures

Extend entry 204's headless render test: the ring stroke against the outline stroke, the ring tone darker than before and still distinct
from the outlines, and the two framings (the whole target view contains every ring; the group view matches today's). Then save before and
after pictures of the sample's plot in both framings and both themes under `docs/figures/` for Alan, and say in the report where they are.

---

## 2026-09-25, entry 209: do every test that needs the Fold 7 now, first, while it is waiting

**Status: actioned 2026-09-25, everything adb could do.** The phone was connected. Entry 205's build was already on it with the orientation and lifecycle checked. Measured: the sample at five working sizes, time, peak memory and hole offsets; every rear camera's characteristics. Built: a Choose a folder button. Request 29 holds the three steps that need Alan's hands, at the top of for-alan.md with the line that the phone can be put away after them. The spike's minimum is now Android 10, per entry 207.

**Action this entry before 206, 207 and 208.** Alan, 2026-09-25: the Fold 7 is on the desk with Wireless debugging on and the screen set not
to turn off, and he does not want to leave it like that longer than needed. Check `adb devices -l` lists it; if not, one line to Alan asking
him to run the `adb connect` line with the address on the phone's Wireless debugging screen.

## 1. What to run on the phone, in one sitting

Everything already built that needs the phone, plus measurements the next stages will need, so the phone can be put away afterward:

1. **Entry 205's build on the phone:** install it and confirm from the log that the activity follows all four orientations and that the
   lifecycle logging works. The physical turning needs Alan (section 2).
2. **Memory against image size (entry 206 section 2.2):** run the engine on the sample scan at several working resolutions (for example the
   full 600 dpi, 400, 300 and 200 dpi, and a 12 MP and an 8 MP photograph size) and record time and peak memory for each, and whether the
   holes and their positions agree with the full resolution result, and by how much. This is the measurement that decides the capped
   resolution, and it needs the phone.
3. **The cameras, for the capture screen:** list every rear camera the phone reports through Camera2, with focal lengths, sensor size, the
   largest still size, and whether intrinsics and distortion terms are reported. Nothing is photographed and nothing is saved but those
   numbers.
4. **The folder picker question (entry 199, `docs/ANDROID.md` section 8 stage B):** a small screen in the spike that opens Android's folder
   picker, so Alan can say in one look whether Google Drive or OneDrive offer a folder there. Only if it is quick to add.
5. Anything else in `docs/ANDROID.md` that is waiting on the phone and can be run by adb alone.

Run them unattended by adb where possible. Write every result into `docs/ANDROID.md`.

## 2. What needs Alan's hands, as one short request

Gather every step that needs him into one request, placed at the top of for-alan.md, that he can do in a few minutes in one go: for
example turning the phone upside down in both screens, and one look at the folder picker. Say exactly what to do and what to look for,
and number the steps.

## 3. Then say he can put the phone away

When nothing more needs the phone, say so plainly as the first line of the report and at the top of for-alan.md: "The phone is no longer
needed; Wireless debugging can be turned off and the screen timeout put back." If something is still running, say how long it will take.

Then carry on with entries 206, 207 and 208.

---

## 2026-09-25, entry 208: the survey's opt in goes on the same first run screen as sending targets and error reports

**Status: recorded 2026-09-25 in `docs/SURVEY.md` section 1**, to be built with the survey: one first run window with three choices, none preselected, the benchmark offered under the survey choice, a Sharing section in Settings, and the window shown once more to people who answered before.

Read with entry 207 section 3.1, which this replaces in one point. Alan, 2026-09-25: "The opt in for the hardware survey and benchmark
testing should be displayed on the same window as the target share and error opt in."

1. **One first run screen, three choices.** Sending targets, error reports, and the hardware survey with its benchmark are asked on the same
   window, one after another, each with its own plain description of what is sent and its own answer. Not a second window, not a later
   prompt. Each stays a separate choice: saying yes to one never turns on another.
2. **Nothing preselected**, on any of the three (entry 203 section 3). If the window grows too long, it scrolls; it does not hide a choice
   behind a "more" link, and a person can answer all three without leaving it.
3. **The benchmark offer** belongs on the same window as the survey choice: for example a line under it saying the benchmark can be run now
   or later from Settings. It does not start by itself.
4. **Settings mirrors it:** the three live together in one section (for example "Sharing"), in the same order and words, so a person finds
   and changes all three in one place. Where the Sending targets and error report settings sit today, move them there.
5. **Existing users**, who already answered the targets and error questions: show them the window once more after the update with their
   earlier answers kept and only the new survey question unanswered, rather than a separate survey popup.
6. Entry 203's wrapping and no-clipping test covers this window, including all three choices at the narrowest size and at 200 percent.
   On Android the same three choices appear together on one screen of the first run flow.

---

## 2026-09-25, entry 207: minimums approved; minimums for every platform; an opt-in hardware and benchmark survey everywhere

**Status: actioned 2026-09-25, except what the entry leaves for later.** The minimums table is in PLATFORM-SUPPORT.md, so it reaches the README and the download page; the desktop memory figure is the analyzer's measured 733 MB peak, the disks are the unpacked downloads, and no platform without a build is listed. ANDROID.md says what .NET's "supported" means and why Android 10 stays. Request 30 asks for the older phones. `docs/SURVEY.md` is the design; the desktop part is built with the next desktop work, as section 3 says, and its server install becomes a request then.

Read with entry 206, which this answers. Alan, 2026-09-25.

## 1. Alan's decisions

1. **The phone minimums in entry 206 section 2 are approved:** Android 10, 4 GB of memory, peak memory under about 400 MB aimed at 300,
   detection about 10 s on a Galaxy A16 class phone and about 30 s on an A06 class phone with progress and cancel, camera at least 8 MP with
   autofocus, installed app under about 100 MB.
2. **No Galaxy A16 will be bought.** Alan has older test phones and will look out what they are. When he sends their models, they become the
   low end reference devices in place of the A16 and A06 classes; until then use the emulator as entry 206 says. Add a request to for-alan.md
   asking for each phone's model, Android version and whether it still charges and boots, so they can be paired the same way as the Fold 7.
3. **An opt-in hardware and benchmark survey, on every platform and operating system GroupLab runs on:** Windows, macOS, Linux and Android,
   and iOS if it ever exists. Section 3.
4. **List minimums for every platform.** Section 2.

## 2. Minimums for every platform

Write one table, in `docs/PLATFORM-SUPPORT.md`, shown on grouplab.org's download page and in the README: the lowest operating system, CPU
architecture, memory, free disk, screen and, for phones, camera, per platform. Each line names where it comes from: .NET's own support list,
Avalonia's, OpenCV's, or GroupLab's measurements.

What .NET 10 itself supports (github.com/dotnet/core, release-notes/10.0/supported-os.md), as the floor nothing can go below:

- **Windows:** Windows 10 version 1607 and later; x64, Arm64 and x86.
- **macOS:** 14 and later; Arm64 and x64.
- **Linux:** Ubuntu 22.04, Debian 12, Fedora 42, RHEL 8 and later; glibc 2.27 for x64 and Arm64; musl 1.2.3.
- **iOS:** 18 and later.
- **Android: 14 and later.** This conflicts with the approved Android 10. It is what Microsoft tests and supports, not necessarily what
  runs: the spike is built for API 24 and runs. Say plainly in `docs/ANDROID.md` what "supported" means there, test on the oldest phone Alan
  finds, and if Android 10 to 13 work, keep Android 10 as GroupLab's minimum with a note that it rests on GroupLab's own testing rather
  than Microsoft's. Android 14 or later alone would cover only about 55% of Android phones in use, which is why this matters.

Then GroupLab's own figures, measured rather than guessed: the desktop peaks at about 730 MB on the 600 dpi sample, so say what the minimum
and recommended memory are on the desktop (likely 4 GB minimum and 8 GB recommended; measure), the disk space the install and a typical
library of sessions take, and the smallest window the layout supports. Which architectures are actually built and published today, and
which are not (for example Windows Arm64 or Linux Arm64), goes in the same table; do not list a platform as supported that has no build.

## 3. The opt-in hardware and benchmark survey

1. **Consent.** Its own choice, separate from sending targets and from error reports, on the first run screen and in Settings: off until
   the person turns it on, and never preselected, the same rule as entry 203 section 3. The wording says exactly what is sent.
2. **What is sent, and nothing more:** operating system and version, CPU model, architecture and core count, total memory, GPU name if
   relevant, screen size and scale, for phones the device model and rear camera resolution, GroupLab's version, and per analysis the image
   size, the working resolution, the time of each stage and the peak memory. Never a name, account, file name, path, photograph, location, IP
   address stored on the server, or a device serial or advertising identifier. A random installation id may be used to count devices once,
   reset whenever the person asks.
3. **A short benchmark.** A fixed built in test, the sample scan or a smaller synthetic sheet, runs when the person chooses it (a button in
   Settings, and offered once after opting in), timed stage by stage. It gives every platform a comparable number, like the survey's
   Steam counterpart, and it tells the person their own result.
4. **Transport.** The same route as error reports and targets: posted to grouplab.org, checked against a schema, rate limited, stored on
   the server. It does not go to the GitHub issues repository. Queued offline like the others.
5. **Publication.** An aggregate page on grouplab.org, like Steam's hardware survey: shares of operating systems, versions, memory, CPU
   classes and phone models, and benchmark times by class, with the date range and sample size, updated from the stored reports. Never an
   individual record. Small groups are merged into "other" so no one device is identifiable.
6. **Use.** Review the minimums in section 2 against it once there are enough reports, and say in STATE.md when that is.

Design it now in a short document, `docs/SURVEY.md`, and build the desktop part with the next desktop work; the Android part comes with the
real app. The server side is a receiver and a worker like the error reports, so Alan will get one install request for it; keep that to one
sitting with the others if any are pending.

Report in plain words for Alan: the minimums table, and what the survey will ask people.

---

## 2026-09-25, entry 206: the phones GroupLab must run on, and the budget that sets

**Status: actioned 2026-09-25, with entries 207 and 208.** `docs/ANDROID.md` has "The phones it must run on" with the sources, and the budget as entry 207 approved it, with the Fold 7's measured working size beside it. iPhone is recorded in PLATFORM-SUPPORT.md above the rule, as a fact. **Not done**: the emulator stand-in for the slow phones is not run yet; the budget says what the Fold 7's time scales to, as an estimate.

Alan asked for a study of the current phone market in the Americas and Europe, in the spirit of the Steam hardware survey, to decide how
much CPU, memory, storage, camera and computation the application may use, and what the minimum is. The planning session researched it on
2026-09-25. Put the findings in `docs/ANDROID.md` as a new section, "The phones it must run on", with the sources, and hold the design to
the budget once Alan approves it (section 2). Where a figure is judgment rather than measurement, it says so.

## 1. What the market looks like

Android against iPhone (StatCounter, web traffic, August 2026): United States iOS 60.7%, Android 39.3%; North America 60.3 / 39.7; United
Kingdom 51.4 / 48.6; Germany 27.6 / 72.4; Europe 37.3 / 62.7; South America 23.1 / 76.9.

Android versions in use, cumulative (apilevels.com from StatCounter, April 2026): 16+ 22.3%, 15+ 41.0%, 14+ 54.5%, 13+ 68.9%, 12+ 78.8%,
11+ 86.9%, 10+ 91.1%, 9+ 93.5%, 8+ 96.1%, 7+ 96.6%.

iOS versions (TelemetryDeck, end of August 2026): iOS 26 86.6%, iOS 18 7.9%, iOS 27 3.3%. Oldest iPhone on iOS 27: iPhone 11 (2019, 4 GB).

What sells: the iPhone 17 was the best selling phone in the US, UK, Germany and France in Q2 2026. Latin America's 2025 top ten was almost
all budget Android under 200 dollars: Galaxy A06 first (7%), Moto G15, Redmi 14C, Moto G05, Redmi A5, Moto G35, Redmi Note 14 4G, Galaxy
A16, A15 and A56.

Memory and storage: no public survey gives installed RAM by region. AnTuTu's Q1 2026 report on Android outside China, which skews toward
enthusiasts, shows 4 GB or less 7.6%, 6 GB 9.8%, 8 GB 39.3%, 12 GB 36.1%, 16 GB 6.8%; storage 128 GB 26.1%, 256 GB 49.7%. Treat it as the
upper bound; the Latin American best sellers ship with 4 GB and 64 or 128 GB.

Speed (Geekbench 6 single and multi core): Fold 7, Snapdragon 8 Elite, about 3196 and 10142; Galaxy A16 5G, Exynos 1330, about 960 and
1826; Galaxy A06, Helio G85, about 405 and 1349.

Cameras: every phone above has 12 MP output or more with autofocus. A Letter sheet framed with margin spans about 13 inches of a 4000 pixel
image, so 12 MP gives roughly 300 pixels an inch and 8 MP about 250, against the quality score's perfect 150 and useless 50.

Android 17 adds a per app memory limit scaled from device RAM, counting native memory (where OpenCV's buffers live), formula unpublished; an
app over it is killed.

## 2. The proposed budget and minimum (judgment, for Alan to approve)

1. Minimum Android 10 (API 29), not 7: about 91% of Android devices; the phones dropped are 2019 or older with 2 to 3 GB, which could not
   hold the engine anyway. Say if the OpenCV build or CameraX makes a different floor better.
2. Minimum memory 4 GB. Peak memory under about 400 MB, aimed at 300 MB, on any image. Today's 716 MB is too much: measure how peak memory
   scales with image size, then process at a capped working resolution (for example the camera's 12 MP, a scan brought to about 300 dpi),
   after measuring the accuracy cost against full resolution on the same images.
3. Reference phones: Galaxy A16 class as the normal low end, Galaxy A06 class as the floor. Detection within about 10 s on the A16 class and
   30 s on the A06 class, with progress and cancel; live capture checks at 10 frames a second or better on the A06 class. Use the emulator
   with limited cores and memory as a rough stand in and say how rough; buying a Galaxy A16 is Alan's decision.
4. Camera at least 8 MP with autofocus, refused with the reason otherwise.
5. Installed app under about 100 MB; warn when free space falls under about 500 MB.
6. Screens down to 360 dp wide.

## 3. GroupLab's own hardware survey

Only with the consent sending targets and error reports already ask for: device model, Android version, RAM, cores, camera resolution,
working resolution, detection time and peak memory, nothing that identifies the person. A Steam style page on grouplab.org can then show what
GroupLab actually runs on. Design now, build with the real app. The Play Console device catalog adds the installed base later.

## 4. iPhone, for the record

Not planned; Alan's decision. iPhone is about 60% of US phones and half of UK phones. If ever reconsidered: floor iPhone 11 on iOS 26 or
later; Avalonia runs on iOS; the engine would need OpenCV built for iOS; GitHub's macOS build machines can build it without anyone owning a
Mac; distribution needs the paid Apple developer program. Record in `docs/PLATFORM-SUPPORT.md` as a fact, not a plan.

## 5. Sources

- https://www.digitalapplied.com/blog/mobile-os-market-share-2026-ios-vs-android
- https://gs.statcounter.com/os-market-share/mobile/north-america
- https://apilevels.com/
- https://telemetrydeck.com/survey/apple/iOS/majorSystemVersions/
- https://www.antutu.com/web/news/detail?id=136552
- https://www.phonearena.com/news/best-selling-smartphones-usa-china-india-germany-uk-france-korea-japan-q2-2026_id182887
- https://www.gsmarena.com/counterpoint_samsung_galaxy_a06_was_the_bestselling_phone_in_latam_for_2025-news-71620.php
- https://nanoreview.net/en/phone-compare/samsung-galaxy-a16-5g-vs-samsung-galaxy-a06
- https://www.cpu-monkey.com/en/compare_cpu-qualcomm_snapdragon_8_elite-vs-mediatek_helio_g85
- https://stora.sh/blog/2026-04-25-android-17-memory-limits-guide
- https://support.apple.com/guide/iphone/iphone-models-compatible-with-ios-27-iphe3fa5df43/ios

---

## 2026-09-25, entry 205: request 27 done: the fold test passed, and upside down portrait does not rotate

**Status: actioned 2026-09-25, except the tablet.** Request 27 closed; ANDROID.md sections 5 and 7 hold the results and Avalonia is confirmed. Upside down portrait: `FullUser`, checked on the Fold 7 over adb with rotation locked at 180 and the settings restored; the tablet waits until it is to hand, not blocking. MOBILE-CAPTURE.md item C5. The repeated start up was the activity made again after Back; degenerate sizes are ignored. Each image's own peak: photograph 635 MB, scan 721 MB.

Alan ran request 27 on the Fold 7 on 2026-09-24 at about 22:40 local time and sent two screenshots of the spike, one on the cover screen
and one on the inner screen. They are not committed; what they show is below.

## 1. What passed

- **Folding, unfolding and turning kept the app.** The Screen list keeps every earlier line through each change: compact 411 by 960 dp at
  2.625 pixels a dp on the cover screen (1080 by 2520), medium 750 by 832 dp unfolded (1968 by 2184), 832 by 750 dp turned, and back.
- **The layout rearranges as designed:** panels stacked on the cover screen, side by side unfolded.
- **Largest system font size:** Alan reports nothing cut off.
- **Detection on the phone:** the 600 dpi sample, load 537 ms, codes and naming 1504 ms, marking 16880 ms, 25 holes, total 18922 ms, peak
  memory 716 MB. The range photograph `20260920_141404.jpg`: 0 of 34 markers found, as on the desktop, peak memory 900 MB (say whether that
  figure is that image's peak or the process's peak so far; if the process's, report each image's own).

Close request 27, record these in `docs/ANDROID.md` sections 5 and 7, and treat Avalonia as confirmed for the phone unless something below
changes that.

## 2. Upside down portrait does not rotate

Alan: "the application did not turn when the phone is rotated 180 degrees so the USB C port is at the top." Android leaves reverse portrait
out unless the activity asks for it. The inner screen of a foldable is close to square and is picked up either way round, and a phone on a
bench or a tripod mount is often upside down, so all four orientations must work.

1. Set the activity's orientation to follow the sensor in all four directions while still honoring the user's rotation lock
   (`ScreenOrientation.FullUser`, rather than `FullSensor`, which ignores the lock). Say which you chose and why.
2. Check the same on the tablet when it is next to hand; not blocking.
3. **Carry this into the capture screen's design.** The photograph must be stored the right way up whichever of the four ways the phone is
   held, and the capture overlays (outline, guidance) must turn with it. Add it to `docs/MOBILE-CAPTURE.md` as a requirement with its test.

## 3. Two things the Screen list shows

1. **The start up sequence appears twice.** At 22:40:46 the list begins `1 by 1 dp, 1 pixels a dp`, then `412 by 960 dp, 1 pixels a dp`,
   then the real size. The same three lines appear again at 22:42:40, without Alan closing the app (he took screenshots around then). The
   earlier lines survived, so either the view was rebuilt inside the same activity, or the activity was recreated and the list lives in
   something static that hid it. Log the activity's own lifecycle (create, destroy, and a count) so the report can say which. If the activity
   is being recreated, the real app will lose state unless it is designed for that; say what the design is.
2. **Transient sizes.** `0 by 0 dp`, `1 by 1 dp` and `1 pixels a dp` appear during start and during a fold. The real layout must ignore
   degenerate sizes and never lay itself out, even for a frame, as compact at the wrong density.

Report in plain words for Alan: whether upside down now works, and what the start up lines meant.

---

## 2026-09-25, entry 204: the composite plot is still too busy: Alan's changes

**Status: actioned 2026-09-25, every section.** All eight changes are in `CompositePlot`, the palette, Compare and the report. Two deviations, both said here: CEP 50, 90 and 95 are told apart as dotted, solid and dashed (the entry allowed dash or weight), and the saved report draws its dashes as runs of dots, because the page has no dashed stroke, and says so in its caption. Before and after pictures are `docs/figures/composite-plot-{before,after}-{light,dark}.png`.

Alan, 2026-09-25, on the analysis screen's composite plot (`src/GroupLab.App/CompositePlot.cs`). He showed a reference plot from another
program; it is not to be named, copied or committed, and nothing below depends on it beyond the description here. In that plot the bull's
ring is one thick, light gray band, the shot outlines are thin and plain, and the two sets of center lines run the full width and height
of the plot, one black, one blue.

His words: "The composite group is still very busy and hard to read."

## 1. What to change

1. **Shot outlines at half their current opacity.** The caliber circles of the shots, and their points when no caliber is set, drawn at
   50 percent of today's opacity. Selected and excluded shots keep their own distinct look.
2. **CEP circles in their own color, and wider than the shot outlines.** CEP 50 and CEP 90 are no longer the shot ink: they are **green**
   (Alan: "Bring back the green color for the group center and the CEP circles") and drawn with a clearly thicker stroke than any shot
   outline. Keep them distinguishable from each other as well, by dash or weight, and say which in the key.
3. **Add CEP 95**, in the same green family, distinguishable from 50 and 90.
4. **Toggles.** CEP 50, CEP 90, CEP 95 and the extreme spread line each get an on and off toggle beside the plot, touch sized and
   keyboard reachable. Defaults: CEP 50 and CEP 90 on, CEP 95 off, extreme spread on. The choices are remembered between sessions. The key
   lists only what is shown.
5. **Extreme spread stays red.**
6. **The group center: green, as full length lines.** Instead of a small cross, one horizontal and one vertical line across the whole plot,
   through the group center, in green.
7. **The bull's center (the point of aim): full length lines too, in a second high contrast color** that cannot be confused with the green,
   the red, or the shot ink. Alan: "They should also be high contrast colors that are separate and easy to identify." Choose it for both
   the light and dark themes and check its contrast against the paper in each.
8. **The bull's rings: wider and low contrast.** Draw the composite bull's rings as a wider stroke in a light gray (dark theme: the
   equivalent low contrast gray), so they read as background and the shots, CEPs and lines read in front of them.

## 2. Constraints

- **Draw order**, back to front: rings, shot outlines, CEP circles, extreme spread, center lines, selection. Nothing important hidden
  under the rings.
- **Color vision.** Red and green alone must not be the only difference between the extreme spread and the group's marks. They already
  differ in shape (a line against circles and crosshairs), which is enough, but check the whole plot through a deuteranopia simulation and
  say what you checked.
- **Theme tokens.** New colors go in the palette the plot already reads (`inks`), for both themes, not as literals in the drawing code.
- **The saved and printed report** draws the plot the same way, with the same toggles as on screen, or the report says what it left out.
- **Compare** and any other place the composite plot is drawn follows the same rules.

## 3. Tests and the report

A headless render test that checks the stroke widths and opacities are in the order above, that the toggles add and remove exactly their
mark and key entry, and that the defaults are as listed. Then a before and after picture of the sample scan's composite plot, in both themes,
saved under `docs/figures/` for Alan to look at, and the report in plain words: what changed and which build it is in.

---

## 2026-09-25, entry 203: the consent choices on the first run screen are cut off

**Status: actioned 2026-09-25, every section.** Every radio and check box with words that can be long now shows them in a wrapping text block, and the readers read that. `Entry203Tests` fails on the old first run card at the default width. The question after Accept and analyze is checked only at widths the analysis screen fits, because below about 1060 units the whole right column runs past the window: question 58. No consent level is ever preselected, and a test holds it. The consent wording is unchanged.

Alan opened nightly 102 and got the first run question "Send your targets to help improve GroupLab?". The two consent choices run off the
right edge of the card and are cut mid sentence: "Testing only. I took these photos, or I have permission to share them. GroupLab may use
them to test a" and "May be published. I took these photos, or I have permission to share them. GroupLab may use them to". Everything else
on the card wraps. A person cannot read what they are agreeing to, which for a consent choice is the one text that must never be cut.

## 1. The cause, as read from the code

`MainWindow.Sending.cs` line 308 onward builds both radio buttons with `Content = "Testing only. " + terms.TestingText` and
`"May be published. " + terms.PublishableText`: a plain string, which Avalonia shows on one line. The Settings section's radios (line 383)
are built the same way and will have the same fault.

## 2. What to do

1. Give every radio button, check box and button whose text can be long a wrapping text block as its content (`TextWrapping.Wrap`), in the
   first run screen, the question after Accept and analyze, the Sending targets and error report sections of Settings, and anywhere else
   the same pattern appears. `PressSend` and any test that reads `Content as string` must read the text block instead.
2. **A test that no text is cut, anywhere it matters.** Render the first run screen, the sending question and each Settings section at the
   narrowest window the application allows and at 150 and 200 percent display scale, and fail if any text block, radio or check box is
   wider than its container or ends in a clipped line. Consent text first; if a general check is practical, run it over every dialog.
3. **Confirm nothing is chosen for the user.** The screenshot shows "May be published" selected, which may simply be Alan's click. Check
   that neither level is ever preselected, on the first run screen, the question or in Settings, and that a test holds it. Consent is chosen,
   never defaulted.
4. Keep the full consent wording exactly as it is; this is layout only.

Report in plain words for Alan: fixed in which build, and whether any other screen had text cut off.

---

## 2026-09-25, entry 202: request 26 done, the Fold 7 is paired

**Status: actioned 2026-09-25, except what needs hands.** Request 26 closed. The spike runs on the Fold 7's cover screen: detection 17.1 s and 714 MB on the sample scan, the photograph refused as on the desktop; `docs/ANDROID.md` section 5 has both beside the desktop's. Two defects found on the phone and fixed on the way: the ArUco and WeChat bindings had compiled to nothing, and the asset list took the system's own images. **Not done**: the inner screen, folding, turning and the font size, which are request 27. The phone's address is in no file.

Alan paired the Fold 7 over wireless debugging on 2026-09-25. `adb devices -l` lists it as `device`, `model:SM_F966U1`, `product:q7quew`,
`device:q7q`. Close request 26.

Now finish entry 198's first stage on the phone: install the spike APK (from the `android` workflow's artifact, or built here now that the
workload and SDK are installed), run it on the cover screen and the inner screen, and fill in `docs/ANDROID.md` section 5 with the phone's
times and peak memory beside the desktop's. Use `adb` yourself; the connection is on the local network and needs no approval from Alan.
Do not write the phone's address or port into any file or log; it changes each time anyway.

Wireless debugging turns itself off after a while. If `adb devices` no longer lists the phone, do not stop: put one line in for-alan.md
asking him to turn Wireless debugging back on and run the `adb connect` line with the address the phone shows, and carry on with anything
that does not need the phone. The fold, unfold and rotation checks of `docs/ANDROID.md` section 7 need Alan's hands; when the spike is on the
phone, write them as a short request with exactly what to do and what to look for.

Report in plain words for Alan: does detection run on his phone, how long it takes, how much memory it uses, and whether the layout
survives folding and turning.

---

## 2026-09-25, entry 201: request 25 done; correction to entry 200 section 2's diagnosis

**Status: actioned 2026-09-25, every part.** Request 25 closed and rewritten with the commands that worked, in order, waiting for the workload install first and keeping `RestoreConfigFile`. Entry 200's NuGet reading, which had gone into request 25 and the results, is corrected there, and nothing about it went into STATE.md.

Read with entry 200.

Alan reran the SDK step with `-p:RestoreConfigFile=C:\Dev\grouplab\nuget.config`: `Build succeeded in 23.0s`, and
`C:\Dev\tools\android-sdk\platform-tools\adb.exe version` prints `Android Debug Bridge version 1.0.41, Version 36.0.0-13206524`.
**Request 25 is done**; close it.

**Correction:** `dotnet nuget list source` shows his user level configuration does have `nuget.org [Enabled]` at
`https://api.nuget.org/v3/index.json`. So entry 200's reading, that his user configuration lacked nuget.org, was wrong. The likelier cause
of the first failure is ordering: he ran the SDK step in an ordinary PowerShell window while the workload install was still running as
administrator in another, so the restore ran against a half installed workload. The retry with the repository config also worked, which
does not prove which of the two mattered. Do not add entry 200's NuGet claim to STATE.md. In request 25's rewrite, say to run the SDK step
only after the workload install has printed `Successfully installed workload(s) android.`, and keep `-p:RestoreConfigFile` as harmless.

Request 26, pairing the Fold 7, is next for Alan.

---

## 2026-09-25, entry 200: request 24 passed; request 25 half done, the SDK step failed on NuGet sources

**Status: section 1 actioned 2026-09-25**: issue 1 checked against request 24 and closed with a note, error reports switched on in 8725f91, request 24 closed. **Section 2 waits on Alan's retry**: request 25 says what is done and what failed, and is rewritten with the commands that worked when his answer comes; his NuGet settings are not touched.

## 1. Request 24 passed: switch error reports on

Alan, 2026-09-25: `send-test-error-report.py` printed `sent: the receiver took it`, and after starting the worker by hand its log read
`2026-09-25T03:29:32Z 3a6cc8fc9476: opened issue 1 for TestReport in ErrorReportCheck.Send`. Check issue 1 in the private repository
looks as request 24 described, then switch automatic error reports on in their own commit with a `Release-note:` trailer, close request
24, and start reading the open issues at the start of each run as entry 194 section 4 says. Alan was told to close issue 1 after looking;
if it is still open, close it yourself with a note that it was the test.

## 2. Request 25: the workload is installed, the SDK is not

- `dotnet workload install android` (as administrator) succeeded: workload version 10.0.401, Microsoft.Android.Sdk 36.1.69 and 35.0.105,
  `Successfully installed workload(s) android.`
- `dotnet new android -o "$env:TEMP\gl-android-probe"` succeeded.
- The `InstallAndroidDependencies` build failed at restore, before fetching anything:
  `error NU1100: Unable to resolve 'Microsoft.NET.ILLink.Tasks (>= 10.0.12)' for 'net10.0-android'` (and the same for android-arm64 and
  android-x64).
- `setx ANDROID_HOME C:\Dev\tools\android-sdk` succeeded, so the variable now points at a folder that may not exist yet.
- The JDK path in the request is right: `C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot` is the only folder there.

The planning session's reading: the probe sits in `%TEMP%`, outside the repository, so the repository's `nuget.config` (which clears the
sources and adds nuget.org) does not apply, and Alan's user level NuGet configuration evidently has no usable nuget.org source. The planning
session has given Alan a diagnostic and a retry that points the restore at the repository's config with `-p:RestoreConfigFile=...`, which
changes nothing on his machine. When his answer comes back:

1. If the retry works, rewrite request 25 so its commands are the ones that worked, and add the NuGet source finding to STATE.md's list of
   things that would surprise somebody, because every build outside the repository on this machine will hit it.
2. Do not change Alan's user level NuGet configuration yourself. If a permanent fix is wanted, write it as a request with the one command.

---

## 2026-09-25, entry 199: Android addendum: every screen size, touch first, and QR codes as the no-account way to move data

**Status: recorded 2026-09-25 in `docs/ANDROID.md` sections 7 and 8, with entry 198.** Not done: section 1.5, the spike on both of the Fold 7's screens and the tablet, waits on requests 25 and 26; section 2.2's measurement of a code read off a laptop screen needs the phone, and the byte count is worked out rather than measured. One change to the order: Stage B, the sync folder, is marked doubtful on Android, because the Drive and OneDrive applications offer files, not folders, to Android's folder picker.

Read with entry 198, and fold both into `docs/ANDROID.md`. Alan, 2026-09-25.

## 1. Phones, foldables and tablets, touch first

Alan: "The app should be built to work on phones, folding phones, and tablets and be dpi and screen size aware and scale itself
appropriately. The interface needs to work well with touchscreens."

1. **Layout by available width, not by device type.** Classes such as compact (phone, and the Fold 7's cover screen), medium (the Fold 7
   unfolded, small tablets) and expanded (Tab S8 Ultra, landscape). One screen rearranges; it is not three apps. The desktop keeps its
   own layout.
2. **Folding and rotating are ordinary events.** Unfolding the Fold 7 mid-review, or turning the tablet, keeps the photograph, the marks,
   the zoom, the selection and any half-finished edit, and relays out within a moment. Test it: a review in progress survives a
   configuration change from compact to medium and back. Respect the hinge if the platform reports one (a split layout must not put a
   control under the fold).
3. **Density aware.** Sizes in density independent units, text that follows the system font size (including the largest accessibility
   sizes without clipping), and images and the target drawn crisp at the screen's real density.
4. **Touch first.** Targets at least 48 dp. Pinch to zoom and two finger pan on the photograph; one finger drag moves a shot only when
   a shot is grabbed, never by accident while panning. Long press where the desktop has right click. No hover dependent information:
   everything the desktop shows on hover (the glossary tooltips included) is reachable by tap. Precise placement of a shot uses a
   magnifier or offset handle so the finger does not hide what it moves; say which, and test it on the Fold 7's cover screen, the
   hardest case.
5. **The spike in entry 198 section 2 runs on both of the Fold 7's screens and on the tablet** and reports whether Avalonia on Android
   handles the density, the fold and the rotation correctly. If it does not, that is a finding that affects the UI decision in 198 2.1.

## 2. QR codes to move data without an account

Alan asked whether a QR code could share data between devices, as a backup or no-account option. The planning session's reading, for you
to confirm or correct with measurements:

1. **A QR code cannot carry a session with its photograph.** One QR code holds at most about 2.9 KB, and far less when read reliably off
   a screen; a photograph is megabytes.
2. **It can carry the marks.** A session's shots (positions on the sheet, not in the photo), the target definition's id, the caliber, the
   distance, the load data and the choices made in review are a few hundred bytes to a few KB, compressed. GroupLab already writes compact
   binary frames into the QR codes on its printed sheets (`Gltd/Binary`, `InstanceCodec`). A **marks QR** shown on one device and scanned
   by the other rebuilds the session's figures and draws the shots on the rendered sheet, with no photograph. It works offline, with no
   network at all, phone to desktop or phone to phone. Measure how many shots fit in one code that a phone reads off a laptop screen at
   arm's length, and what happens above that (several codes in turn, or say "too large, share the file").
3. **It can pair the two devices for a full transfer.** The desktop shows a QR code with a one time key and its local address; the phone
   scans it and sends the whole session, photograph included, straight across the home or range Wi-Fi. No account, no internet, nothing
   leaves the local network, and the key means nothing else can send. Say what it needs on Windows (a firewall prompt, and how it is
   explained to the user) and what happens when the two are not on the same network.
4. **The order this sits in, with entry 198's stages:** A, share a file by hand; then these two QR routes, which need no account; then B,
   the sync folder; and C, sign in, only if needed. Say if a different order is better.
5. None of this changes the photograph rules: no GPS, location or time metadata read, printed, logged or sent.

Not part of the first stage. Record the plan in `docs/ANDROID.md`; build it when the app has sessions to move.

---

## 2026-09-25, entry 198: the Android application starts

**Status: actioned 2026-09-25 as far as it goes without the phone.** Sections 2.1, 2.2, 2.3, 2.5 and 2.6 are answered in `docs/ANDROID.md`, and the spike, the native build script and the `android` workflow are written. Not done: section 2.4 on the phone, which waits on requests 25 (the workload and SDK) and 26 (the Fold 7 paired); whether the native library builds is the first `android` CI run's result. Section 3.1 and 3.2 are requests 26 and 25; 3.3 and 3.4 come later by the entry's own words; section 4 is recorded in ANDROID.md and STATE.md.

Alan, 2026-09-25: start Android development now. Do this after entries 196 and 197; it is large, and this entry is its first stage only.
`docs/PLATFORM-SUPPORT.md` already calls Android planned and high priority, and `docs/MOBILE-CAPTURE.md` is the capture contract
written for it. Both hold.

## 1. Alan's decisions

1. **Scope of the first version: full analysis on the phone, offline.** Take the picture (the capture screen of MOBILE-CAPTURE.md),
   detect, review and correct, the group figures, save sessions. The same engine as the desktop, not a second one. Printing targets,
   the target library editor and Ballistics stay desktop only at first. Ranges often have no signal: nothing in the first version may
   need the network except sending a target and error reports, which queue as they do on the desktop.
2. **Distribution: Google Play testing tracks plus a nightly APK** on the GitHub release and grouplab.org for sideloading. Alan has
   paid the Google Play developer fee. A newer personal Play account must run a closed test with at least 12 testers for 14 days
   before a production listing; the Discord server is where those testers come from. Plan for it, do not promise dates.
3. **Package name: `org.grouplab.app`.** Permanent once on Play.
4. **Sessions between phone and desktop.** Alan's wish: sign in with Google, Microsoft or Apple and use that platform's own storage to
   share files between the apps, with no server of ours. If that is a lot of work, start with sharing files by hand. The planning
   session's reading, for you to confirm or correct with reasons:
   - **Stage A, first version:** share and open a session file by hand (Android share sheet, Drive, email, USB). Nothing else.
   - **Stage B, cheap and close to his wish:** a "sync folder" setting on both. On Android the user picks a folder through the Storage
     Access Framework, which Google Drive and OneDrive both provide as document providers; on the desktop the user picks the folder the
     Google Drive or OneDrive client already syncs. Both apps read and write sessions there. No sign in, no app registrations, no
     tokens, and it uses each person's own storage. Say whether SAF providers are reliable enough for this (conflicts, offline edits).
   - **Stage C, only if B falls short:** sign in and the providers' APIs (Drive app data folder, OneDrive app folder through Microsoft
     Graph). Needs a Google OAuth client and consent screen and an Entra app registration, which Alan would create. **Apple is out**
     for now: iCloud needs the paid Apple developer program, which Alan will not pay for a platform he does not own.
   - Whatever the stage, the session file format is the unit, a session edited on two devices must never silently lose one side's
     changes, and photographs keep the rules they have today: no GPS, location or time metadata read, printed or logged.

## 2. The first stage: prove the engine runs on the phone

Before any screen is designed, answer the questions that decide the architecture, with measurements, in a new `docs/ANDROID.md`:

1. **UI.** Avalonia on .NET Android, sharing `GroupLab.Core` and as much of `GroupLab.App` as fits a touch screen, is the obvious path.
   Confirm it, or say why not. No second codebase in another language unless the measurements force it.
2. **OpenCV on Android.** The detector uses OpenCvSharp, whose official runtimes are Windows, Linux and macOS. Find what runs on
   android-arm64: a community runtime (for example the Sdcb mini runtimes on NuGet), OpenCV's own Android build under the existing
   wrapper, or replacing the few OpenCV calls GroupLab actually makes with managed code. List the calls the detector uses, decide, and
   check the license of whatever is chosen against GPL-3.0.
3. **Camera.** Avalonia has no camera. Name the route: CameraX through the .NET Android bindings is likely. It must give full
   resolution stills, the lens choice of MOBILE-CAPTURE.md item on focal length, focus and exposure control, and a live preview fast
   enough for the capture conditions.
4. **Speed and memory.** A spike APK that loads the sample scan and a phone photograph from the app's own assets, runs detection,
   and prints the time and peak memory. Target devices: Alan's **Samsung Galaxy Z Fold 7** (daily phone; it has a narrow cover screen and a
   wide inner screen, so both layouts matter) and his **Galaxy Tab S8 Ultra**. Say what the desktop takes for the same image.
5. **Minimum Android version.** Choose the lowest that CameraX, the chosen OpenCV route and .NET 10 support without special cases,
   and say what share of devices that leaves out.
6. **Builds.** A CI job that builds a debug APK on every push to main that touches the app or Core, and a signed release APK and AAB
   for nightlies once signing exists (section 3). Unsigned debug APKs are fine until then; never publish one as a nightly.

Stop after the spike and the document, with a report in plain words: does detection run on the Fold 7, how fast, and what the plan is.
No screens beyond what the spike needs.

## 3. What only Alan can do, as requests in for-alan.md, when you reach them

1. **The phone for testing:** Developer options on, Wireless debugging (or USB debugging) on, and the one `adb pair` or `adb connect`
   line for this machine. He keeps Android tools in `C:\Dev\tools` for another project; use those or say what to install.
2. **The .NET Android workload** if it is missing: the exact command, since it installs software.
3. **The signing key, later:** Alan generates the upload keystore himself with `keytool`, keeps it outside the repository, and puts it
   in GitHub secrets; Play App Signing holds the app key. You never read, copy or print the keystore or its passwords, exactly as with the
   SSH key. Write the exact commands and the secret names when the release build needs them, not before.
4. **The Play Console listing,** when there is something to put on it: the app entry, the closed testing track and the testers list.

## 4. Things to keep in view, not to act on now

- **The GPL app store permission.** Alan approved a draft GPL section 7 additional permission for app stores and is having an attorney
  review it before it is committed. Internal and closed testing can go ahead; a public Play listing waits on that review. Say so in
  `docs/ANDROID.md` and in STATE.md.
- **iOS is not planned.** Alan owns an iPad Mini for testing the website only.
- The capture screen's specification, `docs/MOBILE-CAPTURE.md`, is the contract; where Android makes an item impossible, say which and why.

---

## 2026-09-25, entry 197: entry 196 narrowed; the zeroing grid is a print aid, not a scanning target

**Status: actioned 2026-09-25, with entry 196, every section.** Section 2.1's one bull sheet is the MOA zeroing sheet: it is the library's only one bull sheet, and the 5x5 sheet with one bull left cannot be drawn, because its markers come from the lattice its bulls make and every sheet must encode into its own codes. Section 2.2's ragged hole is flagged only once the rounds fired are entered, and a touching pair left whole likewise; question 57 asks whether a sheet of two to four marks may flag a mark against the others.

**Read this before entry 196, and action the two together as this entry says.** Alan and the planning session agreed on 2026-09-25.

## 1. Why

Alan: the zeroing grid is for sighting in by eye at the bench. Fire, read "1.2 mil high and 0.8 right" off the grid, dial, fire again.
A statistical zero comes from the 25 bull sheets. Scanning a zeroing grid afterward loses the order of shots and adjustments, which is
the only thing that mattered, and he never planned to scan one; the grids exist because they print at a perfect scale. Alan has asked
Unholy what he expects a zeroing grid to do; if his answer changes this, it will come as a new entry. Until then, this stands.

## 2. What of entry 196 to do, reframed

1. **Section 2.1, for every sheet with one scoring bull, not for zeroing grids as such.** Single bull group targets are common: 100 yard
   sight in targets, benchrest group targets, many commercial sheets. A sheet with exactly one scoring bull expects a group on it: every
   shot to that bull, no "holds N shots" item, no flag on every shot for outnumbering the bulls, and a gate that covers the whole sheet.
   The same test in section 2.2's style, but on one of GroupLab's own single bull sheets if the library has one, otherwise a synthetic one
   bull definition, not on the zeroing grids.
2. **Touching holes and the ragged hole, for any sheet.** Section 2.2's touching pair, pair across a printed line, and three shot ragged
   hole, and section 2.3's Three shots choice, belong to tight groups everywhere. Test them on a single bull sheet and on the 25 bull sheet.
3. **Zeroing grids keep only what they have:** the every-sheet test and Unholy's scan test, so a scan never breaks and never shows a blank
   result. No zeroing grid specific tests beyond that, and no further work to make them a scanning target.
4. **Drop section 2.4:** no request to Alan for a scanned zeroing grid.
5. **Section 1.2's question about the gate** still gets answered in the report, since it applies to any one bull sheet.

## 3. Say what the zeroing grids are for

In the target library's description of each zeroing grid, the site's target pages and the user guide: a sheet for sighting in by eye,
printed at exact scale, read off the grid at the bench; for a statistical zero and group figures, use a 25 bull sheet, with a link. One
or two sentences, plain, no claim that scanning it is useful. GroupLab still accepts a scanned one without complaint.

## 4. The report

Plain words for Alan: what a five shot group on a single bull sheet looks like after detection, touching and ragged holes, and the new
wording for the zeroing grids.

---

## 2026-09-25, entry 196: a zeroing grid with a group on it, touching holes, and one ragged hole

**Status: actioned 2026-09-25 as entry 197 narrowed it.** Section 2.1 applies to every sheet with one scoring bull, not to sheets with a grid. Section 2.2's tests are on a one bull sheet and the 25 bull sheet, not the four zeroing grids. Section 2.3: three places are not offered, for the reason in PHASE1-RESULTS; the sentence says three only from a named caliber. Section 2.4 was dropped by entry 197. Section 1.4 was wrong in one part: a ragged hole on a sheet of few marks was not flagged at all.

Alan asked whether the zeroing grid finds more than one shot, and shots that touch. The planning session read the code and tests. What it
found, then what to do. Do this after entry 195.

## 1. What is true today, as read from the repository

1. **Detection of several shots:** `EverySheetDetectsTests.HolesOnAZeroingGridsLinesAreFound` puts five separate holes on and across the
   lines of each of the four zeroing grids, and all five are found. Synthetic only; the one real scan (Unholy's) has one shot.
2. **Assignment of several shots:** a zeroing grid has one scoring bull, and nothing in its definition or the default `AssignmentRule` says
   that bull takes more than one shot. With more shots than bulls, `ShotAssignment` gives each shot to its nearest bull within the gate and
   **flags every shot**, and `ReviewQueue` then raises "Bull 1 holds 5 shots ... The sheet expects one a bull". So a normal five-shot zeroing
   group arrives with a review item on every shot, which is the friction Unholy has been reporting. A shot farther than the gate from the
   one bull, which on a zeroing grid is exactly the first shot of a rifle that is far off, may be left unassigned. Say what the gate is
   on a zeroing grid and whether the grid's whole area is inside it.
3. **Two touching holes:** split by shape (`CalibreSplitTests`), with a named caliber stopping false splits; one left whole is flagged
   oversized with a Two shots choice. Not tested where the pair sits on or across a grid line.
4. **Three or more through one ragged hole:** stays one mark, flagged oversized; the review offers One shot, Two shots or Not a shot, so a
   person must add the third shot by hand.

If any of this is wrong, say so in the report; it was read from code, not run.

## 2. What to do

1. **A sheet with one scoring bull expects a group on it.** For a definition with exactly one scoring bull, and for any sheet with a grid,
   the default is every shot to that bull with no limit: no "holds N shots" item, no flag on every shot for having more shots than bulls,
   and a gate that covers the whole printed grid (or the page), so a far-off first shot is still that bull's. Only real doubts (a mark
   that may be two, a candidate refused) reach the review. Consider whether the definition format should say this explicitly, for
   example a per-bull expected count, rather than inferring it from the count of bulls; your call, with the reason.
2. **Tests on all four zeroing grids,** from renders with synthetic holes, counted exactly:
   - a five-shot group about 1 in across, including one touching pair, and a pair straddling a line;
   - one shot 2.5 in from the aim point, at the grid's edge;
   - a three-shot ragged hole, which must at least be flagged as more than one shot.
   Each asserts the count found, that every shot is on the one bull, and that the review holds only the items section 2.1 allows.
3. **The ragged hole review:** where the mark's area holds about three holes of the named caliber, offer Three shots as well, placed from
   the mark's shape the way Two shots already is, or say in the report why that cannot be placed honestly and what a person does instead.
   Never guess a count without a caliber; say it needs one.
4. **A request for Alan, optional:** a real zeroing grid with a five-shot group and at least one touching pair, scanned at 600 dpi, as the
   first real test. One line in for-alan.md, no deadline.

## 3. The report

Plain words for Alan: what a five-shot zeroing group now looks like after detection (how many review items, if any), and what happens
with touching holes and a ragged hole.

---

