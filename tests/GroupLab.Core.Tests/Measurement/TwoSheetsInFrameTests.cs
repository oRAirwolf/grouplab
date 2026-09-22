using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2c: more than one sheet in the frame.
/// <para>
/// <b>The fault was silence, not inaccuracy.</b> Marker ids are unique on a sheet, so two copies of one sheet in a photograph repeat every
/// id. The first marker with each id won by the order the detector happened to list them, the rest were counted "unexpected", and nothing
/// anywhere said a second sheet was in view. The figures were about one of the two targets and the person could not tell which.
/// </para>
/// </summary>
public class TwoSheetsInFrameTests
{
    /// <summary>A marker of the given id, square, with its top-left corner where it is put.</summary>
    private static DetectedMarker At(int id, double x, double y, double side = 40) =>
        new(id, [new PointD(x, y), new PointD(x + side, y), new PointD(x + side, y + side), new PointD(x, y + side)]);

    /// <summary>One sheet's four markers, at a corner of the frame, at a size.</summary>
    private static List<DetectedMarker> Sheet(double x, double y, double side, params int[] ids) =>
        [.. ids.Select((id, i) => At(id, x + ((i % 2) * 400), y + ((i / 2) * 400), side))];

    private static (IReadOnlyList<DetectedMarker> Chosen, int SheetsInView, string? WhichSheet) Read(IReadOnlyList<DetectedMarker> markers)
    {
        var trace = new TraceRecorder();
        using var stage = trace.Begin("S2.fiducials");
        return SheetMeasurer.OneSheet(markers, stage);
    }

    /// <summary>One sheet is the ordinary case and nothing is said about it, because there is nothing to say.</summary>
    [Fact]
    public void OneSheetSaysNothing()
    {
        var one = Sheet(0, 0, 40, 1, 2, 3, 4);

        var (chosen, sheets, which) = Read(one);

        Assert.Equal(1, sheets);
        Assert.Null(which);
        Assert.Equal(one.Count, chosen.Count);
    }

    /// <summary>
    /// Two copies of the same sheet: it knows there are two, measures one of them, and says which. The one with every marker decoded wins
    /// over the one with fewer, because a sheet whose markers all read is the one the measurement can trust.
    /// </summary>
    [Fact]
    public void TwoSheetsAreSeenAndTheCompleteOneIsMeasuredAndNamed()
    {
        // The left sheet has all four markers; the right one is half out of frame and has two.
        var markers = Sheet(0, 0, 40, 1, 2, 3, 4).Concat(Sheet(1600, 0, 40, 1, 2)).ToList();

        var (chosen, sheets, which) = Read(markers);

        Assert.Equal(2, sheets);
        Assert.Equal(4, chosen.Count);
        Assert.All(chosen, m => Assert.True(m.Corners[0].X < 1000, "a marker from the half-seen sheet was measured"));
        Assert.NotNull(which);
        Assert.Contains("2 sheets are in view", which, StringComparison.Ordinal);
        Assert.Contains("4 of its markers decoded", which, StringComparison.Ordinal);
    }

    /// <summary>With both sheets complete, the larger in the frame wins: it is nearer the camera and most square to it.</summary>
    [Fact]
    public void WithBothCompleteTheLargerInTheFrameWins()
    {
        var markers = Sheet(0, 0, 30, 1, 2, 3, 4).Concat(Sheet(1600, 0, 60, 1, 2, 3, 4)).ToList();

        var (chosen, sheets, which) = Read(markers);

        Assert.Equal(2, sheets);
        Assert.Equal(4, chosen.Count);
        Assert.All(chosen, m => Assert.True(m.Corners[0].X >= 1600, "the smaller sheet in the frame was measured"));
        Assert.NotNull(which);
    }

    /// <summary>No marker of one id is ever counted twice into one sheet, which is what would let two sheets be measured as one.</summary>
    [Fact]
    public void ASheetNeverTakesTheSameIdTwice()
    {
        var markers = Sheet(0, 0, 40, 1, 2, 3, 4).Concat(Sheet(1600, 0, 40, 1, 2, 3, 4)).ToList();

        var (chosen, _, _) = Read(markers);

        Assert.Equal(chosen.Select(m => m.Id).Distinct().Count(), chosen.Count);
    }

    /// <summary>Three copies are counted as three, so the number in the sentence is the number in the frame.</summary>
    [Fact]
    public void ThreeCopiesAreCountedAsThree()
    {
        var markers = Sheet(0, 0, 40, 1, 2, 3, 4)
            .Concat(Sheet(1600, 0, 40, 1, 2, 3, 4))
            .Concat(Sheet(3200, 0, 40, 1, 2, 3, 4))
            .ToList();

        var (_, sheets, which) = Read(markers);

        Assert.Equal(3, sheets);
        Assert.Contains("3 sheets are in view", which!, StringComparison.Ordinal);
    }

    /// <summary>The choice is in the trace beside the tile's, so a reader can see what was passed over as well as what was taken.</summary>
    [Fact]
    public void TheChoiceIsRecordedWithWhatItPassedOver()
    {
        var trace = new TraceRecorder();
        using (var stage = trace.Begin("S2.fiducials"))
        {
            SheetMeasurer.OneSheet([.. Sheet(0, 0, 40, 1, 2, 3, 4).Concat(Sheet(1600, 0, 40, 1, 2))], stage);
        }

        var decision = Assert.Single(trace.Records.SelectMany(r => r.Decisions), d => d.What == "sheet");
        Assert.Equal("1 of 2 in view", decision.Chosen);
        Assert.Contains("4 of its markers decoded", decision.Because, StringComparison.Ordinal);
        Assert.Contains(decision.Alternatives, i => i.Contains("2 markers decoded", StringComparison.Ordinal));
    }
}
