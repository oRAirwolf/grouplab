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
  Compare, Shots Needed to Zero, the first run, the printer check and the CSV import. **Moved (entry 290):** they are in
  `mobile/GroupLab.Mobile`, a plain .NET library the Android head links and the iOS head will, and the few Android calls in them are behind one interface,
  `IPhonePlatform`: the app's folders, the bundled sheets, a reduced decode, the memory budget, the camera, sharing, printing, pasting,
  and the update card. Because it is plain .NET it builds and is tested on any machine; `MobileProjectTests` holds it free of Android
  and iOS calls.
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

**OpenCV for iOS is built (entry 290 section 2).** `ios/opencv/build-extern.sh` builds OpenCV and opencv_contrib 4.13.0 as static
libraries, with the same modules as Android and none that need the camera, the screen or the GPU, then OpenCvSharp's native half from the
same tag as the managed package, cut to the same bindings as on Android and made a static library. It does this twice, for iPhone and
iPad (iphoneos arm64) and for the simulator on Apple silicon (iphonesimulator arm64), both with iOS 26 as the lowest version, merges each
into one archive with every third party library OpenCV built, checks that the archive defines the entry points GroupLab calls, and wraps
the two in `OpenCvSharpExtern.xcframework`, zipped with its SHA-256. The `ios` workflow runs it on `macos-26` with Xcode 26, caches the
result under a key made from the script, so it is rebuilt only when the script changes, and keeps it as the artifact `ios-opencv` for
thirty days. The head links it statically and calls it through `__Internal`.

**The lowest iOS version: 26.** Entry 206 section 4 worked the floor out as an iPhone 11 on iOS 26 or later, and it stands: iOS 26 runs on
every iPhone from the 11 of 2019 on and on the iPad mini (6th generation), which is the test device; one floor lets the camera code use
one set of AVFoundation calls with no version checks; and nothing older can be tested, since there is no device to test it on.

**What is built (entry 290 section 2 items 3 and 4):** the head, `ios/GroupLab.iOS`, with the shared screens, the sheets in its bundle,
the icon from the desktop's mark, OpenCV linked statically, and the camera's place holding the files picker until the camera arrives. The
workflow `ios app` builds it for the simulator and for a device, unsigned, and runs a self-test on an iOS 26 simulator: every place along
the bottom is opened and photographed, the imaging is checked on a rendered sheet, and the committed sample scan is analyzed through the
phone's own pipeline and compared with the desktop running the same checks in the same job.

**The camera (entry 290 section 2 item 5):** built in `ios/GroupLab.iOS/Camera`, Capture B as on Android. An AVFoundation session on the
back wide camera in the 4:3 mode whose stream is nearest 1920 by 1440 (`PhoneCamera.ChooseMode` in Core, the rule CameraX follows on
Android), its preview shown whole, a photo output taking that mode's largest 4:3 picture as a JPEG (a HEIC is turned into a JPEG, since
OpenCV on iOS reads none), and the stream's luminance judged frame by frame by the same Core rules as on Android. Guided and Manual, the
torch (off, on, or automatic in five steps of strength by the same TorchGovernor as on Android, and always off once the picture is taken), tap to focus, the level from Core Motion's gravity turned with the
screen, and the camera let go in the background and taken again in the foreground. The overlay is UIKit's own views in one native view,
for the reason Android's is Android's. The iPad mini has one camera at the back, so there is no lens button. Core Motion's gravity needs
no permission, so the only question iOS asks is the camera's. On the simulator, which has no camera, Take a picture opens the files
picker instead, and the self-test proves that it does and that a picture which is not a JPEG comes out as one.

**Sharing, printing, pasting and the idle screen (entry 290 section 2 item 6):** the simulator's self-test hands a GroupLab PDF of the
sample sheet to Share and to Print and checks that iOS's share sheet and print sheet each open and close with GroupLab still there, puts
the sample scan on the pasteboard and presses Paste a picture on Capture through to a saved session, and `ios/selftest.py` reads the idle
screen's screenshot and fails unless it is black to every edge, the strips behind the status bar and the home indicator included.

**Pictures from anywhere (entry 292 section 2):** built in `ios/GroupLab.iOS/Photos` and `ios/GroupLab.Share`. **Choose a photo**
opens the Photos picker (`PHPickerViewController`, images only), which shows the whole library, photographs kept only in iCloud Photos
included, and asks for no permission, since GroupLab sees only what is chosen. It asks for the photograph as it was taken (the current
representation, the file itself, never a copy made for the screen); one kept only in iCloud downloads through the item provider, whose
progress is the same progress line and **Cancel** as on Android, and Cancel stops the download. **From another app** opens Files
(`UIDocumentPickerViewController` for `public.image`), which reaches iCloud Drive, Google Drive, OneDrive, Dropbox and every other
installed Files provider, and downloads a cloud file itself before handing it over. iOS gives no app a way into another app's library, so
**Google Photos reaches GroupLab by sharing**: a share extension, `org.grouplab.app.share`, puts GroupLab in the share sheet of Google
Photos, Photos and every other app, copies each shared picture whole into the app group `group.org.grouplab.app`, and opens GroupLab at
`grouplab://shared`; document types make **Open in GroupLab** work from Files and other apps. Either way the picture goes straight into
analysis, several shared at once as a set, one per sheet; a share the extension could not open GroupLab for waits and is read the next
time GroupLab opens. A HEIC photograph is written as a JPEG for OpenCV, at full size and without its location. Whether the device is
online comes from Network's path monitor, so a photograph that could not be fetched is said to be for that reason. The simulator's
self-test opens and cancels both pickers, reads the sample scan into analysis through **Open in**, and through a share left in the app
group as the extension leaves one and opened at `grouplab://shared` through iOS by the call the extension makes; the workflow checks
that the extension is inside the application and that iOS registered it. When the simulator itself opens that address, iOS asks "Open in
GroupLab?" first, which is why the sitting checks what a share shows. The signed build signs the extension with its own App Store profile, for `org.grouplab.app.share`, and
both profiles carry the app group (section 3).

## The first TestFlight sitting

What only a device can prove, checked on the iPad mini with the first TestFlight build that carries the camera. Each line is a yes or a
no, and a no comes back as a note with what was seen.

1. **The question.** The first Take a picture asks for the camera in iOS's own words and GroupLab's sentence; after Allow, pressing again
   opens the camera. After Don't Allow, GroupLab says so and does not crash.
2. **The preview fills 4:3.** The whole picture is shown, nothing cut from its edges, with black bars rather than a stretch, upright and
   in portrait, in both landscapes and upside down.
3. **The panel.** Back, the instruction, the torch, the checks line and the quality bar are all visible above the preview, the preview
   starting under the panel (entry 313 section 2), and none is under the status bar or the home indicator; the shutter and the two modes are under the camera and answer a tap.
4. **Guided fires.** Over a printed GroupLab sheet the words settle (no flicker between Move closer and Move back), the ring fills, and
   the picture is taken by itself about a second after the words say Hold it there (entry 311 section 1).
5. **Manual shutter.** In Manual the picture is taken only when the shutter is pressed; the mode is remembered after closing GroupLab.
6. **The torch.** Torch: On lights it at full strength while framing, and it goes off the moment the picture is taken; Torch: Off never
   lights it. After Back, and after a picture, it is off. **On Auto (entry 302)**, in a dim room it comes on at the lowest of its five levels and steps up a level at a time, about
   every second and a half, only while the paper is still dim; over glossy paper, or with a bright spot from the torch on the sheet, it
   steps down or goes off, and does not come back up to the level that glared; on paper already bright it goes off. It never flickers.
   The log's `camera.torch` lines give each change, its level of five and its reason, which the sitting's notes can quote.
7. **The level.** Flat over a sheet on a table the dot sits in the ring and turns green; raising an edge sends it toward that edge, in
   portrait and in landscape.
8. **Background and foreground.** With the camera open, go to the Home Screen and come back: the preview runs again, the torch is as
   chosen, and the analysis carries on. The same with the iPad locked and unlocked, and with Split View or Slide Over opened over it.
9. **End to end.** The picture taken is read: the picture check shows, then the result with the holes, and the session is saved with
   the sheet named; a picture taken in landscape is read the right way up.
10. **The shutter's timing.** The press is answered at once, with iOS's shutter sound and the white flash, within about 0.3 s, and the
    result follows as soon as the reading allows. Measured from a screen recording started in Control Center, frame by frame from the
    press to the flash and to the result; the log's `camera.shutter` lines hold the same steps, but the iPad cannot hand its log over yet.
11. **Tap to focus.** A tap on the preview sharpens that part of the sheet and holds it until the next tap.
12. **The Photos picker (entry 292).** Choose a photo opens Photos with no question about access; a photograph on the iPad is read
    at its full size, and a HEIC one reads the right way up.
13. **A photograph kept only in iCloud.** With Optimize iPad Storage on, a photograph not on the iPad downloads with the line saying how
    far it has got, and Cancel stops it and returns to Capture. The same with Wi-Fi off says the iPad is offline.
14. **Files.** From another app opens Files; a picture from iCloud Drive and one from Google Drive or OneDrive, where installed, are read.
15. **Shared from Google Photos.** GroupLab is in Google Photos' share sheet; sharing a photograph there opens GroupLab and reads it straight
    into analysis, with no question from iOS in between, or with only its "Open in GroupLab?" (note which). The same from Photos, and
    with three shared at once, which are read as a set.
16. **Open in GroupLab.** From Files, Share, then GroupLab in the row of apps (or Open in), opens GroupLab and reads the picture.
17. **A share while GroupLab is closed.** Quit GroupLab, share a picture into it: it opens and reads it. If iOS will not let the share
    sheet open GroupLab, the sheet says the picture is waiting, and opening GroupLab reads it.
18. **The sheet beside the numbers (entry 290 section 6).** On the iPad mini in landscape, a result shows the sheet on one side and
    the numbers on the other, and turning the iPad back to portrait puts them one above the other again. The iPhone simulator cannot
    show it: in landscape it is 832 points wide inside its safe area, and the side by side layout starts at 840.

## 3. Building without a Mac

The nightly workflow gains an iOS job on GitHub's `macos-26` runner, free for a public repository, with Xcode 26. It builds the head,
signs it with the distribution certificate and two App Store provisioning profiles, one for the application and one for its share
extension, and uploads it to TestFlight with the App Store Connect API key. The signing material and the key are secrets Alan sets himself
with `gh secret set`, request 55 in `docs/notes/for-alan.md` says how; neither session ever sees them. Until all eight are there, the job
builds without signing and uploads nothing, so a nightly never fails for want of them. The secrets it reads: `APPLE_TEAM_ID`,
`IOS_DIST_CERT_P12`, `IOS_DIST_CERT_PASSWORD`, `IOS_PROFILE`, `IOS_SHARE_PROFILE`, `APPLE_API_ISSUER_ID`, `APPLE_API_KEY_ID` and
`APPLE_API_KEY_P8`.

**The share extension (entry 292 section 2.3) needs its own identifiers in the Apple Developer site**, beside the application's:

1. An **App Group**, `group.org.grouplab.app` (Certificates, Identifiers and Profiles, Identifiers, App Groups).
2. The application's App ID, `org.grouplab.app`, with the **App Groups** capability on and that group ticked.
3. A second App ID, `org.grouplab.app.share`, for the extension, with **App Groups** on and the same group ticked.
4. A second **App Store** distribution profile, for `org.grouplab.app.share`, made with the same distribution certificate; and the
   application's profile made again (or edited and downloaded again) after step 2, so it carries the group.

The application's profile goes in `IOS_PROFILE` and the extension's in `IOS_SHARE_PROFILE`, each as base64. The signed publish gives each
project its own profile (`GroupLabAppProvision` and `GroupLabShareProvision`, which `scripts/ios-signing.py --properties` builds), because a
single profile given on the command line would reach the extension too; `ios/signing-dry-run.sh` proves that with made-up values in every
nightly and every push to an `ios/` branch, and after the publish the job checks that the package carries the extension and that each
bundle holds its own profile.

**In every nightly (entry 290):** the nightly's `ios` job builds GroupLab for iPhone and iPad with the nightly's version on `macos-26`,
with OpenCV from the `ios` workflow's cache. It is not among the jobs publishing waits for, so the other builds are never held up by it.

**The check that decides (entry 290):** `scripts/ios-signing.py --check` reads the eight and prints one line for each, set or not and
whether its shape is right, never a value. None set: the build is not signed and nothing is sent, exit 3. All eight set and right: it
signs. Some set, or one malformed: it fails, naming the secret and what is wrong with it (a team ID that is not ten capitals and digits, a
certificate that is not base64 or not a .p12, a profile for another team or another app than `org.grouplab.app` or, for the extension,
`org.grouplab.app.share`, a profile without the app group `group.org.grouplab.app`, an issuer that is not a UUID, a key that is not a
.p8), so a mistake is found on the night it is made. Its self-test runs with made-up values in every build.

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
