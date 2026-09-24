---
title: Cutting the installer from 97 MB to 81 MB
description: A third of what GroupLab shipped was debug symbols for two libraries that nothing would ever read. Finding it was an accident, and the accident is the point.
group: How GroupLab is built
number: 28
written: 2026-09-22
data_date: 2026-09-22
samples: one Windows build, measured before and after, with the analysis re-run on a 25 shot sheet to prove nothing changed
state: published
no_figure: "The finding is a table of file sizes and it is already in the article. A chart of two bars would add nothing a reader cannot see in the numbers."
found: 100.7 MB of a 332.6 MB install was debug symbols, 100 MB of it in two files. Removing them took the install to 204.3 MB, a 39 percent cut, and made the self-contained build smaller than the framework-dependent one had been.
sure: One build on one machine, measured directly rather than estimated, and checked by re-running the analysis afterwards.
sources:
  - "The measurement and the check that nothing changed: `docs/PHASE1-RESULTS.md`, \"Entry 132 section 2, and the largest single win of the night\"."
  - "What was being measured when it turned up: the same document, \"Entry 133, the light installer, measured\"."
---

## It was found while measuring something else

The question on the table was whether GroupLab should offer a smaller installer that downloads the .NET runtime separately instead of carrying it. That is a real question with real trade-offs, and answering it needed the sizes of both kinds of build.

So both were built and measured. And in the middle of the table was something nobody had asked about:

**100.7 MB of the 332.6 MB install was debug symbols.**

100 MB of that was two files: `libSkiaSharp.pdb` at 80.1 MB and `libHarfBuzzSharp.pdb` at 19.9 MB. Native debugging symbols for the graphics and text-shaping libraries.

## Why they were useless

Nothing at runtime reads them. No crash report GroupLab writes can use them, because it records managed stack traces and these are native symbols. And no person installing a target-measuring program is ever going to attach a debugger to Skia.

They were in the package because they come in the package. Nobody put them there on purpose, and nobody had looked.

## What removing them did

| | |
|---|---|
| installed before | 332.6 MB |
| installed after | **204.3 MB** |
| saved | 128.3 MB, a 39 percent cut |

The installer went from 97.3 MB to about 81 MB.

There is a second result worth more than the first. The whole point of a framework-dependent build is that it is smaller: it leaves out the .NET runtime and downloads it. That build measured **227.9 MB** unpacked.

**The self-contained build, with the symbols removed, is 204.3 MB.** It is now smaller than the "small" alternative, while still carrying its own runtime. The question that started the measurement answered itself in the process, and the answer was: do not build a second kind of installer, just stop shipping 100 MB of files nobody reads.

## What was kept

GroupLab's own debug symbols stay. They are 0.4 MB, and a crash record that names a line in GroupLab's own source is worth having. The distinction is not "symbols are waste", it is that symbols for code you did not write and cannot debug are waste.

## Proved, not assumed

A size cut is exactly the kind of change that quietly breaks something, and "it still starts" is not a check.

The trimmed build was made to validate a target definition and analyze the standard 25 shot sample. It read 25 holes on 25 bulls with a mean radius of 0.232 in, which is what it read before, to the last digit.

## The general point

The specific finding is worth 128 MB to anyone downloading GroupLab. The general one is worth more.

**Nobody was looking for this.** It was found because a different question forced somebody to list what was actually in the package, file by file, rather than reasoning about what ought to be in it. The reasoning would never have found it: the runtime *is* large, the graphics libraries *are* large, and a 332 MB install for a program with a bundled runtime is entirely plausible. Every step of that argument is correct and the conclusion was wrong by a third.

That is what measuring is for, and it is why this project tries to measure things that seem obvious. The times it has paid off have almost all been like this one: not a surprise about the thing being measured, but something else entirely, sitting in plain sight in the numbers nobody had written down before.

## What this means


**For anybody downloading GroupLab:** the install is 204 MB rather than 333 MB, and there is no second, smaller installer to choose between, because the ordinary one is now smaller than the alternative would have been. If you were waiting for a light build, it exists and it is the one you already have.

**For anybody building software:** the useful part is not the 128 MB. It is that the reasoning was sound at every step and the conclusion was wrong by a third. A bundled runtime is large, graphics libraries are large, and 333 MB was entirely plausible. Nothing short of listing the files would have found it, and nobody lists the files unless something makes them.

So the thing to stop believing is that a plausible number has been checked. It has not. It has been accepted.
