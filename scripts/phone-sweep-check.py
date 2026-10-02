"""NOTES-FROM-PLANNING.md entry 352 item 1: one pass of the screen sweep, judged from the results GroupLab Dev wrote.

The same judgement the iOS simulator's sweep makes in ios-app.yml: every step of scripts/scenarios/phone-sweep.json worked, except pressing
past the picture check, which shows only when a picture needs a word, and at least eleven screenshots were taken. Prints each step on its
own line and exits non-zero, with one ::error:: line saying what was missing, when the pass did not find what it needed.

Usage: phone-sweep-check.py <results folder> <pass name>
       phone-sweep-check.py --self-test
"""

from __future__ import annotations

import json
import os
import sys
import tempfile

# The steps allowed to find nothing; PhoneSweepScenarioTests.MayFindNothing holds the same list.
MAY_FIND_NOTHING = ["Use this picture|Use it anyway"]
LEAST_SCREENSHOTS = 11


def judge(folder: str, name: str) -> tuple[list[str], str | None]:
    """The lines to print, and the error, or None where the pass found everything it needed."""
    path = os.path.join(folder, "results.json")
    if not os.path.exists(path):
        return [], f"The sweep's {name} pass wrote no results"
    try:
        with open(path, encoding="utf-8") as handle:
            results = json.load(handle)
        steps = results["steps"]
    except (OSError, ValueError, KeyError, TypeError) as error:
        return [], f"The sweep's {name} pass wrote results that could not be read ({type(error).__name__})"

    lines, bad = [], []
    for step in steps:
        ok = bool(step.get("ok"))
        detail = str(step.get("detail", ""))
        lines.append(f"{name:<13}{step.get('step', '?'):>3} {step.get('do', '?'):<10} {'ok' if ok else 'FAILED':<6} "
                     f"{step.get('ms', 0):>7} ms  {detail}")
        if not ok and not any(m in detail for m in MAY_FIND_NOTHING):
            bad.append(f"{step.get('step', '?')} {step.get('do', '?')}: {detail}")
    shots = [f for f in os.listdir(folder) if f.endswith(".png")]
    lines.append(f"{name}: {len(shots)} screenshots")
    if bad or len(shots) < LEAST_SCREENSHOTS:
        return lines, f"The sweep's {name} pass did not find what it needed: {'; '.join(bad) or 'too few screenshots'}"
    return lines, None


def self_test() -> int:
    with tempfile.TemporaryDirectory() as folder:
        assert judge(folder, "plain")[1] == "The sweep's plain pass wrote no results"
        steps = [{"step": 1, "do": "open", "ok": True, "ms": 5, "detail": "capture"},
                 {"step": 2, "do": "press", "ok": False, "ms": 5, "detail": "nothing named Use this picture|Use it anyway"}]
        with open(os.path.join(folder, "results.json"), "w", encoding="utf-8") as handle:
            json.dump({"steps": steps}, handle)
        assert "too few screenshots" in (judge(folder, "plain")[1] or "")
        for i in range(LEAST_SCREENSHOTS):
            open(os.path.join(folder, f"{i:02}.png"), "wb").close()
        assert judge(folder, "plain")[1] is None
        steps.append({"step": 3, "do": "press", "ok": False, "ms": 5, "detail": "nothing named result-shots"})
        with open(os.path.join(folder, "results.json"), "w", encoding="utf-8") as handle:
            json.dump({"steps": steps}, handle)
        assert "3 press: nothing named result-shots" in (judge(folder, "dark")[1] or "")
        with open(os.path.join(folder, "results.json"), "w", encoding="utf-8") as handle:
            handle.write("{\"steps\": [")
        assert "could not be read" in (judge(folder, "dark")[1] or "")
    print("phone-sweep-check self-test passed")
    return 0


def main(argv: list[str]) -> int:
    if argv[1:] == ["--self-test"]:
        return self_test()
    if len(argv) != 3:
        print(__doc__)
        return 2
    lines, error = judge(argv[1], argv[2])
    for line in lines:
        print(line)
    if error:
        print(f"::error::{error}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
