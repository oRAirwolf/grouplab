# Release notes

Every build of GroupLab anyone could download, newest first. This file is the source of truth: the page at <https://grouplab.org/releases/> is built from it, and it reads here on GitHub too.

GroupLab is unreleased. Everything below is a pre-release, and the version numbers say so.

**How to read this.** Every build says what is in it, whoever it affects. **What you will notice** is the part you meet: something on screen, something that behaves differently, something new or gone, something fixed, or a change to what gets installed. **Under the hood** is everything else in the application in plain words, such as a check that now runs or work nobody can perceive yet. Changes to this website, the guides and the research are not listed here, because they are not in the application; the website says when it changes. A build shows only the headings it has, and a build that changed nothing in the application says so in one line. Where a build has something wrong with it that matters, it says so under **Known issues**.

**Every entry was rewritten on 2026-09-24 from its build's own commits** (NOTES-FROM-PLANNING.md entry 168). Notes that had been cut off at a line break are whole again; website, research and documentation changes are gone from the builds, because they are not in the application and the website says when it changes; and a build that changed nothing in the application says so in one line and names the build it is the same as. **Known issues** sections are kept exactly as they were written. Two builds, nightly 25 and nightly 12, keep the text they were published with, because their notes were written before today's checks existed.

**Why the nightly numbers skip.** Up to nightly 91 a nightly was numbered by the workflow run that built it, and a run that was cancelled or skipped still took its number. From nightly 92 the number is the last published build plus one, so from there a gap means a number was never used, and a build that should not have been made is named as such below rather than hidden.

---

## 0.2.0-nightly.164

**2026-10-03**, commit `2cce12a`. Nightly.

**Under the hood**

- The window that says a picture's codes could not be read keeps the picture back, as the sending question promises, until sending every picture is switched on.
- Error reports that carry GroupLab's log are built but switched off, waiting for the store privacy answers to be confirmed; a report holds what it held before.
- Sending every picture you open is built but switched off, waiting for the store privacy answers to be confirmed; nothing GroupLab sends has changed.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.164)

---

## 0.2.0-nightly.163

**2026-10-03**, commit `0a815d2`. Nightly.

**What you will notice**

- Try again, reading harder, in the window that says a GroupLab sheet's codes could not be read, now reads each corner code on its own, lit evenly and turned square on, and looks for the sheet's printed name, naming each step with Cancel beside it.
- Saying a picture is not a GroupLab sheet is remembered for that picture, so opening it again asks which target it is straight away.
- On the computer, a problem that stops your work, such as a file that could not be opened or saved, now opens a window in the middle of the screen with what to do next, where Enter takes the first choice and Escape closes it.
- When a picture has a GroupLab sheet's corner squares but its codes cannot be read, GroupLab now says so in the middle of the window with Choose the sheet first, and an amber bar keeps the way back after you close it.
- Opening a picture of a target GroupLab did not print is no longer treated as an error: GroupLab asks which target it is in the middle of the window, with marking it by hand first.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.163)

---

## 0.2.0-nightly.162

**2026-10-03**, commit `e586e5b`. Nightly.

**What you will notice**

- A sheet made with GroupLab's target generator and printed without saving it now opens and reads: GroupLab takes the sheet's design from its own square codes, where before it refused the sheet.
- When a sheet's codes name a sheet GroupLab cannot use, it now says so in plain words instead of saying the codes could not be read, and your own saved sheets are recognized from their codes as well.
- On a photograph of a crinkled, curled or torn sheet, GroupLab no longer marks the torn corners, the curled edges, the board showing behind the paper or a bull's printed number as shots.
- A hole beside one of the sheet's small square markers is no longer counted twice, once for the hole and once for the edge of the marker.
- The review count now counts shots, so it never says more need review than there are, and when more marks are found than rounds fired the review starts with the marks on the sheet's own printing.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.162)

---

## 0.2.0-nightly.161

**2026-10-03**, commit `3fe07c3`. Nightly.

**What you will notice**

- The question about sending targets now says plainly that a target goes once you press Accept and analyze, and that a picture GroupLab could not read is never sent.

**Under the hood**

- The test build's real-tap check on the iPhone simulator now makes sure the photo picker is closed before tapping on, and says so plainly when it is not.
- The test build's real-tap check on the iPhone simulator now closes the photo picker that stands in for the missing camera before tapping on.
- The test build's real-tap check on the iPhone simulator now waits until the screen has caught up with the keyboard before tapping, as a person's eye does.
- GroupLab Dev, the test build, now lets the nightly checks on the iPhone simulator and the Android emulator tap the screen as a finger does, so a button that ignores a real tap is caught before a build reaches testers.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.161)

---

## 0.2.0-nightly.160

**2026-10-03**, commit `6b3eb5b`. Nightly.

**What you will notice**

- On the iPhone, buttons work again while a box is being typed in: Continue on the caliber question, Done, Take a picture, Choose a photo and a suggested caliber each do their job on the first tap.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.160)

---

## 0.2.0-nightly.159

**2026-10-02**, commit `e424c70`. Nightly.

This build has no change to the application; it behaves exactly as nightly 158 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.159)

---

## 0.2.0-nightly.158

**2026-10-02**, commit `e424c70`. Nightly.

**What you will notice**

- A damaged or very large chronograph file is now refused with a sentence saying why, where a damaged workbook could close GroupLab before, and a workbook built to fill memory is refused before it is opened.
- On a store-bought target, Find holes no longer marks bold printed ring numbers, such as the 6s and 7s of a scoring bullseye, as holes.
- Reading a target is faster: on the desktop a sheet goes from file to figures in about half the time, and on a phone a photograph is read about a quarter sooner, with every measurement exactly as before.

**Under the hood**

- The automatic Android screen check no longer finds every screen covered by the first-run questions.
- Each nightly that changes the application now also walks every phone screen on an Android emulator, as it already did on the iPhone simulator, so a page that stops opening on Android is caught before anybody installs the build.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.158)

---

## 0.2.0-nightly.157

**2026-10-02**, commit `6fbe044`. Nightly.

**What you will notice**

- The iPhone and iPad build works again; builds 154 to 156 did not reach Apple's test service, so this one carries their changes, among them the keyboard fix.
- On the phone, every box, switch and button now has a name a screen reader reads out, and on a small phone the bull picker's numbers 10 to 25 are no longer cut off.
- On an iPad held upright, typing in a box on a result no longer flips the result to its side-by-side layout and hides the box under the keyboard.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.157)

---

## 0.2.0-nightly.156

**2026-10-02**, commit `3df5d04`. Nightly.

**What you will notice**

- Add a store-bought target, on the Targets screen of the computer and the phone, turns a photo of a target GroupLab does not know into a small file of its fingerprint, name, size and bulls, never the photo, in five steps, to send so a later build recognizes it.
- On the phone, going back to the targets list after opening a sheet no longer fails.
- Changing what a chronograph reading goes with is now a row per reading in the order fired, with one mark to tap or click, teal when paired and amber when it needs a look, and the same four choices on the phone and the computer; giving a reading a shot another reading has swaps the two and says so.
- A chronograph string imported in meters a second now shows its readings in meters a second while you pair them.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.156)

---

## 0.2.0-nightly.155

**2026-10-02**, commit `3124036`. Nightly.

**What you will notice**

- Store-bought targets added after your build now reach GroupLab without a new build: it looks for a newer signed list when it starts, and on a phone only on Wi-Fi with the battery and storage not low.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.155)

---

## 0.2.0-nightly.154

**2026-10-02**, commit `a81e61f`. Nightly.

**What you will notice**

- On the iPhone, the bar with Done no longer stays floating over the screen after the keyboard closes, Done always closes the keyboard, and a tap outside a box closes it.
- Correction: a Garmin Xero file with several strings holds the strings you selected when exporting, not a whole month; earlier notes called it a monthly export.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.154)

---

## 0.2.0-nightly.153

**2026-10-01**, commit `828858f`. Nightly.

**What you will notice**

- Every box and choice on the computer now has a name a screen reader reads out, and a button reached with Tab works with Space or Enter on every screen.
- The Targets screen now fits a window 1060 wide, where its zoom buttons ran past the edge.
- The phone reads a GroupLab sheet's codes in about half the time it took, so a photograph is measured sooner.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.153)

---

## 0.2.0-nightly.152

**2026-10-01**, commit `9590358`. Nightly.

**What you will notice**

- GroupLab now recognizes five Birchwood Casey targets on the computer and the phone: it names the target, places its bulls and sets the scale from its printed size, with a warning that printed sheets can vary and a button to check the scale.
- When a picture of the Shoot-N-C bullseye cannot show whether it is the 6 inch or the 8 inch, GroupLab asks which target it is, with a small drawing of each, and offers your last answer first.

**Under the hood**

- GroupLab can now make the fingerprint of a store-bought target from a photograph, so new targets can be added to the library; nothing on screen uses it yet.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.152)

---

## 0.2.0-nightly.151

**2026-10-01**, commit `663a29c`. Nightly.

**What you will notice**

- Find holes no longer proposes the solid black diamonds printed in a store-bought target's aim discs as holes, and no longer mistakes a printed ring cut off by the edge of a partial scan for a hole.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.151)

---

## 0.2.0-nightly.150

**2026-10-01**, commit `fdffbfb`. Nightly.

**What you will notice**

- When you import a Garmin Xero file, GroupLab now proposes which readings belong to your group from the chronograph's own pauses, left-out shots and deleted shots, and says why; change any mark before accepting.
- Garmin Xero exports in metric units are now read in meters per second, where before their speeds were taken as feet per second.
- GroupLab Dev for Android now checks for updates only when the battery and the storage are not low, as well as on Wi-Fi; Update now in Settings still works any time.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.150)

---

## 0.2.0-nightly.149

**2026-10-01**, commit `b5a2b00`. Nightly.

**What you will notice**

- Chronograph files now include Garmin Xero exports as Excel files, a monthly export choosing its string by name, with deleted shots kept missing and the chronograph’s own figures checked.
- Chronograph readings can now come from a file: a spreadsheet CSV, or, still experimental, a Labradar report or a Garmin Xero export, on the computer and the phone.

**Under the hood**

- A trial of recognizing store-bought targets from a small stored fingerprint was measured from the command line; nothing on screen changes.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.149)

---

## 0.2.0-nightly.148

**2026-10-01**, commit `c9c698a`. Nightly.

**What you will notice**

- On a store-bought target, Find holes no longer marks the printed black diamonds or some of the white ring numbers as holes.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.148)

---

## 0.2.0-nightly.147

**2026-10-01**, commit `a11f667`. Nightly.

**What you will notice**

- On the phone, the keyboard no longer covers what you are typing: the page moves up, a bar on the keyboard says Next or Done, and tapping outside a box closes the keyboard.
- After a picture, the note about a curled sheet no longer shows the same number twice; it now appears only when the curl costs the score, and says in plain words that the sheet looks slightly curled.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.147)

---

## 0.2.0-nightly.146

**2026-10-01**, commit `ba72c69`. Nightly.

**What you will notice**

- Velocity and the vertical now works out velocity's share in the air temperature, altitude and shot angle entered on the Ballistics screen for the session's rifle and load, and says when it assumed a standard day instead.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.146)

---

## 0.2.0-nightly.145

**2026-10-01**, commit `fb08836`. Nightly.

**What you will notice**

- A new block, Velocity and the vertical, shows how much of a group's up-and-down spread comes from velocity alone, from its chronograph readings, with a band on the group picture you can switch off; on the phone it is a card above All figures.
- Photographs taken at an angle where the far edge of the sheet has lifted off the table now place the holes beside that edge correctly, instead of up to 0.08 in off.
- Accepting a chronograph string no longer fails when its first reading belongs to a shot, and each reading is now kept beside its own shot rather than the next one's.
- On the phone, the chips under the group picture that turn the circles on and off are now remembered, as they are on the computer.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.145)

---

## 0.2.0-nightly.144

**2026-10-01**, commit `05ed5bd`. Nightly.

**What you will notice**

- A target photographed from about 3 ft away is now read too, by looking again at each corner marker enlarged, where before none could be read.
- A target photographed from about 2 ft away is now read, where before GroupLab found none of its corner markers and asked you to move closer.
- On a photograph taken through a lens that bends the picture hard, markers near the edge of the frame are no longer left out of lining up the sheet when the lens explains where they are.

**Under the hood**

- GroupLab can now work out how much of a group's up and down spread the load's velocity spread explains at the distance shot, with an honest range, though no screen shows it yet.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.144)

---

## 0.2.0-nightly.143

**2026-09-30**, commit `2c25c28`. Nightly.

**What you will notice**

- On the phone, the camera's level works upright at a target still on its backer as well as flat over a table, says which under the crosshair, and follows the sheet itself once its corner squares are read; Guided takes the picture either way.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.143)

---

## 0.2.0-nightly.142

**2026-09-30**, commit `6d5884c`. Nightly.

**What you will notice**

- On a target GroupLab did not print, Find holes (Experimental) now proposes the holes once you set the scale, on the computer and in GroupLab Dev on the phone; check every one, and those it is unsure of wait in the review with the reason.

**Under the hood**

- The experimental hole finder for targets GroupLab did not print now also sees holes in black bulls and hits on fluorescent targets, and every build measures it on four kinds of drawn target.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.142)

---

## 0.2.0-nightly.141

**2026-09-30**, commit `70a098e`. Nightly.

**What you will notice**

- A mark much bigger than your bullet is now ringed in amber on the result, on the phone as well as the desktop, with a sentence saying how many times your bullet across it is, until you confirm the shot is on the hole or move it.
- On a target photographed well off square, a sliver of printed ring beside a bull on the far side is no longer counted as an extra shot.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.141)

---

## 0.2.0-nightly.140

**2026-09-30**, commit `90ee7ff`. Nightly.

**Under the hood**

- The developer copy for Android can now be run by Google's phone testing service on real Samsung, Pixel and other phones, once the free account is set up.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.140)

---

## 0.2.0-nightly.139

**2026-09-30**, commit `10259c6`. Nightly.

**What you will notice**

- A picture of a sheet that shares its layout with others no longer fails to read on the phone when one of your own saved sheets cannot be drawn. (Error report 9).

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.139)

---

## 0.2.0-nightly.138

**2026-09-30**, commit `366ff9b`. Nightly.

**Under the hood**

- The developer copy can now play a recorded camera clip or a saved picture through the capture screen in place of the camera, and record the camera's last few seconds on the device, so the guidance and the automatic shutter can be tested again on every build; the published app is unchanged.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.138)

---

## 0.2.0-nightly.137

**2026-09-30**, commit `b8e500c`. Nightly.

**What you will notice**

- Settings, About has a new switch, Show diagnostics on the camera, off until you turn it on: it shows a few numbers over the camera and while a picture is read, such as the frame rate, what the guidance is waiting for, the tilt, the step being read, memory and how warm the phone is, so a screenshot shows where GroupLab was.
- The caliber box can now find any of more than six hundred cartridges by name as you type, even written loosely such as 65 creed or 308, and a new setting chooses whether it lists calibers, cartridges or both.
- Settings, About now has Send diagnostics, which puts GroupLab's logs and any kept pictures in one file and opens the share sheet, so you choose where it goes.
- On iPhone and iPad, GroupLab's own folder now appears in the Files app, and Settings, About can keep every picture of a sitting there until you turn it off.
- The line on Capture asking you to allow the camera now goes away once you have allowed it, instead of staying under the buttons.
- On Compare, two loads with the same extreme spread now sit at the same place on the chart, instead of at opposite ends of it.
- Pressing Calculate again on Shots Needed to Zero no longer closes GroupLab on the iPhone and iPad, and pressing Work it out twice on Ballistics is safe too. (Build 134 error report).
- A picture whose codes are hard to read is now read in about half the time on the phone, using much less memory.
- On iPhone and iPad the level on the camera screen now turns its whole crosshair green when the device is flat, as it does on Android, so level is plain at a glance.
- On iPhone and iPad the camera's instruction panel now sits above the preview instead of over it, so the preview shows the whole picture, and the stray dash beside the quality bar is gone.
- Cancel now works at once while a picture is being read, keeps the picture so you can read it again or choose its sheet, and a reading that takes over a minute stops and says why instead of spinning.
- Choosing a caliber now leaves a short name such as 6.5 Creedmoor, 0.264 in, a tap on the box selects it all so you can type a new one straight away, and a clear button empties it, on the phone and the desktop.
- Distance, click value, velocity and the other number boxes on the iPhone, iPad and Android now open the number pad with a decimal point instead of the full keyboard.
- Compare's verdict on the phone now says in plain words how far apart the loads' spreads could be and how many shots would tell them apart, with the exact figures under Details.
- On an iPad, an Android tablet or the open Fold, Compare now draws each load's group large across the whole screen, and a Back to Sessions button sits at the top.
- In Guided mode the camera now takes the picture about a second after the sheet is framed well, instead of making you hold still for several seconds; what counts as well framed is unchanged.

**Under the hood**

- Fixes to today's changes before they reach a build: the camera diagnostics text wraps on a narrow screen, and the caliber box's clear button follows the theme.
- The developer copy of GroupLab can now be driven by a script over the USB cable, pressing buttons by name, reading a picture, taking screenshots and starting over, and only the developer copy has it.
- A separate development build for iPhone and iPad, GroupLab Dev, is now made every night beside GroupLab, for testing only; GroupLab itself carries none of its tools.
- GroupLab Dev can now run a written list of steps by itself and save what happened, with pictures of each screen, for testing without anyone's hands.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.137)

---

## 0.2.0-nightly.136

**2026-09-30**, commit `4cd9a2a`. Nightly.

**What you will notice**

- With a large iPhone turned on its side, a result now shows the target beside the numbers, as on a tablet, instead of one long column.
- With an iPhone turned on its side, the bar of places along the bottom now runs to both edges of the screen instead of stopping short.
- The version at the foot of the Capture page and in Settings now shows a short build code instead of a long one that wrapped across the screen.

**Under the hood**

- The automatic iPad check now photographs every screen with the iPad turned on its side as well as upright.
- The automatic iPhone and iPad check now also photographs every screen on an iPad and on an iPhone turned on its side.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.136)

---

## 0.2.0-nightly.135

**2026-09-30**, commit `417694c`. Nightly.

**What you will notice**

- The Ballistics screen on phones now reads "The solver needs a rifle and a load" instead of a broken list when nothing is chosen yet.
- On phones and tablets the Key button above the group plot is now easy to tap with a thumb; the whole strip around it opens the key.
- When you tap an underlined word for its meaning, the explanation now appears in the same lettering and size as the rest of GroupLab on iPhone, iPad and Android.
- On iPhone and iPad the bar of places along the bottom now reaches the bottom edge of the screen, behind the home indicator, instead of leaving a pale strip beneath it.
- The Mac download is signed and approved by Apple from the next nightly, so it opens without the Terminal command. (Request 55).

**Under the hood**

- A slow answer from Apple no longer stops the Mac download from being signed and approved. (Request 55).
- The iPhone and iPad test run now checks that Compare draws each load's group with its holes.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.134

**2026-09-30**, commit `b26bcb9`. Nightly.

**What you will notice**

- An error report that could appear after comparing sessions and running the benchmark, when GroupLab was opened again over another screen, no longer happens. (Crash reports 9 and 10).

**Under the hood**

- When Apple refuses to approve a Mac build, the build log now lists Apple's reasons so the problem can be fixed the same day. (Request 55).
- On iPhone and iPad, ahead of the first test build for Apple devices, Export all my data no longer stops partway when rifles, barrels or loads are saved.
- On iPhone and iPad a GroupLab data file opened from Files now shows in Settings what importing it would add.
- On iPhone and iPad the torch on Auto now comes on gently, steps up only while the paper is dim, and dims or goes off on glare.
- The iPhone and iPad test run follows the new Capture start with Choose a photo and its links.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.133

**2026-09-30**, commit `33bcca2`. Nightly.

**What you will notice**

- The target designer now takes a bull size in mil, MOA or inches, read at the sheet's distance, with no new built-in sheets.
- Every app can now export all of your data to one file that any GroupLab imports, without overwriting or duplicating anything.
- Any sheet can now print its bulls in black, blue or red, with large solid areas as a lighter tint, and GroupLab finds the color from the photograph.
- On the phone, a figure withheld for too few shots now wraps its words instead of being cut off, and the choices in Settings show their circle beside their words. (Shared phone screens).
- The Targets preview is now drawn live from the same shapes as the printed PDF, so it is exactly what prints and stays sharp at any zoom, with Open as PDF beside it.
- On the phone, Capture is now the first screen with your caliber row and Choose a photo beside Print a target, Compare can stack each load's group on one center with a tap, and every hole's circle matches your bullet.

**Under the hood**

- Importing a data file now copes with two sessions read from the same picture in the same second.
- The detection scoreboard can now score store-bought targets, from a scan before and after shooting.
- The Mac build is ready to be signed and notarized as soon as the Apple certificate is in place, so it will open without a Terminal command.
- The iPhone and iPad test run's hand-marked aim points now measure in true inches.
- The iPhone and iPad test run marks ten aim points by hand and shows each Settings section from its heading.
- The iPhone and iPad test run's tour of the shared screens builds again.
- The iPhone and iPad test run shows hand-marked aim points and each Settings section from its heading.
- The iPhone and iPad test run's tour of the shared screens builds without warnings.
- The iPhone and iPad test run now opens and photographs every screen the phone shares with Android.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.132

**2026-09-30**, commit `d66fa51`. Nightly.

**What you will notice**

- Every split between panes can now be dragged by a visible grip and is remembered, and Settings has Reset layout to put them all back.
- In Settings, the three sharing sections now show their choice and one short line, with the full explanation one tap away under More.
- On the phone, the torch on Auto now starts low, brightens only while the paper is too dim, and dims or turns off on glare or in bright light.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.131

**2026-09-30**, commit `dfb6bc5`. Nightly.

**Under the hood**

- The iPhone and iPad build loads its project files again after the signing change.
- When the Apple secrets are set, the iPhone and iPad test build will be signed with its share sheet entry included.
- The black idle screen now reaches every edge of an iPhone or iPad, behind the clock and the home bar too.
- The iPhone and iPad test run answers the scope question first and reports what shows behind the idle screen.
- The iPhone and iPad test run now checks sharing, printing, pasting a picture and the black idle screen.
- The iPhone and iPad test run now opens GroupLab's share address the way the share sheet does.
- The iPhone and iPad test run now checks that a picture opened in GroupLab is read to the end.
- The iPhone and iPad application now carries its share sheet entry inside it, ready for the first test build.
- The guide explains that on iPhone and iPad a Google Photos picture is shared into GroupLab.
- On iPhone and iPad a photograph can be chosen from Photos or Files, or shared into GroupLab from Google Photos and any other app.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.130

**2026-09-30**, commit `1bafd04`. Nightly.

**What you will notice**

- GroupLab now asks whether your scope is in mil or MOA, and the zero correction, clicks, dope and hit chance then read in that unit alone, with each rifle keeping its own scope unit.
- If GroupLab Dev cannot install an update without your tap, it now asks for it next time you open it instead of waiting silently.
- While GroupLab reads a target, a picture taken at an angle is now named from its square codes in about a second rather than up to half a minute.
- On the phone, holes are fixed on a page of their own with zoom, a crosshair and undo, the result's picture stands upright, and the camera now guides by what the picture will read.

**Under the hood**

- The black resting screen for overnight testing now covers the whole screen, edges included, on every phone.
- The iPhone and iPad test run now checks the camera's fallback before anything else changes the screen.
- The iPhone and iPad application gains its own camera screen, which still has to be checked on a real iPad before it is relied on.
- The iPhone and iPad self-test reads its picture the way a picture chosen from Files is read.
- On iPhone and iPad a picture is now analyzed instead of stopping with "Operation is not supported on this platform".
- GroupLab for iPhone and iPad can now start its image analysis, which failed on its first call before.
- The iPhone and iPad self-test now explains a failure of the image analysis in words, so it can be fixed.
- The iPhone and iPad self-test now starts on the simulator, so every screen and the image analysis are checked there.
- GroupLab for iPhone and iPad keeps the image analysis it needs when the application is linked, ahead of the first test build for Apple devices.
- On iPhone and iPad a test sitting can show the black idle screen, and the screen is kept on only while it does.
- On iPhone and iPad, a photograph saved as HEIC can be chosen or pasted and is read like any other picture.
- GroupLab for iPhone and iPad keeps its settings and records working in the release build, ahead of the first test build for Apple devices.
- GroupLab for iPhone and iPad now builds with the Xcode 26 tools, ahead of the first test build for Apple devices.
- GroupLab for iPhone and iPad now builds and is tested on the simulator, ahead of the first test build for Apple devices.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.129

**2026-09-29**, commit `6cf5d40`. Nightly.

This build has no change to the application; it behaves exactly as nightly 128 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.128

**2026-09-29**, commit `6cf5d40`. Nightly.

**What you will notice**

- On Android you can now choose a target photo from any photo app, including Google Photos, Samsung Gallery, your phone maker's own gallery and cloud-only photos, or share one or several into GroupLab, with the download shown and a smaller copy called out.
- On the phone, Sessions can compare loads with each load's name on its own line above its range, every session named by its load, date and time, and long values in All figures moved under their names.
- GroupLab Dev can now install its own updates without asking you to tap, once it has updated itself the first time.
- A hole in a photograph taken off square that GroupLab read together with the printed rings beside it is now marked on the hole itself and listed for you to check, instead of being missed, counted twice or marked beside it.

**Under the hood**

- Every build now measures how well hole finding holds up under shadows, glare, curled paper, blur and poor light, and fails if it gets worse.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.127

**2026-09-29**, commit `f7298aa`. Nightly.

**What you will notice**

- After GroupLab Dev updates itself with your tap, it now says which nightly it updated to, with a link to what changed.
- Each photo corrected to real inches with your printer's scale now says which day's printer check it used, and after you calibrate or service the printer GroupLab offers a new check.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.126

**2026-09-29**, commit `2ed5801`. Nightly.

**What you will notice**

- On the computer and the phone, a result now makes a one-page report and a picture to share with its results box and mean radius circle, and the computer gains the phone's Shots and clicks, Zero from this group window and aim point colors.
- The 2 MOA sheets Unholy asked for are in the Targets library: nine 2.00 in bulls on a Letter or A4 page, as one page or a set of three, with the plain, C or E bull.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.125

**2026-09-29**, commit `0011246`. Nightly.

**What you will notice**

- GroupLab Dev now updates itself on Android: it looks for a newer nightly, downloads it on Wi-Fi, checks its signature and signing key, and installs it when you leave the app, with Update now and a switch for automatic installs in Settings under About.
- The list of other people's software GroupLab is built with now names what the phone version uses, including the camera and update libraries.
- Tap any number to switch its units now works in the full CEP table too: each number there shows its own unit and switches alone. (Question 70).

**Under the hood**

- Work toward the iPhone and iPad version: a black resting screen for overnight testing on screens that can burn in.
- Work toward the iPhone and iPad version: the phone's screens are now checked on every build on computers of all three kinds.
- The phone's screens are now shared with the iPhone and iPad version being built, with nothing changed in what you see on Android.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.124

**2026-09-29**, commit `b856d1f`. Nightly.

**What you will notice**

- On the phone, a target with several aim points shows each in its own color with its own figures, and another can be added from the result.
- On the phone, Zero from this group can hand the group's offset to Ballistics, whose dope then includes it at every range.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.123

**2026-09-29**, commit `612dd50`. Nightly.

**What you will notice**

- Fudd buster mode, Unholy's idea, shows with your own shots why a few shots mislead: the tightest and widest three-shot groups among them, what averaging small groups would have said, and the zero chased five shots at a time.
- On the phone, a result now lists every shot with its offset and clicks and a Counted switch to leave one out, and Zero from this group says where the group sits and whether to dial.
- On the phone, a target GroupLab did not print can now be marked by hand: set a known length and the aim point, then add each hole under a crosshair that stays in the middle.
- The status bar now says when the target was saved and where, with Show in folder, and it saves by itself once you change or accept it; Settings can switch to a Save button instead.
- A tap on a number now switches that number alone, and GroupLab remembers the unit for that figure; the others follow Settings as before.
- GroupLab's license now allows it to be distributed through Apple's App Store and its test service, as work on an iPhone and iPad version begins.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.122

**2026-09-29**, commit `78ee351`. Nightly.

**What you will notice**

- A shot you leave out of the figures is now left out of every figure shown, the report, the saved session and what is sent, with the figure for every shot still beside each one, as Unholy reported.
- On the phone, the camera's words no longer flip between move closer and move back, the torch goes off after the picture, the camera starts again when you come back to GroupLab, and what you frame is what is saved.
- Photographs whose square codes are small in the picture are now named by reading each code enlarged where the sheet's markers put it, instead of being refused.
- On the phone, the picture on the result keeps its shape and stands upright, a picture check's notes wrap, the zero's windage shows its amount, and a picture with notes no longer scores 100.
- Importing shots from a CSV file now starts from GroupLab's guesses at which column is across, the unit, which way is up and whether the numbers are measured from the aim point or the group's center; on the phone it is under Sessions.
- The glossary now says exactly how mean radius, standard deviation and extreme spread are worked out, and the full figures no longer claim their 95% ellipse holds 95% of later shots.
- On the phone, the camera has a bubble level, Camera and Result buttons stay in view, and the shutter answers at once with a sound and a flash.

**Under the hood**

- Each aim point's own figures and each shot's offset and clicks are worked out, ready for the screens being drawn, and the test build times the shutter step by step.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.121

**2026-09-28**, commit `23325ec`. Nightly.

**What you will notice**

- On the phone, a line that names a word from the glossary now explains it when you tap it, as the computer does.
- On the phone, a picture copied in another app can be pasted on the Capture screen and read like a chosen photograph.
- On the phone, a set of tiled sheets can be shared as one large page with cut lines between them, for a plotter.
- Every picture is still checked, but its score now counts how well the sheet's markers agree and no longer marks down a tilt GroupLab has corrected, so it follows how well a picture measures.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.120

**2026-09-28**, commit `fc8a902`. Nightly.

**What you will notice**

- Photos can now be corrected to real inches with your printer's scale measured once, across and down, from the new printer check page (a card and one photo, a caliper, a ruler or a scanner), and every photo's paper edge is checked for a sheet printed with Fit to page.
- Tap any number to switch its units: angles between MOA and mil, sizes between inches and centimeters, distances between yards and meters, everywhere at once and remembered; right-click or press and hold for every unit.
- Photographs of GroupLab sheets can now be corrected to real inches with your printer's scale: scan one sheet, or measure one distance between two bulls with a ruler, and GroupLab remembers it for that printer.
- A photograph of a sheet that is not lying flat, curled or bowed across its width, is now measured through every printed marker instead of being refused.
- On the phone, a picture shared into GroupLab from another app now opens like a chosen photograph, a result can share its shots as a CSV file, and a large sheet says how to photograph it.
- The printed volunteer pack's ruler check now spells center the American way, as the rest of GroupLab does.
- The volunteer pack's page of instructions now suits the sheet: a zeroing grid is checked by its printed bar and shot as one group at its diamond, and the photographs follow the phone's Guided and Manual camera.

**Under the hood**

- The phone's camera screen now records which of its words and buttons are showing, so the build can be checked on a phone with nobody holding it.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.119

**2026-09-28**, commit `9046087`. Nightly.

**What you will notice**

- On the phone, Ballistics is a tab of its own: the dope for your rifle and load, the trajectory, and the chance of a hit with the group from a result.
- On the phone, Sessions can compare loads: tick two or more, then see one figure at a time with the range each could really be and whether the shots can tell them apart.
- On the phone, the result now opens into every figure the desktop shows, each with a tap to say what it means and what your number of shots can tell; Shots Needed to Zero has a page of its own.
- On the phone, you tap the bulls you fired at on the sheet itself, so each shot is measured from its own bull.
- Every picture on the phone is now checked, taken or chosen: a score on a red, amber and green bar, with numbered notes on the picture saying what GroupLab corrected and what would help next time.
- On the phone, the camera's words, shutter and Back now show over the live picture, and you choose Guided or Manual: it takes the picture itself when everything is right, or when you press.
- In Shots Needed to Zero, within one click is now amber and the closest click teal, on the chart and in the phone's table alike.
- On the phone, a set of sheets is a checklist of what is read and what is still to read, pooled into one group as you go.
- On the phone, a scan says how it was printed, and every size is corrected to real inches.

*The last two lines were added on 2026-09-28: this build carried both, and its notes left them out because a change to the Android application alone was not yet counted as one that ships.*

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.118

**2026-09-28**, commit `401a978`. Nightly.

**What you will notice**

- The four zeroing grids are redrawn as design C3, chosen by Alan with Jylee and Unholy: squares the size of a scope's own clicks, a tick at one click, the numbers outside the grid and a diamond to aim at.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.117

**2026-09-28**, commit `6b80b4e`. Nightly.

**What you will notice**

- The analysis screen's Advanced figures now say how many shots a zeroing group needs to land on the closest click, or within one click, 90, 95 and 99 times in 100; suggested by Jylee.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.116

**2026-09-28**, commit `d211abd`. Nightly.

**What you will notice**

- The preview on the Targets screen now shows every word the sheet prints, its legend, labels, load block and identifier, on the desktop and the phone.
- In the Targets list, Letter sheets now come before A4 in every group, or A4 first where your system is set to a country that uses it.

**Under the hood**

- The target format can now draw the new C3 zeroing grid, with bold legends and its numbers outside the grid; the sheets wait for a detection question before they are offered.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.115

**2026-09-28**, commit `f0a8bbb`. Nightly.

**What you will notice**

- On a phone the result is back in its usual order, with Share and Another target below the sheet, and a tablet or unfolded phone puts the sheet beside its numbers only when held in landscape.
- The phone's Targets screen now starts on Letter paper in the United States and the other countries that use it, where it offered A4.
- Opening GroupLab from another application while it is already running no longer risks an error on a tablet.
- The Ballistics screen is laid out in three columns like the analysis: the settings fold on the left, the chart and a fuller table sit in the middle, and the hold for the range you pick is on the right, with the chance of a hit as a second view of the middle.
- The large format and 300 yard sets now say on the Targets screen that their sheets carry no load block and the load is entered on the session in GroupLab.
- The phone now looks like the desktop: its colors and type, related things on rounded cards, choices you tap as cards, the group's figures as tiles, and a bottom bar with icons.
- GroupLab on Android now has its own icon, the desktop's GroupLab mark, instead of Android's generic one.

**Under the hood**

- The development build can now be given a picture to read by a test, and the phone's log records the most memory an analysis held and how long each step took.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.114

**2026-09-27**, commit `08dca80`. Nightly.

**What you will notice**

- Three more sheets carry the C bull, a black diamond standing on a point with a white center and a small dot, and Design your own sheet and Made for your optic can now draw it too.
- On Android, GroupLab no longer reports that it closed without shutting down when the phone simply closed it in the background, which Android does all the time; a close while you are using it is still reported. (Error report 6).
- A sheet whose printed code happened to hold certain bytes could fail to identify itself when scanned; GroupLab now reads those codes correctly.
- The three large format sheets now print as sets of four Letter or A4 sheets, with the same bulls and spacing, so a home printer can print them; a tabloid or A3 sheet you printed before still reads.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.113

**2026-09-27**, commit `70419ed`. Nightly.

**What you will notice**

- While GroupLab reads a target it now says which step it is on, from loading the picture to measuring the group; on the phone a Cancel button stops it and keeps nothing, and on the desktop a sheet you named by hand can now be canceled too.
- The sheets of a set made for your optic can now be pooled into one group: analyze each sheet in any order, tick them in Session records and choose Pool the chosen, and GroupLab says which sheets of the set are still missing.
- The target library now offers three sheets with a new bull, a black disc with a white center and a small dot, which the aim point test showed can be centered on through a scope at 10x; they sit beside the usual sheets.
- On a group of two to four shots, a mark much larger than the others is now pointed out as possibly two shots, marked as judged from too few marks to be sure.
- A separate development build, GroupLab Dev, now comes with each nightly for testers: it installs beside GroupLab from Google Play, so both can be on one phone, and it says in Settings what it is.
- On the phone, the Capture screen now says to shade the whole sheet or none of it, and to hold the sheet down outside the printed area, because torn tape can look like a hole.
- Settings, under Sharing, now lists every benchmark run with its date, version and time, and every run goes with your next survey report; the published figures count each computer once, by the middle of its runs rather than its best.
- Opening a second target and analyzing it no longer replaces the first target's saved session; each target now keeps its own record.
- On a narrow window or a screen set to 200 percent, the analysis now fits: the side panels narrow, and on the narrowest the figures move under the picture instead of running off the edge.
- On Android, a photo or scan chosen from the phone is now read at the size GroupLab works at instead of at full size first, so a large scan needs far less memory.
- Photographs taken under uneven light are read better: a shadow across part of the sheet no longer hides a hole or turns plain paper into one, and paper near the sheet's edge is no longer mistaken for a hole.
- A session report whose cards are long now keeps them all on its first page, with the plot a little smaller, instead of spilling onto a page of their own.
- GroupLab now checks how much memory a phone or computer has before it reads a large picture, and says so plainly when an image is too big for this machine instead of running out part way through.
- GroupLab now refuses a photograph taken more than 37 degrees off square to the sheet, rather than 40, because tests at the desk showed that is where it starts reading marks that are not shots; it says the angle and the limit when it does.
- The survey question now says plainly what the random number is, and Settings has two new buttons: Reset my survey number, and Delete my survey reports. If you said yes before, GroupLab asks once more before sending anything.

**Under the hood**

- "Made for your optic" on the tour, and a Features page.
- Internal: a one page plan for the hole size test at the range, printed from the same tool as the guides.
- On Android, what GroupLab records while it works now also goes to the phone's system log, without file names or locations, so a problem on a tester's phone can be diagnosed.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.112

**2026-09-27**, commit `a3ac11d`. Nightly.

**What you will notice**

- A sheet can now be given a label of your own, such as the letter written in its serial box, and GroupLab says when you open another copy of a sheet design you have analyzed before; each stays its own session.
- When every shot on a sheet of one shot per bull landed off by the same amount, as an unzeroed rifle does, GroupLab now gives each shot to the bull it was fired at instead of the nearest one, says how far off they all were, and lets you undo it.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.111

**2026-09-27**, commit `3ae5c4b`. Nightly.

**What you will notice**

- On a target GroupLab did not print, you can now place each bull by hand, move several shots onto a bull with a lasso, draw a scale at each bull so an angled photograph is measured right, and keep the bulls as a template for the next sheet; suggested by Unholy (also TNA).
- The analysis screen can now draw and list CEP 99, and under Advanced a circle for any percent you type, each with its range and a plain note when your shots are too few to reach that far out.
- The four zeroing grids are redrawn to be read through a scope: the 100 yard mil grid now reaches a full 1.0 mil each side of the aim in 0.25 mil squares, with heavier lines at every half and whole mil, every heavier line labeled, and each sheet says what its squares are and carries a ruler to check the print, while sheets printed before still read on the computer and the phone.
- Design your own sheet can now make a sheet for your optic: give the distance, the lowest magnification or a red dot's size and the number of shots, and GroupLab sizes a bull you can center on through it and makes as many sheets as the shots need.
- For a target larger than a scanner takes, the Targets screen now says whether a phone photograph of the whole sheet has enough detail, and a tiled target can be printed on one large page with cut lines between its sheets.
- On the phone, units now follow the phone's region, so a US phone asks for yards and gives inches rather than meters and centimeters, and Settings shows the full nightly version.
- When you say yes to the hardware survey, GroupLab now asks whether to run the benchmark now or later, shows how far it has got with a Cancel button, and says when it finished and what it found; Settings shows when it last ran, on the computer and the phone.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.110

**2026-09-26**, commit `51a2058`. Nightly.

**Under the hood**

- A Microsoft Store version of GroupLab can now be built; in it, updates come from the Store and GroupLab's own updater is switched off. Nothing changes in the version you download.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.109

**2026-09-26**, commit `54e06ef`. Nightly.

**What you will notice**

- GroupLab now asks once whether you would like to take part in the hardware survey, a weekly report of what your computer is and how fast GroupLab runs on it; nothing is sent unless you say yes, and you can change your answer in Settings, under Sharing.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.108

**2026-09-25**, commit `5ddd790`. Nightly.

**What you will notice**

- You can share a session with the new Share a session file in the menu, and open one from the phone or another computer with Open a session file; the picture in it carries no location or other photo details.
- Settings now gathers what GroupLab may share (targets, error reports and a coming hardware survey) in one section called Sharing.

**Under the hood**

- Nothing changes on the desktop: the settings file can now also remember the caliber and distance of the last target, which the coming Android app offers again for the next one.
- Nothing changes on the desktop: the code that draws the group plot and saves a session is now shared with the coming Android app, which can photograph a target and show its result.
- A hardware survey and a built-in benchmark are ready but switched off; GroupLab will ask once, and send nothing unless you say yes, when the project's side is installed.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.107

**2026-09-25**, commit `27bd109`. Nightly.

**What you will notice**

- The command line analyzer can now work on a smaller copy of a very large image with --working-megapixels, which the phone application will always do; on the sample it moves each shot by about a thousandth of an inch.

**Under the hood**

- Error reports and the questions about what may be shared now come from code the desktop and the coming Android app share, so both ask in the same words and send in the same way.
- Nothing in the desktop application changes; the Android test build now has a camera screen that says how to hold the phone and takes the picture itself.
- Under the hood: the rules that will tell a phone user how to hold the camera, one instruction at a time, and when to take the picture, are built and tested; nothing on the desktop uses them yet.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.106

**2026-09-25**, commit `e7ef504`. Nightly.

**What you will notice**

- The composite plot's key now sits beside or below the plot instead of over the shots, and on a small plot it folds into a Key button.
- The bull's rings on the composite plot sit further back, darker on the dark theme and lighter on the light one, so the shot outlines stand out in front of them.

**Under the hood**

- Nothing in the application changes; photographs sent to the project now leave the web server once they are safely copied, and the upload page says where they are kept and for how long.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.105

**2026-09-25**, commit `1d92d96`. Nightly.

**What you will notice**

- The bull's rings on the composite plot are wider and darker, and a Group or Whole target switch beside it shows either the group alone or the entire bull; the mouse wheel or a pinch zooms, dragging moves the view, and a double click fits it again.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.104

**2026-09-25**, commit `f377168`. Nightly.

**What you will notice**

- Toggles beside the plot turn the CEP 50, CEP 90 and CEP 95 circles and the extreme spread line on and off, GroupLab remembers them, and the saved report draws the same marks.
- The composite group plot is easier to read: the bull's rings are pale, the shot outlines lighter, the CEP circles green and bolder with CEP 95 added, and the group center and your point of aim are green and blue lines across the whole plot.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.103

**2026-09-25**, commit `bcda1c8`. Nightly.

**What you will notice**

- GroupLab can now send a report when it hits an error, so the problem can be fixed; the first time, it asks whether to send automatically, ask each time, or never, and Settings can change it.
- The two consent choices on the first screen, and the other long choices in the sending question and Settings, now wrap onto several lines instead of running off the edge, so you can read everything you are agreeing to.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.102

**2026-09-25**, commit `5a1e769`. Nightly.

**What you will notice**

- When a mark is large enough to hold three or more shots of the caliber you named, the review now says so and how to mark the rest.
- After you analyze a target, GroupLab now offers to send it to the project to improve detection, asking first unless you choose otherwise; the first time it opens, one screen asks how you want it, and Settings can change that at any time.
- A very large scan, such as a roll sheet, now has its printed codes read at full detail, so it can name itself on Linux and macOS as well as Windows.
- On a target with a single bull, every shot now counts toward that one group, however far out, and the review no longer asks about each shot or says the sheet takes one.
- The codes printed on the roll sheets are searched for in smaller pieces of the image when a first look finds none, so these sheets can name themselves on Linux and macOS.
- A scanned GroupLab sheet whose printed codes could not be found in the whole page now reads them from its corners, so a zeroing grid scanned at 600 dpi names itself instead of asking which sheet it is.
- The zeroing grids now say in the library that they are for sighting in by eye at the bench, and that a zero from a group is shot on a 5x5 sheet.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.101

**2026-09-24**, commit `98da32d`. Nightly.

**What you will notice**

- Settings has a new Error reports section; sending reports of errors to the project is built but not switched on yet, so for now the section says so and nothing is sent.
- A problem report written by hand now keeps its description to 500 characters.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.100

**2026-09-24**, commit `5f08633`. Nightly.

**What you will notice**

- Where a sheet has more bulls than shots and you have not said which you fired at, a short hint beside that choice suggests it; it holds nothing back, and Put this away hides it for that target.
- When GroupLab finds your sheet but no holes on it, it now says so, gives the likely reason, and tells you how to mark the shots by hand, instead of showing an empty result.
- Pressing Set for a caliber while its list of suggestions is open now sets it the first time, instead of failing quietly.
- The zeroing grids now find your shots: before this build GroupLab found the sheet and then refused every hole on it as out of place.
- Choosing a caliber from the suggestions now sets it in one click or with Enter, instead of needing Set pressed twice.
- A scale you set by hand can be set again by tapping the same marks, or changed from the length or rectangle tool, and every figure follows.
- Caliber is spelled caliber everywhere on screen now, including the setup label and the list of cartridges it suggests.
- When GroupLab hits an error and keeps running it now says so, rather than saying it closed unexpectedly, and a report counts repeated errors together.
- With the shot distance entered, each group size now leads with its angle in MOA or your chosen unit, with the size on the paper beneath; a setting puts the size first.

**Under the hood**

- A zeroing grid scanned at 600 dpi now registers and finds its shot once you say which sheet it is; its printed codes can still fail to read on a scan.
- The consent record shipped with the sample now covers what Alan passes on from his testers, and names Unholy for the scan he gave.
- A target sent to the project now carries every part of the photograph's quality score, so the scoring can be tuned from real photographs; sending itself is still switched off.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.99

**2026-09-24**, commit `e84c949`. Nightly.

**What you will notice**

- Settings has a new Sending targets section; sending a target you have analyzed to the project is built but not switched on yet, so for now the section says so and nothing is sent. (Entry 165)
- The upload page on grouplab.org now asks whether your photographs are for testing only or may also be published, and a testing only target is never published. (Entry 165)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.98

**2026-09-24**, commit `099c270`. Nightly.

**Under the hood**

- The article on why a photo cannot tell you your bullet's size now says what a .22 hole's smaller size could be down to, and the test that would tell. (Entry 158)
- GroupLab can now find the printed grid on a commercial gridded target and correct the photograph's perspective and lens from it; it is measured, not yet on a screen. (Entry 158)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.97

**2026-09-24**, commit `88dbc25`. Nightly.

**What you will notice**

- A photograph taken more than 40 degrees off square to the sheet is now refused with the angle named, and every photograph keeps how far off square it was and whether it is good, usable or poor. (Entry 157)
- With the rectangle scale, Find the paper's edges places the four corners on the paper itself when the sheet stands out from what is behind it. (Entry 157)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.96

**2026-09-24**, commit `a70338a`. Nightly.

**What you will notice**

- The Ballistics screen now works out your chance of a hit on a circle, rectangle or IPSC target from your own measured group, first shot and follow-up side by side, with how sure it is and which error is costing you the most hits. (Entry 156)
- Words like sigma, CEP, MOA and bull are underlined with dots in GroupLab and on the website; point at one, or tab to it, for a plain explanation, and click for the whole glossary entry. (Entry 154)
- The user guide no longer says GroupLab can read several sheets of one load as one group, which it cannot yet; the guides and project page now match what each build does. (Entry 159)
- The target library and printing are now one screen, Targets: choose a sheet and everything it takes to print it is beside the list, with no second window. (Entry 155)
- The cartridge list shows .22 centerfire, and the target library's families are spelled the same American way as everything else. (Entry 159)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.95

**2026-09-24**, commit `dbdb3a3`. Nightly.

**What you will notice**

- Shot coordinates can be exported as a CSV file for a spreadsheet, and imported from other target software's CSV by choosing which columns hold the shots. (Entry 169)
- Pinch to zoom now works on a trackpad or touch screen; a touchpad's two finger drag moves the sheet, and Ctrl or Command with a scroll zooms. (Entry 166)
- The bulls you fired at can be chosen a whole row or column at a time from one bull. (Entry 149, 3)
- Target photographs can now be sent from grouplab.org/targets, and the website's top bar says Send a target. The printed volunteer pack gives the new address. (Entry 173)
- Typing a cartridge name in the calibre box now works, so 6.5 Creedmoor gives 0.264 in and GroupLab warns you it is not the .25 calibre; only cartridges two published sources agree on are offered. (Entry 163, 3)
- You can now record the paper a target was printed on and what was behind it, two optional choices on the marking screen, because how big a bullet hole looks depends on both. (Entry 162, 3.2)
- On a Mac, Command Z, Shift Command Z and the other shortcuts now use the Command key, and the undo button says what it will undo. (Entry 166)
- Naming the bulls you fired at and excluding a shot no longer freeze the window for seconds, and the zero correction now says the distance it is for and what it means for your rifle's own zero distance. (Entry 170)
- A build's notes now say only what changed in the application, in whole sentences, and a build that changed nothing in the application is no longer made. (Entry 168, 2 to 4)
- Naming the right calibre no longer makes GroupLab call good holes possibly two; it judges one hole from two against the other holes on your sheet, and tells you when they are a different size from what the calibre suggests. (Entry 161, 3)
- A sheet printed smaller than it should be makes every group read larger, and GroupLab now says so and by how much, instead of wrongly saying the figures were corrected. (Entry 161, 6)
- The analysis screen shows the six figures you read off a target and the zero correction in inches, MOA and mil with the clicks to dial; everything else is under Advanced, and the plot is high contrast. (Entry 169)
- Every word in the application and on the website now uses American spelling: center, caliber, analyze, color. (Entry 169)
- A diagnostics report no longer contains the names of the files you opened, only their type and an anonymous identifier, so a report can be shared without showing what your files are called. (Entry 164, 4)
- A scanned target now reports real inches: if the sheet was printed smaller or larger than it should be, the scan measures that and corrects every size, and a photograph says its figures are in the sheet's own inches. (Entry 171, 1)
- The Equipment button in the left rail now shows a cartridge instead of a rifle that was too thin to read at that size. (Entry 167, 1)
- The calibre, shot distance and rounds fired now sit at the top of the marking panel, marked needed until you answer them or say you do not know. (Entry 163, 4)
- The pan tool, which is the one you start with, now also selects a mark you click, and C switches to it next to V for select. (Entry 163, 1 and 2)
- The analysis screen opens with each judgement as one line, and the reasoning behind it is one click away instead of in the way. (Entry 163, 5)
- GroupLab no longer guesses a cartridge from the size of the holes, which can be wrong by a whole calibre; it tells you what the holes measure and asks what you fired. (Entry 161, 4)

**Under the hood**

- GroupLab's hole centres on scans are now measured against each hole's edge, and a change that moves them further off fails its checks; which centre to report is waiting on a hand-marked comparison. (Entry 170, 4)
- The website's community page now lists the Discord channels and the server's rules, and an article shows a photographed bullet hole whose shadow makes it measure half again the bullet. (Entry 171, 3 and 6)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.94

**2026-09-24**, commit `227917a`. Nightly.

This build has no change to the application; it behaves exactly as nightly 93 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.93

**2026-09-23**, commit `aa8c559`. Nightly.

**What you will notice**

- A sheet your printer shrank still measures correctly, and GroupLab no longer tells you otherwise on the sheet itself or on the print screen. Printing at actual size still matters, for the reason that is actually true. (Entry 152, 3)
- The calibre list now offers 0.222 for a rimfire 22, which was missing: the nearest thing it had was the centrefire 0.224, almost one percent too wide. (Entry 153, 4)
- A target GroupLab did not print can be measured once you set the scale yourself, and the tour now says so on every screen instead of claiming otherwise. (Entry 152, 3 and 4)

**Under the hood**

- Every research article on the website now ends with what the numbers mean for you, and none of them names the developer. (Entry 153, 1 and 2)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.92

**2026-09-23**, commit `57f5a3f`. Nightly.

**What you will notice**

- When the holes on a sheet come out in two clear sizes, GroupLab now measures a hole from the smaller ones and flags the larger ones, instead of measuring nothing and flagging nothing. It still asks you for the calibre. (Entry 149, 2)

**Under the hood**

- Anything GroupLab needs from you is written down in a file now rather than asked for in passing, so nothing is lost and nothing waits on an answer. (Entry 149, 5)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.91

**2026-09-23**, commit `2d8f229`. Nightly.

**What you will notice**

- GroupLab now publishes Mac builds, one for Apple silicon and one for Intel. Nobody has run either on a real Mac, the download page says so beside each one, and it gives the Terminal command macOS needs before it will open unsigned software. (Entry 147)
- A broken or hostile image file that claims to be hundreds of megapixels is now refused with its measured size, instead of being decoded until GroupLab runs out of memory. Real scans are unaffected: the limit is twelve times a 600 dpi letter scan. (Entry 143, 43)
- When you run detection again on a sheet, the marks you had already moved or reassigned are kept where you put them instead of being thrown away, and the button tells you how many it will keep. (Entry 143, 42)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.84

**2026-09-23**, commit `fcdebac`. Nightly.

**What you will notice**

- The screenshots on grouplab.org now show the current version of GroupLab rather than one from several days ago, and the release notes page keeps itself up to date as each nightly build is published. (Entry 144)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.81

**2026-09-23**, commit `2c51788`. Nightly.

This build has no change to the application; it behaves exactly as nightly 78 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.78

**2026-09-22**, commit `aae67d8`. Nightly.

This build has no change to the application; it behaves exactly as nightly 77 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.77

**2026-09-22**, commit `ad41ea1`. Nightly.

This build has no change to the application; it behaves exactly as nightly 76 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.76

**2026-09-22**, commit `f935ab2`. Nightly.

This build has no change to the application; it behaves exactly as nightly 75 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---
## 0.2.0-nightly.75

**2026-09-22**, commit `a82d7f5`. Nightly.

This build has no change to the application; it behaves exactly as nightly 74 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---
## 0.2.0-nightly.74

**2026-09-22**, commit `1ab0b86`. Nightly.

**What you will notice**

- Telling GroupLab which bulls you aimed at now works from the command line too, with the same words the application takes. (Entry 141, 5.3.4)

**Under the hood**

- The bent sheet fits the markers, not the sheet.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---
## 0.2.0-nightly.73

**2026-09-22**, commit `5f1be30`. Nightly.

**What you will notice**

- Opening a photograph or scan is faster: GroupLab used to read and decode the same file three times before showing it to you, and now reads it once. (Entry 130, 6)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---
## 0.2.0-nightly.72

**2026-09-22**, commit `f40260a`. Nightly.

This build has no change to the application; it behaves exactly as nightly 71 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---
## 0.2.0-nightly.71

**2026-09-22**, commit `72afe73`. Nightly.

**What you will notice**

- You can now open a target by dropping the image on the GroupLab window, or by pasting it with Ctrl+V, including a screenshot or an image copied from a browser. A file that turns out not to be an image now says so instead of ending the session. (Entry 137)
- You can now say which bull a shot belongs to straight from the shots list, and tick several shots to move them all to one bull in a single step that undoes in one go. Selecting a shot also highlights every review item that is about it. (Entry 141, 5.3)
- Where you have entered a string of velocities, GroupLab now draws them with the mean and spread marked, gives the SD with the range that many shots really pins it to, and says that an extreme spread can only be compared with another string of the same length. (Entry 141, 5.2.5)
- The Session records screen now draws one load's sessions over time, each with its uncertainty, and says whether the sessions can really tell that the load is getting better or worse. (Entry 141, 5.2.4)
- When a photograph has more than one target sheet in it, GroupLab now says how many it can see and which one the figures are about, instead of quietly measuring whichever it found first. (Entry 130, 2c)
- An image that another program has open for a moment, such as a scan your scanner has only just finished writing, now opens after a short wait instead of being refused. (Entry 141)

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---
## 0.2.0-nightly.66

**2026-09-22**, commit `398f987`. Nightly.

**What you will notice**

- Where you have recorded the velocities for a group, GroupLab now draws each shot's distance from the centre in the order you fired them, and says whether the group really opened up or whether that is what a group of that size looks like anyway. (Entry 141, 5.2.3)
- The analysis now draws how far your shots spread across and up and down, on one scale, and says plainly whether the shots can tell the two apart or whether the group is only lopsided the way small groups usually are. (Entry 141, 5.2.2)
- You can now tell GroupLab which bulls you aimed at, by rows, by the same columns of every row, or as a list, and it reads the sheet that way instead of giving each shot to whichever bull it landed nearest. It says back what you told it. (Entry 141, 5.3.4)
- On a sheet with enough holes, GroupLab now works out what one hole looks like from the sheet itself rather than from the calibre you entered, so a photograph no longer reports most of its holes as possibly two shots. Entering the calibre still helps it find small holes.
- When GroupLab guesses the calibre from the holes, it now offers one of the diameters people actually shoot rather than a raw measurement, with any it cannot tell apart listed beside it, and it picks from a pistol list when your record says pistol. It no longer guesses at all from a photograph, where holes read far wider than they measure.

**Under the hood**

- Saying which bulls were aimed at, and a red I caused.
- The scales held by a test, and a measurement instead of a squint.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.49

**2026-09-22**, commit `b5ea04c`. Nightly.

**What you will notice**

- The ballistics page has an imperial and metric switch, and the numbers in its boxes are converted rather than just relabelled. Compare loads now shows each load's velocity and its spread beside the group. (Entry 131, 8 and 10)
- New target, on Ctrl+N or from the menu, clears the sheet and starts again, and a sheet with edits you have not saved now asks whether to save or discard them before it goes. (Entry 140, 1.4 and 2)
- An update published by a newer build can no longer stop older builds from updating themselves, and the update bar now lists every build you skipped, newest first, with what each one changed. (Entry 139)
- GroupLab no longer flags every hole on a sheet as possibly two shots when the calibre does not fit what you were shooting; it asks once whether the calibre is right. It also no longer tells you that you fired a number of rounds you never entered. (Entry 140, 3)

**Under the hood**

- Question 38 answered by measuring: a photograph has no hole size factor.

### Known issues

- On a photograph, a stated calibre can still read the holes as larger than they are, because holes photographed in low light measure wider than the same holes scanned. This build asks once rather than flagging every hole, which is the nightly.44 problem fixed; the sizes themselves are addressed in a later build. Leaving the calibre empty on a photograph still gives the best result.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.44

**2026-09-22**, commit `75ff2e8`. Nightly.

**What you will notice**

- Opening a new target no longer carries the last one's calibre, rounds fired or distance over to it, which was flagging every hole on the new sheet as possibly two. There is a button to copy that setup across when you do want it.

### Known issues

- On a photograph, a stated calibre can still flag most of the holes as possibly two shots, because holes photographed in low light read larger than the same holes scanned. Leave the calibre empty on a photograph until this is settled, and GroupLab judges the holes against each other instead.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.43

**2026-09-22**, commit `6545cf2`. Nightly.

**What you will notice**

- Fixed an update that older builds refused as unsigned, which had left them unable to update themselves at all.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.42

**2026-09-22**, commit `6616330`. Nightly.

**What you will notice**

- Compare loads now draws each load's mean radius and sigma with the range it could really be, so you can see at a glance whether the shots can tell two loads apart at all.
- Fixed an update that installed and then did not reopen GroupLab. The installer was bringing it back before it had finished, and it closed again immediately.
- Each build's release notes now list only what changed since the previous build, instead of repeating the whole history every time.

**Under the hood**

- The update bar is the one place that combines versions.

### Known issues

- **This build cannot update older builds.** It carried a field in its update information that builds up to nightly 41 do not know, and those builds refuse it as unsigned. Fixed in nightly 43; anybody stuck on an older build can install the newest by hand from the downloads below.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.37

**2026-09-22**, commit `e551dbb`. Nightly.

**What you will notice**

- The ballistics page now draws the trajectory as a curve against range, with drop, wind drift, velocity and energy each on their own, and your zero marked on it.
- Rifles, barrels and loads now have their own Equipment screen, with a full form for each and earlier values offered as you type, instead of the cramped box with one field that meant two different things.

### Known issues

- **Installs and does not reopen.** Pressing Install and restart updates GroupLab and then leaves it closed, and starting it again by hand reports a crash. The installer was bringing GroupLab back before it had finished writing its files. Fixed in the next build.
- The notes on the release itself still open with the whole history since nightly 18. Fixed in the next build.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.35

**2026-09-22**, commit `5690ded`. Nightly.

**What you will notice**

- The zero correction can now show you where your group landed against where you aimed, with the uncertainty around it, so you can see whether it is worth dialling.
- There is now a Release notes page on grouplab.org listing every build and what changed in it, and the update bar can open it at the version being offered.
- The zero correction no longer has its direction cut off at the edge of the panel on a smaller window.
- Every hole on all six test sheets is now found. A hole the paper tore rather than punched was being refused, and a torn hole was being counted as two shots.
- The installer now shows the GroupLab icon instead of a generic one, in your downloads and on the taskbar while it runs.

### Known issues

- **Installs and does not reopen**, as nightly 37 does. Fixed after 37.
- The notes on the release itself open with the whole history since nightly 18.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.31

**2026-09-21**, commit `85a4ac4`. Nightly.

**What you will notice**

- Where you have not said what you were shooting, GroupLab now reads a likely calibre from the holes and asks you to confirm it before accepting, because knowing it finds holes that would otherwise be refused.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.30

**2026-09-21**, commit `3c6db96`. Nightly.

This build has no change to the application; it behaves exactly as nightly 29 does.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.29

**2026-09-21**, commit `dd57b31`. Nightly.

**What you will notice**

- GroupLab now reads the likely calibre from your holes and asks you to confirm or correct it, because knowing it lets GroupLab find holes it would otherwise refuse.
- The shot distance now has its own yards or metres choice beside it, so you can type it in the unit you think in without changing a setting.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.28

**2026-09-21**, commit `37686b9`. Nightly.

**What you will notice**

- You can now click a hole to edit it, move it with the arrow keys, call it a flyer without dropping it from the group, set its size by hand and leave a note on it.
- Every figure on the analysis page now has a ? beside it explaining what it means and what the number of shots does to it.
- The analysis page can now be switched between inches with MOA and centimetres with mil, and remembers which you chose. Your saved sessions are unchanged either way. (Entry 131, 3.2)
- The analysis page can now show your group's mean radius per 100 yards on a scale, against rules of thumb quoted on a Hornady podcast, with a plain note on how much weight your shot count can carry. (Entry 131, 6.1)
- The zero correction is now shown in MOA, mil, inches and centimetres at once, with the number of clicks to turn where your rifle's scope details are recorded. (Entry 131, 3.1)
- Every figure on the analysis page can now tell you what it means in two or three plain sentences, including what having only a few shots does to it. (Entry 131, 4)
- Every edit now shows a short line at the bottom right saying what changed, with an Undo button beside it, instead of changing the sheet silently. (Entry 131, 9)
- A sheet where the whole group landed away from where it was aimed is now measured against the bulls you actually shot at, once you tell GroupLab which those were.
- Rifles and loads now keep everything you type about them. Sight height, zero distance, muzzle velocity, ballistic coefficient and the rest were being lost when the records were saved.
- The download is about a third smaller, and the installed application about 128 MB smaller, with no change to what it does. (Entry 132, 2)

**Under the hood**

- Native libraries only for the platforms anything runs on.

### Known issues

- This build's notes said GroupLab "now works out where your group actually landed before deciding which bull each shot belongs to". That was not true of this build: the working part was there and nothing on any screen could reach it. Corrected from nightly 30 onwards.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.27

**2026-09-21**, commit `e5887a8`. Nightly.

**Under the hood**

- Holes off the grid are kept, and question 35 says what that costs.
- The real update ran, and found one more defect.
- A hole cut by the scan edge is still a hole.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.26

**2026-09-21**, commit `b089122`. Nightly.

**Under the hood**

- Doubt travels with the number.
- The scan's stated resolution, offered and never applied.
- Where the group actually landed.
- A shortfall is never silent, and the size gate follows the calibre.

### Known issues

- The notes on the release itself are commit subjects and say nothing useful. Fixed from nightly 27.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.25

**2026-09-21**, commit `03c5821`. Nightly.

### Fixed

- **GroupLab can update itself at last.** Every build before this one refused its own update as unsigned, whichever version it was, so it could never install anything.

**If you are on nightly 18 or earlier you have to install this one, or a later one, by hand, once.** After that it updates itself.

### Known issues

- The notes on the release itself are commit subjects and say nothing useful. Fixed from nightly 27.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.18

**2026-09-21**, commit `a5e90de`. Nightly.

**Under the hood**

- Entries 126 and 127: the support address, and saying plainly when work is done.

### Known issues

- **Cannot update itself.** It refuses its own update as unsigned. Install a later build by hand once.
- The notes on the release itself are commit subjects and say nothing useful.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.16

**2026-09-21**, commit `b39b7af`. Nightly.

**Under the hood**

- The published nightly called itself a development build.
- The write-up, question 33, and two states nothing reached.
- GroupLab installs its own updates.

### Known issues

- **Cannot update itself.** It refuses its own update as unsigned. Install a later build by hand once.

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.14

**2026-09-21**, commit `9db6500`. Nightly.

This build has no change to the application; it behaves exactly as nightly 12 does.

### Known issues

- **Calls itself a development build** in Settings, and never looks for an update.
- **Cannot update itself.**

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.2.0-nightly.12

**2026-09-21**, commit `862aab2`. Nightly. The first automatic nightly.

**What you will notice**

- This is the first build GroupLab published for itself. From here on a build appears after every change that passes its tests, and one download link always points at the newest one.
- The target library now fills the window instead of sitting in a corner of it.
- A real scanned sheet is included in the download, so there is something to try GroupLab on straight away.

**Under the hood**

- Everything that reaches outside GroupLab now goes through one door, so a test can never open a browser or start an installer on somebody's machine.
- The key that proves an update really came from GroupLab, and each build checking an update against the key built into itself.

### Known issues

- **Calls itself a development build** in Settings, and never looks for an update.
- **Cannot update itself.**

**This build's release no longer exists on GitHub**, so there is nothing to download from it. The entry stays as the record of what the build was.

---

## 0.1.0

**2026-09-21**, commit `5a4cd07`. The first build anyone could download.

### New

- The Windows installer and zip, and the Linux tarball. Each carries its own .NET runtime, so nothing else has to be installed.

### Known issues

- **Published as a release by mistake.** GroupLab is unreleased, and this is marked a pre-release now.
- **Cannot update itself.** Install a nightly by hand.

This build is deliberately not linked for download. It was published as a release when it should not have been, it cannot update itself, and sending anybody to it now would be sending them to the one build that is a dead end. It is listed here because it happened, not because it is worth installing.
