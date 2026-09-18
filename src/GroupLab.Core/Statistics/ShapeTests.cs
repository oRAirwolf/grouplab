using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>
/// The circularity test of docs/STATISTICS.md section 7, question A: the likelihood-ratio statistic, its Bartlett-corrected
/// form, the p-value, and how that p-value was obtained, with the resampling count and seed when it was simulated.
/// </summary>
public sealed record CircularityTest(int Shots, double Statistic, double BartlettStatistic, double PValue, string Method, int? Resamples, ulong? Seed);

/// <summary>
/// The vertical stringing test of docs/STATISTICS.md section 7, question B: Pitman-Morgan's correlation of x + y with x - y, its
/// t on n - 2 degrees of freedom, and the one-sided p-value against the alternative that the vertical spread is the larger, with
/// the two-sided value beside it.
/// </summary>
public sealed record StringingTest(int Shots, double Correlation, double T, double DegreesOfFreedom, double PValueVertical, double PValueTwoSided);

/// <summary>
/// Shape, docs/STATISTICS.md section 7. Circularity and vertical stringing are two different hypotheses and they are two
/// different functions, labelled differently, because a group elongated diagonally is not circular and is not stringing either.
/// The error ellipse's aspect ratio and orientation, reported always and without a test, are <see cref="GroupStatistics.Shape"/>.
/// </summary>
public static class ShapeTests
{
    /// <summary>
    /// Section 7's table: shots needed for 80 percent power to detect vertical stringing at 5 percent, by the ratio of vertical to horizontal
    /// spread, found by simulation. Largest ratio first.
    /// </summary>
    public static readonly IReadOnlyList<(double Ratio, int Shots)> StringingShotsForPower = [(2.00, 19), (1.50, 50), (1.25, 155)];

    /// <summary>
    /// What a stringing test on <paramref name="shots"/> shots could have detected, from section 7's power table, NOTES-FROM-PLANNING.md
    /// entry 103 section 2: "no significant stringing detected" from a small group means very little, so a negative result is never printed
    /// without this beside it. It states the smallest stringing the count catches eight times in ten, and what the next smaller one needs.
    /// </summary>
    public static string StringingPowerSentence(int shots)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string Times(double ratio) => ratio.ToString("0.##", inv) + " times";
        string Needs((double Ratio, int Shots) p) => string.Create(inv, $"{Times(p.Ratio)} needs about {p.Shots} shots");
        var caught = StringingShotsForPower.Where(p => p.Shots <= shots).ToList();
        var missed = StringingShotsForPower.Where(p => p.Shots > shots).ToList();
        if (caught.Count == 0)
        {
            return string.Create(inv, $"From {shots} shots even stringing of {Times(missed[0].Ratio)} would be missed more than 1 time in 5: catching it 8 times in 10 {Needs(missed[0])[(Times(missed[0].Ratio).Length + 1)..]}, and {string.Join(" and ", missed.Skip(1).Select(Needs))}.");
        }

        var smallest = caught[^1];
        return missed.Count == 0
            ? string.Create(inv, $"From {shots} shots this test catches stringing of {Times(smallest.Ratio)} or more at least 8 times in 10.")
            : string.Create(inv, $"From {shots} shots this test catches stringing of {Times(smallest.Ratio)} or more at least 8 times in 10, and often misses less: {string.Join(" and ", missed.Select(Needs))}.");
    }

    /// <summary>Section 7: the Bartlett-corrected likelihood-ratio test is used from 20 shots; below that it is calibrated by simulation.</summary>
    public const int AsymptoticMinimumShots = 20;

    /// <summary>
    /// The likelihood-ratio statistic for H0: Sigma = sigma^2 I, -2 ln Lambda = -n ln(det S / (tr S / 2)^2) with S the
    /// maximum-likelihood covariance. It is invariant to location, scale and rotation, which is what lets a simulation under a
    /// standard circular normal calibrate it for any group.
    /// </summary>
    public static double CircularityStatistic(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        int n = shots.Count;
        var (xx, xy, yy) = GroupStatistics.Covariance(shots);
        double scale = (n - 1.0) / n, sxx = xx * scale, sxy = xy * scale, syy = yy * scale;
        double half = (sxx + syy) / 2;
        return -n * Math.Log(((sxx * syy) - (sxy * sxy)) / (half * half));
    }

    /// <summary>
    /// Circularity, section 7 question A. From <see cref="AsymptoticMinimumShots"/> shots the statistic is multiplied by the
    /// Bartlett factor 1 - 1/n and referred to chi-square on 2 degrees of freedom; below that the corrected test still rejects
    /// circular data about 10 percent of the time at a nominal 5 (section 7's table), so the p-value is instead the share of
    /// <paramref name="resamples"/> simulated circular groups of the same size whose statistic is at least as large.
    /// </summary>
    public static CircularityTest Circularity(IReadOnlyList<PointD> shots, int resamples = 9999, ulong seed = 20260914)
    {
        ArgumentNullException.ThrowIfNull(shots);
        int n = shots.Count;
        double statistic = CircularityStatistic(shots), bartlett = statistic * (1 - (1.0 / n));
        if (n >= AsymptoticMinimumShots)
        {
            return new CircularityTest(n, statistic, bartlett, Distributions.ChiSquare(bartlett, 2).Upper, "Bartlett-corrected likelihood ratio, chi-square on 2 degrees of freedom", null, null);
        }

        var random = new StatisticsRandom(seed);
        var simulated = new PointD[n];
        int atLeast = 0;
        for (int r = 0; r < resamples; r++)
        {
            for (int i = 0; i < n; i++)
            {
                simulated[i] = new PointD(random.NextNormal(), random.NextNormal());
            }

            if (CircularityStatistic(simulated) >= statistic)
            {
                atLeast++;
            }
        }

        return new CircularityTest(n, statistic, bartlett, (1.0 + atLeast) / (resamples + 1.0), "likelihood ratio calibrated by simulated circular groups of the same size", resamples, seed);
    }

    /// <summary>
    /// Vertical stringing, section 7 question B, by Pitman-Morgan: under H0 sigma_x = sigma_y, with the correlation left free, U = x + y
    /// and V = x - y are uncorrelated, because Cov(U, V) = Var(x) - Var(y). The test is exact at every n. Vertical stringing makes
    /// Var(y) the larger and the correlation negative, so its one-sided p-value is the lower tail of t.
    /// </summary>
    public static StringingTest VerticalStringing(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        int n = shots.Count;
        var (xx, xy, yy) = GroupStatistics.Covariance(shots);
        double covUv = xx - yy, varU = xx + (2 * xy) + yy, varV = xx - (2 * xy) + yy;
        double r = covUv / Math.Sqrt(varU * varV), df = n - 2;
        double t = r * Math.Sqrt(df) / Math.Sqrt(1 - (r * r));
        var (lower, upper) = Distributions.StudentT(t, df);
        return new StringingTest(n, r, t, df, lower, Math.Min(1, 2 * Math.Min(lower, upper)));
    }
}
