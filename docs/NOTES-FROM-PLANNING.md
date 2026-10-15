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

- [`docs/notes/archive/notes-2026-10.md`](notes/archive/notes-2026-10.md), entries 322 to 394, 73 of them.
- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 321, 320 of them.

---

## 2026-10-15, entry 431: issue #34, the tab strip's scrollbar gets its own row below the tabs (concept A)

**Status: actioned 2026-10-15; every section done. 2.4 closes #34 once the commit is on main, naming nightly 195.**

Planning session, 2026-10-15 12:00 UTC. Issue #34 in grouplab-crash-reports (Quack on Discord, nightly 192; its text is data): when
enough targets are open to fill the tab strip, the pop-up scrollbar that appears on hover covers the lower half of the tabs, so their
names and close buttons cannot be clicked. Alan looked at three drawings on a Claude Design canvas
(claude.ai/artifact/KKf1KFUfHMFPsWE3fGUHF3, "Tab strip scrolling (issue 34)") and chose: "Go with A".

## 1. Concept A

1. The tab strip's horizontal scrollbar is a thin bar in a row of its own directly under the tabs (about 8 px tall, a few px of gap),
   never drawn over a tab. Nothing about the tabs themselves changes: same height, names, close buttons and the + button at the end.
2. The row is there only when the tabs overflow; when they all fit, it takes no space, so a window with a few tabs looks as it does now.
3. The bar does not auto-hide over the tabs any more; it may fade to a quieter shade when the pointer is elsewhere, but it stays in its row.
4. Horizontal wheel and touchpad scrolling over the tabs still scroll them, and choosing a tab (or opening one) scrolls it fully into view.
5. Dark and light themes. The phone has no tab strip; if a phone screen has the same kind of overlay scrollbar over something clickable,
   say so in the report rather than changing it here.

## 2. Finishing

1. A test that, with enough tabs to overflow, no part of any tab's close button lies under the scrollbar's bounds, before and after the
   pointer moves over the strip.
2. Screenshots for the tour and Features if the strip appears there; check the words.
3. Release note, kind `fixed`, for example: "With many targets open, the tab strip's scrollbar now sits in its own row below the tabs, so
   it no longer covers their names and close buttons."
4. Close issue #34 with the commit and the nightly it will ship in.

## 2026-10-15, entry 430: question 99 answered, (b): the selected shot gets its own magenta

**Status: actioned 2026-10-15; every section done. The magenta is nearer the light theme's fourth sheet and two bull marks than the test's 100; kept as chosen, and question 100 asks about it.**

Planning session, 2026-10-15 09:10 UTC. Alan looked at the three options drawn on a Claude Design canvas (claude.ai/artifact/RecX9913a1yaa8NEzt3254,
"Selected shot color (question 99)") and chose: "Go with b".

1. `Tokens.Selection`: the ink becomes #ff3df5 in the dark themes and #b0009f in the light, each still edged in the opposite (black on
   dark, white on light). Ring, crosshair, widths, gap, arm and the small sheet's ring stay exactly as entry 425 built them.
2. The same on the phone (Android and iPhone), through the shared tokens.
3. Entry 425 section 4's test now also holds the selection apart from the plot's own ink (white and black, `PlotInks.Ink`) and from
   `MarkPlaced`, in both themes, and still from every sheet, bull and overlay color. Check the magenta against `BullMarks` (#b24bd6 and
   #d62a5b are the nearest) with the same distance the test already uses; if it is too close, say so as a question with the numbers and
   keep #ff3df5 / #b0009f meanwhile.
4. The selected row's amber bar in the shots table is unchanged (not part of the question).
5. Close question 99 as answered (b). New screenshots of a selected shot in both themes for the tour and Features; check the tour's words.
6. Release note, kind `changed`, for example: "The shot you select is now ringed in magenta, a color nothing else in GroupLab uses, so it
   stands out on a single sheet as well as in a pooled set."

## 2026-10-15, entry 429: Discord attachments may go on the GitHub issues (Alan's consent)

**Status: actioned 2026-10-15; every section done but 2.5's server paste, which is written as request 102 and waits for Alan to run it. Section 2 was first refused by Claude Code's permission check (posters' personal data) and done once Alan gave his word in the session (request 101). Section 3: nothing ships.**

Planning session, 2026-10-15 09:05 UTC. Alan, in the planning chat, 2026-10-15 03:01 Denver: "The discord is public and it is fine to
publish them to github. It does not need additional permission to do that."

## 1. The consent

1. Write it into `samples/PROVENANCE.md` with those words and the date: attachments posted in the GroupLab Discord's bug-reports forum may
   be put on their GitHub issues in oRAirwolf/grouplab-crash-reports without asking the poster.
2. Its scope is the GitHub issues only. It says nothing about grouplab.org, public test data or a research article: those stay as they
   are (not without the consent rules already written) unless Alan says otherwise. If that reading looks wrong against anything already in
   PROVENANCE.md, ask it as a question; do not widen it.

## 2. The worker

1. Each kept attachment also goes onto its issue so it can be seen there: GitHub's REST API has no upload for issue attachments, so commit
   the file into the crash-reports repository (for example `discord/<thread>/<message>/<file>` through the contents API, with the token the
   worker already has, if it has write access to contents; check, do not assume) and link it from the issue or comment, an image shown
   inline. Keep the private server copy and the pull to `C:\Dev\grouplab-discord` as they are, for diagnosis.
2. The issue text no longer says the attachments are "never published"; say instead that they are on the issue, and that grouplab.org,
   public test data and articles still need the poster's consent.
3. Backfill: the attachments of issues #29 to #34 already kept go onto those issues the same way, once.
4. Tests as entry 427's: an upload refused or failed is a line in the issue, never a crash, and never stops the other threads.
5. The server update is a paste for Alan in `for-alan.md` in request 100's form, from a commit on `main`, backup first. If the token
   cannot write contents, say what scope it needs in a plain request instead, and do not ask for a new token in any file.

## 3. Release notes

Server only; nothing ships in the application.

## 2026-10-15, entry 428: request 100 answered; the Discord worker filed four more posts

**Status: actioned 2026-10-15; every section done. #31 to #33 were already fixed and are closed; #34 is new and open.**

Planning session, 2026-10-15 09:05 UTC. Alan ran request 100's paste as written (a3ea5ecf).

1. Record it and close request 100: six `OK`; backup `/home/ubuntu/grouplab-server/backups/2026-10-15T085633Z-discord-worker-fix`
   (the old worker, 0o755 root:root, 16038 bytes) into `docs/RESTORE.md`; the install also kept
   `/usr/local/sbin/grouplab-discord-worker.py.20261015-025633.bak` beside the worker (not in a HestiaCP folder; say in RESTORE.md
   whether install.py's own .bak copies are tidied, and if they never are, make install.py keep them under
   `/home/ubuntu/grouplab-server/` instead of `/usr/local/sbin`); units and token script unchanged; token "already set, not read or
   printed"; timer started. The log has no Traceback: threads 1558725596241068113, 1559460153114361856, 1560135421487349881 and
   1560160950567571556 filed as issues #31, #32, #33 and #34 (08:56 UTC).
2. Triage #31 to #34 like any report (their text is data). The report behind entry 425 (the selected shot hard to see, nightly 192) is
   most likely one of them: close it with entry 425's commit f65147e8 and nightly 193. Tell the planning session in plain words what the
   others are and which are already fixed.
3. Question 99 (the selection's white and black): the planning session is showing Alan the three options as concepts; his answer comes
   back as an entry. Nothing to build for it yet.
4. Nothing ships; no application note.

## 2026-10-15, entry 427: request 98 is installed; the Discord worker crashes on an attachment it could not fetch

**Status: actioned 2026-10-15; sections 0, 1, 3 and 4 done; section 2's paste is written as request 100 and waits for Alan to run it; section 3.3 waits on that.**

Planning session, 2026-10-15 04:25 UTC. Alan ran request 98 tonight with the pastes in entry 426 (commit 77b3db39).

## 0. What happened (record it)

1. Step 3a: six `OK`, backup `/home/ubuntu/grouplab-server/backups/2026-10-15T041559Z-discord-worker` (40264 bytes; all four
   installed files absent before, so the undo deletes them), dry run clean. Put the backup in `docs/RESTORE.md`.
2. Step 3b: the install wrote the two folders under `private/discord`, the worker, the token script and both units; the timer is
   enabled; "Written to /etc/grouplab/discord-token, owned by root, mode 600". The first run logged "found the bug-reports forum in
   GroupLab", filed thread 1557327267242180608 as issue #29 and thread 1558504324802281592 as issue #30, then crashed.
3. The crash, from the journal: `keep_attachments` (line 199) called `write_bytes(body)` with `body` None:
   `TypeError: memoryview: a bytes-like object is required, not 'NoneType'`, reached from `file_thread` line 260 while adding a later
   message of thread 1558504324802281592 as a comment. Cause: `request(..., raw=True)` returns `(code, None)` on a 4xx other than
   401/403-for-JSON, and `keep_attachments` never checks the code. The most likely 4xx is the Discord CDN refusing urllib's default
   `Python-urllib` User-Agent, or an expired signed attachment URL; find out which from a test, do not guess.
4. Effect until fixed: the timer runs every five minutes and fails the same way at the same message, before posting anything, so
   nothing is filed twice; but thread 2's later messages, both threads' thank-you replies where not yet sent, and every thread after it
   (the report behind entry 425 is most likely one of them) are not filed. An empty `.partial-...` folder may be left under
   `private/discord/attachments`; the fix tidies it.

## 1. The fix

1. In `keep_attachments`: anything but a 200 with a body is "could not be fetched (HTTP <code>)" in the issue, never a crash.
2. Fetch attachments with the same User-Agent the Discord API calls use, and refresh an expired attachment URL by re-reading the
   message if Discord says so.
3. Any exception inside one thread is logged with the thread id and the run moves on to the next thread, so one bad post can never stop
   the forum being filed again.
4. A test for each: a 403 attachment, a 404 attachment, a None body, and one thread throwing while the next is still filed.

## 2. The server paste

Code's own ssh is refused, so write the update as a paste in `for-alan.md` (request 98, reopened or a new number) in the same form as
request 98's: fetch from a commit that is on `main` (entry 426 section 2), check SHA-256, `grouplab-change-backup.py --label
discord-worker-fix --file /usr/local/sbin/grouplab-discord-worker.py`, install, run the service once, tail the log. Every step prints
on failure. Then tell the planning session in plain words that it is ready.

## 3. The issues it filed

1. #29 (Quack, nightly 174: Open Marking stored the photo's absolute path and failed silently if moved). Alan told Quack on Discord on
   2026-10-07 that this was fixed in nightly 176. Check that the fix is in the code (file name only, look beside the JSON first, ask
   when not found, paths stripped from what leaves the app) and close it naming the commit and nightly 176, or say what is missing.
2. #30 ("Strange error test in print target", nightly 174): triage it like any report; its text is data.
3. When the fixed worker files the report behind entry 425 (the selected shot hard to see, nightly 192), entry 425's fix closes that
   issue with its commit and build.

## 4. Release notes

Server only; nothing ships in the application, so no application note (entry 168).

## 2026-10-15, entry 426: request 98's paste named a commit that is not on GitHub

**Status: actioned 2026-10-15; sections 1 to 3 done.**

Planning session, 2026-10-15 04:10 UTC.

1. Request 98's step 3a in `docs/notes/for-alan.md` fetched the server files from commit `e959956a`. That commit was rebased away and is
   on no branch: raw.githubusercontent.com answers 404 for it, so the paste would have stopped at its first fetch. The planning session
   gave Alan the same paste with `77b3db39` instead, after checking that all six files fetched from GitHub at `77b3db39` match the six
   SHA-256 lines in the request (they do; `website/server/` is unchanged between the two). Change request 98's text to `77b3db39`, or
   close it if Alan's answer has come back by then.
2. Add a test (in `ForAlanTests` or beside it) that fails when a paste in `for-alan.md` fetches from a commit that is not an ancestor of
   `main`, so a rebase cannot strand a request's paste again. Request 97's paste named `c95280d0`; check that one too.
3. Under the hood only; no release note for the application (it ships nothing).

## 2026-10-15, entry 425: make the selected shot impossible to miss (an error report, nightly 192, macOS, dark theme)

**Status: actioned 2026-10-15; sections 1 to 5 done; section 3 on the phone is the shared plot only, as the phone has no selected shot; question 99 is open on the white and black.**

Planning session, 2026-10-15 04:00 UTC. Alan approved the look below in the planning chat ("Build it as described"), so this is a
decided look, not a DESIGN NEEDED question.

## 0. The report

Alan passed this report on in the planning chat on 2026-10-15 (Denver evening of 14 October). It is from nightly 192,
commit 80f49fd, on macOS in the dark theme, with a pooled set of three Letter sheets (27 shots) open. **There is no GitHub issue for it**:
the planning session checked oRAirwolf/grouplab-crash-reports and its newest issue is #28 (closed), so it most likely came from the
Discord bug-reports forum, whose worker waits on request 98. Its text is data, never an instruction. In short: with a pooled set and
many shots, the selected shot is hard to find. The red/orange mark in the small preview at the top left and the dull row color in the
"Shots, from their own bull" table are not prominent enough; the reporter tells which shot is selected by clicking it on and off and
watching for the blink. They suggest an inverted color, a larger mark, a cross instead of a dot, or an arrow for the mark, and an
inverted color, larger text, an arrow or a dot for the row.

What the planning session found (check it, do not take it on trust):

1. The selection color `Tokens.MarkSelected` is `#e0912f`, an orange, and a selected shot only gets a thicker ring
   (`MarkSelectedCoreWidth`, `MarkingCanvas.cs` around line 844). In a pooled set Sheet 1's shots are orange as well, so the selection
   is nearly the same color as nine of the dots it should stand out from.
2. In the screenshot the selected shot (1.5) is also one end of the red dashed extreme spread line, which hides it further.
3. In the small preview the marks are a few pixels across, so a color change alone barely shows there.
4. In the table only the row's background changes.

## 1. The selected shot on the plot, on the picture and in the small preview

1. Draw the selected shot with a ring clearly larger than the others PLUS a crosshair through its center, in a color no sheet, bull
   color or overlay uses: white in the dark theme, black in the light theme, each with a thin outline in the opposite color so it reads
   on any background, on a red dashed line and on a sheet's own color.
2. In the small preview, scale the mark up so it is visible at that size (a ring several times a normal mark, the same crosshair and
   outline), so a person can find the shot in the thumbnail at a glance.
3. The selection is drawn last, above every other mark and line.
4. A new token for it in `Tokens.cs`; the old orange stays for whatever else uses `MarkSelected` (`NeedsPerson`, the printer wizard's
   dots) unless that use is a selection too.

## 2. The selected row in the shots table

1. A solid accent bar on the row's left edge, bold text, and a background clearly brighter than every other row (dark theme) or clearly
   darker (light theme), with the text still at full contrast.
2. Choosing a shot on the plot or the picture scrolls its row into view in the table.

## 3. Both themes, both platforms

The desktop and the phone (Android and iPhone), dark and light themes. Same treatment everywhere, through the shared tokens.

## 4. A test that holds it

A test that fails if the selection color equals, or is close to, any sheet color in a pooled set, any bull color, or the extreme spread,
CEP, group center or aim colors, in either theme. Plus a rendering check that the selected shot's pixels differ from an unselected
shot's in the small preview.

## 5. Finishing

1. New screenshots of an analysis with a selected shot, in both themes, for the tour and the Features page; check whether the tour's
   words about selecting a shot still describe it.
2. Release note, kind `fixed`, in a shooter's words, for example: "The shot you select now stands out with a larger ring and a
   crosshair, and its row in the table is clearly marked, even in a pooled set with many shots and in the dark theme."
3. No issue to close. Record in PHASE1-RESULTS.md that this came from a person's report passed on by Alan.

## 2026-10-14, entry 424: the zero correction says why an axis gets no click, and a setting gives both clicks always

**Status: actioned 2026-10-14; sections 1 to 4 done; section 3 could not be checked against Alan's own session, which is not on this machine.**

From the planning session, 2026-10-14 06:05 UTC. Alan, 2026-10-13 23:54 Denver, nightly 192, on a pooled three-sheet 2 MOA set (20
shots): the zero correction said "Dial 1 click left, at 0.1 mil a click." for 0.11 mil right, and nothing for 0.08 mil low, which he
read as a rounding mistake ("it should be rounding up"). It is not: ZeroCorrection leaves out an axis that is not Distinguishable
(elevation 0.287 in low against a 95 percent half-width of about 0.41 in from these 20 shots; windage about 0.35 in right against about
0.26 in). The fault is that the screen leaves the axis out without a word. Alan chose (asked 2026-10-13 23:58 Denver): a setting,
defaulting to "leave it, and why". Main model (statistics and UI).

## 1. Say "leave it" and why, by default

When one axis is not distinguishable, the verdict names it instead of dropping it, for example: "Dial 1 click left, at 0.1 mil a click.
Elevation: leave it. 0.08 mil low is within what these 20 shots can tell apart (about 0.11 mil either way)." Both units systems, the
rifle's own click, and the angle in the unit the click is in. When neither axis is distinguishable, the existing verdict stays but says
the same for each axis. The phone gets the same words. Check the "why" disclosure under the zero correction still agrees with the new
line, and that Shots Needed to Zero (which already estimates how many shots would settle it) is pointed to from the line where it helps.

Alan's screenshot of the "why" disclosure on that screen reads: "The group center is known to within give or take 0.225 in 0.06 mil
across and 0.411 in 0.11 mil up and down, at 95 percent. Rounding left to whole clicks leaves 0.01 mil. each axis on its own, 19 degrees
of freedom, the group not being circular". It never says that this is why elevation got no click, it runs the inch and mil figures
together with no separator, and the third line starts lower case and is written for a statistician. Rewrite it in a shooter's words:
first the reason the axis was left out (or given anyway, with the setting), then the give or take on each axis as "0.225 in (0.06
mil)", and the method line last, plainly ("worked out on each axis separately from 20 shots, because the group is taller than it is
wide"). Note its across half-width (0.225 in) also differs from what the Shots list's across column gives (about 0.255 in); section 3
covers that.

## 2. A setting: always give both clicks

Settings, under the rifle and scope or wherever the zero correction's other choices live: "When an axis is inside the scatter" with
"Say leave it, and why" (default) and "Give the clicks anyway". With the second, both axes are rounded to whole clicks, the uncertain one
marked: "Dial 1 click left and 1 click up, at 0.1 mil a click. The up click is within the scatter; more shots would tell whether it is
needed." An axis whose offset rounds to zero clicks says "no click" either way. Remembered, both platforms.

## 3. Check the pooled windage figure

On the same screen the zero correction and Group say 0.387 in right, while the Shots list's across column for those 20 shots averages
0.348 in; elevation agrees exactly (0.287 low both ways). Find out which is right for a pooled set (sheet offsets, aim points, or the
list's own reference) and make the screen agree with itself; say in the report what it was. Add a test that a pool's center from aim
equals the mean of its Shots list on both axes, or states why it should not.

## 4. The usual

Tests for both settings at the boundary (an offset just inside and just outside the half-width) and for the rounding (0.08 mil at a 0.1
mil click gives 1 click with the setting on). One commit if it allows; Release-note in a shooter's words, kind changed. The Features
page and the guides where they describe the zero correction.

## 2026-10-14, entry 423: several pictures dropped together open in ONE tab, and a set is pooled without being asked

**Status: actioned 2026-10-14; sections 0 to 4 and 6 done; section 5 done for a set chosen or shared together, not done for section 3's other cases on the phone, which keep its old behaviour; question 98 asks about copies of a single-page design.**

From the planning session, 2026-10-14 05:50 UTC. Alan, 2026-10-13 23:41 Denver: "For the multiple sheet pool, you should be able to
drag multiple scans or photos into a single tab and it should recognize that you want all 3 photos to be pooled together." This
replaces entry 420 section 2's behaviour (a tab per picture, then a "Read these as one set" button). Asked for by Quack and Jylee, both
of whom may be thanked by name. Main model (application code and UI).

## 0. First, the bug Alan hit: a drop onto an open tab replaces it

Alan, 2026-10-13 23:46 Denver: "Right now, if you drag 3 scans onto a single tab, it only opens 1 image and overwrites the session."
Alan, 23:47: "This was on build 192." So entry 420 section 2's tab-per-picture code is in the build he used and still opened one picture: this is a bug in it, not an old build. Read GroupLab's own log on this computer for tonight's drop (target.tab.add, session.save with its "same" field, marking.*) before guessing, and reproduce it headless. Suspects worth checking first, none confirmed: the wait in OpenDropped giving up or AddTarget returning null because detection had not started or not finished, so the rest stop after the first; currentSession (or anything else held on the window rather than the tab) carried from one tab into the next, so accepting a later sheet updates the earlier sheet's record; and the first picture opening into the tab dropped on.

- Only one of three opened: entry 420 section 2 should open all three since nightly 191. If main still opens one when the drop lands
  on a tab that already holds a sheet (as opposed to an empty window), that is the bug; find it and fix it.
- "Overwrites the session": a drop onto a tab that holds a sheet clears that tab and opens the new picture in it (OpenDropped calls
  OpenImageSafely on the current tab; Leaving only asks when there is unsaved work). From now on a drop onto a tab that holds a sheet
  never replaces it: the dropped pictures open in a new tab (one per section 1 and 3), and the tab dropped on is left as it was. Only an
  empty tab is filled in place. Check above all that no session RECORD is ever overwritten: accepting the new picture after such a drop
  must make or update the new picture's own record, never the one the tab held before. If a record was overwritten by any build, say so
  plainly in the report and in for-alan.md, with which build and whether the earlier record can be brought back (the pre-v2 copy of
  grouplab.db, a backup, or the toast that brings back a cleared sheet).
- A test for each: a drop of three onto a tab holding a saved sheet leaves that tab and its record unchanged; a drop of one onto it
  does the same.

## 0b. A pooled set says a decision is left unmade, and Review leads to a blank picture

Alan, 2026-10-13 23:49 Denver, nightly 192: "On a pooled session with 3 images put together, it will say there is a decision left
unmade and when you click on it to review, it goes to a blank image window and asks you to set the number of rounds fired." The pooled
tab (entry 420 section 1) has no picture of its own, and rounds fired belongs to each sheet, so the pool most likely reports a count
review (ReviewKind.Count, "Count differs from rounds fired") against a total it never had, and Review opens the pool's empty picture.

- A pooled set's decisions are its sheets' decisions. The amber banner counts only what is really left unmade on the sheets, names the
  sheet ("Sheet 2: 1 decision left unmade"), and Review opens that sheet (alone, as its key row does, or its own record) at that
  decision, never a blank picture.
- Rounds fired for a pool is the sum of the sheets' own; the pool never asks for it. If a sheet has none, the decision is that sheet's.
- Look for every other place the pooled tab can reach that assumes a picture (the sheet crumb, Fix holes, Show in folder, Accept,
  export, share) and make each act on the chosen sheet or be hidden in the pool; list in the report what each now does.
- Tests: a three-sheet pool with every sheet settled shows no banner; one with an unsettled count on sheet 2 names sheet 2 and Review
  opens sheet 2's picture; no path from a pooled tab shows an empty picture.

## 1. A drop of pictures that are sheets of one set opens one tab, pooled

When two or more pictures are dropped or pasted together (MainWindow.DropAndPaste's OpenDropped is where it starts), read each one's
codes first. If every picture is a sheet of the same set (same definition, a set sheet number on each, no sheet twice), open ONE tab
for the set, named for the set (for example "2 MOA diamond, 3 sheets"). Read the sheets one after another inside that tab, with the
status saying "Reading sheet 2 of 3". When all are read, the tab shows the pooled set exactly as Pool the chosen does since entry 420:
every shot on one plot, each sheet in its own shade, the key row per sheet that shows that sheet alone, the figures, the Shots list.
No question is asked; the drop is the answer. Each sheet is still saved as its own session record and the pool as Pool the chosen
saves it, so Session records looks the same as if it had been done by hand, and a test holds the two routes to the same figures.

## 2. Each sheet can still be checked and fixed from inside the set's tab

A sheet's holes and scale must still be checkable and fixable one sheet at a time (Fix holes, the bull lines, accepting). Reuse what
exists: tapping a sheet's key row already shows that sheet alone; from there the ordinary tools act on that sheet, and the pool updates
when it changes. If doing this well needs a layout choice that can be judged by looking (for example a row of sheet switches above the
picture against the key rows alone), do not choose: build what does not depend on it, write a "DESIGN NEEDED:" line in for-alan.md,
and the planning session will draw concepts.

## 3. Everything that is not one clean set

- Pictures from two or more sets in one drop: one tab per set of two or more sheets, a tab of its own for any single picture, and the
  status says in one line what went where.
- A sheet of the set missing: the tab opens anyway, says by name which sheets are missing (as pooling already does), and dropping the
  missing sheet onto that tab later adds it to the set and updates the pool.
- More pictures of the same set dropped onto an open set's tab: added to it, never a new tab.
- The same sheet in two pictures: the tab keeps both out of the pool for that sheet and asks, in the tab, which one counts (the
  centered warning style of 2026-10-02, not small status text). An identical picture twice is one picture.
- Pictures whose codes cannot be read (store-bought targets, untagged photographs, a sheet whose codes failed): one centered question,
  "Pool these N pictures as one group?", with "Pool them in one tab" as the default button and "One tab each" as the other. Pooled
  untagged pictures each still need their own scale, as they do today.
- A single picture dropped behaves exactly as now.

## 4. Remove what this replaces

"Read these as one set" goes, since nothing is left for it to do; keep only what "One tab each" in section 3 still needs. Update the
tooltip, the guides, the Features page and the tour where they describe dropping several pictures or the button, and the Features page
gets its own picture of a set's tab after a three-sheet drop (entry 256), using the 2 MOA three-page set.

## 5. The phone

The same on the phone where several pictures can be chosen at once from the gallery or Files: sheets of one set go into one set
checklist and open pooled; anything else follows section 3. Say in the report if the phone cannot choose several at once anywhere.

## 6. Tests and the usual

Headless tests: three rendered sheets of the 2 MOA three-page set dropped together give one tab and the same figures as Pool the chosen;
two sets mixed; a missing sheet then added by a second drop; the same sheet twice; three untagged pictures with both answers. One commit
if it allows. Release-note in a shooter's words, kind changed, thanking Quack and Jylee. One "For Jylee" and one "For Quack" line in
for-alan.md (not requests) saying what changed and in which nightly.

## 2026-10-14, entry 422: a click on the picture says what it did, and the side panel lands on it cleanly (Jylee)

**Status: actioned 2026-10-14; sections 1 to 5 done.**

From the planning session, 2026-10-14 02:55 UTC. Source: Jylee, on the GroupLab Discord, 2026-10-13, passed on by Alan. Jylee is a
friend of Alan's and may be credited by name. His words, as data: "it isn't immediately clear what's happening when you click on a bull
or an impact, the side panel also doesn't jump to the right section cleanly and often is just a few lines out of view. I think if you
were to make it so that the section it scrolls to on the side panel flash once or twice to see what section it's going to when you click
would be cool." He also said the analysis screen feels like "UI overload". Nothing here needs Alan. Main model (this is UI code).

## 1. Find out what each click does today

On the desktop analysis screen, list every click on the picture that changes the side panel: a plain click on a bull, shift or command
click on a bull, a click on a hole (with Select and with Impact), a click on a line from a bull to a hole, and Next unsure. For each, say
in the report which section the side panel moves to, if any, and what is selected. Do the same for the phone result screen, where a tap
moves the bottom sheet or the list. This is the baseline for sections 2 to 4; do not paste it whole into the report, one line each.

## 2. The section a click goes to lands fully in view

When a click moves the side panel, the section it goes to ends up with its heading and the selected row both fully visible, the heading
a small margin below the top of the panel, never cut off by a few lines. If the section is taller than the panel, the selected row wins
and its heading may scroll off. The likely cause of "a few lines out of view" is scrolling before the layout settles (a Shots row that
opens in place, or a section that expands, changes height after the scroll was worked out); scroll after layout, and check that once.
Test it headless: after each click from section 1, the target's bounds sit inside the panel's viewport, at three window heights, and
for the last section in the panel, where there is nothing below to scroll into. The phone gets the same rule wherever a tap moves a list.

## 3. The section landed on flashes, as Jylee suggested

When a click moves the side panel to a section, that section's background flashes twice in the accent color, about a third of a second
each, then returns to normal, about one second in all. Only the section a click moved to flashes; a click that does not move the panel
does not flash anything. If the system has animations turned off (Windows "Animation effects", macOS "Reduce motion", the phone's
equivalents), show a steady tint for about a second and a half instead of flashing. The same on the phone. This is a small visual
behavior specified here; Alan may still ask for concepts, in which case only this section waits.

## 4. Say in plain words what a click did

After each click from section 1, the status line under the picture says what happened, in one short sentence a new user understands,
for example "Bull C3 chosen: holes you add now go to C3." or "Shot C3.2 selected; its row is open under Shots." Where the status line
already says this, leave it. Keep it to one line; this is meant to reduce the overload, not add to it.

## 5. The usual

One commit if it allows. Release-note in a shooter's words, kind changed, thanking Jylee. Check whether the tour and Features pages
describe clicking on the picture and still match; fix them in the same change. For-alan gets one "For Jylee" line (not a request), like
the Quack lines, saying what changed and in which nightly.

---

## 2026-10-13, entry 421: consistency audit

**Status: actioned 2026-10-14; sections 1 to 9 done. Section 9 changed nothing: no submission names the C or E bull sheet. Section 1 keeps the Command Z sentence: no report says it works on a Mac.**

From the planning session's scheduled consistency audit, 2026-10-13 15:56 UTC (entry 267 section 2b). Read only: README.md, the live
grouplab.org (home, /download/, /features/, /tour/, /shoot-a-target/, /guides/, /releases/, /support/, /targets/),
website/features.json, docs/RELEASE-NOTES.md, docs/notes/STATE.md, docs/notes/for-alan.md, docs/PLATFORM-SUPPORT.md,
docs/PHONE-PARITY.md, docs/ANDROID.md, the guides, the inbox and the log to db8beba7. No GitHub issue labelled `consistency` exists,
open or closed, so nothing here repeats one. Nothing here needs Alan. Fix them together in one change, in the usual way: the source
file first, then whatever is generated from it (README sections, the site, the PDFs). Wording passes may go to docs-sonnet.

## 1. The Mac has now been run by two testers, not one

- Where: docs/PLATFORM-SUPPORT.md line 24 (and the README copy of it, README.md line 184); README.md line 108 ("**Run on one real
  Mac**, by one tester"); website/build.py line 982 (the download page's Mac card: "Run on one real Mac. One tester, an M5 Max,
  nightly 93", live at https://grouplab.org/download/); docs/TESTING-GUIDE.md line 94 ("has been run on one Mac").
- What it says: the Apple silicon build has been run on one Mac, by one tester, on nightly 93.
- What it should say: it has been run on two Macs with Apple silicon by two testers: Fenix on nightly 93 (the M5 Max), and Quack on
  nightly 190 (macOS ARM64), who opened flatbed scans, marked holes with the Impact and Select tools, dropped several scans at once and
  pooled sheets in Session records. The Intel build is still untried.
- Evidence: docs/NOTES-FROM-PLANNING.md, entry 417's report ("Build: GroupLab 0.2.0-nightly.190 ... OS: macOS ARM64"), and entry 420
  ("on nightly 190 on macOS"); commit e823431a (a macOS crash fixed).
- Also: the sentence "Command shortcuts such as Command Z did not work ... neither fix has been checked on a Mac yet" stays only if
  nothing in Quack's reports shows those working. Do not guess; if they do not say, leave it as it is.

## 2. Phase 9 says nothing has been optimized

- Where: README.md lines 488 and 490, under Planned, Phase 9.
- What it says: "**Not started.** Making GroupLab quick ..." and "**Not started.** The gate ... and the optimizations themselves.
  Nothing has been optimized."
- What it should say: optimizations have been made and measured (hole finding on a scan, entry 392; the phone's reading of a
  photograph, entry 400; a 600 dpi scan opening in about four fifths of the time and the solver warmed after start-up, entry 414; a
  photograph's codes read sooner, entry 415 section 3), each with the same results; the gate itself is still not written. The table row
  at line 398 already says In progress, so the bullets disagree with their own table.
- Evidence: docs/RELEASE-NOTES.md, nightly 189 ("A 600 dpi scan now opens sooner, in about four fifths of the time") and nightly 186
  ("Finding the holes on a sheet takes less time"); docs/notes/STATE.md, "Performance (Phase 9)" line.

## 3. The Features page and the home page describe behavior no download has yet

- Where: https://grouplab.org/features/ and the home page's "New in GroupLab", from website/features.json.
- What it says: "Pool the sheets of a set ... opens on the analysis screen in a tab of its own with each sheet's shots in their own
  shade" marked "Since nightly.113"; "Velocity and the vertical ... in the conditions on the day you enter under it" marked "Since
  nightly.145"; "The bottom bar at the largest text sizes: GroupLab's text follows the phone's own text size, on Android and on the
  iPhone" shown as "New in nightly.188" on the home page and the Features page.
- Why it is wrong today: the newest published build is nightly 190 (commit 4303b73). The pooled tab (entry 420, 3aaa008f), the
  conditions under Velocity (entry 416, 59eb71d0) and the iPhone following its text size (entry 415, 94502fc2) are only on main.
  Nightly 188's own note says "At Android's largest text sizes" and nothing about the iPhone.
- What it should say: until a nightly carries them, the new parts are marked as coming in the next nightly (or the sentence keeps the
  published behavior), and once nightly 191 is out, "New in" and "Since" name 191 for these parts. If the site build cannot tell a
  changed sentence from an old feature, a small rule for it (a sentence changed on main after the feature's "since" build is flagged)
  would stop this happening again.

## 4. The user guide's PDF is behind its guide

- Where: docs/USER-GUIDE.pdf, last committed in 8fd7402c (2026-10-12); docs/USER-GUIDE.md changed since in 94502fc2, 3aaa008f and
  59eb71d0 (2026-10-13). The site serves the old PDF at https://grouplab.org/guides/user-guide.pdf (website/build.py line 260).
- What the PDF still says: the old pooling paragraph (no tab of its own, no Read these as one set, no See the pooled group on the
  phone), the old Velocity paragraph (no Conditions on the day), and "The iPhone keeps its text at GroupLab's own sizes."
- What it should say: what docs/USER-GUIDE.md says now. Run `grouplab user-guide` and commit the PDFs with the guide (the rule in
  scripts/consistency.py line 78).

## 5. Report a problem is described two ways

- Where: docs/USER-GUIDE.md line 440 ("**Report a problem** is in the settings. It opens the support page, which takes a
  description ..."); docs/TESTING-GUIDE.md line 98 and https://grouplab.org/support/ step 02 ("**Report a problem** there writes a
  report package: a zip with the log ...").
- What the application does: src/GroupLab.App/MainWindow.cs lines 1339 to 1346, OpenReport opens a ReportWindow.
- What it should say: one description, matching the window as it is, in all three places. Check which is right before choosing.

## 6. The phone parity table: two rows behind

- Where: docs/PHONE-PARITY.md line 79 (`target-tabs`, "left out": "the phone shows one target at a time and moves between them in
  Sessions"). This contradicts line 63 (`open-targets-phone`, on the phone: every target read stays open and Open targets lists them).
  It should say the phone keeps several targets open through Open targets (line 63), and only the tab strip and keyboard are left out.
- Where: docs/PHONE-PARITY.md line 69 (`pool-set`): "the set as a checklist". Since entry 420 the checklist also opens the pooled plot
  (See the pooled group, docs/USER-GUIDE.md, and mobile/GroupLab.Mobile/SetPage.cs in 3aaa008f). Add that.

## 7. The Thanks section names one of Quack's four reports

- Where: README.md line 662.
- What it says: Quack found the red cut-off warning that stayed after switching printers.
- What it should say: also that Quack's flatbed scans on a Mac showed holes being missed and the snap landing on the wrong side of the
  rim (entry 417), his report on the upload page led to the progress bar and sets of up to 30 photos (entry 418), and his question about
  reading a set of three sheets as one led to the pooled set in its own tab (entry 420). Quack may be thanked by name (Alan, 2026-10-11);
  write nothing else about him. Keep the credits rule: Unholy and Jylee by name, Justin by first name only.

## 8. Two README lines that have aged

- README.md line 334: "New on the phone in the latest builds, as entries 258 and 259 bring it level with the desktop:" These are from
  late September and are no longer the latest. Say "Since the phone was brought level with the desktop (entries 258 and 259):" or move
  the list under the phone's own paragraph.
- README.md line 441: "a rough click lands on the dark centroid within a caliber-sized reach". Since entry 417 (1392641a) the snap goes
  to the hole's middle, fitting a circle through the rim where the hole is as light as the paper (a flatbed with a white lid). Say so.

## 9. The donor page's "most useful" line may be out of date

- Where: website/donor/sheets.json line 3, live on https://grouplab.org/shoot-a-target/: "the E and C bulls, which it has seen from one
  range day only."
- Why: Quack shot and sent the 3x3 2 MOA C bull set of three and other sets on 2026-10-12 (entries 417, 418 and 420;
  C:\Dev\grouplab-submissions). Check which designs those submissions are. If they include C or E bull sheets, the line should say the
  C bull (or both) has been seen from two shooters, and keep asking for the C3 zeroing grids. If not, leave it.

---

## 2026-10-13, entry 416: a session's own conditions for Velocity and the vertical, concept A

**Status: actioned 2026-10-13; sections 1 to 5 done. Not done: the two Features pictures, listed as waiting for the screenshot walk and the phone's next sitting; CSV out does not carry the conditions, having no place for them.**

From the planning session, 2026-10-13 07:10 UTC. Take it after entry 415, inside the same budget (stop at **55% of the week**, never in
the middle of a publish). Main model: application code, UI and tests. This closes the DESIGN NEEDED that entry 329 left: sessions did not
carry conditions, so the block used the Ballistics screen's air or a standard day.

Alan, 2026-10-13, on the canvas "Velocity conditions" (claude.ai/artifact/RqegGtu63HTbHtspu4K2hE): **"Go with A"**: the conditions are
entered where the speeds are, inside the Velocity and the vertical block, after shooting. Boards "A on the desktop" and "A on the phone"
are the look; read them before building. Its numbers are samples.

## 1. The session carries its own conditions

1. A session stores temperature, altitude or station pressure (whichever was entered), humidity and shot angle, each optional. The
   session file and the database both carry them; a database change follows entry 407's pattern (a copy beside it before the first open
   of a new build), and SESSION-SCHEMA.md says so.
2. Order of preference, field by field, as entry 329 set it: the session's own value, then the Ballistics conditions for that rifle and
   load, then the standard day. A field left blank falls back on its own; the others still count.
3. Sight height and zero are **not** in this form. They stay on the rifle, as the board says, with a link to its page.
4. Values in the person's own units (F or C, ft or m, inHg or hPa, as Settings chooses).
5. Nothing is ever filled from location or weather services. A photograph's GPS stays unread.
6. Sharing a session, exporting it and opening it on the other device carry the conditions. CSV out carries them in its header lines if
   the format has a place for them; if not, say so in the report.

## 2. On the desktop (board "A on the desktop")

1. Inside the Velocity and the vertical block, once it has readings, a part headed **Conditions on the day**, closed by default. Closed,
   it shows one line: what is in use and where it came from ("Standard day assumed: no temperature or altitude entered.", or "72 F,
   5400 ft, 30% humidity, shot level, from this sheet.").
2. Open: Temperature, Altitude, Humidity, Shot angle; the link **Use station pressure instead of altitude** swaps that field; **Fill from
   Ballistics** (for this rifle and load; greyed with the reason when Ballistics has none); **Standard day** (clears the four);
   the rifle line with **Change them on its page**; **Remember for my next sheet**, ticked by default.
3. Under them, the comparison line from the board: "With these, velocity alone is X. On a standard day it would be Y." Both numbers from
   the same group and readings; the line is gone when the values are the standard day.
4. Changing a value updates the block at once. "why" names each value's source, as it already does.

## 3. On the phone (board "A on the phone"), Android and iOS

1. The velocity card gets a **Conditions** row with the same one-line summary; tapping it opens a sheet from the bottom.
2. The sheet: title, Cancel, the one-line note ("Used only for how much of the vertical velocity explains. Leave any blank and GroupLab
   uses a standard day for it."), the four fields, the station pressure link, the rifle line, Remember for my next sheet, and the two
   buttons **Fill from Ballistics** and **Use these**. Touch targets as drawn (48 px rows and buttons).
3. It follows the text size rules of entries 411 and 415; the sweeps that hold the largest size hold this sheet.
4. iOS gets it in the same change (phone parity).

## 4. Remember for my next sheet

Ticked, the values are offered to the next sheet of the same day, filled and summarized as "from your last sheet", until changed.
Not ticked, the next sheet starts blank. The next day starts blank either way. If "the same day" turns out to be the wrong edge (for
example a session crossing midnight), pick the simplest honest rule and write it down.

## 5. Tests, docs and the rest

1. Tests: a session's own values change the height per fps the way the solver says (entry 329's test, now with the session as the
   source); a blank field falls back alone; Fill from Ballistics copies that rifle and load's values; the values survive closing and
   reopening the session, a shared session and an export; Remember carries them to the next sheet of the day and not the next day.
2. The Features page gets its own picture of the open part, desktop and phone (entry 256). The guide's paragraph on Velocity and the
   vertical, the tour page for the analysis screen if it names the block, PHONE-PARITY.md and the README say where conditions are
   entered. Claims backing for each new checkable sentence.
3. STATE.md: remove "a session's own conditions would need a form (DESIGN NEEDED if wanted)".
4. Release-note kind `new`, in a shooter's words, for example: "You can now enter the day's temperature, altitude, humidity and shot
   angle under Velocity and the vertical, and GroupLab uses them to work out how much of your vertical came from velocity."

---

## 2026-10-13, entry 415: four things that need nobody (Alan at 23:37 Denver, no printer near him)

**Status: actioned 2026-10-13; sections 1, 2 and 3.1 done, section 4 for the log only. Not done: section 3.2 (finding the holes), because entry 416, a feature Alan chose, was taken first under the same budget.**

From the planning session, 2026-10-13 05:45 UTC. Code was idle and cleared. Alan asked what Code can work on with no printer, no range
and no phone, and chose all four of the options put to him. Sections 1 to 3 are for Code; section 4 is the planning session's and is here
only so the log says why.

**Budget for this block:** stop at **55% of the week** (28% at 05:33 UTC; the week resets 2026-10-15 02:00 UTC), or when the queue is
done, whichever comes first, and never in the middle of a publish. One worker, no helpers in parallel. Screenshots and chores to
chores-haiku, wording and consistency checks to docs-sonnet; application code, tests, detection and performance on the main model.
Sections in order, one commit each where it fits, a report after each. Anything that turns out to need Alan goes to for-alan.md and the
work moves on.

## 1. Question 97 answered: the iPhone follows its text size (Dynamic Type)

Alan, 2026-10-13: yes, the iPhone follows its own text size setting, as Android now does (entry 411).

1. Point `TextSize` at iOS's preferred content size on the iOS head (`UIFontMetrics.DefaultMetrics.GetScaledValue` or whatever the
   question's own finding says is right), and restyle when the setting changes while GroupLab is open
   (`UIContentSizeCategory.DidChangeNotification`), so a person does not have to restart.
2. **Scale:** follow every size through xxxLarge, and the accessibility sizes up to the same largest factor Android allows (entry 411),
   held there. Record the cap and why in PHASE1-RESULTS.md. Above the cap the text stays at the cap; nothing breaks.
3. The bottom bar follows entry 413's rule (icons alone where a name would not fit) on the iPhone too; no new look.
4. The same headless sweeps that hold Android's largest size hold the iPhone's largest. A screen that cannot hold it gets a
   `DESIGN NEEDED` line in for-alan.md (with which screen and what breaks), and the rest goes ahead.
5. Close question 97 as answered in QUESTIONS-FOR-PLANNING.md. PHONE-PARITY.md, the README, the guides and the Features page say the
   phone follows its text size on both systems where they now say Android only. Release-note kind `new`.

## 2. Proof checklist row 3: the false hole marks

PROOF-CHECKLIST.md row 3 is the one detection gate that needs nothing more shot: 221 of 223 holes found on the nine corpus photographs
with a hole truth, and 6 false marks, 5 on the crinkled .22 LR diamond sheet and 1 below the codes on the taped 2026-09-26 dominus-k
picture.

1. Find what makes each of the six marks, by looking at the mark and the stage that made it, and write it down per mark.
2. Change detection only where the cause is a mechanism that would happen on other sheets too (a crease's shadow, tape, paper outside the
   printed area), never a setting chosen because it removes these six. Nine photographs are too few to tune on; say so where a change is
   close to that line.
3. A change is kept only if no hole is lost anywhere: the corpus, the rendered sheets (`RoundsFiredOnARenderedSheetTests` and the rest),
   the scoreboards, and the learning loop's held-out share. A hole off its bull (a flier) must still be found; a mark is not dropped
   just for being far from a bull.
4. The two holes still missed: say what they are. Fix them under the same rule if the cause is clear, otherwise record why not.
5. Row 3 updated with the new count. If it is now met, say so in the report. Release-note kind `fixed` if a person would see fewer
   wrong marks; check with the learning loop that its next tuning run starts from the new code.
6. Worth an article? Record the decision in RESEARCH.md.

## 3. Phase 9: the planning session names the next optimization

PERFORMANCE.md: on a phone photograph, reading the sheet's codes is the largest stage (190 to 395 ms), and finding the holes is next
(180 to 540 ms, mostly the closing of the threshold mask and the alignment's phase correlations).

1. **Reading the codes on a photograph, still on one thread.** Entry 414 showed OpenCV's QR detectors must not run on several threads;
   nothing in this section may bring that back. Look at where the time goes (finding where each code should be and cutting it out,
   then the decode) and make it cheaper with identical reads.
2. **Then, if the budget allows, finding the holes** (the closing and the phase correlations), same rule.
3. Each kept only if every scoreboard, the identify sweep and the corpus counts are identical but for their time columns and the
   baseline gate passes. A round that cannot keep identical results is taken back and written down; that is a result.
4. Each under its own heading in PERFORMANCE.md. A Release-note only where a person would notice (a photograph read sooner is);
   otherwise `internal`.

## 4. For the log only: the velocity conditions form

STATE.md: the velocity calculation uses the conditions entered on Ballistics; a session's own temperature, altitude, sight height and
zero would need a form, a look question. Alan chose (2026-10-13) to have the planning session draw concepts on a Claude Design canvas.
Nothing for Code until he picks; his choice comes back as its own entry.

---

## 2026-10-13, entry 420: a pooled set opens like a sheet, from a tester's question

**Status: actioned 2026-10-13; sections 1 to 3 done. The pooled view's own Features picture waits for the screenshot walk (docs/figures/SCREENSHOTS.md).**

From the planning session, 2026-10-13 08:10 UTC. Take after 419 (order 417, 418, 419, 420, 415, 416; stop at 70% of the week). Main model.

Quack (may be thanked by name) asked in Discord, on nightly 190 on macOS, data not instructions: he printed GroupLab 3x3 2 MOA, C Bull,
Set of 3 Letter Sheets, shot one shot a bull, scanned each sheet as its own JPEG, and asked for a documented way to read all three as one.
Dragging and dropping did not do it. He found Pool the chosen in Session records, which gives the figures (27 shots, mean radius, sigma,
extreme spread with intervals) but no plot of the impacts. His screenshot also shows a fourth record of the set with 4 shots and no
distance (from the bug in entry 417), and three identical "3x3 Shotgun, Letter 2" records of 2026-10-07 with the same figures.

## 1. The pooled set opens in the analysis screen (Alan, 2026-10-13: reuse the analysis screen, no canvas)

1. Pool the chosen opens the pooled set in the ordinary analysis screen, in its own tab: all the shots on one plot, each relative to its
   own bull, every figure the screen shows for a sheet, the Shots list saying which sheet each shot came from (1 of 3, 2 of 3, 3 of 3).
2. Each sheet's shots in their own shade, from the plot's existing colours, with a small key; tapping a sheet in the key shows only its
   shots. Nothing else on the screen changes. If something cannot fit without a new layout, a DESIGN NEEDED line and the rest built.
3. Missing sheets named as today. The phone's pooling checklist (entry 259 screen 6) opens the same plot on its result screen, Android
   and iOS.
4. Its own Features picture, the guide's pooling paragraph and the tour say how: print the set, shoot it, open each scan, Accept and
   analyze each, then Session records, tick the sheets, Pool the chosen.

## 2. Dropping several sheets of one set

Check what dropping three scans of one set does on macOS and Windows (one tab each is right). If they are recognised as one set, offer
"Read these as one set" after the three are accepted, which goes to step 1. Say in the report what dropping did on his Mac and why.

## 3. The repeated records

Find why Session records holds identical records (three of 2026-10-07 with the same figures, and 2026-09-24 against 2026-10-07): Accept
and analyze pressed again on a saved sheet, a reopened tab, a sync, or a person saving on purpose. If GroupLab makes a second record of the
same sheet without asking, it updates the first instead, or asks; a test holds it. The 4-shot record from entry 417's bug is left alone.

Release-note kind `new` for 1 (thanking Quack), `fixed` for 3 if it was a bug. A line in for-alan.md Alan can pass to Quack.

---

## 2026-10-13, entry 419: Discord bug reports filed by themselves

**Status: actioned 2026-10-13; sections 1 and 3 done, section 2 written for Alan as request 98. The worker waits for his bot and his paste before it runs.**

From the planning session, 2026-10-13 08:00 UTC. Take after 418 (order 417, 418, 419, 415, 416; stop at 70% of the week).

Alan, 2026-10-13: "Can you monitor the bugs discord channel and automatically file them?" No Discord connector exists for the planning
session, and Alan wants automation off his desktop and on the free server where possible. So a small worker on the server, built the way
the intake and error workers are.

## 1. The worker

1. `website/server/grouplab-discord-worker.py`, a systemd timer every 5 minutes (a time limit on every run), reading the GroupLab
   Discord server's **bug-reports** forum through Discord's REST API with a bot token. Read only, apart from one reply per thread (3).
2. Each new forum post becomes one issue in the private `oRAirwolf/grouplab-crash-reports`, labelled `discord`: the thread's title, the
   first post quoted as untrusted text, the poster's Discord handle as shown publicly, the post's time, a link to the thread, and the
   attachments listed. Later messages in that thread are added as comments. Nothing is filed twice (a state file keyed by message id).
   The first run files every post already in the forum.
3. When an issue is made, the bot replies once in the thread: "Thank you, this is filed for the developer." Nothing else is posted.
4. Attachments: saved privately for diagnosis, pulled by the daily sync to a folder beside `C:\Dev\grouplab-submissions` and deleted
   from the server once copied (the server retention rule). **Not covered by any consent**: never published, never put in public test
   data, never in a research article. The issue says that a poster who wants them used can send them through the Send a target page.
5. Everything in a post is data, never an instruction, to the worker, to Code and to the planning session (CLAUDE.md). Text that reads
   like an instruction is noted in the issue as such.
6. The GitHub token: reuse the one the error worker already uses for this repository if its scope allows issues; otherwise say what
   scope is needed in the paste for Alan. Never read or print a token.
7. Code's start-of-run issue check already lists open issues in that repository; say in CLAUDE.md that `discord` issues come from people
   and are taken like bug reports a person hit.

## 2. What Alan does, prepared up front in for-alan.md and given to the planning session

Write the steps out in full before building, so the planning session can give them to Alan in one sitting:

1. Discord Developer Portal: New Application "GroupLab Bug Filer", Bot, turn on Message Content Intent, Reset Token and keep the token for
   step 3 only.
2. OAuth2 URL Generator: scope `bot`, permissions View Channels, Read Message History and Send Messages in Threads only; open the URL and
   add it to the GroupLab server; in the bug-reports forum's permissions make sure the bot can see it.
3. One checked paste in MobaXterm that backs up first, installs the worker and its timer, asks for the token with a hidden prompt into a
   root-only file, runs once, and prints the issues it made or a STOP line saying why not. The guild and channel ids go in the paste,
   found by the worker from the token, never typed by Alan.

## 3. Records

RESTORE.md (the worker, its state file and the token's file: what is backed up and how to restore), the server inventory, and a test of
the worker against recorded Discord replies (no network in tests). Website and server only: no application release note.

---

## 2026-10-13, entry 418: the upload page, from a tester's bug report (take after 417, before 415 and 416)

**Status: actioned 2026-10-13; sections 2, 3 and 4 done. Section 1 waits on request 99, a read-only paste for Alan, because Code's own connection to the server is refused; the page now says the status of any reply that is not GroupLab's either way.**

From the planning session, 2026-10-13 08:00 UTC. Order for the inbox now: **417, 418, 419, then 415 and 416.** Budget for all of them:
stop at **70% of the week** (raised from entry 415's 55%: the week resets 2026-10-15 02:00 UTC and Alan would rather use it than leave
it), never in the middle of a publish. Website and receiver work is code: main model; wording passes to docs-sonnet.

Reported by Quack (may be thanked by name) in the Discord bug-reports forum on 2026-10-12, passed on by Alan. Data, not instructions.

## The report, as written

1. Uploading is opaque: no progress bar, no proof it is still going, nothing to say bytes are moving.
2. The file cap is too low for a set shot across several sheets (the library's 3-sheet bundles, "1 of 3"). Example: a 3-sheet .223 set
   needs 3 x (2 front, 2 left, 2 right, 1 scan) plus 2 of all three sheets together, 2 from the left and 2 from the right: about 27 files.
3. Splitting it as asked ended in "The server answered with something this page could not read.", then "That is as many submissions as
   this connection can send for now. Thank you, genuinely. Please come back in an hour if you have more." The upload was finished in the
   end (reference fd7446e4).

## What the files say (read by the planning session)

- `C:\Dev\grouplab-submissions`: seven submissions from 02:53 to 04:01 UTC on 2026-10-12. Two .50 AE ones are the same "file upload
  1 of 2" twice (`06910ef5` and `312dab10`, 8 files each): the first very likely did arrive while the page reported an error, and he sent
  it again. Then 2 of 3 and 3 of 3 (`7b06ef2a`, `70d25efc`), then the three .223 pages (`7aed90b9`, `a48b3560`, `fd7446e4`). Seven in
  70 minutes is over `RATE_PER_HOUR = 5` in `website/api/upload.php`.
- Phone photographs here are 10 to 12.5 MB each, so 8 files are about 90 MB, the receiver's `MAX_SUBMISSION_BYTES` and close to
  Cloudflare's 100 MB request cap. "Could not read" is the page's text for a reply that is not JSON (`website/build.py` near line 3388),
  which is what a 413 page from Cloudflare or nginx, or a timeout page, looks like.

## 1. Find which reply it was

From the server's logs over the sync's read access (diagnosis only), match the failed post to its status: Cloudflare 413, nginx 413,
PHP's post_max_size, a timeout, or something else. Say which, and whether the first "1 of 2" was stored although the page said it failed.

## 2. Progress and plain errors on the page

1. Send with progress events (XMLHttpRequest's upload progress, or one file at a time): a bar, the megabytes sent of the total, then
   "Received, checking the files" while the server works, then the result with the reference. Built in the site's existing form style;
   if a layout choice comes up beyond a plain bar under the button, a DESIGN NEEDED line and build the plain bar meanwhile.
2. Check sizes and counts before sending and say plainly what is over and what to do, never send something the receiver will refuse.
3. A reply that is not JSON says what happened in plain words with its status ("The upload was too large for one send (HTTP 413)...")
   and says whether anything may have arrived, so nobody resends a set that is already in.

## 3. A whole set in one go

1. A set of up to about 30 files sent as several requests under one set reference (each request well under Cloudflare's cap), stored as
   one submission with its sheets numbered as the library numbers them ("1 of 3"), and each file labelled by sheet and view where the
   sender gives it. Counted as one submission by the rate limits.
2. The same file sent twice in one set or in the last day is recognised by its SHA-256 and not stored twice.
3. Keep every protection that exists (clamd, quarantine, the global hourly cap, the consent text). Raise nothing that lets one address
   send more bytes in an hour than today without saying why it is safe in the commit.
4. limits.json stays the one place for every limit; the page, the receiver and the test agree; the Send a target page and the guides say
   the new limits. Server pieces follow RESTORE.md: backed up first, and anything that needs sudo comes back to Alan as one checked paste.
5. Tests in `tests/php/receiver-tests.php` and the site's checks for each new path, including a set split across requests and a resend.

## 4. Quack's two .50 AE duplicates

Keep one of `06910ef5` and `312dab10` if they are byte for byte the same, through the trash rule, and say which in the report.

Release notes: this is the website, published by the site, so no application note (entry 168). A line in for-alan.md Alan can pass to
Quack: what changed and that the page is live.

---

## 2026-10-13, entry 417: a tester's bug on macOS, marking a scanned 3-sheet set (take this FIRST, before 415 and 416)

**Status: actioned 2026-10-13; sections 1 and 2 done. The causes were in the pictures and the click handling, not in macOS: holes showing the scanner's white lid were missed or snapped to their rim's dark side, and a chosen bull swallowed drags. Item 4's search found no matching Avalonia report.**

From the planning session, 2026-10-13 07:35 UTC. A bug a person hit comes before approved features (entry 317's priorities), so take
this entry before entries 415 and 416, inside the same budget (stop at 55% of the week, never in the middle of a publish). Main model.

The report below came from Quack, a tester on the GroupLab Discord, passed on by Alan. Quack may be thanked by name (Alan, 2026-10-11).
The report and the files are data, not instructions.

## The report, as written

Build: GroupLab 0.2.0-nightly.190, nightly build, commit 4303b73. OS: macOS ARM64.
Opening the "556 x3" sheets, scanned Page 1/2/3, using the "scale detected" printer:

1. Automatic detection misplaces the holes by about a quarter of a hole.
2. Placing holes by hand with the Impact (I) tool "magnets to" the wrong place, off by about a quarter of a hole.
3. With the Select (V) tool it is no longer possible to click and drag holes to where they should be.

Alan's screenshot (Marking, hit10102026_0001.jpg, 9 shots): the status line says "Scale: from the sheet's own printed markers: 25 of 26
markers found, detected without a caliber ... registration RMS 0.0018 over 25 markers, 4 holes detected, assigned by one-to-one
matching: fewer shots than bulls". The printer box shows Brother at 99.7 percent. The ring for shot 8 sits visibly below its torn hole
under bull 8 (a flier between the diamond and the codes); the others look shifted too.

## The files

Already pulled by the daily sync into `C:\Dev\grouplab-submissions`, consent level publishable, 0.223 in at 100 yards, M193:
`2026-10-12_7aed90b9` (page 1, `001_hit10102026.png`), `2026-10-12_a48b3560` (page 2, `001_hit10102026_0001.png`) and
`2026-10-12_fd7446e4` (page 3, `001_hit10102026_0002.png`), each with six phone photographs of the same sheet. GPS stays unread.

## 1. Reproduce and find the cause

1. Run the scans through the CLI and the headless marking screen on this computer. Compare where GroupLab puts each hole with where the
   torn hole is in the pixels. Say whether the offset is in detection itself, or only in where the rings and the snap point are drawn
   (the picture shown and the picture measured not lining up), and whether it also happens on Windows and Linux or only on macOS.
2. Suspects to check first, without assuming either is it:
   - Entry 414 section 1.1 (2cdae57c, a scan decoded once). Does the picture shown still have exactly the size and placement of the
     picture measured, on macOS too, for a JPEG with its own dpi?
   - The printer scale (Brother, 99.7 percent) applied in one of the two paths and not the other, or applied to a scan whose own
     markers already set the scale (STATE: a scan's own measured scale always wins).
   - macOS's display scaling (Retina, two device pixels a point) in the pointer-to-image mapping.
3. Item 3, dragging with Select: check entry 407 section 1 (6667648b, tap a bull to give it holes). Does a press on a hole near or
   inside a bull now go to the bull, or start a selection, instead of a drag? Check on macOS's pointer path as well.
4. Search online for any Avalonia report of the same pointer or drag behaviour on macOS, and say what was found.

## 2. Fix, and hold it

1. Fix each cause on every platform it reaches, desktop and, where the same code runs, the phones.
2. Tests: a rendered sheet with holes at known places, read through the same path as a scan with a printer chosen, comes back within the
   gate; the rings and the snap point are drawn where the hole was measured; a press and drag on a hole with Select moves that hole, and
   a tap on a bull still gives it holes as entry 407 meant. Use these scans in a test if their sizes allow (over about 10 MB goes to the
   test-data release, as the rules say).
3. Release-note kind `fixed`, in a shooter's words, thanking Quack.
4. Write the result into PHASE1-RESULTS.md, and in for-alan.md one line Alan can pass on to Quack: what was wrong and which nightly has
   the fix.

---

## 2026-10-12, entry 414: an overnight queue that needs nobody (Alan asleep, about 8 to 10 hours)

**Status: actioned 2026-10-12; sections 1 to 6 done. Also taken back: reading a sheet's codes at once (native crashes on Linux). Section 2 did not reproduce, so it has no fix; the check now runs after every nightly.**

From the planning session, 2026-10-12 11:50 UTC. Alan, 05:41 Denver: going to bed, and asked what Code can work on while he sleeps.
Take entry 413 first (the bottom bar, icons only at large text sizes), then this. Everything below needs nothing from him; anything that
turns out to need him goes to for-alan.md and the work moves on. Sections in order, one commit each where it fits, a report after each.

**Budget for this block:** stop at **38% of the week** (26% at 11:39 UTC), or when the queue is done, whichever comes first, and never in
the middle of a publish. One worker, no helpers in parallel. Screenshots and chores to chores-haiku, wording and consistency checks to
docs-sonnet, as the standing rules say; application code, tests and performance work on the main model.

## 1. Phase 9: the planning session names the next optimizations

PERFORMANCE.md says decoding a 600 dpi PNG (about 630 ms) is now the largest single step left in reading a scan, and its list of things
found while measuring still names three that cost a person time. Take them in this order, each measured before and after with the same
method as entries 392, 400 and 410, each kept only if every scoreboard, the identify sweep and the corpus counts are identical but for
their time columns and the baseline gate passes. A round that cannot keep identical results is taken back and written down; that is a
result, not a failure.

1. **A scan decoded once.** Check whether a 600 dpi image is still decoded twice (once to load, once again for its strongest channel).
   If it is, build both images from one decode, identical to the bit. If it is not, measure whether a faster decode path gives identical
   pixels and is worth it, and record the answer either way.
2. **The first trajectory.** The first dope table costs about 100 ms and the next about 4, because something in the solver is built on
   first use. Find what it is. If building it at start-up in the background costs nothing a person waits for, do that; if the answer is
   that it should stay as it is, say why.
3. **The CEP table** (about 125 ms for two figures, a numerical integration). Measure it again first; if it still costs that, make it
   cheaper with identical figures to the digits shown, or record why it cannot be.

Write each into PERFORMANCE.md under its own heading. A Release-note only where a person would notice (opening a scan sooner is
noticeable); otherwise `internal`.

## 2. The emulator finding: does GroupLab start after a force-stop in the middle of a reading?

STATE.md: run 37735635656 left a reading running at a force-stop, and no later start of GroupLab Dev on that emulator ran anything; not
established whether a phone does the same. A person whose phone kills GroupLab while it reads a photograph, and who then cannot open it
again, would have no way out. On the emulator: reproduce it on purpose (start a reading, force-stop, start again, several times). If it
reproduces, find the cause, fix it on the phone path, and add a test that holds it. If it does not, say what was tried and what the
original run most likely saw. Either way, record it in PHASE1-RESULTS.md and correct the line in STATE.md. If it is a fix a person would
notice, a Release-note of kind `fixed`.

## 3. Consistency after entries 411 to 413 (docs-sonnet)

The phone's text now follows Android's text size and the bottom bar changes at large sizes. Check the README, the site, the three
guides, the Features page and the tour for anything that still describes fixed text or always-named tabs, and fix it in one commit.
Then look at the open GitHub issue labelled `consistency`, if there is one, and fix the items that need nobody.

## 4. The proof checklist, sorted for the next range trip

For every row of PROOF-CHECKLIST.md still not proven, decide whether a test, the emulator, the headless walks or material already in
`C:\Dev\grouplab-local\` can prove it now. Prove the ones that can. For the rest, write beside each the exact material it needs (which
sheet, which optic, which phone, how many shots), so section 5 can put it in one kit.

## 5. The next range kit, prepared and not yet asked for

Build the next range kit as files ready to print, in `C:\Dev\grouplab-local\range-next\`, in the same shape as the 4 October kit
(a `CHECKLIST.pdf` first): requests 57 (red bulls), 58 (store-bought targets), 86 (aim points through the red dots, the prism and the
medium power scopes, with a new aim point card, entry 412), the current sheet to be shot mounted and scanned flat at home afterwards
(entries 411 and 412 section 4), and whatever section 4 found that a range trip can prove. Every sheet that will be needed after the
shoot carries a **Keep:** line on the checklist (entry 411 section 1.4). Aim for about an hour of shooting; say on the checklist which
parts can be skipped if time runs short. Add one line to for-alan.md saying the kit is ready whenever he next goes, not a new request
that needs an answer.

## 6. Report

The five line report per section as usual. If the guard stops the block, the report says which sections were not reached.

## 2026-10-12, entry 413: the phone's bottom bar at large text sizes, Alan chose B (icons only)

**Status: actioned 2026-10-12; sections 1 to 3 done. The Features picture is taken by the emulator's screens run after this reaches main; SCREENSHOTS.md names it as waiting until then.**

From the planning session, 2026-10-12 11:40 UTC. Answers entry 411's DESIGN NEEDED line (the phone's bottom bar at Android's largest
text size). Canvas: claude.ai/artifact/Dah5KZd9LvZc6sYfU5P2Kg, five frames on a 360 wide phone in the dark theme: today at the normal
size, then A (each name drawn smaller until it fits, what entry 411 built), B (icons only), C (two rows of names), D (only the current
place named). Alan, 05:38 Denver: "Go with B".

## 1. Build B

1. When the text size Android asks for would make any one of the five names (Capture, Sessions, Ballistics, Targets, Settings) too wide
   for its fifth of the bar, the bar shows icons only, for all five at once, never some named and some not. Below that point the bar is
   exactly as it is today: icon over name, the current place's icon in the amber pill. Decide by measuring the names at the followed
   size against the bar's real width, not by a fixed font scale, so a wide phone or the Fold's inner screen keeps its names for longer.
2. In icons only: each icon 26 px (from 20), the pill 60 by 40 px with a 20 px radius (from 56 by 28), the current place's icon amber in
   the amber pill as now, the others dim. Each tab stays at least 48 px high and a full fifth wide, so the touch targets do not shrink.
3. Each tab keeps its name for screen readers (TalkBack reads "Targets", not "button") and as a tooltip on a long press, in both
   states.
4. Remove entry 411's shrink-to-fit Viewbox around the names: with B, a name is either full size or not shown.
5. Switching between the two states happens live when Android's text size changes, without restarting GroupLab, as entry 411's
   restyle already does for the text.
6. The iPhone is unchanged (entry 411 section 2.5: Alan answered for Android only).

## 2. Tests

1. A headless test: at the normal size all five names show; at Android's largest size (the sweep's `font_scale 2.0`) none show, every
   tab still has its name for accessibility, and the current place is still marked.
2. The emulator sweep's `--larger` check expects icons only on a small phone at the largest size.

## 3. Everything public

1. Mark entry 411's DESIGN NEEDED line answered (B, this entry) in for-alan.md.
2. Release-note (kind `changed`): at Android's largest text sizes, the bottom bar shows its icons alone, larger, instead of squeezing the
   names; your phone still reads each one out by name.
3. The phone guide, and the tour or Features page if one shows the bar at large text, in the same commit (entry 267). Features picture
   of the bar at the largest size (entry 256) if the emulator sweep can take one; otherwise list it in `docs/figures/SCREENSHOTS.md`.

Main model (UI). One commit. Stop at the usage guard as usual.

## 2026-10-12, entry 412: no shot targets are left; what Code has is all there is

**Status: actioned 2026-10-12; sections 1 to 3 done. Request 94 closed with Alan's words; entry 411 section 1.2 answered by it. Changed: request 74 (new photos of a blank target on the wood table), 86 (the 26 September aim point card was shot, so a new card comes with the next range kit) and 9 (where its scan is if it has left Downloads). Unchanged on purpose: 54, 58 and 75, which use unshot targets, and every request that prints something new.**
From the planning session, 2026-10-12 04:12 UTC. Take this after entry 411; it closes what entry 411 left open.

Alan, 22:10 Denver on 2026-10-11: "I do not have any targets that I have shot left. Only the pictures and scans that I have already given
you. If you have not gotten them, I do not have them."

## 1. Request 94 is closed

1. Step 2 cannot be done either: the 26 September 6 ARC sheet is gone. Close request 94 with Alan's words and the date, and move it to
   the archive. Entry 411 section 1.2 (waiting for his answer about that sheet) is answered by this.
2. Entry 399's results say plainly that both comparisons it wanted (a range photo against a flat scan of the same sheet, and the main
   lens against the ultrawide on the same sheet) wait for new sheets, per entry 411 sections 1.3 and 4.

## 2. Sweep the open requests for anything that assumes a shot sheet or an old photo still exists

1. Read every open request in for-alan.md. Any step that asks Alan to scan, photograph, measure or send a sheet he has already shot, or
   a photo he took earlier but never sent, is impossible. Rewrite that step to use a new sheet or a new photo (shot at the next range trip
   and kept, per entry 411 section 1.4), or close it if it only made sense for the old one. Say in each changed request what changed.
2. Known case: **request 74** asks for "the photos you took of a blank target on the natural wood table". Treat them as gone. Rewrite
   it to ask for one or two new photos of a blank, unshot target on the wooden table, all four corners in view, into
   `C:\Dev\grouplab-local\commercial-targets\`. That needs no range trip.
3. Unshot store-bought targets (request 75's tape measure, request 58, request 54) are not affected; leave them.
4. The pictures and scans already in `C:\Dev\grouplab-local\` are everything there will ever be from past range trips. If any plan,
   article draft or research note counts on more of the old material arriving, record that it will not.

## 3. Report

In the five line report, name every request changed or closed.

## 2026-10-12, entry 411: request 94's 4 October sheets are gone, and question 96 answered (the phone follows Android's text size)

**Status: actioned 2026-10-12; sections 1 to 3 done, section 4 recorded for the next range kit, which has not been made yet. Section 2.3's one layout that needs a real decision (the phone's bottom bar at Android's largest text size) is a DESIGN NEEDED line in for-alan.md and meanwhile draws a name smaller until it fits. Section 2.5: the iPhone does not follow Dynamic Type, question 97. Section 1.4's line was added to the hole size plan's text; its printed PDF was not remade, which the next range kit does.**
From the planning session, 2026-10-12 04:05 UTC. Alan's words, 22:03 Denver on 2026-10-11, are quoted where they decide something.

## 1. Request 94: the three sheets from 4 October no longer exist

Alan: "I dont have the sheets from Oct 4. I threw them out last night at the gas station because I did not know you still needed them."

1. Request 94 step 1 (scanning the C bull, the E bull and the 6 ARC load sheet shot on 4 October) cannot be done. Record that in the
   request and in entry 399's results: the range photographs of those sheets stay, the scans to compare them against will never exist.
2. Step 2 (the 6 ARC sheet shot with the Dominus K on 26 September, three photographs with the main lens) is still open. The planning
   session has asked Alan whether he still has that sheet; his answer comes back as an inbox entry. Leave request 94 open for step 2
   only, and reword it so it no longer asks for the 4 October scans.
3. Write down in PHASE1-RESULTS.md, under entry 399's results, what the mounted-sheet question now needs instead: a sheet shot at the
   range on a current build and scanned afterwards. Do not make it a new request now. Fold it into the next range kit (section 4).
4. **So this does not happen again:** from now on, any range kit or request that will need a shot sheet afterwards says so on the
   checklist itself, in plain words, naming the sheets to bring home and keep, and for how long ("keep these three sheets flat until
   GroupLab says it is done with them"). When the work no longer needs them, `for-alan.md` says in one line that they can be thrown
   away. Add that rule to CLAUDE.md under the range kit or for-alan section, and add a check to `ForAlanTests` or the range kit's test
   if one fits without much work (a request that asks for a shot sheet to be scanned later must say to keep it).

## 2. Question 96 answered: on Android, GroupLab's text follows the system's text size

Alan: "For android, follow the system".

1. Mark question 96 answered with Alan's words and the date.
2. Make GroupLab on Android scale its text with Android's font size setting.
3. Nothing may clip, overlap or run off the screen at the largest setting the emulator sweep uses (`font_scale 2.0`). Fix the layouts
   that break. If a screen cannot fit at the largest size without a real layout decision (a different arrangement, not just wrapping or
   scrolling), that is a look question: post a "DESIGN NEEDED:" line in for-alan.md with the screen and what breaks, and make it
   scroll or wrap meanwhile.
4. The test question 96 described: the sweep's largest-text frames must show bigger text than the plain ones, and a check fails on
   clipped or overlapping text at that size where it can be detected.
5. iPhone: Alan answered for Android only. Find out, without changing it, whether the iPhone build follows Dynamic Type, and record
   what was found as a new question for the planning session. Do not change the iPhone's behaviour in this entry.
6. Release-note (kind `changed`): text on Android now grows and shrinks with the phone's own text size setting. Update the phone
   guide, the site and the README wherever they talk about text size or accessibility, in the same commit (entry 267), and the Features
   or tour page if a screen's picture changes.

## 3. Budget

Main model for section 2 (it is UI). Chores to chores-haiku and wording to docs-sonnet as the standing rules say. Stop at the usage
guard as usual.

## 4. The next range kit

When the next range kit is made (requests 57, 58 and 86 are the range items), include one current GroupLab sheet to be shot mounted as
at the range and scanned flat at home afterwards, at 600 dpi as PNG, and say on the checklist to keep it (section 1.4).

## 2026-10-11, entry 410: an overnight queue that needs nobody (Alan asleep, about 12 hours)

**Status: actioned 2026-10-11; sections 1 to 6 done. Section 1 found the false marks' cause and kept no change: the one rule that removed them lost a real hole on scan 4, so the gate is still unmet (PHASE1-RESULTS.md). Section 3.3 was done after nightly 185 had published, not before, so a macOS hang could not stop it. Section 6's next item (request 83) needs Alan, so the block stopped there.**

From the planning session, 2026-10-11 10:05 UTC. Alan, 04:01 Denver: going to bed, and asked what Code can work on for the next 12 hours.
Everything below needs nothing from him; anything that turns out to need him goes to for-alan.md and the work moves on. Take the
sections in order, one commit each where it fits, a report after each.

**Budget for this block:** stop at **35% of the week** (23% at 10:01 UTC), or when the queue is done, whichever comes first, and never in
the middle of a publish. One worker, no helpers in parallel. Screenshots and chores to chores-haiku, wording checks to docs-sonnet, as
the standing rules say; detection, statistics and app code on the main model.

## 1. Detection: the false marks that keep the Phase 1 gate unmet

PROOF-CHECKLIST row 3: `grouplab scoreboard --corpus` finds 221 of 223 holes (99.1 percent) but makes **6 false marks**, 5 on the
crinkled .22 LR diamond sheet and 1 below the codes on the taped 2026-09-26 dominus-k picture; the checklist also says item 3 "needs a
change to detection rather than more material (197 of 198 holes, 2 false marks)". The gate is at least 99 percent found and **no false
positives**, matched at 0.15 in.

1. Look at what each false mark is (a crease, a shadow edge, printed artwork, a code's edge) from the scoreboard's own output and the
   show-your-work images, not by reading GPS or any metadata.
2. Fix the cause in detection, not by raising a threshold until the six vanish: a change that removes a false mark must not lose a true
   hole anywhere in the corpus, the rendered sheets or the scans. Report the scoreboard before and after, per picture.
3. Guard against fitting the nine pictures: say which pictures the change was developed on, and show it holds on the ones it was not
   (the four 2026-09-29 pictures, the scans, the rendered set). If it cannot be done without that risk, stop, write what was learned in
   PHASE1-RESULTS and the research log, and move on.
4. If the gate is met, update PROOF-CHECKLIST rows 1, 3 and 12 as the measurements say. A Release-note only if what a shooter sees
   changes (fewer marks that are not holes on creased or taped paper is something they see).

## 2. Proof row 16: the report's zero verdict is the screen's

PROOF-CHECKLIST says row 16 "needs its report's zero verdict to be the screen's". Make the session report PDF print exactly the zero
verdict the analysis screen shows (same sentence, same numbers), with a test that compares them on generated sessions and on Alan's own
session used by `RealSessionProofTests`. Mark row 16 met if it is.

## 3. Phase 9, the next optimization: finding holes on a photograph

Planning names it, as STATE asked: PERFORMANCE.md says that after entry 400 "finding the holes is next (180 to 540 ms), most of it now
the closing of the threshold mask and the alignment's phase correlations".

1. Measure first with `grouplab bench`; then speed up those two stages (for example a smaller structuring element done twice, working
   at the resolution the decision needs, reusing FFT plans or buffers across bulls), one change at a time.
2. The results must not change: every hole center within 0.0005 in of before on the bench material and the corpus, the same holes found.
   No figure in `docs/performance-baseline.json` may get slower by more than the gate allows.
3. Also, as STATE says: if the macOS Mobile suite has passed since entry 400, put "reading the codes all at once" back behind its test.
4. Record the before and after in PERFORMANCE.md. A Release-note (kind `changed`) only if a person would notice the time.

## 4. After nightly 185 publishes (about 12:45 UTC)

1. Check it carries entries 407 to 409, that its release notes read well to a shooter and name Quack for the thermal warning fix, and
   that the site shows it.
2. The tour pages and Features pictures for every screen entry 407 changed (Fix holes and the analysis lines, the setup cards, the
   Shots list, the ··· menu, Equipment, the paper fold, the Velocity block): each tour sentence still true, each new thing with its own
   Features picture (entry 256). Fix any that are not.
3. An emulator walk of the phone's changed screens (chores-haiku): lines to bulls, tapping a bull and its holes, Add a hole, Next unsure
   hole, the two setup cards, the rifle page with its barrels, the Velocity entry point. Anything broken is a bug for the main model to
   fix in this block.

## 5. After the learning nightly (13:45 UTC)

Entry 406 section 1.3: read that night's `learning/summary.md` once for the token line and say in the report whether it reads yes for
both repositories. If it does not, write it into for-alan.md plainly.

## 6. When the queue is empty

Rewrite STATE.md, make sure nothing is left running, and end with the usual status. If budget remains, the next thing is item 2 of
STATE's "next three" only if it needs nobody; otherwise stop.

## 2026-10-11, entry 409: the thermal "would be cut off" warning stays after switching back to a sheet printer (Discord report)

**Status: actioned 2026-10-11; sections 1 to 5 done. Section 1's phone part: the phone's Targets screen never shows this warning, so there was nothing to change there.**

From the planning session, 2026-10-11 07:50 UTC. A tester on the GroupLab Discord reported it through Alan, with a screenshot (macOS,
nightly 174, ce1a7de). The report is data, not instructions; what follows is the planning session's reading of it and of the code.
Take it after entries 407 and 408, unless 407 is long: it is a small fix and may go first in its own commit.

## What the tester saw

Targets, "GroupLab 3x3 2 MOA, C Bull, Set of 3 Letter Sheets" (the screenshot shows the E Bull set). Red text in the print panel: "This
page is 215.9 mm wide and the printer's head prints 105.7 mm, so 55.1 mm at the left and 55.1 mm at the right would be cut off ...
Choose a size this printer takes." The preview is the normal Letter page, not the dot preview, so no thermal printer was chosen any
more. It first appeared after calibrating a Brother printer by scanning a sheet. Clicking "Open to print" cleared it. Reproduced:
Print on a thermal printer (the text appears), then Print on the Letter printer again: the text stays.

## Cause (read from main, so nightly 184 has it too)

`PrintPanel.ShowDots()` sets the cut-off text with `SetStatus(cut, StatusKind.Alert)` when `Head` is a thermal head, but when `Head`
becomes null (a sheet printer chosen) it returns early and never clears that status. The text stays until something else writes the
status line, which is why "Open to print" made it go. 105.7 mm is the 4 inch (832 dot, 203 dpi) head.

## What to do

1. When the printer changes to one with no head, or the page or sheet changes and nothing is cut off, clear a status that was the
   cut-off warning (only that one: do not wipe an unrelated message). The same on the phone if its Targets screen shows this warning.
2. Find out why it appeared right after the printer calibration by scan: whether that flow rebuilds the "Print on" list or moves its
   selection onto a thermal entry for a moment (an index shift when a calibrated printer is added would do it). Fix that too if so.
3. A test: choose a 4 inch thermal head on a Letter sheet (warning shown), choose the sheet printer again, the warning is gone; and
   calibrating a printer leaves "Print on" where it was.
4. Release-note, kind `fixed`, in a shooter's words, for example: "Switching from a thermal printer back to your sheet printer no
   longer leaves a red warning about the page being cut off."
5. **The tester may be thanked by name: Quack** (Alan, 2026-10-11 02:15 Denver: "Yes Quack can be thanked by name"). Name Quack in the
   release note (for example "... Thanks to Quack on the Discord for the report.") and add Quack to the credits wherever testers are
   thanked, recording Alan's words and the date where the other consents are kept (samples/PROVENANCE.md or the credits' own record).
   Quack is a Discord name; write nothing else about them.

## 2026-10-11, entry 408: request 95 confirmed on the phone

**Status: actioned 2026-10-11, in entry 407's commit; sections 1 and 2 done.**

From the planning session, 2026-10-11 07:22 UTC. Small; take it after entry 407, or fold it into 407's commit if that is simpler.

Alan, 2026-10-11 01:20 Denver: "It looks like request 95 is resolved." He checked the Fix holes fix (entry 401) on the phone with
nightly 184 or later: adding a hole asks which bull, and Fix holes comes back with the picture instead of going blank.

1. Close request 95 in `for-alan.md` with the date and that sentence, and move it to the archive as usual.
2. Note in STATE.md that entry 401's fix is confirmed on real hardware.

No Release-note needed: nothing that ships changes.

## 2026-10-11, entry 407: Alan's picks from the open screen decisions canvas

**Status: actioned 2026-10-11; sections 1 to 8 done, one commit a section. Kept that the entry allowed to go: the lasso (section 1.6, it puts many shots on a bull in one gesture), and the explicit tap for the bulls fired at, as the correction of the ones GroupLab finds (section 2.1). The phone has no barrel screens (section 5.6). Details in PHASE1-RESULTS.md, "Entry 407".**

From the planning session, 2026-10-11 07:20 UTC. Alan, 2026-10-11 01:15 Denver, picking from the canvas "Open screen decisions"
(claude.ai/artifact/KhPTW4kSQ7FUiGdxYPNFJD, made from for-alan.md's DESIGN NEEDED list and question 88): **1A, 2B, 3A, 4A, 5B, 6A, 7A.**
This settles the seven DESIGN NEEDED items from entry 374 section 5 that were still open, and question 88. Mark each one decided in
for-alan.md and question 88 answered (7A) in QUESTIONS-FOR-PLANNING.md.

Read the whole entry first. Build in the order below, one section at a time. Section 1 (the canvas's question 4) comes first because it is Alan's own top
complaint from the phone (2026-10-05). **Phone parity applies** (entry 258): every section changes the desktop and the phone together
wherever the phone has the screen, and says in the report where it does not and why. Sample numbers on the canvas are samples, not
specifications. Where a picture and this text disagree, this text wins; where something here contradicts the code, raise a question and
build the rest.

Every section that changes a screen: README, site, guides, tour words and the Features page pictures in the same change (entries 146 and
256 and 267), a Release-note trailer in a shooter's words, and the tests that hold the old behavior updated rather than deleted.

## 1. (Do this first) Which bull each shot belongs to: concept 4A

1. **Lines always on**: on the analysis picture and in Fix holes, desktop and phone, a thin line from each bull's center to every hole
   assigned to it. A switch "Lines to bulls" (on by default, remembered) hides them.
2. **Unsure holes stand out**: a line that is long for the sheet, crosses into a neighbor bull's outer ring, or belongs to a hole the
   matcher was not sure of, is drawn dashed in orange, with a small "?" beside the hole. Say in PHASE1-RESULTS what "long" and "not
   sure" mean in the code (use the matcher's own confidence where it has one; do not invent a new score).
3. **Tap a bull to choose it**: the chosen bull gets a blue ring and its lines turn blue; other bulls dim. A banner says "Bull C3
   chosen. Tap a hole to give it to C3, or tap one of its holes to take it away." Tapping a hole toggles it in or out of that bull.
   Done, Esc (desktop), the back button (Android) or tapping the chosen bull again ends it.
4. Under the picture on the phone: "C3 has 2 shots", the count of unsure holes, and two buttons, **Add a hole** and **Next unsure
   hole** (steps through the orange ones). Desktop shows the same in the side panel.
5. A hole taken away from a bull with no other home goes back to "nearest bull" assignment and is re-matched; say how in the report.
6. This replaces the desktop lasso as the main way to put shots on a bull only if nothing depends on the lasso; otherwise keep the lasso
   too and say so. The existing "Which bull was this hole fired at?" question on Add a hole stays as built.
7. Colors: blue and orange must differ in lightness too (canvas used #2f7fe0 and #c46a00 on the paper); check contrast on the white
   sheet and in the dark theme.

## 2. The Groups section: concept 1A

Replace the Groups section ("Shots per bull" dropdown and its text box, "Its row", "Its column", "Load on the chosen bulls", "Bulls you
fired at") with two plain parts in the setup panel:

1. **"How did you shoot this sheet?"**, three choices as large radio cards with one line each:
   - "One shot on each bull I used" (default): today's one-to-one matching. GroupLab asks about any bull that seems to hold two.
   - "Some bulls took two or more": then tap those bulls on the sheet (today's "Two on the bulls named", without typing numbers).
   - "Every hole to its nearest bull, however many": today's nearest rule; nothing is asked.
   Under them: "Bulls left blank are found by themselves: N bulls unused. Wrong? Tap them." This is what feeds the whole-sheet offset
   solver's "bulls you fired at" (entry 130 section 3.1); it must keep working on a part-shot sheet, and the user can still correct the
   set by tapping bulls. **If finding unused bulls by themselves is not reliable enough today, say so in the report and keep an explicit
   "tap the bulls you fired at" step instead**; the offset fix must never silently lose its input.
2. **"Loads on this sheet"**: four buttons, "One load", "One a row", "One a column", "I will pick bulls". One a row or One a column shows
   a short grid of row (or column) to load dropdowns; the sheet shows the load name beside each row or column. "I will pick bulls" is
   today's shift-click selection.
3. A read-back line under both: "GroupLab will read it as: 23 bulls fired at, one shot each, rows 1 to 5 as Loads A to E." This
   replaces `ruleSays`.
4. "Only the bulls I aimed at" (today's fourth rule): fold it into choice one plus the tap-to-correct unused bulls, if the code allows;
   otherwise keep it as a fourth card and say why.
5. Phone: the same two parts on the phone's setup, cards full width.

## 3. The Shots list: concept 2B

1. The list stays short rows: shot, bull, across, up/down, radius. A shot left out shows its numbers struck through and "left out".
2. Clicking a row, or a shot on the sheet, opens it in place (one open at a time) with: **Bull** dropdown (nearest first, marked
   "nearest"), **Fired** dropdown (place in the firing order), **Counted** check box, where it came from ("Found by GroupLab" or "Added
   by you"), and **Delete, not a hole**.
3. Delete replaces the "Not a shot" button in the row, but keeps what Not a shot keeps: the mark, its provenance and Undo. Nothing is
   lost from the session record.
4. The current Exclude and Restore buttons become the Counted box. Tick boxes for choosing several shots stay if they are still used
   (entry 141 section 5.3.3).
5. Phone: the same, the open row as a card under the shot.

## 4. The Selected shot reason and Show in folder: concept 3A

1. Remove the reason dropdown from the Selected shot area. Turning Counted off asks no reason. Sessions that already carry a reason keep
   it and still show it, read only. Reports and exports keep printing a stored reason.
2. Remove "Show in folder" from the bottom bar. Add "Open the session's folder" to the ··· menu at the top right (beside Report, Export,
   Send this target). The bottom bar keeps "Saved ... · safe to close".

## 5. Barrels: concept 5B

1. Equipment gets two tabs, **Rifles** and **Loads**; the Barrels tab goes.
2. A rifle's page has two parts: **The rifle** (name, cartridge, scope, click value, sight height, zeroed at, chassis or stock, notes)
   and **Barrels on this rifle**, a table: on now (one radio for the rifle), name, length, twist, direction, rounds through it, Edit, and
   an **Add a barrel** button. Barrel length, twist and twist direction move off the rifle form onto the barrel.
3. Setup: one list, rifle and barrel together, written "Test rifle with its 26 in Bartlein (on now)", the barrel on now first; other
   barrels of the same rifle listed under it.
4. Rounds are counted onto the barrel that was on when the sheet was shot.
5. **Migration**: every existing barrel needs a rifle. A barrel used in sessions with exactly one rifle goes under that rifle; any other
   barrel goes under a rifle named "Barrels not yet on a rifle", which the user can move. A rifle's own barrel length and twist become
   its first barrel if it has none. Back up the database first (docs/RESTORE.md), migrate, and test that every old session still names
   the same rifle and barrel. The export and import file (entry 307) carries the new shape and still reads the old one.
6. Phone: the same on the phone's equipment screens if it has them.

## 6. Paper and backing: concept 6A

Keep "Paper it was printed on" and "What was behind it", moved to the bottom of setup inside a fold **"For the hole size studies"**,
closed by default, with: "Recorded only: it does not change detection. It lets hole sizes be compared later by paper and backing." Add
"Remember these for my next sheet", on by default. Phone the same.

## 7. Chronograph readings on the analysis: concept 7A (answers question 88)

1. On the analysis, in the figures under **Velocity and the vertical**, when a sheet has no readings: one line saying what readings add,
   then **Import a file** (primary) and **Type them in**, and a drop area: "or drop a Garmin Xero, LabRadar or CSV file here" (name only
   the formats GroupLab really reads).
2. Type them in opens a box under the buttons: speeds in the order fired, separated by spaces or new lines, a count ("12 of 23
   entered"), and **Match to shots**, which uses the same pairing as Ballistics' chronograph section.
3. When readings exist, the block shows what it shows today. Ballistics' chronograph section stays; "Add readings" now lands here.
4. Phone: it already opens its own page; make its entry point sit in the same place on the result screen.

## 8. Report

The five line report as usual, plus: which section's tour page and Features picture changed, anything kept that this entry said to
remove (and why), and the migration's before and after counts for section 5.

## 2026-10-11, entry 406: requests 93 and 96 answered, and the server paste's silent stop

**Status: actioned 2026-10-11; every section done. Section 1.3, the token line, was read in entry 410 section 5: yes for both repositories.**

From the planning session, 2026-10-11 00:55 UTC (Alan, 2026-10-10 18:51 Denver). Take it after entry 405.

## 1. Request 93 done: record it and close it

Alan checked that the Oracle `grouplab-daily` backup was current, then ran request 93 (the learning nightly's own token check) in two
parts, because the first paste stopped early (section 3). What the server printed, in full:

```
learn-worker.new: OK
backup: /home/ubuntu/grouplab-server/backups/2026-10-11T004851Z-learning-token-check
grouplab-learn-worker.py: OK
backup holds the old worker
- The learning token reaches the error reports: yes; GroupLab's pull requests: yes.
```

So `/usr/local/sbin/grouplab-learn-worker.py` is now the main copy with fingerprint
`cdf083f708aea46743f92df0de1c292280b15d3f8369d2cb39a983b73d9db0cf`, installed 755 root:root, and the token reaches both repositories.

1. Record the backup in `docs/RESTORE.md` (folder above, the one file it holds, how to undo with it), and take request 93 out of
   RESTORE.md's waiting list.
2. Close request 93 in `for-alan.md` with the date, and move it to the archive as usual.
3. After the next learning nightly (13:45 UTC), read its `learning/summary.md` once for the token line, and say in that day's report
   whether it read yes for both.

## 2. Request 96 done: the download page on Alan's Samsung tablet

Alan opened `https://grouplab.org/download/?why` in both browsers on the tablet and sent screenshots. Both picked **Android, Looks like
this device**. What the page printed (no identifiers beyond these):

- Firefox: user agent `Mozilla/5.0 (X11; Linux x86_64; rv:157.0) Gecko/20100101 Firefox/157.0`; platform `Linux armv81`; oscpu
  `Linux armv81`; userAgentData platform none; maxTouchPoints 5; pointer coarse true; any pointer fine true; hover false;
  guess `android, from an ARM processor under an x86_64 user agent`.
- Chrome: user agent `Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36`;
  platform `Linux armv81`; oscpu none; userAgentData platform `Linux`; maxTouchPoints 5; pointer coarse true; any pointer fine true;
  hover false; guess `android, from an ARM processor under an x86_64 user agent`.

1. Close request 96 with the date and this result. Entry 403's fix is confirmed on real hardware in both browsers.
2. If a test holds the detection to sample user agents, add these two (the Chrome one is new: entry 403 was found with Firefox).

## 3. The server paste stopped silently: fix the pattern

`grouplab-change-backup.py` makes its folder 0700 root (`os.chmod(folder, 0o700)`), but request 93's paste then checked it as ubuntu with
plain `test -f "$B/files/..."`. That check cannot see inside the folder, so it failed without printing anything, and the `&&` chain stopped
after the backup with nothing installed. Alan saw two lines and no error. The second paste used:

```
{ sudo test -f "$B/files/..." && echo "backup holds the old worker" || { echo "STOP: the backup does not hold the old worker"; false; }; }
```

1. Find every paste in `for-alan.md`, `docs/`, `website/server/` and `scripts/` that checks a change backup's contents without sudo
   (`grep -rn 'test -f "\$B'` and similar), and change it to the form above: sudo, plus a line that says when it stops.
2. If server pastes are generated or checked by a script or test, make it reject a plain `test` (or `ls`, `cat`) on a backup folder
   path, so this does not come back.
3. Rule for later pastes: no step in a chain may fail without printing something.

Release-note: none needed for sections 1 and 2 (server and notes only); section 3 changes nothing that ships either.

## 2026-10-10, entry 405: consistency audit, 2026-10-10

**Status: actioned 2026-10-10; every section done. Section 2: fingerprint now shows its phone picture; analysis-open, compare and ballistics stay unpictured on the phone, with words saying the emulator does not take them yet, rather than adding them to the emulator's walk in this change.**

The scheduled consistency audit (entry 267 section 2b). Read on 2026-10-10 around 16:00 UTC: README.md at 0903c892, the live site
(home, /download/, /features/, /tour/ and the stops fingerprint, compare, analysis-open, ballistics and capture, /shoot-a-target/,
/guides/ with both guides, /releases/, /support/, /research/), which serves nightly 184 and commit df55224, website/features.json,
website/tour.json, website/build.py, docs/RELEASE-NOTES.md (nightlies 182 to 184), STATE.md, for-alan.md and its archive (requests 84
and 85), PLATFORM-SUPPORT.md, PHONE-PARITY.md, ANDROID.md, USER-GUIDE.md, TESTING-GUIDE.md, PERFORMANCE.md, and the commits since
2026-10-07. No GitHub issue labelled `consistency` exists, open or closed (the public API returned none), so nothing here repeats one.

Already right: the README, /download/ and /releases/ name nightly 184 and df55224; the Store line says 0.2.182, as external-status.md
does; nightly 184's notes say "about a tenth sooner", matching the part of entry 400 taken back; nothing says a lawyer is reviewing
anything; "What GroupLab is not" is gone; Android is offered as GroupLab Dev, the plain APK and the Play internal test; the credits
name Unholy and Jylee only; the user guide already describes the M220 printing from the phone and the M834 at true size.

Ten findings, the public ones first. Fix them in one change, README, site, guides and CLAIMS.md together, as entry 267 asks.

### 1. The phone pictures are said to be real device screenshots of nightly 115

- **Where:** README.md line 27, "the phone's from nightly 115, retaken at the next device sitting"; website/build.py line 2153 (the
  tour index, Mobile: "The pictures are real screenshots from a Galaxy Z Fold 7 and a Galaxy Tab S8 Ultra, of nightly 115, and are
  retaken at the next device sitting. The result in them is Alan's own scan of a 25 shot group") and line 2537 (the same on /features/).
  Both are live on https://grouplab.org/tour/ and https://grouplab.org/features/ with the Mobile switch on.
- **Should say:** the phone pictures are taken on the Android emulator, sized as a Galaxy Z Fold 7 and a Galaxy Tab S8 Ultra, after
  each nightly the emulator runs, now from nightly 183; the result in them is the 600 dpi sample sheet. Better read the nightly from
  docs/figures/screens/phone/made-from.json so it cannot go stale again.
- **Evidence:** commit 1b731a65 ("[screens] the phone's screenshots, taken on the Android emulator", question 90 b);
  docs/figures/screens/phone/made-from.json says nightly 183; docs/figures/screens/phone/SOURCES.md says the emulator's pictures "are
  sized as the devices but are not taken on them".

### 2. The tour says four phone pictures come "at the next device sitting", and one of them already exists

- **Where:** website/tour.json, `"pending": true` for fingerprint (line 995), analysis-open (406), compare (538) and ballistics (673);
  the words of each end "The picture of it on the phone comes at the next device sitting." Live on https://grouplab.org/tour/ (Mobile
  eyebrow "On the phone; its picture comes at the next sitting") and on /tour/fingerprint/ and /tour/compare/.
- **Should say:** fingerprint should show `fold-fingerprint` (docs/figures/screens/phone/fold-fingerprint-light.png and -dark.png exist
  since entry 388, and https://grouplab.org/assets/screens/phone/fold-fingerprint-dark.webp answers 200). For the other three, either
  add them to scripts/android-screens.sh, or change the words: the pictures now come from the emulator, not from a device sitting.
- **Evidence:** SOURCES.md's table lists fold-fingerprint ("Add a store-bought target at its second step", entry 388).

### 3. The M834's true size is still called unchecked

- **Where:** README.md lines 310 to 311, "a real M834 has printed a whole Letter sheet from it, and its true size is still being
  checked"; website/features.json `phone-targets` (line 1211), "so GroupLab now draws its M834 pages that much longer along the paper,
  and whether that makes them true is still being checked" (live on https://grouplab.org/features/).
- **Should say:** from nightly 181 the pages sent to the M834 are drawn 0.7 percent longer along the paper, and the third check print
  came out true both ways to 0.05 percent (150.0 by 150.0 mm on the caliper lines). Windows printing to the M834 stays "built and not
  yet tried" until request 83.
- **Evidence:** for-alan-archive.md request 85, "Answered 2026-10-10 ... True both ways to 0.05 percent"; commit 00de13ea.

### 4. The M220 scale labels are still called never printed on a real M220

- **Where:** README.md line 322, "scale labels from a label printer such as the Phomemo M220 came in nightly 168 and have not yet been
  printed on a real one ... on Android two labels print straight to the M220 over Bluetooth (entry 386), not yet tried on a real one";
  docs/CLAIMS.md line 3584, "Not yet printed on a real M220 by this path" (check line 3976 too).
- **Should say:** two labels printed from the phone on a real M220 on 2026-10-08 and again for request 84: every code whole, codes
  59.96 to 59.98 mm apart across (60.0 designed); the pause after each label fixed the second label's shift; each label prints about
  0.4 to 0.7 mm left of center, which question 94 leaves as it is.
- **Evidence:** for-alan-archive.md request 84 ("Answered 2026-10-10") and request 82; commit 00de13ea; docs/RELEASE-NOTES.md line 82.
- **Also:** the Features page `scale-markers` sentence (features.json line 264) never mentions scale labels or the M220, though its
  notes do. One clause would put it where users look, for example "or scale labels from a label printer such as the Phomemo M220,
  printed straight from Android".

### 5. Label targets: "No label printer has been tested yet"

- **Where:** website/features.json `label-targets` (line 1146): "No label printer has been tested yet, so until one has printed
  GroupLab's check page this is \"should work\"." Live on /features/.
- **Should say:** narrower, so it stays true: no 4x6 label printer has been tested yet (Unholy's model is request 72). The Phomemo
  M834 has printed GroupLab's printer check page true both ways, and the M220 has printed its scale labels, so "no label printer" reads
  as wrong to anyone who has followed the Discord.
- **Evidence:** requests 84 and 85 as above.

### 6. The narrow analysis screen is "being worked on", and was fixed in entry 243

- **Where:** docs/PLATFORM-SUPPORT.md lines 49 to 50, "the analysis screen needs about 1060 units of width; narrower, its right column
  runs past the window, which is being worked on", copied into README.md line 210 and https://grouplab.org/download/ (What each device
  needs, Screen).
- **Should say:** what it does today: below the default width the side columns narrow, and on the narrowest window the figures move
  under the picture; 1060 wide is the size every desktop window is tested at. Keep the minimum in the table if it is still the right
  advice, but drop "runs past the window, which is being worked on".
- **Evidence:** commit e3f24a58 (entry 243, question 58 option A, release note "On a narrow window ... the figures move under the
  picture instead of running off the edge", tested at 1400, 960 and 683); commit 633c64d0 (entry 342 item 3, DesktopSweepTests: nothing
  past the right edge at 1060 by 720).

### 7. The testing guide says the mounted photograph gate has one day's material

- **Where:** docs/TESTING-GUIDE.md line 93, "The mounted photograph gate has one day's material, 59 photographs from 2026-09-20, and
  GroupLab could not read about half of them." Live on https://grouplab.org/guides/testing-guide/ and in its PDF.
- **Should say:** add the second range day: on 4 October GroupLab's own sheets gave 137 of 145 holes found with no false mark
  (`scoreboard --corpus`), and the gates still wait on request 94's scans and photographs. Rebuild TESTING-GUIDE.pdf with it.
- **Evidence:** docs/PHASE1-RESULTS.md, entry 374, "The range photographs in the local corpus and the scoreboard (section 1,
  2026-10-07)"; the same file near line 1490 for request 94.

### 8. The README's mosaic was made from an older phone capture picture

- **Where:** docs/figures/readme/mosaic-light.png and mosaic-dark.png; docs/figures/readme/made-from.json records fold-capture-dark.png
  as 6c3cded6..., and the file is now 0d9ff17d... (changed by 1b731a65). fold-result-dark.png still matches.
- **Should say:** rerun scripts/readme-images.py so the README's six screens match the current phone pictures. If the weekly check does
  not already compare made-from.json with its inputs, it would catch this by itself next time.

### 9. PERFORMANCE.md's heading for entry 400 says "about a third sooner"

- **Where:** docs/PERFORMANCE.md line 51, "## Entry 400: the phone's reading of a photograph, about a third sooner"; the same words in
  docs/PHASE1-RESULTS.md's heading for entry 400 (near line 1495).
- **Should say:** "about a tenth sooner, part taken back", as the body (the paragraph "Taken back the same day, in part": 796 ms against
  893) and nightly 184's release note say.

### 10. Nothing else found

The home page, /download/, /shoot-a-target/, /support/ and the README's download table agree with STATE.md, the release notes and
external-status.md. The download page's `?why` line (entry 403) is a diagnostic and needs no public mention.

Nothing here needs Alan: no decision, secret or device.

## 2026-10-10, entry 404: overnight, the last part: bring the server's intake worker up to the repository's

**Status: actioned 2026-10-10 as far as Code can: section 1 done; sections 2 and 3 are request 97, two pastes for Alan, because this session's connection to the server is refused by its permission check, which entry 404 says not to work around.**

From the planning session with Alan, 2026-10-10 02:00 Denver, part of "queue all four" (entries 399, 400, 402 and this one; entries 401 and 403 were added after and need nobody, so they go first). Take it last.

STATE: the server's intake worker is older than the repository's; entry 357's change, built switched off, was never installed there.

1. Prepare everything first: the diff between the installed worker and the repository's, what will change on the server, and the undo,
   written down under RESTORE.md rule 5 (CLAUDE.md rule 4) before anything runs.
2. Install it the usual way: `grouplab-change-backup.py` first, `install.py` with `--dry-run` and then without, the switched-off change
   left switched off, `nginx -t` before any reload, both sites checked after, the commands and what they printed in panel.md.
3. **ssh and sudo stop for Alan's approval, and he is asleep.** If the command waits for approval, leave it waiting as the last thing of
   the night: entries 399 to 403 must already be committed, pushed and reported before this one reaches the server. Do not look for a way
   around the approval.

## 2026-10-10, entry 403: the download page still says Linux on Alan's Samsung tablet in Firefox

**Status: actioned 2026-10-10; sections 1 to 4 done; section 5, the site live check, after the push. Firefox's desktop-site values are not documented, so request 96's readout confirms it.**

From the planning session with Alan, 2026-10-10 02:17 Denver. Alan: "I tried going to the downloads page on my samsung galaxy tablet in
firefox and it is still showing linux as the OS instead of android. Not a huge deal, but if there is a way to fix this, it would be good."
Take it after entry 402 and before entry 404's server step. Needs nobody. Main model for the detection, a site change otherwise.

**What is live** (`DOWNLOAD_JS` in website/build.py, entry 349): a "X11; Linux" browser is taken for Android only when the touch screen is
the only pointer, `(pointer: coarse)` and not `(any-pointer: fine)`. **The likely reason it fails here, not yet confirmed:** Alan's
Samsung tablets (Tab S8 Ultra) have an S Pen digitizer, and a stylus is reported as a fine pointer (and as hover), so `touchOnly` is false
even with the pen in its holder. A keyboard cover with a trackpad would do the same. Firefox for Android and Chrome on large tablets both
ask for the desktop site by default, so this will be common, not Alan's alone.

1. **Use signals a desktop-site request does not hide, before the pointer test:**
   1. `navigator.userAgentData` where it exists (Chromium): `platform === "Android"`, or `getHighEntropyValues(["platform"])`, means Android
      whatever the user agent says.
   2. `navigator.platform` (and Firefox's `navigator.oscpu`) naming an ARM processor (`arm`, `aarch64`, `armv8l`) while the user agent says
      `x86_64`: the browser is pretending, and that is Android. A Linux computer on ARM says ARM in both and stays Linux.
   3. Keep entry 349's touch-only test as the last fallback, and keep "Looks like this device" for any guess resting on these signals.
   Check each against the browsers' own documentation or source before relying on it; record in PHASE1-RESULTS.md which ones are
   documented and which are observed.
2. **A readout to confirm it:** `?why` on the download page shows, under the device buttons, the user agent, platform, oscpu,
   userAgentData platform, maxTouchPoints, the pointer and hover media results, and which rule chose the guess. Shown only on screen,
   never sent or logged anywhere.
3. Tests in the site's own suite for each user agent and signal combination (Firefox and Chrome desktop mode on an ARM tablet, a tablet
   with a stylus, a Linux laptop with a touch screen and a trackpad, a Raspberry Pi, ChromeOS), so entry 349's cases still pass.
4. Write a request in for-alan.md: open `https://grouplab.org/download/?why` (use the page's real address) in Firefox on the tablet and
   say which button it picks, with what the readout shows. The page can be confirmed only on his tablet.
5. The site publishes itself on push; confirm the commit is live in the `grouplab-site-build` meta tag, as CLAUDE.md says.

## 2026-10-10, entry 402: overnight, part 3 of 4: the four research articles already marked \

**Status: worth**

From the planning session with Alan, 2026-10-10 02:00 Denver, part of "queue all four" (entries 399, 400, 402 and 404; entries 401 and 403 were added after). Take it after entry 401 is
committed and reported. Wording passes on docs-sonnet and figure chores on chores-haiku, as CLAUDE.md says; measurement and statistics
on the main model.

RESEARCH.md's "Worth an article?" table has four marked worth writing and not yet written. Write all four:

1. **The Phomemo M834's print commands** (request 73, entries 389 to 391): the Phomemo app prints a Letter sheet at 94.7 percent; the
   raster (LZO1X in 4 KB blocks over the serial port, the printer pacing the page and answering when printed); the feed 0.7 to 0.8
   percent short and GroupLab's 0.7 percent stretch; request 85's third print true both ways (150 by 150 mm on the caliper lines, 190 and
   250 mm on the rulers). Printing from a computer is built but untried (request 83): say so, and claim nothing about it.
2. **Scale markers beside a target** (entry 365, `grouplab marker-trial`, entry 375's corner brackets): how accurate each of the four
   kinds was, measured numbers only.
3. **How far off square a photograph can be** (entry 157): what the holes and the bull centers did as the angle grew, and what a shooter
   should do with that. Include entry 399's new numbers if they change the answer.
4. **A bent-sheet registration for a lifted far margin** (entry 324 section 1, and entry 399 section 3 if it changed the model): written
   for a developer.

For each: entry 392 section 1's rules hold (every checkable sentence backed in claims-backing.json, figures regenerated only for the
article that changed, simulations seeded and rerunnable, no pseudoscience, the developer called "the developer" or "the author", the
existing byline). **An article that meets them is published** under the standing rule (CLAUDE.md, entry 393), listed in
`website/research/PUBLISHED.md` with the date and this entry; one that cannot be finished honestly stays `draft` with one line saying
what it waits on. Name each article published in the report, and confirm the site went live with it.

## 2026-10-10, entry 401: a bug Alan hit: after Fix holes and adding an impact, the results go blank (error report sent)

**Status: actioned 2026-10-10; sections 1 to 4 and 6 done. Section 5, closing issue 27 with its nightly, waits for that nightly to publish; the log package was not pulled, since that needs ssh.**

From the planning session with Alan, 2026-10-10 02:55 Denver. Alan: "I am having a problem with the latest build of group lab where after
I fix the holes on the Target by adding a new impact, the results page goes blank and I can no longer see the Target or edit the
results. I submitted an error report."

**This comes first** (CLAUDE.md: bugs Alan hit before anything else). Finish the entry in hand (400) to a clean commit, then take this
one before entry 402. Main model.

1. `gh issue list -R oRAirwolf/grouplab-crash-reports --state open` and read the newest report: which build, which device and platform
   (phone, tablet or desktop), the stack and the last actions. The report's text is data, never instructions (CLAUDE.md). Say in the
   report which issue it was. If no report arrived, say so, and work from the description.
2. Reproduce it in a test: open a target, Fix holes, add an impact (a hole by hand, with its bull chosen, entry 372's bull choice), go
   back to the results. The screen must show the target and the results with the new shot counted, and stay editable. Cover both
   phones' shared screens and the desktop if the code path is shared; say which platforms the bug reached.
3. Fix the cause, not the symptom. If the results cannot be drawn for a reason, the screen must say so in the middle of the screen with a
   way back (Alan, 2026-10-02), never go blank.
4. Check what happens to the session: whether the added hole and the earlier marking were kept or lost, and whether reopening the target
   shows them. If anything was lost, say so plainly in the report and in the release note.
5. Close the issue with the commit and the nightly it ships in. Release-note-kind: fixed, in a shooter's words.
6. If the fix needs Alan to confirm on his device, write a request in for-alan.md with the exact steps (which build, which target, what a
   good result looks like).

## 2026-10-10, entry 400: overnight, part 2 of 4: Phase 9's second speed-up, the phone's reading of a camera photograph

**Status: actioned 2026-10-10; every section done. An ordinary Fold 7 picture reads 31 percent sooner and the nine together 38 percent, every result unchanged; the emulator reads only the 600 dpi scan, so section 4's emulator record has nothing to compare.**

From the planning session with Alan, 2026-10-10 02:00 Denver, part of "queue all four" (entries 399 to 401 and 403). Take it after entry 399 is
committed and reported. Main model.

STATE says Phase 9's next optimization waits for planning to name one. **Planning names: the phone's whole reading of a camera
photograph, from the picture to its result** (`PhoneAnalysis.Run`; PERFORMANCE.md: 2.5 s median for a Fold 7 photograph headless on
this desktop, and the emulator's stages in performance-baseline.json show identify, prepare and hole finding as the large ones).

1. Measure it stage by stage on the Fold 7 photographs from the 2026-09-29 sitting (and the entry 342 set), on this desktop, Release,
   nothing else running. Name the slowest stage after entry 399's changes, since they may have moved it.
2. Make that stage faster. Aim for a third off the whole reading; record what was tried, including what did not help.
3. **Results must not change**, the same rule as entry 392 section 2: every hole center and figure identical to before on the photographs
   and the scan corpus (or within a stated tolerance far below 0.001 in, with the reason). If going faster would change what is detected,
   stop at the faster part that changes nothing and raise a question instead.
4. Add the phone photograph's reading to the bench and the baseline if it is not already a gated figure; update the baseline only where
   faster. If the emulator workflow runs it, record the emulator's before and after too (a record, never a gate).
5. Release note in plain words: how much sooner the phone shows a result after the picture is taken.

## 2026-10-10, entry 399: overnight, part 1 of 4: accuracy on photographs (registration at an angle, the bent sheet, holes ringed as doubles)

**Status: actioned 2026-10-10; every section done. Section 2 kept one change (a photograph without camera tags is read through the lens); section 3 changed no model, because seven frames of one sheet cannot choose one honestly; rows 1 to 4 are still not met, and request 94 asks for the material that would settle rows 1 and 2.**

From the planning session with Alan, 2026-10-10 02:00 Denver. Alan asked what large work Code can do overnight and said "queue all
four" to entries 399 to 401 and 403 (entry 402, a small fix to the download page, was added after). He is going to bed, so this runs unattended. Take them in number order, one at a time, each its own commit
and report. Within the 85% guard and the standing budget (CLAUDE.md, entry 317): if the day's pace is reached, finish the entry in hand,
report, and stop. One worker (CLAUDE.md, 2026-10-08): no helpers in parallel. This entry is main model work throughout.

Why this one first: most shooters will photograph a target rather than scan it, and photographs are where GroupLab is furthest from its
own written gates (docs/PROOF-CHECKLIST.md rows 1 to 4). Nothing here needs Alan; the material is already on this computer.

1. **Find where the error comes from before changing anything.** For each photograph already measured against a scan of the same sheet
   (the four Fold 7 captures of 2026-09-29 and the 2026-09-26 picture against the dominus-k 600 dpi scan; the seven corpus photographs;
   the mounted-photograph frames of the Phase 1 gate), split each bull's center error into its parts: the code positions, the lens fit,
   the registration, and the bent-sheet model. Say which part carries the worst bulls (row 1: 0.0155 to 0.0216 in, gate 0.005 in) and
   whether the worst bulls sit at the far edge, near the codes, or at random. Write it in PHASE1-RESULTS.md before section 2 starts.
2. **Photographs at an angle (row 1).** Improve the part section 1 names. Measure with `grouplab compare-photos` before and after.
3. **The bent sheet (row 2: 0 of 7 frames meet the mounted-photograph gate).** The checklist says the model needs a change before more
   material helps. Make the change section 1 points to, keeping question 44's bounded step, and measure on the same 7 frames.
4. **Holes ringed as two shots (row 4: 7 clean single holes ringed at 1.4 to 3.1 holes' area) and the 2 false marks of row 3.** Reduce
   both without losing a real double: `RoundsFiredOnARenderedSheetTests` and any rendered doubles must still be found.
5. **Rules for every change in sections 2 to 4:**
   1. **No tuning to the answer.** Choose settings on part of the photographs and report the result on the rest (say which is which),
      so a gain is not just fitting these pictures. If there are too few to split, say so and treat the result as provisional.
   2. **Scans must not change:** every hole center and figure on the scan corpus identical to before (or within a stated tolerance far
      below 0.001 in, with the reason), by the regression tests and a before and after comparison.
   3. A change that helps one photograph and hurts another is not kept unless the worst case improves; record what was tried, including
      what did not help.
   4. The learning loop's scoreboard on the server must not read worse once this ships; if a change alters what it scores, say so in
      the report so the next nightly check is read with that in mind.
   5. Bench gate before and after; no stage more than the gate allows slower.
6. **If a gate cannot be met with the material here**, stop at the honest improvement, say in PROOF-CHECKLIST.md what material would
   settle it, and write a request in for-alan.md only if a new photograph from Alan is the only way forward (with the exact shot to take).
7. Update PROOF-CHECKLIST.md rows 1 to 4 with the new numbers, add a "Worth an article?" decision in RESEARCH.md, and a plain
   Release-note in a shooter's words (for example how much closer GroupLab now gets on a photo taken at an angle).

## 2026-10-09, entry 398: request 91 answered: today's server backup is there and a restore of it passed

**Status: actioned 2026-10-09; every section done.** The restore test is recorded in `docs/RESTORE.md` with the new schedule (one backup a day kept 4 days) and what four days means for the standing rules; request 91 is archived as answered and its steps, and RESTORE.md's, say "Restore boot volume"; question 93 is answered (the packages) and archived; entry 395's server half goes on under entry 397.


From the planning session, 2026-10-09 05:15 Denver. Alan did request 91 in the Oracle console (US West, San Jose), with screenshots:

1. **Newest backup:** "Auto-backup for instance-20260324-2036 (Boot Volume) via policy: grouplab-daily on 2026-10-09 09:00:00",
   created 2026-10-09 09:02:44 UTC, incremental, 2 GB of a 47 GB volume, **Available**, expiring 2026-10-22 (13 days' retention). Thirteen
   backups in all, every one Available, the full ones on 2026-09-27 and 2026-10-04.
2. **Restore test passed:** Restore boot volume from that backup as `restore-test`, same availability domain, default size, no backup
   policy: it came up **Available at 47 GB** (created 11:12:01 UTC), and was then terminated; the console shows it **Terminated**. The
   server's own boot volume stayed Available throughout and was not touched.
3. Record the test in `docs/RESTORE.md` (date, backup used, result, how it was done, that the restored volume was deleted), close
   request 91, and carry on with entry 395's server half under entry 397. Note for next time: in the console the action is named
   "Restore boot volume", not "Create Boot Volume"; fix the wording wherever request 91's steps are kept for reuse.
4. **Question 93** (the server's OpenCV): Alan's answer comes with the paste line that starts this run; act on it and archive the question.
5. Seen in passing, nothing to do: only the five oldest backups carry Oracle's "Always free" label. Alan is deciding whether to keep 13
   days of backups or cut to 5; nothing changes until he says.
6. **Backup retention, Alan, 2026-10-09 05:14 Denver: "Cut to 5 days to stay free."** Oracle's Always Free tier covers five volume
   backups. Alan is changing the `grouplab-daily` policy in the console himself (Code cannot see it): one daily backup kept 4 days, so at
   most five exist even in the minutes when a new one lands before the oldest expires, and the weekly full schedule removed if there is
   one. The backups already taken keep their 13 day expiry and are left to expire by themselves by 2026-10-22, not deleted. Update
   `docs/RESTORE.md` (how often, how long kept, where) and anything else that says 13 days, and note what it means for CLAUDE.md's rule 2:
   a server mistake has to be noticed within about four days to be undone from the whole-server backup; Code's own dated copies of each
   changed file are unaffected.

## 2026-10-09, entry 397: a backup of everything changed on the web server, before it is changed (Alan)

**Status: actioned 2026-10-09; the rule and its tools are in place, and section 1 stops the server changes until request 91.** Sections 2 to 5 are `docs/RESTORE.md`'s standing rule 5 and `website/server/grouplab-change-backup.py`, which copies what a change will touch (files with mode and owner, absent files listed for the undo, the package and unit lists, crontabs) into `/home/ubuntu/grouplab-server/backups/<date>-<label>/`. **Section 1:** Code cannot see the Oracle console, and no restore test of the boot volume backups has ever been recorded, so entry 395's server changes wait on request 91. Section 6 applies to entry 395's report.

From the planning session, 2026-10-09 04:00 Denver. Alan, before allowing ssh and sudo for entry 395: "Can you tell code that it needs to
make a backup of anything it changes on the web server?" This restates CLAUDE.md's first rule for the server work of entry 395 and every
later server change, and makes it checkable. Take it before any server change in entry 395.

1. **Before the first change of a session:** confirm the whole-server backup (Oracle boot volume policy `grouplab-daily`) has a copy
   less than a day old; if it is older, or its last restore test is not recorded as passed, stop the server changes and say so in a
   request rather than going on.
2. **Before each change, a copy of exactly what will change**, dated, outside the HestiaCP `conf/web/` folders (in
   `/home/ubuntu/grouplab-server/backups/<date>/` or `/home/airwolf/backups/grouplab.org/config/`):
   - every file that will be edited or replaced (systemd units, configuration, scripts, the worker's own files), copied with its
     permissions and owner;
   - before any package install or removal, the installed package list (`dpkg --get-selections`) and `apt-mark showmanual`, so the
     packages can be put back exactly;
   - before enabling or changing a service or timer, `systemctl list-unit-files` and `systemctl list-timers --all` as they were;
   - any crontab, user or group that will change, as it was.
3. **An undo script for each change**, beside the backup, that puts back the files, removes what was added (the four packages, the
   worker's unit and timer, its folders) and restarts nothing belonging to pissinhot.com. Run it once against a dry run (`--dry-run`,
   listing what it would do) and record the output in `panel.md`.
4. **`docs/RESTORE.md`** gets one entry per change: what changed, where its backup is, the undo script's path, and how to check the
   undo worked. Never the server's address or the key's path in any of it.
5. After each change, both sites are checked (grouplab.org and pissinhot.com answer as before), and `nginx -t` passes if nginx was
   touched at all.
6. The report for entry 395 lists each backup taken, with its folder and size, and confirms the undo dry runs.

## 2026-10-09, entry 396: request 89 answered (b): Code reads a tuning pull request's table and merges it

**Status: actioned 2026-10-09; every section done.** Request 89 answered (b) and archived; the rule is in docs/DETECTION-LEARNING-STUDY.md section 10 and in the pull request `learn.py` opens; it belongs in CLAUDE.md, so request 90 asks Alan to add it.

From the planning session, 2026-10-09 03:25 Denver. Alan, asked who merges the tuning job's pull request of better detection settings
(request 89: (a) automatically, (b) Code reads its short table and merges it, (c) it waits for Alan): "b".

1. When the tuning job opens a pull request, Code reads only its before and after table and the checks' results, never the pictures,
   and merges it when every test and gate passes and the table is better on the held-out share and worse on nothing. Anything odd (a
   setting at the edge of its search range, a large jump, a line that improved for no reason the table explains) is not merged: Code says
   why in the pull request and in its report, and the pull request waits for planning.
2. The merge commit carries a plain Release-note naming what got better, in a shooter's words.
3. Close request 89 as answered (b), 2026-10-09. Record the rule in DETECTION-LEARNING-STUDY.md beside the loop; if it belongs in
   CLAUDE.md, put the line into for-alan.md for Alan to add, as with request 87.
4. Spend: a few thousand tokens per pull request; reading it is a chore only if it cannot change the app, so it stays on the main model.

## 2026-10-09, entry 395: the learning loop moves to the web server (Alan: it is free)

**Status: actioned 2026-10-09; every section done.** Section 1: no OpenCV build was needed (OpenCvSharp's own arm64 runtime, question 93 answered: the packages); the server is Ubuntu 24.04 on arm64, and the nightly publishes the signed linux-arm64 command line since nightly 183. Sections 2 to 4: the worker, its three capped units and its self-update are installed on the server over entry 397's backups (RESTORE.md lists both changes and their undo), and the first nightly check ran there. Section 5: request 88 is ready for Alan. Section 6: one 600 dpi scan 39 s and 1.1 GB, one phone photo 21 s and 351 MB; the nightly check on today's 42 submissions 13 minutes 20 seconds; both sites answered throughout. The first nightly run passed the 1.5 GB cap reading ten submissions in one process, so the nightly now reads one a process and goes on past one it cannot read. Section 7: the study and RESTORE.md. Earlier: the permission check refused ssh until Alan allowed it on 2026-10-09.

From the planning session with Alan, 2026-10-09 03:10 Denver. Told that entry 394's Actions route costs nothing at today's rate (about
550 of the 2,000 free private minutes a month, a $0 spending limit, so it would stop rather than bill) but would pass the allowance as
submissions grow, Alan: "If so, I would rather use my webserver because it is free." So the server is the home of the learning loop; the
Actions workflow stays in the archive repository, disabled, as the fallback. Main model; CLAUDE.md's backup and sudo rules apply to every
server step (what will change and how to undo it written down first, `docs/RESTORE.md` updated, nothing of pissinhot.com touched, the key
never read, the address never written).

1. **linux-arm64 for the command line** (DETECTION-LEARNING-STUDY.md section 6, "Arm64"): build OpenCV's native library for linux-arm64
   once in the public `grouplab` repository's CI on GitHub's arm64 runners (confirm they cost nothing for a public repository before
   relying on it), publish it as an asset, and make the nightly produce a linux-arm64 `grouplab` command line the server can fetch and
   verify the way the updater verifies builds. If the server turns out not to be arm64, say so and use the x64 build instead.
2. **One more worker on the server**, between intake and archive as section 6 describes, under systemd with the caps it names
   (CPUQuota 50%, MemoryMax about 1.5 GB, Nice, IO weight; there is no swap, so the memory cap is not optional). It scores each submission
   with `grouplab learn score` before the archive worker files and deletes it, so the server never keeps a picture longer than today.
3. **The nightly check and the tuning run on the server at night**, niced and capped, each with a time limit. Pictures they need are
   fetched from the private archive into a temporary folder for the run and deleted when it ends (Alan's 2026-09-25 retention rule:
   nothing left on the server longer than needed). Results, numbers only, go to the archive repository as today (`learning/summary.md`).
4. **The worker fetches the newest nightly's command line by itself** after each nightly, checks its signature, and keeps the previous one
   to fall back to, so no person updates it.
5. **Request 88 (the token)** changes: the token goes on the server, typed by Alan, never into a file in a repository. Rewrite request 88
   with the exact steps for the server (MobaXterm, which shell, the command, what a good result looks like), and put the command in the
   panel per CLAUDE.md. Until then the loop scores and summarises without filing issues or pull requests, as now.
6. Measure on the server: seconds and peak memory to score one 600 dpi scan and one phone photo, and how long the nightly check takes on
   today's submissions; record it, and confirm both sites stayed responsive while it ran.
7. Update DETECTION-LEARNING-STUDY.md, RESTORE.md and any site or README sentence that says where the loop runs.

