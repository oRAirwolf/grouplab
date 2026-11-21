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

## Entry 461 part 1: every research row that said "not yet" decided (2026-10-21)

Ten rows of `docs/RESEARCH.md`'s "Worth an article?", each checked for whether its awaited data now exists:

- **Written and published (two new articles and one update):** `/research/recognizing-store-bought-targets/` (entry 332 and question
  87, decided on what exists as entry 341 says: the scale is measured only against the learned sheet, and the article shows the
  warning a shooter checks; no maker's artwork); `/research/opencv-on-android-from-dotnet/` (entry 198: the build, its two traps and
  the Fold 7's first run); and two new sections of `/research/wrong-bull/` (entries 374 and 376 Part A: a row higher fits exactly as
  well, the shooting order breaks the tie, and a real tie is asked). Both new articles have a drawn thumbnail, are listed in
  `website/research/PUBLISHED.md`, and are backed by rules in `docs/claims-backing.json`; the research card's sentence is backed alone.
- **Not written, with the reason in the row:** entry 410 section 1 (already decided twice since, entries 415 and 434), entry 362
  (five photographs on one counter; request 74 never came), entry 261 (still synthetic), entries 196 and 197 (synthetic; the only real
  sheet is the orange commercial target), entry 344 (no real photo of a real poster), and entry 158 program A step 2 (the orange
  commercial target, and a scan detector on photographs).
- Checks: the site builds; `claims.py --check` all backed; consistency 0 findings; README checked.

## Entry 461 part 2.1: screen reader audits on all three platforms (2026-10-21)

- **The computer** (`tests/Shared/ScreenReaderAudit.cs`): Avalonia hands Windows UI Automation its automation peers, so the peer tree
  is what Narrator reads. Every window the entry 447 tests tab round (the eight places, the report, the printer check and the six
  windows of `Entry447WindowsTests`) is walked: every control a person presses, types into or chooses, shown and enabled, must be in
  the control view (not Raw), carry a name in words, a control type that says what it is, and be reached by Tab (a stop, or inside a
  stop that takes it with the arrows). `Entry461AccessibilityTests` holds the audit to a window made to fail it.
- **What it found and was fixed:** a button or choice holding a panel was named by Avalonia after the panel's type, so Narrator read
  the printer check's four ways to print as "Avalonia.Controls.StackPanel" and three marking buttons as "Avalonia.Controls.DockPanel";
  the old tab round missed it because the name was not empty. `SpokenNames` (desktop and phone) names each such control from the words
  inside it unless the code named it, and keeps it current when the content changes; a type-shaped name now counts as no name in both
  checks. On the phone, Settings' "Run the benchmark now" was 44 tall, now 48 (`BenchmarkPanel`'s `runHeight`).
- **The phones, headless** (`Entry461PhoneAccessibilityTests`): the first run and every place, the same walk with a 48 by 48 touch
  target: no other finding.
- **The phones, on the platforms' own trees:** a scenario step `audit` (`Scenario.Audit`) stops on each of the sweep's 13 screens
  (`scripts/scenarios/phone-audit.json`, held to the sweep by `ScreenReaderAuditTests`); `scripts/screen-reader-audit.py` reads
  Android's accessibility node tree with uiautomator (every node that can be tapped named, with a role, 48 by 48 dp, and every control
  GroupLab Dev says can be tapped present in the tree) and runs XCTest's `performAccessibilityAudit` through the XCUITest runner
  (element detection, hit regions, descriptions and traits judged; contrast, Dynamic Type and clipped text kept in the artifact). New
  steps "Screen reader audit" in android-emulator.yml (`scripts/android-audit.sh`) and ios-app.yml.
- **The first runs (39539794514, 39539797209).** Android read all 13 screens: every node named, every role given, and two findings
  that are GroupLab's, "More ▸" 47 dp wide (now at least as wide as tall, `MoreFold`) and the bull buttons of "How you shot this sheet"
  44 dp tall (now 48, their gap 1 where it was 3, so the grid keeps its height). The rest was one fault: **every control's box in
  Android's accessibility tree sat 128 pixels, the status bar, above where it is drawn**, because Avalonia 12.1.2's helper sets each
  box on the screen from the control's place in GroupLab's view and never adds where the view is (it calls `SetBoundsInScreen` and never
  `GetLocationOnScreen`); TalkBack's outline was drawn above what it read, and Android cut the top and bottom controls to slivers.
  `AccessibilityOnScreen` wraps Avalonia's helper on the Android head, answering exactly as it does and moving each box by the view's
  place before Android clips it. A scroll bar's own arrows are left out of the judging, as on the computer. iOS: the runner did not
  build, because XCTest has no audit types for actions or parent and child; they were taken out.
- **The second runs (39544356145, 39544358247).** Android: the wrapper installed (`accessibility.bounds installed=True` in GroupLab Dev's
  log) and the 100 misplaced boxes were gone; the three left were controls cut off by the bottom of the scrolled view, whose box is the
  sliver showing, now not judged for size, as the Accessibility Test Framework does not. iOS read all 13 screens: 20 issues. Fixed:
  empty lines of text (a status line with nothing to say yet) were read as elements with no description, now kept out of the tree while
  empty (`SpokenNames.Quiet`); "grouplab.org" alone was not a name a person can say, now "GroupLab's website, grouplab.org". Not
  judged, kept in the artifact: the scroll bar, a 16 point strip down the right edge, and text XCTest found inside the photograph of
  the sheet on Fix holes (its handwriting and printed labels), which no label can represent and the words above it say.
- Request 105 in for-alan.md: a later twenty minutes with a real screen reader, for what a machine cannot judge. The guide's
  keyboard part says what is checked and that no person using a screen reader has tried it yet.

## Entry 461 part 2.2: a phone photograph's codes, measured (2026-10-21)

- The bench's photograph reads its codes square on in 662 to 861 ms (`grouplab analyze`, three fresh runs): the marker search 68 to
  92 ms, placing and cutting out the 8 places about 75, and about 450 ms in OpenCV's two readers failing in full on the 6 places with no
  code (WeChat 44 to 68 ms each, the second reader 5 to 35), where the other sheets sharing the frozen sheet's markers print theirs.
- **Nothing changed:** a failing read is the reader's fixed sequence, and reading fewer places changes how many codes a picture is said
  to have read (entry 415's finding). Question 106 puts the one real option to planning, with what it would cost; the bench baseline is
  unchanged. Detail in docs/PERFORMANCE.md. QR codes are still read on one thread.
- Worth an article? Not written: ordinary engineering, and the table is in PERFORMANCE.md.

## Entry 461 part 3: synchronization through the person's own cloud folder, option A, the engine (2026-10-21)

- **Decided:** option A of docs/notes/sync-options.md (Alan, 2026-10-20); recorded there, in DESIGN.md section 18 and the decision log.
  Option B is not decided and not built.
- **Revisions (3.1 item 1):** schema 6 adds `sync_records`, `sync_seen`, `sync_conflicts` and `sync_kept` (docs/SESSION-SCHEMA.md); the
  first open of the new build copies `grouplab.db` to `grouplab.pre-v5.db` before upgrading, as every schema change does. Each record
  the export carries has a version vector (`VersionVector`); a device finds its own changes by each record's SHA-256 at every run.
- **Change files (item 2):** whole records with their identity (`DataExport.Identity` for a session) and revision, in
  `GroupLab sync/changes/<device>/`, written only by that device, beside their name and renamed into place, at most 25 sessions a file,
  ending `"end": true`.
- **Reading and merging (item 3):** `SyncEngine.Run`, at start and every five minutes once a screen calls it (`SyncEngine.Every`);
  newer wins, the same record changed on two devices is kept both ways and listed, a deletion against a change keeps the change and
  lists it on both, deletion markers kept 90 days in the database and always in the change files. `SyncEngine.Resolve`: keep this
  one, keep the other, keep both (sessions), understood. Every record a merge replaces or takes away is kept whole first (`Kept`).
- **Tiers (item 4):** geometry and the proof picture always; the originals only with `SyncOptions.Originals`, to
  `GroupLab sync/originals/<sha256>.<ext>`; the photograph's path on a device never travels.
- **Reaching the folder (item 5):** `DirectorySyncFolder` (the computer, and iOS once a bookmark has opened it), `SafSyncFolder`
  (Android's folder picker, the Storage Access Framework, with its permission kept), `BookmarkSyncFolder` (a security-scoped bookmark
  to a folder in Files). The two phone ones build in CI and are reached by nothing.
- **Tests (item 6):** `SyncEngineTests`, twelve: two devices (a session with its strings, a rifle, a sheet, a setting), three devices,
  the same session changed on two (listed both sides, chosen, settled everywhere), keep both, a deletion against an edit, a half-written
  change file read once whole, a device back after 200 days (the deletion still reaches it), a folder that disappears (nothing changes),
  nothing lost (a load and a sheet kept as they were), originals only when asked, each device only its own files.
- **`grouplab sync <database> <folder>`**, with `--list` and `--choose`, for testing. Nothing on any screen, setting or menu; the
  README and the site say nothing of sync.
- **Measured for the screens (3.2):** on this computer's 22 sessions, a proof picture's median is 226 KB, a marking's 38 KB and a
  design's 7 KB: about 350 KB a target in the folder, 70 MB for a year of 200. The DESIGN NEEDED in for-alan.md carries the figures.
- **Not built:** tidying old change files; the three screens (DESIGN NEEDED).

## Entry 462 section 1: a full audit of grouplab.org (2026-10-21)

**How.** `scripts/site-audit.cjs`, new, opens every page the builder makes (79) in Chrome at 390 and 1440 px in both themes and reports
what a reader would hit: the page wider than the screen, words cut off inside a box, words drawn over words, a picture or file that does
not load, a link to a page that is not built, and every axe-core finding for WCAG 2.1 A and AA. Anything not on the site is refused, so a
slow outside server cannot hold a page. External links were fetched one by one (165, all answer) and the download page's ten files were
compared with the rolling nightly's assets (all there; nightly 214 named). Lighthouse 12.2.1 ran on six pages.

**Found and fixed.** Eighteen research articles, What can be measured and the hole detection deep dive were wider than a phone (file names in sources and
code did not wrap); two research articles offered JSON downloads the builder never copied (suppressor shift, photo hole size); the upload
page's file field had no label; the tour diagram marked its chosen link "pressed", which a link cannot be (now "current"); ten tables and
one listing scrolled sideways with no way to reach them by keyboard (each that scrolls now takes focus); the tour's cards skipped from the
page title to a third level heading; a table caption on the hole detection page shrank to a word per line on a phone; two small lines
under 4.5 to 1 now use the theme's existing dim and on-amber colours. The tour's words were checked against its pictures: five pages fixed
(the key is bottom left, Compare's plots and figures, Fingerprint's five ways to give a length, the optic's "Save to your own sheets").
Features' platform lists were checked against PHONE-PARITY.md and about 30 of its on-screen names against the source; none wrong
(the demo tour does have thirteen steps). The release notes page names nightly 214; the research index has all 39 published articles,
each with its picture. `claims.py --check`, `consistency.py` (with the live site) find nothing; the stale consistency issue was closed.

**Waiting on a look decision** (for-alan.md, DESIGN NEEDED of entry 462 section 1, pictures in `docs/figures/site/entry-462/`): links
inside sentences in the dark theme (about 2 to 1 against the text, no underline; 101 links) and amber words on How it works' grey panels
in the light theme (4.19 to 1). `website/a11y-pending.json` lists them so the check prints them as waiting.

**Lighthouse, after the fixes** (local build, light): accessibility, best practices, SEO. Home 95, 100, 100; Features 100, 100, 100;
download 96, 100, 100; user guide 96, 100, 100; How many shots 96, 100, 100; tour 98 before its heading fix, 100, 100. Every remaining
accessibility point is the link finding above.

**In CI.** `checks.yml`'s website job now runs `site-audit.cjs --only a11y` (axe on every page, both themes, at phone width): 67 s here.

## Entry 462 section 2: three new research articles, written and published (2026-10-21)

Each meets entry 392 section 1: every number is worked out by GroupLab's own code and held by
`tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs`, whose simulations are seeded (2026) and rerun to the same values; every
checkable sentence is backed in `docs/claims-backing.json`; the developer is not named; the byline is the existing one; each has a drawn
thumbnail and is listed in `website/research/PUBLISHED.md`. No link to any site but grouplab.org and the two sources named.

1. **Does your rifle group as well as Top Gun says it should?** (`top-gun-and-your-groups`) One five-shot group is 0.67 to 1.36 times
   the true average five-shot group, 8 times in 10; GroupLab's interval on Top Gun's basis is 0.82 to 1.29 times from 20 shots; a rifle
   on the trend is called clearly wider 3 to 4 times in 100 at any size, and one 25 percent worse is caught 25, 35, 58, 73 and 90 percent
   of the time from 5, 10, 20, 30 and 50 shots. The book is cited by title and chapter only; nothing is quoted.
2. **What a canted scope costs at distance** (`canted-scope-at-distance`) For the point blank article's load, 2 degrees of cant moves the
   shot 0.8 in sideways at 300 yd and 12.5 in at 1000 while losing under a quarter inch of height; a 60 MOA scope reaches 910 yd on a flat
   rail and 1270 on a 20 MOA rail (`ScopeMount.Farthest`).
3. **When a click is not what it says: the sight scale factor** (`when-a-click-is-not-a-click`) Error sizes from Gun Digest (about 2
   percent typical, 5 a reason to return the scope) and The Firearm Blog (one scope at 110 percent); at 1000 yd a 2 percent error moves the
   center 6.3 in and the first-round chance on a 10 in circle falls from 36 to 20 percent (GroupLab's hit probability for the centred
   case, the same shots moved for the rest). Checking against the code changed one passage: GroupLab's test reads the turret with the
   reticle on the lines and fires no shots.

Candidate 4 found three investigations since entry 402 with no row: the screen reader audits, synchronization and the site audit, each
decided not written in RESEARCH.md with its reason.

## Entry 462 section 3: the interface sweep, faults fixed and looks sent to Alan (2026-10-21)

**How.** `tests/Shared/LayoutAudit.cs`, new, shared by the desktop and phone suites, walks a window's controls and reports words cut off
(trimmed with no tooltip, wider or taller than their box, or hidden by a box that clips and does not scroll that way), a control a person
uses past the window's edge, words drawn over words (a dialog's solid card or a tooltip over them excepted), and words under WCAG AA
contrast (4.5 to 1, 3 to 1 for large) with both colours named. On the desktop it runs in the every-screen walk (`Entry109Tests`) when
`GROUPLAB_LAYOUT_AUDIT` names a file: every screen at 1280x720, 1400x900, 1920x1080 and 2560x1440 in both themes; the weekly screenshot job
now fails on any finding, after committing the pictures. It also runs on every push beside the screen reader audit in the entry 447 window
tests. On the phone, `Entry462PhoneLayoutTests` lays out every place at 360, 412 and 600 wide, both themes, at the ordinary and the
largest text size (the iPhone's largest is held at Android's, so one pass covers both), headless, the same Avalonia code the emulator
runs. A chores-haiku walk of 55 pictures found what a machine cannot judge.

**First walk: 417 findings; after the fixes: none, desktop and phone.** Fixed: Ballistics' table headings cut off (they wrap), its
Elevation figure running over its label in a narrow panel (it goes under), its right panel's last lines under the status bar with no way
to scroll (it scrolls, the bar drawn in the padding so nothing narrows); the hit plot's key off its right edge and two long choices cut
off in their boxes (they wrap); equipment labels with a long unit and the flagged-marks heading cut off (they wrap); the shot table's
"center" cut off (its first column 52 px); every trimmed name (tabs, the path, session lists) now has its whole text as a tooltip
(`TrimmedTips`); the hint inside an empty box was the base theme's grey at half strength, about 2 to 1 (now whole, in the faint text
colour); faint notes inside an expander were 4.12 to 1 on its grey (now dim). **Failures as the centred warning** (Alan, 2026-10-02,
entry 356): fifteen save, print and open failures still said in a small line now also raise it: the point blank card, the scale marker
page, labels, check label and stickers, the session database at start, a marking or session that will not open, a session file, a
chronograph file (twice), a scale rectangle that cannot be used, and on the phone the M220 card and labels. **Same thing, same words:**
chronograph readings and the velocity block's step said "fps" where every other screen says "ft/s"; the phone's designer said "Distance,
yards" and the computer "Distance, yd"; the phone's Settings said "Sizes on the paper" for what the computer calls "Lengths". The
published Compare and Sessions pictures dated their second session 2099-01-01; it is now a week before. **Not checked by machine:** focus
visibility (entry 447 holds the tab order and names) and screens deeper than the phone's bottom places.

**Looks for Alan** (DESIGN NEEDED in for-alan.md, eight lines): marking labels stacking, the analysis key over the plot and unit-less
headings, the store-target question over the bull, the set tab's pill, the first run taller than 720, the demo card over its text,
Compare's half-empty card, and two phone pages.

## Entry 462 section 5: what is still done by hand, automated where nothing from Alan is needed (2026-10-21)

`docs/notes/AUTOMATION.md`, new: 24 recurring steps, who does each, how often, what already does it, and whether it can be automated
with nothing from Alan; the "handoff task" the entry names was not found anywhere and is left for the planning session to name.

**Built:** `scripts/run-start.py` does the start of every run (open requests, error reports, earlier scratch folders cleared by
clean-scratch.py's own rule, external status in a line a store, the inbox, the week's usage, whether the logs need splitting);
`scripts/site-live.py` asks grouplab.org's build tag once a minute until a commit is live or the time is up. With sections 1 and 3, the
site's accessibility and every screen's layout are now checked by machine. **Prepared, off:** `scripts/tester-notes.py` and a nightly
step post each new "For <tester>" note once its fix is published, opening "Hey <name>,", only once `DISCORD_TESTERS_WEBHOOK` exists
(request 107; the notes already in for-alan.md are recorded as passed on); `scripts/oracle-backups.py` and `oracle-backups.yml` list the
boot volume backups on Mondays with a read-only key and keep a `backups` issue while the newest Full is over eight days old or everything
expires within a week, only once the five `OCI_*` secrets exist (request 106, replacing request 46's look). Both are in RESTORE.md with
how to stop them. Self-tests pass; with no secrets both say so and do nothing. **Left by hand:** STATE, folding entries, release-note
wording, publishing articles, merging tuning pull requests and the planning session's relay are judgements; the whole-server restore test
needs more than read access.

## Entry 462 section 4: more pictures in the guides and on Features (2026-10-21)

**The user guide** has seven more pictures, five of them marked (entry 437's style), each from the walk's own renders: Scope and mount
(three fields marked), the zeroing grids (Open as PDF), Shots Needed to Zero (two controls), Point blank range (three), Walk me through a
demo (Next and End the tour, and the phone's demo), and the bar along the bottom at the largest text. Sections left without one say why
in the docs pass: the keyboard picture shows nothing a keyboard does, the optic picture does not show the optic's fields, and the
problem section already holds its one marked picture. The testing guide and the statistics primer never send the reader to a screen,
so they have none added. The guide PDF is regenerated.

**Features:** five entries that showed a general screen have their own picture, made by the screenshot walk: Conditions on the day
(`conditions`), Every shot, and which count (`shots-window`, a shot left out), Zero from this group (`zero-window`), A picture to share
(`share-window`) and Words explained where they appear (`word-explained`, Mean radius's explanation open). Each is in SOURCES.md,
screens.json and the analysis tour page's further pictures. The fourteen still waiting each say why in `docs/figures/SCREENSHOTS.md`
(mostly a click, a drag or a file the headless walk cannot make yet). Rendering the new Conditions picture found a fault the layout
audit names: the link to change the rifle's figures ran past a wide window's edge; it wraps now. The rest of the pictures are re-rendered
by the screenshot job from main (it runs when a screen's files change), so they show section 3's fixes; local renders matched CI's but for
the status line's clock. **Not done:** phone pictures beside the new Features entries, which need the emulator's walk.

## Entry 462 section 0: question 106 answered, a photograph's codes left as they are (2026-10-21)

Alan chose (b): every place on a photograph is still read, so a picture showing two sheets is still caught. Question 106 is archived
as answered in `docs/notes/archive/questions-answered.md`, the measurement stays in `docs/PERFORMANCE.md` with the answer under it, and
Phase 9 has no next step queued. Nothing in the application changed.

## Entry 463: the three sync screens Alan chose (1A, 2A, 3B), and sync a person can reach (2026-10-21)

**Shared.** `src/GroupLab.Core/Sync/SyncScreens.cs` holds the three screens' words and figures for the computer and the phone alike: the
status line ("Synced 2 minutes ago: 3 changes in, 1 out."; the engine counts changes, not sessions, so the line says changes), the other
devices seen from the folder's device files, the side by side of a session changed on two devices (rows for holes with what was added,
taken away or moved, mean radius, rifle, barrel, load, caliber, distance and notes, each that differs marked), and the two cards' sizes,
now and a year at the person's pace (their own rate where there are ten sessions over sixty days, else 200 a year, and it says which).
`SyncEngine.Versions` finds a waiting session's two versions and `SyncEngine.Measure` what the records weigh. `SyncSetup` keeps this
device's folder, name, originals choice and last run in the settings, never synchronized itself.

**1A, the computer:** Settings has a "Sync between devices" section: the folder in full with Change folder, this device's name, the status
line with Sync now, the other devices with when each last wrote, the two cards, and Stop syncing with "Stops on this device only. Nothing
here or in the folder is deleted." It runs every five minutes while GroupLab is open and at once from Sync now. A folder that cannot be
reached is said at the top of the section in amber with Choose the folder again, and the first time in a run in the centred warning;
nothing is changed or deleted. **The phone:** the same items stacked on a Sync page under Settings; the folder through Android's folder
picker (`SafSyncFolder`) or a folder in Files on iPhone and iPad (`BookmarkSyncFolder`), both from entry 461, now reached through the
platform (`ChooseSyncFolder`, `WithSyncFolder`). **2A:** a centred dialog headed "This session was changed on two devices", the session's
load, sheet and date, "Both versions are kept until you choose.", two columns (each device's name and time, a small plot of its holes on
one scale, its rows), and Keep each device's, Keep both as two sessions and Decide later; on the phone the two stack. A waiting choice is
found again from a banner on Sessions and a "2 versions" tag on its row, on both. **3B:** the two cards with bars on one scale a row and
the line under them; the "Only on Wi-Fi" choice was not built.

**Reachable:** the user guide's Settings part has Sync between devices and When a session was changed on two devices, each with a marked
picture the walk renders (`sync-settings`, `sync-conflict`); Features has "Sync between your devices" with its own picture; the README and
the home page no longer say synchronization is not built; Phase 7 reads Built, not proven. Option B is not mentioned anywhere.

**Tests:** `SyncScreensTests` (Core, four), `Entry463Tests` (computer, three: a folder chosen, run and stopped with nothing deleted; a folder
gone said in the section and once in the middle; a session changed on two devices found on Sessions and kept both ways), and
`Entry463PhoneTests` (phone, three, the same, with the layout audit at a phone's width). The layout audit's overlap check now decides what
covers what from the tree rather than a hit test, which missed a dialog's card at 1280 by 720. The Android head builds here; the iPhone's
picker is proved by ios-app.yml on its next run. **Not done:** the phone's pictures of the Sync page and the side by side, which need the
emulator's walk; and sync is not proven on two real devices.

## Entry 466: request 107 answered "not for now" (2026-10-21)

Alan read drafts of what `scripts/tester-notes.py` would post and said "Lets just not do the notes for now", replacing the yes of
entry 464 section 0. Request 107 is closed and moved whole to `docs/notes/for-alan-archive.md` with that answer and the four
conditions for taking it up again; `for-alan.md` now counts 25 open. `scripts/tester-notes.py` and its step in nightly.yml stay as
built, switched off while `DISCORD_TESTERS_WEBHOOK` does not exist, and nobody is asked for it. `docs/notes/AUTOMATION.md` says so.
The "For ..." notes stay in `for-alan.md` for Alan to pass on by hand.

## Entry 465: the eleven interface looks Alan chose, built (2026-10-21)

Built by five helpers in their own worktrees under entry 467, merged and tested together here. Each item is its own commit.

| item | what a person sees now | held by |
|---|---|---|
| 1A Marking (645d83f3) | bull numbers outside the outer ring at the top left; hole tags just outside the ring, never on a tag, a bull number or a hole; the selected shot an open magenta ring with four ticks and a magenta tag; close holes on Any target fanned in a column on leader lines | `MarkTags.Place` (Core, shared by both); `MarkTagsTests`, `Entry465MarkingTests`, `Entry465PhoneMarkingTests` |
| 2A Analysis (95c9e91b) | the shots headings carry the unit chosen ("across, in"); the plot's key in a box under the plot, two columns where it fits | `Entry465Tests`, `Entry465PhoneTests` |
| 3B Store-bought (54c20722) | "Which target is this?" a bar along the bottom of the picture, the bull moved up whole above it; a bottom sheet on the phone | `StoreTargetScreenTests`, `StoreTargetPhoneTests` |
| 4B Set tab (4294d76e, c30b4505) | the Scale checked pill at the top of the side panel with one line saying how the scale was found, built only from what each sheet's scale records | `ScaleSaid`, `ScalePillTests`, `ScaleSaidTests` |
| 5A First run (eea0a1e7) | one question a step, "Question N of M", a step bar, Back, Next and Skip for now; every step fits 1280 by 720; every question's words compared with a recording taken before the change | `FirstRunSteps`, `FirstRunWordsBefore` (both suites) |
| 6A Demo tour (0742f96f, 30cb8ac7) | the card beside the outlined part with a pointer, the bottom of the window only where no side has room; on the phone the step's number moves clear of the card | `DemoCardPlacement`, `Entry465PhoneDemoTests`, the phone sweep |
| 7B Compare loads (06ccbfac) | with two loads, "What the difference means": the main figures side by side and GroupLab's existing sentence on whether these shots tell them apart and how many would | `LoadComparison.Compare`, `Entry465CompareTests` |
| 8A Phone, Scale markers (fd3e362d) | plain body text, a "What is this?" button by each marker's name; five new glossary entries; the Targets library rows and the optic card made plain too; Settings, Capture, Sessions, Ballistics and the figures left as they read well | `Entry465PhoneWordsTests` |
| 9A Phone, Targets at largest text (fd3e362d) | one field to a row, the label above, when a label would not fit half the width on one line | `Entry465PhoneWordsTests` |
| 10B Site links (3d5edaef) | a 1 px underline at 55% in sentences, full 2 px on hover and focus, in both themes: the light theme had the same fault on six links | `scripts/site-audit.cjs`, 82 pages, 0 findings |
| 11A Site, How it works (3d5edaef) | a white panel behind the amber numbers, stage labels and strips, passing 4.5 to 1 | `scripts/site-audit.cjs` |

**Merging.** Three helpers each named a new test file `Entry465PhoneTests` or `Entry465Tests`; the later ones became
`Entry465PhoneWordsTests`, `Entry465MarkingTests` and `Entry465PhoneMarkingTests`, and the backing names them. Put together, the phone
sweep found the tour's step number under the card's title on the result plot, which now fills the screen since its key moved under it;
fixed in 30cb8ac7. A temp folder reused across ended test runs carried a "did not exit" marker into the next run and put GroupLab's own
error bar on the first screen; a fresh folder cleared it, so nothing in the application was wrong there.

**Tests** on the merged head: Core 3112 passed (the two local-only failures were the inbox line, which CI sees empty, and one British
spelling in a site comment, fixed); App 624 passed before 5A, 6A, 3B and 4B were merged and 638 on the merged head (30cb8ac7); Mobile 219 passed with the sweep and demo tests passing after the fix. Claims 5726, all backed; README
current; site build ok. **Is it worth an article?** No: these are GroupLab's own screens, and nothing in them changes what another
shooter does or another developer builds.

## Entry 464: sync tidying, proofs, two articles, speed and an audit (2026-10-21)

Worked by four helpers under entry 467, then the audit on docs-sonnet, merged and tested here. Section 0 (request 107) was replaced by
entry 466 before it was started.

**1. Sync tidying (1a9850bb).** Each device writes the change files it has read, and the records waiting for a choice there, to
`GroupLab sync/read/<device>.json`, and that is the only basis for tidying. Once a day a device moves only its own files, and only one
that every other device lists as read, whose every record a later file of its own carries newer, and in which nothing waits for a
choice anywhere. The move copies the file whole to `GroupLab sync/trash/<device>/`, reads it back and compares it before the original
goes; the trash is emptied 14 days after the move, counted from the move. A device with no read position, or one that cannot be read,
holds everything back, and a write to a folder that has gone missing now refuses rather than making a new empty one. The Sync block on
the computer and the phone and `grouplab sync` say what was tidied and what it freed. Tests: two devices, one away a month; a choice
waiting; the folder going missing at six points; the 14 days; the phone's layout. Built, not proven, until two real devices run it.
Question 109: a device that never comes back holds tidying back for ever.

**2. Proofs (551d39cd).** Of the 27 rows still built, not proven in `docs/PROOF-CHECKLIST.md`, eleven moved to Done with their
evidence named: significance testing, subgroups, doubles, comparing photos, rounds fired, session records, the report, the target
library (a gate written for it), the Garmin Xero import (this computer only), load against load, and `grouplab bench` on its recorded
passes of entries 388, 415 and 433. The other sixteen each have a line saying the material that would prove them (a camera and a shot
sheet, a printer and paper, a range and a chronograph, a person timed, a phone sitting, Google Play). Nothing of entry 465's was moved.

**3. Articles (2ad2524f, 053746fe).** No row in "Worth an article?" still said not yet; every "not written" row's reason still holds,
and the aim-points draft still waits on request 86. Two pieces never decided passed the test and are published: "How much of your
vertical is your velocity?" and "Where a first-round miss comes from", each with seeded figures held by `ArticleFiguresTests`. The
canted-scope article's source line named a file that does not exist; it now names the ballistics code.

**4. Speed and the audit (c4217def, 3d15f53d, 4b255fc8).** Measured stage by stage from opening a picture to its analysis on the
computer and the phone's path (`docs/PERFORMANCE.md`): on a photograph the codes are the largest stage, as question 106 left them; on a
600 dpi scan, the holes and the PNG's decode. One gain taken: a JPEG's grey and colour copies are decoded side by side, opening a phone
photograph about a third sooner, held to the bit by `ImageLoaderSameResultTests`, and the corpus, synthetic scoreboard and counts are the
same line for line. A larger gain on the first picture after start-up, publishing precompiled, is question 110. The consistency audit
after entry 465 found one stale page (`docs/SURVEY.md` still described the first run as one screen), fixed; consistency 0 findings.

**Is it worth an article?** Sync tidying and the speed gain: no, ordinary engineering a shooter never sees. The proofs: no, a record.

## Entry 467: helpers in parallel until 20:00 Denver (2026-10-21)

Nine main-model helpers started at 19:55 UTC, each in its own worktree on a piece whose files did not overlap. **The five-hour window,
not the week, was the limit**: nine helpers took it from 14 to 33 percent in about twelve minutes, which would have spent it well
before its 22:10 reset and stopped everything. So the four entry 464 helpers were paused at once and the 2A and 7B helper stopped once
its work was merged, and resumed with their context when the window reset; `scripts/usage-statusline.js` now writes the five-hour
window's reset into `usage-now.json` so the next run can see it. Entry 465's five finished first, as entry 467 section 5 asks, then
464's four and the audit on docs-sonnet. The week went from 45 to 57 percent.

**Merging.** Each helper's commits were cherry-picked onto phase-1 in turn. The only real conflicts were the claims backing, resolved as
JSON (both sides kept), the register regenerated, and three test files given the same name by different helpers, renamed. The merged
tree's App suite is the test that counts, because no helper saw the others' work: it found the phone tour's badge under the card. Two
stopped helpers left shell processes running, ended by number; a temp folder reused across ended runs carried a "did not exit" marker
into the next run, so every run now starts from an empty one.

## Entry 468: Settings in eight categories, and six new settings (2026-10-22)

Alan found Settings "very cluttered" and chose concept A on the canvas, with every proposed new setting. The computer, Android and the
iPhone and iPad changed together; every setting kept its stored key and meaning.

**1. The structure (c4e1ab07, 8aae8700, c01a659e).** `SettingsMenu` and `SettingsPages` (`src/GroupLab.App/SettingsMenu.cs`, shared with the phones) hold the
eight categories, their one line, what each row is found by, and what putting a page back does. On the computer
(`MainWindow.SettingsMenu.cs`) the side menu is a column of buttons reached by Tab and moved through with Up and Down, each named to a
screen reader, with Find a setting above it, one page of cards at a time beside it, the status line under both, and the page shown last
remembered (`settingsPage`). On the phone (`SettingsView`) Settings opens the list of the eight with their current choices in one line,
the search box at the top, and each page with "‹ Settings" back (a page back, so Android's Back returns to the list). A search filters
the same controls in place on every page, so a choice made among the results is the one on its page. "Put this page back as it came"
asks first (the computer's centred question, the phone's inline one) and resets only that page's settings, never data, printers, copies or
sync. **Saving** (by itself or with a Save button) had no category in the entry; it sits on Sync and your data. The desktop's Printers
section moved from a long column to its own page; the phone's About split into Updates and about and Help and problems. The tests that
read the old long page now open the page they mean; the control bench, the keyboard sweep, the screen reader and layout audits walk all
eight pages (`Entry447Tests`, `DesktopSweepTests`, `ControlBenchTests`); the phone's scripted walks press the category first.

**2.1 Daily copies (c4e1ab07).** `RecordsCopies` (Core) makes a `VACUUM INTO` copy beside the database at the first open of a day, on by
default (`dailyCopies`), keeps the newest 14 in `copies/`, moves older ones to `trash/copies/` named by the day they moved, and removes
them 14 days later. Back up now, Restore a copy (each copy with its day, sessions and size) and, on the computer, Open the folder; a
restore copies the records as they are first, so it can be undone the same way. GroupLab then reads the restored records afresh in place
(the record book, Sessions and the library) rather than restarting the process: the session store holds no connection open, so nothing
of the old file survives, and the phones open the store each time anyway. `RecordsCopiesTests`: a copy made while sessions are being
written passes SQLite's integrity check, the restore round trip, the fourteen kept and the trash's fourteen days, and a full disk leaving
nothing half written with the records and the earlier copy byte for byte unchanged. `docs/RESTORE.md` has the section.

**2.2 to 2.6, the other new settings (7ce223ff, 4882f861).**
- **Shooting defaults:** the distance (last used, or always this one; stored `newTargetDistance`), and on the computer rounds fired and
  rifle and load. Today's computer behaviour for rounds fired is that it is asked for and marked until answered, so the default is named
  **Ask**, as the entry's rule that today's behaviour is the default requires; One a bull fills the count from the sheet's bulls once
  detection finds them, and Not known pre-answers it. Rounds fired and rifle and load are computer only, because the phone asks for
  neither per target; its page says so. **Paper** (Letter or A4, `paper`) is one setting shown on Shooting defaults and on Printers and
  paper; every sheet, report and printer check that chose by region now asks `AppSettingsStore.LetterPaper()`, which is the region's
  until one is chosen.
- **The camera (phone):** Starts in Guided or Manual (`cameraStarts`); until it is chosen the camera starts as it was last left, as it
  always has, which is Guided on a new install. The shutter sound is on unless turned off; the iPhone's AVFoundation makes its own sound for
  every picture, and Android 13 and later say where the law requires one, so there the switch is shown on and cannot be changed, with a line
  saying why.
- **The analysis opens with** the main figures or every figure (`analysisOpens`): every figure opens More figures, Advanced and the full
  CEP table on the computer and every folded section on the phone, without changing the fold the computer remembers.
- **Search and reset** as in section 1.
- **Text size on the computer** (Normal, Larger 1.15, Largest 1.3, `textSize`) through `TextSize.Choose`, the same path the phones use to
  follow their system size, so every token-sized style grows alike. `Entry468Tests.NothingIsCutOffAtAnyTextSize` runs the layout audit on
  the analysis, Sessions, Targets, Ballistics, Compare and all eight Settings pages at all three sizes: one finding at Largest, "Keep these
  on the records" on Ballistics two pixels past its column, fixed by letting its words wrap. The phones keep following the system and say
  so. **Reduce motion** (`reduceMotion`: follow the system, on, off) is read by `Landing.Still()`, which the landing flash and both phones'
  camera flash now use; the camera's flash becomes a still tint held a moment.

**Tests.** Core 7 new (`RecordsCopiesTests`), App 10 new and Mobile 9 new (`Entry468Tests`). CI on 4882f861 is green on all three
systems (Core 3138, App 649, Mobile 231). **What the pushes found.** The iPhone app's data round trip on ios-app.yml found that a
`.grouplab` file opened from another app no longer showed its import plan: the hand-off opened Settings at its list, and
`Shell.ShowSettings` built Settings twice so the first copy took the waiting file (8aae8700, c01a659e, with a test). Then the App suite
took 48 minutes on macOS and 72 on Linux, where the job ran out of its 90 before the Mobile suite: every window asked
`TextSize.Choose` for Normal and each call rebuilt the whole application's styles; it now does nothing when the size has not changed
(4882f861), and the App suite went back to 21 minutes on macOS and 30 on Linux. The iPhone app's own screen-reader audit has timed out on Apple's side on the result
screen in the last three runs (once on Targets as well), while Settings has no findings; the result screen took 67 s against 36 s in
the last green run with every screen slower, so it is recorded here, not changed blind. Locally the two test-host stops in the full
App run were the 10-minute hang limit dumping the screenshot walk, which takes 10.5 minutes, not crashes.

**Documentation.** The guide's section 12 rewritten around the eight pages with a section for the daily copies, and its other references
to Settings, About and the old headings moved to the new pages; the tour's Settings page; three Features entries (Settings in eight
categories, Daily copies, Shooting defaults) with their pictures; README; claims backed.

**Is it worth an article?** No: a settings screen and a backup are ordinary engineering; nothing here changes what another shooter does.

## Entry 469: the consistency audit of 2026-10-22 (2026-10-23)

**Section 1, Settings' old page names.** Every place the audit listed now names the eight pages: the support page's three steps and
"On a phone", the download page's Which build and GroupLab Dev lines, the README (Which build, GroupLab Dev, Phase 7), the Features
`sync` sentence (reworded for nightly 219), the hardware survey block, what-grouplab-sends, the tour's first run, both guides and their
PDFs, PHONE-PARITY, ANDROID and UPDATES. Two more found on the way: CONTRIBUTING (the Detailed logging box) and SURVEY (Sharing). The
eight retired forms are in docs/RETIRED-WORDING.json with entry 468 as the reason, allowed in the release notes, features.json and the
claims files that quote them; "top of Settings" is also allowed in three places where it means the first row of a page, which is still
right. Wording by docs-sonnet, checked here.

**Section 2, nightly 218's notes.** The two import lines fixed a fault that c4e1ab07 brought in and c01a659e and 8aae8700 took out inside
the same build. The guide line got through because 457bc019 also touched two shipping files, though only their comments, so the generator
rightly counted it and read its trailer: the trailer was the fault, not the generator. A correction with no notes now takes a commit out
(scripts/release-notes.py), and the three are corrected that way. Nightly 218's published text is not edited (CLAUDE.md); nightly 219's
notes carry one line, through the correction of 6feb3f9a, saying the import works as it did in 217. Question 111 asks planning whether
that is right.

**Section 3, the phone's pictures of Settings.** The audit missed a commit: the walk ran on nightly 218's code and 583a38fe (13:32 UTC on
2026-10-22) replaced fold-settings and tab-settings-landscape with the list of categories, and made-from.json says 218. The walk pictured
only the list, so it now also opens Shooting defaults and Sync and your data (`settings-shooting`, `settings-data`); the Settings feature
uses `fold-settings` for its phone side, and the two pages wait in SCREENSHOTS.md for the walk's next run.

**Section 4.** RESTORE.md says the Discord posts are off by Alan's decision (entry 466) and oracle-backups.yml is off until request 106's
secrets are set.

**Is it worth an article?** No: wording kept in step with the application.

## Entry 470: Android's back on Settings, Sync and your data (2026-10-23)

**The fault.** Alan: the back gesture did not leave Settings, Sync and your data. `Screens.Pill` marked every phone button whose words
began with "Back" as the page's way back, and entry 468 put "Back up now" below "‹ Settings" on that page, so `Shell.WayBack()`, which
takes the last way back showing (entry 376 item B1), pressed "Back up now": each back made a copy of the records and the page stayed.

**The fix.** The rule is gone. `Screens.BackButton(words, chosen)` makes a way back and says so, and every real one uses it: Compare,
the CSV import (two), Fix holes, the Fudd buster, the one page report, the chronograph pairing, Sync, the printer check, Sessions, the set,
Share (two), Shots, Shots to zero, Targets, the velocity pages (three), Zero from, the first run's Back and each Settings page's
"‹ Settings". On nested pages the last one showing is still the innermost page's: within Settings only the open page's "‹ Settings" is
visible, and Sync replaces the Settings view rather than sitting over it. The desktop has no way back chosen by words, and on the iPhone
nothing raises Avalonia's BackRequested, so only the guided demo's Back step used `WayBack` there, fixed by the same change.

**The copies.** `RecordsCopies.Make` keeps the newest 14 and moves the rest to `trash/copies` for 14 days. The copies began with nightly
218 on 2026-10-22, so on any phone the oldest a stray back could have pushed out is that day's own copy, of records that are still in
place, and it waits in the trash until 5 November at the earliest. No device needs its copies put back.

**Tests.** `Entry470Tests`: on every category page Back presses "‹ Settings", returns to the list and makes no copy; from Sync, Back
returns to Sync and your data, then to the list, then to Capture (the entry's "a second Back returns to Capture" skips the list, which
sits between them); and a source check that every phone button whose words are a way back is made with `BackButton` and that no
`BackButton` has words that are not one. `BackEverywhereTests` now builds its inner page's way back the same way. Mobile suite 234 passed.
The emulator walk does not drive Back, so nothing was added to it.

**Is it worth an article?** No: an ordinary fault in how a button was marked; nothing here changes what another shooter does.

## Entry 471: Alan's answers to questions 107 to 110, and the push of entry 469 (2026-10-23)

**Section 1.** `gh auth status` showed the GroupLab account active. Entry 469's commit was rebased onto nightly 219's notes commits
(351e9918) and pushed; its site went live at 351e9918. Nightly 219 had been built from entry 470 alone, so its notes carry entry 470's line
and not the "Correction:" line entry 469 had put on 6feb3f9a. That correction now leaves 6feb3f9a with the note 219 published and rides in
this entry's commit instead, so nightly 220 says it once (question 111 (a)).

**Question 107 (a).** On the phone, a found hole's tag fell back to its count where `ShotLabels` gives no text, which is every hole on a
marking with no bulls; it now has no tag there, as on the computer (`MarkingAPage.Viewer`). `Entry471PhoneTagTests`: no hole tags on a
marking with no bulls, one on a bull.

**Question 108 (b).** Nothing built; `StatedResolutionScale.Offer` is still reached by no screen.

**Question 109 (a).** "Forget this device" beside each device under Other devices in the folder, on the computer and the phones (Android
and iPhone share the phone page), asks once on the page in the way "Put this page back as it came" does, saying nothing in the folder is
deleted. Placed in the plainest place, the device's own line, inside the 1A look, so no look question was raised. `SyncForget.Forget`
writes `GroupLab sync/forgotten/<device>.json`; tidying leaves a forgotten device out of the devices it waits for, and the list leaves it
out. Nothing is deleted. A forgotten device that syncs again writes the forgetting's identity into its own device file and is seen again;
tidying in the meantime is safe for it, because a change file is tidied only when a later one holds a newer version of all it carries.
**The new folder fault** is fixed by an identity: the first device in a folder writes `GroupLab sync/folder.json`, the database keeps the
identity its records were last written to (`SessionStore.SyncFolderId`), and a different one sets every record to be written again
(`MarkSyncUnwritten`). A device upgrading from an older GroupLab whose change files are already in the folder takes it as its own and
writes nothing again. `SyncForgetTests` (four), `Entry471SyncForgetWindowTests` and `Entry471SyncForgetPhoneTests`.

**Question 110 (a).** `PublishReadyToRun` on any publish of the desktop app for a runtime; the phones are unchanged. Checked first, the
command line published both ways for win-x64: the four local corpus scoreboards and the synthetic one identical but for their times, and
the 600 dpi sample's marking identical in all 1,080 values. Cold, a fresh process on the Fold 7's picture-1, five runs alternated: median
1,534 ms before and 1,109 precompiled. The desktop app for win-x64 zips to 102.8 MB against 91.2; the published Windows zip went from
114.4 MB in nightly 219 to 126.5 in nightly 220, the figures the download page gives (corrected the same evening: it first said 103
rather than 91, the bare app's figures). `docs/PERFORMANCE.md` has the figures.

**Is it worth an article?** No: a forgetting button, a tag rule and a build switch; the precompiled measurement is in PERFORMANCE.md and
would change nothing another shooter does.

## Entry 472: the overnight queue, work that needs nobody (2026-10-24)

**Section 1, the class of fault entry 470 found.** `Entry472BackTests` opens every place along the bottom, a result, each of the eight
Settings pages, Sync and the printer check, presses each button on them as the phone sweep does, and then Back: it must go to the screen
the press came from (to Capture where the press went to another place), and change nothing the phone keeps: every file under its folder
by content, the copies of the records, and what was shared, printed or opened. A page class no button reaches fails `EveryPageIsWalked`
until it is reached or named with its reason. It found five ways back Android's back could not see, all now fixed:
- **A result** had no marked way back: on Capture, back closed GroupLab; on Sessions it went to Capture. "Another target" is now the
  result's way back (to Capture's start, or to the list on Sessions).
- **How did you shoot this sheet** had only Done, unmarked, so back closed GroupLab. Done is now its way back, and saves only when the
  answer changed (before, Done saved the session every time).
- **The picture's check** before a result had none; back now goes to Capture's start and keeps the reading, as it already is in Sessions.
- **Add a store-bought target**'s own back arrow was unmarked, so back went to Capture; **the printer check**'s first step had only
  "Skip for now", so back went to Capture rather than Printers. Both marked. Marking A's Cancel is marked too.
- **A target switched to from another place** (Sessions' row for a target already open on Capture, or the open targets sheet) now returns
  to that place on back, not to the start of Capture.
The mark for a way out not worded as Back is `Screens.LeavesPage`. The iPhone has no system back and Avalonia raises none there (entry
470), so on iOS the change is the same shared screens; the screen sweep both the emulator and the simulator run
(`scripts/scenarios/phone-sweep.json`) now presses Back instead of each page's back button by name, six times, with a wait for the page
it should land on, and `PhoneSweepScenarioTests` runs it headlessly. On the computer every Escape handler was read: each closes a
dialog, ends a choosing or clears a field, and none saves or deletes beyond the guided tour's own sample (entry 439); the store-bought
target question's Escape pressed the last button in its row and now names "Not sure". **Other behaviour chosen from words**, three found
and replaced with explicit marks: the plot's key decided which sheet a tap showed alone by reading "Sheet N:" from the row's words; the
benchmark's bar was hidden by comparing the status line's words; the phone's guided tour found the picture check's button by its words.

**Section 2, every importer against bad files.** `BadFilesTests` gives a session file (.grouplab), a marking, a CSV of shots, a GroupLab
data file, a picture (JPEG, PNG, TIFF and BMP) and its Orientation, and a sync change file nothing, one byte, about sixty truncations,
120 changed bytes, 30 cut-out runs, a placeholder of zeros, a picture, words, a zip of something else, and the file twice; the text formats
also in UTF-8 with and without a mark, UTF-16 both ways and Latin-1, each with LF, CRLF and CR line endings; and very large or deeply
nested input. Each must end in what it read or in the refusal its callers catch, in words, inside 30 seconds. Found and fixed:
- **A session file** damaged inside its zip, or with a value of the wrong kind, faulted (`InvalidDataException`, `InvalidOperationException`)
  instead of being refused: now "This session file is damaged, so it was not opened. Share it again from the device it came from."
- **A marking** with a damaged value faulted (an unknown name, a missing value): now refused as damaged in words.
- **A GroupLab data file** that was not UTF-8 faulted, and one with a session's sheet name, a chronograph string's source or readings
  missing passed the check and then **stopped part way through the import** on the database's rule, leaving the sessions before it
  imported. Everything the database needs is now checked before anything is written, so it is imported whole or refused.
- **A sync change file** damaged so that it still parsed was **applied in part** (one record of two), then marked read, so the whole copy
  arriving later was never taken; others faulted the run on a missing value. A change file is now taken only when every record in it can
  be applied, and files GroupLab writes now carry a checksum of their records (older files have none and are read as before), so a
  damaged copy is left as still arriving and the whole one is taken when it comes.
- **An empty picture** faulted inside OpenCV: now "is empty, so there is no picture in it yet".
The CSV of shots, the chronograph exports (now with every line ending as well) and a photograph's Orientation (the values 0 to 9 and 255,
missing, said as another type, and damaged) needed nothing. Nothing was unclear enough to need Alan.

**Section 3, sync as close to proven as it can be without two devices.** `CloudClientTests` gives each of three simulated devices its own
copy of the folder, as a cloud client does, and a simulated cloud moves files between the copies: up to six minutes late each way, in any
order, a quarter of them arriving first as part of the file, now and then a file kept twice as a conflicted copy named the way OneDrive
("name-PHONE.json"), Google Drive ("name (1).json") or Dropbox ("name (Alan's conflicted copy 2026-10-24).json") names it, and a write
to a name another device changed meanwhile kept as such a copy. Over two simulated hours the devices make targets, change them, take some
away, add rifles and change a setting, syncing after each; the phone's clock is five hours ahead and the tablet's three behind; the
tablet's folder goes away for twenty minutes and later part way through a run; and the computer forgets the tablet while the tablet goes
on writing. Then everything arrives, the questions are answered on one device at a time, and the test holds that every device has the same
targets with the same marks, the same rifles and the same setting, and that every version anybody saved and did not change or take away
themselves is still there, as a target or kept beside it. Six runs, two for each company's naming. A deliberate fault (a half-arrived file
marked read) fails all six. **Found:** nothing wrong in GroupLab beyond section 2's change file fix. One thing a person could meet: the same
question answered on two devices before they have synced makes each answer a new version the other device then asks about; answering on
one device and letting it sync settles it. **Proven in tests now:** delays, order, half files, conflicted copies, the folder going and
coming back, clocks hours wrong, three devices, and forgetting a device that is still writing. **Still needs Alan's two devices:** a real
cloud client's own behaviour (OneDrive on Windows, Google Drive or Dropbox on Android, iCloud on the iPhone through Files), the phones'
own folder access (Android's folder picker, the iPhone's bookmark) over weeks, and a device away for days.

**Section 4, the phone's new Settings pictures.** The emulator's walk of 2026-10-23 (run 40018305491, nightly 220) took
`fold-settings-shooting` and `fold-settings-data` in both themes and committed them (0fc0fed8). The Features page now shows them as the
phone's side of Shooting defaults and Daily copies of your records, in place of the line saying they waited; `docs/figures/SCREENSHOTS.md`
says so. The README links the two features and shows no phone picture of Settings, so it needed nothing. The tour's Settings page names the
computer's pages and is unchanged.

**Section 5, the proof checklist.** Read again. Nothing has come into `grouplab-local` since entry 464 (the range kit waits for its trip),
so no row moved on material. Row 27's two halves are each held by a machine (the desktop opening a phone's session file in
`SessionFileTests`; the phone's own Share this session writing a file that reader opens with every shot where it was, in the new
`Entry472ShareTests`), and what is left is Android's share sheet carrying it across, which needs the
phone. `docs/PROOF-CHECKLIST.md` now opens with one line per thing still needing Alan, what it proves and about how long, shortest first:
fifteen lines, from five minutes at the phone sitting (row 27) to a range with 300 yards (row 25), and sync's real devices among them.

**Section 6.** The panel note and `docs/notes/STATE.md` say what was done, found and fixed; nothing in this entry needs Alan. The week's usage
was 12 percent after section 5, well under the 50 percent the entry set.

## Entry 473: the consistency audit's five fixes, and error reports 38 to 40 (2026-10-26)

**Before the entry: error reports 38, 39 and 40**, all from one Windows computer on nightly 221. 40 and 39 were the same fault: somebody
marking each sheet by hand on copies of one clean picture of the target lost each sheet to the next, because entry 420 section 3 took
the same picture (SHA-256 and design) for the same sheet. The logs showed a new picture's first save going to the existing session as
the same sheet. Now the same picture is the same sheet only where at least half the holes of each match (`SessionRecords.SameHoles`, a
quarter inch where the scale is known), and an updated record is kept first (60e60bc1, `Entry420Tests`). 38 asked to delete rifles and
loads: Delete on a rifle, barrel or load, asked first, on the computer and the phone (1147821e, `EquipmentDeleteTests` in three suites).
A fourth package, from an iPhone, showed the demo's session number reused after the demo deleted its own session: not a fault.

**Section 1.** The sync card said "coming in the next build" because its phrase read "on your computer and phones" and nightly 215's
note says "your phones". Copied exactly; the page now reads "Since nightly.215, changed in nightly.220". `never_published` in
website/build.py holds the Features page back when a feature that has arrived has a phrase in no published note and in no commit since
the newest nightly.

**Section 2.** The phone walk now has a `sync-sample` step: a folder in GroupLab's own storage with a second device, Workshop computer,
in it (`PhoneSync.Sample`), since Android's folder picker cannot be driven; it opens Sync between devices, takes `settings-sync` and puts
everything back. `PhoneScreensScenarioTests` runs it headless. The picture comes from the emulator run dispatched on 2026-10-26.

**Section 3.** Nightly 221's archives, their files summed: Windows 266.1, Linux 258.8, macOS 231.3 (Apple silicon) and 261.5 (Intel)
MB, against nightly 103's 226, 219 and 185. The tables say 270, 260, and 240 on Apple silicon or 270 on Intel.

**Section 4.** "In the settings" and "the top of Settings" now name the page: Updates and about (the update check and This build),
General (the demo), Units and figures (the scope and length units), Help and problems (Report a problem).

**Section 5.** The guide's sync section and the Features sentence say a change file still being copied, or damaged, is left alone until
it is whole.

## Entry 474: the overnight queue (2026-10-26)

**Section 1, speed.** Finding the holes on the 600 dpi scan 403 to 310 ms, the generated sheet 189 to 117, a phone photograph 125 to
97, one sheet from file to figures 327 to 267, a batch of ten 2,755 to 2,126 (`grouplab bench --runs 5`). Three exact changes: the
sheet drawn in bands of rows at once (`SceneRasterizer`, 81 to 19 ms), the moved print check's darkest and lightest taken row by row
(`DiscExtremes`, 46 to 10), the residual's paper level placed once per row and column (44 to 30). The four corpus scoreboards, the
synthetic and any-target boards and `analyze --json` on 16 pictures are identical before and after; the bench gate passed; the window's
gate fails on "a new window ready to use" before and after alike (docs/PERFORMANCE.md).

**Section 2, audits.** New: `SettingsSearchAuditTests` on both platforms (every Settings row found by the words on it, and 25 everyday
words), `PagesUnderPlacesAuditTests` (every phone Settings page, Sync, the rifle and load forms and their Delete question at the largest
text, layout and screen reader), and the marking screen and Equipment added to the computer's every-text-size audit. Fixed: field names
on the marking screen ran past their box at Larger and Largest; OneDrive found nothing on either, and bigger, millimeters, bug and rifle
nothing on the phone; a phone's default name in a sync folder was GroupLab's version line, now its maker and model. Back was already
walked on every phone page. The phone's Sync picture, taken by the emulator walk, is on Features.

**Section 3, tests.** `scripts/slowest-tests.py` writes each suite's 15 slowest tests into every CI leg's summary. The phone's blind close
test 87 to 21 s, the band test 115 to 7 s. The desktop screenshot walk (291 s, a quarter of the App suite) was measured without writing
its pictures and took the same time, so nothing was changed there. The proof checklist's open rows all need hardware or a person.

**Section 4, light installer.** Framework-dependent: 165.3 MB unpacked, 67.2 MB zipped against 245.8 and 102.8; start-up the same (679
and 672 ms against 682 and 679); missing only the .NET runtime. Recommendation in question 36: not the light installer; leave out
OpenCV's unused video reader (11.9 MB of every zip) instead.

**Section 5, learning loop.** Score every three minutes, nightly at 13:45 UTC and tune on the 1st at 16:00 UTC, every run finishing;
72 submissions read, 26 corrected of the 50 tuning needs, 15 with marks moved (54), added (37) or removed (9); 380 of 383 holes found.

