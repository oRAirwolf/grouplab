#!/usr/bin/env python3
"""What the project keeps on GitHub, against a budget: NOTES-FROM-PLANNING.md entry 217 section 2.

    python scripts/storage-ledger.py            write docs/notes/STORAGE.md from what GitHub reports now
    python scripts/storage-ledger.py --check    print it and write nothing
    python scripts/storage-ledger.py --free     first free what is over budget, where entry 217 section 3 allows it without asking

**Freeing, so far only the first of entry 217 section 3's four kinds:** Actions artifacts over their budget, oldest first, only ones
older than a day (entry 220; it was three), until the budget is met. The other three, old builds, unreferenced test-data files and old archived submissions,
are listed against their budgets here and are freed by hand until one comes near its budget; none is near today.

It reads through the `gh` command line, as whoever is signed in to it, so run by Alan's pull or by the Claude Code session it can see the
private repositories; a repository it cannot read is listed as not readable rather than as empty. It writes names, sizes, counts and
consent levels only, never what a submission or a report contains. The budgets are in `docs/notes/storage-budgets.json`, the one place
to change them.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
OUT = REPO / "docs" / "notes" / "STORAGE.md"
BUDGETS = REPO / "docs" / "notes" / "storage-budgets.json"
OWNER = "oRAirwolf"


def gh(path: str) -> object | None:
    """One GitHub API read, or None when it cannot be read. A listing comes back as its pages in one list (`--slurp`)."""
    paged = "per_page=" in path
    run = subprocess.run(["gh", "api", *(["--paginate", "--slurp"] if paged else []), path], capture_output=True, text=True, encoding="utf-8")
    if run.returncode != 0 or not run.stdout.strip():
        return None
    return json.loads(run.stdout)


def size(n: float) -> str:
    for unit in ("bytes", "KB", "MB", "GB"):
        if n < 1024 or unit == "GB":
            return f"{n:,.0f} {unit}" if unit == "bytes" else f"{n:,.1f} {unit}"
        n /= 1024
    return f"{n:,.1f} GB"


def releases(repo: str) -> list[dict] | None:
    pages = gh(f"repos/{OWNER}/{repo}/releases?per_page=100")
    return None if pages is None else [r for page in pages for r in page]


def free_artifacts(live: list[dict], allowed: float) -> tuple[list[dict], list[str]]:
    """Actions artifacts over budget: the oldest first, only those older than a day, until what is left fits.

    Entry 220: a day, not three, because at about forty pushes a day each leaving 400 MB of Windows packages, three days of them was 80 GB.
    A nightly downloads its packages within its own run, so nothing still needed is ever older than a day.
    """
    cutoff = datetime.now(timezone.utc).timestamp() - 86400
    total = sum(a["size_in_bytes"] for a in live)
    gone: dict[str, list[int]] = {}
    keep = []
    for a in sorted(live, key=lambda a: a["created_at"]):
        made = datetime.fromisoformat(a["created_at"].replace("Z", "+00:00")).timestamp()
        if total > allowed and made < cutoff:
            run = subprocess.run(["gh", "api", "-X", "DELETE", f"repos/{OWNER}/grouplab/actions/artifacts/{a['id']}"], capture_output=True, text=True)
            if run.returncode == 0:
                total -= a["size_in_bytes"]
                g = gone.setdefault(a["name"], [0, 0])
                g[0] += 1
                g[1] += a["size_in_bytes"]
                continue
        keep.append(a)
    return keep, [f"Actions artifacts, {name}: {count} deleted, {size(n)}, older than a day" for name, (count, n) in sorted(gone.items(), key=lambda g: -g[1][1])]


def main() -> int:
    parser = argparse.ArgumentParser(description="Write the ledger of what is stored on GitHub.")
    parser.add_argument("--check", action="store_true", help="print it and write nothing")
    parser.add_argument("--free", action="store_true", help="free what is over budget, where entry 217 allows it without asking")
    args = parser.parse_args()
    freed: list[str] = []

    budgets = json.loads(BUDGETS.read_text(encoding="utf-8"))["budgets"]
    rows: list[tuple[str, str, float | None, str]] = []   # repository, what, bytes, detail
    notes: list[str] = []

    # grouplab, public: the repository, its releases, and Actions' artifacts and caches.
    main_repo = gh(f"repos/{OWNER}/grouplab")
    rows.append(("grouplab", "repository", main_repo["size"] * 1024 if main_repo else None, "public"))
    rel = releases("grouplab")
    if rel is not None:
        test_data = [r for r in rel if r["tag_name"] == "test-data"]
        builds = [r for r in rel if r["tag_name"] != "test-data"]
        rows.append(("grouplab", "releases, the builds", sum(a["size"] for r in builds for a in r["assets"]),
                     f"{len(builds)} releases, the newest {builds[0]['tag_name'] if builds else 'none'}"))
        rows.append(("grouplab", "releases, test-data", sum(a["size"] for r in test_data for a in r["assets"]),
                     f"{sum(len(r['assets']) for r in test_data)} files"))
    else:
        rows.append(("grouplab", "releases", None, "not readable"))
    artifacts = gh(f"repos/{OWNER}/grouplab/actions/artifacts?per_page=100")
    if artifacts is not None:
        live = [a for page in artifacts for a in page.get("artifacts", []) if not a.get("expired")]
        allowed = budgets.get("grouplab Actions artifacts", {}).get("gigabytes", 0) * 1024 ** 3
        if args.free and not args.check and allowed and sum(a["size_in_bytes"] for a in live) > allowed:
            live, freed = free_artifacts(live, allowed)
        rows.append(("grouplab", "Actions artifacts", sum(a["size_in_bytes"] for a in live), f"{len(live)} not yet expired"))
    caches = gh(f"repos/{OWNER}/grouplab/actions/cache/usage")
    if caches is not None:
        rows.append(("grouplab", "Actions caches", caches.get("active_caches_size_in_bytes", 0), f"{caches.get('active_caches_count', 0)} caches"))

    # grouplab-crash-reports, private: the repository and its issues.
    crash = gh(f"repos/{OWNER}/grouplab-crash-reports")
    issues = gh(f"search/issues?q=repo:{OWNER}/grouplab-crash-reports+is:issue")
    rows.append(("grouplab-crash-reports", "repository", crash["size"] * 1024 if crash else None,
                 f"private, {issues['total_count'] if isinstance(issues, dict) else 'an unknown number of'} issues" if crash else "not readable"))

    # grouplab-submissions-archive, private: one release a month, one asset a submission (entry 217 section 1).
    archive = gh(f"repos/{OWNER}/grouplab-submissions-archive")
    if archive is None:
        rows.append(("grouplab-submissions-archive", "releases", None, "not created yet, or not readable"))
    else:
        months = releases("grouplab-submissions-archive") or []
        for r in sorted(months, key=lambda r: r["tag_name"]):
            subs = [a for a in r["assets"] if a["name"] != "manifest.json"]
            rows.append(("grouplab-submissions-archive", r["tag_name"], sum(a["size"] for a in r["assets"]), f"{len(subs)} submissions"))
        if not months:
            rows.append(("grouplab-submissions-archive", "releases", 0, "private, empty"))

    # grouplab-testdata, public.
    testdata = gh(f"repos/{OWNER}/grouplab-testdata")
    rows.append(("grouplab-testdata", "repository", testdata["size"] * 1024 if testdata else None, "public" if testdata else "not readable"))

    # The budget each row counts against: its repository and what, or the repository alone.
    def budget_for(repo: str, what: str) -> tuple[str, float] | None:
        for key in (f"{repo} {what}", f"{repo} releases" if repo == "grouplab-submissions-archive" else "", repo):
            if key in budgets:
                return key, budgets[key]["gigabytes"] * 1024 ** 3
        return None

    used: dict[str, float] = {}
    for repo, what, n, _ in rows:
        b = budget_for(repo, what)
        if b and n is not None:
            used[b[0]] = used.get(b[0], 0) + n

    total = sum(n for _, _, n, _ in rows if n is not None)
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    lines = [
        "# What is stored on GitHub",
        "",
        f"Written by `scripts/storage-ledger.py` on {now}, from what GitHub reports, NOTES-FROM-PLANNING.md entry 217 section 2. Names, sizes and",
        "counts only. Budgets are in `docs/notes/storage-budgets.json`; when one is reached, space is freed oldest first in the order entry 217",
        "section 3 sets, and every deletion is listed here and in `docs/notes/for-alan.md`.",
        "",
        f"**In all: {size(total)}.**",
        "",
        "| Repository | What | Size | Detail |",
        "|---|---|---|---|",
    ]
    lines += [f"| {repo} | {what} | {size(n) if n is not None else 'not readable'} | {detail} |" for repo, what, n, detail in rows]
    lines += ["", "## Against the budgets", "", "| Budget | Used | Allowed | Share |", "|---|---|---|---|"]
    over = []
    for key, b in budgets.items():
        allowed = b["gigabytes"] * 1024 ** 3
        n = used.get(key, 0)
        share = n / allowed if allowed else 0
        lines.append(f"| {key} | {size(n)} | {b['gigabytes']:g} GB | {share:.0%} |")
        if share >= 1:
            over.append(key)
    lines += ["", "Why each budget is what it is:", ""]
    lines += [f"- **{key}:** {b['why']}" for key, b in budgets.items()]
    lines += ["", "**Over budget:** " + (", ".join(over) if over else "nothing."), ""]
    if freed:
        lines += ["## Freed on this run", "", *[f"- {line}" for line in freed], ""]
    notes.append(f"total {size(total)}")

    text = "\n".join(lines)
    if args.check:
        print(text)
    else:
        OUT.write_text(text, encoding="utf-8", newline="\n")
        print(f"wrote {OUT.relative_to(REPO)}: {size(total)} in all, {len(over)} over budget")
    return 0


if __name__ == "__main__":
    sys.exit(main())
