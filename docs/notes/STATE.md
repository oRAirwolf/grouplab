# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-26, after entries 224 and 225.

---

## In flight

- Done this run: 171, 173, 164, 174 to 185, 166, 169, 170, 159, 154 to 158, 165, 186 to 225 (154's choice of hole centre is question 51), and 172 in part:
  its measurements wait on entries 157 and 158, because GroupLab cannot yet find holes on a sheet it did not print.
- **Where a hole's centre is**, question 51: the reported centre leans toward the scanner's shadow by about 0.011 in, the
  same way on every scan; the choice of a replacement waits on request 9's hand markings.
- **A sheet with one scoring bull takes a group** (entries 196 and 197): no count, no limit, nothing to review for it.
  The zeroing grids are for sighting in by eye; no more work makes them a scanning target.
- **Storage on GitHub**: `docs/notes/STORAGE.md`, written by `scripts/storage-ledger.py` from the pull and this session; see its total.
  Submissions leave the server only once archived in `grouplab-submissions-archive` and proven (entries 215 to 217).
- **Minimums** are in PLATFORM-SUPPORT.md (entry 207): Android 10, 4 GB; the survey (`docs/SURVEY.md`) is open since entry 223.
- **Android has started** (entries 198 and 199): the plan is `docs/ANDROID.md`; **detection runs on the Fold 7**: 17 s and
  714 MB for the 600 dpi sample (desktop 8 s). Folding, turning and the font size passed (entry 205); all four ways up since. Build the spike as Release; a debug APK does not start. **A public Play listing waits on the attorney's review of the GPL app
  store permission**; internal and closed testing do not.
- **Sending targets, error reports and the survey are on** (entries 195, 200, 223); crash issues are read at every start.
- **A receiver counts as live only when an empty POST to it returns its own error from the live site**, not when it is in the
  repository or the include (entry 195: error-report.php was in both and never shipped).
- **The next stable release**: generated notes with the unsigned build paragraph (question 52 A); it also goes to the Store.
- Entry 149 section 3 A is built; D is the quiet hint of entry 187. Section 4 waits with entry 172 section 3 item 1.

## Next

- **Backups and automation (entries 222, 224, 225)**: nightly backup to `grouplab-backups` and a weekly restore test, both passing; the
  server archives submissions by itself. The Oracle boot volume policy is on; **when Alan confirms the first backup, close request 35 and
  widen sudo** as entry 222 section 6.2 allows.
- **The Microsoft Store (entry 224)**: MSIX built in CI; tagged releases go to the Store by themselves once request 38 is done.
- **Android signing**: the nightly signs the APK and AAB; request 36 names the AAB once a nightly carries it.
- The roadmap is done up to what waits on Alan: A6 needs request 36's key (the nightly's signed Android job is committed and builds
  nothing until the key is set), A7 the older phones, D2 request 9, D4 request 37.

## The roadmap (entry 219), in place of the next three

Worked through without waiting for an entry; stopped only by a request to Alan, batched, or a decision that is his. Android first,
alternating so desktop feedback never waits more than one Android item.

- A1 the working resolution in Core: **done**, 8 MP, 1.2 to 7.9 thousandths mean shift; the desktop app's use of it waits.
- **A2 the CameraX capture screen spike: built**; the measurement is request 33, one sitting.
- **A3 the `org.grouplab.app` project: built**, CI uploads `grouplab-apk`; not yet on a phone (next sitting).
- **D1 the survey and benchmark: built, and open** (entry 223).
- **A4 built**: take or choose a picture at the working size, the result with figures, photo and the desktop's plot, corrected by
  touch with a magnifier, caliber and distance, the sheet by name, Sessions. Not yet on a phone (request 33 step 6).
- **A5 built**: a `.grouplab` session file, shared by hand both ways (request 33 step 7).
- **A6 built, waiting on request 36**: the nightly signs an APK and AAB once the upload key is in the secrets (tried here with a throwaway
  key). A7 the older phones, then a closed test.
- D2 question 51 when request 9 arrives. D3 feedback first whenever it comes. **D4 done**: `docs/RELEASE-PLAN.md`, and request 37.
- Waiting on requests: Program A (entry 158) on request 19's ST-4 scan; Program B's article on request 20's test.
## Blocked, and on what

- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 sections 3.2 and 5.** Request 16: the Mac tester's measurement and his name for a thanks.

Open requests in `docs/notes/for-alan.md`: **11** (35 most urgent, backups and the archive token; 36 the Android key, same sitting; 37
  the signing choice; the end of 34; 33, the Fold 7's camera; then 9, 16, 20, 18, 32 and 21, optional). Request 30 is a note of the older phones, used only at the milestones in `docs/ANDROID.md`.

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

- **Last nightly:** 0.2.0-nightly.105, published from 1d92d96, carrying the composite plot of entries 204 and 210 and the notes
  commit fix. Entries 213 and 214's key and rings come in the next.
- **The site serves the newest commit that touched it.** `docs/PLATFORM-SUPPORT.md` and the nightly's own notes commits do not start
  the site workflow, so a publish is started by hand after those. Both receivers are live and switched on.
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.
## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

## Things that would surprise somebody who was not here yesterday

- **An error GroupLab survives is no longer called a close** (entry 192); each run leaves a marker so a real close is caught.
- **A size is an angle first** wherever the distance is known, the size on the paper beneath (entry 189).
- **The upload page asks for one of two consent levels**, testing only or may be published (`consent_v2`), and a
  testing only target can never reach `samples/`, the research build or the site (entry 165).
- **Command Z works on a Mac now, and pinch zoom exists**, on no hardware checked yet; a plain scroll pans on a Mac.
- **A printed grid registers a target GroupLab did not print** (`GridRegistration`, entry 158), and the scan detector finds only about one shot in seven in photographs of overlapping groups.
- **A photograph over 40 degrees off square is refused**, naming the angle, and every photograph keeps its angle and a quality score (entry 157).
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
