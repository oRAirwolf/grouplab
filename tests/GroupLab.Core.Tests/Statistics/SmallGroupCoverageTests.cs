using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// What the marking panel's intervals actually cover at the small shot counts people fire, NOTES-FROM-PLANNING.md entry 24 section 1
/// and entry 23 section 2. Sigma's and mean radius's intervals are closed form, section 3.3, with the c4 correction on both endpoints,
/// so their coverage of the true sigma is exactly P(q_lo / k² ≤ χ²(2(n−1)) ≤ q_hi / k²) with k = 1 / c4(2n − 1), computed by
/// <see cref="IntervalCoverage.RayleighSigma"/> and checked here by simulation. Extreme spread's two interval forms are measured by
/// simulation: shotGroups' <see cref="RangeStatistics.Interval"/>, and <see cref="RangeStatistics.MeanInterval"/>, which the panel uses.
/// </summary>
public class SmallGroupCoverageTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void ThePanelIntervalsHaveTheCoverageTheyAreLabelledWith(int n)
    {
        double exact = IntervalCoverage.RayleighSigma(n);
        const int groups = 40000;
        var random = new StatisticsRandom((ulong)(2400 + n));
        var shots = new PointD[n];
        int sigmaCovered = 0, shotGroupsFormCovered = 0, meanFormCovered = 0;
        double meanSpread = RangeStatisticsTable.Default.Lookup(RangeStatistic.ExtremeSpread, n, 1)!.Value.Mean;
        for (int g = 0; g < groups; g++)
        {
            for (int i = 0; i < n; i++)
            {
                shots[i] = new PointD(random.NextNormal(), random.NextNormal());
            }

            var rayleigh = GroupStatistics.Rayleigh(shots);
            sigmaCovered += rayleigh.Sigma.Lower <= 1 && 1 <= rayleigh.Sigma.Upper ? 1 : 0;
            double observed = GroupGeometry.MaximumPairDistance(shots).Distance;
            var shotGroupsForm = RangeStatistics.Interval(RangeStatistic.ExtremeSpread, observed, n);
            shotGroupsFormCovered += shotGroupsForm.Lower <= meanSpread && meanSpread <= shotGroupsForm.Upper ? 1 : 0;
            var meanForm = RangeStatistics.MeanInterval(RangeStatistic.ExtremeSpread, observed, n);
            meanFormCovered += meanForm.Lower <= meanSpread && meanSpread <= meanForm.Upper ? 1 : 0;
        }

        double simulated = (double)sigmaCovered / groups, shotGroupsCoverage = (double)shotGroupsFormCovered / groups, meanCoverage = (double)meanFormCovered / groups;
        output.WriteLine($"n = {n}: sigma and mean radius, exact {100 * exact:0.00}%, simulated {100 * simulated:0.00}%; extreme spread, shotGroups' form {100 * shotGroupsCoverage:0.00}%, the panel's form {100 * meanCoverage:0.00}%");

        // The standard error of a proportion near 0.95 over 40,000 groups is about 0.0011.
        Assert.InRange(simulated - exact, -0.0045, 0.0045);
        Assert.InRange(meanCoverage, 0.95 - 0.0045, 0.95 + 0.0045);
    }

    /// <summary>The exact coverage rises toward the nominal 95 percent as the c4 correction goes to 1, and is never above it.</summary>
    [Fact]
    public void TheExactCoverageIsBelowNominalAndApproachesIt()
    {
        double previous = 0;
        foreach (int n in new[] { 2, 3, 5, 10, 20, 50, 100 })
        {
            double coverage = IntervalCoverage.RayleighSigma(n);
            Assert.True(coverage > previous && coverage < 0.95, $"n = {n}: {coverage}");
            previous = coverage;
        }

        Assert.InRange(IntervalCoverage.RayleighSigma(100), 0.949, 0.95);
    }
}
