"""How long GroupLab Dev takes from the shutter press to each step, NOTES-FROM-PLANNING.md entry 283.

Alan: "a lot of lag between pressing the shutter and the phone responding". This presses the shutter over adb, through the same path as a
finger (GroupLab Dev's org.grouplab.test.press extra), waits for the result, and does it again; then it reads GroupLab Dev's own log and
prints, for each step, the median and the slowest, as a Markdown table for docs/ANDROID.md. The steps are the capture screen's own
(press, request sent, exposure started, saved) and the analysis's (working copy made, sheet read, analyzed, first result on screen).

    python scripts/shutter-timing.py --serial <adb serial> [--presses 20] [--mode manual|guided] [--capture latency|quality]

The phone must face a GroupLab sheet, with the torch set on the capture screen as the run wants it. Addresses are never printed: the serial is only passed to adb. Nothing of the pictures is kept on
this computer; the phone keeps its sessions as it always does.
"""

from __future__ import annotations

import argparse
import re
import statistics
import subprocess
import sys
import time
from datetime import datetime

ADB = r"C:\Dev\tools\android-sdk\platform-tools\adb.exe"
PACKAGE = "org.grouplab.app.dev"
STEPS = ["requested", "exposed", "saved", "prepared", "read", "analyzed", "shown"]
STAMP = re.compile(r"^(\S+Z)\s+\w+\s+(\S+)\s+(.*)$")


def adb(serial: str, *args: str) -> str:
    return subprocess.run([ADB, "-s", serial, *args], capture_output=True, text=True, timeout=120).stdout


def log_text(serial: str) -> str:
    logs = adb(serial, "shell", "run-as", PACKAGE, "ls", "-t", "files/logs").split()
    logs = [name for name in logs if name.startswith("grouplab-") and name.endswith(".log")]
    return adb(serial, "shell", "run-as", PACKAGE, "cat", f"files/logs/{logs[0]}") if logs else ""


def shown_count(serial: str) -> int:
    return log_text(serial).count("step=shown")


def when(stamp: str) -> float:
    return datetime.fromisoformat(stamp.replace("Z", "+00:00")).timestamp() * 1000


def runs(text: str) -> list[dict[str, float]]:
    """Each press and the milliseconds from it to every later step, by the log's own clock."""
    found: list[dict[str, float]] = []
    press: float | None = None
    current: dict[str, float] = {}
    for line in text.splitlines():
        m = STAMP.match(line)
        if not m:
            continue
        at, event, rest = when(m.group(1)), m.group(2), m.group(3)
        step = re.search(r"step=(\w+)", rest)
        if event == "camera.shutter" and step and step.group(1) == "press":
            press, current = at, {}
            continue
        if press is None:
            continue
        name = step.group(1) if event == "camera.shutter" and step else {"phone.prepare": "prepared", "phone.detect": "read"}.get(event)
        if name in STEPS and name not in current:
            current[name] = at - press
            if name == "shown":
                found.append(current)
                press = None
    return found


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--serial", required=True)
    parser.add_argument("--presses", type=int, default=20)
    parser.add_argument("--mode", choices=["manual", "guided"], default="manual")
    parser.add_argument("--capture", choices=["latency", "quality"], default="latency")
    parser.add_argument("--settle", type=float, default=4, help="seconds between the camera starting and the press")
    args = parser.parse_args()

    activity = adb(args.serial, "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE).strip().splitlines()[-1]
    before = shown_count(args.serial)
    for i in range(args.presses):
        adb(args.serial, "shell", "am", "start", "-n", activity, "--es", "org.grouplab.test.camera", args.mode,
            "--es", "org.grouplab.test.capturemode", args.capture, "--es", "org.grouplab.test.press", str(args.settle))
        deadline = time.time() + 120
        while shown_count(args.serial) <= before + i and time.time() < deadline:
            time.sleep(2)
        print(f"press {i + 1} of {args.presses}", file=sys.stderr)

    measured = runs(log_text(args.serial))[-args.presses:]
    print(f"{len(measured)} presses, {args.mode}, {args.capture} capture\n")
    print("| From the press to | median ms | slowest ms |")
    print("|---|---|---|")
    for step in STEPS:
        values = [r[step] for r in measured if step in r]
        if values:
            print(f"| {step} | {statistics.median(values):.0f} | {max(values):.0f} |")
    # Entry 268: the screen goes back to GroupLab Dev's black idle screen when the run is done.
    adb(args.serial, "shell", "am", "start", "-n", activity, "--es", "org.grouplab.test.idle", "1")
    return 0 if measured else 1


if __name__ == "__main__":
    sys.exit(main())
