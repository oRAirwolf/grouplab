#!/usr/bin/env python3
"""Count GroupLab's hardware survey reports into an aggregate, NOTES-FROM-PLANNING.md entries 207, 208 and 241 and docs/SURVEY.md.

The receiver, website/api/survey.php, writes each report it accepts into private/survey/incoming, already cut down to its
schema, with the installation number replaced by a keyed hash and the time by the day. This counts each one and deletes it.

**One machine, one vote a version** (entry 241). What is kept, for each installation hash, is the classes its machine falls in,
the month it was last seen, and for each version of GroupLab and each benchmark workload, how many runs it has sent and how its
times fall in quarter seconds (each stage in twentieths of a second), from which its median is read. The runs themselves, their
days and their order are not kept once counted. A machine not seen for twelve months is dropped, and a request to delete, which
the receiver stores as the hash alone, drops it at once. No report is kept once counted, and none longer than thirty days
whatever happens (entries 215 and 216).

**What is published** is written to public.json beside the state, and copied into the site where the survey page reads it:
shares of platforms, memory, cores and phone models, and the benchmark by class, as the median of the machines' own medians,
never of runs, with how many runs each median rests on. Any group smaller than ten machines is merged into "other" or not
shown, so no one machine can be picked out.

**Every report is untrusted.** Anybody can send one. Nothing in it is acted on; its strings are only ever sorted into classes.

It needs no network, and its unit gives it none.
"""

from __future__ import annotations

import json
import os
import re
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(os.environ.get("GROUPLAB_SURVEY_ROOT", "/home/airwolf/web/grouplab.org/private/survey"))
INCOMING = ROOT / "incoming"
REFUSED = ROOT / "refused"
STATE = ROOT / "state.json"
PUBLIC = ROOT / "public.json"
# Entry 241 section 5: the page at grouplab.org/survey/ reads this copy. The site sync leaves it in place.
SITE_COPY = Path(os.environ.get("GROUPLAB_SURVEY_SITE", "/home/airwolf/web/grouplab.org/public_html/survey/aggregate.json"))
LOG = Path(os.environ.get("GROUPLAB_SURVEY_LOG", "/home/airwolf/logs/grouplab-survey-worker.log"))

STATE_VERSION = 2
MOST_A_RUN = 2000
INCOMING_DAYS = 30
REFUSED_DAYS = 7
MACHINE_MONTHS = 12
SMALLEST_GROUP = 10
BUCKET_MS = 250
STAGE_BUCKET_MS = 50
REPORTS = ("grouplab-survey-1", "grouplab-survey-2")
DELETE = "grouplab-survey-delete-1"


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


def runs_of(report: dict) -> list[dict]:
    """The benchmark runs a report carries: the second schema's list, or the first schema's one run under the report's version."""
    version = report.get("version") if isinstance(report.get("version"), str) else "unknown"
    sent = report.get("benchmarks")
    if not isinstance(sent, list):
        sent = [report["benchmark"]] if isinstance(report.get("benchmark"), dict) else []
    out = []
    for b in sent[:10]:
        if isinstance(b, dict) and isinstance(b.get("totalMilliseconds"), int) and isinstance(b.get("workload"), str):
            ran = b.get("version") if isinstance(b.get("version"), str) and b.get("version") else version
            out.append({"version": ran[:64], "workload": b["workload"][:60], "total": b["totalMilliseconds"],
                        "stages": [(s["stage"][:60], s["milliseconds"]) for s in b.get("stages", []) if isinstance(s, dict)
                                   and isinstance(s.get("stage"), str) and isinstance(s.get("milliseconds"), int)]})
    return out


def add(buckets: dict, value: int, width: int) -> None:
    key = str(value // width * width)
    buckets[key] = buckets.get(key, 0) + 1


def median(buckets: dict[str, int], width: int) -> int | None:
    total = sum(buckets.values())
    if total == 0:
        return None
    seen = 0
    for start in sorted(buckets, key=int):
        seen += buckets[start]
        if seen * 2 >= total:
            return int(start) + width // 2
    return None


def middle(values: list[int]) -> int | None:
    if not values:
        return None
    s = sorted(values)
    n = len(s)
    return s[n // 2] if n % 2 else (s[n // 2 - 1] + s[n // 2]) // 2


def count(report: dict, state: dict) -> None:
    installation = report.get("installation")
    day = report.get("day")
    if not isinstance(installation, str) or not re.fullmatch(r"[0-9a-f]{64}", installation):
        raise ValueError("no installation hash")
    if not isinstance(day, str) or not re.fullmatch(r"\d{4}-\d{2}-\d{2}", day):
        raise ValueError("no day")
    machines = state.setdefault("machines", {})
    if report.get("schema") == DELETE:
        if machines.pop(installation, None) is not None:
            state["deleted"] = state.get("deleted", 0) + 1
        return
    machine = machines.get(installation, {})
    versions = machine.get("versions", {})
    machine = classes(report)
    machine["month"] = day[:7]
    for run in runs_of(report):
        kept = versions.setdefault(run["version"], {}).setdefault(run["workload"], {"runs": 0, "total": {}, "stages": {}})
        kept["runs"] += 1
        add(kept["total"], run["total"], BUCKET_MS)
        for stage, ms in run["stages"]:
            add(kept["stages"].setdefault(stage, {}), ms, STAGE_BUCKET_MS)
    if versions:
        machine["versions"] = versions
    machines[installation] = machine
    state["first"] = min(state.get("first", day), day)
    state["last"] = max(state.get("last", day), day)
    state["reports"] = state.get("reports", 0) + 1


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


def publish(state: dict) -> dict:
    machines = list(state.get("machines", {}).values())
    tally: dict[str, dict[str, int]] = {"platform": {}, "memory": {}, "cores": {}, "device": {}}
    for m in machines:
        for field in tally:
            if field in m:
                tally[field][m[field]] = tally[field].get(m[field], 0) + 1

    # Each machine once a version, by the median of its own runs; a class is published from ten such medians or more.
    groups: dict[tuple, list[tuple[int, int, dict]]] = {}
    for m in machines:
        for version, workloads in m.get("versions", {}).items():
            for workload, kept in workloads.items():
                own = median(kept["total"], BUCKET_MS)
                if own is not None:
                    stages = {s: median(b, STAGE_BUCKET_MS) for s, b in kept["stages"].items()}
                    groups.setdefault((workload, m["platform"], m["cores"]), []).append((own, kept["runs"], stages))
    benchmarks = []
    for (workload, plat, core), votes in sorted(groups.items()):
        if len(votes) < SMALLEST_GROUP:
            continue
        names = sorted({s for _, _, st in votes for s in st})
        benchmarks.append({
            "workload": workload, "platform": plat, "cores": core, "machines": len(votes),
            "medianMilliseconds": middle([v for v, _, _ in votes]),
            "runsPerMachine": [min(r for _, r, _ in votes), max(r for _, r, _ in votes)],
            "stageMedians": {s: middle([st[s] for _, _, st in votes if st.get(s) is not None]) for s in names},
        })
    counted = sum(len(v) for v in groups.values())
    return {
        "machines": len(machines),
        "reports": state.get("reports", 0),
        "from": state.get("first"),
        "to": state.get("last"),
        "platforms": merged(tally["platform"]),
        "memory": merged(tally["memory"]),
        "cores": merged(tally["cores"]),
        "phones": merged(tally["device"]),
        "benchmark": benchmarks,
        "benchmarkMachines": counted,
        "smallestGroup": SMALLEST_GROUP,
    }


def forget_old_machines(state: dict, today: datetime) -> None:
    month = today.year * 12 + today.month - 1 - MACHINE_MONTHS
    cutoff = f"{month // 12:04d}-{month % 12 + 1:02d}"
    machines = state.get("machines", {})
    for key in [k for k, m in machines.items() if m.get("month", "") < cutoff]:
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


def load_state() -> dict:
    """The state, begun again when it is the first version's: its machines were keyed by a hash the receiver no longer makes (entry 241)."""
    if not STATE.is_file():
        return {"version": STATE_VERSION}
    state = json.loads(STATE.read_text(encoding="utf-8"))
    if state.get("version") != STATE_VERSION:
        old = ROOT / "state-1.json"
        STATE.replace(old)
        log(f"the first version's state was set aside as {old.name}; counting begins again under the keyed hash")
        return {"version": STATE_VERSION}
    return state


def main() -> int:
    sweep()
    if not INCOMING.is_dir():
        log("nothing to do: there is no incoming folder yet")
        return 0
    state = load_state()
    done = 0
    for path in sorted(p for p in INCOMING.glob("*.json") if not p.name.startswith("."))[:MOST_A_RUN]:
        try:
            report = json.loads(path.read_text(encoding="utf-8"))
            if not isinstance(report, dict) or report.get("schema") not in (*REPORTS, DELETE):
                raise ValueError("not a report")
            count(report, state)
        except (OSError, ValueError, KeyError, TypeError) as e:
            REFUSED.mkdir(parents=True, exist_ok=True)
            path.replace(REFUSED / path.name)
            log(f"{path.name}: refused, {type(e).__name__}")
            continue
        write(STATE, state)
        path.unlink()
        done += 1
    forget_old_machines(state, datetime.now(timezone.utc))
    write(STATE, state)
    public = publish(state)
    write(PUBLIC, public)
    if SITE_COPY.parent.is_dir():
        try:
            write(SITE_COPY, public)
        except OSError as e:
            log(f"the site's copy could not be written: {type(e).__name__}")
    if done:
        log(f"{done} reports counted; {len(state.get('machines', {}))} machines")
    return 0


if __name__ == "__main__":
    sys.exit(main())
