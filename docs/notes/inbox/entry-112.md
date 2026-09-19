## 2026-09-19, entry 112: session records, the report, the target library, and the solver on screen

**Status: open.**

**Alan is at the range on 20 September, and nothing here waits on what he brings back.** This is the largest stretch of Phase 4 still marked Not started, plus the payoff of Phase 5's solver. **It is more than one run.** Work in the order of the sections, finish each one to a clean state with its tests, and say in the status line where you stopped. If a section raises a design question this entry does not answer, raise it in `docs/QUESTIONS-FOR-PLANNING.md`, build what does not depend on the answer, and move on.

### 1. Session records, and the storage `DESIGN.md` section 15 names

**Today an analysis lives only as a marking file the person saves somewhere, and nothing lists them.** The rail's Session records destination still says it is not built.

**Storage.** `DESIGN.md` section 15 says: "Storage is SQLite with a documented schema and full JSON export." Nothing uses SQLite yet, and the rifles, barrels and loads live in a JSON file beside the settings. **This is the moment to follow section 15**, before sessions start accumulating in some other form:
- **One SQLite database in the application's data folder**, holding sessions and the rifle, barrel and load records. The existing record file migrates into it on first run and is kept as a backup, not deleted.
- **A documented schema**, as a document in `docs/`, with its version number. A test holds the document to the schema the code creates.
- **Full JSON export** of everything, and import of that export, with a test that the two round-trip exactly.
- **Section 18's three tiers.** Geometry is always kept. A proof image of about 150 dpi is kept by default. The full-resolution original stays where the person has it, and the session stores its path and its hash, not a copy. No image is ever required to reopen and read a session, because the sheet re-renders from its definition.
- **Section 15's chronograph rule is the reason the schema matters now:** "the shot sequence and the chronograph sequence are separate ordered lists that get reconciled, never assumed to align." **Leave room for both lists and a mapping between them in the schema now**, even though Xero import is Phase 5, so adding it does not require a migration of every session.
- **The library:** `Microsoft.Data.Sqlite` is MIT and SQLite itself is public domain. Note both in `THIRD-PARTY-NOTICES.md`. If you prefer a different binding, say why.

**What a session is.** One analysed sheet with its date, distance, rifle, barrel, load and calibre, the marking with every edit and exclusion, the figures as computed, and the proof image. **Accept and analyse saves it.** Reopening it returns to the analysis state exactly as it was.

**The Session records screen**, the rail's destination. A list, newest first: date, sheet name, rifle, load, distance, shot count, mean radius with its interval. Filter by rifle and by load. Open one to its analysis. Delete asks first. The concept images do not draw this screen, so follow the concept's language: the table style of the shot list, the row and hairline pattern from entry 109, and no new visual devices.

### 2. The report

**The Report button on the analysis screen is in the concept and not built.** A report is a PDF of one session, drawn by GroupLab's own PDF writer, for printing or sending to someone.

**Page one, the result:**
- sheet, date, distance, rifle, barrel, load and calibre;
- the composite plot;
- the headline figures, **each with its interval**;
- the zero correction and its verdict;
- the two judgement cards, verdict and test.

**Page two, the evidence:**
- the shot table with bulls;
- every exclusion **with its reason**;
- any decision left unmade, stated as the analysis screen states it;
- the registration's quality;
- the "why" text of each figure and card, which fits on paper where it did not fit on screen;
- GroupLab's version and the sheet's identifier.

**Two rules the report must keep:**
- **Exclusions are never hidden.** `docs/STATISTICS.md` section 10: "every report prints the full and reduced figures side by side so an exclusion can never be hidden." When anything is excluded, every figure appears twice, with and without. Test it.
- **Nothing on paper is more certain than the screen.** Every interval, hedge and power statement the screen carries goes into the report. The stringing power statement in particular stays beside its result.

**A test** renders a report for a session with an exclusion and one without, and checks that the text it contains is what the rules require.

### 3. The target library

**The rail's Target library destination.** `docs/figures/screens/library-and-print.png` is the concept.
- The built-in sheets, read only, and the person's own sheets from the parametric editor, saved as GLTD files in the application's data folder.
- A person's sheet can be renamed, duplicated as the start of a new one, and deleted after asking.
- The print screen lists both, and a person's sheet prints exactly as a built-in one does, through the same refusals.
- **A sheet a session was analysed against must stay readable.** Deleting a person's sheet that a session uses either refuses with the reason or keeps the definition inside the session. Say which you chose and why.

### 4. The solver on screen

**Phase 5's solver is validated and invisible.** `DESIGN.md` section 3 and entry 110 section 2g name what it is for. The first two uses are ready now:
- **The records gain what the solver needs**, all optional:
  - on the rifle: sight height and zero distance;
  - on the load: muzzle velocity, BC, drag model G1 or G7, the BC's reference atmosphere, and bullet weight;
  - twist rate, bullet length and bullet diameter, for spin drift.
  
  A record missing any of these simply cannot use the solver, and the screen says which field is missing.
- **The zero correction carried to another distance.** The analysis screen's zero block already says "moving a zero between distances needs the ballistic solver." When the rifle and load carry what it needs, give the correction at a second distance the person chooses, with the uncertainty carried through, not dropped. Keep the existing refusal when the offset cannot be told from zero: a correction at 500 yd derived from an offset nobody can distinguish from zero is still nothing.
- **A dope table** for a rifle and load: range, drop and wind per 10 mph in the person's units and clicks, from `grouplab trajectory`'s engine, with the atmosphere as an input. Show "Aerodynamic jump is not modelled" beside it, as the command does.
- **Hit probability at distance and distance normalisation wait.** They need the propagation of the group's sigma through the solver to be specified, which is its own entry.

**The README's states and `DESIGN.md` move with each section.**

### 5. Things not to do in this entry

- **Do not touch anything from `C:\Dev\grouplab-range-2026-09-20\`.** It arrives with its own entry.
- **Do not start Garmin Xero import.** It needs a real export file, and none is in the repository. The schema leaves room for it, as section 1 says.
- **Do not start Android.** Alan wants the Windows application right first.
