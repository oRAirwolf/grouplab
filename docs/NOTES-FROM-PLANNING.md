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

- [`docs/notes/archive/notes-2026-09.md`](notes/archive/notes-2026-09.md), entries 1 to 261, 260 of them.

---

## 2026-09-28, entry 277: PRIORITY, before anything else: GroupLab Dev on the Fold 7 for the camera test, now

**Status: done 2026-09-28.** GroupLab Dev nightly 121 (the build with the camera screen and the recalibrated picture score) is on the Fold 7, installed from the nightly's own signed APK over the old 119, and it opens to Capture. The phone first showed offline and then vanished from wireless debugging; the line at the top of `for-alan.md` asked Alan to turn it off and on, and it came back without pairing again. A build made locally could not replace the nightly's, being version 1 against 119, so the nightly's asset is the one to install.

Alan is doing the camera test now (request 50, line 2 of for-alan.md). Before any other entry: install the newest GroupLab Dev (nightly 121 or later, whichever build carries the camera screen and the recalibrated picture score) on the Fold 7 over wireless debugging, the way you did for the earlier sittings. Only GroupLab Dev is touched; nothing else on the phone. If the phone does not answer adb, write one line at the top of for-alan.md saying exactly what Alan must do (for example "turn Wireless debugging off and on and read me the address and port"), and tell him in the panel. When it is installed and opens to Capture, say so in the panel in one line: "GroupLab Dev nightly N is on the Fold 7: go ahead with the camera test." Then continue with the rest of the inbox in number order.

## 2026-09-28, entry 276: answer to question 68 ("Apple mobile")

**Status: done 2026-09-28.** Question 68 closed as answered; the paragraph is listed under "settled" in `docs/RETIRED-WORDING.json`, which the audit's reader and `scripts/consistency.py` share, so it is not reported again.

Keep what you did. Alan's settled sentences stay word for word, and "iOS is not planned." in front of them is enough: the rest reads as what would happen if an iOS version were ever made, which agrees with `docs/ANDROID.md` and the README. Do not rewrite the paragraph or change the pinned sentence in `MacBuildsTests`. If Alan ever wants the paragraph reworded, he will say so and it will come as its own entry. Close question 68, and tell the next consistency audit's reader (the entry 267 rule) that this paragraph is settled wording, so it is not reported again: a line in the audit's notes, or wherever the weekly CI check keeps its exceptions.

## 2026-09-28, entry 275: consistency audit, 2026-09-28

**Status: done 2026-09-28.** All sixteen fixed in one change, the README, the site, the guides and the assets together, every new sentence backed. Two done differently, and why: section 1's three stops say in words that the phone has them since nightly 119, a new "pending" Mobile side the site build accepts until their pictures are taken at the next sitting; section 9's two lines are added to nightly 119 in RELEASE-NOTES.md with a line saying when and why, and its GitHub release is left as published. Section 9 also found the cause: `android/` was still classed as the spike, so a change to the Android application alone did not count as shipping; `android/GroupLab.Android/` now ships. Section 7's "Apple mobile" paragraph is Alan's settled wording, held literally by a test, so it keeps his sentences with "iOS is not planned." in front; question 68 asks whether to reword it. Not done: the phone pictures themselves, at the next device sitting.

The scheduled consistency audit of entry 267 section 2b. It read the README, the live site (home, /download/, /features/, /tour/,
/shoot-a-target/, /guides/, /releases/, /support/, fetched about 15:50 UTC), `website/features.json`, `website/tour.json`, the newest
release notes, STATE, for-alan, PLATFORM-SUPPORT, PHONE-PARITY, ANDROID, the testing guide, the inbox and the last five days of commits.
No GitHub issue labelled `consistency` exists yet. Nothing here needs Alan. Fix each in the same way as any change: the README, the site,
the guides and the assets together, with claims backing where a sentence is published.

### 1. The tour's Mobile side still calls three phone screens desktop only

- **Where:** `website/tour.json`, the `mobile` field of `analysis-open` (line 300), `compare` (line 418) and `ballistics` (line 524),
  shown at https://grouplab.org/tour/ with the Mobile switch, and on each stop's own page.
- **What it says:** "On the desktop only, for now." For `analysis-open` it adds that the phone shows the figures and the plot but not the
  explanations.
- **What it should say:** since nightly 119 the phone has every figure with its explanation, compare loads, and Ballistics as a fifth tab
  (docs/RELEASE-NOTES.md nightly 119; docs/PHONE-PARITY.md rows `why`, `compare`, `ballistics` read "on the phone"). Give each a phone
  screenshot at the next device sitting, and until then words saying it is on the phone since nightly 119. Check `equipment` too: if the
  phone's Ballistics tab lets you enter a rifle and load, that stop is not desktop only either.

### 2. The phone's pictures show the old capture screen and four tabs, under a caption that says "the current build"

- **Where:** `docs/figures/screens/phone/fold-capture-light.png` and `-dark.png` (nightly 115), used by `scripts/readme-images.py`
  (line 42) for the "Photograph it" tile of `docs/figures/readme/mosaic-*.png`, and by the tour's Capture stop. README.md line 29 says
  under the mosaic: "The pictures come from the current build."
- **What it shows:** the nightly 115 Capture form (caliber, distance, Take a picture, Choose a photograph) and a tab bar of four tabs.
- **What it should show:** nightly 119's Capture B over the live picture, Guided and Manual, the quality bar, and five tabs with Ballistics.
  The retake is already planned for the device sitting (entry 253 section 3). Until then README line 29 should not claim the current build
  for the phone tile, for example: "The desktop pictures come from the current build; the phone's from nightly 115, retaken at the next
  device sitting." Every phone screenshot on the site shows four tabs, so the same caveat applies to the Features and tour captions that
  do not already name nightly 115.

### 3. The download page's Android card is behind the build

- **Where:** `website/build.py` line 719, shown on https://grouplab.org/download/ under Android.
- **What it says:** "An early test build: it photographs or opens a sheet and reads it with the same engine as the desktop." and
  "Builds up to nightly 118 showed only the camera on the capture screen; the next build shows its words, shutter and Back".
- **What it should say:** nightly 119 is out, so "the next build" is now wrong: "Since nightly 119 the capture screen shows its words,
  shutter and Back over the picture", or drop the line. The description should match the README's Android row (README.md, the
  "Before you install" table): every figure with its explanation, the bulls you fired at, Shots Needed to Zero, compare loads,
  Ballistics, printing and sessions; marking by hand is not on the phone yet.

### 4. The minimums table on /download/ is printed as raw text

- **Where:** https://grouplab.org/download/, "Minimums". The HTML is one `<p>` holding `| | Operating system | Built and published |
  ...` with the pipes and the `|---|` row visible. Source: `docs/PLATFORM-SUPPORT.md` line 31 and the table around it, converted by
  `website/build.py`.
- **What it should be:** a real table, as it renders in the README. The converter used for the platform statement does not handle
  Markdown tables. Add a site build check that no published page contains a line starting with `|---` or `| |`.

### 5. "Sends nothing anywhere" contradicts three features

- **Where:** the footer of every page (`website/build.py` line 489): "The application keeps everything on your own computer and sends
  nothing anywhere." README.md, "Before you install": "It sends nothing anywhere, and an update check sends nothing about you."
  `docs/TESTING-GUIDE.md` line 5: "it keeps everything on your own machine".
- **Why it is wrong:** /features/ lists "Send a target to the project", "Error reports" ("automatically, after asking, or never") and
  "The hardware survey"; the tour's first run asks those three questions.
- **What it should say:** something true and short, for example "It keeps everything on your own computer and sends nothing you have not
  agreed to: targets, error reports and the survey each ask first." Keep it backed in `docs/claims-backing.json`.

### 6. The Features page says GroupLab updates itself on macOS and Linux

- **Where:** `website/features.json` line 612, key `updates`, platforms Windows, macOS, Linux; https://grouplab.org/features/#updates.
- **What it says:** "GroupLab updates itself, and the update bar lists every build you skipped".
- **What it should say:** only the Windows installer updates itself; the zip, the tarball and the Mac builds tell you and leave the
  download to you (the download page, "Updating", and README "Before you install" both say so). For example: "The Windows installer
  updates itself, and on every desktop build the update bar lists each build you skipped, newest first, with what each changed."

### 7. iOS and the phone's marker detector are described three different ways

- **Where and what:**
  - `docs/PLATFORM-SUPPORT.md` lines 75 to 77, "Apple mobile", shown in the README and on /download/: an iOS version "would be tested
    on" the iPad Mini and "cannot be produced at present" for want of a Mac.
  - `docs/ANDROID.md` section 1: "iOS is not planned. The iPad Mini is for testing the website only." README Phase 8 and License agree
    with ANDROID.md.
  - README.md line 555, the architecture diagram: `iOS planned`. Line 561: `libapriltag mobile, planned`.
  - README "Built with", Imaging row: "On mobile the marker detector is the AprilTag reference implementation under BSD-2-Clause,
    reached through P/Invoke." `docs/ANDROID.md` section 3 says the phone runs GroupLab's own OpenCV build (ArUco with the AprilTag
    36h11 dictionary, `libOpenCvSharpExtern.so`), and no Android or core source names libapriltag.
  - The home page, "Not built yet": "... hand marking on the phone · iOS".
- **What it should say:** "Apple mobile": iOS is not planned; the iPad Mini is used to test the website. The diagram: iOS "not planned",
  and the imaging backend as OpenCV on both desktop and phone (drop the libapriltag box, or mark it "not used"). The Imaging row: OpenCV
  through OpenCvSharp on the desktop and a GroupLab build of OpenCV on Android. The home page: drop iOS from "Not built yet", or say
  "not planned".

### 8. The testing guide's "What is not done yet" is out of date

- **Where:** `docs/TESTING-GUIDE.md` (and its PDF), linked from https://grouplab.org/guides/ and from /support/ step 01.
- **What it says:** line 80, "The Equipment screen is not built."; line 81, "Sending in sheets is coming. `grouplab.org/upload` is written
  ... the server is not installed yet, so the link does not work." "It keeps itself up to date" names only the zip and the Linux tarball.
  Android is not mentioned anywhere in it.
- **What it should say:** the Equipment screen exists (tour /tour/equipment/, README Phase 4 "Done. Records for rifles, barrels and
  loads"); sending works (https://grouplab.org/shoot-a-target/ has the upload, Features "Send a target" since nightly 102); the Mac builds
  also only tell you of a newer build; and a short Android part: the APK or the Play internal test, and what the phone does not do yet
  (marking by hand, CSV, large sheet advice, a picture shared in, from PHONE-PARITY). Regenerate the PDF.

### 9. Nightly 119's notes leave out two phone features it carries

- **Where:** `docs/RELEASE-NOTES.md`, 0.2.0-nightly.119, and so /releases/ and README "What is new".
- **What is missing:** the set of sheets as a checklist and the scan pill (entry 259 screens 6 and 7, commit `ffc5c21`, an ancestor of
  nightly 119's `9046087`). README "Status" lists both as new on the phone.
- **What it should say:** two more lines under nightly 119, for example "On the phone, a set of sheets is a checklist: the sheets read so
  far pooled into one group, and those still to read." and "On the phone, a scan says how large it was printed, and every size is
  corrected to real inches."

### 10. Two phase states in the README contradict their own items

- **Where:** README.md line 378, Phase 5 "**Not started**", while its items include four "Built, not proven" (chronograph strings by hand,
  the ballistic solver, load against load, hit probability at another distance). Phase 9 is "Not started" while `grouplab bench` is
  "Built, not proven".
- **What it should say:** "In progress" for both, by the README's own four states ("being built, not usable" does not fit either, so
  choose the nearest honest state and say why in one line). `DESIGN.md` section 21 changes with it, since `ReadmeTests` holds them equal.

### 11. Android is left off in the README's developer sections

- **Where:** README "Building": "Every nightly build is published for Windows, Linux and macOS". "Repository layout" has no row for
  `android/` or `website/`.
- **What it should say:** every nightly also publishes the Android APK and GroupLab Dev; add rows for `android/` (the Android shell and
  its OpenCV build) and `website/` (the site's source and build).

### 12. Hand marking on any target: said twice, and never said to be desktop only

- **Where:** README "How it works" opens with "Any target works ... mark the holes by hand" and repeats it at line 294 ("It also works on
  targets GroupLab did not print"). The home page's "Your own targets" and the tour index's "Your own targets" say the same with no
  platform.
- **What it should say:** keep one of the two README paragraphs, and add in each place that marking by hand is on the computer and coming
  to the phone (PHONE-PARITY `other-targets`: coming). The hero may keep "Photograph any target", since the phone photographs and the
  computer marks.

### 13. The home page's eyebrow names one platform

- **Where:** `website/build.py` line 568, https://grouplab.org/: "Free · open source · GPL-3.0 · Windows test build".
- **What it should say:** the same page's "What it is today" and README line 5 name Windows, macOS, Linux and Android. For example
  "Free · open source · GPL-3.0 · test builds for Windows, macOS, Linux and Android". The Windows download button can stay as it is.

### 14. The minimums name a Microsoft Store copy that does not exist

- **Where:** `docs/PLATFORM-SUPPORT.md` line 31, Windows: "version 1809 or later for the Microsoft Store copy", in the README and on
  /download/. The same section says "nothing is listed for a platform that has no published build", and the Store listing waits on
  request 38.
- **What it should say:** drop the Store clause until the Store copy is published, or write "for the Microsoft Store copy, once it is
  published".

### 15. The phone has no guide

- **Where:** https://grouplab.org/guides/: "Both guides describe the Windows application". `docs/USER-GUIDE.md` mentions the phone four
  times, only as a camera. The Android-only features on /features/ (Guided or Manual, Every picture checked, Print a sheet from the phone,
  and others) have no "In the user guide" link.
- **What it should be:** a phone part in the user guide, or a short phone guide beside the two, covering install (APK or Play test),
  Capture in Guided and Manual, reading the picture check, the result's figures, Sessions and compare, Ballistics and Targets. Link each
  Android feature to it.

### 16. STATE and for-alan disagree on the open requests

- **Where:** `docs/notes/STATE.md`: "Open requests ...: **7** (50 ...; 46 ...; 38 ...; then 9, 16 and 20)". `docs/notes/for-alan.md`:
  "**Open: 8**", with 52 and 33 besides. STATE's blocked list also says request 16 waits on "his name for a thanks", which entry 189
  answered (Fenix, thanked in the README).
- **What it should say:** the same count and list in both, and request 16 as the trackpad check only.

## 2026-09-28, entry 274: answers to questions 65 and 66 (question 67 is with Alan)

**Status: done 2026-09-28.** Question 65: entry 261's wording corrected in this log. Question 66: the home page's "Your own targets", the tour index and the marking stop show a sample target GroupLab draws itself, marked by hand, labelled as a sample (`marking-other`, made by the screenshot walk); the Features entry uses it. Question 67 stays with Alan; nothing waits on it.

**Question 65, where the detection scoreboard lives.** `tools/` stays read only, and CLAUDE.md is not amended. When the scoreboard is built (the study's recommendation), it lives where everything else Code writes lives: the scoring code and its fixtures in the test projects, so it runs with every build like any other test, and any command-line runner or report script under `scripts/`. What you did for the study (a scratch test, deleted once its numbers were in `docs/DETECTION-LEARNING-STUDY.md`, with its conditions listed) was right. Correct entry 261's wording in the log so it no longer names `tools/study/`.

**Question 66, the "Your own targets" picture.** Do not use the OnTarget scans on the home page or in the tour, not even cropped. Use (c) now: a generated "other target" drawn by GroupLab itself, plainly not a GroupLab sheet (no tags, no QR code, a simple aim point and plain rings or a square), shot and marked on the desktop's marking screen, with the scale set by hand, as a real user would. Label it as a sample. Option (b), Alan's own photograph of a plain store-bought target, replaces it later if he takes one; I am asking him in chat and will pass on his answer. Keep the screenshot job and the Features page in step (entry 267's rule).

**Question 67, the card outline's 3 mm gap.** A visual change from an approved drawing, so it goes to Alan (standing rule). I have drawn three options on the printer check canvas (A the gap as built, B a hairline on the card's edge, C corner marks only) and recommended A. Keep what you built; I will send his answer as its own entry. Nothing waits on it.

**DESIGN NEEDED in for-alan.md (entry 258's two phone screens).** Concepts are with Alan on the phone parity canvas, rows 8 and 9: marking a target GroupLab did not print (A, one step at a time: scale, bull, holes; B, one screen with Scale, Bull, Holes and Template tools), and importing shots from a CSV file (A, the file as a table with a role over each column; B, GroupLab guesses the columns, unit and origin and shows the group to check). In both marking ideas the holes are found automatically and the scale is set by hand. Do not build either screen until his choice arrives as its own entry. Change the DESIGN NEEDED line to say the concepts are with Alan.

## 2026-09-28, entry 273: Alan approved the printer check and unit-tap concepts. Build them as drawn

**Status: done 2026-09-28, in part.** Built as drawn: the check page (grid style 4, question 67), the three wizard screens on both platforms with the phone's camera looking for the card, Printers in Settings, the offers at first run and first print, the paper-edge check and the line on every photo, and tap to switch units with its note, press and hold, and the one-time hint. Not done: numbers inside sentences (Shots Needed to Zero's size, the hit chance's prose) and the desktop's full table, whose cells are bare numbers under unit headings, are not tappable; the phone's Ballistics, Compare and Shots Needed to Zero follow a change but their numbers are not yet tappable; the card photo and the phone screens wait for a device sitting; question 67 asks about the outline's 3 mm gap.

Alan: "I like the concepts. Go ahead and implement them." The canvas is claude.ai/artifact/ECbWJdj8VcYwpv8gd9Appc. This releases the hold in entries 271 and 272. Build:

1. **Tap to switch units,** as in the demo board:
   - Tapping any angular number switches MOA and mil everywhere. Tapping any size on paper switches inches and cm. Tapping a distance switches yards and meters.
   - A short "Angles now in mil everywhere · remembered" note appears at the bottom.
   - Labels keep their dotted underline and tap-to-explain.
   - Press and hold (right-click on the desktop) lists every unit.
   - A one-time hint card reads "Tap a number to switch units".
   - It uses the same setting as Settings and the unit switches. Desktop, phone, reports, Compare, Ballistics and Shots Needed to Zero all follow it.
2. **The Scale check page,** as drawn: Letter, and A4 for A4 locales.
   - The title and "Print at Actual size (100%). Never Fit to page."
   - A code naming the page (GL-SCALE-LTR-1, and an A4 equivalent) and tags.
   - A card outline, 85.60 by 53.98 mm, with "Lay any bank, gift or ID card here".
   - Three crosshairs in an L, 150.00 mm center to center across and down, labeled in mm and inches.
   - Two ruler lines, 250.0 mm down the side and 190.0 mm across the bottom (adjust to fit A4 and label to match).
   - The four numbered instructions.
   The page is built from a definition in the library like any sheet, so it reads itself from a photo or scan.
3. **The wizard,** three screens as drawn:
   - **Pick a method:** name the printer, Print the check page, then choose Card photo, Digital caliper, Ruler or tape, or Scanner. Next, or "Skip for now, run it later from Settings".
   - **The card photo:** Capture B's style, with live checks for card edges, printed outline, tags and focus, and the automatic shutter.
   - **The result:** across and down percentages with their uncertainty, what it means for a group, the paper-edge check agreeing or not, and "Save and finish" or "Check again another way".
   For the caliper and ruler methods, the middle screen is two number fields with a unit choice and a picture of where to measure.
   When it appears: offered at first run (skippable), offered again the first time a sheet is printed, and always in Settings, under Printers.
4. **Settings, under Printers:** the list of named printers with their factors and dates, a default, Check again, Add a printer, Delete, and a switch to turn correction off.
5. **The paper-edge check on every photo,** and the line on each photo result: "Corrected for My printer, 99.2 by 99.4%", or "Measured in the sheet's own inches" with a link to run the check.

The screenshot job, the Features page (one entry for the printer check and one for tap to switch units, each with its own picture), the user guide, the tour and the README all get updated in the same change (entry 267's rule).

## 2026-09-28, entry 272: Alan's choices for checking the print scale, and switching units by tapping a number

**Status: done 2026-09-28, with entry 273,** which released its build. The card, caliper, ruler and scan methods, the thickness correction, the result's words and the paper-edge check are in; the coin is left out as Alan chose. Not done: a card photographed for real, which waits for Alan's first check.

### 1. The printer check (follows entry 271; the study is in the planning project, `print-scale-study.md`)

Alan took every recommendation:

1. **Methods at launch:** card photo, digital caliper, ruler or tape, and scan. The coin is left out.
2. **When the wizard appears:** offered at first run (skippable), offered again the first time a sheet is printed, and always in Settings.
3. **Profiles:** named printer profiles with one default ("My printer").
4. **The paper-edge check on every photo: yes.** Warn when it disagrees with the profile by more than about 1.5 percent, or when there is no profile and the sheet looks fit-to-page.

The build:
- **The Scale check page,** one Letter (A4 where the locale uses it) sheet with:
  - a card outline, 85.60 by 53.98 mm;
  - two caliper crosshair pairs, across and down, 150.00 mm apart between centres;
  - ruler lines both ways, as long as the page allows, with their designed lengths printed in mm and inches;
  - tags, a code naming the page, and "Print at Actual size (100 percent), never Fit to page".
- **The card photo:** detect the card's edges and the printed outline in one photo, correct for the card's thickness (0.76 mm, using the tag model's camera distance), and measure both directions. Report the uncertainty. Card size tolerance: unused cards 85.47 to 85.72 by 53.92 to 54.03 mm, worn cards within about 0.3 percent.
- **Caliper and ruler:** type the measured lengths, with the unit chosen.
- **Scan:** the existing scan path saves the measured scale to a profile.
- **The result:** for example "My printer prints at 99.2% across and 99.4% down (plus or minus 0.3%)". Save it, use it for every photo, and show it in one line on each result.
- **Design:** planning is making concepts of the check page and the wizard screens now. Build the measuring logic first and the screens once Alan has chosen.

### 2. Switch units by tapping a number (Alan)

"We need to find a way to make it easier to switch any value presented between moa and mil or inches to centimeters. Maybe if you click on the value, it switches and remembers that."

- **Tap (or click) any angular value to switch MOA and mil. Tap any length to switch inches and cm.** Every value of that kind switches together, everywhere, and the choice is remembered. It is the same setting as the unit switches and Settings, so they always agree.
- A value shown as both (an angle with its size on paper beneath) switches the part tapped.
- **Long-press on the phone, or right-click on the desktop,** shows every unit the value can take (MOA, mil, IPHY where offered, in, cm, mm) so nothing is out of reach.
- It must stay discoverable and not collide with tap-to-explain (entry 259, which opens a figure's explanation from its label). The value switches units; the label explains. Show a short hint the first time a result appears ("Tap a number to switch units"), and keep the dotted underline on labels only.
- Desktop, phone, reports, Compare, Ballistics and Shots Needed to Zero all follow it. Reports and exports state their units in their headers.
- Planning's concept canvas includes a working demo of this. Build it now. Alan may adjust the details after seeing the demo.

## 2026-09-28, entry 271: real inches on photographs too (the print scale, measured once and remembered)

**Status: done 2026-09-28** (3a1e70b), sections 1, 2, 4 and 5; section 3 is written up as a study. The wizard, the check page and card detection were held by the entry and are released by entries 272 and 273.

Alan: "I was under the impression that the photos could also check the scale of the prints. I thought this was a design requirement."
DESIGN.md's "Print scale verification" still says that because the fiducials are at known coordinates, GroupLab detects the print scale
"automatically". That is true for a scan only. WHAT-CAN-BE-MEASURED.md (entry 171) explains why a photograph cannot: with no absolute
ruler in the frame, a sheet printed small is indistinguishable from a full-size sheet a little farther away. Fix the wording, and then
close the gap as far as physics allows, with as little work for the shooter as possible.

1. **Correct DESIGN.md** (and anything else that says photos measure the print scale) to match WHAT-CAN-BE-MEASURED.md: the tags measure
   the sheet's shape (perspective, curl, lens) on any picture, and its absolute size on a scan.
2. **A printer profile, measured once and applied from then on.** Print scale is a property of a printer and its settings, and it is
   stable. So:
   - When a **scan** of a GroupLab sheet measures its print scale, offer to save it: "Your printer printed this sheet at 99.2 percent.
     Use this for photos of sheets from the same printer?" The person names the printer (default: "My printer").
   - Or, with no scanner, **type one ruler measurement** (bull 1 to bull 5, the distance printed on the sheet) once, on the desktop or the
     phone, and save it the same way.
   - After that, **photographs are corrected automatically** using the chosen printer profile (a setting, with the last one used as the
     default), and the result says so in one line: "Corrected for My printer's 99.2 percent, measured from a scan on 28 September." With
     no profile, the existing line stays ("measured in the sheet's own inches").
   - A per-sheet override: on any photo, "Measure this sheet with a ruler" corrects just that one.
   - Profiles travel with sessions and between phone and desktop (the session file carries the factor used). Record the method in the
     saved marking, as today.
3. **A known object in the frame, as a study (add to entry 261's study, no build yet):** could a credit card sized card (ISO/IEC 7810 ID-1,
   85.60 by 53.98 mm, a size every wallet has) laid flat on the sheet give the absolute scale from a photo alone, to better than about
   0.5 percent at phone resolutions, given its rounded corners and edge contrast? Measure it on real photos before promising anything.
   Phone depth estimates and autofocus distance are not accurate enough (percent-level at best) and are not to be used for scale.
4. **Tell users plainly** in the tour, the user guide and the capture screen: for real inches from a photo, scan one sheet or measure one
   ruler distance once per printer; GroupLab remembers it.
5. Tests: a synthetic sheet printed at 96.2 percent, photographed, reads true size once the profile is applied; a missing profile reads
   in sheet inches with the line; a profile from a scan and from a ruler agree within their stated uncertainty.

**Update, same day:** Alan asked planning to study easy ways for an average person to verify the print scale: a first-run wizard,
also reachable from Settings, that prints a check page with outlines of known objects (a credit card, a coin) and marks for a ruler or a
digital caliper. The options are with Alan for decision. **Build now:** the printer profile plumbing (items 1, 2's storage and the
automatic correction of photos, 4's wording, 5's tests), and the scan and typed-ruler paths. **Hold** the wizard, the check page and
any card or coin detection until a later entry brings Alan's choices.

## 2026-09-28, entry 270: say plainly, everywhere, that GroupLab works on any target, not only its own sheets

**Status: in part 2026-09-28** (3693025). Not done: section 3's picture of a commercial target marked by hand, which waits on question 66.

Feedback from the reloading Discord, after Alan's announcement: "The whole 'print a target sheet' bit is gonna be a barrier to entry that most people won't bother with. When your competitors can do it from just a picture, why bother?" Alan: this person did not understand that GroupLab works with non-GroupLab targets, and that needs emphasis here and on the site.

The README, the site's home page, the Features page and the tour all lead with "print a GroupLab sheet". That reads as a requirement. Change the message to:

- **GroupLab works on any target.** Photograph or scan whatever you shot on. You set the scale once, then mark the holes by hand today. **Automatic hole detection on any target is the goal** (entry 261, section 7), so say it as a goal, not as a feature.
- **A GroupLab sheet is the fast lane, not a requirement.** On its own printed sheets everything is automatic (scale, every hole, which bull each shot belongs to), and one shot per bull gives large groups.

Changes:
1. **The pitch** in the README (entry 266) and on the home page, for example: "Photograph any target. GroupLab measures the group and tells you honestly what the size is worth. Print a GroupLab sheet and it does everything by itself." Keep it to two short sentences.
2. **The README's three captions under the product shot:** caption 1 names both paths, any target or a GroupLab sheet. The mosaic's first tile reads "Any target, or print a sheet".
3. **The home page and the tour:** a clear "Your own targets" section near the top, with a picture of a commercial target marked by hand (the desktop's marking screen on a non-GroupLab target). Once the phone can do it (entry 258), use the phone too.
4. **The Features page:** move "Targets GroupLab did not print" into the first group, and rename it to something plainer, such as "Any target you already shoot".
5. **The mission,** in one line wherever the pitch is expanded (README "Why it exists", home page): GroupLab also aims to help shooters think in mean radius and confidence rather than extreme spread from a few shots.
6. The consistency audit (entry 267) now also checks that no page implies GroupLab sheets are required.

## 2026-09-28, entry 269: answer to DESIGN NEEDED, Shots Needed to Zero colors

**Status: done 2026-09-28** (246f6c5).

Alan chose **option 1** (the parity canvas, board "ZeroColors"): **"Within 1 click" is amber everywhere, and "Closest click" is teal everywhere**, on the phone and on the desktop. Change the desktop chart to match: the within-1-click line becomes amber and the closest-click line teal, with the legend and table row names written as they are now. Update the website's pictures and the Features page entry through the screenshot job. Remove the question from `for-alan.md`.

## 2026-09-28, entry 268: both devices are on the charger for the night (read now, with entry 263)

**Status: followed 2026-09-28.** GroupLab Dev's black idle screen (f2b1b81) was shown whenever a device was not in use. The Tab S8 Ultra did not answer adb all night.

Alan, about 11:20 UTC (05:20 his time): he is putting the Tab S8 Ultra on the charger now, with Wireless debugging and Stay awake on. The Fold 7 goes on a charging cable with the same settings while he sleeps. If a device is not reachable at first, it should be shortly, so retry every 10 to 15 minutes for the first hour before marking it skipped.

- Use both overnight for the unattended work in entry 263: checking entry 260's camera fix with an adb screenshot or UI dump on the Fold 7 (front and inner screens only if it is unfolded; do not ask), entry 253's phone and tablet screenshots, entry 262's torch-strength readings, and entry 258 and 259's screens as they land.
- Nothing needs Alan tonight. The camera test with the printed sheet waits for the morning. Put its steps at the top of `for-alan.md`.
- The usual device rules: touch only GroupLab and GroupLab Dev, never read notifications, put every changed setting back, and do not unlock or change the lock screen. If a device locks, note it and move on.

### OLED screens: keep them dark whenever they are not in use (Alan, standing rule)

Alan: "both my phone and tablet have OLED screens and I don't like keeping them on at the risk of burn in. I have the brightness on my tablet set to the absolute minimum and will do the same to my phone before I go to bed, but as a general rule, it makes me uncomfortable leaving my screen on constantly."

This applies to every device sitting, not only tonight:
- **A black idle screen.** Add to GroupLab Dev a full-screen, pure black idle view with no text, no status bar and no navigation bar (immersive mode). On an OLED screen, black pixels are switched off, so nothing can burn in, and the device stays awake and unlocked for adb. Whenever Code is not actively driving a device for more than about a minute, bring up that view over adb, for example with an intent extra to GroupLab Dev, the way the existing test extras work.
- **Keep the screen on only for work.** Batch device work so the screen shows real content for as short a time as possible. Never leave a static screen (a result, Settings, the camera) on while waiting.
- **Brightness:** do not raise it. Take screenshots over adb, which do not depend on brightness. If a camera test needs light, that is a morning task with Alan anyway.
- **At the end of the night, or when device work is done,** leave the black idle view up and write in `for-alan.md` that the devices can be picked up. Do not turn off Stay awake or lock the device, since that would stop the next session from reaching it. Alan turns Stay awake off in the morning.
- Record the rule in `docs/ANDROID.md` under the device-testing section, and in the device scripts.
- **Alan must always be able to leave it easily.** Use ordinary immersive mode only, never screen pinning, kiosk or lock-task mode, and never block Home, Back or Recents. A swipe up from the bottom (Home) or the Back gesture closes it as with any app. A single tap anywhere shows, for a few seconds, one dim line of text, "GroupLab Dev idle screen, used for overnight testing", and a Close button at least 48 dp. Tapping Close ends it. Test on both devices that each of these works.

## 2026-09-28, entry 267: no lawyer review, and a standing consistency audit of the README, the site and every asset

**Status: done 2026-09-28** (dac0f4b): no lawyer's review; scripts/consistency.py runs in CI and weekly, and opens an issue on a finding.

### 1. No lawyer, for now

Alan: "I am not going to get a lawyer's review unless this application really takes off. As of right now, I am not willing to pay a lawyer hundreds or thousands of dollars for this."

- **Take out every claim that something is "with a lawyer" or "waits on the attorney"**: README (License, Planned), `docs/ANDROID.md` (line 25, the public Play listing), `DESIGN.md`, `docs/CLAIMS.md` and `docs/claims-backing.json`, STATE.md, the site, and anywhere else the search finds (`grep -rni "lawyer\|attorney"`). Leave the planning history and the archive as they are.
- **The public Google Play listing is no longer blocked by a lawyer.** Its remaining gate is Google's own rule for new personal accounts: a closed test with at least 12 testers for 14 days. Record that in ANDROID.md as the path to a public listing.
- **The GPL section 7 app-store permission** concerns Apple's App Store, and iOS is not planned. Record it as "not in force, and not pursued now; it would be revisited only before an iOS release". Keep one plain sentence on the reason in the License section, without any mention of a lawyer.
- **The effect on outside contributions:** adding such a permission later needs every copyright holder's agreement. In CONTRIBUTING.md, say plainly that contributions are accepted under GPL-3.0 and may later be offered under an added app-store permission, and ask contributors to agree to that when they open a pull request. Keep it one or two sentences.
- The patent and trademark searches (docs/PATENT-SEARCH.md, TRADEMARK-SEARCH.md) stay as they are; only change a line that says a lawyer is reviewing something now.

### 2. The consistency audit, as a standing rule

Alan: "the github readme, grouplab.org website and all of the assets need to be checked every so often to make sure they are up to date and agree with what is currently happening in the project. I should not have to find these oversights because they should be analyzed. It does not have to happen with every build, but it should at least happen once every few days or a week."

Two layers, both automatic:

**(a) The mechanical checks, in CI, weekly, plus on demand.** Add a scheduled workflow (for example weekly, on Monday at 13:07 UTC) that runs a consistency script, and opens or updates one GitHub issue labelled `consistency` listing every finding. It closes the issue when there are none. It checks:
- every platform with a published build is offered on /download/, in the README and in PLATFORM-SUPPORT.md (entry 265);
- the version, date and commit named in the README, on the site and in the release notes agree with the newest nightly;
- counted facts agree everywhere, taken from the source, not typed: sheets in the library (`<!--count-->` markers), platforms, features;
- every feature in `features.json` appears on the Features page, in the README summary and in PHONE-PARITY.md with a phone status;
- every screenshot and generated image is current (entry 253's stale check), including the README's two new images (entry 266) and the donor pack (entry 264);
- every link on the site and in the README resolves (internal links always; external links, allowing for temporary failures);
- the claims in CLAIMS.md still have their backing (claims-backing.json);
- no retired wording remains, from a list kept in the repository: for now "lawyer", "attorney", and any feature named as missing that has since shipped, such as "marking a target by hand is not on the phone yet" once it ships;
- the site's banned-term and IP-address checks.

The script also runs in the ordinary build as a warning, so it can be run locally at any time.

**(b) The judgement review, every three days.** A scheduled planning session (set up by planning, not Code) reads the README, the live site, the release notes, STATE.md, for-alan.md, the inbox and recent commits. It looks for what a script cannot see: a page describing something that has changed, a promise that no longer holds, wording that contradicts a recent decision, a missing mention of something new. It writes its findings as a numbered inbox entry for Code, and tells Alan only when something needs him. Code actions those entries like any other.

**The rule, from now on:** whenever Code changes behaviour, a platform, a decision or a user-facing name, it updates the README, the site, the guides and the assets in the same change, or lists why not in the commit. The audits catch whatever slips through; they do not replace doing it.

## 2026-09-28, entry 266: the new README, as Alan chose it

**Status: done 2026-09-28** (f1d775e). Entry 270 then changed its pitch and first caption.

Alan chose, on the canvas claude.ai/artifact/KeUwJotHu9SvkkGUPoPBSJ (boards "Chosen: B's picture over C's mosaic" and its light-theme twin): **design B's product shot at the top, with design C's six-screen mosaic under it**, and the new order. Build it so the README stays fully automatic.

**Approved by Alan (2026-09-28): "I like the new design. Lets go with it."** One change: **take out the "What GroupLab is not" section.** Alan: "It comes off kind of mean." It is not moved into a fold either. The rules it stated still hold for the project: no OnTarget compatibility, no pseudoscience, not commercial, not an electronic target system. Keep them where contributors read them (CONTRIBUTING.md or DESIGN.md), worded as what GroupLab does rather than as what it is not, and keep the site build's banned-term check. Check grouplab.org for a similar "is not" section, and soften or remove it the same way.

**The top, in order:**
1. `# GroupLab`, then the pitch in bold: "Print a target, shoot it, photograph it. GroupLab measures every hole and tells you honestly what your group size is worth." Then one line: free and open source under GPL-3.0, no account, no ads, no paid tier, on Windows, macOS, Linux and Android. (Keep "GroupLab is a working name" somewhere lower down, for example in License.)
2. Badges: license, the newest nightly, the tests, platforms, and Discord. Use shields.io or GitHub's own badges; the nightly badge must update itself.
3. **The product shot (B),** one image made by the screenshot job:
   - a shot GroupLab sheet (the published sample scan), tilted, behind;
   - the desktop analysis screen in a window;
   - the phone result overlapping at the right;
   - numbered amber callouts 1, 2 and 3.
   Make a dark and a light version (a `<picture>` with `prefers-color-scheme` sources), and alt text that describes it.
4. Under it, a **markdown table** of the three numbered captions, so the captions are real text:
   - 1, print and shoot a GroupLab sheet;
   - 2, on the computer, every hole found, then the group with honest ranges;
   - 3, on the phone, photograph it at the range.
5. **The mosaic (C),** one image, 3 by 2 tiles with their captions inside the image: Print a sheet, Photograph it, Every hole found, Honest numbers, Compare loads, Ballistics and hit chance. Dark and light versions again. Under it, one centred line saying the pictures come from the current build, with a link to the tour.
6. One centred line of links: Download · Website · Features · Tour · User guide · Discord.

**Then the sections, in order:**
- **What it does:** three columns (Reads targets, Honest statistics, Prints sheets), a few lines each, generated from `features.json` groups, with a link to the Features page. This replaces the long link list.
- **Download:** the nightly's number and date, and a single table by platform: Windows (installer, zip), macOS (Apple silicon, Intel), Linux (tarball), Android (APK, Play test). The long notes go into a `<details>` titled "Before you install": unsigned builds, the Mac quarantine command in a code block, the samples, where it keeps files, and updates.
- **What is new:** in `<details>`, generated as today.
- **What is supported, and what is not:** in `<details>`, generated as today.
- **Why it exists:** two short paragraphs.
- **How it works:** the flow, then one line on targets GroupLab did not print.
- **Status and plan:** Status, Planned and Deferred, each in `<details>`, generated as today.
- **For developers:** Built with, Architecture, Building, Repository layout and Test data, each in `<details>`.
- **Thanks:** Unholy, Jylee (Shots Needed to Zero, and helped choose the C3 grid, with Unholy) and Fenix, by the names they gave.
- **License.**

**Rules:**
- Nothing else is deleted: apart from "What GroupLab is not", every sentence of today's README lands in a fold, or on the page a fold links to.
- Keep every generated marker and `scripts/readme.py`. Blank lines inside `<details>` let GitHub render markdown there.
- Both images come from the screenshot job and join entry 253's stale check.
- Render the README with GitHub's markdown (for example `gh api markdown`) in both themes before committing, and look at it.
- The website's home page may borrow the two images later; that is a separate decision.

## 2026-09-28, entry 265: the download page on grouplab.org is missing Android

**Status: done 2026-09-28** (0a81418): Android is on /download/, and the site build fails when a published platform has no download. Section 4 is confirmed with the push that carries it.

Alan noticed that grouplab.org/download/ does not offer the Android build. The Android decisions (entries 198 and 199) said the nightly APK goes on GitHub **and grouplab.org**. So this is an omission, not a decision. The page mentions Android only in the minimums table, and its description still says "for Windows, Linux or macOS".

1. Add Android to /download/ beside the desktop builds:
   - the signed APK (`grouplab-android.apk`), with its minimums (Android 10 or later, arm64, 4 GB);
   - how to install it (allow installing from the browser, and remove any Play copy first, since the keys differ);
   - the Google Play internal test, by invitation, with how to ask for an invitation (the Discord);
   - GroupLab Dev for testers.
   Mark it as an early test build, and state the known camera problem plainly until entry 260's fix ships. Once it ships, the note comes out.
2. Update the page description and meta text, the home page's download button text if it names platforms, and anywhere else on the site that lists only three platforms.
3. Add a site build check that every platform in `docs/PLATFORM-SUPPORT.md` with a published build has a download on /download/, so a platform cannot go missing again.
4. Publish the site as usual and confirm it is live.

## 2026-09-28, entry 264: the donor pack and the Shoot a target page get the new sheets

**Status: done 2026-09-28** (bde8f6b, 1009cd2): the donor pack is one sheet of each style, built from the library, and /shoot-a-target/ offers each sheet or the whole pack.

Alan: "the donor page of the grouplab website may need updating with the new targets, as the default ones were hard to use. Maybe one of each?"

The donor pack (`website/donor/`, shown on /shoot-a-target/) still offers only GL-CF25-LTR and GL-CF25-LTR-D, which Alan and his testers found hard to use.

1. **One of each current style.** Offer one sheet of each style a donor might shoot:
   - the E bull sheet;
   - the C bull sheet;
   - the C bull with the centre dot, if it is a separate sheet;
   - the C3 zeroing grid, mil at 100 yd and MOA at 100 yd (and 100 m where it exists);
   - the standard 5x5 load sheet;
   - Made for your optic, as a link to the Targets screen and the website's generator, since it is made for each person.
   Letter first, A4 beside it. Each sheet gets a one-line description of when to use it, and a picture of the sheet itself (entry 256's rule), not a screen.
2. **Built, not copied.** Generate the donor PDFs from the target library in the site build, as the other sheets are, so a redrawn sheet updates the pack by itself. Add the pack to entry 253's stale check, so the site refuses to publish a pack that no longer matches the library.
3. **The instructions.** Update `docs/VOLUNTEER-PACK.md` and the instructions PDF:
   - how to check the printed size for each sheet style, since the bull 1 to bull 5 measure does not fit a zeroing grid;
   - the photo guidance as it will be after entry 260 (Guided or Manual, the torch, and whatever the check says);
   - what to write on each sheet.
   Keep "Submitting means following the terms on that page."
4. **The page.** On /shoot-a-target/, let the donor download the whole pack or any single sheet. List which sheet is most useful to the project right now. For example, if the corpus lacks photos of zeroing grids or of the C bull, say so, and take the list from entry 261's study once it exists.

Tell Alan when it is live, with the page link.

## 2026-09-28, entry 263: overnight rules, read now, before continuing entry 260

**Status: followed through the night of 2026-09-28.** The morning summary is at the top of for-alan.md.

Alan is going to bed (about 04:45 his time, 10:45 UTC) and wants Code busy all night without waiting on him.

1. **Never stop to wait for Alan overnight.** When a step needs him (a camera test with the sheet, a decision, a secret), write it at the top of `for-alan.md` for the morning. Then **move straight on** to the next piece of work that does not need him. Where a DESIGN NEEDED question comes up, write the line and keep building the parts it does not affect.
2. **The order:**
   - Entry 260 part 1 and the checking logic of part 2. Verify over adb, with no one present, that the words, shutter and Back show over the live preview on every screen and orientation available.
   - Then entry 259's screens, starting with the shared code of entry 258.
   - Then entry 261's study.
   - Then entry 262's device measurements (reading the torch strength characteristics needs no person).
   - Entries 253, 255 and 257's remaining device work wherever it can run unattended.
   Ship nightlies as usual.
3. **Devices overnight:** Alan will say whether he leaves them on the charger, unlocked, with Stay awake and Wireless debugging on. If a device is not reachable, skip its steps and note it. Touch only GroupLab and GroupLab Dev, never read notifications, and put every changed setting back.
4. **The morning summary:** at the top of `for-alan.md`, in five lines or fewer: what was finished, what is waiting on Alan (with the camera test steps if the fix is in), and what is next.

## 2026-09-28, entry 262: torch brightness, a bracketed burst, and combining frames (investigate, then build what the phones support)

**Status: in part 2026-09-28.** Step 1 on the Fold 7 only: Android 16, strength levels 1 to 5, default 1 (docs/ANDROID.md section 16). Not done: the Tab S8 Ultra, which did not answer; setting the level during a session and timing the settling; steps 2 to 4.

After entry 260. Alan: "Can the application adjust the flash's brightness? Can it progressively increase the brightness of the flash and take a burst of photos at different brightnesses and then try and composite them together or do some post processing to get the best image possible?"

**What the platform allows (planning's reading; confirm on the devices):**

- Android 13 added torch strength levels (`CameraManager.turnOnTorchWithStrengthLevel`). That call cannot be used while the camera is open, and CameraX always has it open.
- Android 15 added `CaptureRequest.FLASH_STRENGTH_LEVEL`, which sets the torch strength **during** a camera session. It is reached through Camera2Interop, or through CameraX 1.5's own torch strength API.
- It is optional for phone makers. A device supports it only if `FLASH_INFO_STRENGTH_MAXIMUM_LEVEL` is greater than 1.
- GroupLab supports Android 10 and later, so on older or unsupported phones the torch is simply on or off.

**Step 1, measure, at the next device sitting:**

- Read and record `FLASH_INFO_STRENGTH_MAXIMUM_LEVEL` and the default level on the Fold 7 and the Tab S8 Ultra, with each device's Android version.
- Check whether setting the level during a session works on each.
- Time how long exposure takes to settle after each torch change.

Record the results in `docs/ANDROID.md`.

**Step 2, a bracketed burst, where supported:** one press takes a short series. For example: torch off, then low, medium and high, or as many levels as the device offers, each after exposure settles. It must stay short enough that a hand-held phone does not drift badly. Because the sheet carries its own tags, every frame can be aligned exactly to the others, so small movement between frames does not matter.

**Step 3, choose or combine. Measure each against the corpus and real photos before shipping:**

- **(a) Choose the best single frame** by the quality score (entry 260). This is the simplest option and keeps the measurement on one real exposure.
- **(b) Combine per region:** for each area of the sheet, take the frame with the least shadow and no glare, after aligning them all on the tags.
- **(c) Flash and no-flash processing:** use the torch frame to find and remove the shadows cast by the phone and hand, and the no-torch frame to find and remove glare.

Hole edges and centres must not move because of combining. Measure the centre error of (b) and (c) against (a). Ship a combined image only where it is measured to be at least as accurate, and record which method made each picture in the capture record.

**Step 4, the setting:** under Torch, Auto uses the burst when the light is dim or there are shadows. On and Off stay available. The panel says what was done, for example "Torch at 3 levels, best frame kept".

If the devices do not support strength levels, report that. Fall back to a torch-on and torch-off pair, which works on every phone with a torch, and keep steps 3 and 4 for that pair.

