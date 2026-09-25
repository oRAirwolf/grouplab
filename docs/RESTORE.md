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
| `grouplab-crash-reports` issues | their text and comments in the nightly backup | nightly | `grouplab-backups` | read them from the backup's `crash-reports.json` |
| `grouplab` releases (the builds) | rebuilt from the tagged commit by the nightly workflow | on demand | GitHub Actions | run the workflow at the tag |
| `grouplab-testdata` | its own git history, and the bundle of it in the nightly backup | nightly | `grouplab-backups` | as the repository |
| `grouplab-backups` itself | the newest backups on this computer, until the next one succeeds | nightly | `C:\Dev\grouplab-local\backups` | copy back as a release asset |
| The server's GroupLab files: scripts, units, the nginx include, `.user.ini` | the repository (`website/server/`), and `install.py`'s dated copies | every change | the repository; `/home/ubuntu/grouplab-server/`, `/home/airwolf/backups/grouplab.org/config/` | `sudo python3 install.py --intake`, `--errors`, `--survey` |
| The server's private folders: `ready`, `incoming`, error reports, survey | nothing waits there for long: each is archived, turned into an issue, or counted and deleted | as the workers run | as each row above | nothing to restore; a lost report is sent again by the application |
| **The server as a whole, pissinhot.com included** | HestiaCP's own user backups: one copy a user, on the server only | daily | `/backup` on the server | **a gap until request 35 step 3**: nothing leaves the machine |

**The gaps, today:** the server as a whole (request 35 step 3, Oracle boot volume backups); `grouplab-backups` does not exist yet
(request 35 step 1), so nothing in the nightly column runs until it does. Until the whole-server gap is closed, Code's use of sudo on the
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

**The server.** For GroupLab's own files, `install.py` as in the table. For the whole machine, once request 35 step 3 is done: in the Oracle
Cloud console, the boot volume's backups, **Create Boot Volume** from the newest, and attach it in place of the damaged one.
