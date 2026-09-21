# How GroupLab updates itself

`NOTES-FROM-PLANNING.md` entries 119 and 121. This page is the whole of it: what a build is, what an update check sends, how a build is signed, what happens when something does not verify, and how the key is rotated.

## Trains

A **train** is a stream of builds you can follow.

| Train | What is on it | Today |
|---|---|---|
| **Nightly** | Every commit that passes the tests on Windows, Linux and macOS | The only train with builds on it |
| **Beta** | Builds somebody has looked at | Not available yet |
| **Release** | A numbered version somebody chose to release | Not available yet |

A version says which train it came from: `0.2.0-nightly.14` is a nightly, `0.2.0-beta.1` a beta, `0.2.0` a release. A build made on somebody's own machine is a **development build**: it has no train, says so on the settings page, and never offers to update itself.

**A less steady train takes from a steadier one, never the other way.** Someone on nightly is offered a newer beta or release; someone on release is never offered a nightly. Moving to a steadier train never downgrades you: it waits until that train passes what you have, and says so.

**The ordering is not quite SemVer, deliberately.** SemVer compares pre-release words as text, which would put `beta` below `nightly` because "b" sorts before "n". GroupLab ranks the trains by how steady they are instead, so a beta of a version reads as newer than a nightly of it. `UpdateOrder` is where that lives and says why; `SemanticVersion` stays strictly SemVer because it also reads tags. Question 30 in `QUESTIONS-FOR-PLANNING.md` asks the planning session to confirm it.

## What a check sends

**Nothing about you, and nothing about your shooting.** A check is one plain HTTPS GET for one small file at a fixed address:

```
https://github.com/oRAirwolf/grouplab/releases/download/nightly/update-manifest.json
```

The request carries a User-Agent naming GroupLab and its version, and nothing else: no account, no identifier, no machine name, no count of sessions, no targets. GitHub sees that some copy of GroupLab of some version asked for a public file, as any browser would. Because the address is fixed and public, the check never touches GitHub's API and cannot run into its rate limit.

Offline, or GitHub unreachable, means **no message at all**, only a line in the diagnostic log. A program that nags about the network when you are at the range is a program you turn off.

## What a build is signed with

Each build publishes `update-manifest.json`: its version, its train, its commit, when it was published, the release notes, and for every file its name, its size and its SHA-256. The manifest is signed, and **the application refuses a manifest whose signature does not verify, and any download whose SHA-256 does not match the manifest**, saying so in plain words rather than failing quietly.

**The algorithm is ECDSA over the P-256 curve with SHA-256.** Entry 119 asked for Ed25519. .NET 10 has no Ed25519 of its own, and this project cannot add a package for one: the machine GroupLab is built on has no NuGet source configured, so a restore uses only what is already in its cache. P-256 is in the box on every platform GroupLab builds for, and OpenSSL on the build runner signs it with one command. Every signed manifest names its algorithm, so moving to Ed25519 later is a manifest the application can tell apart rather than a silent change. Question 31 records it.

### The key

- **The private half lives only as a GitHub Actions secret**, named `GROUPLAB_UPDATE_SIGNING_KEY`. It is never in the repository, never in a file in a working copy, and never in a message.
- **The public half is compiled into the application**, in `src/GroupLab.Core/Updates/UpdateKeys.cs`.
- **A build with no public key installs nothing.** It says "This build carries no update key, so it cannot tell a real update from a forged one and will not install any", and points at the download page. That is the safe way round.
- **The workflow fails loudly rather than publishing unsigned.** The nightly checks for the secret before it builds anything, and stops with the reason in the run summary when it is missing.

### Making the key, once

```
grouplab update-key
```

prints both halves and what to do with each. Then:

1. `gh secret set GROUPLAB_UPDATE_SIGNING_KEY` in the repository, and paste the private half when it asks.
2. Paste the public half into `UpdateKeys.PublicKey` and commit it.

The private half is a base64 PKCS#8 key on one line. Anyone who has it can sign an update that GroupLab will install, so it is treated as the password it is.

### Rotating the key

Rotation is the same three steps with one addition, and it is not free: **every build already installed trusts the old key only**, so a build signed with a new key is refused by everything older than the change.

1. Make a new pair with `grouplab update-key`.
2. Set the secret to the new private half.
3. Commit the new public half.
4. **Tell people.** Builds older than step 3 will say the signature does not match and will stop offering updates; those copies have to be downloaded again by hand. The settings page's refusal names the reason so a person can act on it.

Rotate when the private half may have been seen by anybody else, and at no other time.

## What the updater does

1. **It checks on every launch by default**, in the background after the window is up. It never slows or blocks starting.
2. **How often is yours to choose:** on every launch, once a day, once a week, or never. "Check now" always works.
3. **A newer build shows as a bar in the window**, not a dialog, naming the version and showing its notes. Three choices: **Update now**, **Later**, **Skip this version**. Later asks again at the next check. Skip stays quiet until something newer than the skipped build appears.
4. **Update now is silent.** GroupLab downloads the file, checks its SHA-256 against the manifest, verifies the manifest's signature, and installs with no installer windows and no administrator prompt, because the installer is a per-user one. If it has to close to finish, it **saves everything first**, says in one line that it will close and reopen, and comes back on the screen you were on.
5. **The zip and the Linux tarball cannot replace themselves.** They check and notify in the same way, and Update now opens the download page instead.

## The mechanism, and why it is this one

**GroupLab updates itself by running its own Inno Setup installer silently.** It downloads the installer the signed manifest names, checks its SHA-256, saves your work, starts the installer with `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /relaunch=yes`, closes, and the installer puts the new build in place and starts it again.

The alternative was a maintained updater framework, and it was not taken for one plain reason: **this repository has no NuGet source configured**, so no package can be added to it at all. That is a fact about the machine and the build, not a judgement about the frameworks. Against that:

- The installer already exists, is built by the same workflow on every commit, and is the file a tester downloads by hand anyway. One artefact, one code path, one thing to keep working.
- It is a per-user install (`PrivilegesRequired=lowest`), so nothing asks for administrator rights, which was a requirement rather than a preference.
- Its silent switches are documented and stable, and `/relaunch=yes` is read by a five-line `[Code]` function in `packaging/windows/grouplab.iss`, so GroupLab comes back up rather than leaving the person staring at a closed window.
- Nothing new has to be trusted. A framework would be one more thing between a signed manifest and the files on disk.

What it costs is written below under "If a new build will not start".

**Only Windows can do this.** The zip and the Linux tarball were unpacked wherever their owner chose, and GroupLab does not write over a folder it did not make. On those platforms the bar says a newer build exists and points at the download, and the person installs it the way they installed the first one.

### If a new build will not start

**This cannot be guaranteed, and pretending otherwise would be worse than saying so.** The installer replaces the program folder in place; the previous build's files are gone once it has run. There is no second copy kept aside and no automatic roll back.

What is true, and what you do:

1. **Your work is never at risk.** Everything you care about lives in `%APPDATA%\GroupLab`: the settings, the sessions database, your own sheets and the log. No installer and no uninstaller touches that folder. A bad build cannot lose a session.
2. **Every nightly keeps its own release.** The workflow publishes `v<version>` alongside the rolling `nightly` tag and keeps the newest thirty. So the build you were on yesterday still exists at its own address.
3. **To go back:** download the previous build's installer from its own release page and run it. It installs over the broken one, and your things are exactly where you left them.

**When this changes.** Keeping the previous install beside the new one, with a "Roll back to <version>" shortcut, is built at the first of these two things happening, and not before:

1. the beta train opening, or
2. Alan saying that a second person is testing GroupLab.

Until then the three steps above are the answer, because the whole argument for keeping a second copy is that somebody who cannot diagnose a broken build is left stuck, and today there is nobody in that position. Proving the new build starts before the old one is removed, which is what the updater frameworks do, is **not** to be built: it is the right answer for a product with a support queue and the wrong one here. This was question 33, answered by entry 124 section 2.

## What a tester will see, honestly

- **SmartScreen.** No code signing certificate exists, so Windows may warn on a freshly downloaded installer: "Windows protected your PC", then More info and Run anyway. Nothing here works around that, and nothing should: the fix is a signed build, which costs money the project has not spent.
- **A nightly may be broken.** Passing the tests is not the same as somebody having used it.
- **An update is a new build, not a patch.** The whole application is replaced.

## What the workflow needs from the repository

The nightly publishes with `GITHUB_TOKEN`, and `nightly.yml` asks for `permissions: contents: write`. That is not sufficient on its own. **Settings, Actions, General, Workflow permissions must be set to "Read and write permissions".**

This was established rather than assumed. With the repository default left at read-only, the publish step failed with `HTTP 403: Resource not accessible by integration` on `POST /repos/oRAirwolf/grouplab/releases`, although the job log printed `Contents: write` in its own token summary. After the setting was changed and nothing else, the same workflow on the same commit published `v0.2.0-nightly.12` and moved the rolling `nightly` release without any other change:

- refused, default read-only: <https://github.com/oRAirwolf/grouplab/actions/runs/35572294871>
- published, default read and write: <https://github.com/oRAirwolf/grouplab/actions/runs/35573031594>

The reason the workflow-level `permissions:` block is not enough is that it can only narrow what the repository default already allows; it cannot raise it. A workflow asking for more than the default gets the default, and the log prints what was asked for rather than what was granted, which is what made the first diagnosis a guess until it was tested.

## Where your things are

Updating never touches `%APPDATA%\GroupLab`: your settings, your sessions database, your own sheets and the log stay where they are, before and after. Removing GroupLab leaves that folder too; delete it when you want the data gone.
