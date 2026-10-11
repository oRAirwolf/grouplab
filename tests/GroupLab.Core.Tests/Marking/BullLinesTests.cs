using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 1: a line from each bull to its holes, the unsure ones marked, and a tap on a hole while a bull is
/// chosen giving it to that bull or taking it away.
/// </summary>
public class BullLinesTests
{
    /// <summary>Three bulls in a row, three hundred pixels apart, a hundred pixels an inch.</summary>
    private static MarkingSession Sheet(params (PointD At, int Bull)[] holes)
    {
        var session = new MarkingSession();
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            [new BullAim(1, "C1", new PointD(100, 100)), new BullAim(2, "C2", new PointD(400, 100)), new BullAim(3, "C3", new PointD(700, 100))],
            [.. holes.Select(h => (h.At, (int?)h.Bull))], "a test sheet");
        return session;
    }

    [Fact]
    public void EveryHoleWithABullHasOneLineFromThatBullsCenter()
    {
        var session = Sheet((new PointD(110, 105), 1), (new PointD(395, 90), 2));
        var lines = BullLines.For(session.State);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.False(l.Unsure));
        Assert.Equal(new PointD(400, 100), lines.Single(l => l.Bull == 2).From);
    }

    [Fact]
    public void ALineFarLongerThanTheSheetsOthersAndCrossingANeighbourIsUnsure()
    {
        // Three short lines, and one from C1 reaching most of the way to C2.
        var session = Sheet((new PointD(110, 100), 1), (new PointD(405, 100), 2), (new PointD(705, 100), 3), (new PointD(330, 100), 1));
        var far = session.State.Shots.Single(s => s.Image.X == 330);
        var line = BullLines.For(session.State).Single(l => l.Shot == far.Id);
        Assert.True(line.Doubt.HasFlag(LineDoubt.Long));
        Assert.True(line.Doubt.HasFlag(LineDoubt.CrossesNeighbour));
        Assert.Equal([far.Id], BullLines.Unsure(session.State));
        Assert.Contains("crosses into a neighboring bull", BullLines.Words(line.Doubt), StringComparison.Ordinal);
    }

    [Fact]
    public void AWholeGroupLandedLowTogetherLightsNoLine()
    {
        // Every hole an inch low: every line long, and none long for this sheet.
        var session = Sheet((new PointD(100, 200), 1), (new PointD(400, 200), 2), (new PointD(700, 200), 3));
        Assert.Empty(BullLines.Unsure(session.State));
    }

    [Fact]
    public void ABullAPersonChoseIsNeverDrawnUnsure()
    {
        var session = Sheet((new PointD(110, 100), 1), (new PointD(405, 100), 2), (new PointD(705, 100), 3), (new PointD(330, 100), 1));
        var far = session.State.Shots.Single(s => s.Image.X == 330);
        session.AssignBull(far.Id, 1);
        Assert.Empty(BullLines.Unsure(session.State));
    }

    [Fact]
    public void NextUnsureGoesRoundToTheFirst()
    {
        var session = Sheet((new PointD(110, 100), 1), (new PointD(405, 100), 2), (new PointD(705, 100), 3), (new PointD(330, 100), 1));
        int only = BullLines.Unsure(session.State).Single();
        Assert.Equal(only, BullLines.NextUnsure(session.State, null));
        Assert.Equal(only, BullLines.NextUnsure(session.State, only));
    }

    [Fact]
    public void TappingAHoleWhileABullIsChosenGivesItToThatBull()
    {
        var session = Sheet((new PointD(110, 100), 1), (new PointD(405, 100), 2));
        var hole = session.State.Shots.Single(s => s.Bull == 2);
        string said = BullChoosing.Tap(session, 3, hole.Id);
        Assert.Equal(3, session.State.Find(hole.Id)!.Bull);
        Assert.True(session.State.Find(hole.Id)!.BullChosen);
        Assert.Contains("to bull C3", said, StringComparison.Ordinal);
        Assert.Equal("C3 has 1 shot", BullChoosing.Holds(session.State, 3));
    }

    [Fact]
    public void TakingAHoleAwayPutsItBackOnItsNearestBullUnchosen()
    {
        var session = Sheet((new PointD(110, 100), 1), (new PointD(405, 100), 2));
        var hole = session.State.Shots.Single(s => s.Bull == 2);
        session.AssignBull(hole.Id, 3);
        string said = BullChoosing.Tap(session, 3, hole.Id);
        var after = session.State.Find(hole.Id)!;
        Assert.Equal(2, after.Bull);
        Assert.False(after.BullChosen);
        Assert.Contains("on bull C2 now", said, StringComparison.Ordinal);
    }

    [Fact]
    public void TakingAHoleAwayFromItsOwnNearestBullMovesItToTheNextNearest()
    {
        var session = Sheet((new PointD(110, 100), 1), (new PointD(405, 100), 2));
        var hole = session.State.Shots.Single(s => s.Bull == 2);
        session.TakeFromBull(hole.Id);
        var after = session.State.Find(hole.Id)!;
        Assert.NotEqual(2, after.Bull);
        Assert.True(after.BullChosen, "taking it away put it straight back");
        session.Undo();
        Assert.Equal(2, session.State.Find(hole.Id)!.Bull);
    }

    [Fact]
    public void ANumberedBullIsNamedAsABull()
    {
        Assert.Equal("Bull 7 chosen. Tap a hole to give it to bull 7, or tap one of its holes to take it away.", BullChoosing.Banner("7"));
        var session = new MarkingSession();
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), [new BullAim(1, "7", new PointD(100, 100))], [(new PointD(101, 100), (int?)1)], "a test sheet");
        Assert.Equal("Bull 7 has 1 shot", BullChoosing.Holds(session.State, 1));
    }

    [Fact]
    public void TheBannerSaysWhatATapDoes() =>
        Assert.Equal("Bull C3 chosen. Tap a hole to give it to C3, or tap one of its holes to take it away.", BullChoosing.Banner("C3"));
}
