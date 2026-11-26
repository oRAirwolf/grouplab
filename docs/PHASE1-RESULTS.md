# Phase 1 results

**Brief** `docs/PHASE1-BRIEF.md`
**Branch** `phase-1`
**Reproduce** each table with the command named above it

---

## Where the Phase 1 gates stand

Stated plainly, `docs/NOTES-FROM-PLANNING.md` entry 33 section 5, so that "not yet measured" is never read as "passed". The gates are `docs/PHASE1-BRIEF.md` section 2's.

| Gate | Threshold | Status |
|---|---|---|
| Conformance test 43 | 0.001 in worst bull on a synthetic raster | **Met**, and checked on every test run. Must not regress |
| Paper gate | 0.005 in worst bull on the ten printed sheets | **Met.** Ten of ten through the selected model, worst bull 0.00319 in (M1.5) |
| Photograph gate, flat | 0.005 in worst bull on `main_flat1-3` | **Not met.** The flat frames nearly pass: the frame that decoded every marker is inside on every scoring bull, and the failures are named (M1.11) |
| Photograph gate, mounted | 0.005 in worst bull on the seven usable pinned frames | **Not met: 0 of 7**, and an open requirement (M1.11). It cannot be settled until mounted GroupLab sheets are photographed, which is this weekend's paper session |
| Hole detection, point 1 | At least 25 of 27 on `300_nm_hand_load` with zero false positives | **Met,** as a reproduction of the verified survey result rather than a fresh position check (M2.1) |
| Hole detection, point 2 | 99 percent of holes with no false positives on synthetic sheets, matched at 0.15 in | **Not met,** under the amended tolerance too (M2.2) |
| Hole detection, point 3 | Centre accuracy against truth | Reported, not gated |
| Statistics | `docs/STATISTICS.md` section 15.5 | **Met on points 1 and 3 to 6**: 45,476 keys compared with nothing pending ("Entry 28"), coverage 94.73 percent, the Monte Carlo table within tolerance on all 490 cells. **Point 2 is met on the shots and not on the series**, because the two fixtures group shot 242 differently |

**Not a brief gate, and new:** the whole path from image to group now runs as one command and matches synthetic truth ("Entry 33" below).

**Not a brief gate, measured for the first time: the Phase 0 gate record on Linux and macOS**, entry 32 section 3.
- **Windows** reproduces it byte for byte.
- **Linux** reproduces every table, and its records differ only below the precision any table reports.
- **macOS** reproduces the paper and photograph gate tables, and differs in measurements 1 and 2.
- **Linux: explained,** on entry 48 section 2's three terms. **macOS: not yet explained,** and localised to S2 ("Entry 48" below).

---

## What is not done, and why

| section | what | why |
|---|---|---|
| 4.3, 4.4, 4.5 | the server purge, deletion as part of the intake run, and the backup question | all need SSH; the backup question is a read on Alan's list |
| 6.1, second half | deleting the six from the old server | SSH, after the install |
| 6.2 | the redirect | SSH, and only after the new page is live and tested |
| 8.2 | one real test submission through the live page, and one real crash report | the page is not live until the install has run |

## Entry 498: question 114 answered, Find holes on report 50's diamonds, the App Store audit, a pooled set on two devices (2026-11-18)

**0.** Question 114 answered (a): the iPhone builds stay 1.0.<build>; moved to the answered archive, nightly.yml unchanged. Whether
nightly 232 reaches TestFlight as 1.0.232 is checked once the nightly this commit makes has run. **1.** The two holes Find holes misses on
error report 50's scan are refused by the light-in-print pass ("a light spot without print all round it", 0.47 in): in the brightest
channel the orange diamond is as light as the paper, so each hole's white middle and the orange are one patch. A light pass on the
picture's neutral lightness (twice the grey less the brightest channel) found box 5's hole with nothing false on the scan, but refused
box 4's as straight sided at 0.87 round against printed squares at 0.83 to 0.88, and on the other local pictures it added at least 40
marks on 18, 7 on 5 store-bought blanks never shot; not kept, Find holes stays at 9 of 11 with nothing false (448 ms on the scan).
Nothing from the scan was committed. 1.3, more speed on a scan, was not taken. **2.** The App Store audit, by the docs worker: 6
findings, all fixed (three on the home page's words, two in TESTING-GUIDE.md with its PDF, one in docs/store/LISTING.md); four retired
phrases; no "1.0 (N)" numbering after 231 anywhere; the tour pages have no TestFlight-only wording and still describe their screens.
**3.** A pooled set changed on two devices was already kept both ways by sync (entry 495), but nothing showed it: the banner and the
choose prompt listed sessions only. Now `SyncEngine.PoolVersions` gives the two versions, `SessionPools.Version` reads each as this device
does (its sheets by name, how many are not here yet), and `SyncScreens.ComparePools` lays them out as rows (sheets, not here, pooled as),
a sheet not here yet never a difference on its own. The Sessions banner counts pooled sets ("1 pooled set to choose between"), the pool's
row carries the 2 versions tag, and the session prompt's card shows the pool with no plot and "Keep both as two pooled sets", on the
computer and the phone. `CloudClientTests.APooledSetChangedOnTwoDevicesKeepsBothUntilChosen` (six runs, three clouds, clocks hours apart,
late, out of order and half written files) holds that a pool reaching a device before its sheets asks nothing, that both versions survive,
that the asked device's two versions are the desk's and the phone's, and that each of the three choices ends with every device the same;
`Entry463Tests` and `Entry463PhoneTests` hold the banner, the tag and keeping both. The screen's look is a DESIGN NEEDED line in
for-alan.md. The user guide and the Features sentence say it.

## Entry 499: Snap to holes, the snap staying put when unsure, and a night with no backup (2026-11-18)

**0.** The nightly backup of 18 November never started: Windows Update restarted the computer twice at 20:15 Denver on the 17th and
nobody signed in until 06:16, and the task runs only while somebody is signed in, so at 02:30 there was no session to run in (its
StartWhenAvailable did not help, since the task could not run signed out). Run by hand: backup-2026-11-18, "result: backup ok", and its
restore test passed (8 files, 2 joined from parts). `automation-report.py` now writes "No nightly backup since <day>" in for-alan.md when
the newest backup is more than 26 hours old, and CLAUDE.md has Code run it at the start of every run (backup tests 27 of 27). The
Discord attachments step of the hand run left two folders on the server "would not unpack": run from Git Bash, GNU tar came first on the
PATH and read C:\ as a host; `Get-DiscordAttachments.ps1` now calls Windows' tar by its path, and both folders were fetched and
checked. **1.** The tester's reply is recorded under report 50 as Alan's yes. **2.** Snap to holes, on by default, in the marking toolbar
beside the tools and on Shooting defaults (Find a setting finds it by "snap" or "manual"), remembered in settings: off, a new impact goes
exactly where it is let go and the preview is a ring at the pointer with no dashed line; Alt (Option on a Mac) held does the same for
one placement; the log notes "snap off" or "snap skipped (Alt)" at debug level. Alt clashes with nothing: holding it shows the shortcut
labels as before, and the window has no menu bar for its release to move the focus to. A dragged shot was already placed exactly where it
is let go (`MoveShot`, no snap); the Impact tool's summary now says that only a new impact snaps. The phone does not snap, and the parity
table and the guide say nothing about snapping there. `Entry499Tests` holds the default, the switch, Alt and a restart. **3.** Rebuilt
from report 50's measurements: a ring torn where orange meets the black band, and two rings touching 41 pixels apart (a hole 52
across). Measured before: a click on the lower ring's centre went 41 pixels to the upper ring, and clicks on the torn ring moved 6 to 32
pixels and ended 19 to 23 off it. Now the outline step takes a passing circle within half a hole of the click before a cleaner one
farther off (`Snapping.NearShare`), so every click on either ring lands within 0.2 pixels of its own centre; and with no outline found, a
dark weight that would move the mark more than a quarter of a hole (`UnsureShare`, 13 pixels here) leaves it where it was clicked, with
"Placed where you clicked: GroupLab could not find the edge of a hole here" on the notice line. The quarter: on the rebuilt torn ring
the weight never landed nearer than 19 pixels, so a smaller move cannot be trusted either, and a hole on paper is found by its outline
and never reaches it. On the rebuilt torn ring the outline step itself accepts a circle shifted onto the orange, so that case is not
fixed; on the real scan box 4's torn hole keeps 4 of 6 off-centre clicks where they were made and box 5's lands on one place about a
fifth of a hole from its white middle. A light hole closed in by dark print found by its outline was not built. `SnapStaysPutTests` holds
the touching pair, a click on print with no hole near it, and a hole on paper; the 598 Core marking and snap tests pass unchanged.
**4.** The tester's build 231 result is reproduced when the scale reads the scan as about 800 dots an inch instead of 600 (the "4"
proposed; with a caliber, the pair as one): their scale was most likely about a quarter short. Find holes finds 7 holes at 550 or 650
against 9 at 600. **5.** The line for Alan to post on Discord is in for-alan.md.

## Entry 500: the App Store is the way in on iPhone and iPad; the public TestFlight invitation retired (2026-11-18)

By the docs worker, checked on the main model. **1.** The download page's iPhone and iPad card says only that GroupLab 1.0 is on the App
Store, which gets a new version when one is submitted and approved; the "Join the iPhone and iPad beta" button, the TestFlight sentence
and point, and the "Install TestFlight first" fold are gone, and so is the TESTFLIGHT constant, used nowhere else; the Updating paragraph
and how-it-works.json no longer invite anyone to the beta. **2.** The README's download row links the App Store (from `readme.py`, whose
constant became APP_STORE), and the public-beta wording is gone from its lines and from docs/PLATFORM-SUPPORT.md (the README's platform
statement regenerated from it), the user guide and the testing guide (both PDFs regenerated); the GPL section 7 permission and the
statements that each nightly goes to TestFlight stay, since the beta goes on for its existing testers. `consistency.py` checked the three
documents link the TestFlight invitation and now checks they link the App Store. The two claims keys for the old button are gone and nine
new decided entries cite this entry; "Join the iPhone and iPad beta" and the public link are retired wording. Checks: readme current,
claims all backed, site build ok, consistency 0 findings, Core suite passing. Kept on purpose: the support page's line telling existing
testers how to send feedback from TestFlight.

## Entry 501: GroupLab for Mac on the Mac App Store, built as far as the sandbox; signing waits on request 110 (2026-11-18)

**1. The sandbox audit** is docs/MAC-APP-STORE.md, read from the code: GroupLab's own files all land in the container and work as they are;
a session's picture, reopened later by path, is now copied in the sandbox into GroupLab's own `originals/` folder, once a picture by its
SHA-256 (`MainWindow.KeptHere`); the sync folder is reached through a security-scoped bookmark made when it is chosen and resolved before
the first run (`MacSandbox`, `syncBookmark`); Save for a printer app asks for a folder in the sandbox; links and files open through
NSWorkspace (`TheOutsideWorld.OpensThings`); Open marking already asks for a photo it cannot see; the desktop has no camera and its M834
path is Windows only; the network is client only; the JIT is the one runtime exception kept. **Nothing needs a temporary exception**, so
nothing stopped the build. **2.** Universal purchase on GroupLab's existing record, so the store copy is `org.grouplab.app` (the download
stays `org.grouplab.GroupLab`, and the two can sit on one Mac with their own records); the App Privacy answers hold. What only Alan can
do is request 110 (first numbered 107, which the Discord tester notes already had), written at the start: macOS on the record, the Mac Installer Distribution certificate, the Mac App Store profile.
**3.** `mac-store.yml` after each nightly: the store copy published with `GroupLabDistribution=store` (updater off, and Settings says the
Mac App Store keeps it up to date), bundled by `macos-bundle.py --store-build` (version 1.0, the nightly's number as the build, the
encryption answer, the profile inside), checked inside the sandbox by `--self-check` (reads the demo sheet, accepts and analyzes it,
saves a session, keeps the picture, follows a bookmark, all in the container), and, once request 110's secrets exist, signed for the
store, packaged with `productbuild` and sent to TestFlight's internal group. Run by hand on GitHub's Mac machines (three runs on
2026-11-18): the store copy publishes and bundles; signed ad hoc it starts in the sandbox, which makes its container, and is then stopped,
because the sandbox refuses it the connection to LaunchServices every window application needs ("unable to get application ASN from
launchservicesd ... aborting"); a copy signed for the store runs only once installed from TestFlight. So the step reports in the summary
without turning the run red, and the proof that it reads a sheet and saves a session in the sandbox needs a development signature or the
TestFlight build on a real Mac. The first two runs also found the .NET libraries in Contents/MacOS must be signed, as the download signs them. Held off a Mac by `MacBundleTests` (the store copy's plist and
profile) and `Entry501Tests` (a picture kept once in `originals/`, the open hook, the store's words). **3.4** waits for the submission
entry. **4.** Nothing was submitted; the line saying GroupLab for Mac is ready to submit waits until a TestFlight build works.

**Signed and on TestFlight, 2026-11-18, request 110 answered** (first numbered 107, which the Discord tester notes already had; renumbered, archived, and `ForAlanTests.EveryRequestNumberIsUsedOnce` holds the numbers apart). Two faults were found before the first upload and fixed in 94827fba: `testflight.py` read every build on the record, so the first Mac build, sharing the iPhone build's number, would have been taken for the newest iPhone build and added to Public Beta and submitted for Beta App Review; it now reads the iPhone and iPad builds only (`filter[preReleaseVersion.platform]`), and `--mac-internal` gives a Mac build to GroupLab Team and nothing else. And .NET's `createdump`, a second executable the store refuses without a sandbox of its own, is left out of the store copy. The first run (44409935372) found one more executable in the bundle, the command line `grouplab`, and signed it into the app's sandbox (app-sandbox with inherit); the copy was signed with Apple Distribution, packaged by `productbuild` with the Mac Installer Distribution certificate, and altool sent it; its GroupLab Team step stopped on a missing pyjwt. The second run (44411686807, 14a27170) found Apple already had build 233, waited for nothing, and read Mac build 233 as valid and already in GroupLab Team (the internal group's automatic distribution). The half-hourly TestFlight step on 14a27170 then moved the iPhone 1.0.233 on as before and never touched the Mac build. Whether the copy runs in the sandbox, reads a sheet and saves a session is request 111, on a real Mac.

## Entry 505: iOS 18 allowed with an unsupported warning, question 116 (a), and the week's queue (2026-11-20)

**1.** The iPhone and iPad application, its share extension and OpenCV are built for iOS 18.0 (`SupportedOSPlatformVersion` in both
projects, `IOS_MIN` in `ios/opencv/build-extern.sh`), the three lines entry 502 measured on `ios/floor-18-trial`; the trial's workflow
changes were not taken, so there is no iOS 18 leg and the self-test stays on iOS 26. The branch is in the trash
(`C:\Dev\grouplab-trash\2026-11-20\ios-floor-18-trial.bundle`, verified) and deleted here and on GitHub. Below iOS 26 the phone's start
shows `UnsupportedIosView` before the first run's questions (`Shell.Begin`), in Alan's words, with the switch that turns "Try GroupLab
anyway" on; the acceptance is kept with the version (`unsupportedOsAccepted`), and `UnsupportedIos.Due` shows it again only below 26
on a version not accepted, never on 26 or later, never on Android or a Mac running the iPad application (`IosPhone.IosVersion`).
**"Not now" differs from the words:** iOS gives an application no way to close itself and Apple's guidelines ask that it never does,
so it stops the start, saves nothing, and says "GroupLab has not started" with a way to read the warning again. Settings, About says
"Unsupported iOS version (18.6): no support, no guarantee." on such a device. A scripted run accepts it as a person would
(`Scenario.AnswerFirstRun`). Error reports from below 26 are headed "unsupported iOS" in `docs/notes/error-reports.md` (section 5).
The way out in IOS-PLAN.md is live, unchanged. README, PLATFORM-SUPPORT.md (and so the download page), the user guide and its PDF say
"Requires iOS 26 or later. It also installs on iOS 18 to 25, without support."; the App Store description gains it with the next
version Alan releases, set in App Store Connect (docs/store/LISTING.md); nothing was submitted. **Tests:** `Entry505UnsupportedIosTests`
(the show and hide rule over 12 versions, Alan's words, the switch and Not now, the shell remembering the version and asking again on a
new one, the About line). The OpenCV archive is rebuilt for 18.0 by the next `ios` run on main, before the nightly's iPhone build.

**2.** Question 116 is closed, (a): `scripts/testflight.py` (`mac_testers`) already returned the link and never printed it; its line now
says only that Apple approved the Mac build for Mac testers and where Alan copies the link (Apps, GroupLab, TestFlight, Mac testers,
Public Link), and that line is what reaches external-status.md and so for-alan.md. Self-test passed.

**3.1.** Nothing to build. Entry 495's "sheet question on the phone" is the computer's question before a sheet a pool uses is deleted
(`SessionPools.DeleteSheetQuestion`); the phone deletes a session only when it is a reading nobody saw (a stopped reading, a picture
taken again), so no saved sheet can be deleted from under a pool there. The day the phone gains a delete for a saved sheet, it asks
that question.

**3.2 (a).** Look 1A's "?" beside an unsure line's hole was drawn at the hole's top left whatever lay there, on the computer and on the
phone's Fix holes page, so it sat on the hole's own tag whenever the tag had gone there. Now it is drawn after the tags, at the first
corner round the hole (top left first, then the other corners, then the four sides, further out in steps) that lies on no tag, bull
number, hole or other "?" (`MarkTags.Questions`; `MarkingCanvas`, `BullLineInk.Questions` on `MarkingAPage`). On the crowded sheet at
four zooms and the hand-marked target at three, with every hole asked about at once, none lies on anything (`MarkTagsTests`, 2 new);
with corners alone 2 of 26 did at the smallest zoom, hence the sides. The phone's result picture still draws no tags: whether it should is
a DESIGN NEEDED line in for-alan.md, and the next item was taken.

**3.2 (b).** Not built: question 117. Keeping both checks of one printer means each printer travelling in sync on its own, and the
App Store's GroupLab 1.0 and earlier nightlies sync the printers as one whole list, so a newer build that stopped writing it would look
to them like a device that deleted every printer. (a), a record per printer with the whole list still written for older builds, is
recommended; the "which to use?" question would then be a DESIGN NEEDED line.

**3.2 (c).** Entry 499 section 3.3's light hole closed in by dark print, built. With no design known the snap now looks first for a light
middle with dark all round it (`Snapping.LightOutline`): centres within half a hole of the click, light themselves, a light-middle radius
from a quarter to three quarters of a hole, counted where 41 of the 48 directions (`LightShare`, 0.85) show light just inside and dark
just outside, then refined twice by a circle fitted where the dark starts. On the rebuilt torn ring of error report 50 (orange meeting
black, `SnapStaysPutTests`) every one of the seven clicks lands 0.5 to 0.6 pixels from the centre, against 19 to 23 off before; a hole
through solid black on a white-lid scan, which used to stay where it was clicked, lands within 1.5 pixels. A hole dark all through finds
no light middle and goes on to the outline as before; a ring on paper finds its own centre either way. The touching pair, a click on print
with no hole and a hole on paper are unchanged, and all 3250 Core tests and the 63 App marking and snap tests pass.

**3.3.** Nothing to write that needs nobody. Of the 45 articles 44 are published; the one draft, `aim-points-by-optic-class`, waits on
request 86 (the range sitting through other optics), which the entry skips. Every other row under "Worth an article?" in RESEARCH.md
already carries its decision and reason; today's snap fix is added there as not written.

**3.4.** Measured again and nothing changed (PERFORMANCE.md, entry 505 section 3.4): the scan's stages 795.5 ms, the photograph's 811.7;
what is left is the photograph's codes (question 106 (b)), hole finding (taken four times) and the marker step, which is OpenCV's
detector over the page and could only be made cheaper by moving the corners.

**3.5.** The consistency pass (docs-sonnet) read the README, PLATFORM-SUPPORT.md, the user and testing guides, the store listings and the
site's features, links and tour against 1.0 and this entry, and found nothing false: every iPhone line says the App Store and the iOS 18
sentence, every Android line the open closed test, the newest nightly is 235, and no retired wording is left. The tour's two sentences
about the "?" ("beside the hole") still hold. `Get-TargetSubmissions.ps1` is **not retired**: submissions no longer need it, but its
`-CrashReports` pull is still the only way to fetch an error report's log package, and the issue each report becomes says to use it;
for-alan.md says so.

## Entry 504: the pooled set's choice as concept A, question 115 (a), and Mac testers for request 111 (2026-11-20)

**1.** Entry 498's plain rows are replaced by concept A on both platforms, from one shared model, `PoolChoices.Build`
(`src/GroupLab.Core/Sync/PoolChoices.cs`): each version's sheets in its own order, read by `SessionPools.InOrder`, each tagged
**same** (both have it), **only in this one** (amber, counted) or **not here yet** (grey with a dashed edge, `DashedTag`, never counted
and never amber, even when the other version lacks it too); a pool naming one sheet twice is the same only as often as the other names
it. The count is the amber items only, how it was pooled once plus each sheet only in one version, and the line names them ("2
differences: how the sheets were pooled, and sheet 3"; the phone's "3 differences: how pooled, sheets 3 and 4"). The computer's
`ShowPoolConflict` is a dialog 820 wide: the line, the two cards side by side (this computer's on the left, "made", the other's
"arrived"), and Decide later (text only), Keep both as two pooled sets, Keep this computer's, Keep the other's, right-aligned. The phone's
`ShowPoolChoice` is a full page, Choose a version, with Back, the cards stacked and the four choices fixed at the bottom in two rows of
two, each 48 tall. The pool is named by the rifle and load its sheets share, else "Pooled set of N sheets", dated by its newest sheet.
Held by `PoolChoicesTests` (five: the three tags, the count and both lines, not here yet never counting, a sheet named twice),
`Entry463Tests` (the computer's choices, line and tags) and `Entry463PhoneTests` (the page, its line, tags, Back and 44 or taller
buttons); entry 498's own tests are kept. The guide's sync part describes it, with a new screenshot, `pool-conflict`, rendered by
`Entry109Tests`. **2.** Question 115 answered (a): "beta or stable" is "numbered release" in README.md, scripts/readme.py and
docs/RELEASE-PLAN.md; the consistency check finds it nowhere else. **3.** `scripts/testflight.py` (`mac_testers`, run by every testflight
workflow run after the iPhone and iPad step, on its own so a refusal there moves nothing here) makes the external group Mac testers where
it is missing, with a public link limited to 5, puts the newest Mac App Store build in it with its notes as What to Test, answers the
export question and submits it for Beta App Review; while a Mac build of the group waits for review nothing newer is added. Only Mac
builds go in, Public Beta never takes one, and nothing is submitted for App Store review; the self-test holds all of that. The link is
never printed (question 116), and request 111 carries the friend's four steps.

## Entry 503: consistency audit of 19 November, seven findings (2026-11-19)

All seven fixed in one change, by docs-sonnet and checked by the main model: the README's In progress state now fits an application in use and still being built; the README says 1.0 reached the App Store and the Microsoft Store from nightly builds on Alan's word, and what the eight release checks are now for is question 115; the testing guide points at Google Play's closed test (PDF regenerated, the old phrase in RETIRED-WORDING.json) and no longer says test build; the guides page says the newest build; the support page names TestFlight feedback for testers still on the beta; Features has cards for the App Store and Google Play's closed test, with their PHONE-PARITY.md rows; How it works says Built, not Planned. Checks: readme --check, claims --check (all backed), consistency 0 findings, the site build ok, 98 Core tests on the README, features, wording, guides, parity and claims passed.

## Entry 502: could GroupLab run on iOS 18? A study on a branch, nothing shipped (2026-11-19)

On `ios/floor-18-trial`, never merged, the app, the share extension and OpenCV were built for iOS 18.0 with the iOS 26 SDK and `ios app` ran once (run 44559880612). The platform analyzer listed 0 warnings on the simulator build, and the device build passed with warnings as errors, so no call GroupLab makes needs more than iOS 18. On an iPhone 16 Pro Max and an iPad mini (A17 Pro) on iOS 18.6 all 88 self-test checks passed, none failed, the sample scan gave 25 shots and mean radius 0.232 in as on 26, and the dark theme, iPad, landscape and desktop comparison steps passed. Not run: Real taps (not in that workflow) and the camera (none on the simulator). The runner had no iOS 18 runtime, so it downloaded iOS 18.6, 8.86 GB, about 18 minutes with the boot; a nightly leg would be about 55 minutes of a Mac runner. Apple takes uploads built with the iOS 26 SDK down to iOS 13, and `testflight.py` reads no floor. The devices helped are the iPhone XS, XS Max, XR and the iPad 7th generation. Recommendation (a), keep iOS 26, with the way out and the middle way written down in `docs/IOS-PLAN.md` in case Alan prefers (c).

## Entry 506: the 2 MOA center dot, printers in sync one by one (question 117 (a)), and tags on the phone's zoomed result (2026-11-21)

**1.** Error reports 52 (diagnostics, package 2026-11-20_5e12187f, pulled read only with `Get-TargetSubmissions.ps1 -CrashReports
-KeepOnServer`) and 53 (the Discord post naming it) were one report: a photograph of `GL-CF9-LTR` read on nightlies 233 and 235 as 16
holes, nine real .30 caliber holes and the printed center dot (0.197 in) of seven bulls, found as 0.14 in marks 0.004 to 0.013 in from
the bulls' centers. With no caliber named the hole size came from the smaller marks, so each real hole was flagged as about 3.3 holes.
The dots read darker in the photograph than the rest of the print the residual is scaled by. `RenderDifferenceHoleDetector` now refuses
a dark mark lying wholly inside a printed solid disc no wider than the largest hole (`PrintedDots`, `InsideAPrintedDot` with 0.02 in of
slack, `DarkerThanThePrint`); a hole beside or across a dot, or one showing light through it, is still found. `PrintedDotTests` darkens
the dots of a synthetic 2 MOA sheet and failed before the change. The photograph was not in the package, so the mechanism is inferred
from the log and the screenshots; the Discord reply asks for the photograph through grouplab.org/targets. Commit ac96c06b; issues
closed.

**2.** Question 117 (a). `SyncEngine` writes each printer's check as its own record (`PrinterKind`, keyed by name) beside the whole
`printers` setting, which it still writes, marked `perPrinter`, for GroupLab 1.0 (build 226) and older nightlies. `MergePrinterList`:
a list from a newer build changes nothing (its printers arrive one by one); an older build's list is merged by name and each printer's
own date, which is taken as the later of its `measuredOn` and its `changedOn` (the entry says "changedOn"; in the code `changedOn` is
only set when a printer is marked calibrated or serviced, so on its own it would be empty for most printers). A printer not here is
added unless it was deleted here on or after its date; a newer check replaces this one, which is kept aside; an older one is left; two
of the same date wait for the person. Nothing missing from an older build's list is removed, and where the merge leaves a different
list, it is sent back as the newer one, so 1.0 takes every printer kept. Two devices changing any record to the same content are no
longer asked about it. The question is `PrinterChoices` in concept A's pattern, on the computer (`ShowPrinterConflict`) and the phone
(`ShowPrinterChoice`), counted in the Sessions banner and offered under the printer in Printers and paper. `PrinterSyncTests` plays 1.0
by its own rules for the list and covers: add and delete between two new builds, Keep both and Use the other's, 1.0 and a nightly
ending with every printer, 1.0 never deleting on the nightly, a nightly's delete not undone by 1.0's stale list, 1.0's newer and older
checks, 1.0's same-day check waiting, and two nightlies merging one 1.0 list without a question. Commit 46d07536.

**3.** The phone's result picture (`ResultView.SheetPicture`) now pinches in and out about its middle (1 to 8 times the fitted size), is
dragged while enlarged without the page scrolling under it, and a tap on it still lights a bull. At the fitted size nothing changes. Once
enlarged, the holes' tags are placed by look 1A's rule (`MarkTags.Place`, the bulls' printed rings where known) and drawn only where
`MarkTags.Overlaps` finds nothing, with the "?" kept clear of them as on Fix holes; pinched back out they go. **What differs from what
the answer pictured:** look 1A moves a tag outward until it is clear, so on every sheet tried, a crowded one of sixteen holes on two
touching bulls included, the tags fit as soon as the picture is enlarged at all; in practice they appear on the first pinch in. If Alan
wants a larger threshold it is one number. `Entry506PhoneTagsTests`. The phone's code is the same on Android and the iPhone.

**4.** STATE.md rewritten.

## Entry 507: error report 54, and the splash on the computer as concept C, animated (2026-11-21)

**1.** The new Discord report was error report 54, not 52 or 53 again: nightly 236 on a Mac, Session records' date column a fixed 96
pixels and the mean radius column a fixed 220, so "2026-11-15, 21:06" read "2026-11-15,..." and the interval was cut. Both columns now
take the width of their widest text in the font they are drawn in (`MainWindow.SessionColumnsFor`), never narrower than before and
capped at 200 and 340 so the sheet's name keeps its room. Columns still cannot be dragged; nothing is cut off now, so that is not
proposed. `CrashReport54Tests` failed before the change. Commit f7d53745; issue closed; the Discord reply is in for-alan.md.

**2.** The splash (`StartupSplash`), concept C as the canvas draws it: 600 by 360, the dark ground and its 30 pixel grid, the mark, name
and `SemanticVersion.Words` ("GroupLab 1.0, build 236"), the group on its crosshair and ring with "0.47 in", and the steps bottom left,
the current one bright and finished ones dimmed with ", done". Three real steps: **Opening your records** and **Loading the target
library**, which now run on the thread pool from the first line of `Main` beside Avalonia's own start-up (`StartupWork`; the window's
constructor takes what they opened, and a failure is thrown in the constructor exactly where the window always reported it), then
**Getting the screens ready**, the main window's constructor. `StartupSequence` marks each step as it really ends: one shot drops in a
step (twice its size to its own, 150 ms), the last fills the group, draws the extreme spread (200 ms) and shows the teal center and the
label; the main window is shown the moment it is built and the splash closes when it opens (or 300 ms after first drawing, if later),
with no taskbar entry, no close button, never taking the focus, centered on the screen (Avalonia's CenterScreen; the main window opens where the system puts a new window, normally the same screen, and a person with two screens is worth asking). The
movement is composition animations on the render thread, so it continues while the window's thread builds; between steps start-up waits
for one frame (at most 100 ms if none comes). With less motion asked for (Settings, or the system: Windows' animation effects, macOS's
Reduce motion, GNOME's animations), the finished picture shows from the start and only the lines change. A window that cannot be built
closes the splash and is thrown from `Main` as before. **What differs from the canvas:** its spread line joined two shots that are not
the farthest apart; the splash joins the two that are, as an extreme spread does; and the mark and name are the brand's lockup art, as in the header, rather than a typed 34 pixel name, which is not on GroupLab's type scale. **Phones:** Android 12 and later's system start screen
stands on #131417 with the icon on #1a1c20 (`values-v31/styles.xml`) whatever the phone's light or dark setting; the iPhone's launch
screen is #131417 (`LaunchBackground` color in both Info.plists). **Not done:** the iPhone's empty outline of the home screen, which
needs a storyboard drawn for every screen size; the canvas's own note says only the icon and the color change. **Time**
(`scripts/startup-time.ps1` now finds the main window by title and prints the splash's time): as it ships, precompiled, 697 ms to the
main window without the splash and 711 with it, the splash showing at 361; in the gate's plain Release build 1,210 before and 1,316
after, inside the gate, the extra being the first window's one-time setup. PERFORMANCE.md has the table. `Entry507SplashTests` (14):
the shots following the steps for three, four and five steps, no added wait when the work is already done, the window waiting for the
work and not the splash, a failed build, less motion, the version words, the window's manners, the drawn picture, the records handed
over once, a records failure reported by the window, and the phones' grounds. The Features page lists it, without a picture: it shows
for under a second and the screenshot walk opens GroupLab already started. The user guide does not describe the first seconds, so it is
unchanged.

**3.** STATE.md rewritten.

## Entry 508: the iPhone's launch screen as an outline of the home screen, proved on the simulator, the logs split, the zoomed result pictured (2026-11-21)

**1.** `LaunchScreen.storyboard` replaces the plain launch color (Info.plist and Info.dev.plist name it with `UILaunchStoryboardName`; the
project names it as an `InterfaceDefinition`, its default items off, so it can never be left out silently). It draws the Capture page
as the iPhone shows it at the usual text size, measured from the Fold's screenshot and the code: the lockup at its least height (36, the
page's 16 plus the logo's 8 from the top of the safe area, `CapturePage.LogoLeast`), the caliber card (147 down, 88 high, corner 10),
Take a picture (250 down, 48 high, the amber pill) and the pair Choose a photo and Print a target (314 down, two equal pills 8 apart),
and the bar along the bottom (72 above the home indicator's area to the edge, the panel color under a one point rule). No words but the
name, which is the application's own lockup, light and dark. The colors are `Tokens.Light` and `Tokens.Dark`, in light and dark
appearances, since the phone follows the system's appearance unless the person chose one in Settings (a launch screen cannot read that
choice, so someone who chose dark on a light phone sees the light outline first). Auto Layout to the safe area makes it the same on
the iPad, the iPhone Duo and sideways. **Where it differs from the canvas:** the Phones board drew a top bar with "GroupLab" in it and
three equal panels; the real first screen has no top bar but the lockup, so the outline follows the screen, as the entry's own rule
(line up with the real first screen) asks. The intro sentence and anything below the pair are left out: their height depends on the
width and the text size. **ibtool refuses user-defined runtime attributes in a launch screen** (ios app run 44903286973), so the
rounded corners are stretchable 3x images whose caps hold the corners, written with the colors and the storyboard by
`scripts/ios-launch.py`. `Entry508LaunchScreenTests`: the plists, the project item, no text and no runtime attributes, each color and
each shape's fill and edge against the palette in both appearances, the lockup byte for byte the application's.

**2.** The ios app workflow now checks, after the simulator build, that Info.plist names the storyboard, that
`LaunchScreen.storyboardc` is in the bundle, and that the compiled asset catalog holds the three colors, the lockup and the three
shapes. **Run 44903286973 failed** at ibtool (the runtime attributes, above). **Run 44903839710 passed** on all three jobs, the launch
screen's checks among them, but its one screenshot at launch came before iOS had drawn anything (all black). **Run 44906341195 passed**
on all three jobs (GroupLab Dev's after one rerun of its real-tap step, below) and its burst caught the launch screen in its first
shot, light: the lockup, the empty card, the amber pill, the two choice pills and the bar, then the Capture page over it. On that
440 point iPhone the intro wraps to two lines, so the real card and buttons sit about 20 points higher than the outline, which was
measured where it wraps to three (402 points); on a phone of the common width they meet. **Not caused by this entry:** GroupLab Dev's
real-tap step on Targets' keyboard Next fails now and then (44891904639 at 09:56, before any of this entry's commits, and 44906341195's
first attempt: the button covered by a light-dismiss overlay or not showing); it passed on the rerun and in 44903839710. What opens the
overlay is listed in STATE.md as work that needs nobody.

**3.** `scripts/split-logs.py`, run by the chores worker after copying the three logs and the archive to the trash folder: 36 notes
entries and 33 results sections moved (a new `notes-2026-11.md` and `results-476-500.md`); every heading of the 278 in the notes, 48 in
the results and 15 in the questions is still in the live file or the archive, and no link in the repository points into the notes by
anchor. The live notes went from 278 KB to 79 KB.

**4.** GroupLab Dev's scenarios gained a `zoom` step: the result's picture put at the top of the page and enlarged by a quarter at a
time until the holes' tags show (failing if they never do), or by a given factor. The screenshot walk takes `result-zoomed` on the
Fold after the result and zooms back out; `PhoneScreensScenarioTests` runs the walk headless and holds it. The emulator run 44903608845 took it (eede5ffb, `fold-result-zoomed-light` and `-dark`): the whole sample sheet at the first step
in, 1.25 times, every hole's number in its tag clear of the rest, as entry 506 found. It is now the phone picture of "Every hole
found" on the Features page and of the tour's marking page, whose caption and parts were rewritten for it (the landscape result it
replaces there is still taken and still on the analysis page's list of sources). Commit c3fad756.

## Entry 510: the reply with a video on closed issue 53, two new reports, and the worker that reopens a closed issue (2026-11-22)

**1, the suggestions.** Two open: issue 56 asks to rename a session so Session records can tell rows apart; the computer already has
"This sheet's own label" in the shot column, shown after the sheet's name in Session records, but it is hard to find, and the phone has
none. Issue 55's thread adds a large sheet number and tag in the load block's square. Both in `docs/notes/suggestions.md` with a
recommendation (a Rename on Session records rows and the label on the phone; the numeral later, with it); nothing built.

**1, the reply with the video.** Listing every `discord` issue's comments since 2026-11-20 found it on closed issue 53: a 43 second
screen recording on an iPhone, build 238 (2.7 MB, kept on the issue and pulled to `C:\Dev\grouplab-discord`). Frames read with OpenCV
in a scratch environment: caliber chosen first, 9 shots found, no center dot taken, so entry 506's fix holds. Two new faults on the
result's picture (entry 506 section 3's zoom): every hole circle a fixed 9 units whatever the zoom, and after the first pinch the
picture kept every touch from the gesture recognizers (entry 374 section 4's guard against the page scrolling), the pinch's own
included, so it could be zoomed neither further nor back. Fix holes' viewer had the same guard and the same fault. Fixed by
`TwoFingers`, which both pictures use to follow two fingers themselves while enlarged, and by `SheetPicture.HoleRadius` (9 units fitted,
the bullet's own size once enlarged, as Fix holes draws it). `Entry510PinchTests` raise real touch pointers; the two pinch tests fail
with the tracking switched off. Issue 53 reopened for the reply, closed with the commit.

**Also, new bug report 55** (taken first, CLAUDE.md): a filled load block printed its bottom-right square empty when the serial was not
four Crockford characters ("T-0002", or none), because the refused instance code drew nothing, and the preview always drew the blank
block. Now the square carries the sheet's code and the serial in words when it has no code, `PrintPanel.Options` draws the preview
with the PDF's own choices and follows the fields as typed, and the line under the preview says what the code needs
(`Report55LoadBlockSquareTests`, `PrintScreenTests.TheFilledPreviewIsThePdfsPageAndTheLineSaysWhatTheSquareNeeds`).

**2, the gap.** `grouplab-discord-worker.py` now reopens a closed issue and labels it `new-reply` when a later message in its thread
is by anybody but the server's owner (read once from Discord and kept in the state); the owner's own answers stay comments.
`tests/python/discord-worker-tests.py`: 45 pass, four of them new. Installing it needs sudo and Alan's look at the Oracle backup, so it
is request 112, with its install and undo written in `docs/RESTORE.md`. Until it is proven, `scripts/closed-replies.py` lists closed
`discord` issues with a reply after closing that nothing has answered, beside the open issue list (CLAUDE.md).

**Worth an article?** No: a fix of two faults, nothing another shooter or developer would do differently.

## Entry 509: the consistency audit of 2026-11-22, five findings (2026-11-22)

**1.** The iOS 18 to 25 line no longer sits beside the App Store link as if the App Store copy installed there: README, PLATFORM-SUPPORT.md
(and so the download page's table), USER-GUIDE.md and its PDF say GroupLab installs on iOS 18 to 25 from build 236, and the App Store
copy (version 1.0, build 226) still needs iOS 26 until its next version. **2.** The download page's App Store panel now reads the
released version and its build from `docs/notes/external-status.md` (`app_store_copy`, `app_store_words` in website/build.py), so it
follows the next release by itself, and says what later builds changed on iPhone and iPad reaches the App Store with its next version;
the Features card "On iPhone and iPad, from the App Store" says the same, marked reworded for nightly 239. **3.** "the newest build" in
the user guide and "the guide to trying GroupLab for the first time" in the guides page's description; "newest test build" and "trying
the test build" are in `docs/RETIRED-WORDING.json`. **4.** README and ANDROID.md say each nightly goes to Play's internal test and its
closed test, the closed track only when Google is not reviewing an earlier one (entry 487, play-upload.yml `--tracks internal,alpha`).
**5.** STATE's iOS line: on the App Store, the nightlies to TestFlight for the testers already in it. The new site sentences are backed
in claims-backing.json. Nothing was submitted to the App Store; that stays Alan's word.

**Worth an article?** No: wording kept in step with what GroupLab does.

## Entry 511: naming a session (suggestion 56), and the Discord bot posting Code's replies (2026-11-23)

**2, taken first, because it ends in Alan's paste.** The Discord worker (`website/server/grouplab-discord-worker.py`) now reads
`discord-replies/` in `grouplab-crash-reports` each run, with the GitHub token it already holds, and posts at most five waiting replies,
each once, in the thread its issue was filed from (`thread_of`, the worker's own state; a `thread:` line that disagrees is refused). The
words go as plain text, `allowed_mentions` empty and link previews off; then a comment on the issue with the words and the message's
link, and the file moves to `posted/`. Refused, with a line on the issue and the file moved to `not-posted/`: a locked or gone thread, a
message by the server's owner since the reply's `written:` time (read by the Discord id that time gives), and words that break the rules
(`reply_problems`: bold, an at sign, a link other than grouplab.org or GroupLab's releases, over 1900 characters, no closing line "(Posted
by the GroupLab bot for Alan.)"). `post-after: nightly N` waits until GroupLab's public releases list nightly N. An archived thread that
is not locked is opened again first. A run that stopped between posting and keeping the link finds its own message and posts nothing
twice. `discord-replies/PAUSED` stops everything. The dry run says whether the bot may post in each forum's threads, worked out from its
roles and the forums' overwrites (`thread_permissions`), and what each waiting reply would do. 68 checks pass in
`tests/python/discord-worker-tests.py`, 23 of them new. Committed at 9cf53235; request 113 installs it (a look at that day's Oracle boot
volume backup, six hashes, a dry run that stops if the bot may not post, the change backup, the install, one run). RESTORE.md has the row
and the undo. The backlog: 53-1 (the second pinch and the circles), 55-1 (the load block square) and 56-1 (session names, being built)
are in `discord-replies/`; report 52's reply and report 54's were not written again, because Alan had posted both on 21 November.

**1, suggestion 56, yes to (a) and (b).** The name is the label the analysis screen already kept (`MarkingState.SheetLabel`), so no
schema change: `SessionStore.Rename` writes it into the stored marking (`MarkingFile.WithLabel`) and the stored sheet name, which keeps
its old shape ("target, name") so a device on an older build reads it as before; `SessionSummary.Label` is read from the marking in
the list's query (`json_extract`, guarded by `json_valid`). `SessionNames` puts the name first: it names the session (in place of the
load, still told apart by date and time where two share it), and the line beneath is the target alone; `Titled` makes the Session
records sheet column "name, target". On the computer: Rename in each session row's menu (Open, Rename) and on F2, typed in place, Enter
to save, Escape to leave it (`MainWindow.SessionNames.cs`); the box at the top of the analysis column as "Name this session", beside
"on" and the target's name, Enter or leaving it saves; the tab is "name, target". A tab holding the renamed session, showing or
parked, takes the name, so its next save keeps it. On the phone: the same box on the result page (`SessionNameCard`), Rename a session
under Sessions, and the result's title "name, target"; an open result of the same session takes the name. Tests: 6 in
`SessionRenameTests`, 2 in `Entry511Tests` (App), 2 in `Entry511PhoneTests`; the Core, App and Mobile suites run one after another
before the push. User guide section 6 and its PDF, the Features page (`session-names`, since next), the tour's Session records and
analysis stops, and PHONE-PARITY.md changed with it. The large numeral in the load block's square stays open as "later, with session
names"; whether the session's name should print there is request 114. Issue 56 closed with the commit, for nightly 241.

## The archive

Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.

- [`docs/notes/archive/results-026-050.md`](notes/archive/results-026-050.md), entries 026 to 050, 16 section(s).
- [`docs/notes/archive/results-051-075.md`](notes/archive/results-051-075.md), entries 051 to 075, 10 section(s).
- [`docs/notes/archive/results-076-100.md`](notes/archive/results-076-100.md), entries 076 to 100, 6 section(s).
- [`docs/notes/archive/results-101-125.md`](notes/archive/results-101-125.md), entries 101 to 125, 26 section(s).
- [`docs/notes/archive/results-126-150.md`](notes/archive/results-126-150.md), entries 126 to 150, 15 section(s).
- [`docs/notes/archive/results-151-175.md`](notes/archive/results-151-175.md), entries 151 to 175, 25 section(s).
- [`docs/notes/archive/results-176-200.md`](notes/archive/results-176-200.md), entries 176 to 200, 23 section(s).
- [`docs/notes/archive/results-201-225.md`](notes/archive/results-201-225.md), entries 201 to 225, 21 section(s).
- [`docs/notes/archive/results-226-250.md`](notes/archive/results-226-250.md), entries 226 to 250, 25 section(s).
- [`docs/notes/archive/results-251-275.md`](notes/archive/results-251-275.md), entries 251 to 275, 27 section(s).
- [`docs/notes/archive/results-276-300.md`](notes/archive/results-276-300.md), entries 276 to 300, 23 section(s).
- [`docs/notes/archive/results-301-325.md`](notes/archive/results-301-325.md), entries 301 to 325, 15 section(s).
- [`docs/notes/archive/results-326-350.md`](notes/archive/results-326-350.md), entries 326 to 350, 17 section(s).
- [`docs/notes/archive/results-351-375.md`](notes/archive/results-351-375.md), entries 351 to 375, 21 section(s).
- [`docs/notes/archive/results-376-400.md`](notes/archive/results-376-400.md), entries 376 to 400, 30 section(s).
- [`docs/notes/archive/results-401-425.md`](notes/archive/results-401-425.md), entries 401 to 425, 26 section(s).
- [`docs/notes/archive/results-426-450.md`](notes/archive/results-426-450.md), entries 426 to 450, 27 section(s).
- [`docs/notes/archive/results-451-475.md`](notes/archive/results-451-475.md), entries 451 to 475, 35 section(s).
- [`docs/notes/archive/results-476-500.md`](notes/archive/results-476-500.md), entries 476 to 500, 20 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 145 section(s).

## Entry 512: the Discord bot reads help and support and marks every message; a session's name in the load block; a loose sheet in a set (2026-11-23)

**Request 113, closed.** Alan ran it at 08:03 UTC; over ssh the installed worker's sha256 is bddba5d8..., 9cf53235's, and its log shows
53-1, 55-1, 56-2 and 57-1 posted at 08:04 UTC; the timer is active. Recorded in RESTORE.md with the backup folder and its undo.

**1. The worker reads four forums** (`website/server/grouplab-discord-worker.py`, committed at ddb1e8fd). Forums are found by
`canonical(name)`: lower case with any leading emoji, dash, dot or space removed, so a forum named with a question-mark emoji and a dash before Help reads as help. Help and support threads
are issues labelled `discord`, `question` and the forum's name, posting no "filed" line (`FORUMS[...]["thanks"]`). Every later message on
an issue labels it `new-reply`, open or closed, reopening it if closed (`reopen_for_reply`); the server owner's message is marked "(Alan,
the server's owner)" on its comment and labels it `alan-replied`, never reopening it. Labels are made once each and remembered in the
state (`make_labels`). The dry run lists every forum channel the bot can see and which it reads, says "no forum by that name" for one of
the four it cannot find, names each permission the bot lacks in a forum it reads ("give its role Send Messages in Threads and Read
Message History there") and then ends with code 3. 84 checks pass in `tests/python/discord-worker-tests.py`, 16 of them new.
Request 115 installs it, in the same form as 113, and the paste stops on code 3 or a forum not found.

**2. The standing rule** is in CLAUDE.md under the error reports: at every check, one reply file for every new message from anybody
but Alan or the bot, labels removed once answered, a thread Alan wrote in last left to him, every reply posted listed in for-alan.md.

**3. Request 114, (b) carefully.** `RenderOptions.SheetName`, typed in a new Session name box on Targets (`PrintPanel`), prints in the
load block's square, and a sheet of a set prints its place there, "2 of 3", up to `SceneBuilder.SquarePlaceSize` (8 mm), about four times
the other words (`AddSquareWords`). Both print whenever the square carries words; the lines are centred on the square as a whole, so a
sheet with neither prints exactly as before (`ASingleSheetWithoutANamePrintsExactlyAsBefore`). The code's case has no room: the square
is 280 dmm and the version 11 code 244 dmm with its quiet zone; nothing is added there and Targets says so (`NameNotWithCode`), and
question 118 asks where they should go. The six set designs' printed artwork changed, so `grouplab corpus counts` was run: 55 of 55
images unchanged (the load block is an excluded zone for hole finding), recorded with `--write`. The phone has no Session name box yet;
its sets print their place from the same code.

**Report 57's suggestion, yes to the narrow case.** `SetPool.StandsIn` (one page, the set's page size, the same bulls and rings) and
`SetPool.FindStandIn` find loose sheets ticked with one set's sheets; Pool the chosen counts each as a sheet missing from those ticked,
asks "Which sheet of the set does this one replace?" with a button for each where several are missing, and refuses in amber where none
is (`PoolRefusal.NoPlaceLeft`). The pool saved from it keeps the loose sheet's place in its numbers, and `SessionPools.SharedDefinition`
now takes the set's design for a pool whose other sheets stand in for it, so it opens again whole. `Entry512StandInTests` (Core) and
`Entry512StandInPoolTests` (App) hold it. The user guide, the Features page (`square-name`, and `pool-set` widened) and the Targets tour
page's load block line say both.

**Worth an article?** No: neither is research or a measurement.

## Entry 513: every new build to every store, Apple releasing by itself (2026-11-23)

- **App Store, iPhone, iPad and Mac:** `scripts/app-store.py --run`, from `testflight.yml` after every nightly and every half hour. The
  newest processed build from a nightly with published notes goes on an App Store version named by its own version, release type after
  approval, then to App Review through review submissions; one review per platform at a time, never cancelled; an approved version held
  for release is released; a turned-down version is reused by the next build and said in for-alan.md. The listing is made whole first
  (the iOS 18 to 25 sentence; for a first Mac version the iPhone listing's keywords and links, `docs/store/apple/mac-description.txt`,
  five screenshots fitted to 2880 by 1800 from the desktop screens by `apple-screens.py mac`, and the iPhone version's review contact).
  `--inspect` reads only (run 45169660718 found what the script expects). Self-test of 21 checks, held by `TestFlightTests`.
- **Submitted 2026-11-23 11:59 UTC** (run 45170084864): iPhone and iPad 1.0.242 (build 242) and the Mac's 1.0 (build 240).
- **Mac store copy numbered 1.0.<build>** (`macos-bundle.py`): every store build said 1.0, which Apple would refuse after the first Mac
  release. `MacBundleTests`.
- **Google Play production:** `play-upload.py --production` takes the closed track's newest completed release; until Google allows it,
  one dated blocked line a day in external-status.md. `play-upload-tests.py`, 19 checks.
- **Pause:** `STORE_SUBMISSIONS_PAUSED`, a repository variable or a file, in testflight.yml, play-upload.yml and store-follow.yml.

## Entry 515: the Discord bot's replies end "This is an AI generated response." (2026-11-23)

- `SIGNED` in `website/server/grouplab-discord-worker.py` is the new line; `reply_problems` refuses a reply ending with the old line or
  none, and any reply naming Alan. `discord-worker-tests.py`, 89 checks. No reply was waiting; the nine posted before are unchanged
  (request 117).
- Installed by Code at 12:15 UTC from 5eeeed2b: hashes `OK`, dry run posting nothing, backup first, one run, timer, both sites 200;
  recorded in `docs/RESTORE.md` with request 115's install of 11:02 UTC.

## Entry 514: an overnight queue (2026-11-23)

1. **Google Play's public listing:** request 118, a Discord post in Alan's voice for two or three more Android testers (12 opted in, 5
   days of 14); request 119, answers to Google's Apply for production questions from the record only, for 2 December or after.
2. **The iOS real-tap flake on Targets' keyboard Next:** GroupLab Dev's log of run 44891904639 showed the keyboard room recording the
   keyboard closed seven seconds after it came up, with the box still focused and no tap from anybody, and nothing raising the bar again.
   An input pane saying Open with no rectangle was taken for closed; now it takes the pane's own or changes nothing, the watch runs
   while a box has the focus and raises the bar again, and every close says why (`keyboard.closed why=`). `Entry514KeyboardTests`, four
   cases that fail without it; the phone suite passes (293). proved by 3 green runs of the iOS app workflow in a row, 45174815465, 45179981633 and 45187081635.
3. **Accessibility by machine:** `Entry514AccessibilityTests` (every computer screen, Settings page and window in dark, light and high
   contrast) and `Entry514PhoneReadingOrderTests` (every phone place read top to bottom by the automation tree, both themes); nothing
   found. `docs/ACCESSIBILITY-AUDIT.md`.
4. **Every file GroupLab opens:** a pooled set's file and a picture under another kind's name added to `BadFilesTests`; both pass as
   the code is. GroupLab opens no PDF.
5. **A phone photograph on Android:** the emulator's stages in three runs of the same code, in `docs/PERFORMANCE.md`: the holes 4,207
   to 6,416 ms, the codes 1,291 to 2,497. Question 119 was asked on a wrong reading (the build is already compiled ahead of time) and
   withdrawn; the measuring switch builds with Mono's LLVM code generator instead.
6. **Consistency after entry 513:** the README, the guide and the download page say every store follows the nightlies.

## Alan's queue of 2026-11-24 (in the session), part 1: the phone's Session name box, Delete this session with the sheet question, the App Store wording (2026-11-25)

**1.1. The Session name box on the phone's Targets.** A sheet with a load block has a Session name box under its picture (60
characters, "optional, such as Rifle A 140 gr"), and the picture follows it as it is typed. Print, Share the PDF, both printer-app
shares, the M834 print and the plotter page all print it in the load block's square (`TargetsPage.SessionNameFor`, `RenderOptions.SheetName`,
the computer's entry 512 rendering). The phone prints the load block blank, so the square always carries words and the name always
prints; the computer's case of a filled block's code filling the square cannot arise there. A sheet with no load block shows no box.
**Tests:** `Entry512PhoneNameTests` (2).

**1.2. Entry 495's sheet question on the phone.** Entry 505's 3.1 found nothing to ask it on, because the phone deleted no saved
session. Now a saved session's result has Delete this session: it asks first, plainly or, where pooled sets use the sheet, with the
computer's question (`SessionPools.DeleteSheetQuestion`, naming each pool); Delete takes it out of the store and out of each pool, which
keeps its other sheets, and deletes the phone's copy of the picture once no other session keeps it (`SessionStore.CountWithImagePath`,
since one picture can be several sessions, report 40); Keep it puts the choice back. A change made after deleting does not save the
session again. A pooled result keeps only Delete this pooled set. **Tests:** `Entry495PhoneDeleteTests` (2).

**1.3. The App Store wording** (docs worker). Apple's public lookup on 2026-11-25 has the iPhone and iPad copy at version 1.0.243
(released 2026-11-24 18:22 UTC; 1.0.242 on 2026-11-23 22:33 UTC), minimum iOS 18.0. README, PLATFORM-SUPPORT.md (the download page),
the user guide (PDF regenerated), Features (`app-store`, reworded at nightly 245) and docs/store/LISTING.md now say the App Store copy
installs on iOS 18 to 25 without support and follows the nightlies, each released by Apple once approved; "still needs iOS 26" and "is
with Apple for review" are retired wording. **The Mac's 1.0 is not released:** external-status.md had it in review at 21:34 UTC on
2026-11-24 and the App Store page shows no Mac version, so nothing says it is; the queue's "the Mac 1.0" was taken as still to come.

**Checks:** Mobile suite 300 passed, 6 skipped; Core 3,300 with the bench coverage line for the new classes; App suite before commit;
claims all backed, readme current, site build ok. The usage guard stops at 95 until the week resets (2026-11-26 02:00 UTC).

## Alan's queue of 2026-11-24, part 2: Pool the chosen takes any sheets, on the computer and the phone (2026-11-25)

Alan said yes to the general case of report 57's suggestion. **One planner for both** (`ChosenPools.Plan`, Core): sheets of one set
pool as the set, counted by their codes (entry 243); a loose sheet with the set's bulls stands in for a missing sheet, asking which where
several are (entry 512); **any other mix** (different designs, loose sheets of one design, or a loose sheet when every sheet of the set is
ticked already) pools as separate sheets in the order ticked, each shot about its own bull with its own sheet's scale, the model pooled
pictures have used since entry 423 (`SetPool.CompositeOfPictures`), and is saved as a pool of pictures, so it reopens, shares and syncs
as any pool does. Still refused, in amber: one loose sheet alone, and sheets shot at different distances (more than half an inch apart)
or with different calibers, because their inches do not pool; a sheet's distance and caliber are its marking's or, where the marking has
none, its session's. The words moved to Core (`PoolRefusal`) so the phone says the same. **Computer:** Session records' Pool the chosen
uses it, with the hint "Tick two or more sheets to read them as one group, each shot about its own bull; the sheets of a set count by
their codes." **Phone:** Sessions, Compare loads gains Pool the chosen under Compare; a refusal is an amber card under it, a stand-in's
question is a page of Sheet N choices, and the pool is saved and opened as a saved pool's row opens. **Look:** a DESIGN NEEDED line in
for-alan.md (where the phone's button sits, and whether a mixed pool's key names each sheet's design); built the plainest way meanwhile.
**Tests:** `ChosenPoolsTests` (4), `Report57PoolRefusalTests` (rewritten: pools, saves and reopens three mixed sheets, then refuses one
sheet, distances and calibers), `Entry512StandInPoolTests` (its last step now pools four as separate sheets), `Report57PhonePoolTests`.
Docs (docs worker): the guide's "not built yet" paragraph, Features `pool-set`, PHONE-PARITY.md and suggestions.md's issue 57 entry.

## Alan's queue of 2026-11-24, part 3: Phase 9 on the phones, Android about twice as fast to read a picture, every result identical (2026-11-25)

**Measured:** finding the holes now times its nine steps (`RenderDifferenceResult.Took`), which the phone's log writes as `read.holes`
with its cores and whether code is compiled as it runs, and `phone.detect` ends with a result fingerprint (`ReadFingerprint`, every bull's
and hole's position, diameter and bull, to the bit); the computer's bench files the same steps and prints the fingerprint. On the computer
with 2 cores a photograph's holes take 140 ms, against 5.3 to 10.1 s on the Android emulator, so the cores were not the reason: Android's
compiled code was. **Changed:** Android's Release builds use Mono's LLVM code generator (`EnableLLVM`). On the emulator, two runs side by
side (45520552937, 45520556735): start-up 1,886 to 1,290 ms, the 600 dpi scan read in 9.9 s to 4.5 s (its holes 4.3 s to 1.4 s),
switching targets 243 to 109 ms, the sweep's photographs 9.2 to 16.7 s to 3.6 to 7.8 s; **every result identical**, both pictures'
fingerprints the same in every reading of both builds; the APK 1 MB smaller, the build about a minute longer. `android-emulator.yml`'s
measuring switch is now `no_llvm`. **iPhone:** the simulator in CI runs GroupLab's code in Mono's interpreter (`jit=False`, nothing
compiled ahead of time there), 20 to 37 s a reading, so it says nothing about an iPhone, which runs code compiled ahead of time; the
device build with LLVM is tried in a measuring run, and a real iPhone's figures wait on a sitting (PERFORMANCE.md). The fingerprints
also showed that a picture reads the same every time on each platform, but the iOS simulator and the Android emulator give different
fingerprints for the same picture (arm64 against x86_64, OpenCV's own code paths among them); nothing claims otherwise. **Tests:**
`ReadFingerprintTests`; the bench coverage line for the fingerprint. Details and tables: docs/PERFORMANCE.md.

## Alan's queue of 2026-11-24, part 4: the consistency pass, hole finding on dot-printed scans checked, and a tidy (2026-11-25)

**4.1 Consistency** (docs worker, after parts 1 and 2 had updated the App Store and Pool the chosen wording): the README, the site's
sources, USER-GUIDE.md and PLATFORM-SUPPORT.md agree with each other and the code. One stale sentence fixed: TESTING-GUIDE.md said the
iPhone build "is released on the App Store as version 1.0"; it now says 1.0 was released on 17 November and each nightly that passes its
checks follows once Apple approves it (PDF regenerated). One fault in the site's build, fixed on the main model: the download page's
App Store line read only the status file's old wording ("App Store version 1.0 (build 226): released"), and since entry 513 the status
check writes "On the App Store now: version 1.0.243", so the page still said version 1.0; it now reads either (`app_store_copy`, website
build.py) and says version 1.0.243, from build 243. The platform table's iPhone column reads "first released as version 1.0 on
2026-11-17" rather than naming 1.0 as the copy. Claims all backed, readme current, consistency 0 findings, site build ok.

**4.2 Hole finding on printed and scanned sheets after report 60's fix.** The corpus: `grouplab corpus counts`, 55 of 55 images
unchanged from the record. Then a sweep (a scratch test, deleted once it had answered) of synthetic E-bull and C-bull sheets, nine .22 holes
on the print of each, the bulls printed as a laser printer's screen at 45 degrees (85 and 141 lines an inch, 30 to 60 percent covered,
as a scanner sees it: each pixel the share the dots cover) and scanned at 150, 300 and 600 dpi, each read with the scan check and
without it (as nightly 243 did):

| what was scanned | with the check | without it |
|---|---|---|
| 40 to 60 percent screens, every resolution (GroupLab's blue and red tints print at about 35 to 42 percent on a black and white printer) | every hole found that the plain black sheet finds, nothing extra | up to 86 false holes at 600 dpi with the fine screen, 24 to 35 at 300 |
| a 30 percent screen at 300 or 600 dpi | 3 to 7 of 9 | the same 3 to 7 of 9 |
| plain black print, paper grain up to 20 levels | never read as a dot pattern (texture at most 0.032, against 0.06) | the same |
| a dark backing behind the paper, solid or dots | no hole on the print found | the same: a dark hole in dark print cannot be told from it |

So the fix never removed a hole the old reading found and never added one; it took away every false hole the screens made. The two
limits are older than it: a screen lighter than about 40 percent at 300 dpi and finer, and a scan without the white lid behind the paper.
**Test kept:** `DotScreenScanTests` (a 141 and an 85 line screen at 40 and 60 percent found whole with nothing else, the false holes
without the check, plain print with grain never a dot pattern).

**4.3 Tidy, backups first** (the newest backup 2026-11-24 09:30 UTC, its last restore test passed; announced in panel.md): the packaging
build output in `out/` from 20 September (`publish`, `pubobj`, `package`, 1.16 GB, rebuilt by the packaging scripts) deleted as build
output; `out/guide`, `out/reports`, `out/screens`, `out/sample-check.png` and an old tree listing (about 105 MB, generated, untouched since
September and 11 October) moved to `C:\Dev\grouplab-trash6-11-25\`, kept 14 days; the trash day 2026-11-08 emptied by
`scripts/cleanup.py`. Left alone: `Claude outputs/` (it may be Alan's own), the Phase 0 print set in `out/` and `reference/`.

## Question 120 answered (b): LLVM on for the iPhone and iPad builds, on Android's evidence (2026-11-25)

**Done.** `GroupLabAppleLlvm` in `ios/GroupLab.iOS/GroupLab.iOS.csproj` turns on `MtouchUseLlvm` for a Release device build
(`ios-arm64`) only; the simulator interprets and never gets it, and `-p:GroupLabAppleLlvm=false` builds one without it. The switch, one
line, and its undo are in `docs/RESTORE.md`. `ios/llvm-evidence.sh` runs after the device build in `ios-app.yml` and after the signed
publish in `nightly.yml`: it counts LLVM's object files (`*.llvm.o`) and its arguments in a detailed build log, writes `ios-llvm.txt`
(kept 90 days as `ios-device-llvm` and `ios-nightly-llvm`), a line in the step summary, and a warning when LLVM was asked for and is not
seen. The detailed log itself is deleted on the runner, never uploaded, since the nightly writes it beside the signing secrets.
**Proved before the nightly:** the unsigned device build (ios-app 45543325296, on 4e9031db) says `llvm: ran`, with 230 LLVM argument
lines in its log, LLVM output files named for the assemblies (the four kept in `ios-llvm.txt` include the share extension's and
the SDK's), and two `*.llvm.o`
left in its obj folder; the device job took 5 min 34 s.

**The Mac:** the Mac App Store copy is the computer's application (`mac-store.yml` publishes `src/GroupLab.App` for `osx-arm64`), on
.NET's own runtime and compiled ahead of time with ReadyToRun since question 110; it has no Mono, so there is no LLVM to turn on and
nothing in it changed. Question 120's option (b) said otherwise, and that was wrong.

**Still to do:** the first nightly's `ios-llvm.txt` shows LLVM ran (recorded here when it has); new error reports from iPhone, iPad and
the Mac are read for anything LLVM could cause, and the switch goes off at once if one is; at request 50's sitting, with an iPhone or
iPad to hand, the sweep picture read on a build with LLVM and one without, `read.holes` and the result fingerprint compared, then the
number goes in `docs/PERFORMANCE.md`. The two DESIGN NEEDED looks from part 2 stay as built (Alan).

## Entry 516: the consistency audit of 25 November, and why the Mac App Store copy kept getting a 409 (2026-11-26)

**Section 2, the 409, first.** App Store Connect's review submissions (a read-only inspect, testflight run 45785427242) showed the
Mac's 1.0 (build 240), submitted on 23 November, **rejected** by App Review on 25 November, its review submission left in
`UNRESOLVED_ISSUES` with the item `REJECTED`. `scripts/app-store.py` then did what entry 513 designed, putting the newest build (247) on
the turned-down version, but asked Apple to review that same submission again, and Apple refuses until the rejected item is resolved:
the associated error, now printed, was "Version is not ready to be submitted yet". The refusal's associated errors had been dropped by
`said_by`'s predecessor in `scripts/testflight.py`, so the status note repeated a bare 409 for a day, and the rejection itself was said
nowhere, because a rejection was only reported when no newer build was ready. Fixed: refusals carry their associated errors (named by
kind, never by id) and where the newest version stands; inspect lists the review submissions; a rejection is said for Alan whether or
not a newer build replaces it; `Apple.submit` marks a rejected item resolved, as "Resubmit to App Review" does, and if Apple refuses
that, gives up only that answered submission and makes a new one (never one in review). **Result:** testflight run 45786792184, "App
Store, Mac: build 247 submitted for App Review as version 1.0.247". Apple's reason for the rejection is not in the API; request 121
asks Alan to copy it from App Store Connect, since build 247 will be turned down for the same reason if it is in the app or the listing.

**Section 1:** testflight.yml now asks for a publish of the site when the App Store version in external-status.md changes, as
store-status.yml does for the Store (`WebsiteWorkflowTests.BothStatusWorkflowsPublishTheSiteWhenAStoreVersionChanges`, and testflight.yml
joins the allowed dispatchers). The download page's generated "The App Store copy is version N, from build N" line is backed by a rule
in claims-backing.json, since an exact sentence would have gone unbacked, and the page been held back, with every new version.
**Section 3:** the second tester's steady use through nightly 247 (reports 54, 55, 57, 60 and issue 62) added to PLATFORM-SUPPORT.md,
the README and the download page; the Mac panel says Apple turned down the first version and the newest build went in its place.
**Section 4:** request 120. **Section 5:** the Support and Discord pages say the forum replies are often written by an AI assistant and
end "This is an AI generated response." **Section 6:** pool-set's sentence says Delete this session "asks first", reworded for
nightly 247. **Section 7:** the user guide's phone section and section 6's list name Delete this session; the PDFs regenerated.
**Section 8:** Quack's thanks names the dot-printed scans behind nightly 244's fix. **Section 9:** STATE.md rewritten; also, nightly
247's `ios-nightly-llvm` says `llvm: ran` (question 120's first check).

## Decision log

One line per method choice where there was a real alternative: what was rejected, and why.

- **M0: `markerSize` is 8 modules, over the brief's 10.** TARGET-SCHEMA.md section 3.7, the renderer and FIDUCIAL-DECISION.md all put 8 modules in `markerSize`; 10 would print the wrong modules and two of the five sheets would not render.
- **M0: quiet zone of two modules, over a fixed 10 dmm.** It keeps each marker in the proportion FIDUCIAL-DECISION.md settled at 0.5 mm, so only the module scale changes.
- **M0: `GL-CF25-LTR` as printed, over the 454 dmm sighter gap of the pending geometry change.** The 0.5 mm sheet is then the Phase 0 sheet by identifier, which makes it the sweep's control; the sighter geometry does not bear on the module floor.
- **M0: each sheet's lattice derived at its own footprint, over forcing the 0.5 mm lattice onto all five.** The derivation rule is the format's, and an explicit lattice would be a different fiducial scheme; the count difference is handled at measurement by registering from the shared positions.
- **M0: a footprint parameter in `tools/layout/layout.py`, over a C#-only generator.** CONTRIBUTING.md makes the layout tool the authority; the C# derivation is checked against it marker for marker, and `layouts.json` is byte-identical with the parameter at its default.
- **M0: sheets named by module, frozen by identifier once printed.** Names a person can match to a printout while the set is live, and the entry 11 rule when it becomes a measurement input.
- **M1: the cross-section as a tangent angle integrated along arc length, over a height function of page position.** Integration makes the page-to-sheet map an isometry by construction; a height function stretches the sheet wherever it slopes.
- **M1: a cubic tangent angle, three coefficients, over five.** The brief's lower bound; noise-free recovery is exact at every bend swept, and each extra coefficient is more bend for noise to fit.
- **M1: corner residuals in image pixels, over page dmm.** A detector's corner error is a pixel quantity; reclassification and selection stay in page dmm so the Phase 0 distances mean what they meant.
- **M1: six starting ruling angles, over a single start.** The ruling angle has no gradient until the sheet bends, so one start can settle on the wrong family.
- **M1: focal length refined from the brief's EXIF estimate, over fixing it at the estimate.** Starts from 0.55 to 1.80 times truth all refine to within 1.4 percent; a fixed wrong focal length leaves the pose unable to reproduce even a flat sheet's homography.
- **M1: reclassify every corner at 12.7 then 2.54 dmm, over 2.54 only.** The rendered 1.00 in bow: 96 of 136 corners and a fail, against 136 and 0.00040 in.
- **M1: an F test at p = 0.001 for keeping the bend, over always fitting it or a fixed residual threshold.** Always fitting doubled a flat sheet's worst bull; a residual threshold would depend on scale and marker count, which an F test does not.
- **M1: synthetic corner noise from `main_flat1`'s residual, 0.52 px per axis, over the 0.16 px a clean render gives.** The render's figure would flatter the model; the flat frame's includes whatever its flat model left, so it is a ceiling.
- **M1: a forward-difference Jacobian, over analytic derivatives.** Fourteen parameters, and noise-free recovery to under 0.00001 in shows it is accurate enough.
- **M1: a bull whose edge profile has under 3 or over 4096 samples fails with a reason, over clamping the sample count.** A clamped count would still sample a mapping already known to be degenerate and report a centre from it.
- **M1: one starting focal length per joint fit, the EXIF candidate with the lower alone-fit residual, over the group median or a vote.** Four of the seven 2.2 mm frames carry one 35 mm equivalent and three the other, so a median or vote decides by frame count. Measured on those frames the choice does not matter: the costs are 0.1 percent apart and the fitted results are identical from either start.
- **M1: the joint-fit lens key left as focal length and f-number, with the table frames reported rather than regrouped.** Entry 6 settled the key, and regrouping after seeing these frames degenerate would be fitting the method to them.
- **M1: the lens held on the mounted frames was fitted flat and shared on the camera's flat frames, over the median of Phase 0's per-frame lenses or a fit on the full-marker bent frames.** A flat frame has no bend for the lens to absorb, a median breaks the pairing of k1 and k2, and a bent frame is the absorption being tested.
- **M1: the leftover-shape diagnostic on two terms, curvature along the rulings and a saddle, over a saddle alone.** A twist is a saddle only in the truth's rulings: the synthetic fit turned its rulings to 72 degrees, and in turned rulings a saddle is a difference of squares, half of which the bend absorbs.
- **M1: the real-frame lens run written and started before the sweep's numbers were read, over adjusting it to them.** Brief section 3.3's rule applied a second time.
- **M1: M1.2's "at random" marker rows relabelled as the first markers in raster order, over re-running them at random.** The label was wrong and the numbers right, and they are cited in entry 13; M1.7's sweep carries a random coverage of its own.
- **M1: corner quality placed on the sweep by a robust sigma over all corners, over the RMS of the kept corners.** The kept corners are truncated at 2.54 dmm, and their RMS stays between 0.9 and 1.2 px from 1 px of noise to 5.
- **M1: joint fits keyed on physical focal length, f-number, 35 mm equivalent and image size, over the equivalent and image size alone.** Entry 16 section 2 asks for the pixel geometry; the equivalent alone would join the cropped table frames to the main camera, which shares their 23 mm and their image size but not their distortion.
- **M1: the general developable surface as folds along turning rulings 10 dmm apart, over a smooth parametrisation of a tangent developable.** Rigid strips keep the isometry exact for any turn and reduce to the cylinder, within 0.018 dmm, when the rulings do not turn.
- **M1: a general surface whose rulings cross inside the page is undefined, over letting it fold through itself.** No sheet of paper takes that shape, and a fit that could reach it would report a shape that cannot exist.
- **M1: the minimiser takes a backward difference, and holds a parameter, at an undefined boundary, over a smooth barrier penalty.** The change leaves every fit that never meets the boundary unchanged, which the cylinder's raw rows confirm; a barrier would change every general fit's cost.
- **M1: stopped at the general surface with its joint fit unconverged and its alone fits as the measurement, over constraining its turn on weakly bent frames.** Entry 16 section 5 stops at the general developable surface, and a constraint would be a further model decision.
- **M1: one frame at a time by default, over the joint fit.** A user photographs one target at a time, the lens barely matters (M1.7), and sharing a camera cost `main1` a factor of two; the joint fit stays behind `--joint`.
- **M1: fewer than eight kept corners select the plane, over the bend.** Less evidence has to mean fewer parameters; the old default preferred the model with more.
- **M1: the correlation diagnostic on per-marker mean residuals between neighbouring markers, with a permutation null, over corner pairs or a variogram.** Corners of one marker share their detection and would read as structure that is not the sheet, and a permutation test needs no model of the noise.
- **M1: the mounted gate left at 0.005 in and recorded as open, over a separate looser gate.** Entry 17 section 2: the error budget argues for 0.003 to 0.005 in, and a number chosen after seeing the results is not a gate.
- **M2: morphology and blob extraction behind the imaging backend, the arithmetic, filters and measures in Core, over the whole primitive in Core or the whole primitive in the backend.** Core keeps no OpenCV dependency, and the port can be checked against the survey step by step because the backend calls are the survey's own.
- **M2: the baseline compared through the survey's own roll-up band, 0.15 to 0.55 in, over the detector's 0.60 in.** The survey's tables were computed through that band, and two detected holes fall between the two.
- **M2: render-and-difference opens with a 0.012 in disk, over the survey's 0.032 in.** After differencing only registration slivers remain to remove, and the survey's disk erased thin rims around pale cores, which were both detectors' misses.
- **M2: each bull's cell aligned to the observed image by phase correlation, over trusting the registration or opening wider.** The rings are fiducials already printed, and a wider opening would bring back the faint-hole misses.
- **M2: rim closure recorded and not gated, over a threshold between arrowheads and holes.** The two overlapped on synthetic sheets, and the synthesis draws no C-shaped rim, the case the signature exists to tolerate.
- **M2: merged neighbours split at an elongation of 1.45 by two-means, over refusing them as too large.** Refusal lost both holes of a pair; a split reports two and flags them.
- **M2: the oversized flag against the sheet's median, over a calibre size window.** The synthetic sheet carries no calibre; the median's failure when every bull holds a pair is reported.
- **M2: the synthesis calibration stopped at four iterations with its gaps reported, over iterating until it matched.** Each iteration moved one quantity at the cost of another, and the gaps' direction says which way the recall errs.
- **M2: thresholds set on seeds 1 to 3 and the gate read on seeds 1001 to 1003, over one seed set.** A threshold read off the seeds it is gated on is fitted to them.
- **M2: truth matched nearest pairs first within 0.15 in, over an optimal one-to-one matching.** Scoring should not share the rule of the assignment it is used to evaluate.

- **M3: special functions written for the engine, over a numerics package.** Nothing may be installed, and section 15.3's 1e-12 needs control of every step: the incomplete gamma prefactor through R's deviance form, and every upper tail computed as a tail.
- **M3: CorrNormal CEP through a trapezoid rule over angle on the Hoyt CDF, over Imhof's integral or a series.** The integrand is smooth and periodic, so the rule converges geometrically and matches shotGroups' hit probabilities to 1e-15.
- **M3: the minimum-volume ellipse by Khachiyan's algorithm at shotGroups' 0.001 tolerance, over an exact solver.** Section 15.3 says to match the tolerance or expect disagreement; at the package's tolerance it agrees to 3e-13.
- **M3: range statistics from ten groups per replication with running means, over simulating each group count separately.** Every cell keeps its full 10 million independent replications at a fifth of the cost of fifty-five groups.
- **M3: quantiles from 8,192-bin histograms, over storing ten million values per cell.** One bin is 7.3e-4 of the mean against a 5e-3 tolerance, and 65,536 bins ran three times slower for no measurable gain.
- **M3: the table read between rows by R's fmm spline on shotGroups' own grid, over simulating every n or interpolating linearly.** At n = 92 the spline reproduces shotGroups, and linear interpolation is 4.8e-5 off.
- **M3: a key held back only on evidence checked key by key, over excluding by dataset or loosening a tolerance.** The point of aim is detected from the fixture's own two centres, a Monte Carlo p-value by being an integer over 9999, and a disputed CEP by not being a root of its own distribution.
- **M3: the Fligner-Killeen statistic at 1e-10 relative, over 1e-12.** It subtracts n mean^2 from a sum of squares of up to 530 normal-quantile scores, which costs about three digits; the worst difference is 2.4e-12.
- **M3: the flyer expectation as an integral, over section 10's alternating binomial sum.** At 25 shots the sum's terms reach 5.2 million with alternating signs; the integral has no cancellation.
- **M3: the pre-pooling guard as pairwise F tests with Holm's adjustment, over Bartlett's test of homogeneity.** Section 11 names section 8.1's F test, and section 8.4 names Holm for its family.
- **M3: the engine's own xoshiro256** generator for resampling, over the runtime's Random.** Section 6 requires a recorded seed to reproduce an interval exactly, and the runtime does not promise its stream across versions.

- **Entry 20: bull centres from ring fits confirmed by centre dots, over the survey's annulus matched filter.** Holes break the rings and the scan's rings carry light stripes, so neither survives as a clean blob, while the dots do; the dot centroids are kept beside the ring centres and agree to a median 0.0019 in.
- **Entry 20: grid orientation found from the bulls, over trusting pixel order or reading EXIF orientation.** The photograph's pixels are stored a quarter turn from upright; lattice phase and the sighter line's 0.2-pitch offset place the grid whatever the storage, and an unmirrored layout picks the column direction.
- **Entry 19: the hole detector run on the whole photograph and on the registered sheet, over the whole photograph only.** Untuned on the whole frame it finds nothing, which is the finding; the sheet-only run shows what registration would let the same primitive do, and is labelled as that.

- **M4: the marking screen and the correction screen as one screen over one model, over a separate manual mode.** Entry 21 section 3 and DESIGN.md section 13 describe the same interactions; one immutable model gives both undo and a test surface that needs no window.
- **M4: the image shown from the pipeline's own OpenCV decode, over Avalonia's decoder.** The two can disagree about EXIF orientation, and a mark must land on the pixels the statistics and the detector use.
- **M4: the image sized by its decoded pixel grid, over the bitmap's size.** The bitmap's size follows the file's DPI tag; the headless test showed marks scaling with it.
- **M4: the UI built in C#, over XAML.** Avalonia 12 compiles bindings by default and the screen is one window; code keeps every control's wiring in one place a reader can follow.
- **M4: a headless test through the platform's pointer input, over calling the model directly.** It caught two faults that would have reached a person, which the model tests could not.
- **Entry 24: a dispersion figure withheld below five shots, over printing it with a warning.** A warning beside a three-decimal headline is what Alan read past; the count and the centre offset stay, because they are exact at any count.
- **Entry 24: each interval labelled with its exact coverage, over removing the c4 correction from the endpoints to make them 95 percent.** Brief section 5 has GroupLab match shotGroups' intervals; stating their coverage keeps that and stops the label from lying.
- **Entry 24: extreme spread's interval in the form that covers the expected spread, over shotGroups' `getRangeStat` form.** The latter covers 84.7 percent at two shots; entry 23 section 1 says to stay exact where shotGroups is not, and the harness still checks its form.
- **Entry 26: rotation as a view property of the marking state, over rotating the decoded pixels on load.** Rotating pixels changes the frame every saved position is in; a view property moves no mark, gives undo for free and reopens as it was left.
- **Entry 26: the stored pixel frame as the canonical frame, over the upright displayed frame.** It is the frame M4.1 files were already written in, so version 1 migrates without moving a mark, and it can be checked against the file itself.
- **Entry 24 section 5: the hole-size flag at nominal plus 0.132 in, over a ratio of the calibre.** Section 3.5 found the hole deficit roughly constant in absolute terms rather than proportional.
- **Entry 25: one application-wide unit setting, over a unit dropdown beside each input.** Entry 25 section 1 asks for it, and a setting that every figure obeys cannot leave one figure behind in inches.
- **Entry 25: "mil" as the milliradian, over the 6400 NATO mil of section 12.5's table.** Turrets are marked in milliradians; offering the NATO mil under the same name is the dialling error entry 25 warns about.
- **Entry 25: lengths and distance stored in inches whatever the setting, over storing in the unit the user worked in.** A file then means the same on every machine, which the test asserts by writing one marking under both settings.
- **Entry 25: printing through the system's PDF print command, over drawing pages to a printer from the application.** Avalonia has no printing API and nothing may be installed; the PDF is the path that already works, and what it cannot control is said on the screen and on the sheet.
- **Entry 25: the actual-size note as a render option, on in the print screen and off by default, over adding it to every render.** Phase 0's pages and the gates measured on them stay item for item as they were.
- **Entry 25: `/PrintScaling /None` in every PDF GroupLab writes, over only the print screen's.** A sheet printed from the command line is measured the same way and deserves the same request.
- **Entries 22 and 27: the scrubber in C#, over running `scrub_exif.py`.** Its library is not installed and nothing may be installed; the C# version follows the script's policy, keeps digital zoom for entry 27, and also removes XMP and trailing data, which the script leaves.
- **Entry 27: triage by decoded markers, holding rather than refusing what fails it.** Markers are the check the application already has; a held file costs a person one look and an `--accept`, where a refused one would need resubmitting.
- **Entry 22: the committed-image guard names the 16 photographs with GPS, over failing the suite until history is rewritten.** The rewrite is a decision for question 13; a named list keeps the suite green without letting a seventeenth image in or letting the list go stale.
- **Entry 28: rebuilding the aimed coordinates from the shot and a recovered aim, over excluding the four Fligner-Killeen keys as a known difference.** The rebuild reproduces R's doubles and all four statistics to 5.5e-13, so the gate checks something true rather than recording a gap; regenerating the fixtures at full precision would make it unnecessary.
- **Entry 28: a stated digital zoom of 0 read as 1 in the lens key, over keeping the tag's value.** The EXIF standard defines 0 as digital zoom not used, which is the geometry of 1, and every Pixel photograph in `scans/mounted/` states it.
- **Entry 28: refusing a `meta.json` whose opt-out field is missing, over treating it as false.** A missing opt-out is unknown, and publishing on unknown consent cannot be undone.
- **Entry 33: `grouplab analyze` composing `AutomaticMarking` and `GroupAnalysis`, over a separate pipeline for the command line.** The screen and the command then run the same code, so a fault found by one is fixed in both, which is how the sighter fault reached the marking screen's fix.
- **Entry 33: a printed sheet analysed even when its definition fails today's validator, over refusing it.** Validation decides whether to print a sheet; a sheet already on paper is what it is, and refusing it would make every Phase 0 sheet unanalysable.
- **Entry 33: `--target` required, over guessing the definition from the image.** Nothing reads the printed identifier or codes yet, and the built-in definitions share marker ids.
- **Entry 34: two of the owner's photographs held, over publishing all 28.** A screenshot and a messenger download cannot show who took them. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one until Alan confirms costs nothing.
- **Entry 34: a donated submission whose files were all held published as a provenance record alone, over leaving it out.** The record shows that a consented submission arrived and why none of it is published, which is the question a contributor would ask.
- **Entry 34: a separate `publish-owner` path, over running the owner's photographs through `intake`.** Intake requires a consent record, and entry 34 section 2 rules out inventing one.
- **Entry 35: a missing camera make alone is enough to hold a file, over requiring a copy's file name as well.** Entry 35 section 1 calls the make the stronger signal. A person can still accept a held donated file by name, so a false hold costs a look, and a false publication cannot be undone.
- **Entry 35: an edited phone copy that keeps its camera metadata, the four `~2` photographs, not held.** The rule is about lost camera geometry. Those copies keep their make, model and focal length, and nothing in entry 35 asks for more.
- **Entry 37: a submission whose consent cannot be read withholds its files' hashes, over ignoring it.** The rule exists because publishing under ambiguous consent cannot be undone, and an unreadable `meta.json` is the most ambiguous consent there is.
- **Entry 37: a file held for a consent conflict cannot be accepted by name, unlike a file triage holds.** Triage is a judgement about usefulness that a person can overrule; an opt-out is the contributor's decision, and only the contributor can change it.
- **Entry 37: the two conflicted submissions not published even as provenance records.** Publishing a record of a submission whose consent is in question, with its answers and credit name, waits for the contributor's answer as the photographs do.
- **Entry 36: `DFdistr` left at its committed version, over committing a JSON whose numbers are strings or editing `sg_distr.R` here.** Nothing reads it, `tools/` is the authority and planning's to change, and a fixture committed in a broken shape would be read as correct later.
- **Entries 39 and 40: any unassigned shot on a sheet of several scoring bulls withholds every figure, over quoting the assigned shots alone.** Leaving shots out without saying so would be a different wrong number, and the sentence in their place tells the user exactly what to do.
- **Entry 40: a tap on printed ink placed where it was tapped, with a note, over snapping onto the ink and naming it.** The centre of a printed stroke is never the answer, and a tap placed where the user put it is at worst as wrong as the user.
- **Entry 39: a moved shot follows its nearest bull unless the user assigned it elsewhere, over keeping whatever bull it had.** A shot dragged across to the next bull is almost always a correction of position, and a deliberate reassignment is recognisable because it differs from the nearest.
- **Entry 39: an impact placed by press, drag and release, over click, drag, click.** It is one gesture with a finger or a pointer, and a plain tap still places a shot.
- **Entry 42: a text colour below 4.5:1 moved along its own hue, over changing which role the text takes.** Section 2 says so. The contrast is measured on the three surfaces text sits on, not on the sunk image area, which carries marks. Counting the image area too would have pushed light `faint` almost onto `dim`, erasing the difference between the two.
- **Entry 42: styles rebuilt when the theme changes, over binding each control to a theme resource.** The shell is built in code, and rebuilding one style set keeps every colour decision in `AppStyles` and `Tokens` rather than spread across every control.
- **Entry 42: a unit left at its figure's size for now, over splitting the figure text.** The existing tests read that text, and section 1 requires them to pass unchanged.
- **Entry 42: mark labels in light text on a dark plate, over text in the mark's colour.** The mark's colour as text could not be read on the first screenshots, and the colour survives as a bar beside the number.
- **Entry 41: image facts from the metadata GroupLab already reads, over reading the further facts section 2 permits.** Bit depth, lens model, ISO, exposure and a count of EXIF tags would each mean reading more of a photograph's metadata for the sake of a log, which is the step the rule exists to stop.
- **Entry 41: the directory named in symbols on the first log line, over the resolved path.** Section 3 asks that the first line report where the log is, and section 3 also forbids a path. `%LOCALAPPDATA%\GroupLab\logs` satisfies both.
- **Entry 45: `last_action` as the name of the last event logged, over a separate list of action names.** Every user action already writes a stable event name, such as `print.select`, and a second list would drift from it.
- **Entry 41: Send saves the package before sending, into the log directory when the user has not saved it.** Section 7 says the zip is kept if sending fails, and the only way to guarantee that is for it to exist before the attempt.
- **Entry 41: a crash record not rewritten when a second crash lands in the same second of the same process.** The receiver's name pattern leaves no room for a counter, and the first crash is usually the cause.
- **Entry 46: an alert ring at the measured size on a flagged hole, over every impact ring at its measured size.** The size check reads an extent only for a dark region on paper, so a measured ring for every shot would silently fall back to the calibre on ink and on a dark backer. Every ring stays comparable, and the one that disagrees shows by how much.
- **Entry 35 section 6 item 3: the definition from the sheet's QR codes, over choosing among definitions by registering against each.** A frame that passes its CRC names one definition. A registration that fits well against the wrong definition is possible, because the built-in definitions share marker ids.
- **Entry 35 section 6 item 3: refuse and ask when the codes do not settle it, over falling back to the nearest match.** Eight of the 37 Phase 0 images are not identified and fall back to naming the definition. A wrong definition would produce a plausible wrong group.
- **Entry 35 section 6 item 3: the frozen Phase 0 definitions shipped beside the application, over the live library only.** The sheets already printed carry the frozen identifiers, and the print screen's list does not show them.
- **Entry 37 section 5: no size recorded for a note with no unit, over assuming inches.** Every size in the first notes had its unit, and a wrong assumption is a scale error of 25.4 or 2.54 times, which the marking screen could not detect.
- **Entry 37 section 5: the stated size offered in the rectangle prompt, over setting the scale from it.** The size is the sheet's, and only a person can say which corners are the sheet's and which side was tapped first.
- **Entry 37 section 4: the scrubber's keep list left as entry 29 set it, with `LensModel` reported rather than added.** Adding a field to what is published is a publication decision, and the lens grouping does not need it.
- **Entry 46 section 3: the count line replaces the "Placed:" line, over keeping both.** They carried the same three numbers, and the one planning asked for sits above the figures where it is read first.
- **Entry 35 section 6 item 2: the gate record workflow left failing on Linux and macOS, over passing within a tolerance.** Entry 32 section 3 asks for byte identity or an explained difference, and choosing a tolerance that makes the difference pass would be choosing the gate.
- **Entry 47: double resolution tried second, over a capability fallback.** The WeChat module is present in the runtime packages for all three platforms. The failures were detection on code modules under five pixels, which only a larger working image can help.
- **Entry 47: resolutions past 8000 px skipped, over trying every one.** Doubling a 600 DPI scan cost 52.7 s and gave nothing the scan did not, and the bound lost no image on the sweep.
- **Entry 47: identification counted per platform in the gate record workflow, over a per-platform expectation in the unit tests.** No count is known yet for Linux or macOS, and an expectation written before measuring would be a guess.
- **Entry 48: Linux's gate record difference explained and left red in the workflow, over a Linux reference record or a rule that passes it.** Making the job green needs either Linux's own records committed as its reference, or a rule about which differences pass. The first adds about 12 MB and the second is a gate written after the results, so the choice is planning's.
- **Entry 48: the owner corpus republished from the originals through `publish-owner`, over editing the published files.** Every published file still comes from the one scrubber, and the 13 files without a lens model reproduce byte for byte, which shows nothing else changed.
- **Entry 49 section 1: the printed tables committed as Windows text files, over comparing the platforms with one another inside one run.** A committed reference fails a single platform's job on its own, and it states in the repository what the record is.
- **Entry 49 section 4: the README's platform guard requires equality with the CI matrix, over naming at least as many.** A README naming a platform CI does not build would be false in the other direction.
- **Entry 49 section 2: the marker sort held for question 15, over committing it with regenerated records.** It changes committed evidence and a benchmark the Phase 1 surface work is measured against, which entry 49 did not foresee, and the question file exists for a measurement that contradicts what was written down.
- **Entry 52: every record regenerated twice under identical code, without the sort and with it, over comparing the sorted run with the committed records.** Five records were already stale, and comparing against them would have credited their changes to the sort.
- **Entry 52: tables from fits no command reproduces left as measured and marked, over updating the columns that can be regenerated.** Half a row regenerated disagrees with its other half and with the conclusions written beneath the table.
- **Entry 52 section 3: reordering applied at the homography fit, over shuffling the detector's output.** The sort puts any shuffled detection back in order, so only a shuffle after it measures the registration's sensitivity to order.
- **Entry 52 section 3: the edge fit's leave-one-out run from the converged pass's start, over rerunning the whole locator per point.** It isolates one point's weight in the fit; rerunning the locator would also move the rays and confound the two.
- **Entry 49 section 2: the journal replayed by call order, with the image hash reported beside it, over looking each detection up by its image.** A platform whose raster differs would match nothing and replay nothing, and the rerun exists to hand it Windows' corners anyway.
- **Entry 49 section 2: only the two measurements whose tables differ replayed, over all eight.** The other six already print identically on macOS, so replaying them could only repeat what the gate record shows.
- **Entry 58 section 4: the opt-out's second key is what a file scrubs to, over the decoded pixels.** It collapses the four re-exports to one value as a pixel hash would, and it keeps a consent mechanism inside `GroupLab.Core`, which has no image decoder and whose tests run on every platform.
- **Entry 58 section 3: either key alone withholds, over requiring both.** The same reasoning as entry 37 section 1's two opt-out signals: redundancy is the point, and publishing under ambiguous consent cannot be undone.
- **Entry 55 section 3 item 1: the counts recorded in the stage record and in `grouplab measure --json`, over the committed spike records.** A field in the spike records would regenerate thirteen of them and move the gate record's raw comparison, for a diagnostic that changes no figure anybody reads.
- **Entry 55 section 3 item 1: a ray counted as marginal within a tenth of the crossing threshold on either side, over counting only the rays that failed it.** A ray that just cleared the threshold is as easily flipped by the image as one that just missed it, and the tenth is the convention the leave-one-out already uses for the rejection limit.
- **Entry 61 section 3: no catch added for `PlatformNotSupportedException`, over adding one defensively.** The runtime throws `Win32Exception` for a verb off Windows, so a catch for the other would assert a behaviour that does not exist and would outlive anybody who remembers why it is there.
- **Entry 64: the working tree left alone, over running the fix as written.** Nothing differed, so the command would have been a no-op dressed as a repair, and running it would have left a false record that something was cleaned.
- **Entry 61 section 5 item 2: the tarball built on `ubuntu-latest`, over pinning `ubuntu-24.04`.** Pinning would freeze the glibc floor deliberately and freeze it silently apart from the test matrix, which tracks `ubuntu-latest`; printing the release it built on keeps the day they diverge visible, which is what entry 63 section 2 asks for.
- **Entry 61 section 5 item 2: the step fails when the native imaging library is missing, over shipping whatever publish produced.** A tarball without `libOpenCvSharpExtern.so` installs, launches, and then cannot detect a marker, which is a failure that arrives late and in front of a user rather than in CI.
- **Entries 65 to 69: the concept image left uncommitted, over committing a second copy.** The committed `docs/figures/screens/assignment-editor.png` is the same picture pixel for pixel, and the copy would add 481 KB and a content-credential block to the history for nothing.
- **Entry 65 section 4 step 2: wording that names no platform, over "on Linux".** The branch it describes runs on macOS as well.
- **Entry 70 section 3: a moved shot recorded as the difference from the bull detection gave it, over a list of notices appended at each edit.** The difference is derived from state, so undo, redo and a shot moved back all keep it true without bookkeeping, and it stays visible for as long as it stands rather than until the next edit.
- **Entry 70 section 3: the counts rule falls back to the nearest free bull, over the nearest of all bulls.** A bull a person has decided is not available to the matching in either mode, so the two methods differ only in whether the matching is forced.
- **Entry 70 section 6: three status states, over styling only the failures.** Success needs its own colour for the same reason alert does: a line that reads the same whether it worked or not teaches people to stop reading it.
- **Entry 74: pinning read from `BullChosen` alone, over `Corrected` or a chosen bull.** A shot becomes corrected when it is moved or marked not a shot, and neither of those chooses a bull.
- **Entry 73 section 1: a shot's pool is its nearest bull's, with the margin still measured to every bull, over a pool chosen by page region.** The sheet already says which bulls are sighters, and nearest-bull is the rule's own fallback; a region would be a second geometry to keep in step with every definition.
- **Entry 73 section 1: the overall method reported as nearest-bull when either pool fell back, over reporting the scoring pool's.** Reporting one-to-one would hide that a pool stopped being matched, which is the thing section 13 says must be said; the reason names each pool.
- **Entry 73 section 5: no mark size invented when no calibre is set, over drawing a nominal diameter.** A ring at a made-up diameter reads as a claim about the bullet, which is the kind of implied fact section 2 rules out.
- **Entry 73 section 7: the interval labels left at their exact coverage, over matching them.** Entry 24 decided the label states the coverage the interval actually has.
- **Entry 71: intake run on a copy without the unlisted scan, over adding the scan to the manifest.** Whether the consent covers the scan is the contributor's question, and the submission as received is left as it is.
- **Entry 71: the worst bull reported beside the worst clean bull, over the worst bull alone.** On a shot sheet the holes cut the rings of the bulls they hit, and separating the two shows the error is registration.
- **Entry 76 section 4: the printed name reverted, over landing it with a box that covers both captions.** On a sheet already printed the wider box hid a real hole, and the analyser cannot tell which caption a sheet carries; where the name goes is question 16.
- **Entry 76 section 4: detection cancelled at checkpoints between stages, over interrupting a stage.** A stage stopped halfway leaves nothing a person can use, and the checkpoints are where the work can be dropped cleanly.
- **Entry 76 section 4: a moved shot drops its measured diameter, over keeping it.** The measurement described the point the detector chose, and a ring at that size around a point a person chose would claim a measurement nobody made.
- **Entry 76 section 1: the aspect's null integrated exactly, over a simulated table.** The density has a closed form for every n, so there is no table to extend or to seed.
- **Entry 76 section 2: the two split detections merged at their midpoints for the re-run, over the pipeline's figures or hand-picked positions.** The pipeline's figures count one hole twice, and the midpoint uses only what the pipeline found, which is the question entry 76 asked.
- **Entry 75: a shot with no bull named "unassigned, at x, y" in the list, over "unassigned" alone.** Two such shots would otherwise read the same, and the position is a fact the screen has, not an order.
- **Entry 77 section 3 item 1: a blob counted as swallowed only when it passed every shape filter, over every blob centred in a zone.** Printed matter leaves residue that the size and shape filters refuse anyway, and counting it would bury the one number that means a hole may have been lost.
- **Entry 77 section 3 item 2: the check made standing by an artwork fingerprint test, over running the corpus inside the test suite.** The corpus takes minutes and its counts differ by platform. The fingerprint is fast and exact on every platform, and it forces the comparison to be run where it can be.
- **Entry 77 section 3 item 2: the committed corpus punched with synthetic holes on three offset grids, over committing a real shot sheet.** No shot sheet has consent to be committed, and the Phase 0 scans are real print made before every change since.
- **Entry 77 section 4: oversize measured three ways, over the detector's diameter alone.** Entry 73's warnings came from the screen's size check, which is a different measurement, and the test had to see the one that was reported.
- **Entry 77 section 5: a sheet with no clear place prints no name, over shrinking the name further or trying another margin.** Section 5 states the rule. One place with one clearance is also what the placement test can check on every sheet.
- **Entry 77 section 5: the identifier caption left at the bottom beside the new name line, over moving it.** It is C6's recovery path, and its zone is what every sheet already printed is read with.
- **Entry 78 section 4: the calibre only vetoes a split, over also splitting a round blob of two holes' area.** Splitting on size alone would cut a genuinely odd hole to fit the expectation, which section 4's second guard rules out; the blob is flagged instead.
- **Entry 79 section 1: a measured ratio per image kind, over the calibre or one pooled ratio.** Scans and photographs measure holes differently, 0.944 against 0.986, and section 1 asked for them apart.
- **Entry 79 section 1: a photograph without camera data taken as a scan, over refusing the ratio.** There is nothing in such a file to tell the two apart, and the one such photograph measures 0.924, within the scan ratio's spread.
- **Entry 78 section 4: a calibre named after an uncorrected detection detects again, over asking.** Nothing a person did is lost, and a result found without the size is the one section 3 says is wrong.
- **Entry 80 section 2: the old sweep stopped unfinished, over letting it run.** Its synthetic holes were the wrong size, and its real rows used the bullet diameter, so its result could only have been discarded.
- **Entry 80 section 2: the synthetic holes scaled to read like .308 on a scan, over reweighting the sweep.** A scale fixes what the holes are; a weight would only change how much a wrong population counts.
- **Entry 80 section 2: no split threshold adopted, over adopting the survivor.** The only setting the real holes allow also changes the default without a calibre and moves committed synthetic records, and no real merged pair has been measured to say what it costs.
- **Entry 78 section 2: the residue fix proposed and not built, over a plain elongation cap.** A cap alone would refuse two real holes joined by the closing, a silent loss, and the safe form depends on the threshold still open.
- **Entry 81 section 2: the oversize flag rebuilt around a single-hole size, over keeping the median rule and lowering the calibre flag alone.** The median rule flagged ordinary holes on a tight sheet and let pairs through on a sheet full of them, so a calibre-only fix would have left the loud failure silent whenever no calibre is named.
- **Entry 81 section 2: the single hole without a calibre is the sheet's 25th percentile mark, over its median.** On the composite sheets half the marks were merged pairs, and the median moved to a pair's size and flagged none of them.
- **Entry 81 section 3: the residue fix on size and elongation together, over solidity.** Solidity overlaps across all three populations, real single holes reaching 0.59, while size splits the elongated ones cleanly.
- **Entry 78 section 2: a small elongated blob kept as one hole below 2.2, over refusing every blob the size vetoes.** Refusing a real hole is a silent loss, and the margin between the most elongated real hole, 1.72, and the split threshold, 1.80, is too thin to refuse on.
- **Entry 82 section 2: the floor used to veto and never to flag, over clamping the quarter-point alone.** On the clean photographs there were no round marks to clamp, and a floor that flagged would flag every real hole, since each is larger than the smallest a bullet makes.
- **Entry 82 section 2: the floor at 0.16 in, over 0.17.** 0.17 is the bullet; a .17 hole measures about 0.944 of it on a scan, and the floor must sit below any real hole.
- **Entry 82 section 3: two sizes need a gap of five pooled deviations, over three.** An even spread of sizes cut in half is 3.3 apart, so three would ask for a calibre on any wide one-calibre sheet.
- **Entry 82 section 3: no flags at all on two sizes, over tentative flags on the larger group.** Entry 82 asks for one sentence rather than many flags, and a sheet of two calibres would have every larger hole flagged as a merge.
- **Entry 82 section 6: the detector's flag drawn beside the size check's, over merging the two.** They measure different things, the detector's residual against the size check's dark region, and each says what it measured.
- **Entry 83 section 2: sizes converted at each blob's own scale, over leaving the detector alone as entry 83 section 4 asked.** Section 2's test found a wrong unit conversion rather than a tuning question. A size read 30 percent wrong on any oblique photograph is a defect, and the fix is a few lines.
- **Entry 83 section 2: the clean photographs' rise from 24 to 35 accepted, over restoring the single scale for the veto.** The single scale was wrong for real holes and for residue alike, and the residue it hid is the kind section 3 says cannot be resolved without a calibre.
- **Entry 83 section 4: the review queue computed from the marking, over storing it.** Every edit changes what needs review, and a stored queue would go stale; only a person's "keep it" is stored, because nothing else can know it.
- **Entry 83 section 4: the editor built into the marking screen, over a separate mode with Accept and analyse.** The statistics are already live on every edit, so an accept step would commit nothing. Discard edits is kept, as one undoable step.
- **Entry 83 section 4: the review keys taken on the tunnel route, over the window's key handler.** A focused button would otherwise take Space and Enter, and pressing Space would press the last choice again.
- **Entry 87 section 1: the screen's calibre size check removed, over keeping it and feeding it into the queue.** It measures the dark region connected to a mark, so a printed ring is part of every mark that touches one, and it read five confirmed single holes at about twice their size. Adding those five to the queue would have made the queue wrong rather than the panel.
- **Entry 87 section 1: `HoleSize` kept as a measurement.** The apparent extent is what the harnesses read and what the snap radius needs, and keeping it without a threshold is what makes the removal a removal of a judgement rather than of a number.
- **Entry 86 section 3: the enclosed-ink fraction recorded on every detection although nothing reads it.** It is the quantity that separates a hole on a ring from one beside it, it cost nothing to carry, and it is what proved the hypothesis wrong rather than plausible.
- **Entry 87 section 2: the README's states tied to DESIGN.md by a test, over a review habit.** Entry 60 found the README stale for days, and the two documents can now only disagree by failing a test.
- **Entry 90: the parametric editor and the visual designer deferred, over giving them a phase.** Alan is getting a specification from Jeff, and a phase for a screen nobody has specified would be a date attached to a guess. The deferral is explicit, carries its reason, and is checked by a test, which is the difference between parking something and losing it.
- **Entry 90: assisted hole placement raised as a question, over scheduling it or dropping it.** The detector differences against a definition and a store-bought target has none, so the bullet is not schedulable as written; but two of the three meanings of "assisted" are already built, so it is not droppable either. That is a design answer and not a wording fix.
- **Entry 90: the scope test requires a citation on a deferral.** A bullet could otherwise satisfy the test with the word alone, which is exactly the quiet drop the test exists to catch.
- **Entry 88: the oversize flag left alone although the measurement points at a defect in it.** Four of five flags on real material hold one hole's worth of ink inside a ragged hull, so flagging on ink area would remove them; the threshold is one the Phase 1 gate is measured against, and entry 83 section 3 says stop tuning the detector. Recorded with its numbers instead.
- **Entry 84: `PhotographHoleToCalibre` changed to the re-measured 0.948 and `ScanHoleToCalibre` left at 0.944.** The photograph figure was measured with the single-scale defect in place; the scan figure was not, and its 0.006 move comes from a longer verified hole list rather than from the fix.
- **Entry 84: the 25-shot rehearsal recorded as a rehearsal.** It measures the software's share of the two minutes and nothing about a person, so recording it as the Phase 3 gate would put a state of "done" on a page where the thing being gated has not happened.
- **Entry 94 section 1: the oversize threshold left at 1.35 while the quantity under it changed.** Area reads about six percent lower than the hull-derived reference it is compared against, so the threshold is already slightly stricter; and the value that would catch every 0.10 in pair would also flag S1b again, which is the false flag the change exists to remove.
- **Entry 94 section 1: the hull area kept beside the mark's area rather than dropped.** Solidity is the ratio of the two, and entry 88's shape measurements rest on it.
- **Entry 94 section 4: the split's two centres computed at detection time and carried on the flag, over computing them when the choice is taken.** The review queue has no image, and a choice that needs the pixels is a choice that needs a mouse.
- **Entry 94 section 2: subgroups keyed by bull rather than by shot.** A shot moves between bulls during review and its load does not; the sheet's layout is what holds the loads.
- **Entries 91 and 92: the zero correction refuses rather than rounds.** Where the offset is inside the sampling error the panel gives a shot count instead of a number, because a bare figure will be dialled.
- **Entry 93 section 3: high contrast derived from the dark tokens in code, over a fourth hand-drawn palette.** A palette that cannot be derived is evidence the roles carry values rather than meanings, and deriving it is what proves the concept is a design language rather than one screenshot.
- **Entry 95 section 2: the count item acts on its first candidate only.** Naming three and acting on one keeps it a key press; a person who disagrees with the ranking reaches the right mark through its own item or by selecting it.
- **Entry 95 section 2: every mark gets a size in holes, measured against the veto's size where no flag size exists.** The ranking needs a size on unflagged marks, which are most of the candidates, and only the flag's own size may raise the flag.
- **Entry 95 section 3: nothing changed for the mounted gate.** The investigation found where the error lives and what would separate the two explanations left; a change made before that photograph would be tuning against the frames being gated.
- **Entry 96 section 2: the warp that passes `IMG_5819` not adopted.** It was found on the frames being gated, it makes `IMG_5820` worse, and a model chosen because it passes the frames that prompted it is not a gate result.
- **Entry 97 section 1: a shot a person placed is neutral, not teal.** Teal means the software found it on its own, and a mark a person put down or moved is not that.
- **Entry 97 section 2: the marking keeps a copy of the rifle rather than its name.** A correction read from a saved marking must be the one that was right when it was shot, whatever the record book says since.
- **Entry 97 section 2: a barrel's count grows only on a person's step.** Counting automatically on detection would count a reopened sheet twice.
- **Entry 97 section 3: the per-stage rasters left for the next batch.** The records land live and cost nothing; the rasters would need keeping images the detector currently throws away, which section 19 allows for one interactive analysis and which deserves its own measurement of cost.
- **Entry 97 section 5: the window rehearsal pinned at 10 presses.** It is its own baseline at 300 DPI, and a ceiling the next batch must not raise.
- **Entry 98 section 3: a control added that the entry did not ask for.** The holed half and the clean half are different places on the page, and without the same split on unshot sheets a positional difference would have read as hole damage.
- **Entry 98 section 2: the nominal hole is .30 when nothing better is known.** It is the middle of what the corpus carries, and the sheet's own measured holes replace it as soon as the detector has run.
- **Entry 99: the editor ports the library's solver rather than calling it.** `tools/` is planning's and Python is not shipped; the port is held to the original by rebuilding ten sheets exactly.
- **Entry 99: the fewest markers allowed is 9, the fewest any built-in sheet carries.** It is the one figure the project has shown registration holding at, on GL-LR300-T's tile, and choosing a lower one would be a guess.
- **Entry 98 section 5: only the residual is kept.** The markers, corners and rejections were already in the result, so the interactive run's extra cost is one image.
- **Entry 101: the homography's final fit carried to convergence, over stopping at OpenCV's ten iterations.** A fixed iteration count reproduces the native figures only as far as every other detail of its solver does, and convergence is a definition every platform reaches the same way.
- **Entry 101: native code kept for the integer steps.** Candidate detection, decoding and RANSAC's inlier choice were identical on every platform in every gate record run; porting them would add risk to steps that already agree.
- **Entry 101: the contour lines fitted in double precision, over emulating OpenCV's single precision.** The emulation reproduces native and proves the contours are the same, but its answer is up to 0.09 px from the least-squares line it sets out to compute.
- **Entry 101 section 5: the picture carried on the stage record, over a separate artefact channel.** The record already reached the timeline live, so attaching the picture to it makes the two arrive together, and a trace put on the timeline after its run draws its pictures by the same path.
- **Entry 103 section 1: extreme spread drawn as the line between its two shots, over the concept's circle.** A circle that size reads as a region containing the shots; extreme spread is a distance between two of them.
- **Entry 103 section 1: Show work opens the timeline in the editor, over a second timeline in the analysis state.** The timeline already shows the work, and one copy cannot disagree with itself.
- **Entry 103 section 2: the flyer card says "further out than a group this size usually puts its worst" below one time in twenty, over always saying "not a flyer".** The old line said not a flyer whatever the distance; the card still leaves the call to the shooter.
- **Entry 103 section 3: held as a question, over running the sweep.** The sort was already committed by entry 52, and regenerating would have reproduced entry 101's records.
- **Entry 104 section 2: the worst shot calibrated by simulation at every count, over withholding the verdict below some count.** The calibration is valid from the dispersion minimum up and costs milliseconds, so there was no count where withholding was the more honest answer.
- **Entry 104 section 4: only the inked discs faded, over fading the whole bull.** Fading the paper as well turned it grey on dark chrome, and the paper-on-dark contrast is most of the concept's character.
- **Entry 105 section 6: the work bar defaulted closed, over open.** Alan asked for the strip off the screen; a failure stays a prominent error without it, and Show work turns red and says so when a stage fails.
- **Entry 105 section 7: a name read by its leading number only after the table, over the table alone.** "6.5 Creedmoor" and "30-06" are what shooters type, and their leading number is the table's name; a wildcat still falls through to the old rule and says what it read.
- **Entry 105 section 4: the mark drawn in the files' own colours until question 20 is answered, over recolouring it to the nearest tokens.** Recolouring would change a mark Alan chose to a set of colours nobody chose.
- **Entry 105 section 5: the icons drawn by a command from the committed mark, over a script outside the build.** The CLI already carries the imaging library, and nothing new is installed.
- **Entry 106 section 1: the viewer path on every platform with a confirmation dialog, over keeping the print verb anywhere.** The verb printed silently at the viewer's own scaling on Alan's machine, and GroupLab cannot see what any registered print command does.
- **Entry 106 section 4: the PDF drawn by the renderer's own writer, over printing the Markdown through a browser.** The other PDFs came from Chromium by hand; a command in the repository keeps the list and its PDF in step with the code, and installs nothing.
- **Entry 106 section 5: raised as question 21, over building it now.** It is about a run of its own, three decisions are open, and its test needs a PDF printer the CI runner may not have.
- **Entry 107 section 1: "9mm" read as a diameter, over refusing it.** The section's rule reads any number marked mm, and its test list refuses "9mm"; the rule is built and the conflict is question 22.
- **Entry 107 section 2: the quiet zone not counted in the margin refusal, over refusing on it.** The margin leaves bare paper and the quiet zone is bare paper, so it prints as intended, and a smudge near the edge lands on ink, which is refused on its own account.
- **Entry 107 section 2: `WindowsPrinter` in the CLI project, over the application.** The Core tests reach it there to print and measure a real job, and they already reference that project for the imaging backend.
- **Entry 107 section 2: markers located on the printed page with no fitting, over registering through a homography.** A homography absorbs scale and offset, which are the errors the test exists to catch.
- **Entry 108 section 2: designations compared as decimals at the value typed, over matching the text.** ".270" and ".27" are one value, and matching text would let one form through that the other refuses.
- **Entry 108 section 2: the refused value shown as typed with its unit, over normalising it.** The person sees their own entry named, which is what the refusal is about.
- **Entry 109 section 1: "why" as a compact disclosure on each item, remembered per item, over one panel of explanations.** An explanation read beside the figure it explains needs no hunting, and closed it is one short line.
- **Entry 109 section 1: the older size names mapped onto the five, over replacing every use.** Every existing use lands on the scale at once, and the test catches any new size.
- **Entry 109 section 2: settings as a screen in the main window, over a dialog.** The rail is navigation, and the gear is a destination like Print.
- **Entry 109 section 3: the flyer card's hedge kept in view, over moving it behind "why".** Without "by that measure alone" the verdict would say more than the test can.
- **Entry 109 section 3: excluded rows struck through, over a word in the row.** One number per shot leaves no room for a word, and the tooltip says it.
- **Entry 110 section 2f: the tolerances committed and pushed before the reference tables were generated, over writing them in the same commit.** The history then shows the order, which is the point of stating them first.
- **Entry 110 section 2f: both references generated on a GitHub runner, over installing Node and py-ballisticcalc here.** Nothing is installed on the development machine, and the same runner re-checks the JavaScript on every push.
- **Entry 110 section 2f: the G1 cases held as named known failures, over a red check or a wider tolerance.** A red check would block every other change, and a wider tolerance would make the gate mean nothing; the named list fails the day it is no longer true.
- **Entry 110 section 2a: the Coriolis vertical term held out rather than ported with its sign corrected.** The entry said to port it as it stands, so the conflict is a question, not a quiet fix.
- **Entry 110 section 2c: aerodynamic jump left out, over Litz's fit from memory.** The entry required the coefficients from the publication, which was not to hand.
- **Entry 110 section 2b: the rifle zeroed on the flat and then tilted, over zeroing at the shooting angle.** A rifle is zeroed at a range and then carried to the hill.
- **Entry 111 section 1: poncelet as the second transcription, over the gehtsoft ports.** Those are py-ballisticcalc's own ancestry, so agreeing with them would prove nothing; poncelet reached JBM's McCoy tables through JBM's files.
- **Entry 111 section 1: the JavaScript's tables kept only for the compatible mode, over deleting them.** The transcription check compares the port with the file as it is, and that needs the file's tables.
- **Entry 111 section 3: "why" as a plain button beside the item's last line, over a toggle.** The theme paints a checked toggle amber, and an open explanation needs no one's attention.
- **Entry 111 section 3: the count kept in the placement sentence, over the "Shots" row.** The sentence carries the count and how the shots were placed; the row carried the count alone.
- **Entry 111 section 4: the timing read from the log, over a stopwatch in the window.** The log already records every step with its time and no path, so the measurement needed no change to the application.
- **Entry 112 section 1: the sheet's registration kept in the marking file, over re-detecting on reopen.** Section 18 says no image is ever needed to reopen, and without the mapping a session reopened from its marking had no scale.
- **Entry 112 section 1: the proof image as a JPEG in the database, over a file beside it.** One file is the whole record, and the export carries it.
- **Entry 112 section 2: the report's words from the screen's own functions, over a second wording.** Two wordings drift, and the rule is that paper says nothing the screen does not.
- **Entry 112 section 3: deleting a sheet a session used allowed, over refusing.** Every session keeps its own copy of the definition, and refusing would make a sheet undeletable while any session of it is kept.
- **Entry 112 section 4: elevation carried by a central difference of the zero range, over a new solver input.** It needed no change to the validated solver, and a test holds it to a directly flown change within 1 percent.
- **Entry 112 section 4: the dope table on a Ballistics screen with its own rail slot, over a panel in the analysis.** The analysis column is 372 pixels, and the table has seven columns; question 26 asks.
- **Entry 113 section 2: the chart slot as Compare loads, over Reports.** The concept's chart icon is Compare loads, and a report is written from its analysis.
- **Entry 113 section 2: sessions at different distances compared as angles, over refusing.** Dispersion scales with distance, and the screen says it compared them as angles.
- **Entry 113 section 3: an optional fixed launch angle on the solver's input, over a zero-range difference for velocity.** A change of velocity with the zero kept would move the bore; the angle must be held, and without the field nothing changes.
- **Entry 113 section 4: the rule re-baselines the detected reading, over counting its moves as edits.** A rule is how the sheet is read, and flagging every shot it placed as moved would raise the doubles again.
- **Entry 113 section 6: JPEG pictures in GroupLab's own PDF writer, over another PDF tool.** The project's reading documents already come from that writer, and a JPEG goes in as it is.
- **Entry 113 section 7: the new screens held to their themes' tested text roles, over pixel contrast measurement.** The roles are what ThemeTests holds to their ratios, and a render's pixels vary between machines.
- **Entry 114 section 1: every filled shape drawn as a closed path, over keeping `FillRect` with a capability check.** Both drivers report the same RASTERCAPS, so a capability check cannot tell them apart; a path is honoured by every driver.
- **Entry 114 section 1: the page aborted when a drawing call fails, over printing what did draw.** A sheet with a marker missing looks normal and cannot be measured, and the fault is found only when it comes back from the range.
- **Entry 114 section 1: the in-app Print button demoted, over disabling it.** Hiding it would leave a person who wants it with no path and no reason; the screen now says what was wrong and what is safer today.
- **Entry 114 section 1: the drivers the tests print through are a fixed list, over enumerating the machine's printers.** A driver can open an application when it is printed to, as the OneNote one does.
- **Entry 115 section 2: bulls chosen with shift and click, over a plain click.** Every bull on a shot sheet has a hole on it, and a plain click there is the hole, which is what the editor has always done with it.
- **Entry 115 section 3: the velocity SD written to the load record with its provenance, over showing it on screen alone.** A figure that looks typed and a figure that was measured are different things, and the record is where the solver reads it.
- **Entry 115 section 3: the schema upgraded in place on open, over refusing an older database.** A person's sessions are not something to make them re-create, and the upgrade is one statement in one transaction.
- **Entry 115 section 4: a sheet read as the wrong definition warned about, over refused.** A damaged or marked-up sheet looks the same to the evidence, and the person can see the sheet.
- **Entry 115 section 4: the low-resolution threshold measured, over assumed.** The detector reads a sheet at 96 dpi and fails at 60, so the message is keyed at 120 rather than at the 150 that seemed obvious.
- **Entry 115 section 6: the corrected JavaScript generated from the original by a script, over edited by hand.** Every change is then exactly the five, and the file can be regenerated when the original changes.
- **Entry 116 section 1: a folder publish, over a single file.** The native OpenCV library then sits beside the executable where the loader expects it, nothing unpacks itself into a temporary folder on first run, and an antivirus sees an ordinary folder rather than the self-extracting shape it distrusts.
- **Entry 116 section 2: the shot sample generated by GroupLab, over shipping no shot sheet.** The repository holds no shot sheet that may be published, and a tester who cannot see the figures in the first minute has not seen GroupLab. It is question 29 all the same.
- **Entry 116 section 2a: the packaging script warns about a missing Inno Setup locally and refuses in a release.** A working copy should still build a zip on a machine with no installer tooling; a release that quietly shipped one asset of two would be worse than a failed release.
- **Entry 116 section 3: every asset attached twice, versioned and stable.** A bug report then names a build, and the README's front-page links keep working with nobody editing the README after a release.
- **Entry 117: the benchmark generates its own material, over reading anything a person has.** It then runs on a bare checkout, on CI and on a machine that has never analysed a target, and no case can quietly depend on one machine's folder.
- **Entry 117: the stage timings come from the stage record, over a second timing path.** Two clocks disagree eventually, and the one the application already files is the one a person sees in Show work.
- **Entry 117 section 3a: coverage checked by reflection over what does work, over a list of cases.** A hand list rots the moment a feature is added; this one named 25 gaps the first time it ran.
- **Entry 117 section 3b: the interface benchmark lives in the test project, over the command line.** Timing a control needs a windowing platform, and shipping a headless one inside the application to measure it would be a cost carried by every person to serve a benchmark.
- **Entry 117 section 3b: a dropdown measured by choosing from it, over opening its popup.** Choosing is the work; opening a popup headlessly crashes in the toolkit, and it would have been measuring the platform rather than GroupLab.
- **Entry 118: the anchors derived in the test from the heading text, over read from the list.** A renamed heading then fails the test rather than leaving a link that scrolls nowhere, which is the failure a reader cannot see.
- **Entry 118: the contents list after the Download section, over at the top.** Somebody who came to get the program should not have to read past a contents list to find it.
- **Entry 119 section 1: the updater ranks the trains, over SemVer's own text ordering.** SemVer puts beta below nightly because "b" sorts before "n", and section 4.5 needs the opposite. Question 30.
- **Entry 119 section 3: ECDSA P-256, over the Ed25519 the entry asks for.** .NET 10 has no Ed25519 and this machine has no NuGet source, so no package can be restored to provide one. The algorithm is named in every manifest, so a later change is one older builds refuse by name. Question 31.
- **Entry 119 section 3: the workflow fails loudly with no key, over publishing unsigned.** An unsigned manifest is one the application would have to trust without being able to check it.
- **Entry 120 section 4: the failing result carries its definition, over the screen falling back to the pipeline's words.** The advice written for a sheet that will not register was unreachable in exactly the case it was written for.
- **Entry 120 section 10: the preview sits in the grid row, over inside the scroll viewer.** Inside one it measured its own natural size and left the window two thirds empty; the scroll viewer is now used only when zoomed, where panning is the point of it.
- **Entry 121: the version raised to 0.2.0, over renaming what is already published.** v0.1.0 is history and stays where it is; the train moves above it instead.
- **Entry 122: one interface for everything outside the process, over telling the benchmark not to click that button.** An exclusion list would have fixed this button and left the next one to be found by somebody's browser opening.
- **Entry 461: version vectors noticed by hash at each run, over a revision counter bumped in every save.** A counter alone cannot tell
  "changed on two devices since they last met" from "changed twice on one", and bumping in every save would have touched every screen
  that saves; the hash finds a device's own changes wherever they were made, and the vector says how two versions stand.
- **Entry 391: the M834's feed taken as 99.30 percent, the mean of all five readings against the lengths drawn, over planning's 99.35 or the scan's 99.49 alone.** The scan is the most precise single reading and agreed with the rendering across, but its scale along the carriage is unproven, and the plain mean keeps every reading within the instruments' own 0.2 percent.
