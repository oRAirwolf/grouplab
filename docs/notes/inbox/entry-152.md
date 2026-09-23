# 2026-09-23, entry 152: say plainly what GroupLab can measure, because two published claims are wrong

Alan, reading the tour: two statements on the site do not match what the application does. One of them
is contradicted by a research article the same site publishes, which is the worst kind of error because
a reader who reads both learns that the site cannot be trusted rather than which sentence is right.

Find the truth in the code first. Then fix every place that says otherwise. Do not fix the wording by
guessing which version is true.

## 1. The two claims

**`website/tour.json`, the library screen:**

> GroupLab can only measure a sheet it printed, because the measurement comes from the markers and
> codes printed around the bulls rather than from anything you tell it.

Alan: "This is not true. It can read any target as long as a scale is defined, right?"

**`website/tour.json`, the print screen:**

> Everything GroupLab measures depends on the sheet being the size it says it is. A sheet printed at
> 97 percent measures three percent small, and nothing later can recover that.

Alan: "Doesn't GroupLab correct for sheets being printed at the wrong scale? I know we emphasize the
importance of this, but isn't it technically a non issue?"

**And `website/research/printer-true-size.md`, published, says the opposite:**

> On a scan, GroupLab detects this (a sheet printed at 96.2 percent was measured at 0.96200) and
> corrects every figure for it.

## 2. Establish what is true, with file and line references

Answer these in the run report, each one pointing at the code that settles it:

1. **What can establish scale?** Enumerate every path: printed GroupLab markers and codes, a scan's own
   DPI, the sheet dimensions, a user entered known distance between two points, a reference object of
   known size, anything else. For each, say whether it exists today, where, and what it needs.
2. **Can GroupLab measure a target it did not print?** If yes, say exactly what the user has to supply
   and what still works: hole detection, group statistics, comparison between groups, the review queue.
   If no, say which step refuses and why.
3. **What is lost without the printed markers?** Name it precisely. Likely candidates are automatic
   scale, automatic orientation, perspective correction, bull positions and therefore automatic
   assignment, and the print scale recovery in item 4. A feature that still works must not be listed
   as lost, and one that does not must not be left out.
4. **Does GroupLab recover a wrong print scale?** The research article says it measured 0.96200 on a
   sheet printed at 96.2 percent. Say what the code does with that number: is every figure corrected,
   or is it only reported? If the markers carry the scale, then a uniformly shrunk sheet measures
   correctly, because the markers shrank with everything else, and Alan's "technically a non issue" is
   right for the uniform case.
5. **What genuinely cannot be recovered?** This is the honest version of the warning and it should
   survive, because there is a real hazard here even if the stated one is wrong. Candidates: a print
   that is not uniform between the two axes, a sheet scaled or cropped after printing, a printer whose
   scaling varies across the page, and a photograph or scan whose own geometry is wrong. Measure what
   the code detects out of those, and say which it catches and which it cannot.

## 3. Then rewrite

Rewrite the two tour paragraphs, and any other place on the site, in the docs or in the application
that repeats either claim. Sweep for it, do not fix only the two sentences named above. The shape the
replacement should take, subject to what section 2 finds:

- The library page says what the library is for, and says that a GroupLab sheet is the easy path
  because the sheet carries its own scale, orientation and bull positions, and that any target can be
  measured once the scale is set, naming what is manual in that case.
- The print page keeps the real reason to print at actual size, which is not that a shrunk sheet cannot
  be measured but whatever section 2 item 5 finds to be genuinely unrecoverable. Keep the ruler check
  and the printed instruction; they are good and they stay.

## 4. Every tour page states what it does without a GroupLab sheet

Add one line to each tour page saying what that screen does with a target GroupLab did not print. On
some screens the answer is "no difference", and saying so is worth a line.

## 5. Tests

1. Add the two claims to the banned sentences test, the same mechanism entry 145 used for "nothing in
   this build changes". Ban the phrase "only measure a sheet it printed" and the claim that a scale
   error cannot be recovered.
2. A test that the tour and the research articles do not contradict each other on this point. The
   cheap version is a shared source: put the statement of what GroupLab can measure in one place and
   have both the tour and the article reference it, rather than each writing its own sentence.
3. Record in `docs/PHASE1-RESULTS.md` what section 2 measured, because the answer to "does it correct
   for print scale" is a fact about this project that should not have to be rediscovered.
