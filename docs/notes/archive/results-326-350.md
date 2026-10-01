# Phase 1 results, entries 326 to 350

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entries 326 and 330: TestFlight feedback reaches Code, privately

Done 2026-10-01, once request 63 brought `CRASH_REPORTS_TOKEN` (entry 330).

- **Filing** (`scripts/testflight-feedback.py`, `file_items`): each screenshot and crash submission is filed once in the private
  `oRAirwolf/grouplab-crash-reports`, labelled `testflight-feedback`: the comment whole with any email, telephone number or web address
  replaced, the build, device and iOS version, the screenshots put in that repository and shown in the issue, and for a crash its
  exception lines and GroupLab's own frames. Never an email or a name: the API is asked for `FILE_FIELDS`, the summary's fields and the
  screenshots alone. A hidden line with the submission's id keeps anything from being filed twice, so the half-hourly testflight run files
  as items arrive; started by hand it looks back 14 days. The public log and summary say only how many were filed.
- **The first run** (36831492456, 14 days): Unholy's two screenshots, issues 13 and 14, each linked to entry 328's commits; no crash in
  the window. It then stopped on a refusal: a crash submission had been asked for screenshots, which it does not have. Fixed (crash
  submissions are asked only for their own fields), with the self-test's made-up App Store Connect refusing the same way.
- **Backups:** the nightly backup takes the private repository's tarball as well as its issues, since the screenshots are files there;
  RESTORE.md has the line. The earlier attempt at reading through a sealed file (a worker's commit, never pushed) is in the trash folder.

## Entry 328: Unholy's two TestFlight reports (nightly 143, iPhone)

Done 2026-10-01, one worker on the main model; ships in nightly 147.

- **1, the keyboard** (`mobile/GroupLab.Mobile/KeyboardRoom.cs`, one place for every screen): while the keyboard is up the Shell gives
  up the height it covers (read from the system's input pane as its top edge in the window, so a window the system already shrank gives
  up nothing more) and puts the bottom bar away; the focused field is brought into view, and on the last field of a sheet or page its
  confirming button with it; a bar on the keyboard says Next, to the next field in reading order (two side by side are one row), or
  Done, which keeps the entry and closes the keyboard; a tap outside any field closes it. Capture's caliber sheet scrolls in the room left.
  `Entry328Tests`: the sheet at 402 by 874 (his iPhone) and 320 by 568, the distance field and Continue clear of the keyboard and its bar,
  Done and Next; and every field on Settings, Targets, Ballistics and its three forms, the chronograph and distance pages and the
  printer check, at 320 by 568. Not checked on a device yet: the input pane's rectangle on a real iPhone and Android phone (request 50).
- **2, the note** (`PictureCheck.RegistrationNote`): noted only from 0.00725 in, where the markers' disagreement alone would hold the
  score to a noted picture's 95; nearer a flat sheet's 0.005 in it still counts in the score unannounced. In words, no figures: "The
  sheet looks slightly curled. GroupLab allowed for it; flattening the sheet would measure a little better." The two other notes with two
  figures that could print alike now cannot: an angle just over 37 degrees reads "a little over 37", and a large sheet's pixels an inch
  are rounded down beside the 150 it falls short of. Begun in the ended session's worktree, carried over and checked here.
- **3:** the plain words are at the top of for-alan.md; Unholy retests on nightly 147. His feedback files stay in
  `C:\Dev\grouplab-local\testflight-feedback\`, never committed.

