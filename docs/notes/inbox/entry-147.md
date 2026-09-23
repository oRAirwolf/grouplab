# Entry 147: macOS test builds, and a plain statement of what is supported

Written by the planning session at 02:10 Mountain on 2026-09-23. Alan's decision, in his words below. Do this after entry 129.

Today every build is tested on macOS, because `build and test` runs on `macos-latest` as well as Windows and Linux, and no macOS download exists. That is a reasonable state and a confusing one to a reader, because nothing on the site says either half of it. This entry publishes a macOS build, marks it honestly, and says plainly what will and will not happen.

## 1. Build and publish a macOS test build

1. Add macOS to the packaging alongside the Linux tarball: `osx-arm64` for Apple silicon and `osx-x64` for Intel Macs, self-contained, two separate downloads rather than a universal binary.
2. Package each as a proper `.app` bundle inside a `.tar.gz` or `.zip`, with `Info.plist`, the GroupLab icon, and a name that reads correctly in Finder. A bare executable runs from a terminal and behaves like a stranger in the dock, which is not worth publishing.
3. Publish both as nightly assets beside the Windows and Linux ones, named so the architecture is obvious, for example `grouplab-macos-arm64.tar.gz` and `grouplab-macos-x64.tar.gz`.
4. Build them on the `macos-latest` runner, which is already in the matrix, so the packaging is done by the platform it targets.
5. **The updater does not offer these builds.** The silent install and relaunch chain is the Windows installer, and a macOS build must not be offered an update it cannot apply. Check what the update path does on macOS and make it say plainly that updates are manual there.
6. **Label them untested everywhere they appear**: on the GitHub release, on the download page, in the file name if you can do it without making the name silly. Nobody has run this on a Mac.

## 2. The terminal command, and why it is needed

An unsigned application downloaded from the internet is quarantined by macOS, and Gatekeeper refuses to open it. Give the exact command on the download page and in the README, with a sentence saying what it does:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Say that this removes the quarantine flag macOS puts on downloaded files, that it is the standard way to run unsigned software, and that a reader who is not comfortable doing that should not run the build. Adjust the path in the instructions to wherever the pages tell people to put the app.

## 3. The platform statement, word for word

Alan has settled this wording. Publish it as its own section on the download page, titled "What is supported, and what is not", linked from the README, and do not reword it. It avoids the first and second person on purpose, and it says "they" of the author on purpose.

---

**Windows is the supported platform.** It is where GroupLab is developed and tested by hand, and the installer and automatic updates are built for it.

**Linux builds are published and are worth trying.** The download is a self-contained 64-bit tarball, so it runs on most desktop distributions without anything else being installed alongside it. The test suite runs on Linux on every build. Hands-on testing has not started yet. Linux can be tested here on virtual machines under VMware Workstation, and there is no bare metal Linux machine, but the real reason is that the application is still under heavy development, with features, layouts, appearance and internal workings changing daily. Testing a moving target on a second platform would mostly produce findings that are obsolete a week later.

**macOS builds are published and have never been run on a Mac.** The tests run on macOS on every build, so the code works at that level, but nobody has opened the window, printed a target or saved a session on real hardware. These builds are an experiment rather than a release.

### What happens once the application settles

Other platforms get proper attention once the pace of change slows and the Windows application is generally working the way the developer wants it to.

**Android is planned and is a high priority**, because that is the mobile platform in daily use here. Hands-on Linux testing follows, on virtual machines. macOS depends on the hardware question below.

### Running the macOS build

macOS quarantines anything downloaded from the internet and refuses to open software that is not signed by a registered Apple developer. After the application has been moved to the Applications folder, this removes the quarantine flag:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Anyone not comfortable running that command should not run this build.

### Why it is not signed

Signing a macOS application requires the Apple developer programme, which costs 99 dollars a year. The developer of GroupLab does not own a Mac, does not intend to buy one, and is not going to pay a yearly fee for a platform they do not own.

That is the whole reason. It is not a technical obstacle and it is not indifference to Mac users. If a developer or contributor wants signed macOS releases enough to donate a Mac for testing and cover the developer fees, the project will set it up.

### Signing elsewhere

The one-off 25 dollar Google Play developer fee has been paid. A signed Windows version through the Microsoft Store is intended in due course, and a code signing certificate may be bought if the price turns out to be reasonable.

### Apple mobile

An iPad Mini, sixth generation, is available as test hardware, and an iOS version of GroupLab would be tested on it. Building and signing an iOS application requires a Mac and the Apple developer programme, so that version cannot be produced at present, for the same reason the macOS build is unsigned. The hardware to test it exists; the machine to build it does not.

### Other Linux builds

The published Linux build is x86-64. Other targets can be added to the nightly builds on request: Arm64 for a Raspberry Pi or an Arm laptop, or a package built for a particular distribution rather than a tarball. Adding one is a line of configuration rather than a project. The reason a dozen are not published already is simply that nobody has asked for them.

Requests go to support@grouplab.org, naming the distribution and the architecture.

### Reports from Linux and macOS are welcome

A report is useful even when the answer is that it crashed on startup. "It opened and the buttons are the wrong size" is a useful report, and so is a crash report, which GroupLab can send on request. The address is support@grouplab.org.

---

Two notes for Code rather than for the page. Keep the build targets as a single list in the workflow, so adding one really is a line, and say in your report what a person must tell us for a target to be added without further questions. And a test should fail if this section's macOS wording, or the sentence saying nobody has run it on a Mac, disappears while a macOS asset is still published.

## 3.2 The same statement in all three places

The wording in section 3 goes, identically, to:

1. **The website**, as its own section of the download page at `https://grouplab.org/download/`, with its own heading so it can be linked to directly.
2. **The repository README**, which is the first page anyone sees on GitHub. Put a short "What is supported" section there with the same words, or the first two paragraphs and a link to the download page if the README would otherwise get unwieldy. The three facts that must appear on GitHub itself, not only behind a link, are that Windows is supported, that the macOS build has never been run on a Mac, and that other Linux targets can be requested.
3. **Every GitHub release that carries a macOS asset**, as a short note in the release body, next to the downloads, saying the macOS build is untested and unsigned, giving the quarantine command, and linking to the full statement.

Keep one source for the words: hold the statement in a single file in the repository, generate the website section and the README section from it, and have the release note quote from it. A statement that has to be edited in three places is a statement that will disagree with itself within a month. A test fails if the copies drift apart.

## 4. Ask for what would change it

Close the section with a plain invitation: if someone with a Mac wants to run the build and report what happens, that is useful on its own, and the support address is the way to do it. Make it easy to say "it started" or "it crashed at this point", and make it clear that crash reports from macOS are welcome even though macOS is not supported.

## 5. Keep it true

- A test fails if a macOS asset is published without the untested wording on the download page.
- The download page names each build's architecture, so an Apple silicon owner does not take the Intel one by accident.
- If a macOS build ever fails to package, the nightly still publishes the Windows and Linux ones rather than failing entirely, and says which one is missing.
