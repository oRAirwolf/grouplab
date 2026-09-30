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
        Assert.Contains(report.Explanation, e => e.StartsWith("Each load's spread could really be anywhere in a range, and those ranges overlap (the sigma intervals)", StringComparison.Ordinal));

        string detectable = (100 * (SampleSize.MinimumDetectableRatio(10) - 1)).ToString("0", CultureInfo.InvariantCulture);
        Assert.Equal($"With 10 shots a load, this test detects a difference in dispersion of about {detectable} percent or more 80 percent of the time, at the 5 percent level; smaller differences it will usually miss.", report.Tests[0].Power);
        Assert.EndsWith("That is not evidence that they are the same.", report.Tests[0].Verdict, StringComparison.Ordinal);
        Assert.Equal("Group center, Hotelling's T squared", report.Tests[1].Name);
        Assert.StartsWith("It detects a shift between centers of about ", report.Tests[1].Power, StringComparison.Ordinal);
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
        Assert.Equal("Group centers, MANOVA", report.Tests[1].Name);
        Assert.Equal(3, report.Pairs.Count);
        Assert.All(report.Pairs, p => Assert.True(p.HolmPValue >= p.PValue));
        Assert.Contains(report.Explanation, e => e.Contains("Holm's method", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => LoadComparison.Compare([("a", Group(5, 8, 0.1)), ("b", Group(6, 2, 0.1))], Inches));
        Assert.Throws<ArgumentException>(() => LoadComparison.Compare([("a", Group(5, 8, 0.1))], Inches));
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 295 section 1.4: on the phone the extreme spread chart said "there is a difference these shots can see"
    /// over a card saying "These two loads are not distinguishable on this evidence". The chart's sentence and the verdict are one decision
    /// now, and across many comparisons, of two loads and of three, alike and far apart, in size and in where they group, they never
    /// disagree; a figure drawn with no range says it has none and points to mean radius; and the ranges drawn for mean radius and CEP 90
    /// overlap exactly when the sigma intervals the verdict reads do.
    /// </summary>
    [Fact]
    public void TheChartsSentenceNeverDisagreesWithTheVerdict()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int seed = 0;
        foreach (int shots in (int[])[5, 10, 25])
        {
            foreach (double ratio in (double[])[1, 1.3, 2, 3.5])
            {
                foreach (double shift in (double[])[0, 0.6])
                {
                    for (int repeat = 0; repeat < 6; repeat++)
                    {
                        var two = LoadComparison.Compare([("a", Group(++seed, shots, 0.1)), ("b", Group(++seed, shots, 0.1 * ratio, shift))], Inches);
                        var three = LoadComparison.Compare(
                            [("a", Group(++seed, shots, 0.1)), ("b", Group(++seed, shots, 0.1 * ratio, shift)), ("c", Group(++seed, shots, 0.1))], Inches);
                        Agrees(two, seen);
                        Agrees(three, seen);
                    }
                }
            }
        }

        // The run reached both sides of the question, or it proved nothing.
        Assert.Contains("do not tell them apart", seen);
        Assert.Contains("a difference these shots can see", seen);
    }

    private static void Agrees(LoadComparisonReport report, HashSet<string> seen)
    {
        bool differs = report.Headline.Contains("differ in dispersion", StringComparison.Ordinal);
        bool placed = report.Headline.Contains("different places", StringComparison.Ordinal);
        foreach (string figure in (string[])["Mean radius", "CEP 90"])
        {
            string says = LoadComparison.ChartSays(report, figure, hasRange: true);
            foreach (string phrase in (string[])["do not tell them apart", "a difference these shots can see"])
            {
                if (says.Contains(phrase, StringComparison.Ordinal))
                {
                    seen.Add(phrase);
                }
            }

            if (says.Contains("a difference these shots can see", StringComparison.Ordinal) || says.Contains("finds a difference", StringComparison.Ordinal))
            {
                Assert.True(differs, $"the chart sees a difference and the verdict says \"{report.Headline}\"");
            }

            if (says.Contains("do not tell them apart", StringComparison.Ordinal) || says.Contains("finds no difference", StringComparison.Ordinal))
            {
                Assert.False(differs, $"the chart sees none and the verdict says \"{report.Headline}\"");
            }

            Assert.False(placed && says.EndsWith("do not tell them apart.", StringComparison.Ordinal),
                "the chart says the loads are not told apart and the verdict places them differently");
            Assert.Equal(report.IntervalsOverlap, says.Contains("overlaps every other", StringComparison.Ordinal) || says.StartsWith("These ranges overlap", StringComparison.Ordinal));

            // The ranges the chart draws are the ones the verdict reads.
            var ranges = report.Groups.Select(g => figure == "CEP 90" ? g.Rayleigh.Cep(0.9) : g.MeanRadius).ToList();
            Assert.Equal(report.IntervalsOverlap, ranges.Max(r => r.Lower) <= ranges.Min(r => r.Upper));
        }

        string spread = LoadComparison.ChartSays(report, "Extreme spread", hasRange: false);
        Assert.Contains("no range to compare", spread, StringComparison.Ordinal);
        Assert.Contains("mean radius", spread, StringComparison.Ordinal);
        Assert.DoesNotContain("difference these shots can see", spread, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 312 section 3: the verdict reads in plain words first, and the statistician's sentences (the ratio with its 95 percent interval
    /// and the power) are behind Details, with nothing lost between them.
    /// </summary>
    [Fact]
    public void TheVerdictSaysItInPlainWordsFirstWithTheExactFiguresBehindDetails()
    {
        Assert.Equal("Their spreads are within about a third of each other either way; these shots cannot say which is smaller.", LoadComparison.Apart("a", "b", 0.76, 1.34));
        Assert.Equal("Their spreads could be the same, or either could be up to 2.5 times the other's; these shots cannot say which.", LoadComparison.Apart("a", "b", 0.4, 1.2));
        Assert.Equal("wide spreads more than tight: by somewhere between 40 and 180 percent.", LoadComparison.Apart("wide", "tight", 1.4, 2.8));
        Assert.Equal("tight spreads less than wide: by somewhere between 20 and 50 percent.", LoadComparison.Apart("tight", "wide", 0.5, 0.8));
        Assert.Equal("a quarter", LoadComparison.Fraction(0.26));
        Assert.Equal("5 percent", LoadComparison.Fraction(0.05));

        var report = LoadComparison.Compare([("41.5 gr", Group(1, 10, 0.12)), ("42.1 gr", Group(2, 10, 0.10))], Inches);
        Assert.Equal(report.Explanation[0], report.Plain[0]);
        Assert.Contains(report.Plain, p => p.StartsWith("Their spreads ", StringComparison.Ordinal));
        Assert.Equal("To tell a 10 percent difference in spread apart would take about 434 shots each; these groups have 10 and 10.", report.Plain[^1]);
        Assert.All(report.Plain, p => Assert.DoesNotContain("95 percent", p, StringComparison.Ordinal));
        Assert.All(report.Plain, p => Assert.DoesNotContain("power", p, StringComparison.Ordinal));
        Assert.Equal(report.Explanation.Skip(1), report.Details);

        var three = LoadComparison.Compare([("a", Group(5, 8, 0.1)), ("b", Group(6, 8, 0.1)), ("c", Group(7, 8, 0.1))], Inches);
        Assert.Contains(three.Details, e => e.Contains("Holm's method", StringComparison.Ordinal));
        Assert.Contains(three.Plain, p => p.Contains("under Details", StringComparison.Ordinal));
    }
}
