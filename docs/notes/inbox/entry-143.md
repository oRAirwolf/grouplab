# Entry 143: answers to questions 41 to 46, and the review of research batch 1

Written by the planning session at 04:30 Mountain on 2026-09-23. The review in section 1 is the planning session's, sent to Alan at the same time as this entry; the answers in section 2 are decisions.

## 1. Research batch 1: approved to publish, with four things to fix

Alan has the same review and will send "publish research batch 1" himself. The writing is good and the honesty is right: the primer article in particular says what nine shots can and cannot support, which is the whole point of the section. Fix these, and take them as the standard for later batches.

1. **Every card on the index needs a lead image, or none of them do.** Today one card in four has a thumbnail and the rest are empty, so the row is as tall as the tallest card with three large blanks in it. Give every article a lead figure, and where an article has no natural chart, a plain titled panel in the site's own style is better than a gap.
2. **The charts are light panels on a dark page.** Every figure is drawn on a near-white surface, which glares against the dark theme and looks pasted on. Have each figure script write both a light and a dark version from the same data, and let the page choose by the reader's theme. This applies to the planning drafts as well: `_style.py` should grow a dark palette and the scripts should call it, rather than each script deciding for itself. If that is more work than it is worth this week, the fallback is a consistent light card behind every figure, so at least they all look deliberate.
3. **Front matter says `status: published` on articles that are not published.** Two different meanings of the word are in play. Use `state: draft | ready | published`, where `published` is set only when a batch actually goes live, and make the build refuse a page whose state says published when it is not in a published batch.
4. **The narrow renders are not phone renders**, as `docs/RESEARCH.md` says plainly, and that is an honest note rather than a fault. Leave it. Alan will look at the real pages on his phone after the first publish and report anything that breaks.

Nothing else blocks publication. Publish batch 1 when Alan's message arrives, then offer batches 2 and 3 for review the same way rather than publishing them with it.

## 2. Questions 41 to 46

### Question 41: dragging a shot onto a bull means two different things

**Dragging always moves the shot, and never changes which bull it belongs to.** A mark's position is a measurement and a drag is how it is corrected; nothing else may ride on that gesture. Entry 141 section 5.3 item 3 is amended: assignment happens through the bull picker in the shots list, through the keyboard (select, type the bull number), and through the multiple-selection assignment you have built. Drag onto a bull is removed from the specification. If you later want a pointer gesture for assignment, it must be a distinct one, such as a drag with a modifier key held, and it must leave the hole where it is and say in the toast which bull it moved the shot to.

### Question 42: a corrected shot does not survive a second detection

**Build the matching you propose**, and do not lose a person's correction silently.

- Match each kept corrected shot to the nearest freshly detected shot within **one hole's width**, using the sheet's own size reference from entry 141 section 4 where it exists and the stated calibre where it does not. Where a match exists, the person's position and chosen bull win over the detector's. Where none exists, keep the corrected shot as it is.
- Never produce two marks for one hole; that remains the rule your current code protects.
- Also make the button honest: detecting again says, in one line, how many hand corrections it will carry over.
- Test on scan 5 and scan 6, and with a generated sheet where a correction is moved beyond one hole's width and must therefore survive on its own.

### Question 43: entry 137 names an image safety the desktop does not have

**Both, as their own item, after the current queue.** Not urgent.

1. A cap at **400 megapixels**, phrased as a limit against a hostile or broken file rather than a judgement about scanning, with the number and the measured size in the message. Alan's 600 dpi letter scans are about 32 megapixels, so the cap is twelve times his largest real file.
2. Move the decode to a background thread with a timeout, because the window freezing on a large scan is a real complaint waiting to happen. Show progress while it runs.

Until then, say in entry 137's record that the pixel cap and decode limit were not built and why, so the specification and the code agree.

### Question 44: the bent-sheet model crashes on one photograph, and improves the wrong points

**Measure first, build nothing.** Run the leave-one-marker-out measurement on the existing surface model across the paired photographs and report it. A model that cannot predict a marker it did not see will not predict a hole, and that decides whether the thin-plate spline in entry 130 section 6b item 2 is worth writing at all. Do not write the spline until that measurement is in.

The crash: spend up to an hour finding it, because an `IndexOutOfRangeException` in registration is worth understanding even in an unreachable model. If it is not obvious in that time, leave it with a test that records the crashing photograph and a note, rather than a speculative fix.

### Question 45: scan 6 reads 9 holes tonight where entry 130 recorded 10

**This is the first thing to do, before any new feature.** A real shot that the software used to find and now does not is the most serious kind of regression this project can have, and shot 6 is the one shot on that sheet that proves a group can contain something far from everything else.

1. Re-run scan 6 at the commit before entry 141 section 4 landed, and at the commit after, and report both counts.
2. If section 4 cost the hole, fix it so the sheet's own size reference does not drop a hole that a stated calibre finds, and say in the fix what the mechanism was.
3. Record the hole counts for all six range scans as a checked expectation somewhere a change like this trips over, without committing the scans: a small file of counts plus each scan's SHA-256, and a test that runs only when the folder is present and is skipped with a clear message when it is not.
4. You were right to say your earlier check was weaker than your sentence implied. Flag counts are not hole counts, and the correction belongs in the record.

### Question 46: the sheet offset is solved over every bull

**Run the measurement you propose**, both ways on scan 5, and compare each result against the offset Alan's table implies.

- If the wide solve is the better estimate, keep the code and rewrite the paragraph: the restraint is delivered by the matching, which only considers aimed bulls, and the solve is allowed to use the whole printed grid because the grid is geometry, not evidence about where the shooter aimed.
- If the narrow solve is as good, narrow it and fix the five-shot test.
- Either way, the test that broke is a case worth keeping: add it with a name that says which reading it is pinning.

Do not leave the code and the comment disagreeing, whichever way it goes.

## 3. Order

Question 45, then the batch 1 fixes in section 1, then questions 46, 42 and 41, then question 44's measurement. Question 43 last. Entry 129, the server work, waits for Alan and does not move.
