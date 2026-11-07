# Questions for the planning session

Questions going out from the Claude Code session to the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** When a genuine decision blocks you, append a dated section at the top with `Status: open`. State the question, the options with their real costs, and what you would choose and why. Commit it, push it, and stop. The answer comes back as an `open` entry in `docs/NOTES-FROM-PLANNING.md`, and once it does, change this entry's status to `answered <date>` with a one-line pointer to the notes entry that answered it. Never delete an entry.

**What belongs here.** A decision that changes the specification, changes a gate, changes geometry, or commits the project to something that is expensive to reverse. A conflict between two documents. A measurement that contradicts something written down.

**What does not.** Anything a measurement can settle, because the planning session can run measurements against the committed scans and hand back numbers. Permission for work the brief already authorises. Anything answerable by reading the documents the brief points at.

**Write it for a reader who has the repository and not the conversation.** Quote the section you are citing rather than paraphrasing it, give the numbers rather than describing them, and name the file and line where the conflict lives. A question that arrives with its evidence attached usually comes back answered in one round trip rather than three.

---

## Answered, and moved

These 58 are in [`docs/notes/archive/questions-answered.md`](notes/archive/questions-answered.md), whole. They are listed here so a
number is never reused and a question is never lost:

> 112, 111, 110, 109, 108, 107, 106, 105, 104, 103, 102, 101, 100, 99, 98, 97, 96, 95, 94, 93, 92, 91, 90, 89, 88, 87, 86, 85, 84, 83, 82, 81, 80, 79, 78, 77, 76, 75, 74, 73, 72, 71, 70, 69, 68, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 43, 36, 34.

---

## 2026-09-28, question 67: the printer check page is grid style 4, and its card outline stands 3 mm outside the card

**Status: open, being tested (entry 278 section 3):** Alan chose to test rather than choose. The three pages, A as built, B a hairline on the card's edge and C corner marks, are made; request 53 asks for them printed, scanned and photographed with a card, and the answer comes from measuring those photographs. A stays built until then.

Entry 273 says the check page "is built from a definition in the library like any sheet, so it reads itself from a photo or scan." The
format had no way to say "this is a check page" or to print its crosshairs, rulers, card outline and words, and it requires at least one
bull (schema `minItems: 1`, reader and decoder both). **What I built,** following question 59's precedent for the zeroing grids: a grid
style 4 whose artwork is derived in code from the page size (`GridStyle4`, TARGET-SCHEMA.md section 3.13), with the three crosshairs'
circles as bulls that are not scored, so the one-bull rule is met without changing it. On the wire a style 4 grid is a style 1 grid with
another style byte; a reader from before this refuses the frame, as it refuses any style it does not know. `GL-SCALE-LTR-1` and
`GL-SCALE-A4-1` are in the library and count as no sheet in the README's number, because they are not targets.

**One change from the approved drawing, made on a measurement:** the card outline stands 3 mm outside the card's size, and its note
reads "either side up, inside the line" instead of "edges on the line". A dark card whose edge lies on the black line shows no edge at
all: card and line are one dark band, and the card can be read as much as the line's width larger, about 1 percent, three times the
0.3 percent the card method promises. With the gap a card laid inside the line has its edges on white paper, and the synthetic test
reads dark, very dark and light grey cards, shifted and turned, to within 0.15 percent across and 0.2 down. **The question:** is the gap
acceptable as drawn, or should the concept change some other way (a thinner line, corner marks only)? A white card on white paper shows
only its shadow and is not yet measured; the wizard will say a card with color works best.

## 2026-09-24, question 51: which hole centre GroupLab should report, now that the one it reports leans toward the shadow

**Status: open. Nothing is changed yet; request 9's hand markings are the evidence that decides it.** Entry 187 section 5 agreed: nothing changes until request 9's two hand markings arrive.

### What was measured, entry 170 section 4

The detector reports each hole's **residual-weighted centroid**: every pixel inside the hole's hull, weighted by how far it is from paper.
On a scan the lid seen through a hole is light grey and the shadow of the torn edge is nearly black, so the shadow carries most of the
weight. Against a circle fitted to each hole's edge, `HoleEdgeFit`, the reported centre sits toward the shadow **the same way on all six
scans measured**, down and to the left on these, by about 0.011 in on average and up to 0.043 in on one hole. On the friend's scan, the
edge-fitted centres give an extreme spread 0.029 in larger than the detector's, which at 25.4 yd is the 0.11 MOA the outside user found
after moving the holes by hand.

### What was tried

1. **The edge fit as the reported centre.** It brings every real scan into agreement with itself, but on the synthetic sheets, whose true
   centres are known, it moved some holes by up to 0.039 in and failed three tests: a synthetic hole's rim is lobed and its edge fades,
   and the fit follows that. No quality gate separated its good fits from its bad ones, because real torn edges are just as ragged.
2. **The plain area centroid**, every hole pixel counted once. It passed every synthetic test, but on the sample scan it sat no closer to
   the edge fits than the weighted one did, 0.012 in.

### The options

1. **Keep the weighted centroid** until the hand markings say otherwise. What happens now.
2. **The area centroid.** Unbiased where the hole is symmetric, cannot be moved by shadow depth, and passes the synthetic truth.
3. **The edge fit, with a better model of a synthetic hole** so it can be tested honestly first.

### What I would choose

**Wait for request 9, then choose by it.** Two markings of the same scan by the same person say how far a person's click wanders; where the
three centres sit against those clicks says which one a person means by the centre of a hole. Choosing before that is choosing between two
estimates with no reference, which is how the weighted centroid was chosen in the first place.

---

## Answered, and moved

These 52 are in [`docs/notes/archive/questions-answered.md`](notes/archive/questions-answered.md), whole. They are listed here so a
number is never reused and a question is never lost:

> 56, 55, 54, 53, 52, 50, 49, 48, 47, 46, 45, 44, 42, 41, 40, 39, 38, 37, 35, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1.

---

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
