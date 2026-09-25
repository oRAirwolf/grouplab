# GroupLab on Android

NOTES-FROM-PLANNING.md entries 198 and 199, 2026-09-25. This is the plan and what the first stage found. The first stage proves the
desktop's engine runs on the phone before any screen is designed; it ends with a report of how fast, and the rest of this document is
the plan that report decides.

**Where it stands.** The spike and its build are written: `android/GroupLab.Android.Spike/`, `android/opencv/build-extern.sh` and
`.github/workflows/android.yml`. Whether the native library builds and the APK is made is what the first CI run of the `android`
workflow shows. **Nothing has run on a phone yet**: that waits on requests 25 (the Android workload and SDK on Alan's machine) and 26
(the Fold 7 paired for debugging) in `docs/notes/for-alan.md`.

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
- **A public Play listing waits on the attorney's review** of the draft GPL section 7 additional permission for app stores. Internal and
  closed testing can go ahead before it.
- **iOS is not planned.** The iPad Mini is for testing the website only.

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

If a MOBILE-CAPTURE.md item proves impossible on Android, it is named here with the reason. None is known yet.

## 5. Speed and memory

**The desktop, for comparison**, the spike's own code (`SpikeRun.Run`) on Alan's machine, 2026-09-25, a Release build:

| Image | Size | Load | Codes and naming | Marking | Holes | Peak memory |
|---|---|---|---|---|---|---|
| The published sample scan, 600 dpi | 4958 by 6458 | 0.4 s | 1.2 s, 2 codes | 6.2 s | 25 of 25 | 732 MB |
| A range photograph from 2026-09-20 | 4000 by 3000 | 0.07 s | 0.9 s, 1 code | refused: no markers found | | 440 MB |

The photograph's refusal is the desktop's own result on that picture, not an Android one; the spike runs it to time the stages a
photograph goes through.

**On the phone**: not measured yet (requests 25 and 26). The spike runs the same images; any photograph pushed into its folder is run as
well. **The number to watch is memory**: a phone kills an application over a few hundred megabytes more readily than it slows it down,
and 732 MB for a 600 dpi Letter scan is more than a phone application should hold. If the phone refuses it, the capture is processed at
a capped resolution, which MOBILE-CAPTURE.md section 5 already does for the quality score.

## 6. The lowest Android version: 7.0 (API 24)

The native build is made for API 24, as Sdcb's is, and that sets the floor: CameraX and .NET 10 both go lower. Android 7.0 is from
2016, and Google's device share figures, read in Android Studio, will say what share it leaves out when the first build is published.

## 7. Phones, foldables and tablets, touch first (entry 199 section 1)

- **Layout by the width available, not the device.** Compact, under 600 dp: a phone, and the Fold 7's cover screen. Medium, under
  840 dp: the Fold 7 open, small tablets. Expanded: the Tab S8 Ultra, and any landscape tablet. One screen rearranges; the spike already
  stacks its two panels when compact and puts them side by side otherwise. The desktop keeps its own layout.
- **Folding and turning are ordinary events.** The activity declares that it handles size, orientation, density and layout changes
  itself, so it is not destroyed and the photograph, marks, zoom, selection and a half-finished edit stay where they are while the view
  lays itself out again. The test to write when there is a review screen: a review in progress survives compact to medium and back.
  **The hinge**: Android reports it through Jetpack WindowManager (`Xamarin.AndroidX.Window`), not through Avalonia, so a split layout
  reads it there and keeps controls off it.
- **Density.** Avalonia works in density independent units and draws at the screen's real density, which the spike reports as pixels
  a dp. **The system font size is not followed by Avalonia on its own**: Android gives it as a font scale, and the application applies
  it to its text. The spike shows whether the largest accessibility sizes clip.
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
2. **The marks QR code, no account.** A photograph cannot go in a QR code; the marks can. The shots in sheet coordinates, the sheet's
   id, the caliber, the distance, the load and the review choices, in a compact binary frame of the kind `Gltd/Binary` already writes.
   Worked out, not yet measured: a shot takes about five bytes, so a 25-shot session with its load is a few hundred bytes compressed,
   well inside a mid-sized code. How large a code a phone reads off a laptop screen at arm's length is measured on the Fold 7; above
   that, the application says to share the file instead, rather than showing several codes in turn.
3. **The pairing QR code, no account, whole sessions.** The desktop shows a code holding a one time key and its local address; the
   phone sends the session, photograph included, over the local network, and only something holding the key is accepted. Windows asks
   once whether GroupLab may take connections on private networks; the application says why before that prompt appears. On different
   networks, or a range network that keeps devices apart, it says so and offers the file route.
4. **Stage B, a sync folder: doubtful on Android, to be tried on the Fold 7 before it is planned.** Picking a folder on Android uses the
   Storage Access Framework's folder picker, and the Google Drive and OneDrive applications offer single files to it but, as far as I
   know, not whole folders. If that holds, Stage B works on the desktop side only. Their conflict handling is also their own: an edit
   made offline on both sides comes back as two files, which the revision rule above would at least catch.
5. **Stage C, sign in, only if B falls short**: the Drive application data folder and OneDrive's application folder, through an OAuth
   client and an Entra registration Alan would create. Apple is out.

This is not part of the first stage; it is built when the application has sessions to move.

## 9. Builds

`.github/workflows/android.yml`, on every push to `main` that touches Core, the imaging code, the sheets or `android/`:

1. **OpenCV for android-arm64**, from the script, cached until the script changes.
2. **The spike APK**, a debug build, kept as a workflow artifact for fourteen days. It checks that the APK carries the native library,
   the sheets and the sample.

An unsigned debug APK is never published as a nightly. A signed release APK and an AAB for Play need the upload key, which Alan
generates and keeps outside the repository (entry 198 section 3.3); the commands and secret names are written when the release build
needs them.

## 10. Running the spike on the phone

Once requests 25 and 26 are done, with the APK from the `android` workflow's artifact:

```powershell
C:\Dev\tools\android-sdk\platform-tools\adb.exe install -r grouplab-spike-debug.apk
C:\Dev\tools\android-sdk\platform-tools\adb.exe logcat -c
C:\Dev\tools\android-sdk\platform-tools\adb.exe logcat -s GroupLabSpike
```

Open GroupLab spike on the phone, fold and unfold it, turn it, and press Run detection. Each size the screen takes and each image's
times appear on screen and in the log. A photograph is added with `adb push` into
`/sdcard/Android/data/org.grouplab.app.spike/files/`.
