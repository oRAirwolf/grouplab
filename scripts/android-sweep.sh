#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 352 item 1: the screen sweep on an Android emulator, the same scenario the iOS simulator runs
# (scripts/scenarios/phone-sweep.json, from ios-app.yml), with GroupLab Dev for x86_64. .github/workflows/android-emulator.yml runs this
# inside reactivecircus/android-emulator-runner, whose script input runs each line on its own, so everything with a loop in it lives here.
#
# Three passes, as on the simulator: as the emulator is set, at the largest text size, and dark. Each one starts GroupLab Dev with the
# scenario named by the extra org.grouplab.test.scenario (mobile/GroupLab.Mobile/Dev/Scenario.cs), waits for results/status to say done,
# and copies the results back over adb with run-as, which works because GroupLab Dev is debuggable. GroupLab Dev ending before the status
# says done is a crash and fails the run; so does a step that could not find what it needed, or too few screenshots.
#
# Usage: android-sweep.sh <GroupLab Dev APK for x86_64> <output folder>. Needs adb on the path and one emulator attached.
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
adb shell settings put global window_animation_scale 0 || true
adb shell settings put global transition_animation_scale 0 || true
adb shell settings put global animator_duration_scale 0 || true
# The emulator's own launcher sometimes stops answering, and Android's "isn't responding" box then sits over GroupLab Dev and takes
# every tap (run 37118457310, 2026-10-03). GroupLab Dev's own crashes are still caught from the log below, so no box is needed.
adb shell settings put global hide_error_dialogs 1 || true

# -g grants every permission the manifest asks for, so the camera's question never covers the screens.
adb install -r -g "$APK" > "$OUT/install.log" 2>&1 || { cat "$OUT/install.log"; echo "::error::GroupLab Dev did not install on the emulator"; exit 1; }
COMPONENT=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)
case "$COMPONENT" in
  "$PKG"/*) echo "GroupLab Dev starts as $COMPONENT" ;;
  *) echo "::error::No launcher activity for $PKG ($COMPONENT)"; exit 1 ;;
esac
UID_OF_APP=$(adb shell pm list packages -U "$PKG" | tr -d '\r' | grep "^package:$PKG " | sed 's/.*uid://')
[ -n "$UID_OF_APP" ] || { echo "::error::No user id for $PKG"; exit 1; }

# A file into GroupLab Dev's own scenario folder: exec-in carries the bytes untouched, and the size read back proves it arrived whole.
put() {
  local from=$1 to=$2 size
  # Through the shared temporary folder: piping into run-as arrived cut short on the emulator (run 37002647081).
  adb push "$from" "/data/local/tmp/$to" > /dev/null
  adb shell chmod 644 "/data/local/tmp/$to"
  adb shell run-as "$PKG" cp "/data/local/tmp/$to" "files/scenario/$to"
  adb shell rm -f "/data/local/tmp/$to"
  size=$(adb exec-out "run-as $PKG stat -c %s files/scenario/$to 2>/dev/null" | tr -d '\r')
  if [ "$size" != "$(wc -c < "$from" | tr -d ' ')" ]; then
    echo "::error::$to arrived in GroupLab Dev's folder as $size bytes, not $(wc -c < "$from")"
    return 1
  fi
}

failed=0
# The same sheet at 300 dpi where ImageMagick is there: the 600 dpi scan took 52 s to read on the iPhone simulator and hit the one-minute
# limit on a reading (run 36996193435); the sweep is about the screens, not the reading's speed.
SAMPLE="$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png"
# ImageMagick is not on the runner, so Python's imaging library makes the copy, installed if it is not there.
python3 -c "import PIL" 2> /dev/null || python3 -m pip install --quiet --user pillow > /dev/null 2>&1 || true
python3 - "$SAMPLE" /tmp/sweep-sample.png <<'PY' && SAMPLE=/tmp/sweep-sample.png || echo "::warning::the sweep reads the 600 dpi sample"
import sys
from PIL import Image
image = Image.open(sys.argv[1])
image.resize((image.width // 2, image.height // 2), Image.LANCZOS).save(sys.argv[2], dpi=(300, 300))
PY

for pass in plain largest-text dark; do
  case "$pass" in
    plain) adb shell settings put system font_scale 1.0; adb shell cmd uimode night no > /dev/null ;;
    largest-text) adb shell settings put system font_scale 2.0 ;;
    dark) adb shell settings put system font_scale 1.0; adb shell cmd uimode night yes > /dev/null ;;
  esac
  adb shell am force-stop "$PKG"
  adb logcat -c || true
  adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/$NAME.ran; mkdir -p files/scenario'"
  put "$SAMPLE" sample.png || { failed=1; continue; }
  put "$HERE/scripts/scenarios/phone-sweep.json" "$NAME" || { failed=1; continue; }
  adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$NAME" > /dev/null

  start=$(date +%s)
  status=""
  while [ $(( $(date +%s) - start )) -lt 900 ]; do
    status=$(adb exec-out "run-as $PKG cat files/scenario/results/status 2>/dev/null" | tr -d '\r' || true)
    [ "$status" = "done" ] && break
    if [ -z "$(adb shell pidof "$PKG" | tr -d '\r')" ]; then
      sleep 2
      status=$(adb exec-out "run-as $PKG cat files/scenario/results/status 2>/dev/null" | tr -d '\r' || true)
      [ "$status" = "done" ] && break
      echo "::error::GroupLab Dev ended during the sweep's $pass pass (status: ${status:-none})"
      failed=1
      break
    fi
    sleep 3
  done
  echo "$pass: status after $(( $(date +%s) - start )) s: ${status:-none}"
  [ "$status" = "done" ] || failed=1

  adb exec-out screencap -p > "$OUT/sweep-$pass-last.png" || true
  adb logcat -d > "$OUT/sweep-$pass-logcat.txt" 2>&1 || true
  # Only GroupLab Dev's own lines are judged: another process on the emulator stopping is not GroupLab's crash.
  adb logcat -d --uid="$UID_OF_APP" > "$OUT/sweep-$pass-logcat-app.txt" 2>&1 || true
  if grep -E "FATAL EXCEPTION|Fatal signal|Unhandled managed exception" "$OUT/sweep-$pass-logcat-app.txt" | grep -q .; then
    echo "::error::GroupLab Dev crashed during the sweep's $pass pass; sweep-$pass-logcat-app.txt has the lines"
    grep -E -A 12 "FATAL EXCEPTION|Fatal signal|Unhandled managed exception" "$OUT/sweep-$pass-logcat-app.txt" | head -40
    failed=1
  fi
  adb shell am force-stop "$PKG"

  mkdir -p "$OUT/sweep/$pass"
  adb exec-out "run-as $PKG find files/scenario/results -type f 2>/dev/null" | tr -d '\r' | while read -r file; do
    [ -n "$file" ] || continue
    local_path="$OUT/sweep/$pass/${file#files/scenario/results/}"
    mkdir -p "$(dirname "$local_path")"
    adb exec-out "run-as $PKG cat '$file'" > "$local_path" < /dev/null
  done
  python3 "$HERE/scripts/phone-sweep-check.py" "$OUT/sweep/$pass" "$pass" || failed=1
done

adb shell settings put system font_scale 1.0 || true
adb shell cmd uimode night no > /dev/null || true
exit $failed
