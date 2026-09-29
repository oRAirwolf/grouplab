using Avalonia.Headless.XUnit;
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

    /// <summary>Dots with no range are not a comparison, and the chart does not read "no overlap" into them (entry 295 section 1.4).</summary>
    [Fact]
    public void DotsWithNoRangeClaimNoDifference()
    {
        var chart = new IntervalChart { Rows = [new IntervalRow("a", 1.94, null, null), new IntervalRow("b", 2.67, null, null)] };
        Assert.False(chart.HasRanges);
        Assert.Contains("no range to compare", chart.Description, StringComparison.Ordinal);
        Assert.Equal("what the comparison says", new IntervalChart { Rows = chart.Rows, Says = "what the comparison says" }.Description);
    }

    /// <summary>
    /// Entry 295 section 1: each name on its own line, its range and value on the line beneath, and nothing drawn over anything else, from a
    /// phone's narrowest card to the desktop's width, at the desktop's text size and at twice the phone's; the chart as tall as its rows.
    /// </summary>
    [AvaloniaFact]
    public void NoNameValueOrRangeIsDrawnOverAnother()
    {
        var chart = new IntervalChart
        {
            Rows =
            [
                new IntervalRow("GroupLab 5x5 Load Development with Load Block, Letter", 0.72, 0.55, 0.95),
                new IntervalRow("GroupLab 5x5 Load Development with Load Block, Letter, 2026-09-29", 0.85, 0.66, 1.12),
                new IntervalRow("41.5 gr", 0.61, null, null),
            ],
            Length = inches => inches.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " MOA",
        };
        foreach (double size in (double[])[GroupLab.App.Theme.Tokens.SecondarySize, 28])
        {
            chart.TextSize = size;
            foreach (double width in (double[])[180, 256, 320, 700])
            {
                var (rows, height) = chart.Places(width);
                var boxes = rows.SelectMany(r => new[] { r.Name, r.Value, r.Bar }).ToList();
                for (int i = 0; i < boxes.Count; i++)
                {
                    Assert.True(boxes[i].Right <= width + 0.5, $"a box runs past the edge at {width} wide, text {size}");
                    for (int j = i + 1; j < boxes.Count; j++)
                    {
                        Assert.False(boxes[i].Intersects(boxes[j]), $"{boxes[i]} and {boxes[j]} overlap at {width} wide, text {size}");
                    }
                }

                Assert.Equal(boxes.Max(b => b.Bottom), height, 3);
                chart.Measure(new Avalonia.Size(width, double.PositiveInfinity));
                Assert.InRange(chart.DesiredSize.Height, height - 1, height + 1);
            }
        }
    }
}
