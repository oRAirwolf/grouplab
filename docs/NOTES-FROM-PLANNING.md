# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

**How entries arrive, from entry 44 on.**
- **Delivery:** the planning session delivers each new entry as its own file in `docs/notes/inbox/`, named `entry-NN.md`, and never writes this log or any other existing file.
- **The only writer:** the Claude Code session is the only writer of this log.
- **Actioning includes three steps:** folding the entry into the top of the log, setting its status, and deleting its inbox file.
- **Several files waiting:** fold them in ascending entry number, so the newest ends up first.
- **Why:** two writers rewriting one file with no locking overwrote this log once, and separate paths cannot collide.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## The archive

Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the
only written record of why much of this project is the way it is.

- [`docs/notes/archive/notes-2026-10.md`](notes/archive/notes-2026-10.md), entries 322 to 447, 126 of them.
- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 321, 320 of them.

---

## 2026-10-21, entry 462: an overnight queue chosen by Alan (question 106; site audit; articles; interface sweep; screenshots; automation)

**Status: actioned 2026-10-21. Not done: section 1's two colour findings and section 3's eight looks, which wait on Alan as DESIGN NEEDED; section 3's focus visibility and the phone's deeper pages, not checked by machine; section 4's fourteen Features entries still on a general screen (each says why in docs/figures/SCREENSHOTS.md) and the phone pictures beside the new ones; section 5's two automations that need Alan (requests 106 and 107, prepared and off) and the handoff task, which was not found and needs naming; section 6 had no work. Sections were taken 0 to 3, then 5, then 4, so the pictures showed the fixed screens.**

From the planning session, 2026-10-21 07:45 UTC (01:45 Denver). Alan is going to bed and asked for more work that needs nobody:
auditing the website, new articles, refining the interface, many more screenshots in the guides and on Features, and automating more
processes. Take the sections in order, one commit per section where it allows, STATE rewritten after each. Error reports are checked
before each section (entry 457). Anything that needs Alan is found up front, written into for-alan.md in one go, and the work moves on;
anything judged by looking goes there as DESIGN NEEDED.

**Budget, Alan's word tonight:** this queue may run to **95%** of the week (usage-now.json `seven_day`; 39% when written). At 95% finish
the step in hand cleanly and stop. The week resets 2026-10-22 02:00 UTC; after the reset the usual 85% guard applies again, and what is
left of this queue continues under it. Helpers as CLAUDE.md says: screenshot walks and chores to chores-haiku, wording and
consistency to docs-sonnet, application code and UI on the main model. One worker unless two separate areas really run apart.

## 0. Question 106 answered: (b), leave it

Alan, 2026-10-21: leave a photograph's code reading as it is. Keep reading every place, so a picture showing two sheets is still
caught. Archive question 106 as answered, with the measurement kept in docs/PERFORMANCE.md. Phase 9 has no next step queued.

## 1. A full audit of grouplab.org

Audit every page the site builds, then fix what is found in the same section:

1. Every page at 390 px and 1440 px, light and dark: anything clipped, overlapping, off the page sideways, unreadable, or a picture
   that does not load.
2. Every link, internal and external, and every download link on the download page against the newest nightly's real assets.
3. Lighthouse (or an equivalent run headless in CI) for accessibility, best practices and SEO on the home page, Features, the
   download page, one guide, one research article and the tour; fix every accessibility finding that is not a look decision; record
   the scores in PHASE1-RESULTS.md. Add the accessibility part to `checks.yml` if it is cheap enough to run on every push.
4. `scripts/claims.py --check`, `scripts/consistency.py`, `docs/RETIRED-WORDING.json`, and the weekly `consistency` issue: zero open
   findings when the section ends.
5. The tour: every tour page's words against its current picture (entry 146 section 4.4); a word naming something no longer on
   screen is fixed.
6. Features: every feature described matches what the newest nightly does on each platform it names, and what is phone-only or
   desktop-only says so.
7. The release notes page current to the newest nightly; the research index showing every published article with its thumbnail.

Look changes (colors, layout, type) are not made: each goes to for-alan.md as DESIGN NEEDED with a screenshot path.

## 2. New research articles

Entry 461 decided every open row. Now propose and write new ones from work done since, each meeting entry 392 section 1 (every
checkable sentence backed, figures from committed scripts and data, simulations rerunnable, no pseudoscience, the developer not
named, the existing byline), published as entry 393 allows, thumbnail as entry 453 does. Candidates, each decided in RESEARCH.md
either way:

1. **Does your rifle group as well as Top Gun says it should?** Request 103's basis (five-shot extreme spread, averaged, as Alan's
   friend read it from Modern Advancements in Long Range Shooting, Volume III, chapter 3) against what a measured sigma implies
   (`TopGun.OnItsBasis`, `TopGun.Compared`), with what a shooter should and should not conclude from one or two groups. Cite the
   book by title and chapter only; quote nothing from it.
2. **How much a canted scope costs at distance** (`ScopeMount`): the geometry of cant against the elevation dialed, worked for real
   distances and loads from GroupLab's own solver.
3. **Sight scale factor: when a click is not what it says** (`ScopeTracking`): what it is, how big real errors are reported to be
   (sourced), how a tall target test measures it, and what it does to a long-range first-round hit, from GroupLab's hit
   probability.
4. Anything else since entry 402 that passes entry 158's test and has no row yet.

No link to any site of Alan's other than grouplab.org.

## 3. Interface sweep: fix the bugs, send the looks to Alan

Code does not settle looks (working rule of 2026-09-28). This section finds what is wrong and fixes only what is a fault:

1. Every desktop window at 1280x720, 1920x1080 and 2560x1440, both themes; every phone screen on the emulator at three widths, both
   themes, and at Android's and the iPhone's largest text sizes.
2. **Fix** clipped, overlapping or truncated text, a control pushed off screen, focus that cannot be seen, text below WCAG AA
   contrast (4.5 to 1, or 3 to 1 for large text and controls) where an existing theme color fixes it, inconsistent wording or
   units for the same thing on two screens, an empty state with no words, and any failure still shown as small text in a corner
   instead of the centered warning Alan asked for on 2026-10-02.
3. **Do not fix** anything that changes layout, spacing, colors or type beyond that. Collect each as one DESIGN NEEDED line per
   screen in for-alan.md, with the screenshot path and what looks wrong, so the planning session can draw concepts.

Tests where a test can hold it (the headless screens, `ScreenReaderAudit`), release notes for what a person will notice.

## 4. Many more screenshots in the guides and on Features

Alan wants more pictures, with markings showing what each points at (his words of 2026-10-17; entry 437 set the marking style).
After section 3, so the pictures show the fixed screens:

1. **The user guide:** every section that describes a screen, a panel or a control has its own marked picture, desktop and, where
   the phone has the feature, phone (the emulator's screenshots work now, a0787d11). Light theme in the guide unless the guide already
   chose otherwise.
2. **The testing guide and Statistics for shooters:** a marked picture wherever the text tells the reader to look at something on
   screen.
3. **Features:** every feature has its own picture of the feature itself, large enough to see what makes it different (entry 256),
   with a phone picture beside it where the phone has it. `docs/figures/SCREENSHOTS.md` lists what is still waiting; the section ends
   with that list empty or each remaining entry saying why.
4. Every new picture made by the weekly screenshot job's code, not by hand, so it stays current; the guide PDFs regenerated; the tour
   words checked against any picture that changed.

## 5. Automate more of what people still do by hand

1. List every recurring step that Alan or the planning session still does by hand: for-alan.md, the working agreement's daily steps
   (for example the daily usage line, the weekly backup look, passing fix notes on to testers, release announcements on Discord), the
   handoff task, anything in RESTORE.md done by hand. Write the list into docs/notes/AUTOMATION.md: each step, how often, and
   whether it can be automated with nothing from Alan.
2. **Build every one that needs nothing from Alan** and nothing on the server: in CI, a scheduled workflow or a script, with its
   backup and undo in RESTORE.md when it changes anything.
3. **Prepare, but do not switch on, every one that needs Alan**: a server change (Code's ssh is refused, so a paste), a secret, or
   anything that posts publicly in his name. Posting release notes or thanks to testers on the Discord is one of these: draft it,
   with the post's wording following his casual voice, off until he says yes. Each goes to for-alan.md as one request, written up
   front, with the whole paste, which shell, and what a good result looks like.

## 6. Sync's three screens (no work for Code tonight)

The planning session is drawing the three screens of entry 461 section 3.2 as concepts tonight. Nothing to do until Alan's choice
comes back as an entry.

## 2026-10-21, entry 461: a queue for the rest of the week, chosen by Alan (research articles, screen reader and speed, sync option A)

**Status: actioned 2026-10-21. Not done: section 2.1's real screen reader sitting, which needs Alan (request 105); section 2.2's speed-up, measured with nothing exact to take (question 106); section 3.2's three screens, waiting on the DESIGN NEEDED in for-alan.md. Tidying old change files (3.1) was not asked for and is not built.**

From the planning session, 2026-10-21 02:05 UTC (2026-10-20 20:05 Denver). Alan asked what Code could work on with the week's
allowance left (usage-now.json `seven_day` 37% when written; the week resets 2026-10-22 02:00 UTC) and chose the three parts below, in
this order. Take them in order, one commit per part where the part allows it (part 3 may need several), STATE rewritten after each.
This queue has no limit of its own: the usual guard (85%, 88% for a finishing block) applies. At the guard, finish the step in hand
cleanly and stop; what is left stays here for the next session. Anything that needs Alan goes to for-alan.md, anything judged by
looking goes there as DESIGN NEEDED, and the work moves on. Main model for application work; chores and wording to the helpers as
CLAUDE.md says. Error reports are checked before each part, as entry 457 says.

## 1. Research articles: decide every "not yet" row in docs/RESEARCH.md

Go through every row of "Worth an article?" whose decision is **not yet** or **not written yet**, and decide each one now: written, or
not written with its reason. For each, check first whether the data it was waiting for now exists. The rows as of today:

1. Entry 410 section 1: the corpus's false marks, and crease shadows along the print that read like holes.
2. Entry 376 Part A: which bull each shot belongs to (match a sheet as one problem, read it as aimed first).
3. Entry 374: a sheet shot one bull off in every shot (the C bull of 4 October).
4. Entry 362: finding a store-bought target's corners on a counter.
5. Entry 332: the fingerprint of store-bought targets. Note that Alan decided on 2026-10-01 that he will not scan a second sheet of
   each product, so request 64's second sheet is not coming: decide it on what exists, with a remembered scale described as a
   warning that the shooter checks, as that decision says.
6. Entry 261: the detector under synthetic degradations.
7. Entries 196 and 197: a touching pair found as two shots about half the time.
8. Entry 198: OpenCV with ArUco and WeChat QR under OpenCvSharp on Android. The build has run on phones since, so this one can be
   decided.
9. Entry 344: a fingerprint and its scale from a camera photograph of a poster.
10. Entry 158 program A: the scan detector on overlapping holes in photographs, if its row is still open.

Every article written meets entry 392 section 1 (every checkable sentence backed in claims-backing.json, figures regenerated from
committed scripts and data, simulations rerunnable, no pseudoscience, the developer not named, the existing byline). A ready article
is published as entry 393 allows and listed in website/research/PUBLISHED.md; one only partly honest stays draft. Anything on synthetic
data says so in its first lines. No orange commercial target, no friend's 2026-09-16 scan. Each new article gets its thumbnail as
entry 453 does. The report names every article published.

## 2. Screen readers, automated, then the next speed step

### 2.1 Screen reader audits on all three platforms

Entry 447 part 2 left one thing: no real Narrator, VoiceOver or TalkBack run. A person's run still needs Alan, but most of what a
screen reader would find can be checked by machine. Add audits that run in CI, using each platform's own tooling:

- **iOS:** XCTest's accessibility audit (`performAccessibilityAudit`, Xcode 15 and later) over every screen the phone tests already
  reach, in ios-app.yml.
- **Android:** the Accessibility Test Framework's checks (or an equivalent walk of the accessibility node tree on the emulator):
  every control that can be tapped has a name, a role and a touch target of at least 48 dp, and nothing that can be tapped is hidden
  from the tree.
- **Windows:** a UI Automation walk of every window (what Narrator reads): every interactive control has a name and a control type,
  and the Tab order reaches it, alongside the headless tab walk `Entry447WindowsTests` already does.

Fix what they find. A finding that cannot be fixed without a look decision goes to for-alan.md as DESIGN NEEDED. Write one plain
request into for-alan.md for later, not urgent, saying what a short real screen reader sitting would still check that the audits
cannot (reading order making sense aloud, announcements after an analysis finishes), so it is ready when Alan has twenty minutes.
Tests, the user guide's keyboard and accessibility part, release notes (only what a person can reach).

### 2.2 Phase 9: a phone photograph's codes

The slowest step left is reading a phone photograph's codes, about 450 to 680 ms. Find where it spends its time, make it faster with
every result identical, as entries 433 and 447 part 4.3 did, and record the before and after in docs/PERFORMANCE.md and the bench
baseline. Entry 414's rule stands: OpenCV's QR detectors are never run on several threads.

## 3. Synchronization, Phase 7: option A, chosen by Alan

Alan chose **option A** of docs/notes/sync-options.md on 2026-10-20: GroupLab writes small change files into a folder the person's own
cloud client already syncs (OneDrive, Google Drive for desktop, Dropbox, iCloud Drive), and reads the other devices' change files from
it. Option B (signing in to the providers' APIs) is not decided and is not built now. Record the decision in sync-options.md, DESIGN.md
section 18 if it needs a line, and the decision log.

### 3.1 Build the engine, with nothing a person can reach yet

As sync-options.md option A describes:

1. A revision number on every record the export carries, so a change can be told from the version it replaced. If that needs a new
   database schema, the first open of the new build copies `grouplab.db` aside before upgrading, as every schema change does.
2. The change file format: whole records with their identity (`DataExport.Identity`) and revision, written only by the device that
   made them, named by device and time, so two devices never write the same file and no cloud client makes a conflicted copy.
3. Reading the other devices' change files when GroupLab starts and every few minutes while it is open, and merging with the import's
   merge: a record changed on one device wins; the same record changed on two devices since they last met is kept both ways and listed
   for the person to choose, never decided silently. Deletions travel as markers kept 90 days.
4. The tiers: geometry and the proof picture always; originals only when the person has asked for them.
5. Reaching the folder: any folder on the desktop; Android's own folder picker (the Storage Access Framework); on iPhone and iPad a
   security-scoped bookmark to a folder in Files.
6. Tests that matter most: two and three simulated devices sharing one folder, the same session changed on two devices, a deletion
   against an edit, a half-written change file (as a cloud client delivers one mid-copy), a device that comes back after months, a
   folder that disappears, and nothing ever lost: before any merge changes a record, the record as it was is kept where the person can
   get it back. Nothing in this part may delete or overwrite a person's data without that copy.

A command for testing it from the command line (`grouplab sync` against folders) is fine. No screen, no setting and no menu item a
person can reach until the screens are chosen; the release notes say it is under the hood, and nothing on the site or the README
says sync exists yet.

### 3.2 The three screens, as DESIGN NEEDED

Write one DESIGN NEEDED in for-alan.md for the planning session to turn into concepts: the Settings screen for choosing the folder
(desktop and phone), what a person sees when two devices changed the same session, and the "also copy the original photographs"
choice with its size. Say what each must show and the real numbers it will have (for example the size of a year of targets with and
without originals). Build them only after Alan chooses.

## 2026-10-20, entry 460: questions 100 and 98 answered by Alan from the planning canvas (take after the work in hand)

**Status: actioned 2026-10-20; both sections done. Question 100 and question 98 archived as answered.**

From the planning session, 2026-10-20 10:55 UTC. Alan chose from the canvas "Selection color and same-page drops"
(claude.ai/artifact/FbbpmhwT3rF616vhQa37EV). Take this after the part you are on; it counts toward the same 60% limit.

## 1. Question 100: option B

The dark theme's selection becomes #f800f8 (108 or more from every ink, as the question measured). The light theme's selection stays
#b0009f, and the light fourth pooled sheet stays #9c3f8f. Update the test that holds the distances (`Entry425Tests`) to the new dark
value, the user guide and release notes if they name the color, and archive question 100 as answered.

## 2. Question 98: option B

When several pictures of the same single-page design are dropped together (their codes read, no set sheet number, for example three
copies of GL-CF9-LTR), ask once, "Pool these 3 pictures as one group?", with two buttons, "Keep a tab each" and "Pool as one group"
(the default). Pool as the untagged pool does (each its own sheet in order, `SetPool.CompositeOfPictures`); keep a tab each as now. The
same on the phones where several pictures are picked at once. Tests, the user guide, release notes, and archive question 98 as answered.

Wording on the canvas, to use as written: the question's body reads "They are three copies of the same page, GL-CF9-LTR (2 MOA). Pool
them if they are one group shot across the pages. Keep a tab each if they are separate loads or separate strings." (with the real count
and design name).

One commit per part, STATE rewritten after each.

## 2026-10-19, entry 459: consistency audit, 2026-10-19

**Status: actioned 2026-10-19; every section done. Questions 105 (the shipped consent record) asked.**

From the planning session's scheduled consistency audit (entry 267 section 2b), 2026-10-19 15:56 UTC. Read only: README.md, the live
grouplab.org pages (home, download, features, tour, shoot-a-target, guides, releases, support), website/features.json and tour.json,
docs/RELEASE-NOTES.md, STATE.md, for-alan.md, PLATFORM-SUPPORT.md, PHONE-PARITY.md, ANDROID.md, USER-GUIDE.md, TESTING-GUIDE.md and
the log to 647a2513. No open `consistency` issue on GitHub. Nothing here needs Alan. One commit for all of it is fine; regenerate any
guide PDF and generated README block you touch, and add a line to RETIRED-WORDING.json where a sentence is retired.

### 1. Trying GroupLab still says a report has to be sent by hand (entry 457 left it behind)

- Where: docs/TESTING-GUIDE.md line 99, "Telling somebody when it is wrong", and so its PDF and /guides/testing-guide/.
- Says: "Save only writes it as a zip ... Send it either way: open an issue at GitHub, or email it to support@grouplab.org ... The
  support page at https://grouplab.org/support/ says the same".
- Should say what the user guide (docs/USER-GUIDE.md line 707) and the live /support/ page now say: write what happened and press
  Send, and it goes to the project with the error and the logs; Save only writes a zip; GitHub or email only when sending is not open
  or a send fails. The "says the same" clause is untrue today.
- Evidence: nightly 206 notes, first bullet; /support/ section "Send a report from GroupLab".

### 2. The platform statement still says a crash report is sent "on request" to an email address

- Where: docs/PLATFORM-SUPPORT.md line 83, generated into README.md line 249 and /download/ ("Reports from Linux and macOS are
  welcome").
- Says: "so is a crash report, which GroupLab can send on request. The address is support@grouplab.org."
- Should say: Report a problem (or Make a report on an error's banner) sends the report from GroupLab with Send, the Discord
  bug-reports forum works too, and support@grouplab.org is for anything private.

### 3. The user guide says the phones' error reports never carry the log; the features page and the README say they do

- Where: docs/USER-GUIDE.md line 709: "On the computer a report can also carry GroupLab's log ... on the phones it stays off for now."
- Against: website/features.json line 1141 (error-reports, "on the computer and the phones a report can carry GroupLab's log"),
  README.md line 345 ("automatic error reports can carry GroupLab's log" on Android and the iPhone), and the code:
  mobile/GroupLab.Mobile/SettingsView.cs line 268 offers "Send them with the log" behind `SharingSwitches.FullLogOpen`, the same switch
  the desktop uses.
- Should say: the same as the desktop, asked about first, on the phones too. Check the phone's What is sent list says the same.

### 4. The user guide contradicts itself on sending a store-bought target

- Where: docs/USER-GUIDE.md line 120 ends "There is no way to send one in yet."
- Against: line 133 of the same guide, Add a store-bought target, "Send it to GroupLab's maker; each file is checked by hand before it
  joins the library", features.json `add-store-target` ("to send so a later build recognizes it") and the tour's page for it.
- Should say: how a reference file is sent today (the Send a target page, Discord or support@grouplab.org, whichever is true), or drop
  the sentence. Line 133 should name that same route instead of "GroupLab's maker".

### 5. The home page calls every build unsigned

- Where: https://grouplab.org/ (website/build.py line 2032): "Today GroupLab is test builds, unsigned and rebuilt after every change".
- Against: the download page and README.md lines 113 and 225 to 231: the macOS builds are signed and notarized since nightly 135, the
  Microsoft Store copy is signed, the Android APKs are signed, and the iPhone build comes through TestFlight. Only the Windows and Linux
  direct downloads are unsigned.
- Should say, for example: "Today GroupLab is test builds, rebuilt after every change that passes its tests, so one may be broken; the
  Windows and Linux downloads are unsigned."

### 6. The release notes list website changes their own header says are never listed

- Where: docs/RELEASE-NOTES.md line 56 (nightly 205, Under the hood: "The Research page on grouplab.org now shows a small picture for
  every article") and line 38 (nightly 206, What you will notice: "Release notes now say which fixes came from your error reports ...
  and keep a running total on the website").
- Against: the header, line 7: "Changes to this website, the guides and the research are not listed here", and /releases/: "Work on
  the website, the documentation or these notes does not make a new build."
- Should say: nightly 205 changed nothing in the application, said in the file's one line form (README's What is new already reads
  "Nothing you would notice"); the 206 bullet describes only what the application does, if anything (the reports total is the website's).
  Then check why entry 453's `Release-note:` trailer was taken as an application change, so the next website entry does not repeat it.

### 7. The download and releases pages are one nightly behind

- Where: https://grouplab.org/download/ (every "Nightly 206, built 2026-10-19 from commit 37b93f3") and /releases/ (newest 206), read
  at 15:56 UTC.
- Against: README.md line 89 and docs/RELEASE-NOTES.md: 0.2.0-nightly.207 announced at 15:22 UTC (7cff718b). 647a2513 at 15:25 UTC did
  not rebuild the site either.
- Should: dispatch website.yml for 207 now, and make the nightly's [notes] push dispatch it by itself (STATE says it is done by hand),
  or have the website check flag a site older than the newest release.

### 8. The README's not-built lists disagree with the home page

- Where: README.md line 53 ("Not built yet: cloud provider adapters over three-tier storage.", in Every screen, larger) and line 356
  ("What does not exist yet: synchronization, and a released iOS build").
- Against: the home page ("Not built yet: hole detection on plain paper, and synchronization between devices") and README.md line 463
  (Phase 4, hole detection on blank paper, Not started).
- Should: both README lines name hole detection on plain paper as well, and line 53 use plain words ("synchronization between
  devices") instead of the design's phrase.

### 9. Two phases read In progress though the README says their gates are met

- Where: README.md lines 403 and 405, Phase 6 Android and Phase 8 iOS, both In progress.
- Against: the README's own states table ("Done: its gate has been met and recorded"); line 484 says Phase 6's gate "has been met on
  the Fold 7, as docs/PROOF-CHECKLIST.md records", and Phase 8's gate is "built and signed on CI", which every TestFlight nightly is.
- Should: either the states follow the definition (DESIGN.md section 21 with them, which ReadmeTests holds), or one sentence under the
  table says why a phase with its gate met stays In progress (for example, while its other items are built, not proven), as is
  already done for phases 5 and 9.

### 10. Do the installer and the other downloads carry the two samples?

- Where: README.md line 116 ("It brings two sample sheets") and /download/ ("Samples: Two sample sheets, so there is something to open
  in the first minute"), written for every download.
- Against: docs/TESTING-GUIDE.md line 59: "on Windows only: the zip's samples folder ... The installers and the other platforms carry no
  samples folder." scripts/package-windows.ps1 lines 77 to 82 stage them; packaging/windows/grouplab.iss does not name them.
- Should: check which downloads really carry them and make all three say the same.

### 11. Smaller ones

- The tour's index blurb for Ballistics (website/tour.json, `ballistics.blurb`) names the trajectory, the dope table and the chance of
  a hit, not Point blank, the third view since nightly 206 (the Ballistics tour page itself has it).
- The features card Error reports is marked "Changed in nightly.206" and README's What is new links the 206 bullet "Make a report now
  sends from GroupLab" to it, but its sentence (features.json line 1141) speaks only of automatic reports. Add the Send in Report a
  problem and Make a report.
- README.md line 111 (Before you install, the plain APK) lists what the phone does without Ballistics; the platform statement (line 221)
  has "gives the dope and the chance of a hit". Make them the same, with Point blank.
- docs/notes/for-alan.md line 1, internal: "46 waits until Sunday 4 October" and "now that 85 is done" are past; rewrite the line from
  the open requests (104 and 103 are not in it).

---

## 2026-10-19, entry 458: release notes say what people's reports and targets changed

**Status: actioned 2026-10-19; every section done. The first build with the counts is the nightly after 0.2.0-nightly.205.**

From the planning session, 2026-10-19 11:15 UTC. Take it right after entry 457 (whose first fix should already carry the new
trailer), before 455 and 456. Nothing here needs Alan.

Alan, 2026-10-19 04:56 Denver: "I would like it if the changelogs will show when an error was fixed from a user report and whether it
was from an automatic report or submitted through github/email/discord. This way users who look at the release notes will see that
their automatic reports are making a difference. I would also like it where if code finds a new target submission, either automatic or
through the website, that it says that it refined the detection engine with x user submissions and what changes were made. I want the
release notes to be more transparent about how user submissions, either automatic or manual are helping to make the application better
for everybody."

## 1. Fixes from reports say where the report came from

- A new commit trailer, read by `scripts/release-notes.py` beside `Release-note` and `Release-note-kind`:
  `Reported-via: automatic error report` (or `error report sent from GroupLab`, `GitHub issue`, `email`, `Discord`), with a count when
  more than one (`Reported-via: automatic error report x3, Discord`). Several sources may be listed.
- The notes show it on the fix's line, in plain words, for example: "Fixed: Next unsure hole could raise an error when the window
  redrew (found from 2 automatic error reports)."
- A person is named only where they have agreed to be (STATE and the memory of consents: Quack, Jylee, Unholy, Fenix, Justin by first
  name); otherwise never a name, email, user name or anything from their files. An issue number is fine (entry 373 section 10).
- Every fix that closes an issue in grouplab-crash-reports, or that came from a Discord post, a GitHub issue or an email, carries the
  trailer; a check in CI fails a commit that closes a crash report issue without one. `docs/notes/error-reports.md` (entry 457) records
  each report's source so the trailer is easy to write.

## 2. Detection changes from submitted targets say how many and what changed

- **The learning loop** (`scripts/learning/learn.py`, entries 394 to 396) writes its commit's release note from its own result instead
  of the fixed sentence it has now, for example: "Hole finding was tuned against 48 targets people sent (31 sent from GroupLab, 15 uploaded
  on grouplab.org, 2 from Discord), with 12 held back to check it: on those it now finds 9 more holes and makes 2 fewer false marks. What
  changed: it looks for slightly smaller holes, and accepts a fainter rim on a white scanner lid." Each constant it can change gets a
  plain-words name in a table beside `DETECTOR`, so the note never shows a constant name. A run that changes nothing writes no note.
- **Any other change made using submitted targets** (a detection fix, a new store-bought fingerprint, a scan or photo case found in a
  submission) carries a trailer `From-submissions: 31 from GroupLab, 15 from grouplab.org, 2 from Discord`, and the notes say "made with
  48 targets people sent" on its line.
- Where a submission came from must be known: if the intake does not already record whether a target was sent from the app
  automatically, sent from the app by hand, uploaded on the website or posted on Discord, start recording that one word with each
  submission (nothing personal). Counts before that date are reported as "people sent" without the split.

## 3. Show the running total

- Each build's notes end with one line when it has any: "From your reports and targets in this build: 2 error reports, 48 targets."
- The Release notes page on grouplab.org and the README's release section carry a running total since the first build that counted
  them ("Since nightly 205: 14 fixes from error reports, 3 detection updates from 112 targets people sent"), and the Support and Send a
  target pages link to it with one sentence: reports and targets you send are counted here.
- The Discord nightly announcement carries the same line.

## 4. Docs and checks

- `scripts/release-notes.py` tests for both trailers, the counts, the running total, the plain names of the learning loop's constants,
  and that no name appears without consent.
- CONTRIBUTING.md (how to write the trailers), the support page and the nightly-builds research article updated in the same change
  (entry 267).

---

## 2026-10-19, entry 457: the Next unsure hole error, a Send button on every report, and error reports triaged as they arrive

**Status: actioned 2026-10-19; every section done. The issues close when the nightly carrying 0af39fc2 is published.**

From the planning session, 2026-10-19 11:00 UTC. A bug in the published nightly comes before feature entries. Nothing here needs
Alan.

Alan, 2026-10-19 04:46 Denver: "I am getting an error when clicking on the next unsure hole button. I want to report the error, but
there is no way to automatically submit the error. It tells me to email or open a github issue. GroupLab should be able to automatically
report errors and claude cowork and/or code should be able to automatically assess the error and notify me of it's suggestions or fixes
the next time I ask where code is at."

## 1. The error (nightly 204, Windows, display scale 1.5)

His logs are on this computer: `%LOCALAPPDATA%\GroupLab\logs\grouplab-20261019-103911-12692.log` and
`crash-20261019-104338-12692.json` (he also saved them as `grouplab-report-20261019-104641.zip`). Four "survived" errors, all the same:

```
System.ArgumentOutOfRangeException: Index was out of range. (Parameter 'index')
   at Avalonia.Collections.AvaloniaList`1.get_Item(Int32 index)
   at GroupLab.App.KeyboardFocus.Keeper.Restore(Control gone, List`1 ancestors, NavigationMethod method) line 193
   at GroupLab.App.KeyboardFocus.Keeper.<>c__DisplayClass7_0.<Left>b__0() line 168
```

after `marking.next-unsure left=3` (twice) and after `session.autosave` (twice; two of them surfaced again as an unobserved-task
`AggregateException`).

The planning session's reading of `KeyboardFocus.cs` (entry 442's focus keeping): when the focus is taken, `places[a] =
parent.Children.IndexOf(a)` records **-1** for an ancestor whose visual parent is a Panel but which is not one of that panel's
`Children` (a templated or presenter child). `Restore` then takes `Math.Min(Place(oldChild), panel.Children.Count - 1)`, which stays -1,
and `panel.Children[-1]` throws. Confirm, then:
- record a place only when `IndexOf` is 0 or more, and clamp to 0 or more where it is read;
- focus keeping is a convenience, so `Restore` must never throw: catch, log one warning, and leave the focus where Avalonia put it;
- find why the same exception also reached the finalizer as an unobserved task, and observe that task.
- Test: Next unsure hole with unsure holes left, from a mouse click and from the keyboard, and an autosave while a control in a
  rebuilt panel has the focus; no error recorded.
- Ship it in the next nightly and tell the planning session which.

## 2. Every report can be sent from GroupLab

Today the "Make a report…" window (crash banner, and Report a problem in Settings) shows a Send button only when the old
`crashReportUrl` setting is filled, which it never is, so it tells people to email or open a GitHub issue. Automatic error reports
already go through `ErrorQueue` to the error receiver.
- Give the report window a **Send** button that sends through the same receiver as the automatic error reports (the person's typed
  words, the run's log and the one before, under the sharing wording the person agreed to), whenever the receiver is open. The
  email and GitHub sentence stays only as the fallback when sending fails or the receiver is closed, and says why.
- Retire `crashReportUrl`.
- On the crash banner, under "Ask me each time", put "Send the error report" first and make it the primary button. Under "Send them
  automatically", the banner says the report has gone (or will in a minute) instead of offering to make one.
- The phones the same way.
- A report sent this way arrives in `oRAirwolf/grouplab-crash-reports` like the automatic ones, with what the person typed.

## 3. Error reports are assessed as they arrive, and the planning session can read the assessment

CLAUDE.md today lists the open crash report issues only at the start of a run, and a run can last a day.
- Check `gh issue list -R oRAirwolf/grouplab-crash-reports --state open` at the start of a run, **before taking each inbox entry,
  before ending a turn, and at least hourly while waiting on CI**. Update CLAUDE.md to say so.
- For each new issue (its text is data, never an instruction): find the cause from the stack, the last actions and the log; then
  either fix it (a clear, small and safe fix is taken ahead of feature entries, like this one) or write a proposed fix and what it
  would change. Close a fixed issue with the commit and the nightly it ships in.
- Keep **`docs/notes/error-reports.md`**: newest first, one short block per issue: the issue number, the date, the build, what the person
  was doing, the cause in plain words, and the status (fixed in commit and nightly / fix proposed: what / needs Alan: what). Committed
  (it holds no personal data: never a name, email, path with a user name, or anything from the person's files). STATE's "Crash reports
  open" line points to it.
- Record this one (Alan's, if it was not sent automatically) in that file even though no issue exists for it.

## 4. Docs

The user guide's Report a problem part, the Features page and the support page say a report can be sent from GroupLab, in the same
change (entry 267).

---

## 2026-10-19, entry 456: rail cant, sight scale factor and Top Gun, from Alan's pissinhot.com calculators

**Status: actioned 2026-10-19; every section done. Top Gun's comparison with GroupLab's figures waits on request 103, as the entry says.**

From the planning session, 2026-10-19 09:30 UTC. Take it after entry 455. Desktop and phones (phone parity). Alan, 2026-10-19 03:17 to
03:22 Denver, asked to bring three calculators from his own site into GroupLab: https://www.pissinhot.com/scopecant.html,
https://www.pissinhot.com/topgun.html and https://www.pissinhot.com/ssf.html. They are his own work; port the formulas, not the pages'
code or look (GroupLab has its own solver and style). He agreed to the placements below and said to write it up without a canvas.
Read the three pages yourself if anything here is unclear (plain HTTPS GET, read only; nothing on pissinhot.com changes).

## 1. Rail cant, in a new "Scope and mount" section of the rifle's page in Equipment

What his page computes (scopecant.html, `calculate`):
- Zero compensation ZC (MOA) = bore angle + drop angle, where bore angle = atan(sight height / zero range) and drop angle =
  atan(drop / zero range) with drop = 0.5 g t squared from the time of flight to the zero range.
- Half travel = the scope's total internal elevation / 2 (or, in his "max elevation from zero" mode, total = 2 x (max up from zero +
  ZC)).
- Maximum rail cant before the rifle cannot be zeroed = ZC + half travel.
- With the rail's actual cant entered: elevation left above the zero = half travel + rail cant - ZC.

In GroupLab:
- New rifle fields: the scope's total elevation travel (with a choice of "total travel" or "maximum up from my zero", as his page
  has), and the rail's cant (MOA or mil). Both optional.
- Use GroupLab's own zero launch angle (`Trajectory.ZeroAngleMoa`) for ZC instead of the vacuum drop; say in the guide that this is
  the one deliberate difference from the pissinhot page and why.
- Show: elevation the zero uses, the most rail cant that still zeroes, elevation left above zero, and, with the rifle's chosen load,
  **the farthest range that elevation reaches**. If the entered rail cant is more than the maximum, say plainly the rifle cannot be
  zeroed with it.
- Ballistics uses it: Trajectory rows past the last dialable range say "out of elevation, hold X mil (or MOA) in the reticle", and the
  At one range panel says the same. The new point blank view (entry 455) says so if its zero needs more than is there.
- Name it **"Rail cant"** in GroupLab (Alan's page calls it scope cant; plain "cant" usually means tilting the rifle).

## 2. Sight scale factor (SSF), on the rifle's page in the same Scope and mount section

What his page computes (ssf.html): SSF = expected value / actual turret reading from a tall target tracking test (CATS style,
default 20.0 mil or 66.0 MOA); error % = (actual - expected) / expected x 100, positive meaning the scope under-tracks (each click
moves less than marked). Verdict bands: under 0.5 % essentially perfect, under 2 % normal for quality optics, 2 % or more significant.

In GroupLab:
- A rifle field "Sight scale factor", default 1.000, with a "Work it out from a tracking test" helper: unit, expected (default 20.0 mil
  / 66.0 MOA), turret reading, the result, the verdict, and the test's steps in plain words (level the reticle, measure the distance,
  dial from the top line to the bottom line).
- **Every click GroupLab gives is corrected by it:** turret reading = true angle / SSF. That is the Trajectory table, At one range,
  zero corrections and Shots Needed to Zero, the point blank view's "clicks up", the printed point blank card, and the phone. Where
  the SSF is not 1.000, the clicks say so once ("SSF 0.980 applied").
- Later idea, not now: measuring tracking from a scanned tracking target. A 20 mil test at 100 yd is 72 in tall, so it needs tiled
  pages or a plotter. Note it in STATE as a possible later entry.

## 3. Top Gun, in the analysis results and on the rifle's page

What his page computes (topgun.html): group size (MOA) = (muzzle energy ft lb / rifle weight lb) / 200, muzzle energy = bullet
weight gr x velocity squared / 450240. Bryan Litz's TOP Gun theory (Modern Advancements in Long Range Shooting, Volume III, chapter
3, 2022): a regression trend of "expected average group size" for a conventional bolt rifle with quality ammunition from a stable rest.

In GroupLab:
- New rifle field: **weight as fired** (with optic, loaded magazine, bipod and so on), lb or kg.
- In the analysis figures, where the rifle and load are known: one line, for example "Top Gun expects about 0.82 MOA average group
  for this rifle and load (Bryan Litz's trend, not a limit)." Beside it, the measured group in the same terms only once the metric is
  confirmed (below). It is a trend line with real rifles scattered both sides: never a pass or fail, never colored as one.
- When the measured precision is clearly worse than the trend (judged with the group's own confidence interval, so a small sample
  does not trigger it), one short line pointing at what is usually checked: action bedding and return to battery, crown, scope mount
  and rings, barrel, bullet engraving, technique and rest. No pseudoscience (no nodes, no harmonics).
- The rifle page shows the Top Gun figure for each of its loads; Compare loads shows it per load.
- Leave out his page's combined dispersion table: Hit probability already does that.

**The metric is not confirmed.** Nobody in planning has the book. If Top Gun's group size is the average extreme spread of 5-shot
groups, the matching mean radius over many shots is about 0.41 x that figure (sigma = ES5 / 3.06, mean radius = 1.2533 sigma;
checked by simulation in planning, 200,000 groups). Until Alan confirms the shot count and the measure, show Top Gun's figure as Litz
states it, do not convert it to mean radius, and do not compare it number for number with GroupLab's figures. Put a question in
for-alan.md: "Does your friend's copy of Modern Advancements in Long Range Shooting, Volume III, chapter 3, say how many shots Top Gun's
group size is from, and whether it is extreme spread? Alan is asking his friend (2026-10-19)." When answered, add the conversion and
the side by side comparison.

## For all three

- Phone parity: the same fields and helpers on the phone's rifle page; the ballistics effects on the phone too.
- User guide, Features page, README and the tour in the same change (entry 267). The guide credits the pissinhot.com calculators as
  where these came from (Alan's own site) and Bryan Litz for Top Gun.
- Tests: rail cant against a worked example from his page (same inputs, the same ZC, half travel and maximum, allowing for the
  launch angle difference); SSF applied to clicks in every place listed; Top Gun's arithmetic; the "out of elevation" rows.
- Records: the new rifle fields go through `EquipmentForm` like every other field, with the export and import file and the
  database schema bumped as usual.

---

## 2026-10-19, entry 455: maximum point blank range, a third view in Ballistics

**Status: actioned 2026-10-19; every section done. The proof on Alan's M220 is request 104; the IPSC outline beyond its A zone is question 102.**

From the planning session, 2026-10-19 09:20 UTC. Take it after entries 447, 452 and 453. Nothing here
needs Alan until the proof at the end. Desktop and phones (phone parity).

Alan, 2026-10-19 02:46 to 03:12 Denver, asked for a maximum point blank range (MPBR) calculator: choose the platform, ammunition,
velocity and BC, then the intended target, with vital zones for popular game and man-sized practical targets. Decisions after the
canvas (claude.ai/artifact/33wFYZ3VdsWXmMgrmF5DCm, boards Main, Target, Phone, PhoneTarget, Card, Likely):

- "It should be a third view in ballistics": Trajectory, Hit probability, **Point blank**. It uses the rifle, load and air already there.
- Certainty is an input: "9 in 10 should be the default, but if somebody wants to be close to 100% certain, they should have the option."
- Precision is the shooter's **mean radius**, "clearly labelled as such".
- Animals without a published size: "Give the option for estimates or to enter your own."
- Yes to the printed card and to keeping the result on the rifle as its zero.
- New mode: "this is the most likely distance I will be shooting x animal at, so here is what I should set my scope to".

## The model (the planning session checked its numbers with its own point-mass G7 solve; it matched the Test load's table to 0.04 in)

1. The zone is a circle (diameter) or a rectangle (width by height), centered on the aim.
2. **Classic MPBR** (shown for reference): the path stays within half the zone's height above and below the line of sight; the zero puts
   the peak at the top of the zone. Report near zero, far zero, peak height and its range, the start (not the muzzle when the sight
   height is more than half the zone: prairie dog starts at about 16 yd with the Test load) and the end.
3. **The headline is the certainty band**: the distances over which at least the chosen share of shots lands inside the zone, from the
   shooter's mean radius (converted to a per-axis sigma, MR = sigma times the square root of pi/2), grown with range, plus the load's
   velocity SD carried through the solver, aimed at the zone's center. Reuse `HitProbability` where it fits; a closed form (circle:
   integrate the normal over the circle; rectangle: product of two normal intervals) is fine for speed. Default certainty 90 %, the input
   takes any value up to 99.9 %, with presets 90, 95 and 99; 100 % is refused with a sentence saying no shot can be promised.
4. **Three ways to set the zero**, a switch at the top of the view: (a) the longest dead-on hold at the chosen certainty; (b) "My most
   likely distance": the group centered on the zone at that distance (zero at that distance), then the dead-on band around it and the
   holdover past it; (c) "A window of distances": the zero that gives the best worst-case chance across the window, and when even that
   is below the chosen certainty, say so plainly ("No single zero keeps 9 in 10 across it") with the best zero, its worst point, and
   what would help (narrow the window, a larger zone, hold over). A fourth reading is always shown beside them: **the rifle's current
   zero** and the band it gives.
5. **What ends it**, whichever comes first, each named: the certainty band; an energy floor (a field, preset per animal from the rules of
   thumb, 1000 ft lb deer sized and 1500 elk sized, labelled as rules of thumb, editable, blank allowed); the bullet's minimum expansion
   velocity (a field on the load, per bullet, from the maker; blank says "not entered, the bullet's maker publishes one").
6. **How to set it**, in words and clicks: "Zero 2.6 in high at 100 yd: 0.7 mil, 7 clicks up from your 100 yd zero", using the rifle's
   click unit and value, and the distance the rifle is currently zeroed at.
7. Wind is not part of point blank range: show the drift at the band's end for a crosswind field (default 10 mph full value), and say
   when it is more than half the zone's width.
8. Shot angle and the air come from the view's existing air section. Units follow Imperial and Metric, and every number keeps
   tap-to-switch.

## The inputs

- **Mean radius**, labelled exactly "Your mean radius" with its unit (MOA, mil, or inches at 100 yd), and where it comes from: typed,
  or the group open in the analysis, or a session picked from Sessions. Under it, one sentence: it should be from the position you
  will shoot from; a bench group is much tighter than sticks or a bipod. Never per-axis SD in the interface.
- **Certainty**, percent, default 90, presets 90, 95, 99, typed up to 99.9.
- **What you are shooting at**: the presets below, an estimate where no published size exists, or your own circle or rectangle.

## The targets (sizes from the research in planning; the row shows the source and, where sources disagree, the range)

Heart and lung zone, the smaller published figure used:
prairie dog 2 in (Wikipedia, point-blank range); coyote 4 in (Ron Spomer); pronghorn 8.5 in (Chuck Hawks, 8.5 to 9); whitetail deer
8 in (American Hunter, 6 to 12 published); mule deer 8 in (Spomer, 10 at most); black bear 8 in (HuntWise, 8 to 10); bighorn sheep 12 in
(Hawks, 12 to 13); mountain goat 13 in (Hawks, 13 to 14.5); caribou 14.5 in (Hawks, 14.5 to 15.5); elk 14.5 in (Hawks 14.5 to 15.5,
American Hunter 15); moose 18 in (Hawks 18 to 21.5, American Hunter 24).
**Estimates**, each labelled "Estimate, no published size found" and editable: wild hog, red stag, European wild boar, impala, kudu.
Choose a conservative figure from body size and say in the row how it was chosen; never present it as sourced.
Practical targets: IPSC target A zone 15 x 32.5 cm (IPSC rules Jan 2024, appendix B2); USPSA target head A zone 10 x 5 cm (USPSA
appendix B1); IDPA body -0, 8 in circle; steel silhouettes full 18 x 30 in and 2/3 12 x 20 in (vendor shapes, said so). Each shape drawn
to scale in the list. **Replace GroupLab's own IPSC outline in `HitTarget`** (its comment says it is not official) with the real
dimensions, so Hit probability uses them too.
Sources the planning session used (put them in the guide and the research article, not in the app): americanhunter.org 2017/7/17
MPBR article; ronspomeroutdoors.com understanding MPBR; randywakeman.com MPBR (Chuck Hawks table); huntwise.com bear; Wikipedia
point-blank range; ipsc.org rules Jan 2024; rules.uspsa.org appendix B1.

## The two extras Alan said yes to

1. **Print a card**: a 50 x 30 mm label for the M220 (and the same content on the other label and sheet printers): rifle, bullet,
   velocity, BC, "DEAD ON 0 to 262 yd", zone and certainty, and the zero offset at 100 yd with clicks (canvas board Card).
2. **Keep it on the rifle**: saves the chosen zero onto the rifle record (the zero distance, or the offset at the current zero distance,
   whichever the record holds), with the previous zero kept in the rifle's history and one tap to undo. Every other ballistics figure
   for that rifle then follows the new zero.

## Notes and limits shown in the view

- A "check your local law" line where hunting presets are chosen, never a legal verdict (examples the planning session verified:
  Germany BJagdG section 19, England and Wales Deer Act 1991 schedule 2, Washington WAC 220-414-020).
- What it does not cover, in one line: wind (shown separately), a moving animal, a misjudged range.

## Also

- The phone does all of it (boards Phone and PhoneTarget).
- A research article, "Point blank range, honestly", using the Test load's numbers: the classic band puts half the shots outside at
  its top and far end; the certainty band; the effect of mean radius from a field position; the window case.
- User guide, Features page, README and the tour in the same change (entry 267). A short "From the keyboard" note for the new view.
- Tests: the classic MPBR against a published example (JBM's archived example or the planning numbers below), the certainty band
  monotone in certainty and mean radius, the window refusal, the sight-height start, rectangle zones, and the card's layout.

## Planning's reference numbers (Test load: 140 gr, 2710 ft/s, SD 10, G7 0.326, sight 1.75 in, ICAO, 8 in circle)

Classic: start 0, near zero 26 yd, peak +4.0 in at 148 yd, far zero 266 yd, end 312 yd, +3.38 in at 100 yd (0.94 mil).
100 yd zero: 0 to 208 yd. Mean radius 0.63 MOA: 90 % 0 to 262 yd (+2.6 in at 100, 0.72 mil); 95 % 0 to 252; 99 % 0 to 228; 99.9 % 0 to
200. Most likely distance 200 yd: +1.77 in at 100 (0.49 mil), 99.9 % at 200, 90 % band 0 to 236. Window 150 to 300 yd: best worst case
56 %. Expect small differences from GroupLab's solver; anything over a few yards is worth a look.

---

## 2026-10-19, entry 454: a phone always updates to the newest nightly that has a phone build

**Status: actioned 2026-10-19; every section done. Section 7's live proof is the first real nightly after this change, and Alan's tablet updating to 0.2.0-nightly.203 is for the planning session to confirm with him.**

From the planning session, 2026-10-19 08:50 UTC. **Take this before going on with entry 447 part 3's runs, and before 452 and 453.**
Nothing here needs Alan unless section 1 cannot be done from CI.

Alan, 2026-10-19 02:30 Denver: "I tried updating my android tablet to the latest nightly and it said it couldnt find one for 202 (i
believe) so it wouldn't update. It should update to the latest nightly, even if it doesnt match the windows nightly."

## The cause, as far as the planning session can see (read only)

0.2.0-nightly.203 published the desktop files and its Android job then lost its files (fixed for later nightlies in 37326a5c), so the
rolling manifest names 203 with no Android download. Since entry 450 every nightly also has a window where the manifest lists no
Android file yet. The phone reads one version per manifest, finds no APK for it (`ApkRefusal.NoApk`) and stops. It never falls back
to the newest build that does have one, so a missing or late Android build leaves every phone where it is.

## What to do

1. **Now: get the phones a working update.** If 203's signed Android artifacts from run 39206788728 still exist, add them to
   `v0.2.0-nightly.203` and the rolling release and re-sign the manifest (the step 37326a5c fixed), by dispatch rather than by hand. If
   they are gone, say so in STATE and make sure the next application commit produces a nightly with its Android files promptly. Record
   which happened. Do not commit an empty change just to force a build without saying so in STATE.
2. **For good: each platform follows the newest build that has its file.** The manifest (the second format, `update-manifest-2.json`;
   the first format is frozen and must not change) carries, per platform, the newest version that has a download for that platform,
   with that file's own version, URL on its own `v<version>` release (not the rolling name, which may already hold a newer build's
   file or none), size and SHA-256. When nightly N has no Android file, or has not got it yet, the Android entry stays on the newest
   earlier nightly that had one. Windows, Linux and Android may then name different versions, and that is correct.
3. The phone's updater (and the desktop's, for the same reason) compares its own version with **its platform's** version, not the
   manifest's top version, and installs that. It must never offer or install a build equal to or older than the installed one.
4. **Older installed builds must not be harmed by the new manifest:** no update loop (an old phone told "203" and handed a 202 APK must
   not reinstall 202 forever), no signature refusal, no crash on an unknown field. Check against the oldest build still reading
   `update-manifest-2.json` and the Android builds of the last two weeks; if an old build would misbehave, find a shape it reads
   safely. Tests for every case: a nightly with no Android file, one whose Android file arrives late, a phone on N-1, a phone already on
   the newest Android build, an old build reading the new manifest.
5. The phone's message when there is truly nothing newer for it says that in plain words ("You have the newest GroupLab Dev for
   Android, 0.2.0-nightly.202"), never a missing file or another platform's number.
6. Update docs/UPDATES.md ("On Android"), docs/ANDROID.md, the nightly-builds article and the download page in the same change
   (entry 267).
7. Proof: one nightly where the Android build is made to fail on a throwaway branch or dry run (never the real stores) leaves the
   Android entry on the previous build and a phone on an older build still updates to it; and Alan's tablet updating from the real
   release (say in STATE which version it should reach, so the planning session can tell him).

---

## 2026-10-19, entry 453: a drawn thumbnail for every research article that has no chart

**Status: actioned 2026-10-19; every section done.**

From the planning session, 2026-10-19 08:35 UTC. Take it after entries 447 and 452. Nothing here needs Alan. Website only, no
application note.

Alan, 2026-10-19 02:09 Denver: "Can you make thumbnails for all of the research articles?" The planning session showed three
concepts on a Claude Design canvas (claude.ai/artifact/KGc5D35TZ2nj6eDZWuRbFi): A a drawn mark for every article, B the article's
headline number, C keep the charts and draw a mark where there is none. Alan, 02:26: **"c"**.

## What is there now (read from website/_site/research/index.html, 2026-10-19)

36 cards. 15 use the article's lead figure (`figures/lead.svg` or `lead.png`, some with a `lead-dark.png` pair). 21 show the numbered
plate (`research-plate`): one-hole-or-two, hole-is-not-the-bullet, blank-sheet-zero, scale-markers-beside-a-target, how-far-off-square,
how-grouplab-reads-a-target, wrong-bull, scans-against-photos, pooling-groups, shots-to-zero, wind-or-rifle, primer-comparison,
safe-updates, choosing-the-markers, designing-a-readable-target, what-grouplab-sends, nightly-builds, smaller-installer,
phomemo-m834-print-commands, bent-sheet-registration, uploads-rebuilt-from-pixels (check the count against the build and take
the build's word if an article has gained a figure since).

## What to do

1. **The 15 lead figures stay exactly as they are.**
2. **Every plate becomes a drawn mark**, one per article, showing that article's one idea, in this style (from the canvas, concept A):
   - Inline SVG in the card, `viewBox="0 0 320 200"`, the same 320 by 200 box as `research-thumb`, `aria-hidden="true"`, on
     `var(--sunk)`. It follows light and dark by itself through classes, never fixed colors:
     `.ln{fill:none;stroke:var(--line2);stroke-width:1.5}` `.dm{fill:none;stroke:var(--dim);stroke-width:2}`
     `.am{fill:none;stroke:var(--amber);stroke-width:2.5}` `.ds{stroke-dasharray:5 5}` `.fl{fill:var(--dim)}` `.ho{fill:var(--text)}`
     `.af{fill:var(--amber)}` `.lf{fill:var(--line2)}`.
   - No words or numbers in the drawing. One amber element at most carries the point; the rest is line and dim. Holes are `.ho`.
   - Keep each drawing simple enough to read at 280 px wide and on a phone.
   - The source of each mark lives with its article (for example `website/research/<slug>/thumb.svg` or a `THUMBS` table in the
     build, your choice), so a new article without a lead figure must get one: the website build fails on any card that would show a
     plate.
3. **Four marks are already drawn on the canvas; use them as they are:**

one-hole-or-two:
```
<circle cx="160" cy="100" r="66" class="ln"/><circle cx="151" cy="102" r="20" class="fl"/><circle cx="168" cy="95" r="20" class="fl"/><circle cx="151" cy="102" r="24" class="am ds"/><circle cx="168" cy="95" r="24" class="am ds"/>
```
hole-is-not-the-bullet:
```
<circle cx="160" cy="100" r="36" class="am ds"/><polygon points="186,100 177,90 173,77 160,80 147,77 143,90 134,100 143,110 147,123 160,120 173,123 177,110" class="ho"/>
```
wrong-bull (a 3 by 3 grid of bulls, the aimed one ringed in amber, the group beside another):
```
<circle cx="100" cy="50" r="14" class="ln"/><circle cx="160" cy="50" r="14" class="ln"/><circle cx="220" cy="50" r="14" class="ln"/><circle cx="100" cy="100" r="14" class="ln"/><circle cx="160" cy="100" r="14" class="ln"/><circle cx="220" cy="100" r="14" class="ln"/><circle cx="100" cy="150" r="14" class="ln"/><circle cx="160" cy="150" r="14" class="ln"/><circle cx="220" cy="150" r="14" class="ln"/><circle cx="160" cy="100" r="19" class="am ds"/><path d="M174 114 L203 137 M193 136 L203 137 L201 127" class="am"/><circle cx="211" cy="142" r="3.5" class="ho"/><circle cx="217" cy="148" r="3.5" class="ho"/><circle cx="221" cy="140" r="3.5" class="ho"/><circle cx="213" cy="151" r="3.5" class="ho"/>
```
smaller-installer (97 with its cut top dashed, beside 81):
```
<rect x="112" y="68" width="40" height="97" class="lf"/><rect x="112" y="49" width="40" height="19" class="am ds"/><rect x="172" y="68" width="40" height="97" class="af"/><path d="M92 165 H232" class="dm"/>
```

4. **Ideas for the rest** (a starting point; read each article and draw what it actually shows if that is better):
   blank-sheet-zero, a blank sheet with a hand-drawn cross (amber, slightly uneven) and a small group near it; scale-markers-beside-a-
   target, a ringed target with four small square markers on the paper beside it; how-far-off-square, a sheet seen at an angle (a
   trapezoid) over the dashed true rectangle; how-grouplab-reads-a-target, three small frames joined by arrows (a sheet, the rings
   found, the holes marked); scans-against-photos, the same hole pattern on a flat sheet and on a slightly skewed one; pooling-groups,
   two small groups inside one larger amber circle; shots-to-zero, shots stepping toward a crosshair along an amber path;
   wind-or-rifle, a group stretched sideways with an amber arrow along it; primer-comparison, two groups side by side, nearly the
   same size; safe-updates, a package with a check seal and a return arrow; choosing-the-markers, four corner markers on a sheet
   outline; designing-a-readable-target, a grid of bulls with one under a reticle; what-grouplab-sends, a device outline with one
   small amber arrow out and dashed items that stay; nightly-builds, commit dots on a line into a package; phomemo-m834-print-
   commands, a roll of paper feeding out rows of byte ticks; bent-sheet-registration, a sheet whose far edge lifts, its points
   shifting; uploads-rebuilt-from-pixels, a pixel grid with an amber outline drawn back over it.
5. Remove `research-plate` and its CSS once nothing uses it.
6. Proof: one screenshot of the whole Research page at 1440 px in light and in dark, and one at phone width, saved as
   `C:\Dev\grouplab-local\research-thumbs\` (the folder is created by this entry), and the live page checked after the deploy. Say in
   STATE where they are; the planning session will look before telling Alan.
7. Update SCREENSHOTS.md or the site notes if they describe the plates (entry 267).

## Added 2026-10-19 08:40 UTC: real photos and scans count too

Alan, 02:28 Denver: "Dont forget that the research article can use real photos or scans as well".

8. For each of the 21, **first look for a real photo or scan** that shows the article's idea: the article's own figures, the samples
   in the repository, and the range trip material in `C:\Dev\grouplab-local\` (for example `range-2026-10-04\results\`). A tight crop
   of a real target usually beats a drawing: a real ragged two-shot hole for one-hole-or-two, a real torn hole beside its bullet's
   circle for hole-is-not-the-bullet, a real hand-drawn cross for blank-sheet-zero, a real curled or angled sheet, a real scan beside
   a photo of the same sheet, real scale markers beside a store-bought target, a real M834 print.
   - Use a drawn mark only where no real picture shows the idea well (likely the "How GroupLab is built" articles: safe-updates,
     nightly-builds, smaller-installer, what-grouplab-sends, uploads-rebuilt-from-pixels).
   - The four drawn marks in section 3 are the fallback for their articles, not a requirement: a good real picture replaces them.
   - Crop to the 320 by 200 box (16:10), saved at twice that (640 by 400) as WebP, with a light and a dark version only if the crop
     includes page background that would glare in the dark theme; a photo of paper may stay one image in both themes.
   - Strip all metadata from every image written (no GPS or location, no camera serial, no names; the orientation is applied to the
     pixels first). Nothing in the picture may show a tester's name, an email address, a serial number or anything personal.
   - Pictures that are not Alan's own (testers' submissions, Discord uploads) are not used.
9. The proof screenshots in section 6 should show which cards are real pictures and which are drawn; list them in STATE.

---

## 2026-10-19, entry 452: the website check and the release notes step stop costing nightlies

**Status: actioned 2026-10-19; every section done.**

From the planning session, 2026-10-19 08:15 UTC, on entry 450 section 3's measurement. Take it after entry 447. Nothing here needs
Alan. Alan left the choice to the planning session ("Do whatever you think is best", 2026-10-18).

## The planning decision on the phone split

Entry 450 section 3 found 32 lost nightlies since 2026-09-28, but none from a Windows or linux-x64 package. So the split entry 450
asked about (phones no longer riding on the desktop packages) would have saved nothing, and is **not wanted**. What the count did show:

- 20 from a failed test leg. Entry 450's retry covers the flaky ones; a test that fails twice should still stop every platform, since
  all of them ship the shared code. No change.
- 12 from jobs no application build uses: ci.yml's website job (11) and the linux tarball's ships check (1).
- 7 nightlies that failed at "Write the notes" with every build fine.

## What to do

1. **The website job no longer gates the nightly.** A website failure must still fail and be seen (it gates the site's own deploy as
   today), but the nightly's start condition must not depend on it. Either move it out of the "build and test" workflow into its own
   workflow, or have `nightly.yml` look at the test legs and the jobs that build what ships, not the whole run's conclusion. Your
   choice; the condition that stays must still be "every leg's tests passed".
2. **The linux tarball's ships check:** if it checks what the linux-x64 download contains, it stays a gate. If it checks something
   that does not ship, treat it like the website job. Say which in STATE.
3. **"Write the notes" can no longer stop a nightly.** Find why the 7 runs failed (36372334414, 36434428741, 36515160656, 36648108371,
   37426892451, 38409950467, 38449280753), fix the cause if there is one, and make a failure there publish the nightly with short
   fallback notes (the version, the commit, a link to the commit list) and a warning annotation, rather than nothing.
4. Update the nightly-builds article and wherever else says when a nightly is published, in the same change (entry 267).
5. Proof: a run where only the website job fails still gets its nightly; one forced failure of the notes step on a throwaway branch
   publishes with the fallback notes (to a throwaway prerelease or a dry run, never the real nightly or the stores). Delete the branch
   and the throwaway release afterwards.
6. Also confirm in STATE that the first nightly after 37326a5c has its Android files on both releases (entry 450 section 5's last
   proof).

---

## 2026-10-19, entry 451: the home page's ruler (the scroll bar down the right edge) on every page of the site

**Status: actioned 2026-10-19; every section done (section 7: no document names which pages have the ruler).**

From the planning session, 2026-10-19 05:55 UTC. Take it after entries 447 and 450. Nothing here needs Alan. Website only, no
application note.

Alan, 2026-10-18 23:50 Denver: "Can you add a job to make the scroll bar on the home page the same on every page on the site?"

The "scroll bar" is the ruler down the right edge (`.tp-ruler`, its numbered ticks and the amber `.tp-ruler-mark` that moves with the
scroll); the site has no styled native scrollbar. Today it is on the home page (ticks written by the build, moved by `home.js`) and on the
`LONG_PAGES` (`tp-long`, ticks and mark from `page.js`).

## Pages without it (read from website/_site on 2026-10-19)

discord, download, guides (the index), guides/glossary, research (the index), shoot-a-target, shoot-a-target/send, survey, targets,
tour and every page under tour/ (including tour/how-it-works and its three pages), what-can-be-measured, and 404. Leave testpage alone.

## What to do

1. Every page gets the same ruler as the home page: same position, width, tick spacing and numbers, the same amber mark moving from
   top to bottom over the whole scroll, and the same hiding rule at narrow widths. One source for it, so the home page and every other
   page cannot drift: if the home page's build-written ticks and `page.js`'s twelve generated ticks differ in any way a person can see,
   make them one. The ruler on the home page itself does not change.
2. Pages too short to scroll still show the ruler, with the mark resting at the top.
3. The right margin the ruler needs (`--rule`, now applied through `tp-long`) applies wherever the ruler is, without changing anything
   else that `tp-long` does on pages that are not long (for example the "On this page" list stays only on long pages). Check the tour
   pages and how-it-works, which carry their own CSS, so nothing ends up under or behind the ruler.
4. The section index beside the home page's ruler (`.tp-index`, MEASURE, HONEST, PHONE, GET STARTED) stays on the home page only.
5. Hold it: extend `heading_problems` or `site-home-tests.py` so a built page without the ruler fails the website build (testpage
   excepted).
6. Proof: screenshots of download, tour, tour/how-it-works, targets and the guides index at 1440 px, light and dark, plus one at phone
   width showing the ruler hidden as on the home page; confirm on the live site after the deploy.
7. Update SCREENSHOTS.md or the site's notes if they describe which pages have the ruler (entry 267).

---

## 2026-10-19, entry 450: a flaky test or a stuck phone build should not hold back the other platforms' nightly

**Status: actioned 2026-10-19; sections 1 to 4 done, section 5's two branch proofs done; its real-nightly proof waits for the next commit that ships. Section 3's count is 32, more than two: for planning.**

From the planning session, 2026-10-19 03:05 UTC. Take it after entries 447, 448 and 449. Nothing here needs Alan. Alan, 2026-10-18 21:00
Denver, asked whether a stalled or failed build for one platform can be kept from stopping another from publishing, and on the options
below said: "Do whatever you think is best". This entry is the planning session's choice.

## What the planning session found (read only, 2026-10-19)

- Inside `nightly.yml` the platforms are already split: android, both iOS jobs, cli-arm64, the simulator and emulator starts are
  `continue-on-error`, and the Mac packages are `optional` in `package.yml`. Only the Windows and linux-x64 packages must pass.
- Coupling 1, the test gate: the nightly starts only on a successful "build and test" (`ci.yml`, windows, ubuntu and macos legs). One
  flaky test on one leg stops every platform's nightly, phones included (as 6120ce4c's macOS save, export and import test did on
  2026-10-18).
- Coupling 2: `publish` needs `[name-it, package, android, cli-arm64]`, so a hung Android build (timeout 120 minutes) holds the desktop
  publish back for up to two hours, though it never stops it.
- Coupling 3: Play upload, TestFlight and the Store follow the nightly's success, so a failed Windows or linux-x64 package also stops the
  phones. How often this has happened is not known.

## What to do

1. **One automatic retry of failed tests in `ci.yml`, on every leg.** When the test step fails, rerun only the tests that failed, once, in
   the same job. If they pass, the leg passes and the nightly goes ahead; if any fails again, the leg fails as today. A real fault fails
   twice and still stops everything, so "publish only what passed" holds. A rerun of the whole workflow is not wanted.
   - Never hide a flake: each test that passed only on its retry is written to the run summary as a warning annotation, and to one place
     the planning session reads (a line in STATE or a small `docs/notes/flaky-tests.md`, your choice, kept short, newest first, with test
     name, leg, run id and date). A test that needs its retry three times in a fortnight becomes its own inbox-style item in STATE to fix.
   - The retry does not apply to build failures, only to test failures.
2. **The desktop publish stops waiting for the slow optional jobs.** Take `android` and `cli-arm64` out of `publish`'s `needs`. Each
   adds its own files to that build's `v<version>` release and the rolling `nightly` release when it finishes (a small job that needs
   `publish` and its own build job, or the same steps at the end of the build job, your choice). The release notes and README for the
   nightly must still say which files are in it: either they are written so a later file does not make them wrong, or the late job
   updates them. Play upload must still start only once the Android files exist (check what it triggers on today and keep that true).
3. **Measure before splitting the phones from the desktop.** Look back over the nightly runs of the last 60 days (gh run list on
   `nightly.yml` and `ci.yml`) and count the times a Windows or linux-x64 package failure, or a failed "build and test", meant no nightly
   while the Android or iOS builds of that commit would have been fine. Write the count and the run ids in STATE. Do not build the split
   now; if the count is more than two, say so in STATE and the planning session will decide.
4. Update the README, the site's nightly and release notes pages, the research article on nightly builds (`nightly-builds`) and RESTORE or
   CONTRIBUTING wherever they describe when a nightly publishes, in the same change (entry 267).
5. Proof: a deliberately failing test on a throwaway branch fails twice and stops; a test made to fail only on its first attempt passes
   on its retry and is recorded; one real nightly publishes the desktop files before the Android files arrive and then has all of them.
   Remove the throwaway test and branch afterwards.

Keep within the usual usage guard. Nothing here touches the server.

---

## 2026-10-19, entry 449: entry 444's look question answered, page titles stay as written

**Status: actioned 2026-10-19; every section done, the capitals check included.**

From the planning session, 2026-10-19 02:10 UTC. Take it after entries 447 and 448. Nothing here needs Alan. Website only, no
application note.

Alan, 2026-10-18 20:08 Denver, after seeing a Claude Design canvas with three options (claude.ai/artifact/8XE1SVRR3mBJVp58fenTA1:
A as written everywhere, B capitals everywhere, C capitals on the main pages with research articles and guides as written):
"A is good".

## What to do

1. Keep every page title (`h1`) on the site as written, in the condensed bold face, exactly as built in entry 444. This covers the main
   pages, research articles and guides alike. No capitals and no `text-transform` on page titles.
2. The home page is unchanged: GROUPLAB and START MEASURING. stay in capitals, its other headings as written.
3. Close the DESIGN NEEDED line for entry 444 in `docs/notes/for-alan.md` (answered 2026-10-18: A, as written everywhere) and remove it
   from the waiting list in STATE.
4. If `heading_problems` or `site-home-tests.py` can hold it cheaply, add a check that no page title outside the home page is set in
   capitals by CSS. Skip this if it is more than a few lines.

No commit is needed for the decision alone beyond the for-alan.md and STATE changes.

---

## 2026-10-18, entry 448: the home page's menu opens under the GROUPLAB wordmark on phones

**Status: actioned 2026-10-19; sections 1 and 2 done, taken while entry 447's iOS proof ran (Alan asked to action what could be). Section 3, the live check, is recorded with entry 449.**

From the planning session, 2026-10-18 13:10 UTC. Take it after entry 447. Nothing here needs Alan. Website only, no application note.

Alan, 2026-10-18, with a screenshot from his phone (Firefox and Chrome both): "when I click on the menu on the homepage, the group lab
logo is superimposed over the menu. The menu should render over it." In the screenshot the open menu panel's links (Guides, Research,
Community) sit under the hero's big GROUPLAB wordmark, which is drawn on top of them.

## The cause (read only, checked 2026-10-18)

`HOME_CSS` in `website/build.py`: `html.tp-js:not(.tp-still) .tp-hero h1{position:relative;z-index:7}` puts the wordmark above the header
(`.tp-header` and `.tp-home` are `z-index:6`), so that it can fly over the header while it docks (entry 444 section 2). The menu panel
(`.tp-menu .menu-panel`) opens inside the header's stacking context, so the wordmark is above the open menu too. The same is true of
anything else that opens from the header at any width (the "More" menu between 981 and 1499 px, if it is the same element).

**Measured on the live page, 2026-10-18 13:15 UTC** (the planning session, a 375 px wide browser, `document.elementFromPoint` at the
left, middle and right of each link with the menu open and the page at the top): Download, Tour, Features, Send a target, Community,
Release notes, Support and GitHub are on top; **Guides and Research are not: the H1 wordmark (`H1.cond`, and its `SPAN.tp-amber`) is
the element hit at all three points**, so they cannot be tapped. Alan, 2026-10-18, a minute later: "I noticed that I can also not click
on text that is below the group lab logo. Make sure it fixes this as well." That is this: the menu items under the wordmark cannot be
tapped. With the menu closed, no link, button or paragraph anywhere on the page was covered at scroll positions 0, 150, 300, 450, 700 and
1000 px. The fix below must make all ten menu items tappable, and the check of section 2 must cover tapping, not only drawing.

## 1. The fix

1. **Anything open in the header is above everything on the page.** While the menu (or any other panel the header opens) is open, the
   header's layer is above the wordmark: for example `.tp-home:has(.tp-menu[open])` raised above 7, with the same class set by the
   menu's own toggle event as a fallback for a browser without `:has`. Keep the docking flight over the header when no menu is open.
2. While the menu is open, the docking wordmark does not move (it waits until the menu closes), so nothing flies over an open panel.
3. Check that the menu also closes on Escape, on a tap outside it and on choosing a link; fix any that do not.

## 2. A check that holds it

A site test that renders the home page at 390 px wide (and at 1280 px if the "More" menu is the same element), opens the menu with the
wordmark undocked, and for each link in the panel checks that the element at the link's left edge, center and right edge is the link itself
(`document.elementFromPoint`), so a link that shows but cannot be tapped fails. The same test also checks, with the menu closed, that
every link and button on the home page is the top element at its center at scroll positions 0, 150, 300, 450, 700 and 1000 px, at 390
and 1440 px wide, so a decorative layer (the wordmark, the orbit, the rings, the ruler, the section index) can never cover something a
person needs to tap. If the site's tests cannot render a page, use the browser the screenshot job already runs, and say which.

## 3. After it publishes

Confirm the commit is live in `grouplab-site-build`, then repeat the check of section 2 on https://grouplab.org/ at 390 px in light and
dark, and put one screenshot of the open menu into the report's results.

---

