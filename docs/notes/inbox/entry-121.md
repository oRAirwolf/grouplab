# 2026-09-21, entry 121: v0.1.0 was published as a release, so nightly starts at 0.2.0

Before entry 119 reached you, Alan had already followed my earlier advice and pushed the tag `v0.1.0`. `release.yml` ran on it and published a full release: the GitHub API shows release `v0.1.0`, not a draft, not a pre-release, six assets, published 2026-09-21T03:35:15Z, and it is what `releases/latest` returns. That advice was mine and it is now wrong for the plan in entry 119. This entry puts it right.

## 1. The problem

Under SemVer, `0.1.0-nightly.N` sorts **below** `0.1.0`. With `<Version>` still 0.1.0, every nightly would be older than the published release: a nightly user's updater would offer 0.1.0 as newer (entry 119 section 4.5 lets nightly users take a newer release), which is a downgrade to older code, and the nightly train would never look newer than anything already public.

## 2. What to do

1. Set `<Version>` in `Directory.Build.props` to `0.2.0`, so nightly builds are `0.2.0-nightly.N`, which sorts above `0.1.0`. Add a test that the version in `Directory.Build.props` is greater than every non-nightly `v*` tag in the repository, so this cannot happen again.
2. Treat `v0.1.0` as history, outside every train. It has no update manifest, so the updater cannot see it; keep it that way. Never publish a manifest for it and never point a train at it. Do not delete the tag or the release; Alan will mark it as a pre-release himself on GitHub so that `releases/latest` stops pointing at it.
3. The release and beta trains stay greyed out as entry 119 says. The existence of `v0.1.0` does not make the release train available.
4. The README links only to the rolling `nightly` release (entry 119 section 7). Nothing in the repository links to `v0.1.0` or to `releases/latest`; extend `ReleaseAssetTests` to fail if either appears.
5. If you already published any `0.1.0-nightly.N` builds, leave them; the next nightly will be `0.2.0-nightly.N` and sorts above them. Say in your report which versions exist.
6. If the workflow you built for entry 119 is named anything other than `nightly` (the Actions list showed a workflow named "test build" at 04:46 UTC), rename it to `nightly` and make sure only one such workflow exists.

## 3. CI on main

The Actions list at about 04:50 UTC showed "build and test" on `main` completed with **failure** for the run created at 04:18:25 UTC, while the same commit's run on `phase-1` was still in progress. Find out why, fix it, and say what it was. A nightly must never be built from a commit whose CI failed.

## 4. Prove it

Report the version of the newest nightly, confirm with the API that `releases/latest` no longer returns `v0.1.0` after Alan changes it (or that it still does if he has not yet, and say so), and confirm the updater on a 0.2.0 nightly does not offer `v0.1.0`.
