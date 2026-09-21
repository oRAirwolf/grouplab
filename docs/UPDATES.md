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

## What a tester will see, honestly

- **SmartScreen.** No code signing certificate exists, so Windows may warn on a freshly downloaded installer: "Windows protected your PC", then More info and Run anyway. Nothing here works around that, and nothing should: the fix is a signed build, which costs money the project has not spent.
- **A nightly may be broken.** Passing the tests is not the same as somebody having used it.
- **An update is a new build, not a patch.** The whole application is replaced.

## Where your things are

Updating never touches `%APPDATA%\GroupLab`: your settings, your sessions database, your own sheets and the log stay where they are, before and after. Removing GroupLab leaves that folder too; delete it when you want the data gone.
