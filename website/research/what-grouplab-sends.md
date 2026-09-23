---
title: What GroupLab sends from your computer
description: Four things leave your machine, all of them only when you ask. Here is the complete list, what is in each, and the rule that stops a fifth appearing by accident.
group: How GroupLab is built
number: 26
written: 2026-09-22
data_date: 2026-09-22
samples: not a measurement: an account of the code, with the test that keeps it true
state: published
found: GroupLab sends four things, none of them without an action from you, and none containing where you were. A test fails the build if any part of the program learns to reach the outside world on its own.
sure: This describes the code as it stands and the tests that hold it there. It is not an audit by anyone else, and you are welcome to read the source instead.
sources:
  - "The one way out, and the test that keeps it the only one: `src/GroupLab.Core/Updates/OutsideWorld.cs` and `tests/GroupLab.Core.Tests/OneWayOutTests.cs`."
  - "What a crash report contains and how it is sent: `docs/CRASH-REPORTING.md`."
---

## The whole list

1. **An update check.** A plain request for a small file saying what the newest build is.
2. **An update download**, if you accept one.
3. **A crash report**, if you choose to send one.
4. **A target photograph**, if you submit one to the research collection.

That is all of it. There is no analytics, no usage reporting, no licence check and no phoning home. Nothing is sent while you are marking a target, and nothing is sent because you opened the program.

## The update check

A GET for a small JSON file. The only thing it says about you is a User-Agent of `GroupLab` and its version number, because a server needs to know which build is asking in order to answer sensibly.

It does not send your machine name, your account, your screen, your Windows version or a unique identifier of any kind. There is nothing to correlate one check with another beyond the fact that somebody on that version asked.

You can turn it off, and then nothing is sent at all.

## A crash report, and only if you send it

When GroupLab crashes it writes a record to a folder on your own machine and shows you where. **Nothing is sent at that moment.** If you choose to send it, one HTTPS POST carries a zip and the version number.

Three things about that are deliberate:

**One attempt, no retry.** A crash reporter that retries in the background eventually sends something you have forgotten agreeing to. If the send fails, the zip stays where it was saved and GroupLab tells you so.

**No authentication.** There is no key, because any secret inside an open source program is public the moment it ships, and pretending otherwise would be theatre.

**No metadata, no paths.** The log carries no file paths and no image metadata. That is a rule with a test behind it rather than an intention.

## Your photographs, and what is stripped

If you submit a target photograph to the research collection, that is an upload you started, to a page you visited, with a consent record written at the time.

**Location is never read.** Not stripped on the way out: never read in the first place, at any point, by any part of GroupLab. A photograph taken on a phone usually carries where it was taken, and a target photograph usually means somewhere you shoot. That is not information this project wants to hold, so the code does not look at it.

## The rule that stops a fifth thing appearing

All four go through one interface. Nothing else in the program is allowed to open a browser, fetch a file, start a process or read the clipboard.

That is not a convention, it is a test. `OneWayOutTests` reads every source file in the repository and fails the build if anything outside that one file starts a process with the shell or launches a URI, and a second guard does the same for the clipboard.

**It exists because of a real accident.** The settings page's link to the repository once opened the real default browser, and an automated test that clicks every control it finds opened browser tabs on the developer's machine. The same class of mistake had already printed to a real printer. Both were fixed the same way: one door, and the tests replace it with a recorder that writes down what would have happened.

That is the part worth trusting more than any promise on this page. A rule a person has to remember is a rule that lasts until somebody is in a hurry. A rule that fails the build is a different kind of thing.

## Reading the clipboard

GroupLab can open an image you paste with Ctrl+V. It reads the clipboard **only** at that moment, never on its own and never in the background, and the same one-door rule covers it: exactly one file in the program may touch a real clipboard, and the build fails the day a second one learns how.
