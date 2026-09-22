# Overnight, 2026-09-22 into 2026-09-23

NOTES-FROM-PLANNING.md entry 141. This is the live state of tonight's queue: read at the start of every wake-up, updated after every commit.

**The printing deadline is lifted**: Alan has printed everything he needs for 2026-09-23, so there is no push freeze and no print report. The section 1.1 print tests stay, because they protect future printing.

**Next step:** entry 141 section 5.2, the graphics, and the rest of 5.3, with research batch 2 between items.

---

## The deadline

**Alan prints his targets from the newest nightly before he leaves, between 12:00 and 13:00 Mountain on 2026-09-23.** Entry 141 section 1: stop pushing to main at 07:30 Mountain (13:30 UTC), verify printing on the nightly that produces, and report by 09:00 Mountain (15:00 UTC).

**Printing is covered by tests that need no printer, as of the first commit tonight.** `PrintGateTests` renders every sheet in the library through `TargetRenderer`, which is the call `PrintWindow.SavePdf` makes, and then reads the PDF that came out: 88 checks over 22 sheets.

| what it holds | how |
|---|---|
| Every sheet renders, blank and filled, with no error | the diagnostics, and a page for every tile |
| The page is the size the sheet asks for | the `/MediaBox` read out of the PDF, letter being 612 by 792 points |
| The markers, the codes and the bulls are on the page | the scene's own layers |
| **The printed scale is exact** | the two furthest markers the definition places, measured back out of the PDF rasterised at 300 dpi by the same detector that reads a scan |

The scale check measures 8 to 10 inches and agrees to **0.0000 to 0.0006 in**, against the 0.005 in entry 141 section 1.1 asks for. It is not a test that can pass by doing nothing: a definition with no stored markers fails it by name.

## The queue

| # | Item | State |
|---|---|---|
| 1.1 | Print path covered by tests that need no printer | **done** |
| 1.2 to 1.5 | Stop pushing at 07:30 Mountain, verify the nightly, print report by 09:00 | not yet: the times have not come |
| 2 | CI: concurrency, a scheduled nightly, push main only | **done bar the proof**: `build and test` cancels an older run on the same branch, `nightly.yml` runs at 12:00 UTC on the newest commit main has passed and does nothing where it already has a nightly, and pushing goes to main alone. Section 2.4 wants a run of each path, which needs the schedule to fire |
| 3 | Finish what is queued | **done before tonight**: the website republish confirmed live, entry 139 section 5 proved from nightly 44 to 49, and the calibre guess on Alan's lists |
| 4 | Question 38 approved: the sheet's own marks are the reference | **done**: twelve or more round marks outrank a stated calibre, the quarter-point makes it robust to the doubles it is judging, and the thirteen images now flag 0 or 1 mark each where the photograph Alan met flagged 15 of 15. Question 40 raised where it meets entry 82 section 3 |
| 5 | The interface: type scale, graphics, editing shots and tying them to bulls | **5.1 done**: both scales held by a source test, eighteen off-scale gaps brought onto the ladder, and a measurement test for text running past the window edge at both sizes. **5.3.4 done**: which bulls you aimed at, with rows and columns presets, reachable from the shots per bull control, saying back what it was told. The end to end proof against entry 120 is not done. 5.2 and the rest of 5.3 not started |
| 6 | Entry 137, the rest of 131, entry 130 2c, 6b and 6, questions 34 and 36, entry 129 prepared | not started |
| 142 | The Research section: the page, the navigation link, the article format, the build and its checks | **done** |
| 142 | Research batch 1: articles 1, 3, 5 and 6 | **written and rendered, waiting for Alan's review.** Nothing published. `docs/RESEARCH.md` |
