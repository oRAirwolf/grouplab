# Phase 1 results, entries 426 to 450

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 425: the selected shot made impossible to miss (2026-10-15)

From a person's report passed on by Alan in the planning chat (nightly 192, macOS, dark theme, a pooled set of three Letter sheets, 27
shots); no issue was filed, most likely because the Discord worker was stopped (entry 427). Its text was data; nothing in it read as an
instruction. The planning session's findings were checked: the selection was `Tokens.MarkSelected`, #e0912f, which is the dark theme's
first sheet color in a pooled set; on the plot a picked shot was drawn in `PlotInks.Accent`, the extreme spread's own red, so a picked end
of the spread line vanished into it (the committed `analysis-light` picture shows exactly that); the small sheet grew the dot from 2.4 to
3.2 pixels; and the table row only took the amber tint.

1. **The mark** (`Marks.Selection`, `Tokens.Selection`): a ring standing 6 pixels outside the shot (outside the caliber outline on the
   plot) and a crosshair through its centre reaching 8 pixels past the ring, white edged in black on the dark and high contrast themes,
   black edged in white on the light, drawn after every other mark and line on the picture (`MarkingCanvas`), the plot (`CompositePlot`,
   after the band labels; the crosshair runs in to the dot) and the small sheet (`SheetThumbnail`, a ring of 9 pixels, several times a
   2.4 pixel shot). A picked shot keeps its own sheet's or mark's color beneath it. The plot's key adds "the shot you picked, ringed with
   a cross". `MarkSelected` stays the orange of a lit bull, a lasso, the review queue and the printer wizard's dots, none of them a
   selection of a shot.
2. **The row** (`AppStyles.SelectedRow`, `Tokens.SelectedRow`): an amber bar 4 pixels wide down its left edge, a fill a fifth of the way
   from the shaded row toward the text (brighter on dark, darker on light), and bold cells at full text contrast. A shot chosen on the
   plot or the picture brings its row into view once, when the choice changes, so a panel scrolled elsewhere is not pulled back on every
   redraw.
3. **Both themes, both platforms**: the desktop in dark, light and high contrast. The phone has no selected shot: it draws no selection
   on its picture (its amber rings are marks to check, which stay orange per section 1.4) and its plot is the shared `CompositePlot`,
   so a selection there would take the same mark; nothing else on the phone changed.
4. **Tests** (`Entry425Tests`, 9): the selection's ink is 100 or more in RGB from every pooled sheet color, the bull, the extreme spread,
   CEP and the center's green, the aim's blue and the old orange, in all three themes, and its edge is the opposite of it; the selected row
   is brighter (dark) or darker (light) than the background, panel and shaded row by 1.4 to 1 or more with text at 7 to 1 (high contrast
   at its own ratio); selecting a shot on the small sheet changes 250 or more pixels in each theme; a picked shot marks its row alone,
   bold, and the key says what the ring is. App 571, Mobile 179, Core cross-cutting 37 passed.
5. **Finishing**: the screenshot walk now pictures the analysis with one end of the extreme spread picked, so the next screenshot run
   after the nightly replaces `analysis` and `analysis-open` in both themes (checked locally in `out/screens`). The tour's analysis page
   said "The widest pair is picked out in bold", which was not true (nothing picks it until its line is clicked); it now describes the
   picked row and the plot's ring, and the user guide's analysis section says the same. Release note kind `fixed`, claimed on the
   Features page under the analysis screen. No issue to close: the report will be filed when request 100 runs, and is then closed with
   this commit.

**Question 99** records that the dark theme's white is also a single sheet's plot ink and nearly the placed marks' #e6e8ea, so there the
ring and crosshair, not the color, tell the selection apart; built as the entry says meanwhile.

**After it, its CI (f65147e8 and e52cd430):** red on macOS and Windows, neither from the selection. Twice on macOS and once on Windows
(e8aa367a), a phone test changing the text size stopped inside Avalonia 12.1.2 with "UnsetValueType to IBrush" while it re-evaluated
every control for the new styles, and the tests after it ran at the largest size. It never happened alone or in four local runs of the
suite. e52cd430 took the new styles out and added them again on that failure; on its own CI run the removal stopped in the same way, so
the fault sits on one control and is not passing, and that change is taken back (its release note corrected in
`docs/release-note-corrections.json`). What the phone tests did leave: nine tests opened windows they never closed, four of them the
whole phone (`SharedScreensTests`, one after pressing every button on its result), and every later change of text size restyled them
too. They are closed now (`SharedScreensTests.CloseStarted`, `IosWideScreenTests.CloseOpened`, `Entry295Tests`). Whether that was the
cause is not proven: it cannot be reproduced here, so the next runs will say. And `SolverWarmUpTests` failed on Windows at 1033 ms
against its 600 ms bound, the warm-up thread starting late on a busy runner; it now holds what it meant, that the flights run on a
thread that is not the caller's (`SolverUse.WarmedOnThread`), not how long a thread took to start.

## Entry 426: request 98's paste named a commit that is not on GitHub (2026-10-15)

1. **Request 98** is answered (Alan ran it on 14 October with the planning session's pastes at 77b3db39, entry 427) and moved whole to
   `docs/notes/for-alan-archive.md`; its paste there now names 77b3db39, the commit Alan ran, instead of e959956a, which is on no branch.
   Open requests: 23.
2. **The test**: `ForAlanTests.EveryPasteFetchesFromACommitOnMain` reads every `raw.githubusercontent.com/oRAirwolf/grouplab/<commit>/`
   in `for-alan.md` and `for-alan-archive.md` and fails on a commit that is not on the history of what is checked out (`git merge-base
   --is-ancestor`), which on CI is main. CI's test job was a one-commit checkout, so it now fetches main's history first (commits and
   trees only, no tags, so `ReleaseNotesTests` and `VersionTests` see the checkout as before); a shallow checkout on CI fails the test
   rather than checking nothing. Before the change e959956a was the only stranded commit; request 97's c95280d0 is on main.
3. No release note: nothing here ships.

**Just before this entry, not from it (e8aa367a):** build and test was red on 77b3db39 (all three systems) and 800e1d93 (Windows). Entry 424's
report sentence joins the distance and rounding to the first sentence of the screen's line and keeps "what to leave and why" after it, as
the entry meant; `RealSessionProofTests.AlansOwnSessionsReportGivesTheScreensZeroVerdict` still held question 91's older form and now
holds this one (passes on the 2026-09-26 6 ARC scans). The Windows failures were `PrinterConnectTests`: each blocking printer connect
ran on the shared pool, so on a busy runner a connect that answers at once never started inside its 200 ms; each now has its own thread
(`PrinterConnect.FirstThatConnects`), and one that finished as the limit fired counts as connected.

## Entry 427: the Discord worker installed, and its crash on an attachment fixed (2026-10-15)

0. **Recorded**: request 98 is answered and archived (entry 426); the install of 2026-10-15 04:16 UTC and its backup
   `2026-10-15T041559Z-discord-worker` (40,264 bytes, the four installed paths absent before) are in `docs/RESTORE.md` with the undo.
1. **The cause, measured rather than guessed**: from this machine, Cloudflare in front of Discord's attachment store answered 403 to
   urllib's own User-Agent for every file, a public default avatar included, and 200 for the avatar with the bot's User-Agent; an
   attachment link it does not know answered 404. So the worker's attachment fetch was refused, `request(..., raw=True)` returned
   `(403, None)` and `keep_attachments` wrote None. **The fix** (`website/server/grouplab-discord-worker.py`, a3ea5ecf): attachments
   are fetched with the bot's User-Agent; a 401, 403, 404 or 410 reads the message again once for fresh signed links and tries again;
   anything else is "could not be fetched (HTTP <code>)" in the issue; a half-kept `.partial-` folder from an earlier stop is cleared;
   any other failure inside one thread is logged as "thread <id>: not filed this run (<kind> at line <n>)", never the post's words,
   and the run takes the next thread. `tests/python/discord-worker-tests.py`: 23 passed, with a 403, a 404, an answer with no body, an
   expired link refreshed from its message, a thread that throws ahead of one that is still filed, and urllib's own agent refused as
   Cloudflare does; against the old worker the first run fails as the server's did.
2. **The paste** is request 100 in `for-alan.md` and at the top of `panel.md`: fetch the six server files at a3ea5ecf (on main), check
   their SHA-256 (five unchanged from request 98), `grouplab-change-backup.py --label discord-worker-fix --file
   /usr/local/sbin/grouplab-discord-worker.py`, `install.py --discord`, run the service once, tail the log; every step prints on
   failure. Waiting for Alan.
3. **The issues it filed**: #29 (a marking file's absolute photo path, nightly 174) was fixed by 3e59e4b5, entry 383, first in nightly
   176: the photo is named by file name only, found beside the marking first, and asked for when missing. #30 (the red cut-off text
   that stayed after switching back to a sheet printer, nightly 174) was fixed by e740a25a, entry 409, first in nightly 185. Both
   closed with those commits. Nothing in either read as an instruction. The report behind entry 425 is not filed yet; it will be once
   request 100 is run, and entry 425's fix closes it.
4. No application release note: nothing here ships.

## Entry 428: request 100 recorded, and the four posts the Discord worker filed (2026-10-15)

1. **Request 100 answered** (Alan, 08:56 UTC, a3ea5ecf): six `OK`, the units and token script unchanged, the token "already set, not
   read or printed", the timer started, no Traceback. Recorded in `docs/RESTORE.md` with its backup
   `2026-10-15T085633Z-discord-worker-fix` (the old worker, 16,038 bytes) and an undo; the request is archived. `install.py`'s own
   dated `.bak` beside the worker is tidied already: `keep_newest_backup` removes the older copies of the same file at each install, so
   one is kept per file, and `backup_path` never puts one in a HestiaCP folder. Nothing in `install.py` changed.
2. **The four issues it filed**, all from Quack, none of their text an instruction:
   - #33 (nightly 192, the selected shot hard to see in a pooled set, dark theme): the report behind entry 425; closed with f65147e8,
     nightly 193. Entry 430 then makes the ring magenta.
   - #32 (nightly 190, holes a quarter of a hole off on the 556 x3 scans, the snap the same, Select not dragging): entry 417's report;
     fixed by 1392641a, first in nightly 191; closed.
   - #31 (the upload page: no progress, too few files for a three-sheet set): entry 418's report; fixed by afc61240, live on
     grouplab.org since 2026-10-13; closed.
   - #34 (nightly 192, the tab strip's pop-up horizontal scrollbar covers the tabs' names and close buttons when many tabs are open):
     **new and not fixed**; left open for the planning session.
3. Question 99 is answered by entry 430; nothing was built for it here.
4. No application release note: nothing here ships.

## Entry 429: Discord attachments on their GitHub issues (2026-10-15)

1. **The consent** is in `samples/PROVENANCE.md` with Alan's words and the date: attachments in the Discord's bug-reports forum may go
   on their GitHub issues in `oRAirwolf/grouplab-crash-reports` without asking the poster; grouplab.org, public test data and research
   articles are not covered and stay as they were. Nothing already in PROVENANCE.md reads against it, so no question.
2. **Not done.** The worker change (each kept file committed under `discord/<thread>/<message>/` through the contents API and shown on
   its issue, a refusal as a line in the issue, a once-only backfill of kept folders, the issue wording, tests) was refused by Claude
   Code's own permission check as handling posters' personal data, so it was not written; nothing on the server or GitHub changed.
   Request 101 asks Alan for his word in the session. Found while preparing it: the token the worker uses (`grouplab-error-reports`,
   entry 194) was specified as Issues: Read and write only, so committing files will very likely also need Contents: Read and write
   added to that token; the worker would say so in its log rather than assume. The pull to this computer has never run, so the six
   issues' kept attachments are still on the server, and a backfill has to run there.
3. No application release note: nothing ships.

**Section 2, done later the same day** on Alan's word in the session ("Go ahead with entry 429 section 2", request 101 answered), with
his note that the `grouplab-error-reports` token now has Contents: Read and write on the crash-reports repository.

1. **The worker** (`put_on_issue`, `attachment_line` in `website/server/grouplab-discord-worker.py`): each kept attachment is also
   committed through the contents API to `discord/<thread>/<message>/<id>-<name>` on `main` and shown on its issue or comment, a picture
   inline and any other file as a link. A file already there from a run that stopped (422) is read back and counts as on the issue. A
   refusal, a server error or no answer is a line saying so ("kept privately for diagnosis; it could not be put on this issue (GitHub
   answered 403)"), never an exception, so it stops no other file and no other thread. The private server copy, its manifest and the pull
   to `C:\Dev\grouplab-discord` are unchanged.
2. **The wording**: the issue now says the attachments are on it with a private copy kept, and that grouplab.org, public test data or a
   research article still needs the poster's consent (`ATTACHMENTS_LINE`); the worker's docstring and `Get-DiscordAttachments.ps1` say
   the same.
3. **The backfill** ran once from this computer, not the server: the pull of 15 October had already copied all six kept folders here and
   removed them from the server. A one-off script imported the worker's own `put_on_issue` and `attachment_line`, checked each file
   against its manifest's SHA-256, committed it, and replaced its "kept privately" line in place: #30 (one comment), #31 (the post and one
   comment), #32, #33 and #34, six pictures. All six issues #29 to #34 had their "never published" line replaced. Their text before is in
   `C:\Dev\grouplab-local\discord-issues-29-34-before-entry-429-2026-10-15.json` (the weekly restore test last failed, so a dated copy
   came first); today's 09:39 nightly backup also holds it. #34's rendered body shows the picture as an image tag on the issue. The
   backfill used Alan's own GitHub login on this computer, so the server's token has not yet been shown to write contents.
4. **Tests** (`tests/python/discord-worker-tests.py`, 30 passed, run in CI): the picture is committed whole in its thread's and
   message's folder and shown; a 403 and a 502 from GitHub are lines in the issue with the file still kept privately; an already committed
   file is shown; a text file is a link; and a thread after the failures is still filed.
5. **The server** is request 102, one paste in request 100's form from this commit on `main`: fetch and check the six files, back up the
   worker, install, run once, then prove the token can write contents by committing `discord/check/entry-429.txt` (OK or STOP with
   GitHub's answer). `docs/RESTORE.md`'s rows for the crash-reports files and the Discord copies now name `discord/`.
   **Run by Alan on 2026-10-16 at 07:45 UTC**: six files `OK`, backed up, installed, `OK: the token can put attachments on issues`
   (the check file is commit 26e83e9f in `grouplab-crash-reports`), and the worker's log clean; recorded in `docs/RESTORE.md`.

## Entry 430: the selected shot in magenta (question 99 answered) (2026-10-15)

1. **`Tokens.Selection`** is #ff3df5 edged in black in the dark themes (high contrast included) and #b0009f edged in white in the
   light; ring, crosshair, widths, gap, arm and the small sheet's ring are unchanged.
2. **The phone** draws its plot with the same `CompositePlot` and tokens, on Android and the iPhone alike, so it takes the magenta with no
   change of its own; it has no selected shot to show yet (entry 425), so nothing there looks different today.
3. **The test** (`Entry425Tests`, 12 passed) now also holds the selection apart from the plot's own ink (`PlotInks.Ink`) and
   `MarkPlaced` in every theme, still from every sheet, bull and overlay color, and its edge is the opposite of it (at least 6 to 1;
   7.2 dark, 6.3 light). Too close, as section 3 foresaw: dark #ff3df5 is 84 from bull mark #b24bd6; light #b0009f is 68 from the
   fourth pooled sheet's plum #9c3f8f, 88 from #d62a5b and 93 from #b24bd6. Kept as Alan chose; question 100 has the numbers and
   three options, and the test holds the plum pair at 65 and the bull marks at 80 until it is answered.
4. The selected row's amber bar is unchanged.
5. **Question 99** is answered (b) and archived. The tour's key line and the user guide now say magenta; the Features entry for the
   analysis claims the new note. `analysis-dark.png` and `analysis-light.png` already show a picked shot (entry 425's walk) and are
   reshot by `screenshots.yml`, which this push starts.
6. Release note, kind `changed`, in the commit.

## Entry 431: the tab strip's scrollbar in a row of its own (issue #34) (2026-10-15)

1. **`TabScrollBar`** (`src/GroupLab.App/TabScrollBar.cs`): an 8 px bar 4 px under the tabs, drawn from the theme's own colours (the
   track in `Sunk`, the thumb in `Faint`, `Dim` while the pointer is on the strip or the bar or the thumb is held). It is there only
   while the tabs overflow (`IsVisible` follows the strip's extent and viewport), so a window whose tabs fit looks as it did. Dragging the
   thumb scrolls the strip; pressing the track beside it moves a page. The strip's own `ScrollViewer` keeps scrolling by wheel and
   touchpad with its Fluent bar hidden, which is the bar that was drawn over the lower half of the tabs. The tabs are unchanged.
2. **A tab chosen or opened is scrolled into view whole**, close button and all, once the row has its new width; only when the chosen
   tab changes, so scrolling by hand to look along the row is not undone by the next refresh.
3. **The test** `TargetTabsTests.TheTabStripsScrollbarHasARowOfItsOwnAndCoversNoTab` (15 tabs in a 900 px window): no tab and no close
   button intersects the bar's bounds, before and after the pointer moves over the strip; the strip's height does not change; with one
   tab the row takes no space; the last tab's close button lies inside the strip's viewport; pressing the track scrolls back. The tab
   tests (11), the theme, sweep and bench tests (78 in all) and the Features, shipping and guide checks pass.
4. **The phone** has no tab strip, and none of its screens has a horizontal pop-up bar over something it can be tapped on.
5. **Screenshots**: the tour's and Features' marking pictures show one tab, so the strip looks as before and nothing is reshot; the
   tour's words on the tabs are still true. The user guide's paragraph on several targets now says a thin bar under the tabs scrolls
   them when they do not fit; its PDF follows with the nightly's guide run. `target-tabs` in `website/features.json` claims the note.
6. Issue #34 is closed with the commit and nightly 195.

## Entry 432: the iOS workflow's red steps (2026-10-16)

1. **"The iPad and landscape" was a check wrong about the app.** It first failed on run 37416825125 (e0c3277f, 2026-10-06), not
   2026-10-11; it last passed on 7dfc3691. Every tour stopped after the Settings screen and sat "running" until its 400 s limit: the
   app logged `capture.ask calibre=False` as the sample was handed over, and nothing answered. Entry 376 item B3 (5df65754, between those
   two commits) made GroupLab ask for the caliber of every new target and empty it after each result, on Alan's word; the self-test still
   relied on the caliber it saves before the start, which the app now never reads. The full self-test had the same fault (paste, chosen
   picture, cancel while reading, open in, shared, the parity tour and the data round trip all FAILED in `ios-results.json`), unseen
   because the Compare step that judges them never ran after the iPad step failed. Nothing in the app was wrong on an iPad or in landscape.
2. **The fix is to the check**: while the self-test or the tour runs, the question is answered as a person answers it, the sample's
   caliber typed into the box and Continue pressed, each time it shows (`AnswerCalibreWhile` in `ios/GroupLab.iOS/SelfTest.cs`). A new
   check, "caliber asked", passes only where the question was asked at least once, so B3 is now held on iOS too. Nothing was skipped,
   deleted or loosened. The iOS project builds only on the Mac runners, so this is proven by the run in section 4.
   **Run 38873299212 (25c20598) then found a real fault**: "The iPad and landscape" and Real taps passed, and the Compare step, which had
   not run since 2026-10-05, failed on the second picture onwards (chosen, cancel while reading, open in, shared, and the parity tour and
   data round trip after them). The question sits on the Capture start; after the first picture's result the Capture screen shows the
   result, so a second picture shared or opened into GroupLab raised the question where nobody could see it and was never read, on iPhone,
   iPad and Android alike, since entry 376 B3. Fixed in the app: the start comes back with the question, the result staying open in its
   tab (`AskFirst` in `CapturePage`); `Entry432Tests` fails on the old page and passes on the new. Release-note kind `fixed`.
   **Run 38878915397 (2776a941)** then passed every picture check, the iPad and landscape and Real taps; Compare failed only on "data
   round trip: no data file was written", although the log shows the export written (426,038 bytes) and the share sheet open. Since
   entry 377 (1fb8f104, 2026-10-05) the phone writes the export in the cache's `shared` folder, and the check looked only in the cache
   itself: another check wrong about the app, hidden behind the iPad step. It now looks where the app writes (`DataSelfTest`).
4. **Green:** ios-app.yml run 38883488382 on a2a0345a passed every step, the self-test, the iPad and landscape, Compare and Real taps.
   Real taps' Next on Targets passed there too; it fails on some runs only, and the new log lines will say where when it next does.
3. **"Real taps" fails at two different taps**, each on some runs only (18 red runs read):
   - **Step 23, closing the photo picker** (12 runs). The simulator has no camera, so Take a picture opens the picker in its place. On a
     slow runner the app asked for it 2.8 s after the camera's place showed (run 38513312105: `camera.none` 21.385, `ios.pick` 24.161),
     past the close step's 2 s quiet time, and the close step read the log's "picker opened" in the moment before the picker was on
     screen and failed at once; every later step failed after it. Fixed in the step (`Close` in `mobile/GroupLab.Mobile/Dev/Scenario.cs`):
     a picker asked for and not yet seen is waited for and closed within the step's own 15 s; one still unseen then fails as before.
     `PhoneTouchScenarioTests.CloseWaitsForAPickerStillOnItsWay` brings the picker after the quiet time: it fails on the old step and
     passes on the new one. Not a retry: the step now waits for the thing it closes.
   - **Steps 5 to 12, Next on the keyboard on Targets** (5 runs). The first press on Next after a tap into a field is lost and each later
     press moves the focus one field, so every check is one field behind (run 38705763402). The runner delivered the press at the right
     place (08:48:48.68), the hold had found Next settled and reachable there, exactly as in a passing run (38740221248), and the app
     logged nothing: no focus change and no keyboard closing. Avalonia 12.1.2's iOS touch handling and its text responder were read and
     neither takes the press. The cause is not found yet, so the press, the release and Next's move of the focus are now each written to
     the log (`keyboard.bar`, `keyboard.next` in `KeyboardRoom`), and the next failing run says which one stops. No retry was put round it.
5. Nothing needed Alan.

## Entry 433: finding the holes faster (2026-10-16)

1. **Measured first**, from one Release build, with timers put into `RenderDifferenceHoleDetector.Detect` and taken out after, on the
   16 corpus photographs the four local scoreboards read and the 600 dpi sample with 25 shots (docs/PERFORMANCE.md, "Entry 433"). All of
   finding the holes: 498 ms median on a photograph (125 to 1726), 1238 on the scan. The largest step is now the marks one by one (132
   and 344, most of it each mark's moments), then the close (44 and 202), then the alignment (52 and 159); the close is 3 to 437 ms on a
   photograph, more than the 3 to 14 entry 410 saw on its pictures; why it grew was not looked into.
2. **The close** now works only the columns that can change (`BinaryMorphology.Pass`): an erosion from each row's first set pixel to its
   last, a dilation within a half width of another row's set pixels. On the scan 202 ms to 102, and hole finding about 1238 to 1133; on a
   photograph the median close 44 to 42, the slowest 437 to 322. **The phase correlations were not changed:** OpenCV already pads each
   cell to its best transform size and the cells already run at once, so what is left is the transforms, about 33 ms of work a cell at
   600 dpi, and a different transform gives different shifts. No QR detector runs on several threads.
3. **Nothing moved:** the four corpus scoreboards, the synthetic scoreboard, `corpus counts` (committed and local) and the identify sweep
   are identical before and after but for their time columns. `BinaryMorphologyTests` holds the close to OpenCV's own on six new wide
   pictures with marks inside them as well as the ten before; a stretch cut too short fails 16 of 18. The baseline gate passed.
   **Found while checking, not changed:** `grouplab corpus counts` reports that its committed record differs from main's own output on
   29 of 55 images (and the local manifest's likewise), before this change as after it; the record has not been rewritten since
   detection changed in entries 415 and 417. Rewriting it with `--write` is a review of what changed, left to the planning session.
4. **Written down** under its own heading in docs/PERFORMANCE.md, before and after. Release-note kind `internal`: a 600 dpi scan's holes
   are found about a tenth sooner, and no person would notice.
5. **Worth an article?** Not written (docs/RESEARCH.md): ordinary engineering a shooter never sees.

## Entry 434: the four false hole marks left after entry 415 (2026-10-16)

1. **What makes each of the four**, from the mark, the expected print and the residual around it (a scratch test, deleted after, running
   each corpus picture exactly as `grouplab scoreboard --corpus` does):
   - **diamond-22lr, two marks centred on a diamond's white window** (bulls at about (1.36, 8.40) and (2.81, 8.42) in): the window's
     edges sit unevenly off the render where the crinkle stretched it, and the aiming dot inside it shows as a crescent; the close joins
     the bands and the crescent into one ring of residue (hull solidity 0.50 and 0.46, about 0.26 in). One shift of the print cannot
     explain a stretch, which is why entry 415's moved-print check lets them through.
   - **diamond-22lr, one mark at (1.47, 3.03) in**: a real hole beside a diamond's edge, whose crease shadow lies along that edge. The two
     closed into one long mark (elongation 2.08, about two calibres of area); the split put one centre on the hole, which is found, and
     the other on the shadow, which is the false mark.
   - **dominus-k, one mark at (7.83, 10.56) in**, below the frame: faint residue on the tape's edge, 0.126 in across, a quarter of a
     .243 calibre's area, just over the size floor.
2. **The candidate tried, measured before any change:** a hole's residual is a step all round its rim, a shadow's or a band's is not.
   For every mark on the nine pictures (222 real holes and the four), the weakest of eight sectors' step from inside the mark to a ring
   just outside it. **It separates nothing:** real holes step as weakly as the false marks (the weakest real holes -1.54, -1.01, -0.80);
   two false marks lie among 5 and 16 real holes, one beyond all but one; only the split's far half (-3.14) is below every real hole,
   and a line placed for one half of one split on one sheet is the setting the entry rules out. Nine photographs are too few to tune on.
   Entry 410's rule about faint marks hugging a printed edge was not tried again.
3. **Nothing was changed in detection, so no hole could be lost**; the corpus, rendered sheets, scans, range pictures, scoreboards and
   the learning loop are as main left them (entry 433's checks of 2026-10-16: every scoreboard identical).
4. **Closed by section 4:** nothing honest removes them; this is what was tried and why it failed.
5. **Proof checklist row 3** still says 4 false marks and now names each one's cause. No release note a person would see. **Worth an
   article?** Not written (docs/RESEARCH.md).

## Entry 435: consistency audit of 2026-10-16 (2026-10-16)

1. **The user guide's PDF** is made again from the guide (`grouplab user-guide`), so it has the magenta ring on the selected shot and the
   tab strip's scrollbar in a row of its own; the testing guide's PDF was remade with it (its words are unchanged, its pictures newer).
   **Why the check of 2026-10-07 did not stop it:** `guide_pdfs` in `scripts/consistency.py` did find it, but CI runs that script with
   `--warn`, so it was only ever a warning. It now asks git whether the guide has a commit after its PDF's last one (a rebase cannot
   reorder that, as it can commit times), and a new CI step, "The guide PDFs are made from their guides", runs it with `--guides`, which
   fails. Before the PDFs were remade it failed naming the three commits.
2. **The download page** says each of the newest thirty builds keeps a release, older ones are removed with their tags, and their notes
   stay on the Release notes page (`website/build.py`).
3. **The README's support link** line says Support GroupLab in Settings opens grouplab.org/support, with the address in one place held by
   `SupportLinkTests`. Its state stays Built, not proven: nothing new proves it.
4. **The README and the support page** say a post in the Discord's bug-reports forum becomes a bug report for the developer, its pictures
   on that report and nowhere else, and that anything private goes to the support address.
5. **The Mac sentence** about Command shortcuts and pinch zoom now follows the first tester's (nightly 93) sentences in
   `docs/PLATFORM-SUPPORT.md`, and the README's copy was regenerated from it; the download page follows on the next site build.
6. **Thanks and credits**: Jylee's line names the click that brings a shot's place into view and, with Quack, the dropped set; Quack's
   line names the dropped set; `shots-table` ends "Suggested by Jylee." and `pool-set` "Asked for by Quack and Jylee.", both marked
   reworded since nightly 196.
7. **The phone's half of entry 422** is in the `shots-table` sentence and the parity row.
8. **The GPL section 7 permission**: the wrong words were in six backing records' "where" fields in `docs/claims-backing.json`; they now
   cite entry 279 section 1 (the permission is in LICENSE) and entry 267 (no lawyer review), and `docs/CLAIMS.md` was regenerated.
   `DESIGN.md` line 560 still says the permission is not in force; it is the design as decided in its day, a record, and is left.

The wording of sections 2 to 8 was done by docs-sonnet and read before commit. readme `--check`, claims `--check` (all backed) and the
site build pass.

## Entry 436: the user guide's contents open by section, follow the reader, and every section has a link (2026-10-17)

1. **Subsections.** 62 bold leads in `docs/USER-GUIDE.md` became `###` headings with their own words, the trailing period or comma
   dropped (docs-sonnet). Three were shortened where the lead ran on into its sentence: "Several bulls on any target", "Words underlined
   with dots", "Android updates itself from an APK"; in each, the whole original lead stays in the paragraph. Left alone on purpose:
   wrapped text and button names inside a sentence (`**Marking**`, `**Result** button`, `**Add hole here**`, `**It is a store-bought
   target**`, `**Printer calibrated or serviced**`), leads in the middle of a paragraph, three that read as button names (Advanced,
   Back, Export) and one with a button name nested in it. No `##` line changed. Per section:
   - **Mil or MOA**: Each rifle keeps its own; Everything aiming follows it; Getting GroupLab.
   - **1. Print a sheet**: Three sheets come with the E bull as well; And three with the C bull; The large format sheets print on Letter or A4; Label targets are for a thermal label printer; The zeroing grids are for sighting in by eye; Made for your optic; Several bulls on any target; Store-bought targets; Scale markers; Add a store-bought target; Find holes (Experimental); Large targets; Check the size before you shoot; Your own sheet; A volunteer pack.
   - **2. Shoot it**: none, it has no natural parts.
   - **3. Photograph or scan it**: A flat scan at 600 dpi is the best record of a sheet; Flat on a table, or straight on at the backer; Real inches from a photograph.
   - **4. Mark it and settle the review queue**: Several targets open at once; When a picture names no sheet; Change an answer; Name the caliber if you know it; Holes between bulls, and marks off the grid.
   - **5. Read the analysis**: Tap a number to switch units; Click a hole to edit it; Words underlined with dots; Six figures stay in view; Velocity and the vertical; The zero correction; The two cards; What each "why" says, in plain words; The full CEP table and the fitted ellipse; Fudd buster mode; Shots Needed to Zero.
   - **6. Sessions and the report**: One-page report.
   - **7. Comparing loads**: The loads are never ranked by their figures alone; Each session is named by what tells it apart.
   - **8. The zero correction at another distance, and the dope table**: Chronograph readings; How the screen is laid out; The dope table; At another distance; Hit probability.
   - **9. Keeping GroupLab up to date**: Android updates itself from an APK.
   - **10. Comparing several sheets at once**: The sheets of a set pool into one group; Quicker: drop them together.
   - **11. If something goes wrong**: Report a problem; Send to GroupLab and Send diagnostics, on the phone; Sending in targets.
   - **12. Settings**: none, it has no natural parts.
   - **13. On the phone**: The bar along the bottom; Capture; Show diagnostics on the camera; A photograph from any photo app; On iPhone and iPad; Updates on phones that stop apps in the background; The picture check; The result; Several targets open at once; Sessions, Ballistics and Targets; A target GroupLab did not print.
2. **The contents list** (`toc_list`, `page_guide` in `website/build.py`) is each section with its subsections under it, all in the HTML.
   `/assets/js/guide-toc.js` (no library, `defer`) folds every section but the one being read, marks the current section and
   subsection with the amber left border the hover already used (`aria-current="location"` on the deeper one), opens a section when its
   line is clicked, and scrolls the list itself when it is taller than the window. An `IntersectionObserver` on the headings says when
   to look again; the heading being read is the last one above 30 percent of the window. The fold is a 0.2 s height change, none under
   `prefers-reduced-motion`. Below 1100 px the same list is a closed "On this page" disclosure at the top of the guide, in the download
   page's existing fold style; choosing a line closes it.
3. **Links.** Every h2 and h3 has an id; the h2 ids are unchanged (`site-guide-tests.py` holds them to the slugs of the `##` lines). The
   one repeated subsection, "Several targets open at once", is `13-several-targets-open-at-once` in section 13. Each heading carries a
   `#` link mark shown on hover and keyboard focus; clicking it puts the full address in the address bar with `replaceState`, scrolls the
   heading up, copies the address, and says "Link copied" in a `role="status"` note (or "The link is in the address bar" when the
   clipboard refuses). While scrolling, the address bar follows the heading being read with `replaceState`, only after the reader has
   scrolled, so the history gains nothing. An address with a section lands it just under the sticky header (the page already had
   `scroll-padding-top: 88px`; the headings add 16 px) with its section open.
4. **Checked in headless Chrome** at 1400 by 900 and 390 by 844, both themes, from a local copy of the built site: on load nothing is
   open; scrolled into section 5 it opens with "Six figures stay in view" marked and the address following, history length unchanged; in
   section 13 the list had scrolled itself 273 px to keep the entry in sight; `#5-read-the-analysis` and `#fudd-buster-mode` landed
   184 px down before the margin fix, which is what found the doubled padding; clicking section 8 in the list opened it; the phone
   disclosure works and closes on a choice; with scripts off the side list shows every section and subsection; the testing guide has
   its 9 sections and 2 subsections in the same list.
5. **The build's checks** (`guide_problems`): an id used twice, a link to `#x` with nothing called x, a side list that does not name
   every heading in order, and a user guide with no h3. `feature_problems` now accepts any id on the built guide, so a Features link
   to a subsection is checked the same way. **One choice to note:** these are page checks, so under entry 387 a failure holds back the
   guide page (keeping the copy grouplab.org serves) rather than stopping the whole site; the message names the page, and a test holds
   that. If planning wants it to stop the publish instead, it is one line (`paged` to `problems`).
6. **Features.** 31 of the 62 "In the user guide" links now point at their subsection (docs-sonnet's choice, checked here), for
   example scale-markers, fudd-buster, shots-to-zero, picture-check; the other 31 span several subsections and keep their section.
7. **The PDFs** were made again. Subsections are set at 11.5 pt with no rule under them, as `DocumentPdf` already set level 3 headings.
   GroupLab's PDF writer now writes bookmarks when it is given them (`PdfWriter.Write(pages, bookmarks)`, `DocumentPdf.Bookmarks`): one per
   section, its subsections under it, folded, and the panel opens with them. With no bookmarks a PDF is byte for byte as before
   (`APdfWithNoBookmarksIsUnchanged`), so no sheet changes. The user guide is 1421 KB (1348 before), the testing guide 287 KB.
8. **What else reads the guide.** `scripts/claims.py` keyed 35 backed sentences by their old bold leads; the backing of each was carried
   to its new wording (28 by a matching script, 7 contents-list or picture lines by hand), and the extractor now ignores the `#` marks.
   `claims.py --check` 5303 backed, `consistency.py --offline` 0 findings, `readme.py --check` and `counts.py --check` current.
   `UserGuideTests` read no bold leads and needed no change; it gained the bookmark tests.
9. **Tour and Features** do not describe the guide page's layout, so nothing there is now wrong.
10. **Live, section 5** (checked 2026-10-17 by chores-haiku, read only, with curl): grouplab.org served 436b5f1d. `#5-read-the-analysis`,
   `#fudd-buster-mode` and `#13-several-targets-open-at-once` each name exactly one heading (an h2, then two h3s), and the side list has
   each, the two subsections nested under sections 5 and 13; `guide-toc.js` and the 1421 KB PDF answer 200. The list is in the page
   twice by design: the side list and the phone's "On this page" disclosure.

## Entry 440: the new home page, concept B, built at /testpage/ for Alan to approve (2026-10-17)

1. **The page** is `page_testpage` in `website/build.py`, with its own stylesheet (`/assets/css/testpage.css`) and script
   (`/assets/js/testpage.js`), the site's tokens, self-hosted Plex fonts and theme switch. It has the concept's own header and footer
   and its sections in the canvas's order: the orbit hero, the feature strip, 01 / MEASURE with six cards, 02 / HONEST, 03 / PHONE,
   the closing call and the footer. The canvas's words are kept. **The home page at `/` is unchanged.**
2. **Out of sight:** a `noindex, nofollow` robots tag, not in `sitemap.xml`, in no navigation, linked from no page. `testpage_problems`
   checks all four on every build and stops the publish if one fails (a hidden page found is a whole-site matter, not a page to hold
   back); `tests/python/site-testpage-tests.py` (10 checks, run by website.yml) proves each check catches its case.
3. **Where the buttons go:** Download free and Download for desktop go to `/download/`, which picks the visitor's platform (the
   current home page's button links the Windows installer directly, which a generic "Download free" should not do to a Mac or phone
   visitor); View the source to GitHub; the phone buttons to `/download/?device=android#android` and `?device=iphone#iphone`; each
   card to its Features entry (every-hole, six-figures, compare, ballistics, pool-set, one-page-report).
4. **Motion**, all `transform` and `opacity`: the two rings turn opposite ways (120 s and 200 s a turn) with each picture turned back
   so it stays upright; the wordmark docks into the header as the hero scrolls away and hands over to the small one (the header fills
   in only then, so it never covers the wordmark on its way); the ruler's amber line follows how far down the page the reader is
   (hidden below 860 px); the section index stays bottom right and marks the section in view (hidden on phones); the strip scrolls
   sideways and stops under the pointer; headings and cards ease in once. **Pause motion** stops all of it and is remembered in the
   browser (`grouplab-motion`, read and written in try/catch). Under `prefers-reduced-motion`, or with scripts off, nothing moves,
   nothing is hidden, and the strip wraps instead of scrolling.
5. **Weight**, first load with the cache off, measured in headless Chrome from a local copy: the test page 430 KB in 24 files at 1440
   by 900 and 380 KB at 390 by 844; the current home page 523 KB at both. Two things made it lighter than a first build's 636 KB: the
   orbit and the cards use their own copies (360 and 560 pixels, WebP), the orbit's phone screens cut to their slot's shape; and
   headings and links are set as the canvas sets them, in plain Plex Sans at 400 and 600, so two font files (Plex Sans 500 and Mono
   500) are not needed.
6. **Glossary words:** the page's main content goes through the same glossary step as every page. Decorative labels (the strip, the
   section labels, the cards' label strips) now opt out with `data-terms="off"`, which the glossary check honours too, so a word is
   explained where it is read as prose and never inside a moving strip.
7. **Checked** in headless Chrome: dark and light at 1440 wide and 390 wide; docked state (wordmark gone, header word shown, header
   filled); Pause motion stops the rings and is remembered across a reload; reduced motion shows everything still; the section index
   marks the section in view; no sideways scroll at 390. **Claims:** the page's 13 checkable sentences are backed in
   `docs/claims-backing.json` (the cards by their Features entries, the platforms by docs/PLATFORM-SUPPORT.md, the licence by LICENSE).
   No sentence had to change.
8. **Live** (sections 4.2 and 4.3): grouplab.org served 74cf38fd within about seven minutes of the publish; `/testpage/` answers 200
   with its noindex tag and is not in the live sitemap, and the home page is unchanged. Eight screenshots of the live page, desktop at
   1440 and phone at 390, light and dark, the hero and the whole page each (after scrolling through once, so every section has eased
   in and every picture loaded), are in `C:\Dev\grouplab-local\homepage-concept-b\screens\` for the planning session to show Alan.

## Entry 437: more screenshots in the user guide, and markings on them that stay right (2026-10-17)

1. **Pictures added** (docs-sonnet chose and captioned them; checked here against the pictures): 19 new, none of the seven removed, so
   the guide shows 26. Per section: Mil or MOA, scope-unit and equipment; 1, scale-markers, fingerprint and store-target; 2,
   bull-lines and groups; 3, shots-open; 4, which-target; 5, velocity; 8, pairing and ballistics-hit (the ballistics picture moved down
   to "How the screen is laid out"); 10, set-tab; 12, settings-data (new, below); 13, four phone pictures (fold-capture,
   fold-result-landscape, fold-open-targets, fold-sessions-landscape), shown at a phone's size. **One new render**: the lower half of
   Settings, `settings-data`, which section 12 describes (Your data, Diagnostics, Report a problem), added to the render walk, to
   `screens.json` and `SOURCES.md`, and to the tour's Settings stop as its second picture. **Screens the guide describes that have no
   render yet**, each needing a new state in the render walk, listed for the next pass: the update bar (9); the Report a problem
   window (11), a window of its own that the walk does not open; the live camera with its level (3 and 13), which needs a camera; the
   Design your own sheet form scrolled to Make the sheet (1); the hole editor (5); on the phone, the picture check, Fix holes,
   marking by hand, Ballistics and Targets (13), which come from the emulator's sweep, not this walk.
2. **Where things are on each picture.** When `Entry109Tests` photographs a screen at 1400 by 900 in the light theme, it now writes
   `<screen>-bounds.json` beside it (`ScreenBounds`): every visible control with a name, as `[x, y, width, height]` in the picture's
   own pixels, clipped to what is on the picture. A control's name is its `Name`, else its accessible name, else the words on it
   (button, tab, check box, menu entry); parts of a control's template are left out, and the first visible control with a name keeps it.
   The weekly screenshot job writes them with the pictures and commits the folder, so a picture and its positions are always retaken
   together. **The phone pictures have none:** they are taken on the Android emulator by `scripts/android-screens.sh` from the screen
   alone, and nothing there reads the application's layout yet; section 13's phone pictures are therefore shown unmarked.
3. **The markdown says which controls a picture marks**, in one line under it, `<!-- marks: Accept and analyze | Next unsure hole -->`,
   names in the order the text mentions them; GitHub shows nothing for it. `render_guide` takes it out and draws the markings;
   the text's "(1)" and the caption's "1 ..." become numbers that point at their outline. **The site build fails** (the guide page is held
   back, as every page check since entry 387) when a marking names a control not in its picture's positions file, a marked picture
   has no positions file, a picture has more than three markings, a caption does not list its markings, or a "(n)" in the text has no
   marked picture with that number under its heading. `site-guide-tests.py` holds each of these (26 checks).
4. **The look, as section 3 set it:** an amber (`--amber`) outline 2 px wide with a 1 px halo in the page's background, outside and
   inside, and a 22 px amber badge with its number in the page's dark text color (`--on-amber`) in Plex Sans, on the outline's top left
   corner, moved inside the picture at its edge and along the outline when it would sit on another badge (`mark_overlay`). It is laid
   over the picture as positioned elements sized in percent of the picture, so it scales with it and the PNG is untouched; HTML rather
   than an SVG layer, because a badge has to stay 22 px across however small the picture is shown, which an SVG scaled with the
   picture cannot do. Pointing at or focusing a number in the text or caption thickens that outline to 4 px (no animation under
   reduced motion). Badges and outlines are `aria-hidden`; the caption, which is the alt text, says the same in words.
5. **In the PDF** `GuideVerb` draws the same outlines and badges into each picture from the same positions file and the same
   placement (white halo for the paper), and `DocumentPdf` skips the marks line and sets a phone's tall picture at most half a page
   high at its own proportions instead of across the page.
6. **Which controls are marked** (2.4, docs-sonnet's choice from each positions file): scope-unit, Mil, MOA and Both; equipment, the
   scope unit and click value fields; targets, Open as PDF and Bulls in; scale-markers, the three print buttons; fingerprint, the
   steps, Next and Its printed size; bull-lines, Lines to bulls, Next unsure hole and Done; shots-open, Check your printer and Measure
   this sheet with a ruler; which-target, its three answers; velocity, Conditions on the day and Velocity band; sessions, the rifle
   and load filters and Delete; pairing, the three choices under a reading; ballistics, Work out the table and the Trajectory and Hit
   probability tabs; ballistics-hit, the target shape; set-tab, the set's tab; settings-data, Export all my data, Import data and What GroupLab sends. A second marking on set-tab (a single table row
   standing for the whole Shots list) was dropped here as misleading. **Unmarked, and why:** analysis-open and settings, because no
   control the text under them sends the reader to has a name in their positions files (the six figures are not controls); marking,
   because it shares its heading with which-target; compare, which has no positions file yet (below); store-target, whose picture is
   put together from two windows and has none; and the phone pictures.
7. **The pictures were retaken** to go with their positions: the positions must come from the same render as the picture, so this
   commit carries a fresh render of every desktop screen, exactly as the weekly job makes them, except **Compare loads**, whose
   published picture is Alan's own two suppressor sheets from the test data (186 MB, not downloaded here): it keeps its picture, has
   no positions file and no markings until the weekly job retakes both. **Fixed on the way:** the render walk analyzes the same
   sample many times, and its "This looks like another copy of a sheet" notices had piled up in the corner of every later screen (two
   in the published pictures, covering Open as PDF on Targets); a notice fades by itself in a few seconds, so the walk now clears
   them before each picture.
8. **Checked** in headless Chrome, all 14 marked pictures at 1400 by 900 in both themes: every outline sits on its control, badges
   clear the edges, and numbers in text and caption thicken their outline. Before and after, the Session records picture:
   [before](figures/entry-437/sessions-before.png), [after](figures/entry-437/sessions-after.png).
9. **The PDF** was made again: 35 pages, the markings drawn into its pictures, the phone pictures half a page high and centered; the
   user guide PDF is 4104 KB (1421 before), a reasonable download for 26 pictures. Pictures are now held to 1400 by 1100 pixels, which
   is what half a page shows at about 200 dots an inch.
10. **Claims:** the extractor now ignores a picture's badges and the bracketed numbers, as it ignores the heading marks; the backing of
   each sentence that only gained a number was carried over, and each new caption is backed by its picture and positions file
   (5322 claims, all backed).

## Entry 438: a statistics primer for shooters, from nothing to judging a group (2026-10-17)

1. **Where it lives.** `docs/STATISTICS-PRIMER.md`, "Statistics for shooters, from the beginning", is the fourth tab beside User guide,
   Trying GroupLab and Glossary, with entry 436's contents list and a link on every section and the guides index, sitemap and README
   naming it. It has its own PDF (111 KB, bookmarked) and its own link preview picture. Linked from the home page, the analysis section
   of the user guide, the glossary's head and the five articles (cep-explained, how-many-shots, mean-radius-or-extreme-spread,
   moa-mils-inches, pooling-groups). Published, since it meets section 5 (below).
2. **How it teaches.** It opens on a real 25-shot group read on the phone (a consented scan, already published) and builds 18
   sections in order: the center, extreme spread, mean radius, angles, sigma, CEP, luck, intervals, comparing loads and p-values, how
   many shots, flyers, velocity SD, hit probability, what is good enough, reading a claim, what no number can tell you. Each has the
   idea in one sentence, a picture or a figure where one helps, "At the range", and "In GroupLab" linking to the user guide's
   subsection (a link into another guide goes to its page on the site, and the site build checks the heading exists). The formula comes
   last, folded under "The math, if you want it", and labeled the same in the PDF. Angles come before sigma so no unit is used before
   it is explained.
3. **The numbers are GroupLab's.** `PrimerNumbers` in Core works out every figure on a fixed seed (40 groups each of 5, 10 and 20
   shots with their figures; how far extreme spread and mean radius wander over 4000 groups at 3 to 30 shots; two identical loads
   compared 2000 times; the hit table; shots needed), `grouplab primer` writes `docs/figures/primer/primer-data.json`, and
   `PrimerTests` fails if the committed file is not what Core computes, and holds four figures to the published articles (0.68 to
   1.92 from five shots, 81 shots for 20 percent tighter, CEP 50 and mean radius as 1.18 and 1.25 sigma, the test calling identical
   loads different about one time in twenty). The page does no statistics: the build draws each figure as an SVG from the file, and
   `primer.js` only steps through more of the same groups.
4. **Figures that teach luck**: six five-shot groups with **Shoot five more** and a choice of 5, 10 or 20 shots; the spread of extreme
   spread by shot count; two identical loads with **Compare again**, keeping a tally of how often one "won". Also the center, mean
   radius and CEP 50 and 90 drawn on groups GroupLab simulated. With scripts off, or reduced motion, each is a still picture whose caption
   says what it shows (checked: all six drawn, every button hidden).
5. **Judging what is good** is by job: a hit table from precision alone for 4 and 8 inch circles at 100 to 800 yards, with what it
   leaves out said plainly. **No outside typical figures are given** (factory, match or benchrest groups, velocity SDs): no measured
   source was found that could be backed, and the page says so rather than quote one. Reading a "half-MOA rifle" claim and the honest
   limits have their own sections.
6. **Reading level** (`scripts/readability.py`, Flesch-Kincaid grade, leaving out the folded math, tables, pictures and links): the
   whole page grade 5.1, no section above 6.1 after docs-sonnet's plain-language pass (5.2 before it); a general newspaper aims at
   about 8 to 9. The pass changed no number, claim, link, heading or math part.
7. **Easy to pass on**: a one-page summary, "five things to know", made from the guide itself (the five points marked in it, each with
   its section's "At the range" advice), in Letter and A4 (`DocumentPdf` now sets A4 and tables), linked at the top and bottom of the
   page. Each section's opening sentence stands alone.
8. **Section 5's rules**: every checkable sentence backed (one rule for the primer's surfaces naming the data file, `PrimerTests` and
   STATISTICS.md, 5403 claims backed); no pseudoscience; the developer not named; American spelling (0 British forms); no em dashes;
   the glossary's words used its way. On a phone the six-column hit table scrolls inside itself (all guides' tables do now below 860
   px), and the page is 390 px wide at 390.

## Entry 439: "Walk me through a demo", a guided tour of GroupLab on a sample target (2026-10-17)

1. **Where it is offered.** Once, after the first run's questions are answered, as its own card: "Walk me through a demo (about
   five minutes)" or "Not now", and never again on its own after either (Esc is Not now). Any time from Settings, a row near the top
   ("Walk me through a demo", and "Carry on from step N of 13" when a tour was left part way, which does the steps before it for the
   person so the screen is where that step expects), and from the header's menu. The offer sits beside the first run, not on it, so
   the first run's own questions and tests are unchanged.
2. **The sample is made by GroupLab** when the demo starts: the 25-bull Letter sheet as it prints, one simulated hole a bull, the way
   the tests make theirs (`SyntheticSheet`), written into the application's own folder. **This differs from the entry**, which names
   a shipped `samples/sample-25-shots.png`: no such file exists, and the only sample (`gl-cf25-ltr-d-25-shots-600-dpi.png`) is a real
   person's consented scan carried by the Windows zip alone. Question 101 asks whether this is what was meant; the testing guide's
   stale sentence is corrected. It opens in a tab of its own named "Demo: sample sheet", is read as any sheet is (24 of 25 holes found,
   so the review queue has something in it), and is never offered to the project.
3. **The steps** are one list of data (`MainWindow.DemoSteps`: screen, named control, words, and what Next does), 13 of them: the
   sample, GroupLab reading it, the review queue, the caliber, Accept and analyze, the plot and figures, each figure's range and why,
   clicking a hole, the zero correction, Session records and the report, Targets and its print button, Ballistics, and where to
   get help.
   Comparing loads and several sheets are left out: honestly shown they need a second sample. **The tour never prints**: its Targets
   step points at Open to print, the print button on every platform (Print... is Windows only), and has no action (a test holds
   that).
4. **How it behaves.** Each step draws entry 437's marking around the real control, an amber outline with a halo in the window's own
   background and the step's number in a badge, with a card beside it (the step, "6 of 13", Back, Next, End the tour) placed below,
   above or beside the control so it never covers it, and following it as the window lays out. The rest of the window stays usable
   and is not dimmed. The person can do the step or press Next and GroupLab does it. Tab and Enter reach every button, Next takes
   the focus with the step's words as its description, and Esc ends the tour. Checked in both themes at 1280 by 720 and 1400 by 900.
5. **Nothing the demo makes is kept** unless the last step's "Keep this demo in Sessions" is ticked: its tab closes with nothing
   asked, every session it saved (found by its picture) is deleted, and its picture is removed. `Entry439Tests` holds that the
   database is the same after a finished tour and after one ended part way with Esc, that keeping keeps exactly one session, and that
   the offer comes once.
6. **It stays in step with the application.** Controls are named one way for the guide's positions files and the demo
   (`ControlNames`, which the test recorder now uses too), and the headless walk fails, naming the step, when a step points at a
   control that is not on its screen. To give the steps something to point at, the rail's six buttons now carry accessible names
   (they had only tooltips, so a screen reader could not name them), and the picture, status line, review queue, plot and zero
   correction carry names.
7. **The weekly render walk** photographs the demo at its plot step (`demo-tour`), for the Features page (a new feature, "since next")
   and the tour's Settings stop; the user guide gains a "Walk me through a demo" subsection under Settings and a line at its start;
   the testing guide's first minute now starts with the demo; the README's feature list names it.
8. **The phone tour (section 5) is the next entry's work**: the desktop tour filled this one. Android and iPhone need the same offer
   and steps for their own screens (Capture with the sample chosen from the app's files, the result, Shots, Share a picture, Sessions,
   Ballistics, Targets), and their own headless walk.

## Entry 441: concept B ("Orbit") is the grouplab.org home page (2026-10-18)

1. **The home page is concept B.** `page_home` in `website/build.py` is entry 440's page, renamed from `page_testpage`, with its
   stylesheet and script now `assets/css/home.css` and `assets/js/home.js`. Nothing about its motion changed, so pause motion, reduced
   motion and scripts off still leave it complete and still.
2. **The page before it is restorable in one step**: it is `page_home` in `website/build.py` at b216ce56, named in the commit message
   and in the comment above the new page. The helpers only it used (the README's product picture, its alt text and its three lines,
   and the two product WebP files) went with it; they are in the same commit.
3. **Indexed and in the sitemap.** The noindex is gone, and the page now carries a canonical address and the same sharing tags as every
   other page. `/` was already in the sitemap. Entry 440's check is turned around as `home_problems`: the home page is concept B, does
   not ask to stay out of search, is in the sitemap; the old preview address is not in the sitemap and is a page with one link to `/`
   (no meta refresh, which entry 151 bans), never a second copy; the navigation does not name it. `tests/python/site-home-tests.py`
   (renamed from `site-testpage-tests.py`) holds it with 22 checks, run by website.yml before the build.
4. **What the page before carried and a visitor still needs**: downloads, the guides, Shoot a target and Research are in the header;
   support, release notes, GitHub and Discord were in the footer, which now adds the guides, Shoot a target and the hardware survey
   (the survey was reachable only from the old footer). The honest limits are one paragraph under the three points: test builds,
   unsigned and rebuilt after every change that passes, so one may be broken; not built yet, hole detection on plain paper and
   synchronization; links to the download page's expectations and the full status. Not carried over: the "New in GroupLab"
   spotlight (the Features page has its own, "Newest"), and the tour and statistics primer paragraphs (the header reaches both).
5. **Everything that describes the home page**: the README describes no part of it and pictures none; no screenshot of the site
   exists in `docs/figures`; `docs/WEBSITE.md` names only the live check, which reads the build tag the page still carries.
   `scripts/consistency.py`: 0 findings. Claims: three new sentences backed, 5408 claims all backed.

## Entry 442: the desktop from the keyboard, Quack's request (2026-10-18)

1. **Why the focus went back to the top.** Choosing a shot calls `PickShots`, which refreshes the analysis, and the refresh clears the
   shot table and builds it again (`offsetTable.Children.Clear()`), so the row that had the focus left the window and the focus with it.
   The same happens wherever a panel is rebuilt when something in it is chosen: the tabs (`ShowTabs`), session rows, the library list
   and the rest of the 85 panels that are cleared and rebuilt. One fix covers them all: `KeyboardFocus` (new,
   `src/GroupLab.App/KeyboardFocus.cs`) remembers the focused control and its ancestors when it takes the focus, and when it leaves the
   window with nothing else focused, gives the focus to its replacement: the control with the same `Tag` (each shot row now carries its
   shot), otherwise the same kind of control in the same place of the rebuilt panel. It comes back the way it went, so a ring drawn for
   the keyboard is drawn again and a click draws none. On the main window and the figures' own window.
2. **Arrow keys.** The shot table answers its own (`OnShotKey`): Up and Down choose the shot before and after through `PickShots`, as a
   click does; Home and End the first and last; Left and Right the same bull on the nearest sheet before and after in a pooled set
   (`SetPool.SheetOf` and `BullOnSheet`), nothing on one sheet; Esc clears the choice. The focus follows the chosen row. Elsewhere,
   `KeyboardFocus` moves between buttons by where they sit: Left and Right to the nearest button on the same line, Up and Down to the
   nearest in the same column for the rail's buttons and a list's rows, within the bar or scrolled panel the button is in (the tab
   strip, the header, the toolbar, a side column). It acts only when a plain button has the focus: text boxes, number boxes, check
   boxes, radio buttons, switches and drop downs keep their own arrows, and moving never presses anything. The marking picture's arrows
   are left alone; the status line suggests what to do with them.
3. **The caliber line.** `UnitSwitch`'s number pattern did not allow a number written without its leading zero, so a tap remembered in
   centimeters read ".224 in (5.69 mm)" as 224 inches after the point and wrote ".568.96 cm (0.57 cm)". The pattern now reads ".224" whole
   and never starts inside a number, and the caliber line, already in both units, is no longer a number a tap switches. Nothing to do
   with the culture: every figure is written with the invariant culture.
4. **Tests.** `Entry442Tests`: on one sheet, Tab from the window's start into the shot table, Space, and the focus is on the chosen row
   with its keyboard ring, the next Tab on the row after, Enter the same; Quack's pooled set of three sheets end to end with Space, Up,
   Down, Home, End, Right and Left across sheets and Esc; the rail with Up and Down, the top bar and the tab strip with Left and Right,
   a text box keeping its arrows; the caliber line. With `KeyboardFocus` switched off three of the four fail. `UnitSwitchTests` gains
   the number without its leading zero.
5. **Focus ring.** Fluent's own ring, rendered and looked at on a tab and a chosen row; it is not clipped by the tab strip.
6. **Everything public.** The user guide's new "From the keyboard" under Read the analysis; a Features entry, "Work it from the
   keyboard", with its own picture from the render walk (`keyboard`, a row chosen with Space and the ring on it); the README's Phase 3
   line; three release notes crediting Quack; a note for Quack in for-alan.md.

## Entry 443: one header, one footer and the home page's look on every page (2026-10-18)

1. **The header.** `site_header` in `website/build.py` writes the one header every page carries, the home page's, with every link it
   had before entry 441: Download, Tour, Features, Shoot a target (Send a target while the upload page is open), Guides, Research,
   Community, Release notes, Support and GitHub. As entry 304 had it, from 981 to 1499 pixels the last four move into More, and below
   981 every link is in the menu. The page being read is underlined in amber. The link words are written as before and set in capitals
   by the stylesheet, so no word changed. Only the home page has Pause motion and the docking wordmark, since only it moves.
2. **The footer.** `site_footer` writes the one footer, in the home page's look with the site footer's words and all ten links (the
   home page's footer had four).
3. **One look.** Every other page takes the home page's header and footer, its type (headings in Plex Sans 600, labels in mono
   capitals), its buttons (mono capitals, an 8 pixel corner, a hairline border, amber for the main one), square-cornered panels, notes
   and callouts, and 40 pixel gutters; colors and both themes were already the same tokens. No orbit, no docking and no motion off the
   home page, and no script added, so with scripts off or reduced motion every page is complete and still. No ruler or section index
   on the long pages: they would add weight and motion for little help. No page's words or addresses changed.
4. **Weight.** Measured over all 76 pages (HTML, stylesheets, scripts, preloaded fonts and the images a page loads first): every page
   is lighter, by 2.2 to 9.5 KB; the home page 116,407 to 114,171 bytes, the download page 221,526 to 212,011. The stylesheets are served
   without their comments (`served_css`), and the old header and footer rules and seven rules only the page before concept B used are
   gone; `site.css` is 35,144 bytes, `home.css` 9,461.
5. **Checks.** `header_problems` fails the build when a page lacks the shared header or footer, or its header lacks one of the links;
   `tests/python/site-home-tests.py` holds it (32 checks), and that the home page and a plain page take their header and footer from
   the same two functions. `scripts/claims.py` now reads the shared header and footer once, as a surface of its own, instead of running
   them into each page's first and last sentences; the 20 page sentences this split out are backed as the same words as before.
6. **Looked at** in headless Chrome at 1600, 1400 and 1200 pixels and at the narrowest Chrome allows (512): the header, the More menu,
   the footer, the download, support, release notes and user guide pages. At 360 pixels 22 pages scroll sideways, the same 22 before
   this entry (wide tables and figures in research articles and two guides): recorded, not fixed here.
7. **Pictures of the site.** None exists in the README or `docs/figures`, so none to retake. The user guide's PDF, behind since entry
   442, is regenerated.

## Entry 444: the home page's look really on every page, and every link in the bar from 1280 pixels (2026-10-18)

1. **Headings.** Every page's h1 and h2 are the home page's display face, IBM Plex Sans Condensed bold (entry 443 had set them in Plex
   Sans 600). The h1 runs from 44 to 80 pixels with the window (it was 52 at most), a guide's or article's from 40 to 60, an h2 from 30
   to 46. The home page's own section headings keep the face they were designed in. Titles are as written, not in capitals: the look
   question is a DESIGN NEEDED line in `docs/notes/for-alan.md`.
2. **The top band.** Every page's top is in the hero's order: the mono label with a short amber rule before it, the condensed title, the
   lead, and the page's buttons, which were already the home page's (now 48 pixels tall, as "Download free"). Research, Release notes
   and Tour gained the label their neighbours had; an article's and a tour screen's breadcrumb is set as the label. Behind every top
   band but the home page's, the home page's rings, still: an inline drawing (`PAGE_RINGS`) in the theme's own line color on the sunk
   background, fading into the page, so it is right in both themes with no picture to load.
3. **Ruler and section index** on the guides, research articles, release notes, features and support (`LONG_PAGES`): the ruler down the
   right edge as on the home page, its amber line following the scroll, and the section index, one link to each h2 (on the release
   notes, the twelve newest builds), the headings given an address where they had none. At 1400 pixels the home page's index would
   cover the content's right edge, so on a long page it shows its numbers and opens the names on hover or focus. `page.js` adds the
   ruler's numbers and marks the section being read; nothing moves by itself, and with scripts off both are there, unmarked.
4. **Cards, tables, code.** Research cards, feature cards and the Newest cards sit in the home page's hairline grid, a picture edge to
   edge on a research card; corners square on folds, picks, tabs and badges; list markers amber; table headings in mono capitals; code
   blocks on the sunk background with an amber edge.
5. **The header.** All ten links show from 1280 pixels (measured: nothing overflows at 1280, 1300 or 1500 on the home page, support or
   the user guide), tighter type and spacing below 1500; More from 981 to 1279; the menu below. On the home page from 1280 to 1499 Pause
   motion shows its icon, as on a phone. The home page's header is blurred and tinted from the start, so the orbiting screens no
   longer show through its links, and solid once the wordmark has docked; the wordmark flies above it while it docks.
6. **Weight.** Same measure as entry 443, over all 76 pages: every page is lighter, by 452 to 9,900 bytes; the home page 114,179 to
   104,279, features 245,790 to 240,043, download 238,531 to 232,035, the user guide 320,341 to 314,111, support 151,389 to 146,107,
   release notes 320,303 to 315,118. Paid for by serving the how-it-works pages' and the download page's own styles only to them
   (`how-it-works.css`, `download.css`), 15 unused rules removed, and scripts served without their comment lines (`served_js`).
   `site.css` is 28,250 bytes.
7. **Checks.** `heading_problems` fails the build when a page's h1 is not the display face: it reads the stylesheets each page links,
   at every width, and fails on a rule reaching an h1 (by element or by one of its classes) with another face or weight, on an h1 with
   its own style, and when nothing gives `.tp h1` the display face. `tests/python/site-home-tests.py` holds it and the rest (49 checks).
   `scripts/claims.py` cuts the section index out of each page as it does the header, since it repeats the page's headings; the tour's
   opening, now with its label, is backed as the same words.
8. **Proof.** Twenty-four pictures in `docs/figures/site/entry-444/`: home, features, download, the user guide, support and release
   notes, at 1400 by 900 and at 390 on a phone, dark and light. Also looked at with scripts off and with reduced motion (complete and
   still), and no sideways scroll at 390.

## Entry 445: the guides' "On this page" on the other long pages (2026-10-18)

1. **One function.** `with_contents` in `website/build.py` puts the guides' contents on every page `CONTENTS_PAGES` names, and the
   guides now take theirs from it too: `contents` reads a page's h2 and h3 headings (a small HTML reader, `_Headings`), gives an
   address to any heading without one and a link mark to each, and `contents_navs` writes the side list and the same list folded at the
   top below 1100 pixels. A page laid out as a guide gets the list beside its article; any other gets it beside everything after its top
   band. `guide-toc.js` now follows the list's own entries rather than a guide's headings, so it works on every page, and passes over an
   entry the Desktop | Mobile switch hides.
2. **Pages.** The guides, features, every tour screen (the list follows the view where a screen's sections differ by side; on the
   features page both sides have the same sections), every research article and the research index, release notes, support, what can be
   measured, shoot a target and the community page. **Not given one:** the download page (one section a device, behind its own picker),
   the tour's index (its headings are links to the screens, not sections) and how it works (its map links each part already). The
   release notes are now grouped: "The newest builds", the ten newest each named in the list, then "Older builds" as one entry; a
   build's own headings sit a level below its name.
3. **One list, not two.** Entry 444's numbered section index is gone from every page that had it (all of them now have the list); the
   home page keeps its own. The ruler down the edge stays.
4. **Checks.** `guide_problems` runs on every page with the list: every id once, every link lands, the list names every heading in
   order in the HTML as built, so it is whole with scripts off. `tests/python/site-guide-tests.py` 56 checks (26 new),
   `site-home-tests.py` 49. `scripts/claims.py` cuts the lists out like the header; six sentences that used to run on from a list are
   backed as the same words as before.
5. **Weight** (HTML, stylesheets, scripts, preloaded fonts and first images, as entry 443 measured): features 239,843 to 260,369 bytes
   (73 entries listed twice), a tour screen 284,419 to 291,851, a research article 152,319 to 158,991, release notes 314,918 to 320,794,
   support 145,907 to 152,246; the user guide 313,911 to 312,438 and the primer 201,537 to 199,610 are lighter (the numbered index gone).
6. **Pictures.** Features, release notes, support, a research article and a tour screen at 1400 pixels and at a phone's 390, both
   themes, in `docs/figures/site/entry-445/` (headless Edge). Nothing on the site or in the README pictures the website's own pages, so
   nothing there to retake.
7. **Also found.** `scripts/american-spelling.py` let a backslash in a quoted string match two ways, so where it paired a quote wrongly it
   tried every split of every backslash after it: with this entry's builder it ran for hours (the spelling test ten minutes before it was
   stopped). A backslash now only matches as an escape; the whole check takes a second. Paired rightly, it found two style names
   spelled "centre" in the builder, now "center"; nothing a reader sees changed.

## Entry 446: the phone's "Walk me through a demo" (2026-10-18)

1. **The tour on the phone.** `PhoneDemo` (`mobile/GroupLab.Mobile/PhoneDemo.cs`), shared by Android and the iPhone: thirteen steps as
   data (`PhoneDemo.Steps`), each naming its place, the control it outlines by automation id, its words and what Next does. The sample
   opens as a chosen picture from GroupLab's own files (never the camera), then the plot, the figures, Fix holes, Shots and its table, Zero
   from this group, Share a picture and its page, Sessions, Ballistics, Targets' Made for your optic, and Settings. Each step draws the
   desktop's amber outline and numbered badge around the real control and a card under it, over it, or in the larger room left, where it
   scrolls. Offered once after the first run's questions (`Shell.Begin`); Settings has a **Guided demo** card with **Walk me through a
   demo** and **Carry on from step N of 13**. Next does a step; **End the tour** or the phone's Back ends it (`Shell.Back`). The card's
   words are the desktop's, now in `src/GroupLab.App/DemoWords.cs`, linked into the phones.
2. **One sample.** `DemoSample` in Core makes the 25-bull Letter sheet at 300 dpi with one hole a bull, from a fixed seed; the desktop's
   `WriteDemoSheet` and the phones both call it, so all three make the same pixels (`DemoSampleTests`). Question 101 archived as answered.
3. **Nothing kept unless chosen.** Ending removes the demo's target with nothing asked, its session, the session's picture folder and the
   sample; ticking **Keep this demo in Sessions** on the last step keeps them. The caliber the demo names (.308 Winchester, as on the
   desktop) goes back to what was typed. Text goes through the phone's styles and so `TextSize.Of`, the iPhone's Dynamic Type included;
   the badge grows with the text; a screen reader hears each card as "N of 13: title. words" and again on Next, which takes the focus.
4. **Walked in CI.** A scenario step, `demo`, walks every step and fails on a control not on its screen or on anything left behind; it is
   the last step of `phone-sweep.json`, which the iOS simulator run (ios-app.yml) and the Android emulator run both play, and headless in
   `PhoneSweepScenarioTests`. `Entry446Tests` (6): the walk, keeping, ending part way by Back, Settings' carry-on, the card inside the
   screen and named at Android's largest text, the offer once. The phone screens walk pictures step 2 as `fold-demo`, waiting for the
   emulator's first run (SCREENSHOTS.md); the Features entry's Mobile side says so until then.
5. **Also found.** The card's first placement measured its own words on every layout pass, against its scroll's own measuring, and
   the screen under it was never laid out again; placement now reads the size the last layout found and sets a value only when it
   changes. The screen quality check now treats the card's words over the page as an overlay, as a sheet is.
6. **Same change:** the user guide's demo section, the Features entry (Android, iPhone and iPad), the parity table, the release note.
   No look question: the outline, badge and card are the desktop's styles.

## Entry 447: an overnight queue, its four parts (2026-10-18)

1. **The marking picture's arrow keys.** With a hole selected and the picture holding the focus, an arrow nudges the hole one pixel of
   the picture and Shift and an arrow ten, in the direction the arrow points on the screen however the view is turned
   (`MarkingCanvas.OnKeyDown`); a run of presses, ended by any other key, a press on the picture or a pause of two seconds
   (`MarkingCanvas.NudgeRun`), is one step Undo takes back (`MarkingSession.NudgeShot`). With nothing selected the arrows move the view,
   40 pixels a press and 200 with Shift. A text or number box with the focus keeps its arrows. `Entry447Tests` (4); the user guide's
   From the keyboard section has the bullet.
2. **Keyboard and screen reader pass.** Desktop: every screen (marking, analysis, Sessions, Targets, Ballistics, Compare, Equipment,
   Settings) and the report and printer check windows tabbed all the way round headless: the focus never lost, the round reaching the
   bar down the left (so nothing holds it inside a panel), every stop named as a screen reader reads it (its automation peer's name).
   About 380 stops; 14 had no name and now do: the column grips ("Column width: the left and right arrows widen or narrow it"), the
   caliber box (named on its inner field too, `CaliberBoxes.Named`, the phone's as well), the marking picture, and the printer check's
   name, across, down and unit boxes (by the words beside them). The user guide's tool keys gained B, O, Ctrl+N, Ctrl+V, Delete on a
   placed bull and the arrows. Phones, at Android's largest text: every place along the bottom, the result and the five pages one press
   from it, every control named (none was missing); the one fault was that TalkBack and VoiceOver read the bar along the bottom before
   every screen, so the frame now holds the page first (`Shell`, a grid in place of the dock). Tests: `Entry447Tests` on both, the
   desktop's two round trips and the phones' names and order. The focus ring is Fluent's own, unchanged. **Left** (in STATE): the
   dialogs not opened by the tests (CSV mapping, store-bought target choice, Fudd buster, group tools, print, the figures window), the
   phones' pages two presses away (Fix holes, velocity, fingerprint), and a run with a real screen reader on each system.
3. **The iOS Real taps flake, found and fixed at its cause.** The red runs share one thing: a tap lost just after a field took the
   focus and the keyboard came up for the first time. Two records were added to find it: what the drawing held when GroupLab's own
   check could not reach a control, and every press and release the window receives while a scenario runs (`scenario.pointer`). The
   loop of runs then caught it (run 39142559271): the tap on the distance box reached the app 0.7 s late and its release 1.8 s after the
   press, while the keyboard first came up and held the screen's thread; by then the wait that decides a finger is being held had run
   out, so the box opened its Cut, Copy and Paste menu, and the first tap on Next landed on that menu's dismissing layer
   (`LightDismissOverlayLayer`), closing it and nothing else; every later Next worked, one field behind. A person meets the same when
   the keyboard is slow to come up. **The first fix** (nightly 201) closed such a menu when the late release showed the finger had been
   down less than the hold's wait; 12 runs in a row were green, and the 13th (39172455366) showed it closing the menu while a
   dismissing layer stayed over the window, because the field's hold does more than open the menu (it selects a word and shows
   handles). **The fix at that cause** (`KeyboardRoom.HoldOffForAFocusingPress`): the press that first puts the focus in a field is a
   tap to place the caret and bring up the keyboard, so the field's hold is off while that press is down and comes back once its
   release has been handled; a long press on a field that already has the focus opens its menu as before (`Entry447HoldTests`).
   **Two more causes, found on 2026-10-19 with more records** (a stalled screen thread, `scenario.stall`; each holder of an unreached
   control as a hit test sees it; a count of frames drawn). Run 39243232233: XCTest spent 34 s in "Wait for com.apple.springboard to
   idle" before one press, with GroupLab answering throughout, and the press lost its release; a finger does not wait for the home
   screen, so the tapper now replaces XCTest's wait for idle with one that returns at once (`NoWaitForIdle`, printed in
   touch-runner.log). Run 39284329697, then 39292731869 with frames counted: after the first keyboard, and only the first, the
   simulator drew nothing for 2 to 6 seconds (131 frames from the keyboard opening until 2.3 s later) while the screen's thread
   answered, and every holder of the bar's Next was hit-testable, enabled, shown and in place; Avalonia judges a press against the last
   drawing, so Next could not be reached until drawing resumed, and a hold gave up after 5 seconds. A person cannot tap a bar that has
   not been drawn, so this is the simulator's drawing, not GroupLab's fault. The check now counts only time in which frames are drawn
   toward its 5 seconds, waits out a pause in drawing up to 30 seconds and says it ("nothing was drawn", never "covered"), and a control
   that is really covered still fails after 5 seconds of drawing (`PhoneTouchScenarioTests.AHoldWaitsOutAPauseInDrawingAndSaysIt`).
   No sleep or retry was added; question 104 asks planning whether this counts as fixed at the cause. The scripted taps' check also
   refuses a tap a menu's layer would take, and says so. **The proof**: 20 runs of ios-app.yml in a row, all green, from 34d259e6 on
   2026-10-19 and 20 (one more, 39304940907, was cancelled by another run starting and is not counted): 39299320603, 39306970375,
   39310435399, 39314918748, 39319450122, 39323658807, 39327999986, 39332104867, 39335983262, 39339795636, 39342772254, 39345433622,
   39348682023, 39351458267, 39354305928, 39356898269, 39359305053, 39361859347, 39364572136, 39366879795. The second showed the pause
   waited out: 1.0 s with nothing drawn, then Next reached.
4. **Tests and speed.** 4.1, **the corpus counts record rewritten** (`scans/phase1/measurements/detection-counts.json`, with
   `grouplab corpus counts --write`), approved by planning: 29 of 55 images changed, every one listed old and new in the commit. No
   image lost a real hole: on all 27 punched scans the punched holes found rose or held (by 0 to 5), never above the number punched. On
   the unpunched photographs, where every hole found is spurious, over the six whose count changed the spurious holes fell from 36 to
   16; two rose by 2 (ultrawide3, telephoto2) and four fell, telephoto3 from 17 to 3. The changes are entries 415 and 417's.
   4.2, **the proof checklist.** Two rows the repository's own material could prove: row 12, `grouplab compare-photos`, against its
   proposed gate (it reads gates 1 and 2 on real pictures), by `PhotoComparisonTests.OnAlansOwnScanAndPhotographItReadsBothGates` on
   Alan's 6 ARC dominus-k scan and its Fold 7 photograph from the test-data release (worst bull 0.0057 in, 25 of 25 holes, none false;
   it reads, it does not meet); and row 18, the support link, whose written gate (one browser launch once a page exists) the live
   support page and `OutsideWorldTests.TheSupportButtonAsksForTheSupportPageAndOpensNothing` meet: its README row is now Done, and
   row 12's README line says what it reads, its state left to planning since its gate is only proposed. Every other row still names
   material from Alan, a detection change or an unwritten gate.
   4.3, **speed** (docs/PERFORMANCE.md, "Entry 447 section 4.3"): measured first; the slowest step left in reading a photograph is
   finding its holes, and inside it a large mark's rows were found by testing every pixel from each end against the mark's hull. They
   are now found by halving, with the same ends on every row (`RowInsideTests`): finding the holes on 15 local corpus photographs
   13,260 to 11,044 ms in all, load-sheet-close 1,055 to 702, the bench's phone photograph 172 to 140. Every scoreboard, the counts and
   the identify sweep are the same but for time. Two other tries (stopping the code reading early, whole-number moments) measured
   nothing and were taken back.

## Entry 448: the home page's menu above the wordmark, and everything on it tappable (2026-10-19)

1. **The fix.** While the header's menu is open the header is raised above the docking wordmark (`.tp-home:has(.tp-menu[open])`, and a
   `tp-menu-open` class the menu's own toggle sets for a browser without `:has`), and the wordmark waits where it is until the menu
   closes. The menu now closes on Escape (the focus back on its button), on a tap outside it and on choosing a link (the site script).
   **Also found by the check:** once docked, the big wordmark is invisible but was still on top of the header's small GroupLab link, so
   after scrolling that link could not be tapped at any width; the wordmark now takes no taps (`pointer-events:none`).
2. **The check** (`tests/site/taps.mjs`, run in website.yml before anything publishes, in the runner's own Chrome; Edge here): the built
   site served locally and the home page driven headless at 390, 1440 and 1280 px. With the menu open and the wordmark undocked, every link
   is the element at its left edge, middle and right edge; the menu closes again on its button, Escape, a tap outside and a link; with it
   closed, every link, button and summary on screen is the element at its own middle at scroll 0, 150, 300, 450, 700 and 1000. Before the
   fix it named Guides and Research under `H1.cond` at 390 px, as planning measured, and the header's GroupLab link under it at scroll 300,
   450 and 700 at all three widths: 11 faults. After: none.
3. **Live:** see entry 449's results (checked once this published).

## Entry 449: page titles stay as written (2026-10-19)

1. Alan chose A: every page title as written in the condensed bold face, as built in entry 444; the home page keeps GROUPLAB and START
   MEASURING. in capitals. Nothing on the site changed. The DESIGN NEEDED line is marked answered in for-alan.md and gone from STATE.
2. `heading_problems` now also fails a page, other than the home page, whose title a stylesheet turns to capitals (three lines);
   `site-home-tests.py` holds it (50 checks).
3. **Entry 448 section 3, on the live site** (c00232ec, 2026-10-19): grouplab.org at 390 px in light and dark, the menu opened with the
   wordmark undocked: all ten links are the element at their left edge, middle and right edge, and the panel is drawn over the
   wordmark (`docs/figures/site/entry-448/menu-open-390-light.webp` and `-dark`). The tap check passed in CI on its first run there.

## Entry 447 part 2, what was left: six windows and the phones' deeper pages (2026-10-20)

- **Desktop, headless** (`Entry447WindowsTests`, one test a window): importing shot coordinates, Which target is this?, Fudd buster,
  the three group tools (Shots and clicks, Zero from this group, Share a picture), the print confirmation, and the figures in their own
  window, each tabbed all the way round: the focus never lost, something taking it, every stop named as a screen reader reads it.
- **Found and fixed**: the coordinates window's four drop downs and its distance box had no names (now named by the words beside them,
  the box as "Shot at, in yards, needed for mil or MOA"); Fudd buster had nothing that could take the focus, so a keyboard could not
  scroll it and Tab lost the focus (its page now takes the focus, named, and the arrows and Page Down read down it). The other four
  had no fault.
- **Phones, headless at Android's largest text** (`Entry447Tests` on the phone, extended): Fix holes and the readings page from the
  result, the store-bought target's fingerprint from Targets, and the printer check from Settings: every control named, and the
  screen read before the bar. No fault.
- **Still left**: a run with a real screen reader (Narrator, VoiceOver, TalkBack) on each system; that needs Alan's devices, and the
  headless tests stand in until then.

## Phase 9, 2026-10-20: finding the holes on a 600 dpi scan about a fifth sooner, every result the same

- **Measured first**: the slowest step left in reading was finding the holes on the 600 dpi scan (about 490 ms in the bench); inside it,
  besides the alignment (which entry 433 showed cannot be cheaper with the same shifts), the open of the grey residual and the threshold
  after it (85 ms) and setting everything beyond the sheet to paper on one thread (37 ms).
- **The change**: the residual is thresholded and then opened as a 0 and 1 image (`BinaryMorphology.Open`, taken by the imaging backend
  for any 0 and 1 open), which for a flat element gives the same pixels as opening and then thresholding; the pass beyond the sheet runs
  on every core. `BinaryMorphologyTests.ThresholdingThenOpeningIsOpeningThenThresholdingToTheByte` holds it to the byte against OpenCV.
- **Before and after**: the bench's 600 dpi scan, finding the holes, 492 to 378 ms; a phone photograph 143 to 127; `grouplab analyze` on
  the 25 hole 600 dpi sample 1,134 to 1,024. Detail in docs/PERFORMANCE.md.
- **Nothing moved**: the four local corpus scoreboards, the synthetic and any-target scoreboards and both corpus counts are line for line
  the same but for their time columns.

