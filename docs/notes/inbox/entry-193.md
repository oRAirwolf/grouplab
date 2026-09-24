## 2026-09-24, entry 193: on nightly 99 the zeroing grid is found, but no shots are

Read with entry 191, which it updates. Unholy, after updating from nightly 95 to nightly 99:

> "looks like it detected the grid now that I updated to nightly.99. It clearly doesn't know how to handle a zeroing grid. It detected
> the bull but 0 shots"

So:

1. **The sheet not being found was nightly 95.** Something between 95 and 99 fixed it. Confirm with his scan
   (`C:\Dev\grouplab-submissions\unholy\2026-09-24_zeroing-grid-mil-100yd.png`, per entry 191) that 95 fails and 99 finds the sheet, and
   name the commit that changed it. No fix needed for that part, but the test from entry 191 still goes in so it cannot come back.
2. **The real defect now: the bull is found and zero shots are.** Work out why, in plain words. Likely places to look, not conclusions:
   the grid lines and numbers being masked out along with the holes, holes that sit on or cross grid lines, the expected hole size
   (was a caliber set?), or the zeroing grid's definition not declaring where shots may land. Say which it was.
3. Fix it so the shots on his scan are found, with a count Alan or Unholy can check by eye, and add the scan as a test with that count.
   Check the other three zeroing grids (MOA and mil, 100 yd and 100 m) the same way from renders with synthetic holes on and across lines.
4. When a sheet is found and no shots are, the application should never show a blank result. It should say it found the sheet and no
   holes, and offer marking by hand, with the likely reason if it knows one.

Report in plain words for Alan: how many shots it now finds on Unholy's scan, and whether the zeroing grids work in the next build.
