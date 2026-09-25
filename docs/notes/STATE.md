# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-25, after entries 211 and 212.

---

## In flight

- Done this run: 171, 173, 164, 174 to 185, 166, 169, 170, 159, 154 to 158, 165, 186 to 212 (154's choice of hole centre is question 51), and 172 in part:
  its measurements wait on entries 157 and 158, because GroupLab cannot yet find holes on a sheet it did not print.
- **Where a hole's centre is**, question 51: the reported centre leans toward the scanner's shadow by about 0.011 in, the
  same way on every scan; the choice of a replacement waits on request 9's hand markings.
- **A sheet with one scoring bull takes a group** (entries 196 and 197): no count, no limit, nothing to review for it.
  The zeroing grids are for sighting in by eye; no more work makes them a scanning target.
- **Minimums** are in PLATFORM-SUPPORT.md (entry 207): Android 10, 4 GB; the survey is designed in `docs/SURVEY.md` and built with
  the next desktop work, its opt in on the one first run window with the other two (entry 208).
- **The composite plot** (entries 204 and 210): wide mid grey rings in page units, Group or Whole target with zoom, half strength outlines, green CEP 50, 90 and 95 on toggles, green and blue lines
  across the plot for the centre and the aim; the report and Compare follow the same toggles.
- **Radios and check boxes with long words take `Wrapped(words)`** (entry 203): a plain string never wraps, and the consent
  choices on nightly 102's first run screen were cut mid sentence. `Entry203Tests` checks for cut text.
- **The roll sheets name themselves on every system** since 3c3fa98: they were the only sheets over the 8000 pixel working limit,
  so their codes were read at half resolution; the corners are now cut from the full image.
- **Android has started** (entries 198 and 199): the plan is `docs/ANDROID.md`; **detection runs on the Fold 7**: 17 s and
  714 MB for the 600 dpi sample (desktop 8 s). Folding, turning and the font size passed (entry 205); all four ways up since. Build the spike as Release; a debug APK does not start. **A public Play listing waits on the attorney's review of the GPL app
  store permission**; internal and closed testing do not.
- **Sending targets from GroupLab is on** (entry 195, commit 1606619). **Error reports are on** (entry 200, 8725f91); the
  open issues in `oRAirwolf/grouplab-crash-reports` are read at the start of every run.
- **A receiver counts as live only when an empty POST to it returns its own error from the live site**, not when it is in the
  repository or the include (entry 195: error-report.php was in both and never shipped).
- **The next stable release**: `release.yml`'s body becomes the generated notes with the unsigned build paragraph after them
  (question 52, option A).
- Entry 149 section 3 A is built; D is the quiet hint of entry 187. Section 4 waits with entry 172 section 3 item 1.

## The next three

1. **The capture screen's CameraX spike on the Fold 7**, the next stage of Android, when an entry asks for it.
2. Program A steps 3 and 4 (entry 158) when request 19's scan of the ST-4 arrives.
3. Program B's article when request 20's test is shot.

## Blocked, and on what

- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 sections 3.2 and 5.** Request 16: the Mac tester's measurement and his name for a thanks.

Open requests in `docs/notes/for-alan.md`: **6** (9 most urgent; then 16, 20, 18, 12, and 21, optional). Request 30 is a note
  of the older phones, used only at the milestones in `docs/ANDROID.md`.

## Open questions

Seven, all in `docs/QUESTIONS-FOR-PLANNING.md`. Entry 187 answered 50, 52, 53, 54 and 55; entry 195 answered 56.


- **58** the analysis screen needs about 1060 units wide; at 200 percent on a 1920 screen its right column is cut
- **57** may a sheet of two to four marks flag one mark against the others
- **51** which hole centre GroupLab should report; agreed to wait on request 9
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's center means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.102, published from 5a1e769, carrying entries 195 to 199 and the roll sheets' codes. Nightly 101
  carried 187 to 194.
- **The site serves the commit after nightly 102**, published by hand because neither the nightly's notes commit nor
  `docs/PLATFORM-SUPPORT.md` starts the site workflow; the upload page is live at `grouplab.org/targets/`, `pissinhot.com/targets` redirects there, and the error report
  and application receivers publish with it, both switched off in the application until requests 22 and 24.
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

## Things that would surprise somebody who was not here yesterday

- **An error GroupLab survives is no longer called a close** (entry 192); each run leaves a marker so a real close is caught.
- **A size is an angle first** wherever the distance is known, the size on the paper beneath (entry 189).
- **The zeroing grids found no holes at all** until entry 189: their one bull had a cell of no size.
- **The upload page asks for one of two consent levels**, testing only or may be published (`consent_v2`), and a
  testing only target can never reach `samples/`, the research build or the site (entry 165).
- **Every upload is virus scanned**, streamed to clamd, since request 14 (entries 182 and 183).
- **The web upload path works end to end**, desktop and phone, and opted out submissions are accepted again (entry 186).
- **Command Z works on a Mac now, and pinch zoom exists**, on no hardware checked yet; a plain scroll pans on a Mac.
- **A printed grid registers a target GroupLab did not print** (`GridRegistration`, entry 158), and the scan detector finds only about one shot in seven in photographs of overlapping groups.
- **A photograph over 40 degrees off square is refused**, naming the angle, and every photograph keeps its angle and a quality score (entry 157).
- **Ballistics has a hit probability by simulation** (entry 156): per-shot and per-string errors, first and second round, what costs the most.
- **The library and printing are one screen, Targets** (entry 155); the old tour addresses link to it.
- **Every word a shooter may not know explains itself**, in the app and on the site, from `glossary.json` (entry 154).
- **Every published sentence has its backing**: `scripts/claims.py --check` fails CI otherwise (entry 159).
- **The analysis screen shows six figures and the zero block**; the rest is under Advanced. Everything a user
  reads is in American spelling, and a test holds it.
- **A scan reports real inches.** A photograph stays in the sheet's own inches and says so.
- **Temporary files clean themselves up.** Tests write into one folder per run, CI fails on a leak, and
  each run starts with `scripts/clean-scratch.py`. The scratch area had reached 18 GB.
- **Nothing under `website/server/` may hold a carriage return**: it is copied to Linux as it is.
- **Nothing is written into a HestiaCP `conf/web/<domain>/` folder** but the include itself: anything named
  `nginx.ssl.conf_` there is live configuration, and a backup beside it broke `nginx -t` once.
- **Alan's own photographs and scans may be published**, by his standing consent in `samples/PROVENANCE.md`, and so may
  what he passes on from Unholy (also TNA) and his other friends, entry 190; the 2026-09-16 friend scan never is.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel; the panel is mirrored in
  `docs/notes/panel.md` for the planning session.
