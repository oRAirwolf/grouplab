# What is stored on GitHub

Written by `scripts/storage-ledger.py` on 2026-09-25 11:14 UTC, from what GitHub reports, NOTES-FROM-PLANNING.md entry 217 section 2. Names, sizes and
counts only. Budgets are in `docs/notes/storage-budgets.json`; when one is reached, space is freed oldest first in the order entry 217
section 3 sets, and every deletion is listed here and in `docs/notes/for-alan.md`.

**In all: 52.0 GB.**

| Repository | What | Size | Detail |
|---|---|---|---|
| grouplab | repository | 277.1 MB | public |
| grouplab | releases, the builds | 11.9 GB | 33 releases, the newest v0.2.0-nightly.107 |
| grouplab | releases, test-data | 111.3 MB | 2 files |
| grouplab | Actions artifacts | 39.0 GB | 459 not yet expired |
| grouplab | Actions caches | 304.2 MB | 7 caches |
| grouplab-crash-reports | repository | 0 bytes | private, 3 issues |
| grouplab-submissions-archive | archive-2026-09 | 379.3 MB | 27 submissions |
| grouplab-testdata | repository | 57.2 MB | public |

## Against the budgets

| Budget | Used | Allowed | Share |
|---|---|---|---|
| grouplab releases, the builds | 11.9 GB | 20 GB | 60% |
| grouplab releases, test-data | 111.3 MB | 2 GB | 5% |
| grouplab Actions artifacts | 39.0 GB | 5 GB | 780% |
| grouplab Actions caches | 304.2 MB | 10 GB | 3% |
| grouplab-crash-reports | 0 bytes | 1 GB | 0% |
| grouplab-submissions-archive releases | 379.3 MB | 25 GB | 1% |
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
