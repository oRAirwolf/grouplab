# 2026-09-24, entry 179: 18 GB of scratch files in Alan's temp folder, and they must clean themselves up

Alan noticed his temp folder had grown very large and measured it:

    %LOCALAPPDATA%\Temp\claude\c--Dev-grouplab    17.96 GB
    loose files directly in %TEMP%                1.39 GB

`c--Dev-grouplab` is this repository's Claude Code scratch area, one folder per session. The planning
session writes nothing there; everything in it is this session's, or earlier Claude Code sessions' on this
repository.

**Alan wants Claude Code to do the cleanup itself**, deleting only what is no longer in use, and to keep it
clean from now on. He will not delete anything by hand in case something is still needed. Do this entry
right after entry 178.

## 1. Find out what filled it

Before changing anything, report the ten largest files and the five largest folders under
`c--Dev-grouplab`, by name and size only, and say what wrote each: rendered PDFs, rasterized sheets,
600 dpi test scans, the scratch profiling files (`ScratchFreezeProfile`, `ScratchCepProfile`), screenshot
renders, downloaded release assets, or anything else. Report the same for any files the test suite leaves
directly in `%TEMP%`.

## 1.1 Clean up now, carefully

1. **The current session's folder:** you know what in it is still in use. Delete every file that has served
   its purpose, and keep only what a queued entry still needs. The entry 173 test image, for example, is no
   longer needed now that entry 177 has recorded the test.
2. **Earlier sessions' folders:** those sessions have ended, and only the current session is running. Delete
   a folder only when its newest file is more than 24 hours old, so nothing touched recently is removed.
3. **The empty random named folders in `%TEMP%`** of section 4: delete the ones your test runs created, and
   only those. If a folder cannot be attributed to this suite, leave it.
4. **One command, shown before it runs.** List what will be deleted and its total size in the panel, then
   run the deletion as a single command, so Alan approves once, knowing what it removes.
5. Report the size of `%LOCALAPPDATA%\Temp\claude\c--Dev-grouplab` before and after.

## 2. Make every temporary file clean itself up

1. **Tests.** Every test that writes a file writes it inside one directory created for that test run, and
   the run deletes that directory when it finishes, pass or fail. No test writes straight into `%TEMP%` or
   the scratch area.
2. **A test that proves it**: run the suite, then assert that no file it created remains anywhere under
   `%TEMP%`. A test suite that leaks files is broken in the same way a test that leaks memory is.
3. **Scratch profiling tests** (`ScratchFreezeProfile.cs`, `ScratchCepProfile.cs` and any like them) are
   either turned into real tests with budgets, per entry 170, or deleted once they have answered their
   question. They must not stay in the suite writing large output on every run.
4. **Your own scratch work.** When a file in the session scratchpad has served its purpose, delete it.
   Large intermediates, a 56 MB scan copy, a rendered sheet, belong there only while they are in use.
5. At the start of each run, delete scratch folders of earlier Claude Code sessions on this repository
   whose newest file is more than seven days old. Never touch the current session's folder, and never touch
   anything outside `%LOCALAPPDATA%\Temp\claude\c--Dev-grouplab`.

## 3. Write it down

Add the rule to `CLAUDE.md`: temporary files are created in one place, deleted when finished, and never
allowed to accumulate. Record in `docs/PERFORMANCE.md` how large the scratch area was before this entry
and how large it is after a full test run once the fixes are in.

## 4. A second sign of the same leak (added 2026-09-24)

Alan's full listing of `%TEMP%` shows `claude` at 18.05 GB and everything else small (Diagnostics 0.41,
VS Code 0.22, Roslyn 0.15), **plus a long run of empty folders with random names** such as `qzdijs2r.12j`,
`qzg1izkn.n53`, `r03td1hq.xjg`. That eight dot three pattern is what `Path.GetRandomFileName` produces,
which is the usual way a test makes a temporary directory. They are almost certainly this suite's test runs
creating directories and never deleting them. Confirm which tests create them, and include them in section
2's cleanup and in the test that fails on leaks.
