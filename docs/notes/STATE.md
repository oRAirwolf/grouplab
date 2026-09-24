# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-24, after entry 185.

---

## In flight

- Done this run: 171, 173, 164, 174 to 185, 166, 169, 170 (its choice of hole centre is question 51), and 172 in part:
  its measurements wait on entries 157 and 158, because GroupLab cannot yet find holes on a sheet it did not print.
- **Where a hole's centre is**, question 51: the reported centre leans toward the scanner's shadow by about 0.011 in, the
  same way on every scan; the choice of a replacement waits on request 9's hand markings.
- Alan's order from here: **159, 154, 155, 156, 157, 158, 165**, as he gave it.
- **The first Discord post** comes with the next published nightly; entry 184 section 3.3 quotes it then. Entries 156
  and 165 were amended after they were first read, so each is read again before it starts.
- Entry 149 section 3 A is built; D is question 50. Section 4 waits with entry 172 section 3 item 1.

## The next three

1. **Entry 159.** The claims register; it also takes entry 166's platform statement with its evidence.
2. **Entry 154.** A word a shooter does not know gets an explanation where they meet it.
3. **Entry 155.** One Targets screen, because the library and the print dialog do the same job.

## Blocked, and on what

- **Every opted out submission is refused** until request 15 installs entry 183's worker; one waits in refused
  to be moved back.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 sections 3.2 and 5.** Request 16: the Mac tester's measurement and his name for a thanks.
- **Entry 185 section 2.** Request 17: one command makes the test data release a draft.

Open requests in `docs/notes/for-alan.md`: **6** (15 most urgent, then 9, 16, 17, 12, and 5 being applied).

## Open questions

Seven, all in `docs/QUESTIONS-FOR-PLANNING.md`.

- **52** a stable release's body is fixed text, not the generated notes its announcement uses
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

**Holds:** 154, 155, 156, 157, 158, 159, 165

## Things that would surprise somebody who was not here yesterday

- **Every upload is virus scanned**, streamed to clamd, since request 14 (entries 182 and 183).
- **The web upload path works end to end**, desktop and phone. Entry 129 is complete.
- **Command Z works on a Mac now, and pinch zoom exists**, on no hardware checked yet; a plain scroll pans on a Mac.
- **The analysis screen shows six figures and the zero block**; the rest is under Advanced. Everything a user
  reads is in American spelling, and a test holds it.
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
