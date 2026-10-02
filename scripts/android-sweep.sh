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
  adb exec-in "run-as $PKG sh -c 'cat > files/scenario/$to'" < "$from"
  size=$(adb exec-out "run-as $PKG stat -c %s files/scenario/$to 2>/dev/null" | tr -d '\r')
  if [ "$size" != "$(wc -c < "$from" | tr -d ' ')" ]; then
    echo "::error::$to arrived in GroupLab Dev's folder as $size bytes, not $(wc -c < "$from")"
    return 1
  fi
}

failed=0
for pass in plain largest-text dark; do
  case "$pass" in
    plain) adb shell settings put system font_scale 1.0; adb shell cmd uimode night no > /dev/null ;;
    largest-text) adb shell settings put system font_scale 2.0 ;;
    dark) adb shell settings put system font_scale 1.0; adb shell cmd uimode night yes > /dev/null ;;
  esac
  adb shell am force-stop "$PKG"
  adb logcat -c || true
  adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/$NAME.ran; mkdir -p files/scenario'"
  put "$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png" sample.png || { failed=1; continue; }
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
