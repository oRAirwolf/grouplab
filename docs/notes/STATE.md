# Where this project is, right now

**Rewritten at the end of every run, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entry 160 section 2. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`, which together weighed 2 MB before entry 160 split
them. It is also how the planning session stops working from a stale picture, which has already
happened once, with entry 148.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-24, after entry 171.

---

## In flight

- Entries 149 to 153, 160 to 163, 167, 168 and 171 are done. **Entry 164 is finished and waiting to be
  committed**: it was set aside so 171 could go first.
- Alan's order from here: **173, 164, 170, 172, 166, 169, 159, 154, 155, 156, 157, 158, 165.** Entry 173
  opens the upload page at `grouplab.org/targets/` and puts it in the top bar. Entries 156
  and 165 were amended after they were first read, so each is read again before it starts.
- Entry 149 sections 3 and 4 were never done and nothing brought them back. Entry 171 section 6.1 puts
  section 3 with entry 170 section 2 and section 4 with entry 172 section 3 item 1.

## The next three

1. **Entry 164.** The first macOS log: marker 28 is printer banding, the Mac is faster than Windows on
   the same file, and a report no longer carries file names. Done, needs committing.
2. **Entry 170.** An outside user's defects: two interface freezes, a zero correction that does not say
   its distance, and hole centres he had to move by hand. With entry 149 section 3.
3. **Entry 172.** Ground truth for the 2026-09-20 ST-4 target, which measures entry 170's hole centres.

## Blocked, and on what

- **Entry 129, what is left of it.** The server side is finished (entry 171). What remains is opening the
  page, Alan's end to end test, ingesting the six waiting submissions, and the pissinhot.com redirect,
  whose commands are written out in request 1 of `docs/notes/for-alan.md`.
- **Entry 156, hit probability.** The mathematics is decided; the layout waits on screenshots of the
  tools Alan already uses. Request 2.
- **Entry 158, the paper-tearing program.** Step 1 needs the photograph annotations. Request 3.
- **The friend's 59 MB scan in CI.** The `test-data` release is created by `ci.yml` on the next push to
  main; the file is attached to it once, and CI then fetches and checks it on every run.

Open requests in `docs/notes/for-alan.md`: **4** (1, 2, 3, and 5, which Alan is applying).

## Open questions

Four, all in `docs/QUESTIONS-FOR-PLANNING.md`. Entry 171 closed 39, 41, 42, 45, 46, 48 and 49 and most
of 44; answered ones are listed there by number and live whole in the archive.

- **44, the part still open** the bent-sheet model throws at a point outside the page
- **43** entry 137 names an image safety the desktop does not have; item 1 is built, item 2 is not
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's centre means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.94. Numbers count builds, not workflow runs, and since entry 168 a
  night with no application change builds nothing.
- **The site was current with main** at `a33be90` when this was written; entry 171's push publishes the
  community page's channels and rules, the corrected print scale wording, and the tour's print page.
- **Windows, Linux and both macOS downloads answer 200.** What was checked on a real Mac is being
  rewritten under entry 166.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ, because this was the fact that was wrong last time.

**Holds:** 154, 155, 156, 157, 158, 159, 164, 165, 166, 169, 170, 172, 173

## Things that would surprise somebody who was not here yesterday

- **A scan now reports real inches.** Entry 171 answered question 49: a scan measures the print scale and
  every distance is multiplied by it, so a sheet printed at 96 percent reads its true size. A photograph
  cannot measure it, stays in the sheet's own inches, and says so in one line. `docs/WHAT-CAN-BE-MEASURED.md`.
- **Alan's own photographs and scans may be published**, unless he names one, by a standing consent of
  2026-09-24 in `samples/PROVENANCE.md`. It does not reach anything a friend shot, and the 2026-09-16
  friend scan is still never published.
- **A sample over about 10 MB is never committed.** It goes on the `test-data` release with its hash in
  `tests/test-data.json`, and `TestDataTests` fails on a large committed file.
- **A target GroupLab did not print can be measured**, once the scale is set by hand. Exactly five
  things need a GroupLab sheet, and they are listed in `docs/WHAT-CAN-BE-MEASURED.md`.
- **Cartridge names are matched before numbers.** Typing 6.5 offers 6.5 Creedmoor, not .257. Forty
  cartridges are confirmed by two sources; thirty three wait for a second one. `docs/CALIBRES.md`.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never in the Claude Code panel, with one
  exception: a command he pastes into a shell. Entry 149 section 5.
