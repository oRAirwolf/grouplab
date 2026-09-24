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

- Entries 149 to 153, 160 to 164, 167, 168, 171 and 173 are done. **The upload page is open at
  `grouplab.org/targets/`.**
- Alan's order from here: **170, 172, 166, 169, 159, 154, 155, 156, 157, 158, 165.** Entries 156
  and 165 were amended after they were first read, so each is read again before it starts.
- Entry 149 sections 3 and 4 were never done and nothing brought them back. Entry 171 section 6.1 puts
  section 3 with entry 170 section 2 and section 4 with entry 172 section 3 item 1.

## The next three

1. **Entry 170.** An outside user's defects: two interface freezes, a zero correction that does not say
   its distance, and hole centres he had to move by hand. With entry 149 section 3.
2. **Entry 172.** Ground truth for the 2026-09-20 ST-4 target, which measures entry 170's hole centres.
3. **Entry 166.** The Mac tester's answers: Command shortcuts, pinch zoom, and what was checked on a Mac.

## Blocked, and on what

- **The upload page's end to end test and the redirect.** The page is open (entry 173). A browser has to
  pass Turnstile and the pull runs sudo, so both are Alan's: request 1 of `docs/notes/for-alan.md`.
- **Entry 156, hit probability.** The mathematics is decided; the layout waits on screenshots of the
  tools Alan already uses. Request 2.
- **Entry 158, the paper-tearing program.** Step 1 needs the photograph annotations. Request 3.

Open requests in `docs/notes/for-alan.md`: **6** (1, 2, 9, 10, 11, and 5, which Alan is applying).

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
- **The site serves `9ade3bf`**, entry 171. Entry 173's push publishes the open upload page and the top
  bar's "Send a target".
- **Windows, Linux and both macOS downloads answer 200.** What was checked on a real Mac is being
  rewritten under entry 166.

## The inbox

`docs/notes/inbox/` holds the entries below. A test reads this line and the directory and fails when
they differ, because this was the fact that was wrong last time.

**Holds:** 154, 155, 156, 157, 158, 159, 165, 166, 169, 170, 172, 177, 178

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
