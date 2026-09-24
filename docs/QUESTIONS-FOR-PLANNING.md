# Questions for the planning session

Questions going out from the Claude Code session to the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** When a genuine decision blocks you, append a dated section at the top with `Status: open`. State the question, the options with their real costs, and what you would choose and why. Commit it, push it, and stop. The answer comes back as an `open` entry in `docs/NOTES-FROM-PLANNING.md`, and once it does, change this entry's status to `answered <date>` with a one-line pointer to the notes entry that answered it. Never delete an entry.

**What belongs here.** A decision that changes the specification, changes a gate, changes geometry, or commits the project to something that is expensive to reverse. A conflict between two documents. A measurement that contradicts something written down.

**What does not.** Anything a measurement can settle, because the planning session can run measurements against the committed scans and hand back numbers. Permission for work the brief already authorises. Anything answerable by reading the documents the brief points at.

**Write it for a reader who has the repository and not the conversation.** Quote the section you are citing rather than paraphrasing it, give the numbers rather than describing them, and name the file and line where the conflict lives. A question that arrives with its evidence attached usually comes back answered in one round trip rather than three.

---

## 2026-09-23, question 48: entry 160's fourteen day rule would have moved nothing

**Status: open. Nothing is blocked; the entry count was applied, which is what the section is for.**

### What entry 160 section 1.1 says

> The live `NOTES-FROM-PLANNING.md` keeps the last fifteen entries or the last fourteen days, whichever is longer.

### Why it does not work here

This repository is eleven days old. Entry 1 is dated 2026-09-13 and today is 2026-09-23, so **every entry in the file falls inside fourteen days**, and "whichever is longer" selects the whole 1.1 MB file. Applied literally, section 1 moves nothing and the entry does not happen.

The rule is written for a project with a normal rate of entries. This one produced 151 entries in eleven days, so an age-based rule and a count-based rule are two orders of magnitude apart.

### What I did

**Applied the fifteen entry rule and ignored the fourteen day clause**, because the section's purpose is stated in its own first paragraph: the live file has to be small enough to read. Fifteen entries is 97 KB against 1,102 KB.

### What I would choose

Drop the fourteen day clause, or invert it to "whichever is **shorter**", which gives the same answer here and also behaves sensibly in a quiet month. A quiet month under the current wording would keep fifteen entries that might be months old, which is fine, so the clause is doing no work in either direction.

### Where it lives

`scripts/split-logs.py`, `LIVE_ENTRIES`, with the reasoning in a comment beside it.

---

## Answered, and moved

These 38 are in [`docs/notes/archive/questions-answered.md`](notes/archive/questions-answered.md), whole. They are listed here so a
number is never reused and a question is never lost:

> 47, 40, 38, 37, 35, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1.

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

