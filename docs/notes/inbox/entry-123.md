# 2026-09-21, entry 123: the updater's missing half, and the 403 checked rather than assumed

Your report on entries 119 and 122 was clear and the signing chain is proven. Two things follow from it.

## 1. The 403: verify the cause after Alan changes the setting

Alan is changing Settings, Actions, General, Workflow permissions to "Read and write permissions" as you suggested. I am not certain it is the cause, so treat the next nightly as the test of your diagnosis, not as the fix:

1. A workflow-level `permissions: contents: write` normally raises the token above a read-only repository default, and your own log printed `Contents: write`. `release.yml` also created a release successfully under the same read-only default. So the default may not be what refused the POST.
2. After the setting change, on the next green push, check whether "Publish this build" succeeds. If it does, record in `docs/UPDATES.md` that the setting is required and why.
3. If it still returns 403, look further before touching anything else, and report what you find: repository and account rulesets on tags (`gh api repos/oRAirwolf/grouplab/rulesets`, and any rule matching `v*` or `nightly`), tag protection, whether `gh release create --target <sha>` is creating a tag on a commit the token may not tag, and the exact request and response from a run with `GH_DEBUG=api` set on that step. Do not widen any other permission or setting yourself; bring the evidence back.

## 2. Build the updater's mechanism (entry 119 section 4.4 and 4.6)

You were right to say it plainly: the rules exist, the machinery does not. Build it now:

1. **Choose the mechanism** per entry 119 section 4.6 (the Inno Setup installer run silently, or a maintained framework if it meets every requirement and needs no administrator rights), and justify the choice in your report.
2. **Download** the installer named in the verified manifest to a folder GroupLab owns, in the background, with progress shown in the update bar and a way to cancel. Resume or restart cleanly after a dropped connection. Verify SHA-256 against the manifest before anything runs; a mismatch deletes the file and says so.
3. **Install silently**: save everything (the session store and any open work), say in one line that GroupLab will close and reopen to finish updating, start the installer silently through `IOutsideWorld` (so tests use the recorder, entry 122), exit, and have the installer relaunch GroupLab on the screen the user was on. No installer window and no elevation prompt may appear.
4. **After the update**, the first launch of the new version says in one line that it updated from version A to version B, with a link to the notes. If the new version fails to start, the previous install must still be usable; say how that is guaranteed or, if it cannot be with the chosen mechanism, say so and what the user does.
5. **The update check's request** goes through `IOutsideWorld` too, as entry 122 section 1 required, so no test ever touches the network.
6. **Tests without the network or a real installer**, as entry 119 section 9 lists, including: hash mismatch, a truncated download, cancel mid-download, and the save, close, install, reopen sequence driven by the recorder.
7. **The real test, once two nightlies exist** (entry 119 section 10 step 3): install the older nightly on this machine with its installer, open a session in it, let it find the newer one, press Update now, and report exactly what you saw at each step, including whether any window or prompt appeared, how long it took, and that the session survived. If only one nightly exists, push a second small commit to make one.

## 3. Report

The 403 outcome from section 1, the mechanism chosen and why, the section 2.7 walkthrough, and the run URLs and versions.
