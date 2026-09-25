#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 198 section 2.2: OpenCV and OpenCvSharp's native half, built for android-arm64.
#
# OpenCvSharp publishes native libraries for Windows, Linux and macOS, not Android. The one community build for Android, Sdcb's
# "mini" runtime, carries core, imgproc, imgcodecs and dnn only, and GroupLab also needs calib3d (the homography), objdetect (the plain
# QR decoder), aruco (the markers) and wechat_qrcode (the QR detector that finds the codes). So this builds the same way Sdcb's
# pipeline does (NDK, API 24, static C++ runtime, static OpenCV linked into one libOpenCvSharpExtern.so), with GroupLab's modules, and
# compiles only the OpenCvSharp bindings for them. OpenCV, opencv_contrib and OpenCvSharp are Apache-2.0, which GPL-3.0 accepts.
#
# Usage: build-extern.sh <work folder> <output folder>. Needs cmake, make, git and an NDK in ANDROID_NDK (or ANDROID_NDK_HOME).
set -euo pipefail

OPENCV_VERSION=4.13.0
# The managed OpenCvSharp4 package GroupLab references; the native half has to be built from the same tag.
OPENCVSHARP_VERSION=4.13.0.20260627
ABI=arm64-v8a
API=24
MODULES=core,imgproc,imgcodecs,calib3d,features2d,flann,objdetect,dnn,aruco,wechat_qrcode

WORK=$(realpath -m "${1:?work folder}")
OUT=$(realpath -m "${2:?output folder}")
NDK=${ANDROID_NDK:-${ANDROID_NDK_HOME:?ANDROID_NDK or ANDROID_NDK_HOME must name an NDK}}
TOOLCHAIN="$NDK/build/cmake/android.toolchain.cmake"
mkdir -p "$WORK" "$OUT"
cd "$WORK"

[ -d opencv ] || git clone --quiet --depth 1 -b "$OPENCV_VERSION" https://github.com/opencv/opencv opencv
[ -d opencv_contrib ] || git clone --quiet --depth 1 -b "$OPENCV_VERSION" https://github.com/opencv/opencv_contrib opencv_contrib
[ -d opencvsharp ] || git clone --quiet --depth 1 -b "$OPENCVSHARP_VERSION" https://github.com/shimat/opencvsharp opencvsharp

cmake -S opencv -B opencv-build -Wno-dev \
  -D CMAKE_BUILD_TYPE=Release \
  -D CMAKE_TOOLCHAIN_FILE="$TOOLCHAIN" -D ANDROID_ABI=$ABI -D ANDROID_PLATFORM=android-$API -D ANDROID_STL=c++_static \
  -D CMAKE_INSTALL_PREFIX="$WORK/opencv-install" \
  -D OPENCV_EXTRA_MODULES_PATH="$WORK/opencv_contrib/modules" \
  -D BUILD_LIST=$MODULES \
  -D OPENCV_FORCE_3RDPARTY_BUILD=ON -D BUILD_SHARED_LIBS=OFF \
  -D BUILD_EXAMPLES=OFF -D BUILD_DOCS=OFF -D BUILD_TESTS=OFF -D BUILD_PERF_TESTS=OFF -D BUILD_JAVA=OFF \
  -D BUILD_ANDROID_EXAMPLES=OFF -D BUILD_ANDROID_PROJECTS=OFF \
  -D WITH_PROTOBUF=ON -D WITH_QUIRC=ON -D WITH_ADE=OFF -D WITH_FFMPEG=OFF -D WITH_GSTREAMER=OFF -D WITH_OPENEXR=OFF \
  -D OPENCV_ENABLE_NONFREE=OFF
cmake --build opencv-build --parallel "$(nproc)"
cmake --install opencv-build > /dev/null

# The bindings: OpenCvSharp's own list of every module is cut to GroupLab's. NO_CONTRIB leaves out a dozen contrib modules GroupLab
# never calls, and with them aruco and wechat_qrcode, which it does; those two are put back by name.
EXTERN=opencvsharp/src/OpenCvSharpExtern
python3 - "$EXTERN" <<'PY'
import sys
from pathlib import Path
extern = Path(sys.argv[1])
cmake = extern / "CMakeLists.txt"
text = cmake.read_text()
files = "core*.cpp imgproc*.cpp imgcodecs*.cpp std*.cpp calib3d*.cpp features2d*.cpp flann*.cpp objdetect*.cpp dnn.cpp aruco*.cpp wechat_qrcode*.cpp"
assert "file(GLOB OPENCVSHARP_FILES *.cpp)" in text
text = text.replace("file(GLOB OPENCVSHARP_FILES *.cpp)",
                    f"file(GLOB OPENCVSHARP_FILES {files})\nadd_compile_definitions(NO_HIGHGUI=1 NO_VIDEO=1 NO_STITCHING=1 NO_ML=1 NO_CONTRIB=1)")
cmake.write_text(text)
header = extern / "include_opencv.h"
text = header.read_text()
anchor = "#endif // NO_CONTRIB"
assert anchor in text
text = text.replace(anchor, anchor + "\n#include <opencv2/aruco.hpp>\n#include <opencv2/aruco/charuco.hpp>\n#include <opencv2/dnn.hpp>\n#include <opencv2/wechat_qrcode.hpp>\n", 1)
header.write_text(text)
PY

cmake -S opencvsharp/src -B opencvsharp-build -Wno-dev \
  -D CMAKE_BUILD_TYPE=Release -D CMAKE_POLICY_VERSION_MINIMUM=3.5 \
  -D CMAKE_TOOLCHAIN_FILE="$TOOLCHAIN" -D ANDROID_ABI=$ABI -D ANDROID_PLATFORM=android-$API -D ANDROID_STL=c++_static \
  -D OpenCV_DIR="$WORK/opencv-install/sdk/native/jni"
cmake --build opencvsharp-build --parallel "$(nproc)"

LIB=opencvsharp-build/OpenCvSharpExtern/libOpenCvSharpExtern.so
"$NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-strip" --strip-unneeded "$LIB"
mkdir -p "$OUT/$ABI"
cp "$LIB" "$OUT/$ABI/"
# What the library needs from the system: nothing beyond Android's own libraries, or it will not load on a phone.
"$NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-readelf" -d "$OUT/$ABI/libOpenCvSharpExtern.so" | grep NEEDED
du -h "$OUT/$ABI/libOpenCvSharpExtern.so"
