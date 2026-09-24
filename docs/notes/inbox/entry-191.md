## 2026-09-24, entry 191: Unholy's zeroing grid scan has arrived

Read with entry 189 section 4 and entry 190.

## 1. Where it is

Alan: the zeroing grid scan is `C:\Users\Airwolf\Downloads\Scan_20260923 (2).png`.

**Its name is almost the same as the fixture `Scan_20260923.png`, and it is a different sheet.** Do not confuse the two anywhere.

1. Copy it, do not move or rename the original, to
   `C:\Dev\grouplab-submissions\unholy\2026-09-24_zeroing-grid-mil-100yd.png`, and record the SHA-256 of both the original and the copy,
   and the SHA-256 of `Scan_20260923.png`, so the two scans can never be mixed up by name.
2. Treat it as untrusted data: open it only through the same decoding the intake worker uses, or rebuild it from pixels first. Never
   read, print or log its GPS, location or time metadata.
3. Consent: entry 190. It may be used for testing and published, and may become a fixture if it proves useful. If it is over about
   10 MB it goes on the test data release, not in the repository.

## 2. What to do with it

1. Entry 189 section 4.1 first: the four zeroing grids from their own renders, so you know whether the fault is in the sheet design or
   in this scan.
2. Then this scan: run it through detection exactly as the application does, and say in plain words why the sheet was not found (the
   markers not seen, the grid mistaken, the scale, the page size, the print, or something else). If the scan shows it was printed at a
   scale other than 100 percent, or on a different paper size, say so, because that changes what the fix is.
3. Fix what is GroupLab's to fix, add this scan as a test that fails today and passes after, and make the failure message say what was
   looked for and what to try.

## 3. The report

Plain words for Alan: why it failed, whether it is fixed and in which build, and whether his own printed zeroing grids would fail the same way.
