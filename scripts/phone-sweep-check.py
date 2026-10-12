"""NOTES-FROM-PLANNING.md entry 352 item 1: one pass of the screen sweep, judged from the results GroupLab Dev wrote.

The same judgement the iOS simulator's sweep makes in ios-app.yml: every step of scripts/scenarios/phone-sweep.json worked, except pressing
past the picture check, which shows only when a picture needs a word, and at least eleven screenshots were taken. Prints each step on its
own line and exits non-zero, with one ::error:: line saying what was missing, when the pass did not find what it needed.

Entry 411 section 2.4: with --larger, the largest-text pass is held to larger text than the plain pass, screen by screen, from the middle
text size each screenshot's quality record carries, so GroupLab following Android's text size is seen on the emulator itself. Entry 413
section 2.2: the bar along the bottom shows its names in the plain pass and its icons alone in the largest-text pass, on the sweep's phone.

Usage: phone-sweep-check.py <results folder> <pass name>
       phone-sweep-check.py --larger <plain results folder> <largest-text results folder>
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


# The least growth that counts as following the system: Android's largest size, 2.0, grows a 16 point body to 28.
LEAST_GROWTH = 1.2


def records(folder: str, field: str) -> dict:
    """One field of each screenshot's quality record, by the record's file name, where it has one."""
    found = {}
    for name in sorted(os.listdir(folder)) if os.path.isdir(folder) else []:
        if name.endswith(".quality.json"):
            try:
                with open(os.path.join(folder, name), encoding="utf-8") as handle:
                    value = json.load(handle).get(field)
            except (OSError, ValueError, AttributeError):
                continue
            if value is not None:
                found[name] = value
    return found


def text_sizes(folder: str) -> dict[str, float]:
    """Each screenshot's middle text size, by its quality record's file name, where it has text."""
    return {name: float(size) for name, size in records(folder, "text").items()
            if isinstance(size, (int, float)) and not isinstance(size, bool) and size > 0}


def bars(folder: str, expected: str) -> list[str]:
    """The screens whose bar along the bottom is not drawn the way expected ("names" or "icons")."""
    return [name[:-len(".quality.json")] for name, bar in records(folder, "bar").items() if bar != expected]


def larger(plain: str, large: str) -> tuple[list[str], str | None]:
    """The lines to print, and the error, or None where every screen shown in both passes has larger text in the second."""
    before, after = text_sizes(plain), text_sizes(large)
    both = sorted(set(before) & set(after))
    lines = [f"text {name[:-len('.quality.json')]:<34} {before[name]:>6.2f} -> {after[name]:>6.2f}" for name in both]
    if not both:
        return lines, "The sweep's plain and largest-text passes have no screen with text in common to compare"
    flat = [name[:-len(".quality.json")] for name in both if after[name] < before[name] * LEAST_GROWTH]
    if flat:
        return lines, f"GroupLab's text did not grow with Android's largest text size on {', '.join(flat)}"
    named, iconed = bars(large, "icons"), bars(plain, "names")
    if named:
        return lines, f"The bar along the bottom still showed its names at Android's largest text size on {', '.join(named)}"
    if iconed:
        return lines, f"The bar along the bottom showed its icons alone at the plain text size on {', '.join(iconed)}"
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
    with tempfile.TemporaryDirectory() as plain, tempfile.TemporaryDirectory() as large:
        assert "no screen" in (larger(plain, large)[1] or "")
        for folder, size in ((plain, 16), (large, 28)):
            with open(os.path.join(folder, "01-capture.quality.json"), "w", encoding="utf-8") as handle:
                json.dump({"text": size, "findings": []}, handle)
        assert larger(plain, large)[1] is None
        for folder, bar in ((plain, "names"), (large, "names")):
            with open(os.path.join(folder, "02-sessions.quality.json"), "w", encoding="utf-8") as handle:
                json.dump({"text": 16 if folder == plain else 28, "bar": bar, "findings": []}, handle)
        assert "still showed its names" in (larger(plain, large)[1] or "")
        with open(os.path.join(large, "02-sessions.quality.json"), "w", encoding="utf-8") as handle:
            json.dump({"text": 28, "bar": "icons", "findings": []}, handle)
        assert larger(plain, large)[1] is None
        with open(os.path.join(large, "01-capture.quality.json"), "w", encoding="utf-8") as handle:
            json.dump({"text": 16, "findings": []}, handle)
        assert "did not grow" in (larger(plain, large)[1] or "")
    print("phone-sweep-check self-test passed")
    return 0


def main(argv: list[str]) -> int:
    if argv[1:] == ["--self-test"]:
        return self_test()
    if len(argv) == 4 and argv[1] == "--larger":
        lines, error = larger(argv[2], argv[3])
    elif len(argv) == 3:
        lines, error = judge(argv[1], argv[2])
    else:
        print(__doc__)
        return 2
    for line in lines:
        print(line)
    if error:
        print(f"::error::{error}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
