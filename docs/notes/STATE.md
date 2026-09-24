# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-24, after entry 170.

---

## In flight

- Done this run: 171, 173, 164, 174 to 181, and 170, whose choice of hole centre is question 51.
- **Where a hole's centre is**, question 51: the reported centre leans toward the scanner's shadow by about 0.011 in, the
  same way on every scan; the choice of a replacement waits on request 9's hand markings.
- Alan's order from here: **172, 166, 169, 159, 154, 155, 156, 157, 158, 165.** Entries 156
  and 165 were amended after they were first read, so each is read again before it starts.
- Entry 149 section 3 A is built; D is question 50. Section 4 goes with entry 172 section 3 item 1.

## The next three

1. **Entry 172.** Ground truth for the 2026-09-20 ST-4 target, which measures detection and hole centres.
2. **Entry 166.** The Mac tester's answers: Command shortcuts, pinch zoom, what was checked on a Mac.
3. **Entry 169.** The analysis screen cut down, and the rest of that user's review.

## Blocked, and on what

- **The server's intake worker** is installed with the ClamAV daemon and HEIC (request 11, entry 181's
  line ending hot fix applied). Confirm from the next submission that it scans.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.

Open requests in `docs/notes/for-alan.md`: **3** (9 most urgent, then 12, and 5 being applied).

## Open questions

Six, all in `docs/QUESTIONS-FOR-PLANNING.md`.

- **51** which hole centre GroupLab should report; waits on request 9
- **50** question 37's D cannot find the offset without being told the bulls
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's centre means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.94. Tonight's builds carry entries 164, 170 and 171's application changes.
- **The site serves `a77a1c7` or later**; nothing since has changed a page. The upload page is live at
  `grouplab.org/targets/` and takes photographs; `pissinhot.com/targets` redirects there.
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** 154, 155, 156, 157, 158, 159, 165, 166, 169, 172

## Things that would surprise somebody who was not here yesterday

- **The web upload path works end to end**, desktop and phone. Entry 129 is complete.
- **A scan reports real inches.** A photograph stays in the sheet's own inches and says so.
- **Temporary files clean themselves up.** Tests write into one folder per run, CI fails on a leak, and
  each run starts with `scripts/clean-scratch.py`. The scratch area had reached 18 GB.
- **Nothing under `website/server/` may hold a carriage return**: it is copied to Linux as it is.
- **Nothing is written into a HestiaCP `conf/web/<domain>/` folder** but the include itself: anything named
  `nginx.ssl.conf_` there is live configuration, and a backup beside it broke `nginx -t` once.
- **Alan's own photographs and scans may be published**, by his standing consent in `samples/PROVENANCE.md`;
  the 2026-09-16 friend scan never is.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel; the panel is mirrored in
  `docs/notes/panel.md` for the planning session.
