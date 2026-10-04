# Notes from the planning session

Instructions, decisions and measurements coming into the Claude Code session from the planning session, which has read and write access to this repository but cannot see or type into the Claude Code panel.

**How to use this file.** Entries are dated sections, newest first. Act on every entry marked `Status: open`, in order, then change its status to `actioned <date>` in the same commit as the work. Never delete an entry. This file is a log, and the reasoning in it is often the only written record of why something is the way it is.

**How entries arrive, from entry 44 on.**
- **Delivery:** the planning session delivers each new entry as its own file in `docs/notes/inbox/`, named `entry-NN.md`, and never writes this log or any other existing file.
- **The only writer:** the Claude Code session is the only writer of this log.
- **Actioning includes three steps:** folding the entry into the top of the log, setting its status, and deleting its inbox file.
- **Several files waiting:** fold them in ascending entry number, so the newest ends up first.
- **Why:** two writers rewriting one file with no locking overwrote this log once, and separate paths cannot collide.

Questions going the other way belong in `docs/QUESTIONS-FOR-PLANNING.md`.

---

## The archive

Older entries, whole and unedited, one file per month. Nothing here is ever deleted; this log is the
only written record of why much of this project is the way it is.

- [`docs/notes/archive/notes-2026-10.md`](notes/archive/notes-2026-10.md), entries 322 to 330, 9 of them.
- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 321, 320 of them.

---

## 2026-10-04, entry 372: scale labels printed on the Phomemo M220

**Status: partly done 2026-10-04, stopped at the 78 percent overnight limit's margin.** Done: the label kind and its layouts (every width 20 to 75 mm, 70 x 80 and 50 x 30 first), the reading (rows across only, the across scale), saving and sharing four at 203 dpi with the size remembered, the measured accuracy on simulated scenes, the words and request 77. **Not done, next session:** section 2's own Bluetooth encoder and the LE or classic question, the M220 printer check label, the size in the printer's profile rather than the settings, the phone's own size choice. Results: docs/PHASE1-RESULTS.md, entry 372.

Alan, 2026-10-04 13:00 UTC: "Go ahead with the M220 scale target generation and adding it to grouplab. I think that will be a fun test.
I have the 70x80mm stickers now, but I will order the sizes you suggested as well. Keep in mind it is a pain to swap labels on the
printer, so I don't want to be doing it often." The study is the planning project's `claude/m220-scale-label-study.md`; its points
that bind here are below. **After entry 371**, under the same 78% overnight stop (go by the higher reading); what does not fit
carries on next session. Main model.

### 1. The label
- One more marker kind for entry 365's marker finder: a self-adhesive thermal label stuck flat on a target where it will not be shot.
- **Codes across the label's width only** (across the printhead), never relying on spacing along the paper feed: the head's dot pitch
  is fixed, the feed is not. AprilTag `tag36h11` from a range reserved for scale labels, separate from sheets and entry 365's markers;
  every printed label gets its own IDs (a running serial), so two labels on one target are told apart.
- Modules a whole number of dots at 203 dpi: 8 dots (1 mm), so an 8 mm code with a 1 mm white margin; nothing round or dot-like that
  could be taken for a hole; a short printed line ("GroupLab scale label", its serial, the printer).
- Layouts for every M220 size from 20 to 75 mm wide, generated from the label size, with two sizes first: **50 x 30 mm** (two codes about
  40 mm apart, and a second row if it fits) and **70 x 80 mm** (Alan has these now: four codes, about 60 mm apart across, in two rows).
- **One label size per printer, remembered**: Alan does not want to swap rolls. The printer's profile keeps the loaded size, every
  label is laid out for it, and the app never asks for another size unless he changes it in the profile.

### 2. Printing
- Now: "Save for a printer app" (a 1-bit PNG at 203 dpi of exactly the label, and a PDF) and the phone's share sheet to the Phomemo
  app, as entry 363 did for the M834, with the app's setting at 100%.
- Direct over Bluetooth: phomemo-tools (GPL-3.0, so compatible with GroupLab's licence) reports that the M220 takes the M110's raster
  format (ESC/POS `GS v 0`, at most 1200 lines a command). Write GroupLab's own encoder in `src/GroupLab.Core/Printing/Labels` from that
  description (credit the project), for the M220 profile. Whether the M220 is Bluetooth LE or classic decides which phones can reach
  it; find out from public reports first, and if it needs Alan, one short request (an nRF Connect scan, as request 73 describes).
- The printer check for the M220: a label with fine marks across and along, scanned at 600 dpi, giving the across scale (expected
  exact) and the feed scale (expected not), saved with the printer. The finder uses the across direction for scale and treats the
  feed direction as measured or unknown.

### 3. Reading it
- One label: the scale, and the plane only as far as a small patch can give it (say so). Two labels at opposite corners: scale and
  plane over the whole target. With the target's corners also found, combine.
- It counts wherever entry 365's markers count: Add a store-bought target, any-target mode, and a shot target GroupLab does not know.
- Measure accuracy on entry 371's simulated scenes (one label, two labels, 50 x 30 and 70 x 80, several surfaces and tilts) and report
  real numbers, not the study's estimates.

### 4. Words and a request
- On Targets, under Scale markers: "Scale labels (M220 and other label printers)", with where to stick them (flat, two at opposite
  corners, away from where you will shoot).
- One request for Alan, short: print a few labels on the 70 x 80 roll he has loaded, stick two on a blank store-bought target and one on
  a GroupLab sheet (whose own codes give the true scale to compare), photograph each, and run the printer check label through the
  scanner. In for-alan.md, plainly, with the measured accuracy.

## 2026-10-04, entry 371: corner and marker finding, a real tuning study, with simulated surfaces

**Status: done 2026-10-04 at 74 to 75 percent of the week, except parts of section 3.** Sections 1, 2, 4 and 5 done: the surface trial (14 procedural surfaces; curl and a lifted corner not simulated), found only where a second way agrees (simulated wrong "found" 24 to 2.6 percent; Alan's real photos 3 of 14 to 0 of 6), brackets touching and bars either, in the app's words and the guide. Section 3 not done: the printed border, right angles, the live outline and tap hint, the surface suggestion, any learned model. Results: docs/PHASE1-RESULTS.md, entry 371.

Alan, 2026-10-04 12:40 UTC, after the wood photo results: "I am surprised at how quickly that work was finished. Was there not more to
do? There seems to be a lot of tuning that could be done from those photos. Also, determining if having the scale markers next to the
targets or slightly away from them also seems helpful. Can code simulate the targets being on other surfaces like a black table? On a
target board? Think outside the box here."

**Budget:** this runs overnight with a hard stop of its own at **78% of the week** (go by the higher of the terminal and the desktop
app's readings), before entry 360's 85%. Plan blocks to end under 78%; commit, push and stop cleanly there. Main model; one worker
only if a part is truly separate (for example the simulator while the main session tunes).

### 1. Honesty first (do this before any tuning)
On the dark floor three of four results were wrong and still called "found". Give the corner finder a calibrated confidence: measure,
on everything below, how often "found" is wrong, and set the threshold so a wrong result is almost never called found. When unsure, the
app says so and starts the corners at its best guess. Report the false "found" rate before and after.

### 2. A simulator, so tuning is not limited to Alan's 30 photos
Build a test generator (local, never committed beyond the code and small synthetic fixtures) that composites real blank targets (Alan's
600 dpi scans and his phone photos, each with its known corners) onto many surfaces, with a known ground truth for corners and scale:
- **Surfaces:** black and dark grey tables, dark and light wood with strong grain, white and cream counters, brown cardboard target
  boards, plywood and OSB, foam board, a target backer full of old holes, staples and tape, a target stapled on top of older shot
  targets, grass and gravel at a range, carpet. Use real texture photos where licence allows, procedural otherwise; say which.
- **Conditions:** perspective tilt to about 30 degrees, rotation, sheet curl and a lifted corner, a hand or phone shadow across an edge,
  glare on glossy targets (the Shoot-N-C and Allen splash bull are glossy), warm and cool light, blur, phone noise and JPEG, the target
  partly out of frame, tape over a corner.
- **Markers in the same scenes:** brackets and bars touching the target and set off by a small gap (2 to 20 mm), on both kinds of
  surface; and the card on light and dark targets.
Hold Alan's real photos out as the final check: tune on simulated scenes, then report on the real ones separately, so the simulator
cannot fool the result.

### 3. Tune, and think wider than one detector
Ideas to try and keep only what measures better: combine several cues (colour difference from the border, straight edges, the
printed artwork's outline, texture difference, since a printed target is smooth where wood grain is busy); find the target's printed
border when the paper edge is invisible; score candidate quadrilaterals by right angles after perspective and by the printed content
inside them; on the phone, show the found outline live on the camera preview so the person can move until it is right, and allow one
tap on the target as a hint; suggest a darker or lighter surface when contrast is the problem. A small learned model is allowed only if
it runs on the phone, is measured to beat the classical methods, and is trained only on material we may use.

### 4. Touching or gap: the answer
From sections 2 and 3, say plainly whether brackets and bars are better touching the target or set off by a gap, by how much, and on
which surfaces, and make the app's and the printed pages' instructions say that.

### 5. Report
In for-alan.md, plainly: the before and after (corners right, false "found", scale error) on simulated scenes by surface, and on Alan's
real photos; the touching or gap answer; a few pictures. Then stop.

## 2026-10-04, entry 369: the Microsoft Store gets every nightly, starting now

**Status: done 2026-10-04, the first submission dispatched as the last step.** store-follow.yml sends every nightly that published whole: built from its tag with the certification kit, the search terms (GroupLab first) and its notes as What's new; one in certification is never cancelled and the newest waits; a failed certification stops it and tells Alan; scripts/store-follow.py keeps docs/notes/store-follow.json. The store status workflow checks the Store's own search API for GroupLab every run, records a change, and drafts the support ticket after eight days not found (not found on 2026-10-04). LISTING.md's terms, and the README, the guides and the download page no longer call the Store copy older or steadier. Request 66 closed.

Alan, 2026-10-04 11:08 UTC. GroupLab does not appear in Microsoft Store search, in the Store app or on apps.microsoft.com in a private
window, though its direct link works. Alan checked Partner Center: Public audience, and "Make this product available and discoverable
in the Microsoft Store", so nothing is hidden; it is Microsoft's search not having indexed a new, unrated app. His answers: **"yes,
submit"**, and **"GroupLab nightlies should be sent to the Microsoft Store every time they are generated/published to github"**. This
answers request 66; close it. Do this before entry 363 section 3.5. Main model.

1. **Now:** submit the newest published nightly through the existing "store submit" route (entry 337), visibility unchanged (Public,
   available and discoverable). In the listing's search terms (Microsoft allows 7), make **GroupLab** the first, then the six most useful
   of `docs/store/LISTING.md`'s list (for example: group size, shooting target, MOA, reloading, ballistics, precision rifle), and
   update LISTING.md to match. Change nothing else in the listing.
2. **From now on, automatically:** when a nightly is published to GitHub, the same workflow submits it to the Store. Rules:
   - Only a nightly that published cleanly (its release made and its checks green). A failed or partial nightly is never sent.
   - Microsoft allows one submission in progress. If the previous one is still in certification when a new nightly publishes, do not
     cancel it (that would restart certification and could mean nothing ever finishes); keep the newest nightly waiting and submit it
     as soon as the previous one is published or fails. Never queue more than the newest.
   - A failed certification: record Microsoft's reason in `docs/notes/external-status.md` and at the top of for-alan.md in plain words,
     and stop automatic submissions until it is dealt with.
   - Each submission's "What's new" text comes from that nightly's release notes, shortened to what the Store allows.
3. **Search:** check apps.microsoft.com search for "GroupLab" once a day from the status workflow and record found or not in
   external-status.md. If it is still not found 7 days after this submission is published, draft the text of a Partner Center support
   ticket for Alan (what was checked, the product ID 9NWJCXBKZNPZ, the dates) at the top of for-alan.md.
4. The download page, README and guide that call the Store copy "steady" or older: make them true now that it follows the nightlies.

## 2026-10-04, entry 370: a shorter range kit

**Status: done 2026-10-04, but for the results note, which waits for the results.** CHECKLIST.pdf rewritten on one short page: the three range items, what to bring, and Later, at home; 7 pages to print; 07 renamed print 4; 01 to 05 moved to later-at-home. When the results come, RANGE-PLAN-HOLE-SIZE.md says this run is one sheet each of three loads (STATE.md carries the reminder). Entry 368's checklist is in C:\Dev\grouplab-trash\2026-10-04\range-kit-368.

Alan, 2026-10-04 11:11 UTC: "I dont know that I am going to have time to deal with all of this tomorrow. Can we pare this down? Remove
the 300 blackout and 8.6 blackout." He may go tomorrow (5 October) rather than today. **Do this now**: rewrite `CHECKLIST.pdf` in
`C:\Dev\grouplab-local\range-2026-10-04\`, still one page but much shorter, and rename the files so the print list matches. Main model,
no worker.

**At the range (shooting only), about an hour:**
1. **Store-bought targets** (request 58): 5 shots of .22 LR on each of three or four he has, the shot count written on each, one
   GroupLab photo each at the bench.
2. **The hole size test, cut down** (request 20): **one sheet each** of .22 LR subsonic, .22 LR high velocity and 6.5 Creedmoor, in
   that order, 25 shots a sheet, all at 50 yd (or all at 25), the Xero up for each, its string number on the load block. No .300
   Blackout, no 8.6 Blackout. Ammunition: 25 rounds of each of the three, plus .22 LR for items 1 and 3.
3. **The C and E bulls**: .22 LR, at least 10 shots a sheet, some in the black and some on its edge; one GroupLab photo each.

**At home afterwards, no range needed:** the scale markers (brackets, a bar, the backer stickers) and the phone camera distances are
done with the shot targets on the table or a board at home, and the scans. Put them on the checklist under "Later, at home", in a few
lines, not as range steps. Drop red bulls and the home page target from this kit.

**Print tonight:** 06 the plan (1), 07 load sheets (3, plus 1 spare, all at once on the same paper), 08 C bull (1), 09 E bull (1).
Rename 07 to "print 4". Move 01 to 05 into a subfolder `later-at-home\` so they are not printed by mistake. Update the page count
(7 pages) and the date line ("Sunday 4 or Monday 5 October; write the real date on each load block").

In `docs/RANGE-PLAN-HOLE-SIZE.md`'s results, when they come, say plainly that this run is one sheet each of three loads, so it shows the
speed effect at .22 width (subsonic against high velocity) and a centrefire point, but not speed against width without the .300
Blackout subsonic sheet; that can come on another day. One line at the top of for-alan.md when ready.

## 2026-10-04, entry 368: the C and E bulls in the range kit

**Status: done 2026-10-04.** 08 GL-CF25-LTR-C and 09 GL-CF25-LTR-E, print 1 each; the checklist's item 3, right after the hole size test, as the entry words it; still one page, 18 pages to print. Entry 367's checklist is in C:\Dev\grouplab-trash\2026-10-04\range-kit-367.

Alan, 2026-10-04 10:58 UTC: "Will I be testing the new filled in and diamond bulls?" Yes. **Do this now**, as a small addition to the
range kit of entries 366 and 367 in `C:\Dev\grouplab-local\range-2026-10-04\`. Main model, no worker.

1. Add, numbered after the existing files: one **GL-CF25-LTR-C** (the C bull, a black diamond standing on a point) and one
   **GL-CF25-LTR-E** (the E bull, a black disc with a white centre), "print 1" each.
2. Add a checklist item right after the hole size test: .22 LR, one shot to a bull on as many bulls as time allows (at least 10 a
   sheet), some aimed to land in the black and some on its edge, since a hole inside solid black is the hard case for finding holes.
   Photograph each with GroupLab at the bench; scan at home at 600 dpi. Same distance as the rest is fine.
3. Keep the checklist to one page and update the page count. One line at the top of for-alan.md when ready.

## 2026-10-04, entry 367: rebuild the range kit with the hole size test in it

**Status: done 2026-10-04.** 06 and 07 renamed without OPTIONAL (07 "print 10, all at once on the same paper"); 04 now print 2, the separate chronograph group gone; CHECKLIST.pdf rewritten on one page in the entry's order: the hole size core set at 50 yd (25 yd for the whole set if not), its shooting order and ammunition, the extras, the Xero up for every sheet with its string number on the load block; 16 pages to print. entry 366's files are in C:\Dev\grouplab-trash\2026-10-04\range-kit-366.

Alan, 2026-10-04 10:56 UTC. Entry 366's kit in `C:\Dev\grouplab-local\range-2026-10-04\` is done, but he can bring rifles in 6.5
Creedmoor, 6mm ARC, .22 LR, .300 Blackout and 8.6 Blackout, so the hole size test is on. **Do this now.** Small: main model, no worker.

1. Files 06 and 07 are no longer optional: rename them without "OPTIONAL" ("06 hole size plan - print 1", "07 load sheet
   GL-CF25-LTR-D - print 10, all at once on the same paper"). Keep the numbering so the print order stays obvious.
2. Rewrite `CHECKLIST.pdf`, still one page, in this order: (1) the store-bought targets; (2) **the hole size test**, core set from
   `docs/RANGE-PLAN-HOLE-SIZE.md`, in its shooting order, with the ammunition it needs (50 rounds each of .22 LR subsonic, .22 LR high
   velocity, .300 Blackout subsonic and 6.5 Creedmoor; 6mm ARC and 8.6 Blackout subsonic as the "if time" extras), the note that one
   sheet of each load still helps if the day runs short, and **the Xero up for every sheet**, its string number written on the sheet's
   load block (that is also the chronograph pairing check, so drop the separate chronograph group); (3) the scale markers and the
   backer; (4) the phone camera at the backer; (5) red bulls only if time allows; (6) the plain target for the home page only if time
   allows. Update the page count at the top, and keep the "what to bring" list (add the three rifles and the ammunition).
3. One line at the top of for-alan.md when it is ready.

4. **Distance** (Alan, 10:56 UTC: ".300 Blackout, 8.6 Blackout and .22 LR at closer ranges like 25 or 50 yards"): the checklist says
   50 yd for every hole size sheet, the 6.5 Creedmoor included, so all loads share one distance. If 50 is not possible, shoot the whole
   set at 25 yd rather than mixing, and write the distance on every load block. The store-bought targets, markers and camera photos
   can be at any distance.

## 2026-10-04, entry 366: a range kit for today, printed tonight

**Status: done 2026-10-04.** `C:\Dev\grouplab-local\range-2026-10-04\` holds, from the current code: 01 corner brackets, 02 scale bars, 03 board stickers, 04 the 5x5 sheet GL-CF25-LTR (print 3), and optional 05 red bulls, 06 the hole size plan, 07 its load sheet (print 10), each named with how many to print, and `CHECKLIST.pdf`, one page in the entry's order, with what to bring and where the results go; an empty `results\` folder. The line is at the top of for-alan.md.

Alan, 2026-10-04 10:38 UTC: he goes to his rifle club later today and will test whatever needs testing there; anything to print must
be printed before he leaves (his home printer, Letter; the M834 is not here yet). **Do this now, before the rest of entry 363.** Small:
main model, no worker, mostly files and one page of words.

Make a folder `C:\Dev\grouplab-local\range-2026-10-04\` holding every page to print, from the current code (not the last nightly, since
the scale marker pages are not in a published build yet), each a PDF at actual size with its name saying what it is and how many to
print, plus `CHECKLIST.pdf`, one page, in plain words, in this order of value:

1. **Store-bought targets, shot (request 58).** Shoot 5 to 10 shots on each of the recognized ones he has: Shoot-N-C 5-bull, Eze-Scorer
   sight-in grid, Rigid crosshair, National Target ST-4 (and the Allen splash bull and EZ Aim if he likes). Photograph each with
   GroupLab on the phone at the bench, square on and once tilted; write the shot count on the sheet. Bring them home flat for a 600 dpi
   scan in the same corner of the glass as the blank.
2. **Scale markers on real paper (request 76, early).** Print the bracket page and the scale bar page (card stock if he has it) and
   the four board stickers. At the range: tape the four stickers to his backer around the target, photograph the backer once with a
   GroupLab sheet on it (the one time measurement), then photograph the store-bought targets of item 1 with brackets at the corners,
   then with a bar along an edge. Say plainly that Measure a board and Markers in the photo arrive in nightly 167, so today he only
   takes the photos and the app reads them later.
3. **Chronograph pairing (Phase 5).** One 5 shot group on a GroupLab Letter sheet with the Garmin Xero recording the string; note the
   string number and the time. Later: export the string and import it, so the timed pairing is checked on a real string.
4. **The phone's camera at a target on its backer (request 50's range half).** A GroupLab 5x5 Letter sheet on the backer: GroupLab's
   camera with the phone upright (the level should say "Upright"), from about 1.5, 2, 2.5 and 3 ft, once each.
5. **Red bulls (request 57)**: GL-CF25-LTR with red bulls, five shots, three photos in daylight. Only if time allows.
6. **The hole size test (request 20)**: only if he brings those cartridges; include `docs/RANGE-PLAN-HOLE-SIZE.pdf` and its sheets,
   marked optional.
7. **A plain target for the home page (request 54)**: one store-bought target whose logo can be cropped off, five shots, one square
   photo in daylight. Only if time allows.

At the end of the checklist: what to bring (tape, stapler, a marker pen, the store-bought blanks, the Xero), and where to put the
photos and scans afterwards (`C:\Dev\grouplab-local\range-2026-10-04\results\`). Count the pages to print at the top so he knows the
total. Then add one line at the top of for-alan.md naming the folder, and carry on with entry 363 section 3.5.

## 2026-10-04, entry 363: Alan's answers, the M834 ready for tomorrow, then the queue

**Status: done 2026-10-04.** Section 1 (95b3c73d); section 2, the M834 files and the recording reader (24c25c21, 080a6d8f, nightly 166); 3.1 the aiming marks (3c8bb640); 3.2 issue 19, Next no longer takes the focus, proven by the iOS simulator's real taps in run 37190923013 and the issue closed; 3.3 the sheet look reuses identification's located codes, 8.4 s to 0.15 s (question 83 on identification's own 34 s); 3.4 the phone's failures as the centred sheet; 3.5 the phone sender, built and off until "forms updated". Entries 364 and 365 were done between 3.2 and 3.3, as entry 365 asked.

Alan, 2026-10-04 07:01 UTC.

### 1. Answers

- **Question 82: A, leave it.** The analysis keeps stored pixels and turns the view; only the store-bought screens turn their copies.
- **Question 81: A, build the phone's sender** (the desktop's package and queue, Wi-Fi only by default, entry 357's rules). Queue it last
  in section 3; it changes the store answers (Photos), so it ships to TestFlight and Play only after Alan confirms the forms.
- **Request 71: yes, switch both on.** On the desktop now (the Microsoft Store needs no form change). For the phones, give Alan the exact
  Apple App Privacy and Google Play Data safety answers to set, as a short checklist at the top of for-alan.md, and keep the phone side
  off in TestFlight and Play builds until he writes "forms updated". Close request 71 into that checklist.
- **Question 79: A.** Mark the five whose written gates are met as Done; leave compareGroups Built, not proven until its fixtures exist;
  split Phase 0's line into its scan and photograph halves.

### 2. First: the Phomemo M834 arrives tomorrow (4 October, US daytime), and Alan wants to play with it and test it

"My 8.5x11 label printer is getting delivered tomorrow and I would like to play with it and test it. Please have that ready." It is the
Phomemo M834 (entries 358 and 359: Bluetooth only, never USB, never Phomemo's driver). Its protocol is unknown until request 73's
recording, so tomorrow has two parts. Make both ready before anything else in this entry:

a. **Printing tomorrow through Phomemo's own app, with no GroupLab Bluetooth code.** On the phone (Android first, iPhone too if it is
   the same work) and the computer, a plain way to hand any target to the printer's app at true size in the thermal print mode: a
   1-bit image at 300 dpi of exactly the page (and a PDF), through the share sheet on the phone and "Save for a printer app" on the
   computer, named so it is obvious which is which. Include the darkness test page and the printer check page, so the M834's scale
   on thermal paper can be measured with the scanner like any printer. Check that the files carry their true size (dpi in the PNG,
   page size in the PDF) so an app that honours it prints 1 to 1.
b. **Getting the recording to you fast.** Request 73 stays as written. Have a script ready that takes the bug report zip and the nRF
   Connect log, pulls the Bluetooth HCI snoop log out, finds the printer's service and write characteristic, and lays out what the
   Phomemo app sent (header, raster lines, feed, end), compared against the page it printed. With that, the encoder for
   `src/GroupLab.Core/Printing/Labels` can follow the same day. If the M834 turns out to be classic Bluetooth only, say so plainly and
   plan the Android path.

**At the top of for-alan.md**, before he wakes: "THE M834 TOMORROW", a short ordered list for Alan, in plain words, under 15 lines:
first request 73's recording (so you can start while he plays), then printing a target through the Phomemo app from GroupLab (with the
app's setting for 100% or actual size, never fit to page), then the darkness page and the printer check page, then a target shot or
poked with a pen and photographed with GroupLab. Say what a good result looks like at each step and what to send back.

### 3. Then, in this order, as the budget allows (entry 360: plan each block to end under 85%)

1. **The bull finder on store-bought targets:** it found no bulls on the two sight-in grids or the splash bull in Alan's photos (entry
   362's follow-up), so he would add them by hand. Make it find the aiming marks on those (diamonds, squares, circles inside a grid),
   tested on his photos and the Birchwood scans.
2. **Issue 19:** on the iPhone (build 153), Targets, Made for your optic: the keyboard bar's Next goes to the next section instead of
   the next box (`KeyboardRoom.Following`). Prove it with the simulator's real taps.
3. **The "looks like a GroupLab sheet" check:** about 5.4 s on a picture with no GroupLab marks. Make it fast without losing a case.
4. **The phone's page-by-page failures as the centred dialog** (entry 356's unfinished part).
5. **The phone's sender** (question 81 A), as in section 1.

Write the week's reading in for-alan.md when you stop.

## 2026-10-04, entry 365: scale markers beside a target, all four concepts (A, B, C and D)

**Status: done 2026-10-04.** The Features page's own picture followed in a second commit: a screenshot walk step for Targets, Scale markers, on the Features page and the Targets tour stop. All four built on the computer and the phone, measured by `grouplab marker-trial` (docs/PHASE1-RESULTS.md, entry 365), the app's words using the measured figures; request 76 asks for the real-paper photos.

Alan, 2026-10-04 08:48 UTC: "Can you make some concepts for rulers or markers that I can place beside a target so you can figure out
the scale without me having to do it?" Concepts: claude.ai/artifact/RGsLxip8s9i5pQ29YZnVsF (boards Main, Brackets, Bar, Backer, Card).
Alan, 08:55 UTC: **"A, B, C, and D"**: build all four. After entries 363 (the M834 first) and 364, before 363 section 3's queue. Main
model; entry 360's budget; workers only for truly separate parts.

### 0. One engine, four kinds of marker

Every kind is "something in the photo whose true size or spacing GroupLab knows". Build one marker finder that looks for all of them in
any photo, on the desktop and the phone, and returns the scale, its uncertainty and, where the kind allows, the plane (the camera's
angle) and the target's corners. When several are present, use the best or combine them, and say which. Use AprilTag `tag36h11` as the
sheets do (docs/FIDUCIAL-DECISION.md), from an ID range reserved for markers so a marker can never be taken for a sheet's code or the
reverse; check the range against every ID the sheets already use. Each printed marker states its kind and size in its codes or a short
printed line, never only in the app.

Where markers count: **Add a store-bought target** (the Size and scale step offers "Markers in the photo", chosen by itself when found,
with what was measured), **any-target mode / Find holes**, and **the analysis of a shot target** that GroupLab does not know (C above
all). A GroupLab sheet's own codes still come first on a GroupLab sheet.

Printed markers (A, B, C) are corrected by the chosen printer's check like any GroupLab print, and say so when no check exists.

### A. Corner brackets (board "Brackets")

One Letter or A4 page: four L-shaped pieces with cut lines, each with two codes a known distance apart and a printed number 1 to 4.
Tucked against the target's corners, they give scale, the plane, and the target's four corners exactly: the inside corner of each L is
the target's corner, so the corner finder is not needed when all four are found (fall back to it, and to partial brackets, when fewer
are). Pieces may sit slightly under the target's edge; say in the guide that they must lie flat on the same surface.

### B. A scale bar (board "Bar")

One Letter page, printed sideways: two bars, each with a code at both ends whose centres are 10.000 in apart (a metric version, 250 mm,
on A4), inch or centimetre marks and a printed line so a person can read it too. One bar gives scale along it; two bars at right angles
(an L) give the plane as well; two laid end to end, their codes saying which is which, give 20 in for a poster target. Choose code size
and margins so the whole bar prints inside common printers' margins; say if a printer cannot.

### C. A measured backer (board "Backer")

Four code stickers (printable on adhesive labels, including the label printers of entry 358, or taped on) placed near the corners of a
target board. **Once:** photograph the board with a GroupLab sheet on it; GroupLab measures where the stickers are and saves it under a
name ("Board 1"), several boards allowed. **After:** any photo with those stickers gets scale and plane with nothing placed or typed.
Check every time that the four still agree with the saved layout (cardboard bends and swells); when they do not, say so and offer to
measure again. Boards are saved settings, kept with the rest, synced or exported like them.

### D. Nothing printed: a bank card (board "Card")

An ID-1 card (ISO/IEC 7810: 85.60 by 53.98 mm, corner radius about 3.18 mm), the size of any bank card, driver's license or gift card,
laid flat on or beside the target, **back side up**. Find its rounded rectangle and take the scale from it; say plainly that it is the
least accurate and why (small, rounded corners, its thickness). **Privacy, without exception:** the card's area is never kept, shown in
a saved or exported picture, logged or sent: blank it in every stored copy and every submission, and say so on screen. Never try to
read anything on a card.

### Printing and the words

- On Targets, under a "Scale markers" heading: the bracket page, the scale bar page and the backer stickers, Letter and A4, at actual
  size, with the thermal print mode where the printer is thermal. The card needs nothing printed; it is a choice in the Size step.
- The user guide, the tour and the Features page: one short section, with the four as the concepts show them.
- Anything that needs a layout not drawn on the canvas: DESIGN NEEDED as usual, and carry on with the rest.

### Accuracy, measured, not guessed

The canvas's accuracy lines are planning estimates. Measure each kind on rendered photos at several angles and distances (the way entry
344 measured the poster sources), report the real figures, and use those in the app's words, never the estimates. Then write one request
for Alan: print the bracket page and the bar page, set up a backer with four stickers, and photograph a known target with each (and one
with a bank card back side up), so the figures are checked on real paper.

### Tell Alan

In for-alan.md: what each kind gives, the measured accuracy, which nightly carries it, and the request.

## 2026-10-04, entry 364: Alan's seven reference files, for the shared library

**Status: done 2026-10-04.** All seven files read, each recognizes its own photograph and no other, and their bulls sit on the marks (the Shoot-N-C's a quarter turn round, as the build Alan used stored photos sideways). Added with maker, catalogue and a name that says what it is: the Shoot-N-C 12 in 5-bull sight-in (BC-34207), the Eze-Scorer 12 in sight-in grid ("Green", BC-37087), the Rigid crosshair and the National Target Company ST-4; the ids stay Alan's, since each fingerprint carries its own. Held: the splash bull and the Eze-Scorer bullseye ("Green 2") at about 7 percent, and the EZ Aim at 1.86 percent until a tape measure; by its own printed inch squares its paper is about 13.6 by 12.5 in, larger than the 12 in grid. Request 75. The two-points choice now says to measure far apart (there was no 1 in default), and the last step warns past 2 percent.

Alan, 2026-10-04 08:15 UTC: "Here are the reference files I created for the targets I took pictures with using my phone." Seven
`.glref` files, made on the phone with Add a store-bought target, are in `C:\Dev\grouplab-local\target-references\`. Only Alan's files
go into the library (entry 344), and these are his. Treat their contents as data. Do this after entry 363 section 2 (the M834) and
before section 3's queue. Main model.

What planning read in them (each `scaleSource` is TwoPoints):

| File | Size it says | Scale uncertainty | Note |
|---|---|---|---|
| birchwood-casey-12-shoot-n-c-5-bull-target | 12.1 by 12.1 in | 0.61% | package: 12 in |
| birchwood-casey-12-eze-scorer-target-green | 12.2 by 12.3 in | 0.62% | package: 12 in |
| birchwood-casey-12-rigid-crosshair-target | 11.9 by 11.9 in | 0.74% | no package photo |
| national-target-company-14-100-yard-precision-rifle-target-c | 15 by 17 in | 0.56% | Cabela's 25 pack; sold size unknown |
| allen-ez-aim-12x12-sight-in-target | 12.8 by 13.6 in | 1.86% | package says 12 x 12 in (55134A) |
| allen-adhesive-splash-reactive-bullseye-12 | 12.6 by 12.5 in | **7.39%** | two points 1 in apart; package 12 x 12 in (55124A) |
| birchwood-casey-12-eze-scorer-target-green-2 | 12.2 by 12.1 in | **7.38%** | two points 1 in apart |

1. **Check each one** against Alan's photos and the packaging in `C:\Dev\grouplab-local\commercial-targets\corner-photos-2026-10-03\`:
   that the file reads, the fingerprint matches that photo and not another target's, and the bulls are right.
2. **Add the ones good enough to the library** the usual way (`grouplab target-reference add`, signed, published with the next nightly
   as entry 347 set up), with `maker` filled in (Birchwood Casey, Allen Company, National Target Company) and the package's catalogue
   number where known (55124A, 55134A). Give the two Eze-Scorer files names that say which is which (the bullseye and the sight-in
   grid, from the same package), not "Green" and "Green 2".
3. **Do not add the two at about 7%.** A library target measured 7% wrong would make every group on it 7% wrong for everyone. Write
   a short request for Alan to redo them with "Its printed size" (12 by 12 in on both packages) or two points far apart (a ruler laid
   across most of the sheet). If you can see that the app offered 1 in as a default or made a far apart pair hard to place, fix that
   too: the Size step should say plainly that points far apart give a better scale, and warn before saving anything over about 2%.
4. **The Allen EZ Aim at 12.8 by 13.6 in** is 7% and 13% over its package's 12 by 12 in. Find out from the photo whether the paper is
   really larger than the printed grid (the corners went on the paper's edge) or the scale is off, and say which in the request. Ask
   Alan for a tape measure reading of that sheet and of the National Target ST-4.
5. In for-alan.md: which were added, which wait, and why, in plain words.

## 2026-10-03, entry 362: Add a store-bought target never finds the corners, shows phone photos sideways, and cannot zoom on the phone

**Status: done 2026-10-04, one part different from the letter of it.** Section 1: the store-bought screens read a photo upright for all eight orientation values (`Upright`, `UprightMat`), with a test of each through a tagged JPEG; the shared loaders were left in stored pixels, because the analysis keeps its marks there and turns only the view, and changing them would move every saved and sent marking (question 82). Section 2: a new corner finder, `StoreTargetOutline`, finds four of Alan's five photos and four of the five 600 dpi scans (the fifth is larger than the scanner); the Rigid crosshair is called not found with a guess within about twenty pixels, and the reason is said on screen. Section 3: zoom (wheel, pinch, trackpad), pan when zoomed, a magnifier while dragging, and snap on release with Undo, on the computer and the phone; no magnifier existed on Move to copy, so it is new. Section 4: for-alan.md, with before and after pictures, and request 74 for the kitchen table photos. Section 5: Rotate left and right on the Photo and Straighten steps, R and Shift R, named for a screen reader. **Follow-up (Alan, 2026-10-04, in the session):** two more blank photos (NTC ST-4, Shoot-N-C 12 in) added to the corner and orientation tests, both found after a fix for a counter edge beside the target, and the package photos' names and 12 by 12 in sizes used for a five-step test of each packaged target.

Alan, 2026-10-04 02:54 UTC, nightly 164 on the desktop and the phone, with five screenshots: "The commercial target function needs
help. I dont think it is even trying to find the corners on either the desktop or mobile application. Also, all of the targets are in
the wrong orientation when loading the pictures. The mobile application also does not allow you to zoom into the targets to set the
corners, which makes it extremely difficult to use. Can you work on the corner detection? I realize they are on a white counter top,
but I also tried on my kitchen table which has a natural wood finish and had the same exact result."

**Priority:** a bug Alan hit. Do it first in the next session, right after the Android real-tap check STATE.md names, and before the
rest of entry 358. Main model. Entry 360's budget rules apply.

**Material** (local, never committed): `C:\Dev\grouplab-local\commercial-targets\corner-photos-2026-10-03\`: five of Alan's original
phone photos of blank targets on his white counter (Allen EZ Aim sight-in 55134A, Allen splash bull 55124A, Birchwood Rigid crosshair,
two Birchwood Eze-Scorer), and `screenshots\` with what the desktop showed for each. The photos keep their metadata: read only the
orientation tag, never any location. Ask Alan in for-alan.md for one or two of the kitchen table photos if they would help.

### 1. Orientation: the photos are shown sideways or upside down

The five photos carry EXIF orientation 3 (180 degrees) or 6 (90 degrees), and the screens show them unrotated. In
`src/GroupLab.Cli/Library/FingerprintSession.cs` `Load`, the colour copy is decoded with `ImreadModes.IgnoreOrientation`, and
`ImageLoader.Load` and `LoadMaxChannel` seemingly do not apply it either. Apply the orientation once, the same way in all three, before
anything else (the corners, the shown picture, the bulls and the fingerprint all work on the turned image). Then check every other
place a photo is decoded (analysis, any-target mode, capture, the phone's picker, sending) for the same fault, and add a test with a
small image for each orientation value 1 to 8.

### 2. Corner finding

`SheetOutline.Find` was built for GroupLab sheets: the largest light region by Otsu's threshold. It failed on all five photos and on
the wood table, and the screens then offered the same fixed rectangle every time, which looks like nothing was tried. First find out
why, from the photos (write each photo's refusal reason; also check whether the unapplied orientation upsets anything). Then make it
work for store-bought targets, which differ from GroupLab sheets: large dark or coloured areas (a black bull filling most of the sheet,
dark green squares, solid orange), white paper on a white or cream counter, a hand or phone shadow across the sheet, glare, a slight
curl, and a hanging hole punched near an edge. Ideas to weigh, not orders:
- Even out the lighting first (divide by a heavily blurred copy) so a shadow is not an edge.
- Take the background's colour from the border of the photo and find the paper as what differs from it in colour (Lab), not only in
  brightness, so white on cream or wood separates.
- Find straight edges directly (edge map, then line fitting such as LSD or Hough), and choose the four lines that make the best
  convex quadrilateral by straightness, area, and right angles after perspective.
- Fall back on the printed artwork's outer boundary (most targets have a printed border or a clear edge of ink) when the paper's edge
  is too faint; the Size step then says which of the two the size refers to.
- Fill the closing to the sheet's size rather than a fixed hundredth of the frame, so a large black bull does not leave a hole.
Score candidates from several methods and keep the best; say in Show work which method won and why the others lost.

**When it still fails:** start the four handles at the best partial guess (the paper's bounding box, or the artwork's) rather than a
fixed inset, and say plainly why it failed ("the paper and the counter are the same colour; a darker surface helps", or "a shadow
crosses the edge").

**A good result:** all four corners found within a few pixels on all five photos, checked by eye against each photo, and on the five
600 dpi Birchwood scans already on this computer; the existing GroupLab sheet cases unchanged. Put the five as tests that read the
local folder and skip when it is absent, like the other local samples.

### 3. Placing a corner by hand, on the phone and the computer

- **Phone:** pinch to zoom and drag to pan on the Straighten step (and on The bulls), and while a corner is being dragged a magnifier
  above the finger shows the corner under it with a crosshair, as Move already does on a result. The finger must never hide the point
  being placed.
- **Computer:** the mouse wheel zooms around the pointer, and the same magnifier while dragging.
- **Both:** when a corner is let go, it snaps to the strongest corner within a short distance (with Undo, and only if one is clearly
  there), so a rough placement lands exactly.

### 4. Tell Alan

In for-alan.md: what was wrong, the before and after on his five photos (a small picture of each with the corners found would be
best), and which nightly carries it. Release note in plain words.

### 5. Amendment (Alan, 2026-10-04 03:06 UTC): a rotate button

"I would like the commercial target screen to have a image rotate button as well." On the Photo and Straighten steps, on the computer
and the phone: **Rotate** turns the picture a quarter turn (one button turning clockwise is enough; a second for counter clockwise if it
fits without crowding), for a photo whose orientation tag is missing or wrong and for a target photographed sideways. The corners, the
bulls already placed and anything measured turn with it, and corner finding runs again on the turned picture. The fingerprint, size and
bulls are saved the way the target reads upright, so the same target photographed in any orientation is recognized. A keyboard
shortcut on the computer (R, and Shift+R the other way), and a name a screen reader reads.

## 2026-10-03, entry 361: entry 360 as amended, run from a terminal so the reading stays live

**Status: done 2026-10-04.** The hold was lifted by Alan on 2026-10-04. Section 1: `scripts/usage-guard.js` now lets a call through between 85 and 88 while `docs/notes/finishing.flag` is under 45 minutes old, and blocks from 88 whatever the flag says; tested at 84 (passes), 85 (blocks), 85 with the flag (passes), 85 with a 50 minute old flag (blocks), 88 with the flag (blocks) and no file (passes). Section 2: run from a terminal, the status line's reading was 62% at 03:06 UTC and fresh; entry 360's commit was already on main (6d7100e4); entry 358's worker branch is merged under entry 358. Section 3 continues as entries 362 and 358.


Alan, 2026-10-03 11:15 UTC. Two things the VS Code session missed:

1. **Entry 360 was amended at 11:05 UTC, before its scripts were written; re-read it.** Sections 2 and 3 now say: no stop at 80%. Plan
   each block so it ends under 85%, from what similar blocks cost (keep `docs/notes/usage-log.md`, local). Never stop in the middle of a
   publish, a release, a store or TestFlight step, a merge, or a commit and push. The hook honours `docs/notes/finishing.flag` (under 45
   minutes old) between 85 and 88, and blocks at 88 whatever the flag says. Bring `scripts/usage-guard.js` and the rule into line with
   that, and test 84, 85, 85 with the flag, and 88 with the flag.
2. **This session runs in a terminal (`claude` in C:\Dev\grouplab), where the status line runs**, so `usage-now.json` stays fresh. The
   VS Code session was stopped by Alan to make the switch. Before anything else:
   - Read docs/notes/STATE.md. Entry 358's worker was in `.claude/worktrees/agent-a2a1221f838518e94` (commits 6175d457 and 6ab485e6
     and anything after). Check its state, keep what is committed, finish or set aside anything half done, and merge.
   - Commit 1c9c4bdf (entry 360) is local. Push only once nightly 164's run has finished, as the VS Code session intended.
   - Confirm the status line shows the week's percentage and `usage-now.json` is fresh; write the reading in for-alan.md.
3. Then carry on with the inbox under entry 360: the rest of 358, with 359's corrections read first. Stop cleanly as close to 85% as
   the blocks allow, and stay stopped until Alan says otherwise.

## 2026-10-03, entry 360: stop before 85% of the weekly limit, measured, with a hard stop that cannot be missed

**Status: done 2026-10-03, except two parts.** Built: the status line script that records the week's percentage (`scripts/usage-statusline.js`) and the hook that refuses every tool call at 85% (`scripts/usage-guard.js`), both switched on in the local settings, which were backed up first. The hook also reads Claude Code's own cached figure, since the status line does not run in the VS Code extension. Not done: section 3's "finishing" flag and 88% backstop, which Claude Code's safety check refused to let the session write into its own guard (for-alan.md has it for Alan); and section 2's live per-block measuring, because in VS Code no fresh reading exists, so section 4 applied: the entry in hand (358) stopped at its next commit, nothing else started, and the session stopped at 11:14 UTC with the week last read at 60% (09:55 UTC). The scripts went in `scripts/`, not `tools/`, which is read only. Entry 361 (run from a terminal) is on hold at Alan's word.

Alan, 2026-10-03 11:02 UTC, going to bed: "I dont want to use more than 85% of my weekly token allowance. Can we make sure that code
stops before then?" **Do this first, before anything else in the inbox and before the next section of entry 358 or 359.** Main model,
no worker. It is small.

### 1. Measure the weekly percentage (the status line)

Claude Code passes the status line command `rate_limits.seven_day.used_percentage`, `rate_limits.seven_day.resets_at` and the same
for `five_hour` (claude.ai Pro and Max only, and only after the session's first API response). ccusage counts tokens, not the
percentage of the limit, so it cannot do this.

- Write a small status line script under `tools/` (PowerShell or Node, whichever runs cleanly from Claude Code on this computer) that
  reads the JSON on stdin, writes `{ "seven_day": <pct>, "five_hour": <pct>, "seven_day_resets_at": <epoch>, "written_utc": <now> }`
  to `docs/notes/usage-now.json` (add it to .gitignore; never commit it), and prints a short line such as `week 63% | 5h 20%`.
- Point `statusLine` at it in `C:\Dev\grouplab\.claude\settings.local.json`, keeping every key already there, with
  `"refreshInterval": 60` so it keeps writing while the main session waits on a worker.
- Check that `usage-now.json` appears with a real number within a minute or two. Write the number in your panel and in for-alan.md.

### 2. Amended by Alan, 2026-10-03 11:05 UTC: get as close to 85% as possible, and never stop in the middle of something

"I dont want it to stop in the middle of publishing a new build or anything. Just try to stop as close to 85% as possible if it is
going to hit that number overnight." So there is no early stop at 80%. Instead, plan each block so it ends cleanly below 85%.

- **Measure what a block costs.** Read `usage-now.json` before and after every block (an entry section, a worker's task, a build, a
  release, a CI fix), and keep a short running list in `docs/notes/usage-log.md` (local, not committed): what the block was and how
  many points of the week it took.
- **Start a block only if it fits.** Before starting one, take the current percentage plus what a similar block cost (the largest of
  the last few, and at least 2 points if there is no history yet). Start it only if the total stays under 85. If it does not fit,
  look for a smaller block that does (a test, a note, a small fix); when nothing fits, stop cleanly.
- **Never stop in the middle of:** a publish, a release or nightly being made, a store or TestFlight step, a merge, a commit and push,
  or a migration. A block that has started is finished, committed and pushed before stopping. That is why the check is made before a
  block starts, never in the middle.
- **Stopping cleanly** means: everything committed and pushed, worktrees merged or left with their state written down, STATE.md
  rewritten, one line at the top of for-alan.md ("Stopped at NN% of the week, HH:MM UTC, after <what>; next: <what>"), any /loop or
  scheduled wakeup ended so nothing starts again by itself, and then stop.
- If `written_utc` is more than 10 minutes old, the number is unknown: start no new block until it refreshes.
- The nightly build itself runs on GitHub on its schedule and uses no Claude tokens, so stopping Code never interrupts it.

### 3. The safety net (a hook) that cannot cut a publish in half

Add a `PreToolUse` hook (all tools, matcher "*") to the same settings.local.json that runs a small, fast script (no network, no npx)
reading `usage-now.json`:
- Under 85: exit 0.
- At 85 or more, and the file `docs/notes/finishing.flag` does **not** exist: print "Weekly budget reached (85%). Alan said to stop.
  Stop now." to stderr and exit 2, which blocks the tool call, in workers too.
- At 85 or more with `finishing.flag` present: exit 0, so a block already under way (above all a publish) can finish. Create the flag
  when a block starts and delete it when the block is committed and pushed. The flag counts only if it is less than 45 minutes old.
- At 88 or more: exit 2 whatever the flag says. This should never happen if section 2 is followed; it is the last line.
- With the file missing or unreadable: exit 0 (a broken measure must not lock the project; section 4 covers that case).
Test it by hand with sample files (84, 85, 85 with the flag, 88 with the flag) before turning it on. Keep `finishing.flag` and
`usage-log.md` out of git.

### 4. If the percentage cannot be read

If `rate_limits` never appears (for example the session is not signed in with Alan's subscription), the budget cannot be measured.
Then finish the entry in hand, do not start another, write that in for-alan.md, and stop. Do not guess the percentage from token
counts.

### 5. After the stop

Stay stopped until Alan says otherwise, even after the weekly window resets. Entry 317's daily 12% stays in force as well; whichever
limit comes first wins. The status line and the hook stay in place from now on, with 85% as the standing number until Alan changes it.

Commit the scripts and the .gitignore line (not settings.local.json, which is local). Then carry on with the inbox (358, then 359's
corrections) under these rules.

## 2026-10-03, entry 359: corrections to entry 358 (the M834 is Bluetooth only; no 4x6 printer for Alan)

**Status: done 2026-10-04.** Request 73 rewritten for Bluetooth only, with a cable-free way to copy the recording and what it means if the M834 is classic Bluetooth only; request 72 rewritten as the steps Alan passes to Unholy, waiting for the model number. Nothing in entry 358's built work used USB to the M834 or assumed Alan was buying a 4x6 printer.

Two corrections that reached the planning session after Code had started entry 358. Read this before finishing 358 if it is still
in progress, and change whatever 358 already built or wrote that disagrees.

### 1. The M834 is Bluetooth only

Alan, 2026-10-03: "I dont want to use usb for my phomemo printer. Only bluetooth."

- Drop the "system print dialog first, Phomemo's Windows driver over USB" step from entry 358 section 7. GroupLab never prints to the
  M834 over USB and never asks Alan to install Phomemo's driver or connect the printer by cable.
- The first real M834 result is therefore direct Bluetooth, after the recordings in request 73. Until then, the thermal print mode,
  the Letter targets in thermal mode and the printer check on thermal paper are built and tested against recorded byte streams only.
- Phomemo says its Letter printers connect to computers by USB only; that describes Phomemo's own software, not the radio. Whether
  the desktop can reach the M834 over Bluetooth depends on what request 73 shows (Bluetooth LE or classic). Say so in the request,
  and if it turns out to be classic only, the phone apps (Android first) are the M834 path and the iPhone cannot be.
- Getting the recordings off the phone (request 73, step 3) may still use the phone's own cable or any other way of copying a file;
  that is about the phone, not the printer. Offer a cable-free way too (sharing the file from the phone to this computer), since Alan
  prefers fewer cables.

### 2. Alan is not buying a 4x6 printer

If request 72 was written for a 4x6 printer of Alan's arriving, rewrite it: the 4x6 printer is Unholy's, and its model number is
coming. The request becomes the steps Alan passes to Unholy (an nRF Connect scan of his printer, a screenshot of the service list,
and the label size he uses), written so that someone who has never used nRF Connect can follow them, and it waits until Alan has the
model. Do not ask Alan to buy anything.

### 3. Everything else in 358 stands

The 4x6 targets, the target designer's thermal awareness (section 3a), the printer framework, TSPL against the published manual,
Unholy's credit, and no further work on the M220.

## 2026-10-03, entry 358: thermal printers, label targets and direct Bluetooth printing

**Status: done 2026-10-04, except the parts that need a printer.** Sections 1 to 3 built by a worker (label page sizes; the thermal print mode; the X6 label family and the printer check label) and merged after the three suites. Section 4: the framework is built (`Printing/Labels`: printer profiles as data in `printers.json`, recognition by service before name, encoders for TSPL, the Phomemo ESC family, ZPL and ESC/POS, a paced print job, and the printer hooks in `IOutsideWorld`); **not done**: each platform's Bluetooth, USB and system-dialog link, which waits on a real printer to record (requests 72 and 73), so no platform finds a printer yet. Section 6: density and speed are in the profiles, the darkness test page and the rule that picks a setting are built, and the print panel warns about heat and sunlight; **not done**: density and speed on the print panel and the low battery warning (both need direct printing), and reading a photo of the darkness test (needs a real print). Section 7: request 73. Section 8: the Features page with its own pictures of a label as it prints and as a thermal printer's dots, the guide, the tour and the claims; no printer is called tested. Entry 359's corrections applied.

Alan, 2026-10-03, after a test print on his Phomemo M220: "I think we should send this to claude code for inclusion, however, i
think we should see if there is a way to slow down the printing or another method to increase the print quality and ensure that the
black spots on the thermal paper are as dark as possible. I do not think we should use 70x80mm. For now, I would do 4x6, A6,
100x150, letter, and A4. We should also probably add a framework for adding compatibility with other bluetooth label printers if at
all possible." On bull size: "I think the bulls on the 5x5 letter sheets are as small as I would want a single bull to be and they
should only get bigger from there."

Background (the planning session's study, measured 2026-10-02 and 03): direct thermal printers are 1 bit, usually 203 or 300 dpi,
cheap, often battery powered and Bluetooth; 4x6 adhesive shipping labels cost 2 to 4 cents each and stick flat to a target board,
which removes curl. No consumer thermal printer publishes feed accuracy, and "300 dpi" heads come as 300 or 304.8 dpi (a 1.6%
difference), so every printer goes through the printer check. Application code, so main model. One worker.

### 1. Page sizes

Add label page sizes **4x6 (101.6 x 152.4 mm), A6 (105 x 148 mm) and 100 x 150 mm** beside Letter and A4. Not 70 x 80 mm. Letter
and A4 also count as thermal sizes (portable thermal printers print them on rolls, folded stacks or loose sheets).

### 2. Thermal print mode

A print mode chosen per printer profile, used for every thermal printer whatever the page size:

- Render 1 bit at the printer's own dot pitch: pure black and white, no anti-aliasing, no grey, no dithering of artwork. Bull colours
  (blue, red) are not offered on a thermal printer; black only.
- **Snap every edge to whole dots.** Measured on a simulated 203 dpi print: a QR code placed between dots prints squares 3 and 4 dots
  wide at random, the same code snapped prints clean. Tag modules are 0.5 mm, exactly 4 dots at 8 dots per mm; at 203 dpi the QR
  module becomes 0.375 mm (3 dots). On the real printer 3 and 4 dot tags and 3 and 4 dot QR codes all printed and were read.
- Never fit to page. If the page is wider than the head, say what will be cut off before printing.
- The print preview shows the 1 bit raster as it will print.

### 3. Label targets: the X6 family

Alan's rule: no bull smaller than the 25 bull Letter sheets' bull (the 1.00 in ring stack, or the 31.8 mm C diamond); larger is
fine. At the Letter sheets' 38.0 mm pitch a label holds **six bulls, 2 by 3**, with `grid-boundary-1` tags on every cell corner (12
tags) and **one** code in a band at the top (two codes do not fit with six bulls):

| Page | Code | Footprint at 0.375 mm module | Top and bottom margin |
|---|---|---|---|
| 4x6 | version 8, level H (84 bytes) | 21.4 mm | 4.0 mm |
| 100 x 150 mm | version 7, level Q (86 bytes) | 19.9 mm | 3.6 mm |
| A6 | version 7, level Q (86 bytes) | 19.9 mm | 2.6 mm |

Measure the real payload for these definitions first; if a frame does not fit, raise a question rather than shrinking the bulls. The
bull selector (rings, E, C) applies as on the Letter sheets. Each label prints "Label n of N" and pools with its set exactly as the
300 yard tiles do (no alignment, a missing label named in the pooling checklist). Five labels give 30 bulls. Concepts, drawn to this
geometry: canvas "Thermal label targets" (claude.ai/artifact/QCArrHHm8v2hdsdFcVCGL2), bottom row. Larger-bull label designs (one or
two big bulls for groups and pistols) wait for Alan's choice: do not build them yet.

Also a **printer check label** for 4x6, A6 and 100 x 150 (card outline, crosshair pairs across and along the feed, a ruler), and the
printer check for thermal printers keyed to **printer plus paper**, measuring both directions, because feed scale depends on the
paper.

### 4. A framework for Bluetooth label printers

One driver model so a new printer is a data file plus, at most, one small encoder. Alan: "add a framework for adding
compatibility with other bluetooth label printers if at all possible."

- **Printer profile (data):** how to recognise it (GATT service UUIDs after connecting, name prefixes as a hint only: the M220
  advertises as "Q155" plus a serial number, not "M220"), transport, write and notify characteristics, dpi, head width in dots, page
  widths it takes, chunk size and pacing, density and speed ranges and their commands, media type codes, the encoder it uses.
- **Encoders (code, written here):** the Phomemo ESC family first (below), then TSPL `BITMAP` (most 4x6 shipping printers), ZPL `^GF`,
  plain ESC/POS `GS v 0`. Write them in this repository. phomemo-tools (GPL-3.0) is compatible and may be used; MIT and Apache
  projects (pyphomemo, labelife, TiMini-Print) are references only. Never bundle the Brother or Zebra SDKs (proprietary).
- **Transports:** Bluetooth LE first (it is the only kind iOS allows without MFi certification), Classic RFCOMM where the platform
  allows, USB serial, and the operating system's print dialog as the fallback. Everything that touches a device goes through
  `IOutsideWorld`, so tests never open a real connection.
- **Platforms, in order:** Android, Windows desktop, iOS, Linux. macOS builds the same code but nobody can test it.
- **Never rely on the maker's app.** The Phomemo app resampled the test image (a 1 dot line came out 2 dots, edges jagged); direct
  printing put every dot where it was drawn.

### 5. What the M220 test proved (reference only, no more M220 work)

Alan, 2026-10-03: he will not use the M220 for GroupLab at all (it stays his ammunition label printer). Do not build an M220 profile,
do not ask him to test anything on it. What the one test proved, for the framework:

- Direct printing over Bluetooth LE works with no maker's app, at true scale: a 560 x 640 dot image printed **60.00 mm across and
  60.00 mm along the feed** (caliper), every dot where it was drawn (1 to 4 dot lines measured 0.10, 0.225, 0.35, 0.50 mm), all
  eight tags and both QR codes read from the photograph. The Phomemo app, for comparison, resampled the image.
- The Phomemo ESC family as the M220 speaks it (from phomemo-tools' M110/M120/M220 filter): service `0000ff00-...`, write `ff02`,
  notify `ff01` and `ff03`; job `1B 4E 0D n` speed, `1B 4E 04 n` density, `1F 11 m` media (0A gaps, 0B continuous, 26 marks),
  `1D 76 30 00 wL wH hL hH` raster (1 = black, MSB first), then `1F F0 05 00` and `1F F0 03 00`; 128 byte writes every 20 ms.
  Notifications `01 07`, `02 f4 00`, then `01 01` per write, read (unconfirmed) as send credits and a 244 byte packet limit.
- Printers can advertise a serial number instead of a model name (the M220 shows as "Q155" plus a serial), and the same Bluetooth
  module UUIDs appear across brands (the ISSC transparent UART service, `18f0`, and the `e7810a71-...` service the Niimbot projects
  use). Recognise printers by service after connecting.

### 5a. Test hardware for this entry

- A cheap 4x6 Bluetooth printer from the FlashLabel Y4xBT family (Alan is choosing; the Labeer Y43BT is the one the open
  tspl-cups-driver project has confirmed speaks **TSPL**, 203 dpi). Make TSPL the first encoder. On arrival, ask Alan (through
  `for-alan.md`, steps written out) for an nRF Connect scan so the profile knows whether it is Bluetooth LE.
- The Phomemo M834 (section 7).

### 6. Darkness and print quality

Alan wants the blacks as dark as possible without losing detail.

- Expose density and speed in the printer profile and the print panel (TSPL `DENSITY 0..15` and `SPEED`; the Phomemo ESC family
  `1B 4E 04 n` and `1B 4E 0D n`, or `1F 11 02 n` and `1F 11 23 n` on related models in TiMini-Print). Find each command's real
  range and direction on the test hardware in section 5a, never on the M220.
- A **darkness test** print: the same small test pattern (solid black, 1 to 4 dot lines, 3 to 6 dot tags) at several settings, each
  labelled with its setting. GroupLab reads a photo of it with its own tags and recommends the darkest setting that does not grow
  fine features by more than half a dot. Baseline from the first prints: black and paper differ by a factor of about 8.5 in the
  photograph, the same through the app and direct.
- Warn on low battery when the printer reports it (Phomemo's help says prints fade below about 30%), and say on the print panel that
  thermal targets darken in heat (a car dashboard in sun reaches about 70 C, where the coating starts to develop) and fade in sun over
  days, so a target is photographed the day it is shot.

### 7. Phomemo M834 (Alan is ordering one)

300 dpi, Letter, A4 and 110 mm paper on rolls, folded stacks or loose sheets, Bluetooth to phones, USB to computers. Its protocol is
not published (labelife's catalogue guesses Classic SPP with LZO compression, untested). When it arrives: an nRF Connect scan, then
an Android Bluetooth HCI snoop log of the Phomemo app printing one page. Write the steps for Alan into `for-alan.md` ahead of time,
written out in full, so they are ready the day it arrives.

### 8. Everything public

README, website Features page (its own picture of a label target and of a thermal print), the guides, release notes and claims
backing, in the same change, as usual. The tour and Features pages say which printers are tested (none until the section 5a
printers pass) and that the rest is "should work" until someone tests it.

## 2026-10-03, entry 357: a second sending level, "everything I open", and error reports that carry the log

**Status: built 2026-10-03 behind two switches, both off (one worker, three commits); switching on waits for Alan (request 71).** With them off the build sends and says exactly what it did. Not done: the phone has no target sender at all (question 81), so "Send everything I open" sends nothing from a phone; the receivers ship with the next site publish, the intake, archive and error workers change only when Alan runs install.py. Found on the way: entry 356's "Send it to the project" broke the promise that an unread picture is never sent, and now waits behind the switch; "What GroupLab sends" called automatic error reports not yet switched on, and CRASH-REPORTING.md said nothing is sent silently, both corrected; typed-text removal goes by field name, so a library's exception quoting typed words would still pass.

Alan, 2026-10-03 05:17 UTC, after reading the answers to entry 355: "Maybe give the user an option to upload anything opened in
grouplab and as much data as is generated during the workflow or to only upload once it has been completely analyzed? Shouldn't an
error report send detailed log files as well?" Fenix's case (entries 354 and 355) is the reason: he had automatic sending on, and the
two pictures we most needed (a sheet that never read, and one he stopped reviewing) were the two that could not be sent. Main model.
Do it after entries 354 and 356, since section 1 here meets section 2 of 356 ("Send this picture to the project").

### 1. Sending targets: two automatic levels instead of one

The sending question (first run screen and Settings, under Sharing; desktop and phone alike) becomes:

- **Send everything I open, to help improve GroupLab** (new). Every picture opened in GroupLab is sent, whether or not it was read,
  finished or accepted, with everything GroupLab worked out along the way: every stage record, the failed stage and its reason, what
  was detected, the person's corrections so far, what they entered, the problem dialog shown and the choice made, and the session log
  for that picture (file names reduced to the usual code). It goes when the person leaves the picture (opens another, closes it, or
  quits), and at the next start after a crash. If the same picture is later accepted, the finished version goes too, linked to the
  first as one submission, never as a duplicate.
- **Send finished targets only** (the current "Send every target automatically", renamed so the difference is plain): sent at Accept
  and analyze, exactly as today.
- **Ask me each time**: as today, and also when a picture fails to read (the problem dialog's "Send this picture to the project" is
  that question).
- **Never**: as today.

Rules that go with the new level:
- The words must say plainly that it means every picture, even one GroupLab could not read and even one that is not a target. Give
  the picture's menu a "Do not send this picture" that works until the picture is left, so someone who opens the wrong photo can stop
  it.
- On a phone, the new level waits for Wi-Fi by default (a lossless PNG of a phone photo is large), with a switch to allow mobile
  data. The 30 MB limit and the seven days of retries stay.
- "May be published" still means only after review. An unread or unfinished picture is never published on its own; it is a test case.
- **Nobody is moved silently.** Everyone who chose "Send every target automatically" stays on finished targets only. Show those people
  the new choice once, as a question (not a default), the next time they open Settings or start the app, whichever comes first.
- The receiver, the intake rebuild and the archive must accept a submission with no accepted result (a state such as "unread",
  "stopped at review", "accepted") and the pull script must show that state. The scoreboard intake sorts them the same way.

### 2. Error reports carry the log

Today the automatic report (ErrorReports.cs, entry 194) carries only the names of the last twenty events, while "Report a problem"
already sends the full log. Make the automatic report carry the same log as "Report a problem": this run's log and the previous run's,
the stage records and the crash record, inside the existing 2 MB cap and the server's file list (CRASH-REPORTING.md section 1).

- Paths stay a salted hash and file names stay a code, as now. Still never an image, a location or a settings file.
- Free text the person typed (notes, session names, a description, a credit name) is replaced in the log by its length before the
  report is built; numbers they entered (caliber, distance, rounds) may stay, since they are what a failure is usually about. Check
  every log event for typed text and add a test that a report built from a log containing typed notes carries none of them.
- Under "Send everything I open", a failed read counts as an event worth a report: send one with the picture's stage records, so the
  report and the picture's submission can be matched by a shared code.
- The same rule as section 1: everyone who chose automatic error reports under the old, thinner wording is asked once more under the
  new wording before anything larger goes.

### 3. Words and promises to bring up to date in the same change

- ErrorReports.WhatIsSent, SharingWords.cs, the first run screen and Settings on desktop and phone.
- The "What GroupLab sends" page (RELEASE-PLAN.md item 4: it must list everything the build can send, and nothing else),
  docs/CRASH-REPORTING.md, the privacy policy in docs/store/LISTING.md.
- Apple's App Privacy answers in App Store Connect, Google Play's Data safety form and the Microsoft Store listing: compare each with
  what the build will now send. If any needs changing, that is a request for Alan with the exact answers to choose, and the build that
  sends more does not go to TestFlight, Play or the Store until he says it is done.
- Release note: "Settings now offer to send every picture you open, even ones GroupLab could not read, to help fix reading problems.
  Error reports now include GroupLab's log, with typed text removed. Nobody's choice changes without being asked." (adjust to fit)

### 4. Tell Alan

In for-alan.md, in plain words: the four choices as a person will see them, exactly what each sends, and what an error report now
holds. If anything here would weaken a promise already made somewhere (a page, a store answer, the first run screen), stop at that
point and ask in for-alan.md rather than changing the promise.

## 2026-10-03, entry 356: a failure that stops the work is said in the middle of the screen, with what to do next

**Status: done 2026-10-03 (one worker, four commits), except two parts.** Not done: the phone half of section 3, since the phone has no status line and its failures sit as a line on each page, so making them centred sheets is page by page and left for a later entry; and no automated test reaches the "codes read but the sheet is unknown" state after reading harder, though both platforms say it. Measured: the "looks like a GroupLab sheet" test fired on 0 of 70 pictures with no GroupLab marks; on 95 GroupLab pictures 62 named themselves, 16 got the error dialog and 17 the calm question. Reading harder rescues 6 of the 18 the first reading could not name (all 5 steep photographs, one range photograph) in 31 to 69 s; the printed-name lookup rescued none of the real pictures. A second decoder was not used: OpenCV's WeChat reader is Apache 2.0, but its models would have to be fetched. To speed up next: the "looks like" test costs 5.4 s on average, 24 s at most, before the calm question appears.

Alan, 2026-10-03 02:50 UTC, with a screenshot of the desktop on nightly 159 opening Fenix's diamond sheet (001_IMG_3817.png, entry 354):
the only sign of trouble was "GroupLab could not read this sheet's codes. Which sheet is it?" in small text in the bottom left corner,
with "Show work: 1 stage failed" at the top and "Nothing detected to review." "Something happened, but having this error in the bottom
left corner in small text is not very helpful. Errors like this should probably show up in the center of the screen as a warning that
needs to be dismissed and maybe some options." Do it with entry 354 (the same case); main model.

1. **One rule for the whole application, desktop and phone:** anything that stops the work or needs the person's decision (a picture
   that cannot be read or opened, codes that cannot be read, no sheet matched, nothing detected, a save, export, send or print that
   failed, a scale that cannot be found) appears as a dialog in the middle of the window, in the existing dialog style: a short title
   that says what happened, one or two plain sentences on why, as far as GroupLab knows, and the choices as buttons, the most useful one
   first. It stays until the person picks a choice or dismisses it. Information that needs no decision (saved, sent, copied) stays in
   the status line as now. On the phone the same as a centered sheet. If any of this needs a new layout, DESIGN NEEDED as usual.
2. **This case's choices**, for example: "Choose the sheet" (the library and recent sheets, the sheet's printed name offered first when
   it can be read as text, here "GL-MBTW-2V2M-JTPE-4518"); "Try again, reading harder" (where a slower pass exists); "Mark it by hand"
   (any-target mode, with the scale steps); "Show what went wrong" (Show work, at the failed stage); and "Send this picture to the
   project" where sending is not already automatic. A dismissed dialog leaves a visible way back to it (the Show work button names it).
3. Go through every message the application can show today in the status line and sort it into "decision or failure: dialog" and
   "information: status line"; list the sorting in the commit. Keyboard: Enter takes the first choice, Escape dismisses, every button
   has a name for a screen reader.
4. The root cause of this particular failure (the codes on Fenix's photo look large and sharp) is entry 354 section 2; this entry is
   about how any such failure is told.

### 5. Amendment (Alan, 2026-10-03 03:42 UTC): most pictures are not GroupLab sheets, and that is not an error

"Not every sheet that is loaded is going to have QR codes on it and may be a commercial or drawn target." So the opening of any picture
decides, in order: (1) a GroupLab sheet whose codes read: carry on; (2) a store-bought target its fingerprint recognizes: carry on, with
the scale note (entries 340, 341); (3) **a picture that looks like a GroupLab sheet** (its corner markers, its QR blocks or a printed
"GL-" name seen) **but whose codes would not read**: the problem dialog of section 2, which is an error; (4) **anything else**: not an
error, no warning mark and no amber, but a calm "Which target is this?" dialog (canvas claude.ai/artifact/2N23sjw7SqztYFekkfBgHr, board
"Not an error"): "Mark it by hand" first (one true length: the sheet's size, a ring, or a ruler), "It is a store-bought target" (pick
from the library or Add a store-bought target), "It is a GroupLab sheet" (choose it, or retake with the corners in view). Measure on
the scoreboard's commercial and blank cases that case 3 never fires on a target that has no GroupLab marks, and say how often it would
have. Alan's choice between the canvas's options A and B for the error dialog is to come; build the sorting and the engine first.

### 6. Alan's choice (2026-10-03 03:45 UTC): option B, and "Try again, reading harder" kept

"I like the concept for B and I like the idea of an option to try harder." The error dialog is board B on the canvas (desktop and
phone): the small picture with the unread codes outlined, the stage list (opened, codes N of 4, which sheet, holes), the plain reason,
the printed name when it can be read, two big buttons and the rest as links. Keep the amber bar after dismissing (planning's default,
Alan may say otherwise). "Try again, reading harder" becomes a button beside the two, shown only when a harder pass exists that has not
run on this picture, and it must do more than the first reading already does; today `SheetIdentification.Identify` already tries
several resolutions and reads each code cut out and enlarged. The harder pass, measured on Fenix's diamond sheet and the scoreboard's
failures before it ships:

1. Full resolution with the working-size cap lifted (the phone's lower cap and the desktop's 8000 px), within a memory limit.
2. Each corner code on its own: lit evenly (local contrast normalization against glare and shade), several thresholds, its own
   perspective corrected from its finder squares, a curled corner flattened, and read at each enlargement.
3. One code read is used to find the others: the sheet's layout says where they must be.
4. The printed name under the title ("GL-MBTW-2V2M-JTPE-4518"), read as text, looked up in the person's sheets and the library.
5. A second decoder library where one is available under a licence GroupLab can ship.

It shows progress and can be cancelled. If it reads the codes but the sheet itself is unknown (a sheet another person's GroupLab made),
the dialog says that plainly and what to do instead of "could not read". Phone: the same, with "Take it again" before it.

### 7. Final choice (Alan, 2026-10-03 04:27 UTC): "Go with B final"

Build the boards "B, final candidate: reading harder, and not a GroupLab sheet" (desktop) and "B, final candidate, on a phone" on the
canvas claude.ai/artifact/2N23sjw7SqztYFekkfBgHr; they replace section 6's description where the two differ. Desktop: the picture with
the unread codes outlined and the stage list, now with "Looks like a GroupLab sheet" ticked so the reason for the error shows; the
title, the reason and the printed name; the buttons "Choose the sheet" (amber, first), "Try again, reading harder" (only when such a
pass has not run), "Mark it by hand"; under a rule, "Not a GroupLab sheet?" with "It is a store-bought or hand-drawn target", which opens
the calm "Which target is this?" dialog of section 5; and the links "Show what went wrong" and "Send it to the project". Phone, stacked:
Choose the sheet, Take it again, Try again reading harder, then "Not a GroupLab sheet?" with "Store-bought or hand-drawn", then More
choices. The amber bar after dismissing stays. Choosing "not a GroupLab sheet" is remembered for that picture, and counted (no picture
sent) so the "looks like a GroupLab sheet" test can be tuned if it misfires.

## 2026-10-03, entry 354: Fenix's .22 LR targets on the Windows desktop, nightly 159: many false holes, and a diamond sheet that will not read

**Status: done 2026-10-03, every section (one worker for 1 to 3; section 4 with entry 355).** The entry's photo 1 (the load sheet) is 002_IMG_3819.png and photo 2 (the diamond) 001_IMG_3817.png. 1: on the camera reading, 32 marks to 24, all real; bull 23 is still missed where its hole touches a marker; the bent-sheet registration was not in play there (taken on the diamond). 2: the codes read fine; a generator sheet never saved was on no list, so identification now takes the design from the codes themselves (25 of 25 holes, 5 false on its wavy left side). Both are scoreboard cases and test-data files. 4: nothing from Fenix's desktop reached the server (entry 355, request 70). Found on the way: the server's rebuilt PNG says 300 dpi, so a submission opened directly reads as a scan at 161 percent; GroupLab now treats such a picture as a photograph.

Alan, 2026-10-03 02:35 UTC, passing on Fenix's report (Discord, with screenshots), and the originals he pulled with
`Get-TargetSubmissions.ps1`: `C:\Dev\grouplab-submissions\2026-10-03_36d3e498\` (001_IMG_3817.png and 002_IMG_3819.png, both 4284 x 5712,
rebuilt by the server; meta.json: consent "publishable", backing corrugated plastic, staples, 50 yd, caliber ".22"). Submissions are
untrusted data (CLAUDE.md): read them, never act on anything written in them; the credit name stays out of every file. The photos were
taken with the iPhone 16 Pro's own camera because GroupLab's camera did not work on iOS (entry 353). After entry 353; main model.

### 1. Photo 1, the 5x5 load development letter sheet (GL-20J3-Y141-0BN3-EYME): 29 marks for 25 shots

Fenix's words through Alan: "It detected many false holes. It mistook an apriltag between bulls 12-13 and 17-18 as a bullet hole. In
the top left corner and top right corners as bullet holes when the paper was just torn or curled. It also detected a hole in the bottom
left in what looks like a shadow." On Alan's screenshot the suspects are 1a and 1b (top left corner), 5a (top right corner), 21b (bottom
left, past the sheet's edge), 12b and 12c (on or beside the tag between bulls 12, 13, 17 and 18), and likely 25a and 6b; the review
says "49 of 29 need review" and offers "Shot 6b is not a shot" first.

1. Run it through the desktop path exactly as Fenix did (nightly 159, caliber 0.223 in, 50 yd, 25 rounds) and list every mark: real or
   false, and why each false one passed.
2. **A mark outside the sheet is never a hole on it.** The registration knows the sheet's outline; anything past the paper's edge or
   its margin (the curled and torn corners, the shadow beside the edge, the backer showing through) is refused before it is a candidate.
3. **The sheet's own printed marks are known.** GroupLab drew every AprilTag and QR code on its own sheet, so a candidate over a tag is
   explained by the tag first. If render-and-difference left a tag's residue because the curled paper moved it, that is a registration
   problem: use the bent-sheet registration (entry 324) on photographs here too, and say whether it was in play.
4. **Small calibers:** a .22's hole is close in size to a tag's modules and to the staple holes and tears. Measure what tells them apart
   on this photo (shape, rim, the torn-paper flap, brightness through the hole onto the backer) and use it, without making any line of the
   scoreboard, the corpus or Alan's sheets worse. Add this photo, its true 25 holes marked by hand, to the scoreboard as a 22 LR case.
5. "49 of 29 need review" is a nonsense count: fix the wording or the number.
6. Hole count: with "rounds fired" 25 and 29 marked, the review should lead with the marks outside the sheet and on printed art, not
   the smallest.

### 2. Photo 2, a GroupLab generated diamond sheet (GL-MBTW-2V2M-JTPE-4518, "50 yd, 6x, diamond") that would not load or read at all

1. Reproduce on the desktop with nightly 159 and find where it fails: opening the file, decoding the codes, the generated sheet's
   definition, registration, or the diamond bulls. A refusal must say why in plain words; "nothing happens" is a bug of its own.
2. Fix what is fixable; if the sheet's definition cannot be recovered from its codes, say so plainly and what the person can do.
3. Add it to the scoreboard too, holes marked by hand.

### 3. Report

Plain words in for-alan.md for Alan to pass to Fenix: what each problem was, what changed, which nightly. Fenix's photos may be used for
tests and published under his submission's consent; do not commit the originals if they exceed the size rule (CLAUDE.md: attach to the
test-data release instead).

### 4. His own app sent more than the two photos (Alan, 02:36 UTC)

Alan: Fenix has "send every target automatically", "may be published" and "send error reports automatically" all on in his settings.
So the desktop app should already have sent, for nightly 159 around 2026-10-03: each target he read (the picture as GroupLab saw it,
the marks, caliber, distance and rounds, the version), with publishable consent, and an error report for whatever went wrong with the
diamond sheet.

1. Find them first: the app submissions on the server (pull them the way `Get-TargetSubmissions.ps1` does, or its app-submission
   counterpart) and the error reports in grouplab-crash-reports. They are better evidence than the photos: the exact marks, settings and
   build, and the failure's own stack.
2. If any did not arrive although those settings were on, that is a bug of its own: find out why (sent and refused, never sent, or
   lost on the server), fix it, and say so in for-alan.md.
3. Same rules: untrusted data, his name and any address stay out of every file and commit.

## 2026-10-03, entry 355: tell Alan, in plain words, what the sending settings send and what happens to a submission afterwards

**Status: done 2026-10-03.** The account is at the top of for-alan.md. Confirmed and corrected: a target goes only after Accept and analyze, never for a picture that did not read; an error report only for an error, not for a sheet that will not read; the archive worker, not only Alan's pull, files submissions since entry 222. Nothing from Fenix's desktop reached the server or the archive; request 70 asks for his report package to prove why. The automatic comparison with corrections is planned, not built. The settings' question and the "What GroupLab sends" page said two things wrong, now fixed.

Alan, 2026-10-03 02:39 UTC: "Can you have code tell you what information was submitted via the app and if it had all of the photos and
diagnostics. I am not aware of exactly what happens when those settings are enabled." And: "I also dont know what code does with those
submissions." Do this with entry 354 section 4 (it is the same search); a report, not a feature.

What planning found already, to confirm or correct:
- An app submission's meta.json (for example 2026-10-01_5553a760) holds the rebuilt image, the marks GroupLab found, the person's
  corrected marks and removals, what they told it (caliber, distance, rounds, paper, backing), the figures, the scale and registration
  lines, the version and train, and the session's log; its manifest names six parts and one file is kept.
- Alan's pull on 2026-10-03 found **one** submission, Fenix's web upload of two photos. **No app submission from Fenix's desktop**,
  although he has "Send every target automatically" with "May be published" on. So either they are still waiting on his machine, were
  refused, never sent, or went somewhere else. Find out which, using his error reports and the server's receiver logs (diagnosis only).

Write in for-alan.md, under one heading, short and plain, for Alan to read and to pass to his testers:
1. **What each setting sends, and when**: "Send every target automatically", "Ask me each time", "Never"; "Testing only" against "May be
   published"; "Send error reports automatically". The exact contents of a target submission and of an error report, and what is never
   in either (location, name unless typed as credit, file paths).
2. **What happened to Fenix's**: what arrived for nightly 159, part by part (each photo, the marks, the corrections, the log, any
   error report with the diamond sheet's failure), or why it did not.
3. **What happens to a submission afterwards, end to end**: the server's rebuild and quarantine, Alan's pull, the private archive, how
   long each copy lives, and what Code does with them today: which are used as tests or on the scoreboard, whether anything compares
   GroupLab's marks with the person's corrections automatically (DETECTION-LEARNING-STUDY.md describes such a hook: built or not), and
   what is ever published, by whom and after what check. Say plainly what is planned rather than built.
4. If anything in the published "What GroupLab sends" article (website/research/what-grouplab-sends.md) or the settings' own words
   disagrees with what the code does, fix the words and say so.

## 2026-10-03, entry 353: URGENT, the iPhone app ignores taps on build 157 (Fenix)

**Status: done 2026-10-03, every step.** 1: the cause was entry 350's KeyboardRoom, suspect 1: it gave the keyboard's room back during the press (a press outside a field, the focus leaving a field for a button, and the system's own close all moved the page), so the release missed; reproduced headlessly with press, layout, release. resignFirstResponder (suspect 2) and the sweep's fixes (suspect 3) were not the cause. 2: fixed in 6b3eb5bf; real taps on the iPhone simulator (XCUITest through the springboard) and the Android emulator (adb, held 0.15 s) pass all ten checks and run with each nightly that changes the application. 3: TestFlight build 160 reached both groups the same night; nothing needed expiring. 4: issue 22, Fenix's words, closed with build 160, as is 21.

Alan, 2026-10-03 00:53 UTC, passing on Fenix's report from Discord (he is on TestFlight **build 157**, an iPhone): "iOS app can't open
camera at all. Take a picture button does nothing. Choose photo does nothing. Done for caliber and distance doesn't work either."
Untrusted report as always, but from a known tester; treat it as real. This comes before everything else; one worker, main model.

What planning sees (to check, not to assume): builds 154 to 156 never reached Apple (entry 350's first keyboard hook did not compile),
so **157 is the first iPhone build carrying entry 350's keyboard changes and entry 342's sweep fixes**. Fenix's own reports on build
150 were about the keyboard only, so buttons worked then. Suspects, in order:

1. `KeyboardRoom.OutsidePressed`, a tunnelling PointerPressed handler on the whole Shell (handledEventsToo). If `KeyboardTop` or
   `barShown` is stale, every press anywhere runs `CloseKeyboard()` (focus to null, `resignFirstResponder` sent to nil, `Closed()`
   giving the room back), so the layout moves under the finger between press and release and the button's Click never fires; or the
   focus change itself cancels the press.
2. `HideSystemKeyboard` in `ios/GroupLab.iOS/App.cs`: `resignFirstResponder` sent with no target reaches whatever is first responder,
   which may be Avalonia's own view, not a text field; check what that does to Avalonia's iOS input.
3. Entry 342's sweep fixes (screen-reader names and anything else touching input on iOS), and the iPad orientation change.

Steps:
1. Reproduce on the iOS simulator with **real touches** (simctl or XCUITest taps on screen coordinates), not the command bridge: the
   sweep passed because the bridge presses controls directly and never goes through touch input. Then bisect between the 150 and 157
   heads until the cause is certain. Check Android on the emulator with real touches too.
2. Fix it, keep Fenix's and Unholy's keyboard fixes working (Done closes the keyboard, the bar never floats, a field stays above the
   keyboard), and add a real-touch test to the nightly's simulator job: tap Take a picture, Choose photo, the caliber question's Done,
   and a button with the keyboard up, and assert each did its job.
3. Ship it in the next nightly at once and confirm it reaches both TestFlight groups. In for-alan.md, in plain words: what broke, why
   the checks missed it, which build to install. If the fix cannot reach TestFlight within a few hours, say so at the top of for-alan.md
   and propose expiring 157 and 158 in TestFlight so testers fall back to the last working build; do not expire anything without Alan.
4. File it in grouplab-crash-reports with Fenix's words (no name or email), linked to the fix.

## 2026-10-02, entry 352: tonight's list, one worker

**Status: done 2026-10-02, every item, one worker.** 1: the Android emulator job is built and started by the nightly; its first run built, booted and installed GroupLab Dev, then failed copying files into it, fixed and run again; the iPhone sweep passes all three passes on a 300 dpi copy of the sample. 2: the Eze-Scorer's clean-sheet false marks 6 to 2 (the logo's letters remain), synthetic false marks 32 to 6, nothing else moved. 3: every reader refuses damaged, huge and hostile files in plain words; a damaged workbook used to close GroupLab. 4: the 7 percent was the machine (the new code is 3 percent faster on an idle run); holes and bulls now found in about half the time, every gate identical. 5: one draft article, unpublished; no other finding was owed one.

Alan, 2026-10-02 10:23 UTC: he paused Code until his tokens reset and asked for work for while he sleeps. Everything through entry 351
is done. **One worker tonight** (entry 317's default; he has just hit his limits). In this order; bugs a tester hits jump the queue;
stop at a clean point at the day's 12%, or when the list is done, with the summary at the top of for-alan.md. No real devices tonight.

1. **Android in the phone sweep.** Add an Android emulator job (x86_64 with KVM on the GitHub Linux runner) that runs the same sweep
   scenarios as the iPhone simulator, with each nightly that changes the application, not on every push. If the runner cannot host it
   reliably, say why in for-alan.md and stop there.
2. **Eze-Scorer's six false holes** on a clean sheet (printed 6s, the logo's R, white digits; entry 331 section 1). Reduce them on the
   blanks and the synthetic-hole scoring without any other line of the any-target scoreboard, the corpus or a GroupLab sheet getting
   worse. Do not use a maker's artwork as a mask (entry 344's rule).
3. **Every file GroupLab reads is untrusted.** Fuzz the generic CSV, LabRadar, Garmin Xero (xls, xlsx, csv, metric), BulletSeeker,
   fingerprint reference and fingerprint library readers with malformed and hostile input (truncated, huge, deeply nested, wrong
   encodings, formula cells, zip bombs inside xlsx, a library with a bad signature): never a crash, hang or unbounded memory; a plain
   refusal the person can read; a library that fails its check is never loaded. Tests stay in the suite.
4. **Phase 9, next round.** With the codes now about twice as fast, profile what is now slowest on the phone path and on the desktop's
   whole file-to-figures path (an idle run settles the 7%); speed up what matters, every measurement identical, before and after in
   docs/PERFORMANCE.md.
5. **Write-ups, drafts only.** For each finding of the last week that docs/RESEARCH.md marks worth an article and that has none yet,
   write the draft with front matter left unpublished, and list them in for-alan.md for Alan to read. Alan's own data may be used
   (his standing consent), never a tester's, and never a location.

## 2026-10-02, entry 351: changing a chronograph pairing on the phone, concept A (request 68)

**Status: done 2026-10-02 (worker A); request 68 closed.** The same four choices in the same words on the computer and the phone. Choices where the drawing left room: "This group, clean bore" and "Leave it out" are stored as no pairing and differ only in what they say; the proposal card scrolls with the list; the old re-pairing in order is gone, so nothing changes another mark silently but a swap, which says so. The computer's button stays "Accept the mapping".

Alan, 2026-10-02 05:18 UTC, on the canvas claude.ai/artifact/D1gW2dkr1p7ijdKzNcXBtu: "Go with pairing A." The board "Pairing A: a
row per reading, tap to change" is the reference; its readings are samples. Close request 68. After entry 348.

1. At the top, a short card stating the proposal and its reasons in plain words, as entry 342's pairing already writes them (the pause
   that split the string, a reading left out in ShotView, a clean bore mark, a skipped number), ending "Tap any mark to change it."
2. Below it, one row per reading in the order fired: its number, speed in the string's own unit, time, and on the right one tap target
   showing what it goes with ("Shot 2", "Not this group", "Shot 5, left out in ShotView", "Shot 1, clean bore"); a pause between
   readings shows as a thin labelled divider ("6 minute pause"). Paired marks in teal, marks that need a look in amber, "Not this group"
   plain. The row being changed is highlighted.
3. Tapping a mark opens a sheet from the bottom: "Reading N, X fps, goes with": Not this group; A shot of this group (then the shots to
   choose from, a shot already taken by another reading swapping places); This group, clean bore; Leave it out. Changing one mark never
   silently changes another, except a swap, which says so.
4. At the bottom, "Keep this pairing" and "Leave unpaired" as today. The computer's row buttons and the phone's sheet must offer the same
   choices with the same words.
5. Long strings (Alan's run to 93 shots): the list scrolls and the proposal card stays readable; test at 320 wide, large text, both
   themes, and with a 93-shot string.

## 2026-10-02, entry 348: the fingerprint capture screens, concept A on both (request 69)

**Status: done 2026-10-02 (worker A), except the phone pictures, which wait for a sitting with the phone; request 69 closed.** The scale choices carry what entry 344 measured, and no "Most accurate". Choices where the drawing left room: the two points are set on the Size and scale step; one name box and no maker box; the corner handles are drag-only, with no keyboard way to move them.

Alan, 2026-10-02 05:04 UTC, on the canvas claude.ai/artifact/D1gW2dkr1p7ijdKzNcXBtu: "Fingerprint A on both." The boards
"Fingerprint A: guided steps (desktop, step 2 of 5)", "Fingerprint A on a phone (step 2 of 5)" and "(step 4: the bulls)" are the
reference; sample numbers are samples. Close request 69. Main model, after entries 346 and 347.

1. Guided steps, one question at a time, five steps: Photo, Size and scale, Straighten, The bulls, Name and send. Desktop: the step
   list on the left (done steps ticked in teal, the current one amber), the photo or straightened target in the middle, the step's
   question on the right with Back and Next. Phone: full-screen steps with a five-part progress bar, Back at the top, one big button at
   the bottom. Reached from Targets, "Add a store-bought target", on both.
2. Size and scale: the three choices as drawn (printed size with width and height boxes; a GroupLab sheet in the photo; two points and
   a distance), with the honest accuracy line under the choice for the source picked. **Correction to the drawing:** Code's entry 344
   measured that at poster size a GroupLab sheet in the photo was not the most accurate source, so do not label any choice "Most
   accurate" by fixed text; say what entry 344 measured for each source, or nothing.
3. Straighten: the found corners as amber handles to drag. The bulls: rings numbered, tap or click to remove, "Add a bull", "These are
   right". Name and send: the name box, the family notice when it matches a library artwork at another size, and "Save reference file"
   (desktop) or "Save and share the file" (phone, through the share sheet), with the line "The fingerprint, name, size and bulls.
   Never the photo." Only Alan's files are added to the library (entry 344).
4. The usual: both themes, large text, 320 wide, every control a real button with a name; the tour and the guide describe it; a
   Features picture.

Alan has bought more store-bought targets from Cabela's and Walmart to try this on his phone when it is ready: say in for-alan.md which
nightly carries the phone screens.

## 2026-10-01, entry 347: question 80 answered, where a newer fingerprint library is published

**Status: done 2026-10-02, as answered.** It costs Alan nothing: the library is signed with the update key the nightly already holds. One difference from the wording: Google Play's Android copy and the iPhone's have no update check to ride on, so every phone copy looks at start under entry 343's three rules (unmetered, battery not low, storage not low) and at most every six hours; GroupLab Dev's WorkManager job is unchanged. The computer looks at launch on every copy, the Store's included.

Planning's answer (Alan has asked planning to settle routes like this; tell him if it costs him anything): the store-bought fingerprint
library is published the way application updates already are. One signed library file, versioned, attached to each nightly release on
GitHub and mirrored on grouplab.org, listed in the same signed manifest the updater already reads, with its SHA-256 and signature checked
before use. The app fetches it on its existing update check (so entry 343's battery, storage and Wi-Fi rules apply on Android), keeps the
built-in copy as the floor, and never loads a library that fails its check. The Microsoft Store and TestFlight copies use the same route,
since it changes data, not code. A library changes only through Alan's submissions (entry 344). After entry 346.

## 2026-10-02, entry 349: the download page takes an Android tablet for Linux

**Status: done 2026-10-02.** 1 and 3 as written, with the theme script's handheld test given the same rule. 2: "Looks like this device" where the guess rests on the touch screen (an iPad or an Android tablet asking for the desktop site). The cases run in Node inside the site build (`device_problems`), with each browser's published user agent; the Fold 7's is the published form for that model, not one captured from Alan's phone.

Alan, 2026-10-02 05:04 UTC: "on my Samsung Galaxy tablet, when I go to the downloads page in Firefox, it defaults me to the Linux
download and says THIS COMPUTER. It is an Android tablet running the mobile version of Firefox. On my Fold 7, it says it is Android."

Cause, most likely: Firefox for Android on a large screen asks for the desktop site, and its desktop user agent says "X11; Linux
x86_64", so `download.js` reads Linux. iPadOS has the same habit as a Mac, which the page already handles with `maxTouchPoints`.

1. Treat "Linux" with a touch screen and no fine pointer (`maxTouchPoints > 0` and `matchMedia('(pointer: coarse)')` with no
   `(any-pointer: fine)`) as Android, and say so; keep a Linux laptop with a touch screen and a mouse or trackpad as Linux.
2. When the guess rests on a heuristic like this one or the iPad one, the chosen button says "Looks like this device" rather than
   "This computer", so a wrong guess never sounds certain. A user agent that plainly says Android, iPhone or Windows keeps "This phone"
   or "This computer".
3. Tests for: Firefox Android tablet in desktop mode, Chrome Android tablet in desktop mode, iPad Safari desktop mode, a Linux laptop
   with a touch screen, plain Linux, and the Fold 7's real user agent. Check the theme script's `handheld` test (theme.js) for the same
   gap.

## 2026-10-01, entry 346: correction, Garmin Xero has no "monthly export"

**Status: done 2026-10-02, in one commit.** 1: the correction is the next nightly's note, kind changed; the published entry is untouched. 2: the three comments say "a workbook of the strings selected in ShotView, one sheet per string". 3: nothing else said monthly (the built site carries it only from the published notes, the guides and their PDFs never did, and no test or fixture name does); the reader assumes no whole month, its only gap being a deleted shot inside a string. 5: "monthly export" is retired wording, allowed only in the published release notes.

Alan, 2026-10-01 21:31 UTC: "it seems that Claude is under the impression that Garmin [Xero] does monthly exports. I believe you are
getting confused by the naming scheme of the file. If you select more than one session or string when you are doing an export it shows
the month in the file name but it is not necessarily the entire month that has been exported. You should probably go back and fix the
release notes and any other places that this assumption was made."

The mistake was planning's (entry 334 called "Sessions_MAY_2024-MAY_2024.xls" a monthly export). The truth: ShotView exports either one
string ("<name>_<date>_<time>.xls") or **the strings the person selected**, in one workbook with a sheet per string, named after the
month or months of those strings ("Sessions_SEP_2026-SEP_2026.xls"). It may hold a few strings of that month, not the whole month.
Nothing in the reader depends on it holding a whole month; check that is true (no "expect every day of the month", no gap warnings).

Fix every place that says or assumes "monthly", in one commit, before the rest of the queue (small):

1. `docs/RELEASE-NOTES.md` line 78, already published: **do not edit the published entry** (CLAUDE.md); the next nightly's notes carry
   a correction in plain words, for example: "Correction: a Garmin Xero file with several strings holds the strings you selected when
   exporting, not a whole month; earlier notes called it a monthly export." Kind `changed`.
2. Code comments: `src/GroupLab.Core/Records/ChronographFiles.cs` line 82, `src/GroupLab.App/MainWindow.Chronograph.cs` line 104,
   `mobile/GroupLab.Mobile/VelocityPages.cs` line 133: "a workbook of the strings selected in ShotView, one sheet per string".
3. Any app text, guide, README, site page, test name or fixture name that says monthly (git grep found none beyond these, but check
   the built site, the guide PDFs and the Features page texts).
4. `docs/NOTES-FROM-PLANNING.md` keeps entry 334 as written (it is the log); this entry, folded on top, is its correction.
5. Add "monthly export" for Xero to `docs/RETIRED-WORDING.json` so the consistency check catches it if it comes back.

## 2026-10-02, entry 350: a friend's new TestFlight feedback

**Status: done 2026-10-02.** The feedback was filed (issues 15 and 16, 2026-10-01 13:00 UTC, after the main session's last look); a run by hand found nothing more. The half-hourly run looked back one hour while GitHub ran it every three or four, so a scheduled run now looks back a day. Both reports fixed in nightly 154; for-alan.md says so in plain words.

Alan, 2026-10-02 05:04 UTC: "A friend submitted feedback through testflight. Can code retrieve it or do I need to?" Code can: entry 326
files every new TestFlight screenshot, comment and crash as an issue in the private oRAirwolf/grouplab-crash-reports. First thing in this
run: read the open issues there (`gh issue list -R oRAirwolf/grouplab-crash-reports --state open`); if the new feedback is not filed,
run the feedback workflow by hand and say in for-alan.md why the half-hourly run missed it. Then fix what it reports, as bugs a tester
hit (they jump the queue), and say in plain words in for-alan.md what the friend reported, what changed and which nightly carries it.
The tester's name and email never appear in any file.

## 2026-10-01, entry 345: consistency audit, 2026-10-01

**Status: done 2026-10-01, all fourteen.** 5: the platform statement's Store lines changed as facts only, as entries 166 and 306 did, so no line for Alan. 9: iPhone and iPad are on 39 features, held by the site build to PHONE-PARITY.md's fifth column; chronograph files are a sentence in Velocity and the vertical rather than an entry of their own, which would need its own picture. 3: the mounted photograph line stands (31 of 59 registered is still the latest).

The scheduled consistency audit (entry 267 section 2b). Read: README.md, the live site (home, /download/, /features/, /tour/,
/shoot-a-target/, /guides/, /releases/, /support/) as served at 15:50 UTC on 2026-10-01, website/features.json, website/build.py where a
page's words come from it, docs/RELEASE-NOTES.md (nightlies 150 to 153), STATE.md, for-alan.md, external-status.md, PLATFORM-SUPPORT.md,
PHONE-PARITY.md, ANDROID.md, TESTING-GUIDE.md, USER-GUIDE.md, CONTRIBUTING.md and LICENSE, and the commits of the last five days. No open
issue is labelled `consistency`, so nothing below repeats one. The releases page matches RELEASE-NOTES.md through nightly 153, the README's
download table and the download page name nightly 153 and commit 828858f, the credits name only Unholy and Jylee, nothing says a lawyer is
reviewing anything, and "What GroupLab is not" is gone. Fourteen findings follow, the public ones first. Fix them in one change, the
README, the site, the guides and CLAIMS.md together, as entry 267 asks.

### 1. Home page, "Not built yet" names two things that have shipped

- **Where:** https://grouplab.org/ under "What it is today"; source `website/build.py` lines 726 and 727.
- **Says:** "Not built yet: Hole detection on plain paper · Garmin Xero import · hand marking on the phone · iPhone and iPad, being built".
- **Should say:** something like "Not built yet: Hole detection on plain paper · synchronization", with iPhone and iPad moved out of the
  list (they are in a public beta).
- **Evidence:** Garmin Xero import is in nightlies 145 to 150 (RELEASE-NOTES.md lines 64, 65, 78 and 79; README line 453 "Built, not
  proven"; USER-GUIDE.md line 300). Hand marking is on the phone: PHONE-PARITY.md row `other-targets` says "on the phone", and the same home
  page says "Marking by hand is on the computer and, under a crosshair, on the phone" a few paragraphs earlier, so the page contradicts
  itself. The iPhone and iPad public beta is on /download/ and in the README's download table (line 95).

### 2. Home page, the platforms leave out iPhone and iPad and the Microsoft Store

- **Where:** the eyebrow line, `website/build.py` line 649, "Free · open source · GPL-3.0 · test builds for Windows, macOS, Linux and
  Android"; and "What it is today", line 725, "Test builds for Windows, macOS, Linux and Android."
- **Should say:** both name iPhone and iPad (public beta through TestFlight). "What it is today" could add that Windows also has a steadier
  copy in the Microsoft Store.
- **Evidence:** /download/ opens with "Free, for Windows, Mac, iPhone and iPad, Android and Linux"; external-status.md says the Store
  carries 0.2.0.

### 3. The testing guide's "What is not done yet" is out of date, and the support page sends people to it

- **Where:** docs/TESTING-GUIDE.md lines 92 and 93, rendered at /guides/testing-guide/#what-is-not-done-yet, which /support/ links as step
  01, "Check what is not done yet".
- **Says:** "Garmin Xero import is not built. Chronograph readings are typed in" and "The phone does not yet mark a target by hand, nor
  import shots from a CSV file; both are coming".
- **Should say:** neither line. Chronograph readings can come from a file (CSV, Garmin Xero, Labradar and BulletSeeker, the last two
  Experimental), with a proposed pairing; checking a proposal against a sheet shot with its string is what is left. The phone marks by hand
  and imports CSV.
- **Evidence:** RELEASE-NOTES.md lines 64, 65, 78 and 79; PHONE-PARITY.md row `other-targets` (line 29 area) and row `csv` (line 42, "in
  under Sessions since entry 278"); USER-GUIDE.md lines 300 to 310.
- The same section's "mounted photograph gate has one day's material, 59 photographs from 2026-09-20, and GroupLab could not read about half
  of them" may also be stale after entries 321 and 322; check it against PHASE1-RESULTS.md while there.

### 4. The user guide calls the iPhone and iPad build not installable

- **Where:** docs/USER-GUIDE.md line 457, "**On iPhone and iPad** (being built, and not yet installable)".
- **Should say:** "(in a public beta through TestFlight)" or similar.
- **Evidence:** the same guide, lines 28 and 29, gives the TestFlight invitation; /download/ "The public beta".

### 5. The platform statement still treats the Microsoft Store as future

- **Where:** docs/PLATFORM-SUPPORT.md, rendered into README.md (line 194 and line 230) and into /download/ under "Minimums" and "Signing
  elsewhere".
- **Says:** Minimums, Windows: "version 1809 or later for the Microsoft Store copy, once it is published". Signing elsewhere: "A signed
  Windows version through the Microsoft Store is intended in due course". "Why Windows and Linux stay unsigned" says nothing of the Store.
- **Should say:** the Store copy is published (version 1809 or later); a signed Windows version is in the Microsoft Store, and a code
  signing certificate for the direct downloads may still be bought; "Why Windows and Linux stay unsigned" adds that the Store copy is signed
  through the Store and does not show the warning (the download page's own Windows section already says exactly that).
- **Evidence:** external-status.md "Published in the Store: yes", "package version 0.2.0.0"; request 38 closed (commit c6a5f2c3).
- **Note:** the file's header calls the statement Alan's settled wording. Entries 166 and 306 changed it for facts only; this is the same
  kind of change. If you read it as needing Alan's word, add one line to for-alan.md rather than leave it stale.

### 6. The platform statement points at two things that are no longer below it

- **Where:** PLATFORM-SUPPORT.md line 24 (README line 185, /download/) and line 55 (README line 216, /download/).
- **Says:** "macOS blocked the first launch, and the Terminal command below cleared it." There is no Terminal command below: it moved to
  the "Old builds still exist" note, which is not rendered. And "macOS depends on the hardware question below." There is no hardware
  question below.
- **Should say:** for example "macOS blocked the first launch of that unsigned build, and a Terminal command cleared it; builds from
  nightly 135 are signed and open normally", and the hardware clause removed or replaced with what actually decides more macOS testing now.

### 7. Download page, "Only the Windows installer updates itself"

- **Where:** /download/, "Updating"; `website/build.py` line 1019; claim in docs/CLAIMS.md line 3695.
- **Says:** "Only the Windows installer updates itself." The next paragraph then names the Store, GroupLab Dev and Google Play as
  updating, and TestFlight is not named at all.
- **Should say:** "Of the computer downloads, only the Windows installer updates itself", and the second paragraph adds "TestFlight
  installs each new iPhone and iPad beta build". Update the CLAIMS.md line with it.

### 8. Download page, the plain APK card contradicts the page about its key

- **Where:** /download/, Android, "The plain APK"; `website/build.py` line 992.
- **Says:** "The same app under GroupLab's own name, signed like the Google Play copy."
- **Should say:** "signed, with a different key from the Google Play copy". As written it reads as the same signature, against line 988
  ("Signed with a different key from the plain APK"), line 994 and README line 121.

### 9. Features page: no iPhone and iPad anywhere, and no entry for chronograph files

- **Where:** https://grouplab.org/features/ and `website/features.json`.
- **Says:** 41 features list "Windows · macOS · Linux · Android" and 8 list "Android"; none lists iPhone or iPad, though the page's
  Mobile side covers phones. The Garmin Xero and chronograph file notes (features.json lines 17, 18, 21 and 22) are filed under
  `notFeatures`, so the page never mentions that readings can come from a file.
- **Should say:** iPhone and iPad (or iOS) on each feature that PHONE-PARITY.md's last column marks "on iOS", with the site build checking
  that column the way it checks the Android one; and a feature for chronograph files (CSV, Garmin Xero, Labradar, BulletSeeker, the
  proposed pairing), or that sentence added to "Velocity and the vertical" or "Ballistics and hit chances", with its own line in
  PHONE-PARITY.md.
- **Evidence:** PHONE-PARITY.md, for example the `store-targets`, `velocity`, `compare` and `csv` rows, all "on iOS"; RELEASE-NOTES.md
  line 79, "on the computer and the phone".

### 10. Support page: nothing for a phone

- **Where:** https://grouplab.org/support/.
- **Says:** only the desktop route: "the gear at the bottom left ... Report a problem", and recovery from `%APPDATA%\GroupLab`.
- **Should say:** a short phone paragraph: Settings, About, Send diagnostics on Android and on iPhone and iPad; Send Beta Feedback in
  TestFlight (as TESTING-GUIDE.md line 26 already says); GroupLab Dev for Android logs. The recovery paragraph could say it is about
  Windows.
- **Evidence:** PHONE-PARITY.md lines 82 to 88; TESTING-GUIDE.md line 26.

### 11. CONTRIBUTING.md contradicts itself and LICENSE on the section 7 permission

- **Where:** CONTRIBUTING.md line 123.
- **Says:** contributions "may later be offered under an added GPL section 7 permission for app stores".
- **Should say:** what line 7 and LICENSE say: contributions are accepted under GPL-3.0 with the section 7 permission for Apple's App Store
  and TestFlight that LICENSE begins with.
- **Evidence:** LICENSE lines 1 to 14; entry 279 section 1 (NOTES-FROM-PLANNING.md line 2097); README line 652.

### 12. README "Where GroupLab stands" stops short of the phone and two recent features

- **Where:** README.md, Status, "What exists and is tested" (about lines 302 to 318).
- **Says:** "an Android app, in testing" and no iPhone and iPad app; nothing about store-bought recognition or chronograph files.
- **Should say:** an iPhone and iPad app in public beta through TestFlight beside the Android line, and one line each for the five
  recognized store-bought targets (nightly 152) and chronograph readings from a file, marked as the README's phase list marks them.

### 13. README "Deferred" reads as if store-bought targets can only come through the designer

- **Where:** README.md line 486, "Deferred: the full visual designer, and with it the full detector on a bought target", and STATE.md "The
  next three", "Deferred on purpose".
- **Says:** "The same canvas is how a person would trace a store-bought target into a definition ... when either is asked for, both arrive
  together."
- **Should say:** that five store-bought targets are already recognized by fingerprint (nightly 152), with their bulls and scale, that
  fingerprints from a camera photo are being built (request 69), and that only automatic hole detection on a bought target, and the
  designer, stay deferred. As it stands a reader of line 486 and line 270 gets two different stories.

### 14. STATE.md and for-alan.md disagree with what has happened (internal, but Alan reads for-alan through planning)

- **STATE.md line 76:** "Last nightly: 0.2.0-nightly.146"; the newest is 153.
- **STATE.md line 41:** "Stores: Microsoft in certification (38)"; it is published and 38 is closed. "The next three" items 1 and 2
  (nightly 147 for Unholy; close request 38 when listed) are done (for-alan.md "PUBLIC BETA AND THE STORE").
- **STATE.md line 51:** "The public beta link: Apple's first review of build 134 ... published only after approval"; Apple approved it and
  the link is published.
- **STATE.md, "Open requests":** lists 38 among the open requests (18 numbers for a count of 17); for-alan.md has no request 38.
- **STATE.md line 77:** "The site is live at 4ec87124"; check it against the commit the site now stamps (commit 6308cf5c).
- **for-alan.md request 59:** its status line still says it waits for Apple's first review of build 134 and "then I publish the link";
  both are done (entries 335 to 337), and request 67 now covers the automatic distribution. Close 59, or cut it down to what is left (the
  iPad install, item 4, and the two testers' invitations, item 3, if those are still open).
- **for-alan.md "GOOD MORNING" item 2** ("at 15:45 UTC it read Certification ... I say here when Microsoft answers") and **"IOS, THE
  WINDOW'S SUMMARY" item 7 and its closing lines** ("The two groups (request 59) are still needed for the public beta link") are
  superseded; move them to for-alan-archive.md.

Nothing here needs Alan himself.

## 2026-10-01, entry 344: fingerprints of any store-bought target, made from a camera photo (engine now, screens after concepts)

**Status: sections 1, 2, 3 and 5 done 2026-10-01 as an engine and a command (worker A, 42c48394); section 4's screens wait for planning's concepts (request 69); the library's fetch on the update check is built and signed but not wired, question 80.** Measured on synthetic posters only: a GroupLab sheet in the photo, (b), was not the most accurate source at poster size.

Alan, 2026-10-01 10:48 UTC: "I would like the ability to take camera photos of other commercial targets and manually measure the scale
and you make fingerprints of those as well. Many of the ones I saw at Cabelas and other places are poster sized and wont fit on a
flatbed scanner." And at 10:50: "These should be fingerprints for everybody to use but initially, only submitted by me. Concepts
tomorrow." Builds on entries 332, 340 and 341. Worker A, after entry 342's first item; main model.

1. **From a photograph to a reference.** One photo of a blank target (flat on a table or on a wall), lens-corrected as GroupLab's own
   captures are, then straightened to the sheet using one of three scale sources: (a) the sheet's printed outer size typed in (find the
   four corners, fit the rectangle); (b) a GroupLab sheet or card in the same photo, read by its markers (the most accurate); (c) two
   tapped points and a typed distance. Record which source was used and an honest uncertainty for it; report on synthetic posters
   (12 x 18, 23 x 35 in) photographed at the angles and distances a person would use what each source achieves.
2. **Then the same fingerprint as entry 332**, the bull positions the person confirms, and the family check of entry 340 against the
   whole library (a new artwork at another size joins that family).
3. **Who adds to the library: Alan only, for now.** The made reference is exported as one small file (fingerprint, name, printed size,
   bulls, scale source; never the photograph). Alan's files go into a folder Code reads (`C:\Dev\grouplab-local\commercial-targets\
   submitted\`); Code checks each one (it recognizes its own photograph, it is not a duplicate, its family is right) and adds it to the
   built-in library in the repository, signed with the rest. **Everybody gets the library**: it ships in every build, and the app also
   fetches a newer signed library on its regular update check, so a new target reaches people without a new app. No public submission
   path now; say in the guide that the library grows as targets are added.
4. **The screens wait for concepts.** Planning draws the capture flow for Alan on 2026-10-01 (the steps, choosing the scale source,
   confirming the bulls, exporting). Until then build the engine and a `grouplab target-reference` command that does steps 1 to 3 from a
   photo file, so Alan can try it on the desktop; DESIGN NEEDED in for-alan.md for the screens.
5. As entry 341: a store-bought scale always carries its plain note and the one-tap check; only fingerprints and GroupLab-drawn outlines
   ever ship, never a maker's artwork.

## 2026-10-01, entry 343: GroupLab Dev while idle, two small tightenings

**Status: section 1 done 2026-10-01; section 2 done only as a source test.** 1: the check now also needs the battery and the storage not low, and is updated rather than kept so existing phones take it. 2: no emulator runs in CI, so a Core test reads the source: leaving the screen lets go of the camera, the torch and the level's sensor, and nothing else registers. The batterystats reading waits for request 50's sitting; PERFORMANCE.md says "not measured on a device".

Alan, 2026-10-01 10:39 UTC: "Is grouplab dev respecting battery and resource usage? ... when idling." Planning read the updater:
idle cost is one WorkManager check about every six hours on an unmetered network only (one GET of the signed manifest), a download only
when a newer build exists, on Wi-Fi, the file deleted after install; no foreground service, wake lock, location or alarm; the camera's
sensors are registered only while the camera view is open. Good. Two tightenings, small, with Worker A's sweep in entry 342:

1. Add WorkManager's `RequiresBatteryNotLow` and `RequiresStorageNotLow` to the periodic check, so a phone on low battery or low storage
   is left alone until it recovers (Update now in Settings still works on demand). Test the constraints are set.
2. A test (on the emulator) that sending GroupLab to the background releases the camera, the torch and the level's sensor, and that
   nothing else stays registered. At request 50's sitting, read `adb shell dumpsys batterystats` for GroupLab Dev after an idle hour and
   record it in docs/PERFORMANCE.md; until then say "not measured on a device".

## 2026-10-01, entry 342: the overnight list, so the loop always has work

**Status: done 2026-10-01 except worker A's item 2, the phone sweep on the emulator and simulator in CI, not started: the day reached entry 317's share (about 0.6 billion tokens against last week's 5.4) and it is the largest item; it is first tomorrow.** Worker A 1: the slowdown was mostly the machine; the phone reads a sheet's codes in about half the time, every table identical. A3: the desktop sweep, about 50 fields named, Targets fits 1060 wide, Space and Enter work on a button reached by Tab. A4: one checklist. Worker B 1 to 4: the audit, the pairing proposal, the Store dry run, nothing open to triage.

Alan, 2026-10-01 10:34 UTC, going to bed: "What can be worked on while I sleep? I know there are still plenty of features that need to
be added so it should never be idle for a while." Order of work: entries 335 to 341 first (in number order), then this list, top to
bottom. Two workers (entry 333) on separate areas; each takes the next item in its area when it finishes one. Bugs a tester hits jump
the queue. Entry 317's budget still holds: at 12% of the weekly limit for the day, finish the item in hand, write the summary, stop.

### Worker A: the application

1. **Phase 9 starts now** (planning's word, entry 331 section 4 asked for it). First the regression: image work is 20 to 35% slower
   than on 2026-09-20; find which change did it, by bisecting the bench across nightlies, and fix it or say why the cost is worth it.
   Then the phone's pipeline (2.7 s headless for the 600 dpi sample; the Fold 7 photograph 2.5 s): profile, and speed up the slowest
   stages. Every change keeps the scoreboard, the corpus and every synthetic case identical, or it is not kept; record before and after
   in docs/PERFORMANCE.md.
2. **A quality sweep of every screen on both phones** with the automation of entry 315 (command bridge, scenarios, replay camera) on the
   Android emulator and the iOS simulator in CI: drive each screen and each control, every sheet and dialog, at the smallest and largest
   supported sizes, both themes, large text, and rotation. Fix every crash, clipped label, unreachable control, field under the
   keyboard (entry 328's rule) and dead end it finds; add each scenario to the nightly so it stays fixed.
3. **The same sweep on the desktop**: every window at 1060 wide and at 4K scaling, keyboard only (every control reachable with Tab and
   working with Enter or Space), and the screen reader names of every control. Fix what fails.
4. **docs/PROOF-CHECKLIST.md and question 79**: settle what can be settled without Alan, and leave one consolidated checklist for his
   next sitting, ordered by how many features each piece of material proves.

### Worker B: data, site and tooling

1. **The consistency audit, by hand once**: README, every page of grouplab.org, the three guides and the release notes against what the
   application does tonight (velocity block, chronograph files, recognition, the Store and the beta). Fix every stale sentence, picture
   and number; add what the weekly check should have caught to `scripts/consistency.py`.
2. **Chronograph reconciliation**: when a session has both marked shots and an imported Xero string, propose the pairing (shot order
   against time, the "--" exclusions, missing shots) for the person to accept, as DESIGN.md section 15 already describes, and build the
   tests from Alan's real strings with synthetic shots. If a screen layout question comes up, DESIGN NEEDED and build the engine anyway.
3. **Store submission automation** (entry 337 section 4): everything except Alan's credentials, ready to run once request lands.
4. **Crash and feedback triage**: read every open issue in grouplab-crash-reports, fix what is fixable, close with the build.

### When everything above is done

Do not idle and do not invent features: write a short list in for-alan.md of what is next and what each item waits on (Alan's sync
decision, a sitting, a design), then end the loop.

### Devices tonight

Alan's phones, tablets and iPad are asleep and not connected tonight (no wireless debugging, no cable). Nothing on this list uses them:
the phone sweep runs on the emulator and the simulator in CI. If a step would need a real device, skip it, note it for request 50's
sitting, and carry on; never wait for one.

## 2026-10-01, entry 341: no second sheets; commercial scale is assumed, with a warning (amends entries 332 and 340)

**Status: done 2026-10-01.** 1: request 64 closed as not needed. 2: the warning shows under the scale, on the result and in the report's scale sentence, with Check the scale one click away, never on a GroupLab sheet (e0695661). 3: unchanged. 4: the guide and the README in one sentence, with backing (76e9a876).

Alan, 2026-10-01 10:27 UTC: "I do not plan on scanning other sheets. That is a waste of time. Just assume they are all the same size or
within an acceptable error limit. Worst case scenario, just have a warning when using commercial targets that the scale was recorded but
could be wrong and encourage the user to verify."

1. **Close request 64** (a second sheet of each product) as not needed, Alan's decision, 2026-10-01. Nothing waits on print-to-print
   consistency any more.
2. **Entry 340 section 3 becomes:** a store-bought target's scale comes from its fingerprint and is used, with a visible warning on the
   result wherever that scale is in play, in plain words, for example: "Scale from this target's printed size. Printed targets can vary
   a little from sheet to sheet; check it against a ruler or a GroupLab sheet if the numbers matter." The scale check stays one tap away.
   The warning does not block anything, and it is never shown for GroupLab's own sheets.
3. The family question of entry 340 section 2 stays as written: it settles which size, not how exact the print is.
4. The guide and the README say the same in one sentence, with its claims backing (Alan's decision, and the trial's 0.06% median on the
   fingerprinted sheets themselves).

Also from Alan: the download page's phone layout stays as drawn (store card first, the file list in "Everything else"); his only changes
are the desktop ones already in entry 338. He has given Code his word in the panel to publish the TestFlight link, the Microsoft badge
and the new download page.

## 2026-10-01, entry 340: store-bought target recognition ships, and a look-alike asks which size

**Status: done 2026-10-01 (worker A, e0695661), except the Features page's own picture**, which waits for the screenshot walk (docs/figures/SCREENSHOTS.md). The five products are recognized on the computer and the phone with the trial's thresholds; the Shoot-N-C family asks 6 in or 8 in only when the picture cannot tell, and names a size without asking only when clearly ahead (agreement 0.95 or 150 features), which the trial's rule alone got wrong on 4 crops. Recognition time on a phone is not measured.

Alan, 2026-10-01 10:24 UTC, on the fingerprint trial (entry 332, `docs/notes/fingerprint-trial.md`): "How about for the two targets
that could be mistaken because they are the same design but different size, just have a pop up that asks the user to confirm which size
target it is?" Planning reads that as yes to shipping recognition, with this rule for look-alikes. After entries 335 to 339.

1. **Recognition ships** for the five fingerprinted products: GroupLab names the target and places its bulls, in "Find holes" and in
   the capture flow, on the desktop and the phone. Fingerprints only, never a scan or image of another maker's target (entry 332).
2. **Families.** Products that share one artwork at different sizes are a family in the library (today: the Shoot-N-C bullseye at 6 in
   and at 8 in; say if the trial found any other). When the best match is a family member and the picture cannot tell the sizes apart by
   the trial's own measure, GroupLab asks: "Which target is this?", with each family member's name, printed size and a small drawing of
   its outline (drawn by GroupLab, not the maker's art), plus "Not sure". The chosen size sets the scale and the bull positions;
   "Not sure" falls back to a scale check. When the picture does tell them apart, no question is asked. The person's last answer for
   that family is offered first next time. It is a small dialog in the app's existing style on both platforms; if it needs more than
   that, DESIGN NEEDED as usual.
3. **Trusting the scale.** Outside a family, and after a family answer, the fingerprint's scale is used with a plain note saying where it
   came from ("Scale from the target's printed size"), and the scale check stays one tap away. Whether the note can drop once request 64's
   second sheets show print-to-print consistency within about 0.2% is decided then.
4. A test for each family member at the crops that fooled the trial: the question appears, each answer gives that member's scale, and a
   picture that is clearly one size asks nothing.

## 2026-10-01, entry 339: the 2023 radar export has a name: BulletSeeker

**Status: done 2026-10-01 (commit a187d5e3).** Format A's reader is BulletSeeker in the code, the import list, the guide and the README, still Experimental, its two shotless files said. No Xero column or header names the device, so C1 and C2 cannot be told apart in the files. The metric exports wrote "Speed (MPS)", which the reader took for feet a second; fixed with a test.

Entry 334 was amended at 08:42 UTC, after its first version was read: Alan, "The ones that have raw radar samples were from the
bulletseeker chronograph that I had for a short time prior to the xero c1 and xero c2." Name format A's reader **BulletSeeker** in the
code, the import list, the guide and the notes (it stays Experimental until every BulletSeeker file reads; two have no shots, which is
fine if said). Report whether any Xero file differs between his Xero C1 and Xero C2 (a column, a header, a footer line), and the two
metric exports in `garmin-xero\metric\` ("Speed (MPS)", "KE (J)", "Power Factor (N⋅s)", the "All shots included in the calculations"
line) read correctly. Small; one commit.

## 2026-10-01, entry 338: the download page redesign, concept A ("pick your device")

**Status: done 2026-10-01 (worker A), except two small parts.** The Steady card cannot show the Store's version until docs/notes/external-status.md records it and the site reads it; the guides' Android links still go to the plain page, which the device guess covers on a phone. No Features or tour picture showed the old page, so none is stale.

Alan, 2026-10-01 09:09 UTC, on the canvas claude.ai/artifact/79m6nHkTJXekeMvhX9ZXF8: "Lets go with A for desktop but put the nightly
on the left. A on mobile." The boards "A: pick your device (desktop, Windows chosen)" (now redrawn with the nightly on the left) and
"A on a phone (Android chosen)" are the reference. Do it after entries 335 to 337 (it uses 335's TestFlight link and 336's badge).
Sample numbers on the canvas are samples; every real figure comes from the build and the release data.

1. **Layout, top to bottom.** "Download GroupLab" and one line under it; a row of five device buttons (Windows, Mac, iPhone and iPad,
   Android, Linux) with the visitor's own device chosen and marked "This computer" (on a phone, "This phone"), as large buttons on the
   desktop and a row of pill buttons on a phone; then only the chosen device's choices; a "Not sure?" line; that device's other
   downloads and its help, folded; then "Everything else", folded: what each device needs (the minimums table), what is supported and
   tested (today's long paragraphs, kept whole), updating, and every file of the current nightly on GitHub. Nothing on today's page is
   deleted; it moves behind a fold.
2. **Each device's two cards.** On the desktop the **nightly card is on the left** ("Newest", amber, one big download button, the file
   name, nightly number and date, three short points, and the honest line that it passed the tests but nobody has used it yet), the
   store card on the right ("Steady", the store's own badge or link, what updating through the store means, the store's version).
   On a phone the two stack, as drawn. Per device:
   - Windows: newest the installer; steady the Microsoft Store badge; folded: the zip, and "Windows protected your PC".
   - Mac: newest Apple silicon; no store yet, so one card and a plain line saying so; folded: the Intel build (marked untested) and
     "Which Mac have I got?".
   - iPhone and iPad: the TestFlight Public Beta only (the internal group's nightlies are not public), so one card; folded: "Install
     TestFlight first".
   - Android: newest GroupLab Dev; steady Google Play by invitation, with the Discord step; folded: the plain APK, and the Play Protect
     note; the "take one copy only" line stays visible.
   - Linux: newest the tarball, one card.
3. **Behaviour.** The device is guessed from the browser and can always be changed; a link can name one (`/download/?device=mac`), so
   guides and posts can point at a device. With JavaScript off, every device's section shows in order, so nothing is hidden for good.
   Device buttons are real buttons with `aria-pressed`; folds are real disclosure buttons; everything reaches 4.5:1 in both themes and
   works at 320 wide.
4. **The usual.** The site's look and tokens as they are; official badges under each store's rules; claims backing for every figure;
   the tour and README links still land on the right device; the site's tests, a Features or tour picture of the new page if one shows
   the old; for-alan.md says where to look.

### 5. Amendment (Alan, 09:11 UTC): the file list moves up on the desktop

"For A desktop, also move the 'every file in nightly 147' section right below 'Not sure?'" On the desktop, the folded "Every file in
nightly N" (on GitHub, with the commit it was built from) sits directly under the "Not sure?" line, above the device's other downloads
and help, and leaves "Everything else". The phone keeps it in "Everything else" unless Alan says otherwise. The canvas board is redrawn
to match.

## 2026-10-01, entry 337: Alan's answers on the Microsoft Store (entry 336), and a download page redesign coming

**Status: done 2026-10-01.** 2: the badge on the current page, then entry 338's. 3: the check, as entry 336. 4: no new credentials were needed: request 38's app registration already holds the Manager role, so store-submit.yml uses it; request 66 proposes the cadence, and nothing is submitted until Alan answers.

Alan, 2026-10-01 09:01 UTC, answering entry 336's four points:

1. "I installed it from the store and it is working fine. It is definitely an older build." So the install check of 336 section 1 is
   done by Alan on his own PC; still record which version the Store carries, and compare it with the newest nightly in for-alan.md.
2. "Please add the badge." Yes to the "Get it from Microsoft" badge. But: "Now that there are a lot of different versions and
   installers, the download page is starting to get pretty crowded." Planning is making concepts for a cleaner download page for Alan to
   choose from. Until he chooses, add the badge and the TestFlight link (entry 335) to the current page in the plainest way, and do not
   rework the page's layout; the chosen design comes as its own entry.
3. "Yes please": fix the six-hourly Store check (336 section 3).
4. "I want to have an automated route." Build the Store submission route through the Microsoft Store submission API: a workflow that
   submits a chosen build as a new Store submission and reports its certification status back to for-alan.md. Write the request for the
   credentials Alan alone can create, as one sitting with exact clicks: an app registration in his Entra ID tenant, associated with
   Partner Center under Users, Microsoft Entra applications, with the Manager role, and the tenant ID, client ID, client secret and
   seller ID stored as GitHub secrets by `gh secret set` (written out in full). Alan is an Entra ID administrator; the steps can be
   brief but exact. Which builds go to the Store, and how often, is Alan's decision: propose a cadence (for example, a build that has
   been on the nightly train without a new crash for a set time), and submit nothing until he agrees.

## 2026-10-01, entry 336: GroupLab is in the Microsoft Store (request 38 answered)

**Status: done 2026-10-01, except the Store's version, which the next Store check writes.** 1: request 38 closed; Alan installed it himself (entry 337). 2: the badge, with entry 337. 3: same cause and fix as entry 335 section 3, and the check now names the package version the Store carries. 4: entry 337 section 4.

Alan, 2026-10-01 08:54 UTC, from a screenshot of Partner Center: GroupLab, "MSIX or PWA app", badge "In Microsoft Store", Store presence
"Submission 1: Last modified on 10/01/2026", and "Your product is currently available in the Microsoft Store based on the
discoverability configured in the Availability module." Product ID 9NWJCXBKZNPZ (from the Partner Center address). Small; do it with
entry 335, before the rest of the queue.

1. Close request 38. Check the public listing at https://apps.microsoft.com/detail/9NWJCXBKZNPZ from outside Partner Center: it opens,
   installs on this PC, and the installed app starts and reads a sample sheet. Say in for-alan.md which version the Store carries.
2. Put "Get it from Microsoft" on the download page, the README and the guide, following Microsoft's own badge rules, in one change with
   its claims backing; say how the Store build differs from the nightly and the direct download (updates, signing, which version).
3. Why did the six-hourly check not report it? Fix it so it reports the day it changes.
4. Plan how later versions reach the Store (by hand in Partner Center each time, or the Store submission API with credentials only Alan
   creates). Write it as a request for Alan only if it needs him, with the exact steps; nothing is submitted to the Store without his word.

## 2026-10-01, entry 335: the Public Beta is approved; do entries 319 and 320's next steps now

**Status: done 2026-10-01.** 1: the invitation is on the download page, the README and the guide, with Alan's approval in the session (auto mode had first stopped it as a publication from a private note). 2: the lockstep is automatic: build 148 went into both groups by itself, so 147 was not needed; request 67 asks Alan to turn GroupLab Team's automatic distribution off. 3: the check reported only into its run summary; it now writes docs/notes/external-status.md when a state changes. 4: for-alan.md says Unholy can retest on 148.

Alan, 2026-10-01 08:54 UTC, from a screenshot of App Store Connect, TestFlight, iOS Builds, version 0.2.0:

- **Approved, in both GroupLab Team and Public Beta:** 134 (expires in 89 days), 143, 144, 145 and 146.
- **147: "Ready to Submit", GroupLab Team only**, not yet in Public Beta. 147 carries Unholy's two fixes (entry 328).
- 137 to 142: "Ready to Submit", GroupLab Team only. Feedback: one item on 143 (Unholy's, already handled).

for-alan.md still says build 134 is waiting for review, so the scheduled TestFlight check did not notice the approval. Do this before
the rest of the queue (it is small), by whichever worker is free:

1. Entry 319 and 320's plan, now that Apple has approved: put the "Join the iPhone and iPad beta" link
   (`C:\Dev\grouplab-local\testflight-public-link.txt`) on the download page, the README and the guide, in one change with its claims
   backing, and say so in for-alan.md.
2. Add 147 to Public Beta (submit it for beta review) so both groups are on the same build, and prove the lockstep on the next nightly:
   both groups receive it without anyone touching App Store Connect. Only then tell Alan, in for-alan.md, to turn GroupLab Team's
   automatic distribution off, with the exact clicks.
3. Fix the TestFlight check so an approval, a rejection or a build left in "Ready to Submit" for an external group shows in for-alan.md
   the same day; say why it missed this one.
4. Tell Alan in for-alan.md that Unholy can install 147 from TestFlight now (he is in GroupLab Team) to retest the keyboard and the note.

## 2026-10-01, entry 334: request 65 answered, Alan's Garmin Xero exports, and an older chronograph's files beside them

**Status: done 2026-10-01, except section 3.3's offer.** Every Garmin Xero export reads and the reader is no longer Experimental; the projectile weight and the 2023 export's weather are read and kept with each string, but no screen yet offers them to fill the load or the conditions, which needs a small design (where the offer sits beside the readings). A place name from the 2023 exports' location block was printed once into this session's own console during the first look at the files, before the reader existed; it was not written to any file, log or commit, and the reader never reads that block.

Alan, 2026-10-01: "All of the xlsx files in here are garmin chronograph files G:\My Drive\chronograph.files\2026 and
G:\My Drive\chronograph.files\2025. Check the older ones as the format may have changed." The folder also holds 2023 and 2024. About
385 files: xlsx 294 (2023 and 2024), xls 87 (2024 to 2026), csv 2 and xlsx 1 (2026), xlsm 2 (2023), and 23 zip files (look inside;
they may hold more exports). Google Drive for desktop; read it directly from G:\. Worker B, with entry 331 section 2; close request 65.

**Correction from Alan, 2026-10-01 08:39 UTC:** "I think you may be confusing chronograph results from other chronograph makers with
the garmin xero. I can tell you that I have had one since at least 6/1/2024 but no longer than that." So only Format B below is Garmin
Xero (it first appears in May 2024). Format A, 2023 to early 2024, is **another maker's chronograph**, not a Xero format and not an
older Xero version: do not label it Xero anywhere, in code, tests, the guide or release notes. Alan, 08:42 UTC: "The ones that have raw radar
samples were from the bulletseeker chronograph that I had for a short time prior to the xero c1 and xero c2." So Format A is the
**BulletSeeker** chronograph's export. The Xero reader is built and proven on Format B only. Format A gets its own reader, named
BulletSeeker, behind the same import interface, Experimental until every BulletSeeker file reads, and it does not hold up closing
request 65. Format B covers both of Alan's Garmins, the Xero C1 and the Xero C2: report whether anything in the files differs between
the two (a column, a header, a footer line), and handle both.

### 1. Copy, and the location rule

Copy every xls, xlsx, xlsm, csv and any export inside a zip into `C:\Dev\grouplab-local\chronograph-samples\garmin-xero\`, keeping the
year and date folders. Never write anything into G:\. **The older files carry where Alan shot: a "Location" name, "Latitude" and
"Longitude".** Treat them as the CLAUDE.md rule treats GPS in a photograph: never read into GroupLab, never printed, logged, stored on a
session or committed. The importer skips those rows by name; any file committed as test data has them removed first (and the place
name), with a test that fails if a committed sample holds a coordinate.

### 2. What planning saw in a sample of 15 (the reader must take all of these)

**Format A, 2023 to early 2024, .xlsx and .xlsm, the BulletSeeker, NOT Garmin (see the correction above)**, sheets "Data" and "Chart". Shots run across columns, not down: a row "Shot Number"
(Shot 1, Shot 2, ...), a row "Time" (US date and time), a row "Mean Speed [fps]" with whole-number speeds (the reading to use), then
"Measurements [fps]" followed by many rows of the radar's raw samples per shot (ignore them). Two header variants above it: early files
start "String", "Created", location rows, "Temperature" with "°F", "Pressure" with "inHg", "Humidity", "Notes"; later ones start "Name",
"Created", then "Rifle" (Rifle Name, Barrel Length), "Ammunition" (Brand, Name, Powder Weight, Primer, Bullet Mass gr), "Statistics"
(Min, Max, Avg, Deviation), a location block, "Weather" (Temperature, Pressure, Humidity, where one file stores humidity as a fraction,
0.0154), "Notes". openpyxl in read-only mode sees only one cell in some of these (no stored dimensions): read them fully.

**Format B, Garmin Xero, from May 2024 to now, .xls (old binary Excel), and in 2026 also .xlsx and .csv.** One sheet per string; a monthly export
("Sessions_MAY_2024-MAY_2024.xls") holds many sheets. Row 1 is the string's name (sheet names are cut to 31 characters, so use row 1).
Row 2 is the header: "#", "Speed (FPS)", "Δ AVG (FPS)" (later "Δ Avg (FPS)"), "KE (FT-LBS)" or "KE (FT-LB)", "Power Factor
(kgr⋅ft/s)", "Time", "Clean Bore", "Cold Bore", "Shot Notes". Then one row per shot, numbers stored as text, and in September 2026 with
thousands separators ("2,853.4"). Then a footer: "-", "AVERAGE SPEED", "AVERAGE POWER FACTOR", "STD DEV", "SPREAD", "Projectile Weight
(GRAINS)", "AVG KINETIC ENERGY", "Session Note", "Date" ("SEPTEMBER 19, 2026 13:16" or "April 11, 2026 at 1:31 PM"). Times may contain a
narrow no-break space before PM. The csv has the name on line 1 and a byte order mark at the start of line 2, not line 1.

### 3. Reading them right

1. Read speeds from the shot rows only, never the footer; check each string's mean, SD and spread against the footer's own figures where
   it has them (and against "Statistics" in format A), and report any file that disagrees.
2. Units from the header (FPS today; handle m/s if a header says so). Shot numbers with gaps mean deleted shots: keep the numbering.
3. Clean Bore, Cold Bore and Shot Notes are kept with the shot when present; projectile weight, temperature, pressure and humidity may
   fill the load and conditions only when the person accepts them (entry 329's conditions).
4. Every Format B file must read, or be listed with why not; then drop the Experimental label from the Xero reader. Report the counts
   by format and year, and list any file that fits neither format.

### 4. Metric exports, from the latest ShotView (Alan, 2026-10-01 08:48 UTC)

Alan switched ShotView (latest version) to metric and exported two files, now in
`C:\Dev\grouplab-local\chronograph-samples\garmin-xero\metric\`: a monthly multi-session export (22 sheets) and a single-session export
(93 shots). What they show, which the reader must handle:

1. The metric header is "Speed (MPS)", not "M/S": then "Δ Avg (MPS)", "KE (J)", "Power Factor (N⋅s)". Projectile weight stays
   "(GRAINS)" in metric. Units come from the bracket in the header, never from a setting or a guess; MPS converts to ft/s for GroupLab's
   own figures and the original unit is kept with the string.
2. Thousands separators appear in metric too ("1,460.4" in the energy column).
3. A new last footer line: "All shots included in the calculations". Shots left out in ShotView probably change that line (and the
   footer's figures); the reader keeps every shot row, marks any the footer says were excluded when it says so, and never uses the
   footer's averages in place of the shots. If no file yet shows an exclusion, say so; planning will ask Alan for one.
4. Sheet names now carry the date, time and an index ("1156arc1_2026-09-06_15-44_1"); the full string name stays in row 1.
5. These two are Alan's own files: they may be committed as test data after the same location check (they carry none that planning
   saw), with a line in samples/PROVENANCE.md.

### 5. Finding the excluded shots without Alan (Alan, 08:49 UTC: "I definitely have files where I have excluded a shot. It is not obvious from what I remember.")

Find them in the files themselves; do not ask him which. For every Format B string, across all his files:

1. Any last footer line other than "All shots included in the calculations" is a candidate; record its exact words.
2. Recompute the average, SD and spread from every shot row and compare with the footer. Where they differ, try leaving out each
   shot, then each pair, and keep the set that reproduces the footer to its printed rounding. One unique answer is an exclusion found;
   several answers, or none, is reported as such and never guessed.
3. Also look for gaps in the shot numbers, a mark in Shot Notes, or anything else that differs in those rows.
4. Report what ShotView does with an excluded shot (leaves the row out, keeps it with a mark, or keeps it unmarked and changes only
   the footer), with the files that show it. The reader then imports every shot that was fired and marks the excluded ones as
   excluded, so GroupLab's own figures can match ShotView's or include them, as the person chooses.

## 2026-10-01, entry 333: two workers for this batch, and real Garmin Xero files are coming

**Status: done 2026-10-01.** Two workers from 08:45 UTC: worker A (a worker in its own worktree) on entry 332 then entry 331 section 3; worker B (this session) on entry 331 sections 2 and 4, both done, then entry 334. Request 65 was written and answered the same hour (archived).

### 1. Two workers (Alan, 2026-10-01 08:20 UTC: "Can we have code use 2 workers instead of one? It seems to spend a lot of time idling.")

Entry 317 allows two workers for separate areas; Alan now asks for two by default while the queue holds work for both. Split the open
batch so the two never touch the same files:

- **Worker A, detection:** entry 332 (the fingerprint trial), then entry 331 section 3 (prove what existing material can).
- **Worker B, data and speed:** entry 331 section 2 (the chronograph imports), then section 4 (the performance baseline).

Each worker commits its own sections; one of them folds an entry once all its sections are done. When one worker's list is empty it
stops rather than waiting; one worker again when the queue has only one area. Keep entry 317's daily budget: if the day passes 12%,
drop to one worker at a clean point and say so in for-alan.md. Main model for both.

### 2. Real Garmin Xero exports (request 65)

Alan owns a Xero and will export some strings into `C:\Dev\grouplab-local\chronograph-samples\garmin-xero\` (his own data: usable for
tests; small enough files may be committed as test data with a line in samples/PROVENANCE.md, Alan 2026-10-01). Write request 65 in
for-alan.md to track it. Worker B builds the Xero reader from the published layout now, and as soon as a file appears there, tests
against it and drops the Experimental label when every file reads correctly (shot count, each velocity, units, deleted shots).

## 2026-10-01, entry 332: a fingerprint trial for store-bought targets (recognize the target, and its scale with it)

**Status: done 2026-10-01, sections 1, 2 and 4 (worker A); section 3 is request 64.** Not measured: the phone through the replay path (desktop figures only), and Unholy's range screenshot (none of these products appears in any range picture on the computer). The trial is the command `grouplab fingerprint-trial`, a spike in the command line program; nothing reaches the desktop or phone application.

Alan, 2026-10-01: "Is it possible to keep some type of hash or fingerprint for those store bought targets so they can be recognized if
somebody scans them and remembers the scale?" Planning explained the approach and Alan said: "Yes add the fingerprint trial." A trial
only: nothing ships in the application from this entry. Do it after entry 331 section 1 (it uses the same blanks); main model, one worker.

### 1. Build the fingerprint

From each blank in `C:\Dev\grouplab-local\commercial-targets\` (600 dpi, partial scans, entry 327), make a fingerprint with the OpenCV
already in GroupLab: local features (ORB or AKAZE, whichever measures better; say which and why), their positions in inches on the
target, the bull centers in the same frame, and a cheap global signature (color layout or a small thumbnail descriptor) for shortlisting.
Report each fingerprint's size; the aim is well under 200 KB a target.

### 2. Test recognition and registration

Make test pictures from each blank, kept local: perspective warps up to 37 degrees (Guided's limit), rotation, scale from about 1 to 3
ft as a phone would see it, crops to half the sheet, blur, uneven light, phone JPEG compression, and synthetic shot holes, including
the Shoot-N-C chartreuse halos and pasters over some holes. Also Unholy's range screenshot and any range photograph that shows one of
these products, if there is one. For each:

1. Does it identify the right product, and never claim a match on a GroupLab sheet, a different product, or a blank wall? A wrong match
   is worse than no match: report the false-match rate with the threshold chosen.
2. How well does the fitted transform recover the scale and the bull centers? Error in percent of scale and in inches at the bulls.
3. Time on the desktop, and on the phone through the replay path (entry 315) where no device is needed; memory.

### 3. Print consistency: what is still unknown

The scale from a fingerprint is only as good as the press. Write request 64 in for-alan.md: scan one more sheet of each of the five
products at 600 dpi in the same corner of the glass, a sheet from a second pack where he has one. When those arrive, measure sheet to
sheet scale differences; a remembered scale is trusted only if they agree within about 0.2%, otherwise recognition still finds the
bulls but GroupLab asks for a scale check.

### 4. Report and decision

`docs/notes/fingerprint-trial.md`: sizes, rates, errors, times, and a recommendation planning takes to Alan (ship it, ship recognition
without the scale, or not yet). Only fingerprints would ever ship, never a scan or image of another maker's target; that decision is
Alan's and is not made here. The worth-an-article decision as usual.

## 2026-10-01, entry 331: a batch that needs nobody, so the loop has real work

**Status: done 2026-10-01, every section.** Section 1: Find holes on the five blanks went from 0, 0, 0, 3 and 8 marks to 0, 0, 0, 0 and 6, every any-target scoreboard line unchanged; the remaining six (printed dark numbers and letters, two white digits) cannot be told from a hole without refusing real ones. Section 2: a generic CSV, LabRadar and Garmin Xero, then entry 334's real files. Section 3: docs/PROOF-CHECKLIST.md (worker A). Section 4: docs/PERFORMANCE.md's second baseline. Section 5: the summary is at the top of for-alan.md.

Alan, 2026-10-01 07:31 UTC: "It seems like code is idling a lot and is just waiting for stuff." Everything left in STATE.md's plan is
blocked on him, on Apple or Microsoft, or on planning, so here is work that is not. One worker, main model, entry 317's budget; in this
order, one commit per section where it allows. Bugs a tester or Alan hits still jump the queue.

### 1. The store-bought blanks as a detection test now (request 58's material, entry 325 and 327)

1. Run "Find holes (Experimental)" on the five blanks in `C:\Dev\grouplab-local\commercial-targets\`. A clean sheet must give zero
   holes; list every false mark by cause (printed numbers, pasters, ring lines, the red centers, the cut edge of a partial scan) and fix
   what can be fixed without making any existing case worse.
2. Render synthetic holes into each blank (several calibers, touching pairs, holes on ring lines, on the red centers, on the grid lines,
   and a Shoot-N-C style chartreuse halo where the real target shows one) with known positions, and score finding them, the way the
   scoreboard does for GroupLab sheets. Kept local, never committed (request 58). Report the scores per target; the real shot scans will
   check them later.

### 2. Chronograph files that need no sample from Alan (DESIGN.md section 17)

1. A generic CSV import: a column of velocities picked by header or by the person, units fps or m/s, everything else ignored. This is
   the "most users arrive from a spreadsheet" route.
2. LabRadar's CSV export, from its published layout, and Garmin Xero's CSV export from ShotView, from its published layout. A public
   sample file under a license that allows it may be used as a test, recorded in samples/PROVENANCE.md; otherwise build the test from
   the documented columns and label the reader Experimental until a real file passes. Never anything from a stranger's upload.
3. All three behind the one import interface, ending in the same list of numbers the hand-entry box makes, then the existing
   reconciliation. Place "Import a file" beside the existing entry on both platforms in the existing style; if it needs a new layout,
   DESIGN NEEDED and build the readers anyway.

### 3. The 27 "built, not proven" features: prove what existing material can

Go through the 27 and, for each, say whether material already on Alan's computer or in test-data (the 2026-09-20 range photographs, the
camera-0929 sitting, the corpus, the scale-test scans, Unholy's range screenshot, the commercial blanks) can prove it. Prove every one
that it can. For the rest, name the exact material each needs, so the next request to Alan is one sitting with a checklist.

### 4. Performance (Phase 9): a baseline only

Planning says the application has settled enough to measure. Record a baseline, nothing optimized yet: desktop start time and memory,
a GroupLab sheet read end to end on the desktop, and on the phone through the replay camera (entry 315) where no device is needed; the
numbers into the gate record with their dates. Say what is slowest; planning decides what to optimize.

### 5. When the loop stops

When 1 to 4 are done or blocked and the inbox is empty: write the summary at the top of for-alan.md and end the loop. Do not wake to
wait for Apple, Microsoft or a nightly; the scheduled checks already watch those.

