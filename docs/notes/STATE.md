# Where this project is, right now

**Rewritten at the end of every entry, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entries 160 and 180. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`. Alan does not read the Claude Code panel: what is in it for
him is mirrored in `docs/notes/panel.md` (local, not committed), and what needs him is in
`docs/notes/for-alan.md`, which starts with the count of open requests.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-24, after entry 165.

---

## In flight

- Done this run: 171, 173, 164, 174 to 185, 166, 169, 170, 159, 154 to 158, 165 (154's choice of hole centre is question 51), and 172 in part:
  its measurements wait on entries 157 and 158, because GroupLab cannot yet find holes on a sheet it did not print.
- **Where a hole's centre is**, question 51: the reported centre leans toward the scanner's shadow by about 0.011 in, the
  same way on every scan; the choice of a replacement waits on request 9's hand markings.
- Alan's list is done. The inbox is empty.
- **Sending targets from the application is built and switched off** (entry 165): `appOpen` is false until request 21's
  nginx block is in and a later entry turns it on.
- Entry 149 section 3 A is built; D is question 50. Section 4 waits with entry 172 section 3 item 1.

## The next three

1. **Turn on sending from the application** once request 21 answers 400: `appOpen` true, and a check against the live receiver.
2. Program A steps 3 and 4 (entry 158) when request 19's scan of the ST-4 arrives.
3. Program B's article when request 20's test is shot.

## Blocked, and on what

- **Every opted out submission is refused** until request 15 installs entry 183's worker; one waits in refused
  to be moved back.
- **Entry 170 section 4.4.** Request 9: the same scan marked by hand twice.
- **Entry 166 sections 3.2 and 5.** Request 16: the Mac tester's measurement and his name for a thanks.
- **Entry 185 section 2.** Request 17: one command makes the test data release a draft.
- **Entry 165, switching it on.** Request 21: one nginx block for the application's receiver.

Open requests in `docs/notes/for-alan.md`: **10** (15 most urgent, then 21, 9, 16, 17, 19, 20, 18, 12, and 5 being applied).

## Open questions

Nine, all in `docs/QUESTIONS-FOR-PLANNING.md`.

- **54** entry 157's 40 degree limit, the quality score's levels, a white board behind the sheet
- **53** entry 156's presets, its refusal threshold and a pooled group's velocity share
- **52** a stable release's body is fixed text, not the generated notes its announcement uses
- **51** which hole centre GroupLab should report; waits on request 9
- **50** question 37's D cannot find the offset without being told the bulls
- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's centre means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.98, published 16:16 UTC.
- **The site serves `099c270`**, and entry 165 changes the guide, the tour, one article and the upload page's consent. The upload page is live at
  `grouplab.org/targets/` and takes photographs; `pissinhot.com/targets` redirects there.
- **The site sync** checks for as long as nginx can serve a replaced file, read from nginx at run time.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ.

**Holds:** none

## Things that would surprise somebody who was not here yesterday

- **The upload page asks for one of two consent levels**, testing only or may be published (`consent_v2`), and a
  testing only target can never reach `samples/`, the research build or the site (entry 165).
- **Every upload is virus scanned**, streamed to clamd, since request 14 (entries 182 and 183).
- **The web upload path works end to end**, desktop and phone. Entry 129 is complete.
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
- **Alan's own photographs and scans may be published**, by his standing consent in `samples/PROVENANCE.md`;
  the 2026-09-16 friend scan never is.
- **A sample over about 10 MB is never committed**; it goes on the `test-data` release.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never only in the panel; the panel is mirrored in
  `docs/notes/panel.md` for the planning session.
