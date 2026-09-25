# What is stored on GitHub

Written by `scripts/storage-ledger.py` on 2026-09-25 08:29 UTC, from what GitHub reports, NOTES-FROM-PLANNING.md entry 217 section 2. Names, sizes and
counts only. Budgets are in `docs/notes/storage-budgets.json`; when one is reached, space is freed oldest first in the order entry 217
section 3 sets, and every deletion is listed here and in `docs/notes/for-alan.md`.

**In all: 87.1 GB.**

| Repository | What | Size | Detail |
|---|---|---|---|
| grouplab | repository | 274.0 MB | public |
| grouplab | releases, the builds | 11.6 GB | 33 releases, the newest v0.2.0-nightly.105 |
| grouplab | releases, test-data | 111.3 MB | 2 files |
| grouplab | Actions artifacts | 74.8 GB | 880 not yet expired |
| grouplab | Actions caches | 304.2 MB | 7 caches |
| grouplab-crash-reports | repository | 0 bytes | private, 1 issues |
| grouplab-submissions-archive | releases | 0 bytes | private, empty |
| grouplab-testdata | repository | 57.2 MB | public |

## Against the budgets

| Budget | Used | Allowed | Share |
|---|---|---|---|
| grouplab releases, the builds | 11.6 GB | 20 GB | 58% |
| grouplab releases, test-data | 111.3 MB | 2 GB | 5% |
| grouplab Actions artifacts | 74.8 GB | 5 GB | 1496% |
| grouplab Actions caches | 304.2 MB | 10 GB | 3% |
| grouplab-crash-reports | 0 bytes | 1 GB | 0% |
| grouplab-submissions-archive releases | 0 bytes | 25 GB | 0% |
| grouplab-testdata | 57.2 MB | 1 GB | 6% |

Why each budget is what it is:

- **grouplab releases, the builds:** about thirty nightlies of five packages near 470 MB each, which the thirty release rule already keeps to, with room for the stable releases
- **grouplab releases, test-data:** the large samples CI fetches; today two scans near 60 MB, so this is room for about thirty more
- **grouplab Actions artifacts:** build and test output kept for days, not months; retention is set short so this is rarely reached
- **grouplab Actions caches:** GitHub's own cap for one repository's caches, which it enforces by evicting the oldest
- **grouplab-crash-reports:** issues are text; a gigabyte is far more than a project this size writes
- **grouplab-submissions-archive releases:** submissions of 3 to 60 MB each, so between four hundred and eight thousand of them; entry 217's own example
- **grouplab-testdata:** the public test data repository; its large files live on the test-data release instead

**Over budget:** grouplab Actions artifacts

## Freed on this run

- Actions artifacts, grouplab-win-x64: 85 deleted, 34.1 GB, older than three days
- Actions artifacts, grouplab-linux-x64: 176 deleted, 15.5 GB, older than three days
- Actions artifacts, windows-package: 24 deleted, 9.9 GB, older than three days
- Actions artifacts, linux-package: 28 deleted, 5.0 GB, older than three days
- Actions artifacts, gate-record-ubuntu-latest: 204 deleted, 274.9 MB, older than three days
- Actions artifacts, gate-record-windows-latest: 203 deleted, 273.5 MB, older than three days
- Actions artifacts, gate-record-macos-latest: 202 deleted, 272.2 MB, older than three days
- Actions artifacts, windows-corners: 187 deleted, 18.5 MB, older than three days
- Actions artifacts, ballistic-reference-tables: 1 deleted, 18.2 KB, older than three days
