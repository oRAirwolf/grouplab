using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 3: the Shots list's short rows and its open row. Delete keeps what Not a shot kept, the bull list
/// starts with the nearest, and a place in the firing order is given once and kept with the marking.
/// </summary>
public class ShotListTests
{
    private static MarkingSession Sheet()
    {
        var session = new MarkingSession();
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            [new BullAim(1, "1", new PointD(100, 100)), new BullAim(2, "2", new PointD(400, 100)), new BullAim(3, "3", new PointD(700, 100))],
            [(new PointD(130, 60), (int?)1), (new PointD(395, 100), (int?)2), (new PointD(700, 150), (int?)3)], "a test sheet");
        return session;
    }

    [Fact]
    public void ARowSaysAcrossUpOrDownAndHowFarFromItsOwnBull()
    {
        var session = Sheet();
        var first = ShotList.Rows(session.State).First();
        Assert.Equal("1", first.Bull);
        Assert.Equal(0.30, first.AcrossInches!.Value, 6);
        Assert.Equal(0.40, first.UpInches!.Value, 6);
        Assert.Equal(0.50, first.RadiusInches!.Value, 6);
    }

    [Fact]
    public void DeleteNotAHoleKeepsTheMarkItsProvenanceAndUndo()
    {
        var session = Sheet();
        var shot = session.State.Shots[1];
        session.SetNotAShot(shot.Id, true);
        var row = ShotList.Rows(session.State).Last();
        Assert.Equal(shot.Id, row.ShotId);
        Assert.True(row.NotAHole);
        Assert.Equal(shot.Image, session.State.Find(shot.Id)!.Image);
        Assert.Equal("Found by GroupLab, moved by you", ShotList.From(session.State.Find(shot.Id)!));
        session.Undo();
        Assert.False(session.State.Find(shot.Id)!.NotAShot);
    }

    [Fact]
    public void TheBullListStartsWithTheNearestMarkedSoAndEndsWithNoBull()
    {
        var session = Sheet();
        var options = ShotList.Bulls(session.State, session.State.Shots[1].Id);
        Assert.Equal("2 (nearest)", options[0].Words);
        Assert.Equal(ShotList.NoBull, options[^1].Words);
        Assert.Null(options[^1].Index);
    }

    [Fact]
    public void APlaceInTheFiringOrderIsHeldByOneShotAndKeptInTheFile()
    {
        var session = Sheet();
        var (a, b) = (session.State.Shots[0].Id, session.State.Shots[1].Id);
        session.SetFired(a, 1);
        session.SetFired(b, 2);
        session.SetFired(b, 1);
        Assert.Equal(1, session.State.Find(b)!.Fired);
        Assert.Equal(2, session.State.Find(a)!.Fired);
        Assert.Equal(["Not known", "1st", "2nd", "3rd"], ShotList.FiredChoices(session.State));

        var read = MarkingFile.Read(MarkingFile.Write(session.State)).State;
        Assert.Equal(1, read.Find(b)!.Fired);
        Assert.Null(read.Find(session.State.Shots[2].Id)!.Fired);
    }

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(11, "11th")]
    [InlineData(22, "22nd")]
    public void OrdinalsReadAsSpoken(int n, string said) => Assert.Equal(said, ShotList.Ordinal(n));
}
