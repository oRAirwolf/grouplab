# Overnight, 2026-09-22 into 2026-09-23

NOTES-FROM-PLANNING.md entry 141. This is the live state of tonight's queue: read at the start of every wake-up, updated after every commit.

**The printing deadline is lifted**: Alan has printed everything he needs for 2026-09-23, so there is no push freeze and no print report. The section 1.1 print tests stay, because they protect future printing.

**Next step:** questions 34 and 36 answered with a recommendation, then entry 129 prepared.

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

## Entry 141 section 5, in detail

| section | state |
|---|---|
| 5.1 type and spacing scales | **done**: both held by a source test, eighteen off-scale gaps brought onto the ladder, and a measurement test for text running past the window edge at both sizes |
| 5.2.1 the group plot | **done before this entry** |
| 5.2.2 across and up and down | **done**: two strips on one scale, with a caption that never calls a group lopsided without saying whether the shots can tell |
| 5.2.3 shot order | **done**: distance from the centre against the order fired, a permutation test, and a calibration test for how often it cries trend on random orders. It draws nothing where the order is not known |
| 5.2.4 sessions over time | **done**: one load's sessions as dots with their intervals and no trend line at all, the same permutation test asked of sessions, and no chart at all for a mixture of loads |
| 5.2.5 velocity | **done**: the readings drawn, the SD with its own chi-squared interval, the extreme spread with what it depends on, calibrated against 2000 simulated strings |
| 5.2.6 one set of charts | **done**: one colour convention for every chart, held by a source test rather than by looking |
| 5.3.1 one selection, three places | **done**: the review queue was the place that did not agree, and now marks every item about the selected shot |
| 5.3.2 move, add, delete, undo | **already built before this entry** |
| 5.3.3 assign by picker, keyboard, drag; several at once | picker **done**; keyboard already built; several at once **done** by ticking rows, as one undo step; dragging onto a bull is **question 41** |
| 5.3.4 which bulls were aimed at | **done**: rows and columns presets, reachable from the shots per bull control. The end to end proof against entry 120's ground truth is **not** done: it needs a registered sheet |
| 5.3.5 hand edits are never overwritten | done for re-assignment; **re-detection is question 42** |
| 5.3.6 everything updates on every edit | **already built**: `Refresh` recomputes the analysis and rebuilds every figure on each change |
| 5.3.7 a review item opens its shot | **already built**: `FocusReview` selects it and centres the view on it |

## The queue

| # | Item | State |
|---|---|---|
| 1.1 | Print path covered by tests that need no printer | **done** |
| 1.2 to 1.5 | Stop pushing at 07:30 Mountain, verify the nightly, print report by 09:00 | not yet: the times have not come |
| 2 | CI: concurrency, a scheduled nightly, push main only | **done bar the proof**: `build and test` cancels an older run on the same branch, `nightly.yml` runs at 12:00 UTC on the newest commit main has passed and does nothing where it already has a nightly, and pushing goes to main alone. Section 2.4 wants a run of each path, which needs the schedule to fire |
| 3 | Finish what is queued | **done before tonight**: the website republish confirmed live, entry 139 section 5 proved from nightly 44 to 49, and the calibre guess on Alan's lists |
| 4 | Question 38 approved: the sheet's own marks are the reference | **done**: twelve or more round marks outrank a stated calibre, the quarter-point makes it robust to the doubles it is judging, and the thirteen images now flag 0 or 1 mark each where the photograph Alan met flagged 15 of 15. Question 40 raised where it meets entry 82 section 3 |
| 5 | The interface: type scale, graphics, editing shots and tying them to bulls | **5.1, 5.2 and 5.3 are all done**, bar two questions and one proof. See the table below. |
| - | `docs/RELEASE-NOTES.md` caught up | **done**: nightlies 49 and 66 added from their `Release-note:` trailers. `ReleaseNotesTests` found it, not a person: the file had fallen two published builds behind, and a releases page missing its newest entry looks exactly like one nobody has updated |
| 6 | Entry 137, the rest of 131, entry 130 2c, 6b and 6, questions 34 and 36, entry 129 prepared | **entry 131 section 1's checklist pass done**: the Equipment form was the one real failure and is now two columns with fields sized to what they hold; the library list looked cut off and measurement says it is not. `NothingIsCutOffTests` now holds every word against the panel it is in, across five screens, and fails if it measured nothing. **entry 130 sections 2c and 6b item 1 done**: every photograph paired with its own scan, hole agreement measured, and scan 2 found to be unreadable so the 14:14 burst has no truth to pair with. A hole in a photograph is out by about 0.03 in at the median against a 0.17 in mean radius. **entry 130 section 6 item 1 done**: the application decoded the same file three times; one read and one decode saved, and the third saving refused because the test proved it would change the image. **the earlier 6b record**: the mounted photograph gate has its first real record. 28 of 59 photographs could not be read at all, 27 of them because no code on the sheet could be read; of the 31 that registered, 18 are inside the 0.005 in gate at the median bull and 1 at the worst. Written into `DESIGN.md` [r10]. Items 2 and 3 want section 2c's pairing first. **entry 130 section 2c in part**: two sheets in one photograph are now counted, one is chosen by a written rule, and the summary and the trace both say which. Its four measurement items still need the range folder read. **entry 137 done**: drop an image on the window or paste one, both through the same call Open uses, with the clipboard behind `IOutsideWorld` and a second guard in `OneWayOutTests` so only one file may touch a real one. Question 43 raised where entry 137 names an image safety the desktop does not have. The rest not started |
| 142 | The Research section: the page, the navigation link, the article format, the build and its checks | **done** |
| 142 | Research batch 1: articles 1, 3, 5 and 6 | **written and rendered, waiting for Alan's review.** Nothing published. `docs/RESEARCH.md` |
| 142 | Research batch 2: articles 2, 12, 17, 18, 19 and 20 | **written and rendered 2026-09-22**, waiting with batch 1. Nothing published |
| - | Entry 131, all sections | **done 2026-09-22.** Its status line had said eight were undone; six had been built under entries 135 and 141 without it being corrected, and two were already described as done three lines below it |
