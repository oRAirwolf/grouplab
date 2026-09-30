#!/usr/bin/env python3
"""The iOS self-test's verdict, NOTES-FROM-PLANNING.md entry 290 section 2 item 4.

    python3 ios/selftest.py <ios results.json> [<desktop results.json>] [--screens <folder>]

The simulator's results (written by the head's SelfTest into Documents/selftest/results.json) and, where the desktop reference ran, the
desktop's (ios/SelfTestReference, the same checks from the same code). It prints one line a check, writes a table to the GitHub step summary
when there is one, and fails when:

- a check on the simulator failed (a skipped one, where this build had no OpenCV, is a notice and not a failure);
- a number both measured differs by more than the tolerance below, or the shots found differ in number, bull or place;
- a place along the bottom was not photographed, where --screens names the folder the screenshots went to;
- where the tour ran, Compare did not draw a group for each of two loads with its holes, or was not photographed (entry 301);
- the idle screen's screenshot is not black from edge to edge (entry 290 section 2 item 6): behind the status bar and the home indicator
  too, allowing iOS's own clock, battery and indicator drawn over it.

The tolerance is the gate record's printed precision: the phase 0 gate record (.github/workflows/gate-record.yml) prints lengths to three
decimals of an inch, so two platforms agree when every length agrees to within 0.001 in. Pixel and count figures are held to the same 0.001,
or one part in a thousand of a large one. Time is never compared.
"""

from __future__ import annotations

import json
import os
import struct
import sys
import zlib
from pathlib import Path

TOLERANCE = 0.001
NOT_COMPARED = {"seconds"}
PLACES = ["capture", "sessions", "ballistics", "targets", "settings"]


def png_rows(path: Path) -> tuple[list[bytes], int]:
    """An 8-bit, non-interlaced PNG's unfiltered rows and its bytes a pixel, with nothing but the standard library."""
    data = path.read_bytes()
    pos, idat, width, height, channels = 8, b"", 0, 0, 0
    while pos < len(data):
        length, kind = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height, depth, colour, _, _, interlace = struct.unpack(">IIBBBBB", body)
            if depth != 8 or interlace:
                raise ValueError(f"{path.name}: a {depth}-bit{' interlaced' if interlace else ''} PNG is not read here")
            channels = {0: 1, 2: 3, 4: 2, 6: 4}[colour]
        elif kind == b"IDAT":
            idat += body
        pos += 12 + length
    raw, stride, rows, previous = zlib.decompress(idat), width * channels, [], bytearray(width * channels)
    for y in range(height):
        kind, line = raw[y * (stride + 1)], bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        for i in range(stride):
            left = line[i - channels] if i >= channels else 0
            up = previous[i]
            corner = previous[i - channels] if i >= channels else 0
            if kind == 1:
                line[i] = (line[i] + left) & 255
            elif kind == 2:
                line[i] = (line[i] + up) & 255
            elif kind == 3:
                line[i] = (line[i] + (left + up) // 2) & 255
            elif kind == 4:
                guess = left + up - corner
                pa, pb, pc = abs(guess - left), abs(guess - up), abs(guess - corner)
                line[i] = (line[i] + (left if pa <= pb and pa <= pc else up if pb <= pc else corner)) & 255
        rows.append(bytes(line))
        previous = line
    return rows, channels


def black_share(rows: list[bytes], channels: int) -> float:
    """The share of pixels whose brightest colour is under 24 of 255."""
    colours = min(channels, 3)
    dark = total = 0
    for row in rows:
        for x in range(0, len(row), channels):
            total += 1
            dark += max(row[x:x + colours]) < 24
    return dark / total if total else 0.0


def idle_problems(shot: Path) -> list[str]:
    """Entry 290 section 2 item 6: the idle screen black to every edge, the status bar's strip and the home indicator's included."""
    rows, channels = png_rows(shot)
    strip = max(1, len(rows) * 6 // 100)
    parts = {"the whole screen": (rows, 0.97), "the strip behind the status bar": (rows[:strip], 0.85),
             "the strip behind the home indicator": (rows[-strip:], 0.93)}
    found = []
    for name, (part, least) in parts.items():
        share = black_share(part, channels)
        print(f"idle screen: {name} is {share:.1%} black")
        if share < least:
            found.append(f"the idle screen is not black edge to edge: {name} is {share:.1%} black, under {least:.0%}")
    return found


def close(a: float, b: float) -> bool:
    return abs(a - b) <= max(TOLERANCE, TOLERANCE * max(abs(a), abs(b)))


def main(argv: list[str]) -> int:
    screens = None
    if "--screens" in argv:
        i = argv.index("--screens")
        screens = Path(argv[i + 1])
        argv = argv[:i] + argv[i + 2:]
    if not argv:
        print(__doc__)
        return 2
    ios = json.loads(Path(argv[0]).read_text(encoding="utf-8"))
    desktop = json.loads(Path(argv[1]).read_text(encoding="utf-8")) if len(argv) > 1 and Path(argv[1]).exists() else None
    theirs = {c["name"]: c for c in desktop["checks"]} if desktop else {}

    failures: list[str] = []
    rows: list[str] = []
    for check in ios["checks"]:
        name = check["name"]
        state = "skipped" if check["skipped"] else "passed" if check["passed"] else "FAILED"
        if state == "FAILED":
            failures.append(f"{name}: {check['detail']}")
        compared = ""
        other = theirs.get(name)
        if other is not None and state != "FAILED":
            worst = 0.0
            differing = []
            for key, value in check["numbers"].items():
                if key in NOT_COMPARED or key not in other["numbers"]:
                    continue
                d = abs(value - other["numbers"][key])
                worst = max(worst, d)
                if not close(value, other["numbers"][key]):
                    differing.append(f"{key} {value} against {other['numbers'][key]}")
            mine, its = check["shots"], other["shots"]
            if mine or its:
                if len(mine) != len(its) or any(a[0] != b[0] for a, b in zip(mine, its)):
                    differing.append(f"{len(mine)} shots against {len(its)}, or on other bulls")
                else:
                    for a, b in zip(mine, its):
                        d = max(abs(a[1] - b[1]), abs(a[2] - b[2]))
                        worst = max(worst, d)
                        if d > TOLERANCE:
                            differing.append(f"the shot on bull {a[0]} at {a[1]}, {a[2]} against {b[1]}, {b[2]}")
            compared = f"largest difference {worst:.6f}"
            if differing:
                failures.append(f"{name} differs from the desktop: " + "; ".join(differing[:6]))
                compared += ", DIFFERS: " + "; ".join(differing[:3])
            else:
                compared += ", the same as the desktop"
        print(f"{state:8} {name}: {check['detail']}" + (f" [{compared}]" if compared else ""))
        rows.append(f"| {name} | {state} | {check['detail']} | {compared or 'iOS only'} |")

    names = {c["name"]: c for c in ios["checks"]}
    if "chosen picture" in names and "sample pipeline" in names:
        a = names["chosen picture"]["numbers"].get("meanRadius")
        b = names["sample pipeline"]["numbers"].get("meanRadius")
        if a is not None and b is not None and not close(a, b):
            failures.append(f"the chosen picture's saved mean radius {a} is not the pipeline's {b}")

    # Entry 301: where the tour ran, Compare drew each of two loads' groups with its holes, and was photographed.
    if ios.get("opencv"):
        groups = names.get("compare groups")
        if groups is None:
            failures.append("Compare's drawn groups were not checked")
        elif groups["numbers"].get("loads", 0) < 2 or groups["numbers"].get("holes", 0) < 6:
            failures.append(f"Compare drew {groups['numbers'].get('loads', 0)} loads' groups with {groups['numbers'].get('holes', 0)} holes, not two with three or more each")
        else:
            print(f"compare groups: {groups['detail']}")
        if screens is not None and not (screens / "97-compare-groups.png").exists():
            failures.append("no screenshot of Compare's drawn groups")

    if screens is not None:
        taken = sorted(p.name for p in screens.glob("*.png"))
        for place in PLACES:
            if not any(n.endswith(f"-{place}.png") for n in taken):
                failures.append(f"no screenshot of {place}")
        print("screenshots: " + ", ".join(taken))
        idle = screens / "90-idle.png"
        if idle.exists():
            failures.extend(idle_problems(idle))
        else:
            failures.append("no screenshot of the idle screen")

    if not ios.get("opencv"):
        print("::notice::This build had no OpenCV for iOS, so the imaging and pipeline checks were skipped.")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as out:
            out.write(f"### iOS self-test on the simulator ({ios['platform']}, budget {ios['budgetMb']} MB)\n\n")
            if desktop:
                out.write(f"Compared with {desktop['platform']} to within {TOLERANCE}.\n\n")
            out.write("| Check | Result | What it measured | Against the desktop |\n|---|---|---|---|\n")
            out.write("\n".join(rows) + "\n\n")
            out.write(("**Failed:** " + " / ".join(failures)) if failures else "Every check passed.")
            out.write("\n")

    for failure in failures:
        print(f"::error::{failure}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
