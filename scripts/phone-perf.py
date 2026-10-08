"""NOTES-FROM-PLANNING.md entry 388 section 4: Phase 9's baseline on the Android emulator, as figures.

scripts/android-perf.sh leaves startup.txt (Android's TotalTime for each cold start, the first thrown away) and one folder per run of
scripts/scenarios/phone-perf.json, each with the scenario's results.json and the log it kept (perf.txt). This prints one JSON object:
start-up, reading the 600 dpi scan from the picture handed over to its result, switching between three open targets, and the reading's
own stages (the read.stage lines), each as the median, fastest and slowest in milliseconds with how many there were.

Usage: phone-perf.py <folder>
       phone-perf.py --self-test
"""

from __future__ import annotations

import json
import os
import re
import statistics
import sys
import tempfile

STAGE = re.compile(r'read\.stage\s.*?\bstage=("[^"]*"|\S+).*?\bms=(\d+)')


def figure(values: list[float]) -> dict | None:
    if not values:
        return None
    return {"median": round(statistics.median(values), 1), "low": round(min(values), 1), "high": round(max(values), 1), "count": len(values)}


def figures(root: str) -> dict:
    out: dict = {}
    startup = []
    path = os.path.join(root, "startup.txt")
    if os.path.exists(path):
        with open(path, encoding="utf-8") as handle:
            startup = [float(line) for line in handle.read().split() if line.strip().isdigit()][1:]
    out["start-up to a usable window"] = figure(startup)

    reads, switches, stages = [], [], {}
    for name in sorted(os.listdir(root)):
        folder = os.path.join(root, name)
        if not name.startswith("run-") or not os.path.isdir(folder):
            continue
        try:
            with open(os.path.join(folder, "results.json"), encoding="utf-8") as handle:
                steps = json.load(handle)["steps"]
        except (OSError, ValueError, KeyError):
            continue
        reading = None
        for step in steps:
            do, ms, ok = step.get("do"), float(step.get("ms", 0)), bool(step.get("ok"))
            if do == "picture":
                reading = 0.0
            elif reading is not None and do in ("wait", "press") and not str(step.get("detail", "")).startswith("open-targets"):
                reading += ms if ok else 0
                if do == "wait" and ok and str(step.get("detail", "")).startswith("ResultView"):
                    reads.append(reading)
                    reading = None
            if do == "press" and ok and str(step.get("detail", "")).startswith("open-targets-row-"):
                switches.append(ms)
        log = os.path.join(folder, "perf.txt")
        if os.path.exists(log):
            with open(log, encoding="utf-8", errors="replace") as handle:
                for line in handle:
                    if (m := STAGE.search(line)) and "began=" not in line:
                        stages.setdefault(m.group(1).strip('"'), []).append(float(m.group(2)))
    out["read a 600 dpi scan, from the picture to its result"] = figure(reads)
    out["switch between three open targets"] = figure(switches)
    out["stages"] = {name: figure(values) for name, values in sorted(stages.items())}
    return out


def self_test() -> int:
    with tempfile.TemporaryDirectory() as root:
        with open(os.path.join(root, "startup.txt"), "w", encoding="utf-8") as handle:
            handle.write("900\n500\n520\n510\n")
        os.makedirs(os.path.join(root, "run-1"))
        steps = [{"do": "picture", "ok": True, "ms": 5}, {"do": "wait", "ok": True, "ms": 4000, "detail": "FeedbackView after 4.0 s"},
                 {"do": "press", "ok": True, "ms": 10, "detail": "Use this picture"}, {"do": "wait", "ok": True, "ms": 1000, "detail": "ResultView after 1.0 s"},
                 {"do": "press", "ok": True, "ms": 2, "detail": "result-open-targets"}, {"do": "press", "ok": True, "ms": 80, "detail": "open-targets-row-0"}]
        with open(os.path.join(root, "run-1", "results.json"), "w", encoding="utf-8") as handle:
            json.dump({"steps": steps}, handle)
        with open(os.path.join(root, "run-1", "perf.txt"), "w", encoding="utf-8") as handle:
            handle.write("2026-10-08T00:00:00.000Z  INFO   read.stage     stage=S5-S8 ms=700 status=ok\n"
                         "2026-10-08T00:00:00.000Z  INFO   read.stage     stage=identify began=True\n")
        said = figures(root)
        ok = (said["start-up to a usable window"] == {"median": 510.0, "low": 500.0, "high": 520.0, "count": 3}
              and said["read a 600 dpi scan, from the picture to its result"]["median"] == 5010.0
              and said["switch between three open targets"]["median"] == 80.0
              and list(said["stages"]) == ["S5-S8"])
    print("self-test", "passed" if ok else "FAILED")
    return 0 if ok else 1


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(2)
    print(json.dumps(figures(sys.argv[1]), indent=2))
