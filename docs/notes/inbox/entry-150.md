# 2026-09-23, entry 150: an executable is built only when the application changes

Alan, reading the releases page: "it seems like a lot of builds and releases are extremely minor, like
just updating release note pages being brought up to date or research articles were written. Why does
this need a new executable? Can't these things be done without creating a new executable and having to
get compiled and tested? That seems like a total waste of time." He names nightly 84, which was all
website changes, and the release note entry that only explained why the history starts where it does.

He is right, and this is the first entry in the queue because every later entry in it writes website
content. Fix the waste before doing the work that would multiply it.

## 1. What a build is for

**A new executable exists only when something that ships inside the executable changed.** That is:

- `src/**`
- `tests/**` where a test failure would have stopped the build
- project and dependency files: `*.csproj`, `Directory.Packages.props`, `Directory.Build.props`,
  `global.json`, `NuGet.config`
- `targets/**` where a definition is embedded in the application rather than only printed by the site
- the installer and packaging inputs
- the build and release scripts and workflows themselves

Everything else is content. `website/**`, `docs/**`, `README.md`, the research articles, the tour pages
and `docs/RELEASE-NOTES.md` are content, and content publishes through the site path that entry 144
built. Content never produces an executable.

Write this list down in one place that both the workflow and a test read, so the two cannot drift.
A path that is in neither list is a failure, not a default, because a new top level directory should
make somebody decide which side it is on rather than silently picking one.

## 2. The gate

A scheduled workflow does not get a paths filter, so build the gate as a job:

1. A first job compares the commit the last nightly was built from against the commit the nightly would
   build now, and outputs a boolean `application-changed`.
2. Every build, test, package and release job depends on that boolean.
3. Where nothing shipping changed, the run ends immediately, writes one line into the workflow summary,
   **"No application change since nightly NN. No build produced."**, and creates no release, no tag and
   no assets.
4. A skipped night does not consume a nightly number. Nightly numbers count builds, not days.

## 3. The two loops that must stay shut

1. **The nightly appends its own release note entry and pushes.** Entry 144 guarded that loop. Verify
   the guard still holds with the gate in place, because a release notes commit is now content and must
   not itself wake the nightly.
2. **The site publish workflow must not trigger the nightly, and the nightly must not trigger the site
   publish** except through the release note append that entry 144 already defined.

Prove both with a run, not with a reading of the YAML.

## 4. Say it on the releases page

The releases page should not look like a page nobody updated on the nights when nothing was built.
Add one sentence at the top of the list: nightly builds are produced on the nights the application
changed, and a gap in the numbers means nothing shipped that night. That is the same principle as
entry 145: say plainly what happened, including when what happened was nothing.

## 5. Measure the waste that has already happened

In the run report, not in a committed file, list every nightly of the last 30 days whose diff against
the previous nightly touched no path in section 1's shipping list. That number is the answer to Alan's
question and it is worth knowing. Do not delete or edit any published release.

## 6. Tests

1. A test that fails if the nightly workflow has no gate job, or if any build job does not depend on it.
2. A test over the two path lists in section 1: every top level directory in the repository appears in
   exactly one of them.
3. A dry run of the gate against the last 30 nightlies that prints which would have been skipped, so
   the logic is exercised against real history rather than against a fixture.
