# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-24, after entry 180.

---

## In flight

- Done this run: 171, 173, 164, 174, 175, 176, 177, 178, 179, 180, and entry 170 sections 1 to 3.
- **Entry 170 section 4, the hole centres, is part done and set aside**, uncommitted, in this session's
  scratchpad (`e170-s4`). The finding: the detector's centre is pulled toward the scanner's shadow, 0.011 in
  on average, the same way on every scan. The area centroid halves it; an edge fit removes it on real scans
  but moved some synthetic holes up to 0.039 in, so neither is committed yet.
- Alan's order from here: **170 (section 4), 172, 166, 169, 159, 154, 155, 156, 157, 158, 165.** Entries 156
  and 165 were amended after they were first read, so each is read again before it starts.
- Entry 149 section 3 A is built; D is question 50. Section 4 goes with entry 172 section 3 item 1.

## The next three

1. **Entry 170 section 4.** Decide between the area centroid and an edge fit with a better gate, against
   the synthetic truth and the real scans, then request 9's hand markings when they come.
2. **Entry 172.** Ground truth for the 2026-09-20 ST-4 target, which measures detection and hole centres.
3. **Entry 166.** The Mac tester's answers: Command shortcuts, pinch zoom, what was checked on a Mac.

## Blocked, and on what

- **The server's virus scanner.** Request 11: clamav-daemon, HEIC decoding and the committed intake worker.
  Until then uploads are rebuilt from pixels but not scanned, and the pull script says so.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.

Open requests in `docs/notes/for-alan.md`: **4** (11 most urgent, then 9, 12, and 5 being applied).

## Open questions

Five, all in `docs/QUESTIONS-FOR-PLANNING.md`.

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

**Holds:** 154, 155, 156, 157, 158, 159, 165, 166, 169, 170, 172

## Things that would surprise somebody who was not here yesterday

- **The web upload path works end to end**, desktop and phone. Entry 129 is complete.
- **A scan reports real inches.** A photograph stays in the sheet's own inches and says so.
- **Temporary files clean themselves up.** Tests write into one folder per run, CI fails on a leak, and
  each run starts with `scripts/clean-scratch.py`. The scratch area had reached 18 GB.
- **Nothing is written into a HestiaCP `conf/web/<domain>/` folder** but the include itself: anything named
  `nginx.ssl.conf_` there is live configuration, and a backup beside it broke `nginx -t` once.
- **Alan's own photographs and scans may be published**, by his standing consent in `samples/PROVENANCE.md`;
  the 2026-09-16 friend scan never is.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel; the panel is mirrored in
  `docs/notes/panel.md` for the planning session.
