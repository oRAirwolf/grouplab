using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 278 section 5: every spread figure GroupLab shows is exactly what its name and explanation say, checked on a
/// five-shot group worked by hand. Ballistic-X reported a "vertical SD" of 0.051 in for a five-shot group 0.524 in high, which no sample
/// standard deviation can be; here each figure is worked out on paper first and the code must agree.
/// </summary>
public class SpreadFiguresTests
{
    // A plus sign of five shots, in inches: one in the middle and four 2 in out along the axes. The center is (0, 0).
    private static readonly PointD[] Plus = [new(0, 0), new(2, 0), new(0, 2), new(-2, 0), new(0, -2)];

    private static GroupFigures Figures() => GroupAnalysis.Analyse(ShotCsv.Marking(Plus, null)).AllShots!;

    [Fact]
    public void ExtremeSpreadIsTheLargestCenterToCenterDistance()
    {
        // (2, 0) to (-2, 0), and (0, 2) to (0, -2): 4 in. Nothing else is further apart.
        Assert.Equal(4, Figures().ExtremeSpread!.Value, 9);
    }

    [Fact]
    public void TheAxisStandardDeviationsAreSampleStandardDeviations()
    {
        // Across: 0, 2, 0, -2, 0 about a mean of 0, squares summing to 8. With n - 1 = 4 below, the variance is 2 and the SD the square root
        // of 2, 1.41421 in. The population SD, n = 5 below, would be 1.26491: the test tells them apart. The same holds up and down.
        var f = Figures();
        Assert.Equal(Math.Sqrt(2), f.SdX!.Value, 9);
        Assert.Equal(Math.Sqrt(2), f.SdY!.Value, 9);
        Assert.NotEqual(Math.Sqrt(8.0 / 5), f.SdX.Value, 3);
    }

    [Fact]
    public void SigmaIsTheCorrectedRootMeanSquareRadius()
    {
        // Squared radii 0 + 4 + 4 + 4 + 4 = 16 on 2(n - 1) = 8 degrees of freedom: root 2. The small-sample correction divides by
        // c4(2n - 1) = c4(9) = (1/2) Gamma(4.5) / Gamma(4) = 105 root(pi) / 192, 0.969311. Sigma = 192 root 2 / (105 root pi) = 1.458989 in.
        Assert.Equal(192 * Math.Sqrt(2) / (105 * Math.Sqrt(Math.PI)), Figures().Sigma!.Value, 9);
    }

    [Fact]
    public void MeanRadiusIsSigmaTimesRootHalfPiAndSaysSo()
    {
        // Sigma times root(pi / 2) = 192 / 105 = 64 / 35 = 1.828571 in exactly. The plain average of these five distances, 0, 2, 2, 2, 2, is
        // 1.6 in: the figure is the model's estimate of the average distance, not that average, and the glossary says which it is.
        Assert.Equal(64.0 / 35, Figures().MeanRadius!.Value, 9);
        Assert.NotEqual(1.6, Figures().MeanRadius!.Value, 2);
        var precise = Glossary.Find("mean radius")!.Precise!;
        Assert.Contains("square root of pi over 2", precise, StringComparison.Ordinal);
        Assert.Contains("rather than the plain average", precise, StringComparison.Ordinal);
    }

    [Fact]
    public void CepFiftyIsSigmaTimesRootTwoLnTwo()
    {
        Assert.Equal(192 * Math.Sqrt(2) / (105 * Math.Sqrt(Math.PI)) * Math.Sqrt(2 * Math.Log(2)), Figures().Cep50!.Value, 9);
    }

    [Fact]
    public void TheGlossarySaysWhichStandardDeviationAndWhichSpread()
    {
        Assert.Contains("n minus 1", Glossary.Find("standard deviation")!.Precise!, StringComparison.Ordinal);
        Assert.Contains("center-to-center", Glossary.Find("extreme spread")!.Precise!, StringComparison.Ordinal);
    }
}
