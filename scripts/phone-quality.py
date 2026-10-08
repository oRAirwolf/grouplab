"""NOTES-FROM-PLANNING.md entry 388 section 2: the quality sweep's faults, gathered from every size and theme into one report.

scripts/android-quality.sh runs the screen sweep at each layout and copies each pass's results into a folder of its own; every screenshot
there has a <screen>.quality.json beside it (Scenario.Quality). This prints a Markdown report: one row per fault, the screen it is on and
every layout it appears in, so a fault seen at one width only stands out from one seen everywhere. It exits 0 whatever it finds: the
faults are for a person to judge, some are a layout decision, and the report says which pass wrote nothing at all.

Usage: phone-quality.py <folder holding one folder per pass>
       phone-quality.py --self-test
"""

from __future__ import annotations

import json
import os
import sys
import tempfile

KINDS = {"small": "under 44 units to press", "cut": "words cut short", "off": "past the side of the screen", "overlap": "text over text"}


def gather(root: str) -> tuple[dict, list[str]]:
    """The faults keyed by (kind, screen, what), each with the passes it appears in, and the passes that wrote no quality file."""
    faults: dict = {}
    empty = []
    for name in sorted(os.listdir(root)):
        folder = os.path.join(root, name)
        if not os.path.isdir(folder):
            continue
        files = [f for f in sorted(os.listdir(folder)) if f.endswith(".quality.json")]
        if not files:
            empty.append(name)
        for file in files:
            try:
                with open(os.path.join(folder, file), encoding="utf-8") as handle:
                    said = json.load(handle)
            except (OSError, ValueError):
                empty.append(f"{name}/{file}")
                continue
            for f in said.get("findings", []):
                what = f.get("id") or f.get("text") or f.get("type", "?")
                key = (f.get("kind", "?"), said.get("screen", file), what)
                entry = faults.setdefault(key, {"passes": [], "detail": f.get("detail"), "type": f.get("type"),
                                                "size": f"{f.get('width')} x {f.get('height')}"})
                if name not in entry["passes"]:
                    entry["passes"].append(name)
    return faults, empty


def report(root: str) -> str:
    faults, empty = gather(root)
    lines = ["# The phone's quality sweep", ""]
    if empty:
        lines += [f"Passes with no quality files: {', '.join(empty)}.", ""]
    if not faults:
        lines.append("No faults found at any size or theme.")
        return "\n".join(lines) + "\n"
    for kind, words in KINDS.items():
        rows = sorted((k, v) for k, v in faults.items() if k[0] == kind)
        if not rows:
            continue
        lines += [f"## {words.capitalize()} ({len(rows)})", "", "| Screen | Control | Size | Detail | Where |", "|---|---|---|---|---|"]
        for (_, screen, what), v in rows:
            control = f"{v['type']} {what}".replace("|", "/")
            lines.append(f"| {screen} | {control} | {v['size']} | {v['detail'] or ''} | {', '.join(v['passes'])} |")
        lines.append("")
    return "\n".join(lines) + "\n"


def self_test() -> int:
    with tempfile.TemporaryDirectory() as root:
        for name, findings in [("cover-light", [{"kind": "small", "type": "Button", "id": "x", "width": 30, "height": 30}]),
                               ("small-light", [{"kind": "small", "type": "Button", "id": "x", "width": 30, "height": 30},
                                                {"kind": "cut", "type": "TextBlock", "text": "Cut", "detail": "trimmed"}]),
                               ("inner-dark", [])]:
            os.makedirs(os.path.join(root, name))
            with open(os.path.join(root, name, "result.quality.json"), "w", encoding="utf-8") as handle:
                json.dump({"screen": "result", "findings": findings}, handle)
        os.makedirs(os.path.join(root, "nothing"))
        text = report(root)
        ok = ("| result | Button x | 30 x 30 |  | cover-light, small-light |" in text and "trimmed | small-light |" in text
              and "Passes with no quality files: nothing." in text and "Text over text" not in text)
    print("self-test", "passed" if ok else "FAILED")
    return 0 if ok else 1


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(2)
    sys.stdout.write(report(sys.argv[1]))
