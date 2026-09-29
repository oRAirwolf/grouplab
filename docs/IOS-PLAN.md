# GroupLab on iPhone and iPad: the plan

NOTES-FROM-PLANNING.md entry 278 section 6 and entry 279 section 1. Alan decided on 2026-09-28 that GroupLab will be built for iOS. He
enrols in the Apple Developer Program as an individual from his iPad mini (6th generation). He has no Mac and no iPhone, and the plan needs
neither: GitHub's Mac machines build and sign every build, and TestFlight puts it on the iPad mini, and later on testers' iPhones.

This is a plan with sizes. Nothing here is built yet except where a line says so.

## 1. What carries over unchanged

Everything below the screen is already platform-free .NET and runs on iOS as it is:

- `GroupLab.Core`: the target format, the renderer, detection, registration, the statistics, the ballistics, the picture check, the
  sessions database (SQLite through Microsoft.Data.Sqlite) and every figure and word the phone shows.
- The phone's screens that are Avalonia controls, not Android ones: the result, the figures, Sessions, Ballistics, Targets, Settings,
  Compare, Shots Needed to Zero, the first run, the printer check and the CSV import. They move from `android/GroupLab.Android/` to a shared
  mobile project both heads link, with the few Android calls in them behind small interfaces.
- The desktop's shared pieces the phone already links: settings, the plot, the curves, unit taps, the diagnostic log and error reports.

## 2. What is new, and how big

| Piece | What it is | Size |
|---|---|---|
| The iOS head | `ios/GroupLab.iOS`, an Avalonia iOS application, bundle ID `org.grouplab.app`, iPhone and iPad, with the shared mobile screens | 3 to 4 days, most of it moving the screens into the shared project |
| OpenCV for iOS | OpenCV and OpenCvSharp's native half built as a static framework for iOS arm64 (and the simulator), with the same modules as Android's `android/opencv/build-extern.sh`: the ArUco and AprilTag 36h11 detector, the QR readers, calib3d, imgproc; linked statically, since iOS loads no outside library | 2 to 3 days, most of it the first CI build |
| The camera screen | AVFoundation: a capture session, the preview layer, a photo output and a video data output for the live analysis at 1920 by 1440, 4:3 throughout; the same Capture B design, Guided and Manual, the picture score, the bubble level from Core Motion, and the torch, whose level iOS lets an application set while the camera runs; hosted in the Avalonia screen as a native view, as on Android | 4 to 5 days |
| Printing, sharing, files | The iOS print sheet, the share sheet, and the files picker through Avalonia's storage provider | 2 days |
| The OLED rules | The black idle screen while not in use, the screen kept awake only while a sitting drives it, and every setting put back | half a day |
| The build | A job on GitHub's `macos-26` machine with Xcode 26 that builds, signs and sends each nightly to TestFlight (section 3) | 1 to 2 days, most of it waiting for builds |

About three weeks of work in all, done a piece at a time around the rest of the inbox.

**The lowest iOS version: 26.** Entry 206 section 4 worked the floor out as an iPhone 11 on iOS 26 or later, and it stands: iOS 26 runs on
every iPhone from the 11 of 2019 on and on the iPad mini (6th generation), which is the test device; one floor lets the camera code use
one set of AVFoundation calls with no version checks; and nothing older can be tested, since there is no device to test it on.

## 3. Building without a Mac

The nightly workflow gains an iOS job on GitHub's `macos-26` runner, free for a public repository, with Xcode 26. It builds the head,
signs it with the distribution certificate and the App Store provisioning profile, and uploads it to TestFlight with the App Store Connect
API key. The signing material and the key are secrets Alan sets himself with `gh secret set`, request 55 in `docs/notes/for-alan.md`
says how; neither session ever sees them. Until all seven are there, the job builds without signing and uploads nothing, so a nightly
never fails for want of them. The secrets it reads: `APPLE_TEAM_ID`, `IOS_DIST_CERT_P12`, `IOS_DIST_CERT_PASSWORD`, `IOS_PROFILE`,
`APPLE_API_ISSUER_ID`, `APPLE_API_KEY_ID` and `APPLE_API_KEY_P8`.

Alan installs from TestFlight on the iPad mini. iPhone testers come later, by TestFlight invitation.

## 4. The App Store, and the licence

Entry 279 section 1 adds to LICENSE a GPLv3 section 7 additional permission for distribution through Apple's App Store and TestFlight
under Apple's terms, and CONTRIBUTING says that every contribution is accepted under the licence with it. Alan is the only copyright holder, so
he can grant it. No other project's GPL code enters the iOS build: the AprilTag code table is BSD-2-Clause, the fonts are under the SIL Open
Font License, the packages are MIT or Apache-2.0, and OpenCV, opencv_contrib and OpenCvSharp are Apache-2.0 (`THIRD-PARTY-NOTICES.md`).
Submission to the App Store waits for Alan, once the application is ready.

## 5. In what order

1. The secrets and the app record (request 55), which need only Alan and can happen first.
2. The shared mobile project, which improves the Android build too, since one set of screens serves both.
3. OpenCV for iOS in CI, then the head with the screens and no camera, sent to TestFlight: the first build on the iPad mini.
4. The camera screen.
5. Printing, sharing, files and the OLED rules.
