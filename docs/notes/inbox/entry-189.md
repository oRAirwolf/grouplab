## 2026-09-24, entry 189: Unholy's feedback: caliber, figures at 100 yards, the zeroing grid, the scale, the caliber box

Do this after entries 187 and 188. All of it is from the outside tester whose scan is `Scan_20260923.png`, who goes by **Unholy**.
Alan: "This is from Unholy and should be credited to him." Screenshots were seen by the planning session; the facts are below.

## 1. Credit, and the thanks list

Request 16 asked whether the project wants a list of testers. It does now: Alan wants Unholy credited, and the macOS tester is
"Fenix". Add a short **Thanks** section to README.md naming testers by the name they gave, with one plain line each on what they
tested. No other names are invented; add one only when Alan gives it. Close that half of request 16. Wherever the log or a release
note says "the friend", "the outside user" or "TNA" for this person's feedback, user-facing text says Unholy from now on;
internal logs need not be rewritten.

## 2. "Calibre" is still in the application

The Setup panel's label reads **Calibre** with "needed" beside it. `scripts/american-spelling.py` did not catch it because of its own
rule: a string literal with no space is treated as a key and never checked. `"Calibre"` in `MainWindow.cs` (the `Needed(...)` and
`Readout(...)` calls) and in `MainWindow.Report.cs` is a one-word label, not a key.

1. Fix every one-word label a user reads: Calibre, Centre, Colour, Analyse and any other the word list has.
2. Close the gap so it cannot recur. For example, check one-word literals too when they are the label argument of the helpers that
   put text on screen (`Needed`, `Readout`, `ReportFigure`, headings, buttons, tooltips), or check every literal whose whole text is a
   word on the British list and require "British on purpose" on the few that are real keys. Choose whichever you can make exact, and
   add a test with `"Calibre"` as a label that fails on today's code.
3. Entry 187 section 8 asked to fix the existing lines in `docs/RELEASE-NOTES.md`. The script's header says a published release's
   notes are never edited. The header wins: leave past notes as they are and make sure new ones are checked. Entry numbers still
   come out of new user-facing notes.

## 3. Show the group as an angle at 100 yards first, the measured size second

Unholy: "if the target is not at 100 yards it should be giving data corrected for 100 yards that most people are familiar with."
His example group, shot at about 25 yd: extreme spread 0.422 in, which he calls "about 1.66 MOA at 100 yards", and the same for
mean radius and CEP. He would make that the main number, with the "raw size" in smaller text beneath.

Two points about his arithmetic, so the change is right:

- 0.422 in x 100 / 25.4 = 1.66 is **inches at 100 yards**, which is GroupLab's **SMOA**. True MOA at 25.4 yd is about 1.59. Both are
  in common use and GroupLab already has both units, so this is a display change, not new math.
- An angle is what makes groups at different distances comparable. That is his point, and it is right.

What to build:

1. When the shot distance is known, every size in the Group panel (extreme spread, width and height, mean radius, CEP 50 and 90, and
   center from aim) shows the angular figure first, large, in the user's chosen angular unit, and the measured size on the target
   beneath it, smaller, labeled so it is clear it is the size on the paper at the distance shot. Same order in the report and in
   Compare.
2. Default angular unit stays MOA. SMOA is the choice for people who think in inches at 100 yards; say that in its tooltip in the
   glossary's words, with his example worked in both units.
3. When no distance is set, show the measured size as now, and say in the panel that an angle needs the distance, with the
   distance box one click away. Never guess the distance.
4. A setting that puts the measured size first, for people who shoot one distance only. Default off.
5. Mean radius keeps its emphasis as the headline figure, whatever unit leads.

## 4. GroupLab could not find its own "Zeroing Grid, mil at 100 yd"

Unholy: GroupLab "was unable to detect the Zeroing Grid, mil at 100yd target and was unable to analyze it." That is
`GL-ZERO-MIL-100Y`, one of GroupLab's own printed sheets, so this is a defect, not a limit.

1. Before any photograph arrives, check what can be checked: render all four zeroing grids (mil and MOA, 100 yd and 100 m) and run
   each through sheet detection and analysis end to end. If a test for every library sheet does not exist, write it: every sheet in
   the library must detect from its own render. Report which of the four fail and why.
2. If they all pass from a render, the fault is in his photograph or print, and Alan is asking Unholy for the image. It arrives as a
   new submission or through Alan; treat it as untrusted data like any submission, and do not publish it, because Unholy's consent
   covers `Scan_20260923.png` only.
3. When the sheet is not found, the message should say what was looked for and what to try, not only that it failed.

## 5. A scale set by hand cannot be changed

Unholy: "if doing a manual target, after setting the scale manually you are unable to change it unless you close and relaunch the
program." Reproduce it: set the scale by hand on a target that is not a GroupLab sheet, then try to set it again. Setting the scale
again must work at any time, and changing it must recompute every figure. Add a test.

## 6. The caliber box needs two clicks

Unholy: "When choosing caliber if I type '6.5' it shows 6.5 creed in the list but if I click it, the text in the box doesn't change
and I have to click 'set' twice for it to actually set it." The `AutoCompleteBox` (`calibreBox`, `FilterMode None`) is not taking the
clicked suggestion into its text, so the first Set reads the typed "6.5".

1. Clicking a suggestion, or Enter or Tab on a highlighted one, puts it in the box and sets it. One action, no Set needed after a
   suggestion is chosen; Set stays for a typed caliber that is not in the list.
2. Test it through the UI test harness the App tests use, including typing "6.5" and clicking "6.5 Creedmoor".
3. Rename the field and its label to caliber in anything a user reads (section 2).

## 7. The report

Plain words for Alan: which of these are fixed and in which build, which wait on Unholy's photograph, and whether all four
zeroing grids detect from their own renders.
