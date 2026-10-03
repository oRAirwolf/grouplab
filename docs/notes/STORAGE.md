# What is stored on GitHub

Written by `scripts/storage-ledger.py` on 2026-10-03 02:23 UTC, from what GitHub reports, NOTES-FROM-PLANNING.md entry 217 section 2. Names, sizes and
counts only. Budgets are in `docs/notes/storage-budgets.json`; when one is reached, space is freed oldest first in the order entry 217
section 3 sets, and every deletion is listed here and in `docs/notes/for-alan.md`.

**In all: 86.5 GB.**

| Repository | What | Size | Detail |
|---|---|---|---|
| grouplab | repository | 653.1 MB | public |
| grouplab | releases, the builds | 19.7 GB | 34 releases, the newest store-draft-0.2.0 |
| grouplab | releases, test-data | 166.6 MB | 9 files |
| grouplab | Actions artifacts | 59.7 GB | 1838 not yet expired |
| grouplab | Actions caches | 918.5 MB | 14 caches |
| grouplab-crash-reports | repository | 2.4 MB | private, 22 issues |
| grouplab-submissions-archive | archive-2026-09 | 451.4 MB | 33 submissions |
| grouplab-submissions-archive | archive-2026-10 | 45.3 MB | 2 submissions |
| grouplab-backups | releases | 5.0 GB | private, 6 backups |
| grouplab-testdata | repository | 57.2 MB | public |

## Against the budgets

| Budget | Used | Allowed | Share |
|---|---|---|---|
| grouplab releases, the builds | 19.7 GB | 20 GB | 98% |
| grouplab releases, test-data | 166.6 MB | 2 GB | 8% |
| grouplab Actions artifacts | 59.7 GB | 5 GB | 1193% |
| grouplab Actions caches | 918.5 MB | 10 GB | 9% |
| grouplab-crash-reports | 2.4 MB | 1 GB | 0% |
| grouplab-submissions-archive releases | 496.7 MB | 25 GB | 2% |
| grouplab-testdata | 57.2 MB | 1 GB | 6% |
| grouplab-backups releases | 5.0 GB | 10 GB | 50% |

Why each budget is what it is:

- **grouplab releases, the builds:** about thirty nightlies of five packages near 470 MB each, which the thirty release rule already keeps to, with room for the stable releases
- **grouplab releases, test-data:** the large samples CI fetches; today two scans near 60 MB, so this is room for about thirty more
- **grouplab Actions artifacts:** build and test output kept for days, not months; retention is set short so this is rarely reached
- **grouplab Actions caches:** GitHub's own cap for one repository's caches, which it enforces by evicting the oldest
- **grouplab-crash-reports:** issues are text; a gigabyte is far more than a project this size writes
- **grouplab-submissions-archive releases:** submissions of 3 to 60 MB each, so between four hundred and eight thousand of them; entry 217's own example
- **grouplab-testdata:** the public test data repository; its large files live on the test-data release instead
- **grouplab-backups releases:** entry 222: seventeen nightly backups kept at about 450 MB each, with room for the repository and the originals to grow

**Over budget:** grouplab Actions artifacts

## The server's week

As of 2026-09-27: on the server, workers deleted or archived: nothing; the server's own backup is from 2026-09-26; the Oracle boot volume backups are not seen by this report: Alan can check them in the Oracle console, under Boot Volume Backups, whenever he wants.
