# 2026-09-23, entry 149: answers to questions 35, 37, 40 and 47, and the Alan list for this run

Written by the planning session on 2026-09-23 from Alan's feedback document. This entry is first because
it unblocks four open questions and because section 5 is what stops Alan sitting and watching output.

## 1. Question 47: keep the five kinds

**Keep `new`, `fixed`, `changed`, `user` and `internal`.** Entry 145 section 3.1 named two because two
headings are what a reader sees, and it should have said so rather than naming the vocabulary. Your
reasoning is right on all three counts, and the strongest of them is the third: a change whose only
benefit is a shorter list of words, paid for by rewriting 45 commit messages, is not worth making.

Do this much:
1. Correct entry 145 section 3.1 where it is quoted in `CLAUDE.md`, so the documentation names all five
   words and says which heading each one lands under. Right now `CLAUDE.md` names three and entry 145
   names two, and neither matches the code.
2. Add one sentence to the same place saying why there are five: the heading is what the reader sees,
   the kind is what the writer says.
3. A test that the set of words the generator accepts and the set the documentation names are the same
   set. That is the thing that drifted.

## 2. Question 40: take the quarter point of the smaller group

**Build what you proposed.** Where the round marks fall into two clear sizes and the sheet's own marks
are the reference, take the quarter point of the **smaller** group rather than refusing to read a size.

The reasoning that decides it is yours: refusing flags nothing, and a sheet that really does carry five
doubles is exactly the sheet where flagging nothing is worst. Both of the cases the code cannot tell
apart are handled correctly by taking the smaller group. If the smaller marks are singles, the doubles
are flagged. If they are a second, smaller calibre, the larger holes are flagged and entry 140 section
3.2's guard turns that flood into one question about the calibre.

Conditions:
1. Entry 82 section 3 is amended by this entry, not worked around. Say so where the rule lives.
2. The description still asks for the calibre in the two-size case. Taking a reading does not mean the
   question stops being worth asking.
3. `CryingWolfTests` keeps both rows of your table and gains the new behaviour on the second row.
4. One correction to the arithmetic in the question, which matters elsewhere as well: **rimfire 22 is
   nominally 0.222 in, not 0.224**. The 0.224 figure belongs to centrefire 22 cartridges such as
   5.56x45 and 22 ARC. Entry 153 section 4 sweeps this everywhere.

## 3. Question 37: build A and D together

**Build A and D. Do not build C.** Your recommendation is accepted whole, including the reason for
refusing C, which is the right reason: a shooter knowing which bulls they aimed at is a fact, and
turning a fact into a guess on the one question where a wrong answer is invisible is the trade this
project does not make.

- **A, clicking the bulls on the sheet.** It belongs on the marking screen with the other bull
  interactions, and entry 131's editing popover already needs the gesture. Clicked bulls light up.
  There is a way to select a row, a column and everything, because a shooter who used every bull
  should not click twenty five times.
- **D, offering it where it would change the answer.** When a certain offset exists that would move
  shots, the review queue says so in plain words and offers to apply it. That is what makes A
  discoverable.
- Until A exists, the release note wording stays as you have it. Do not let a note claim this is fixed
  for anybody who has not read the code.

## 4. Question 35: A, and then finish it

**Keep one bull's width**, which your section 5 has already measured as safe on all six scans. That is
option A and the measurement supports it.

The honest completion is the part you named yourself: **re-run the survey's own baselines against the
narrowed rule**, because that is where the false positives were counted in the first place, and six
real scans are not that survey. Treat it as blocking the claim rather than blocking the release: until
it has run, "no false holes anywhere" is not a sentence this project may publish, and the results file
should say which of the two bodies of evidence stands behind the claim it does make.

## 5. Requests for Alan go to a file, not to the panel

**Do not ask Alan for anything through the Claude Code panel except a command he must paste into a
shell.** He has said plainly that the panel is hard to read and that answering questions there is
harder than answering them in the planning session. Everything else goes through the planning session,
which reads this repository.

The mechanism:

1. Create and maintain `docs/notes/for-alan.md`. It is a list of open requests, newest first, each one
   numbered, each one saying in plain words what is needed, why, and what a good answer looks like.
   A request that has been answered is marked answered with the date and left in place.
2. **Write a request there and carry on.** Never stop and wait for an answer. If an entry cannot finish
   without one, do everything else in it, say in the report which part is waiting, and move to the next
   entry.
3. At the start of a run, print the count of open requests in that file and nothing more. The planning
   session reads the file and puts the requests to Alan in a form he can answer in one sitting.
4. His answers come back the way everything else does, as an inbox entry.

The exception, and it is the only one: **a command Alan must paste into a shell**. Those still go in the
panel because he runs them from there. Give the command written out in full, say which shell it goes
into, and say what a good result looks like. He works inside MobaXterm and does not need the SSH
connection commands.

Three requests already exist and should be written into `docs/notes/for-alan.md` on the first commit of
this run, so the file starts with real content:

1. **Entry 129 sections 4, 6 and 8.2.** These need the server. State exactly what each one needs.
2. **The hit probability screenshots** entry 156 asks for.
3. **The photograph annotations** entry 158 section 2 asks for.

Items 2 and 3 are already in hand with the planning session, so write them down and do not chase them.

## 6. The report after each entry

After each entry, a report of no more than five lines: the entry number, what changed, the test result,
the commit, and whether the site has published it yet. Nothing longer, and no request for Alan inside
it.
