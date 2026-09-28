# Phase 1 results, entries 201 to 225

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entries 206 to 208: the phones GroupLab must run on, the minimums, and the survey design

**The phones** (entry 206, approved in entry 207): the planning session's market study is in `docs/ANDROID.md`, "The phones it must run
on", with its sources, and the budget beside what the Fold 7 measured: Android 10 or later, 4 GB, peak under about 400 MB (373 MB at the
8 MP working size), detection about 10 s on the A16 class and 30 s on the A06 class (estimated from Geekbench, not yet run), 8 MP camera,
under 100 MB installed, 360 dp screens. The spike's minimum is now Android 10.

**The minimums table** (entry 207 section 2), in `docs/PLATFORM-SUPPORT.md` and so in the README and on the download page: .NET 10's
operating system floors; the architectures actually published (Windows x64, macOS Apple silicon and Intel, Linux x64; no Arm64 on Windows
or Linux); 4 GB of memory with 8 GB recommended, from the analyzer's measured peak of 733 MB on the 600 dpi sample; installed sizes from
the unpacked nightly 103 downloads, 226 MB Windows, 219 MB Linux and 185 MB macOS on Apple silicon, and about 220 KB a saved session; a
window about 1060 wide, question 58. Android is listed as planned.

**The survey** (entries 207 section 3 and 208): `docs/SURVEY.md`. What is asked and where, what is sent and never sent, the benchmark,
the route, the published page with groups under 10 merged, and the review of the minimums at 200 reports from a platform. Built with the
next desktop work; the Android part with the real application.

**Request 30** asks for the older test phones' models, Android versions and whether they still work.

## Entries 224 and 225: backups reach GitHub, the Store is built, the whole server can be restored

**Backups** (224 section 1.1). `grouplab-backups` was empty, and GitHub makes no release in a repository with no commit, so the first run
failed and its two reports became issues 4 and 5; the backup now gives an empty repository a README as its first commit. Tonight's backup is
in the repository and the restore test passed against it. Issues 4 and 5 are closed with the fix.

**The archive worker** (1.2). The token script's closing words named the error worker, copied from its script; they now name the archive
worker, and a test holds that. With nothing waiting, each run now checks that its token reaches the archive: on the server it reads
`"token": "ok"` and `"archive": "reachable"`.

**The signed Android build** (1.4). Nightly 109 built and signed the APK and AAB with Alan's key, and the release carried neither: both
upload lists name their files, and the Android ones were not named. They are now, with versioned names on the numbered release and plain
ones on the rolling release. Request 36 names the AAB once a nightly carries it.

**Windows signing** (2): on hold, request 37 closed as not yet; the comparison stays in `docs/RELEASE-PLAN.md`.

**The Microsoft Store** (3). `scripts/package-msix.ps1` makes an MSIX of the self-contained build, stamped as the Store's copy
(`GroupLabDistribution=store`), in which GroupLab's own updater is off and Settings says the Store keeps it up to date (`StoreBuildTests`).
The manifest's floor is Windows 10 version 1809, the Store's, now in the minimums table. CI builds and checks the package on every push with
a stand-in identity. `release.yml` builds it with Partner Center's identity from repository variables, attaches it to the release, and sends
each tagged release to the Store with Microsoft's own tooling (`microsoft/microsoft-store-apppublisher`, `msstore publish`). **The split**:
stable releases to the public listing, nightlies never; a beta flight only if a beta train is ever made. The first submission is by hand,
because the automatic path needs the app already live. The listing, ready to paste, is `docs/store/LISTING.md`; Alan's part is request 38.

**The whole server** (224 section 1.3, 225). `docs/RESTORE.md` has the restore from an Oracle boot volume backup, what is lost, what to check,
and that the backups are crash consistent; the weekly line says the Oracle backups are checked in the console, not by the report. The policy is
on; the first backup is due 2026-09-26 09:00 UTC, and until Alan confirms it, sudo stays limited to GroupLab's own files.

## Entry 223: request 34 done, and the survey opened

Request 34 is closed: Alan's reload and checks passed, and the `503` he saw the second time was the receiver's own "closed". **The survey
is open**: the worker is installed and tested, the desktop's question is tested in `Entry208Tests`, and the phone asks it the same way.
`surveyOpen` in limits.json and `OPEN` in the receiver are true together, as the site's build requires; the receiver test that proves a
closed receiver refuses now makes its own closed copy. The article what-grouplab-sends lists the survey as the fifth thing that can leave
a computer, and says that nothing goes when the program opens except what the person chose to have sent by itself. The tour's and the
user guide's Settings now describe one Sharing section with its three parts; the tour also said error reports were not switched on yet,
which had been untrue since entry 200.

## Entry 222: as much automation as possible, backups first

**Section 6, first.** `docs/RESTORE.md` lists everything Code or the planning session can change, its backup, how often, where, and how to
restore it, and CLAUDE.md now opens with the rule. **The gap it names**: the server as a whole has no copy off the machine. HestiaCP keeps one
backup a user, on the server itself (`BACKUP_SYSTEM='local'`), made daily; the newest is 2026-09-25. Until Oracle's boot volume backups are
on (request 35 step 3), Code's sudo is limited to GroupLab's own files and its installer, so it does not reload nginx, which pissinhot.com
shares; request 34's reload stays Alan's. `ubuntu`'s sudo is already passwordless (`NOPASSWD: ALL`), so no sudoers line is needed.

**Section 2.** `grouplab-archive-worker.py` archives each checked submission, proves it by download and SHA-256, rewrites the manifest and
only then deletes the folder; installed by Code with `install.py --archive` and running every ten minutes, waiting for the token (request
35 step 2). systemd would not start it without a credential file, so the installer leaves an empty root-only one. 14 checks against a
stand-in GitHub, in CI. **The backlog**: Code ran the fixed pull under Windows PowerShell 5.1, and the 9 on grouplab.org and 18 in the old
pissinhot.com folder are archived, proven and off the server; the archive check restored all 27 under both shells. Two more 5.1 faults
were fixed on the way: `Get-FileHash` honored `-WhatIf`, and the check read a JSON array as one object and found no months.

**Section 3.** `scripts/backup.py`, nightly at 03:30: the archive's submissions copied here and checked against the manifest, then git
bundles of this repository and `grouplab-testdata`, `local.zip` of the local-only files, `grouplab-local` and `grouplab-originals`, and
the crash reports, 433 MB tonight with a manifest of every file's SHA-256; kept 7 daily, 4 weekly and 6 monthly in `grouplab-backups`,
or on this computer until that repository exists (request 35 step 1). The weekly restore test checks every file and clones the bundle;
it passed today. A failure is sent as an error report. **Encryption** (section 3.6): not added. The repository is private, as the archive
is; encrypting would put a passphrase in Alan's hands whose loss makes every backup useless, which is a worse failure than the one it
guards against for this data. If ever wanted, a 7-Zip AES archive with the passphrase in his password manager, at no other cost.

**Section 4.** `scripts/cleanup.py`, weekly: only what rebuilds itself is deleted directly (it cleared 5.3 GB of old numbered build
folders today); anything else goes to `C:\Dev\grouplab-trash\<date>\`, emptied after fourteen days and never before a backup since.
`scripts/server-week.py` reads the workers' week and the server's own backup, read-only, into the weekly line and STORAGE.md. The weekly
line sits under the open count in for-alan.md, written by `scripts/automation-report.py`. Two scheduled tasks run all of it as Alan
while he is logged on, with no password: registered, and the weekly one run once, result 0.

## Entry 220: request 31's pull stopped at the archive; fixed, and tested under both shells

**Nothing was removed from the server.** In `Get-TargetSubmissions.ps1`'s last loop each folder is archived (`Add-ToArchive`) before
`sudo rm` runs for it, and the very first `Add-ToArchive` threw, so the script ended before any removal. The 9 are on the server and here.

**The cause** was Windows PowerShell 5.1 turning `gh release view`'s "release not found", on stderr for a month with no release yet, into a
terminating error under `$ErrorActionPreference = 'Stop'`, `2>$null` notwithstanding. `scripts/NativeCommand.ps1`'s `Invoke-Native` now
runs every program in `SubmissionArchive.ps1`, `Get-TargetSubmissions.ps1`, `Remove-ReadSubmissions.ps1` and `Test-SubmissionsArchive.ps1`
(gh, ssh, cmd, tar and the ledger's python): stderr collected, never fatal, the exit code alone deciding. **Tested for the case itself**:
`tests/powershell/archive-tests.ps1` runs the archive against a stand-in gh (`fake-gh.py`) that says "release not found" on stderr as the
real one does; nine checks pass under Windows PowerShell 5.1.26100 and PowerShell 7.6 on this machine, the old script fails it with
Alan's exact error, and CI now runs it under both shells on Windows. `SubmissionScriptsTests` fails if a raw program call comes back.

**Dry runs**: with `-WhatIf` the ledger only prints (`storage-ledger.py --check`), and the pull says "would archive and then remove N".

**Actions artifacts**: both figures were true. Entries 215 to 217 freed the ones older than three days; at about forty pushes a day, each
leaving about 400 MB of Windows packages, three days of new ones came to 81.7 GB in 954. CI's package artifacts now keep a day (they were
three), the ledger frees anything over a day old (it was three), and this run freed 44 GB; 38.1 GB remains, all under a day old.

**The crash reports repository** has three issues. Number 1 is the closed test report. Numbers 2 and 3 arrived at 10:34 UTC from Alan's
machine, from nightlies 35 and 31, about a week old: each closed at start because a file the installation should hold was missing, the
Fluent theme library in one and SQLite's native library in the other. They read as damaged installs of those builds, sent now by the newer
build's queue; current builds do not start that way, and nothing in the code is at fault. Left open, as entry 194 section 4 asks.

**Entry 221**: the planning session's renamed lock file was empty and has been deleted.

## Entry 219, item A5: sharing a session file by hand

`GroupLab.Core.Records.SessionPackage` writes and reads a `.grouplab` file: `session.json` (schema, revision, the writing device as
"GroupLab version on the system or phone model", never a machine's own name), the desktop's marking file with no path from the writer's
machine in it, the sheet, and the picture. `CleanImage.From` re-encodes the picture from its stored pixels, so a photograph's EXIF, GPS,
XMP and comments stay behind; a test writes a location into a real JPEG and finds none of it in the file. Reading a stranger's file
takes only the four known names, each within its size however small it claims to be. The desktop's menu shares and opens one, the
phone's result shares through the share sheet and Sessions opens one; either way it becomes a session of its own, saved in the
records. `SessionPackageTests` (five) and `SessionFileTests`. On a phone: request 33, step 7.

## Entry 219, item A4: capture to result on the phone, corrected by touch

Take a picture or choose one; the working copy (8 MP) becomes the session's image, which settles the question item A1 left of storing a
scale beside the marking: there is none to store. The sheet is named, the holes found, the figures and the photograph with its holes
shown, and the desktop's composite plot drawn; the session saved in the desktop's database, and listed under Sessions. Three pieces
of the desktop moved into shared code on the way, each now called by both: the plot fills itself from a marking
(`CompositePlot.Show`), a session record is built in Core (`SessionRecords.Build`), and the survey's question is on the phone's first
run and in its Settings. Built here and in CI; **not yet on a phone**, which is the next sitting's (request 33 gains two steps).
**Second part**: correcting by touch (move with a magnifier, add, remove, undo, each saved at once), the caliber and distance asked
before the picture and remembered, angles from the distance, and the sheet chosen by name where its codes cannot be read. The caliber
and distance are kept in the desktop's settings file (`LoadShotSetup`), which `AndroidSharingTests` checks. A4 is built; its
measurement on a phone is request 33's step 6.

## Entry 219, item D1: the hardware survey and benchmark, built and switched off

**The benchmark** (`GroupLab.Core.Survey.Benchmark`): GL-CF25-LTR at 300 dpi with one hole in each of its 25 bulls, the same pixels on
every machine (a test compares their hash), analyzed as a photograph is; it finds all 25 here. **The report** (`SurveyReport`) holds
exactly docs/SURVEY.md section 2's list: `Keys` names every field and a test walks a built report against it, and another checks that
the user name, machine name and home folder never appear. A random installation number, replaceable in Settings.

**The desktop**: the survey is the third question on the first run screen, after targets and error reports, with the benchmark offered
under it and nothing chosen; somebody who answered the other two before sees the screen once more with only the survey and a line
saying their answers are kept. Settings gathers all three under one section, **Sharing**. Once the person says yes, each analysis's
sizes, times and memory are kept in a small file beside the settings, and a report goes at most weekly, or when a benchmark is
waiting; saying no deletes that file. The queue (`SurveyQueue`) is shared with the Android application. `Entry208Tests`, seven tests,
and `Entry203Tests` now covers the survey's words at every width.

**The server**: `website/api/survey.php` rebuilds a report from its named fields, stores a salted hash of the installation number and the
day only, and limits an installation to three reports a day; `grouplab-survey-worker.py`, with no network, counts each report into
classes and deletes it, and writes the published totals with every group under ten merged into "other". Receiver tests in
`tests/php/receiver-tests.php`, run by CI; `tests/python/survey-worker-tests.py`, ten checks. `install.py --survey`.

**Switched off**: `surveyOpen` is false in limits.json until request 34 installs the worker. The receiver answered on the live site as
soon as it was published, before any nginx change, so it now refuses everything while `surveyOpen` is false, and the site's build
holds its `OPEN` to that; nothing is stored that no worker would delete. **The release notes** now take a heading
written into a note off its start: nightly 107 carried "Under the hood: the rules ..." under Under the hood. The first try at nightly
107, on 6c976e3, failed creating its release with a 403 from GitHub with nothing changed in the workflow or the repository's settings;
the next, on 27bd109, published.

## Entry 219, item A3: the application project, org.grouplab.app

`android/GroupLab.Android` builds here and in the `android` workflow, which now uploads `grouplab-apk` beside the spike's (the spike stays
until item A4, because request 33 installs it). Navigation along the bottom, the first run's questions, Settings under Sharing, the log,
crash records and the error report queue, all through the desktop's own files compiled as they are. To share them, the desktop's
literals moved into `SharingWords` and its error sending into `ErrorQueue`, which the main window now calls; the desktop behaves as
before. Two things in the shared files only the Android build could see: a settings default named through the main window's own enum,
now compiled only on the desktop, and a comment naming a list by an old name. `AndroidSharingTests`, three tests. **Not yet on a phone**: no device was
attached; it goes on at the next sitting with request 33's.

## Entry 219, item A2: the capture screen spike, built; measured in request 33

**In Core, tested on the desktop**: `CaptureGuidance.Judge` gives docs/MOBILE-CAPTURE.md's one instruction in item C3's order from the
outline and the frame's quality, and `Ready` only when every condition holds (item C1); a condition holds at half its quality part,
the angle within the limit. `CaptureGuidance.JudgeFrame` judges a whole frame the way the desktop judges a photograph: outline, the
sheet's markers, angle and quality. `CaptureScreenTests` are the two tests the document named as still to write, and a whole frame:
a Letter sheet on a dark board is ready, the same sheet edge to edge is told to move back. A render's pure white paper read as blown
out, which it would be in a photograph too, so the test's paper is 225.

**On the phone**: `CameraSession` binds CameraX (1.6.2 bindings) to the activity: a preview on the camera's surface hosted inside the
Avalonia screen, a still at the largest size in maximum quality, and an analysis stream. The sheet is named from its codes on the
stream, then each frame is judged; three ready frames in a row fire the shutter. The lens is chosen by zoom, 0.6x, 1x and 3x; a tap
locks focus and exposure there. The still is analyzed at 8 MP. Every line goes to `spike-log.txt` on the phone. Built here; the
`android` workflow now builds Release, so Alan can install its APK himself. **Not measured yet**: request 33, one sitting.

## Entry 219, item A1: the working resolution in Core

**`WorkingSize`** (Core): 8 megapixels for the phone, always, from entry 209's Fold 7 measurements; 60 for the desktop, only for images
far larger than a Letter or A4 sheet. `ImageLoader.Load` and `LoadMaxChannel` take the limit, decode a JPEG reduced by a power of two
where that does not undershoot, then resample by area to exactly the working size, and scale the resolution with it. `grouplab analyze
--working-megapixels` uses it. `WorkingSizeTests`: the sizes, and the sample read at 8 MP with both channels alike and its resolution
scaled.

**The accuracy cost, full size against 8 MP**, measured with `grouplab analyze` on 2026-09-25. The sample: 25 of 25 shots on the same
bulls, a mean shift of 1.2 thousandths of an inch, 4.2 at most; mean radius 0.232 and sigma 0.185 in, unchanged. Two range photographs,
chosen as the first of the 59 whose full size result is plausible (four have a mean radius under 0.5 in; the others are misread at any
size, which is the photograph detector's known weakness, not the working size): 15 of 15 shots on the same bulls, mean shift 3.6
thousandths, 13.3 at most, mean radius 0.232 to 0.233; and 15 of 16, mean shift 7.9, 38.4 at most, mean radius 0.249 to 0.250.

**Not done**: the desktop application does not bring very large images down yet; the saved session would have to record the working
scale, and it waits for the roll sheet work that needs it. **Next on the roadmap**: A2, the CameraX capture spike.

## Entries 215 to 218: nothing kept on the server, a private archive, and a ledger of GitHub

**The pull** (`scripts/Get-TargetSubmissions.ps1`) now, for every submission still on the server whose copy here verifies, backlog
included: zips it into the private archive's release for its month (`scripts/SubmissionArchive.ps1`), downloads it back and compares
the SHA-256, rewrites that month's `manifest.json` (name, size, SHA-256, consent level), and only then removes the folder from the
server with `sudo rm`, one line a folder. A folder that does not verify, or that the archive does not prove, stays and is listed.
`-KeepOnServer` removes nothing; `-NoArchive` removes after the local check alone. Tested end to end against the real archive with a
synthetic submission dated 1999, uploaded, proven, recognised on a second run, and the release deleted. `SubmissionScriptsTests` holds
the order. `scripts/Test-SubmissionsArchive.ps1` restores and checks everything in the archive; on the empty archive it reports none.

**On the server** the workers now bound every folder: intake refused 7 days and ready 60 (quarantine by attempts, entry 176); error
reports unsent 30 days and set aside 7. `tests/python/error-worker-tests.py` has three new checks (23 in all); `WorkerLimitTests`
holds the numbers. The upload page no longer says submissions wait until read: it says when they leave, where the copies are, and links
to "Where it is kept, and for how long" in `what-grouplab-sends`, the one place the rules are written.

**The ledger** (`scripts/storage-ledger.py`, `docs/notes/STORAGE.md`, budgets in `docs/notes/storage-budgets.json`): GitHub held about
152 GB for the project, of which 140 GB was Actions artifacts, 1,990 of them, mostly per-push Windows and Linux packages kept 30 days.
The packaging workflow's now keep one day, which is all the nightly needs, and CI's three; `--free` deleted about 65 GB older than
three days, oldest first. About 75 GB remains, all from the last three days' pushes and uploaded under the old thirty day retention, so
it stays over budget until later runs of the ledger free it as it ages; new uploads expire in one to three days by themselves. The releases hold 11.6 GB against 20; the archive is empty; the rest is small.

**Entry 218**: the archive confirmed private with Alan's login (`visibility` PRIVATE), and its README replaced with one saying what it
is, that it is never made public, and that the ledger tracks it.

**Waiting on Alan**: request 31, the workers installed and one pull on each server's folder, which clears the backlog; request 32, a
backup of the local copy, his decision.

## Entries 213 and 214: the key off the plot, the rings further back

**The key** (`CompositePlot.KeyLayout`): beside the plot or below it, whichever leaves the plot the larger square, and a small Key button
in a strip of its own where neither leaves 240 units; the button opens the key over the plot only when pressed. Everything is drawn and
clipped in `DataRect`. The first version preferred beside, and on the sample the 410 wide key left the plot a 250 wide strip, so the
larger square decides. `Entry213Tests`: the key and the plot's area never meet, and every shot and the center are inside the area, at
1400 by 900 and at 1060 by 700, and a 320 by 300 plot collapses to the button. The report's key was already the caption under its
square. The toggles along the plot's bottom edge still sit over it, as they have since entry 109; they were not part of this entry.

**The rings** (entry 214): `#282828` on the dark paper, half entry 210's lightness, and `#bdbdbd` on white, moved toward the paper for
the same relationship, since toward black would have made them heavier than the outlines. `Entry210Tests.TheRingsSitBehindTheOutlinesAndStillShow`
holds both themes: fainter against the paper than the half strength outlines, apart from them in tone, and at least 1.2 to 1.

**The caliber in the pictures**: 0.308 was named by the figure test only to draw outlines; the application never read it. The sample's
load block says 6.5 Creedmoor (`samples/sample.json`), and the pictures now use 0.264 in. The 6.5 Creedmoor line in PROVENANCE.md that
entry 213 read belongs to Unholy's 2026-09-23 scan. Pictures: `docs/figures/composite-plot-210-{group,whole}-{light,dark}.png`, replaced.

## Entries 211 and 212: request 29, and the older phones held back

**Request 29**: upside down turned on both of the Fold 7's screens, and the folder picker offered Google Drive, where a folder could be
chosen; OneDrive was not seen. Stage B of the sync plan is possible through Drive; its reliability is for later.

**The older phones** are the reference devices, used only at named milestones (entry 212): once before the first Play closed testing
release, and for a problem the Fold 7 and the emulator cannot show. The Essential PH-1 was already connected; its model, Android 10 (API
29), Snapdragon 835 and 4 GB were read from it and nothing else was done. The Galaxy S20 5G is Android 13 with 8 GB; a OnePlus is to come.
The working size stays the Fold 7's 8 MP. `docs/ANDROID.md`, "The phones it is tested on".

**Also this run: a nightly lost behind a notes commit.** The notes commit for these entries was pushed while entry 210's build ran, and
the nightly for entry 210 stood down because main had moved on, trusting the newer push to bring its own; a notes commit brings none. The
nightly now publishes anyway when every newer commit is a notes or screenshot commit and none touches a workflow, which is the rule that
made it stand down; `WebsiteWorkflowTests` holds both conditions.

## Entry 210: the composite plot's rings, and the whole target

**The rings** are drawn `CompositePlot.BullRingInches` wide, 0.05 in on the page, so they stay in proportion as the view zooms: about 19
pixels on the sample's group view against entry 204's 4, held between 4 and 40 pixels. They are a solid mid grey, `#a0a0a0` on white and
`#505050` on the dark paper, darker than before and 60 to 75 apart (summed channels) from the half strength outlines, which stay thin.
Everything else is drawn over them; on the sample, holes sitting on a ring read as black dots on the grey.

**Framing**: Group, the group alone as before, or Whole target, every ring with the group inside, beside the plot and remembered
(`AppSettingsStore.LoadPlotWholeTarget`); Compare has the same choice. **Zoom and pan**: the wheel zooms about the pointer, a touchpad's
pinch and a touch pinch zoom, a drag that starts on empty paper pans while one that starts on a shot still picks it, and a double click
or tap returns to the fitted view; everything is drawn as vectors, so it stays crisp. The report keeps the whole target, as it always
spanned the rings or the shots, because its page has no clipping to cut a ring at a group framing; its caption says so.

**Tests**, `Entry210Tests`: the ring tone darker than entry 204's and apart from the outlines in both themes; the ring width at least
three times 4 and clearly wider than an outline; the group view holding every shot; the whole target holding every ring's edge and every
shot; the choice remembered; zoom about a point keeping it still and the reset restoring the scale. Entry 204's layer test now holds the
rings below every mark instead of faint.

**Pictures**, the sample in both framings and themes: `docs/figures/composite-plot-210-{group,whole}-{light,dark}.png`; the before
pictures are entry 204's `composite-plot-after-{light,dark}.png`. In the whole target view the key sits over the top left shot, as it
always has at that corner.

**The log split, fixed on the way.** NOTES-FROM-PLANNING.md had grown back to 293 KB, and `scripts/split-logs.py` run a second time
overwrote each archive file with only the entries being moved: the September notes archive lost about 8,400 lines in the working copy,
and the results' "newest" were taken by position in a file no longer in order, so entries 186 to 210 were archived and 154 to 185 kept.
Nothing was committed. The logs were restored from the last commit plus entry 210, and the script now merges into what each archive
holds, chooses what stays live by entry number, keeps a result's unnumbered parts with it, rebuilds each index from everything archived,
and stops before writing if any block would be lost. Checked independently: of 16,475 non-blank lines before, none is missing after
except four index and header lines rewritten with their new counts. `DiscordLinkTests` now exempts the notes archive as it did the live
notes, as CLAUDE.md asks of a test that reads a log.

## Entry 209: the Fold 7 in one sitting

**Resolution against memory and time**, the sample at 600, 400, 367 (12 MP), 300 (8 MP) and 200 dpi, each run in a fresh process on a
file already at that size: 16.1, 6.6, 5.1, 3.3 and 1.6 s; 715, 461, 427, 373 and 307 MB; 25 of 25 holes at every size, with a mean shift
from the full resolution run of 1.6, 1.2, 2.5 and 2.9 thousandths of an inch and at most 11.8. The first attempt shrank the image after
loading it at full size and every size peaked near 530 MB, which is why the real application decodes at the working size. The spike at
rest holds about 274 MB. Full table in `docs/ANDROID.md` section 5.

**The cameras**, read through Camera2 with the camera permission granted over adb and no camera opened: a 22 mm equivalent main camera
built from physical cameras 2 (ultrawide, 14 mm), 5 (wide, 22 mm) and 6 (telephoto, 66 mm); largest ordinary stills 12.5, 12.0, 12.5 and
10.0 MP; every one reports intrinsics and distortion. `docs/ANDROID.md` section 4.

**Built**: `SpikeScaled`, `SpikeCameras`, a launch extra naming a task to run unattended, and a Choose a folder button that logs only the
provider. The spike's minimum is Android 10, entry 207's.

**Request 29**: upside down on both screens and one look at the folder picker; then the phone is no longer needed.

## Entry 205: the Fold 7 passed folding; all four ways up; the start up lines explained

**Request 27, Alan's hands**: folding, unfolding and turning kept every line and rearranged the panels as designed; the largest font size
cut nothing; his run of the sample took 18.9 s at 716 MB. Avalonia is confirmed for the phone.

**Upside down portrait** did not turn, because Android leaves reverse portrait out unless the activity asks. The spike's activity now asks
for `ScreenOrientation.FullUser`, all four directions while honoring the rotation lock, which `FullSensor` would ignore. Checked over adb:
rotation locked at 180 degrees with the spike in front turned the display to 180, `dumpsys` gives the activity's requested orientation as
`SCREEN_ORIENTATION_FULL_USER`, and the phone's own settings (rotation following the sensor, at 0) were put back straight after.
`docs/MOBILE-CAPTURE.md` gains item C5: the photograph stored upright and the overlays turning, whichever way the device is held.

**The repeated start up.** The spike now logs each create and destroy of its activity and each time its view is shown, with counts. Back
finishes the activity with the process still alive, and opening it again creates a second one ("the 2 time in this process"); Avalonia's
single view is the application's, so the list carried over. Going home and back, and opening recents, log nothing. The provisional sizes
(1 by 1, and the full size at 1 pixel a dp) are logged as ignored and not laid out.

**Each image's own peak memory**, in a fresh process with pushed photographs run before the sample: photograph 635 MB, scan 721 MB.

## Entry 204: the composite plot, quieter

**What changed**, back to front as it is drawn: the bull's rings are a wide pale grey band (4 wide; 1.5:1 on white, 1.7:1 on the dark
paper); each shot's outline is at half strength, and so is its point when no caliber is set, while a picked or excluded shot keeps its
own look; CEP 50, 90 and 95 are green, 2.5 wide against the outlines' 1.5, dotted, solid and dashed; the extreme spread stays a red dashed
line; the group centre is a pair of green lines across the whole plot and the aim point a pair of blue ones; a picked shot is drawn last.
The key lists only what is drawn and names each mark by colour and pattern. Toggles beside the plot turn CEP 50, 90, 95 and the extreme
spread on and off, 44 high and reached with Tab, CEP 95 off at first, and `AppSettingsStore.LoadPlotMarks` remembers them. Compare's small
plots follow the same toggles and its caption says what they show. The report draws the same marks in the light theme's inks; its page
has no dashed stroke, so CEP 50 is a ring of dots and CEP 95 a ring of dashes made of dots, and its caption names each and says when the
extreme spread was left out. The glossary's CEP entry covers 95.

**The colours**, in the palette both themes read (`Tokens.Plot`): green `#007a4d` light and `#3ddc84` dark, blue `#0055d4` and
`#5aa9ff`, each at least 5.4:1 on its paper. **Deuteranopia**, simulated with Machado's 2009 matrix: the red and the green come close
(46 and 60 apart on a 0 to 441 scale, light and dark) and are told apart by shape, a dashed line between two shots against circles and
lines across the whole plot; the blue stays at least 150 from both and from the ink.

**Tests**, `Entry204Tests`: the stroke widths, opacity and contrasts in order; the defaults, each toggle adding and removing exactly its
key entry, the choice remembered, and the report caption following it; and a render in which the aim point's row and column read blue and
the group centre's green at the plot's edges. `ThemeTests` now allows the one half strength the entry asks for and holds the new inks to
4.5:1; `Entry105Tests` reads the new key. The figures are drawn from the published sample scan with .308 named, which the sample does not
record, so that the outlines show.

## Entry 203: the consent choices wrap; the analysis screen at narrow windows

**The fault.** A radio button given a plain string shows it on one line, so on nightly 102's first run screen both consent choices ran
off the card mid sentence. Settings' own consent radios already wrapped; the first run card's, the question after Accept and analyze's,
the Sending targets and Error reports choices, and three check boxes did not. Each now takes `MainWindow.Wrapped(words)`, a text block
that wraps, and `MainWindow.WordsOf` reads a button's words either way, so `PressSend` and the Settings text lists read the text block.
The consent wording is unchanged.

**The test**, `Entry203Tests`, looks at every visible text block under the first run card, the question and Settings, including the
ones a radio's template draws, and fails on a line wider than its space or a block past the edge of anything that clips it. It runs at
1400, 960 and 683 units wide: Avalonia lays out in device independent units, so 150 and 200 percent display scale are a narrower window
in them. With the old first run card it fails at 1400 on the testing only line. It passes now at every width for the first run card and
Settings.

**Found on the way: the analysis screen needs about 1060 units.** Its three columns are 300, at least 320 and 372 wide, fixed, and do
not shrink, so at 960 (a 1920 pixel screen at 200 percent) the right column runs 97 past the window, sending question and figures with
it. That is the look of the main screen, so it is question 58; the question's test runs at 1400 and 1060 until it is answered.

**Nothing is chosen for the person**: neither consent level is selected on the first run card, the question, or in Settings while none
has been chosen, and `NeitherConsentLevelIsChosenForThePerson` holds it. The selected level in Alan's screenshot was his own click.

## Entries 201 and 202: the Android SDK installed; detection runs on the Fold 7

**Entry 201.** Request 25 done on the retry; the likelier cause of the first failure was running the SDK step before the workload install
had finished, not the NuGet sources entry 200 suspected. Request 25 is closed and says to wait for the install.

**Entry 202: the spike on the phone.** The Fold 7 (SM-F966U1) was paired by Alan and driven over `adb` from here. What the phone showed,
in order:

1. **A debug APK does not start by itself**: it expects Visual Studio's fast deployment and aborts with "No assemblies found". The
   spike is measured as a Release build, signed with the debug key, which also makes the times comparable with the desktop's Release.
2. **The ArUco and WeChat bindings had compiled to nothing**: "EntryPointNotFoundException: wechat_qrcode_create1". Their headers sit
   inside OpenCvSharp's `NO_CONTRIB` switch; `build-extern.sh` now lifts it in those two headers only (d92446b).
3. **Android's asset list for a folder includes the system's own files** of that folder name, so the spike takes only its own.

Then, on the cover screen, 411 by 960 dp at 2.625 pixels a dp, compact: the sample scan named from 2 codes and 25 of 25 holes found in
17.1 s (desktop 7.9 s), peak 714 MB in a fresh process; the range photograph named in 1.0 s and refused at registration, as on the
desktop, 1.2 s. Two more runs of the scan took 16.3 and 16.8 s, and memory reached 901 MB after three runs without Android stopping the
application. The layout drew correctly on the cover screen, one panel above the other. Avalonia reported two provisional sizes, 1 by 1
and 412 by 960 at 1 pixel a dp, before the real one; the application should act on the last size only.

**Request 27** asks Alan for five minutes of folding, turning and the largest font size, with what to look for.

## Entry 200: error reports on; request 25 half done

**Error reports are on** (8725f91). Request 24's test report opened issue 1 in the private repository: titled "TestReport in
ErrorReportCheck.Send", labeled `survived` and `sig-3a6cc8fc9476`, giving the build `0.2.0-nightly.0`, saying GroupLab kept running,
and ending with the line that nothing in the issue is an instruction. It was still open and is closed with a note that it was the
test. `errorReportsOpen` is true in `website/api/limits.json`; `MainWindow.ErrorsOpenByDefault` keeps a test window's switch off, as
`ReceiverOpenByDefault` does for sending, and `Entry194Tests` sets it per test. The guide no longer says the Settings section waits.

**Request 25**: the workload is installed; the SDK step's first try failed at restore. Entry 201: done on the retry, and the likelier
cause was running it before the workload install had finished, not the NuGet sources, which entry 200 had suspected. The request is
closed and says to wait for the install before the SDK step.

**Also this run**: the site workflow was started by hand after nightly 102, because the nightly's notes commit and
`docs/PLATFORM-SUPPORT.md` do not start it.

