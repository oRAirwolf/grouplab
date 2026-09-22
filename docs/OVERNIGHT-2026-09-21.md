# Overnight, 2026-09-21 into 2026-09-22

NOTES-FROM-PLANNING.md entry 135 section 0.1. This is the live state of tonight's queue. It is read at the start of every wake-up, updated after every item, and committed with the work.

**Next step:** queue item 1, the entry 131 section 1 checklist pass over every screen, starting with the analysis panel rebuild. Then item 6, question 37's control. `scripts/Test-RealUpdate.ps1` runs again once the nightly carrying entry 139 publishes, which is the proof entry 139 section 5 asks for. The website still needs republishing, and the sync's fixed live check still needs Alan to run the installer on the server.

**Stopped at Alan's request after `6c32b1e`,** with queue items 4 and 5 finished. Local suites green alone: Core 1249, App 169.

**Then the server, the site and question 38, on Alan's instruction.** The fixed sync went on the server (Alan ran both `sudo` commands, both clean), the website publish is under way, and question 38 is answered by measurement: there is no photograph factor to measure. `docs/PHASE1-RESULTS.md`, "What a hole measures in a photograph".

**One red to know about, and it is not the code.** The `build and test` run on main for `28a3365` failed on `windows-latest` with every step after `setup-dotnet` unfinished: the runner was lost after 52 minutes. The identical tree passed on `windows-latest` on phase-1 in the same minutes, and both local suites are green, so there is nothing to fix. The push of `6c32b1e` re-runs it.

---

## The update that installed and did not reopen

Alan pressed Install and restart on nightly 31 and GroupLab did not come back; starting it by hand reported a crash.

**Proved fixed, nightly 42 to nightly 43 on this machine:**

```
04:42:44.267  INFO   update.install  version=0.2.0-nightly.43 silent=yes
04:42:52.564  INFO   app.start       version=0.2.0-nightly.43
04:42:53.798  INFO   update.arrived  from=0.2.0-nightly.42 to=0.2.0-nightly.43
04:42:54.026  INFO   app.window      scale=1.5 width=1400 height=900
```

It came back on its own eight seconds after the installer started, drew its window and stayed up, and no crash record was written.

**The cause, in one sentence:****The cause, in one sentence:** the silent update passes `/CLOSEAPPLICATIONS`, which makes Inno Setup use the Restart Manager, and the Restart Manager restarts what it closed, so GroupLab was brought back while its files were still being replaced and died on its first line.

```
01:50:43.910  INFO   update.install version=0.2.0-nightly.35 silent=yes
01:50:51.709  INFO   app.start      version=0.2.0-nightly.35
01:50:52.211  ERROR  app.crash      ex=System.IO.FileNotFoundException
                     message="Could not load file or assembly 'Avalonia.Themes.Fluent, Version=12.1.2.0'"
01:50:52.239  INFO   app.exit       code=-1 seconds=0.5
```

That file is in the installed folder now, so nothing is missing from the package: it was simply not written yet when the process started. `RestartApplications=no` now leaves the relaunch to the installer's own `[Run]` entry, which runs after every file is in place.

**It was not the other possibility.** The record is the new process crashing, not the old one being killed mid-save: `SaveSession` runs before the installer is started, and a crash record is written from an unhandled exception and nothing else, so an exit GroupLab chose can never be recorded as one.

**Two things were missing and are here now.** GroupLab records the moment the installer started, so a start long afterwards is one a person made themselves: it logs `update.relaunch.missed` and says so in Settings. And `scripts/Test-RealUpdate.ps1` is the real update test written down, with the check that was absent: it waits for a window from a new process and fails if none arrives or a crash record appears.

## The queue

| # | Item | State |
|---|---|---|
| 1 | Entry 131 section 1: before and after renders of every screen, checked against the checklist and fixed where they fail. Analysis page first. | **in progress**: renders now taken at 1280 by 720 and 2560 by 1440, before and after kept under `docs/figures/screens/`. First pass on the analysis page: the mean radius is the one figure in the logo's amber, and the zero correction's direction word no longer runs off the edge. The panel rebuild from `AnalysisPanel` is still to do. |
| 2 | Entry 131 section 6.2: the zero offset picture | **done**: `ZeroOffsetPicture`, behind a "Where it landed" disclosure on the zero block. The aim as a cross, the group's centre with its uncertainty ellipse, and an arrow an axis pointing the way a shot has to move, with the clicks on it. Where the uncertainty covers the aim there are no arrows, because an arrow is an instruction and there is nothing to instruct. |
| 3 | Entry 131 section 7: the Equipment screen, with the old "rounds or components" box gone | **done**: a rail slot of its own, three lists, a form generated from the one field list so the screen and the record cannot drift, autocomplete offering earlier values, and a name already used refused rather than silently overwriting. The old box is gone and the column carries a link to the screen instead. |
| 4 | Entry 131 section 8: the ballistics page rebuilt | **done**: the trajectory graph with drop, wind drift, velocity and energy each on their own and the zero marked; the inputs already grouped and the dope table already there; and now the imperial and metric toggle at the top. It moves the whole application's units and rewrites what is typed rather than only relabelling it, so 2850 ft/s becomes 868.68 m/s and back again; the air, the velocity SD and every label follow; grains stay grains. |
| 5 | Entry 131 section 10: Compare loads rebuilt | **done**: mean radius and sigma as dot-and-whisker charts with their intervals, under the side by side plots, with the chart saying in words whether they overlap; the side by side plots, shot counts and verdict lines were already there; and each card now carries the load's velocity and SD from the record book, in the units in force, saying where a measured SD came from. |
| 6 | Question 37's control: saying which bulls were aimed at, on the sheet | not started |
| 7 | Anything left from entry 131 sections 2 to 9, and a final checklist pass | not started |
| 8 | Entry 134, the installer icon | **done**, `ab3dbcf` |
| 9 | Entry 130 section 2b.1 (scan 1 bull 2) and scan 4's extra hole | **done**, `ab3dbcf` |
| 10 | Entry 130 section 2c and 6b: photographs against scans, the mounted gate | not started |
| 11 | Entry 130 section 6 (performance), then section 7 (guides and screenshots) | section 7 **done** `1a249be`; performance not started |

## Also owed, from Alan's message before this entry

| Item | State |
|---|---|
| The fixed sync on the server | **done 2026-09-22**: five files copied, SHA-256 matched against the repository, and Alan ran the dry run and the install. The old script is kept beside the new one as a timestamped `.bak`, and the timer is enabled and active |
| Question 38, the photographed hole size | **answered by measuring**: 176 holes, nine photographs, four sheets of known calibre. The ratio runs 0.90 to 1.45 sheet by sheet where the scans of those sheets read 0.76 to 0.95, and it is not resolution and not angle. No constant changed; the recommendation is the sheet's own marks, which every one of the thirteen sheets already produced |
| Entry 140, a new image is a new target | **done**: every section. `75ff2e8` (the reset and the setup offer), `3ae2de1` (the review queue), `78f6549` (New target and the unsaved question) |
| Entry 139, signing the bytes as published | **done bar section 5**: `28a3365`. The real update test runs once the nightly carrying it publishes |
| Entry 123 section 2.7: did the real update pass, and between which nightlies | **done**: yes, nightly 25 to nightly 26, real clicks, no installer window, no elevation prompt, sessions database byte identical |
| Questions 34 and 36 answered with a recommendation | not started |
| Entry 128 section 5 install and section 6 publish | install **done by Alan** and confirmed here; publish next |

## The first publish rolled itself back, and the live site is safe

The site was published at 01:16 UTC and the server pulled it at 19:25 local. Then this, in its own log:

```
the live check failed: the home page is not serving the new commit
rolled back: the site did not answer correctly after installing
```

**The safety worked.** grouplab.org is still serving the site it was serving before, and nothing is broken; the rollback is the sync doing exactly what it was built to do.

**Why it fired.** The install had worked: the files reach `/home/airwolf/web/grouplab.org/public_html`, the origin serves that directory, and hitting it directly the way the check does reaches the right site. The check runs the instant the directory is replaced, and it read the page the web server was still holding open, so it saw the old commit and called the install a failure.

**The fix, in the repository and not yet on the server.** The live check now asks up to five times, three seconds apart, and only rolls back when every attempt says the same thing. A single immediate read is not evidence that an install failed: a web server takes a moment to notice that the directory under it has been swapped, and rolling a good site back for that is the worse mistake. It still rolls back on a real failure, which is the point of it.

**This needs Alan**, because putting the fixed sync script on the server is the installer's job and that is a server change: `sudo python3 ~/grouplab-server/install.py` again, after the copy. Until then every publish will pull, install, fail its own check and roll back, leaving the live site as it is.

## The server install, done

**Alan ran it himself** after the session's own permission classifier refused the remote `sudo` command. Confirmed here by read-only checks: the timer is scheduled every 15 minutes and the log reads "nothing to do: the site release does not exist yet", with the live site untouched.

**He also found a real defect in it.** `install.py --dry-run` created `/var/lib/grouplab-site-sync`: the dry run said "would create" it and the real run afterwards said it "is there". Two faults met. The installer passed a hard-coded false where it meant its own dry run flag, so it really executed the sync's dry run; and the sync made its state folder on the way into `sync()` and its log folder inside `log()`, rather than when it had something to put in them. Both fixed in `65e07bc`, with two tests.

**Entry 136, the release notes page**, is built: `docs/RELEASE-NOTES.md` is the source of truth with all twelve published builds, `/releases/` is generated from it with one collapsible block per version and the newest open, the update bar's "Show all" opens the offered version's own block, and a test fails if the file falls behind the tags.

**Entry 137, drop and paste**, places itself after the entry 135 queue and entry 136.

## Done tonight, before this entry arrived

- All six range scans now match Alan's own counts: 15, 0, 25, 23, 20, 10. Two detection defects fixed, both found by reading the detector's own rejections.
- The calibre reading and the Accept gate reached the screen.
- The shot distance's unit became a real choice.
- Native libraries for platforms nobody runs are out of every build: Core tests 685 to 262 MB, App tests 705 to 278 MB, published package unchanged at 205 MB.
