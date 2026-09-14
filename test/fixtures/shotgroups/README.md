# shotGroups reference fixtures

Generated output from the R package **shotGroups**, checked in so that the M3
statistics gate can run without an R installation. These are the numbers
`docs/STATISTICS.md` section 15 measures GroupLab against.

## Provenance and licence

| | |
|---|---|
| Package | shotGroups 0.8.4 |
| R | 4.3.3 |
| `coin` installed | **yes**, so `compareGroups` used the **exact permutation** branch |
| `mvoutlier` installed | no; `groupShape` warns and its robust branch falls back |
| Generated | 14 September 2026, regenerated 15 September with the point of aim |
| Author | Daniel Wollschlaeger |
| Licence | GPL-2 or later |

shotGroups is GPL-2-or-later and GroupLab is GPL-3.0, so there is no licence
conflict. **These files are shotGroups' output, not GroupLab's own data.** They
are included as a comparison reference with attribution, per `STATISTICS.md`
section 15.1, and nothing in the application may read them at runtime. They
exist for the test suite. Attribution to Wollschlaeger belongs in the
repository per `DESIGN.md` section 4.

## Files

| File | Rows | What |
|---|---|---|
| `shotGroups_DF300BLK.*` | 600 | 20 shots, 1 group, 100 yd, inches. The canonical single-group case |
| `shotGroups_DFscar17.*` | 520 | 10 shots, 1 group. Small `n`, where the `c4` correction is largest |
| `shotGroups_DFcciHV.*` | 1857 | 40 shots, 2 groups. The Ansari-Bradley and Wilcoxon branch |
| `shotGroups_DF300BLKhl.*` | 2553 | 60 shots, 3 groups. The Fligner-Killeen and Kruskal-Wallis branch |
| `shotGroups_DFcm.*` | 9464 | 487 shots, 9 series, 25 m, cm. Metric half of the unit-conversion test |
| `shotGroups_DFinch.*` | 9464 | 487 shots, 9 series, 27.34 yd, inches. Imperial half. **Nearly the same data, see below** |
| `shotGroups_DFsavage.*` | 6563 | 180 shots, 9 series, three distances. The angular negative test |
| `shotGroups_DFlandy04.*` | 5108 | 175 shots, 6 groups, unequal sizes |
| `shotGroups_DFlandy01.*` | 32543 | 530 shots, 53 groups |
| `shotGroups_DFdistr.*` | 590 | shotGroups' Monte Carlo range-statistic table |

Each dataset is emitted twice with identical content: `.json` is canonical and
is what the harness reads, `.csv` is the same rows in long form for diffing by
eye. About 13 MB in total, most of it `DFlandy01`.

## Key scheme

`<scope>.<function>.<component>.<row>.<col>`, order-independent and diffable.

`scope` is **empty for whole-dataset results**, so every key the earlier
version of `sg_dump.R` produced is unchanged and still carries the same value.
That was checked: on `DF300BLK` all 454 original keys are present and all 454
values match, excepting the one stochastic key below.

Per-group results carry `series.<label>.` in front. The package keys
multi-group data on `series` and not on `group`, and in `DFsavage` the two
disagree, 1 group against 9 series. This dump follows `series` throughout and
records both labels against every shot, as `shots.seriesIndex` and
`shots.groupIndex`, so a reader can see the difference rather than inherit it.

## What is in a fixture beyond the original dump

- **`shots.x`, `shots.y`, `shots.xPOA`, `shots.yPOA`, `shots.distance`** for
  every shot. Without the input coordinates a reimplementation has to obtain
  them from the very package it is being validated against, which is not an
  independent test.

  **The point-of-aim pair was added on 15 September and it matters.**
  `groupLocation`, `groupSpread` and `groupShape` take the data frame and use
  each shot's aim; `getXYmat(..., relPOA = FALSE)` does not carry it. A fixture
  built only from the matrix therefore cannot reproduce anything those three
  computed, which cost a reimplementation 2,163 keys before this was found. It
  shows as the frame-based centre disagreeing with the matrix-based one, in
  every scope of `DFcm` and `DFinch` and in none of the other seven, because
  only those two have a non-zero aim: **9 distinct aim points each**, up to
  20.4 cm and 8.0 in from the origin. On `DFinch` the two centres sit 7.1 in
  apart. Both coordinate forms are emitted rather than one, because
  `xyTopLeft = TRUE` flips y and a reader deriving either from the other has a
  sign convention to get wrong.
- **Per-series results**, the whole battery, for every multi-group dataset.
  The pooled figures are kept but they describe a group that was never fired.
- **`compareGroups`**, both branches, with the test names probed rather than
  assumed so the fixture records which branch actually ran.
- **`multiGroup.*`**, the real multi-group range statistics, see below.
- **`angular.nDistances`** always, and `getMOA` / `fromMOA` only when there is
  exactly one distance in the frame.

## Four things worth knowing before using these

**One value is stochastic.** `groupShape.multNorm.p.value` is a Monte Carlo
energy test and moves between two identical runs: 0.5424 and 0.5590 on
`DF300BLK`, 0.8346 and 0.8379 on `DFcciHV`. The script now seeds the generator
so regeneration reproduces, but **a seed does not make the value comparable**,
because another implementation draws from a different generator. The key is
listed in each JSON under `stochastic` so a harness can exclude it by name
rather than discover it as a failure at a 1e-12 tolerance. Every other value in
every fixture is deterministic, checked by running each dataset twice and
diffing.

**`DFsavage` emits no angular keys at all, and that is the result.**
`STATISTICS.md` section 15.5 point 3 wants multiple distances in one frame to
suppress angular output rather than produce a wrong number. `angular.nDistances`
is 3 and there are no `getMOA` or `fromMOA` keys. The count is recorded either
way, so the absence is positive evidence rather than a gap.

**Range statistics stop at n = 100.** That is the largest cell shotGroups
tabulates. `getRangeStat` itself runs at any size, warning and returning NA
intervals past the table, but `range2sigma`, `range2CEP` and `getRangeStatEff`
raise an error. On the pooled scope of `DFcm`, `DFinch`, `DFsavage`,
`DFlandy04` and `DFlandy01` those three are recorded as `_error` keys. The
per-series scopes are all well inside the table and complete.

**`DFcm` and `DFinch` are not quite the same data**, which `STATISTICS.md`
section 15.2 assumes they are. Every `DFcm` shot is a `DFinch` shot times 2.54
to 1e-15, but one shot, number 242, sits in series 5 of one frame and series 4
of the other, so those series hold different shots and different counts. The
unit-conversion gate of section 15.5 point 2 can be met on the shots; it cannot
be met on the series as shipped.

**`DFlandy01` does not exercise what it was chosen for.** `STATISTICS.md`
section 15.2 picked it for "range statistics with many groups", but
`getRangeStat` takes a coordinate matrix and has no group argument: handed a
53-group frame it pools all 530 shots into one. The package's actual
multi-group path is the `nGroups` argument of `range2sigma`, `range2CEP` and
`getRangeStatEff`, which is emitted here under `multiGroup.*`. **Those tabulate
only up to 10 groups**, so `DFlandy01` at 53 is past the table and carries
`multiGroup.beyondTable` instead of values. `DFlandy04` at 6 groups is the
fixture that actually exercises the multi-group range path.

## Regenerating

Needs R with shotGroups, and `jsonlite` and `coin` for full output.

```sh
for d in DF300BLK DFscar17 DFcciHV DF300BLKhl DFcm DFinch DFsavage DFlandy04 DFlandy01; do
  Rscript tools/shotgroups/sg_dump.R "$d" "test/fixtures/shotgroups/shotGroups_$d"
done
Rscript tools/shotgroups/sg_distr.R test/fixtures/shotgroups/shotGroups_DFdistr
```

Regeneration is an occasional maintenance task pinned to a stated shotGroups
version, not part of the build. If the version changes, change the table at the
top of this file in the same commit, and expect values to move.

Note that `STATISTICS.md` section 15.1 says the R script lives under
`test/fixtures/shotgroups/` alongside the fixtures. It is in `tools/shotgroups/`
instead, where it already was. Reconcile the wording or move the script, but do
not leave the document describing a layout the repository does not have.
