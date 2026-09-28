"""Whether the phone's capture screen can be seen and used while the camera runs, NOTES-FROM-PLANNING.md entry 260.

On 2026-09-28 the Fold 7's capture screen showed the camera and nothing else: the instruction, the Take button and Back were drawn under
the preview. This opens GroupLab Dev's camera over adb, with nobody holding the phone, reads the screen's views from a UI dump, and fails
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
import xml.etree.ElementTree as ET

ADB = r"C:\Dev\tools\android-sdk\platform-tools\adb.exe"
PACKAGE = "org.grouplab.app.dev"
NEEDED = {"Instruction": False, "Shutter": True, "Back": True}
LEAST = 44


def adb(serial: str, *args: str) -> str:
    return subprocess.run([ADB, "-s", serial, *args], capture_output=True, text=True, timeout=60).stdout


def activity(serial: str) -> str:
    out = adb(serial, "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE)
    return out.strip().splitlines()[-1]


def dump(serial: str) -> ET.Element:
    adb(serial, "shell", "uiautomator", "dump", "/data/local/tmp/gl-ui.xml")
    xml = adb(serial, "shell", "cat", "/data/local/tmp/gl-ui.xml")
    adb(serial, "shell", "rm", "-f", "/data/local/tmp/gl-ui.xml")
    return ET.fromstring(xml[xml.index("<?xml"):] if "<?xml" in xml else xml)


def bounds(node: ET.Element) -> tuple[int, int, int, int]:
    x0, y0, x1, y1 = map(int, re.findall(r"\d+", node.get("bounds", "[0,0][0,0]")))
    return x0, y0, x1, y1


def check(serial: str, mode: str) -> list[str]:
    adb(serial, "shell", "am", "start", "-n", activity(serial), "--es", "org.grouplab.test.camera", mode)
    time.sleep(7)
    root = dump(serial)
    screen = root.find(".//node")
    sx0, sy0, sx1, sy1 = bounds(screen) if screen is not None else (0, 0, 10**6, 10**6)
    found: list[str] = []
    for name, clickable in NEEDED.items():
        nodes = [n for n in root.iter("node") if n.get("content-desc") == name and n.get("package") == PACKAGE]
        if not nodes:
            found.append(f"{mode}: no {name} on the screen")
            continue
        x0, y0, x1, y1 = bounds(nodes[0])
        if x1 - x0 < LEAST or y1 - y0 < LEAST:
            found.append(f"{mode}: {name} is {x1 - x0} by {y1 - y0} pixels, under {LEAST}")
        if x0 < sx0 or y0 < sy0 or x1 > sx1 or y1 > sy1:
            found.append(f"{mode}: {name} runs off the screen")
        if nodes[0].get("visible-to-user", "true") != "true":
            found.append(f"{mode}: {name} is not visible")
        if clickable and nodes[0].get("clickable") != "true":
            found.append(f"{mode}: {name} cannot be pressed")
        if name == "Instruction" and not nodes[0].get("text", "").strip():
            found.append(f"{mode}: the instruction says nothing")
    # Avalonia's own views are not in the dump, so the bar is checked by where the capture screen ends: with the bar hidden it reaches
    # the bottom of the window, less at most Android's own navigation.
    capture = [n for n in root.iter("node") if n.get("content-desc") == "Capture screen"]
    if not capture:
        found.append(f"{mode}: the capture screen is not showing")
    elif bounds(capture[0])[3] < 0.9 * sy1:
        found.append(f"{mode}: the capture screen stops {sy1 - bounds(capture[0])[3]} pixels above the bottom, so the app's bar is showing")
    words = next((n.get("text") for n in root.iter("node") if n.get("content-desc") == "Instruction"), "")
    print(f"  {mode}: instruction {words!r}")
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
    for line in problems:
        print(f"  {args.label}: {line}")
    print(f"{args.label}: {'capture screen usable' if not problems else f'{len(problems)} problems'}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
