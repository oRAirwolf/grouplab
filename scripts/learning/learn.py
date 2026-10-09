#!/usr/bin/env python3
"""The learning loop's driver, NOTES-FROM-PLANNING.md entry 394. No person and no Claude takes part in any step.

It runs in the private repository oRAirwolf/grouplab-submissions-archive's GitHub Actions (the workflow is learn.yml beside this file,
copied there as .github/workflows/learn.yml), from a checkout of GroupLab at the newest nightly's tag, so every submission is read
exactly as that nightly reads it. Nothing on the server changes.

    python learn.py pending     how many archived submissions are not on the scoreboard yet
    python learn.py score       every submission not yet on the real scoreboard: one row each
    python learn.py nightly     every submission with a person's corrections read again, the synthetic board beside it, the check against
                                the baseline, the summary, and the issue in the error-report repository when a line is worse
    python learn.py tune        the constant search, once there are enough corrected submissions, and a pull request if it passed

What it keeps, in the archive repository's learning/ folder: rows.jsonl (one row a submission, numbers and labels only), baseline.jsonl,
summary.md (a few lines, written every night) and tuning.md. Never a picture, never a location, never a name or a file path: the pictures
are unpacked into the runner's temporary folder, read, and gone with the runner. Nothing is uploaded as an artifact.

Environment: ARCHIVE and GROUPLAB, the two checkouts; CLI, the built grouplab.dll; BUILD, the nightly's tag; GH_TOKEN, the workflow's own
token, which reads and writes the archive repository; LEARNING_TOKEN, optional, a fine-grained token for the error-report repository's
issues and GroupLab's pull requests (request 88). Without it the scoring, the check and the summary still run and the report is skipped,
and the log says so.
"""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

ARCHIVE_REPO = "oRAirwolf/grouplab-submissions-archive"
REPORTS_REPO = "oRAirwolf/grouplab-crash-reports"
GROUPLAB_REPO = "oRAirwolf/grouplab"
ISSUE_TITLE = "Learning loop: real targets read worse than their baseline"
DETECTOR = "src/GroupLab.Core/Detection/RenderDifferenceHoleDetector.cs"
# RealScoreboard.CorrectedForTuning, held equal to it by LearningLoopTests.
CORRECTED_FOR_TUNING = 50


def env(name: str, default: str | None = None) -> str:
    value = os.environ.get(name, default)
    if value is None:
        sys.exit(f"learn.py: {name} is not set")
    return value


def run(args: list[str], token: str | None = None, cwd: str | None = None, check: bool = True) -> subprocess.CompletedProcess:
    environment = dict(os.environ)
    if token is not None:
        environment["GH_TOKEN"] = token
    return subprocess.run(args, cwd=cwd, env=environment, capture_output=True, text=True, encoding="utf-8", check=check)


def learning() -> Path:
    folder = Path(env("ARCHIVE")) / "learning"
    folder.mkdir(exist_ok=True)
    return folder


def read_rows(path: Path) -> dict[str, dict]:
    if not path.exists():
        return {}
    rows = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    return {row["Submission"]: row for row in rows}


def write_rows(path: Path, rows: dict[str, dict]) -> None:
    path.write_bytes("".join(json.dumps(rows[k], separators=(",", ":")) + "\n" for k in sorted(rows)).encode("utf-8"))


def submissions() -> dict[str, str]:
    """Every submission the archive holds, name to the release it is in, read from each month's manifest."""
    tags = json.loads(run(["gh", "release", "list", "-R", ARCHIVE_REPO, "--limit", "200", "--json", "tagName"]).stdout)
    found: dict[str, str] = {}
    for tag in sorted(t["tagName"] for t in tags if t["tagName"].startswith("archive-")):
        text = run(["gh", "release", "download", tag, "-R", ARCHIVE_REPO, "-p", "manifest.json", "-O", "-"]).stdout
        for entry in json.loads(text.lstrip("﻿")).get("submissions", []):
            if re.fullmatch(r"\d{4}-\d{2}-\d{2}_[0-9a-f]{8}", entry.get("name", "")):
                found[entry["name"]] = tag
    return found


def score(names: list[str], where: dict[str, str]) -> list[dict]:
    """Downloads and unpacks each submission into the runner's temporary folder and scores it with the nightly's command line."""
    if not names:
        return []
    work = Path(tempfile.mkdtemp(prefix="learn-"))
    try:
        folders = []
        for name in names:
            run(["gh", "release", "download", where[name], "-R", ARCHIVE_REPO, "-p", f"{name}.zip", "-D", str(work)])
            folder = work / name
            with zipfile.ZipFile(work / f"{name}.zip") as archive:
                archive.extractall(folder)
            (work / f"{name}.zip").unlink()
            folders.append(str(folder))
        out = work / "rows.jsonl"
        done = run(["dotnet", env("CLI"), "learn", "score", *folders, "--build", env("BUILD"), "--library", str(Path(env("GROUPLAB")) / "targets"),
                    "--out", str(out)], cwd=env("GROUPLAB"), check=False)
        # The command line's own lines are counts by submission id, nothing else; they go to the log so a run can be followed.
        print(done.stderr.strip())
        if done.returncode != 0:
            sys.exit(f"learn.py: grouplab learn score failed with {done.returncode}")
        return [json.loads(line) for line in out.read_text(encoding="utf-8").splitlines() if line.strip()]
    finally:
        shutil.rmtree(work, ignore_errors=True)


def command_score(every_corrected: bool) -> int:
    rows_path = learning() / "rows.jsonl"
    rows = read_rows(rows_path)
    where = submissions()
    corrected = [n for n, r in rows.items() if r.get("Holes") is not None]
    wanted = sorted(set(where) - set(rows)) + (sorted(n for n in corrected if n in where) if every_corrected else [])
    print(f"{len(where)} submissions in the archive, {len(rows)} on the scoreboard, {len(wanted)} to read")
    for row in score(wanted, where):
        rows[row["Submission"]] = row
    write_rows(rows_path, rows)
    return 0


def synthetic_verdict() -> str:
    """The synthetic board, read by the same build: a pass, or the conditions that fell."""
    grouplab = env("GROUPLAB")
    done = run(["dotnet", env("CLI"), "scoreboard", "--synthetic", "--baseline", "docs/scoreboard/synthetic-baseline.json"], cwd=grouplab, check=False)
    drops = [line[5:] for line in done.stderr.splitlines() if line.startswith("DROP ")]
    return "every condition within its margin." if done.returncode == 0 else f"{len(drops)} condition(s) fell: " + "; ".join(drops)


def report(drops_text: str) -> None:
    """Opens the one issue, or adds tonight's lines to it, in the private error-report repository. No picture is attached."""
    token = os.environ.get("LEARNING_TOKEN")
    if not token:
        print("LEARNING_TOKEN is not set (request 88), so the report was not filed; the summary in learning/summary.md has it")
        return
    found = json.loads(run(["gh", "issue", "list", "-R", REPORTS_REPO, "--state", "open", "--search", ISSUE_TITLE, "--json", "number,title"], token=token).stdout)
    number = next((i["number"] for i in found if i["title"] == ISSUE_TITLE), None)
    body = (f"Build {env('BUILD')}. These lines of the real scoreboard read worse than their baseline by more than the scoreboard's margins "
            f"(docs/DETECTION-LEARNING-STUDY.md section 9). Numbers only; no picture is attached.\n\n{drops_text}")
    if number is None:
        run(["gh", "issue", "create", "-R", REPORTS_REPO, "--title", ISSUE_TITLE, "--body", body], token=token)
    else:
        run(["gh", "issue", "comment", str(number), "-R", REPORTS_REPO, "--body", body], token=token)


def command_nightly() -> int:
    command_score(every_corrected=True)
    folder = learning()
    verdict = folder / "synthetic.txt"
    verdict.write_text(synthetic_verdict(), encoding="utf-8")
    drops = Path(tempfile.mkdtemp(prefix="learn-")) / "drops.md"
    done = run(["dotnet", env("CLI"), "learn", "check", "--rows", str(folder / "rows.jsonl"), "--baseline", str(folder / "baseline.jsonl"), "--write-baseline",
                "--build", env("BUILD"), "--summary", str(folder / "summary.md"), "--drops", str(drops), "--synthetic", str(verdict)], check=False)
    print(done.stdout.strip())
    verdict.unlink()
    if done.returncode == 1 and drops.read_text(encoding="utf-8").strip():
        report(drops.read_text(encoding="utf-8"))
    elif done.returncode not in (0, 1):
        sys.exit(f"learn.py: grouplab learn check failed with {done.returncode}: {done.stderr.strip()}")
    return 0


def command_tune() -> int:
    rows = read_rows(learning() / "rows.jsonl")
    where = submissions()
    corrected = sorted(n for n, r in rows.items() if r.get("Holes") is not None and n in where)
    if len(corrected) < CORRECTED_FOR_TUNING:
        # Below the number the held-out check needs nothing is downloaded: the count alone decides, as the command line would.
        result = {"corrected": len(corrected), "needed": CORRECTED_FOR_TUNING, "waiting": True,
                  "reason": f"{len(corrected)} corrected submissions of the {CORRECTED_FOR_TUNING} a held-out check needs (RealScoreboard.CorrectedForTuning)"}
        (learning() / "tuning.md").write_text(tuning_text(result), encoding="utf-8")
        return 0
    work = Path(tempfile.mkdtemp(prefix="tune-"))
    try:
        for name in corrected:
            run(["gh", "release", "download", where[name], "-R", ARCHIVE_REPO, "-p", f"{name}.zip", "-D", str(work)])
            with zipfile.ZipFile(work / f"{name}.zip") as archive:
                archive.extractall(work / name)
            (work / f"{name}.zip").unlink()
        result_path = work / "result.json"
        run(["dotnet", env("CLI"), "learn", "tune", str(work), "--out", str(result_path), "--library", "targets"], cwd=env("GROUPLAB"))
        result = json.loads(result_path.read_text(encoding="utf-8"))
        (learning() / "tuning.md").write_text(tuning_text(result), encoding="utf-8")
        if result.get("accepted"):
            pull_request(result)
    finally:
        shutil.rmtree(work, ignore_errors=True)
    return 0


def tuning_text(result: dict) -> str:
    lines = [f"# Tuning, {env('BUILD')}", "", f"- {result['reason']}."]
    if not result.get("waiting"):
        lines.append(f"- Constants: {json.dumps(result['constants'])}, from {json.dumps(result['was'])}.")
        held = result["heldOut"]
        lines.append(f"- Held out: {held['submissions']} submissions, net found minus false {held['netBefore']} before and {held['netAfter']} after.")
    return "\n".join(lines) + "\n"


def pull_request(result: dict) -> None:
    """A branch with the new constants and nothing else, and a pull request with the table. How it is merged waits on request 89."""
    token = os.environ.get("LEARNING_TOKEN")
    if not token:
        print("LEARNING_TOKEN is not set (request 88), so the pull request was not opened; learning/tuning.md has the result")
        return
    grouplab = env("GROUPLAB")
    source = Path(grouplab) / DETECTOR
    text = source.read_text(encoding="utf-8")
    for name, value in result["constants"].items():
        text, n = re.subn(rf"(double {name} = )[0-9.]+", rf"\g<1>{value}", text, count=1)
        if n != 1:
            sys.exit(f"learn.py: {name} not found once in {DETECTOR}")
    source.write_text(text, encoding="utf-8")
    branch = f"tuning/{env('BUILD')}"
    run(["git", "checkout", "-b", branch], cwd=grouplab)
    run(["git", "-c", "user.name=grouplab-learning", "-c", "user.email=grouplab-learning@users.noreply.github.com", "commit", "-am",
         f"Detection constants from the learning loop, {env('BUILD')}\n\nRelease-note: Hole finding uses settings tuned against the targets people "
         f"have sent and corrected, and finds more holes on the ones held back to check it. (Entry 394)\nRelease-note-kind: changed"], cwd=grouplab)
    run(["git", "push", f"https://x-access-token:{token}@github.com/{GROUPLAB_REPO}.git", branch], cwd=grouplab)
    held = result["heldOut"]
    body = "\n".join([
        "The learning loop's constant search (NOTES-FROM-PLANNING.md entry 394 section 4) found these, and they passed: better on the held-out",
        "submissions, worse on none, and G3 holds around them (recall above 90 percent with any threshold 30 percent off).", "",
        "| Constant | Was | Now |", "|---|---|---|",
        *[f"| {k} | {result['was'][k]} | {v} |" for k, v in result["constants"].items()], "",
        f"Held out: {held['submissions']} submissions. Publishable ones: found {held['publicFound']}; false marks {held['publicFalseMarks']}. "
        f"Testing-only ones checked as well: {held['testingOnlyCount']}, whose numbers stay private.", "",
        result["table"],
        "How this is merged is request 89's answer; until then it waits."])
    run(["gh", "pr", "create", "-R", GROUPLAB_REPO, "--head", branch, "--base", "main", "--title", f"Detection constants from the learning loop, {env('BUILD')}",
         "--body", body], token=token)


def command_pending() -> int:
    """How many archived submissions are not on the scoreboard yet, for the workflow to skip building when there are none."""
    print(len(set(submissions()) - set(read_rows(learning() / "rows.jsonl"))))
    return 0


def main() -> int:
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    if command == "pending":
        return command_pending()
    if command == "score":
        return command_score(every_corrected="--all" in sys.argv)
    if command == "nightly":
        return command_nightly()
    if command == "tune":
        return command_tune()
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main())
