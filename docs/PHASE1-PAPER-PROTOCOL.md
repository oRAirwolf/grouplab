# Phase 1 paper session

**Everything physical the project needs, in one list.** Nothing else is being asked for before this, and nothing has been held back to ask for later.

Split into three sittings so the range day stays short. Only part B needs the range, and part A can be done any evening.

- **Part A, at home, before you go.** Print everything, scan what never gets shot, scan the sheets you are about to shoot. About 45 minutes.
- **Part B, at the range.** Shoot two sheets, photograph them mounted before you take them down. About 15 minutes on top of a normal session.
- **Part C, at home, after.** Scan the shot sheets, drop the files in folders. About 20 minutes.

**About 53 rounds.** Two sheets, one shot per scoring bull, plus three sighters on S2. A third sheet is optional and only if you want a second calibre in the set.

---

## Part A: at home, before you go

### A1. Print the pack

**Print it from the application, not from a command.** The print screen now exists, it lists all twenty-two built-in sheets, and it drives the printer with scaling disabled rather than asking you to remember. **This is the first time a human has used it**, so it is a test as much as a task. If it fails or confuses you, note what happened and fall back to the command below; that report is worth as much as the paper.

Print **three copies of `GL-CF25-LTR`**. Print **S1 with the load data block filled in** with your real load before you shoot it, and **S2 and S3 with a blank block** to write at the range. Both paths need exercising and this is the natural moment.

The five marker sweep sheets are not in the built-in library, so those still come from the command line:

> Print the five marker module sweep sheets from `scans/phase1/module-sweep/` as PDFs, one page each, Letter, and tell me the identifier printed on each sheet.

**Eight pages.** Three target sheets and five sweep sheets.

**Print settings, and the one that matters.** Brother MFC-J430W, plain paper, Normal quality, portrait, and **scale set to 100 percent with "fit to page" and "shrink to fit" both off**. Everything else in this session is worthless if the scale is wrong, and a printer driver will silently shrink a page to its own margins given the chance. These are the settings from the Phase 0 protocol, unchanged, so the new sheets stay comparable with the old ones.

**Check one sheet before printing the rest.** Measure between two bull centres across the widest span with the calipers. It should be five grid pitches. If it is short by a percent or two, scaling is on somewhere.

### A2. Label them, briefly

One code per sheet, small, in pen, in the bottom margin. Nothing else. The filename carries everything else.

| Code | Sheet | What happens to it |
|---|---|---|
| `S1` | GL-CF25-LTR | Shot normally |
| `S2` | GL-CF25-LTR | Shot deliberately awkwardly |
| `S3` | GL-CF25-LTR | Optional, second calibre |
| `C1` | GL-CF25-LTR | Never shot. Flat control |
| `M03` `M04` `M05` `M06` `M08` | Sweep sheets | Never shot. Scanned only |

Last time labelling and scanning cost you an hour. This is one code per sheet and the codes are two characters.

### A3. Scan the five sweep sheets

600 DPI, colour, no auto-crop, no descreen, no sharpening. Save as `scans/phase1/module-sweep/scans/M03-600.jpg` and so on.

These never go to the range. They exist to find the smallest marker your printer can lay down cleanly, which is the one measurement nobody can make without your actual printer.

### A4. Scan S1, S2 and S3 **before** you shoot them

Same settings. Save as `scans/phase1/paper/S1-before-600.jpg` and so on.

**This is the step most worth not skipping.** Your printer puts ink about five hundredths of a millimetre from where it is asked, and that error travels with the paper rather than with the printer. Comparing a shot sheet against a scan of that same sheet removes it completely. Comparing against the ideal design does not. Ten minutes of scanner time buys a cleaner answer than any amount of software.

### A5. The flat control, C1

Lay C1 flat on a table indoors. Photograph it square on, whole sheet, all four edges in frame with a bit of margin, in decent even light. Two or three frames is plenty.

Then scan it, `C1-600.jpg`.

This is the only sheet in the set printed from the corrected geometry and never shot, and it answers a question that is currently open: whether the sighter row fix actually makes the flat photograph gate pass. Right now that is recorded as not measured.

**Take one of these frames carefully.** Square on, well lit, whole sheet, nothing clipped. That one gets used as the "yes, like this" example on the upload page, replacing the drawing I made because no such photo exists anywhere in the project.

---

## Part B: at the range

### B1. Shoot S1 normally

Your real load, the way you would actually shoot it. **25 rounds, one per scoring bull. Leave the three sighter bulls empty.**

That sentence is new and it matters. `GL-CF25-LTR` carries 25 scoring bulls plus 3 sighters, and the original text just said "one per bull", which is ambiguous on a sheet that has both.

### B2. Shoot S2 deliberately awkwardly

**This is the more valuable sheet and the easier one to shoot.** A second sheet of well-centred groups teaches the software nothing the first one did not. Spread the holes on purpose:

- Several on bare paper between bulls, well away from any printing
- Several cutting through a printed ring stroke
- A couple on or touching a printed numeral
- **At least one pair overlapping inside a single bull**, close enough that the two tears merge
- **At least one shot landing in a neighbouring bull's cell**
- **Three rounds into the three sighter bulls**

That last one is new and it is deliberate. A bug found on 15 September was counting shots on sighter bulls as part of the measured group, which inflates the result silently and produces a plausible wrong answer. It is fixed, and it has only ever been tested against a synthetic sheet. Three rounds gives it a real one.

28 rounds on this sheet: 25 scoring plus 3 sighters. Do not try to make it neat.

### B3. Optional: S3, a second calibre

Only if you want hole diameter variety in the set. Hole size tracks bullet diameter, so .308 on one sheet and 6.5 mm on another spans the range. Skip it without guilt.

### B4. Photograph each shot sheet while it is still mounted

**Before you take anything down.** This cannot be recreated afterwards, and it is the case the project has never had: a whole GroupLab sheet, with its markers, still fixed where it was shot.

Four frames per sheet, thirty seconds each:

1. Square on, from wherever you would normally stand to take the photo
2. Deliberately off to one side, a steep angle
3. Further back, so the sheet is smaller in the frame. **Walk backwards. Do not pinch to zoom**, because zooming crops the sensor and changes the lens geometry the software has to undo, and on some phones the metadata does not record that it happened
4. Whatever the light is doing worst. Your shadow across it, or sun glare, or deep shade

Take them **however you normally photograph a target**. Do not try to take good photographs. Then take one extra frame of each sheet with the main camera specifically, so there is something directly comparable with the Phase 0 set.

Whole sheet, all four edges in frame, every time. A close-up of the group is the one thing that cannot be used.

### B5. Write down what is behind the paper

One line, in your notes or a photograph of the backer itself: what each sheet was stapled to. Cardboard, corrugated plastic, OSB, and what colour.

A measurement in September found that a hole photographed against a dark backer reads about 0.023 in off from the same hole scanned against the white scanner lid, which is nearly three times the noise floor. That makes the backer a term in the error budget rather than a detail, and right now it has been measured once, on one sheet, against one mat.

### B6. Two marks in pen, on S2 only

After shooting, before it comes down. Thirty seconds:

- Draw an **X** over one hole, the way you would to exclude it
- Draw an **arrow** from a bull to the shot that landed in a neighbour's cell

Real targets in the sample set carry both, drawn by whoever shot them, and the software has to cope. This is the first time either will exist on a sheet whose geometry is known exactly.

---

## Part C: at home, after

### C1. Scan the shot sheets

600 DPI, same settings as before. `S1-after-600.jpg`, `S2-after-600.jpg`, `S3-after-600.jpg`.

If a sheet is torn or curled, scan it anyway and do not flatten it first. A damaged sheet is a real case.

### C2. Put everything in a staging folder **outside** the repository, and tell me the total size

```
C:\Dev\grouplab-session-2026-09-20\paper\        S1/S2/S3 before and after, C1
C:\Dev\grouplab-session-2026-09-20\mounted\      the range photographs
C:\Dev\grouplab-session-2026-09-20\sweep\        M03 to M08
```

**Do not commit them yet.** This changed on 15 September. Eight scans at 600 DPI plus a dozen phone photographs is plausibly three hundred megabytes, and the repository was deliberately rebuilt that week to remove weight and is now public. Where each set belongs is a decision worth thirty seconds rather than a surprise:

- Sheets that a gate reads by path belong in the code repository.
- Mounted photographs are closer to the donated corpus and probably belong in `grouplab-testdata` under `owner/`.

Run `Get-ChildItem <folder> -Recurse | Measure-Object Length -Sum` and send me the number. I will tell you where each set goes, and Claude Code moves them in one commit.

### C3. Tell me three things

1. Anything that went wrong or felt stupid. Especially if a step was more work than it looked.
2. How the scale check in A1 came out.
3. Whether the printer produced anything visibly rough on the 0.3 mm sweep sheet, which is the one most likely to be past what it can do.
4. **How the print screen behaved.** It had never been used by a person before this session.

**One thing that is different from last time.** As of 15 September the whole path runs as a single command, `grouplab analyze`, so a scan can go in and a group size can come out the same day. You are no longer waiting on integration work before your paper turns into a number.

---

## What this produces, and why each piece is here

| Item | Answers |
|---|---|
| S1, before and after | Hole detection on known geometry, against a scan of the same physical sheet |
| S2, before and after | The hard cases: overlapping holes, holes on ink, cross-cell shots, pen marks |
| Mounted photographs | The mounted gate, which currently fails on nine frames of one sheet hanging from a single pin. This is the first realistic sample |
| C1, flat and scanned | Whether the corrected sighter geometry passes the flat gate. Currently unmeasured |
| One careful C1 frame | Replaces the drawn example on the upload page |
| Sweep sheets M03 to M08 | The smallest marker your printer can lay down, which no literature can supply |

**Nothing here is being kept back for a second session.** If something in this list turns out to be wrong or missing, that is my error rather than a planned follow-up.
