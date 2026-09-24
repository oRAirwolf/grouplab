# Where this project is, right now

**Rewritten at the end of every run, never appended to. Under 120 lines, and a test holds it there.**

NOTES-FROM-PLANNING.md entry 160 section 2. Both sessions read this file first, instead of searching
`NOTES-FROM-PLANNING.md` and `PHASE1-RESULTS.md`, which together weighed 2 MB before entry 160 split
them. It is also how the planning session stops working from a stale picture, which has already
happened once, with entry 148.

If something here disagrees with the logs, the logs are right and this file is out of date. Say so.

**Last rewritten:** 2026-09-23, after entry 160.

---

## In flight

- **Entry 153 is closed**, section 5 included, within the one limit on publishing real material.
- **Entry 159 is next**, then 154, 155, 156, 157, 158, and entry 161, which arrived during this run.

## The next three

1. **Entry 159.** Audit the website, the README and the application for claims that are not true, and
   build the claims register the audit produces.
2. **Entry 154.** A word a shooter does not know gets an explanation where they meet it: one glossary
   source, tooltips on the site that work on tap as well as hover, and the same text in the
   application.
3. **Entry 155.** One Targets screen, because the library and the print dialog do the same job.

Then, in this order and not reordered: 156, 157, 158, and 161.

## Blocked, and on what

- **Entry 129, the upload page.** Built, tested, and gated out of the site by `"open": false` in
  `website/api/limits.json`. It needs five things done on the server, which only Alan can do. They are
  written out in full as request 1 in `docs/notes/for-alan.md`.
- **Entry 156, hit probability.** The mathematics is decided; the layout waits on screenshots of the
  tools Alan already uses. Request 2.
- **Entry 158, the paper-tearing program.** Step 1 needs the photograph annotations. Request 3.
- **Entry 151, the community page.** Live and honest, and it names the five channel groups without
  naming the channels, because nobody has read them out of the server. Request 4.
- **The shadow crop entry 153 section 5 asks for.** It exists only in a photograph and no photograph has
  a consent record. Request 6 asks whether one crop of one photographed hole may be published.

Nothing else is blocked. Everything in the queue can proceed today.

## Open questions

Eleven, all in `docs/QUESTIONS-FOR-PLANNING.md`, newest first. Answered ones are listed there by number
and live whole in `docs/notes/archive/questions-answered.md`.

- **49** should a scan report real inches, now it knows the print scale (entry 161)
- **48** entry 160's fourteen day rule would have moved nothing
- **46** the sheet offset is solved over every bull, and narrowing it makes things worse
- **45** scan 6 reads 9 holes tonight where entry 130 recorded 10
- **44** the bent-sheet model crashes on one photograph and improves the wrong points on the rest
- **43** entry 137 names an image safety the desktop does not have; item 1 is built, item 2 is not
- **42** a corrected shot does not survive a second detection
- **41** dragging a shot onto a bull means two different things
- **39** close calibre pairs straddling two lists; moot since entry 161 stopped the guess naming one
- **36** a light installer, measured, and why shrinking the one we have beat it
- **34** pooling two sheets of one load needs a rule for what a pooled group's centre means

## Builds and the site

- **Last nightly:** 0.2.0-nightly.93. Numbers count builds now, not workflow runs, so a gap means
  nothing shipped that night rather than that something broke.
- **The site is current with main.** `grouplab.org` serves `a3fa45b`, which is main's head.
- **Windows, Linux and both macOS downloads answer 200.** The macOS builds are untested on real
  hardware and every page carrying them says so.

## The inbox

`docs/notes/inbox/` holds entries **154 to 159 and 161**. 161 arrived during this run and has not been
read yet.

## Things that would surprise somebody who was not here yesterday

- **A sheet printed at the wrong size is read correctly and measured in its own inches.** Entry 152 said
  it measures correctly; entry 161 read the code and it does not. Printed at 96 percent, every group reads
  4 percent large; a scan says so and a photograph cannot. `docs/WHAT-CAN-BE-MEASURED.md` is the source.
- **A target GroupLab did not print can be measured**, once the scale is set by hand. Exactly five
  things need a GroupLab sheet, and they are listed in that same file.
- **The corpus detection-counts record had been stale since entry 101** and is now current. It is
  gated on the printed artwork rather than on the counts, which is how four entries of accepted change
  went unrecorded with nothing going red.
- **A rimfire 22 is 0.222.** It was missing from the calibre list entirely: 0.2215 is 5.45x39 and
  0.224 is the centrefire 22.
- **Two red pushes this week were both my tests, not the code**, and both were Windows-only: a pattern
  anchored on a line ending against a CRLF checkout, and a test that read a file the test run had open.
- **Requests for Alan go in `docs/notes/for-alan.md`**, never in the Claude Code panel, with one
  exception: a command he pastes into a shell. Entry 149 section 5.
