using GroupLab.App;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.3: did the group open up as it was shot?
/// <para>
/// <b>The thing that has to be right is the silence.</b> A sheet does not record what order it was shot in. Numbering the holes left to
/// right and calling that the order would draw a chart that looks exactly like a real one and means nothing at all, and a reader has no way
/// to tell the two apart. So with no order known this says so and draws nothing.
/// </para>
/// </summary>
public class ShotOrderChartTests
{
    [Fact]
    public void WithNoOrderKnownItSaysSoAndDrawsNothing()
    {
        var chart = new ShotOrderChart();

        Assert.Equal("The order these shots were fired in is not known, so there is nothing to show.", chart.Description);
        Assert.Empty(chart.Radii);
    }

    /// <summary>A group that really did open up says so, and says how sure that is.</summary>
    [Fact]
    public void AGroupThatOpenedUpSaysSo()
    {
        double[] radii = [0.05, 0.09, 0.14, 0.20, 0.26, 0.33, 0.41, 0.50];
        var chart = new ShotOrderChart { Radii = radii, Trend = ShotOrderTrend.Of(radii) };

        Assert.Contains("opened up as it was shot", chart.Description, StringComparison.Ordinal);
        Assert.Contains("8 shots are enough to say so", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// And the case that matters more: a group that wanders, as every group does, and cannot support the shape somebody will see in it.
    /// </summary>
    [Fact]
    public void AGroupThatOnlyWandersSaysTheShotsCannotTell()
    {
        // Ranks 4, 7, 2, 8, 1, 6, 3, 5: a group that wanders about its own average and goes nowhere.
        double[] radii = [0.20, 0.28, 0.13, 0.33, 0.11, 0.25, 0.16, 0.22];
        var chart = new ShotOrderChart { Radii = radii, Trend = ShotOrderTrend.Of(radii) };

        Assert.True(Math.Abs(chart.Trend!.Correlation) < 0.2, $"the example is not trendless: correlation {chart.Trend.Correlation}");
        Assert.Contains("cannot tell that from chance", chart.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("enough to say so", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>Too few shots for the test is not the same as no order at all, and it says the different thing.</summary>
    [Fact]
    public void TooFewShotsForTheTestSaysThatRatherThanNothing()
    {
        double[] radii = [0.10, 0.20, 0.15];
        var chart = new ShotOrderChart { Radii = radii, Trend = ShotOrderTrend.Of(radii) };

        Assert.Contains("3 shots in the order fired", chart.Description, StringComparison.Ordinal);
        Assert.Contains("cannot show a trend at all", chart.Description, StringComparison.Ordinal);
    }
}
