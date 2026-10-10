# Backups, and how to restore

NOTES-FROM-PLANNING.md entry 222. Alan: "The most important thing to me is that anything that you have access to delete or change, that
there are backups in place to minimize the damage if something bad happens." This file is the list of everything Code or the planning
session can delete or change, what backs each one up, how often and where, and how to get it back. **Anything with no backup is a gap,
listed as one, and Code does not act on it until the gap is closed.**

## If something is gone, first

1. **Stop.** Do not run the thing that deleted it again, and do not "tidy up" around it.
2. Find it in the table below, and follow its restore line.
3. Say what happened in the planning session, so the rule that let it happen is changed.

## What can be changed, and what backs it up

| What | Its backup | How often | Where | Restore |
|---|---|---|---|---|
| This repository, every branch and tag | GitHub itself, and a git bundle of every ref in the nightly backup | every push; nightly | github.com/oRAirwolf/grouplab; `grouplab-backups` | "The repository" below |
| Its files git does not hold: `docs/notes/panel.md`, `.claude/` settings | the nightly backup zip | nightly | `grouplab-backups` | "Local files" below |
| `C:\Dev\grouplab-local` | the nightly backup zip | nightly | `grouplab-backups` | "Local files" below |
| `C:\Dev\grouplab-originals` | the nightly backup zip | nightly | `grouplab-backups` | "Local files" below |
| `C:\Dev\grouplab-submissions` | the private archive, every submission zipped and proven by SHA-256 | as each one arrives | `grouplab-submissions-archive` releases | `scripts\Test-SubmissionsArchive.ps1` restores and checks them; the sync copies any missing back |
| `grouplab-submissions-archive` | this computer's `C:\Dev\grouplab-submissions`, checked against the manifest | nightly | this computer | nothing is deleted from the archive unless this copy verifies (entry 217) |
| `grouplab-submissions-archive`'s own files: `.github/workflows/learn.yml` and `learning/` (the real scoreboard's rows, its baseline, the nightly summary, the tuning record), entry 394 | the workflow's source in this repository (`scripts/learning/learn.yml`); the repository's git history; its tarball in the nightly backup | every run; nightly | GitHub; `grouplab-backups` | copy `scripts/learning/learn.yml` back as `.github/workflows/learn.yml`; unpack `submissions-archive-files.tar.gz` and push `learning/`; or rebuild the rows with `learn.py score` then `nightly`, which loses only the baseline's history |
| `grouplab-crash-reports` issues | their text and comments in the nightly backup | nightly | `grouplab-backups` | read them from the backup's `crash-reports.json` |
| `grouplab-crash-reports` files (TestFlight screenshots, entry 326) | the repository's tarball in the nightly backup; App Store Connect keeps the originals | nightly | `grouplab-backups` | unpack `crash-reports-files.tar.gz` and push its `testflight/` folder back |
| `grouplab` releases (the builds) | rebuilt from the tagged commit by the nightly workflow | on demand | GitHub Actions | run the workflow at the tag |
| `grouplab-testdata` | its own git history, and the bundle of it in the nightly backup | nightly | `grouplab-backups` | as the repository |
| `grouplab-backups` itself | the newest backups on this computer, until the next one succeeds | nightly | `C:\Dev\grouplab-local\backups` | copy back as a release asset |
| The server's GroupLab files: scripts, units, the nginx include, `.user.ini` | the repository (`website/server/`), and `install.py`'s dated copies | every change | the repository; `/home/ubuntu/grouplab-server/`, `/home/airwolf/backups/grouplab.org/config/` | `sudo python3 install.py --intake`, `--errors`, `--survey`, `--learning --backup <a fresh change backup>` |
| The server's private folders: `ready`, `incoming`, error reports, survey | nothing waits there for long: each is archived, turned into an issue, or counted and deleted | as the workers run | as each row above | nothing to restore; a lost report is sent again by the application |
| **The server as a whole, pissinhot.com included** | Oracle Cloud boot volume backups, policy `grouplab-daily`: one a day at 09:00 UTC, kept 4 days, so never more than the five Always Free covers (entry 398); restore tested 2026-10-09; and HestiaCP's own user backups, one a user, on the server | daily | Oracle Cloud, off the machine; `/backup` on the server | "The whole server" below |

**The gaps, today:** none. **The schedule** (entry 398 section 6, Alan, 2026-10-09: "Cut to 5 days to stay free."): one backup a
day at 09:00 UTC, kept 4 days, with no weekly full schedule, so at most five exist even in the minutes when a new one lands before the
oldest expires; Oracle's Always Free tier covers five volume backups. Alan changes the policy in the console; Code cannot see it.
**Why the old expiry did not match the old setting** (Alan, 2026-10-09): the daily schedule was set to keep its backups 2 days, but
Oracle kept them 13 days, so thirteen backups existed on 2026-10-09 rather than the two or three expected. On 2026-10-09 Alan set the
daily schedule to 4 days and removed the weekly schedule. The backups taken before the change keep their 13 day expiry and expire by
themselves by 2026-10-22; none is deleted by hand. Whether the new 4 days holds is seen in the console after 2026-10-13: a backup older
than four days still listed there means Oracle is again keeping them longer than set. Oracle keeps
the chain of incremental backups whole by itself, so the newest one restores the whole volume as it was then.

**What four days means for the standing rules.** A mistake on the server has to be noticed within about four days to be undone from the
whole-server backup; after that every backup carries it. Code's own dated copy of each file it changes (rule 5 below) is not affected:
those stay in `/home/ubuntu/grouplab-server/backups/` until removed by hand, so a change Code made can be undone however late it is found.

**Restore tests of the whole server** (newest first):

- **2026-10-09, passed** (request 91, entry 398, done by Alan in the console): the backup "Auto-backup for instance-20260324-2036 (Boot
  Volume) via policy: grouplab-daily on 2026-10-09 09:00:00", created 2026-10-09 09:02:44 UTC, incremental, 2 GB of the 47 GB volume,
  Available, was restored with **Restore boot volume** as `restore-test`, same availability domain, default size, no backup policy. It
  came up **Available at 47 GB** (created 11:12:01 UTC) and was then terminated; the console shows it Terminated. The server's own boot
  volume stayed Available and was not touched. Thirteen backups existed, every one Available.

Before it: the first Oracle boot volume backup was 2026-09-26 (entry 230), the first Full 2026-09-27 (entry 235); no restore of either
was tried.

**Sudo on the server** (entry 230 section 1.3): with an off-machine copy of the whole server, Code's sudo is no longer limited to
GroupLab's own files. Everything else holds: nothing of pissinhot.com is touched apart from the approved `/targets` redirect, `nginx -t`
passes before any reload and both sites are checked after, nothing is written into HestiaCP's `conf/web/<domain>/` folders, and **before
any sudo change outside GroupLab's own files, what will change and how to undo it are written here or in the commit first.**

**How it runs**: two scheduled tasks on this computer, registered by `scripts\Register-GroupLabTasks.ps1` as Alan, only while he is logged
on, with no password stored: `\GroupLab\Nightly backup` at 03:30 (`scripts\backup.py`: the archive copied here, then the backup) and
`\GroupLab\Weekly check` on Sundays at 04:30 (the restore test, `scripts\cleanup.py`, and the week's line in `docs/notes/for-alan.md`).
Each writes to `C:\Dev\grouplab-local\automation.log`; a failure is also sent as an error report, so it reaches Code. Until the whole-server gap is closed, Code's use of sudo on the
server is limited to GroupLab's own files and its installer.

## The standing rules

1. **Before anything destructive**, Code checks that the newest backup covering it is less than a day old and passed its last restore test.
   If not, it makes one first: a fresh backup run, or for a server file a dated copy outside the HestiaCP `conf/web/` folders, as the
   installer does.
2. **Nothing is force pushed to `main`, ever.**
3. **Deletions on this computer go through the trash first**: `C:\Dev\grouplab-trash\<date>\`, emptied after 14 days and never before a
   nightly backup has succeeded since. Build output and test leftovers, which rebuild themselves, are deleted directly.
4. **Proof, not assumption**: a weekly restore test downloads the newest backup, checks every file against its manifest, and restores the
   bundle into a clone to compare with GitHub. The weekly line in `docs/notes/for-alan.md` says whether it passed.

5. **Every change on the server is backed up before it is made** (entry 397, Alan, 2026-10-09: "make a backup of anything it changes on
   the web server").
   1. Before the first change of a session, the newest Oracle boot volume backup (policy `grouplab-daily`) is less than a day old and its
      last restore test is recorded as passed, here. If not, the server changes stop and a request in `docs/notes/for-alan.md` says so.
   2. Before each change, `website/server/grouplab-change-backup.py` (run with sudo on the server) copies exactly what it will touch into
      `/home/ubuntu/grouplab-server/backups/<date>-<label>/`, never under HestiaCP's `conf/web/`: every file edited, replaced or created
      (with mode and owner; one that does not exist yet is listed as absent), `dpkg --get-selections` and `apt-mark showmanual` before a
      package changes, `systemctl list-unit-files` and `list-timers --all` before a unit changes, and any crontab that will change.
   3. Each change has an undo beside its backup that puts the files back, removes what it added and restarts nothing of pissinhot.com.
      It is run once with `--dry-run` and the output goes in `docs/notes/panel.md`.
   4. One entry below per change: what changed, where its backup is, the undo's path, and how to check the undo worked. Never the
      server's address or the key's path.
   5. After each change both sites are checked, and `nginx -t` passes if nginx was touched at all.

**Changes made under rule 5** (newest first). The restore test passed on 2026-10-09 (above), and the newest Oracle backup was from
09:02 UTC that day when these were made.

- **Waiting for Alan (request 93), the nightly's own token check** (question 95 (b), 2026-10-10). Will change:
  `/usr/local/sbin/grouplab-learn-worker.py` only (the nightly asks GitHub with the learning token for one issue and one pull request and
  writes the answer into its log and `summary.md`). Backup: `grouplab-change-backup.py --label learning-token-check` of that one file,
  made by the same paste before the install. Undo: `sudo install -m 755 -o root -g root <backup>/files/usr/local/sbin/grouplab-learn-worker.py
  /usr/local/sbin/grouplab-learn-worker.py`. Check: the worker's SHA-256 is the backup manifest's, and both sites answer. The backup's
  folder goes here when Alan sends it.
- **2026-10-09 11:47 UTC, the synthetic board's time limit** (entry 395). Changed: `/usr/local/sbin/grouplab-learn-worker.py` only (a
  synthetic board past its hour no longer stops the night's run). Backup:
  `/home/ubuntu/grouplab-server/backups/2026-10-09T114707Z-learning-worker-synthetic/`, 117,551 bytes. Undo and check: as below.
- **2026-10-09 11:33 UTC, the learning worker's memory fix** (entry 395 section 6). Changed: `/usr/local/sbin/grouplab-learn-worker.py`
  (the nightly reads each submission in a process of its own and skips one it cannot read) and the three `grouplab-learn-*.service`
  units (`OOMPolicy=continue`, so a command line killed for memory fails only its own submission). Backup:
  `/home/ubuntu/grouplab-server/backups/2026-10-09T113248Z-learning-worker-oom/`, 116,322 bytes, the same ten paths, packages and units.
  Undo: `sudo python3 install.py --learning-undo --backup <that folder>` puts back the worker as first installed; the undo of the whole
  worker is the entry below. Check: the worker's SHA-256 is the backup's, and both sites answer.
- **2026-10-09 11:22 UTC, the learning worker** (entry 395). Changed: six packages and 75 they pulled in (libraries only, no service),
  `/usr/local/sbin/grouplab-learn-worker.py` and `grouplab-set-learning-token` (new), `/usr/local/sbin/grouplab-archive-worker.py`
  (replaced by the one that waits for a score, at most six hours; the installer also kept the old one beside it), six units in
  `/etc/systemd/system/` (new) and three timers enabled, an empty `/etc/grouplab/learning-token` (new), the folders
  `/home/airwolf/web/grouplab.org/private/learning` and `/home/airwolf/grouplab-learning` (new). Before it, the staging folder was copied
  whole to `/home/ubuntu/grouplab-server-before-learning-20261009.tgz`. Backup:
  `/home/ubuntu/grouplab-server/backups/2026-10-09T112228Z-learning-worker/`, 78,544 bytes (the archive worker with its mode and owner,
  the other nine paths recorded as absent, `packages.txt`, `manual.txt`, `unit-files.txt`, `timers.txt`). Undo:
  `sudo python3 install.py --learning-undo --backup <that folder>`; its dry run listed the three timers, nine files to remove, the archive
  worker to put back, the 81 packages to purge and the two folders (`docs/notes/panel.md`, 2026-10-09). Check: no `grouplab-learn` timer
  in `systemctl list-timers --all`, the archive worker's SHA-256 is the backup's, `dpkg --get-selections` matches `packages.txt`, and
  both sites answer. Both answered 200 after the install.

## Restoring

**The repository.** From GitHub: `git clone https://github.com/oRAirwolf/grouplab`. If GitHub's copy is the damaged one, from the newest
backup: download `grouplab.bundle` from the newest release of `grouplab-backups`, then
`git clone grouplab.bundle grouplab-restored` and `git -C grouplab-restored remote set-url origin https://github.com/oRAirwolf/grouplab`.
Every branch and tag is in the bundle.

**Local files.** Download `local.zip` and `manifest.json` from the newest release of `grouplab-backups`
(`gh release download <tag> -R oRAirwolf/grouplab-backups -D C:\Dev\restore`), unzip, and copy back only the folder that was lost. The
manifest lists every file with its SHA-256, so a copy can be checked before it is trusted.

**A submission.** `scripts\Test-SubmissionsArchive.ps1` downloads and checks every one; the zip for one is
`gh release download archive-YYYY-MM -R oRAirwolf/grouplab-submissions-archive -p <name>.zip`.

**The server's GroupLab files.** `install.py` as in the table.

**The whole server** (entries 224 and 225), pissinhot.com included. In the Oracle Cloud console, region US West (San Jose), compartment
spetsnaz (root):

1. **Storage**, **Block Storage**, **Boot Volume Backups**: choose the newest backup from before the problem, then **Restore boot volume**
   from it, in the same availability domain as the instance.
2. **Compute**, **Instances**, the server, its **Storage** tab, **Replace boot volume**, and choose the volume made in step 1. The console
   stops the instance, swaps the volume and starts it again; the old volume is kept, detached, until it is deleted by hand.
3. **What is lost:** everything written after the backup was taken: submissions, error reports and survey reports received since, and
   anything on pissinhot.com changed since. Submissions archived before the backup are safe in the archive and on this computer.
4. **What to check after:** both sites answer (`curl -sS -o /dev/null -w '%{http_code}' https://grouplab.org/` and the same for
   pissinhot.com, `200` each); `systemctl list-timers 'grouplab-*' --no-pager` lists the site sync and the intake, error, survey and archive
   workers and the learning loop's score, nightly and tune with next runs; and `sudo cat /home/airwolf/web/grouplab.org/private/archive-worker/status.json` says `"token": "ok"`.

**These backups are crash consistent**: the volume as it was at one instant, like pulling the power. That is fine for the web sites and
HestiaCP, and MySQL recovers on start as it would after a power cut.

**Proof, for the whole server:** the weekly check in the automation report reads the server's own HestiaCP backup file and its date, but it
cannot see the Oracle console. The Oracle backups are checked by Alan in the console, under Boot Volume Backups, whenever he wants to; the
weekly line says so rather than claiming it. A restore test is the same as step 1 above to a volume named `restore-test`, waiting
for it to say Available, then **Terminate** on it under Boot Volumes, never Replace boot volume; the last one is listed above.
