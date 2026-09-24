using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 166 section 2: the first macOS tester was not sure what undo would undo. The undo button's tooltip names the
/// step, read from the two markings either side of it, by the label the shot wears on the sheet.
/// </summary>
public class ChangeWordsTests
{
    private static MarkingSession OneBull() =>
        new(MarkingState.Empty with { ImagePath = "a-target.png", Bulls = [new BullAim(0, "6", new PointD(50, 50))] });

    [Fact]
    public void NothingToUndoSaysNothing()
    {
        var session = OneBull();
        Assert.Null(session.UndoWords);
        Assert.Null(session.RedoWords);
    }

    [Fact]
    public void EachStepIsNamedByTheShotItChanged()
    {
        var session = OneBull();
        int id = session.AddShot(new PointD(51, 50), 0);
        Assert.Equal("add shot 6", session.UndoWords);

        session.MoveShot(id, new PointD(53, 50));
        Assert.Equal("move shot 6", session.UndoWords);

        session.Undo();
        Assert.Equal("move shot 6", session.RedoWords);
        Assert.Equal("add shot 6", session.UndoWords);

        session.SetNotAShot(id, true);
        Assert.Equal("mark shot 6 as not a shot", session.UndoWords);
        Assert.Null(session.RedoWords);

        session.SetNotAShot(id, false);
        session.DeleteShot(id);
        Assert.Equal("delete shot 6", session.UndoWords);
    }

    [Fact]
    public void ChangesToTheSheetAreNamedToo()
    {
        var session = OneBull();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        Assert.Equal("set the scale", session.UndoWords);
        session.SetPointOfAim(new PointD(40, 40));
        Assert.Equal("move the point of aim", session.UndoWords);
        session.Rotate(1);
        Assert.Equal("turn the view", session.UndoWords);
    }
}
