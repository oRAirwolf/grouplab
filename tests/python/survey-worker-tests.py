#!/usr/bin/env python3
"""The survey worker on reports in a folder of its own, NOTES-FROM-PLANNING.md entries 207 and 208 and docs/SURVEY.md.

It checks that each report is counted and deleted, that one machine is counted once however often it reports, that anything
smaller than ten is merged into "other" before it is published, that the benchmark's median is by class, and that something
that is not a report is set aside.

    python3 tests/python/survey-worker-tests.py
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORKER = REPO / "website" / "server" / "grouplab-survey-worker.py"

passed = 0
failed: list[str] = []


def check(what: str, ok: bool, detail: str = "") -> None:
    global passed
    if ok:
        passed += 1
        print("ok   " + what)
    else:
        failed.append(what + (": " + detail if detail else ""))
        print("FAIL " + what + (": " + detail if detail else ""))


def report(installation: int, os_text: str, memory: int, cores: int, bench: int | None = None, device: str | None = None) -> dict:
    machine = {"os": os_text, "architecture": "X64", "cores": cores, "memoryMegabytes": memory}
    if device:
        machine["device"] = device
    out = {"schema": "grouplab-survey-1", "version": "0.2.0", "machine": machine, "analyses": [],
           "installation": f"{installation:064x}", "day": datetime.now(timezone.utc).strftime("%Y-%m-%d")}
    if bench is not None:
        out["benchmark"] = {"workload": "GL-CF25-LTR-300dpi-25-holes-1", "totalMilliseconds": bench, "stages": []}
    return out


def run(root: Path) -> subprocess.CompletedProcess:
    env = dict(os.environ, GROUPLAB_SURVEY_ROOT=str(root), GROUPLAB_SURVEY_LOG=str(root / "worker.log"))
    return subprocess.run([sys.executable, str(WORKER)], env=env, capture_output=True, text=True, timeout=60)


def main() -> int:
    root = Path(tempfile.mkdtemp(prefix="grouplab-survey-"))
    try:
        incoming = root / "incoming"
        incoming.mkdir(parents=True)
        n = 0

        def put(r: dict | str) -> None:
            nonlocal n
            n += 1
            (incoming / f"2026-09-25_{n:04d}.json").write_text(r if isinstance(r, str) else json.dumps(r), encoding="utf-8")

        # Twelve Windows 11 machines with 16 GB and 8 cores, each with a benchmark; one of them reports three times.
        for i in range(12):
            put(report(i + 1, "Microsoft Windows 10.0.26200", 16000, 8, bench=1800 + i * 10))
        put(report(1, "Microsoft Windows 10.0.26200", 16000, 8, bench=1810))
        put(report(1, "Microsoft Windows 10.0.26200", 16000, 8))
        # Three Macs and two phones: fewer than ten each.
        for i in range(3):
            put(report(100 + i, "Darwin 24.1.0 Darwin Kernel Version 24.1.0", 32768, 10))
        put(report(200, "Android 15", 12000, 8, device="SM-F966U"))
        put(report(201, "Android 10", 4096, 8, device="Pixel 3"))
        put("not json at all")

        result = run(root)
        check("the worker runs", result.returncode == 0, result.stderr[-500:])
        left = sorted(p.name for p in incoming.glob("*.json"))
        check("every report is deleted once counted", left == [], str(left))
        check("something that is not a report is set aside", len(list((root / "refused").glob("*.json"))) == 1)

        public = json.loads((root / "public.json").read_text(encoding="utf-8"))
        check("one machine is counted once however often it reports", public["machines"] == 17, str(public["machines"]))
        check("a platform with ten or more machines is named", public["platforms"].get("Windows 11") == 12, str(public["platforms"]))
        check("platforms with fewer than ten are merged into other", public["platforms"].get("other") == 5
              and "macOS 15" not in public["platforms"] and "Android 15" not in public["platforms"], str(public["platforms"]))
        check("a phone model seen fewer than ten times is never named", "SM-F966U" not in json.dumps(public), json.dumps(public["phones"]))
        bench = public["benchmark"]
        check("the benchmark's median is published by class once there are ten runs", len(bench) == 1 and bench[0]["runs"] == 13
              and bench[0]["platform"] == "Windows 11" and 1750 <= bench[0]["medianMilliseconds"] <= 1900, json.dumps(bench))

        state = (root / "state.json").read_text(encoding="utf-8")
        check("the state keeps classes, not reports", "0.2.0" not in state and "X64" not in state)

        result = run(root)
        again = json.loads((root / "public.json").read_text(encoding="utf-8"))
        check("a second run with nothing new changes nothing", result.returncode == 0 and again == public)
    finally:
        shutil.rmtree(root, ignore_errors=True)

    print(f"survey worker tests: {passed} passed, {len(failed)} failed")
    for f in failed:
        print("  FAILED  " + f)
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
