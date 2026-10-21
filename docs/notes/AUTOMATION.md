# What is still done by hand, and what could stop being

NOTES-FROM-PLANNING.md entry 462 section 5: every recurring step that Alan, the planning session or Claude Code still does by hand,
how often, and whether it can be automated with nothing from Alan. Kept current: a step built or switched on moves to the sections below.

| Step | Who does it now | How often | Already automated? | Automatable with nothing from Alan? | Notes |
|---|---|---|---|---|---|
| Usage line: `npx.cmd ccusage@latest daily`, one line in for-alan.md, one in usage-log.md | Code | daily | Part: `scripts/usage-statusline.js` writes usage-now.json | yes | CLAUDE.md, "The standing budget", entry 317 |
| Print the count of open requests in for-alan.md | Code | start of every run | no | yes | CLAUDE.md, "Anything that needs Alan", entry 149 |
| List open crash-report issues (`gh issue list`), also hourly while waiting | Code | before each entry, each turn end | Issues are made by the server worker and testflight-feedback.py | yes (listing and counting); no, needs a judgement (the assessment) | CLAUDE.md, entries 194 and 457 |
| `scripts/clean-scratch.py <session folder> --delete` | Code | start of every run | Script exists; `scripts/Run-Automation.ps1` runs cleanup.py weekly, not this | yes | CLAUDE.md, "Temporary files", entry 179 |
| Rewrite docs/notes/STATE.md | Code | end of every entry | no | no, needs a judgement | CLAUDE.md, entries 160 and 180; a test holds it under 120 lines |
| Fold an entry into NOTES-FROM-PLANNING.md, delete its inbox file, write PHASE1-RESULTS.md | Code | every entry | no | no, needs a judgement | CLAUDE.md, "Where the work comes from" |
| Read docs/notes/external-status.md | Code, planning | start of a run | Written by `scripts/status-note.py` from testflight.yml, store-status.yml, store-follow.yml | yes (a one-line digest in STATE) | CLAUDE.md, entries 335 and 336; only the reading is by hand |
| Confirm a pushed site commit is live (`grouplab-site-build`) within 20 minutes | Code | every publishing push | Part: website.yml checks the meta tag and rolls back | yes | CLAUDE.md, "The website"; the report line is by hand |
| Keep docs/RELEASE-NOTES.md current | Code | when behind | Yes: nightly.yml writes and pushes each build's entry | n/a | CLAUDE.md, "Release notes" |
| Write a `Release-note:` trailer on every commit | Code | every commit | Checked by `scripts/release-notes.py` in nightly.yml | no, needs a judgement | CLAUDE.md, entries 132 and 145 |
| Fix the weekly consistency findings | Code | weekly | Found by consistency.yml (issue labelled `consistency`) | no, needs a judgement | `scripts/consistency.py`, CLAUDE.md entry 267 |
| Tour words against a changed screenshot | Code | after screens change | Pictures: screenshots.yml, Mondays | no, needs a judgement | CLAUDE.md, entry 146 section 4.4 |
| Split the logs when they grow back (`scripts/split-logs.py`) | Code | when live files grow | Script only | yes | CLAUDE.md, entry 160 |
| Publish a ready research article | Code | per article | no | no, needs a judgement | CLAUDE.md, entries 158 and 393; PUBLISHED.md by hand |
| Merge learning-loop tuning pull requests | Code | as they arrive | The pull request is made by the learning workflow | no, needs a judgement | CLAUDE.md, entry 396 |
| Nightly and weekly backup, restore test, weekly line | Alan's computer | nightly, Sundays | Yes: Register-GroupLabTasks.ps1 tasks, `scripts/backup.py`, `automation-report.py` | n/a | RESTORE.md "How it runs"; only while Alan is logged on |
| One look at the Oracle boot volume backups (a copy older than 4 days) | Alan | now and then | no | no, needs a secret (an Oracle key) | for-alan.md request 46; RESTORE.md entry 398 |
| Restore test of the whole server | Alan | not scheduled | no | no, needs a secret (an Oracle key) | RESTORE.md, tested 2026-10-09 |
| Announce a build on Discord | Code, workflows | every build | Yes: `scripts/discord-announce.py` in nightly.yml and release.yml | n/a | Stable also goes to #announcements; webhook secrets are Alan's |
| Pass fix notes ("For Quack" lines) on to testers | Alan | per fix | Lines are written by Code in for-alan.md | no, posts in Alan's name | Draft can be made; entry 462 section 5.3 |
| Thank testers on Discord | Alan | per fix or idea | no | no, posts in Alan's name | Same draft route as above |
| Read the panel and for-alan.md, tell Alan what needs him | Planning | each session | no | no, needs a judgement | CLAUDE.md, entry 180 |
| Read the Discord bug-reports forum | Server worker, Code | continuous | Yes: the worker makes issues labelled `discord` (RESTORE.md, entry 419) | n/a | Code assesses each issue |
| Phone and Store submissions and approvals | Workflows, Alan | per build | Part: testflight.yml, play-upload.yml, store-follow.yml, store-submit.yml | n/a | Apple and Microsoft steps that need Alan are in for-alan.md |

The handoff task named in entry 462 was not found in CLAUDE.md, RESTORE.md, STATE.md or NOTES-FROM-PLANNING.md; the planning session
should say what it is.

## Built in entry 462

- **`python scripts/run-start.py <session scratch folder>`**: the start of every run in one command. It prints the open requests' count,
  the open error reports (titles printed as data), clears earlier sessions' scratch folders by clean-scratch.py's own rule, gives
  external-status.md in a line a store, lists the inbox, the week's usage and whether the logs need splitting (`split-logs.py --check`).
  Replaces rows 2 to 5 and the split check. Changes nothing but the scratch folders, which clean-scratch.py already deletes.
- **`python scripts/site-live.py [<commit>] [--minutes 30]`**: asks grouplab.org's build tag once a minute until the commit is live or
  the time is up, and says which. Replaces the by-hand confirmation after a publishing push. Changes nothing.
- **The site's accessibility check on every push** (`scripts/site-audit.cjs --only a11y` in checks.yml) and **the layout check of every
  screen weekly** (screenshots.yml) and on every push (the entry 447 window tests), from sections 1 and 3: problems that were found by eye.
- The usage line stays part by hand: run-start prints the week's figures, and the line saying which model did what is a judgement.

## Prepared, off until Alan says yes

- **Tester notes in the Discord** (request 107): `scripts/tester-notes.py`, a step in nightly.yml. Posts each new "For <tester>" note
  once its fix is published, opening "Hey <name>,", through `DISCORD_TESTERS_WEBHOOK`; without it, the nightly's summary shows drafts.
  The notes in for-alan.md on 2026-10-21 are recorded as already passed on (`.github/tester-notes-posted.txt`). Undo: delete the secret.
- **The weekly Oracle backup look** (request 106, replacing request 46's look by hand): `scripts/oracle-backups.py` and
  `.github/workflows/oracle-backups.yml`, Mondays, with a key that can only read boot volume backups; keeps one issue labelled backups
  open while the newest Full is over eight days old or every backup expires within a week. Without the five OCI secrets it says so and
  does nothing. Undo: delete the secrets, or the Oracle user.
- **Thanking testers** goes with the tester notes: each note already opens with the thanks.
- **The whole-server restore test** stays by hand: restoring needs more than read access, and nothing here should hold a key that can.
