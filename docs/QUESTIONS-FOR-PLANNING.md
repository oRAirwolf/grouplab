# Questions for the planning session

Questions going out from the Claude Code session to the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** When a genuine decision blocks you, append a dated section at the top with `Status: open`. State the question, the options with their real costs, and what you would choose and why. Commit it, push it, and stop. The answer comes back as an `open` entry in `docs/NOTES-FROM-PLANNING.md`, and once it does, change this entry's status to `answered <date>` with a one-line pointer to the notes entry that answered it. Never delete an entry.

**What belongs here.** A decision that changes the specification, changes a gate, changes geometry, or commits the project to something that is expensive to reverse. A conflict between two documents. A measurement that contradicts something written down.

**What does not.** Anything a measurement can settle, because the planning session can run measurements against the committed scans and hand back numbers. Permission for work the brief already authorises. Anything answerable by reading the documents the brief points at.

**Write it for a reader who has the repository and not the conversation.** Quote the section you are citing rather than paraphrasing it, give the numbers rather than describing them, and name the file and line where the conflict lives. A question that arrives with its evidence attached usually comes back answered in one round trip rather than three.

---

## 2026-09-23, question 47: I kept `new`, `fixed` and `changed` as release note kinds, where entry 145 names two

**Status: answered 2026-09-23.** Answered by entry 149 section 1: keep all five. `CLAUDE.md` names them and `ReleaseNoteKindsTests` holds the documentation and the generator to the same set.

### What entry 145 says

Section 3.1, quoted:

> `Release-note-kind:` says which of the two headings it belongs under: `user` or `internal`.

Read strictly, that retires the three kinds `scripts/release-notes.py` has taken since entry 132: `new`, `fixed` and `changed`.

### What I did instead

`Release-note-kind:` now accepts five words. `internal` puts a note under **Under the hood**. `new`, `fixed`, `changed` and `user` all put it under **What you will notice**, which is the only thing the two headings ask of a kind.

### Why

1. **Every trailer in the history uses the three older words.** 45 commits carry one. Retiring them would make every one of them unreadable to the generator on the day the change landed, and section 5 is explicit that this is "a rewording, not a rewrite of history".
2. **They still say something the two headings cannot.** "Fixed" and "New" tell a reader whether something was broken or absent before, which "you will notice" does not. They are not used as sub-headings any more, so nothing on the page shows them; they are simply a writer saying a little more than the minimum.
3. **The cost of being wrong is one line.** If the planning session wants exactly two, `KINDS` and `NOTICED` lose three entries and every historic trailer needs rewriting, which is a scripted change over commit messages this project does not rewrite. That is the real reason to ask rather than assume.

### What I would choose

Keep the five. If the answer is two, say so and I will map the three older words to `user` on read and stop documenting them, which keeps the history readable without keeping the vocabulary.

### Where it lives

`scripts/release-notes.py`, the `KIND`, `KINDS`, `SAME` and `NOTICED` definitions, and `CLAUDE.md`'s release notes section, which still names the three older words.

---

## 2026-09-22, question 46: the sheet offset is solved over every bull, and narrowing it makes things worse

**Status: open. Nothing is broken today; this is a line that does not do what it reads as doing.**

### What it says

`MarkingSession.SheetOffset` is documented, at length and convincingly, as the restraint that makes the whole feature safe:

> **It is only applied where the shooter has said which bulls they aimed at.** That restraint is the whole of the design. A sheet of twenty five bulls where ten were shot has a translation that explains the holes for almost any reading, so solving over every bull would let the software choose between them on a margin it cannot justify.

### What it does

```csharp
if (rule.PerBull.ContainsKey(open[i].Index))
```

`AimedBulls.For` puts **every scoring bull** in `PerBull`, giving nought shots to the ones nobody aimed at. So that test is true for the whole sheet, and the solver is handed every bull as a candidate, which is the thing the paragraph above says must not happen.

### What happened when I narrowed it

Changing it to `rule.For(open[i].Index) > 0`, which is what the documentation describes, made `SheetOffsetAssignmentTests.ToldWhichBullsWereAimedAtEveryShotFindsItsOwn` put **five of twenty shots on bulls nobody aimed at**. The real scan 5 was unaffected and still matches Alan's table exactly either way.

So the narrower question is the one the documentation asks for and the one that makes a passing proof fail. I have put it back as it was, with a note at the line, rather than shipping a change that turns a proof red on the strength of a comment.

### What I think is going on, and would check

`ImpactOffsets.Solve` takes the candidate bulls as indices into the open list. Given all twenty-five, it has more geometry to fit the translation against and lands on the right one; given only the twenty aimed at, it has less and lands slightly differently, and the matching that follows then goes wrong for the five shots furthest from their bulls.

If that is right, then the documented restraint is real but it is delivered by the **matching**, which is restricted to the aimed bulls, rather than by the offset solve, which benefits from seeing the whole grid. That would make the line correct and the paragraph above it wrong, which is worth fixing in the words rather than the code.

**What would settle it:** run `ImpactOffsets.Solve` on scan 5's geometry both ways and compare the offsets it returns against the offset Alan's table implies. One measurement, and it decides whether the code or its documentation is the thing to change.

## 2026-09-22, question 45: scan 6 reads 9 holes tonight where entry 130 recorded 10

**Status: open. It is a disagreement between two measurements, not a design question.**

### What is recorded

`docs/NOTES-FROM-PLANNING.md`, entry 130 item 2b.6, done 2026-09-22:

> **Three of the four missed holes are back**, two of them only when the calibre is named: scan 5 goes 18 to 20 and scan 6 goes 9 to 10, both exactly Alan's own counts.

Alan's table for scan 6 is 10 shots.

### What it reads tonight

Read in place from the 600 dpi scan of sheet 6 in the range folder, three ways:

| run | holes |
|---|---|
| `analyze --calibre .243` | **9** |
| `analyze` with no calibre | **9** |
| `analyze --calibre .243 --sighters` | **9** |

Scan 5 still reads 20, which is what the same item recorded, so this is scan 6 alone.

### Why it matters more than one hole

The missing one is **shot 6**, which entry 120 describes as landing left of bull 21, much lower than the rest, "a real shot, not a flyer to delete". It is the hardest hole on the sheet and the most interesting: it is the shot that proves a group can contain something a long way from everything else.

With the aimed bulls set, scan 6's nine detected holes assign one per aimed bull except bull 6, so **the assignment is right for every shot it has**. The gap is detection, not assignment.

### What I have not done

I have not gone looking for which change cost it, and I am not guessing. The obvious suspect is tonight's entry 141 section 4, which made the sheet's own marks the size reference in place of a stated calibre, and section 4.5 required the scans to be untouched by it. My own write-up of that change said the scans were untouched, and what I checked was the **flag** counts, not the hole counts. That is a weaker check than the sentence I wrote implies, and it is worth saying so.

**What would settle it:** re-run scan 6 at the commit before entry 141 section 4 landed and compare the hole count. That is one command and I would rather it were run deliberately than folded into other work.

## 2026-09-22, question 44: the bent-sheet model crashes on one photograph, and improves the wrong points on the rest

**Status: open, and nothing a person can reach is affected.**

### The crash

`grouplab compare-photos <scan 4> --model surface 20260920_153336.jpg` throws:

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at GroupLab.Core.Detection.ExpectedImage.Render(...) ExpectedImage.cs:line 31
```

Line 31 is the `mapping.ToPage` call, so the index comes from `DevelopableSurface.ToPage`, through `FoldedSheet.Sheet`. The other fourteen paired photographs of 2026-09-20 run under the same model.

**It is not reachable from the application.** `RegistrationModel.Auto` never chooses `Surface`, and the window passes no measure options, so this is a defect in a candidate model rather than a live fault. That is why it is a question and not a fix: guessing at it would be changing registration code on a hunch.

**Where I would look.** `FoldedSheet.Sheet` picks a fold with `Beyond(side, 0, ...)` and then `Last(side, ...)`, both of which index `Side.Sx`, `Side.Dx` and `Side.T` at an index the bisection derives from `side.T.Length`. A side with fewer folds than the bisection assumes indexes past the end. `count` is computed from the page's half width and half height, so a model whose `PageCentreX` or `PageCentreY` is degenerate would produce it. One photograph out of fifteen is consistent with a fit that went somewhere strange rather than with an everyday off-by-one.

### The finding that matters more

Run on the seven paired photographs it does not crash on, the bent-sheet model **improves the bull-centre error on seven of seven at the median and worsens the hole-position error on seven of seven**.

The markers are what the model is fitted to. The holes are the points it was not fitted to. A model that gets better where it was fitted and worse where it was not is describing the markers rather than the sheet, and that is the classic signature of a fit with too much freedom for its evidence.

**What I would do:** before building the thin-plate spline entry 130 section 6b item 2 asks for, measure the existing surface model leave-one-marker-out, which is the same measurement the entry wants for the new one and needs no scans at all. If the existing model already fails to predict held-out markers, a spline with more freedom will fail harder, and that is worth knowing before writing it.

## 2026-09-22, question 43: entry 137 names an image safety the desktop does not have

**Status: open, and nothing is blocked by it.** Entry 137 section 4:

> **What is accepted**: the same types and size limits as Open. Anything else is refused with the same plain message Open gives. **The same image safety applies (pixel cap, decode with a time limit) as for any file.**

**There is no pixel cap and no decode time limit on the desktop's Open path.** `ImageLoader.Load` calls `Cv2.ImDecode` and throws where it cannot decode, and nothing measures the result. The caps that exist are in `GroupLab.Core.Publication.Intake`, which is the submission path on the server and is reached by nothing the desktop does.

**What I built:** exactly the safety Open has, which is what the section's first two sentences ask for. All three routes in now share one guarded call and one refusal sentence, which is an improvement on what was there, since a file that would not decode previously went out through the crash reporter.

**What I did not build, and why not.** A pixel cap is a number somebody has to choose, and the wrong one refuses work people legitimately do: a 1200 dpi flatbed scan of a letter sheet is about 130 megapixels, and Alan's own range scans are large. A decode time limit needs a way to stop OpenCV part way, which it does not offer, so it would mean decoding on a background thread and abandoning it, which changes the shape of opening an image rather than adding a check to it.

**What I would do if you want them:**

1. A cap high enough to be about denial of service rather than taste, for example 400 megapixels, with a message saying the number and what was measured.
2. The decode moved to a background thread with a timeout, which is worth doing anyway because a large scan makes the window stop responding today.

Both belong to opening an image in general rather than to drop and paste, so they are their own item whenever you want them.

## 2026-09-22, question 42: a corrected shot does not survive a second detection

**Status: open.** Entry 141 section 5.3 item 5:

> A shot moved or assigned by hand is marked as manual, shown differently, and **never changed by a later re-detection or re-assignment.**

**Re-assignment is already safe.** `BullChosen` pins a person's bull, and `Rematch` leaves those shots alone.

**Re-detection is not.** `MarkingSession.Load` keeps exactly one kind of shot:

```csharp
var kept = State.Shots.Where(s => s.Provenance == ShotProvenance.Manual)
```

So a shot **placed** by hand survives detection, and a detected shot a person **moved or reassigned**, which is `ShotProvenance.Corrected`, is thrown away and replaced by whatever the detector finds this time. The person's correction is gone with no message.

**Why it is written that way, which is not a mistake.** A corrected shot is a detected shot. Keeping it and adding the fresh detection of the same hole would leave two marks on one hole, which is worse than losing the correction and much harder to notice.

**What I would do, and have not done:** match each kept corrected shot to the nearest shot in the new detection within about one hole's width, and where there is one, put the person's position and chosen bull back on it rather than the detector's. Where there is none, keep it as it is, since the detector has stopped finding that hole and the person said it is there. That satisfies section 5.3 item 5 without ever producing two marks for one hole. It needs a distance to be agreed and a test on the range scans.

**The alternative is to say so instead:** if detecting again is meant to be "start over", then the button that does it should say that corrections will be lost and ask first, which is a smaller change and an honest one. I would rather build the matching, but this is a behaviour question rather than a bug, so it is yours.

## 2026-09-22, question 41: dragging a shot onto a bull means two different things

**Status: open.** Entry 141 section 5.3 asks for both of these, one item apart:

> 2. **Move a shot by dragging it**; add one by a click in add mode; delete with the Delete key or a button.
>
> 3. **Assign a shot to a bull by dragging it onto the bull**, by a bull picker in the shots list, or by keyboard.

**These are the same gesture with two meanings, and the difference matters.** A mark's position is a measurement: it is where the hole is on the paper, and every figure GroupLab reports is computed from it. Dragging it is how a person corrects a mark the detector put in the wrong place by a few thousandths of an inch. Which bull a shot belongs to is a different fact entirely, and changing it must never move the hole.

If dragging onto a bull reassigns, then a person dragging a mark a long way to correct a badly placed one silently changes its bull as well. If dragging never reassigns, section 5.3 item 3's first route does not exist.

**What is built today**, `MarkingCanvas.OnPointerPressed` and `MarkingSession.AssignBull`:

- Dragging a mark moves it, as one undo step, and the nearest bull is recomputed unless a person has chosen one.
- **Click the hole, then click the bull** reassigns it, sets `BullChosen`, and never moves the mark. DESIGN.md section 13 calls this the reassignment.
- Typing a bull's label and pressing Enter does the same for the selected shot.

**What I would do, and have not done:** keep dragging as move only, and read section 5.3 item 3's "dragging it onto the bull" as satisfied by the existing click-hole-then-click-bull, which is the same two-target gesture without the risk. Then add the two routes that are genuinely missing: a bull picker on each shots-list row, and assigning several selected shots at once.

**If you want a real drag-to-assign**, the way that does not destroy a measurement is a drag that starts on the shots-list row rather than on the mark: the row is a name, not a position, so dropping it on a bull can only mean assignment. Say which you want and I will build it.

### And the same section's "select several shots" has a gesture already spoken for

Section 5.3 item 3 ends: *"Select several shots and assign them together."* The obvious gesture for adding a shot to a selection is control-click or shift-click on the mark, and on the marking canvas both are taken: entry 115 section 2 gave them to choosing **bulls** for the load field, and says explicitly "never a hole".

**What I built, and will change if you say otherwise:** a tick box on each shots-list row, the same control Session records already uses to choose sessions for comparing, with one picker above the list that assigns every ticked shot at once, as a single undo step. That needs no gesture at all, so nothing in entry 115 has to move, and it puts the multi-shot answer in the same place as the single-shot one.

## 2026-09-22, corrections made while importing the research drafts

**Status: recorded, not blocking.** Entry 142 section 3 says to raise a disagreement rather than change a claim silently. These are the changes I made to drafts on import, each with the code that settles it, so nothing was changed quietly.

### The drafts caught one of mine first, which is worth saying

`photographing-targets` asks: *"later in the afternoon: your commit says three hours later, the burst times I have are 15:33 and 16:56, please reconcile."*

**The draft is right and I was wrong.** 15:33 to 16:56 is an hour and twenty minutes, not three hours. `docs/PHASE1-RESULTS.md` said "with the sun three hours lower" and the article said "an hour and a half"; both now say an hour and twenty minutes. The conclusion is untouched, because what matters is that the sun moved between the two photographs, not how far.

### What I changed in the drafts

| draft | what it said | what the code says |
|---|---|---|
| `when-to-adjust-zero` | "where the true centre lies, 90 percent of the time" | **95 percent**. `ZeroCorrection.Level` is 0.95 and every zero figure on the screen is quoted at it. |
| `cep-explained` | "CEP 50 and CEP 90 as dashed circles" | CEP 50 is **dotted** and CEP 90 **dashed**; CEP 95 is in the figures beside them. |
| `cep-explained` | "a warning when the group is clearly oval" | Not a threshold on aspect ratio. It is a **circularity test**: "Round, as far as 24 shots can tell" when the shots cannot separate the two axes, "Not round" when they can, with the error ellipse's aspect and angle beside it. I added why the distinction matters: with few shots almost every group measures oval and almost none of them is. |

### What I checked and left alone

`mean-radius-or-extreme-spread`'s three checks are all exactly right: the 86 percent more ammunition at 25 shots is `docs/STATISTICS.md` section 5's own figure, the mean radius scale marks are word for word `MeanRadiusScale.cs`, and the headline figure is in the logo's amber.

### One thing I could not do

The drafts' figure scripts need numpy, scipy and matplotlib, and this machine has no package source to install from, so I cannot regenerate a figure from repository data as entry 142 section 3 asks. The committed figures are used and the build names the missing package rather than failing. **If a figure needs regenerating from repository data, the script has to be one that runs without those packages**, as the ones I write are.

---

## 2026-09-22, known limits of the calibre guess, recorded rather than tuned

**Status: recorded 2026-09-22, on Alan's instruction not to tune the guess on four scans.**

The guess snaps to Alan's lists and offers the neighbours it cannot separate. On the four range scans of known calibre it preselects the right one on two, offers it on three, and misses on one. These are the two that are not right, written down so they are not rediscovered:

| scan | known | preselected | offered beside it | what is wrong |
|---|---|---|---|---|
| 3 | 6.5 Creedmoor, .264 | .277 | **.264**, .284, .257 | the estimate lands at 0.2708, which is 0.0068 from .264 and 0.0062 from .277, so the wrong one is nearer by four ten-thousandths |
| 4 | .22 LR, .222 | .204 | .172 | the estimate lands at 0.1901, and **.222 is not offered at all**: its holes measure 0.052 in under the bullet where the three centrefire sheets give 0.013 to 0.019 |

**Scan 3 is the estimator running a little high.** Putting back a fixed 0.0202 in for the paper overshoots by 0.002 to 0.007 in on centrefire, and .264 and .277 are only 0.013 in apart, so a small bias is enough to tip it. The right answer is still offered, second, and the reading is marked rough.

**Scan 4 is the rimfire blind spot** question 38 found: the hole-to-bullet ratio is not calibre-independent, and it was measured on .264, .308 and .338 only.

**Neither is tuned.** Four scans, of which one is rimfire, cannot fit a correction that would not simply be four numbers memorised. What both need is a measurement across more sheets, and in particular more small-calibre ones. Until then the guess is offered and never applied, which is what makes a miss cost a person one keystroke.

---

## 2026-09-22, question 40: at a third doubles, entry 141 section 4.2 and entry 82 section 3 ask for opposite things

**Status: answered 2026-09-23.** Answered by entry 149 section 2: take the quarter-point of the smaller group. Entry 82 section 3 is amended by it, and the description still asks for the calibre.

### 1. The two rules

Entry 141 section 4.2:

> The sheet's reference is robust to the doubles it is judging: take it from the marks that agree with each other, never a plain mean of all marks.

Entry 82 section 3, which the code still follows:

> Where the round marks fall clearly into two groups, no one size fits: a sheet shot with two calibres, or one with as many merged pairs as single holes. Nothing is flagged, the veto falls back to the bound, and the description asks for the calibre.

### 2. Where they meet

On a generated sheet of fifteen bulls:

| doubles among the marks | what happens |
|---|---|
| 3 of 18 marks, a sixth | the sheet's own quarter-point, unmoved to a thousandth of an inch, and the doubles are flagged |
| 5 of 20 marks, a quarter | the marks fall into two clear sizes, so entry 82 section 3 refuses to read a size and asks for the calibre. **Nothing is flagged, including the five real doubles** |

Section 4.2 is satisfied in the first row and cannot be in the second while section 3 stands.

### 3. Why it is not obvious which should win

The two sizes on such a sheet are singles and merged pairs. The two sizes on a sheet shot with two calibres are two calibres. **The code cannot tell them apart from the sizes alone**: a merged pair is about twice the area of a single, so about 1.41 times the diameter, and .224 against .308 is 1.38. The measurement that would separate them is not in this evidence.

### 4. What I would do

Where the sheet's own marks are the reference, **take the quarter-point of the smaller group rather than refusing**. Whichever the two sizes turn out to be, the smaller marks are the better estimate of one hole: if they are singles, the doubles are then flagged correctly; if they are a second, smaller calibre, the larger holes are flagged and entry 140 section 3.2's guard turns that into one question about the calibre rather than a flood. Refusing flags nothing either way, which is the worst of the three outcomes on a sheet that really does hold five doubles.

I have not built it. It changes entry 82 section 3's behaviour beyond what entry 141 asked for, and it is a judgement about a case with no measurement behind it. `CryingWolfTests` pins both rows above, so whichever way this is settled the change is one line and the test says what moved.

---

## 2026-09-22, question 39: three of Alan's five close calibre pairs straddle his own two lists

**Status: open, and handled in the meantime.**

### 1. What the requirement says

Alan's 2026-09-22 requirement for the calibre guess gives two lists and five examples of pairs the measurement cannot separate:

> Rifle: .172, .204, .222 (shown as 22LR), .224, .243, .257, .264, .277, .284, .308, .338, .375, .416, .458, .510
> Pistol: .312, .355, .400, .410, .430, .451, .500

> where the measured size cannot tell two diameters apart (for example .222 and .224, .308 and .312, .451 and .458, .500 and .510, .400 and .410)

Three of those five pairs have one member on each list:

| pair | apart | where they live |
|---|---|---|
| .222 and .224 | 0.002 in | both rifle |
| .400 and .410 | 0.010 in | both pistol |
| **.308 and .312** | 0.004 in | rifle and pistol |
| **.451 and .458** | 0.007 in | pistol and rifle |
| **.500 and .510** | 0.010 in | pistol and rifle |

So a guess that may only offer diameters from one list can never offer three of the five pairs Alan named.

### 2. What I did

The **preselection** comes from the firearm type's list, as asked. The **neighbours** are drawn from both lists. A person who has set "rifle" and shot a 0.310 in bullet is offered .308 first with .312 beside it, rather than being told .308 on evidence that cannot separate them. The alternative, offering only same-list neighbours, would state a calibre as measured when it is not, which is the one thing the requirement says never to do.

It also covers the commoner case: the firearm type is a field somebody may simply not have set, and the default is rifle.

### 3. What I would like settled

Confirm this, or say that the lists are meant to be strict both ways and the three cross-list pairs are simply not offered together. If strict, .308 and .312 in particular will read as a firm answer on evidence that cannot support one, and I would want the wording changed to say so.

Nothing depends on the answer: the behaviour above is in, with tests for all five pairs.

---

## 2026-09-22, question 38: on a photograph, every hole measures about half again what the bullet is, so a stated calibre flags the whole sheet

**Status: answered 2026-09-22.** Alan: "yes, measure the photographed hole size factor yourself". Measured, and the answer is that **there is no factor to measure**: it is not a property of the medium. `docs/PHASE1-RESULTS.md`, "What a hole measures in a photograph", has the numbers and the recommendation, which is that a photograph's doubles are judged against the sheet's own marks rather than against any constant. No constant was changed.

### 1. What the photograph reads

Entry 140 section 4 asked me to run `20260920_165624.jpg` and report the review queue. The registration is as good as it gets: **34 of 34 markers, RMS 0.0047 in**. The group is sensible too: mean radius 0.232 in, extreme spread 0.787 in over 15 shots. So the scale is right.

The measured hole diameters are not sensible. Fifteen 6.5 mm holes, one on each of bulls 1 to 15:

| | smallest | largest | median |
| --- | --- | --- | --- |
| measured across | 0.302 in | 0.457 in | 0.383 in |

A 6.5 mm bullet is 0.264 in, and docs/SCAN-MEASUREMENTS.md measured real scan holes at 0.2099 in across. These read **1.4 to 1.7 times the bullet**, and the detector is measuring them consistently: judged against the sheet's own quarter-point mark, fourteen of the fifteen come in under 1.35 holes and nothing is flagged.

### 2. What it does to the "possibly two holes" check

Against a stated calibre the same fifteen marks read:

| calibre stated | hole taken as | marks flagged | range, holes' area |
| --- | --- | --- | --- |
| .224 | 0.212 in | 15 of 15 | 1.98 to 3.62 |
| 6.5 mm, **the right one** | 0.249 in | 15 of 15 | 1.42 to 2.61 |
| .308 | 0.291 in | 11 of 15 | 1.36 to 1.92 |

The 6.5 mm row matches Alan's screenshot ("shot 15: 0.412 in, 2.36 holes"; here a mark of 0.416 in reads 2.36 holes). So **the calibre that followed him across was the right calibre for this sheet**, and it still flagged every hole. Entry 140 section 3's suspicion that the previous sheet's calibre was kept is confirmed, but the carry-over was not what made the flags wrong: stating the true calibre does the same thing on this photograph.

Entry 140 section 3.2 is now in, so this is one review item asking about the calibre rather than fifteen, and section 1 means the calibre no longer arrives uninvited. Nobody is flooded any more. But a person who correctly types 6.5 mm on this photograph still gets told the sheet looks like doubles, and that is wrong.

### 3. What I think is happening

Entry 82's reference sizes are calibrated on **scans**: a flatbed's white lid behind the sheet, the core of the hole reading V 192. This is a photograph, with whatever was behind the target and light across the surface. The residual a photographed hole leaves is the hole plus its disturbed rim plus the shadow in it, and the hull thrown round that is wider than the paper that is actually missing. `RenderDifferenceOptions.LargestHoleInches` is 0.60, so 0.46 in is well inside what the detector will accept as one hole; nothing refuses these, they are simply measured large.

### 4. What I would do

**Scale the flag's reference by how the sheet was imaged**, not by changing the calibre a person typed. Concretely: where the marks came from a photograph rather than a scan, take the flag size as the calibre times a photograph factor measured from the corpus, in the same way entry 80 section 2 measured the render-and-difference factor of 0.944 for scans. On this sheet the factor needed is about 1.45, and the fifteen marks would then read near 1.0 holes each while a genuine pair still reads near 2.

I have not built it, because the factor has to be measured rather than guessed, and the only photographs I may read are Alan's range folder, which is one session of one calibre. **What I need is either permission to measure the factor across the range folder's photographs** (read only, nothing committed, no metadata read), **or the number itself.**

Until then the behaviour is safe rather than right: one question instead of fifteen, and no assumed calibre.

---

## 2026-09-22, question 37: the point-of-impact correction works, and there is no way for a shooter to switch it on

**Status: answered 2026-09-23.** Answered by entry 149 section 3: build A and D, not C. Not built yet; recorded as outstanding in `docs/PHASE1-RESULTS.md` under entry 149.

### 1. What happened

Entry 130 section 3.1 asked for the sheet's point of impact to be found before assignment and the holes assigned in that shifted frame. The solver was built last night. Tonight it turned out to be **connected to nothing**: `ImpactOffsets.Solve` was called by no part of the assignment path, so scans 4, 5 and 6 still assigned exactly as entry 120 found them, and scan 5 still reported a mean radius of 1.120 in from a group measured against bulls it was never fired at.

It is connected now, and it works: on a synthetic sheet shot at the second to fifth bull of every row with the whole group landing a bull to the left, all twenty shots find the bull they were fired at.

### 2. The problem

**It only runs where the shooter has said which bulls they aimed at, and there is no control anywhere that says so.**

The restraint is deliberate and I would keep it. A sheet of twenty five bulls with ten shot has a translation that explains the holes for almost any reading, so solving over every bull would have GroupLab choosing between readings on a margin it cannot justify, on exactly the sheets where being wrong is quietest. Where the shooter names the bulls, the question stops being "where might these have been aimed" and becomes "how far from there did they land", which is arithmetic.

But today the only way to name them is `AssignmentRule.PerBull`, which exists for the doubles sheet of entry 113 and is reached from no screen for this purpose. **So the fix for the worst defect this project has found is in the build and out of reach.**

### 3. The question

**How should a shooter say which bulls they aimed at?**

- **A. On the sheet itself, by clicking.** Before or during marking, click the bulls you shot at; they light up; everything else is left empty. Clear, and it is the same gesture as the rest of the marking screen.
- **B. As a pattern, chosen from a short list.** "Every bull", "the first N", "columns 2 to 5 of each row", "these rows". Fast for the regular cases and useless for an irregular one.
- **C. From the rounds fired.** The shooter already enters how many rounds they fired. Take the N bulls the shots best fit and let them correct it. Needs no new control at all and guesses, which is the thing this project does not do.
- **D. Ask only when it would change the answer.** Assign as today, and when a certain offset exists that would move shots, say so in the review queue and offer to apply it.

**I would build A and D together.** A is the honest control and belongs on the marking screen with the other bull interactions; it is also what entry 131's editing popover already needs, since clicking a bull is one of its gestures. D is what makes A discoverable: nobody will use a control they have no reason to look for, and the review queue is where GroupLab already says "this needs you". C I would not build: it turns a fact the shooter knows into a guess the software makes, on the one question where guessing wrong is invisible.

**Until one of them exists, the note in the release should not claim this is fixed for anybody who has not read the code.** I have worded tonight's release note as "once you tell GroupLab which those were" for that reason.

---

## 2026-09-22, question 36: a light installer, measured, and why shrinking the one we have beat it

**Status: open**

### 1. What was asked

Entry 133: measure a framework-dependent build, say what a light installer would cost and gain, and recommend. Measure and propose only.

### 2. The figures, measured on this machine tonight

| | unpacked | zip | installer |
|---|---|---|---|
| self-contained, as shipped before tonight | 332.6 MB | about 102 MB | 97.3 MB |
| framework-dependent, no runtime inside | 227.9 MB | 78.2 MB | not built |
| **self-contained, with tonight's symbol fix** | **204.3 MB** | not built | not built |

What makes up the framework-dependent build, 78 files:

| part | MB |
|---|---|
| Avalonia and Skia | 121.4 |
| OpenCV's native library | 93.6 |
| everything else | 7.9 |
| GroupLab itself | 5.0 |

**Measuring this is what found the real problem.** 100.7 MB of the shipped build was debug symbols, and 100 MB of that was two files: `libSkiaSharp.pdb` at 80.1 MB and `libHarfBuzzSharp.pdb` at 19.9 MB. Native symbols for Skia and HarfBuzz, which nothing at runtime reads, no crash report here can use, and no user will ever open in a debugger. They are now left out, which took 332.6 MB to 204.3 MB with the analysis unchanged: 25 holes, 25 shots, mean radius 0.232 in on the sample, exactly as before.

**So the self-contained build is now smaller than the framework-dependent one was**, 204.3 MB against 227.9 MB, and it carries its own runtime.

### 3. Updates

Entry 133 section 2 expected the gain to be in updates rather than first installs, and that was the right instinct, but the arithmetic has moved. An update today downloads the whole installer, about 97 MB, and will now be substantially less. A framework-dependent update would carry perhaps 60 MB of that, since Avalonia, Skia and OpenCV travel either way and they are 215 of the 228 MB. **The runtime is not what makes a GroupLab update large. The drawing and vision libraries are.**

### 4. No administrator prompt, ever

The per-user route does exist: Microsoft's `dotnet-install` script installs into a folder without elevation, and since .NET 9 an application host can be told to look in a private location with `AppHostDotNetSearch` and `AppHostRelativeDotNet`. On a clean Windows user account that works without a prompt.

**But it is a new failure surface on somebody's first run**, and a first run is when a person decides whether to keep the application. The install script must be downloaded and its hash checked, a 70 MB runtime fetched over whatever connection they have, and any of it can fail behind a corporate proxy or an antivirus that objects to a script fetching an executable. The self-contained build has none of those steps: the installer is the application.

Where a suitable runtime is already present, the private copy would be skipped and nothing downloaded, so the light installer would be small and fast for exactly the people who least need it to be.

### 5. Keeping the runtime patched

This is the part I would not want to own. A self-contained build gets .NET security fixes whenever GroupLab is rebuilt, which is every green push. A per-user runtime under `%LOCALAPPDATA%\GroupLab\dotnet` is patched by nobody: Windows Update does not see it, Microsoft's updater does not know about it, and it would fall to GroupLab to notice a CVE, fetch a new runtime and swap it under a running application. That is a real ongoing obligation on a project with one maintainer.

### 6. Cost

Two packages to build, test and support on every release. The updater must keep each install on its own kind for ever, because a light install cannot take a self-contained update or the reverse, which means the train, the manifest and the installer all grow a dimension. Given entry 123 section 2.7 found three separate defects in the single-package updater in one night, doubling its cases is not a small ask.

### 7. Recommendation

**Not yet, and shrink the one we have instead.** The measurement says the light installer solves a smaller problem than it looked: the runtime is about 100 MB of a 332 MB build, and tonight's symbol fix removed 128 MB for no cost, no new failure mode and no second package.

What I would do next, in order:

1. **Done tonight:** leave out the native debug symbols. 128.3 MB, no behaviour change.
2. **Next, and I would measure before building:** `opencv_videoio_ffmpeg4130_64.dll` is 27.3 MB and GroupLab reads still images. If video capture is genuinely unused, that is 27 MB more for nothing.
3. **Then consider trimming.** It would reach the managed assemblies, not the 215 MB of native libraries, so the gain is modest. Avalonia uses reflection for styling and data binding, so trimming it risks failures that appear only at runtime on a screen nobody tested, which is the worst kind. I would not trim without a full control walk on all three platforms afterwards.

Revisit the light installer if the download is still thought too large after 1 and 2, because then the remaining weight really is Avalonia, Skia and OpenCV, and none of those is fixed by leaving the runtime out.

---

## 2026-09-22, question 35: holes off the bull grid are now kept, and that loosens the one rule that removed every false positive

**Status: answered 2026-09-23.** Answered by entry 149 section 4: keep one bull's width, then re-run the entry 121 survey's own baselines against the narrowed rule before any claim is made from it.

### 1. What was asked for and what was built

Entry 130 section 2b.4: "Holes anywhere on the sheet's registered page count, including outside the grid and the margins; they are shown, counted and offered for assignment or left unassigned, never dropped."

Scan 5 had two such holes, above bull 2 and left of bull 12, and neither was detected. They were refused purely for position, by this rule in `RenderDifferenceHoleDetector`:

> a candidate centred outside every bull's cell is refused, **which is the position prior that would have removed every false positive the survey's baselines made**

### 2. Why I did not do exactly what was asked

The entry says anywhere on the page, including the margins. The detector's own documentation says the margins are where the false positives were, and that this rule is what removed all of them.

An invented hole is worse than a missed one. A shooter who fired fifteen and is shown fourteen can see that a shot is missing; a shooter shown sixteen cannot see that one was never fired, and the figures move either way. Entry 120 measured "no false holes anywhere" across six real scans with this rule in place, and that is a property worth more than two recovered holes.

So the rule is **narrowed rather than dropped**: a mark within one bull's width of a cell is kept, left unassigned, and put in the review queue; a mark further out is still refused. That admits both of scan 5's holes, because both are beside a bull rather than out in the margin.

### 3. What I could not do, and it matters

**Entry 130 section 2b.6 asks for all six scans to be re-run afterwards, and I could not reach it.** So this change is reasoned but not measured: I do not know how many of the survey's false positives fall within a bull's width of a cell rather than out in the margins. If many do, this reintroduces them.

### 4. The question

**Is a bull's width the right boundary, and should it be confirmed against the scans before the next release?**

- **A. Keep it at one bull's width**, and run section 2b.6 as the first thing in the next session; tighten it if false positives reappear.
- **B. Tighten it now** to something smaller, half a bull's width, which still admits scan 5's two holes if they are close in, at the cost of missing a wilder shot.
- **C. Do exactly what the entry says** and accept anywhere on the page, taking the false positives back.

**I would choose A**, and I would treat running 2b.6 as blocking the next release rather than as tidying up. The change is off by nothing: it is live in the detector now, and until the scans are re-run the claim "no false holes anywhere" is no longer something this project can say truthfully.

### 5. Measured, later the same night

Section 2b.6 was run after all: all six scans, read only. **The narrowed rule did not reintroduce a single false hole.**

| Scan | Shots (Alan) | Entry 120 | Now, no calibre | Now, calibre named |
|---|---|---|---|---|
| 1 | 15 | 14 | 14 | 14 |
| 2 | a zero group | 0 | 0 | — |
| 3 | 25 | 25 | 25 | 25 |
| 4 | 23 | 19 | 19 | **24** |
| 5 | 20 | 18 | **20** | **20** |
| 6 | 10 | 9 | 9 | **10** |

Scan 5 reads 20 against Alan's 20, which is the two holes this change was made for and nothing else. Scans 1, 2, 3 and 6 are unchanged or better, and none of them is over its ground truth. Scan 3's gate figures are identical: 25 holes on 25 bulls, mean radius 0.232 in.

**So one bull's width is measured as safe on this material, and I would now choose A with more confidence than I had when I wrote it.** The one over-count in the table, scan 4 at 24 against 23, comes from the size gate of section 2b.3 and not from this rule: without a calibre that scan still reads 19, below its ground truth, so no position-refused mark is being admitted there.

**What is still unmeasured** is the survey's own baselines, which is where the false positives were counted in the first place. This table is six real scans, not that survey, and re-running it against the narrowed rule is the honest completion of this question.

I have deliberately not claimed it in the results.

---

## 2026-09-22, question 34: pooling two sheets of one load needs a rule for what a pooled group's centre means

**Status: open**

### 1. What was asked for

Entry 130 section 4.2: "Pool two sheets of one load (scans 1 and 3, 40 shots): let a person combine sessions of the same load into one group for analysis and comparison, keeping each shot's sheet and bull. If this turns out large, build the core and record the rest as a question."

It turned out large, and the reason is not the plumbing.

### 2. The part that is plumbing, and is fine

Keeping each shot's sheet and bull, gathering shots from two sessions, and computing dispersion over the combined set is straightforward. Mean radius, sigma and the shape tests all work on a set of radii from a centre, and forty shots is simply a better estimate than twenty. That part can be built without asking anybody.

### 3. The part that is a decision

**A pooled group has no single centre, and which centre is used changes what the figures mean.**

Two sheets of one load, shot at different times, have two points of impact. They usually differ, because the rifle was picked up and put down, the ammunition warmed, the wind changed, or the shooter's position moved. So there are three defensible things "the pooled group's centre" could be, and they measure different quantities:

- **A. One centre for all forty shots.** The dispersion then includes the movement between the two sessions. This measures what the rifle and shooter together will do over a day, which is what somebody zeroing for a match wants.
- **B. Each sheet centred on itself, then the radii pooled.** The dispersion is the within-session dispersion only, and the movement between sessions is thrown away. This measures the ammunition and the rifle, which is what somebody comparing two loads wants.
- **C. Both, reported side by side**, with the difference between them named as the session-to-session movement.

These are not the same number and the gap between them is the interesting part: if A is much larger than B, the rifle is not holding its zero between sessions, and that is a finding in itself.

### 4. Why I am not choosing

`docs/STATISTICS.md` is explicit that a figure has to say what it is an estimate of. B pooled into one mean radius reads exactly like a twenty shot group's mean radius but is not an estimate of the same thing, and nothing on the screen would distinguish them. A is honest but answers a question a load comparison is not asking. Choosing quietly would put a number in front of somebody that means something other than what they think it means, which is the failure mode entry 120 section 2 was about.

### 5. What I would choose, and why

**C, with B as the headline.** A person pooling two sheets of one load is almost always comparing loads, so the within-session dispersion is the figure they want, and it is the one that stays comparable with every other group in the record book. The session-to-session movement is then reported beside it as its own quantity, in inches, rather than being hidden inside a larger mean radius.

It costs one extra line on the screen and answers both questions instead of silently answering one.

### 6. What is built meanwhile

Nothing of the pooling, deliberately. The core of it is inseparable from the choice above: the first thing the code has to do is pick a centre. Building it with a centre chosen by me and changing it later would mean any pooled figure recorded in between is not comparable with the ones after, and the record book keeps figures.

---

## 2026-09-21, question 33: an update that will not start cannot roll itself back, and making it able to costs a second copy of the program

**Status: answered 2026-09-21** by `docs/NOTES-FROM-PLANNING.md` entry 124 section 2: keep option A now, build option B at the first of the beta train opening or a second person testing, and do not build option C. The trigger is recorded in `docs/UPDATES.md` under "When this changes", and the "If a new build will not start" paragraph now also appears in `docs/TESTING-GUIDE.md`, where a tester will look for it.

### 1. What was asked for

Entry 123 section 2.4: "If the new version fails to start, the previous install must still be usable; say how that is guaranteed or, if it cannot be with the chosen mechanism, say so and what the user does."

### 2. What is true today, and it is the "cannot" half

The mechanism chosen in entry 123 section 2.1 is the Inno Setup installer run silently. `packaging/windows/grouplab.iss` installs with

```
[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
```

so the new build is written over the old one in `{app}`. Once it has run, the previous build's files are gone. **There is no second copy and nothing to roll back to on disk.** Saying it were otherwise would be a lie in the one place a lie costs most.

What is guaranteed, and what the person actually does, is in `docs/UPDATES.md` under "If a new build will not start":

1. Nothing of theirs is at risk. Everything lives in `%APPDATA%\GroupLab`, which no installer or uninstaller touches. A bad build cannot lose a session.
2. The build they were on still exists at its own address, because `nightly.yml` publishes `v<version>` beside the rolling `nightly` tag and keeps the newest thirty.
3. To go back they download that build's installer and run it over the broken one.

That is three steps and a working machine to do them on. It is not nothing, but it is not a roll back either: it needs the person to notice, to know where to look, and to have a browser.

### 3. The options, with their real costs

**A. Leave it as it is.** Cost: nothing new. A build that will not start needs a person to go and fetch the previous one. Every nightly tester is Alan today, so the cost is currently one person's ten minutes, once, if it ever happens.

**B. Keep the previous install beside the new one.** The installer moves `{app}` to `{app}.previous` before writing, and a `Roll back to <version>` shortcut runs the previous copy's own installer. Cost: the install doubles on disk, about 190 MB becomes about 380 MB, for as long as the previous copy is kept; the uninstaller has to learn to remove both; and it is new `[Code]` in the installer, which is the one part of the build nothing tests.

**C. Have GroupLab prove the new build starts before the old one is gone.** The installer would run the new build once with a `--prove-it` switch and put the old one back if it does not report success within some number of seconds. Cost: all of B, plus a timeout to get wrong, plus a mechanism that can itself fail, and a person watching a progress bar for longer. This is what the updater frameworks do, and they do it with years of edge cases behind them.

### 4. What I would choose, and why

**A, for now, and B when there is a second tester.** The whole argument for B is that somebody who is not Alan hits a broken nightly, cannot diagnose it, and gives up; today there is nobody in that position, and the nightly train is explicitly the train where broken builds are expected. When the beta train opens, or when anybody but Alan installs GroupLab, B stops being insurance and starts being necessary, and it is cheaper to build then than to build now and maintain untested installer code in the meantime.

I would not build C at all. It is the right answer for a product with a support queue and the wrong one for a project where the honest fallback is a three-line paragraph in `docs/UPDATES.md`.

---

## 2026-09-21, question 32: a portrait page cannot fill half a landscape window, so the library's acceptance test measures something else

**Status: answered 2026-09-21** by `docs/NOTES-FROM-PLANNING.md` entry 124 section 3: option A, the measures held below. Entry 124 asked whether the 240 by 340 figure in section 2 was a slip. **It is not: measured, the preview is 242 by 342 at 1280 by 720 and 750 by 1062 at 2560 by 1440.** Nor does it mean the 95 percent test should be failing, because that test measures the preview against the room it is given, not against the window: the preview gets 337 pixels of height and fills 342 of it. The reason the page is not the 450 to 550 pixels entry 124 expected is that only 337 of the window's 720 reach the preview at all. The budget is in section 5 below, added when the question was closed.

### 1. What was asked for

Entry 120 section 10.4: "a layout test that renders the library at 1280 by 720 and at 2560 by 1440 and asserts that no list item's text is truncated or overlapping, and that the preview's area is at least half the window's".

### 2. Why the second half cannot be met

The preview is a page, and a page is portrait: 8.5 by 11, an aspect of 0.77. A window is landscape: 1280 by 720 is 1.78. Fitted to the full height of a 1280 by 720 window the page is about 240 by 340 pixels at the scale the preview renders, which is 9 percent of the window's area, and no layout can do better without cutting the page off or stretching it out of shape. At 2560 by 1440 it is 22 percent. **The measure is unreachable at any window shape a person actually uses.**

### 3. What is built and what is held instead

The layout does what section 10.2 asks: the list column is wide enough for the longest name and its paper and bulls with nothing cut or overlapping, the sheet takes everything the list does not, and the preview fills that room to its full height and grows with the window. The test holds:

- no name trimmed, wrapped rather than cut, and never drawn into the size column;
- the size column inside the list column, which caught a second cut the first render had;
- the preview at least 95 percent of the height it is given, so it is filling its room rather than sitting at its own size;
- the sheet wider than the list at every size, and taking all the width left over.

### 4. The question

**Is that the right measure?** If half the window's area is wanted literally, the preview would have to be cropped to the window's aspect, or shown two pages side by side, or the sheet rotated; each is a different screen and none is obviously better.

- **A.** Keep the measures above, which say "it fills the room it has" rather than "it is half the window".
- **B.** Name a different number, such as the preview filling at least 90 percent of the height it is given, which is what A already holds.
- **C.** Change the screen so a page can fill more of a landscape window.

**I would keep A.**

### 5. The measured budget, added 2026-09-21 when entry 124 section 3 closed this

Entry 124 expected a letter page fitted to the full height of a 1280 by 720 window to be roughly 450 to 550 pixels tall "after the window's own chrome". It is 342. The difference is not the fit; it is how little of the window reaches the preview. Measured by `LibraryLayoutTests`, which prints these numbers on every run:

| | 1280 by 720 | 2560 by 1440 |
|---|---|---|
| window | 720 | 1440 |
| everything above and below the split: the header, the status line, the screen's heading block and the margins | 210 | 210 |
| the chosen sheet's detail block: name, summary, identifier and file, the read-only line, Print and Duplicate | 129 | 129 |
| the zoom row under the preview | 39 | 39 |
| **left for the page** | **342** | **1062** |
| page drawn | 242 by 342 | 750 by 1062 |

The three deductions are the same number of pixels at both sizes, because none of them grows with the window: 378 of the height is spent before the page gets any. At 1280 by 720 that is 53 percent of the window; at 2560 by 1440 it is 26 percent. That is the whole of the difference, and it is why the page looks right on a large screen and cramped on a small one.

**Nothing was changed on the strength of this**, because the 342 is exactly the room the preview is given and it is filling it; making the page bigger at 720 means taking room from the heading block or the sheet's detail block, and which of those a person needs less is a design question rather than a measurement. It is recorded here so the numbers are on the record and the next person asking "why is the page small" has the answer without re-deriving it.

---

## 2026-09-21, question 31: the update manifest is signed with ECDSA P-256, not the Ed25519 entry 119 asks for

**Status: answered 2026-09-21** by `docs/NOTES-FROM-PLANNING.md` entry 130 section 5: option A. ECDSA P-256 with SHA-256 stays, and every manifest names the algorithm it was signed with, in its own `algorithm` field, which the application checks before it checks the signature. .NET 10 has no Ed25519, this repository has no NuGet source so none can be added, and a signature scheme nobody can verify is worse than a different one everybody can. The name in the manifest is what makes a later change possible: a build that meets an algorithm it does not know refuses it by name rather than guessing.

### 1. Why not Ed25519

Entry 119 section 3.2 says "Generate an Ed25519 key pair". Two things stand in the way, and neither is a preference:

- **.NET 10 has no Ed25519.** Its cryptography has ML-DSA and SLH-DSA, the post-quantum signatures, and ECDSA and RSA, and no Ed25519.
- **No package can be added.** This machine has no NuGet source configured at all: `dotnet nuget list source` reports none, and a restore uses only what is already in the local cache. Adding BouncyCastle or NSec for Ed25519 fails at restore, here and on any machine set up the same way.

### 2. What is built

**ECDSA over P-256 with SHA-256**, which is in .NET on every platform GroupLab builds for, and which OpenSSL on the build runner can also produce. Everything else is as the entry describes: the private half lives only as the repository secret `GROUPLAB_UPDATE_SIGNING_KEY`, the public half is compiled into the application, the workflow fails loudly rather than publishing unsigned, and a build with no key installs nothing.

**Every signed manifest names its algorithm**, and the application refuses one whose algorithm it does not know. So changing to Ed25519 later is a manifest older builds refuse by name rather than a silent substitution, which is the property that matters.

### 3. The question

- **A.** Keep ECDSA P-256, and say so in `docs/UPDATES.md`, which it does.
- **B.** Use ML-DSA, which is also in the box and is post-quantum, at the cost of a much larger signature and a much less familiar algorithm.
- **C.** Add a NuGet source to the build machine so a package can be restored, and use Ed25519 after all. That is a change to the machine rather than to the code, and it is Alan's to make.

**I would keep A** until somebody has a reason to prefer another, since what protects a person here is that the key is secret and the algorithm is named, not which curve it is.

---

## 2026-09-21, question 30: the ordering entry 119 asks for is not the SemVer ordering it cites

**Status: answered 2026-09-21** by `docs/NOTES-FROM-PLANNING.md` entry 130 section 5: option A. Strict SemVer 2.0.0 precedence, so a pre-release sorts below the release it is named for and `0.2.0-nightly.16` is older than `0.2.0`. The two ranks a third train would need are written down in `docs/UPDATES.md` rather than built, because a rank that nothing uses is a rank nothing tests. What the updater actually relies on is narrower and safer than any ranking: a train takes builds from itself and from steadier trains only, so ordering never has to decide between two trains that both have a claim.

### 1. The conflict

Entry 119 section 1.3 says: "Ordering follows SemVer precedence, so `0.1.0-nightly.12 < 0.1.0-beta.1 < 0.1.0 < 0.2.0-nightly.1`. Write that ordering as tests."

**SemVer precedence does not give that ordering.** SemVer 2.0.0 rule 11 compares pre-release identifiers that are not numbers "lexically in ASCII sort order". `beta` sorts before `nightly`, so strict SemVer puts `0.1.0-beta.1` **below** `0.1.0-nightly.12`, which is the opposite of the line above. The rest of the entry needs the entry's ordering rather than SemVer's: section 4.5 says a nightly user takes a newer beta or release and never the reverse, which only works if a beta of the same version reads as newer than a nightly of it.

### 2. What is built

Both, in two places, so neither is bent to fit the other.

- **`SemanticVersion` is strictly SemVer**, because it also reads tags and anything else that claims to be a version, and a type called SemanticVersion that is not one would be a trap.
- **`UpdateOrder` is what the updater uses.** It ranks the trains by how steady they are, nightly below beta below release, and falls back to SemVer's own comparison for anything not on a train. The entry's ordering is a test, and so is the fact that strict SemVer disagrees, so nobody later "fixes" one into the other by accident.

### 3. The question

**Is the train ranking what you meant?** I have assumed yes, because section 4.5 cannot work otherwise.

- **A.** Keep it: the updater ranks trains, and the documentation says where it departs from SemVer and why.
- **B.** Rename the trains so that SemVer's own ordering is right, for example `alpha` for nightly, since `alpha < beta`. That gives one rule everywhere and costs a rename of the tag and the pre-release identifier. It also makes the version string say "alpha" to a person who was told the train is called nightly.
- **C.** Something else you have in mind.

**I would keep A** and write B down as the thing to do if a third train ever appears, since two ranks are easy to hold in the head and four are not.

---

## 2026-09-20, question 29: the repository has no shot GroupLab sheet that may be published, so the package's sample is generated

**Status: answered 2026-09-21**, by `docs/NOTES-FROM-PLANNING.md` entry 120 section 9. Neither A nor B: Alan gave a real sheet instead. Scan 3 from the 2026-09-20 range day is the sample, published under the consent record in `samples/PROVENANCE.md` with his words in it, and its ground truth of 25 shots is what the package's self-test holds the package to. The generated sample is no longer in the package.

### 1. What entry 116 section 2 asks for

"A `samples` folder with one or two scans **already committed in the public repository**, so nothing new is published, and the sheet definitions they belong to. **Nothing from `scans/` that is not already public, and nothing donated.**"

### 2. What is in the repository

- **`scans/phase0/gl-cf25-ltr-*.png`** are Alan's own printed GroupLab sheets, scanned at 300 and 600 dpi. **They have no shots in them**: they are print-quality references. Analysed, they give "24 markers found, 0 holes".
- **`scans/phase0/20260913_*.jpg`** are Alan's own photographs of the same unshot sheets.
- **`scans/phase1/*.jpg`** are donated or collected from elsewhere, and are the ones the entry excludes.
- **Yesterday's sheet**, the one real shot GroupLab sheet, is not in the repository: entry 114 section 2's scan is not on this machine.

So a sample that lets a tester "open GroupLab, analyse a sheet and read the figures" cannot come from the repository's public, undonated images, because none of them has a hole in it.

### 3. What is built

The package carries two samples:
- **`samples/gl-cf25-ltr-300-dpi.png`**, copied from `scans/phase0`, which is Alan's own unshot sheet. It shows registration on real paper through a real printer and scanner.
- **`samples/sample-25-shots.png`**, **generated at packaging time** by `grouplab sample`, which renders GroupLab's own sheet and shoots at it with a seeded random number generator. It carries nobody's data, needs nobody's consent, and is not a photograph of anything; it is the sample the README.txt tells a tester to open, and it is not committed to the repository.

A test holds the packaging script to that: the only image it copies from the repository is the unshot sheet.

### 4. The question

**Is a generated sample acceptable in the package?** It is new in the sense that it did not exist before, which the entry's "so nothing new is published" may rule out, and it is not new in the sense the rule is for: it is not anyone's target, and no consent question can arise.

- **A.** Keep it, as built.
- **B.** Ship only the unshot scan, and let a tester see registration but no figures until they shoot a sheet.
- **C.** Wait for a real shot sheet of Alan's own, with its provenance record, and ship that instead.

**I would keep A** and replace it with C when a real sheet exists, because a tester who cannot see the figures in the first minute has not seen GroupLab at all.

---

## 2026-09-20, question 28: the support link needs an address

**Status: answered 2026-09-21**, twice, and closed for good by `docs/NOTES-FROM-PLANNING.md` entry 126 section 1. Entry 120 section 9 answered it first with a placeholder, because there was no address and inventing one would have sent somebody who wanted to help the project to a stranger's website. grouplab.org then went live, so `SupportLink.Address` is now `https://grouplab.org/support/` and `SupportLink.Email` is `support@grouplab.org`. It is still one constant in `src/GroupLab.App/SupportLink.cs` and still opened through `IOutsideWorld`. `SupportLinkTests` no longer holds that there is no address: it holds that these two are the only ones anywhere a person receives, and that grouplab.org is the only GroupLab domain named at all, because the website is built from this repository and a wrong domain here would be published.

### 1. What is asked for

`DESIGN.md` section 20 and the README's Phase 4 list promise "an unobtrusive support link, one menu item opening a browser, with no payment handled inside the application". Entry 115 section 7 says it needs a page address that has not been given, so nothing is built.

### 2. What is needed to build it

- **The address**, exactly as it should be opened, for example `https://pissinhot.com/support` or a Ko-fi or GitHub Sponsors page.
- **What the item should say.** "Support GroupLab" is the obvious wording; anything else is Alan's to choose.
- **Where it belongs:** the settings screen, beside Report a problem, is the unobtrusive place. The header's menu is the other candidate and is busier.

### 3. What is built

Nothing, as the entry says. It is one menu item and a browser launch once the address exists, and the launch path already exists for the print screen's Open to print.

---

## 2026-09-19, question 27: a sheet's subgroups can be compared, but nothing on screen assigns bulls to them

**Status: answered 2026-09-20** by `docs/NOTES-FROM-PLANNING.md` entry 115 section 2: option A, a load field in the editor's panel, with the addition that several bulls are set at once. Built: with the select tool, shift and click chooses bulls on the sheet, and the load field sets them all, from the loads the records carry. A bull with no load belongs to no subgroup, and the panel says how many those are.

### 1. What entry 113 section 2 asks, and what the code has

Entry 113 section 2: "Pick two or more from the Session records list, or the subgroups of one sheet, and see them side by side."

The subgroups exist in the engine:
- `MarkingSession.SetSubgroup(bull, name)` sets them.
- `GroupAnalysis.Subgroups` analyses them.
- The marking file keeps them.

The README marks this "Built, not proven". But no screen and no command sets a subgroup. A search of `src/GroupLab.App` and `src/GroupLab.Cli` for `SetSubgroup` finds nothing. The only way a sheet carries subgroups today is a marking file written by hand, or by a test.

### 2. What is built

The comparison screen compares a sheet's subgroups whenever the marking open in the analysis names two or more, and a test covers that path. Sessions chosen in Session records are the path a person can use today.

### 3. The options

- **A. A load field in the editor's selected-bull panel.** It names the load for the selected bull, or for a run of bulls chosen together.
- **B. A small table on the analysis screen.** One row per bull, with its load typed in.
- **C. Leave subgroups to the marking file** until a sheet needs them. Jeff's thirty bulls, entry 89 section 3, is the case that does.

**I would build A.** It is where a person already selects bulls, and it is a small change. I have not built it, because the entry does not ask for it.

---

## 2026-09-19, question 26: the dope table has no slot in the concept's rail, so it has a seventh one

**Status: answered 2026-09-20** by `docs/NOTES-FROM-PLANNING.md` entry 115 section 1: option A, the Ballistics slot stays. Seven buttons including the gear is not a crowded rail, and a dope table belongs to a rifle and a load rather than to a sheet. Nothing changed.

### 1. What entry 112 section 4 asks, and where it could go

Entry 112 section 4 asks for "a dope table for a rifle and load: range, drop and wind per 10 mph in the person's units and clicks", and for the solver's fields on the rifle and load records. The concept's rail (`docs/figures/screens/`) has five destinations: the analysis, the target library, print, session records and reports. None of them is a place for a table that belongs to a rifle and load rather than to a sheet.

### 2. What is built

A sixth destination, **Ballistics**, between Session records and Reports, with its own icon, a trajectory's arc. The rail now has seven buttons with the gear. The screen:
- picks a rifle and a load from the records;
- edits their solver fields;
- takes the air;
- shows the table.

The analysis's zero block carries its correction from the same records and air.

The solver's inputs are in the units the solver and most published load data use:
- sight height, twist and bullet length and diameter in inches;
- muzzle velocity in ft/s;
- bullet weight in grains;
- temperature in degrees F, pressure in inHg and altitude in feet.

The zero distance and the table's ranges follow the distance unit, and every output follows the unit settings.

### 3. The options

- **A. Keep the seventh slot.** Costs one more icon in a rail the concept drew with five.
- **B. Put the table under Session records.** Costs mixing a rifle's table into a list of sheets.
- **C. Put it behind the rifle and load records in the editor's side panel.** The panel is 372 pixels wide, and the table has seven columns.

**I would keep A**, and I would ask separately whether the inputs should follow a metric setting too. Changing either is a small change.

---

## 2026-09-19, question 25: ballistics.js's G1 table is not the standard G1 function, so every G1 load fails the independent gate

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 111 section 1: option A. The standard G1 and G7 tables are carried, the values two independent transcriptions agree on, and G1 now passes the gate under the tolerances as first committed.

### 1. What the comparison found

The tolerance was committed before either reference existed (`docs/BALLISTICS-VALIDATION.md` section 2).

**Against py-ballisticcalc 2.3.1:**
- **All four G7 cases pass at every range to 1000 yd.** The largest share of any allowance used is 4 percent for drop and wind and 7 percent for time of flight; the .308 case reads 37.51 MOA against 37.49.
- **Both G1 cases fail from 100 yd on.** At 1000 yd:

| Case | Drop, port | Drop, reference | Time of flight, port against reference |
|---|---|---|---|
| .308 168 gr, G1 0.462 | 31.77 MOA | 40.33 MOA | 13.2 percent short |
| 6mm 105 gr, G1 0.536 | 22.70 MOA | 28.47 MOA | 13.1 percent short |

**The cause is the table, not the solver.** Comparing the two G1 tables point by point, 60 of their 79 shared Mach points differ:

| Mach | ballistics.js | py-ballisticcalc |
|---|---|---|
| 0.80 and below | the same | the same |
| 1.0 | 0.5210 | 0.4805 |
| 1.4 | 0.5295 | 0.6625, the peak |
| 2.0 | 0.4139 | 0.5934 |
| 3.0 | 0.3274 | 0.5133 |
| 5.0 | 0.2571 | 0.4988 |

The JavaScript's supersonic G1 is a different curve, about 8 percent high near Mach 1 and 30 to 50 percent low above Mach 2. py-ballisticcalc's values are the standard G1 function as it is generally published, 0.4805 at Mach 1.0 and a peak of about 0.66 near Mach 1.4. So a G1 load on the website flies with too little drag, and its drop at 1000 yd reads about 8.6 MOA short for the .308 case.

**The G7 table matches everywhere a rifle bullet flies.** It differs from py-ballisticcalc only at Mach 3.5 and above, 0.192 against 0.1935 at Mach 4.0 and 0.1523 against 0.1618 at Mach 5.0, above any case here.

### 2. The options

- **A. Carry the standard G1 table**, the BRL function as published, from a source you name, such as McCoy's *Modern Exterior Ballistics* or the BRL report itself. The transcription check then lists the table as an intentional difference, and the gate is run again. A few lines and a fixture, once the source is chosen. **What I would choose,** with the source checked against the publication rather than copied from py-ballisticcalc, whose values are its own transcription.
- **B. Keep the JavaScript's table** and state that GroupLab's G1 is not the standard G1. I see no case for this: every published G1 coefficient is referenced to the standard function.

**Either way, the website's G1 results are affected now,** independently of GroupLab.

---

## 2026-09-19, question 24: ballistics.js's Coriolis vertical term has its sign reversed, and a smaller wind-direction fault beside it

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 111 section 2: option A. The Coriolis vertical term is ported with its sign corrected. The signed crosswind and the reading of the BC reference atmosphere are accepted, and aerodynamic jump stays out until the fit is checked against its page.

### 1. The conflict

Entry 110 section 2a lists "the Coriolis horizontal and vertical (Eötvös) terms" under **port as it stands**. The vertical term cannot be ported as it stands, because its sign is reversed.

`reference/ballistics-js/ballistics.js`, `coriolisVertical`:

> Firing east increases apparent gravity (bullet drops more), firing west decreases it.

It returns `-0.5 × 2Ω V cos(lat) sin(az) × t²`, which is downward for fire toward the east.

**The physics says the opposite.** In local east, north and up coordinates, the Earth's rotation is Ω(0, cos φ, sin φ). For a bullet moving east at speed V, v = (V, 0, 0). The Coriolis acceleration is −2Ω × v = (0, −2ΩV sin φ, +2ΩV cos φ).
- **Its vertical component is +2ΩV cos φ, upward.** This is the Eötvös effect: a body moving east is lighter, so fire toward the east strikes high and fire toward the west strikes low.
- **The size of the term is right:** half of 2ΩV cos φ times t², with V the average speed, is Ω X t cos φ. The fault is only the sign.
- **The horizontal term is right:** −2ΩV sin φ toward the south is to the right of a shooter facing east, in the northern hemisphere, which is what the file says.

At 45 degrees north, fire due east, 1000 yd and 1.6 s, the term is Ω × 3000 ft × 1.6 s × cos 45°, about 3.0 in. So the file puts the impact about 5.9 in from where the physics does, about 0.6 MOA.

### 2. The options

- **A. Port it with the sign corrected**, and a test that fire toward the east strikes high. About fifteen lines. This is what I would choose: the term is small, and its sign is not in doubt.
- **B. Leave it out** and say so wherever the solver's output is shown, as for aerodynamic jump.

**Meanwhile it is left out.** `grouplab trajectory` says "The Coriolis vertical (Eötvös) term is not modelled."

### 3. Beside it, not blocking

- **`solveExtended`'s wind direction.** It takes the crosswind as `speed × sin(windDir − azimuth)`, from a wind's from-direction, and adds it to the horizontal total with the sign that means right. So a wind from the east, fired north, drifts the bullet right, when it blows it left. Spin drift and Coriolis both use positive for right, so the total mixes conventions. The port does not carry the mapping over: it takes a signed crosswind, positive from the left, drifting the bullet right. The lag rule itself is ported.
- **Aerodynamic jump.** Entry 110 section 2c allows either Litz's published fit, checked against the book, or nothing. I have no copy of *Applied Ballistics for Long Range Shooting* to check the coefficients against, so it is left out, and the output says "Aerodynamic jump is not modelled." If you have the book, the fit, its units and its sign convention from the page would let it go in.
- **The BC's reference atmosphere, section 2e: how I read it.** The drag constant carries ICAO density. A BC stated against Army Standard Metro is flown with the density ratio taken against Army Standard Metro density: the density of 59 °F, 29.5275 inHg and 78 percent humidity, by the same formula. At the same air that is 1.8 percent more drag than the same number read as ICAO, the "percent or two" section 2e names. A test holds the ratio of the two densities at 1.018. If you meant a different convention, say which.

---

## 2026-09-19, question 23: entry 109, three statements the code does not bear out, and two places where the entry's own limits meet

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 110 section 1: yes to all of it. What was done at each point stands, the stringing power sentence stays whole, the group's inputs stay in the panel, and the lead figure is the one named exception.

### 1. Statements the code does not bear out

- **Section 3e, the crumb.** "The breadcrumb's middle crumb says 'the sheet'. Use the sheet's file name, as the editor's breadcrumb does." It already did: `ShowAnalysis` set the crumb to the image's file name whenever the marking has one, and fell back to "the sheet" only without one. The render entry 109 was read from came from a test that applied a detection without ever opening a file. **Done:** the fallback is now the sheet's own name from its definition, and the new renders open a file, so they show what a person sees.
- **Section 3b, the framing.** "Frame the group, not the bull, as entry 103 section 1 asked." The plot already framed the shots with their calibre outlines and never the bull. What made it look bull-sized was a margin of 35 percent of the group's extent on each side, which with .308 outlines on a 25-shot group came to about the bull's own size. **Done:** the margin is 10 percent, and the rings now run off the frame.
- **Section 2a, the tool strip.** "Entry 93 marks the icon tool strip as done, but the render shows text labels." The labels were entry 93's own choice, recorded at the strip: "The name stays beside the icon, because an icon alone is a guess for anyone who has not used the application before." So the strip matched its decision rather than falling short of it. **Done as entry 109 asks:** icons alone, each named with its key in a tooltip. The reason entry 93 gave is now answered by the tooltip, not by the strip.

### 2. Where the entry's own limits meet

- **The stringing power statement "stays visible ... Keep it to one line."** The statement is `ShapeTests.StringingPowerSentence`, one sentence of two or three clauses, for example "From 24 shots this test catches stringing of 2 times or more at least 8 times in 10, and often misses less: 1.5 times needs about 50 shots." At the 372 pixel column it wraps to three or four lines. Shortening it would reword the finding STATISTICS.md section 7 requires beside the result, which the covering message rules out. **Done:** it is kept whole, in view, as the card's one stringing line, and it wraps.
- **The panel "is then the review queue, the selected detection and the scale."** The panel also holds the group's inputs, the calibre, the shot distance, the rifle, barrel and load and the rounds fired, and the shot list. They are the task, not settings, and the entry names nowhere else for them. **Done:** they stay, below the scale, each section divided by a rule.

### 3. A size outside the five

Principle 2 says five styles and no more, and section 3a says mean radius keeps its lead size. The lead size is a sixth. **Done:** the five are tokens, the lead figure is the one named exception, and a test holds every text on the marking, analysis and settings screens to those six sizes.

---

## 2026-09-19, question 22: entry 107 section 1 asks for "9mm" refused, but its own rule reads it as a diameter; and the pick list is 37 diameters, not 36

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 108: option B, extended to inches. Calibre designations are refused in both units, the real diameters beside them still read, and the count is 37.

### 1. The conflict

Entry 107 section 1 gives the rule:

> **Millimetres, only when marked:** "7.82 mm" or "7.82mm" reads as 7.82 mm.

and, in its tests:

> names are refused with the sentence, including "300 Blackout", "6.5 Creedmoor", "30 Cal." and "9mm";

**"9mm" is a number marked mm, so the rule reads it, and the test says refuse it.** No syntax tells a metric calibre name from a diameter in millimetres: "9mm" and "9.02mm" have the same form. And the trap the section removes for bare numbers comes back for marked ones:

| Typed | Read as | The bullet |
|---|---|---|
| 9mm | 0.354 in | .355 in (9.02 mm), 0.03 mm off |
| 7.62mm | 0.300 in | .308 or .310 in, 0.2 mm off |
| 6.5mm | 0.256 in | .264 in, 0.2 mm off |
| 5.56mm | 0.219 in | .224 in, 0.13 mm off |

### 2. The options

- **A. Keep the rule as written.** "9mm" reads 0.354 in, near enough; "7.62mm" and "6.5mm" read 0.2 mm small. Costs nothing, and leaves the name trap open for anyone who adds mm to a metric name.
- **B. Refuse a millimetre value that is a metric calibre designation**, such as 5.45, 5.56, 5.7, 6, 6.5, 6.8, 7, 7.5, 7.62, 7.65, 8, 9, 9.3, 10 and 12.7, with the same refusal sentence. About twenty lines and a test. It is a short list of numbers, not names, but it is a list that can fall behind, which is what section 1 set out to remove.
- **C. Require hundredths in millimetres**, since bullet diameters in millimetres are quoted as 7.82, 9.02, 6.71. It refuses "9mm" and "6.5mm" but not "7.62mm" or "5.56mm", so it does not close the trap on its own.

**What I would choose: B**, because 7.62 and 6.5 are the numbers most likely to be typed with mm after them, and a refusal costs the person one retype while a wrong diameter costs a wrong edge-to-edge figure without any sign.

### 3. The count

The section says the pick list holds "the distinct diameters in Alan's two lists, 36 of them, from .172 to .510". Counting the distinct diameters in entry 105 section 7's two lists gives **37**, from .172 to .510 with .223 excluded. All 37 are in the pick list, and `docs/CALIBRES.md` lists them. If one should not be there, name it.

---

## 2026-09-19, question 21: printing from inside GroupLab, scoped, with the plan, its cost and three decisions it needs

**Status: answered 2026-09-19** by `docs/NOTES-FROM-PLANNING.md` entry 107 section 2: build it. The margin refusal covers inked items and not the quiet zone, Linux and macOS keep the viewer path, and Print is the primary beside Open to print on Windows.

### 1. What entry 106 section 5 asks

> Scope it first. If the plan is clear, has no open design decision and fits one run with its tests, build it. Otherwise raise it as a question with the plan and its cost.

**It is raised, for three reasons.** It does not fit one run beside the rest of entry 106. Three design decisions in section 4 below are open. And the test the entry calls the one that matters depends on a printer the CI runner may not have, which I cannot check from here.

### 2. The plan

- **Windows, through Win32 by P/Invoke:** `PrintDlgEx` for the real dialog (printer, copies, pages), then GDI (`StartDoc`, `StartPage`, `EndPage`, `EndDoc`) to draw.
  - **Against `System.Drawing.Printing`:** it needs the `System.Drawing.Common` package, a new dependency, and it is Windows-only anyway, so it buys nothing over the Win32 calls it wraps.
  - **Against the WinRT print manager:** it needs a print document source built for its preview model, which is more machinery for the same result.
- **Drawn as vector from the scene, never a rasterised PDF.** The renderer's scene is in half-dmm. GDI can be set to a mapping mode with those units, so one unit of the definition is one unit on the paper. The scene's items are:
  - **Bull bands:** GDI paths from two ellipses, even-odd filled.
  - **Rectangles:** markers, codes and rules, each a GDI rectangle.
  - **Text:** set in Arial, the metric match of the Helvetica the PDF names.
- **True page coordinates.** `GetDeviceCaps` gives the printer's physical offsets, `PHYSICALOFFSETX` and `PHYSICALOFFSETY`, and every item is shifted by them, because GDI's origin is the printable area, not the paper's edge.
- **Refusals.**
  - **The paper:** a paper size in the dialog that is not the sheet's page, such as a Letter sheet on A4, is refused with both sizes named. Nothing is ever scaled and no scaling is offered.
  - **The margin:** any marker, code or bull that falls in the printer's unprintable margin is refused with the item and the margin named.
- **The confirmation, after `EndDoc` returns:** the printer, the sheet, the page count and "at actual size". It says the job was sent to the print queue, never that the paper came out.
- **Linux and macOS:** the viewer path stays, because a CUPS path needs its own dialog and its own margin query. That is the smaller half of the value and worth doing only once Windows proves the approach.

### 3. The cost

- **About one full run.** Roughly 300 lines of P/Invoke declarations and dialog handling, 200 of scene drawing and margin checks, the confirmation, and the tests.
- **The test the entry names:** print a sheet to a PDF printer and check that the markers in the output sit at their definition coordinates within 0.1 mm. It would print to "Microsoft Print to PDF" with an output file named in the job, bypassing the dialog, then rasterise the output with the test project's existing PDF rasteriser and run the registration on it.
- **The risk in that test:** whether `windows-latest` has "Microsoft Print to PDF" installed. It is an optional Windows feature on the server editions. If it is absent, the test skips on CI and the same check is run once by hand on Alan's machine: print the sheet to "Microsoft Print to PDF", then `grouplab measure` the saved file.

### 4. The three decisions it needs

1. **The margin rule's reach.** Refuse when a marker's quiet zone falls in the unprintable margin, or only when the marker itself does? The quiet zone is part of what makes a marker decode, so I would refuse on the quiet zone too.
2. **Linux and macOS.** Keep the viewer path there, as above, or build CUPS printing in the same work?
3. **Whether it replaces "Open to print" or sits beside it.** I would put Print beside it on Windows and keep Open to print everywhere, since a person with a colour-managed viewer workflow may still prefer it.

**What I would choose:** refuse on the quiet zone, keep the viewer path on Linux and macOS for now, and put Print beside Open to print on Windows. Given those answers, it is one run.

---

## 2026-09-19, question 20: the mark's light amber and its grey are not the tokens entry 105 section 4 says they are

**Status: answered** by `docs/NOTES-FROM-PLANNING.md` entry 106 section 2: option B. The palettes gain the mark's brand roles at the files' values, the mark is drawn from them, and `ThemeTests` holds the files and the roles to each other and the roles to 3:1.

### 1. What entry 105 section 4 says

> **The light variant** is the same geometry with the rings at `#8f8b83` and the holes at `#a9660f`, the light theme's amber.

> **The colours are tokens**, not literals: the grey is the rail and ring grey and the amber is `Tokens.Amber` in each theme, so the in-app mark follows the theme.

### 2. What the tokens are

In `src/GroupLab.App/Theme/Tokens.cs`:
- **Dark `Amber` is `#e0912f`.** It matches the dark mark.
- **Light `Amber` is `#965d12`, not `#a9660f`.** It was set for text contrast on the light panel, and `ThemeTests` holds it to that.
- **No token is `#6b727b`, `#8f8b83`, `#8a9199` or `#6f6b64`.** The nearest grey, `MarkFaint`, is `#697079`. The rail's icons are drawn in the text colours, not in a grey of their own.

So "the amber is `Tokens.Amber` in each theme" and "the light variant's holes are `#a9660f`" cannot both hold. The same goes for "the grey is the rail and ring grey" and a ring grey that no token has.

### 3. What I did meanwhile

The in-app mark and lockup are drawn from the committed SVG for the theme showing: the dark file in dark and high contrast, the light file in light. Each is drawn with the colours in the file, exactly as chosen. You named the files the source of truth, and redrawing the mark in colours nobody chose would be a guess. The icons use the dark file, as the entry says.

### 4. The options

- **A. The mark keeps its colours, as a fixed brand asset.** The files stay the source; the mark does not follow the theme beyond choosing its file. Nothing else changes.
- **B. The tokens gain the mark's colours as brand roles**, a ring grey, a word grey and a mark amber in each palette, set to the files' values. The mark is drawn from the tokens, and a test holds the files and the tokens to each other.
- **C. The mark takes the existing tokens.** Light amber becomes `#965d12` in the mark, and a grey is chosen from the palette. That changes a mark Alan chose.

**I would choose B.** It keeps what Alan chose, and it makes "the colours are tokens" true, so the mark follows a future palette change instead of silently diverging from it.

---

## 2026-09-18, question 19: entry 103 section 3 asks for a sort that entry 52 already committed

**Status: answered** by `docs/NOTES-FROM-PLANNING.md` entry 104 section 1: confirmed, nothing to commit and nothing to regenerate. The one surviving item, the option C risk, is in `DESIGN.md` section 22, and `QuestionStatusTests` now fails when a question marked open is one a notes heading says was answered.

### 1. What entry 103 section 3 asks

> `docs/QUESTIONS-FOR-PLANNING.md` question 15 asks whether to commit `MarkerDetection.InIdentifierOrder()` when doing so changes every printed table on Windows and no gate verdict. **Option A. Commit the sort and regenerate everything it touches.**

It also asks that `docs/PHASE0-RESULTS.md` section 4.5 be amended "with dated before and after figures", because "`ultrawide3.jpg` goes from 21 to 18".

### 2. What the repository shows

- **The sort is committed**, by the commit titled "Entry 52: sort the markers before use, and regenerate every record and quoted figure with them". `SheetMeasurer.DetectFiducials` takes both detection passes through `InIdentifierOrder()` (`src/GroupLab.Core/Measurement/SheetMeasurer.cs`, lines 183 and 194).
- **Question 15 was answered by entry 52**, whose heading is "question 15 answered, and the thing underneath it", with its status "actioned 2026-09-15" and its first bullet "the sort is committed with every record regenerated, and no verdict changed". Question 15's own status line was never changed from open, which is what made it look unanswered.
- **Section 4.5 already carries the before and after figures.** It reads: "Before entry 52 sorted the markers they read 0.015 to 0.091 in, 8 to 21 and 0.014 to 0.060 in. The frames, the markers and the corners are the same; only their order changed". Since entry 101 the figures to beat are 0.018 to 0.113 in and 8 to 22 of 25.
- **`docs/PHASE1-RESULTS.md` "Entry 52"** has the before and after table for every frame whose figures moved, headed by the sentence entry 52 section 2 asked for.

So there is no sort to commit and no regeneration it would cause. Running the sweep would produce the records entry 101 already committed.

### 3. What is still open in section 3

**The `DESIGN.md` section 22 risk for option C.** Section 22 has no entry for RANSAC's consensus. Entry 52 section 3 and the stability measurement ("Entry 52 sections 3 and 4") established the fragility, and entry 101 left RANSAC's inlier choice native. Adding the risk is documents only and needs no regeneration.

### 4. What I would choose

- **Question 15:** marked answered, pointing at entry 52 as well as entry 103. Done in this commit, because leaving it open is how section 3 came to be written.
- **Section 3's sort and sweep:** nothing to do. Confirm, and I will close this question.
- **The section 22 risk for option C:** add it in the next run, alone, as section 3 intended. It is held here only because section 3 as a whole rests on a premise that turned out to be stale, and the instruction was to stop and ask rather than act on part of it.

---

## 2026-09-18, question 18: assisted hole placement on a target with no definition, which the detector as built cannot do

**Status: answered** by `docs/NOTES-FROM-PLANNING.md` entry 103 section 4. "Assisted" means the snap, which is built; blank-paper detection is Phase 4 with a gate naming the material it needs; a user-traced definition on a bought target is the visual designer's second promise; option C is refused until somebody asks, and then as research with its own gate; option D is refused.

### 1. The promise, and why it has no phase

`DESIGN.md` section 3, In scope:

> A secondary mode that analyses any target, including store-bought targets and blank paper, using a user-defined scale and **manual or assisted hole placement**

Entry 90 section 5:

> Assisted hole placement on a store-bought target has no definition to render and difference against, so the detector as built cannot do it. **Whether that bullet is achievable at all is a design question**, and it should be answered before it is either scheduled or quietly dropped.

**That is correct about the detector.** `RenderDifferenceHoleDetector` renders the definition through the fitted registration and differences the image against it. With no definition there is no render, no artwork mask, no exclusion zones and no bull-cell position prior. Nothing in it degrades gracefully; it has no input.

**Everything else in the bullet is built.** The scale is set by a reference length or rectangle, marking by hand works on any photograph, and the whole editor, the queue and the statistics run off hand-marked shots.

### 2. What is already built and what it means for the word "assisted"

**Two of the three things "assisted" could mean already exist, and they exist without a definition.**

| What | Built | Needs a definition |
|---|---|---|
| **A tap snapped to the hole under it.** `Snapping.ToHole` takes the artwork as an optional argument. With no artwork it snaps to the dark centroid within the radius | **yes** | no |
| **A snap radius from the calibre**, so the snap is the size of a hole rather than an arbitrary distance | **yes** | no |
| **Finding holes unprompted**, with no tap to start from | yes, `RenderDifferenceHoleDetector` | **yes, absolutely** |

So the question is not whether assistance is possible. **It is whether "assisted placement" means the second column or the third**, and the document does not say.

### 3. Three cases, and they are not equally hard

**Blank paper is not the hard case and it may be nearly free.** A sheet with nothing printed on it has a predictable appearance: paper. Render-and-difference degenerates to finding dark blobs on a light field, which is what the difference stage does once the render is a constant. The size filters work, because the user has given a scale; the shape and solidity filters work unchanged. What is lost is the artwork mask, the exclusion zones and the bull-cell prior, and on blank paper there is nothing for them to do.

**A store-bought printed target is the hard case, and it is the one the bullet names first.** Rings, numbers, a logo and a scoring legend are dark, connected and shaped like nothing in particular. They are exactly what the artwork mask exists to remove, and there is no mask. The clean-photograph residue figures give the scale of the problem: on sheets where the artwork *is* modelled, 35 spurious detections survive on the clean frames. Unmodelled artwork would not add a few; each ring edge is a candidate the pipeline has no reason to refuse.

**A definition the user creates for a store-bought target is the third case, and it loops straight back into the deferred designer.** If the user can trace the rings of a bought target once, the sheet has a definition, the full detector applies, and this bullet becomes a use of the visual designer rather than a separate mode. That is the only route that gets the full detector onto bought targets, and it cannot be scheduled while the designer is deferred.

### 4. The options, with their real costs

| | Option | Cost | What the user gets |
|---|---|---|---|
| **A** | **Define "assisted" as the snap that exists**, and say so in section 3: a tap lands on the hole under it, at a radius set by the calibre. No unprompted detection without a definition | **none, it is built.** One sentence in section 3 and one feature line with a state | Every hole still needs a tap. On a 25-shot sheet that is 25 taps, which is the current secondary mode |
| **B** | **Definition-free detection on blank paper only.** The difference stage against a constant paper model, the existing size, shape and solidity filters, and every candidate into the review queue | **moderate.** A second entry point into the pipeline and a scale-only registration path. Measurable on donated material only if anybody shoots blank paper, and nobody in the corpus has | Unprompted detection where the sheet has no artwork, which is a real workflow: plain paper at a known scale is what people use for a quick ladder |
| **C** | **Definition-free detection on printed bought targets**, by learning the artwork from the image: fitting concentric rings, or exploiting the symmetry a bought target has | **high, and it is research.** No corpus material at all, and the failure mode is false positives on artwork, which is the one thing the Phase 1 gate forbids outright (G2: zero false positives from target artwork) | The bullet as literally written |
| **D** | **Drop "assisted" from the bullet** and promise manual placement in that mode | **none** | Honest, and it removes a capability the design has promised since revision 1 |

### 5. What I would choose, and why

**A now, B named in Phase 4 or Phase 5, C refused until somebody asks for it, and never D.**

- **A is already true and unsaid**, which is the worst state for a promise to be in. The snap is assistance, it works with no definition, and one sentence makes the bullet accurate today.
- **B is worth having and is small**, but it should be scheduled against a real photograph of a real sheet of paper with real holes in it, and no such image exists in the corpus. Naming it in a phase and shooting one sheet of plain paper at the next range session costs almost nothing and turns it into an ordinary measured feature.
- **C would put unmodelled artwork into the detector**, and the Phase 1 gate's G2 exists precisely to prevent that. It is also the case the user can solve for themselves under option three of section 3, by tracing the target once in the designer, which is a better answer than a ring-fitting heuristic.
- **D is a last resort** because nothing about the promise turned out to be wrong. The word was simply never defined.

### 6. What the answer needs to settle

1. **Which meaning of "assisted" section 3 intends**, so the bullet can be made accurate rather than hopeful.
2. **Whether blank-paper detection is worth a phase**, and if so which one. It is the only one of the three that is both useful and cheap.
3. **Whether a user-traced definition for a bought target is the intended route to the full detector on bought targets.** If it is, the bullet and the deferred designer are one item and should say so, and the designer's specification should carry it.

Until the answer comes back, the bullet is recorded as deferred in `DESIGN.md` section 3 and in the README's Planned section, both pointing at this question.

---

## 2026-09-17, question 17: the real holes veto the shipped split threshold, and only a higher one survives

**Status: answered 2026-09-17**, by NOTES-FROM-PLANNING.md entry 81. 1.80 goes in everywhere, not only with a calibre, and the gap it rests on is measured with pairs made from real holes.

### 1. What entry 80 section 2 asked

> Synthetic material is for coverage, because 848 holes explore the parameter space in a way 99 cannot. **Real holes get the veto.** A threshold that the synthetic sweep prefers but that misclassifies a single real hole is the wrong threshold.

### 2. What the sweep found

**The synthetic holes were rescaled first.** Unscaled, the current detector reads them at 0.335 in, which is 1.088 of .308. The survey they were fitted to mixed .264, .308 and .338. Scaled by 0.871, they read 0.291 in, 0.944 of .308, matching real scans. The run is `grouplab holes split-calibration --local <manifest>`.

**Blobs misclassified, split elongation against size veto.**
- **Synthetic** is the six scans punched with single holes: a single hole split in two.
- **Real** is Alan's scan, the friend's scan and the friend's photograph, which has no camera data: a single hole split in two.
- **Real photos** is the six mounted frames.

| Elongation | Size veto | Synthetic singles split | Real split | Real photos split | Real spurious |
|---|---|---|---|---|---|
| 1.45 (shipped) | none | 16 | 4 | 4 | 18 |
| 1.45 | under 1.2 to 1.8 holes | 0 to 3 | **2** | 0 | 9 |
| 1.60 | under 1.2 to 1.8 holes | 0 to 2 | **1** | 0 | 9 |
| **1.80** | **under 1.2 to 1.8 holes** | **0** | **0** | **0** | **9** |
| 1.80 | none | 3 | 0 | 0 | 17 |

**The two real blobs still split at 1.45 are each one hole joined to printed or photographed ink.**
- Alan's sighter hole beside the print note.
- A hole beside bull 10 in the friend's photograph.

Both hold more than two holes' area, so the size veto cannot see them. Only a higher elongation threshold leaves them whole.

**The synthetic pairs say what 1.80 costs.** Of about 840 merged overlapping pairs, 5 split at 1.45 with no veto and none at 1.80. The corpus holds no real merged pair, so the cost on real neighbours is not measured.

### 3. The options

1. **Elongation 1.80 with the size veto at 1.5 holes.** It misclassifies no real hole, and it halves real spurious detections, 18 to 9. **Cost:** it changes the default without a calibre as well, so the committed synthetic records move, and a real pair of neighbours whose shape alone separated them would stay one mark.
2. **Keep 1.45, and use 1.80 only when a calibre is named.** Nothing changes without a calibre. **Cost:** the two real holes stay split whenever no calibre is named.
3. **Keep 1.45 everywhere** until a sheet with real merged pairs exists. **Cost:** two real holes in nine images stay split.

**What I would choose: option 2 now, and option 1 once a real merged pair has been measured.** It is the one change that no real hole argues against and that moves nothing already recorded.

### 4. The photograph residue, entry 78 section 2, which depends on the answer

**On photographs every spurious detection is a split half:** 64 on clean sheets and 19 on real ones.

| | Elongation, median (range) | Solidity, median |
|---|---|---|
| Spurious blobs | 4.6 (1.54 to 7.4) | 0.65 |
| True holes | 1.17 (at most 1.67) | 0.95 |

**Why a plain cap is not enough.**
- **How elongated a real pair can be.** Two equal round holes that touch have an elongation of at most about 2.24.
- **What a cap would remove.** A cap there would remove 70 of the 83 spurious halves and no true hole.
- **Why it is not safe alone.** The difference stage's closing can join two real holes up to about 0.11 in apart, whose elongation can reach about 2.6. A plain cap could therefore silently drop two real holes.

**The proposed fix combines both conditions.** A blob the size veto calls too small for two holes, and more elongated than any single hole measured, is refused as residue rather than kept as one hole. Without a calibre, the size would come from the sheet's own round holes. It is not built, because it sits on the threshold chosen above.

---

## 2026-09-17, question 16: the printed name cannot go on the caption line without hiding holes on sheets already printed

**Status: answered 2026-09-17**, by NOTES-FROM-PLANNING.md entry 77 section 5. The name goes outside the analysed region, so it needs no exclusion box; a sheet with no such place carries no name. No box is added over the print note.

### 1. What entry 76 section 4 asked

> **The printed name: name first, identifier second, same caption line.** Something of the form `GroupLab 5x5 Load Development, A4 · GL-20J3-Y141-0BN3-EYME`.

> Establish whether the caption is excluded from the difference region before changing it; if it is not, that exclusion is part of this change rather than a follow-up.

### 2. What the check found

**The caption is inside the analysed region, and it is excluded only by a box sized to its own text.**
- **Why a box is needed at all.** `RenderDifferenceHoleDetector` builds the expected artwork with `SceneRasterizer`, which draws no text. Every printed word would read as a difference unless a zone covers it.
- **How the caption's box is sized.** The identifier's box is estimated from the text's length, at 0.6 times the font size per character, 25 dmm tall, plus 10 dmm all round. Its baseline is 80 dmm above the bottom edge.

**The change was built and measured.**
- **What was built.** The caption became name, middle dot, identifier. The box was widened to cover both the new caption and the identifier alone, which is what every sheet printed so far carries. It was measured with the exact Helvetica widths.
- **What happened on Alan's scan**, a sheet printed before the change: the detector found **13 holes instead of 15**. The wider box covers blank paper beside the old caption, and a real sighter hole sits there, at page (3.070, 10.573) in.
- **What was done.** The change was reverted in full, and the scan reads 15 again.

**So any caption wider than the one already printed hides holes on every sheet already printed**, because the analyser has no way to know which caption a given sheet carries.

### 3. The options

1. **Put the name on a line of its own, outside every region a hole can reach**, for example in the top margin beside a code. **Cost:** a new exclusion zone there, which on old sheets covers paper nobody shoots near. It also departs from "same caption line".
2. **Keep the name on the caption line, and exclude it only where ink is actually present:** threshold the band against the paper, and exclude only the glyph blobs that are found. **Cost:** a new mechanism in the detector, and a hole that touches a glyph could be excluded with it.
3. **Keep the name on the caption line, and accept that sheets printed before the change lose holes in the widened band.** **Cost:** a known silent miss on real sheets. I would not choose this.
4. **Encode which caption a sheet carries**, so the analyser excludes the right box. **Cost:** a GLTD change, and every sheet printed so far has no such field, so it only helps sheets printed afterwards. Old sheets would need the current box as their default.

**What I would choose: option 1.** It keeps every printed sheet reading as it does today, and it needs no new detector mechanism. The name is for the person holding the paper, and a line of its own is at least as easy to read.

### 4. A related exclusion, for the same decision

**The scan's detection at page (2.909, 10.784) in is not a hole.** It sits on the printed sentence "Print at actual size, 100 percent", whose baseline is 45 dmm above the bottom edge. That sentence is in no exclusion zone, which also answers entry 73 section 4.

- **What would fix it.** A box around that sentence alone, covering its glyph band plus 10 dmm. It would remove the false positive without reaching the sighter hole above it.
- **Why it waits.** It changes what the detector reads on every printed sheet, which is the same kind of change as section 3. It belongs with that decision.
- **What I would choose:** add the box, sized exactly to the sentence as printed today, which has not changed.

---

## 2026-09-15, question 15: sorting the markers changes the Phase 0 record on Windows, not only on macOS

**Status: answered**, option A, by `docs/NOTES-FROM-PLANNING.md` entry 103 section 3, and first by entry 52, which committed the sort and regenerated every record with it. This status line was left open when entry 52 was actioned, which is why entry 103 answered it again; question 19 records that the sort was already in.

### 1. What entry 49 section 2 asked

> Sort the markers by identifier before use. Unconditionally, and not to make a gate green.

### 2. What happened

**Implemented, measured, not committed.**
- **The change:** `MarkerDetection.InIdentifierOrder()` orders markers and rejections by identifier, then by their first corner. `SheetMeasurer` and `PageRegistration` take the detection through it.
- **Where it is:** the patch is kept outside the repository, and the tree is as committed.
- **The measurement:** the eight Phase 0 spike commands were rerun on Windows, and their printed tables compared with the ones committed in `scans/phase0/measurements/tables`.

**Every printed table changes on Windows, and no gate verdict does.**

| Table | Lines that change | Gate verdicts that change |
|---|---|---|
| `sheets` | 10 | none: the paper gate column is identical on every sheet |
| `photos` | 9 | none: the flat gate frames change in no count, and the mounted frames still fail |
| `markers` | 53 | not a gate |
| `refinement` | 21 | not a gate |
| `threshold` | 8 | none: every row still has 10 of 10 sheets inside the paper gate |
| `scale` | 1 | none |
| `field` | 4 | not a gate |
| `detectors` | 1 | not a gate |

**On the scans the figures move in the last places.** `gl-cf25-ltr-1-600-dpi.png`:
- **Residual maximum:** 0.00504 against 0.00507 in.
- **Worst bull by edge fit:** 0.00254 against 0.00251 in.

**On the mounted photographs the counts move:**

| Frame | Corners kept | Scoring bulls over the gate |
|---|---|---|
| `ultrawide1.jpg` | 66 against 68 of 136 | 13 against 14 of 25 |
| `ultrawide2.jpg` | 54 against 46 of 136 | 20 against 19 of 25 |
| `ultrawide3.jpg` | 42 against 51 of 128 | 21 against 18 of 25 |
| `main1.jpg` | 90 against 94 of 136 | unchanged |
| `main2.jpg` | 25 against 32 of 104 | 21 against 22 of 25 |

`ultrawide1.jpg`'s worst scoring bull goes from 0.03301 to 0.03902 in.

**Why.**
- **The mechanism:** `Cv2.FindHomography` with RANSAC draws its samples by position in the correspondence list.
- **On a flat scan:** the consensus is the same whatever the order, and only the refinement's last digits move.
- **On a curved sheet:** many corners sit near the RANSAC threshold, so a different sample path settles on a different consensus set, 42 corners against 51 on the same frame.
- **What that means:** the mounted figures are one arbitrary ordering of the detector's output. That is the order dependence entry 49 section 2 calls a defect, and it is larger than the platform comparison suggested.

### 3. Why this is a decision rather than a fix

**The mounted figures are a benchmark.** `docs/PHASE0-RESULTS.md` section 4.5 states them as "the figures to beat: worst scoring bull 0.015 to 0.091 in, 8 to 21 of 25 scoring bulls over the gate", and the Phase 1 surface work reports against them.

**Committing the sort changes committed evidence.**
- **Certainly regenerated:** the eight gate records and their tables.
- **Probably regenerated, not measured:** the other committed records computed through registration, such as the `surface-*.json`, `mounted-pair.json` and hole detection records.
- **Also re-derived:** every document figure that quotes those records.

**Not committing it** leaves a known order dependence in the pipeline.

### 4. The options

- **A. Commit the sort and regenerate everything it touches.**
  - **What it involves:** every committed record computed through registration, and every document figure quoting them, amended with dated before and after figures and the verdicts shown unchanged.
  - **Cost:** about an afternoon.
  - **Consequence:** the mounted baseline moves; for example `ultrawide3.jpg` goes from 21 to 18 scoring bulls over the gate.
- **B. Commit the sort and regenerate only the gate record.**
  - **What it involves:** the other records are marked as measured in the detector's order until their next rerun.
  - **Cost:** smaller.
  - **Consequence:** the committed records then hold two orderings, the kind of unmarked disagreement entry 49 section 1 warned against in another form.
- **C. Remove the dependence at its source.**
  - **What it involves:** replace RANSAC's random consensus in registration with a deterministic robust fit, such as an iteratively reweighted homography over all corners.
  - **Cost:** larger, since it changes the figures as well.
  - **Consequence:** it answers the fragility in section 2 rather than fixing one ordering of it.
- **D. Do not sort now.** Record the order dependence as a known defect beside entry 49 section 5's edge fit finding, and decide after the range session.

### 5. What I would choose, and why

**A, and C as the question behind it.**
- **Why A:**
  - A record that follows one stated order is worth more than figures that were one arbitrary order.
  - No verdict changes.
  - The cost is an afternoon now, against more later, when the range session's frames have been measured under the old order too.
- **Why C matters:** RANSAC's consensus on a curved sheet moving with an input that should not matter is the same class of fragility as the edge fit's one point of thirty. The sort makes it repeatable; it does not make it stable.

---

## 2026-09-15, question 13: where donated images live, and coordinates that are already in the repository's history

**Status: answered 2026-09-15**: section 1 by `docs/NOTES-FROM-PLANNING.md` entry 28 section 4 (option A, a GPL-3.0 data repository), section 3 by entry 28 section 1 (the real `meta.json`), and section 2 by entry 29 (option (a), approved by Alan). Before that it read: blocks committing any image: `scans/mounted/`, held by entry 23 section 5, and every donated submission. Nothing else waits. The intake tool, its tests and the publication test are built and committed, and they work against whatever directory the answer names.

### 1. Entry 22 section 1: where the images live

**What the repository is.**
- **History:** one pack of 157.5 MiB, after one rewrite already.
- **Tracked images:** 78, which with the PDFs come to 163 MB on disk, most of it the Phase 0 scans the gates and tests read.
- **What is arriving:** the mounted set held back is 28 files and 80 MB, and entry 22 estimates half a gigabyte to a gigabyte of donations.

**The options, with what each costs here.**

- **A. A separate data repository, `grouplab-testdata`.**
  - **For it:** the code repository stays near its present size and keeps its history. The data carries its own licence, provenance and lifecycle.
  - **How the code finds it:** a pinned commit and URL recorded in this repository, and a checkout beside it. The publication test already runs against a directory and does nothing when the directory is absent.
  - **Cost:** two repositories to keep in step, and tests that need donated images say they did not run on a machine without the checkout.
- **B. Git LFS here.**
  - **For it:** one repository.
  - **Cost:** every contributor needs LFS. A hosted LFS quota of the usual size is smaller than the corpus entry 22 expects, so it becomes a billing question as well. Adding LFS after images land means rewriting history again.
- **C. A curated subset here, the rest elsewhere.**
  - **For it:** cheapest to set up.
  - **Cost:** a person curates every submission, and the breadth that makes a donated corpus worth having stays outside.

**What I would choose: A,** for the reason entry 22 gives: the two have different lifecycles. `scans/mounted/` would go there too, scrubbed, as the first contents. The Phase 0 and Phase 1 scans stay here, because committed tests and gate records read them by path.

### 2. A finding: coordinates are already committed

`PublicationTests` reads every committed image. **16 of the 78 carry an EXIF GPS block, all of them Phase 0 phone photographs, and 13 of those hold a non-zero latitude and longitude:**
- `scans/phase0/20260913_130543.jpg`, `_130550`, `_130554` and `_130559`;
- `main1` to `main3`;
- `telephoto1` to `telephoto3`;
- `ultrawide1` to `ultrawide3`.

The other three, `main_flat1` to `main_flat3`, carry an empty position. The coordinates were not printed anywhere in this session.

They are Alan's photographs, so there is no consent question, but they are his coordinates in a repository that is going public. Entry 23 section 5 gave the same reason for scrubbing `scans/mounted/`.

**The options.**

- **(a) Rewrite history before the repository goes public.** Replace the 16 files in every commit with scrubbed copies.
  - **What stays the same:** `ImageScrubber` changes no pixel, and a test shows `main1.jpg` decodes identically scrubbed. So every measurement stands.
  - **What changes:** the file bytes, and so any recorded hash of them.
  - **The cost:** a second `git filter-repo` and a force push, which rewrites every clone.
- **(b) Scrub at the tip only.** One ordinary commit. The coordinates stay in history for anyone who looks.
- **(c) Leave them.**

**What I would choose: (a), before the repository is public,** because after that no rewrite takes them back. The rewrite and the force push are not mine to do. They are irreversible and they change what everyone has cloned, so I have not run either.

**Meanwhile the test holds the line.** It names these 16 files and fails if any other committed image carries GPS. It also fails if one of the 16 is scrubbed and left on the list, so the list cannot go stale.

### 3. What the intake tool assumed about the upload page, to confirm or correct

`grouplab intake <submission> <public directory> [--accept <file>]...` implements entry 22 section 2 with entry 27 section 1's triage. It reads these fields of `meta.json`, which no document specifies:
- **Provenance:** `submissionId`, `consentVersion` and `submittedAt`, all required.
- **Answers:** `answers`, copied into the provenance record as they are.
- **Opt-out:** `doNotPublish` or `optOut`, as true, beside the `DO-NOT-PUBLISH` file, which it checks first.
- **Hashes:** `files`, either an object of name to SHA-256 or an array of `{ "name", "sha256" }`.

If the page writes other names, it is a small change. A submission missing any of these is refused with the reason, never published.

**Triage, entry 27 section 1.**
- **The rule:** a file is a candidate when at least four GroupLab markers decode, which is what registration needs.
- **Everything else is held, not published,** with the reason written per file into the provenance record, until a person accepts it by name with `--accept`.
- **Not built:** the rectangular sheet boundary check entry 27 mentions. A commercial target a person judges usable for the manual path is exactly what `--accept` is for.

---

## 2026-09-15, question 14: shotGroups' Fligner-Killeen statistic on the two frames with a point of aim

**Status: answered 2026-09-15**, by `docs/NOTES-FROM-PLANNING.md` entry 28 section 3: the input is `shots.xPOA`. The difference turned out to be the fixture's 15-digit JSON, which drops the last bits of every aimed coordinate and so changes the ties; with the aims recovered and the subtraction redone, all four statistics match to 5.5e-13, `docs/STATISTICS.md` section 15.4 item 15. Before that it read: nothing waits on it. Four keys are pending with this question named, and every other key of the regenerated fixtures is compared.

**Where it stands.** With `shots.xPOA` and `shots.yPOA`, question 11's 2,163 awaiting keys are compared, and all of them pass. The exceptions are `compareGroups.FlignerX.statistic` and `compareGroups.FlignerY.statistic` on `DFinch` and `DFcm`, the two frames with a point of aim. On every frame whose aim is the origin, `GroupComparison.FlignerKilleen` matches shotGroups to 1e-13, `DF300BLKhl` at 0.09095279082138376 against 0.09095279082139468.

**What was tried**, on the aimed coordinates unless stated, with series as the groups, in a probe not committed:

| Variant | `DFinch` X | `DFinch` Y | `DFcm` X | `DFcm` Y |
|---|---|---|---|---|
| **shotGroups** | **10.075167218103388** | **2.804615516292583** | **10.864287796660232** | **2.808136916489293** |
| GroupLab: median by double (a + b) / 2, mid-ranks | 10.073187875409229 | 2.8032500664687734 | 10.860814627148528 | 2.8084659551083533 |
| Average scores for ties instead of mid-ranks | 10.073188286646975 | 2.803252133360352 | 10.860810402714064 | 2.808471773103334 |
| Median averaged in extended precision | 10.07066704956823 | 2.8051598279443146 | 10.865351991682529 | 2.811102334457731 |
| Raw coordinates, `shots.x` and `shots.y` | 10.072777268231373 | 2.801201252282855 | 10.945938713945823 | 2.839215001709699 |
| y negated, for `xyTopLeft` | | 2.8032500664687734 | | 2.8084659551083533 |

- **Nothing reaches 1e-5.** The differences, 1.2e-4 to 4.9e-4 relative, are the size a handful of ties produce, and these frames have them: series of 46 to 92 shots with up to 9 exactly tied absolute deviations.
- **The other compareGroups keys pass on the aimed coordinates,** the MANOVA intercept row included. That row is not shift-invariant, so compareGroups does read the aimed frame.
- **So the input vector is the unknown.**

**The ask: one R run.** In `sg_dump.R`, emit the vector compareGroups hands to its Fligner-Killeen test for each axis, as `compareGroups.FlignerX.input.<i>` and `compareGroups.FlignerY.input.<i>`, beside the statistic. That will say whether the values are centred or rounded differently, or tied differently.

**What I would do with the answer.**
- **If the input differs:** match it, and the four keys are compared like the rest.
- **If the input is the same and the statistic still differs:** it is R's tie handling at the last bit, and it goes to `STATISTICS.md` section 15.4 as a thirteenth known difference.

---

## 2026-09-15, question 12: the fewest shots a group size is quoted for, and entry 24's coverage premise

**Status: answered 2026-09-15**, by `docs/NOTES-FROM-PLANNING.md` entry 28 section 2: option A, five shots. Before that it read: nothing waits on it. The marking panel withholds every dispersion figure below 5 shots in the interim, which is one constant, `GroupAnalysis.MinimumShotsForDispersion` in `src/GroupLab.Core/Marking/GroupAnalysis.cs`.

**What entry 24 section 1 asks.** "Pick the thresholds from `STATISTICS.md` section 9, which already models how well sigma is known from n shots, rather than from anybody's taste. If section 9 does not give a clean answer, raise it as a question rather than choosing a round number."

**What section 9 gives.** Section 9.1's table is continuous in n and sets no threshold. It names one row: "**The five-shot row is the one to put in front of a user.** A five-shot group locates the rifle's true dispersion somewhere between 0.675 and 1.916 times the measured value, a factor of 2.84." Five is taken from that sentence. The same interval at the counts in question, from `SampleSize.SigmaIntervalMultiples`:

| n | True sigma, as multiples of the measured | Factor |
|---|---|---|
| 2 | 0.521 to 6.285 | 12.1 |
| 3 | 0.599 to 2.874 | 4.8 |
| 5 | 0.675 to 1.916 | 2.84 |
| 10 | 0.756 to 1.479 | 1.96 |
| 20 | 0.817 to 1.289 | 1.58 |

**The coverage premise is not the one entry 24 cites.** Entry 24 section 1 cites entry 23 section 2's 79.5 percent at ten shots. That figure is the bootstrap BCa interval for the Grubbs-Patnaik CEP, which the panel does not show. The panel's intervals are closed form or come from the simulated range-statistic table, and their coverage, measured over 40,000 circular normal groups at each n in `tests/GroupLab.Core.Tests/Statistics/SmallGroupCoverageTests.cs`, is:

| n | Mean radius and sigma, exact | Simulated | Extreme spread, shotGroups' `getRangeStat` form | Extreme spread, the form the panel now uses |
|---|---|---|---|---|
| 2 | 92.51 % | 92.60 % | 84.66 % | 95.10 % |
| 3 | 93.70 % | 93.50 % | 89.39 % | 94.96 % |
| 5 | 94.34 % | 94.42 % | 92.31 % | 95.17 % |
| 10 | 94.71 % | 94.78 % | 93.91 % | 94.93 % |
| 20 | 94.86 % | 94.92 % | 94.68 % | 94.94 % |

- **Mean radius and sigma** are under nominal only because the c4 correction multiplies both endpoints, which is how shotGroups does it and what brief section 5 has GroupLab match. The exact coverage is `P(q_lo / k² ≤ χ²(2(n − 1)) ≤ q_hi / k²)` with `k = 1 / c4(2n − 1)`, now `IntervalCoverage.RayleighSigma`, and the panel labels each interval with it, "94.3% interval" at five shots, never a bare 95.
- **Extreme spread's shotGroups form** scales the observed spread by the table's quantiles over its mean, and so does not cover the expected spread at its stated level. The panel now uses `RangeStatistics.MeanInterval`, observed times mean over the quantiles, which does. The harness still compares shotGroups' form against shotGroups. This is handled and not a question; it goes to section 15.4 as a known difference with the next statistics amendment unless you say otherwise.

**So Alan's two-shot interval was not overconfident about coverage.** 0.476 to 5.744 in covers the truth 92.5 percent of the time. What misled was a headline to three decimals over an interval spanning a factor of twelve, which is a width problem, and withholding the figure fixes it either way.

**Options.**

- **A. Five, as built.** Section 9.1's named row. Between 5 and 19 shots the panel also prints section 9.1's range in words: "From 5 shots the true group size could be anywhere from 0.68 to 1.92 times what they measure."
- **B. Ten.** The factor falls to 1.96, and it matches your example sentence, "10 before the interval means much". It withholds every five-shot group, which is most of what people fire.
- **C. Five for the figure and ten for the interval.** Print the figure from five and its interval only from ten. This hides the one thing that says how little five shots know, so I would not.

**What I would choose: A.** It is the row section 9.1 already tells us to put in front of a user, and the range sentence up to twenty carries the rest of entry 24's "between that minimum and about twenty". If you pick B it is one constant and one test.

---

## 2026-09-14, question 11: three differences the M3 harness found between shotGroups and its own fixtures

**Status: answered 2026-09-15**, by `docs/NOTES-FROM-PLANNING.md` entry 23 section 1: A, A and A.

**Where it stands.** `tests/GroupLab.Core.Tests/Statistics/ShotGroupsFixtureTests.cs` accounts for every key of the nine fixtures:

- **Compared:** 38,899 keys, including the range statistics through GroupLab's own Monte Carlo table and `compareGroups`, 0 outside section 15.3's tolerances.
- **Excluded, with the specification's or the fixture's reason:** 23,602.
- **Pending, not yet built:** none.
- **Awaiting or disputed:** 2,163 and 998 keys, for the three reasons below. The counts grew from this question's first version because `compareGroups` also takes the frame and also reports a CorrNormal CEP.

**1. The fixture cannot reproduce anything shotGroups computed from the data frame for `DFcm` and `DFinch`.**

- **The mechanism:** `groupLocation`, `groupSpread` and `groupShape` take the frame, which carries each shot's point of aim. The fixture carries `shots.x` and `shots.y` from `getXYmat(..., relPOA = FALSE)` and no aim.
- **The fixture's own evidence:** its frame-based centre disagrees with its matrix-based centre (`groupLocation.ctr` against `getConfEll.ctr`) in 10 of 10 scopes of `DFcm` and 10 of 10 of `DFinch`. For example, `DFinch` series 8 gives x = 0.3607 against 7.7067.
- **The other seven datasets:** agree in every scope.
- **Where spread is affected too:** covariance, `groupSpread.covXY` against `getConfEll.cov`, differs in the pooled scope of both. It also differs in `DFcm` series 5 but not in `DFinch` series 5, although section 15.2 calls them the same data.
- **What the harness does:** these 2,163 keys, including the two datasets' `compareGroups`, are routed to "awaiting" by that test on the fixture, not by dataset name.
- **Why it matters for the gate:** section 15.5 point 2, `DFcm` and `DFinch` agreeing after conversion, is exactly what they would show.
- **And the two are not the same data.** Every `DFcm` shot is a `DFinch` shot times 2.54 to 1e-15, but shot 242, at (15.25016, -10.90422) cm, is in series 5 of `DFcm` and series 4 of `DFinch`, so those two series hold different shots in the two frames (series 5 has 47 shots against 46). `tests/GroupLab.Core.Tests/Statistics/UnitSystemTests.cs` asserts that this one shot is the only difference, then shows GroupLab's results agree to 1e-12 after conversion with both grouped the inch file's way, and its angular results differ by exactly the rounding of 25 m to 27.34 yd, 1.2e-5. Point 2 is met on the shots; it cannot be met on the frames as shipped.

**2. shotGroups' CorrNormal CEP is looser than its own distribution, and section 15.3 compares it at 1e-8.**

- **The distribution agrees:** its CorrNormal hit probabilities, `getHitProb`, match GroupLab's Hoyt CDF to 1e-15 on every fixture.
- **The quantile does not:** under that same CDF, shotGroups' own CEPs miss their probability. On `DF300BLK` the misses are +3.7e-6, +2.9e-6 and -5.8e-7 at 0.50, 0.90 and 0.95. GroupLab's CEP meets it to 1e-12.
- **The size of the difference:** 3e-6 to 2.3e-5 relative, across 899 keys, each checked by the harness for exactly that evidence.
- **The likely cause:** a root finder with a coarse default tolerance.

**3. shotGroups' `fromMOA` for SMOA is 1 + 6.21288e-10 times the exact inverse of its own `getMOA`.**

- **Where:** in every scope of every fixture, 99 keys.
- **The tolerance it misses:** section 12.5's anchor, "1 inch at 100 yards is exactly 1.000000 SMOA", holds for `getMOA`, but the round trip misses section 15.3's 1e-12 for angular conversions.

**Four findings, handled and not questions.**

- **`getMinBBox`:** its angle is the direction of the longer side, while its width is always the first side.
- **`getMaxPairDist`:** it reports whichever tied pair R met first, which on `DFlandy04` differs from GroupLab's, at the same 0.442108583947428.
- **The fixture's MANOVA is the intercept row.** `sg_dump.R` takes `MANOVA[1, ]`, which in R's `anova.mlm` tests whether the mean over all shots is the origin, not section 8.2's test of the group centres. GroupLab reproduces that row for the gate, to 1e-13 on the five datasets whose frames are not shifted, and computes the group test separately. Row 2 would be the group test.
- **The multi-group p-values are Monte Carlo.** Every Fligner-Killeen and Kruskal-Wallis p-value is an exact multiple of 1/9999 (`DF300BLKhl`: 9554, 2385 and 6623 over 9999), so `coin` resampled for them, and like `groupShape.multNorm.p.value` no other generator can reproduce them. Their statistics match, and the harness excludes each p-value only after checking that it is such a multiple. The two-group Ansari-Bradley and Wilcoxon p-values are exact and match to 1e-15.

**Options.**

- **For 1:**
  - **A.** Regenerate the two fixtures with each shot's point-of-aim-relative coordinates as well, `getXYmat(..., relPOA = TRUE)`, as `shots.xPOA` and `shots.yPOA`. It is one R run.
  - **B.** Leave the 2,163 keys out of the gate and say so.
- **For 2:**
  - **A.** Gate the CorrNormal distribution at 1e-8 through the hit probabilities, which already pass. Require GroupLab's CEP to satisfy that distribution at 1e-12, and compare shotGroups' CEP at 1e-4 relative.
  - **B.** Keep 1e-8 on the CEP and replicate shotGroups' root finder, including whatever tolerance it happens to use.
- **For 3:**
  - **A.** Add it to section 15.4's known differences.
  - **B.** Reproduce shotGroups' constant.

**What I would choose: A, A and A.** Each keeps GroupLab exact where shotGroups is not, and keeps the gate checking something true.

**Not a question, but yours to know.** `grouplab stats coverage` measures GroupLab's BCa bootstrap covering a known truth 79.5, 89.1 and 92.7 percent of the time at 10, 25 and 50 shots, against a nominal 95. Section 6 flags only groups under 10 shots as unreliable. Nothing in section 15.5 gates it; it bears on what the interface should say beside a bootstrap interval. `docs/PHASE1-RESULTS.md` M3.1 has the table.

---

## 2026-09-14, question 10: the shotGroups fixtures M3 is gated on are not in the repository, and R cannot run here

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 18 section 1.

**What the documents say exists.**

- `DESIGN.md` line 520, Phase 2: "The shotGroups fixtures are already generated, by `tools/shotgroups/sg_dump.R`."
- `docs/PHASE1-BRIEF.md` section 5: "Generated fixtures under `test/fixtures/shotgroups/` with a README stating provenance, package version, R version and licence".
- `docs/STATISTICS.md` section 15.1: "The fixtures are **generated output**, checked in for reproducibility so that contributors need no R installation to run the test suite."

**What the repository has.**

- **Only the driver:** `tools/shotgroups/sg_dump.R`, added in `b53c606`.
- **No output:** no `shotGroups_*.json` or `.csv` anywhere in the tree, and no `test/fixtures/`.
- **No R:** `where Rscript` finds nothing.
- **Why I don't install it:** entry 2 says "Nothing needs installing on this machine".

**What the gate needs that `sg_dump.R` does not write, even once run.** This is for `docs/STATISTICS.md` section 15.5, items 1, 2, 3 and 5.

1. **The inputs.**
   - **What the dump holds:** shotGroups' outputs keyed `<function>.<component>.<row>.<col>`, and no coordinates.
   - **What GroupLab needs:** each dataset's shots, to compute its own side. That is `point.x`, `point.y`, `distance`, the units, and both `group` and `series`, since section 15.4 item 8 says the savage frames key on `series`.
   - **Licence:** these are shotGroups' data. Section 15.1's arrangement, generated fixtures with provenance in the test tree, is the one I would apply to them, but that is yours to confirm.
2. **Groups.** The script passes the whole data frame to `getXYmat` and to every function. So `DFcciHV`, `DF300BLKhl`, `DFlandy04`, `DFlandy01` and `DFsavage` each give one pooled result, not per-group results.
3. **`compareGroups`.** It is never called. The two-group and multi-group branches of section 15.2 have no reference, and the `coin` branch, section 15.4 item 6, is unrecorded.
4. **The Monte Carlo reference.** Item 5 checks GroupLab's tables against `DFdistr` "to **within 0.2 percent on the mean and 0.5 percent on the 2.5 and 97.5 percent quantiles**, for `n` from 2 to 50 and `nGroups` from 1 to 10". Nothing exports those cells.
5. **The eight datasets of section 15.2.** Each is one invocation.

**Options.**

- **A. The planning session extends the dump, runs it with R 4.3.3 and shotGroups 0.8.4, and commits the output.** This is how question 2's libapriltag corners came back.
  - **What it holds:** the inputs, per-group outputs, `compareGroups` with the `coin` branch recorded, and the `DFdistr` cells, with section 15.1's README.
  - **Where it lands:** the test project is `tests/GroupLab.Core.Tests/`, so I'd put it in `tests/GroupLab.Core.Tests/Fixtures/shotgroups/`, unless you want the brief's `test/fixtures/shotgroups/`.
  - **Cost:** one R session.
- **B. Allow R and shotGroups to be installed on this machine.**
  - **Cost:** it reverses entry 2 for a toolchain that is not part of the build.
  - **A second cost:** I would then write the fixture extension against an R API I cannot check against its documentation here.
- **C. Build M3 without the comparisons, and read them in Phase 2.**
  - **What needs nothing:**
    - sections 3.4, 9.1, 9.2, 10 and 12.5 publish exact values that depend only on `n` and constants;
    - items 4 and 6 are synthetic truth;
    - the Monte Carlo tables can be generated.
  - **Cost:** gate items 1, 2, 3 and 5 go unread. `DF300BLK`'s values quoted in sections 3.3 and 4 cannot be reproduced without its 20 shots.

**What I would choose: A, with C's build going ahead while A is run.** The brief and the specification authorise the build; only the gate reading needs the files. If you agree, say so in the answer and I will start the build without waiting for them.

---

## 2026-09-14, question 9: gate 2's 0.01 in hole-centre tolerance sits at the noise floor measured on real paper

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 18 section 3.

`docs/PHASE1-BRIEF.md` section 4.4:
- **Item 2:** "render-and-difference recovers at least 99 percent of holes with no false positives at a hole-centre tolerance of 0.01 in".
- **Item 3:** "The 0.008 in centroid noise floor measured on real paper is the number it will eventually be judged against, and a synthetic figure far below it means the synthesis is too clean."

**The measurement.** From `docs/PHASE1-RESULTS.md` M2.2, held-out seeds 1001 to 1003, run once, three sheets per row:

| DPI | Holes per bull | Holes | Within 0.15 in | Within 0.01 in | Strays | Centre error median / 95th pct (in) |
|---|---|---|---|---|---|---|
| 600 | 1 | 84 | 100.0% | 76.2% | 1 | 0.0063 / 0.0192 |
| 600 | 2 | 168 | 94.0% | 61.9% | 2 | 0.0074 / 0.0375 |
| 300 | 1 | 84 | 100.0% | 82.1% | 0 | 0.0067 / 0.0148 |
| 300 | 2 | 168 | 96.4% | 56.5% | 0 | 0.0082 / 0.0328 |

**Why the tolerance decides it, not the detector.**

- **Where 0.01 in falls:** the median centre error is the paper floor item 3 names, which puts 0.01 in near the 75th percentile of the error.
- **What 99 percent would take:** the 99th percentile of the error under 0.01 in. That is a centre error well below the floor, which item 3 says means a synthesis too clean.
- **Why a better detector can't fix it:** the synthetic truth centre is the point a lobed rim was drawn around. A centroid of a lobed star reaches that point only as closely as the lobes allow.

**Options.**

- **A. Keep 0.01 in.** The gate then fails on any synthesis that passes item 3's realism test.
- **B. Gate recall and false positives at a match tolerance, and keep centre accuracy under item 3.**
  - **The tolerance:** 0.15 in, the survey's hit tolerance. Centre accuracy is judged against paper.
  - **How the held-out run reads under B:**
    - one hole per bull meets 99 percent recall at both resolutions, but has 1 stray over three sheets at 600 DPI;
    - two per bull does not meet it, at 94.0 and 96.4 percent.
- **C. Set the tolerance from the floor, for example 0.02 in.** One hole per bull's 95th percentile is 0.019 and 0.015 in, so it would also sit near the line.

**What I would choose: B.** It separates finding a hole from locating it. Locating is then judged against paper, which no synthesis can stand in for.

---

## 2026-09-14, question 8: the sheet contradicts entry 15's reading of the table frames' EXIF, and the joint-fit lens key mixes two pixel geometries

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 16 section 2.

Entry 15 section 1: "Two of the three tags say ultrawide and one says main, so the odd one out is the 35 mm equivalent and it is wrong."

**`docs/PHASE1-RESULTS.md` M1.5 measured the opposite on three of the four table frames.**

- **Fitted alone:** `20260913_130543`, `130550` and `130554` recover 2366, 2353 and 2771 px. That is near the 2556 px their 23 mm tag gives, and far from the 1621 to 1940 px that `ultrawide1-3` recover.
- **Fitted jointly:** forced into the ultrawide's shared 1659 px, they degenerate, keeping 8, 4 and 0 of 136 corners.
- **The fourth frame:** `130559` recovers 906 px on a nearly frontal flat sheet, which constrains its focal length least.
- **Not the start:** `ultrawide1-3` give the same figures to five digits whether they start at 1444 or 2556 px.

**One explanation fits all three tags being true.** The phone took the close-up table frames on the ultrawide sensor and cropped them to the main camera's field of view. The physical focal length and f-number then describe the lens, and the 35 mm equivalent describes the pixels. A joint fit keyed on focal length and f-number (entry 6) then puts two pixel geometries into one fit, and their normalised distortion differs too. That explanation is not established. The mixing is: the shared 2.2 mm lens, k1 -0.031 and k2 +0.022, includes the four ungated table frames.

**Options.**

- **A. Key joint fits on focal length, f-number and 35 mm equivalent together.**
  - The table frames become their own group, and `ultrawide1-3` get a lens of their own.
  - It is one grouping key in `SurfaceFrames.FitByLens`.
  - It changes the M1.5 ultrawide figures through the shared lens, and they would be re-run and reported.
- **B. Keep entry 6's key, and leave out of joint fits any frame whose tags disagree with the rest of its group.** The effect on these frames is the same, but a frame with a wrong tag is lost instead of grouped.
- **C. Keep the key as it is.** The ultrawide's shared lens stays contaminated by four ungated frames.

**What I would choose: A.** A joint fit shares pixel geometry, and the 35 mm equivalent is the tag that describes it. Separately, for the protocol: record which camera took each frame, and shoot from far enough that the phone keeps that camera.

---

## 2026-09-14, question 7: three of the five marker module sweep sheets for next weekend now fail test 26f as an error

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 16 section 3.

**How the sweep came to fail.**

1. **Built on the Phase 0 sheet.** `docs/PHASE1-RESULTS.md` M0 built the sweep on `GL-CF25-LTR` as printed for Phase 0, gap 456. So its 0.5 mm sheet is the Phase 0 sheet by identifier, `GL-YCSK-DZZ1-R0VJ-4T5Y`, and the sweep carries its own control.
2. **The geometry commit moved the live sheet.** Entry 13's geometry commit, `3dcc814` (was `d73b9a4` before the 2026-09-14 rewrite), made test 26f an error and moved the live sheet to gap 454.
3. **Rebased on the frozen definition.** The sweep's base is now `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, the first argument of `grouplab sweep module`. It reproduces all five definitions and PDFs byte for byte.
4. **Three sheets fail.** The validator now reports 26f errors on the 0.5, 0.6 and 0.8 mm sheets: `GL-YCSK-DZZ1-R0VJ-4T5Y`, `GL-683J-3ZR8-60D5-0FGG` and `GL-SEBE-5F06-GVTF-3CZK`. Their sighters sit outside the marker lattice, and the sweep renders them only by accepting an invalid definition. The 0.3 and 0.4 mm sheets carry an extra marker row and conform.

**Options.**

- **A. Print the five as they are.**
  - The sweep measures the module floor, which the sighters do not bear on, and the 0.5 mm sheet stays the Phase 0 sheet.
  - Cost: three printed sheets the validator rejects, which the sweep README and the M0 section would have to call measurement sheets, not library sheets.
- **B. Rebase the sweep on the live `GL-CF25-LTR`, gap 454.**
  - Five sheets that conform, with five new identifiers, and the M0 table and PDFs regenerated. It is a few minutes of work.
  - The 0.5 mm sheet is no longer the Phase 0 sheet, so the control sits one 2 dmm sighter change away from what Phase 0 printed.

**What I would choose: A.** For a module-floor measurement, a control identical by identifier is worth more than sighters that conform when the measurement never uses them. B is right if nothing printed should fail the validator.

---

## 2026-09-14, question 6: a sighter gap cannot make three of the four sheets conform, so test 26f cannot become an error in the geometry commit as written

**Status: answered 2026-09-14**, by `docs/NOTES-FROM-PLANNING.md` entry 13.

Notes entry 11 asks for one commit that sets `GL-CF25-LTR` to a sighter gap of 454, `GL-LR300-R24` and `GL-LR300-R36` to 1142, `GL-CF25-100M-A4` "per the sweep", declares `cells.sighterGap` on each, and promotes test 26f to an error. Before changing anything I ran those changes through `tools/layout/layout.py`, unmodified. **Only `GL-CF25-LTR` conforms afterwards.** The other three keep bull columns outside the lattice horizontally, which is question 4's finding 1, and no sighter gap reaches a column. Promoting 26f to an error in that commit fails three built-in sheets.

**The measurement.** Margins are how far the outermost marker centre lies beyond the outermost bull centre on each side, in dmm; negative is outside, and zero conforms under the inclusive rule. Every row places with zero layout errors and zero warnings.

| Sheet | Change | Scoring bulls | Markers | Left / right / top / bottom | 26f |
|---|---|---|---|---|---|
| `GL-CF25-LTR` | as printed, gap 456 | 25 | 34 | +190 / +190 / +190 / **-266** | fails |
| `GL-CF25-LTR` | **gap 454, entry 11** | 25 | 38 | +190 / +190 / +190 / +190 | **conforms** |
| `GL-CF25-100M-A4` | as printed, gap 480 | 25 | 32 | **-200 / -200** / +200 / +200 | fails |
| `GL-CF25-100M-A4` | any gap from 480 down to 301 | 25 | | columns unchanged | **fails** |
| `GL-LR300-R24` | as printed, gap 1219 | 30 | 35 | **-508 / -508** / +508 / **-508** | fails |
| `GL-LR300-R24` | **gap 1142, entry 11** | 30 | 40 | **-508 / -508** / +508 / +508 | **fails** |
| `GL-LR300-R36` | as printed, gap 1219 | 36 | 48 | **-508 / -508** / +508 / **-508** | fails |
| `GL-LR300-R36` | **gap 1142, entry 11** | 36 | 56 | **-508 / -508** / +508 / +508 | **fails** |

**Why no gap reaches the columns.** `layout.py` places the lattice columns half a pitch outside the outermost bull columns and drops any marker whose box crosses half the safe margin. On `GL-CF25-100M-A4`, five columns at a 400 dmm pitch on a 2100 dmm page put the outer marker columns at x = 50 and 2050, boxes 20 to 80 against a limit of 60, so both columns go. The rolls are the same at a 1016 dmm pitch.

**Two changes do make them conform**, measured the same way:

| Sheet | Change | Scoring bulls | Markers | Left / right / top / bottom |
|---|---|---|---|---|
| `GL-CF25-100M-A4` | `grid-boundary-half-1`, gap 480 | 25 | 88 | 0 / 0 / +200 / +200 |
| `GL-LR300-R24` | `grid-boundary-half-1`, gap 1142 | 30 | 113 | 0 / 0 / +508 / +508 |
| `GL-LR300-R36` | `grid-boundary-half-1`, gap 1142 | 36 | 151 | 0 / 0 / +508 / +508 |
| `GL-CF25-100M-A4` | 4 columns | **20** | 36 | +200 all round |
| `GL-LR300-R24` | 5 columns | **25** | 48 | +508 all round |
| `GL-LR300-R36` | 8 columns | **32** | 63 | +508 all round |

**Options, with their costs.**

- **A. The half lattice on the three sheets, in the one geometry commit.** Every bull brackets, no scoring bull is lost, and the scheme already ships on the 300 yard tiles, which conform at the same margin of zero. It takes the marker count to 2.75, 3.2 and 3.1 times what it was, which is more ink near the bulls and more identifiers, though R36's 151 is well inside the 587 the family holds. The outer columns become interpolated along the lattice edge rather than surrounded, which is the tiles' condition.
- **B. Drop a column.** Every bull brackets with margin to spare, but `GL-CF25-100M-A4` falls to 20 scoring bulls, below the library's 25, and the rolls lose 5 and 4 bulls. `TARGET-LIBRARY.md` section 1 would need a third documented exception.
- **C. Land `GL-CF25-LTR` now, keep 26f a warning, and design the columns separately.** Cheapest today, but it is two geometry commits where entry 9 and entry 11 ask for one, and the rolls' identifiers would change twice if their fix changes the scheme.

**What I would choose:** A, because it is the only option that makes every sheet conform without losing a bull or a library requirement, and it uses a scheme the library already ships.

**A second, smaller conflict with "does not change the schema".** The worked example of TARGET-SCHEMA.md section 4 is `GL-CF25-LTR` as printed: identifier `GL-YCSK-DZZ1-R0VJ-4T5Y`, `sighterGap` 456, and a paragraph explaining the 456. `ReferenceEncoderParityTests.Section4DocumentEncodesToTheReferenceSheetBytes` asserts that it encodes to `tools/gltd/check.py`'s bytes for the live `GL-CF25-LTR`, and four test files pin the identifier. Once the live sheet moves to 454 that test fails. Either section 4 changes, with a new identifier, or it stays as the as-printed reference and the test compares it with the frozen definition, `targets/frozen/phase0/GL-YCSK-DZZ1-R0VJ-4T5Y.gltd.json`, which is byte-identical to it. **I would keep section 4 as it is** and re-point the one test, so the schema text is untouched, as entry 11 intends.

**Two small corrections to entry 11, for the record.** `cells.sighterGap` is specified in TARGET-SCHEMA.md sections 3.6 and 7, not 3.10. `docs/TARGET-LIBRARY.md` lists no identifiers; what the commit changes there is the marker counts in its sheet table, 34 to 38 for `GL-CF25-LTR` and the counts of whichever option is chosen for the other three.

**What landed while this is open.** The frozen-fixture part of entry 11 does not depend on the answer, so it is committed: the three definitions the sample set was printed from are in `targets/frozen/phase0/` with a README, `grouplab spike` and the paper gate test resolve against them, a test loads, identifies and validates each, and rerunning every spike command against them reproduces every measured value in `scans/phase0/measurements/`; only the detection times in `threshold.json` differ, as they do between any two runs. Nothing in `tools/layout`, `targets/*.gltd.json` or the validator's severities has changed. Entry 11 stays open until this is answered.

---

## 2026-09-13, question 5: the wall photographs are not of a flat sheet

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 8.

Notes entry 6 describes the nine new photographs as sheet 3 "taped flat to a wall", and sets them three questions: lens, flatness, and sighter geometry. In every frame the sheet hangs from a single pin at the top centre and its edges are visibly curved (`ultrawide1.jpg` shows the fixing at the top edge, `telephoto2.jpg` an orange pin); nothing holds the lower half. The measurement agrees, and as a result the set cannot answer any of the three questions. `docs/PHASE0-RESULTS.md` section 3a has the full table; `scans/phase0/measurements/photos.json` has every corner and bull.

**The measurement, shipped pipeline, inches.**

| Set | Frames | Corners the fit keeps within 0.01 in | Corner RMS over all corners | Worst bull | Scoring bulls over the gate |
|---|---|---|---|---|---|
| Sheet 1 on a table, ultrawide | 4 | 135 to 136 of 136 | 0.0025 to 0.0038 | 0.0057 to 0.0108 | 0 to 3 of 25 |
| Sheet 3 on a wall, ultrawide | 3 | 42 to 66 of 128 to 136 | 0.023 to 0.060 | 0.070 to 0.091 | 13 to 21 of 25 |
| Sheet 3 on a wall, main | 3 | 25 to 90 of 104 to 136 | 0.014 to 0.057 | 0.048 to 0.114 | 8 to 21 of 25 |
| Sheet 3 on a wall, telephoto | 1 (two excluded) | 38 of 132 | 0.031 | 0.096 | 21 of 25 |

The misfit is largest at the free bottom edge: the two lowest marker rows sit 0.026 to 0.138 in from the fit on every wall frame, against 0.0025 to 0.0033 in on the table frames. `telephoto2`, on the longest lens, needs 0.020 in RMS from a homography alone.

**What that does to entry 6's three questions.** Lens: every lens fails by the size of the surface misfit, so a lens effect of 0.004 to 0.011 in cannot be seen; focal length does not rescue a curved sheet, which is all the set shows. Flatness: the table frames are the flatter set, 6 to 16 times better by worst bull with the same lens, so the paired comparison is inverted. Sighters: scoring bulls fail on every frame, so the sighters cannot be isolated.

**Options, with their costs.**

- **A. Reshoot sheet 3 held flat**, taped along all four edges or laid under a sheet of glass, main camera, one square-on and one about 20 degrees off-axis, whole sheet in frame. About two minutes, no new paper. It answers entry 6's questions as intended, and the pipeline, raw-row output and grouped table are already in place, so the report follows the same day.
- **B. Close Phase 0 on the evidence as it stands.** The paper gate passes, the photograph gate fails, and what is known is that the table frames fail on the sighters and by a thousandth or two on the scoring bulls, and that a hanging sheet fails by a tenth. The photograph path moves to Phase 1 with the flatness and sighter questions open. Costs nothing now; Phase 1 then starts without knowing whether a flat photograph passes.
- **C. Register a curved sheet**, by local or piecewise registration from nearby markers. It would make hanging sheets usable, which matters because users will pin targets up this way, but it goes beyond "markers, homography and bull location" in the brief, and it cannot help the sighters, which no nearby marker brackets.

**What I would choose:** A, because it is the experiment entry 6 intended and costs two minutes. C is worth recording as a Phase 1 requirement whatever A shows, because a pinned sheet is the normal case at a range. The spike report carries the photograph gate as a failure on all eleven usable frames.

---

## 2026-09-13, question 4: the bracketing rule, proposed, and three sheets a sighter gap cannot fix

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 9.

Notes entry 5 asks for the wording of a TARGET-SCHEMA.md section 7 rule and a conformance test, from the finding that the sighters of `GL-CF25-LTR` sit outside the marker lattice. The geometry change is deferred, as entry 5 now says, and nothing in `tools/layout`, `targets/` or the validator has been changed. Measuring the rule against the whole library before proposing it turned up two things it has to decide.

**Every sheet, measured on the geometry that was printed.** How far a marker centre lies beyond the outermost bull centre on each side, in dmm; negative means a bull lies outside the lattice on that side. The last column is entry 5's sighter-only criterion, reproduced by sweeping `sighterGap` down from 1.2 times the pitch in `tools/layout/layout.py`.

| Sheet | Left | Right | Top | Bottom | Smallest gap with a marker row below the sighters |
|---|---|---|---|---|---|
| GL-CF25-LTR | +190 | +190 | +190 | **-190** | **454**, 38 markers |
| GL-CF25-LTR-D | +190 | +190 | +190 | +190 | no sighters |
| GL-CF25-A4 | +190 | +190 | +190 | +190 | 456, unchanged |
| GL-CF25-100M-A4 | **-200** | **-200** | +200 | +200 | 480, unchanged |
| GL-CF30-LTR | +175 | +175 | +175 | +175 | no sighters |
| GL-RF25-LTR, GL-RF25-A4 | +127 | +127 | +127 | +127 | 304, unchanged |
| GL-RF36-LTR | +127 | +127 | +127 | +127 | 304, unchanged |
| GL-LR25-TAB, GL-LR25-A3 | +254 | +254 | +254 | +254 | 609, unchanged |
| GL-LR30-TAB | +254 | +254 | +254 | +254 | 609, unchanged |
| GL-LR300-T | **0** | **0** | **0** | **0** | no sighters |
| GL-LR300-TA4 | **0** | **0** | +508 | **0** | no sighters |
| GL-LR300-R24 | **-508** | **-508** | +508 | **-508** | **1142**, 40 markers |
| GL-LR300-R36 | **-508** | **-508** | +508 | **-508** | **1142**, 56 markers |
| GL-LR300-R42 | +508 | +508 | +508 | +508 | 1219, unchanged |

The GL-CF25-LTR bottom figure is before the fix; at 454 it becomes +190. Entry 5's table is reproduced exactly.

**Finding 1: a sighter gap cannot bracket three sheets.** On `GL-CF25-100M-A4`, `GL-LR300-R24` and `GL-LR300-R36` the outermost bull columns sit outside the lattice horizontally, and on R24 and R36 that stays true at a gap of 1142. The mechanism is the one entry 5 found for the row: the lattice column beyond the outermost bulls is dropped by the half-safe-margin edge test. On `GL-CF25-100M-A4` the columns would be at x = 50 and 2050, whose boxes reach 20 and 2080 against limits of 60 and 2040. A rule worded "the lattice must bracket every bull" makes these three sheets non-conforming, and the fix is not a declared sighter gap. It would move the bull grid or the page, which is a geometry decision.

**Finding 2: the tiles sit exactly on the edge.** On `GL-LR300-T` and `GL-LR300-TA4` the outermost markers share coordinates with the outermost bulls, a margin of 0. They are bracketed if the rule is inclusive and not if it is strict.

**Finding 3: GLTD-B does not carry `sighterGap`.** Section 5 has no field for it, so a definition decoded from a sheet's codes loses the declaration and raises test 23's warning on every decode of the three changed sheets, which section 3.6 says the field exists to prevent. Either the body gains the field, or test 23 is scoped to documents that were not decoded, or a decoded sighter gap that brackets is exempt.

**Proposed wording, for section 7**, after the sighter-gap rule:

> **The fiducial lattice must bracket every bull.** Every bull centre, sighters included, must lie on or inside the rectangle bounded by the outermost surviving marker centres. A bull outside it is interpolated on a flat scan and extrapolated on anything that is not flat, and the Phase 0 photographs measured the cost: the sighters of GL-CF25-LTR, one dropped marker row outside the lattice, were the worst bull on three photographs of four. Where a derived scheme leaves the sighter row outside, a generator shortens the sighter gap from 1.2 times the pitch, one dmm at a time, until the lattice brackets, and declares `cells.sighterGap`.

**Proposed conformance test**, numbered to sit with the other layout tests:

> 26f. A bull centre outside the rectangle bounded by the outermost surviving marker centres is an error.

**Options for the two decisions.**

- **Inclusive or strict.** Inclusive, as worded above, keeps the tiles conforming; a bull on the lattice's edge is interpolated along that edge. Strict would also fail both tiles, whose only fix is a denser scheme. **I would choose inclusive.**
- **Error or warning, given finding 1.** As an error, three more sheets need a geometry change before the rule can land. As a warning first, it can land with the sighter fix and name the three. **I would choose a warning until the three sheets are fixed, then an error**, so the rule is not blocked on geometry nobody has designed yet.

---

## 2026-09-13, question 3: PHASE0-PRELIM's split of the paper error belongs to its centroid

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 4.

`docs/PHASE0-PRELIM.md` section 3: "**Not the measurement method.** The result is unchanged across three window radii, and a symmetric estimator applied to a symmetric object cannot manufacture a spatially structured field." Section 5a: "roughly 0.0019 inches of it is systematic and reproducible across sheets, and roughly 0.0010 inches is random from sheet to sheet." DESIGN.md section 21 repeats both figures.

The spike reproduces the preliminary centroid and measures a second locator against it on the same registration. The document is yours, so this is raised rather than edited.

**The measurement**, sheets 1 to 3 of `GL-CF25-LTR` at 600 DPI, the same homography for both locators, inches:

| Locator | Single sheet mean / worst | Systematic mean / worst | Random RMS | Systematic after a quadratic over the page, mean / worst |
|---|---|---|---|---|
| Thresholded centroid, the preliminary method | 0.00217 / 0.00431 | 0.00201 / 0.00374 | 0.00103 | 0.00147 / 0.00314 |
| Edge fit to the declared disc radii, shipped | 0.00132 / 0.00316 | 0.00128 / 0.00279 | 0.00047 | 0.00039 / 0.00071 |

The edge fit was chosen on the synthetic raster before it saw paper, as `docs/PHASE0-SPIKE-BRIEF.md` section 5 requires: 0.00022 in worst at 300 DPI and 0.00013 at 600, against the centroid's 0.00069 and 0.00025.

**What that does to the written account.**

1. **The method was part of the limit.** Stability across mask radii of 90, 100 and 110 dmm could not show otherwise, because all three masks hold the same ink: the inner ring and the dot. The edge fit also uses the outer ring and locates edges at the midpoint of their own local ink and paper levels, so ink density does not move it.
2. **The split is about 0.0013 in systematic and 0.0005 in random**, not 0.0019 and 0.0010.
3. **Most of the systematic part is smooth.** A quadratic over the page leaves 0.0004 in mean and 0.0007 worst. The prize a printer calibration could claim, section 6 of the brief's last item, is most of the systematic field rather than a fraction of it.
4. **The field is still paper-fixed.** The rotated rescan correlates with sheet 2 at +0.63 under the edge fit, against -0.11 for the scanner-fixed prediction, so section 5a's conclusion stands. The gate conclusion stands too: five thousandths holds with more margin.

**Options.** A: amend `docs/PHASE0-PRELIM.md` and the DESIGN.md section 21 figures with a dated note citing the spike, leaving the original text. B: leave both as a record of what the scratch measurement showed, and let `docs/PHASE0-RESULTS.md` carry the corrected figures. **I would choose A**, because DESIGN.md is what the next phase reads, and it currently states a random component twice the measured one.

---

## 2026-09-13, question 2: libapriltag's corners, to re-rank the detectors with the Phase 0 locator

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 3.

Notes entry 2 asks for measurement 8 of `docs/FIDUCIAL-DECISION.md` section 10 to be re-run with the Phase 0 bull locator and the ranking reported. Only the OpenCV half can be re-run here: nothing was installed, as entry 2 instructs, so libapriltag does not run on this machine.

**What would settle it.** For each of `gl-cf25-ltr-1-600-dpi.png`, `gl-cf25-ltr-2-600-dpi.png`, `gl-cf25-ltr-3-600-dpi.png`, `gl-cf25-ltr-2-600-dpi-rot180.png` and `gl-cf25-ltr-1-300-dpi.png`, the libapriltag detections from the same run as entry 2, committed as `scans/phase0/apriltag-corners.json`:

```
{ "<image file>": [ { "id": 0, "corners": [[x, y], [x, y], [x, y], [x, y]] }, ... ], ... }
```

Corners as `pupil-apriltags` returns them, in its own winding and pixel convention. The harness applies the conversion entry 2 records (winding reversed, no rotation) and fits each detector's corners through the same registration and the same shipped edge-fit locator. No decision is needed, only the file.

---

## 2026-09-13, question 1: the photograph gate fails on all four photographs, and the lens is not the main suspect

**Status: answered 2026-09-13**, by `docs/NOTES-FROM-PLANNING.md` entry 5.

`docs/PHASE0-SPIKE-BRIEF.md` section 7: "If the photograph gate fails, **the lens is the first suspect, not the code.** Two frames from the main camera settle it in about a minute". It fails, it was diagnosed before anything else, and the diagnosis points somewhere else, so the choice of what to do next is yours.

**The result, shipped pipeline** (homography with radial distortion, edge-fit locator), inches:

| Photograph | Markers | Corner residual RMS | Homography alone RMS | Distortion at the frame edge | Bull mean / worst | Worst bull |
|---|---|---|---|---|---|---|
| `20260913_130543.jpg` | 33/34 | 0.00319 | 0.00343 | 0.992 in | 0.00229 / 0.00530 | 15 |
| `20260913_130550.jpg` | 32/34 | 0.00329 | 0.00390 | 0.344 in | 0.00262 / 0.00973 | S3 |
| `20260913_130554.jpg` | 34/34 | 0.00370 | 0.00387 | 0.370 in | 0.00309 / 0.01083 | S3 |
| `20260913_130559.jpg` | 34/34 | 0.00248 | 0.00297 | 0.171 in | 0.00198 / 0.00922 | S3 |

**What it is not.**

- **Not the printer.** The photographs' bull fields correlate with sheet 1's own 600 DPI scan field at -0.07 to +0.33, where scans of different sheets correlate at +0.85 to +0.94. The error is added by the photograph path.
- **Not a lens term the model misses, nor any smooth global warp.** Refitting with three radial coefficients and a free distortion centre, or a quadratic or cubic warp on top of the homography, evaluated leave one marker out, improves the corner residual on no photograph consistently and brings no photograph inside the gate: the best worst bull, whichever model gives it, is 0.0050, 0.0062, 0.0063 and 0.0076 in.

**What it is, as far as the data goes.**

1. **The sighters are extrapolated.** In `targets/GL-CF25-LTR.gltd.json` the lowest marker row is at y = 8.854 in and the sighter row at y = 9.902 in. The three sighters are the only bulls on the sheet outside the marker lattice, by 1.05 in, and they are the worst bull on three of the four photographs. On a flat scan this costs little (S3 is also the worst bull on sheets 2 and 3, at 0.0032 and 0.0029 in); on a photograph it amplifies whatever the planar model gets wrong.
2. **The sheet is not one plane.** Registering each bull from only its nearest 6 or 8 markers, instead of the whole sheet, brings the worst scoring bull from 0.0053 to 0.0035 in, 0.0066 to 0.0029, and 0.0034 to 0.0021 on three photographs, and leaves the fourth at 0.0057. The sighters stay at 0.008 to 0.013 in whatever the markers, because every choice still extrapolates to them. In all four frames the sheet lies on a table rather than pinned to a wall as `docs/PHASE0-PRINT-PROTOCOL.md` section 7 asks, and a free sheet of paper does not lie flat.
3. **The lens reading is uncertain.** EXIF says f/2.2 and 2.2 mm, which reads as the ultra-wide, but also a 35 mm equivalent of 23 mm, which is normally the main camera. The fitted distortion at the outermost marker is 0.004 to 0.011 in, so the lens term is real but it is being fitted.

**Options, with their costs.**

- **A. Two frames with the main camera of sheet 3, the control, taped flat to a wall**, one square-on and one off-axis, per protocol section 7. About a minute, no new paper. It separates flatness and lens from the sighter geometry: if the scoring bulls then pass and only the sighters fail, the geometry is the finding.
- **B. Put markers below the sighter row.** This is a change to the `grid-boundary-1` placement in `tools/layout`, which the brief says not to touch, and it changes the derived marker lattice of every sheet with a sighter band. Expensive, and it should wait for A.
- **C. Gate photographs on scoring bulls only.** A redefinition of the gate. Not recommended without A; with A it may still be the wrong answer, since a sighter is still a bull a shooter fires at.

**What I would choose:** A first, then B or C with its result. The spike report carries the photograph gate as a failure, not as a pass on a narrower definition.
