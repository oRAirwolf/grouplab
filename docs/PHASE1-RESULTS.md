# Phase 1 results

**Brief** `docs/PHASE1-BRIEF.md`
**Branch** `phase-1`
**Reproduce** each table with the command named above it

---

## Where the Phase 1 gates stand

Stated plainly, `docs/NOTES-FROM-PLANNING.md` entry 33 section 5, so that "not yet measured" is never read as "passed". The gates are `docs/PHASE1-BRIEF.md` section 2's.

| Gate | Threshold | Status |
|---|---|---|
| Conformance test 43 | 0.001 in worst bull on a synthetic raster | **Met**, and checked on every test run. Must not regress |
| Paper gate | 0.005 in worst bull on the ten printed sheets | **Met.** Ten of ten through the selected model, worst bull 0.00319 in (M1.5) |
| Photograph gate, flat | 0.005 in worst bull on `main_flat1-3` | **Not met.** The flat frames nearly pass: the frame that decoded every marker is inside on every scoring bull, and the failures are named (M1.11) |
| Photograph gate, mounted | 0.005 in worst bull on the seven usable pinned frames | **Not met: 0 of 7**, and an open requirement (M1.11). It cannot be settled until mounted GroupLab sheets are photographed, which is this weekend's paper session |
| Hole detection, point 1 | At least 25 of 27 on `300_nm_hand_load` with zero false positives | **Met,** as a reproduction of the verified survey result rather than a fresh position check (M2.1) |
| Hole detection, point 2 | 99 percent of holes with no false positives on synthetic sheets, matched at 0.15 in | **Not met,** under the amended tolerance too (M2.2) |
| Hole detection, point 3 | Centre accuracy against truth | Reported, not gated |
| Statistics | `docs/STATISTICS.md` section 15.5 | **Met on points 1 and 3 to 6**: 45,476 keys compared with nothing pending ("Entry 28"), coverage 94.73 percent, the Monte Carlo table within tolerance on all 490 cells. **Point 2 is met on the shots and not on the series**, because the two fixtures group shot 242 differently |

**Not a brief gate, and new:** the whole path from image to group now runs as one command and matches synthetic truth ("Entry 33" below).

**Not a brief gate, measured for the first time: the Phase 0 gate record on Linux and macOS**, entry 32 section 3.
- **Windows** reproduces it byte for byte.
- **Linux** reproduces every table, and its records differ only below the precision any table reports.
- **macOS** reproduces the paper and photograph gate tables, and differs in measurements 1 and 2.
- **Linux: explained,** on entry 48 section 2's three terms. **macOS: not yet explained,** and localised to S2 ("Entry 48" below).

---

## What is not done, and why

| section | what | why |
|---|---|---|
| 4.3, 4.4, 4.5 | the server purge, deletion as part of the intake run, and the backup question | all need SSH; the backup question is a read on Alan's list |
| 6.1, second half | deleting the six from the old server | SSH, after the install |
| 6.2 | the redirect | SSH, and only after the new page is live and tested |
| 8.2 | one real test submission through the live page, and one real crash report | the page is not live until the install has run |

## Entry 461 part 1: every research row that said "not yet" decided (2026-10-21)

Ten rows of `docs/RESEARCH.md`'s "Worth an article?", each checked for whether its awaited data now exists:

- **Written and published (two new articles and one update):** `/research/recognizing-store-bought-targets/` (entry 332 and question
  87, decided on what exists as entry 341 says: the scale is measured only against the learned sheet, and the article shows the
  warning a shooter checks; no maker's artwork); `/research/opencv-on-android-from-dotnet/` (entry 198: the build, its two traps and
  the Fold 7's first run); and two new sections of `/research/wrong-bull/` (entries 374 and 376 Part A: a row higher fits exactly as
  well, the shooting order breaks the tie, and a real tie is asked). Both new articles have a drawn thumbnail, are listed in
  `website/research/PUBLISHED.md`, and are backed by rules in `docs/claims-backing.json`; the research card's sentence is backed alone.
- **Not written, with the reason in the row:** entry 410 section 1 (already decided twice since, entries 415 and 434), entry 362
  (five photographs on one counter; request 74 never came), entry 261 (still synthetic), entries 196 and 197 (synthetic; the only real
  sheet is the orange commercial target), entry 344 (no real photo of a real poster), and entry 158 program A step 2 (the orange
  commercial target, and a scan detector on photographs).
- Checks: the site builds; `claims.py --check` all backed; consistency 0 findings; README checked.

## Entry 461 part 2.1: screen reader audits on all three platforms (2026-10-21)

- **The computer** (`tests/Shared/ScreenReaderAudit.cs`): Avalonia hands Windows UI Automation its automation peers, so the peer tree
  is what Narrator reads. Every window the entry 447 tests tab round (the eight places, the report, the printer check and the six
  windows of `Entry447WindowsTests`) is walked: every control a person presses, types into or chooses, shown and enabled, must be in
  the control view (not Raw), carry a name in words, a control type that says what it is, and be reached by Tab (a stop, or inside a
  stop that takes it with the arrows). `Entry461AccessibilityTests` holds the audit to a window made to fail it.
- **What it found and was fixed:** a button or choice holding a panel was named by Avalonia after the panel's type, so Narrator read
  the printer check's four ways to print as "Avalonia.Controls.StackPanel" and three marking buttons as "Avalonia.Controls.DockPanel";
  the old tab round missed it because the name was not empty. `SpokenNames` (desktop and phone) names each such control from the words
  inside it unless the code named it, and keeps it current when the content changes; a type-shaped name now counts as no name in both
  checks. On the phone, Settings' "Run the benchmark now" was 44 tall, now 48 (`BenchmarkPanel`'s `runHeight`).
- **The phones, headless** (`Entry461PhoneAccessibilityTests`): the first run and every place, the same walk with a 48 by 48 touch
  target: no other finding.
- **The phones, on the platforms' own trees:** a scenario step `audit` (`Scenario.Audit`) stops on each of the sweep's 13 screens
  (`scripts/scenarios/phone-audit.json`, held to the sweep by `ScreenReaderAuditTests`); `scripts/screen-reader-audit.py` reads
  Android's accessibility node tree with uiautomator (every node that can be tapped named, with a role, 48 by 48 dp, and every control
  GroupLab Dev says can be tapped present in the tree) and runs XCTest's `performAccessibilityAudit` through the XCUITest runner
  (element detection, hit regions, descriptions and traits judged; contrast, Dynamic Type and clipped text kept in the artifact). New
  steps "Screen reader audit" in android-emulator.yml (`scripts/android-audit.sh`) and ios-app.yml.
- **The first runs (39539794514, 39539797209).** Android read all 13 screens: every node named, every role given, and two findings
  that are GroupLab's, "More ▸" 47 dp wide (now at least as wide as tall, `MoreFold`) and the bull buttons of "How you shot this sheet"
  44 dp tall (now 48, their gap 1 where it was 3, so the grid keeps its height). The rest was one fault: **every control's box in
  Android's accessibility tree sat 128 pixels, the status bar, above where it is drawn**, because Avalonia 12.1.2's helper sets each
  box on the screen from the control's place in GroupLab's view and never adds where the view is (it calls `SetBoundsInScreen` and never
  `GetLocationOnScreen`); TalkBack's outline was drawn above what it read, and Android cut the top and bottom controls to slivers.
  `AccessibilityOnScreen` wraps Avalonia's helper on the Android head, answering exactly as it does and moving each box by the view's
  place before Android clips it. A scroll bar's own arrows are left out of the judging, as on the computer. iOS: the runner did not
  build, because XCTest has no audit types for actions or parent and child; they were taken out.
- **The second runs (39544356145, 39544358247).** Android: the wrapper installed (`accessibility.bounds installed=True` in GroupLab Dev's
  log) and the 100 misplaced boxes were gone; the three left were controls cut off by the bottom of the scrolled view, whose box is the
  sliver showing, now not judged for size, as the Accessibility Test Framework does not. iOS read all 13 screens: 20 issues. Fixed:
  empty lines of text (a status line with nothing to say yet) were read as elements with no description, now kept out of the tree while
  empty (`SpokenNames.Quiet`); "grouplab.org" alone was not a name a person can say, now "GroupLab's website, grouplab.org". Not
  judged, kept in the artifact: the scroll bar, a 16 point strip down the right edge, and text XCTest found inside the photograph of
  the sheet on Fix holes (its handwriting and printed labels), which no label can represent and the words above it say.
- Request 105 in for-alan.md: a later twenty minutes with a real screen reader, for what a machine cannot judge. The guide's
  keyboard part says what is checked and that no person using a screen reader has tried it yet.

## Entry 461 part 2.2: a phone photograph's codes, measured (2026-10-21)

- The bench's photograph reads its codes square on in 662 to 861 ms (`grouplab analyze`, three fresh runs): the marker search 68 to
  92 ms, placing and cutting out the 8 places about 75, and about 450 ms in OpenCV's two readers failing in full on the 6 places with no
  code (WeChat 44 to 68 ms each, the second reader 5 to 35), where the other sheets sharing the frozen sheet's markers print theirs.
- **Nothing changed:** a failing read is the reader's fixed sequence, and reading fewer places changes how many codes a picture is said
  to have read (entry 415's finding). Question 106 puts the one real option to planning, with what it would cost; the bench baseline is
  unchanged. Detail in docs/PERFORMANCE.md. QR codes are still read on one thread.
- Worth an article? Not written: ordinary engineering, and the table is in PERFORMANCE.md.

## Entry 461 part 3: synchronization through the person's own cloud folder, option A, the engine (2026-10-21)

- **Decided:** option A of docs/notes/sync-options.md (Alan, 2026-10-20); recorded there, in DESIGN.md section 18 and the decision log.
  Option B is not decided and not built.
- **Revisions (3.1 item 1):** schema 6 adds `sync_records`, `sync_seen`, `sync_conflicts` and `sync_kept` (docs/SESSION-SCHEMA.md); the
  first open of the new build copies `grouplab.db` to `grouplab.pre-v5.db` before upgrading, as every schema change does. Each record
  the export carries has a version vector (`VersionVector`); a device finds its own changes by each record's SHA-256 at every run.
- **Change files (item 2):** whole records with their identity (`DataExport.Identity` for a session) and revision, in
  `GroupLab sync/changes/<device>/`, written only by that device, beside their name and renamed into place, at most 25 sessions a file,
  ending `"end": true`.
- **Reading and merging (item 3):** `SyncEngine.Run`, at start and every five minutes once a screen calls it (`SyncEngine.Every`);
  newer wins, the same record changed on two devices is kept both ways and listed, a deletion against a change keeps the change and
  lists it on both, deletion markers kept 90 days in the database and always in the change files. `SyncEngine.Resolve`: keep this
  one, keep the other, keep both (sessions), understood. Every record a merge replaces or takes away is kept whole first (`Kept`).
- **Tiers (item 4):** geometry and the proof picture always; the originals only with `SyncOptions.Originals`, to
  `GroupLab sync/originals/<sha256>.<ext>`; the photograph's path on a device never travels.
- **Reaching the folder (item 5):** `DirectorySyncFolder` (the computer, and iOS once a bookmark has opened it), `SafSyncFolder`
  (Android's folder picker, the Storage Access Framework, with its permission kept), `BookmarkSyncFolder` (a security-scoped bookmark
  to a folder in Files). The two phone ones build in CI and are reached by nothing.
- **Tests (item 6):** `SyncEngineTests`, twelve: two devices (a session with its strings, a rifle, a sheet, a setting), three devices,
  the same session changed on two (listed both sides, chosen, settled everywhere), keep both, a deletion against an edit, a half-written
  change file read once whole, a device back after 200 days (the deletion still reaches it), a folder that disappears (nothing changes),
  nothing lost (a load and a sheet kept as they were), originals only when asked, each device only its own files.
- **`grouplab sync <database> <folder>`**, with `--list` and `--choose`, for testing. Nothing on any screen, setting or menu; the
  README and the site say nothing of sync.
- **Measured for the screens (3.2):** on this computer's 22 sessions, a proof picture's median is 226 KB, a marking's 38 KB and a
  design's 7 KB: about 350 KB a target in the folder, 70 MB for a year of 200. The DESIGN NEEDED in for-alan.md carries the figures.
- **Not built:** tidying old change files; the three screens (DESIGN NEEDED).

## Entry 462 section 0: question 106 answered, a photograph's codes left as they are (2026-10-21)

Alan chose (b): every place on a photograph is still read, so a picture showing two sheets is still caught. Question 106 is archived
as answered in `docs/notes/archive/questions-answered.md`, the measurement stays in `docs/PERFORMANCE.md` with the answer under it, and
Phase 9 has no next step queued. Nothing in the application changed.

## Entry 462 section 1: a full audit of grouplab.org (2026-10-21)

**How.** `scripts/site-audit.cjs`, new, opens every page the builder makes (79) in Chrome at 390 and 1440 px in both themes and reports
what a reader would hit: the page wider than the screen, words cut off inside a box, words drawn over words, a picture or file that does
not load, a link to a page that is not built, and every axe-core finding for WCAG 2.1 A and AA. Anything not on the site is refused, so a
slow outside server cannot hold a page. External links were fetched one by one (165, all answer) and the download page's ten files were
compared with the rolling nightly's assets (all there; nightly 214 named). Lighthouse 12.2.1 ran on six pages.

**Found and fixed.** Eighteen research articles, What can be measured and the hole detection deep dive were wider than a phone (file names in sources and
code did not wrap); two research articles offered JSON downloads the builder never copied (suppressor shift, photo hole size); the upload
page's file field had no label; the tour diagram marked its chosen link "pressed", which a link cannot be (now "current"); ten tables and
one listing scrolled sideways with no way to reach them by keyboard (each that scrolls now takes focus); the tour's cards skipped from the
page title to a third level heading; a table caption on the hole detection page shrank to a word per line on a phone; two small lines
under 4.5 to 1 now use the theme's existing dim and on-amber colours. The tour's words were checked against its pictures: five pages fixed
(the key is bottom left, Compare's plots and figures, Fingerprint's five ways to give a length, the optic's "Save to your own sheets").
Features' platform lists were checked against PHONE-PARITY.md and about 30 of its on-screen names against the source; none wrong
(the demo tour does have thirteen steps). The release notes page names nightly 214; the research index has all 39 published articles,
each with its picture. `claims.py --check`, `consistency.py` (with the live site) find nothing; the stale consistency issue was closed.

**Waiting on a look decision** (for-alan.md, DESIGN NEEDED of entry 462 section 1, pictures in `docs/figures/site/entry-462/`): links
inside sentences in the dark theme (about 2 to 1 against the text, no underline; 101 links) and amber words on How it works' grey panels
in the light theme (4.19 to 1). `website/a11y-pending.json` lists them so the check prints them as waiting.

**Lighthouse, after the fixes** (local build, light): accessibility, best practices, SEO. Home 95, 100, 100; Features 100, 100, 100;
download 96, 100, 100; user guide 96, 100, 100; How many shots 96, 100, 100; tour 98 before its heading fix, 100, 100. Every remaining
accessibility point is the link finding above.

**In CI.** `checks.yml`'s website job now runs `site-audit.cjs --only a11y` (axe on every page, both themes, at phone width): 67 s here.

## Entry 462 section 2: three new research articles, written and published (2026-10-21)

Each meets entry 392 section 1: every number is worked out by GroupLab's own code and held by
`tests/GroupLab.Core.Tests/Ballistics/ArticleFiguresTests.cs`, whose simulations are seeded (2026) and rerun to the same values; every
checkable sentence is backed in `docs/claims-backing.json`; the developer is not named; the byline is the existing one; each has a drawn
thumbnail and is listed in `website/research/PUBLISHED.md`. No link to any site but grouplab.org and the two sources named.

1. **Does your rifle group as well as Top Gun says it should?** (`top-gun-and-your-groups`) One five-shot group is 0.67 to 1.36 times
   the true average five-shot group, 8 times in 10; GroupLab's interval on Top Gun's basis is 0.82 to 1.29 times from 20 shots; a rifle
   on the trend is called clearly wider 3 to 4 times in 100 at any size, and one 25 percent worse is caught 25, 35, 58, 73 and 90 percent
   of the time from 5, 10, 20, 30 and 50 shots. The book is cited by title and chapter only; nothing is quoted.
2. **What a canted scope costs at distance** (`canted-scope-at-distance`) For the point blank article's load, 2 degrees of cant moves the
   shot 0.8 in sideways at 300 yd and 12.5 in at 1000 while losing under a quarter inch of height; a 60 MOA scope reaches 910 yd on a flat
   rail and 1270 on a 20 MOA rail (`ScopeMount.Farthest`).
3. **When a click is not what it says: the sight scale factor** (`when-a-click-is-not-a-click`) Error sizes from Gun Digest (about 2
   percent typical, 5 a reason to return the scope) and The Firearm Blog (one scope at 110 percent); at 1000 yd a 2 percent error moves the
   center 6.3 in and the first-round chance on a 10 in circle falls from 36 to 20 percent (GroupLab's hit probability for the centred
   case, the same shots moved for the rest). Checking against the code changed one passage: GroupLab's test reads the turret with the
   reticle on the lines and fires no shots.

Candidate 4 found three investigations since entry 402 with no row: the screen reader audits, synchronization and the site audit, each
decided not written in RESEARCH.md with its reason.

## Entry 462 section 3: the interface sweep, faults fixed and looks sent to Alan (2026-10-21)

**How.** `tests/Shared/LayoutAudit.cs`, new, shared by the desktop and phone suites, walks a window's controls and reports words cut off
(trimmed with no tooltip, wider or taller than their box, or hidden by a box that clips and does not scroll that way), a control a person
uses past the window's edge, words drawn over words (a dialog's solid card or a tooltip over them excepted), and words under WCAG AA
contrast (4.5 to 1, 3 to 1 for large) with both colours named. On the desktop it runs in the every-screen walk (`Entry109Tests`) when
`GROUPLAB_LAYOUT_AUDIT` names a file: every screen at 1280x720, 1400x900, 1920x1080 and 2560x1440 in both themes; the weekly screenshot job
now fails on any finding, after committing the pictures. It also runs on every push beside the screen reader audit in the entry 447 window
tests. On the phone, `Entry462PhoneLayoutTests` lays out every place at 360, 412 and 600 wide, both themes, at the ordinary and the
largest text size (the iPhone's largest is held at Android's, so one pass covers both), headless, the same Avalonia code the emulator
runs. A chores-haiku walk of 55 pictures found what a machine cannot judge.

**First walk: 417 findings; after the fixes: none, desktop and phone.** Fixed: Ballistics' table headings cut off (they wrap), its
Elevation figure running over its label in a narrow panel (it goes under), its right panel's last lines under the status bar with no way
to scroll (it scrolls, the bar drawn in the padding so nothing narrows); the hit plot's key off its right edge and two long choices cut
off in their boxes (they wrap); equipment labels with a long unit and the flagged-marks heading cut off (they wrap); the shot table's
"center" cut off (its first column 52 px); every trimmed name (tabs, the path, session lists) now has its whole text as a tooltip
(`TrimmedTips`); the hint inside an empty box was the base theme's grey at half strength, about 2 to 1 (now whole, in the faint text
colour); faint notes inside an expander were 4.12 to 1 on its grey (now dim). **Failures as the centred warning** (Alan, 2026-10-02,
entry 356): fifteen save, print and open failures still said in a small line now also raise it: the point blank card, the scale marker
page, labels, check label and stickers, the session database at start, a marking or session that will not open, a session file, a
chronograph file (twice), a scale rectangle that cannot be used, and on the phone the M220 card and labels. **Same thing, same words:**
chronograph readings and the velocity block's step said "fps" where every other screen says "ft/s"; the phone's designer said "Distance,
yards" and the computer "Distance, yd"; the phone's Settings said "Sizes on the paper" for what the computer calls "Lengths". The
published Compare and Sessions pictures dated their second session 2099-01-01; it is now a week before. **Not checked by machine:** focus
visibility (entry 447 holds the tab order and names) and screens deeper than the phone's bottom places.

**Looks for Alan** (DESIGN NEEDED in for-alan.md, eight lines): marking labels stacking, the analysis key over the plot and unit-less
headings, the store-target question over the bull, the set tab's pill, the first run taller than 720, the demo card over its text,
Compare's half-empty card, and two phone pages.

## Entry 462 section 5: what is still done by hand, automated where nothing from Alan is needed (2026-10-21)

`docs/notes/AUTOMATION.md`, new: 24 recurring steps, who does each, how often, what already does it, and whether it can be automated
with nothing from Alan; the "handoff task" the entry names was not found anywhere and is left for the planning session to name.

**Built:** `scripts/run-start.py` does the start of every run (open requests, error reports, earlier scratch folders cleared by
clean-scratch.py's own rule, external status in a line a store, the inbox, the week's usage, whether the logs need splitting);
`scripts/site-live.py` asks grouplab.org's build tag once a minute until a commit is live or the time is up. With sections 1 and 3, the
site's accessibility and every screen's layout are now checked by machine. **Prepared, off:** `scripts/tester-notes.py` and a nightly
step post each new "For <tester>" note once its fix is published, opening "Hey <name>,", only once `DISCORD_TESTERS_WEBHOOK` exists
(request 107; the notes already in for-alan.md are recorded as passed on); `scripts/oracle-backups.py` and `oracle-backups.yml` list the
boot volume backups on Mondays with a read-only key and keep a `backups` issue while the newest Full is over eight days old or everything
expires within a week, only once the five `OCI_*` secrets exist (request 106, replacing request 46's look). Both are in RESTORE.md with
how to stop them. Self-tests pass; with no secrets both say so and do nothing. **Left by hand:** STATE, folding entries, release-note
wording, publishing articles, merging tuning pull requests and the planning session's relay are judgements; the whole-server restore test
needs more than read access.

## Entry 462 section 4: more pictures in the guides and on Features (2026-10-21)

**The user guide** has seven more pictures, five of them marked (entry 437's style), each from the walk's own renders: Scope and mount
(three fields marked), the zeroing grids (Open as PDF), Shots Needed to Zero (two controls), Point blank range (three), Walk me through a
demo (Next and End the tour, and the phone's demo), and the bar along the bottom at the largest text. Sections left without one say why
in the docs pass: the keyboard picture shows nothing a keyboard does, the optic picture does not show the optic's fields, and the
problem section already holds its one marked picture. The testing guide and the statistics primer never send the reader to a screen,
so they have none added. The guide PDF is regenerated.

**Features:** five entries that showed a general screen have their own picture, made by the screenshot walk: Conditions on the day
(`conditions`), Every shot, and which count (`shots-window`, a shot left out), Zero from this group (`zero-window`), A picture to share
(`share-window`) and Words explained where they appear (`word-explained`, Mean radius's explanation open). Each is in SOURCES.md,
screens.json and the analysis tour page's further pictures. The fourteen still waiting each say why in `docs/figures/SCREENSHOTS.md`
(mostly a click, a drag or a file the headless walk cannot make yet). Rendering the new Conditions picture found a fault the layout
audit names: the link to change the rifle's figures ran past a wide window's edge; it wraps now. The rest of the pictures are re-rendered
by the screenshot job from main (it runs when a screen's files change), so they show section 3's fixes; local renders matched CI's but for
the status line's clock. **Not done:** phone pictures beside the new Features entries, which need the emulator's walk.

## The archive

Older results, whole and unedited, banded by the entry they belong to. Nothing here is ever deleted.

- [`docs/notes/archive/results-026-050.md`](notes/archive/results-026-050.md), entries 026 to 050, 16 section(s).
- [`docs/notes/archive/results-051-075.md`](notes/archive/results-051-075.md), entries 051 to 075, 10 section(s).
- [`docs/notes/archive/results-076-100.md`](notes/archive/results-076-100.md), entries 076 to 100, 6 section(s).
- [`docs/notes/archive/results-101-125.md`](notes/archive/results-101-125.md), entries 101 to 125, 26 section(s).
- [`docs/notes/archive/results-126-150.md`](notes/archive/results-126-150.md), entries 126 to 150, 15 section(s).
- [`docs/notes/archive/results-151-175.md`](notes/archive/results-151-175.md), entries 151 to 175, 25 section(s).
- [`docs/notes/archive/results-176-200.md`](notes/archive/results-176-200.md), entries 176 to 200, 23 section(s).
- [`docs/notes/archive/results-201-225.md`](notes/archive/results-201-225.md), entries 201 to 225, 21 section(s).
- [`docs/notes/archive/results-226-250.md`](notes/archive/results-226-250.md), entries 226 to 250, 25 section(s).
- [`docs/notes/archive/results-251-275.md`](notes/archive/results-251-275.md), entries 251 to 275, 27 section(s).
- [`docs/notes/archive/results-276-300.md`](notes/archive/results-276-300.md), entries 276 to 300, 23 section(s).
- [`docs/notes/archive/results-301-325.md`](notes/archive/results-301-325.md), entries 301 to 325, 15 section(s).
- [`docs/notes/archive/results-326-350.md`](notes/archive/results-326-350.md), entries 326 to 350, 17 section(s).
- [`docs/notes/archive/results-351-375.md`](notes/archive/results-351-375.md), entries 351 to 375, 21 section(s).
- [`docs/notes/archive/results-376-400.md`](notes/archive/results-376-400.md), entries 376 to 400, 30 section(s).
- [`docs/notes/archive/results-401-425.md`](notes/archive/results-401-425.md), entries 401 to 425, 26 section(s).
- [`docs/notes/archive/results-426-450.md`](notes/archive/results-426-450.md), entries 426 to 450, 26 section(s).
- [`docs/notes/archive/results-451-475.md`](notes/archive/results-451-475.md), entries 451 to 475, 11 section(s).
- [`docs/notes/archive/results-milestones.md`](notes/archive/results-milestones.md), the milestone work, before results were written per entry, 145 section(s).

## Entry 463: the three sync screens Alan chose (1A, 2A, 3B), and sync a person can reach (2026-10-21)

**Shared.** `src/GroupLab.Core/Sync/SyncScreens.cs` holds the three screens' words and figures for the computer and the phone alike: the
status line ("Synced 2 minutes ago: 3 changes in, 1 out."; the engine counts changes, not sessions, so the line says changes), the other
devices seen from the folder's device files, the side by side of a session changed on two devices (rows for holes with what was added,
taken away or moved, mean radius, rifle, barrel, load, caliber, distance and notes, each that differs marked), and the two cards' sizes,
now and a year at the person's pace (their own rate where there are ten sessions over sixty days, else 200 a year, and it says which).
`SyncEngine.Versions` finds a waiting session's two versions and `SyncEngine.Measure` what the records weigh. `SyncSetup` keeps this
device's folder, name, originals choice and last run in the settings, never synchronized itself.

**1A, the computer:** Settings has a "Sync between devices" section: the folder in full with Change folder, this device's name, the status
line with Sync now, the other devices with when each last wrote, the two cards, and Stop syncing with "Stops on this device only. Nothing
here or in the folder is deleted." It runs every five minutes while GroupLab is open and at once from Sync now. A folder that cannot be
reached is said at the top of the section in amber with Choose the folder again, and the first time in a run in the centred warning;
nothing is changed or deleted. **The phone:** the same items stacked on a Sync page under Settings; the folder through Android's folder
picker (`SafSyncFolder`) or a folder in Files on iPhone and iPad (`BookmarkSyncFolder`), both from entry 461, now reached through the
platform (`ChooseSyncFolder`, `WithSyncFolder`). **2A:** a centred dialog headed "This session was changed on two devices", the session's
load, sheet and date, "Both versions are kept until you choose.", two columns (each device's name and time, a small plot of its holes on
one scale, its rows), and Keep each device's, Keep both as two sessions and Decide later; on the phone the two stack. A waiting choice is
found again from a banner on Sessions and a "2 versions" tag on its row, on both. **3B:** the two cards with bars on one scale a row and
the line under them; the "Only on Wi-Fi" choice was not built.

**Reachable:** the user guide's Settings part has Sync between devices and When a session was changed on two devices, each with a marked
picture the walk renders (`sync-settings`, `sync-conflict`); Features has "Sync between your devices" with its own picture; the README and
the home page no longer say synchronization is not built; Phase 7 reads Built, not proven. Option B is not mentioned anywhere.

**Tests:** `SyncScreensTests` (Core, four), `Entry463Tests` (computer, three: a folder chosen, run and stopped with nothing deleted; a folder
gone said in the section and once in the middle; a session changed on two devices found on Sessions and kept both ways), and
`Entry463PhoneTests` (phone, three, the same, with the layout audit at a phone's width). The layout audit's overlap check now decides what
covers what from the tree rather than a hit test, which missed a dialog's card at 1280 by 720. The Android head builds here; the iPhone's
picker is proved by ios-app.yml on its next run. **Not done:** the phone's pictures of the Sync page and the side by side, which need the
emulator's walk; and sync is not proven on two real devices.

## Entry 466: request 107 answered "not for now" (2026-10-21)

Alan read drafts of what `scripts/tester-notes.py` would post and said "Lets just not do the notes for now", replacing the yes of
entry 464 section 0. Request 107 is closed and moved whole to `docs/notes/for-alan-archive.md` with that answer and the four
conditions for taking it up again; `for-alan.md` now counts 25 open. `scripts/tester-notes.py` and its step in nightly.yml stay as
built, switched off while `DISCORD_TESTERS_WEBHOOK` does not exist, and nobody is asked for it. `docs/notes/AUTOMATION.md` says so.
The "For ..." notes stay in `for-alan.md` for Alan to pass on by hand.

## Entry 465: the eleven interface looks Alan chose, built (2026-10-21)

Built by five helpers in their own worktrees under entry 467, merged and tested together here. Each item is its own commit.

| item | what a person sees now | held by |
|---|---|---|
| 1A Marking (645d83f3) | bull numbers outside the outer ring at the top left; hole tags just outside the ring, never on a tag, a bull number or a hole; the selected shot an open magenta ring with four ticks and a magenta tag; close holes on Any target fanned in a column on leader lines | `MarkTags.Place` (Core, shared by both); `MarkTagsTests`, `Entry465MarkingTests`, `Entry465PhoneMarkingTests` |
| 2A Analysis (95c9e91b) | the shots headings carry the unit chosen ("across, in"); the plot's key in a box under the plot, two columns where it fits | `Entry465Tests`, `Entry465PhoneTests` |
| 3B Store-bought (54c20722) | "Which target is this?" a bar along the bottom of the picture, the bull moved up whole above it; a bottom sheet on the phone | `StoreTargetScreenTests`, `StoreTargetPhoneTests` |
| 4B Set tab (4294d76e, c30b4505) | the Scale checked pill at the top of the side panel with one line saying how the scale was found, built only from what each sheet's scale records | `ScaleSaid`, `ScalePillTests`, `ScaleSaidTests` |
| 5A First run (eea0a1e7) | one question a step, "Question N of M", a step bar, Back, Next and Skip for now; every step fits 1280 by 720; every question's words compared with a recording taken before the change | `FirstRunSteps`, `FirstRunWordsBefore` (both suites) |
| 6A Demo tour (0742f96f, 30cb8ac7) | the card beside the outlined part with a pointer, the bottom of the window only where no side has room; on the phone the step's number moves clear of the card | `DemoCardPlacement`, `Entry465PhoneDemoTests`, the phone sweep |
| 7B Compare loads (06ccbfac) | with two loads, "What the difference means": the main figures side by side and GroupLab's existing sentence on whether these shots tell them apart and how many would | `LoadComparison.Compare`, `Entry465CompareTests` |
| 8A Phone, Scale markers (fd3e362d) | plain body text, a "What is this?" button by each marker's name; five new glossary entries; the Targets library rows and the optic card made plain too; Settings, Capture, Sessions, Ballistics and the figures left as they read well | `Entry465PhoneWordsTests` |
| 9A Phone, Targets at largest text (fd3e362d) | one field to a row, the label above, when a label would not fit half the width on one line | `Entry465PhoneWordsTests` |
| 10B Site links (3d5edaef) | a 1 px underline at 55% in sentences, full 2 px on hover and focus, in both themes: the light theme had the same fault on six links | `scripts/site-audit.cjs`, 82 pages, 0 findings |
| 11A Site, How it works (3d5edaef) | a white panel behind the amber numbers, stage labels and strips, passing 4.5 to 1 | `scripts/site-audit.cjs` |

**Merging.** Three helpers each named a new test file `Entry465PhoneTests` or `Entry465Tests`; the later ones became
`Entry465PhoneWordsTests`, `Entry465MarkingTests` and `Entry465PhoneMarkingTests`, and the backing names them. Put together, the phone
sweep found the tour's step number under the card's title on the result plot, which now fills the screen since its key moved under it;
fixed in 30cb8ac7. A temp folder reused across ended test runs carried a "did not exit" marker into the next run and put GroupLab's own
error bar on the first screen; a fresh folder cleared it, so nothing in the application was wrong there.

**Tests** on the merged head: Core 3112 passed (the two local-only failures were the inbox line, which CI sees empty, and one British
spelling in a site comment, fixed); App 624 passed before 5A, 6A, 3B and 4B were merged and 638 on the merged head (30cb8ac7); Mobile 219 passed with the sweep and demo tests passing after the fix. Claims 5726, all backed; README
current; site build ok. **Is it worth an article?** No: these are GroupLab's own screens, and nothing in them changes what another
shooter does or another developer builds.

## Entry 464: sync tidying, proofs, two articles, speed and an audit (2026-10-21)

Worked by four helpers under entry 467, then the audit on docs-sonnet, merged and tested here. Section 0 (request 107) was replaced by
entry 466 before it was started.

**1. Sync tidying (1a9850bb).** Each device writes the change files it has read, and the records waiting for a choice there, to
`GroupLab sync/read/<device>.json`, and that is the only basis for tidying. Once a day a device moves only its own files, and only one
that every other device lists as read, whose every record a later file of its own carries newer, and in which nothing waits for a
choice anywhere. The move copies the file whole to `GroupLab sync/trash/<device>/`, reads it back and compares it before the original
goes; the trash is emptied 14 days after the move, counted from the move. A device with no read position, or one that cannot be read,
holds everything back, and a write to a folder that has gone missing now refuses rather than making a new empty one. The Sync block on
the computer and the phone and `grouplab sync` say what was tidied and what it freed. Tests: two devices, one away a month; a choice
waiting; the folder going missing at six points; the 14 days; the phone's layout. Built, not proven, until two real devices run it.
Question 109: a device that never comes back holds tidying back for ever.

**2. Proofs (551d39cd).** Of the 27 rows still built, not proven in `docs/PROOF-CHECKLIST.md`, eleven moved to Done with their
evidence named: significance testing, subgroups, doubles, comparing photos, rounds fired, session records, the report, the target
library (a gate written for it), the Garmin Xero import (this computer only), load against load, and `grouplab bench` on its recorded
passes of entries 388, 415 and 433. The other sixteen each have a line saying the material that would prove them (a camera and a shot
sheet, a printer and paper, a range and a chronograph, a person timed, a phone sitting, Google Play). Nothing of entry 465's was moved.

**3. Articles (2ad2524f, 053746fe).** No row in "Worth an article?" still said not yet; every "not written" row's reason still holds,
and the aim-points draft still waits on request 86. Two pieces never decided passed the test and are published: "How much of your
vertical is your velocity?" and "Where a first-round miss comes from", each with seeded figures held by `ArticleFiguresTests`. The
canted-scope article's source line named a file that does not exist; it now names the ballistics code.

**4. Speed and the audit (c4217def, 3d15f53d, 4b255fc8).** Measured stage by stage from opening a picture to its analysis on the
computer and the phone's path (`docs/PERFORMANCE.md`): on a photograph the codes are the largest stage, as question 106 left them; on a
600 dpi scan, the holes and the PNG's decode. One gain taken: a JPEG's grey and colour copies are decoded side by side, opening a phone
photograph about a third sooner, held to the bit by `ImageLoaderSameResultTests`, and the corpus, synthetic scoreboard and counts are the
same line for line. A larger gain on the first picture after start-up, publishing precompiled, is question 110. The consistency audit
after entry 465 found one stale page (`docs/SURVEY.md` still described the first run as one screen), fixed; consistency 0 findings.

**Is it worth an article?** Sync tidying and the speed gain: no, ordinary engineering a shooter never sees. The proofs: no, a record.

## Entry 467: helpers in parallel until 20:00 Denver (2026-10-21)

Nine main-model helpers started at 19:55 UTC, each in its own worktree on a piece whose files did not overlap. **The five-hour window,
not the week, was the limit**: nine helpers took it from 14 to 33 percent in about twelve minutes, which would have spent it well
before its 22:10 reset and stopped everything. So the four entry 464 helpers were paused at once and the 2A and 7B helper stopped once
its work was merged, and resumed with their context when the window reset; `scripts/usage-statusline.js` now writes the five-hour
window's reset into `usage-now.json` so the next run can see it. Entry 465's five finished first, as entry 467 section 5 asks, then
464's four and the audit on docs-sonnet. The week went from 45 to 57 percent.

**Merging.** Each helper's commits were cherry-picked onto phase-1 in turn. The only real conflicts were the claims backing, resolved as
JSON (both sides kept), the register regenerated, and three test files given the same name by different helpers, renamed. The merged
tree's App suite is the test that counts, because no helper saw the others' work: it found the phone tour's badge under the card. Two
stopped helpers left shell processes running, ended by number; a temp folder reused across ended runs carried a "did not exit" marker
into the next run, so every run now starts from an empty one.

## Entry 468: Settings in eight categories, and six new settings (2026-10-22)

Alan found Settings "very cluttered" and chose concept A on the canvas, with every proposed new setting. The computer, Android and the
iPhone and iPad changed together; every setting kept its stored key and meaning.

**1. The structure (c4e1ab07, 8aae8700, c01a659e).** `SettingsMenu` and `SettingsPages` (`src/GroupLab.App/SettingsMenu.cs`, shared with the phones) hold the
eight categories, their one line, what each row is found by, and what putting a page back does. On the computer
(`MainWindow.SettingsMenu.cs`) the side menu is a column of buttons reached by Tab and moved through with Up and Down, each named to a
screen reader, with Find a setting above it, one page of cards at a time beside it, the status line under both, and the page shown last
remembered (`settingsPage`). On the phone (`SettingsView`) Settings opens the list of the eight with their current choices in one line,
the search box at the top, and each page with "‹ Settings" back (a page back, so Android's Back returns to the list). A search filters
the same controls in place on every page, so a choice made among the results is the one on its page. "Put this page back as it came"
asks first (the computer's centred question, the phone's inline one) and resets only that page's settings, never data, printers, copies or
sync. **Saving** (by itself or with a Save button) had no category in the entry; it sits on Sync and your data. The desktop's Printers
section moved from a long column to its own page; the phone's About split into Updates and about and Help and problems. The tests that
read the old long page now open the page they mean; the control bench, the keyboard sweep, the screen reader and layout audits walk all
eight pages (`Entry447Tests`, `DesktopSweepTests`, `ControlBenchTests`); the phone's scripted walks press the category first.

**2.1 Daily copies (c4e1ab07).** `RecordsCopies` (Core) makes a `VACUUM INTO` copy beside the database at the first open of a day, on by
default (`dailyCopies`), keeps the newest 14 in `copies/`, moves older ones to `trash/copies/` named by the day they moved, and removes
them 14 days later. Back up now, Restore a copy (each copy with its day, sessions and size) and, on the computer, Open the folder; a
restore copies the records as they are first, so it can be undone the same way. GroupLab then reads the restored records afresh in place
(the record book, Sessions and the library) rather than restarting the process: the session store holds no connection open, so nothing
of the old file survives, and the phones open the store each time anyway. `RecordsCopiesTests`: a copy made while sessions are being
written passes SQLite's integrity check, the restore round trip, the fourteen kept and the trash's fourteen days, and a full disk leaving
nothing half written with the records and the earlier copy byte for byte unchanged. `docs/RESTORE.md` has the section.

**2.2 to 2.6, the other new settings (7ce223ff, 4882f861).**
- **Shooting defaults:** the distance (last used, or always this one; stored `newTargetDistance`), and on the computer rounds fired and
  rifle and load. Today's computer behaviour for rounds fired is that it is asked for and marked until answered, so the default is named
  **Ask**, as the entry's rule that today's behaviour is the default requires; One a bull fills the count from the sheet's bulls once
  detection finds them, and Not known pre-answers it. Rounds fired and rifle and load are computer only, because the phone asks for
  neither per target; its page says so. **Paper** (Letter or A4, `paper`) is one setting shown on Shooting defaults and on Printers and
  paper; every sheet, report and printer check that chose by region now asks `AppSettingsStore.LetterPaper()`, which is the region's
  until one is chosen.
- **The camera (phone):** Starts in Guided or Manual (`cameraStarts`); until it is chosen the camera starts as it was last left, as it
  always has, which is Guided on a new install. The shutter sound is on unless turned off; the iPhone's AVFoundation makes its own sound for
  every picture, and Android 13 and later say where the law requires one, so there the switch is shown on and cannot be changed, with a line
  saying why.
- **The analysis opens with** the main figures or every figure (`analysisOpens`): every figure opens More figures, Advanced and the full
  CEP table on the computer and every folded section on the phone, without changing the fold the computer remembers.
- **Search and reset** as in section 1.
- **Text size on the computer** (Normal, Larger 1.15, Largest 1.3, `textSize`) through `TextSize.Choose`, the same path the phones use to
  follow their system size, so every token-sized style grows alike. `Entry468Tests.NothingIsCutOffAtAnyTextSize` runs the layout audit on
  the analysis, Sessions, Targets, Ballistics, Compare and all eight Settings pages at all three sizes: one finding at Largest, "Keep these
  on the records" on Ballistics two pixels past its column, fixed by letting its words wrap. The phones keep following the system and say
  so. **Reduce motion** (`reduceMotion`: follow the system, on, off) is read by `Landing.Still()`, which the landing flash and both phones'
  camera flash now use; the camera's flash becomes a still tint held a moment.

**Tests.** Core 7 new (`RecordsCopiesTests`), App 10 new and Mobile 9 new (`Entry468Tests`). CI on 4882f861 is green on all three
systems (Core 3138, App 649, Mobile 231). **What the pushes found.** The iPhone app's data round trip on ios-app.yml found that a
`.grouplab` file opened from another app no longer showed its import plan: the hand-off opened Settings at its list, and
`Shell.ShowSettings` built Settings twice so the first copy took the waiting file (8aae8700, c01a659e, with a test). Then the App suite
took 48 minutes on macOS and 72 on Linux, where the job ran out of its 90 before the Mobile suite: every window asked
`TextSize.Choose` for Normal and each call rebuilt the whole application's styles; it now does nothing when the size has not changed
(4882f861), and the App suite went back to 21 minutes on macOS and 30 on Linux. The iPhone app's own screen-reader audit has timed out on Apple's side on the result
screen in the last three runs (once on Targets as well), while Settings has no findings; the result screen took 67 s against 36 s in
the last green run with every screen slower, so it is recorded here, not changed blind. Locally the two test-host stops in the full
App run were the 10-minute hang limit dumping the screenshot walk, which takes 10.5 minutes, not crashes.

**Documentation.** The guide's section 12 rewritten around the eight pages with a section for the daily copies, and its other references
to Settings, About and the old headings moved to the new pages; the tour's Settings page; three Features entries (Settings in eight
categories, Daily copies, Shooting defaults) with their pictures; README; claims backed.

**Is it worth an article?** No: a settings screen and a backup are ordinary engineering; nothing here changes what another shooter does.

## Decision log

One line per method choice where there was a real alternative: what was rejected, and why.

- **M0: `markerSize` is 8 modules, over the brief's 10.** TARGET-SCHEMA.md section 3.7, the renderer and FIDUCIAL-DECISION.md all put 8 modules in `markerSize`; 10 would print the wrong modules and two of the five sheets would not render.
- **M0: quiet zone of two modules, over a fixed 10 dmm.** It keeps each marker in the proportion FIDUCIAL-DECISION.md settled at 0.5 mm, so only the module scale changes.
- **M0: `GL-CF25-LTR` as printed, over the 454 dmm sighter gap of the pending geometry change.** The 0.5 mm sheet is then the Phase 0 sheet by identifier, which makes it the sweep's control; the sighter geometry does not bear on the module floor.
- **M0: each sheet's lattice derived at its own footprint, over forcing the 0.5 mm lattice onto all five.** The derivation rule is the format's, and an explicit lattice would be a different fiducial scheme; the count difference is handled at measurement by registering from the shared positions.
- **M0: a footprint parameter in `tools/layout/layout.py`, over a C#-only generator.** CONTRIBUTING.md makes the layout tool the authority; the C# derivation is checked against it marker for marker, and `layouts.json` is byte-identical with the parameter at its default.
- **M0: sheets named by module, frozen by identifier once printed.** Names a person can match to a printout while the set is live, and the entry 11 rule when it becomes a measurement input.
- **M1: the cross-section as a tangent angle integrated along arc length, over a height function of page position.** Integration makes the page-to-sheet map an isometry by construction; a height function stretches the sheet wherever it slopes.
- **M1: a cubic tangent angle, three coefficients, over five.** The brief's lower bound; noise-free recovery is exact at every bend swept, and each extra coefficient is more bend for noise to fit.
- **M1: corner residuals in image pixels, over page dmm.** A detector's corner error is a pixel quantity; reclassification and selection stay in page dmm so the Phase 0 distances mean what they meant.
- **M1: six starting ruling angles, over a single start.** The ruling angle has no gradient until the sheet bends, so one start can settle on the wrong family.
- **M1: focal length refined from the brief's EXIF estimate, over fixing it at the estimate.** Starts from 0.55 to 1.80 times truth all refine to within 1.4 percent; a fixed wrong focal length leaves the pose unable to reproduce even a flat sheet's homography.
- **M1: reclassify every corner at 12.7 then 2.54 dmm, over 2.54 only.** The rendered 1.00 in bow: 96 of 136 corners and a fail, against 136 and 0.00040 in.
- **M1: an F test at p = 0.001 for keeping the bend, over always fitting it or a fixed residual threshold.** Always fitting doubled a flat sheet's worst bull; a residual threshold would depend on scale and marker count, which an F test does not.
- **M1: synthetic corner noise from `main_flat1`'s residual, 0.52 px per axis, over the 0.16 px a clean render gives.** The render's figure would flatter the model; the flat frame's includes whatever its flat model left, so it is a ceiling.
- **M1: a forward-difference Jacobian, over analytic derivatives.** Fourteen parameters, and noise-free recovery to under 0.00001 in shows it is accurate enough.
- **M1: a bull whose edge profile has under 3 or over 4096 samples fails with a reason, over clamping the sample count.** A clamped count would still sample a mapping already known to be degenerate and report a centre from it.
- **M1: one starting focal length per joint fit, the EXIF candidate with the lower alone-fit residual, over the group median or a vote.** Four of the seven 2.2 mm frames carry one 35 mm equivalent and three the other, so a median or vote decides by frame count. Measured on those frames the choice does not matter: the costs are 0.1 percent apart and the fitted results are identical from either start.
- **M1: the joint-fit lens key left as focal length and f-number, with the table frames reported rather than regrouped.** Entry 6 settled the key, and regrouping after seeing these frames degenerate would be fitting the method to them.
- **M1: the lens held on the mounted frames was fitted flat and shared on the camera's flat frames, over the median of Phase 0's per-frame lenses or a fit on the full-marker bent frames.** A flat frame has no bend for the lens to absorb, a median breaks the pairing of k1 and k2, and a bent frame is the absorption being tested.
- **M1: the leftover-shape diagnostic on two terms, curvature along the rulings and a saddle, over a saddle alone.** A twist is a saddle only in the truth's rulings: the synthetic fit turned its rulings to 72 degrees, and in turned rulings a saddle is a difference of squares, half of which the bend absorbs.
- **M1: the real-frame lens run written and started before the sweep's numbers were read, over adjusting it to them.** Brief section 3.3's rule applied a second time.
- **M1: M1.2's "at random" marker rows relabelled as the first markers in raster order, over re-running them at random.** The label was wrong and the numbers right, and they are cited in entry 13; M1.7's sweep carries a random coverage of its own.
- **M1: corner quality placed on the sweep by a robust sigma over all corners, over the RMS of the kept corners.** The kept corners are truncated at 2.54 dmm, and their RMS stays between 0.9 and 1.2 px from 1 px of noise to 5.
- **M1: joint fits keyed on physical focal length, f-number, 35 mm equivalent and image size, over the equivalent and image size alone.** Entry 16 section 2 asks for the pixel geometry; the equivalent alone would join the cropped table frames to the main camera, which shares their 23 mm and their image size but not their distortion.
- **M1: the general developable surface as folds along turning rulings 10 dmm apart, over a smooth parametrisation of a tangent developable.** Rigid strips keep the isometry exact for any turn and reduce to the cylinder, within 0.018 dmm, when the rulings do not turn.
- **M1: a general surface whose rulings cross inside the page is undefined, over letting it fold through itself.** No sheet of paper takes that shape, and a fit that could reach it would report a shape that cannot exist.
- **M1: the minimiser takes a backward difference, and holds a parameter, at an undefined boundary, over a smooth barrier penalty.** The change leaves every fit that never meets the boundary unchanged, which the cylinder's raw rows confirm; a barrier would change every general fit's cost.
- **M1: stopped at the general surface with its joint fit unconverged and its alone fits as the measurement, over constraining its turn on weakly bent frames.** Entry 16 section 5 stops at the general developable surface, and a constraint would be a further model decision.
- **M1: one frame at a time by default, over the joint fit.** A user photographs one target at a time, the lens barely matters (M1.7), and sharing a camera cost `main1` a factor of two; the joint fit stays behind `--joint`.
- **M1: fewer than eight kept corners select the plane, over the bend.** Less evidence has to mean fewer parameters; the old default preferred the model with more.
- **M1: the correlation diagnostic on per-marker mean residuals between neighbouring markers, with a permutation null, over corner pairs or a variogram.** Corners of one marker share their detection and would read as structure that is not the sheet, and a permutation test needs no model of the noise.
- **M1: the mounted gate left at 0.005 in and recorded as open, over a separate looser gate.** Entry 17 section 2: the error budget argues for 0.003 to 0.005 in, and a number chosen after seeing the results is not a gate.
- **M2: morphology and blob extraction behind the imaging backend, the arithmetic, filters and measures in Core, over the whole primitive in Core or the whole primitive in the backend.** Core keeps no OpenCV dependency, and the port can be checked against the survey step by step because the backend calls are the survey's own.
- **M2: the baseline compared through the survey's own roll-up band, 0.15 to 0.55 in, over the detector's 0.60 in.** The survey's tables were computed through that band, and two detected holes fall between the two.
- **M2: render-and-difference opens with a 0.012 in disk, over the survey's 0.032 in.** After differencing only registration slivers remain to remove, and the survey's disk erased thin rims around pale cores, which were both detectors' misses.
- **M2: each bull's cell aligned to the observed image by phase correlation, over trusting the registration or opening wider.** The rings are fiducials already printed, and a wider opening would bring back the faint-hole misses.
- **M2: rim closure recorded and not gated, over a threshold between arrowheads and holes.** The two overlapped on synthetic sheets, and the synthesis draws no C-shaped rim, the case the signature exists to tolerate.
- **M2: merged neighbours split at an elongation of 1.45 by two-means, over refusing them as too large.** Refusal lost both holes of a pair; a split reports two and flags them.
- **M2: the oversized flag against the sheet's median, over a calibre size window.** The synthetic sheet carries no calibre; the median's failure when every bull holds a pair is reported.
- **M2: the synthesis calibration stopped at four iterations with its gaps reported, over iterating until it matched.** Each iteration moved one quantity at the cost of another, and the gaps' direction says which way the recall errs.
- **M2: thresholds set on seeds 1 to 3 and the gate read on seeds 1001 to 1003, over one seed set.** A threshold read off the seeds it is gated on is fitted to them.
- **M2: truth matched nearest pairs first within 0.15 in, over an optimal one-to-one matching.** Scoring should not share the rule of the assignment it is used to evaluate.

- **M3: special functions written for the engine, over a numerics package.** Nothing may be installed, and section 15.3's 1e-12 needs control of every step: the incomplete gamma prefactor through R's deviance form, and every upper tail computed as a tail.
- **M3: CorrNormal CEP through a trapezoid rule over angle on the Hoyt CDF, over Imhof's integral or a series.** The integrand is smooth and periodic, so the rule converges geometrically and matches shotGroups' hit probabilities to 1e-15.
- **M3: the minimum-volume ellipse by Khachiyan's algorithm at shotGroups' 0.001 tolerance, over an exact solver.** Section 15.3 says to match the tolerance or expect disagreement; at the package's tolerance it agrees to 3e-13.
- **M3: range statistics from ten groups per replication with running means, over simulating each group count separately.** Every cell keeps its full 10 million independent replications at a fifth of the cost of fifty-five groups.
- **M3: quantiles from 8,192-bin histograms, over storing ten million values per cell.** One bin is 7.3e-4 of the mean against a 5e-3 tolerance, and 65,536 bins ran three times slower for no measurable gain.
- **M3: the table read between rows by R's fmm spline on shotGroups' own grid, over simulating every n or interpolating linearly.** At n = 92 the spline reproduces shotGroups, and linear interpolation is 4.8e-5 off.
- **M3: a key held back only on evidence checked key by key, over excluding by dataset or loosening a tolerance.** The point of aim is detected from the fixture's own two centres, a Monte Carlo p-value by being an integer over 9999, and a disputed CEP by not being a root of its own distribution.
- **M3: the Fligner-Killeen statistic at 1e-10 relative, over 1e-12.** It subtracts n mean^2 from a sum of squares of up to 530 normal-quantile scores, which costs about three digits; the worst difference is 2.4e-12.
- **M3: the flyer expectation as an integral, over section 10's alternating binomial sum.** At 25 shots the sum's terms reach 5.2 million with alternating signs; the integral has no cancellation.
- **M3: the pre-pooling guard as pairwise F tests with Holm's adjustment, over Bartlett's test of homogeneity.** Section 11 names section 8.1's F test, and section 8.4 names Holm for its family.
- **M3: the engine's own xoshiro256** generator for resampling, over the runtime's Random.** Section 6 requires a recorded seed to reproduce an interval exactly, and the runtime does not promise its stream across versions.

- **Entry 20: bull centres from ring fits confirmed by centre dots, over the survey's annulus matched filter.** Holes break the rings and the scan's rings carry light stripes, so neither survives as a clean blob, while the dots do; the dot centroids are kept beside the ring centres and agree to a median 0.0019 in.
- **Entry 20: grid orientation found from the bulls, over trusting pixel order or reading EXIF orientation.** The photograph's pixels are stored a quarter turn from upright; lattice phase and the sighter line's 0.2-pitch offset place the grid whatever the storage, and an unmirrored layout picks the column direction.
- **Entry 19: the hole detector run on the whole photograph and on the registered sheet, over the whole photograph only.** Untuned on the whole frame it finds nothing, which is the finding; the sheet-only run shows what registration would let the same primitive do, and is labelled as that.

- **M4: the marking screen and the correction screen as one screen over one model, over a separate manual mode.** Entry 21 section 3 and DESIGN.md section 13 describe the same interactions; one immutable model gives both undo and a test surface that needs no window.
- **M4: the image shown from the pipeline's own OpenCV decode, over Avalonia's decoder.** The two can disagree about EXIF orientation, and a mark must land on the pixels the statistics and the detector use.
- **M4: the image sized by its decoded pixel grid, over the bitmap's size.** The bitmap's size follows the file's DPI tag; the headless test showed marks scaling with it.
- **M4: the UI built in C#, over XAML.** Avalonia 12 compiles bindings by default and the screen is one window; code keeps every control's wiring in one place a reader can follow.
- **M4: a headless test through the platform's pointer input, over calling the model directly.** It caught two faults that would have reached a person, which the model tests could not.
- **Entry 24: a dispersion figure withheld below five shots, over printing it with a warning.** A warning beside a three-decimal headline is what Alan read past; the count and the centre offset stay, because they are exact at any count.
- **Entry 24: each interval labelled with its exact coverage, over removing the c4 correction from the endpoints to make them 95 percent.** Brief section 5 has GroupLab match shotGroups' intervals; stating their coverage keeps that and stops the label from lying.
- **Entry 24: extreme spread's interval in the form that covers the expected spread, over shotGroups' `getRangeStat` form.** The latter covers 84.7 percent at two shots; entry 23 section 1 says to stay exact where shotGroups is not, and the harness still checks its form.
- **Entry 26: rotation as a view property of the marking state, over rotating the decoded pixels on load.** Rotating pixels changes the frame every saved position is in; a view property moves no mark, gives undo for free and reopens as it was left.
- **Entry 26: the stored pixel frame as the canonical frame, over the upright displayed frame.** It is the frame M4.1 files were already written in, so version 1 migrates without moving a mark, and it can be checked against the file itself.
- **Entry 24 section 5: the hole-size flag at nominal plus 0.132 in, over a ratio of the calibre.** Section 3.5 found the hole deficit roughly constant in absolute terms rather than proportional.
- **Entry 25: one application-wide unit setting, over a unit dropdown beside each input.** Entry 25 section 1 asks for it, and a setting that every figure obeys cannot leave one figure behind in inches.
- **Entry 25: "mil" as the milliradian, over the 6400 NATO mil of section 12.5's table.** Turrets are marked in milliradians; offering the NATO mil under the same name is the dialling error entry 25 warns about.
- **Entry 25: lengths and distance stored in inches whatever the setting, over storing in the unit the user worked in.** A file then means the same on every machine, which the test asserts by writing one marking under both settings.
- **Entry 25: printing through the system's PDF print command, over drawing pages to a printer from the application.** Avalonia has no printing API and nothing may be installed; the PDF is the path that already works, and what it cannot control is said on the screen and on the sheet.
- **Entry 25: the actual-size note as a render option, on in the print screen and off by default, over adding it to every render.** Phase 0's pages and the gates measured on them stay item for item as they were.
- **Entry 25: `/PrintScaling /None` in every PDF GroupLab writes, over only the print screen's.** A sheet printed from the command line is measured the same way and deserves the same request.
- **Entries 22 and 27: the scrubber in C#, over running `scrub_exif.py`.** Its library is not installed and nothing may be installed; the C# version follows the script's policy, keeps digital zoom for entry 27, and also removes XMP and trailing data, which the script leaves.
- **Entry 27: triage by decoded markers, holding rather than refusing what fails it.** Markers are the check the application already has; a held file costs a person one look and an `--accept`, where a refused one would need resubmitting.
- **Entry 22: the committed-image guard names the 16 photographs with GPS, over failing the suite until history is rewritten.** The rewrite is a decision for question 13; a named list keeps the suite green without letting a seventeenth image in or letting the list go stale.
- **Entry 28: rebuilding the aimed coordinates from the shot and a recovered aim, over excluding the four Fligner-Killeen keys as a known difference.** The rebuild reproduces R's doubles and all four statistics to 5.5e-13, so the gate checks something true rather than recording a gap; regenerating the fixtures at full precision would make it unnecessary.
- **Entry 28: a stated digital zoom of 0 read as 1 in the lens key, over keeping the tag's value.** The EXIF standard defines 0 as digital zoom not used, which is the geometry of 1, and every Pixel photograph in `scans/mounted/` states it.
- **Entry 28: refusing a `meta.json` whose opt-out field is missing, over treating it as false.** A missing opt-out is unknown, and publishing on unknown consent cannot be undone.
- **Entry 33: `grouplab analyze` composing `AutomaticMarking` and `GroupAnalysis`, over a separate pipeline for the command line.** The screen and the command then run the same code, so a fault found by one is fixed in both, which is how the sighter fault reached the marking screen's fix.
- **Entry 33: a printed sheet analysed even when its definition fails today's validator, over refusing it.** Validation decides whether to print a sheet; a sheet already on paper is what it is, and refusing it would make every Phase 0 sheet unanalysable.
- **Entry 33: `--target` required, over guessing the definition from the image.** Nothing reads the printed identifier or codes yet, and the built-in definitions share marker ids.
- **Entry 34: two of the owner's photographs held, over publishing all 28.** A screenshot and a messenger download cannot show who took them. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one until Alan confirms costs nothing.
- **Entry 34: a donated submission whose files were all held published as a provenance record alone, over leaving it out.** The record shows that a consented submission arrived and why none of it is published, which is the question a contributor would ask.
- **Entry 34: a separate `publish-owner` path, over running the owner's photographs through `intake`.** Intake requires a consent record, and entry 34 section 2 rules out inventing one.
- **Entry 35: a missing camera make alone is enough to hold a file, over requiring a copy's file name as well.** Entry 35 section 1 calls the make the stronger signal. A person can still accept a held donated file by name, so a false hold costs a look, and a false publication cannot be undone.
- **Entry 35: an edited phone copy that keeps its camera metadata, the four `~2` photographs, not held.** The rule is about lost camera geometry. Those copies keep their make, model and focal length, and nothing in entry 35 asks for more.
- **Entry 37: a submission whose consent cannot be read withholds its files' hashes, over ignoring it.** The rule exists because publishing under ambiguous consent cannot be undone, and an unreadable `meta.json` is the most ambiguous consent there is.
- **Entry 37: a file held for a consent conflict cannot be accepted by name, unlike a file triage holds.** Triage is a judgement about usefulness that a person can overrule; an opt-out is the contributor's decision, and only the contributor can change it.
- **Entry 37: the two conflicted submissions not published even as provenance records.** Publishing a record of a submission whose consent is in question, with its answers and credit name, waits for the contributor's answer as the photographs do.
- **Entry 36: `DFdistr` left at its committed version, over committing a JSON whose numbers are strings or editing `sg_distr.R` here.** Nothing reads it, `tools/` is the authority and planning's to change, and a fixture committed in a broken shape would be read as correct later.
- **Entries 39 and 40: any unassigned shot on a sheet of several scoring bulls withholds every figure, over quoting the assigned shots alone.** Leaving shots out without saying so would be a different wrong number, and the sentence in their place tells the user exactly what to do.
- **Entry 40: a tap on printed ink placed where it was tapped, with a note, over snapping onto the ink and naming it.** The centre of a printed stroke is never the answer, and a tap placed where the user put it is at worst as wrong as the user.
- **Entry 39: a moved shot follows its nearest bull unless the user assigned it elsewhere, over keeping whatever bull it had.** A shot dragged across to the next bull is almost always a correction of position, and a deliberate reassignment is recognisable because it differs from the nearest.
- **Entry 39: an impact placed by press, drag and release, over click, drag, click.** It is one gesture with a finger or a pointer, and a plain tap still places a shot.
- **Entry 42: a text colour below 4.5:1 moved along its own hue, over changing which role the text takes.** Section 2 says so. The contrast is measured on the three surfaces text sits on, not on the sunk image area, which carries marks. Counting the image area too would have pushed light `faint` almost onto `dim`, erasing the difference between the two.
- **Entry 42: styles rebuilt when the theme changes, over binding each control to a theme resource.** The shell is built in code, and rebuilding one style set keeps every colour decision in `AppStyles` and `Tokens` rather than spread across every control.
- **Entry 42: a unit left at its figure's size for now, over splitting the figure text.** The existing tests read that text, and section 1 requires them to pass unchanged.
- **Entry 42: mark labels in light text on a dark plate, over text in the mark's colour.** The mark's colour as text could not be read on the first screenshots, and the colour survives as a bar beside the number.
- **Entry 41: image facts from the metadata GroupLab already reads, over reading the further facts section 2 permits.** Bit depth, lens model, ISO, exposure and a count of EXIF tags would each mean reading more of a photograph's metadata for the sake of a log, which is the step the rule exists to stop.
- **Entry 41: the directory named in symbols on the first log line, over the resolved path.** Section 3 asks that the first line report where the log is, and section 3 also forbids a path. `%LOCALAPPDATA%\GroupLab\logs` satisfies both.
- **Entry 45: `last_action` as the name of the last event logged, over a separate list of action names.** Every user action already writes a stable event name, such as `print.select`, and a second list would drift from it.
- **Entry 41: Send saves the package before sending, into the log directory when the user has not saved it.** Section 7 says the zip is kept if sending fails, and the only way to guarantee that is for it to exist before the attempt.
- **Entry 41: a crash record not rewritten when a second crash lands in the same second of the same process.** The receiver's name pattern leaves no room for a counter, and the first crash is usually the cause.
- **Entry 46: an alert ring at the measured size on a flagged hole, over every impact ring at its measured size.** The size check reads an extent only for a dark region on paper, so a measured ring for every shot would silently fall back to the calibre on ink and on a dark backer. Every ring stays comparable, and the one that disagrees shows by how much.
- **Entry 35 section 6 item 3: the definition from the sheet's QR codes, over choosing among definitions by registering against each.** A frame that passes its CRC names one definition. A registration that fits well against the wrong definition is possible, because the built-in definitions share marker ids.
- **Entry 35 section 6 item 3: refuse and ask when the codes do not settle it, over falling back to the nearest match.** Eight of the 37 Phase 0 images are not identified and fall back to naming the definition. A wrong definition would produce a plausible wrong group.
- **Entry 35 section 6 item 3: the frozen Phase 0 definitions shipped beside the application, over the live library only.** The sheets already printed carry the frozen identifiers, and the print screen's list does not show them.
- **Entry 37 section 5: no size recorded for a note with no unit, over assuming inches.** Every size in the first notes had its unit, and a wrong assumption is a scale error of 25.4 or 2.54 times, which the marking screen could not detect.
- **Entry 37 section 5: the stated size offered in the rectangle prompt, over setting the scale from it.** The size is the sheet's, and only a person can say which corners are the sheet's and which side was tapped first.
- **Entry 37 section 4: the scrubber's keep list left as entry 29 set it, with `LensModel` reported rather than added.** Adding a field to what is published is a publication decision, and the lens grouping does not need it.
- **Entry 46 section 3: the count line replaces the "Placed:" line, over keeping both.** They carried the same three numbers, and the one planning asked for sits above the figures where it is read first.
- **Entry 35 section 6 item 2: the gate record workflow left failing on Linux and macOS, over passing within a tolerance.** Entry 32 section 3 asks for byte identity or an explained difference, and choosing a tolerance that makes the difference pass would be choosing the gate.
- **Entry 47: double resolution tried second, over a capability fallback.** The WeChat module is present in the runtime packages for all three platforms. The failures were detection on code modules under five pixels, which only a larger working image can help.
- **Entry 47: resolutions past 8000 px skipped, over trying every one.** Doubling a 600 DPI scan cost 52.7 s and gave nothing the scan did not, and the bound lost no image on the sweep.
- **Entry 47: identification counted per platform in the gate record workflow, over a per-platform expectation in the unit tests.** No count is known yet for Linux or macOS, and an expectation written before measuring would be a guess.
- **Entry 48: Linux's gate record difference explained and left red in the workflow, over a Linux reference record or a rule that passes it.** Making the job green needs either Linux's own records committed as its reference, or a rule about which differences pass. The first adds about 12 MB and the second is a gate written after the results, so the choice is planning's.
- **Entry 48: the owner corpus republished from the originals through `publish-owner`, over editing the published files.** Every published file still comes from the one scrubber, and the 13 files without a lens model reproduce byte for byte, which shows nothing else changed.
- **Entry 49 section 1: the printed tables committed as Windows text files, over comparing the platforms with one another inside one run.** A committed reference fails a single platform's job on its own, and it states in the repository what the record is.
- **Entry 49 section 4: the README's platform guard requires equality with the CI matrix, over naming at least as many.** A README naming a platform CI does not build would be false in the other direction.
- **Entry 49 section 2: the marker sort held for question 15, over committing it with regenerated records.** It changes committed evidence and a benchmark the Phase 1 surface work is measured against, which entry 49 did not foresee, and the question file exists for a measurement that contradicts what was written down.
- **Entry 52: every record regenerated twice under identical code, without the sort and with it, over comparing the sorted run with the committed records.** Five records were already stale, and comparing against them would have credited their changes to the sort.
- **Entry 52: tables from fits no command reproduces left as measured and marked, over updating the columns that can be regenerated.** Half a row regenerated disagrees with its other half and with the conclusions written beneath the table.
- **Entry 52 section 3: reordering applied at the homography fit, over shuffling the detector's output.** The sort puts any shuffled detection back in order, so only a shuffle after it measures the registration's sensitivity to order.
- **Entry 52 section 3: the edge fit's leave-one-out run from the converged pass's start, over rerunning the whole locator per point.** It isolates one point's weight in the fit; rerunning the locator would also move the rays and confound the two.
- **Entry 49 section 2: the journal replayed by call order, with the image hash reported beside it, over looking each detection up by its image.** A platform whose raster differs would match nothing and replay nothing, and the rerun exists to hand it Windows' corners anyway.
- **Entry 49 section 2: only the two measurements whose tables differ replayed, over all eight.** The other six already print identically on macOS, so replaying them could only repeat what the gate record shows.
- **Entry 58 section 4: the opt-out's second key is what a file scrubs to, over the decoded pixels.** It collapses the four re-exports to one value as a pixel hash would, and it keeps a consent mechanism inside `GroupLab.Core`, which has no image decoder and whose tests run on every platform.
- **Entry 58 section 3: either key alone withholds, over requiring both.** The same reasoning as entry 37 section 1's two opt-out signals: redundancy is the point, and publishing under ambiguous consent cannot be undone.
- **Entry 55 section 3 item 1: the counts recorded in the stage record and in `grouplab measure --json`, over the committed spike records.** A field in the spike records would regenerate thirteen of them and move the gate record's raw comparison, for a diagnostic that changes no figure anybody reads.
- **Entry 55 section 3 item 1: a ray counted as marginal within a tenth of the crossing threshold on either side, over counting only the rays that failed it.** A ray that just cleared the threshold is as easily flipped by the image as one that just missed it, and the tenth is the convention the leave-one-out already uses for the rejection limit.
- **Entry 61 section 3: no catch added for `PlatformNotSupportedException`, over adding one defensively.** The runtime throws `Win32Exception` for a verb off Windows, so a catch for the other would assert a behaviour that does not exist and would outlive anybody who remembers why it is there.
- **Entry 64: the working tree left alone, over running the fix as written.** Nothing differed, so the command would have been a no-op dressed as a repair, and running it would have left a false record that something was cleaned.
- **Entry 61 section 5 item 2: the tarball built on `ubuntu-latest`, over pinning `ubuntu-24.04`.** Pinning would freeze the glibc floor deliberately and freeze it silently apart from the test matrix, which tracks `ubuntu-latest`; printing the release it built on keeps the day they diverge visible, which is what entry 63 section 2 asks for.
- **Entry 61 section 5 item 2: the step fails when the native imaging library is missing, over shipping whatever publish produced.** A tarball without `libOpenCvSharpExtern.so` installs, launches, and then cannot detect a marker, which is a failure that arrives late and in front of a user rather than in CI.
- **Entries 65 to 69: the concept image left uncommitted, over committing a second copy.** The committed `docs/figures/screens/assignment-editor.png` is the same picture pixel for pixel, and the copy would add 481 KB and a content-credential block to the history for nothing.
- **Entry 65 section 4 step 2: wording that names no platform, over "on Linux".** The branch it describes runs on macOS as well.
- **Entry 70 section 3: a moved shot recorded as the difference from the bull detection gave it, over a list of notices appended at each edit.** The difference is derived from state, so undo, redo and a shot moved back all keep it true without bookkeeping, and it stays visible for as long as it stands rather than until the next edit.
- **Entry 70 section 3: the counts rule falls back to the nearest free bull, over the nearest of all bulls.** A bull a person has decided is not available to the matching in either mode, so the two methods differ only in whether the matching is forced.
- **Entry 70 section 6: three status states, over styling only the failures.** Success needs its own colour for the same reason alert does: a line that reads the same whether it worked or not teaches people to stop reading it.
- **Entry 74: pinning read from `BullChosen` alone, over `Corrected` or a chosen bull.** A shot becomes corrected when it is moved or marked not a shot, and neither of those chooses a bull.
- **Entry 73 section 1: a shot's pool is its nearest bull's, with the margin still measured to every bull, over a pool chosen by page region.** The sheet already says which bulls are sighters, and nearest-bull is the rule's own fallback; a region would be a second geometry to keep in step with every definition.
- **Entry 73 section 1: the overall method reported as nearest-bull when either pool fell back, over reporting the scoring pool's.** Reporting one-to-one would hide that a pool stopped being matched, which is the thing section 13 says must be said; the reason names each pool.
- **Entry 73 section 5: no mark size invented when no calibre is set, over drawing a nominal diameter.** A ring at a made-up diameter reads as a claim about the bullet, which is the kind of implied fact section 2 rules out.
- **Entry 73 section 7: the interval labels left at their exact coverage, over matching them.** Entry 24 decided the label states the coverage the interval actually has.
- **Entry 71: intake run on a copy without the unlisted scan, over adding the scan to the manifest.** Whether the consent covers the scan is the contributor's question, and the submission as received is left as it is.
- **Entry 71: the worst bull reported beside the worst clean bull, over the worst bull alone.** On a shot sheet the holes cut the rings of the bulls they hit, and separating the two shows the error is registration.
- **Entry 76 section 4: the printed name reverted, over landing it with a box that covers both captions.** On a sheet already printed the wider box hid a real hole, and the analyser cannot tell which caption a sheet carries; where the name goes is question 16.
- **Entry 76 section 4: detection cancelled at checkpoints between stages, over interrupting a stage.** A stage stopped halfway leaves nothing a person can use, and the checkpoints are where the work can be dropped cleanly.
- **Entry 76 section 4: a moved shot drops its measured diameter, over keeping it.** The measurement described the point the detector chose, and a ring at that size around a point a person chose would claim a measurement nobody made.
- **Entry 76 section 1: the aspect's null integrated exactly, over a simulated table.** The density has a closed form for every n, so there is no table to extend or to seed.
- **Entry 76 section 2: the two split detections merged at their midpoints for the re-run, over the pipeline's figures or hand-picked positions.** The pipeline's figures count one hole twice, and the midpoint uses only what the pipeline found, which is the question entry 76 asked.
- **Entry 75: a shot with no bull named "unassigned, at x, y" in the list, over "unassigned" alone.** Two such shots would otherwise read the same, and the position is a fact the screen has, not an order.
- **Entry 77 section 3 item 1: a blob counted as swallowed only when it passed every shape filter, over every blob centred in a zone.** Printed matter leaves residue that the size and shape filters refuse anyway, and counting it would bury the one number that means a hole may have been lost.
- **Entry 77 section 3 item 2: the check made standing by an artwork fingerprint test, over running the corpus inside the test suite.** The corpus takes minutes and its counts differ by platform. The fingerprint is fast and exact on every platform, and it forces the comparison to be run where it can be.
- **Entry 77 section 3 item 2: the committed corpus punched with synthetic holes on three offset grids, over committing a real shot sheet.** No shot sheet has consent to be committed, and the Phase 0 scans are real print made before every change since.
- **Entry 77 section 4: oversize measured three ways, over the detector's diameter alone.** Entry 73's warnings came from the screen's size check, which is a different measurement, and the test had to see the one that was reported.
- **Entry 77 section 5: a sheet with no clear place prints no name, over shrinking the name further or trying another margin.** Section 5 states the rule. One place with one clearance is also what the placement test can check on every sheet.
- **Entry 77 section 5: the identifier caption left at the bottom beside the new name line, over moving it.** It is C6's recovery path, and its zone is what every sheet already printed is read with.
- **Entry 78 section 4: the calibre only vetoes a split, over also splitting a round blob of two holes' area.** Splitting on size alone would cut a genuinely odd hole to fit the expectation, which section 4's second guard rules out; the blob is flagged instead.
- **Entry 79 section 1: a measured ratio per image kind, over the calibre or one pooled ratio.** Scans and photographs measure holes differently, 0.944 against 0.986, and section 1 asked for them apart.
- **Entry 79 section 1: a photograph without camera data taken as a scan, over refusing the ratio.** There is nothing in such a file to tell the two apart, and the one such photograph measures 0.924, within the scan ratio's spread.
- **Entry 78 section 4: a calibre named after an uncorrected detection detects again, over asking.** Nothing a person did is lost, and a result found without the size is the one section 3 says is wrong.
- **Entry 80 section 2: the old sweep stopped unfinished, over letting it run.** Its synthetic holes were the wrong size, and its real rows used the bullet diameter, so its result could only have been discarded.
- **Entry 80 section 2: the synthetic holes scaled to read like .308 on a scan, over reweighting the sweep.** A scale fixes what the holes are; a weight would only change how much a wrong population counts.
- **Entry 80 section 2: no split threshold adopted, over adopting the survivor.** The only setting the real holes allow also changes the default without a calibre and moves committed synthetic records, and no real merged pair has been measured to say what it costs.
- **Entry 78 section 2: the residue fix proposed and not built, over a plain elongation cap.** A cap alone would refuse two real holes joined by the closing, a silent loss, and the safe form depends on the threshold still open.
- **Entry 81 section 2: the oversize flag rebuilt around a single-hole size, over keeping the median rule and lowering the calibre flag alone.** The median rule flagged ordinary holes on a tight sheet and let pairs through on a sheet full of them, so a calibre-only fix would have left the loud failure silent whenever no calibre is named.
- **Entry 81 section 2: the single hole without a calibre is the sheet's 25th percentile mark, over its median.** On the composite sheets half the marks were merged pairs, and the median moved to a pair's size and flagged none of them.
- **Entry 81 section 3: the residue fix on size and elongation together, over solidity.** Solidity overlaps across all three populations, real single holes reaching 0.59, while size splits the elongated ones cleanly.
- **Entry 78 section 2: a small elongated blob kept as one hole below 2.2, over refusing every blob the size vetoes.** Refusing a real hole is a silent loss, and the margin between the most elongated real hole, 1.72, and the split threshold, 1.80, is too thin to refuse on.
- **Entry 82 section 2: the floor used to veto and never to flag, over clamping the quarter-point alone.** On the clean photographs there were no round marks to clamp, and a floor that flagged would flag every real hole, since each is larger than the smallest a bullet makes.
- **Entry 82 section 2: the floor at 0.16 in, over 0.17.** 0.17 is the bullet; a .17 hole measures about 0.944 of it on a scan, and the floor must sit below any real hole.
- **Entry 82 section 3: two sizes need a gap of five pooled deviations, over three.** An even spread of sizes cut in half is 3.3 apart, so three would ask for a calibre on any wide one-calibre sheet.
- **Entry 82 section 3: no flags at all on two sizes, over tentative flags on the larger group.** Entry 82 asks for one sentence rather than many flags, and a sheet of two calibres would have every larger hole flagged as a merge.
- **Entry 82 section 6: the detector's flag drawn beside the size check's, over merging the two.** They measure different things, the detector's residual against the size check's dark region, and each says what it measured.
- **Entry 83 section 2: sizes converted at each blob's own scale, over leaving the detector alone as entry 83 section 4 asked.** Section 2's test found a wrong unit conversion rather than a tuning question. A size read 30 percent wrong on any oblique photograph is a defect, and the fix is a few lines.
- **Entry 83 section 2: the clean photographs' rise from 24 to 35 accepted, over restoring the single scale for the veto.** The single scale was wrong for real holes and for residue alike, and the residue it hid is the kind section 3 says cannot be resolved without a calibre.
- **Entry 83 section 4: the review queue computed from the marking, over storing it.** Every edit changes what needs review, and a stored queue would go stale; only a person's "keep it" is stored, because nothing else can know it.
- **Entry 83 section 4: the editor built into the marking screen, over a separate mode with Accept and analyse.** The statistics are already live on every edit, so an accept step would commit nothing. Discard edits is kept, as one undoable step.
- **Entry 83 section 4: the review keys taken on the tunnel route, over the window's key handler.** A focused button would otherwise take Space and Enter, and pressing Space would press the last choice again.
- **Entry 87 section 1: the screen's calibre size check removed, over keeping it and feeding it into the queue.** It measures the dark region connected to a mark, so a printed ring is part of every mark that touches one, and it read five confirmed single holes at about twice their size. Adding those five to the queue would have made the queue wrong rather than the panel.
- **Entry 87 section 1: `HoleSize` kept as a measurement.** The apparent extent is what the harnesses read and what the snap radius needs, and keeping it without a threshold is what makes the removal a removal of a judgement rather than of a number.
- **Entry 86 section 3: the enclosed-ink fraction recorded on every detection although nothing reads it.** It is the quantity that separates a hole on a ring from one beside it, it cost nothing to carry, and it is what proved the hypothesis wrong rather than plausible.
- **Entry 87 section 2: the README's states tied to DESIGN.md by a test, over a review habit.** Entry 60 found the README stale for days, and the two documents can now only disagree by failing a test.
- **Entry 90: the parametric editor and the visual designer deferred, over giving them a phase.** Alan is getting a specification from Jeff, and a phase for a screen nobody has specified would be a date attached to a guess. The deferral is explicit, carries its reason, and is checked by a test, which is the difference between parking something and losing it.
- **Entry 90: assisted hole placement raised as a question, over scheduling it or dropping it.** The detector differences against a definition and a store-bought target has none, so the bullet is not schedulable as written; but two of the three meanings of "assisted" are already built, so it is not droppable either. That is a design answer and not a wording fix.
- **Entry 90: the scope test requires a citation on a deferral.** A bullet could otherwise satisfy the test with the word alone, which is exactly the quiet drop the test exists to catch.
- **Entry 88: the oversize flag left alone although the measurement points at a defect in it.** Four of five flags on real material hold one hole's worth of ink inside a ragged hull, so flagging on ink area would remove them; the threshold is one the Phase 1 gate is measured against, and entry 83 section 3 says stop tuning the detector. Recorded with its numbers instead.
- **Entry 84: `PhotographHoleToCalibre` changed to the re-measured 0.948 and `ScanHoleToCalibre` left at 0.944.** The photograph figure was measured with the single-scale defect in place; the scan figure was not, and its 0.006 move comes from a longer verified hole list rather than from the fix.
- **Entry 84: the 25-shot rehearsal recorded as a rehearsal.** It measures the software's share of the two minutes and nothing about a person, so recording it as the Phase 3 gate would put a state of "done" on a page where the thing being gated has not happened.
- **Entry 94 section 1: the oversize threshold left at 1.35 while the quantity under it changed.** Area reads about six percent lower than the hull-derived reference it is compared against, so the threshold is already slightly stricter; and the value that would catch every 0.10 in pair would also flag S1b again, which is the false flag the change exists to remove.
- **Entry 94 section 1: the hull area kept beside the mark's area rather than dropped.** Solidity is the ratio of the two, and entry 88's shape measurements rest on it.
- **Entry 94 section 4: the split's two centres computed at detection time and carried on the flag, over computing them when the choice is taken.** The review queue has no image, and a choice that needs the pixels is a choice that needs a mouse.
- **Entry 94 section 2: subgroups keyed by bull rather than by shot.** A shot moves between bulls during review and its load does not; the sheet's layout is what holds the loads.
- **Entries 91 and 92: the zero correction refuses rather than rounds.** Where the offset is inside the sampling error the panel gives a shot count instead of a number, because a bare figure will be dialled.
- **Entry 93 section 3: high contrast derived from the dark tokens in code, over a fourth hand-drawn palette.** A palette that cannot be derived is evidence the roles carry values rather than meanings, and deriving it is what proves the concept is a design language rather than one screenshot.
- **Entry 95 section 2: the count item acts on its first candidate only.** Naming three and acting on one keeps it a key press; a person who disagrees with the ranking reaches the right mark through its own item or by selecting it.
- **Entry 95 section 2: every mark gets a size in holes, measured against the veto's size where no flag size exists.** The ranking needs a size on unflagged marks, which are most of the candidates, and only the flag's own size may raise the flag.
- **Entry 95 section 3: nothing changed for the mounted gate.** The investigation found where the error lives and what would separate the two explanations left; a change made before that photograph would be tuning against the frames being gated.
- **Entry 96 section 2: the warp that passes `IMG_5819` not adopted.** It was found on the frames being gated, it makes `IMG_5820` worse, and a model chosen because it passes the frames that prompted it is not a gate result.
- **Entry 97 section 1: a shot a person placed is neutral, not teal.** Teal means the software found it on its own, and a mark a person put down or moved is not that.
- **Entry 97 section 2: the marking keeps a copy of the rifle rather than its name.** A correction read from a saved marking must be the one that was right when it was shot, whatever the record book says since.
- **Entry 97 section 2: a barrel's count grows only on a person's step.** Counting automatically on detection would count a reopened sheet twice.
- **Entry 97 section 3: the per-stage rasters left for the next batch.** The records land live and cost nothing; the rasters would need keeping images the detector currently throws away, which section 19 allows for one interactive analysis and which deserves its own measurement of cost.
- **Entry 97 section 5: the window rehearsal pinned at 10 presses.** It is its own baseline at 300 DPI, and a ceiling the next batch must not raise.
- **Entry 98 section 3: a control added that the entry did not ask for.** The holed half and the clean half are different places on the page, and without the same split on unshot sheets a positional difference would have read as hole damage.
- **Entry 98 section 2: the nominal hole is .30 when nothing better is known.** It is the middle of what the corpus carries, and the sheet's own measured holes replace it as soon as the detector has run.
- **Entry 99: the editor ports the library's solver rather than calling it.** `tools/` is planning's and Python is not shipped; the port is held to the original by rebuilding ten sheets exactly.
- **Entry 99: the fewest markers allowed is 9, the fewest any built-in sheet carries.** It is the one figure the project has shown registration holding at, on GL-LR300-T's tile, and choosing a lower one would be a guess.
- **Entry 98 section 5: only the residual is kept.** The markers, corners and rejections were already in the result, so the interactive run's extra cost is one image.
- **Entry 101: the homography's final fit carried to convergence, over stopping at OpenCV's ten iterations.** A fixed iteration count reproduces the native figures only as far as every other detail of its solver does, and convergence is a definition every platform reaches the same way.
- **Entry 101: native code kept for the integer steps.** Candidate detection, decoding and RANSAC's inlier choice were identical on every platform in every gate record run; porting them would add risk to steps that already agree.
- **Entry 101: the contour lines fitted in double precision, over emulating OpenCV's single precision.** The emulation reproduces native and proves the contours are the same, but its answer is up to 0.09 px from the least-squares line it sets out to compute.
- **Entry 101 section 5: the picture carried on the stage record, over a separate artefact channel.** The record already reached the timeline live, so attaching the picture to it makes the two arrive together, and a trace put on the timeline after its run draws its pictures by the same path.
- **Entry 103 section 1: extreme spread drawn as the line between its two shots, over the concept's circle.** A circle that size reads as a region containing the shots; extreme spread is a distance between two of them.
- **Entry 103 section 1: Show work opens the timeline in the editor, over a second timeline in the analysis state.** The timeline already shows the work, and one copy cannot disagree with itself.
- **Entry 103 section 2: the flyer card says "further out than a group this size usually puts its worst" below one time in twenty, over always saying "not a flyer".** The old line said not a flyer whatever the distance; the card still leaves the call to the shooter.
- **Entry 103 section 3: held as a question, over running the sweep.** The sort was already committed by entry 52, and regenerating would have reproduced entry 101's records.
- **Entry 104 section 2: the worst shot calibrated by simulation at every count, over withholding the verdict below some count.** The calibration is valid from the dispersion minimum up and costs milliseconds, so there was no count where withholding was the more honest answer.
- **Entry 104 section 4: only the inked discs faded, over fading the whole bull.** Fading the paper as well turned it grey on dark chrome, and the paper-on-dark contrast is most of the concept's character.
- **Entry 105 section 6: the work bar defaulted closed, over open.** Alan asked for the strip off the screen; a failure stays a prominent error without it, and Show work turns red and says so when a stage fails.
- **Entry 105 section 7: a name read by its leading number only after the table, over the table alone.** "6.5 Creedmoor" and "30-06" are what shooters type, and their leading number is the table's name; a wildcat still falls through to the old rule and says what it read.
- **Entry 105 section 4: the mark drawn in the files' own colours until question 20 is answered, over recolouring it to the nearest tokens.** Recolouring would change a mark Alan chose to a set of colours nobody chose.
- **Entry 105 section 5: the icons drawn by a command from the committed mark, over a script outside the build.** The CLI already carries the imaging library, and nothing new is installed.
- **Entry 106 section 1: the viewer path on every platform with a confirmation dialog, over keeping the print verb anywhere.** The verb printed silently at the viewer's own scaling on Alan's machine, and GroupLab cannot see what any registered print command does.
- **Entry 106 section 4: the PDF drawn by the renderer's own writer, over printing the Markdown through a browser.** The other PDFs came from Chromium by hand; a command in the repository keeps the list and its PDF in step with the code, and installs nothing.
- **Entry 106 section 5: raised as question 21, over building it now.** It is about a run of its own, three decisions are open, and its test needs a PDF printer the CI runner may not have.
- **Entry 107 section 1: "9mm" read as a diameter, over refusing it.** The section's rule reads any number marked mm, and its test list refuses "9mm"; the rule is built and the conflict is question 22.
- **Entry 107 section 2: the quiet zone not counted in the margin refusal, over refusing on it.** The margin leaves bare paper and the quiet zone is bare paper, so it prints as intended, and a smudge near the edge lands on ink, which is refused on its own account.
- **Entry 107 section 2: `WindowsPrinter` in the CLI project, over the application.** The Core tests reach it there to print and measure a real job, and they already reference that project for the imaging backend.
- **Entry 107 section 2: markers located on the printed page with no fitting, over registering through a homography.** A homography absorbs scale and offset, which are the errors the test exists to catch.
- **Entry 108 section 2: designations compared as decimals at the value typed, over matching the text.** ".270" and ".27" are one value, and matching text would let one form through that the other refuses.
- **Entry 108 section 2: the refused value shown as typed with its unit, over normalising it.** The person sees their own entry named, which is what the refusal is about.
- **Entry 109 section 1: "why" as a compact disclosure on each item, remembered per item, over one panel of explanations.** An explanation read beside the figure it explains needs no hunting, and closed it is one short line.
- **Entry 109 section 1: the older size names mapped onto the five, over replacing every use.** Every existing use lands on the scale at once, and the test catches any new size.
- **Entry 109 section 2: settings as a screen in the main window, over a dialog.** The rail is navigation, and the gear is a destination like Print.
- **Entry 109 section 3: the flyer card's hedge kept in view, over moving it behind "why".** Without "by that measure alone" the verdict would say more than the test can.
- **Entry 109 section 3: excluded rows struck through, over a word in the row.** One number per shot leaves no room for a word, and the tooltip says it.
- **Entry 110 section 2f: the tolerances committed and pushed before the reference tables were generated, over writing them in the same commit.** The history then shows the order, which is the point of stating them first.
- **Entry 110 section 2f: both references generated on a GitHub runner, over installing Node and py-ballisticcalc here.** Nothing is installed on the development machine, and the same runner re-checks the JavaScript on every push.
- **Entry 110 section 2f: the G1 cases held as named known failures, over a red check or a wider tolerance.** A red check would block every other change, and a wider tolerance would make the gate mean nothing; the named list fails the day it is no longer true.
- **Entry 110 section 2a: the Coriolis vertical term held out rather than ported with its sign corrected.** The entry said to port it as it stands, so the conflict is a question, not a quiet fix.
- **Entry 110 section 2c: aerodynamic jump left out, over Litz's fit from memory.** The entry required the coefficients from the publication, which was not to hand.
- **Entry 110 section 2b: the rifle zeroed on the flat and then tilted, over zeroing at the shooting angle.** A rifle is zeroed at a range and then carried to the hill.
- **Entry 111 section 1: poncelet as the second transcription, over the gehtsoft ports.** Those are py-ballisticcalc's own ancestry, so agreeing with them would prove nothing; poncelet reached JBM's McCoy tables through JBM's files.
- **Entry 111 section 1: the JavaScript's tables kept only for the compatible mode, over deleting them.** The transcription check compares the port with the file as it is, and that needs the file's tables.
- **Entry 111 section 3: "why" as a plain button beside the item's last line, over a toggle.** The theme paints a checked toggle amber, and an open explanation needs no one's attention.
- **Entry 111 section 3: the count kept in the placement sentence, over the "Shots" row.** The sentence carries the count and how the shots were placed; the row carried the count alone.
- **Entry 111 section 4: the timing read from the log, over a stopwatch in the window.** The log already records every step with its time and no path, so the measurement needed no change to the application.
- **Entry 112 section 1: the sheet's registration kept in the marking file, over re-detecting on reopen.** Section 18 says no image is ever needed to reopen, and without the mapping a session reopened from its marking had no scale.
- **Entry 112 section 1: the proof image as a JPEG in the database, over a file beside it.** One file is the whole record, and the export carries it.
- **Entry 112 section 2: the report's words from the screen's own functions, over a second wording.** Two wordings drift, and the rule is that paper says nothing the screen does not.
- **Entry 112 section 3: deleting a sheet a session used allowed, over refusing.** Every session keeps its own copy of the definition, and refusing would make a sheet undeletable while any session of it is kept.
- **Entry 112 section 4: elevation carried by a central difference of the zero range, over a new solver input.** It needed no change to the validated solver, and a test holds it to a directly flown change within 1 percent.
- **Entry 112 section 4: the dope table on a Ballistics screen with its own rail slot, over a panel in the analysis.** The analysis column is 372 pixels, and the table has seven columns; question 26 asks.
- **Entry 113 section 2: the chart slot as Compare loads, over Reports.** The concept's chart icon is Compare loads, and a report is written from its analysis.
- **Entry 113 section 2: sessions at different distances compared as angles, over refusing.** Dispersion scales with distance, and the screen says it compared them as angles.
- **Entry 113 section 3: an optional fixed launch angle on the solver's input, over a zero-range difference for velocity.** A change of velocity with the zero kept would move the bore; the angle must be held, and without the field nothing changes.
- **Entry 113 section 4: the rule re-baselines the detected reading, over counting its moves as edits.** A rule is how the sheet is read, and flagging every shot it placed as moved would raise the doubles again.
- **Entry 113 section 6: JPEG pictures in GroupLab's own PDF writer, over another PDF tool.** The project's reading documents already come from that writer, and a JPEG goes in as it is.
- **Entry 113 section 7: the new screens held to their themes' tested text roles, over pixel contrast measurement.** The roles are what ThemeTests holds to their ratios, and a render's pixels vary between machines.
- **Entry 114 section 1: every filled shape drawn as a closed path, over keeping `FillRect` with a capability check.** Both drivers report the same RASTERCAPS, so a capability check cannot tell them apart; a path is honoured by every driver.
- **Entry 114 section 1: the page aborted when a drawing call fails, over printing what did draw.** A sheet with a marker missing looks normal and cannot be measured, and the fault is found only when it comes back from the range.
- **Entry 114 section 1: the in-app Print button demoted, over disabling it.** Hiding it would leave a person who wants it with no path and no reason; the screen now says what was wrong and what is safer today.
- **Entry 114 section 1: the drivers the tests print through are a fixed list, over enumerating the machine's printers.** A driver can open an application when it is printed to, as the OneNote one does.
- **Entry 115 section 2: bulls chosen with shift and click, over a plain click.** Every bull on a shot sheet has a hole on it, and a plain click there is the hole, which is what the editor has always done with it.
- **Entry 115 section 3: the velocity SD written to the load record with its provenance, over showing it on screen alone.** A figure that looks typed and a figure that was measured are different things, and the record is where the solver reads it.
- **Entry 115 section 3: the schema upgraded in place on open, over refusing an older database.** A person's sessions are not something to make them re-create, and the upgrade is one statement in one transaction.
- **Entry 115 section 4: a sheet read as the wrong definition warned about, over refused.** A damaged or marked-up sheet looks the same to the evidence, and the person can see the sheet.
- **Entry 115 section 4: the low-resolution threshold measured, over assumed.** The detector reads a sheet at 96 dpi and fails at 60, so the message is keyed at 120 rather than at the 150 that seemed obvious.
- **Entry 115 section 6: the corrected JavaScript generated from the original by a script, over edited by hand.** Every change is then exactly the five, and the file can be regenerated when the original changes.
- **Entry 116 section 1: a folder publish, over a single file.** The native OpenCV library then sits beside the executable where the loader expects it, nothing unpacks itself into a temporary folder on first run, and an antivirus sees an ordinary folder rather than the self-extracting shape it distrusts.
- **Entry 116 section 2: the shot sample generated by GroupLab, over shipping no shot sheet.** The repository holds no shot sheet that may be published, and a tester who cannot see the figures in the first minute has not seen GroupLab. It is question 29 all the same.
- **Entry 116 section 2a: the packaging script warns about a missing Inno Setup locally and refuses in a release.** A working copy should still build a zip on a machine with no installer tooling; a release that quietly shipped one asset of two would be worse than a failed release.
- **Entry 116 section 3: every asset attached twice, versioned and stable.** A bug report then names a build, and the README's front-page links keep working with nobody editing the README after a release.
- **Entry 117: the benchmark generates its own material, over reading anything a person has.** It then runs on a bare checkout, on CI and on a machine that has never analysed a target, and no case can quietly depend on one machine's folder.
- **Entry 117: the stage timings come from the stage record, over a second timing path.** Two clocks disagree eventually, and the one the application already files is the one a person sees in Show work.
- **Entry 117 section 3a: coverage checked by reflection over what does work, over a list of cases.** A hand list rots the moment a feature is added; this one named 25 gaps the first time it ran.
- **Entry 117 section 3b: the interface benchmark lives in the test project, over the command line.** Timing a control needs a windowing platform, and shipping a headless one inside the application to measure it would be a cost carried by every person to serve a benchmark.
- **Entry 117 section 3b: a dropdown measured by choosing from it, over opening its popup.** Choosing is the work; opening a popup headlessly crashes in the toolkit, and it would have been measuring the platform rather than GroupLab.
- **Entry 118: the anchors derived in the test from the heading text, over read from the list.** A renamed heading then fails the test rather than leaving a link that scrolls nowhere, which is the failure a reader cannot see.
- **Entry 118: the contents list after the Download section, over at the top.** Somebody who came to get the program should not have to read past a contents list to find it.
- **Entry 119 section 1: the updater ranks the trains, over SemVer's own text ordering.** SemVer puts beta below nightly because "b" sorts before "n", and section 4.5 needs the opposite. Question 30.
- **Entry 119 section 3: ECDSA P-256, over the Ed25519 the entry asks for.** .NET 10 has no Ed25519 and this machine has no NuGet source, so no package can be restored to provide one. The algorithm is named in every manifest, so a later change is one older builds refuse by name. Question 31.
- **Entry 119 section 3: the workflow fails loudly with no key, over publishing unsigned.** An unsigned manifest is one the application would have to trust without being able to check it.
- **Entry 120 section 4: the failing result carries its definition, over the screen falling back to the pipeline's words.** The advice written for a sheet that will not register was unreachable in exactly the case it was written for.
- **Entry 120 section 10: the preview sits in the grid row, over inside the scroll viewer.** Inside one it measured its own natural size and left the window two thirds empty; the scroll viewer is now used only when zoomed, where panning is the point of it.
- **Entry 121: the version raised to 0.2.0, over renaming what is already published.** v0.1.0 is history and stays where it is; the train moves above it instead.
- **Entry 122: one interface for everything outside the process, over telling the benchmark not to click that button.** An exclusion list would have fixed this button and left the next one to be found by somebody's browser opening.
- **Entry 461: version vectors noticed by hash at each run, over a revision counter bumped in every save.** A counter alone cannot tell
  "changed on two devices since they last met" from "changed twice on one", and bumping in every save would have touched every screen
  that saves; the hash finds a device's own changes wherever they were made, and the vector says how two versions stand.
- **Entry 391: the M834's feed taken as 99.30 percent, the mean of all five readings against the lengths drawn, over planning's 99.35 or the scan's 99.49 alone.** The scan is the most precise single reading and agreed with the rendering across, but its scale along the carriage is unproven, and the plain mean keeps every reading within the instruments' own 0.2 percent.
