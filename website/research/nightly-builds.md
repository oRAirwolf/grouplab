---
title: Nightly builds, from commit to installer
description: Every change that passes its tests becomes an installer you can download within about half an hour. Here is what happens in between, and the three mistakes that shaped it.
group: How GroupLab is built
number: 27
written: 2026-09-22
data_date: 2026-09-22
samples: not a measurement: an account of the build pipeline and the failures that shaped it
status: published
found: The hard parts are not building. They are making sure a build happens on a quiet day, that two pushes do not race, and that the release notes say something a shooter can use.
sure: This describes the pipeline as it stands. The numbers in it, such as the run times, are from ordinary runs and are not benchmarks.
sources:
  - "The workflow itself, with its reasoning in comments: `.github/workflows/nightly.yml`."
  - "The release-note rule and why it exists: `CLAUDE.md`, \"Release notes: say what changed, to a person who shoots\"."
---

## The shape of it

A change is pushed. Tests run on Windows, macOS and Linux. If they all pass, a nightly build starts: it packages a Windows installer, signs a manifest saying what is in it, tags the commit, and publishes a release.

From push to downloadable installer is about half an hour, most of it tests.

That is the easy description. The interesting parts are the three things that went wrong.

## Mistake one: nothing was built on a quiet day

The build was triggered by a successful test run, which only happens when somebody pushes.

So a day with no pushes produced no build. So did a day whose runs were all cancelled, which happens more than you would think. Anybody following the nightly train would see it simply stop, with no explanation, because nothing had failed.

There is now a **scheduled run** as well, once a day. It finds the newest commit on main that passed its tests, and builds that. If that commit already has a nightly it does nothing and says so, so a quiet day produces one build or none rather than a duplicate.

The general shape of the bug: *a trigger that depends on activity is silent about inactivity.* Anything that is supposed to happen regularly needs a clock somewhere, not only an event.

## Mistake two: half the builds were cancelling each other

Pushes went to two branches at once, at the same commit. The test workflow ran twice on identical code, each run triggered a nightly, and a rule that keeps only the newest nightly cancelled the first.

The logs looked alarming: on one day, half the nightly runs were cancellations. Nothing was broken and nothing was lost, because the run being cancelled was building the same commit as the one replacing it. But it was impossible to tell that from the outside, and a log full of cancellations is a log nobody reads carefully.

Fixed by triggering on one branch only. The rule that keeps the newest push stays, because two quick pushes genuinely should not interleave.

## Mistake three, and the one that mattered to people

For a while the release notes were generated from commit subject lines. That produced entries like:

> Entry 130 item 3.3: doubt travels with the number

That is written for the project's own log. Somebody deciding whether to install a build cannot use it at all.

Notes are now written **by hand, as part of making the change**, in a trailer on the commit:

```
Release-note: When GroupLab finds fewer holes than the shots you fired, it now
says so and lists the bulls with nothing on them, instead of reporting a clean
result.
Release-note-kind: fixed
```

Nothing is guessed from a subject line. A commit with nothing a person would notice, a test, a document, a build change, carries no trailer and appears only in a count at the end.

**And the generator refuses bad notes**, failing the build rather than publishing them. A note is rejected if it is only a reference, begins with "Entry", is shorter than eight words, or uses words that mean nothing to a shooter: folded, gate record, harness, fixture, refactor, and a list of others. That was tested against seven notes, six deliberately bad, and it refused all six for the right reason.

## The rule that is easiest to get wrong

A release note describes **what somebody can do after installing the build**, not what is in the repository.

One nightly told people GroupLab "now works out where your group actually landed before deciding which bull each shot belongs to". The code to do it existed. It was wired to nothing, and no screen could reach it. The sentence was untrue on the day it was published and nobody reading it could have known.

So: if the working part cannot be reached from any screen, the note says so in the same breath or there is no note. And a published release is never edited afterwards to cover it up; the correction goes in the next one, where the people who read the wrong one will see it.

## What each build carries

- The installer, per-user, needing no administrator rights.
- A manifest listing every file with its size and SHA-256, signed, so the updater can refuse a tampered download.
- The release notes for that version only, meaning the changes since the previous **published** build. A cancelled or skipped nightly never got a tag, so its changes roll into the next real one rather than vanishing.
- The notes of the last several versions as well, so somebody who skipped five builds can be shown everything they missed rather than only the newest.
