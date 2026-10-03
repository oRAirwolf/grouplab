#!/usr/bin/env python3
"""NOTES-FROM-PLANNING.md entry 353 step 2: real touches on the iOS simulator and the Android emulator.

On TestFlight build 157 every button on the Capture screen did nothing on an iPhone while a field had the focus, and the screen sweep
passed, because a scenario presses a button by raising its click and a finger's press never went through the screen's input at all. This
script is the finger. GroupLab Dev runs scripts/scenarios/phone-touch.json; at each "hold" step it stops and writes results/hold.json, the
automation id to tap and every control showing with its place on the screen (mobile/GroupLab.Mobile/Dev/Scenario.cs). This script takes
the middle of that control's place, refuses where the control is not there, not enabled, covered, or under the keyboard, taps it with the
platform's own touch, writes results/tapped.json saying what it did, and takes the hold away. The scenario's "expect" steps then say
whether each tap did its job, from what the application itself shows and logs afterwards.

  touch-test.py ios --data <GroupLab Dev's data container> --taps <folder the XCUITest runner watches> [--pid <simctl launch's>]
  touch-test.py android --package org.grouplab.app.dev
  touch-test.py check <results folder> <scenario>
  touch-test.py --self-test

iOS: simctl has no tap, so a small XCUITest runner (scripts/touch/ios, built with XcodeGen) waits in the simulator for a request file in
the --taps folder, "x y seconds" in points, presses there through the springboard for that long, and writes "done". Android: adb's input
in pixels, held for the same time as a swipe that does not move, because "input tap" sends the finger down and up in the same instant and
a page that moves between the two, which is what build 157 did, would never be caught by it.
"""

import json
import os
import subprocess
import sys
import tempfile
import time

HOLD = "hold.json"
TAPPED = "tapped.json"
# How long the finger stays down: a person's tap is about a tenth of a second, long enough for a layout pass to run between down and up.
PRESS_SECONDS = 0.15


def tap_point(hold):
    """The point to tap for a hold, in the units the hold gives, and what was done or why not; the point is None where it refuses."""
    tap = hold.get("tap")
    control = next((c for c in hold.get("controls", []) if c.get("id") == tap), None)
    if control is None:
        return None, f"no control with the id {tap} is showing"
    if not control.get("enabled"):
        return None, f"{tap} is not enabled"
    if hold.get("reached") is False:
        return None, f"{tap} is covered at its middle by {hold.get('under')}"
    screen = control["screen"]
    x = screen["x"] + screen["width"] / 2
    y = screen["y"] + screen["height"] / 2
    covered = hold.get("coveredFrom")
    if covered is not None and y >= covered:
        return None, f"{tap} is under the keyboard"
    view = hold.get("view")
    if view and not (view["x"] <= x < view["x"] + view["width"] and view["y"] <= y < view["y"] + view["height"]):
        return None, f"{tap} is off the screen at {x:.0f},{y:.0f}"
    return (x, y), f"tapped at {x:.0f},{y:.0f} {hold.get('units', '')}".rstrip()


class Ios:
    """GroupLab Dev's results folder in the simulator's container, read on the Mac, and the XCUITest runner's request folder."""

    units = "points"

    def __init__(self, data, taps, pid=None):
        self.results = os.path.join(data, "Documents", "scenario", "results")
        self.taps = taps
        self.pid = pid

    def alive(self):
        """Whether GroupLab Dev still runs: the simctl launch that started it with --console-pty lasts as long as it does."""
        if self.pid is None:
            return True
        try:
            os.kill(self.pid, 0)
            return True
        except OSError:
            return False

    def read(self, name):
        try:
            with open(os.path.join(self.results, name), encoding="utf-8") as f:
                return f.read()
        except OSError:
            return None

    def write(self, name, text):
        path = os.path.join(self.results, name)
        with open(path + ".part", "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        os.replace(path + ".part", path)

    def remove(self, name):
        try:
            os.remove(os.path.join(self.results, name))
        except OSError:
            pass

    def tap(self, x, y):
        done = os.path.join(self.taps, "done")
        if os.path.exists(done):
            os.remove(done)
        request = os.path.join(self.taps, "request")
        with open(request + ".part", "w", encoding="utf-8", newline="\n") as f:
            f.write(f"{x:.1f} {y:.1f} {PRESS_SECONDS}\n")
        os.replace(request + ".part", request)
        start = time.time()
        while time.time() - start < 60:
            if os.path.exists(done):
                os.remove(done)
                return
            time.sleep(0.1)
        raise RuntimeError("the XCUITest runner did not answer in 60 s")


class Android:
    """GroupLab Dev's results folder read and written over adb with run-as, which works because GroupLab Dev is debuggable."""

    units = "pixels"

    def __init__(self, package):
        self.package = package

    def alive(self):
        return bool(self._run("shell", "pidof", self.package).stdout.strip())

    def _run(self, *args, text=None):
        return subprocess.run(["adb", *args], capture_output=True, text=True, timeout=60, input=text)

    def read(self, name):
        said = self._run("exec-out", f"run-as {self.package} cat files/scenario/results/{name} 2>/dev/null")
        return said.stdout if said.returncode == 0 and said.stdout.strip() else None

    def write(self, name, text):
        # Through the shared temporary folder, as scripts/android-sweep.sh puts files: piping into run-as arrived cut short on the emulator.
        with tempfile.NamedTemporaryFile("w", delete=False, suffix=".json", encoding="utf-8", newline="\n") as f:
            f.write(text)
        try:
            self._run("push", f.name, f"/data/local/tmp/{name}")
            self._run("shell", "chmod", "644", f"/data/local/tmp/{name}")
            self._run("shell", "run-as", self.package, "cp", f"/data/local/tmp/{name}", f"files/scenario/results/{name}")
            self._run("shell", "rm", "-f", f"/data/local/tmp/{name}")
        finally:
            os.remove(f.name)

    def remove(self, name):
        self._run("shell", "run-as", self.package, "rm", "-f", f"files/scenario/results/{name}")

    def tap(self, x, y):
        x, y = round(x), round(y)
        said = self._run("shell", "input", "swipe", str(x), str(y), str(x), str(y), str(int(PRESS_SECONDS * 1000)))
        if said.returncode != 0:
            raise RuntimeError(f"adb input failed: {said.stderr.strip()}")


def drive(device, seconds):
    """Answers every hold until the scenario says it is done; the number of holds answered."""
    start = time.time()
    answered = 0
    while time.time() - start < seconds:
        text = device.read(HOLD)
        if text:
            try:
                hold = json.loads(text)
            except ValueError:
                time.sleep(0.2)
                continue
            point, why = tap_point(hold)
            if point is not None and hold.get("units") != device.units:
                point, why = None, f"the hold is in {hold.get('units')}, and taps here are in {device.units}"
            ok = False
            if point is not None:
                try:
                    device.tap(*point)
                    ok = True
                except (RuntimeError, OSError, subprocess.SubprocessError) as e:
                    why = f"the tap failed: {e}"
            print(f"{time.time() - start:6.1f} s  {hold.get('tap')}: {why}", flush=True)
            device.write(TAPPED, json.dumps({"ok": ok, "detail": why}))
            device.remove(HOLD)
            answered += 1
            continue
        if (device.read("status") or "").strip() == "done":
            return answered
        if not device.alive():
            time.sleep(2)
            if (device.read("status") or "").strip() == "done":
                return answered
            raise SystemExit(f"::error::GroupLab Dev ended during the touch scenario ({answered} taps answered)")
        time.sleep(0.3)
    raise SystemExit(f"::error::The touch scenario did not finish in {seconds} s ({answered} taps answered)")


def check(folder, scenario_path):
    """Every step of the touch scenario ran and did what it should; the lines it prints say which did not."""
    path = os.path.join(folder, "results.json")
    if not os.path.exists(path):
        return ["the touch scenario wrote no results"]
    results = json.load(open(path, encoding="utf-8"))
    scenario = json.load(open(scenario_path, encoding="utf-8"))
    bad = []
    for step in results.get("steps", []):
        print(f"{step['step']:>3} {step['do']:<10} {'ok' if step['ok'] else 'FAILED':<6} {step['ms']:>7} ms  {step['detail']}")
        if not step["ok"]:
            bad.append(f"{step['step']} {step['do']}: {step['detail']}")
    if "error" in results:
        bad.append(results["error"])
    if len(results.get("steps", [])) != len(scenario["steps"]):
        bad.append(f"{len(results.get('steps', []))} of the scenario's {len(scenario['steps'])} steps ran")
    return bad


def self_test():
    hold = {"tap": "go", "units": "pixels", "view": {"x": 0, "y": 0, "width": 1080, "height": 2400}, "coveredFrom": 1500, "reached": True,
            "controls": [{"id": "go", "enabled": True, "screen": {"x": 100, "y": 200, "width": 300, "height": 120}},
                         {"id": "low", "enabled": True, "screen": {"x": 100, "y": 1600, "width": 300, "height": 120}},
                         {"id": "off", "enabled": False, "screen": {"x": 0, "y": 0, "width": 10, "height": 10}}]}
    assert tap_point(hold) == ((250, 260), "tapped at 250,260 pixels"), tap_point(hold)
    assert tap_point({**hold, "tap": "low"})[1] == "low is under the keyboard"
    assert tap_point({**hold, "tap": "off"})[1] == "off is not enabled"
    assert tap_point({**hold, "tap": "gone"})[1] == "no control with the id gone is showing"
    assert tap_point({**hold, "reached": False, "under": "Border"})[1] == "go is covered at its middle by Border"
    assert tap_point({**hold, "view": {"x": 0, "y": 0, "width": 200, "height": 200}})[0] is None
    with tempfile.TemporaryDirectory() as folder:
        steps = [{"do": "hold", "tap": "go"}, {"do": "expect", "keyboard": True}]
        with open(os.path.join(folder, "s.json"), "w", encoding="utf-8") as f:
            json.dump({"steps": steps}, f)
        ran = [{"step": 1, "do": "hold", "ok": True, "ms": 5, "detail": "go: tapped"}, {"step": 2, "do": "expect", "ok": False, "ms": 9, "detail": "the keyboard is not up"}]
        with open(os.path.join(folder, "results.json"), "w", encoding="utf-8") as f:
            json.dump({"steps": ran}, f)
        assert check(folder, os.path.join(folder, "s.json")) == ["2 expect: the keyboard is not up"]
        with open(os.path.join(folder, "results.json"), "w", encoding="utf-8") as f:
            json.dump({"steps": ran[:1]}, f)
        assert check(folder, os.path.join(folder, "s.json")) == ["1 of the scenario's 2 steps ran"]
    print("touch-test self-test: ok")


def main(argv):
    if argv[:1] == ["--self-test"]:
        self_test()
        return 0
    if argv[:1] == ["check"] and len(argv) == 3:
        bad = check(argv[1], argv[2])
        if bad:
            print(f"::error::The real-touch test found taps that did not do their job: {'; '.join(bad)}")
            return 1
        return 0
    if argv[:1] in (["ios"], ["android"]):
        options = dict(zip(argv[1::2], argv[2::2]))
        seconds = float(options.get("--seconds", 900))
        if argv[0] == "ios":
            device = Ios(options["--data"], options["--taps"], int(options["--pid"]) if "--pid" in options else None)
        else:
            device = Android(options.get("--package", "org.grouplab.app.dev"))
        answered = drive(device, seconds)
        print(f"{answered} taps answered")
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
