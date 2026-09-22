# Overnight, 2026-09-21 into 2026-09-22

NOTES-FROM-PLANNING.md entry 135 section 0.1. This is the live state of tonight's queue. It is read at the start of every wake-up, updated after every item, and committed with the work.

**Next step:** publish the site once CI is green (entry 128 section 6 and entry 136 section 3 together, since the release notes page is built), confirm it is live, then the Equipment screen. The analysis panel still needs rebuilding from `AnalysisPanel`, which exists and is tested and which no screen uses yet.

---

## The queue

| # | Item | State |
|---|---|---|
| 1 | Entry 131 section 1: before and after renders of every screen, checked against the checklist and fixed where they fail. Analysis page first. | **in progress**: renders now taken at 1280 by 720 and 2560 by 1440, before and after kept under `docs/figures/screens/`. First pass on the analysis page: the mean radius is the one figure in the logo's amber, and the zero correction's direction word no longer runs off the edge. The panel rebuild from `AnalysisPanel` is still to do. |
| 2 | Entry 131 section 6.2: the zero offset picture | **done**: `ZeroOffsetPicture`, behind a "Where it landed" disclosure on the zero block. The aim as a cross, the group's centre with its uncertainty ellipse, and an arrow an axis pointing the way a shot has to move, with the clicks on it. Where the uncertainty covers the aim there are no arrows, because an arrow is an instruction and there is nothing to instruct. |
| 3 | Entry 131 section 7: the Equipment screen, with the old "rounds or components" box gone | not started |
| 4 | Entry 131 section 8: the ballistics page rebuilt | not started |
| 5 | Entry 131 section 10: Compare loads rebuilt | not started |
| 6 | Question 37's control: saying which bulls were aimed at, on the sheet | not started |
| 7 | Anything left from entry 131 sections 2 to 9, and a final checklist pass | not started |
| 8 | Entry 134, the installer icon | **done**, `ab3dbcf` |
| 9 | Entry 130 section 2b.1 (scan 1 bull 2) and scan 4's extra hole | **done**, `ab3dbcf` |
| 10 | Entry 130 section 2c and 6b: photographs against scans, the mounted gate | not started |
| 11 | Entry 130 section 6 (performance), then section 7 (guides and screenshots) | section 7 **done** `1a249be`; performance not started |

## Also owed, from Alan's message before this entry

| Item | State |
|---|---|
| Entry 123 section 2.7: did the real update pass, and between which nightlies | **done**: yes, nightly 25 to nightly 26, real clicks, no installer window, no elevation prompt, sessions database byte identical |
| Questions 34 and 36 answered with a recommendation | not started |
| Entry 128 section 5 install and section 6 publish | install **done by Alan** and confirmed here; publish next |

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
