using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 10: the comparison's figures drawn with the range each could really be.
/// <para>
/// <b>This chart carries the project's central argument, so what it says in words is worth holding.</b> Two loads reading 0.42 in and
/// 0.51 in look like a winner and a loser in a table. Whether they are depends entirely on whether their intervals overlap, and that is a
/// question a reader should not have to answer by measuring the picture with their eye.
/// </para>
/// </summary>
public class IntervalChartTests
{
    [Fact]
    public void IntervalsThatOverlapAreSaidToTellNothingApart()
    {
        var chart = new IntervalChart
        {
            Rows =
            [
                new IntervalRow("41.2 gr", 0.42, 0.34, 0.58),
                new IntervalRow("41.8 gr", 0.51, 0.41, 0.70),
            ],
        };

        Assert.True(chart.AllOverlap);
        Assert.Contains("do not tell them apart", chart.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void IntervalsThatDoNotOverlapAreSaidToShowADifference()
    {
        var chart = new IntervalChart
        {
            Rows =
            [
                new IntervalRow("41.2 gr", 0.30, 0.26, 0.36),
                new IntervalRow("41.8 gr", 0.80, 0.70, 0.95),
            ],
        };

        Assert.False(chart.AllOverlap);
        Assert.Contains("a difference these shots can see", chart.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// Touching at a single point is overlapping. The two intervals share a value, so the evidence does not separate them, and rounding a
    /// boundary into a verdict is exactly the kind of false confidence this chart exists to prevent.
    /// </summary>
    [Fact]
    public void TouchingAtOnePointCountsAsOverlapping()
    {
        var chart = new IntervalChart
        {
            Rows =
            [
                new IntervalRow("a", 0.30, 0.20, 0.40),
                new IntervalRow("b", 0.50, 0.40, 0.60),
            ],
        };

        Assert.True(chart.AllOverlap);
    }

    /// <summary>One group is not a comparison, and neither is a group whose figures carry no interval.</summary>
    [Fact]
    public void OneGroupIsNotAComparison()
    {
        Assert.False(new IntervalChart { Rows = [new IntervalRow("only one", 0.42, 0.34, 0.58)] }.AllOverlap);
        Assert.False(new IntervalChart
        {
            Rows = [new IntervalRow("a", 0.42, null, null), new IntervalRow("b", 0.51, null, null)],
        }.AllOverlap);
    }

    [Fact]
    public void WithNothingToCompareItSaysSo()
    {
        Assert.Equal("Nothing to compare yet.", new IntervalChart().Description);
    }
}
