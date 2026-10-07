#!/usr/bin/env bash
# Question 90 (b), Alan, 2026-10-07: the phone's published screenshots (docs/figures/screens/phone) taken on the Android emulator, so they
# no longer wait on a sitting with Alan's devices. .github/workflows/android-emulator.yml runs this after the sweep, inside the emulator
# runner. The emulator's screen is set to the size and density of each device pictured, the Galaxy Z Fold 7's cover screen and the
# Galaxy Tab S8 Ultra, upright and sideways, light and dark; GroupLab Dev runs scripts/scenarios/phone-screens.json, whose screenshots are
# GroupLab's own window rendered (so no status bar), and each one the page needs is copied under its published name. made-from.json then
# names the nightly they were taken on. The launcher icons are not retaken: they show Android's launcher, not GroupLab.
#
# Usage: android-screens.sh <GroupLab Dev APK for x86_64> <output folder> <nightly number>. Needs adb and one emulator attached.
set -uo pipefail

APK=${1:?the APK}
OUT=${2:?the output folder}
NIGHTLY=${3:?the nightly number}
PKG=org.grouplab.app.dev
EXTRA=org.grouplab.test.scenario
NAME=phone-screens.json
HERE=$(cd "$(dirname "$0")/.." && pwd)
PHONE="$HERE/docs/figures/screens/phone"
mkdir -p "$OUT"

adb wait-for-device
adb shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 2; done'
adb shell svc power stayon true || true
adb shell input keyevent 82 || true
adb shell settings put global hide_error_dialogs 1 || true
adb shell settings put system accelerometer_rotation 0 || true
adb install -r -g "$APK" > "$OUT/install.log" 2>&1 || { cat "$OUT/install.log"; echo "::error::GroupLab Dev did not install"; exit 1; }
COMPONENT=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)

put() {
  adb push "$1" "/data/local/tmp/$2" > /dev/null
  adb shell chmod 644 "/data/local/tmp/$2"
  adb shell run-as "$PKG" cp "/data/local/tmp/$2" "files/scenario/$2"
  adb shell rm -f "/data/local/tmp/$2"
}

# The sample sheet at 300 dpi, as the sweep reads it; its consent is in samples/PROVENANCE.md.
python3 -c "import PIL" 2> /dev/null || python3 -m pip install --quiet --user pillow > /dev/null 2>&1 || true
python3 - "$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png" /tmp/screens-sample.png <<'PY'
import sys
from PIL import Image
image = Image.open(sys.argv[1])
image.resize((image.width // 2, image.height // 2), Image.LANCZOS).save(sys.argv[2], dpi=(300, 300))
PY

failed=0
taken=0
# device, size, density, rotation (0 upright, 1 sideways), the file name's middle, and which screens that layout publishes.
# The layouts come in on descriptor 3: adb shell reads standard input, and on the first run it swallowed every layout after the first.
while read -r device size density rotation middle screens <&3; do
  adb shell wm size "$size"
  adb shell wm density "$density"
  adb shell settings put system user_rotation "$rotation"
  for theme in light dark; do
    if [ "$theme" = dark ]; then adb shell cmd uimode night yes > /dev/null; else adb shell cmd uimode night no > /dev/null; fi
    adb shell am force-stop "$PKG"
    adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/$NAME.ran; mkdir -p files/scenario'"
    put /tmp/screens-sample.png sample.png
    put "$HERE/scripts/scenarios/$NAME" "$NAME"
    adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$NAME" > /dev/null
    start=$(date +%s)
    status=""
    while [ $(( $(date +%s) - start )) -lt 900 ]; do
      status=$(adb exec-out "run-as $PKG cat files/scenario/results/status 2>/dev/null" | tr -d '\r' || true)
      [ "$status" = "done" ] && break
      sleep 3
    done
    echo "$device $middle $theme: ${status:-none} after $(( $(date +%s) - start )) s"
    for screen in ${screens//,/ }; do
      if [ "$middle" = "-" ]; then to="$PHONE/$device-$screen-$theme.png"; else to="$PHONE/$device-$screen-$middle-$theme.png"; fi
      if adb exec-out "run-as $PKG cat files/scenario/results/$screen.png" > "$OUT/$device-$screen-$middle-$theme.png" 2> /dev/null \
         && python3 -c "import sys; from PIL import Image; Image.open(sys.argv[1]).verify()" "$OUT/$device-$screen-$middle-$theme.png" 2> /dev/null; then
        cp "$OUT/$device-$screen-$middle-$theme.png" "$to"
        taken=$((taken + 1))
      else
        echo "::warning::no $screen screenshot for $device $middle $theme; the published one is kept"
        failed=1
      fi
    done
    adb shell am force-stop "$PKG"
  done
done 3<<'LAYOUTS'
fold 1080x2520 420 0 - firstrun,capture,result,settings,targets
fold 1080x2520 420 1 landscape result,sessions
tab 2960x1848 340 0 landscape result,capture,sessions,targets,settings
tab 2960x1848 340 1 portrait result
LAYOUTS

adb shell wm size reset || true
adb shell wm density reset || true
adb shell settings put system user_rotation 0 || true
adb shell cmd uimode night no > /dev/null || true
echo "$taken screenshots taken"
# Stamped only when every picture was retaken, so a picture kept from before is never dated by this nightly.
if [ "$failed" = 0 ] && [ "$taken" -gt 0 ]; then
  printf '{\n  "about": "The nightly these phone and tablet pictures were taken on: on the Android emulator, sized as each device, by scripts/android-screens.sh (question 90).",\n  "nightly": %s\n}\n' "$NIGHTLY" > "$PHONE/made-from.json"
fi
exit $failed
