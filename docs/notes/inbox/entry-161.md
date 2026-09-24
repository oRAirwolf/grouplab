# 2026-09-24, entry 161: naming the calibre makes the reading worse, and the hole to calibre constant is wrong

Alan's friend shot `Scan_20260923.png` on 2026-09-23: **ten shots, one per bull on bulls 1 to 10,
6.5 Creedmoor, so 0.264 in.** That is the ground truth for everything below. The scan is on Alan's
machine at `C:\Users\Airwolf\Downloads\Scan_20260923.png`. It is a friend's target: use it as a local
fixture only and publish nothing from it until Alan has a consent record for it, the same standing rule
as the 2026-09-16 friend scan.

He loaded it into nightly 93 and it read the holes as .308. He set the calibre to .264, which is the
truth, **and the reading got substantially worse.** Software that gets worse when it is told the truth
has the wrong model in it, and this entry is about finding it rather than about tuning a threshold.

## 1. What the two runs reported

| | no calibre named | calibre named as .264 |
|---|---|---|
| markers found | 38 of 38 | 38 of 38 |
| registration RMS | 0.0035 in | 0.0035 in |
| holes detected | 10 | 10 |
| assignment | one per bull, bulls 1 to 10 | one per bull, bulls 1 to 10 |
| size reference used | 0.284 in, from the sheet's own marks | 0.249 in, from 0.264 times the constant |
| shot 6 size | 1.12 holes | 1.45 holes |
| shot 4 size | 1.12 holes | 1.45 holes |
| shot 3 size | 1.11 holes | 1.44 holes |
| review items | 1 | 6 |

**The detection is perfect in both runs.** Ten holes, one per bull, bulls 1 to 10, exactly the ground
truth. Nothing is missed and nothing is invented. Every one of those six review items is a false alarm,
and so is the one in the first run.

## 2. The measurement that settles it

The application's own panel reports: **ten holes measuring 0.301 in across the middle.**

- Bullet diameter: 0.264 in.
- Measured hole: 0.301 in.
- **Hole divided by calibre: 1.140.**

`AutomaticMarking.HoleToCalibre` is 0.945, that is, a hole is assumed to measure less than the bullet
because the paper closes behind it. On this sheet the hole measures 14 percent **more** than the
bullet. The constant is wrong by 21 percent in diameter on this material, and because the doubles test
works in area, by 46 percent in area:

    (0.301 / 0.249)^2 = 1.46

which is the 1.45 the screen shows. The arithmetic of the false alarms is fully explained by the
constant. Confirm it rather than taking it from me.

## 3. Why naming the calibre makes it worse, which is the real defect

Without a calibre, the size reference comes from the sheet's own marks: 0.284 in, and the ten holes sit
within a few percent of it, so only one is flagged. **With a calibre named, the measured reference is
discarded and replaced by 0.945 times the calibre.** Every hole on the sheet is then 46 percent
oversized by definition, and five more shots get flagged.

Entry 141 section 4.2 already decided this: the sheet's own reference is taken from the marks that
agree with each other. The calibre path does not honour it. This is the fix:

1. **A named calibre never replaces the sheet's own measured reference for the doubles test.** The
   marks on the sheet are a measurement; the calibre is a fact about the bullet, and the relationship
   between the two is the thing that is not known.
2. **Where the two disagree**, say so, in the scale panel, in plain words: "these holes measure 0.301 in
   and a 0.264 bullet would be expected to make about 0.249; the sheet's own marks are being used."
   A disagreement that large is information the shooter wants, and it is also how this defect would
   have been caught.
3. The calibre still does the work only it can do: naming the bullet for the record, for the ballistics
   screen, and for any comparison between sheets.

Check this against question 40 as well, which is about the same reference, and make sure the two
answers agree.

## 4. The calibre guess should stop naming a cartridge

Told nothing, the application guessed **.308, and offered .312 beside it.** The truth was .264. That is
an error of 0.044 in, and both offered options were wrong.

The guess inverts the same constant, so it fails the same way. Until section 5 has measured what the
ratio actually depends on, the guess should not name a specific calibre at all. Replace it with the
measurement and the question:

> These ten holes measure 0.301 in across. A hole is not the bullet: the reading moves with the paper,
> the backing and how fast the bullet was going, and on this sheet it is larger than the bullet.
> Name what you fired.

That is honest, it is still useful, and it follows the rule question 37 settled: a fact the shooter
knows does not become a guess the software makes.

## 5. What this does to the research

This is a measurement for entry 158 program B and it should be recorded as one.

- Earlier scans measured hole over calibre between 0.76 and 0.95.
- This scan measures 1.140, on a 6.5 Creedmoor at about 2845 fps.

**So the ratio is not a constant and is not even on one side of 1.** That is a stronger result than the
photograph finding of question 38, because both of these are scans, so imaging does not explain it.
Write it into `docs/PHASE1-RESULTS.md` with the numbers, and it is very likely the material for the
article entry 158 program B was told to hold back until the data could support one. Assess it under
entry 158 section 1.

Do not replace 0.945 with 1.140. Replacing one wrong constant with another wrong constant is the
mistake this whole finding argues against. The reference is the sheet.

## 6. The print scale, and Alan is right

The friend printed with no scaling and the application reported 100.3 percent.

Alan: "I dont think this needs correction because the apriltags are the source of truth."

Confirm and say so plainly in the report, because entry 152 is asking the same question. The markers
solved at 38 of 38 with a registration RMS of 0.0035 in, and a uniform print scale error moves the
markers with everything else, so a hole measured against the markers is measured correctly. The only
residual is that a figure reported in sheet coordinates is 0.3 percent smaller than the physical
target, which on a one inch group is 0.003 in. Say whether the application reports sheet coordinates or
corrects to physical inches, because entry 152 has to answer that anyway.

## 7. The rounds fired question

The remaining review item, "Count differs from rounds fired", exists because nobody said how many
rounds were fired. Ten in the "Rounds fired at the group" field resolves it. That is working as
designed. Nothing to change, but check that the panel makes it obvious that filling the field is the
answer.

## 8. Tests

1. `Scan_20260923.png` becomes a local fixture with its ground truth: ten shots, one per bull, bulls 1
   to 10, 0.264 in. Unpublished, and not added to `samples/` until there is a consent record.
2. A test that, with the calibre named correctly, the sheet produces **no** "possibly two holes" items.
   That is the regression this entry exists to prevent.
3. A test that naming the correct calibre never increases the number of review items on any fixture.
   That is the general form of the fault and it is worth holding forever.
4. Re-run the six earlier scans both ways, with and without their calibre named, and report the review
   item counts in a table. If naming the calibre makes any of them worse, that is the same defect and
   it has been there all along.
