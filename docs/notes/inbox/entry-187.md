## 2026-09-24, entry 187: answers to questions 50 to 55, sending switched on after one test, request 19 closed, release note spelling

Do this after entry 186. Your last report was written before Alan ran request 15; entry 186 records that it is installed and
working, so the "request 15 is most urgent" line is out of date.

## 1. Question 55: yes, switch sending on, after one end to end package

Alan's answer, 2026-09-24: yes. Your suggested order, exactly:

1. Send one real package from the application to the live receiver, marked **testing only**, from a sample target that holds no
   photograph of anyone's and no location data. Say in the report which image it was.
2. Watch it reach `ready` through the worker (you cannot read the server log; Alan's pull is the check). Put the pull command in
   for-alan.md as a new request, with the good result written out: the folder name, the files it holds, and the checksums matching.
3. Only after Alan reports the pull matched, set `appOpen` true, in its own commit, with a `Release-note:` trailer saying sending is
   now switched on and what the first run question asks.
4. Then add the same package's folder to request 12's clearing list, so it is removed with `Remove-ReadSubmissions.ps1` along with
   the old test uploads, and never kept as a fixture.

Request 21 stays optional. If the test package is slow or times out, say so; that is when request 21 matters.

## 2. Question 54: keep 40 degrees, keep the levels, build both paths for a white backer

1. **40 degrees** stays until request 18's photographs measure it. If those are never taken, 40 stands.
2. **The quality score's levels** stand as judgment. Keep every score's parts with the submission, so real submissions can tune them
   later. Say they are judgment where `docs/MOBILE-CAPTURE.md` lists them, which I believe it already does.
3. **White paper on a white board:** build both. The guidance ("put something darker behind the sheet") and a manual corner path on
   the capture screen, because Alan and his friend both shoot on light backers. The manual path is the one that must never fail.

## 3. Question 53: accept all three, with one change later

1. The presets' figures stand as judgment, documented as they are. No sources to add.
2. The refusal by interval width, not a fixed shot count, is right. Keep it.
3. Pooled sessions at different distances: nearest distance is acceptable now because it errs cautious and the screen says so.
   Taking the velocity share out per session before pooling is the better answer; do it when `Pooling.Recentred` is next touched,
   not as its own piece of work.

## 4. Question 52: option A

Next time Alan asks for a stable release, `release.yml`'s body becomes the generated notes with the unsigned build paragraph after
them. Nothing to do before then except a line in STATE.md so it is not forgotten.

## 5. Question 51: wait for request 9

Agreed. Nothing changes until Alan's two hand markings arrive.

## 6. Question 50: option 2, quietly

Option 2, only where the sheet has more scoring bulls than shots and nobody has said which bulls. But Alan's friend's feedback was
about screens asking too much, so:

- It is a hint beside the "Bulls you fired at" control, not a review item that counts as unresolved or blocks Accept.
- Answering it or dismissing it once removes it for that target.
- It never claims anything would move.

## 7. Request 19: closed, the ST-4 sheet is gone

Alan no longer has the 2026-09-20 ST-4. Mark request 19 answered, 2026-09-24, "sheet no longer exists". Program A steps 3 and 4 need
another commercial gridded sheet: add one line to request 20 (the hole size shoot), asking Alan to scan any commercial gridded sheet
he shoots before throwing it away, flat, at 600 dpi, with a note of distance, cartridge and which mark each group aimed at. Do not
open a new request for it.

## 8. The release notes still have British spellings and entry numbers

`docs/RELEASE-NOTES.md` has 17 lines with "calibre" or "centre", and many user-facing lines end in "(Entry 157)" and similar. Both
are user-facing text on the releases page and in Discord.

1. Run `scripts/american-spelling.py` over `docs/RELEASE-NOTES.md` and over every `Release-note:` trailer the generator reads, and
   make the build fail on a British spelling there, as it does for the site. Fix the existing lines in the file. Published release
   bodies already on GitHub can stay as they are unless the fix is one command per release; do not rewrite history for spelling.
2. Drop "(Entry N)" from the text a user reads. Entry numbers mean nothing to anyone outside this project. Keep them in the commit
   and in NOTES-FROM-PLANNING.md, where they are useful.

## 9. What Alan needs to see in the report

Plain words, no entry or question numbers on their own: what was switched on or is waiting on his pull, and the one command he runs.
