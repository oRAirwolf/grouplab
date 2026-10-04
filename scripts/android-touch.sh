#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 353 step 2: real taps on the Android emulator, beside the iOS simulator's (ios-app.yml, "Real taps").
# TestFlight build 157 ignored every tap on the Capture screen while a field had the focus, and the screen sweep passed, because a scenario
# presses buttons by raising their click and never goes through the screen's input. GroupLab Dev runs scripts/scenarios/phone-touch.json,
# which stops at each control to tap; scripts/touch-test.py reads where it is on the screen, in pixels, from GroupLab Dev's own record and
# presses there with adb's input, held as a finger is. The scenario's own steps then say whether each tap did its job, from what GroupLab
# Dev shows and logs afterwards. .github/workflows/android-emulator.yml runs this after the sweep, inside reactivecircus/android-emulator-runner.
#
# Usage: android-touch.sh <GroupLab Dev APK for x86_64> <output folder>. Needs adb on the path and one emulator attached.
set -uo pipefail

APK=${1:?the APK}
OUT=${2:?the output folder}
PKG=org.grouplab.app.dev
EXTRA=org.grouplab.test.scenario
NAME=phone-touch.json
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
# The keyboard on the screen even where the emulator reports a hardware one, so a field's tap brings it up as it does on a phone.
adb shell settings put secure show_ime_with_hard_keyboard 1 || true
adb shell settings put system font_scale 1.0 || true
adb shell cmd uimode night no > /dev/null || true

adb install -r -g "$APK" > "$OUT/touch-install.log" 2>&1 || { cat "$OUT/touch-install.log"; echo "::error::GroupLab Dev did not install on the emulator"; exit 1; }
COMPONENT=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)
case "$COMPONENT" in
  "$PKG"/*) ;;
  *) echo "::error::No launcher activity for $PKG ($COMPONENT)"; exit 1 ;;
esac
UID_OF_APP=$(adb shell pm list packages -U "$PKG" | tr -d '\r' | grep "^package:$PKG " | sed 's/.*uid://')

adb shell am force-stop "$PKG"
adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/$NAME.ran; mkdir -p files/scenario'"
# Through the shared temporary folder, as the sweep puts its files: piping into run-as arrived cut short on the emulator.
adb push "$HERE/scripts/scenarios/$NAME" "/data/local/tmp/$NAME" > /dev/null
adb shell chmod 644 "/data/local/tmp/$NAME"
adb shell run-as "$PKG" cp "/data/local/tmp/$NAME" "files/scenario/$NAME"
adb shell rm -f "/data/local/tmp/$NAME"

failed=0
adb logcat -c || true
adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$NAME" > /dev/null
python3 "$HERE/scripts/touch-test.py" android --package "$PKG" --seconds 900 || failed=1

adb exec-out screencap -p > "$OUT/touch-last.png" || true
[ -n "$UID_OF_APP" ] && adb logcat -d --uid="$UID_OF_APP" > "$OUT/touch-logcat-app.txt" 2>&1 || true
if grep -E "FATAL EXCEPTION|Fatal signal|Unhandled managed exception" "$OUT/touch-logcat-app.txt" 2> /dev/null | grep -q .; then
  echo "::error::GroupLab Dev crashed during the real taps; touch-logcat-app.txt has the lines"
  failed=1
fi
adb shell am force-stop "$PKG"

mkdir -p "$OUT/touch"
adb exec-out "run-as $PKG find files/scenario/results -type f 2>/dev/null" | tr -d '\r' | while read -r file; do
  [ -n "$file" ] || continue
  local_path="$OUT/touch/${file#files/scenario/results/}"
  mkdir -p "$(dirname "$local_path")"
  adb exec-out "run-as $PKG cat '$file'" > "$local_path" < /dev/null
done
python3 "$HERE/scripts/touch-test.py" check "$OUT/touch" "$HERE/scripts/scenarios/$NAME" || failed=1
exit $failed
