---
title: "The range test log"
description: "Every range and bench test behind GroupLab in one place: what was shot, with what, what it was meant to find out, and where the results are written up. Updated after every trip."
group: Range tests
number: 30
written: 2026-09-22
data_date: "Running log; latest entry 2026-09-26"
samples: "See each entry"
state: draft
no_figure: "This is a log of what happened on a range day, and the figures the sheets produced are given in it. The developer's standing consent of 2026-09-24 now allows those range sheets to be shown, and no figure has been drawn from them yet."
found: "see the article"
sure: "see the article"
data:
  - data/range-day-2026-09-20.csv
---

## Print tests, September 2026

**Question:** does a GroupLab sheet print at its true size, and can GroupLab detect when it does not?

**What was done:** test sheets printed at 100 percent and deliberately at 96.2 percent, scanned at 300 and 600 dpi.

**What we learned:** the developer's printer at "Actual size" measured 1.0001 horizontally and 1.0006 vertically. GroupLab measured the reduced sheet at 0.96200 (600 dpi) and 0.96201 (300 dpi).

**Written up in:** "Does your printer print at true size?", "Scanner traps".

## First range sheet

**Question:** does a real, shot sheet read the way generated test sheets do?

**What was done:** nine shots on a 25-bull letter sheet, scanned flat.

**What we learned:** this first real sheet started the work on how GroupLab reads holes in real paper, rather than in simulated targets.

## Range day, 2026-09-20

**Question:** how does GroupLab do across calibers, with shots that land between bulls, with a blank zero sheet, and with phone photos against scans of the same sheets?

**What was done:** five 25-bull load development sheets (GL-CF25-LTR-D), one blank zero sheet and one commercial target, all at 100 yards. Every sheet was scanned at 600 dpi and photographed with a phone in several bursts through the afternoon, 59 photos in all. A second phone photographed the 6mm Creedmoor sheet.

| Sheet | Cartridge | Load as written | Shots | What it tests |
|---|---|---|---|---|
| 1 | 6.5 Creedmoor, 28 in Seekins | 153.5 gr LRHT, 42.4 gr H4350, Alpha brass, 7.5 BR primer, 2.874 in | 15 | One shot per bull, rows 1 to 3 |
| 2 | 6.5 Creedmoor | Zero group | | A blank sheet with a hand-drawn cross |
| 3 | 6.5 Creedmoor | Same as sheet 1 | 25 | A full sheet; now GroupLab's sample scan |
| 4 | .22 LR, 20 in CZ | SK Standard Plus, 40 gr | 23 | Strong crosswind, windage changed after row 2 |
| 5 | 6 ARC, 18 in RTR | 108 gr ELD-M, 27 gr N140, Starline brass, GM205MAR, 2.250 in | 20 | A load the rifle was not zeroed for: shots land nearer the wrong bull |
| 6 | 6mm Creedmoor, AI AXSR | 120 gr LRHT, 39.0 gr N550, Lapua brass, 2.810 in | 10 | Five shots with GM205MAR primers, five with CCI BR-4 |

**What we learned:**

- GroupLab's counts matched the shooter's on every scanned load sheet, after two detection defects found on these sheets were fixed.
- Assigning each hole to its nearest bull gives confident but wrong groups when shots land between bulls (sheets 4 and 5). GroupLab now lets you say which bulls you aimed at.
- Switching primers on sheet 6 moved the point of impact clearly, while the difference in spread could not be shown from five shots each.
- Photos place a sheet almost as precisely as scans, but hole sizes in photos depend on the light.
- The original bull was hard to see through lower magnification and less expensive glass.

**Written up in:** "Why a photo cannot tell you your bullet's size", "A bullet hole is not the bullet", "Did the primer matter?", "When shots land on the wrong bull", "Scans against phone photos", "Wind or rifle?", "Zeroing on a blank sheet", "Pooling groups", "How to photograph a target", "Scanner traps", "Can you see the bull?".

## Range day and aim point test, 2026-09-26

**Questions:** does swapping suppressors move the point of impact, which aim point designs can two shooters center on through four scopes, and how does a real 25 shot sheet fare when every shot lands nearer the wrong bull?

**What was done:** three 25 bull sheets at 100 yards on an OSB backer, and the aim point card scored by the developer and his friend Justin.

| Sheet | Cartridge | Load as written | Shots | What it tests |
|---|---|---|---|---|
| 7 | 6 ARC, 18 in AR-15 | 105 gr Aeromatch, 24.2 gr N135, Starline brass, GM205MAR, 2.250 in, Thunder Beast Dominus K | 25 | The first suppressor, shot first |
| 8 | 6 ARC, 18 in AR-15 | The same load, Magnus S | 25 | The second suppressor, 20 to 30 minutes later |
| 9 | 6.5 Creedmoor | 153.5 gr LRHT, 42.4 gr H4350, Alpha SRP brass, GM205MAR, 2.873 in, Magnus S | 25 | Every shot about 0.9 in high: nearer the bull above than its own |

The 6.5 load is not the one of sheets 1 and 3: those were written with a 7.5 BR primer and 2.874 in, so sheet 9 is not pooled with them.

**What we learned:**

- With the Magnus S the 6 ARC shots centered 0.28 in lower than with the Dominus K, p = 0.049, and the spread did not change. The order the sheets were shot in cannot be separated from the suppressor.
- At 10x the current bull could not be centered through any of the three high power scopes. Designs whose center subtends about 3.4 arcminutes or more could. Through the PLxC at 4x only the 2 inch bull could.
- A fine crosshair reticle covers a design whose center is a small feature.
- On sheet 9 GroupLab found 23 of 25 holes and gave 5 of them to the bull above their own.
- Three sheets printed from one PDF carry one design identifier; the developer told them apart by writing K, M and C in the serial box.

**Written up in:** "Did the suppressor move the point of impact?", "Can you see the bull?", "Aim points for 1x to high power optics".

## Aim point test, planned for 2026-09-23

**Question:** which aim point designs can shooters see and center on at 100 yards, across scopes of different magnification and glass?

**What was done:** a card of nine designs at 100 yards, scored through a Vortex Razor HD Gen III 6-36x56, a DNT TheOne 7-35x56, a Vortex Strike Eagle 5-25x56 and a Primary Arms PLxC 1-8x24, plus a friend's scope, by two observers.

**What we learned:** it was shot on 2026-09-26 instead; the results are in the section above.

**Written up in:** "Can you see the bull?", "Aim points for 1x to high power optics".

## Next

- Aim point tests for 1x red dots and prisms, low power and medium power variables, at distances suited to each.
- More sheets per load, so loads can be compared with enough shots to mean something (see "How many shots do you need?").

## Data

- `data/range-day-2026-09-20.csv`: the six range-day sheets, cartridges, loads as written, shots and notes.
- The 2026-09-26 sheets: `suppressor-shift/data/offsets-2026-09-26.csv`, and the aim point scores and groups in `can-you-see-the-bull/data/`.

## What this means

**This page is evidence, not advice.** It exists so that the numbers quoted elsewhere on this site can be traced to a day, a rifle and a sheet, rather than appearing as figures from nowhere.

**What to take from it:** how much goes wrong on an ordinary range day. Sheets that could not be read, a scanner that cropped without saying so, a zero that was not what it was assumed to be. Every one of those became a rule somewhere else in this project, and none of them would have been found by reasoning.

**If you are running your own test**, the transferable part is writing it down at the bench. Nearly every difficulty here was recoverable only because something was recorded at the time.
