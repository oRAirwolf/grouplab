# 2026-09-21, entry 131: the interface overhaul, and working through the night without stopping

Alan used the application tonight and wants a large step up in how it looks and how easy it is to use. This entry is his list, with my specification for each item. Alan is asleep while you work it.

## 0. Stop ending turns while there is work in the queue

Last night's run ended its turn four times with `STATUS: WAITING, NOT FINISHED` while entries 129, 130 and now this one were full of work that needed nothing from anyone. A background watcher does not wake you: when you end a turn, everything stops until Alan types, and Alan is asleep. So, replacing the waiting rule in `CLAUDE.md` for good:

1. **Waiting for CI or a nightly is never a reason to end a turn.** Start the wait, then work on the next queued item. Check the wait between items with a quick `gh run list`. If truly nothing else can be done, poll in the foreground (a `sleep` of a few minutes inside a command, repeated), never by ending the turn.
2. **A turn ends only when** every queue is empty, or something needs Alan and nothing else can proceed without it. Even then, finish everything that does not need him first.
3. **Order of work tonight**: entry 130 section 1 (the real update test: keep it moving whenever its nightly is ready) and section 2b (missed holes), then **this entry**, then the rest of entry 130 (sections 2, 2c, 3 to 7, including 6b), then what entry 129 can do without the server. Push in batches; never push while a nightly you need is running; keep main green.
4. All of entry 130 section 0 still applies: no SSH, no settings, no website publish, no `v*` tags but the nightly's, record questions with a recommendation and move on.

## 1. How to do design work tonight

1. The look is fixed by `DESIGN.md` and the tokens in `Tokens.cs`: dark and light, IBM Plex, the logo's orange as the accent. Everything here works inside that system; you are making it clearer, calmer and more consistent, not changing the brand.
2. **Before building each screen**, produce a mockup and look at it: if Claude Design is available to you (the `/design` command in Claude Code), use it to explore and choose a layout; if it is not, render the screen as you build it. Either way, save before and after renders of every screen you change, at 1280 by 720 and 2560 by 1440, in dark and light, under `docs/figures/screens/current/`, look at every one yourself against this checklist, and fix what fails:
   - one type scale (for example 12, 14, 16, 20, 28) and nothing between; labels one size and weight, values one size and weight;
   - figures in tables or aligned grids, never loose lines of mixed sizes;
   - **the headline figure of each block in the logo's orange, large and prominent**; everything else in the neutral text colours;
   - generous, consistent spacing from the token scale; nothing clipped, overlapping or truncated at either window size;
   - every control's purpose obvious without reading a paragraph.
3. Tests keep passing, the gate record stays identical, and every new control is covered by the interface benchmark and the recorder (entry 122).

## 2. Editing shots: click a hole, click a bull

This is the most important item here. Editing must be quick and obvious.

1. **Click a detected hole** and a small editor opens beside it (a popover, not a dialog) with:
   - **Move**: drag the hole, or nudge it with the arrow keys (fine steps, larger with Shift), with a magnifier showing the pixels under it while dragging;
   - **Delete**;
   - **Assign to bull**: a list of bulls, plus clicking a bull on the sheet while the editor is open;
   - **Mark as**: sighter, flyer (kept but called out), or excluded from the group (kept on record, left out of the figures), each clearly shown on the sheet;
   - **Hole size**: adjust the diameter ring if the detected size is wrong;
   - a note field.
2. **Click a bull**: if it has shots, the same editor opens for them (tabs or a list when there are several); if it has none, **Add shot** places one at the click, and **Add several** stays in adding mode so each click adds a shot to that bull until Escape or Done. Also on the bull: its load (from entry 115's load per bull), and "not shot".
3. **Undo and redo** for every edit (Ctrl+Z, Ctrl+Y), and keyboard shortcuts shown in the popover (Delete, arrows, A for add, Escape).
4. **The shots list** on the analysis page gets an Edit button (opens the same editor with the hole highlighted on the sheet) and a Delete button on every row, with Undo in the confirmation (section 9).
5. Every edit updates the figures at once, and the figures carry the "uncertain" marking from entry 130 section 3 until the assignments are reviewed.

## 3. Units on the analysis page

1. **Zero correction in all four units at once**: MOA, mil, inches and centimetres, in a small table (angle and linear, up/down and left/right). If the session's rifle has scope units recorded, that unit is the headline (in orange) and in the scope's click value too ("Up 8 clicks at 0.1 mil"); otherwise MOA is the headline. The others stay visible beneath.
2. **A metric/imperial toggle** on the analysis page switching every linear and angular figure between inches with MOA and centimetres with mil. It remembers the choice, defaults from the Settings units, and never changes stored data.
3. **The shot distance's unit** is a dropdown beside the number: yards or metres.

## 4. Explaining every figure

1. A small **?** button beside each figure (zero correction, centre from aim, mean radius, sigma, extreme spread, CEP, the confidence intervals, anything else shown). Hover or click shows a two or three sentence plain explanation with a "More" link.
2. The "More" link goes to a glossary page on the website, `https://grouplab.org/guides/glossary/#<term>`, which you add to `website/` (built from a new `docs/GLOSSARY.md`, so it also reads on GitHub). The site is not published tonight; the links start working at the first publish after the server install. Until then the application may fall back to the GitHub copy of `docs/GLOSSARY.md`; say which you chose.
3. The explanations are accurate and plain, with no pseudoscience. Where a figure depends on sample size (every one does), say so in one sentence.

## 5. The analysis page's right-hand panel, redesigned

Today the text is many sizes and busy. Rebuild it as clear blocks, each with a heading, a table of figures and one headline figure in orange:

1. **Group**: shots, mean radius (headline), sigma, extreme spread, CEP, each with its interval where one exists.
2. **Zero**: the correction (headline, in the scope's units), centre from aim, the four-unit table from section 3.1.
3. **Shots**: the list with Edit and Delete.
4. **Load and rifle**: from the equipment records (section 7), with a link to change them.
5. Consistent spacing and alignment, numbers right-aligned in columns, units in a lighter weight beside them.

## 6. Pictures of the figures

1. **Mean radius on a scale.** A horizontal bar showing the group's mean radius per 100 yards (angular, so it is comparable across distances), with its confidence interval as a band, against reference marks. Alan's reference is what a Hornady podcast has said about mean radius per 100 yards: around 0.3 in is "pretty good", inside about 0.2 in is solid, below 0.2 in "you've really got something", and 0.175 in (0.35 in at 200 yards, over 20 to 30 shots) "really, really good". Label the marks as that source's rules of thumb, attributed, not as GroupLab's verdict; show the group's shot count beside it, and say in the tooltip that with few shots the interval is wide and the comparison weak.
2. **Zero offset picture.** Clicking the zero block opens a view showing the point of aim (the bull), the group's centre (point of impact) with its uncertainty ellipse, and arrows showing the correction as up/down and left/right in the scope's units and clicks.
3. **Calibre, best guess, confirmed before Accept.** The analysis page shows GroupLab's best guess at the calibre from the measured hole sizes (as a diameter, following the application's calibre rule), and **Accept cannot proceed until the person confirms or corrects it**, with the stated calibre then used by the size gate (entry 130 section 2b.3).

## 7. Equipment: rifles, barrels and loads, on their own screen

1. A new button on the left rail, **Equipment**, with three lists: Rifles, Barrels, Loads. Each opens a form; every field optional except a name.
2. **Rifle**: name, manufacturer, cartridge, barrel length, barrel twist (and direction), scope, scope units (MOA or mil) and click value, chassis or stock, notes. A rifle can have several barrels over its life.
3. **Barrel**: name, rifle, length, twist, round count, installed date, notes.
4. **Load**, separate from rifles because one load is shot in several rifles: name, bullet calibre (diameter), bullet weight, bullet name, brass manufacturer, brass cartridge, powder, powder charge, primer, cartridge overall length, cartridge base to ogive, velocity (with SD if known), notes. A load can be linked to any number of rifles.
5. **Autocomplete everywhere**: as a person types in any field, offer earlier values from that field that match, most used first.
6. The analysis page, the session records and the ballistics page pick rifle, barrel and load from these lists.
7. **This replaces the confusing "rounds or components" box** (`MainWindow.cs`, the `newDetail` text box shared between a barrel's round count and a load's components, under "New rifle, barrel or load"). Remove it; move existing records into the new forms without losing anything, with a test that old records load.

## 8. The ballistics page, redesigned

It is a mess today. Rebuild it after the layout of Alan's own calculator at `https://www.pissinhot.com/ballistic.html?s=e81e3d9a` (read it for layout only; do not copy its code, whose physics had the six faults entry 115 corrected): inputs grouped in clear sections (projectile, rifle and zero, atmosphere, wind and firing angle, range and step), with rifle and load picked from Equipment to fill them; an imperial/metric toggle; a **trajectory graph** with switchable series (drop, wind drift, velocity, energy) against range; and a **dope table** with range, drop and windage in the scope's units and clicks, velocity and energy, with the zero range marked. Keep "Aerodynamic jump is not modelled" visible, and every other honesty note the solver already carries.

## 9. Confirmations

Every change a person makes (an edit, a delete, a save, a setting) shows a small **toast** at the bottom right that disappears after about three seconds, saying what changed, with **Undo** where the change can be undone. One consistent component used everywhere; never a dialog for a confirmation.

## 10. Compare loads, redesigned

Make it a clear, attractive comparison: side by side group plots on a shared scale, mean radius and sigma with their intervals as bars or dot-and-whisker charts, shot counts, velocity and SD where known, and the verdict lines entry 113 already writes (never ranking on a point estimate). Alan may put reference screenshots in `C:\Dev\grouplab-design-refs\`; if that folder exists, read them for layout and follow them; if not, design it yourself within section 1.

## 11. Report

In the morning report (entry 130 section 8), for this entry: before and after renders of every screen changed, a short list of what was built per section, anything left undone and why, and questions with your recommendation.
