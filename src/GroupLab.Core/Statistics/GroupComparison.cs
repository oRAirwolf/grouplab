using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>A test's statistic and its p-value.</summary>
public readonly record struct TestResult(double Statistic, double PValue);

/// <summary>A one-way MANOVA row: Wilks' lambda, its F, the F's degrees of freedom, and the p-value.</summary>
public readonly record struct ManovaResult(double Wilks, double F, double Df1, double Df2, double PValue);

/// <summary>
/// Comparing groups, docs/STATISTICS.md section 8: dispersion by the exact F test with the ratio's interval (8.1), location by
/// MANOVA, which for two groups is Hotelling's two-sample test (8.2), the non-parametric backstops shotGroups' <c>compareGroups</c>
/// runs (8.3), and Holm's correction for a family of comparisons (8.4).
/// </summary>
public static class GroupComparison
{
    /// <summary>Ranks with ties given the mean of the ranks they span, 1-based, as R's <c>rank</c> does by default.</summary>
    public static double[] MidRanks(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        int n = values.Count;
        var order = Enumerable.Range(0, n).OrderBy(i => values[i]).ToArray();
        var ranks = new double[n];
        for (int i = 0; i < n;)
        {
            int j = i;
            while (j + 1 < n && values[order[j + 1]] == values[order[i]])
            {
                j++;
            }

            for (int k = i; k <= j; k++)
            {
                ranks[order[k]] = ((i + j) / 2.0) + 1;
            }

            i = j + 1;
        }

        return ranks;
    }

    /// <summary>
    /// The exact permutation test of a two-sample linear statistic, as the R package <c>coin</c> computes it with its shift
    /// algorithm, which is the branch shotGroups' <c>compareGroups</c> takes when <c>coin</c> is installed (section 15.4 point 6):
    /// the sum of the first group's scores, standardised by its permutation mean and variance, with the two-sided p-value the
    /// probability that a random split lies at least as far from that mean. Scores must be multiples of one half, which mid-ranks
    /// always are, so the distribution is counted exactly over doubled integer scores.
    /// </summary>
    public static TestResult ExactTwoSample(IReadOnlyList<double> scores, IReadOnlyList<bool> first)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(first);
        int n = scores.Count, m = first.Count(f => f);
        var doubled = new int[n];
        for (int i = 0; i < n; i++)
        {
            double twice = 2 * scores[i];
            doubled[i] = (int)Math.Round(twice);
            if (Math.Abs(twice - doubled[i]) > 1e-9 || doubled[i] < 0)
            {
                throw new ArgumentException("exact permutation scores must be non-negative multiples of one half.", nameof(scores));
            }
        }

        double mean = scores.Average(), variance = scores.Sum(a => (a - mean) * (a - mean)) / n, observed = 0;
        int observedDoubled = 0;
        for (int i = 0; i < n; i++)
        {
            if (first[i])
            {
                observed += scores[i];
                observedDoubled += doubled[i];
            }
        }

        double z = (observed - (m * mean)) / Math.Sqrt(m * (n - m) / (double)(n - 1) * variance);

        // ways[size, sum]: how many subsets of the scores seen so far have that size and doubled sum.
        int total = doubled.Sum();
        var ways = new double[m + 1, total + 1];
        ways[0, 0] = 1;
        int seen = 0;
        foreach (int a in doubled)
        {
            seen++;
            for (int size = Math.Min(m - 1, seen - 1); size >= 0; size--)
            {
                for (int sum = total - a; sum >= 0; sum--)
                {
                    if (ways[size, sum] != 0)
                    {
                        ways[size + 1, sum + a] += ways[size, sum];
                    }
                }
            }
        }

        double centre = m * (double)total / n, distance = Math.Abs(observedDoubled - centre), all = 0, extreme = 0;
        for (int sum = 0; sum <= total; sum++)
        {
            all += ways[m, sum];
            if (Math.Abs(sum - centre) >= distance * (1 - 1e-12))
            {
                extreme += ways[m, sum];
            }
        }

        return new TestResult(z, extreme / all);
    }

    /// <summary>The Ansari-Bradley test for a difference in scale, exact: scores min(r, n - r + 1) of the mid-ranks.</summary>
    public static TestResult AnsariBradley(IReadOnlyList<double> values, IReadOnlyList<bool> first)
    {
        var ranks = MidRanks(values);
        int n = values.Count;
        return ExactTwoSample([.. ranks.Select(r => Math.Min(r, n - r + 1))], first);
    }

    /// <summary>The Wilcoxon rank-sum test, exact, on mid-ranks.</summary>
    public static TestResult WilcoxonRankSum(IReadOnlyList<double> values, IReadOnlyList<bool> first) => ExactTwoSample(MidRanks(values), first);

    /// <summary>The Kruskal-Wallis test with the tie correction, its p-value from chi-square on k - 1 degrees of freedom.</summary>
    public static TestResult KruskalWallis(IReadOnlyList<double> values, IReadOnlyList<int> groups)
    {
        var ranks = MidRanks(values);
        int n = values.Count;
        var levels = groups.Distinct().ToList();
        double h = 12.0 / (n * (n + 1.0)) * levels.Sum(l =>
        {
            double sum = 0;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (groups[i] == l)
                {
                    sum += ranks[i];
                    count++;
                }
            }

            return sum * sum / count;
        }) - (3.0 * (n + 1));
        double ties = values.GroupBy(v => v).Sum(g => Math.Pow(g.Count(), 3) - g.Count());
        h /= 1 - (ties / ((double)n * n * n - n));
        return new TestResult(h, Distributions.ChiSquare(h, levels.Count - 1).Upper);
    }

    /// <summary>
    /// The Fligner-Killeen test of homogeneous scale as R's <c>fligner.test</c> computes it: values centred on their group's
    /// median, scores qnorm((1 + rank|x| / (n + 1)) / 2), and the statistic's p-value from chi-square on k - 1 degrees of freedom.
    /// </summary>
    public static TestResult FlignerKilleen(IReadOnlyList<double> values, IReadOnlyList<int> groups)
    {
        int n = values.Count;
        var levels = groups.Distinct().ToList();
        var medians = levels.ToDictionary(l => l, l =>
        {
            var v = Enumerable.Range(0, n).Where(i => groups[i] == l).Select(i => values[i]).Order().ToArray();
            return v.Length % 2 == 1 ? v[v.Length / 2] : (v[(v.Length / 2) - 1] + v[v.Length / 2]) / 2;
        });
        var ranks = MidRanks([.. Enumerable.Range(0, n).Select(i => Math.Abs(values[i] - medians[groups[i]]))]);
        var a = ranks.Select(r => Distributions.NormalQuantile((1 + (r / (n + 1))) / 2)).ToArray();
        double mean = a.Average(), variance = a.Sum(s => (s - mean) * (s - mean)) / (n - 1);
        double statistic = levels.Sum(l =>
        {
            double sum = 0;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (groups[i] == l)
                {
                    sum += a[i];
                    count++;
                }
            }

            return sum * sum / count;
        });
        statistic = (statistic - (n * mean * mean)) / variance;
        return new TestResult(statistic, Distributions.ChiSquare(statistic, levels.Count - 1).Upper);
    }

    /// <summary>The within-group sums of squares and cross-products of the shots, (xx, xy, yy), and the group means.</summary>
    private static ((double Xx, double Xy, double Yy) Within, Dictionary<int, PointD> Means) WithinGroups(IReadOnlyList<PointD> shots, IReadOnlyList<int> groups)
    {
        var means = groups.Distinct().ToDictionary(l => l, l => GroupStatistics.Centre([.. Enumerable.Range(0, shots.Count).Where(i => groups[i] == l).Select(i => shots[i])]));
        double xx = 0, xy = 0, yy = 0;
        for (int i = 0; i < shots.Count; i++)
        {
            double dx = shots[i].X - means[groups[i]].X, dy = shots[i].Y - means[groups[i]].Y;
            xx += dx * dx;
            xy += dx * dy;
            yy += dy * dy;
        }

        return ((xx, xy, yy), means);
    }

    /// <summary>
    /// The intercept row of R's multivariate <c>anova</c> for the model (x, y) ~ group: whether the mean over all shots is the
    /// origin, given the groups. It is what shotGroups' <c>compareGroups</c> fixture records as its MANOVA, the first row of the
    /// table, and it is not the group test of section 8.2, which <see cref="ManovaGroups"/> is. With one hypothesis degree of
    /// freedom Wilks' lambda is 1 / (1 + q), q = N m' E^-1 m, and F = q (dfE - 1) / 2 is computed from q directly, not from 1 - lambda.
    /// </summary>
    public static ManovaResult ManovaIntercept(IReadOnlyList<PointD> shots, IReadOnlyList<int> groups)
    {
        var ((xx, xy, yy), means) = WithinGroups(shots, groups);
        var grand = GroupStatistics.Centre(shots);
        int n = shots.Count;
        double det = (xx * yy) - (xy * xy);
        double q = n * ((yy * grand.X * grand.X) - (2 * xy * grand.X * grand.Y) + (xx * grand.Y * grand.Y)) / det;
        double dfE = n - means.Count, f = q * (dfE - 1) / 2;
        return new ManovaResult(1 / (1 + q), f, 2, dfE - 1, Distributions.F(f, 2, dfE - 1).Upper);
    }

    /// <summary>
    /// The one-way MANOVA of group centres, docs/STATISTICS.md section 8.2, by Wilks' lambda, whose F is exact for two variables:
    /// F = ((1 - sqrt lambda) / sqrt lambda) (N - k - 1) / (k - 1) on 2(k - 1) and 2(N - k - 1) degrees of freedom. For two groups
    /// it is Hotelling's two-sample T^2.
    /// </summary>
    public static ManovaResult ManovaGroups(IReadOnlyList<PointD> shots, IReadOnlyList<int> groups)
    {
        var ((ex, exy, ey), means) = WithinGroups(shots, groups);
        var grand = GroupStatistics.Centre(shots);
        double hx = 0, hxy = 0, hy = 0;
        foreach (var (level, mean) in means)
        {
            int count = groups.Count(g => g == level);
            double dx = mean.X - grand.X, dy = mean.Y - grand.Y;
            hx += count * dx * dx;
            hxy += count * dx * dy;
            hy += count * dy * dy;
        }

        int n = shots.Count, k = means.Count;
        double wilks = ((ex * ey) - (exy * exy)) / (((ex + hx) * (ey + hy)) - ((exy + hxy) * (exy + hxy)));
        double root = Math.Sqrt(wilks), df1 = 2.0 * (k - 1), df2 = 2.0 * (n - k - 1);
        double f = (1 - root) / root * (n - k - 1) / (k - 1);
        return new ManovaResult(wilks, f, df1, df2, Distributions.F(f, df1, df2).Upper);
    }

    /// <summary>
    /// The ratio of two groups' dispersions, docs/STATISTICS.md section 8.1: F = (sA / dfA) / (sB / dfB) from the sums of squared
    /// radii, so the c4 correction never enters the test, with its two-sided p-value, and the ratio of the corrected sigmas with
    /// the interval sigmaA / sigmaB divided by sqrt F(1 - alpha / 2) and by sqrt F(alpha / 2).
    /// </summary>
    public static (Estimate Ratio, double F, double PValue) DispersionRatio(RayleighEstimate a, RayleighEstimate b, double level = 0.95)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        double f = a.SumOfSquaredRadii / a.DegreesOfFreedom / (b.SumOfSquaredRadii / b.DegreesOfFreedom);
        var (lower, upper) = Distributions.F(f, a.DegreesOfFreedom, b.DegreesOfFreedom);
        double ratio = a.Sigma.Value / b.Sigma.Value, alpha = 1 - level;
        var interval = new Estimate(
            ratio,
            ratio / Math.Sqrt(Distributions.FQuantile(1 - (alpha / 2), a.DegreesOfFreedom, b.DegreesOfFreedom)),
            ratio / Math.Sqrt(Distributions.FQuantile(alpha / 2, a.DegreesOfFreedom, b.DegreesOfFreedom)));
        return (interval, f, Math.Min(1, 2 * Math.Min(lower, upper)));
    }

    /// <summary>Holm's step-down adjustment of a family of p-values, docs/STATISTICS.md section 8.4, returned in the input order.</summary>
    public static double[] Holm(IReadOnlyList<double> pValues)
    {
        ArgumentNullException.ThrowIfNull(pValues);
        int m = pValues.Count;
        var order = Enumerable.Range(0, m).OrderBy(i => pValues[i]).ToArray();
        var adjusted = new double[m];
        double running = 0;
        for (int rank = 0; rank < m; rank++)
        {
            running = Math.Max(running, Math.Min(1, (m - rank) * pValues[order[rank]]));
            adjusted[order[rank]] = running;
        }

        return adjusted;
    }
}
