# Release notes

Every build of GroupLab anyone could download, newest first. This file is the source of truth: the page at <https://grouplab.org/releases/> is built from it, and it reads here on GitHub too.

GroupLab is unreleased. Everything below is a pre-release, and the version numbers say so.

**How to read this.** Every build says what is in it, whoever it affects. **What you will notice** is the part you meet: something on screen, something that behaves differently, something new or gone, something fixed, or a change to what gets installed. **Under the hood** is everything else in the application in plain words, such as a check that now runs or work nobody can perceive yet. Changes to this website, the guides and the research are not listed here, because they are not in the application; the website says when it changes. A build shows only the headings it has, and a build that changed nothing in the application says so in one line. Where a build has something wrong with it that matters, it says so under **Known issues**.

**Every entry was rewritten on 2026-09-24 from its build's own commits** (NOTES-FROM-PLANNING.md entry 168). Notes that had been cut off at a line break are whole again; website, research and documentation changes are gone from the builds, because they are not in the application and the website says when it changes; and a build that changed nothing in the application says so in one line and names the build it is the same as. **Known issues** sections are kept exactly as they were written. Two builds, nightly 25 and nightly 12, keep the text they were published with, because their notes were written before today's checks existed.

**Why the nightly numbers skip.** Up to nightly 91 a nightly was numbered by the workflow run that built it, and a run that was cancelled or skipped still took its number. From nightly 92 the number is the last published build plus one, so from there a gap means a number was never used, and a build that should not have been made is named as such below rather than hidden.

---

## 0.2.0-nightly.104

**2026-09-25**, commit `f377168`. Nightly.

**What you will notice**

- Toggles beside the plot turn the CEP 50, CEP 90 and CEP 95 circles and the extreme spread line on and off, GroupLab remembers them, and the saved report draws the same marks.
- The composite group plot is easier to read: the bull's rings are pale, the shot outlines lighter, the CEP circles green and bolder with CEP 95 added, and the group center and your point of aim are green and blue lines across the whole plot.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.104)

---

## 0.2.0-nightly.103

**2026-09-25**, commit `bcda1c8`. Nightly.

**What you will notice**

- GroupLab can now send a report when it hits an error, so the problem can be fixed; the first time, it asks whether to send automatically, ask each time, or never, and Settings can change it.
- The two consent choices on the first screen, and the other long choices in the sending question and Settings, now wrap onto several lines instead of running off the edge, so you can read everything you are agreeing to.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.103)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.102)

---

## 0.2.0-nightly.101

**2026-09-24**, commit `98da32d`. Nightly.

**What you will notice**

- Settings has a new Error reports section; sending reports of errors to the project is built but not switched on yet, so for now the section says so and nothing is sent.
- A problem report written by hand now keeps its description to 500 characters.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.101)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.100)

---

## 0.2.0-nightly.99

**2026-09-24**, commit `e84c949`. Nightly.

**What you will notice**

- Settings has a new Sending targets section; sending a target you have analyzed to the project is built but not switched on yet, so for now the section says so and nothing is sent. (Entry 165)
- The upload page on grouplab.org now asks whether your photographs are for testing only or may also be published, and a testing only target is never published. (Entry 165)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.99)

---

## 0.2.0-nightly.98

**2026-09-24**, commit `099c270`. Nightly.

**Under the hood**

- The article on why a photo cannot tell you your bullet's size now says what a .22 hole's smaller size could be down to, and the test that would tell. (Entry 158)
- GroupLab can now find the printed grid on a commercial gridded target and correct the photograph's perspective and lens from it; it is measured, not yet on a screen. (Entry 158)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.98)

---

## 0.2.0-nightly.97

**2026-09-24**, commit `88dbc25`. Nightly.

**What you will notice**

- A photograph taken more than 40 degrees off square to the sheet is now refused with the angle named, and every photograph keeps how far off square it was and whether it is good, usable or poor. (Entry 157)
- With the rectangle scale, Find the paper's edges places the four corners on the paper itself when the sheet stands out from what is behind it. (Entry 157)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.97)

---

## 0.2.0-nightly.96

**2026-09-24**, commit `a70338a`. Nightly.

**What you will notice**

- The Ballistics screen now works out your chance of a hit on a circle, rectangle or IPSC target from your own measured group, first shot and follow-up side by side, with how sure it is and which error is costing you the most hits. (Entry 156)
- Words like sigma, CEP, MOA and bull are underlined with dots in GroupLab and on the website; point at one, or tab to it, for a plain explanation, and click for the whole glossary entry. (Entry 154)
- The user guide no longer says GroupLab can read several sheets of one load as one group, which it cannot yet; the guides and project page now match what each build does. (Entry 159)
- The target library and printing are now one screen, Targets: choose a sheet and everything it takes to print it is beside the list, with no second window. (Entry 155)
- The cartridge list shows .22 centerfire, and the target library's families are spelled the same American way as everything else. (Entry 159)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.96)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.95)

---

## 0.2.0-nightly.94

**2026-09-24**, commit `227917a`. Nightly.

This build has no change to the application; it behaves exactly as nightly 93 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.94)

---

## 0.2.0-nightly.93

**2026-09-23**, commit `aa8c559`. Nightly.

**What you will notice**

- A sheet your printer shrank still measures correctly, and GroupLab no longer tells you otherwise on the sheet itself or on the print screen. Printing at actual size still matters, for the reason that is actually true. (Entry 152, 3)
- The calibre list now offers 0.222 for a rimfire 22, which was missing: the nearest thing it had was the centrefire 0.224, almost one percent too wide. (Entry 153, 4)
- A target GroupLab did not print can be measured once you set the scale yourself, and the tour now says so on every screen instead of claiming otherwise. (Entry 152, 3 and 4)

**Under the hood**

- Every research article on the website now ends with what the numbers mean for you, and none of them names the developer. (Entry 153, 1 and 2)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.93)

---

## 0.2.0-nightly.92

**2026-09-23**, commit `57f5a3f`. Nightly.

**What you will notice**

- When the holes on a sheet come out in two clear sizes, GroupLab now measures a hole from the smaller ones and flags the larger ones, instead of measuring nothing and flagging nothing. It still asks you for the calibre. (Entry 149, 2)

**Under the hood**

- Anything GroupLab needs from you is written down in a file now rather than asked for in passing, so nothing is lost and nothing waits on an answer. (Entry 149, 5)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.92)

---

## 0.2.0-nightly.91

**2026-09-23**, commit `2d8f229`. Nightly.

**What you will notice**

- GroupLab now publishes Mac builds, one for Apple silicon and one for Intel. Nobody has run either on a real Mac, the download page says so beside each one, and it gives the Terminal command macOS needs before it will open unsigned software. (Entry 147)
- A broken or hostile image file that claims to be hundreds of megapixels is now refused with its measured size, instead of being decoded until GroupLab runs out of memory. Real scans are unaffected: the limit is twelve times a 600 dpi letter scan. (Entry 143, 43)
- When you run detection again on a sheet, the marks you had already moved or reassigned are kept where you put them instead of being thrown away, and the button tells you how many it will keep. (Entry 143, 42)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.91)

---

## 0.2.0-nightly.84

**2026-09-23**, commit `fcdebac`. Nightly.

**What you will notice**

- The screenshots on grouplab.org now show the current version of GroupLab rather than one from several days ago, and the release notes page keeps itself up to date as each nightly build is published. (Entry 144)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.84)

---

## 0.2.0-nightly.81

**2026-09-23**, commit `2c51788`. Nightly.

This build has no change to the application; it behaves exactly as nightly 78 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.81)

---

## 0.2.0-nightly.78

**2026-09-22**, commit `aae67d8`. Nightly.

This build has no change to the application; it behaves exactly as nightly 77 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.78)

---

## 0.2.0-nightly.77

**2026-09-22**, commit `ad41ea1`. Nightly.

This build has no change to the application; it behaves exactly as nightly 76 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.77)

---

## 0.2.0-nightly.76

**2026-09-22**, commit `f935ab2`. Nightly.

This build has no change to the application; it behaves exactly as nightly 75 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.76)

---
## 0.2.0-nightly.75

**2026-09-22**, commit `a82d7f5`. Nightly.

This build has no change to the application; it behaves exactly as nightly 74 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.75)

---
## 0.2.0-nightly.74

**2026-09-22**, commit `1ab0b86`. Nightly.

**What you will notice**

- Telling GroupLab which bulls you aimed at now works from the command line too, with the same words the application takes. (Entry 141, 5.3.4)

**Under the hood**

- The bent sheet fits the markers, not the sheet.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.74)

---
## 0.2.0-nightly.73

**2026-09-22**, commit `5f1be30`. Nightly.

**What you will notice**

- Opening a photograph or scan is faster: GroupLab used to read and decode the same file three times before showing it to you, and now reads it once. (Entry 130, 6)

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.73)

---
## 0.2.0-nightly.72

**2026-09-22**, commit `f40260a`. Nightly.

This build has no change to the application; it behaves exactly as nightly 71 does.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.72)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.71)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.66)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.49)

---

## 0.2.0-nightly.44

**2026-09-22**, commit `75ff2e8`. Nightly.

**What you will notice**

- Opening a new target no longer carries the last one's calibre, rounds fired or distance over to it, which was flagging every hole on the new sheet as possibly two. There is a button to copy that setup across when you do want it.

### Known issues

- On a photograph, a stated calibre can still flag most of the holes as possibly two shots, because holes photographed in low light read larger than the same holes scanned. Leave the calibre empty on a photograph until this is settled, and GroupLab judges the holes against each other instead.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.44)

---

## 0.2.0-nightly.43

**2026-09-22**, commit `6545cf2`. Nightly.

**What you will notice**

- Fixed an update that older builds refused as unsigned, which had left them unable to update themselves at all.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.43)

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

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.42)

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
