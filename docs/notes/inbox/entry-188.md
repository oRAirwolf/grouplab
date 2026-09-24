## 2026-09-24, entry 188: request 17 is done; check the next CI run reads the draft

Alan ran request 17 on 2026-09-24, in PowerShell:

- `gh release edit test-data --draft=true` printed `https://github.com/oRAirwolf/grouplab/releases/tag/untagged-5682142e6c7eae4fcdbc`.
- `gh release list --limit 5` now shows only "Latest nightly" and nightlies 99 to 96. "Test data, not a build" is gone from the list.

Mark request 17 answered. Then check two things on the next CI run, and put the answer in the report in plain words:

1. The address changed to `untagged-...`, which is what GitHub shows for a draft. Confirm the `test-data` tag itself still exists
   (`git ls-remote --tags origin test-data`), and that the test data job finds the draft through the API by that tag or by its id, not
   by the public download address.
2. That job's log says it read the release through the API and the tests that use the file ran, not skipped.

If either fails, the request's own undo is `gh release edit test-data --draft=false`. Put that in for-alan.md as a new request
rather than running it, since it needs Alan's approval.
