#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 388 section 2: the quality sweep of every phone screen on the Android emulator. GroupLab Dev runs the screen
# sweep (scripts/scenarios/phone-sweep.json) at the Galaxy Z Fold 7's cover screen, its inner screen and a small phone, light and dark, and
# the cover screen once more at the largest text. Every screenshot a scenario takes writes its layout's faults beside it (Scenario.Quality
# in mobile/GroupLab.Mobile/Dev/Scenario.cs): controls under 44 units, words cut short, anything past the side, text over text.
# scripts/phone-quality.py gathers them into one report. .github/workflows/android-emulator.yml runs this when asked for, not every nightly.
#
# Usage: android-quality.sh <GroupLab Dev APK for x86_64> <output folder>. Needs adb and one emulator attached.
set -uo pipefail

APK=${1:?the APK}
OUT=${2:?the output folder}
PKG=org.grouplab.app.dev
EXTRA=org.grouplab.test.scenario
NAME=phone-sweep.json
HERE=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$OUT"

adb wait-for-device
adb shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 2; done'
adb shell svc power stayon true || true
adb shell input keyevent 82 || true
adb shell settings put global hide_error_dialogs 1 || true
adb shell settings put system accelerometer_rotation 0 || true
# A clean install, so nothing an earlier step on this emulator left behind changes what is measured here (entry 388).
adb uninstall "$PKG" > /dev/null 2>&1 || true
adb install -g "$APK" > "$OUT/install.log" 2>&1 || { cat "$OUT/install.log"; echo "::error::GroupLab Dev did not install"; exit 1; }
COMPONENT=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)

put() {
  adb push "$1" "/data/local/tmp/$2" > /dev/null
  adb shell chmod 644 "/data/local/tmp/$2"
  adb shell run-as "$PKG" cp "/data/local/tmp/$2" "files/scenario/$2"
  adb shell rm -f "/data/local/tmp/$2"
}

python3 -c "import PIL" 2> /dev/null || python3 -m pip install --quiet --user pillow > /dev/null 2>&1 || true
python3 - "$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png" /tmp/quality-sample.png <<'PY'
import sys
from PIL import Image
image = Image.open(sys.argv[1])
image.resize((image.width // 2, image.height // 2), Image.LANCZOS).save(sys.argv[2], dpi=(300, 300))
PY

failed=0
# name, size, density, font scale, themes. The cover screen is 411 units wide, the inner screen 750, the small phone 360.
while read -r name size density font themes <&3; do
  adb shell wm size "$size"
  adb shell wm density "$density"
  adb shell settings put system font_scale "$font"
  for theme in ${themes//,/ }; do
    if [ "$theme" = dark ]; then adb shell cmd uimode night yes > /dev/null; else adb shell cmd uimode night no > /dev/null; fi
    adb shell am force-stop "$PKG"
    adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/$NAME.ran; mkdir -p files/scenario'"
    put /tmp/quality-sample.png sample.png
    put "$HERE/scripts/scenarios/$NAME" "$NAME"
    adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$NAME" > /dev/null
    start=$(date +%s)
    status=""
    while [ $(( $(date +%s) - start )) -lt 900 ]; do
      status=$(adb exec-out "run-as $PKG cat files/scenario/results/status 2>/dev/null" | tr -d '\r' || true)
      [ "$status" = "done" ] && break
      sleep 3
    done
    echo "$name $theme: ${status:-none} after $(( $(date +%s) - start )) s"
    [ "$status" = "done" ] || adb logcat -d > "$OUT/logcat-$(date +%s).txt" 2>&1 || true
    [ "$status" = "done" ] || failed=1
    mkdir -p "$OUT/$name-$theme"
    adb exec-out "run-as $PKG find files/scenario/results -type f 2>/dev/null" | tr -d '\r' | while read -r file; do
      [ -n "$file" ] || continue
      adb exec-out "run-as $PKG cat '$file'" > "$OUT/$name-$theme/${file##*/}" < /dev/null
    done
    adb shell am force-stop "$PKG"
  done
done 3<<'LAYOUTS'
cover 1080x2520 420 1.0 light,dark
inner 1968x2184 420 1.0 light,dark
small 720x1280 320 1.0 light,dark
cover-largest-text 1080x2520 420 2.0 light
LAYOUTS

adb shell wm size reset || true
adb shell wm density reset || true
adb shell settings put system font_scale 1.0 || true
adb shell cmd uimode night no > /dev/null || true
python3 "$HERE/scripts/phone-quality.py" "$OUT" > "$OUT/report.md" || failed=1
cat "$OUT/report.md"
exit $failed
