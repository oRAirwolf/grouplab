using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// The engine against docs/STATISTICS.md's own published figures, which need no shotGroups: the constants of sections 3.4 and
/// 12.5, the sample-size tables of section 9, the flyer table of section 10 and the pooled degrees of freedom of section 11, each
/// to the digits the document prints; and section 15.5's known-truth gates, points 4 and 6, on simulated groups.
/// </summary>
public class SpecificationTableTests(ITestOutputHelper output)
{
    private static void Printed(double expected, double actual, int decimals) =>
        Assert.True(Math.Abs(Math.Round(actual, decimals) - expected) <= 0.5 * Math.Pow(10, -decimals) + 1e-12, $"expected {expected} to {decimals} decimals, got {actual:R}");

    [Fact]
    public void Section34MultiplesAreTheClosedForms()
    {
        Printed(1.2533141373, RayleighEstimate.MeanRadiusFactor, 10);

        // Section 3.4 prints 1.1774100226; sqrt(2 ln 2) is 1.17741002251547..., which rounds to ...225. The table's last digit is
        // a slip, so the constant is checked against its closed form instead.
        Assert.Equal(RayleighEstimate.MedianRadiusFactor, Math.Sqrt(2 * Math.Log(2)), 15);
        Printed(0.6551363776, RayleighEstimate.RadialSdFactor, 10);
        var unit = GroupStatistics.FromSumOfSquares(2, 2, 3);
        double sigma = unit.Sigma.Value;
        Printed(2.1459660263, unit.Cep(0.90).Value / sigma, 10);
        Printed(2.4477468307, unit.Cep(0.95).Value / sigma, 10);
    }

    [Fact]
    public void Section125AnchorsHold()
    {
        Printed(1.000000, Angular.ToAngle(1, 100, 36, AngularUnit.Smoa), 6);
        Printed(0.954930, Angular.ToAngle(1, 100, 36, AngularUnit.Moa), 6);
        Printed(0.95492965241113508368, Angular.Constant(AngularUnit.Moa) / Angular.Constant(AngularUnit.Smoa), 15);
    }

    [Theory]
    [InlineData(3, 35.4, 0.599, 2.874)]
    [InlineData(5, 25.0, 0.675, 1.916)]
    [InlineData(10, 16.7, 0.756, 1.479)]
    [InlineData(15, 13.4, 0.794, 1.352)]
    [InlineData(20, 11.5, 0.817, 1.289)]
    [InlineData(25, 10.2, 0.834, 1.249)]
    [InlineData(30, 9.3, 0.847, 1.222)]
    [InlineData(50, 7.1, 0.877, 1.163)]
    [InlineData(100, 5.0, 0.910, 1.109)]
    public void Section91TableIsReproduced(int n, double cvPercent, double lower, double upper)
    {
        Printed(cvPercent, 100 * SampleSize.SigmaCoefficientOfVariation(n), 1);
        var (lo, hi) = SampleSize.SigmaIntervalMultiples(n);
        Printed(lower, lo, 3);
        Printed(upper, hi, 3);
    }

    [Theory]
    [InlineData(1.05, 1650, 1651)]
    [InlineData(1.10, 434, 434)]
    [InlineData(1.15, 202, 203)]
    [InlineData(1.25, 80, 81)]
    [InlineData(1.50, 25, 26)]
    [InlineData(2.00, 10, 10)]
    public void Section92TableIsReproduced(double ratio, int approximation, int exact)
    {
        Assert.Equal(approximation, SampleSize.ShotsPerLoadApproximation(ratio));
        Assert.Equal(exact, SampleSize.ShotsPerLoad(ratio));
    }

    [Theory]
    [InlineData(3, 1.825, 1.456, 0.124, 0.430)]
    [InlineData(5, 2.068, 1.650, 0.198, 0.608)]
    [InlineData(10, 2.370, 1.891, 0.357, 0.846)]
    [InlineData(15, 2.533, 2.021, 0.485, 0.940)]
    [InlineData(20, 2.644, 2.110, 0.587, 0.976)]
    [InlineData(25, 2.727, 2.176, 0.669, 0.991)]
    [InlineData(30, 2.794, 2.229, 0.734, 0.996)]
    public void Section10TableIsReproduced(int n, double worstInSigmas, double worstInMeanRadii, double beyondTwice, double beyondOneAndAHalf)
    {
        Printed(worstInSigmas, Flyers.ExpectedWorstInSigmas(n), 3);
        Printed(worstInMeanRadii, Flyers.ExpectedWorstInMeanRadii(n), 3);
        Printed(beyondTwice, Flyers.ProbabilityWorstBeyond(n, 2), 3);
        Printed(beyondOneAndAHalf, Flyers.ProbabilityWorstBeyond(n, 1.5), 3);
    }

    [Fact]
    public void Section10SimulatedFiguresAgreeToFourDecimals()
    {
        // Section 10's table prints 2.534 at 15 shots. The alternating sum evaluated at 60 significant digits gives
        // 2.5334504175890846, so the printed digit is a slip and the row above carries the correct 2.533.
        Assert.Equal(2.5334504175890846, Flyers.ExpectedWorstInSigmas(15), 12);
        Printed(2.0675, Flyers.ExpectedWorstInSigmas(5), 4);
        Printed(2.7274, Flyers.ExpectedWorstInSigmas(25), 4);
    }

    [Fact]
    public void Section11EightTwentyFiveShotTargetsPoolTo384DegreesOfFreedom()
    {
        var random = new StatisticsRandom(11);
        var targets = Enumerable.Range(0, 8).Select(_ => (IReadOnlyList<PointD>)[.. Enumerable.Range(0, 25).Select(_ => new PointD(random.NextNormal(), random.NextNormal()))]).ToList();
        Assert.Equal(384, Pooling.Recentred(targets).DegreesOfFreedom);
        Assert.Equal(398, Pooling.AllIn(targets).DegreesOfFreedom);
        Assert.Equal(28, Pooling.CompareBeforePooling(targets).Count);
    }

    /// <summary>
    /// Section 15.5 points 4 and 6: over 10,000 simulated 25-shot groups of known sigma, the corrected estimate is unbiased and its
    /// 95 percent interval covers the truth between 94.0 and 96.0 percent of the time, with the centre estimated and with it known,
    /// which differ in their degrees of freedom and their c4 argument at once (section 3.2).
    /// </summary>
    [Fact]
    public void Section155RayleighSigmaIsUnbiasedWithStatedCoverageInBothCentreConfigurations()
    {
        const int groups = 10000, n = 25;
        const double sigma = 1.7;
        var random = new StatisticsRandom(155);
        var centre = new PointD(3, -2);
        int coveredEstimated = 0, coveredKnown = 0;
        double sumEstimated = 0, sumKnown = 0;
        var shots = new PointD[n];
        for (int g = 0; g < groups; g++)
        {
            for (int i = 0; i < n; i++)
            {
                shots[i] = new PointD(centre.X + (sigma * random.NextNormal()), centre.Y + (sigma * random.NextNormal()));
            }

            var estimated = GroupStatistics.Rayleigh(shots);
            var known = GroupStatistics.Rayleigh(shots, knownCentre: centre);
            Assert.Equal(2 * (n - 1), estimated.DegreesOfFreedom);
            Assert.Equal(2 * n, known.DegreesOfFreedom);
            coveredEstimated += estimated.Sigma.Lower <= sigma && sigma <= estimated.Sigma.Upper ? 1 : 0;
            coveredKnown += known.Sigma.Lower <= sigma && sigma <= known.Sigma.Upper ? 1 : 0;
            sumEstimated += estimated.Sigma.Value;
            sumKnown += known.Sigma.Value;
        }

        double coverageEstimated = 100.0 * coveredEstimated / groups, coverageKnown = 100.0 * coveredKnown / groups;
        double biasEstimated = (sumEstimated / groups / sigma) - 1, biasKnown = (sumKnown / groups / sigma) - 1;
        output.WriteLine($"centre estimated: coverage {coverageEstimated:0.00}%, bias {biasEstimated:+0.0000;-0.0000}; centre known: coverage {coverageKnown:0.00}%, bias {biasKnown:+0.0000;-0.0000}");
        Assert.InRange(coverageEstimated, 94.0, 96.0);
        Assert.InRange(coverageKnown, 94.0, 96.0);

        // One sigma estimate's CV is about 10 percent, so the mean of 10,000 has a standard error near 0.001.
        Assert.InRange(biasEstimated, -0.004, 0.004);
        Assert.InRange(biasKnown, -0.004, 0.004);
    }

    /// <summary>
    /// Section 7's simulated sizes: Pitman-Morgan holds its nominal 5 percent at every n, and the Bartlett-corrected likelihood
    /// ratio at 20 shots runs a little over it, 0.059 in section 7's table.
    /// </summary>
    [Fact]
    public void Section7TestsHoldTheirSimulatedSizes()
    {
        const int replications = 20000;
        var random = new StatisticsRandom(7);
        int stringing = 0, circularity = 0;
        var shots10 = new PointD[10];
        var shots20 = new PointD[20];
        for (int r = 0; r < replications; r++)
        {
            for (int i = 0; i < 10; i++)
            {
                double z1 = random.NextNormal(), z2 = random.NextNormal();
                shots10[i] = new PointD(z1, (0.5 * z1) + (Math.Sqrt(0.75) * z2));
            }

            for (int i = 0; i < 20; i++)
            {
                shots20[i] = new PointD(random.NextNormal(), random.NextNormal());
            }

            stringing += ShapeTests.VerticalStringing(shots10).PValueTwoSided < 0.05 ? 1 : 0;
            circularity += ShapeTests.Circularity(shots20).PValue < 0.05 ? 1 : 0;
        }

        double sizeStringing = (double)stringing / replications, sizeCircularity = (double)circularity / replications;
        output.WriteLine($"Pitman-Morgan size at n = 10, correlation 0.5: {sizeStringing:0.0000}; Bartlett-corrected circularity size at n = 20: {sizeCircularity:0.0000}");
        Assert.InRange(sizeStringing, 0.045, 0.055);
        Assert.InRange(sizeCircularity, 0.053, 0.065);
    }

    [Fact]
    public void BootstrapIsReproducibleFromItsSeedAndFlagsSmallGroups()
    {
        var random = new StatisticsRandom(6);
        IReadOnlyList<PointD> shots = [.. Enumerable.Range(0, 25).Select(_ => new PointD(random.NextNormal(), 2 * random.NextNormal()))];
        double Radius(IReadOnlyList<PointD> s) => GroupGeometry.MinimumEnclosingCircle(s).Radius;
        var first = Bootstrap.Interval(shots, Radius, resamples: 999, seed: 42);
        var second = Bootstrap.Interval(shots, Radius, resamples: 999, seed: 42);
        Assert.Equal(first, second);
        Assert.True(first.Lower < first.Upper && first.Reliable);
        Assert.False(Bootstrap.Interval(shots.Take(8).ToList(), Radius, resamples: 199).Reliable);
    }
}
