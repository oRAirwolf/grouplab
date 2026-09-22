# Release notes

Every build of GroupLab anyone could download, newest first. This file is the source of truth: the page at <https://grouplab.org/releases/> is built from it, and it reads here on GitHub too.

GroupLab is unreleased. Everything below is a pre-release, and the version numbers say so.

**How to read this.** Each build lists what a person would notice, under New, Fixed and Changed. Anything a person would not notice, a test or an internal change, is not listed. Where a build has something wrong with it that matters, it says so under **Known issues**.

**Why the nightly numbers skip.** A nightly is numbered by its run, and a run that is cancelled or skipped still takes its number. Two things caused most of the gaps, both fixed on 2026-09-21: every push went to two branches and started two runs, so one of each pair was cancelled, and a run whose commit was already published skipped rather than publishing again.

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
