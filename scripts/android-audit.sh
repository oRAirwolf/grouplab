#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 461 section 2.1: what TalkBack would find, checked by machine on the Android emulator, beside the iOS
# simulator's (ios-app.yml, "Screen reader audit"). GroupLab Dev runs scripts/scenarios/phone-audit.json, the sweep's screens, stopping at
# each; scripts/screen-reader-audit.py reads Android's accessibility node tree there with uiautomator and judges it against GroupLab Dev's
# own record of what is showing: every node that can be tapped named, with a role and 48 by 48 dp to touch, and nothing that can be tapped
# missing from the tree. .github/workflows/android-emulator.yml runs this after the real taps, inside reactivecircus/android-emulator-runner.
#
# Usage: android-audit.sh <GroupLab Dev APK for x86_64> <output folder>. Needs adb on the path and one emulator attached.
set -uo pipefail

APK=${1:?the APK}
OUT=${2:?the output folder}
PKG=org.grouplab.app.dev
EXTRA=org.grouplab.test.scenario
NAME=phone-audit.json
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

adb install -r -g "$APK" > "$OUT/audit-install.log" 2>&1 || { cat "$OUT/audit-install.log"; echo "::error::GroupLab Dev did not install on the emulator"; exit 1; }
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
# The sample the result screens are read from, at 300 dpi where Python's imaging library is there, as the sweep reads it.
SAMPLE="$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png"
python3 -c "import PIL" 2> /dev/null || python3 -m pip install --quiet --user pillow > /dev/null 2>&1 || true
python3 - "$SAMPLE" /tmp/audit-sample.png <<'PY' && SAMPLE=/tmp/audit-sample.png || true
import sys
from PIL import Image
image = Image.open(sys.argv[1])
image.resize((image.width // 2, image.height // 2), Image.LANCZOS).save(sys.argv[2], dpi=(300, 300))
PY
adb push "$SAMPLE" /data/local/tmp/sample.png > /dev/null
adb shell chmod 644 /data/local/tmp/sample.png
adb shell run-as "$PKG" cp /data/local/tmp/sample.png files/scenario/sample.png
adb shell rm -f /data/local/tmp/sample.png

failed=0
adb logcat -c || true
adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$NAME" > /dev/null
python3 "$HERE/scripts/screen-reader-audit.py" android --package "$PKG" --out "$OUT/audit" --seconds 1500 || failed=1

adb exec-out screencap -p > "$OUT/audit-last.png" || true
[ -n "$UID_OF_APP" ] && adb logcat -d --uid="$UID_OF_APP" > "$OUT/audit-logcat-app.txt" 2>&1 || true
if grep -E "FATAL EXCEPTION|Fatal signal|Unhandled managed exception" "$OUT/audit-logcat-app.txt" 2> /dev/null | grep -q .; then
  echo "::error::GroupLab Dev crashed during the audit; audit-logcat-app.txt has the lines"
  failed=1
fi
adb shell am force-stop "$PKG"
python3 "$HERE/scripts/screen-reader-audit.py" judge "$OUT/audit" || failed=1
exit $failed
