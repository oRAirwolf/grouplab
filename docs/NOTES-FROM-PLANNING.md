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

