#!/usr/bin/env python3
"""The slowest tests of a run, from its TRX results. NOTES-FROM-PLANNING.md entry 474 section 3.

    python3 scripts/slowest-tests.py <results-folder> [--top 15] [--summary <file>]

ci.yml deletes each leg's TRX results once the test step is done, so which tests take the time was seen nowhere. This reads every
.trx file under the folder (one folder per test project), prints each project's time summed and its slowest tests, and, given
--summary (the job's $GITHUB_STEP_SUMMARY), adds the same as a table there. It changes nothing and always exits 0: it is a
report, never a gate.
"""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def seconds(duration: str) -> float:
    h, m, s = duration.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


def read(folder: Path) -> dict[str, list[tuple[float, str]]]:
    """Each project's tests with their seconds, the project named by the folder its TRX file is in."""
    found: dict[str, list[tuple[float, str]]] = {}
    for trx in sorted(folder.rglob("*.trx")):
        try:
            root = ET.parse(trx).getroot()
        except ET.ParseError:
            continue
        rows = found.setdefault(trx.parent.name, [])
        for result in root.iterfind(".//t:UnitTestResult", NS):
            rows.append((seconds(result.get("duration", "0:0:0")), result.get("testName", "?")))
    return found


def main(argv: list[str]) -> int:
    if not argv:
        print(__doc__)
        return 0
    folder, top, summary = Path(argv[0]), 15, None
    if "--top" in argv:
        top = int(argv[argv.index("--top") + 1])
    if "--summary" in argv:
        summary = argv[argv.index("--summary") + 1]
    lines = []
    for project, rows in read(folder).items():
        rows.sort(reverse=True)
        lines.append(f"**{project}**: {len(rows)} tests, {sum(d for d, _ in rows) / 60:.1f} min summed; the slowest:")
        lines.append("")
        lines.append("| seconds | test |")
        lines.append("|---|---|")
        lines += [f"| {d:.1f} | {name.replace('|', '/')[:160]} |" for d, name in rows[:top]]
        lines.append("")
    text = "\n".join(lines)
    print(text)
    if summary and text:
        with open(summary, "a", encoding="utf-8", newline="\n") as out:
            out.write("### The slowest tests\n\n" + text + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
