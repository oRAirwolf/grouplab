#!/usr/bin/env bash
# NOTES-FROM-PLANNING.md entry 414 section 2: does GroupLab start again after Android stops it in the middle of reading a photograph?
# Run 37735635656 force-stopped GroupLab Dev while a reading ran, and no later start on that emulator ran anything. A person whose phone
# ends GroupLab while it reads, and who then cannot open it, would have no way out, so this does it on purpose. A control round first:
# stopped while idle, started again, the short scenario must run, or the check itself is broken and says so. Then each round starts a
# reading (scripts/scenarios/phone-force-stop-read.json), stops GroupLab Dev a different number of seconds into it, starts it again with
# scripts/scenarios/phone-force-stop-check.json, and waits up to two minutes for that to finish. Every wait is bounded. A round whose
# start ran nothing keeps Android's log. .github/workflows/android-emulator.yml runs this after the real taps.
#
# Usage: android-force-stop.sh <GroupLab Dev APK for x86_64> <output folder>. Needs adb on the path and one emulator attached.
set -uo pipefail

APK=${1:?the APK}
OUT=${2:?the output folder}
PKG=org.grouplab.app.dev
EXTRA=org.grouplab.test.scenario
READ=phone-force-stop-read.json
CHECK=phone-force-stop-check.json
HERE=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$OUT"

adb wait-for-device
adb shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 2; done'
adb shell svc power stayon true || true
adb shell input keyevent 82 || true
adb shell settings put global hide_error_dialogs 1 || true
adb shell settings put system font_scale 1.0 || true
adb install -r -g "$APK" > "$OUT/install.log" 2>&1 || { cat "$OUT/install.log"; echo "::error::GroupLab Dev did not install on the emulator"; exit 1; }
COMPONENT=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)
case "$COMPONENT" in
  "$PKG"/*) ;;
  *) echo "::error::No launcher activity for $PKG ($COMPONENT)"; exit 1 ;;
esac

# Through the shared temporary folder, as the sweep puts its files.
put() {
  adb push "$1" "/data/local/tmp/$2" > /dev/null
  adb shell chmod 644 "/data/local/tmp/$2"
  adb shell run-as "$PKG" sh -c "'mkdir -p files/scenario; cp /data/local/tmp/$2 files/scenario/$2'"
  adb shell rm -f "/data/local/tmp/$2"
}
status() { adb exec-out "run-as $PKG cat files/scenario/results/status 2>/dev/null" | tr -d '\r' || true; }
fresh() { adb shell run-as "$PKG" sh -c "'rm -rf files/scenario/results files/scenario/*.ran'"; }

# The sample sheet at 300 dpi, as the sweep reads it; its consent is in samples/PROVENANCE.md.
python3 -c "import PIL" 2> /dev/null || python3 -m pip install --quiet --user pillow > /dev/null 2>&1 || true
python3 - "$HERE/samples/gl-cf25-ltr-d-25-shots-600-dpi.png" /tmp/force-stop-sample.png <<'PY'
import sys
from PIL import Image
image = Image.open(sys.argv[1])
image.resize((image.width // 2, image.height // 2), Image.LANCZOS).save(sys.argv[2], dpi=(300, 300))
PY
put /tmp/force-stop-sample.png sample.png

# Starts the short scenario and says what became of it within two minutes: done, running, or nothing.
again() {
  fresh
  put "$HERE/scripts/scenarios/$CHECK" "$CHECK"
  adb logcat -c || true
  local began=$(date +%s) s=""
  adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$CHECK" > "$OUT/start-$1.txt" 2>&1
  while [ $(( $(date +%s) - began )) -lt 120 ]; do
    s=$(status)
    [ "$s" = done ] && break
    sleep 3
  done
  echo "${s:-nothing} after $(( $(date +%s) - began )) s"
  [ "$s" = done ] || adb logcat -d > "$OUT/logcat-$1.txt" 2>&1 || true
}

failed=0
adb shell am force-stop "$PKG"
control=$(again control)
echo "control: stopped while idle, started again: $control" | tee "$OUT/rounds.txt"
case "$control" in
  done*) ;;
  *) echo "::error::GroupLab Dev did not run the short scenario even after a stop while idle, so the rounds below prove nothing"; exit 1 ;;
esac

for after in 2 5 9 14 20; do
  adb shell am force-stop "$PKG"
  fresh
  put "$HERE/scripts/scenarios/$READ" "$READ"
  adb logcat -c || true
  adb shell am start -W -n "$COMPONENT" --es "$EXTRA" "$READ" > "$OUT/start-read-$after.txt" 2>&1
  # Counted from the scenario's own start, when it has written its status; at most a minute waiting for that.
  waited=0
  while [ "$(status)" != running ] && [ $waited -lt 60 ]; do sleep 1; waited=$((waited + 1)); done
  sleep "$after"
  midway=$(status)
  adb logcat -d > "$OUT/logcat-read-$after.txt" 2>&1 || true
  reading=$(grep -cE "read\.|S0\.|identify" "$OUT/logcat-read-$after.txt" || true)
  adb shell am force-stop "$PKG"
  result=$(again "after-$after")
  echo "stopped ${after} s into the reading (its status then: ${midway:-none}, $reading reading lines in the log): started again, $result" | tee -a "$OUT/rounds.txt"
  case "$result" in
    done*) rm -f "$OUT/logcat-read-$after.txt" ;;
    *) failed=1 ;;
  esac
done

adb shell am force-stop "$PKG"
if [ "$failed" = 1 ]; then
  echo "::error::GroupLab Dev, stopped in the middle of a reading, did not run anything when started again; rounds.txt and the logcat files say which"
fi
exit $failed
