using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 113 section 2: comparing loads. Every negative carries what it could have detected, the loads are never
/// ranked by point estimate, and overlapping intervals are said to leave the loads unseparated.
/// </summary>
public class LoadComparisonTests
{
    private static List<PointD> Group(int seed, int n, double sigma, double dx = 0)
    {
        var random = new Random(seed);
        double Normal() => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        return [.. Enumerable.Range(0, n).Select(_ => new PointD(dx + (sigma * Normal()), sigma * Normal()))];
    }

    private static string Inches(double value) => value.ToString("0.000", CultureInfo.InvariantCulture) + " in";

    [Fact]
    public void TwoLoadsTheDataCannotSeparateAreSaidToBeUnseparatedWithWhatTheTestCouldHaveSeen()
    {
        // The first group is the wider one, so a ranking by point estimate would put it second: the report keeps the order it was given.
        var report = LoadComparison.Compare([("41.5 gr", Group(1, 10, 0.12)), ("42.1 gr", Group(2, 10, 0.10))], Inches);
        Assert.Equal(["41.5 gr", "42.1 gr"], report.Groups.Select(g => g.Name));
        Assert.True(report.Groups[0].Rayleigh.Sigma.Value > report.Groups[1].Rayleigh.Sigma.Value);
        Assert.Equal("These two loads are not distinguishable on this evidence.", report.Headline);
        Assert.True(report.IntervalsOverlap);
        Assert.Contains(report.Explanation, e => e.StartsWith("The sigma intervals overlap, so the data do not separate them.", StringComparison.Ordinal));

        string detectable = (100 * (SampleSize.MinimumDetectableRatio(10) - 1)).ToString("0", CultureInfo.InvariantCulture);
        Assert.Equal($"With 10 shots a load, this test detects a difference in dispersion of about {detectable} percent or more 80 percent of the time, at the 5 percent level; smaller differences it will usually miss.", report.Tests[0].Power);
        Assert.EndsWith("That is not evidence that they are the same.", report.Tests[0].Verdict, StringComparison.Ordinal);
        Assert.Equal("Group centre, Hotelling's T squared", report.Tests[1].Name);
        Assert.StartsWith("It detects a shift between centres of about ", report.Tests[1].Power, StringComparison.Ordinal);
        Assert.All(report.Tests, t => Assert.False(string.IsNullOrEmpty(t.Power)));
        Assert.Equal([434, 81, 26], report.Resolve.Select(r => r.ShotsPerLoad));
    }

    [Fact]
    public void AClearDifferenceIsReportedAsOneWithItsInterval()
    {
        var report = LoadComparison.Compare([("wide", Group(3, 30, 0.3)), ("tight", Group(4, 30, 0.1, dx: 0.4))], Inches);
        Assert.Equal("These two loads differ in dispersion and in where they group.", report.Headline);
        var pair = Assert.Single(report.Pairs);
        Assert.True(pair.Ratio.Lower > 1);
        Assert.Contains(report.Explanation, e => e.StartsWith("wide's sigma is ", StringComparison.Ordinal));
    }

    [Fact]
    public void MoreThanTwoLoadsAreComparedTogetherAndEveryPairIsAdjusted()
    {
        var report = LoadComparison.Compare([("a", Group(5, 8, 0.1)), ("b", Group(6, 8, 0.1)), ("c", Group(7, 8, 0.1))], Inches);
        Assert.Equal("Dispersion, Fligner-Killeen across all the groups", report.Tests[0].Name);
        Assert.Equal("Group centres, MANOVA", report.Tests[1].Name);
        Assert.Equal(3, report.Pairs.Count);
        Assert.All(report.Pairs, p => Assert.True(p.HolmPValue >= p.PValue));
        Assert.Contains(report.Explanation, e => e.Contains("Holm's method", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => LoadComparison.Compare([("a", Group(5, 8, 0.1)), ("b", Group(6, 2, 0.1))], Inches));
        Assert.Throws<ArgumentException>(() => LoadComparison.Compare([("a", Group(5, 8, 0.1))], Inches));
    }
}
