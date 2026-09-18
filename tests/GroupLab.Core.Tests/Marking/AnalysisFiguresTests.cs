using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 103: the figures the analysis state's stack gained, CEP and width by height, and the two shape tests its
/// judgement cards read, with the power sentence a negative stringing result is never printed without.
/// </summary>
public class AnalysisFiguresTests
{
    /// <summary>A group at known positions, a hundred pixels to the inch, from a point of aim at the origin.</summary>
    private static GroupFigures Figures(IReadOnlyList<PointD> inches)
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        foreach (var p in inches)
        {
            session.AddShot(new PointD(100 * p.X, 100 * p.Y));
        }

        return GroupAnalysis.Analyse(session.State).AllShots!;
    }

    private static readonly PointD[] Group =
    [
        new(0.10, 0.02), new(-0.05, 0.11), new(0.03, -0.12), new(-0.09, -0.04), new(0.12, 0.08),
        new(-0.02, 0.05), new(0.06, -0.03), new(-0.11, 0.09), new(0.01, -0.07), new(0.04, 0.13),
    ];

    [Fact]
    public void CepIsSigmaTimesItsCircularMultipleAtEachLevel()
    {
        var figures = Figures(Group);
        double sigma = figures.Sigma!.Value;
        Assert.Equal(sigma * Math.Sqrt(-2 * Math.Log(0.5)), figures.Cep50!.Value, 12);
        Assert.Equal(sigma * Math.Sqrt(-2 * Math.Log(0.1)), figures.Cep90!.Value, 12);
        Assert.Equal(sigma * Math.Sqrt(-2 * Math.Log(0.05)), figures.Cep95!.Value, 12);
        Assert.True(figures.Cep90.Lower < figures.Cep90.Value && figures.Cep90.Value < figures.Cep90.Upper);
    }

    [Fact]
    public void WidthAndHeightAreTheGroupsExtentOnEachAxisWithEachAxisSpread()
    {
        var figures = Figures(Group);
        Assert.Equal(0.12 - -0.11, figures.Width!.Value, 9);
        Assert.Equal(0.13 - -0.12, figures.Height!.Value, 9);
        var (xx, _, yy) = GroupStatistics.Covariance(Group);
        Assert.Equal(Math.Sqrt(xx), figures.SdX!.Value, 9);
        Assert.Equal(Math.Sqrt(yy), figures.SdY!.Value, 9);
    }

    /// <summary>The report carries the two tests of STATISTICS.md section 7 as the two different things they are.</summary>
    [Fact]
    public void TheReportCarriesCircularityAndStringingAsTwoTests()
    {
        var figures = Figures(Group);
        Assert.Equal(ShapeTests.Circularity(Group).PValue, figures.Circularity!.PValue, 12);
        Assert.Contains("likelihood ratio", figures.Circularity.Method, StringComparison.Ordinal);
        Assert.Equal(ShapeTests.VerticalStringing(Group).PValueVertical, figures.Stringing!.PValueVertical, 12);

        // Shots on a line leave the shape undefined, and the tests say so rather than returning a number.
        var line = Figures([.. Enumerable.Range(0, 6).Select(i => new PointD(0.05 * i, 0))]);
        Assert.Null(line.Circularity);
        Assert.NotNull(line.ShapeTestsUnavailable);
    }

    /// <summary>Section 7's table: 19 shots for 2 times, 50 for 1.5 and 155 for 1.25, at 80 percent power.</summary>
    [Theory]
    [InlineData(10, "even stringing of 2 times would be missed more than 1 time in 5: catching it 8 times in 10 needs about 19 shots, and 1.5 times needs about 50 shots and 1.25 times needs about 155 shots")]
    [InlineData(25, "catches stringing of 2 times or more at least 8 times in 10, and often misses less: 1.5 times needs about 50 shots and 1.25 times needs about 155 shots")]
    [InlineData(60, "catches stringing of 1.5 times or more at least 8 times in 10, and often misses less: 1.25 times needs about 155 shots")]
    [InlineData(200, "catches stringing of 1.25 times or more at least 8 times in 10.")]
    public void TheStringingPowerSentenceSaysWhatTheCountCouldHaveCaught(int shots, string expected)
    {
        string sentence = ShapeTests.StringingPowerSentence(shots);
        Assert.StartsWith($"From {shots} shots", sentence, StringComparison.Ordinal);
        Assert.Contains(expected, sentence, StringComparison.Ordinal);
    }
}
