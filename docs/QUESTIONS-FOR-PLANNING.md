# Questions for the planning session

Questions going out from the Claude Code session to the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** When a genuine decision blocks you, append a dated section at the top with `Status: open`. State the question, the options with their real costs, and what you would choose and why. Commit it, push it, and stop. The answer comes back as an `open` entry in `docs/NOTES-FROM-PLANNING.md`, and once it does, change this entry's status to `answered <date>` with a one-line pointer to the notes entry that answered it. Never delete an entry.

**What belongs here.** A decision that changes the specification, changes a gate, changes geometry, or commits the project to something that is expensive to reverse. A conflict between two documents. A measurement that contradicts something written down.

**What does not.** Anything a measurement can settle, because the planning session can run measurements against the committed scans and hand back numbers. Permission for work the brief already authorises. Anything answerable by reading the documents the brief points at.

**Write it for a reader who has the repository and not the conversation.** Quote the section you are citing rather than paraphrasing it, give the numbers rather than describing them, and name the file and line where the conflict lives. A question that arrives with its evidence attached usually comes back answered in one round trip rather than three.

---

## 2026-09-24, question 50: question 37's D cannot find the offset without being told the bulls

**Status: open. Nothing is blocked: A is built, and D is not, for the reason below.**

### What entry 149 section 3 asks

> **D, offering it where it would change the answer.** When a certain offset exists that would move shots, the review queue says so in
> plain words and offers to apply it. That is what makes A discoverable.

### Why it cannot be built as written

I built it: with no bulls named, work out what naming every scoring bull would do, and offer "Every bull" where a certain offset would
move shots. On `SheetOffsetAssignmentTests`' shifted sheet, twenty shots aimed at columns 2 to 5 and landing one bull to the left, it
finds **nothing**: every bull named moves no shot and the offset is not certain.

That is geometry, not a bug. If every bull was aimed at, a group shifted one whole bull to the left is exactly the same sheet as the same
group aimed one bull to the left with no shift. The two readings cannot be told apart from the holes, so the solver is rightly uncertain,
which is what question 46 measured on scan 5. **The offset that matters is only certain once the shooter has said which bulls**, and at
that point the matching already applies it. So there is no case where D has something certain to offer that A has not already been told.

What D could honestly say without the fact, "the shots sit 0.4 in left of their bulls, all by the same amount", is a partial shift, which
the zero correction already reports, and it is blind to the whole-bull shift that is the actual defect.

### The options

1. **Leave D out.** A, the row and column selection and the "Bulls you fired at" control are how the fact gets in.
2. **Make D a prompt, not a finding.** Where nobody has said which bulls and the sheet has more bulls than shots, one review item asks
   "Which bulls did you fire at?" and points at the control, with no claim that anything would move. It is discoverability without a
   guess, and it would appear on nearly every partly used sheet.

### What I would choose

**Option 2, but only where the sheet has more scoring bulls than shots**, because that is exactly the case where the one-to-one matching
has room to pick the wrong bulls and nothing on the screen says so.

---

## 2026-09-24, question 44, the part still open: the bent-sheet model throws outside the page

**Status: open, and nothing a person can reach is affected.** Entry 171 section 4 closed the rest of question 44, which is in the answered archive.

`compare-photos --model surface` throws on `20260920_153336.jpg`: `SurfaceMapping.ToPage` is a Newton iteration from a homography's guess, nothing bounds where it steps, and a point far outside the sheet reaches fold arrays built to span the page and no further. Only `ExpectedImage.Render` asks for such a point, and only through that command. `SurfaceCrashTests` records it. The open part is whether to bound the iteration or clamp the fold lookup, and it waits until the surface model is offered anywhere a person can reach.

---

## Answered, and moved

These 46 are in [`docs/notes/archive/questions-answered.md`](notes/archive/questions-answered.md), whole. They are listed here so a
number is never reused and a question is never lost:

> 49, 48, 47, 46, 45, 44, 42, 41, 40, 39, 38, 37, 35, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1.

---

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

