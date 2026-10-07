using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>Question 34, entry 386 section 5: a group pooled from several sheets, measured each from its own centre first.</summary>
public class PooledSpreadTests
{
    private static readonly PointD[] Cross = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    [Fact]
    public void EachSheetFromItsOwnCentreIsTheHeadlineAndTheMovementBetweenThemIsItsOwnFigure()
    {
        var shots = Cross.Select(p => ("A", p)).Concat(Cross.Select(p => ("B", new PointD(p.X + 2, p.Y)))).ToList();
        var pooled = PooledSpread.Of(shots)!;
        Assert.Equal(2, pooled.Sheets);
        Assert.Equal(8, pooled.Shots);
        Assert.Equal(1, pooled.WithinMeanRadius, 9);
        Assert.Equal(1, pooled.Movement, 9);
        Assert.Equal((2 + (2 * Math.Sqrt(2))) / 4, pooled.AllMeanRadius, 9);
        Assert.StartsWith("Pooled from 2 sheets: mean radius 1.000 in with each sheet measured from its own center", pooled.Words(), StringComparison.Ordinal);
    }

    [Fact]
    public void OneSheetOrSheetsOfOneShotHaveNothingToSeparate()
    {
        Assert.Null(PooledSpread.Of([.. Cross.Select(p => ("A", p))]));
        Assert.Null(PooledSpread.Of([("A", new PointD(0, 0)), ("B", new PointD(1, 0)), ("C", new PointD(2, 0))]));
    }

    [Fact]
    public void GroupLabsOwnExportsNameTheirSheetAndSessionForEachKeptRow()
    {
        var table = ShotCsv.Read("shot,x right (in),y up (in),sheet,session\n1,0.1,0.2,GL-CF25-LTR,Monday\n2,0.3,0.1,GL-CF25-LTR,Monday\nTotal,,,,\n3,0.2,0.2,GL-CF25-LTR,Tuesday\n");
        Assert.Equal(["GL-CF25-LTR|Monday", "GL-CF25-LTR|Monday", "GL-CF25-LTR|Tuesday"], ShotCsv.SheetsOf(table, 1, 2));
        Assert.Null(ShotCsv.SheetsOf(ShotCsv.Read("x,y\n1,2\n"), 0, 1));
    }
}
