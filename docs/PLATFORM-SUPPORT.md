# What is supported, and what is not

NOTES-FROM-PLANNING.md entry 147 section 3. **This file is the one source of this statement.** It is word for word as Alan settled it, and it is not to be reworded; entry 166 section 4 replaced the macOS paragraph with what the first tester on a real Mac checked, and nothing more. It avoids the first and second person on purpose, and it says "they" of the author on purpose.

Entry 147 section 3.2, as entry 168 section 5 amended it: the statement appears on the download page and in `README.md`, both generated from here, and every release that carries a macOS asset gets one line generated from its lead sentences with a link to the download page, because a published release is frozen and a whole copy on it goes stale. Nothing else may restate it, because a second copy is a copy that goes stale and nobody notices which one is wrong.

- `website/build.py` renders it into `/download/`.
- `scripts/platform-support.py` writes it into `README.md` between its two markers, and checks that it is current.
- `.github/workflows/nightly.yml` appends the one line `scripts/platform-support.py --release` makes to a release whose assets include a macOS build.

---

**Windows is the supported platform.** It is where GroupLab is developed and tested by hand, and the installer and automatic updates are built for it.

**Linux builds are published and are worth trying.** The download is a self-contained 64-bit tarball, so it runs on most desktop distributions without anything else being installed alongside it. The test suite runs on Linux on every build. Hands-on testing has not started yet. Linux can be tested here on virtual machines under VMware Workstation, and there is no bare metal Linux machine, but the real reason is that the application is still under heavy development, with features, layouts, appearance and internal workings changing daily. Testing a moving target on a second platform would mostly produce findings that are obsolete a week later.

**macOS builds are published, and the Apple silicon build has been run on one Mac.** One tester ran nightly 93 on a MacBook Pro with an M5 Max, under macOS 27, natively rather than under Rosetta. macOS blocked the first launch, and the Terminal command below cleared it. Opening, detecting and analysing the published sample, saving a session, printing a target to PDF, quitting with Command Q and sending the diagnostics report all worked, and text was sharp on the Retina display. Command shortcuts such as Command Z did not work, and pinch zoom had not been built on any platform; both are fixed in builds after nightly 94, and neither fix has been checked on a Mac yet. **The Intel build has never been run on a Mac.** The tests run on macOS on every build. These builds are an experiment rather than a release. The updater does not install them, and the developer still does not own a Mac.

## What happens once the application settles

Other platforms get proper attention once the pace of change slows and the Windows application is generally working the way the developer wants it to.

**Android is planned and is a high priority**, because that is the mobile platform in daily use here. Hands-on Linux testing follows, on virtual machines. macOS depends on the hardware question below.

## Running the macOS build

macOS quarantines anything downloaded from the internet and refuses to open software that is not signed by a registered Apple developer. After the application has been moved to the Applications folder, this removes the quarantine flag:

```
xattr -dr com.apple.quarantine /Applications/GroupLab.app
```

Anyone not comfortable running that command should not run this build.

## Why it is not signed

Signing a macOS application requires the Apple developer programme, which costs 99 dollars a year. The developer of GroupLab does not own a Mac, does not intend to buy one, and is not going to pay a yearly fee for a platform they do not own.

That is the whole reason. It is not a technical obstacle and it is not indifference to Mac users. If a developer or contributor wants signed macOS releases enough to donate a Mac for testing and cover the developer fees, the project will set it up.

## Signing elsewhere

The one-off 25 dollar Google Play developer fee has been paid. A signed Windows version through the Microsoft Store is intended in due course, and a code signing certificate may be bought if the price turns out to be reasonable.

## Apple mobile

An iPad Mini, sixth generation, is available as test hardware, and an iOS version of GroupLab would be tested on it. Building and signing an iOS application requires a Mac and the Apple developer programme, so that version cannot be produced at present, for the same reason the macOS build is unsigned. The hardware to test it exists; the machine to build it does not.

## Other Linux builds

The published Linux build is x86-64. Other targets can be added to the nightly builds on request: Arm64 for a Raspberry Pi or an Arm laptop, or a package built for a particular distribution rather than a tarball. Adding one is a line of configuration rather than a project. The reason a dozen are not published already is simply that nobody has asked for them.

Requests go to support@grouplab.org, naming the distribution and the architecture.

## Reports from Linux and macOS are welcome

A report is useful even when the answer is that it crashed on startup. "It opened and the buttons are the wrong size" is a useful report, and so is a crash report, which GroupLab can send on request. The address is support@grouplab.org.
