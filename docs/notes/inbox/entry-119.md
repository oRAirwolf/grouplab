# 2026-09-21, entry 119: nightly builds that publish themselves, an updater with three trains, and one download link

This entry replaces an earlier draft of entry 119 that was never sent. Alan asked two things. First, whether new versions reach the Releases page on their own. Today they do not: `release.yml` runs only on a pushed `v*` tag or a manual run (which makes a draft), so the README's download links stay on whatever was last tagged, and until the first tag exists they return 404. Second, he wants GroupLab to update itself. His requirements, in his words where it matters, are in section 4 and they are not to be traded away.

Terms used throughout. A **train** is a stream of builds a user can follow: **release**, **beta**, **nightly**. For now only nightly exists. Release and beta are built into the updater and the settings page but shown greyed out, with the words "Not available yet", until Alan asks for the first beta or release.

## 1. Versions

1. Every build has a unique, ordered SemVer version. Nightly: `<Version>-nightly.<N>`, where `<Version>` is the value in `Directory.Build.props` (0.1.0 today) and `<N>` is a build number that only ever increases (the GitHub run number of the nightly workflow is acceptable; say what you used). Beta, later: `<Version>-beta.<N>`. Release, later: `<Version>`.
2. The running application knows its full version, its train and its short commit, all stamped in at build time. A local developer build says so ("development build") and never offers to update itself.
3. Ordering follows SemVer precedence, so `0.1.0-nightly.12 < 0.1.0-beta.1 < 0.1.0 < 0.2.0-nightly.1`. Write that ordering as tests.

## 2. A nightly build after every green push

Add a workflow, `nightly.yml`:

1. Trigger it with `workflow_run` on the `ci` workflow, `types: [completed]`, limited to the branches `phase-1` and `main`, and run only when `github.event.workflow_run.conclusion == 'success'`. A push that fails CI on any of the three operating systems publishes nothing. Pull requests and other branches publish nothing. Add `concurrency: { group: nightly, cancel-in-progress: true }`.
2. Check out and build exactly `github.event.workflow_run.head_sha`, never the branch head.
3. Build the same packages `release.yml` builds, by the same steps. Move those steps into a reusable workflow (`workflow_call`) or composite action that both `release.yml` and `nightly.yml` call, so the two cannot drift.
4. Publish a GitHub pre-release per build, tag `v<version>` (for example `v0.1.0-nightly.14`), `prerelease: true`, `make_latest: false`, carrying the versioned assets, the update manifest from section 3, and release notes from section 5 as its body. Keep the newest 30 nightly releases and delete older nightly releases together with their tags; never delete a beta or release, and never delete anything not created by this workflow.
5. Also maintain one rolling pre-release with the fixed tag `nightly`, moved to the newest nightly commit each time, carrying the stable asset names (`grouplab-setup-win-x64.exe`, `grouplab-win-x64.zip`, `grouplab-linux-x64.tar.gz`) and the same notes. Its download addresses never change: `https://github.com/oRAirwolf/grouplab/releases/download/nightly/<stable name>`. This is what the README links to.
6. Nothing new goes into the packages beyond what entry 120 section 9 allows (scan 3 as the sample, with its consent record). The friend's earlier scan never.

## 3. The update manifest

1. Each build publishes `update-manifest.json` as a release asset: version, train, commit, publish time, and for each platform the asset file name, its size and SHA-256, and the release notes as Markdown.
2. The manifest is signed. Generate an Ed25519 key pair; the private key lives only as a GitHub Actions secret (tell Alan the exact secret name and the exact steps to create it, since you cannot set secrets yourself; the workflow must fail loudly, not publish unsigned, when the secret is missing). The public key is compiled into the application. The application refuses any manifest whose signature does not verify, and any download whose SHA-256 does not match the manifest, and says so in plain words. Write down in `docs/UPDATES.md` how the key is rotated.
3. The application finds the newest build on each train through one fixed address per train that GitHub serves without the API (for nightly: the `nightly` rolling release's `update-manifest.json`), so a check costs one small HTTPS request and cannot run into the API's unauthenticated rate limit.
4. An update check sends nothing about the user or their data. It is a plain GET with a User-Agent naming GroupLab and its version, nothing else. Say this in `docs/UPDATES.md` and in the settings page's help text.

## 4. The updater in the application

Alan's requirements:

1. **Checks by default on every launch**, and notifies the user when a newer version exists on their train. The check runs in the background after the main window is up and never slows or blocks startup. Offline, or GitHub unreachable, means no message at all, only a line in the diagnostic log.
2. **How often it checks is configurable**: On every launch (default), Once a day, Once a week, Never (manual only). Plus a "Check now" button that always works.
3. **The notification** is a non-modal bar or toast in the main window, not a dialog, naming the new version and showing its release notes (collapsed to a few lines with "Show all"). Three choices: **Update now**, **Later**, **Skip this version**. "Later" asks again at the next check; "Skip" stays silent until a newer version than the skipped one appears.
4. **If the user chooses to update, it happens silently in the background**: download, verify signature and hash, install with no installer windows or prompts. If installing requires GroupLab to close, GroupLab first saves everything it holds (the session store and any open work), tells the user in one line that it will close and reopen to finish updating, closes, lets the installer run silently, and reopens on the screen the user was on. Nothing the user entered is ever lost to an update; write a test that proves an open session survives it.
5. **Trains**: a choice of Release, Beta, Nightly on the settings page. Release and Beta are greyed out with "Not available yet"; Nightly is selected and is the only choice for now. When the other trains exist, a user on a less stable train also receives newer builds from a more stable one (nightly users get a beta or release if it is newer), never the reverse. Moving to a more stable train never downgrades automatically; it waits for that train to pass the installed version, and says so.
6. **Mechanism**: the installer is per user with no administrator rights (entry 116), so no elevation prompt may ever appear. Choose the mechanism and justify it in your report: either the Inno Setup installer run with its silent switches (`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS` and a relaunch), or a maintained .NET update framework such as Velopack if it meets every requirement here, its licence is compatible with GPL-3.0, and it does not require administrator rights. Do not write your own installer. If you switch installers, the stable asset names and the README link must keep working.
7. **The zip and the Linux tarball** cannot update themselves: they check and notify the same way, and "Update now" opens the download page for that train instead. Say so on the settings page for those builds.
8. **No code signing certificate exists.** Windows SmartScreen may warn on a freshly downloaded installer. Do not try to work around SmartScreen; record the limitation in `docs/UPDATES.md` and in the testing guide, with what a tester will see.

## 5. Release notes for every build, automatically

1. The workflow writes the notes for each build from the commits since the previous build on the same train: the first line of each commit message, grouped under plain headings (new, fixed, changed, other) by a rule you state, skipping merge commits and commits that only fold planning notes. Attribution trailers and session links are removed.
2. The notes go into the GitHub release body, into `update-manifest.json`, and are what the in-application notification shows.
3. Before publishing, the workflow checks the notes against the same rules as the public repository: no em dash characters (replace with a comma or colon), nothing from the private range folder or any submission, no GPS coordinates, no server address. A failed check fails the build rather than publishing.

## 6. The settings page

1. **About**: the full version (for example `0.1.0-nightly.14`), the train, the short commit, and a link to `https://github.com/oRAirwolf/grouplab` that opens in the browser. The version text is selectable so a tester can copy it into a bug report.
2. **Updates**: the train choice (section 4.5), how often to check (4.2), Check now, the time of the last check and its result, and the one-sentence privacy note (3.4).
3. **Support**: the placeholder from entry 120 section 9.
4. Render the settings page at 1280 by 720 and 2560 by 1440 into `docs/figures/screens/current/` and look at both yourself.

## 7. The README: one download link

Replace the Download section with a single table for the latest nightly build: the installer, the zip and the Linux tarball, all linking to `releases/download/nightly/<stable name>`, with one sentence saying it is rebuilt automatically after every change that passes the tests, may be broken, and updates itself (the installer build). No release table, no mention of `releases/latest`, until Alan asks for the first full release; at that point the README gets a Release table and a Latest build table, and not before. Update `ReleaseAssetTests` to hold exactly this, and keep the entry 118 contents list in step.

## 8. Tagged releases stay deliberate

Do not push a `v*` tag other than the nightly tags the workflow creates, ever. A beta or release happens only when Alan asks for one in a command, naming the version. The manual draft path from the Actions tab may stay for testing, but its drafts must never become the target of any README link. The draft "GroupLab 0.1.0, draft" made by hand on 2026-09-21 is superseded by the nightly train; leave it for Alan to delete.

## 9. Tests without the network

The updater's logic is tested without touching GitHub: version ordering, train rules (section 4.5), check-interval rules, skip and later, signature rejection, hash rejection, a truncated download, offline, a manifest from the wrong train, a development build never offering to update, and the save, close, install, reopen sequence driven by a fake installer. The real end-to-end check is section 10.

## 10. Prove it

1. After pushing, confirm with the GitHub API that CI passed on the commit, that `nightly.yml` followed and succeeded, and that both the per-build pre-release and the rolling `nightly` release exist, are pre-releases, point at that commit, carry every asset and a manifest whose signature verifies with the public key compiled into the application.
2. Download `https://github.com/oRAirwolf/grouplab/releases/download/nightly/grouplab-setup-win-x64.exe` over HTTP (no browser), check its SHA-256 against the manifest and against the versioned copy.
3. On this machine: install the previous nightly with the installer, run it, let it find the newer nightly, choose Update now, and confirm it updates silently, closes, reopens on the same screen, reports the new version on the settings page, and kept an open session intact. If only one nightly exists yet, push a second trivial commit to get two, and say so.
4. Confirm the README has exactly one download table and every link in it resolves.
5. Report the run URLs, versions, hashes and what you saw in step 3.
6. If the Ed25519 secret is not yet set, stop after building the workflow and the application side, report the exact steps for Alan to create the secret, and leave publishing to the next run.
