using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 169 section 8: shot coordinates out as CSV for spreadsheets, and in from any program's CSV through a mapping
/// of columns and units, with no program's format assumed.
/// </summary>
public class ShotCsvTests
{
    [Fact]
    public void AnyCommonSeparatorAndQuotedFieldsAreRead()
    {
        var table = ShotCsv.Read("\"Shot, as fired\";X (mm);Y (mm)\r\n1;-5,1;3,0\r\n\r\n\"2\";4.2;-1.5\r\n");
        Assert.Equal(["Shot, as fired", "X (mm)", "Y (mm)"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(1, ShotCsv.Guess(table, across: true));
        Assert.Equal(2, ShotCsv.Guess(table, across: false));
    }

    [Fact]
    public void UnitsAndDirectionBecomeInchesOnTheScreensAxes()
    {
        var table = ShotCsv.Read("x,y\n25.4,25.4\n1,-2\nTotal,\n");
        var (mm, skipped) = ShotCsv.Shots(table, 0, 1, CoordinateUnit.Millimetre, yUpIsPositive: true, null);
        Assert.Equal(1, skipped);
        Assert.Equal(1, mm[0].X, 12);
        Assert.Equal(-1, mm[0].Y, 12);   // up on the target is negative y on the screen

        // One MOA at 100 yards is about 1.047 in.
        var (moa, _) = ShotCsv.Shots(ShotCsv.Read("x,y\n1,0\n"), 0, 1, CoordinateUnit.Moa, yUpIsPositive: true, 3600);
        Assert.Equal(1.047, moa[0].X, 3);
        Assert.Throws<ArgumentException>(() => ShotCsv.Shots(table, 0, 1, CoordinateUnit.Mil, true, null));
    }

    [Fact]
    public void AnImportedMarkingMeasuresLikeAMarkedOneAndWritesBackTheSameOffsets()
    {
        var offsets = new List<PointD> { new(-0.2, 0.1), new(0.15, -0.05), new(0.02, 0.24), new(-0.1, -0.18), new(0.12, 0.06) };
        var state = ShotCsv.Marking(offsets, 3600);
        Assert.NotNull(GroupAnalysis.Analyse(state).AllShots?.MeanRadius);

        var back = ShotCsv.Read(ShotCsv.Write(state));
        var (again, _) = ShotCsv.Shots(back, 2, 3, CoordinateUnit.Inch, yUpIsPositive: true, null);
        Assert.Equal(offsets.Select(o => o.X).Order(), again.Select(o => Math.Round(o.X, 4)).Order());
        Assert.Contains("x right (MOA at 100 yd)", back.Headers);
    }
}
