---
title: What GroupLab sends from your computer
description: Four things leave your machine, all of them only when you ask. Here is the complete list, what is in each, and the rule that stops a fifth appearing by accident.
group: How GroupLab is built
number: 26
written: 2026-09-22
data_date: 2026-09-22
samples: not a measurement: an account of the code, with the test that keeps it true
state: published
no_figure: "A list of network calls has no picture. The one thing a reader could look at, the crash report contents, is quoted in full in the article."
found: GroupLab sends four things, none of them without an action from you, and none containing where you were. A test fails the build if any part of the program learns to reach the outside world on its own.
sure: This describes the code as it stands and the tests that hold it there. It is not an audit by anyone else, and you are welcome to read the source instead.
sources:
  - "The one way out, and the test that keeps it the only one: `src/GroupLab.Core/Updates/OutsideWorld.cs` and `tests/GroupLab.Core.Tests/OneWayOutTests.cs`."
  - "What a crash report contains and how it is sent: `docs/CRASH-REPORTING.md`."
---

## The whole list

1. **An update check.** A plain request for a small file saying what the newest build is.
2. **An update download**, if you accept one.
3. **An error report**, when GroupLab hits an error: sent by itself only if you said so, offered otherwise, never if you said never.
4. **A target**, if you choose to send one to the project: from the page at `grouplab.org/targets/`, or from GroupLab itself after you analyze it.

A fifth, **the hardware survey**, is built and switched off until its receiver is installed on the server. When it opens, GroupLab
asks once, on the same screen as the questions above, and nothing is sent unless you say yes: then a short report of what your machine
is and how fast GroupLab ran on it goes at most once a week. What a report holds is listed on that screen and in `docs/SURVEY.md`, and
it never holds your name, a file name, a photograph or a location.

That is all of it. There is no analytics, no usage reporting, no license check and no phoning home. Nothing is sent while you are marking a target, and nothing is sent because you opened the program.

## The update check

A GET for a small JSON file. The only thing it says about you is a User-Agent of `GroupLab` and its version number, because a server needs to know which build is asking in order to answer sensibly.

It does not send your machine name, your account, your screen, your Windows version or a unique identifier of any kind. There is nothing to correlate one check with another beyond the fact that somebody on that version asked.

You can turn it off, and then nothing is sent at all.

## An error report, and only as you chose

When GroupLab hits an error, or closes without shutting down, it writes a record to a folder on your own machine. The first time it can, it asks whether to send such reports to the project: automatically, only when you say so each time, or never. Until you choose, it asks each time. **This is built but not switched on yet**: until the project's receiver for it opens, GroupLab asks nothing and sends nothing, and a crash is reported by hand as before.

**What a report holds:** the version of GroupLab and the system it runs on, the error and where in GroupLab it happened, and the names of the last few things done, such as setting the caliber, with nothing that was typed into them. Never a photograph or scan, a file name, a location, or anything you wrote. The same error several times goes as one report with a count, and a day's reports are capped at 20.

It goes to grouplab.org, not to GitHub, and the application holds no key or token. The project's server turns it into an issue in a private repository, and the token that lets it do that lives on the server and nowhere else.

**A report you agreed to send is tried again.** If it cannot go at that moment it is kept on your machine and tried again for seven days, never twice once it has gone, and never with a dialog in your way. A report made by hand, from the banner or Report a problem, goes once, as a zip you can look at first, with a description of up to 500 characters if you write one.

Two things about all of it are deliberate:

**No authentication.** There is no key, because any secret inside an open source program is public the moment it ships, and pretending otherwise would be theatre. So anybody can send a report, and nothing in one is ever taken as an instruction.

**No metadata, no paths.** The log carries no file paths and no image metadata. That is a rule with a test behind it rather than an intention.

## Your photographs, and what is stripped

If you send a target from the page, that is an upload you started, to a page you visited, with a consent record written at the time.

GroupLab can also send one itself, and it asks first. After you analyze a target, a short panel under the figures offers to send it: the image, the holes GroupLab found, the ones you moved, added or removed, what you told it, the figures and the log from that session. Nothing goes until you press Send and choose one of two levels of consent, testing only or may be published. Settings lets you say instead that every target should go, or none. The first time GroupLab opens after an update that carries it, one screen asks which you want, and nothing is chosen for you.

**Unlike a crash report, a target you agreed to send is tried again.** If it cannot go at that moment it is kept on your machine and tried when GroupLab next starts, for seven days, then deleted. Settings shows anything waiting, with a button to discard it. That is the one thing GroupLab may send when it opens, and only because you already said yes to it.

**The photograph leaves with its pixels and without its place.** GroupLab removes the location, the time, the camera's serial and everything else a phone writes beside the picture, and checks that the picture itself is untouched. Where it cannot do that without changing a pixel it saves the picture again without loss, and where even that would be too large it does not send it and tells you why.

**Location is never read.** Not stripped on the way out: never read in the first place, at any point, by any part of GroupLab. A photograph taken on a phone usually carries where it was taken, and a target photograph usually means somewhere you shoot. That is not information this project wants to hold, so the code does not look at it.

## Where it is kept, and for how long

NOTES-FROM-PLANNING.md entries 215 to 217. Nothing people send stays on the web server longer than it is needed, and every place it
can wait has a limit.

- **A target, from the page or from GroupLab.** Within minutes it is rebuilt from its pixels and the file you sent is deleted. The rebuilt
  copy waits on the server until the developer's next pull, which checks it, copies it onto the developer's own machine and into the
  project's private archive hosted by GitHub, proves the archive holds it, and then deletes it from the server. One the server refuses is
  deleted after seven days; one nobody pulls is deleted after sixty.
- **Once kept by the project**, a target is on the developer's machine and in the private archive, which is never made public. It is
  published only if you chose "may be published", and only after it has been looked at. The archive keeps to a budget, and the oldest
  submissions in it may be deleted to stay inside it, never while one is still used by a test or an article.
- **An error report** becomes an issue in the project's private repository on GitHub, and the copy on the server is deleted the moment
  the issue is opened or updated. One that cannot be sent is deleted after thirty days.
- **On your own machine**, a target you agreed to send and could not is tried again for seven days, then deleted, as above.
- **A hardware survey report**, once the survey opens, is counted into totals within the hour and deleted. None is kept on the server
  longer than thirty days whatever happens. What stays is counts: for each installation, only a hash of its random number, the classes
  its machine falls in and the day it was last seen, dropped after 180 days. Nothing smaller than ten machines is ever published.

## The rule that stops a fifth thing appearing

All four go through one interface. Nothing else in the program is allowed to open a browser, fetch a file, start a process or read the clipboard.

That is not a convention, it is a test. `OneWayOutTests` reads every source file in the repository and fails the build if anything outside that one file starts a process with the shell or launches a URI, and a second guard does the same for the clipboard.

**It exists because of a real accident.** The settings page's link to the repository once opened the real default browser, and an automated test that clicks every control it finds opened browser tabs on the developer's machine. The same class of mistake had already printed to a real printer. Both were fixed the same way: one door, and the tests replace it with a recorder that writes down what would have happened.

That is the part worth trusting more than any promise on this page. A rule a person has to remember is a rule that lasts until somebody is in a hurry. A rule that fails the build is a different kind of thing.

## Reading the clipboard

GroupLab can open an image you paste with Ctrl+V. It reads the clipboard **only** at that moment, never on its own and never in the background, and the same one-door rule covers it: exactly one file in the program may touch a real clipboard, and the build fails the day a second one learns how.

## What this means

**You can read this list and then check it.** The point of the test that fails the build is that the list stays four items long without anybody remembering to keep it that way. If you would rather verify than trust, the test is named in the sources and it is short.

**Nothing here is a promise about your network.** It is a statement about what this program does. If something on your machine is watching what it talks to, this article tells you what it should see and nothing more.

**The part worth carrying away:** a program that reaches the outside world in four places can be described in a paragraph, and one that reaches it in forty cannot be described at all. That is the reason for the rule, rather than any particular one of the four.
