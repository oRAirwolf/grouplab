#!/usr/bin/env python3
"""One retry of the tests that failed, in the same CI job. NOTES-FROM-PLANNING.md entry 450 section 1.

    python3 scripts/test-retry.py plan <first-results> --out <plan> <project>...
    python3 scripts/test-retry.py verdict <first-results> <retry-results> --leg <os> --run <id> --out <flaky.tsv>

One flaky test on one leg of "build and test" used to stop every platform's nightly, the phones' too. So when the
test step fails, ci.yml runs only the tests that failed, once more, in the same job. If every one passes the second
time, the leg passes; if any fails again, the leg fails as it always did, so a real fault still stops everything.

plan reads the TRX results of the first run, one folder per test project named after it, and writes one line per
project to retry: the project, a tab, and a test filter naming its failed tests. It writes nothing and exits 1 when
the failure is not a failed test, so nothing is retried: a project that failed with no results, a test process that
crashed or hung, or more than LIMIT failed tests, which is a fault rather than a flake. A build failure never gets
this far, because the tests run only after the build step passed.

verdict compares the two runs. Each test that passed only on its retry is never hidden: it becomes a warning
annotation, a line in the job's summary, and a row in the tab-separated file ci.yml uploads as an artifact, which
scripts/flaky-tests.py gathers into docs/notes/flaky-tests.md for the planning session. It exits 1 when any test
failed twice.
"""

from __future__ import annotations

import argparse
import datetime
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
# More failed tests than this is a broken change, not a flaky test, and retrying it only makes the red arrive later.
LIMIT = 30
# Words the test platform writes into a results file's run information when the test process itself went wrong.
ABORTED = ("aborted", "crashed", "hang", "timeout")


def results(folder: Path) -> tuple[dict[str, list[str]], list[str]]:
    """Each test's fully qualified name with every outcome it had, and any run-level error text, from the TRX files
    in one folder."""
    outcomes: dict[str, list[str]] = {}
    errors: list[str] = []
    for trx in sorted(folder.glob("**/*.trx")):
        root = ET.parse(trx).getroot()
        names = {}
        for test in root.iterfind("t:TestDefinitions/t:UnitTest", NS):
            method = test.find("t:TestMethod", NS)
            if method is not None:
                names[test.get("id")] = method.get("className", "") + "." + method.get("name", "")
        for result in root.iterfind("t:Results/t:UnitTestResult", NS):
            name = names.get(result.get("testId")) or (result.get("testName") or "").split("(")[0]
            outcomes.setdefault(name, []).append(result.get("outcome", ""))
        for info in root.iterfind("t:ResultSummary/t:RunInfos/t:RunInfo", NS):
            text = " ".join(info.itertext()).strip()
            if info.get("outcome") == "Error" and any(word in text.lower() for word in ABORTED):
                errors.append(text.splitlines()[0][:200] if text else "a run-level error")
    return outcomes, errors


def failed(outcomes: dict[str, list[str]]) -> list[str]:
    return sorted(name for name, seen in outcomes.items() if "Failed" in seen)


def plan(first: Path, projects: list[str], out: Path) -> int:
    lines = []
    total = 0
    for project in projects:
        folder = first / Path(project).name
        if not any(folder.glob("**/*.trx")):
            print(f"{project} failed and wrote no results, so this is not a failed test and nothing is retried")
            return 1
        outcomes, errors = results(folder)
        if errors:
            print(f"{project}'s test process went wrong ({errors[0]}), which a retry of single tests cannot answer, so nothing is retried")
            return 1
        names = failed(outcomes)
        if not names:
            print(f"{project} failed with no failed test in its results, so nothing is retried")
            return 1
        total += len(names)
        # Names are namespaces, classes and methods, so none carries a character the filter syntax treats as special.
        lines.append(project + "\t" + "|".join("FullyQualifiedName=" + n for n in names))
    if total > LIMIT:
        print(f"{total} tests failed, more than {LIMIT}, which is a fault rather than a flake, so nothing is retried")
        return 1
    with out.open("w", encoding="utf-8", newline="\n") as f:
        f.write("".join(line + "\n" for line in lines))
    print(f"{total} failed tests in {len(lines)} projects, each run once more")
    return 0


def verdict(first: Path, retry: Path, leg: str, run: str, out: Path) -> int:
    before, _ = results(first)
    after, _ = results(retry)
    flaky, twice = [], []
    for name in failed(before):
        seen = after.get(name, [])
        (flaky if seen and all(o == "Passed" for o in seen) else twice).append(name)

    today = datetime.datetime.now(datetime.timezone.utc).date().isoformat()
    for name in flaky:
        print(f"::warning title=Passed only on its retry::{name} failed on {leg} and passed when it was run again, run {run}. "
              "It is a flaky test, recorded in docs/notes/flaky-tests.md (entry 450).")
    for name in twice:
        print(f"::error title=Failed twice::{name} failed on {leg}, and failed again on its retry")
    if flaky:
        with out.open("a", encoding="utf-8", newline="\n") as f:
            f.write("".join(f"{today}\t{leg}\t{run}\t{name}\n" for name in flaky))

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8", newline="\n") as f:
            f.write(f"### {leg}: the failed tests, run once more\n\n")
            f.writelines(f"- passed only on its retry, so a flaky test: `{name}`\n" for name in flaky)
            f.writelines(f"- failed again: `{name}`\n" for name in twice)
            f.write("\n")
    print(f"{len(flaky)} passed on their retry, {len(twice)} failed twice")
    return 1 if twice else 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("plan")
    p.add_argument("first", type=Path)
    p.add_argument("projects", nargs="+")
    p.add_argument("--out", type=Path, required=True)
    v = sub.add_parser("verdict")
    v.add_argument("first", type=Path)
    v.add_argument("retry", type=Path)
    v.add_argument("--leg", required=True)
    v.add_argument("--run", required=True)
    v.add_argument("--out", type=Path, required=True)
    args = parser.parse_args(argv)
    if args.command == "plan":
        return plan(args.first, args.projects, args.out)
    return verdict(args.first, args.retry, args.leg, args.run, args.out)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
