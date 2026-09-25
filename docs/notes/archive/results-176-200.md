# Phase 1 results, entries 176 to 200

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 175: the site sync's window follows nginx

The planning session confirmed on the server what the sync log had shown under entry 174: nginx's `open_file_cache_valid 60s` lets it
serve a file rsync has already replaced for up to a minute, the check requested the home page every three seconds for fifteen, and so it
kept the stale entry warm and read it every time. Alan applied a hot fix on the server, 12 tries 10 seconds apart.

**The repository now matches and goes further.** `grouplab-site-sync.py` has the same two values as its floor, and reads
`open_file_cache_valid` from the nginx configuration when it runs, in any unit nginx accepts, and makes enough tries to wait that long plus
30 seconds. A server set to 60 seconds gets the 12 tries the hot fix set; one set to three minutes would get 22. An unreadable file falls
back to 12. It only reads: nothing reloads nginx or changes its settings, which the whole server and pissinhot.com share.

**First deploy through.** `317932e` at 02:54 Mountain, first attempt, then `a77a1c7` at 02:59, first attempt. Whether the hot fix was in
by then the log cannot say. The live `/targets/` page carries `name="photos[]"`.

## Entry 176: the intake worker, made to finish

**What killed it.** `clamscan` loads the whole signature database into its own memory, about a gigabyte, inside the worker's 1 GB
limit. Every run was killed about 13 seconds in, and the worker tried again every two minutes. Nothing had caught it because no test had
run the scanner.

**What changed in the worker.**

1. **The daemon.** `clamdscan --fdpass`, so clamd, which runs as its own user, is handed an open file and quarantine stays `0750 airwolf`.
   The unit now has `RestrictAddressFamilies=AF_UNIX` beside `PrivateNetwork=yes`: the only socket it may open is clamd's.
2. **Limits that agree.** The cap was 600 megapixels, which needs about 7 GB to rebuild; it is 120 megapixels, 1.44 GB at the worker's
   peak of three copies at four bytes a pixel, and `MemoryMax=1600M`. A 108 megapixel phone and a 1200 dpi letter scan fit; a 200
   megapixel phone photograph is refused with the reason.
3. **Nothing stuck in silence.** The attempt count is written before each run, so a run the kernel kills still counts, and the third
   failed start sends the submission to `refused/` with `refused.txt`. Each submission gets one log line per run. The sweep no longer
   touches quarantine at all: a folder there is one the worker has not finished, and the hour rule would have deleted the first two real
   submissions.
4. **A scanner that did not scan is loud.** Each file's record says `clean, clamdscan` or `not scanned: ...`, the submission carries
   `notScanned`, the log says SCANNER DID NOT COMPLETE, and `Get-TargetSubmissions.ps1` prints how many files the scanner did not run on.
5. **HEIC.** Ubuntu 24.04 has no Pillow plugin for it, so `heif-convert` from `libheif-examples` decodes it to a PNG with the image
   already upright, and that is rebuilt like any file. Its camera facts do not come across; the record says it was HEIC.

**The installer** refuses `--intake` while Pillow, heif-convert, clamdscan or an answering clamd is missing, naming the package.

**The pull script** also reports what is waiting in quarantine and how long the oldest has waited.

**The test entry 176 section 7 asks for.** `tests/python/worker-tests.py`, in a CI job on `ubuntu-24.04`: the real worker, the real
clamd and clamdscan with a test signature, under `systemd-run -p MemoryMax=1600M -p RestrictAddressFamilies=AF_UNIX -p PrivateNetwork=yes`.
A phone-shaped JPEG with Orientation 6 and a GPS block must come out upright, without GPS, recorded as scanned; a HEIC must come out;
a file carrying the signature must be refused with the reason. It uses a generated photograph rather than a real one, because a real phone
file carries a location and would put it in the repository.

## Entry 177: the pull script, the removal script and the upright photograph

**The web path works end to end**, from a desktop browser and a phone, entry 173 section 1.3.

**The pull crash, and what else had it.** The worker writes each file under `stored`; the pull script read `stored_name`, joined the
null to the submission's folder, and a folder passed `Test-Path`. The check is now one function, `Test-SubmissionFolder` in
`scripts/SubmissionCheck.ps1`, which reads either key and requires a file. `Remove-ReadSubmissions.ps1` had the mirror fault, reading only
`stored`, which the old pissinhot.com submissions do not have, and a worse one: it iterated over the ledger object rather than its
`submissions` list, so under strict mode it stopped at the first `.id` and had never worked against the ledger as written. It now uses
the same check, reads the right list, and takes `-Only` to name submissions; a parameter called `-Id` would have been silently overwritten
by the loop's `$id`, because PowerShell names ignore case. CI's worker job runs the shared check on meta.json from a real worker run.

**Orientation.** The worker turned the pixels upright and then wrote the camera's Orientation, 6, into the new file, so GroupLab and
any viewer turned the photograph a second time. It now writes 1 and keeps the camera's value as `OriginalOrientation` in the JSON
record, which never reaches the file. The worker test asserts a phone JPEG with Orientation 6 comes out upright with the tag saying 1.
`ImageScrubber` keeps the original tag correctly, because it does not touch the pixels.

**The two submissions.** Both verify against their meta.json with the fixed check. `2026-09-24_58d94b23`'s tag is rewritten 6 to 1 on
this machine with every pixel and every other tag unchanged; its `meta.json` keeps the pulled hash as `sha256AsPulled` beside the new
one, and `orientation-fix.json` records why. The ledger records both: the test image as a test that is never published, the photograph
as Alan's own duplicate of a range frame, not added to any public set. Removing them from the server is Alan's command in request 1.

## Entry 178: the move from pissinhot.com is done, and a rule about HestiaCP's folders

**Entry 129 is complete.** `pissinhot.com/targets` and `www.pissinhot.com/targets` answer 301 to `https://grouplab.org/targets/`, the old
receiver answers 410, both sites answer 200, and the last pull from pissinhot.com found 18 on the server and 18 here.

**The fault was in my instructions.** Request 1 told Alan to back the include up beside itself. HestiaCP loads every `nginx.conf_` and
`nginx.ssl.conf_` file in a domain's `conf/web` folder, so the backup was loaded too and `nginx -t` failed on a duplicate
`client_max_body_size`. The `&&` kept the reload from running, so nothing live broke, but any other reload in between, a certificate
renewal for one, would have failed.

**The same fault was waiting in `install.py`.** Its `put` kept the old copy of anything it replaced as `<name>.<time>.bak` beside it, and
one of the files it installs is `nginx.ssl.conf_grouplab` in exactly such a folder. It has never fired, because the include was installed
fresh each time, but the second install that changed it would have left a live duplicate. Backups of anything under
`/home/airwolf/conf/web` now go to `/home/airwolf/backups/grouplab.org/config/`, named with the domain, and the dry run says where.

**The rule is written down** in `CLAUDE.md`'s standing constraints and in `docs/WEBSITE.md`, and request 1 now carries the commands that
worked, with a minute's wait before the checks, because a graceful reload lets an old worker answer a request or two.

## Entry 179: temporary files

**What filled 18 GB.** This session's scratchpad, nothing else: every time a test build had to avoid a locked folder I made a new
`altbinN`, fourteen of them at about 705 MB each; repository clones and the three copies made for the history rewrite; downloaded release
zips, installers and packages; rendered and rasterised sheets and one folder per research question. The largest single files were the git
packs of those clones, 157 to 158 MB each, and nightly zips and installers of 97 to 133 MB. **The suite's own leaks were in `%TEMP%`**:
14,987 `grouplab-settings-*.json` from App tests making a settings store and never removing it, 60 bench folders, 5 end to end images,
and 4,301 empty folders with random eight dot three names.

**The empty folders are `dotnet test`'s, not any test's.** No code in the repository makes a random temporary name, and a run of a few
Core tests left two new ones with every test process's temporary directory already redirected. So they are made by the runner process
before any test starts, two a run.

**What changed.**

1. `tests/Shared/TestTempRoot.cs`, a module initializer in both test projects, points `TMP`, `TEMP` and `TMPDIR` at a new
   `grouplab-tests/<pid>-<guid>` folder before any test runs and deletes it at process exit; a killed run's folder is swept by the next
   run after a day. Every test, the code under test and any child process writes there, so a forgotten cleanup line no longer leaks.
2. CI runs `dotnet test` with the runner's own temporary directory redirected to a folder it removes, and brackets the suite with
   `scripts/temp-leak-check.py snapshot` and `check`, which fails on any new `grouplab-*` entry, any run folder that did not remove itself,
   or any new empty random folder. Locally it passed on a full Core run.
3. `TestTempLeakTests` checks the redirection is in force and that no `Scratch*.cs` test is left in the suite.
4. `scripts/clean-scratch.py` removes earlier Claude Code sessions' scratch folders on this repository that nothing has touched for seven
   days, never this session's and nothing outside `c--Dev-grouplab`. It removed 15 empty ones today.
5. The scratchpad went from 18.4 GB to 707 MB in one listed delete, keeping only the App build in use and two small folders of work in
   progress.

**Entry 178 section 5, done with it.** `install.py` keeps only its newest `name.YYYYMMDD-HHMMSS.bak` of each file it replaces; a copy
made by hand under another name, such as the sync script's `.before-window`, is left alone.

## Entry 180: what the panel says is written down too

The planning session cannot see the panel, and Alan relies on it to say what needs him. So every message for him in the panel is now
also written to `docs/notes/panel.md`, which is ignored by git and read by the planning session, and a command that will stop for his
approval has its reason written there first. `docs/notes/for-alan.md` starts with the number of open requests and the most urgent;
requests 1, 2 and 10 were finished and still read as open, and are closed. `ForAlanTests` fails if the count and the requests disagree.
STATE.md is rewritten at the end of every entry.

## Entry 181: nothing copied to the server can carry a carriage return

The worker would not start after request 11 because its shebang read `python3` followed by a carriage return. The files under
`website/server/` were CRLF in the working tree Alan copies from. **The cause was this session's editing**: Python's `write_text` on
Windows translates line endings, so each script edit wrote CRLF, and git's `text=auto` normalised it on commit, so the repository never
showed it. `.gitattributes` now forces LF for `website/server/**` and shell scripts in every working tree, the copies here are rewritten
LF, the installer refuses a file with a carriage return and says which and how to strip it (tried on a CRLF file and an LF one), and a
test fails if any file in that folder holds one. My own edit scripts write bytes from now on.

## Entry 195: the error receiver shipped, sending on, and Unholy's codes read

**The error receiver was never shipped.** `website/build.py` listed and copied three receivers and not `error-report.php`, so the live
address answered "File not found." once nginx routed it, and request 24 stopped at its last step. It is listed and copied now; the site
build fails when a listed receiver is missing from its output or a receiver in `website/api/` is not listed; and `ReceiversShipTests`
holds every receiver to the list, the copy and the nginx include, and failed on the code before the fix. Published as 62f8b4a; an empty
post from outside then answered `{"ok":false,"code":"bad_report","error":"The report arrived empty."}` with 400. Request 24 is its last
step only.

**Sending targets is on.** Request 22's folder holds the rebuilt PNG, `meta.json` from the application, testing only and opted out,
`DO-NOT-PUBLISH` and `CONSENT.txt`; the rebuilt image's pixels equal the published sample's, with the same pixel hash as `sample.json`.
`appOpen` is true in its own commit, 1606619, and the guide, tour and article say so. The test is marked read in the ledger and on request
12's removal list; the sending program in a temporary folder was deleted. Request 21 stays optional: 17.6 MB went in 1.6 s.

**Request 23 passed**, and the zeroing grid scan is in `tests/test-data.json`, so CI fetches it and `UnholyZeroingGridTests` runs there.

**All four codes on Unholy's scan now read.** On the whole 5100 by 7013 scan neither detector found a code, at full or half resolution;
in each corner third, the WeChat detector found the code and the plain decoder read all 85 bytes, at full, half and a third of the
resolution alike. So where the whole image gives nothing, `ReadCodes` searches each corner third on its own. The scan names itself from
its four codes, and the every-sheet test again requires each sheet to name itself, on all three systems. Resampling in our own code, a
second decoder and larger printed codes were not needed.

**Tests.** Core `ReceiversShipTests`, and `UnholyZeroingGridTests` now also wants the sheet named from four codes.

## Entry 194: error reports into a private repository, built and switched off

**What users are asked.** Beside the target question on the first run screen, and in Settings under Error reports: send error reports
automatically, ask each time, or never. Until someone chooses, it asks: the crash banner offers "Send the error report", and nothing
goes by itself. Never sends nothing. While the receiver is closed, which it is, nothing is asked and Settings says so.

**What a report holds.** Built from the crash records GroupLab already writes: the build and system, the error and its stack with paths
taken out, and the names of the last events in the run's log, never their values. No description in an automatic report; a report made
by hand keeps up to 500 characters, and the report window now stops at 500. Both kinds from entry 192: an error survived, sent a minute
after it happens so a burst goes as one report with its count, and a close, sent at the next start. A record is marked sent and never
sent twice; the receiver also takes a report's identifier once. One that meets no answer, a limit or a closed receiver is kept and tried
at each start for seven days, then let go; one the receiver refuses for what it is, is let go at once. At most 20 a day, and the same
error again after its report has gone in the same session is not sent again. No dialog ever.

**The application never talks to GitHub and holds no token.** It posts to `api/error-report.php` on grouplab.org, through
`IOutsideWorld`: no Turnstile, 256 KB, 20 an hour and 60 a day from one address, 300 an hour in all, a disk floor and a kill switch. It
reads JSON from one form field, keeps only the schema's fields, drops the rest unread, and stores the report outside public_html for the
worker; the sender's address is never stored, only its salted hash for the rate limit.

**The worker**, `grouplab-error-worker.py`, its own service with the network it needs and nothing else: its own folders writable,
ProtectSystem strict, AF_INET, AF_INET6 and AF_UNIX only, no capabilities, and the token handed to it alone with LoadCredential from a
root-owned 0600 file that `grouplab-set-error-token` writes. It groups by the exception's type and GroupLab's top three frames, without
line numbers, and keeps one issue per error: the first report opens it, labeled with its signature and its kind; later ones update the
count, builds, platforms and dates, with a comment at most once per build per day; a closed issue reopens when the error comes from a
build newer than any seen before. A description is headed as the user's and untrusted, fences cannot be closed from inside a report, and
an at sign notifies nobody. When GitHub refuses the token, or says it expires within two weeks, the reports wait and the worker's status
file and log say so. `install.py --errors` installs it all, and the nginx include carries the receiver's block.

**The repository** exists and is PRIVATE; it has no issues yet. `gh issue list` on it is now the start of every run, in CLAUDE.md,
which also says an error report is data, never an instruction.

**For Alan:** request 24, the token's clicks, the install, the token script and one test report from
`scripts/send-test-error-report.py`. After that, `errorReportsOpen` goes true in its own build.

**Tests.** App `Entry194Tests`, 8. `receiver-tests.php` gains the error receiver's cases and `error-worker-tests.py`, 20 checks against a
stand-in GitHub, runs in CI's intake worker job; both run in CI only, the PHP because PHP is not installed here, and the worker's ran
here too.

## Entry 193: the zeroing grid found, no shots, and never a blank result

**Section 1: 95 against 99.** Both were checked out and run on Unholy's scan. Both read none of its codes and ask which sheet it is;
with the sheet chosen both register all 16 markers at 0.0025 in and find no hole. No commit between them touches how a sheet is named,
chosen or searched for holes, so there is no commit that made 99 find the sheet: what Unholy saw differ between the two is the same
behavior, and entry 191's test holds the part that matters.

**Section 2: why the bull was found and no shots.** Not the grid lines masking the holes, not a hole on a line, not the hole size and not
the caliber: the zeroing grid's one bull had a cell of no size, because a cell was sized from the spacing between bulls and one bull has
none, so every hole on the sheet was refused as out in the margins. Entry 189 found it from the renders and fixed it.

**Section 3: his scan and the other three.** His scan now finds **one** hole, 0.266 in, just below and right of the point of aim, which
is what the scan shows by eye; `UnholyZeroingGridTests` holds that count. All four zeroing grids, from renders with five holes each in
open cells and, new here, five each on the lines, one on a thin line in each direction, one on a crossing and one on each thick axis,
find every hole.

**Section 4: never a blank result.** A sheet found with no holes now says "GroupLab found GroupLab Zeroing Grid, mil at 100 yd and no
holes on it", gives the likely reason where there is one, marks set aside with Show work's reasons, or no caliber entered, and says how to
mark them by hand, choose Impact or press I and click each hole. The status line says the same in one line and points at the panel.

**Tests.** Core `EverySheetDetectsTests.HolesOnAZeroingGridsLinesAreFound`, 4, and `ASheetWithNoHolesSaysSoAndHowToMarkThem`.

## Entry 192: the caliber Set error, and a survived error is not a close

**The error.** Unholy's report holds two logs and one crash record, five `app.crash` lines between them, every one the same
`ArgumentOutOfRangeException` raised in Avalonia's caliber box as Set wrote its text with the list open. Set now closes the list before the
text changes, and the text it writes is not taken for typing, so the suggestions are not remade under it. With entry 189's change, a
chosen suggestion sets in one action and Set works first time. `Entry189Tests.SetWithTheListOpenSetsTheCaliberFirstTime` types "6.5",
highlights a suggestion and presses Set. It passes on the old code too, because the headless window cannot throw this exception; the
exception needs a desktop's own list, and the fix is written to its stack.

**The message.** A record now carries its kind: `survived`, from the window's thread or an unobserved task, or `closed`, from the process.
Each run leaves a marker with its process id and removes it on a clean exit, so a run that ends without reaching its exit is recorded as a
close at the next start. The banner says "GroupLab hit an error 5 times and kept running" for Unholy's case, calls only a real close
closing, and keeps Make a report for both. Records written before this have no kind and are still described as closes.

**Is surviving safe?** Not always, and it is written where the handler is installed. A handler that throws part way keeps whatever it had
changed before the throw. The caliber Set button threw before it told the session anything, so nothing was left half done; a handler that
changes the marking and throws before saving it, or changes one of two settings that go together, would leave them apart. Whether to keep
swallowing or offer to save and restart is a decision, and it is not changed here.

**Grouping.** A report now ends its environment text with every crash record on the computer grouped by the exception and the first
GroupLab frame, with a count, the kind and the last action: "5 times: System.ArgumentOutOfRangeException in ... , survived". It rides in
`environment.txt`, which the receiver already takes, so the receiver does not change.

**Still open from Unholy's and Fenix's reports:** nothing. Unholy's five were this one error. Fenix has sent no report; request 16's
trackpad half still asks for one.

**Tests.** App `Entry192Tests`, 2, and `Entry189Tests.SetWithTheListOpenSetsTheCaliberFirstTime`.

## Entry 191: Unholy's zeroing grid scan

**Kept apart from `Scan_20260923.png`.** The scan was copied, not moved, to `C:\Dev\grouplab-submissions\unholy\` as
`2026-09-24_zeroing-grid-mil-100yd.png`, identical to the original, SHA-256 367e55e5...7d73. `samples/PROVENANCE.md` has a record of
its own with that hash, the published copy's and both of `Scan_20260923.png`'s. It was decoded to pixels and written again before
anything else read it, so no metadata was read.

**What the scan is.** GroupLab's "Zeroing Grid, mil at 100 yd", printed at actual size: its markers put the sheet at 600.1 dpi on a 600
dpi scan. Letter paper. One shot, 0.266 in, just below and right of the point of aim.

**Why it failed, in plain words.** Two things, and neither is the print or the scan.

1. **Its codes do not read.** The four square codes are found and not decoded, on this scan as on the render at 200 dpi (question 56).
   So GroupLab cannot name the sheet and asks which sheet it is. That is the same on nightlies 95, 99 and today.
2. **Once the sheet is chosen, it registers perfectly and found no shot.** All 16 markers, 0.0025 in RMS, and 0 holes on nightlies 95
   and 99, because the grid's one bull had a cell of no size and every hole was refused as out of place. Entry 189 fixed that; today it
   finds the one hole.

Nightlies 95 and 99 were checked out and run on the scan: they behave the same. No commit between them changes how a sheet is named or
chosen, so "not found on 95, found on 99" is not a change in the code; entry 193 asks the same.

**The test.** `UnholyZeroingGridTests` loads the published copy, runs the chosen sheet, and wants 16 markers, 600 dpi and exactly one
hole where it is. It found none before entry 189. It runs here; in CI it skips with its reason until request 23 puts the file on the
test data release and the file joins the list CI fetches.

**Would Alan's own zeroing grids fail the same way?** Before entry 189, yes: every zeroing grid, whatever printed it, found no shots. From
the build carrying entry 189 they find them. Whether their codes read depends on the scan, question 56.

## Entry 190: Alan's standing consent covers Unholy and his other friends

Alan's words, 2026-09-24, are now in the three places the consent rules live: `samples/PROVENANCE.md` has a record of its own for what
he passes on from Unholy, who is also TNA, and his other friends; `CLAUDE.md` has a short section on what may be published and on whose
word, and its "never publish" line points at the consent records rather than naming one scan; and STATE.md says so. The 2026-09-23 scan's
record names Unholy now. The 2026-09-16 scan stays never published, and a stranger's submission is still governed by the level its sender
chose. The correction to entry 189 section 4.2 means Unholy's zeroing grid scan may be used and published, as entry 191 does.

## Entry 189: Unholy's feedback: the zeroing grids, the caliber box, the scale, and angles first

**The zeroing grids found no holes, from their own renders.** A new test renders every sheet in the library at 300 dpi with holes on
it, has it name itself from its codes, and runs detection end to end. Sixteen passed; all four zeroing grids found none of five holes
placed in open cells. The cause: detection keeps a mark only inside a bull's cell or within one cell of it, and a cell is sized from the
spacing between bulls. A zeroing grid has one bull, so its cell was a point and every hole was refused as out in the margins. A bull
with no cell of its own now takes the measurement grid it sits in (`RenderDifferenceHoleDetector.DetectionCells`); the renderer's use of
the cells is unchanged. All twenty sheets now pass, `EverySheetDetectsTests`. Three sheets that first missed one hole of five did so
because the test put a hole on printed ink and called it paper; the test now reads the render to say which.

**The codes are marginal on two sheets.** GL-ZERO-MIL-100Y's codes did not read from a clean render at 200 dpi, while they read at 150
and 300, and under one pattern of synthetic scanner noise at 300 dpi neither its codes nor GL-LR300-R36's read, where every other
sheet's did; with the holes placed differently, both read. Straightening each found code and thresholding it before decoding was tried
and did not help, and was taken out. Question 56.

**The caliber box.** Choosing a suggestion wrote its text into the box, and the box's text handler made the suggestions afresh on every
change, which threw the choice away, so "6.5" stayed and Set had to be pressed twice. The suggestions are now made again only for text
a person typed, and a suggestion chosen by a click, or by Enter or Tab on a highlighted one, goes into the box and is set in one action.
A test types "6.5" and presses Down and Enter, which failed before; the click is sent as what its release hands on, because the headless
window closes the list on a press before the list sees it. Entry 192 found the exception behind the Set button half of this.

**"Calibre" on screen.** The spelling check skipped any string with no space in it as a key. A string whose whole text is one British
word is now checked too, and the real keys, in the session file and the target format, say "British on purpose" on their line; the
check's `--self-test` holds the `Needed("Calibre", ...)` case, which it did not catch before. Fixed: the Setup label, the load readout,
the report, the Analyze tooltip, the still-needed list, the CSV import's unit names, Show work's parameter name, and the eight cartridge
family names ("not the same as .25 caliber"), whose names are now swept too.

**A scale set by hand could not be set again** by tapping the same two marks, because a tap on an end of the scale in use grabbed the end
and let it go where it was. A tap that does not move is now a tap, which starts a new scale; a drag still moves the end. The length and
rectangle tools also offer "Change the length (size) of the scale in use". A test sets a length, sets it again on the same marks and
somewhere else, then changes it from the tool, and checks the sizes follow each scale.

**Angles first.** With a distance, every size in view, the report's figures and Compare's cards and charts give the angle in the chosen unit first
and the size on the paper beneath, at the distance shot. Without one, sizes are on the paper and the panel says an angle needs the
distance, with a button to set it. A Settings box puts the size on the paper first. SMOA is in the glossary with Unholy's example, 0.422
in at 25.4 yd is 1.66 SMOA and 1.59 MOA, and its words are the Angles setting's tooltip.

**Thanks.** The README thanks Unholy and Fenix; request 16's naming half came back.

**Tests.** Core `EverySheetDetectsTests`, 20; App `Entry189Tests`, 4. Core 1642 passed, 2 skipped; App 288.

## Entry 188: the test data release is a draft, and CI still reads it

**Request 17 is answered.** The release is a draft, off the releases page.

1. **The tag is still there**: `git ls-remote --tags origin test-data` names `refs/tags/test-data`. There is one release with that tag,
   a draft, and no second one was made: the job's `gh release view test-data` found the draft, so it said "already exists" rather than
   creating another.
2. **CI reads it through the API, by its tag.** `scripts/test-data.py` lists the repository's releases through the API with the job's
   token, which sees drafts, picks the one whose tag is `test-data`, and downloads each file by its API address, never the public
   one. The run on a7c67cd said `reading the release through the API, which sees it as a draft too`, and its file came from the cache
   with its hash verified. So the download itself was exercised once from here with the same script and a token that can see the
   draft: 59,215,934 bytes, hash verified.
3. **The tests that read the file ran.** On Windows and Ubuntu the only Core tests skipped were the printer tests that need Windows
   drivers; `CalibreNeverMakesItWorseTests`, `HoleCentreAgreementTests` and the scan cases in `CaptureTests` ran and passed.

**The red check on a7c67cd** was the Windows App suite, whose test runner failed before any test started ("Test process did not return
valid JSON"); Core passed on the same runner and nothing in that commit touched the App. The next commit's run is the check.

## Entry 187: the answers to questions 50 to 55, and sending waits on one test

**Sending, question 55.** The one package is ready: the published sample scan, 25 marks detected, testing only, 17.6 MB, built by
the application's own code and posted through its own network path. This session was not allowed to post it, so it is request 22:
one command for Alan, then the pull, with the good result written out. `appOpen` stays false until the pull matches; request 12 now
lists the test's folder for removal.

**Question 54.** 40 degrees and the score's levels stand, recorded as judgment in `docs/MOBILE-CAPTURE.md` already. Every part of every
score now travels with a sent target, from the same record the session file writes (`MarkingFile.CaptureDocument`), where before the
package carried one sentence. White paper on a white board: the guidance and the corners placed by hand both exist on the desktop, and
a test now makes such a photograph, lit unevenly as Alan's range photographs are, proves the paper cannot be found and says so, then
sets the scale from four corners placed by hand.

**Question 53** accepted; a comment on `Pooling.Recentred` says to take the velocity share out per session the next time it is
touched. **Question 52**, option A, is a line in STATE.md for the next stable release. **Question 51** waits on request 9.

**Question 50, quietly.** Where a sheet has more scoring bulls than shots and nobody has said which bulls, one line beside "Bulls you
fired at" says how many of each and to choose them and press These ones, with Put this away. It is not a review item and holds nothing
back. Answering or putting it away is remembered for that target, the newest 200 kept.

**Request 19** is closed: the sheet is gone. Request 20 asks, in one line, for any commercial gridded sheet to be scanned before it is
thrown away.

**Release notes.** `scripts/release-notes.py` refuses a note with a British spelling, from the spelling script's own word list, and
takes the "(Entry N)" reference off the text a reader sees while it stays in the commit. Its self-test holds both. The published lines
were corrected and then put back, per entry 189 section 2.3.

**Tests.** Core `CaptureDocumentTests`, 1; App `Entry187Tests`, 3: the hint, answering it, and white on white.

## Entry 186: request 15 is installed, request 5 is done, and an opted out pull stays out

**Requests 15 and 5 are answered** in `docs/notes/for-alan.md`, and opted out submissions are accepted again: the one the old worker
refused went back to quarantine and reached ready with its PNG, `meta.json` and `DO-NOT-PUBLISH`. Eight requests are open.

**A folder moved back keeps its attempt count.** The count is what stops a submission that kills the worker from being tried for ever,
so a move back must not reset it; the cost is two moves back and no more, and the worker's comment says to delete `.attempts` before a
third. Only a comment changed, so the next install replaces the server's worker for nothing but that.

**The installer's backups.** `keep_newest_backup` removes the installer's own older `name.YYYYMMDD-HHMMSS.bak` files of the same name,
which the backup it kept on 2026-09-24 matches, so one should be left. It cannot be listed from here; request 15 carries the one `ls`
line to check, and asks for nothing to be deleted.

**An opted out submission stays out when it is pulled**, and tests already prove each step, so none was written:

- `worker-tests.py` takes an opted out submission, and one moved back from refused, through the real worker to ready and checks each
  keeps `DO-NOT-PUBLISH` and `exclude_from_public_dataset` true, then runs the pull script's own `Test-SubmissionFolder` on it and
  checks it reads as opted out.
- The pull copies each folder whole with `tar`, so the marker arrives with it, into `C:\Dev\grouplab-submissions`, outside the
  repository.
- Nothing that builds public data, fixtures or releases reads that folder. The only way in is `grouplab intake`, and
  `IntakeTests.OptedOutUnconsentedUnprovenancedAlteredOrUnknownSubmissionsAreRefusedAndNothingIsWritten` refuses the marker by
  itself, the field by itself, and a testing only consent. `scripts/release-notes.py` refuses a note naming a submission folder.

## Entry 185: the releases page, and the next nightly's notes

**The rolling `nightly` release is needed.** The updater reads its manifest from
`releases/download/nightly/update-manifest.json`, and the download page and the README link its stable asset names. So it stays, and
the nightly now titles it "Latest nightly (always the newest build, moves with every build)" with one line naming the numbered release
it points at. No notes, and no platform line, are repeated on it, so nothing there can go stale; `rewrite-release-notes.py --github`
writes the same line. The rolling release that still shows nightly 94 and the old platform line is replaced by the next nightly.

**The test data release.** A draft is off the public page, but its files are not at the public download address, and a workflow token
with read access cannot see a draft at all. So `scripts/test-data.py` reads the release through the API when it has a token, which finds
a draft too, and the one CI job that holds `contents: write` fetches and verifies the files and passes them to the test jobs as a one day
artifact. Both routes fetched and verified the 59 MB scan here. The test jobs' own token is not widened. The release is created as a
draft from now on; making the existing one a draft is request 17, with the command that undoes it.

**The next nightly.** Nightly 95 had already failed, at Write the notes: entry 173's note names grouplab.org/targets, and the path check
refused it as a file in this repository. A commit cannot be edited, and the note was right, so the check now passes an address on
grouplab.org and still refuses any other path; the self-test holds both (42688df). The notes it will publish were checked before it
did: nineteen lines under What you will notice, among them pan by default, the caliber names, the freezes, the zero correction's
distance and the scan in real inches. The report quotes them once it has published.

## Entry 184: each published build in #builds

`scripts/discord-announce.py` builds one embed: the version as its title, linked to the build's release page; the notes from
`scripts/release-notes.py`, the generator of the release body, less the platform line, with both headings kept; and the download page.
A description over Discord's 4096 characters ends at a whole line with "Full notes on the release page". Nobody is pinged. The
nightly runs it after its release is published, in a step that cannot fail the build; a night the gate skips never reaches it. A tagged
release, and never a draft, posts to #builds and #announcements. The webhook addresses come only from the two secrets Alan added,
masked in the log, and the script never prints one; with a secret missing it says so and posts nothing to that channel.
`.github/announced-builds.txt` records each version once posted, and a rerun posts nothing.

Tested: the script's self-test (normal notes, notes over the limit, only Under the hood, no notes at all, no address in a message);
`DiscordAnnounceTests` for the ordering, the masking, the stable-only channel and no webhook address anywhere in the repository. The
dry run on nightly 95's real notes printed a 25 line embed. Question 52: a stable release's own body is fixed text, not these notes.

## Entry 183: the opt out travels with the submission

**Scanning works.** After request 14 the first upload's log read `clean, clamdscan`: the stream through clamd's own socket, at the
new limits. Request 14 is closed.

**The fault.** The receiver writes a `DO-NOT-PUBLISH` marker beside `meta.json` when the opt out is ticked. The worker took every file
in the folder but `meta.json` for an upload, tried to decode the marker, and refused the submission. Every opted out submission was
refused; the three before it had the box clear.

**The marker is kept**, because a person listing the folder should see it without opening a file, and because the publishing build
already withholds on either record. So the worker knows it now:

- It reads `meta.json` first and refuses a submission without one, since neither its files nor its consent are then known.
- It rebuilds only the uploads the receiver recorded by `stored_name`. Anything else in the folder, except the receiver's record and
  marker and its own bookkeeping, is a refusal naming the file, never a decode attempt.
- `exclude_from_public_dataset` must be true or false, and must agree with the marker. Where they disagree it refuses and says which
  way, rather than choosing one.
- The marker moves with the folder to ready, and the log's ready line says the submission opted out.
- An upload whose bytes no longer match the SHA-256 the receiver recorded is refused.
- A folder moved back from refused, whose original the worker already rebuilt and deleted, is rebuilt again from the worker's own PNG,
  through the same scan, and recorded as `rebuiltAgain`; `refused.txt` is dropped.

**The pull script's check** reads the opt out from either record, treats a missing flag as opted out rather than as false, and
reports the two disagreeing. The publishing build already withheld on either record, and its tests already covered each case.

**The consent test.** CI's worker job now runs the real receiver, through the harness its own tests use, which moved to one shared
file so the two cannot drift. It sends one photograph with the opt out ticked and one without, and one more that is left as the refused
submission was. The real worker then runs under the unit's sandbox and the real clamd, and the pull script's own check reads each
result. Each must arrive with its flag, its marker or lack of one, one scanned PNG and no original. A disagreeing pair of records and an
unrecorded file must each be refused with its reason. Run on Windows without clamd against the same shapes, the worker did exactly
that. This test would have caught this fault, entry 174's field name and entry 177's key.

## Entry 182: the scanner takes a stream

**Why nothing was scanned.** The worker runs in its own mount namespace, from `ProtectSystem=strict`, `ProtectHome=read-only`,
`ReadWritePaths` and `PrivateTmp`. A descriptor it opened and passed with `--fdpass` referred to a mount clamd cannot see from its own
root, and clamd's AppArmor profile refused it as a disconnected path, so every scan ended "Not a regular file". Entry 176's loud
reporting is what caught it: the worker's log and the pull script both said the scanner did not complete.

**The fix, and the part the entry left out.** `clamdscan --stream` sends the bytes over the socket, so nothing about the sandbox or the
distribution's AppArmor profile changes. Streaming needs clamd's `StreamMaxLength` above the largest file scanned, which is the rebuilt
PNG, not the upload: at the 120 megapixel cap, three bytes a pixel, about 361 MB. **But `StreamMaxLength` is not the only limit.**
`MaxFileSize` and `MaxScanSize` skip anything larger and report it clean unless `AlertExceedsMax` is on, and Ubuntu sets them at 25 MB and
100 MB. So all three go to 400M, `AlertExceedsMax yes` makes an oversized file a finding rather than a pass, and the worker treats an
over-limit answer as not scanned, never clean. `install.py --intake` refuses to finish while `clamd.conf` says less, naming each line.

**Tested where it failed.** The CI worker job now runs the worker under the unit's own mount sandbox, not only its memory limit, with
clamd configured as the server will be and a photograph whose rebuilt PNG is over 25 MB, and it must come out recorded `clean, clamdscan`.

