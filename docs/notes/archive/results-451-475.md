# Phase 1 results, entries 451 to 475

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 450: a flaky test or a slow phone build no longer holds back the other platforms' nightly (2026-10-19)

Sections 1, 2 and 4 were built by a helper in its own worktree (Alan asked for the inbox to be worked in parallel), reviewed and pushed
here as d1c07ea7; section 5's proofs were run here.

1. **One retry of failed tests** (`ci.yml`, all three legs): the test step writes TRX results; when it fails, `scripts/test-retry.py`
   reruns only the tests that failed, once, in the same job, and the leg passes only if every one passes. A crash, a hang, a project
   with no results or more than 30 failures is not retried, and a build failure never reaches it. A test that passed only on its retry
   is a warning on the run, a line in the leg's summary and a row in a `flaky-tests-<leg>` artifact; `scripts/flaky-tests.py` (main's
   runs only) gathers them into `docs/notes/flaky-tests.md`, with a "To fix" list for any test retried three times in a fortnight.
2. **The desktop publish no longer waits** for Android or the arm64 command line: `publish` needs only the naming and the packages; a
   late `android-files` job re-signs the update manifest with the Android downloads and adds the APKs, the AAB and the symbols to the
   build's release and the rolling `nightly` release, and `server-command-line` does the same for the arm64 tarball. Play upload, TestFlight
   and the Store still start only when the whole nightly run has ended, so after the Android files exist. The notes and the release body
   never list files, so a later file does not make them wrong; README, docs/ANDROID.md, docs/UPDATES.md, the download page and the
   `nightly-builds` article say the Android files join when their build finishes. `NightlyNeverWaitsTests` (5) hold it.
3. **Measured** (GitHub keeps runs from 2026-09-15; counted from 2026-09-28, when the signed Android build first ran in the nightly):
   **32 times** a failed "build and test" meant no nightly while the phone builds would have been fine: 20 failed on one or two of the
   three legs (36667907420, 36749517180, 36827714330, 37008809245, 37722546938, 37830239736, 37907966181, 38208601401, 38367496105,
   38391812817, 38398018198, 38526570152, 38686202750, 38694447563, 38998534625, 36369536300, 37002640014, 37199873957, 38136708283,
   38386382784) and 12 only in a job no phone uses (the website job 11 times, the linux tarball's ships check once). No Windows or
   linux-x64 package failed in a nightly. More than two, so said in STATE for planning; the split itself is not built. Also seen: 7
   nightlies failed at "Write the notes" with the Android build fine (36372334414, 36434428741, 36515160656, 36648108371, 37426892451,
   38409950467, 38449280753), and ci.yml's website job gates every platform's nightly and is not helped by the retry.
4. **Proofs** on a throwaway branch (`proof/entry-450`, deleted): with a test that always fails and one that fails only on its first
   attempt (run 39202156149), every leg retried both, the second passed and was recorded (warning and `flaky-tests-<leg>` artifact on all
   three), the first failed twice and the leg failed; with only the second (run 39206563079) every leg passed on its retry and recorded
   it. The real nightly: 0.2.0-nightly.203 (run 39206788728, from d1c07ea7) published the desktop files on its own, and the new Android
   job then failed, because it checked the repository out after downloading the signed files and the checkout emptied them away; no
   Android file reached either release. Fixed in 37326a5c (checkout first, held by
   `NightlyNeverWaitsTests.ALateJobChecksOutBeforeItDownloadsAnything`); the next nightly is the proof that the Android files join.

## Entry 451: the home page's ruler on every page (2026-10-19)

1. **One ruler.** `RULER` in `website/build.py` is the home page's ruler, its twelve numbered ticks written by the build, and both the
   home page and `shell()` use it, so they cannot drift; `page.js` no longer makes ticks of its own and only moves the amber mark, over
   the whole scroll (at the top on a page too short to scroll). It is on every page (`tp-ruled`, which carries the `--rule` margin on
   the header, main and footer), and hidden below 861 px as on the home page. `tp-long` keeps only what long pages do; the numbered section
   index stays the home page's. Nothing is fixed to the right edge in the tour's, how it works' or the download page's own styles.
2. **Held:** `header_problems` fails the build for a page without the ruler; `site-home-tests.py` (50) holds that a short page has it
   and no section index; the tap check (`tests/site/taps.mjs`) still finds nothing covered.
3. **Pictures** in `docs/figures/site/entry-451/`: download, tour, tour/how-it-works, targets and the guides index at 1400 px, light and
   dark, and targets at 390 px with the ruler hidden. Live check: below, once published.

## Entry 454: a phone updates to the newest nightly that has an Android file (2026-10-19)

1. **Nightly 203 repaired.** Its signed Android files were still on run 39206788728 (until 2026-10-20 06:00 UTC). The Android job's
   gathering, signing and uploading is now one script, `scripts/nightly-android-files.sh`, run by nightly.yml and by a new dispatchable
   `android-files.yml` that takes a nightly run and its version (26be6361). Run 39223723577 added the APKs, the AAB and the symbols to
   `v0.2.0-nightly.203` and the moving release and signed the manifest again: both formats now list `apk-dev` and `apk` for 203. A phone
   on 202 or earlier is offered 0.2.0-nightly.203. Rerun once this change is on main, so 203's manifest also carries the new per-platform
   list that 204 carries forward.
2. **Each platform follows its own newest build.** The second-format manifest gains `platforms`: per platform, the newest build with a
   file for it, its commit and date, and its files on that build's own numbered release. `scripts/nightly-manifest.sh` passes this
   build's numbered files (`--own`) and the manifest before it (`--carry`, this build's own when the Android job signs again, the moving
   release's when the publish job signs first); a platform this build has no file for keeps the earlier entry when it is an older build,
   and a manifest before that does not verify with the key carries nothing. The first format is unchanged (`Legacy()` strips the list
   and the command line refuses to sign it there).
3. **Both updaters compare with their platform's build**: `UpdateManifest.OnPlatform`, used by `UpdatePolicy.Decide`, `UpdateRun` (the
   desktop passes its platform) and `AndroidUpdates.CheckAsync`. Equal or older is never offered.
4. **Older builds:** they ignore the new field (the second format is verified over the bytes as written), and the top of the manifest
   lists an Android file only when it belongs to the top build, so an older phone is never handed an older APK under a newer number and
   cannot loop; it waits, as before, for a nightly with its Android file. `NewestForEachPlatformTests` (8): no Android file, a late one,
   a phone on the build before, one on the newest Android build, a manifest with no Android build at all, an older build's record
   reading the new manifest, the desktop for its own platform, and the carrying rules. Core suite 3040 passed.
5. **Words:** "You have the newest GroupLab Dev for Android, 0.2.0-nightly.202." (`AndroidUpdates.Newest`), shown by the phone for
   nothing newer and for a manifest that names no Android build; no other platform's number.
6. docs/UPDATES.md, docs/ANDROID.md, the nightly-builds article and the download page say so (written by the docs helper, reviewed);
   the Android updates feature claims the note.
7. **Proof, a dry run** with a throwaway key and stand-in files through the real command line: 203 signed with Android, then 204 with
   the Android build failed: 204's Android entry stayed on 203 (`v0.2.0-nightly.203`'s own file), `update-check --as android
   0.2.0-nightly.202` was offered 203, a copy on 203 nothing, Windows on 203 was offered 204, and the first format carried no new field.
   Alan's tablet should now reach **0.2.0-nightly.203**; the first real nightly after this change is the live proof.
   Live: nightly 204 (run 39231460564) was signed first with Android's entry carried from 203, then moved to 204 when its Android
   files arrived three minutes later; a phone on 203 is offered 204.

## Entry 452: the website check and a refused note stop costing nightlies (2026-10-19)

1. **The website job no longer gates the nightly.** It moved out of ci.yml ("build and test", whose success starts the nightly) into
   `.github/workflows/checks.yml` ("checks no nightly waits for"), under the same name, so it still runs and fails on every push and pull
   request; website.yml's own gate is unchanged. "build and test" now holds the three test legs (each retrying a failed test once, entry
   450) and the jobs that build what ships: the Windows package, the linux tarball, the intake worker and the test data.
2. **The linux tarball's ships check** (`shipping-gate.py --check-generated`) compares the list of what ships with what the build reads.
   That list decides whether a nightly is built; it is not what the Linux download contains. So it moved to checks.yml as "ships list".
   The tarball job itself, which packs the Linux publish and checks it carries its native OpenCV library, stays a gate.
3. **"Write the notes".** All seven runs (36372334414, 36434428741, 36515160656, 36648108371, 37426892451, 38409950467, 38449280753)
   failed for one cause: the checker refused a trailer ('folded', "did not change", 'TestFlight' five times, 'millimetres', 'centres'),
   and it ran only after every build had finished. Fixed at the cause: checks.yml's "release notes" job runs `release-notes.py --check` on
   every push, so a refused note is red on its own commit and is put right in `docs/release-note-corrections.json`. And if one still
   reaches the nightly, it publishes with `release-notes.py --fallback` (the version, the commit, a link to the list of changes since the
   build before) and a warning annotation and summary; `docs/RELEASE-NOTES.md` takes the notes the build went out with
   (`notes-written.md`) instead of asking the checker again. `NightlyNeverWaitsTests` (8) holds both.
4. docs/UPDATES.md and the nightly-builds article say what decides a nightly and what happens to a refused note (written by the docs
   helper, corrected: 12 nightlies were lost to the website job and the ships list alone, not 32).
5. **Proofs.** Throwaway branch `proof/entry-452` (deleted): with the site build made to fail, "checks no nightly waits for" failed on
   "website builds" (run 39226777937) while "build and test", the nightly's trigger, passed on all three legs and every build job (run
   39226778099). The notes fallback, as a dry run of the nightly's own step on a commit carrying a refused note ('centres'): it exited 0,
   wrote the short notes, an empty notes history, the warning and the summary, and `--check` named the same commit. No release was made.
6. **The first nightly after 37326a5c**, 0.2.0-nightly.204 (run 39231460564): the desktop files were published at 09:44:37 UTC with
   Android's entry carried from 203, and the Android files joined both releases at 09:47:36, Android's entry moving to 204.

## Entry 453: a thumbnail for every research article (2026-10-19)

Alan chose concept C: keep the charts, give every other article its own picture. Built by a helper in its own worktree (Alan asked for
the inbox to be worked in parallel), reviewed here picture by picture and taken as 7e4b1719. Website only.

1. The 15 lead figures are unchanged. The build agreed with the entry's list of 21 plates.
2. Every plate is gone. Each of the 21 has `website/research/<slug>/thumb.webp` (a real picture) or `thumb.svg` (a drawn mark),
   inlined on `var(--sunk)` in the 320 by 200 box, aria-hidden, colored only through the entry's eight classes. `research_lead` stops
   the build on a published article with neither a lead figure nor a thumbnail; `thumb_problems` checks every thumbnail (the box, the
   classes, no words or fixed colors, one solid amber element at most, a picture WebP 640 by 400 with no EXIF, XMP or ICC).
3. Of the four canvas marks, smaller-installer is used as given; real pictures replaced one-hole-or-two, hole-is-not-the-bullet and
   wrong-bull, as section 8 allows. The canvas marks carry dashed amber guides, so the rule counts solid amber elements only.
4. Drawn marks (7): safe-updates, what-grouplab-sends, nightly-builds, smaller-installer, uploads-rebuilt-from-pixels, shots-to-zero (a
   calculation with no sheet behind it) and bent-sheet-registration (its own pictures are kept off the site).
5. `research-plate` and its CSS are removed.
6. Proof screenshots of the whole Research page at 1440 px light and dark and at 390 px are in `C:\Dev\grouplab-local\research-thumbs\`,
   with `which-is-which.txt`. Live check: below.
7. SCREENSHOTS.md and the site notes never described the plates. Nine card keys in docs/claims-backing.json were renamed with the
   card text.
8. Real pictures (14), crops of Alan's own scans and photographs from the 2026-09-20 range trip, the 2026-09-26 aim point card, the
   2026-10-04 corner bracket photograph and the 2026-10-08 M834 check print, each turned upright and rebuilt from pixels with no
   metadata; sources, sizes and SHA-256 in samples/PROVENANCE.md, "Entry 453". Checked here: nothing in any crop shows a name, an
   address, a serial or a QR code; nothing is a tester's, from Discord or the 2026-09-16 friend scan. Two no_figure lines the pictures
   made untrue were corrected (primer-comparison, how-far-off-square).
9. Real pictures: one-hole-or-two, hole-is-not-the-bullet, blank-sheet-zero, scale-markers-beside-a-target, how-far-off-square,
   how-grouplab-reads-a-target, wrong-bull, scans-against-photos, pooling-groups, wind-or-rifle, primer-comparison, choosing-the-markers,
   designing-a-readable-target, phomemo-m834-print-commands. Drawn: the seven in section 4. Lead charts: the other fifteen.

Checks: the site build, the four site suites, the tap check, claims (all backed), readme and consistency pass. Worth an article? No: a
presentation change with no measurement in it.

## Entry 455: maximum point blank range, a third view in Ballistics (2026-10-19)

1. **The model** (`src/GroupLab.Core/Ballistics/PointBlank.cs`): the trajectory flown once from the rifle's zero, every other zero the
   same flight tilted; the chance of a hit at each distance from the mean radius (per-axis sigma, MR = sigma times the root of pi/2,
   growing with distance) and the velocity SD carried through the solver as a vertical spread, aimed at the zone's center; a circle by
   48-point Gauss-Legendre over the part of it the normal has weight in, a rectangle as two intervals. Classic MPBR for reference, the
   certainty band, the three ways to set the zero (longest dead-on, most likely distance, a window, with the refusal when no zero holds the
   window) and the rifle's present zero beside them; what ends it (certainty, an energy floor, the bullet's least expansion velocity,
   each named); 100 % refused in words.
2. **Against planning's numbers** (Test load, 8 in circle, 0.63 MOA): classic start 0, near zero 25 yd (26), peak +4.00 in at 148 yd,
   far zero 266 (266), end 313 (312), +3.38 in at 100; 90 % 0 to 263 yd (262) at +2.60 in, 95 % 251 (252), 99 % 228 (228), 99.9 % 201
   (200); the 100 yd zero 0 to 207 yd by the classic rule (208); most likely 200 yd +1.76 in (1.77), 99.93 % at 200, band 0 to 237 (236);
   window 150 to 300 best worst case 58.8 % (56). Prairie dog starts at 15.5 yd (about 16). All within 2 yd. `PointBlankTests` (15).
3. **The words, the same everywhere** (`PointBlankReport`): "Dead on 0 to 263 yd", "9 in 10 shots in an 8 in circle", "Zero 2.6 in high at
   100 yd: 0.72 mil, 7 clicks up from your 100 yd zero.", the ending, the wind's drift at the far end with a warning past half the zone's
   width, the present zero, the classic figure, the law line for hunting targets, what is not counted.
4. **The targets** (`PointBlankTargets`): the eleven sourced game zones (the smaller figure where sources disagree), five estimates
   labelled as such and editable, the IPSC A zone, the USPSA head A zone, the IDPA body, two steel silhouettes said to be a vendor's
   shape, your own circle or rectangle; drawn to scale in the list. Energy floors 1000 and 1500 ft lb as rules of thumb.
5. **The computer** (`MainWindow.PointBlank.cs`): the third view with its settings on the left, the answer card, a picture of the path
   with the zone and the dead-on stretch, Print a card (a 203 dpi picture and a PDF), Keep this zero on the rifle and Put back.
   **The phone**: Ballistics' Point blank tab, the same inputs and words, the card shared or printed on the M220 over Bluetooth (when
   50 by 30 mm labels are loaded), keep and put back. `Entry455Tests` on both (3 and 1).
6. **The card** (`PointBlankCard`): 50 by 30 mm, six lines, nothing round; `TheCardIsFiftyByThirtyAndEveryLineFits`.
7. **Records, schema version 4**: a load's least expansion velocity (Equipment, Ballistics' load section, the phone's load form, the
   export) and a rifle's previous zeros (kept as text so equal histories compare equal), with the upgrade, docs/SESSION-SCHEMA.md and
   the export accepting versions 2 to 4.
8. **IPSC in Hit probability** is now the official A zone, 15 by 32.5 cm; GroupLab's own outline is gone. The rest of the outline is
   question 102.
9. **Docs** (docs helper, reviewed): the user guide's Point blank range part with From the keyboard and the sources, its PDF, the
   Features entry with its picture (`ballistics-point`), the tour, README, and the research article "Point blank range, honestly",
   published under entry 393's rule (every figure held by `PointBlankTests.TheArticlesFiguresHold`). Worth an article? Yes, written:
   the classic zero keeps 46 % of shots in the circle at its peak and 42 % at its far end.
10. **Alan's proof**: request 104, the card on his M220 once a nightly carries it. Request 103 (entry 456's Top Gun question) was
   written at the same time.

## Entry 456: rail cant, sight scale factor and Top Gun, from Alan's pissinhot.com calculators (2026-10-19)

The formulas are the entry's own account of the three pages. Reading the pages here: the safety check refused the fetch of the scope cant
page, and the other two came back as page text without their scripts; the entry's formulas were complete, so nothing waited on it.

1. **Rail cant** (`ScopeMount`, the rifle page's Scope and mount section): the elevation the zero uses is GroupLab's zero launch angle
   (the one deliberate difference: his page adds a vacuum drop angle; for the Test load at 100 yd the page's formula gives 4.053 MOA and
   the solver 4.012), half the travel (or the most up from the zero plus the zero's, his second mode), the most rail cant that still
   zeroes, the elevation left above the zero with the rail's cant and the farthest range it reaches with a chosen load (1550 yd for the
   screenshot rifle, 30 mil and a 6 mil rail), and the plain refusal when the rail is canted past what zeroes. Ballistics uses it: the
   Trajectory table's clicks say "out of elevation, hold X mil in the reticle" past the last dialable range, At one range says the same,
   and Point blank says when its zero needs more than is left; the phone's dope table too.
2. **Sight scale factor**: a rifle field and a tracking-test helper (expected 20.0 mil or 66.0 MOA, the turret's reading, the factor,
   the error, the band: under 0.5 % essentially perfect, under 2 % normal, 2 % or more significant, and the steps). Every click is the true
   angle over the factor: `Clicks.For` (Trajectory, At one range, zero carried, Point blank and its card), the analysis's `FourUnits.Clicks`,
   Shots Needed to Zero (`SigmaClicks`), the phone's dope table; "SSF 0.980 applied" said once beside them. **Later idea, not built**:
   tracking measured from a scanned tracking target, which at 20 mil and 100 yd is 72 in tall (tiled pages or a plotter); in STATE.
3. **Top Gun**: a rifle field, weight as fired; the figure as Litz states it on the rifle page for each load, in the analysis's advanced
   figures (computer and phone) and in Compare loads (both), never coloured or worded as a pass or a fail. Not converted to mean radius and
   not compared with GroupLab's figures: `TopGun.MetricConfirmed` is false until request 103 is answered, and the "clearly worse than the
   trend" pointer (`TopGun.WorseThanTrend`, no pseudoscience) waits on it too. The combined dispersion table is left out.
4. **Records, schema version 5**: `scope_travel`, `scope_travel_kind`, `rail_cant`, `sight_scale_factor`, `weight_as_fired_lb` on rifles,
   through `EquipmentForm` (with a Section heading for Scope and mount), the record book file, the export and import (versions 2 to 5), and
   the phone's rifle form. A desktop rifle save now keeps the rifle's previous zeros (entry 455), which its form does not show.
5. **Phone parity**: the same fields, figures, tracking helper and Top Gun in Ballistics' rifle form; the dope's holds; Top Gun in the
   result and Compare. **Docs** (docs helper, reviewed): the guide's Scope and mount part with the credits (pissinhot.com, Bryan Litz), its
   PDF, the Features entry with its picture (`equipment-scope`), the tour, README, PHONE-PARITY.
6. **Tests**: `ScopeMountTests` (7: the page's worked example against the solver's angle, both travel kinds, the refusal, the hold, the
   tracking bands, the factor in every click count, Top Gun's arithmetic), `Entry456Tests` on the computer (2) and the phone (1), and
   the equipment form's round trip with every new field.

## Entry 457: the Next unsure hole error, a Send button on every report, and error reports triaged as they arrive (2026-10-19)

1. **The error** (issue 37, nightly 204 on Windows, display scale 1.5, four times in one session; Alan's logs read on this computer):
   planning's reading was right. The focus keeping (`KeyboardFocus.Keeper`) recorded place -1 for a control whose visual parent is a panel
   but which is not one of its Children, and `Restore` read `Children[-1]` after Next unsure hole and after the autosave. A place is now
   recorded only when it is one and read clamped, and `Restore` never throws (one warning in the log). The unobserved-task copies came
   from Avalonia faulting the posted operation's own task as well as raising the error, so a `Restore` that cannot throw ends both; that
   is also issue 9's return on nightly 204. Issue 36 (GroupLab Dev on Android, a tap on the result's sheet near nothing) was MinBy over
   none; it now finds none. Fixed in 0af39fc2, shipping in the nightly after 205; `Entry457Tests` on both reproduce each fault on the
   code before and pass now (the first version of the desktop test passed on the old code too, and was rewritten until it did not).
2. **Send on every report**: the report window's first button is Send whenever the error receiver is open; it goes through the same
   receivers as the automatic error reports (`ErrorReports.SendDiagnosticsAsync`, now with the report's own error and its run's logs),
   with what the person wrote; a contact stays in a saved report only. The email and GitHub sentence shows only when sending is not open
   or a send fails, and says why. `crashReportUrl` and its uploader are retired. The crash banner, asked each time, has Send the error
   report first and primary; sending automatically, it says the report has gone or goes within a minute. The phone, asked each time, puts
   the same offer in the note along the top. `Entry457Tests.TheReportWindowSendsFirstAndTheWayRoundIsOnlyTheFallback`.
3. **Triaged as they arrive**: CLAUDE.md now lists the open issues before each inbox entry, before ending a turn and at least hourly
   while waiting on CI, and assesses each at once. `docs/notes/error-reports.md` holds issues 37, 36 and 9 (37 also Alan's own report);
   STATE's crash line points to it. The three issues close when the nightly with 0af39fc2 is out.
4. **Docs**: the guide's Report a problem part, the phone's part, the error reports feature's note and the support page say a report can
   be sent from GroupLab, with GitHub and email as the fallback.

## Entry 458: release notes say what people's reports and targets changed (2026-10-19)

1. **Where a fix's report came from**: `scripts/release-notes.py` reads `Reported-via:` (automatic error report, error report sent from
   GroupLab, GitHub issue, email, Discord, with counts) and puts it on the fix's line, "(found from 5 automatic error reports)"; any other
   word, a name among them, is refused. A commit that closes a crash report issue without it fails the release notes check on every push.
   Entry 457's fix (0af39fc2) already carries it, so the next nightly's notes say "found from 5 automatic error reports". People who
   agreed to be named (Quack, Jylee, Unholy, Fenix, Justin) stay in the commit's prose only; the trailer never carries a name.
2. **Detection changes from targets**: `From-submissions:` says how many targets people sent went into a change ("made with 48 targets
   people sent" on its line). The learning loop writes its commit's note from its own result (`learn.py release_note`): the targets and
   where they came from, how many held back, how many more holes and fewer false marks on those (public totals only, from the tuning
   result's new numeric fields), and what changed in plain words from `KNOB_WORDS`, never a constant's name; no change, no commit. The app
   now records one word with each target, automatic or by hand (`TargetPackages.HowSent`), and the receiver keeps it in `meta.json` beside
   `source` (app or web); targets from before say "people sent" without the split.
3. **The running total**: each build's notes end with "From your reports and targets in this build: ..." when there is any; the releases
   page has "What your reports and targets changed" (`/releases/#from-you`), the README's What is new carries the total once a build has
   counted, the Support and Send a target pages link to it, and the Discord announcement carries the build's line with its notes.
4. **Checks and docs**: the notes self-test covers both trailers, the counts, the total, a name refused and the crash report rule;
   `SendEverythingTests.APackageSaysHowItWasSentInOneWord`. CONTRIBUTING.md says how to write the trailers and the nightly-builds article
   mentions the counts (docs helper, reviewed).

## Entry 459: consistency audit of 2026-10-19 (2026-10-19)

Wording by the docs helper (docs-sonnet), reviewed; sections 6, 7 and 11's last bullet on the main model.

1. **Trying GroupLab** (TESTING-GUIDE and its PDF) says to write what happened and press Send; Save only writes a zip; GitHub or email
   only when sending is not open or fails.
2. **The platform statement** (PLATFORM-SUPPORT.md, so README and /download/): reports are sent from GroupLab with Send, the Discord
   bug-reports forum works too, support@grouplab.org for anything private.
3. **Phones and the log**: the user guide now says what the code does (`SettingsView` offers "Send them with the log" behind the same
   switch as the computer).
4. **Store-bought targets**: the user guide names the one route that exists, email to support@grouplab.org (the Send a target page
   takes images only and the app only saves the file); "no way to send one in yet" retired.
5. **Home page**: only the Windows and Linux downloads are called unsigned.
6. **Website notes in the application's notes**: nightly 205's entry now says it changed nothing in the application (only the consent
   record beside the Windows sample is longer), and 206's bullet says what the application does (a sent target records how it was sent),
   both also in `docs/release-note-corrections.json`. The cause: `samples/PROVENANCE.md` ships in the Windows zip, so entry 453's
   consent records for the research pictures made 205 a real build whose only note was the website one. Question 105 proposes shipping
   only the sample's own record.
7. **The site a nightly behind**: published by hand for 207 (live at f094544b with 209). The cause: the nightly's notes and README pushes
   use its own token, which starts no workflow, so the paths filter never saw them. The publish job now dispatches website.yml after the
   README (`actions: write`); `WebsiteWorkflowTests.NoOtherWorkflowStartsTheWebsiteWorkflow` allows exactly that one step, and
   `NightlyNeverWaitsTests.TheNightlyPublishesTheSiteAfterItsNotes` holds it.
8. **Not built yet**: both README lists name hole detection on plain paper, in plain words.
9. **Phases 6 and 8**: one sentence under README's table says why a phase whose gate is met stays In progress.
10. **Samples**: the Windows zip and installer carry the two sheets (the installer is built from the same staging folder); macOS, Linux
    and the phones do not; README, /download/ and TESTING-GUIDE say so.
11. Tour Ballistics blurb names Point blank; the Error reports card names Send (reworded since 207); the phone lists name Point blank;
    for-alan.md's first line rewritten from the open requests, and request 104 corrected to nightly 206 (Point blank is not in 205).

## Questions 102 to 105, answered by Alan in the session (2026-10-19)

- **102**: keep the A zone only. Hit probability's IPSC choice stays the official 15 by 32.5 cm A zone; no other zones are added.
- **103**: keep the credit as it is. The guide and README say the three calculators began as ones GroupLab's developer wrote before
  GroupLab, and the old site is not named.
- **104**: keep it. The Real taps check goes on waiting out a measured pause in the simulator's drawing, and saying so.
- **105**: yes. The Windows package now carries only the sample's own consent, `samples/sample-consent.md`, under the name
  `samples/PROVENANCE.md` it always had in the zip. That section is a committed file rather than one cut out at packaging time,
  because the shipping gate decides by path: `samples/PROVENANCE.md` left the ships list and `samples/sample-consent.md` joined it,
  so a change to the website's consents no longer makes a build. `ReleaseAssetTests.ThePackageCarriesItsLicenceItsNoticesAndItsSamples`
  holds the file to the full record's first section word for word, and `ShippingPathsTests` holds which of the two ships.

## Request 103, answered by Alan: Top Gun is five-shot extreme spread, and groups are now set beside it (2026-10-20)

- **The answer**: Litz's Top Gun group size (Modern Advancements in Long Range Shooting, Volume III, chapter 3) is the extreme spread
  of five-shot groups, averaged. Request 103 is closed and archived in `docs/notes/for-alan-archive.md`.
- **The comparison**: `TopGun.OnItsBasis` gives a group's own figure on that basis, the average five-shot extreme spread its sigma
  implies: the range-statistic table's five-shot mean (3.066 at sigma 1) times the group's Rayleigh sigma, in MOA at the distance, with
  the sigma's interval carried through. So a 20 or 50 shot session is compared fairly, not by cutting it into fives. `TopGun.Compared`
  says "clearly wider" (with entry 456's list of what is usually checked) or "clearly tighter" only where the whole interval is on one
  side of the trend, and otherwise "not clearly different at this many shots". No distance, or under five shots, and no line is shown.
- **Where**: the analysis's Advanced figures on the computer and the phone (`ResultFigures`, key `topGunYours`; `MainWindow`), and
  Compare loads on both, each group under its load's trend (`TopGun.Lines`, now given the groups and the distance).
- **Proof**: `ScopeMountTests.YourGroupOnTopGunsBasisIsTheAverageFiveShotSpread` draws 40,000 five-shot groups at sigma 0.5 in at 100 yd
  and their average spread (1.4668 MOA) lands within 0.01 MOA of the figure on its basis (1.4639); `ResultFiguresTests.AdvancedSetsTheGroupBesideTopGunOnItsBasis`.
- The guide, README and the Features sentence say it; `TopGun.MetricConfirmed` is gone, and every "average group" now reads "average
  five-shot group".

## Entry 460: questions 100 and 98 answered by Alan from the planning canvas (2026-10-20)

1. **Question 100, option B.** The dark themes' selection is now #f800f8 (`Tokens.Selection`), 108 or more from every plot ink and bull
   mark where #ff3df5 was 84 from the violet bull mark; edged in black, 6.3 to 1. The light theme's #b0009f and its fourth pooled
   sheet's #9c3f8f are kept. `Entry425Tests` now holds the dark selection 100 from the bull marks and the light one at 80, and the light
   plum pair at 65, as answered. No guide, page or release note named the old value. Question 100 archived.
2. **Question 98, option B.** Two or more pictures of the same single-page design dropped together (codes read, no set sheet number) are
   sorted as copies (`DropSorting.Copies`) and asked about once with Alan's words (`CopiesQuestion`): "Pool these 3 pictures as one
   group?", "They are three copies of the same page, GroupLab 3x3 2 MOA, Letter. ...", **Pool as one group** (the default) or **Keep a
   tab each**. Pooled, they open in one tab named "GroupLab 3x3 2 MOA, Letter, 3 copies pooled" and pool as pictures with no codes do
   (`SetPool.CompositeOfPictures`), but each is read and its holes found as it opens, since its codes read (`SetGroup.CopiesOf`); a later
   copy dropped onto that tab joins it. Kept, a tab each as before. On the phone, copies chosen together are asked the same question
   (`CapturePage.Copies`, `CapturePage.PoolCopies`) and pooled open as one group on the result screen. Tests: `Entry460Tests` (sorting,
   the words, a drop of three 2 MOA copies with both answers) and `Entry460PhoneTests`; the entry 423 drop tests still pass. The guide's
   drop paragraph says it. Question 98 archived.

