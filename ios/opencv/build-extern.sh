#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 290 section 2 item 2: OpenCV and OpenCvSharp's native half, built for iOS.
#
# The same modules, versions and bindings as android/opencv/build-extern.sh, built static, since an iOS application loads no library of
# its own from outside itself. For each of two slices, the device (iphoneos arm64) and the simulator on Apple silicon (iphonesimulator
# arm64), both with iOS 26 as the lowest version, this builds OpenCV as static libraries, then OpenCvSharpExtern as a static library, and
# merges them with every third party library OpenCV built into one archive. The two archives go into OpenCvSharpExtern.xcframework, which
# the iOS head links and calls through __Internal. OpenCV, opencv_contrib and OpenCvSharp are Apache-2.0, which GPL-3.0 accepts.
#
# Usage: build-extern.sh <work folder> <output folder>. Runs on macOS with Xcode 26, cmake, git and python3.
# Writes OpenCvSharpExtern.xcframework.zip, its .sha256 and link-frameworks.txt (the Apple frameworks OpenCV asks to be linked with).
set -euo pipefail

OPENCV_VERSION=4.13.0
# The managed OpenCvSharp4 package GroupLab references; the native half has to be built from the same tag.
OPENCVSHARP_VERSION=4.13.0.20260627
IOS_MIN=26.0
MODULES=core,imgproc,imgcodecs,calib3d,features2d,flann,objdetect,dnn,aruco,wechat_qrcode
# Entry points GroupLab's own code reaches through OpenCvSharp, one or more for each module it uses, checked by name in both archives.
# Taken from the OpenCvSharpExtern sources at the tag above, not guessed.
EXPORTS="core_Mat_new1 core_split imgcodecs_imdecode_vector imgcodecs_imencode_vector imgproc_morphologyEx imgproc_phaseCorrelate
imgproc_createHanningWindow imgproc_connectedComponentsWithStats imgproc_findContours1_vector imgproc_adaptiveThreshold imgproc_resize
calib3d_findHomography_InputArray objdetect_QRCodeDetector_new objdetect_QRCodeDetector_detectAndDecode aruco_getPredefinedDictionary
aruco_ArucoDetector_create aruco_ArucoDetector_detectMarkers wechat_qrcode_create1 wechat_qrcode_WeChatQRCode_detectAndDecode"

mkdir -p "${1:?work folder}" "${2:?output folder}"
WORK=$(cd "$1" && pwd)
OUT=$(cd "$2" && pwd)
JOBS=$(sysctl -n hw.ncpu)
cd "$WORK"

[ -d opencv ] || git clone --quiet --depth 1 -b "$OPENCV_VERSION" https://github.com/opencv/opencv opencv
[ -d opencv_contrib ] || git clone --quiet --depth 1 -b "$OPENCV_VERSION" https://github.com/opencv/opencv_contrib opencv_contrib
if [ ! -d opencvsharp ]; then
  git clone --quiet --depth 1 -b "$OPENCVSHARP_VERSION" https://github.com/shimat/opencvsharp opencvsharp
  # The bindings: OpenCvSharp's own list of every module is cut to GroupLab's, exactly as on Android, and the library is made static.
  # NO_CONTRIB leaves out a dozen contrib modules GroupLab never calls, and with them aruco and wechat_qrcode, which it does; those two
  # are put back by name.
  python3 - opencvsharp/src/OpenCvSharpExtern <<'PY'
import sys
from pathlib import Path
extern = Path(sys.argv[1])
cmake = extern / "CMakeLists.txt"
text = cmake.read_text()
files = "core*.cpp imgproc*.cpp imgcodecs*.cpp std*.cpp calib3d*.cpp features2d*.cpp flann*.cpp objdetect*.cpp dnn.cpp aruco*.cpp wechat_qrcode*.cpp"
assert "file(GLOB OPENCVSHARP_FILES *.cpp)" in text
text = text.replace("file(GLOB OPENCVSHARP_FILES *.cpp)",
                    f"file(GLOB OPENCVSHARP_FILES {files})\nadd_compile_definitions(NO_HIGHGUI=1 NO_VIDEO=1 NO_STITCHING=1 NO_ML=1 NO_CONTRIB=1)")
# iOS links it into the application, so it is an archive, not a shared library.
assert text.count("add_library(OpenCvSharpExtern SHARED ${OPENCVSHARP_FILES})") == 1
text = text.replace("add_library(OpenCvSharpExtern SHARED ${OPENCVSHARP_FILES})", "add_library(OpenCvSharpExtern STATIC ${OPENCVSHARP_FILES})")
# On Apple its CMake looks for Eigen and HDF5 from Homebrew, for the contrib modules that use them; GroupLab's modules use neither, and
# a Mac's own Homebrew copies would be the wrong platform for an iPhone.
for name in ("Eigen3", "HDF5"):
    line = f"find_package({name} CONFIG REQUIRED)"
    assert text.count(line) == 1, name
    text = text.replace(line, f"# GroupLab iOS: {line} left out")
# Tesseract belongs to a contrib module not built here; a Homebrew copy found on the Mac would be linked for the wrong platform.
text = text.replace("find_package(Tesseract QUIET)", "set(Tesseract_FOUND OFF)")
# wechat_qrcode links iconv from the iOS SDK, and OpenCV's static configuration names it as Iconv::Iconv, which has to exist before that
# configuration is read. OpenCvSharp looks for it only afterwards.
assert text.count("find_package(OpenCV REQUIRED)") == 1
text = text.replace("find_package(OpenCV REQUIRED)", "find_package(Iconv REQUIRED)\nfind_package(OpenCV REQUIRED)")
cmake.write_text(text)
header = extern / "include_opencv.h"
text = header.read_text()
anchor = "#endif // NO_CONTRIB"
assert anchor in text
text = text.replace(anchor, anchor + "\n#include <opencv2/aruco.hpp>\n#include <opencv2/aruco/charuco.hpp>\n#include <opencv2/dnn.hpp>\n#include <opencv2/wechat_qrcode.hpp>\n", 1)
header.write_text(text)
# The two bindings' own headers are wrapped in the same switch, so with it set they compile to nothing (entry 202 on Android). Their
# wrappers are lifted, and only theirs.
for name in ("aruco.h", "wechat_qrcode.h"):
    binding = extern / name
    text = binding.read_text()
    assert text.count("#ifndef NO_CONTRIB") == 1, name
    binding.write_text(text.replace("#ifndef NO_CONTRIB", "#if 1 // GroupLab: built for iOS without the other contrib modules", 1))
PY
fi

# One slice: OpenCV static, OpenCvSharpExtern static, then everything merged into slices/<sdk>/libOpenCvSharpExtern.a.
build_slice() {
  local sdk=$1
  local common=(-G "Unix Makefiles" -Wno-dev
    -D CMAKE_BUILD_TYPE=Release
    -D CMAKE_SYSTEM_NAME=iOS -D CMAKE_SYSTEM_PROCESSOR=arm64 -D CMAKE_OSX_SYSROOT="$sdk" -D CMAKE_OSX_ARCHITECTURES=arm64
    -D CMAKE_OSX_DEPLOYMENT_TARGET=$IOS_MIN)
  local install="$WORK/opencv-install-$sdk"

  cmake -S opencv -B "opencv-build-$sdk" "${common[@]}" \
    -D CMAKE_INSTALL_PREFIX="$install" \
    -D OPENCV_EXTRA_MODULES_PATH="$WORK/opencv_contrib/modules" \
    -D BUILD_LIST=$MODULES \
    -D OPENCV_FORCE_3RDPARTY_BUILD=ON -D BUILD_SHARED_LIBS=OFF \
    -D BUILD_EXAMPLES=OFF -D BUILD_DOCS=OFF -D BUILD_TESTS=OFF -D BUILD_PERF_TESTS=OFF -D BUILD_JAVA=OFF -D BUILD_OBJC=OFF \
    -D BUILD_opencv_apps=OFF -D BUILD_opencv_python3=OFF -D BUILD_opencv_world=OFF \
    -D WITH_PROTOBUF=ON -D WITH_QUIRC=ON -D WITH_ADE=OFF -D WITH_FFMPEG=OFF -D WITH_GSTREAMER=OFF -D WITH_OPENEXR=OFF \
    -D WITH_OPENCL=OFF -D WITH_ITT=OFF -D WITH_IPP=OFF -D WITH_EIGEN=OFF -D WITH_LAPACK=OFF -D WITH_KLEIDICV=OFF \
    -D WITH_TBB=OFF -D WITH_OPENMP=OFF -D WITH_AVFOUNDATION=OFF -D WITH_CAP_IOS=OFF \
    -D OPENCV_ENABLE_NONFREE=OFF > "$WORK/opencv-configure-$sdk.log" || { tail -60 "$WORK/opencv-configure-$sdk.log"; exit 1; }
  grep -E "To be built:|Baseline:|Dispatched code|Disabled:| (ZLib|JPEG|PNG|TIFF|WEBP|JPEG 2000|HAL|Protobuf|Lapack):" "$WORK/opencv-configure-$sdk.log" || true
  cmake --build "opencv-build-$sdk" --parallel "$JOBS" > "$WORK/opencv-build-$sdk.log" 2>&1 || { grep -E "error|Error" "$WORK/opencv-build-$sdk.log" | head -40; exit 1; }
  cmake --install "opencv-build-$sdk" > /dev/null

  local config
  config=$(dirname "$(find "$install" -name OpenCVConfig.cmake | head -1)")
  cmake -S opencvsharp/src -B "opencvsharp-build-$sdk" "${common[@]}" \
    -D CMAKE_POLICY_VERSION_MINIMUM=3.5 -D OpenCV_DIR="$config" > "$WORK/opencvsharp-configure-$sdk.log" \
    || { tail -60 "$WORK/opencvsharp-configure-$sdk.log"; exit 1; }
  cmake --build "opencvsharp-build-$sdk" --parallel "$JOBS" > "$WORK/opencvsharp-build-$sdk.log" 2>&1 \
    || { grep -E "error|Error" "$WORK/opencvsharp-build-$sdk.log" | head -40; exit 1; }

  local extern
  extern=$(find "opencvsharp-build-$sdk" -name libOpenCvSharpExtern.a | head -1)
  [ -n "$extern" ] || { echo "no libOpenCvSharpExtern.a for $sdk"; exit 1; }
  # Every archive OpenCV installed (its modules and the third party libraries it built for them) plus the bindings, as one archive.
  local libs
  libs=$(find "$install" -name '*.a' | sort)
  echo "Merged into the $sdk archive:"
  for lib in $libs "$extern"; do echo "  $(basename "$lib")"; done
  mkdir -p "slices/$sdk"
  # libtool names a member it finds twice under the same file name; that is expected across OpenCV's modules and harmless.
  xcrun libtool -static -no_warning_for_no_symbols -o "slices/$sdk/libOpenCvSharpExtern.a" $libs "$extern" 2> "$WORK/libtool-$sdk.log" \
    || { cat "$WORK/libtool-$sdk.log"; exit 1; }
  grep -v "same member name" "$WORK/libtool-$sdk.log" || true

  # The check: every entry point GroupLab calls is defined in the merged archive, or the application would fail when it first called it.
  local defined missing=""
  defined=$(xcrun nm -gU "slices/$sdk/libOpenCvSharpExtern.a" 2>/dev/null | awk '$2 == "T" { print $3 }' | sort -u)
  for name in $EXPORTS; do
    grep -qx "_$name" <<< "$defined" || missing="$missing $name"
  done
  if [ -n "$missing" ]; then
    echo "::error::The $sdk archive does not define:$missing"
    exit 1
  fi
  echo "The $sdk archive defines all $(wc -w <<< "$EXPORTS" | tr -d ' ') checked entry points, and $(grep -c . <<< "$defined") symbols in all."

  # The Apple frameworks and system libraries OpenCV's static configuration says its modules need, for the head's linker.
  grep -rhoE -- "-framework [A-Za-z]+|lib[a-z0-9+]+\.tbd" "$config" 2>/dev/null >> "$WORK/link-frameworks.raw" || true
  if grep -rq "Iconv::Iconv" "$config"; then echo "-liconv" >> "$WORK/link-frameworks.raw"; fi
  # OpenCV and the bindings are C++, and an archive carries no note of the C++ library it needs.
  echo "-lc++" >> "$WORK/link-frameworks.raw"
}

rm -f "$WORK/link-frameworks.raw"
build_slice iphoneos
build_slice iphonesimulator

rm -rf OpenCvSharpExtern.xcframework
xcodebuild -create-xcframework \
  -library slices/iphoneos/libOpenCvSharpExtern.a \
  -library slices/iphonesimulator/libOpenCvSharpExtern.a \
  -output OpenCvSharpExtern.xcframework > /dev/null
# The slices xcodebuild recognised: one ios-arm64 and one ios-arm64-simulator, or the framework is wrong.
ls OpenCvSharpExtern.xcframework
[ -d OpenCvSharpExtern.xcframework/ios-arm64 ] && [ -d OpenCvSharpExtern.xcframework/ios-arm64-simulator ] \
  || { echo "::error::The xcframework does not hold the device and simulator slices"; exit 1; }

rm -f "$OUT/OpenCvSharpExtern.xcframework.zip"
zip -qry "$OUT/OpenCvSharpExtern.xcframework.zip" OpenCvSharpExtern.xcframework
(cd "$OUT" && shasum -a 256 OpenCvSharpExtern.xcframework.zip > OpenCvSharpExtern.xcframework.zip.sha256)
sort -u "$WORK/link-frameworks.raw" > "$OUT/link-frameworks.txt" 2>/dev/null || : > "$OUT/link-frameworks.txt"
echo "Link with:"; sed 's/^/  /' "$OUT/link-frameworks.txt"
cat "$OUT/OpenCvSharpExtern.xcframework.zip.sha256"
du -h "$OUT/OpenCvSharpExtern.xcframework.zip" slices/*/libOpenCvSharpExtern.a
