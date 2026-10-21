---
title: "OpenCV's marker and QR detectors on Android, from .NET"
description: "In September 2026 no published runtime gave a .NET application on Android OpenCV's ArUco markers and WeChat QR detector. Building OpenCvSharp's native half yourself is one script, and two of its traps cost a phone run each."
group: How GroupLab is built
number: 40
written: 2026-10-21
data_date: "2026-09-25, and every Android build since"
samples: "one build script, its first run on a Galaxy Z Fold 7 on 2026-09-25, and GroupLab's Android builds since, which carry the same library"
state: published
no_figure: "The article is about a build; its findings are a list of steps and two measured runs, given as text and a table."
found: "OpenCvSharp's own runtimes stop at Windows, Linux and macOS, and the one community build for Android leaves out the homography, the markers and the QR codes. A static build of OpenCV 4.13.0 with ten modules, linked into OpenCvSharp's native library, runs the same code on a phone as on the desktop."
sure: "What the runtimes offered was checked on 2026-09-25 and may have changed since. The phone timings are one phone, one sample scan and one range photograph."
sources:
  - "The build: `android/opencv/build-extern.sh`, run by `.github/workflows/android.yml` and the nightly's Android job; the iOS twin `ios/opencv/build-extern.sh`."
  - "The routes compared, the calls GroupLab makes and the phone's first timings: `docs/ANDROID.md`, sections 3 and 5 (2026-09-25)."
  - "The first run on the phone and the binding that compiled to nothing: `docs/notes/archive/results-201-225.md`, \"Entries 201 and 202\"."
---

## What was missing

GroupLab is written in C# and reads targets with OpenCV through OpenCvSharp, a .NET wrapper whose native half, `libOpenCvSharpExtern`, holds OpenCV and a C interface to it. On the desktop that native half comes ready made. On Android, when we looked on 25 September 2026, it did not:

| Route | What it gave |
|---|---|
| OpenCvSharp's official runtimes | Windows, Linux and macOS only. |
| The one community "mini" runtime for android-arm64 | core, imgproc, imgcodecs and dnn: no homography, no markers, no QR. |
| Rewriting the calls in managed code | The WeChat QR detector, the marker detector and a robust homography would each be a project, and a second engine to keep in step with the first. |

GroupLab needs about thirty OpenCV functions across core, imgproc, imgcodecs, calib3d and objdetect, and two contrib modules: aruco, for the AprilTag 36h11 markers in a sheet's corners, and wechat_qrcode, the QR detector that finds a sheet's codes, used without its neural network models but needing dnn to build. So the only route that kept one engine on every platform was to build the native half ourselves.

## The build

The whole of it is one script, `build-extern.sh`, run in CI and cached until the script changes. In order:

1. **Pin the versions.** OpenCV and opencv_contrib at 4.13.0, OpenCvSharp at 4.13.0.20260627. The managed package and the native library must come from the same tag, or calls fail at run time with nothing at build time to warn you.
2. **Build OpenCV static, for arm64-v8a at API 24**, with the NDK's toolchain, the C++ runtime linked statically, and only the modules needed: core, imgproc, imgcodecs, calib3d, features2d, flann, objdetect, dnn, aruco and wechat_qrcode. Third party libraries are built from source, and Java, examples, tests and video are off.
3. **Cut OpenCvSharp's bindings to the same modules.** Its CMake file compiles every binding it has. The script narrows that to the modules' own files and defines the switches that leave out highgui, video, stitching, ml and the contrib modules.
4. **Put the two contrib modules back by name**, their headers included after the contrib switch.
5. **Link OpenCV into one `libOpenCvSharpExtern.so`**, aligned for 16 KB pages, which Android 15 and Google Play ask of every native library.
6. **Check it.** The script fails if any loadable segment is not 16 KB aligned, prints every library it needs from the system (anything beyond Android's own and it will not load on a phone), and keeps an unstripped copy beside the stripped one, because Play turns a native crash's addresses into names only with those symbols.

OpenCV, opencv_contrib and OpenCvSharp are Apache-2.0, which a GPL-3.0 application may combine.

## The two traps

**A binding that compiles to nothing.** The first build went onto the phone and stopped at "EntryPointNotFoundException: wechat_qrcode_create1". The aruco and wechat_qrcode bindings' own headers sit inside the same switch that leaves out the other contrib modules, so with it set they compiled to empty files and the build succeeded without them. The script now lifts that switch in those two headers only, and asserts that it found exactly one to lift, so a new OpenCvSharp tag that moves it fails the build instead of the phone.

**A debug APK does not start by itself.** A .NET Android debug build expects Visual Studio's fast deployment and aborts with "No assemblies found" when installed alone. Measure on a Release build, which also makes the times comparable with the desktop's.

## On the phone

The first Release build on a Galaxy Z Fold 7's cover screen, 25 September 2026, ran the desktop's own code on the published 600 dpi sample scan:

| | the phone | the desktop |
|---|---|---|
| the sample scan, at its full 32 megapixels | 25 of 25 holes, 17.1 s, peak 714 MB | 25 of 25 holes, 7.9 s |
| the same scan worked at 8 megapixels | 25 of 25 holes, 3.3 s, peak 373 MB | |

The phone was a little over twice the desktop's time, nearly all of it in marking the holes; decoding and reading the codes were close to the desktop's. GroupLab's phone now works at 8 megapixels, which is where the second row comes from. The same library has shipped in every Android build since, and the same script's twin builds it for iOS as a static framework.

## What this means

**If you are building a .NET application on Android that needs OpenCV's contrib modules,** you do not need a second engine or a rewrite: build OpenCvSharp's native half with the modules you call, from the same tag as the managed package. Expect two things to bite: a binding that compiles to nothing behind a switch, which only a call on the device reveals, and the 16 KB page alignment Play now asks for. Check both in the build script, not on the phone.
