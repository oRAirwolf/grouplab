# GroupLab Statistics Reference

**Version** 1.0 (draft)
**Implements** DESIGN.md section 14
**Validated against** shotGroups 0.8.4 (Daniel Wollschlaeger, CRAN, GPL >= 2), R 4.3.3
**Status** Specification for review. No application code written.

Every closed-form estimator in section 3 was implemented independently in R during the writing of this document and checked against shotGroups on its `DF300BLK` fixture. Agreement was **exact to machine precision**, delta 0.000e+00 on sigma, its confidence interval, mean radius, median radius, radial standard deviation, and CEP at the 50th, 90th and 95th percentiles. Every simulation figure quoted is from a run reported here, with the seed and the replication count stated.

---

## 1. Principles

Four rules that decide every design question below.

**R1. Never report a point estimate without an interval.** DESIGN.md section 2's governing principle is that the software should never help a shooter believe something the data does not support. A five-shot group gives a dispersion estimate whose 95 percent interval spans a factor of 2.8. Printing one number is not neutral; it is misleading, and the interval is the correction.

**R2. Prefer the efficient estimator, but report the traditional one.** Rayleigh sigma uses every shot. Extreme spread uses two. Section 5 quantifies the gap. But extreme spread is the language the audience speaks, so it is reported, labelled, and placed next to something better.

**R3. State the assumption alongside the number.** Almost every closed form below assumes a circular bivariate normal. Where the group is not circular, the number is wrong in a knowable direction, and the software should say which.

**R4. Estimating the centre costs two degrees of freedom, and the code must never forget it.** This is the single most commonly botched detail in shooting statistics, and section 3.2 shows exactly where it enters.

---

## 2. Notation and the model

`n` shots. Shot `i` has coordinates `(x_i, y_i)` in the composite group frame, which is the offset from that shot's own bull centre. Distances are linear at the target plane, in the canonical storage unit of DESIGN.md section 14.

**What a composite group from a sheet of bulls measures, `docs/NOTES-FROM-PLANNING.md` entry 72 section 2.** With one shot per bull, the offsets pool two things that no statistic can separate from one sheet: the rifle's dispersion, and the shooter re-aiming at a different bull for every shot. A sight zeroed for another distance adds a different correction on every bull, and at short range, where a subsonic load prints furthest from where it is aimed, that term is at its largest. **So a sigma from such a sheet is the sheet's dispersion, with both sources in it, and is never reported as the rifle's.** This is a property of every load development sheet GroupLab prints, not a fault of any one target.

`c` is the group centre, `(x̄, ȳ)` unless a known point of aim is supplied. `r_i` is the radial distance from shot `i` to the centre.

**The working model** is the bivariate normal with covariance `Σ`. The **circular** case is `Σ = σ²I`, under which `r` follows a Rayleigh distribution with scale `σ`. Most closed forms assume it. The **elliptical** case is general `Σ`, under which `r` follows a Hoyt distribution.

Whether the circular assumption holds is not asserted, it is **tested**, in section 7, and the answer conditions which estimators are shown.

**The coordinates are taken as exact, and they are not.** Added 15 September 2026, `docs/NOTES-FROM-PLANNING.md` entry 52 section 4. Every interval in this document describes shot-to-shot dispersion, the scatter of the rifle and the shooter about their centre. None describes how far a coordinate would move if the same photograph were measured again, and the pipeline has at least two places where it would. Both were measured on the Phase 0 photographs (`docs/PHASE1-RESULTS.md` "Entry 52 sections 3 and 4"):

- **Registration of a sheet that is not flat.** The same corners were handed to the homography fit in 200 orders, on each of seven mounted photographs. They gave 15 to 70 distinct results per frame, and a worst bull spanning up to threefold, 0.030 to 0.096 in on one frame. On two of the three flat photographs the result was identical all 200 times, and on the third it took two values.
- **The edge fit on a bull seen by few edge points.** Leaving any one point out moves a bull by at most 0.0002 in where it has 106 to 900 points. On the one photograph whose sheet overflows the frame, a bull with 14 points moves by up to 0.0032 in.

**An interval computed from coordinates that can move is narrower than the truth by however much they move.** On the flat photographs the registration's share was nothing on two and 0.0035 in on the third. On a mounted sheet it reaches hundredths, and nothing the application prints includes it yet. It is recorded here as a known source of uncertainty, measured on these frames and not yet carried into any figure a user sees.

---

## 3. Rayleigh sigma, the preferred estimator

### 3.1 Why it is preferred

`σ` is the scale parameter of the whole radial distribution. Every other circular measure is a fixed multiple of it, so estimating `σ` well estimates all of them well. It uses all `n` shots. It has a closed-form confidence interval. And its sampling distribution is exactly known, which makes both significance testing and sample-size planning exact rather than simulated.

### 3.2 The estimator, exactly

Let `p = 2` (dimensions) and

```
rSqSum = Σᵢ ‖(xᵢ, yᵢ) − c‖²
```

**Centre estimated from the data**, which is the normal case:

```
varHat  = rSqSum / (p · (n − 1))          = rSqSum / (2(n − 1))
corrFac = 1 / c4(p·n − (p − 1))           = 1 / c4(2n − 1)
dfChi   = p · (n − 1)                     = 2(n − 1)
```

**Centre known**, when a true point of aim is supplied:

```
varHat  = rSqSum / (p · n)                = rSqSum / (2n)
corrFac = 1 / c4(p·n + 1)                 = 1 / c4(2n + 1)
dfChi   = p · n                           = 2n
```

In both cases

```
σ̂ = corrFac · √varHat
```

where `c4` is the standard bias-correction factor for the square root of a variance estimate,

```
c4(k) = √(2/(k−1)) · exp( lnΓ(k/2) − lnΓ((k−1)/2) )
```

clamped to 1 when it would exceed 1 or overflow. Compute it through the **log-gamma** function, never through a ratio of gamma functions, which overflows above about `k = 340` and is reachable with a large pooled group.

**The two cases differ in three places at once**, and getting one right while getting another wrong produces an estimate that is close enough to look correct and wrong enough to matter. This is the first unit test to write.

### 3.3 The confidence interval

`rSqSum / σ²` is chi-square distributed on `dfChi` degrees of freedom, so

```
σ̂_lo = corrFac · √( rSqSum / χ²(1 − α/2, dfChi) )
σ̂_up = corrFac · √( rSqSum / χ²(α/2, dfChi) )
```

Note that the bias-correction factor multiplies the endpoints as well as the point estimate. That is what shotGroups does, GroupLab matches it, and the choice should be documented rather than silently inherited: it keeps the interval centred on the corrected estimate at the cost of no longer having exactly nominal coverage for `σ` itself. The difference is small and it is a deliberate compatibility decision.

**Verification.** Independent implementation against shotGroups 0.8.4 on `DF300BLK`, 20 shots at 100 yd:

| Quantity | GroupLab formula | shotGroups `getRayParam` | Delta |
|---|---|---|---|
| σ̂ | 1.420090647279 | 1.420090647279 | 0.000e+00 |
| lower 95 | 1.160563286589 | 1.160563286589 | 0.000e+00 |
| upper 95 | 1.830181915497 | 1.830181915497 | 0.000e+00 |

### 3.4 Everything else is a multiple of sigma

Under the circular model:

| Measure | Multiple | Value |
|---|---|---|
| Mean radius `MR` | `σ·√(π/2)` | 1.2533141373 σ |
| Median radius `MEDR` | `σ·√(2 ln 2)` | 1.1774100226 σ |
| Radial standard deviation `RSD` | `σ·√((4−π)/2)` | 0.6551363776 σ |
| `CEP(q)` | `σ·√(−2 ln(1−q))` | see below |
| `CEP(0.50)` | | 1.1774100226 σ |
| `CEP(0.90)` | | 2.1459660263 σ |
| `CEP(0.95)` | | 2.4477468307 σ |

Their confidence intervals are the sigma interval rescaled by the same constant. This is what shotGroups does and it is correct under the model: the constants carry no sampling error.

Verified exactly against `getRayParam` and `getCEP(type="Rayleigh")` at all three levels, delta 0.000e+00.

**Two honesty requirements.** The mean radius **computed directly** as `(1/n)Σrᵢ` is not the same number as `σ̂·√(π/2)`; the first is an estimate that makes no distributional assumption, the second is model-based and more efficient. Report the model-based one as the headline and keep the direct one available, because a large discrepancy between them is itself diagnostic of a non-circular or contaminated group. And every one of these numbers **inherits the circular assumption**. When section 7 rejects circularity, they must be presented as approximations with the direction of error stated, or replaced by the elliptical forms in section 4.

---

## 4. CEP for the elliptical case

When the group is not circular, `CEP` has no simple closed form and the literature offers a dozen estimators. shotGroups implements eleven. GroupLab should not.

**Implement three, in this order.**

| Estimator | Basis | Why |
|---|---|---|
| **`CorrNormal`** | Correlated bivariate normal in polar coordinates, that is the Hoyt distribution | The reference. Exact under the model at every probability level. This is the one to be right about |
| **`Rayleigh`** | Circular closed form | Exact when the group is circular, which is the common case, and trivially cheap. Warn when the error-ellipse aspect ratio exceeds 2 |
| **`GrubbsPatnaik`** | Patnaik two-moment central chi-square approximation | A fast, well-understood approximation, useful as an independent cross-check on the exact one |

Deliberately not implemented in version one: `GrubbsPearson`, `GrubbsLiu` (identical to Pearson when accuracy is off), `Krempasky`, `Ignani`, `RMSE`, `Ethridge`, `RAND`, `Valstar`. Several are restricted to the 50 percent level, several do not generalise, and one, `RMSE`, is characterised by shotGroups' own documentation as becoming "seriously wrong" when bias is not small. Implementing eleven estimators for a user who cannot choose between them is not a feature. **Implementing the exact one properly is.**

Reference values on `DF300BLK` at the 50 percent level, produced by shotGroups, which are the validation targets:

```
CorrNormal    1.50022083074366
GrubbsPatnaik 1.49502636547150
GrubbsPearson 1.45824737501618
Rayleigh      1.67202896098667
```

The spread between them, 1.458 to 1.672 or about 15 percent, is itself worth showing a user once. It is a concrete demonstration that "the CEP" is not a single well-defined number when the group is not circular.

**Hit probability** inverts the same machinery. Under the circular model,

```
P(hit within radius R) = 1 − exp( −R² / (2σ̂²) )
```

Verified against `getHitProb(type="Rayleigh")` at `r` = 0.5, 1 and 2: agreement to 1.1e-16. For the offset case, where the group centre is not the aim point, this becomes the Rice distribution, and shotGroups' `accuracy=TRUE` path is the reference.

Per DESIGN.md section 14, hit probability **at distance** goes through the ballistic solver rather than by scaling the group linearly. Section 12 covers that.

---

## 5. Extreme spread, and why it is demoted rather than removed

Extreme spread is the maximum pairwise distance. It uses two of `n` shots and discards the rest, it is the maximum of a set so it is biased upward by every outlier, and its distribution has no closed form.

**The cost, measured.** From shotGroups' own Monte Carlo tables via `efficiency()`:

| Goal | Via extreme spread | Via Rayleigh sigma |
|---|---|---|
| 95 percent CI of width 20 percent, 5-shot groups | 28 groups, **140 shots** | 25 groups, **124 shots** |
| 95 percent CI of width 20 percent, 25-shot groups | 7.5 groups, **188 shots** | 4 groups, **101 shots** |

At five shots per group the penalty is modest, about 13 percent more ammunition, because with five shots the extreme spread is not throwing away much. **At twenty-five shots per group the penalty is 86 percent more ammunition for the same confidence.** That is the number to show a shooter, and it is precisely the regime GroupLab's one-shot-per-bull design puts them in.

Put differently, from a single group:

| | 95 percent CI on sigma | Ratio of upper to lower |
|---|---|---|
| One 5-shot extreme spread | 0.207 to 0.622, from ES = 1.00 | **3.01** |
| One 25-shot Rayleigh sigma | 0.834 to 1.249 times σ̂ | **1.50** |

**What GroupLab does.** Report extreme spread, because it is the language of the audience and refusing to speak it is not honesty, it is rudeness. Report it with a confidence interval, from the Monte Carlo lookup table, so the user can see how wide it is. Place the Rayleigh figures next to it. And offer conversion in both directions, since a shooter arriving with a lifetime of extreme-spread numbers needs a bridge: `range2sigma` and `range2CEP` in shotGroups are exactly that bridge and their behaviour should be reproduced.

The lookup tables come from Wollschlaeger's own ten-million-replication Monte Carlo simulation, shipped as `DFdistr`, not from the Taylor and Grubbs tables the documentation cites as background. **GroupLab must generate its own tables** rather than shipping a GPL data file, and must validate them against `DFdistr`. That is a specific Phase 2 task with a specific acceptance test, and it is in section 15.

---

## 6. Confidence intervals: closed form where possible, bootstrap where not

| Quantity | Method | Notes |
|---|---|---|
| Rayleigh sigma, MR, MEDR, RSD, circular CEP | **Closed form, chi-square** | Section 3.3. Exact under the model |
| Per-axis standard deviations | **Closed form, chi-square on `n−1` df** | Univariate, so `n−1`, not `2(n−1)`. Easy to get wrong by copying the sigma code |
| Group centre | **Closed form, Hotelling T²** | Also gives the confidence ellipse |
| Extreme spread, figure of merit, bounding-box diagonal | **Monte Carlo lookup** | No closed form exists |
| Minimum enclosing circle radius, bounding box dimensions | **Bootstrap** | Order statistics of a geometric construction |
| Elliptical CEP | **Bootstrap** | shotGroups has this on its own to-do list, unimplemented |
| Difference or ratio between two groups | **Closed form where the F test applies**, bootstrap otherwise | Section 8 |

**Bootstrap specification**, so that two implementations can agree:

- **BCa** by default, which corrects for both bias and skewness and matters here because these statistics are skewed. Percentile as a fallback when BCa's acceleration cannot be computed.
- **9999 resamples** for a 95 percent interval. Not 1000. The Monte Carlo error on a 2.5 percent quantile from 1000 resamples is large enough to be visible in the reported digits, which makes the software look non-deterministic.
- **Seed recorded** in the analysis record, so a reported interval can be reproduced exactly. This is not optional in a measurement tool.
- Resample **shots**, not residuals.
- With fewer than about 10 shots, report that the bootstrap interval is unreliable rather than reporting it silently.

---

## 7. Shape: circularity and vertical stringing are two different questions

DESIGN.md section 14 asks for a "bivariate normal fit with a test for circularity, which detects vertical stringing". Those are two different hypotheses and conflating them produces a test that fires on the wrong thing.

**Question A, is the group circular?** `H0: Σ = σ²I`. This is rotation-invariant, so it also rejects a group elongated diagonally. The likelihood-ratio test statistic is

```
−2 ln Λ = −n · ln( det S / (tr S / 2)² ),   S the maximum-likelihood covariance
```

which is asymptotically chi-square on 2 degrees of freedom.

**Question B, is the group stringing vertically?** `H0: σ_x = σ_y` in **target coordinates**, with the correlation left free. A group tilted 45 degrees is non-circular but is not stringing, and a shooter told "your group is not circular" when the elongation is diagonal has been told something true and useless.

The right test for B is **Pitman-Morgan**. Under `H0`, `U = x + y` and `V = x − y` are uncorrelated, because `Cov(U,V) = Var(x) − Var(y)`. So

```
t = r_UV · √(n − 2) / √(1 − r_UV²),   on n − 2 degrees of freedom
```

and it is a **one-sided** test when the alternative is specifically vertical stringing.

**Both were simulated.** 20,000 to 40,000 replications, seeds 7 and 11, reported here because the small-sample behaviour decides how they should be used.

Size of the sphericity likelihood-ratio test on genuinely circular data, at nominal 5 percent:

| n | Uncorrected | Bartlett-corrected |
|---|---|---|
| 5 | 0.165 | 0.103 |
| 10 | 0.091 | 0.069 |
| 15 | 0.075 | 0.061 |
| 20 | 0.067 | 0.059 |
| 25 | 0.063 | 0.057 |
| 50 | 0.058 | 0.053 |
| 100 | 0.054 | 0.050 |

The Bartlett correction multiplies the statistic by `1 − 1/n` for the bivariate case. It helps and it is not enough at small `n`: at five shots the corrected test still rejects circular data 10 percent of the time at a nominal 5 percent.

Pitman-Morgan, by contrast, held size at 0.049 to 0.051 at every combination tested, `n` of 10 and 25 crossed with correlation 0 and 0.5. It is exact.

**Therefore.** Use the **Bartlett-corrected likelihood-ratio test for circularity at `n ≥ 20`**, and a parametric bootstrap calibration below that. Use **Pitman-Morgan for vertical stringing at any `n`**. Label them differently in the interface, because they answer different questions.

**And be honest about power, which is the more useful number.** Shots needed for 80 percent power to detect vertical stringing at 5 percent, by simulation:

| σ_y / σ_x | Shots needed |
|---|---|
| 1.25 | **155** |
| 1.50 | **50** |
| 2.00 | **19** |

A 25-shot group has roughly 49 percent power against 1.5 times stringing. **Half the time, a group that really is stringing by fifty percent will not be flagged.** That belongs in the interface next to the result, not in a footnote, because "no significant stringing detected" from 25 shots means very little and users will read it as meaning a lot.

Also report, always, and without a test attached: the **error-ellipse aspect ratio** and its orientation. It is descriptive, it needs no assumption, and for most users it is the more useful output.

**Beside it, what a circular group of that size gives** (NOTES-FROM-PLANNING.md entry 76 section 1, the pattern of section 10). A descriptive aspect read alone misleads at small `n`, because sampling alone elongates a circular group. The aspect is `√(l₁/l₂)`, with `l₁ ≥ l₂` the eigenvalues of the centred sums of squares. For `n` circular shots those follow a 2 by 2 Wishart distribution on `m = n − 1` degrees of freedom. Integrating out the scale leaves, for `u = 1/aspect` on (0, 1),

```
f(u) ∝ u^(m−2) · (1 − u²) / (1 + u²)^m
```

which is exact. The table prints its values. Entry 76's simulated quantiles at 10 and 12 shots agree with them within simulation noise, the largest difference being 0.01 at the tail, and a seeded simulation agrees as well (`CircularAspectTests`).

| n | median | 75th | 90th | 95th | 99th percentile |
|---|---|---|---|---|---|
| 5 | 2.03 | 2.82 | 4.06 | 5.24 | 9.17 |
| 10 | 1.53 | 1.83 | 2.22 | 2.51 | 3.25 |
| 12 | 1.46 | 1.71 | 2.02 | 2.26 | 2.81 |
| 25 | 1.28 | 1.42 | 1.58 | 1.68 | 1.92 |

The report carries the median and the probability that circular shots exceed the measured aspect, and the screen reads, for example, "aspect 2.82; ten circular shots give about 1.5 and exceed 2.82 one time in forty". This is still not the test for circularity above. It is a reference, so the reader can see whether an elongation is unusual for the count, and it carries no verdict.

---

## 8. Comparing two loads

The question a handloader actually asks: does load A genuinely group better than load B, or is the difference noise?

### 8.1 Dispersion, which is the question that matters

Under the circular model this has an **exact closed form**, and it is worth using rather than reaching for a bootstrap.

`rSqSum / σ²` is chi-square on `2(n−1)` degrees of freedom for each group independently, so under `H0: σ_A = σ_B`

```
F = ( rSqSumA / dfA ) / ( rSqSumB / dfB )  ~  F( dfA, dfB ),   df = 2(n − 1)
```

Note the estimator's `c4` bias correction **cancels** in the ratio when the two groups have the same `n`, and nearly cancels when they do not. The test statistic should be built from `rSqSum` directly, not from `σ̂`, to avoid reintroducing it.

Report the **ratio with a confidence interval**, not just a p-value:

```
σ̂A/σ̂B  divided by  √F(1−α/2, dfA, dfB)   to   σ̂A/σ̂B  divided by  √F(α/2, dfA, dfB)
```

"Load A's dispersion is 0.87 times load B's, 95 percent CI 0.71 to 1.08" is a far better answer than "p = 0.21", and it is the same computation.

### 8.2 Location

Hotelling's T² for a difference in group centres, which is the standard two-sample multivariate test and is what shotGroups' MANOVA path reduces to for two groups.

### 8.3 Non-parametric backstops

When the circularity test in section 7 rejects, or when the group is visibly contaminated, the F test's assumption is broken. shotGroups' `compareGroups` runs Ansari-Bradley on each axis and Wilcoxon on the distance-to-centre for the two-group case, and Fligner-Killeen with Kruskal-Wallis for more than two. Reproduce those as the fallback path.

One implementation warning that will otherwise cost a day: **shotGroups produces different p-values depending on whether the `coin` package is installed**, because it switches between exact permutation distributions and base R's asymptotic approximations. Any validation fixture must record which branch generated it.

### 8.4 Multiple comparisons

A user testing six powder charges against each other is running fifteen tests, and at 5 percent will find a "significant" difference by chance about half the time. GroupLab must **correct, and say that it corrected**. Holm-Bonferroni for the family of pairwise comparisons within one comparison view. This is not in DESIGN.md and it should be, because a tool built around not letting people fool themselves that ships an uncorrected pairwise comparison matrix has a hole in exactly its stated purpose.

---

## 9. Sample size planning

The most useful and least welcome output in the whole application.

### 9.1 How well is sigma known from n shots?

`σ̂` has a coefficient of variation of approximately `1 / (2√(n−1))`. Simulated at `n = 25` over 40,000 replications: predicted 10.21 percent, observed 10.26 percent, with a bias of −0.03 percent, confirming both the correction factor and the approximation.

| n | df | CV of σ̂ | 95 percent CI as a multiple of σ̂ | CI width |
|---|---|---|---|---|
| 3 | 4 | 35.4 % | 0.599 to 2.874 | 2.27 σ̂ |
| **5** | 8 | **25.0 %** | **0.675 to 1.916** | 1.24 σ̂ |
| 10 | 18 | 16.7 % | 0.756 to 1.479 | 0.72 σ̂ |
| 15 | 28 | 13.4 % | 0.794 to 1.352 | 0.56 σ̂ |
| 20 | 38 | 11.5 % | 0.817 to 1.289 | 0.47 σ̂ |
| **25** | 48 | **10.2 %** | **0.834 to 1.249** | 0.42 σ̂ |
| 30 | 58 | 9.3 % | 0.847 to 1.222 | 0.38 σ̂ |
| 50 | 98 | 7.1 % | 0.877 to 1.163 | 0.29 σ̂ |
| 100 | 198 | 5.0 % | 0.910 to 1.109 | 0.20 σ̂ |

**The five-shot row is the one to put in front of a user.** A five-shot group locates the rifle's true dispersion somewhere between 0.675 and 1.916 times the measured value, a factor of 2.84. Two rifles whose five-shot groups differ by a factor of two are entirely consistent with being identical.

### 9.2 How many shots to prove load A beats load B?

Under the F test of section 8.1, the shots per load needed to detect a dispersion ratio `k` with 80 percent power at 5 percent two-sided:

```
n ≈ 1 + (z_{α/2} + z_β)² / (2 · (ln k)²)
```

The approximation was checked against an exact search over the F distribution and agrees to within one shot everywhere tested:

| k | Improvement | Approximation | Exact | Shots per load |
|---|---|---|---|---|
| 1.05 | 4.8 % tighter | 1650 | 1651 | **1651** |
| 1.10 | 9.1 % tighter | 434 | 434 | **434** |
| 1.15 | 13.0 % tighter | 202 | 203 | **203** |
| 1.25 | 20.0 % tighter | 80 | 81 | **81** |
| 1.50 | 33.3 % tighter | 25 | 26 | **26** |
| 2.00 | 50.0 % tighter | 10 | 10 | **10** |

**This table is the single most valuable thing in the application.** The entire practice of load development by comparing three-shot groups is answered by it. Detecting a genuine ten percent improvement takes **434 shots per load**, which is more than most barrels last. Detecting a third takes 26, which is a morning.

The correct interface behaviour is not to refuse the comparison. It is to run it, report the interval, and say what the data can and cannot support. "These 20 shots per load cannot resolve a difference smaller than about 45 percent. The observed difference is 12 percent, 95 percent CI −18 to 52 percent" is a complete, honest answer, and it is more useful than either a p-value or a refusal.

**A design consequence worth stating.** These numbers make the pooled and virtual groups of section 11 the most important feature in the statistics layer, not a convenience. A shooter who fires 25 rounds of a load per session over eight sessions has 200 shots, which is a usable sample. The same shooter looking at eight separate 25-shot groups has nothing. Pooling is what makes the arithmetic survivable.

---

### 9.3 How finely a multi-bull sheet can be spaced for a given group

(NOTES-FROM-PLANNING.md entries 56 and 76.) On a square lattice of spacing `s`, each shot is assigned to its nearest bull. A shot is assigned correctly while it stays inside its own bull's cell, a square of side `s`. For shots centred on the aim with per-axis sigma `σ`, the misassignment rate is therefore closed form:

```
P(misassigned) = 1 − (Φ(s/2σ) − Φ(−s/2σ))²
```

For a centre offset `(μx, μy)`, each axis term becomes `Φ((s/2 − μ)/σ) − Φ((−s/2 − μ)/σ)`.

| Spacing / σ | Misassigned per shot |
|---|---|
| 3 | 24.9% |
| 4 | 8.9% |
| 5 | 2.5% |
| **6** | **0.54%** |
| 7 | 0.09% |
| 8 | 0.013% |

**The rule:** bull spacing wants to be at least 6 σ at the shooting distance, and 7 σ is comfortable. For the 1.5 in spacing of `GL-CF25-LTR`, that means σ at or under 0.25 in on the paper.

**What the table rests on.**

- **Simulation.** Entry 56 checked it against 400,000 simulated shots at three ratios, and the two agree to three decimals.
- **Real paper: one sheet corroborates it, and only as a count of two.** That sheet is the friend's `GL-CF25-LTR` of entry 56.
  - Entry 76 section 2 suspected that sheet's figures were inflated by the sighter pooling defect, since fixed, so it was re-run through the fixed pipeline.
  - They were not. That sheet's sighters were never pooled: planning's hand analysis left them out, and so does the pipeline.
  - Its ten scoring holes give σ 0.390 in, against entry 56's 0.386 in. That is a spacing ratio of 3.84, where the table expects 1.1 misassigned shots in ten centred on the aim, or 2.1 at the group's measured offset of 0.23 in across and 0.29 in vertically.
  - **Observed: 2 of 10.**
- **The second real sheet**, Alan's scan of entry 72, sits at a ratio of 5.5 with 0 of 10 misassigned. The table expects 0.12 of a shot there, so that sheet agrees with the table but cannot test it.

**The table is corroborated on real paper by one sheet of ten shots, whose count agrees with it.** Two misassigned shots out of ten cannot distinguish 10 percent from 30 percent. So the corroboration is weak, and the table's authority is the closed form and the simulation.

## 10. Flyer handling

DESIGN.md section 14 requires that before permitting an exclusion, GroupLab states what the mathematics expects from a group that size. Here is the calculation.

For `n` shots from a circular bivariate normal, the expected maximum radius is

```
E[R_max] = σ · √(π/2) · Σ_{k=1}^{n} C(n,k) · (−1)^{k+1} / √k
```

derived from `E[max] = ∫₀^∞ (1 − F(r)ⁿ) dr` with `F(r) = 1 − exp(−r²/2σ²)`. Verified by simulation over 200,000 replications: at `n = 5`, formula 2.0675 against simulated 2.0676; at `n = 25`, formula 2.7274 against simulated 2.7274.

**Compute this with alternating-sign binomial care.** At `n = 25` the terms reach about 5.2 million and alternate in sign, so naive double-precision summation loses most of the significant digits. Sum in log space with sign tracking, or use the equivalent integral form. This is a real numerical trap and it belongs in the code comments.

**MR in this table is the population mean radius, `σ√(π/2)`, and every radius is measured from the true centre.** It describes shots from a gun whose σ and point of impact are known. It is not the reference for a worst shot measured against a group's own mean radius about the group's own centre, which is what a screen has; that case is below the table.

| n | E[worst] / σ | E[worst] / MR | P(worst > 2 × MR) | P(worst > 1.5 × MR) |
|---|---|---|---|---|
| 3 | 1.825 | 1.456 | 0.124 | 0.430 |
| 5 | 2.068 | 1.650 | 0.198 | 0.608 |
| 10 | 2.370 | 1.891 | 0.357 | 0.846 |
| 15 | 2.534 | 2.021 | 0.485 | 0.940 |
| 20 | 2.644 | 2.110 | 0.587 | 0.976 |
| **25** | **2.727** | **2.176** | **0.669** | **0.991** |
| 30 | 2.794 | 2.229 | 0.734 | 0.996 |

**The dialog text writes itself from the 25-shot row.** In a 25-shot group, the worst shot is *expected* to sit at 2.18 times the mean radius. The probability that it exceeds twice the mean radius is **0.67**, and the probability that it exceeds 1.5 times is **0.99**. A shooter who calls anything beyond twice the mean radius a flyer will discard a perfectly ordinary shot **two times in three**.

**What the screen judges a worst shot by** (NOTES-FROM-PLANNING.md entry 104 section 2). The flyer card measures the worst shot from the group's own centre in the group's own mean radius, the Rayleigh estimate `√(π/2) · σ̂` with σ̂ from section 3.3, fitted to the same shots. The worst shot inflates the mean radius it is divided by, so this ratio is compressed: about its own centre the worst of `n` shots can be at most `√((n−1)/n)` of the root sum of squared radii, which is about 1.96 group mean radii at five shots. The closed form above, fed this ratio, is conservative, and badly so at small `n`. Simulated, 200,000 circular groups a count (seed 7):

| n | closed form's 5 percent line | the screen's statistic, true 5 percent line | closed-form p at the true line | mean of the screen's statistic |
|---|---|---|---|---|
| 5 | 2.416 | **1.733** | 0.391 | 1.440 |
| 10 | 2.592 | 2.204 | 0.199 | 1.783 |
| 15 | 2.689 | 2.409 | 0.146 | 1.947 |
| 25 | 2.807 | 2.623 | 0.107 | 2.130 |

**At five shots the closed form's line is past anything a group can produce**, so a card built on it could never flag a shot. The screen therefore does not use the closed form. `Flyers.CalibrateWorst` simulates 9,999 circular groups of the same size, seeded, measures each the way the screen does, and reports the share at least as extreme and the mean; the card reads those. Entry 104's own measurement divided by the arithmetic mean of the radii, a third statistic, whose lines are 2.108, 2.477, 2.621 and 2.774 at the same counts; the screen does not quote that one.

**The table above still stands for what it says**, and so does the dialog text built on it: a shooter who calls anything past twice the true mean radius a flyer discards an ordinary shot two times in three at 25 shots. The calibration sharpens where the card's own line sits; it does not make wide shots rarer.

Per DESIGN.md, exclusion remains permitted, since the legitimate case exists: a called flyer, an obviously bad round, a shot the shooter knows they pulled. It is gated by this statement, it requires a reason from a short list, the reason is recorded, and every report prints the full and reduced figures side by side so an exclusion can never be hidden.

**One addition worth making.** When a user excludes a shot, show the effect immediately and in both directions: the group statistics with and without, and the change in the confidence interval. Excluding the worst shot from a 25-shot group narrows the point estimate and biases it downward, and seeing the interval fail to narrow correspondingly is instructive.

---

## 11. Pooled and virtual groups

Two different questions live here, they give different answers, and the software must not silently pick one.

**Question A, how does this rifle and load disperse?** Pool after re-centring each target on its own centre. This removes zero drift, sight adjustment and load-to-load point-of-impact shift, and measures dispersion alone.

```
rSqSum_pooled = Σ over targets t of Σ over shots in t of ‖(xᵢ,yᵢ) − c_t‖²
df            = 2 · (N − k)     for k targets, N total shots
varHat        = rSqSum_pooled / (2 (N − k))
corrFac       = 1 / c4(2N − 2k + 1)
```

**Each re-centring costs two degrees of freedom.** Eight 25-shot targets pooled this way give `2 × (200 − 8) = 384` degrees of freedom, not 398. Small, and wrong is wrong.

**Question B, where does this rifle and load put shots, all in?** Pool raw offsets from the point of aim without re-centring. This includes zero drift between sessions, and it is the right question for a hunter or a competitor, because the rifle's behaviour across sessions is what they experience.

Report both, label them clearly, and default to A for load development and B for anything expressed as hit probability.

**Guard against pooling things that should not be pooled.** Different barrels, different lots, an intervening scope adjustment, or a large temperature difference all break the assumption that these are draws from one distribution. GroupLab knows most of this from its own records, so it can check and warn. A cheap and effective test: run the section 8.1 F test **between** the targets being pooled, and if they differ significantly, say so before pooling rather than after.

---

## 12. The solver-coupled analyses

These are the four places where the ballistic solver of DESIGN.md section 16 enters the statistics layer.

### 12.1 Velocity regressed against vertical dispersion

Ordinary least squares of vertical position on muzzle velocity,

```
yᵢ = a + b·vᵢ + εᵢ
```

reported with the slope, its confidence interval, `R²`, and the residual standard deviation. `R²` is the interesting output: it is the fraction of vertical dispersion attributable to velocity variation.

**The solver supplies an independent prediction of the slope**, `dy/dv` at the shot distance under the recorded conditions. Comparing the fitted slope against the predicted slope is a check on both. A fitted slope far from the prediction means either the chronograph, the shot-to-velocity mapping, or the solver inputs are wrong, and the software should say which it suspects.

**This depends entirely on the reconciliation of DESIGN.md section 15 being correct.** If shot `i` is paired with the wrong velocity, this regression produces a confident, plausible, wrong slope. The regression must refuse to run on an unreconciled or low-confidence mapping, and the analysis record must carry the mapping's provenance.

### 12.2 Predicted versus measured vertical

The analysis DESIGN.md section 14 says is argued constantly and almost never answered numerically.

```
σ_y,predicted = |dy/dv| · SD(v)                     from the solver and the chronograph
σ_y,measured                                        from the shots
σ_y,other     = √( max(0, σ²_y,measured − σ²_y,predicted) )
```

Report all three, with intervals on the measured and residual components. The interpretation is the payoff:

- **Measured much greater than predicted:** the limit is the rifle or the shooter. Tightening velocity extreme spread will not help, and the software should say so plainly.
- **They agree:** the ammunition is the limit, and velocity consistency is where the gains are.

**Two honesty requirements.** The subtraction of variances can go negative through sampling error alone, and clamping it to zero silently hides that. Report the raw difference and the clamped estimate, and where the interval on `σ_y,other` includes zero, say that the data cannot distinguish the two explanations. And `SD(v)` from a typical string of 10 to 25 rounds carries the same wide interval as any other small-sample standard deviation, roughly plus or minus 15 percent at `n = 25`, which propagates straight into `σ_y,predicted`. Propagate it rather than treating the chronograph number as exact.

### 12.3 Distance normalisation

Angular conversion, `MOA` and `mil`, handles the geometry. It does not handle the physics: dispersion does not scale linearly with distance, because wind, aerodynamic jump and the velocity-to-vertical mapping all grow non-linearly, and transonic transition can add dispersion abruptly.

So: convert linearly for the angular columns, which is honest and is what the units mean, and use the **solver** for any statement of the form "this group at 100 yards implies this at 600 yards". Label the second as a prediction with its assumptions listed, never as a measurement. Warn on transonic range.

### 12.4 Hit probability at distance

Per DESIGN.md section 14, through the solver rather than by scaling the group. The dispersion at the target distance is the measured dispersion propagated by the solver, plus the wind and velocity contributions at that distance, and the hit probability follows from section 4 applied to that propagated dispersion.

### 12.5 Angular conversion constants

shotGroups uses the **half-angle form** throughout, `angle = k · atan(x / (2·dst))`, not the small-angle approximation. GroupLab should match, because the difference is real at short distances and matching removes an entire class of validation mismatch.

| Unit | Constant | Value |
|---|---|---|
| degrees | 360/π | 114.59155902616464175 |
| radians | 2 | |
| MOA | 21600/π | 6875.4935415698785052 |
| SMOA (IPHY) | 1/atan(1/7200) | 7200.0000462962960581 |
| mrad | 2000 | |
| mil (NATO, 6400) | 6400/π | 2037.1832715762602978 |

`MOA2SMOA = 0.95492965241113508368`.

DESIGN.md section 14 makes true MOA the default at 1.047 inches per 100 yards, with IPHY available. Both are here, and the sanity anchor is that 1 inch at 100 yards is exactly 1.000000 SMOA and 0.954930 MOA, which is confirmed by execution.


### 12.6 Hit probability by simulation

NOTES-FROM-PLANNING.md entry 156, built in `HitProbability`. Section 12.4's figure carries the group's sigma to a distance and integrates it over the target, which is right for a single shot with no error but the rifle's. The Ballistics screen's hit probability goes further, by simulation, because the errors that decide a hit at distance are not all of the same kind.

**Per shot and per string.** The dispersion, with the precision's sigma on each axis, and the muzzle velocity's spread through the drop are drawn for every shot. The wind call, the range estimate, the zero, the drag, the air, the shot's inclination and the Earth's rotation are drawn once for a string and shared by every shot in it, because the shooter reads them once and fires. Drawing a wind call per shot would make it average out across a string like dispersion does, and flatter every figure about more than one shot.

**Carried through the solver.** Each source's effect on the impact is the trajectory with that input moved, less the believed one, with the elevation and wind dialed from the belief and the sight's zero angle held. It is fitted by a quadratic through two solves either side of the belief, at the bias plus two standard deviations. The sources are added, which leaves out how they interact; that is second order at the sizes below.

**Rifle precision** is the per-axis standard deviation of the shots about their own center, as an angle: for circular dispersion, sigma. A radial figure such as the mean radius is about 1.25 times it and is never put in its place. It comes from the group open in the analysis, from a load's sessions pooled re-centered as section 11 does, or typed. The group already holds the velocity's share of its vertical at the distance it was shot, so that share is taken out in quadrature before the velocity is drawn per shot, as section 12.2 does.

**Sigma is drawn too.** For every string, sigma is drawn from its sampling distribution, sigma times sqrt(df / chi-squared(df)), at stratified quantiles. The interval on every probability is widened by the answer worked out again at both ends of sigma's 95 percent interval, on the same random numbers, and by 1.96 standard errors of the simulation, and the screen says which of the two is the larger. A typed precision has no known uncertainty and the screen says that too.

**The second round** is fired after the first impact is seen and its whole miss dialed off. That removes every per-string error but adds the first shot's own dispersion and velocity, so the second shot lands at its per-shot error less the first's. A second-round figure that forgets the subtraction is too high.

**What a person is shown.** Never a point without its interval; two significant figures at most, and no digit finer than the trial count supports. The cost of each source is the first-round probability it takes away, found by working the answer out without it on the same random numbers.

**When it refuses.** When sigma's interval alone moves the first-round answer across more than half the scale, the answer says nothing useful, and the screen says so with the number of shots in one group that would narrow it to half the scale, found from section 9.1's interval multiples.

**The confidence presets.** One choice sets every uncertainty a shooter cannot measure. They are GroupLab's own judgment of each situation, written down here so they can be argued with; none is taken from another calculator. Each figure is one standard deviation.

| source | Known distance, measured air | Lasered distance, estimated wind | Estimated distance, estimated wind |
|---|---|---|---|
| range | 0.5 yd | 1.5 yd | 5 percent of the distance |
| wind, full value | 1.5 mph | 3 mph | 4 mph |
| drag | 1 percent | 2 percent | 3 percent |
| temperature | 2 °F | 5 °F | 10 °F |
| station pressure | 0.03 inHg | 0.1 inHg | 0.3 inHg |
| humidity | 5 percent | 15 percent | 25 percent |
| inclination | 0.5 degrees | 1 degree | 2 degrees |
| azimuth | 5 degrees | 10 degrees | 15 degrees |
| latitude | 0.5 degrees | 1 degree | 2 degrees |

- **Range.** A marked range leaves only where the frame stands against its marker. A rangefinder reads to the yard, and its beam can return from the ground or a berm near the target. A distance judged by eye is wrong in proportion to the distance, so it is a share of it.
- **Wind.** Read from flags and mirage at a familiar range is better than estimated from what can be seen downrange, which is better than estimated with nothing to read.
- **Drag.** A drag confirmed against the shooter's own drops, a published figure for the bullet, and a published figure used for another lot or another rifle.
- **The air.** A weather meter at the line, a phone or a forecast, and a guess from the season.
- **Angles and latitude.** Measured with an angle indicator and a compass, read from a map, and estimated.

The muzzle velocity's spread, the precision and the zero are never set by a preset: they come from what GroupLab measured. A bias, the true value less the believed one, is never set by a preset either, because it is something a person knows about their own equipment.
---

## 12a. Did the group open up as it was shot?

A barrel warming, a shooter tiring, a rest settling: all things people believe they can see in a group, and a group of ten shots fired in a random order will look like one of them often enough to convince somebody. The question is worth answering because the answer is usually no.

**The statistic** is Spearman's rank correlation between the order a shot was fired in and its distance from the group's centre. Ranks rather than the radii themselves for two reasons: the question is whether later shots sit further out, not whether they sit further out in proportion to anything; and one wild shot should not be allowed to decide it.

**The p-value is a permutation test**, two sided. Under the hypothesis that order carries nothing, every ordering of the same shots is equally likely, so the null distribution is built from the shots themselves by shuffling: no distributional assumption at all, which is what matters at the sample sizes people actually shoot. It is two sided because a shooter looking for a barrel warming will find one at half the price otherwise, and a shooter looking for settling in will find the opposite.

**Below five shots it answers nothing**, because every ordering is then a large share of the ones there are and no p-value can be small.

**Calibration.** `ShotOrderTrendTests` fires 400 simulated groups of ten Rayleigh radii in random order and requires the share called a trend at the 5 percent level to stay under 11 percent. That is the test that decides whether this is worth showing anybody: what matters is not that a real trend is found, but how often one is announced when there is none.

**Where the order comes from.** Only from a chronograph string mapped to the shots. A sheet on its own does not record what order it was shot in, and nothing here invents one.

## 12b. Is this load getting better or worse?

The same question as section 12a, asked of sessions instead of shots: do later sessions of one load measure larger than earlier ones, more than a shuffle of the same sessions would.

**It is the same statistic and the same test**, Spearman's rank correlation with a two-sided permutation p-value, and the same floor of five values below which nothing is claimed. Nothing about the arithmetic changes when the ordered thing is a session rather than a shot, so it is not implemented twice.

**No trend line is drawn.** Four sessions plotted against the date climb or fall; they always do, and a shooter reads a barrel wearing or a batch of powder going off into that line. The sessions are dots with their intervals and the verdict is in words.

**Each session carries its own interval**, because two sessions of five shots are far weaker evidence than two of thirty and nothing else in the picture would show that. Where every interval covers every other, that is said outright.

## 12c. A velocity SD without its own uncertainty is misleading

Every chronograph prints an SD and an extreme spread, and both mislead on their own.

**The SD.** A sample SD of 10 ft/s over ten shots does not mean the rifle holds 10 ft/s. The interval is chi-squared on n minus 1 degrees of freedom:

    lower = s * sqrt((n - 1) / chi2(1 - a/2, n - 1))
    upper = s * sqrt((n - 1) / chi2(a/2, n - 1))

At ten shots and 95 percent that is **6.9 to 18.3 ft/s** for a measured 10. At thirty shots, which almost nobody fires for this, it is still 8.0 to 13.4. The width is the point, and it is why a load worked up on velocity SD from ten-shot strings is being chosen on noise.

**Calibration.** `VelocitySdIntervalTests` draws 2000 strings of ten shots from a rifle whose velocity SD is truly 12 ft/s and requires the share of intervals covering 12 to fall between 93 and 97 percent.

**The extreme spread** grows with the number of shots on its own, for the same reason as a group's extreme spread (section 4): more shots means more chances at both tails. So it is reported with the count it came from and described as comparable only with another string of the same length.

## 13. Units

Everything is stored canonically as linear distance at the target plane, per DESIGN.md section 14.

Output supports inches, centimetres, MOA and mil as independently toggleable columns displayed simultaneously. Angular columns require a known distance and are **not offered** when distance is unset. Not greyed out, not shown as zero: absent, with the reason available.

`getDistance` inverts the conversion, which is what "what distance would make this group 1 MOA" needs.

---

## 14. What GroupLab adds beyond shotGroups

shotGroups is substantially the statistics layer GroupLab needs, and DESIGN.md section 4 is right that porting it with attribution and validating against it is the correct approach. Six things it does not do, which are GroupLab's actual contribution.

1. **Sample size planning.** shotGroups has `efficiency()` for CI width on range statistics. It has nothing for "how many shots to distinguish these two loads", which is section 9.2 and is the question users actually have.
2. **The flyer gate.** Nothing in shotGroups tells a user what the worst shot is expected to be before they discard it.
3. **Multiple-comparison correction** across a family of load comparisons. Section 8.4.
4. **Solver coupling.** Predicted versus measured vertical, velocity regression against a predicted slope, hit probability propagated to distance. Sections 12.1 through 12.4. Nothing analogous exists.
5. **Separating circularity from vertical stringing.** shotGroups reports an aspect ratio and runs normality tests; it has no direct test for either hypothesis in section 7.
6. **Confidence intervals on everything, presented by default.** shotGroups computes many of them; making them non-optional and putting them next to every headline figure is a product decision, and it is the project's whole premise.

Item 4 is the one that could not be built any other way, and it is the strongest reason for the project to exist.

---

## 15. Validation plan against shotGroups

DESIGN.md section 21 sets the Phase 2 gate as statistical output matching shotGroups within numerical tolerance on shared test data. Here is how.

### 15.1 The reference harness

R 4.3.3 with shotGroups 0.8.4 was installed and run during the writing of this document, so this is a description of something that works rather than a proposal.

A driver script dumps every numeric output to a tidy CSV and matching JSON, keyed `<function>.<component>.<row>.<col>`, which is order-independent and diffable. Runs on `DF300BLK`, `DFscar17` and `DFcciHV` produced 454, 444 and 474 rows respectively.

**Where the reference lives.** shotGroups is **GPL >= 2**, and GroupLab is GPL-3.0, so linking is not the issue. But GroupLab must not ship shotGroups' data or its Monte Carlo tables as its own. The arrangement:

- The R script and the generated fixture JSON live in the repository under `test/fixtures/shotgroups/`, with a README stating the provenance, the package version, the R version and the licence.
- The fixtures are **generated output**, checked in for reproducibility so that contributors need no R installation to run the test suite.
- Regenerating them is a documented, occasional maintenance task, pinned to a stated shotGroups version.
- Attribution to Wollschlaeger appears in the repository, per DESIGN.md section 4.

**Amended 2026-09-14, by `docs/NOTES-FROM-PLANNING.md` entry 18 and `test/fixtures/shotgroups/README.md`.** The generated fixtures are in `test/fixtures/shotgroups/`, as the first point says, but the R scripts that write them, `sg_dump.R` and `sg_distr.R`, are in `tools/shotgroups/`, where the dump already was. The first point is left as written, as a record of the plan.

### 15.2 Fixtures

| Dataset | Shots | Groups | Distance | Why |
|---|---|---|---|---|
| `DF300BLK` | 20 | 1 | 100 yd, inches | The canonical single-group case. Start here |
| `DFscar17` | 10 | 1 | 100 yd, inches | Small `n`. Exercises `c4` at low degrees of freedom, where the correction is largest |
| `DFcciHV` | 40 | 2 | 100 yd, inches | Two groups. The Ansari-Bradley and Wilcoxon branch of `compareGroups` |
| `DF300BLKhl` | 60 | 3 | 100 yd, inches | Three groups. The Fligner-Killeen and Kruskal-Wallis branch |
| `DFcm` and `DFinch` | 487 each | 3 | 25 m / 27.34 yd | The unit-conversion regression test. **Amended 2026-09-15, `NOTES-FROM-PLANNING.md` entry 23 section 1:** they are the same shots grouped differently, not the same data. Every `DFcm` shot is a `DFinch` shot times 2.54, but shot 242 is in series 5 of `DFcm` and series 4 of `DFinch`, so those two series hold different shots. The conversion test is met on the shots and cannot be met on the series as shipped |
| `DFsavage` | 180 | 9 series | 100, 200, 300 m | **Multiple distances in one frame.** Angular columns must drop out. A negative test |
| `DFlandy04` | 175 | 6 | 50 yd | Unequal group sizes, 5 x 25 plus 1 x 50. **Amended 2026-09-14, entry 18 section 2:** also the fixture that exercises the multi-group range path, `nGroups` = 6 in `range2sigma`, `range2CEP` and `getRangeStatEff`, emitted under `multiGroup.*` |
| `DFlandy01` | 530 | 53 | 50 m | Large. Range statistics with many groups. **Amended 2026-09-14, entry 18 section 2:** it cannot reach that purpose. `getRangeStat` has no group argument and pools all 530 shots, and the multi-group range tables stop at 10 groups, so the fixture carries `multiGroup.beyondTable`. It remains the large per-series battery |

Plus GroupLab's own fixtures, which shotGroups cannot provide: synthetic groups drawn from a known `Σ` so that the estimator can be checked against **truth** rather than against another implementation, and the four solver-coupled analyses in section 12.

### 15.3 Tolerances

| Class | Tolerance | Rationale |
|---|---|---|
| Closed-form scalars: sigma, its CI, MR, MEDR, RSD, circular CEP, hit probability | **1e-12 relative** | These matched at 0.000e+00 during drafting. Anything worse is a bug, not a tolerance |
| Angular conversions | **1e-12 relative** | Pure arithmetic with published constants |
| Hoyt-based `CorrNormal` CEP | **1e-8 relative** | Numerical quadrature. Implementations may differ in the last digits |
| Minimum enclosing circle, minimum-area bounding box | **1e-9 absolute** on radius and dimensions | Geometric constructions, exact up to floating point, but different hull orderings can shift the last digits |
| Minimum-volume enclosing ellipse | **1e-4 relative** | Iterative with a default tolerance of 1e-3. Match the tolerance or expect disagreement |
| Range statistics from lookup | **2e-3 relative** | GroupLab generates its own Monte Carlo tables. See below |
| Bootstrap intervals | **Not compared numerically.** Compare coverage over 1000 simulated datasets | Stochastic. Bitwise agreement is meaningless and a coverage test is the real question |

**The Monte Carlo tables need their own gate.** GroupLab must generate its own rather than ship `DFdistr`. Acceptance: at least 10 million replications per `(n, nGroups)` cell, matching shotGroups' own table to **within 0.2 percent on the mean and 0.5 percent on the 2.5 and 97.5 percent quantiles**, for `n` from 2 to 50 and `nGroups` from 1 to 10. Generating this is hours of compute and belongs in a separate tool, run once, with the output checked in.

### 15.4 Known differences to encode as expected, not as failures

Discovered by reading shotGroups 0.8.4's source and worth writing into the comparison harness so nobody chases them.

1. **`getRayParam` returns `MEDRciUP` with a capital P**, where its siblings are `sigCIup`, `RSDciUp`, `MRciUp`. A field-name mapping will miss it.
2. **`groupSpread` names the minimum-ellipse columns `semi_major` and `semi_minor` with underscores, but the confidence-ellipse columns `semi-major` and `semi-minor` with hyphens.**
3. **CI column names embed a literal space and parenthesis**: `"sigma ("`, `"MR )"`, `"sdX ("`. Stable, but they will break a naive parser.
4. **`getCEP`'s output column order differs from its argument's `choices` order.** Match by name, never by position.
5. **`GrubbsLiu` is numerically identical to `GrubbsPearson`** when `accuracy=FALSE`, agreeing to 14 digits. Not a bug; do not treat the coincidence as a validation success.
6. **`compareGroups` gives different p-values depending on whether `coin` is installed**, switching between exact permutation and asymptotic tests. Record which branch generated every fixture.
7. **`analyzeGroup` has no `plots` argument** and always plots. Headless runs need `pdf(NULL)`.
8. **In `DFsavage` and `DFtalon` the `group` column has one level while `series` has nine.** `compareGroups` and `combineData` key on `series`. A fixture loader that uses `group` gets nonsense.
9. **The comment block in `compareGroups.R` lines 280 to 283 has the two-group and multi-group test labels inverted** relative to the code. The code is right. Do not port the comment.

**Items 10 to 12 added 2026-09-15, `NOTES-FROM-PLANNING.md` entry 23 section 1, found by the M3 harness (`docs/QUESTIONS-FOR-PLANNING.md` question 11).**

10. **The fixture's MANOVA row is R's intercept row.** `sg_dump.R` takes `MANOVA[1, ]`, which in `anova.mlm` tests whether the mean over all shots is the origin, not section 8.2's test of the group centres; row 2 would be the group test. The harness reproduces row 1 and GroupLab computes the group test separately. The next reader of the fixture will assume row 1 is the group test.
11. **`fromMOA` in SMOA is not the exact inverse of `getMOA`.** In every scope of every fixture it is the inverse times 1 + 6.21288e-10, so section 12.5's anchor holds for `getMOA` and the round trip misses section 15.3's 1e-12. The harness encodes the constant rather than reproducing it in GroupLab.
12. **shotGroups' CorrNormal CEP is not a root of its own distribution.** Its hit probabilities match GroupLab's Hoyt CDF to 1e-15, but under that CDF its CEPs miss their probability: on `DF300BLK` by +3.7e-6, +2.9e-6 and -5.8e-7 at 0.50, 0.90 and 0.95, and by 3e-6 to 2.3e-5 relative across 899 keys, which is a root finder with a coarse tolerance. For this key, section 15.3's 1e-8 is replaced by three checks. The distribution is gated through the hit probabilities at 1e-8. GroupLab's CEP must be a root of it to 1e-12. shotGroups' CEP is compared at 1e-4 relative.

**Items 13 and 14 added 2026-09-15, `NOTES-FROM-PLANNING.md` entry 28 sections 2 and 5.**

13. **`getRangeStat`'s interval does not cover the expected range statistic at the level it states.** It scales the observed statistic by the table's 2.5 and 97.5 percent quantiles over its mean. That covers the expected extreme spread less often than 95 percent, most at the smallest groups. GroupLab reports the interval formed as the observed statistic times the mean over each quantile, `RangeStatistics.MeanInterval`, which does cover it. The harness still compares shotGroups' form against shotGroups. Coverage of the expected extreme spread over 40,000 circular normal groups at each n, `SmallGroupCoverageTests`:

    | n | shotGroups' `getRangeStat` form | GroupLab's form |
    |---|---|---|
    | 2 | 84.66 % | 95.10 % |
    | 3 | 89.39 % | 94.96 % |
    | 5 | 92.31 % | 95.17 % |
    | 10 | 93.91 % | 94.93 % |
    | 20 | 94.68 % | 94.94 % |

14. **`compareGroups` pairs coordinates with the wrong series labels when a frame's rows are not in series order.** It builds the coordinate columns with `split()` then `rbind()`, which returns rows in factor level order, and attaches them with `cbind()` to a frame still in its original order.
    - **In the fixtures:** in `DFlandy01`, 519 of 530 rows carry a coordinate from another row.
    - **Why no fixture value changes:** every series there is a contiguous block of ten shots. The misalignment only relabels whole groups of equal size, which leaves the Fligner-Killeen, Kruskal-Wallis and MANOVA statistics unchanged. The per-series outputs are computed from the correctly named list, not the pasted columns.
    - **What GroupLab does:** it pairs every coordinate with its own label. The fixtures happen not to distinguish the two, and a frame with interleaved series would.
    - **Where it is recorded:** `flignerProbe.rowsSortedBySeries` records it per dataset. It is 0 only for `DFlandy01`.
    - **The order `rbind` uses:** the dataset's own factor levels. In `DFlandy01` that is the labels' leading number, 1 to 53, where the fixture's `seriesLabels` list them as strings, "10_..." before "1_...".
    - **What the harness checks:** it pastes the coordinates that way for the probe keys, and every `flignerProbe` key of `DFlandy01` then matches. The same pasting shows the statistics unchanged, because the relabelled groups all hold ten shots.

**Item 15 added 2026-09-15, found answering `docs/QUESTIONS-FOR-PLANNING.md` question 14, and amended the same day when the defect was fixed, `NOTES-FROM-PLANNING.md` entry 36.**

15. **Until 2026-09-15 the fixtures did not carry R's doubles exactly, and on a frame with a point of aim that moved a rank statistic.** This was a property of the fixtures, not a difference between implementations (`NOTES-FROM-PLANNING.md` entry 30 section 4). The values were stored at 15 significant digits. That is enough for every comparison at 1e-12 and not enough to reproduce a rank-based statistic with near-ties.
    - **The cause.** `sg_dump.R` wrote its JSON with `jsonlite::toJSON(digits = 15)` and its CSV with `write.csv`'s 15 significant digits. Neither round-trips every double: 5,957 keys of `DFinch` differed between its two files.
    - **Which values it spared and which it did not.** Shot coordinates with three decimals survived. The point-of-aim-relative coordinates did not, because R computes them as `point.x - aim.x` and the subtraction's noise sits in exactly the bits that were lost.
    - **Why it mattered for one statistic.** Closed forms were unaffected at 1e-12. The Fligner-Killeen statistic was affected, because it ranks absolute deviations from the group median, and a last-bit difference makes or breaks a tie. Read back from the JSON, `DFinch`'s x statistic came out 10.073187875409229 against R's 10.075167218103388.
    - **What the harness did meanwhile.** The aim is a short decimal, so the harness recovered each aim from `shots.x` and `shots.xPOA` to six decimals and redid the subtraction. With that, all four Fligner-Killeen statistics matched shotGroups to 5.5e-13 relative or better.
    - **The fix, entry 36.** Both scripts now write 17 significant digits, the round-trip precision of a double: `sprintf("%.17g")` in the CSV, and `digits = I(17)` in the JSON. The `digits = NA` this item once named as the fix still emits 15 digits on jsonlite 2.0.0.
    - **Checked on the nine regenerated datasets:**
      - no key added or removed;
      - no stored number moved by more than 5.6e-16 relative;
      - CSV and JSON agree bit for bit on 71,056 numeric values. Three negative zeros in `DFlandy01`, written `-0` in the CSV and `0` in the JSON, are now written `0` by both scripts (entry 38).
    - **The reconstruction is removed.** The harness reads `shots.xPOA` directly, and the four Fligner-Killeen keys pass from the fixture alone. Against the true stored values, the reconstruction was exact on 3,775 of 3,978 coordinates. The other 203 are all `DFcm`, off by at most 3.6e-15, because its aims in centimetres are not six-decimal numbers.
    - **Its only known casualty** was question 14's four Fligner-Killeen keys, and the day spent on them.
    - **`shotGroups_DFdistr`, entry 38.** Its first regenerated JSON stored every table value as a string, because `sg_distr.R` formatted the columns as text for the CSV before building the JSON from the same frame. The script now keeps a numeric copy for the JSON. Regenerated:
      - its 9,440 table values are 2,360 integers and 7,080 doubles;
      - they agree with the CSV bit for bit;
      - none moved from the 15-digit version by more than 4.4e-16 relative.

### 15.5 Phase 2 gate

1. Every closed-form quantity in section 15.3 matches within tolerance on all eight fixtures.
2. Both unit systems agree: `DFcm` and `DFinch` produce identical results after conversion.
3. Multiple-distance data correctly suppresses angular output rather than producing a wrong number.
4. Known-truth synthetic tests recover `σ` with the correct bias and the stated coverage: over 10,000 simulated 25-shot groups, the 95 percent interval covers the true `σ` between 94.0 and 96.0 percent of the time.
5. The Monte Carlo tables meet section 15.3's tolerance against `DFdistr`.
6. The estimator is checked in **both** the estimated-centre and known-centre configurations. Section 3.2 is where a silent error would live.

---

## 16. Open questions

1. **Should the `c4` bias correction apply to the confidence-interval endpoints?** shotGroups does it, GroupLab matches for validation, but it means the interval no longer has exactly nominal coverage for `σ`. Match for compatibility, or diverge and document? My inclination is to match, and to add a note in the reference documentation, because divergence would make every validation comparison need a special case.

2. **Which is the headline dispersion figure in the primary panel?** Sigma is the right estimator and means nothing to the audience. Mean radius is a fixed multiple of it and is intuitive. CEP is meaningful to some and unfamiliar to most. My suggestion is mean radius with its interval as the headline, sigma immediately beneath as the underlying estimate, and extreme spread present but visually subordinate. This is a product decision, not a statistical one.

3. **Should pooling default to re-centred or raw?** Section 11 argues for re-centred in load development and raw in hit probability. That is a defensible default but it means the same button does different things in different views, which needs care in the interface.

4. **Is the ballistics.js port going to give `dy/dv` directly**, or does it need numerical differentiation of the trajectory solution? If numerical, the step size and its error need specifying, because that derivative feeds section 12.2, which is the project's most distinctive analysis.

5. **Do you want the Bayesian version?** For a shooter accumulating groups over a barrel's life, a prior from previous sessions with the same rifle and load is genuinely more informative than each session's independent interval, and the conjugate structure here is straightforward, since the inverse-gamma is conjugate for the Rayleigh scale. It is real work and it is arguably out of scope for version one, but it fits the project's premise better than almost anything else on the list.
