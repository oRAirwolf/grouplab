# Phase 1 results, entries 476 to 500

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 475: app clips for the launch videos (2026-10-26)

40 clips in `C:\Dev\grouplab-local\video-clips\` with `clips.csv` (file, video and chapter, what it shows, length, size, build) and each
clip's last frame as a PNG; nothing committed but the means of making them. `tests/Shared/ScreenClip.cs` draws GroupLab headless, frame by
frame, redrawing only when the screen changes, with a pointer gliding eased between targets on the computer and a widening ring where a
finger taps on the phone, and encodes H.264 at 60 frames a second through OpenCV with Windows' own encoder, reopening each clip to see it
plays. The computer is 2560 by 1440, dark, at the larger text size, on GroupLab's 25 shot sample with "Sample rifle, .308 Win", Load A and
Load B (Load B the same sheet's holes a third further from their bulls, saved as a sheet of its own); the phone is the Fold 7's 1080 by 2520.
Made: video 1's analysis, compare, zero, phone result and photo to holes, and three montage pieces; video 2's chapters 3, 5, 6, 7, 8 (compare,
Fudd buster, velocity, sessions), 9, 11 (share, sync, restore) and 12; video 3's steps 2, 3, 4, 5, 8, 9 (the phone's half) and help. A
chores-haiku pass on the last frames found 10 clips wrong at first, every one the script's fault (the loads lacked their reference
atmosphere, three buttons open windows of their own, Hit probability waits for its Work out button, the first run's answers are buttons);
all fixed and checked again. What was not made and why is in NOTES-FROM-PLANNING.md's entry 475 status.

## Entries 476 to 479: four consistency audits, and error reports 41 to 44 (2026-11-07)

Error reports first (entry 457). **43** (iOS, automatic): "Which bulls did you fire at?" was handed its answers only for a keyboard's Enter, so the phone showed the question with only its ×, and the × put the result back while the question's layer still held it, which threw. Every phone sheet now shows answers it is given and closes through `ProblemSheet.PutBack`; `ResultScreenTests.EveryAnswerToWhichBullsPutsTheResultBackWithoutAnError` threw the reported error on the old code. **42** (iOS, sent by a person): renaming a rifle or load kept a second one, on the computer and the phone alike, because records are kept by name; `RecordBook.With(record, was)` renames, a rifle's barrels going with it, and a name in use is refused (`EquipmentRenameTests`, the phone's `EquipmentDeleteTests`). The person's words stopped at 500 characters because the receiver kept 500 of the 2000 the application sends; it keeps 2000 now (`Entry194Tests.TheReceiverKeepsTheWholeNoteTheApplicationSends`). Where the phone keeps rifles and loads is question 112. **41** and **44**: thanks and a question for Alan, no fault.

**Entry 476.** The site fell behind three times in a fortnight because three jobs push what it is built from with their own token, which starts no workflow: the Store's version on /download/, the phone's screenshots and (entry 479 section 7) the weekly screenshots. Each now dispatches website.yml after its push, the Store's only when the version line changed. Nightly 222's notes were not edited (CLAUDE.md: a published release is corrected in the next one); `attributions` in docs/release-note-corrections.json count its two fixes from three reports in the running total on the site and in the README, and `CLOSES_REPORT` now matches "error report" too. The prose findings of 476 to 479 were made by docs-sonnet and checked here: claims all backed, readme current, the site builds, the guide PDFs regenerated.

**Entries 477 to 479.** As their status lines say. Phase 4 in the README is now "Built, not proven", since Find holes (Experimental) ships; ReadmeTests holds only the four state names, so no test changed for it. A feature the phone leaves out on purpose now says why on the Features page's Mobile side (`mobile.words`), and `feature_problems` refuses words on a feature the phone has.

## Entry 480: the iPhone Duo, waiting on Microsoft's iOS tools (2026-11-07)

**Can it be built?** Apple's and GitHub's halves are there: the `xcode-27` image has Xcode 27.1 (27A9275) as default and an iPhone Duo simulator under iOS 27.1, and Microsoft's workload set 10.0.402.1 targets the iOS 27.1 SDK. Tried on ios-app.yml: run 42343772140 stopped on CA1422 (`UIApplication.CanOpenUrl`, retired in iOS 27; the self-test now asks it only below 27), and run 42344190162 on IL2037, the trimmer resolving no members for explicitly implemented properties and events of Avalonia 12.1.2's app delegate, view controller and text input responder. That is dotnet/macios issue 26969, fixed upstream and promised for the next .NET 10 release; its maintainer says such members could be trimmed away, and for the text input that is typing, so the error was not silenced. Avalonia 12.1.3 to 12.1.5 say nothing about it. ios-app.yml is back on Xcode 26 as it was; `scripts/ios-xcode27.py <workflow> <workload set>` makes the change when a fixed workload ships.

**What holds now.** `Entry480FoldTests` changes the window between the Duo's reported sizes (466 by 678 outer, 626 by 890 and 890 by 626 inner) on the result, Fix holes, Capture and Ballistics: session, marks, zoom, the place under the crosshair and half-typed words stay. It found two faults, both fixed: the crosshair stood over another part of the target after a size change (it moved about 200 pixels on unfolding), and the result read the window's shape from its Bounds, which a window sets after laying out its children, so turned on its side it stayed one column; it now reads ClientSize. The same fault hit any phone or tablet turned mid-result, so it has a release note. `Entry462PhoneLayoutTests` now includes the three Duo sizes, and is clean. On the inner screen upright the crease runs under Ballistics, the middle place, as on the Fold 7 open: a DESIGN NEEDED line, nothing changed.

## Entry 482: the iPhone Duo crease kept, and Rifles and loads in the phones' Settings (2026-11-07)

**Section 1.** Alan's 1A: the Duo's bottom bar stays evenly spaced with Ballistics on the fold; nothing changed, recorded in
docs/IOS-PLAN.md (The iPhone Duo) and for-alan.md.

**Section 2.** `SettingsView` puts a "Rifles and loads" row between Shooting defaults and Printers and paper, built by `Screens.Row` with
its new trailing words ("Open Ballistics", a third of the row at most, wrapping at the largest text); `Shell.ShowRiflesAndLoads` opens
`BallisticsPage` at the rifle form with a "‹ Settings" way back, so the phone's Back returns to the Settings list. While searching, the
row shows above the results when every word typed is in its words (rifle, load, barrel, equipment and its own lines). Same code on
Android, iOS and GroupLab Dev; the desktop keeps Equipment. `Entry482Tests` (3) hold the place in the list, the spoken name, the 48
point height, the form opened, Back, and the search. The back walk (`Entry472BackTests`) treated every press that landed on a place as
going back to Capture; it now expects the screen before when the place was opened with its own way back. Phone suite 262, 258 passed
and 3 skipped before that change, the walk and 482's tests passing after it.

## Entry 481: the week's queue, parts A to E (2026-11-07)

**A, the clips.** `DesktopMoreClips` and `PhoneMoreClips` (skipped unless `GROUPLAB_VIDEO_CLIPS` is set) made ten clips, 50 in
`C:\Dev\grouplab-local\video-clips\` with clips.csv. A chores-haiku pass on the last frames found three wrong (Match to shots pressed off
screen, a tap on Aim 2 below the screen opening Ballistics, Show work in red); the first two were the scripts, now checked in the clip
itself, and the third a real fault: a recognized store-bought target counted its unread codes as a failed stage. `recognizedPicture` in
MainWindow.StoreTargets.cs leaves that stage listed but uncounted. The phone's report clip draws the PDF through PDFtoImage, added to
the phone tests for it; the camera clips (ch04, step 7) went into request 50's sitting.

**B, the video reader.** `LeaveOutOpenCvVideoReader` in Directory.Build.targets drops `opencv_videoio_ffmpeg*` from every build and
publish (Windows only carries it as a file). `scripts/package-windows.ps1`'s zip 114,384,706 bytes against nightly 223's 126,565,994;
227 MB unpacked; the Core suite 3163 passed and the App suite 655 passed with the file gone, and a clip still encodes.

**C, research.** All articles but `aim-points-by-optic-class` (request 86) were already published; RESEARCH.md's table said otherwise
for eighteen and now follows PUBLISHED.md. `smaller-installer` gains "Found the same way again, six weeks later".

**D, speed.** Window.Levels in BullLocators.cs counts each row's run inside the bull's page circle from its two ends (homography and
lens mappings only). Bench, median ms: 600 dpi scan P0.bulls 117 to 35, photograph 45 to 21, generated sheet 21 to 8, a batch of ten
2,173 to 2,083. Scoreboards and 21 analyses identical but for times; BullLevelsTests holds the histogram. Details in PERFORMANCE.md.

**E, the scan's own resolution.** `StatedResolutionScale.Offer` is reached at last: under the length and rectangle tools on the computer
and at the phone's scale step, with the number, used only when chosen; `DotsPerInchOf` recognizes it afterwards, so `ScaleSaid` gives it
"⚠ Scale from the scan's resolution" and "From the scan's own resolution, 600 dpi, ...". Held by StatedResolutionScaleTests,
Entry481StatedScaleTests and Entry481StatedScalePhoneTests; the guide says so.

## Entry 483: the 2026-11-07 consistency audit, six findings (2026-11-08)

**1.** Nightly 224's Windows zip unpacks to 237.6 MB (file sizes summed, 2026-11-07), 28.6 MB less than nightly 221's 266.1 with
OpenCV's video reader left out; docs/PLATFORM-SUPPORT.md says 240 MB installed, rounded up like the others, and its source line names
nightly 224 for Windows and nightly 221 for the rest. **2.** `other-targets` in website/features.json carries the offer and claims nightly
224's phrase, reworded in 224; PHONE-PARITY.md's row too. **3.** website/tour.json takes an optional Mobile `card` for the tour index
(the default stays for every other card); Equipment's card and page and Settings' phone part name the Rifles and loads row.
**4.** scripts/release-notes.py: `said_already` keeps a note that names its source from getting it again, a note not of kind `fixed` says
"suggested in", and `total_line` counts suggestions apart ("1 change suggested in 1 error report"); three new self-test checks. The README's
and the site's running totals read the new part (`FROM_YOU_PARTS`), and an attribution may move counts: nightly 224's is -1 fix, +1
suggestion. The total now reads 5 fixes from 10 error reports and 1 change suggested in 1 error report. **5.** README Phase 9 line.
**6.** TESTING-GUIDE.md and its PDF; `ReportWindow` placeholder "An email address, kept only in a saved report. Optional."
The wording of 2, 3 and 5 was done by docs-sonnet. Core 3166 passed, the one failure the local inbox line (gone once the entry was
moved); App report tests 24 passed; site Python tests, readme, claims and consistency checks clean.

## Entry 484: the rest of the week's work (2026-11-08)

**Section 1.** RESEARCH.md's article table has one row not published, `aim-points-by-optic-class`, a draft waiting on request 86's
shooting; every other row was published or decided "not written" with its reason (entry 481 part C caught the table up). Nothing new for
for-alan.md. **Section 2.** A fresh bench on 5dfdb5f4 put the photograph's codes (470 ms, question 106) and the scan's holes (317 ms,
entries 433 and 474) first; the next a person waits on that had never been worked were Find holes on a store-bought target (274 ms) and
a hit probability (87 ms). Both were taken down with every result the same, measured and proven in docs/PERFORMANCE.md (Entry 484 section
2): `HitProbability` keeps the zero, the drop per fps and the strata's quantiles by their inputs (`Recent`), 576 answers identical bit for
bit; `GreyMorphology` does a grey open and close from the element's rows, `GreyMorphologyTests` 15 cases to the byte against OpenCV, and
the scoreboards and 21 analyses unchanged. Commit 9a38be6c, with the README's Phase 9 line and two release notes. **Section 3.**
docs/PROOF-CHECKLIST.md, Entry 484 section 3: none of the 16 unproven rows can be proven by a machine alone (material for 13, a planning
decision for rows 3, 4's second half and 5's point 2); the 18 named tests exist and passed in the whole Core (3,181) and App (656) suites,
and the bench gate passed, so the 15 proven or met rows still hold. **Section 4** waits for its date, 2026-11-11.

## Entry 485: the iPhone Duo said where a person would look (2026-11-15)

**Section 1.** The published "Apple mobile" section of docs/PLATFORM-SUPPORT.md now says that on the iPhone Duo GroupLab runs, but its
inner screen shows an iPhone-sized window until a fix in Microsoft's iOS tools lets it be built with Apple's newest tools, and that the
Duo is checked only in GroupLab's own tests. `scripts/platform-support.py --readme` carried it into README.md; TESTING-GUIDE.md's "What
is not done yet" has the same line, and docs/TESTING-GUIDE.pdf was rebuilt. platform-support `--check`, claims (5,945 backed),
consistency (0 findings) and readme `--check` pass. When the Duo build lands, this sentence becomes entry 480 section 7's "fills both
screens from nightly N".

## Entry 486: the Apple agreement, signed (2026-11-15)

**Section 1.** App Store Connect refused GroupLab's requests from 2026-11-13 15:24 UTC (403, an agreement not signed or expired). Alan
accepted the updated Apple Developer Program License Agreement and the Free Apps Agreement became Active on 2026-11-15 (entry 487
section 0), before Code ran, so for-alan-archive.md has it as request 108, answered that day. The TestFlight status workflow run by
hand, 43701076184, wrote "Both groups have build 225" to docs/notes/external-status.md at 09:50 UTC. No public page changed. The next
nightly's iPhone and iPad upload is the last check; no nightly has run since 225.

## Entry 487: Play's closed test, the App Store listing, and the App Store pictures (2026-11-15)

**Section 5** (first): the TestFlight check run by hand (43701076184) reads both groups on build 225 again; entry 486 records it, and
request 108 is in for-alan-archive.md, answered. **Section 3:** docs/store/PLAY.md and LISTING.md record what was set in Play
Console and App Store Connect; `docs/store/play-listing.txt` holds the listing read back through the Play API (66 and 1,479
characters, as set); `scripts/play-screens.py` makes the seven Play pictures into docs/store/play-phone/. **Section 1:**
`scripts/play-upload.py` sends each nightly to `internal` and `alpha`, its notes the build's own noticed lines (at most 500
characters). Found on the way: Play's commit by default cancels whatever is in review and submits the new edit, so the next nightly
would have pulled the first closed release out of Google's review; the commit now asks for `ERROR_IF_IN_REVIEW`, and a run that meets
a review sends nothing and says so. The read-only run (43701402551) showed the in-review closed release as `completed`, so that guard,
not the track's status, is what holds it. tests/python/play-upload-tests.py, 15 checks, runs in CI. **Section 2:** the README, the
download page (its Play card is now the closed test's), PLATFORM-SUPPORT.md, ANDROID.md and both guides say the closed test is in
Google's review, how to join, and the 12 testers for 14 days; claims backed; live as c902f743. **Section 4:** `apple-screens.yml`
built GroupLab Dev from v0.2.0-nightly.225 on macos-26 and walked the seven places on an iPhone 17 Pro Max and an iPad Pro 13-inch
(M5) simulator, dark, every step passing, at 1320 by 2868 and 2064 by 2752; committed in docs/store/apple/ with made-from.json and
uploaded to version 1.0's English (U.S.) sets, APP_IPHONE_67 and APP_IPAD_PRO_3GEN_129, seven each in order (run 43705118949). The
first run's workflow carried two of ios-app.yml's jobs by a copying mistake, so the fitting ran here; the workflow is corrected and
has `upload_only`.

## Entry 488: GroupLab 1.0, still in active development (2026-11-15)

**Section 1.** `Directory.Build.props` is 1.0.0; the next nightly is 1.0.0-nightly.226 (the number is the highest `v*-nightly.*` tag
plus one, whatever the base). `VersionTests.OnePointOhIsNewerThanEveryZeroPointTwoBuildAndReadsAsABuildNumber` holds 1.0.0-nightly.226
above every 0.2.0 build, GroupLab Dev's `-dev` included. A person reads "GroupLab 1.0, build 226" (`SemanticVersion.Words`): in Settings,
This build, above the exact build line (which stays as it was, because it is the first line of every log and crash record); the
phones' Settings, About and their Updates summary; the phone's Capture footer; the update bar and Android's update messages; the
/releases/ headings, with the exact version beside each; the download page's newest-build lines; and the Discord title. `Words` is
never serialized: `SignableBytesTests` showed that a new property on the version changes the signed bytes of every published
manifest. The tag filters that took `v0.` (play-upload, apple-screens, android-emulator) take any numbered nightly; the site's
"Since nightly.N" trimming takes any base. Store strings the build is set to carry: Microsoft Store 1.0.226.0 (package-msix.ps1,
third number = patch times 1000 plus N); Android version code N and versionName 1.0.N (1.0.N-dev for GroupLab Dev); Apple
CFBundleShortVersionString 1.0 and CFBundleVersion N (Apple took a short version that stayed 0.2.0 across every upload, so a fixed 1.0
is expected to be accepted). docs/RELEASE-NOTES.md keeps its machine headings, which the notes, TestFlight, Play and Discord tools
read; the page built from it shows the people form. A noticed note that begins "GroupLab is now version" opens its build's notes
(release-notes.py), and features.json lists it as not a feature. **Section 2.** One sentence (`ActiveDevelopment.Sentence` in the
application) on the README, the home and download pages, the user guide's opening (PDF rebuilt), the desktop's and phones' Settings,
and LISTING.md's Store description; /releases/ no longer says GroupLab is unreleased. RETIRED-WORDING.json retires "There is no full
release yet", "GroupLab is unreleased" and "Phase 1 is unreleased". Claims backed (5,953); consistency 0 findings.
**Section 3, confirmed on nightly 226** (1.0.0-nightly.226, commit 05abdd80, 2026-11-15 14:23 UTC): the Microsoft Store package
1.0.226.0, submitted by store follow run 43724284898; Android version code 226 and versionName 1.0.226 (read from the published
bundle), on Play's internal and alpha tracks (play upload run 43724284865); Apple build 226 with short version 1.0, taken by
TestFlight and waiting for beta review at 14:49 UTC. Play accepted the alpha release with ERROR_IF_IN_REVIEW, so nothing of
GroupLab's was in Google's review by then: the closed test looks approved, and its opt-in link should now be on the track's Testers tab.

## Entry 489: the overnight queue (2026-11-15)

**Section 1, the backup.** The log shows 10 November with a Discord line and no backup line (3 November is older than the log's
week). Every `gh` call in scripts/backup.py now has a limit (3 minutes; 40 for uploads and the restore download) and one retry after
30 seconds; the run has a 50 minute ceiling enforced inside the script; every run ends with a "result:" line naming the failed part.
Run-Automation.ps1 writes a "started" line before each step, and the weekly line names a night with no backup line ("no backup line
on 10 November" today). GitHub holds no backup-2026-11-03 or backup-2026-11-10 release, half-made or whole, so nothing was removed.
Proven by a dry run forcing a timeout (`result: backup failed at copy archive`, exit 1) and one forcing the ceiling (exit 3);
tests/python/backup-tests.py 14 checks. **Section 2, the App Store pre-review:** docs/IOS-PLAN.md, "Before submitting to the App
Store". The one finding: no PrivacyInfo.xcprivacy in the app or the share extension, now added to both. Everything else checked was in
order. **Section 3:** grouplab.org/android-test/, linked from the download page's Play card and every Discord announcement; the opt-in
link's place is `PLAY_TEST_LINK`. **Section 4:** five tablet pictures for each of 7 inch (1920 by 1080) and 10 inch (2560 by 1440) in
docs/store/play-tablet/, from nightly 225's Tab S8 Ultra pictures, by scripts/play-screens.py; not uploaded.

## Entry 490: the Play closed test is open (2026-11-15)

Google approved and published the first closed release on 2026-11-15, as the planning session read in the Play Console. The opt-in
page, https://play.google.com/apps/testing/org.grouplab.app, is `PLAY_TEST_LINK` in website/build.py, so step 2 of /android-test/ is
live (open it signed in with the group's Google account, press Become a tester), and step 3 links the Play page, which opens for a
person once opted in. The download card links the opt-in page; the README's newest-build table links /android-test/, the group and
the opt-in page (scripts/readme.py). PLATFORM-SUPPORT.md, ANDROID.md, TESTING-GUIDE.md (PDF rebuilt) and docs/store/PLAY.md say the
test is open and keep the 12 testers for 14 days line. Discord needed nothing: every announcement already links /android-test/.
Found on the way: entry 488 had backed the download page's build lines as five literal sentences naming build 225, which the first
1.0 build broke; the existing pattern rule now reads "GroupLab 1.0, build N" as well as "Nightly N", and the five are gone.
Error report 45, a diagnostics send with no note from a first start of build 226 on Android, had no error in its log; closed.

## Entry 491: GroupLab 1.0 submitted to App Review (2026-11-15)

The planning session submitted version 1.0 with build 226 at Alan's word, 20:36 Denver on 2026-11-15; it is Waiting for Review, release
manual, free in 175 countries or regions, not offered on Apple silicon Macs or Apple Vision Pro. **Section 1:** recorded in
docs/store/LISTING.md (Apple, "Submitted to App Review") and docs/IOS-PLAN.md section 4; the README's platform row, iOS paragraph and
Phase 8 line, PLATFORM-SUPPORT.md and the download page's iPhone card say it is with Apple's review and TestFlight stays the way in;
four sentences backed by this entry. **Section 2:** `scripts/testflight.py --app-store` reads the newest iOS App Store version and its
build (appStoreVersions with the build included, appVersionState or the older appStoreState) and says it in plain words; the testflight
workflow runs it after every nightly and on its half-hourly schedule and writes an `app-store` section into external-status.md beside
TestFlight's, committed only when it changes. A rejection or an approval also prints a warning and a line for for-alan.md; Apple's
reasons are not in the API, so the line says where to read them. Its self-test covers no version, waiting, rejected, approved and a
live version with a newer one in review. **Section 3:** the check only reads; the nightly's iOS job uploads builds and the TestFlight
step works on builds and beta groups only; apple-screens.yml, the only thing that writes to a version, runs by hand and leaves a set
with pictures alone. Nothing was changed for it.

## Entry 492: the feature-requests forum becomes suggestions (2026-11-16)

**Section 1, written and tested, not yet live:** `website/server/grouplab-discord-worker.py` reads a list of forums, bug-reports
(required, as before) and feature-requests (read when the bot can see it); a post there becomes an issue labelled `discord` and
`suggestion` whose first line names the forum, and a post with no title is "Discord: a suggestion". A state file from before keeps
working: the worker looks for forums again until it has found every one, so the forum made after it first ran is found on its next run.
`--dry-run` finds the forums and prints, by forum, how many posts there are, how many are filed and the ids it would file, and writes
nothing (no issue, no reply, no state, no log; only GET requests to Discord). tests/python/discord-worker-tests.py: 41 checks, 11 new.
It is installed by request 109 (05b289ab), which Alan runs once he has seen today's Oracle backup: RESTORE.md rule 5.1 needs it, and Code
cannot see the Oracle console. **Section 2:** `docs/notes/suggestions.md`, with none filed yet; CLAUDE.md lists `suggestion` issues at
the error reports' points and says none is built without Alan's yes.

## Entry 493: a printer's check typed in from another GroupLab (2026-11-16)

**Section 1:** Check your printer's fifth way, **Numbers from another GroupLab** ("Copy them from a computer or phone that checked this
printer."), on the computer and the phone. The form is decided once, `PrinterProfile.CopyWords`, two decimals of a percent: every checked
printer in Printers and paper now shows "To type into another GroupLab: 99.23% across, 99.41% down" on every platform, since before only
"99.2 by 99.4%" was shown and a pair that rounded alike showed as one number. The typing screen names where the numbers are and takes
"99.23" or "99.23%"; a number outside 85% to 115%, the range any check is held to (`SheetReference.LowestBelievable` and
`HighestBelievable`), is refused in a sentence naming it, and a number that does not read asks again. A typed check is method `Typed`, given
0.3% give or take (the copy has the numbers and not how they were measured), and says "typed in from another device's check on 16 November"
in Settings, on the check's result and in each result it corrects, where a measured one says "measured with a caliper on ..." or "checked
...". A typed check is saved as a card check of the same give or take, marked `typed`: a build before this one reads it as a card check rather than dropping the printer, which its next save of any printer would have taken off every synced device.
**Section 2, in part:** printer checks already travel in Export all my data, Import data and sync (entry 461 carries `printers` as one
setting); tests now prove it for a typed check. Not built: keeping both checks of a same-named printer and asking which to use. Import
names the difference and keeps this device's check; sync takes the newer printers list whole and keeps the one it replaced among its kept
copies, so nothing is lost but nothing asks. Keeping both needs a sync record per printer, with a mark for a deleted printer (merging whole
lists would bring a deleted printer back), and a which-to-use question on both platforms. **Section 3:** the user guide's printer passage
and Settings list (PDF rebuilt), the tour's Printers and paper line, PHONE-PARITY.md's `printer-scale` row and the Features sentence, which
credits Quack; no Features picture shows the wizard (its picture is the check page, unchanged), so none is stale. Tests: Core
`Entry493Tests` (7: the same scale typed as measured, refusals, the words, the saved form, export and import, a second device by sync),
App and Mobile `Entry493Tests` (the wizard walked as a person would, then Printers and paper).

## Entry 494: the feature-requests forum is live; suggestions 48 and 49 (2026-11-16)

**Section 1:** request 109 is answered and moved to the archive; the worker's install of 11:49 UTC is recorded in `docs/RESTORE.md`
under rule 5 with its backup folder and its undo. Entry 492 section 1 is done. **Section 2:** issue 49 (type in a printer's check from
another GroupLab) is assessed in `docs/notes/suggestions.md` as built by entry 493 and closed, naming nightly 228. **Section 3:** issue
48 (Tab after choosing a shot, and the arrow keys) is assessed: posted from nightly 198, it was already built by entry 442 and shipped in
nightly 199, so nothing is left to build; recommended closed, left open for the planning session. **Section 4:** nothing on the server
was changed or deleted; the worker's own `.bak` beside it stays.

## Entry 495: a pooled set saved as a record of its own (2026-11-16)

Question 113 answered (b). **Store:** schema 7 adds `pools` (`SessionStore.Pools.cs`): a key, when it was made, its sheets in order by
`DataExport.Identity`, and its choices (a set, with each sheet's number where its marking may not say, or pictures in order); never a
copy of a shot. A database of version 6 is copied first and gains the table. `SessionStore.Delete` takes a deleted sheet out of every
pool in the same transaction, keeping one mention for each twin that shares its identity. **Logic** (`SessionPools`): save or find the
pool of these sheets, open it from its sheets as they are now (the same `SetPool.Composite` or `CompositeOfPictures` as before), its row
named "Pooled set of N sheets" and dated by its newest sheet (row id the pool's id negated, so one list sorts and filters both), the
words, and the chosen rows for Compare. **Sharing** (`PoolPackage`): one `.grouplab` zip whose `session.json` says `grouplab-pool-1`
(a build from before pools says it is from a newer GroupLab) and each sheet's own session file; opening it brings each sheet in as a
session unless the same picture with the same holes is here, then saves the pool. **Export, import and sync** carry pools (`pools` in
the data file and the database's JSON, kind `pool` in the change files, keyed by its key; a pool new on two devices with Keep both gets
a key of its own). **Computer:** the pooled tab keeps its saved sheets (`PoolSources`, parked with the tab; a set tab's pool by its
set); Accept, Save, closing the tab and closing the window save it; Session records lists pools, opens them, deletes only the pool, and
names the pools a sheet is in before it is deleted; Share writes the pool's file; opening one opens the pool; Compare takes it.
**Phone:** the pooled group is saved as a pool as it shows (set page, copies pooled from Capture, a saved pool reopened); Sessions lists
and opens pools and Compare takes them; Share this pooled set; Delete this pooled set; opening a pool's file. The phone's pooled copies
from Capture had no pooled mark and were saved as one session holding every copy's shots; they are now a pool. **Not done:** a screen to
choose between two devices' versions of one pool (none exists for rifles or loads either), and the sheet question on the phone, which
deletes no session. **Tests:** `PoolRecordTests` (6: figures again and a fixed hole, delete both ways, twins, the shared file on a
second store, export, import, the database's JSON, sync add, change and delete, the upgrade from 6), `PoolSetTests` on the computer
(saved, one row, reopened, shared and opened whole on a second GroupLab, twice) and `Entry420Tests` on the phone.

## Entry 496: the saved pooled set's look kept as built (2026-11-17)

Alan answered "keep it" to entry 495's DESIGN NEEDED line (how a saved pooled set looks in Session records, and the pooled tab's
saved line). The line in `docs/notes/for-alan.md` is marked answered with the date; nothing in the application changed.

## Entry 497: consistency audit of 16 November (2026-11-17)

**1.** Google Play's closed test is open to anyone in the README's Android bullet (from PLATFORM-SUPPORT.md), ANDROID.md, TESTING-GUIDE.md
and the /download/ fold; "with a closed test to come" and "once Google approves it" are retired wording. **2.** The 1.0 wording on
RELEASE-NOTES.md's opening (the historical line kept), the support page's lead, the home page's status line and TESTING-GUIDE.md (PDF
regenerated). **3.** Nightly 226 has notes written from its own commits, saying they were written afterwards; the privacy manifest note
is listed in features.json's notFeatures. **4.** `release-notes.py` counts a suggestion in what it was asked in ("1 change suggested in
2 Discord posts") and refuses a new note whose words name a source its Reported-via also gives (`names_source`, two self-test checks);
the running total in `readme.py` and `website/build.py` reads either wording and says "N changes people suggested"; nightly 228's and
229's entries no longer name the Discord twice and their lines count Discord posts. **5.** The README, the support page and
PLATFORM-SUPPORT.md say a feature-requests post becomes a suggestion that is read and answered. **6.** The tour's Session records and
the README's screen list name saved pooled sets; Quack's thanks name the typed-in printer check. Checks: claims all backed, readme
current, consistency 0 findings, site build ok. The wording was done by the docs worker, section 4 on the main model.

