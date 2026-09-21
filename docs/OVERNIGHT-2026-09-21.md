# Overnight, 2026-09-21 into 2026-09-22

NOTES-FROM-PLANNING.md entry 135 section 0.1. This is the live state of tonight's queue. It is read at the start of every wake-up, updated after every item, and committed with the work.

**Next step:** entry 131 section 1, the analysis page. Take the before renders at the two sizes entry 131 asks for, then rebuild the right-hand panel from `AnalysisPanel`, which exists and is tested and which no screen uses yet.

---

## The queue

| # | Item | State |
|---|---|---|
| 1 | Entry 131 section 1: before and after renders of every screen, checked against the checklist and fixed where they fail. Analysis page first. | **in progress** |
| 2 | Entry 131 section 6.2: the zero offset picture | not started |
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
| Entry 128 section 5 install and section 6 publish | **blocked**, see below |

## Blocked, and why

**The entry 128 server install.** Alan approved the four SSH commands and the copy went through; the remote `sudo` command was then refused by this session's own permission classifier, not by Alan. Entry 135 section 0.4 forbids SSH and server changes tonight in any case, so it stays where it is. It needs either a permission rule for that command or a turn where the classifier allows it.

**Entry 136, the release notes page**, ends in a site publish, and the site cannot deploy until that install has run. Folded with its status rather than written blind.

**Entry 137, drop and paste**, places itself after the entry 135 queue and entry 136.

## Done tonight, before this entry arrived

- All six range scans now match Alan's own counts: 15, 0, 25, 23, 20, 10. Two detection defects fixed, both found by reading the detector's own rejections.
- The calibre reading and the Accept gate reached the screen.
- The shot distance's unit became a real choice.
- Native libraries for platforms nobody runs are out of every build: Core tests 685 to 262 MB, App tests 705 to 278 MB, published package unchanged at 205 MB.
