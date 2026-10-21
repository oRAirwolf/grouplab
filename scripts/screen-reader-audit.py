#!/usr/bin/env python3
"""NOTES-FROM-PLANNING.md entry 461 section 2.1: what TalkBack and VoiceOver would find, checked by machine on the emulator and the simulator.

GroupLab Dev runs scripts/scenarios/phone-audit.json, the screen sweep's screens; at each "audit" step it stops and writes results/audit.json,
the screen's name and every control showing with its place (mobile/GroupLab.Mobile/Dev/Scenario.cs, Audit). This script reads the same
screen with the platform's own tooling, keeps what it found in the output folder, writes results/audited.json and takes the audit away:

- Android: the accessibility node tree TalkBack reads, from uiautomator. Every node that can be tapped has a name (its text or its
  description), a role (a class that says what it is), and a touch target of at least 48 by 48 dp, the Accessibility Test Framework's
  rule; and every control GroupLab Dev says is showing and can be tapped has a node there, so nothing that can be tapped is hidden.
- iOS: XCTest's own accessibility audit (performAccessibilityAudit, Xcode 15 and later), run by the XCUITest runner in scripts/touch/ios
  on GroupLab Dev as it shows. Element detection, hit regions, descriptions and traits are judged; contrast,
  Dynamic Type and clipped text are kept and listed, since they are questions of look and not of what a screen reader can reach.

  screen-reader-audit.py android --package org.grouplab.app.dev --out <folder> [--seconds 1200]
  screen-reader-audit.py ios --data <GroupLab Dev's data container> --taps <the runner's folder> --out <folder> [--pid <simctl launch's>]
  screen-reader-audit.py judge <folder>
  screen-reader-audit.py --self-test
"""

import importlib.util
import json
import os
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ElementTree

AUDIT = "audit.json"
AUDITED = "audited.json"
SMALLEST_DP = 48
# What a person taps, types into or chooses, by the control's type as GroupLab Dev names it.
INTERACTIVE = {"Button", "ToggleButton", "RadioButton", "CheckBox", "TextBox", "ComboBox", "ToggleSwitch", "Slider", "NumericUpDown",
               "ListBoxItem", "TabItem", "AutoCompleteBox"}
# Android classes that say nothing about what a node does.
NO_ROLE = {"", "android.view.View", "android.view.ViewGroup", "android.widget.FrameLayout"}
# XCTest's audit types that are about reaching and understanding a control, judged; the rest are kept and listed.
IOS_JUDGED = {"elementDetection", "hitRegion", "sufficientElementDescription", "trait"}


def _touch():
    """scripts/touch-test.py, whose device classes read and write GroupLab Dev's results folder; its name has a hyphen, so by path."""
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "touch-test.py")
    spec = importlib.util.spec_from_file_location("touch_test", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def bounds(text):
    """uiautomator's "[x1,y1][x2,y2]" as x, y, width, height."""
    m = re.fullmatch(r"\[(-?\d+),(-?\d+)\]\[(-?\d+),(-?\d+)\]", text or "")
    if not m:
        return None
    x1, y1, x2, y2 = (int(v) for v in m.groups())
    return x1, y1, x2 - x1, y2 - y1


def overlap(a, b):
    """The share of box a that box b covers."""
    x = max(0, min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]))
    y = max(0, min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1]))
    return (x * y) / (a[2] * a[3]) if a[2] > 0 and a[3] > 0 else 0


def android_findings(audit, xml_text, dpi, package):
    """Every finding on one screen, from GroupLab Dev's record of it and uiautomator's tree of it."""
    root = ElementTree.fromstring(xml_text)
    parents = {child: parent for parent in root.iter() for child in parent}
    nodes = []
    for node in root.iter("node"):
        if node.get("package") != package:
            continue
        # A scroll bar's own arrows and thumb are the platform's, read by TalkBack as the scroll they are part of, as on the computer.
        above, inside_scroll_bar = parents.get(node), False
        while above is not None:
            inside_scroll_bar |= above.get("class") == "ScrollBar"
            above = parents.get(above)
        if inside_scroll_bar:
            continue
        box = bounds(node.get("bounds"))
        if box is None or box[2] <= 0 or box[3] <= 0:
            continue
        nodes.append((node, box))

    found = []
    scale = 160 / dpi
    # The view's own edges: a control scrolled part way out of it is only the sliver that shows, so its size is not judged, as the
    # Accessibility Test Framework does not judge a control the screen cuts off.
    own = [b for n, b in nodes if not (n.get("class") or "").startswith("android.")] or [b for _, b in nodes]
    top = min((b[1] for b in own), default=0)
    bottom = max((b[1] + b[3] for b in own), default=0)
    for node, box in nodes:
        if node.get("clickable") != "true" and node.get("long-clickable") != "true":
            continue
        name = (node.get("text") or "").strip() or (node.get("content-desc") or "").strip()
        what = f"{node.get('class') or 'a node'} \"{name}\" at {box[0]},{box[1]}"
        if not name:
            found.append(f"{what}: no name")
        if (node.get("class") or "") in NO_ROLE:
            found.append(f"{what}: no role, its class is {node.get('class') or 'empty'}")
        width, height = box[2] * scale, box[3] * scale
        cut_off = box[1] <= top + 1 or box[1] + box[3] >= bottom - 1
        if not cut_off and (width < SMALLEST_DP - 0.5 or height < SMALLEST_DP - 0.5):
            found.append(f"{what}: {width:.0f} by {height:.0f} dp, smaller than {SMALLEST_DP} by {SMALLEST_DP} to touch")

    view = audit.get("view") or {}
    covered = audit.get("coveredFrom")
    for control in audit.get("controls", []):
        if control.get("type") not in INTERACTIVE or not control.get("enabled"):
            continue
        s = control.get("screen") or {}
        box = (s.get("x", 0), s.get("y", 0), s.get("width", 0), s.get("height", 0))
        if box[2] < 2 or box[3] < 2:
            continue
        if view and not (box[0] >= view["x"] - 1 and box[1] >= view["y"] - 1 and box[0] + box[2] <= view["x"] + view["width"] + 1
                         and box[1] + box[3] <= view["y"] + view["height"] + 1):
            continue
        if covered is not None and box[1] + box[3] > covered:
            continue
        area = box[2] * box[3]
        if not any(overlap(box, n) >= 0.5 and n[2] * n[3] <= 4 * area for _, n in nodes):
            label = control.get("name") or control.get("text") or control.get("id") or ""
            found.append(f"{control.get('type')} \"{label}\" at {box[0]:.0f},{box[1]:.0f}: hidden from TalkBack's tree, though it can be tapped")
    return found


def density(run):
    """The emulator's dots an inch, the override where one is set."""
    said = run("shell", "wm", "density").stdout
    override = re.search(r"Override density:\s*(\d+)", said)
    physical = re.search(r"Physical density:\s*(\d+)", said)
    return int((override or physical).group(1)) if (override or physical) else 160


def answer_android(device, audit, out):
    run = device._run
    name = audit.get("name", "screen")
    xml_text = None
    for _ in range(4):
        dumped = run("shell", "uiautomator", "dump", "/data/local/tmp/gl-window.xml")
        said = run("exec-out", "cat", "/data/local/tmp/gl-window.xml")
        if said.returncode == 0 and "<hierarchy" in said.stdout:
            xml_text = said.stdout
            break
        time.sleep(2)
    run("shell", "rm", "-f", "/data/local/tmp/gl-window.xml")
    if xml_text is None:
        findings = [f"uiautomator could not read the screen: {(dumped.stdout + dumped.stderr).strip()[:200]}"]
    else:
        with open(os.path.join(out, name + ".xml"), "w", encoding="utf-8", newline="\n") as f:
            f.write(xml_text)
        findings = android_findings(audit, xml_text, density(run), device.package)
    return {"screen": name, "platform": "android", "findings": findings, "kept": []}


def judged(issue):
    """Whether an issue XCTest found is GroupLab's to fix. Two are not, and are kept in the artifact: the scroll bar, a strip 16 points wide
    down the right edge, which is the platform's own part, as on the computer and Android; and text found inside a picture with no element
    named (the photograph of a sheet, with its handwriting and printed labels), which no label can represent and the words around it say."""
    kind = issue.get("type")
    frame = issue.get("frame") or []
    if kind == "error":
        return True
    if kind not in IOS_JUDGED:
        return False
    if kind == "hitRegion" and len(frame) == 4 and frame[2] <= 20 and frame[3] >= 200:
        return False
    if kind == "elementDetection" and not frame and not issue.get("label"):
        return False
    return True


def answer_ios(device, audit, out):
    name = audit.get("name", "screen")
    folder = device.taps
    result = os.path.join(folder, "audit-result.json")
    done = os.path.join(folder, "done")
    for path in (result, done):
        if os.path.exists(path):
            os.remove(path)
    request = os.path.join(folder, "request")
    with open(request + ".part", "w", encoding="utf-8", newline="\n") as f:
        f.write("audit\n")
    os.replace(request + ".part", request)
    start = time.time()
    while time.time() - start < 180 and not os.path.exists(done):
        time.sleep(0.2)
    if not os.path.exists(done):
        return {"screen": name, "platform": "ios", "findings": ["the XCUITest runner did not finish its audit in 180 s"], "kept": []}
    os.remove(done)
    try:
        with open(result, encoding="utf-8") as f:
            issues = json.load(f)
    except (OSError, ValueError) as e:
        return {"screen": name, "platform": "ios", "findings": [f"the runner's audit could not be read: {e}"], "kept": []}
    with open(os.path.join(out, name + ".ios-audit.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(issues, f, indent=1)
    findings, kept = [], []
    for issue in issues:
        line = f"{issue.get('type')}: {issue.get('summary')}" + (f" (\"{issue['label']}\")" if issue.get("label") else "")
        (findings if judged(issue) else kept).append(line)
    return {"screen": name, "platform": "ios", "findings": findings, "kept": kept}


def drive(device, answer, out, seconds):
    """Answers every audit until the scenario says it is done; the screens audited."""
    start = time.time()
    screens = 0
    while time.time() - start < seconds:
        text = device.read(AUDIT)
        if text:
            try:
                audit = json.loads(text)
            except ValueError:
                time.sleep(0.2)
                continue
            said = answer(device, audit, out)
            with open(os.path.join(out, audit.get("name", "screen") + ".findings.json"), "w", encoding="utf-8", newline="\n") as f:
                json.dump(said, f, indent=1)
            detail = f"{len(said['findings'])} findings" + (f", {len(said['kept'])} kept for their look" if said["kept"] else "")
            print(f"{time.time() - start:6.1f} s  {said['screen']}: {detail}", flush=True)
            device.write(AUDITED, json.dumps({"ok": True, "detail": detail}))
            device.remove(AUDIT)
            screens += 1
            continue
        if (device.read("status") or "").strip() == "done":
            return screens
        if not device.alive():
            time.sleep(2)
            if (device.read("status") or "").strip() == "done":
                return screens
            raise SystemExit(f"::error::GroupLab Dev ended during the audit ({screens} screens read)")
        time.sleep(0.3)
    raise SystemExit(f"::error::The audit did not finish in {seconds} s ({screens} screens read)")


def judge(folder, least=10):
    """Fails on any finding, and on too few screens read; the summary goes to the step's summary where there is one."""
    rows = []
    for name in sorted(os.listdir(folder)):
        if name.endswith(".findings.json"):
            with open(os.path.join(folder, name), encoding="utf-8") as f:
                rows.append(json.load(f))
    lines = [f"### The screen reader audit read {len(rows)} screens"]
    bad = 0
    for row in rows:
        for finding in row["findings"]:
            lines.append(f"- {row['screen']}: {finding}")
            bad += 1
    kept = sum(len(r["kept"]) for r in rows)
    if kept:
        lines.append(f"\n{kept} findings about look (contrast, Dynamic Type, clipped text) kept in the artifact, not judged here.")
    text = "\n".join(lines)
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as f:
            f.write(text + "\n")
    if len(rows) < least:
        print(f"::error::The audit read {len(rows)} screens, fewer than {least}")
        return 1
    if bad:
        print(f"::error::The screen reader audit found {bad} things to fix")
        return 1
    return 0


def self_test():
    xml_text = """<hierarchy rotation="0">
<node class="android.widget.FrameLayout" package="org.grouplab.app.dev" bounds="[0,0][1080,2400]" clickable="false" text="" content-desc="">
<node class="android.widget.Button" package="org.grouplab.app.dev" bounds="[100,200][500,326]" clickable="true" text="Analyze" content-desc=""/>
<node class="Panel" package="org.grouplab.app.dev" bounds="[0,128][1080,2337]" clickable="false" text="" content-desc=""/>
<node class="Button" package="org.grouplab.app.dev" bounds="[100,2317][500,2337]" clickable="true" text="Scrolled half out" content-desc=""/>
<node class="android.widget.Button" package="org.grouplab.app.dev" bounds="[100,400][200,480]" clickable="true" text="" content-desc=""/>
<node class="android.view.View" package="org.grouplab.app.dev" bounds="[600,400][900,526]" clickable="true" text="Odd" content-desc=""/>
<node class="android.widget.Button" package="com.android.systemui" bounds="[0,0][10,10]" clickable="true" text="" content-desc=""/>
<node class="ScrollBar" package="org.grouplab.app.dev" bounds="[1038,300][1080,900]" clickable="false" text="" content-desc="">
<node class="RepeatButton" package="org.grouplab.app.dev" bounds="[1038,300][1080,342]" clickable="true" text="Line up" content-desc=""/>
</node>
</node></hierarchy>"""
    audit = {"name": "t", "view": {"x": 0, "y": 0, "width": 1080, "height": 2400}, "controls": [
        {"type": "Button", "name": "Analyze", "enabled": True, "screen": {"x": 100, "y": 200, "width": 400, "height": 126}},
        {"type": "Button", "name": "Gone", "enabled": True, "screen": {"x": 100, "y": 1000, "width": 400, "height": 126}},
        {"type": "Button", "name": "Off", "enabled": False, "screen": {"x": 100, "y": 1200, "width": 400, "height": 126}},
        {"type": "TextBlock", "name": "Words", "enabled": True, "screen": {"x": 100, "y": 1400, "width": 400, "height": 126}}]}
    found = android_findings(audit, xml_text, 420, "org.grouplab.app.dev")
    assert not any("Analyze" in f for f in found), found
    assert "android.widget.Button \"\" at 100,400: no name" in found, found
    assert any(f.startswith("android.widget.Button \"\" at 100,400: 38 by 30 dp") for f in found), found
    assert "android.view.View \"Odd\" at 600,400: no role, its class is android.view.View" in found, found
    assert "Button \"Gone\" at 100,1000: hidden from TalkBack's tree, though it can be tapped" in found, found
    assert not any("Off" in f or "Words" in f or "systemui" in f for f in found), found
    assert len(found) == 4, found
    assert bounds("[1,2][11,22]") == (1, 2, 10, 20)
    assert not judged({"type": "hitRegion", "frame": [424, 62, 16, 787]})
    assert judged({"type": "hitRegion", "frame": [16, 600, 40, 30]})
    assert not judged({"type": "elementDetection", "summary": "Potentially inaccessible text"})
    assert judged({"type": "sufficientElementDescription", "frame": [16, 465, 408, 21]})
    assert not judged({"type": "contrast", "frame": [1, 2, 3, 4]})
    print("screen-reader-audit self-test passed")
    return 0


def main(argv):
    if argv[1:2] == ["--self-test"]:
        return self_test()
    if argv[1:2] == ["judge"]:
        return judge(argv[2])
    args = {argv[i]: argv[i + 1] for i in range(2, len(argv) - 1, 2)}
    out = args["--out"]
    os.makedirs(out, exist_ok=True)
    touch = _touch()
    seconds = int(args.get("--seconds", "1200"))
    if argv[1] == "android":
        device = touch.Android(args.get("--package", "org.grouplab.app.dev"))
        drive(device, answer_android, out, seconds)
    elif argv[1] == "ios":
        device = touch.Ios(args["--data"], args["--taps"], int(args["--pid"]) if "--pid" in args else None)
        drive(device, answer_ios, out, seconds)
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
