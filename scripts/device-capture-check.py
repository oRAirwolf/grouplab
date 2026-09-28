"""Whether the phone's capture screen can be seen and used while the camera runs, NOTES-FROM-PLANNING.md entry 260.

On 2026-09-28 the Fold 7's capture screen showed the camera and nothing else: the instruction, the Take button and Back were drawn under
the preview. This opens GroupLab Dev's camera over adb, with nobody holding the phone, reads what the capture screen reports it shows (a UI dump cannot see native views hosted in Avalonia), and fails
unless the instruction, the shutter and Back are all on the screen, inside it, at least 44 pixels each way, and the app's own bar along the
bottom is hidden. It checks Guided and Manual. Run it on each device and each screen and orientation a sitting has.

    python scripts/device-capture-check.py --serial <adb serial> [--label "Fold 7 cover, portrait"]

Addresses are never printed: the serial is only passed to adb. The camera shows whatever the phone faces; nothing of it is saved.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import time

ADB = r"C:\Dev\tools\android-sdk\platform-tools\adb.exe"
PACKAGE = "org.grouplab.app.dev"
LEAST = 44


def adb(serial: str, *args: str) -> str:
    return subprocess.run([ADB, "-s", serial, *args], capture_output=True, text=True, timeout=60).stdout


def activity(serial: str) -> str:
    out = adb(serial, "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE)
    return out.strip().splitlines()[-1]


def leave_idle(serial: str) -> None:
    """The idle screen closed first, as a person would, so the camera opens in the one GroupLab Dev window and not in a second."""
    top = adb(serial, "shell", "dumpsys", "activity", "activities")
    if re.search(r"topResumedActivity=[^\n]*IdleActivity", top):
        adb(serial, "shell", "input", "keyevent", "KEYCODE_BACK")
        time.sleep(1.5)


def layout(serial: str) -> str | None:
    """The capture screen's last report of what can be seen, from GroupLab Dev's own log (the line CaptureScreen writes after layout)."""
    logs = adb(serial, "shell", "run-as", PACKAGE, "ls", "-t", "files/logs").split()
    if not logs:
        return None
    text = adb(serial, "shell", "run-as", PACKAGE, "cat", f"files/logs/{logs[0]}")
    lines = [l for l in text.splitlines() if "camera.layout" in l]
    return lines[-1] if lines else None


def check(serial: str, mode: str) -> list[str]:
    leave_idle(serial)
    before = layout(serial)
    adb(serial, "shell", "am", "start", "-n", activity(serial), "--es", "org.grouplab.test.camera", mode)
    time.sleep(7)
    line = layout(serial)
    found: list[str] = []
    if line is None or line == before:
        return [f"{mode}: the capture screen did not report its layout; it may not have opened"]
    seen = dict(re.findall(r"(\w+)=([0-9x]+|hidden)", line))
    for name in ("instruction", "shutter", "back"):
        value = seen.get(name, "hidden")
        if value == "hidden":
            found.append(f"{mode}: the {name} cannot be seen")
            continue
        w, h = map(int, value.split("x"))
        if name != "instruction" and (w < LEAST or h < LEAST):
            found.append(f"{mode}: the {name} is {w} by {h} pixels, under {LEAST}")
    bottom, window = int(seen.get("bottom", "0")), int(seen.get("window", "0") or 0)
    if window and bottom < 0.9 * window:
        found.append(f"{mode}: the capture screen stops {window - bottom} pixels above the bottom, so the app's bar is showing")
    print(f"  {mode}: {line.split('seen=', 1)[-1].strip()}")
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--serial", required=True)
    parser.add_argument("--label", default="this device")
    args = parser.parse_args()
    problems = check(args.serial, "guided") + check(args.serial, "manual")
    adb(args.serial, "shell", "input", "keyevent", "KEYCODE_BACK")
    adb(args.serial, "shell", "am", "start", "-n", activity(args.serial), "--es", "org.grouplab.test.camera", "guided")
    adb(args.serial, "shell", "input", "keyevent", "KEYCODE_BACK")
    # Entry 268: the phone and the tablet have OLED screens, so a device is never left showing a lit screen; the check ends on
    # GroupLab Dev's black idle screen (docs/ANDROID.md section 15).
    adb(args.serial, "shell", "am", "start", "-n", activity(args.serial), "--es", "org.grouplab.test.idle", "1")
    for line in problems:
        print(f"  {args.label}: {line}")
    print(f"{args.label}: {'capture screen usable' if not problems else f'{len(problems)} problems'}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
