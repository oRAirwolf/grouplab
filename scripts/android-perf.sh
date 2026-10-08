#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 388 section 4, Phase 9's baseline on the Android emulator. Start-up is Android's own figure for a cold start
# (am start -W, TotalTime), one thrown away and five timed. Then GroupLab Dev runs scripts/scenarios/phone-perf.json twice: Alan's 600 dpi
# scan read three times, each a target of its own, and the three gone round by the Open targets sheet six times. scripts/phone-perf.py
# turns the step times and the reading's own stage log into figures. The emulator runs on GitHub's machine with software drawing, so these
# are a record of the emulator, not of a phone, and nothing is gated on them.
#
# Usage: android-perf.sh <GroupLab Dev APK for x86_64> <output folder>. Needs adb and one emulator attached.
set -uo pipefail

APK=${1:?the APK}
OUT=${2:?the output folder}
PKG=org.grouplab.app.dev
EXTRA=org.grouplab.test.scenario
NAME=phone-perf.json
HERE=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$OUT"

adb wait-for-device
adb shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 2; done'
adb shell svc power stayon true || true
adb shell input keyevent 82 || true
adb shell settings put global hide_error_dialogs 1 || true
adb shell wm size reset || true
adb shell wm density reset || true
adb install -r -g "$APK" > "$OUT/install.log" 2>&1 || { cat "$OUT/install.log"; echo "::error::GroupLab Dev did not install"; exit 1; }
COMPONENT=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)

: > "$OUT/startup.txt"
for i in 0 1 2 3 4 5; do
  adb shell am force-stop "$PKG"
  sleep 2
  adb shell am start -W -n "$COMPONENT" | tr -d '\r' | grep '^TotalTime' | sed 's/TotalTime: //' >> "$OUT/startup.txt"
  sleep 4
done
adb shell am force-stop "$PKG"

failed=0
for run in 1 2; do
  adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/$NAME.ran; mkdir -p files/scenario'"
  adb push "$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png" /data/local/tmp/scan.png > /dev/null
  adb shell chmod 644 /data/local/tmp/scan.png
  adb shell run-as "$PKG" cp /data/local/tmp/scan.png files/scenario/scan.png
  adb push "$HERE/scripts/scenarios/$NAME" "/data/local/tmp/$NAME" > /dev/null
  adb shell chmod 644 "/data/local/tmp/$NAME"
  adb shell run-as "$PKG" cp "/data/local/tmp/$NAME" "files/scenario/$NAME"
  adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$NAME" > /dev/null
  start=$(date +%s)
  status=""
  while [ $(( $(date +%s) - start )) -lt 2400 ]; do
    status=$(adb exec-out "run-as $PKG cat files/scenario/results/status 2>/dev/null" | tr -d '\r' || true)
    [ "$status" = "done" ] && break
    sleep 5
  done
  echo "run $run: ${status:-none} after $(( $(date +%s) - start )) s"
  [ "$status" = "done" ] || failed=1
  mkdir -p "$OUT/run-$run"
  for file in results.json perf.txt; do
    adb exec-out "run-as $PKG cat files/scenario/results/$file" > "$OUT/run-$run/$file" 2> /dev/null || true
  done
  adb shell am force-stop "$PKG"
done
adb shell rm -f /data/local/tmp/scan.png "/data/local/tmp/$NAME" || true
python3 "$HERE/scripts/phone-perf.py" "$OUT" | tee "$OUT/figures.json" || failed=1
exit $failed
