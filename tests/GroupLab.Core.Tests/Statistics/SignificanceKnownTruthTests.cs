using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// docs/PROOF-CHECKLIST.md row 6 and entry 363: significance testing stayed built, not proven, until the MANOVA row and the dispersion ratio's
/// interval had fixtures. shotGroups' fixture records only R's intercept row of the MANOVA (docs/STATISTICS.md section 15.4 point 10), and no
/// reference implementation is installed here, so these are the known-truth fixtures section 15.2 describes for what shotGroups cannot provide:
/// groups drawn from a known distribution, so each test is checked against the truth rather than against another program. A person relies on
/// two things: that "p below 0.05" happens one time in twenty when the loads are alike, and that "95 percent interval" covers the true ratio
/// 95 times in a hundred. Both are measured here, and the two-group MANOVA is held to Hotelling's two-sample test written out independently.
/// </summary>
public class SignificanceKnownTruthTests(ITestOutputHelper output)
{
    private static List<PointD> Group(StatisticsRandom random, int n, double sigma, double cx = 0, double cy = 0) =>
        [.. Enumerable.Range(0, n).Select(_ => new PointD(cx + (sigma * random.NextNormal()), cy + (sigma * random.NextNormal())))];

    /// <summary>
    /// Section 8.2: for two groups the MANOVA of centres is Hotelling's two-sample T². Written out here from its textbook form, pooled
    /// covariance and all, on unequal groups with different centres: Wilks' lambda is 1 / (1 + T² / (N - 2)), and the p-value is that of
    /// T² (N - 3) / (2 (N - 2)) on 2 and N - 3 degrees of freedom.
    /// </summary>
    [Fact]
    public void TwoGroupManovaIsHotellingsTwoSampleTest()
    {
        var random = new StatisticsRandom(3631);
        foreach (var (n1, n2, shift) in new[] { (5, 5, 0.0), (10, 7, 0.4), (25, 25, 0.15), (5, 25, 1.0) })
        {
            var a = Group(random, n1, 0.3);
            var b = Group(random, n2, 0.3, shift, -shift / 2);
            var shots = a.Concat(b).ToList();
            var groups = Enumerable.Repeat(0, n1).Concat(Enumerable.Repeat(1, n2)).ToList();

            static (double X, double Y) Mean(IReadOnlyList<PointD> g) => (g.Average(p => p.X), g.Average(p => p.Y));
            var (ax, ay) = Mean(a);
            var (bx, by) = Mean(b);
            double sxx = 0, sxy = 0, syy = 0;
            foreach (var (g, mx, my) in new[] { (a, ax, ay), (b, bx, by) })
            {
                foreach (var shot in g)
                {
                    sxx += (shot.X - mx) * (shot.X - mx);
                    sxy += (shot.X - mx) * (shot.Y - my);
                    syy += (shot.Y - my) * (shot.Y - my);
                }
            }

            int n = n1 + n2;
            sxx /= n - 2;
            sxy /= n - 2;
            syy /= n - 2;
            double dx = ax - bx, dy = ay - by, det = (sxx * syy) - (sxy * sxy);
            double t2 = (double)n1 * n2 / n * ((syy * dx * dx) - (2 * sxy * dx * dy) + (sxx * dy * dy)) / det;
            double f = t2 * (n - 3) / (2.0 * (n - 2));
            double p = Distributions.F(f, 2, n - 3).Upper;

            var manova = GroupComparison.ManovaGroups(shots, groups);
            Assert.Equal(1 / (1 + (t2 / (n - 2))), manova.Wilks, 1e-12);
            Assert.True(Math.Abs(manova.PValue - p) <= 1e-9 * Math.Max(p, 1e-12) + 1e-14, $"{n1} and {n2} shots: MANOVA p {manova.PValue:R}, Hotelling p {p:R}");
        }
    }

    /// <summary>
    /// The MANOVA of three groups with the same centre: over 20,000 sheets its p-value falls below 0.05 between 4.5 and 5.5 percent of the
    /// time, and below 0.01 between 0.7 and 1.3 percent, which is what a person reading "p = 0.03" takes it to mean. Two and four groups are
    /// held the same way, and a real difference of centres is found most of the time, so the test is not merely quiet.
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(3, 5)]
    [InlineData(4, 10)]
    public void TheManovaRejectsAlikeCentresAsOftenAsItsLevelSays(int k, int perGroup)
    {
        const int sheets = 20000;
        var random = new StatisticsRandom((ulong)(36300 + (10 * k) + perGroup));
        int below5 = 0, below1 = 0, found = 0;
        var groups = Enumerable.Range(0, k).SelectMany(g => Enumerable.Repeat(g, perGroup)).ToList();
        for (int s = 0; s < sheets; s++)
        {
            var alike = Enumerable.Range(0, k).SelectMany(_ => Group(random, perGroup, 0.25)).ToList();
            double p = GroupComparison.ManovaGroups(alike, groups).PValue;
            below5 += p < 0.05 ? 1 : 0;
            below1 += p < 0.01 ? 1 : 0;
            if (s < 2000)
            {
                // The first group's centre moved by two sigmas: a difference a person would want found.
                var apart = Enumerable.Range(0, k).SelectMany(g => Group(random, perGroup, 0.25, g == 0 ? 0.5 : 0)).ToList();
                found += GroupComparison.ManovaGroups(apart, groups).PValue < 0.05 ? 1 : 0;
            }
        }

        double size5 = (double)below5 / sheets, size1 = (double)below1 / sheets, power = found / 2000.0;
        output.WriteLine($"{k} groups of {perGroup}: p < 0.05 in {100 * size5:0.00}%, p < 0.01 in {100 * size1:0.00}%, a two-sigma shift found in {100 * power:0.0}%");

        // The standard error of a proportion near 0.05 over 20,000 sheets is about 0.0015, and near 0.01 about 0.0007.
        Assert.InRange(size5, 0.045, 0.055);
        Assert.InRange(size1, 0.007, 0.013);
        Assert.True(power > 0.5, $"a two-sigma shift of one centre was found in only {100 * power:0.0}% of sheets");
    }

    /// <summary>
    /// Section 8.1: the dispersion ratio's 95 percent interval, over 20,000 pairs of groups whose true sigmas are known. At equal sizes the c4
    /// correction cancels and it covers the true ratio between 94.5 and 95.5 percent of the time. At five shots against twenty-five it only
    /// nearly cancels, as section 8.1 says, because the interval is centred on the ratio of the corrected sigmas: its coverage is then exactly
    /// P(Flo / k² ≤ F ≤ Fhi / k²) with k the ratio of the two c4 factors, 94.6 percent either way round, and the simulation is held to that
    /// and the exact figure to never below 94. The F test's p-value is below 0.05 about one time in twenty when the sigmas are equal.
    /// </summary>
    [Theory]
    [InlineData(10, 10, 1.0)]
    [InlineData(10, 10, 1.5)]
    [InlineData(5, 25, 0.7)]
    [InlineData(25, 5, 1.0)]
    public void TheDispersionRatioIntervalCoversTheTrueRatioAsLabelled(int nA, int nB, double trueRatio)
    {
        const int pairs = 20000;
        var random = new StatisticsRandom((ulong)(36310 + (100 * nA) + nB + (int)(10 * trueRatio)));
        int covered = 0, rejected = 0;
        double k = double.NaN, dfA = 2.0 * (nA - 1), dfB = 2.0 * (nB - 1);
        for (int s = 0; s < pairs; s++)
        {
            var a = GroupStatistics.Rayleigh(Group(random, nA, 0.3 * trueRatio));
            var b = GroupStatistics.Rayleigh(Group(random, nB, 0.3));
            var (ratio, _, p) = GroupComparison.DispersionRatio(a, b);
            covered += ratio.Lower <= trueRatio && trueRatio <= ratio.Upper ? 1 : 0;
            rejected += p < 0.05 ? 1 : 0;
            k = a.CorrectionFactor / b.CorrectionFactor;
        }

        // What the interval's construction gives exactly, from the F distribution alone.
        double lo = Distributions.FQuantile(0.025, dfA, dfB), hi = Distributions.FQuantile(0.975, dfA, dfB);
        double exact = Distributions.F(hi / (k * k), dfA, dfB).Lower - Distributions.F(lo / (k * k), dfA, dfB).Lower;
        double coverage = (double)covered / pairs, rate = (double)rejected / pairs;
        output.WriteLine($"{nA} against {nB} shots, true ratio {trueRatio}: interval covered it in {100 * coverage:0.00}% (exactly {100 * exact:0.00}%), p < 0.05 in {100 * rate:0.00}%");

        // The standard error of a proportion near 0.95 over 20,000 pairs is about 0.0015.
        Assert.InRange(coverage - exact, -0.0045, 0.0045);
        Assert.InRange(exact, 0.94, 0.9501);
        if (nA == nB)
        {
            Assert.Equal(0.95, exact, 1e-9);
        }

        if (trueRatio == 1.0)
        {
            Assert.InRange(rate, 0.045, 0.055);
        }
    }
}
