## 2026-09-18, entry 84: the editor behaves like the concept and does not look like it, and the gate needs 25 shots nobody has fired

**Status: open.** Section 2 is a blocker with a cheap way around it. Section 3 is a small correctness follow-up that should not be forgotten.

### 1. Behaviour first was the right order, and appearance is now the remaining half

The review queue, the counter, the card with its choices, the keyboard path of Space, Enter, a bull label, and N, and a "keep it" decision that saves and undoes: **that is the concept's behaviour, and it is in.**

**On the friend's scan all ten scoring shots land on their true bulls, where nearest-bull would have put two wrong, and three presses of Enter settle the queue.** That is the thing working.

**What is not in is the concept's appearance**: the icon tool strip, the breadcrumb header, the paper-coloured sheet on dark chrome, the left rail, the typography. **That was not asked for and it should not have been**, because a screen styled before it is used is styled against a guess.

**The order from here is Alan first, not planning first.** An hour in the current editor, doing the job it exists for, produces the list a styling pass should be written against. Anything I specify before that is me describing a picture.

### 2. The Phase 3 gate needs a 25-shot sheet and both real sheets have ten

`DESIGN.md` section 21 sets the gate at a full 25-shot target with several misassignments corrected in under two minutes. **Neither real sheet can test it**, and a range session is the critical path it has always been.

**There is a useful halfway step that costs nobody a trip to the range.** Build a synthetic 25-shot sheet with errors deliberately injected: two contested assignments, one merged pair, one false candidate, one bull with two shots and one with none. **Time the workflow against that now.**

**It is not the gate and it must not be recorded as passing it.** What it does is let the two-minute workflow be exercised, timed and improved before anybody drives anywhere, so that when a real 25-shot sheet exists the session measures the editor rather than discovering an obvious problem with it.

**When the real sheet is shot, one session produces three things at once**: the Phase 3 gate timed properly, the ground-truth marking entry 83 wants for scoring detector changes, and a second real sheet for the corpus.

### 3. Two things the scale fix leaves behind

**The photograph ratio must be re-measured.** Entry 79's 0.986 was taken with a single scale for the whole image, which section 2 of this week's work has just shown is wrong on any oblique frame. **Anything resting on that number rests on a measurement made with the defect in place.**

**Spurious detections on the clean photographs rose from 24 to 35**, because far-side residue now reads at its true larger size and fewer slivers fall under the floor. **That is the correct direction and it should be left alone**, per entry 83 section 3. Worth recording the rise deliberately, so that a future reader does not see 24 become 35 and treat it as a regression.

### 4. The third explanation was the right one, and none of the first two were mine by accident

The false flags on the oblique frames were explained, in order, as out-of-focus corners, then as residue on printed ink, then as **one scale applied to an image whose true scale varies from 0.85 to 1.30 across the sheet.** Only the third was right, and it was found by testing the second rather than by reasoning further.

**All three were mine, and the only reason the wrong two cost minutes instead of days is that each was written with the test that would kill it.** That is now the fourth time this week the pattern has held, and it is the single practice worth keeping from it.

**It is also the strongest argument for section 1's ordering.** A styling specification written from a screenshot would be the same kind of guess, and unlike a detector hypothesis it would take a week to disprove.
