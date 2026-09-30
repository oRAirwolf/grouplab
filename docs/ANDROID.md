# GroupLab on Android

NOTES-FROM-PLANNING.md entries 198 and 199, 2026-09-25. This is the plan and what the first stage found. The first stage proves the
desktop's engine runs on the phone before any screen is designed; it ends with a report of how fast, and the rest of this document is
the plan that report decides.

**Where it stands.** The spike and its build are written: `android/GroupLab.Android.Spike/`, `android/opencv/build-extern.sh` and
`.github/workflows/android.yml`. **Both build in CI** (5a1e769): the native library is 20 MB, needs nothing but Android's own system
libraries, and is aligned for 16 KB pages; the debug APK is 39 MB and carries it, the sheets and the sample scan. **Detection runs on the
Fold 7** (entry 202, section 5): the desktop's engine, unchanged, names the sample scan from its codes and finds all 25 holes in
17 seconds. **Folding, unfolding, turning and the largest font size passed** (request 27, entry 205, section 7), so Avalonia is confirmed
for the phone.

## 1. What Alan decided (entry 198 section 1)

- **The first version analyzes on the phone, offline**: the capture screen of `docs/MOBILE-CAPTURE.md`, detection, review and
  correction, the group figures, and saved sessions, with the desktop's engine, not a second one. Printing, the target designer and
  Ballistics stay on the desktop at first. Nothing needs the network except sending a target and error reports, which queue as they do
  on the desktop.
- **Distribution**: Google Play testing tracks, and a nightly APK on the GitHub release and grouplab.org for sideloading. A newer
  personal Play account runs a closed test with at least 12 testers for 14 days before a production listing; the testers come from the
  Discord server. No dates are promised.
- **The package name is `org.grouplab.app`**, permanent once on Play. The spike uses `org.grouplab.app.spike`, so nothing built from
  it can be taken for the real application.
- **The path to a public Play listing is Google's own rule** for a new personal developer account: a closed test with at least 12
  testers for 14 days. Nothing else holds it (entry 267: the GPL section 7 app-store permission concerns Apple's App Store, not Google's).
- **iOS is being built** (entry 278 section 6, `docs/IOS-PLAN.md`), tested on the iPad Mini and built on GitHub's Mac machines.

## 2. The user interface: Avalonia on .NET Android

**Confirmed as the path, to be proved by the spike on the phone.** GroupLab.Core is plain .NET and runs on Android unchanged, and
Avalonia 12 runs on .NET Android with the same controls, styles and layout as the desktop. The desktop's `MainWindow` is written for a
mouse and a wide window, so the phone gets its own views, but they are Avalonia views in C# over the same Core, not a second codebase.
What would force a different answer: Avalonia failing the checks in section 7 on the Fold 7 (the density, the fold, rotation, the
system font size), or the camera preview (section 4) not being hostable inside it.

## 3. OpenCV on the phone

**The calls GroupLab makes.** Counted on 2026-09-25 across `src/`: about thirty OpenCV functions (decoding and encoding images, splitting
channels, thresholds, morphology, contours, connected components, the distance transform, resizing, phase correlation and a RANSAC
homography), the ArUco marker detector with its parameters and the AprilTag 36h11 dictionary, the WeChat QR detector used without
its neural network models, and OpenCV's plain QR decoder. They sit in OpenCV's core, imgproc, imgcodecs, calib3d and objdetect
modules, and in two contrib modules, aruco and wechat_qrcode, which needs dnn.

**The routes, and the one chosen.**

| Route | Why or why not |
|---|---|
| OpenCvSharp's official runtimes | Windows, Linux and macOS only. |
| Sdcb's "mini" runtime for android-arm64 | It exists and is built the right way, but carries only core, imgproc, imgcodecs and dnn: no homography, no markers, no QR. |
| Replace the calls with managed code | The WeChat detector, the marker detector and a RANSAC homography are each a project, and a second engine is what entry 198 rules out. |
| **Build OpenCvSharp's native half for Android with GroupLab's modules** | **Chosen.** `android/opencv/build-extern.sh` does what Sdcb's pipeline does (NDK, API 24, the C++ runtime linked statically, OpenCV static inside one `libOpenCvSharpExtern.so`) with GroupLab's modules, and compiles only the OpenCvSharp bindings for them. It pins OpenCV 4.13.0 and OpenCvSharp 4.13.0.20260627, the version the desktop uses, because the managed and native halves must match. CI keeps the result until the script changes. |

**Licenses.** OpenCV (since 4.5), opencv_contrib and OpenCvSharp are Apache-2.0; Sdcb's pipeline, read for how the build is done and not
copied, is Apache-2.0 too. Apache-2.0 code may be combined into a GPL-3.0 work, which is the direction GroupLab needs.

## 4. The camera

Avalonia has no camera. **The route is CameraX through the .NET Android bindings** (`Xamarin.AndroidX.Camera.*`), with its preview
view hosted inside the Avalonia screen as a native Android view. What MOBILE-CAPTURE.md needs from it:

- **Full resolution stills**: CameraX's still capture at its largest size, in maximum quality mode.
- **Lens choice, item L2**: CameraX lists each rear camera, and its Camera2 interop gives each one's focal lengths and sensor size, so
  the longest lens that frames the sheet can be chosen and named. On the Fold 7 that is a choice between the wide, ultrawide and
  telephoto cameras.
- **Distortion and the record, items L3 and L5**: the intrinsics and distortion terms come from the same Camera2 interop where the
  device reports them. No location is read from the capture or the file, as on the desktop.
- **Focus and exposure**: tap and automatic metering on the sheet, exposure compensation, and a lock while the photograph is taken.
- **The live preview** runs on the camera's own surface, so its speed does not depend on Avalonia; the checks the capture screen makes
  on it run on a smaller analysis stream beside it.

**The Fold 7's rear cameras** (entry 209, read through Camera2 without opening a camera):

| Camera | Focal length | 35 mm equivalent | Sensor | Largest still | Intrinsics and distortion |
|---|---|---|---|---|---|
| 0, the main one, made of 2, 5 and 6 | 6.25 mm | about 22 mm | 9.79 by 7.34 mm | 4080 by 3060, 12.5 MP | reported |
| 2, ultrawide | 2.20 mm | about 14 mm | 5.60 by 4.20 mm | 4000 by 3000, 12.0 MP | reported |
| 5, wide | 6.25 mm | about 22 mm | 9.79 by 7.34 mm | 4080 by 3060, 12.5 MP | reported |
| 6, telephoto | 7.00 mm | about 66 mm | 3.65 by 2.74 mm | 3648 by 2736, 10.0 MP | reported |

Every camera reports its intrinsics and radial distortion terms, so items L3 and L5 have what they need on this phone. The main camera's
largest ordinary still is 12.5 MP; its full sensor needs Camera2's maximum resolution mode, which the capture screen does not need at an 8 MP
working size. For item L2 the telephoto is the longest lens, at about 66 mm, and the wide the fallback when the sheet does not fit.

If a MOBILE-CAPTURE.md item proves impossible on Android, it is named here with the reason. None is known yet.

## 5. Speed and memory

**The desktop, for comparison**, the spike's own code (`SpikeRun.Run`) on Alan's machine, 2026-09-25, a Release build:

| Image | Size | Load | Codes and naming | Marking | Holes | Peak memory |
|---|---|---|---|---|---|---|
| The published sample scan, 600 dpi | 4958 by 6458 | 0.4 s | 1.2 s, 2 codes | 6.2 s | 25 of 25 | 732 MB |
| A range photograph from 2026-09-20 | 4000 by 3000 | 0.07 s | 0.9 s, 1 code | refused: no markers found | | 440 MB |

The photograph's refusal is the desktop's own result on that picture, not an Android one; the spike runs it to time the stages a
photograph goes through.

**On the Fold 7** (SM-F966U1, the cover screen), the same code in a Release build of the spike, 2026-09-25:

| Image | Load | Codes and naming | Marking | Holes | In all | Peak memory |
|---|---|---|---|---|---|---|
| The published sample scan, 600 dpi | 0.6 s | 1.5 s, 2 codes | 15.0 s | 25 of 25 | 17.1 s | 714 MB |
| The same range photograph | 0.1 s | 1.0 s, 1 code | refused: no markers found, as on the desktop | | 1.2 s | |

Two further runs of the scan took 16.3 and 16.8 s, and Alan's own run on 2026-09-24 took 18.9 s at a peak of 716 MB. **Each image's
own peak**, from a fresh process with the photograph run first (entry 205): the photograph 635 MB, the scan 721 MB; the 900 MB Alan saw
was the process's highest so far, after the scan. ****Working resolution on the Fold 7** (entry 209, for entry 206 section 2.2): the sample brought to each size once, then measured in a fresh
process on the smaller file alone, since a phone never decodes a 32 MP scan to work on 8 of it. Hole offsets are from the full resolution
run on the same phone, in thousandths of an inch; the full resolution run is itself a measurement, not the truth.

| Working size | As a Letter scan | In all | Holes | Peak memory | Offset from full, mean and largest |
|---|---|---|---|---|---|
| 32.0 MP, the scan as it is | 600 dpi | 16.1 s | 25 of 25 | 715 MB | |
| 14.2 MP | 400 dpi | 6.6 s | 25 of 25 | 461 MB | 1.6 and 8.4 |
| 12.0 MP, a camera's still | 367 dpi | 5.1 s | 25 of 25 | 427 MB | 1.2 and 6.4 |
| 8.0 MP | 300 dpi | 3.3 s | 25 of 25 | 373 MB | 2.5 and 11.8 |
| 3.6 MP | 200 dpi | 1.6 s | 25 of 25 | 307 MB | 2.9 and 8.2 |

The spike at rest holds about 274 MB (`dumpsys meminfo`, total resident), so at 8 MP the engine adds about 100 MB.

**The working size is now a Core setting** (entry 219 item A1): `WorkingSize.PhoneMegapixels`, 8, which the phone application always
uses, and `ImageLoader.Load(path, most)` decodes at it, reducing a JPEG while decoding where it can. `grouplab analyze
--working-megapixels 8` does the same on the desktop. **What it costs in accuracy**, full size against 8 MP on the desktop, 2026-09-25:
the sample, 25 of 25 shots on the same bulls, a mean shift of 1.2 thousandths of an inch and 4.2 at most, mean radius and sigma
unchanged at 0.232 and 0.185 in; two range photographs of 12 MP that read plausibly at full size, 15 of 15 and 15 of 16 shots on the
same bulls, mean shifts of 3.6 and 7.9 thousandths (13.3 and 38.4 at most), mean radius 0.232 to 0.233 and 0.249 to 0.250 in. **What it says for the
budget**: a working size of 8 MP, 300 dpi for a Letter sheet, is under entry 206's 400 MB with every hole found and a mean shift of a few
thousandths of an inch; the 300 MB aim is reached only near 200 dpi, most of it the application at rest. Loading at full size and shrinking
afterwards costs about 530 MB whatever the working size, so the real application decodes at the working size, which the camera does for a
photograph and a reduced decode does for a scan. **Until entry 239 the application did not:** a picked file was decoded whole by
OpenCV and only then shrunk, which is the 555 MB request 43 measured on the tablet. It is now decoded by Android at a power of two
fraction (`WorkingSize.SampleFor`), a 600 dpi Letter scan at a half, 8 megapixels from the start.

The phone is a little over twice the desktop's time**, all of it in the marking;
loading and naming are close to the desktop's. The peak is the whole process's highest so far, so it is read from the first run in a
fresh process; the photograph ran after the scan and its own peak cannot be separated. **Memory held**: 714 MB, and 901 MB after three
runs, and Android did not stop the application. It is still more than a phone application should hold, so the real application
processes a capture at a capped resolution, as MOBILE-CAPTURE.md section 5 already does for the quality score, and marks a 600 dpi scan
in one pass without keeping earlier images.

## 6. The lowest Android version: 10 (API 29)

**Android 10 is GroupLab's minimum** (entry 207, approving entry 206 section 2.1): about 91 percent of Android devices in use; the phones
it leaves out are from 2019 or before with 2 or 3 GB, which could not hold the engine anyway. The spike is built for it. The native
library is built for API 24 and runs on 29.

**What "supported" means.** .NET 10 lists Android 14 and later as supported: what Microsoft tests and answers for, not what runs. The
spike was built for API 24 and runs, and the application is built for API 29. So Android 10 to 13 rest on GroupLab's own testing, on the
oldest phone Alan finds (request 30), not on Microsoft's. Android 14 and later alone would be about 55 percent of the Android phones in
use, which is why GroupLab does not simply follow .NET's list.

## The phones it is tested on (entries 211 and 212)

**All development and routine testing runs on the Fold 7.** Alan's older phones replace the Galaxy A16 and A06 classes of entry 206 as the
low end references; the market figures below stay as the reason for the budget. They are brought out only at named milestones, each one
sitting that does everything needing them at once, never for one small check:

- **Once before the first Play closed testing release:** the PH-1 and the S20. Install, start, detection at the chosen working size with
  its time and peak memory, and the capture screen once. **This is where Android 10 is confirmed as the minimum** (.NET 10 answers only
  for 14 and later, section 6) or the minimum is revisited.
- **Otherwise only** for a reported problem that cannot be reproduced on the Fold 7 or the emulator.

**The Fold 7 is batched too**: when a stage needs it, everything needing it is gathered into one sitting and put to Alan in advance in
`docs/notes/for-alan.md`, as entry 209 did.

| Phone | Android | Processor and memory | Role |
|---|---|---|---|
| Galaxy Z Fold 7, SM-F966U1 | 16 | Snapdragon 8 Elite | development, every day; measured in section 5 |
| Galaxy S20 5G | 13 | 8 GB, 128 GB | the middle reference, below Android 14 |
| Essential PH-1 | 10 (API 29), its last update | Snapdragon 835 (msm8998), 4 GB, read from the phone on 2026-09-25 | the floor: the minimum Android and memory, a processor in the budget phones' class |
| OnePlus 6T | 11, its last update | Snapdragon 845, 6 or 8 GB, as the planning session understands it; confirmed when next connected | optional at the milestone: it fills the Android 11 and 12 gap, and comes out only if it adds something the PH-1 and the S20 do not |

**The working size is chosen on the Fold 7**: 8 MP, 300 dpi for a Letter sheet, at 373 MB and 3.3 s there (section 5). Scaled by the
Geekbench figures below, that is roughly 25 s on the floor's class, inside the 30 s the budget allows; the emulator with limited cores and
memory is the next estimate, and the PH-1 at the milestone is the measurement.

## The phones it must run on

Entry 206, the planning session's study of 2026-09-25, in the spirit of the Steam hardware survey; approved by Alan in entry 207. **Where a
figure is judgment rather than measurement, it says so.**

**Android against iPhone** (StatCounter, web traffic, August 2026): United States 39.3 percent Android, North America 39.7, United
Kingdom 48.6, Germany 72.4, Europe 62.7, South America 76.9.

**Android versions in use, cumulative** (apilevels.com from StatCounter, April 2026): 16 or later 22.3 percent, 15 or later 41.0, 14 or
later 54.5, 13 or later 68.9, 12 or later 78.8, 11 or later 86.9, 10 or later 91.1, 9 or later 93.5, 8 or later 96.1, 7 or later 96.6.

**What sells.** In Latin America the 2025 top ten was almost all Android under 200 dollars, led by the Galaxy A06 at 7 percent, with the
Moto G15, Redmi 14C, Moto G05, Redmi A5, Moto G35, Redmi Note 14 4G, Galaxy A16, A15 and A56. The iPhone 17 led in the United States,
United Kingdom, Germany and France in the second quarter of 2026.

**Memory and storage.** No public survey gives installed memory by region. AnTuTu's first quarter 2026 report on Android outside China,
which leans toward enthusiasts, has 4 GB or less at 7.6 percent, 6 GB 9.8, 8 GB 39.3, 12 GB 36.1 and 16 GB 6.8; storage 128 GB 26.1 and
256 GB 49.7. It is the upper bound; the Latin American best sellers ship with 4 GB and 64 or 128 GB.

**Speed** (Geekbench 6, single and multi core, about): the Fold 7's Snapdragon 8 Elite 3196 and 10142; the Galaxy A16 5G's Exynos 1330 960
and 1826; the Galaxy A06's Helio G85 405 and 1349. So the A16 class is about a third of the Fold 7 on one core and a fifth on all of
them, and the A06 class an eighth on one core.

**Cameras.** Every phone above takes 12 MP or more with autofocus. A Letter sheet framed with margin spans about 13 inches of a 4000 pixel
image, so 12 MP gives about 300 pixels an inch and 8 MP about 250, against the quality score's perfect 150 and useless 50.

**Android 17** adds a limit on each application's memory scaled from the phone's own, counting native memory, where OpenCV's buffers
live; its formula is not published, and an application over it is ended.

### The budget, approved (entry 207)

1. **Android 10 or later**, section 6.
2. **4 GB of memory at least. Peak memory under about 400 MB, aimed at 300 MB**, on any image. Measured on the Fold 7 (section 5): 373 MB
   at an 8 MP working size with every hole found, 307 MB at 3.6 MP, and 274 MB of it the application at rest. So the engine works at
   8 MP, decoding at that size rather than shrinking a full decode.
3. **Speed** (judgment): detection within about 10 s on a Galaxy A16 class phone and about 30 s on an A06 class, with progress and a
   cancel; the capture screen's live checks at 10 frames a second or better on the A06 class. The Fold 7 takes 3.3 s at 8 MP; scaled by
   the Geekbench figures that is roughly 10 to 17 s on the A16 class and 25 s or more on the A06 class, which is only an estimate until
   it is run on a slower phone. **No Galaxy A16 will be bought**: the S20 and the PH-1 are the references (above), measured at the
   milestone before the first closed test; the Android emulator with limited cores and memory stands in until then, as a rough guide
   only, since it runs on the desktop's processor.
4. **A rear camera of at least 8 MP with autofocus**, refused with the reason otherwise.
5. **The installed application under about 100 MB**; a warning when free space falls under about 500 MB.
6. **Screens down to 360 dp wide.**

**GroupLab's own survey** (entries 207 and 208, `docs/SURVEY.md`) will replace these borrowed figures with what GroupLab actually runs on.

**Sources** (entry 206 section 5): digitalapplied.com/blog/mobile-os-market-share-2026-ios-vs-android;
gs.statcounter.com/os-market-share/mobile/north-america; apilevels.com; telemetrydeck.com/survey/apple/iOS/majorSystemVersions;
antutu.com/web/news/detail?id=136552; phonearena.com, best selling smartphones Q2 2026 (id182887); gsmarena.com, Counterpoint on the Galaxy
A06 in Latin America 2025 (news-71620); nanoreview.net, Galaxy A16 5G against A06; cpu-monkey.com, Snapdragon 8 Elite against Helio G85;
stora.sh, Android 17 memory limits guide; support.apple.com, iPhone models compatible with iOS 27.

## 7. Phones, foldables and tablets, touch first (entry 199 section 1)

- **Layout by the width available, not the device.** Compact, under 600 dp: a phone, and the Fold 7's cover screen. Medium, under
  840 dp: the Fold 7 open, small tablets. Expanded: the Tab S8 Ultra, and any landscape tablet. One screen rearranges; the spike already
  stacks its two panels when compact and puts them side by side otherwise. The desktop keeps its own layout.
- **Folding and turning are ordinary events.** The activity declares that it handles size, orientation, density and layout changes
  itself, so it is not destroyed and the photograph, marks, zoom, selection and a half-finished edit stay where they are while the view
  lays itself out again. The test to write when there is a review screen: a review in progress survives compact to medium and back.
  **Measured on the Fold 7** (request 27, entry 205): compact 411 by 960 dp at 2.625 pixels a dp on the cover screen, medium 750 by 832 dp
  unfolded, 832 by 750 turned, and back, with every earlier line kept and the panels stacked or side by side as designed.
- **All four ways up**, upside down portrait included: the activity asks for `FullUser`, which follows the sensor in every direction and
  still honors the rotation lock (`FullSensor` ignores the lock). Checked over adb on 2026-09-25: with rotation locked at 180 degrees the
  spike's screen turned to 180, the activity reports `SCREEN_ORIENTATION_FULL_USER`, and the phone's own rotation settings were put back.
  The capture screen's side of it is MOBILE-CAPTURE.md item C5. The tablet is checked when it is next to hand.
- **The start up sizes, and what repeated them.** Avalonia reports `1 by 1` and then the full size at 1 pixel a dp before the screen's
  density is known, and sizes of nothing during a fold; the spike logs them as ignored and never lays out from them, and the application
  does the same. Alan's second start up sequence was the activity being made again: pressing Back finishes the activity while the process
  lives on, and opening it again creates a new one (the log reads "activity destroyed, finishing" and then "activity created, the 2 time").
  Avalonia's single view belongs to the application, not the activity, so it and its list carried over. **The design**: everything a
  person is doing lives with the application, not the activity, and a session is written to disk whenever the application stops, so a
  new activity, or a process Android has ended, opens where the person left off.
  **The hinge**: Android reports it through Jetpack WindowManager (`Xamarin.AndroidX.Window`), not through Avalonia, so a split layout
  reads it there and keeps controls off it.
- **Density.** Avalonia works in density independent units and draws at the screen's real density, which the spike reports as pixels
  a dp. **The system font size is not followed by Avalonia on its own**: Android gives it as a font scale, and the application applies
  it to its text. The spike shows whether the largest accessibility sizes clip.
- **The composite plot's key** never covers the data (entry 213): beside the plot or below it, whichever leaves the plot the larger
  square, and a small Key button in its own strip where neither fits. On a phone it is the button by default.
- **Touch.** Targets at least 48 dp. Pinch to zoom and two finger pan on the photograph. One finger moves a shot only when it was
  grabbed, never while panning. A long press where the desktop has a right click. Nothing only on hover: everything the desktop shows on
  hover, the glossary included, opens on a tap. **Precise placement uses an offset handle with a magnifier**: the shot sits a finger's
  width above the finger, and a magnified view of the spot shows while dragging. The Fold 7's cover screen is the case to test it on.

## 8. Sessions between phone and desktop (entry 198 section 1.4, entry 199 section 2)

The session file is the unit whatever the route. A session edited on two devices never silently loses one side: each file carries a
revision and the device that wrote it, and a write that finds the file changed since it was read keeps both and asks. Photographs keep
today's rule on every route: no GPS, location or time metadata is read, printed, logged or sent.

**The order, with one change from the planning session's reading:**

1. **Stage A, the first version: share a session file by hand**, through Android's share sheet, a file manager, email or USB.
   **Built** (entry 219 item A5): a `.grouplab` file holds the marking, the sheet and the picture re-encoded from its pixels with no
   metadata at all, and says which device wrote which revision. **Share this session** on the phone's result hands it to the share
   sheet; **Open a session file** on Sessions, and **Share a session file** and **Open a session file** in the desktop's menu, do the
   rest. Opening one always makes a session of its own, so nothing is merged and nothing is lost. A file is read only as far as its
   four known entries, each within its size.
2. **The marks QR code, no account.** A photograph cannot go in a QR code; the marks can. The shots in sheet coordinates, the sheet's
   id, the caliber, the distance, the load and the review choices, in a compact binary frame of the kind `Gltd/Binary` already writes.
   Worked out, not yet measured: a shot takes about five bytes, so a 25-shot session with its load is a few hundred bytes compressed,
   well inside a mid-sized code. How large a code a phone reads off a laptop screen at arm's length is measured on the Fold 7; above
   that, the application says to share the file instead, rather than showing several codes in turn.
3. **The pairing QR code, no account, whole sessions.** The desktop shows a code holding a one time key and its local address; the
   phone sends the session, photograph included, over the local network, and only something holding the key is accepted. Windows asks
   once whether GroupLab may take connections on private networks; the application says why before that prompt appears. On different
   networks, or a range network that keeps devices apart, it says so and offers the file route.
4. **Stage B, a sync folder: possible through Google Drive** (request 29, entry 211): in the spike's folder picker on the Fold 7, Google
   Drive is listed and a folder in it can be chosen once opened. OneDrive is unknown; it may not be installed on the phone. Whether
   writes there sync reliably, and what an edit on both sides does, is the next question for this stage, when there are sessions to move. Picking a folder on Android uses the
   Storage Access Framework's folder picker, and the Google Drive and OneDrive applications offer single files to it but, as far as I
   know, not whole folders. If that holds, Stage B works on the desktop side only. Their conflict handling is also their own: an edit
   made offline on both sides comes back as two files, which the revision rule above would at least catch.
5. **Stage C, sign in, only if B falls short**: the Drive application data folder and OneDrive's application folder, through an OAuth
   client and an Entra registration Alan would create. Apple is out.

This is not part of the first stage; it is built when the application has sessions to move.

## 9. Builds

`.github/workflows/android.yml`, on every push to `main` that touches Core, the imaging code, the sheets or `android/`:

1. **OpenCV for android-arm64**, from the script, cached until the script changes. A build on this machine puts the same library in
   `android/native/arm64-v8a/`, which is never committed and which both projects read.
2. **The application's APK**, `grouplab-apk`, from `android/GroupLab.Android`, the permanent id `org.grouplab.app` (entry 219 item A3).
   It checks that the APK carries the native library and the sheets.
3. **The spike's APK**, `grouplab-spike-apk`, until item A4 moves the camera into the application; request 33 installs it. Both are
   Release builds kept as workflow artifacts for fourteen days.

An unsigned debug APK is never published as a nightly. The nightly's `android` job signs a release APK and an AAB for Play with the
upload key, which Alan generated and keeps outside the repository (entry 198 section 3.3) and which the job reads from the repository's
secrets; without them it builds nothing and the rest of the nightly publishes. Section 12 says what Play does with the AAB.

## 10. Running the spike on the phone

The `android` workflow's artifact `grouplab-spike-apk` is a Release build signed with the build machine's debug key, so it installs
and starts by itself; a Debug build expects Visual Studio's fast deployment and does not (entry 202). A copy built on this machine is
signed with a different debug key, so switching between the two needs `adb uninstall org.grouplab.app.spike` first.

Every line the spike shows also goes to `spike-log.txt` in its own folder, `/sdcard/Android/data/org.grouplab.app.spike/files/`, so
a sitting's measurements can be pulled over adb afterwards. **Camera** opens the capture screen of entry 219 item A2: the preview, the
one instruction, 0.6x, 1x and 3x, tap to focus and lock, the automatic shutter after three ready frames, and Take.

## 11. The application (entry 219 item A3)

`android/GroupLab.Android` is GroupLab itself, `org.grouplab.app`. **Its screens are the shared mobile project's** since entry 290:
`mobile/GroupLab.Mobile`, a plain .NET library the iOS head is being built to link, holds every Avalonia screen and the desktop files they compile, and
reaches the phone only through `IPhonePlatform`. This head keeps what is Android's own: the activity, the camera (CameraX), printing and
sharing through intents, the idle screen, the updater, and `AndroidPhone`, its answers to `IPhonePlatform`. Four places along the bottom, where a thumb reaches them: Capture,
Sessions, Targets and Settings; Back from any of the others returns to Capture, and Back from Capture leaves. Targets (entry 243 section
3.4) is the library and "Made for your optic", each sheet printed through Android's own print dialog or shared as the desktop's PDF. On a
window at least 840 dp wide (entry 243 section 3.3) a result puts the sheet beside its numbers. The first run asks what may be
shared before any of them, on one scrolling screen, in the desktop's order and words, with nothing chosen for the person; a question is
asked only while the project takes what it asks about, as on the desktop, so the survey joins when its receiver opens. Settings has the
same answers under **Sharing**, the hardware survey included, which is asked only once its receiver is open (entry 208).

It shares the desktop's code by compiling the files as they are, not by copying them: the settings file and its format
(`AppSettings.cs`), the questions' words (`SharingWords.cs`), the log, the crash records and the error report queue
(`Diagnostics/`), and the imaging code. `AndroidSharingTests` holds that on the desktop: every linked file exists, each choice's words
are written once, and the phone's first run preselects nothing. The log and the crash records go in the application's own files.

**Capture to result** (item A4, first part). **Take a picture** opens the capture screen moved in from the spike: the preview, one
instruction at a time, the lens by zoom, tap to focus, and the shutter that fires by itself. **Choose a photograph** takes one already on
the phone. Either is reduced to the working size of item A1, and that working copy becomes the session's own image, so every coordinate
in the marking is in its pixels and the session opens anywhere; the full photograph and the picker's copy are deleted. The sheet names
itself from its codes, the holes are found by the desktop's own code, and the result shows the group's figures, the photograph with a
ring on every hole, and the desktop's composite plot, filled by the same method from the same marking. The session is saved in the
desktop's own database and format, and **Sessions** lists them, newest first, to open again.

**Correcting the holes** (item A4, second part; entry 291 section 2). The photograph on the result is shown upright as the sheet is,
turned by where the registration puts the page's top rather than by the file's tags, across the whole width and no taller than itself,
and it takes no touch. **Fix holes** opens a page built on Marking A: the picture pans and pinches under a crosshair fixed in the middle,
and **Add a hole here**, or with a ring under the crosshair **Move this hole** (then **Put the hole here**) and **Remove this hole**, act
on the hole there. **Undo** takes back each change; **Done** returns to the result, which measures again and saves; leaving by **Back to
the result** or Android's back asks whether to keep the changes. The **caliber** and the **distance**
are asked on Capture before the picture, remembered for the next target; the caliber goes to detection, as on the desktop, and the
distance turns the figures into angles. Where the sheet's codes cannot be read, the result asks **which sheet it is** by name and
detects as that sheet, as the desktop does.

## 12. Google Play (entry 231)

**The first internal testing release.** Alan created the app in the Play Console, package `org.grouplab.app`, and uploaded nightly
110's AAB to internal testing on 2026-09-27. Play read it as version code 110 (0.2.0-nightly.110), Android 10 and up (API 29), target
SDK 36, arm64-v8a only, with 2 required features. The version code is the nightly's number, so every nightly's AAB is newer than the
last. Alan installed it on the Fold 7 from the Play Store and it opened to the first-run sharing window.

**The opt-in link** is https://play.google.com/apps/internaltest/4701684356677501640. Only an account on the internal testers list can
use it; anybody else sees an error page.

**Moving between the Play copy and a nightly APK.** Google re-signs what Play installs with its own app signing key, and a nightly APK
is signed with the upload key, so Android refuses to install either over the other. To move from one to the other, uninstall GroupLab
first, then install the other. Uninstalling deletes the sessions and settings kept on the phone, so move anything worth keeping off it
first.

**Play's two warnings on that release.**

1. *No deobfuscation file.* There is nothing to give it: the build does not run R8 or ProGuard on the Java side (`AndroidLinkTool` is
   not set, so the Java code is dexed by D8 unshrunk and keeps its names), so no mapping file exists. The C# code is trimmed, which
   renames nothing. The warning is Play's default and can be left.
2. *Native code without debug symbols.* `android/opencv/build-extern.sh` keeps an unstripped copy of GroupLab's own OpenCV library,
   and every nightly with an AAB puts it on its numbered release as `grouplab-<version>-android-native-symbols-<commit>.zip`, in the
   layout Play asks for (`arm64-v8a/libOpenCvSharpExtern.so`). It holds the library's symbol table, which is what turns an address in
   a native crash into a function name. The .NET runtime's libraries come from Microsoft already stripped, with their symbols on
   Microsoft's symbol server, so Play will go on noting those. Until uploads are automated the zip is uploaded by hand beside the AAB,
   under the release's **App bundle explorer**, **Downloads**, **Native debug symbols**.

**Automatic upload, planned and not started.** The same shape as the Microsoft Store (request 38): a Google Cloud service account with
release rights on this app only, invited in the Play Console; its JSON key as a repository secret that Alan adds himself; and a step
in the nightly, skipped while the secret is absent, that uploads each nightly's AAB and its symbols to internal testing through the
Play Developer API. Alan's request with the exact steps is written after the Store work of request 38 is done.

## 13. The development build, and when the Play copy is needed (entry 234)

**GroupLab Dev** is the same application built with `-p:GroupLabDev=true`: its own id, `org.grouplab.app.dev`, so it installs beside the
copy from Google Play and neither replaces the other; "GroupLab Dev" on the home screen with an icon of its own; debuggable, so
`adb shell run-as org.grouplab.app.dev` reaches its log, settings and results; and its version ends in `-dev`, which marks its error
reports and keeps its survey reports out of every published figure. Its Settings say it is the development build. Every nightly with the
upload key publishes it as `grouplab-android-dev.apk`. Nothing in the code names the package: the file provider's authority is the
application id and `.files`, from the manifest's `${applicationId}` and `SessionFiles.Authority` alike.

**Driving the phone** is one script, `scripts/android/Test-OnPhone.ps1`: it connects over wireless debugging (mDNS first, then asks for
the address), installs the newest GroupLab Dev, starts it, takes screenshots, and pulls its own log and the logcat into
`C:\Dev\grouplab-local\android-<build>\`. The phone's address is never written to a file.

**Logcat.** Every line the application logs above DEBUG also goes to logcat under the tag `GroupLab`, scrubbed as the log file is: no
path, no file name, no location. That is how a problem on a tester's phone from Google Play is read, since that copy is not debuggable.

**Look and icon** (entries 246 and 248): the phone uses the desktop's styles and tokens with look B's cards on top of them, as DESIGN.md
section 19 describes, and its icons, GroupLab's dark and GroupLab Dev's light, adaptive with a monochrome layer and PNGs for older
launchers, are written by `scripts/android-icons.py` from the desktop's mark.

**A picture without the picker** (entry 246): GroupLab Dev reads a picture named by the extra `org.grouplab.test.picture`, a file name in
its own `files/test` folder, exactly as a chosen photograph, so a device is measured without opening the system's picker, which shows the
owner's own pictures. Put the file there with `run-as org.grouplab.app.dev`, then `am start -n <its activity> --es
org.grouplab.test.picture <name>`; the log's `phone.prepare` and `phone.detect` lines give the working size, the most memory held and each
stage's time. The release build has no such way in.

**Scenario files** (entry 315 section 2), in GroupLab Dev on Android and iOS alike: a JSON file of steps (open a place, read a picture
from the scenario folder, wait for a screen or words, press a button by its name or words, type, take a screenshot, write the visible
controls as a tree, sleep, copy the log) run with nobody's hands. It waits in `files/scenario/scenario.json`, or is named by the extra
`org.grouplab.test.scenario` with a file name in that folder; it runs once, and `files/scenario/results/` gets `results.json` (each step,
whether it worked, its time and the memory held), the screenshots, the trees and the log. The format and its steps are described in
`mobile/GroupLab.Mobile/Dev/Scenario.cs`. The older extras stay as shortcuts. The Play build is built without any of it.

**The automation bridge** (entry 315 section 1), in GroupLab Dev only: a small command server on the device's own 127.0.0.1, port 47315,
reached over the cable with `adb forward` (or `pymobiledevice3 usbmux forward` on iOS). Every scenario step is a command, one line of
JSON each way, with the same code behind it, and the steps now include going back, reading a picture through to its result, choosing
in a list or turning a check box on or off, scrolling, setting one value in the settings file, and a reset that takes away the settings
and the sessions so the next run starts as a first run (a sitting's kept pictures stay). The bridge also answers the controls showing,
the newest log lines, the lines that carry a time, a screenshot and the memory held. A request needs this run's key, made at random when
it starts, shown in Settings, About and written to `files/bridge/key`; a switch there turns it off. A request without the key, or longer
than 64 KiB, is refused and its connection closed. The controls a script needs carry automation ids that do not change with their words
(`tab-capture`, `capture-take-picture`, `result-fix-holes`, `fix-done`, `settings-send-diagnostics` and the rest, which a test holds).
`scripts/app-bridge.py --platform android` forwards the port, reads the key, can copy a picture into the scenario folder first with
`--push`, and sends one command. Not yet: the live camera cannot be driven (a clip or a picture can be replayed through it instead, below), the system's own pickers,
share sheet and permission questions are outside the application and out of its reach, and there is no debug overlay or per-stage
timing beyond what the log already says (section 4). The Play build is built without any of it.

**The replay camera** (entry 315 section 3), in GroupLab Dev only, on Android and iOS alike: the capture screen takes its frames from a
recorded clip or a picture instead of the camera, and its level's readings from the clip instead of the gravity sensor, so Guided, the
words, the level, the torch on Auto and the shutter's timing can be tested by script against every build. Nothing else changes: both
phones' cameras hand each frame to the same shared code (`mobile/GroupLab.Mobile/CameraJudge.cs`), which is what a replay runs too. The
preview and the torch are still the camera's; the shutter takes the frame showing and hands it on to be read as a picture.

- *The clip format*, one for both phones, so a clip recorded on either tests both: a folder holding `clip.json` and one greyscale JPEG
  a frame, the luminance the guidance judges. `clip.json` has `"format": "grouplab-camera-clip"`, `"version": 1`, the device, the
  time recorded, the still's size (`picture`) and the analysis frame's (`stream`), and `frames`, each with its `file`, its `ms` from
  the first frame, the level's `gravity` in the screen's axes, the torch's level (`torch` of `torchOf`) and its mean `light`. A
  replay hands each frame on with its recorded time, never sooner and never skipping one, so the waits are the recording's on any
  device. A picture on its own is played as a clip: the same picture again and again at the Fold 7's pace, the phone flat.
- *Recording*: the switch under About in GroupLab Dev's Settings, or the scenario step `{ "do": "record", "seconds": 6 }`
  (`"every"` keeps every Nth frame, `"width"` makes them smaller, `"on": false` turns it off). While it is on, each time the camera
  closes, taken or not, its last few seconds go into a new folder under `files/clips/` (`Documents/clips/` on iOS), named for the time.
  The frames are kept at the stream's own size unless asked smaller, because a smaller frame reads fewer of the sheet's markers and
  would replay a camera that saw less. Clips stay on the device: they are not uploaded, not in the diagnostics file, and never
  committed. Copy one off with `adb exec-out run-as org.grouplab.app.dev tar c files/clips > clips.tar` into `C:\Dev\grouplab-local\`.
- *Replaying*: copy a clip's folder (or a picture) into `files/clips/` or the scenario folder, then run the scenario step
  `{ "do": "replay", "clip": "20260930-101500", "seconds": 60 }` (`"manual": true` for Manual, `"loop": true` to play it round again).
  It opens the camera, waits until the shutter fires or the clip has played through, and writes `replay-<name>.json` to the results:
  every frame's words, its own instruction, the ring round the shutter and any torch change, and whether and where it was taken.
  `{ "do": "replay" }` with no clip puts the live camera back. The bridge takes the same two steps.
- *In the tests*: `tests/GroupLab.Mobile.Tests/CameraReplayTests.cs` records clips made from the committed sample, reads them back and
  replays them through the same code, and holds that a steady clip is taken once its frames have been ready long enough and an unsteady
  one never is.

**Every picture of a sitting is kept** (entry 291 section 7.5), in GroupLab Dev only: each picture the camera takes goes into its own
folder under `files/sitting/`, numbered `picture-0001` on, as `picture.jpg` with every metadata segment taken out (no location, no time,
no camera settings; the picture's own data is copied, not re-encoded), `live.txt` with what the last live frame read before it (the
markers read and foretold, the module size, the room beyond the printing, the shake, and the frame's and the picture's size and crop), and
`analysis.txt` with the analysis's trace once the picture is read. Nothing is sent anywhere, and the release and Play builds keep nothing.
A whole sitting comes off at once with `adb exec-out run-as org.grouplab.app.dev tar c files/sitting > sitting.tar`, into
`C:\Dev\grouplab-local\`, never the repository. The switch under About in GroupLab Dev's Settings turns it off and deletes what was kept.

**The Play copy is needed only to test the Play path itself:** once before the closed test begins, and whenever the release build changes
in a way the development build would not show (signing, the store's app bundle, what Play strips or adds). Day to day, testing is on
GroupLab Dev, installed over adb.

## 14. The memory budget, scaled to the device (entry 240)

Entry 206's single number is replaced by a rule, `MemoryBudget.Phone`. Before each analysis the application asks Android what it has
(`ActivityManager.getMemoryInfo`: total, available, the low memory threshold and whether memory is low now) and takes **a quarter of what
is available above the threshold**, never under the **floor of 400 MB** (today's 4 GB phone at 8 megapixels) and never over **a tenth of
the device's memory**, well inside where Android would stop GroupLab or push other applications out; when the device says memory is low,
the floor. The memory classes are logged beside it. Worked out with a typical 45 percent available:

| Device | Memory | Available | Budget |
|---|---|---|---|
| Essential PH-1 | 4 GB | 1.8 GB | 400 MB, the floor |
| Galaxy S20 | 8 GB | 3.6 GB | 819 MB, the ceiling |
| Galaxy Z Fold 7 | 12 GB | 5.4 GB | 1,229 MB, the ceiling |
| Galaxy Tab S8 Ultra | 16 GB | 7.2 GB | 1,638 MB, the ceiling |

**The extra memory buys nothing, so it is not spent.** An analysis costs about 260 MB and 14.25 MB a megapixel (the Fold 7's 373 MB at 8
and 715 at 32). Measured on this machine at each working size: the published scan's shots moved about 0.0005 in between 8 and 24
megapixels, and on the three kitchen photographs of entry 233 the 8 and 12 megapixel readings agreed to 0.001 in, against a photo to scan
difference of 0.015 to 0.027 in, with the odd extra mark going either way. So **every phone works at 8 megapixels**
(`MemoryBudget.PhoneWorkingMegapixels`: the floor holds 9.8), and more memory never makes two phones' results differ. **On the desktop**,
which works at full size, memory only decides where it must stop: an image larger than half the computer's memory holds at the same costs
is refused with that reason, below the fixed 400 megapixel cap (`MemoryBudget.DesktopMostMegapixels`); with 12 GB or more that is the cap
itself. How much time the phone's 50 s spends where waits on the devices (entry 239).

## 15. Device sittings: the black idle screen, and never a lit screen left waiting (entry 268)

Alan: "both my phone and tablet have OLED screens and I don't like keeping them on at the risk of burn in." So, in every device sitting:

- **When Code is not driving a device for more than about a minute, it shows GroupLab Dev's black idle screen** over adb:
  `adb shell am start -n org.grouplab.app.dev/<main activity> --es org.grouplab.test.idle 1`. It is pure black, with the status and
  navigation bars hidden by ordinary immersive mode; an OLED screen's black pixels are off, so nothing can burn in, and the device stays
  awake and unlocked for adb. It is never screen pinning, kiosk or lock task: Home, Back and Recents leave it like any application, and a
  tap shows one dim line, "GroupLab Dev idle screen, used for overnight testing", and a Close button of 48 dp for four seconds.
- **The screen shows real content only while work is being done**, batched, and never a static screen (a result, Settings, the camera)
  while waiting. Brightness is never raised; screenshots over adb do not depend on it.
- **At the end of the night**, the idle screen is left up and `docs/notes/for-alan.md` says the devices can be picked up. Stay awake is
  not turned off and the device is not locked, so the next session can reach it; Alan turns Stay awake off.
- `scripts/device-capture-check.py` ends on the idle screen.

## 16. The torch's strength (entry 262)

Fold 7 (SM-F966U1): Android 16, SDK 36; back camera 0: flash available, strengthMaximumLevel 5, strengthDefaultLevel 1. Read 2026-09-28 11:22 UTC from dumpsys media.camera static metadata. The Tab S8 Ultra's reading follows at the next connection.
Whether the level can be set during a camera session, and how long exposure takes to settle after each change, are measured with GroupLab
Dev in a later build.

Entry 302 uses the levels: the torch on Auto starts at level 1 and steps up and down during the session (docs/MOBILE-CAPTURE.md section 9),
through CameraX's own torch strength setting where the phone reports it supported, and on and off elsewhere. `camera.start` logs the
levels offered and the default; each `camera.torch` line the level chosen and why, which answers at the next sitting whether the level
takes effect while the camera runs.

## The shutter, from the press to the result (entry 283)

Alan found a long wait between pressing the shutter and the phone answering. Every step is now in GroupLab Dev's log as `camera.shutter`
with its milliseconds from the press: `press`, `requested`, `exposed` (the sensor started, the moment of the shutter sound on the phone's
own camera), `saved`; then `phone.prepare` (the working copy), `phone.detect` (the sheet read), `analyzed`, and `shown` (the first result on
the screen). The press answers at once with the shutter sound and a white flash, the live analysis stands aside until the picture is
saved, and the picture is taken in CameraX's minimum latency mode rather than maximum quality. The target is a sound and a flash within
about 0.3 s, and the result as soon as the detector allows.

    python scripts/shutter-timing.py --serial <adb serial> --presses 20 --mode manual --capture latency
    python scripts/shutter-timing.py --serial <adb serial> --presses 20 --mode manual --capture quality

The script presses the shutter through GroupLab Dev's own path, waits for each result, and prints the median and slowest milliseconds of
each step as a table for this section. **Not measured yet:** it needs the build that carries the timings, on the Fold 7 and the Tab S8
Ultra, torch off and on, Guided and Manual, at the next sitting.


## 17. GroupLab Dev updates itself (entry 288)

GroupLab Dev keeps itself on the newest nightly with no adb and no file to open: it checks the signed manifest at launch and about every
six hours, downloads on Wi-Fi in the background, checks the SHA-256 and that the APK is signed like the installed copy, and installs
through a PackageInstaller session, never while the camera is open, a sheet is being read or a change is unsaved. `docs/UPDATES.md`, "On
Android", has the whole of it. The code is `AndroidUpdates` in Core, which holds the rules and their tests, and the `Updates` folder of this
project, compiled only with `-p:GroupLabUpdater=true`, which only GroupLab Dev's APK has.

**The first update of a copy installed by adb needs one tap**, because adb, not GroupLab, is its installer of record; after it GroupLab is.
On Android 12 and later every update after that is meant to install without a tap once GroupLab has left the screen. Whether the Fold 7
and the Tab S8 Ultra, both on Android 16 (API 36), do so is recorded here after the first nightly that carries the updater has been installed over adb and
the one after it has arrived by itself: the first prompt, whether the second is silent, that sessions and settings are kept, and the time
from publishing to installing (the log's `update.installed` line carries `minutesFromPublish`).

**The first self-update, measured on the Tab S8 Ultra (Android 16) on 2026-09-29.** Nightly 125 was installed over adb (installer of record:
none). Nightly 126 was published at 10:00 UTC; at the next start GroupLab Dev found it, downloaded its 44,884,287 bytes over Wi-Fi in
2 seconds, and checked the hash and the signing certificate. It then showed its one sentence about "Install unknown apps" with "Open the
setting", which opened Android's page for GroupLab Dev; after the permission was allowed, the next start handed the file to Android, which
asked "Do you want to update this app?" once. One tap on Update installed nightly 126 in place at 10:56 UTC, 56 minutes after publishing
(most of that the time until the app was next opened). The sessions were still there afterwards, and the installer of record became
GroupLab Dev itself, which is what lets the next update go without a tap. **Found on the way:** Android asking for the tap was treated as a
failure, so the pending version was forgotten and the "Updated to nightly N" notice did not appear after the tapped install; fixed for
nightly 127. **The second update, nightly 126 to 127 on the tablet:** found at start, 44.9 MB in 2 s, and when GroupLab left the screen it handed
the file to Android with "no tap needed", as GroupLab was now its own installer of record; Android still answered that it wanted a tap,
and from the background it could not show one, so the update waited for the next start. Android 12 and later honor "no tap needed" only
from an app that also declares `UPDATE_PACKAGES_WITHOUT_USER_ACTION`; GroupLab Dev did not. It declares it from nightly 128. **Nightly 126 to 128** still needed a tap, because it is the installing build's own declaration
that counts and 126 had none; asked from the background, Android showed nothing, and every later start tried the silent way again, so
the tap was given through the notice's "Install now" (Android's one question, then Update). Nightly 128 on the tablet holds the
permission (granted at install) and is its own installer of record, so 128 to 129 is the no-tap test. From 129 a tap Android wants in
the background is remembered and asked for on screen at the next start.

**The Fold 7 (Android 16), from its own log.** Nightly 125 over adb; at 11:00 UTC its first self-update to 126 asked once and was
tapped. At 18:16 UTC the six-hourly background check (WorkManager, `worker=True`) found nightly 129 by itself and downloaded it in
1 second; 126 did not yet declare the permission, so Android asked and the tap was given 24 seconds later. The Fold is now on 129,
holding the permission and its own installer of record, so its next update is the no-tap one. Alan confirmed it updated itself
without adb (entry 303). Before the first self-update Android made him wait for Google Play Protect's scan of the new copy, as it does
for any app from outside the Play Store; it is quick, and the user guide now says so.

**adb stays for tests and logs only.** Installing a nightly over adb is still how a sitting starts on a device that has no updater yet.

## 18. Pictures from any photo app (entry 292)

Alan: "It is important that it can access cloud stored photos and not just ones local to the phone." Before entry 292, **Choose a
photograph** opened the documents picker, which reached Drive and Images and nothing else. Now there are three ways in, and none of them
asks for a storage permission, because the picker or the app grants GroupLab the one picture chosen and nothing more:

1. **Choose a photograph** opens the system photo picker, images only (`PickVisualMedia` from AndroidX Activity). On a phone where Google
   Photos is the cloud media app it shows cloud-only photos too. It is built into Android 13 and later and added to Android 11 and 12 by
   Google Play system updates; the manifest asks Google Play services to add it to Android 10.
2. **From another app** opens a chooser of every app that answers `ACTION_GET_CONTENT` for `image/*`, by name: Google Photos, Samsung
   Gallery, the maker's own gallery (Xiaomi and Redmi, OPPO and OnePlus, realme, vivo, Honor, Nubia and RedMagic, Motorola, Huawei), Drive,
   OneDrive, Dropbox and Files. This is how a photo editor reaches them.
3. **Receiving:** GroupLab and GroupLab Dev answer `ACTION_SEND` and `ACTION_SEND_MULTIPLE` for `image/*`, and `ACTION_VIEW` and
   `ACTION_EDIT` for `image/*`, so they are in every app's share sheet, Open with and Edit with. GroupLab reads the picture and never writes
   it back. Several shared at once are read one after another as a set, one per sheet, and the last one's result leads to the set.

**A photograph kept only in the cloud** is fetched through its `content://` stream a piece at a time on a worker thread, with a line saying
which app it comes from where Android says so ("Getting the photo from Google Photos, 2.1 of 6.4 MB") and a Cancel that returns at once,
even from a download that has stalled. A fetch that fails says so, and says the phone is offline where Android's connectivity service
says it is (its permission is already in the manifest, merged in from the AndroidX libraries), with what to do instead. **The whole
photograph, always:**
the size received is compared with the size the app states (the `width` and `height` columns, asked for by name, never a whole row) or,
where it states none, the size the camera recorded in the picture (EXIF `PixelXDimension` and `PixelYDimension`); a smaller copy is said to
be one before it is read, with the way to the whole one. The picture is stood upright from its own orientation, as a camera picture is, its
location is never read, and the copy in the cache is deleted once the session has its working copy. `PhotoIntake` in the shared project
holds these decisions and `PhotoIntakeTests` tests every path off the phone; `PhotoPickers` in this project opens the pickers.

**Phones without Google Play services** (Huawei, and phones sold with Chinese-market software). Where the photo picker is missing, Choose a
photograph goes straight to the apps chooser, never to an error. Nothing else GroupLab needs depends on Google Play services, checked
against the packages the build restores on 2026-09-29: the camera is CameraX over Camera2, OpenCV is built into the application, SQLite is
the bundled `e_sqlite3`, and the updater's WorkManager runs on Android's own JobScheduler. The one use of Google Play services is the photo
picker's backport to Android 10, and a phone without it gets the apps instead. The updater also checks at every launch, not only in the
background (`SelfUpdate.Launched`, from `App`), so a battery manager that stops background work delays an update and never loses one.
