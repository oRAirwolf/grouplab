## 2026-09-25, entry 217: track everything stored on GitHub, and free space by deleting the oldest when needed

Read with entry 216, which this changes in one important way. Alan, 2026-09-25: "We should keep track of what is being stored on github
and delete old submissions, builds, or files as space is needed."

## 1. Store the archive as release assets, not Git LFS

The planning session checked GitHub's documentation after entry 216 and found a reason to change it:

- **Git LFS space cannot be freed by deleting files.** GitHub: "To remove Git LFS objects from a repository, delete and recreate the
  repository." Deleted LFS files keep counting against the 10 GiB. That defeats deleting old submissions as space is needed.
- **Release assets can be deleted one by one, and are not metered.** GitHub: each asset under 2 GiB, up to 1000 assets a release, and
  "There is no limit on the total size of a release, nor bandwidth usage."

So, in the private `grouplab-submissions-archive` repository (still created by Alan):

1. One release per month, for example `archive-2026-09`, created by the pull. Each submission is one asset: a zip of its folder exactly as
   pulled (`meta.json`, consent, `DO-NOT-PUBLISH`, the image), named by the submission's folder name. Plus a `manifest.json` asset per
   release listing every submission, its size, its SHA-256 and its consent level, rewritten when the release changes.
2. The order of entry 216 section 2 stands: verify here, upload, verify the uploaded asset by its SHA-256 (download it back, or compare
   GitHub's reported digest if it gives one), and only then remove the server copy.
3. No Git LFS in the archive. The repository's own files are just a README saying what it is and that it is never made public.
4. The release tags there are the archive repository's own and are not `v*`; the no `v*` tags rule is about the main repository.

## 2. A ledger of everything on GitHub

A script, run by the pull and by any run that publishes, that writes `docs/notes/STORAGE.md` (committed, no secrets, no submission contents
beyond names, sizes and consent level) with, per repository:

- **grouplab** (public): repository size; releases and their assets (the nightlies kept by the thirty release rule, the rolling `nightly`,
  `test-data`); Actions artifacts and caches.
- **grouplab-crash-reports** (private): issue count and repository size.
- **grouplab-submissions-archive** (private): each month's release, its asset count and total size, and the total.
- **grouplab-testdata** (public): repository size.

It shows totals against a budget, and STATE.md carries one line with the grand total. The Actions storage and minutes that count against
the account's free allowance for private repositories are listed with that allowance.

## 3. Budgets, and what is deleted first when one is reached

GitHub does not cap release storage, but the project keeps itself to a budget so it stays a reasonable use of a free service: propose one
per category (for example the archive at 25 GB), say why, and let Alan change them in one place. When a category reaches its budget, the
pull or the publishing run frees space by itself, oldest first, in this order, and records every deletion in `STORAGE.md` and the log:

1. **Actions artifacts and caches** older than they need to be (set their retention short so this rarely happens).
2. **Old builds:** the thirty release rule already keeps the nightlies in hand; nothing more unless the budget says so, and never the newest
   nightly, the rolling `nightly`, or anything a stable release needs.
3. **`test-data` assets no test or CI job references any more.** Never one a test uses.
4. **Old submissions in the archive**, oldest month first, with these guards: only one whose copy in `C:\Dev\grouplab-submissions` still
   verifies against the manifest's SHA-256 (so a copy remains), never one used as a fixture, in `test-data`, or referred to by a document or
   article, and never one marked "may be published" that has not yet been reviewed for the public data set. A deletion is listed, with the
   reason, in for-alan.md in plain words the same run ("removed 12 submissions from September 2026 to stay under the archive's 25 GB").
   Alan decided this can happen without asking first.

## 4. Order of work

Build the ledger first (it needs nothing from Alan), then the archive once the repository exists, then the budgets. Entry 215's server
retention rules and entry 216's privacy text stand.
