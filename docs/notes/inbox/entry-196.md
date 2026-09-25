## 2026-09-25, entry 196: a zeroing grid with a group on it, touching holes, and one ragged hole

Alan asked whether the zeroing grid finds more than one shot, and shots that touch. The planning session read the code and tests. What it
found, then what to do. Do this after entry 195.

## 1. What is true today, as read from the repository

1. **Detection of several shots:** `EverySheetDetectsTests.HolesOnAZeroingGridsLinesAreFound` puts five separate holes on and across the
   lines of each of the four zeroing grids, and all five are found. Synthetic only; the one real scan (Unholy's) has one shot.
2. **Assignment of several shots:** a zeroing grid has one scoring bull, and nothing in its definition or the default `AssignmentRule` says
   that bull takes more than one shot. With more shots than bulls, `ShotAssignment` gives each shot to its nearest bull within the gate and
   **flags every shot**, and `ReviewQueue` then raises "Bull 1 holds 5 shots ... The sheet expects one a bull". So a normal five-shot zeroing
   group arrives with a review item on every shot, which is the friction Unholy has been reporting. A shot farther than the gate from the
   one bull, which on a zeroing grid is exactly the first shot of a rifle that is far off, may be left unassigned. Say what the gate is
   on a zeroing grid and whether the grid's whole area is inside it.
3. **Two touching holes:** split by shape (`CalibreSplitTests`), with a named caliber stopping false splits; one left whole is flagged
   oversized with a Two shots choice. Not tested where the pair sits on or across a grid line.
4. **Three or more through one ragged hole:** stays one mark, flagged oversized; the review offers One shot, Two shots or Not a shot, so a
   person must add the third shot by hand.

If any of this is wrong, say so in the report; it was read from code, not run.

## 2. What to do

1. **A sheet with one scoring bull expects a group on it.** For a definition with exactly one scoring bull, and for any sheet with a grid,
   the default is every shot to that bull with no limit: no "holds N shots" item, no flag on every shot for having more shots than bulls,
   and a gate that covers the whole printed grid (or the page), so a far-off first shot is still that bull's. Only real doubts (a mark
   that may be two, a candidate refused) reach the review. Consider whether the definition format should say this explicitly, for
   example a per-bull expected count, rather than inferring it from the count of bulls; your call, with the reason.
2. **Tests on all four zeroing grids,** from renders with synthetic holes, counted exactly:
   - a five-shot group about 1 in across, including one touching pair, and a pair straddling a line;
   - one shot 2.5 in from the aim point, at the grid's edge;
   - a three-shot ragged hole, which must at least be flagged as more than one shot.
   Each asserts the count found, that every shot is on the one bull, and that the review holds only the items section 2.1 allows.
3. **The ragged hole review:** where the mark's area holds about three holes of the named caliber, offer Three shots as well, placed from
   the mark's shape the way Two shots already is, or say in the report why that cannot be placed honestly and what a person does instead.
   Never guess a count without a caliber; say it needs one.
4. **A request for Alan, optional:** a real zeroing grid with a five-shot group and at least one touching pair, scanned at 600 dpi, as the
   first real test. One line in for-alan.md, no deadline.

## 3. The report

Plain words for Alan: what a five-shot zeroing group now looks like after detection (how many review items, if any), and what happens
with touching holes and a ragged hole.
