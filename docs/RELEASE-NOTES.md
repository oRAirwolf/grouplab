# Release notes

Every build of GroupLab anyone could download, newest first. This file is the source of truth: the page at <https://grouplab.org/releases/> is built from it, and it reads here on GitHub too.

GroupLab is unreleased. Everything below is a pre-release, and the version numbers say so.

**How to read this.** Each build lists what a person would notice, under New, Fixed and Changed. Anything a person would not notice, a test or an internal change, is not listed. Where a build has something wrong with it that matters, it says so under **Known issues**.

**Why the nightly numbers skip.** A nightly is numbered by its run, and a run that is cancelled or skipped still takes its number. Two things caused most of the gaps, both fixed on 2026-09-21: every push went to two branches and started two runs, so one of each pair was cancelled, and a run whose commit was already published skipped rather than publishing again.

---

## 0.2.0-nightly.76

**2026-09-22**, commit `f935ab2`. Nightly.

Nothing in this build changes what you see or do. It carries internal work only.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.76)

---
## 0.2.0-nightly.75

**2026-09-22**, commit `a82d7f5`. Nightly.

Nothing in this build changes what you see or do. It carries internal work only.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.75)

---
## 0.2.0-nightly.74

**2026-09-22**, commit `1ab0b86`. Nightly.

### New

- Telling GroupLab which bulls you aimed at now works from the command line too, with the same words the application takes.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.74)

---
## 0.2.0-nightly.73

**2026-09-22**, commit `5f1be30`. Nightly.

### Changed

- Opening a photograph or scan is faster: GroupLab used to read and decode the same file three times before showing it to you, and now reads it once.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.73)

---
## 0.2.0-nightly.72

**2026-09-22**, commit `f40260a`. Nightly.

Nothing in this build changes what you see or do. It carries internal work only.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.72)

---
## 0.2.0-nightly.71

**2026-09-22**, commit `72afe73`. Nightly.

### New

- You can now open a target by dropping the image on the GroupLab window, or by pasting it with Ctrl+V, including a screenshot or an image copied from a browser. A file that turns out not to be an image now says so instead of ending the session.
- You can now say which bull a shot belongs to straight from the shots list, and tick several shots to move them all to one bull in a single step that undoes in one go. Selecting a shot also highlights every review item that is about it.
- Where you have entered a string of velocities, GroupLab now draws them with the mean and spread marked, gives the SD with the range that many shots really pins it to, and says that an extreme spread can only be compared with another string of the same length.
- The Session records screen now draws one load's sessions over time, each with its uncertainty, and says whether the sessions can really tell that the load is getting better or worse.

### Fixed

- When a photograph has more than one target sheet in it, GroupLab now says how many it can see and which one the figures are about, instead of quietly measuring whichever it found first.
- An image that another program has open for a moment, such as a scan your scanner has only just finished writing, now opens after a short wait instead of being refused.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.71)

---
## 0.2.0-nightly.66

**2026-09-22**, commit `398f987`. Nightly.

### New

- The analysis now draws how far your shots spread across and up and down, on one scale, and says plainly whether the shots can tell the two apart or whether the group is only lopsided the way small groups usually are.
- Where you have recorded the velocities for a group, GroupLab now draws each shot's distance from the centre in the order you fired them, and says whether the group really opened up or whether that is what a group of that size looks like anyway.
- You can now tell GroupLab which bulls you aimed at, by rows, by the same columns of every row, or as a list, and it reads the sheet that way instead of giving each shot to whichever bull it landed nearest. It says back what you told it.

### Fixed

- On a sheet with enough holes, GroupLab now works out what one hole looks like from the sheet itself rather than from the calibre you entered, so a photograph no longer reports most of its holes as possibly two shots. Entering the calibre still helps it find small holes.

### Changed

- When GroupLab guesses the calibre from the holes, it now offers one of the diameters people actually shoot rather than a raw measurement, with any it cannot tell apart listed beside it, and it picks from a pistol list when your record says pistol. It no longer guesses at all from a photograph, where holes read far wider than they measure.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.66)

---

## 0.2.0-nightly.49

**2026-09-22**, commit `b5ea04c`. Nightly.

### New

- The ballistics page has an imperial and metric switch, and the numbers in its boxes are converted rather than just relabelled. Compare loads now shows each load's velocity and its spread beside the group.
- New target, on Ctrl+N or from the menu, clears the sheet and starts again, and a sheet with edits you have not saved now asks whether to save or discard them before it goes.

### Fixed

- An update published by a newer build can no longer stop older builds from updating themselves, and the update bar now lists every build you skipped, newest first, with what each one changed.
- GroupLab no longer flags every hole on a sheet as possibly two shots when the calibre does not fit what you were shooting; it asks once whether the calibre is right. It also no longer tells you that you fired a number of rounds you never entered.

### Known issues

- On a photograph, a stated calibre can still read the holes as larger than they are, because holes photographed in low light measure wider than the same holes scanned. This build asks once rather than flagging every hole, which is the nightly.44 problem fixed; the sizes themselves are addressed in a later build. Leaving the calibre empty on a photograph still gives the best result.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.49)

---

## 0.2.0-nightly.44

**2026-09-22**, commit `75ff2e8`. Nightly.

### Fixed

- Opening a new target no longer carries the last one's calibre, rounds fired or distance over to it, which was flagging every hole on the new sheet as possibly two. There is a button to copy that setup across when you do want it.

### Known issues

- On a photograph, a stated calibre can still flag most of the holes as possibly two shots, because holes photographed in low light read larger than the same holes scanned. Leave the calibre empty on a photograph until this is settled, and GroupLab judges the holes against each other instead.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.44)

---

## 0.2.0-nightly.43

**2026-09-22**, commit `6545cf2`. Nightly.

### Fixed

- Fixed an update that older builds refused as unsigned, which had left them unable to update themselves at all.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.43)

---

## 0.2.0-nightly.42

**2026-09-22**, commit `6616330`. Nightly.

### New

- Compare loads now draws each load's mean radius and sigma with the range it could really be, so you can see at a glance whether the shots can tell two loads apart at all.

### Fixed

- Fixed an update that installed and then did not reopen GroupLab. The installer was bringing it back before it had finished, and it closed again immediately.
- Each build's release notes now list only what changed since the previous build, instead of repeating the whole history every time.

### Known issues

- **This build cannot update older builds.** It carried a field in its update information that builds up to nightly 41 do not know, and those builds refuse it as unsigned. Fixed in nightly 43; anybody stuck on an older build can install the newest by hand from the downloads below.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.42)

---

## 0.2.0-nightly.37

**2026-09-22**, commit `e551dbb`. Nightly.

### New

- The ballistics page now draws the trajectory as a curve against range, with drop, wind drift, velocity and energy each on their own, and your zero marked on it.
- Rifles, barrels and loads now have their own Equipment screen, with a full form for each and earlier values offered as you type, instead of the cramped box with one field that meant two different things.

### Known issues

- **Installs and does not reopen.** Pressing Install and restart updates GroupLab and then leaves it closed, and starting it again by hand reports a crash. The installer was bringing GroupLab back before it had finished writing its files. Fixed in the next build.
- The notes on the release itself still open with the whole history since nightly 18. Fixed in the next build.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.37)

---

## 0.2.0-nightly.35

**2026-09-22**, commit `5690ded`. Nightly.

### New

- The zero correction can now show you where your group landed against where you aimed, with the uncertainty around it, so you can see whether it is worth dialling.
- There is now a Release notes page on grouplab.org listing every build and what changed in it, and the update bar can open it at the version being offered.

### Fixed

- The zero correction no longer has its direction cut off at the edge of the panel on a smaller window.

### Changed

- The installer now shows the GroupLab icon instead of a generic one, in your downloads and on the taskbar while it runs.

### Known issues

- **Installs and does not reopen**, as nightly 37 does. Fixed after 37.
- The notes on the release itself open with the whole history since nightly 18.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.35)

---

## 0.2.0-nightly.31

**2026-09-21**, commit `85a4ac4`. Nightly.

### New

- Where you have not said what you were shooting, GroupLab now reads a likely calibre from the holes and asks you to confirm it before accepting, because knowing it finds holes that would otherwise be refused.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.31)

---

## 0.2.0-nightly.30

**2026-09-21**, commit `3c6db96`. Nightly.

Nothing in this build changes what you see or do. It carries internal work only.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.30)

---

## 0.2.0-nightly.29

**2026-09-21**, commit `dd57b31`. Nightly.

### New

- GroupLab now reads the likely calibre from your holes and asks you to confirm or correct it, because knowing it lets GroupLab find holes it would otherwise refuse.

### Changed

- The shot distance now has its own yards or metres choice beside it, so you can type it in the unit you think in without changing a setting.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.29)

---

## 0.2.0-nightly.28

**2026-09-21**, commit `37686b9`. Nightly.

### New

- Every figure on the analysis page now has a ? beside it explaining what it means and what the number of shots does to it.
- The analysis page can now be switched between inches with MOA and centimetres with mil, and remembers which you chose. Your saved sessions are unchanged either way.
- The analysis page can now show your group's mean radius per 100 yards on a scale, against rules of thumb quoted on a Hornady podcast, with a plain note on how much weight your shot count can carry.
- The zero correction is now shown in MOA, mil, inches and centimetres at once, with the number of clicks to turn where your rifle's scope details are recorded.
- Every edit now shows a short line at the bottom right saying what changed, with an Undo button beside it, instead of changing the sheet silently.

### Fixed

- Rifles and loads now keep everything you type about them. Sight height, zero distance, muzzle velocity, ballistic coefficient and the rest were being lost when the records were saved.

### Changed

- The guides now cover updates, the shot editor, the explanations beside each figure and why naming your calibre matters.
- The download is about a third smaller, and the installed application about 128 MB smaller, with no change to what it does.

### Known issues

- This build's notes said GroupLab "now works out where your group actually landed before deciding which bull each shot belongs to". That was not true of this build: the working part was there and nothing on any screen could reach it. Corrected from nightly 30 onwards.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.28)

---

## 0.2.0-nightly.27

**2026-09-21**, commit `e5887a8`. Nightly.

The first build whose notes were written for a person rather than taken from commit subjects. It opens with a plain summary of everything since nightly 18, because those builds went out with notes that said nothing.

### Fixed

- When GroupLab finds fewer holes than the shots you fired, it now says so and names the bulls with nothing on them, instead of showing a clean result you have no reason to question.
- Holes from small calibres such as .22 LR are no longer refused as too small when you have entered the calibre.
- A hole cut off by the edge of the scan is detected instead of being ignored.
- A shot that landed off the bulls is kept and offered, instead of being dropped.

### Changed

- Where the shot to bull assignment is not certain, the group figures say so, and the zero correction is withheld rather than being given from shots that may belong elsewhere.
- A blank sheet scanned on a flatbed can use the scan's own resolution as its scale. GroupLab shows the number and you can refuse it.
- The Support button opens the support page at grouplab.org, and the report window tells you both ways to send a report.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.27)

---

## 0.2.0-nightly.26

**2026-09-21**, commit `b089122`. Nightly.

The build used to prove the updater end to end: nightly 25 updated itself to this one on a real machine, with real clicks, in under two minutes and with no installer window and no administrator prompt.

### Known issues

- The notes on the release itself are commit subjects and say nothing useful. Fixed from nightly 27.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.26)

---

## 0.2.0-nightly.25

**2026-09-21**, commit `03c5821`. Nightly.

### Fixed

- **GroupLab can update itself at last.** Every build before this one refused its own update as unsigned, whichever version it was, so it could never install anything.

**If you are on nightly 18 or earlier you have to install this one, or a later one, by hand, once.** After that it updates itself.

### Known issues

- The notes on the release itself are commit subjects and say nothing useful. Fixed from nightly 27.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.25)

---

## 0.2.0-nightly.18

**2026-09-21**, commit `a5e90de`. Nightly.

### Known issues

- **Cannot update itself.** It refuses its own update as unsigned. Install a later build by hand once.
- The notes on the release itself are commit subjects and say nothing useful.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.18)

---

## 0.2.0-nightly.16

**2026-09-21**, commit `b39b7af`. Nightly.

### Fixed

- The build no longer describes itself as a development build in Settings, and looks for updates.

### Known issues

- **Cannot update itself.** It refuses its own update as unsigned. Install a later build by hand once.

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.16)

---

## 0.2.0-nightly.14

**2026-09-21**, commit `9db6500`. Nightly.

### Known issues

- **Calls itself a development build** in Settings, and never looks for an update.
- **Cannot update itself.**

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.14)

---

## 0.2.0-nightly.12

**2026-09-21**, commit `862aab2`. Nightly. The first automatic nightly.

### Known issues

- **Calls itself a development build** in Settings, and never looks for an update.
- **Cannot update itself.**

[Downloads for this build](https://github.com/oRAirwolf/grouplab/releases/tag/v0.2.0-nightly.12)

---

## 0.1.0

**2026-09-21**, commit `5a4cd07`. The first build anyone could download.

### New

- The Windows installer and zip, and the Linux tarball. Each carries its own .NET runtime, so nothing else has to be installed.

### Known issues

- **Published as a release by mistake.** GroupLab is unreleased, and this is marked a pre-release now.
- **Cannot update itself.** Install a nightly by hand.

This build is deliberately not linked for download. It was published as a release when it should not have been, it cannot update itself, and sending anybody to it now would be sending them to the one build that is a dead end. It is listed here because it happened, not because it is worth installing.
