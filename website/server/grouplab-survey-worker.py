#!/usr/bin/env python3
"""Count GroupLab's hardware survey reports into an aggregate, NOTES-FROM-PLANNING.md entries 207 and 208 and docs/SURVEY.md.

The receiver, website/api/survey.php, writes each report it accepts into private/survey/incoming, already cut down to its
schema, with the installation number replaced by a salted hash and the time by the day. This counts each one and deletes it.

**Counts, not records** (docs/SURVEY.md section 5). What is kept is, for each installation hash, the classes its machine falls
in and the day it was last seen, so one machine is counted once and a machine that changes is counted where it is now; and,
for each class of machine, how many benchmark runs fell in each quarter second. No report is kept once counted, and none is
kept longer than thirty days whatever happens (entries 215 and 216). A machine not seen for 180 days is dropped.

**What is published** is written to public.json beside the state: shares of platforms, memory, cores and phone models, and
the benchmark's median by class, with the date range and the number of machines. Any group smaller than ten is merged into
"other", so no one machine can be picked out.

**Every report is untrusted.** Anybody can send one. Nothing in it is acted on; its strings are only ever sorted into classes.

It needs no network, and its unit gives it none.
"""

from __future__ import annotations

import json
import os
import re
import sys
import time
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(os.environ.get("GROUPLAB_SURVEY_ROOT", "/home/airwolf/web/grouplab.org/private/survey"))
INCOMING = ROOT / "incoming"
REFUSED = ROOT / "refused"
STATE = ROOT / "state.json"
PUBLIC = ROOT / "public.json"
LOG = Path(os.environ.get("GROUPLAB_SURVEY_LOG", "/home/airwolf/logs/grouplab-survey-worker.log"))

MOST_A_RUN = 2000
INCOMING_DAYS = 30
REFUSED_DAYS = 7
MACHINE_DAYS = 180
SMALLEST_GROUP = 10
BUCKET_MS = 250


def log(message: str) -> None:
    line = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ") + " " + message
    try:
        LOG.parent.mkdir(parents=True, exist_ok=True)
        with LOG.open("a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass
    print(line, flush=True)


def platform(os_text: str) -> str:
    """The platform and its main version, from the operating system's own description."""
    s = os_text.strip()
    if s.startswith("Android"):
        m = re.match(r"Android (\d+)", s)
        return f"Android {m.group(1)}" if m else "Android"
    if "Windows" in s:
        m = re.search(r"10\.0\.(\d+)", s)
        return "Windows 11" if m and int(m.group(1)) >= 22000 else "Windows 10" if m else "Windows"
    if s.startswith("Darwin") or "macOS" in s:
        m = re.search(r"Darwin (\d+)", s)
        return f"macOS {int(m.group(1)) - 9}" if m and int(m.group(1)) >= 20 else "macOS"
    if s.startswith("Linux") or "Linux" in s:
        return "Linux"
    return "other"


def memory(megabytes) -> str:
    if not isinstance(megabytes, int) or megabytes <= 0:
        return "unknown"
    gb = megabytes / 1024
    for top, name in ((3.5, "under 4 GB"), (7, "4 to 8 GB"), (14, "8 to 16 GB"), (30, "16 to 32 GB")):
        if gb < top:
            return name
    return "32 GB or more"


def cores(n) -> str:
    if not isinstance(n, int) or n <= 0:
        return "unknown"
    for top, name in ((2, "1 or 2"), (4, "3 or 4"), (8, "5 to 8"), (16, "9 to 16")):
        if n <= top:
            return name
    return "more than 16"


def classes(report: dict) -> dict:
    machine = report.get("machine") if isinstance(report.get("machine"), dict) else {}
    out = {
        "platform": platform(str(machine.get("os", ""))),
        "memory": memory(machine.get("memoryMegabytes")),
        "cores": cores(machine.get("cores")),
    }
    device = machine.get("device")
    if isinstance(device, str) and device.strip():
        out["device"] = device.strip()[:60]
    return out


def count(report: dict, state: dict) -> None:
    installation = report.get("installation")
    day = report.get("day")
    if not isinstance(installation, str) or not re.fullmatch(r"[0-9a-f]{64}", installation):
        raise ValueError("no installation hash")
    if not isinstance(day, str) or not re.fullmatch(r"\d{4}-\d{2}-\d{2}", day):
        raise ValueError("no day")
    seen = classes(report)
    seen["last"] = day
    state.setdefault("machines", {})[installation] = seen
    state["first"] = min(state.get("first", day), day)
    state["last"] = max(state.get("last", day), day)
    state["reports"] = state.get("reports", 0) + 1
    bench = report.get("benchmark")
    if isinstance(bench, dict) and isinstance(bench.get("totalMilliseconds"), int) and isinstance(bench.get("workload"), str):
        key = f"{bench['workload']}|{seen['platform']}|{seen['cores']}"
        bucket = str(bench["totalMilliseconds"] // BUCKET_MS * BUCKET_MS)
        buckets = state.setdefault("benchmarks", {}).setdefault(key, {})
        buckets[bucket] = buckets.get(bucket, 0) + 1


def merged(counts: dict[str, int]) -> dict[str, int]:
    """Groups smaller than ten go into "other", and "other" too when it is still under ten."""
    out: dict[str, int] = {}
    other = 0
    for name, n in sorted(counts.items(), key=lambda kv: (-kv[1], kv[0])):
        if n < SMALLEST_GROUP or name == "other":
            other += n
        else:
            out[name] = n
    if other:
        out["other"] = other
    return out


def median(buckets: dict[str, int]) -> int | None:
    total = sum(buckets.values())
    if total == 0:
        return None
    seen = 0
    for start in sorted(buckets, key=int):
        seen += buckets[start]
        if seen * 2 >= total:
            return int(start) + BUCKET_MS // 2
    return None


def publish(state: dict) -> dict:
    machines = list(state.get("machines", {}).values())
    tally: dict[str, dict[str, int]] = {"platform": {}, "memory": {}, "cores": {}, "device": {}}
    for m in machines:
        for field in tally:
            if field in m:
                tally[field][m[field]] = tally[field].get(m[field], 0) + 1
    benchmarks = []
    for key, buckets in sorted(state.get("benchmarks", {}).items()):
        runs = sum(buckets.values())
        if runs >= SMALLEST_GROUP:
            workload, plat, core = key.split("|", 2)
            benchmarks.append({"workload": workload, "platform": plat, "cores": core, "runs": runs, "medianMilliseconds": median(buckets)})
    return {
        "machines": len(machines),
        "from": state.get("first"),
        "to": state.get("last"),
        "platforms": merged(tally["platform"]),
        "memory": merged(tally["memory"]),
        "cores": merged(tally["cores"]),
        "phones": merged(tally["device"]),
        "benchmark": benchmarks,
    }


def forget_old_machines(state: dict, today: datetime) -> None:
    cutoff = (today - timedelta(days=MACHINE_DAYS)).strftime("%Y-%m-%d")
    machines = state.get("machines", {})
    for key in [k for k, m in machines.items() if m.get("last", "") < cutoff]:
        del machines[key]


def write(path: Path, data: dict) -> None:
    tmp = path.with_suffix(".tmp")
    tmp.write_text(json.dumps(data, indent=1, sort_keys=True) + "\n", encoding="utf-8")
    tmp.replace(path)


def sweep() -> None:
    """Incoming reports older than thirty days, and refused files older than seven, are deleted with their names in the log."""
    now = time.time()
    for place, days, what in ((INCOMING, INCOMING_DAYS, "never counted"), (REFUSED, REFUSED_DAYS, "set aside as not a report")):
        for path in place.glob("*.json") if place.is_dir() else []:
            if now - path.stat().st_mtime > days * 86400:
                path.unlink(missing_ok=True)
                log(f"{path.name}: deleted after {days} days, {what}")


def main() -> int:
    sweep()
    if not INCOMING.is_dir():
        log("nothing to do: there is no incoming folder yet")
        return 0
    state = json.loads(STATE.read_text(encoding="utf-8")) if STATE.is_file() else {}
    done = 0
    for path in sorted(p for p in INCOMING.glob("*.json") if not p.name.startswith("."))[:MOST_A_RUN]:
        try:
            report = json.loads(path.read_text(encoding="utf-8"))
            if not isinstance(report, dict) or report.get("schema") != "grouplab-survey-1":
                raise ValueError("not a report")
            count(report, state)
        except (OSError, ValueError) as e:
            REFUSED.mkdir(parents=True, exist_ok=True)
            path.replace(REFUSED / path.name)
            log(f"{path.name}: refused, {type(e).__name__}")
            continue
        write(STATE, state)
        path.unlink()
        done += 1
    forget_old_machines(state, datetime.now(timezone.utc))
    write(STATE, state)
    write(PUBLIC, publish(state))
    if done:
        log(f"{done} reports counted; {len(state.get('machines', {}))} machines")
    return 0


if __name__ == "__main__":
    sys.exit(main())
