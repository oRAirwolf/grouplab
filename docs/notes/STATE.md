# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-28, after entry 245.

---

## In flight

- Done: everything through entry 244, the server sitting included, apart from what the status lines name: device measurements and
  tablet screenshots on request 45 (238 to 240, 242, 243 section 3.3), the phone's look on request 49
  (243 section 3.5), the website generator (question 62 (b), later). 244: the README is generated in part and held by CI.
- **Entry 243 built:** pooling a set's sheets, progress and Cancel everywhere, the phone's Targets screen and side by side on big screens,
  E and C bulls beside the usual one (C a diamond standing on a point: the format's first square, rules 20a and 20b), and the large
  format sheets as 2 by 2 Letter and A4 sets (originals frozen in `targets/frozen/large-format-1`; question 63).
- **The zeroing grids were redrawn** (entry 226, grid style 2, question 59): plus or minus 1.0 mil at 100 yd exactly, 0.25 mil and
  0.5 MOA squares, the whole unit heaviest, labels, the scale and a ruler printed. The old four are frozen in `targets/frozen/zero-grid-1`.
  **Every published build before this one cannot read a style 2 frame**; the old sheets still read in the new build.
- **The target generator** is on the Targets screen ("Made for your optic"), on the phone too, disc or diamond; a set of sheets is a
  tiled assembly, and Session records pools its sheets ("Pool the chosen").
- **A target GroupLab did not print** can have bulls placed by hand, a lasso, templates, and a scale at each bull (entry 228,
  Unholy's); Android has no hand marking yet.
- **A sheet whose shots all landed off by the same amount** is assigned by that amount when the solver is certain (entry 229): the
  6.5 sheet's shots all go to their own bulls. Copies of one design stay separate sessions with labels of their own.
- **The suppressor article is published** (`suppressor-shift`): Magnus S 0.28 in lower than Dominus K, p = 0.049.
- **Where a hole's centre is**, question 51: waits on request 9's hand markings.
- **Storage on GitHub**: `docs/notes/STORAGE.md`. Submissions leave the server only once archived and proven (entries 215 to 217).
- **Minimums** are in PLATFORM-SUPPORT.md (entry 207): Android 10, 4 GB; the survey (`docs/SURVEY.md`) is open since entry 223.
- **Android**: the app `org.grouplab.app`, signed in every nightly since 110; detection runs on the Fold 7. Now ships the frozen
  definitions too. **A public Play listing waits on the attorney's review of the GPL app store permission.**
- **Both devices are off adb until request 45**; GroupLab Dev (`org.grouplab.app.dev`) is built by the nightly from entry 234.
- **The Fold 7 and the Tab S8 Ultra run nightly 111**, side-loaded (entries 235, 236); the tablet only for layout work.
- **The Play internal test ran on the Fold 7** (entry 232). Alan installed it from Play too (entry 231); `docs/ANDROID.md` section 12
  has the link, the uninstall-first rule and the symbols zip each nightly now makes. Automatic Play upload waits on request 38.
- **The survey page is live** (entry 241): grouplab.org/survey/, its worker installed on 2026-09-27; counting restarted under the
  keyed hash, so the everyone-else half is empty until reports arrive. The workers' time limits are in force (TimeoutStartSec).
- **Sending targets, error reports and the survey are on** (entries 195, 200, 223); crash issues are read at every start.
- **A receiver counts as live only when an empty POST to it returns its own error from the live site** (entry 195).

## Next

- **The benchmark is offered after Yes** and runs with progress and a Cancel, on the desktop and the phone (entry 227); **CEP 99** and a
  percent of one's own are on the analysis screen. Next **228** (several bulls and a scale at each on other people's targets, Unholy's), **229** (duplicate identifiers,
  the whole-sheet wrong-bull assignment on the 6.5 sheet, spelling) and **230** (the Oracle backup recorded, sudo widened).
- **Backups (entries 222 to 235)**: nightly to `grouplab-backups`, weekly restore test; Oracle: daily incremental, first Full on
  2026-09-27; the weekly Full moved to Sunday 12:00 UTC, first due 2026-10-04 (request 46). Sudo is not limited to GroupLab (230).
- **The Microsoft Store**: MSIX built in CI; tagged releases go to the Store by themselves once request 38 is done.

## The roadmap (entry 219), in place of the next three

- A1, A3, A4, A5 built; A2 the capture screen's measurement is request 33; **A6 built** and on Play's internal test; A7 the older phones.
- D1 the survey: built and open. D2 question 51 when request 9 arrives. D4 done (`docs/RELEASE-PLAN.md`, request 37).
- Waiting on requests: Program A (entry 158) on request 19's ST-4 scan; Program B's article on request 20's test.

## Blocked, and on what

- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 sections 3.2 and 5.** Request 16: the Mac tester's measurement and his name for a thanks.

Open requests in `docs/notes/for-alan.md`: **9** (45 reconnect the Fold 7 and the tablet; 49 choose the phone's look, A, B or C;
46 one look at the backups on 4 October; 38 the Store; 44 one line off the Fold 7; 33 the Fold 7's camera; then 9, 16 and 20).

## Open questions

Five, all in `docs/QUESTIONS-FOR-PLANNING.md`; 57 to 63 were answered by entries 243 and 245.

- **51** which hole centre GroupLab should report; agreed to wait on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's center means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.114, from 08dca80: entries 243 and 244, the C bull, and the phone's close report (error issue 6, closed).
- **The site serves the newest commit that touched it.** Notes commits do not start the site workflow; a publish is started by hand.
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ. The planning session's files are not committed, so while a run is working through them the
line reads what the repository holds, and the test fails locally until the last is done.

**Holds:** 246, 247, 248, 249

## Things that would surprise somebody who was not here yesterday

- **A disc can be a square** (entry 243 section 4): its diameter is the diagonal, and a square is measured as itself, not as the
  circle through its points, by the marker drop test, the validator and the edge fit. Older builds refuse a C sheet rather than misread it.
- **A QR code's bytes can arrive as UTF-8** from OpenCV; the reader now takes the reading that can be right (entry 243 section 4).
- **A zeroing grid is now drawn by its style**: style 2's strokes, labels, statement and ruler are fixed by `GridStyle2`, and a
  label breaks the line behind it on purpose.
- **Inbox files are moved to `C:\Dev\grouplab-trash\<date>\`**, not deleted, since the backup rule of entry 222.
- **An error GroupLab survives is no longer called a close** (entry 192); each run leaves a marker so a real close is caught.
- **A size is an angle first** wherever the distance is known, the size on the paper beneath (entry 189).
- **The upload page asks for one of two consent levels**, and a testing only target can never reach `samples/` or the site (entry 165).
- **A printed grid registers a target GroupLab did not print** (`GridRegistration`, entry 158).
- **A photograph over 37 degrees off square is refused** (was 40; entry 238's angled photographs), naming the angle, and keeps its angle and a quality score (entry 157).
- **Every word a shooter may not know explains itself**, in the app and on the site, from `glossary.json` (entry 154).
- **Every published sentence has its backing**: `scripts/claims.py --check` fails CI otherwise (entry 159).
- **Publishing an article is a decision** recorded in `website/research/PUBLISHED.md`; `ready` means finished and not live.
- **Everything a user reads is in American spelling**, and a test holds it.
- **A scan reports real inches.** A photograph stays in the sheet's own inches and says so.
- **Temporary files clean themselves up.** Tests write into one folder per run, CI fails on a leak.
- **Nothing under `website/server/` may hold a carriage return**: it is copied to Linux as it is.
- **Nothing is written into a HestiaCP `conf/web/<domain>/` folder** but the include itself.
- **Alan's own photographs and scans may be published**, and so may what he passes on from Unholy (also TNA) and his other friends;
  the 2026-09-16 friend scan never is. Justin is credited as "Justin" only.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel.
