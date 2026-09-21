# 2026-09-21, entry 119: a test build that publishes itself after every green push

Alan asked whether new versions reach the Releases page on their own. Today they do not. `release.yml` runs only on a pushed `v*` tag or a manual run from the Actions tab (which makes a draft). Ordinary pushes build and test in `ci.yml` and publish nothing, so the README download links stay on whatever was last tagged, and until the first tag exists they return 404. Alan wants testers to get the newest build without anyone remembering to release it. This entry adds that, and keeps numbered versions as a deliberate act.

## 1. A rolling test build, published by the workflow

Add a workflow, `test-build.yml`, that publishes a GitHub pre-release under one fixed tag, `test-build`, every time CI passes on the working branch.

1. Trigger it with `workflow_run` on the `ci` workflow, `types: [completed]`, limited to the branches `phase-1` and `main`, and run the jobs only when `github.event.workflow_run.conclusion == 'success'`. A push that fails CI on any of the three operating systems publishes nothing. Pull requests and other branches publish nothing.
2. Check out and build exactly `github.event.workflow_run.head_sha`, never the branch head, so the published build is the commit that passed.
3. Build the same packages `release.yml` builds, by the same steps: the Windows installer, the Windows zip, the Linux tarball. Do not copy the steps by hand into a second file. Move them into a reusable workflow (`workflow_call`) or a composite action that both `release.yml` and `test-build.yml` call, so the two can never drift.
4. Name the assets the same two ways as the release: once with the version and the short commit (`grouplab-setup-0.1.0-win-x64-<sha>.exe` and the rest), once under the stable names (`grouplab-setup-win-x64.exe`, `grouplab-win-x64.zip`, `grouplab-linux-x64.tar.gz`).
5. Publish to the release whose tag is `test-build`: move the tag to the tested commit, delete the previous assets, upload the new ones, and set it as a pre-release (`prerelease: true`, `make_latest: false`). Title it `GroupLab test build 0.1.0-<sha>` and put the commit, the date, and the first line of each commit message since the previous test build in the body, followed by one line saying this build is untested by hand and may be broken.
6. Add `concurrency: { group: test-build, cancel-in-progress: true }` so two quick pushes cannot interleave uploads, and the newest one wins.
7. Because it is a pre-release, GitHub's `releases/latest` keeps pointing at the newest numbered version. The test build is reached by its own fixed address, `https://github.com/oRAirwolf/grouplab/releases/download/test-build/<stable name>`, which never changes.
8. Nothing new goes into the packages. The rule from entry 116 stands: samples only from already-public images with a consent record, and the friend's earlier scan never.

## 2. The README Download section shows both

Split the Download section into two tables:

1. **Test build**, first, with one sentence saying it is rebuilt automatically after every change that passes the tests and is the one to use for testing and bug reports. Links use `releases/download/test-build/<stable name>`.
2. **Latest release**, the numbered version, with the existing `releases/latest/download/<stable name>` links.

Extend `ReleaseAssetTests` so it holds both sets of links to the stable names the workflows produce, and keep the table of contents from entry 118 in step with the headings (its test will tell you if not).

## 3. Numbered releases stay deliberate

Do not push a `v*` tag on your own, ever. A numbered release happens only when Alan asks for one in a command, naming the version. When he does: set `<Version>` in `Directory.Build.props`, commit, push, wait for CI to pass, then push the tag `v<version>`. The manual draft path from the Actions tab stays as it is.

## 4. The build names itself

Make sure the running application shows its version and short commit where a tester can read it and copy it into a bug report (the About screen or the status bar, whichever already exists), so a report from a test build names the exact commit. If this is already there, say so and move on.

## 5. Prove it

1. After pushing, confirm with the GitHub API that the `ci` run on that commit passed, that the `test-build` run followed it and succeeded, and that the `test-build` release exists, is marked pre-release, points at that commit, and carries all six assets.
2. Download `https://github.com/oRAirwolf/grouplab/releases/download/test-build/grouplab-setup-win-x64.exe` over HTTP (no browser, no install), check it is a Windows executable of plausible size, and report its SHA-256 beside the SHA-256 of the versioned copy in the same release. They must match.
3. Confirm `releases/latest` did not move to the test build.
4. Report the run URLs and the hashes in your summary.
