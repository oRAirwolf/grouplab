"""A macOS crash report (.ips), as the few lines that say where it stopped (request 121).

    python3 scripts/ips-summary.py <report.ips> ...

Prints the exception, the application-specific information macOS kept, the faulting thread's frames by library and symbol, and the
last exception backtrace when an Objective-C exception ended the run. Nothing about the machine or the person is printed.
"""
from __future__ import annotations

import json
import sys


def frames(thread: dict, images: list[dict], limit: int = 40) -> list[str]:
    out = []
    for f in thread.get("frames", [])[:limit]:
        image = images[f["imageIndex"]].get("name", "?") if 0 <= f.get("imageIndex", -1) < len(images) else "?"
        out.append(f"    {image}  {f.get('symbol', hex(f.get('imageOffset', 0)))}{'+' + str(f['symbolLocation']) if 'symbolLocation' in f else ''}")
    return out


def summary(path: str) -> list[str]:
    text = open(path, encoding="utf-8", errors="replace").read()
    header, _, body = text.partition("\n")
    report = json.loads(body)
    images = report.get("usedImages", [])
    lines = [f"{json.loads(header).get('os_version', '')}: {report.get('exception', {}).get('type')} {report.get('exception', {}).get('signal', '')}",
             f"  termination: {report.get('termination', {}).get('indicator', '')}",
             f"  application specific: {json.dumps(report.get('asi', {}))[:1500]}"]
    if report.get("lastExceptionBacktrace"):
        lines.append("  last exception backtrace:")
        lines += frames({"frames": report["lastExceptionBacktrace"]}, images)
    threads = report.get("threads", [])
    fault = report.get("faultingThread", 0)
    if 0 <= fault < len(threads):
        lines.append(f"  faulting thread {fault} ({threads[fault].get('queue', threads[fault].get('name', ''))}):")
        lines += frames(threads[fault], images)
    return lines


if __name__ == "__main__":
    for p in sys.argv[1:]:
        try:
            print("\n".join(summary(p)))
        except (OSError, ValueError, KeyError) as e:
            print(f"{p}: not read ({e})")
